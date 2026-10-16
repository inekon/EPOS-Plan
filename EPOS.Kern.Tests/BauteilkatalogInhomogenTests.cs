using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>BK-8: inhomogene Aufbauten im Bauteilkatalog-Import</b> — Abschnitte mit verschiedener Schichtzahl werden an
    /// gemeinsamen Schichtgrenzen geteilt und rechnen nach DIN EN ISO 6946, 6.7 (Handrechnung), ein fehlender Flächenanteil
    /// eines einzigen Abschnitts wird als Rest zu 1 eingesetzt; je Fall Annahme und Ablehnung, die Gleichheit für Aufbauten mit
    /// gleicher Schichtzahl und die Zählung der Hinweise in der Vorschau. Synthetische Dateien im Format des Exports, runde Werte.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class BauteilkatalogInhomogenTests : IDisposable
    {
        private readonly List<string> _ordner = new List<string>();

        public void Dispose()
        {
            foreach (string o in _ordner)
                try { Directory.Delete(o, true); } catch (IOException) { }
        }

        private const string KOPF_B = "UId;ShortDesc;LongDesc;InfoDesc;HeatConductivity;Density;HeatCapacity;MaterialGroupType;MaterialType;IsCapillary;MoistureContent;StampEdit";
        private const string KOPF_A = "UId;BasisUId;ShortDesc;LongDesc;InfoDesc;Thickness;UValue;RValue;InternalCoefficientOfHeatTransfer;ExternalCoefficientOfHeatTransfer;FractionOfFrame;GValue;StampEdit";
        private const string KOPF_S = "UId;DimensionUId;LayerSectionUId;SeqNum;ShortDesc;LongDesc;Thickness;ThermalConductivity;Density;HeatCapacity;MaterialGroupType;RValue";
        private const string KOPF_X = "UId;DimensionUId;SectionNum;SectionShareValve;LongDesc";
        private const string ST = "2025-03-14T10:20:30";

        private static string G(int n) => "20000000-0000-0000-0000-" + n.ToString("D12", CultureInfo.InvariantCulture);
        private static string Z(double d) => d.ToString("R", CultureInfo.InvariantCulture);

        /// <summary>Eine Schicht der Probe: (Abschnitt 1 oder 2, Dicke, λ oder null, Name).</summary>
        private sealed record Lage(int Abschnitt, double Dicke, double? Lambda, string Name = "Probeschicht");

        /// <summary>
        /// Ein Außenwandaufbau mit zwei Abschnitten (Anteile in Prozent, leer = <c>null</c>) und den Schichten je Abschnitt in
        /// Reihenfolge von innen nach außen; geplant ohne Datenbank.
        /// </summary>
        private BauteilkatalogImport.Plan Planen(double? f1, double? f2, params Lage[] lagen)
        {
            BauteilkatalogImport.Plan p = BauteilkatalogImport.Planen(BauteilkatalogQuelle.Lesen(Ordner(f1, f2, lagen)), new BauteilkatalogStand());
            Assert.Null(p.Vorschau.Abgelehnt);
            return p;
        }

        /// <summary>Schreibt die Probe in einen frischen Ordner und liefert ihn.</summary>
        private string Ordner(double? f1, double? f2, params Lage[] lagen)
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-bk8-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(o);
            _ordner.Add(o);
            var utf8 = new UTF8Encoding(false);
            var a = new List<string> { string.Join(";", G(101), G(901), "Probewand", "", "", "0.2", "", "", "", "", "", "", ST) };
            var x = new List<string>
            {
                string.Join(";", G(401), G(101), "1", f1.HasValue ? Z(f1.Value) : "", "Abschnitt eins"),
                string.Join(";", G(402), G(101), "2", f2.HasValue ? Z(f2.Value) : "", "Abschnitt zwei")
            };
            var s = new List<string>();
            int nr = 200;
            foreach (int ab in new[] { 1, 2 })
            {
                int seq = 0;
                foreach (Lage l in lagen.Where(l => l.Abschnitt == ab))
                    s.Add(string.Join(";", G(++nr), G(101), G(400 + ab), (++seq).ToString(CultureInfo.InvariantCulture), l.Name, "",
                                      Z(l.Dicke), l.Lambda.HasValue ? Z(l.Lambda.Value) : "", "500", "1000", "5", ""));
            }
            File.WriteAllText(Path.Combine(o, "Baustoffe.csv"), KOPF_B + "\r\n", utf8);
            File.WriteAllText(Path.Combine(o, "Aufbauten.csv"), KOPF_A + "\r\n" + string.Join("\r\n", a) + "\r\n", utf8);
            File.WriteAllText(Path.Combine(o, "Schichten.csv"), KOPF_S + "\r\n" + string.Join("\r\n", s) + "\r\n", utf8);
            File.WriteAllText(Path.Combine(o, "Schichten_Abschnitte.csv"), KOPF_X + "\r\n" + string.Join("\r\n", x) + "\r\n", utf8);
            File.WriteAllText(Path.Combine(o, "Fenster.csv"), "UId;DimensionUId;GlazingUValue;GlazingGValue\r\n", utf8);
            File.WriteAllText(Path.Combine(o, "Basis_Auszug.csv"), "UId;GroupType;SubType;MaterialUId\r\n" + G(901) + ";1;0;\r\n", utf8);
            return o;
        }

        /// <summary>Der geplante Aufbau als Modell, so wie ihn der Katalog nach dem Schreiben liest (Ebene, dann Abschnitt).</summary>
        private static BauteilaufbauModel Modell(BauteilkatalogImport.Plan p)
        {
            BauteilkatalogImport.Satz a = p.Aufbauten.Single();
            return new BauteilaufbauModel
            {
                Bezeichner = "Probewand",
                Bauteilart = (string)a.Werte["Bauteilart"],
                Schichten = a.Schichten.OrderBy(s => (int)s.Werte["Reihenfolge"]).ThenBy(s => (int)s.Abschnitt.Werte["Nummer"])
                                       .Select(s => new BauteilschichtModel
                                       {
                                           Reihenfolge = (int)s.Werte["Reihenfolge"], Dicke = s.W("Dicke").Value, Lambda = s.Lambda,
                                           Rho = s.Rho, Cp = s.Cp, IstLuftschicht = (int)s.Werte["IstLuftschicht"] == 1,
                                           Abschnitt = (int)s.Abschnitt.Werte["Nummer"]
                                       }).ToList(),
                Abschnitte = p.Abschnitte.Where(x => x.Vertreter == a).Select(x => new BauteilabschnittModel
                {
                    Nummer = (int)x.Werte["Nummer"], Flaechenanteil = x.W("Flaechenanteil")
                }).ToList()
            };
        }

        private static void Verworfen(BauteilkatalogImport.Plan p, string grund)
        {
            Assert.Empty(p.Aufbauten);
            Assert.Empty(p.Schichten);
            Assert.Empty(p.Abschnitte);
            Assert.Equal(1, p.Vorschau.Aufbauten.Gruende[grund]);
            Assert.Equal(0, p.Vorschau.Aufbauten.Neu);
            Assert.Equal(0, p.Vorschau.SchichtzahlAngeglichen + p.Vorschau.AnteilErgaenzt);
        }

        // ---- Verschiedene Schichtzahl --------------------------------------------------------

        [Fact]
        public void Verschiedene_Schichtzahl_wird_an_gemeinsamen_Grenzen_geteilt_und_rechnet_nach_6_7()
        {
            // Abschnitt 1 (f 0,8): Dämmung 200 mm, λ 0,04. Abschnitt 2 (f 0,2): 50 mm λ 0,5, dann 150 mm λ 0,15.
            BauteilkatalogImport.Plan p = Planen(80, 20, new Lage(1, 0.2, 0.04), new Lage(2, 0.05, 0.5), new Lage(2, 0.15, 0.15));
            BauteilkatalogVorschau v = p.Vorschau;
            Assert.Equal(1, v.Aufbauten.Neu);
            Assert.Equal(0, v.Aufbauten.Abgelehnt);
            Assert.Equal(1, v.SchichtzahlAngeglichen);
            Assert.Equal(1, v.Teilschichten);
            Assert.Equal(0, v.AnteilErgaenzt);
            Assert.Equal(3, v.Schichten.Neu);                    // Zeilen der Quelle
            Assert.Equal(4, p.Schichten.Count);                  // geschrieben: die Dämmung in 50 + 150 mm geteilt
            Assert.Contains(v.Protokoll, z => z.Contains(G(101)) && z.Contains("3 → 4"));

            BauteilaufbauModel m = Modell(p);
            Assert.Equal(new[] { 1, 1, 2, 2 }, m.Schichten.Select(s => s.Reihenfolge).ToArray());
            Assert.Equal(new int?[] { 1, 2, 1, 2 }, m.Schichten.Select(s => s.Abschnitt).ToArray());
            Assert.Equal(0.05, m.Schichten[0].Dicke, 12);
            Assert.Equal(0.15, m.Schichten[2].Dicke, 12);
            Assert.Null(InhomogeneSchichten.Pruefen(m));

            // Handrechnung nach DIN EN ISO 6946, 6.7: R_si 0,13, R_se 0,04.
            double r11 = 0.05 / 0.04, r12 = 0.15 / 0.04, r21 = 0.05 / 0.5, r22 = 0.15 / 0.15;
            double oben = 1.0 / (0.8 / (0.13 + r11 + r12 + 0.04) + 0.2 / (0.13 + r21 + r22 + 0.04));      // R'_T  = 3,2029
            double unten = 0.13 + 1.0 / (0.8 / r11 + 0.2 / r21) + 1.0 / (0.8 / r12 + 0.2 / r22) + 0.04;    // R''_T = 2,9681
            Assert.Equal(3.2029, oben, 4);
            Assert.Equal(2.9681, unten, 4);
            Assert.Equal(0.0380, (oben - unten) / (oben + unten), 4);                                        // e = 3,8 %

            BauteilaufbauKennwerte k = BauteilaufbauCtrl.Kennwerte(m);
            Assert.True(k.Gerechnet, k.Grund);
            Assert.Equal(oben, k.RT_Oben_M2KW!.Value, 9);
            Assert.Equal(unten, k.RT_Unten_M2KW!.Value, 9);
            Assert.Equal(2.0 / (oben + unten), k.U_WM2K!.Value, 9);
        }

        [Fact]
        public void Die_Gesamtdicke_darf_um_die_Toleranz_abweichen_sonst_wird_benannt_verworfen()
        {
            // 201 mm gegen 200 mm: 1 mm ≤ max(1 mm, 1 % · 201 mm) - geteilt in der genormten Lage, jeder Abschnitt behält seine Dicke.
            BauteilkatalogImport.Plan p = Planen(80, 20, new Lage(1, 0.2, 0.04), new Lage(2, 0.05, 0.5), new Lage(2, 0.151, 0.15));
            Assert.Equal(1, p.Vorschau.SchichtzahlAngeglichen);
            BauteilaufbauModel m = Modell(p);
            Assert.Equal(0.2, m.Schichten.Where(s => s.Abschnitt == 1).Sum(s => s.Dicke), 12);
            Assert.Equal(0.201, m.Schichten.Where(s => s.Abschnitt == 2).Sum(s => s.Dicke), 12);
            Assert.True(BauteilaufbauCtrl.Kennwerte(m).Gerechnet);

            // 210 mm gegen 200 mm: 10 mm > 2,1 mm.
            Verworfen(Planen(80, 20, new Lage(1, 0.2, 0.04), new Lage(2, 0.05, 0.5), new Lage(2, 0.16, 0.15)), R.BKI_GRUND_DICKEN);
        }

        [Fact]
        public void Eine_ruhende_Luftschicht_wird_nicht_geteilt()
        {
            // Die Luftschicht (ohne λ, Widerstand nach Tabelle) läge über einer Grenze des anderen Abschnitts: verworfen.
            Verworfen(Planen(80, 20, new Lage(1, 0.2, null, "Luftschicht"), new Lage(2, 0.05, 0.5), new Lage(2, 0.15, 0.15)),
                      R.BKI_GRUND_LUFTSCHICHT);

            // Fallen ihre Grenzen auf gemeinsame Ebenen, bleibt sie ganz und der Aufbau rechnet.
            BauteilkatalogImport.Plan p = Planen(80, 20, new Lage(1, 0.05, 0.5), new Lage(1, 0.05, 0.04), new Lage(1, 0.1, 0.04),
                                                 new Lage(2, 0.05, null, "Luftschicht"), new Lage(2, 0.15, 0.15));
            Assert.Equal(1, p.Vorschau.SchichtzahlAngeglichen);
            BauteilaufbauModel m = Modell(p);
            Assert.Single(m.Schichten, s => s.IstLuftschicht);
            Assert.Equal(0.05, m.Schichten.Single(s => s.IstLuftschicht).Dicke, 12);
            Assert.Equal(6, m.Schichten.Count);
            Assert.True(BauteilaufbauCtrl.Kennwerte(m).Gerechnet, BauteilaufbauCtrl.Kennwerte(m).Grund);
        }

        [Fact]
        public void Eine_Schicht_ohne_Dicke_neben_verschiedener_Schichtzahl_wird_benannt_verworfen()
        {
            Verworfen(Planen(80, 20, new Lage(1, 0.2, 0.04), new Lage(2, 0.0, 0.5), new Lage(2, 0.2, 0.15)), R.BKI_GRUND_OHNE_DICKE);
        }

        [Fact]
        public void Der_geteilte_Aufbau_rechnet_nach_dem_Schreiben_wie_geplant()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Lage[] lagen = { new Lage(1, 0.2, 0.04), new Lage(2, 0.05, 0.5), new Lage(2, 0.15, 0.15) };
            BauteilkatalogVorschau e = BauteilkatalogImport.Uebernehmen(Ordner(80, null, lagen));
            Assert.Null(e.Fehler);
            Assert.True(e.Geschrieben);
            Assert.Equal((1, 1, 1), (e.SchichtzahlAngeglichen, e.Teilschichten, e.AnteilErgaenzt));
            long id = Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT \"ID\" FROM \"Tab_Bauteilaufbau_STAMM\" WHERE \"Quellkennung\" = ?", new DbParam("@k", G(101))), CultureInfo.InvariantCulture);
            BauteilaufbauModel gelesen = new BauteilaufbauCtrl().LesenKatalogsatz((int)id);
            Assert.Equal(4, gelesen.Schichten.Count);
            Assert.Equal(new double?[] { 0.8, 0.2 }, gelesen.Abschnitte.Select(x => x.Flaechenanteil.HasValue ? Math.Round(x.Flaechenanteil.Value, 12) : (double?)null).ToArray());
            BauteilaufbauKennwerte k = BauteilaufbauCtrl.Kennwerte(gelesen), g = BauteilaufbauCtrl.Kennwerte(Modell(Planen(80, 20, lagen)));
            Assert.True(k.Gerechnet, k.Grund);
            Assert.Equal(g.RT_Oben_M2KW!.Value, k.RT_Oben_M2KW!.Value, 9);
            Assert.Equal(g.RT_Unten_M2KW!.Value, k.RT_Unten_M2KW!.Value, 9);
        }

        // ---- Fehlender Flächenanteil -------------------------------------------------------

        [Fact]
        public void Ein_fehlender_Flaechenanteil_wird_als_Rest_eingesetzt_und_gezaehlt()
        {
            BauteilkatalogImport.Plan p = Planen(80, null, new Lage(1, 0.2, 0.04), new Lage(2, 0.2, 0.13));
            BauteilkatalogVorschau v = p.Vorschau;
            Assert.Equal(1, v.Aufbauten.Neu);
            Assert.Equal(1, v.AnteilErgaenzt);
            Assert.Equal(0, v.SchichtzahlAngeglichen);
            Assert.Equal(0.2, p.Abschnitte.Single(x => (int)x.Werte["Nummer"] == 2).W("Flaechenanteil").Value, 12);
            Assert.Contains(v.Protokoll, z => z.Contains(G(101)) && z.Contains("2: 0"));

            // Rechnet wie derselbe Aufbau mit gepflegten Anteilen 0,8 / 0,2.
            BauteilaufbauKennwerte k = BauteilaufbauCtrl.Kennwerte(Modell(p));
            BauteilaufbauKennwerte g = BauteilaufbauCtrl.Kennwerte(Modell(Planen(80, 20, new Lage(1, 0.2, 0.04), new Lage(2, 0.2, 0.13))));
            Assert.True(k.Gerechnet, k.Grund);
            Assert.Equal(g.U_WM2K!.Value, k.U_WM2K!.Value, 12);

            // Die Hülle nennt die Hinweise in der Vorschau.
            EPOS.UI.Dialoge.Bedarf.BauteilkatalogVorschauDaten d = BauteilkatalogImportHuelle.AlsDaten(v);
            Assert.False(string.IsNullOrEmpty(d.Inhomogen));
            Assert.Null(BauteilkatalogImportHuelle.AlsDaten(Planen(80, 20, new Lage(1, 0.2, 0.04), new Lage(2, 0.2, 0.13)).Vorschau).Inhomogen);
        }

        [Fact]
        public void Ohne_bestimmbaren_Rest_bleibt_der_fehlende_Flaechenanteil_eine_Ablehnung()
        {
            const string GRUND = "inhomogen: Abschnitt ohne Flächenanteil";
            Verworfen(Planen(null, null, new Lage(1, 0.2, 0.04), new Lage(2, 0.2, 0.13)), GRUND);    // zwei fehlen
            Verworfen(Planen(100, null, new Lage(1, 0.2, 0.04), new Lage(2, 0.2, 0.13)), GRUND);    // nichts offen
            Verworfen(Planen(99.5, 0, new Lage(1, 0.2, 0.04), new Lage(2, 0.2, 0.13)), GRUND);      // nur die Toleranz offen
            // Ein gesetzter Anteil über 1 bleibt die Summenablehnung.
            Verworfen(Planen(80, 30, new Lage(1, 0.2, 0.04), new Lage(2, 0.2, 0.13)), "inhomogen: Flächenanteile ergeben nicht 1 ± 0,01");
        }

        // ---- Gleiche Schichtzahl ----------------------------------------------------------

        [Fact]
        public void Aufbauten_mit_gleicher_Schichtzahl_bleiben_unveraendert()
        {
            // Auch mit versetzten Grenzen (100 + 100 mm neben 50 + 150 mm) bildet die k-te Schicht jedes Abschnitts die Ebene k.
            BauteilkatalogImport.Plan p = Planen(80, 20, new Lage(1, 0.1, 0.04), new Lage(1, 0.1, 0.035),
                                                 new Lage(2, 0.05, 0.5), new Lage(2, 0.15, 0.13));
            BauteilkatalogVorschau v = p.Vorschau;
            Assert.Equal(0, v.SchichtzahlAngeglichen + v.Teilschichten + v.AnteilErgaenzt);
            Assert.Equal(4, p.Schichten.Count);
            BauteilaufbauModel m = Modell(p);
            Assert.Equal(new[] { 0.1, 0.05, 0.1, 0.15 }, m.Schichten.Select(s => s.Dicke).ToArray());
            Assert.Equal(new[] { 1, 1, 2, 2 }, m.Schichten.Select(s => s.Reihenfolge).ToArray());

            // Die Grenzwerte folgen der bisherigen Ebenenbildung (Schicht k neben Schicht k).
            double oben = 1.0 / (0.8 / (0.17 + 0.1 / 0.04 + 0.1 / 0.035) + 0.2 / (0.17 + 0.05 / 0.5 + 0.15 / 0.13));
            double unten = 0.17 + 1.0 / (0.8 / (0.1 / 0.04) + 0.2 / (0.05 / 0.5)) + 1.0 / (0.8 / (0.1 / 0.035) + 0.2 / (0.15 / 0.13));
            BauteilaufbauKennwerte k = BauteilaufbauCtrl.Kennwerte(m);
            Assert.Equal(oben, k.RT_Oben_M2KW!.Value, 9);
            Assert.Equal(unten, k.RT_Unten_M2KW!.Value, 9);
        }
    }
}

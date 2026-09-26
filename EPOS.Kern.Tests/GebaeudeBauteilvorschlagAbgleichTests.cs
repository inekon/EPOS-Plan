using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Namensabgleich im Bauteilvorschlag</b> (Stufe G4b, Ergänzung Welle 1; Mehrzonenkonzept 3.5 und
    /// 6.3, Datenaustauschkonzept 3.6) — ohne Datenbank, gegen die Auslieferungssaat
    /// (<see cref="BaustoffabgleichDaten.AusSaat"/>): das Probenhaus mit Materialnamen der Autorensysteme
    /// und Nullwerten, die Nullwertprobe, die Gegenprobe des gbXML-Wegs, Band, Luftschicht und Schraffur,
    /// der Vorrang der gemerkten Zuordnung und die Materialliste für die Anwenderzuordnung.
    /// </summary>
    public class GebaeudeBauteilvorschlagAbgleichTests : IDisposable
    {
        private const string MATERIALHAUS = "ifc4_haus_materialnamen.ifc";

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public GebaeudeBauteilvorschlagAbgleichTests(ITestOutputHelper aus)
        {
            _aus = aus;
        }

        public void Dispose() => _kultur.Dispose();

        internal static Baustoffabgleich Saat(params BaustoffNamenzuordnung[] zuordnungen)
            => new Baustoffabgleich(BaustoffabgleichDaten.AusSaat(zuordnungen));

        internal static GebaeudeBauteilvorschlag Mit(string probe, Baustoffabgleich abgleich = null)
            => GebaeudeBauteilvorschlag.Bilden(BauteilvorschlagProbe.Lesen(probe), 0, null, null, abgleich ?? Saat());

        private static GebaeudeAufbauzeile AufbauDerZeile(GebaeudeBauteilvorschlag v, string name)
        {
            GebaeudeBauteilzeile z = v.Zeilen.First(x => x.Bauteil.Bezeichner == name);
            Assert.True(z.Bauteil.ID_Aufbau.HasValue, name + " trägt keinen Aufbau.");
            return v.Aufbauten.Single(a => a.Aufbau.ID == z.Bauteil.ID_Aufbau.Value);
        }

        private static int?[] Stamm(GebaeudeAufbauzeile a) => a.Stammbaustoffe.ToArray();

        private static void FolgeOderUmkehrung(int?[] erwartet, int?[] ist)
            => Assert.True(erwartet.SequenceEqual(ist) || erwartet.Reverse().SequenceEqual(ist),
                           "erwartet " + string.Join(",", erwartet) + " (oder umgekehrt), ist " + string.Join(",", ist));

        // =====================================================================
        //  Das Probenhaus mit Materialnamen und Nullwerten
        // =====================================================================

        /// <summary>
        /// Ohne Abgleich trägt das Probenhaus (Stoffwerte voller Nullen, wie die gemessenen Dateien) keinen
        /// Aufbau; mit dem Abgleich sechs, alle mit Herkunft <c>KATALOG</c>, und die innere Masse kommt aus
        /// den Innenbauteilen statt aus dem Faktor. Die Kellerdecke bleibt ohne Aufbau: „Fußbodenaufbau"
        /// trifft nichts (Tabelle 3.5).
        /// </summary>
        [Fact]
        public void Das_Probenhaus_mit_Materialnamen_bekommt_seine_Aufbauten_aus_dem_Katalog()
        {
            GebaeudeBauteilvorschlag ohne = BauteilvorschlagProbe.Vorschlag(MATERIALHAUS);
            Assert.False(ohne.Abgelehnt);
            Assert.False(ohne.AbgleichAktiv);
            Assert.Empty(ohne.Aufbauten);
            Assert.Empty(ohne.Materialien);
            Assert.Equal(Innenweg.Innenflaechenfaktor, ohne.Innenweg);

            GebaeudeBauteilvorschlag v = Mit(MATERIALHAUS);
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Select(m => m.ToString())));
            Assert.True(v.AbgleichAktiv);
            Assert.Equal(6, v.Aufbauten.Count);
            Assert.All(v.Aufbauten, a =>
            {
                Assert.Equal(Importherkunft.Katalog, a.Herkunft);
                Assert.Equal(DbWerte.HERKUNFT_KATALOG, a.Aufbau.Herkunft);
                Assert.All(a.Aufbau.Schichten, s => Assert.Null(s.ID_Baustoff));     // die Projektkopie setzt erst der Schreibweg
            });
            Assert.Equal(Innenweg.Bauteile, v.Innenweg);
            Assert.Null(v.Innenflaechenfaktor);

            // Außenwand EG (deutsche Vorlage): Kalkzementputz, Mineralwolle λD 0,035, Kalksandstein 1800, Gipsputz 1200.
            GebaeudeAufbauzeile eg = AufbauDerZeile(v, "EG Süd");
            FolgeOderUmkehrung(new int?[] { 1, 36, 20, 2 }, Stamm(eg));
            BauteilschichtModel ks = eg.Aufbau.Schichten.Single(s => Math.Abs(s.Dicke - 0.175) < 1e-9);
            Assert.Equal((0.99, 1800.0, 1000.0), (ks.Lambda.Value, ks.Rho.Value, ks.Cp.Value));
            BauteilschichtModel mw = eg.Aufbau.Schichten.Single(s => Math.Abs(s.Dicke - 0.14) < 1e-9);
            Assert.Equal((0.036, 40.0, 1030.0), (mw.Lambda.Value, mw.Rho.Value, mw.Cp.Value));
            Assert.Equal(new[] { "Gipsputz", "Kalksandstein 2816491304", "Mineralwolle 102890377", "Putz, Kalk-Zement" },
                         eg.Baustoffquellen.Select(q => q.Kennung).OrderBy(k => k, StringComparer.Ordinal));
            Assert.All(eg.Baustoffquellen, q => Assert.Equal(GebaeudeBauteilvorschlag.QUELLTYP_IFC_BAUSTOFF, q.Quelltyp));

            // Außenwand OG (englische Vorlage) mit einer ruhenden Luftschicht: ohne λ, ρ, c und ohne Baustoff.
            GebaeudeAufbauzeile og = AufbauDerZeile(v, "OG Nord");
            FolgeOderUmkehrung(new int?[] { 1, 39, null, 13, 48 }, Stamm(og));
            BauteilschichtModel luft = Assert.Single(og.Aufbau.Schichten, s => s.IstLuftschicht);
            BauteilvorschlagProbe.Nah(0.04, luft.Dicke);
            Assert.Null(luft.Lambda);
            Assert.Null(luft.Rho);
            Assert.Null(luft.Cp);

            // Das Dach ohne die Schraffur; Stahlbeton genau (N3), EPS über das Synonym, die Bahn über das Synonym.
            GebaeudeAufbauzeile dach = AufbauDerZeile(v, "Dach");
            FolgeOderUmkehrung(new int?[] { 56, 39, 10 }, Stamm(dach));

            // Die Kellerdecke bleibt beim U-Wert der Datei.
            GebaeudeBauteilzeile kd = v.Zeilen.Single(z => z.Bauteil.Bezeichner == "Kellerdecke");
            Assert.Null(kd.Bauteil.ID_Aufbau);
            Assert.Equal(0.35, kd.Bauteil.U_Wert);
            Assert.Equal(Importherkunft.Ifc, kd.HerkunftU);

            // Die Materialliste der Datei — der Eingang der Anwenderzuordnung.
            Assert.Equal(20, v.Materialien.Count);
            Assert.Equal(new[] { "Fußbodenaufbau" }, v.OhneTreffer.Select(m => m.Name));
            GebaeudeMaterialzeile gips = v.Materialien.Single(m => m.Name == "Gipsputz");
            Assert.Equal(Abgleichstufe.Teilwort, gips.Treffer.Stufe);
            Assert.Equal(2, gips.Treffer.Baustoff.ID);
            Assert.Equal(4, gips.Schichten);
            Assert.Equal(4, gips.SchichtenAusKatalog);
            Assert.Equal("gipsputz", gips.Normiert);
            Assert.Equal(Abgleichsonderfall.Luftschicht, v.Materialien.Single(m => m.Name == "Air").Treffer.Sonderfall);
            Assert.Equal(0, v.Materialien.Single(m => m.Name == "Ortbeton - bewehrt").SchichtenAusKatalog);   // nur in der Kellerdecke

            // Die Meldungen des Abgleichs.
            PruefMeldung ab = Assert.Single(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.ABGLEICH);
            Assert.Equal(PruefStufe.Info, ab.Stufe);
            Assert.Equal(new[] { "39", "6", "16", "20" }, ab.Werte);
            PruefMeldung unbekannt = Assert.Single(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.BAUSTOFF_UNBEKANNT);
            Assert.Equal(PruefStufe.Warnung, unbekannt.Stufe);
            Assert.Equal(new[] { "1", "Fußbodenaufbau" }, unbekannt.Werte);
            PruefMeldung weg = Assert.Single(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.SCHICHT_VERWORFEN);
            Assert.Equal(new[] { "2", "Radial Gradient Fill 1515460218, Solid 397409098" }, weg.Werte);
            Assert.Equal(new[] { "4" }, Assert.Single(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.LUFTSCHICHT).Werte);
            PruefMeldung stoff = Assert.Single(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.STOFFWERTE_UNVOLLSTAENDIG);
            Assert.Equal("Fußbodenaufbau", stoff.Werte[1]);
            Assert.DoesNotContain(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.STOFFWERT_UNGUELTIG);   // Nullen sind Fehlstellen, nicht ungültig

            // Jede Meldung hat ihren Text in der Anzeigekultur.
            foreach (PruefMeldung m in v.Meldungen)
                Assert.DoesNotContain("IMP_", GebaeudeZuordnungsModell.MeldungText(m), StringComparison.Ordinal);
            Assert.DoesNotContain("GIMP_", GebaeudeZuordnungsModell.BelegText(gips.Treffer.Beleg), StringComparison.Ordinal);
        }

        /// <summary>
        /// Die gemerkte Zuordnung schließt die Lücke: „Fußbodenaufbau" → Zementestrich (N7), und die
        /// Kellerdecke trägt ihren Aufbau; es bleibt kein Name ohne Treffer.
        /// </summary>
        [Fact]
        public void Die_gemerkte_Zuordnung_vervollstaendigt_die_Kellerdecke()
        {
            GebaeudeBauteilvorschlag v = Mit(MATERIALHAUS, Saat(new BaustoffNamenzuordnung("fussbodenaufbau", 5)));
            Assert.False(v.Abgelehnt);
            Assert.Equal(7, v.Aufbauten.Count);
            FolgeOderUmkehrung(new int?[] { 5, 10, 36 }, Stamm(AufbauDerZeile(v, "Kellerdecke")));
            Assert.Empty(v.OhneTreffer);
            Assert.Equal(Abgleichstufe.Anwender, v.Materialien.Single(m => m.Name == "Fußbodenaufbau").Treffer.Stufe);
            Assert.DoesNotContain(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.BAUSTOFF_UNBEKANNT);
        }

        /// <summary>
        /// Die Nullwertprobe: Die Dämmung trägt λ = 0,04 und ρ = c = 0. λ im Band bleibt der Wert der Datei,
        /// ρ und c kommen aus dem getroffenen Baustoff; ohne Abgleich entsteht kein Aufbau.
        /// </summary>
        [Fact]
        public void Die_Nullwertprobe_nimmt_die_fehlenden_Werte_aus_dem_Katalog_und_behaelt_lambda_der_Datei()
        {
            Assert.Empty(BauteilvorschlagProbe.Vorschlag("ifc4_schichten_nullwerte.ifc").Aufbauten);
            GebaeudeBauteilvorschlag v = Mit("ifc4_schichten_nullwerte.ifc");
            Assert.False(v.Abgelehnt);
            Assert.Equal(4, v.Aufbauten.Count);
            foreach (GebaeudeAufbauzeile a in v.Aufbauten)
            {
                Assert.Equal(Importherkunft.Katalog, a.Herkunft);
                for (int i = 0; i < a.Aufbau.Schichten.Count; i++)
                {
                    BauteilschichtModel s = a.Aufbau.Schichten[i];
                    if (a.Stammbaustoffe[i] == 36)
                        Assert.Equal((0.04, 40.0, 1030.0), (s.Lambda.Value, s.Rho.Value, s.Cp.Value));
                    else
                        Assert.Null(a.Stammbaustoffe[i]);                                   // Putz, Mauerwerk, Beton: Werte der Datei
                }
            }
            Assert.All(v.Aufbauten, a => Assert.Contains(36, a.Stammbaustoffe.Where(x => x.HasValue).Select(x => x.Value)));
        }

        // =====================================================================
        //  Die Gegenprobe des gbXML-Wegs
        // =====================================================================

        /// <summary>
        /// Vollständige Stoffwerte der Datei rechnen; der Abgleich ist die Gegenprobe. Beim Probenhaus weicht
        /// allein „Innenputz" (λ 1,0 gegen Gipsputz 0,43) um mehr als 50 % ab; „Estrich" (0,75 gegen 1,4,
        /// −46 %) liegt in der Streuung. Die Aufbauten sind dieselben wie ohne Abgleich.
        /// </summary>
        [Fact]
        public void Bei_gbXML_ist_der_Abgleich_die_Gegenprobe()
        {
            GebaeudeBauteilvorschlag ohne = BauteilvorschlagProbe.Vorschlag("gbxml_haus_si.xml");
            GebaeudeBauteilvorschlag v = Mit("gbxml_haus_si.xml");
            Assert.Equal(ohne.Aufbauten.Count, v.Aufbauten.Count);
            for (int j = 0; j < v.Aufbauten.Count; j++)
            {
                Assert.Equal(Importherkunft.GbXml, v.Aufbauten[j].Herkunft);
                Assert.Equal(ohne.Aufbauten[j].Aufbau.Schichten.Select(s => (s.Dicke, s.Lambda, s.Rho, s.Cp)),
                             v.Aufbauten[j].Aufbau.Schichten.Select(s => (s.Dicke, s.Lambda, s.Rho, s.Cp)));
                Assert.All(v.Aufbauten[j].Stammbaustoffe, x => Assert.Null(x));
                Assert.Empty(v.Aufbauten[j].Baustoffquellen);
            }
            PruefMeldung g = Assert.Single(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.LAMBDA_GEGENPROBE);
            Assert.Equal(PruefStufe.Warnung, g.Stufe);
            Assert.Equal(new[] { "Innenputz", "1", "Gipsputz 1200", "0.43", "132.6" }, g.Werte);
            Assert.DoesNotContain(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.ABGLEICH);
            Assert.All(v.Materialien, m => Assert.Equal(m.Schichten, m.SchichtenAusDatei));
            Assert.Empty(v.OhneTreffer);                                     // „Mauerwerk 24 cm" braucht keinen Baustoff
            Assert.Equal(0.5, GebaeudeBauteilvorschlag.LAMBDA_GEGENPROBE_GRENZE);
        }

        // =====================================================================
        //  Band, R-Wert, Luftschicht, Schraffur — an einem synthetischen Abbild
        // =====================================================================

        /// <summary>
        /// Eine Wand mit vier Schichten: Beton mit λ = 1 000 (außerhalb des Bands: gilt als nicht geliefert,
        /// λ aus dem Katalog, ρ und c der Datei bleiben), eine Luftschicht ohne Werte (ruhende Luftschicht),
        /// eine Schraffur (verworfen) und ein Ziegel nur mit R-Wert (λ = d/R der Datei, ρ und c aus dem
        /// Katalog). Ohne Abgleich bleibt es beim Rückfall.
        /// </summary>
        [Fact]
        public void Band_R_Wert_Luftschicht_und_Schraffur()
        {
            GbxmlAbbild a = BauteilvorschlagProbe.Synthetisch();
            var aufbau = new AbbildAufbau { Kennung = "kon-1", Name = "Probewand", Status = Aufbaustatus.Unvollstaendig };
            aufbau.Schichten.Add(new AbbildSchicht { BaustoffKennung = "m-beton", Name = "Beton", DickeM = 0.2, LambdaWmK = 1000, RhoKgM3 = 2300, CpJkgK = 900 });
            aufbau.Schichten.Add(new AbbildSchicht { BaustoffKennung = "m-luft", Name = "Luftschicht", DickeM = 0.04 });
            aufbau.Schichten.Add(new AbbildSchicht { BaustoffKennung = "m-solid", Name = "Solid 123456789", DickeM = 0.001 });
            aufbau.Schichten.Add(new AbbildSchicht { BaustoffKennung = "m-ziegel", Name = "Ziegel", DickeM = 0.1, RWertM2KW = 0.2 });
            AbbildBauteil wand = BauteilvorschlagProbe.Flaeche("aw", Bauteilart.Aussenwand, Randbedingung.Aussenluft, 30, 0.5, 90, 180, "R1");
            wand.Aufbau = aufbau;
            a.Gebaeude[0].Bauteile.Add(wand);

            GebaeudeBauteilvorschlag ohne = BauteilvorschlagProbe.Bilden(a);
            Assert.Empty(ohne.Aufbauten);

            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, 0, null, null, new GbxmlImportProfil(), null, Saat());
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Select(m => m.ToString())));
            GebaeudeAufbauzeile z = Assert.Single(v.Aufbauten);
            Assert.Equal(Importherkunft.Katalog, z.Herkunft);
            // Innen → außen (gbXML: erste Schicht außen): Ziegel, Luft, Beton — die Schraffur fällt weg.
            Assert.Equal(new int?[] { 13, null, 9 }, Stamm(z));
            BauteilschichtModel ziegel = z.Aufbau.Schichten[0], luft = z.Aufbau.Schichten[1], beton = z.Aufbau.Schichten[2];
            Assert.Equal((0.1, 0.5, 1800.0, 1000.0), (ziegel.Dicke, ziegel.Lambda.Value, ziegel.Rho.Value, ziegel.Cp.Value));
            Assert.True(luft.IstLuftschicht);
            Assert.Null(luft.Lambda);
            Assert.Equal((0.2, 2.0, 2300.0, 900.0), (beton.Dicke, beton.Lambda.Value, beton.Rho.Value, beton.Cp.Value));
            Assert.Equal(new[] { "m-beton", "m-ziegel" }, z.Baustoffquellen.Select(q => q.Kennung).OrderBy(k => k, StringComparer.Ordinal));
            Assert.All(z.Baustoffquellen, q => Assert.Equal(GebaeudeBauteilvorschlag.QUELLTYP_GBXML_BAUSTOFF, q.Quelltyp));

            Assert.Equal(new[] { "1", "Beton" }, Assert.Single(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.STOFFWERT_UNGUELTIG).Werte);
            Assert.Equal(new[] { "1", "Solid 123456789" }, Assert.Single(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.SCHICHT_VERWORFEN).Werte);
            Assert.Equal(new[] { "1" }, Assert.Single(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.LUFTSCHICHT).Werte);
            // Der U-Wert der Wand kommt aus den Schichten (E45/2) — samt der ruhenden Luftschicht nach Tabelle 8.
            GebaeudeBauteilzeile w = v.Zeilen.Single(x => x.Kennung == "aw");
            Assert.Null(w.Bauteil.U_Wert);
            Assert.Equal(Importherkunft.Katalog, w.HerkunftU);
            double rLuft = Bauteilreduktion.Luftschichtwiderstand(0.04, Waermestromrichtung.Horizontal);
            BauteilvorschlagProbe.Nah(1.0 / (0.13 + 0.1 / 0.5 + rLuft + 0.2 / 2.0 + 0.04), w.USchichten, 1e-12);
        }

        // =====================================================================
        //  Auskunft: Aufbauten je Probe ohne und mit Abgleich
        // =====================================================================

        /// <summary>
        /// Zur Auskunft, nicht als Abnahme: je Importprobe die Zahl der Aufbauten ohne und mit dem Abgleich
        /// gegen die Auslieferungssaat, die Materialnamen samt Stufe und die Meldungen.
        /// </summary>
        [Theory]
        [InlineData("ifc4_haus.ifc")]
        [InlineData(MATERIALHAUS)]
        [InlineData("ifc4_schichten.ifc")]
        [InlineData("ifc4_schichten_nullwerte.ifc")]
        [InlineData("ifc2x3_schichten.ifc")]
        [InlineData("ifc4_vorhangfassade.ifc")]
        [InlineData("ifc4_rueckfaelle.ifc")]
        [InlineData("gbxml_haus_si.xml")]
        [InlineData("gbxml_rwert_schicht.xml")]
        [InlineData("gbxml_innenflaechen_teilweise.xml")]
        public void Auskunft_Aufbauten_ohne_und_mit_Abgleich(string probe)
        {
            GebaeudeBauteilvorschlag ohne = BauteilvorschlagProbe.Vorschlag(probe);
            GebaeudeBauteilvorschlag mit = Mit(probe);
            int namen = mit.Materialien.Count(m => m.BrauchtAbgleich);
            int getroffen = mit.Materialien.Count(m => m.BrauchtAbgleich && m.Treffer != null && m.Treffer.Stufe != Abgleichstufe.Keine);
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "{0}: Aufbauten ohne Abgleich {1}, mit {2} (davon KATALOG {3}); Namen {4}, ohne Werte der Datei {5}, davon getroffen {6}; Innenweg {7} → {8}",
                probe, ohne.Aufbauten.Count, mit.Aufbauten.Count, mit.Aufbauten.Count(a => a.Herkunft == Importherkunft.Katalog),
                mit.Materialien.Count, namen, getroffen, ohne.Innenweg, mit.Innenweg));
            foreach (GebaeudeMaterialzeile m in mit.Materialien)
                _aus.WriteLine("  " + m + " · Datei " + m.SchichtenAusDatei + " · Katalog " + m.SchichtenAusKatalog);
            Assert.True(mit.Aufbauten.Count >= ohne.Aufbauten.Count);
        }
    }
}

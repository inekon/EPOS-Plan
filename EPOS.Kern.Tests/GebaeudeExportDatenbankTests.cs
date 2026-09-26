using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G7a, Welle 2 — der Rundlauf über die Datenbank</b> (Umsetzungsauftrag G7a, W2, Proben 1b,
    /// 1c und 11): Das Gebäude wird <b>im Test aufgebaut, nicht gesät</b> (benannte Abweichung von
    /// Datenaustauschkonzept 9, Probe 1) — Projekt 1039, Gebäude 10643, eine Arbeitskopie der
    /// Testdatenbank. Exportiert wird über den Leseweg der Hülle (<see cref="GebaeudeExportSatz.Lesen"/>),
    /// importiert über <see cref="GebaeudeImportAblauf"/> und den Bauteilvorschlag samt Namensabgleich des
    /// Projekts; verglichen werden die wirksamen Werte Zeile gegen Zeile.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class GebaeudeExportDatenbankTests : IDisposable
    {
        private const int PROJEKT = 1039;
        private const int GEBAEUDE = 10643;
        private const int OHNE_ZONE = 10642;
        private const double Genau = 1e-6;

        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;

        public GebaeudeExportDatenbankTests(ITestOutputHelper ausgabe)
        {
            _ausgabe = ausgabe;
        }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        // ==================================================================
        //  Die Vorrichtung
        // ==================================================================

        private static int Aufbau(string name, params BauteilschichtModel[] schichten)
        {
            var m = new BauteilaufbauModel { Bezeichner = name };
            for (int i = 0; i < schichten.Length; i++)
            {
                schichten[i].Reihenfolge = i + 1;
                m.Schichten.Add(schichten[i]);
            }
            BauteilaufbauCtrl.Ergebnis e = new BauteilaufbauCtrl().ProjektSpeichern(PROJEKT, m);
            Assert.True(e.Ok, e.Meldung);
            return e.Id;
        }

        private static BauteilschichtModel S(double d, double? l, double? r, double? c, bool luft = false)
            => new BauteilschichtModel { Dicke = d, Lambda = l, Rho = r, Cp = c, IstLuftschicht = luft };

        /// <summary>Die Außenwand mit Katalogstoff und ruhender Luftschicht als Projektkopie (samt Projektstoff).</summary>
        private static int Wandaufbau()
        {
            var ctrl = new BauteilaufbauCtrl();
            var wand = new BauteilaufbauModel
            {
                Bezeichner = "Wand G7a Innendämmung", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                Schichten =
                {
                    new BauteilschichtModel { Reihenfolge = 1, Dicke = 0.0125, Lambda = 0.25, Rho = 900.0, Cp = 1000.0 },
                    new BauteilschichtModel { Reihenfolge = 2, Dicke = 0.06, Lambda = 0.035, Rho = 30.0, Cp = 1030.0 },
                    new BauteilschichtModel { Reihenfolge = 3, Dicke = 0.02, IstLuftschicht = true },
                    new BauteilschichtModel { Reihenfolge = 4, ID_Baustoff = 18, Dicke = 0.175 },
                    new BauteilschichtModel { Reihenfolge = 5, Dicke = 0.015, Lambda = 0.87, Rho = 1800.0, Cp = 1000.0 },
                }
            };
            int stamm = ctrl.KatalogSpeichern(wand).Id;
            int kopie = ctrl.CopyFromStamm(stamm, PROJEKT);
            Assert.True(kopie > 0);
            return kopie;
        }

        /// <summary>
        /// Legt die Zone des Gebäudes 10643 an: Wand mit Innendämmung (ruhende Luftschicht) samt Fenster,
        /// Außenwände, Dach 30°, Erdreich-Bodenplatte, Boden UND Wand gegen unbeheizt, Innenpaar mit
        /// gespiegeltem Aufbau, Innendecke ohne Partner, eine äquivalente Luftschicht ohne Rohdichte.
        /// </summary>
        private static void Anlegen()
        {
            int wand = Wandaufbau();
            int decke = Aufbau("Decke G7a", S(0.01, 0.7, 1400.0, 1000.0), S(0.18, 2.3, 2400.0, 1000.0), S(0.01, 0.7, 1400.0, 1000.0));
            int innen = Aufbau("Innenwand G7a", S(0.115, 0.99, 1800.0, 1000.0), S(0.04, 0.04, 30.0, 1400.0));
            int gespiegelt = Aufbau("Innenwand G7a (Gegenseite)", S(0.04, 0.04, 30.0, 1400.0), S(0.115, 0.99, 1800.0, 1000.0));
            int dach = Aufbau("Dach G7a", S(0.02, 0.13, 500.0, 1600.0), S(0.03, 0.18, null, null, luft: true), S(0.2, 0.035, 30.0, 1400.0),
                              S(0.0125, 0.25, 900.0, 1000.0));

            BauteilModel B(int id, string name, string art, double a, string rand, int? aufbau = null, double? u = null,
                           double? neigung = null, double? azimut = null)
                => new BauteilModel { ID = id, Bezeichner = name, Bauteilart = art, Flaeche = a, Randbedingung = rand, ID_Aufbau = aufbau,
                                      U_Wert = u, Neigung = neigung, Azimut = azimut };
            var zone = new ZoneModel
            {
                ID = -1, Bezeichner = "Wohnen G7a", Nutzflaeche = 130.0, IstBeheizt = true, Luftwechsel_Nutzer = 0.4,
                Bauteile =
                {
                    B(-1, "Wand Süd", DbWerte.BAUTEILART_AUSSENWAND, 22.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, wand, neigung: 90.0, azimut: 180.0),
                    B(-2, "Fenster Süd", DbWerte.BAUTEILART_FENSTER, 6.0, null, u: 1.1, neigung: 90.0, azimut: 180.0),
                    B(-3, "Wand Nord", DbWerte.BAUTEILART_AUSSENWAND, 28.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, wand, neigung: 90.0, azimut: 0.0),
                    B(-4, "Wand Ost", DbWerte.BAUTEILART_AUSSENWAND, 20.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, wand, neigung: 90.0, azimut: 90.0),
                    B(-5, "Dach 30°", DbWerte.BAUTEILART_DACH, 75.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, dach, neigung: 30.0, azimut: 180.0),
                    B(-6, "Bodenplatte", DbWerte.BAUTEILART_BODENPLATTE, 65.0, DbWerte.RANDBEDINGUNG_ERDREICH, decke, neigung: 180.0),
                    B(-7, "Kellerdecke", DbWerte.BAUTEILART_BODENPLATTE, 65.0, DbWerte.RANDBEDINGUNG_UNBEHEIZT, decke, neigung: 180.0),
                    B(-8, "Wand Garage", DbWerte.BAUTEILART_AUSSENWAND, 12.0, DbWerte.RANDBEDINGUNG_UNBEHEIZT, wand, neigung: 90.0, azimut: 270.0),
                    B(-9, "Innenwand A", DbWerte.BAUTEILART_INNENWAND, 45.0, null, innen, neigung: 90.0),
                    B(-10, "Innenwand B", DbWerte.BAUTEILART_INNENWAND, 45.0, null, gespiegelt, neigung: 90.0),
                    B(-11, "Geschossdecke", DbWerte.BAUTEILART_DECKE, 80.0, null, decke, neigung: 0.0),
                },
            };
            zone.Bauteile[1].Rahmenanteil = 0.3;
            zone.Bauteile[0].Psi_L = 1.5;
            GebaeudeZonenCtrl.Ergebnis e = new GebaeudeZonenCtrl().SpeichernJeGebaeude(GEBAEUDE, new List<ZoneModel> { zone });
            Assert.True(e.Ok, e.Meldung);
        }

        private static byte[] Exportieren(int idProjekt, int idZ, string plz, out GebaeudeExportSatz satz, out GebaeudeExportPlan plan)
        {
            satz = GebaeudeExportSatz.Lesen(idProjekt, idZ, plz);
            plan = new GebaeudeExportAblauf().Vorbereiten(satz, GbxmlExportProbe.Profil());
            Assert.False(plan.Abgelehnt, plan.Ablehnung?.ToString());
            return ExportSatzProbe.Datei(plan);
        }

        private static GebaeudeBauteilvorschlag Importieren(byte[] datei, out GebaeudeImportAblauf ablauf)
        {
            ablauf = new GebaeudeImportAblauf();
            ablauf.Lesen(new MemoryStream(datei), "export.xml", new GbxmlImportProfil());
            Assert.NotNull(ablauf.Abbild);
            return GebaeudeBauteilvorschlag.Bilden(ablauf, 0, null, null, new BaustoffabgleichCtrl(PROJEKT).Abgleich());
        }

        // ==================================================================
        //  Probe 1b — der Datenbank-Rundlauf
        // ==================================================================

        [Fact]
        public void Probe1b_Export_und_Rueckimport_Zeile_gegen_Zeile()
        {
            if (!_db.Vorhanden) return;
            Anlegen();
            byte[] datei = Exportieren(PROJEKT, GEBAEUDE, "01067", out GebaeudeExportSatz satz, out GebaeudeExportPlan plan);
            Assert.False(satz.Klassenweg);
            GebaeudeBauteilvorschlag v = Importieren(datei, out GebaeudeImportAblauf ablauf);

            Assert.DoesNotContain(v.Meldungen, m => m.Schluessel == "IMP_BAUTEIL_PROT_U_ABWEICHUNG");
            Assert.Equal(130.0, v.Zone.Nutzflaeche);
            Assert.Contains(plan.Meldungen, m => m.Schluessel == GebaeudeExportAblauf.LUFTSCHICHT_ANGEHOBEN);

            ZoneModel quelle = satz.Zonen[0];
            var paar = new Dictionary<string, string>();   // Kennung der Innenwand B → die der Innenwand A
            int idA = quelle.Bauteile.Single(b => b.Bezeichner == "Innenwand A").ID, idB = quelle.Bauteile.Single(b => b.Bezeichner == "Innenwand B").ID;
            int vorn = Math.Min(idA, idB);
            foreach (BauteilModel b in quelle.Bauteile)
            {
                bool oeffnung = b.Bauteilart == DbWerte.BAUTEILART_FENSTER;
                bool partner = (b.ID == idA || b.ID == idB) && b.ID != vorn;
                string kennung = partner ? "epos-bauteil-" + vorn.ToString(CultureInfo.InvariantCulture)
                               : (oeffnung ? "epos-oeffnung-" : "epos-bauteil-") + b.ID.ToString(CultureInfo.InvariantCulture);
                List<GebaeudeBauteilzeile> rueck = v.Zeilen.Where(z => z.Kennung == kennung).ToList();
                Assert.True(rueck.Count > 0, "keine Zeile zu " + b.Bezeichner);

                Bauteilart art = GebaeudeZonenabbildung.ArtAusZeile(b.Bauteilart).Value;
                Bauteilrand rand = GebaeudeZonenabbildung.RandAusZeile(art, b.Randbedingung).Value;
                double neigung = b.Neigung ?? BauteilEingang.VorgabeNeigung(art);
                Umkehrzelle zelle = GbxmlUmkehrung.Zelle(art, b.Randbedingung, neigung);
                bool innen = rand == Bauteilrand.Innen;
                GebaeudeBauteilzeile z = innen ? rueck.Single(x => x.Seite == (partner ? "B" : "A")) : Assert.Single(rueck);
                double flaeche = b.Bezeichner == "Geschossdecke" ? b.Flaeche / 2.0 : b.Flaeche;

                string wo = b.Bezeichner;
                Assert.Equal(GebaeudeZonenabbildung.ArtFuerZeile(zelle.RueckArt.Value), z.Bauteil.Bauteilart);
                Assert.Equal(zelle.RueckRand, GebaeudeZonenabbildung.RandAusZeile(z.Bauteil.Bauteilart, z.Bauteil.Randbedingung));
                Nah(flaeche, z.Bauteil.Flaeche, wo + " Fläche");
                Nah(innen && partner ? 180.0 - neigung : neigung, z.Bauteil.Neigung, wo + " Neigung");
                if (b.Azimut.HasValue) Nah(b.Azimut.Value, z.Bauteil.Azimut, wo + " Azimut");
                double uQuelle = U(b, rand, neigung, satz.Aufbauten);
                double uRueck = U(z.Bauteil, innen ? Bauteilrand.Innen : zelle.RueckRand.Value, neigung, v.AufbautenJeId);
                Nah(uQuelle, uRueck, wo + " U");

                // Benannte Verluste, die das Probegebäude trägt, kommen nicht zurück.
                if (b.Psi_L.HasValue) Assert.Null(z.Bauteil.Psi_L);
                if (b.Rahmenanteil.HasValue) Assert.Null(z.Bauteil.Rahmenanteil);
            }
            Assert.Null(v.Zone.Luftwechsel_Nutzer);   // benannter Verlust (Zone)

            // Gebäudewerte über die Zuordnung: Infiltration, Tagessollwert, Fläche je Nutzer (wirksam).
            ProjektGebaeudeModel g = satz.Gebaeude;
            GebaeudeImportSatz s = v.Satz;
            Nah(g.Luftwechsel_Infiltration ?? GebaeudeFestwerte.VORGABE_LUFTWECHSEL_INFILTRATION,
                s.Zeile(GebaeudeZielfelder.LUFTWECHSEL_INFILTRATION).Wert, "Infiltration");
            Nah(g.Raumsolltemperatur_Tag, s.Zeile(GebaeudeZielfelder.SOLL_TAG).Wert, "Tagessollwert");
            Nah(g.Flaeche_Nutzer, s.Zeile(GebaeudeZielfelder.FLAECHE_JE_NUTZER).Wert, "Fläche je Nutzer");
        }

        private static double U(BauteilModel b, Bauteilrand rand, double neigung, IReadOnlyDictionary<int, BauteilaufbauModel> aufbauten)
        {
            if (b.U_Wert.HasValue) return b.U_Wert.Value;
            Assert.True(b.ID_Aufbau.HasValue, b.Bezeichner + ": weder U noch Aufbau");
            return Bauteilreduktion.UWertAusSchichten(GebaeudeZonenabbildung.AlsSchichten(aufbauten[b.ID_Aufbau.Value], b.Bezeichner),
                                                      neigung, rand, b.Bezeichner).U_WM2K;
        }

        private static void Nah(double erwartet, double? ist, string wo)
        {
            Assert.True(ist.HasValue, wo + ": kein Wert");
            Assert.True(Math.Abs(erwartet - ist.Value) <= Genau * Math.Max(1.0, Math.Abs(erwartet)),
                        wo + ": erwartet " + erwartet.ToString("R", CultureInfo.InvariantCulture) + ", ist " + ist.Value.ToString("R", CultureInfo.InvariantCulture));
        }

        // ==================================================================
        //  Probe 11 — Kennungen
        // ==================================================================

        [Fact]
        public void Probe11_zweimal_gleich_neu_gespeichert_gleich_Duplikat_anders()
        {
            if (!_db.Vorhanden) return;
            Anlegen();
            byte[] a = Exportieren(PROJEKT, GEBAEUDE, null, out GebaeudeExportSatz satz, out _);
            byte[] b = Exportieren(PROJEKT, GEBAEUDE, null, out _, out _);
            Assert.Equal(a, b);

            // Den Wandaufbau unverändert neu speichern: Die Schichten bekommen neue IDs, die Kennungen bleiben.
            int wand = satz.Zonen[0].Bauteile.Single(x => x.Bezeichner == "Wand Süd").ID_Aufbau.Value;
            BauteilaufbauModel m = new BauteilaufbauCtrl().LesenProjektsatz(wand);
            List<int> vorher = m.Schichten.Select(x => x.ID).ToList();
            Assert.True(new BauteilaufbauCtrl().ProjektSpeichern(PROJEKT, m).Ok);
            Assert.NotEqual(vorher, new BauteilaufbauCtrl().LesenProjektsatz(wand).Schichten.Select(x => x.ID).ToList());
            byte[] c = Exportieren(PROJEKT, GEBAEUDE, null, out _, out _);
            Assert.Equal(a, c);

            // Ein dupliziertes Projekt trägt andere Kennungen.
            string name = Convert.ToString(DataRepository.ExecuteScalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?",
                                                                        new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture);
            int neu = new ProjektDuplizierenCtrl().Duplizieren(name, name + " G7a");
            Assert.True(neu > 0);
            int idZ = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT g.ID_ProjektGebaeude FROM Tab_Gebaeude g INNER JOIN Tab_Zone z ON z.ID_Gebaeude = g.ID WHERE g.ID_Projekt = ?",
                new DbParam("@p", neu)), CultureInfo.InvariantCulture);
            byte[] d = Exportieren(neu, idZ, null, out _, out _);
            HashSet<string> idsA = Ids(a), idsD = Ids(d);
            Assert.Empty(idsA.Where(k => k.StartsWith("epos-bauteil-", StringComparison.Ordinal)).Intersect(idsD));
            Assert.DoesNotContain("epos-gebaeude-" + GEBAEUDE.ToString(CultureInfo.InvariantCulture), idsD);
        }

        private static HashSet<string> Ids(byte[] datei)
        {
            XDocument d = XDocument.Load(new MemoryStream(datei));
            List<string> ids = d.Descendants().Attributes("id").Select(x => x.Value).ToList();
            Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
            foreach (string id in ids) System.Xml.XmlConvert.VerifyNCName(id);
            foreach (XAttribute r in d.Descendants().Attributes().Where(x => x.Name.LocalName.EndsWith("IdRef", StringComparison.Ordinal)))
                Assert.Contains(r.Value, ids);
            return new HashSet<string>(ids, StringComparer.Ordinal);
        }

        // ==================================================================
        //  Klassenweg
        // ==================================================================

        [Fact]
        public void Klassenweg_liest_die_Uebernahme_und_nimmt_ihre_Protokolleintraege_heraus()
        {
            if (!_db.Vorhanden) return;
            SimulationProtokoll.NeuStarten();
            SimulationProtokoll.Aktuell.Hinweis("Eintrag des letzten Laufs");
            Protokollstand vorher = SimulationProtokoll.Aktuell.Stand;

            byte[] datei = Exportieren(PROJEKT, OHNE_ZONE, null, out GebaeudeExportSatz satz, out GebaeudeExportPlan plan);

            Assert.Equal(vorher, SimulationProtokoll.Aktuell.Stand);
            Assert.Equal(new[] { "Eintrag des letzten Laufs" }, SimulationProtokoll.Aktuell.Hinweise);
            Assert.True(satz.Klassenweg);
            Assert.True(satz.Uebernahme.Ok, satz.Uebernahme.Meldung);
            Assert.Equal(satz.Uebernahmeprotokoll.Count, plan.Meldungen.Count(m => m.Schluessel == GebaeudeExportAblauf.UEBERNAHME_PROTOKOLL));
            _ausgabe.WriteLine("Klassenweg: Hochrechnung in " + satz.UebernahmeLaufzeitMs.ToString("0", CultureInfo.InvariantCulture)
                               + " ms, Faktor " + satz.Uebernahme.Faktor.ToString("R", CultureInfo.InvariantCulture)
                               + ", " + satz.Uebernahmeprotokoll.Count.ToString(CultureInfo.InvariantCulture) + " Protokolleinträge herausgenommen.");

            GebaeudeBauteilvorschlag v = Importieren(datei, out _);
            Assert.Contains(v.Zeilen, z => z.Kennung.StartsWith("epos-klasse-" + OHNE_ZONE.ToString(CultureInfo.InvariantCulture) + "-", StringComparison.Ordinal));
            string schema = GbxmlExportProbe.Schemakopie();
            if (schema != null) Assert.Empty(GbxmlExportProbe.Schemapruefung(schema, datei));
        }

        [Fact]
        public void Klassenweg_ohne_Klimaregion_benannt_abgelehnt()
        {
            if (!_db.Vorhanden) return;
            DataRepository.ExecuteNonQuery("UPDATE Tab_Projekt SET ID_Klimaregion = 0 WHERE ID = ?", new DbParam("@p", PROJEKT));
            GebaeudeExportSatz satz = GebaeudeExportSatz.Lesen(PROJEKT, OHNE_ZONE, null);
            Assert.False(satz.Uebernahme.Ok);
            GebaeudeExportPlan plan = new GebaeudeExportAblauf().Vorbereiten(satz, GbxmlExportProbe.Profil());
            Assert.True(plan.Abgelehnt);
            Assert.Equal(GebaeudeExportAblauf.UEBERNAHME_ABGELEHNT, plan.Ablehnung.Schluessel);
        }

        // ==================================================================
        //  Probe 1c — Importprobe → EPOS → Export → Import
        // ==================================================================

        [Fact]
        public void Probe1c_Importprobe_importiert_exportiert_und_beide_gelesen_gleiche_Felder()
        {
            if (!_db.Vorhanden) return;
            GebaeudeImportAblauf probe = BauteilvorschlagProbe.Lesen(ZonenRundlauf.PROBE);
            GebaeudeBauteilvorschlag v1 = GebaeudeBauteilvorschlag.Bilden(probe, 0, null);
            Assert.False(v1.Abgelehnt);
            GebaeudeZonenCtrl.Vorschlagsergebnis e = new GebaeudeZonenCtrl().VorschlagSchreiben(GEBAEUDE, v1);
            Assert.True(e.Ok, e.Meldung);

            byte[] datei = Exportieren(PROJEKT, GEBAEUDE, null, out GebaeudeExportSatz satz, out _);
            GebaeudeBauteilvorschlag v2 = Importieren(datei, out _);

            // Je Zeile des ersten Imports die geschriebene EPOS-Zeile (Quellkennung) und ihre Zeile im Rückimport.
            List<BauteilModel> epos = satz.Zonen[0].Bauteile;
            Assert.Equal(v1.Zeilen.Count, epos.Count);
            Assert.Equal(v1.Zone.Nutzflaeche.Value, v2.Zone.Nutzflaeche.Value, 6);
            foreach (IGrouping<string, BauteilModel> gruppe in epos.GroupBy(x => x.Bauteilart + "|" + (x.Randbedingung ?? "")))
            {
                double a1 = gruppe.Sum(x => x.Flaeche);
                double a2 = v2.Zeilen.Where(z => z.Bauteil.Bauteilart + "|" + (z.Bauteil.Randbedingung ?? "") == gruppe.Key).Sum(z => z.Bauteil.Flaeche);
                Assert.True(Math.Abs(a1 - a2) <= 1e-6 * Math.Max(1.0, a1), gruppe.Key + ": " + a1 + " gegen " + a2);
            }
            double ua1 = epos.Where(x => x.Randbedingung != null || !GebaeudeZonenabbildung.LeerHeisstInnen(GebaeudeZonenabbildung.ArtAusZeile(x.Bauteilart).Value))
                             .Sum(x => x.Flaeche * U(x, GebaeudeZonenabbildung.RandAusZeile(x.Bauteilart, x.Randbedingung).Value,
                                                     x.Neigung ?? BauteilEingang.VorgabeNeigung(GebaeudeZonenabbildung.ArtAusZeile(x.Bauteilart).Value), satz.Aufbauten));
            double ua2 = v2.Zeilen.Where(z => z.Summenfeld != null)
                               .Sum(z => z.Bauteil.Flaeche * U(z.Bauteil, GebaeudeZonenabbildung.RandAusZeile(z.Bauteil.Bauteilart, z.Bauteil.Randbedingung).Value,
                                                             z.Bauteil.Neigung ?? BauteilEingang.VorgabeNeigung(GebaeudeZonenabbildung.ArtAusZeile(z.Bauteil.Bauteilart).Value),
                                                             v2.AufbautenJeId));
            Assert.True(Math.Abs(ua1 - ua2) <= 1e-6 * ua1, "Σ U·A: " + ua1 + " gegen " + ua2);
        }
    }
}

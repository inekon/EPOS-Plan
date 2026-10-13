using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Welle DA-2 — Rundlauf gbXML Export → Import</b> (E138 F5): Eine Zone mit fünf Kalendern (Grundwoche mit Stundenwerten,
    /// Ferien, Zeitraum, Betriebspause, Wochentagsbezug, neun Feiertage) und den Lasten je Zone (Personen mit Wärme je Person,
    /// Geräte und Licht, Außenluft je Person, Nutzerluftwechsel) wird geschrieben, mit dem eigenen Leser wieder eingelesen,
    /// über den Importweg der DA-1 (<see cref="GbxmlKonditionierung"/>) zu Kalendern gebildet und Feld für Feld verglichen:
    /// je Kalender die 8760-Reihe, der Nennwert und die Perioden (Bezeichner, Beginn, Ende, Art), dazu die Lastfelder der
    /// Zone (<see cref="ZonenLastfelderSchema"/>). Werte sind runde Phantasiewerte, ohne Datenbank.
    /// </summary>
    public class GbxmlExportRundlaufTests
    {
        private const double FLAECHE = 100.0;
        private const double VOLUMEN = 250.0;

        private static Kalenderangabe Woche(Func<int, int, double> wert)
        {
            var w = new double[Kalenderwoche.WOCHENWERTE];
            for (int d = 0; d < 7; d++)
                for (int h = 0; h < Kalenderwoche.TAGESSTUNDEN; h++) w[Kalenderwoche.Stelle(d, h)] = wert(d, h);
            return Kalenderangabe.AusWoche(w);
        }

        private static IEnumerable<Kalenderregel> Feiertage(Kalenderangabe a)
            => Enumerable.Range(0, DbWerte.KOND_FEIERTAGE.Count).Select(k => Kalenderregel.Feiertag(
                   Standardfahrplan.RANG_FEIERTAG + k, KonditionierungsvorlagenSaattabelle.FEIERTAGSNAMEN[k], DbWerte.KOND_FEIERTAGE[k], a));

        private static bool Tag(int d, int h) => d < 5 && h >= 8 && h < 18;

        /// <summary>Die fünf Kalender der Probe.</summary>
        internal static List<Konditionierungskalender> Kalender()
        {
            return new List<Konditionierungskalender>
            {
                new Konditionierungskalender(Konditionierungsgroesse.Heizsoll,
                    Woche((d, h) => d < 5 ? (h >= 6 && h < 22 ? 21.0 : 17.0) : (h >= 8 && h < 20 ? 19.0 : 17.0)), null,
                    Feiertage(Kalenderangabe.AusWert(18.0)).Concat(new[]
                    {
                        Kalenderregel.Zeitraum(Standardfahrplan.RANG_EIGEN, DbWerte.KOND_ART_ZEITRAUM, "Messe", 100, 110, Kalenderangabe.AlsWochentag(6)),
                        Kalenderregel.Zeitraum(Standardfahrplan.RANG_EIGEN + 1, DbWerte.KOND_ART_FERIEN, "Sommerferien", 200, 240, Kalenderangabe.AusWert(16.0)),
                    })),
                new Konditionierungskalender(Konditionierungsgroesse.Kuehlsoll,
                    Woche((d, h) => Tag(d, h) ? 26.0 : double.NaN), null,
                    new[] { Kalenderregel.Zeitraum(Standardfahrplan.RANG_EIGEN, DbWerte.KOND_ART_ZEITRAUM, "Hitzeperiode", 180, 190, Kalenderangabe.AusWert(25.0)) }),
                new Konditionierungskalender(Konditionierungsgroesse.Lueftung,
                    Woche((d, h) => Tag(d, h) ? 2.0 : 0.5), null, Array.Empty<Kalenderregel>()),
                new Konditionierungskalender(Konditionierungsgroesse.Geraete,
                    Woche((d, h) => Tag(d, h) ? 1.0 : 0.2), 800.0,
                    new[]
                    {
                        Kalenderregel.Zeitraum(Standardfahrplan.RANG_EIGEN, DbWerte.KOND_ART_ZEITRAUM, "Umbau", 50, 60, Kalenderangabe.AusWert(0.5)),
                        Kalenderregel.Zeitraum(Standardfahrplan.RANG_EIGEN + 1, DbWerte.KOND_ART_BETRIEBSPAUSE, "Betriebsurlaub", 210, 220, Kalenderangabe.Abgeschaltet),
                    }),
                new Konditionierungskalender(Konditionierungsgroesse.Personen,
                    Woche((d, h) => Tag(d, h) ? 1.0 : 0.0), 420.0,
                    Feiertage(Kalenderangabe.AusWert(0.0))),
            };
        }

        /// <summary>Das Probenabbild mit Kalendern und Lasten an Zone 11.</summary>
        internal static GebaeudeAbbild Abbild()
        {
            GebaeudeAbbild a = GbxmlExportProbe.Abbild();
            AbbildRaum r = a.Gebaeude[0].Raeume.Single(x => x.Kennung == GebaeudeExportKennung.Raum(GbxmlExportProbe.ZONE));
            var k = new AbbildKonditionierung();
            foreach (Konditionierungskalender kal in Kalender()) k.Kalender.Add(new AbbildKalender(kal, null));
            r.Konditionierung = k;
            r.LichtWm2 = 3.0;
            r.LuftwechselNutzerJeH = 2.0;
            r.SollKuehlenC = 26.0;
            r.GbxmlLasten = new GbxmlRaumlasten { WaermeJePersonW = 80.0, AussenluftJePersonM3h = 30.0 };
            return a;
        }

        private static (AbbildRaum Raum, string Xml) Rundlauf()
        {
            byte[] datei = GbxmlExportProbe.Schreiben(Abbild());
            GbxmlAbbild gelesen = GbxmlExportProbe.Lesen(datei);
            AbbildRaum r = gelesen.Gebaeude.SelectMany(g => g.Raeume).Single(x => x.Kennung == GebaeudeExportKennung.Raum(GbxmlExportProbe.ZONE));
            return (r, Encoding.UTF8.GetString(datei));
        }

        [Fact]
        public void Der_Export_schreibt_Plaene_Lasten_und_Konstanten_mit_stabilen_Kennungen()
        {
            (_, string xml) = Rundlauf();
            string zone = GebaeudeExportKennung.Zone(GbxmlExportProbe.ZONE);
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                Assert.Contains("<Schedule id=\"" + zone + "-" + Konditionierungsgroessen.Kennwort(g) + "\"", xml, StringComparison.Ordinal);
            foreach (string s in new[] { "heatSchedIdRef", "coolSchedIdRef", "airChangesSchedIdRef", "peopleScheduleIdRef",
                                         "equipmentScheduleIdRef", "lightScheduleIdRef", "<PeopleHeatGain", "<OAFlowPerPerson",
                                         "dayType=\"Holiday\"", "<BeginDate>2001-07-19</BeginDate>", "<EndDate>2001-08-28</EndDate>",
                                         "type=\"ExtSolar\"", "type=\"ExtIR\"" })
                Assert.Contains(s, xml, StringComparison.Ordinal);
            // Zweimal geschrieben, dieselbe Datei (stabile Kennungen, kein Zufall).
            Assert.Equal(GbxmlExportProbe.Schreiben(Abbild()), GbxmlExportProbe.Schreiben(Abbild()));
        }

        [Fact]
        public void Export_und_Import_geben_Kalender_Nennwerte_und_Lastfelder_ohne_Verlust_zurueck()
        {
            (AbbildRaum r, _) = Rundlauf();
            Assert.NotNull(r.GbxmlLasten);
            Zonenkonditionierung k = GbxmlKonditionierung.Bilden("Zone 1", new[] { r }, FLAECHE, VOLUMEN);
            Assert.NotNull(k);
            int felder = 0;
            foreach (Konditionierungskalender quelle in Kalender())
            {
                Groessenkonditionierung g = k.Groesse(quelle.Groesse);
                Assert.Equal(Konditionierungsherkunft.GbxmlDatei, g.Herkunft);
                Konditionierungskalender ziel = g.Kalender;
                Assert.NotNull(ziel);
                // Die 8760-Reihe (Wochentagsraster der Testdatenbank: 1. Januar Donnerstag) - Stunde für Stunde.
                double[] a = quelle.Auswerten(3, 0), b = ziel.Auswerten(3, 0);
                for (int h = 0; h < a.Length; h++)
                    Assert.True(Gleich(a[h], b[h]), Konditionierungsgroessen.Kennwort(quelle.Groesse) + " Stunde " + h + ": " + a[h] + " ≠ " + b[h]);
                felder += a.Length;
                Assert.Equal(quelle.Nennwert, ziel.Nennwert);
                felder++;
                // Die Perioden (ohne Feiertage): Bezeichner, Beginn, Ende, Art.
                var pq = quelle.Perioden.Where(p => !p.IstFeiertag).OrderBy(p => p.Beginn).ToList();
                var pz = ziel.Perioden.Where(p => !p.IstFeiertag).OrderBy(p => p.Beginn).ToList();
                Assert.Equal(pq.Select(p => (p.Bezeichner, p.Beginn, p.Ende, p.Art)), pz.Select(p => (p.Bezeichner, p.Beginn, p.Ende, p.Art)));
                felder += 4 * pq.Count;
                Assert.Equal(quelle.Perioden.Count(p => p.IstFeiertag), ziel.Perioden.Count(p => p.IstFeiertag));
                Assert.Equal(quelle.Perioden.Where(p => p.IstFeiertag).Select(p => p.Feiertagsregel).OrderBy(x => x),
                             ziel.Perioden.Where(p => p.IstFeiertag).Select(p => p.Feiertagsregel).OrderBy(x => x));
                felder += quelle.Perioden.Count(p => p.IstFeiertag);
            }
            // Der Nennluftwechsel der Zone (Vorgabe TAG) und der Heiz- und Kühlsollwert der Zone.
            Assert.Equal(2.0, k.Groesse(Konditionierungsgroesse.Lueftung).Vorgabe(DbWerte.KOND_ZEILE_TAG).Wert);
            felder++;

            // Die Lastfelder der Zone (Schemaschritt DA-2): ohne Umrechnung zurück.
            var zone = new ZoneModel();
            GbxmlKonditionierung.Lastfelder(zone, new[] { r }, FLAECHE);
            Assert.Equal(3.0, zone.Licht_W_m2);
            Assert.Equal(80.0, zone.Personen_Waerme_W);
            Assert.Equal(30.0, zone.Aussenluft_Person);
            felder += 3;
            Assert.True(felder > 5 * 8760);
        }

        [Fact]
        public void Der_Import_meldet_abweichende_Absorptionsgrade_und_schweigt_bei_den_Konstanten()
        {
            byte[] datei = GbxmlExportProbe.Schreiben(Abbild());
            Assert.DoesNotContain(GbxmlExportProbe.Lesen(datei).Meldungen,
                                  m => m.Schluessel.EndsWith("ABSORPTION_ABWEICHUNG", StringComparison.Ordinal));
            string xml = Encoding.UTF8.GetString(datei).Replace(">0.6</Absorptance>", ">0.8</Absorptance>");
            var meldungen = GbxmlExportProbe.Lesen(Encoding.UTF8.GetBytes(xml)).Meldungen.ToList();
            Assert.Contains(meldungen, m => m.Schluessel.EndsWith("ABSORPTION_ABWEICHUNG", StringComparison.Ordinal));
        }

        private static bool Gleich(double x, double y)
            => x.Equals(y) || (double.IsFinite(x) && double.IsFinite(y) && Math.Abs(x - y) <= 1e-9);
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Welle DA-1: gbXML-Import von Lasten und Zeitplänen</b> (Konzept Datenaustausch der Simulationsdaten 4.1, E138) an einer
    /// synthetischen Datei mit runden Phantasiewerten: zwei Räume einer Zone, Temperatur-, Anteils- und An/Aus-Pläne, zwei
    /// Jahresabschnitte, eine Ferienwoche, ein Feiertagsbild, ein Tagesplan mit 48 Werten, ein Plan fremden Typs, ein Tagesplan mit
    /// 23 Werten und ein Verweis ins Leere. Geprüft: Werte und Einheiten, Kalender je Art mit Perioden und Wochentagen,
    /// Verdichtung mit benanntem Verlust, Datei vor Profil, fehlende Pläne → Profilweg, ungültige Verweise benannt.
    /// </summary>
    public sealed class GbxmlLastenZeitplaeneTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const string PRAEFIX = GbxmlImportProfil.MELDUNGSPRAEFIX;

        // ==================================================================
        //  Die Probe
        // ==================================================================

        private static string Tag(string id, string typ, Func<int, double> wert, int werte = 24)
        {
            var sb = new StringBuilder();
            sb.Append("<DaySchedule id=\"").Append(id).Append("\" type=\"").Append(typ).Append("\">");
            for (int i = 0; i < werte; i++)
                sb.Append("<ScheduleValue>").Append(wert(i).ToString(CultureInfo.InvariantCulture)).Append("</ScheduleValue>");
            return sb.Append("</DaySchedule>").ToString();
        }

        private static string Woche(string id, params (string Typ, string Tag)[] tage)
            => "<WeekSchedule id=\"" + id + "\">" + string.Concat(tage.Select(t => "<Day dayType=\"" + t.Typ + "\" dayScheduleIdRef=\"" + t.Tag + "\"/>"))
               + "</WeekSchedule>";

        private static string Plan(string id, string typ, params (string Id, string Name, string Von, string Bis, string Woche)[] jahre)
            => "<Schedule id=\"" + id + "\" type=\"" + typ + "\">" + string.Concat(jahre.Select(j =>
                   "<YearSchedule id=\"" + j.Id + "\">" + (j.Name == null ? "" : "<Name>" + j.Name + "</Name>")
                   + "<BeginDate>" + j.Von + "</BeginDate><EndDate>" + j.Bis + "</EndDate><WeekScheduleId weekScheduleIdRef=\"" + j.Woche + "\"/></YearSchedule>"))
               + "<Name>" + id + "</Name></Schedule>";

        /// <summary>Die Probedatei; <paramref name="mitLasten"/> = false lässt Lasten und Pläne weg (nur Sollwerte der Zone).</summary>
        internal static byte[] Probe(bool mitLasten = true)
        {
            string raum1 = mitLasten
                ? "<Space id=\"r-1\" zoneIdRef=\"z-1\" conditionType=\"HeatedAndCooled\" peopleScheduleIdRef=\"s-pers\" equipmentScheduleIdRef=\"s-ger\" lightScheduleIdRef=\"s-licht\">"
                  + "<Name>Buero A</Name><Area>20</Area><Volume>60</Volume><PeopleNumber unit=\"NumberOfPeople\">2</PeopleNumber>"
                  + "<PeopleHeatGain unit=\"W\" heatGainType=\"Sensible\">80</PeopleHeatGain>"
                  + "<EquipPowerPerArea unit=\"WattPerSquareMeter\">10</EquipPowerPerArea><LightPowerPerArea unit=\"WattPerSquareMeter\">5</LightPowerPerArea>"
                  + "<OAFlowPerPerson unit=\"CubicMPerHr\">30</OAFlowPerPerson></Space>"
                : "<Space id=\"r-1\" zoneIdRef=\"z-1\"><Name>Buero A</Name><Area>20</Area><Volume>60</Volume></Space>";
            string raum2 = mitLasten
                ? "<Space id=\"r-2\" zoneIdRef=\"z-1\" conditionType=\"HeatedAndCooled\" peopleScheduleIdRef=\"s-aktiv\" equipmentScheduleIdRef=\"s-ger\">"
                  + "<Name>Buero B</Name><Area>30</Area><Volume>90</Volume><PeopleNumber unit=\"NumberOfPeople\">3</PeopleNumber>"
                  + "<EquipPowerPerArea unit=\"WattPerSquareMeter\">10</EquipPowerPerArea></Space>"
                : "<Space id=\"r-2\" zoneIdRef=\"z-1\"><Name>Buero B</Name><Area>30</Area><Volume>90</Volume></Space>";
            string zone = "<Zone id=\"z-1\"" + (mitLasten ? " heatSchedIdRef=\"s-heiz\" coolSchedIdRef=\"s-kuehl\" airChangesSchedIdRef=\"s-fehlt\"" : "") + ">"
                          + "<Name>Zone 1</Name><DesignHeatT unit=\"C\">21</DesignHeatT><DesignCoolT unit=\"C\">26</DesignCoolT></Zone>";
            string plaene = !mitLasten ? "" : string.Concat(
                // Heizen: Werktag 6–18 Uhr 21 °C, sonst 17 °C; Wochenende 17 °C; Feiertag 16 °C; Ferienwoche 3.–9. August 16 °C.
                Tag("d-heiz-wt", "Temp", h => h >= 6 && h < 18 ? 21 : 17), Tag("d-heiz-we", "Temp", _ => 17), Tag("d-heiz-ft", "Temp", _ => 16),
                Woche("w-heiz", ("Weekday", "d-heiz-wt"), ("Weekend", "d-heiz-we"), ("Holiday", "d-heiz-ft")),
                Woche("w-heiz-ferien", ("All", "d-heiz-ft")),
                Plan("s-heiz", "Temp", ("y-heiz", null, "2026-01-01", "2026-12-31", "w-heiz"), ("y-ferien", "Ferien Sommer", "2026-08-03", "2026-08-09", "w-heiz-ferien")),
                // Kühlen: Werktag 24 °C, Wochenende 99 °C (= aus).
                Tag("d-kuehl-wt", "Temp", _ => 24), Tag("d-kuehl-we", "Temp", _ => 99),
                Woche("w-kuehl", ("Weekday", "d-kuehl-wt"), ("Weekend", "d-kuehl-we")),
                Plan("s-kuehl", "Temp", ("y-kuehl", null, "2026-01-01", "2026-12-31", "w-kuehl")),
                // Personen: erstes Halbjahr Werktag 8–17 Uhr 1, zweites Halbjahr Werktag 8–12 Uhr 0,5 in 48 Halbstundenwerten.
                Tag("d-pers-a", "Fraction", h => h >= 8 && h < 17 ? 1 : 0), Tag("d-null", "Fraction", _ => 0),
                Tag("d-pers-b", "Fraction", i => i >= 16 && i < 24 ? (i % 2 == 0 ? 0.4 : 0.6) : 0, 48),
                Woche("w-pers-a", ("Weekday", "d-pers-a"), ("Weekend", "d-null")),
                Woche("w-pers-b", ("Weekday", "d-pers-b"), ("Weekend", "d-null")),
                Plan("s-pers", "Fraction", ("y-a", null, "2026-01-01", "2026-06-30", "w-pers-a"), ("y-b", null, "2026-07-01", "2026-12-31", "w-pers-b")),
                // Geräte An/Aus: Werktag 7–19 Uhr an, Samstag und Sonntag einzeln aus, ein Auslegungstag (übergangen).
                Tag("d-ger-wt", "OnOff", h => h >= 7 && h < 19 ? 1 : 0),
                Woche("w-ger", ("Weekday", "d-ger-wt"), ("Sat", "d-null"), ("Sun", "d-null"), ("HeatingDesignDay", "d-ger-wt")),
                Plan("s-ger", "OnOff", ("y-ger", null, "2026-01-01", "2026-12-31", "w-ger")),
                // Licht: immer 0,5.
                Tag("d-licht", "Fraction", _ => 0.5), Woche("w-licht", ("All", "d-licht")),
                Plan("s-licht", "Fraction", ("y-licht", null, "2026-01-01", "2026-12-31", "w-licht")),
                // Nicht abbildbar: fremder Typ und ein Tagesplan mit 23 Werten.
                Tag("d-aktiv", "ActivityLevel", _ => 100), Woche("w-aktiv", ("All", "d-aktiv")),
                Plan("s-aktiv", "ActivityLevel", ("y-aktiv", null, "2026-01-01", "2026-12-31", "w-aktiv")),
                Tag("d-kaputt", "Fraction", _ => 1, 23), Woche("w-kaputt", ("All", "d-kaputt")),
                Plan("s-kaputt", "Fraction", ("y-kaputt", null, "2026-01-01", "2026-12-31", "w-kaputt")));
            string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                + "<gbXML xmlns=\"http://www.gbxml.org/schema\" version=\"6.01\" temperatureUnit=\"C\" lengthUnit=\"Meters\" areaUnit=\"SquareMeters\" volumeUnit=\"CubicMeters\" useSIUnitsForResults=\"true\">"
                + "<Campus id=\"c-1\"><Location><Name>Probeort</Name><Latitude>48</Latitude><Longitude>11</Longitude><CADModelAzimuth>0</CADModelAzimuth></Location>"
                + "<Building id=\"b-1\" buildingType=\"Office\"><Name>Probebau</Name>" + raum1 + raum2 + "</Building></Campus>"
                + zone + plaene + "</gbXML>";
            return Encoding.UTF8.GetBytes(xml);
        }

        internal static GebaeudeAbbild Lesen(byte[] datei)
        {
            using var s = new MemoryStream(datei);
            return new GbxmlLeser().Lesen(s, new GbxmlImportProfil(), null, CancellationToken.None);
        }

        private static Zonenkonditionierung Bilden(GebaeudeAbbild a)
        {
            List<AbbildRaum> r = a.Gebaeude[0].Raeume;
            return GbxmlKonditionierung.Bilden("Zone 1", r, r.Sum(x => x.FlaecheM2 ?? 0.0), r.Sum(x => x.VolumenM3 ?? 0.0));
        }

        private static int Stelle(int wochentag, int stunde) => Kalenderwoche.Stelle(wochentag, stunde);

        private static IEnumerable<string> Namen(IEnumerable<PruefMeldung> m) => m.Select(x => x.Schluessel);

        // ==================================================================
        //  Leser
        // ==================================================================

        [Fact]
        public void Leser_LastenEinheitenUndPlaeneJeRaum()
        {
            GebaeudeAbbild a = Lesen(Probe());
            AbbildRaum r1 = a.Gebaeude[0].Raeume.Single(r => r.Kennung == "r-1");
            AbbildRaum r2 = a.Gebaeude[0].Raeume.Single(r => r.Kennung == "r-2");
            Assert.NotNull(r1.GbxmlLasten);
            Assert.Equal(80.0, r1.GbxmlLasten.WaermeJePersonW);
            Assert.Equal("Sensible", r1.GbxmlLasten.WaermeArt);
            Assert.Equal(30.0, r1.GbxmlLasten.AussenluftJePersonM3h);
            Assert.Equal("s-pers", r1.GbxmlLasten.PersonenPlan.Kennung);
            Assert.Equal("s-heiz", r1.GbxmlLasten.HeizPlan.Kennung);
            Assert.Null(r1.GbxmlLasten.LueftungPlan);   // s-fehlt: Verweis ins Leere
            Assert.Null(r2.GbxmlLasten.LichtPlan);

            // Der Heizplan: zwei Abschnitte, der Ferienabschnitt 3.–9. August = Tage 215–221 im Gemeinjahr.
            GbxmlZeitplan heiz = r1.GbxmlLasten.HeizPlan;
            Assert.True(heiz.IstTemperatur);
            Assert.Null(heiz.Verworfen);
            Assert.Equal(2, heiz.Abschnitte.Count);
            Assert.Equal((215, 221), (heiz.Abschnitte[1].Beginn, heiz.Abschnitte[1].Ende));
            Assert.Equal(16.0, heiz.Abschnitte[0].Woche.Feiertag[0]);
            double[] woche = heiz.Abschnitte[0].Woche.Woche();
            Assert.Equal(21.0, woche[Stelle(0, 6)]);
            Assert.Equal(17.0, woche[Stelle(0, 5)]);
            Assert.Equal(17.0, woche[Stelle(5, 12)]);

            // 48 Halbstundenwerte → Stundenmittel 0,5; benannt.
            GbxmlZeitplan pers = r1.GbxmlLasten.PersonenPlan;
            Assert.Equal(0.5, pers.Abschnitte[1].Woche.Woche()[Stelle(2, 9)], 10);
            Assert.Equal(1, pers.VerdichteteTage);

            // Nicht abbildbar, benannt verworfen.
            Assert.Equal("PLAN_TYP", r2.GbxmlLasten.PersonenPlan.Verworfen);
            List<string> namen = Namen(a.Meldungen).ToList();
            Assert.Contains(PRAEFIX + "PLAN_TYP", namen);
            Assert.Contains(PRAEFIX + "PLAN_STUNDEN", namen);
            Assert.Contains(PRAEFIX + "PLAN_WOCHE_LUECKE", namen);
            Assert.Contains(PRAEFIX + "PLAN_TAGTYP", namen);
            Assert.Contains(PRAEFIX + "PLAN_ZEITSCHRITT", namen);
            Assert.Contains(a.Meldungen, m => m.Schluessel == PRAEFIX + "VERWEIS_LEER" && m.Werte.Contains("s-fehlt"));
        }

        [Fact]
        public void Leser_Jahrestag_Gemeinjahr()
        {
            Assert.Equal(1, GbxmlZeitplanleser.Jahrestag("2026-01-01"));
            Assert.Equal(59, GbxmlZeitplanleser.Jahrestag("2024-02-29"));   // 29. Februar → 28.
            Assert.Equal(365, GbxmlZeitplanleser.Jahrestag("--12-31"));
            Assert.Null(GbxmlZeitplanleser.Jahrestag("31.12.2026"));
        }

        // ==================================================================
        //  Abbildung auf die Konditionierung
        // ==================================================================

        [Fact]
        public void Abbildung_Heizsoll_StandardwocheFerienUndFeiertage()
        {
            Zonenkonditionierung k = Bilden(Lesen(Probe()));
            Assert.NotNull(k);
            Assert.True(k.AusGbxml);
            Groessenkonditionierung h = k.Groesse(Konditionierungsgroesse.Heizsoll);
            Assert.Equal(Konditionierungsherkunft.GbxmlDatei, h.Herkunft);
            Assert.Equal(21.0, h.Vorgabe(DbWerte.KOND_ZEILE_TAG).Wert);
            Konditionierungskalender kal = h.Kalender;
            Assert.Equal(Angabeart.Woche, kal.Grundangabe.Art);
            Assert.Equal(21.0, kal.Standardwoche[Stelle(0, 10)]);
            Assert.Equal(17.0, kal.Standardwoche[Stelle(6, 10)]);
            Assert.Equal(9, kal.Perioden.Count(p => p.IstFeiertag));
            Assert.All(kal.Perioden.Where(p => p.IstFeiertag), p => Assert.Equal(16.0, p.Angabe.Wert));
            Kalenderregel ferien = kal.Perioden.Single(p => !p.IstFeiertag);
            Assert.Equal(DbWerte.KOND_ART_FERIEN, ferien.Art);
            Assert.Equal("Ferien Sommer", ferien.Bezeichner);
            Assert.Equal((215, 221), (ferien.Beginn, ferien.Ende));
            Assert.Equal(16.0, ferien.Angabe.Wert);
            Assert.StartsWith(WindowsFormsApplication1.MyResource.Resource.IMP_GBXML_KOND_HERKUNFT, h.Bemerkung);
        }

        [Fact]
        public void Abbildung_Personen_NennwertUndZweiAbschnitte()
        {
            Zonenkonditionierung k = Bilden(Lesen(Probe()));
            Konditionierungskalender kal = k.Groesse(Konditionierungsgroesse.Personen).Kalender;
            // 2 × 80 W (Datei) + 3 × 70 W (ohne Wärmeangabe die Vorgabe); der Plan des zweiten Raums ist verworfen → voll.
            Assert.Equal(370.0, kal.Nennwert);
            // Standardwoche = zweites Halbjahr (184 Tage), Periode = erstes Halbjahr (Tage 1–181).
            Assert.Equal(Math.Round((160 * 0.5 + 210) / 370.0, 4), kal.Standardwoche[Stelle(0, 9)]);
            Assert.Equal(Math.Round(210 / 370.0, 4), kal.Standardwoche[Stelle(0, 3)]);
            Kalenderregel halbjahr = kal.Perioden.Single();
            Assert.Equal(DbWerte.KOND_ART_ZEITRAUM, halbjahr.Art);
            Assert.Equal((1, 181), (halbjahr.Beginn, halbjahr.Ende));
            Assert.Equal(1.0, halbjahr.Angabe.Woche[Stelle(1, 9)]);
            Assert.Contains(k.Meldungen, m => m.Schluessel == PRAEFIX + "PLAN_TEILWEISE");
        }

        [Fact]
        public void Abbildung_GeraeteUndLicht_GewichtetVerdichtet()
        {
            Zonenkonditionierung k = Bilden(Lesen(Probe()));
            Konditionierungskalender kal = k.Groesse(Konditionierungsgroesse.Geraete).Kalender;
            Assert.Equal(600.0, kal.Nennwert);   // 10 W/m² × 50 m² + 5 W/m² × 20 m²
            Assert.Equal(Math.Round(550 / 600.0, 4), kal.Standardwoche[Stelle(0, 10)]);
            Assert.Equal(Math.Round(50 / 600.0, 4), kal.Standardwoche[Stelle(0, 3)]);
            Assert.Equal(Math.Round(50 / 600.0, 4), kal.Standardwoche[Stelle(5, 10)]);
            Assert.Empty(kal.Perioden);
            Assert.Contains(k.Meldungen, m => m.Schluessel == PRAEFIX + "VERDICHTET_RAEUME");
        }

        [Fact]
        public void Abbildung_KuehlsollUndLueftung_BegrenztUndUmgerechnet()
        {
            Zonenkonditionierung k = Bilden(Lesen(Probe()));
            Groessenkonditionierung kuehl = k.Groesse(Konditionierungsgroesse.Kuehlsoll);
            Assert.Equal(26.0, kuehl.Vorgabe(DbWerte.KOND_ZEILE_TAG).Wert);
            Assert.Equal(24.0, kuehl.Kalender.Standardwoche[Stelle(2, 12)]);
            Assert.True(double.IsNaN(kuehl.Kalender.Standardwoche[Stelle(6, 12)]));   // 99 °C → aus
            Assert.Contains(k.Meldungen, m => m.Schluessel == PRAEFIX + "WERTE_BEGRENZT" && m.Werte[2] == "48");

            Groessenkonditionierung lueftung = k.Groesse(Konditionierungsgroesse.Lueftung);
            Assert.Null(lueftung.Kalender);
            Assert.Equal(0.4, lueftung.Vorgabe(DbWerte.KOND_ZEILE_TAG).Wert, 10);   // 2 × 30 m³/h / 150 m³
            Assert.Contains(k.Meldungen, m => m.Schluessel == PRAEFIX + "AUSSENLUFT_PERSON");
        }

        [Fact]
        public void Abbildung_Zaehler_JeZone()
        {
            Zonenkonditionierung k = Bilden(Lesen(Probe()));
            PruefMeldung z = k.Meldungen.Single(m => m.Schluessel == PRAEFIX + "ZONE_LASTEN");
            // 7 Lastwerte, 6 Pläne, 2 verdichtet (Geräte aus zwei Plänen, Kühlwerte begrenzt), 1 verworfen (ActivityLevel).
            Assert.Equal(new[] { "Zone 1", "7", "6", "2", "1" }, z.Werte);
        }

        [Fact]
        public void Abbildung_OhneLasten_ProfilwegUnveraendert()
        {
            GebaeudeAbbild a = Lesen(Probe(mitLasten: false));
            Assert.All(a.Gebaeude[0].Raeume, r => Assert.Null(r.GbxmlLasten));
            Assert.Null(Bilden(a));
        }

        [Fact]
        public void Abbildung_NennwertOhnePlan_ProfilplanBleibt()
        {
            GebaeudeAbbild a = Lesen(Probe());
            foreach (AbbildRaum r in a.Gebaeude[0].Raeume) r.GbxmlLasten.PersonenPlan = null;
            Groessenkonditionierung p = Bilden(a).Groesse(Konditionierungsgroesse.Personen);
            Assert.Equal(Konditionierungsherkunft.GbxmlDatei, p.Herkunft);
            Assert.Null(p.Kalender);
            Assert.Equal(370.0, p.NennwertDatei);
        }

        [Fact]
        public void DateiVorProfil_AbweichungAlsHinweis()
        {
            Zonenkonditionierung k = Bilden(Lesen(Probe()));
            var profil = new Raumnutzungsprofil { Bezeichner = "Probebuero", Personen_Flaeche = 10, Personen_Waerme = 70, Geraete_Leistung = 12 };
            List<PruefMeldung> h = GbxmlKonditionierung.Profilabgleich(k, profil, 50.0);
            PruefMeldung p = Assert.Single(h);   // Personen 370 W gegen 350 W; Geräte 600 W gleich
            Assert.Equal(PRAEFIX + "PROFIL_ABWEICHUNG", p.Schluessel);
            Assert.Equal("370", p.Werte[2]);
            Assert.Equal("350", p.Werte[4]);
        }
    }
    /// <summary>Welle DA-1 am Zonenplan: Die Zone aus den beiden Räumen trägt die Konditionierung der Datei (Datei vor Profil).</summary>
    public sealed class GbxmlLastenZonenplanTests : IClassFixture<TestDatenbank>, IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly TestDatenbank _db;

        public GbxmlLastenZonenplanTests(TestDatenbank db) => _db = db;

        public void Dispose() => _kultur.Dispose();

        [Fact]
        public void Zonenplan_ZoneTraegtGbxmlKonditionierung()
        {
            GebaeudeAbbild a = GbxmlLastenZeitplaeneTests.Lesen(GbxmlLastenZeitplaeneTests.Probe());
            Zonenplan zp = Zonenplan.Vorschlag(a, 0, GebaeudeImportProfil.ZONENREGEL_X1);
            Planzone z = Assert.Single(zp.Zonen);
            Assert.NotNull(z.Projektdatei);
            Assert.True(z.Projektdatei.AusGbxml);
            Assert.False(z.Projektdatei.AusIfc);
            Assert.Equal(370.0, z.Projektdatei.Groesse(Konditionierungsgroesse.Personen).Kalender.Nennwert);

            GebaeudeAbbild ohne = GbxmlLastenZeitplaeneTests.Lesen(GbxmlLastenZeitplaeneTests.Probe(mitLasten: false));
            Assert.All(Zonenplan.Vorschlag(ohne, 0, GebaeudeImportProfil.ZONENREGEL_X1).Zonen, x => Assert.Null(x.Projektdatei));
        }
    }
}

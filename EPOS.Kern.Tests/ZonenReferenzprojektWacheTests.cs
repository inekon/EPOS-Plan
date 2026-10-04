using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using EPOS.Referenzlaeufe.Skripte;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Zonenbestand ohne die Zonenprojekte 1052 und 1054</b> (G6d, AK1z): Tests, die auf einer Arbeitskopie
    /// eigene Zonen, Bauteile, Luftströme oder Zonenkalender anlegen und die Tabellen im Ganzen zählen, zählen ohne
    /// die Zeilen von 1052 (<see cref="ZonenReferenzprojektWacheTests"/>) und seiner Kopie 1054
    /// (<see cref="ZonenHeizkreisReferenzprojektWacheTests"/>) — den einzigen Projekten der Testdatenbank mit Zonen.
    /// Ohne sie ist jede Abfrage die Zählung der ganzen Tabelle.
    /// </summary>
    internal static class Zonenbestand
    {
        /// <summary>Die Zonenprojekte der Testdatenbank.</summary>
        internal static readonly long[] PROJEKTE = { Zonenprojekt1052.NEU, Zonenprojekt1054.NEU };

        /// <summary>Die Gebäude der Zonenprojekte.</summary>
        internal const string GEBAEUDE_ZONENPROJEKTE = "SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt IN (1052, 1054)";

        /// <summary>Die Zonen der Zonenprojekte.</summary>
        internal const string ZONEN_ZONENPROJEKTE = "SELECT ID FROM Tab_Zone WHERE ID_Gebaeude IN (" + GEBAEUDE_ZONENPROJEKTE + ")";

        /// <summary>Zonen ohne die der Zonenprojekte.</summary>
        internal const string ZONEN = "SELECT COUNT(*) FROM Tab_Zone WHERE ID NOT IN (" + ZONEN_ZONENPROJEKTE + ")";

        /// <summary>Bauteile ohne die der Zonenprojekte.</summary>
        internal const string BAUTEILE = "SELECT COUNT(*) FROM Tab_Bauteil WHERE ID_Zone NOT IN (" + ZONEN_ZONENPROJEKTE + ")";

        /// <summary>Luftströme ohne die der Zonenprojekte.</summary>
        internal const string LUFTSTROEME = "SELECT COUNT(*) FROM Tab_Zonenluftstrom WHERE ID_ZoneA NOT IN (" + ZONEN_ZONENPROJEKTE + ")";

        /// <summary>Die Bedingung „kein Eigentum eines Zonenprojekts" an Kalender- und Vorgabezeilen (Spalte <c>ID_Gebaeude</c>).</summary>
        internal const string NICHT_ZONENPROJEKTE = "(ID_Gebaeude IS NULL OR ID_Gebaeude NOT IN (" + GEBAEUDE_ZONENPROJEKTE + "))";

        /// <summary>Die Kalender der Zonenprojekte (die Heizkalender ihrer beheizten Zonen).</summary>
        internal const string KALENDER_ZONENPROJEKTE = "SELECT ID FROM Tab_Konditionierungskalender WHERE ID_Gebaeude IN (" + GEBAEUDE_ZONENPROJEKTE + ")";
    }

    /// <summary>
    /// <b>Wache des Referenzprojekts 1052 „Referenzprojekt Zonen"</b> (Mehrzonenkonzept 9, Stufe G6d) — das
    /// ungekoppelte Zonenprojekt der Testdatenbank (seine Kopie 1054 rechnet gekoppelt): drei Zonen am Gebäude der Kopie von 1018 (Gästezimmer und
    /// Gastronomie beheizt, Keller unbeheizt am Erdreich), Trennwand, Kellerdecken, ein Luftstrom, ein
    /// Heizkalender je beheizter Zone, Aufheizoptimierung an. Zonenschnitt und gesäte Zellen stehen in EINER
    /// Quelle, dem Bauplan <c>Referenzlaeufe/Skripte/referenzprojekt_1052_bauplan.cs</c> (hier verlinkt); das
    /// Saatskript <c>referenzprojekt_1052_zonen.cs</c> zieht ihn.
    /// <list type="bullet">
    /// <item><b>Genau 1052 und seine Kopie 1054 tragen Zonen</b>, Trennflächen, Luftströme und Zonenkalender; 1052
    /// bleibt ungekoppelt.</item>
    /// <item><b>Jede gesäte Zelle</b> steht wie im Bauplan (<see cref="Zonenprojekt1052.Pruefen"/>: Kopf,
    /// jede Zonen- und Bauteilzelle samt Nachbarzone, Luftstrom, Herkunft der Kalender, Aufheizvorgabe), dazu
    /// die Kennzahlen des Schnitts ausdrücklich.</item>
    /// <item><b>Dieselben Programmwege auf einer Arbeitskopie</b> ergeben einen bitgleichen Abdruck (ohne
    /// Schlüssel, die Bezüge zwischen den Zonen über den Rang) — bricht die Übernahme, der OK-Weg der Zonen,
    /// „Vorlage übernehmen" oder der Kopierweg, fällt es hier auf, nicht erst in der Basis (B28 des Entwurfs
    /// KP3, Festlegung 33 sinngemäß).</item>
    /// <item><b>Die Zonenkalender springen zu verschiedenen Stunden</b> (Wohnen 6/22 Uhr, Büro 7/18 Uhr).</item>
    /// <item><b>Die Zonenschleife rechnet 1052 deterministisch</b> (drei Zonen, zwei beheizt; zwei Läufe
    /// bitgleich), mit einem Aufheizplan je Zone (R3), und <b>ein Lauf schreibt drei Zonenzeilen</b> nach
    /// <c>Tab_ErgebnisZone</c>.</item>
    /// </list>
    /// 1052 steht in der Basis R34 (Einfrierregel „gesäte Zonendaten",
    /// <c>Referenzlaeufe/LIESMICH.md</c>).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZonenReferenzprojektWacheTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _aus;

        public ZonenReferenzprojektWacheTests(TestDatenbank db, ITestOutputHelper aus)
        {
            _db = db;
            _aus = aus;
        }

        private const int PROJEKT = Zonenprojekt1052.NEU;

        private static string F(double x, string format = "F2") => x.ToString(format, CultureInfo.InvariantCulture);

        private static long Zahl(string sql, params object[] w) => Zonenprojekt1052.Zahl(sql, w);

        private static List<long> Liste(string sql)
            => Zonenprojekt1052.Tabelle(sql).Rows.Cast<DataRow>().Select(r => Convert.ToInt64(r[0], CultureInfo.InvariantCulture)).ToList();

        // =====================================================================
        //  Genau 1052
        // =====================================================================

        /// <summary>
        /// Zonen, Trennflächen, Luftströme und Zonenkalender tragen genau 1052 und seine Kopie 1054 (AK1z,
        /// <see cref="ZonenHeizkreisReferenzprojektWacheTests"/>); 1052 selbst bleibt ungekoppelt.
        /// </summary>
        [Fact]
        public void Genau_1052_und_1054_tragen_Zonen_Trennflaechen_Luftstroeme_und_Zonenkalender()
        {
            if (!_db.Vorhanden) return;
            long[] PROJEKTE = Zonenbestand.PROJEKTE;
            Assert.Equal(PROJEKTE, Liste(
                "SELECT DISTINCT g.ID_Projekt FROM Tab_Zone z JOIN Tab_Gebaeude g ON g.ID = z.ID_Gebaeude ORDER BY 1"));
            Assert.Equal(PROJEKTE, Liste(
                "SELECT DISTINCT g.ID_Projekt FROM Tab_Bauteil b JOIN Tab_Zone z ON z.ID = b.ID_Zone " +
                "JOIN Tab_Gebaeude g ON g.ID = z.ID_Gebaeude WHERE b.ID_Nachbarzone IS NOT NULL ORDER BY 1"));
            Assert.Equal(PROJEKTE, Liste(
                "SELECT DISTINCT g.ID_Projekt FROM Tab_Zonenluftstrom l JOIN Tab_Zone z ON z.ID = l.ID_ZoneA " +
                "JOIN Tab_Gebaeude g ON g.ID = z.ID_Gebaeude ORDER BY 1"));
            Assert.Equal(PROJEKTE, Liste(
                "SELECT DISTINCT g.ID_Projekt FROM Tab_Konditionierungskalender k JOIN Tab_Gebaeude g ON g.ID = k.ID_Gebaeude " +
                "WHERE k.ID_Zone IS NOT NULL ORDER BY 1"));
            Assert.Equal(Zonenprojekt1052.NAME, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Z_ProjektGebaeude WHERE ID_Projekt = ?", PROJEKT));
            // 1052 bleibt ungekoppelt: keine Kopplungsstufe, kein Heizkreis, keine Übergabe an den Zonen.
            Assert.Null(KonfigurationCtrl.AnlagenkopplungLesen(PROJEKT));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ? AND Heizkreis_Aktiv <> 0", PROJEKT));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Zone WHERE ID_Gebaeude IN (SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?) AND " +
                                  "COALESCE(Uebergabe_Art, Uebergabe_Exponent, Uebergabe_Leistung_Nenn, Auslegung_Vorlauf, Auslegung_Ruecklauf, " +
                                  "Auslegung_Raumtemperatur, Regler_Proportionalband) IS NOT NULL", PROJEKT));
        }

        // =====================================================================
        //  Jede gesäte Zelle
        // =====================================================================

        [Fact]
        public void Jede_gesaete_Zelle_steht_wie_im_Bauplan()
        {
            if (!_db.Vorhanden) return;
            List<string> abw = Zonenprojekt1052.Pruefen(PROJEKT);
            Assert.True(abw.Count == 0, string.Join("\n", abw));

            // Die Kennzahlen des Schnitts ausdrücklich (gegen einen Bauplan, der mit dem Kern mitwandert).
            int geb = Zonenprojekt1052.Gebaeude(PROJEKT);
            List<ZoneModel> z = new GebaeudeZonenCtrl().LesenJeGebaeude(geb);
            Assert.Equal(new[] { Zonenprojekt1052.ZONE_GAESTE, Zonenprojekt1052.ZONE_GASTRO, Zonenprojekt1052.ZONE_KELLER },
                         z.Select(x => x.Bezeichner).ToArray());
            Assert.Equal(new[] { true, true, false }, z.Select(x => x.IstBeheizt).ToArray());
            Assert.Equal(new[] { 15, 14, 2 }, z.Select(x => x.Bauteile.Count).ToArray());
            Assert.Equal(300.0, z[0].Nutzflaeche.Value, 9);
            Assert.Equal(200.0, z[1].Nutzflaeche.Value, 9);
            Assert.Equal(531.554, z[2].Nutzflaeche.Value, 3);
            Assert.Equal(0.0, z[2].Interne_Waermegewinne);
            Assert.Equal(0.0, z[2].Bewohner);

            BauteilModel trenn = z[0].Bauteile.Single(b => b.Bezeichner == Zonenprojekt1052.TRENNWAND);
            Assert.Equal(DbWerte.RANDBEDINGUNG_ZONE, trenn.Randbedingung);
            Assert.Equal(z[1].ID, trenn.ID_Nachbarzone);
            Assert.Null(trenn.Trennflaeche_Zuordnung);
            Assert.Equal(30.0, trenn.Flaeche);
            Assert.Equal(0.6, trenn.U_Wert);
            foreach (ZoneModel x in z.Take(2))
            {
                BauteilModel decke = x.Bauteile.Single(b => b.Bezeichner == Zonenprojekt1052.KELLERDECKE);
                Assert.Equal(z[2].ID, decke.ID_Nachbarzone);
                Assert.Equal(DbWerte.BAUTEILART_DECKE, decke.Bauteilart);
            }
            Assert.Equal(531.554, z[0].Bauteile.Single(b => b.Bezeichner == Zonenprojekt1052.KELLERDECKE).Flaeche
                                  + z[1].Bauteile.Single(b => b.Bezeichner == Zonenprojekt1052.KELLERDECKE).Flaeche, 3);
            Assert.All(z[2].Bauteile, b => Assert.Equal(DbWerte.RANDBEDINGUNG_ERDREICH, b.Randbedingung));
            Assert.Equal(230.6, z[2].Bauteile.Single(b => b.Bezeichner == Zonenprojekt1052.KELLERWAENDE).Flaeche, 1);
            Assert.DoesNotContain(z[2].Bauteile, b => b.ID_Nachbarzone.HasValue);   // je Paar führt EINE Seite

            ZonenluftstromModel l = new GebaeudeZonenCtrl().LuftstroemeJeGebaeude(geb).Single();
            Assert.Equal((z[0].ID, z[1].ID, 150.0), (l.ID_ZoneA, l.ID_ZoneB, l.Volumenstrom));

            Aufheizvorgabe v = KonfigurationCtrl.AufheizvorgabeLesen(PROJEKT);
            Assert.True(v.An);
            Assert.Null(v.Bemessung);
            Assert.Equal(0.2, v.Reserve);
            Assert.Null(v.Art);
            Assert.False(v.HatAufschlag);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ? AND Aufheizzeit_Manuell_H IS NOT NULL", PROJEKT));
            _aus.WriteLine("1052: Zonen {0}; Faktor der Übernahme {1}", string.Join(", ", z.Select(x => x.Bezeichner + " " +
                F(x.Nutzflaeche.Value) + " m²")), Zonenprojekt1052.Planen(Zonenprojekt1052.VORLAGE, out _).Faktor.ToString("R", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Die Heizkalender der beiden beheizten Zonen springen zu verschiedenen Stunden: „Wohnen" hebt um 6 Uhr
        /// und senkt um 22 Uhr (18 → 20 °C), „Büro" hebt um 7 Uhr und senkt um 18 Uhr (16 → 20 °C) und bleibt am
        /// Wochenende und an Feiertagen unten.
        /// </summary>
        [Fact]
        public void Die_Zonenkalender_springen_zu_verschiedenen_Stunden()
        {
            if (!_db.Vorhanden) return;
            int geb = Zonenprojekt1052.Gebaeude(PROJEKT);
            List<ZoneModel> z = new GebaeudeZonenCtrl().LesenJeGebaeude(geb);
            const int JAHR = Konditionierungsarbeitsstand.BEZUGSJAHR_VORGABE;
            int w0 = Konditionierungsarbeitsstand.WochentagDesErstenJanuar(JAHR);
            var kond = new KonditionierungCtrl();
            (HashSet<int> hoch, HashSet<int> runter, double min, double max) Spruenge(ZoneModel x)
            {
                Konditionierungsstand s = kond.StandLesen(KonditionierungCtrl.Eigner.Zone(geb, x.ID), out string m);
                Assert.Null(m);
                double[] reihe = s.Kalender(Konditionierungsgroesse.Heizsoll).Auswerten(w0, JAHR);
                Assert.Equal(8760, reihe.Length);
                var hoch = new HashSet<int>();
                var runter = new HashSet<int>();
                for (int h = 1; h < reihe.Length; h++)
                {
                    if (reihe[h] > reihe[h - 1]) hoch.Add(h % 24);
                    if (reihe[h] < reihe[h - 1]) runter.Add(h % 24);
                }
                return (hoch, runter, reihe.Min(), reihe.Max());
            }
            var gaeste = Spruenge(z[0]);
            var gastro = Spruenge(z[1]);
            Assert.Equal(new[] { 6 }, gaeste.hoch.ToArray());
            Assert.Equal(new[] { 22 }, gaeste.runter.ToArray());
            Assert.Equal((18.0, 20.0), (gaeste.min, gaeste.max));
            Assert.Equal(new[] { 7 }, gastro.hoch.ToArray());
            Assert.Equal(new[] { 18 }, gastro.runter.ToArray());
            Assert.Equal((16.0, 20.0), (gastro.min, gastro.max));
            Assert.Null(kond.StandLesen(KonditionierungCtrl.Eigner.Zone(geb, z[2].ID), out _).Kalender(Konditionierungsgroesse.Heizsoll));
        }

        // =====================================================================
        //  Dieselben Programmwege — bitgleicher Abdruck
        // =====================================================================

        [Fact]
        public void Dieselben_Programmwege_auf_einer_Arbeitskopie_ergeben_einen_bitgleichen_Abdruck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            // Unter der Kultur des Saatskripts: Die Herkunft eines übernommenen Kalenders steht in der
            // Anzeigesprache in Bemerkung („aus Vorlage Wohnen").
            using var kultur = new Kulturvorrichtung("de-DE");
            const string NACHBAU = "G6d Nachbau 1052";
            string soll = Zonenprojekt1052.Abdruck(PROJEKT, true);

            int id = new ProjektDuplizierenCtrl().Duplizieren(Zonenprojekt1052.VORLAGE_NAME, NACHBAU);
            Assert.True(id > PROJEKT, "Die Kopie fiel auf " + id);
            string fehler = Zonenprojekt1052.Bauen(id);
            Assert.True(fehler == null, fehler);
            Assert.Empty(Zonenprojekt1052.Pruefen(id));
            string ist = Zonenprojekt1052.Abdruck(id, true).Replace(NACHBAU, Zonenprojekt1052.NAME);

            if (ist != soll)
            {
                string[] a = soll.Split('\n'), b = ist.Split('\n');
                int i = 0;
                while (i < Math.Min(a.Length, b.Length) && a[i] == b[i]) i++;
                Assert.Fail("Abdruck weicht ab ab Zeile " + i + ":\n  1052:    " + (i < a.Length ? a[i] : "∅") +
                            "\n  Nachbau: " + (i < b.Length ? b[i] : "∅"));
            }
            _aus.WriteLine("Abdruck 1052 = Nachbau {0}: {1} Zeilen, {2} Zeichen", id, soll.Count(c => c == '\n'), soll.Length);
        }

        // =====================================================================
        //  Die Zonenschleife
        // =====================================================================

        [Fact]
        public void Die_Zonenschleife_rechnet_1052_deterministisch_mit_einem_Aufheizplan_je_Zone()
        {
            if (!_db.Vorhanden) return;
            Aufheizvorgabe vorgabe = KonfigurationCtrl.AufheizvorgabeLesen(PROJEKT);
            List<AufheizLauf.Gebaeudelauf> eins = AufheizLauf.Projekt(PROJEKT, vorgabe);
            List<AufheizLauf.Gebaeudelauf> zwei = AufheizLauf.Projekt(PROJEKT, vorgabe);
            AufheizLauf.Gebaeudelauf a = Assert.Single(eins), b = Assert.Single(zwei);
            Assert.True(a.Gerechnet && b.Gerechnet);
            Assert.NotNull(a.Mehrzonen);
            Assert.NotNull(b.Mehrzonen);
            Assert.Equal(3, a.Mehrzonen.Zonen.Count);
            AufheizGrenzfallTests.Bitgleich(a.Ziel, b.Ziel, "1052, Heizreihe");
            AufheizGrenzfallTests.Bitgleich(a.Ergebnis, b.Ergebnis, "1052");
            for (int i = 0; i < 3; i++)
                AufheizGrenzfallTests.Bitgleich(a.Mehrzonen.Zonen[i], b.Mehrzonen.Zonen[i], "1052, " + a.Mehrzonen.Eingaenge[i].Bezeichnung);

            IReadOnlyList<ZonenEingang> e = a.Mehrzonen.Eingaenge;
            Assert.Equal(2, e.Count(z => !z.Aufheizplan.Unbeheizt));
            foreach (ZonenEingang z in e)
            {
                Aufheizplan p = z.Aufheizplan;
                Assert.NotNull(p);
                if (p.Unbeheizt)
                {
                    Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT, p.Zustand);
                    Assert.Equal(Zonenprojekt1052.ZONE_KELLER, z.Bezeichnung);
                    continue;
                }
                Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, p.Zustand);
                Assert.Equal(DbWerte.AUFHEIZ_QUELLE_ZIEL, p.Bemessung.Quelle);
                Assert.True(p.Geaendert, z.Bezeichnung + ": keine Rampe");
                Assert.True(p.Aufheiztage > 0);
                _aus.WriteLine("{0}: P_auf {1} kW, t_auf,max {2} h, Rampentage {3}, Aufheizstunden {4}, längste Rampe {5} h, " +
                               "Jahresheizwärme {6} MWh, Spitze {7} kW", z.Bezeichnung, F(p.Bemessung.AufheizleistungW / 1000.0, "F3"),
                               p.Bemessung.Wirksam.AufheizzeitMaxH, p.Aufheiztage, p.AufheizstundenH, p.LaengsteRampeH,
                               F(a.Mehrzonen.Zonen[e.ToList().IndexOf(z)].HeizlastW.Sum() / 1e6, "F3"),
                               F(a.Mehrzonen.Zonen[e.ToList().IndexOf(z)].HeizlastW.Max() / 1000.0, "F3"));
            }
            Assert.NotNull(a.Mehrzonen.Aufheizgebaeude);
            Assert.Equal(2, a.Mehrzonen.Aufheizgebaeude.ZonenBeheizt);
            Assert.Equal(1, a.Mehrzonen.Aufheizgebaeude.ZonenUnbeheizt);
        }

        [Fact]
        public void Ein_Lauf_schreibt_drei_Zonenzeilen_nach_Tab_ErgebnisZone()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            int kopf = new SimulationRunner().SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);

            ErgebnisModel m = new ErgebnisCtrl().Load(PROJEKT);
            ErgebnisGebaeudeModel g = Assert.Single(m.Gebaeude);
            Assert.Equal(new[] { Zonenprojekt1052.ZONE_GAESTE, Zonenprojekt1052.ZONE_GASTRO, Zonenprojekt1052.ZONE_KELLER },
                         g.Zonen.Select(z => z.Bezeichner).ToArray());
            Assert.Equal(new[] { true, true, false }, g.Zonen.Select(z => z.IstBeheizt).ToArray());
            Assert.All(g.Zonen.Take(2), z => Assert.True(z.HeizwaermeMwh > 0.0));
            Assert.Null(g.Zonen[2].HeizwaermeMwh);
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisZone WHERE ID_ErgebnisGebaeude IN " +
                                  "(SELECT ID FROM Tab_ErgebnisGebaeude WHERE ID_Ergebnis = ?)", kopf));
            _aus.WriteLine("Tab_ErgebnisZone, Lauf {0}: {1}", kopf, string.Join("; ", g.Zonen.Select(z =>
                z.Bezeichner + " " + (z.HeizwaermeMwh.HasValue ? F(z.HeizwaermeMwh.Value, "F3") + " MWh, Spitze " + F(z.SpitzeKw ?? double.NaN, "F3") + " kW"
                                                               : "unbeheizt") + ", θ " + F(z.MittlereRaumtemperaturC ?? double.NaN) + " °C")));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Wache der sechs Referenzprojekte der Zapfprofil-Optionen</b> (Konzept Zapfprofil Benutzerführung, ZB12;
    /// Einfrierregel „gesäte Zapfprofil-Eingaben", <c>Referenzlaeufe/LIESMICH.md</c>).
    ///
    /// <para><b>Gegenstand:</b> die Projekte 1066 bis 1071, Kopien von 1045, gesät von
    /// <c>Referenzlaeufe/Skripte/referenzprojekte_zapfprofil_optionen.cs</c> — 1066 Mischnutzung (vier Zonen,
    /// Wohnungstabelle, Zirkulation nach Leitungslänge), 1067 Kalender (Kalender der Nutzungsart mit fünf eigenen
    /// Ferienzeiträumen, Gebäudekalender mit Feiertagsland), 1068 stochastische Jahresreihe, 1069 Typtage (erfundener
    /// Typtagsatz), 1070 Kalibrierung und manuelle Werte, 1071 Auslegung (vier Topologien, Ecodesign-Bedarfstag).</para>
    ///
    /// <para><b>Was sie hält:</b> jede Zelle der Projektzeilen und Zonen samt Wohnungstypen und Ferienzeiträumen, den
    /// Gebäudekalender von 1067, den eingespielten Typtagsatz; die Jahressummen der Generator-Bilanz je Projekt und Zone
    /// und — Stunde für Stunde zeichengleich — die Brauchwasserreihe der aktuellen Basis; für 1071 die eingefrorenen Werte
    /// der Auslegung (Empfehlung je Topologiegruppe und der Vergleich der Verfahren, stochastisch gerechnet). Die
    /// Auslegung wirkt nicht auf die Simulation; ohne diese Wache stünde sie außerhalb des Regressionsnetzes.
    /// Alles kulturunabhängig (InvariantCulture, Muster <see cref="ZapfprofilReferenzprojektWacheTests"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ZapfprofilOptionenReferenzprojekteWacheTests : IClassFixture<TestDatenbank>
    {
        /// <summary>Die Vorlage der sechs Kopien.</summary>
        private const int VORLAGE = 1045;

        private const string KATALOG = "TEST-1";

        private const string WOHNEN = "Wohnen groß (abgeleitet)";
        private const string EFH = "Ein- und Zweifamilienhaus (abgeleitet)";
        private const string STUDENT = "Studentenwohnheim (abgeleitet)";
        private const string HOTEL = "Hotel (aus Messung, je Zimmer)";
        private const string BUERO = "Büro (Setzung)";
        private const string SCHULE = "Schule (Setzung)";
        private const string GEWERBE = "Gewerbe Schichtbetrieb (Setzung)";

        /// <summary>Die sechs Projekte mit ihren Namen.</summary>
        private static readonly Dictionary<int, string> Projekte = new Dictionary<int, string>
        {
            [1066] = "Referenzprojekt Zapfprofil Mischnutzung",
            [1067] = "Referenzprojekt Zapfprofil Kalender",
            [1068] = "Referenzprojekt Zapfprofil Stochastisch",
            [1069] = "Referenzprojekt Zapfprofil Typtage",
            [1070] = "Referenzprojekt Zapfprofil Kalibrierung",
            [1071] = "Referenzprojekt Zapfprofil Auslegung",
        };

        /// <summary>
        /// Die Jahressummen der Generator-Bilanz je Projekt [kWh/a] — Zapfung und Zirkulation — und je Zone (Name, Zapfung,
        /// Zirkulation), wie sie die Basis R53 einfror.
        /// </summary>
        private static readonly Dictionary<int, (double Zapfung, double Zirkulation, (string Zone, double Zapfung, double Zirkulation)[] Zonen)> Bilanzen =
            new Dictionary<int, (double, double, (string, double, double)[])>
            {
                [1066] = (55854.23498666664, 6306.172687154715, new[]
                {
                    ("Wohnen", 9651.31831999988, 1362.3137735757325),
                    ("Büro", 5961.666666666669, 841.5079001837256),
                    ("Hotel", 29063.125000000222, 4102.351013395854),
                    ("Schule", 11178.125000000042, 0.0),
                }),
                [1067] = (19375.416666666795, 0.0, new[]
                {
                    ("Schule", 14904.16666666681, 0.0),
                    ("Büro", 4471.2499999999745, 0.0),
                }),
                [1068] = (5006.621378499975, 0.0, new[] { ("Wohnen", 5006.621378499975, 0.0) }),
                [1069] = (5006.62137850001, 0.0, new[] { ("Wohnen", 5006.62137850001, 0.0) }),
                [1070] = (12474.999999999989, 4672.000000000514, new[]
                {
                    ("Wohnen gemessen", 7000.000000000008, 2449.0891096084806),
                    ("Gewerbe manuell", 5474.999999999917, 2222.91089039207),
                }),
                [1071] = (26104.320025000023, 4204.999999999333, new[]
                {
                    ("Speicher", 12667.355294999998, 2780.5048859938124),
                    ("Frischwasserstation", 6489.679560000048, 1424.4951140063051),
                    ("Durchfluss", 2121.6260099999986, 0.0),
                    ("Wohnungsstation", 4825.65915999994, 0.0),
                }),
            };

        private readonly TestDatenbank _db;

        public ZapfprofilOptionenReferenzprojekteWacheTests(TestDatenbank db) { _db = db; }

        public static IEnumerable<object[]> Projektnummern() => Projekte.Keys.Select(p => new object[] { p });

        // =================================================================================
        // Die Kopien
        // =================================================================================

        /// <summary>
        /// Die sechs Projekte stehen als Kopien von 1045: Name, Beschreibung, Kosten nicht geändert, die Kopie derselben Klimaregion,
        /// genau ein Gebäude mit dem Namen des Gebäudes von 1045 — und alle rechnen über den Generator.
        /// </summary>
        [Fact]
        public void Die_sechs_Projekte_sind_Kopien_von_1045_auf_dem_Generator()
        {
            if (!_db.Vorhanden) return;

            string klima = "SELECT k.Bezeichner FROM Tab_Projekt p JOIN Tab_Klimaregion k ON k.ID = p.ID_Klimaregion WHERE p.ID = ?";
            object vorlageKlima = EineZeile(klima, VORLAGE)[0];
            string gebaeudeName = Convert.ToString(EineZeile("SELECT Gebaeudename FROM Tab_Gebaeude WHERE ID_Projekt = ?", VORLAGE)[0],
                                                   CultureInfo.InvariantCulture);
            foreach (KeyValuePair<int, string> p in Projekte)
            {
                DataRow r = EineZeile("SELECT Projektname, Beschreibung, Kosten_Geaendert FROM Tab_Projekt WHERE ID = ?", p.Key);
                Assert.Equal(p.Value, Convert.ToString(r["Projektname"], CultureInfo.InvariantCulture));
                Assert.StartsWith(p.Value + ": Kopie von Projekt 1045", Convert.ToString(r["Beschreibung"], CultureInfo.InvariantCulture));
                Assert.Equal(DBNull.Value, r["Kosten_Geaendert"]);
                Assert.Equal(vorlageKlima, EineZeile(klima, p.Key)[0]);
                Assert.Equal(gebaeudeName, Convert.ToString(EineZeile("SELECT Gebaeudename FROM Tab_Gebaeude WHERE ID_Projekt = ?", p.Key)[0],
                                                            CultureInfo.InvariantCulture));
                Assert.Equal(BrauchwasserWeg.Generator, ZapfprofilCtrl.Weg(p.Key));
            }
        }

        // =================================================================================
        // Die gesäten Eingaben
        // =================================================================================

        /// <summary>Die Projektzeile je Projekt — jede Spalte, auch die leeren.</summary>
        [Theory]
        [MemberData(nameof(Projektnummern))]
        public void Die_Projektzeile_steht_wie_gesaet(int projekt)
        {
            if (!_db.Vorhanden) return;

            var gesetzt = new Dictionary<string, object>
            {
                ["Weg"] = TwwSchema.WEG_GENERATOR,
                ["Jahresreihe_Stochastisch"] = projekt == 1068 ? 1L : 0L,
                ["Seed"] = (long)projekt,
                ["Realisierungen"] = 10L,
                ["Perzentil"] = 99L,
                ["Zirk_Auto"] = projekt == 1070 ? 0L : 1L,
                ["Zirk_Methode"] = projekt == 1066 ? 1L : 3L,
                ["Lade_Auto"] = 1L,
                ["Speicherart"] = 1L,
                ["Personen_Auto"] = 1L,
                ["Typtage_Aktiv"] = projekt == 1069 ? 1L : 0L,
                ["Aenderungsdatum"] = new DateTime(2026, 10, 13),
            };
            switch (projekt)
            {
                case 1066:
                    gesetzt["Zirk_Laenge_m"] = 120.0;
                    gesetzt["Zirk_Verlust_W_m"] = 10.0;
                    gesetzt["Zirk_Laufzeit_h"] = 18.0;
                    break;
                case 1069:
                    gesetzt["Typtage_Klimazone"] = 4L;
                    gesetzt["Typtage_Gebaeudeart"] = "referenzhaus";
                    break;
                case 1070:
                    gesetzt["Zirk_Laufzeit_h"] = 16.0;
                    gesetzt["Zirk_Manuell_Kw"] = 0.8;
                    break;
                case 1071:
                    gesetzt["Bedarfstag_Quelle"] = (long)ZapfBedarfstagquelle.Ecodesign;
                    gesetzt["ID_Bedarfstag"] = Zahl("SELECT ID FROM Tab_TwwBedarfstag_STAMM WHERE Bezeichner = ? AND Katalogversion = 'TEST-1'",
                                                    "Ecodesign-Zapfprofil L");
                    gesetzt["Erzeugerart"] = (long)ZapfErzeugerart.Waermepumpe;
                    gesetzt["Uebertrager_Werkstoff"] = (long)ZapfUebertragerwerkstoff.Edelstahl;
                    break;
            }
            DataRow r = EineZeile("SELECT * FROM Tab_TwwProjekt WHERE ID_Projekt = ?", projekt);
            ZeileGleich(r, gesetzt, "ID", "ID_Projekt");
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_TwwKonstruktorzeile WHERE ID_TwwProjekt = ?", Ganz(r, "ID")));
        }

        /// <summary>
        /// Die Zonen je Projekt in ihrer Reihenfolge — jede Spalte, auch die leeren; die Nutzungsart über Bezeichner und
        /// Katalogversion, das gebundene Gebäude als das eine Gebäude des Projekts.
        /// </summary>
        [Theory]
        [MemberData(nameof(Projektnummern))]
        public void Die_Zonen_stehen_wie_gesaet(int projekt)
        {
            if (!_db.Vorhanden) return;

            long gebaeude = Zahl("SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?", projekt);
            List<Dictionary<string, object>> soll = Zonen(projekt, gebaeude);
            DataTable t = DataRepository.GetDataTable("SELECT * FROM Tab_TwwZone WHERE ID_Projekt = ? ORDER BY Reihenfolge",
                                                      new DbParam("@p", projekt));
            Assert.Equal(soll.Count, t.Rows.Count);
            for (int i = 0; i < soll.Count; i++)
            {
                soll[i]["Reihenfolge"] = (long)(i + 1);
                ZeileGleich(t.Rows[i], soll[i], "ID", "ID_Projekt");
            }
        }

        /// <summary>
        /// Die Wohnungstabellen: 1066 Wohnen mit Belegung nach Raumzahl und der Ausstattung „Testklasse A", 1071 vier Zonen
        /// mit Wohnungstabelle (eine davon mit eigener Personenzahl); alle übrigen Zonen ohne Wohnungstypen.
        /// </summary>
        [Fact]
        public void Die_Wohnungstabellen_stehen_wie_gesaet()
        {
            if (!_db.Vorhanden) return;

            var soll = new Dictionary<(int, string), string>
            {
                [(1066, "Wohnen")] = "4|2|-|Testklasse A; 2|3|-|Testklasse A; 3|4|-|Testklasse A",
                [(1071, "Speicher")] = "6|3|-|-; 4|4|-|-",
                [(1071, "Frischwasserstation")] = "8|2|-|-",
                [(1071, "Durchfluss")] = "1|4|-|-",
                [(1071, "Wohnungsstation")] = "4|3|2|-",
            };
            DataTable t = DataRepository.GetDataTable(
                "SELECT z.ID_Projekt, z.Name, w.Anzahl, w.Raumzahl, w.Personen, a.Schluessel FROM Tab_TwwWohnungstyp w " +
                "JOIN Tab_TwwZone z ON z.ID = w.ID_Zone LEFT JOIN Tab_TwwDin4708Wert_STAMM a ON a.ID = w.ID_Ausstattung " +
                "WHERE z.ID_Projekt BETWEEN 1066 AND 1071 ORDER BY z.ID_Projekt, z.Reihenfolge, w.Reihenfolge");
            var ist = new Dictionary<(int, string), List<string>>();
            foreach (DataRow r in t.Rows)
            {
                var k = ((int)Ganz(r, "ID_Projekt"), Convert.ToString(r["Name"], CultureInfo.InvariantCulture));
                if (!ist.TryGetValue(k, out List<string> l)) ist[k] = l = new List<string>();
                l.Add(Ganz(r, "Anzahl") + "|" + Text(r["Raumzahl"]) + "|" + Text(r["Personen"]) + "|" + Text(r["Schluessel"]));
            }
            Assert.Equal(soll.Keys.OrderBy(k => k).ToArray(), ist.Keys.OrderBy(k => k).ToArray());
            foreach (KeyValuePair<(int, string), string> s in soll)
                Assert.Equal(s.Value, string.Join("; ", ist[s.Key]));
        }

        /// <summary>
        /// Der Kalender von 1067: die Schulzone mit fünf eigenen Ferienzeiträumen (Weihnachten über den Jahreswechsel), die
        /// Bürozone am Gebäude mit Feiertagsland Bayern und Wochenmaske Sa + So; kein anderes Projekt der sechs trägt Ferien.
        /// </summary>
        [Fact]
        public void Die_Ferien_und_der_Gebaeudekalender_von_1067_stehen_wie_gesaet()
        {
            if (!_db.Vorhanden) return;

            DataTable t = DataRepository.GetDataTable(
                "SELECT z.ID_Projekt, z.Name AS Zone, f.Reihenfolge, f.Name, f.Beginn, f.Ende FROM Tab_TwwZonenferien f " +
                "JOIN Tab_TwwZone z ON z.ID = f.ID_Zone WHERE z.ID_Projekt BETWEEN 1066 AND 1071 ORDER BY z.ID_Projekt, f.Reihenfolge");
            string ist = string.Join("; ", t.Rows.Cast<DataRow>().Select(r =>
                Ganz(r, "ID_Projekt") + " " + r["Zone"] + " " + Ganz(r, "Reihenfolge") + " " + r["Name"] + " " + Ganz(r, "Beginn") + "-" + Ganz(r, "Ende")));
            Assert.Equal("1067 Schule 1 Weihnachten 355-6; 1067 Schule 2 Winter 44-48; 1067 Schule 3 Ostern 90-103; " +
                         "1067 Schule 4 Pfingsten 146-151; 1067 Schule 5 Sommer 205-247", ist);

            DataRow g = EineZeile("SELECT Feiertagsland, Wochenendtage FROM Tab_Gebaeude WHERE ID_Projekt = ?", 1067);
            Assert.Equal("BY", Convert.ToString(g["Feiertagsland"], CultureInfo.InvariantCulture));
            Assert.Equal(96L, Ganz(g, "Wochenendtage"));
            foreach (int p in Projekte.Keys.Where(p => p != 1067))
            {
                DataRow a = EineZeile("SELECT Feiertagsland, Wochenendtage FROM Tab_Gebaeude WHERE ID_Projekt = ?", p);
                Assert.Equal(DBNull.Value, a["Feiertagsland"]);
                Assert.Equal(DBNull.Value, a["Wochenendtage"]);
            }
        }

        /// <summary>
        /// Der eingespielte Typtagsatz ist der erfundene des Saatskripts: Quelle „Referenztyptage (fiktiv)", eine Klimazone,
        /// eine Gebäudeart, sechs Kategorien ohne Bewölkung mit 365 Kalendertagen und Σ n·F = 0, drei Kennwerte, keine
        /// Tagesgänge.
        /// </summary>
        [Fact]
        public void Der_erfundene_Typtagsatz_steht_wie_gesaet()
        {
            if (!_db.Vorhanden) return;

            TwwTyptagstand stand = TwwTyptagCtrl.Stand();
            Assert.True(stand.Vorhanden);
            Assert.Equal(33, stand.Zeilen);
            Assert.Equal(new[] { 4 }, stand.Klimazonen.ToArray());
            Assert.Equal(new[] { "R01", "R02", "R03", "R04", "R05", "R06" }, stand.Typtage.ToArray());
            Assert.Equal(new[] { "referenzhaus" }, stand.Gebaeudearten.ToArray());
            Assert.False(stand.MitTagesgaengen);
            Assert.Equal("Referenztyptage (fiktiv)", stand.Quelle);
            Assert.Equal("2026-10", stand.Ausgabe);
            Assert.Equal("2026-10-13", stand.DatumImport);

            var anzahl = new Dictionary<string, int> { ["R01"] = 90, ["R02"] = 20, ["R03"] = 100, ["R04"] = 25, ["R05"] = 105, ["R06"] = 25 };
            var faktor = new Dictionary<string, double>
            {
                ["R01"] = 0.00005, ["R02"] = -0.0001, ["R03"] = -0.0001, ["R04"] = -0.0002, ["R05"] = 0.0001, ["R06"] = 0.00008
            };
            DataTable t = DataRepository.GetDataTable("SELECT Art, Klimazone, Gebaeudeart, Typtag, Wert FROM Tab_TwwTyptag_IMPORT " +
                                                      "WHERE Art IN ('ANZAHL','FAKTOR') ORDER BY Art, Typtag");
            Assert.Equal(12, t.Rows.Count);
            double pruefsumme = 0;
            foreach (DataRow r in t.Rows)
            {
                Assert.Equal(4L, Ganz(r, "Klimazone"));
                Assert.Equal("referenzhaus", Convert.ToString(r["Gebaeudeart"], CultureInfo.InvariantCulture));
                string tt = Convert.ToString(r["Typtag"], CultureInfo.InvariantCulture);
                double w = Kommazahl(r, "Wert");
                if (Convert.ToString(r["Art"], CultureInfo.InvariantCulture) == "ANZAHL") Assert.Equal(anzahl[tt], w);
                else { Assert.Equal(faktor[tt], w); pruefsumme += anzahl[tt] * w; }
            }
            Assert.Equal(365, anzahl.Values.Sum());
            Assert.True(Math.Abs(pruefsumme) < 1e-12, "Σ n·F = " + pruefsumme.ToString("R", CultureInfo.InvariantCulture));

            var kennwerte = DataRepository.GetDataTable("SELECT Typtag, Wert FROM Tab_TwwTyptag_IMPORT WHERE Art = 'KENNWERT' ORDER BY Typtag")
                                          .Rows.Cast<DataRow>().ToDictionary(r => Convert.ToString(r["Typtag"], CultureInfo.InvariantCulture),
                                                                             r => Kommazahl(r, "Wert"));
            Assert.Equal(new Dictionary<string, double> { ["heizgrenze.referenzhaus"] = 16.0, ["pruefsumme.toleranz"] = 1e-9, ["wintergrenze"] = 2.0 },
                         kennwerte);
        }

        // =================================================================================
        // Die Rechnung
        // =================================================================================

        /// <summary>
        /// <b>Die Generator-Bilanz je Projekt</b> rechnet wie die Basis: ohne Abbruch und ohne abgelehnte Zone, die
        /// Jahressummen von Zapfung und Zirkulation je Projekt und Zone auf 1e‑6 genau und die Stundenreihe zeichengleich
        /// mit <c>waermebedarf_brauchwasser.csv</c> der aktuellen Basis.
        /// </summary>
        [Theory]
        [MemberData(nameof(Projektnummern))]
        public void Die_Generator_Bilanz_rechnet_wie_die_Basis(int projekt)
        {
            if (!_db.Vorhanden) return;

            var pc = new ProjektCtrl();
            pc.ReadSingle(projekt);
            var sim = new SimulationWaermebedarf();
            sim.Waermebedarf_berechnen(projekt, pc.m_ID_Klimaregion);
            Assert.True(string.IsNullOrEmpty(sim.Fehlertext), "Der Generatorweg brach ab: " + sim.Fehlertext);
            ZapfprofilErgebnis e = sim.Zapfprofil;
            Assert.NotNull(e);
            Assert.Empty(e.Ablehnungen);
            Assert.Equal(projekt == 1068, e.Stochastisch);

            var soll = Bilanzen[projekt];
            Gleich(soll.Zapfung, e.Zapfung.JahressummeKwh, "Zapfung");
            Gleich(soll.Zirkulation, e.Zirkulation.JahressummeKwh, "Zirkulation");
            Assert.Equal(soll.Zonen.Select(z => z.Zone).ToArray(), e.JeZone.Select(z => z.Zone).ToArray());
            foreach (var (zone, zapfung, zirkulation) in soll.Zonen)
            {
                ZonenErgebnis z = e.JeZone.Single(x => x.Zone == zone);
                Assert.False(z.Abgelehnt, zone);
                Gleich(zapfung, z.JahresbedarfZapfungKwh, zone + " Zapfung");
                Gleich(zirkulation, z.JahresverlustZirkulationKwh, zone + " Zirkulation");
            }

            // Die Merkmale der Optionen am Ergebnis.
            if (projekt == 1067)
            {
                Assert.Equal(85, e.JeZone.Single(z => z.Zone == "Schule").Kalender.Count(t => t == ZapfTagtyp.Ruhetag));
                Assert.Equal(0, e.JeZone.Single(z => z.Zone == "Büro").Kalender.Count(t => t == ZapfTagtyp.Ruhetag));
            }
            if (projekt == 1070)
            {
                Gleich(1.1604632267480741, e.JeZone.Single(z => z.Zone == "Wohnen gemessen").Kalibrierfaktor ?? double.NaN, "Kalibrierfaktor");
                Assert.Null(e.JeZone.Single(z => z.Zone == "Gewerbe manuell").Kalibrierfaktor);
                Gleich(0.8 * 16.0 * 365.0, e.Zirkulation.JahressummeKwh, "Zirkulation manuell");
            }

            string[] basis = BasisReihe(projekt, "waermebedarf_brauchwasser.csv");
            Assert.Equal("Index;Wert", basis[0]);
            Assert.Equal(8760, basis.Length - 1);
            for (int h = 0; h < 8760; h++)
            {
                string ist = h.ToString(CultureInfo.InvariantCulture) + ";" + ZahlText(sim.brauchwasserwerte[h]);
                Assert.True(basis[h + 1] == ist, $"Projekt {projekt}, Stunde {h}: Basis '{basis[h + 1]}', Lauf '{ist}'");
            }
        }

        /// <summary>
        /// <b>Die Auslegung von 1071, eingefroren</b> (stochastisch gerechnet, damit auch das Perzentil steht): vier
        /// Topologiegruppen mit dem Ecodesign-Tag, keine Ablehnung, Erzeugerart Wärmepumpe; je Gruppe die Empfehlung
        /// (Verfahren, Volumen, Leistung, Nenninhalt) und der Vergleich der Verfahren (Stand, Volumen, Leistung, empfohlen).
        /// </summary>
        [Fact]
        public void Die_Auslegung_von_1071_steht_wie_eingefroren()
        {
            if (!_db.Vorhanden) return;

            Assert.True(ZapfprofilCtrl.KalenderLesen(1071, out int w0, out bool[] we));
            Auslegungsrechnung r = ZapfprofilCtrl.Auslegung(1071, ZapfprofilCtrl.Lies(1071), w0, we,
                                                            new Auslegungslauf(null, null, Stochastisch: true));
            Assert.Equal(ZapfErzeugerart.Waermepumpe, r.Erzeugerart);
            Assert.Empty(r.Ergebnis.Ablehnungen);

            var soll = new (ZapfTopologie Topologie, ZapfAuslegungsverfahren Verfahren, double? V, double? P, double? Nenn, string Vergleich)[]
            {
                (ZapfTopologie.Speicher, ZapfAuslegungsverfahren.Summenlinie, 1175.805261677388, 6.633992207622741, 1500.0,
                 "Summenlinie Gerechnet 1175.805261677388 6.633992207622741 empfohlen; Perzentil Gerechnet 463.7980791914722 6.633992207622741; " +
                 "Normvergleich Gerechnet 246.11641640621195 -"),
                (ZapfTopologie.Frischwasserstation, ZapfAuslegungsverfahren.Minutenspitze, null, 173.04, null,
                 "Minutenspitze Gerechnet - 173.04 empfohlen; Perzentil Gerechnet - 77.2825417119427; Normvergleich AusserhalbGueltigkeit - -"),
                (ZapfTopologie.Durchfluss, ZapfAuslegungsverfahren.Minutenspitze, null, 21.63, null,
                 "Minutenspitze Gerechnet - 21.63 empfohlen; Perzentil Gerechnet - 59.55693910735072; Normvergleich AusserhalbGueltigkeit - -"),
                (ZapfTopologie.Wohnungsstation, ZapfAuslegungsverfahren.Minutenspitze, null, 86.52, null,
                 "Minutenspitze Gerechnet - 86.52 empfohlen; Perzentil Gerechnet - 72.89569350482742; Normvergleich AusserhalbGueltigkeit - -"),
            };
            Assert.Equal(soll.Select(s => s.Topologie).ToArray(), r.Ergebnis.Gruppen.Select(g => g.Topologie).ToArray());
            foreach (var s in soll)
            {
                Auslegungsgruppe g = r.Ergebnis.Gruppen.Single(x => x.Topologie == s.Topologie);
                Assert.Equal(ZapfBedarfstagquelle.Ecodesign, g.Bedarfstagwahl?.Quelle);
                Auslegungsempfehlung em = g.Empfehlung;
                Assert.True(em.Rechenbar, s.Topologie + ": " + em.GrundText);
                Assert.Equal(s.Verfahren, em.Verfahren);
                Gleich(s.V, em.VolumenL, s.Topologie + " Volumen");
                Gleich(s.P, em.LeistungKw, s.Topologie + " Leistung");
                Gleich(s.Nenn, em.NenninhaltL, s.Topologie + " Nenninhalt");

                // Der Vergleich der Verfahren — gerundet auf zehn signifikante Stellen, damit nur eine echte Änderung auffällt.
                Assert.Equal(Gerundet(s.Vergleich), string.Join("; ", g.Dreiergruppe.Select(w =>
                    w.Verfahren + " " + w.Status + " " + Zahltext10(w.VolumenL) + " " + Zahltext10(w.LeistungKw) + (w.Empfohlen ? " empfohlen" : ""))));
            }
        }

        // =================================================================================
        // Die Sollzonen
        // =================================================================================

        private static List<Dictionary<string, object>> Zonen(int projekt, long gebaeude)
        {
            switch (projekt)
            {
                case 1066:
                    return new List<Dictionary<string, object>>
                    {
                        Zone(WOHNEN, "Wohnen", 16.0, gebaeude),
                        Zone(BUERO, "Büro", 40.0),
                        Zone(HOTEL, "Hotel", 20.0),
                        Zone(SCHULE, "Schule", 150.0, zirkulation: false),
                    };
                case 1067:
                    return new List<Dictionary<string, object>>
                    {
                        Zone(SCHULE, "Schule", 200.0, zirkulation: false),
                        Zone(BUERO, "Büro", 30.0, gebaeude, zirkulation: false),
                    };
                case 1068:
                case 1069:
                    return new List<Dictionary<string, object>> { Zone(WOHNEN, "Wohnen", 8.3, gebaeude, zirkulation: false) };
                case 1070:
                    var gemessen = Zone(WOHNEN, "Wohnen gemessen", 10.0, gebaeude);
                    gemessen["Jahresmesswert"] = 7000.0;
                    gemessen["Jahresmesswert_Einheit"] = 1L;
                    gemessen["Jahresmesswert_Bilanzgrenze"] = 1L;
                    gemessen["Jahresmesswert_Quelle"] = "Ablesung (fiktiv)";
                    gemessen["Jahresmesswert_Zeitraum"] = "2025";
                    var manuell = Zone(GEWERBE, "Gewerbe manuell", 20.0);
                    manuell["Tagesbedarf_Auto"] = 0L;
                    manuell["Tagesbedarf_Manuell_Kwh"] = 15.0;
                    manuell["Zapftemperatur"] = 55.0;
                    manuell["Kaltwasser_Mittel"] = 12.0;
                    manuell["Kaltwasser_Amplitude"] = 4.0;
                    double[] auslastung = { 1.0, 1.0, 1.0, 1.0, 1.0, 0.8, 0.6, 0.5, 1.0, 1.0, 1.0, 0.7 };
                    for (int m = 0; m < 12; m++) manuell["Auslastung_" + (m + 1).ToString("00", CultureInfo.InvariantCulture)] = auslastung[m];
                    return new List<Dictionary<string, object>> { gemessen, manuell };
                default: // 1071
                    return new List<Dictionary<string, object>>
                    {
                        Zone(WOHNEN, "Speicher", 21.0, gebaeude, topologie: ZapfTopologie.Speicher),
                        Zone(STUDENT, "Frischwasserstation", 8.0, topologie: ZapfTopologie.Frischwasserstation),
                        Zone(EFH, "Durchfluss", 3.0, topologie: ZapfTopologie.Durchfluss, zirkulation: false),
                        Zone(WOHNEN, "Wohnungsstation", 8.0, topologie: ZapfTopologie.Wohnungsstation, zirkulation: false),
                    };
            }
        }

        private static Dictionary<string, object> Zone(string nutzungsart, string name, double bezugsmenge, long? gebaeude = null,
                                                       ZapfTopologie topologie = ZapfTopologie.Speicher, bool zirkulation = true)
        {
            var z = new Dictionary<string, object>
            {
                ["ID_Nutzungsart"] = Zahl("SELECT ID FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = ? AND Katalogversion = 'TEST-1'", nutzungsart),
                ["Name"] = name,
                ["Bezugsmenge"] = bezugsmenge,
                ["Niveau"] = 2L,
                ["Topologie"] = (long)topologie,
                ["Zirkulation"] = zirkulation ? 1L : 0L,
                ["Tagesbedarf_Auto"] = 1L,
            };
            if (gebaeude.HasValue) z["ID_Gebaeude"] = gebaeude.Value;
            return z;
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        private static void Gleich(double? soll, double? ist, string was)
        {
            if (!soll.HasValue) { Assert.True(!ist.HasValue, was + ": erwartet leer, steht " + ist); return; }
            Assert.True(ist.HasValue, was + ": erwartet " + soll + ", steht leer");
            Assert.True(Math.Abs(ist.Value - soll.Value) <= 1e-6 * Math.Max(1.0, Math.Abs(soll.Value)),
                string.Format(CultureInfo.InvariantCulture, "{0}: {1:R} statt {2:R}", was, ist.Value, soll.Value));
        }

        /// <summary>Eine Zahl auf zehn signifikante Stellen (InvariantCulture); leer als „-".</summary>
        private static string Zahltext10(double? d) => d.HasValue ? d.Value.ToString("G10", CultureInfo.InvariantCulture) : "-";

        /// <summary>Die Zahlen eines Solltexts auf zehn signifikante Stellen.</summary>
        private static string Gerundet(string text)
            => Regex.Replace(text, @"-?\d+\.\d+", m => double.Parse(m.Value, CultureInfo.InvariantCulture).ToString("G10", CultureInfo.InvariantCulture));

        private static long Zahl(string sql, object wert)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, new DbParam("@p", wert)), CultureInfo.InvariantCulture);

        private static DataRow EineZeile(string sql, object wert)
        {
            DataTable t = DataRepository.GetDataTable(sql, new DbParam("@p", wert));
            Assert.True(t != null && t.Rows.Count == 1, "Erwartet genau eine Zeile: " + sql + " (" + wert + ")");
            return t.Rows[0];
        }

        private static long Ganz(DataRow r, string spalte) => Convert.ToInt64(r[spalte], CultureInfo.InvariantCulture);

        private static double Kommazahl(DataRow r, string spalte) => Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture);

        private static string Text(object o)
            => o == null || o == DBNull.Value ? "-"
             : o is double d ? d.ToString("R", CultureInfo.InvariantCulture)
             : Convert.ToString(o, CultureInfo.InvariantCulture);

        /// <summary>Jede gesetzte Spalte trägt ihren Wert, jede übrige (außer <paramref name="ohne"/>) NULL.</summary>
        private static void ZeileGleich(DataRow r, IReadOnlyDictionary<string, object> gesetzt, params string[] ohne)
        {
            foreach (DataColumn c in r.Table.Columns)
            {
                if (ohne.Contains(c.ColumnName)) continue;
                object ist = r[c];
                if (!gesetzt.TryGetValue(c.ColumnName, out object soll))
                {
                    Assert.True(ist == null || ist == DBNull.Value, $"{c.ColumnName}: erwartet NULL, steht '{ist}'.");
                    continue;
                }
                Assert.False(ist == null || ist == DBNull.Value, $"{c.ColumnName}: erwartet '{soll}', steht NULL.");
                bool gleich = soll switch
                {
                    string s => s == Convert.ToString(ist, CultureInfo.InvariantCulture),
                    double d => d == Convert.ToDouble(ist, CultureInfo.InvariantCulture),
                    long l => l == Convert.ToInt64(ist, CultureInfo.InvariantCulture),
                    DateTime dt => dt == Convert.ToDateTime(ist, CultureInfo.InvariantCulture),
                    _ => Equals(soll, ist)
                };
                Assert.True(gleich, $"{c.ColumnName}: erwartet '{soll}', steht '{ist}'.");
            }
        }

        /// <summary>Die Schreibweise des Referenzlaufs (<c>Ergebnisexport.Zahl</c>).</summary>
        private static string ZahlText(double d)
        {
            if (double.IsNaN(d)) return "NaN";
            if (double.IsPositiveInfinity(d)) return "Inf";
            if (double.IsNegativeInfinity(d)) return "-Inf";
            return d.ToString("G9", CultureInfo.InvariantCulture);
        }

        /// <summary>Die Zeilen einer Datei eines Projekts in der <b>aktuellen</b> Basis.</summary>
        private static string[] BasisReihe(int projekt, string datei)
        {
            string[] basen = Directory.GetDirectories(Path.Combine(Wurzel(), "Referenzlaeufe"))
                .Where(o => Regex.IsMatch(Path.GetFileName(o), @"^\d{4}-\d{2}-\d{2}_R\d+_"))
                .ToArray();
            Assert.True(basen.Length == 1,
                "Unter Referenzlaeufe/ liegt nicht genau eine Basis: " + string.Join(", ", basen));

            string pfad = Path.Combine(basen[0], "Projekt_" + projekt, datei);
            Assert.True(File.Exists(pfad), "Die Basis führt " + pfad + " nicht.");
            return File.ReadAllLines(pfad);
        }

        private static string Wurzel()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "EPOS.Kern", "EPOS.Kern.csproj")))
                d = d.Parent;
            Assert.True(d != null, "Die Repowurzel ist vom Ausgabeordner aus nicht zu finden.");
            return d.FullName;
        }
    }
}

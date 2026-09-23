using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E8c (E8b‑Q3, Anwenderentscheid 23.09.2026: Lesart b) — <b>die Probe der
    /// Betriebskostengliederung vergleicht nur die Positionen des ersten Jahres.</b>
    ///
    /// <para><b>Der Befund.</b> Wort- und Tabellenbericht halten die Summe der
    /// Betriebskostenpositionen gegen die angesetzten Betriebskosten p. a. und warnen bei
    /// einer Differenz „die Gliederung ist unvollständig". Die angesetzten Betriebskosten
    /// sind aber die Jahr-1-Zahl der Rechnung; eine Position mit späterem Startjahr (KD6)
    /// steht in der Tabelle, zahlt jedoch erst ab ihrem Jahr. Im Prüffall „hybtest" (Wartung
    /// BHKW 1.800 €, Wartung Kessel 600 € ab Jahr 6) warnte der Bericht deshalb 2.400 gegen
    /// 1.800 €, obwohl nichts fehlte.</para>
    ///
    /// <para><b>Die Lösung.</b> Die Position trägt ihr Startjahr
    /// (<c>KostenPositionNachweis.StartJahr</c>, Nachweisumschlag Fassung 9); die Probe
    /// summiert nur die Positionen des ersten Jahres, und die Herleitungsspalte nennt „ab
    /// Jahr X". Eine echte Lücke — hier eine Position der Rechnung, deren Kostenart in keinem
    /// Block der Tabelle steht — warnt weiterhin.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BetriebskostenStartjahrGliederungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        private const int BHKW = 7;              // Tab_KostenKomponente.ID
        private const int VARIANTE_A = 1041;     // ohne eigene Betriebszeilen in der Testdatenbank

        // =====================================================================
        //  Die Probe — rein
        // =====================================================================

        /// <summary>
        /// Der Befund in Zahlen: Über ALLE Positionen (2.400 €) warnte die Probe gegen die
        /// Betriebskosten p. a. (1.800 €); über die Positionen des ersten Jahres geht sie auf.
        /// </summary>
        [Fact]
        public void Die_Probe_vergleicht_nur_die_Positionen_des_ersten_Jahres()
        {
            var positionen = new List<KostenPositionNachweis>
            {
                Fest("Wartung BHKW", 1800.0, null),
                Fest("Wartung Kessel", 600.0, 6),
            };
            double alle = positionen.Sum(x => x.BetragJahr);
            double erstesJahr = positionen.Where(WirtschaftlichkeitZeilen.LaeuftImErstenJahr)
                                          .Sum(x => x.BetragJahr);

            Assert.Equal(2400.0, alle, 9);
            Assert.Equal(1800.0, erstesJahr, 9);
            Assert.NotEqual("", WirtschaftlichkeitZeilen.GliederungAbweichung(alle, 1800.0, DE));
            Assert.Equal("", WirtschaftlichkeitZeilen.GliederungAbweichung(erstesJahr, 1800.0, DE));
        }

        /// <summary>
        /// Eine echte Lücke warnt weiterhin — mit der Summe des ersten Jahres und der
        /// angesetzten Zahl; die Toleranz bleibt 0,50 €, ohne Vergleichszahl gibt es keine
        /// Probe.
        /// </summary>
        [Fact]
        public void Eine_echte_Luecke_warnt_weiterhin()
        {
            Assert.Equal(
                "⚠ Die Summe der Positionen des ersten Jahres (1.800,00) weicht von den " +
                "angesetzten Betriebskosten (2.300,00) ab — die Gliederung ist unvollständig.",
                WirtschaftlichkeitZeilen.GliederungAbweichung(1800.0, 2300.0, DE));

            Assert.Equal("", WirtschaftlichkeitZeilen.GliederungAbweichung(1800.0, 1800.5, DE));
            Assert.NotEqual("", WirtschaftlichkeitZeilen.GliederungAbweichung(1800.0, 1800.51, DE));
            Assert.Equal("", WirtschaftlichkeitZeilen.GliederungAbweichung(1800.0, null, DE));

            using (new Kulturvorrichtung("en-US"))
                Assert.Equal(
                    "⚠ The sum of the first-year items (1,800.00) differs from the operating " +
                    "cost applied (2,300.00) — the breakdown is incomplete.",
                    WirtschaftlichkeitZeilen.GliederungAbweichung(1800.0, 2300.0,
                        CultureInfo.GetCultureInfo("en-US")));
        }

        /// <summary>
        /// Die Grenze ist die der Summenschleife: Startjahr ≥ 2 zahlt später, ohne Startjahr,
        /// mit 0 oder 1 zahlt die Position ab dem ersten Jahr.
        /// </summary>
        [Theory]
        [InlineData(null, true)]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        [InlineData(6, false)]
        public void Das_erste_Jahr_endet_wo_die_Summenschleife_es_enden_laesst(int? start, bool imErstenJahr)
        {
            Assert.Equal(imErstenJahr, WirtschaftlichkeitZeilen.LaeuftImErstenJahr(Fest("X", 1.0, start)));
        }

        /// <summary>
        /// Die Herleitungsspalte nennt das Startjahr — an der festen, der bemessenen und der
        /// szenariogepflegten Position; ohne Startjahr bleibt sie, wie sie war.
        /// </summary>
        [Fact]
        public void Die_Herleitungsspalte_nennt_das_Startjahr()
        {
            Assert.Equal("ab Jahr 6", WirtschaftlichkeitZeilen.HerleitungZeile(Fest("K", 600.0, 6), DE));
            Assert.Equal("", WirtschaftlichkeitZeilen.HerleitungZeile(Fest("K", 600.0, null), DE));

            var bemessen = new KostenPositionNachweis
            {
                Bemessung = DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, Komponente = BHKW,
                Menge = 300.0, Einheitpreis = 12.0, BetragJahr = 3600.0, StartJahr = 6
            };
            Assert.Equal("300,00 kW × 12,000 €/kW·a · ab Jahr 6",
                         WirtschaftlichkeitZeilen.HerleitungZeile(bemessen, DE));
            bemessen.StartJahr = null;
            Assert.Equal("300,00 kW × 12,000 €/kW·a", WirtschaftlichkeitZeilen.HerleitungZeile(bemessen, DE));

            var szenario = Fest("S", 700.0, 4);
            szenario.SzenarioGepflegt = true;
            Assert.Equal(R.WIRT_BK_SZENARIOWERT + " · ab Jahr 4",
                         WirtschaftlichkeitZeilen.HerleitungZeile(szenario, DE));

            using (new Kulturvorrichtung("en-US"))
                Assert.Equal("from year 6", WirtschaftlichkeitZeilen.HerleitungZeile(
                    Fest("K", 600.0, 6), CultureInfo.GetCultureInfo("en-US")));
        }

        /// <summary>
        /// Der Nachweisumschlag (Fassung 9) trägt das Startjahr hin und zurück und schreibt es
        /// nur, wo es gesetzt ist; ein Umschlag der Fassung 8 liest sich „ab dem ersten Jahr".
        /// </summary>
        [Fact]
        public void Der_Umschlag_traegt_das_Startjahr()
        {
            var e = new WirtschaftlichkeitErgebnis();
            e.Betriebskosten.Add(Fest("Wartung BHKW", 1800.0, null));
            e.Betriebskosten.Add(Fest("Wartung Kessel", 600.0, 6));

            string grund;
            string text = ErgebnisNachweisUmschlag.Schreiben(e, out grund);
            Assert.Null(grund);
            Assert.Contains("\"Version\":" + ErgebnisNachweisUmschlag.FASSUNG, text);
            Assert.Single(text.Split(new[] { "\"StartJahr\"" }, StringSplitOptions.None).Skip(1));
            Assert.Contains("\"StartJahr\":6", text);

            var zurueck = new WirtschaftlichkeitErgebnis();
            ErgebnisNachweisUmschlag.Lesen(text).Uebernimm(zurueck);
            Assert.Equal(2, zurueck.Betriebskosten.Count);
            Assert.Null(zurueck.Betriebskosten[0].StartJahr);
            Assert.Equal(6, zurueck.Betriebskosten[1].StartJahr);

            var alt = new WirtschaftlichkeitErgebnis();
            ErgebnisNachweisUmschlag.Lesen(
                "nw1:{\"Version\":8,\"Betriebskosten\":[{\"Bezeichnung\":\"Wartung Kessel\",\"BetragJahr\":600}]}")
                .Uebernimm(alt);
            KostenPositionNachweis a = Assert.Single(alt.Betriebskosten);
            Assert.Null(a.StartJahr);
            Assert.True(WirtschaftlichkeitZeilen.LaeuftImErstenJahr(a));
        }

        // =====================================================================
        //  Am echten Lauf — Nachweisliste, Tabellen- und Wortbericht
        // =====================================================================

        /// <summary>
        /// Die Nachweisliste liest das Startjahr wie die Summenschleife: Die Positionen des
        /// ersten Jahres ergeben genau die Betriebskosten p. a. der Rechnung, die übrige
        /// Position steht mit ihrem Jahr im Topf „ab Jahr".
        /// </summary>
        [Fact]
        public void Die_Nachweisliste_traegt_das_Startjahr_der_Summenschleife()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            PositionenHybtest();

            List<KostenPositionNachweis> liste =
                WirtschaftlichkeitCtrl.LiesBetriebskostenPositionen(VARIANTE_A, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Equal(2, liste.Count);
            Assert.Null(liste.Single(x => x.Gruppe == "Wartung BHKW").StartJahr);
            Assert.Equal(6, liste.Single(x => x.Gruppe == "Wartung Kessel").StartJahr);

            WirtschaftlichkeitCtrl.BetriebsTopfe topfe =
                WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(VARIANTE_A, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Null(topfe.Fehler);
            Assert.Equal(topfe.BetriebSofort + topfe.EndenergieSofort,
                         liste.Where(WirtschaftlichkeitZeilen.LaeuftImErstenJahr).Sum(x => x.BetragJahr), 9);
            KeyValuePair<double, int> spaeter = Assert.Single(topfe.BetriebAbJahr);
            Assert.Equal(600.0, spaeter.Key, 9);
            Assert.Equal(6, spaeter.Value);
        }

        /// <summary>
        /// Der Prüffall „hybtest": Tabellen- und Wortbericht warnen nicht mehr, und die
        /// Position der Wartung ab Jahr 6 trägt „ab Jahr 6" in ihrer Herleitung.
        /// </summary>
        [Fact]
        public void Hybtest_Kein_Hinweis_mehr_und_das_Startjahr_steht_an_der_Position()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            PositionenHybtest();
            BerichtsDaten daten = Gruppe();
            Assert.Equal(1800.0, Ergebnis(daten).BetriebskostenJahr.Value, 9);

            string ordner = TempOrdner();
            try
            {
                using (XLWorkbook wb = Excel(daten, ordner))
                {
                    IXLWorksheet w = wb.Worksheet("Wirtschaftlichkeit");
                    Assert.Empty(Warnungen(w));
                    int titel = ZeileMitText(w, R.WIRT_BK_TITEL);
                    Assert.True(titel > 0, "Der Betriebskostenblock fehlt.");
                    IXLRow kessel = w.RowsUsed().First(z => z.RowNumber() > titel &&
                                                            z.Cell(2).GetString() == "Wartung Kessel");
                    Assert.Equal("ab Jahr 6", kessel.Cell(4).GetString());
                    Assert.Equal(600.0, kessel.Cell(5).GetDouble(), 9);
                }
                string wort = Wort(daten, ordner);
                Assert.DoesNotContain(WarnungsAnfang(), wort);
                Assert.Contains("ab Jahr 6", wort);
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>
        /// Eine echte Lücke: Eine Betriebsposition mit der Kostenart „ZUSCHUSS" steht in der
        /// Rechnung (Betriebskosten p. a. 2.300 €), aber in keinem Block der Tabelle — beide
        /// Berichte warnen mit der Summe des ersten Jahres (1.800 €), die Startjahr-Position
        /// bleibt dabei außen vor.
        /// </summary>
        [Fact]
        public void Eine_echte_Luecke_warnt_in_beiden_Berichten()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            PositionenHybtest();
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_ProjektWerte (ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, " +
                "Gruppe, Kostenart, Bemessung) VALUES (1041, 136, 7, 2, 500.0, 'Wartung BHKW', 'ZUSCHUSS', 'BETRAG')");
            BerichtsDaten daten = Gruppe();
            Assert.Equal(2300.0, Ergebnis(daten).BetriebskostenJahr.Value, 9);

            string erwartet = string.Format(R.WIRT_BK_ABWEICHUNG,
                                            1800.0.ToString("N2", DE), 2300.0.ToString("N2", DE));
            string ordner = TempOrdner();
            try
            {
                using (XLWorkbook wb = Excel(daten, ordner))
                    Assert.Equal(erwartet, Assert.Single(Warnungen(wb.Worksheet("Wirtschaftlichkeit"))));
                Assert.Contains(erwartet, Wort(daten, ordner));
            }
            finally { Aufraeumen(ordner); }
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private static KostenPositionNachweis Fest(string gruppe, double betrag, int? start)
        {
            return new KostenPositionNachweis
            {
                Bezeichnung = gruppe, Gruppe = gruppe, Kostenart = DbWerte.KOSTENART_BETRIEBSGEBUNDEN,
                Bemessung = DbWerte.BEMESSUNG_BETRAG, BetragJahr = betrag, StartJahr = start
            };
        }

        /// <summary>Die zwei Betriebspositionen des Prüffalls „hybtest" (Blattstruktur-Wache,
        /// Stufe 1): Wartung BHKW 1.800 €, Wartung Kessel 600 € ab Jahr 6.</summary>
        private static void PositionenHybtest()
        {
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_ProjektWerte (ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, " +
                "Gruppe, Kostenart, Bemessung) VALUES (1041, 83, 7, 2, 1800.0, 'Wartung BHKW', 'BETRIEBSGEBUNDEN', 'BETRAG')");
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_ProjektWerte (ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, " +
                "Gruppe, Kostenart, Bemessung, StartJahr) VALUES (1041, 79, 2, 2, 600.0, 'Wartung Kessel', " +
                "'BETRIEBSGEBUNDEN', 'BETRAG', 6)");
        }

        /// <summary>Stamm 1040 mit den Varianten 1041/1042 und synthetischen Energiekosten —
        /// die Prüfgruppe der Formelmappe, gerechnet ohne zu speichern.</summary>
        private static BerichtsDaten Gruppe()
        {
            int[] ids = { 1040, VARIANTE_A, 1042 };
            double[] energie = { 12000.0, 9000.0, 7000.0 };
            string[] namen = { "Stammprojekt", "Variante A", "Variante B" };
            var daten = new BerichtsDaten { IdStamm = ids[0], Stammprojektname = "Stammprojekt" };
            for (int i = 0; i < ids.Length; i++)
                daten.Varianten.Add(new VariantenDaten
                {
                    IdProjekt = ids[i], IstStamm = i == 0, Projektname = "Stammprojekt",
                    Variantenname = i == 0 ? "" : namen[i], Ergebnis = new ErgebnisModel(),
                    Energiekosten = energie[i]
                });
            WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(ids[0]);
            p.IdStamm = ids[0];
            daten.Wirtschaftlichkeit = new WirtschaftlichkeitCtrl().Berechne(daten, p, 0, false);
            return daten;
        }

        private static WirtschaftlichkeitErgebnis Ergebnis(BerichtsDaten daten)
        {
            return daten.Wirtschaftlichkeit.Single(x => x.IdProjekt == VARIANTE_A &&
                                                        x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
        }

        private static BerichtsKonfiguration VolleKonfiguration()
        {
            var k = new BerichtsKonfiguration();
            foreach (BerichtsKonfiguration.BausteinDef d in BerichtsKonfiguration.AlleBausteine)
                k.AktiveBausteine.Add(d.Schluessel);
            return k;
        }

        private static XLWorkbook Excel(BerichtsDaten daten, string ordner)
        {
            string ziel = Path.Combine(ordner, "gliederung.xlsx");
            new ExcelBerichtGenerator().Erzeuge(daten, VolleKonfiguration(), ziel);
            return new XLWorkbook(ziel);
        }

        private static string Wort(BerichtsDaten daten, string ordner)
        {
            string ziel = Path.Combine(ordner, "gliederung.docx");
            new WordBerichtGenerator().Erzeuge(daten, VolleKonfiguration(), ziel);
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            return string.Join("\n", doc.MainDocumentPart.Document.Body
                .Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().Select(p => p.InnerText));
        }

        /// <summary>Der feste Anfang der Warnung — bis zum ersten Platzhalter.</summary>
        private static string WarnungsAnfang()
        {
            string t = R.WIRT_BK_ABWEICHUNG;
            return t.Substring(0, t.IndexOf("{0}", StringComparison.Ordinal));
        }

        /// <summary>Erste Zeile, deren Spalte A den Text trägt; 0 = keine.</summary>
        private static int ZeileMitText(IXLWorksheet w, string text)
        {
            IXLCell c = w.Column(1).CellsUsed().FirstOrDefault(
                x => string.Equals(x.GetString().Trim(), text, StringComparison.Ordinal));
            return c == null ? 0 : c.Address.RowNumber;
        }

        /// <summary>Die Zellen der Spalte A, die mit der Warnung beginnen.</summary>
        private static List<string> Warnungen(IXLWorksheet w)
        {
            string anfang = WarnungsAnfang();
            return w.Column(1).CellsUsed().Select(c => c.GetString())
                    .Where(s => s.StartsWith(anfang, StringComparison.Ordinal)).ToList();
        }

        private static string TempOrdner()
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-e8c-gliederung-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(o);
            return o;
        }

        private static void Aufraeumen(string ordner)
        {
            try { Directory.Delete(ordner, true); } catch { /* Aufräumen darf nicht scheitern */ }
        }
    }
}

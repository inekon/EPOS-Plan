using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E1 — die <b>Blattstruktur-Wache</b> über Excel- und Wortbericht
    /// (Analysepapier 2026-09-19, Befund N4; Protokoll 05/§ 3.2 und § 3.4).
    ///
    /// <para><b>Warum es sie gibt.</b> <c>ExcelBerichtGenerator.Erzeuge</c> und
    /// <c>WordBerichtGenerator.Erzeuge</c> wurden bis E1 von KEINEM Test gerufen —
    /// die beiden Methoden, an deren Ende der Bericht steht, den der Anwender in
    /// Händen hält. Ohne Wache über die Blattstruktur ist keine Stufe des
    /// Formelberichts abnehmbar: Ein verschobener Block, ein verlorenes Blatt, eine
    /// umbenannte Kopfzeile fielen erst beim Anwender auf.</para>
    ///
    /// <para><b>Was gepinnt wird.</b> Die GERÜSTE, nicht die Inhalte: Blattzahl und
    /// Blattnamen, je Blatt die Ankerzeilen (Titel, Kopf der Kennzahlentabelle,
    /// Szenario-Blocküberschriften, Kopf der Mehrjahrestabelle) und die Werte EINER
    /// festen Ankerzeile; im Wortbericht die Reihenfolge der Überschriften, die Zahl
    /// der Tabellen und die Kopfzeile der Kennzahlentabelle.</para>
    ///
    /// <para><b>Was bewusst NICHT gepinnt wird:</b> alles mit Zeitstempel — die
    /// Zeile „Berichtsdatum" der Übersicht und der Parameterblock mit seinem
    /// „Rechenstand". Sie ändern sich bei jedem Lauf; ein Test, der sie pinnt, ist
    /// am nächsten Tag rot.</para>
    ///
    /// <para><b>Die Daten sind synthetisch</b> (Muster
    /// <c>ReferenzprojektTests.Gruppendaten()</c>): ein Stamm und eine Variante mit
    /// verschiedenen Energiekosten. Die Testdatenbank wird trotzdem gebraucht — der
    /// Wirtschaftlichkeitsblock fragt Speicher- und KWKG-Zustände ab.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BerichtBlattstrukturWacheTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const int STAMM = 9001;
        private const int VARIANTE_A = 9002;

        // =====================================================================
        //  Die Prüfgruppe
        // =====================================================================

        private static BerichtsDaten Gruppendaten()
        {
            var daten = new BerichtsDaten { IdStamm = STAMM, Stammprojektname = "Stammprojekt" };
            daten.Varianten.Add(Stand(STAMM, true, "Stammprojekt", 12000.0));
            daten.Varianten.Add(Stand(VARIANTE_A, false, "Variante A", 9000.0));

            daten.Wirtschaftlichkeit = new WirtschaftlichkeitCtrl().Berechne(daten, Parametersatz());
            return daten;
        }

        private static VariantenDaten Stand(int id, bool istStamm, string name, double energie)
        {
            return new VariantenDaten
            {
                IdProjekt = id,
                IstStamm = istStamm,
                Projektname = "Stammprojekt",
                Variantenname = istStamm ? "" : name,
                Ergebnis = new ErgebnisModel(),
                Energiekosten = energie
            };
        }

        private static WirtschaftlichkeitParameter Parametersatz()
        {
            return new WirtschaftlichkeitParameter
            {
                IdStamm = STAMM,
                IdReferenzprojekt = 0,
                Zinssatz = 3.0,
                Betrachtungszeitraum = 20,
                PreissteigerungEnergie = 0.0,
                PreissteigerungBetrieb = 0.0
            };
        }

        /// <summary>Alle Bausteine an — sonst fehlte gerade der Block, um den es geht.</summary>
        private static BerichtsKonfiguration VolleKonfiguration()
        {
            var k = new BerichtsKonfiguration();
            foreach (BerichtsKonfiguration.BausteinDef d in BerichtsKonfiguration.AlleBausteine)
                k.AktiveBausteine.Add(d.Schluessel);
            return k;
        }

        private static string TempOrdner()
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-e1-bericht-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(o);
            return o;
        }

        // =====================================================================
        //  Excel
        // =====================================================================

        /// <summary>
        /// Blattzahl und Blattnamen in ihrer Reihenfolge: zwei feste Blätter, das
        /// Wirtschaftlichkeitsblatt, dann EIN Blatt je Variante.
        /// </summary>
        [Fact]
        public void Excel_traegt_fuenf_Blaetter_in_fester_Reihenfolge()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string ziel = Path.Combine(ordner, "probe.xlsx");
                string zurueck = new ExcelBerichtGenerator().Erzeuge(Gruppendaten(), VolleKonfiguration(), ziel);

                Assert.Equal(ziel, zurueck);
                Assert.True(File.Exists(ziel), "Die Mappe wurde nicht geschrieben.");

                using var wb = new XLWorkbook(ziel);
                Assert.Equal(5, wb.Worksheets.Count);
                Assert.Equal(
                    new[] { "Übersicht", "Vergleich", "Wirtschaftlichkeit", "Stamm", "Variante A" },
                    wb.Worksheets.OrderBy(w => w.Position).Select(w => w.Name).ToArray());
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>Die Ankerzeilen der drei festen Blätter.</summary>
        [Fact]
        public void Excel_Ankerzeilen_der_Blaetter_stehen_fest()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string ziel = Path.Combine(ordner, "probe.xlsx");
                new ExcelBerichtGenerator().Erzeuge(Gruppendaten(), VolleKonfiguration(), ziel);

                using var wb = new XLWorkbook(ziel);

                // ---- Übersicht ------------------------------------------------
                IXLWorksheet ue = wb.Worksheet("Übersicht");
                Assert.Equal("EPOS-Plan — Variantenvergleich", ue.Cell(1, 1).GetString());
                Assert.Equal("Projekt", ue.Cell(3, 1).GetString());
                Assert.Equal("Stammprojekt", ue.Cell(3, 2).GetString());
                // Kopf der Variantentabelle
                Zeile(ue, 9, "Rolle", "Bezeichner", "Projektname");
                // Kopf der Gewerketabelle
                Zeile(ue, 13, "Gewerk", "Stamm", "Variante A");

                // ---- Vergleich ------------------------------------------------
                Zeile(wb.Worksheet("Vergleich"), 1, "Gruppe", "Kennzahl", "Einheit");

                // ---- Wirtschaftlichkeit ---------------------------------------
                IXLWorksheet w = wb.Worksheet("Wirtschaftlichkeit");
                Assert.Equal("Wirtschaftlichkeit — Kapitalwertmethode (DIN EN 17463)",
                             w.Cell(1, 1).GetString());

                // ETAPPE E2 (VALERI-Lücke G7): der Zeitraumhinweis steht seither auch im
                // Excel-Blatt — er stand nur in Word und auf der Seite.
                Assert.StartsWith("Betrachtungszeitraum T = 20 a", w.Cell(4, 1).GetString());

                // Die drei Szenario-Blöcke, jeder mit seinem eigenen Tabellenkopf.
                //
                // AUFTRAG U6: Jeder Block ist um ZWEI Zeilen gewachsen — die Erlösrubrik
                // gliedert innen nach Komponente, und die Prüfgruppe führt weder BHKW
                // noch Photovoltaik; übrig bleibt der Block „projektweit" mit seinem
                // Kopf und seiner Zwischensumme. Die ZAHLEN sind unverändert; U6 hat
                // keine Rechenwirkung. Alles hinter den Blöcken wandert um sechs Zeilen.
                Assert.Equal("Szenario: Erwartet", w.Cell(8, 1).GetString());
                Zeile(w, 9, "Kennzahl", "Stamm", "Variante A");
                Assert.Equal("projektweit", w.Cell(15, 1).GetString());
                Assert.Equal("Summe projektweit", w.Cell(18, 1).GetString());
                Assert.Equal("Szenario: Best", w.Cell(26, 1).GetString());
                Zeile(w, 28, "Kennzahl", "Stamm", "Variante A");
                Assert.Equal("Szenario: Worst", w.Cell(45, 1).GetString());
                Zeile(w, 47, "Kennzahl", "Stamm", "Variante A");

                // ETAPPE E2 (VALERI-Lücke G8): die Bandbreitentafel mit der Spalte
                // „Spanne" und der Referenzzeile darüber.
                Assert.Equal("Bandbreite der Kapitalwertdifferenz (Worst / Erwartet / Best)",
                             w.Cell(64, 1).GetString());
                Zeile(w, 65, "Variante", "ΔKW Worst [€]", "ΔKW Erwartet [€]");
                Assert.Equal("Spanne [€]", w.Cell(65, 5).GetString());
                Assert.Equal("Stammprojekt", w.Cell(66, 1).GetString());   // Referenzzeile
                Assert.Equal("Variante A", w.Cell(67, 1).GetString());

                // Der Kapitalwert-Verlauf und die Mehrjahrestabelle.
                Assert.Equal("Kapitalwert-Verlauf (kumulierte Barwerte, ohne Restwert) [€]",
                             w.Cell(72, 1).GetString());
                Zeile(w, 73, "Jahr", "Stamm", "Variante A");
                Assert.Equal("Mehrjahresübersicht der Zahlungsströme", w.Cell(97, 1).GetString());
                Assert.Equal("Stamm", w.Cell(100, 1).GetString());
                Zeile(w, 101, "Jahr", "Energiekosten", "Netto nominal");

                // ---- Variantenblatt -------------------------------------------
                IXLWorksheet s = wb.Worksheet("Stamm");
                Assert.Equal("Stamm — Stammprojekt", s.Cell(1, 1).GetString());
                Zeile(s, 4, "Gruppe", "Kennzahl", "Wert");
                Zeile(s, 7, "Erzeuger", "Wärme [MWh/a]", "Strom [MWh/a]");
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>
        /// Die Werte EINER festen Ankerzeile — „Nettobarwert über T" im Szenario
        /// „Erwartet" (Zeile 20). Sie ist der Zahlenanker des Blattes: Bewegt sich
        /// der Rechenweg, fällt dieser Fall, auch wenn die Struktur steht.
        ///
        /// <para>Gegengeprüft gegen das Ende des Kapitalwert-Verlaufs (Jahr 20,
        /// Zeile 88) — bei Restwert 0 müssen beide Zahlen gleich sein.</para>
        ///
        /// <para><b>ETAPPE E2:</b> Die Zeile ist von 18 auf 20 gewandert — der
        /// Zeitraumhinweis (G7) steht jetzt über den Blöcken, und die
        /// Differenzkennzahl steht nach Q19 ÜBER dem Nettobarwert. Die ZAHLEN sind
        /// unverändert; E2 hat keine Rechenwirkung.</para>
        ///
        /// <para><b>AUFTRAG U6:</b> Und von 20 auf 22 — die Erlösrubrik gliedert innen
        /// nach Komponente, die Prüfgruppe bekommt dadurch den Kopf „projektweit" und
        /// seine Zwischensumme dazu. Die ZAHLEN sind wieder unverändert; U6 verteilt,
        /// es rechnet nicht.</para>
        /// </summary>
        [Fact]
        public void Excel_Ankerzeile_Nettobarwert_traegt_die_gerechneten_Werte()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string ziel = Path.Combine(ordner, "probe.xlsx");
                new ExcelBerichtGenerator().Erzeuge(Gruppendaten(), VolleKonfiguration(), ziel);

                using var wb = new XLWorkbook(ziel);
                IXLWorksheet w = wb.Worksheet("Wirtschaftlichkeit");

                Assert.Equal("Nettobarwert über T [€]", w.Cell(22, 1).GetString());
                Assert.Equal(-178529.70, w.Cell(22, 2).GetDouble(), 2);
                Assert.Equal(-133897.27, w.Cell(22, 3).GetDouble(), 2);

                // ANWENDERENTSCHEID Q19 (E2): Die Differenzkennzahl steht DARÜBER.
                Assert.Equal("Kapitalwert gegenüber Stamm [€]", w.Cell(21, 1).GetString());
                Assert.Equal(44632.42, w.Cell(21, 3).GetDouble(), 2);

                // AUFTRAG U6: Die Zwischensumme des einzigen Komponentenblocks IST hier
                // die Summe des Blocks A — die Gliederung ordnet, sie rechnet nicht.
                Assert.Equal("Summe projektweit", w.Cell(18, 1).GetString());
                Assert.Equal(w.Cell(19, 2).GetDouble(), w.Cell(18, 2).GetDouble(), 2);
                Assert.Equal(w.Cell(19, 3).GetDouble(), w.Cell(18, 3).GetDouble(), 2);

                // Letztes Jahr des Verlaufs — ohne Restwert dieselbe Zahl.
                Assert.Equal(20.0, w.Cell(94, 1).GetDouble(), 6);
                Assert.Equal(-178529.70, w.Cell(94, 2).GetDouble(), 2);
                Assert.Equal(-133897.27, w.Cell(94, 3).GetDouble(), 2);

                // Die Mehrjahrestabelle des Stamms: nominale Energiekosten je Jahr.
                Assert.Equal(-12000.00, w.Cell(103, 2).GetDouble(), 2);
                Assert.Equal(-12000.00, w.Cell(103, 3).GetDouble(), 2);

                // ETAPPE E2 (G8): die Spanne der Bandbreitentafel = Best − Worst.
                Assert.Equal(44957.21 - 44312.12, w.Cell(67, 5).GetDouble(), 2);
            }
            finally { Aufraeumen(ordner); }
        }

        // =====================================================================
        //  Word
        // =====================================================================

        /// <summary>
        /// Die Reihenfolge der Hauptüberschriften — sie ist die Reihenfolge der
        /// Bausteine und damit das Gerüst des Berichts.
        /// </summary>
        [Fact]
        public void Word_traegt_die_Hauptueberschriften_in_Bausteinreihenfolge()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string ziel = Path.Combine(ordner, "probe.docx");
                string zurueck = new WordBerichtGenerator().Erzeuge(Gruppendaten(), VolleKonfiguration(), ziel);

                Assert.Equal(ziel, zurueck);
                Assert.True(File.Exists(ziel), "Das Dokument wurde nicht geschrieben.");

                using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
                Body body = doc.MainDocumentPart.Document.Body;

                Assert.Equal("Stammprojekt", ErsterMitStil(body, "Title"));
                Assert.Equal("Variantenvergleich — Energie- und Wärmeversorgung",
                             ErsterMitStil(body, "Subtitle"));

                Assert.Equal(
                    new[]
                    {
                        "Inhalt",
                        "Projektbeschreibung",
                        "Komponenten & Varianten",
                        "Berechnungsergebnisse je Variante",
                        "Variantenvergleich",
                        "Wirtschaftlichkeit",
                        "Anhang",
                    },
                    MitStil(body, "Heading1").ToArray());
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>
        /// Zahl der Tabellen und die Kopfzeile der Kennzahlentabelle. Die
        /// Kennzahlentabelle ist die erste, deren Kopf mit „Kennzahl" beginnt.
        /// </summary>
        [Fact]
        public void Word_traegt_zehn_Tabellen_mit_der_Kennzahlentabelle_darunter()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string ziel = Path.Combine(ordner, "probe.docx");
                new WordBerichtGenerator().Erzeuge(Gruppendaten(), VolleKonfiguration(), ziel);

                using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
                Body body = doc.MainDocumentPart.Document.Body;

                List<Table> tabellen = body.Descendants<Table>().ToList();
                Assert.Equal(10, tabellen.Count);

                // Die ersten Köpfe in ihrer Reihenfolge — das Gerüst der Tabellen.
                Assert.Equal(new[] { "Projekt", "Stammprojekt" }, Kopf(tabellen[0]));
                Assert.Equal(new[] { "Gewerk", "Stamm", "Variante A" }, Kopf(tabellen[2]));
                Assert.Equal(new[] { "Erzeuger", "Wärme [MWh/a]", "Strom [MWh/a]",
                                     "Energieträger", "Verbrauch [MWh/a]" }, Kopf(tabellen[3]));

                // Die Kennzahlentabelle der Wirtschaftlichkeit.
                string[] kennzahlen = tabellen.Select(Kopf)
                                              .FirstOrDefault(k => k.Length > 0 && k[0] == "Kennzahl");
                Assert.NotNull(kennzahlen);
                Assert.Equal(new[] { "Kennzahl", "Stamm", "Variante A" }, kennzahlen);
            }
            finally { Aufraeumen(ordner); }
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private static void Zeile(IXLWorksheet ws, int zeile, params string[] erwartet)
        {
            for (int i = 0; i < erwartet.Length; i++)
                Assert.Equal(erwartet[i], ws.Cell(zeile, i + 1).GetString());
        }

        private static IEnumerable<string> MitStil(Body body, string stil)
        {
            return body.Descendants<Paragraph>()
                       .Where(p => p.ParagraphProperties?.ParagraphStyleId?.Val?.Value == stil)
                       .Select(p => p.InnerText)
                       .Where(t => !string.IsNullOrWhiteSpace(t));
        }

        private static string ErsterMitStil(Body body, string stil)
        {
            return MitStil(body, stil).FirstOrDefault();
        }

        private static string[] Kopf(Table t)
        {
            TableRow r = t.Elements<TableRow>().FirstOrDefault();
            return r == null ? new string[0] : r.Elements<TableCell>().Select(c => c.InnerText).ToArray();
        }

        private static void Aufraeumen(string ordner)
        {
            try { Directory.Delete(ordner, true); } catch { /* Aufräumen darf nicht scheitern */ }
        }
    }
}

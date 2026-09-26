using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Excel-Engine der Berichtsvorlagen</b> (Konzept Berichtsvorlagen 4.4, 4.7, 4.10, 7.1–7.4; Etappe BV-E7):
    /// Zellplatzhalter aller Arten, Namen mit Punkt und Unterstrich, alle Blattmarken samt Entfall, das Musterblatt mit
    /// 0/1/3/7 Ständen, die Standardmappe gleich dem Weg ohne Vorlage (Messlatten 1030 und Gruppe), gleiche Zahlen wie
    /// Word, reservierte Namen, Paketschutz, <c>FullCalculationOnLoad</c>, <c>.xltx</c>, <c>.xlsm</c> abgelehnt, die
    /// Aptos-Vorlage und das Füllen ohne Datenbank.
    /// </summary>
    [Collection("Testdatenbank")]
    public class ExcelVorlagenfuellerTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;
        private readonly string _ordner = Probevorlagen.TempOrdner("epos-bv-e7-");

        public ExcelVorlagenfuellerTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose()
        {
            _kultur.Dispose();
            Probevorlagen.Aufraeumen(_ordner);
        }

        // =====================================================================
        //  Proben
        // =====================================================================

        /// <summary>Die synthetische Gruppe (ohne Datenbank füllbar, solange die Wirtschaftlichkeit nicht angehakt ist)
        /// mit gesetzten Kennzahlen je Stand.</summary>
        private static BerichtsDaten Gruppe(int staende)
        {
            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(staende);
            for (int i = 0; i < daten.Varianten.Count; i++)
            {
                VariantenDaten v = daten.Varianten[i];
                v.Kennzahlen["energie.waermebedarf"] = 1234.5 + i;
                v.Kennzahlen["energie.wp_deckung"] = 62.5 + i;
                v.Kennzahlen["eff.jaz"] = 3.456;
            }
            daten.Warnungen.Add("Erste Warnung");
            daten.Warnungen.Add("Zweite Warnung");
            return daten;
        }

        /// <summary>Alle Häkchen außer der Wirtschaftlichkeit — die Mappe entsteht dann ohne Datenbank.</summary>
        private static BerichtsKonfiguration OhneWirtschaft(bool mitErgebnissen = true)
        {
            var k = new BerichtsKonfiguration();
            foreach (BerichtsKonfiguration.BausteinDef d in BerichtsKonfiguration.AlleBausteine)
                if (d.Schluessel != BerichtsKonfiguration.B_WIRTSCHAFT && (mitErgebnissen || d.Schluessel != BerichtsKonfiguration.B_ERGEBNISSE))
                    k.AktiveBausteine.Add(d.Schluessel);
            return k;
        }

        private (Fuellergebnis Ergebnis, string Pfad) Fuelle(byte[] vorlage, BerichtsDaten daten, BerichtsKonfiguration konfig,
                                                            string name = "mappe.xlsx")
        {
            string ziel = Path.Combine(_ordner, name);
            Fuellergebnis e = new ExcelVorlagenfueller().Fuelle(vorlage, daten, konfig, new Erstellerangaben { Firma = "Probe GmbH" }, ziel);
            foreach (string m in e.Meldungen()) _ausgabe.WriteLine(m);
            return (e, ziel);
        }

        private static List<string> Blattnamen(string pfad)
        {
            using var wb = new XLWorkbook(pfad);
            return wb.Worksheets.OrderBy(w => w.Position).Select(w => w.Name).ToList();
        }

        // =====================================================================
        //  Zellplatzhalter
        // =====================================================================

        /// <summary>
        /// <b>Zellplatzhalter aller Arten</b> (Konzept 4.4, 4.6, 7.1): allein in der Zelle ein typisierter Wert — Zahl
        /// als Zahl mit dem Kernformat bei „Standard“ und dem Format der Vorlage sonst, Datum als Datum, Text als Text,
        /// Liste zeilenweise —; die Prozentregel (Anteil bei Prozentformat, mit Hinweis); im Satz Textersetzung; ein
        /// unbekannter Schlüssel und ein Wert je Stand außerhalb des Musterblatts bleiben gelb stehen; ohne Wert eine
        /// leere Zelle, mit <c>|mit grund</c> der Leerwert samt Grund.
        /// </summary>
        [Fact]
        public void Zellplatzhalter_aller_Arten()
        {
            byte[] vorlage = Excelprobe.Mappe(wb =>
            {
                IXLWorksheet ws = wb.Worksheets.Add("Deckblatt");
                ws.Cell("A1").Value = "{{bericht.titel}}";
                ws.Cell("A2").Value = "{{stamm.kennzahl.energie.waermebedarf}}";
                ws.Cell("A3").Value = "{{stamm.kennzahl.energie.waermebedarf}}";
                ws.Cell("A3").Style.NumberFormat.Format = "0.000";
                ws.Cell("A4").Value = "{{stamm.kennzahl.energie.wp_deckung}}";
                ws.Cell("A5").Value = "{{stamm.kennzahl.energie.wp_deckung}}";
                ws.Cell("A5").Style.NumberFormat.NumberFormatId = 10;
                ws.Cell("A6").Value = "{{bericht.datum}}";
                ws.Cell("A7").Value = "{{bericht.warnungen}}";
                ws.Cell("A8").Value = "Kunde: {{projekt.name}}, {{bericht.varianten.anzahl}} Varianten";
                ws.Cell("A9").Value = "{{projekt.gibtsnicht}}";
                ws.Cell("A10").Value = "{{stand.anzeige}}";
                ws.Cell("A11").Value = "{{stamm.kennzahl.eff.jaz|stellen 1}}";
                ws.Cell("A12").Value = "{{stamm.kennzahl.eff.bhkw_bh}}";
                ws.Cell("A13").Value = "{{stamm.kennzahl.eff.bhkw_bh|mit grund}}";
                ws.Cell("A14").Value = "{{bericht.varianten.anzahl}}";
                ws.Cell("A15").Value = "{{kapitel.wirtschaftlichkeit}}";
                ws.Cell("A16").Value = "{{#je stand}}";
            });

            var (e, pfad) = Fuelle(vorlage, Gruppe(3), OhneWirtschaft());
            using var wb = new XLWorkbook(pfad);
            IXLWorksheet d = wb.Worksheet("Deckblatt");

            Assert.Equal("Stammprojekt", d.Cell("A1").GetString());
            Assert.Equal(1234.5, d.Cell("A2").GetDouble());
            Assert.Equal("#,##0", d.Cell("A2").Style.NumberFormat.Format);
            Assert.Equal("0.000", d.Cell("A3").Style.NumberFormat.Format);   // Format der Vorlage bleibt
            Assert.Equal(62.5, d.Cell("A4").GetDouble());                      // Prozent 0–100 bei „Standard“
            Assert.Equal("#,##0.0", d.Cell("A4").Style.NumberFormat.Format);
            Assert.Equal(0.625, d.Cell("A5").GetDouble(), 12);                 // Prozentformat: der Anteil
            Assert.Contains(e.Hinweise, h => h.Contains("A5", StringComparison.Ordinal) && h.Contains("Prozentformat", StringComparison.Ordinal));
            Assert.True(d.Cell("A6").Value.IsDateTime, "Das Datum ist ein echtes Datum.");
            Assert.Equal(14, d.Cell("A6").Style.NumberFormat.NumberFormatId);
            Assert.Equal("Erste Warnung\nZweite Warnung", d.Cell("A7").GetString());
            Assert.Equal("Kunde: Stammprojekt, 2 Varianten", d.Cell("A8").GetString());
            Assert.Equal("{{projekt.gibtsnicht}}", d.Cell("A9").GetString());
            Assert.Equal(XLColor.Yellow, d.Cell("A9").Style.Fill.BackgroundColor);
            Assert.Equal("{{stand.anzeige}}", d.Cell("A10").GetString());
            Assert.Equal(3.456, d.Cell("A11").GetDouble());
            Assert.Equal("#,##0.0", d.Cell("A11").Style.NumberFormat.Format);
            Assert.True(d.Cell("A12").Value.IsBlank, "Ohne Wert eine leere Zelle, nie 0.");
            Assert.StartsWith("— (", d.Cell("A13").GetString());
            Assert.Equal(2.0, d.Cell("A14").GetDouble());
            Assert.Equal("{{kapitel.wirtschaftlichkeit}}", d.Cell("A15").GetString());
            Assert.Equal("{{#je stand}}", d.Cell("A16").GetString());

            Assert.Contains(e.Unbekannte, b => b.Normalform == "{{projekt.gibtsnicht}}" && b.Art == Fuellbefundart.Unbekannt);
            Assert.Contains(e.Unbekannte, b => b.Normalform == "{{stand.anzeige}}" && b.Art == Fuellbefundart.Kontext);
            Assert.Contains(e.Unbekannte, b => b.Normalform == "{{kapitel.wirtschaftlichkeit}}" && b.Art == Fuellbefundart.FalscheStelle);
            Assert.Contains(e.Unbekannte, b => b.Normalform == "{{#je stand}}" && b.Art == Fuellbefundart.Block);
            Assert.Contains(e.Unbekannte, b => b.Fundort == "Blatt „Deckblatt“, Zelle A9");
            Assert.Equal(2, e.Leere["stamm.kennzahl.eff.bhkw_bh"]);
        }

        /// <summary>
        /// <b>Namen mit Punkt und mit Unterstrich</b> (Konzept 4.4, 5.5): <c>EPOS.projekt.name</c> auf einer Zelle bekommt
        /// den Wert in die Zelle, <c>EPOS_stamm__kennzahl__energie__waermebedarf</c> ohne Bereich wird eine Konstante
        /// (<c>RefersTo</c>), ein Parameter in Prozent als Anteil; ein Name auf mehreren Zellen bleibt und steht im Ergebnis.
        /// </summary>
        [Fact]
        public void Namen_mit_Punkt_und_Unterstrich()
        {
            byte[] vorlage = Excelprobe.Mappe(wb =>
            {
                IXLWorksheet ws = wb.Worksheets.Add("Werte");
                ws.Cell("B2").Value = "alt";
                wb.DefinedNames.Add("EPOS.projekt.name", ws.Range("B2"));
                wb.DefinedNames.Add("EPOS.stamm.kennzahl.eff.jaz", ws.Range("B3"));
                wb.DefinedNames.Add("EPOS_stamm__kennzahl__energie__waermebedarf", "=0");
                wb.DefinedNames.Add("EPOS.bericht.titel", ws.Range("C1:C3"));
                wb.DefinedNames.Add("EPOS.projekt.gibtsnicht", ws.Range("D1"));
                ws.Cell("E1").FormulaA1 = "=EPOS_stamm__kennzahl__energie__waermebedarf*2";
            });

            var (e, pfad) = Fuelle(vorlage, Gruppe(2), OhneWirtschaft());
            using var wb = new XLWorkbook(pfad);
            IXLWorksheet w = wb.Worksheet("Werte");
            Assert.Equal("Stammprojekt", w.Cell("B2").GetString());
            Assert.Equal(3.456, w.Cell("B3").GetDouble());
            Assert.Equal("#,##0.00", w.Cell("B3").Style.NumberFormat.Format);
            string konstante = wb.DefinedNames.First(n => n.Name == "EPOS_stamm__kennzahl__energie__waermebedarf").RefersTo;
            Assert.Equal(1234.5, double.Parse(konstante.TrimStart('='), CultureInfo.InvariantCulture));
            Assert.Equal(2469.0, w.Cell("E1").Value.GetNumber(), 6);   // ClosedXML rechnet über den Namen
            Assert.Contains(e.Unbekannte, b => b.Normalform == "{{bericht.titel}}" && b.Art == Fuellbefundart.NichtUnterstuetzt);
            Assert.Contains(e.Unbekannte, b => b.Normalform == "{{projekt.gibtsnicht}}" && b.Fundort == "Name „EPOS.projekt.gibtsnicht“");
            Assert.True(Excelprobe.VolleNeuberechnung(pfad), "Die Vorlage trägt eine Formel — Excel rechnet beim Öffnen neu.");
            Assert.Contains(e.Hinweise, h => h.Contains("Formeln (1)", StringComparison.Ordinal));
        }

        // =====================================================================
        //  Blattmarken
        // =====================================================================

        /// <summary>
        /// <b>Blattmarken</b> (Konzept 7.2): das erzeugte Blatt tritt an die Stelle der Marke und trägt den heutigen Namen;
        /// Blätter ohne Marke hängen hinten an; eine Marke ohne Inhalt (hier die Wirtschaftlichkeit ohne Häkchen) entfällt
        /// mit ihrem Blatt und steht als Hinweis im Ergebnis; Anwenderblätter bleiben, wo sie stehen.
        /// </summary>
        [Fact]
        public void Blattmarken_ersetzen_ihr_Blatt_ohne_Marke_haengt_an_ohne_Inhalt_entfaellt()
        {
            byte[] vorlage = Excelprobe.Mappe(wb =>
            {
                wb.Worksheets.Add("Deckblatt").Cell("A1").Value = "{{bericht.titel}}";
                wb.Worksheets.Add("Hier Vergleich").Cell("A1").Value = "{{blatt.vergleich}}";
                wb.Worksheets.Add("Geld").Cell("A1").Value = "{{blatt.wirtschaftlichkeit}}";
                wb.Worksheets.Add("Details").Cell("A1").Value = "{{blatt.detail}}";
                wb.Worksheets.Add("Anhang").Cell("A1").Value = "Anhang";
            });

            var (e, pfad) = Fuelle(vorlage, Gruppe(3), OhneWirtschaft());
            Assert.Equal(new[] { "Deckblatt", "Vergleich", "Stamm", "Variante A", "Variante B", "Anhang", "Übersicht" }, Blattnamen(pfad));
            Assert.Contains(e.Hinweise, h => h.Contains("{{blatt.wirtschaftlichkeit}}", StringComparison.Ordinal) &&
                                            h.Contains("„Geld“", StringComparison.Ordinal));
            Assert.Empty(e.Unbekannte);

            using var wb = new XLWorkbook(pfad);
            Assert.Equal("Gruppe", wb.Worksheet("Vergleich").Cell(1, 1).GetString());
            Assert.Equal("EPOS-Plan — Variantenvergleich", wb.Worksheet("Übersicht").Cell(1, 1).GetString());
        }

        /// <summary>
        /// <b>Die Standardmappe als Vorlage</b> (Konzept 7.1, 7.2: nur Blattmarken in heutiger Folge) füllt dieselbe Mappe
        /// wie der Weg ohne Vorlage — Blatt für Blatt, Zeile für Zeile, Name für Name — und trifft die eingefrorenen
        /// Messlatten des Tabellenberichts (1030 und die Gruppe, alle Häkchen). Ohne Vorlage ändert sich nichts.
        /// </summary>
        [Theory]
        [InlineData(BerichtVorlagenMesslatteTests.PROBE_1030)]
        [InlineData(BerichtVorlagenMesslatteTests.PROBE_GRUPPE)]
        public void Standardmappe_fuellt_wie_ohne_Vorlage_und_trifft_die_Messlatte(string probe)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsDaten daten = BerichtVorlagenMesslatteTests.Probe(probe);
            BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();
            string ohne = Path.Combine(_ordner, "ohne.xlsx");
            new ExcelBerichtGenerator().Erzeuge(daten, konfig, ohne);
            var (e, mit) = Fuelle(ExcelVorlagenfueller.Standardmappe(), daten, konfig);

            Assert.Empty(e.Unbekannte);
            Assert.Empty(e.Warnungen);
            List<string> a = Berichtsstruktur.Excel(ohne), b = Berichtsstruktur.Excel(mit);
            Gleich(probe, a, b);
            Assert.Equal(Excelprobe.VolleNeuberechnung(ohne), Excelprobe.VolleNeuberechnung(mit));

            // Die Formelmappe auf dem Vorlagenweg: dieselben Formeln mit denselben nachgetragenen Ergebnissen.
            List<string> formelnOhne = Formeln(ohne), formelnMit = Formeln(mit);
            Assert.NotEmpty(formelnOhne);
            Gleich(probe + " Formeln", formelnOhne, formelnMit);

            string messlatte = Path.Combine(Berichtsdatenproben.Repowurzel(),
                                            BerichtVorlagenMesslatteTests.MESSLATTEN_REPO.Replace('/', Path.DirectorySeparatorChar),
                                            "Bericht_Excel_" + probe + ".txt");
            List<string> erwartet = File.ReadAllText(messlatte, Encoding.UTF8).Split('\n').Select(z => z.TrimEnd('\r'))
                                        .Where(z => z.Length > 0 && !z.StartsWith("# Strukturmesslatte", StringComparison.Ordinal)).ToList();
            Gleich(probe + " Messlatte", erwartet, b);
        }

        /// <summary>
        /// <b>Die Standardmappe ohne Wirtschaftlichkeit, mit leerem Verlauf und null Varianten</b> (Konzept 7.2: „ein Test
        /// deckt B_WIRTSCHAFT aus, leeren Verlauf und null Varianten ab“): dieselbe Mappe wie ohne Vorlage; die Marken
        /// ohne Inhalt entfallen mit Hinweis.
        /// </summary>
        [Fact]
        public void Standardmappe_ohne_Wirtschaft_leerer_Verlauf_null_Varianten()
        {
            BerichtsDaten daten = Gruppe(1);
            daten.Varianten[0].Zeitreihen = null;
            BerichtsKonfiguration konfig = OhneWirtschaft();
            string ohne = Path.Combine(_ordner, "ohne.xlsx");
            new ExcelBerichtGenerator().Erzeuge(daten, konfig, ohne);
            var (e, mit) = Fuelle(ExcelVorlagenfueller.Standardmappe(), daten, konfig);

            Gleich("ohne Wirtschaft", Berichtsstruktur.Excel(ohne), Berichtsstruktur.Excel(mit));
            Assert.Equal(new[] { "Übersicht", "Vergleich", "Stamm" }, Blattnamen(mit));
            foreach (string marke in new[] { "{{blatt.wirtschaftlichkeit}}", "{{blatt.verlauf}}", "{{blatt.checkliste}}" })
                Assert.Contains(e.Hinweise, h => h.Contains(marke, StringComparison.Ordinal));
        }

        // =====================================================================
        //  Musterblatt
        // =====================================================================

        /// <summary>
        /// <b>Das Musterblatt <c>blatt.detail</c></b> (Konzept 7.2) mit 0, 1, 3 und 7 Ständen: je Stand ein Klon an der
        /// Markenstelle, benannt wie das heutige Detailblatt, die <c>stand.*</c>-Platzhalter mit den Werten des Standes
        /// (typisiert und im Satz), die Marke in A1 geleert, das Musterblatt selbst entfällt; ohne Detailblätter (Häkchen
        /// „Ergebnisse je Variante“ aus) entfällt es mit Hinweis.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(7)]
        public void Musterblatt_je_Stand_geklont(int staende)
        {
            byte[] vorlage = Excelprobe.Mappe(wb =>
            {
                wb.Worksheets.Add("Vorne").Cell("A1").Value = "{{bericht.titel}}";
                IXLWorksheet m = wb.Worksheets.Add("Muster");
                m.Cell("A1").Value = "{{blatt.detail}}";
                m.Cell("A2").Value = "{{stand.anzeige}}";
                m.Cell("B3").Value = "{{stand.kennzahl.energie.waermebedarf}}";
                m.Cell("A4").Value = "Stand {{stand.anzeige}}: JAZ {{stand.kennzahl.eff.jaz}}";
                m.Cell("C5").Value = "fest";
                wb.Worksheets.Add("Hinten").Cell("A1").Value = "Ende";
            });

            BerichtsDaten daten = Gruppe(Math.Max(1, staende));
            var (e, pfad) = Fuelle(vorlage, daten, OhneWirtschaft(mitErgebnissen: staende > 0));

            List<string> namen = Blattnamen(pfad);
            int vorne = namen.IndexOf("Vorne"), hinten = namen.IndexOf("Hinten");
            Assert.DoesNotContain("Muster", namen);
            if (staende == 0)
            {
                Assert.Equal(vorne + 1, hinten);
                Assert.Contains(e.Hinweise, h => h.Contains("{{blatt.detail}}", StringComparison.Ordinal));
                return;
            }
            List<string> klone = namen.Skip(vorne + 1).Take(hinten - vorne - 1).ToList();
            Assert.Equal(daten.Varianten.Select(v => v.IstStamm ? "Stamm" : v.Anzeige).ToList(), klone);

            using var wb = new XLWorkbook(pfad);
            for (int i = 0; i < daten.Varianten.Count; i++)
            {
                IXLWorksheet k = wb.Worksheet(klone[i]);
                VariantenDaten v = daten.Varianten[i];
                string anzeige = v.IstStamm ? "Stammprojekt" : v.Anzeige;
                Assert.True(k.Cell("A1").Value.IsBlank);
                Assert.Equal(anzeige, k.Cell("A2").GetString());
                Assert.Equal(1234.5 + i, k.Cell("B3").GetDouble());
                Assert.Equal("Stand " + anzeige + ": JAZ 3,46", k.Cell("A4").GetString());
                Assert.Equal("fest", k.Cell("C5").GetString());
            }
            Assert.Empty(e.Unbekannte);
            Assert.Equal(daten.Varianten.Count, e.Stellen["stand.anzeige"] / 2);
        }

        // =====================================================================
        //  Reservierte Namen, gleichnamige Blätter
        // =====================================================================

        /// <summary>
        /// <b>Reservierte Namen</b> (Konzept 7.4): Eine Vorlage mit <c>Zins_i</c> würde die Formelmappe sprengen — die Engine
        /// entfernt den Namen und warnt, die Formelmappe legt ihn neu an. <b>Gleichnamige Anwenderblätter</b> (7.2) bekommen
        /// einen freien Namen mit Warnung, statt dass <c>Worksheets.Add</c> wirft.
        /// </summary>
        [Fact]
        public void Reservierte_Namen_und_gleichnamige_Blaetter()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            byte[] vorlage = Excelprobe.Mappe(wb =>
            {
                IXLWorksheet ws = wb.Worksheets.Add("Vergleich");
                ws.Cell("A1").Value = "mein Vergleich";
                wb.DefinedNames.Add("Zins_i", ws.Range("B1"));
                wb.DefinedNames.Add("p_E_Guenstig", "=0.02");
            });

            var (e, pfad) = Fuelle(vorlage, BerichtVorlagenMesslatteTests.Probe(BerichtVorlagenMesslatteTests.PROBE_GRUPPE),
                                   Berichtsdatenproben.VolleKonfiguration());
            Assert.Contains(e.Warnungen, w => w.Contains("„Zins_i“", StringComparison.Ordinal));
            Assert.Contains(e.Warnungen, w => w.Contains("„p_E_Guenstig“", StringComparison.Ordinal));
            Assert.Contains(e.Warnungen, w => w.Contains("„Vergleich (Vorlage)“", StringComparison.Ordinal));

            List<string> namen = Blattnamen(pfad);
            Assert.Equal("Vergleich (Vorlage)", namen[0]);
            Assert.Contains("Vergleich", namen);
            using var wb = new XLWorkbook(pfad);
            Assert.Equal("mein Vergleich", wb.Worksheet("Vergleich (Vorlage)").Cell("A1").GetString());
            IXLDefinedName zins = wb.DefinedNames.First(n => n.Name == ExcelFormelmappe.ZINS);
            Assert.StartsWith(BerichtTexte.T("Wirtschaftlichkeit") + "!", zins.RefersTo.Trim('\''), StringComparison.Ordinal);
        }

        // =====================================================================
        //  Paket, Formate, Schrift
        // =====================================================================

        /// <summary><b>Paketschutz</b> (Konzept 7.4): Was ClosedXML beim Füllen verliert — hier eine Form in einer Zeichnung —,
        /// steht mit Namen in den Warnungen.</summary>
        [Fact]
        public void Paketschutz_nennt_verlorene_Formen()
        {
            byte[] vorlage = Excelprobe.MitForm(Excelprobe.Mappe(wb =>
                wb.Worksheets.Add("Deckblatt").Cell("A1").Value = "{{bericht.titel}}"));
            var (e, pfad) = Fuelle(vorlage, Gruppe(2), OhneWirtschaft());
            Assert.Equal(0, Excelprobe.Formen(pfad));
            string verlust = Assert.Single(e.Warnungen, w => w.StartsWith("Beim Füllen verloren:", StringComparison.Ordinal));
            Assert.Contains("Formen", verlust);
        }

        /// <summary>
        /// <b><c>.xltx</c> wird eine Arbeitsmappe, <c>.xlsm</c> wird abgelehnt</b> (Konzept 7.1, Messprobe 3 von BV-E0):
        /// Die Vorlage wird über das SDK umgestellt (Hinweis), die Datei ist eine <c>.xlsx</c>; eine Mappe mit Makros und
        /// eine Datei, die keine Mappe ist, sind benannte Fehler. Ohne Formel in Vorlage und Mappe keine volle Neuberechnung.
        /// </summary>
        [Fact]
        public void Xltx_wird_Arbeitsmappe_xlsm_und_Fremdes_benannt_abgelehnt()
        {
            byte[] mappe = Excelprobe.Mappe(wb => wb.Worksheets.Add("Deckblatt").Cell("A1").Value = "{{bericht.titel}}");
            var (e, pfad) = Fuelle(Excelprobe.AlsVorlage(mappe), Gruppe(1), OhneWirtschaft());
            Assert.Equal(DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook, Excelprobe.Typ(pfad));
            Assert.Contains(e.Hinweise, h => h.Contains(".xltx", StringComparison.Ordinal));
            Assert.False(Excelprobe.VolleNeuberechnung(pfad));

            var makro = Assert.Throws<NotSupportedException>(() => Fuelle(Excelprobe.MitMakros(mappe), Gruppe(2), OhneWirtschaft(), "m.xlsx"));
            Assert.Contains(".xlsm", makro.Message);
            var fremd = Assert.Throws<InvalidDataException>(() => Fuelle(Probevorlagen.AusAbsaetzen("Word"), Gruppe(2), OhneWirtschaft(), "w.xlsx"));
            Assert.False(string.IsNullOrWhiteSpace(fremd.Message));
            Assert.Throws<InvalidDataException>(() => Fuelle(Encoding.UTF8.GetBytes("kein Paket"), Gruppe(2), OhneWirtschaft(), "t.xlsx"));
            var leer = Assert.Throws<ArgumentException>(() => Fuelle(Array.Empty<byte>(), Gruppe(2), OhneWirtschaft(), "l.xlsx"));
            Assert.Equal("vorlage", leer.ParamName);
        }

        /// <summary>
        /// <b>Die Aptos-Vorlage</b> (Konzept 7.1, 12 „Excel mit Standardschrift Aptos“): Die Anwenderblätter behalten ihre
        /// Schrift, die erzeugten Blätter stehen ausdrücklich in Calibri 11 — Titel behalten ihre Größe.
        /// </summary>
        [Fact]
        public void Aptos_Vorlage_erzeugte_Blaetter_bleiben_Calibri_11()
        {
            byte[] vorlage = Excelprobe.MitStandardschrift(Excelprobe.Mappe(wb =>
            {
                IXLWorksheet ws = wb.Worksheets.Add("Deckblatt");
                ws.Cell("A1").Value = "{{bericht.titel}}";
                ws.Cell("A2").Value = "fest";
                wb.Worksheets.Add("M").Cell("A1").Value = "{{blatt.vergleich}}";
            }), "Aptos Narrow");

            var (_, pfad) = Fuelle(vorlage, Gruppe(2), OhneWirtschaft());
            using var wb = new XLWorkbook(pfad);
            Assert.Equal("Aptos Narrow", wb.Worksheet("Deckblatt").Cell("A2").Style.Font.FontName);
            foreach (string blatt in new[] { "Übersicht", "Vergleich", "Stamm", "Variante A" })
            {
                IXLWorksheet ws = wb.Worksheet(blatt);
                foreach (IXLCell c in ws.CellsUsed(XLCellsUsedOptions.Contents))
                {
                    Assert.Equal("Calibri", c.Style.Font.FontName);
                    Assert.True(c.Style.Font.FontSize == 11 || c.Style.Font.FontSize >= 13,
                                blatt + "!" + c.Address + ": Schriftgröße " + c.Style.Font.FontSize);
                }
                Assert.Equal(11, ws.Cell(2, 1).Style.Font.FontSize);
            }
        }

        // =====================================================================
        //  Word gegen Excel, ohne Datenbank
        // =====================================================================

        /// <summary>
        /// <b>Gleiche Zahlen wie Word, ohne Datenbank</b> (Konzept 5.1, 12; BV-E3): Nach dem Sammeln der Gruppe 1019 füllen
        /// Word (Block <c>{{#je stand}}</c>) und Excel (Musterblatt) alle Zahlen <c>stand.wirtschaft.*</c> und
        /// <c>stand.kennzahl.*</c> — mit werfendem <see cref="IDatenzugriff"/>, ohne Zugriff und ohne Nachholen. Jede Zelle
        /// der Mappe ist die Zahl, die Word im Katalogformat zeigt; ein leerer Wert ist in Word der Strich und in Excel leer.
        /// </summary>
        [Fact]
        public void Word_gegen_Excel_fuer_Standwerte_ohne_Datenbank()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const int gruppe = 1019;
            List<int> varianten = new VariantenCtrl().LadeGruppe(gruppe, "").Where(v => !v.IstStamm).Select(v => v.IdProjekt).ToList();
            BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();
            BerichtsDaten daten = new BerichtsDatenSammler().SammleFuerBericht(gruppe, "Probe " + gruppe, varianten,
                Berichtsbedarf.FuerLauf(konfig, null, Startweg.Gewaehlt, true), null, CancellationToken.None, null);

            List<Vorlagenfeld> felder = Vorlagenfeldkatalog.Alle
                .Where(f => f.Art == Vorlagenfeldart.Zahl && f.Kontext == Vorlagenfeldkontext.Stand &&
                            (f.Schluessel.StartsWith("stand.wirtschaft.", StringComparison.Ordinal) ||
                             f.Schluessel.StartsWith("stand.kennzahl.", StringComparison.Ordinal)))
                .ToList();
            Assert.True(felder.Count > 100, "Zu wenige Standwerte: " + felder.Count);

            byte[] excel = Excelprobe.Mappe(wb =>
            {
                IXLWorksheet m = wb.Worksheets.Add("Muster");
                m.Cell(1, 1).Value = "{{blatt.detail}}";
                m.Cell(1, 2).Value = "{{stand.anzeige}}";
                for (int i = 0; i < felder.Count; i++)
                {
                    m.Cell(i + 2, 1).Value = felder[i].Schluessel;
                    m.Cell(i + 2, 2).Value = "{{" + felder[i].Schluessel + "}}";
                }
            });
            var absaetze = new List<string> { "{{#je stand}}", "§{{stand.anzeige}}" };
            absaetze.AddRange(felder.Select(f => f.Schluessel + "=" + "{{" + f.Schluessel + "|ohne einheit}}"));
            absaetze.Add("{{/je}}");
            byte[] word = Probevorlagen.AusAbsaetzen(absaetze.ToArray());

            string xlsx = Path.Combine(_ordner, "werte.xlsx"), docx = Path.Combine(_ordner, "werte.docx");
            Fuellergebnis ex = null, wo = null;
            List<string> zugriffe = BerichtWertesatzTests.OhneDatenbank(() =>
            {
                ex = new ExcelVorlagenfueller().Fuelle(excel, daten, konfig, new Erstellerangaben(), xlsx);
                wo = new WordBerichtGenerator().ErzeugeMitVorlage(daten, konfig, word, new Erstellerangaben(), docx);
            });
            Assert.True(zugriffe.Count == 0, "Datenbankzugriffe beim Füllen:\n" + string.Join("\n", zugriffe.Take(20)));
            Assert.Empty(daten.Wirtschaft.Nachgeholt);
            Assert.Empty(ex.Unbekannte);
            Assert.Empty(wo.Unbekannte);

            // Word: je Stand „§anzeige“, dann „schluessel=text“.
            var wordWerte = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            using (WordprocessingDocument doc = WordprocessingDocument.Open(docx, false))
            {
                Dictionary<string, string> aktuell = null;
                foreach (W.Paragraph p in doc.MainDocumentPart.Document.Body.Elements<W.Paragraph>())
                {
                    string t = p.InnerText;
                    if (t.StartsWith("§", StringComparison.Ordinal)) wordWerte[t.Substring(1)] = aktuell = new Dictionary<string, string>();
                    else if (aktuell != null && t.Contains('=')) aktuell[t.Substring(0, t.IndexOf('='))] = t.Substring(t.IndexOf('=') + 1);
                }
            }
            Assert.Equal(daten.Varianten.Count, wordWerte.Count);

            int zahlen = 0;
            CultureInfo de = CultureInfo.GetCultureInfo("de-DE");
            using var wb = new XLWorkbook(xlsx);
            foreach (IXLWorksheet k in wb.Worksheets.Where(w => w.Cell(1, 2).GetString().Length > 0 && wordWerte.ContainsKey(w.Cell(1, 2).GetString())))
            {
                Dictionary<string, string> w = wordWerte[k.Cell(1, 2).GetString()];
                for (int i = 0; i < felder.Count; i++)
                {
                    Vorlagenfeld f = felder[i];
                    IXLCell c = k.Cell(i + 2, 2);
                    string wortwert = w[f.Schluessel];
                    if (c.Value.IsNumber)
                    {
                        Assert.Equal(wortwert, c.GetDouble().ToString(string.IsNullOrEmpty(f.Format) ? "N0" : f.Format, de));
                        zahlen++;
                    }
                    else
                    {
                        Assert.True(c.Value.IsBlank, k.Name + " " + f.Schluessel + ": " + c.Value);
                        Assert.Equal(Vorlagenfeld.STRICH, wortwert);
                    }
                }
            }
            _ausgabe.WriteLine("Zahlen verglichen: " + zahlen);
            Assert.True(zahlen > 150, "Zu wenige Zahlen verglichen: " + zahlen);
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>Jede Formelzelle der Mappe mit Blatt, Adresse, Formel und zwischengespeichertem Ergebnis.</summary>
        private static List<string> Formeln(string pfad)
        {
            var z = new List<string>();
            using var wb = new XLWorkbook(pfad);
            foreach (IXLWorksheet ws in wb.Worksheets.OrderBy(w => w.Position))
                foreach (IXLCell c in ws.CellsUsed(XLCellsUsedOptions.Contents).Where(c => c.HasFormula))
                    z.Add(ws.Name + "!" + c.Address + " =" + c.FormulaA1 + " → " + c.CachedValue);
            return z;
        }

        private static void Gleich(string was, List<string> a, List<string> b)
        {
            if (a.SequenceEqual(b, StringComparer.Ordinal)) return;
            int i = 0;
            while (i < a.Count && i < b.Count && string.Equals(a[i], b[i], StringComparison.Ordinal)) i++;
            Assert.Fail(was + ": weichen ab (" + a.Count + " / " + b.Count + " Zeilen), zuerst in Zeile " + (i + 1) + ": „" +
                        (i < a.Count ? a[i] : "—") + "“ gegen „" + (i < b.Count ? b[i] : "—") + "“");
        }
    }
}

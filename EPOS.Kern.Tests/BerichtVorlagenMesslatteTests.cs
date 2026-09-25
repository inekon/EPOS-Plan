using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Messlatte der Berichtsvorlagen</b> (Konzept Berichtsvorlagen mit Platzhaltern,
    /// Etappe BV-E0; Abschnitt 11 „Migration“ Nr. 1 und 5).
    ///
    /// <para><b>Was sie festhält.</b> Den Bericht, wie ihn der Bausteinweg HEUTE schreibt —
    /// und zwar MIT DER ECHTEN VORLAGE des Repositoriums
    /// (<see cref="Berichtsdatenproben.VORLAGE_REPO"/>), nicht mit den Ersatzstilen, die ein
    /// Lauf ohne Vorlage bekommt. Zwei Proben (<see cref="Berichtsdatenproben"/>): das
    /// Referenzprojekt 1030 und die synthetische Gruppe, beide mit allen Bausteinen samt
    /// Wirtschaftlichkeit und der Tabelle „Nicht monetarisierbare Wirkungen“. Die Etappen
    /// BV-E1 ff. bauen den Wortbericht auf Vorlagen mit Platzhaltern um; sie treffen diese
    /// Messlatte oder begründen jede Abweichung (Konzept Abschnitt 11 Nr. 1).</para>
    ///
    /// <para><b>Die eingefrorenen Listen</b> liegen unter <c>EPOS.Kern.Tests/Messlatten/</c>:
    /// <c>Bericht_Word_1030.txt</c>, <c>Bericht_Word_Gruppe.txt</c>,
    /// <c>Bericht_Excel_1030.txt</c>, <c>Bericht_Excel_Gruppe.txt</c> (UTF-8 ohne BOM, CRLF;
    /// gelesen über die Repowurzel, die Zeilenenden sind beim Vergleich gleichgültig). Aufbau
    /// der Zeilen: <see cref="Berichtsstruktur"/>. Das Laufdatum steht darin nirgends — jedes
    /// Datum im Text ist <c>&lt;datum&gt;</c>.</para>
    ///
    /// <para><b>Neu einfrieren.</b> Weicht ein Lauf ab, schreibt der Test die AKTUELLE Liste
    /// in den Testausgabeordner — <c>EPOS.Kern.Tests/bin/&lt;Konfiguration&gt;/net10.0/Messlatten/</c>
    /// — und meldet den ersten abweichenden Block. Ist die Abweichung gewollt und begründet
    /// (Konzept 11 Nr. 1: Stiletiketten nach der Bereinigung, Lage der Wirkungstafel,
    /// <c>{{bericht.datum}}</c> statt des DATE-Felds, jede neue Zeile eines Bausteins), wird
    /// die Datei von dort nach <c>EPOS.Kern.Tests/Messlatten/</c> kopiert und mit der
    /// Begründung im selben Commit eingecheckt. Eine fehlende Messlatte entsteht auf demselben
    /// Weg. Nie von Hand editieren — die Liste ist, was der Lauf schreibt.</para>
    ///
    /// <para><b>Nebenbei gemessen:</b> die Laufzeit von Wort- und Tabellenbericht für 1030 und
    /// für eine Gruppe mit sieben Ständen (<see cref="Laufzeit"/>) — der Maßstab für die
    /// Leistungsabnahme der späteren Etappen („heute + 10 %“).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BerichtVorlagenMesslatteTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;

        public BerichtVorlagenMesslatteTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose() => _kultur.Dispose();

        /// <summary>Probe „Referenzprojekt 1030“ (<see cref="Berichtsdatenproben.Projektdaten1030"/>).</summary>
        public const string PROBE_1030 = "1030";

        /// <summary>Probe „synthetische Gruppe“ (<see cref="Berichtsdatenproben.Gruppendaten"/>).</summary>
        public const string PROBE_GRUPPE = "Gruppe";

        // =====================================================================
        //  1 — der Bericht mit der echten Vorlage
        // =====================================================================

        /// <summary>
        /// <b>Mit echter Vorlage.</b> Der Wortbericht, erzeugt aus der Berichtsvorlage des
        /// Repositoriums mit allen Bausteinen samt Wirtschaftlichkeit, ist (a) in jeder
        /// Office-Fassung von 2007 bis 2021 gültiges OpenXML und (b) endet mit der
        /// Abschnittsangabe der Vorlage: Hinter der letzten <c>w:sectPr</c> des Rumpfs steht
        /// kein Element. Beides traf die Wirkungstafel, solange sie am Einfügeanker vorbei an
        /// den Rumpf gehängt wurde (Konzept 2.4, Befund BW:678) — ohne Vorlage fällt das nicht
        /// auf, weil der Rumpf dann keine Abschnittsangabe hat.
        /// </summary>
        [Theory]
        [InlineData(PROBE_1030)]
        [InlineData(PROBE_GRUPPE)]
        public void Mit_echter_Vorlage_gueltig_und_nichts_hinter_der_Abschnittsangabe(string probe)
        {
            string vorlage = Berichtsdatenproben.Berichtsvorlage();
            if (vorlage == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string ziel = Path.Combine(ordner, "bericht.docx");
                new WordBerichtGenerator().Erzeuge(Probe(probe), Berichtsdatenproben.VolleKonfiguration(), ziel, vorlage);

                using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
                Body body = doc.MainDocumentPart.Document.Body;

                // Die Probe löst die Wirkungstafel aus — sonst prüfte der Fall die Stelle nicht.
                Assert.Contains(body.Descendants<Table>(), t => Kopf(t).FirstOrDefault() == R.WIRT_NM_SP_KATEGORIE);

                // (b) Hinter der letzten Abschnittsangabe steht nichts.
                SectionProperties sect = body.Elements<SectionProperties>().LastOrDefault();
                Assert.NotNull(sect);
                List<string> dahinter = sect.ElementsAfter().Select(Beschreibe).ToList();

                // (a) Der Validator in allen sechs Fassungen.
                var befunde = new List<string>();
                foreach (FileFormatVersions fassung in WordBerichtSvgWacheTests.Fassungen)
                {
                    List<ValidationErrorInfo> alle = new OpenXmlValidator(fassung).Validate(doc).ToList();
                    List<ValidationErrorInfo> fehler = alle;
                    _ausgabe.WriteLine(probe + " · " + fassung + ": " + alle.Count + " Meldungen");
                    foreach (ValidationErrorInfo f in alle)
                        _ausgabe.WriteLine("   " + Text(f));
                    if (fehler.Count > 0)
                        befunde.Add(fassung + ": " + fehler.Count + " Fehler — " +
                                    string.Join(" | ", fehler.Take(3).Select(Text)));
                }
                _ausgabe.WriteLine(probe + " · hinter der Abschnittsangabe: " + dahinter.Count +
                                   (dahinter.Count > 0 ? " (" + string.Join(", ", dahinter) + ")" : ""));

                Assert.True(dahinter.Count == 0 && befunde.Count == 0,
                    "Probe " + probe + ": " + dahinter.Count + " Element(e) hinter der Abschnittsangabe" +
                    (dahinter.Count > 0 ? " (" + string.Join(", ", dahinter) + ")" : "") +
                    (befunde.Count > 0 ? "; Validator: " + string.Join(" || ", befunde) : ""));
            }
            finally { Aufraeumen(ordner); }
        }

        // =====================================================================
        //  2 — die Strukturmesslatte
        // =====================================================================

        /// <summary>
        /// <b>Die Strukturmesslatte.</b> Wortbericht (mit echter Vorlage) und Tabellenbericht
        /// der Probe, als Strukturliste gegen die eingefrorene Liste unter
        /// <c>EPOS.Kern.Tests/Messlatten/</c>. Beide Listen werden verglichen, bevor der Fall
        /// fällt — eine Abweichung im Wortbericht verdeckt keine im Tabellenbericht.
        /// </summary>
        [Theory]
        [InlineData(PROBE_1030)]
        [InlineData(PROBE_GRUPPE)]
        public void Messlatte_Word_und_Excel(string probe)
        {
            string vorlage = Berichtsdatenproben.Berichtsvorlage();
            if (vorlage == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                BerichtsDaten daten = Probe(probe);
                BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();

                string docx = Path.Combine(ordner, "bericht.docx");
                new WordBerichtGenerator().Erzeuge(daten, konfig, docx, vorlage);
                string xlsx = Path.Combine(ordner, "bericht.xlsx");
                new ExcelBerichtGenerator().Erzeuge(daten, konfig, xlsx);

                var befunde = new List<string>();
                Vergleiche("Bericht_Word_" + probe + ".txt", Berichtsstruktur.Word(docx), befunde);
                Vergleiche("Bericht_Excel_" + probe + ".txt", Berichtsstruktur.Excel(xlsx), befunde);
                Assert.True(befunde.Count == 0, string.Join(Environment.NewLine + Environment.NewLine, befunde));
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>Die erste Zeile der Messlatten des Vorlagenwegs.</summary>
        private const string KOPFZEILE_VORLAGE =
            "# Strukturmesslatte BV-E2 (BerichtVorlagenMesslatteTests, Vorlagenweg mit der Standardvorlage) — ";

        /// <summary>
        /// Die Programmfassung des Deckblatts im Vorlagenweg — fest, damit die Messlatte nicht an der Fassung
        /// des Testwirts hängt (<c>ersteller.version</c> nimmt sonst die Produktfassung des Einstiegs).
        /// </summary>
        private const string FASSUNG = "9.9.9.9";

        /// <summary>Wo der Kapitelteil beginnt: die Überschrift des Inhaltsverzeichnisses.</summary>
        private const string KAPITELTEIL = "Absatz [Heading1] Inhalt";

        /// <summary>
        /// <b>Die Messlatte des Vorlagenwegs</b> (Konzept 11 Nr. 1 und 2, Etappe BV-E2): Die Standardvorlage im
        /// vollen Aufbau ergibt über die Engine für 1030 und die Gruppe denselben Bericht wie der Bausteinweg —
        /// BIS AUF das Deckblatt, das die Vorlage aus Platzhaltern trägt (begründete Abweichung, Anhang B.3).
        /// Eingefroren ist der Rumpf (<c>Messlatten/Bericht_Word_&lt;probe&gt;_Vorlage.txt</c>, neu einfrieren wie
        /// die übrigen); Kopf- und Fußzeile hält <see cref="WordVorlagenfuellerTests"/>, weil das Logo der
        /// Kopfzeile an der Fassung des Werkzeugs hängt. <b>Zeilengleich zur Messlatte des alten Wegs</b> ist
        /// der Kapitelteil ab dem Inhaltsverzeichnis: Der Kapitelkopf der Vorlage trägt dort das Format „EPOS
        /// Kapitelkopf“ (auf Überschrift 1 aufgebaut, Gliederungsebene 1) statt Überschrift 1 — das ist die
        /// einzige Umschrift —, und den Seitenumbruch vor der Checkliste des Anhangs E setzt die Engine vor
        /// deren Kapitelkopf, wo der Bausteinweg ihn vor seine Überschrift schreibt.
        /// </summary>
        [Theory]
        [InlineData(PROBE_1030)]
        [InlineData(PROBE_GRUPPE)]
        public void Messlatte_Vorlagenweg_mit_der_Standardvorlage(string probe)
        {
            string pfad = BerichtsvorlageDateiWacheTests.Pfad(BerichtsvorlageDateiWacheTests.STANDARD);
            if (pfad == null || !File.Exists(pfad)) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string docx = Path.Combine(ordner, "vorlage.docx");
                Fuellergebnis ergebnis = new WordBerichtGenerator().ErzeugeMitVorlage(
                    Probe(probe), Berichtsdatenproben.VolleKonfiguration(), File.ReadAllBytes(pfad),
                    new Erstellerangaben { Firma = "INEKON GmbH", Version = FASSUNG }, docx);
                Assert.Empty(ergebnis.Unbekannte);

                List<string> rumpf = Berichtsstruktur.Word(docx).SkipWhile(z => z != "## Rumpf").ToList();
                var befunde = new List<string>();
                Vergleiche("Bericht_Word_" + probe + "_Vorlage.txt", rumpf, befunde, KOPFZEILE_VORLAGE);

                string alt = Path.Combine(Berichtsdatenproben.Repowurzel(), MESSLATTEN_REPO.Replace('/', Path.DirectorySeparatorChar),
                                          "Bericht_Word_" + probe + ".txt");
                List<string> kapitelAlt = Zeilen(File.ReadAllText(alt, Encoding.UTF8)).SkipWhile(z => z != KAPITELTEIL).ToList();
                List<string> kapitelNeu = rumpf.SkipWhile(z => z != KAPITELTEIL)
                                               .Select(z => z.Replace("Absatz [EPOSKapitelkopf] ", "Absatz [Heading1] "))
                                               .ToList();
                Assert.NotEmpty(kapitelAlt);
                if (!kapitelAlt.SequenceEqual(kapitelNeu, StringComparer.Ordinal))
                    befunde.Add("Kapitelteil ab dem Inhaltsverzeichnis weicht von Bericht_Word_" + probe + ".txt ab. " +
                                Unterschied(kapitelAlt, kapitelNeu));
                else
                    _ausgabe.WriteLine(probe + ": Kapitelteil " + kapitelNeu.Count + " Zeilen, zeilengleich zum alten Weg");

                Assert.True(befunde.Count == 0, string.Join(Environment.NewLine + Environment.NewLine, befunde));
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>Unterordner der Messlatten, repo-relativ.</summary>
        internal const string MESSLATTEN_REPO = "EPOS.Kern.Tests/Messlatten";

        /// <summary>
        /// Vergleicht die Liste des Laufs mit der eingefrorenen Datei <paramref name="datei"/>.
        /// Bei Abweichung (oder fehlender Datei) steht die aktuelle Liste danach im
        /// Testausgabeordner, und <paramref name="befunde"/> nennt den ersten abweichenden Block.
        /// </summary>
        private void Vergleiche(string datei, List<string> struktur, List<string> befunde, string kopfzeile = KOPFZEILE)
        {
            var aktuell = new List<string> { kopfzeile + datei };
            aktuell.AddRange(struktur);

            string wurzel = Berichtsdatenproben.Repowurzel();
            string pfad = Path.Combine(wurzel, MESSLATTEN_REPO.Replace('/', Path.DirectorySeparatorChar), datei);
            List<string> erwartet = File.Exists(pfad) ? Zeilen(File.ReadAllText(pfad, Encoding.UTF8)) : null;
            if (erwartet != null && erwartet.SequenceEqual(aktuell, StringComparer.Ordinal))
            {
                _ausgabe.WriteLine(datei + ": " + aktuell.Count + " Zeilen, gleich der Messlatte");
                return;
            }

            string ausgabe = Path.Combine(AppContext.BaseDirectory, "Messlatten", datei);
            Directory.CreateDirectory(Path.GetDirectoryName(ausgabe));
            File.WriteAllText(ausgabe, string.Join("\r\n", aktuell) + "\r\n", new UTF8Encoding(false));

            befunde.Add(erwartet == null
                ? datei + ": Die Messlatte fehlt (" + MESSLATTEN_REPO + "/" + datei + "). Die Liste dieses Laufs (" +
                  aktuell.Count + " Zeilen) steht unter " + ausgabe + " — prüfen und nach " + MESSLATTEN_REPO + " kopieren."
                : datei + ": weicht von der Messlatte ab. " + Unterschied(erwartet, aktuell) + Environment.NewLine +
                  "Die Liste dieses Laufs steht unter " + ausgabe + " — ist die Abweichung begründet, von dort nach " +
                  MESSLATTEN_REPO + " kopieren (Kopfkommentar der Testklasse).");
        }

        /// <summary>Die erste Zeile jeder Messlatte — sie nennt Herkunft und Regel.</summary>
        private const string KOPFZEILE = "# Strukturmesslatte BV-E0 (BerichtVorlagenMesslatteTests, echte Vorlage) — ";

        /// <summary>Die Zeilen eines Textes, gleich welche Zeilenenden; eine Schlusszeile ohne Inhalt entfällt.</summary>
        private static List<string> Zeilen(string text)
        {
            List<string> zeilen = text.Split('\n').Select(z => z.TrimEnd('\r')).ToList();
            if (zeilen.Count > 0 && zeilen[^1].Length == 0) zeilen.RemoveAt(zeilen.Count - 1);
            return zeilen;
        }

        /// <summary>
        /// Der erste abweichende Block: gemeinsamer Anfang und gemeinsames Ende abgezogen, was
        /// dazwischen steht — je Seite höchstens zehn Zeilen, mit Zeilennummer der Datei.
        /// </summary>
        private static string Unterschied(List<string> erwartet, List<string> aktuell)
        {
            int anfang = 0;
            while (anfang < erwartet.Count && anfang < aktuell.Count &&
                   string.Equals(erwartet[anfang], aktuell[anfang], StringComparison.Ordinal))
                anfang++;
            int ende = 0;
            while (ende < erwartet.Count - anfang && ende < aktuell.Count - anfang &&
                   string.Equals(erwartet[erwartet.Count - 1 - ende], aktuell[aktuell.Count - 1 - ende], StringComparison.Ordinal))
                ende++;

            var sb = new StringBuilder();
            sb.Append("Messlatte ").Append(erwartet.Count).Append(" Zeilen, Lauf ").Append(aktuell.Count)
              .Append(" Zeilen; erste Abweichung in Zeile ").Append(anfang + 1).Append('.');
            Block(sb, "Messlatte", erwartet, anfang, erwartet.Count - ende);
            Block(sb, "Lauf", aktuell, anfang, aktuell.Count - ende);
            return sb.ToString();
        }

        private static void Block(StringBuilder sb, string seite, List<string> zeilen, int von, int bis)
        {
            sb.Append(Environment.NewLine).Append("  ").Append(seite);
            if (bis <= von)
            {
                sb.Append(": keine Zeile an dieser Stelle (vor Zeile ").Append(von + 1).Append(')');
                return;
            }
            sb.Append(" (Zeilen ").Append(von + 1).Append('–').Append(bis).Append("):");
            for (int i = von; i < bis && i < von + 10; i++)
                sb.Append(Environment.NewLine).Append("    ").Append(i + 1).Append(": ").Append(zeilen[i]);
            if (bis - von > 10) sb.Append(Environment.NewLine).Append("    … ").Append(bis - von - 10).Append(" weitere");
        }

        // =====================================================================
        //  3 — die Laufzeit heute
        // =====================================================================

        /// <summary>Sicherheitsgrenze je Erzeugung — der Fall misst, er bewertet nicht.</summary>
        private const double GRENZE_SEKUNDEN = 120.0;

        /// <summary>Erzeugungen je Probe und Ausgabe.</summary>
        private const int LAEUFE = 3;

        /// <summary>
        /// <b>Die Laufzeit heute</b> — der Maßstab für die Leistungsabnahme der späteren Etappen
        /// (Konzept 13, BV-E5: „heute + 10 %“). Wortbericht (mit echter Vorlage) und
        /// Tabellenbericht je <see cref="LAEUFE"/>-mal für 1030 und für die synthetische Gruppe
        /// mit sieben Ständen; gemessen wird allein die Erzeugung, nicht Simulation und
        /// Wirtschaftlichkeitsrechnung der Probe davor. Die Zeiten stehen in der Testausgabe; der
        /// Fall hält nur die Sicherheitsgrenze von <see cref="GRENZE_SEKUNDEN"/> s je Erzeugung.
        /// </summary>
        [Fact]
        public void Laufzeit()
        {
            string vorlage = Berichtsdatenproben.Berichtsvorlage();
            if (vorlage == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();
                var proben = new List<(string Name, BerichtsDaten Daten)>
                {
                    ("1030", Probe(PROBE_1030)),
                    ("Gruppe mit 7 Ständen", Probe(PROBE_GRUPPE, 7)),
                };
                Assert.Equal(7, proben[1].Daten.Varianten.Count);

#if DEBUG
                const string konfiguration = "Debug";
#else
                const string konfiguration = "Release";
#endif
                _ausgabe.WriteLine("Laufzeit · " + konfiguration + " · " +
                                   System.Runtime.InteropServices.RuntimeInformation.OSDescription + " · " +
                                   System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);

                var zuLang = new List<string>();
                int nummer = 0;
                foreach ((string name, BerichtsDaten daten) in proben)
                    foreach (string art in new[] { "Word", "Excel" })
                    {
                        var zeiten = new List<long>();
                        for (int lauf = 1; lauf <= LAEUFE; lauf++)
                        {
                            string ziel = Path.Combine(ordner, "lauf_" + (++nummer) + (art == "Word" ? ".docx" : ".xlsx"));
                            Stopwatch uhr = Stopwatch.StartNew();
                            if (art == "Word") new WordBerichtGenerator().Erzeuge(daten, konfig, ziel, vorlage);
                            else new ExcelBerichtGenerator().Erzeuge(daten, konfig, ziel);
                            uhr.Stop();
                            zeiten.Add(uhr.ElapsedMilliseconds);
                            if (uhr.Elapsed.TotalSeconds > GRENZE_SEKUNDEN)
                                zuLang.Add(name + " · " + art + " · Lauf " + lauf + ": " +
                                           uhr.Elapsed.TotalSeconds.ToString("0.0") + " s");
                        }
                        List<long> sortiert = zeiten.OrderBy(z => z).ToList();
                        _ausgabe.WriteLine("Laufzeit " + name + " · " + art + ": " + string.Join(" / ", zeiten) +
                                           " ms (Median " + sortiert[sortiert.Count / 2] + " ms)");
                    }

                Assert.True(zuLang.Count == 0, "Über der Sicherheitsgrenze von " + GRENZE_SEKUNDEN + " s: " +
                                               string.Join("; ", zuLang));
            }
            finally { Aufraeumen(ordner); }
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private static string Text(ValidationErrorInfo f)
            => f.Id + " @ " + (f.Part != null ? f.Part.Uri.ToString() : "?") + " " + f.Path?.XPath + ": " + f.Description;

        /// <summary>Der Berichtsbaum der Probe, mit Wirtschaftlichkeit und Wirkungen.</summary>
        internal static BerichtsDaten Probe(string probe, int staende = 2)
        {
            BerichtsDaten daten;
            WirtschaftlichkeitParameter p;
            if (probe == PROBE_1030)
            {
                daten = Berichtsdatenproben.Projektdaten1030();
                p = new WirtschaftlichkeitCtrl().LadeParameter(Berichtsdatenproben.PROJEKT_1030);
                p.IdStamm = Berichtsdatenproben.PROJEKT_1030;
                // Der KWKG-Förderbeginn FEST: Ohne Inbetriebnahmedatum rechnet der Zuschlag ab
                // dem Folgejahr des LAUFS (WirtschaftlichkeitCtrl.Foerderbeginn), und die
                // Herleitung der Sätze („… Stand 2027“) kippte zum Jahreswechsel. Mit dem
                // 01.01.2027 sind Wort- und Tabellenliste der Probe gleich denen ohne Datum
                // (gemessen am 25.09.2026) — nur eben in jedem Jahr.
                p.KwkgInbetriebnahme = new DateTime(2027, 1, 1);
            }
            else
            {
                daten = Berichtsdatenproben.Gruppendaten(staende);
                p = Berichtsdatenproben.Parametersatz(Berichtsdatenproben.STAMM);
            }
            Berichtsdatenproben.MitWirtschaftlichkeit(daten, p);
            Berichtsdatenproben.MitWirkungen(daten, p);
            return daten;
        }

        private static string Beschreibe(OpenXmlElement e)
        {
            string text = e.InnerText ?? "";
            return e.LocalName + (text.Length > 0 ? " „" + (text.Length > 40 ? text.Substring(0, 40) + "…" : text) + "“" : "");
        }

        private static string[] Kopf(Table t)
        {
            TableRow r = t.Elements<TableRow>().FirstOrDefault();
            return r == null ? new string[0] : r.Elements<TableCell>().Select(c => c.InnerText).ToArray();
        }

        private static string TempOrdner()
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-bv-e0-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(o);
            return o;
        }

        private static void Aufraeumen(string ordner)
        {
            try { Directory.Delete(ordner, true); } catch { /* Aufräumen darf nicht scheitern */ }
        }
    }
}

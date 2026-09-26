using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using A = DocumentFormat.OpenXml.Drawing;
using ASVG = DocumentFormat.OpenXml.Office2019.Drawing.SVG;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Rundlauf der ausführlichen Vorlage</b> (Entscheid BV-E8-4; Konzept Berichtsvorlagen 6.3, 12): Beide Dateien —
    /// deutsch und englisch, in ihrer Sprache — füllen das Referenzprojekt 1030, die Gruppe 1019 (zwei Varianten, über den
    /// Sammler) und die synthetische Gruppe mit drei Ständen ohne Prüferfehler und ohne unbekannten Platzhalter; im Bericht
    /// bleibt kein <c>{{</c>, der Validator ist in jeder Fassung grün, jede Bildstelle trägt SVG mit PNG-Rückfall.
    ///
    /// <para><b>Gegen den Standardbericht</b> mit denselben Daten: dieselbe Folge der Kapitelköpfe, jede Strukturtabelle
    /// des Standardberichts steht Zelle für Zelle gleich in der ausführlichen Fassung (dieselben Quellen, derselbe
    /// Tabellenbauer), und die Werte der Musterzeile „Auf einen Blick“ stehen Zahl für Zahl in der Kennzahltafel der
    /// Wirtschaftlichkeit.</para>
    ///
    /// <para>Mit der Umgebungsvariable <c>EPOS_BERICHT_ABLAGE</c> legt der Lauf die gefüllten Berichte zur Ansicht in
    /// diesen Ordner.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class AusfuehrlichRundlaufTests : IDisposable
    {
        private readonly ITestOutputHelper _ausgabe;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly string _ordner;

        internal const string AUSFUEHRLICH = BerichtsvorlagenCtrl.DATEI_AUSFUEHRLICH;
        internal const string AUSFUEHRLICH_EN = BerichtsvorlagenCtrl.DATEI_AUSFUEHRLICH_EN;

        private const string PROBE_1019 = "1019";
        private const string PROBE_GRUPPE3 = "Gruppe3";

        public AusfuehrlichRundlaufTests(ITestOutputHelper ausgabe)
        {
            _ausgabe = ausgabe;
            _ordner = Path.Combine(Path.GetTempPath(), "epos-ausfuehrlich-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_ordner);
        }

        public void Dispose()
        {
            _kultur.Dispose();
            try { Directory.Delete(_ordner, true); } catch { /* Aufräumen darf nicht scheitern */ }
        }

        [Theory]
        [InlineData(AUSFUEHRLICH, false, BerichtVorlagenMesslatteTests.PROBE_1030)]
        [InlineData(AUSFUEHRLICH_EN, true, BerichtVorlagenMesslatteTests.PROBE_1030)]
        [InlineData(AUSFUEHRLICH, false, PROBE_1019)]
        [InlineData(AUSFUEHRLICH_EN, true, PROBE_1019)]
        [InlineData(AUSFUEHRLICH, false, PROBE_GRUPPE3)]
        [InlineData(AUSFUEHRLICH_EN, true, PROBE_GRUPPE3)]
        public void Die_ausfuehrliche_Vorlage_fuellt_ohne_Befund_wie_der_Standardbericht(string datei, bool englisch, string probe)
        {
            string pfad = BerichtsvorlageDateiWacheTests.Pfad(datei);
            string standardpfad = BerichtsvorlageDateiWacheTests.Pfad(BerichtsvorlageDateiWacheTests.STANDARD);
            if (pfad == null || !File.Exists(pfad) || standardpfad == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int vorher = Sprache.Nummer;
            try
            {
                using var kultur = new Kulturvorrichtung(englisch ? "en-US" : "de-DE");
                Sprache.Nummer = englisch ? 1 : 0;
                byte[] vorlage = File.ReadAllBytes(pfad);
                BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();

                // Prüfer: kein Fehler, kein unbekannter Schlüssel, Sprache der Vorlage = Sprache des Laufs, kein Kapitel.
                Pruefbefund befund = Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Voll, Pruefkontext.Aus(konfig, englisch, 1, datei));
                foreach (Pruefmeldung m in befund.Meldungen) _ausgabe.WriteLine(m.Stufe + ": " + m.Text);
                Assert.Empty(befund.Meldungen.Where(m => m.Stufe == Befundstufe.Fehler).Select(m => m.Text));
                Assert.Empty(befund.UnbekannteSchluessel);
                Assert.False(befund.SpracheAbweichend);
                Assert.Equal(englisch ? "en" : "de", befund.Sprache);
                Assert.Equal((int?)Vorlagenfeldkatalog.KatalogfassungWord, befund.Katalogfassung);
                Assert.Empty(befund.Schluessel.Where(s => s.StartsWith("kapitel.", StringComparison.Ordinal) || s == "bericht.inhalt"));
                Assert.True(befund.HatWirtschaftlichkeit);
                Assert.True(befund.DeckblattAusPlatzhaltern);

                BerichtsDaten daten = Daten(probe, konfig);
                string ziel = Path.Combine(_ordner, "ausfuehrlich.docx");
                string standard = Path.Combine(_ordner, "standard.docx");
                Fuellergebnis ergebnis = new WordBerichtGenerator().ErzeugeMitVorlage(daten, konfig, vorlage, Ersteller(), ziel);
                foreach (string w in ergebnis.Warnungen.Concat(ergebnis.Fehler)) _ausgabe.WriteLine("Lauf: " + w);
                Assert.Empty(ergebnis.Unbekannte.Select(u => u.Normalform + " @ " + u.Fundort));
                Assert.Empty(ergebnis.Fehler);
                Fuellergebnis vergleich = new WordBerichtGenerator().ErzeugeMitVorlage(daten, konfig, File.ReadAllBytes(standardpfad),
                                                                                       Ersteller(), standard);
                Assert.Empty(vergleich.Fehler);
                Ablegen(ziel, probe, englisch, "ausfuehrlich");
                Ablegen(standard, probe, englisch, "standard");

                using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
                using WordprocessingDocument std = WordprocessingDocument.Open(standard, false);
                MainDocumentPart main = doc.MainDocumentPart;
                using (WordprocessingDocument leer = WordprocessingDocument.Open(pfad, false))
                    Assert.Equal(leer.MainDocumentPart.WordprocessingCommentsPart.Comments.Elements<Comment>().Count(), ergebnis.EntfernteKommentare);

                var teile = new List<OpenXmlElement> { main.Document.Body };
                teile.AddRange(main.HeaderParts.Select(h => (OpenXmlElement)h.Header));
                teile.AddRange(main.FooterParts.Select(f => (OpenXmlElement)f.Footer));
                foreach (OpenXmlElement teil in teile)
                    Assert.DoesNotContain("{{", teil.InnerText, StringComparison.Ordinal);
                Assert.Null(main.WordprocessingCommentsPart);
                Assert.Empty(main.Document.Body.Elements<Table>().Where(Tabellenmuster.IstMuster));

                List<string> fehler = BerichtsvorlageDateiWacheTests.Validatorfehler(doc);
                Assert.True(fehler.Count == 0, string.Join(" | ", fehler.Take(5)));

                // Jede Bildstelle: PNG-Rückfall und SVG.
                List<A.Blip> blips = main.Document.Body.Descendants<A.Blip>().ToList();
                Assert.True(blips.Count >= 4, blips.Count + " Bilder im Rumpf");
                foreach (A.Blip blip in blips)
                {
                    Assert.Equal("image/png", main.GetPartById(blip.Embed.Value).ContentType);
                    ASVG.SVGBlip svg = blip.Descendants<ASVG.SVGBlip>().FirstOrDefault();
                    Assert.NotNull(svg);
                    OpenXmlPart svgTeil = main.GetPartById(svg.Embed.Value);
                    Assert.Equal("image/svg+xml", svgTeil.ContentType);
                    using var leser = new StreamReader(svgTeil.GetStream(FileMode.Open, FileAccess.Read), Encoding.UTF8);
                    Assert.StartsWith("<svg", leser.ReadToEnd(), StringComparison.Ordinal);
                }

                // Gegen den Standardbericht: dieselbe Abschnittsfolge ...
                List<string> koepfe = Kapitelkoepfe(main.Document.Body), stdKoepfe = Kapitelkoepfe(std.MainDocumentPart.Document.Body);
                _ausgabe.WriteLine("Kapitelköpfe: " + string.Join(" | ", koepfe));
                Assert.Equal(stdKoepfe, koepfe);

                // ... jede Strukturtabelle des Standardberichts mit Zahlen Zelle für Zelle gleich ...
                List<string> tafeln = Tafeln(main.Document.Body, false), stdTafeln = Tafeln(std.MainDocumentPart.Document.Body, true);
                List<string> fehlend = stdTafeln.Where(t => !tafeln.Contains(t)).ToList();
                foreach (string t in fehlend) _ausgabe.WriteLine("fehlt: " + (t.Length > 160 ? t.Substring(0, 160) + "…" : t));
                Assert.True(fehlend.Count == 0, fehlend.Count + " von " + stdTafeln.Count + " Tafeln des Standardberichts fehlen");

                // ... die Kennzahltafeln des Stamms aus Einzelwerten wie die Eigenschaftstafeln des Kapitels (mit Sammler
                // erhoben; die Proben ohne Sammler führen keine Kennzahlen des Stamms) ...
                if (probe == PROBE_1019)
                    foreach (string ueberschrift in englisch
                                 ? new[] { "Energy demand (base simulation result)", "Coverage by demand type" }
                                 : new[] { "Energiebedarf (Simulationsergebnis Stamm)", "Deckungsgrade je Bedarfsart" })
                        Assert.Equal(TafelNach(std.MainDocumentPart.Document.Body, ueberschrift), TafelNach(main.Document.Body, ueberschrift));

                // ... und die Musterzeile der Wirtschaftlichkeit mit den Zahlen der Kennzahltafel.
                int geprueft = Wirtschaftswerte(main.Document.Body, englisch);
                _ausgabe.WriteLine(probe + " " + datei + ": " + ergebnis.Ersetzt + " ersetzt, " + blips.Count + " Bilder, "
                                   + stdTafeln.Count + " Tafeln gleich, " + geprueft + " Wirtschaftswerte gleich");
            }
            finally
            {
                Sprache.Nummer = vorher;
            }
        }

        /// <summary>Der Berichtsbaum einer Probe: 1030 und die synthetische Gruppe wie die Messlatte, 1019 über den Sammler.</summary>
        private static BerichtsDaten Daten(string probe, BerichtsKonfiguration konfig)
        {
            if (probe == PROBE_GRUPPE3)
                return BerichtVorlagenMesslatteTests.Probe(BerichtVorlagenMesslatteTests.PROBE_GRUPPE, 3);
            if (probe != PROBE_1019)
                return BerichtVorlagenMesslatteTests.Probe(probe);
            List<int> varianten = new VariantenCtrl().LadeGruppe(1019, "").Where(v => !v.IstStamm).Select(v => v.IdProjekt).ToList();
            BerichtsDaten daten = new BerichtsDatenSammler().SammleFuerBericht(1019, "Probe 1019", varianten,
                Berichtsbedarf.FuerLauf(konfig, null, Startweg.Gewaehlt, false), null, CancellationToken.None, null);
            Assert.True(daten.Wirtschaft?.AusDiesemLauf == true, "Die Rechnung dieses Laufs fehlt: " + daten.WirtschaftlichkeitFehler);
            return daten;
        }

        private static Erstellerangaben Ersteller() => new Erstellerangaben { Firma = "Firma 1", Version = "1.0" };

        /// <summary>Legt den gefüllten Bericht in <c>EPOS_BERICHT_ABLAGE</c> ab, wenn die Variable gesetzt ist.</summary>
        private static void Ablegen(string datei, string probe, bool englisch, string art)
        {
            string ablage = Environment.GetEnvironmentVariable("EPOS_BERICHT_ABLAGE");
            if (string.IsNullOrWhiteSpace(ablage)) return;
            Directory.CreateDirectory(ablage);
            File.Copy(datei, Path.Combine(ablage, "Bericht_" + probe + "_" + art + (englisch ? "_en" : "") + ".docx"), true);
        }

        /// <summary>Die Texte der Absätze im Format „EPOS Kapitelkopf“ in Dokumentfolge.</summary>
        private static List<string> Kapitelkoepfe(Body rumpf)
            => rumpf.Descendants<Paragraph>()
                    .Where(p => p.ParagraphProperties?.ParagraphStyleId?.Val?.Value == "EPOSKapitelkopf")
                    .Select(p => p.InnerText.Trim()).ToList();

        /// <summary>
        /// Jede Tabelle als Text: Zeilen durch „¶“, Zellen durch „|“ getrennt — ohne geschachtelte Tabellen und Bildtabellen.
        /// Mit <paramref name="nurStruktur"/> nur die Strukturtabellen des Standardberichts (Kopfzeile auf der
        /// Kopfhinterlegung des Tabellenbauers) mit mindestens einer Zahl unter dem Kopf — die Eigenschaftstafeln der
        /// Kapitel sind keine Strukturtabellen, die ausführliche Vorlage baut sie aus Einzelwerten. In der Checkliste von
        /// Anhang E zählt die Spalte „Stelle“ nicht: Sie nennt die Kapitelplatzhalter der Vorlage.
        /// </summary>
        private static List<string> Tafeln(Body rumpf, bool nurStruktur)
        {
            var tafeln = new List<string>();
            foreach (Table t in rumpf.Descendants<Table>().Where(t => !t.Ancestors<Table>().Any() && !t.Descendants<A.Blip>().Any()))
            {
                List<List<string>> zeilen = t.Elements<TableRow>()
                    .Select(z => z.Elements<TableCell>().Select(c => c.InnerText.Trim()).ToList()).ToList();
                if (zeilen.Count == 0) continue;
                if (nurStruktur)
                {
                    bool kopf = t.Elements<TableRow>().First().Elements<TableCell>().All(c =>
                        c.TableCellProperties?.Shading?.Fill?.Value == WordBerichtGenerator.HEAD_FILL);
                    if (!kopf || !zeilen.Skip(1).SelectMany(z => z).Any(z => z.Any(char.IsDigit))) continue;
                }
                if (zeilen[0].Count > 3 && (zeilen[0][3] == "Stelle im Bericht" || zeilen[0][3] == "Place in the report"))
                    foreach (List<string> z in zeilen.Where(z => z.Count > 3)) z.RemoveAt(3);
                tafeln.Add(string.Join("¶", zeilen.Select(z => string.Join("|", z))));
            }
            return tafeln;
        }

        /// <summary>
        /// Die Werte (letzte Spalte) der ersten Tabelle nach der Überschrift <paramref name="ueberschrift"/> — die
        /// Beschriftungen nicht: Die Eigenschaftstafel des Kapitels führt sie auch im englischen Bericht teils deutsch,
        /// die Vorlage nimmt die Beschriftung der Kennzahl in der Sprache des Berichts.
        /// </summary>
        private static string TafelNach(Body rumpf, string ueberschrift)
        {
            List<OpenXmlElement> folge = rumpf.ChildElements.ToList();
            int stelle = folge.FindIndex(e => e is Paragraph p && p.InnerText == ueberschrift);
            Assert.True(stelle >= 0, "Überschrift „" + ueberschrift + "“ fehlt");
            Table t = folge.Skip(stelle).OfType<Table>().First();
            return string.Join("¶", t.Elements<TableRow>().Select(z => z.Elements<TableCell>().Last().InnerText.Trim()));
        }

        /// <summary>
        /// Die Musterzeile „Auf einen Blick“ gegen die Kennzahltafel der Wirtschaftlichkeit: jeder Wert, der eine Zahl trägt,
        /// steht als Zelle in der Tafel „Kennzahlen im Szenario ‚Erwartet‘“ (die erste Tabelle nach ihrer Überschrift).
        /// Rückgabe: die Zahl der geprüften Werte.
        /// </summary>
        private static int Wirtschaftswerte(Body rumpf, bool englisch)
        {
            List<OpenXmlElement> folge = rumpf.ChildElements.ToList();
            string kopf = englisch ? "Key figures, scenario \"Expected\"" : "Kennzahlen im Szenario „Erwartet“";
            int stelle = folge.FindIndex(e => e is Paragraph p && p.InnerText == kopf);
            Assert.True(stelle >= 0, "Überschrift der Kennzahltafel fehlt");
            Table tafel = folge.Skip(stelle).OfType<Table>().First();
            string blick = englisch ? "At a glance:" : "Auf einen Blick:";
            int musterstelle = folge.FindIndex(e => e is Paragraph p && p.InnerText == blick);
            Assert.True(musterstelle > stelle, "„Auf einen Blick“ fehlt");
            Table muster = folge.Skip(musterstelle).OfType<Table>().First();
            var zellen = new HashSet<string>(tafel.Descendants<TableCell>().Select(c => c.InnerText.Trim()), StringComparer.Ordinal);
            int geprueft = 0;
            foreach (TableRow zeile in muster.Elements<TableRow>().Skip(1))
                foreach (TableCell zelle in zeile.Elements<TableCell>().Skip(1))
                {
                    string wert = zelle.InnerText.Trim();
                    if (!wert.Any(char.IsDigit)) continue;
                    Assert.True(zellen.Contains(wert), "Wert „" + wert + "“ steht nicht in der Kennzahltafel");
                    geprueft++;
                }
            Assert.True(geprueft > 0, "keine Zahl in der Musterzeile");
            return geprueft;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Leistungstest der Berichtsvorlagen</b> (Konzept Berichtsvorlagen 8.5, 13; Abnahme BV-E5: „Leistungstest
    /// 60 Seiten innerhalb ‚heute + 10 %‘“).
    ///
    /// <para><b>Was er misst.</b> Einen Bericht von rund sechzig Seiten — die synthetische Gruppe mit sieben Ständen,
    /// alle Bausteine samt Wirtschaftlichkeit und Wirkungen, also mit allen Tabellen und über fünfzig Bildern —
    /// einmal auf dem <b>heutigen Bausteinweg</b> (<see cref="WordBerichtGenerator.Erzeuge"/> mit der Stilvorlage
    /// <c>Berichtsvorlage.docx</c>) und einmal auf dem <b>Vorlagenweg</b>
    /// (<see cref="WordBerichtGenerator.ErzeugeMitVorlage"/> mit der ausgelieferten Standardvorlage) — derselbe
    /// Umfang, dieselben Daten. Gemessen wird allein das Schreiben, nicht Simulation und Wirtschaftlichkeit davor.</para>
    ///
    /// <para><b>Kriterium:</b> Median Vorlagenweg ≤ Median Bausteinweg × <see cref="GRENZE"/>. Ein Verhältnis statt
    /// einer Zeit, damit der Fall auf jedem Läufer gilt; die Läufe wechseln sich ab (A B, B A …), ein Aufwärmpaar
    /// zählt nicht. Ein Durchgang über der Grenze wird bis zu <see cref="DURCHGAENGE"/>-mal wiederholt — ein
    /// ausgelasteter Läufer trifft einen Durchgang, eine echte Verlangsamung jeden. Die Zeiten stehen in der
    /// Testausgabe. Gekennzeichnet mit <c>[Trait("Kategorie","Messung")]</c> (ausfilterbar mit
    /// <c>--filter "Kategorie!=Messung"</c>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    [Trait("Kategorie", "Messung")]
    public class BerichtsvorlagenLeistungTests : IDisposable
    {
        /// <summary>„Heute + 10 %“ (Konzept 8.5).</summary>
        private const double GRENZE = 1.10;

        /// <summary>Gemessene Paare je Durchgang (nach dem Aufwärmpaar).</summary>
        private const int PAARE = 5;

        /// <summary>Höchstzahl der Durchgänge.</summary>
        private const int DURCHGAENGE = 3;

        /// <summary>Stände der Probe: der Stamm und sechs Varianten.</summary>
        private const int STAENDE = 7;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;
        private readonly string _ordner = Probevorlagen.TempOrdner("epos-bv-e5-leistung");

        public BerichtsvorlagenLeistungTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose()
        {
            Probevorlagen.Aufraeumen(_ordner);
            _kultur.Dispose();
        }

        /// <summary>
        /// Der Vorlagenweg mit der Standardvorlage schreibt die Gruppe mit sieben Ständen höchstens 10 % langsamer
        /// als der Bausteinweg; beide Berichte tragen dieselbe Zahl Tabellen und Bilder.
        /// </summary>
        [Fact]
        public void Vorlagenweg_mit_sieben_Staenden_innerhalb_heute_plus_zehn_Prozent()
        {
            string stilvorlage = Berichtsdatenproben.Berichtsvorlage();
            string standard = BerichtsvorlageDateiWacheTests.Pfad(BerichtsvorlageDateiWacheTests.STANDARD);
            if (stilvorlage == null || standard == null || !File.Exists(standard)) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsDaten daten = BerichtVorlagenMesslatteTests.Probe(BerichtVorlagenMesslatteTests.PROBE_GRUPPE, STAENDE);
            Assert.Equal(STAENDE, daten.Varianten.Count);
            BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();
            byte[] vorlage = File.ReadAllBytes(standard);
            var ersteller = new Erstellerangaben { Firma = "Musterfirma", Version = "9.9.9.9" };

            int nummer = 0;
            string Ziel() => Path.Combine(_ordner, "lauf_" + (++nummer) + ".docx");
            long Bausteinweg()
            {
                string ziel = Ziel();
                Stopwatch uhr = Stopwatch.StartNew();
                new WordBerichtGenerator().Erzeuge(daten, konfig, ziel, stilvorlage);
                uhr.Stop();
                return uhr.ElapsedMilliseconds;
            }
            long Vorlagenweg()
            {
                string ziel = Ziel();
                Stopwatch uhr = Stopwatch.StartNew();
                Fuellergebnis e = new WordBerichtGenerator().ErzeugeMitVorlage(daten, konfig, vorlage, ersteller, ziel);
                uhr.Stop();
                Assert.Empty(e.Unbekannte);
                return uhr.ElapsedMilliseconds;
            }

            // Derselbe Umfang: gleich viele Tabellen und Bilder auf beiden Wegen.
            string a = Ziel();
            new WordBerichtGenerator().Erzeuge(daten, konfig, a, stilvorlage);
            string b = Ziel();
            new WordBerichtGenerator().ErzeugeMitVorlage(daten, konfig, vorlage, ersteller, b);
            (int tabellenA, int bilderA, int absaetzeA) = Umfang(a);
            (int tabellenB, int bilderB, int absaetzeB) = Umfang(b);
            _ausgabe.WriteLine("Umfang Bausteinweg: " + tabellenA + " Tabellen, " + bilderA + " Bilder, " + absaetzeA + " Absätze");
            _ausgabe.WriteLine("Umfang Vorlagenweg: " + tabellenB + " Tabellen, " + bilderB + " Bilder, " + absaetzeB + " Absätze");
            Assert.Equal(tabellenA, tabellenB);
            Assert.Equal(bilderA, bilderB);
            Assert.True(bilderA >= 50, "Die Probe soll über fünfzig Bilder tragen, hat " + bilderA);

#if DEBUG
            const string konfiguration = "Debug";
#else
            const string konfiguration = "Release";
#endif
            _ausgabe.WriteLine("Leistung · " + konfiguration + " · " + System.Runtime.InteropServices.RuntimeInformation.OSDescription +
                               " · " + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);

            var verhaeltnisse = new List<double>();
            for (int durchgang = 1; durchgang <= DURCHGAENGE; durchgang++)
            {
                Bausteinweg();
                Vorlagenweg();
                var baustein = new List<long>();
                var vorlagenweg = new List<long>();
                for (int i = 0; i < PAARE; i++)
                {
                    if (i % 2 == 0) { baustein.Add(Bausteinweg()); vorlagenweg.Add(Vorlagenweg()); }
                    else { vorlagenweg.Add(Vorlagenweg()); baustein.Add(Bausteinweg()); }
                }
                double mb = Median(baustein), mv = Median(vorlagenweg);
                double verhaeltnis = mv / Math.Max(1.0, mb);
                verhaeltnisse.Add(verhaeltnis);
                _ausgabe.WriteLine("Durchgang " + durchgang + ": Bausteinweg " + string.Join(" / ", baustein) + " ms (Median " + mb +
                                   "), Vorlagenweg " + string.Join(" / ", vorlagenweg) + " ms (Median " + mv + "), Verhältnis " +
                                   verhaeltnis.ToString("0.000"));
                if (verhaeltnis <= GRENZE) break;
            }

            Assert.True(verhaeltnisse.Min() <= GRENZE,
                "Der Vorlagenweg ist langsamer als heute + 10 %: Verhältnis " +
                string.Join(" / ", verhaeltnisse.Select(v => v.ToString("0.000"))) + " (Grenze " + GRENZE.ToString("0.00") + ")");
        }

        private static double Median(List<long> werte)
        {
            List<long> s = werte.OrderBy(w => w).ToList();
            return s.Count % 2 == 1 ? s[s.Count / 2] : (s[s.Count / 2 - 1] + s[s.Count / 2]) / 2.0;
        }

        /// <summary>Tabellen, Bilder und Absätze im Rumpf eines Berichts.</summary>
        private static (int Tabellen, int Bilder, int Absaetze) Umfang(string pfad)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(pfad, false);
            Body body = doc.MainDocumentPart.Document.Body;
            return (body.Descendants<Table>().Count(), body.Descendants<DW.DocProperties>().Count(), body.Descendants<Paragraph>().Count());
        }
    }
}

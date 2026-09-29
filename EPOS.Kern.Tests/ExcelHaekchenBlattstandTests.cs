using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Häkchen wirken auf die Blätter der Excel-Vorlage</b> (Entscheid BV-Q2 (c), Konzept
    /// Berichtsvorlagen 7.2, 10.2): der Blattstand, den der Excel-Prüfer aus der Mappe misst
    /// (<see cref="ExcelBlattstand"/>, <see cref="Pruefbefund.Bausteine"/>), und die Wirkung eines
    /// abgewählten Bausteins beim Füllen — ein Blatt ohne angehaktes Häkchen entsteht nicht, seine
    /// Blattmarke entfällt mit dem Hinweis, den eine Marke ohne Inhalt schon immer bekommt.
    /// </summary>
    public class ExcelHaekchenBlattstandTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;
        private readonly string _ordner = Probevorlagen.TempOrdner("epos-bv-q2c-");

        public ExcelHaekchenBlattstandTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose()
        {
            _kultur.Dispose();
            Probevorlagen.Aufraeumen(_ordner);
        }

        // =====================================================================
        //  Proben
        // =====================================================================

        private static BerichtsDaten Gruppe(int staende)
        {
            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(staende);
            for (int i = 0; i < daten.Varianten.Count; i++)
                daten.Varianten[i].Kennzahlen["energie.waermebedarf"] = 1000 + i;
            return daten;
        }

        /// <summary>Die genannten Häkchen abgewählt; die Wirtschaftlichkeit bleibt immer draußen (sie bräuchte die Datenbank).</summary>
        private static BerichtsKonfiguration Ohne(params string[] abgewaehlt)
        {
            var aus = new HashSet<string>(abgewaehlt, StringComparer.Ordinal) { BerichtsKonfiguration.B_WIRTSCHAFT };
            var k = new BerichtsKonfiguration();
            foreach (BerichtsKonfiguration.BausteinDef d in BerichtsKonfiguration.AlleBausteine)
                if (!aus.Contains(d.Schluessel)) k.AktiveBausteine.Add(d.Schluessel);
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

        private Pruefbefund Pruefe(byte[] vorlage)
        {
            Pruefbefund b = ExcelVorlagenpruefer.Pruefe(vorlage, Pruefstufe.Schnell,
                new Pruefkontext { Dateiname = "probe.xlsx", Ausgabe = Vorlagenausgabe.Excel });
            _ausgabe.WriteLine(Probevorlagen.Liste(b));
            return b;
        }

        private static readonly string[] EXCELBAUSTEINE =
        {
            BerichtsKonfiguration.B_PROJEKT, BerichtsKonfiguration.B_KOMPONENTEN, BerichtsKonfiguration.B_ERGEBNISSE,
            BerichtsKonfiguration.B_VERGLEICH, BerichtsKonfiguration.B_WIRTSCHAFT,
        };

        // =====================================================================
        //  1 — die Zuordnung Blatt ↔ Häkchen
        // =====================================================================

        /// <summary>
        /// Jedes erzeugte Blatt außer den Diagrammdaten gehört mindestens einem Häkchen, jedes Häkchen der
        /// Mappe (alles außer <c>NurWord</c>) mindestens einem Blatt — sonst stünde ein Eintrag der Liste
        /// ohne Entsprechung da.
        /// </summary>
        [Fact]
        public void Jedes_erzeugte_Blatt_und_jedes_Excel_Haekchen_hat_seine_Entsprechung()
        {
            foreach (ExcelBerichtGenerator.Blattart art in Enum.GetValues<ExcelBerichtGenerator.Blattart>())
                Assert.True(ExcelBerichtGenerator.Blattbausteine.ContainsKey(art), art.ToString());

            Assert.Empty(ExcelBerichtGenerator.Blattbausteine[ExcelBerichtGenerator.Blattart.Diagrammdaten]);
            Assert.Equal(EXCELBAUSTEINE.OrderBy(s => s, StringComparer.Ordinal),
                         ExcelBerichtGenerator.Blattbausteine.Values.SelectMany(v => v).Distinct()
                                              .OrderBy(s => s, StringComparer.Ordinal));
            Assert.Equal(EXCELBAUSTEINE.OrderBy(s => s, StringComparer.Ordinal),
                         ExcelBlattstand.Moegliche.OrderBy(s => s, StringComparer.Ordinal));
        }

        /// <summary>
        /// Ein Blatt entsteht, solange EINES seiner Häkchen gesetzt ist: Die Übersicht trägt
        /// Projektbeschreibung und Komponenten, sie entfällt erst, wenn beide fehlen. Ohne Konfiguration
        /// entsteht jedes Blatt.
        /// </summary>
        [Fact]
        public void Ein_Blatt_entsteht_solange_eines_seiner_Haekchen_gesetzt_ist()
        {
            ExcelBerichtGenerator.Blattart uebersicht = ExcelBerichtGenerator.Blattart.Uebersicht;
            Assert.True(ExcelBerichtGenerator.BlattGewaehlt(uebersicht, null));
            Assert.True(ExcelBerichtGenerator.BlattGewaehlt(uebersicht, Ohne(BerichtsKonfiguration.B_PROJEKT)));
            Assert.True(ExcelBerichtGenerator.BlattGewaehlt(uebersicht, Ohne(BerichtsKonfiguration.B_KOMPONENTEN)));
            Assert.False(ExcelBerichtGenerator.BlattGewaehlt(uebersicht,
                Ohne(BerichtsKonfiguration.B_PROJEKT, BerichtsKonfiguration.B_KOMPONENTEN)));

            // Die Diagrammdaten hängen an keinem Häkchen — sie tragen die Zahlen der vorhandenen Diagramme.
            Assert.True(ExcelBerichtGenerator.BlattGewaehlt(ExcelBerichtGenerator.Blattart.Diagrammdaten,
                Ohne(EXCELBAUSTEINE)));
        }

        // =====================================================================
        //  2 — der Blattstand einer Excel-Vorlage
        // =====================================================================

        /// <summary>
        /// Die Regel auf ihren drei Größen: Wer die erzeugten Blätter anhängt, führt jeden Baustein der
        /// Mappe; wer sie nicht anhängt, führt die Häkchen seiner Blattmarken und die Kapitel, die er aus
        /// Einzelelementen deckt; ohne Marke und ohne Anhang führt die Vorlage keine Blätter.
        /// </summary>
        [Fact]
        public void Die_Regel_des_Blattstands()
        {
            var keine = Array.Empty<ExcelBerichtGenerator.Blattart>();
            var wirtschaft = new[] { ExcelBerichtGenerator.Blattart.Wirtschaftlichkeit };

            Assert.True(ExcelBlattstand.FuehrtBlaetter(keine, true));
            Assert.True(ExcelBlattstand.FuehrtBlaetter(wirtschaft, false));
            Assert.False(ExcelBlattstand.FuehrtBlaetter(keine, false));

            // Hängt sie an, führt sie alles — auch ohne eine einzige Marke.
            Assert.Equal(EXCELBAUSTEINE, ExcelBlattstand.Gefuehrt(keine, true, new[] { "bericht.titel" }));

            // Ohne Anhang zählt die Marke; ein Schlüssel, den kein Kapitel der Mappe deckt, ändert nichts.
            Assert.Equal(new[] { BerichtsKonfiguration.B_WIRTSCHAFT },
                         ExcelBlattstand.Gefuehrt(wirtschaft, false, new[] { "blatt.wirtschaftlichkeit", "bericht.titel" }));

            // Ohne Anhang zählt auch, was die Vorlage aus Einzelelementen nachbildet.
            Assert.Contains(BerichtsKonfiguration.B_VERGLEICH,
                            ExcelBlattstand.Gefuehrt(keine, false, new[] { "tabelle.vergleich.liste" }));
            Assert.Empty(ExcelBlattstand.Gefuehrt(keine, false, Array.Empty<string>()));
        }

        /// <summary>
        /// Der Prüfer legt den Blattstand in den Befund: Eine gewöhnliche Vorlage hängt die erzeugten
        /// Blätter an und führt damit jeden Baustein der Mappe — die Häkchenliste bleibt vollständig frei.
        /// </summary>
        [Fact]
        public void Eine_Vorlage_die_anhaengt_fuehrt_jeden_Baustein()
        {
            byte[] vorlage = Excelprobe.Mappe(wb => wb.Worksheets.Add("Deckblatt").Cell("A1").Value = "{{bericht.titel}}");
            Pruefbefund b = Pruefe(vorlage);
            Assert.True(b.HatKapitel);
            Assert.Equal(EXCELBAUSTEINE, b.Bausteine);
        }

        /// <summary>
        /// Eine Vorlage, die das Anhängen abschaltet (<c>EPOS.Blattanhang</c> = nein), führt nur die
        /// Häkchen ihrer Blattmarken; die übrigen graut die Hülle aus.
        /// </summary>
        [Fact]
        public void Ohne_Blattanhang_fuehrt_die_Vorlage_nur_ihre_Marken()
        {
            byte[] vorlage = Excelprobe.Mappe(wb =>
            {
                wb.Worksheets.Add("Deckblatt").Cell("A1").Value = "{{bericht.titel}}";
                wb.Worksheets.Add("Hier Vergleich").Cell("A1").Value = "{{blatt.vergleich}}";
                wb.CustomProperties.Add(ExcelVorlagenmappe.EIGENSCHAFT_BLATTANHANG, "nein");
            });

            Pruefbefund b = Pruefe(vorlage);
            Assert.True(b.HatKapitel);
            Assert.Equal(new[] { BerichtsKonfiguration.B_VERGLEICH }, b.Bausteine);
        }

        /// <summary>
        /// Eine Vorlage ohne Blattmarke und ohne Anhang führt gar keine Blätter: Die Seite zeigt dann die
        /// leise Zeile „Den Inhalt bestimmt die Vorlage“ statt der Liste.
        /// </summary>
        [Fact]
        public void Ohne_Marke_und_ohne_Anhang_fuehrt_die_Vorlage_keine_Blaetter()
        {
            byte[] vorlage = Excelprobe.Mappe(wb =>
            {
                wb.Worksheets.Add("Deckblatt").Cell("A1").Value = "{{bericht.titel}}";
                wb.CustomProperties.Add(ExcelVorlagenmappe.EIGENSCHAFT_BLATTANHANG, "nein");
            });
            Pruefbefund b = Pruefe(vorlage);
            Assert.False(b.HatKapitel);
        }

        /// <summary>
        /// <b>Die ausgelieferte ausführliche Excel-Vorlage</b> bildet Übersicht, Vergleich und Verlauf aus
        /// Einzelelementen nach und trägt für die übrigen Blätter Marken — sie führt deshalb jeden Baustein
        /// der Mappe, obwohl sie das Anhängen abschaltet. Kein Häkchen steht neben ihr ausgegraut.
        /// </summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Die_ausfuehrliche_Vorlage_fuehrt_jeden_Baustein(bool englisch)
        {
            Pruefbefund b = Pruefe(ExcelAusfuehrlich.Erzeuge(englisch));
            Assert.True(b.HatKapitel);
            Assert.Equal(EXCELBAUSTEINE, b.Bausteine);
        }

        /// <summary>Eine unlesbare Vorlage sagt nichts über die Bausteine — die Häkchenliste bleibt frei.</summary>
        [Fact]
        public void Eine_unlesbare_Vorlage_sagt_nichts()
        {
            Pruefbefund b = ExcelVorlagenpruefer.Pruefe(new byte[] { 1, 2, 3 }, Pruefstufe.Schnell,
                new Pruefkontext { Dateiname = "kaputt.xlsx", Ausgabe = Vorlagenausgabe.Excel });
            Assert.False(b.IstLesbar);
            Assert.False(b.HatKapitel);
            Assert.True(b.Bausteine == null || b.Bausteine.Count == 0);
        }

        // =====================================================================
        //  3 — der Füller: ein abgewählter Baustein wirkt auf die Blätter
        // =====================================================================

        /// <summary>
        /// Ein abgewählter Baustein lässt das Blatt der Vorlage entfallen: Die Blattmarke des
        /// Variantenvergleichs verschwindet samt ihrem Blatt und steht als Hinweis im Ergebnis — dieselbe
        /// Meldung, die eine Marke ohne Inhalt seit je bekommt. Die übrigen Blätter bleiben.
        /// </summary>
        [Fact]
        public void Abgewaehlter_Vergleich_laesst_die_Blattmarke_entfallen()
        {
            byte[] vorlage = Excelprobe.Mappe(wb =>
            {
                wb.Worksheets.Add("Deckblatt").Cell("A1").Value = "{{bericht.titel}}";
                wb.Worksheets.Add("Hier Vergleich").Cell("A1").Value = "{{blatt.vergleich}}";
                wb.Worksheets.Add("Hier Übersicht").Cell("A1").Value = "{{blatt.uebersicht}}";
            });

            var (e, pfad) = Fuelle(vorlage, Gruppe(2), Ohne(BerichtsKonfiguration.B_VERGLEICH), "ohne-vergleich.xlsx");

            Assert.DoesNotContain("Vergleich", Blattnamen(pfad));
            Assert.Contains("Übersicht", Blattnamen(pfad));
            Assert.Contains(e.Hinweise, h => h.Contains("{{blatt.vergleich}}", StringComparison.Ordinal) &&
                                             h.Contains("„Hier Vergleich“", StringComparison.Ordinal));
        }

        /// <summary>
        /// Die Übersicht trägt zwei Häkchen: Mit einem von beiden steht sie noch, erst ohne beide entfällt
        /// ihre Marke. Ein Blatt ohne Marke wird dann auch nicht angehängt.
        /// </summary>
        [Fact]
        public void Die_Uebersicht_entfaellt_erst_ohne_beide_Haekchen()
        {
            Func<byte[]> vorlage = () => Excelprobe.Mappe(wb =>
            {
                wb.Worksheets.Add("Deckblatt").Cell("A1").Value = "{{bericht.titel}}";
                wb.Worksheets.Add("Hier Übersicht").Cell("A1").Value = "{{blatt.uebersicht}}";
            });

            var (_, mitProjekt) = Fuelle(vorlage(), Gruppe(2), Ohne(BerichtsKonfiguration.B_KOMPONENTEN), "mit-projekt.xlsx");
            Assert.Contains("Übersicht", Blattnamen(mitProjekt));

            var (e, ohne) = Fuelle(vorlage(), Gruppe(2),
                Ohne(BerichtsKonfiguration.B_PROJEKT, BerichtsKonfiguration.B_KOMPONENTEN), "ohne-uebersicht.xlsx");
            Assert.DoesNotContain("Übersicht", Blattnamen(ohne));
            Assert.Contains(e.Hinweise, h => h.Contains("{{blatt.uebersicht}}", StringComparison.Ordinal));

            // Ohne Marke hängt das Blatt gar nicht erst an.
            byte[] ohneMarke = Excelprobe.Mappe(wb => wb.Worksheets.Add("Deckblatt").Cell("A1").Value = "{{bericht.titel}}");
            var (_, pfad) = Fuelle(ohneMarke, Gruppe(2),
                Ohne(BerichtsKonfiguration.B_PROJEKT, BerichtsKonfiguration.B_KOMPONENTEN), "ohne-marke.xlsx");
            Assert.DoesNotContain("Übersicht", Blattnamen(pfad));
        }

        /// <summary>
        /// Mit allen Häkchen füllt die Vorlage unverändert: dieselben Blätter in derselben Folge wie ohne
        /// den Entscheid — die Messlatten des Tabellenberichts bleiben damit unberührt.
        /// </summary>
        [Fact]
        public void Mit_allen_Haekchen_bleibt_die_Mappe_unveraendert()
        {
            byte[] vorlage = Excelprobe.Mappe(wb =>
            {
                wb.Worksheets.Add("Deckblatt").Cell("A1").Value = "{{bericht.titel}}";
                wb.Worksheets.Add("Hier Vergleich").Cell("A1").Value = "{{blatt.vergleich}}";
            });

            var (e, pfad) = Fuelle(vorlage, Gruppe(2), Ohne(), "alle-haekchen.xlsx");
            Assert.Equal(new[] { "Deckblatt", "Vergleich", "Übersicht", "Stamm", "Variante A", "Diagrammdaten" },
                         Blattnamen(pfad));
            Assert.DoesNotContain(e.Hinweise, h => h.Contains("{{blatt.vergleich}}", StringComparison.Ordinal));
        }
    }
}

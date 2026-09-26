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
    /// <b>Die ausführliche Excel-Vorlage und der Excel-Baukasten</b> (Etappe BV-E9, Konzept Berichtsvorlagen 4.4, 6.3, 7.1–7.4):
    /// Beide entstehen aus dem Katalog, bestehen den Excel-Prüfer ohne Fehler und füllen sich mit 1030, der Gruppe 1019 und
    /// einer Gruppe mit drei Ständen ohne unbekannte Stelle und ohne übrigen Platzhalter; die Mappe besteht den Validator.
    /// Dazu die Excel-Tabellen je Stand auf dem Musterblatt, die Tabellen mit reiner Excel-Quelle, die Anhang-E-Stelle
    /// als Blatt und Zelle und die Nachbildung jedes erzeugten Blattes aus Einzelelementen (<see cref="Inventar"/>) samt der
    /// Eigenschaft <c>EPOS.Blattanhang</c>. Mit <c>EPOS_BVE9_BEISPIEL</c> (ein Ordner) legt der Fall die leere Vorlage und die gefüllte Mappe
    /// der Gruppe 1019 dort ab.
    /// </summary>
    [Collection("Testdatenbank")]
    public class ExcelAusfuehrlichTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;
        private readonly string _ordner = Probevorlagen.TempOrdner("epos-bv-e9x-");

        public ExcelAusfuehrlichTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose()
        {
            _kultur.Dispose();
            Probevorlagen.Aufraeumen(_ordner);
        }

        private Pruefbefund Pruefe(byte[] vorlage, bool englisch)
        {
            Pruefbefund b = ExcelVorlagenpruefer.Pruefe(vorlage, Pruefstufe.Voll,
                new Pruefkontext { Englisch = englisch, Dateiname = "probe.xlsx", Ausgabe = Vorlagenausgabe.Excel });
            _ausgabe.WriteLine(Probevorlagen.Liste(b));
            return b;
        }

        private (Fuellergebnis Ergebnis, string Pfad) Fuelle(byte[] vorlage, BerichtsDaten daten, string name)
        {
            string ziel = Path.Combine(_ordner, name);
            Fuellergebnis e = new ExcelVorlagenfueller().Fuelle(vorlage, daten, Berichtsdatenproben.VolleKonfiguration(),
                                                                new Erstellerangaben { Firma = "Probe GmbH" }, ziel);
            foreach (string m in e.Meldungen()) _ausgabe.WriteLine(m);
            return (e, ziel);
        }

        /// <summary>Alle Zellen mit übrigen doppelten Klammern (Text und Notizen).</summary>
        private static List<string> Uebrige(string pfad)
        {
            var liste = new List<string>();
            using var wb = new XLWorkbook(pfad);
            foreach (IXLWorksheet ws in wb.Worksheets)
                foreach (IXLCell c in ws.CellsUsed(XLCellsUsedOptions.All))
                {
                    if (c.Value.IsText && c.GetString().Contains("{{", StringComparison.Ordinal))
                        liste.Add(ws.Name + "!" + c.Address + ": " + c.GetString());
                    if (c.HasComment && c.GetComment().Text.Contains("{{", StringComparison.Ordinal))
                        liste.Add(ws.Name + "!" + c.Address + " (Notiz)");
                }
            return liste;
        }

        public static IEnumerable<object[]> Sprachen() { yield return new object[] { false }; yield return new object[] { true }; }

        // =====================================================================
        //  Erzeugen und prüfen
        // =====================================================================

        [Theory]
        [MemberData(nameof(Sprachen))]
        public void Ausfuehrliche_Vorlage_besteht_den_Pruefer_ohne_Fehler_und_ist_wiederholbar(bool englisch)
        {
            byte[] a = ExcelAusfuehrlich.Erzeuge(englisch), b = ExcelAusfuehrlich.Erzeuge(englisch);
            Assert.Equal(BerichtsvorlagenCtrl.Inhaltsschluessel(a), BerichtsvorlagenCtrl.Inhaltsschluessel(b));
            Pruefbefund befund = Pruefe(a, englisch);
            Assert.False(befund.HatFehler, Probevorlagen.Liste(befund));

            string pfad = Path.Combine(_ordner, "leer.xlsx");
            File.WriteAllBytes(pfad, a);
            Assert.Empty(Exceldiagrammbefund.Validierungsfehler(pfad));

            using var wb = new XLWorkbook(pfad);
            // Die vier Blattmarken, die bleiben (Formelmappe, Musterblatt, Checkliste, Diagrammdaten); Notizen an den Elementen.
            var marken = wb.Worksheets.Select(w => w.Cell(1, 1).GetString()).Where(t => t.StartsWith("{{blatt.", StringComparison.Ordinal)).ToList();
            Assert.Equal(new[] { "{{blatt.checkliste}}", "{{blatt.detail}}", "{{blatt.diagrammdaten}}", "{{blatt.wirtschaftlichkeit}}" },
                         marken.OrderBy(s => s, StringComparer.Ordinal));
            Assert.True(wb.Worksheets.Sum(w => w.CellsUsed(XLCellsUsedOptions.Comments).Count()) >= 60);
            Assert.Equal(englisch ? "no" : "nein",
                         wb.CustomProperties.CustomProperty(ExcelVorlagenmappe.EIGENSCHAFT_BLATTANHANG).GetValue<string>());
            Assert.Contains(wb.Worksheets.SelectMany(w => w.Tables), t => t.Name == "EPOS_tabelle__wirtschaft__szenarien");
            Assert.Contains(wb.Worksheets.SelectMany(w => w.Tables), t => t.Name == "EPOS_stand__tabelle__monatswerte");
            Assert.Contains(wb.DefinedNames, n => n.Name == "EPOS.reihe.waermebedarf.monate");
            Assert.Equal(englisch ? "en" : "de", wb.CustomProperties.CustomProperty(Vorlagenpruefer.EIGENSCHAFT_SPRACHE).GetValue<string>());
            Assert.Equal(2, Exceldiagrammbefund.Lies(pfad).Count);
        }

        // =====================================================================
        //  Nachbildung der erzeugten Blätter (Nachtrag BV-E9)
        // =====================================================================

        /// <summary>
        /// Das Inventar der Nachbildung: je erzeugtes Blatt des Standardberichts seine Entsprechung — ein nachgebildetes Blatt
        /// (Name aus der Ressource) mit den Platzhaltern seiner Komponenten, oder die Blattmarke, die bleibt (Formelmappe:
        /// lebende Formeln; Checkliste: Stellen aus der gefüllten Mappe; Diagrammdaten: die Zahlen der Diagramme).
        /// </summary>
        internal static readonly IReadOnlyList<(ExcelBerichtGenerator.Blattart Art, string Blatt, string[] Platzhalter)> Inventar = new[]
        {
            (ExcelBerichtGenerator.Blattart.Uebersicht, nameof(WindowsFormsApplication1.MyResource.Resource.BV_XLA_BLATT_UEBERSICHT), new[]
            {
                "{{projekt.name}}", "{{projekt.kunde}}", "{{projekt.bearbeiter}}", "{{projekt.klimaregion}}", "{{bericht.datum|datum mit zeit}}",
                "{{tabelle.varianten}}", "{{tabelle.komponenten.matrix}}", "{{stamm.bild.speichertemperaturen}}",
            }),
            (ExcelBerichtGenerator.Blattart.Vergleich, nameof(WindowsFormsApplication1.MyResource.Resource.BV_XLA_BLATT_VERGLEICH), new[]
            {
                "{{tabelle.vergleich.liste}}", "{{bild.vergleich.balken.energie.brennstoff}}", "{{bild.vergleich.balken.energie.netzbezug}}",
                "{{bild.vergleich.balken.energie.waermerest}}", "{{bild.vergleich.balken.eff.jaz}}",
            }),
            (ExcelBerichtGenerator.Blattart.Wirtschaftlichkeit, "blatt.wirtschaftlichkeit", new[] { "{{blatt.wirtschaftlichkeit}}" }),
            (ExcelBerichtGenerator.Blattart.Verlauf, nameof(WindowsFormsApplication1.MyResource.Resource.BV_XLA_BLATT_VERLAUF), new[]
            {
                "{{tabelle.wirtschaft.verlauf}}",
            }),
            (ExcelBerichtGenerator.Blattart.Detail, "blatt.detail", new[]
            {
                "{{blatt.detail}}", "{{stand.rolle}} — {{stand.projektname}}", "{{stand.simulationsstand}}", "{{stand.fehler}}",
                "{{stand.tabelle.kennzahlen.liste}}", "{{stand.tabelle.erzeuger}}", "{{stand.tabelle.brennstoffmengen}}",
                "{{stand.bild.waerme_jahresverlauf}}", "{{stand.bild.waerme_dauerlinie}}", "{{stand.bild.strombilanz_monate}}",
                "{{stand.bild.speicherverlauf}}", "{{stand.bild.deckung_waerme}}", "{{stand.bild.deckung_strom}}",
            }),
            (ExcelBerichtGenerator.Blattart.Checkliste, "blatt.checkliste", new[] { "{{blatt.checkliste}}" }),
            (ExcelBerichtGenerator.Blattart.Diagrammdaten, "blatt.diagrammdaten", new[] { "{{blatt.diagrammdaten}}" }),
        };

        /// <summary>Die Wirtschaftlichkeit als Einzelelemente (Blatt „Auswertung“ und Musterblatt) neben der Formelmappe.</summary>
        private static readonly string[] Wirtschaftselemente =
        {
            "{{tabelle.wirtschaft.parameter}}", "{{tabelle.wirtschaft.kennzahlen}}", "{{tabelle.wirtschaft.kennzahlen.guenstig}}",
            "{{tabelle.wirtschaft.kennzahlen.unguenstig}}", "{{tabelle.wirtschaft.szenarien}}", "{{wirtschaft.vorschlag}}",
            "{{tabelle.wirtschaft.nicht_monetaer}}", "{{wirtschaft.hinweise}}", "{{wirtschaft.warnungen}}",
            "{{bild.wirtschaft.kapitalwert_szenarien}}", "{{bild.wirtschaft.barwerte_kumuliert}}", "{{bild.wirtschaft.bruecke}}",
            "{{bild.wirtschaft.spanne}}", "{{stand.tabelle.betriebskosten}}", "{{stand.tabelle.kwkg_module}}",
            "{{stand.tabelle.mehrjahres}}", "{{stand.bild.zahlungsstrom}}",
        };

        [Theory]
        [MemberData(nameof(Sprachen))]
        public void Jedes_Blatt_des_Standardberichts_hat_seine_Entsprechung(bool englisch)
        {
            Assert.Equal(Enum.GetValues(typeof(ExcelBerichtGenerator.Blattart)).Cast<ExcelBerichtGenerator.Blattart>().OrderBy(a => a),
                         Inventar.Select(i => i.Art).OrderBy(a => a));
            Assert.Equal(Inventar.Select(i => i.Art), ExcelAusfuehrlich.Entsprechungen.Select(e => e.Art));

            string pfad = Path.Combine(_ordner, "inventar.xlsx");
            File.WriteAllBytes(pfad, ExcelAusfuehrlich.Erzeuge(englisch));
            using var wb = new XLWorkbook(pfad);
            var feste = new HashSet<string>(ExcelBerichtGenerator.FesteBlattnamen().Values, StringComparer.OrdinalIgnoreCase);
            foreach ((ExcelBerichtGenerator.Blattart art, string blatt, string[] platzhalter) in Inventar)
            {
                string name = blatt.StartsWith("blatt.", StringComparison.Ordinal) ? blatt : ExcelVorlagentexte.T(englisch, blatt);
                IXLWorksheet ws = wb.Worksheets.FirstOrDefault(w => w.Name == name);
                Assert.True(ws != null, art + ": Blatt „" + name + "“ fehlt");
                Assert.DoesNotContain(ws.Name, feste);
                var texte = new HashSet<string>(ws.CellsUsed().Where(c => c.Value.IsText).Select(c => c.GetString()), StringComparer.Ordinal);
                foreach (string p in platzhalter) Assert.True(texte.Contains(p), art + ": " + p + " fehlt auf „" + name + "“");
                // Jedes Element trägt eine Notiz: was es ist und wie man es ändert.
                foreach (IXLCell c in ws.CellsUsed().Where(c => c.Value.IsText && c.GetString().StartsWith("{{", StringComparison.Ordinal)))
                    Assert.True(c.HasComment, name + "!" + c.Address + " ohne Notiz");
            }
            var alle = new HashSet<string>(wb.Worksheets.SelectMany(w => w.CellsUsed()).Where(c => c.Value.IsText).Select(c => c.GetString()));
            foreach (string p in Wirtschaftselemente) Assert.Contains(p, alle);

            // Die Vorlage hängt keine erzeugten Blätter ohne Marke an; der Prüfer nennt die drei, die entfallen.
            Pruefbefund befund = Pruefe(ExcelAusfuehrlich.Erzeuge(englisch), englisch);
            Pruefmeldung m = Assert.Single(befund.Meldungen, x => x.Kennung == nameof(WindowsFormsApplication1.MyResource.Resource.BV_XL_PRUEF_OHNE_ANHANG));
            foreach (string k in new[] { "{{blatt.uebersicht}}", "{{blatt.vergleich}}", "{{blatt.verlauf}}" }) Assert.Contains(k, m.Text);
            Assert.DoesNotContain("{{blatt.detail}}", m.Text);
        }

        /// <summary>Ohne die Eigenschaft hängen die erzeugten Blätter ohne Marke an wie bisher (BV-Q2); mit „nein“ entfallen sie.</summary>
        [Theory]
        [InlineData(null, true)]
        [InlineData("ja", true)]
        [InlineData("nein", false)]
        [InlineData("no", false)]
        [InlineData("FALSE", false)]
        public void Blattanhang_schaltet_das_Anhaengen_der_Blaetter_ohne_Marke(string wert, bool angehaengt)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            byte[] vorlage = Excelprobe.Mappe(wb =>
            {
                IXLWorksheet ws = wb.Worksheets.Add("Eigen");
                ws.Cell(1, 1).Value = "{{projekt.name}}";
                wb.Worksheets.Add("Marke").Cell(1, 1).Value = "{{blatt.vergleich}}";
                if (wert != null) wb.CustomProperties.Add(ExcelVorlagenmappe.EIGENSCHAFT_BLATTANHANG, wert);
            });
            var (e, pfad) = Fuelle(vorlage, ExcelDiagrammeTests.Gruppe1019(), "anhang_" + (wert ?? "ohne") + ".xlsx");
            Assert.Empty(e.Unbekannte);
            using var wb2 = new XLWorkbook(pfad);
            Assert.Contains(wb2.Worksheets, w => w.Name == "Vergleich");          // mit Marke immer
            Assert.Equal(angehaengt, wb2.Worksheets.Any(w => w.Name == "Übersicht"));
            Assert.Equal(angehaengt, wb2.Worksheets.Any(w => w.Name == "Stamm"));
            Assert.Equal(!angehaengt, e.Meldungen().Any(x => x.Contains("{{blatt.uebersicht}}", StringComparison.Ordinal)));
        }

        [Theory]
        [MemberData(nameof(Sprachen))]
        public void Baukasten_fuehrt_jeden_Exceleintrag_und_besteht_den_Pruefer(bool englisch)
        {
            byte[] a = ExcelBaukasten.Erzeuge(englisch);
            Assert.Equal(BerichtsvorlagenCtrl.Inhaltsschluessel(a), BerichtsvorlagenCtrl.Inhaltsschluessel(ExcelBaukasten.Erzeuge(englisch)));
            Pruefbefund befund = Pruefe(a, englisch);
            Assert.False(befund.HatFehler, Probevorlagen.Liste(befund));

            string pfad = Path.Combine(_ordner, "baukasten.xlsx");
            File.WriteAllBytes(pfad, a);
            using var wb = new XLWorkbook(pfad);
            var texte = new HashSet<string>(wb.Worksheets.SelectMany(w => w.CellsUsed()).Where(c => c.Value.IsText).Select(c => c.GetString()));
            IReadOnlyList<Vorlagenfeld> eintraege = ExcelBaukasten.Eintraege();
            foreach (Vorlagenfeld f in eintraege) Assert.Contains("{{" + f.Schluessel + "}}", texte);
            // Jeder Eintrag genau einmal: keine Marke doppelt.
            List<string> marken = wb.Worksheets.SelectMany(w => w.CellsUsed()).Where(c => c.Value.IsText && c.GetString().StartsWith("{{"))
                                    .Select(c => c.GetString()).ToList();
            Assert.Equal(marken.Count, marken.Distinct().Count());
            Assert.DoesNotContain(eintraege, f => f.Art == Vorlagenfeldart.Kapitel || f.Art == Vorlagenfeldart.Schalter
                                                  || f.Kontext == Vorlagenfeldkontext.Gebaeude);
            _ausgabe.WriteLine("Einträge: " + eintraege.Count);
        }

        // =====================================================================
        //  Füllen
        // =====================================================================

        public static IEnumerable<object[]> Proben()
        {
            yield return new object[] { "1030" };
            yield return new object[] { "1019" };
            yield return new object[] { "gruppe3" };
        }

        private static BerichtsDaten Daten(string probe)
        {
            switch (probe)
            {
                case "1030": return BerichtVorlagenMesslatteTests.Probe(BerichtVorlagenMesslatteTests.PROBE_1030);
                case "1019": return ExcelDiagrammeTests.Gruppe1019();
                default: return ExcelDiagrammeTests.Gruppe3();
            }
        }

        [Theory]
        [MemberData(nameof(Proben))]
        public void Ausfuehrliche_Vorlage_fuellt_ohne_unbekannte_Stelle_und_ohne_uebrigen_Platzhalter(string probe)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BerichtsDaten daten = Daten(probe);
            byte[] vorlage = ExcelAusfuehrlich.Erzeuge(false);
            var (e, pfad) = Fuelle(vorlage, daten, "ausfuehrlich_" + probe + ".xlsx");

            Assert.Empty(e.Unbekannte);
            Assert.Empty(e.Warnungen);
            Assert.Empty(Uebrige(pfad));
            Assert.Empty(Exceldiagrammbefund.Validierungsfehler(pfad));

            using var wb = new XLWorkbook(pfad);
            // Je Stand ein Klon des Musterblatts mit seiner eigenen Excel-Tabelle der Monatswerte (freie Namen, keine doppelt).
            List<string> monatstabellen = wb.Worksheets.SelectMany(w => w.Tables).Select(t => t.Name)
                                            .Where(n => n.StartsWith("EPOS_stand__tabelle__monatswerte", StringComparison.Ordinal)).ToList();
            Assert.Equal(daten.Varianten.Count, monatstabellen.Count);
            Assert.Equal(monatstabellen.Count, monatstabellen.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Contains("EPOS_stand__tabelle__monatswerte", monatstabellen);
            Assert.DoesNotContain(wb.Worksheets.SelectMany(w => w.Tables), t => t.Name.StartsWith("EPOSMUSTER_", StringComparison.Ordinal));

            // Die Anhang-E-Stelle nennt Blatt und Zelle dieser Mappe.
            IXLWorksheet liste = wb.Worksheets.FirstOrDefault(w => w.Name == AnhangECheckliste.Blattname);
            if (liste != null)
            {
                List<string> stellen = liste.Column(4).CellsUsed().Select(c => c.GetString()).Where(s => s.Contains("Tabellenbericht")).ToList();
                Assert.NotEmpty(stellen);
                Assert.All(stellen, s => Assert.Contains(", Zelle ", s));
                Assert.Contains(stellen, s => s.Contains("Blatt „Auswertung“", StringComparison.Ordinal));
            }

            // Die nachgebildeten Blätter stehen, die erzeugten Blätter ohne Marke entfallen (EPOS.Blattanhang = nein).
            Assert.DoesNotContain(wb.Worksheets, w => w.Name == "Übersicht" || w.Name == "Vergleich"
                                                     || w.Name == ExcelBerichtGenerator.Verlaufsblattname());
            foreach (string r in new[] { nameof(WindowsFormsApplication1.MyResource.Resource.BV_XLA_BLATT_UEBERSICHT),
                                         nameof(WindowsFormsApplication1.MyResource.Resource.BV_XLA_BLATT_VERGLEICH),
                                         nameof(WindowsFormsApplication1.MyResource.Resource.BV_XLA_BLATT_VERLAUF) })
                Assert.Contains(wb.Worksheets, w => w.Name == ExcelVorlagentexte.T(false, r));

            // Jedes Diagramm des Standardberichts entsteht auch aus der Vorlage (mindestens so oft; die Formelmappe trägt ihre
            // eigenen, „Auswertung“ dieselben noch einmal).
            string standard = Path.Combine(_ordner, "standard_" + probe + ".xlsx");
            new ExcelBerichtGenerator().Erzeuge(daten, Berichtsdatenproben.VolleKonfiguration(), standard);
            List<Exceldiagrammbefund> soll = Exceldiagrammbefund.Lies(standard), ist = Exceldiagrammbefund.Lies(pfad);
            Assert.NotEmpty(soll);
            foreach (IGrouping<string, Exceldiagrammbefund> g in soll.GroupBy(d => d.Name))
                Assert.True(ist.Count(d => d.Name == g.Key) >= g.Count(),
                            g.Key + ": " + ist.Count(d => d.Name == g.Key) + " statt " + g.Count() + " Diagramme");

            string beispiel = Environment.GetEnvironmentVariable("EPOS_BVE9_BEISPIEL");
            if (probe == "1019" && !string.IsNullOrEmpty(beispiel) && Directory.Exists(beispiel))
            {
                File.WriteAllBytes(Path.Combine(beispiel, "Berichtsvorlage_Excel_Ausfuehrlich.xlsx"), vorlage);
                File.Copy(pfad, Path.Combine(beispiel, "Bericht_1019_Excel_ausfuehrlich.xlsx"), true);
            }
        }

        [Theory]
        [MemberData(nameof(Proben))]
        public void Baukasten_fuellt_ohne_unbekannte_Stelle_und_ohne_uebrigen_Platzhalter(string probe)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            var (e, pfad) = Fuelle(ExcelBaukasten.Erzeuge(false), Daten(probe), "baukasten_" + probe + ".xlsx");
            Assert.Empty(e.Unbekannte);
            Assert.Empty(Uebrige(pfad));
            Assert.Empty(Exceldiagrammbefund.Validierungsfehler(pfad));
        }

        /// <summary>Die englische Vorlage füllt einen englischen Bericht ebenso.</summary>
        [Fact]
        public void Englische_Vorlage_fuellt_die_Gruppe_1019()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var en = new Kulturvorrichtung("en-US");
            var (e, pfad) = Fuelle(ExcelAusfuehrlich.Erzeuge(true), ExcelDiagrammeTests.Gruppe1019(), "ausfuehrlich_en.xlsx");
            Assert.Empty(e.Unbekannte);
            Assert.Empty(Uebrige(pfad));
        }

        /// <summary>
        /// Eine Excel-Tabelle je Stand (<c>EPOS_stand__tabelle__*</c>) ist auf dem Musterblatt erlaubt — außerhalb meldet der
        /// Prüfer sie als Fehler (BV-E9, Konzept 7.3).
        /// </summary>
        [Fact]
        public void Excel_Tabelle_je_Stand_nur_auf_dem_Musterblatt()
        {
            byte[] Mappe(bool aufMuster) => Excelprobe.Mappe(wb =>
            {
                IXLWorksheet muster = wb.Worksheets.Add("Muster");
                muster.Cell(1, 1).Value = "{{blatt.detail}}";
                muster.Cell(2, 1).Value = "{{stand.anzeige}}";
                IXLWorksheet ws = aufMuster ? muster : wb.Worksheets.Add("Frei");
                ws.Cell(5, 1).Value = "Monat";
                ws.Cell(5, 2).Value = "Wert";
                ws.Cell(6, 1).Value = "x";
                ws.Range(5, 1, 6, 2).CreateTable("EPOS_stand__tabelle__monatswerte");
            });
            Assert.False(Pruefe(Mappe(true), false).HatFehler);
            Pruefbefund frei = Pruefe(Mappe(false), false);
            Assert.Contains(frei.Meldungen, m => m.Kennung == nameof(WindowsFormsApplication1.MyResource.Resource.BV_XL_PRUEF_TABELLE_STAND));
        }

        // =====================================================================
        //  Die drei Tabellen mit reiner Excel-Quelle
        // =====================================================================

        [Fact]
        public void Drei_Excel_Tabellen_zeigen_die_Zahlen_der_erzeugten_Bloecke()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BerichtsDaten daten = ExcelDiagrammeTests.Gruppe1019();
            Berichtswerte w = Berichtswerte.Aus(daten, Berichtsdatenproben.VolleKonfiguration(), false, new Erstellerangaben());

            foreach (string s in new[] { "tabelle.wirtschaft.parameter", "tabelle.wirtschaft.verlauf", "stand.tabelle.monatswerte" })
            {
                Vorlagenfeld f = Vorlagenfeldkatalog.Finde(s);
                Assert.NotNull(f);
                Assert.Equal(Vorlagenausgabe.Excel, f.Ausgaben);
                Assert.Equal(7, f.Seit);
            }

            Berichtstabelle parameter = Vorlagenfeldkatalog.Loese(Vorlagenfeldkatalog.Finde("tabelle.wirtschaft.parameter"), w, null).Tabelle;
            Assert.NotNull(parameter);
            Assert.Equal(5, parameter.Kopf.Zellen.Count);
            Tabellenzelle zins = parameter.Zeilen[0].Zellen[1];
            Assert.Equal(w.Wirtschaft.Parameter.Zinssatz / 100.0, zins.Zahl.Value, 12);
            Assert.Contains("%", zins.Excelformat);
            Assert.Equal(2, parameter.Hinweise.Count);

            Berichtstabelle verlauf = Vorlagenfeldkatalog.Loese(Vorlagenfeldkatalog.Finde("tabelle.wirtschaft.verlauf"), w, null).Tabelle;
            Assert.NotNull(verlauf);
            Assert.False(verlauf.Listentauglich);
            Assert.True(verlauf.Zeilen.Count > 10, verlauf.ToString());

            Berichtswerte stamm = w.MitStand(daten.Varianten[0]);
            Berichtstabelle monate = Vorlagenfeldkatalog.Loese(Vorlagenfeldkatalog.Finde("stand.tabelle.monatswerte"), stamm, null).Tabelle;
            Assert.True(monate.Listentauglich, monate.ToString());
            Assert.Equal(12, monate.Zeilen.Count);
            double[] waerme = ChartRenderer.MonatsSummenMWh(daten.Varianten[0].Zeitreihen.Hole(ZeitreihenSatz.WAERMEBEDARF));
            Assert.Equal(waerme[0], monate.Zeilen[0].Zellen[1].Zahl.Value, 9);
        }

        // =====================================================================
        //  Katalog v9: Vergleichsliste und Kennzahlblock der erzeugten Blätter
        // =====================================================================

        /// <summary>
        /// <c>tabelle.vergleich.liste</c> und <c>stand.tabelle.kennzahlen.liste</c> (Katalog v9, nur Excel) zeigen Zelle für Zelle
        /// die Zahlen des Blattes „Vergleich“ und des Kennzahlblocks des Detailblatts — die Δ-Zellen als Ergebnis ihrer Formel.
        /// </summary>
        [Fact]
        public void Vergleichsliste_und_Kennzahlliste_zeigen_die_Zahlen_der_erzeugten_Blaetter()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BerichtsDaten daten = ExcelDiagrammeTests.Gruppe1019();
            Berichtswerte w = Berichtswerte.Aus(daten, Berichtsdatenproben.VolleKonfiguration(), false, new Erstellerangaben());
            foreach (string s in new[] { "tabelle.vergleich.liste", "stand.tabelle.kennzahlen.liste" })
            {
                Vorlagenfeld f = Vorlagenfeldkatalog.Finde(s);
                Assert.NotNull(f);
                Assert.Equal(Vorlagenausgabe.Excel, f.Ausgaben);
                Assert.Equal(9, f.Seit);
            }

            Berichtstabelle liste = Vorlagenfeldkatalog.Loese(Vorlagenfeldkatalog.Finde("tabelle.vergleich.liste"), w, null).Tabelle;
            Assert.NotNull(liste);
            Assert.True(liste.Listentauglich, liste.ToString());
            using (var wb = new XLWorkbook())
            {
                ExcelBerichtGenerator.BlattVergleich(wb, daten, new Formelregister());
                IXLWorksheet ws = wb.Worksheets.Last();
                int zeilen = ws.LastRowUsed().RowNumber() - 1;
                Assert.Equal(zeilen, liste.Zeilen.Count);
                for (int j = 0; j < liste.Kopf.Zellen.Count; j++) Assert.Equal(ws.Cell(1, j + 1).GetString(), liste.Kopf.Zellen[j].Text);
                int deltas = 0;
                for (int i = 0; i < zeilen; i++)
                    for (int j = 0; j < liste.Kopf.Zellen.Count; j++)
                    {
                        IXLCell c = ws.Cell(i + 2, j + 1);
                        Tabellenzelle z = liste.Zeilen[i].Zellen[j];
                        if (c.HasFormula) deltas++;
                        if (c.Value.IsNumber) Assert.Equal(c.Value.GetNumber(), z.Zahl.Value, 9);
                        else Assert.Equal(c.GetString(), z.Text);
                    }
                Assert.True(daten.Varianten.Count < 2 || deltas > 0, "keine Δ-Formel im Blatt „Vergleich“");
            }

            VariantenDaten stamm = daten.Varianten[0];
            Berichtstabelle kennzahlen = Vorlagenfeldkatalog.Loese(Vorlagenfeldkatalog.Finde("stand.tabelle.kennzahlen.liste"),
                                                                   w.MitStand(stamm), null).Tabelle;
            Assert.True(kennzahlen.Listentauglich, kennzahlen.ToString());
            Assert.Equal(4, kennzahlen.Kopf.Zellen.Count);
            Assert.Equal(stamm.Kennzahlen.Count(k => k.Value.HasValue && KennzahlenKatalog.Alle(stamm.EmissionsModus).Any(x => x.Schluessel == k.Key)),
                         kennzahlen.Zeilen.Count);
            Assert.True(kennzahlen.Zeilen.Count > 14, "mehr als die 14 Kernkennzahlen");
        }
    }
}

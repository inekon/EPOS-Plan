using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Katalogzeile = EPOS.UI.Dialoge.Berichte.Katalogzeile;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Positionsadressierung</b> (Etappe BV-E9, Entscheidfrage BV-Q10 Lesart b; Konzept Berichtsvorlagen 4.5, 4.7):
    /// <c>stand.&lt;n&gt;.*</c> zählt die Stände wie <c>{{#je stand}}</c> (1 = Stamm), <c>variante.&lt;n&gt;.*</c> nur die
    /// Varianten; beide gelten außerhalb der Blöcke. Der Katalog bildet die Einträge aus dem Muster, statt sie aufzuzählen.
    /// Geprüft mit 0, 1, 3 und 7 Varianten, einer Position über der Zahl der Stände, in Word und Excel, im Prüfer, in der
    /// Katalogansicht und im Baukasten.
    /// </summary>
    [Collection("Testdatenbank")]
    public class VorlagenfeldPositionenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;
        private readonly string _ordner = Probevorlagen.TempOrdner("epos-bv-e9-");

        public VorlagenfeldPositionenTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose()
        {
            _kultur.Dispose();
            Probevorlagen.Aufraeumen(_ordner);
        }

        // =====================================================================
        //  Katalog: Muster statt Aufzählung
        // =====================================================================

        /// <summary>
        /// <see cref="Vorlagenfeldkatalog.Finde"/> löst <c>stand.3.kennzahl.eff.jaz</c> über das Muster auf: Art, Format,
        /// Einheit, Leerwert und Ausgaben wie das Vorbild, Kontext Gruppe, Fassung 7, derselbe Eintrag bei jedem Nachschlagen.
        /// Kein Eintrag steht in <see cref="Vorlagenfeldkatalog.Alle"/>; Position 0, führende Null, ein Paarschlüssel oder ein
        /// Wert ohne Vorbild im Kontext Stand sind keine Positionsschlüssel.
        /// </summary>
        [Fact]
        public void Finde_bildet_den_Eintrag_aus_dem_Muster()
        {
            Vorlagenfeld vorbild = Vorlagenfeldkatalog.Finde("stand.kennzahl.eff.jaz");
            Vorlagenfeld f = Vorlagenfeldkatalog.Finde("stand.3.kennzahl.eff.jaz");
            Assert.NotNull(vorbild);
            Assert.NotNull(f);
            Assert.Equal("stand.3.kennzahl.eff.jaz", f.Schluessel);
            Assert.Equal(vorbild.Art, f.Art);
            Assert.Equal(vorbild.Format, f.Format);
            Assert.Equal(vorbild.Einheit, f.Einheit);
            Assert.Equal(vorbild.Leerwert, f.Leerwert);
            Assert.Equal(vorbild.Ausgaben, f.Ausgaben);
            Assert.Equal(Vorlagenfeldkontext.Gruppe, f.Kontext);
            Assert.Equal(7, f.Seit);
            Assert.Same(f, Vorlagenfeldkatalog.Finde(" Stand.3.Kennzahl.Eff.Jaz "));
            Assert.Equal(Vorlagenfeldkatalog.MUSTER_STAND_POSITION, f.Ableitung.Muster);
            Assert.Contains("3", Vorlagenfeldkatalog.Beschreibung(f, false));
            Assert.Contains(Vorlagenfeldkatalog.Beschreibung(vorbild, false), Vorlagenfeldkatalog.Beschreibung(f, false));
            Assert.Contains("position 3", Vorlagenfeldkatalog.Beschreibung(f, true));

            Vorlagenfeld v = Vorlagenfeldkatalog.Finde("variante.12.anzeige");
            Assert.Equal("variante.12.anzeige", v?.Schluessel);
            Assert.Equal(Vorlagenfeldkatalog.MUSTER_VARIANTE_POSITION, v.Ableitung.Muster);

            Assert.DoesNotContain(Vorlagenfeldkatalog.Alle, e => Vorlagenfeldkatalog.IstPositionsschluessel(e.Schluessel, out _, out _, out _));
            foreach (string kein in new[] { "stand.0.anzeige", "stand.01.anzeige", "stand.3", "stand.3.", "stand.1000.anzeige",
                                            "stand.2.a.anzeige", "stand.2.gibtsnicht", "variante.1.projekt.name", "variante.x.anzeige" })
                Assert.Null(Vorlagenfeldkatalog.Finde(kein));

            // Die Sicht des Prüfers bildet denselben Eintrag; eine ältere Fassung kennt ihn nicht.
            Assert.Equal(f.Schluessel, Vorlagenkatalogsicht.Standard.Finde("stand.3.kennzahl.eff.jaz")?.Schluessel);
            Assert.Null(new Vorlagenkatalogsicht(Vorlagenfeldkatalog.Alle, 6).Finde("stand.3.kennzahl.eff.jaz"));
        }

        // =====================================================================
        //  Werte: 0, 1, 3 und 7 Varianten
        // =====================================================================

        /// <summary>
        /// Jede Position trägt den Wert, den der Block im n-ten Durchlauf trägt: <c>stand.n</c> den n-ten Stand von
        /// <see cref="Berichtswerte.Staende"/> (1 = Stamm), <c>variante.n</c> die n-te Variante. Eine Position über der
        /// Zahl bleibt leer mit Grund („Stand 5 nicht gewählt“), nie 0.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(7)]
        public void Jede_Position_traegt_den_Wert_ihres_Stands(int varianten)
        {
            Berichtswerte w = Berichtswerte.Aus(Gruppe(varianten + 1), null, false, null);
            Assert.Equal(varianten + 1, w.Staende.Count);

            foreach (string rest in new[] { "anzeige", "rolle", "kennzahl.eff.jaz", "kennzahl.energie.waermebedarf" })
            {
                Vorlagenfeld vorbild = Vorlagenfeldkatalog.Finde("stand." + rest);
                for (int n = 1; n <= w.Staende.Count; n++)
                {
                    string soll = Vorlagenfeldkatalog.Loese(vorbild, w.MitStand(w.Staende[n - 1]), null).Text;
                    Assert.Equal(soll, Wert(w, "stand." + n + "." + rest).Text);
                }
                for (int n = 1; n <= w.Varianten.Count; n++)
                {
                    string soll = Vorlagenfeldkatalog.Loese(vorbild, w.MitStand(w.Varianten[n - 1]), null).Text;
                    Assert.Equal(soll, Wert(w, "variante." + n + "." + rest).Text);
                }
            }
            Assert.Equal("Stammprojekt", Wert(w, "stand.1.anzeige").Text);
            if (varianten > 0)
            {
                Assert.Equal(Wert(w, "variante.1.anzeige").Text, Wert(w, "stand.2.anzeige").Text);
                Assert.Contains("Variante A", Wert(w, "variante.1.anzeige").Text);
            }

            Platzhalterwert ueber = Wert(w, "stand." + (varianten + 2) + ".kennzahl.eff.jaz");
            Assert.True(ueber.IstLeer);
            Assert.Equal(Vorlagenfeld.STRICH, ueber.Text);
            Assert.Equal("Stand " + (varianten + 2) + " nicht gewählt", ueber.Grund);
            Platzhalterwert ueberVariante = Wert(w, "variante." + (varianten + 1) + ".anzeige");
            Assert.True(ueberVariante.IstLeer);
            Assert.Equal("Variante " + (varianten + 1) + " nicht gewählt", ueberVariante.Grund);

            Berichtswerte en = Berichtswerte.Aus(Gruppe(varianten + 1), null, true, null);
            Assert.Equal("Case " + (varianten + 2) + " not selected", Wert(en, "stand." + (varianten + 2) + ".anzeige").Grund);
        }

        // =====================================================================
        //  Word und Excel
        // =====================================================================

        /// <summary>
        /// <b>Word</b>: Positionen außerhalb der Blöcke füllen ohne Kontextfehler und ohne gelbe Stelle; eine Strukturtabelle
        /// je Position steht wie die des Blocks; eine Position über der Zahl der Stände zeigt den Leerwert samt Grund
        /// (<c>|mit grund</c> an einer Zahl).
        /// </summary>
        [Fact]
        public void Word_fuellt_Positionen_ausserhalb_der_Bloecke()
        {
            BerichtsDaten daten = Gruppe(4);   // Stamm und drei Varianten
            Berichtswerte w = Berichtswerte.Aus(daten, null, false, null);
            string tabelle = Vorlagenfeldkatalog.Alle
                .Where(f => f.Kontext == Vorlagenfeldkontext.Stand && f.Art == Vorlagenfeldart.Tabelle)
                .Select(f => f.Schluessel)
                .FirstOrDefault(s => Vorlagenfeldkatalog.Loese(Vorlagenfeldkatalog.Finde(s), w.MitStand(w.Staende[2]), null).Tabelle != null);
            _ausgabe.WriteLine("Tabelle je Stand: " + (tabelle ?? "keine mit Zeilen"));

            var absaetze = new List<string>
            {
                "{{stand.1.anzeige}}", "{{stand.3.anzeige}}", "{{variante.3.anzeige}}",
                "JAZ {{variante.2.kennzahl.eff.jaz}}", "{{stand.9.kennzahl.eff.jaz|mit grund}}",
            };
            if (tabelle != null) absaetze.Add("{{stand.3." + tabelle.Substring("stand.".Length) + "}}");
            string ziel = Path.Combine(_ordner, "positionen.docx");
            Fuellergebnis e = new WordBerichtGenerator().ErzeugeMitVorlage(daten, new BerichtsKonfiguration(),
                Probevorlagen.AusAbsaetzen(absaetze.ToArray()), new Erstellerangaben(), ziel);
            foreach (string m in e.Meldungen()) _ausgabe.WriteLine(m);
            Assert.Empty(e.Unbekannte.Select(u => u.Normalform));

            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            Body rumpf = doc.MainDocumentPart.Document.Body;
            List<string> texte = rumpf.Elements<Paragraph>().Select(p => p.InnerText).Where(t => t.Length > 0).ToList();
            foreach (string t in texte) _ausgabe.WriteLine("Absatz: " + t);
            Assert.Equal("Stammprojekt", texte[0]);
            Assert.Equal(Wert(w, "stand.3.anzeige").Text, texte[1]);
            Assert.Contains("Variante B", texte[1]);
            Assert.Contains("Variante C", texte[2]);
            Assert.Equal("JAZ " + Wert(w, "variante.2.kennzahl.eff.jaz").Text, texte[3]);
            Assert.Equal("— (Stand 9 nicht gewählt)", texte[4]);
            Assert.DoesNotContain("{{", rumpf.InnerText, StringComparison.Ordinal);
            if (tabelle != null)
            {
                Berichtstabelle soll = Vorlagenfeldkatalog.Loese(Vorlagenfeldkatalog.Finde(tabelle), w.MitStand(w.Staende[2]), null).Tabelle;
                Assert.Equal(soll.Zeilen.Count + (soll.Kopf != null ? 1 : 0), rumpf.Elements<Table>().Single().Elements<TableRow>().Count());
            }
        }

        /// <summary>
        /// <b>Excel</b>: Zellplatzhalter nach Position gelten auf jedem Blatt (Kontext Gruppe) — eine Zahl als Zahl, ein Text
        /// als Text, dieselben Werte wie in Word; eine fehlende Position gibt eine leere Zelle, mit <c>|mit grund</c> den Grund.
        /// </summary>
        [Fact]
        public void Excel_fuellt_Positionen_auf_jedem_Blatt()
        {
            byte[] vorlage = Excelprobe.Mappe(wb =>
            {
                IXLWorksheet ws = wb.Worksheets.Add("Deckblatt");
                ws.Cell("A1").Value = "{{stand.1.anzeige}}";
                ws.Cell("A2").Value = "{{variante.2.anzeige}}";
                ws.Cell("A3").Value = "{{stand.3.kennzahl.energie.waermebedarf}}";
                ws.Cell("A4").Value = "{{variante.3.kennzahl.eff.jaz}}";
                ws.Cell("A5").Value = "{{variante.3.kennzahl.eff.jaz|mit grund}}";
            });
            BerichtsDaten daten = Gruppe(3);   // Stamm und zwei Varianten
            string ziel = Path.Combine(_ordner, "positionen.xlsx");
            Fuellergebnis e = new ExcelVorlagenfueller().Fuelle(vorlage, daten, new BerichtsKonfiguration(),
                                                                new Erstellerangaben(), ziel);
            foreach (string m in e.Meldungen()) _ausgabe.WriteLine(m);
            Assert.Empty(e.Unbekannte.Select(u => u.Normalform));

            Berichtswerte w = Berichtswerte.Aus(daten, null, false, null);
            using var wb = new XLWorkbook(ziel);
            IXLWorksheet d = wb.Worksheet("Deckblatt");
            Assert.Equal("Stammprojekt", d.Cell("A1").GetString());
            Assert.Equal(Wert(w, "variante.2.anzeige").Text, d.Cell("A2").GetString());
            Assert.Equal(1236.5, d.Cell("A3").GetDouble());
            Assert.True(d.Cell("A4").Value.IsBlank, "Ohne Position eine leere Zelle, nie 0.");
            Assert.Equal("— (Variante 3 nicht gewählt)", d.Cell("A5").GetString());
        }

        /// <summary>
        /// <b>Excel, Tabellen und Bilder je Position</b>: Eine Tabelle je Stand nach Position wird wie jede Tabelle des Berichts
        /// ein erzeugter Bereich an ihrer Zelle; ein Bild je Position kennt die Excel-Seite nicht (seine Diagrammdaten hängen am
        /// Schlüssel des Bildes) — der Prüfer nennt es „später“, der Füller lässt es stehen.
        /// </summary>
        [Fact]
        public void Excel_Tabelle_je_Position_als_Bereich_Bild_je_Position_spaeter()
        {
            byte[] vorlage = Excelprobe.Mappe(wb =>
            {
                IXLWorksheet ws = wb.Worksheets.Add("Tafel");
                ws.Cell("A1").Value = "{{stand.2.tabelle.kennzahlen}}";
                ws.Cell("H1").Value = "{{stand.2.bild.waerme_dauerlinie}}";
            });
            Pruefbefund befund = ExcelVorlagenpruefer.Pruefe(vorlage, Pruefstufe.Voll,
                new Pruefkontext { AnzahlVarianten = 2, Ausgabe = Vorlagenausgabe.Excel, Dateiname = "Probe.xlsx" });
            _ausgabe.WriteLine(Probevorlagen.Liste(befund));
            Assert.Empty(befund.UnbekannteSchluessel);
            Assert.Contains("{{stand.2.bild.waerme_dauerlinie}}", Assert.Single(Probevorlagen.Mit(befund, "VF_PRUEF_SPAETER")).Text);

            BerichtsDaten daten = Gruppe(3);
            string ziel = Path.Combine(_ordner, "tafel.xlsx");
            Fuellergebnis e = new ExcelVorlagenfueller().Fuelle(vorlage, daten, new BerichtsKonfiguration(), new Erstellerangaben(), ziel);
            foreach (string m in e.Meldungen()) _ausgabe.WriteLine(m);
            Assert.Equal(new[] { "{{stand.2.bild.waerme_dauerlinie}}" }, e.Unbekannte.Select(u => u.Normalform));

            Berichtswerte w = Berichtswerte.Aus(daten, null, false, null);
            Berichtstabelle soll = Vorlagenfeldkatalog.Loese(Vorlagenfeldkatalog.Finde("stand.tabelle.kennzahlen"), w.MitStand(w.Staende[1]), null).Tabelle;
            Assert.NotNull(soll);
            using var wb = new XLWorkbook(ziel);
            IXLWorksheet t = wb.Worksheet("Tafel");
            Assert.NotEqual("{{stand.2.tabelle.kennzahlen}}", t.Cell("A1").GetString());
            Assert.Contains(t.CellsUsed(), c => c.GetString() == soll.Zeilen[0].Zellen[0].Text);
        }

        // =====================================================================
        //  Prüfer
        // =====================================================================

        /// <summary>
        /// Der Prüfer kennt die Positionen (kein unbekannter Schlüssel, kein Kontextfehler außerhalb der Blöcke) und gibt
        /// einen Hinweis, wenn die Vorlage mehr Positionen nutzt, als der Lauf hat — einmal je Art (Stand, Variante), in Word
        /// und Excel. Reicht die Auswahl, schweigt er.
        /// </summary>
        [Fact]
        public void Pruefer_weist_auf_Positionen_ueber_der_Auswahl_hin()
        {
            byte[] word = Probevorlagen.AusAbsaetzen("{{stand.3.anzeige}}", "{{stand.5.anzeige}}", "{{stand.4.rolle}}",
                                                     "{{variante.2.kennzahl.eff.jaz}}");
            Pruefbefund zwei = Vorlagenpruefer.Pruefe(word, Pruefstufe.Schnell, new Pruefkontext { AnzahlVarianten = 2 });
            _ausgabe.WriteLine(Probevorlagen.Liste(zwei));
            Assert.Empty(zwei.UnbekannteSchluessel);
            Assert.DoesNotContain(zwei.Meldungen, m => m.Stufe == Befundstufe.Fehler);
            Pruefmeldung m = Assert.Single(Probevorlagen.Mit(zwei, "VF_PRUEF_POSITION"));
            Assert.Equal(Befundstufe.Hinweis, m.Stufe);
            Assert.Equal("Vorlage nutzt {{stand.5.anzeige}}, gewählt sind 3 Stände (2 Varianten) – die Stelle bleibt leer", m.Text);

            Pruefbefund eine = Vorlagenpruefer.Pruefe(word, Pruefstufe.Schnell, new Pruefkontext { AnzahlVarianten = 1 });
            Assert.Equal(2, Probevorlagen.Mit(eine, "VF_PRUEF_POSITION").Count);   // Stand und Variante
            Pruefbefund vier = Vorlagenpruefer.Pruefe(word, Pruefstufe.Schnell, new Pruefkontext { AnzahlVarianten = 4 });
            Assert.Empty(Probevorlagen.Mit(vier, "VF_PRUEF_POSITION"));

            byte[] excel = Excelprobe.Mappe(wb =>
            {
                IXLWorksheet ws = wb.Worksheets.Add("Deckblatt");
                ws.Cell("A1").Value = "{{variante.3.anzeige}}";
                ws.Cell("A2").Value = "{{stand.2.kennzahl.eff.jaz}}";
            });
            Pruefbefund xl = ExcelVorlagenpruefer.Pruefe(excel, Pruefstufe.Voll,
                new Pruefkontext { AnzahlVarianten = 2, Ausgabe = Vorlagenausgabe.Excel, Dateiname = "Probe.xlsx" });
            _ausgabe.WriteLine(Probevorlagen.Liste(xl));
            Assert.Empty(xl.UnbekannteSchluessel);
            Assert.DoesNotContain(xl.Meldungen, x => x.Stufe == Befundstufe.Fehler);
            Assert.Contains("{{variante.3.anzeige}}", Assert.Single(Probevorlagen.Mit(xl, "VF_PRUEF_POSITION")).Text);
        }

        // =====================================================================
        //  Katalogansicht
        // =====================================================================

        /// <summary>Die Katalogansicht zeigt die zwei Muster als eigene Zeilen (Art „Muster“), mit Beschreibung in beiden Sprachen.</summary>
        [Fact]
        public void Katalogansicht_zeigt_die_Muster()
        {
            List<Katalogzeile> de = BerichtsvorlagenGaben.Positionszeilen(false).ToList();
            Assert.Equal(new[] { Vorlagenfeldkatalog.MUSTER_STAND_POSITION, Vorlagenfeldkatalog.MUSTER_VARIANTE_POSITION },
                         de.Select(z => z.Schluessel));
            Assert.All(de, z => Assert.Equal("Muster", z.Art));
            Assert.All(de, z => Assert.Contains("Stammprojekt", z.Beschreibung));
            Assert.All(BerichtsvorlagenGaben.Positionszeilen(true), z => Assert.Contains("base project", z.Beschreibung));
            var eintraege = (IReadOnlyList<Katalogzeile>)BerichtsvorlagenGaben.KatalogGaben()["Eintraege"];
            Assert.Contains(eintraege, z => z.Schluessel == Vorlagenfeldkatalog.MUSTER_STAND_POSITION);
            foreach (Vorlagenfeldmuster muster in Vorlagenfeldkatalog.Positionsmuster)
                Assert.NotNull(Vorlagenfeldkatalog.Finde(muster.Beispiel));
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private static Platzhalterwert Wert(Berichtswerte w, string schluessel)
        {
            Vorlagenfeld f = Vorlagenfeldkatalog.Finde(schluessel);
            Assert.True(f != null, schluessel + " unbekannt");
            return Vorlagenfeldkatalog.Loese(f, w, null);
        }

        /// <summary>Die synthetische Gruppe mit gesetzten Kennzahlen je Stand (ohne Datenbank füllbar).</summary>
        private static BerichtsDaten Gruppe(int staende)
        {
            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(staende);
            for (int i = 0; i < daten.Varianten.Count; i++)
            {
                daten.Varianten[i].Kennzahlen["energie.waermebedarf"] = 1234.5 + i;
                daten.Varianten[i].Kennzahlen["eff.jaz"] = 3.0 + 0.25 * i;
            }
            return daten;
        }
    }
}

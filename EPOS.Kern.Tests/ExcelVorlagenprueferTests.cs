using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Prüfer der Excel-Vorlagen</b> (Konzept Berichtsvorlagen 4.4, 4.7, 4.10, 7.1–7.4, 8.5; Etappe BV-E7): unbekannte
    /// Schlüssel mit Vorschlag, Schlüssel ohne Ausgabe Excel, Tabellen und Bilder (BV-E8), Blöcke, Kontextverstöße,
    /// Blattmarken (nicht in A1, nicht leer, doppelt, Bezüge), gleichnamige Anwenderblätter, reservierte Namen, Namen auf
    /// Bereichen, Formeln (Hinweis), Format und Makros, Sprache und der Paketschutz der vollen Stufe.
    /// </summary>
    public class ExcelVorlagenprueferTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static Pruefbefund Pruefe(byte[] vorlage, Pruefstufe stufe = Pruefstufe.Schnell, string datei = null, bool englisch = false)
        {
            return ExcelVorlagenpruefer.Pruefe(vorlage, stufe, new Pruefkontext { Englisch = englisch, Dateiname = datei, Ausgabe = Vorlagenausgabe.Excel });
        }

        private static List<Pruefmeldung> Mit(Pruefbefund b, string kennung)
        {
            return b.Meldungen.Where(m => m.Kennung == kennung).ToList();
        }

        /// <summary>Die Standardmappe (nur Blattmarken) und eine Vorlage mit gültigen Platzhaltern prüfen ohne Befund — in
        /// beiden Stufen, samt Paketschutz.</summary>
        [Fact]
        public void Standardmappe_und_gueltige_Vorlage_ohne_Befund()
        {
            foreach (Pruefstufe stufe in new[] { Pruefstufe.Schnell, Pruefstufe.Voll })
            {
                Pruefbefund standard = Pruefe(ExcelVorlagenfueller.Standardmappe(), stufe, "Standard.xlsx");
                Assert.True(standard.OhneBefund, Probevorlagen.Liste(standard));
                Assert.True(standard.IstLesbar);
                Assert.Equal(6, standard.AnzahlPlatzhalter);
                Assert.True(standard.HatWirtschaftlichkeit);

                Pruefbefund gut = Pruefe(Excelprobe.Mappe(wb =>
                {
                    IXLWorksheet ws = wb.Worksheets.Add("Deckblatt");
                    ws.Cell("A1").Value = "{{bericht.titel}}";
                    ws.Cell("A2").Value = "Stand: {{bericht.datum|datum lang}}";
                    ws.Cell("A3").Value = "{{bericht.warnungen}}";
                    wb.DefinedNames.Add("EPOS.projekt.kunde", ws.Range("B1"));
                    wb.DefinedNames.Add("EPOS_stamm__kennzahl__eff__jaz", "=0");
                    IXLWorksheet m = wb.Worksheets.Add("Muster");
                    m.Cell("A1").Value = "{{blatt.detail}}";
                    m.Cell("A2").Value = "{{stand.kennzahl.eff.jaz|stellen 1}}";
                }), stufe, "Gut.xltx");
                Assert.True(gut.OhneBefund, Probevorlagen.Liste(gut));
                Assert.Equal(7, gut.AnzahlPlatzhalter);
                Assert.Contains("projekt.kunde", gut.Schluessel);
                Assert.Contains("stamm.kennzahl.eff.jaz", gut.Schluessel);
                Assert.False(gut.HatWirtschaftlichkeit);
            }
        }

        /// <summary>Die Fehler an Zellplatzhaltern: unbekannt (mit Vorschlag), ohne Ausgabe Excel, Tabelle und Bild (erst
        /// BV-E8), Block, Wert je Stand außerhalb des Musterblatts, Wert je Gebäude, Liste im Satz, unbekannte Formatangabe
        /// — je mit Fundort „Blatt …, Zelle …“.</summary>
        [Fact]
        public void Fehler_an_Zellplatzhaltern()
        {
            Pruefbefund b = Pruefe(Excelprobe.Mappe(wb =>
            {
                IXLWorksheet ws = wb.Worksheets.Add("Deckblatt");
                ws.Cell("A1").Value = "{{projekt.kundename}}";
                ws.Cell("A2").Value = "{{kapitel.vergleich}}";
                ws.Cell("A3").Value = "{{tabelle.varianten}}";
                ws.Cell("A4").Value = "{{#je stand}}";
                ws.Cell("A5").Value = "{{stand.anzeige}}";
                ws.Cell("A6").Value = "{{gebaeude.name}}";
                ws.Cell("A7").Value = "Hinweise: {{bericht.warnungen}}";
                ws.Cell("A8").Value = "{{bericht.datum|stellen 2}}";
                ws.Cell("A9").Value = "{{bild.ersteller.logo}}";
                ws.Cell("B1").Value = "{{bericht.titel|farbe rot}}";
            }));

            Assert.True(b.IstLesbar);
            Pruefmeldung unbekannt = Assert.Single(Mit(b, nameof(R.VF_PRUEF_UNBEKANNT)));
            Assert.Equal("Blatt „Deckblatt“, Zelle A1", unbekannt.Fundort);
            Assert.Equal("{{projekt.kunde}}", unbekannt.Vorschlag);
            Assert.Contains("projekt.kundename", b.UnbekannteSchluessel);
            Assert.Equal(3, Mit(b, nameof(R.VF_PRUEF_ORT)).Count);   // Kapitel und Logo (ohne Excel), Liste im Satz
            Assert.Contains(Mit(b, nameof(R.VF_PRUEF_ORT)), m => m.Marke == "{{kapitel.vergleich}}");
            Assert.Single(Mit(b, nameof(R.VF_PRUEF_SPAETER)));
            Assert.Single(Mit(b, nameof(R.BV_XL_PRUEF_BLOCK)));
            Pruefmeldung kontext = Assert.Single(Mit(b, nameof(R.VF_PRUEF_KONTEXT_STAND)));
            Assert.Contains("{{blatt.detail}}", kontext.Text);
            Assert.Single(Mit(b, nameof(R.BV_XL_PRUEF_GEBAEUDE)));
            Assert.Single(Mit(b, nameof(R.VF_PRUEF_ANGABE_UNPASSEND)));
            Assert.Single(Mit(b, nameof(R.VF_PRUEF_ANGABE_UNBEKANNT)));
            Assert.True(b.HatFehler);
        }

        /// <summary>Blattmarken (Konzept 7.2): nicht in A1 bzw. nicht allein, doppelt, auf einem Blatt mit Inhalt (Warnung,
        /// außer dem Musterblatt), Bezüge des Anwenders auf ein Markenblatt (Warnung); ein Anwenderblatt, das heißt wie ein
        /// erzeugtes Blatt, ist ein Fehler.</summary>
        [Fact]
        public void Blattmarken_und_Blattnamen()
        {
            Pruefbefund b = Pruefe(Excelprobe.Mappe(wb =>
            {
                IXLWorksheet a = wb.Worksheets.Add("Deckblatt");
                a.Cell("B2").Value = "{{blatt.uebersicht}}";
                a.Cell("C3").FormulaA1 = "='Mein Vergleich'!A1";
                wb.Worksheets.Add("Mein Vergleich").Cell("A1").Value = "{{blatt.vergleich}}";
                IXLWorksheet voll = wb.Worksheets.Add("Voll");
                voll.Cell("A1").Value = "{{blatt.verlauf}}";
                voll.Cell("A2").Value = "Notiz";
                wb.Worksheets.Add("Zweiter").Cell("A1").Value = "{{blatt.vergleich}}";
                wb.Worksheets.Add("Übersicht").Cell("A1").Value = "eigene Übersicht";
                IXLWorksheet m = wb.Worksheets.Add("Muster");
                m.Cell("A1").Value = "{{blatt.detail}}";
                m.Cell("A2").Value = "{{stand.anzeige}}";
            }));

            Assert.Single(Mit(b, nameof(R.BV_XL_PRUEF_BLATT_ORT)));
            Assert.Equal("Blatt „Zweiter“, Zelle A1", Assert.Single(Mit(b, nameof(R.BV_XL_PRUEF_BLATT_DOPPELT))).Fundort);
            Pruefmeldung nichtLeer = Assert.Single(Mit(b, nameof(R.BV_XL_PRUEF_BLATT_NICHT_LEER)));
            Assert.Equal(Befundstufe.Warnung, nichtLeer.Stufe);
            Assert.Contains("„Voll“", nichtLeer.Text);
            Pruefmeldung bezug = Assert.Single(Mit(b, nameof(R.BV_XL_PRUEF_BLATT_BEZUG)));
            Assert.Contains("Deckblatt", bezug.Text);
            Pruefmeldung name = Assert.Single(Mit(b, nameof(R.BV_XL_PRUEF_BLATTNAME)));
            Assert.Equal(Befundstufe.Fehler, name.Stufe);
            Assert.Contains("{{blatt.uebersicht}}", name.WasTun);
            Assert.Empty(Mit(b, nameof(R.VF_PRUEF_KONTEXT_STAND)));   // auf dem Musterblatt gültig
            Assert.Single(Mit(b, nameof(R.BV_XL_PRUEF_FORMELN)));
        }

        /// <summary>Namen (Konzept 4.4, 7.4): reservierte Namen der Formelmappe (auch mit Szenarioanhang und blattweit) sind
        /// Fehler, ein EPOS-Name auf mehreren Zellen ebenso, ein unbekannter EPOS-Name mit Fundort „Name …“.</summary>
        [Fact]
        public void Reservierte_Namen_und_Namen_auf_Bereichen()
        {
            Pruefbefund b = Pruefe(Excelprobe.Mappe(wb =>
            {
                IXLWorksheet ws = wb.Worksheets.Add("Werte");
                wb.DefinedNames.Add("Zins_i", ws.Range("A1"));
                wb.DefinedNames.Add("p_E_Unguenstig", "=0.03");
                ws.DefinedNames.Add("Risiko_Zuschlag", ws.Range("A2"));
                wb.DefinedNames.Add("EPOS.bericht.titel", ws.Range("B1:B4"));
                wb.DefinedNames.Add("EPOS_projekt__gibtsnicht", ws.Range("C1"));
                wb.DefinedNames.Add("Eigener_Name", ws.Range("D1"));
            }));

            List<Pruefmeldung> reserviert = Mit(b, nameof(R.BV_XL_PRUEF_RESERVIERT));
            Assert.Equal(3, reserviert.Count);
            Assert.All(reserviert, m => Assert.Equal(Befundstufe.Fehler, m.Stufe));
            Assert.Single(Mit(b, nameof(R.BV_XL_PRUEF_NAME_BEREICH)));
            Assert.Equal("Name „EPOS_projekt__gibtsnicht“", Assert.Single(Mit(b, nameof(R.VF_PRUEF_UNBEKANNT))).Fundort);
            Assert.Contains("projekt.gibtsnicht", b.UnbekannteSchluessel);
        }

        /// <summary>
        /// <b>Paketschutz</b> (Konzept 7.4) in der vollen Stufe: Probe-Laden und -Speichern, verlorene Teile mit Namen (Formen); die schnelle Stufe lädt nur. Format und Makros: <c>.xlsm</c> und eine Mappe mit Makros sind Fehler,
        /// eine Datei ohne Mappe unlesbar, ein Word-Dokument ebenso.
        /// </summary>
        [Fact]
        public void Paketschutz_Format_und_Makros()
        {
            byte[] mitForm = Excelprobe.MitForm(Excelprobe.Mappe(wb =>
                wb.Worksheets.Add("Deckblatt").Cell("A1").Value = "{{bericht.titel}}"));
            Pruefbefund voll = Pruefe(mitForm, Pruefstufe.Voll, "Form.xlsx");
            Pruefmeldung verlust = Assert.Single(Mit(voll, nameof(R.BV_XL_PRUEF_VERLUST)));
            Assert.Equal(Befundstufe.Warnung, verlust.Stufe);
            Assert.Contains("Formen", verlust.Text);
            Assert.Empty(Mit(Pruefe(mitForm, Pruefstufe.Schnell), nameof(R.BV_XL_PRUEF_VERLUST)));

            byte[] mappe = Excelprobe.Mappe(wb => wb.Worksheets.Add("A").Cell("A1").Value = "x");
            Pruefbefund makros = Pruefe(Excelprobe.MitMakros(mappe));
            Assert.Single(Mit(makros, nameof(R.VF_PRUEF_MAKROS)));
            Assert.Single(Mit(makros, nameof(R.VF_PRUEF_FORMAT)));
            Assert.Single(Mit(Pruefe(mappe, Pruefstufe.Voll, "Mappe.xlsm"), nameof(R.VF_PRUEF_FORMAT)));

            Pruefbefund word = Pruefe(Probevorlagen.AusAbsaetzen("{{bericht.titel}}"));
            Assert.False(word.IstLesbar);
            Assert.True(word.HatFehler);
            Pruefbefund text = Pruefe(Encoding.UTF8.GetBytes("keine Mappe"));
            Assert.False(text.IstLesbar);
            Assert.Contains(R.BV_XL_PRUEF_GRUND_KEIN_EXCEL, Assert.Single(text.Meldungen).Text);
            Assert.False(Pruefe(Array.Empty<byte>()).IstLesbar);
        }

        /// <summary>Die Sprache aus <c>custom.xml</c> (Konzept 4.9): eine englische Vorlage im deutschen Bericht warnt; eine
        /// Mappe ohne Platzhalter bekommt den Hinweis, dass die erzeugten Blätter hinten anhängen.</summary>
        [Fact]
        public void Sprache_und_Mappe_ohne_Platzhalter()
        {
            byte[] mappe = Excelprobe.Mappe(wb => wb.Worksheets.Add("A").Cell("A1").Value = "nur Text");
            Pruefbefund ohne = Pruefe(mappe);
            Assert.Equal(Befundstufe.Hinweis, Assert.Single(ohne.Meldungen).Stufe);

            Pruefbefund englisch = Pruefe(Excelprobe.MitEigenschaft(mappe, Vorlagenpruefer.EIGENSCHAFT_SPRACHE, "en"));
            Assert.True(englisch.SpracheAbweichend);
            Assert.Single(Mit(englisch, nameof(R.VF_PRUEF_SPRACHE)));
            Assert.False(Pruefe(Excelprobe.MitEigenschaft(mappe, Vorlagenpruefer.EIGENSCHAFT_SPRACHE, "en"), englisch: true).SpracheAbweichend);
        }
    }
}

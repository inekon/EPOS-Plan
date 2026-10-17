using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>VW1b (E88) — die Vorlaufwahl der Wärmepumpe im Bericht</b>: die Kennzahlen der Stunden unter und über den
    /// Kennlinienstützstellen (Summe über die Module mit Wert, sonst null), die Katalogfassung 14 und die Zeilen im
    /// Heizkreisabschnitt, die nur bei belegtem <c>Vorlaufwahl_Stunden</c> erscheinen.
    /// </summary>
    public sealed class VorlaufwahlBerichtTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static ErgebnisWaermepumpeModulModel Modul(string name, string wahl, int? darunter, int? darueber)
            => new ErgebnisWaermepumpeModulModel
            {
                Modul = name, Vorlaufwahl_Stunden = wahl,
                Vorlauf_Darunter_Stunden = darunter, Vorlauf_Darueber_Stunden = darueber,
            };

        private static VariantenDaten Mit(params ErgebnisWaermepumpeModulModel[] module)
        {
            var e = new ErgebnisModel { Waermepumpe = new ErgebnisWaermepumpeModel() };
            e.Waermepumpe.Module.AddRange(module);
            return new VariantenDaten { Ergebnis = e };
        }

        private static double? Wert(string schluessel, VariantenDaten v)
            => KennzahlenKatalog.Alle().Single(k => k.Schluessel == schluessel).Wert(v);

        [Fact]
        public void Die_Kennzahlen_summieren_die_Module_mit_Wert_und_fehlen_ohne_Kennlinienwahl()
        {
            VariantenDaten v = Mit(Modul("WP 1", "35:1549;45:1782;55:122", 1851, 0),
                                   Modul("WP 2", "35:10;45:0;55:0", 4, 2),
                                   Modul("WP 3", null, null, null));
            Assert.Equal(1855.0, Wert(KennzahlenKatalog.SCHLUESSEL_WP_VORLAUF_DARUNTER, v));
            Assert.Equal(2.0, Wert(KennzahlenKatalog.SCHLUESSEL_WP_VORLAUF_DARUEBER, v));

            VariantenDaten ohne = Mit(Modul("WP", null, null, null));
            Assert.Null(Wert(KennzahlenKatalog.SCHLUESSEL_WP_VORLAUF_DARUNTER, ohne));
            Assert.Null(Wert(KennzahlenKatalog.SCHLUESSEL_WP_VORLAUF_DARUEBER, ohne));
            Assert.Null(Wert(KennzahlenKatalog.SCHLUESSEL_WP_VORLAUF_DARUNTER, new VariantenDaten()));

            foreach (string s in new[] { KennzahlenKatalog.SCHLUESSEL_WP_VORLAUF_DARUNTER, KennzahlenKatalog.SCHLUESSEL_WP_VORLAUF_DARUEBER })
            {
                Kennzahl k = KennzahlenKatalog.Alle().Single(x => x.Schluessel == s);
                Assert.NotEqual(k.LabelDe, k.LabelEn);
                Assert.Equal("h/a", k.Einheit);
                Assert.Equal(14, Vorlagenfeldkatalog.SeitDerKennzahl(s));
            }
        }

        [Fact]
        public void Das_Mengenszenario_laesst_die_Stunden_der_Vorlaufwahl_unveraendert()
        {
            VariantenDaten v = Mit(Modul("WP 1", "35:1549;45:1782;55:122", 1851, 0), Modul("WP 2", null, null, null));
            ErgebnisModel k = SzenarioMengen.Ergebnis(v.Ergebnis, 1.1);
            Assert.Equal("35:1549;45:1782;55:122", k.Waermepumpe.Module[0].Vorlaufwahl_Stunden);
            Assert.Equal(1851, k.Waermepumpe.Module[0].Vorlauf_Darunter_Stunden);
            Assert.Equal(0, k.Waermepumpe.Module[0].Vorlauf_Darueber_Stunden);
            Assert.Null(k.Waermepumpe.Module[1].Vorlaufwahl_Stunden);
            Assert.Null(k.Waermepumpe.Module[1].Vorlauf_Darunter_Stunden);
        }

        [Fact]
        public void Die_Texte_nennen_Stuetzstellen_und_Stunden_im_Zahlformat_der_Kultur()
        {
            CultureInfo de = CultureInfo.GetCultureInfo("de-DE");
            Assert.Equal("35 °C 1.549 h, 45 °C 1.782 h, 55 °C 122 h",
                         ProjektbeschreibungBaustein.VorlaufwahlText(de, "35:1549;45:1782;55:122"));
            Assert.Equal("darunter 1.851 h, darüber 0 h", ProjektbeschreibungBaustein.AusserhalbText(de, 1851, 0));
            Assert.Equal("—", ProjektbeschreibungBaustein.VorlaufwahlText(de, null));
        }

        // =================================================================
        // Word: die Zeilen im Heizkreisabschnitt
        // =================================================================

        private static BerichtsDaten Daten(params ErgebnisWaermepumpeModulModel[] module)
        {
            var erg = new ErgebnisModel { Waermepumpe = new ErgebnisWaermepumpeModel() };
            erg.Waermepumpe.Module.AddRange(module);
            erg.Gebaeude.Add(new ErgebnisGebaeudeModel
            {
                ID_Gebaeude = 1, Merkplatz = 1, Gebaeudename = "Haus A",
                Rechenweg = DbWerte.GEBAEUDE_MODELL_VDI6007, HeizwaermeMwh = 70.0, SpitzeKw = 30.0,
                UebergabeArt = DbWerte.UEBERGABE_RADIATOR, VorlaufMittelC = 40.0, RuecklaufMittelC = 33.0,
                UebergabeBegrenztStundenH = 0.0,
            });
            erg.Energiebedarf = new ErgebnisEnergiebedarfModel();
            var eingaben = new DataTable();
            eingaben.Columns.Add("ID", typeof(long));
            eingaben.Rows.Add(1L);
            var d = new BerichtsDaten { Stammprojektname = "Probe" };
            d.Varianten.Add(new VariantenDaten
            {
                IstStamm = true, Projektname = "Probe", Ergebnis = erg,
                Details = new ProjektDetails { IdProjekt = 1, Gebaeude = eingaben }
            });
            return d;
        }

        private static string Schreibe(BerichtsDaten daten)
        {
            using var ms = new MemoryStream();
            using WordprocessingDocument doc = WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
            MainDocumentPart main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body());
            new ProjektbeschreibungBaustein().SchreibeWord(new WordKontext(main, main.Document.Body, null), daten,
                                                           BerichtsKonfiguration.Standard());
            return string.Join("\n", main.Document.Body.Descendants<Text>().Select(t => t.Text));
        }

        [Fact]
        public void Die_Zeilen_stehen_nur_bei_belegter_Vorlaufwahl()
        {
            string mit = Schreibe(Daten(Modul("WP Sole", "35:1549;45:1782;55:122", 1851, 0), Modul("WP ohne", null, null, null)));
            int ab = mit.IndexOf(ProjektbeschreibungBaustein.UEBERSCHRIFT_HEIZKREIS, StringComparison.Ordinal);
            Assert.True(ab >= 0, "Heizkreisabschnitt fehlt");
            string teil = mit.Substring(ab);
            Assert.Contains("Vorlaufwahl der Kennlinie", teil);
            Assert.Contains("35 °C 1.549 h, 45 °C 1.782 h, 55 °C 122 h", teil);
            Assert.Contains("Stunden außerhalb der Stützstellen", teil);
            Assert.Contains("darunter 1.851 h, darüber 0 h", teil);
            Assert.Contains("WP Sole", teil);
            Assert.DoesNotContain("WP ohne", mit);

            string ohne = Schreibe(Daten(Modul("WP ohne", null, null, null)));
            Assert.Contains(ProjektbeschreibungBaustein.UEBERSCHRIFT_HEIZKREIS, ohne);
            Assert.DoesNotContain("Vorlaufwahl der Kennlinie", ohne);
            Assert.DoesNotContain("Stunden außerhalb der Stützstellen", ohne);
        }
    }
}

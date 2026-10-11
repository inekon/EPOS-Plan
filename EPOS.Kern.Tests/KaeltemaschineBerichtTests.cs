using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KU3-4 — die Kältemaschine im Bericht</b>: Kennzahlen der Gruppe Kälte und die Zeilen der Tafel
    /// <c>tabelle.kaelteerzeuger</c> aus <c>Tab_ErgebnisKaeltemaschine</c>; ohne Kältemaschine bleibt alles leer.
    /// </summary>
    public sealed class KaeltemaschineBerichtTests
    {
        private static VariantenDaten Mit(params ErgebnisKaeltemaschineModel[] km)
        {
            var e = new ErgebnisModel();
            e.Kaeltemaschinen.AddRange(km);
            return new VariantenDaten { Ergebnis = e };
        }

        private static double? Wert(string schluessel, VariantenDaten v)
            => KennzahlenKatalog.Alle().Single(k => k.Schluessel == schluessel).Wert(v);

        [Fact]
        public void Die_Kennzahlen_summieren_die_Maschinen_und_fehlen_ohne_Maschine()
        {
            VariantenDaten v = Mit(
                new ErgebnisKaeltemaschineModel { Bezeichner = "A", Kaelteproduktion_MWh = 30, Stromverbrauch_MWh = 10, Hilfsstrom_MWh = 1, FreieKuehlung_MWh = 4, FreieKuehlung_Stunden = 100, Taktstunden = 7 },
                new ErgebnisKaeltemaschineModel { Bezeichner = "B", Kaelteproduktion_MWh = 10, Stromverbrauch_MWh = 2.5, Hilfsstrom_MWh = 0.5, Taktstunden = 3 });
            Assert.Equal(40.0, Wert(KennzahlenKatalog.SCHLUESSEL_KM_ERZEUGUNG, v));
            Assert.Equal(12.5, Wert(KennzahlenKatalog.SCHLUESSEL_KM_STROM, v));
            Assert.Equal(1.5, Wert(KennzahlenKatalog.SCHLUESSEL_KM_HILFSSTROM, v));
            Assert.Equal(3.2, Wert(KennzahlenKatalog.SCHLUESSEL_KM_JAZ, v)!.Value, 9);
            Assert.Equal(4.0, Wert(KennzahlenKatalog.SCHLUESSEL_KM_FREI, v));
            Assert.Equal(100.0, Wert(KennzahlenKatalog.SCHLUESSEL_KM_FREI_STUNDEN, v));
            Assert.Equal(10.0, Wert(KennzahlenKatalog.SCHLUESSEL_KM_TAKT, v));

            VariantenDaten ohne = Mit();
            foreach (string s in new[] { KennzahlenKatalog.SCHLUESSEL_KM_ERZEUGUNG, KennzahlenKatalog.SCHLUESSEL_KM_JAZ,
                                         KennzahlenKatalog.SCHLUESSEL_KM_TAKT, KennzahlenKatalog.SCHLUESSEL_KAELTE_REST })
                Assert.Null(Wert(s, ohne));
            // Zweisprachig: jede neue Kennzahl trägt ein englisches Label, das vom deutschen abweicht.
            Assert.All(KennzahlenKatalog.Alle().Where(k => k.Schluessel.StartsWith("kaelte.km.")),
                       k => Assert.NotEqual(k.LabelDe, k.LabelEn));
        }

        [Fact]
        public void Die_Tafel_der_Kaelteerzeuger_fuehrt_die_Maschinen_mit_Anzahl()
        {
            var km = new List<ErgebnisKaeltemaschineModel>
            {
                new() { Bezeichner = "KM Halle", Anzahl = 2, Kaelteproduktion_MWh = 30, Stromverbrauch_MWh = 10 },
                new() { Bezeichner = "aus", Kaelteproduktion_MWh = 0 },
            };
            Berichtstabelle t = Berichtstabellen.Kaelteerzeuger(null, _ => "", false, CultureInfo.GetCultureInfo("de-DE"), km);
            Assert.False(t.IstLeer);
            Assert.Single(t.Zeilen);   // eine Maschine; die Maschine ohne Kälte fehlt
            Assert.Equal("KM Halle (2 ×)", t.Zeilen[0].Zellen[0].Text);
            Assert.Equal(3.0, t.Zeilen[0].Zellen[3].Zahl!.Value, 9);
            Assert.True(Berichtstabellen.Kaelteerzeuger(null, _ => "", true, CultureInfo.InvariantCulture).IstLeer);
        }

        /// <summary>
        /// KM4-E2 (Konzept Kältemittel als Stammdatum 5.3): Mit einem Code führt die Tafel die Spalte „Kältemittel“ an
        /// zweiter Stelle — die Kältemaschine über ihr Gerät, die Wärmepumpe über den Anlagennamen der Modulzeile; ein
        /// Erzeuger der Klasse 2 trägt einen Stern, die Fußnote steht einmal unter der Tafel. Ohne Code bleibt die Tafel,
        /// wie sie war.
        /// </summary>
        [Fact]
        public void Die_Tafel_der_Kaelteerzeuger_fuehrt_das_Kaeltemittel_mit_Stern_und_Fussnote()
        {
            var kultur = CultureInfo.GetCultureInfo("de-DE");
            var km = new List<ErgebnisKaeltemaschineModel>
            {
                new() { ID_Kaeltemaschine = 7, Bezeichner = "KM 10", Kaelteproduktion_MWh = 5, Stromverbrauch_MWh = 1 },
                new() { ID_Kaeltemaschine = 8, Bezeichner = "KM 20", Kaelteproduktion_MWh = 9, Stromverbrauch_MWh = 3 },
            };
            var wp = new ErgebnisWaermepumpeModel();
            wp.Module.Add(new ErgebnisWaermepumpeModulModel { Modul = "WP Halle", Kaelteproduktion = 4, Stromverbrauch_Kuehlung = 1 });
            var kaeltemittel = new List<KaeltemittelBerichtszeile>
            {
                new(KaeltemittelGeraeteart.Kaeltemaschine, 7, "KM 10", "R513A", KaeltemittelErgebnisklasse.AbStichtag),
                new(KaeltemittelGeraeteart.Kaeltemaschine, 8, "KM 20", "R513A", KaeltemittelErgebnisklasse.Zulaessig),
                new(KaeltemittelGeraeteart.Waermepumpe, 99, "WP Halle", "R290", null),
            };

            Berichtstabelle t = Berichtstabellen.Kaelteerzeuger(wp, _ => "", false, kultur, km, kaeltemittel);
            Assert.Equal("Kältemittel", t.Kopf.Zellen[1].Text);
            Assert.Equal(7, t.Kopf.Zellen.Count);
            Assert.Equal(new[] { "R290", "R513A *", "R513A" }, t.Zeilen.Select(z => z.Zellen[1].Text).ToArray());
            Assert.Equal(Berichtstabellen.FUSSNOTE_KAELTEMITTEL, Assert.Single(t.Hinweise));

            Berichtstabelle en = Berichtstabellen.Kaelteerzeuger(wp, _ => "", true, kultur, km, kaeltemittel);
            Assert.Equal("Refrigerant", en.Kopf.Zellen[1].Text);
            Assert.StartsWith("* For the planned year", Assert.Single(en.Hinweise));

            // Ohne Klasse 2 kein Stern und keine Fußnote; ohne Code keine Spalte.
            Berichtstabelle ohneStern = Berichtstabellen.Kaelteerzeuger(wp, _ => "", false, kultur, km, kaeltemittel.Skip(1).ToList());
            Assert.Empty(ohneStern.Hinweise);
            Assert.Equal("—", ohneStern.Zeilen[1].Zellen[1].Text);
            Berichtstabelle ohne = Berichtstabellen.Kaelteerzeuger(wp, _ => "", false, kultur, km);
            Assert.Equal(6, ohne.Kopf.Zellen.Count);
            Assert.Equal("Anlage", ohne.Kopf.Zellen[0].Text);
            Assert.Equal("Kälte [MWh/a]", ohne.Kopf.Zellen[1].Text);
        }
    }
}

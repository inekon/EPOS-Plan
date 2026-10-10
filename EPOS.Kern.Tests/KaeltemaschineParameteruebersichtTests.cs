using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Erzeuger;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KD-3: Parameterübersicht und Kennlinienbilder der Kältemaschine</b> — die Werte des Vergleichs je Satz
    /// (Typkennfeld und Auslieferungssatz der Testdatenbank, dazu ein Typkennfeld ohne Datenbank) und die Reihen der
    /// zwei Bilder (eine Linie je Kaltwassertemperatur, Renderer der Wärmepumpe).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemaschineParameteruebersichtTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private bool MitKatalog => _db.Vorhanden && KaeltemaschineSchema.Vollstaendig();

        private static string Wert(IReadOnlyList<Parameterwert> w, string spalte)
            => w.Single(p => p.Eintrag.Spalte == spalte).Wert;

        [Fact]
        public void Die_Uebersicht_fuehrt_vierzehn_Zeilen_in_fester_Folge()
        {
            IReadOnlyList<Parameterwert> leer = KaeltemaschineParameteruebersicht.Werte((KaeltemaschineModel)null, false);

            Assert.Equal(new[]
            {
                KaeltemaschineSchema.SPALTE_NENNKAELTELEISTUNG, KaeltemaschineSchema.SPALTE_NENN_EER,
                KaeltemaschineSchema.SPALTE_RUECKKUEHLART, "Typ", KaeltemaschineTeillastSchema.SPALTE_VERDICHTERREGELUNG,
                KaeltemaschineSchema.SPALTE_KAELTEMITTEL, KaeltemaschineSchema.SPALTE_MINDESTTEILLAST,
                KaeltemaschineSchema.SPALTE_HILFSSTROM_RUECKKUEHLUNG, KaeltemaschineSchema.SPALTE_KALTWASSER_VORLAUF_MIN,
                KaeltemaschineParameteruebersicht.ZEILE_G25, KaeltemaschineParameteruebersicht.ZEILE_G50,
                KaeltemaschineParameteruebersicht.ZEILE_G75, KaeltemaschineParameteruebersicht.ZEILE_STUETZSTELLEN,
                KaeltemaschineParameteruebersicht.ZEILE_HERKUNFT
            }, leer.Select(p => p.Eintrag.Spalte).ToArray());
            Assert.All(leer, p => Assert.Equal(ParameterVerwendung.LEER, p.Wert));
            Assert.Equal("EER-Verhältnis g(0,25)", leer.Single(p => p.Eintrag.Spalte == KaeltemaschineParameteruebersicht.ZEILE_G25).Eintrag.Anzeigetext);
            Assert.Equal(R.KM_VGL_STUETZSTELLEN, leer.Single(p => p.Eintrag.Spalte == KaeltemaschineParameteruebersicht.ZEILE_STUETZSTELLEN).Eintrag.Anzeigetext);
        }

        [Fact]
        public void Ein_Typkennfeld_ohne_Datenbank_nennt_Lesezeile_Stuetzstellen_und_Herkunft()
        {
            KaeltemaschinenTypkennfelder.Typkennfeld tk = KaeltemaschinenTypkennfelder.Lesen().First();
            KaeltemaschineModel m = tk.Modell();
            KaeltemaschineTeillastDialogrechnung.Lesestand l = KaeltemaschineTeillastDialogrechnung.Lesezeile(m);

            IReadOnlyList<Parameterwert> w = KaeltemaschineParameteruebersicht.Werte(m, true);

            Assert.Equal(m.Kennlinie.Count.ToString(CultureInfo.CurrentCulture), Wert(w, KaeltemaschineParameteruebersicht.ZEILE_STUETZSTELLEN));
            Assert.Equal(R.KM_HERKUNFT_TYPKENNFELD, Wert(w, KaeltemaschineParameteruebersicht.ZEILE_HERKUNFT));
            Assert.Equal(Math.Round(l.G50, 2).ToString(CultureInfo.CurrentCulture), Wert(w, KaeltemaschineParameteruebersicht.ZEILE_G50));
            Assert.Equal(KaeltemaschineStammCtrl.RueckkuehlartText(m.Rueckkuehlart), Wert(w, KaeltemaschineSchema.SPALTE_RUECKKUEHLART));
        }

        [Fact]
        public void Die_Testdatenbank_liefert_Auslieferungssatz_und_Typkennfeld()
        {
            if (!MitKatalog) return;

            IReadOnlyList<Parameterwert> geraet = KaeltemaschineParameteruebersicht.Werte("Kältemaschine 200 kW wassergekühlt mit Trockenkühler");
            Assert.Equal("200", Wert(geraet, KaeltemaschineSchema.SPALTE_NENNKAELTELEISTUNG));
            Assert.Equal(R.KM_RUECKKUEHLART_TROCKENKUEHLER, Wert(geraet, KaeltemaschineSchema.SPALTE_RUECKKUEHLART));
            Assert.Equal("6", Wert(geraet, KaeltemaschineParameteruebersicht.ZEILE_STUETZSTELLEN));
            Assert.Equal(R.KM_HERKUNFT_AUSLIEFERUNG, Wert(geraet, KaeltemaschineParameteruebersicht.ZEILE_HERKUNFT));
            Assert.Equal(ParameterVerwendung.LEER, Wert(geraet, KaeltemaschineTeillastSchema.SPALTE_VERDICHTERREGELUNG));
            Assert.NotEqual(ParameterVerwendung.LEER, Wert(geraet, KaeltemaschineParameteruebersicht.ZEILE_G25));

            IReadOnlyList<Parameterwert> tk = KaeltemaschineParameteruebersicht.WerteZuId(4);   // Typkennfeld Luft Scroll 20 kW
            Assert.Equal("20", Wert(tk, KaeltemaschineSchema.SPALTE_NENNKAELTELEISTUNG));
            Assert.Equal("24", Wert(tk, KaeltemaschineParameteruebersicht.ZEILE_STUETZSTELLEN));
            Assert.Equal(R.KM_HERKUNFT_TYPKENNFELD, Wert(tk, KaeltemaschineParameteruebersicht.ZEILE_HERKUNFT));
            Assert.Equal(R.KM_REGELUNG_EIN_AUS, Wert(tk, KaeltemaschineTeillastSchema.SPALTE_VERDICHTERREGELUNG));
            Assert.Equal("Typkennfeld", Wert(tk, "Typ"));

            Assert.All(KaeltemaschineParameteruebersicht.Werte("gibt es nicht"), p => Assert.Equal(ParameterVerwendung.LEER, p.Wert));
        }

        [Fact]
        public void Die_Kennlinienbilder_zeichnen_eine_Linie_je_Kaltwassertemperatur()
        {
            var d = new KaeltemaschineDaten { Bezeichner = "X", RueckkuehlartIndex = 0 };
            d.Kennlinie.Add(new KaeltemaschinePunktDaten { Rueckkuehltemperatur = 35, Kaltwassertemperatur = 12, Eer = 3.5, Kaelteleistung = 50 });
            d.Kennlinie.Add(new KaeltemaschinePunktDaten { Rueckkuehltemperatur = 25, Kaltwassertemperatur = 12, Eer = 4.5, Kaelteleistung = 55 });
            d.Kennlinie.Add(new KaeltemaschinePunktDaten { Rueckkuehltemperatur = 35, Kaltwassertemperatur = 6, Eer = 3.0, Kaelteleistung = null });
            d.Kennlinie.Add(new KaeltemaschinePunktDaten { Rueckkuehltemperatur = null, Kaltwassertemperatur = 7, Eer = 3.0 });

            IReadOnlyList<ChartRenderer.KennlinienReihe> eer = KaeltemaschineKennlinienbild.Reihen(d, p => p.Eer);
            Assert.Equal(new[] { 6, 12 }, eer.Select(r => r.Vorlauf).ToArray());
            Assert.Equal(new[] { 25.0, 35.0 }, eer[1].Punkte.Select(p => p.Temperatur).ToArray());   // nach x sortiert
            IReadOnlyList<ChartRenderer.KennlinienReihe> leistung = KaeltemaschineKennlinienbild.Reihen(d, p => p.Kaelteleistung);
            Assert.Equal(new[] { 12 }, leistung.Select(r => r.Vorlauf).ToArray());                    // 6 °C ohne Leistung

            KaeltemaschineKennlinienbilder b = KaeltemaschineKennlinienbild.Modelle(d);
            Assert.NotNull(b.Eer);
            Assert.NotNull(b.Leistung);

            Assert.Same(KaeltemaschineKennlinienbilder.Leer, KaeltemaschineKennlinienbild.Modelle(null));
            KaeltemaschineKennlinienbilder ohne = KaeltemaschineKennlinienbild.Modelle(new KaeltemaschineDaten { Bezeichner = "Y" });
            Assert.Null(ohne.Eer);
            Assert.Null(ohne.Leistung);
        }
    }
}

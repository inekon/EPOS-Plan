using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>UB‑E4 — die Bivalenz im Bericht</b> (Fachkonzept Übergabegrenze 7.1–7.4): die Kennzahlen <c>wp.bivalenz.*</c>
    /// in beiden Sprachen, Bild <c>stand.bild.wp_bivalenz</c> und Tafel <c>stand.tabelle.bivalenz</c> in Katalog v17,
    /// die Tafel mit Bereichswerten und Hinweiszeilen, der Platzhalter ohne Übergabedaten und die Schlüsselwerte des
    /// Exports mit den Spaltennamen.
    /// </summary>
    public sealed class BivalenzBerichtTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static Bereichskennzahlen Bereiche(bool modul) => new Bereichskennzahlen
        {
            Stunden = new int?[] { 4200, 1300, 250, 90 },
            Mwh = new double?[] { 180.5, 95.25, 12.0, 0.0 },
            Spreizung_Unterschritten_h = 3,
            Ruecklauf_Ueberschritten_h = 0,
            Bivalenzpunkt_1 = modul ? -4.2 : (double?)null,
            Bivalenzpunkt_2 = modul ? -9.8 : (double?)null,
            Uebergabe_Max_kW = modul ? 41.5 : (double?)null,
        };

        private static VariantenDaten Stand(bool mitBereichen = true, BivalenzBerichtswerte bivalenz = null)
        {
            var e = new ErgebnisModel { Waermepumpe = new ErgebnisWaermepumpeModel() };
            if (mitBereichen)
            {
                e.Waermepumpe.Bereiche = Bereiche(false);
                e.Waermepumpe.Module.Add(new ErgebnisWaermepumpeModulModel { Modul = "WP 1", Bereiche = Bereiche(true) });
            }
            return new VariantenDaten { Ergebnis = e, Bivalenz = bivalenz };
        }

        private static double? Wert(string schluessel, VariantenDaten v)
            => KennzahlenKatalog.Alle().Single(k => k.Schluessel == schluessel).Wert(v);

        [Fact]
        public void Die_Kennzahlen_wp_bivalenz_stehen_in_beiden_Sprachen_und_seit_Katalog_17()
        {
            IReadOnlyList<string> schluessel = KennzahlenKatalog.BivalenzSchluessel;
            Assert.Equal(13, schluessel.Count);
            foreach (string s in schluessel)
            {
                Kennzahl k = KennzahlenKatalog.Alle().Single(x => x.Schluessel == s);
                Assert.False(string.IsNullOrWhiteSpace(k.LabelDe), s);
                Assert.False(string.IsNullOrWhiteSpace(k.LabelEn), s);
                Assert.NotEqual(k.LabelDe, k.LabelEn);
                Assert.Equal(Vorlagenfeldkatalog.FASSUNG_BIVALENZ, Vorlagenfeldkatalog.SeitDerKennzahl(s));
                Assert.Matches("^wp\\.bivalenz\\.[a-z0-9]+(_[a-z0-9]+)*$", s);
            }
        }

        [Fact]
        public void Die_Kennzahlen_lesen_Summe_und_Modul_und_fehlen_ohne_Bereiche()
        {
            VariantenDaten v = Stand();
            Assert.Equal(4200.0, Wert("wp.bivalenz.wp_allein_stunden", v));
            Assert.Equal(95.25, Wert("wp.bivalenz.parallel_mwh", v));
            Assert.Equal(90.0, Wert("wp.bivalenz.nur_kessel_stunden", v));
            Assert.Equal(-4.2, Wert("wp.bivalenz.punkt_1", v));
            Assert.Equal(-9.8, Wert("wp.bivalenz.punkt_2", v));
            Assert.Equal(41.5, Wert("wp.bivalenz.uebergabe_max", v));
            Assert.Equal(3.0, Wert("wp.bivalenz.spreizung_unterschritten", v));
            Assert.Equal(0.0, Wert("wp.bivalenz.ruecklauf_ueberschritten", v));

            foreach (string s in KennzahlenKatalog.BivalenzSchluessel)
            {
                Assert.Null(Wert(s, Stand(mitBereichen: false)));
                Assert.Null(Wert(s, new VariantenDaten()));
            }
        }

        [Fact]
        public void Bild_und_Tafel_stehen_in_Katalog_17()
        {
            Assert.Equal(17, Vorlagenfeldkatalog.KATALOGFASSUNG);
            Vorlagenfeld bild = Vorlagenfeldkatalog.Finde("stand.bild.wp_bivalenz");
            Assert.NotNull(bild);
            Assert.Equal(Vorlagenfeldart.Bild, bild.Art);
            Assert.Equal(Vorlagenfeldkontext.Stand, bild.Kontext);
            Assert.Equal(17, bild.Seit);
            Assert.NotNull(Vorlagenfeldkatalog.Finde("hat.bild.wp_bivalenz"));

            Vorlagenfeld tafel = Vorlagenfeldkatalog.Finde("stand.tabelle.bivalenz");
            Assert.NotNull(tafel);
            Assert.Equal(Vorlagenfeldart.Tabelle, tafel.Art);
            Assert.Equal(17, tafel.Seit);
            Assert.Equal(Vorlagenausgabe.Beide, tafel.Ausgaben);
            Assert.NotNull(Vorlagenfeldkatalog.Finde("hat.tabelle.bivalenz"));

            // Die Bausteine nehmen Bild und Tafel ab Fassung 17, nicht davor.
            Assert.Contains(ExcelBaukasten.Eintraege(17), f => f.Schluessel == "stand.tabelle.bivalenz");
            Assert.DoesNotContain(ExcelBaukasten.Eintraege(16), f => f.Schluessel == "stand.tabelle.bivalenz");
        }

        [Fact]
        public void Die_Tafel_zeigt_Bereiche_Punkte_und_das_Stundenmodell()
        {
            CultureInfo de = CultureInfo.GetCultureInfo("de-DE");
            Berichtstabelle t = Berichtstabellen.Bivalenz(Stand(), de);
            Assert.False(t.IstLeer);
            Assert.Contains(t.Hinweise, h => h == R.ResourceManager.GetString(nameof(R.BER_HINWEIS_STUNDENMODELL), de));
            Assert.DoesNotContain(t.Hinweise, h => h == R.ResourceManager.GetString(nameof(R.BER_HINWEIS_ANFAHRGRENZE), de));
            Assert.DoesNotContain(t.Hinweise, h => h == R.ResourceManager.GetString(nameof(R.BER_HINWEIS_MINDESTRUECKLAUF), de));

            Berichtstabelle mit = Berichtstabellen.Bivalenz(Stand(bivalenz: new BivalenzBerichtswerte
            {
                HinweisAnfahrgrenze = true, HinweisMindestruecklauf = true,
            }), de);
            Assert.Equal(3, mit.Hinweise.Count);

            CultureInfo en = CultureInfo.GetCultureInfo("en-US");
            Berichtstabelle englisch = Berichtstabellen.Bivalenz(Stand(), en);
            Assert.Contains(englisch.Hinweise, h => h.StartsWith("Hourly model", StringComparison.Ordinal));
        }

        [Fact]
        public void Ohne_Bereiche_und_Bivalenzwerte_bleibt_die_Tafel_mit_Grund_leer()
        {
            Berichtstabelle t = Berichtstabellen.Bivalenz(Stand(mitBereichen: false), CultureInfo.GetCultureInfo("de-DE"));
            Assert.True(t.IstLeer);
            Assert.False(string.IsNullOrEmpty(t.Leergrund));
        }

        [Fact]
        public void Ohne_Uebergabedaten_zeichnet_das_Bild_den_Platzhalter()
        {
            var b = new BivalenzBerichtswerte();
            Assert.False(b.MitUebergabe);
            BivalenzdiagrammModell m = b.Diagramm();
            Assert.False(m.MitUebergabe);
            byte[] png = ChartRenderer.Bivalenzdiagramm(m);
            Assert.True(png.Length > 100);
        }

        [Fact]
        public void Die_Schluesselwerte_tragen_die_Spaltennamen_des_Ergebnisses()
        {
            IReadOnlyList<KeyValuePair<string, double>> w = Bereiche(true).Schluesselwerte();
            Assert.Equal(UebergabegrenzeSchema.SPALTEN_ERGEBNIS_MODUL.Count, w.Count);
            Assert.Equal(UebergabegrenzeSchema.SPALTEN_ERGEBNIS_MODUL, w.Select(kv => kv.Key).ToList());
            Assert.Equal(4200.0, w.Single(kv => kv.Key == UebergabegrenzeSchema.SPALTE_BEREICH_WPALLEIN_H).Value);
            Assert.Equal(41.5, w.Single(kv => kv.Key == UebergabegrenzeSchema.SPALTE_UEBERGABE_MAX).Value);
            // Nicht erhobene Werte fehlen.
            Assert.DoesNotContain(Bereiche(false).Schluesselwerte(), kv => kv.Key == UebergabegrenzeSchema.SPALTE_BIVALENZPUNKT_1);
        }

        [Fact]
        public void Das_Mengenszenario_laesst_die_Bereiche_unveraendert()
        {
            ErgebnisModel k = SzenarioMengen.Ergebnis(Stand().Ergebnis, 1.1);
            Assert.Equal(4200, k.Waermepumpe.Bereiche.Stunden[0]);
            Assert.Equal(-4.2, k.Waermepumpe.Module[0].Bereiche.Bivalenzpunkt_1);
        }
    }
}

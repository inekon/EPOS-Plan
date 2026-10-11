using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Regelleser und die Geräteklasse</b> (Konzept Kältemittel als Stammdatum 3.3 und 4.5;
    /// KM4-E2-a): Vokabular, benanntes Verwerfen, Oberklasse, Zuordnung aus <c>Geraeteart</c> bzw.
    /// <c>Bauart</c>/<c>Typ</c>. Ohne Datenbank.
    /// </summary>
    public class KaeltemittelregelnTests
    {
        private static GesetzParameter Zeile(string schluessel, int jahr = 2027, double? wert = 150.0,
                                             string klasse = DbWerte.GESETZ_KLASSE_FGAS)
            => new GesetzParameter(0, schluessel, klasse, jahr, wert, DbWerte.GESETZ_EINHEIT_OHNE,
                                   DbWerte.GESETZ_STATUS_GESICHERT, "Probe");

        [Fact]
        public void Der_Schluessel_wird_von_hinten_zerlegt()
        {
            Assert.True(Kaeltemittelregeln.SchluesselZerlegen("FGAS_IV_SPLIT_LUFTWASSER_BIS12_GWP",
                out string klasse, out string band, out string krit, out string grund));
            Assert.Equal(DbWerte.FGAS_KLASSE_SPLIT_LUFTWASSER, klasse);
            Assert.Equal(DbWerte.FGAS_BAND_BIS12, band);
            Assert.Equal(DbWerte.FGAS_KRIT_GWP, krit);
            Assert.Null(grund);

            Assert.True(Kaeltemittelregeln.SchluesselZerlegen("FGAS_IV_MONOBLOCK_UEBER12BIS50_GWP",
                out klasse, out band, out krit, out _));
            Assert.Equal(DbWerte.FGAS_KLASSE_MONOBLOCK, klasse);
            Assert.Equal(DbWerte.FGAS_BAND_UEBER12BIS50, band);

            Assert.True(Kaeltemittelregeln.SchluesselZerlegen("FGAS_IV_KUEHLER_ALLE_HFKW",
                out klasse, out band, out krit, out _));
            Assert.Equal(DbWerte.FGAS_KRIT_HFKW, krit);
        }

        [Theory]
        [InlineData("FGAS_IV_KUEHLER_BIS13_GWP", "unbekanntes Band BIS13")]
        [InlineData("FGAS_IV_WARMWASSER_ALLE_GWP", "unbekannte Geräteklasse WARMWASSER")]
        [InlineData("FGAS_IV_KUEHLER_BIS12_FUELLMENGE", "unbekanntes Kriterium FUELLMENGE")]
        [InlineData("FGAS_13_KAELTE_ALLE_GWP", "Präfix FGAS_IV_ fehlt")]
        [InlineData("FGAS_IV_ALLE_GWP", "Band oder Geräteklasse fehlt")]
        [InlineData("FGAS_IV_", "Kriterium fehlt")]
        public void Ein_unbekannter_Schluessel_wird_benannt_verworfen(string schluessel, string erwarteterGrund)
        {
            var verworfen = new List<string>();
            IReadOnlyList<KaeltemittelRegel> regeln = Kaeltemittelregeln.Zerlegen(
                new[] { Zeile(DbWerte.GESETZ_FGAS_IV_KUEHLER_BIS12_GWP), Zeile(schluessel) }, verworfen);

            Assert.Single(regeln);
            Assert.Single(verworfen);
            Assert.Equal(schluessel + ": " + erwarteterGrund, verworfen[0]);
        }

        [Fact]
        public void Zeilen_anderer_Klassen_werden_uebergangen()
        {
            var verworfen = new List<string>();
            IReadOnlyList<KaeltemittelRegel> regeln = Kaeltemittelregeln.Zerlegen(
                new[] { Zeile("KWKG_IRGENDWAS", klasse: DbWerte.GESETZ_KLASSE_KWKG), Zeile(DbWerte.GESETZ_FGAS_IV_SPLIT_BIS12_FGAS, 2035, 0.0) },
                verworfen);
            Assert.Single(regeln);
            Assert.Empty(verworfen);
            KaeltemittelRegel r = regeln[0];
            Assert.Equal(DbWerte.FGAS_KLASSE_SPLIT, r.Geraeteklasse);
            Assert.Equal(DbWerte.FGAS_KRIT_FGAS, r.Kriterium);
            Assert.Equal(2035, r.Stichtag);
            Assert.Equal(0.0, r.Grenze);
            Assert.Equal("Probe", r.Quelle);
        }

        [Fact]
        public void Die_Saat_zerlegt_sich_ohne_Verwurf_und_kennt_jede_Klasse()
        {
            var verworfen = new List<string>();
            IReadOnlyList<KaeltemittelRegel> regeln = Kaeltemittelregeln.Zerlegen(GesetzKatalog.Vorbelegung(), verworfen);
            Assert.Empty(verworfen);
            Assert.Equal(13, regeln.Count);
            foreach (string k in new[] { DbWerte.FGAS_KLASSE_KUEHLER, DbWerte.FGAS_KLASSE_MONOBLOCK, DbWerte.FGAS_KLASSE_SPLIT,
                                         DbWerte.FGAS_KLASSE_SPLIT_LUFTWASSER, DbWerte.FGAS_KLASSE_SPLIT_LUFTLUFT })
                Assert.Contains(regeln, r => r.Geraeteklasse == k);
            Assert.Contains(GesetzKatalog.KlassenVorrat(), k => k == DbWerte.GESETZ_KLASSE_FGAS);
        }

        [Fact]
        public void Die_Oberklasse_der_Splitvarianten_ist_Split()
        {
            Assert.Equal(DbWerte.FGAS_KLASSE_SPLIT, Kaeltemittelzulaessigkeit.Oberklasse(DbWerte.FGAS_KLASSE_SPLIT_LUFTWASSER));
            Assert.Equal(DbWerte.FGAS_KLASSE_SPLIT, Kaeltemittelzulaessigkeit.Oberklasse(DbWerte.FGAS_KLASSE_SPLIT_LUFTLUFT));
            Assert.Null(Kaeltemittelzulaessigkeit.Oberklasse(DbWerte.FGAS_KLASSE_SPLIT));
            Assert.Null(Kaeltemittelzulaessigkeit.Oberklasse(DbWerte.FGAS_KLASSE_KUEHLER));
        }

        [Theory]
        [InlineData("KWS_LUFT", DbWerte.FGAS_KLASSE_KUEHLER)]
        [InlineData(" kws_wasser ", DbWerte.FGAS_KLASSE_KUEHLER)]
        [InlineData("KWS_FREIKUEHLUNG", DbWerte.FGAS_KLASSE_KUEHLER)]
        [InlineData("ABSORPTION", Kaeltemittelzulaessigkeit.GERAETEKLASSE_ABSORPTION)]
        [InlineData("SPLIT", null)]
        [InlineData("MULTISPLIT", null)]
        [InlineData("VRF", null)]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void Die_Geraeteklasse_der_Kaeltemaschine_folgt_der_Geraeteart(string geraeteart, string erwartet)
        {
            Assert.Equal(erwartet, Kaeltemittelregeln.GeraeteklasseKaeltemaschine(geraeteart));
        }

        [Theory]
        [InlineData("Monoblock", "Luft-Wasser", DbWerte.FGAS_KLASSE_MONOBLOCK)]
        [InlineData(" kompakt ", " luft-wasser ", DbWerte.FGAS_KLASSE_MONOBLOCK)]
        [InlineData("Split", "Luft-Wasser", DbWerte.FGAS_KLASSE_SPLIT_LUFTWASSER)]
        [InlineData("Split", "Sole-Wasser", null)]
        [InlineData("Monoblock", "Wasser-Wasser", null)]
        [InlineData(null, "Luft-Wasser", null)]
        [InlineData("Split", null, null)]
        public void Die_Geraeteklasse_der_Waermepumpe_folgt_Bauart_und_Typ(string bauart, string typ, string erwartet)
        {
            Assert.Equal(erwartet, Kaeltemittelregeln.GeraeteklasseWaermepumpe(bauart, typ));
        }

        [Fact]
        public void Der_Pruefstoff_kommt_aus_der_Tabellenzeile()
        {
            var satz = new KaeltemittelCtrl.Kaeltemittelsatz(11, "R1234ze(E)", "", "HFO", 1.37, "", true, false,
                null, "PFAS", "VORLAEUFIG", "", null, true);
            KaeltemittelStoff s = Kaeltemittelregeln.Stoff(satz);
            Assert.Equal("R1234ze(E)", s.Code);
            Assert.Equal("HFO", s.Gruppe);
            Assert.Equal(1.37, s.Gwp);
            Assert.True(s.FGas);
            Assert.Null(Kaeltemittelregeln.Stoff(null));
        }

        [Fact]
        public void Ohne_Katalog_liest_der_Leser_nichts()
        {
            Assert.Empty(Kaeltemittelregeln.Lesen(null));
        }
    }
}

#nullable enable

using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Gerätegrenzen</b> (Übergabegrenze UB‑E3; Fachkonzept 4.4, 2.11, 8.1 Zeile 13): leere Spalten nach der
    /// Kältemittelklasse mit Herkunft, gepflegte als „Katalog", die abgeleitete Rücklaufgrenze θ_WP,max − σ_min und das
    /// Feld <c>Ruecklauf_Max</c> als min(Feld, abgeleitet). Ohne Datenbank.
    /// </summary>
    public class GeraetegrenzenTests
    {
        [Fact]
        public void Leere_Spalten_nehmen_die_Vorgabe_der_Kaeltemittelklasse()
        {
            Geraetegrenzen g = Geraetegrenzen.Bilden(Geraetespalten.NurKaeltemittel("R410A"), 55.0);
            Assert.Equal(5.0, g.SpreizungAuslegungK);
            Assert.Equal(10.0, g.SpreizungMaxK);
            Assert.Equal(3.0, g.SpreizungMinK);
            Assert.Equal(0.60, g.MindestvolumenstromAnteil, 12);
            Assert.Equal(Geraetegrenzherkunft.VorgabeKaeltemittel, g.SpreizungMaxHerkunft);
            Assert.Equal(Geraetegrenzherkunft.VorgabeKaeltemittel, g.MindestvolumenstromHerkunft);
            Assert.False(g.Abwertung);
            Assert.True(double.IsNaN(g.BezugsruecklaufC));
        }

        [Fact]
        public void Gepflegter_Wert_traegt_die_Herkunft_Katalog()
        {
            var s = new Geraetespalten(6.0, 12.0, 2.0, 50.0, null, null, null, "R32");
            Geraetegrenzen g = Geraetegrenzen.Bilden(s, 55.0);
            Assert.Equal(6.0, g.SpreizungAuslegungK);
            Assert.Equal(12.0, g.SpreizungMaxK);
            Assert.Equal(2.0, g.SpreizungMinK);
            Assert.Equal(0.50, g.MindestvolumenstromAnteil, 12);
            Assert.Equal(Geraetegrenzherkunft.Katalog, g.SpreizungAuslegungHerkunft);
            Assert.Equal(Geraetegrenzherkunft.Katalog, g.SpreizungMaxHerkunft);
            Assert.Equal(Geraetegrenzherkunft.Katalog, g.SpreizungMinHerkunft);
            Assert.Equal(Geraetegrenzherkunft.Katalog, g.MindestvolumenstromHerkunft);
            // Abgeleitet mit dem gepflegten σ_min: 55 − 2 = 53 °C.
            Assert.Equal(53.0, g.RuecklaufGrenzeC, 12);
            Assert.Equal(Geraetegrenzherkunft.Abgeleitet, g.RuecklaufHerkunft);
            Assert.Equal(Geraetegrenzherkunft.Katalog, g.Werte().SpreizungHerkunft);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("SONSTIGES")]
        [InlineData("R999")]
        public void Leeres_oder_unbekanntes_Kaeltemittel_nimmt_die_allgemeine_Vorgabe(string? kaeltemittel)
        {
            Geraetegrenzen g = Geraetegrenzen.Bilden(Geraetespalten.NurKaeltemittel(kaeltemittel), 55.0);
            Assert.Equal(Geraetegrenzherkunft.Vorgabe, g.SpreizungMaxHerkunft);
            Assert.Equal(Geraetegrenzherkunft.Vorgabe, g.MindestvolumenstromHerkunft);
            Assert.Equal(10.0, g.SpreizungMaxK);
            Assert.Equal(52.0, g.RuecklaufGrenzeC, 12);
            Assert.Equal(Geraetegrenzherkunft.Vorgabe, g.Werte().SpreizungHerkunft);
        }

        [Fact]
        public void Abgeleitete_Grenze_52_und_Feld_50()
        {
            Geraetegrenzen leer = Geraetegrenzen.Bilden(Geraetespalten.Leer, 55.0);
            Assert.Equal(52.0, leer.RuecklaufGrenzeC, 12);
            Assert.Equal(Geraetegrenzherkunft.Abgeleitet, leer.RuecklaufHerkunft);

            Geraetegrenzen feld = Geraetegrenzen.Bilden(Geraetespalten.Leer with { RuecklaufMaxC = 50.0 }, 55.0);
            Assert.Equal(50.0, feld.RuecklaufGrenzeC, 12);
            Assert.Equal(Geraetegrenzherkunft.Katalog, feld.RuecklaufHerkunft);

            // Ein Feld über der abgeleiteten Grenze: die strengere gilt (min).
            Geraetegrenzen hoch = Geraetegrenzen.Bilden(Geraetespalten.Leer with { RuecklaufMaxC = 60.0 }, 55.0);
            Assert.Equal(52.0, hoch.RuecklaufGrenzeC, 12);
            Assert.Equal(Geraetegrenzherkunft.Abgeleitet, hoch.RuecklaufHerkunft);
        }

        [Fact]
        public void R744_leer_rechnet_mit_Abwertung_der_Klasse_und_gepflegt_mit_dem_Katalog()
        {
            Geraetegrenzen vorgabe = Geraetegrenzen.Bilden(Geraetespalten.NurKaeltemittel("R744"), 80.0);
            Assert.True(vorgabe.Abwertung);
            Assert.Equal(2.5, vorgabe.AbwertungProzentJeK, 12);
            Assert.Equal(30.0, vorgabe.BezugsruecklaufC, 12);
            Assert.Equal(40.0, vorgabe.RuecklaufGrenzeC, 12);
            Assert.Equal(Geraetegrenzherkunft.VorgabeKaeltemittel, vorgabe.RuecklaufHerkunft);
            Assert.Equal(Geraetegrenzherkunft.VorgabeKaeltemittel, vorgabe.AbwertungHerkunft);
            Assert.Equal(40.0, vorgabe.Werte().RuecklaufGrenzeR744C);

            var s = new Geraetespalten(null, null, null, null, 35.0, 28.0, 0.0, "R744");
            Geraetegrenzen hart = Geraetegrenzen.Bilden(s, 80.0);
            Assert.False(hart.Abwertung);
            Assert.Equal(35.0, hart.RuecklaufGrenzeC, 12);
            Assert.Equal(Geraetegrenzherkunft.Katalog, hart.AbwertungHerkunft);
            Assert.Null(hart.Werte().RuecklaufGrenzeR744C);

            // Abwertung ohne Bezug: 30 °C.
            Geraetegrenzen ohneBezug = Geraetegrenzen.Bilden(Geraetespalten.Leer with { RuecklaufAbwertungProzentJeK = 2.0 }, 55.0);
            Assert.Equal(30.0, ohneBezug.BezugsruecklaufC, 12);
        }

        [Fact]
        public void Bivalenzpruefung_Grenzen_bleibt_die_Vorgabe_nach_Kaeltemittel()
        {
            Geraetegrenzwerte w = Bivalenzpruefung.Grenzen("R410A", 55.0);
            Assert.Equal(52.0, w.RuecklaufMaxC, 12);
            Assert.Equal(Geraetegrenzherkunft.Abgeleitet, w.RuecklaufHerkunft);
            Geraetegrenzwerte k = Bivalenzpruefung.GrenzenAusSpalten(Geraetespalten.Leer with { RuecklaufMaxC = 50.0 }, 55.0);
            Assert.Equal(50.0, k.RuecklaufMaxC, 12);
            Assert.Equal(Geraetegrenzherkunft.Katalog, k.RuecklaufHerkunft);
        }
    }
}

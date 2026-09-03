using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="Ferienzeit"/> nach iU9-W9.0c — die Umrechnung Tag/Monat ↔ Jahrestag und
    /// die vier Pruefregeln, die bis dahin in <c>Form_Gebaeude2</c> standen.
    ///
    /// <para>Kein Datenbankzugriff: Die Klasse rechnet nur. Sie rechnet allerdings im
    /// LAUFENDEN Jahr (Bestand), deshalb pruefen die Faelle den Hin- und den Rueckweg
    /// gegeneinander statt gegen feste Zahlen — nur der 1. Januar ist in jedem Jahr
    /// Tag 1.</para>
    /// </summary>
    public class FerienzeitTests
    {
        [Fact]
        public void Jahrestag_zaehlt_den_ersten_Januar_als_Tag_1()
        {
            Assert.Equal(1, Ferienzeit.Jahrestag("1", "1"));
        }

        [Fact]
        public void Jahrestag_und_TagUndMonat_sind_zueinander_umkehrbar()
        {
            // Der 15. Maerz liegt im Schaltjahr einen Tag spaeter - deshalb hin und zurueck
            // statt gegen eine feste Zahl.
            int tagimjahr = Ferienzeit.Jahrestag("3", "15");
            (int? tag, int? monat) = Ferienzeit.TagUndMonat(tagimjahr);

            Assert.Equal(15, tag);
            Assert.Equal(3, monat);
        }

        [Theory]
        [InlineData("", "1")]
        [InlineData("1", "")]
        [InlineData("abc", "1")]
        [InlineData("13", "1")]     // Monat 13
        [InlineData("0", "1")]      // Monat 0
        [InlineData("1", "32")]     // Tag 32
        [InlineData("2", "30")]     // unmoegliches Datum
        public void Jahrestag_liefert_bei_ungueltiger_Angabe_null(string monat, string tag)
        {
            Assert.Equal(0, Ferienzeit.Jahrestag(monat, tag));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(366)]
        public void TagUndMonat_liefert_bei_0_und_366_zwei_leere_Felder(int jahrestag)
        {
            (int? tag, int? monat) = Ferienzeit.TagUndMonat(jahrestag);

            Assert.Null(tag);
            Assert.Null(monat);
        }

        /// <summary>
        /// Die WINTERregel ist die umgekehrte: Beginn im Dezember, Ende im Januar — der
        /// Beginn muss die groessere Zahl sein (<c>btn_Speichern_Click</c>:177).
        /// </summary>
        [Fact]
        public void Pruefen_meldet_Winterferien_die_nicht_ueber_die_Jahresgrenze_gehen()
        {
            string meldung = Ferienzeit.Pruefen(
                new[] { 10, 0, 0, 0 }, new[] { 20, 0, 0, 0 });

            Assert.Equal(Ferienzeit.MELDUNG_WINTER, meldung);
        }

        [Fact]
        public void Pruefen_nimmt_Winterferien_ueber_die_Jahresgrenze_an()
        {
            string meldung = Ferienzeit.Pruefen(
                new[] { 350, 0, 0, 0 }, new[] { 10, 0, 0, 0 });

            Assert.Null(meldung);
        }

        [Theory]
        [InlineData(1, Ferienzeit.MELDUNG_OSTERN)]
        [InlineData(2, Ferienzeit.MELDUNG_SOMMER)]
        [InlineData(3, Ferienzeit.MELDUNG_HERBST)]
        public void Pruefen_meldet_je_Zeitraum_seinen_eigenen_Text(int stelle, string erwartet)
        {
            int[] beginn = { 366, 0, 0, 0 };
            int[] ende = { 0, 0, 0, 0 };
            beginn[stelle] = 200;
            ende[stelle] = 100;

            Assert.Equal(erwartet, Ferienzeit.Pruefen(beginn, ende));
        }

        /// <summary>Zwei leere Zeitraeume (0/0) sind gueltig — der Regelfall im Katalog.</summary>
        [Fact]
        public void Pruefen_nimmt_vier_leere_Zeitraeume_an()
        {
            Assert.Null(Ferienzeit.Pruefen(new[] { 0, 0, 0, 0 }, new[] { 0, 0, 0, 0 }));
        }

        [Fact]
        public void WinterbeginnGehoben_macht_aus_0_die_366()
        {
            Assert.Equal(366, Ferienzeit.WinterbeginnGehoben(0));
            Assert.Equal(350, Ferienzeit.WinterbeginnGehoben(350));
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="Ferienzeit"/> nach iU9-W9.0c — die Umrechnung Tag/Monat ↔ Jahrestag und
    /// die vier Pruefregeln, die bis dahin in <c>Form_Gebaeude2</c> standen.
    ///
    /// <para>Kein Datenbankzugriff: Die Klasse rechnet nur — im GEMEINJAHR (365 Tage, kein
    /// 29. Februar), wie Kalender und Lauf die Jahrestage deuten (Befund B13, Entwurf KP2
    /// Festlegung 12). Deshalb pruefen die Faelle feste Zahlen, unabhaengig vom laufenden Jahr;
    /// die Wache gegen die Uhr haelt die Klasse davon frei.</para>
    /// </summary>
    public class FerienzeitTests
    {
        [Fact]
        public void Jahrestag_zaehlt_den_ersten_Januar_als_Tag_1()
        {
            Assert.Equal(1, Ferienzeit.Jahrestag("1", "1"));
        }

        /// <summary>
        /// Feste Faelle im Gemeinjahr (B13): Der 1. Maerz ist Tag 60 und der 31. Dezember Tag 365 in
        /// JEDEM Jahr — im Schaltjahr lagen sie mit der Uhr einen Tag spaeter.
        /// </summary>
        [Theory]
        [InlineData(1, 1, 1)]
        [InlineData(2, 28, 59)]
        [InlineData(3, 1, 60)]
        [InlineData(3, 15, 74)]
        [InlineData(12, 31, 365)]
        public void Jahrestag_rechnet_im_Gemeinjahr(int monat, int tag, int erwartet)
        {
            Assert.Equal(erwartet, Ferienzeit.Jahrestag(monat, tag));
            Assert.Equal(erwartet, Ferienzeit.Jahrestag(monat.ToString(), tag.ToString()));
            (int? t, int? m) = Ferienzeit.TagUndMonat(erwartet);
            Assert.Equal(tag, t);
            Assert.Equal(monat, m);
        }

        /// <summary>Der 29. Februar kommt im Gemeinjahr nicht vor — eine Fehleingabe, 0 wie ein unmoegliches Datum.</summary>
        [Fact]
        public void Der_29_Februar_ist_kein_Ferientag()
        {
            Assert.Equal(0, Ferienzeit.Jahrestag("2", "29"));
            Assert.Equal(0, Ferienzeit.Jahrestag(2, 29));
        }

        /// <summary>Hin und zurueck fuer alle 365 Tage — dieselbe Umrechnung wie die Feiertagsregeln (<see cref="Feiertage"/>).</summary>
        [Fact]
        public void Jahrestag_und_TagUndMonat_sind_zueinander_umkehrbar()
        {
            for (int jahrestag = 1; jahrestag <= 365; jahrestag++)
            {
                (int? tag, int? monat) = Ferienzeit.TagUndMonat(jahrestag);
                Assert.True(tag.HasValue && monat.HasValue, "Tag " + jahrestag);
                Assert.Equal(jahrestag, Ferienzeit.Jahrestag(monat, tag));
                Assert.Equal(Feiertage.Gemeinjahrestag(monat.Value, tag.Value), jahrestag);
            }
        }

        /// <summary>
        /// <b>Wache gegen die Uhr</b> (B13): Die Umrechnung liest kein laufendes Jahr — sonst
        /// wanderte jedes Datum ab dem 1. Maerz im Schaltjahr um einen Tag.
        /// </summary>
        [Fact]
        public void Ferienzeit_rechnet_ohne_Uhr()
        {
            string quelltext = File.ReadAllText(Path.Combine(Wurzel(), "EPOS.Kern", "Allgemein", "Ferienzeit.cs"));
            string[] code = quelltext.Split('\n').Select(z => z.Trim())
                                     .Where(z => !z.StartsWith("//", StringComparison.Ordinal)).ToArray();
            Assert.DoesNotContain(code, z => z.Contains("DateTime.Now", StringComparison.Ordinal)
                                             || z.Contains("DateTime.Today", StringComparison.Ordinal)
                                             || z.Contains("DateTime.UtcNow", StringComparison.Ordinal));
        }

        private static string Wurzel([CallerFilePath] string eigeneDatei = "")
        {
            string ordner = Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
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

        /// <summary>Ausserhalb 1 … 365 gibt es im Gemeinjahr kein Datum — kein Uebertrag ins Folgejahr.</summary>
        [Theory]
        [InlineData(-1)]
        [InlineData(367)]
        public void TagUndMonat_ausserhalb_des_Gemeinjahres_liefert_zwei_leere_Felder(int jahrestag)
        {
            (int? tag, int? monat) = Ferienzeit.TagUndMonat(jahrestag);

            Assert.Null(tag);
            Assert.Null(monat);
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

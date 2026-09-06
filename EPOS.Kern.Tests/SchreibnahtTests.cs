using System;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="Schreibnaht"/> — <b>der Nachweis der Welle iF30</b> (Anwenderentscheid
    /// vom 04.09.2026: „streng").
    ///
    /// <para><b>Was hier geprüft wird und was nicht.</b> Diese Klasse fasst keine
    /// Datenbank an: Sie prüft die REGEL — welche Anweisung als schreibend gilt, wann die
    /// Naht wirft, wie eine benannte Freigabe wirkt und dass eine Werkzeug-Freigabe die
    /// Sperre hebt. Dass die Naht in der Zugriffsschicht auch wirklich sitzt, weist
    /// <see cref="SchreibnahtDatenbankTests"/> an einer Arbeitskopie nach.</para>
    ///
    /// <para><b>Ohne Lizenzablage.</b> Kein Fall dieser Klasse ruft
    /// <c>LizenzManager.Pruefe()</c> — jeder legt sein eigenes
    /// <see cref="Schreibnaht.Schreibrecht"/> ein und nimmt es danach zurück. Sonst liefe
    /// die Probe gegen den Zeitanker des Entwicklerrechners und schriebe ihn fort
    /// (dasselbe Risiko R-W15c-3, das <c>LizenzZustandTests</c> vermeidet).</para>
    /// </summary>
    /// <remarks>
    /// <b>Die Sammlung „Testdatenbank" gilt auch hier</b>, obwohl kein Fall die Datenbank
    /// öffnet: <see cref="Schreibnaht.Schreibrecht"/> ist ein STATISCHES Feld, und xunit
    /// führt Testklassen sonst nebeneinander — eine Klasse, die es auf „nein" stellt,
    /// zöge jeder anderen mit Datenbank den Boden weg.
    /// </remarks>
    [Collection("Testdatenbank")]
    public class SchreibnahtTests : IDisposable
    {
        private readonly Func<bool> _vorher;

        public SchreibnahtTests()
        {
            _vorher = Schreibnaht.Schreibrecht;
        }

        public void Dispose()
        {
            Schreibnaht.Schreibrecht = _vorher;
        }

        /// <summary>Stellt die Naht auf „darf nicht schreiben".</summary>
        private static void Lesemodus() => Schreibnaht.Schreibrecht = () => false;

        // ==================================================================
        //  1-3  Was gilt als schreibend?
        // ==================================================================

        /// <summary>
        /// Fall 1: Die vier LESENDEN Anfangswörter kommen durch — auch mit Leerraum,
        /// führender Klammer und Kommentaren davor.
        /// </summary>
        [Theory]
        [InlineData("SELECT * FROM Tab_Projekt")]
        [InlineData("select id from Tab_Projekt")]
        [InlineData("   \r\n\t SELECT 1")]
        [InlineData("(SELECT 1)")]
        [InlineData("-- ein Kommentar\nSELECT 1")]
        [InlineData("/* Block */ SELECT 1")]
        [InlineData("PRAGMA table_info(Tab_Projekt)")]
        [InlineData("SELECT name FROM pragma_table_info(?) ORDER BY cid")]
        [InlineData("EXPLAIN SELECT 1")]
        [InlineData("VALUES (1)")]
        [InlineData("")]
        [InlineData(null)]
        public void Lesende_Anweisungen_erkennt_die_Naht(string sql)
        {
            Assert.False(Schreibnaht.IstSchreibend(sql));
        }

        /// <summary>
        /// Fall 2: Alles andere gilt als schreibend. <b>Die Liste ist die der LESER</b> —
        /// wer eine Schreibform vergäße, hätte ein Loch; eine zu viel abgewiesene
        /// Leseform fällt dagegen sofort auf.
        /// </summary>
        [Theory]
        [InlineData("INSERT INTO Tab_Projekt (ID) VALUES (?)")]
        [InlineData("UPDATE Tab_Projekt SET Projektname = ?")]
        [InlineData("DELETE FROM Tab_Projekt WHERE ID = ?")]
        [InlineData("REPLACE INTO Tab_Projekt (ID) VALUES (?)")]
        [InlineData("CREATE TABLE x (a INTEGER)")]
        [InlineData("DROP INDEX IF EXISTS Projektname")]
        [InlineData("ALTER TABLE Tab_Projekt ADD COLUMN x REAL")]
        [InlineData("VACUUM")]
        [InlineData("VACUUM INTO ?")]
        [InlineData("REINDEX Tab_Projekt")]
        [InlineData("ATTACH DATABASE ? AS alt")]
        [InlineData("WITH x AS (SELECT 1) INSERT INTO y SELECT * FROM x")]
        [InlineData("  \n INSERT INTO Tab_Projekt (ID) VALUES (?)")]
        public void Schreibende_Anweisungen_erkennt_die_Naht(string sql)
        {
            Assert.True(Schreibnaht.IstSchreibend(sql));
        }

        /// <summary>
        /// Fall 3: Das erste Wort wird über Leerraum und beide Kommentarformen hinweg
        /// gelesen — auch über einen unabgeschlossenen Blockkommentar (dann gibt es
        /// keines, und die Anweisung gilt als lesend, weil sie nichts tut).
        /// </summary>
        [Fact]
        public void Das_erste_Wort_steht_hinter_Leerraum_und_Kommentaren()
        {
            Assert.Equal("SELECT", Schreibnaht.ErstesWort("  /* a */ -- b\n select 1"));
            Assert.Equal("UPDATE", Schreibnaht.ErstesWort("\r\n\tupdate x set a = 1"));
            Assert.Equal("", Schreibnaht.ErstesWort("/* nie zu Ende"));
            Assert.Equal("", Schreibnaht.ErstesWort("   "));
        }

        // ==================================================================
        //  4-6  Die Sperre selbst
        // ==================================================================

        /// <summary>Fall 4: Im Lesemodus wirft ein Schreibversuch die eigene Ausnahme.</summary>
        [Fact]
        public void Schreiben_im_Lesemodus_wirft_die_eigene_Ausnahme()
        {
            Lesemodus();

            LesemodusException ex = Assert.Throws<LesemodusException>(
                () => Schreibnaht.Pruefe("UPDATE Tab_Projekt SET Projektname = ?"));

            // Der Anwender liest einen Satz, kein SQL - die Anweisung steht daneben.
            Assert.Equal(Resource.LIZ_LESEMODUS_SPERRE, ex.Message);
            Assert.StartsWith("UPDATE Tab_Projekt", ex.Anweisung);
            Assert.DoesNotContain("UPDATE", ex.Message);
        }

        /// <summary>Fall 5: Lesen bleibt im Lesemodus frei — das ist der ganze Punkt.</summary>
        [Fact]
        public void Lesen_bleibt_im_Lesemodus_frei()
        {
            Lesemodus();

            Schreibnaht.Pruefe("SELECT * FROM Tab_Projekt");
            Schreibnaht.Pruefe("PRAGMA table_info(Tab_Projekt)");
            Assert.False(Schreibnaht.DarfSchreiben());
        }

        /// <summary>Fall 6: Mit Schreibrecht wirft nichts.</summary>
        [Fact]
        public void Mit_Schreibrecht_geht_alles_durch()
        {
            Schreibnaht.Schreibrecht = () => true;

            Schreibnaht.Pruefe("DELETE FROM Tab_Projekt WHERE ID = ?");
            Assert.True(Schreibnaht.DarfSchreiben());
        }

        // ==================================================================
        //  7-10  Die benannten Freigaben
        // ==================================================================

        /// <summary>
        /// Fall 7: Eine Freigabe MIT GRUND lässt im Lesemodus schreiben — und nach ihrem
        /// Ende ist wieder zu.
        /// </summary>
        [Fact]
        public void Eine_Freigabe_mit_Grund_darf_schreiben()
        {
            Lesemodus();

            using (Schreibnaht.Freigabe(Schreibnaht.GRUND_MIGRATION))
            {
                Assert.Equal(Schreibnaht.GRUND_MIGRATION, Schreibnaht.Freigabegrund);
                Schreibnaht.Pruefe("ALTER TABLE Tab_Projekt ADD COLUMN x REAL");
            }

            Assert.Equal("", Schreibnaht.Freigabegrund);
            Assert.Throws<LesemodusException>(
                () => Schreibnaht.Pruefe("ALTER TABLE Tab_Projekt ADD COLUMN x REAL"));
        }

        /// <summary>
        /// Fall 8: Freigaben schachteln sich; der innerste Grund gilt, und erst das
        /// äußerste Ende schließt.
        /// </summary>
        [Fact]
        public void Freigaben_schachteln_sich()
        {
            Lesemodus();

            using (Schreibnaht.Freigabe(Schreibnaht.GRUND_MIGRATION))
            {
                using (Schreibnaht.Freigabe(Schreibnaht.GRUND_PROGRAMMZUSTAND))
                {
                    Assert.Equal(Schreibnaht.GRUND_PROGRAMMZUSTAND, Schreibnaht.Freigabegrund);
                }

                Assert.Equal(Schreibnaht.GRUND_MIGRATION, Schreibnaht.Freigabegrund);
                Schreibnaht.Pruefe("UPDATE Tab_Applikation SET SchemaVersion = 65");
            }

            Assert.Equal("", Schreibnaht.Freigabegrund);
        }

        /// <summary>
        /// Fall 9: Eine Freigabe ohne Grund gibt es nicht — sie bekommt einen sprechenden
        /// Ersatz, damit in der Diagnose nie eine leere Zeile steht.
        /// </summary>
        [Fact]
        public void Eine_Freigabe_ohne_Grund_bekommt_einen()
        {
            Lesemodus();

            using (Schreibnaht.Freigabe(""))
            {
                Assert.NotEqual("", Schreibnaht.Freigabegrund);
                Schreibnaht.Pruefe("VACUUM");
            }
        }

        /// <summary>
        /// Fall 10: Ein Wurf INNERHALB der Freigabe schließt sie trotzdem — das
        /// <c>using</c> trägt, sonst bliebe der Lesemodus nach dem ersten Fehler gehoben.
        /// </summary>
        [Fact]
        public void Eine_Ausnahme_schliesst_die_Freigabe_trotzdem()
        {
            Lesemodus();

            Assert.Throws<InvalidOperationException>((Action)Abbrechen);

            Assert.Equal("", Schreibnaht.Freigabegrund);
            Assert.Throws<LesemodusException>(() => Schreibnaht.Pruefe("VACUUM"));

            static void Abbrechen()
            {
                using (Schreibnaht.Freigabe(Schreibnaht.GRUND_MIGRATION))
                {
                    throw new InvalidOperationException("Abbruch mitten im Schritt");
                }
            }
        }

        // ==================================================================
        //  11-13  Die Werkzeug-Freigabe
        // ==================================================================

        /// <summary>
        /// Fall 11: Die Werkzeug-Freigabe hebt die Sperre für den ganzen Prozess und nennt
        /// ihren Grund — das ist der Weg von Referenzlauf, iOS-Prüfmodus und
        /// Testvorrichtung.
        /// </summary>
        [Fact]
        public void Die_Werkzeugfreigabe_hebt_die_Sperre_und_nennt_den_Grund()
        {
            Lesemodus();
            Assert.False(Schreibnaht.DarfSchreiben());

            Schreibnaht.WerkzeugFreigabe("Prüfstand iF30");

            Assert.True(Schreibnaht.DarfSchreiben());
            Assert.Equal("Prüfstand iF30", Schreibnaht.WerkzeugGrund);
            Assert.Same(Schreibnaht.ImmerErlaubt, Schreibnaht.Schreibrecht);
            Schreibnaht.Pruefe("INSERT INTO Tab_Projekt (ID) VALUES (?)");
        }

        /// <summary>
        /// Fall 12: Sie lässt sich zurücknehmen — danach steht wieder die Lizenzfrage
        /// (<c>LizenzManager.DarfSchreiben</c>), nicht irgendein Rest.
        /// </summary>
        [Fact]
        public void Die_Werkzeugfreigabe_laesst_sich_zuruecknehmen()
        {
            Schreibnaht.WerkzeugFreigabe("Prüfstand iF30");
            Schreibnaht.WerkzeugFreigabeZuruecknehmen();

            Assert.Equal("", Schreibnaht.WerkzeugGrund);
            Assert.Equal((Func<bool>)LizenzManager.DarfSchreiben, Schreibnaht.Schreibrecht);
        }

        /// <summary>
        /// Fall 13: Das Setzen von <see cref="Schreibnaht.Schreibrecht"/> verwirft den
        /// Zwischenspeicher SOFORT. Ohne diese Zusage gälte eine alte Antwort noch
        /// Sekunden weiter — und ein Prüfstand sähe den Zustand nicht, den er einstellt.
        /// </summary>
        [Fact]
        public void Ein_neues_Schreibrecht_gilt_sofort()
        {
            Schreibnaht.Schreibrecht = () => true;
            Assert.True(Schreibnaht.DarfSchreiben());

            Schreibnaht.Schreibrecht = () => false;
            Assert.False(Schreibnaht.DarfSchreiben());

            Schreibnaht.Schreibrecht = () => true;
            Assert.True(Schreibnaht.DarfSchreiben());
        }

        // ==================================================================
        //  14-15  Zwei Zusagen, die man leicht verliert
        // ==================================================================

        /// <summary>
        /// Fall 14: Wirft die Schreibrechtsfrage selbst, wird NICHT gesperrt. „Nie Daten
        /// sperren" (Konzept § 9): Eine unlesbare Lizenzablage darf die Arbeit nicht
        /// anhalten — dieselbe Linie wie <c>ZustimmungCtrl</c> (<c>catch → true</c>).
        /// </summary>
        [Fact]
        public void Eine_kaputte_Lizenzfrage_sperrt_nicht()
        {
            Schreibnaht.Schreibrecht = () => throw new InvalidOperationException("Ablage unlesbar");

            Assert.True(Schreibnaht.DarfSchreiben());
            Schreibnaht.Pruefe("UPDATE Tab_Projekt SET Projektname = ?");
        }

        /// <summary>
        /// Fall 15: Auch ohne jede Frage (<c>null</c>) wird nicht gesperrt — der Zustand
        /// eines Prüfstands, der das Feld leert.
        /// </summary>
        [Fact]
        public void Ohne_Schreibrechtsfrage_wird_nicht_gesperrt()
        {
            Schreibnaht.Schreibrecht = null;

            Assert.True(Schreibnaht.DarfSchreiben());
        }
    }
}

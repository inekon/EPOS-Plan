using System;
using Xunit;
using WindowsFormsApplication1;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die beiden providerneutralen Werkzeuge der Zugriffsschicht und der Vertrag
    /// dahinter (iU6-T4).
    ///
    /// <para>Geprueft wird ausschliesslich, was OHNE Datenbank entscheidbar ist:
    /// <c>UebersetzeParameterzeichen</c> und <c>NormalisiereWert</c> arbeiten auf reinen
    /// Zeichenketten bzw. Werten, und die Vertragsprobe fragt nur nach dem Typ hinter der
    /// Fassade. Kein Fall hier oeffnet eine Verbindung - das bleibt den Referenzlaeufen
    /// vorbehalten.</para>
    /// </summary>
    public class DatenzugriffTests
    {
        // =================================================================================
        // UebersetzeParameterzeichen - die drei Faelle aus dem Kommentar der Methode
        // =================================================================================

        [Fact]
        public void Platzhalter_werden_strikt_in_Reihenfolge_nummeriert()
        {
            Assert.Equal(
                "INSERT INTO T (a, b, c) VALUES (@p0, @p1, @p2)",
                DataRepository.UebersetzeParameterzeichen("INSERT INTO T (a, b, c) VALUES (?, ?, ?)"));
        }

        [Fact]
        public void Fragezeichen_in_einem_Textliteral_bleibt_stehen()
        {
            // Auch mit ''-Escape mitten im Literal: Was zwischen den Hochkommata steht,
            // ist Text und wird nicht nummeriert.
            Assert.Equal(
                "SELECT * FROM T WHERE Bez = 'Wie geht''s?' AND ID = @p0",
                DataRepository.UebersetzeParameterzeichen(
                    "SELECT * FROM T WHERE Bez = 'Wie geht''s?' AND ID = ?"));
        }

        [Fact]
        public void Fragezeichen_in_eckigen_Klammern_bleibt_stehen()
        {
            Assert.Equal(
                "SELECT [Frage?] FROM T WHERE ID = @p0",
                DataRepository.UebersetzeParameterzeichen("SELECT [Frage?] FROM T WHERE ID = ?"));
        }

        // =================================================================================
        // NormalisiereWert - die Speicherform der SQLite-Datei
        // =================================================================================

        [Fact]
        public void Wahrheitswert_wird_zu_0_oder_1()
        {
            Assert.Equal(1, DataRepository.NormalisiereWert(true));
            Assert.Equal(0, DataRepository.NormalisiereWert(false));
        }

        [Fact]
        public void Datum_wird_ISO_Text_ohne_Sekundenbruchteile()
        {
            // Dasselbe Format wie im Migrator - sonst stuenden im selben Feld zwei
            // Schreibweisen.
            object w = DataRepository.NormalisiereWert(new DateTime(2026, 9, 3, 14, 5, 9, 750));
            Assert.Equal("2026-09-03 14:05:09", w);
        }

        [Fact]
        public void Null_und_DBNull_werden_zu_DBNull()
        {
            Assert.Equal(DBNull.Value, DataRepository.NormalisiereWert(null));
            Assert.Equal(DBNull.Value, DataRepository.NormalisiereWert(DBNull.Value));
        }

        [Fact]
        public void Uebrige_Werte_bleiben_unveraendert()
        {
            Assert.Equal(42, DataRepository.NormalisiereWert(42));
            Assert.Equal("Text", DataRepository.NormalisiereWert("Text"));
            Assert.Equal(1.5, DataRepository.NormalisiereWert(1.5));
        }

        // =================================================================================
        // Vertragsprobe
        // =================================================================================

        /// <summary>
        /// Die Fassade haelt ihre Umsetzung hinter <see cref="IDatenzugriff"/>. Faellt
        /// dieser Fall, ist die Naht wieder zugewachsen, die der Kern fuer iOS braucht
        /// (Umsetzungskonzept § 1.4, iF10).
        /// </summary>
        [Fact]
        public void Fassade_haelt_einen_IDatenzugriff()
        {
            Assert.NotNull(DataRepository.Zugriff);
            Assert.IsAssignableFrom<IDatenzugriff>(DataRepository.Zugriff);
        }

        /// <summary>
        /// Der Proben- und Referenzlauf-Haken schlaegt ALLES: Solange
        /// <c>PfadUeberschreibung</c> gesetzt ist, liefern Fassade und Umsetzung genau
        /// diesen Pfad - unabhaengig von den Einstellungen des Anwenders.
        /// </summary>
        [Fact]
        public void Pfadueberschreibung_schlaegt_die_Einstellungen()
        {
            string vorher = DataRepository.PfadUeberschreibung;
            try
            {
                DataRepository.PfadUeberschreibung = "/tmp/probe/Kenndaten.sqlite";
                Assert.Equal("/tmp/probe/Kenndaten.sqlite", DataRepository.GetDBPath());
                Assert.Equal("/tmp/probe/Kenndaten.sqlite", DataRepository.Zugriff.DatenbankPfad);
            }
            finally
            {
                DataRepository.PfadUeberschreibung = vorher;
            }
        }
    }
}

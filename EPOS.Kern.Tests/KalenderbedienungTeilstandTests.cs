using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Schemaschritt <b>Kalenderbedienung</b> auf einem <b>Teilstand</b>: Fehlen der Datenbank Sicht, Indizes oder
    /// Trigger des Schritts, baut ein erneuter Lauf alles Fehlende nach, und ein zweiter Lauf ändert nichts. Dazu der
    /// Befund aus dem Ausnahmeprotokoll eines Anwenders: Der Schritt rief nach dem <c>Commit()</c> im <c>finally</c>
    /// noch eine Anweisung am abgeschlossenen Vorgang (<c>PRAGMA legacy_alter_table = OFF</c>) — gefangen und folgenlos,
    /// aber als ausgelöste Ausnahme „bereits abgeschlossen“ im Protokoll. Die Fälle zählen deshalb jede auf diesem Faden
    /// ausgelöste Ausnahme dieser Art mit.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KalenderbedienungTeilstandTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private const string ABGESCHLOSSEN = "bereits abgeschlossen";

        [Fact]
        public void Fehlen_Trigger_Index_und_Sicht_baut_der_Schritt_sie_ohne_Anweisung_am_abgeschlossenen_Vorgang_nach()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KalenderbedienungSchema.Vollstaendig());

            // Der Teilstand: ein Trigger, ein Index und die Sicht fehlen (der volle Weg mit Commit und finally).
            string trigger = KalenderbedienungSchema.Triggeranweisungen.First().Key;
            string index = KalenderbedienungSchema.Indexanweisungen.First().Key;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                v.Ausfuehren("DROP TRIGGER IF EXISTS \"" + trigger + "\"");
                v.Ausfuehren("DROP INDEX IF EXISTS \"" + index + "\"");
                v.Ausfuehren(GebaeudeSchema.SQL_VIEW_DROP);
                v.Commit();
            }
            Assert.False(KalenderbedienungSchema.Vollstaendig());
            Assert.False(KalenderbedienungSchema.SichtSteht());

            var bericht = new List<string>();
            int aenderungen = 0;
            int abgeschlossen = AbgeschlossenMeldungen(() => aenderungen = KalenderbedienungSchema.Ausfuehren(bericht));

            Assert.Equal(0, abgeschlossen);
            Assert.True(aenderungen > 0, string.Join(" | ", bericht));
            Assert.True(KalenderbedienungSchema.Vollstaendig());
            Assert.Equal(1L, Anzahl("trigger", trigger));
            Assert.Equal(1L, Anzahl("index", index));

            // Der zweite Lauf: nichts zu tun, kein Vorgang.
            var zweiter = new List<string>();
            Assert.Equal(0, KalenderbedienungSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("nichts zu tun", StringComparison.Ordinal));

            // legacy_alter_table steht auf der Verbindung des Pools wieder aus.
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar("PRAGMA legacy_alter_table")));
        }

        [Fact]
        public void Fehlt_nur_die_Sicht_baut_der_Schritt_sie_allein_nach()
        {
            if (!_db.Vorhanden) return;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                v.Ausfuehren(GebaeudeSchema.SQL_VIEW_DROP);
                v.Commit();
            }
            Assert.False(KalenderbedienungSchema.Vollstaendig());

            int aenderungen = 0;
            int abgeschlossen = AbgeschlossenMeldungen(() => aenderungen = KalenderbedienungSchema.Ausfuehren(null));

            Assert.Equal(0, abgeschlossen);
            Assert.Equal(1, aenderungen);
            Assert.True(KalenderbedienungSchema.Vollstaendig());
            Assert.Equal(0, KalenderbedienungSchema.Ausfuehren(null));
        }

        /// <summary>Zählt die auf diesem Faden ausgelösten Ausnahmen „bereits abgeschlossen“ während <paramref name="aktion"/>.</summary>
        private static int AbgeschlossenMeldungen(Action aktion)
        {
            int faden = Environment.CurrentManagedThreadId;
            int zahl = 0;
            EventHandler<FirstChanceExceptionEventArgs> zaehler = (s, e) =>
            {
                if (Environment.CurrentManagedThreadId == faden && e.Exception is InvalidOperationException &&
                    (e.Exception.Message ?? "").Contains(ABGESCHLOSSEN, StringComparison.Ordinal))
                    Interlocked.Increment(ref zahl);
            };
            AppDomain.CurrentDomain.FirstChanceException += zaehler;
            try { aktion(); }
            finally { AppDomain.CurrentDomain.FirstChanceException -= zaehler; }
            return zahl;
        }

        private static long Anzahl(string typ, string name) => Convert.ToInt64(DataRepository.ExecuteScalar(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = ? AND name = ?", new DbParam("@t", typ), new DbParam("@n", name)));
    }
}

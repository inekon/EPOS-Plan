using System;
using Microsoft.Data.Sqlite;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// DIE KLAMMER UM EINEN GANZEN SCHREIBLAUF (iU9-W16a-O-1, Entscheid E-4 zweite
    /// Haelfte).
    ///
    /// <para><b>Das Problem.</b> Ein <see cref="DbVorgang"/> ist EINE Verbindung mit
    /// EINER Transaktion. Der Speicherlauf des Assistenten laeuft ueber 23
    /// Schreibmethoden von <c>WizardCtrl</c>, und diese rufen ihrerseits ein Dutzend
    /// Katalogcontroller (<c>CopyFromStamm</c>, <c>ApplyGanglinieToProjekt</c>,
    /// <c>KostenProjektPositionenCtrl</c> …). Jede dieser Stellen holt sich heute ihre
    /// EIGENE Verbindung aus dem Pool. Ein hereingereichter Vorgang erreichte damit
    /// nur die oberste Schicht: Die tieferen Schreibvorgaenge liefen daran vorbei
    /// (kein Rollback) und wuerden in WAL unter der Schreibsperre des Vorgangs sogar
    /// haengen bleiben (SQLITE_BUSY), und ihre Lesevorgaenge saehen den noch nicht
    /// festgeschriebenen Stand NICHT.</para>
    ///
    /// <para><b>Die Loesung.</b> Der laufende Vorgang wird fuer die Dauer des
    /// Speicherlaufs am FADEN angemeldet. Die Zugriffsschicht fragt an ihrer EINEN
    /// Stelle, an der sie eine Verbindung aufmacht (<see cref="Leihe"/>), ob ein
    /// Vorgang angemeldet ist; ist er es, arbeitet die Anweisung auf DESSEN Verbindung
    /// und in DESSEN Transaktion. Ist keiner angemeldet — der Normalfall im ganzen
    /// uebrigen Programm — passiert genau das, was vorher passierte: eine eigene
    /// Verbindung je Anweisung, ohne Transaktion.</para>
    ///
    /// <para><b>Warum <c>[ThreadStatic]</c> und nicht <c>AsyncLocal</c>.</b> Eine
    /// <c>SqliteConnection</c> darf nicht von zwei Faeden gleichzeitig bedient werden.
    /// <c>AsyncLocal</c> flosse in jeden <c>Task.Run</c> hinein und truege die
    /// Verbindung damit auf fremde Faeden; <c>[ThreadStatic]</c> haelt sie auf dem
    /// einen Faden, auf dem der Speicherlauf steht. Ein Nebenlaeufer bekommt wie
    /// bisher seine eigene Verbindung.</para>
    ///
    /// <para><b>Sie ist ausdruecklich kein zweiter Weg in eine Transaktion.</b>
    /// <c>DataRepository.Vorgang()</c> bleibt der einzige; die Klammer sagt nur, WER
    /// gerade laeuft.</para>
    /// </summary>
    internal static class Vorgangsklammer
    {
        [ThreadStatic]
        private static DbVorgang _aktueller;

        /// <summary>
        /// Der auf diesem Faden angemeldete und noch LAUFENDE Vorgang; <c>null</c> im
        /// Normalfall und ebenso, sobald er festgeschrieben oder zurueckgerollt ist -
        /// danach bekommt jede Anweisung wieder ihre eigene Verbindung.
        /// </summary>
        internal static DbVorgang Aktueller
        {
            get
            {
                DbVorgang v = _aktueller;
                return (v != null && v.Offen) ? v : null;
            }
        }

        /// <summary>
        /// Meldet einen Vorgang fuer die Dauer des <c>using</c>-Blocks an.
        ///
        /// <para><b><c>null</c> aendert nichts.</b> Das ist der Normalfall der 23
        /// Schreibmethoden: Wer ohne Vorgang gerufen wird, laeuft weiter wie bisher —
        /// und wer innerhalb eines fremden Laufs ohne eigenen Vorgang gerufen wird,
        /// bleibt in DESSEN Klammer, statt sie abzuraeumen.</para>
        /// </summary>
        internal static Halter Setzen(DbVorgang vorgang)
        {
            return new Halter(vorgang);
        }

        /// <summary>Der Rueckgabewert von <see cref="Setzen"/> — stellt den Vorstand wieder her.</summary>
        internal readonly struct Halter : IDisposable
        {
            private readonly DbVorgang _vorher;
            private readonly bool _gesetzt;

            internal Halter(DbVorgang vorgang)
            {
                if (vorgang == null)
                {
                    _vorher = null;
                    _gesetzt = false;
                    return;
                }

                _vorher = _aktueller;
                _gesetzt = true;
                _aktueller = vorgang;
            }

            public void Dispose()
            {
                if (_gesetzt) _aktueller = _vorher;
            }
        }

        /// <summary>
        /// Die Verbindung fuer EINE Anweisung: die des angemeldeten Vorgangs (geliehen,
        /// wird nicht geschlossen) oder eine eigene aus dem Pool (wird geschlossen).
        /// </summary>
        internal static Leihverbindung Leihe()
        {
            DbVorgang vorgang = Aktueller;
            if (vorgang != null)
                return new Leihverbindung(vorgang.Verbindung, vorgang.Transaktion, false);

            return new Leihverbindung(DataRepository.OeffneVerbindung(), null, true);
        }
    }

    /// <summary>
    /// Eine Verbindung auf Zeit — entweder geliehen aus dem laufenden
    /// <see cref="DbVorgang"/> oder eigens geoeffnet. <see cref="Dispose"/> schliesst
    /// NUR die eigene; die geliehene gehoert dem Vorgang.
    ///
    /// <para><b>Die Transaktion muss mit.</b> <c>Microsoft.Data.Sqlite</c> weist ein
    /// Kommando ab, dessen <c>Transaction</c> nicht die aktive Transaktion der
    /// Verbindung ist — die geliehene Verbindung reicht deshalb ihre Transaktion mit
    /// heraus.</para>
    /// </summary>
    internal readonly struct Leihverbindung : IDisposable
    {
        private readonly bool _eigene;

        internal Leihverbindung(SqliteConnection verbindung, SqliteTransaction transaktion, bool eigene)
        {
            Verbindung = verbindung;
            Transaktion = transaktion;
            _eigene = eigene;
        }

        /// <summary>Die Verbindung, auf der die Anweisung laeuft.</summary>
        internal SqliteConnection Verbindung { get; }

        /// <summary>Die Transaktion, in der sie laeuft; <c>null</c> ohne Vorgang.</summary>
        internal SqliteTransaction Transaktion { get; }

        public void Dispose()
        {
            if (_eigene && Verbindung != null) Verbindung.Dispose();
        }
    }
}

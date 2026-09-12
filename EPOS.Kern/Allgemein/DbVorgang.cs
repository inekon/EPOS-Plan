using System;
using System.Data;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Datenbankvorgang: EINE Verbindung mit EINER Transaktion, gedacht fuer einen
    /// <c>using</c>-Block (Implementierungskonzept DB-Migration SQLite, 2.6).
    ///
    /// LOEST DAS VERBINDUNGS-TUPEL AB. <c>DataRepository.BeginTransaction()</c> gab bisher
    /// <c>(OleDbConnection, OleDbTransaction)</c> an 18 Dateien heraus; 13 weitere fuehrten
    /// Transaktionen auf selbst geoeffneten Verbindungen. Beide Gruppen wandern in
    /// Arbeitspaket S4e hierher. Nebenbei verschwinden die uneinheitlichen Aufraeummuster:
    /// Die Mehrzahl der Bestandsstellen entsorgt ihre Transaktion heute nie.
    ///
    /// FEHLER WERDEN DURCHGEREICHT, nicht gemeldet. Anders als die sechs Zugriffsmethoden
    /// des <see cref="DataRepository"/> faengt der Vorgang nichts ab - der Aufrufer haelt
    /// die Klammer um mehrere Anweisungen und entscheidet selbst ueber Rollback und
    /// Meldung. Genau so arbeiteten die Bestandsstellen mit dem Tupel auch.
    ///
    /// Die Parameteruebersetzung (? -> @pN, Wertenormalisierung) und der Typ-Rueckweg
    /// beim Lesen laufen ueber DIESELBEN Helfer wie die Zugriffsmethoden - es gibt keine
    /// zweite Fassung davon.
    /// </summary>
    public sealed class DbVorgang : IDisposable
    {
        private SqliteConnection _verbindung;
        private SqliteTransaction _transaktion;

        /// <summary>true, sobald Commit oder Rollback gelaufen ist.</summary>
        private bool _abgeschlossen;

        /// <summary>
        /// Der Name des Sicherungspunkts, wenn dies ein UNTERPUNKT eines laufenden
        /// Vorgangs ist; <c>null</c> beim echten Vorgang mit eigener Verbindung.
        /// </summary>
        private readonly string _sicherungspunkt;

        /// <summary>Fortlaufende Nummer der Sicherungspunkte (Namen muessen eindeutig sein).</summary>
        private static int _punktzaehler;

        /// <summary>
        /// Nur ueber <see cref="DataRepository.Vorgang"/> zu haben - die Verbindung ist
        /// dort bereits geoeffnet und mit den PRAGMAs versehen.
        /// </summary>
        internal DbVorgang(SqliteConnection verbindung)
        {
            _verbindung = verbindung ?? throw new ArgumentNullException(nameof(verbindung));
            _transaktion = _verbindung.BeginTransaction();
            _sicherungspunkt = null;
        }

        /// <summary>
        /// EIN UNTERPUNKT EINES LAUFENDEN VORGANGS (iU9-W16a-O-1).
        ///
        /// <para>SQLite kennt keine geschachtelten Transaktionen, wohl aber
        /// SICHERUNGSPUNKTE. Wer waehrend eines angemeldeten Vorgangs
        /// <c>DataRepository.Vorgang()</c> ruft - und das tun ein Dutzend
        /// Katalogcontroller -, bekommt deshalb keinen zweiten Vorgang auf einer
        /// zweiten Verbindung (die an der Schreibsperre haengen bliebe), sondern
        /// diesen Unterpunkt: dieselbe Verbindung, dieselbe Transaktion, ein eigener
        /// Sicherungspunkt.</para>
        ///
        /// <para>Damit bleibt die Bedeutung fuer den Aufrufer dieselbe wie bisher:
        /// <c>Commit()</c> laesst sein Werk stehen (<c>RELEASE</c>), <c>Rollback()</c>
        /// nimmt GENAU SEINE Aenderungen zurueck (<c>ROLLBACK TO</c>) und laesst den
        /// umgebenden Lauf weiterarbeiten. Nur die Dauerhaftigkeit entscheidet der
        /// aeussere Vorgang.</para>
        /// </summary>
        internal DbVorgang(DbVorgang eltern)
        {
            if (eltern == null) throw new ArgumentNullException(nameof(eltern));
            if (!eltern.Offen)
                throw new InvalidOperationException(
                    "Der umgebende Datenbankvorgang ist bereits abgeschlossen.");

            _verbindung = eltern._verbindung;
            _transaktion = eltern._transaktion;
            _sicherungspunkt = "epos_up_" + (++_punktzaehler).ToString(CultureInfo.InvariantCulture);
            _transaktion.Save(_sicherungspunkt);
        }

        /// <summary>Laeuft der Vorgang noch (Verbindung da, weder Commit noch Rollback)?</summary>
        internal bool Offen
        {
            get { return _verbindung != null && !_abgeschlossen; }
        }

        /// <summary>Die Verbindung des Vorgangs - fuer die <see cref="Leihverbindung"/>.</summary>
        internal SqliteConnection Verbindung
        {
            get { return _verbindung; }
        }

        /// <summary>Die Transaktion des Vorgangs - fuer die <see cref="Leihverbindung"/>.</summary>
        internal SqliteTransaction Transaktion
        {
            get { return _transaktion; }
        }

        private void PruefeOffen()
        {
            if (_verbindung == null)
                throw new ObjectDisposedException(nameof(DbVorgang));
            if (_abgeschlossen)
                throw new InvalidOperationException(
                    "Der Datenbankvorgang ist bereits abgeschlossen (Commit oder Rollback gelaufen).");
        }

        /// <summary>INSERT/UPDATE/DELETE; liefert die Anzahl betroffener Zeilen.</summary>
        public int Ausfuehren(string sql, params DbParam[] parameter)
        {
            PruefeOffen();
            using (SqliteCommand cmd = DataRepository.ErzeugeKommando(_verbindung, _transaktion, sql, parameter))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Einzelwert; DBNull wird - wie in ExecuteScalar - zu null.</summary>
        public object Skalar(string sql, params DbParam[] parameter)
        {
            PruefeOffen();
            using (SqliteCommand cmd = DataRepository.ErzeugeKommando(_verbindung, _transaktion, sql, parameter))
            {
                object ergebnis = cmd.ExecuteScalar();
                if (ergebnis == DBNull.Value) return null;
                return ergebnis;
            }
        }

        /// <summary>
        /// INSERT und die ID des erzeugten Datensatzes - <c>last_insert_rowid()</c> auf
        /// DIESER Verbindung und in DIESER Transaktion.
        /// Signatur ohne <c>params</c>, wie <c>DataRepository.ExecuteInsertAndGetId</c>.
        /// </summary>
        public int EinfuegenUndId(string sql, DbParam[] parameter)
        {
            PruefeOffen();
            using (SqliteCommand cmd = DataRepository.ErzeugeKommando(_verbindung, _transaktion, sql, parameter))
            {
                cmd.ExecuteNonQuery();
            }
            using (SqliteCommand cmdId = _verbindung.CreateCommand())
            {
                cmdId.Transaction = _transaktion;
                cmdId.CommandText = "SELECT last_insert_rowid()";
                return Convert.ToInt32(cmdId.ExecuteScalar());
            }
        }

        /// <summary>
        /// SELECT innerhalb des Vorgangs - sieht also die noch nicht festgeschriebenen
        /// Aenderungen. Typ-Rueckweg wie bei <c>DataRepository.GetDataTable</c>.
        /// </summary>
        public DataTable Lese(string sql, params DbParam[] parameter)
        {
            PruefeOffen();
            using (SqliteCommand cmd = DataRepository.ErzeugeKommando(_verbindung, _transaktion, sql, parameter))
            using (SqliteDataReader leser = cmd.ExecuteReader())
            {
                return DataRepository.LadeTabelle(leser);
            }
        }

        /// <summary>
        /// Schreibt den Vorgang fest. Ein UNTERPUNKT gibt dabei nur seinen
        /// Sicherungspunkt frei - festgeschrieben wird beim aeusseren Vorgang.
        /// </summary>
        public void Commit()
        {
            PruefeOffen();
            if (_sicherungspunkt != null) _transaktion.Release(_sicherungspunkt);
            else _transaktion.Commit();
            _abgeschlossen = true;
        }

        /// <summary>
        /// Rollt den Vorgang zurueck. Ein bereits abgeschlossener Vorgang wird still
        /// uebergangen - damit ein <c>catch</c>-Zweig gefahrlos zurueckrollen kann,
        /// auch wenn SQLite das schon selbst getan hat.
        /// </summary>
        public void Rollback()
        {
            if (_verbindung == null || _abgeschlossen) return;

            if (_sicherungspunkt != null)
            {
                // Der Unterpunkt nimmt GENAU SEINE Aenderungen zurueck; der umgebende
                // Lauf arbeitet weiter. ROLLBACK TO laesst den Punkt stehen - er wird
                // anschliessend freigegeben.
                _transaktion.Rollback(_sicherungspunkt);
                try { _transaktion.Release(_sicherungspunkt); }
                catch (Exception) { /* z. B. von SQLite selbst schon abgeraeumt */ }
            }
            else
            {
                _transaktion.Rollback();
            }

            _abgeschlossen = true;
        }

        /// <summary>
        /// Ohne vorheriges <c>Commit()</c> wird zurueckgerollt; raeumt Transaktion UND
        /// Verbindung ab. Mehrfachaufruf ist zulaessig.
        ///
        /// <para>Ein UNTERPUNKT raeumt weder Transaktion noch Verbindung ab - beide
        /// gehoeren dem umgebenden Vorgang.</para>
        /// </summary>
        public void Dispose()
        {
            if (_verbindung == null) return;

            if (_sicherungspunkt != null)
            {
                try { Rollback(); }
                catch (Exception) { /* z. B. von SQLite selbst schon zurueckgerollt */ }
                _abgeschlossen = true;
                _transaktion = null;
                _verbindung = null;
                return;
            }

            try
            {
                if (!_abgeschlossen)
                {
                    // Kein Commit gesehen -> der Vorgang gilt als gescheitert.
                    try { _transaktion.Rollback(); }
                    catch (Exception) { /* z. B. von SQLite selbst schon zurueckgerollt */ }
                    _abgeschlossen = true;
                }
            }
            finally
            {
                try { _transaktion?.Dispose(); } catch (Exception) { }
                try { _verbindung.Dispose(); } catch (Exception) { }
                _transaktion = null;
                _verbindung = null;
            }
        }
    }
}

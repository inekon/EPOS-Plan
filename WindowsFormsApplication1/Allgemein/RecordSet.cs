using System;
using System.Data;
using System.Data.OleDb;
using Microsoft.Data.Sqlite;

namespace WindowsFormsApplication1
{
    // =====================================================================================
    // ARBEITSPAKET S4b: NUR DIE INNENSEITE UMGESTELLT.
    //
    // Die oeffentliche Flaeche bleibt Zeichen fuer Zeichen: DBCommand, Open, Insert, EOF,
    // Next, Read(string), Read(int), GetString, Close, Dispose - 47 Nutzer haengen daran.
    // Auch das OleDbCommand bleibt, weil es der DATENTRAEGER fuer CommandText und
    // Parameter ist; ein Wechsel des Typs waere eine Aenderung an 47 Dateien.
    //
    // ARBEITSPAKET S4e: Wer in einer laufenden Transaktion lesen oder schreiben will,
    // uebergibt den Vorgang jetzt AUSDRUECKLICH - Open(sql, DbVorgang) bzw.
    // Insert(sql, DbVorgang). Der frueher dafuer vorgesehene Weg (Verbindung und
    // Transaktion am DBCommand setzen) ist entfallen; er hatte nur einen Nutzer
    // (Form_DBBHKW) und ging am Typ-Rueckweg D9 vorbei.
    //
    // ZWEI AENDERUNGEN INNEN, beide bewusst:
    //
    //  1. STATT READER EINE TABELLE. Microsoft.Data.Sqlite liefert Rohtypen (long statt
    //     int, 0/1 statt bool, Text statt DateTime). Der Bestand castet hart
    //     ("(int)rs.Read(...)"), ein boxed long liesse jeden dieser Casts auflaufen. Der
    //     Typ-Rueckweg D9 steckt in DataRepository.LadeTabelle und arbeitet auf einer
    //     DataTable - also wird das Ergebnis materialisiert und der Zeilenzeiger laeuft
    //     ueber die Tabelle. EOF()/Next() behalten dabei ihren eigenwilligen Vertrag:
    //     BEIDE ruecken den Zeiger vor (so verhielt sich der Reader auch).
    //
    //  2. KEINE VERBINDUNG MEHR AM DBCommand. Frueher hat Open()/Insert() die selbst
    //     geoeffnete Verbindung ins DBCommand gehaengt und Close() sie wieder
    //     herausgenommen. Das entfaellt: Jeder Aufruf holt sich seine Verbindung aus dem
    //     Pool und gibt sie sofort zurueck. Fuer die Aufrufer bleibt es gleich - sie
    //     sahen diese Verbindung nur waehrend des Aufrufs, und "DBCommand.Connection ==
    //     null" bedeutet weiterhin genau "niemand von aussen hat eine gesetzt".
    //
    // ABWEICHUNG, DIE SICH NICHT 1:1 HALTEN LAESST: Read()/GetString() ausserhalb einer
    // gueltigen Zeile oder auf einen unbekannten Spaltennamen warfen frueher
    // IndexOutOfRangeException/InvalidOperationException (Reader), jetzt werfen sie
    // ArgumentException/IndexOutOfRangeException (DataRow). Es wirft weiterhin - nur der
    // Ausnahmetyp unterscheidet sich; kein Aufrufer faengt ihn gezielt ab.
    // =====================================================================================

    public class RecordSet : IDisposable
    {
        // Auf OleDbCommand umgestellt, damit Zuweisungen aus dem UI-Code (z.B. transaction) ohne Cast funktionieren
        public OleDbCommand DBCommand { get; set; }

        /// <summary>Das materialisierte Ergebnis des letzten <see cref="Open"/>.</summary>
        private DataTable _ergebnis;

        /// <summary>Zeilenzeiger; -1 = vor der ersten Zeile (wie ein frisch geoeffneter Reader).</summary>
        private int _zeile = -1;

        public RecordSet()
        {
            DBCommand = new OleDbCommand();
        }

        /// <summary>
        /// Die Parameter des DBCommand als Datentraeger-Array fuer die Zugriffsschicht.
        /// </summary>
        private OleDbParameter[] Parameter()
        {
            if (DBCommand == null || DBCommand.Parameters.Count == 0) return null;
            OleDbParameter[] p = new OleDbParameter[DBCommand.Parameters.Count];
            DBCommand.Parameters.CopyTo(p, 0);
            return p;
        }

        public bool Open(string sql)
        {
            return Open(sql, null);
        }

        /// <summary>
        /// Wie <see cref="Open(string)"/>, liest aber INNERHALB eines laufenden
        /// Datenbankvorgangs - sieht also dessen noch nicht festgeschriebene
        /// Aenderungen.
        ///
        /// ARBEITSPAKET S4e: Diese Ueberladung tritt an die Stelle des frueheren
        /// Zweigs, dem der Aufrufer ueber <c>DBCommand.Connection</c> eine fremde
        /// Verbindung samt Transaktion zuwies (allein Form_DBBHKW tat das). Der
        /// Zweig fuellte damals ueber einen <c>OleDbDataAdapter</c>; jetzt laeuft
        /// das Lesen ueber <see cref="DbVorgang.Lese"/> - also ueber DENSELBEN
        /// Typ-Rueckweg wie der Normalfall, statt an ihm vorbei.
        /// </summary>
        public bool Open(string sql, DbVorgang vorgang)
        {
            // Evtl. noch offenes Ergebnis aus einem vorherigen Open verwerfen.
            _ergebnis = null;
            _zeile = -1;

            try
            {
                DBCommand.CommandText = sql;

                if (vorgang != null)
                {
                    _ergebnis = vorgang.Lese(sql, Parameter());
                    return true;
                }

                // Andernfalls öffnen wir eine eigene, interne Verbindung für diese Abfrage.
                using (SqliteConnection conn = StilleDb.OeffneVerbindung())
                using (SqliteCommand cmd = DataRepository.ErzeugeKommando(conn, null, sql, Parameter()))
                using (SqliteDataReader leser = cmd.ExecuteReader())
                {
                    _ergebnis = DataRepository.LadeTabelle(leser);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Öffnen des RecordSets: " + ex.Message + " | SQL: " + sql);
                // Aufraeumen, damit ein halbes Ergebnis im Fehlerfall nicht stehen bleibt.
                Close();
                return false;
            }
        }

        public bool Insert(string sql)
        {
            return Insert(sql, null);
        }

        /// <summary>
        /// Wie <see cref="Insert(string)"/>, schreibt aber INNERHALB des uebergebenen
        /// Vorgangs (ARBEITSPAKET S4e - Ersatz fuer die frueher von aussen gesetzte
        /// Verbindung/Transaktion am DBCommand).
        /// </summary>
        public bool Insert(string sql, DbVorgang vorgang)
        {
            try
            {
                DBCommand.CommandText = sql;

                if (vorgang != null)
                {
                    vorgang.Ausfuehren(sql, Parameter());
                    return true;
                }

                using (SqliteConnection conn = StilleDb.OeffneVerbindung())
                using (SqliteCommand cmd = DataRepository.ErzeugeKommando(conn, null, sql, Parameter()))
                {
                    cmd.ExecuteNonQuery();
                }
                return true;
            }
            catch (Exception sqlEx)
            {
                Console.WriteLine("SQL Fehler beim Insert: " + sqlEx.Message);
                return false;
            }
        }

        public bool EOF()
        {
            if (_ergebnis == null) return true;

            // Verhält sich wie der alte OdbcReader: Liest den nächsten Datensatz.
            // Gibt es keinen, sind wir am Ende (EOF = true).
            if (_zeile + 1 < _ergebnis.Rows.Count) { _zeile++; return false; }
            _zeile = _ergebnis.Rows.Count;
            return true;
        }

        public bool Next()
        {
            if (_ergebnis == null) return false;
            if (_zeile + 1 < _ergebnis.Rows.Count) { _zeile++; return true; }
            _zeile = _ergebnis.Rows.Count;
            return false;
        }

        public Object Read(string name)
        {
            if (_ergebnis == null) return null;
            return _ergebnis.Rows[_zeile][name];
        }

        public Object Read(int index)
        {
            if (_ergebnis == null) return null;
            return _ergebnis.Rows[_zeile][index];
        }

        public String GetString(string name)
        {
            if (_ergebnis == null) return "";
            object wert = _ergebnis.Rows[_zeile][name];
            if (wert == DBNull.Value) return "";
            return wert.ToString();
        }

        public void Close()
        {
            // Das Ergebnis freigeben. Eine eigene Verbindung gibt es nicht mehr zu
            // schliessen - sie lebt nur innerhalb von Open()/Insert().
            _ergebnis = null;
            _zeile = -1;
        }

        // Ermoeglicht "using (var rs = new RecordSet()) { ... }": gibt das Ergebnis frei
        // und zusaetzlich das OleDbCommand (reiner Datentraeger fuer CommandText und
        // Parameter). Ein uebergebener DbVorgang gehoert dem Aufrufer und wird hier
        // weder abgeschlossen noch entsorgt.
        public void Dispose()
        {
            Close();
            if (DBCommand != null)
            {
                DBCommand.Dispose();
                DBCommand = null;
            }
        }
    }
}
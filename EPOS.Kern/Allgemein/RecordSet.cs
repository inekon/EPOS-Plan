using System;
using System.Data;
using Microsoft.Data.Sqlite;

namespace WindowsFormsApplication1
{
    // =====================================================================================
    // ARBEITSPAKET S4b: NUR DIE INNENSEITE UMGESTELLT.
    //
    // Die oeffentliche Flaeche bleibt Zeichen fuer Zeichen: Open, Insert, EOF, Next,
    // Read(string), Read(int), GetString, Close, Dispose - 47 Nutzer haengen daran.
    //
    // ARBEITSPAKET S4e: Wer in einer laufenden Transaktion lesen oder schreiben will,
    // uebergibt den Vorgang jetzt AUSDRUECKLICH - Open(sql, DbVorgang) bzw.
    // Insert(sql, DbVorgang). Der frueher dafuer vorgesehene Weg (Verbindung und
    // Transaktion am Kommando setzen) ist entfallen; er hatte nur einen Nutzer
    // (Form_DBBHKW) und ging am Typ-Rueckweg D9 vorbei.
    //
    // ARBEITSPAKET iU6 / RISIKO iR8: "DBCommand" IST ERSATZLOS GESTRICHEN - UND ES TRITT
    // BEWUSST KEIN EIGENER TYP AN SEINE STELLE.
    //
    // iR8 hatte zwei Wege offen gelassen: die Eigenschaft auf einen providerfreien Typ
    // heben ODER RecordSet mit seinen Masken abloesen (iU9). Die Vermessung vom 03.09.2026
    // hat den Posten zu einer STREICHUNG gemacht: Repositoryweit - Kern, Anwendung, Views,
    // Referenzlauf, Proben, KiKern - greift KEINE Stelle ausserhalb dieser Datei auf
    // RecordSet.DBCommand zu. Niemand setzte Connection, Transaction, CommandText oder
    // Parameters. Seit iU3 entstand das Kommando ohnehin nur lazy IM GETTER und blieb
    // deshalb immer null: MerkeSql() schrieb in ein Objekt, das es nie gab, und Parameter()
    // lieferte ausnahmslos null.
    //
    // Ein Ersatztyp (etwa ein "DbBefehl") haette also eine Fassade fuer null Nutzer
    // geschaffen und dabei den falschen Eindruck erweckt, RecordSet trage Parameter - was
    // es seit der Umstellung auf Open(sql) / Open(sql, DbVorgang) nicht mehr tut. Wer
    // parametrisiert arbeiten will, nimmt DataRepository bzw. DbVorgang mit DbParam; das
    // ist der einzige Weg, den die Zugriffsschicht seit 6486c36 kennt. RecordSet bleibt
    // damit genau das, was es faktisch laengst war: ein vorwaertslaufender Zeilenzeiger
    // ueber ein fertiges SQL ohne Parameter.
    //
    // IDisposable BLEIBT - der Vertrag ist oeffentlich, und Dispose() gibt weiterhin das
    // materialisierte Ergebnis frei; es tut das jetzt ueber Close(), weil es nichts
    // anderes mehr freizugeben gibt.
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
    //  2. KEINE VERBINDUNG MEHR AM KOMMANDO. Frueher hat Open()/Insert() die selbst
    //     geoeffnete Verbindung in das damalige DBCommand gehaengt und Close() sie wieder
    //     herausgenommen. Das entfaellt: Jeder Aufruf holt sich seine Verbindung aus dem
    //     Pool und gibt sie sofort zurueck. Fuer die Aufrufer bleibt es gleich - sie sahen
    //     diese Verbindung ohnehin nur waehrend des Aufrufs.
    //
    // ABWEICHUNG, DIE SICH NICHT 1:1 HALTEN LAESST: Read()/GetString() ausserhalb einer
    // gueltigen Zeile oder auf einen unbekannten Spaltennamen warfen frueher
    // IndexOutOfRangeException/InvalidOperationException (Reader), jetzt werfen sie
    // ArgumentException/IndexOutOfRangeException (DataRow). Es wirft weiterhin - nur der
    // Ausnahmetyp unterscheidet sich; kein Aufrufer faengt ihn gezielt ab.
    // =====================================================================================

    public class RecordSet : IDisposable
    {
        /// <summary>Das materialisierte Ergebnis des letzten <see cref="Open"/>.</summary>
        private DataTable _ergebnis;

        /// <summary>Zeilenzeiger; -1 = vor der ersten Zeile (wie ein frisch geoeffneter Reader).</summary>
        private int _zeile = -1;

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
                if (vorgang != null)
                {
                    _ergebnis = vorgang.Lese(sql);
                    return true;
                }

                // Andernfalls öffnen wir eine eigene, interne Verbindung für diese Abfrage.
                using (SqliteConnection conn = StilleDb.OeffneVerbindung())
                using (SqliteCommand cmd = DataRepository.ErzeugeKommando(conn, null, sql, null))
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
                if (vorgang != null)
                {
                    vorgang.Ausfuehren(sql);
                    return true;
                }

                using (SqliteConnection conn = StilleDb.OeffneVerbindung())
                using (SqliteCommand cmd = DataRepository.ErzeugeKommando(conn, null, sql, null))
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

        // Ermoeglicht "using (var rs = new RecordSet()) { ... }": gibt das materialisierte
        // Ergebnis frei. Seit iU6 ist das ALLES, was hier freizugeben ist - das
        // OleDbCommand ist gestrichen (Begruendung im Kopf). Ein uebergebener DbVorgang
        // gehoert dem Aufrufer und wird hier weder abgeschlossen noch entsorgt.
        public void Dispose()
        {
            Close();
        }
    }
}

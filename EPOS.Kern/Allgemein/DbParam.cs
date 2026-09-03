using System;
using System.Data.OleDb;
using System.Runtime.Versioning;

namespace WindowsFormsApplication1
{
    // =====================================================================================
    // ARBEITSPAKET iU6 (Umsetzungskonzept iOS, § 1.4 / Entscheidung iF10; Befund im
    // Entscheidungsregister § 2.2, Messung B vom 02.09.2026).
    //
    // WARUM ES DIESEN TYP GIBT. Gemessen wurde: "new OleDbParameter("@p0", 42.5)" wirft
    // auf Linux und macOS eine PlatformNotSupportedException SCHON IM KONSTRUKTOR. Der
    // Typ ist ausserhalb von Windows also kein Datentraeger, sondern eine Wand - jeder
    // parametrisierte Zugriff ueber DataRepository/DbVorgang/StilleDb scheitert dort,
    // bevor eine einzige Zeile gerechnet ist. Seit 6486c36 spricht die Zugriffsschicht
    // innen laengst SQLite (Microsoft.Data.Sqlite); nur der PARAMETERTYP an der
    // Aussenflaeche war noch OleDb. Genau den ersetzt DbParam.
    //
    // WAS DER TYP KANN - UND WAS BEWUSST NICHT. Name, Wert, optionaler Typ und optionale
    // Groesse; mehr brauchte der Bestand nie (Direction, IsNullable, SourceColumn und
    // Precision/Scale kommen an keiner Aufrufstelle vor). Der Typ ist reiner
    // Datentraeger: Er bindet nichts, oeffnet nichts und kennt keinen Provider.
    //
    // DIE UEBERGANGSBRUECKE. Der implizite Operator aus OleDbParameter und die Helfer
    // Von()/NachOleDb() halten den Altbestand am Laufen, waehrend er stueckweise
    // umgestellt wird:
    //   - Von()/implizit  : Altaufrufe "GetDataTable(sql, new OleDbParameter(...))"
    //                       kompilieren unveraendert weiter. Auf Windows laufen sie wie
    //                       bisher; auf Linux werfen sie erst zur Laufzeit - und zwar nur
    //                       dort, wo noch ein OleDbParameter GEBAUT wird. Das sind nach
    //                       diesem Paket ausschliesslich die Masken unter Views/ (iU9)
    //                       und RecordSet.DBCommand (iR8).
    //   - NachOleDb()     : der Rueckweg fuer die zwei Stellen, die WIRKLICH noch eine
    //                       Access-Verbindung fuellen - der eingefrorene Access-Zweig der
    //                       SchemaMigration (Schritte 1-61, Hebung eines .accdb-Bestands
    //                       vor der Erstmigration) und GeraeteWaisen, das von dort seine
    //                       offene OleDbConnection hereingereicht bekommt. Beide sind wie
    //                       der EposSqliteMigrator per Definition Windows-Code; sie
    //                       tragen ihre Parameter jetzt trotzdem als DbParam und bauen
    //                       den OleDbParameter erst unmittelbar vor Parameters.AddRange().
    //
    // GEPLANTER ABBAU. Die Bruecke ist Uebergang, kein Dauerzustand:
    //   1. mit iU9 wandern die Masken nach EPOS.UI - damit entfaellt der implizite
    //      Operator und Von() (die Views sind die letzten Erbauer von OleDbParameter);
    //   2. RecordSet.DBCommand (public OleDbCommand, iR8) faellt mit denselben Masken
    //      oder wird vorher auf einen eigenen Typ gehoben;
    //   3. NachOleDb() lebt genau so lange wie der Access-Zweig der SchemaMigration -
    //      also bis der letzte .accdb-Bestand gehoben ist.
    // Danach kann System.Data.OleDb aus dem Projekt verschwinden.
    // =====================================================================================

    /// <summary>
    /// Datentyp eines <see cref="DbParam"/>. Die Namen sind bewusst die des frueheren
    /// <c>OleDbType</c> - der Bestand nutzte genau diese elf Werte, und die
    /// Gleichnamigkeit macht den Umstieg lesbar. Fuer SQLite ist die Angabe nur dort
    /// von Belang, wo ein Wert NULL sein kann; fuer den Access-Rueckweg
    /// (<see cref="DbParam.NachOleDb"/>) ist sie es immer.
    /// </summary>
    public enum DbParamTyp
    {
        /// <summary>Kein Typ angegeben - der Wert entscheidet (Regelfall im Bestand).</summary>
        Unbestimmt = 0,

        BigInt,
        Boolean,
        Date,
        Decimal,
        Double,
        Guid,
        Integer,
        LongVarWChar,
        VarBinary,
        VarWChar,
        Variant,
    }

    /// <summary>
    /// Providerfreier Parameter der Zugriffsschicht - der Ersatz fuer
    /// <c>OleDbParameter</c> als Datentraeger (Umsetzungskonzept iOS § 1.4, iF10).
    ///
    /// <para><b>Gebunden wird nach POSITION, nicht nach Name.</b> Das war schon bei OleDb
    /// so, und die Zugriffsschicht haelt es bei: <c>DataRepository.UebersetzeParameter</c>
    /// nummeriert strikt in Reihenfolge zu <c>@p0 … @pN</c> und wertet
    /// <see cref="Name"/> nicht aus. Der Name bleibt trotzdem erhalten - er ist die
    /// Lesehilfe an der Aufrufstelle, und der Rueckweg nach OleDb braucht ihn.</para>
    ///
    /// <para><b>ACHTUNG, UEBERLADUNGSFALLE (uebernommen von OleDbParameter).</b> Ein
    /// literales <c>0</c> als zweites Argument bindet an
    /// <see cref="DbParam(string, DbParamTyp)"/>, nicht an
    /// <see cref="DbParam(string, object)"/> - die Sprache laesst die literale Null in
    /// jeden Aufzaehlungstyp. Wo eine Null als WERT gemeint ist, deshalb den
    /// dreiargumentigen Weg ueber eine <c>Par(...)</c>-Fabrik nehmen oder
    /// <c>(object)0</c> schreiben. Im Bestand gibt es keine solche Stelle (nachgemessen
    /// am 02.09.2026); der Hinweis steht fuer neuen Code.</para>
    /// </summary>
    public sealed class DbParam
    {
        /// <summary>
        /// Der Name der Aufrufstelle ("?", "@p", "@id" - alles kommt vor). Reine
        /// Lesehilfe; die Bindung laeuft ueber die Position.
        /// </summary>
        public string Name { get; }

        /// <summary>Datentyp, falls die Aufrufstelle einen angegeben hat.</summary>
        public DbParamTyp Typ { get; set; }

        /// <summary>
        /// Feldlaenge, falls angegeben (im Bestand nur bei Textparametern); 0 = keine
        /// Angabe.
        /// </summary>
        public int Groesse { get; }

        private object _wert;

        /// <summary>
        /// Der Wert. <c>null</c> wird beim Setzen zu <see cref="DBNull.Value"/> -
        /// GENAU SO, wie <c>DataRepository.NormalisiereWert</c> es heute mit einem
        /// null-Wert macht ("w == null || w == DBNull.Value" =&gt; DBNull.Value). Die
        /// uebrigen Normalisierungen (bool =&gt; 0/1, DateTime =&gt; ISO-Text, Guid und
        /// decimal) bleiben bewusst DORT: sie gelten fuer die SQLite-Speicherform, nicht
        /// fuer den Datentraeger - der Access-Rueckweg und die Diagnoseausgaben brauchen
        /// den CLR-Typ unveraendert.
        /// </summary>
        public object Wert
        {
            get { return _wert; }
            set { _wert = value ?? DBNull.Value; }
        }

        /// <summary>Name und Wert - der Regelfall an rund 2.300 Aufrufstellen.</summary>
        public DbParam(string name, object wert)
        {
            Name = name;
            Typ = DbParamTyp.Unbestimmt;
            Groesse = 0;
            Wert = wert;
        }

        /// <summary>
        /// Name und ausdruecklicher Typ; der Wert wird anschliessend gesetzt. Noetig
        /// ueberall dort, wo der Wert <see cref="DBNull"/> sein kann - aus DBNull allein
        /// laesst sich kein Spaltentyp ableiten.
        /// </summary>
        public DbParam(string name, DbParamTyp typ)
        {
            Name = name;
            Typ = typ;
            Groesse = 0;
            Wert = null;
        }

        /// <summary>Wie <see cref="DbParam(string, DbParamTyp)"/>, zusaetzlich mit Feldlaenge.</summary>
        public DbParam(string name, DbParamTyp typ, int groesse)
        {
            Name = name;
            Typ = typ;
            Groesse = groesse;
            Wert = null;
        }

        /// <summary>Lesehilfe fuer Protokolle und den Debugger.</summary>
        public override string ToString()
        {
            string w = (_wert == null || _wert == DBNull.Value) ? "NULL" : _wert.ToString();
            return (Name ?? "?") + "=" + w + " [" + Typ + (Groesse > 0 ? "," + Groesse : "") + "]";
        }


        // =================================================================================
        // UEBERGANGSBRUECKE OleDbParameter <-> DbParam (Abbau siehe Kopfkommentar)
        // =================================================================================
        //
        // Der ganze Block ist Windows-Code: System.Data.OleDb wirft ausserhalb von Windows
        // schon im Konstruktor. Er wird auf Nicht-Windows nie AUFGERUFEN - deshalb laesst
        // sich diese Datei dort uebersetzen und ihr uebriger Inhalt ausfuehren.
#pragma warning disable CA1416 // Plattformkompatibilitaet: bewusst Windows-only, siehe oben

        /// <summary>
        /// Nimmt einen Altparameter entgegen, ohne dass die Aufrufstelle sich aendert.
        /// Damit kompilieren die noch nicht umgestellten Aufrufe
        /// <c>DataRepository.GetDataTable(sql, new OleDbParameter(...))</c> unveraendert
        /// weiter (nach diesem Paket: die Masken unter <c>Views/</c>, iU9).
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static implicit operator DbParam(OleDbParameter p)
        {
            if (p == null) return null;
            return new DbParam(p.ParameterName, AusOleDbTyp(p.OleDbType), p.Size) { Wert = p.Value };
        }

        /// <summary>
        /// Dasselbe fuer ein ganzes Array - fuer die Stellen, die ihre Parameter aus einer
        /// <c>OleDbParameterCollection</c> ziehen (<c>RecordSet</c>); der implizite
        /// Operator greift bei Arrays nicht.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static DbParam[] Von(OleDbParameter[] alt)
        {
            if (alt == null) return null;
            DbParam[] ziel = new DbParam[alt.Length];
            for (int i = 0; i < alt.Length; i++) ziel[i] = alt[i];   // impliziter Operator
            return ziel;
        }

        /// <summary>
        /// Der Rueckweg: baut aus Datentraegern echte OleDb-Parameter. NUR fuer die
        /// Stellen, die eine offene <c>OleDbConnection</c> auf eine <c>.accdb</c> fuellen
        /// - der eingefrorene Access-Zweig der <c>SchemaMigration</c> und
        /// <c>GeraeteWaisen</c>.
        ///
        /// <para>Die drei Faelle bilden exakt die Konstruktoren nach, die der Bestand
        /// dort benutzt hat: ohne Typangabe den zweiargumentigen Konstruktor (der Provider
        /// leitet den Typ aus dem Wert ab), mit Typangabe den typisierten und - wo eine
        /// Laenge angegeben war - den dreiargumentigen, in beiden Faellen mit
        /// anschliessender Wertzuweisung.</para>
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static OleDbParameter[] NachOleDb(DbParam[] quelle)
        {
            if (quelle == null) return null;
            OleDbParameter[] ziel = new OleDbParameter[quelle.Length];
            for (int i = 0; i < quelle.Length; i++)
            {
                DbParam q = quelle[i];
                if (q == null) { ziel[i] = null; continue; }

                if (q.Typ == DbParamTyp.Unbestimmt)
                {
                    ziel[i] = new OleDbParameter(q.Name, q.Wert);
                    continue;
                }

                OleDbType t = NachOleDbTyp(q.Typ);
                ziel[i] = q.Groesse > 0
                    ? new OleDbParameter(q.Name, t, q.Groesse) { Value = q.Wert }
                    : new OleDbParameter(q.Name, t) { Value = q.Wert };
            }
            return ziel;
        }

        /// <summary>OleDb-Typ =&gt; eigener Typ. Alles Unbekannte wird
        /// <see cref="DbParamTyp.Unbestimmt"/>; dann entscheidet der Wert.</summary>
        [SupportedOSPlatform("windows")]
        private static DbParamTyp AusOleDbTyp(OleDbType t)
        {
            switch (t)
            {
                case OleDbType.BigInt: return DbParamTyp.BigInt;
                case OleDbType.Boolean: return DbParamTyp.Boolean;
                case OleDbType.Date: return DbParamTyp.Date;
                case OleDbType.Decimal: return DbParamTyp.Decimal;
                case OleDbType.Double: return DbParamTyp.Double;
                case OleDbType.Guid: return DbParamTyp.Guid;
                case OleDbType.Integer: return DbParamTyp.Integer;
                case OleDbType.LongVarWChar: return DbParamTyp.LongVarWChar;
                case OleDbType.VarBinary: return DbParamTyp.VarBinary;
                case OleDbType.VarWChar: return DbParamTyp.VarWChar;
                case OleDbType.Variant: return DbParamTyp.Variant;
                default: return DbParamTyp.Unbestimmt;   // OleDbType.Empty und alles Uebrige
            }
        }

        /// <summary>Eigener Typ =&gt; OleDb-Typ (nur fuer <see cref="NachOleDb"/>).</summary>
        [SupportedOSPlatform("windows")]
        private static OleDbType NachOleDbTyp(DbParamTyp t)
        {
            switch (t)
            {
                case DbParamTyp.BigInt: return OleDbType.BigInt;
                case DbParamTyp.Boolean: return OleDbType.Boolean;
                case DbParamTyp.Date: return OleDbType.Date;
                case DbParamTyp.Decimal: return OleDbType.Decimal;
                case DbParamTyp.Double: return OleDbType.Double;
                case DbParamTyp.Guid: return OleDbType.Guid;
                case DbParamTyp.Integer: return OleDbType.Integer;
                case DbParamTyp.LongVarWChar: return OleDbType.LongVarWChar;
                case DbParamTyp.VarBinary: return OleDbType.VarBinary;
                case DbParamTyp.VarWChar: return OleDbType.VarWChar;
                case DbParamTyp.Variant: return OleDbType.Variant;
                default: return OleDbType.Empty;
            }
        }

#pragma warning restore CA1416
    }
}

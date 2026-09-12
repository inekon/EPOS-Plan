using System;

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
    // ARBEITSPAKET iU6-T3b: DIE UEBERGANGSBRUECKE NACH OleDb IST HIER RAUS.
    //
    // Bis T3a trug diese Datei den impliziten Operator aus OleDbParameter, Von() und
    // NachOleDb() samt Typabbildung - und mit ihnen ein "using System.Data.OleDb", das
    // EPOS.Kern an ein Windows-Paket band. Alle drei sind in die ANWENDUNG gewandert:
    //
    //   WindowsFormsApplication1/Allgemein/DbParamOleDb.cs
    //   [SupportedOSPlatform("windows")] internal static class DbParamOleDb
    //       Aus(OleDbParameter) / Von(OleDbParameter[]) / Nach(DbParam[])
    //
    // Aus()/Von() haben dort keinen Nutzer mehr - der Sweep aus T3a hat die letzten
    // Erbauer von OleDbParameter aus den Masken entfernt. Sie bleiben als Rueckfalltuer
    // fuer Zweige stehen, die noch aus einer Access-Verbindung lesen. Nach() traegt
    // weiterhin die vier Stellen des eingefrorenen Access-Zweigs (SchemaMigration:
    // NonQuery/Skalar/Abfrage, GeraeteWaisen: Ids) und lebt genau so lange wie er.
    //
    // Was hier bleibt, ist damit der reine Datentraeger - kein Provider, kein
    // Plattformbezug, kein #pragma. EPOS.Kern nennt System.Data.OleDb nicht mehr, weder
    // im Quelltext noch als PackageReference.
    // =====================================================================================

    /// <summary>
    /// Datentyp eines <see cref="DbParam"/>. Die Namen sind bewusst die des frueheren
    /// <c>OleDbType</c> - der Bestand nutzte genau diese elf Werte, und die
    /// Gleichnamigkeit macht den Umstieg lesbar. Fuer SQLite ist die Angabe nur dort
    /// von Belang, wo ein Wert NULL sein kann; fuer den Access-Rueckweg
    /// (<c>DbParamOleDb.Nach</c> in der Anwendung) ist sie es immer.
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
    /// am 02.09.2026, erneut vor dem Masken-Sweep iU6-T3a am 03.09.2026); der Hinweis
    /// steht fuer neuen Code.</para>
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
    }
}

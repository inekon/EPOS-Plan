using System;
using System.Data.OleDb;
using System.Runtime.Versioning;

namespace WindowsFormsApplication1
{
    // =====================================================================================
    // ARBEITSPAKET iU6-T3b: DIE BRUECKE OleDbParameter <-> DbParam - JETZT IN DER ANWENDUNG.
    //
    // Bis T3a stand dieser Block in EPOS.Kern/Allgemein/DbParam.cs und band den
    // plattformfreien Kern ueber ein "using System.Data.OleDb" an ein Windows-Paket. Die
    // Rumpfe sind WOERTLICH uebernommen (einschliesslich der Typabbildung in beide
    // Richtungen); geaendert sind nur die Namen: aus dem impliziten Operator wurde
    // Aus(), aus NachOleDb() wurde Nach().
    //
    // WOFUER JEDE HAELFTE DA IST:
    //
    //   Nach(DbParam[])  - der Rueckweg, der WIRKLICH gebraucht wird. Vier Aufrufstellen,
    //                      alle im eingefrorenen Access-Zweig (Arbeitspaket S6):
    //                      SchemaMigration.NonQuery/Skalar/Abfrage und GeraeteWaisen.Ids.
    //                      Sie tragen ihre Parameter als DbParam und bauen den echten
    //                      OleDbParameter erst unmittelbar vor Parameters.AddRange().
    //                      Lebt genau so lange wie der Access-Zweig - also bis der letzte
    //                      .accdb-Bestand gehoben ist.
    //
    //   Aus()/Von()      - der Hinweg. Nach dem Masken-Sweep aus iU6-T3a hat er KEINEN
    //                      Nutzer mehr: die Views bauen keine OleDbParameter mehr. Beide
    //                      bleiben als Rueckfalltuer fuer den Fall stehen, dass ein
    //                      Access-Zweig Parameter aus einer bestehenden
    //                      OleDbParameterCollection uebernehmen muss. Faellt mit dem
    //                      Access-Zweig.
    //
    // Der ganze Block ist Windows-Code: System.Data.OleDb wirft ausserhalb von Windows
    // schon im Konstruktor. Die Anwendung ist net10.0-windows - hier ist das folgenlos.
    // =====================================================================================

    [SupportedOSPlatform("windows")]
    internal static class DbParamOleDb
    {
        /// <summary>
        /// Nimmt einen Altparameter entgegen und macht daraus den providerfreien
        /// Datentraeger. Woertlich der frueher implizite Operator
        /// <c>DbParam(OleDbParameter)</c> aus dem Kern.
        /// </summary>
        public static DbParam Aus(OleDbParameter p)
        {
            if (p == null) return null;
            return new DbParam(p.ParameterName, AusOleDbTyp(p.OleDbType), p.Size) { Wert = p.Value };
        }

        /// <summary>
        /// Dasselbe fuer ein ganzes Array - fuer Stellen, die ihre Parameter aus einer
        /// <c>OleDbParameterCollection</c> ziehen.
        /// </summary>
        public static DbParam[] Von(OleDbParameter[] alt)
        {
            if (alt == null) return null;
            DbParam[] ziel = new DbParam[alt.Length];
            for (int i = 0; i < alt.Length; i++) ziel[i] = Aus(alt[i]);
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
        public static OleDbParameter[] Nach(DbParam[] quelle)
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

        /// <summary>Eigener Typ =&gt; OleDb-Typ (nur fuer <see cref="Nach"/>).</summary>
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
    }
}

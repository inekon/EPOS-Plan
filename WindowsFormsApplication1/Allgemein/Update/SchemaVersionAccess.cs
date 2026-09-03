using System;
using System.Data;
using System.Data.OleDb;
using System.Runtime.Versioning;

namespace WindowsFormsApplication1
{
    // =====================================================================================
    // ARBEITSPAKET iU6-T2: ACCESS-ZWEIG DER ERSTSTART-MIGRATION; NUR WINDOWS.
    //
    // Diese beiden Methoden standen bis iU6 in ApplikationCtrl (Rechenkern). Sie sind
    // WOERTLICH hierher uebernommen - Rumpf, Kommentare und Zusagen unveraendert -, weil
    // EPOS.Kern plattformfrei ist und System.Data.OleDb dort nicht mehr vorkommen darf.
    //
    // Kein partial: ApplikationCtrl liegt im Kern, und eine partial-Haelfte laesst sich
    // ueber Assemblygrenzen hinweg nicht beisteuern. Die beiden Methoden sind ohnehin
    // static und beruehren keinen Instanzzustand; aus ApplikationCtrl brauchen sie nur
    // die Namenskonstante SPALTE_SCHEMAVERSION.
    //
    // EINZIGER AUFRUFER: SchemaMigration.HebeAltbestand - der eingefrorene Access-Zweig,
    // der einen vorhandenen .accdb-Bestand vor der Erstmigration auf Zielstand 61 hebt.
    // Die Verbindung kommt von dort HEREINGEREICHT (ausdruecklicher ACE-Verbindungsstring
    // auf den .accdb-Pfad); DataRepository liefert seit S4a den SQLite-String und ist
    // hier deshalb bewusst nicht im Spiel.
    // =====================================================================================

    [SupportedOSPlatform("windows")]
    internal static class SchemaVersionAccess
    {
        // =========================================================================
        // Schemamarker im ALTBESTAND (ARBEITSPAKET S6, eingefrorener Access-Zweig)
        //
        // Formgleich mit ApplikationCtrl.GetSchemaVersion/SetSchemaVersion - nur eben
        // ueber OleDb auf einer
        // HEREINGEREICHTEN Verbindung. Drei Unterschiede, alle drei Absicht:
        //
        //   1. KEIN DataRepository. Weder Verbindungsstring noch Zugriffsmethode: Beides
        //      zeigt seit S4a auf die SQLite-Datei. Die Verbindung kommt vom Aufrufer
        //      (SchemaMigration.HebeAltbestand baut sie aus einem ausdruecklichen
        //      ACE-Verbindungsstring auf den .accdb-Pfad).
        //   2. SELECT TOP 1 statt LIMIT 1. Die Zugriffsschicht ist auf SQLite umgestellt,
        //      der Altbestand aber nicht: ACE kennt kein LIMIT. Der Dialektwechsel aus S5
        //      gilt hier ausdruecklich NICHT.
        //   3. Ebenso TOLERANT wie die SQLite-Fassung: fehlende Spalte, fehlende Zeile,
        //      fehlende Tabelle -> Version 0, ohne Dialog und ohne Ausnahme. Genau das
        //      braucht der Bootstrap der Migration, der die Markerspalte erst anlegt.
        // =========================================================================

        /// <summary>
        /// Schemastand des ALTBESTANDS ueber die hereingereichte OleDb-Verbindung.
        /// 0 bedeutet "noch nichts migriert" - auch dann, wenn Spalte, Zeile oder
        /// Tabelle fehlen.
        /// </summary>
        internal static int GetSchemaVersionOleDb(OleDbConnection verbindung)
        {
            if (verbindung == null) return 0;
            try
            {
                DataTable dt = new DataTable();
                using (OleDbCommand cmd =
                           new OleDbCommand("SELECT TOP 1 * FROM Tab_Applikation", verbindung))
                using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
                {
                    adapter.Fill(dt);
                }

                if (!dt.Columns.Contains(ApplikationCtrl.SPALTE_SCHEMAVERSION)) return 0;  // Spalte fehlt -> Version 0
                if (dt.Rows.Count == 0) return 0;                          // Zeile fehlt  -> Version 0

                object v = dt.Rows[0][ApplikationCtrl.SPALTE_SCHEMAVERSION];
                if (v == null || v == DBNull.Value) return 0;
                return Convert.ToInt32(v);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Schreibt den Schemastand in den ALTBESTAND. Rueckgabe false, wenn nichts
        /// geschrieben werden konnte - die Migration wertet das als Fehlschlag des
        /// Schritts (gleiche Zusage wie <c>ApplikationCtrl.SetSchemaVersion</c>).
        /// </summary>
        internal static bool SetSchemaVersionOleDb(OleDbConnection verbindung, int version)
        {
            if (verbindung == null) return false;
            try
            {
                using (OleDbCommand cmd = new OleDbCommand(
                           "UPDATE Tab_Applikation SET [" + ApplikationCtrl.SPALTE_SCHEMAVERSION + "] = ?", verbindung))
                {
                    // iU6: KEIN DbParam - hier wird eine echte OleDbCommand-Sammlung
                    // auf einer Access-Verbindung gefuellt (Altbestand-Zweig).
                    cmd.Parameters.Add(new OleDbParameter("@v", version));
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}

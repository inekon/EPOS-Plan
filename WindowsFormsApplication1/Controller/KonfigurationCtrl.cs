using System;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class KonfigurationCtrl : KonfigurationModel
    {
        public KonfigurationModel model = new KonfigurationModel();
        public int rows;

        public enum Energieerzeuger
        {
            BHKW = 0,
            HEIZKESSEL = 1,
            PHOTOVOLTAIK = 2,
            SOLARTHERMIE = 3,
            WAERMEPUMPE = 4
        }


        public KonfigurationCtrl()
        {
            rows = 0;
        }

        ~KonfigurationCtrl()
        {
            rows = 0;
        }

        public void ReadSingle(string sql)
        {
            rows = 0;

            // Nutzt dein DataRepository (intern OLEDB) statt ODBC
            DataTable dt = DataRepository.GetDataTable(sql);

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                if (row[0] != DBNull.Value) model.m_ID = Convert.ToInt32(row[0]);
                if (row[1] != DBNull.Value) model.m_ID_Projekt = Convert.ToInt32(row[1]);
                if (row[2] != DBNull.Value) model.m_BHKW_Grenzleistung = Convert.ToDouble(row[2]);
                if (row[3] != DBNull.Value) model.m_Netzverluste = Convert.ToDouble(row[3]);
                if (row[4] != DBNull.Value) model.m_szNetzverlusteEinheit = row[4].ToString();
                if (row[5] != DBNull.Value) model.m_WP_Heizstab = Convert.ToBoolean(row[5]);
                if (row[6] != DBNull.Value) model.m_Kessel_Betriebsbereitschaft = Convert.ToInt32(row[6]);
                if (row[7] != DBNull.Value) model.m_Tool_1 = row[7].ToString();
                if (row[8] != DBNull.Value) model.m_Tool_2 = row[8].ToString();
                if (row[9] != DBNull.Value) model.m_Tool_3 = row[9].ToString();
                if (row[10] != DBNull.Value) model.m_Tool_4 = row[10].ToString();
                if (row[11] != DBNull.Value) model.m_Tool_5 = row[11].ToString();
                if (row[12] != DBNull.Value) model.m_Tool_6 = row[12].ToString();
                if (row[13] != DBNull.Value) model.m_Ladefuellstand_Min = Convert.ToInt32(row[13]);
                if (row[14] != DBNull.Value) model.m_Ladefuellstand_Max = Convert.ToInt32(row[14]);
                if (row[15] != DBNull.Value) model.m_Ladeleistung_Max = Convert.ToInt32(row[15]);
                if (row[16] != DBNull.Value) model.m_Ladefuellstand_Min_Auswahl = row[16].ToString();
                if (row[17] != DBNull.Value) model.m_Ladefuellstand_Max_Auswahl = row[17].ToString();
                if (row[18] != DBNull.Value) model.m_Ladeleistung_Max_Auswahl = row[18].ToString();
                if (row[19] != DBNull.Value) model.m_Ladeschwellwert = Convert.ToDouble(row[19]);
                if (row[20] != DBNull.Value) model.Betriebsart = Convert.ToInt32(row[20]);
                if (row[21] != DBNull.Value) model.Leistungsgrenze = Convert.ToInt32(row[21]);
                if (row[22] != DBNull.Value) model.Pendelspeicher = Convert.ToDouble(row[22]);

                // --- Feature-Flag der zweikanaligen Kaskade (Paket 4, Etappe 4a) -------
                //
                // NAMENSBASIERT, bewusst NICHT als row[24] an die Ordinalkette angehängt:
                // Die Kette oben ist an die physische Spaltenreihenfolge von
                // Tab_Einstellungen gebunden und damit die brüchigste Stelle des
                // Datenzugriffs - jede weitere Position macht sie nur länger. Über den
                // Spaltennamen ist der Zugriff unabhängig davon, an welcher Position die
                // Migration die Spalte angehängt hat.
                //
                // Fehlt die Spalte (Datenbank noch nicht auf Schemastand 6), bleibt es
                // bei "aus" - dem Vorgabeverhalten des Flags. Deshalb wird der Wert in
                // BEIDEN Zweigen gesetzt und nicht nur bei Treffer: ein wiederverwendetes
                // Model dürfte sonst den Stand des zuvor gelesenen Projekts behalten.
                model.Kaskade_Zweikanalig =
                    dt.Columns.Contains(SchemaKatalog.SPALTE_KASKADE_ZWEIKANALIG) &&
                    row[SchemaKatalog.SPALTE_KASKADE_ZWEIKANALIG] != DBNull.Value &&
                    Convert.ToBoolean(row[SchemaKatalog.SPALTE_KASKADE_ZWEIKANALIG]);

                // --- Einstellung Extrapolation_erlaubt (Paket 8, Konzept 13.4) --------
                //
                // Dasselbe namensbasierte Muster wie beim Feature-Flag darüber, aber mit
                // UMGEKEHRTER Vorbelegung: Fehlt die Spalte (Datenbank noch nicht auf
                // Schemastand 7) oder steht dort NULL, gilt ERLAUBT. Das ist genau das
                // bisherige Verhalten - die Engine fragte nach, und die Antwort war in
                // jedem dokumentierten Lauf "Ja". Ein "verboten" darf deshalb nur aus
                // einem ausdrücklich gesetzten FALSE kommen, nie aus einer Datenlücke.
                //
                // NACHARBEIT PAKET 8, BEFUND N8 — der nie vorbelegte Zustand.
                // Es reicht nicht, fehlende Spalte und NULL abzufangen: Die Spalte steht
                // seit Paket 1 in SchemaKatalog.Schritt2_Speicher und wird deshalb auch
                // von der stillen Rückfallebene (WaermequelleClass.SchemaSicherstellen)
                // angelegt - mit dem Access-Default FALSE, denn ein Ja/Nein-Feld kennt
                // kein NULL. Auf einer Datenbank, die diese Spalte hat, aber
                // Migrationsschritt 7 noch nicht gelaufen ist, stünde damit überall
                // "verboten", und jeder extrapolierende Wärmepumpenlauf bräche ab. Genau
                // das trifft die Referenzlauf-Suite in Weg B: Der Modus "projekt"
                // migriert nicht. Solange der Schemastand unter 7 liegt, ist das FALSE
                // deshalb kein Anwenderwille, sondern eine Datenlücke - und die bedeutet
                // ERLAUBT, wie überall sonst bei dieser Einstellung.
                model.Extrapolation_erlaubt =
                    !dt.Columns.Contains(SchemaKatalog.SPALTE_EXTRAPOLATION_ERLAUBT) ||
                    row[SchemaKatalog.SPALTE_EXTRAPOLATION_ERLAUBT] == DBNull.Value ||
                    Convert.ToBoolean(row[SchemaKatalog.SPALTE_EXTRAPOLATION_ERLAUBT]) ||
                    ExtrapolationVorbelegungFehlt();

                rows = 1;
            }
        }

        /// <summary>
        /// Liest das Feature-Flag <c>Kaskade_Zweikanalig</c> eines Projekts DIALOGFREI
        /// (Paket 4, Etappe 4a) - für die Oberfläche, die den Schalter anzeigt, ohne den
        /// ganzen Einstellungssatz zu laden.
        ///
        /// Fehlende Spalte, fehlende Zeile und NULL liefern gleichermaßen <c>false</c>;
        /// das ist die Vorbelegung des Flags.
        /// </summary>
        public static bool KaskadeZweikanaligLesen(int idProjekt)
        {
            if (idProjekt <= 0) return false;

            object v = StilleDb.Scalar(
                "SELECT [" + SchemaKatalog.SPALTE_KASKADE_ZWEIKANALIG + "] " +
                "FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt));

            if (v == null) return false;
            try { return Convert.ToBoolean(v); }
            catch { return false; }
        }

        /// <summary>
        /// Schreibt das Feature-Flag <c>Kaskade_Zweikanalig</c> eines Projekts.
        ///
        /// Bewusst ein EIGENES, zielgenaues UPDATE statt einer Erweiterung von
        /// <see cref="Update"/>: Dessen Spaltenliste und die von <see cref="Insert"/>
        /// sind an die Ordinalkette in <see cref="ReadSingle"/> gekoppelt, und auf einer
        /// Datenbank ohne Schemastand 6 würde ein erweitertes UPDATE das Speichern der
        /// GESAMTEN Konfiguration scheitern lassen - wegen eines Vorschauschalters.
        ///
        /// Dialogfrei (Konzept 13.4). Rückgabe false, wenn keine Zeile getroffen wurde
        /// (Projekt ohne Einstellungssatz) oder die Spalte fehlt.
        /// </summary>
        public static bool KaskadeZweikanaligSchreiben(int idProjekt, bool wert)
        {
            if (idProjekt <= 0) return false;

            int betroffen = StilleDb.NonQuery(
                "UPDATE Tab_Einstellungen SET [" + SchemaKatalog.SPALTE_KASKADE_ZWEIKANALIG + "] = ? " +
                "WHERE ID_Projekt = ?",
                StilleDb.Par("@wert", OleDbType.Boolean, wert),
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt));

            return betroffen > 0;
        }

        /// <summary>
        /// Liest die Einstellung <c>Extrapolation_erlaubt</c> eines Projekts DIALOGFREI
        /// (Paket 8, Konzept 13.4) — für die Oberfläche, die den Schalter anzeigt, ohne
        /// den ganzen Einstellungssatz zu laden.
        ///
        /// Fehlende Spalte, fehlende Zeile und NULL liefern gleichermaßen <c>true</c>;
        /// das ist die Vorbelegung der Einstellung und das bisherige Verhalten.
        /// </summary>
        public static bool ExtrapolationErlaubtLesen(int idProjekt)
        {
            if (idProjekt <= 0) return true;

            object v = StilleDb.Scalar(
                "SELECT [" + SchemaKatalog.SPALTE_EXTRAPOLATION_ERLAUBT + "] " +
                "FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt));

            if (v == null) return true;
            try { if (Convert.ToBoolean(v)) return true; }
            catch { return true; }

            // Befund N8: ein FALSE aus einer Datenbank ohne Migrationsschritt 7 ist die
            // Vorbelegung von Access, nicht der Wille des Anwenders (Begründung in
            // ReadSingle).
            return ExtrapolationVorbelegungFehlt();
        }

        /// <summary>
        /// true, solange die Datenbank den Migrationsschritt 7 (Vorbelegung
        /// <c>Extrapolation_erlaubt = WAHR</c>) noch nicht hinter sich hat — dann ist ein
        /// gespeichertes FALSE die Access-Vorbelegung einer angehängten YESNO-Spalte und
        /// nicht die Entscheidung des Anwenders (Nacharbeit Paket 8, Befund N8).
        ///
        /// Bewusst LESEND: Die Alternative wäre gewesen, die stille Rückfallebene
        /// <c>WaermequelleClass.SchemaSicherstellen</c> die Spalte nachvorbelegen zu
        /// lassen. Das trägt nicht — sie läuft erst in <c>Do_Simulation</c>, also NACH
        /// dem Lesen der Konfiguration im <c>SimulationRunner</c>, und hätte den
        /// laufenden Lauf nicht mehr erreicht. Ein Leser, der die Datenlücke erkennt,
        /// wirkt sofort und schreibt nichts in eine fremde Datenbank.
        ///
        /// Der erreichte Zielstand wird gemerkt: Auf einer gepflegten Datenbank fällt
        /// genau ein Marker-Lesevorgang je Programmlauf an, danach nichts mehr.
        /// </summary>
        private static bool _schemastand7Erreicht = false;

        private static bool ExtrapolationVorbelegungFehlt()
        {
            if (_schemastand7Erreicht) return false;

            try
            {
                if (ApplikationCtrl.GetSchemaVersion() >= SchemaMigration.SCHRITT_7_EXTRAPOLATION)
                {
                    _schemastand7Erreicht = true;
                    return false;
                }
            }
            catch { /* Marker nicht lesbar - dann gilt die Datenlücke */ }

            return true;
        }

        /// <summary>
        /// Schreibt die Einstellung <c>Extrapolation_erlaubt</c> eines Projekts.
        ///
        /// Bewusst ein EIGENES, zielgenaues UPDATE statt einer Erweiterung von
        /// <see cref="Update"/> — dieselbe Begründung wie bei
        /// <see cref="KaskadeZweikanaligSchreiben"/>: Die Spaltenlisten von
        /// <see cref="Insert"/>/<see cref="Update"/> hängen an der Ordinalkette in
        /// <see cref="ReadSingle"/>, und auf einer Datenbank ohne die Spalte würde ein
        /// erweitertes UPDATE das Speichern der GESAMTEN Konfiguration scheitern lassen.
        ///
        /// Dialogfrei (Konzept 13.4). Rückgabe false, wenn keine Zeile getroffen wurde
        /// oder die Spalte fehlt.
        /// </summary>
        public static bool ExtrapolationErlaubtSchreiben(int idProjekt, bool wert)
        {
            if (idProjekt <= 0) return false;

            int betroffen = StilleDb.NonQuery(
                "UPDATE Tab_Einstellungen SET [" + SchemaKatalog.SPALTE_EXTRAPOLATION_ERLAUBT + "] = ? " +
                "WHERE ID_Projekt = ?",
                StilleDb.Par("@wert", OleDbType.Boolean, wert),
                StilleDb.Par("@proj", OleDbType.Integer, idProjekt));

            return betroffen > 0;
        }

        public bool Insert(int ID_Projekt)
        {
            try
            {
                // Umstellung auf sichere Parameter-Marker (?) statt ungesicherter String-Verkettung
                string sql = @"
                    INSERT INTO TAB_Einstellungen 
                    (
                        ID_Projekt, BHKW_Grenzleistung, Netzverluste, NetzverlusteEinheit, 
                        WP_Heizstab, Kessel_Betriebsbereitschaft, 
                        Tool_1, Tool_2, Tool_3, Tool_4, Tool_5, Tool_6,
                        Ladefuellstand_Min, Ladefuellstand_Max, Ladeleistung_Max,
                        Ladefuellstand_Min_Auswahl, Ladefuellstand_Max_Auswahl, 
                        Ladeleistung_Max_Auswahl, Ladeschwellwert, Betriebsart, Leistungsgrenze, Pendelspeicher
                    ) 
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                // Die Parameter werden als OLEDB-Objekte an dein DataRepository gereicht
                OleDbParameter[] parameters = new OleDbParameter[]
                {
                    new OleDbParameter("?", ID_Projekt),
                    new OleDbParameter("?", model.m_BHKW_Grenzleistung),
                    new OleDbParameter("?", model.m_Netzverluste),
                    new OleDbParameter("?", model.m_szNetzverlusteEinheit ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_WP_Heizstab),
                    new OleDbParameter("?", model.m_Kessel_Betriebsbereitschaft),
                    new OleDbParameter("?", model.m_Tool_1 ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_Tool_2 ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_Tool_3 ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_Tool_4 ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_Tool_5 ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_Tool_6 ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_Ladefuellstand_Min),
                    new OleDbParameter("?", model.m_Ladefuellstand_Max),
                    new OleDbParameter("?", model.m_Ladeleistung_Max),
                    new OleDbParameter("?", model.m_Ladefuellstand_Min_Auswahl ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_Ladefuellstand_Max_Auswahl ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_Ladeleistung_Max_Auswahl ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_Ladeschwellwert),
                    new OleDbParameter("?", model.Betriebsart),
                    new OleDbParameter("?", model.Leistungsgrenze),
                    new OleDbParameter("?", model.Pendelspeicher)
                };

                // Übergabe an das DataRepository
                DataRepository.ExecuteNonQuery(sql, parameters);

                // PAKET 8 (Konzept 13.4): Die Spaltenliste oben bleibt unangetastet -
                // sie gehört zur Ordinalkette von ReadSingle, und auf einer Datenbank
                // ohne Schemastand 7 würde ein erweitertes INSERT das Anlegen der
                // GESAMTEN Konfiguration scheitern lassen. Die Vorbelegung kommt
                // deshalb als eigenes, stilles UPDATE hinterher: Access belegt eine
                // angehängte YESNO-Spalte in einer neuen Zeile mit False - ohne diese
                // Zeile stünde jedes NEUE Projekt auf "Extrapolation verboten" und
                // damit auf anderem Verhalten als der migrierte Bestand.
                ExtrapolationErlaubtSchreiben(ID_Projekt, true);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Einfügen der Konfiguration: " + ex.Message);
                MessageBox.Show("Allgemeiner Fehler: " + ex.Message);
                return false;
            }
        }

        public bool Update(int ID_Projekt)
        {
            try
            {
                // SQL-Update-String mit Positions-Parametern (?)
                string sql = @"
            UPDATE TAB_Einstellungen 
            SET 
                BHKW_Grenzleistung = ?, 
                Netzverluste = ?, 
                NetzverlusteEinheit = ?, 
                WP_Heizstab = ?, 
                Kessel_Betriebsbereitschaft = ?, 
                Tool_1 = ?, 
                Tool_2 = ?, 
                Tool_3 = ?, 
                Tool_4 = ?, 
                Tool_5 = ?, 
                Tool_6 = ?,
                Ladefuellstand_Min = ?, 
                Ladefuellstand_Max = ?, 
                Ladeleistung_Max = ?,
                Ladefuellstand_Min_Auswahl = ?, 
                Ladefuellstand_Max_Auswahl = ?, 
                Ladeleistung_Max_Auswahl = ?, 
                Ladeschwellwert = ?,
                Betriebsart = ?,
                Leistungsgrenze = ?,
                Pendelspeicher = ?
            WHERE ID_Projekt = ?";

                // Die Parameter-Reihenfolge entspricht exakt den Fragezeichen im SQL-String
                OleDbParameter[] parameters = new OleDbParameter[]
                {
            new OleDbParameter("?", model.m_BHKW_Grenzleistung),
            new OleDbParameter("?", model.m_Netzverluste),
            new OleDbParameter("?", model.m_szNetzverlusteEinheit ?? (object)DBNull.Value),
            new OleDbParameter("?", model.m_WP_Heizstab),
            new OleDbParameter("?", model.m_Kessel_Betriebsbereitschaft),
            new OleDbParameter("?", model.m_Tool_1 ?? (object)DBNull.Value),
            new OleDbParameter("?", model.m_Tool_2 ?? (object)DBNull.Value),
            new OleDbParameter("?", model.m_Tool_3 ?? (object)DBNull.Value),
            new OleDbParameter("?", model.m_Tool_4 ?? (object)DBNull.Value),
            new OleDbParameter("?", model.m_Tool_5 ?? (object)DBNull.Value),
            new OleDbParameter("?", model.m_Tool_6 ?? (object)DBNull.Value),
            new OleDbParameter("?", model.m_Ladefuellstand_Min),
            new OleDbParameter("?", model.m_Ladefuellstand_Max),
            new OleDbParameter("?", model.m_Ladeleistung_Max),
            new OleDbParameter("?", model.m_Ladefuellstand_Min_Auswahl ?? (object)DBNull.Value),
            new OleDbParameter("?", model.m_Ladefuellstand_Max_Auswahl ?? (object)DBNull.Value),
            new OleDbParameter("?", model.m_Ladeleistung_Max_Auswahl ?? (object)DBNull.Value),
            new OleDbParameter("?", model.m_Ladeschwellwert),
            new OleDbParameter("?", model.Betriebsart),
            new OleDbParameter("?", model.Leistungsgrenze),
            new OleDbParameter("?", model.Pendelspeicher),
            // ID_Projekt steht am Ende, weil das WHERE-Statement ganz unten steht!
            new OleDbParameter("?", ID_Projekt)
                };

                // Übergabe an dein bestehendes DataRepository
                DataRepository.ExecuteNonQuery(sql, parameters);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Konfiguration: " + ex.Message);
                MessageBox.Show("Allgemeiner Fehler beim Speichern: " + ex.Message);
                return false;
            }
        }

        public bool Delete(int ID_Projekt)
        {
            try
            {
                // Sauberes ANSI-SQL für OLEDB ohne das ungültige "DELETE *"
                string sql = "DELETE FROM Tab_Einstellungen WHERE ID_Projekt = ?";
                OleDbParameter parameter = new OleDbParameter("?", ID_Projekt);

                DataRepository.ExecuteNonQuery(sql, parameter);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Löschen der Konfiguration: " + ex.Message);
                return false;
            }
        }
    }
}

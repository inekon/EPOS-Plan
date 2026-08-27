using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Zugriff auf die Alt-Zuordnung <c>Z_ProjektPufferSp</c> (Projekt ↔ Pufferspeicher
    /// je Erzeuger, mit Betriebstemperaturen, Priorität und Speicherschwellen).
    ///
    /// <b>STILLGELEGT ab Migrationsschritt 51 / Paket A1</b> (Konzept
    /// <c>Konzept_Brauchwasser_Heizung_Pufferspeicher.md</c> § 9, Leitentscheidung L1):
    /// Die Tabelle wird von der Anwendung nicht mehr gelesen und nicht mehr geschrieben.
    /// Ihre beiden Aufgaben sind abgelöst — die Senkenzuordnung durch
    /// <c>Z_AnlageSenke</c> (Schritt 50), die Betriebstemperaturen durch die
    /// Projektkopie <c>Tab_Pufferspeicher.Vorlauf</c>/<c>Ruecklauf</c>, an die
    /// Schritt 51 die noch fehlenden Paare einmalig übernommen hat
    /// (<c>SchemaMigration.SCHRITT_51_ALTPFAD_STILLLEGUNG</c>). Mit dem Altpfad
    /// entfallen auch der Dialog <c>Form_KonfigPufferspeicher</c> und die Brücke
    /// <c>WpSenkeSpiegeln</c>.
    ///
    /// <b>Die Klasse bleibt bis zum Aufräumpaket L stehen</b> — unverändert, ohne
    /// Aufrufer. Sie ist der Rückweg, solange die Tabelle selbst noch in der Datenbank
    /// steht (auch die wird von Schritt 51 ausdrücklich NICHT gelöscht): Sollte sich in
    /// einem Bestand zeigen, dass die Übernahme etwas übersehen hat, ist die Leseseite
    /// dafür noch da. Neuer Code darf sie nicht mehr verwenden.
    /// </summary>
    class Z_ProjektPufferSpCtrl : Z_ProjektPufferSpModel
    {
        private List<Z_ProjektPufferSpModel> _internalList = new List<Z_ProjektPufferSpModel>();

        public int rows => _internalList.Count;
        public new List<Z_ProjektPufferSpModel> items => _internalList;

        public Z_ProjektPufferSpModel model;

        public Z_ProjektPufferSpCtrl()
        {
            model = new Z_ProjektPufferSpModel();
        }

        public bool Delete()
        {
            try
            {
                string sql = "DELETE FROM Z_ProjektPufferSp WHERE ID_Projekt = ?";
                OleDbParameter[] ps = { new OleDbParameter("@idProj", ID_Projekt) };

                return DataRepository.ExecuteSQL(sql, ps);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Delete: " + ex.Message);
                return false;
            }
        }

        // B0-1: Einmalige Prüfung je Prozess, ob die Schwellen-Spalten existieren.
        // SchemaSicherstellen legt sie zwar an, schluckt ein fehlgeschlagenes ALTER
        // aber still (schreibgeschützte/gesperrte DB). Insert fällt dann auf die alte
        // 7-Spalten-Variante zurück, statt nach dem Delete des Speicherns sämtliche
        // Zuordnungen des Projekts zu verlieren.
        private static bool? _schwellenSpalten;
        private static bool SchwellenSpaltenVorhanden()
        {
            if (_schwellenSpalten.HasValue) return _schwellenSpalten.Value;
            try
            {
                DataTable dt = DataRepository.GetDataTable("SELECT TOP 1 * FROM Z_ProjektPufferSp");
                _schwellenSpalten = dt != null && dt.Columns.Contains("Schwelle_Ein")
                                               && dt.Columns.Contains("Schwelle_Aus");
            }
            catch { _schwellenSpalten = false; }
            return _schwellenSpalten.Value;
        }

        public bool Insert()
        {
            try
            {
                // Seit der DB-Migration hat Z_ProjektPufferSp die Pflichtspalte ID_Pufferspeicher
                // mit erzwungener Beziehung auf Tab_Pufferspeicher.ID (Projekt-Tabelle).
                // Die ID wird hier immer frisch aus dem Bezeichner + Projekt aufgeloest;
                // fehlt die Projektkopie, wird sie aus den Stammdaten angelegt (Muster wie
                // bei Heizkessel/BHKW, siehe PufferSpCtrl.CopyFromStamm).
                PufferSpCtrl psp = new PufferSpCtrl();
                int idPuffer = psp.GetProjektId(PufferSp, ID_Projekt);
                if (idPuffer <= 0) idPuffer = psp.CopyFromStamm(PufferSp, ID_Projekt);
                if (idPuffer <= 0)
                {
                    // PAKET 8 (Konzept 13.4): Das Konzept führt diese Stelle als eine der
                    // acht Engine-MessageBoxen. AM HEUTIGEN STAND ist sie das nicht mehr:
                    // Die Simulation liest Z_ProjektPufferSp nur noch (ReadAll in
                    // SimulationControl), INSERT kommt ausschließlich aus
                    // Form_Simulation_Config.btn_Speichern_Click. Der Dialog bleibt dort
                    // also richtig - und geht trotzdem über die gemeinsame
                    // Entscheidungsstelle, damit er im Engine-Modus stumm bliebe.
                    DataRepository.FehlerMelden(
                        "Der Pufferspeicher '" + PufferSp + "' wurde weder im Projekt noch in den " +
                        "Stammdaten gefunden!\nDie Zuordnung kann nicht gespeichert werden.");
                    return false;
                }
                ID_Pufferspeicher = idPuffer;

                // Umstellung von unparametrisiertem SELECT-String auf standardkonformes VALUES-Statement mit Parametern
                // B0-1: Schwelle_Ein/Schwelle_Aus mitschreiben — sie hängen an der
                // Zuordnungszeile und gingen beim Delete/Insert-Zyklus des Speicherns
                // sonst verloren (stiller Rückfall auf 10/95 %).
                if (SchwellenSpaltenVorhanden())
                {
                    string sql = @"INSERT INTO Z_ProjektPufferSp
                                   (
                                       ID_Projekt, ID_Pufferspeicher, Erzeuger, Pufferspeicher,
                                       Vorlauf, Ruecklauf, Prioritaet, Schwelle_Ein, Schwelle_Aus
                                   )
                                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";

                    OleDbParameter[] ps = {
                        new OleDbParameter("@idProj", ID_Projekt),
                        new OleDbParameter("@idPuf", idPuffer),
                        new OleDbParameter("@erz", Erzeuger ?? (object)DBNull.Value),
                        new OleDbParameter("@puf", PufferSp ?? (object)DBNull.Value),
                        new OleDbParameter("@vor", Vorlauf),
                        new OleDbParameter("@rue", Ruecklauf),
                        new OleDbParameter("@prio", Prioritaet),
                        new OleDbParameter("@sEin", Schwelle_Ein.HasValue ? (object)Schwelle_Ein.Value : DBNull.Value),
                        new OleDbParameter("@sAus", Schwelle_Aus.HasValue ? (object)Schwelle_Aus.Value : DBNull.Value)
                    };

                    return DataRepository.ExecuteSQL(sql, ps);
                }
                else
                {
                    // Rückfallebene: Schema ohne Schwellen-Spalten — Zuordnung ohne
                    // Schwellen speichern (Verhalten wie vor B0-1), kein Datenverlust.
                    string sql = @"INSERT INTO Z_ProjektPufferSp
                                   (
                                       ID_Projekt, ID_Pufferspeicher, Erzeuger, Pufferspeicher,
                                       Vorlauf, Ruecklauf, Prioritaet
                                   )
                                   VALUES (?, ?, ?, ?, ?, ?, ?)";

                    OleDbParameter[] ps = {
                        new OleDbParameter("@idProj", ID_Projekt),
                        new OleDbParameter("@idPuf", idPuffer),
                        new OleDbParameter("@erz", Erzeuger ?? (object)DBNull.Value),
                        new OleDbParameter("@puf", PufferSp ?? (object)DBNull.Value),
                        new OleDbParameter("@vor", Vorlauf),
                        new OleDbParameter("@rue", Ruecklauf),
                        new OleDbParameter("@prio", Prioritaet)
                    };

                    return DataRepository.ExecuteSQL(sql, ps);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Insert: " + ex.Message);
                return false;
            }
        }

        public void ReadAll(string szFilter)
        {
            string sql;
            if (string.IsNullOrEmpty(szFilter))
            {
                sql = "SELECT * FROM Z_ProjektPufferSp ORDER BY Prioritaet";
            }
            else
            {
                sql = "SELECT * FROM Z_ProjektPufferSp WHERE " + szFilter + " ORDER BY Prioritaet";
            }

            // Abfrage über das zentrale DataRepository laden
            DataTable dt = DataRepository.GetDataTable(sql, null);

            // Interne Liste vor dem erneuten Befüllen leeren
            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                Z_ProjektPufferSpModel item = new Z_ProjektPufferSpModel();

                // Sicheres Auslesen über Spaltennamen statt fehleranfälliger numerischer Indizes
                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                    item.ID = Convert.ToInt32(row["ID"]);

                if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value)
                    item.ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);

                if (dt.Columns.Contains("ID_Pufferspeicher") && row["ID_Pufferspeicher"] != DBNull.Value)
                    item.ID_Pufferspeicher = Convert.ToInt32(row["ID_Pufferspeicher"]);

                if (dt.Columns.Contains("Erzeuger") && row["Erzeuger"] != DBNull.Value)
                    item.Erzeuger = row["Erzeuger"].ToString();

                // Beachtet die Namensänderung beim Mapping (Pufferspeicher Spalte -> Property PufferSp)
                if (dt.Columns.Contains("Pufferspeicher") && row["Pufferspeicher"] != DBNull.Value)
                    item.PufferSp = row["Pufferspeicher"].ToString();

                if (dt.Columns.Contains("Vorlauf") && row["Vorlauf"] != DBNull.Value)
                    item.Vorlauf = Convert.ToInt32(row["Vorlauf"]);

                if (dt.Columns.Contains("Ruecklauf") && row["Ruecklauf"] != DBNull.Value)
                    item.Ruecklauf = Convert.ToInt32(row["Ruecklauf"]);

                if (dt.Columns.Contains("Prioritaet") && row["Prioritaet"] != DBNull.Value)
                    item.Prioritaet = Convert.ToInt32(row["Prioritaet"]);

                // B0-1: Schwellen der Speicherregelung mitlesen (Spalten existieren nach
                // SchemaSicherstellen; in Alt-Datenbanken bleiben sie ggf. null)
                if (dt.Columns.Contains("Schwelle_Ein") && row["Schwelle_Ein"] != DBNull.Value)
                    item.Schwelle_Ein = Convert.ToDouble(row["Schwelle_Ein"]);

                if (dt.Columns.Contains("Schwelle_Aus") && row["Schwelle_Aus"] != DBNull.Value)
                    item.Schwelle_Aus = Convert.ToDouble(row["Schwelle_Aus"]);

                // Das Element der dynamischen Liste hinzufügen
                _internalList.Add(item);
            }
        }
    }
}
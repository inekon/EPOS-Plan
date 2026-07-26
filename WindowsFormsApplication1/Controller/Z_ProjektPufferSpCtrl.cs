using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
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
                    System.Windows.Forms.MessageBox.Show(
                        "Der Pufferspeicher '" + PufferSp + "' wurde weder im Projekt noch in den Stammdaten gefunden!\n" +
                        "Die Zuordnung kann nicht gespeichert werden.",
                        "Pufferspeicher Zuordnung",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Warning);
                    return false;
                }
                ID_Pufferspeicher = idPuffer;

                // Umstellung von unparametrisiertem SELECT-String auf standardkonformes VALUES-Statement mit Parametern
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

                // Das Element der dynamischen Liste hinzufügen
                _internalList.Add(item);
            }
        }
    }
}
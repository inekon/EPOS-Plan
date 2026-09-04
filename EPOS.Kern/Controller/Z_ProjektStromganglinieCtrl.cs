using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    class Z_ProjektStromganglinieCtrl : Z_ProjektStromganglinieModel
    {
        private List<Z_ProjektStromganglinieModel> _internalList = new List<Z_ProjektStromganglinieModel>();
        public int rows => _internalList.Count;
        public new List<Z_ProjektStromganglinieModel> items => _internalList;
        public Z_ProjektStromganglinieModel model;

        public Z_ProjektStromganglinieCtrl()
        {
            model = new Z_ProjektStromganglinieModel();
        }

        /// <summary>
        /// Die Stromganglinien EINES Projekts samt Bezeichner — der Ersatz fuer den
        /// konkatenierten INNER JOIN, der bis iU9-W12.0g DREIMAL im Bestand stand
        /// (<c>Form_Start.cs:455-462</c>, <c>StromganglinieKontextMenuCtrl.cs:128-135</c>
        /// und, tot, <c>Form_Stromganglinie.cs:44</c> — Befund W12-B4).
        /// </summary>
        /// <param name="idProjekt">Projektschluessel.</param>
        /// <returns>Die Zuordnungen in der Reihenfolge der Zuordnungstabelle; nie <c>null</c>.</returns>
        /// <remarks>
        /// <b>Ohne ORDER BY, mit Absicht.</b> Der Vorlaeufer hatte keines, und die
        /// Liste wird beim Schliessen des Dialogs 1:1 zurueckgeschrieben
        /// (<c>WizardCtrl.Del_Stromganglinie</c> + <c>Add_Stromganglinie</c>). Eine
        /// Sortierung hier waere eine stille Umsortierung der Ablage.
        /// </remarks>
        public static List<Z_ProjektStromganglinieModel> LiesProjekt(int idProjekt)
        {
            List<Z_ProjektStromganglinieModel> liste = new List<Z_ProjektStromganglinieModel>();

            DataTable dt = DataRepository.GetDataTable(
                "SELECT z.ID AS ID_Z, z.ID_Projekt, z.ID_Ganglinie, g.Bezeichner " +
                "FROM Z_ProjektStromganglinie AS z " +
                "INNER JOIN Tab_Stromganglinie AS g ON z.ID_Ganglinie = g.ID " +
                "WHERE z.ID_Projekt = ?",
                new DbParam("@projekt", DbParamTyp.Integer) { Wert = idProjekt });
            if (dt == null) return liste;

            foreach (DataRow row in dt.Rows)
            {
                Z_ProjektStromganglinieModel item = new Z_ProjektStromganglinieModel();
                if (row["ID_Z"] != DBNull.Value) item.m_ID_Z = Convert.ToInt32(row["ID_Z"]);
                item.m_ID_Projekt = idProjekt;
                if (row["ID_Ganglinie"] != DBNull.Value)
                    item.m_ID_Stromganglinie = Convert.ToInt32(row["ID_Ganglinie"]);
                if (row["Bezeichner"] != DBNull.Value)
                    item.m_szStromganglinie = row["Bezeichner"].ToString();
                liste.Add(item);
            }
            return liste;
        }

        public void ReadAll(string sql)
        {
            // Abfrage über das zentrale DataRepository laden
            DataTable dt = DataRepository.GetDataTable(sql, null);

            // Interne Liste vor dem erneuten Befüllen leeren
            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                Z_ProjektStromganglinieModel item = new Z_ProjektStromganglinieModel();

                // Sicheres Auslesen über Spaltennamen statt numerischer Indizes
                if (dt.Columns.Contains("ID_Z") && row["ID_Z"] != DBNull.Value)
                    item.m_ID_Z = Convert.ToInt32(row["ID_Z"]);

                if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value)
                    item.m_ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);

                if (dt.Columns.Contains("ID_Ganglinie") && row["ID_Ganglinie"] != DBNull.Value)
                    item.m_ID_Stromganglinie = Convert.ToInt32(row["ID_Ganglinie"]);

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    item.m_szStromganglinie = row["Bezeichner"].ToString();

                // Das Element der dynamischen Liste hinzufügen
                _internalList.Add(item);
            }
        }
    }
}
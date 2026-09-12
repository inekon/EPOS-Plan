using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    class Z_ProjektSolarganglinieCtrl : Z_ProjektSolarganglinieModel
    {
        private List<Z_ProjektSolarganglinieModel> _internalList = new List<Z_ProjektSolarganglinieModel>();
        public int rows => _internalList.Count;
        public new List<Z_ProjektSolarganglinieModel> items => _internalList;

        public Z_ProjektSolarganglinieCtrl()
        {
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
                Z_ProjektSolarganglinieModel item = new Z_ProjektSolarganglinieModel();

                // Sicheres Auslesen über Spaltennamen statt fehleranfälliger numerischer Indizes
                if (dt.Columns.Contains("ID_Z") && row["ID_Z"] != DBNull.Value)
                    item.m_ID_Z = Convert.ToInt32(row["ID_Z"]);

                if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value)
                    item.m_ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);

                if (dt.Columns.Contains("ID_Solarganglinie") && row["ID_Solarganglinie"] != DBNull.Value)
                    item.m_ID_Solarganglinie = Convert.ToInt32(row["ID_Solarganglinie"]);

                if (dt.Columns.Contains("Solarganglinie") && row["Solarganglinie"] != DBNull.Value)
                    item.m_szSolarganglinie = row["Solarganglinie"].ToString();

                // Das Element der dynamischen Liste hinzufügen
                _internalList.Add(item);
            }
        }

        /// <summary>
        /// Die Solarthermie-Ganglinien EINES Projekts samt ihrem Namen (iU9-W7.0e) —
        /// der Verbund, der bis hierher in <c>Form_Start.pBox_Solarthermie_Click</c>
        /// stand (Z. 1416-1431) und dort mit einem <c>RecordSet</c> Zeile für Zeile
        /// gelesen wurde.
        ///
        /// <para><b>Warum der Verbund noetig ist.</b> <c>Z_ProjektSolarganglinie</c>
        /// traegt nur die Id der Ganglinie; den ANZEIGENAMEN hat allein
        /// <c>Tab_Solarganglinie</c>. Der Dialog zeigt Namen — ohne diesen Verbund
        /// muesste er je Zeile einzeln nachfragen.</para>
        ///
        /// <para><b>Die Spaltennamen weichen ab</b>, und das ist Bestand:
        /// <c>Z_ProjektSolarganglinie</c> heisst die Zuordnungs-Id <c>ID</c> und die
        /// Ganglinien-Id <c>ID_Ganglinie</c>, waehrend das Modell sie <c>m_ID_Z</c> und
        /// <c>m_ID_Solarganglinie</c> nennt. <see cref="ReadAll"/> liest deshalb andere
        /// Spalten als diese Abfrage — die eine bekommt <c>SELECT *</c> der Tabelle, die
        /// andere die Aliasnamen des Verbunds.</para>
        /// </summary>
        public static List<Z_ProjektSolarganglinieModel> LiesProjekt(int projektId)
        {
            var liste = new List<Z_ProjektSolarganglinieModel>();

            DataTable dt = DataRepository.GetDataTable(
                "SELECT Z_ProjektSolarganglinie.ID, Z_ProjektSolarganglinie.ID_Projekt, " +
                "Z_ProjektSolarganglinie.ID_Ganglinie, Tab_Solarganglinie.Bezeichner " +
                "FROM Z_ProjektSolarganglinie INNER JOIN Tab_Solarganglinie ON " +
                "Z_ProjektSolarganglinie.ID_Ganglinie = Tab_Solarganglinie.ID " +
                "WHERE Z_ProjektSolarganglinie.ID_Projekt = ?",
                new DbParam("@proj", projektId));

            if (dt == null) return liste;

            foreach (DataRow row in dt.Rows)
            {
                liste.Add(new Z_ProjektSolarganglinieModel
                {
                    m_ID_Z = row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0,
                    m_ID_Projekt = projektId,
                    m_ID_Solarganglinie = row["ID_Ganglinie"] != DBNull.Value
                        ? Convert.ToInt32(row["ID_Ganglinie"]) : 0,
                    m_szSolarganglinie = row["Bezeichner"] != DBNull.Value
                        ? row["Bezeichner"].ToString() : ""
                });
            }
            return liste;
        }
    }
}
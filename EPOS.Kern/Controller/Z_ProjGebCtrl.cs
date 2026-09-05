using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    class Z_ProjGebCtrl : Z_ProjGebModel
    {
        // Das neue dynamische Listen-Schema
        private List<Z_ProjGebModel> _internalList = new List<Z_ProjGebModel>();

        public int rows => _internalList.Count;
        public new List<Z_ProjGebModel> items => _internalList;

        private Z_ProjGebModel model;

        public Z_ProjGebCtrl()
        {
            model = new Z_ProjGebModel();
        }

        /// <summary>
        /// Die GEBAEUDEZUORDNUNGEN eines Projekts (iU9-W9.0d) — der JOIN, der bis Welle 9
        /// DREIMAL wortgleich im Oberflaechencode stand: <c>Form_Start.pBox_Gebaude_Click</c>
        /// (:312-338), <c>GebaeudeKontextMenuCtrl.ContextMenuItemBearbeiten_Click</c>
        /// (:87-117) und <c>WizardParent.LoadZGeb</c>.
        ///
        /// <para><b>Zwei Fassungen des JOIN, eine hier.</b> Das Startbild las
        /// <c>ID_Gebaeude</c> aus der Spalte <c>Z_ProjektGebaeude.ID</c> (also aus derselben
        /// Zahl wie <c>ID_Z</c>), das Kontextmenue aus <c>Tab_Gebaeude.ID_ProjektGebaeude</c>.
        /// Beide zeigen auf dieselbe Zeile — <c>ID_ProjektGebaeude</c> IST der Verweis auf
        /// <c>Z_ProjektGebaeude.ID</c> —, das Kontextmenue nennt sie nur beim Namen.
        /// Uebernommen ist die Fassung des Kontextmenues.</para>
        /// </summary>
        public static List<Z_ProjGebModel> LiesProjekt(int idProjekt)
        {
            var liste = new List<Z_ProjGebModel>();

            const string sql =
                "SELECT Z_ProjektGebaeude.ID, Z_ProjektGebaeude.[ID_Projekt], " +
                "[Tab_Gebaeude].ID_ProjektGebaeude, [Tab_Gebaeude].Gebaeudename, " +
                "[Tab_Gebaeude].Baualtersklasse, Z_ProjektGebaeude.Wohnflaeche_Waermebedarf, " +
                "Einheit_Waermebedarf_Wohnflaeche, Jahresnutzungsgrad, dezWarmwasserbereitung, " +
                "Gebaeudeart, Beschreibung FROM [Tab_Gebaeude] " +
                "INNER JOIN Z_ProjektGebaeude ON [Tab_Gebaeude].ID_ProjektGebaeude = Z_ProjektGebaeude.ID " +
                "WHERE Z_ProjektGebaeude.ID_Projekt = ?";

            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("@id", idProjekt));
            if (dt == null) return liste;

            foreach (DataRow row in dt.Rows)
            {
                var item = new Z_ProjGebModel();
                item.ID_Z = Convert.ToInt32(row["ID"]);
                item.ID_Projekt = idProjekt;
                item.ID_Gebaeude = Convert.ToInt32(row["ID_ProjektGebaeude"]);
                item.Gebaeudename = Text(row, "Gebaeudename");
                item.Wohnflaeche = Zahl(row, "Wohnflaeche_Waermebedarf");
                item.Einheit = Text(row, "Einheit_Waermebedarf_Wohnflaeche");
                item.Jahresnutzungsgrad = Zahl(row, "Jahresnutzungsgrad");
                item.DezentralWarmwasser = row["dezWarmwasserbereitung"] != DBNull.Value &&
                                           Convert.ToBoolean(row["dezWarmwasserbereitung"]);
                item.Gebaeudeart = Text(row, "Gebaeudeart");
                item.Beschreibung = Text(row, "Beschreibung");
                item.Baualtersklasse = Text(row, "Baualtersklasse");
                liste.Add(item);
            }
            return liste;
        }

        private static string Text(DataRow row, string spalte)
            => row[spalte] == DBNull.Value ? "" : row[spalte].ToString();

        private static double Zahl(DataRow row, string spalte)
            => row[spalte] == DBNull.Value ? 0.0 : Convert.ToDouble(row[spalte]);

        public void ReadAll(string sql)
        {
            // Abfrage über das zentrale DataRepository holen
            DataTable dt = DataRepository.GetDataTable(sql, null);

            // Liste vor dem Befüllen leeren
            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                Z_ProjGebModel item = new Z_ProjGebModel();

                // Sicheres Mapping über die Spaltennamen statt numerischer Indizes
                if (dt.Columns.Contains("ID_Z") && row["ID_Z"] != DBNull.Value)
                    item.ID_Z = Convert.ToInt32(row["ID_Z"]);

                if (dt.Columns.Contains("ID_Gebaeude") && row["ID_Gebaeude"] != DBNull.Value)
                    item.ID_Gebaeude = Convert.ToInt32(row["ID_Gebaeude"]);

                if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value)
                    item.ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);

                if (dt.Columns.Contains("Wohnflaeche") && row["Wohnflaeche"] != DBNull.Value)
                    item.Wohnflaeche = Convert.ToDouble(row["Wohnflaeche"]);

                if (dt.Columns.Contains("Einheit") && row["Einheit"] != DBNull.Value)
                    item.Einheit = row["Einheit"].ToString();

                if (dt.Columns.Contains("Jahresnutzungsgrad") && row["Jahresnutzungsgrad"] != DBNull.Value)
                    item.Jahresnutzungsgrad = Convert.ToDouble(row["Jahresnutzungsgrad"]);

                if (dt.Columns.Contains("DezentralWarmwasser") && row["DezentralWarmwasser"] != DBNull.Value)
                    item.DezentralWarmwasser = Convert.ToBoolean(row["DezentralWarmwasser"]);

                // Element dynamisch zur internen Liste hinzufügen
                _internalList.Add(item);
            }
        }
    }
}

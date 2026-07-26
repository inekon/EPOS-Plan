using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    class TagVCtrl : TagVModel
    {
        // Das besprochene dynamische Listen-Schema
        private List<TagVModel> _internalList = new List<TagVModel>();
        public int rows => _internalList.Count;
        public new List<TagVModel> items => _internalList;

        public TagVCtrl()
        {
        }

        public void ReadAll(string sql)
        {
            // Daten abrufen über das zentrale DataRepository
            DataTable dt = DataRepository.GetDataTable(sql, null);

            // Interne Liste vor dem erneuten Laden leeren
            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                // Korrektur: Hier wird nun korrekterweise das Model (statt des Controllers) erzeugt
                TagVModel item = new TagVModel();

                // Spaltenbasiertes, sicheres Auslesen über Spaltennamen statt numerischer Indizes
                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                    item.ID = Convert.ToInt32(row["ID"]);

                if (dt.Columns.Contains("Name") && row["Name"] != DBNull.Value)
                    item.Name = row["Name"].ToString();

                if (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value)
                    item.Beschreibung = row["Beschreibung"].ToString();

                if (dt.Columns.Contains("Veraenderbar") && row["Veraenderbar"] != DBNull.Value)
                    item.Veraenderbar = Convert.ToBoolean(row["Veraenderbar"]);

                // Fallback, falls die Spalte in der Access-Tabelle "Veränderbar" (mit Umlaut) geschrieben ist
                else if (dt.Columns.Contains("Veränderbar") && row["Veränderbar"] != DBNull.Value)
                    item.Veraenderbar = Convert.ToBoolean(row["Veränderbar"]);

                // STAMM-Katalog: Namensfeld heißt "Bezeichner" (wird im Model als Name geführt)
                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    item.Name = row["Bezeichner"].ToString();

                // Neues STAMM-Schutzfeld
                if (dt.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value)
                    item.ReadOnly = Convert.ToBoolean(row["ReadOnly"]);

                // Das fertige Element der dynamischen Liste hinzufügen
                _internalList.Add(item);
            }
        }
    }
}

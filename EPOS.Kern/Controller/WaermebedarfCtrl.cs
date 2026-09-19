using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    class WaermebedarfCtrl : WaermebedarfModel
    {
        private List<WaermebedarfModel> _internalList = new List<WaermebedarfModel>();
        public int rows => _internalList.Count;
        public new List<WaermebedarfModel> items => _internalList;

        public WaermebedarfCtrl()
        {
        }

        public bool Delete(string szName)
        {
            try
            {
                // Parametrisierte Abfrage ohne unsaubere Stringverkettungen
                string sql = "DELETE FROM Tab_Waermebedarf WHERE Bezeichner = ?";
                DbParam[] ps = {
                    new DbParam("@bez", szName ?? (object)DBNull.Value)
                };

                return DataRepository.ExecuteSQL(sql, ps);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Delete: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Legt einen Waermebedarfs-KOPF im PROJEKT an (GL-1).
        ///
        /// <para><b>Das Projekt ist Pflicht.</b> <c>Tab_Waermebedarf</c> ist eine
        /// Projekttabelle; ihre Spalte <c>ID_Projekt</c> traegt seit Schemaschritt 96
        /// einen Fremdschluessel auf <c>Tab_Projekt</c>. Ein Kopfsatz ohne Projekt ist
        /// ueber <c>ID_Projekt</c> nicht auffindbar und wird vom Loeschweg des Projekts
        /// nicht mitgenommen.</para>
        ///
        /// <para><b>Katalogware gehoert nicht hierher.</b> Ein externer Waermebedarf
        /// OHNE Projekt ist Katalogware und wird ueber
        /// <see cref="WaermebedarfStammCtrl"/> nach <c>Tab_Waermebedarf_STAMM</c>
        /// geschrieben — der Weg, den die Importkette seit iU9-W9-E-3 nimmt
        /// (<c>GanglinienZiel.Waermebedarf</c>); ins Projekt kommt er als Kopie. Ein
        /// Aufruf ohne gueltige Projektnummer wird deshalb BENANNT abgewiesen statt
        /// still mit NULL gespeichert.</para>
        /// </summary>
        /// <param name="idProjekt">Das Projekt, dem der Kopfsatz gehoert; &gt; 0.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="idProjekt"/> ist 0 oder kleiner.
        /// </exception>
        public bool Insert(int idProjekt)
        {
            if (idProjekt <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(idProjekt),
                    "Ein Waermebedarf wird nur MIT Projekt in Tab_Waermebedarf " +
                    "geschrieben. Katalogware gehoert nach Tab_Waermebedarf_STAMM " +
                    "(WaermebedarfStammCtrl).");

            try
            {
                // Ermittlung der nächsten ID direkt über das Repository
                string sqlCount = "SELECT COUNT(*) FROM Tab_Waermebedarf";
                object countResult = DataRepository.ExecuteScalar(sqlCount, null);
                int count = countResult != null ? Convert.ToInt32(countResult) : 0;

                if (count == 0)
                {
                    m_ID_Ganglinie = 1;
                }
                else
                {
                    string sqlMax = "SELECT MAX(ID) FROM Tab_Waermebedarf";
                    object maxResult = DataRepository.ExecuteScalar(sqlMax, null);
                    m_ID_Ganglinie = (maxResult != null ? Convert.ToInt32(maxResult) : 0) + 1;
                }

                // Standardkonformes INSERT INTO ... VALUES-Statement
                //
                // ID_Projekt kommt als PARAMETER herein (GL-1). Ein unbekanntes Projekt
                // weist die Datenbank ueber den Fremdschluessel aus Schemaschritt 96 ab;
                // der Fang unten meldet das und gibt false zurueck.
                string sql = "INSERT INTO Tab_Waermebedarf (ID, ID_Projekt, Bezeichner) VALUES (?, ?, ?)";
                DbParam[] ps = {
                    new DbParam("@id", m_ID_Ganglinie),
                    new DbParam("@proj", idProjekt),
                    new DbParam("@bez", m_szBezeichner ?? (object)DBNull.Value)
                };

                return DataRepository.ExecuteSQL(sql, ps);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Insert: " + ex.Message);
                return false;
            }
        }

        public void ReadAll()
        {
            string sql = "SELECT * FROM Tab_Waermebedarf ORDER BY Bezeichner";
            DataTable dt = DataRepository.GetDataTable(sql, null);

            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                WaermebedarfModel item = new WaermebedarfModel();

                // Spaltenbasiertes, sicheres Auslesen über Spaltennamen
                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                    item.ID = Convert.ToInt32(row["ID"]);

                // Die Ganglinien-ID ist im aktuellen Schema der Primärschlüssel der Kopftabelle
                item.m_ID_Ganglinie = item.ID;

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    item.m_szBezeichner = row["Bezeichner"].ToString();

                _internalList.Add(item);
            }
        }

        public void ReadSingle(string szBezeichner)
        {
            string sql = "SELECT * FROM Tab_Waermebedarf WHERE Bezeichner = ?";
            DbParam[] ps = {
                new DbParam("@bez", szBezeichner ?? (object)DBNull.Value)
            };

            DataTable dt = DataRepository.GetDataTable(sql, ps);

            // Löscht Instanzdaten standardmäßig für den Fall, dass nichts gefunden wird
            ID = 0;
            m_ID_Ganglinie = 0;
            m_szBezeichner = string.Empty;

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                    ID = Convert.ToInt32(row["ID"]);

                // Die Ganglinien-ID ist im aktuellen Schema der Primärschlüssel der Kopftabelle
                m_ID_Ganglinie = ID;

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    m_szBezeichner = row["Bezeichner"].ToString();
            }
        }
    }
}
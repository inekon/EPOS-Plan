using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Zuordnung Projekt ↔ Stromverbraucher (<c>Z_Projekt_Stromverbraucher</c>). Gelesen
    /// wird sie allein über <see cref="LiesProjekt"/> — der Name je Zeile ist der der
    /// Projektkopie, auf die die Zeile per ID zeigt (Auftrag SV1 vom 30.09.2026; die frühere
    /// Lesung <c>ReadAll(sql)</c> mit dem Bezeichner der Zuordnungszeile ist entfallen).
    /// </summary>
    class Z_ProjektStromverbraucherCtrl : Z_ProjektStromverbraucherModel
    {
        public Z_ProjektStromverbraucherCtrl()
        {
        }

        /// <summary>
        /// Setzt die Jahressumme der Zuordnungszeilen eines Projekts, deren Projektkopie
        /// <paramref name="szBezeichner"/> heißt.
        ///
        /// <para><b>Über die ID</b> (Auftrag SV1 vom 30.09.2026): Gesucht wird die Kopie DIESES
        /// Projekts mit dem Namen, den der Dialog zeigt (<see cref="LiesProjekt"/>), und
        /// geändert werden die Zeilen, die per <c>ID_Stromverbraucher</c> auf sie zeigen. Der
        /// Bezeichner der Zuordnungszeile selbst ist vielfach noch der Katalogname — über ihn
        /// gesucht, traf die Änderung bei einer umbenannten Kopie („EFH_3_Pers (P1017)") keine
        /// Zeile, genau wie die Lesung im Lauf.</para>
        /// </summary>
        public bool UpdateSumme(double dSumme, string szBezeichner, int IDProjekt)
        {
            try
            {
                // Parametrisierte Query gegen SQL-Injections und Formatierungsprobleme bei Nachkommastellen (Double)
                string sql = "UPDATE Z_Projekt_Stromverbraucher SET Summe = ? " +
                             "WHERE ID_Projekt = ? AND ID_Stromverbraucher IN " +
                             "(SELECT ID FROM Tab_Stromverbraucher WHERE ID_Projekt = ? AND Bezeichner = ?)";

                DbParam[] ps = {
                    new DbParam("@summe", dSumme),
                    new DbParam("@idProj", IDProjekt),
                    new DbParam("@idProjKopie", IDProjekt),
                    new DbParam("@bez", szBezeichner ?? (object)DBNull.Value)
                };

                bool ok = DataRepository.ExecuteSQL(sql, ps);

                // AENDERUNGSDATUM: Die Jahressumme ist Bedarf - ein Lauf von vorher ist
                // danach überholt (Muster WizardCtrl.Add_/Del_).
                if (ok) MerkmalUebernahmeCtrl.MarkiereProjektGeaendert(IDProjekt);
                return ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei UpdateSumme: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Die STROMVERBRAUCHER-ZUORDNUNGEN eines Projekts (iU9-W9.0d) — der JOIN aus
        /// <c>Form_Start.pBox_StdLastProfil_Click</c> (:494-509) und
        /// <c>StrombedarfKontextMenuCtrl.ContextMenuItemBearbeiten_Click</c>.
        /// </summary>
        public static List<Z_ProjektStromverbraucherModel> LiesProjekt(int idProjekt)
        {
            var liste = new List<Z_ProjektStromverbraucherModel>();

            const string sql =
                "SELECT Z_Projekt_Stromverbraucher.ID, Z_Projekt_Stromverbraucher.ID_Projekt, " +
                "Z_Projekt_Stromverbraucher.ID_Stromverbraucher, Z_Projekt_Stromverbraucher.Summe, " +
                "Tab_Stromverbraucher.Bezeichner " +
                "FROM Z_Projekt_Stromverbraucher INNER JOIN Tab_Stromverbraucher ON " +
                "Z_Projekt_Stromverbraucher.ID_Stromverbraucher = Tab_Stromverbraucher.ID " +
                "WHERE Z_Projekt_Stromverbraucher.ID_Projekt = ? " +
                "ORDER BY Z_Projekt_Stromverbraucher.ID";

            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("@id", idProjekt));
            if (dt == null) return liste;

            foreach (DataRow row in dt.Rows)
            {
                var item = new Z_ProjektStromverbraucherModel();
                item.m_ID_Z = Convert.ToInt32(row["ID"]);
                item.m_ID_Projekt = idProjekt;
                item.m_ID_Stromverbraucher = Convert.ToInt32(row["ID_Stromverbraucher"]);
                item.m_szVerbraucher = row["Bezeichner"] == DBNull.Value ? "" : row["Bezeichner"].ToString();
                item.m_Summe = row["Summe"] == DBNull.Value ? 0.0 : Convert.ToDouble(row["Summe"]);
                liste.Add(item);
            }
            return liste;
        }
    }
}
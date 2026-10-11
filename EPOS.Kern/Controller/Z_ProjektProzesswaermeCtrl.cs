using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Zuordnung Projekt ↔ Prozesswärme (<c>Z_Projekt_Prozesswaerme</c>). Gelesen wird sie
    /// allein über <see cref="LiesProjekt"/> — der Name je Zeile ist der der Projektkopie, auf
    /// die die Zeile per <c>ID_Prozesswaerme</c> zeigt (Auftrag SV2, wie SV1 beim
    /// Stromverbraucher; die frühere Lesung <c>ReadAll(sql)</c> mit dem Bezeichner der
    /// Zuordnungszeile ist entfallen).
    /// </summary>
    class Z_ProjektProzesswaermeCtrl : Z_ProjektProzesswaermeModel
    {
        public Z_ProjektProzesswaermeCtrl()
        {
        }

        /// <summary>
        /// Setzt die Jahressumme der Zuordnungszeilen eines Projekts, deren Projektkopie
        /// <paramref name="szBezeichner"/> heißt.
        ///
        /// <para><b>Über die ID</b> (Auftrag SV2): Gesucht wird die Kopie DIESES Projekts mit dem
        /// Namen, den der Dialog zeigt (<see cref="LiesProjekt"/>), und geändert werden die
        /// Zeilen, die per <c>ID_Prozesswaerme</c> auf sie zeigen. Der Bezeichner der
        /// Zuordnungszeile selbst kann ein anderer sein — der Name, unter dem sie einmal angelegt
        /// wurde; über ihn gesucht, traf die Änderung bei einer umbenannten Kopie keine Zeile.</para>
        /// </summary>
        public bool UpdateSumme(double dSumme, string szBezeichner, int IDProjekt)
        {
            try
            {
                // Parametrisierte Query: Verhindert SQL-Injections und regelt Nachkommastellen (Double) automatisch fehlerfrei
                string sql = "UPDATE Z_Projekt_Prozesswaerme SET Summe = ? " +
                             "WHERE ID_Projekt = ? AND ID_Prozesswaerme IN " +
                             "(SELECT ID FROM Tab_Prozesswaerme WHERE ID_Projekt = ? AND Bezeichner = ?)";

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
        /// Die PROZESSWAERME-ZUORDNUNGEN eines Projekts (iU9-W9.0d) — der JOIN aus
        /// <c>Form_Start.pBox_Prozess_Click</c> (:213-229) und
        /// <c>ProzesswaermeKontextMenuCtrl.ContextMenuItemBearbeiten_Click</c>. Der Name je
        /// Zeile ist der der Projektkopie, auf die sie per ID zeigt; die Reihenfolge ist die der
        /// Zuordnungs-ID — dieselbe, in der der Lauf rechnet (SV2).
        /// </summary>
        public static List<Z_ProjektProzesswaermeModel> LiesProjekt(int idProjekt)
        {
            var liste = new List<Z_ProjektProzesswaermeModel>();

            const string sql =
                "SELECT Z_Projekt_Prozesswaerme.ID, Z_Projekt_Prozesswaerme.ID_Projekt, " +
                "Z_Projekt_Prozesswaerme.ID_Prozesswaerme, Tab_Prozesswaerme.Bezeichner, " +
                "Z_Projekt_Prozesswaerme.Summe " +
                "FROM Z_Projekt_Prozesswaerme INNER JOIN Tab_Prozesswaerme ON " +
                "Z_Projekt_Prozesswaerme.ID_Prozesswaerme = Tab_Prozesswaerme.ID " +
                "WHERE Z_Projekt_Prozesswaerme.ID_Projekt = ? " +
                "ORDER BY Z_Projekt_Prozesswaerme.ID";

            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("@id", idProjekt));
            if (dt == null) return liste;
            // PW2/BW2: der Betriebskalender je Zuordnungszeile (spaltentolerant).
            Dictionary<int, int?> kalender = BetriebskalenderCtrl.KalenderDerZuordnungen("Z_Projekt_Prozesswaerme", idProjekt);

            foreach (DataRow row in dt.Rows)
            {
                var item = new Z_ProjektProzesswaermeModel();
                item.ID_Z = Convert.ToInt32(row["ID"]);
                item.ID_Projekt = idProjekt;
                item.ID_Prozesswaerme = Convert.ToInt32(row["ID_Prozesswaerme"]);
                item.szProzessname = row["Bezeichner"] == DBNull.Value ? "" : row["Bezeichner"].ToString();
                item.Summe = row["Summe"] == DBNull.Value ? 0.0 : Convert.ToDouble(row["Summe"]);
                // PW1 Stufe 1: das Temperaturpaar der Projektkopie (spaltentolerant).
                (item.Vorlauf, item.Ruecklauf) = ProzesswaermeStammCtrl.ProjektTemperaturpaar(item.ID_Prozesswaerme);
                item.ID_Betriebskalender = kalender.TryGetValue(item.ID_Z, out int? k) ? k : null;
                liste.Add(item);
            }
            return liste;
        }
    }
}

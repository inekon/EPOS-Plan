using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    class Z_ProjektBrauchwasserCtrl : Z_ProjektBrauchwasserModel
    {
        private List<Z_ProjektBrauchwasserModel> _internalList = new List<Z_ProjektBrauchwasserModel>();
        public int rows => _internalList.Count;
        public new List<Z_ProjektBrauchwasserModel> items => _internalList;

        public Z_ProjektBrauchwasserCtrl()
        {
        }

        public bool UpdateSumme(double dSumme, string szBezeichner, int IDProjekt)
        {
            try
            {
                // Parametrisierte Query: Typkonvertierungen (z. B. Dezimaltrennzeichen bei Double) werden automatisch korrekt gehandhabt
                string sql = @"UPDATE Z_Projekt_Brauchwasser 
                               SET Summe = ? 
                               WHERE Bezeichner = ? 
                                 AND ID_Projekt = ?";

                DbParam[] ps = {
                    new DbParam("@summe", dSumme),
                    new DbParam("@bez", szBezeichner ?? (object)DBNull.Value),
                    new DbParam("@idProj", IDProjekt)
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
        /// Die BRAUCHWASSER-ZUORDNUNGEN eines Projekts (iU9-W9.0d) — der JOIN aus
        /// <c>Form_Start.pBox_Brauchwasser_Click</c> (:1863-1879) und aus
        /// <c>Form_Gebaeude2.btn_Brauchwasser_Click</c> (:224-241), dort wortgleich.
        /// </summary>
        public static List<Z_ProjektBrauchwasserModel> LiesProjekt(int idProjekt)
        {
            var liste = new List<Z_ProjektBrauchwasserModel>();

            const string sql =
                "SELECT Z_Projekt_Brauchwasser.ID, Z_Projekt_Brauchwasser.ID_Projekt, " +
                "Z_Projekt_Brauchwasser.ID_Brauchwasser, Tab_Brauchwasser.Bezeichner, " +
                "Z_Projekt_Brauchwasser.Summe " +
                "FROM Z_Projekt_Brauchwasser INNER JOIN Tab_Brauchwasser ON " +
                "Z_Projekt_Brauchwasser.ID_Brauchwasser = Tab_Brauchwasser.ID " +
                "WHERE Z_Projekt_Brauchwasser.ID_Projekt = ?";

            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("@id", idProjekt));
            if (dt == null) return liste;

            foreach (DataRow row in dt.Rows)
            {
                var item = new Z_ProjektBrauchwasserModel();
                item.ID_Z = Convert.ToInt32(row["ID"]);
                item.ID_Projekt = idProjekt;
                item.ID_Brauchwasser = Convert.ToInt32(row["ID_Brauchwasser"]);
                item.szBezeichner = row["Bezeichner"] == DBNull.Value ? "" : row["Bezeichner"].ToString();
                item.Summe = row["Summe"] == DBNull.Value ? 0.0 : Convert.ToDouble(row["Summe"]);
                liste.Add(item);
            }
            return liste;
        }

        /// <summary>
        /// <b>Gleicht die Liste dem gespeicherten Stand?</b> — die Probe vor dem Neuschreiben der
        /// Brauchwasser-Zuordnungen (Löschen und Neuanlegen, <c>WizardCtrl.Del_/Add_Projekt_Brauchwasser</c>),
        /// damit ein OK ohne Änderung nichts schreibt und das Änderungsdatum des Projekts nicht
        /// setzt. Gleich heißt: dieselbe Zahl Zeilen in derselben Reihenfolge wie
        /// <see cref="LiesProjekt"/>, je Zeile derselbe Bezeichner und dieselbe Summe, und die
        /// gespeicherte Zeile zeigt schon auf die Projektkopie ihres Bezeichners — genau den Stand,
        /// den das Neuanlegen herstellen würde. Im Zweifel ungleich: dann wird geschrieben.
        /// </summary>
        public static bool GleichGespeichert(int idProjekt, IReadOnlyList<Z_ProjektBrauchwasserModel> liste)
        {
            liste ??= Array.Empty<Z_ProjektBrauchwasserModel>();
            List<Z_ProjektBrauchwasserModel> bestand = LiesProjekt(idProjekt);
            if (bestand.Count != liste.Count) return false;
            for (int i = 0; i < bestand.Count; i++)
            {
                Z_ProjektBrauchwasserModel alt = bestand[i], neu = liste[i];
                if (neu == null) return false;
                if (!string.Equals(alt.szBezeichner ?? "", neu.szBezeichner ?? "", StringComparison.Ordinal)) return false;
                if (!alt.Summe.Equals(neu.Summe)) return false;
                if (alt.ID_Brauchwasser != BrauchwasserStammCtrl.GetProjektId(alt.szBezeichner, idProjekt)) return false;
            }
            return true;
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
                Z_ProjektBrauchwasserModel item = new Z_ProjektBrauchwasserModel();

                // Spaltenbasiertes, sicheres Auslesen über Spaltennamen statt numerischer Indizes
                if (dt.Columns.Contains("ID_Z") && row["ID_Z"] != DBNull.Value)
                    item.ID_Z = Convert.ToInt32(row["ID_Z"]);

                if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value)
                    item.ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);

                if (dt.Columns.Contains("ID_Brauchwasser") && row["ID_Brauchwasser"] != DBNull.Value)
                    item.ID_Brauchwasser = Convert.ToInt32(row["ID_Brauchwasser"]);

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    item.szBezeichner = row["Bezeichner"].ToString();

                // Fallback, falls die Spalte in Access exakt wie die Variable heißt
                else if (dt.Columns.Contains("szBezeichner") && row["szBezeichner"] != DBNull.Value)
                    item.szBezeichner = row["szBezeichner"].ToString();

                if (dt.Columns.Contains("Summe") && row["Summe"] != DBNull.Value)
                    item.Summe = Convert.ToDouble(row["Summe"]);

                // Das Element der dynamischen Liste hinzufügen
                _internalList.Add(item);
            }
        }
    }
}
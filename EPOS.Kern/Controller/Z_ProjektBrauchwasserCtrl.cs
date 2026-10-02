using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Zuordnung Projekt ↔ Brauchwasser (<c>Z_Projekt_Brauchwasser</c>). Gelesen wird sie
    /// allein über <see cref="LiesProjekt"/> — der Name je Zeile ist der der Projektkopie, auf
    /// die die Zeile per <c>ID_Brauchwasser</c> zeigt (Auftrag SV2, wie SV1 beim
    /// Stromverbraucher; die frühere Lesung <c>ReadAll(sql)</c> mit dem Bezeichner der
    /// Zuordnungszeile ist entfallen).
    /// </summary>
    class Z_ProjektBrauchwasserCtrl : Z_ProjektBrauchwasserModel
    {
        public Z_ProjektBrauchwasserCtrl()
        {
        }

        /// <summary>
        /// Setzt die Jahressumme der Zuordnungszeilen eines Projekts, deren Projektkopie
        /// <paramref name="szBezeichner"/> heißt.
        ///
        /// <para><b>Über die ID</b> (Auftrag SV2): Gesucht wird die Kopie DIESES Projekts mit dem
        /// Namen, den der Dialog zeigt (<see cref="LiesProjekt"/>), und geändert werden die
        /// Zeilen, die per <c>ID_Brauchwasser</c> auf sie zeigen. Der Bezeichner der
        /// Zuordnungszeile selbst kann ein anderer sein — der Name, unter dem sie einmal angelegt
        /// wurde; über ihn gesucht, traf die Änderung bei einer umbenannten Kopie keine Zeile.</para>
        /// </summary>
        public bool UpdateSumme(double dSumme, string szBezeichner, int IDProjekt)
        {
            try
            {
                // Parametrisierte Query: Typkonvertierungen (z. B. Dezimaltrennzeichen bei Double) werden automatisch korrekt gehandhabt
                string sql = "UPDATE Z_Projekt_Brauchwasser SET Summe = ? " +
                             "WHERE ID_Projekt = ? AND ID_Brauchwasser IN " +
                             "(SELECT ID FROM Tab_Brauchwasser WHERE ID_Projekt = ? AND Bezeichner = ?)";

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
        /// Die BRAUCHWASSER-ZUORDNUNGEN eines Projekts (iU9-W9.0d) — der JOIN aus
        /// <c>Form_Start.pBox_Brauchwasser_Click</c> (:1863-1879) und aus
        /// <c>Form_Gebaeude2.btn_Brauchwasser_Click</c> (:224-241), dort wortgleich. Der Name
        /// je Zeile ist der der Projektkopie, auf die sie per ID zeigt; die Reihenfolge ist die
        /// der Zuordnungs-ID — dieselbe, in der der Lauf rechnet (SV2).
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
                "WHERE Z_Projekt_Brauchwasser.ID_Projekt = ? " +
                "ORDER BY Z_Projekt_Brauchwasser.ID";

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
        /// gespeicherte Zeile zeigt schon auf die Projektkopie, auf die das Neuanlegen sie schriebe
        /// — die zugeordnete Kopie, wenn sie zu diesem Projekt gehört und den Namen trägt
        /// (<see cref="BrauchwasserStammCtrl.GetProjektIdUeberId"/>, SV2), sonst die Kopie gleichen
        /// Namens. Das ist genau der Stand, den das Neuanlegen herstellen würde. Im Zweifel
        /// ungleich: dann wird geschrieben.
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
                int kopie = BrauchwasserStammCtrl.GetProjektIdUeberId(neu.ID_Brauchwasser, neu.szBezeichner, idProjekt);
                if (kopie <= 0) kopie = BrauchwasserStammCtrl.GetProjektId(neu.szBezeichner, idProjekt);
                if (alt.ID_Brauchwasser != kopie) return false;
            }
            return true;
        }
    }
}
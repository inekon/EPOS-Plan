using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Controller der PROJEKTKOPIE <c>Tab_Wechselrichter</c> (Stufe S1 des
    /// <c>Konzept_Wechselrichter_EPOS-Plan.md</c>, Anwenderentscheid <b>W6‑E‑2</b>
    /// vom 06.09.2026) — der Zwilling von <see cref="PhotovoltaikCtrl"/>.
    ///
    /// <para><b>Warum es die Kopie gibt.</b> „Projekte KOPIEREN Katalogsätze, alle
    /// persistierten Verweise zeigen auf die Projektkopie, nie auf die
    /// <c>_STAMM</c>-Tabelle" (<see cref="KatalogRegistry"/>). Ein Projekt, das vor
    /// drei Jahren gerechnet wurde, rechnet damit heute noch mit den Gerätedaten von
    /// damals — auch wenn der Katalog inzwischen gepflegt wurde. Die Strangzuordnung
    /// der Stufe S2 (<c>Z_AnlageStrang.ID_Wechselrichter</c>) wird deshalb auf
    /// <c>Tab_Wechselrichter.ID</c> zeigen, nicht auf den Katalog.</para>
    ///
    /// <para><b>Keine Rechenwirkung in S1.</b> Kein Rechenweg liest diese Tabelle; sie
    /// steht bereit. Der Referenzlauf bleibt byte-gleich.</para>
    /// </summary>
    public class WechselrichterCtrl
    {
        /// <summary>Die Projekttabelle.</summary>
        public const string TABLE = SchemaKatalog.TAB_WECHSELRICHTER;

        private readonly List<WechselrichterModel> _liste = new List<WechselrichterModel>();

        /// <summary>Zahl der gelesenen Sätze.</summary>
        public int rows => _liste.Count;

        /// <summary>Die gelesenen Sätze.</summary>
        public List<WechselrichterModel> items => _liste;

        // =================================================================
        //  Lesen
        // =================================================================

        /// <summary>Alle Projektkopien eines Projekts, nach Bezeichner sortiert.</summary>
        public void ReadAll(int idProjekt)
        {
            _liste.Clear();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE + "] WHERE ID_Projekt = ? ORDER BY Bezeichner",
                new DbParam("@idProj", idProjekt));
            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
                _liste.Add(WechselrichterStammCtrl.AusZeile(row));
        }

        /// <summary>Eine Projektkopie über ihren Primärschlüssel; <c>null</c>, wenn es sie nicht gibt.</summary>
        public WechselrichterModel ReadSingle(int id)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE + "] WHERE ID = ?", new DbParam("@id", id));
            return (dt == null || dt.Rows.Count == 0) ? null : WechselrichterStammCtrl.AusZeile(dt.Rows[0]);
        }

        /// <summary>
        /// Die Id der Projektkopie zu einem Bezeichner, oder 0 — der Vorabtest von
        /// <see cref="CopyFromStamm(int,int)"/>.
        /// </summary>
        public int GetProjektId(string szBezeichner, int idProjekt)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM [" + TABLE + "] WHERE Bezeichner = ? AND ID_Projekt = ? ORDER BY ID",
                new DbParam("@bez", szBezeichner ?? ""),
                new DbParam("@idProj", idProjekt));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        /// <summary>Führt das Projekt bereits eine Kopie dieses Bezeichners?</summary>
        public bool ExistsInProjekt(string szBezeichner, int idProjekt)
        {
            return GetProjektId(szBezeichner, idProjekt) > 0;
        }

        // =================================================================
        //  Katalog → Projekt
        // =================================================================

        /// <summary>
        /// Kopiert einen Katalogsatz (<c>Tab_Wechselrichter_STAMM</c>) in die
        /// Projekttabelle, sofern das Projekt ihn noch nicht führt.
        /// </summary>
        /// <returns>
        /// Die Id der kopierten ODER vorhandenen Projektzeile, <c>-1</c> bei Fehler —
        /// wortgleich zu <see cref="PhotovoltaikCtrl.CopyFromStamm(int,int)"/>. Genau
        /// dieser Wert wird in Stufe S2 an <c>Z_AnlageStrang.ID_Wechselrichter</c>
        /// stehen.
        /// </returns>
        /// <remarks>
        /// <b><c>ReadOnly</c> wird NICHT übernommen</b> — die Spalte gibt es in der
        /// Projekttabelle nicht; sie sagt „gehört zur Auslieferung" und ist eine
        /// Aussage über den KATALOG. Alles andere geht Spalte für Spalte mit: Eine
        /// Spalte nur auf einer Seite wäre hier sofort ein Datenverlust (Konzept 3,
        /// Hausregeln), und genau deshalb kommt die Spaltenliste aus
        /// <see cref="WechselrichterSchema.Fachspalten"/> und nicht aus einer
        /// abgetippten Aufzählung.
        /// </remarks>
        public int CopyFromStamm(int stammId, int idProjekt)
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT * FROM [" + WechselrichterStammCtrl.TABLE + "] WHERE ID = ?",
                    new DbParam("@id", stammId));

                if (dt == null || dt.Rows.Count == 0)
                {
                    // Gemeinsame Entscheidungsstelle wie bei den fuenf baugleichen
                    // Geschwistern: Dialog in der Bedienung, Protokolleintrag im Lauf.
                    DataRepository.FehlerMelden(
                        string.Format(MyResource.Resource.WRK_MSG_STAMM_FEHLT, stammId));
                    return -1;
                }

                DataRow s = dt.Rows[0];
                string bezeichner = s["Bezeichner"] == DBNull.Value ? "" : s["Bezeichner"].ToString();

                int vorhandeneId = GetProjektId(bezeichner, idProjekt);
                if (vorhandeneId > 0) return vorhandeneId;

                int neueId = DataRepository.GetMaxID(TABLE) + 1;

                string sql = "INSERT INTO [" + TABLE + "] " +
                             "(ID, ID_Projekt, Bezeichner, Firma, Beschreibung, " +
                             string.Join(", ", WechselrichterSchema.Fachspalten) + ") VALUES (?, ?, ?, ?, ?, " +
                             Fragezeichen(WechselrichterSchema.Fachspalten.Length) + ")";

                var ps = new List<DbParam>
                {
                    new DbParam("@id", neueId),
                    new DbParam("@idProj", idProjekt),
                    new DbParam("@bez", bezeichner),
                    Spaltenwert(s, "Firma"),
                    Spaltenwert(s, "Beschreibung")
                };
                foreach (string spalte in WechselrichterSchema.Fachspalten) ps.Add(Spaltenwert(s, spalte));

                return DataRepository.ExecuteSQL(sql, ps.ToArray()) ? neueId : -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Kopieren des Wechselrichters aus den Stammdaten: " + ex.Message);
                return -1;
            }
        }

        /// <summary>Dieselbe Kopie über den Bezeichner des Katalogsatzes.</summary>
        public int CopyFromStamm(string szBezeichner, int idProjekt)
        {
            int stammId = DataRepository.GetIdByName(
                WechselrichterStammCtrl.TABLE, "Bezeichner", szBezeichner);
            if (stammId <= 0) return -1;
            return CopyFromStamm(stammId, idProjekt);
        }

        /// <summary>Entfernt die Projektkopie eines Bezeichners aus einem Projekt.</summary>
        public bool DeleteFromProjekt(string szBezeichner, int idProjekt)
        {
            return DataRepository.ExecuteSQL(
                "DELETE FROM [" + TABLE + "] WHERE Bezeichner = ? AND ID_Projekt = ?",
                new DbParam("@bez", szBezeichner ?? ""),
                new DbParam("@idProj", idProjekt));
        }

        // =================================================================
        //  Hilfsmittel
        // =================================================================

        /// <summary>
        /// Der Wert einer Spalte des Katalogsatzes als Parameter; fehlende Spalte und
        /// NULL werden beide zu <c>DBNull</c>.
        /// </summary>
        private static DbParam Spaltenwert(DataRow row, string spalte)
        {
            object wert = row.Table.Columns.Contains(spalte) ? row[spalte] : DBNull.Value;
            return new DbParam("@" + spalte, wert ?? DBNull.Value);
        }

        private static string Fragezeichen(int anzahl)
        {
            var teile = new List<string>(anzahl);
            for (int i = 0; i < anzahl; i++) teile.Add("?");
            return string.Join(", ", teile);
        }
    }
}

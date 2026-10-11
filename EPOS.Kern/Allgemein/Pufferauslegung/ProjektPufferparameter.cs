using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Vorgaben der Pufferauslegung je Projekt</b> (<c>Tab_PufferAuslegungParameter</c>,
    /// Schemaschritt <see cref="ProjektkopienKatalogeSchema"/>, Anwenderentscheid 03.10.2026): die
    /// Projektkopie der Vorgabetabelle <c>Tab_PufferAuslegungParameter_STAMM</c>. Sie entsteht, sobald
    /// ein Projekt eine Auslegung speichert, und vor jedem Katalogabgleich für jedes Projekt mit
    /// Auslegung — WERTGLEICH zum Stamm. Gelesen wird sie über
    /// <see cref="PufferAuslegungParameter.Lesen(int)"/>: eingebaute Vorgaben, darüber der Stamm, darüber
    /// die Kopie des Projekts. Der Abgleich fasst nur den Stamm an.
    /// </summary>
    public static class ProjektPufferparameter
    {
        /// <summary>Die Projektkopie.</summary>
        public const string TAB = ProjektkopienKatalogeSchema.TAB_PUFFERPARAMETER;

        private const string FASSUNG =
            "(SELECT MAX(\"" + Katalogfassung.SPALTE_FASSUNG + "\") FROM \"" + Katalogfassung.TAB_APPLIKATION + "\")";

        private const string EINFUEGEN =
            "INSERT INTO \"" + TAB + "\" (\"ID_Projekt\", \"Schluessel\", \"Wert\", \"Einheit\", \"Quelle\", \"Herkunftsart\", \"" +
            ProjektkopienKatalogeSchema.SPALTE_HERKUNFT + "\") ";

        private const string SQL_SICHERN_ALLE =
            EINFUEGEN +
            "SELECT p.\"ID_Projekt\", s.\"Schluessel\", s.\"Wert\", s.\"Einheit\", s.\"Quelle\", s.\"Herkunftsart\", " + FASSUNG + " " +
            "FROM (SELECT DISTINCT \"ID_Projekt\" FROM \"" + PufferAuslegungSchema.TAB + "\") AS p " +
            "CROSS JOIN \"" + PufferAuslegungSchema.TAB_PARAMETER + "\" AS s " +
            "WHERE NOT EXISTS (SELECT 1 FROM \"" + TAB + "\" AS k WHERE k.\"ID_Projekt\" = p.\"ID_Projekt\" AND k.\"Schluessel\" = s.\"Schluessel\") " +
            "ORDER BY p.\"ID_Projekt\", s.\"ID\"";

        private const string SQL_SICHERN_PROJEKT =
            EINFUEGEN +
            "SELECT ?, s.\"Schluessel\", s.\"Wert\", s.\"Einheit\", s.\"Quelle\", s.\"Herkunftsart\", " + FASSUNG + " " +
            "FROM \"" + PufferAuslegungSchema.TAB_PARAMETER + "\" AS s " +
            "WHERE NOT EXISTS (SELECT 1 FROM \"" + TAB + "\" AS k WHERE k.\"ID_Projekt\" = ? AND k.\"Schluessel\" = s.\"Schluessel\") " +
            "ORDER BY s.\"ID\"";

        private const string SQL_OFFEN =
            "SELECT COUNT(*) FROM (SELECT DISTINCT \"ID_Projekt\" FROM \"" + PufferAuslegungSchema.TAB + "\") AS p " +
            "CROSS JOIN \"" + PufferAuslegungSchema.TAB_PARAMETER + "\" AS s " +
            "WHERE NOT EXISTS (SELECT 1 FROM \"" + TAB + "\" AS k WHERE k.\"ID_Projekt\" = p.\"ID_Projekt\" AND k.\"Schluessel\" = s.\"Schluessel\")";

        /// <summary>Die Leseanweisung der Kopie eines Projekts.</summary>
        public const string SQL_LESEN =
            "SELECT \"Schluessel\", \"Wert\", \"Quelle\" FROM \"" + TAB + "\" WHERE \"ID_Projekt\" = ? AND \"Schluessel\" LIKE ?";

        private const string SQL_ZURUECKSETZEN =
            "UPDATE \"" + TAB + "\" SET " +
            "\"Wert\" = (SELECT s.\"Wert\" FROM \"" + PufferAuslegungSchema.TAB_PARAMETER + "\" AS s WHERE s.\"Schluessel\" = \"" + TAB + "\".\"Schluessel\"), " +
            "\"Einheit\" = (SELECT s.\"Einheit\" FROM \"" + PufferAuslegungSchema.TAB_PARAMETER + "\" AS s WHERE s.\"Schluessel\" = \"" + TAB + "\".\"Schluessel\"), " +
            "\"Quelle\" = (SELECT s.\"Quelle\" FROM \"" + PufferAuslegungSchema.TAB_PARAMETER + "\" AS s WHERE s.\"Schluessel\" = \"" + TAB + "\".\"Schluessel\"), " +
            "\"Herkunftsart\" = (SELECT s.\"Herkunftsart\" FROM \"" + PufferAuslegungSchema.TAB_PARAMETER + "\" AS s WHERE s.\"Schluessel\" = \"" + TAB + "\".\"Schluessel\"), " +
            "\"" + ProjektkopienKatalogeSchema.SPALTE_HERKUNFT + "\" = " + FASSUNG + " " +
            "WHERE \"ID_Projekt\" = ? " +
            "AND EXISTS (SELECT 1 FROM \"" + PufferAuslegungSchema.TAB_PARAMETER + "\" AS s WHERE s.\"Schluessel\" = \"" + TAB + "\".\"Schluessel\")";

        /// <summary>Gibt es die Kopietabelle?</summary>
        public static bool Vorhanden() => DataRepository.TabelleVorhanden(TAB);

        /// <summary>
        /// Legt im Vorgang <paramref name="v"/> die fehlenden Kopien an — für jedes Projekt mit
        /// Auslegung (<paramref name="idProjekt"/> <c>null</c>) bzw. für das eine Projekt. Eine stehende
        /// Kopie bleibt. Liefert die Zahl der angelegten Zeilen.
        /// </summary>
        public static int Sichern(DbVorgang v, int? idProjekt)
        {
            if (v == null) throw new ArgumentNullException(nameof(v));
            return idProjekt.HasValue
                ? v.Ausfuehren(SQL_SICHERN_PROJEKT, new DbParam("@p", idProjekt.Value), new DbParam("@p2", idProjekt.Value))
                : v.Ausfuehren(SQL_SICHERN_ALLE);
        }

        /// <summary>Legt die fehlenden Kopien eines Projekts an (außerhalb eines Vorgangs).</summary>
        public static int Sichern(int idProjekt)
        {
            if (idProjekt <= 0 || !Vorhanden() || !DataRepository.TabelleVorhanden(PufferAuslegungSchema.TAB_PARAMETER)) return 0;
            return Math.Max(0, DataRepository.ExecuteNonQuery(SQL_SICHERN_PROJEKT, new DbParam("@p", idProjekt),
                                                              new DbParam("@p2", idProjekt)));
        }

        /// <summary>Wie viele Kopien fehlen den Projekten mit Auslegung? −1 ohne Kopietabelle.</summary>
        public static int OffeneKopien()
        {
            if (!Vorhanden()) return -1;
            object o = DataRepository.ExecuteScalar(SQL_OFFEN);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Führt das Projekt eine Kopie?</summary>
        public static bool HatKopie(int idProjekt)
        {
            if (idProjekt <= 0 || !Vorhanden()) return false;
            object o = DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + TAB + "\" WHERE \"ID_Projekt\" = ?",
                                                    new DbParam("@p", idProjekt));
            return o != null && o != DBNull.Value && Convert.ToInt32(o, CultureInfo.InvariantCulture) > 0;
        }

        /// <summary>
        /// <b>„Auf Katalog zurücksetzen"</b>: die heutigen Werte des Stamms in jede Zeile der Kopie, die
        /// es im Stamm gibt; fehlende Zeilen kommen hinzu. Liefert die Zahl der geschriebenen Zeilen.
        /// </summary>
        public static int Zuruecksetzen(int idProjekt)
        {
            if (idProjekt <= 0 || !Vorhanden()) return 0;
            int n = Math.Max(0, DataRepository.ExecuteNonQuery(SQL_ZURUECKSETZEN, new DbParam("@p", idProjekt)));
            return n + Sichern(idProjekt);
        }
    }
}

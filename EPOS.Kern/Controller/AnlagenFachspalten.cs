using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die FACHSPALTEN von <c>Tab_Energieanlagen</c> und der EINE Weg, sie von einer
    /// bestehenden Anlagenzeile auf eine neu angelegte zu übertragen.
    ///
    /// <para><b>Was eine Fachspalte ist.</b> Jede Spalte, die die Tabelle JETZT führt und
    /// die Einfügeanweisung <see cref="AnlagenSql.SQL_ANLAGE_INSERT"/> nicht nennt, ohne
    /// <c>ID</c> — das Komplement des Modells. Keine zweite Liste: Wer eine Spalte ins
    /// Modell aufnimmt, verkleinert die Menge von selbst; wer per Schemaschritt eine Spalte
    /// anlegt, ohne das Modell zu erweitern (Sondenfeld, KWKG, Steuer, Quellangaben), ist
    /// von selbst geschützt. Dieselbe Menge rettet der Speicherweg des Assistenten
    /// (<c>WizardCtrl.Fachspalten</c> leitet hierher weiter).</para>
    ///
    /// <para><b>Was nie übertragen wird.</b> Schlüssel und Projektbezug (<c>ID</c>,
    /// <c>ID_Projekt</c>) und die Ergebnisspalten (<see cref="ERGEBNIS"/>): Werte, die die
    /// Simulation schreibt, gehören zum Lauf des Quellprojekts, nicht zur Anlage. Die
    /// Tabelle führt gegenwärtig keine — die Ergebnisse stehen in eigenen Tabellen —, die
    /// Menge hält den Platz, und die Wache <c>FachspaltenEinordnungWacheTests</c> verlangt
    /// für jede neue Spalte eine bewusste Einordnung.</para>
    ///
    /// <para><b>Verweise auf projekteigene Zeilen</b> (<see cref="PROJEKTBEZUG"/>):
    /// Innerhalb eines Projekts unverändert; über Projektgrenzen auf die gleichnamige
    /// Zeile des Zielprojekts abgebildet (Muster <c>KomponentenUebernahmeCtrl</c> bei den
    /// Pufferverweisen), sonst leer — nie ein Verweis in ein fremdes Projekt.</para>
    /// </summary>
    internal static class AnlagenFachspalten
    {
        private const string TABELLE = "Tab_Energieanlagen";

        /// <summary>Schlüssel und Projektbezug — nie übertragen.</summary>
        public static readonly HashSet<string> AUSSCHLUSS =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ID", "ID_Projekt" };

        /// <summary>
        /// Ergebnisspalten der Simulation — nie übertragen. Gegenwärtig leer: Die Simulation
        /// schreibt keine Spalte von <c>Tab_Energieanlagen</c>.
        /// </summary>
        public static readonly HashSet<string> ERGEBNIS =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Fachspalten, die auf eine PROJEKTEIGENE Zeile zeigen (Spalte → Tabelle mit
        /// <c>ID_Projekt</c> und <c>Bezeichner</c>).
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> PROJEKTBEZUG =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "WQ_ID_Quellprofil", "Tab_Quellprofil" },
                { KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE, KaeltemaschineSchema.TAB_PROJEKT }
            };

        /// <summary>Die Spalten, die <see cref="AnlagenSql.SQL_ANLAGE_INSERT"/> nennt - einmal aus der Anweisung gelesen.</summary>
        private static HashSet<string> m_InsertSpalten;

        /// <summary>Die Modellspalten: die Spalten der vollständigen Einfügeanweisung.</summary>
        public static HashSet<string> Modellspalten()
        {
            if (m_InsertSpalten != null) return m_InsertSpalten;

            string sql = AnlagenSql.SQL_ANLAGE_INSERT;
            HashSet<string> menge = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int auf = sql.IndexOf('(');
            int zu = auf >= 0 ? sql.IndexOf(')', auf) : -1;
            if (auf >= 0 && zu > auf)
            {
                foreach (string s in sql.Substring(auf + 1, zu - auf - 1).Split(','))
                {
                    string name = s.Trim();
                    if (name.Length > 0) menge.Add(name);
                }
            }
            m_InsertSpalten = menge;
            return menge;
        }

        /// <summary>
        /// Die Fachspalten: alle Spalten von <c>Tab_Energieanlagen</c>, die
        /// <see cref="AnlagenSql.SQL_ANLAGE_INSERT"/> nicht nennt, ohne <c>ID</c> - in
        /// Schemareihenfolge. Leer, wenn die Tabelle nur die Modellspalten führt.
        /// </summary>
        public static List<string> Fachspalten()
        {
            List<string> fach = new List<string>();
            HashSet<string> insert = Modellspalten();
            foreach (string spalte in DataRepository.SpaltenVonTabelle(TABELLE))
            {
                if (string.Equals(spalte, "ID", StringComparison.OrdinalIgnoreCase)) continue;
                if (insert.Contains(spalte)) continue;
                fach.Add(spalte);
            }
            return fach;
        }

        /// <summary>
        /// Die Fachspalten, die eine Übernahme von einer Quellzeile überträgt:
        /// <see cref="Fachspalten"/> ohne <see cref="AUSSCHLUSS"/> und <see cref="ERGEBNIS"/>.
        /// Liest das Schema — deshalb VOR einem offenen Vorgang erfragen.
        /// </summary>
        public static List<string> UebertragbareSpalten()
        {
            List<string> liste = new List<string>();
            foreach (string spalte in Fachspalten())
                if (!AUSSCHLUSS.Contains(spalte) && !ERGEBNIS.Contains(spalte))
                    liste.Add(spalte);
            return liste;
        }

        /// <summary>
        /// Überträgt die Fachspalten der Anlagenzeile <paramref name="idQuelle"/> auf die
        /// eben angelegte Zeile <paramref name="idZiel"/> — EIN UPDATE mit Zeilenwert-
        /// Zuweisung aus der Quellzeile, NULL eingeschlossen (sonst trüge die neue Zeile die
        /// Vorgabe ihrer Spalte statt des Quellwerts). Verweise aus <see cref="PROJEKTBEZUG"/>
        /// werden in das Projekt der Zielzeile abgebildet.
        /// </summary>
        /// <param name="v">Der offene Vorgang der Übernahme — die neue Zeile ist nur dort sichtbar.</param>
        /// <param name="spalten">Aus <see cref="UebertragbareSpalten"/>, vor dem Vorgang erfragt.
        /// Spaltennamen stammen aus dem Schema, nie aus einer Eingabe.</param>
        /// <returns>Anzahl der Projektverweise, die im Ziel keine Entsprechung fanden und leer bleiben.</returns>
        public static int Uebertragen(DbVorgang v, IReadOnlyList<string> spalten, int idQuelle, int idZiel)
        {
            if (v == null || spalten == null || spalten.Count == 0 || idQuelle <= 0 || idZiel <= 0 ||
                idQuelle == idZiel)
                return 0;

            object projektZiel = v.Skalar("SELECT ID_Projekt FROM " + TABELLE + " WHERE ID = ?",
                                          new DbParam("@z", idZiel));
            if (projektZiel == null || projektZiel == DBNull.Value) return 0;
            int idProjektZiel = Convert.ToInt32(projektZiel, CultureInfo.InvariantCulture);

            var ziel = new List<string>();
            var quelle = new List<string>();
            var ps = new List<DbParam>();
            var bezuege = new List<string>();
            foreach (string spalte in spalten)
            {
                if (AUSSCHLUSS.Contains(spalte) || ERGEBNIS.Contains(spalte)) continue;
                ziel.Add("[" + spalte + "]");

                if (PROJEKTBEZUG.TryGetValue(spalte, out string tabelle))
                {
                    // Gleiches Projekt: der Verweis selbst; sonst die gleichnamige Zeile des
                    // Zielprojekts (kleinste ID bei Namensdoppeln), sonst NULL.
                    quelle.Add("CASE WHEN q.ID_Projekt = ? THEN q.[" + spalte + "] ELSE " +
                               "(SELECT pz.ID FROM [" + tabelle + "] pz JOIN [" + tabelle + "] pq " +
                               "ON pq.Bezeichner = pz.Bezeichner WHERE pq.ID = q.[" + spalte + "] " +
                               "AND pz.ID_Projekt = ? ORDER BY pz.ID LIMIT 1) END");
                    ps.Add(new DbParam("@pz", idProjektZiel));
                    ps.Add(new DbParam("@pz", idProjektZiel));
                    bezuege.Add(spalte);
                }
                else
                {
                    quelle.Add("q.[" + spalte + "]");
                }
            }
            if (ziel.Count == 0) return 0;

            ps.Add(new DbParam("@q", idQuelle));
            ps.Add(new DbParam("@z", idZiel));
            v.Ausfuehren("UPDATE " + TABELLE + " SET (" + string.Join(", ", ziel) + ") = (SELECT " +
                         string.Join(", ", quelle) + " FROM " + TABELLE + " q WHERE q.ID = ?) WHERE ID = ?",
                         ps.ToArray());

            int verloren = 0;
            foreach (string spalte in bezuege)
            {
                object n = v.Skalar("SELECT COUNT(*) FROM " + TABELLE + " q, " + TABELLE + " z " +
                                    "WHERE q.ID = ? AND z.ID = ? AND q.[" + spalte + "] IS NOT NULL " +
                                    "AND z.[" + spalte + "] IS NULL",
                                    new DbParam("@q", idQuelle), new DbParam("@z", idZiel));
                if (n != null && n != DBNull.Value) verloren += Convert.ToInt32(n, CultureInfo.InvariantCulture);
            }
            return verloren;
        }
    }
}

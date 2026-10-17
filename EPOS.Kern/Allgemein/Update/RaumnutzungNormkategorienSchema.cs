using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // NP6 - DIE KATEGORIEN SIA 2024 UND VDI 2078 ENTFALLEN, DIE KATEGORIE DIN BEKOMMT DIE WOHNPROFILE
    // (Entscheid E129, Anwender 11.10.2026; Konzept Nutzungsprofile 5.2 und 5.3).
    //
    // WAS. Die ausgelieferten Kategorien (ReadOnly = 1) des Katalogs der Nutzungsprofile mit der Art SIA_2024 oder
    // VDI_2078 fallen weg. Beide waren seit ihrer Saat leer und schreibgeschuetzt; VDI 2078 definiert keine
    // Nutzungsprofile je Raumtyp. Werte aus eigenen Ausgaben fuehrt der Anwender in eigenen Kategorien.
    //
    //   ohne Profil   DELETE der Kategorie
    //   mit Profil    wider Erwarten: die Kategorie wird eine eigene (Art EIGEN, ReadOnly 0) und im Bericht benannt;
    //                 Profile und Zuordnungen bleiben unberuehrt, nichts wird geloescht
    //
    // DIE WOHNPROFILE DER DIN. Die ausgelieferte Kategorie DIN/TS 18599-10 (Art DIN_V_18599_10, ReadOnly 1) bekommt
    // zwei Profile nach Tabelle 5 der Norm - Einfamilienhaus und Mehrfamilienhaus -, wie die uebrigen ohne Werte (kein
    // Kennwert, kein Zeilenbild, keine Stunden), ohne Nummer (die Norm vergibt keine), ReadOnly 1. Angelegt nur, wenn in der
    // Kategorie noch kein Profil dieses Namens steht (ohne Unterschied der Schreibung). Die Zuordnung DIN_NUMMER 70 und 71
    // der Projektdatei bleibt auf dem Muster Wohnen (NP-F13, NP-F19): Keine Zuordnung aendert sich.
    //
    // KEIN TABELLENNEUBAU: Der CHECK auf Tab_Raumnutzungskatalog.Art behaelt beide Werte (entfallen, im CHECK nur noch
    // Bestand), damit das Schema aller Datenbanken gleich bleibt. Die Saat der Schritte 189 und 190 bleibt, wie sie ist;
    // sie saet die beiden Kategorien weiter, dieser Schritt nimmt sie danach heraus.
    //
    // ERGEBNISNEUTRAL. Kein Rechenweg liest eine Kategorie. Wiederholbar: ein zweiter Lauf findet nichts.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in EPOS.Kern.Tests und die
    // Paketanhebung (Art Katalog: ein Paket fuehrt keinen Katalog der Nutzungsprofile).
    // ====================================================================================

    /// <summary>
    /// <b>NP6</b> — die Kategorien „SIA 2024“ und „VDI 2078“ des Katalogs der Nutzungsprofile entfallen (E129). EINE Quelle
    /// für Migration, Werkzeug, Testkopie und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class RaumnutzungNormkategorienSchema
    {
        /// <summary><b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (219, hängt an 218 KaelteArtSchema).</summary>
        public const int SCHRITT = KaelteArtSchema.SCHRITT + 1;

        /// <summary>Die Kategorien.</summary>
        public const string TAB_KATALOG = RaumnutzungSchema.TAB_KATALOG;

        /// <summary>Die Profile.</summary>
        public const string TAB_PROFIL = RaumnutzungSchema.TAB_PROFIL;

        /// <summary>
        /// Die entfallenen Arten (E129) — im <c>CHECK</c> von <c>Tab_Raumnutzungskatalog.Art</c> nur noch Bestand.
        /// </summary>
        public static readonly IReadOnlyList<string> ARTEN_ENTFALLEN = new[]
        {
            RaumnutzungSchema.ART_SIA_2024, RaumnutzungSchema.ART_VDI_2078
        };

        /// <summary>
        /// Die Namen der zwei Wohnprofile der Kategorie DIN (Tabelle 5 der DIN/TS 18599-10:2025-10, ohne Nummer). Die Namen
        /// bestätigt der Anwender an seiner Ausgabe (Q47) — eine Umbenennung ist eine Zeile hier.
        /// </summary>
        public const string WOHNEN_EFH = "Wohngebäude – Einfamilienhaus";

        /// <summary>Der Name des Wohnprofils Mehrfamilienhaus (siehe <see cref="WOHNEN_EFH"/>).</summary>
        public const string WOHNEN_MFH = "Wohngebäude – Mehrfamilienhaus";

        /// <summary>Die Beschreibung beider Wohnprofile.</summary>
        public const string WOHNEN_BESCHREIBUNG =
            "Nutzungsrandbedingungen für Wohngebäude nach DIN/TS 18599-10:2025-10, Tabelle 5; ohne Nummer in der Norm";

        /// <summary>Die zwei Wohnprofile der Kategorie DIN in Anlegereihenfolge.</summary>
        public static readonly IReadOnlyList<string> WOHNPROFILE = new[] { WOHNEN_EFH, WOHNEN_MFH };

        private const string SQL_WOHNPROFIL_FEHLT =
            "SELECT COUNT(*) FROM \"" + TAB_KATALOG + "\" k WHERE k.\"Art\" = ? AND k.\"ReadOnly\" = 1 " +
            "AND NOT EXISTS (SELECT 1 FROM \"" + TAB_PROFIL + "\" p WHERE p.\"ID_Katalog\" = k.\"ID\" AND p.\"Bezeichner\" = ? COLLATE NOCASE)";

        private const string SQL_WOHNPROFIL_SAAT =
            "INSERT INTO \"" + TAB_PROFIL + "\" (\"ID_Katalog\", \"Nummer\", \"Bezeichner\", \"Beschreibung\", \"ReadOnly\") " +
            "SELECT k.\"ID\", NULL, ?, ?, 1 FROM \"" + TAB_KATALOG + "\" k WHERE k.\"Art\" = ? AND k.\"ReadOnly\" = 1 " +
            "AND NOT EXISTS (SELECT 1 FROM \"" + TAB_PROFIL + "\" p WHERE p.\"ID_Katalog\" = k.\"ID\" AND p.\"Bezeichner\" = ? COLLATE NOCASE)";

        private const string SQL_KANDIDATEN =
            "SELECT k.\"ID\", k.\"Bezeichner\", k.\"Art\", " +
            "(SELECT COUNT(*) FROM \"" + TAB_PROFIL + "\" p WHERE p.\"ID_Katalog\" = k.\"ID\") AS \"Profile\" " +
            "FROM \"" + TAB_KATALOG + "\" k WHERE k.\"ReadOnly\" = 1 AND k.\"Art\" IN (?, ?) ORDER BY k.\"ID\"";

        private const string SQL_LOESCHEN =
            "DELETE FROM \"" + TAB_KATALOG + "\" WHERE \"ID\" = ? AND \"ReadOnly\" = 1 " +
            "AND NOT EXISTS (SELECT 1 FROM \"" + TAB_PROFIL + "\" p WHERE p.\"ID_Katalog\" = ?)";

        private const string SQL_EIGEN =
            "UPDATE \"" + TAB_KATALOG + "\" SET \"Art\" = ?, \"ReadOnly\" = 0 WHERE \"ID\" = ?";

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { TAB_KATALOG, TAB_PROFIL };

        private static DbParam[] ArtParameter() => new[]
        {
            new DbParam("@a1", ARTEN_ENTFALLEN[0]), new DbParam("@a2", ARTEN_ENTFALLEN[1])
        };

        /// <summary>
        /// Was noch offen ist: je ausgelieferte Kategorie der entfallenen Arten 1, je fehlendes Wohnprofil in der ausgelieferten
        /// Kategorie DIN 1. (−1: Katalog fehlt.)
        /// </summary>
        public static int Offen()
        {
            if (!Voraussetzungen().All(DataRepository.TabelleVorhanden)) return -1;
            int offen = DataRepository.GetDataTable(SQL_KANDIDATEN, ArtParameter()).Rows.Count;
            foreach (string name in WOHNPROFILE)
                offen += Convert.ToInt32(DataRepository.ExecuteScalar(SQL_WOHNPROFIL_FEHLT,
                    new DbParam("@a", RaumnutzungSchema.ART_DIN_V_18599_10), new DbParam("@b", name)), CultureInfo.InvariantCulture);
            return offen;
        }

        /// <summary>Steht der Schritt — keine ausgelieferte Kategorie SIA 2024 oder VDI 2078 mehr, beide Wohnprofile in DIN?</summary>
        public static bool Vollstaendig() => Offen() == 0;

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang aus (wiederholbar); Rückgabe = Zahl der gelöschten oder umgestellten
        /// Kategorien und der angelegten Wohnprofile. Fehlt eine vorausgesetzte Tabelle, wirft er benannt.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            var zeilen = new List<string>();
            int aenderungen = 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    DataTable kandidaten = v.Lese(SQL_KANDIDATEN, ArtParameter());
                    foreach (DataRow r in kandidaten.Rows)
                    {
                        long id = Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture);
                        long profile = Convert.ToInt64(r["Profile"], CultureInfo.InvariantCulture);
                        string name = Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture);
                        string art = Convert.ToString(r["Art"], CultureInfo.InvariantCulture);
                        if (profile == 0)
                        {
                            aenderungen += v.Ausfuehren(SQL_LOESCHEN, new DbParam("@id", id), new DbParam("@id2", id));
                            zeilen.Add("Kategorie '" + name + "' (" + art + ", ohne Profil) entfernt");
                        }
                        else
                        {
                            aenderungen += v.Ausfuehren(SQL_EIGEN, new DbParam("@a", RaumnutzungSchema.ART_EIGEN),
                                                        new DbParam("@id", id));
                            zeilen.Add("Kategorie '" + name + "' (" + art + ") traegt " +
                                       profile.ToString(CultureInfo.InvariantCulture) +
                                       " Profil(e) - NICHT geloescht, sondern eigene Kategorie (Art EIGEN, ReadOnly 0)");
                        }
                    }
                    foreach (string name in WOHNPROFILE)
                    {
                        int n = v.Ausfuehren(SQL_WOHNPROFIL_SAAT, new DbParam("@b", name), new DbParam("@d", WOHNEN_BESCHREIBUNG),
                                             new DbParam("@a", RaumnutzungSchema.ART_DIN_V_18599_10), new DbParam("@b2", name));
                        if (n > 0) zeilen.Add("Wohnprofil '" + name + "' in der Kategorie DIN angelegt (ohne Nummer, ohne Werte)");
                        aenderungen += n;
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            if (bericht != null)
            {
                if (zeilen.Count == 0)
                    bericht.Add("steht bereits - keine ausgelieferte Kategorie SIA 2024 oder VDI 2078, Wohnprofile der DIN stehen; nichts zu tun");
                foreach (string z in zeilen) bericht.Add(z);
                bericht.Add("uebrige Profile und alle Zuordnungen unberuehrt; kein Rechenweg liest eine Kategorie, der Referenzlauf bleibt byte-gleich");
            }
            return aenderungen;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}

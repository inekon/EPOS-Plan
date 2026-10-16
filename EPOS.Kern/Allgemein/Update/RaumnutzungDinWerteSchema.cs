using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DA-6 - DIE DIN-NUTZUNGSPROFILE WERDEN MIT WERTEN AUSGELIEFERT (Entscheid E135; Konzept Datenaustausch
    // Simulationsdaten Abschnitt 5). Schritt 231, angemeldet im Statuskopf.
    //
    // WAS. Die 45 ausgelieferten Profile der Kategorie DIN/TS 18599-10 (Art DIN_V_18599_10, ReadOnly 1) bekommen die Werte
    // der Saat RaumnutzungDinSaat (Fassung 2025-10): die 25 Kennwerte und die Aussenluft je Person, zugeordnet ueber die
    // DIN-Nummer (fuehrende Nullen gleich), die zwei Wohnprofile ohne Nummer ueber ihren Namen. Die Beschreibung bekommt
    // den Quellenvermerk der Saat, wo sie leer ist oder den ausgelieferten Text der Wohnprofile traegt. Quellenhinweis und
    // Beschreibung der Kategorie nennen danach die Fassung (nur wo der ausgelieferte Text des Schritts 189 steht).
    //
    // NUR WO KEIN WERT STEHT. Ein Profil wird gesaet, wenn es weder Kennwert noch Aussenluft je Person traegt; ein Profil
    // mit eingelesenen Werten des Anwenders (Werte_Herkunft 'EINGELESEN') bleibt wie es ist. Kein Zeilenbild, keine
    // Stundenprofile. Wiederholbar: ein zweiter Lauf findet nichts.
    //
    // HERKUNFT. Werte_Herkunft bleibt NULL = "wie ausgeliefert"; die Fassung steht im Quellenhinweis der Kategorie. Eine dritte
    // Marke ('AUSGELIEFERT') braeuchte den Neubau der STRICT-Tabelle Tab_Raumnutzungsprofil (der CHECK der Spalte laesst nur
    // 'EINGELESEN' zu, SQLite aendert keinen CHECK), samt Umkopieren und der drei Kindtabellen mit ON DELETE CASCADE - ohne
    // Gewinn: Ausgeliefert heisst bereits ReadOnly 1, und eine leere Herkunft an einem ausgelieferten Profil traegt dieselbe
    // Aussage.
    //
    // ERGEBNISNEUTRAL. Kein Referenzprojekt nutzt ein Nutzungsprofil (Tab_Zone.Nutzungsprofil ueberall leer); kein Rechenweg
    // liest ein Profil ohne Zone.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in EPOS.Kern.Tests und die
    // Paketanhebung (Art Katalog: ein Paket fuehrt keinen Katalog der Nutzungsprofile). Dazu nutzen "Normwerte einlesen"
    // (Ruecknahme) und die Auslieferungsvorlage den Schreibweg der Saat.
    // ====================================================================================

    /// <summary>
    /// <b>DA-6</b> — die Werte der DIN-Nutzungsprofile werden gesät (E135). EINE Quelle für Migration, Werkzeug, Testkopie,
    /// Rücknahme der Normwerte und Auslieferungsvorlage (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class RaumnutzungDinWerteSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht. Angemeldet ist 231; der Nachfolger
        /// <see cref="BauteilkatalogAbschnittSchema"/> (232) hängt über <c>+ 1</c> hier an.
        /// </summary>
        // vorläufig; nach Schritt 230 auf `KaeltebedarfsprofilVorlagenSchema.SCHRITT + 1` umhängen
        public const int SCHRITT = GeraetekostenEntfallSchema.SCHRITT + 1;

        private static readonly string Q_PROFIL = "\"" + RaumnutzungSchema.TAB_PROFIL + "\"";
        private static readonly string Q_KATALOG = "\"" + RaumnutzungSchema.TAB_KATALOG + "\"";

        /// <summary>Die Spalten, die die Saat schreibt: die 25 Kennwerte und die Außenluft je Person.</summary>
        public static IReadOnlyList<string> Wertspalten { get; } =
            RaumnutzungSchema.SPALTEN_KENNWERTE.Select(s => s.Spalte)
                .Concat(new[] { RaumnutzungNormwerteSchema.SPALTE_AUSSENLUFT_PERSON }).ToList();

        private static readonly string DIN_KATALOG =
            "\"ID_Katalog\" IN (SELECT k.\"ID\" FROM " + Q_KATALOG + " k WHERE k.\"Art\" = ? AND k.\"ReadOnly\" = 1)";

        private static readonly string LEER = string.Join(" AND ", Wertspalten.Select(s => "\"" + s + "\" IS NULL"));

        private static readonly string SETZEN =
            "UPDATE " + Q_PROFIL + " SET " + string.Join(", ", Wertspalten.Select(s => "\"" + s + "\" = ?")) +
            ", \"" + RaumnutzungNormwerteSchema.SPALTE_HERKUNFT + "\" = NULL, \"" + RaumnutzungNormwerteSchema.SPALTE_QUELLE +
            "\" = NULL, \"" + RaumnutzungNormwerteSchema.SPALTE_DATUM + "\" = NULL, " +
            "\"Beschreibung\" = IIF(\"Beschreibung\" IS NULL OR \"Beschreibung\" = '' OR \"Beschreibung\" = ?, ?, \"Beschreibung\") " +
            "WHERE \"ID\" = ? AND \"ReadOnly\" = 1";

        /// <summary>Sät ein leeres Profil (Schritt).</summary>
        private static readonly string SQL_SAEN = SETZEN + " AND " + LEER;

        /// <summary>Stellt ein Profil mit eingelesenen Werten auf die Saat zurück (Rücknahme der Normwerte).</summary>
        private static readonly string SQL_ZURUECK = SETZEN + " AND \"" + RaumnutzungNormwerteSchema.SPALTE_HERKUNFT + "\" = ?";

        private static readonly string SQL_PROFILE =
            "SELECT \"ID\", \"Nummer\", \"Bezeichner\", \"" + RaumnutzungNormwerteSchema.SPALTE_HERKUNFT + "\", " +
            string.Join(", ", Wertspalten.Select(s => "\"" + s + "\"")) + ", IIF(" + LEER + ", 1, 0) AS \"Leer\" FROM " + Q_PROFIL +
            " WHERE \"ReadOnly\" = 1 AND " + DIN_KATALOG + " ORDER BY \"ID\"";

        private static readonly string SQL_KATALOG_QUELLE =
            "UPDATE " + Q_KATALOG + " SET \"Quellenhinweis\" = ? WHERE \"Art\" = ? AND \"ReadOnly\" = 1 AND " +
            "(\"Quellenhinweis\" IS NULL OR \"Quellenhinweis\" = '' OR \"Quellenhinweis\" = ?)";

        private static readonly string SQL_KATALOG_BESCHREIBUNG =
            "UPDATE " + Q_KATALOG + " SET \"Beschreibung\" = ? WHERE \"Art\" = ? AND \"ReadOnly\" = 1 AND " +
            "(\"Beschreibung\" IS NULL OR \"Beschreibung\" = '' OR \"Beschreibung\" = ?)";

        private static readonly string SQL_KATALOG_OFFEN =
            "SELECT COUNT(*) FROM " + Q_KATALOG + " WHERE \"Art\" = ? AND \"ReadOnly\" = 1 AND " +
            "(\"Quellenhinweis\" IS NULL OR \"Quellenhinweis\" = '' OR \"Quellenhinweis\" = ? OR " +
            "\"Beschreibung\" IS NULL OR \"Beschreibung\" = '' OR \"Beschreibung\" = ?)";

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { RaumnutzungSchema.TAB_KATALOG, RaumnutzungSchema.TAB_PROFIL };

        private static bool VoraussetzungenStehen()
            => Voraussetzungen().All(DataRepository.TabelleVorhanden) && RaumnutzungNormwerteSchema.Lesbar();

        /// <summary>Ein ausgeliefertes DIN-Profil der Datenbank mit seinen Werten.</summary>
        private sealed record Zeile(long Id, string Nummer, string Bezeichner, string Herkunft, object[] Werte, bool Leer)
        {
            public Raumnutzungsprofil Saat => RaumnutzungDinSaat.Zu(Nummer, Bezeichner);
        }

        private static List<Zeile> Zeilen(Func<string, DbParam[], DataTable> lese)
        {
            DataTable t = lese(SQL_PROFILE, new[] { new DbParam("@a", RaumnutzungSchema.ART_DIN_V_18599_10) });
            var liste = new List<Zeile>();
            if (t == null) return liste;
            foreach (DataRow r in t.Rows)
                liste.Add(new Zeile(Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture), Text(r["Nummer"]), Text(r["Bezeichner"]),
                                    Text(r[RaumnutzungNormwerteSchema.SPALTE_HERKUNFT]),
                                    Wertspalten.Select(s => r[s] == DBNull.Value ? null : r[s]).ToArray(),
                                    Convert.ToInt64(r["Leer"], CultureInfo.InvariantCulture) == 1));
            return liste;
        }

        private static string Text(object o) => o == null || o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);

        /// <summary>
        /// Was noch offen ist: je leeres DIN-Profil mit Saat 1, dazu 1, solange die Kategorie den Text vor der Saat trägt
        /// (−1: eine Voraussetzung fehlt).
        /// </summary>
        public static int Offen()
        {
            if (!VoraussetzungenStehen()) return -1;
            int profile = Zeilen((s, p) => DataRepository.GetDataTable(s, p)).Count(z => z.Leer && z.Saat != null);
            int katalog = Convert.ToInt32(DataRepository.ExecuteScalar(SQL_KATALOG_OFFEN,
                new DbParam("@a", RaumnutzungSchema.ART_DIN_V_18599_10), new DbParam("@q", RaumnutzungDinSaat.QUELLENHINWEIS_VOR_SAAT),
                new DbParam("@b", RaumnutzungDinSaat.BESCHREIBUNG_VOR_SAAT)), CultureInfo.InvariantCulture);
            return profile + (katalog > 0 ? 1 : 0);
        }

        /// <summary>Steht der Schritt?</summary>
        public static bool Vollstaendig() => Offen() == 0;

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang aus (wiederholbar); Rückgabe = Zahl der gesäten Profile. Fehlt eine
        /// vorausgesetzte Tabelle oder Spalte, wirft er benannt.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            if (!RaumnutzungNormwerteSchema.Lesbar())
                throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Spalten des Schritts " +
                                                    RaumnutzungNormwerteSchema.SCHRITT.ToString(CultureInfo.InvariantCulture) + " fehlen.");
            int gesaet, katalog;
            int ohneSaat, mitWerten;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    List<Zeile> zeilen = Zeilen((s, p) => v.Lese(s, p));
                    ohneSaat = zeilen.Count(z => z.Saat == null);
                    mitWerten = zeilen.Count(z => !z.Leer && z.Saat != null);
                    gesaet = 0;
                    foreach (Zeile z in zeilen.Where(z => z.Leer && z.Saat != null))
                        gesaet += v.Ausfuehren(SQL_SAEN, Parameter(z.Saat, z.Id));
                    katalog = v.Ausfuehren(SQL_KATALOG_QUELLE, new DbParam("@q", RaumnutzungSaat.QUELLE_DIN),
                                           new DbParam("@a", RaumnutzungSchema.ART_DIN_V_18599_10), new DbParam("@alt", RaumnutzungDinSaat.QUELLENHINWEIS_VOR_SAAT))
                            + v.Ausfuehren(SQL_KATALOG_BESCHREIBUNG, new DbParam("@b", RaumnutzungSaat.BESCHREIBUNG_DIN),
                                           new DbParam("@a", RaumnutzungSchema.ART_DIN_V_18599_10), new DbParam("@alt", RaumnutzungDinSaat.BESCHREIBUNG_VOR_SAAT));
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
                bericht.Add(gesaet == 0 && katalog == 0
                    ? "steht bereits - die DIN-Nutzungsprofile tragen ihre Werte; nichts zu tun"
                    : RaumnutzungSchema.TAB_PROFIL + ": " + gesaet + " DIN-Profil(e) mit den Werten der Fassung " + RaumnutzungDinSaat.FASSUNG +
                      " gesaet, " + mitWerten + " mit vorhandenen Werten unberuehrt, " + ohneSaat + " ohne Saatzeile; Kategorietexte: " + katalog);
                bericht.Add("kein Zeilenbild, keine Stundenprofile; kein Referenzprojekt nutzt ein Nutzungsprofil, der Referenzlauf bleibt byte-gleich");
            }
            return gesaet;
        }

        /// <summary>
        /// <b>Stellt die Saat an Profilen mit eingelesenen Werten wieder her</b> — im Vorgang des Aufrufers (Rücknahme der
        /// Normwerte, <see cref="RaumnutzungNormwerte.Entfernen"/>). Rückgabe = Zahl der zurückgestellten Profile; Profile
        /// ohne Saatzeile bleiben für den Aufrufer.
        /// </summary>
        internal static int Wiederherstellen(DbVorgang v)
        {
            if (v == null) throw new ArgumentNullException(nameof(v));
            int n = 0;
            foreach (Zeile z in Zeilen((s, p) => v.Lese(s, p))
                         .Where(z => z.Saat != null && z.Herkunft == RaumnutzungNormwerteSchema.HERKUNFT_EINGELESEN))
            {
                var par = Parameter(z.Saat, z.Id).ToList();
                par.Add(new DbParam("@h", RaumnutzungNormwerteSchema.HERKUNFT_EINGELESEN));
                n += v.Ausfuehren(SQL_ZURUECK, par.ToArray());
            }
            return n;
        }

        /// <summary>
        /// <b>Die ausgelieferten DIN-Profile, deren Werte nicht der Saat entsprechen</b> (Nummer bzw. Name): ein Profil mit
        /// Saatzeile und anderen oder fehlenden Werten, ein Profil ohne Saatzeile mit Werten. Leer ohne die Voraussetzungen.
        /// </summary>
        public static IReadOnlyList<string> Abweichungen()
        {
            if (!VoraussetzungenStehen()) return Array.Empty<string>();
            var liste = new List<string>();
            foreach (Zeile z in Zeilen((s, p) => DataRepository.GetDataTable(s, p)))
            {
                Raumnutzungsprofil saat = z.Saat;
                object[] soll = saat == null ? new object[Wertspalten.Count] : Werte(saat);
                if (!Enumerable.Range(0, soll.Length).All(i => Gleich(soll[i], z.Werte[i])))
                    liste.Add((z.Nummer ?? "") + " " + z.Bezeichner);
            }
            return liste;
        }

        /// <summary>Die Werte eines Saatprofils in der Reihenfolge von <see cref="Wertspalten"/> (Wahrheitswerte als 0/1).</summary>
        public static object[] Werte(Raumnutzungsprofil p)
            => p.Kennwerte().Concat(new object[] { p.Aussenluft_Person }).Select(w => w is bool b ? (b ? 1 : 0) : w).ToArray();

        private static DbParam[] Parameter(Raumnutzungsprofil saat, long id)
        {
            object[] w = Werte(saat);
            var par = new List<DbParam>();
            for (int i = 0; i < w.Length; i++) par.Add(new DbParam("@w" + i.ToString(CultureInfo.InvariantCulture), w[i]));
            par.Add(new DbParam("@balt", RaumnutzungNormkategorienSchema.WOHNEN_BESCHREIBUNG));
            par.Add(new DbParam("@b", saat.Beschreibung));
            par.Add(new DbParam("@id", id));
            return par.ToArray();
        }

        private static bool Gleich(object a, object b)
        {
            if (a == null || b == null) return a == null && b == null;
            if (a is string || b is string) return string.Equals(Convert.ToString(a, CultureInfo.InvariantCulture),
                                                                   Convert.ToString(b, CultureInfo.InvariantCulture), StringComparison.Ordinal);
            double da = Convert.ToDouble(a, CultureInfo.InvariantCulture), db = Convert.ToDouble(b, CultureInfo.InvariantCulture);
            return Math.Abs(da - db) <= 1e-9;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}

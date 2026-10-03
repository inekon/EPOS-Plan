using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE BRENNSTOFFE DES PROJEKTS - Projektkopie des Brennstoffkatalogs (Anwenderentscheid
    // 03.10.2026, Schemaschritt ProjektkopienKatalogeSchema, Konzept Simulationsablauf Abschnitt 22).
    //
    // DIE BRENNSTOFFART BLEIBT DIE ID DES STAMMSATZES. Geräte (Tab_Heizkessel.Brennstoff,
    // Tab_BHKW.Brennstoff), Träger (energy_carrier.ID_Brennstoff), Umrechnungen und der
    // Referenzkessel (Tab_ProjektWirtschaftlichkeit.RefKessel_ID_Brennstoff) führen sie; der Kern
    // verzweigt an vielen Stellen über ihre Nummernbereiche (Gas 1–5 und 14, Öl 6–9 und 18–22,
    // Strom 13 …). Eine Umdeutung dieser Spalten auf die ID der Kopie hätte jede dieser Stellen
    // umgeschrieben. Deshalb bleibt der Verweis, wie er ist, und die Kopie hängt über
    // (ID_Projekt, ID_Brennstoff) am Projekt: Wer im Projekt einen WERT des Brennstoffs liest
    // (Emissionsfaktoren, Heizwerte, Kategorie, Name, Preisvorgaben), liest ihn über Sicht() -
    // die Kopie des Projekts, für eine dem Projekt noch unbekannte Brennstoffart der Stamm.
    //
    // DER ABGLEICH fasst nur Tab_Brennstoff_Stamm an. Vor jedem Schreiben sichert er die Kopien
    // aller Projekte (Sichern) - ein Projekt rechnet nach einem Update wie vorher.
    // ====================================================================================

    /// <summary>
    /// Die Projektkopie des Brennstoffkatalogs (<c>Tab_Brennstoff</c>): Lesesicht, Sicherung,
    /// Übernahme, Zurücksetzen und Bearbeiten. Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class ProjektBrennstoffe
    {
        /// <summary>Die Projektkopie.</summary>
        public const string TAB = ProjektkopienKatalogeSchema.TAB_BRENNSTOFF;

        /// <summary>Der Katalog.</summary>
        public const string TAB_STAMM = "Tab_Brennstoff_Stamm";

        /// <summary>Die Fachspalten in fester Folge — dieselben wie im Katalogregister.</summary>
        public static readonly IReadOnlyList<string> FACHSPALTEN = new[]
        {
            "ID_Kategorie", "Bezeichner", "Einheit", "PreisEinheit", "Hi", "Hs", "CO2", "SO2", "NOx", "Staub",
            "PE_Faktor", "Standard_Grundpreis", "Standard_Arbeitspreis", "Standard_Leistungspreis"
        };

        /// <summary>
        /// Die Spalten, die der Projektdialog bearbeiten lässt. Kategorie, Name und Einheiten bestimmen
        /// die Brennstoffart und bleiben, wie der Katalog sie ausgeliefert hat.
        /// </summary>
        public static readonly IReadOnlyList<string> BEARBEITBAR = new[]
        {
            "Hi", "Hs", "CO2", "SO2", "NOx", "Staub", "PE_Faktor",
            "Standard_Grundpreis", "Standard_Arbeitspreis", "Standard_Leistungspreis"
        };

        private static readonly string SPALTEN = string.Join(", ", FACHSPALTEN.Select(s => "\"" + s + "\""));
        private static readonly string SPALTEN_S = string.Join(", ", FACHSPALTEN.Select(s => "s.\"" + s + "\""));

        /// <summary>Die Katalogfassung der Datenbank als Unterabfrage (leer = nie abgeglichen).</summary>
        private const string FASSUNG =
            "(SELECT MAX(\"" + Katalogfassung.SPALTE_FASSUNG + "\") FROM \"" + Katalogfassung.TAB_APPLIKATION + "\")";

        /// <summary>
        /// <b>Die Lesesicht der Brennstoffe eines Projekts</b> als Tabellenausdruck mit den Spalten
        /// <c>ID</c> (die Brennstoffart) und den <see cref="FACHSPALTEN"/>: die Kopie des Projekts, für
        /// jede Brennstoffart ohne Kopie der Stammsatz. Ohne Projekt (<paramref name="idProjekt"/> ≤ 0)
        /// oder ohne Kopietabelle ist es der Katalog selbst.
        /// </summary>
        /// <param name="idProjekt">Das Projekt.</param>
        /// <param name="parameter">
        /// Die Parameter des Ausdrucks — der Aufrufer stellt sie an die Stelle, an der der Ausdruck im
        /// SQL-Text steht (vor die Parameter des WHERE).
        /// </param>
        public static string Sicht(int idProjekt, out DbParam[] parameter)
        {
            if (idProjekt <= 0 || !Vorhanden())
            {
                parameter = Array.Empty<DbParam>();
                return TAB_STAMM;
            }
            parameter = new[] { new DbParam("@sichtProjekt", idProjekt), new DbParam("@sichtProjekt2", idProjekt) };
            return "(SELECT \"ID_Brennstoff\" AS \"ID\", " + SPALTEN + " FROM \"" + TAB + "\" WHERE \"ID_Projekt\" = ? " +
                   "UNION ALL SELECT s.\"ID\", " + SPALTEN_S + " FROM \"" + TAB_STAMM + "\" AS s WHERE NOT EXISTS " +
                   "(SELECT 1 FROM \"" + TAB + "\" AS k WHERE k.\"ID_Projekt\" = ? AND k.\"ID_Brennstoff\" = s.\"ID\"))";
        }

        /// <summary>Hängt die Parameter der Sicht vor die übrigen Parameter.</summary>
        public static DbParam[] Mit(DbParam[] sicht, params DbParam[] weitere)
        {
            var l = new List<DbParam>(sicht ?? Array.Empty<DbParam>());
            if (weitere != null) l.AddRange(weitere);
            return l.ToArray();
        }

        /// <summary>Gibt es die Kopietabelle (Schemaschritt <see cref="ProjektkopienKatalogeSchema.SCHRITT"/>)?</summary>
        public static bool Vorhanden() => DataRepository.TabelleVorhanden(TAB);

        // =================================================================================
        //  Sichern - die Saat des Schritts und die Vorstufe des Abgleichs
        // =================================================================================

        private const string SQL_SICHERN_ALLE =
            "INSERT INTO \"" + TAB + "\" (\"ID_Projekt\", \"ID_Brennstoff\", " +
            "\"ID_Kategorie\", \"Bezeichner\", \"Einheit\", \"PreisEinheit\", \"Hi\", \"Hs\", \"CO2\", \"SO2\", \"NOx\", " +
            "\"Staub\", \"PE_Faktor\", \"Standard_Grundpreis\", \"Standard_Arbeitspreis\", \"Standard_Leistungspreis\", " +
            "\"" + ProjektkopienKatalogeSchema.SPALTE_HERKUNFT + "\") " +
            "SELECT p.\"ID\", s.\"ID\", s.\"ID_Kategorie\", s.\"Bezeichner\", s.\"Einheit\", s.\"PreisEinheit\", s.\"Hi\", " +
            "s.\"Hs\", s.\"CO2\", s.\"SO2\", s.\"NOx\", s.\"Staub\", s.\"PE_Faktor\", s.\"Standard_Grundpreis\", " +
            "s.\"Standard_Arbeitspreis\", s.\"Standard_Leistungspreis\", " + FASSUNG + " " +
            "FROM \"Tab_Projekt\" AS p CROSS JOIN \"" + TAB_STAMM + "\" AS s " +
            "WHERE NOT EXISTS (SELECT 1 FROM \"" + TAB + "\" AS k WHERE k.\"ID_Projekt\" = p.\"ID\" AND k.\"ID_Brennstoff\" = s.\"ID\") " +
            "ORDER BY p.\"ID\", s.\"ID\"";

        private const string SQL_SICHERN_PROJEKT =
            "INSERT INTO \"" + TAB + "\" (\"ID_Projekt\", \"ID_Brennstoff\", " +
            "\"ID_Kategorie\", \"Bezeichner\", \"Einheit\", \"PreisEinheit\", \"Hi\", \"Hs\", \"CO2\", \"SO2\", \"NOx\", " +
            "\"Staub\", \"PE_Faktor\", \"Standard_Grundpreis\", \"Standard_Arbeitspreis\", \"Standard_Leistungspreis\", " +
            "\"" + ProjektkopienKatalogeSchema.SPALTE_HERKUNFT + "\") " +
            "SELECT p.\"ID\", s.\"ID\", s.\"ID_Kategorie\", s.\"Bezeichner\", s.\"Einheit\", s.\"PreisEinheit\", s.\"Hi\", " +
            "s.\"Hs\", s.\"CO2\", s.\"SO2\", s.\"NOx\", s.\"Staub\", s.\"PE_Faktor\", s.\"Standard_Grundpreis\", " +
            "s.\"Standard_Arbeitspreis\", s.\"Standard_Leistungspreis\", " + FASSUNG + " " +
            "FROM \"Tab_Projekt\" AS p CROSS JOIN \"" + TAB_STAMM + "\" AS s " +
            "WHERE p.\"ID\" = ? AND (? = 0 OR s.\"ID\" = ?) " +
            "AND NOT EXISTS (SELECT 1 FROM \"" + TAB + "\" AS k WHERE k.\"ID_Projekt\" = p.\"ID\" AND k.\"ID_Brennstoff\" = s.\"ID\") " +
            "ORDER BY s.\"ID\"";

        private const string SQL_OFFEN =
            "SELECT COUNT(*) FROM \"Tab_Projekt\" AS p CROSS JOIN \"" + TAB_STAMM + "\" AS s " +
            "WHERE NOT EXISTS (SELECT 1 FROM \"" + TAB + "\" AS k WHERE k.\"ID_Projekt\" = p.\"ID\" AND k.\"ID_Brennstoff\" = s.\"ID\")";

        /// <summary>
        /// Legt die fehlenden Kopien an — WERTGLEICH zum Stamm, je Projekt jede Brennstoffart (eine
        /// stehende Kopie bleibt, wie sie ist). Läuft im Vorgang <paramref name="v"/>, damit der Abgleich
        /// sie in seiner Transaktion vor das erste Schreiben setzen kann.
        /// </summary>
        /// <param name="v">Der laufende Vorgang.</param>
        /// <param name="idProjekt">Nur dieses Projekt; <c>null</c> = alle.</param>
        /// <returns>Die Zahl der angelegten Kopien.</returns>
        public static int Sichern(DbVorgang v, int? idProjekt)
        {
            if (v == null) throw new ArgumentNullException(nameof(v));
            return idProjekt.HasValue
                ? v.Ausfuehren(SQL_SICHERN_PROJEKT, new DbParam("@p", idProjekt.Value), new DbParam("@b0", (object)0), new DbParam("@b", (object)0))
                : v.Ausfuehren(SQL_SICHERN_ALLE);
        }

        /// <summary>Wie viele Kopien fehlen (Projekt × Stammsatz ohne Kopie)? 0 ohne Kopietabelle nicht: dann −1.</summary>
        public static int OffeneKopien()
        {
            if (!Vorhanden()) return -1;
            object o = DataRepository.ExecuteScalar(SQL_OFFEN);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        // =================================================================================
        //  Der Projektdialog „Brennstoffe des Projekts"
        // =================================================================================

        private const string SQL_KOPIEN =
            "SELECT k.\"ID\" AS \"ID_Kopie\", k.\"ID_Brennstoff\", k.\"ID_Kategorie\", k.\"Bezeichner\", k.\"Einheit\", " +
            "k.\"PreisEinheit\", k.\"Hi\", k.\"Hs\", k.\"CO2\", k.\"SO2\", k.\"NOx\", k.\"Staub\", k.\"PE_Faktor\", " +
            "k.\"Standard_Grundpreis\", k.\"Standard_Arbeitspreis\", k.\"Standard_Leistungspreis\", " +
            "k.\"" + ProjektkopienKatalogeSchema.SPALTE_HERKUNFT + "\" AS \"Herkunft\", " +
            "s.\"Hi\" AS \"S_Hi\", s.\"Hs\" AS \"S_Hs\", s.\"CO2\" AS \"S_CO2\", s.\"SO2\" AS \"S_SO2\", s.\"NOx\" AS \"S_NOx\", " +
            "s.\"Staub\" AS \"S_Staub\", s.\"PE_Faktor\" AS \"S_PE_Faktor\", s.\"Standard_Grundpreis\" AS \"S_Standard_Grundpreis\", " +
            "s.\"Standard_Arbeitspreis\" AS \"S_Standard_Arbeitspreis\", " +
            "s.\"Standard_Leistungspreis\" AS \"S_Standard_Leistungspreis\", s.\"ID\" AS \"S_ID\" " +
            "FROM \"" + TAB + "\" AS k LEFT JOIN \"" + TAB_STAMM + "\" AS s ON s.\"ID\" = k.\"ID_Brennstoff\" " +
            "WHERE k.\"ID_Projekt\" = ? ORDER BY k.\"ID_Brennstoff\"";

        private const string SQL_OHNE_KOPIE =
            "SELECT s.\"ID\", s.\"Bezeichner\", s.\"Einheit\" FROM \"" + TAB_STAMM + "\" AS s " +
            "WHERE NOT EXISTS (SELECT 1 FROM \"" + TAB + "\" AS k WHERE k.\"ID_Projekt\" = ? AND k.\"ID_Brennstoff\" = s.\"ID\") " +
            "ORDER BY s.\"ID\"";

        private const string SQL_ZURUECKSETZEN =
            "UPDATE \"" + TAB + "\" SET " +
            "\"ID_Kategorie\" = (SELECT s.\"ID_Kategorie\" FROM \"" + TAB_STAMM + "\" AS s WHERE s.\"ID\" = \"" + TAB + "\".\"ID_Brennstoff\"), " +
            "\"Bezeichner\" = (SELECT s.\"Bezeichner\" FROM \"" + TAB_STAMM + "\" AS s WHERE s.\"ID\" = \"" + TAB + "\".\"ID_Brennstoff\"), " +
            "\"Einheit\" = (SELECT s.\"Einheit\" FROM \"" + TAB_STAMM + "\" AS s WHERE s.\"ID\" = \"" + TAB + "\".\"ID_Brennstoff\"), " +
            "\"PreisEinheit\" = (SELECT s.\"PreisEinheit\" FROM \"" + TAB_STAMM + "\" AS s WHERE s.\"ID\" = \"" + TAB + "\".\"ID_Brennstoff\"), " +
            "\"Hi\" = (SELECT s.\"Hi\" FROM \"" + TAB_STAMM + "\" AS s WHERE s.\"ID\" = \"" + TAB + "\".\"ID_Brennstoff\"), " +
            "\"Hs\" = (SELECT s.\"Hs\" FROM \"" + TAB_STAMM + "\" AS s WHERE s.\"ID\" = \"" + TAB + "\".\"ID_Brennstoff\"), " +
            "\"CO2\" = (SELECT s.\"CO2\" FROM \"" + TAB_STAMM + "\" AS s WHERE s.\"ID\" = \"" + TAB + "\".\"ID_Brennstoff\"), " +
            "\"SO2\" = (SELECT s.\"SO2\" FROM \"" + TAB_STAMM + "\" AS s WHERE s.\"ID\" = \"" + TAB + "\".\"ID_Brennstoff\"), " +
            "\"NOx\" = (SELECT s.\"NOx\" FROM \"" + TAB_STAMM + "\" AS s WHERE s.\"ID\" = \"" + TAB + "\".\"ID_Brennstoff\"), " +
            "\"Staub\" = (SELECT s.\"Staub\" FROM \"" + TAB_STAMM + "\" AS s WHERE s.\"ID\" = \"" + TAB + "\".\"ID_Brennstoff\"), " +
            "\"PE_Faktor\" = (SELECT s.\"PE_Faktor\" FROM \"" + TAB_STAMM + "\" AS s WHERE s.\"ID\" = \"" + TAB + "\".\"ID_Brennstoff\"), " +
            "\"Standard_Grundpreis\" = (SELECT s.\"Standard_Grundpreis\" FROM \"" + TAB_STAMM + "\" AS s WHERE s.\"ID\" = \"" + TAB + "\".\"ID_Brennstoff\"), " +
            "\"Standard_Arbeitspreis\" = (SELECT s.\"Standard_Arbeitspreis\" FROM \"" + TAB_STAMM + "\" AS s WHERE s.\"ID\" = \"" + TAB + "\".\"ID_Brennstoff\"), " +
            "\"Standard_Leistungspreis\" = (SELECT s.\"Standard_Leistungspreis\" FROM \"" + TAB_STAMM + "\" AS s WHERE s.\"ID\" = \"" + TAB + "\".\"ID_Brennstoff\"), " +
            "\"" + ProjektkopienKatalogeSchema.SPALTE_HERKUNFT + "\" = " + FASSUNG + " " +
            "WHERE \"ID_Projekt\" = ? AND \"ID_Brennstoff\" = ? " +
            "AND EXISTS (SELECT 1 FROM \"" + TAB_STAMM + "\" AS s WHERE s.\"ID\" = \"" + TAB + "\".\"ID_Brennstoff\")";

        /// <summary>Eine Zeile des Projektdialogs.</summary>
        public sealed class Eintrag
        {
            /// <summary>Die Brennstoffart (ID des Stammsatzes).</summary>
            public int IdBrennstoff { get; init; }

            /// <summary>Die Kategorie (<c>Tab_BrennstoffKategorien</c>).</summary>
            public int IdKategorie { get; init; }

            /// <summary>Der Name.</summary>
            public string Bezeichner { get; init; } = "";

            /// <summary>Die Mengeneinheit.</summary>
            public string Einheit { get; init; } = "";

            /// <summary>Die Werte der bearbeitbaren Spalten (<see cref="BEARBEITBAR"/>), leer = nicht gepflegt.</summary>
            public IReadOnlyDictionary<string, double?> Werte { get; init; } = new Dictionary<string, double?>();

            /// <summary>Die Werte des Katalogs zu denselben Spalten; leer, wenn der Stammsatz fehlt.</summary>
            public IReadOnlyDictionary<string, double?> Katalogwerte { get; init; } = new Dictionary<string, double?>();

            /// <summary>Gibt es den Stammsatz noch?</summary>
            public bool KatalogVorhanden { get; init; }

            /// <summary>Die Katalogfassung zur Zeit der Kopie (leer = nie abgeglichen).</summary>
            public int? Herkunft { get; init; }

            /// <summary>Die Spalten, in denen die Kopie vom Katalog abweicht.</summary>
            public IReadOnlyList<string> Abweichungen =>
                KatalogVorhanden
                    ? BEARBEITBAR.Where(s => !Gleich(Werte.TryGetValue(s, out double? a) ? a : null,
                                                     Katalogwerte.TryGetValue(s, out double? b) ? b : null)).ToList()
                    : Array.Empty<string>();

            /// <summary>Weicht die Kopie vom Katalog ab?</summary>
            public bool WeichtAb => Abweichungen.Count > 0;
        }

        /// <summary>Ein Brennstoff des Katalogs, den das Projekt noch nicht als Kopie führt.</summary>
        public sealed record Katalogsatz(int IdBrennstoff, string Bezeichner, string Einheit);

        /// <summary>Die Kopien eines Projekts samt Vergleich mit dem Katalog, nach Brennstoffart.</summary>
        public static List<Eintrag> Liste(int idProjekt)
        {
            var l = new List<Eintrag>();
            if (idProjekt <= 0 || !Vorhanden()) return l;
            DataTable t = DataRepository.GetDataTable(SQL_KOPIEN, new DbParam("@p", idProjekt));
            if (t == null) return l;
            foreach (DataRow r in t.Rows)
            {
                var w = new Dictionary<string, double?>(StringComparer.Ordinal);
                var k = new Dictionary<string, double?>(StringComparer.Ordinal);
                foreach (string s in BEARBEITBAR)
                {
                    w[s] = Zahl(r[s]);
                    k[s] = Zahl(r["S_" + s]);
                }
                l.Add(new Eintrag
                {
                    IdBrennstoff = Convert.ToInt32(r["ID_Brennstoff"], CultureInfo.InvariantCulture),
                    IdKategorie = r["ID_Kategorie"] == DBNull.Value ? 0 : Convert.ToInt32(r["ID_Kategorie"], CultureInfo.InvariantCulture),
                    Bezeichner = Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture) ?? "",
                    Einheit = r["Einheit"] == DBNull.Value ? "" : Convert.ToString(r["Einheit"], CultureInfo.InvariantCulture),
                    Werte = w,
                    Katalogwerte = k,
                    KatalogVorhanden = r["S_ID"] != DBNull.Value,
                    Herkunft = r["Herkunft"] == DBNull.Value ? null : Convert.ToInt32(r["Herkunft"], CultureInfo.InvariantCulture),
                });
            }
            return l;
        }

        /// <summary>Die Brennstoffe des Katalogs, die das Projekt noch nicht als Kopie führt.</summary>
        public static List<Katalogsatz> OhneKopie(int idProjekt)
        {
            var l = new List<Katalogsatz>();
            if (idProjekt <= 0 || !Vorhanden()) return l;
            DataTable t = DataRepository.GetDataTable(SQL_OHNE_KOPIE, new DbParam("@p", idProjekt));
            if (t == null) return l;
            foreach (DataRow r in t.Rows)
                l.Add(new Katalogsatz(Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture),
                                      Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture) ?? "",
                                      r["Einheit"] == DBNull.Value ? "" : Convert.ToString(r["Einheit"], CultureInfo.InvariantCulture)));
            return l;
        }

        /// <summary>
        /// <b>„Aus Katalog übernehmen"</b>: legt die Kopie eines Stammsatzes wertgleich an; eine stehende
        /// Kopie bleibt. Liefert <c>true</c>, wenn eine Kopie entstand.
        /// </summary>
        public static bool Uebernehmen(int idProjekt, int idBrennstoff)
        {
            if (idProjekt <= 0 || idBrennstoff <= 0 || !Vorhanden()) return false;
            return DataRepository.ExecuteNonQuery(SQL_SICHERN_PROJEKT, new DbParam("@p", idProjekt),
                                                  new DbParam("@b0", idBrennstoff), new DbParam("@b", idBrennstoff)) > 0;
        }

        /// <summary>
        /// Legt für ein Projekt die Kopien aller Brennstoffarten an, die es noch nicht führt (etwa nach
        /// dem Import eines älteren Pakets). Liefert die Zahl der neuen Kopien.
        /// </summary>
        public static int Sichern(int idProjekt)
        {
            if (idProjekt <= 0 || !Vorhanden()) return 0;
            return Math.Max(0, DataRepository.ExecuteNonQuery(SQL_SICHERN_PROJEKT, new DbParam("@p", idProjekt),
                                                              new DbParam("@b0", (object)0), new DbParam("@b", (object)0)));
        }

        /// <summary>
        /// <b>„Auf Katalog zurücksetzen"</b>: schreibt die heutigen Werte des Stammsatzes in die Kopie
        /// und vermerkt die Fassung. Ohne Stammsatz bleibt die Kopie, wie sie ist (<c>false</c>).
        /// </summary>
        public static bool Zuruecksetzen(int idProjekt, int idBrennstoff)
        {
            if (idProjekt <= 0 || idBrennstoff <= 0 || !Vorhanden()) return false;
            return DataRepository.ExecuteNonQuery(SQL_ZURUECKSETZEN, new DbParam("@p", idProjekt),
                                                  new DbParam("@b", idBrennstoff)) > 0;
        }

        /// <summary>
        /// <b>Bearbeiten</b>: schreibt die bearbeitbaren Werte (<see cref="BEARBEITBAR"/>) der Kopie. Ein
        /// fehlender Schlüssel bleibt, <c>null</c> leert das Feld. Ein unbekannter Schlüssel oder ein nicht
        /// endlicher Wert wird benannt abgelehnt.
        /// </summary>
        /// <returns><c>null</c> bei Erfolg, sonst der Grund.</returns>
        public static string Speichern(int idProjekt, int idBrennstoff, IReadOnlyDictionary<string, double?> werte)
        {
            if (werte == null || werte.Count == 0) return null;
            if (idProjekt <= 0 || idBrennstoff <= 0 || !Vorhanden())
                return MyResource.Resource.PBRS_MSG_KEINE_KOPIE;
            var sets = new List<string>();
            var p = new List<DbParam>();
            foreach (KeyValuePair<string, double?> kv in werte)
            {
                string spalte = BEARBEITBAR.FirstOrDefault(s => string.Equals(s, kv.Key, StringComparison.Ordinal));
                if (spalte == null)
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PBRS_MSG_FELD_UNBEKANNT, kv.Key);
                if (kv.Value.HasValue && (double.IsNaN(kv.Value.Value) || double.IsInfinity(kv.Value.Value) || kv.Value.Value < 0))
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PBRS_MSG_WERT_UNGUELTIG, kv.Key);
                sets.Add("\"" + spalte + "\" = ?");
                p.Add(kv.Value.HasValue ? new DbParam("@" + spalte, kv.Value.Value) : new DbParam("@" + spalte, DbParamTyp.Double));
            }
            p.Add(new DbParam("@p", idProjekt));
            p.Add(new DbParam("@b", idBrennstoff));
            int n = DataRepository.ExecuteNonQuery("UPDATE \"" + TAB + "\" SET " + string.Join(", ", sets) +
                                                   " WHERE \"ID_Projekt\" = ? AND \"ID_Brennstoff\" = ?", p.ToArray());
            return n > 0 ? null : MyResource.Resource.PBRS_MSG_KEINE_KOPIE;
        }

        // =================================================================================
        //  Hilfen
        // =================================================================================

        private static double? Zahl(object o)
        {
            if (o == null || o == DBNull.Value) return null;
            try { return Convert.ToDouble(o, CultureInfo.InvariantCulture); }
            catch (FormatException) { return null; }
            catch (InvalidCastException) { return null; }
        }

        private static bool Gleich(double? a, double? b)
        {
            if (!a.HasValue || !b.HasValue) return a.HasValue == b.HasValue;
            return a.Value.Equals(b.Value);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KBP-5a - EINGABEART FUER PROZESSWAERME UND BRAUCHWASSER, NUTZUNGSTAGE NACH NORM AN DEN KAELTEVORLAGEN
    // (Konzept Kaeltebedarfsprofile 3, 8.1 sinngemaess, 8.3; Entscheid OE-5; Entscheid des Anwenders 16.10.2026 zum
    // Kalendervorschlag der Kaeltevorlagen).
    //
    // WAS.
    //   Z_Projekt_Prozesswaerme  Eingabeart        TEXT NOT NULL DEFAULT 'jahr' CHECK IN ('jahr','monat','woche','typtag')
    //   Z_Projekt_Brauchwasser   Eingabeart        (dieselbe Spalte, derselbe CHECK wie Z_Projekt_Kaeltebedarf)
    //   Tab_Kaeltebedarf_STAMM   Nutzungstage_Norm INTEGER nullbar, CHECK 1 ... 365
    //   Tab_Kaeltebedarf         Nutzungstage_Norm (dieselbe Spalte an der Projektkopie; CopyFromStamm traegt sie mit)
    //   Tab_Betriebskalender     Ferien_wie_Sonntag INTEGER NOT NULL DEFAULT 0 CHECK IN (0,1) (KBP-5d, Entscheid des
    //                            Anwenders 16.10.2026: ein Ferientag rechnet wie ein Sonntag - Tagesmenge und Stundengang)
    //   Saat: allein Vorlage 8 "Prozesskaelte einschichtig" = 230 Nutzungstage (DIN/TS 18599-10:2025-10, Hallenprofile
    //   22-24), danach die Pruefsumme dieser einen Zeile neu, wenn sie vorher stimmte.
    //
    // ERGEBNISNEUTRAL. ADD COLUMN mit Vorgabe setzt jede Bestandszuordnung auf 'jahr' - der heutige Weg -, kein UPDATE
    // einer Zuordnung und keines Kalenders - jeder Bestandskalender rechnet mit Ferien_wie_Sonntag = 0 wie heute. Die Nutzungstage sind eine Angabe fuer den Kalendervorschlag (KBP-5b), kein Rechenweg liest sie.
    // Die Zeilen 1-6 des Kaeltekatalogs bleiben leer und damit bytegleich samt Pruefsumme (eine leere Fachspalte traegt
    // zur Pruefsumme nichts bei). Wiederholbar.
    //
    // DREI LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema und die Testvorrichtung in EPOS.Kern.Tests;
    // dazu Z_ProjektProzesswaermeCtrl, Z_ProjektBrauchwasserCtrl, WizardCtrl und KaeltebedarfStammCtrl nach Vorhandensein.
    // ====================================================================================

    /// <summary>
    /// <b>KBP-5a</b> — Eingabeart der Prozesswärme- und Brauchwasserzuordnung (Entscheid OE-5) und die Nutzungstage nach
    /// Norm an den Kältevorlagen. EINE Quelle für Migration, Werkzeug, Testkopie und Nachweis (ADR-001 Option C).
    /// </summary>
    public static class BedarfsprofilEingabeartSchema
    {
        /// <summary><b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht.</summary>
        // KETTE: geparkt auf 234 - 231 RaumnutzungDinWerteSchema, 232 BauteilkatalogAbschnittSchema und 233
        // BauteilUHerkunftSchema liegen noch nicht vor. Sobald 233 auf origin liegt, umhaengen auf
        // "BauteilUHerkunftSchema.SCHRITT + 1" und im selben Schritt eintragen: SchemaMigration.SCHRITTE_SQLITE,
        // Paketanhebung.STUFEN, SchemaStand.Zielversion; danach die Testdatenbank heben.
        public const int SCHRITT = 234;

        /// <summary>Die Zuordnung der Prozesswärme.</summary>
        public const string TAB_PROZESS = "Z_Projekt_Prozesswaerme";

        /// <summary>Die Zuordnung des Brauchwassers.</summary>
        public const string TAB_BRAUCHWASSER = "Z_Projekt_Brauchwasser";

        /// <summary>Der Kältekatalog (Kopf).</summary>
        public const string TAB_KAELTE_STAMM = KaeltebedarfSchema.TAB_KOPF_STAMM;

        /// <summary>Die Projektkopie des Kältekopfs.</summary>
        public const string TAB_KAELTE = KaeltebedarfSchema.TAB_KOPF;

        /// <summary>Die Spalte der Eingabeart — derselbe Name wie an der Kältezuordnung.</summary>
        public const string SPALTE_EINGABEART = KaeltebedarfsprofilVorlagenSchema.SPALTE_EINGABEART;

        /// <summary>Die Spalte der Nutzungstage nach Norm an den Kältevorlagen.</summary>
        public const string SPALTE_NUTZUNGSTAGE = "Nutzungstage_Norm";

        /// <summary>Der Betriebskalender (KBP-5d).</summary>
        public const string TAB_KALENDER = BedarfNetzKalenderSchema.TAB_KALENDER;

        /// <summary>Die Spalte „Ferien wie Sonntag“ am Betriebskalender (KBP-5d), neben „Feiertag wie Sonntag“.</summary>
        public const string SPALTE_FERIEN_WIE_SONNTAG = "Ferien_wie_Sonntag";

        /// <summary>Kleinster zulässiger Wert der Nutzungstage.</summary>
        public const int NUTZUNGSTAGE_MIN = 1;

        /// <summary>Größter zulässiger Wert der Nutzungstage (Gemeinjahr).</summary>
        public const int NUTZUNGSTAGE_MAX = 365;

        /// <summary>Der Katalogschlüssel der einen gesäten Vorlage: „Prozesskälte einschichtig“.</summary>
        public const string SAAT_SCHLUESSEL = "KB:PROZESSKAELTE_EINSCHICHTIG";

        /// <summary>Die ID derselben Vorlage (die Saat von KBP-2 setzt die IDs ausdrücklich).</summary>
        public const int SAAT_ID = 8;

        /// <summary>Die Nutzungstage der Vorlage 8 nach DIN/TS 18599-10:2025-10 (Hallenprofile 22–24, einschichtig).</summary>
        public const int SAAT_NUTZUNGSTAGE = 230;

        private static string Q(string s) => "\"" + s + "\"";

        /// <summary>Der Spaltentyp der Eingabeart: derselbe wie an <c>Z_Projekt_Kaeltebedarf</c>.</summary>
        public static readonly string TYP_EINGABEART = KaeltebedarfsprofilVorlagenSchema.TYP_EINGABEART;

        /// <summary>Der Spaltentyp der Nutzungstage: nullbar, ohne Vorgabe, Prüfklausel 1 … 365.</summary>
        public static readonly string TYP_NUTZUNGSTAGE =
            "INTEGER CHECK (" + Q(SPALTE_NUTZUNGSTAGE) + " IS NULL OR (" + Q(SPALTE_NUTZUNGSTAGE) + " >= " +
            NUTZUNGSTAGE_MIN.ToString(CultureInfo.InvariantCulture) + " AND " + Q(SPALTE_NUTZUNGSTAGE) + " <= " +
            NUTZUNGSTAGE_MAX.ToString(CultureInfo.InvariantCulture) + "))";

        /// <summary>Der Spaltentyp von „Ferien wie Sonntag“: 0/1, Vorgabe 0 (Ferientag wie bisher über den Ferienfaktor).</summary>
        public static readonly string TYP_FERIEN_WIE_SONNTAG =
            "INTEGER NOT NULL DEFAULT 0 CHECK (" + Q(SPALTE_FERIEN_WIE_SONNTAG) + " IN (0,1))";

        /// <summary>Die fünf Spalten in Tabellenfolge.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN = new[]
        {
            (TAB_PROZESS, SPALTE_EINGABEART, TYP_EINGABEART),
            (TAB_BRAUCHWASSER, SPALTE_EINGABEART, TYP_EINGABEART),
            (TAB_KAELTE_STAMM, SPALTE_NUTZUNGSTAGE, TYP_NUTZUNGSTAGE),
            (TAB_KAELTE, SPALTE_NUTZUNGSTAGE, TYP_NUTZUNGSTAGE),
            (TAB_KALENDER, SPALTE_FERIEN_WIE_SONNTAG, TYP_FERIEN_WIE_SONNTAG),
        };

        /// <summary>Der Persistenzwert einer gelesenen Eingabeart: einer der vier Werte, sonst 'jahr'.</summary>
        public static string Eingabeart(object wert) => KaeltebedarfsprofilVorlagenSchema.Eingabeart(wert);

        /// <summary>Die gelesenen Nutzungstage: 1 … 365 oder null (keine Angabe).</summary>
        public static int? Nutzungstage(object wert)
        {
            if (wert == null || wert == DBNull.Value) return null;
            if (!double.TryParse(Convert.ToString(wert, CultureInfo.InvariantCulture), NumberStyles.Float,
                                 CultureInfo.InvariantCulture, out double d)) return null;
            int n = (int)Math.Round(d);
            return n >= NUTZUNGSTAGE_MIN && n <= NUTZUNGSTAGE_MAX ? n : null;
        }

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { TAB_PROZESS, TAB_BRAUCHWASSER, TAB_KAELTE_STAMM, TAB_KAELTE, TAB_KALENDER };

        /// <summary>Steht „Ferien wie Sonntag“ am Betriebskalender?</summary>
        public static bool FerienWieSonntagLesbar() => DataRepository.SpalteVorhanden(TAB_KALENDER, SPALTE_FERIEN_WIE_SONNTAG);

        /// <summary>Die Anweisung, die eine Spalte anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE " + Q(s.Tabelle) + " ADD COLUMN " + Q(s.Spalte) + " " + s.Typ;

        /// <summary>Steht die Eingabeart an der Zuordnung <paramref name="tabelle"/>?</summary>
        public static bool EingabeartLesbar(string tabelle) => DataRepository.SpalteVorhanden(tabelle, SPALTE_EINGABEART);

        /// <summary>Stehen die Nutzungstage am Kältekatalog?</summary>
        public static bool NutzungstageLesbar() => DataRepository.SpalteVorhanden(TAB_KAELTE_STAMM, SPALTE_NUTZUNGSTAGE);

        /// <summary>Stehen die fünf Spalten?</summary>
        public static bool SchemaVollstaendig() => SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>Schema vollständig und die Saat der Vorlage 8 gesetzt (oder die Vorlage fehlt)?</summary>
        public static bool Vollstaendig() => SchemaVollstaendig() && SaatOffen() == 0;

        /// <summary>Ist die Vorlage 8 da, aber ohne Nutzungstage? 1 = offen, sonst 0.</summary>
        public static int SaatOffen()
        {
            if (!NutzungstageLesbar()) return 1;
            DataRow r = SaatZeile();
            return r != null && r[SPALTE_NUTZUNGSTAGE] == DBNull.Value ? 1 : 0;
        }

        /// <summary>
        /// Führt den Schritt aus (wiederholbar): die Spalten in EINEM Vorgang, danach die Saat der Vorlage 8.
        /// Rückgabe = Zahl der angelegten Spalten. Fehlt eine Voraussetzung, wirft er benannt.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung.
            var offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();
            if (offen.Count > 0)
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        foreach ((string Tabelle, string Spalte, string Typ) s in offen) v.Ausfuehren(Anlegen(s));
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
                if (bericht != null)
                    foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                        bericht.Add(s.Tabelle + "." + s.Spalte + " angelegt");
            }
            else
            {
                bericht?.Add("Eingabeart, Nutzungstage und Ferien wie Sonntag stehen bereits");
            }

            Saeen(bericht);
            bericht?.Add("KEIN UPDATE an Zuordnungen und Kalendern; jeder Betriebskalender rechnet mit " +
                         SPALTE_FERIEN_WIE_SONNTAG + " = 0 wie bisher, jede Prozesswaerme- und Brauchwasserzuordnung rechnet mit Eingabeart '" +
                         KaeltebedarfsprofilVorlagenSchema.EINGABE_JAHR + "', die Kaeltezeilen 1-6 bleiben unberuehrt, " +
                         "der Referenzlauf bleibt byte-gleich");
            return offen.Count;
        }

        /// <summary>
        /// Die Saat: Vorlage 8 bekommt ihre Nutzungstage, nur wenn das Feld leer ist (eine Anwenderangabe bleibt). Stimmte
        /// ihre Prüfsumme vorher, wird sie mit dem neuen Wert neu gebildet; eine abweichende (vom Anwender geänderte) Zeile
        /// behält ihre Summe — der Katalogabgleich erkennt sie dann wie jede andere Änderung.
        /// </summary>
        private static void Saeen(IList<string> bericht)
        {
            DataRow r = SaatZeile();
            if (r == null || r[SPALTE_NUTZUNGSTAGE] != DBNull.Value) return;

            bool stimmig = false;
            Katalogtabelle t = Katalogfassung.Tabelle(TAB_KAELTE_STAMM);
            if (t != null && Katalogfassung.SpaltenVorhanden(TAB_KAELTE_STAMM) && r[Katalogfassung.SPALTE_PRUEFSUMME] != DBNull.Value)
                stimmig = string.Equals(Convert.ToString(r[Katalogfassung.SPALTE_PRUEFSUMME], CultureInfo.InvariantCulture),
                                        Katalogfassung.PruefsummeDerZeile(t, Katalogfassung.VorhandeneFachspalten(t), r,
                                                                          Katalogfassung.LeseOhneVorgang),
                                        StringComparison.Ordinal);

            if (!DataRepository.ExecuteSQL("UPDATE " + Q(TAB_KAELTE_STAMM) + " SET " + Q(SPALTE_NUTZUNGSTAGE) + " = ? " +
                                           "WHERE \"ID\" = ? AND " + Q(SPALTE_NUTZUNGSTAGE) + " IS NULL",
                                           new DbParam("@tage", SAAT_NUTZUNGSTAGE), new DbParam("@id", SAAT_ID)))
                throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Nutzungstage der Vorlage " +
                                                    SAAT_ID.ToString(CultureInfo.InvariantCulture) + " liessen sich nicht setzen.");
            bericht?.Add(TAB_KAELTE_STAMM + ": Vorlage " + SAAT_ID.ToString(CultureInfo.InvariantCulture) + " mit " +
                         SAAT_NUTZUNGSTAGE.ToString(CultureInfo.InvariantCulture) + " Nutzungstagen nach Norm");
            if (stimmig)
                GeraetekostenEntfallSchema.PruefsummenNeuBilden(
                    new Dictionary<string, List<long>> { [TAB_KAELTE_STAMM] = new List<long> { SAAT_ID } }, bericht);
        }

        /// <summary>Die Zeile der Vorlage 8 (ID und Katalogschlüssel), alle Spalten; <c>null</c>, wenn sie fehlt.</summary>
        private static DataRow SaatZeile()
        {
            if (!DataRepository.SpalteVorhanden(TAB_KAELTE_STAMM, Katalogfassung.SPALTE_SCHLUESSEL)) return null;
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM " + Q(TAB_KAELTE_STAMM) + " WHERE \"ID\" = ? AND " +
                                                       Q(Katalogfassung.SPALTE_SCHLUESSEL) + " = ?",
                                                       new DbParam("@id", SAAT_ID), new DbParam("@schl", SAAT_SCHLUESSEL));
            return dt != null && dt.Rows.Count > 0 && dt.Columns.Contains(SPALTE_NUTZUNGSTAGE) ? dt.Rows[0] : null;
        }

        // ---- Lesen und Schreiben der Eingabeart an Prozesswaerme und Brauchwasser (spaltentolerant) -------------------

        /// <summary>
        /// Die Eingabeart je Zuordnungszeile (<c>ID</c> → Wert) eines Projekts; ohne Spalte leer (jede Zeile heißt dann
        /// 'jahr'). Die ganze Zeile wird gelesen: Die Spalte entsteht erst im Schemaschritt.
        /// </summary>
        public static Dictionary<int, string> EingabeartenDerZuordnungen(string tabelle, int idProjekt)
        {
            var je = new Dictionary<int, string>();
            if (!EingabeartLesbar(tabelle)) return je;
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM " + Q(tabelle) + " WHERE \"ID_Projekt\" = ?",
                                                       new DbParam("@p", idProjekt));
            if (dt == null || !dt.Columns.Contains(SPALTE_EINGABEART)) return je;
            foreach (DataRow r in dt.Rows)
                je[Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture)] = Eingabeart(r[SPALTE_EINGABEART]);
            return je;
        }

        /// <summary>
        /// Schreibt die Eingabeart der Zuordnungszeile <paramref name="idZ"/>. 'jahr' ist die Vorgabe der Spalte und wird
        /// nicht geschrieben (das INSERT des Bestands bleibt unverändert); ohne Spalte nur 'jahr' zulässig — eine andere
        /// Art lehnt der Weg benannt ab (Rückgabe <c>false</c>), statt sie still fallen zu lassen.
        /// </summary>
        public static bool EingabeartSetzen(string tabelle, int idZ, string wert)
        {
            string e = Eingabeart(wert);
            if (e == KaeltebedarfsprofilVorlagenSchema.EINGABE_JAHR) return true;
            if (!EingabeartLesbar(tabelle))
            {
                DataRepository.FehlerMelden("Die Eingabeart \"" + e + "\" laesst sich erst nach Schemaschritt " + Nr +
                                            " speichern (" + tabelle + ").");
                return false;
            }
            return DataRepository.ExecuteSQL("UPDATE " + Q(tabelle) + " SET " + Q(SPALTE_EINGABEART) + " = ? WHERE \"ID\" = ?",
                                             new DbParam("@e", e), new DbParam("@id", idZ));
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}

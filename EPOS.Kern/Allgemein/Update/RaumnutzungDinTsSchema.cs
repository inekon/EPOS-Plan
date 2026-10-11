using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // NP5b - DIE KATEGORIE DIN AUF DIE DIN/TS 18599-10:2025-10 (Schritt 190; Entscheid E96; Konzept
    // Nutzungsprofile 5.2 und 5.4).
    //
    // ANLASS. Die DIN/TS 18599-10:2025-10 ersetzt die DIN V 18599-10:2018-09 und nummeriert die Nutzungen
    // ab 22 neu: 2018 zaehlte 1 bis 21, 22.1 bis 22.3 und 23 bis 41, 2025 zaehlt durchgehend 1 bis 43
    // (22.1 bis 22.3 -> 22 bis 24, 23 bis 41 -> 25 bis 43). Ausgeliefert werden weiter allein Nummer und
    // Name - Tatsachen ohne Werte (E90 bleibt).
    //
    // WAS DER SCHRITT TUT (ein Vorgang, KEIN DDL, wiederholbar):
    //
    //   1. Die ausgelieferte Kategorie (ReadOnly, Art DIN_V_18599_10) mit dem Namen der Ausgabe 2018 heisst
    //      danach "DIN/TS 18599-10" und traegt Beschreibung und Quellenhinweis der Ausgabe 2025.
    //   2. Ihre ausgelieferten Profile der Saat 2018 (24 Paare Nummer/Name, SAAT_2018) bekommen Nummer und
    //      Namen der Ausgabe 2025 - die Ids bleiben, eine Zuordnung oder ein Duplikat des Anwenders zeigt
    //      danach auf dieselbe Nutzung. Nummer und Name sind je Kategorie eindeutig: Die Zeilen werden
    //      zuerst geparkt (Nummer leer, Hilfsname), dann gesetzt.
    //   3. Die uebrigen Nutzungen der Ausgabe 2025 (19) entstehen ohne Werte; fehlt die Kategorie, entsteht
    //      sie aus der Saat.
    //   4. Die Zuordnung DIN_NUMMER zaehlt nach der Ausgabe 2025: Der Schluessel ist die Nummer der
    //      HottCAD-Projektdatei (PdProfileUsage.ProfileUsageType), und HottCAD zaehlt nach der DIN/TS
    //      18599-10:2025-10 (E96, Anwender 06.10.2026; Konzept 5.4). Die ausgelieferten Zeilen ab 22 der Saat
    //      2018 (ZUORDNUNG_2018: 28, 29 Bibliothek, 31 Turnhalle, 35 Fitnessraum, 41 Lagerhallen) bekommen den
    //      Schluessel derselben Nutzung in 2025 (30, 31, 33, 37, 43); Id und Profil bleiben. Der neue Schluessel
    //      31 ist der alte der Turnhalle: Die Zeilen werden zuerst geparkt (Hilfsschluessel), dann gesetzt.
    //      Steht auf dem neuen Schluessel schon eine andere Zeile (eine eigene des Anwenders), bleibt sie, und
    //      die ausgelieferte entfaellt - benannt im Migrationsprotokoll.
    //   5. Die Zuordnung DIN_NUMMER bekommt die ausgelieferten Zeilen 19 -> EPOS-Muster Verkehr und
    //      20 -> EPOS-Muster Lager (E96, Konzept 5.4) - nur, wenn fuer den Schluessel noch keine Zeile steht;
    //      eine Zeile des Anwenders (auch "keine") bleibt. 19 und 20 zaehlen 2018 und 2025 gleich.
    //
    // WAS ER NICHT ANFASST: eigene Kategorien, Profile und Zuordnungen des Anwenders, jedes Profil, das
    // nicht zur Saat 2018 gehoert, jede eigene Zeile der Zuordnung, das Profil jeder stehenden Zeile, die
    // Schluessel 44 bis 47 der Projektdatei (keine Normnummern, ohne Zuordnung) und die Profilnamen, die als
    // Kopie an Zone und Kalender stehen (Q41). Trifft der Umbau auf eine fremde Zeile
    // gleichen Namens oder gleicher Nummer, bricht er benannt ab und nimmt alles zurueck.
    //
    // DIE SAAT EINER NEUEN DATENBANK steht schon in RaumnutzungSaat (Ausgabe 2025); Schritt 189 saet sie
    // und ruft vorher AlteFassungUmbauen, damit er auch auf einer Datei mit der Saat 2018 keine zweite
    // Kategorie anlegt. Beide Wege enden im selben Bestand.
    //
    // ERGEBNISNEUTRAL. Kein Rechenweg liest den Katalog; der Referenzlauf bleibt byte-gleich.
    //
    // VIER LESER wie Schritt 189: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die
    // Testvorrichtung in EPOS.Kern.Tests und die Paketanhebung (Art Katalog: ein Paket fuehrt keinen
    // Katalog der Nutzungsprofile).
    // ====================================================================================

    /// <summary>
    /// <b>NP5b</b> — die ausgelieferte Kategorie DIN auf die DIN/TS 18599-10:2025-10: Name, Quellenhinweis,
    /// Nummern und Namen der 43 Nutzungen, Ids bleiben, dazu die Zuordnung DIN nach der Zählung 2025 und DIN 19 und 20 (E96).
    /// EINE Quelle für Migration,
    /// Werkzeug, Testkopie und
    /// Nachweis (ADR-001 Option C). Anlass und Grenzen stehen im Kopf der Datei.
    /// </summary>
    public static class RaumnutzungDinTsSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (angemeldet als 190).
        /// </summary>
        public const int SCHRITT = RaumnutzungSchema.SCHRITT + 1;

        /// <summary>
        /// Die Schlüssel <c>DIN_NUMMER</c>, die der Schritt der Zuordnung beifügt: 19 (Verkehrsflächen) und 20 (Lager, Technik,
        /// Archiv) zählen in beiden Ausgaben gleich (E96, Konzept 5.4). Ihre Zeilen stehen in <see cref="RaumnutzungSaat.Zuordnungen"/>.
        /// </summary>
        public static readonly IReadOnlyList<string> SCHLUESSEL_ZUORDNUNG = new[] { "19", "20" };

        /// <summary>Die ausgelieferten Zuordnungszeilen der <see cref="SCHLUESSEL_ZUORDNUNG"/> — aus der Saat, eine Quelle.</summary>
        public static IReadOnlyList<RaumnutzungSaatzuordnung> Zuordnungen { get; } = RaumnutzungSaat.Zuordnungen
            .Where(z => z.Art == RaumnutzungSchema.ZUORDNUNG_DIN && SCHLUESSEL_ZUORDNUNG.Contains(z.Schluessel)).ToList();

        /// <summary>
        /// Die ausgelieferten Zeilen <c>DIN_NUMMER</c> ab 22 der Saat von Schritt 189 vor E96 — Schlüssel nach der Zählung
        /// 2018 und EPOS-Muster —, eingefroren, damit der Schritt sie erkennt. Ihre Schlüssel nach der Zählung 2025
        /// (<see cref="Nummer2025"/>) stehen mit denselben Mustern in <see cref="RaumnutzungSaat.Zuordnungen"/>.
        /// </summary>
        public static readonly IReadOnlyList<(string Schluessel, string Profil)> ZUORDNUNG_2018 = new[]
        {
            ("28", RaumnutzungSaat.SCHULE), ("29", RaumnutzungSaat.SCHULE), ("31", RaumnutzungSaat.SPORT),
            ("35", RaumnutzungSaat.SPORT), ("41", RaumnutzungSaat.LAGER),
        };

        /// <summary>
        /// Die Schlüssel der <see cref="ZUORDNUNG_2018"/>, die allein die Zählung 2018 trägt (28, 29, 35, 41; 31 ist 2025 die
        /// Bibliothek – Freihandbereich): Steht einer davon ausgeliefert, zählt die Zuordnung noch nach 2018.
        /// </summary>
        public static IReadOnlyList<string> SCHLUESSEL_NUR_2018 { get; } = ZUORDNUNG_2018.Select(z => z.Schluessel)
            .Except(ZUORDNUNG_2018.Select(z => Nummer2025(z.Schluessel)), StringComparer.Ordinal).ToList();

        /// <summary>Der Name der Kategorie in der Saat von Schritt 189 vor E96 (Ausgabe 2018).</summary>
        public const string KATEGORIE_DIN_2018 = "DIN V 18599-10";

        /// <summary>
        /// Die 24 Paare Nummer/Name der Saat von Schritt 189 vor E96 (DIN V 18599-10:2018-09) — eingefroren, damit
        /// der Umbau sie erkennt; ohne Werte.
        /// </summary>
        public static readonly IReadOnlyList<(string Nummer, string Name)> SAAT_2018 = new (string, string)[]
        {
            ("1", "Einzelbüro"), ("2", "Gruppenbüro"), ("3", "Großraumbüro"),
            ("4", "Besprechung, Sitzung, Seminar"), ("5", "Schalterhalle"),
            ("8", "Klassenzimmer, Gruppenraum (Kindergarten)"), ("9", "Hörsaal, Auditorium"),
            ("10", "Bettenzimmer"), ("11", "Hotelzimmer"), ("12", "Kantine"), ("13", "Restaurant"),
            ("21", "Rechenzentrum"), ("25", "Bühne"), ("26", "Messe/Kongress"), ("27", "Ausstellung/Museum"),
            ("28", "Bibliothek – Lesesaal"), ("29", "Bibliothek – Freihandbereich"),
            ("31", "Turnhalle"), ("32", "Parkhaus (Büro/Privat)"), ("33", "Parkhaus (öffentlich)"),
            ("34", "Saunabereich"), ("35", "Fitnessraum"),
            ("40", "Arztpraxen, therapeutische Praxen"), ("41", "Lagerhallen, Logistikhallen"),
        };

        /// <summary>
        /// Die Nummer der Ausgabe 2025 zu einer Nummer der Ausgabe 2018: 1 bis 21 bleiben, 22.1 bis 22.3 werden 22 bis
        /// 24, 23 bis 41 werden 25 bis 43; jede andere Angabe <c>null</c>.
        /// </summary>
        public static string Nummer2025(string nummer2018)
        {
            switch (nummer2018)
            {
                case "22.1": return "22";
                case "22.2": return "23";
                case "22.3": return "24";
            }
            if (string.IsNullOrEmpty(nummer2018) || nummer2018.Length > 2 || !nummer2018.All(char.IsAsciiDigit)) return null;
            int n = int.Parse(nummer2018, NumberStyles.None, CultureInfo.InvariantCulture);
            if (n >= 1 && n <= 21 && nummer2018[0] != '0') return nummer2018;
            if (n >= 23 && n <= 41) return (n + 2).ToString(CultureInfo.InvariantCulture);
            return null;
        }

        /// <summary>Der Name der Ausgabe 2025 zu einer Nummer der Ausgabe 2025 (<see cref="RaumnutzungSaat.Din"/>); sonst <c>null</c>.</summary>
        public static string Name2025(string nummer2025)
            => RaumnutzungSaat.Din.FirstOrDefault(p => string.Equals(p.Nummer, nummer2025, StringComparison.Ordinal))?.Bezeichner;

        // =================================================================
        //  Die Anweisungen
        // =================================================================

        private const string K = "\"" + RaumnutzungSchema.TAB_KATALOG + "\"";
        private const string P = "\"" + RaumnutzungSchema.TAB_PROFIL + "\"";

        /// <summary>Die ausgelieferte Kategorie DIN unter einem Namen.</summary>
        internal const string SQL_KATEGORIE =
            "SELECT \"ID\" FROM " + K + " WHERE \"ReadOnly\" = 1 AND \"Art\" = ? AND \"Bezeichner\" = ? COLLATE NOCASE";

        /// <summary>Irgendeine Kategorie unter einem Namen.</summary>
        internal const string SQL_KATEGORIE_NAME = "SELECT COUNT(*) FROM " + K + " WHERE \"Bezeichner\" = ? COLLATE NOCASE";

        /// <summary>Name, Beschreibung und Quellenhinweis der Ausgabe 2025.</summary>
        internal const string SQL_KATEGORIE_UMBENENNEN =
            "UPDATE " + K + " SET \"Bezeichner\" = ?, \"Beschreibung\" = ?, \"Quellenhinweis\" = ? WHERE \"ID\" = ?";

        /// <summary>Beschreibung und Quellenhinweis, nur wenn sie abweichen.</summary>
        internal const string SQL_KATEGORIE_TEXTE =
            "UPDATE " + K + " SET \"Beschreibung\" = ?, \"Quellenhinweis\" = ? WHERE \"ID\" = ? AND " +
            "(\"Beschreibung\" IS NOT ? OR \"Quellenhinweis\" IS NOT ?)";

        /// <summary>Stimmen Beschreibung und Quellenhinweis der Kategorie?</summary>
        internal const string SQL_KATEGORIE_TEXTE_GLEICH =
            "SELECT COUNT(*) FROM " + K + " WHERE \"ID\" = ? AND \"Beschreibung\" IS ? AND \"Quellenhinweis\" IS ?";

        /// <summary>Die ausgelieferten Profile einer Kategorie.</summary>
        internal const string SQL_PROFILE_AUSGELIEFERT =
            "SELECT \"ID\", \"Nummer\", \"Bezeichner\" FROM " + P + " WHERE \"ID_Katalog\" = ? AND \"ReadOnly\" = 1 ORDER BY \"ID\"";

        /// <summary>Eine Zeile parken: Nummer leer, Hilfsname.</summary>
        internal const string SQL_PROFIL_PARKEN = "UPDATE " + P + " SET \"Nummer\" = NULL, \"Bezeichner\" = ? WHERE \"ID\" = ?";

        /// <summary>Nummer und Name der Ausgabe 2025 setzen.</summary>
        internal const string SQL_PROFIL_SETZEN = "UPDATE " + P + " SET \"Nummer\" = ?, \"Bezeichner\" = ? WHERE \"ID\" = ?";

        /// <summary>Trägt eine ANDERE Zeile der Kategorie den Namen oder die Nummer?</summary>
        internal const string SQL_PROFIL_BELEGT =
            "SELECT COUNT(*) FROM " + P + " WHERE \"ID_Katalog\" = ? AND \"ID\" <> ? AND " +
            "(\"Bezeichner\" = ? COLLATE NOCASE OR \"Nummer\" = ? COLLATE NOCASE)";

        /// <summary>Die Saat einer Nutzung ohne Werte — nur, wenn weder Name noch Nummer in der Kategorie stehen.</summary>
        internal const string SQL_SAAT_PROFIL =
            "INSERT INTO " + P + " (\"ID_Katalog\", \"Nummer\", \"Bezeichner\", \"ReadOnly\") SELECT k.\"ID\", ?, ?, 1 FROM " + K +
            " k WHERE k.\"ID\" = ? AND NOT EXISTS (SELECT 1 FROM " + P + " p WHERE p.\"ID_Katalog\" = k.\"ID\" AND " +
            "(p.\"Bezeichner\" = ? COLLATE NOCASE OR p.\"Nummer\" = ? COLLATE NOCASE))";

        /// <summary>
        /// Eine Zeile der Zuordnung auf ein ausgeliefertes EPOS-Muster — nur, wenn für (Art, Schlüssel) noch keine Zeile steht
        /// (eindeutiger Index ohne Unterschied der Schreibung) und das Muster besteht. Sechs Parameter: Art, Schlüssel,
        /// Katalogart, Muster, Art, Schlüssel. NOT EXISTS statt INSERT OR IGNORE: SQLite zählt den AUTOINCREMENT-Stand
        /// auch bei ignoriertem Einfügen hoch.
        /// </summary>
        internal const string SQL_SAAT_ZUORDNUNG =
            "INSERT INTO \"" + RaumnutzungSchema.TAB_ZUORDNUNG + "\" (\"Art\", \"Schluessel\", \"ID_Profil\", \"ReadOnly\") " +
            "SELECT ?, ?, p.\"ID\", 1 FROM " + P + " p JOIN " + K + " k ON k.\"ID\" = p.\"ID_Katalog\" " +
            "WHERE k.\"ReadOnly\" = 1 AND k.\"Art\" = ? AND p.\"ReadOnly\" = 1 AND p.\"Bezeichner\" = ? COLLATE NOCASE " +
            "AND NOT EXISTS (SELECT 1 FROM \"" + RaumnutzungSchema.TAB_ZUORDNUNG + "\" z WHERE z.\"Art\" = ? " +
            "AND z.\"Schluessel\" = ? COLLATE NOCASE)";

        private const string Z = "\"" + RaumnutzungSchema.TAB_ZUORDNUNG + "\"";

        /// <summary>Die ausgelieferte Zeile der Zuordnung unter (Art, Schlüssel).</summary>
        internal const string SQL_ZUORDNUNG_AUSGELIEFERT =
            "SELECT \"ID\" FROM " + Z + " WHERE \"ReadOnly\" = 1 AND \"Art\" = ? AND \"Schluessel\" = ? COLLATE NOCASE";

        /// <summary>Wie viele ausgelieferte Zeilen der Zuordnung stehen unter (Art, Schlüssel)?</summary>
        internal const string SQL_ZAHL_ZUORDNUNG_AUSGELIEFERT =
            "SELECT COUNT(*) FROM " + Z + " WHERE \"ReadOnly\" = 1 AND \"Art\" = ? AND \"Schluessel\" = ? COLLATE NOCASE";

        /// <summary>Den Schlüssel einer Zeile der Zuordnung setzen (Parken und Umstellen).</summary>
        internal const string SQL_ZUORDNUNG_SCHLUESSEL = "UPDATE " + Z + " SET \"Schluessel\" = ? WHERE \"ID\" = ?";

        /// <summary>Trägt eine ANDERE Zeile derselben Art den Schlüssel? Liefert ihre Auslieferungsmarke.</summary>
        internal const string SQL_ZUORDNUNG_BELEGT =
            "SELECT \"ReadOnly\" FROM " + Z + " WHERE \"Art\" = ? AND \"ID\" <> ? AND \"Schluessel\" = ? COLLATE NOCASE " +
            "ORDER BY \"ID\" LIMIT 1";

        /// <summary>Eine ausgelieferte Zeile der Zuordnung entfernen.</summary>
        internal const string SQL_ZUORDNUNG_ENTFERNEN = "DELETE FROM " + Z + " WHERE \"ID\" = ? AND \"ReadOnly\" = 1";

        /// <summary>Steht die Nutzung genau so (Nummer, Name, ausgeliefert) in der Kategorie?</summary>
        internal const string SQL_ZAHL_PROFIL_GENAU =
            "SELECT COUNT(*) FROM " + P + " WHERE \"ID_Katalog\" = ? AND \"ReadOnly\" = 1 AND \"Nummer\" = ? AND \"Bezeichner\" = ?";

        // =================================================================
        //  Stand
        // =================================================================

        /// <summary>Die Tabellen, die Schritt 189 angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { RaumnutzungSchema.TAB_KATALOG, RaumnutzungSchema.TAB_PROFIL };

        private static RaumnutzungSaatkategorie KategorieDin
            => RaumnutzungSaat.Kategorien.Single(k => k.Art == RaumnutzungSchema.ART_DIN_V_18599_10);

        /// <summary>
        /// Wie viel noch fehlt: die Kategorie unter dem Namen 2018 (1), die Kategorie 2025 fehlt (1 und je Nutzung 1) oder
        /// trägt andere Texte (1), je Nutzung der Ausgabe 2025, die nicht genau so steht (1), je ausgelieferte Zeile der
        /// Zuordnung, die noch nach 2018 zählt (1, <see cref="ZuordnungOffen"/>), je Schlüssel der
        /// <see cref="SCHLUESSEL_ZUORDNUNG"/> ohne Zeile (1). −1 ohne die Tabellen von Schritt 189.
        /// </summary>
        public static int Offen()
        {
            if (!Voraussetzungen().All(DataRepository.TabelleVorhanden)) return -1;
            RaumnutzungSaatkategorie din = KategorieDin;
            int offen = ZuordnungOffen((sql, p) => DataRepository.ExecuteScalar(sql, p));
            foreach (RaumnutzungSaatzuordnung z in Zuordnungen)
                if (Zahl(DataRepository.ExecuteScalar(RaumnutzungSchema.SQL_ZAHL_ZUORDNUNG_GESAAT,
                                                      new DbParam("@a", z.Art), new DbParam("@s", z.Schluessel))) == 0)
                    offen++;
            if (Id(DataRepository.ExecuteScalar(SQL_KATEGORIE, new DbParam("@a", din.Art), new DbParam("@b", KATEGORIE_DIN_2018))) != null)
                offen++;
            long? id = Id(DataRepository.ExecuteScalar(SQL_KATEGORIE, new DbParam("@a", din.Art), new DbParam("@b", din.Bezeichner)));
            if (id == null) return offen + 1 + RaumnutzungSaat.Din.Count;
            if (Zahl(DataRepository.ExecuteScalar(SQL_KATEGORIE_TEXTE_GLEICH, new DbParam("@i", id.Value),
                                                  new DbParam("@d", din.Beschreibung), new DbParam("@q", din.Quellenhinweis))) == 0)
                offen++;
            foreach (RaumnutzungSaatprofil p in RaumnutzungSaat.Din)
                if (Zahl(DataRepository.ExecuteScalar(SQL_ZAHL_PROFIL_GENAU, new DbParam("@i", id.Value),
                                                      new DbParam("@n", p.Nummer), new DbParam("@b", p.Bezeichner))) == 0)
                    offen++;
            return offen;
        }

        /// <summary>Steht die Kategorie DIN auf der Ausgabe 2025, zählt die Zuordnung nach 2025, und trägt sie die Schlüssel 19 und 20?</summary>
        public static bool Vollstaendig() => Offen() == 0;

        // =================================================================
        //  Ausführung
        // =================================================================

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang aus — Umbau der Fassung 2018, Zuordnung nach der Zählung 2025, Kategorie und
        /// Nutzungen der Ausgabe 2025, Zuordnung DIN 19 und 20 —
        /// für Migration der Schale, <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>. <b>Wiederholbar.</b>
        /// Fehlt eine Voraussetzung, belegt eine fremde Zeile Name oder Nummer oder fehlt ein ausgeliefertes Muster der
        /// Zuordnung, wirft er benannt und nimmt alles zurück.
        /// </summary>
        /// <param name="bericht">Nimmt Zeilen auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der geänderten oder angelegten Zeilen; 0, wenn alles stand.</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t + " fehlt; Schritt " +
                                                        RaumnutzungSchema.SCHRITT.ToString(CultureInfo.InvariantCulture) +
                                                        " ist nicht gelaufen.");
            if (Vollstaendig())
            {
                bericht?.Add("steht bereits - Kategorie DIN/TS 18599-10 mit 43 Nutzungen, Zuordnung DIN nach der Zaehlung 2025 " +
                             "samt 19 und 20; nichts zu tun");
                return 0;
            }

            int aenderungen = 0;
            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.Vorgang())
            {
                zeilen.AddRange(AlteFassungUmbauen(v, ref aenderungen));
                zeilen.AddRange(ZuordnungUmstellen(v, ref aenderungen));
                zeilen.AddRange(Saat(v, ref aenderungen));
                zeilen.AddRange(ZuordnungErgaenzen(v, ref aenderungen));
                v.Commit();
            }
            if (bericht != null)
                foreach (string z in zeilen) bericht.Add(z);
            bericht?.Add("KEIN Kennwert und keine Zeile des Anwenders geaendert, von der Zuordnung nur ausgelieferte Schluessel " +
                         "umgestellt und Zeilen ergaenzt; der Referenzlauf bleibt byte-gleich");
            return aenderungen;
        }

        /// <summary>
        /// Der Umbau der Fassung 2018 im laufenden Vorgang: Kategorie umbenennen, Texte setzen, die ausgelieferten Profile der
        /// <see cref="SAAT_2018"/> umnummerieren und umbenennen (Ids bleiben). Ohne Kategorie unter dem Namen 2018 tut er
        /// nichts. Auch Schritt 189 ruft ihn vor seiner Saat.
        /// </summary>
        internal static List<string> AlteFassungUmbauen(DbVorgang v, ref int aenderungen)
        {
            var zeilen = new List<string>();
            RaumnutzungSaatkategorie din = KategorieDin;
            long? id = Id(v.Skalar(SQL_KATEGORIE, new DbParam("@a", din.Art), new DbParam("@b", KATEGORIE_DIN_2018)));
            if (id == null) return zeilen;
            if (Zahl(v.Skalar(SQL_KATEGORIE_NAME, new DbParam("@b", din.Bezeichner))) > 0)
                throw new InvalidOperationException("Schemaschritt " + Nr + ": Neben der Kategorie \"" + KATEGORIE_DIN_2018 +
                                                    "\" steht schon eine Kategorie \"" + din.Bezeichner + "\" - der Umbau " +
                                                    "nimmt alles zurueck; bitte die eigene Kategorie umbenennen.");
            aenderungen += v.Ausfuehren(SQL_KATEGORIE_UMBENENNEN, new DbParam("@b", din.Bezeichner), new DbParam("@d", din.Beschreibung),
                                        new DbParam("@q", din.Quellenhinweis), new DbParam("@i", id.Value));
            zeilen.Add("Kategorie \"" + KATEGORIE_DIN_2018 + "\" heisst \"" + din.Bezeichner + "\" (Quellenhinweis DIN/TS 18599-10:2025-10)");

            // Welche ausgelieferten Zeilen gehoeren zur Saat 2018 - und wohin gehen sie?
            var umbau = new List<(long Id, string Nummer, string Name)>();
            DataTable t = v.Lese(SQL_PROFILE_AUSGELIEFERT, new DbParam("@i", id.Value));
            foreach (DataRow r in t.Rows)
            {
                string nummer = Text(r["Nummer"]), name = Text(r["Bezeichner"]);
                if (!SAAT_2018.Any(s => s.Nummer == nummer && s.Name == name)) continue;
                string neu = Nummer2025(nummer);
                string neuName = Name2025(neu);
                if (neu == null || neuName == null) continue;
                umbau.Add((Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture), neu, neuName));
            }

            // Parken (Nummer leer, Hilfsname), dann setzen - Nummer und Name sind je Kategorie eindeutig.
            foreach ((long Id, string Nummer, string Name) u in umbau)
                v.Ausfuehren(SQL_PROFIL_PARKEN, new DbParam("@b", Hilfsname(u.Id)), new DbParam("@i", u.Id));
            foreach ((long Id, string Nummer, string Name) u in umbau)
            {
                if (Zahl(v.Skalar(SQL_PROFIL_BELEGT, new DbParam("@k", id.Value), new DbParam("@i", u.Id),
                                  new DbParam("@b", u.Name), new DbParam("@n", u.Nummer))) > 0)
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": In der Kategorie \"" + din.Bezeichner +
                                                        "\" traegt schon eine andere Zeile den Namen \"" + u.Name +
                                                        "\" oder die Nummer " + u.Nummer + " - der Umbau nimmt alles zurueck.");
                aenderungen += v.Ausfuehren(SQL_PROFIL_SETZEN, new DbParam("@n", u.Nummer), new DbParam("@b", u.Name),
                                            new DbParam("@i", u.Id));
            }
            zeilen.Add(T(umbau.Count) + " ausgelieferte(s) Profil(e) auf Nummer und Namen der Ausgabe 2025 umgestellt (Ids bleiben)");
            return zeilen;
        }

        /// <summary>
        /// Wie viele ausgelieferte Zeilen der Zuordnung noch nach der Zählung 2018 stehen: keine, solange keiner der
        /// <see cref="SCHLUESSEL_NUR_2018"/> ausgeliefert steht (dann ist 31 schon die Bibliothek – Freihandbereich), sonst
        /// jede ausgelieferte Zeile auf einem Schlüssel der <see cref="ZUORDNUNG_2018"/>.
        /// </summary>
        private static int ZuordnungOffen(Func<string, DbParam[], object> skalar)
        {
            long Ausgeliefert(string schluessel)
                => Zahl(skalar(SQL_ZAHL_ZUORDNUNG_AUSGELIEFERT,
                               new[] { new DbParam("@a", RaumnutzungSchema.ZUORDNUNG_DIN), new DbParam("@s", schluessel) }));
            if (SCHLUESSEL_NUR_2018.Sum(Ausgeliefert) == 0) return 0;
            return (int)ZUORDNUNG_2018.Sum(z => Ausgeliefert(z.Schluessel));
        }

        /// <summary>
        /// Die Zuordnung <c>DIN_NUMMER</c> auf die Zählung 2025 im laufenden Vorgang (E96, Anwender 06.10.2026): jede
        /// ausgelieferte Zeile auf einem Schlüssel der <see cref="ZUORDNUNG_2018"/> bekommt den Schlüssel derselben Nutzung in
        /// 2025 (<see cref="Nummer2025"/>); Id und Profil bleiben. Erst parken, dann setzen — der neue Schlüssel 31 ist der alte
        /// der Turnhalle. Trägt eine andere Zeile den neuen Schlüssel, bleibt sie, und die ausgelieferte entfällt benannt.
        /// Zählt die Zuordnung schon nach 2025, tut er nichts. Auch Schritt 189 ruft ihn vor seiner Saat.
        /// </summary>
        internal static List<string> ZuordnungUmstellen(DbVorgang v, ref int aenderungen)
        {
            var zeilen = new List<string>();
            if (ZuordnungOffen((sql, p) => v.Skalar(sql, p)) == 0) return zeilen;

            var umbau = new List<(long Id, string Alt, string Neu)>();
            foreach ((string Schluessel, string Profil) z in ZUORDNUNG_2018)
            {
                long? id = Id(v.Skalar(SQL_ZUORDNUNG_AUSGELIEFERT, new DbParam("@a", RaumnutzungSchema.ZUORDNUNG_DIN),
                                       new DbParam("@s", z.Schluessel)));
                if (id != null) umbau.Add((id.Value, z.Schluessel, Nummer2025(z.Schluessel)));
            }

            // Parken (Hilfsschluessel), dann setzen - (Art, Schluessel) ist eindeutig, und 31 wechselt die Nutzung.
            foreach ((long Id, string Alt, string Neu) u in umbau)
                v.Ausfuehren(SQL_ZUORDNUNG_SCHLUESSEL, new DbParam("@s", Hilfsname(u.Id)), new DbParam("@i", u.Id));
            var umgestellt = new List<string>();
            var entfallen = new List<string>();
            foreach ((long Id, string Alt, string Neu) u in umbau)
            {
                object belegt = v.Skalar(SQL_ZUORDNUNG_BELEGT, new DbParam("@a", RaumnutzungSchema.ZUORDNUNG_DIN),
                                         new DbParam("@i", u.Id), new DbParam("@s", u.Neu));
                if (belegt != null && belegt != DBNull.Value)
                {
                    aenderungen += v.Ausfuehren(SQL_ZUORDNUNG_ENTFERNEN, new DbParam("@i", u.Id));
                    entfallen.Add(u.Alt);
                    zeilen.Add("Zuordnung " + RaumnutzungSchema.ZUORDNUNG_DIN + " " + u.Alt + " (Zaehlung 2018): die ausgelieferte " +
                               "Zeile entfaellt - auf " + u.Neu + " steht schon eine " + (Zahl(belegt) == 0 ? "eigene" : "ausgelieferte") +
                               " Zeile, sie bleibt");
                    continue;
                }
                aenderungen += v.Ausfuehren(SQL_ZUORDNUNG_SCHLUESSEL, new DbParam("@s", u.Neu), new DbParam("@i", u.Id));
                umgestellt.Add(u.Alt + " -> " + u.Neu);
            }
            zeilen.Insert(0, "Zuordnung " + RaumnutzungSchema.ZUORDNUNG_DIN + " auf die Zaehlung 2025 (HottCAD, E96): " +
                             T(umgestellt.Count) + " ausgelieferte Zeile(n) umgestellt" +
                             (umgestellt.Count > 0 ? " (" + string.Join(", ", umgestellt) + ")" : "") + ", " +
                             T(entfallen.Count) + " entfallen");
            return zeilen;
        }

        /// <summary>Kategorie (falls sie fehlt), ihre Texte und die fehlenden Nutzungen der Ausgabe 2025 im laufenden Vorgang.</summary>
        private static List<string> Saat(DbVorgang v, ref int aenderungen)
        {
            RaumnutzungSaatkategorie din = KategorieDin;
            int kategorie = v.Ausfuehren(RaumnutzungSchema.SQL_SAAT_KATALOG,
                new DbParam("@b", din.Bezeichner), new DbParam("@a", din.Art), new DbParam("@d", din.Beschreibung),
                new DbParam("@q", din.Quellenhinweis), new DbParam("@r", din.Reihenfolge), new DbParam("@b2", din.Bezeichner));
            long? id = Id(v.Skalar(SQL_KATEGORIE, new DbParam("@a", din.Art), new DbParam("@b", din.Bezeichner)));
            if (id == null)
                throw new InvalidOperationException("Schemaschritt " + Nr + ": Der Name \"" + din.Bezeichner +
                                                    "\" gehoert einer eigenen Kategorie - die Saat nimmt alles zurueck.");
            int texte = v.Ausfuehren(SQL_KATEGORIE_TEXTE, new DbParam("@d", din.Beschreibung), new DbParam("@q", din.Quellenhinweis),
                                     new DbParam("@i", id.Value), new DbParam("@d2", din.Beschreibung), new DbParam("@q2", din.Quellenhinweis));
            int profile = 0;
            foreach (RaumnutzungSaatprofil p in RaumnutzungSaat.Din)
                profile += v.Ausfuehren(SQL_SAAT_PROFIL, new DbParam("@n", p.Nummer), new DbParam("@b", p.Bezeichner),
                                        new DbParam("@i", id.Value), new DbParam("@b2", p.Bezeichner), new DbParam("@n2", p.Nummer));
            aenderungen += kategorie + texte + profile;
            return new List<string>
            {
                "Saat: " + T(kategorie) + " Kategorie, " + T(texte) + " Textsatz, " + T(profile) +
                " Nutzung(en) der Ausgabe 2025 ohne Werte angelegt",
            };
        }

        /// <summary>
        /// Die Zuordnung DIN 19 und 20 im laufenden Vorgang: je Schlüssel ohne Zeile die ausgelieferte Zeile auf das EPOS-Muster
        /// (<see cref="Zuordnungen"/>); eine Zeile des Anwenders bleibt. Fehlt das Muster, wirft er benannt.
        /// </summary>
        private static List<string> ZuordnungErgaenzen(DbVorgang v, ref int aenderungen)
        {
            int neu = 0;
            foreach (RaumnutzungSaatzuordnung z in Zuordnungen)
            {
                neu += v.Ausfuehren(SQL_SAAT_ZUORDNUNG, new DbParam("@a", z.Art), new DbParam("@s", z.Schluessel),
                                    new DbParam("@k", RaumnutzungSchema.ART_EPOS_MUSTER), new DbParam("@p", z.Profil),
                                    new DbParam("@a2", z.Art), new DbParam("@s2", z.Schluessel));
                if (Zahl(v.Skalar(RaumnutzungSchema.SQL_ZAHL_ZUORDNUNG_GESAAT, new DbParam("@a", z.Art),
                                  new DbParam("@s", z.Schluessel))) == 0)
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Das ausgelieferte Muster \"" + z.Profil +
                                                        "\" der Kategorie \"" + z.Kategorie + "\" fehlt - die Zuordnung " +
                                                        z.Art + " " + z.Schluessel + " entsteht nicht; der Schritt nimmt alles zurueck.");
            }
            aenderungen += neu;
            return new List<string>
            {
                "Zuordnung " + RaumnutzungSchema.ZUORDNUNG_DIN + " " + string.Join(", ", SCHLUESSEL_ZUORDNUNG) + ": " + T(neu) +
                " Zeile(n) auf die EPOS-Muster angelegt, " + T(Zuordnungen.Count - neu) + " stand(en) schon",
            };
        }

        // =================================================================
        //  Kleinkram
        // =================================================================

        /// <summary>Der Hilfsname einer geparkten Zeile — kurz, ohne Leerzeichen am Rand, eindeutig über die Id.</summary>
        internal static string Hilfsname(long id) => "~" + Nr + "~" + id.ToString(CultureInfo.InvariantCulture);

        private static long? Id(object wert)
            => wert == null || wert == DBNull.Value ? null : Convert.ToInt64(wert, CultureInfo.InvariantCulture);

        private static long Zahl(object wert)
            => wert == null || wert == DBNull.Value ? 0 : Convert.ToInt64(wert, CultureInfo.InvariantCulture);

        private static string Text(object wert)
            => wert == null || wert == DBNull.Value ? null : Convert.ToString(wert, CultureInfo.InvariantCulture);

        private static string T(long zahl) => zahl.ToString(CultureInfo.InvariantCulture);

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}

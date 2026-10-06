using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // NP5b - DIE KATEGORIE DIN AUF DIE DIN/TS 18599-10:2025-10 (Schritt 190; Entscheid E94; Konzept
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
    //
    // WAS ER NICHT ANFASST: eigene Kategorien, Profile und Zuordnungen des Anwenders, jedes Profil, das
    // nicht zur Saat 2018 gehoert, die Zuordnung DIN_NUMMER (sie schluesselt die Nummer der
    // HottCAD-Projektdatei, deren Zaehlung kein Beleg im Repositorium entscheidet; Konzept 5.4) und die
    // Profilnamen, die als Kopie an Zone und Kalender stehen (Q41). Trifft der Umbau auf eine fremde Zeile
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
    /// Nummern und Namen der 43 Nutzungen, Ids bleiben (E94). EINE Quelle für Migration, Werkzeug, Testkopie und
    /// Nachweis (ADR-001 Option C). Anlass und Grenzen stehen im Kopf der Datei.
    /// </summary>
    public static class RaumnutzungDinTsSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (angemeldet als 190).
        /// </summary>
        public const int SCHRITT = RaumnutzungSchema.SCHRITT + 1;

        /// <summary>Der Name der Kategorie in der Saat von Schritt 189 vor E94 (Ausgabe 2018).</summary>
        public const string KATEGORIE_DIN_2018 = "DIN V 18599-10";

        /// <summary>
        /// Die 24 Paare Nummer/Name der Saat von Schritt 189 vor E94 (DIN V 18599-10:2018-09) — eingefroren, damit
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
        /// trägt andere Texte (1), je Nutzung der Ausgabe 2025, die nicht genau so steht (1). −1 ohne die Tabellen von
        /// Schritt 189.
        /// </summary>
        public static int Offen()
        {
            if (!Voraussetzungen().All(DataRepository.TabelleVorhanden)) return -1;
            RaumnutzungSaatkategorie din = KategorieDin;
            int offen = 0;
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

        /// <summary>Steht die Kategorie DIN auf der Ausgabe 2025?</summary>
        public static bool Vollstaendig() => Offen() == 0;

        // =================================================================
        //  Ausführung
        // =================================================================

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang aus — Umbau der Fassung 2018, Kategorie und Nutzungen der Ausgabe 2025 —
        /// für Migration der Schale, <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>. <b>Wiederholbar.</b>
        /// Fehlt eine Voraussetzung oder belegt eine fremde Zeile Name oder Nummer, wirft er benannt und nimmt alles zurück.
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
                bericht?.Add("steht bereits - Kategorie DIN/TS 18599-10 mit 43 Nutzungen; nichts zu tun");
                return 0;
            }

            int aenderungen = 0;
            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.Vorgang())
            {
                zeilen.AddRange(AlteFassungUmbauen(v, ref aenderungen));
                zeilen.AddRange(Saat(v, ref aenderungen));
                v.Commit();
            }
            if (bericht != null)
                foreach (string z in zeilen) bericht.Add(z);
            bericht?.Add("KEIN Kennwert, keine Zuordnung und keine Zeile des Anwenders geaendert; der Referenzlauf bleibt byte-gleich");
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

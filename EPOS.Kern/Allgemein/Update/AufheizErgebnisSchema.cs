using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KP-S3 - DIE ERGEBNISSPALTEN DER AUFHEIZOPTIMIERUNG (Entwurf KP3 Abschnitt 4,
    // Teilkonzept 4.8; Festlegungen 22, 23, 25, 26).
    //
    // WOZU. Die Aufheizrechnung legt je Gebaeude und je Zone ihr Ergebnis ab: Zustand,
    // Bemessung (nur am Gebaeude), t_auf,max, T_a,B, P_auf und Quelle, die Tageszaehler W1 bis
    // W3, Rampenstunden, laengste Rampe, Spruenge aus „aus" (W4), die Kappungsstunden
    // HeizleistungMax_H und an der Zone die Sommerlueftungsstunden (E54 je Zone). Je Tabelle
    // VIERZEHN Spalten:
    //
    //   Tab_ErgebnisGebaeude  26 -> 40   (ohne UNBEHEIZT, mit GEMISCHT und Aufheiz_Bemessung)
    //   Tab_ErgebnisZone      15 -> 29   (ohne GEKOPPELT und GEMISCHT, mit Sommerlueftungsstunden_H)
    //
    // ALLE NULLBAR. NULL heisst „Schalter aus" oder Tagesbilanz-Weg (Muster E30, Grundsatz 4);
    // Temperaturen ohne Bereichspruefung wie MittlereRaumtemperatur_C, Stunden mit BETWEEN 0 AND
    // 8760 wie Ueberhitzungsstunden_H, Tage mit BETWEEN 0 AND 365, Zeiten mit BETWEEN 0 AND 47;
    // HeizleistungMax_H ist eine Summe von Zeitanteilen und deshalb REAL (wie UebergabeBegrenzt_H).
    //
    // KEIN DML, KEIN TABELLENNEUBAU. Jede vorhandene Ergebniszeile ist eine Zeile ohne
    // Aufheizrechnung. Die Ergebnistabellen tragen keinen Kostenstempel (Schritt 159).
    //
    // DER NAME STEHT NUR ALS ARGUMENT (Muster KesselKennlinieSchema).
    //
    // DREI LESER: der Schemaschritt in WindowsFormsApplication1/Allgemein/Update/
    // SchemaMigration.cs, das Werkzeug Werkzeuge/Testdatenbankschema und die Testvorrichtung
    // samt Nachweis in EPOS.Kern.Tests; dazu ErgebnisCtrl (Schreiben und Lesen nach Vorhandensein).
    // ====================================================================================

    /// <summary>
    /// <b>KP-S3</b> — die vierzehn Ergebnisspalten der Aufheizoptimierung je Gebäude- und Zonentabelle
    /// (Entwurf KP3 Abschnitt 4) — EINE Quelle für Migration, Werkzeug, Testkopie, Controller und
    /// Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class AufheizErgebnisSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht; vergeben mit
        /// <see cref="AufheizvorgabeSchema.SCHRITT"/> unmittelbar vor dem Schemacommit gegen <c>origin</c>
        /// (Festlegung 23: zwei Schritte statt einem).
        /// </summary>
        public const int SCHRITT = AufheizvorgabeSchema.SCHRITT + 1;

        /// <summary>Die Gebäudetabelle des Ergebnisses.</summary>
        public const string TAB_GEBAEUDE = ErgebnisGebaeudeSchema.TAB;

        /// <summary>Die Zonentabelle des Ergebnisses.</summary>
        public const string TAB_ZONE = ZonenkopplungSchema.TAB_ERGEBNIS;

        /// <summary>Die beiden Tabellen, in dieser Reihenfolge.</summary>
        public static readonly string[] TABELLEN = { TAB_GEBAEUDE, TAB_ZONE };

        /// <summary><c>Aufheiz_Zustand</c>: Festlegung 25; NULL = Schalter aus oder Tagesbilanz-Weg.</summary>
        public const string SPALTE_ZUSTAND = "Aufheiz_Zustand";

        /// <summary><c>Aufheiz_Bemessung</c> (nur Gebäude): die Variante des Laufs — der Bericht liest nicht <c>Tab_Einstellungen</c>.</summary>
        public const string SPALTE_BEMESSUNG = "Aufheiz_Bemessung";

        /// <summary><c>Aufheizzeit_Max_H</c> [h]: t_auf,max, 0 bis 47; NULL bei UNERREICHBAR.</summary>
        public const string SPALTE_ZEIT_MAX = "Aufheizzeit_Max_H";

        /// <summary><c>Aufheiz_Aussen_C</c> [°C]: T_a,B — die kälteste Stunde, bei (b) abzüglich ΔT_K.</summary>
        public const string SPALTE_AUSSEN = "Aufheiz_Aussen_C";

        /// <summary><c>Aufheiz_Leistung_Kw</c> [kW]: P_auf, skaliert wie die Spitzen; größer 0.</summary>
        public const string SPALTE_LEISTUNG = "Aufheiz_Leistung_Kw";

        /// <summary><c>Aufheiz_Leistungsquelle</c>: Grenze, Ziel, am Gebäude auch gemischt.</summary>
        public const string SPALTE_QUELLE = "Aufheiz_Leistungsquelle";

        /// <summary><c>Aufheiztage</c>: Tage mit n &gt; 1.</summary>
        public const string SPALTE_TAGE = "Aufheiztage";

        /// <summary><c>Aufheiztage_Begrenzt</c>: Tage mit n − 1 = D bei größerem Bedarf (W2).</summary>
        public const string SPALTE_TAGE_BEGRENZT = "Aufheiztage_Begrenzt";

        /// <summary><c>Aufheiztage_Unerreichbar</c>: Tage ohne erreichbares n ≤ 48 (W1 je Tag).</summary>
        public const string SPALTE_TAGE_UNERREICHBAR = "Aufheiztage_Unerreichbar";

        /// <summary><c>Aufheiztage_Nachweisband</c>: Tage, an denen der Lauf über dem Band lag (W3).</summary>
        public const string SPALTE_TAGE_NACHWEISBAND = "Aufheiztage_Nachweisband";

        /// <summary><c>Aufheizstunden_H</c> [h]: Σ (n − 1).</summary>
        public const string SPALTE_STUNDEN = "Aufheizstunden_H";

        /// <summary><c>Aufheizzeit_Laengste_H</c> [h]: das größte n − 1, 0 bis 47.</summary>
        public const string SPALTE_ZEIT_LAENGSTE = "Aufheizzeit_Laengste_H";

        /// <summary><c>Aufheizspruenge_Aus</c>: Sprünge aus „aus" ohne Rampe (W4), darunter der Beginn der Heizperiode.</summary>
        public const string SPALTE_SPRUENGE_AUS = "Aufheizspruenge_Aus";

        /// <summary><c>HeizleistungMax_H</c> [h]: Σ der Kappungsanteile, auch ohne Kopplung (B22) — REAL.</summary>
        public const string SPALTE_HEIZLEISTUNG_MAX = "HeizleistungMax_H";

        /// <summary><c>Sommerlueftungsstunden_H</c> [h] (nur Zone): E54 je Zone; am Gebäude steht die Spalte seit Schritt 107.</summary>
        public const string SPALTE_SOMMERLUEFTUNG = "Sommerlueftungsstunden_H";

        /// <summary>Spaltenzahl von <c>Tab_ErgebnisGebaeude</c> nach diesem Schritt (26 + 14; B24).</summary>
        public const int SPALTENZAHL_ERGEBNIS_GEBAEUDE = KonditionierungVorlagenSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE + 14;

        /// <summary>Spaltenzahl von <c>Tab_ErgebnisZone</c> nach diesem Schritt (15 + 14; B24).</summary>
        public const int SPALTENZAHL_ERGEBNIS_ZONE = KonditionierungVorlagenSchema.SPALTENZAHL_ERGEBNIS_ZONE + 14;

        /// <summary>Die vierzehn Spalten von <c>Tab_ErgebnisGebaeude</c> in Anlegereihenfolge, mit Typ und Prüfklausel.</summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string>> SpaltenGebaeude = new[]
        {
            Text(SPALTE_ZUSTAND, DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR,
                 DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT),
            Text(SPALTE_BEMESSUNG, DbWerte.AUFHEIZ_BEMESSUNGEN.ToArray()),
            Ganz(SPALTE_ZEIT_MAX, 47),
            Spalte(SPALTE_AUSSEN, "REAL"),
            Spalte(SPALTE_LEISTUNG, "REAL CHECK (\"" + SPALTE_LEISTUNG + "\" > 0)"),
            Text(SPALTE_QUELLE, DbWerte.AUFHEIZ_QUELLE_GRENZE, DbWerte.AUFHEIZ_QUELLE_ZIEL, DbWerte.AUFHEIZ_QUELLE_GEMISCHT),
            Ganz(SPALTE_TAGE, 365),
            Ganz(SPALTE_TAGE_BEGRENZT, 365),
            Ganz(SPALTE_TAGE_UNERREICHBAR, 365),
            Ganz(SPALTE_TAGE_NACHWEISBAND, 365),
            Ganz(SPALTE_STUNDEN, 8760),
            Ganz(SPALTE_ZEIT_LAENGSTE, 47),
            Ganz(SPALTE_SPRUENGE_AUS, 8760),
            Spalte(SPALTE_HEIZLEISTUNG_MAX, "REAL CHECK (\"" + SPALTE_HEIZLEISTUNG_MAX + "\" BETWEEN 0 AND 8760)"),
        };

        /// <summary>Die vierzehn Spalten von <c>Tab_ErgebnisZone</c> in Anlegereihenfolge, mit Typ und Prüfklausel.</summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string>> SpaltenZone = new[]
        {
            Text(SPALTE_ZUSTAND, DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR,
                 DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT),
            Ganz(SPALTE_ZEIT_MAX, 47),
            Spalte(SPALTE_AUSSEN, "REAL"),
            Spalte(SPALTE_LEISTUNG, "REAL CHECK (\"" + SPALTE_LEISTUNG + "\" > 0)"),
            Text(SPALTE_QUELLE, DbWerte.AUFHEIZ_QUELLE_GRENZE, DbWerte.AUFHEIZ_QUELLE_ZIEL),
            Ganz(SPALTE_TAGE, 365),
            Ganz(SPALTE_TAGE_BEGRENZT, 365),
            Ganz(SPALTE_TAGE_UNERREICHBAR, 365),
            Ganz(SPALTE_TAGE_NACHWEISBAND, 365),
            Ganz(SPALTE_STUNDEN, 8760),
            Ganz(SPALTE_ZEIT_LAENGSTE, 47),
            Ganz(SPALTE_SPRUENGE_AUS, 8760),
            Spalte(SPALTE_HEIZLEISTUNG_MAX, "REAL CHECK (\"" + SPALTE_HEIZLEISTUNG_MAX + "\" BETWEEN 0 AND 8760)"),
            Ganz(SPALTE_SOMMERLUEFTUNG, 8760),
        };

        /// <summary>Die Spalten einer der beiden Tabellen.</summary>
        public static IReadOnlyList<KeyValuePair<string, string>> Spalten(string tabelle)
            => tabelle == TAB_ZONE ? SpaltenZone : SpaltenGebaeude;

        /// <summary>Stehen alle 28 Spalten (vierzehn je Tabelle)? Dann ist der Schritt gelaufen.</summary>
        public static bool Vollstaendig()
        {
            foreach (string tabelle in TABELLEN)
                foreach (KeyValuePair<string, string> s in Spalten(tabelle))
                    if (!DataRepository.SpalteVorhanden(tabelle, s.Key)) return false;
            return true;
        }

        /// <summary>
        /// Die Anweisungen des Schritts — Beschreibung und SQL, je fehlende Spalte eine; leer, wenn alles
        /// steht (<b>wiederholbar</b>). Fehlt eine Tabelle ganz, scheitert das <c>ALTER TABLE</c> benannt.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                foreach (string tabelle in TABELLEN)
                    foreach (KeyValuePair<string, string> s in Spalten(tabelle))
                    {
                        if (DataRepository.SpalteVorhanden(tabelle, s.Key)) continue;
                        yield return new KeyValuePair<string, string>(
                            tabelle + "." + s.Key + " anlegen",
                            "ALTER TABLE \"" + tabelle + "\" ADD COLUMN \"" + s.Key + "\" " + s.Value);
                    }
            }
        }

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang aus — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>; die Migration der Schale geht denselben Weg über ihre eigenen Helfer,
        /// aus derselben <see cref="Anweisungen"/>. <b>Kein DML.</b>
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (0 bis 28).</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            // Die Auskunft VOR dem Vorgang (eigene Verbindung, siehe AufheizvorgabeSchema).
            List<KeyValuePair<string, string>> offen = Anweisungen.ToList();
            if (offen.Count == 0)
            {
                bericht?.Add(TAB_GEBAEUDE + " und " + TAB_ZONE + ": Aufheizspalten vorhanden");
                return 0;
            }
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (KeyValuePair<string, string> a in offen) v.Ausfuehren(a.Value);
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            foreach (KeyValuePair<string, string> a in offen) bericht?.Add(a.Key);
            return offen.Count;
        }

        private static KeyValuePair<string, string> Spalte(string name, string typ)
            => new KeyValuePair<string, string>(name, typ);

        private static KeyValuePair<string, string> Text(string name, params string[] werte)
            => Spalte(name, "TEXT CHECK (\"" + name + "\" IN (" + AufheizvorgabeSchema.Liste(werte) + "))");

        private static KeyValuePair<string, string> Ganz(string name, int hoechstens)
            => Spalte(name, "INTEGER CHECK (\"" + name + "\" BETWEEN 0 AND " +
                            hoechstens.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")");
    }
}

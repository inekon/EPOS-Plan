using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // K-F1 - DAS RUECKKUEHLWERK ALS EIGENES GLIED: SCHEMA UND KATALOG (S-KF1; Entwurf Split/VRF/Rueckkuehlwerk
    // Abschnitt 5.2, 5.8 und 9; Entscheid E120, Fragen KD-Q8 und KD-Q9 nach Empfehlung).
    //
    // WAS.
    //   Tab_Rueckkuehlwerk_STAMM     Katalog der Auslieferung (ReadOnly, Katalogspalten, Kostenvorlagen)
    //   Tab_Rueckkuehlwerk           Projektkopie: ID_Projekt (Kaskade), ID_Stamm (SET NULL)
    //   Tab_Energieanlagen           ID_Rueckkuehlwerk (Verweis auf die PROJEKTKOPIE, SET NULL) und
    //                                Wasserpreis_EUR_m3 (Frage KD-Q8: Preis optional an der Anlage, leer = keine Kosten)
    //   Tab_ErgebnisKaeltemaschine   sechs Kennzahlen der Rueckkuehlung (Entwurf 5.8), leer bis K-F2/K-F3 sie fuellen
    //
    // Das Rueckkuehlwerk ist KEIN Erzeuger der Anlagenliste und hat KEINEN Platz in der Kaeltefolge (K8, fortgeschrieben):
    // Es wird an der Anlagenzeile einer Kaeltemaschine gewaehlt. Alle Fachspalten sind nullbar - leer heisst Vorgabe, und
    // die Vorgaben sind die Festwerte der passenden Rueckkuehlart (KaelteFestwerte).
    //
    // KEINE SAAT. Der Startkatalog ist nicht Teil von K-F1; die Katalogtabelle entsteht leer.
    //
    // ERGEBNISNEUTRAL. Kein Rechenweg liest die Tabellen oder Spalten (die Rechnung baut K-F1-b); jede Bestandszeile steht
    // danach auf NULL. Alles in EINEM Vorgang mit abgeschalteten Fremdschluesseln; der Schritt ist wiederholbar.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in EPOS.Kern.Tests und die
    // Paketanhebung (Art Katalog).
    // ====================================================================================

    /// <summary>
    /// <b>K-F1</b> — das Rückkühlwerk als eigenes Glied: Katalog, Projektkopie, Verweis an der Anlagenzeile der Kältemaschine,
    /// Wasserpreis und Ergebnisspalten. EINE Quelle für Migration, Werkzeug, Testkopie und Nachweis (ADR-001 Option C).
    /// Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class RueckkuehlwerkSchema
    {
        /// <summary><b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (214).</summary>
        // Hängt an Schritt 213 (K1, KaeltebedarfSchema).
        public const int SCHRITT = KaeltebedarfSchema.SCHRITT + 1;

        /// <summary>Der Katalog.</summary>
        public const string TAB_STAMM = "Tab_Rueckkuehlwerk_STAMM";

        /// <summary>Die Projektkopie.</summary>
        public const string TAB_PROJEKT = "Tab_Rueckkuehlwerk";

        /// <summary>Die Anlagentabelle des Projekts.</summary>
        public const string TAB_ANLAGEN = KaeltemaschineAnlageSchema.TAB_ANLAGEN;

        /// <summary>Das Ergebnis je Kältemaschine.</summary>
        public const string TAB_ERGEBNIS = KaeltemaschineAnlageSchema.TAB_ERGEBNIS;

        // ---- Verweise ----

        /// <summary>Verweis der Anlagenzeile auf die Projektkopie des Rückkühlwerks.</summary>
        public const string SPALTE_ID_RUECKKUEHLWERK = "ID_Rueckkuehlwerk";

        /// <summary>Verweis der Projektkopie auf ihren Katalogsatz.</summary>
        public const string SPALTE_ID_STAMM = "ID_Stamm";

        /// <summary>Wasserpreis an der Anlagenzeile [€/m³]; leer = keine Wasserkosten (Frage KD-Q8).</summary>
        public const string SPALTE_WASSERPREIS = "Wasserpreis_EUR_m3";

        // ---- Fachspalten (Katalog und Projektkopie) ----

        public const string SPALTE_BEZEICHNER = "Bezeichner";
        public const string SPALTE_BESCHREIBUNG = "Beschreibung";
        public const string SPALTE_BAUART = "Bauart";
        public const string SPALTE_NENNLEISTUNG = "Nennleistung_kW";
        public const string SPALTE_ANNAEHERUNG_NENN = "Annaeherung_Nenn_K";
        public const string SPALTE_ANNAEHERUNG_WEG = "Annaeherung_Weg";
        public const string SPALTE_VENTILATOR_NENN = "Ventilator_Nenn_kW";
        public const string SPALTE_VENTILATOR_REGELUNG = "Ventilator_Regelung";
        public const string SPALTE_VENTILATOR_STUFEN = "Ventilator_Stufen";
        public const string SPALTE_VENTILATOR_DREHZAHL_MIN = "Ventilator_Drehzahl_Min";
        public const string SPALTE_BEFEUCHTUNG_WIRKUNGSGRAD = "Befeuchtung_Wirkungsgrad";
        public const string SPALTE_BEFEUCHTUNG_AB = "Befeuchtung_Ab_C";
        public const string SPALTE_VERDUNSTUNG_FAKTOR = "Verdunstung_Faktor";
        public const string SPALTE_EINDICKUNG = "Eindickung";
        public const string SPALTE_DRIFT_ANTEIL = "Drift_Anteil";
        public const string SPALTE_FREIKUEHLUNG_SCHALTUNG = "Freikuehlung_Schaltung";
        public const string SPALTE_MODULKOSTEN = "Modulkosten";

        // ---- Persistenzwerte (deutsch, eingefroren) ----

        public const string BAUART_TROCKEN = "TROCKEN";
        public const string BAUART_ADIABAT = "ADIABAT";
        public const string BAUART_HYBRID = "HYBRID";
        public const string BAUART_KUEHLTURM_OFFEN = "KUEHLTURM_OFFEN";
        public const string BAUART_KUEHLTURM_GESCHLOSSEN = "KUEHLTURM_GESCHLOSSEN";

        /// <summary>Die Bauarten in fester Folge.</summary>
        public static readonly IReadOnlyList<string> BAUARTEN = new[]
        {
            BAUART_TROCKEN, BAUART_ADIABAT, BAUART_HYBRID, BAUART_KUEHLTURM_OFFEN, BAUART_KUEHLTURM_GESCHLOSSEN
        };

        public const string WEG_FEST = "FEST";
        public const string WEG_LASTABHAENGIG = "LASTABHAENGIG";

        /// <summary>Die Wege der Annäherung; leer = <see cref="WEG_FEST"/>.</summary>
        public static readonly IReadOnlyList<string> ANNAEHERUNG_WEGE = new[] { WEG_FEST, WEG_LASTABHAENGIG };

        public const string REGELUNG_EIN_AUS = "EIN_AUS";
        public const string REGELUNG_STUFEN = "STUFEN";
        public const string REGELUNG_DREHZAHL = "DREHZAHL";

        /// <summary>Die Regelungen des Ventilators.</summary>
        public static readonly IReadOnlyList<string> VENTILATOR_REGELUNGEN = new[] { REGELUNG_EIN_AUS, REGELUNG_STUFEN, REGELUNG_DREHZAHL };

        public const string SCHALTUNG_PARALLEL = "PARALLEL";
        public const string SCHALTUNG_REIHE = "REIHE";

        /// <summary>Die Schaltungen der freien Kühlung; leer = <see cref="SCHALTUNG_PARALLEL"/> (Frage KD-Q9).</summary>
        public static readonly IReadOnlyList<string> FREIKUEHLUNG_SCHALTUNGEN = new[] { SCHALTUNG_PARALLEL, SCHALTUNG_REIHE };

        // ---- Eingabegrenzen (Hauswerte; sie stehen im CHECK und in RueckkuehlwerkStammCtrl.Pruefen) ----

        /// <summary>Höchste Annäherung im Nennpunkt [K].</summary>
        public const double ANNAEHERUNG_MAX_K = 30;

        /// <summary>Bereich der Schaltgrenze der Befeuchtung [°C].</summary>
        public const double BEFEUCHTUNG_AB_MIN_C = -30, BEFEUCHTUNG_AB_MAX_C = 50;

        /// <summary>Höchster Verdunstungsfaktor (Vielfaches der rechnerischen Verdunstung).</summary>
        public const double VERDUNSTUNG_FAKTOR_MAX = 3;

        /// <summary>Höchste Eindickung.</summary>
        public const double EINDICKUNG_MAX = 20;

        // ---- Ergebnisspalten (Entwurf 5.8) ----

        public const string SPALTE_VENTILATORSTROM = "Ventilatorstrom_MWh";
        public const string SPALTE_WASSER = "Wasser_m3";
        public const string SPALTE_STUNDEN_NASS = "Stunden_Nass";
        public const string SPALTE_TEILFREIKUEHLUNG = "TeilFreikuehlung_MWh";
        public const string SPALTE_TEILFREIKUEHLUNG_STUNDEN = "TeilFreikuehlung_Stunden";
        public const string SPALTE_RUECKKUEHLTEMPERATUR_MITTEL = "Rueckkuehltemperatur_Mittel";

        private static string Q(string s) => "\"" + s + "\"";

        private static string Liste(IEnumerable<string> werte) => string.Join(",", werte.Select(w => "'" + w + "'"));

        private static string Text(string s, IEnumerable<string> werte)
            => "TEXT CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " IN (" + Liste(werte) + "))";

        private static string Bereich(string s, double von, double bis)
            => "REAL CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " BETWEEN " + Zahl(von) + " AND " + Zahl(bis) + ")";

        private static string Groesser(string s, double grenze)
            => "REAL CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " > " + Zahl(grenze) + ")";

        private static string NichtNegativ(string s) => "REAL CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " >= 0)";

        private static string Zahl(double d) => d.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Die Fachspalten in Tabellenfolge — Name und Typ samt Klausel; dieselben an Katalog und Projektkopie. Alle nullbar
        /// außer dem Bezeichner: leer heißt Vorgabe.
        /// </summary>
        public static readonly IReadOnlyList<(string Spalte, string Typ)> FACH_SPALTEN = new[]
        {
            (SPALTE_BEZEICHNER, "TEXT NOT NULL CHECK (length(" + Q(SPALTE_BEZEICHNER) + ") BETWEEN 1 AND 255)"),
            (SPALTE_BESCHREIBUNG, "TEXT"),
            (SPALTE_BAUART, Text(SPALTE_BAUART, BAUARTEN)),
            (SPALTE_NENNLEISTUNG, Groesser(SPALTE_NENNLEISTUNG, 0)),
            (SPALTE_ANNAEHERUNG_NENN, Bereich(SPALTE_ANNAEHERUNG_NENN, 0, ANNAEHERUNG_MAX_K)),
            (SPALTE_ANNAEHERUNG_WEG, Text(SPALTE_ANNAEHERUNG_WEG, ANNAEHERUNG_WEGE)),
            (SPALTE_VENTILATOR_NENN, NichtNegativ(SPALTE_VENTILATOR_NENN)),
            (SPALTE_VENTILATOR_REGELUNG, Text(SPALTE_VENTILATOR_REGELUNG, VENTILATOR_REGELUNGEN)),
            (SPALTE_VENTILATOR_STUFEN, "INTEGER CHECK (" + Q(SPALTE_VENTILATOR_STUFEN) + " IS NULL OR " + Q(SPALTE_VENTILATOR_STUFEN) + " >= 2)"),
            (SPALTE_VENTILATOR_DREHZAHL_MIN, Bereich(SPALTE_VENTILATOR_DREHZAHL_MIN, 0, 1)),
            (SPALTE_BEFEUCHTUNG_WIRKUNGSGRAD, Bereich(SPALTE_BEFEUCHTUNG_WIRKUNGSGRAD, 0, 1)),
            (SPALTE_BEFEUCHTUNG_AB, Bereich(SPALTE_BEFEUCHTUNG_AB, BEFEUCHTUNG_AB_MIN_C, BEFEUCHTUNG_AB_MAX_C)),
            (SPALTE_VERDUNSTUNG_FAKTOR, "REAL CHECK (" + Q(SPALTE_VERDUNSTUNG_FAKTOR) + " IS NULL OR (" + Q(SPALTE_VERDUNSTUNG_FAKTOR) +
                                        " > 0 AND " + Q(SPALTE_VERDUNSTUNG_FAKTOR) + " <= " + Zahl(VERDUNSTUNG_FAKTOR_MAX) + "))"),
            (SPALTE_EINDICKUNG, "REAL CHECK (" + Q(SPALTE_EINDICKUNG) + " IS NULL OR (" + Q(SPALTE_EINDICKUNG) + " > 1 AND " +
                                Q(SPALTE_EINDICKUNG) + " <= " + Zahl(EINDICKUNG_MAX) + "))"),
            (SPALTE_DRIFT_ANTEIL, Bereich(SPALTE_DRIFT_ANTEIL, 0, 1)),
            (SPALTE_FREIKUEHLUNG_SCHALTUNG, Text(SPALTE_FREIKUEHLUNG_SCHALTUNG, FREIKUEHLUNG_SCHALTUNGEN)),
            (SPALTE_MODULKOSTEN, NichtNegativ(SPALTE_MODULKOSTEN)),
        };

        /// <summary>Die Namen der Fachspalten in Tabellenfolge (Register der Katalogfassung, Schreibweg).</summary>
        public static readonly string[] Fachspalten = FACH_SPALTEN.Select(s => s.Spalte).ToArray();

        /// <summary>Die Spalten an der Anlagenzeile: Verweis und Wasserpreis.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN_ANLAGE = new[]
        {
            (TAB_ANLAGEN, SPALTE_ID_RUECKKUEHLWERK, "INTEGER REFERENCES " + Q(TAB_PROJEKT) + " (\"ID\") ON DELETE SET NULL"),
            (TAB_ANLAGEN, SPALTE_WASSERPREIS, NichtNegativ(SPALTE_WASSERPREIS)),
        };

        /// <summary>Die Kennzahlen der Rückkühlung am Ergebnis der Kältemaschine (Entwurf 5.8).</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN_ERGEBNIS = new[]
        {
            (TAB_ERGEBNIS, SPALTE_VENTILATORSTROM, NichtNegativ(SPALTE_VENTILATORSTROM)),
            (TAB_ERGEBNIS, SPALTE_WASSER, NichtNegativ(SPALTE_WASSER)),
            (TAB_ERGEBNIS, SPALTE_STUNDEN_NASS, AnlagenfahrplanSchema.Stunden(SPALTE_STUNDEN_NASS)),
            (TAB_ERGEBNIS, SPALTE_TEILFREIKUEHLUNG, NichtNegativ(SPALTE_TEILFREIKUEHLUNG)),
            (TAB_ERGEBNIS, SPALTE_TEILFREIKUEHLUNG_STUNDEN, AnlagenfahrplanSchema.Stunden(SPALTE_TEILFREIKUEHLUNG_STUNDEN)),
            (TAB_ERGEBNIS, SPALTE_RUECKKUEHLTEMPERATUR_MITTEL, "REAL"),
        };

        /// <summary>Die Spalten, die der Schritt an bestehende Tabellen anhängt.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN =
            SPALTEN_ANLAGE.Concat(SPALTEN_ERGEBNIS).ToArray();

        private static string Fachspaltentext() =>
            string.Join(",\n", FACH_SPALTEN.Select(s => "    " + Q(s.Spalte) + " " + s.Typ));

        /// <summary>Der Katalog: Fachspalten, ReadOnly, die drei Katalogspalten und die zwei Kostenvorlagen (Muster Kältemaschine).</summary>
        public static string SqlCreateStamm() =>
            "CREATE TABLE IF NOT EXISTS " + Q(TAB_STAMM) + " (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            Fachspaltentext() + ",\n" +
            "    \"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1)),\n" +
            string.Join(",\n", KatalogfassungSchema.Katalogspalten().Select(k => "    " + Q(k.Key) + " " + k.Value)) + ",\n" +
            "    " + Q(KatalogkostenUrsprungSchema.SPALTE_ID_KOSTENVORLAGE) + " INTEGER REFERENCES \"Tab_KostenVorlage\" (\"ID\") ON DELETE SET NULL,\n" +
            "    " + Q(KatalogkostenInvestitionSchema.SPALTE_ID_KOSTENVORLAGE_INVESTITION) + " INTEGER REFERENCES \"Tab_KostenVorlage\" (\"ID\") ON DELETE SET NULL\n" +
            ") STRICT";

        /// <summary>Die Projektkopie: ID_Projekt (Kaskade), ID_Stamm (SET NULL), Fachspalten.</summary>
        public static string SqlCreateProjekt() =>
            "CREATE TABLE IF NOT EXISTS " + Q(TAB_PROJEKT) + " (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Projekt\" INTEGER NOT NULL REFERENCES \"Tab_Projekt\" (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE,\n" +
            "    " + Q(SPALTE_ID_STAMM) + " INTEGER REFERENCES " + Q(TAB_STAMM) + " (\"ID\") ON DELETE SET NULL,\n" +
            Fachspaltentext() + "\n" +
            ") STRICT";

        /// <summary>Die beiden Tabellen in Anlegefolge (Katalog vor Projektkopie).</summary>
        public static IReadOnlyList<KeyValuePair<string, string>> Tabellen() => new[]
        {
            new KeyValuePair<string, string>(TAB_STAMM, SqlCreateStamm()),
            new KeyValuePair<string, string>(TAB_PROJEKT, SqlCreateProjekt()),
        };

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() =>
            new[] { "Tab_Projekt", "Tab_KostenVorlage", TAB_ANLAGEN, TAB_ERGEBNIS, Katalogfassung.TAB_APPLIKATION };

        /// <summary>Die Anweisung, die eine Spalte anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE " + Q(s.Tabelle) + " ADD COLUMN " + Q(s.Spalte) + " " + s.Typ;

        /// <summary>Steht der Schritt — Tabellen, Spalten, Katalogindex? (Wächter der Leser, etwa für einen älteren Stand auf iOS.)</summary>
        public static bool Vollstaendig() =>
            Tabellen().All(t => DataRepository.TabelleVorhanden(t.Key))
            && SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte))
            && KatalogfassungSchema.KatalogspaltenVollstaendig(Katalogfassung.Stufe5);

        /// <summary>Steht der Verweis an der Anlagenzeile? (Leser der Anlagenzeile auf einem älteren Stand.)</summary>
        public static bool AnlagenspaltenVorhanden() =>
            SPALTEN_ANLAGE.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte))
            && DataRepository.TabelleVorhanden(TAB_PROJEKT);

        /// <summary>Wie viele Anlagenzeilen tragen einen Verweis oder Wasserpreis? (Nachweis: nach dem Schritt 0.)</summary>
        public static int ZeilenMitWert()
        {
            if (!AnlagenspaltenVorhanden()) return 0;
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + Q(TAB_ANLAGEN) + " WHERE " + Q(SPALTE_ID_RUECKKUEHLWERK) + " IS NOT NULL OR " +
                Q(SPALTE_WASSERPREIS) + " IS NOT NULL");
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Führt den Schritt aus (wiederholbar); Rückgabe = Zahl der angelegten Tabellen, Spalten und Indizes. Fehlt eine
        /// vorausgesetzte Tabelle, wirft er benannt.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            // Die Auskunft VOR dem Vorgang - TabelleVorhanden/SpalteVorhanden arbeiten auf einer eigenen Verbindung.
            var tabellen = Tabellen().Where(t => !DataRepository.TabelleVorhanden(t.Key)).ToList();
            var offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();

            int n = 0;
            var zeilen = new List<string>();
            if (tabellen.Count > 0 || offen.Count > 0)
            {
                using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
                {
                    try
                    {
                        foreach (KeyValuePair<string, string> t in tabellen)
                        {
                            v.Ausfuehren(t.Value);
                            zeilen.Add(t.Key + " angelegt (leer, keine Saat)");
                            n++;
                        }
                        foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                        {
                            v.Ausfuehren(Anlegen(s));
                            zeilen.Add(s.Tabelle + "." + s.Spalte + " angelegt (leer)");
                            n++;
                        }
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
            }
            // Der Katalogindex (und Katalogspalten, falls eine Fassung ohne sie die Tabelle angelegt haette).
            foreach (KeyValuePair<string, string> a in KatalogfassungSchema.KatalogspaltenAnweisungen(Katalogfassung.Stufe5).ToList())
            {
                DataRepository.ExecuteNonQuery(a.Value);
                zeilen.Add(a.Key);
                n++;
            }
            if (bericht != null)
            {
                if (n == 0) bericht.Add("steht bereits - Rueckkuehlwerk (Katalog, Projektkopie, Anlagenverweis, Ergebnis); nichts zu tun");
                foreach (string z in zeilen) bericht.Add(z);
                bericht.Add("KEIN DML an Bestandsdaten, der Referenzlauf bleibt byte-gleich");
            }
            return n;
        }

        /// <summary>
        /// Trägt nach einem Paketimport den Katalogverweis der Projektkopien über den Bezeichner nach — nur bei genau einem
        /// Treffer (Muster <see cref="KaeltemaschineSchema.SqlNachtragProjekt"/>).
        /// </summary>
        public static string SqlNachtragProjekt() =>
            "UPDATE " + Q(TAB_PROJEKT) + " SET " + Q(SPALTE_ID_STAMM) + " = (SELECT s.\"ID\" FROM " + Q(TAB_STAMM) + " s " +
            "WHERE s.\"Bezeichner\" = " + Q(TAB_PROJEKT) + ".\"Bezeichner\") " +
            "WHERE \"ID_Projekt\" = ? AND " + Q(SPALTE_ID_STAMM) + " IS NULL " +
            "AND (SELECT COUNT(*) FROM " + Q(TAB_STAMM) + " s2 WHERE s2.\"Bezeichner\" = " + Q(TAB_PROJEKT) + ".\"Bezeichner\") = 1";

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}

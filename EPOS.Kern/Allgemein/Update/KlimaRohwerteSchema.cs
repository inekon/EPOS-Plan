using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KD-4 - ROHWERTE DES KLIMADATENIMPORTS (Konzept Klimadatenimport §§ 2, 3, 6; Entscheide KD-Q2, KD-Q4, KD-Q7).
    //
    // WAS.
    //   Tab_Klimastunde_STAMM (neu, STRICT): je Klimaregion des Katalogs und Stunde (1 ... 8760 im Raster der Quelle)
    //     die Stundenwerte, wie die Quelle sie liefert - alle Felder der DWD-TRY-Datei (t p WR WG N x RF B D A E IL)
    //     und der PVGIS-TMY-Antwort (T2m RH G(h) Gb(n) Gd(h) IR(h) WS10m WD10m SP), je Groesse eine nullbare Spalte
    //     mit der Einheit im Namen. NULL heisst "die Quelle fuehrt die Groesse nicht", nie 0. Fremdschluessel auf
    //     Tab_Klimaregion_STAMM mit ON DELETE CASCADE, Primaerschluessel (ID_Klimaregion, Stunde).
    //   Tab_Klimaregion_STAMM und Tab_Klimaregion: Standorthoehe, Bezugspunkt (Art, Name, Breite, Laenge, Hoehe,
    //     Entfernung) und die gelesene Ausfuehrung - alle nullbar.
    //   Tab_Klimaregion_STAMM allein: Zeitbezug der Rohtabelle ('UTC' bei PVGIS, 'MEZ' bei TRY) und Rohdaten (0/1,
    //     Vorgabe 0 = keine Rohwerte).
    //
    // NICHT IN DER PROJEKTKOPIE (KD-Q4). Es gibt keine Tab_Klimastunde; der Export liest den Katalogsatz.
    // KEIN NACHLADEN (KD-Q7). Bestandsregionen behalten Rohdaten = 0, nur ein Neuimport fuellt die Rohtabelle.
    // ERGEBNISNEUTRAL. Kein Rechenweg liest die Rohtabelle oder die neuen Regionsspalten (Wache
    // KlimaRohwerteRechenwegWacheTests); Tab_Solar(_STAMM) und Tab_Klimadaten(_STAMM) bleiben unberuehrt. Wiederholbar.
    //
    // DREI LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema und die Testvorrichtung in EPOS.Kern.Tests;
    // dazu KlimaImportAblauf (Schreibweg) und KlimaregionStammCtrl (Projektkopie der Regionsspalten) nach Vorhandensein.
    // ====================================================================================

    /// <summary>
    /// <b>KD-4</b> — Rohwerte des Klimadatenimports in eigener Tabelle (Entscheid KD-Q2) und die Herkunftsspalten der
    /// Region. EINE Quelle für Migration, Werkzeug, Testkopie und Nachweis (ADR-001 Option C).
    /// </summary>
    public static class KlimaRohwerteSchema
    {
        /// <summary><b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht.</summary>
        // KETTE: geparkt auf 238 - 235 (KB-7), 236 (KBP-4) und 237 BauteilStrahlungSchema liegen noch nicht vor.
        // Sobald 237 auf origin liegt, umhaengen auf "BauteilStrahlungSchema.SCHRITT + 1" und im selben Schritt
        // eintragen: SchemaMigration.SCHRITTE_SQLITE, Paketanhebung.STUFEN, SchemaStand.Zielversion; danach die
        // Testdatenbank heben.
        public const int SCHRITT = 238;

        /// <summary>Die Rohtabelle (nur Katalog).</summary>
        public const string TAB_ROH = "Tab_Klimastunde_STAMM";

        /// <summary>Der Regionskatalog.</summary>
        public const string TAB_REGION_STAMM = "Tab_Klimaregion_STAMM";

        /// <summary>Die Projektkopie der Region.</summary>
        public const string TAB_REGION = "Tab_Klimaregion";

        public const string SPALTE_REGION = "ID_Klimaregion";
        public const string SPALTE_STUNDE = "Stunde";

        // ---- die Rohspalten (Einheit im Namen) -------------------------------------------------------------------
        public const string SPALTE_TEMPERATUR = "Temperatur_C";
        public const string SPALTE_LUFTDRUCK = "Luftdruck_hPa";
        public const string SPALTE_WINDRICHTUNG = "Windrichtung_Grad";
        public const string SPALTE_WINDGESCHWINDIGKEIT = "Windgeschwindigkeit_m_s";
        public const string SPALTE_BEDECKUNGSGRAD = "Bedeckungsgrad_Achtel";
        public const string SPALTE_WASSERDAMPF = "Wasserdampfgehalt_g_kg";
        public const string SPALTE_LUFTFEUCHTE = "Luftfeuchte_Prozent";
        public const string SPALTE_DIREKT_HORIZONTAL = "Direkt_Horizontal_W_m2";
        public const string SPALTE_DIREKT_NORMAL = "Direkt_Normal_W_m2";
        public const string SPALTE_DIFFUS = "Diffus_Horizontal_W_m2";
        public const string SPALTE_GLOBAL = "Global_Horizontal_W_m2";
        public const string SPALTE_GEGENSTRAHLUNG = "Gegenstrahlung_W_m2";
        public const string SPALTE_AUSSTRAHLUNG = "Ausstrahlung_W_m2";
        public const string SPALTE_QUALITAET = "Qualitaet_Strahlung";

        /// <summary>Die vierzehn Rohspalten in Tabellenfolge.</summary>
        public static readonly IReadOnlyList<string> ROHSPALTEN = new[]
        {
            SPALTE_TEMPERATUR, SPALTE_LUFTDRUCK, SPALTE_WINDRICHTUNG, SPALTE_WINDGESCHWINDIGKEIT, SPALTE_BEDECKUNGSGRAD,
            SPALTE_WASSERDAMPF, SPALTE_LUFTFEUCHTE, SPALTE_DIREKT_HORIZONTAL, SPALTE_DIREKT_NORMAL, SPALTE_DIFFUS,
            SPALTE_GLOBAL, SPALTE_GEGENSTRAHLUNG, SPALTE_AUSSTRAHLUNG, SPALTE_QUALITAET
        };

        /// <summary>Kleinste Stunde im Raster der Quelle.</summary>
        public const int STUNDE_MIN = 1;

        /// <summary>Größte Stunde (Gemeinjahr, kein Schaltjahr).</summary>
        public const int STUNDE_MAX = 8760;

        // ---- die Regionsspalten -----------------------------------------------------------------------------------
        public const string SPALTE_STANDORT_HOEHE = "Standort_Hoehe_m";
        public const string SPALTE_BEZUG_ART = "Bezugspunkt_Art";
        public const string SPALTE_BEZUG_NAME = "Bezugspunkt_Name";
        public const string SPALTE_BEZUG_LAT = "Bezugspunkt_Latitude";
        public const string SPALTE_BEZUG_LON = "Bezugspunkt_Longitude";
        public const string SPALTE_BEZUG_HOEHE = "Bezugspunkt_Hoehe_m";
        public const string SPALTE_ENTFERNUNG = "Entfernung_km";
        public const string SPALTE_AUSFUEHRUNG = "Ausfuehrung";
        public const string SPALTE_ZEITBEZUG = "Zeitbezug";
        public const string SPALTE_ROHDATEN = "Rohdaten";

        /// <summary>Persistenzwerte der Bezugspunktart (sprachneutral).</summary>
        public const string ART_STATION = "station";
        public const string ART_DATEI = "datei";
        public const string ART_PVGIS = "pvgis";

        /// <summary>Persistenzwerte des Zeitbezugs der Rohtabelle.</summary>
        public const string ZEITBEZUG_UTC = "UTC";
        public const string ZEITBEZUG_MEZ = "MEZ";

        private static string Q(string s) => "\"" + s + "\"";

        private static string Zahl(double d) => d.ToString(CultureInfo.InvariantCulture);

        private static string Nullbar(string spalte, string bedingung) =>
            "CHECK (" + Q(spalte) + " IS NULL OR (" + bedingung + "))";

        /// <summary>Die Anweisung, die die Rohtabelle anlegt. CHECK-Bereiche nur, wo sie physikalisch sicher sind.</summary>
        public static readonly string SQL_CREATE =
            "CREATE TABLE IF NOT EXISTS " + Q(TAB_ROH) + " (\n" +
            "    " + Q(SPALTE_REGION) + " INTEGER NOT NULL,\n" +
            "    " + Q(SPALTE_STUNDE) + " INTEGER NOT NULL CHECK (" + Q(SPALTE_STUNDE) + " BETWEEN " +
            STUNDE_MIN.ToString(CultureInfo.InvariantCulture) + " AND " + STUNDE_MAX.ToString(CultureInfo.InvariantCulture) + "),\n" +
            "    " + Q(SPALTE_TEMPERATUR) + " REAL,\n" +
            "    " + Q(SPALTE_LUFTDRUCK) + " REAL " + Nullbar(SPALTE_LUFTDRUCK, Q(SPALTE_LUFTDRUCK) + " > 0") + ",\n" +
            "    " + Q(SPALTE_WINDRICHTUNG) + " REAL " + Nullbar(SPALTE_WINDRICHTUNG,
                Q(SPALTE_WINDRICHTUNG) + " >= 0 AND " + Q(SPALTE_WINDRICHTUNG) + " <= 360") + ",\n" +
            "    " + Q(SPALTE_WINDGESCHWINDIGKEIT) + " REAL " + Nullbar(SPALTE_WINDGESCHWINDIGKEIT,
                Q(SPALTE_WINDGESCHWINDIGKEIT) + " >= 0") + ",\n" +
            "    " + Q(SPALTE_BEDECKUNGSGRAD) + " REAL,\n" +
            "    " + Q(SPALTE_WASSERDAMPF) + " REAL " + Nullbar(SPALTE_WASSERDAMPF, Q(SPALTE_WASSERDAMPF) + " >= 0") + ",\n" +
            "    " + Q(SPALTE_LUFTFEUCHTE) + " REAL " + Nullbar(SPALTE_LUFTFEUCHTE, Q(SPALTE_LUFTFEUCHTE) + " >= 0") + ",\n" +
            "    " + Q(SPALTE_DIREKT_HORIZONTAL) + " REAL,\n" +
            "    " + Q(SPALTE_DIREKT_NORMAL) + " REAL,\n" +
            "    " + Q(SPALTE_DIFFUS) + " REAL,\n" +
            "    " + Q(SPALTE_GLOBAL) + " REAL,\n" +
            "    " + Q(SPALTE_GEGENSTRAHLUNG) + " REAL,\n" +
            "    " + Q(SPALTE_AUSSTRAHLUNG) + " REAL,\n" +
            "    " + Q(SPALTE_QUALITAET) + " INTEGER,\n" +
            "    PRIMARY KEY (" + Q(SPALTE_REGION) + ", " + Q(SPALTE_STUNDE) + "),\n" +
            "    FOREIGN KEY (" + Q(SPALTE_REGION) + ") REFERENCES " + Q(TAB_REGION_STAMM) + " (" + Q(SPALTE_REGION) +
            ") ON DELETE CASCADE\n" +
            ") STRICT";

        /// <summary>Die Spalten an Katalog UND Projektkopie der Region.</summary>
        public static readonly IReadOnlyList<(string Spalte, string Typ)> REGIONSSPALTEN = new[]
        {
            (SPALTE_STANDORT_HOEHE, "REAL"),
            (SPALTE_BEZUG_ART, "TEXT " + Nullbar(SPALTE_BEZUG_ART, Q(SPALTE_BEZUG_ART) + " IN ('" + ART_STATION + "','" +
                                                 ART_DATEI + "','" + ART_PVGIS + "')")),
            (SPALTE_BEZUG_NAME, "TEXT"),
            (SPALTE_BEZUG_LAT, "REAL " + Nullbar(SPALTE_BEZUG_LAT, Q(SPALTE_BEZUG_LAT) + " >= -90 AND " + Q(SPALTE_BEZUG_LAT) + " <= 90")),
            (SPALTE_BEZUG_LON, "REAL " + Nullbar(SPALTE_BEZUG_LON, Q(SPALTE_BEZUG_LON) + " >= -180 AND " + Q(SPALTE_BEZUG_LON) + " <= 180")),
            (SPALTE_BEZUG_HOEHE, "REAL"),
            (SPALTE_ENTFERNUNG, "REAL " + Nullbar(SPALTE_ENTFERNUNG, Q(SPALTE_ENTFERNUNG) + " >= 0")),
            (SPALTE_AUSFUEHRUNG, "TEXT"),
        };

        /// <summary>Die Spalten allein am Katalog (sie beschreiben die Rohtabelle).</summary>
        public static readonly IReadOnlyList<(string Spalte, string Typ)> KATALOGSPALTEN = new[]
        {
            (SPALTE_ZEITBEZUG, "TEXT " + Nullbar(SPALTE_ZEITBEZUG, Q(SPALTE_ZEITBEZUG) + " IN ('" + ZEITBEZUG_UTC + "','" +
                                                ZEITBEZUG_MEZ + "')")),
            (SPALTE_ROHDATEN, "INTEGER NOT NULL DEFAULT 0 CHECK (" + Q(SPALTE_ROHDATEN) + " IN (0,1))"),
        };

        /// <summary>Alle Spalten des Schritts, je Tabelle, in Anlegefolge.</summary>
        public static IReadOnlyList<(string Tabelle, string Spalte, string Typ)> Spalten()
        {
            var l = new List<(string, string, string)>();
            foreach (var s in REGIONSSPALTEN) l.Add((TAB_REGION_STAMM, s.Spalte, s.Typ));
            foreach (var s in KATALOGSPALTEN) l.Add((TAB_REGION_STAMM, s.Spalte, s.Typ));
            foreach (var s in REGIONSSPALTEN) l.Add((TAB_REGION, s.Spalte, s.Typ));
            return l;
        }

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { TAB_REGION_STAMM, TAB_REGION };

        /// <summary>Die Anweisung, die eine Spalte anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE " + Q(s.Tabelle) + " ADD COLUMN " + Q(s.Spalte) + " " + s.Typ;

        /// <summary>Steht die Rohtabelle?</summary>
        public static bool RohtabelleVorhanden() => DataRepository.TabelleVorhanden(TAB_ROH);

        /// <summary>Stehen Rohtabelle und alle Regionsspalten?</summary>
        public static bool SchemaVollstaendig() =>
            RohtabelleVorhanden() && Spalten().All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>Stehen die Regionsspalten an der Projektkopie (Kopie der Herkunft)?</summary>
        public static bool ProjektspaltenVorhanden() =>
            REGIONSSPALTEN.All(s => DataRepository.SpalteVorhanden(TAB_REGION, s.Spalte));

        /// <summary>
        /// Führt den Schritt aus (wiederholbar): Tabelle und Spalten in EINEM Vorgang. Rückgabe = Zahl der angelegten
        /// Spalten. Fehlt eine Voraussetzung, wirft er benannt. KEIN UPDATE und KEIN Nachladen (KD-Q7).
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            // Die Auskunft VOR dem Vorgang - TabelleVorhanden/SpalteVorhanden arbeiten auf einer eigenen Verbindung.
            bool tabelleFehlt = !RohtabelleVorhanden();
            var offen = Spalten().Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();
            if (tabelleFehlt || offen.Count > 0)
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        if (tabelleFehlt) v.Ausfuehren(SQL_CREATE);
                        foreach (var s in offen) v.Ausfuehren(Anlegen(s));
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
                    if (tabelleFehlt) bericht.Add(TAB_ROH + " angelegt");
                    foreach (var s in offen) bericht.Add(s.Tabelle + "." + s.Spalte + " angelegt");
                }
            }
            else
            {
                bericht?.Add("Rohtabelle und Regionsspalten stehen bereits");
            }

            bericht?.Add("KEIN Nachladen: Bestandsregionen tragen " + SPALTE_ROHDATEN + " = 0, nur ein Neuimport fuellt " +
                         TAB_ROH + "; kein Rechenweg liest sie, der Referenzlauf bleibt byte-gleich");
            return offen.Count;
        }

        // ---- Schreibweg des Imports (nur nach dem Schritt) ------------------------------------------------------

        /// <summary>
        /// Schreibt die Rohreihe einer neuen Katalogregion und setzt <c>Rohdaten = 1</c> samt Zeitbezug. Nur rufen, wenn
        /// <see cref="SchemaVollstaendig"/> VOR dem Vorgang wahr war. Stunden außerhalb 1 … 8760 werden nicht geschrieben.
        /// Rückgabe = Zahl der geschriebenen Stunden.
        /// </summary>
        public static int RohwerteSchreiben(DbVorgang v, int idRegion, KlimaRohreihe reihe)
        {
            if (v == null) throw new ArgumentNullException(nameof(v));
            if (reihe == null || reihe.Stunden.Count == 0) return 0;

            string sql = "INSERT INTO " + Q(TAB_ROH) + " (" + Q(SPALTE_REGION) + ", " + Q(SPALTE_STUNDE) + ", " +
                         string.Join(", ", ROHSPALTEN.Select(Q)) + ") VALUES (?, ?" +
                         string.Concat(Enumerable.Repeat(", ?", ROHSPALTEN.Count)) + ")";
            int n = 0;
            foreach (KlimaRohstunde s in reihe.Stunden)
            {
                if (s == null || s.Stunde < STUNDE_MIN || s.Stunde > STUNDE_MAX) continue;
                v.Ausfuehren(sql,
                    new DbParam("?", DbParamTyp.Integer) { Wert = idRegion },
                    new DbParam("?", DbParamTyp.Integer) { Wert = s.Stunde },
                    Reell(s.Temperatur), Reell(s.Luftdruck_hPa), Reell(s.Windrichtung_Grad),
                    Reell(s.Windgeschwindigkeit_m_s), Reell(s.Bedeckungsgrad_Achtel), Reell(s.Wasserdampfgehalt_g_kg),
                    Reell(s.Luftfeuchte_Prozent), Reell(s.DirektHorizontal), Reell(s.DirektNormal), Reell(s.DiffusHorizontal),
                    Reell(s.GlobalHorizontal), Reell(s.Gegenstrahlung), Reell(s.Ausstrahlung),
                    new DbParam("?", DbParamTyp.Integer) { Wert = s.QualitaetStrahlung });
                n++;
            }

            v.Ausfuehren("UPDATE " + Q(TAB_REGION_STAMM) + " SET " + Q(SPALTE_ROHDATEN) + " = ?, " + Q(SPALTE_ZEITBEZUG) +
                         " = ? WHERE " + Q(SPALTE_REGION) + " = ?",
                         new DbParam("@roh", n > 0 ? 1 : 0),
                         new DbParam("@zeit", string.IsNullOrEmpty(reihe.Zeitbezug) ? (object)DBNull.Value : reihe.Zeitbezug),
                         new DbParam("@id", idRegion));
            return n;
        }

        /// <summary>
        /// Schreibt Standorthöhe, Bezugspunkt und Ausführung an die Katalogregion. Nur rufen, wenn
        /// <see cref="SchemaVollstaendig"/> VOR dem Vorgang wahr war.
        /// </summary>
        public static void HerkunftSchreiben(DbVorgang v, int idRegion, KlimaRegionsherkunft h)
        {
            if (v == null) throw new ArgumentNullException(nameof(v));
            if (h == null) return;
            v.Ausfuehren("UPDATE " + Q(TAB_REGION_STAMM) + " SET " +
                         string.Join(", ", REGIONSSPALTEN.Select(s => Q(s.Spalte) + " = ?")) +
                         " WHERE " + Q(SPALTE_REGION) + " = ?",
                         Reell(h.StandortHoeheM), Text(h.BezugspunktArt), Text(h.BezugspunktName),
                         Reell(h.BezugspunktLatitude), Reell(h.BezugspunktLongitude), Reell(h.BezugspunktHoeheM),
                         Reell(h.EntfernungKm), Text(h.Ausfuehrung),
                         new DbParam("@id", idRegion));
        }

        private static DbParam Reell(double? w) =>
            new DbParam("?", DbParamTyp.Double) { Wert = w.HasValue && !double.IsNaN(w.Value) ? (object)w.Value : null };

        private static DbParam Text(string w) =>
            new DbParam("?", string.IsNullOrWhiteSpace(w) ? (object)DBNull.Value : w.Trim());

        /// <summary>Der Persistenzwert einer Bezugspunktart.</summary>
        public static string Artschluessel(KlimaBezugspunktArt art) => art switch
        {
            KlimaBezugspunktArt.Repraesentanzstation => ART_STATION,
            KlimaBezugspunktArt.Dateistandort => ART_DATEI,
            _ => ART_PVGIS
        };

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}

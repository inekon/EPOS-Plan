using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // PVG - PHOTOVOLTAIK MIT PROFIL ODER GANGLINIE (Schemaschritt 205, Anwenderwunsch 08.10.2026).
    //
    // WAS. Eine Photovoltaikanlage rechnet entweder ueber ihre Module (Klimadaten, Modulkennwerte,
    // Ausrichtung - das Profil) oder ueber eine eingelesene Ganglinie: die AC-Leistung der Anlage in kW
    // je Zeitschritt, im Raster der Datei - 8 760 Stunden- oder 35 040 Viertelstundenwerte. Fuenf
    // Tabellen nach dem Muster der Solarthermieganglinie:
    //
    //   Tab_PvGanglinie_STAMM      Katalogkopf (Bezeichner, Beschreibung, Raster_Minuten 60|15,
    //                              Nennleistung_kWp optional, Jahresarbeit_kWh, Spitze_kW, ReadOnly,
    //                              Katalogschluessel, Pruefsumme, Ausgelaufen)
    //   Tab_PvGanglinieDaten_STAMM Werte des Katalogs, eine Zeile je Zeitschritt, Reihenfolge = ID
    //   Tab_PvGanglinie            Projektkopie des Kopfs (an Tab_Projekt, ON DELETE CASCADE)
    //   Tab_PvGanglinieDaten       Werte der Projektkopie
    //   Z_ProjektPvGanglinie       Zuordnung Projekt -> Projektkopie (ueber die ID)
    //
    // DAS RASTER BLEIBT. Anders als die Solarthermieganglinie (immer Stundenmittel) speichert die
    // PV-Ganglinie im importierten Raster: Die Photovoltaik bilanziert je Viertelstunde, und eine
    // gemessene Viertelstundenreihe traegt die Spitzen, die die Einspeisegrenze kappt.
    //
    // ERGEBNISNEUTRAL. Reines DDL, keine Zeile entsteht; kein Referenzprojekt fuehrt eine PV-Ganglinie.
    //
    // NUMMER. 205 = ZonenKatalogSchema.SCHRITT + 1. Eingetragen in SchemaStand.Zielversion, im Register
    // der Paketanhebung (Art Katalog), in der SchemaMigration der Schale, in Werkzeuge/Testdatenbankschema
    // und in EPOS.Kern.Tests/TestDatenbank.
    // ====================================================================================

    /// <summary>
    /// <b>PVG</b> — die Tabellen der PV-Ganglinie (Katalog, Projektkopie, Zuordnung). Anlass und Bauform
    /// stehen im Kopf der Datei.
    /// </summary>
    public static class PvGanglinieSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> (Schritt 205) — die EINE Stelle, an der sie steht: der Schritt
        /// hinter <see cref="ZonenKatalogSchema"/> (Schritt 204).
        /// </summary>
        public const int SCHRITT = ZonenKatalogSchema.SCHRITT + 1;

        /// <summary>Der Katalogkopf.</summary>
        public const string TAB_KOPF_STAMM = "Tab_PvGanglinie_STAMM";

        /// <summary>Die Werte des Katalogs.</summary>
        public const string TAB_DATEN_STAMM = "Tab_PvGanglinieDaten_STAMM";

        /// <summary>Die Projektkopie des Kopfs.</summary>
        public const string TAB_KOPF = "Tab_PvGanglinie";

        /// <summary>Die Werte der Projektkopie.</summary>
        public const string TAB_DATEN = "Tab_PvGanglinieDaten";

        /// <summary>Die Zuordnung Projekt → Projektkopie.</summary>
        public const string TAB_ZUORDNUNG = "Z_ProjektPvGanglinie";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_PvGanglinie_STAMM</c>.</summary>
        public const string SQL_CREATE_KOPF_STAMM =
            "CREATE TABLE IF NOT EXISTS \"Tab_PvGanglinie_STAMM\" (" +
            "\"ID\" INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "\"Bezeichner\" TEXT, " +
            "\"Beschreibung\" TEXT, " +
            "\"Raster_Minuten\" INTEGER NOT NULL DEFAULT 60 CHECK (\"Raster_Minuten\" IN (15,60)), " +
            "\"Nennleistung_kWp\" REAL CHECK (\"Nennleistung_kWp\" IS NULL OR \"Nennleistung_kWp\" > 0), " +
            "\"Jahresarbeit_kWh\" REAL, " +
            "\"Spitze_kW\" REAL, " +
            "\"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1)), " +
            "\"Katalog_Schluessel\" TEXT CHECK (\"Katalog_Schluessel\" IS NULL OR length(\"Katalog_Schluessel\") BETWEEN 1 AND 120), " +
            "\"Katalog_Pruefsumme\" TEXT CHECK (\"Katalog_Pruefsumme\" IS NULL OR length(\"Katalog_Pruefsumme\") = 64), " +
            "\"Katalog_Ausgelaufen\" INTEGER NOT NULL DEFAULT 0 CHECK (\"Katalog_Ausgelaufen\" IN (0,1))" +
            ") STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_PvGanglinieDaten_STAMM</c>.</summary>
        public const string SQL_CREATE_DATEN_STAMM =
            "CREATE TABLE IF NOT EXISTS \"Tab_PvGanglinieDaten_STAMM\" (" +
            "\"ID\" INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "\"ID_Ganglinie\" INTEGER NOT NULL, " +
            "\"Wert\" REAL, " +
            "\"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1)), " +
            "FOREIGN KEY (\"ID_Ganglinie\") REFERENCES \"Tab_PvGanglinie_STAMM\" (\"ID\") ON UPDATE CASCADE ON DELETE CASCADE" +
            ") STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_PvGanglinie</c> — die Projektkopie.</summary>
        public const string SQL_CREATE_KOPF =
            "CREATE TABLE IF NOT EXISTS \"Tab_PvGanglinie\" (" +
            "\"ID\" INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "\"ID_Projekt\" INTEGER, " +
            "\"Bezeichner\" TEXT, " +
            "\"Beschreibung\" TEXT, " +
            "\"Raster_Minuten\" INTEGER NOT NULL DEFAULT 60 CHECK (\"Raster_Minuten\" IN (15,60)), " +
            "\"Nennleistung_kWp\" REAL CHECK (\"Nennleistung_kWp\" IS NULL OR \"Nennleistung_kWp\" > 0), " +
            "\"Jahresarbeit_kWh\" REAL, " +
            "\"Spitze_kW\" REAL, " +
            "FOREIGN KEY (\"ID_Projekt\") REFERENCES \"Tab_Projekt\" (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE" +
            ") STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_PvGanglinieDaten</c>.</summary>
        public const string SQL_CREATE_DATEN =
            "CREATE TABLE IF NOT EXISTS \"Tab_PvGanglinieDaten\" (" +
            "\"ID\" INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "\"ID_Ganglinie\" INTEGER NOT NULL, " +
            "\"Wert\" REAL, " +
            "FOREIGN KEY (\"ID_Ganglinie\") REFERENCES \"Tab_PvGanglinie\" (\"ID\") ON UPDATE CASCADE ON DELETE CASCADE" +
            ") STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Z_ProjektPvGanglinie</c>.</summary>
        public const string SQL_CREATE_ZUORDNUNG =
            "CREATE TABLE IF NOT EXISTS \"Z_ProjektPvGanglinie\" (" +
            "\"ID\" INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "\"ID_Projekt\" INTEGER NOT NULL, " +
            "\"ID_Ganglinie\" INTEGER NOT NULL, " +
            "\"Bezeichner\" TEXT, " +
            "FOREIGN KEY (\"ID_Projekt\") REFERENCES \"Tab_Projekt\" (\"ID\") ON UPDATE CASCADE ON DELETE CASCADE, " +
            "FOREIGN KEY (\"ID_Ganglinie\") REFERENCES \"Tab_PvGanglinie\" (\"ID\") ON UPDATE CASCADE ON DELETE CASCADE" +
            ") STRICT";

        /// <summary>Die Tabellenanweisungen in Anlegereihenfolge (Köpfe vor Werten und Zuordnung).</summary>
        public static IEnumerable<KeyValuePair<string, string>> Tabellenanweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(TAB_KOPF_STAMM, SQL_CREATE_KOPF_STAMM);
                yield return new KeyValuePair<string, string>(TAB_DATEN_STAMM, SQL_CREATE_DATEN_STAMM);
                yield return new KeyValuePair<string, string>(TAB_KOPF, SQL_CREATE_KOPF);
                yield return new KeyValuePair<string, string>(TAB_DATEN, SQL_CREATE_DATEN);
                yield return new KeyValuePair<string, string>(TAB_ZUORDNUNG, SQL_CREATE_ZUORDNUNG);
            }
        }

        /// <summary>Die Indizes (Name → Anweisung).</summary>
        public static IEnumerable<KeyValuePair<string, string>> Indexanweisungen
        {
            get
            {
                yield return Index("UX_Tab_PvGanglinie_STAMM_Bezeichner",
                    "CREATE UNIQUE INDEX IF NOT EXISTS \"UX_Tab_PvGanglinie_STAMM_Bezeichner\" ON \"Tab_PvGanglinie_STAMM\" (\"Bezeichner\")");
                yield return Index("UX_Tab_PvGanglinie_STAMM_Katalog_Schluessel",
                    "CREATE UNIQUE INDEX IF NOT EXISTS \"UX_Tab_PvGanglinie_STAMM_Katalog_Schluessel\" ON \"Tab_PvGanglinie_STAMM\" " +
                    "(\"Katalog_Schluessel\") WHERE \"Katalog_Schluessel\" IS NOT NULL");
                yield return Index("idx_PvGanglinieDatenStamm_Ganglinie",
                    "CREATE INDEX IF NOT EXISTS \"idx_PvGanglinieDatenStamm_Ganglinie\" ON \"Tab_PvGanglinieDaten_STAMM\" (\"ID_Ganglinie\")");
                yield return Index("idx_PvGanglinie_Projekt",
                    "CREATE INDEX IF NOT EXISTS \"idx_PvGanglinie_Projekt\" ON \"Tab_PvGanglinie\" (\"ID_Projekt\")");
                yield return Index("idx_PvGanglinieDaten_Ganglinie",
                    "CREATE INDEX IF NOT EXISTS \"idx_PvGanglinieDaten_Ganglinie\" ON \"Tab_PvGanglinieDaten\" (\"ID_Ganglinie\")");
                yield return Index("idx_ZProjektPvGanglinie_Projekt",
                    "CREATE INDEX IF NOT EXISTS \"idx_ZProjektPvGanglinie_Projekt\" ON \"Z_ProjektPvGanglinie\" (\"ID_Projekt\")");
                yield return Index("idx_ZProjektPvGanglinie_Ganglinie",
                    "CREATE INDEX IF NOT EXISTS \"idx_ZProjektPvGanglinie_Ganglinie\" ON \"Z_ProjektPvGanglinie\" (\"ID_Ganglinie\")");
            }
        }

        private static KeyValuePair<string, string> Index(string name, string sql) => new KeyValuePair<string, string>(name, sql);

        /// <summary>Die Tabellen, die der Schritt voraussetzt.</summary>
        public static IEnumerable<string> Voraussetzungen()
        {
            yield return "Tab_Projekt";
        }

        // =================================================================
        //  Stand und Ausführung
        // =================================================================

        /// <summary>Kann der Datenweg die PV-Ganglinie nutzen? Alle fünf Tabellen stehen.</summary>
        public static bool Lesbar()
        {
            foreach (KeyValuePair<string, string> a in Tabellenanweisungen)
                if (!DataRepository.TabelleVorhanden(a.Key)) return false;
            return true;
        }

        /// <summary>Steht der Schritt? <see cref="Lesbar"/> und alle Indizes.</summary>
        public static bool Vollstaendig()
        {
            if (!Lesbar()) return false;
            foreach (KeyValuePair<string, string> a in Indexanweisungen)
            {
                object sql = DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = ?", new DbParam("@i", a.Key));
                if (sql == null || sql == DBNull.Value) return false;
            }
            return true;
        }

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang aus — für die Migration der Schale, <c>Werkzeuge/Testdatenbankschema</c>
        /// und <c>EPOS.Kern.Tests</c>. <b>Wiederholbar</b> (vorhandene Tabelle = nichts zu tun), <b>kein DML</b>.
        /// Fehlt eine Voraussetzung, bricht er benannt ab.
        /// </summary>
        /// <param name="bericht">Nimmt Zeilen auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Tabellen (höchstens fünf).</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + SCHRITT.ToString(CultureInfo.InvariantCulture) +
                                                        ": Die Tabelle " + t + " fehlt; ein frueherer Schritt ist nicht gelaufen.");

            List<string> tabellen = Tabellenanweisungen.Where(a => !DataRepository.TabelleVorhanden(a.Key)).Select(a => a.Key).ToList();

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (KeyValuePair<string, string> a in Tabellenanweisungen) v.Ausfuehren(a.Value);
                    foreach (KeyValuePair<string, string> a in Indexanweisungen) v.Ausfuehren(a.Value);
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }

            bericht?.Add(tabellen.Count.ToString(CultureInfo.InvariantCulture) + " von 5 Tabelle(n) angelegt (" +
                         string.Join(", ", Tabellenanweisungen.Select(a => a.Key)) + ")");
            return tabellen.Count;
        }
    }
}

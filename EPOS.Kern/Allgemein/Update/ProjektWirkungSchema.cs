using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE NICHT MONETARISIERBAREN WIRKUNGEN ALS LISTE JE PROJEKT - Schemaschritt 126
    // (Etappe E17, Konzept Wirtschaftlichkeit § 2.11.2 V-G11; Entscheid E17-Q1 a).
    //
    // WOZU. DIN EN 17463 verlangt, die Wirkungen einer Massnahme, die sich nicht in Euro
    // fassen lassen, zu ERFASSEN, zu KATEGORISIEREN (6.1: Energiefluss, finanziell,
    // sonstig) und nach Dauer und Wirkung auf Organisation, Mitarbeiter und Umwelt zu
    // BEURTEILEN (8.2). Bis hierher trug das Projekt dafuer EIN Freitextfeld
    // (Tab_ProjektWirtschaftlichkeit.Nicht_Monetaer, Schritt 72) - ohne Kategorie und
    // ohne Beurteilung. Dieser Schritt legt die Liste an: eine Zeile je Wirkung.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // NutzungsdauerSchema (75): DREI Leser - der Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs, das Werkzeug
    // Werkzeuge/Testdatenbankschema und die Testvorrichtung EPOS.Kern.Tests/TestDatenbank.
    //
    // STRICT, BEZIEHUNG UEBER DIE ID. Der Fremdschluessel auf Tab_Projekt traegt
    // ON DELETE CASCADE ON UPDATE CASCADE wie die Projekttabellen aus Schritt 96: Ein
    // geloeschtes Projekt nimmt seine Wirkungen mit. Das generische Duplizieren und der
    // Projekttransfer (ProjektDuplizierenCtrl, ProjektExportImportCtrl) erkennen die
    // Tabelle an ihrer Spalte ID_Projekt und kopieren sie ohne Codeaenderung.
    //
    // DIE SKALEN (E17-Q2 a). Dauer 1..3 (kurz, mittel, lang), je Wirkungsgrad 0..3
    // (keine, gering, mittel, stark). NULL heisst "nicht beurteilt" - so steht eine aus
    // dem Freitext uebernommene Wirkung da. Die BEURTEILUNG (Dauer x staerkste Wirkung)
    // ist eine Anzeige und wird NICHT gespeichert; die Regel steht einmal in
    // NichtMonetaereWirkungen.Beurteilung.
    //
    // DIE UEBERNAHME DES FREITEXTS (E17-Q3 a). Ein gepflegter Freitext wird EINE Wirkung
    // der Kategorie SONSTIG ohne Beurteilung (Sortierung 1). Nur fuer Projekte, die noch
    // keine Wirkung tragen - so bleibt der Schritt wiederholbar, und ein zweiter Lauf
    // legt nichts doppelt an. Das Freitextfeld selbst bleibt stehen und lesbar (Altfeld);
    // geschrieben wird es von keiner Maske mehr.
    //
    // ERGEBNISNEUTRAL. Kein Rechenweg liest die Tabelle; die Wirkungen fliessen nie in
    // den Kapitalwert. Der Referenzlauf bleibt byte-gleich.
    // ====================================================================================

    /// <summary>
    /// <b>DDL und Freitextuebernahme der Tabelle <c>Tab_ProjektWirkung</c></b> —
    /// Schemaschritt 126. Anlass und Regeln stehen im Kopf der Datei.
    /// </summary>
    public static class ProjektWirkungSchema
    {
        /// <summary>Die Nummer des Schemaschritts (vorlaeufig, E17; Regel „wer zuerst pusht").</summary>
        public const int SCHRITT = 126;

        /// <summary>Die Tabelle, die der Schritt anlegt.</summary>
        public const string TABELLE = "Tab_ProjektWirkung";

        /// <summary>Der Index auf der Projektspalte (Loeschweitergabe und Laden).</summary>
        public const string INDEX = "idx_ProjektWirkung_Projekt";

        /// <summary>Die Kategorie, unter der ein uebernommener Freitext steht.</summary>
        public const string KATEGORIE_UEBERNAHME = "SONSTIG";

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_ProjektWirkung</c> — zehn Spalten, <b>STRICT</b>,
        /// Fremdschluessel auf <c>Tab_Projekt</c> mit Weitergabe. Die Wertebereiche stehen
        /// als CHECK in der Tabelle; NULL heisst in Dauer und Wirkung „nicht beurteilt".
        /// </summary>
        public const string SQL_CREATE =
            "CREATE TABLE IF NOT EXISTS \"Tab_ProjektWirkung\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Projekt\" INTEGER NOT NULL,\n" +
            "    \"Sortierung\" INTEGER NOT NULL,\n" +
            "    \"Kategorie\" TEXT NOT NULL CHECK (\"Kategorie\" IN ('ENERGIEFLUSS','FINANZIELL','SONSTIG')),\n" +
            "    \"Beschreibung\" TEXT NOT NULL,\n" +
            "    \"Dauer\" INTEGER CHECK (\"Dauer\" BETWEEN 1 AND 3),\n" +
            "    \"Wirkung_Organisation\" INTEGER CHECK (\"Wirkung_Organisation\" BETWEEN 0 AND 3),\n" +
            "    \"Wirkung_Mitarbeiter\" INTEGER CHECK (\"Wirkung_Mitarbeiter\" BETWEEN 0 AND 3),\n" +
            "    \"Wirkung_Umwelt\" INTEGER CHECK (\"Wirkung_Umwelt\" BETWEEN 0 AND 3),\n" +
            "    FOREIGN KEY (\"ID_Projekt\") REFERENCES \"Tab_Projekt\" (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE\n" +
            ") STRICT";

        /// <summary>Der Index auf <c>ID_Projekt</c>.</summary>
        public const string SQL_INDEX =
            "CREATE INDEX IF NOT EXISTS \"idx_ProjektWirkung_Projekt\" ON \"Tab_ProjektWirkung\" (\"ID_Projekt\")";

        /// <summary>
        /// Die Uebernahme des Freitexts: je Projekt mit gepflegtem Text und OHNE eigene
        /// Wirkung eine Zeile SONSTIG, Sortierung 1, ohne Beurteilung. Nur Projekte, die es
        /// gibt (der Fremdschluessel verlangte es sonst).
        /// </summary>
        public const string SQL_UEBERNAHME =
            "INSERT INTO \"Tab_ProjektWirkung\" (\"ID_Projekt\", \"Sortierung\", \"Kategorie\", \"Beschreibung\") " +
            "SELECT w.\"ID_Projekt\", 1, '" + KATEGORIE_UEBERNAHME + "', TRIM(w.\"Nicht_Monetaer\") " +
            "FROM \"Tab_ProjektWirtschaftlichkeit\" w " +
            "WHERE TRIM(COALESCE(w.\"Nicht_Monetaer\", '')) <> '' " +
            "AND EXISTS (SELECT 1 FROM \"Tab_Projekt\" p WHERE p.\"ID\" = w.\"ID_Projekt\") " +
            "AND NOT EXISTS (SELECT 1 FROM \"Tab_ProjektWirkung\" x WHERE x.\"ID_Projekt\" = w.\"ID_Projekt\") " +
            "ORDER BY w.\"ID_Projekt\"";

        /// <summary>Wie viele Freitexte der Schritt noch uebernehmen wuerde.</summary>
        public const string SQL_OFFEN =
            "SELECT COUNT(*) FROM \"Tab_ProjektWirtschaftlichkeit\" w " +
            "WHERE TRIM(COALESCE(w.\"Nicht_Monetaer\", '')) <> '' " +
            "AND EXISTS (SELECT 1 FROM \"Tab_Projekt\" p WHERE p.\"ID\" = w.\"ID_Projekt\") " +
            "AND NOT EXISTS (SELECT 1 FROM \"Tab_ProjektWirkung\" x WHERE x.\"ID_Projekt\" = w.\"ID_Projekt\")";

        /// <summary>Was ein Lauf getan hat.</summary>
        public sealed class Bericht
        {
            /// <summary>Die Tabelle wurde in diesem Lauf angelegt.</summary>
            public bool TabelleAngelegt;

            /// <summary>Zahl der uebernommenen Freitexte.</summary>
            public int Uebernommen;

            /// <summary>Die Zeile fuer Protokoll und Werkzeug.</summary>
            public string Zeile()
            {
                return TABELLE + (TabelleAngelegt ? " angelegt" : " stand bereits") + ", " +
                       Uebernommen.ToString(CultureInfo.InvariantCulture) +
                       " Freitext(e) als Wirkung der Kategorie " + KATEGORIE_UEBERNAHME +
                       " ohne Beurteilung uebernommen";
            }
        }

        /// <summary>Steht die Tabelle?</summary>
        public static bool TabelleVorhanden()
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?",
                new DbParam("@t", TABELLE));
            return o != null && o != DBNull.Value && Convert.ToInt64(o) > 0;
        }

        /// <summary>Steht der Zielstand — Tabelle da und kein Freitext mehr offen?</summary>
        public static bool Vollstaendig()
        {
            if (!TabelleVorhanden()) return false;
            object o = DataRepository.ExecuteScalar(SQL_OFFEN);
            return o != null && o != DBNull.Value && Convert.ToInt64(o) == 0;
        }

        /// <summary>
        /// Legt Tabelle und Index an (wenn sie fehlen) und uebernimmt die gepflegten
        /// Freitexte. <b>Wiederholbar:</b> Ein zweiter Lauf legt nichts an und uebernimmt
        /// nichts. Fehler werfen — der Aufrufer (Migration, Werkzeug, Vorrichtung) meldet sie.
        /// </summary>
        public static Bericht Ausfuehren()
        {
            var b = new Bericht { TabelleAngelegt = !TabelleVorhanden() };
            DataRepository.ExecuteNonQuery(SQL_CREATE);
            DataRepository.ExecuteNonQuery(SQL_INDEX);
            b.Uebernommen = DataRepository.ExecuteNonQuery(SQL_UEBERNAHME);
            if (b.Uebernommen < 0) b.Uebernommen = 0;
            return b;
        }
    }
}

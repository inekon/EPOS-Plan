-- ============================================================================
-- Reduziere-Testdatenbank.sql
--
-- ZWECK
--   Reduziert eine KOPIE der produktiven Kenndaten.sqlite auf die dreizehn
--   Referenzprojekte der Referenzlauf-Suite und schrumpft die Datei mit VACUUM.
--   Ergebnis ist die Datei, die als Referenzlaeufe/Kenndaten_Test.sqlite
--   eingecheckt wird (Umsetzungskonzept iE6, Entscheidung iF14).
--
--   Die dreizehn Projekte (Tab_Projekt.ID):
--     1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024, 1030, 1039, 1040, 1041, 1042
--   Quelle der Liste: Referenzlaeufe/LIESMICH.md, Basis 2026-08-30_B3-Kaskade
--   (332 CSV). Die Projekte 1043 und 1044 gehoeren bewusst NICHT dazu.
--
-- VORAUSSETZUNG - NUR AUF EINER KOPIE AUSFUEHREN
--   Es gilt die Schutzregel der Referenzlauf-Suite: "die produktive Datenbank
--   wird nie beschrieben" (Referenzlaeufe/LIESMICH.md, Umsetzungskonzept 3.8).
--   Dieses Skript LOESCHT Daten. Vor dem Lauf also:
--     1. EPOS-Plan beenden (keine Schreibsperre auf der Datenbank).
--     2. C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite kopieren, z. B. nach
--        C:\Temp\Kenndaten_Test.sqlite.
--     3. Dieses Skript ausschliesslich gegen die KOPIE laufen lassen.
--   Das Skript enthaelt keine Pfadangabe und keinen ATTACH - es wirkt immer auf
--   genau die Datenbank, die die aufrufende Sitzung geoeffnet hat. Die Wahl der
--   richtigen Datei liegt damit vollstaendig beim Anwender.
--
-- AUFRUF AUF WINDOWS
--   a) Kommandozeile (empfohlen):
--        copy "C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite" "C:\Temp\Kenndaten_Test.sqlite"
--        sqlite3.exe "C:\Temp\Kenndaten_Test.sqlite" ".read Reduziere-Testdatenbank.sql"
--      Bei Pfaden mit Backslash in .read die Trennzeichen verdoppeln oder das
--      Skript ins aktuelle Verzeichnis legen.
--   b) SQLiteStudio: Kopie oeffnen, Skript im SQL-Editor einfuegen, "Execute all"
--      (Umschalt+F9). Siehe Punkt "KONTROLLSCHRITT 1" - SQLiteStudio verwaltet
--      seine Verbindung selbst, PRAGMA foreign_keys greift dort nicht immer.
--
-- KONTROLLSCHRITT 1 - Fremdschluessel muessen eingeschaltet sein
--   Reines SQL kann sich nicht selbst abbrechen. Der Anwender fuehrt deshalb VOR
--   dem Skript einmal von Hand aus:
--        PRAGMA foreign_keys = ON;
--        PRAGMA foreign_keys;      -- muss 1 liefern
--   Liefert die zweite Zeile 0, sind die neunzehn ON-DELETE-CASCADE-Beziehungen
--   wirkungslos (Rev.-2-Konzept 5.3). Dieses Skript ist zwar bewusst so gebaut,
--   dass es AUCH DANN vollstaendig reduziert - jede Kaskade hat unten ein
--   explizites Gegenstueck (Stufe 1b und Stufe 3) -, aber die Kontrollabfrage
--   PRAGMA foreign_key_check am Ende ist nur mit eingeschalteten
--   Fremdschluesseln aussagekraeftig.
--
-- IDEMPOTENZ
--   Ein zweiter Lauf aendert nichts: alle DELETE-Bedingungen treffen dann keine
--   Zeile mehr, das UPDATE auf Tab_Applikation setzt einen bereits gesetzten
--   Wert. Nachweis: sql/tools/Reduziere-Testdatenbank.probe.py.
--
-- ============================================================================
-- WAS GELOESCHT WIRD
-- ============================================================================
--
-- Stufe 1 - Tab_Projekt selbst
--   DELETE FROM Tab_Projekt WHERE ID NOT IN (Behalten-Liste).
--   Tab_Projekt.ID ist INTEGER PRIMARY KEY AUTOINCREMENT, also nie 0 und nie
--   NULL. Daraus folgt die unten durchgaengig verwendete Regel: eine Zeile mit
--   ID_Projekt = 0 (oder NULL) gehoert KEINEM Projekt - sie ist Katalog- oder
--   Vorgabezeile und bleibt.
--
-- Stufe 1a - 19 Tabellen mit FOREIGN KEY ... REFERENCES Tab_Projekt ON DELETE
--            CASCADE. Sie werden vom DELETE der Stufe 1 automatisch geleert:
--     Tab_Einstellungen           (ID_Projekt)   Z_ProjektGebaeude
--     Tab_Energieanlagen          (ID_Projekt)   Z_ProjektPufferSp
--     Tab_Klimaregion             (ID_Projekt)   Z_ProjektSolarganglinie
--     Tab_Kostenprofil            (ID_Projekt)   Z_ProjektStromganglinie
--     Tab_Preisreihe              (ID_Projekt)   Z_ProjektWaermebedarf
--     Tab_ProjektTarif            (ID_Projekt)   Z_Projekt_Brauchwasser
--     Tab_ProjektWerte            (ProjektID!)   Z_Projekt_Prozesswaerme
--     Tab_ProjektWirtschaftlichkeit              Z_Projekt_Stromverbraucher
--     Tab_Pufferspeicher          (ID_Projekt)   energy_price
--                                                energy_project_settings
--   Achtung: Tab_ProjektWerte verknuepft ueber die Spalte ProjektID, nicht
--   ID_Projekt - als einzige der neunzehn.
--
-- Stufe 1b - dieselben 19 Tabellen noch einmal explizit (Sicherheitsnetz, falls
--   PRAGMA foreign_keys nicht greift). In einer intakten Datenbank mit
--   eingeschalteten Fremdschluesseln treffen diese Anweisungen keine Zeile mehr.
--
-- Stufe 2 - 26 Tabellen mit Spalte ID_Projekt OHNE Fremdschluessel; hier muss
--   von Hand geloescht werden. Aus den 28 Tabellen des Schemas, die ID_Projekt
--   ohne FK tragen, sind zwei ausgenommen (siehe "WAS BLEIBT"):
--     Tab_BHKW                    Tab_Kenndaten            Tab_Solarganglinie
--     Tab_Brauchwasser            Tab_Klimadaten           Tab_Solarkollektoren
--     Tab_Brauchwassertyp         Tab_PV                   Tab_Stromganglinie
--     Tab_Ergebnis                Tab_ProjektPhotovoltaik  Tab_Stromspeicher
--     Tab_ErgebnisStromMatrix     Tab_Prozesstyp           Tab_Stromverbraucher
--     Tab_ErgebnisWirtSensitivitaet  Tab_Prozesswaerme     Tab_Stromverbrauchertyp
--     Tab_ErgebnisWirtschaftlichkeit Tab_Quellprofil       Tab_Variante
--     Tab_Gebaeude                Tab_Solar                Tab_WP
--     Tab_Heizkessel                                       Tab_Waermebedarf
--
--   Dazu kommt eine 27. Tabelle, die nicht in der Vormessung stand, weil sie die
--   Spalte anders nennt:
--     Berichtskonfiguration       (ProjektID, ohne Fremdschluessel)
--
--   Jede dieser Anweisungen schont Katalogzeilen:
--     WHERE <spalte> IS NOT NULL AND <spalte> <> 0 AND <spalte> NOT IN (behalten)
--   Begruendung: Der Katalogmarker ist je Tabelle verschieden, weil das Schema
--   aus Access stammt. Drei Faelle:
--     a) ID_Projekt NOT NULL DEFAULT 0 -> Marker ist die 0. Betrifft die elf
--        Katalogtabellen mit Projektkopien: Tab_Brauchwassertyp, Tab_Prozesstyp,
--        Tab_Stromverbrauchertyp, Tab_Klimadaten, Tab_Solarkollektoren,
--        Tab_Stromganglinie, Tab_Solarganglinie, Tab_Waermebedarf,
--        Tab_Gebaeude, Tab_Kenndaten, Tab_Stromverbraucher.
--     b) ID_Projekt NOT NULL ohne DEFAULT -> NULL ist unmoeglich, der Marker
--        muss also ebenfalls die 0 sein: Tab_Brauchwasser, Tab_Heizkessel,
--        Tab_PV, Tab_Prozesswaerme, Tab_Solar, Tab_Stromspeicher.
--     c) ID_Projekt ohne NOT NULL und ohne DEFAULT -> Marker ist NULL:
--        Tab_BHKW, Tab_WP, Tab_Quellprofil, Tab_Variante,
--        Tab_ProjektPhotovoltaik, Tab_Ergebnis und die drei uebrigen
--        Tab_Ergebnis*-Tabellen (sowie Tab_Applikation, siehe unten).
--   Die Bedingung oben faengt alle drei Faelle in einem Zug ab. Dass 0 wie NULL
--   behandelt werden darf, folgt aus Tab_Projekt.ID AUTOINCREMENT: eine
--   Projekt-ID 0 kann es nicht geben.
--
-- Stufe 3 - zweite Ebene: Detailtabellen ohne eigene Projektspalte, die an den
--   Tabellen der Stufe 2 haengen. Sie sind die Masse der Datei (8760 Zeilen je
--   Ganglinie). Alle haben ON DELETE CASCADE, werden also bei eingeschalteten
--   Fremdschluesseln bereits mit der Elternzeile entfernt; die Anweisungen unten
--   sind Sicherheitsnetz und raeumen zugleich Waisen aus der Access-Zeit weg,
--   die PRAGMA foreign_key_check sonst melden wuerde:
--     Tab_StromganglinieDaten  -> Tab_Stromganglinie.ID   (ID_Ganglinie)
--     Tab_SolarganglinieDaten  -> Tab_Solarganglinie.ID   (ID_Ganglinie)
--     Tab_WaermebedarfDaten    -> Tab_Waermebedarf.ID     (ID_Ganglinie)
--     Tab_QuellprofilDaten     -> Tab_Quellprofil.ID      (ID_Quellprofil)
--     Tab_PreisreiheDaten      -> Tab_Preisreihe.ID       (ID_Preisreihe)
--     Tab_DBTagV               -> Tab_Gebaeude.ID         (ID_Gebaeude)
--     Tab_DBTagVDaten          -> Tab_DBTagV.ID           (ID_TagV)
--     Tab_Kenndaten            -> Tab_WP.ID               (ID_WP)
--     Tab_Kenndaten_Kuehlung   -> Tab_WP.ID               (ID_WP)
--     Tab_ErgebnisBHKW / -Energiebedarf / -Heizkessel / -Photovoltaik /
--     -Pufferspeicher / -Solarthermie / -Stromspeicher / -Waermepumpe
--                              -> Tab_Ergebnis.ID         (ID_Ergebnis)
--     Tab_ErgebnisBHKWModul / -HeizkesselModul / -PhotovoltaikModul /
--     -SolarthermieModul / -WaermepumpeModul -> jeweilige Ergebnis-Detailzeile
--     Tab_StromspeicherVariante-> Tab_Energieanlagen.ID   (ID_Energieanlage)
--     Z_AnlagePufferVerbund    -> Tab_Energieanlagen.ID   (ID_Anlage)
--     Z_AnlageSenke            -> Tab_Energieanlagen.ID   (ID_Anlage)
--   Von diesen Verweisspalten tragen genau zwei ein DEFAULT 0
--   (Tab_DBTagV.ID_Gebaeude, Tab_Kenndaten.ID_WP); dort bedeutet 0 "kein
--   Bezug" und wird wie NULL geschont. Alle uebrigen sind ohne DEFAULT.
--
-- ============================================================================
-- WAS BLEIBT
-- ============================================================================
--
--   * Alle *_STAMM-Tabellen. Sie sind der Auslieferungskatalog und haben mit
--     Projekten nichts zu tun - auch dann nicht, wenn sie aus Access-Zeiten eine
--     Spalte ID_Projekt mitschleppen. Betroffen ist genau eine Tabelle:
--     Tab_Kenndaten_Kuehlung_STAMM (ID_Projekt INTEGER DEFAULT 0). Sie haengt
--     ueber ID_WP an Tab_WP_STAMM und wird deshalb NICHT angefasst.
--   * Tab_Applikation. Einzeilige Statustabelle (Schemastand, Version,
--     Emissionsmodus). Sie wird nicht geleert, sondern nur umgehaengt:
--     ID_Projekt zeigt danach auf 1030, ein Projekt der Behalten-Liste.
--   * Tab_Einstellungen wird nur REDUZIERT, nicht geleert: die Tabelle ist
--     projektbezogen (FK auf Tab_Projekt, ON DELETE CASCADE), fuehrt aber
--     ID_Projekt NOT NULL DEFAULT 0. Zeilen mit ID_Projekt = 0 sind die globalen
--     Vorgaben und bleiben stehen.
--   * Alle uebrigen Kataloge ohne Projektbezug: Tab_Brennstoff_Stamm,
--     Tab_BrennstoffKategorien, Tab_Gesetzesparameter, Tab_Kostenfaktor,
--     Tab_KostenGruppenKatalog, Tab_KostenKomponente, Tab_KostenVorlage(Position),
--     Tab_Kraftwerkspark, Tab_Typ_Energieanlagen, emissionsart, emissionswert,
--     energy_carrier, energy_conversion, pricing_model.
--   * Alle Views (14 Stueck) bleiben unveraendert; sie enthalten keine Daten.
--
-- ============================================================================
-- BEKANNTE GRENZEN
-- ============================================================================
--
--   1. TEXTVERKNUEPFUNGEN. Das Schema verknuepft an mehreren Stellen ueber
--      Textfelder statt ueber IDs (CLAUDE.md: "Verknuepfungen laufen vielfach
--      ueber Textfelder (Bezeichner, Typname) statt ueber IDs"). Solche Bezuege
--      loest dieses Skript NICHT auf und loescht danach auch nichts:
--        - Tab_Applikation.Projektname (Teil des Primaerschluessels) traegt den
--          Projektnamen als Text, ohne Bezug zu Tab_Projekt.Projektname.
--        - Tab_ProjektWerte.Gruppe verweist per Text auf
--          Tab_KostenGruppenKatalog.GruppenName (Fremdschluessel auf eine
--          Textspalte, ON DELETE NO ACTION) - Katalogseite, bleibt ohnehin.
--        - Geraete- und Typbezeichner (Typname, Bezeichner) in den Anlagen- und
--          Katalogtabellen. Nach der Reduzierung koennen dort Namen stehen, zu
--          denen kein Katalogeintrag mehr existiert bzw. umgekehrt Katalog-
--          eintraege, die kein Projekt mehr benutzt. Beides ist harmlos - der
--          Katalog wird vollstaendig behalten -, aber es ist die einzige
--          verbleibende Quelle fuer "Waisen", die PRAGMA foreign_key_check
--          nicht sieht, weil es dort keine Fremdschluessel gibt.
--   2. OPTIONALE ID-VERWEISE OHNE FREMDSCHLUESSEL werden bewusst nicht
--      aufgeraeumt, weil dort 0/NULL "kein Bezug" bedeutet und ein NOT-IN-Loeschen
--      gueltige Zeilen treffen koennte:
--        Tab_ErgebnisPufferspeicher.ID_Pufferspeicher, .ID_Anlage
--        Tab_ErgebnisStromspeicher.ID_Energieanlage
--        Tab_StromspeicherVariante.ID_Preisreihe, .ID_Kostenprofil
--        Z_AnlagePufferVerbund.ID_Senke
--        Tab_Gebaeude.ID_ProjektGebaeude (DEFAULT 0)
--      Alle diese Zeilen verschwinden in der Praxis ohnehin ueber ihren
--      Hauptbezug (ID_Ergebnis bzw. ID_Energieanlage bzw. ID_Projekt).
--   3. VORBESTEHENDE FK-VERLETZUNGEN aus der Access-Zeit blockieren den COMMIT
--      nicht (SQLite zaehlt nur Verletzungen, die die laufende Transaktion
--      erzeugt). Sie tauchen aber in PRAGMA foreign_key_check auf. Bekannter
--      Kandidat: Zeilen mit ID_Projekt = 0 in den Stufe-1a-Tabellen
--      (Tab_Einstellungen, Tab_Klimaregion, energy_price,
--      energy_project_settings, Z_ProjektPufferSp - alle DEFAULT 0), denn ein
--      Projekt 0 gibt es nicht. Dieses Skript laesst sie absichtlich stehen: sie
--      sind Vorgabe- bzw. Katalogzeilen.
--   4. ECHTE PROJEKTUEBERGREIFENDE VERWEISE lassen den COMMIT scheitern - mit
--      Absicht. Beispiel: eine Energieanlage eines behaltenen Projekts, die auf
--      einen Pufferspeicher eines geloeschten Projekts zeigt
--      (Tab_Energieanlagen.ID_PUFFER / WQ_ID_Puffer / WS_ID_Puffer /
--      WS_ID_Puffer2 -> Tab_Pufferspeicher, ON DELETE NO ACTION). In diesem Fall
--      bricht COMMIT mit "FOREIGN KEY constraint failed" ab, die Kopie bleibt
--      unveraendert, und der Datenbestand muss von Hand geprueft werden.
--   5. Tab_Variante.ID_ProjektRef verweist ohne Fremdschluessel auf ein zweites
--      Projekt (Variantenvergleich). Zeigt es nach der Reduzierung ins Leere,
--      wird es auf NULL gesetzt - siehe Stufe 2b.
--
-- ============================================================================


-- ----------------------------------------------------------------------------
-- Fremdschluessel einschalten. MUSS ausserhalb einer Transaktion stehen,
-- innerhalb ist das PRAGMA wirkungslos. Kontrolle: siehe KONTROLLSCHRITT 1.
-- ----------------------------------------------------------------------------
PRAGMA foreign_keys = ON;


-- ----------------------------------------------------------------------------
-- Behalten-Liste. Temporaere Tabelle, damit die dreizehn IDs genau einmal im
-- Skript stehen. DROP IF EXISTS, damit ein zweiter Lauf in derselben Sitzung
-- (SQLiteStudio haelt die Verbindung offen) nicht an "table already exists"
-- scheitert.
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS temp.behalten;

CREATE TEMP TABLE behalten (ID INTEGER PRIMARY KEY);

INSERT INTO behalten (ID) VALUES
    (1007), (1008), (1011), (1017), (1018), (1021), (1023),
    (1024), (1030), (1039), (1040), (1041), (1042);


BEGIN;

-- Innerhalb der Transaktion die FK-Pruefung bis zum COMMIT aufschieben. Die
-- Kaskaden laufen weiterhin; nur der Zeitpunkt der Pruefung verschiebt sich.
-- Das macht die Reihenfolge der DELETEs unkritisch - sonst koennte ein
-- ON DELETE NO ACTION (z. B. Tab_Energieanlagen -> Tab_Pufferspeicher) eine
-- Anweisung scheitern lassen, obwohl eine spaetere Anweisung die verletzende
-- Zeile ohnehin entfernt. Das PRAGMA setzt sich mit dem COMMIT selbst zurueck.
PRAGMA defer_foreign_keys = ON;


-- ============================================================================
-- STUFE 1 - Projekte. Loest die neunzehn ON-DELETE-CASCADE-Ketten aus.
-- ============================================================================
DELETE FROM "Tab_Projekt"
 WHERE "ID" NOT IN (SELECT "ID" FROM behalten);


-- ============================================================================
-- STUFE 1b - Sicherheitsnetz fuer die neunzehn Kaskadentabellen.
-- Trifft nichts, wenn Stufe 1 mit eingeschalteten Fremdschluesseln lief.
-- Reihenfolge: Tab_Energieanlagen vor Tab_Pufferspeicher (NO ACTION).
-- ============================================================================
DELETE FROM "Tab_Energieanlagen"           WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Z_ProjektPufferSp"            WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Pufferspeicher"           WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Einstellungen"            WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Klimaregion"              WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Kostenprofil"             WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Preisreihe"               WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_ProjektTarif"             WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_ProjektWirtschaftlichkeit" WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Z_ProjektGebaeude"            WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Z_ProjektSolarganglinie"      WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Z_ProjektStromganglinie"      WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Z_ProjektWaermebedarf"        WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Z_Projekt_Brauchwasser"       WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Z_Projekt_Prozesswaerme"      WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Z_Projekt_Stromverbraucher"   WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "energy_price"                 WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "energy_project_settings"      WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
-- Einzige der neunzehn mit abweichendem Spaltennamen:
DELETE FROM "Tab_ProjektWerte"             WHERE "ProjektID"  IS NOT NULL AND "ProjektID"  <> 0 AND "ProjektID"  NOT IN (SELECT "ID" FROM behalten);


-- ============================================================================
-- STUFE 2 - Tabellen mit Projektspalte OHNE Fremdschluessel.
-- Hier passiert die eigentliche Arbeit; nichts davon laeuft ueber Kaskaden.
-- ============================================================================

-- --- Ergebnisse (raeumen ueber Kaskade 15 Detailtabellen mit ab) ------------
DELETE FROM "Tab_Ergebnis"                 WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_ErgebnisStromMatrix"      WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_ErgebnisWirtSensitivitaet" WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_ErgebnisWirtschaftlichkeit" WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);

-- --- Anlagen und Komponenten ------------------------------------------------
DELETE FROM "Tab_BHKW"                     WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Heizkessel"               WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_WP"                       WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_PV"                       WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_ProjektPhotovoltaik"      WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Solarkollektoren"         WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Stromspeicher"            WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Quellprofil"              WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Variante"                 WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);

-- --- Bedarfe, Ganglinien, Projektkopien der Kataloge -------------------------
DELETE FROM "Tab_Waermebedarf"             WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Stromganglinie"           WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Solarganglinie"           WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Brauchwasser"             WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Brauchwassertyp"          WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Prozesswaerme"            WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Prozesstyp"               WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Stromverbraucher"         WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Stromverbrauchertyp"      WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Gebaeude"                 WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Klimadaten"               WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Solar"                    WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);
DELETE FROM "Tab_Kenndaten"                WHERE "ID_Projekt" IS NOT NULL AND "ID_Projekt" <> 0 AND "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);

-- --- Sonstiges: Spalte heisst ProjektID, kein Fremdschluessel ----------------
DELETE FROM "Berichtskonfiguration"        WHERE "ProjektID"  IS NOT NULL AND "ProjektID"  <> 0 AND "ProjektID"  NOT IN (SELECT "ID" FROM behalten);


-- ============================================================================
-- STUFE 2b - nicht loeschen, sondern umhaengen.
-- ============================================================================

-- Tab_Applikation ist die einzeilige Statustabelle (Schemastand, Version).
-- Sie wird auf ein behaltenes Projekt gesetzt statt geleert.
-- Zeilen mit ID_Projekt IS NULL bleiben unangetastet (NULL NOT IN (...) ist
-- weder wahr noch falsch): dort ist kein Projekt geoeffnet, das ist gueltig.
UPDATE "Tab_Applikation"
   SET "ID_Projekt" = 1030
 WHERE "ID_Projekt" NOT IN (SELECT "ID" FROM behalten);

-- Variantenvergleich: Verweis auf ein geloeschtes Zweitprojekt wird geleert.
UPDATE "Tab_Variante"
   SET "ID_ProjektRef" = NULL
 WHERE "ID_ProjektRef" IS NOT NULL
   AND "ID_ProjektRef" <> 0
   AND "ID_ProjektRef" NOT IN (SELECT "ID" FROM behalten);


-- ============================================================================
-- STUFE 3 - zweite Ebene: Detailtabellen ohne eigene Projektspalte.
-- Reihenfolge Eltern vor Kind. Alle Bezuege haben ON DELETE CASCADE; diese
-- Anweisungen treffen daher nur vorbestehende Waisen bzw. springen ein, wenn
-- PRAGMA foreign_keys nicht griff.
-- ============================================================================

-- --- Ganglinien und Profile (die Masse der Datei) ---------------------------
DELETE FROM "Tab_StromganglinieDaten" WHERE "ID_Ganglinie"   IS NOT NULL AND "ID_Ganglinie"   NOT IN (SELECT "ID" FROM "Tab_Stromganglinie");
DELETE FROM "Tab_SolarganglinieDaten" WHERE "ID_Ganglinie"   IS NOT NULL AND "ID_Ganglinie"   NOT IN (SELECT "ID" FROM "Tab_Solarganglinie");
DELETE FROM "Tab_WaermebedarfDaten"   WHERE "ID_Ganglinie"   IS NOT NULL AND "ID_Ganglinie"   NOT IN (SELECT "ID" FROM "Tab_Waermebedarf");
DELETE FROM "Tab_QuellprofilDaten"    WHERE "ID_Quellprofil" IS NOT NULL AND "ID_Quellprofil" NOT IN (SELECT "ID" FROM "Tab_Quellprofil");
DELETE FROM "Tab_PreisreiheDaten"     WHERE "ID_Preisreihe"  IS NOT NULL AND "ID_Preisreihe"  NOT IN (SELECT "ID" FROM "Tab_Preisreihe");

-- --- Gebaeude -> Tagesverlaeufe (zwei Ebenen, Eltern zuerst) ----------------
-- Tab_DBTagV.ID_Gebaeude ist NOT NULL DEFAULT 0; die 0 bedeutet "kein Bezug"
-- und wird deshalb - wie bei ID_Projekt - geschont.
DELETE FROM "Tab_DBTagV"      WHERE "ID_Gebaeude" IS NOT NULL AND "ID_Gebaeude" <> 0 AND "ID_Gebaeude" NOT IN (SELECT "ID" FROM "Tab_Gebaeude");
DELETE FROM "Tab_DBTagVDaten" WHERE "ID_TagV"     IS NOT NULL AND "ID_TagV"     NOT IN (SELECT "ID" FROM "Tab_DBTagV");

-- --- Waermepumpen-Kennfelder ------------------------------------------------
-- Tab_Kenndaten.ID_WP ist NOT NULL DEFAULT 0 - 0 wird geschont;
-- Tab_Kenndaten_Kuehlung.ID_WP ist ohne DEFAULT, dort genuegt IS NOT NULL.
DELETE FROM "Tab_Kenndaten"          WHERE "ID_WP" IS NOT NULL AND "ID_WP" <> 0 AND "ID_WP" NOT IN (SELECT "ID" FROM "Tab_WP");
DELETE FROM "Tab_Kenndaten_Kuehlung" WHERE "ID_WP" IS NOT NULL AND "ID_WP" NOT IN (SELECT "ID" FROM "Tab_WP");

-- --- Ergebnis-Detailtabellen ------------------------------------------------
DELETE FROM "Tab_ErgebnisBHKW"           WHERE "ID_Ergebnis" IS NOT NULL AND "ID_Ergebnis" NOT IN (SELECT "ID" FROM "Tab_Ergebnis");
DELETE FROM "Tab_ErgebnisEnergiebedarf"  WHERE "ID_Ergebnis" IS NOT NULL AND "ID_Ergebnis" NOT IN (SELECT "ID" FROM "Tab_Ergebnis");
DELETE FROM "Tab_ErgebnisHeizkessel"     WHERE "ID_Ergebnis" IS NOT NULL AND "ID_Ergebnis" NOT IN (SELECT "ID" FROM "Tab_Ergebnis");
DELETE FROM "Tab_ErgebnisPhotovoltaik"   WHERE "ID_Ergebnis" IS NOT NULL AND "ID_Ergebnis" NOT IN (SELECT "ID" FROM "Tab_Ergebnis");
DELETE FROM "Tab_ErgebnisPufferspeicher" WHERE "ID_Ergebnis" IS NOT NULL AND "ID_Ergebnis" NOT IN (SELECT "ID" FROM "Tab_Ergebnis");
DELETE FROM "Tab_ErgebnisSolarthermie"   WHERE "ID_Ergebnis" IS NOT NULL AND "ID_Ergebnis" NOT IN (SELECT "ID" FROM "Tab_Ergebnis");
DELETE FROM "Tab_ErgebnisStromspeicher"  WHERE "ID_Ergebnis" IS NOT NULL AND "ID_Ergebnis" NOT IN (SELECT "ID" FROM "Tab_Ergebnis");
DELETE FROM "Tab_ErgebnisWaermepumpe"    WHERE "ID_Ergebnis" IS NOT NULL AND "ID_Ergebnis" NOT IN (SELECT "ID" FROM "Tab_Ergebnis");

-- --- Modulzeilen (dritte Ebene, nach den Detailtabellen) --------------------
DELETE FROM "Tab_ErgebnisBHKWModul"         WHERE "ID_ErgebnisBHKW"         IS NOT NULL AND "ID_ErgebnisBHKW"         NOT IN (SELECT "ID" FROM "Tab_ErgebnisBHKW");
DELETE FROM "Tab_ErgebnisHeizkesselModul"   WHERE "ID_ErgebnisHeizkessel"   IS NOT NULL AND "ID_ErgebnisHeizkessel"   NOT IN (SELECT "ID" FROM "Tab_ErgebnisHeizkessel");
DELETE FROM "Tab_ErgebnisPhotovoltaikModul" WHERE "ID_ErgebnisPhotovoltaik" IS NOT NULL AND "ID_ErgebnisPhotovoltaik" NOT IN (SELECT "ID" FROM "Tab_ErgebnisPhotovoltaik");
DELETE FROM "Tab_ErgebnisSolarthermieModul" WHERE "ID_ErgebnisSolarthermie" IS NOT NULL AND "ID_ErgebnisSolarthermie" NOT IN (SELECT "ID" FROM "Tab_ErgebnisSolarthermie");
DELETE FROM "Tab_ErgebnisWaermepumpeModul"  WHERE "ID_ErgebnisWaermepumpe"  IS NOT NULL AND "ID_ErgebnisWaermepumpe"  NOT IN (SELECT "ID" FROM "Tab_ErgebnisWaermepumpe");

-- --- Anhaengsel der Energieanlagen ------------------------------------------
DELETE FROM "Tab_StromspeicherVariante" WHERE "ID_Energieanlage" IS NOT NULL AND "ID_Energieanlage" NOT IN (SELECT "ID" FROM "Tab_Energieanlagen");
DELETE FROM "Z_AnlagePufferVerbund"     WHERE "ID_Anlage"        IS NOT NULL AND "ID_Anlage"        NOT IN (SELECT "ID" FROM "Tab_Energieanlagen");
DELETE FROM "Z_AnlageSenke"             WHERE "ID_Anlage"        IS NOT NULL AND "ID_Anlage"        NOT IN (SELECT "ID" FROM "Tab_Energieanlagen");


COMMIT;


-- ----------------------------------------------------------------------------
-- VACUUM schreibt die Datei neu und gibt den freigewordenen Platz zurueck.
-- Muss NACH dem COMMIT stehen - VACUUM ist innerhalb einer Transaktion nicht
-- erlaubt. Hier faellt der Groessengewinn an (Ziel: deutlich unter 100 MB,
-- der GitHub-Grenze fuer Einzeldateien).
-- ----------------------------------------------------------------------------
VACUUM;

DROP TABLE IF EXISTS temp.behalten;


-- ============================================================================
-- KONTROLLABFRAGEN (zum Kopieren; erwartete Ergebnisse rechts)
-- ============================================================================
--
--   SELECT COUNT(*) FROM Tab_Projekt;
--       -> 13
--
--   SELECT group_concat(ID, ',') FROM (SELECT ID FROM Tab_Projekt ORDER BY ID);
--       -> 1007,1008,1011,1017,1018,1021,1023,1024,1030,1039,1040,1041,1042
--
--   PRAGMA foreign_key_check;
--       -> keine Zeile (leere Ausgabe). Meldet sie etwas, sind es entweder
--          vorbestehende Waisen aus der Access-Zeit (siehe GRENZEN 3) oder ein
--          echter projektuebergreifender Verweis (GRENZEN 4).
--
--   PRAGMA integrity_check;
--       -> ok
--
--   SELECT page_count * page_size / 1048576.0 AS MB
--     FROM pragma_page_count(), pragma_page_size();
--       -> Dateigroesse in MB nach dem VACUUM. Muss unter 100 liegen, sonst
--          nimmt GitHub die Datei nicht an.
--
--   SELECT ID_Projekt FROM Tab_Applikation;
--       -> 1030 (bzw. mehrfach 1030, falls die Tabelle mehrere Zeilen fuehrt)
--
--   -- Gegenprobe: keine Projektzeile ausserhalb der dreizehn, Beispiel
--   SELECT COUNT(*) FROM Tab_Ergebnis
--    WHERE ID_Projekt IS NOT NULL AND ID_Projekt <> 0
--      AND ID_Projekt NOT IN (1007,1008,1011,1017,1018,1021,1023,1024,1030,1039,1040,1041,1042);
--       -> 0
--
-- Danach: Referenzlauf gegen die reduzierte Datenbank, erwartet 332/332
-- byte-gleich gegen Referenzlaeufe/2026-08-30_B3-Kaskade.
-- ============================================================================

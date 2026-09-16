-- ============================================================================
-- Bereinige-Pufferdubletten.sql
--
-- ZWECK
--   Entfernt aus Z_ProjektPufferSp jede Zeile, die eine WORTGLEICHE Wiederholung
--   einer anderen Zeile ist: gleiches Projekt, gleicher Pufferspeicher (Id und
--   Bezeichner), gleicher Erzeuger, gleiche Vor- und Ruecklauftemperatur,
--   gleiche Prioritaet, gleiche Schwellen. Von jeder solchen Gruppe bleibt die
--   Zeile mit der KLEINSTEN Id stehen; entfernt werden die uebrigen - also die
--   jeweils zweite und jede weitere.
--
--   In Referenzlaeufe/Kenndaten_Test.sqlite trifft das fuenf Zeilen:
--
--     Projekt 1007  Puffer 1007007  Solarthermie   10060 bleibt, 10172 faellt
--     Projekt 1008  Puffer 1008007  BHKW           10057 bleibt, 10071 faellt
--     Projekt 1008  Puffer 1008007  Waermepumpe    10058 bleibt, 10072 faellt
--     Projekt 1008  Puffer 1008008  Heizkessel     10059 bleibt, 10073 faellt
--     Projekt 1046  Puffer 1054215  Solarthermie   10374 bleibt, 10375 faellt
--
--   Das ist eine DATENBEREINIGUNG, kein Schemaschritt: Struktur, Indizes und
--   Schemastand der Datenbank bleiben unberuehrt.
--
-- WARUM ES NICHTS KOSTET
--   Eine wortgleiche Wiederholung traegt keine Angabe, die die verbleibende
--   Zeile nicht ebenso traegt - alle Spalten ausser der Id stimmen ueberein.
--   Wer sie liest, liest danach dasselbe.
--
--   Gelesen wird die Tabelle heute ohnehin nicht mehr je Erzeuger: Sie ist mit
--   Migrationsschritt 51 stillgelegt, die Simulation holt Senken und
--   Quellspeicher aus Z_AnlageSenke und Z_AnlagePufferVerbund. Was noch auf sie
--   zugreift, ist gegen Wiederholungen unempfindlich:
--     - GeraeteWaisen.Referenzen sammelt ID_Pufferspeicher in eine MENGE;
--     - Referenzlauf/Projektauswahl setzt daraus zwei Merkmale (Puffer
--       vorhanden, Puffer fuer Waermepumpe) - Wahrheitswerte;
--     - Referenzlauf/Migrationslauf zaehlt die Zeilen fuer eine Protokollzeile.
--   Auf Z_ProjektPufferSp.ID zeigt kein Fremdschluessel.
--
-- WAS DAS SKRIPT NICHT TUT
--   1. Es legt KEINEN eindeutigen Index an und macht keinen Schemaschritt. Ob
--      zwei Zeilen mit demselben Erzeuger je fachlich richtig sein koennen, ist
--      nicht belegt; ein Index wuerde diese Frage stillschweigend entscheiden.
--      Statt eines Zwangs wacht ein Testfall (PufferzuordnungWacheTests).
--   2. Es fasst keine Zeile an, die sich in irgendeiner Spalte von ihren
--      Geschwistern unterscheidet - auch nicht zwei Zeilen mit demselben
--      Erzeuger, aber anderen Temperaturen oder anderer Prioritaet.
--   3. Es fasst keine andere Tabelle an.
--
-- WIEDERHOLBARKEIT
--   Die Bedingung ist aus der Tabelle selbst gebildet. Nach dem ersten Lauf
--   gibt es keine Gruppe mit mehr als einer Zeile mehr; ein zweiter Lauf trifft
--   keine Zeile und aendert nichts.
--
-- AUFRUF - NIE VON HAND AUF DER ECHTEN DATEI
--   Der Laeufer prueft zuerst gegen eine Kopie und schreibt erst danach:
--     python3 sql/tools/Bereinige-Pufferdubletten.py --db <pfad>            (Probe)
--     python3 sql/tools/Bereinige-Pufferdubletten.py --db <pfad> --anwenden (Probe, dann Schreiben)
--   Das Skript enthaelt keine Pfadangabe und kein ATTACH - es wirkt immer auf
--   genau die Datenbank, die die aufrufende Sitzung geoeffnet hat.
--
-- SQL-DIALEKT
--   Nur SQLite-Kernsprache: DELETE, Unterabfrage, MIN, GROUP BY. Keine
--   Access-Schreibweise, kein Datumsliteral, kein Umlaut in einem Bezeichner.
--   GROUP BY fasst in SQLite gleiche NULL-Werte zusammen; die beiden
--   Schwellenspalten stehen in dieser Datenbank durchgaengig auf NULL und
--   gehoeren trotzdem in die Gruppierung, damit ein gepflegter Wert eine Zeile
--   schuetzt.
-- ============================================================================

DELETE FROM Z_ProjektPufferSp
WHERE ID NOT IN (
    SELECT MIN(ID)
    FROM Z_ProjektPufferSp
    GROUP BY ID_Projekt, ID_Pufferspeicher, Pufferspeicher, Erzeuger,
             Vorlauf, Ruecklauf, Prioritaet, Schwelle_Ein, Schwelle_Aus
);

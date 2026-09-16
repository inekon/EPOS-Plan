-- ============================================================================
-- Bereinige-Probierpuffer.sql
--
-- ZWECK
--   Entfernt aus Referenzlaeufe/Kenndaten_Test.sqlite die zwei Probierreste
--   "test" mit 2 Litern Gesamtvolumen und 4 000 EUR Investition samt der
--   leeren Anlagenzeile, an der jeder von ihnen haengt:
--
--     Tab_Pufferspeicher  1007009  Projekt 1007  "test"  2 Ltr  4 000 EUR
--     Tab_Pufferspeicher  1054216  Projekt 1046  "test"  2 Ltr  4 000 EUR
--     Tab_Energieanlagen    11238  Projekt 1007  "test"  ID_Type 12
--     Tab_Energieanlagen    14941  Projekt 1046  "test"  ID_Type 12
--
--   Das ist eine DATENBEREINIGUNG, kein Schemaschritt: Struktur und
--   Schemastand der Datenbank bleiben unberuehrt.
--
-- WARUM
--   Zwei Liter Nutzinhalt und 4 000 EUR Investition sind kein Speicher,
--   sondern ein Probierrest. Im hydraulischen Weg rechnen beide nicht mit -
--   sie stehen in keiner Zeile von Z_ProjektPufferSp. Ueber die Anlagenzeile
--   zaehlen sie aber sehr wohl: Die Baugroesse des Gewerks Pufferspeicher ist
--   das Gesamtvolumen, und die Summenabfrage dahinter geht ueber
--   Tab_Energieanlagen.ID_PUFFER, nicht ueber Z_ProjektPufferSp. In den
--   Projekten 1007 und 1046 liefert sie deshalb 1 380 l statt 1 378 l. Heute
--   fragt keine Kostenposition diese Summe ab; sobald jemand eine Position
--   mit EUR-je-Liter anlegt, zaehlen die zwei Liter sofort mit.
--
-- WAS DAS SKRIPT NICHT ANFASST
--   1. Den dritten Satz mit demselben Bezeichner: Tab_Pufferspeicher 1018022
--      (Projekt 1023, "test", 500 Ltr, 0 EUR). 500 Liter sind ein
--      plausibler Speicher; 1023 ist ausserdem kein Probierrest, sondern ein
--      gepflegtes Projekt.
--   2. Die zwei Zeilen in Tab_ErgebnisPufferspeicher, deren
--      ID_Pufferspeicher auf 1054216 zeigt (IDs 85 und 92). Sie gehoeren zum
--      gespeicherten Ergebniskopf 211 des Projekts 1044 und tragen den
--      Bezeichner eines ganz anderen Geraets - ihr Verweis ist schon heute
--      falsch und nicht erst nach dieser Bereinigung. Sie haengen damit NICHT
--      ausschliesslich an den zwei Probierresten, sondern sind Teil eines
--      fremden Ergebnisses. Die Spalte traegt in dieser Datenbank durchgaengig
--      unzuverlaessige Verweise (die Haelfte der 36 Zeilen zeigt auf einen
--      Speicher eines anderen Projekts oder ins Leere); sie hat keinen
--      Fremdschluessel, und der Ergebniskopf faellt samt seinen Zeilen, sobald
--      das Projekt neu gerechnet wird.
--
-- WIEDERHOLBARKEIT
--   Jede Anweisung ist ueber ihre Merkmale gebunden (Id, Projekt, Bezeichner,
--   Volumen, Investition). Ein zweiter Lauf trifft keine Zeile mehr und
--   aendert nichts. Eine Datenbank, in der dieselben Ids etwas anderes
--   bedeuten, bleibt unberuehrt: Alle fuenf Merkmale muessen zusammenpassen.
--
-- AUFRUF - NIE VON HAND AUF DER ECHTEN DATEI
--   Der Laeufer prueft zuerst gegen eine Kopie und schreibt erst danach:
--     python3 sql/tools/Bereinige-Probierpuffer.py --db <pfad>            (Probe)
--     python3 sql/tools/Bereinige-Probierpuffer.py --db <pfad> --anwenden (Probe, dann Schreiben)
--   Das Skript enthaelt keine Pfadangabe und kein ATTACH - es wirkt immer auf
--   genau die Datenbank, die die aufrufende Sitzung geoeffnet hat.
--
-- FREMDSCHLUESSEL
--   Der Laeufer schaltet PRAGMA foreign_keys ein. Die Reihenfolge unten ist
--   trotzdem so gewaehlt, dass sie auch ohne eingeschaltete Fremdschluessel
--   nichts Verwaistes hinterlaesst: erst die Zuordnungen, dann die
--   Anlagenzeile, zuletzt der Speicher.
-- ============================================================================

-- ---------------------------------------------------------------------------
-- Stufe 1 - Zuordnungen, die an der ANLAGENZEILE haengen
--   In der Testdatenbank trifft keine dieser Anweisungen eine Zeile; sie
--   stehen fuer den allgemeinen Fall, damit die Bereinigung auch in einer
--   gepflegten Datenbank keine Waise hinterlaesst.
-- ---------------------------------------------------------------------------
DELETE FROM Z_AnlageSenke
WHERE ID_Anlage IN (
    SELECT a.ID FROM Tab_Energieanlagen a
    INNER JOIN Tab_Pufferspeicher p ON p.ID = a.ID_PUFFER
    WHERE a.ID IN (11238, 14941) AND a.Bezeichner = 'test' AND a.ID_Type = 12
      AND p.ID IN (1007009, 1054216) AND p.Bezeichner = 'test'
      AND p.Gesamtvolumen = 2 AND p.Investitionskosten = 4000.0
      AND p.ID_Projekt IN (1007, 1046));

DELETE FROM Z_AnlagePufferVerbund
WHERE ID_Anlage IN (
    SELECT a.ID FROM Tab_Energieanlagen a
    INNER JOIN Tab_Pufferspeicher p ON p.ID = a.ID_PUFFER
    WHERE a.ID IN (11238, 14941) AND a.Bezeichner = 'test' AND a.ID_Type = 12
      AND p.ID IN (1007009, 1054216) AND p.Bezeichner = 'test'
      AND p.Gesamtvolumen = 2 AND p.Investitionskosten = 4000.0
      AND p.ID_Projekt IN (1007, 1046));

DELETE FROM Z_AnlageStrang
WHERE ID_Anlage IN (
    SELECT a.ID FROM Tab_Energieanlagen a
    INNER JOIN Tab_Pufferspeicher p ON p.ID = a.ID_PUFFER
    WHERE a.ID IN (11238, 14941) AND a.Bezeichner = 'test' AND a.ID_Type = 12
      AND p.ID IN (1007009, 1054216) AND p.Bezeichner = 'test'
      AND p.Gesamtvolumen = 2 AND p.Investitionskosten = 4000.0
      AND p.ID_Projekt IN (1007, 1046));

-- ---------------------------------------------------------------------------
-- Stufe 2 - Kostenpositionen, die an der ANLAGENZEILE verankert sind
--   Ebenfalls 0 Zeilen in der Testdatenbank. Eine Position, deren Anker
--   wegfaellt, waere danach keiner Anlage mehr zuzuordnen.
-- ---------------------------------------------------------------------------
DELETE FROM Tab_ProjektWerte
WHERE ID_Anlage IN (
    SELECT a.ID FROM Tab_Energieanlagen a
    INNER JOIN Tab_Pufferspeicher p ON p.ID = a.ID_PUFFER
    WHERE a.ID IN (11238, 14941) AND a.Bezeichner = 'test' AND a.ID_Type = 12
      AND p.ID IN (1007009, 1054216) AND p.Bezeichner = 'test'
      AND p.Gesamtvolumen = 2 AND p.Investitionskosten = 4000.0
      AND p.ID_Projekt IN (1007, 1046))
   OR ID_AnlageGeraet IN (
    SELECT a.ID FROM Tab_Energieanlagen a
    INNER JOIN Tab_Pufferspeicher p ON p.ID = a.ID_PUFFER
    WHERE a.ID IN (11238, 14941) AND a.Bezeichner = 'test' AND a.ID_Type = 12
      AND p.ID IN (1007009, 1054216) AND p.Bezeichner = 'test'
      AND p.Gesamtvolumen = 2 AND p.Investitionskosten = 4000.0
      AND p.ID_Projekt IN (1007, 1046));

-- ---------------------------------------------------------------------------
-- Stufe 3 - Die leere Anlagenzeile
--   ID_Type 12 ist das Gewerk Pufferspeicher; die Zeile traegt ausser
--   Bezeichner, Projekt und ID_PUFFER keinen gepflegten Wert.
-- ---------------------------------------------------------------------------
DELETE FROM Tab_Energieanlagen
WHERE ID IN (11238, 14941) AND Bezeichner = 'test' AND ID_Type = 12
  AND ID_Projekt IN (1007, 1046)
  AND ID_PUFFER IN (
    SELECT p.ID FROM Tab_Pufferspeicher p
    WHERE p.ID IN (1007009, 1054216) AND p.Bezeichner = 'test'
      AND p.Gesamtvolumen = 2 AND p.Investitionskosten = 4000.0
      AND p.ID_Projekt IN (1007, 1046));

-- ---------------------------------------------------------------------------
-- Stufe 4 - Die Zuordnung Projekt <-> Speicher
--   0 Zeilen in der Testdatenbank - genau das ist der Grund, warum die zwei
--   Saetze im hydraulischen Weg nicht mitrechnen. Die Anweisung steht fuer
--   den allgemeinen Fall.
-- ---------------------------------------------------------------------------
DELETE FROM Z_ProjektPufferSp
WHERE ID_Pufferspeicher IN (
    SELECT p.ID FROM Tab_Pufferspeicher p
    WHERE p.ID IN (1007009, 1054216) AND p.Bezeichner = 'test'
      AND p.Gesamtvolumen = 2 AND p.Investitionskosten = 4000.0
      AND p.ID_Projekt IN (1007, 1046));

-- ---------------------------------------------------------------------------
-- Stufe 5 - Die zwei Probierreste selbst
-- ---------------------------------------------------------------------------
DELETE FROM Tab_Pufferspeicher
WHERE ID IN (1007009, 1054216) AND Bezeichner = 'test'
  AND Gesamtvolumen = 2 AND Investitionskosten = 4000.0
  AND ID_Projekt IN (1007, 1046);

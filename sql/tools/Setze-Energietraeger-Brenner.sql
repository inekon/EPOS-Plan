-- ============================================================================
-- Setze-Energietraeger-Brenner.sql
--
-- ZWECK
--   Traegt an jeder BRENNER-Anlage (Tab_Energieanlagen.ID_Type 10 Heizkessel,
--   11 BHKW) den Energietraeger nach, der zum Brennstoff ihres Geraets gehoert:
--
--     Tab_Energieanlagen.ID_Carrier  <-  energy_carrier.id
--
--   Die Zuordnung laeuft ueber den Brennstoff des GERAETS -
--   Tab_Heizkessel.Brennstoff bzw. Tab_BHKW.Brennstoff - und von dort ueber
--   energy_carrier.ID_Brennstoff auf den Traeger.
--
--   Das ist eine DATENBEREINIGUNG, kein Schemaschritt: Struktur und
--   Schemastand der Datenbank bleiben unberuehrt.
--
-- WARUM
--   Die Simulation kennt genau eine Quelle fuer die Emissionsfaktoren eines
--   Brenners: den zugeordneten Energietraeger
--   (SimulationControl.EnergietraegerZuordnungLesen -> Emissionsquelle.Fuer).
--   Ist ID_Carrier leer, warnt der Lauf, das Ergebnismodul bekommt keine
--   carrier_id, und KostenEmissionRechner zaehlt die Anlage als
--   "Verbrauch ohne Traeger" mit kostenVollstaendig = false. Brennstoff,
--   Kosten und Emissionen dieser Anlagen lassen sich im Bericht keinem Traeger
--   zuordnen. Genau das schliesst dieses Skript.
--
-- WAS DAS SKRIPT NICHT ANFASST
--   1. Jede Anlage, die bereits einen Traeger traegt (ID_Carrier > 0). Ein
--      gepflegter Traeger ist ein Anwenderwert und wird nie ueberschrieben -
--      auch dann nicht, wenn er zu einem anderen Brennstoff gehoert als das
--      Geraet.
--   2. Waermepumpe (ID_Type 1), Solarthermie (2), Photovoltaik (3),
--      Stromspeicher (4) und Pufferspeicher (12). Diese Gewerke fuehren im
--      ganzen Bestand keinen ID_Carrier; das ist so vorgesehen
--      (ProjektEnergietraegerCtrl, WErzeugerCtrl) und keine Luecke.
--   3. Jede andere Spalte der Anlagenzeile.
--
-- DIE EINE AUSNAHME - BRENNSTOFF 13 "Elektrische Energie" -> TRAEGER 60
--   Zu Brennstoff 13 fuehrt der Katalog DREI Traeger: 54 "Strom Variante",
--   58 "Elektrische Energie 2" und 60 "Elektrische Energie". Die Aufloesung
--   ueber energy_carrier.ID_Brennstoff ist damit nicht eindeutig, und Stufe 2
--   laesst solche Anlagen bewusst stehen. Fuer diesen einen Fall waehlt
--   Stufe 1 den NAMENSGLEICHEN Traeger 60.
--
--   DAS IST EINE ANNAHME, KEINE ABLEITUNG. Sie steht hier, damit sie sichtbar
--   und umstossbar bleibt: Wer einen der beiden anderen Traeger fuer richtig
--   haelt, aendert die Zahl an dieser einen Stelle. Die drei Traeger
--   unterscheiden sich in Heizwert und Preis (54 fuehrt hs = 0, 58 und 60
--   fuehren hs = 1; die Projektuebersteuerungen in energy_project_settings
--   tragen je Traeger verschiedene Arbeits- und Grundpreise), in den
--   wirksamen Emissionsfaktoren dagegen nicht.
--
-- WIEDERHOLBARKEIT
--   Beide Stufen treffen nur Zeilen mit LEEREM Traeger und setzen einen Wert
--   groesser 0. Ein zweiter Lauf trifft keine Zeile mehr und aendert nichts.
--   Eine Datenbank ohne den erwarteten Katalog bleibt unberuehrt: Stufe 1
--   verlangt, dass Traeger 60 wirklich zu Brennstoff 13 gehoert, Stufe 2
--   verlangt Eindeutigkeit.
--
-- AUFRUF - NIE VON HAND AUF DER ECHTEN DATEI
--   Der Laeufer prueft zuerst gegen eine Kopie und schreibt erst danach:
--     python3 sql/tools/Setze-Energietraeger-Brenner.py --db <pfad>            (Probe)
--     python3 sql/tools/Setze-Energietraeger-Brenner.py --db <pfad> --anwenden (Probe, dann Schreiben)
--   Das Skript enthaelt keine Pfadangabe und kein ATTACH - es wirkt immer auf
--   genau die Datenbank, die die aufrufende Sitzung geoeffnet hat.
--
-- FREMDSCHLUESSEL
--   Tab_Energieanlagen.ID_Carrier traegt keinen Fremdschluessel auf
--   energy_carrier. Beide Stufen holen den Wert trotzdem aus dem Katalog
--   selbst; eine ID, die es dort nicht gibt, kann so nicht entstehen.
-- ============================================================================

-- ---------------------------------------------------------------------------
-- Stufe 1 - Die benannte Ausnahme: Brennstoff 13 -> Traeger 60
--   Die EXISTS-Bedingung bindet die 60 an den Katalog: Fuehrt eine Datenbank
--   den Traeger 60 nicht oder nicht zu Brennstoff 13, trifft die Anweisung
--   keine Zeile, statt eine falsche ID zu stempeln.
-- ---------------------------------------------------------------------------
UPDATE Tab_Energieanlagen
SET ID_Carrier = 60
WHERE ID_Type IN (10, 11)
  AND (ID_Carrier IS NULL OR ID_Carrier = 0)
  AND COALESCE(
        (SELECT k.Brennstoff FROM Tab_Heizkessel k WHERE k.ID = Tab_Energieanlagen.ID_Kessel),
        (SELECT b.Brennstoff FROM Tab_BHKW       b WHERE b.ID = Tab_Energieanlagen.ID_BHKW)) = 13
  AND EXISTS (SELECT 1 FROM energy_carrier ec WHERE ec.id = 60 AND ec.ID_Brennstoff = 13);

-- ---------------------------------------------------------------------------
-- Stufe 2 - Die EINDEUTIGE Aufloesung Brennstoff -> Traeger
--   Nur wo der Katalog zum Brennstoff des Geraets genau EINEN Traeger fuehrt,
--   wird er gesetzt. Jeder mehrdeutige Brennstoff bleibt stehen - der Laeufer
--   bricht davor ab und nennt die Kandidaten.
-- ---------------------------------------------------------------------------
UPDATE Tab_Energieanlagen
SET ID_Carrier = (
        SELECT ec.id FROM energy_carrier ec
        WHERE ec.ID_Brennstoff = COALESCE(
            (SELECT k.Brennstoff FROM Tab_Heizkessel k WHERE k.ID = Tab_Energieanlagen.ID_Kessel),
            (SELECT b.Brennstoff FROM Tab_BHKW       b WHERE b.ID = Tab_Energieanlagen.ID_BHKW)))
WHERE ID_Type IN (10, 11)
  AND (ID_Carrier IS NULL OR ID_Carrier = 0)
  AND COALESCE(
        (SELECT k.Brennstoff FROM Tab_Heizkessel k WHERE k.ID = Tab_Energieanlagen.ID_Kessel),
        (SELECT b.Brennstoff FROM Tab_BHKW       b WHERE b.ID = Tab_Energieanlagen.ID_BHKW)) IS NOT NULL
  AND (SELECT COUNT(*) FROM energy_carrier ec
       WHERE ec.ID_Brennstoff = COALESCE(
            (SELECT k.Brennstoff FROM Tab_Heizkessel k WHERE k.ID = Tab_Energieanlagen.ID_Kessel),
            (SELECT b.Brennstoff FROM Tab_BHKW       b WHERE b.ID = Tab_Energieanlagen.ID_BHKW))) = 1;

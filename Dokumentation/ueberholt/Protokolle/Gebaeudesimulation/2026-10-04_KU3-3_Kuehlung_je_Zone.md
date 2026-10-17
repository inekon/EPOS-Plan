# Protokoll KU3-3 — Kühlung je Zone (04.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle KU3-3 (E67, E68), ein Opus-Auftrag (176 Aufrufe), Commits `2f1611f`, `7786743`, Merge `3694194`. Statuszeile #715.
**Entscheid:** keiner neu beim Anwender; Entscheide der Orchestrierung (Abschnitt 4). Kein Schemaschritt, Basis R35 unverändert, Referenzlauf byte-gleich.

## 1 Auftrag

Die Kühlspalten der Zone (`Tab_Zone.Kuehl_*`) lesen und bedienbar machen (Kühlkonzept 3.5): Vererbung von Gebäude auf Zone, gleichzeitiges Heizen und Kühlen im Mehrzonenweg, Ergebnisse je Zone, Zonendialog, Bedarfsdialog, Bericht, Export.

## 2 Vorgehen

Ein Opus-Auftrag in zwei Commits (Rechenweg samt Ergebnissen, Dialoge samt Tests); ein konfliktfreier Merge; Statuszeile und Papiere aus der Orchestrierung.

## 3 Ergebnis

- **Vererbungsregel** (`GebaeudeModellEingang.KuehlungAufloesen`, gleich in `Zonenvorgaben.Bilden`): (1) der Projektschalter `Kuehlbetrieb` steht über allem; (2) `Tab_Zone.Kuehlung_Aktiv` NULL = wie Gebäude, 0 schaltet die Zone aus (auch wenn das Gebäude kühlt), 1 ein (auch wenn das Gebäude nicht kühlt); (3) `Kuehl_Sollwert` der Zone, sonst des Gebäudes, ohne beide aus; der Nachtwert ebenso vererbt, wirksam nur über den Kühlkalender; (4) `Kuehlleistung_Max` der Zone, sonst ab zwei Zonen die Gebäudegrenze nach Flächenanteil, sonst die Gebäudegrenze, sonst unbegrenzt; (5) eigene Kühlzeile oder eigener Kühlkalender der Zone gilt, sonst der des Gebäudes (`Vdi6007Rechenweg.KuehlungWirksamFuer`, `Konditionierungseingang.ZonenBestand`); (6) eine unbeheizte Zone kühlt nie.
- **Gleichzeitiges Heizen und Kühlen:** Gebäudesummen je Richtung sind die Summe der Zonen, nichts wird saldiert (F-K15, K6); neu am Gebäude des Mehrzonenwegs `GleichzeitigHeizenKwh` und `GleichzeitigKuehlenKwh` (Summen über die Stunden, in denen eine Zone heizt und eine kühlt) neben `StundenHeizenUndKuehlen`; Laufhinweis `SIMENG_KU3_ZONEN_GLEICHZEITIG`, wenn größer 0. Rechenprobe: 2 610 h, Heizen 3 661 kWh, Kühlen 4 583 kWh.
- **Ergebnisweg:** gespeichert nur der Kältebedarf je Zone (`Tab_ErgebnisZone.Kuehlenergie`, bestand); nur im Lauf Kältespitze und Kühlstunden je Zone (`Geb[n].Zone[k].KaeltespitzeKw`, `.StundenMitKuehlbedarf`) und am Gebäude `StundenHeizenUndKuehlen`, `GleichzeitigHeizenKwh`, `GleichzeitigKuehlenKwh` — Schlüssel nur bei wirksamer Kühlung (Referenzlauf byte-gleich).
- **Bericht und Export:** Tabelle „Kältebedarf je Zone“ unter der Zonentabelle aus dem gespeicherten Ergebnis, nur wenn eine Zone Kälte gespeichert hat; Export `KaeltebedarfKWh` je Zone, `KaeltelastW` je Zone bleibt leer (keine gespeicherte Spitze).
- **Dialoge:** Zonendialog mit neuer Gruppe „Kühlung der Zone“ (wie Gebäude / Ja / Nein, Kühlleistungsgrenze mit Leeranzeige Flächenanteil bzw. unbegrenzt, unbeheizte Zone mit Begründungszeile); Kühlsollwerte Tag/Nacht in der Spalte „Kühlen“ der Zonenmatrix wie am Gebäude (E56 F3 a), mit Hinweiszeile; Kühlspalte der Zone nicht mehr gesperrt (Zonenregel in `Konditionierungsarbeit` aufgehoben, Ressourcen `KOND_MSG_ZONE_KUEHLEN` und `KOND_TXT_KUEHLEN_ZONE` entfernt); Hülle `ZoneDaten` vier neue Felder, `GebaeudeKatalogHuelle`, `KonditionierungHuelle`; KI-Felder `kuehlung_aktiv`, `kuehlleistung_max` (Abdeckungswache 18 statt 16); Bedarfsdialog mit Zonentabelle (Kältebedarf, Kältespitze, Kühlstunden) und Zeile zum gleichzeitigen Heizen und Kühlen.
- **Tests:** `KuehlungJeZoneTests` (8), `KuehlungJeZoneDatenbankTests` (Kopie von 1052: Gästezimmer 0,986 MWh/a, Gastronomie mit `Kuehlung_Aktiv = 0` und Keller ohne Kälte, Gebäude = Summe), `ZonenKuehlungDialogTests` (6); vier Bestandstests nachgezogen.

## 4 Entscheide der Orchestrierung

- Die Kühlsollwerte der Zone stehen in der Zonenmatrix statt in der Gruppe (bestätigt; Abweichung vom Auftragswortlaut).
- Die Kältelast je Zone entsteht erst mit einer gespeicherten Spitze (Schemaschritt in KU3-4 prüfen).

## 5 Nachweise

Kern-Filter 958 grün, 7 rot durch die fehlende Testdatenbank 181 (auf `6f5367b` gegengeprüft); UI 260/260 und 323/323; Designer 14 822 ohne Befund; SQL 2 318/0; Referenzlauf 8 CI-Projekte und 1052 gegen R35 PASS, 287 Dateien byte-gleich; Windows-Schale 0 Fehler.

## 6 Offenes

- Sichtabnahme Zonendialog (Kühlgruppe) und Bedarfsdialog unter Windows beim Anwender.
- Testdatenbank 181 pushen; Gate und CI-Kennung nachtragen (Gate gemeinsam mit KU3-4a).
- Folgewellen: KU3-4a (Schemaschritt 183), KU3-4b (Referenzprojekt, Einfrierregel, Basis R36, Wiki, Logbuch), KU3-5 Kältespeicher.

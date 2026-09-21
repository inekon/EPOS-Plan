# KI‑F7 — Nachzüge aus den Wellen F4 und F5 (Protokoll, 21.09.2026)

Statuszeile #427 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Konzept
[`Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md`](../../../aktuell/Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md)
(§ 7 KI‑D‑Q7); Vorgänger [`KIF5_Erzeugerkataloge_Protokoll.md`](KIF5_Erzeugerkataloge_Protokoll.md) (Abschnitt „Offen, mit
Entscheid des Anwenders") und [`KIF4_Kosten_Wirtschaftlichkeit_Protokoll.md`](KIF4_Kosten_Wirtschaftlichkeit_Protokoll.md)
(drei Lücken der Kostenverwaltung). Zweig `ki-f7`, Commits `a720fad0` (Wärmepumpe und Photovoltaik), `f06af23f`
(Kostenverwaltung); Merge: siehe Statuszeile #427. Anwenderentscheid 21.09.2026: „Umsetzen".

## Wärmepumpe und Photovoltaik

- `Form_WP`, Feld `modulkosten`: nur lesbar, wie die Maske es zeigt. Nur `KiFeldSetzenTests` nutzte
  das Feld als setzbaren Fall (drei Stellen); sie nehmen jetzt `nennleistung` derselben Maske,
  dazu ein Fall für die Ablehnung und ein Katalogfall, dass beide Wärmepumpenmasken die
  Modulkosten nur lesbar führen.
- `Form_PV`, Auslegungstemperaturen: Sie liegen projektweit in `Tab_Einstellungen`
  (`Ausleg_T_Kalt`, `Ausleg_T_Heiss`); der Schreibweg läuft vom Strangbaustein über den Dialog in
  `KonfigurationCtrl.AuslegungstemperaturenSchreiben`. Neue Felder `auslegung_kalt` und
  `auslegung_heiss` (Zahl, °C, leer erlaubt, Kommentar nennt die Projektweite). Dafür bindet die
  Maske über die Sichtklasse `PhotovoltaikKiSicht` (dreizehn Felder als Durchreiche an die
  Erzeugerzeile, zwei über Delegaten an die lebenden Felder des Bausteins, Setzen über
  `InvokeAsync`); Eintrag in `OhneMarkupprobe` mit Begründung.
- Überlagerung „Anlagenwerte": eigener Katalogeintrag `Form_PV_Anlagenwerte` mit
  `wr_nennleistung`, `wr_eta10`, `wr_eta50`, `wr_eta100`, Daten-Objekt `PvAnlagenwerteKiSicht`
  (der Arbeitsstand des Fensters), Ziel wie `Form_PV`, Markup-Probe auf dem Strangbaustein bleibt.
  Der frühere Ausschluss nach Fachkonzept 11.6 steht umgeschrieben im Kommentar: Die Maske meldet
  sich nur an, solange die Überlagerung offen ist; OK und Abbrechen bleiben der Klick des Anwenders.
- `Form_PV`, Feld `modell_erweitert`: Wahl mit den zwei Einträgen der Maske („Einfach",
  „Erweitert"); die Eigenschaft bleibt der Wahrheitswert, die Sichtklasse trägt die Begleitwahl.

## Kostenverwaltung

Neue Sichtklasse `KostenKomponenteKiSicht`; alle bisherigen Felder unverändert (auch `variante`
aus #423 und das Raster). Drei neue Wahl-Felder: `komponentenwahl` (Einträge des Dialogs; nur
lesbar, ein Wechsel lädt einen anderen Positionssatz), `pv_verguetung` (übernehmen oder eigene;
nur lesbar, weil der Weg über die Hülle das Reiterblatt neu baut und den Vergütungsdialog öffnet),
`pv_projekt` (setzbar, wählt das Ziel des Knopfes; ohne gezeigte Liste benannt abgelehnt). Der
Baustein `ErtragBonus` bekam drei öffentliche Wege, damit der Wirt anmeldet und nicht das Blatt.

## Zahlen und Abnahme

Katalog 63 → 64 Masken, 724 → 733 Felder. 13 neue Ressourcenschlüssel je Sprache, zwei Texte
neu gefasst, Designer wiederholbar. Sechs neue bunit-Zeugen und vier Katalogfälle. Die Wache der
Markup-Probe prüft jetzt mindestens 65 statt 90 Feldpfade, weil `Form_PV` und die
Kostenverwaltung über Sichtklassen binden — die vom Entscheid gekaufte Folge, mit Herleitung im
Kommentar. Worktree: Kern-Filter 0 Fehler, 10 309 Tests grün (1 übersprungen). Gate im
Hauptbaum: siehe Statuszeile #427.

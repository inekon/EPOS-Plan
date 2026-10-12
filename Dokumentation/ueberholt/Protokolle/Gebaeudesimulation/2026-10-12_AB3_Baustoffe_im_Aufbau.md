# AB3 — Baustoffwahl im Bauteilaufbau statt Abschnitt „Baustoffe“

Wunsch des Anwenders vom 11.10.2026: Baustoffe und Bauteilkatalog gehören in die Administration
(Rubrik Gebäude), nicht in den Gebäudeimport; die Baustoffwahl soll mit dem Bauteilaufbau verbunden sein.

## Befund

- Administration › Gebäude führt „Baustoffe“ (`BaustoffKatalogDialog`) und „Bauteilaufbauten“
  (`BauteilaufbauDialog`) bereits (`EPOS.UI/Bausteine/Menuetabelle.cs`); am Menü wurde nichts geändert.
- Eine Schicht ohne Stoffwert erreichte die Liste „Bauteilaufbauten“ bisher nie: Ein Aufbau, dem nach Datei und
  Namensabgleich ein Wert fehlt, wird verworfen (Ersatzaufbau oder ohne Aufbau), und seine Dateischichten gingen
  verloren. Die Baustoffwahl an der Schicht brauchte deshalb diese Schichten im Vorschlag.

## Umsetzung

- Kern: `GebaeudeBauteilzeile.Dateischichten` (Record `GebaeudeDateischicht`) trägt bei unvollständigem Aufbau die
  Schichten der Datei innen → außen mit den Werten nach Datei und Abgleich — nur Anzeige, die Rechnung liest sie
  nicht. Namensabgleich N1–N7 und gemerkte Zuordnungen unverändert.
- Hülle: `GebaeudeAufbauHuelle.Dateischichtzeile` macht daraus Anzeigezeilen (`OhneWert`, Herkunft Datei, Katalog
  oder leer, Schlüssel des Materialnamens); die Liste fasst sie je Aufbauzeile zusammen.
- Dialog: Abschnitt „Baustoffe“ entfernt. Die aufgeklappte Zeile zeigt die Schichten des Aufbaus und, bei
  unvollständigem Aufbau, die Schichten der Datei. Fehlt ein Stoffwert, steht an der Schicht die Katalogwahl nach
  Stoffgruppe; sie schreibt die Zuordnung des Materialnamens in dasselbe Wörterbuch wie zuvor
  (`Baustoffzuordnungen`, Ergebnis, Merken beim Speichern der Gebäudeliste) und ordnet neu zu. Danach heißt die
  Herkunft „Zuordnung von Hand“; die eigene Zuordnung bleibt wählbar und lässt sich entfernen. Schichten mit Werten
  aus Datei oder Abgleich sind nur Anzeige. Über der Liste: „Baustoffe und Bauteilaufbauten pflegen Sie unter
  Administration › Gebäude.“ Der Sprung „Zur Baustoffzuordnung“ des Steckbriefs klappt die Zeile mit der Schicht auf.
  Der Knopf „Baustoff-Zuordnungen…“ im Gebäudedialog bleibt.
- Texte: neun Schlüssel entfernt (`GIMP_DLG_GRP_BAUSTOFFE`, `GIMP_DLG_BAUSTOFFE_HINWEIS`, `GIMP_AB_ZUR_BAUSTOFF`,
  `GIMP_DLG_SP_MATERIALNAME`, `…_SCHICHTEN`, `…_ABGLEICH`, `…_BAUSTOFF`, `…_STOFFWERTE`, `…_WERTE_AUS`), drei neu
  (`GIMP_AB_VERWALTUNG_HINWEIS`, `GIMP_AB_HERKUNFT_HAND`, `GIMP_AB_DATEISCHICHTEN`), `GIMP_AB_HINWEIS` ergänzt.
  Die KI-Eingabebilanz bleibt bei 30 (eine Klappliste je Materialzeile wird eine je Schicht).

## Nachweis

- Kern-Filter und Windows-Schale (`EnableWindowsTargeting=true`): 0 Fehler.
- Kern-Tests (Filter GebaeudeImport, Baustoff, Bauteilaufbau, Bauteilschicht, KiDialog, KiMasken, Wache, Ressource,
  GebaeudeBauteilvorschlag, GebaeudeAufbau, Steckbrief): 1 153 grün, darunter zwei neue Fälle AB3.
- UI-Tests (dieselben Filter samt Stilblatt): 614 grün, darunter fünf neue Fälle AB3.

## Sichtabnahme (Windows)

- Import einer IFC mit Materialnamen ohne Stoffwerte: Zeile „ohne Aufbau“ bzw. Ersatzaufbau aufklappen, Schicht
  gelb mit Klappliste; Wahl ändert den Vorschlag, Herkunft „Zuordnung von Hand“, „Zuordnung entfernen“.
- Import der Projektdatei: alle Schichten „aus der Datei“, keine Klappliste.
- Hinweiszeile über der Liste; Breite der Klappliste in der Schichtzeile (Umbruch im schmalen Fenster).

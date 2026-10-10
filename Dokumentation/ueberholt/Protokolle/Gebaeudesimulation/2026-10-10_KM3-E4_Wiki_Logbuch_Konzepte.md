# Protokoll KM3-E4 — Wiki-Quellen, Logbuch-Entwurf, Konzepte nach ueberholt (10.10.2026)

**Sitzung:** Gebäudesimulation, Zweig `km3-e4`, Statuszeile **#879** (vorläufige Nummer). **Entscheid:** E116. Konzepte: [`Konzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md`](../../Konzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md), [`Umsetzungskonzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md`](../../Umsetzungskonzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md).

## 1 Auftrag

Etappe E4 des Umsetzungskonzepts: die gebaute Funktion in den Repo-Quellen des Wikis beschreiben, die Logbuch-Sätze entwerfen, die beiden Konzepte als „wie gebaut“ nach `ueberholt/` legen, Statuszeile und Protokoll schreiben.

## 2 Befund

- Beide Kühlungsseiten hatten Kältemaschine und Kennlinie, aber keinen Teillastweg.
- Die Verweise auf die beiden Konzepte standen im Index, in drei KM3-Protokollen und in `Referenzlaeufe/LIESMICH.md` (sieben Fundstellen, zusätzlich die Selbstverweise und relativen Pfade in den verschobenen Dateien).

## 3 Änderungen je Datei

- `Projekte/Wiki/Programm Dokumentation - Kühlung.wiki`: Abschnitt „Teillast und Takten der Kältemaschine“ (Katalogdialog, Anlagendialog, Ergebnisgrößen, Beispiel).
- `Projekte/Wiki/Grundlagen - Kühlung.wiki`: Abschnitt „Teillast und Takten der Kältemaschine“ (Teillastkurve, Taktgrenze und Taktverlust, Randweg, Folgeschaltung).
- `Dokumentation/aktuell/Wiki_Update_2026-09-26.md`: zwei Sätze unter „Version offen (Vorschlag 1.2.1)“, beide Kühlungsseiten im Verzeichnis der ausstehenden Uploads.
- Beide Konzepte: Abschnitt „Umsetzung — wie gebaut“, `git mv` nach `Dokumentation/ueberholt/`; Index, Protokollverweise und `Referenzlaeufe/LIESMICH.md` nachgezogen.
- `Status_iOS_Migration.md` (#879), `Status_Gebaeudesimulation_VDI6007.md` (Stufenzeile KM3).

## 4 Abnahme

`EPOS.Kern.Tests` (Filter `DokumentationLinkWache|WikiProduktdatenWache|RepositoryOrdnungWache|Wiki`) 52/52, Build 0 Fehler; Konfliktmarker keine; Gegenlesemuster der Wiki-Regel auf beide Seiten: keine Treffer in den neuen Absätzen.

## 5 Offen

Wiki-Upload (Freigabe und Versionsnummer beim Anwender).

**CI:** läuft (Vermerk folgt).

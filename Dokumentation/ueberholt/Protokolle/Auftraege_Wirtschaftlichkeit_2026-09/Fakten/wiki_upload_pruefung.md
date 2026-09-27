# Prüfung Live gegen Repo vor dem Wiki-Sammel-Upload 26.09.2026

Nur gelesen: kein Commit, kein Upload, keine Datei außer diesem Bericht geändert.
Grundlage: `scratchpad\wiki\live\<n>.wiki` (action=raw), `repo\<n>.wiki` (Worktree pm26), `vergleich.txt`,
das Papier `Dokumentation/aktuell/Wiki_Update_2026-09-26.md` (Abschnitt 1) und das Konzept Hilfesystem,
Regeln 2 bis 4 (Kopf, Live-Stand zuerst übernehmen, Kategorie, Titel und Anker bleiben).

Vorgehen: Mit `diff` die Zeilen gesucht, die nur live stehen. Jede dieser Zeilen satzweise gegen den ganzen
Repo-Text geprüft (py -3, difflib), die Treffer von Hand gelesen und für jeden Wegfall die Ursache
gesucht: Papier Abschnitt 1, dazu `git log -S` auf die Repo-Quelle. Außerdem geprüft:
- Anker: alle Namen aus `{{Anker|a|b|…}}` und `<span id>`, dazu die Überschriften.
- Kategorie und Kopf-Kommentar.
- Wikilinks: Linkziele, die live stehen und im Repo fehlen.
- `help_mapping.txt`: jedes Ziel `Seite#anker` der 18 Seiten muss in der Repo-Quelle stehen.
- Alle 138 Live-Seiten des Wikis (allpages, action=raw) auf Links `…/<Seite>#<fragment>` in die 18 Seiten,
  deren Ziel in der Repo-Quelle fehlt.

## Ergebnis in Kürze

| Nr | Titel | Verdikt |
|---|---|---|
| 1 | Klimadaten | **vorher Live-Text übernehmen**: „Siehe auch" (2 Links) und Abschnitt „Berechnung" |
| 2 | Simulationsergebnisse | einfacher Ersatz |
| 3 | Stromspeicher | einfacher Ersatz |
| 4 | Hilfe-Assistent | einfacher Ersatz |
| 5 | Wirtschaftlichkeit | einfacher Ersatz |
| 6 | Kosten | einfacher Ersatz |
| 7 | Pufferspeicher | einfacher Ersatz |
| 8 | Gebäudemodell VDI 6007 | neu, **Kategorie-Zeile fehlt** |
| 9 | Kühlung | neu, **Kategorie-Zeile fehlt** |
| 10 | Gerätekataloge | neu, vollständig |
| 11 | Simulation | einfacher Ersatz (Repo-Datei mit BOM) |
| 12 | Photovoltaik | einfacher Ersatz |
| 13 | Varianten | einfacher Ersatz (Repo-Datei mit BOM) |
| 14 | Gebäude | einfacher Ersatz |
| 15 | Baustoffe und Bauteilaufbauten | neu, vollständig |
| 16 | Gebäudeimport | neu, vollständig |
| 17 | Brauchwasser-Zapfprofil | neu, vollständig |
| 18 | Berichtsvorlagen | neu, vollständig |

Querbefunde:
- Kein `{{Anker}}`-Name der Live-Seiten fehlt in einer Repo-Quelle. Die Zweitnamen tragen die Anker
  (Klimadaten: `region`, `koordinaten`, `bezeichnung`, `daten-einlesen`; Gebäude: `luftwechsel`,
  `u-werte`, `anschlussmasse`).
- Alle `help_mapping.txt`-Ziele der 18 Seiten stehen in den Repo-Quellen.
- Keine der 138 Live-Seiten verlinkt auf ein Fragment der 11 bestehenden Seiten, das nach dem Upload
  fehlen würde. Neun Fragment-Links bestehen, alle auf `{{Anker}}`-Namen, die das Repo führt.
- Wegfallende Überschriften (automatische Überschriftanker) sind unten je Seite genannt. Keiner davon
  ist in `help_mapping.txt`, in `Projekte/Wiki` oder auf einer Live-Seite verlinkt. Nach Regel 4 („eine
  umbenannte Überschrift behält ihren alten Anker") ist das unkritisch, weil diese Überschriften nicht
  umbenannt, sondern ganz neu gegliedert wurden. Bei der Wiki-Suche nach „#Eingaben" kamen Seiten als
  Treffer, aber nur, weil die Suche das Wort einzeln sucht. Die Fragment-Prüfung aller Seiten fand
  keinen Link.
- Kategorie `[[Kategorie:Programm Dokumentation]]` steht live und im Repo gleich auf allen 11
  bestehenden Seiten. Sie fehlt in den neuen Seiten 8 und 9.
- Kopf-Kommentar: Bei 9 der 11 bestehenden Seiten steht er live und im Repo wortgleich. Bei Nr. 1 und
  Nr. 14 hat die Live-Seite keinen Kopf, die Repo-Quelle trägt ihn nach Regel 2. Das ist so gewollt.
- Nr. 11 und Nr. 13 sind im Repo UTF-8 mit BOM, die Live-Seiten ohne. Der Upload muss den BOM
  entfernen (sonst steht U+FEFF vor dem Kommentar), wie offenbar beim letzten Upload.
  Alle Repo-Quellen haben CRLF, das normalisiert MediaWiki.

---

## 1 · Programm Dokumentation/Klimadaten

| Feld | Befund |
|---|---|
| Verdikt | **vorher Live-Text übernehmen** |
| Begründung | Die Repo-Quelle ist eine Neufassung (Papier: „neue Seite …; die vier Anker der Live-Seite … bleiben als Zweitnamen erhalten (#507)"). Fast alle 18 Live-Zeilen stehen darin umformuliert (a) oder sind bewusst ersetzt (b). Zwei Blöcke hat die Repo-Quelle nie getragen (`git log -S` ohne Treffer). Sie sind echter Live-Inhalt (c): der Abschnitt „Siehe auch" mit zwei Links und der nach der Kategorie angehängte Abschnitt „Berechnung". Alle vier Linkziele bestehen live (HTTP 200). |
| Kopf / Kategorie | Live ohne Kopf, Repo mit Kopf (gewollt). Kategorie gleich. |
| Anker | Alle 7 Live-Anker stehen im Repo (`region` bei `regionsliste`, `koordinaten` und `bezeichnung` bei `standort`, `daten-einlesen` bei `einlesen`). Überschriften, die wegfallen: `Eingaben` (umgegliedert, unkritisch) und `Berechnung` (fällt mit dem fehlenden Abschnitt, siehe unten). |

Einordnung der Live-Zeilen:
- (a) Einleitung (8.760 Stunden, Strahlungsarten): steht umformuliert in der Einleitung.
- (a) „jedes Projekt ist genau einer Region zugeordnet": steht als „Die Klimaregion des Projekts …"
  unter `uebernahme`.
- (a) `standort`, `region`, `koordinaten` und `bezeichnung`: stehen im Abschnitt „Standort" („über
  einen Ortsnamen, den das Programm in Koordinaten auflöst, oder von Hand über Longitude, Latitude und
  eine Bezeichnung").
- (a) `daten-einlesen`: steht in „Einlesen" und „Was gespeichert wird" (vier Fassadenwerte,
  Sonnenwinkel).
- (a) „mitgelieferte schreibgeschützt, eigene löschen": steht unter `regionsliste` (Schloss).
- (a) `diagramme`: steht in „Die zwei Diagramme".

**Kritische Live-Zeilen im Wortlaut (c):**

```
== Siehe auch ==

* [[Klimadaten festlegen]] – Import und Zuordnung der Region im Programmablauf
* [[Grundlagen/Klimadaten]] – Herkunft und Aufbau der Klimadatensätze
```
Zielstelle: im Repo-Abschnitt `== Siehe auch ==` zu den zwei vorhandenen Einträgen
(`Programm Dokumentation/Photovoltaik`, `Programm Dokumentation/Einstellungen`) hinzufügen.

```
== Berechnung ==
Wie die Klimadaten in den Lauf eingehen — Aussentemperatur in Ortszeit, Tagtypen,
Wochenendkennzeichen und der daraus abgeleitete Kalender —, steht unter
[[Programm Dokumentation/Berechnung/Simulationsablauf|Berechnung: Simulationsablauf]]
und [[Programm Dokumentation/Berechnung/Wärmebedarf|Berechnung: Wärmebedarf]].
```
Zielstelle: als eigener Abschnitt vor `== Siehe auch ==`, wie auf den Seiten Gebäude, Photovoltaik,
Pufferspeicher, Simulation, Simulationsergebnisse und Stromspeicher. Live hängt er hinter der
Kategorie. Nach dem Vorbild von Gebäude kann er zusätzlich `{{Anker|berechnung}}` tragen.
Wortlaut-Hinweis: Live steht „Aussentemperatur" (ss). Das ist beim Übernehmen zu entscheiden
(wortgleich lassen oder „Außentemperatur").

## 2 · Programm Dokumentation/Simulationsergebnisse

| Feld | Befund |
|---|---|
| Verdikt | einfacher Ersatz |
| Begründung | Keine der 6 Live-Zeilen fehlt inhaltlich. `uebersicht`, `ringe` und „Komponentenliste" stehen wortgleich und sind im Repo verlängert (Kältedeckung und Block „Kälte", Papier KU2) (a). „Auslegungsprüfung Erdreich": aus „Befund nach VDI 4640" wird „Einstufung nach VDI 4640" (E12, Tabuwort) (b). „Unter Parameter → Stromspeicher → Auslegung optimieren" wird zu „① Konfiguration → Stromspeicher auslegen…" (#274, Einstieg aus ①) (b). Die Links des Berechnungsabschnitts stehen gleich. |
| Kopf / Kategorie | gleich / gleich |
| Anker | alle 3 Live-Anker vorhanden (Repo 6); keine Überschrift fällt weg |

## 3 · Programm Dokumentation/Stromspeicher

| Feld | Befund |
|---|---|
| Verdikt | einfacher Ersatz |
| Begründung | Die 65 Live-Zeilen sind (a) wortgleich verlängert oder in Wortwahl nachgezogen: „vorher" → „zuvor", „bisherige" → „zuvor ermittelte", „Befund" → „Diagnose", „Oracle" → „Idealwissen", „Entscheidungszeitpunkt" → „Planungszeitpunkt", „vorher → nachher" → „ungekappt → gekappt" (E12). Oder sie sind (b) bewusst ersetzt: Der Einstieg über das Simulationsergebnis („Speicherflotte & Auslegung öffnen") wird zum Einstieg über die Simulationskonfiguration (Commit 811dabe8a, #274). Die Größensuche mit Größenkopplung, das zweiphasige Grob- und Feinraster samt Schalter, die Marke Grob/Fein und die Rasterzahlen (240/18/258) werden zur Gerätesuche (Commit 66ab617a7 vom 15.09.2026 „die Groessensuche waehlt Geraete"). Der Datenzoom durch Ziehen geht in die neue Diagrammbedienung über (Papier: „Beschreibung der Auslegungsbilder auf die neue Diagrammbedienung nachgezogen"). Kein Live-Inhalt ist neuer als das Repo. |
| Kopf / Kategorie | gleich / gleich |
| Anker | alle 30 Live-Anker vorhanden (Repo 39). Die Überschrift „Phase 2: das Feinraster" fällt weg, weil der Inhalt bewusst entfällt. Sie ist nirgends verlinkt. |

## 4 · Programm Dokumentation/Hilfe-Assistent

| Feld | Befund |
|---|---|
| Verdikt | einfacher Ersatz |
| Begründung | „Freigegeben sind dafür sechs Masken: …" wird zur vollständigen Maskenliste samt Öffnungsweg (Papier: „Liste der Masken, … wächst mit den Wellen KI‑F1b bis F6", #416, #419) (b). „fünf Laufparameter … Heizstab der Wärmepumpe" wird zu „Laufparameter … Kühlung rechnen" und „Setzen kann er dort die Einstellwerte …" (Commit 111dfd490, #458) (b). „Bestätigungsblock", „Was er ablehnt", „Sicherungskopie" und „Abbrechen" stehen im Repo und sind erweitert (Weg über Duplizieren…, nicht bedienbare Felder) oder in der Wortwahl nachgezogen: „bisher/künftig" → „aktuelle/neue", „vorher" → „zuerst", „bisherige" → „zuvor gespeicherte" (E12) (a/b). |
| Kopf / Kategorie | gleich / gleich |
| Anker | 8/8 vorhanden; keine Überschrift fällt weg |

## 5 · Programm Dokumentation/Wirtschaftlichkeit

| Feld | Befund |
|---|---|
| Verdikt | einfacher Ersatz |
| Begründung | Die Live-Seite ist ein alter Stand (8,8 KB gegen 73,6 KB). Alle 25 Live-Zeilen sind nach dem Papier bewusst ersetzt (b). Stammprojekt/Varianten werden zu Referenz/Versionen. Best/Worst werden zu Günstig/Ungünstig. Die Statuszeile heißt jetzt Szenariozeile. Aus sieben Größen und vierzehn Feldern werden neun Größen und achtzehn Felder (Szenariowerte, #462). Das Freitextfeld „Nicht monetäre Wirkungen" wird zur Liste (#479). Aus den Knöpfen „Tarifstruktur…" und „Verlauf…" werden das Rollenmodell (Anker `strombezug`) und der Abschnitt Verlauf (Anker `verlauf`). Die Aufschläge auf den Strombezug stehen jetzt unter Kosten#aufschlaege. Word- und Excel-Bericht sind neu beschrieben (Anker `bericht`, `bericht-excel`). Kein Satz trägt Inhalt, der im Repo fehlt. Links: nichts fehlt. |
| Kopf / Kategorie | gleich / gleich |
| Anker | alle 10 Live-Anker vorhanden (Repo 69). Die Überschriften „Eingaben" und „Nachweisblock und Vorschlag" fallen weg (Neugliederung; die Anker `nachweis` und `vorschlag` bleiben). Sie sind nirgends verlinkt. |

## 6 · Programm Dokumentation/Kosten

| Feld | Befund |
|---|---|
| Verdikt | einfacher Ersatz |
| Begründung | 5 der 7 Live-Zeilen stehen wortgleich als Anfang längerer Repo-Zeilen (a). Die Zeile `stromtraeger` ist um den Elektrokessel ergänzt (a). „Sie übernimmt keinen möglicherweise veralteten Gesamtbetrag einer früheren Auslegung." ist bewusst umformuliert zu „ein bereits ausgewiesener Gesamtbetrag geht nicht ein" (Commit 2e3b068c9 „Speicherauslegung nennt nur den gueltigen Stand") (b). |
| Kopf / Kategorie | gleich / gleich |
| Anker | alle 10 Live-Anker vorhanden (Repo 53); keine Überschrift fällt weg |

## 7 · Programm Dokumentation/Pufferspeicher

| Feld | Befund |
|---|---|
| Verdikt | einfacher Ersatz |
| Begründung | „Investitionskosten – der Betrag, der in die Wirtschaftlichkeit eingeht …" steht jetzt im Punkt „Alle Daten anzeigen", ein Satz ist wortgleich (Papier #422: „Aufklapper … mit den Investitionskosten statt des entfallenen Detailfelds") (b). `speicherliste`: Der erste Satz ist wortgleich, „anlegen, übernehmen und entfernen" ist ausführlicher beschrieben (a). Die Berechnungszeile steht gleich. |
| Kopf / Kategorie | gleich / gleich |
| Anker | alle 10 Live-Anker vorhanden (Repo 11) |

## 8 · Programm Dokumentation/Gebäudemodell VDI 6007

| Feld | Befund |
|---|---|
| Verdikt | neu |
| Begründung | Live 404. Der Kopf ist vollständig (Wikititel, Repo-Pfad, Pflegeregel). 12 Anker sind vorhanden, darunter die im Papier genannten `heizkreis`, `sollwert-zeitprogramm`, `kuehluebergabe` und `bauteilweg`. `help_mapping.txt` hat kein Ziel auf diese Seite. |
| **Mangel** | **Die Zeile `[[Kategorie:Programm Dokumentation]]` fehlt.** Die Datei endet mit der Grenzen-Liste. Vor dem Upload am Dateiende ergänzen (Konzept Regel 4, Abschnitt 50: Kategorie auf allen Unterseiten). |

## 9 · Programm Dokumentation/Kühlung

| Feld | Befund |
|---|---|
| Verdikt | neu |
| Begründung | Live 404. Der Kopf ist vollständig. 10 Anker sind vorhanden (`kuehlbetrieb`, `kaelteerzeugung` und `abrechnung` laut Papier). Es gibt keine help_mapping-Ziele. |
| **Mangel** | **Die Zeile `[[Kategorie:Programm Dokumentation]]` fehlt**, Ergänzung wie bei Nr. 8. (Nebenbefund außerhalb dieses Uploads: Auch `Mehrzonenmodell.wiki` und `Projekttransfer.wiki` unter `Projekte/Wiki` tragen keine Kategorie.) |

## 10 · Programm Dokumentation/Gerätekataloge

| Feld | Befund |
|---|---|
| Verdikt | neu |
| Begründung | Kopf und Kategorie sind vollständig. 15 Anker. Alle 16 help_mapping-Ziele (`heizkessel` … `stromspeicher`, `dubletten`, `import`, `import-kuehlkennlinien`) sind vorhanden. |

## 11 · Programm Dokumentation/Simulation

| Feld | Befund |
|---|---|
| Verdikt | einfacher Ersatz |
| Begründung | Alle 8 Live-Zeilen sind verlängert oder nachgezogen (a/b). Das Schließkreuz ✕ neben „← zurück" ist ergänzt. „Gesamt" / „nur Gesamt" heißt jetzt „Summe Wärmeerzeugung" / „nur Summe Stromverbrauch" (Commit 450738e64 „Summenlinien heissen nach ihrer Summe"). „aufgezogener Ausschnitt" heißt jetzt „Zeitausschnitt". Der Einstieg „Speicherflotte & Auslegung öffnen" auf dem Ergebnisreiter wird zu „Stromspeicher auslegen…" in ① (Commit 811dabe8a, #274). „vorher" heißt jetzt „zuvor" (E12). |
| Kopf / Kategorie | inhaltlich gleich; **Repo-Datei mit UTF-8-BOM**, beim Upload entfernen / Kategorie gleich |
| Anker | alle 14 Live-Anker vorhanden (Repo 19); die 4 help_mapping-Ziele sind vorhanden |

## 12 · Programm Dokumentation/Photovoltaik

| Feld | Befund |
|---|---|
| Verdikt | einfacher Ersatz |
| Begründung | Genau die im Papier genannte E12-Bereinigung: Aus „den Befund", „jedem roten oder gelben Befund" und „rote Befunde" werden „die Diagnose", „jeder roten oder gelben Meldung" und „rote Meldungen" (b). Die vierte Diff-Zeile (Berechnungslink) steht wortgleich im Repo, sie verschiebt sich nur. |
| Kopf / Kategorie | gleich / gleich |
| Anker | 10/10 vorhanden |

## 13 · Programm Dokumentation/Varianten

| Feld | Befund |
|---|---|
| Verdikt | einfacher Ersatz |
| Begründung | „Variante löschen" steht wortgleich und ist um den Kohärenzsatz ergänzt (E12, Anker `pv-verguetung`) (a). Die Kommentar- und die Kategoriezeile melden nur wegen des BOM bzw. der Zeilenlage einen Unterschied, inhaltlich sind sie gleich. |
| Kopf / Kategorie | inhaltlich gleich; **Repo-Datei mit UTF-8-BOM**, beim Upload entfernen / Kategorie gleich |
| Anker | 2 Live-Anker vorhanden (Repo 4) |

## 14 · Programm Dokumentation/Gebäude

| Feld | Befund |
|---|---|
| Verdikt | einfacher Ersatz |
| Begründung | Die Repo-Quelle ist laut Papier „aus dem Live-Stand (action=raw, 3 654 Zeichen, zwölf Anker) … neu angelegt". Die Live-Größe ist heute unverändert 3 654 B, live hat sich seitdem also nichts geändert. Alle 25 Live-Zeilen stehen umformuliert und erweitert im Repo (a): Filter/Freitextfilter wird zum Suchfeld mit Trichtern, Wohngebäude/Gewerbe heißt jetzt Verwendung. Gebäudename, Gebäudeart und Beschreibung stehen im Detailblock `verbrauch`. Verbrauch in Öl, Gas oder Brennstoff und der Jahresnutzungsgrad stehen wortnah im Repo. Die Fensterflächen stehen nach Orientierung. Die U-Werte stehen in „Hülle: Transmission je Bauteil". Die Luftwechselrate in 1/h bestimmt laut Repo die Lüftungsverluste. Wärmebrücken und Anschlusslängen sind zusammengelegt, die Ferienzeiträume vorhanden, die Brauchwasserprofile stehen unter „Brauchwasser…". Die Links „Gebäude und Gebäudetypen" und „Grundlagen/Wärmebedarfsrechnung" stehen im Repo. Der hinter der Kategorie angehängte Live-Abschnitt „Berechnung" (mit „24-Stunden-Kapazitaetsmodell") ist durch den Repo-Abschnitt `berechnung` bewusst ersetzt: „Speicherfähigkeit", Klassen- und Bauteilweg, Link auf Berechnung/Wärmebedarf bleibt (b, G3). |
| Kopf / Kategorie | Live ohne Kopf, Repo mit Kopf (gewollt) / Kategorie gleich |
| Anker | Alle 12 Live-Anker vorhanden (Repo 47). `luftwechsel`, `u-werte` und `anschlussmasse` stehen als Zweitnamen bei `kenngroessen`, `flaechen` und `waermebruecken`. Wegfallende Überschriften: „Eingaben", „Eingabe der Gebäudedaten", „Gebäudedaten: Bauteilflächen und U-Werte", „Gebäudedaten: Raumsolltemperaturen, Wärmebrücken, Ferienzeiten" (Neugliederung, nirgends verlinkt). Alle 21 help_mapping-Ziele vorhanden. |

## 15 · Programm Dokumentation/Baustoffe und Bauteilaufbauten

| Feld | Befund |
|---|---|
| Verdikt | neu |
| Begründung | Kopf und Kategorie vollständig. 5 Anker (`baustoffe`, `bauteilaufbauten`, `schichten`, `summen`, `speichern`), wie im Papier. Beide help_mapping-Ziele vorhanden. |

## 16 · Programm Dokumentation/Gebäudeimport

| Feld | Befund |
|---|---|
| Verdikt | neu |
| Begründung | Kopf und Kategorie vollständig. 27 Anker. Die help_mapping-Ziele `zuordnung` und `baustoffzuordnungen` sind vorhanden. |

## 17 · Programm Dokumentation/Brauchwasser-Zapfprofil

| Feld | Befund |
|---|---|
| Verdikt | neu |
| Begründung | Kopf und Kategorie vollständig. 35 Anker, wie im Papier („35 Anker"). Alle 16 help_mapping-Ziele vorhanden. |

## 18 · Programm Dokumentation/Berichtsvorlagen

| Feld | Befund |
|---|---|
| Verdikt | neu |
| Begründung | Kopf und Kategorie vollständig. 14 Anker, darunter alle im Papier genannten (`vorlage` … `einstellungen`, `kapitel`, `haekchen`, `logo`). Die 3 help_mapping-Ziele (`pruefliste`, `platzhalterkatalog`, `einstellungen`) sind vorhanden. |

---

## Vor dem Upload zu tun (Repo-Quelle, nicht im Wiki)

1. **Klimadaten** (`Projekte/Wiki/Programm Dokumentation - Klimadaten.wiki`):
   - die zwei Siehe-auch-Einträge `[[Klimadaten festlegen]]` und `[[Grundlagen/Klimadaten]]` ergänzen;
   - den Abschnitt `== Berechnung ==` mit den Links auf Berechnung/Simulationsablauf und
     Berechnung/Wärmebedarf vor „Siehe auch" einfügen (Wortlaut oben).
2. **Gebäudemodell VDI 6007** und **Kühlung**: am Dateiende `[[Kategorie:Programm Dokumentation]]` ergänzen.
3. Beim Hochladen von **Simulation** und **Varianten** den BOM der Repo-Datei nicht mitsenden.

Diff-Beleg für den Auftragsbericht (Regel 3): `scratchpad\wiki\vergleich.txt` und dieser Bericht.

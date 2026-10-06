# Protokoll HC-4 — „Datei erneut lesen…“ im Gebäudedialog (06.10.2026)

**Sitzung:** IFC / Gebäudeimport, Statuszeile **#749**. Ein Opus-Agent im Worktree: `9a1198f7` Kern liest die Importdatei erneut, ohne zu schreiben; `afd6c9e2` Knopf im Gebäudedialog samt Hülle. Merge `768217eb` (ohne Konflikt; Designerprüfung danach grün). Kein Schemaschritt, keine Persistenz der Geometrie, Basis unverändert.
**Entscheid:** E87 (F3); Konzept [HottCAD-Verbund](../../../aktuell/Gebaeudesimulation/2026-10-05_Konzept_HottCAD_Verbund_IFC_Projektdatei_Viewer.md) 4 und Welle 6.5; Vorwelle [HC-2](2026-10-06_HC-2_Farbmodus_Randbedingung.md).

## 1 Auftrag

Die Ansicht eines importierten Gebäudes soll sich aus der Importdatei erneut zeigen lassen, ohne die Geometrie zu speichern: Der Anwender wählt die Datei, der Kern prüft sie gegen die gespeicherte Importquelle und baut die Ansicht, ohne etwas zu schreiben.

## 2 Zuschnitt

| Teil | Modell | Inhalt |
|---|---|---|
| Kern | Opus (Worktree) | `GebaeudeNeulesen`, Controller `GebaeudeImportCtrl.LesenQuellenDerZuordnung`, Tests |
| Hülle und Dialog | Opus (Worktree) | `GebaeudeNeulesenHuelle` (in `GebaeudeHuelle.Gaben`), `GebaeudeNeulesenDaten`, Knopf und Überlagerung im `GebaeudeDialog`, Texte, Tests |

## 3 Gebaut

- **Knopf** „Datei erneut lesen…“ im Aktionsschlitz des Gebäudedialogs, nur bei einer Projektkopie mit Importquelle.
- **Dateiwahl** über `Dienste.Datei`; der gespeicherte Dateiname steht im Titel des Wählers und unter Windows als erster Filter (eine wörtliche Vorbelegung kennt `IDateiDienst.DateiOeffnenAsync` nicht).
- **Kern** `EPOS.Kern/Allgemein/Import/Gebaeude/GebaeudeNeulesen.cs` prüft Format, Größengrenze der Plattform und SHA-256. Nur bei passendem Hash derselbe Weg wie der Import (Leser, Raumkörper, Körpernachbarschaft, Flächenklassifikation), Zonierung nach der gespeicherten Zonenregel, Grundriss; keine Zuordnung, kein Schreiben, kein Zonenplan. `GebaeudeImportCtrl.LesenQuellenDerZuordnung` und `Gebaeudekennung` lesen nur; keine neue SQL-Anweisung.
- **Hülle** `EPOS.UI.Daten/Bedarf/GebaeudeNeulesenHuelle.cs`, **Daten** `EPOS.UI/Dialoge/Bedarf/GebaeudeNeulesenDaten.cs`.
- **Dialog** `EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor`: eigene breite Überlagerung „Importdatei erneut lesen“ mit Quellzeile (Name, Format, Importzeitpunkt), beide Farbmodi; Kreuz und Esc schließen sie; kein Speichern, kein `Geaendert`.
- **Zustände:** passend (Ansicht, Bestätigung, Herkunft der Zonen — Regel der Datei ohne Handzuordnungen, bei HottCAD-IFC zusätzlich ohne Projektdatei); Hash abweichend (keine Ansicht, Dateiname, Dateidatum, beide Hashes zu 12 Zeichen, „Andere Datei wählen…“); nicht lesbar; Format unbekannt; zu groß; keine Quelle (kein Knopf). Der Abbruch der Dateiwahl ist kein Fehler.
- **iOS:** gleicher Weg, iOS-Grenze des Formats; der Filter gilt nur gemeinsam für beide Formate (der Wähler bildet auf Typkennungen ab); ohne Wähler wie Abbruch; keine benannte Ablehnung nötig.
- **Texte:** 15 neue Schlüssel `GEB_BTN_NEU_LESEN*`, `GEB_NL_*` in beiden Sprachen.
- **`Tab_Importquelle`** trägt ID_Gebaeude, Format (IFC/GBXML), Dateiname, Hash, Groesse, Schemastand, Zeitpunkt, Programmfassung, Zonenregel, FehlendeEntitaeten — nur den Namen, nie den Pfad. Ob eine Projektdatei dazugeladen war, ist nicht gespeichert; deshalb gibt es keine Quellzeile dazu, und das Format ist nur IFC oder GBXML.

## 4 Abweichungen vom Konzept

1. Überlagerung statt eingebettet im Detailblock (Hausregel DL-2: Detailblock ohne Knöpfe).
2. Kein Knopf im Assistenten (kein Aktionsschlitz, wie bei „Hülle und Zonen…“ und beim Export).
3. Der Dateiname ist nicht wörtlich vorbelegt (Titel und Filter statt Vorbelegung).
4. Das Datum beider Dateien (Konzept 8, GId-Abgleich) betrifft die Projektdatei und wird nicht gezeigt.
5. Beim ersten Lesen gibt es keine Laufanzeige.

## 5 Prüfungen des Agenten

Kern-Filter und Windows-Schale (Linux) je 0 Fehler. Filter samt Wachen: EPOS.Kern.Tests 361/361, EPOS.UI.Tests 339/339. 23 neue Tests: 8 Kern ohne Datenbank, 2 Kern mit Datenbank, 9 Hülle, 4 bunit.

## 6 Gate 749

Hauptbaum, Kopf `768217eb`: ⟨GATE749⟩.

## 7 Offen

- **Sichtabnahme unter Windows:** Wähler mit Namensfilter, Breite der Überlagerung, Wechsel Zonen/Randbedingung, Hinweis bei geänderter Datei, Esc schließt nur die Ansicht.
- **Logbuch-Entwurf** (Version beim Anwender erfragen): „Im Gebäudedialog zeigt ‚Datei erneut lesen…‘ die Ansicht eines importierten Gebäudes erneut aus seiner Importdatei, ohne etwas zu ändern.“
- **Wiki-Upload** „Gebäude“ gebündelt.
- Offen bleibt nur Konzept 5.3 (Diagnose an echten Projektdateien, drei Annahmen, gespeicherter Zonenplan als Startplan, Ziehen mit der Maus, iOS-Lauf für `.sqproj`). Merkposten: Ob die Projektdatei beim Import dazugeladen war, ist nicht gespeichert.

# PI‑1 — Projekttransfer: Mehrfachauswahl im Export, Sammellauf im Import

**19.09.2026** · Zweig `pi1` · Konzept
[`Konzept_Projekttransfer_EPOS-Plan.md`](../../../aktuell/Konzept_Projekttransfer_EPOS-Plan.md)
Rev. 3, Etappe T7 · Statuszeile **#373** (im Zweig als #372 vergeben, beim Zusammenführen umnummeriert)

---

## 1 Auftrag und Entscheid

**Anwenderauftrag vom 19.09.2026 (Wortlaut):**

> „Administrations‑Dialoge: Beim Menü Projekt‑Import soll eine Mehrfachauswahl
> möglich sein. Dabei soll berücksichtigt werden, wenn Variantenprojekt
> importiert werden (diese sollen zusammen mit Stamm importiert werden, nicht
> mehrfach)."

Gemeint ist **Projekte → Export/Import** (`.wpx`‑Pakete).

**Entscheid PI‑Q1:** beides — (a) Import mehrerer Paketdateien in einem Lauf
**und** (c) Export mit Mehrfachauswahl der Projekte. Eine gewählte Variante
reist mit ihrem Stamm im selben Paket; je Stammgruppe entsteht genau ein Paket,
nie zwei. **Nicht gewählt:** eine Häkchenliste der Varianten im Import — was im
Paket steckt, reist mit.

**Festlegungen des Orchestrators:**

- ein **Sammelbericht** neben dem ersten Paket, nicht je Paket;
- das Häkchen einer in der Liste **ausdrücklich gewählten** Variante ist
  gesperrt — weich, mit Grund;
- die **Sicherungskopie** fällt einmal je Lauf, vor dem ersten Paket.

## 2 Befund vor der Arbeit

| Stelle | Stand vor PI‑1 |
|---|---|
| `EPOS.UI/Dialoge/Projekt/ProjektTransferDialog.razor` | Export über ein `Auswahlfeld` (eine Wahl) plus eine `Mehrfachauswahl` der Varianten; Import über eine `Dateiwahl` (ein Pfad) |
| `EPOS.UI/Dienste/ProjektTransferDaten.cs` | neun Gaben, alle auf **ein** Projekt und **ein** Paket zugeschnitten |
| `WindowsFormsApplication1/Views/Projekt/ProjektTransferHuelle.cs` | `MASS` 720 × 640; **eigener JSON‑Zerleger** des Manifests in der Schale |
| `EPOS.Kern/Controller/ProjektExportImportCtrl.cs` | `Exportieren` meldet über `Meldung.Zeigen`; `Importieren` führt `nameZuId` **je Aufruf**; keine Prüfung, dass das Hauptprojekt kein Variantenprojekt ist |

Die Folge im Betrieb: Wer Stamm und Varianten einzeln bekommen hatte, importierte
sie nacheinander — die Variante fand ihren Stamm nicht (er kam erst im nächsten
Lauf) und stand am Ziel eigenständig; ein Stamm in zwei Paketen entstand zweimal,
einmal echt und einmal als „(2)".

## 3 Umsetzung

### 3.1 Kern — vier Stücke ohne Oberfläche

| Datei | Inhalt |
|---|---|
| `EPOS.Kern/Model/Projektgruppierung.cs` (neu) | `Transfergruppe` und **die Wahlregel**: `Gruppieren`, `Nachgezogen`, `Variantenzahl`. Rein, ohne Datenbank — alles, was die Regel braucht, steht schon in `ProjektKopfZeile` |
| `EPOS.Kern/Allgemein/Katalog/Projekttransferprofil.cs` (neu) | das Listenprofil der `Katalogliste`: sechs Spalten, Zeilenschlüssel ist die **Projekt‑Id** (Projektnamen sind nicht zwingend eindeutig) |
| `EPOS.Kern/Controller/ProjektTransferSammel.cs` (neu, `partial`) | `Paketkopf`/`PaketKopf` (liest `manifest.json`, wirft nie), `Reihenfolge`, `ExportGruppen`, `Paketdateiname`, `ImportierenMehrere`, `Sammelstand`, `Fortschrittsbruecke`, die zwei Bilanzen |
| `EPOS.Kern/Controller/ProjektExportImportCtrl.cs` | Klasse `partial`; `Exportieren` ruft das neue, stumme `ExportEines`; `Importieren` ruft `ImportierenIntern` mit `stand = null` und bleibt **Zeile für Zeile** gleich |

**Die Wahlregel steht einmal.** Dialog und Kern rufen dieselbe: Jede gewählte
Zeile wird auf ihre Stammgruppe abgebildet, jede Gruppe kommt genau einmal vor,
zu jeder reisen alle Varianten ihres Stamms mit — außer den abgewählten, und
eine ausdrücklich gewählte lässt sich nicht abwählen. Zwei Fassungen wären zwei
Wahrheiten darüber, was mitreist.

**Die Exportwache** `TRANSFER_EXPORT_VARIANTE_ALS_STAMM`: Ein Paket, dessen
Hauptprojekt selbst eine Variante ist und das **weitere** Varianten mitführt,
ließe sich am Ziel nicht verknüpfen. Eine Variante **allein** bleibt erlaubt —
das ist der Weg der Vergütungsbeilage (§ 2.16), und er hat seine eigene Probe.

**Die Namensabbildung über den Lauf** (`Sammelstand.NameZuId`): Beim Auflösen
einer Verknüpfung geht ein Projekt, das dieser Lauf angelegt hat, einem
gleichnamigen des Zielbestands **vor**. Eingetragen wird unter dem
**Quellnamen**, auch wenn das Projekt am Ziel umbenannt wurde.

**Transaktion je Paket, Teilerfolg erlaubt.** Ein Lauf über fünf Pakete, bei dem
das dritte scheitert, behält die ersten beiden. Der Abbruch greift **zwischen**
zwei Paketen — ein Paket wird nie halb eingespielt.

### 3.2 Oberfläche

- **Exportblatt:** die `Katalogliste` in Mehrfachbetrieb mit eigenem
  `Katalogfilterstand`, Suche, Sortierung und Trichtern;
  `Katalogliste.razor` selbst bleibt unverändert. Die Wahl ist eine Menge von
  Projekt‑Ids und überlebt jeden Filterwechsel; die Klickregel liegt in
  `Zeilenmarkierung` und rechnet auf Anzeigeindizes. **Keine Vorbelegung** —
  eine vorgehakte Zeile wäre eine Wahl, die der Anwender nicht getroffen hat.
- **Der Stammzug ist sichtbar:** Spalte „mitgenommen" und Hinweisbanner.
- **Je Stammgruppe ein Häkchenblock**, alle an; das Häkchen einer gewählten
  Variante ist weich gesperrt (`aria-disabled` plus `title`) — ein hart
  gesperrtes Element zeigt seinen Tooltip nie.
- **Eine Gruppe:** weiter der Speichern‑Dialog mit Namensvorschlag.
  **Mehrere:** Zielordnerzeile, ein Paket je Gruppe, Bilanzbanner und
  Zeilenbericht.
- **Importblatt:** Knopf „Dateien wählen…", Vorschauraster in der
  **Laufreihenfolge** mit Datei, Hauptprojekt, Varianten, Schemastand und
  Hinweis („Variante von …", „derselbe Stamm wie Paket n — wird übersprungen").
  Zielnamefeld ab zwei Paketen weg, an seiner Stelle die leise Zeile mit dem
  Grund.
- **Naht** `ProjektTransferDaten`: `PaketVorschau` + `StammQuelle`/`Konflikt`,
  sechs Delegaten mit Vorgabe `null` — **nur nachgestellt**, damit die
  positionsbasierten Aufrufe in Hülle und Tests stehen bleiben.
- **Baustein `Mehrfachauswahl`:** neuer Parameter `Sperrgrund`
  (`Func<int,string>`); „Keine" lässt gesperrte Einträge stehen. Ohne den
  Delegaten unverändert.
- **Textbündel** `ProjektTransferTexte` (22 Einträge); die vierzig
  Bestandsparameter bleiben einzeln — sie umzubauen wäre eine Änderung an
  achtzig Stellen ohne Bezug zum Auftrag.

### 3.3 Hülle

`MASS` 1240 × 800 (die Maske trägt jetzt Listen); `PaketeLesen` über
`Dienste.Datei.DateienOeffnenAsync`, `ZielordnerWaehlen` über
`OrdnerWaehlenAsync`, beide mit der Pfadvorgabe aus `EinstellungenCtrl`;
`ExportierenMehrere`, `ImportierenMehrere` und `Paketkopf` an den Kern;
`NameVergeben` über `ProjektCtrl.IdVonName`. **Der eigene JSON‑Zerleger ist
entfallen** — das Paketformat gehört dem Kern, und eine zweite Lesart in der
Schale, die iOS nicht hat, wäre eine zweite Wahrheit.

## 4 Befunde der Arbeit

**PI‑B1 — Die Katalogliste erkennt ihre Zeilen an der Referenz.**
`Katalogliste.BeiMehrfachwahl` bestimmt den Anzeigeindex eines Klicks über
`ReferenceEquals` gegen ihre gefilterte Liste. Der Dialog baute seine Zeilen bei
**jeder** Wahl neu; die Liste meldete Index −1, und der zweite Klick auf
dieselbe Zeile blieb wirkungslos. **Behoben:** Die Zeilenobjekte bleiben
dieselben, nur der Wert der Spalte „mitgenommen" wird in der Zeile nachgezogen;
neu gebaut wird nur, wenn ein anderer Bestand hereinkommt.

**PI‑B2 — Ein wiederhergestellter Anker wählt seine Zeile mit.**
Die Markierung wurde vor jedem Klick aus der Wahl neu aufgebaut und der Anker
mit `Hinzufuegen` zurückgesetzt — das **wählt** die Ankerzeile. Ein Klick auf
eine zweite Zeile nahm die erste mit in die Wahl. **Behoben:** Die Markierung
ist der laufende Zustand der Klickregel und wird nur bei einem Filterwechsel neu
aufgebaut; dafür bekommt `Zeilenmarkierung` die Methode `AnkerLoesen` (der Anker
fällt, die Markierung bleibt).

**PI‑B3 — `Path.GetInvalidFileNameChars()` antwortet je Plattform verschieden.**
Unter Linux und macOS gelten nur der Schrägstrich und das Nullzeichen als
verboten. Ein auf einem Mac geschriebenes „Haus: Nord.wpx" ließe sich unter
Windows nicht einmal ablegen — und der Transfer **zwischen** Rechnern ist der
Zweck dieser Datei. **Behoben:** Der Paketname nimmt eine feste Liste plus die
der Laufzeit.

## 5 Nachweise

| Was | Ergebnis |
|---|---|
| `EPOS.Kern.Tests/ProjekttransferSammelTests` | **PI1–PI13 grün**; P1–P13 unverändert grün (27 Fälle zusammen) |
| `EPOS.UI.Tests/Dialoge/ProjektTransferMehrfachTests` | **12 grün** (neu) |
| `EPOS.UI.Tests/Dialoge/ProjektTransferDialogTests` | **17 grün** (fünf Fälle auf die Mehrfachliste umgestellt) |
| Volle Suite `WP-Plan.Kern.slnf` | grün in **beiden Kulturen** (`de-DE`, `en-US`) |
| Windows‑Schale (`EnableWindowsTargeting=true`) | **0 Fehler, 0 Warnungen** |
| `SqlDialektPruefer` | 1 505 SQL‑Texte, **0 Fundstellen** |
| `ResourceDesigner` | wiederholbar, keine Abweichung |
| Referenzlauf 1030/1007/1017/1045/1046 gegen `2026-09-18_R9_Kesselbrennstoff` | **byte‑gleich** |

**Kein Schemaschritt**, Testdatenbank unverändert.

## 6 Ressourcen

35 Schlüssel in beiden `.resx`: `PTR_SP_PROJEKT`, `PTR_SP_ART`,
`PTR_SP_VARIANTEN`, `PTR_SP_KUNDE`, `PTR_SP_GEAENDERT`, `PTR_SP_MITGENOMMEN`,
`PTR_ART_STAMM`, `PTR_ART_VARIANTE`, `PTR_LBL_PROJEKTE`, `PTR_HINWEIS_STAMMZUG`,
`PTR_LBL_ZIELORDNER`, `PTR_BTN_ORDNER`, `PTR_GRUPPE_VARIANTEN`,
`PTR_GESPERRT_GEWAEHLT`, `PTR_MSG_KEINE_WAHL`, `PTR_MSG_KEIN_ORDNER`,
`PTR_BILANZ_EXPORT`, `PTR_BTN_DATEIEN`, `PTR_SP_PAKET_DATEI`,
`PTR_SP_PAKET_PROJEKT`, `PTR_SP_PAKET_VARIANTEN`, `PTR_SP_PAKET_SCHEMA`,
`PTR_SP_PAKET_HINWEIS`, `PTR_PAKET_SCHEMA_OK`, `PTR_PAKET_SCHEMA_ALT`,
`PTR_PAKET_STAMM_AUS`, `PTR_PAKET_DUBLETTE`, `PTR_LAUF_PAKET`,
`PTR_BILANZ_IMPORT`, `PTR_MSG_ZIELNAME_MEHRERE`,
`TRANSFER_EXPORT_VARIANTE_ALS_STAMM`, `TRANSFER_STAMM_BEREITS`,
`TRANSFER_PAKET_SCHEMA`, `TRANSFER_PAKET_UNLESBAR`,
`TRANSFER_PAKET_UNLESBAR_GRUND`.

Stilblatt: Token `--epos-transfer-listenhoehe`, die Regeln
`.epos-transfer-liste/-gruppen/-pakete/-ordner` und die Sperrregel des Schalters
(hart und weich in **einer** Regel).

## 7 Offene Punkte

Sie stehen als Block „Nach #370" in
[`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md) — darunter
die acht Windows‑Abnahmen **A‑PI1‑1** bis **A‑PI1‑8** und der Logbuch‑Satz für
den Wiki‑Sammel‑Upload.

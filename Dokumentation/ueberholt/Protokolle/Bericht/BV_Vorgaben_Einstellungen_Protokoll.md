# Berichtsvorlagen — die zwei Vorgaben in Einstellungen › Bericht (Protokoll)

Statuszeile #600. Die Vorgaben der Installation für die Word-Vorlage (Einstellung `BerichtVorlageWord`) und die
Excel-Vorlage (`BerichtVorlageExcel`) bekommen eine Bedienung: zwei Auswahlfelder in der Rubrik „Bericht“ der
Programmeinstellungen. Anlass: Anwenderentscheid vom 29.09.2026 zu „Nach #597 (a)“ (aus „Nach #512 (g)“ und
„Nach #549 (d3)“); Empfehlung und Ort standen in
[`BV_Reste_Protokoll.md`](BV_Reste_Protokoll.md) Abschnitt 4. Vorgänger:
[`BV_E1_Vorlagenwahl_Protokoll.md`](BV_E1_Vorlagenwahl_Protokoll.md) (Vorgabe, Abweichung und Auflösungskette im Kern)
und [`BV_E7_Excel_Rahmen_Protokoll.md`](BV_E7_Excel_Rahmen_Protokoll.md) (die Excel-Vorgabe). Der gültige Stand steht in
der [Statusdatei](../../../aktuell/Status_iOS_Migration.md) und in der Wiki-Quelle „Berichtsvorlagen“; hier steht, wie
es geworden ist. Ein Opus-5-Agent im eigenen Worktree ab `3fe57f9d`, 29.09.2026; Zusammenführung, Gate, Statuszeile und
Nach-Block macht die Orchestrierung. Kein Schemaschritt, kein Rechenweg, keine Referenzbasis, kein neuer Platzhalter,
keine Katalogfassung, keine Testdatenbank berührt; `EPOS.iOS/` nicht geändert, die Windows-Schale nur mitgebaut.

| Teil | Gegenstand | Commit |
|---|---|---|
| Umsetzung | zwei Auswahlfelder im `EinstellungenDialog`, Hülle `EinstellungenBerichtVorgaben`, KI-Felder, Ressourcen | `6e909b93` |
| Tests | 10 bunit-Fälle, 3 Hüllenfälle | `02e88c60` |
| Wiki | Abschnitt „Einstellungen“ und die zwei Zeilen der Berichtsseite | `9480ee45` |
| Papiere | dieses Protokoll, Indexzeile | Papier-Commit |

## 1 Befund

`BerichtsvorlagenCtrl` führt beide Vorgaben seit BV-E1 bzw. BV-E7 vollständig: `VorgabeWordId`/`SetzeVorgabeWord`
(Einstellung `BerichtVorlageWord`, ohne Einstellung `standard`) und `VorgabeExcelId`/`SetzeVorgabeExcel`
(`BerichtVorlageExcel`, ohne Einstellung `ohne`). Gelesen wurden sie bisher nur von der Auflösungskette
`VorlageFuer`/`ExcelVorlageFuer` (Abweichung des Stammprojekts → Vorgabe → Standard bzw. „ohne Vorlage“) und von
`Entfernen`, das eine entfernte Vorlage aus der Vorgabe nimmt. **Geschrieben hat sie niemand** — die Auswahl der
Berichtsseite setzt die Abweichung des Stammprojekts (`BerichtsKonfiguration`, `SetzeAbweichung`/`SetzeAbweichungExcel`),
und eine Bedienung der Vorgabe gab es nicht. Wer sie setzen wollte, musste die Einstellung von Hand schreiben.

Wie der Kern eine Vorgabe behandelt, deren Datei es nicht mehr gibt, steht in `VorlageFuer`: Die fehlende Kennung
kommt als `Vorlagenwahl.FehlendeId` zurück, die Meldung `BV_VORLAGEN_NICHT_VORHANDEN` nennt sie samt der Vorlage, die
an ihrer Stelle gilt („„Angebot“ nicht vorhanden – „Standardvorlage (EPOS-Plan)“ verwendet“), und gerechnet wird mit der
Standardvorlage. Für Excel dasselbe mit `BV_XL_NICHT_VORHANDEN` („… nicht vorhanden – die Mappe entsteht ohne Vorlage.“).
Die Gruppe „Vorlage“ der Berichtsseite zeigt diesen Fall als **gesperrten, gewählten** Eintrag der Liste mit dem Satz
darunter. Genau das tun die zwei neuen Felder auch.

## 2 Umsetzung

### 2.1 Der Dialog (`EPOS.UI/Dialoge/Admin/EinstellungenDialog.razor`)

Zwei `Auswahlfeld` am Ende der Rubrik „Bericht“, hinter Firma, Vorlagenordner und Logo: **„Vorgabe Word-Vorlage“** und
**„Vorgabe Excel-Vorlage“**, darunter eine `Herleitungszeile` („Gilt für jedes Projekt, das auf der Berichtsseite keine
eigene Vorlage gewählt hat; dort kann ein Stammprojekt abweichen.“). Sie tragen dieselben Daten wie die Auswahl der
Berichtsseite (`Vorlagenzeile`), also auch das Merkmal „mitgeliefert“ und den Sperrgrund je Eintrag.

- **Arbeitsstand wie die übrigen Werte der Rubrik:** `_vorgabeWord`/`_vorgabeExcel` stehen im Dialog; hinaus gehen sie
  erst im OK-Weg, **nach** dem gelungenen Speichern des Wertesatzes, je Wert einmal und nur geändert
  (`VorgabeWordChanged`, `VorgabeExcelChanged`). Abbrechen, Kreuz und Esc melden nichts, ein gescheitertes Speichern
  hält den Arbeitsstand. Ein zweites OK nach dem Speicherweg des Assistenten wiederholt nichts.
- **„Standardwerte“** setzt die Felder auf die Vorgaben der Hülle (`VorgabeWordStandard`, `VorgabeExcelStandard`) —
  Standardvorlage bzw. „ohne Vorlage“ — und speichert wie bisher nicht.
- **Kein Rückweg oder keine Liste, kein Feld** (`VorgabeWordSichtbar`, `VorgabeExcelSichtbar`); die Rubrik selbst steht
  jetzt auch dann, wenn die Hülle nur die zwei Vorgaben reicht.
- **Eine Vorgabe, deren Vorlage es nicht mehr gibt,** steht gesperrt und gewählt in der Liste; ihr Grund hängt als
  `title` am Eintrag und steht als Herleitungszeile unter dem Feld. Das Feld springt nicht still auf einen anderen
  Eintrag, und ein gesperrter Eintrag kommt aus dem `Auswahlfeld` nicht zurück.

### 2.2 Die Hülle (`EPOS.UI.Daten/Bericht/EinstellungenBerichtVorgaben.cs`, neu)

Sie baut Listen, Wahl und Rückwege und wird von `EinstellungenBerichtGaben.Gaben` in den Parametersatz gemischt; die
Windows-Schale ruft weiter nur `EinstellungenBerichtGaben.Gaben()`.

- **Listen** aus `BerichtsvorlagenCtrl.Liste()` und `ListeExcel()` — dieselben Quellen wie die Gruppe „Vorlage“ der
  Berichtsseite. Keine Dateisuche und keine Datenbank in der Oberfläche; die Kennungen des Controllers (`standard`,
  `ohne`, `ausfuehrlich`, `eigen:` + Dateiname) werden je Dialog auf kleine Zahlen abgebildet.
- **Wahl und Fehlfall** aus `VorlageFuer(null)` bzw. `ExcelVorlageFuer(null)`: Ohne Konfiguration bleibt die Abweichung
  eines Stammprojekts außen vor, übrig ist genau die Kette Vorgabe → Standard. Damit kommt der Sperrgrund aus demselben
  Rechenweg, der beim Erstellen greift — ein zweiter Wortlaut entsteht nicht.
- **Rückwege** über `SetzeVorgabeWord`/`SetzeVorgabeExcel`. Die Standardvorlage bzw. „ohne Vorlage“ **entfernt** die
  Einstellung, damit die Vorgabe des Kerns gilt; eine Id, die die Liste nicht kennt, ändert nichts.

### 2.3 Die Berichtsseite

Nachzuziehen war nichts: `BerichtsvorlagenGaben.Stand()` löst die Vorlage bei jedem Aufbau über dieselbe Kette auf und
liest die Einstellung dabei frisch. Ein Test hält das fest (Abschnitt 3): Nach dem Setzen der Vorgabe zeigt die Seite
sie beim nächsten Aufbau, solange das Stammprojekt keine Abweichung trägt; mit Abweichung bleibt diese vorrangig.
Der Hinweis am Feld sagt beides in einem Satz.

### 2.4 Der Hilfe-Assistent

Zwei **Wahlfelder** im Katalog (`KiDialoge.Einstellungen`): `bericht_vorgabe_word` über
`EinstellungenKiSicht.BerichtVorgabeWord` samt Begleiter `BerichtVorgabeWordWahl` und `bericht_vorgabe_excel` über
`BerichtVorgabeExcel` samt `BerichtVorgabeExcelWahl`. Die Einträge sind die der zwei Auswahlfelder, gesetzt wird in den
Arbeitsstand; ohne Feld, bei einem gesperrten Eintrag und bei einer Id, die die Liste nicht kennt, lehnt die Sicht
benannt ab. Katalogzahl 9 → 11 Felder, Eingabebilanz der Maske 10 → 12 Eingabestellen.

### 2.5 Texte

Fünf neue Schlüssel in beiden Sprachen: `EIN_BERICHT_LBL_VORGABE_WORD`, `EIN_BERICHT_LBL_VORGABE_EXCEL`,
`EIN_BERICHT_HINT_VORGABE`, `KI_DLG_ADMSET_BERICHT_VORGABE_WORD_ERL`, `KI_DLG_ADMSET_BERICHT_VORGABE_EXCEL_ERL`.
Der Sperrgrund und der Kurztext „Diese Vorlage ist nicht wählbar.“ kommen aus den vorhandenen Schlüsseln
(`BV_VORLAGEN_NICHT_VORHANDEN`, `BV_XL_NICHT_VORHANDEN`, `BK_BER_VORLAGE_NICHT_WAEHLBAR`). Designer neu erzeugt und
wiederholbar (zweiter Lauf +0).

## 3 Tests

- **`EPOS.UI.Tests/Dialoge/EinstellungenBerichtVorgabenTests.cs`** (neu, 10 Fälle, Kultur de-DE über
  `EposBunitContext`): beide Listen mit Wahl und Hinweiszeile; kein Feld ohne Rückweg und keins ohne Liste; OK übergibt
  eine Änderung erst nach dem gelungenen Speichern und nur einmal; OK ohne Änderung meldet nichts; Abbrechen verwirft;
  ein gescheitertes Speichern hält den Arbeitsstand; eine nicht mehr vorhandene Vorgabe steht gesperrt und gewählt, mit
  ihrem Grund am Eintrag und unter dem Feld, und eine andere Vorlage lässt sich daneben wählen; „Standardwerte“ setzt
  Standardvorlage und „ohne Vorlage“; der Assistent liest und setzt beide Felder und lehnt einen gesperrten oder
  unbekannten Eintrag benannt ab; ohne Felder lehnt er mit dem Namen der Rubrik ab.
- **`EPOS.Kern.Tests/BerichtsvorlagenHuelleTests.cs`** (drei Fälle): Der Parametersatz trifft die Parameter des Dialogs,
  die Listen sind die des Controllers, gewählt ist ohne Einstellung die Standardvorlage bzw. „ohne Vorlage“, der
  Rückweg schreibt die Einstellung, und die Standardvorlage bzw. „ohne Vorlage“ entfernt sie wieder; eine fehlende
  Vorgabe steht gesperrt mit genau dem Satz, den auch der Lauf nennt; die Berichtsseite zeigt eine geänderte Vorgabe
  beim nächsten Aufbau, während eine Abweichung des Stammprojekts vorrangig bleibt.
- **Wachen nachgezogen:** `KiDialogkatalogTests` (Feldzahl, Pfade, Wahl-Typ, Begleiter) und
  `KiMaskenabdeckungWacheTests` (Eingabebilanz des `EinstellungenDialog`).

## 4 Offen

- **Anwenderprobe unter Windows:** die zwei Felder mit einer eigenen Word- und Excel-Vorlage im Vorlagenordner setzen,
  ein Projekt ohne eigene Wahl auf der Berichtsseite öffnen und den Bericht erstellen — die Laufzeile nennt dann
  „Vorgabe der Einstellungen“ als Grund. Dazu der Fall einer Vorlage, die im Explorer weggenommen wird: gesperrter
  Eintrag, Zeile darunter, Lauf mit der Standardvorlage.
- **Reihenfolge der Rubrik:** Die zwei Felder stehen hinter dem Logo. Ob sie besser unmittelbar unter den
  Vorlagenordner gehören, ist eine Sichtfrage der Anwenderprobe.
- **iOS:** Die Rubrik ist plattformfrei und braucht nichts Eigenes; ein iOS-Lauf ist nicht begründet. Die ungebauten
  Änderungen aus BV-E1 bleiben, wie sie sind.
- **Setup-Lauf** nicht nötig: keine Datei der Auslieferung berührt.

## 5 Abnahme

Auf `663e5c58` samt diesem Protokoll: Kern-Filter Release 0 Fehler; `EPOS.UI.Tests` vollständig 6 907 bestanden, 0 rot;
`EPOS.Kern.Tests` mit den betroffenen Klassen (Berichtsvorlagen, `BerichtCtrl`, Einstellungen, Lokalisierung,
Ressourcen, `HuellenTextschluesselWache`, KI-Dialoge und -Wissen) samt `DokumentationLinkWache`,
`RepositoryOrdnungWache`, `WikiProduktdatenWache` 618 bestanden, 0 rot; Windows-Schale (Linux,
`EnableWindowsTargeting`) 0 Fehler; Designer wiederholbar (zweiter Lauf +0). Kein Diagrammbild berührt (ChartProben nicht nötig), kein SQL geändert (SQL-Dialekt-Prüfer nicht
nötig), kein Referenzlauf nötig (kein Rechenweg).

**Logbuch-Vorschlag** (Version beim Anwender zu erfragen): „In den Einstellungen unter ‚Bericht‘ lassen sich die Word-
und die Excel-Vorlage festlegen, mit denen ein Bericht entsteht, solange ein Projekt auf der Berichtsseite keine eigene
Vorlage wählt.“

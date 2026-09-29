# Berichtsvorlagen — „Original geändert – übernehmen?“ (Protokoll)

Statuszeile #NNN (Nummer von der Orchestrierung). Die Zeile „Original geändert – übernehmen?“ der Gruppe „Vorlage“
bekommt ihre Bedienung: Sie steht unter der Prüfzeile der gewählten eigenen Vorlage — Word wie Excel — und bietet
„Übernehmen“ und „Behalten“. Anlass: „Nach #512 (f)“, empfohlen als eigener Auftrag in
[`BV_Reste_Protokoll.md`](BV_Reste_Protokoll.md) Abschnitt 4 („Nach #597 (c)“). Vorgänger:
[`BV_E1_Vorlagenwahl_Protokoll.md`](BV_E1_Vorlagenwahl_Protokoll.md) (Vorlagenordner, Ablagedatei, Herkunft und
Prüfsumme, `OriginalGeaendert`) und [`BV_E7_Excel_Rahmen_Protokoll.md`](BV_E7_Excel_Rahmen_Protokoll.md) (die Zeile
„Excel-Vorlage“). Der gültige Stand steht in der [Statusdatei](../../../aktuell/Status_iOS_Migration.md), in der
Wiki-Quelle „Berichtsvorlagen“ und im Code; hier steht, wie es geworden ist. Ein Opus-5-Agent im eigenen Worktree,
29.09.2026; Zusammenführung, Gate, Statuszeile und Nach-Block macht die Orchestrierung. Kein Schemaschritt, kein
Rechenweg, keine Referenzbasis, kein neuer Platzhalter, keine Katalogfassung, keine Testdatenbank berührt;
`EPOS.iOS/` nicht geändert, die Windows-Schale nur um die Belegung der neuen Naht ergänzt.

| Teil | Gegenstand | Commit |
|---|---|---|
| Kern | `OriginalUebernehmen`, `OriginalBehalten`, Feld `Zurueckgewiesen` der Ablagedatei, Ressourcen | `adbc4279` |
| Oberfläche | Naht `HerkunftDauerhaft`, Hülle, DTO `Originalstand`, Baustein `Vorlagenoriginalzeile.razor` | `fecb4f96` |
| Tests | 5 Kernfälle, 3 Hüllenfälle, 3 bunit-Fälle | `b22e2721` |
| Wiki, Papiere | Abschnitt „Vorlage auf der Berichtsseite“, dieses Protokoll, Indexzeile | Papier-Commit |

## 1 Befund

`BerichtsvorlagenCtrl.OriginalGeaendert(Vorlageneintrag)` lag seit BV-E1 im Kern und war **nicht verdrahtet**: keine
Zeile, keine Handlung. Der Vergleich selbst war vollständig — „Hinzufügen…“ merkt in der Ablagedatei
`.berichtsvorlagen.json` neben dem Dateinamen den Herkunftspfad und die Prüfsumme der kopierten Bytes, und
`OriginalGeaendert` hält die Prüfsumme des Herkunftspfads dagegen; ohne Herkunft oder ohne lesbares Original ist die
Antwort `null`.

Was fehlte, waren drei Dinge: (1) eine Handlung, die das geänderte Original übernimmt — es gab nur „Ersetzen…“ mit
Dateiwahl, was den gemerkten Herkunftspfad ungenutzt ließ; (2) ein Gedächtnis für eine Zurückweisung — ohne das wäre
die Zeile bei **jedem** Aufbau der Gruppe wiedergekommen, solange das Original abweicht, und damit unbrauchbar;
(3) eine Plattformaussage: Auf iOS reicht die Dateiwahl einen Pfad in die Sandbox herein, der Herkunftspfad ist dort
keine dauerhafte Datei des Anwenders, und eine Frage nach dem „Original“ hätte keinen Sinn. Die Hülle konnte das aus
den vorhandenen Wegen (`InWordOeffnen`, `ImOrdnerZeigen`, `OrdnerWaehlbar`) nur erraten.

## 2 Entscheidungsgründe

### 2.1 Schutz der Arbeit am Ort: Sicherung statt Rückfrage

Die Kopie im Vorlagenordner ist der Ort, an dem der Anwender arbeitet (Konzept 10.3: „bearbeitet wird am Ort“). Hat er
sie seit dem Hinzufügen selbst geändert, ist „Übernehmen“ ein Überschreiben eigener Arbeit. Zwei Formen standen zur
Wahl:

* **Rückfrage** — der Baustein `Handlung` trägt dafür ein Feld, „Entfernen“ nutzt es. Sie ist billig, aber ein „Ja“
  löscht die Arbeit endgültig; die Frage schützt nur den, der sie aufmerksam liest.
* **Sicherung nach `Entfernt`** — die bearbeitete Kopie wandert zuerst in den Unterordner `Entfernt` des
  Vorlagenordners, genau wie bei „Entfernen“, und die Rückmeldung nennt den Ordner.

Gewählt ist die **Sicherung**. Sie ist die sicherere Form: Nach einem Fehlgriff steht die eigene Fassung noch da, nach
einem bejahten Dialog nicht. Und sie ist die im Bestand übliche: Der Kern löscht eine Vorlage nie, er legt sie in
`Entfernt` (`BerichtsvorlagenCtrl.Entfernen`, Konzept 10.3 „Liste“). Eine zusätzliche Rückfrage würde das Verfahren
nicht sicherer machen, sondern nur einen Klick verlangen, den die Sicherung überflüssig macht. Gesichert wird **nur**,
wenn die Kopie wirklich abweicht: Stimmt ihre Prüfsumme mit der gemerkten, geht nichts verloren, und der Unterordner
bleibt leer. Lässt sich die Kopie nicht lesen, gilt sie im Zweifel als bearbeitet.

### 2.2 „Behalten“ merkt die zurückgewiesene Prüfsumme

Die Zurückweisung gilt **einem Stand des Originals**, nicht dem Original überhaupt: Die Zeile soll wiederkommen, sobald
sich draußen wieder etwas tut. Gemerkt wird deshalb die Prüfsumme, die zurückgewiesen wurde — im neuen Feld
`Zurueckgewiesen` des Ablageeintrags. `OriginalGeaendert` antwortet `false`, solange das Original genau diese Prüfsumme
trägt. Das Feld wird **duldsam** gelesen: Eine Ablagedatei ohne es bleibt gültig (`System.Text.Json` lässt das Feld
`null`), und die Datei bleibt lesbarer Klartext wie bisher. „Übernehmen“, „Ersetzen…“ und ein erneutes „Hinzufügen…“
schreiben den Eintrag neu und räumen die Zurückweisung damit ab — richtig, denn danach ist die Kopie das Original.

Kein Schemaschritt: Die Ablagedatei liegt im Vorlagenordner, nicht in der Datenbank (Konzept 10.3).

### 2.3 Die Plattform sagt es benannt: `HerkunftDauerhaft`

Statt aus anderen Wegen zu schließen, bekommt die Naht `Berichtsvorlagenwege` eine eigene, benannte Aussage:
`HerkunftDauerhaft` — „bleibt die Datei, aus der ‚Hinzufügen…‘ kopiert hat, erreichbar?“. Die Windows-Schale setzt sie
(`WindowsBerichtsvorlagenwege.Erzeugen`), iOS und jeder Prüfstand lassen sie weg. Ohne sie entsteht die Zeile nicht;
käme eine ihrer Handlungen dennoch herein, lehnt die Hülle sie mit `BK_BER_VORLAGE_ORIGINAL_NICHT_HIER` ab, statt still
zu schreiben (Hausregel `EPOS.UI.Daten`).

### 2.4 Übernehmen geht den Weg von „Ersetzen…“

`OriginalUebernehmen` prüft wie `Ersetzen`: Quelle vorhanden, erlaubte Endung (`.docx`, `.dotx`, `.xlsx`, `.xltx` —
damit sind Makrodateien wie beim Hinzufügen schon an der Endung abgelehnt), gleiche Endung wie die Kopie, keine
Word-Sperrdatei `~$…`. Geschrieben wird über denselben `Lege`/`LegeBytes`-Weg, der die Bytes ganz in den Speicher liest
und danach `Merke` aufruft. Die Hülle schließt wie nach „Ersetzen…“ mit der **vollen** Prüfung ab, und der nächste
Aufbau der Gruppe rechnet ohnehin die Schnellprüfung neu. Die gewählte Vorlage bleibt gewählt, die Abweichung des
Stammprojekts und die Vorgabe der Installation werden nicht angefasst.

### 2.5 Wann geprüft wird

Die Prüfung hängt am Aufbau der Gruppe (`BerichtsvorlagenGaben.Stand`), der ohnehin bei jedem Öffnen der Seite, nach
jeder Handlung und bei jedem Vorlagenwechsel läuft. Sie liest die kleine Datei einmal — keine Dauerüberwachung, kein
Dateisystem-Wächter. Ein nicht erreichbares Original (getrenntes Netzlaufwerk, gelöschte Datei, zu große Datei) bleibt
still: `OriginalGeaendert` liefert `null`, und ohne `true` entsteht keine Zeile.

## 3 Umsetzung

**Kern** (`EPOS.Kern/Controller/BerichtsvorlagenCtrl.cs`): `Ablageeintrag.Zurueckgewiesen` und
`Vorlageneintrag.Zurueckgewiesen` (letzter Konstruktorparameter, vorbelegt); `MerkeZurueckweisung` schreibt nur dieses
Feld und lässt Herkunft, Prüfsumme und Zeitpunkt stehen; `HerkunftPruefsumme` und `AmOrtBearbeitet` als Lesehelfer;
`SichereKopie` legt die Kopie nach `ORDNER_ENTFERNT`; `OriginalGeaendert` berücksichtigt die Zurückweisung;
`OriginalUebernehmen` und `OriginalBehalten` als neue öffentliche Wege mit `Vorlagenergebnis`.

**Naht und Schale**: `Berichtsvorlagenwege.HerkunftDauerhaft`, gesetzt in `WindowsBerichtsvorlagenwege.Erzeugen`.

**Hülle** (`EPOS.UI.Daten/Bericht/BerichtsvorlagenGaben.cs`): `Originalzeile(Vorlageneintrag, bool excel)` baut den
neuen `Originalstand`; `Stand()` belegt `Originalzeile` und `ExcelOriginalzeile`, `Belegen` reicht sie weiter, wenn es
sie gibt. Vier neue Handlungskennungen (`uebernehmen`, `behalten` und beide mit der Vorsilbe `excel:`) in
`HandlungAusfuehren` bzw. `ExcelHandlungAusfuehren`.

**Oberfläche**: `Originalstand` in `BerichtDaten.cs` (Zeichen, Text, zwei `Handlung`en) samt den zwei Feldern von
`Vorlagenstand`; der eigene Baustein `EPOS.UI/Seiten/Berichte/Vorlagenoriginalzeile.razor` zeichnet die Zeile und meldet
die geklickte `Handlung`; `BerichtSeite.razor` bekommt zwei Parameter, zwei Felder samt Nachladen und je eine
Einbindungszeile unter der Word- und unter der Excel-Prüfzeile. Die Handlungen gehen durch `HandlungKlick` — denselben
Weg wie das Menü „…“, mit weicher Sperre, Meldung des Grundes und Nachladen des Standes. Die Zeile nimmt die Form der
Prüfzeile (`epos-vorlage-pruefzeile`, `--befund`), eigen ist nur `epos-vorlage-originalzeile` mit dem kleinen Abstand.

**Texte** (beide Sprachen, Designer nachgezogen): Kern `BV_VORLAGEN_ORIGINAL_KEINS`, `_UEBERNOMMEN`, `_GESICHERT`,
`_BEHALTEN`; Oberfläche `BK_BER_VORLAGE_ORIGINAL_FRAGE`, `_HANDLUNG_UEBERNEHMEN`, `_TIP_UEBERNEHMEN`,
`_HANDLUNG_BEHALTEN`, `_TIP_BEHALTEN`, `_ORIGINAL_NICHT_HIER`.

**KI-Assistent**: nicht berührt. Die Gruppe „Vorlage“ meldet beim Assistenten nur ihre **Felder** an
(`BerichtSeiteKiSicht`: Vorlagenwahl, Prüfzeile, Excel-Wahl, Excel-Prüfzeile, Katalogsuche), keine Handlungen — auch
„Ersetzen…“ und „Entfernen“ stehen dort nicht. Die zwei neuen Handlungen folgen dieser Linie; der Katalog bleibt
unverändert.

## 4 Tests

* `EPOS.Kern.Tests/BerichtsvorlagenCtrlTests` (29 Fälle, alle grün): Übernehmen holt das geänderte Original und führt
  die Ablagedatei nach (danach keine Zeile mehr, `Entfernt` bleibt leer); Übernehmen sichert eine am Ort bearbeitete
  Kopie nach `Entfernt` und nennt den Ordner; Übernehmen lehnt benannt ab an der mitgelieferten Vorlage, ohne gemerktes
  Original, bei fehlendem Original und bei Word-Sperrdatei; Behalten unterdrückt die Zeile bis zur nächsten Änderung,
  lässt die Datei unangetastet, schreibt `Zurueckgewiesen` und fällt mit dem nächsten Übernehmen; eine Ablagedatei ohne
  das neue Feld bleibt gültig; `OriginalGeaendert` ist `null` ohne Herkunft.
* `EPOS.Kern.Tests/BerichtsvorlagenHuelleTests` (33 Fälle, alle grün): die Zeile mit beiden Handlungen und dem
  Herkunftspfad im Kurztext, Übernehmen mit voller Prüfung und gleichbleibender Wahl, Behalten ohne Schreiben,
  weiche Sperre von „Übernehmen“ bei in Word geöffneter Vorlage, und ohne `HerkunftDauerhaft` (iOS) keine Zeile samt
  benannter Ablehnung beider Handlungen.
* `EPOS.UI.Tests/Seiten/BerichtSeiteVorlagenTests` (36 Fälle, alle grün): Zeile unter der Prüfzeile mit Zeichen, Text
  und beiden Knöpfen, Meldung über `HandlungGewaehlt`, weiche Sperre mit Grund statt Handlung, kein Stand = keine
  Zeile, Nachladen bringt und nimmt die Zeile an Word- und Excel-Vorlage.

Abnahme siehe Abschnitt 6.

## 5 Offene Punkte

* **Nur unter Windows prüfbar:** die Zeile am echten Vorlagenordner mit einer Vorlage aus einem gemeinsamen
  Büro-Ordner — Änderung durch einen Kollegen, „Übernehmen“ mit und ohne eigene Bearbeitung am Ort, „Behalten“ über
  mehrere Sitzungen (die Zurückweisung liegt in der Ablagedatei und hält), das Verhalten bei einem getrennten
  Netzlaufwerk und bei einer Datei, die die Cloud nur online hält.
* Die Zeile nennt den Herkunftspfad nur im Kurztext des Knopfes „Übernehmen“. Ob der Pfad in der Zeile selbst stehen
  soll, ist eine Anwenderfrage; der Wortlaut der Zeile folgt dem Konzept.
* Eine Sicherung in `Entfernt` wird nie aufgeräumt — dieselbe Lage wie bei „Entfernen“ seit BV-E1. Wenn der Ordner
  wachsen soll, braucht es eine eigene Regel.
* `EPOS.iOS/` ist nicht berührt; die neue Naht bleibt dort unbelegt, also gibt es die Zeile nicht. Ein iOS-Lauf ist
  nicht begründet.

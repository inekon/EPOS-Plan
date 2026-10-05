# Übergabe HottCAD-Verbund — Stand 05.10.2026

Sitzung „IFC-Ganglinie-Beispiele“ (Orchestrierung Fable 5.1, Agenten Opus 5.5), Zweig `ios_migration_september`. Dieses
Papier hält, was die Fortsetzung des HottCAD-Verbunds braucht; es wandert nach `Dokumentation/ueberholt/`, sobald HC-1,
HC-2 und HC-4 abgeschlossen sind. Grundlage und Regeln: [`CLAUDE.md`](../../../CLAUDE.md),
[Konzept HottCAD-Verbund](2026-10-05_Konzept_HottCAD_Verbund_IFC_Projektdatei_Viewer.md),
[Protokoll HC-3](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-05_HC-3_Zonierungswahl.md),
[Statusdatei Gebäudesimulation](../Status_Gebaeudesimulation_VDI6007.md), [Statusdatei iOS-Migration](../Status_iOS_Migration.md).

## 1. Auftrag des Anwenders (05.10.2026)

Drei Ziele, ausgearbeitet im Konzept HottCAD-Verbund: die IFC-Datei als vollständige Quelle der Hülle (zweiseitige
Randbedingung, Rahmenanteil, Bauteilkörper, Flächenklassifikation), die HottCAD-Projektdatei mit wählbarer Zonierung und
die Ansicht der Hülle mit Randbedingung. Die Fragen F1–F5 des Konzepts sind mit **E87** entschieden (Zonierungswahl mit
Vorgabe DIN-V-18599-Zonen, eigene Größengrenze der Projektdatei 250 MB Windows, 100 MB iOS). Wellen nach Konzept 6:
HC-3 (Nachzug E87 an SQ), HC-1 (Kern: IFC vollenden), HC-2 (Ansicht: Farbmodus Randbedingung), HC-4 („Datei erneut
lesen“ im Gebäudedialog).

## 2. Stand der Wellen

| Welle | Stand |
|---|---|
| HC-3 | **umgesetzt durch die Sitzung „Gebäudesimulation IFC“** als **#736** (Commits `c3d41727` Kern, `99423740` Hülle und Dialog, `229d13f0` und `0b5f83a5` Proben 33, 36, 37, Papiere bis `76a126f6`), dazu **#737** Flächenfilter-Texte (`9910de03`). Gate 736 grün, CI-Lauf `37361791865` (Abschnitt 4). Protokoll siehe Kopf. Die Sätze zu Probe 33 und 36 in [`LIESMICH_Importproben.md`](../../../Referenzlaeufe/Importproben/LIESMICH_Importproben.md) sind nachgetragen |
| HC-3, Dublette | eine zweite, parallele Umsetzung dieser Sitzung (`9612fb3a`, `06d8b4f5`, Merge `b64a5133`, Papiere `2f2797b8`) ist **verworfen**, nie gepusht. Sie bleibt auf den Zweigen `worktree-agent-aa321a7f535f499e5` (Code), `worktree-agent-ae21ef8bc37293252` (Papiere) und `sicherung-hc3-duplikat` erhalten — nicht mergen |
| HC-1 | **läuft** in einem Opus-Agenten im Worktree `.claude/worktrees/agent-a545d1c0434622a4f`, Zweig `worktree-agent-a545d1c0434622a4f`. Committet: Teil 1 `68411baf` zweiseitige Randbedingung, Teil 2 `e6f7dce1` Rahmenanteil, Teil 3 `add95232` Bauteilkörper; Teil 4 Flächenklassifikation in Arbeit |
| HC-2 | nicht begonnen (Konzept 6.4) |
| HC-4 | nicht begonnen (Konzept 6.5) |

**Lehre aus der Dublette:** Eine Ankündigung per Sitzungsnachricht genügt nicht. Vor dem Start einer Welle wird ihre
Zuständigkeit festgelegt und in der Statusdatei angemeldet (wie die Schemanummer), und vor dem Merge wird `origin` geholt
und auf dieselbe Welle geprüft.

## 3. Abnahmeweg HC-1 (Konzept 6.3)

1. Agentenbericht lesen; der Worktree ist sauber und Teil 4 committet.
2. Zweig `worktree-agent-a545d1c0434622a4f` im Hauptbaum mergen — vorher `origin` holen. Der Zweig zweigt vor #736/#737
   ab: Konflikte im Gebäudeimport, besonders `GebaeudeImportDialog.razor` (Flächenfilter #737) und die Ressourcen,
   inhaltlich lösen, beide Seiten behalten; den Designer neu erzeugen statt zu mischen.
3. Gate-Worktree `.claude/worktrees/gate-hc3` auf den Merge-SHA setzen, `git lfs checkout`, Kern-Filter bauen
   (`-p:UseSharedCompilation=false`), voller Testlauf; Fehler aus HC-1 dort auf einem Fix-Zweig beheben und mergen.
4. Papiere: Statuszeile mit der nächsten freien Auftragsnummer nach #737, Protokoll unter
   `Dokumentation/ueberholt/Protokolle/Gebaeudesimulation/`, Konzept 6.1 „HC-1 umgesetzt (#NNN)“,
   Datenaustauschkonzept 15.1 um den Bauteilkörper ergänzen, Indexzeile.
5. Wachen grün, Push.

Der Hauptbaum wird nicht gebaut, solange dort die Windows-Anwendung aus `WindowsFormsApplication1\bin\x64\Debug\` läuft
und Visual Studio offen ist — Build und Tests laufen im Gate-Worktree.

## 4. Befunde

- **CI-Lauf `37361791865`** (Stand `76a126f6`, Status #736/#737): beim Schreiben dieses Papiers noch laufend; Ergebnis
  nachlesen mit `gh run view 37361791865 --json status,conclusion`.
- **Fremder Befund im alten Gate** (Stand der Dublette `b64a5133`, Lauf nicht zu Ende abgewartet): sieben rote Fälle von
  `EPOS.Kern.Tests.GebaeudeEinzonennetzTests.Die_Einzonenreihen_des_Bauteilwegs_bleiben_bitgleich` — „Leistungsgrenze“,
  „AK1 Heizseite“, „ideal“, „Volumen und Raumhöhe“, „Sommerlüftung“, „Kühlung ideal“, „AK1 Kälteseite“. Nicht HC-3;
  gegen den grünen Lauf auf `origin` prüfen, bevor man ihn jemandem zuschreibt.

## 5. Offene Punkte aus #736/#737

- Windows-Sichtabnahme des Wahlfelds „DIN-V-18599-Zonen | Simulationszonen“, der Herkunft je Zone und der Filtertexte.
- Logbuch-Version beim Anwender erfragen; Wiki-Upload der Seite Gebäudeimport im nächsten gebündelten Upload.
- iOS-Grenze 100 MB messen wie G4-8.

## 6. Regeln der Sitzung

- **Haltegrenzen des Anwenders:** 90 % der Fable-Woche, 80 % „Weekly · all models“ — dann anhalten und übergeben.
- **Modellregel:** Fable nur Orchestrierung, Opus Umsetzung, Sonnet Papiere und Suchen, Haiku Prüfungen.
- Keine macOS-, iOS- oder Setup-Läufe ohne Rückfrage.

## 7. Aufräumliste

- Worktrees `agent-aa321a7f535f499e5` und `agent-ae21ef8bc37293252` entfernen (`git worktree remove --force`); die
  Zweige bleiben stehen.
- `gate-hc3` bleibt für die Abnahme von HC-1 stehen und wird danach entfernt.
- `agent-a545d1c0434622a4f` erst nach dem Merge von HC-1 entfernen.

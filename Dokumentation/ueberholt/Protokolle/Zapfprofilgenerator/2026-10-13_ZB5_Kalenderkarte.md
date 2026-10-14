# Zapfprofil: Kalender der Zone als aufklappbare Karte (W3b, 13.10.2026)

Protokoll der Welle **W3b** der Sitzung „Zapfprofilgenerator Cloud“ zu dem Vorschlag **ZB5** (Maske) und dem
Entscheid **ZB‑Q2 A** des
[Konzepts Zapfprofil — Benutzerführung](../../../aktuell/Konzept_Zapfprofil_Benutzerfuehrung_EPOS-Plan.md)
(Befund B6).

**Rahmen.** Worktree `agent-ab1c2fbf6f3ea9b35`, Zweig `worktree-agent-ab1c2fbf6f3ea9b35`, Basis ist der
Stand mit der Welle W3a (Merge `55dcd34ff`), Opus 5.5. Zwei Commits `0aa0b1438`, `10b319e6a`. Kein
Schemaschritt, kein Rechenweg.

---

## 1 Karte „Kalender“ im Zonenblock

Die Felder Kalender, Bundesland und Ferienzeitraum 1 bis 4 weichen der Karte „Kalender“ (`Klappgruppe`).

- **Zugeklappt:** die wirksame Lage in einer Zusammenfassung (Gebäude oder Nutzungsart, Zahl der Ferien,
  Feiertage); sie bricht um, statt den Titel zu stauchen.
- **Aufgeklappt:** die Quelle als waagerechte Wahl (bei mehreren Gebäuden eine Liste); bei gebundenem Gebäude
  dessen Wochenende, Feiertagsland und Ferien als Text mit dem Vermerk „aus dem Gebäude“; die eigenen Ferien
  als Liste mit Name, „von“ und „bis“ (Baustein `Gemeinjahrdatum` wie im Gebäudedialog), „Entfernen“ und
  „+ Ferien“ **ohne Obergrenze**; das Format steht einmal unter der Liste, die Felder tragen es als Tooltip.
- Das gesperrte Feld Bundesland und der Grund `ZPG_GRUND_BUNDESLAND` entfallen; das Feiertagsland nennt die
  Zusammenfassung.

## 2 Jahresraster

Neuer Baustein `EPOS.UI/Bausteine/Jahresraster.razor`: 12 Monate × 31 Tage im eigenen Rollkasten, je Tag eine
Fläche in der Farbe seiner Tagesart, darüber die Legende mit der Zahl der Tage je Art; Monatskurznamen. Dieselbe
Stilregel wie die Kalenderbedienung des Gebäudedialogs, aber ohne Bedienung. Der Baustein rechnet nichts: Die
Tagesarten (365 Werte des Gemeinjahrs) kommen aus den Tagtypen der Vorschau, also aus dem Rechenweg.

## 3 Kern, Hülle, DTO

- Neuer Leser `ZapfprofilCtrl.GebaeudekalenderDesProjekts` (Wochenende, Feiertagsland und Ferien der Gebäude
  des Projekts, über `GebaeudeLesen` des Laufs).
- DTO `ZapfprofilFerienDaten` trägt Von/Bis als **Tag des Jahres**; die Hülle bildet die Liste 1:1 ab, ohne
  aufzufüllen; die Prüfung bleibt wie bisher.
- KI-Sicht: Ferienzeilen `ferien_name`, `ferien_von`, `ferien_bis`; die Zeile hinter der letzten legt an.

## 4 Ressourcen

38 Schlüssel neu, 8 entfernt (beide Sprachen, Designer neu erzeugt): neu die Kalenderkarte (`ZPG_KAL_*`,
Zusammenfassungen `ZPG_KAL_SUM_*`, Ferienliste, Jahresraster, Datumshinweise, KI-Namen der Ferienfelder);
entfernt `ZPG_LBL_BEGINN_TAG`, `…_BEGINN_MONAT`, `…_ENDE_TAG`, `…_ENDE_MONAT`, `ZPG_HINW_FERIEN`,
`ZPG_LBL_BUNDESLAND`, `ZPG_BUNDESLAND_KLIMAREGION`, `ZPG_GRUND_BUNDESLAND`. Die Wiki-Quelle „Brauchwasser-Zapfprofil“
beschreibt die Karte.

## 5 Tests und Abnahme

Neu `EPOS.UI.Tests/Bausteine/JahresrasterTests`, `EPOS.UI.Tests/Dialoge/ZapfprofilDialogStufenTests.Kalender.cs`,
`EPOS.Kern.Tests/ZapfprofilGebaeudekalenderTests`; angepasst die Stufentests, die KI-Maskenabdeckung und
`ZapfprofilBearbeitungOhneAusnahmeTests`. Sichtprobe im Browser, danach der Nachzug des Kopfes und der
Ferienzeile (zweiter Commit). Ergebnisse im Worktree, soweit die Commit-Botschaften sie nennen; maßgeblich ist
das Gate auf dem Merge-Stand (Statuszeile).

## 6 Offen

Der Aufbau der übrigen Karten und die haftende Vorschau folgen mit W4.

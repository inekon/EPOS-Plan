# Protokoll P4a — Pufferspeicher-Auslegung: Ressourcenschlüssel für Herkunft und Rechenweg, Vorbelegung aus Teillastfeldern (03.10.2026)

Kleine Welle nach dem Auftrag „starte 3 und 7 als kleine Welle“ (Anwender, 03.10.2026) aus den Folgeaufträgen des
Blocks „Nach #688“; Vorgänger [`2026-10-03_P3_Pufferauslegung_Bericht_Wiki.md`](2026-10-03_P3_Pufferauslegung_Bericht_Wiki.md).
Bau durch einen Opus-Agenten im Worktree, Zweig `claude/p4a-pufferauslegung` auf `17ada8b0`. Statuszeile **#689**.

## 1 Was gebaut ist

| Punkt | Commit | Inhalt |
|---|---|---|
| 3 | `d6a8944f` | Klasse `Textbaustein` (`EPOS.Kern/Allgemein/Pufferauslegung/Textbaustein.cs`): Schlüssel, deutsches Rückfallmuster, Argumente (Zahlen, Texte, weitere Bausteine); Auflösung über `MyResource.Resource` in der gewünschten Kultur, Zahlen im Format `#,##0.###`; fehlt der Schlüssel, gilt das deutsche Muster. Umgestellt: `PufferKriterium.HerkunftBaustein/RechenwegBaustein`, `PufferWarnung`, `PufferVerlust`, `PufferNutzungsprofilAbleitung`, `PufferAuslegungHerkunft(Feld, Quelle, Baustein)`, `PufferAuslegungGespeichert` (Nutzungsprofil- und Bemessend-Herkunft); Quellen der Vorgabeliste über `PufferAuslegungVorgaben.Quellentext`; Hülle und Bericht lösen in der Oberflächen- bzw. Berichtssprache auf. 143 Schlüssel `PAUS_HERK_*`/`PAUS_WEG_*` de/en. Tests `PufferTextbausteinTests` (7), Bericht (+1), bunit (+1 englischer Fall) |
| 7 | `4125c57a` | `PufferAuslegungCtrl.Vorbelegen`: Wärmepumpe an Rang 1 liest `Mindestleistung_kW` in der Rangfolge Projektgerät (`Tab_WP` über `Tab_Energieanlagen.ID_WP`) → Katalog (`Tab_WP_STAMM` über `ID_Stamm`) → Vorgabe; Wert > 0 setzt `MindestleistungKw` (Summe über Rang 1), unter der Nennleistung dazu `Geregelt`. BHKW an Rang 1: `Tab_BHKW.Mindestlaufzeit_min` > 0 setzt `MindestlaufzeitMin`, K3 rechnet damit. Neue Quelle `PufferHerkunftsquelle.TEILLAST` („Teillastfeld der Anlage“), 4 Schlüssel. Tests in `PufferAuslegungCtrlTests` (+3, Projektkopien 1045 und 1030; Referenzprojekte unberührt) |

## 2 Festlegungen

- Kein Katalogweg beim BHKW: `Tab_BHKW` hat kein `ID_Stamm`, ein Textabgleich über den Bezeichner ist nach den Regeln
  ausgeschlossen; es gilt Projektgerät, dann Vorgabe.
- Keine BHKW-Mindestleistung aus `Grenzleistung` — das würde K3 und D2 des BHKW umdeuten (offen, Anwenderentscheid).
- Deutscher Klartext leicht geändert: Tausendertrennung in der Herkunftsliste, K10 nennt „Flachkollektor“/
  „Röhrenkollektor“, die Zapfprofil-Herkunft die Topologie mit Anzeigenamen.
- Klartexte der Warnungen mit Zahlen (Tooltip), `Nenninhaltsquelle` und Kaskadeneinträge außerhalb der vier
  Erzeugertypen bleiben deutsch bzw. Bezeichner.

## 3 Nachweise

Gate des Agenten (`4125c57a` auf `17ada8b0`): Kern-Filter 0 Fehler; Tests Kern 10 459 (1 übersprungen), UI 7 368,
KiKern 549, SpeicherEngine 397, SpeicherPlanung 27 (1 übersprungen); Windows-Schale auf Linux 0 Fehler; ResourceDesigner
unverändert; SQL-Prüfer 2 271 Texte, 0 Fundstellen; Referenzlauf CI-Sieben 7/7 PASS gegen R33. Merge in den Hauptbaum
(`1feecbf6`) mit origin `0224b5ef` (nur Statusvermerk): Kern-Filter 0 Fehler, UI 7 368, Kern-Auswahl (Pufferauslegung, Textbausteine, Bericht, Wachen, Ressourcen) 838 grün, Windows-Schale 0 Fehler, ResourceDesigner unverändert, SQL-Prüfer 2 271 Texte 0 Fundstellen; der Kern-Lauf der CI ist der volle Nachweis.

## 4 Offen

Entscheid, ob das BHKW `Grenzleistung` als Mindestleistung vorbelegt; Warnungs-Klartexte mit Zahlen als Schlüssel;
Sichtabnahme unter Windows; Wiki-Upload gebündelt (1.2.0.7).

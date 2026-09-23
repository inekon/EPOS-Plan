# Z1 — Zapfprofilgenerator: Bilanz deterministisch (Protokoll, 23.09.2026)

Statuszeile #443 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Zeile Z1
in Kapitel 7 und die Nachträge N7–N9 im
[Umsetzungskonzept](../../../aktuell/Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md); Abschnitt 8
der [Übergabe](../../../aktuell/Zapfprofilgenerator/2026-09-23_Uebergabe_Zapfprofilgenerator.md);
Vorstufe im Protokoll [Z0](2026-09-23_Z0_Grundlagen_und_Schema.md). Zweig `z1` von `7062b849`,
40 Commits bis `1f68bac7` (74 Dateien); letzter Code-Commit `99eb927e`, danach nur Wiki-Entwurf
und Papiere.

## Auftrag

Stufe Z1 nach Kapitel 7 des Umsetzungskonzepts: die Bilanz deterministisch — S1 Mengengerüst mit
Temperaturumrechnung und Messwertgrenzen, S2 Kalender und Formvektor mit Tagtypgewicht und
Ferienregel, S5 Zirkulation mit Zonenanteil und Vorgabe Flächenkennwert, Fassade mit
`Bilanzreihe`, die Weiche im Brauchwasserkanal (2.2) samt getrennter Monatssummen,
`ZapfprofilCtrl.Speichern` und `Eingang`, der Dialog Stufe Einfach mit Vorschau als Überlagerung,
Knopf, Optionsgruppe und Leiste „monatlicher Verlauf" im Bedarfsprofil-Dialog mit gemeinsamem
`DbVorgang`, Wiki-Entwurf; dazu der Inhaltsvergleich im Projektimport (ZU17, N6). Kein
Schemaschritt: Z1 schreibt in die Tabellen von T1 (Schritt 103). Ausgeführt in drei Gruppen
(Rechenweg · Weiche, Schreibweg und Projekttransfer · Oberfläche) durch Agenten mit
`model: opus` im Worktree `z1`, je Gruppe eine Gegenprüfung durch einen zweiten Agenten; ohne
Push und ohne CI-Lauf.

## Gruppen und Commits

| Gruppe | Inhalt | Commits |
|---|---|---|
| 1 Rechenweg | S1 `Mengengeruest`, S2 `Zapfkalender`, `Kaltwassergang`, `Formvektor`; Herkunftsprotokoll, Eingang mit Parameterschlüsseln | `4058c6c7` |
| 1 | `Bilanzreihe`; Zirkulationskanal S5 (Leitungslänge, Anteil, Flächenkennwert, manuell; Zonenanteil, Laufzeitfenster) | `30fab3c6` |
| 1 | Fassade `ZapfprofilRechner` mit Ergebnis, Kennzahlen, Hinweisen und benannten Ablehnungen | `8c19ff28` |
| 1 | Tests zu 2.4 und 4.1–4.3; unabhängiger Referenzfall 8760 h (Python-Skript), `EinheitenWacheTests` | `1d737a7b`, `3dd5c74d` |
| 1 | Nachbesserung: Fläche A_N, Kalenderregel, Messwertregeln, `PARAMETER_FEHLT`, Referenzfall mit vier Zonen; Umgebungswächter | `08596382`, `6c374bf2` |
| 1 | Nachtrag N7 | `a2181bca` |
| 2 Weiche | Testkatalog um die fünfzehn Schlüssel aus `ZapfParameter`; Testdatenbank (LFS) mit 18 Parameterzeilen, Schemastand unverändert | `b3d17830`, `650565e0` |
| 2 | `ZapfprofilCtrl.Eingang`, `Rechnen` und `Speichern` in einem `DbVorgang`; gebundenes Gebäude (A8) | `b5a01bb9` |
| 2 | Weiche `SimulationWaermebedarf.BrauchwasserAusGenerator` exklusiv für Weg `GENERATOR`, Monatssummen der Zirkulation getrennt; `BedarfsVorschauCtrl.ProjektVorschau` mit Arbeitsstand | `65d69a4a` |
| 2 | ZU17: Inhaltsvergleich namensgleicher Tww-Katalogzeilen im Projektimport | `03e42d09` |
| 2 | `ZapfprofilWeicheTests`, `ZapfprofilSpeichernTests`, `TwwKopierstellenTests` | `c5e78f12` |
| 2 | Nachbesserung: Nullzone und Abbruch je Projekt, Abbruchgrund in den Hüllen, Prüfungen des Schreibwegs, Manifestprüfung, Testkatalogskript führt nach | `040e1a69`, `5b422816`, `d6ced005`, `4b363873`, `d19d653c` |
| 2 | Nachtrag N8 | `6c8db2a2` |
| 3 Oberfläche | `ChartRenderer.StundenprofileModell`, `ZapfprofilBilder`, `Zapfauswertung`; elf neue Bilder in `ChartProben` | `b270f36c` |
| 3 | Ressourcen `ZPG_`/`BPF_` beider Sprachen, `Resource.Designer.cs` neu gezogen | `1ecc591e` |
| 3 | DTO `ZapfprofilDaten`, Textbündel `ZapfprofilTexte`; Hülle `EPOS.UI.Daten/Bedarf/ZapfprofilHuelle`, Naht `Zapfprofilwege`, `ZapfprofilBehaelter`; Tests | `7fe297f6`, `868320d8`, `589e5fc7` |
| 3 | `ZapfprofilDialog.razor` Stufe Einfach; Knopf, Optionsgruppe und Leiste im `BedarfsProfileDialog`; Hüllen `BedarfsProfileHuelle`, `StartseiteHuelle` (Windows), `GebaeudeKatalogHuelle`, `BedarfErgebnisHuelle` (plattformfrei); bunit | `8739b82e`, `64020cdc`, `dedda420`, `5c3425cc` |
| 3 | Wiki-Entwurf `Programm Dokumentation - Brauchwasser-Zapfprofil.wiki` (Repo-Quelle, kein Upload) | `874e1457` |
| 3 | Nachbesserung: Schreibweg im OK, Verwaltung ohne Behälter, Hinweis ZU5, Zapfschicht der Monate im Kern, Tagesgang mit dem Kalender der Zone, Meldungen über die Position, Reiter Kennzahlen, leise Zeile, Wiki nachgezogen | `daa4b59b`, `0e8eb55e`, `dc3b9e47`, `d57deaa8`, `5e5e8144`, `04ab0356`, `1409e41f`, `99eb927e`, `83715cfe` |
| 3 | Nachtrag N9; Logbuchdatei unter `Projekte/Wiki/` entfernt (der Satz steht in 5.8) | `1f68bac7` |

Alle 40 Commits tragen den Trailer des arbeitenden Modells; keine Betreffzeile über 72 Zeichen.

## Gates

Im Worktree `z1` nach den Nachbesserungen der Gruppe 3:

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler.
- Voller Testlauf `WP-Plan.Kern.slnf` mit den xUnit-Schaltern: mindestens 11 128 grün, 0 rot,
  1 übersprungen.
- `ChartProben`: 135 Bilder, 0 Verstöße; elf Zapfprofilbilder neu, kein altes geändert.
- Windows-Schale mit beiden Baubefehlen: 0 Fehler.
- Referenzfall 8760 h: je Stunde Abweichung 0 nach Rundung auf 1e‑9 kWh, Monats- und
  Jahressummen und Kennzahlen gleich; das Skript schreibt wiederholbar byte-gleich.
- Wiki-Entwurf: Suchmuster aus `CLAUDE.md` 0 Treffer, `WikiProduktdatenWacheTests` grün.

In Gruppe 2, nach der Weiche: Referenzlauf 1030, 1007, 1017, 1045, 1046 gegen
`2026-09-22_R11_Bestandsbefunde` PASS, byte-gleich. Kein Projekt der Referenzbasis trägt eine
Projektzeile oder Zone (`ZapfprofilWeicheTests`); der Bestandsweg ohne Projektzeile und mit Weg
`BESTAND` bleibt byte-gleich. Gruppe 3 fügt dem Kern die Monatsreihe der Zapfung (auf dem
Bestandsweg 0) und den Hinweis ZU5 (nur im Generatorweg) hinzu; der Referenzlauf auf dem Endstand
und der `SqlDialektPruefer` über die neuen SQL-Texte (`ZapfprofilCtrl`, Projektimport) gehören zum
Gate auf dem Merge-Stand.

## Gegenprüfungen

Je Gruppe prüfte ein zweiter Agent nach der Umsetzung; nachgebessert wurde in eigenen Commits, wo
ein Befund dem Papier oder dem Mockup widersprach, gilt das Papier. Schwereverteilung:

| Gruppe | Befunde | hoch | mittel | gering |
|---|---|---|---|---|
| 1 Rechenweg | 13 | 1 (Betreffzeilen der Commits) | 7 | 5 |
| 2 Weiche, Schreibweg, Projekttransfer | 13 | 0 | 2 | 11 |
| 3 Oberfläche | 12 | 0 | 2 | 10 |
| Summe | 38 | 1 | 11 | 26 |

Der einzige hohe Befund betraf die Betreffzeilen, keinen Fachinhalt; alle wesentlichen Befunde
sind behoben.

| Gruppe | Wichtigste Befunde und Nachbesserung |
|---|---|
| 1 | A_N der Zirkulation aus den Zonenflächen ohne α, sonst kürzte eine Zone außerhalb Z1 doppelt; Samstag und Sonntag nur mit Wochenendkennzeichen; Messwert ohne Einheit bzw. in kWh ohne Grenze benannt abgelehnt; fehlende Schwellen als `PARAMETER_FEHLT` statt Rückfallwert; Referenzfall auf vier Zonen erweitert; Umgebungswächter um Datenzugriff und Umgebung |
| 2 | abgelehnte Zone trägt 0 und steht als Warnung im Protokoll, benannter Abbruch nur, wenn das Projekt nicht rechnen kann (Befunde 1–3); Startseite und Ergebnisvorabrechnung nennen den Abbruchgrund; fremdes Gebäude, Projektgrößen außerhalb ihrer Wertemenge und Wohnungstyp einer anderen Zone benannt abgelehnt, Flächenregel gemischter Zonen (4–6, 10); Kindtabellen des Imports aus festen Tabellen statt aus dem Manifest (7); Testkatalogskript führt abweichende Werte nach (12); 8–10 in N8 |
| 3 | Schreibweg im OK vor dem Schließen, eine Ablehnung hält den Dialog offen (1); Hinweis ZU5 (2); Reiter Kennzahlen nach dem Mockup, die Gleichzeitigkeit bleibt in der Bilanz (3); leise Zeile ohne Verweis auf die gesperrte Stufe (4); Verwaltung reicht keinen Behälter (5); Zapfschicht der Monate im Kern statt in der Hülle (6); Tagesgang mit dem Kalender der Zone (7); Meldungen über die Position, doppelte Zonennamen abgelehnt (8); 9–12 in N9 |

## Abweichungen vom Papier

Die Abweichungen und Festlegungen stehen in den Nachträgen N7 (a)–(k), N8 (a)–(g) und N9 (a)–(l)
des Umsetzungskonzepts; der Hauptteil ist an den betroffenen Stellen mit „(N7)", „(N8)" und „(N9)"
berichtigt. Kurz: Python-Skript statt Tabellenkalkulation für den Referenzfall; Signaturen
(`ZonenStand`, `Mengenergebnis`, zweigeteilte Zirkulation, unveränderliche `Bilanzreihe`); f_θ auch
für die Flächenformel; Laufzeitfenster um den Schwerpunkt der Zapfung; `Speichern` liefert den
Stand mit Ids, `Rechnen` ist der gemeinsame Aufruf von Lauf und Vorschau; Nullzone statt Abbruch,
Abbruch nur je Projekt; Flächenregel des gebundenen Gebäudes; Provenienz nicht im Inhaltsvergleich;
`StundenprofileModell` für mehrere Reihen; Schreibweg im OK vor dem Schließen; Hinweis ZU5 schon
in Z1; Zonenliste als Haustabelle statt `Raster`; Logbuch-Satz in 5.8 statt eigener Datei.

## Zwischenfall

Eine Zweitinstanz eines Workflow-Agenten überschrieb kurz eine Datei; behoben (Regel dazu in der
Übergabe, Abschnitt 8).

## Offene Punkte

- **Sichtabnahme unter Windows** durch den Anwender: Startseite → Kachel Brauchwasser → Knopf
  „Zapfprofil erzeugen…"; Optionsgruppe „Rechenweg Brauchwasser" hin und zurück; Überlagerung mit
  den Reitern Tagesgang, Wochenprofil, Jahresgang und Kennzahlen; OK des Bedarfsprofil-Dialogs;
  Ergebnisdialog mit gestapelten Brauchwassersäulen (Zapfung und Zirkulation).
- **Merge nach `ios_migration_september`:** Dort stehen die Schemaschritte 104 und 105, die
  Testdatenbank auf 105 und die Referenzbasis R12 der Gebäudesimulation
  (`2026-09-23_R12_Gebaeudemodell`). Beim Merge die Testdatenbank von dort übernehmen und
  `Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py` erneut einspielen (LFS); danach das Gate auf
  dem Merge-Stand samt Referenzlauf gegen die dann gültige Basis und `SqlDialektPruefer`.
- **ChartProben-Messlatte:** die elf Zapfprofilbilder beim nächsten Kern-Lauf auf ubuntu in
  `Proben/ChartProben/Messlatte_2026-09-20.sha256` aufnehmen (N9, Folgen (a)).
- **Wiki und Logbuch:** Upload der Seite „Brauchwasser-Zapfprofil" gebündelt mit den übrigen
  Seiten; Logbuch-Satz aus 5.8, Versionsnummer beim Anwender.
- **Folgeposten aus N7–N9:** Bezugstemperaturen einer Nutzungsart mit Flächenformel im Katalog
  (N7 (b), mit dem Auslieferungskatalog); Frage an den Anwender, ob die Provenienz im
  Inhaltsvergleich mitzählt (N8 (e) 1, vor Z2); Journalmodus der Testdatenbank (N8 (g), mit ZU18);
  Verweis der leisen Zeile, Hinweis ZU5 in der Warnliste, `θ_Anzeige` und Schwelle der
  Stundenzählung (N9 (c), (g), (h), Z3/Z4); Zonenliste auf `Raster`, sobald der Baustein einen
  Tabellenfuß führt (N9 (k)).
- Stufen Erweitert und Experte, Dauerlinie, Stochastik und Auslegung stehen weich gesperrt da und
  folgen mit Z2 bis Z4.

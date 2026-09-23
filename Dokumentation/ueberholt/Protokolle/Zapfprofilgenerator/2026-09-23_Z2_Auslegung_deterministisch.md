# Z2 — Zapfprofilgenerator: Auslegung deterministisch (Protokoll, 23.09.2026)

Statuszeile #451 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Zeile Z2
in Kapitel 7 und die Nachträge N10 und N11 im
[Umsetzungskonzept](../../../aktuell/Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md); Abschnitt 9
der [Übergabe](../../../aktuell/Zapfprofilgenerator/2026-09-23_Uebergabe_Zapfprofilgenerator.md);
Vorstufe im Protokoll [Z1](2026-09-23_Z1_Bilanz_deterministisch.md). Zweig `z2` von `13fff671`,
29 Commits bis `89e46543` (61 Dateien); letzter Code-Commit `4b427102`, danach nur Nachtrag N11 und
Wiki-Entwurf. Merge, Nachzug und Tests in sechs Commits `04bf2ae0` bis `cb347e02`, dann die Papiere.

## Auftrag

Stufe Z2 nach Kapitel 7 des Umsetzungskonzepts: die Auslegung deterministisch — Bedarfstag mit
Vorgaberegel und Konstruktor, Wochenreihe, Summenlinie mit Speicherart, Übertrager, Einschaltpunkt,
Wertepaarkurve, Monotonieprüfung und Ladezeit, Schnellpfad, Wohnungstabelle und DIN-4708-Kennzahl,
DIN 1988-300 nachrichtlich, Speicherauslegung nach Vorlage V4 mit Ladefenster, GLF, Plausibilitätsband
und Warnliste, Großanlagenerkennung, Topologiegruppen mit genau einer Empfehlung; Überlagerung
„Auslegung" samt Konstruktor; `SummenlinieModell`. Kein Schemaschritt: Z2 liest und schreibt die
Tabellen von T1 (Schritt 103). Zwei Gruppen (Rechenweg · Oberfläche der Auslegung) durch Agenten mit
`model: opus` im Worktree `z2`, je Gruppe eine Gegenprüfung durch einen zweiten Agenten; ohne Push
und ohne CI-Lauf. Der Abschluss (Merge, Nachzug, Papiere, Gate) lief als eigener Auftrag im selben
Worktree.

## Gruppen und Commits

| Gruppe | Inhalt | Commits |
|---|---|---|
| 1 Rechenweg | `Bedarfstag` mit Vorgaberegel (Konstruktor, Referenztag, Normtag), `Wochenreihe`; `Summenlinie` mit Nachweis, Bisektion, Wertepaarkurve und Schnellpfad; `Din4708Kennzahl` mit Wohnungstabelle | `d44a31d2`, `1b82bfe5`, `4d93d77a` |
| 1 | `TwwSpeicherauslegung` nach Vorlage V4, `Grossanlage`; `Auslegungsergebnis` je Topologiegruppe mit genau einer Empfehlung, Fassade `ZapfprofilAuslegung`, `ZapfprofilTrennungWacheTests` | `547e3f81`, `0349300c` |
| 1 | unabhängiger Referenzfall (Python-Skript); Testkatalogskript um Bedarfstage, DIN-4708-Werte und die Parameter der Auslegung | `0364e93d`, `fc152ff3` |
| 1 | Nachbesserung: Zeitkonstante nach A1 und baubare Wertepaarkurve; Wache des Testkatalogs mit Übergang; Trennungswache; Summenkontrolle; eine Speichertemperatur je Gruppe; Nenninhalt, Füllstand, Katalogtag, Übertragerpaare; Fassadenfall im Referenzfall | `5da04f68`, `312b44fd`, `dae5344d`, `0055ab25`, `c99ae1c5`, `d5d011ac`, `13b18922` |
| 1 | Nachtrag N10 | `0b7080c3` |
| 2 Oberfläche | Datenseite der Auslegung im `ZapfprofilCtrl`; `ChartRenderer.SummenlinieModell` mit zehn neuen Bildern in `ChartProben`; DTO, Hülle `ZapfprofilHuelle.Auslegung`, 234 neue Schlüssel je Sprache | `42d078a7`, `47ef6c59`, `f3aa8cc6` |
| 2 | `ZapfprofilAuslegungDialog.razor` samt `BedarfstagKonstruktor.razor`; Wiki-Abschnitt „Auslegung" (Repo-Quelle); Stilblatt | `b1b02816`, `d3af4063`, `61a61b37` |
| 2 | Nachbesserung: Punkt nie im Rechenweg, Zonenänderung überholt ihn; Sperrgrund des Normtags und `HuellenTextschluesselWacheTests`; Konstruktor öffnet sich, Banner „Spitzen unterschätzt"; Fehlendes benannt gesperrt; Konstruktor mit Entwurfszeilen, Namen und Feldnamen | `556134fc`, `5299e63f`, `90f9a190`, `cf085f8b`, `4b427102` |
| 2 | Nachtrag N11; Wiki nach der Gegenprüfung, Referenzprofil ohne Typcode | `521d3ef4`, `c9388a54`, `89e46543` |
| Abschluss | Merge von `origin` (KU1 Welle 3); Testdatenbank (LFS); Wache ohne Übergang; Nachtrag `Referenzlaeufe/LIESMICH.md`; zweiter Merge (#449); zwei Tests lesen den Katalogstand | `04bf2ae0`, `a228185d`, `8cc16a62`, `7bde6800`, `169716e8`, `cb347e02` |

Alle Commits tragen den Trailer des arbeitenden Modells; keine Betreffzeile über 72 Zeichen.

## Gates

**Im Worktree nach Gruppe 2 (vor dem Merge):** Kern-Build 0 Fehler; voller Testlauf 11 455 grün,
0 rot, 1 übersprungen (EPOS.Kern.Tests 5 098, EPOS.UI.Tests 5 420, KiKern 524, SpeicherEngine 386,
SpeicherPlanung 27); `ChartProben` 145 Bilder ohne Verstoß (zehn neu, kein altes geändert);
Windows-Schale mit beiden Baubefehlen 0 Fehler; Referenzfall der Auslegung (drei Summenlinien,
Speicherauslegung, Fassadenfall mit zwei Zonen) Abweichung 0 auf 1e‑9, zweiter Lauf byte-gleich.

**Auf dem Merge-Stand `cb347e02`** (`origin` bis `39c63361` enthalten):

- Kern-Build 0 Fehler; voller Testlauf 11 750 grün, 0 rot, 1 übersprungen (EPOS.Kern.Tests 5 256,
  EPOS.UI.Tests 5 557, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27); Wartezeit 0 s. Der erste
  Lauf nach dem Nachzug war in zwei Fällen rot, die Werte des Katalogstands Z1 fest trugen
  (`ZapfprofilSpeichernTests`: 18 statt 72 Parameter; `TwwKopierstellenTests`: Wert der
  Ausstattungsklasse A, vom Skript nachgeführt); beide lesen ihn jetzt aus der Testdatenbank
  (`cb347e02`). Fremde rote Tests: keine.
- Werkzeugtests Auslieferungsvorlage 26/26; Windows-Schale mit beiden Baubefehlen 0 Fehler;
  `ChartProben` 145 Bilder, 0 Verstöße; `SqlDialektPruefer` 1 726 SQL-Texte, 0 Fundstellen.
- Referenzlauf 1030, 1007, 1017, 1045, 1046 gegen `2026-09-23_R12_Gebaeudemodell`: 5/5 PASS
  (1 761 589 Werte), alle byte-gleich — nach beiden Merges gerechnet, `.work/` danach gelöscht.
- Linkprobe (Perl, Übergabe Gebäudesimulation Abschnitt 3): 0 FEHLT unter `aktuell/` und für die
  neuen und geänderten Papiere.

## Merge und Nachzug

**Merge `04bf2ae0`** von `origin` (`4b2ae6ce`, Kühlung KU1 Welle 3): Konflikte nur in `Resource.resx`
und `Resource.en-US.resx` — beide Seiten hatten am Dateiende angehängt; beide Blöcke übernommen,
keine Dopplung, gleiche Schlüsselmengen beider Sprachen, `designer_neu.py` ohne Unterschied,
`Resource.Designer.cs` mit CRLF; Testdatenbank in der Fassung von `origin` (Schemastand 113).
**Merge `169716e8`** von `39c63361` (#449), weil `origin` während des Abschlusses weiterrückte; ohne
Konflikt.

**Nachzug** (`a228185d`): `tww_testkatalog_fiktiv.py` auf der Fassung 113 — 63 Zeilen angelegt
(54 Parameter der Auslegung, zwei Bedarfstage mit sechs Ereignissen, ein DIN-4708-Wert),
2 nachgeführt (die zwei Ausstattungsklassen); zweiter Lauf 0/0. Parameter 72, Bedarfstage 3 mit
9 Ereignissen, DIN-4708-Werte 5; keine Zone, kein Projekt, kein Status `AUSLIEFERUNG`/`IMPORT`; jeder
Schlüssel aus `ZapfParameter` und `ZapfAuslegungParameter` vorhanden. Schemastand unverändert,
`integrity_check` ok, `foreign_key_check` leer, keine `-wal`/`-shm`, 67 751 936 Byte; LFS-Filter
aktiv, Zeiger 133 Byte, SHA-256 `fc13e3a7…`. Danach `TwwKatalogWacheTests` streng (0/0 ab dem
ersten Lauf, Übergangsvermerk entfernt) und `TWW_BEDARFSTAG` mit drei Sätzen in der Theorie von
`KatalogpflegeTests` (`8cc16a62`); Nachtrag in `Referenzlaeufe/LIESMICH.md`, Basis unverändert.

## Gegenprüfungen

Je Gruppe prüfte ein zweiter Agent; nachgebessert wurde in eigenen Commits, bei Widerspruch zum
Papier gilt das Papier.

| Gruppe | Befunde | hoch | mittel | gering |
|---|---|---|---|---|
| 1 Rechenweg der Auslegung | 13 | 0 | 7 | 6 |
| 2 Oberfläche der Auslegung | 8 | 1 | 3 | 4 |
| Summe | 21 | 1 | 10 | 10 |

| Gruppe | Wichtigste Befunde und Nachbesserung |
|---|---|
| 1 | Zeitkonstante nach A1 mit c_w in kJ/(kg·K) (1); Wertepaarkurve je Stufe mit Φ_N(V) = min(Φ_Erzeuger, Φ_Ü(V)), jedes Paar baubar (2); die Summenkontrolle verglich die Woche mit sich selbst, jetzt gegen die Tagesmengen des Fensters (5); eine Speichertemperatur je Gruppe, Mindesttemperatur nach W 551 nur bei Großanlage (6, 7); Nenninhalt über dem Listenende, Füllstand am empfohlenen Volumen, Katalogtag nur bei einheitlicher Bezugsart (9–11); Trennungswache (4); Fassadenfall im Referenzfall (13) |
| 2 | **hoch:** Ein übernommener Punkt steuerte über die Großanlagenerkennung Speichertemperatur, Warnliste und den neuen Punkt — die Überlagerung rechnet ohne Punkt, eine Zonenänderung überholt ihn (1); Sperrgrund des Normtags fehlte in beiden Sprachen, dazu die Wache der Hüllenschlüssel (2); Konstruktor und Banner im Razor nie gelesen (3); Fehlendes steht benannt gesperrt (4, 5); Konstruktor trägt seine Zeilen, prüft den Namen, beschriftet jedes Feld (7, 8) |

## Abweichungen vom Papier

Stehen in N10 (a)–(n) und N11 (a)–(k); der Hauptteil ist mit „(N10)" und „(N11)" berichtigt. Kurz:
Fassade bildet Wochenreihe und Bedarfstag je Gruppe selbst (Ensemble mit Z3); DIN-4708-Profil aus
W_z(N) und den Zapfblöcken des Parametersatzes; DIN 1988-300 und Spitze je Wohnungsstation entfallen
bis zu ihren Daten; eine Speichertemperatur je Gruppe; GLF-Grenze als Parameter; Übertragerpaare und
Werkstoff als Laufangaben; eigenes `SummenlinieModell`; Nenninhaltsliste aus dem Parametersatz;
Stufe Einfach („Schnellauslegung"); der Punkt ist Ergebnis, nicht Eingabe; Sätze des Kerns deutsch.

## Offene Punkte

- **Sichtabnahme unter Windows** (offen): Zapfprofil → „Auslegung…"; Wahl des Bedarfstags
  (Vorgaberegel, gesperrte Quellen mit Grund); Karten (a) Summenlinie mit Wertepaarkurve,
  (b) Perzentil gesperrt, (c) Normvergleich mit DIN-4708-Kennzahl und gesperrter Zeile DIN 1988-300;
  Verfahrensvergleich mit Band, Füllstand, Warnliste; Banner „Spitzen unterschätzt"; Konstruktor
  (Zeilen, Name, Fehleingabe hält OK an); OK übernimmt den Punkt, eine Zonenänderung meldet ihn
  überholt; englische Oberfläche gegenlesen. Prüfliste in Abschnitt 9 der Übergabe.
- **ChartProben-Messlatte Linux:** zehn Auslegungsbilder (N11 (a)) und elf Zapfprofilbilder aus Z1
  beim nächsten Kern-Lauf auf ubuntu aufnehmen (alte Zeilen gleich).
- **Wiki und Logbuch:** Upload „Brauchwasser-Zapfprofil" samt Abschnitt „Auslegung" gebündelt;
  Logbuch-Satz (Statuszeile #451), Versionsnummer beim Anwender.
- **DIN 1988-300:** ΣV̇_A der Entnahmearmaturen ins Datenmodell (Schemaschritt; N10 (c), N11 (f)).
- **Erzeugerart und Werkstoff:** Laufangaben ohne Spalte; speichern oder ableiten (N10 (i), Z4).
- **Katalogpaket:** Auslieferungswerte (Speichertemperatur-Vorgabe, GLF-Grenze, Übertragerpaare,
  Nenninhaltsliste), A100-Referenzprofile nach K1/K8; DIN 4708-1 fehlt in der Ablage.
- **Z3-Folgen:** Spitze je Wohnungsstation (N10 (d)), Konsistenzhinweis mit dem Perzentil
  (N11 (f)), Ecodesign-Zapfprofil (N11 (e)); Schemaschritt T2 — Nummer erst bei der Vergabe nach
  `git fetch` gegen `origin` und alle lokalen Zweige messen (heute überall 113, frei ist 114).
- **Z4-Folgen:** N11 (c), (d), (j), (k).

# E3 — Plattform Wirtschaftlichkeit: Datenseite und Rechenaufruf plattformfrei (Protokoll, 22.09.2026)

Statuszeile #431 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Etappe E3 des Analysepapiers
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
(§ 3.2 Befunde P1–P7, § 5 die acht Schritte); Entscheide A1 (Plattform vor der Ergebnisansicht) und A19 (Whitelist: alle
Wirtschaftlichkeitsseiten in der Reihenfolge des Hüllen-Umzugs), beide 20.09.2026 nach Empfehlung; Muster aus
[`KIF8_Oeffnungswege_Protokoll.md`](../KI/KIF8_Oeffnungswege_Protokoll.md) (Hülle in `EPOS.UI.Daten`, Fenster-Adapter,
Nähte in `IProjektQuelle`, Zweige der Wurzel). Zweig `e3`, Commits `d329c6f3` (Schritt 1), `26a46c8d` (2), `6e983219` (4),
`aefa1eb4` (3), `8236089b` (5 und 6), `efd7989b` (7), `01130231` (8); Merge `2cfee66b`. Anwender 22.09.2026: Wiederaufnahme
der Wirtschaftlichkeit („fahre fort"), Umsetzung des Mockups `Dialog_Formel_Zahlenprobe.html`.

## Befund vor der Welle

Der Rechenaufruf für den Bericht lag mit rund 510 Zeilen Fachlogik in `WindowsFormsApplication1/Allgemein/Bericht/BerichtsDatenSammler.cs`
(P1); die Datenseite der Kosten und der Wirtschaftlichkeit mit rund 6 600 Zeilen Hüllen und Gaben in der Windows-Schale, davon
nur rund 340 Zeilen echte Fensternaht (P2); die Wurzel-Whitelist führte von diesem Feld nur `BhkwWirtschaftlichkeit`,
`Energietraeger` und `BerichteKosten`, `IProjektQuelle.BerichteKostenGaben` lieferte auf iOS `null` (P3); die Tarif-Sprünge aus
BHKW- und PV-Dialog öffneten ein zweites WinForms-Fenster, der BHKW-Sprung mit `MessageBox`, die Dateiwahl im PV-Dialog lief
über `OpenFileDialog` (P4); die Ablauffolge des Verlaufs lag in einer Windows-Hülle, `EPOS.UI.Daten` hatte keinen Ordner
`Wirtschaftlichkeit` (P7). iOS konnte keine Wirtschaftlichkeit rechnen.

## Schritte 1–4 (Teil a)

- **(1) Vier nahtlose Hüllen:** `WirtschaftlichkeitParameterHuelle` → `EPOS.UI.Daten/Wirtschaftlichkeit/`,
  `KostenfaktorKatalogHuelle`, `VorlagenUebernahmeHuelle`, `ErtragBonusGaben` → `EPOS.UI.Daten/Kosten/`; Namensraum und
  `internal` unverändert (`InternalsVisibleTo`). Die vier riefen zwei Hüllen, die bis Teil b in der Schale blieben; dafür
  entstand die Übergangsnaht `Wirtschaftlichkeitswege` (Muster `Katalogwege`, sieben Haken, belegt in `Program.Main`) — mit
  Teil b vollständig aufgelöst und gelöscht.
- **(2) PV-Dateiwahl:** `PhotovoltaikVerguetungHuelle.MarktwerteImportieren` über `Dienste.Datei.DateiOeffnenAsync` (Muster
  `SpotpreisImportHuelle`); `Gaben(idStamm)` braucht keinen Fensterbesitzer mehr, `WirtschaftlichkeitSeiteGaben` hat keine
  Windows-Zeile mehr; der Dialogparameter wurde asynchron, drei Fälle in `PhotovoltaikVerguetungDialogTests` auf
  `WaitForAssertion` umgestellt.
- **(4, vor 3) BerichtsDatenSammler:** samt `EnergieMengen` nach `EPOS.Kern/Allgemein/Bericht/` (dort liegen `BerichtsDaten`,
  `BerichtsKonfiguration`, `KostenEmissionRechner`); die Naht `EnergieMengen.BaueBrennstoffmengen` ist nachgemessen
  plattformfrei und zog mit — keine benannte Naht, kein zweiter Aufbau, der Windows-Berichtsweg ruft dieselbe Klasse. Die zwei
  `git grep`-Wächter aus `EPOS.Kern/CLAUDE.md` bleiben leer.
- **(3) Die zwei Seiten:** `KostenSeiteGaben` → `EPOS.UI.Daten/Kosten/`, `WirtschaftlichkeitSeiteGaben` →
  `EPOS.UI.Daten/Wirtschaftlichkeit/`; die drei toten Windows-Zeilen entfallen. Die Zeilenliste der Wirtschaftlichkeitsseite
  entsteht plattformfrei (P7); der Rechenlauf läuft über `Kulturweitergabe.Starten` (Wächter `ParallelitaetWache`).
- Zeugen: `EPOS.Kern.Tests/WirtschaftlichkeitHuellenPlattformTests` (zwölf Fälle: jede verschobene Hülle baut ihren Satz ohne
  Windows-Dienst, je Naht beide Seiten der Regel) und `BerichtsDatenSammlerPlattformTests` (drei Fälle).

## Schritte 5–8 (Teil b)

- **(5 und 6) Hüllen:** `KostenKomponenteHuelle` → `EPOS.UI.Daten/Kosten/` mit Adapter `KostenKomponenteFenster`;
  `GesetzeskatalogHuelle` → `EPOS.UI.Daten/Wirtschaftlichkeit/` mit Adapter `GesetzeskatalogFenster` (Menü Administration);
  `TarifstrukturHuelle`, `PhotovoltaikVerguetungHuelle`, `BhkwWirtschaftlichkeitHuelle`, `KapitalwertVerlaufHuelle` →
  `EPOS.UI.Daten/Wirtschaftlichkeit/`. Ihre Fensterhälften hatten nach dem Fall der Naht keinen Aufrufer mehr und sind ersatzlos
  gefallen (Aufräumregel) — damit ist auch die `MessageBox` aus P4 weg. Der Verlauf läuft plattformfrei (sammeln, rechnen,
  zeichnen über den Renderer des Kerns). Der PV-Sprung aus „Ertrag/Bonus" ist die siebte Überlagerung des Kostendialogs, der
  Knopf folgt „kein Delegat, kein Knopf".
- **(7) Tarif-Sprünge:** BHKW-Tarif, Strombezug und PV-Tarif öffnen die Tarifstruktur als Überlagerung (bis dahin verwarf der
  Wirt den gemeldeten Sprung, der Knopf tat nichts); `WirtschaftlichkeitSeite` mit den Schlüsseln `TarifBhkw`/`TarifPv`,
  `KostenKomponenteDialog` mit einer achten Überlagerung; Titel aus `TarifstrukturTexte`, kein neuer Ressourcenschlüssel. Der
  OK-Weg des PV-Sprungs aus #405 (Q12) bleibt.
- **(8) iOS:** `UebersichtSeiteGaben`, `BerichteKostenHuelle`, `BerichtSeiteGaben` → `EPOS.UI.Daten/Bericht/`; Registry →
  `Dienste.Einstellungen` (der Merkwert der Vergleichsgruppe wechselt die Ablage — beim ersten Start danach steht einmal der
  erste Stamm), `SpecialFolder.MyDocuments` → `Dienste.Pfade.Dokumente`, Variantendialog und Umbenennen-Nachlauf als benannte
  Nähte aus `StartseiteHuelle`, drei `Task.Run` → `Kulturweitergabe`. `IosProjektQuelle.BerichteKostenGaben` liefert alle vier
  Seiten (Übersicht, Kosten, Wirtschaftlichkeit, Bericht) aus einer je Sitzung gehaltenen Hülle; die Whitelist der Wurzel wächst
  von 18 auf 21 Schlüssel (`KOSTENVERWALTUNG`, `NUTZUNGSDAUER_VERWALTUNG`, `GESETZESKATALOG`, je Zweig mit Ablehnungstext);
  Parameter, PV-Vergütung, Tarifstruktur und Verlauf sind Überlagerungen der Wirtschaftlichkeitsseite und über
  `BERICHTE_KOSTEN` erreichbar. Benannt abgelehnt bleibt auf iOS nur der Knopf „Variante anlegen" der Übersicht (Weg dort: die
  Ansicht `PROJEKT_ALS_VARIANTE`). Übersetzungsprobe der geänderten iOS-Datei gegen `EPOS.UI.Daten` fehlerfrei.

## Abweichungen vom Plan, mit Grund

- Schritt 4 vor Schritt 3: `WirtschaftlichkeitSeiteGaben` benutzt die geschachtelten Typen des Sammlers und übersetzt in
  `EPOS.UI.Daten` erst, wenn er plattformfrei ist. Zielort `EPOS.Kern/Allgemein/Bericht/` statt `Controller/`.
- Schritte 5 und 6 in einem Commit: Schritt 5 brauchte den Haken, den Schritt 6 löscht; getrennt gäbe es einen nicht bauenden
  Zwischenstand.
- Statt vier neuer Fenster-Adapter (PV, Tarif, BHKW, Verlauf) keine: ohne Aufrufer wären sie toter Code.
- Drei `CLAUDE.md`-Zeilen berichtigt (Regelwerk, keine Papiere): `EPOS.Kern/CLAUDE.md` und `WindowsFormsApplication1/CLAUDE.md`
  führten `BerichtsDatenSammler` in der Schale, `EPOS.iOS/CLAUDE.md` `BerichteKostenGaben` als Vorgabe `null` bis iU11.
- Referenzlauf über alle dreizehn Basisprojekte statt der fünf CI-Projekte: mit fünf meldet der Vergleich „GESAMT: FAIL",
  weil die übrigen acht im Vergleichslauf fehlen.
- Zwei Wächter lasen `EPOS.UI.Daten` und `*Fenster.cs` nicht (`ParametersatzTests`, `KiChatOeffnerTests`) — nachgezogen;
  die Lücke bestand seit #428.

## Zahlen und Abnahme

- Bau: Kern-Filter 0 Fehler; Windows-Schale (Debug x64) 0 Fehler nach jedem Schritt und auf dem Merge-Stand.
- Tests: Teil a voller Lauf 10 383 grün, Teil b voller Lauf 10 408 grün (je 1 übersprungen, Bestand). Designer 7 492 Einträge,
  unverändert. Referenzlauf gegen `2026-09-19_R10_BhkwWirkungsgrad`: 13/13 Projekte, 3 882 737 Werte innerhalb der Toleranz,
  nach Teil a und nach Teil b.
- Gate #431 auf `2cfee66b`: siehe Statuszeile #431.
- Aus der Schale gewandert: rund 3 000 Zeilen Datenseite nach `EPOS.UI.Daten` (Teil a) plus die Hüllen aus Teil b, 612 Zeilen
  nach `EPOS.Kern`. `Views/Kosten/ErzeugerKostenwege.cs` bleibt zu Recht in der Schale (reicht den Fensterbesitzer an die zwei
  Adapter).

## Abnahme am Gerät (A‑E3‑1, Windows)

1. Anlagendialog → Reiter „Ertrag/Bonus" → „PV-Vergütungsdialog öffnen…": Überlagerung statt Zweitfenster; darin „Tarif…" führt
   in die Tarifstruktur (Sicht PV) als weitere Überlagerung.
2. Wirtschaftlichkeitsseite: „BHKW-Tarif…", „Stromtarif…" und „Tarif…" (Photovoltaik) öffnen die Tarifstruktur (vorher folgenlos).
3. Berichte & Kosten → Übersicht: die zuletzt bearbeitete Vergleichsgruppe wird beim ersten Start einmal nicht erinnert
   (Ablagewechsel), danach wieder; „Variante anlegen", Umbenennen und Löschen unverändert.
4. Menü Administration → Kostenverwaltung, Nutzungsdauern, Gesetzeskatalog und die Anlagendialoge „Investitionskosten…" /
   „Betriebskosten…" gehen weiter als eigenes Fenster auf; Maße und Titel unverändert. Ein Klimaimport im PV-Dialog
   („Marktwerte importieren") über den neuen Dateidienst einmal durchlaufen.

## Offen

- `EPOS.iOS` ist berührt (`IosProjektQuelle.BerichteKostenGaben`): ein `ios.yml`-Lauf ist mit Schritt 8 begründet und läuft
  nur nach Rückfrage beim Anwender.
- Der Merkwert der Vergleichsgruppe steht jetzt in `Dienste.Einstellungen`; der alte Registrywert bleibt ungelesen liegen.
- U39 / Konzept § 2.13 (3): Zeilenliste und Nutzungsdauer-Hinweis der Seite in einen Kern-Controller — Fachergänzung, gehört zu E5.
- Nächste Etappen nach dem Analysepapier: E4 Erlösrubrik und Steuerzeilen (Q15 offen: Leistungsanteil projektweit?), E5
  Ergebnisansicht und V‑A.

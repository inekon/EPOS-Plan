# Analyse 04 — Oberfläche, Hüllen und Plattformfreiheit

*Konzept `Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`
(2 427 Zeilen, Kopf „Stand 02.09.2026") gegen den Codestand vom 19.09.2026, Zweig
`ios_migration_september`. Ohne Wiederholung der Befunde aus `03_Mockup_Code.md` (Kennung `03/#n`,
`03/§8`) und `04_Einheitlichkeit.md` (`04/B01`…`04/B31`); WinForms-Begriffe siehe zusätzlich
`02_Konsistenz_Papiere.md` Abschnitt (d), Kennungen `02/d-1`…`02/d-22`.*

## 0 Ergebnis in fünf Sätzen

Der Dialograum des Konzepts ist als Razor vollständig gebaut — 19 von 19 im Konzept benannten
Dialogen und zwei Seiten existieren, mit 4 126 bunit-Marken im Rücken —, offen ist allein die
Ergebnisansicht § 2.13: Umschalter, Bandbreite, Empfehlungskarten, Gliederung, Brücke, ValERI-Blöcke
und der Dreiszenarien-Verlauf fehlen auf `WirtschaftlichkeitSeite.razor` durchweg. Die Trennung
Hülle/Schale ist erst zu einem Drittel vollzogen: 3 853 Zeilen plattformfreie Datenseite stehen in
`EPOS.UI.Daten/Kosten/`, aber 6 631 Zeilen Datenseite der Wirtschaftlichkeit, der Kostenverwaltung
und der beiden Seiten liegen weiterhin in `WindowsFormsApplication1/Views/` — obwohl **keine einzige
dieser Hüllen SQL absetzt**, alle über Kern-Controller gehen und die WinForms-Naht je Hülle zwischen
0 und 86 Zeilen misst. Die wichtigste Einzelmessung: `WirtschaftlichkeitSeiteGaben.cs` (1 015 Z.)
hat genau **drei** Windows-Zeilen (35, 86, 921), und die dritte existiert nur, weil
`PhotovoltaikVerguetungHuelle` den Marktwert-Import noch über `OpenFileDialog` statt über
`Dienste.Datei` führt — der Beweis, dass der Umzug ein Handgriff und keine Etappe ist.
Auf iOS ist von der ganzen Wirtschaftlichkeit heute **ein** Dialog erreichbar (BHKW,
`AppWurzel.razor:1432`), und selbst der bereits whitelistete Reiter „Berichte & Kosten" bleibt leer,
weil `IProjektQuelle.BerichteKostenGaben` in der iOS-Schale nicht belegt ist und `null` liefert.
Vom Umsetzungsstand U1–U40 sind 20 Zeilen erledigt und 19 zu Recht offen (03/§8); von diesen 19
sind **drei** reine Oberflächenstücke ohne Kern- oder Schemabedarf (U2, U10, U11) und damit die
billigsten Schritte der ganzen Liste.

---

## 1 Dialogmatrix

**Spalten.** *Hülle*: `D` = plattformfrei in `EPOS.UI.Daten`, `W` = nur in
`WindowsFormsApplication1/Views/`, `—` = keine eigene Hülle (eingebetteter Dialog, der Wirt füllt
ihn). *Datenweg*: in allen geprüften Hüllen ausschließlich Kern-Controller — **null** direkte
SQL-Anweisungen (gemessen über alle 14 Hüllen der Ordner `Views/Kosten` und
`Views/Wirtschaftlichkeit`). *iOS*: Maßstab ist die Whitelist `EPOS.UI/Seiten/AppWurzel.razor:1431–1445`
(13 Seitenschlüssel, alles andere `return false` in `:1445`).

| # | Konzeptstelle | Razor-Komponente (Z.) | Hülle (Z.) | Datenweg (Kern-Controller) | iOS | Menüpunkt | bunit (Klasse / Fälle) | Stand gegen das Konzept |
|---|---|---|---|---|---|---|---|---|
| 1 | § 2.2 BHKW | `EPOS.UI/Dialoge/Wirtschaftlichkeit/BhkwWirtschaftlichkeitDialog.razor` (1 293) | `…/Views/Wirtschaftlichkeit/BhkwWirtschaftlichkeitHuelle.cs` (235) **W** | `KwkgAnlagenCtrl`, `WirtschaftlichkeitCtrl` | **ja** — `Seitenschluessel.BhkwWirtschaftlichkeit`, `AppWurzel.razor:1432`; Gaben in `EPOS.iOS/Dienste/IosProjektQuelle.cs:201–242` (lesend **und** schreibend, `:249–264`) | keiner — Knopf „BHKW…" `WirtschaftlichkeitSeite.razor:376` | `BhkwWirtschaftlichkeitDialogTests` / 69 | umgesetzt; **acht** statt sechs Gruppen (`:76`–`:419`), siehe `02/d-1`. Der Sprungknopf „Tarif…" öffnet ein **zweites WinForms-Fenster** (`BhkwWirtschaftlichkeitHuelle.cs:207–225`, mit `MessageBox` `:223`) — auf iOS tot |
| 2 | § 2.3 PV-Vergütung | `…/Wirtschaftlichkeit/PhotovoltaikVerguetungDialog.razor` (695) | `…/Views/Wirtschaftlichkeit/PhotovoltaikVerguetungHuelle.cs` (313) **W** | `ProjektPhotovoltaikCtrl`, `PhotovoltaikCtrl`, `WirtschaftlichkeitCtrl`, `KostenSummenCtrl`, `VariantenCtrl`, `StartseiteCtrl` | **nein** — kein Seitenschlüssel; 0 Treffer „Photovoltaik" als Dialogweg in `EPOS.iOS`; das Konzept sagt es selbst (Z. 1339) | keiner — Knopf „Photovoltaik…" `WirtschaftlichkeitSeite.razor:371` | `PhotovoltaikVerguetungDialogTests` / 31 | umgesetzt; Gruppenzahl/-namen weichen ab (`02/d-6`…`02/d-8`). **Marktwert-Import über `OpenFileDialog`** (`:273`) statt `Dienste.Datei` — die einzige echte Plattformbindung des Dialogs |
| 3 | § 2.4 Parameter | `…/Wirtschaftlichkeit/WirtschaftlichkeitParameterDialog.razor` (663) | `…/Views/Wirtschaftlichkeit/WirtschaftlichkeitParameterHuelle.cs` (148) **W** — **ohne eine einzige WinForms-Anweisung** (kein `using System.Windows.Forms`, kein `Size`, kein `ShowDialog`) | `WirtschaftlichkeitCtrl`, `EmissionsBilanzRechner`, `GesetzKatalog` | nein (nur über die Seite erreichbar) | keiner — Knopf „Parameter…" `WirtschaftlichkeitSeite.razor:161` | `WirtschaftlichkeitParameterDialogTests` / 32 | umgesetzt; vier Gruppen wie im Konzept, Mockup zeigt anderen Zuschnitt (`02/d-10`). **Umzugskandidat Nr. 1** — reines Verschieben der Datei |
| 4 | § 2.5 Trägerkarte / Energieträgerverwaltung | `…/Kosten/EnergietraegerDialog.razor` (1 142) + `EnergietraegerEinstellungen.razor` (697) | `EPOS.UI.Daten/Kosten/EnergietraegerHuelle.cs` (2 773) **D** + Adapter `…/Views/Kosten/EnergietraegerFenster.cs` (67) | `EnergietraegerKatalogCtrl`, `EnergietraegerPreisCtrl`, `StrompreisZerlegungCtrl`, `BrennstoffBestandteilCtrl` | **nein** — `ENERGIETRAEGER_VERWALTUNG` (`Seitenschluessel.cs:344`) fehlt in der Whitelist; der whitelistete Schlüssel `ENERGIETRAEGER_VARIANTE` (`:34`) meint den **Variantendialog**. Das Anlegen eines Trägers wird auf iOS **benannt abgelehnt** (`IosProjektQuelle.cs:164–184`, Rückgabe `""` `:183`) | Administration → Kostenverwaltung → Energieträger (`Menuetabelle.cs:266`) | `EnergietraegerDialogTests` / 81 | umgesetzt; **das Muster für alle übrigen Hüllen** |
| 5 | § 2.5 Strompreis Details | `…/Kosten/StrompreisDetails.razor` (248), eingebettet | über `EnergietraegerHuelle` **D** | `StrompreisZerlegungCtrl` | wie #4 | — | `PreisbloeckeTests` / 32 gesamt (13 Renderaufrufe) | umgesetzt bis auf den Sammelknopf (U11) |
| 6 | § 2.5 Brennstoff-Bestandteile | `…/Kosten/BrennstoffBestandteile.razor` (280), eingebettet | über `EnergietraegerHuelle` **D** | `BrennstoffBestandteilCtrl` | wie #4 | — | `PreisbloeckeTests` (18 Renderaufrufe) | umgesetzt |
| 7 | § 2.5 Emissionsanzeige (D-1/E-1) | `…/Seiten/Berichte/KostenSeite.razor:192` (`title=@…EmissionKurztext`) | `…/Views/BerichteKosten/KostenSeiteGaben.cs` (1 054) **W**, Modus `:658` | `EmissionenCtrl.ModusFuerRechenlauf` | nein (siehe #17) | — | `KostenSeiteTests` / 38 | umgesetzt (eine Spalte, Kopf und Kurztext nach `Emission_Berechnungsmodus`) |
| 8 | Administration: Emissionskatalog | `…/Kosten/EmissionskatalogDialog.razor` (980), eingebettet `EnergietraegerDialog.razor:286` | `EPOS.UI.Daten/Kosten/EmissionskatalogHuelle.cs` (404) **D** | `EmissionskatalogCtrl` | nein (Wirt nicht erreichbar, #4) | kein eigener | `EmissionskatalogDialogTests` / 37 | umgesetzt; kein Spaltenfilter (`04/B20`) |
| 9 | § 2.8 Kostenverwaltung | `…/Kosten/KostenKomponenteDialog.razor` (1 273) | `…/Views/Kosten/KostenKomponenteHuelle.cs` (1 336) **W** | `BetriebskostenCtrl`, `KostenProjektPositionenCtrl`, `KostenSummenCtrl`, `KostenVorlagenCtrl`, `NutzungsdauerCtrl`, `ProjektEnergietraegerCtrl`, `TechnikPlanwertCtrl` | **nein** — `KOSTENVERWALTUNG` (`Seitenschluessel.cs:341`) nicht in der Whitelist | Administration → Kostenverwaltung → Kostenvorlagen (`Menuetabelle.cs:265`) | `KostenKomponenteDialogTests` / 93 | Entwurf B im Kern umgesetzt (U28–U31, U33–U35, U40 erledigt); offen der Rest von U8 (→ U39) |
| 10 | § 2.8 Übernahme aus Vorlage | `…/Kosten/VorlagenUebernahmeDialog.razor` (573), eingebettet `:388` | `…/Views/Kosten/VorlagenUebernahmeHuelle.cs` (366) **W**, **ohne WinForms-Anweisung** | `KostenVorlagenCtrl`, `KostenVorlagenUebernahmeCtrl`, `KostenProjektPositionenCtrl`, `KostenSummenCtrl`, `ProjektEnergietraegerCtrl` | nein | — | `VorlagenUebernahmeDialogTests` / 29 | umgesetzt; Fließtext des Mockups veraltet (`02/d-16`) |
| 11 | § 2.8 Positionskatalog (Kostenfaktoren) | `…/Kosten/KostenfaktorKatalogDialog.razor` (244), eingebettet `:399` | `…/Views/Kosten/KostenfaktorKatalogHuelle.cs` (96) **W**, **ohne WinForms-Anweisung** | `KostenfaktorCtrl` | nein | kein eigener | `KostenfaktorKatalogDialogTests` / 17 | umgesetzt; kein Abbrechen (`04/B02`), kein Filter (`04/B20`) |
| 12 | Nutzungsdauern (AfA) | `…/Kosten/NutzungsdauerDialog.razor` (473) | `EPOS.UI.Daten/Kosten/NutzungsdauerHuelle.cs` (188) **D** + Adapter `…/Views/Kosten/NutzungsdauerFenster.cs` (58) | `NutzungsdauerCtrl` | **nein trotz plattformfreier Hülle** — `NUTZUNGSDAUER_VERWALTUNG` (`Seitenschluessel.cs:358`) fehlt in der Whitelist | Administration → Kostenverwaltung → Nutzungsdauern (`Menuetabelle.cs:272`) | `NutzungsdauerDialogTests` / 13 | umgesetzt; **der Fall, der zeigt, dass plattformfrei ≠ erreichbar** |
| 13 | Gesetzeskatalog | `…/Wirtschaftlichkeit/GesetzeskatalogDialog.razor` (514) + `GesetzeskatalogZeileDialog.razor` (318) | `…/Views/Admin/GesetzeskatalogHuelle.cs` (154) **W** | `GesetzKatalog` | **geteilt**: der Katalog als Nachschlagewerk ist auf iOS erreichbar (`IosProjektQuelle.cs:209, 232` im BHKW-Dialog), die **Verwaltungsmaske nicht** (`GESETZESKATALOG`, `Seitenschluessel.cs:364`) | Administration → Gesetzesparameter (`Menuetabelle.cs:280`) | `GesetzeskatalogDialogTests` / 29 (beide Dialoge in einer Datei) | umgesetzt; Zeilendialog ohne `InfoKnopf` und ohne Kopf (`04`, Merkmalsmatrix Zeile 109) |
| 14 | Tarifstruktur | `…/Wirtschaftlichkeit/TarifstrukturDialog.razor` (541) | `…/Views/Wirtschaftlichkeit/TarifstrukturHuelle.cs` (108) **W** | `WirtschaftlichkeitCtrl` | nein | **keiner** (`04/B29`); drei Zugänge: Seite als Überlagerung (`WirtschaftlichkeitSeiteGaben.cs:928`, `Gaben`), BHKW-Sprung (`BhkwWirtschaftlichkeitHuelle.cs:219`, **zweites Fenster**), PV-Sprung (`PhotovoltaikVerguetungHuelle.cs:59`, **zweites Fenster**) | `TarifstrukturDialogTests` / 25 | umgesetzt; die zwei Sprungwege sind die einzigen verbliebenen Zweitfenster der Wirtschaftlichkeit |
| 15 | § 2.13 (5) Verlauf | `…/Wirtschaftlichkeit/KapitalwertVerlaufDialog.razor` (274) + `KapitalwertVerlaufBilder.cs` (25) | `…/Views/Wirtschaftlichkeit/KapitalwertVerlaufHuelle.cs` (267) **W** | `WirtschaftlichkeitCtrl.BerechneVerlauf` | nein | keiner — Knopf „Verlauf…" `WirtschaftlichkeitSeite.razor:383` | `KapitalwertVerlaufDialogTests` / 17 | **abweichend**: ein Szenario je Lauf, zwei Bilder, eigener Dialog — § 2.13 (5) und § 2.7 verlangen Wegfall des Knopfes und drei Szenarien inline (U2, U3) |
| 16 | § 2.13 / § 2.10 Ergebnisansicht | `…/Seiten/Berichte/WirtschaftlichkeitSeite.razor` (1 036) + `WirtschaftlichkeitDaten.cs` | `…/Views/Wirtschaftlichkeit/WirtschaftlichkeitSeiteGaben.cs` (1 015) **W** | `WirtschaftlichkeitCtrl`, `SpeicherAnzeigeCtrl` | **nein** — Schlüssel `BERICHTE_KOSTEN` steht zwar in der Whitelist (`AppWurzel.razor:1441`), aber `IProjektQuelle.BerichteKostenGaben` ist in der iOS-Schale nicht belegt und liefert `null` (`EPOS.UI/Dienste/IProjektQuelle.cs:272`) | Projekte → Varianten und Bericht… (`Menuetabelle.cs:165`) | `WirtschaftlichkeitSeiteTests` / 58 + `WirtschaftlichkeitSichtTests` / 12 | **teils** — siehe § 6 |
| 17 | § 2.14 Kostenseite | `…/Seiten/Berichte/KostenSeite.razor` + `KostenDaten.cs` | `…/Views/BerichteKosten/KostenSeiteGaben.cs` (1 054) **W** | `KostenVorlagenCtrl.IstErfassungsgruppe` (`:504`), `KostenSummenCtrl`, `ProjektEnergietraegerCtrl`, `WirtschaftlichkeitCtrl`, `EmissionenCtrl` | nein (wie #16) | (Teil der Ansicht Berichte & Kosten) | `KostenSeiteTests` / 38 (+ `BerichteKostenSeiteTests` / 17) | umgesetzt (Erfassungsgruppe im Kern, gelbe Zeile getrennt) |
| 18 | § 2.9 Vergleichsprojekt | Optionsgruppe je Zeile der Vergleichsgruppen-Liste, `WirtschaftlichkeitSeite.razor:120` | wie #16 | `WirtschaftlichkeitCtrl.Berechne(…, referenz)`, `Referenzwahl` | nein | — | `WirtschaftlichkeitSichtTests` (12) | umgesetzt |
| 19 | § 2.15 Vergleichssicht | Optionsgruppe + zwei Klapplisten + Tauschknopf + Erklärzeile, `WirtschaftlichkeitSeite.razor:211–234` | wie #16 | `Vergleichsauswahl.Sicht` | nein | — | `WirtschaftlichkeitSichtTests` (12) | umgesetzt (U37) |
| 20 | § 2.16 Vergütung je Variante | `…/Kosten/ErtragBonus.razor` (292): Optionsgruppe `:78`, Klappliste „Projekt:" `:101/:230`; `PhotovoltaikVerguetungDialog.razor`: Herkunftszeile `:63–65`, Knopf „eigene Werte" `:67–69` | `…/Views/Kosten/ErtragBonusGaben.cs` (319) **W**, **ohne WinForms-Anweisung** | `KostenVorlagenUebernahmeCtrl`, `VariantenCtrl`, `StartseiteCtrl`, `ProjektPhotovoltaikCtrl.LiesAufgeloest` | nein | — (Reiter im Kostendialog) | `ErtragBonusTests` / 17 + `PhotovoltaikVerguetungDialogTests` / 31 | umgesetzt (U38); Ressourcentafel des Mockups nennt noch „Stammprojekt:" (`02/d-17`) |
| 21 | § 2.8 / § 2.11.5 Worst/Best (±) | `…/Kosten/CaseEingabeDialog.razor` (331); Auslöser ±-Knopf `VorlagenZeile.razor:155–159` | — (Gaben aus `KostenKomponenteDialog.razor:1073`) | `KostenProjektPositionenCtrl` über die Kostenhülle | nein | — | `CaseEingabeDialogTests` / 23 (+ `VorlagenZeileTests` / 28) | umgesetzt, aber **nur an Kostenpositionen**; § 2.11.5 Z. 728 verlangt dasselbe Muster an Trägerpreisen, Erlösfeldern und der Rahmen-Gruppe — dort gibt es keinen einzigen ±-Knopf (gemessen: `CaseEingabeDialog` hat genau **einen** Wirt) |
| 22 | § 2.5 Nebenkarten | `KostenprofilDialog.razor` (510) · `LeistungspreisReiheDialog.razor` (288) · `SpotpreisImportDialog.razor` (314) — alle eingebettet in `EnergietraegerDialog.razor:274/278/282` | `EPOS.UI.Daten/Kosten/KostenprofilHuelle.cs` (201) · `LeistungspreisReiheHuelle.cs` (164) · `SpotpreisImportHuelle.cs` (123) — alle **D** | `KostenprofilCtrl`, `PreisreiheCtrl`, `SpotpreisImportCtrl` | nein (Wirt, #4) | — | 24 / 19 / 19 | umgesetzt. **`SpotpreisImportHuelle:57` ist der Beleg, dass Dateiwahl plattformfrei geht** (`Dienste.Datei.DateiOeffnenAsync`) |

**Zwei Querbefunde aus der Matrix.**

1. **Der Datenweg ist nirgends das Problem.** Über alle 14 geprüften Hüllen: 0 Treffer für `SELECT`,
   `INSERT`, `UPDATE`, `DataRepository` oder `SQLite`. Jede Hülle ruft Kern-Controller. Der Umzug
   nach `EPOS.UI.Daten` ist deshalb kein Umbau der Datenseite, sondern das Herauslösen der
   Fensternaht.
2. **Vier Konzeptdialoge haben gar keine eigene Hülle** (`EmissionskatalogDialog`,
   `EnergietraegerVarianteDialog`, `VorlagenPositionDialog`, `CaseEingabeDialog`) — sie sind
   Überlagerungen und erben die Plattformfreiheit ihres Wirts. Für sie ist nichts zu tun; sie folgen
   ihrem Wirt automatisch.

---

## 2 Umsetzungsstand U1–U40

`03/§8` hat den Stand gemessen: **20 Zeilen durchgestrichen** (U8, U16–U21, U23, U24, U26,
U28–U31, U33–U38), davon 19 vollständig belegt und U18 als Sonderfall; **19 Zeilen zu Recht offen**
(U1–U7, U9–U15, U22, U25, U27, U32, U39); **U40 ohne Tafelzeile**, nur Inline-Marker (`03/#28`).
Hier nur die Ergänzung um Größe, Plattformfolge und Abhängigkeit.

**Größenmaß.** S = ein Ort, Oberfläche und Ressourcen, kein Kern. M = ein Dialog plus eine
Kern-Zuarbeit. L = mehrere Orte, dazu Schema oder Rechenweg.

### 2.1 Die 19 offenen Zeilen

| U | Stand laut `03/§8` | Was genau fehlt (Kurzform) | Größe | Plattformfolge | Abhängigkeit |
|---|---|---|---|---|---|
| U1 | offen, bestätigt (0 Treffer „Abwärmeabfuhr") | Kennzeichen + Stromkennzahl σ je Anlage, Fallunterscheidung in der Mengenbildung | **L** | plattformfrei (Kern + BHKW-Dialog, der auf iOS steht) | **Schema** (die Tafel nennt Schritt 92 — vergeben; frei ist 95, `SchemaStand.Zielversion = 94`) · **Kern** (modulscharfe Nutzwärme fehlt im Ergebnismodell) · **Entscheid** (je Modul führen oder aufteilen) |
| U2 | offen | Umschalter „Kennzahlen / ValERI-Bewertung" im Kopf; Knopf „Verlauf…" aus der Fußleiste | **M** | **plattformfrei** — nur `WirtschaftlichkeitSeite.razor` + Ressourcen | Entscheid liegt vor (K-8 = V-1, Konzept Z. 469–472); inhaltlich sinnvoll erst mit den fünf Blöcken (§ 2.11.3) |
| U3 | offen | Verlauf mit drei Szenarien; Reihen nach Variante (Farbe) **und** Szenario (Strichart); plattformfrei nach `EPOS.UI.Daten` | **L** | **plattformfrei gefordert** — heute entsteht der Verlauf allein in `KapitalwertVerlaufHuelle` (W); in `EPOS.UI.Daten` gibt es keinen Ordner `Wirtschaftlichkeit` | **Kern** (`BerechneVerlauf` nimmt einen Szenario-String, `WirtschaftlichkeitVerlauf` trägt einen; `VerlaufsReihen` vergibt Farben nach Index) · **Entscheid** (Bildmaß/Legendenplatz, Renderergrenze zwei Legendenzeilen bei 1240 × 620) |
| U4 | offen | Bandbreite der drei Szenarien nebeneinander | **M** | plattformfrei | **Kern** — dieselbe Dreierreihe wie U3; die Seite hält genau ein Szenario (`WirtschaftlichkeitDaten.cs:164 SzenarioId`) |
| U5 | offen | Empfehlungskarte je Version statt einer Zeile | **M** | plattformfrei | **Kern** (`WirtschaftlichkeitEmpfehlung` je Version); der Stand hat heute nur `Empfehlungszeile` (`WirtschaftlichkeitDaten.cs:139`) |
| U6 | offen | Anlagenbezug je Katalogzeile; vermiedene Stromkosten je Anlage | **L** | plattformfrei (Kern) | **Kern** (`WirtschaftlichkeitZeilen`-Zeile ohne Anlagenfeld; `StromMatrix` für Eigenverbrauch je Anlage) — Voraussetzung für § 2.13 (4) |
| U7 | offen | § 53/53a und § 54 als getrennte Rückgabegrößen | **M** | plattformfrei (Kern) | **Kern** (`SteuerGutschriftRechner` liefert eine Summe) — betrifft § 2.6 A4/A5, dort schon als Abweichung vermerkt (Z. 372–374) |
| U9 | offen | Quellenangabe zur Degradation | **S** | Dialog liegt in der Windows-Hülle → plattformfrei erst nach dem Umzug von `PhotovoltaikVerguetungHuelle` | **Schema** (Quellenfeld) **oder** Entscheid „nur Freitext" |
| U10 | offen (`WIRT_SZEN_HINWEIS` existiert repoweit nicht — nachgemessen: 0 Treffer in `.cs`, `.razor`, `.resx`) | Hinweistext „Was ein Szenario variiert" als Ressource, beide Sprachen | **S** | **plattformfrei** — eine Zeile unter der Annahmentafel | **Entscheid** zum Wortlaut: Konzept Z. 809–817 und Mockup Z. 4095–4101 haben verschiedene Schlusssätze (`02/d-18`). Sonst nichts |
| U11 | offen (Treffer nur als Modellkommentar) | Sammelknopf „Vorschlagswerte übernehmen" am Kopf von „Strompreis Details" | **S** | **plattformfrei** — die Hülle ist bereits `D` (`EnergietraegerHuelle`) | keine |
| U12 | offen | Excel-Formelmappe Stufen 0–3 | **L** | Bericht (plattformfrei über den Generator) | **Kern** (`ExcelBerichtGenerator` schreibt keine Formel) |
| U13 | offen | Spaltengruppe je Szenario im Tabellenbericht, zweites Bild im Wortbericht, Excel-Blatt „Verlauf" | **M** | Bericht | **U3/U4** (ohne Dreierreihe keine Spaltengruppe) |
| U14 | offen (ohne eigene Repo-Definition auffindbar, `03/§8`) | Referenzspalte an beiden Orten der Schemabeschreibung; Randfälle | **S** | Papier | **Doku** — reine Beschreibungslücke zu § 2.9; die Spalte selbst ist gebaut (Schritt 92) |
| U15 | offen (dito) | Vollständige Szenarioabdeckung: Trägerpreise, Erlössätze, Mengenfaktor je Szenario | **L** | plattformfrei + Schema | **Schema** (8 Rahmenspalten, Trägerpreis-Paare, Mengenfaktor) · **Kern** · **Dialoge** (das ±-Muster an drei neuen Orten, § 7) · Konzept § 2.11.5 Z. 706–713 |
| U22 | offen (0 Treffer „Sätze und Herkunft"; acht Klapplisten unverändert) | Überlagerung „Sätze und Herkunft" im BHKW-Dialog | **L** | plattformfrei (BHKW steht auf iOS) | **Entscheid zuerst** — Konzept Z. 178–187 („Vorschlag am Feld, kein Sammelknopf") widerspricht dem Mockup („ein Knopf übernimmt alles"), `02/d-2`. Erst danach Kern (Herkunftsgrößen je Wahl) |
| U25 | offen (`BHW_A_DECKEL_STAFFEL`, `BHW_W_DECKELANTEIL` fehlen) | Staffelzeile unter dem Jahresdeckel; Warnband Deckelanteil | **M** | plattformfrei | **Kern** (`KwkgKontingentRechner`); die Katalogwerte liegen bereits (`GesetzKatalog.cs:1109–1116`) — es fehlen Ressourcen und Anzeige |
| U27 | offen (alle acht `PVV_V_*`/`PVV_HERL_MARKTWERT` fehlen) | Aufgeschlüsselte PV-Vorschau (7 Zeilen) + Marktwert-Herleitung | **M** | Dialog in der Windows-Hülle → erst nach deren Umzug plattformfrei | **Kern** (`PvErloesRechner` Teilgrößen) · 8 Ressourcen |
| U32 | offen (`EnergietraegerPreisCtrl.cs:114–140` legt weiter `ID_Umrechnung = -1` ab) | Eigene Spalte für den Kartenzustand „Preisbasis" | **M** | plattformfrei (Hülle ist `D`) | **Schema** (nächster freier Schritt 95) |
| U39 | offen (`Zeitraumzeile()` liegt nur in der Windows-Schale — nachgemessen: `WirtschaftlichkeitSeiteGaben.cs:404`, einziger Schreiber von `WirtschaftlichkeitStand.Zeitraumzeile`) | Entkopplung Ersatz/Restwert; geräteeigene Dauerspalten; Speicherflotte; **Hinweiszeile plattformfrei nach `EPOS.UI.Daten`** | **L** | **das Stück mit der klarsten Plattformfolge** | **Schema** (Kennzeichen je Position/Technik) · **Kern** (`NutzungsdauerAbgleich.Hinweis` zählt heute nur kürzeste und längste Dauer, nicht „k von n ohne Dauer") · **Umzug** von `WirtschaftlichkeitSeiteGaben` (§ 4) |

### 2.2 Die 20 erledigten Zeilen — Plattformfolge

Alle 20 sind belegt (`03/§8`). Für diese Analyse zählt nur, **wo** sie gelandet sind:

| Gruppe | U-Nummern | Ort | Folge für iOS |
|---|---|---|---|
| Im Kern (Rechenweg, Zeilendefinition, Nachweisumschlag) | U7-nah: U17, U19, U21, U23, U24, U26, U28, U29, U30, U33, U34, U35, U36 | `EPOS.Kern` | plattformfrei — wirkt auf jeder Schale |
| Im Katalog / Schema | U16 (Bereich UMLAGEN), U20 (`Stromst_Befreiung_Modus`, Schritt 88), U38 (Schritt 93) | `EPOS.Kern` + DDL | plattformfrei |
| In der Razor-Oberfläche | U31 (Betriebsseite), U37 (Vergleichssicht), U38 (Optionsgruppe `ErtragBonus.razor:78`) | `EPOS.UI` | plattformfrei — aber der **Wirt** ist Windows-gebunden (#9, #16, #20) |
| Im Bericht | U18 (`ChartRenderer.cs:106, 574–586, 684–703`; `BausteineWirtschaftlichkeit.cs:269–272`) | `EPOS.Kern` | plattformfrei |
| **Windows-gebunden** | **U8** (Knopf „Nutzungsdauern vorbelegen…" am Kostendialog) · **U40** (Bemessungsübernahme aus der Vorlage, `KostenKomponenteHuelle.cs:733–740`) | `…/Views/Kosten/KostenKomponenteHuelle.cs` **W** | **auf iOS nicht vorhanden**, solange die Kostenhülle nicht umzieht |

**U40** (`03/#28`): „umgesetzt", belegt in `VorlagenZeile.razor:115–133`, `KostenHerleitung.cs:156–184`
und `KostenKomponenteHuelle.cs:733–740`. Größe der Nacharbeit **S** (Anhangzeile und Kommentarmarke,
Ziel Mockup + Code); Plattform: nur Windows, weil die Datenseite in der Windows-Hülle liegt;
Abhängigkeit: keine.

---

## 3 WinForms-Reste im Konzept § 2 — nur Neues gegenüber `02/d-1…d-22`

`02` benennt bereits: die Überschriften § 2.2/2.3/2.4 mit `Form_*` (d-22), die Fußleiste mit sieben
Knöpfen (d-11), die zwei Spalten des PV-Dialogs (d-6), die Reiter und die Rasterspalten der
Kostenverwaltung (d-12, d-13), die Fußknöpfe (d-14). **Die folgenden 15 Stellen sind dort nicht
genannt.**

| Nr | Zeile | Konzepttext (alt) | Razor-Entsprechung (neu) — Beleg |
|---|---|---|---|
| n-1 | 107, 108 | Spalte „Eigener Wirtschaftlichkeitsdialog": `Form_BhkwWirtschaftlichkeit`, `Form_PhotovoltaikVerguetung` | `BhkwWirtschaftlichkeitDialog.razor`, `PhotovoltaikVerguetungDialog.razor` — d-22 fasst nur die **Überschriften** an, nicht die Tabelle § 2.1 |
| n-2 | 122 | „Knopf in der Fußleiste von `UcWirtschaftlichkeit`" | `EPOS.UI/Seiten/Berichte/WirtschaftlichkeitSeite.razor:376` (Knopf „BHKW…") |
| n-3 | 136 | „8 Bestandsfelder (heute in `Form_KwkgModule`)" | Gruppe 1b, `BhkwWirtschaftlichkeitDialog.razor:120–228`; die Maske ist gelöscht |
| n-4 | 144, 145 | „DateTimePicker mit Haken" | Datumsfeld mit Kontrollkästchen (Razor `<input type="date">` im Formularraster) |
| n-5 | 146, 147, 153, 154 | „ComboBox" (vier Felder) | Baustein `Auswahlfeld`, z. B. `BhkwWirtschaftlichkeitDialog.razor:203`, `:302` |
| n-6 | 190, 334, 339, 346 | „Tooltip" | Werkzeugtipp = `title`-Attribut; im Code `KostenSeite.razor:192`. Der Rest des Papiers sagt „Werkzeugtipp" (z. B. Z. 1139) — zwei Wörter für dieselbe Sache |
| n-7 | 227 | „914 × 724, festes Fenster" | Die Razor-Komponente kennt kein Maß. Das **Wunschmaß** steht als Konstante der Hülle (`PhotovoltaikVerguetungHuelle.FENSTER_BREITE`); die Plattformhülle macht daraus ihr Fenster — Muster `EnergietraegerHuelle.FENSTER_BREITE/FENSTER_HOEHE` → `EnergietraegerFenster.cs:58`. d-6 ändert nur die Spaltenzahl |
| n-8 | 227, 234, 235, 245 | „CheckBox" (vier Stellen) | Kontrollkästchen / Schalter im Formularraster |
| n-9 | 231, 232 | „Radio", „Radios:" | Baustein `Optionsgruppe`, `PhotovoltaikVerguetungDialog.razor:133` |
| n-10 | 310 | „`UcBkKosten.cs:771-772`" | `EPOS.UI/Seiten/Berichte/KostenSeite.razor` + `…/Views/BerichteKosten/KostenSeiteGaben.cs:658`. Die Maske ist gelöscht — der Zeilenverweis zeigt ins Leere |
| n-11 | 465–466 | „Designer-basiert (FK1/Ä6), Texte über `MyResource` mit GetString-Rückfall" | Razor-Komponenten **ohne** Designer; Texte kommen über `[Parameter]`-Vorgaben und `*Texte`-Bündel und werden in der Hülle mit `Resource.*` bzw. `T(schlüssel, rückfall)` belegt (`04`, § 3, Schlussabsatz) |
| n-12 | 468 | „Fußleiste von `UcWirtschaftlichkeit`" | `WirtschaftlichkeitSeite.razor:363–391`. d-11 berichtigt die **Zahl**, nicht den Namen |
| n-13 | 523, 700, 728 | „`Form_CaseEingabe` (±-Knopf)" — dreimal | `EPOS.UI/Dialoge/Kosten/CaseEingabeDialog.razor`; der ±-Knopf sitzt in `VorlagenZeile.razor:155–159` |
| n-14 | 571, 594, 601, 688 | „Kennzahlengrid", „ListView", „unter dem Grid" | Vergleichstabelle `WirtschaftlichkeitSeite.razor:237–262` (`<table class="epos-raster epos-matrix">`); die Variantenliste ist eine `Optionsgruppe` je Zeile (`:120`) — kein ListView, kein Grid |
| n-15 | 591, 664, 682 | „`UcWirtschaftlichkeit`" — dreimal | `WirtschaftlichkeitSeite.razor` |

**Dazu eine Sachstelle, die kein Begriff, sondern eine unvollständige Begründung ist:**
Z. 1339 sagt, der PV-Dialog sei auf iOS nicht erreichbar, weil eine plattformfreie Hülle fehlt. Das
ist richtig, aber nicht hinreichend — auch **mit** Hülle bliebe er unerreichbar, solange sein Wirt
(die Wirtschaftlichkeitsseite) auf iOS leer bleibt (`IProjektQuelle.cs:272`) und kein Seitenschlüssel
in der Whitelist steht (`AppWurzel.razor:1431–1445`). Zwei Bedingungen, nicht eine.

Z. 909 („wird außer in der **Designer-Datei** nirgends gelesen") meint `Resource.Designer.cs`, die
erzeugte Ressourcenklasse — nicht eine Formular-Designer-Datei. In einem Papier, das an 15 Stellen
von WinForms-Designern spricht, ist das missverständlich.

---

## 4 Plattformfreiheit (iOS)

### 4.1 Was iOS heute kann und nicht kann

| Stück | Erreichbar? | Beleg |
|---|---|---|
| BHKW-Wirtschaftlichkeitsdialog | **ja**, lesend und schreibend | `AppWurzel.razor:1432`; `EPOS.iOS/Dienste/IosProjektQuelle.cs:201–242` (Laden), `:249–264` (Schreiben) |
| Energieträger-**Variantendialog** | ja, lesend | `AppWurzel.razor:1431`; `IosProjektQuelle.cs:138–142`, `:170` |
| Energieträger **anlegen** | **benannt abgelehnt** | `IosProjektQuelle.cs:164–184`, Rückgabe `""` `:183` mit Protokollzeile |
| Gesetzeskatalog als Nachschlagewerk | ja | `IosProjektQuelle.cs:209, 232` |
| Ansicht „Berichte & Kosten" (Kostenseite, Wirtschaftlichkeitsseite) | **nein** — Schlüssel whitelistet, Gaben nicht belegt → `null` | `AppWurzel.razor:1441` gegen `EPOS.UI/Dienste/IProjektQuelle.cs:272` |
| Kostenverwaltung, Nutzungsdauern, Gesetzeskatalog-Maske, Emissionskatalog, Kostenfaktoren, Tarifstruktur, Verlauf, PV-Vergütung, Parameter | **nein** — kein Whitelist-Fall | `AppWurzel.razor:1445` (`return false`) |

**Die Hausregel steht schon.** `EPOS.iOS/CLAUDE.md:15–21`: „Was hier NICHT geht, wird BENANNT
abgelehnt und fällt nicht still aus" — Muster `SimulationPlattformwege.Ohne(<Grund>)` mit Meldung
über `Dienste.Dialog`, und für einen Weg ohne Delegat: „kein Delegat, kein Knopf". Die Nähte dafür
existieren als Vorbild: `EPOS.UI.Daten/Simulation/SimulationPlattformwege.cs` (95 Z.),
`EPOS.UI.Daten/Assistent/AssistentPlattformwege.cs`, `EPOS.UI.Daten/Katalogwege.cs` (32 Z.).

### 4.2 Je Hülle: Naht gegen Datenweg (gemessen)

| Hülle | Z. gesamt | WinForms-Naht (Zeilen) | Was die Naht ist | Datenweg (in Kern-Controller **bereits**) | Umzugsgröße |
|---|---|---|---|---|---|
| `…/Wirtschaftlichkeit/WirtschaftlichkeitParameterHuelle.cs` | 148 | **0** | — (der Dialog erscheint als Überlagerung **in** der Seite, kein eigenes Fenster) | `WirtschaftlichkeitCtrl.LadeParameter/SpeichereParameter`, `EmissionsBilanzRechner`, `GesetzKatalog` | **S** — Datei verschieben, `namespace WindowsFormsApplication1` bleibt (dieselbe Konvention wie `EPOS.UI.Daten/Kosten/*`) |
| `…/Kosten/KostenfaktorKatalogHuelle.cs` | 96 | **0** | — | `KostenfaktorCtrl` | **S** |
| `…/Kosten/VorlagenUebernahmeHuelle.cs` | 366 | **0** | — | 5 Controller | **S** |
| `…/Kosten/ErtragBonusGaben.cs` | 319 | **0** | — | `KostenVorlagenUebernahmeCtrl`, `VariantenCtrl`, `StartseiteCtrl` | **S** |
| `…/Wirtschaftlichkeit/WirtschaftlichkeitSeiteGaben.cs` | 1 015 | **3** (`:35` Feld `Func<Form>`, `:86` Zuweisung, `:921` Weitergabe an `PhotovoltaikVerguetungHuelle.Gaben`) | nur der **Fensterbesitzer**, und der nur für den `OpenFileDialog` des PV-Marktwert-Imports | `WirtschaftlichkeitCtrl`, `SpeicherAnzeigeCtrl` — Unterdialoge kommen als `Gaben` in eine `Ueberlagerung` (`:913–948`) | **M** — erst `OpenFileDialog` → `Dienste.Datei` (s. u.), dann fällt die Naht auf 0 |
| `…/BerichteKosten/KostenSeiteGaben.cs` | 1 054 | **3** (`:6` `using`, `:30` Feld, `:61` Zuweisung) — **und keine davon wird benutzt** | toter Fensterbesitzer; der Kostendialog kommt als Überlagerung über `KostenKomponenteHuelle.GabenProjekt` (`:936`) | 8 Controller | **S** — drei Zeilen streichen, Datei verschieben |
| `…/Admin/GesetzeskatalogHuelle.cs` | 154 | ≈ 31 (`:37` `Size MASS`, `:45–73` `Oeffnen`) | `BlazorDialogForm` + Maß + `ShowDialog` | `GesetzKatalog` (`Gaben` `:74–154`) | **S** — Muster `EnergietraegerFenster` |
| `…/Wirtschaftlichkeit/TarifstrukturHuelle.cs` | 108 | ≈ 42 (`:40–81`, zwei `Oeffnen`-Überladungen) | dito | `WirtschaftlichkeitCtrl` (`Gaben` `:82–108`) | **S** |
| `…/Wirtschaftlichkeit/KapitalwertVerlaufHuelle.cs` | 267 | ≈ 40 (`:57–96`) | dito | `WirtschaftlichkeitCtrl.BerechneVerlauf` (`Gaben` `:97–267`) | **M** — zusammen mit U3 |
| `…/Wirtschaftlichkeit/BhkwWirtschaftlichkeitHuelle.cs` | 235 | ≈ 86 (`:63–119` Öffnen; `:207–235` `TarifOeffnen` **mit `MessageBox` `:223`**) | zweites Fenster für den Tarif-Sprung + Meldungsweg an WinForms vorbei | `KwkgAnlagenCtrl`, `WirtschaftlichkeitCtrl` (`Gaben` `:120–206`) | **M** — der Sprung muss zur Überlagerung werden (`04/B11` hat dafür schon die Bedienregel) |
| `…/Wirtschaftlichkeit/PhotovoltaikVerguetungHuelle.cs` | 313 | ≈ 75 (`:46–100` Öffnen; `:268–287` `MarktwerteImportieren` **mit `OpenFileDialog` `:273`**; Tarif-Sprung `:59`) | zweites Fenster + Dateiwahl + Tarif-Sprung | 6 Controller | **M** |
| `…/Kosten/KostenKomponenteHuelle.cs` | 1 336 | ≈ 61 (`:100–160`, `Oeffnen`/`OeffnenProjekt`/`Zeigen`) | `BlazorDialogForm` + `ShowDialog`; **alle** Unterdialoge kommen bereits als `Gaben` (z. B. Gesetzeskatalog `:212`) | 7 Controller | **M** — größte Datei, aber dünnste Naht im Verhältnis (4,6 %) |

**Summe:** 6 631 Zeilen Datenseite in `Views/`, davon **ca. 341 Zeilen echte WinForms-Naht (5,1 %)**.

### 4.3 Das Muster im Bestand

`EPOS.UI.Daten/Kosten/EnergietraegerHuelle.cs` (2 773 Z., plattformfrei: „Ihre Quellen sind
Kern-Controller, sie kennt kein Fenster", `:14–19`) gegen
`WindowsFormsApplication1/Views/Kosten/EnergietraegerFenster.cs` (**67 Z.**): Der Adapter hält genau
vier Dinge — eine Hülleninstanz (`:43`), den `Geschlossen`-Rückruf (`:50–54`), Titel und Maß aus
Konstanten der Hülle (`:56–59`) und `ShowDialog` (`:63`). Wortgleich dazu
`NutzungsdauerFenster.cs` (58 Z.) — der Beweis, dass das Muster reproduzierbar ist.

**Die Dateiwahl ist der einzige nichttriviale Fall, und auch der ist gelöst.**
`EPOS.UI.Daten/Kosten/SpotpreisImportHuelle.cs:54–57` ruft
`Dienste.Datei.DateiOeffnenAsync(...)` „statt `OpenFileDialog`" (Kommentar `:17–18`), mit dem
Hinweis, dass der Wähler hinter dem Blazor-Ereignis läuft (Befund W13-B-1). Die iOS-Seite ist
belegt (`EPOS.iOS/Dienste/IosDateiDienst.cs:146`, FilePicker). `PhotovoltaikVerguetungHuelle.cs:273`
ist damit ein **Rückstand gegen den eigenen Hausstand**, kein offenes Problem.

### 4.4 Reihenfolge

`04/B22` schlägt vor: `KostenKomponenteHuelle` → `WirtschaftlichkeitParameterHuelle` →
`BhkwWirtschaftlichkeitHuelle` → `PhotovoltaikVerguetungHuelle` → `GesetzeskatalogHuelle`.
**Gemessen ergibt sich eine andere Reihenfolge** — nach Naht-Aufwand und nach dem, was danach
tatsächlich erreichbar ist:

| Schritt | Was | Größe | Warum hier |
|---|---|---|---|
| 1 | Die vier Hüllen **ohne** Naht verschieben: `WirtschaftlichkeitParameterHuelle`, `KostenfaktorKatalogHuelle`, `VorlagenUebernahmeHuelle`, `ErtragBonusGaben` (929 Z.) | **S** | reines Verschieben, null Risiko, drittelt den Rückstand |
| 2 | `PhotovoltaikVerguetungHuelle.MarktwerteImportieren` auf `Dienste.Datei.DateiOeffnenAsync` (Muster `SpotpreisImportHuelle:57`) | **S** | löst gleichzeitig die einzige Naht von `WirtschaftlichkeitSeiteGaben` (`:921`) |
| 3 | `KostenSeiteGaben` (drei tote Zeilen streichen) und `WirtschaftlichkeitSeiteGaben` verschieben (2 069 Z.) | **S/M** | danach sind **beide Seiten** plattformfrei; erst damit lohnt sich `BerichteKostenGaben` in der iOS-Schale |
| 4 | `KostenKomponenteHuelle` (Naht 61 Z.) + `EnergietraegerFenster`-Muster | **M** | größte Nutzenstufe: Kostenverwaltung auf iOS, und mit ihr U8 und U40 |
| 5 | `PhotovoltaikVerguetungHuelle`, `TarifstrukturHuelle`, `GesetzeskatalogHuelle`, `KapitalwertVerlaufHuelle` | **M** | brauchen je einen Fenster-Adapter; die zwei **Tarif-Sprünge** (`BhkwWirtschaftlichkeitHuelle:219`, `PhotovoltaikVerguetungHuelle:59`) werden dabei zu Überlagerungen |
| 6 | `BhkwWirtschaftlichkeitHuelle` (`MessageBox` `:223` → `Dienste.Dialog`) | **M** | der Dialog steht schon auf iOS; erst hier wird auch sein Sprungweg tragfähig |
| 7 | `IosProjektQuelle.BerichteKostenGaben` belegen und die fehlenden Seitenschlüssel in `AppWurzel.razor:1431–1443` aufnehmen | **M** | **ohne diesen Schritt bleibt jeder Umzug folgenlos** — plattformfrei ≠ erreichbar (Beleg: `NutzungsdauerHuelle` ist seit ND-Q3 `D` und trotzdem auf iOS nicht zu öffnen) |

### 4.5 Der Kern-Controller-Weg für die Zeilenliste der Wirtschaftlichkeitsseite

Die Frage aus dem Auftrag („Nach #346 (b)"): `WirtschaftlichkeitSeiteGaben.cs` baut heute
`Zeitraumzeile()` (`:404–417`) und `Vereinfachungszeile()` (`:424 ff.`) selbst — es sind die
**einzigen** Schreiber von `WirtschaftlichkeitStand.Zeitraumzeile` bzw. `.Vereinfachungszeile`
(`WirtschaftlichkeitDaten.cs:175`, `:183`; nachgemessen repoweit: je zwei Treffer, Hülle und Test).

Gemessen ist der Kern dabei schon fast ganz beteiligt: `Zeitraumzeile()` ruft
`_ctrl.LadeParameter`, `WirtschaftlichkeitCtrl.LiesInvestitionen(id, ERWARTET)` und
**`NutzungsdauerAbgleich.Hinweis(T, positionen, kultur)`** — die Textbildung liegt also **bereits**
im Kern. Was in der Hülle steht, ist allein das Einsammeln der Positionen über die Gruppe
(`:410–412`) und ein `try/catch`.

**Der Weg ist deshalb kein Umbau, sondern ein Zusammenziehen:**

1. Eine Kern-Methode, die Gruppe und Szenario nimmt und die fertige Zeile liefert — sinngemäß
   `NutzungsdauerAbgleich.Hinweis(idStamm, gruppe)`; dort auch die von § 2.13 (3) geforderte
   Ergänzung „k von n betragstragenden Positionen ohne Dauer" gegen `NutzungsdauerCtrl.Vorgabe`
   (heute bildet die Methode nur kürzeste und längste Dauer).
2. `Vereinfachungszeile()` ist bereits `static` und kennt nur ein `bool` — sie gehört als
   Textbaustein neben die Zeilendefinition, damit Word, Excel und beide Schalen denselben Satz
   lesen.
3. Danach ruft die plattformfreie Hülle in `EPOS.UI.Daten` nur noch beides auf; die Windows-Schale
   hat damit gar keine Zeilenliste mehr.

Das ist zugleich das letzte Stück von **U39** und der Grund, warum U39 im Konzept unter § 2.13 (3)
und nicht unter „Bericht" steht.

---

## 5 Entscheide § 5 mit Dialogbezug — Stand im Code

| # | Entscheid (Konzept Z.) | Stand im Code | Beleg |
|---|---|---|---|
| **K2** | Hilfsenergie-Basis je Anlage: Weg B, „Dialog benennt die Basis klar" (2123) | **umgesetzt** — das Feld trägt die Bemessung im Klartext; der Dialog sagt ausdrücklich, dass am **Endenergiebedarf (Brennstoff)** der Anlage gerechnet wird, nicht an den Kosten. Eine vierte Spalte gibt es nicht (wie vorgesehen) | `BhkwWirtschaftlichkeitDialog.razor:209–217` |
| **K4** | Tabellenspalte „Brennstoff" ohne Leseweg; kleiner Leser `CarrierId` → Name (2125) | **umgesetzt** — die Anlagentabelle führt die Spalte und füllt sie | `BhkwWirtschaftlichkeitDialog.razor:95–96` (`TemplateColumn … Title="@_t.SpBrennstoff"`, `@context.Brennstoffname`) |
| **K5** | Jahresnutzungsgrad bleibt Projektgröße, „als Projektfeld zeigen" (2126) | **umgesetzt** — steht in Gruppe 3 (Projektebene), mit Kommentarmarke `K5`; 0 % heißt „nicht erfasst" | `BhkwWirtschaftlichkeitDialog.razor:307–311`, Setzer `:749–752` |
| **K6** | WP-Hilfsenergie: Feld nur bei BHKW (2127) | **umgesetzt**, mit ausdrücklicher Begründung im Quelltext | `BhkwWirtschaftlichkeitDialog.razor:609–611` |
| **K9** | § 6.1 zählt „9 Felder", real 11 — Konzeptkorrektur (2130) | **offen, aber nur als Papier** — der Schreibweg führt die 11 Spalten (K7 erfüllt): `KWKG_Kostenanteil`, `Energiesteuer_Wahl`, `Aufteilung_Methode`, `Hilfsenergie_Anteil` sind belegt | `EPOS.Kern/Allgemein/Wirtschaftlichkeit/KwkgAnlagenCtrl.cs:55, 66, 70, 74, 243–244` |
| **D-1** | Eine Emissionsspalte nach `Emission_Berechnungsmodus` (2138) | **umgesetzt** | `KostenSeiteGaben.cs:658`; Anzeige `KostenSeite.razor:192` |
| **E-1** | Modus CO2E: Wert zeigen, Umstand im Werkzeugtipp; kein stiller Rückfall (2139) | **umgesetzt** — der Kurztext reist als `EmissionKurztext` an der Zeile | `KostenSeite.razor:192` (`title=@(spalte >= 7 ? zeile.EmissionKurztext : zeile.Kurztext)`) |
| **D-2** | Erlösrubrik in zwei Blöcken, getrennte Summen (2140) | **umgesetzt** — `BLOCK_A`/`BLOCK_B` als zwei Wege in die Zeilenliste, nicht als Flag | `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitZeilen.cs:95–96, 136, 139, 349, 370` |
| **D-3** | Wählbares Vergleichsprojekt je Gruppe (2141) | **umgesetzt** | `WirtschaftlichkeitSeite.razor:120`; Stand `WirtschaftlichkeitDaten.cs:223`; Hülle `WirtschaftlichkeitSeiteGaben.cs:324, 363` |
| **ET-D-1** | Preisbestandteile in der Abrechnungseinheit an der Anzeigekante (2149) | **umgesetzt** in der plattformfreien Trägerkarte | `EPOS.UI.Daten/Kosten/EnergietraegerHuelle.cs` (Preisbasis/Formelzeile), Dialog `EnergietraegerEinstellungen.razor` |
| **ET-D-2** | Emissionsblock zeigt die Arten **dieses** Trägers; Modus im Katalogkontext nur lesbar (2150) | **umgesetzt** — eigener Emissionskatalogaufruf je Träger | `EnergietraegerHuelle.cs:2171–2190` (`EmissionskatalogHuelle.Aufruf` mit `_gewaehlt.ID`) |
| **ET-D-3** | Preisbasis bietet genau zwei Einträge, Faktor = Heizwert (2151) | **umgesetzt im Kern**, aber der Kartenzustand fällt weiter auf `ID_Umrechnung = -1` zurück — das ist **U32**, offen | `EnergietraegerPreisCtrl.cs:114–140` (`03/§8`) |
| **E1** | Elektroheizkessel gehört zur Stromfamilie; **ein** Stromträger je Projekt (2153) | **umgesetzt** — die Auflösung steht im Kern und wird von der Kostenseite angestoßen | `ProjektEnergietraegerCtrl.StromTraegerDerAnlagen`; `KostenSeiteGaben.cs:141` (`StromTraegerSicherstellen`) |

**Ohne Dialogbezug und deshalb hier nur genannt:** K1 (entschieden: kein Feld), K3 (erledigt mit B6),
K7 (erledigt, siehe K9-Zeile), K8 (entschieden = U2, offen), K10, K11 (erledigt).

---

## 6 Ergebnisansicht § 2.13 (1)–(6)

Maßstab: was `WirtschaftlichkeitSeite.razor` (1 036 Z.), `WirtschaftlichkeitDaten.cs` und
`WirtschaftlichkeitSeiteGaben.cs` (1 015 Z.) **heute zeichnen**. Rechen- und Zeichenlogik bleibt
außen vor (anderer Prüfer).

| Punkt | Was die Seite heute zeigt | Was fehlt | Größe |
|---|---|---|---|
| **(1)** Energiekosten statt „Verbrauchskosten (BHKW)" | Die Vergleichstabelle zieht ihre Zeilen aus `WirtschaftlichkeitZeilen.Kennzahlen` (`WirtschaftlichkeitSeite.razor:237–262`); die „davon ‹Anlage›"-Unterzeilen entstehen im Kern und reisen im Nachweisumschlag (`ErgebnisNachweisUmschlag.cs:74–75, 148, 211, 226`; Bildung `KostenEmissionRechner.cs:593–595`) | nichts an der Oberfläche — die Ansicht muss die vorhandenen Zeilen nur unter der richtigen Überschrift führen (`WIRT_ENK_KOPF` liegt ungelesen bereit) | **S** |
| **(2)** „Zuschuss BAFA" → „Zuschuss" | Ressourcenseite, keine Oberflächenarbeit | — | — |
| **(3)** Ersatz und Restwert je Komponente + Hinweis | `Zeitraumzeile` und `Vereinfachungszeile` stehen als `Herleitungszeile` unter dem Parameternachweis (`WirtschaftlichkeitSeite.razor:179–186`) | **(a)** der Hinweis „k von n Positionen ohne Nutzungsdauer (Position, Betrag)" — `NutzungsdauerAbgleich.Hinweis` bildet nur kürzeste und längste Dauer; **(b)** die Zeile entsteht **nur** in der Windows-Hülle (`WirtschaftlichkeitSeiteGaben.cs:404`) und gehört nach `EPOS.UI.Daten` (§ 4.5, U39); **(c)** eine Zusammenfassung je Komponente (Betrag, Dauer, Ersatzjahr, Restwert, Herkunft) gibt es auf der Seite nicht — sie steht im Kostendialog (U30, erledigt) | **M** |
| **(4)** Erlöse und Vorteile je Komponente | Block A/B sind im Kern gebaut (`WirtschaftlichkeitZeilen.cs:136, 139, 349`), die PV-Herkunft als Zeile `PV_HERKUNFT` (`:487`) | die **innere Gliederung nach Komponente** (BHKW · PV · Kessel · projektweit) mit Zwischensummen — die Katalogzeile trägt kein Anlagenfeld (**U6**) | **L** (Kern zuerst) |
| **(5)** Verlauf mit allen drei Szenarien | eigener Dialog hinter dem Knopf „Verlauf…" (`WirtschaftlichkeitSeite.razor:383–384`, Überlagerung `:452–461`); **ein** Szenario je Lauf, zwei Bilder | Abschnitt „Wie sicher ist das?" mit der Dreierkurve inline; Knopf entfällt; zweigeteilte Legende; Nulldurchgang je Szenario; **plattformfrei nach `EPOS.UI.Daten`** (**U3**, dort fehlt der Ordner `Wirtschaftlichkeit` ganz) | **L** |
| **(6)** Vergleichssicht | **vollständig gebaut**: Optionsgruppe, Listen A/B mit gegenseitigem Ausschluss, Tauschknopf ⇄, weiche Sperre bei nur einem Stand, Erklärzeile (`WirtschaftlichkeitSeite.razor:211–234`, `:830–849`) | — (U37 erledigt) | — |

**Die vier Sonderstücke des Auftrags:**

| Stück | Stand | Beleg |
|---|---|---|
| **Umschalter „Kennzahlen / ValERI-Bewertung" (K8 / V-1)** | **fehlt.** Was an dieser Stelle steht, ist ein *Aufklappblock* „Bewertung nach DIN EN 17463" mit **einem** Textfeld für die nicht monetären Wirkungen und einem Speichern-Knopf — nicht die zweite Ansicht mit den fünf Blöcken | `WirtschaftlichkeitSeite.razor:309–347` (`epos-modulparameter-knopf`, `_bewertungOffen`, `Textfeld Mehrzeilig`) |
| **Hinweistext § 2.11.7** | **fehlt vollständig.** Nachgemessen: `WIRT_SZEN_HINWEIS` hat repoweit **0** Treffer in `.cs`, `.razor` und `.resx`. Die heutige `Szenariozeile` (`:169–172`) ist der Satz **dieses** Szenarios aus `WirtschaftlichkeitSeiteGaben.cs:673`, nicht der Hinweis, was ein Szenario variiert | U10 |
| **Hinweiszeile Nutzungsdauer** | teilweise: Text da, Zahl „k von n" fehlt, Ort ist die Windows-Hülle | § 6 (3) |
| **Verlauf (nur Oberflächenseite)** | Knopf + Überlagerung + ein Szenario; die Seite reicht ihre Szenariowahl **nicht** durch | § 6 (5) |

**Was der Stand der Seite überhaupt nicht kennt** (gemessen an den 26 Eigenschaften von
`WirtschaftlichkeitStand`, `WirtschaftlichkeitDaten.cs`): Bandbreite (U4), Empfehlungskarten je
Version (U5, es gibt nur `Empfehlungszeile` `:139`), Gliederung des Kapitalwerts, Brücke, die fünf
ValERI-Blöcke, der Szenario-Hinweistext. Die Ergebnisansicht ist damit **das größte offene Stück
der ganzen Oberfläche** — und zugleich das einzige, dessen Fehlteile fast alle im Kern und nicht im
Markup liegen.

---

## 7 Bausteine, die das Konzept voraussetzt und die fehlen

`04/B06`, `B09`, `B01`–`B04`, `B20` beschreiben die Regeln. Hier der **Umfang** und der eine
Baustein, den `04` nicht behandelt.

| Baustein | Zustand | Umfang | Verweis |
|---|---|---|---|
| **`Dialogkopf`** (Titel + Kontextzeile + `InfoKnopf` + `Schliesskreuz`) | **existiert nicht** — nachgemessen im Ordner `EPOS.UI/Bausteine/` (59 Dateien, kein `Dialogkopf.razor`). Jeder der 21 Dialoge baut den Kopf selbst, in **vier** Bauarten | eine neue `.razor` von ~40 Z. plus 21 Ersetzungen à ~6 Z. → **M**; der Gewinn ist die Wache | `04/B06` |
| **Kontextzeile „{Projekt} · {Variante} · netto"** | in 7 von 21 Dialogen vorhanden (`04`, Spalte Ktx) | mit dem `Dialogkopf` zusammen, sonst 14 Einzeländerungen | `04/B09`, `04/B10` |
| **Fußleistenregel** (`SpeichernLeiste` mit `RenderFragment Aktionen`) | `SpeichernLeiste.razor` hat 10 Parameter, **keinen** für Aktionsknöpfe (nachgemessen `:37–74`); 11 Dialoge bauen deshalb eine eigene `<div class="epos-leiste">` | ein Parameter + 11 Umbauten → **M** | `04/B01`–`04/B04` |
| **Spaltenfilter in den Kostenkatalogen** | Der Baustein **existiert und ist erprobt**: `Spaltenfilter.razor`, `Katalogliste.razor`, `Katalogfiltertexte.cs`, `Katalogfilterregister.Stand(art)` — in Gebrauch in den Bedarfs-Admindialogen (`BedarfAdminDialog.razor:352`, `BedarfsProfileDialog.razor:549`, `WaermebedarfAdminDialog.razor:304`). In **keinem** Kosten- oder Wirtschaftlichkeitsdialog | vier Dialoge à ~15 Z. (Gesetzeskatalog, Emissionskatalog, Kostenfaktoren, Nutzungsdauern) → **S je Dialog**, **M gesamt**; Tests liegen vor (`SpaltenfilterTests` 15, `KataloglisteTests` 22) | `04/B20` |
| **Szenario-±-Knopf an Trägerpreisen, Erlösfeldern und der Rahmen-Gruppe** | **Das Muster ist im Bestand** — `CaseEingabeDialog.razor` (331 Z.) mit `CaseEingabeErgebnis.cs`, ausgelöst vom ±-Knopf `VorlagenZeile.razor:155–159` (`MitWorstBest`, `WorstBestAngefordert`, `WorstBestKurztext`). **Aber: genau ein Wirt** — `KostenKomponenteDialog.razor:352–354, 1073`. An Trägerpreisen (`EnergietraegerEinstellungen.razor`), Erlösfeldern (`PhotovoltaikVerguetungDialog.razor`) und der Rahmen-Gruppe (`WirtschaftlichkeitParameterDialog.razor`) gibt es keinen | je Ort: ±-Knopf + `Ueberlagerung` + Gaben ≈ 30 Z. Oberfläche — **aber jeder Ort braucht vorher seine Best/Worst-Spalten** (U15, Schema). Oberfläche **S je Ort**, Gesamtvorhaben **L** | **neu, nicht in `04`** — Konzept § 2.11.5 Z. 728 |

**Die Razor-Entsprechung von `Form_CaseEingabe`** (dreimal im Konzept genannt, Z. 523, 700, 728) ist
`EPOS.UI/Dialoge/Kosten/CaseEingabeDialog.razor`; sie ist keine eigene Hülle, sondern eine
Überlagerung, die ihre Gaben vom Wirt bekommt. Für die drei neuen Orte heißt das: **keine neue
Hülle nötig**, nur je ein Gaben-Bauer im Wirt.

---

## 8 Umsetzungsliste dieses Teils

### 8.1 Arbeiten

| # | Punkt | Größe | Plattform | Abhängigkeit | Test / Abnahme |
|---|---|---|---|---|---|
| 1 | U10: Hinweistext „Was ein Szenario variiert" als Ressource, unter die Annahmentafel | **S** | plattformfrei | Entscheid Wortlaut (`02/d-18`) | Ressourcenwache beide Sprachen; bunit: Zeile steht unter der Tafel |
| 2 | U11: Sammelknopf „Vorschlagswerte übernehmen" in `StrompreisDetails` | **S** | plattformfrei (`D`) | keine | bunit: Knopf setzt Werte und Haken; ein Anteil ohne Vorschlag bleibt unberührt |
| 3 | `PhotovoltaikVerguetungHuelle.MarktwerteImportieren` → `Dienste.Datei.DateiOeffnenAsync` | **S** | löst die letzte Naht von `WirtschaftlichkeitSeiteGaben` | Muster `SpotpreisImportHuelle:54–57` | Probe: Import läuft unter Windows unverändert; `_besitzer` entfällt |
| 4 | Vier nahtlose Hüllen nach `EPOS.UI.Daten` verschieben (929 Z.) | **S** | Voraussetzung für iOS | keine | Bau beider Schalen; Referenzlauf unverändert |
| 5 | `KostenSeiteGaben`: drei tote Windows-Zeilen streichen, Datei verschieben | **S** | dito | keine | dito |
| 6 | `WirtschaftlichkeitSeiteGaben` verschieben (nach #3) | **M** | dito | #3 | `WirtschaftlichkeitSeiteTests` (58) unverändert |
| 7 | U2: Umschalter „Kennzahlen / ValERI" im Kopf; Knopf „Verlauf…" entfernen | **M** | plattformfrei | Entscheid liegt vor (K-8/V-1); inhaltlich U3 | bunit: beide Zustände gezeichnet, Fußleiste mit vier Knöpfen |
| 8 | § 2.13 (3): „k von n ohne Nutzungsdauer" in `NutzungsdauerAbgleich.Hinweis`; Zeile über den Kern statt über die Hülle | **M** | plattformfrei (U39-Teil) | Kern | Kern-Test: Zahl stimmt gegen `NutzungsdauerCtrl.Vorgabe`; bunit: Zeile erscheint |
| 9 | Spaltenfilter in die vier Kosten-/Wirtschaftlichkeitskataloge | **M** | plattformfrei | Bausteine liegen vor | `SpaltenfilterTests`-Muster je Dialog |
| 10 | `Dialogkopf` als Baustein + Kontextzeile (21 Dialoge) | **M** | plattformfrei | `04/B06`, `04/B09` | Wache „eine Bauart", bunit je Dialog |
| 11 | `KostenKomponenteHuelle` umziehen (Naht 61 Z.) | **M** | Kostenverwaltung auf iOS; bringt U8 und U40 mit | #4 als Muster | `KostenKomponenteDialogTests` (93) unverändert; Windows-Fenster wie zuvor |
| 12 | Tarif-Sprünge aus BHKW und PV zur Überlagerung machen; `MessageBox` → `Dienste.Dialog` | **M** | beseitigt die letzten Zweitfenster | `04/B11` (Bedienregel) | bunit: Sprung speichert und wechselt |
| 13 | `IosProjektQuelle.BerichteKostenGaben` belegen; fehlende Seitenschlüssel in die Whitelist | **M** | **macht alles Vorherige erst sichtbar** | #6, #11 | Prüflauf auf dem Gerät (`EPOS.iOS/Pruefung/Prueflauf.cs`-Muster) |
| 14 | U3/U4/U5: Dreierreihe, Bandbreite, Empfehlungskarten; Verlaufslogik nach `EPOS.UI.Daten` | **L** | plattformfrei gefordert | Kern (`BerechneVerlauf`, `VerlaufsReihen`, `ChartRenderer`), Entscheid Bildmaß | Zahlenprobe je Szenario; bunit drei Spalten/drei Karten |
| 15 | U6 + § 2.13 (4): Anlagenbezug je Katalogzeile, Erlösrubrik nach Komponente | **L** | plattformfrei (Kern) | Kern (`WirtschaftlichkeitZeilen`, `StromMatrix`) | Zahlenprobe 293.245,6 + 22.914,0 = 316.159,6 €/a |
| 16 | U22: Entscheid, dann Überlagerung „Sätze und Herkunft" | **L** | plattformfrei | **Entscheid zuerst** (`02/d-2`) | bunit je Größe: Vorschlag · Herkunft · eigener Wert · gilt |
| 17 | U15 + ±-Knopf an drei neuen Orten | **L** | plattformfrei + Schema | Schema (Schritt ≥ 95), Kern | A/B-Nachweis je Szenario; bunit je Ort |

### 8.2 Zu berichtigende Konzeptstellen

Die 15 WinForms-Stellen aus § 3 (n-1 … n-15) sind je Zeile mit alt → neu benannt; dazu:

| Zeile | alt | neu |
|---|---|---|
| 468–472 | „Die Fußleiste von `UcWirtschaftlichkeit` ist voll — **sieben** Knöpfe" | `WirtschaftlichkeitSeite.razor:363–391` führt **fünf** Knöpfe (Photovoltaik, BHKW, Strombezug, Verlauf, Berechnen), nach U2 vier — K8 ist damit gegenstandslos (ergänzt `02/d-11` um den Namen) |
| 951–952 | „Die Hinweiszeile füllt heute nur die Windows-Hülle (`WirtschaftlichkeitSeiteGaben`)" | richtig gemessen (`:404`) — zu ergänzen: es ist der **einzige** Schreiber, und die Textbildung liegt bereits im Kern (`NutzungsdauerAbgleich.Hinweis`); zu tun ist das Einsammeln der Positionen, nicht der Text |
| 1002–1003 | „plattformfrei: Rechen- und Zeichenlogik … gehören nach `EPOS.UI.Daten`" | zu ergänzen: dort **gibt es keinen Ordner `Wirtschaftlichkeit`** — er ist mit anzulegen (heute nur `Allgemein`, `Assistent`, `Bedarf`, `Kosten`, `Projekt`, `Pufferspeicher`, `Simulation`, `Stromspeicher`) |
| 1339 | „eine plattformfreie Hülle in `EPOS.UI.Daten` gibt es nicht — auf iOS ist der Dialog nicht erreichbar" | zwei Bedingungen statt einer: zusätzlich `IProjektQuelle.BerichteKostenGaben` (`:272`, liefert `null`) und die Whitelist `AppWurzel.razor:1431–1445` |
| 2130 (K9) | „§ 6.1 zählt ‚9 Felder', real 11 · Konzeptkorrektur" | weiterhin offen, aber rein redaktionell — der Schreibweg führt die 11 Spalten (`KwkgAnlagenCtrl.cs:243–244`); K7 ist damit erfüllt und in der Tafel als solches zu kennzeichnen |
| 2151 (ET-D-3) | „genau zwei Einträge — Abrechnungseinheit und kWh, Faktor = Heizwert" | umgesetzt, **aber** der Kartenzustand fällt weiter auf `ID_Umrechnung = -1` zurück (U32) — die Zeile sollte den offenen Rest benennen |
| 4 (Kopfzeile) | „Stand 02.09.2026" | die Abschnitte § 2.13, § 2.15, § 2.16 und die Entscheide vom 18.09.2026 sind jünger — Kopfstand nachziehen |

---

## Nicht geprüft

- **Rechen- und Zeichenlogik.** Kapitalwert, Verlaufsreihen, `ChartRenderer`-Maße, Zahlenproben und
  die Nachweisumschläge wurden nur dort angefasst, wo eine Oberflächenfrage davon abhängt; die
  Zahlen selbst prüft ein anderer Agent (`01_Nachrechnung.md`).
- **Kein Bau, kein Test, kein Lauf.** Alle Aussagen sind Quelltextmessungen (`grep`/`sed`); die
  Testfallzahlen sind gezählte `[Fact]`/`[InlineData]`-Marken, kein ausgeführter Lauf. Ob die
  genannten Tests **grün** sind, ist nicht geprüft.
- **Ressourcen.** Ob die für U2, U10, U25, U27 nötigen Schlüssel in `en-US` vollständig wären,
  ist nicht geprüft; geprüft wurde nur, dass `WIRT_SZEN_HINWEIS` repoweit fehlt.
- **Die übrigen Dialogordner.** `EPOS.UI/Dialoge/` führt 17 Ordner; geprüft wurden
  `Wirtschaftlichkeit`, `Kosten` und `Admin` sowie `Seiten/Berichte`. Bedarf, Erzeuger, Strom,
  Wärmepumpe, Solarthermie, Photovoltaik, Simulation, Import, Klimadaten, Lizenz, Hilfe, Projekt
  und Allgemein blieben außen vor, soweit sie nicht als Wirt eines geprüften Dialogs auftraten.
- **`KatalogDublettenDialog` und `EinstellungenDialog`** sind in der Matrix nicht geführt — sie
  gehören zur allgemeinen Administration, nicht zum Dialograum des Wirtschaftlichkeitskonzepts.
  `04` führt sie mit.
- **Die iOS-Messung stammt aus einem Unteragenten** und wurde von mir an vier Stellen nachgeprüft
  (`AppWurzel.razor:1431–1445`, `IProjektQuelle.cs:272`, `Seitenschluessel.cs:34/37/341/344/358/364`,
  `EnergietraegerFenster.cs`); die übrigen Angaben zu `EPOS.iOS` (Adapterliste, Ablehnungstexte,
  direktes SQL in `IosProjektQuelle.cs:45–49`) sind übernommen, nicht selbst nachgelesen.
- **Die WinForms-Naht in Zeilen** ist aus Methodengrenzen abgeleitet (Anfangszeile der nächsten
  Methode als Ende), nicht Zeile für Zeile durchgezählt; die Prozentangabe in § 4.2 ist deshalb
  eine Größenordnung, keine exakte Zahl.
- **Der Prüfbereich „Mockup gegen Code" wurde nicht wiederholt.** Die 97 Abweichungen aus `03` und
  die 31 Hausstilbefunde aus `04` sind übernommen; ich habe nur dort nachgemessen, wo diese Analyse
  eine eigene Aussage brauchte (U-Stand, `WIRT_SZEN_HINWEIS`, `Zeitraumzeile`, ±-Knopf-Wirte).

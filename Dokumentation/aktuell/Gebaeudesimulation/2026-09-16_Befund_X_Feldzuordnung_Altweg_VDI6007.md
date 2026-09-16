# Befund X — Feldzuordnung Altweg / VDI 6007: welche Spalte welcher Rechenweg liest (16.09.2026)

**Anlass.** Anwenderentscheid **E20** vom 16.09.2026 („generell soll die neue (VDI 6007) und alte
Gebäudeenergiebedarfsberechnung komplett getrennt werden … Alle Dialoge und eingaben sollen daher
angepasst werden an die zukünftig alleinige VDI 6007 Struktur"),
[Konzept](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) Nachtrag N1.25 und
[`ADR-006`](../ADR-006_Trennung_Altweg_VDI6007.md). N1.25 Punkt 3 verlangt ausdrücklich diesen
Befund: „Welche Spalten das sind, weist ein eigener Befund je Feld aus (Klasse „nur Altweg",
„beide", „nur VDI")."

**Was dieser Befund tut.** Er liest Quelltext, Schema und die Papiere unter
`Dokumentation/aktuell/` und belegt jede Aussage über den **Bestand** mit `Datei:Zeile`. Er weist
je Spalte den Leser des Tagesbilanz-Wegs und den Leser des VDI-Wegs aus, zählt die Klassen, erhebt
die Aufrufstellen des Tagesbilanz-Wegs, leitet daraus ab, was der modellfreie Vorbereitungsschritt
liefern muss, schätzt die Verschiebung nach `Altweg/` und listet, was die Stufe GA entfernt.

**Was dieser Befund nicht tut.** Er entscheidet nichts: Die Vorschläge für den Dialog und die
Aufwände sind Vorschläge, die offenen Punkte in Kapitel 5 gehen an den Anwender. Er nennt keine
Zahlen aus VDI 6007 und nichts aus VDI 6020:2022 (Entscheid E6). Er öffnet die Testdatenbank nicht;
Mengenangaben dazu stammen aus [Befund D](2026-09-15_Befund_D_Testdatenbank.md). Die Leserseite des
VDI-Wegs ist **noch kein Quelltext** — sie steht in Konzept 4 und 6 und in der Eingabetabelle 1.1
der [Rechenschritte](../Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md); das ist als
Quelle je Zeile benannt.

---

## 0. Das Ergebnis in acht Sätzen

1. **Der Schnitt ist klein: von 52 Fachspalten in `Tab_Gebaeude` liest genau eine Handvoll nur der
   Altweg** — `Typ`, `Fensterflaeche_Ost_West`, `Wochenende`, `Ferien` (4 Spalten). **40 lesen
   beide Wege**, **8 liest kein Rechenweg** (Stamm-, Anzeige- und Filterdaten). Der Übergangs­abschnitt
   „Übergang: Tagesbilanz" im Dialog trägt also **vier Felder**, nicht dreißig.
2. **Der Tagesbilanz-Weg besteht aus drei Methoden in `SimulationWaermebedarf.cs` (274 Zeilen) und
   vier Funktionen in `BhkwPlan.cs` (rund 185 Zeilen).** `BhkwPlan.cs` kann **nicht als Ganzes**
   wandern: dieselbe Datei trägt die neun Vektorhelfer, die jeder Rechenzweig des Kerns ruft.
3. **Der Vorbereitungsschritt ist nicht vollständig modellfrei zu bekommen.** Die
   Verbrauchs-Rückrechnung (`Bewohner_und_Flaeche_berechnen:613`) braucht `VerbrauchAlt` aus
   **demselben** Modell, das anschließend den Bedarf rechnet (`:650`). Die Einheitenumrechnung
   (`:617-636`) ist modellfrei, die Rückrechnung selbst ist es nicht: sie muss den Jahreswert vom
   **gewählten Modul** entgegennehmen. Das ist der eine Punkt, an dem die Weiche vor dem
   Vorbereitungsschritt gelesen werden muss.
4. **Brauchwasser und Prozesswärme teilen mit dem Gebäudeweg nur drei Dinge:** den Wochentag des
   1. Januar (`WochentagJan1`, `:952`, `:1003`), die Monatsgrenzen `mo_anfang`/`mo_ende` und —
   mittelbar — die Stundentemperatur. Ihre Profile kommen aus `ProfilBedarf`, nicht aus
   Gebäudedaten; **die Bewohnerzahl liest kein einziger anderer Rechenweg.**
5. **`Anzahl_Bewohner` (`:11`, gesetzt `:577`) und `Wohnflaeche` (`:12`, gesetzt `:578`) sind tot** —
   im ganzen Bestand gibt es keinen Leser. Wie `MaxP` (`:56`, geschrieben `:212`, nie gelesen).
6. **Ein neuer Bestandsbefund für die Stufe GB:** In der Jahresschleife von
   `Berechnung_Gebaeude_Tageswerte` wird `Ferien_Absenkung` **nicht nachgeführt** (`:845-850`
   setzen nur `WE_Absenkung`). Der Wert stammt aus dem letzten Vorlauftag (`:781`, Tag 364) und gilt
   dann für alle 365 Tage. Die Ferienabsenkung ist damit im Bestand entweder ganzjährig an oder
   ganzjährig aus — unabhängig vom eingetragenen Fahrplan.
7. **Der teuerste Teil der Verschiebung ist nicht das Verschieben, sondern der Zustand.**
   `BhkwPlan._prevRoomTemp` ist `static` (`:51`) und trägt die Raumtemperatur über Stunden, Tage,
   **beide Aufrufe je Gebäude** und **alle Gebäude eines Projekts** hinweg. Wer die Reihenfolge der
   Aufrufe auch nur um einen Aufruf ändert, ändert das Ergebnis. E20 verlangt deshalb zu Recht:
   **GB vor der Verschiebung.**
8. **Schätzung: 3,0–5,0 PT für die Verschiebung samt Fassade, Vorbereitungsschritt und
   byte-gleichem Nachweis** — die Spanne aus ADR-006 trägt. Die Stufe **GA** entfernt danach
   4 Spalten je Gebäudetabelle, 7 Methoden, den Schalter „Rechenweg", den Übergangsabschnitt, den
   Vergleich alt/neu und den Rückweg-Test.

---

## 1. Feldzuordnung

### 1.1 Wie die Tabelle zu lesen ist

- **Leser Tagesbilanz** ist eine Stelle im heutigen Quelltext, an der die Größe **in die Rechnung**
  eingeht. Kurze Pfade: `SWB` = `EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs`,
  `BP` = `EPOS.Kern/Allgemein/BhkwPlan.cs`. Reines Lesen aus der Datenbank
  (`ProjektGebaeudeCtrl.ReadAll`, `GebaeudeCtrl.MapRowToModel`) und reines Anzeigen zählen **nicht**
  als Leser.
- **Leser VDI 6007** ist die Zeile der Eingabetabelle
  [Rechenschritte 1.1](../Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) (dort `R1.1`) bzw.
  der Abschnitt des Konzepts, der die Größe verlangt. Es gibt dafür noch keinen Quelltext.
- **Klasse** ist die Klasse des **Rechenlesers**, nicht die des Dialogfelds: „nur Altweg", „beide",
  „nur VDI (neu)", „kein Rechenleser".
- **Dialog** ist der Vorschlag für die VDI-Struktur nach E20: `Hauptstruktur` = sichtbar und
  bearbeitbar wie bisher; `Übergang` = eingeklappter Abschnitt „Übergang: Tagesbilanz", nur bei
  einem Gebäude auf dem Altweg, entfällt mit GA; `Skalierung (E8)` = Skalierungsdialog (bisher
  `GebaeudeWohnflaecheDialog`); `Stammdaten` = Kopf-, Filter- oder Anzeigefeld ohne Rechenwirkung.

Die Spaltennamen sind die des Schemas (`sql/schema/001_grundschema.sql`, `Tab_Gebaeude` ab `:1131`,
`Tab_Gebaeude_STAMM` ab `:1190`). Beide Tabellen führen **dieselben 52 Fachspalten**; `Tab_Gebaeude`
trägt zusätzlich `Gebaeudename`, `ID_ProjektGebaeude`, `ID_Projekt`, `Tab_Gebaeude_STAMM` statt
dessen `Bezeichner` und `ReadOnly`. Die Sicht `Abfrage_Projektgebaeude` (`002_views.sql:89-91`)
liefert 58 Spalten; `ProjektGebaeudeCtrl.ReadAll` liest sie über **Index** `row[0…57]`
(`EPOS.Kern/Controller/ProjektGebaeudeCtrl.cs:41-102`) — der Namensleser kommt mit M3
(Konzept 6.2).

### 1.2 `Tab_Gebaeude` / `Tab_Gebaeude_STAMM` — 52 Fachspalten

| Spalte | Dialogfeld heute | Leser Tagesbilanz | Leser VDI 6007 | Klasse | Dialog nach E20 |
|---|---|---|---|---|---|
| `Gebaeudename` / `Bezeichner` | „Name" (`GebaeudeKatalogDialog.razor:98-105`) | — (nur Anzeige, `GebaeudeBedarfCtrl.cs:123`) | — | kein Rechenleser | Hauptstruktur (Stammdaten) |
| `Typ` | „Gebäudetyp" (`:107`) | `SWB:584` (`DBTagesVeteilung`), `SWB:601` (Wahl `TagTyp_W`/`TagTyp_NW`) | — (R1.1 führt ihn nicht) | **nur Altweg** | **Übergang** (Vorschlag), siehe 5.2 |
| `Beschreibung` | „Beschreibung" (`:109`) | — | — | kein Rechenleser | Hauptstruktur (Stammdaten) |
| `Wohnflaeche_gesamt` | „Wohn-/Nutzfläche" (`:123`) | `SWB:641`, `:645`, `:648` (Bewohner, `FlaecheAlt`) | R1.1 „Katalogfläche (Basis der Rückrechnung)" | beide | Hauptstruktur |
| `Bewohner` | — (Hülle rechnet) | **Ausgabe**: `SWB:571`, `:641`, `:653` | Ausgabe des Vorbereitungsschritts | beide (Ausgabe) | entfällt als Feld, bleibt Ausgabe |
| `Flaeche_Nutzer` | „Fläche / Nutzer" (`:126`) | `SWB:571`, `:641`, `:653` | R1.1 „Fläche je Person" | beide | Hauptstruktur |
| `Interne_Waermegewinne` | „Interne Gewinne" (`:129`) | `SWB:790`, `:806`, `:860`, `:876` → `BP.TaeglHeizlastWG` | R1.1 Φ_int | beide | Hauptstruktur |
| `Bauweise` | „Bauart" (`:119`, abgeleitet) | `SWB:793`, `:809`, `:863`, `:879` (C des RC-Modells) | R1.1 C_ges | beide | Hauptstruktur |
| `Fensterflaeche_Sued` | „Fensterfläche Süd" (`:154`) | `SWB:752`, `:756`, `:822`, `:826` → `BP.SolareGewinneC` | R1.1 A_w,S | beide | Hauptstruktur |
| `Fensterflaeche_Ost_West` | „Fensterfläche Ost/West" (`:157`) | dieselben vier Stellen (Feldname `Fensterflaeche_Ost`, `ProjektGebaeudeCtrl.cs:56`) | nur als **NULL-Vorgabe** für `Fensterflaeche_Ost`/`_West` (Konzept 6.1) | **nur Altweg** | **Übergang** (schreibgesperrt, Summe aus Ost + West) |
| `Fensterflaeche_Nord` | „Fensterfläche Nord" (`:151`) | `SWB:751`, `:755`, `:821`, `:825` | R1.1 A_w,N | beide | Hauptstruktur |
| `Fensterdurchlassgrad` | „Fensterdurchlaßgrad" (`:132`) | dieselben vier Stellen | R1.1 g | beide | Hauptstruktur |
| `Raumsolltemperatur_Nachtabsenkung` | „Nachtabsenkung" (`:221`) | `SWB:789`, `:805`, `:859`, `:875`; zugleich Startwert `BP:398` | R1.1 θ_soll,Nacht | beide | Hauptstruktur |
| `Raumsolltemperatur_Tag` | „Solltemperatur Tag" (`:218`) | `SWB:788`, `:804`, `:858`, `:874` | R1.1 θ_soll,Tag | beide | Hauptstruktur |
| `Raumsolltemperatur_Wochenende` | „WE-Absenkung" (`:227`) | `SWB:777`, `:785`, `:846`, `:855` (wirksam nur bei Wert > 5) | R1.1 θ_soll,WE | beide | Hauptstruktur |
| `Raumsolltemperatur_Ferien` | „Solltemperatur Ferien" (`:230`) | `SWB:689` (setzt `Ferien = 0`), `:787`, `:803`, `:857`, `:873` | R1.1 θ_soll,Fer | beide | Hauptstruktur |
| `Maximaleraumtemperatur` | „Maximaltemperatur" (`:224`) | `SWB:795`, `:811`, `:865`, `:881`; Kappung `BP:430` | R1.1 θ_max; Quelle der Kühllast (Konzept 4.5) | beide | Hauptstruktur |
| `k_Wert_Außenwand` | „U Außenwand" (`:185`) | `SWB:760`, `:768`, `:829`, `:837` → `BP.SpezWaermeverlusteC` | R1.1 U_AW | beide | Hauptstruktur (U·A-Tabelle) |
| `k_Wert_Fenster` | „U Fenster" (`:188`) | dieselben vier Stellen | R1.1 U_w | beide | Hauptstruktur (U·A-Tabelle) |
| `k_Wert_Dachflaeche` | „U Dach" (`:191`) | dieselben vier Stellen | R1.1 U_D | beide | Hauptstruktur (U·A-Tabelle) |
| `k_Wert_Grundflaeche` | „U Grundfläche" (`:194`) | dieselben vier Stellen | R1.1 U_G | beide | Hauptstruktur (U·A-Tabelle) |
| `k_Wert_Sonstiges` | „U Sonstiges" (`:197`) | dieselben vier Stellen | R1.1 U_So | beide | Hauptstruktur (U·A-Tabelle) |
| `Flaeche_Außenwand` | „Fläche Außenwand" (`:160`) | dieselben vier Stellen | R1.1 A_AW | beide | Hauptstruktur (U·A-Tabelle) |
| `gesamte_Fensterflaeche` | — (Hülle rechnet) | dieselben vier Stellen | R1.1 A_w | beide | Hauptstruktur (gerechnet) |
| `Dachflaeche` | „Dachfläche" (`:164`) | dieselben vier Stellen | R1.1 A_D | beide | Hauptstruktur (U·A-Tabelle) |
| `Grundflaeche` | „Grundfläche" (`:167`) | dieselben vier Stellen | R1.1 A_G | beide | Hauptstruktur (U·A-Tabelle) |
| `Sonstige_Flaechen` | „Sonstige Flächen" (`:170`) | dieselben vier Stellen | R1.1 A_So | beide | Hauptstruktur (U·A-Tabelle) |
| `Wohnflaeche` → `Nutzflaeche` (E19) | — (Hülle rechnet) | `SWB:765`, `:773`, `:834`, `:842` (Lüftung) und `:797`, `:813`, `:867`, `:883` (Nenner der Skalierung `BP:435`) | R1.1 A_f | beide | Hauptstruktur |
| `Raumhoehe` | „Raumhöhe" (`:137`) | `SWB:766`, `:774`, `:835`, `:843` | R1.1 H | beide | Hauptstruktur |
| `WBVK_Anschluß_Fenster_Wand` | „ψ Fenster-Wand" (`:244`) | dieselben `SpezWaermeverlusteC`-Stellen | R1.1 ψ_k | beide | Hauptstruktur (U·A-Tabelle) |
| `WBVK_Anschluß_Wand_Dach` | „ψ Wand-Dach" (`:248`) | dieselben Stellen | R1.1 ψ_k | beide | Hauptstruktur (U·A-Tabelle) |
| `WBVK_Anschluß_Außenwand_Kellerdecke` | „ψ AW-Keller" (`:246`) | dieselben Stellen | R1.1 ψ_k | beide | Hauptstruktur (U·A-Tabelle) |
| `Abmessung_Anschluß_Fenster_Wand` | „Länge Fenster-Wand" (`:261`) | dieselben Stellen | R1.1 L_k | beide | Hauptstruktur (U·A-Tabelle) |
| `Abmessung_Anschluß_Wand_Dach` | „Länge Wand-Dach" (`:264`) | dieselben Stellen | R1.1 L_k | beide | Hauptstruktur (U·A-Tabelle) |
| `Abmessung_Anschluß_Außenwand_Kellerdecke` | „Länge AW-Keller" (`:266`) | dieselben Stellen | R1.1 L_k | beide | Hauptstruktur (U·A-Tabelle) |
| `Luftwechselrate` | „Luftwechselrate" (`:322`) | `SWB:766`, `:774`, `:835`, `:843` | R1.1 n (in G2 daneben n_inf und n_nutz) | beide | Hauptstruktur |
| `Wochenende` | — (Hülle setzt 0/1, `GebaeudeKatalogDialog.razor:809`) | — **kein Leser im Bestand**, siehe 5.1 | — (R1.1 nennt sie ausdrücklich nicht) | **nur Altweg** (Bestandsflag) | **Übergang** |
| `Ferien` | — (Hülle setzt 0/1) | `SWB:691` (geschrieben), `:699` (`> 0,9` schaltet den Fahrplan) | — (VDI liest die Zeiträume; 0/366 heißt „aus", R1.1) | **nur Altweg** | **Übergang** |
| `Ferienbeginn_1…4` | „Ferienbeginn" (`:283-286`) | `SWB:701`, `:713`, `:721`, `:728` | R1.1 „Ferienzeiträume" | beide | Hauptstruktur |
| `Ferienende_1…4` | „Ferienende" (`:304-307`) | `SWB:706`, `:714`, `:721`, `:728` | R1.1 „Ferienzeiträume" | beide | Hauptstruktur |
| `WW_Bedarf` | — (Hülle setzt 0, `GebaeudeKatalogDaten.cs:144-148`) | — | — | kein Rechenleser | entfällt (Vorschlag, 5.3) |
| `spez_Waermeverbrauch` | — (`GebaeudeKatalogHuelle.cs:92`: „keine der beiden Masken je anfasst") | — | — (R1.1: „reine Katalogkennzahl, kein Rechnungseingang"), geht in das Abnahmekriterium Konzept 10.4 (4) | kein Rechenleser | bleibt (Abnahme) |
| `Waermebedarf` | — (`GebaeudeKatalogHuelle.cs:92`) | — | — | kein Rechenleser | entfällt (Vorschlag, 5.3) |
| `Baualtersklasse` | „Baujahr (Klasse)" (`:114`) | — (Katalogfilter, `GebaeudeStammCtrl.cs:223-229`) | — | kein Rechenleser | Hauptstruktur (Stammdaten) |
| `Gebaeudeart` | „Gebäudeart" (`:112`) | — (Katalogfilter, `:226-229`) | — | kein Rechenleser | Hauptstruktur (Stammdaten) |
| `Wohngebaeude_Nicht_Wohngebaeude` | „Verwendung" (`:117`) | — (Katalogfilter, `GebaeudeStammCtrl.cs:83-84`) | — | kein Rechenleser | Hauptstruktur (Stammdaten) |

### 1.3 `Z_ProjektGebaeude` — die Skalierungsspalten (E8)

| Spalte | Dialogfeld heute | Leser Tagesbilanz | Leser VDI 6007 | Klasse | Dialog nach E20 |
|---|---|---|---|---|---|
| `Wohnflaeche_Waermebedarf` (→ `Z_AuswahlWohnflaeche`) | „Wärmebedarf/Wohnfläche" (`GebaeudeWohnflaecheDialog.razor:75-77`) | `SWB:571`, `:578`, `:619-635`, `:645`, `:652`, `:796`, `:812`, `:866`, `:882` (Zähler der Skalierung `BP:435`) | R1.1 A_proj; Rechenschritte 8.3 | beide | **Skalierung (E8)** |
| `Einheit_Waermebedarf_Wohnflaeche` | „Art der Angabe" (`:73`) | `SWB:569`, `:617-638` | R1.1 „Einheit der Verbrauchseingabe"; Rechenschritte 8.3 Schritt 1 | beide | **Skalierung (E8)** |
| `Jahresnutzungsgrad` | „Jahresnutzungsgrad" (`:81-83`) | `SWB:619`, `:623`, `:627`, `:631` | R1.1 η; Rechenschritte 8.3 Schritt 1 | beide | **Skalierung (E8)** |
| `dezWarmwasserbereitung` | „dezentrale Warmwasserbereitung" (`:85`) | — **kein Leser im Rechenkern** | — | kein Rechenleser | **Skalierung (E8)**, unverändert |

### 1.4 Die neuen Spalten des VDI-Wegs

Quelle: [Konzept](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 6.1 und Rechenschritte 1.1.
Der Gebäudespalten-Schritt (Papiername **M3**) legt sie an; **der Altweg liest keine davon**
(Konzept 6.1: „Der Bestandsweg liest keine dieser Spalten").

| Spalte | Klasse | Bemerkung |
|---|---|---|
| `Gebaeude_Modell` | **Weiche** | Schalter „Rechenweg"; NULL = VDI 6007 (E1, Konzept N1.1). Gelesen allein von der Weiche und der Anzeige. **Entfällt mit GA** |
| `Fensterflaeche_Ost` | nur VDI (neu) | NULL = ½ `Fensterflaeche_Ost_West` |
| `Fensterflaeche_West` | nur VDI (neu) | NULL = ½ `Fensterflaeche_Ost_West` |
| `Rahmenanteil` | nur VDI (neu) | NULL = 0,3 |
| `Verschattungsfaktor` | nur VDI (neu) | NULL = 0,9 |
| `Grundflaeche_Randbedingung` | nur VDI (neu) | `ERDREICH` / `KELLER` / `AUSSENLUFT`; Spalte der U·A-Tabelle (Umsetzungskonzept 1.10) |
| `Kellertemperatur` | nur VDI (neu) | NULL = 10 °C, nur bei `KELLER` — **steht in Rechenschritte 1.1 und Umsetzungskonzept 1.8/2.4, fehlt in Konzept 6.1** (5.4) |
| `Masseanteil_Aussen` | nur VDI (neu) | NULL = 0,3 |
| `Innenflaechenfaktor` | nur VDI (neu) | NULL = 2,5 |
| `Heizung_Strahlungsanteil` | nur VDI (neu) | NULL = 0,3 |
| `Heizleistung_Max` | nur VDI (neu) | kW; NULL = unbegrenzt |
| `Aussenbauteile_Strahlung` | nur VDI (neu) | 0/1, `NOT NULL DEFAULT 0` |
| `Luftwechsel_Infiltration` | nur VDI (neu, G2) | NULL = 0,3 |
| `Luftwechsel_Nutzer` | nur VDI (neu, G2) | NULL = 0,4 |
| `Sommerlueftung` | nur VDI (neu, G2) | 0/1 |

Nach E20 sind diese Felder **immer sichtbar und bearbeitbar**, auch bei einem Gebäude auf dem
Altweg — sie gelten dann nach der Umstellung (N1.25 Punkt 2). Damit ist Frage **U2** des
Umsetzungskonzepts („verstecken oder gesperrt zeigen") überholt.

### 1.5 Zählung je Klasse

| Klasse | Zahl | Spalten |
|---|---|---|
| **nur Altweg** | **4** | `Typ`, `Fensterflaeche_Ost_West`, `Wochenende`, `Ferien` |
| **beide** | **40** | die 40 Fach- und Flächengrößen der Tabelle 1.2, davon `Bewohner` und `gesamte_Fensterflaeche` als **Ausgabe**- bzw. Rechenfelder |
| **kein Rechenleser** | **8** | `Gebaeudename`/`Bezeichner`, `Beschreibung`, `WW_Bedarf`, `spez_Waermeverbrauch`, `Waermebedarf`, `Baualtersklasse`, `Gebaeudeart`, `Wohngebaeude_Nicht_Wohngebaeude` |
| **Summe `Tab_Gebaeude`** | **52** | Fachspalten ohne `ID`, `ID_ProjektGebaeude`, `ID_Projekt` |
| `Z_ProjektGebaeude` | 3 „beide" + 1 „kein Rechenleser" | Skalierung nach E8 |
| **nur VDI (neu)** | **14** | 11 aus M3 + 3 aus G2; dazu `Gebaeude_Modell` als **Weiche** |

**Was daraus für den Dialog folgt.** Der eingeklappte Abschnitt „Übergang: Tagesbilanz" trägt
**vier Felder**, und zwei davon (`Wochenende`, `Ferien`) sind abgeleitete Flags, die der Dialog
heute selbst setzt und nicht anzeigt — der sichtbare Teil des Abschnitts sind faktisch
**`Typ` und `Fensterflaeche_Ost_West`**. Der Übergangsabschnitt ist damit deutlich kleiner als die
sieben Felder, um die Frage U2 gestritten hat.

---

## 2. Die Aufrufstellen des Tagesbilanz-Wegs

### 2.1 Woraus der Weg besteht

| Ort | Zeilen | Was darin geschieht | Klasse |
|---|---|---|---|
| `SWB.HeizwaermeEinesGebaeudes` | `:566-611` (46) | Einheitenzweig (`:569-576`), Aufruf des Tagesmodells (`:581`), Tagesverteilung (`:584`), Abbruch (`:590-595`), `VectorInit` + `StdWerte` (`:597-608`) | **Fassade in spe** — teils modellfrei, teils Altweg |
| `SWB.Bewohner_und_Flaeche_berechnen` | `:613-656` (44) | `VerbrauchNeu` je Einheit (`:617-636`, modellfrei), Bewohner (`:641`, `:653`), Rückrechnung über `VerbrauchAlt` (`:647-652`, **modellabhängig**) | gemischt |
| `SWB.DBTagesVeteilung` | `:658-682` (25) | liest `Abfrage_Tagverteilung` je `Typ` und `ID_Gebaeude` | **nur Altweg** |
| `SWB.Berechnung_Gebaeude_Tageswerte` | `:684-888` (205) | Ferienmaske (`:686-733`), Vorlauf 15 Tage (`:748-814`), Jahresschleife (`:818-886`), `HeizwaermebedarfGeb[GebaeudeNr]` | **nur Altweg** |
| `BP.StdWerte` | `:259-310` (52) | Tageslast × Tagesgang → 8 760 Watt-Werte | **nur Altweg** |
| `BP.SolareGewinneC` | `:311-341` (31) | isotrope Tagesgewinne aus `Sol_*` und drei Fensterflächen; Rückgabe **× 100** | **nur Altweg** |
| `BP.SpezWaermeverlusteC` | `:342-362` (21) | Transmission + Brücken + Lüftung; Rückgabe **× 100** (`:361`) | **nur Altweg** |
| `BP.TaeglHeizlastWG` | `:364-436` (73 mit Kopf) | 24-Stunden-Kapazitätsmodell; **Skalierung E8 im Rückgabewert** (`:435`) | **nur Altweg** |
| `BP._prevRoomTemp`, `BP.ResetState` | `:51`, `:54` | statische Vortemperatur | **nur Altweg**, aber `static` |
| Felder in `SWB` | `:17-22`, `:24-25`, `:27`, `:31`, `:52-57` | `Sol_N/w/O/S`, `A_Temp`, `WE`, `TagTyp_W/NW`, `Solare_Gewinne`, `SpezWaermeverluste`, `Heizlast`, `TagesVerteilung`, `MaxP`, `F_Absenkung`, `HeizwaermebedarfGeb` | überwiegend Altweg, `WE` beide |

`SWB.KlimakalenderLesen` (`:513-537`) und `SWB.Stundentemperatur_aus_DB` (`:912-920`) sind
**modellfrei** und gehören in den Vorbereitungsschritt, nicht in den Altweg.

### 2.2 Wer den Weg ruft

| Aufrufer | Stelle | Was er will |
|---|---|---|
| **Lauf** — `SimulationRunner.Simuliere_Intern` | `EPOS.Kern/Allgemein/Simulation/SimulationRunner.cs:184` | `Waermebedarf_berechnen(idProjekt, idKlimaregion)` |
| **Lauf** — `SimulationLaufCtrl.Bedarf` | `EPOS.Kern/Controller/SimulationLaufCtrl.cs:120` | dasselbe |
| **Windows-Schale** — `StartseiteHuelle` | `WindowsFormsApplication1/Views/Hauptformular/StartseiteHuelle.cs:465` | dasselbe über `BedarfsZustand.Waerme` (`BedarfsZustand.cs:43`) |
| **Auskunft je Gebäude** — `GebaeudeBedarfCtrl.Rechnen` | `EPOS.Kern/Controller/GebaeudeBedarfCtrl.cs:111-133` | ruft **dieselben zwei Methoden** wie der Lauf: `KlimakalenderLesen` (`:112`), `HeizwaermeEinesGebaeudes` (`:117`), danach `WattToKw` (`:121`) und Summe/Höchstwert/Monate (`:130-133`) |
| **Bedarfsdialog** (Gebäudedialog, Anwenderwunsch W9-E-2) | `EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor:238-248` → `GebaeudeBedarfDialog` | über `GebaeudeBedarfCtrl` |
| **Vorschau Brauchwasser/Prozess** — `BedarfsVorschauCtrl` | `EPOS.Kern/Controller/BedarfsVorschauCtrl.cs:112`, `:149` | erzeugt eine `SimulationWaermebedarf`-Instanz, ruft aber **nur** `Prozesswaerme_berechnen`/`Brauchwasserwaerme_berechnen` — **nicht den Gebäudeweg** |
| **Stromspeicher-Hülle** | `EPOS.UI.Daten/Stromspeicher/StromspeicherAuslegungHuelle.cs:45` | hält eine Instanz, rechnet den Gebäudeweg nicht |
| **Referenzlauf** | `Referenzlauf/Ergebnisexport.cs:59` | schreibt `waermebedarf_gebaeude.csv` aus `wb.Waermebedarf_Gebaeude` (**in Watt**, Umsetzungskonzept 1.8) |
| **Assistent** | `EPOS.Kern/Controller/AssistentCtrl.cs:490-516` | liest nur die vier Zuordnungsspalten aus `Z_ProjektGebaeude` und `Gebaeudename`/`Gebaeudeart`/`Beschreibung`; **rechnet nicht** |

**Bericht und Wirtschaftlichkeit rufen den Gebäudeweg nicht.** Unter `EPOS.Kern/Bericht/` gibt es
keine Fundstelle zu `Tab_Gebaeude`, `ProjektGebaeudeCtrl` oder `GebaeudeCtrl`; der Bericht arbeitet
auf den Kanälen und den Ergebnistabellen.

### 2.3 Was andere Bedarfsarten mit dem Gebäudeweg teilen

| Größe | Wer sie setzt | Wer sie sonst liest | Bewertung |
|---|---|---|---|
| `WochentagJan1` | `KlimakalenderLesen:536` aus `Tab_Klimadaten.WE` | `Prozesswaerme_berechnen:952`, `Brauchwasserwaerme_berechnen:1003` | **geteilt** — gehört in den Vorbereitungsschritt |
| `mo_anfang` / `mo_ende` | Erbauer `SWB:110-114` (`Init.Monatswerte_berechnen`) | Monatssummen aller Kanäle, `GebaeudeBedarfCtrl.cs:133` | **geteilt**, modellfrei |
| `Stundentemperatur[8760]` | `Stundentemperatur_aus_DB:912` über `SolardatenCtrl.ReadOrtszeit` | Wärmepumpe (COP), Erdreichrechnung, Reporting | **geteilt**, modellfrei |
| `WE[365]` | `KlimakalenderLesen:523` | Altweg (`:779`, `:848`); VDI-Weg als Wochenendmaske (R1.2, Frage U7) | **geteilt** |
| `Sol_N/w/O/S[365]`, `A_Temp[365]` | `KlimakalenderLesen:517-521` | nur der Altweg (R1.2: „Die isotropen Tagesmittel … gehören dem Tagesbilanz-Weg") | **nur Altweg** |
| `TagTyp_W[365]`, `TagTyp_NW[365]` | `KlimakalenderLesen:524-525` | nur `BP.StdWerte` (`:604`, `:608`) | **nur Altweg** |
| `Anzahl_Bewohner`, `Wohnflaeche` (Felder von `SWB`) | `:577`, `:578` | **niemand** (Volltextsuche über Kern, Oberfläche, Hüllen und Schale) | **tot** |
| Bewohnerzahl je Gebäude (`item.Bewohner`) | `:571`, `:641`, `:653` | zurück in `Tab_Gebaeude.Bewohner` über die Hüllen; **kein Rechenweg** | kein geteilter Rechenweg |
| Brauchwasser- und Prozessprofile | `ProfilBedarf.Rechnen` (`:955`, `:1006`) | eigene Kataloge und Profile | **unabhängig** vom Gebäude |

**Das Ergebnis dieses Abschnitts in einem Satz:** Außer Klima und Kalender teilt der Gebäudeweg
**nichts** mit Brauchwasser, Prozesswärme, Wirtschaftlichkeit oder Bericht — die Trennung nach E20
schneidet also nur den Gebäudeweg auf, nicht den Lauf.

### 2.4 Was der modellfreie Vorbereitungsschritt liefern muss

**Vor der Weiche, einmal je Lauf:**

1. **Klimakalender** aus `Tab_Klimadaten` und `Tab_Solar`: `Sol_N/W/O/S[365]`, `A_Temp[365]`,
   `WE[365]`, `TagTyp_W[365]`, `TagTyp_NW[365]`, `Stundentemperatur[8760]`, `WochentagJan1`
   (heute `KlimakalenderLesen:513-537` und `Stundentemperatur_aus_DB:912-920`, Anweisung für
   Anweisung).
   *Anmerkung:* Die vier `Sol_*`-Reihen, `A_Temp` und die beiden `TagTyp_*` braucht nur der Altweg
   (2.3). Der Wertträger führt sie trotzdem, weil `KlimadatenCtrl.ReadAll` **eine** Abfrage ist und
   die Aufteilung der Füllschleife byte-gleich bleiben muss.
2. **Monatsgrenzen** `mo_anfang`/`mo_ende`.

**Vor der Weiche, je Gebäude:**

3. **Bewohner aus der Nutzfläche:** `item.Bewohner = Z_AuswahlWohnflaeche / Flaeche_Nutzer`
   bei Einheit „Wohnfläche [m²]" (`:571`), sonst `Wohnflaeche_gesamt / Flaeche_Nutzer` (`:641`)
   bzw. nach der Rückrechnung `Z_AuswahlWohnflaeche / Flaeche_Nutzer` (`:653`).
4. **`VerbrauchNeu` je Einheit** [kWh] nach `:617-636` — reine Einheitenumrechnung mit
   `Jahresnutzungsgrad`, **ohne jedes Modell**.
5. **Die beiden Flächen der Skalierung nach E8:** `Z_AuswahlWohnflaeche` (Projektfläche) und
   `Wohnflaeche`/`Nutzflaeche` (Katalogfläche). **Nicht den Faktor selbst anwenden** — im Altweg
   steckt er im Rückgabewert von `BP.TaeglHeizlastWG` (`:435`), im VDI-Weg ist er eine
   Nachmultiplikation (Rechenschritte 8.3). Wer ihn im Vorbereitungsschritt anwendet, skaliert den
   Altweg zweimal.

**Was der Vorbereitungsschritt NICHT modellfrei liefern kann:**

6. **`VerbrauchAlt`** (`:650`) ist der Jahreswert **des gewählten Modells**. Die Rückrechnung
   `FlaecheNeu = VerbrauchNeu / VerbrauchAlt × FlaecheAlt` (`:651`) ist damit ein Schritt **hinter**
   der Weiche, nicht davor. Vorschlag: Der Vorbereitungsschritt liefert `VerbrauchNeu` und
   `FlaecheAlt`; die Fassade ruft das gewählte Modul ein erstes Mal für `VerbrauchAlt`, setzt
   `Z_AuswahlWohnflaeche = FlaecheNeu` und ruft es ein zweites Mal für die Reihe — **genau die
   Reihenfolge des Bestands** (`:645` → `:647` → `:652` → `:581`). Damit fällt der zweite
   Verzweigungspunkt (`:647`) weg, ohne dass die Rechenfolge sich ändert: Es ist dasselbe Modul,
   zweimal gerufen.

---

## 3. Die Verschiebung nach `Altweg/`

### 3.1 Was Zeichen für Zeichen wandert

Zielordner `EPOS.Kern/Allgemein/Simulation/Altweg/`:

| Neue Datei (Vorschlag) | Inhalt | Herkunft | Zeilen |
|---|---|---|---|
| `Altweg/TagesbilanzRechenweg.cs` | `Berechnung_Gebaeude_Tageswerte`, `DBTagesVeteilung`, der `StdWerte`-Zweig aus `:597-608`, die Altweg-Felder (`Sol_*`, `A_Temp`, `TagTyp_*`, `Solare_Gewinne`, `SpezWaermeverluste`, `Heizlast`, `TagesVerteilung`, `F_Absenkung`, `HeizwaermebedarfGeb`) | `SWB:658-682`, `:684-888`, `:597-608`, Felder `:17-22`, `:24-27`, `:31`, `:52-57` | ≈ 250 |
| `Altweg/TagesbilanzPhysik.cs` | `StdWerte`, `SolareGewinneC`, `SpezWaermeverlusteC`, `TaeglHeizlastWG`, die Vortemperatur und `ResetState` | `BP:51`, `:54`, `:259-310`, `:311-341`, `:342-362`, `:364-436` | ≈ 185 |

**`BhkwPlan.cs` kann nicht als Ganzes wandern.** Dieselbe Datei trägt `VectorInit`, `WattToKw`,
`VectorenAddieren`, `VectorSumme`, `Normieren`, `NetzverlusteC`, `MonatsSumme`, `MonatsGrenzen`,
`Heapsort` und die beiden `StromWocheToJahr` — sie werden von jedem Bedarfs- und Erzeugerzweig
gerufen. Es wandern **allein die vier Physikfunktionen** samt Zustand; die Vektorhelfer bleiben.

**Was bleibt, wird Fassade:** `Waermebedarf_berechnen` (`:128-437`), `KlimakalenderLesen`,
`Stundentemperatur_aus_DB`, der Kanalteil, die Energieprobe, die Ganglinien, Brauchwasser und
Prozess — alles unberührt.

### 3.2 Welche Signaturen die Fassade braucht

Drei Signaturen dürfen sich **nicht** ändern, weil `GebaeudeBedarfCtrl` sie ruft
(`GebaeudeBedarfCtrl.cs:111-117`) und das der Nachweis dafür ist, dass Auskunft und Lauf dieselbe
Zahl liefern:

```
public  void Waermebedarf_berechnen(int ID_Projekt, int ID_Klimaregion)
internal void KlimakalenderLesen(int ID_Klimaregion)
internal bool HeizwaermeEinesGebaeudes(ProjektGebaeudeModel item, int index, double[] ziel)
```

Vorschlag für das Innere (Namen sind Vorschläge):

```
// modellfrei, vor der Weiche
internal sealed class Klimakalender { Sol_N/W/O/S[365]; A_Temp[365]; WE[365];
                                      TagTyp_W[365]; TagTyp_NW[365];
                                      Stundentemperatur[8760]; int WochentagJan1; }
internal static Klimakalender Gebaeudevorbereitung.KalenderLesen(int idKlimaregion);
internal static double        Gebaeudevorbereitung.VerbrauchNeuKwh(ProjektGebaeudeModel item);
internal static void          Gebaeudevorbereitung.BewohnerAusNutzflaeche(ProjektGebaeudeModel item);

// die Weiche
internal interface IGebaeudeRechenweg
{
    bool Rechnen(ProjektGebaeudeModel item, int index, double[] zielWatt, Klimakalender kalender);
    double JahreswertKwh { get; }   // das VerbrauchAlt der Rueckrechnung
}
internal sealed class Altweg.TagesbilanzRechenweg   : IGebaeudeRechenweg
internal sealed class Gebaeude.Vdi6007Rechenweg     : IGebaeudeRechenweg
```

`HeizwaermeEinesGebaeudes` wird damit zu rund zwanzig Zeilen: Weiche lesen, Vorbereitung rufen,
gegebenenfalls Rückrechnung (zwei Aufrufe desselben Moduls), Modul rufen, Rückgabewert
durchreichen.

### 3.3 Umfang und Aufwand

| Teil | Inhalt | Aufwand |
|---|---|---|
| X-a | Ordner `Altweg/`, `TagesbilanzRechenweg.cs`: drei Methoden und die Altweg-Felder verschieben, Sichtbarkeiten, Namensraum | 0,5–0,8 PT |
| X-b | `TagesbilanzPhysik.cs`: vier Funktionen aus `BhkwPlan.cs` heraus, Vortemperatur mit; `BhkwPlanRueckgabeTests` nachziehen (der Wächter sucht die drei Funktionen über `typeof(BhkwPlan).GetMethod(name)`, `BhkwPlanRueckgabeTests.cs:136-141`) | 0,5–0,8 PT |
| X-c | Fassade, Weiche, `Klimakalender`-Wertträger, `Gebaeudevorbereitung`, `IGebaeudeRechenweg` | 1,0–1,5 PT |
| X-d | `GebaeudeBedarfCtrl` auf die Fassade ziehen; `SimulationRunner`, `SimulationLaufCtrl`, `StartseiteHuelle` bleiben unberührt | 0,3–0,5 PT |
| X-e | **Byte-gleicher Referenzlauf** (CI: 1030, 1007, 1017, 1045, 1046; lokal alle dreizehn), Vergleich, Protokoll | 0,5–1,0 PT |
| X-f | Wache „der VDI-Weg referenziert nichts aus `Altweg/`" (4.2) | 0,2–0,4 PT |
| | **Summe** | **3,0–5,0 PT** |

Das deckt sich mit der Spanne aus [`ADR-006`](../ADR-006_Trennung_Altweg_VDI6007.md) und
Konzept N1.25 („rund 3–5 PT").

### 3.4 Risiken

1. **Statischer Zustand über Aufrufgrenzen.** `BP._prevRoomTemp` (`:51`) wird bei `day == 1` mit
   `raumsolltempNacht` vorbelegt (`:398`) und sonst fortgeschrieben (`:433`). Er trägt über
   **Stunden, Tage, beide Aufrufe je Gebäude und alle Gebäude eines Projekts** hinweg. Zwei Folgen:
   (a) das Ergebnis hängt an der **Zeilenreihenfolge** der Gebäude (bekannter Befund, Stufe GB,
   Umsetzungskonzept 1.8); (b) ein Gebäude mit Verbrauchseingabe durchläuft das Modell **zweimal**
   (`:647`, dann `:581`), ein Gebäude mit Flächeneingabe **einmal** — die Verschiebung darf diese
   Zahl nicht um einen Aufruf verändern, sonst ist der Lauf nicht byte-gleich.
2. **Reihenfolge GB vor Verschiebung.** Solange `_prevRoomTemp` `static` ist, ist die Verschiebung
   in eine eigene Klasse keine reine Textbewegung, sondern eine Entscheidung darüber, **wo der
   Zustand künftig lebt**. E20 verlangt daher GB zuerst; dieser Befund bestätigt das.
3. **Der Faktor 100 hängt an zwei Stellen zugleich.** `SpezWaermeverlusteC` gibt das Hundertfache
   zurück (`BP:361`), `SolareGewinneC` ebenso (`BP:341`); die zugehörige Division `/ 100.0` steht
   an den vier Aufrufstellen (`SWB:757`, `:774`, `:827`, `:843`). Funktion und Division müssen
   **gemeinsam** wandern, sonst verschiebt sich das Ergebnis um zwei Zehnerpotenzen.
4. **Der Abbruch bei fehlender Tagesverteilung.** `:590-595` gibt `false` zurück, und `:197`
   bricht damit die **ganze** Bedarfsrechnung ab (`return`, nicht `continue`). Nach der Trennung
   darf dieser Abbruch nur noch aus dem Altweg-Modul kommen; der VDI-Weg kennt keine
   Tagesverteilung und darf hier nicht abbrechen. Der Rückgabetyp `bool` der Fassade bleibt.
5. **Seiteneffekte auf die Modellzeile.** `Berechnung_Gebaeude_Tageswerte` schreibt
   `item.Ferien = 0`, wenn `Raumsolltemperatur_Ferien < 1` (`:689-691`);
   `Bewohner_und_Flaeche_berechnen` schreibt `item.Bewohner` und `item.Z_AuswahlWohnflaeche`
   (`:641`, `:645`, `:652`, `:653`). Die Reihenfolge dieser Schreibvorgänge ist Teil der Rechnung
   und muss erhalten bleiben.
6. **Der Rückgabewert mit dem Faktor E8.** `BP:435` (`return acc * gesamtflaeche / wohnflaeche;`)
   ist zugleich Physik und Skalierung. Wer die Funktion verschiebt, verschiebt den Skalierungsweg
   des Altwegs mit — der VDI-Weg muss seine eigene Nachmultiplikation führen
   (Rechenschritte 8.3, Umsetzungskonzept 1.5).
7. **Die 100er-Grenze.** `HeizwaermebedarfGeb` (`:31`) und `MaxP` (`:56`) sind `double[100]`;
   ein Projekt mit mehr als 100 Gebäuden wirft an `:816` eine `IndexOutOfRangeException`. `MaxP`
   wird an `:212` geschrieben und **nirgends gelesen**. Beides gehört nach GB (Frage U9), nicht in
   die Verschiebung.
8. **Ein neuer Bestandsbefund: `Ferien_Absenkung` wird in der Jahresschleife nicht nachgeführt.**
   In der Vorlaufschleife setzt `:781` `Ferien_Absenkung` je Tag aus `F_Absenkung[Tag]`. In der
   Jahresschleife (`:818-886`) wird nur `WE_Absenkung` neu bestimmt (`:845-849`);
   `Ferien_Absenkung` behält den Wert vom letzten Vorlauftag (Tag 364) und geht so in alle 365
   Aufrufe von `TaeglHeizlastWG` ein (`:872`). Die Ferienabsenkung wirkt damit ganzjährig oder gar
   nicht. **Vorschlag: mit GB beheben** — es ist eine Ergebnisänderung und gehört in denselben
   Einfrierschritt wie die übrigen Ferienbefunde (Umsetzungskonzept 1.8: `:689-733`). Wird es
   nicht behoben, wandert der Fehler mit dem Modul und lebt bis GA weiter.

---

## 4. Was die Stufe GA entfernt

### 4.1 Datenbank

| Gegenstand | Was geschieht |
|---|---|
| `Tab_Gebaeude` und `Tab_Gebaeude_STAMM` | `DROP COLUMN` für **`Gebaeude_Modell`**, **`Fensterflaeche_Ost_West`**, **`Wochenende`**, **`Ferien`** (je Tabelle 4 Spalten) |
| Sicht `Abfrage_Projektgebaeude` | Neubau aus `GebaeudeSchema.SQL_VIEW_NEU` ohne die vier Spalten; `ProjektGebaeudeCtrl` (dann Namensleser) zieht mit |
| `Tab_DBTagV`, `Tab_DBTagVDaten` | verlieren ihren einzigen Rechenleser (`DBTagesVeteilung:670`). Ob die Tabellen und der `GebaeudetypDialog` bleiben, ist eine eigene Frage (5.2) |
| `DbWerte` | `GEBAEUDE_MODELL_TAGESBILANZ` / `_VDI6007` entfallen (Konzept 6.4) |
| Testdatenbank | Schemastand wandert mit; das Übergangs-Referenzprojekt (A15) wird auf VDI 6007 gestellt |

### 4.2 Quelltext

| Gegenstand | Stelle heute |
|---|---|
| Modul `Altweg/` samt `TagesbilanzRechenweg` und `TagesbilanzPhysik` | rund 435 Zeilen (3.1) |
| Die Weiche in `HeizwaermeEinesGebaeudes` und `IGebaeudeRechenweg` | Fassade wird zum geraden Aufruf des einen Moduls |
| Der Altweg-Teil des Klimakalenders | `Sol_N/W/O/S`, `A_Temp`, `TagTyp_W/NW` in `KlimakalenderLesen:517-525` — nur noch `WE`, `Stundentemperatur` und `WochentagJan1` bleiben |
| `modellErzwungen` an `GebaeudeBedarfCtrl.Rechnen` | Umsetzungskonzept 1.4 — der Parameter existiert allein für den Vergleich alt/neu |
| Wache „der VDI-Weg referenziert nichts aus `Altweg/`" | wird gegenstandslos und entfällt mit dem Ordner |

### 4.3 Oberfläche

| Gegenstand | Was verschwindet |
|---|---|
| Schalter **„Rechenweg"** im Gebäudekatalogdialog | Klappliste, Herleitungszeile, Ressourcenschlüssel |
| Abschnitt **„Übergang: Tagesbilanz"** | die vier Felder aus 1.5 samt Aufklapplogik |
| Spalte **„Modell"** in der Projektliste des Gebäudedialogs | Umsetzungskonzept 2.7 Punkt 1 |
| **Vergleich alt/neu** im Bedarfsdialog | die vierspaltige Tabelle und `GebaeudeBedarfDaten.Vergleich` (Umsetzungskonzept 2.7) |
| Ausweis **„Tagesbilanz (Übergangsweg)"** in Bericht und Bedarfsdialog | Konzept N1.25 Punkt 5; danach gilt allein der Produktausweis nach E10 |

### 4.4 Tests und Nachweise

| Gegenstand | Was geschieht |
|---|---|
| **Rückweg-Test** (Arbeitskopie mit `Gebaeude_Modell = 'TAGESBILANZ'` gegen die GB-Basis) | wird eingestellt; der eigene Modus des Referenzlaufs entfällt |
| `BhkwPlanRueckgabeTests` | die Fälle zu `SolareGewinneC`, `SpezWaermeverlusteC`, `TaeglHeizlastWG` entfallen mit den Funktionen (`:48`, `:67`, `:136-141`, `:155`) |
| Datenbankfall „Tagesbilanz ergibt dieselbe Reihe wie der Lauf" | entfällt |
| bunit-Fälle des Übergangsabschnitts und des Schalters | entfallen |
| **Referenzbasis** | neu einfrieren (GA ist ein Einfrierschritt, Konzept N1.25 Punkt 6); Begründung in `Referenzlaeufe/LIESMICH.md`, Logbuch-Eintrag im Wiki |

**Aufwand GA:** die Spanne aus ADR-006 — rund **5–8 PT** — trägt; der Schemaschritt mit vier
`DROP COLUMN` je Tabelle und Sichtneubau ist der aufwendigste Teil, das Neu-Einfrieren der
teuerste Nachweis.

---

## 5. Offene Punkte

| Nr. | Punkt | Vorschlag |
|---|---|---|
| **X1** | `Wochenende` und `Ferien` sind **abgeleitete Flags**, die die Hülle beim Speichern setzt (`GebaeudeKatalogDialog.razor:809`, `GebaeudeKatalogDaten.cs:135-142`) und die der Dialog nie zeigt. Gehören sie in den sichtbaren Übergangsabschnitt oder bleiben sie unsichtbar bis GA? | **Unsichtbar lassen.** Sie sind Ableitungen, keine Eingaben; der Abschnitt „Übergang: Tagesbilanz" zeigt dann `Typ` und `Fensterflaeche_Ost_West` |
| **X2** | `Typ` ist die Verbindung zur Tagesverteilung (`Tab_DBTagV`) **und** ein Katalogmerkmal, das der Anwender im Dialog wählt und im Gebäudetypen-Dialog pflegt. Wandert er mit GA oder bleibt er? | **Bleiben**, aber in der Hauptstruktur als Stammdatum; der Wegfall von `Tab_DBTagV` ist mit GA eine eigene Frage. Der Befund weist ihn deshalb als „nur Altweg (Rechenleser)" aus, schlägt aber „Hauptstruktur" für den Dialog vor, sobald der Anwender dies bestätigt |
| **X3** | `WW_Bedarf` und `Waermebedarf` (Spalte) haben **keinen einzigen Leser** und werden von keiner Maske angefasst (`GebaeudeKatalogHuelle.cs:92`). Mit GA entfernen oder als eigener Aufräumschritt? | Mit GA mitnehmen, wenn der Schemaschritt ohnehin `DROP COLUMN` fährt — aber als eigener Punkt ausweisen, weil es nicht Altweg, sondern toter Bestand ist |
| **X4** | **`Kellertemperatur` fehlt in der Spaltentabelle Konzept 6.1**, steht aber in Rechenschritte 1.1 (`:144`), Umsetzungskonzept 1.8 (Einfrierregel) und 2.4 (DTO, „zwölf Felder"). | Konzept 6.1 nachziehen — **nicht Gegenstand dieses Auftrags**, gehört dem Konzeptpapier |
| **X5** | Der **Vorbereitungsschritt ist nicht vollständig modellfrei** (2.4 Punkt 6): `VerbrauchAlt` kommt aus dem gewählten Modul. ADR-006 und N1.25 beschreiben den Schritt als modellfrei. | Formulierung schärfen: modellfrei sind Klimakalender, Bewohner und `VerbrauchNeu`; die **Rückrechnung** ist ein zweiter Aufruf **desselben** Moduls hinter der Weiche. Das erfüllt E20 („kein Weg ruft den anderen") und ändert die Rechenfolge nicht |
| **X6** | Der Befund **`Ferien_Absenkung` wird nicht nachgeführt** (3.4 Punkt 8) ist eine Ergebnisänderung. | In **GB** beheben, vor der Verschiebung — sonst wandert der Fehler in das eingefrorene Modul |

---

**Zurück zum Konzept:**
[`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
(N1.25) · [`ADR-006`](../ADR-006_Trennung_Altweg_VDI6007.md) ·
[`Umsetzungskonzept`](../Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) ·
[`Rechenschritte`](../Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md)

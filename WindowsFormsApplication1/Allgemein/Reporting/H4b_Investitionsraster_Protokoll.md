# H4b — Gerätewelt-Bezugsgrößen und Investitionsraster (Umsetzungsprotokoll)

Etappe der H-Serie (Pflichtpositionen/Bemessungsarten, Konzept Kostendialoge § 5.3 in
Verbindung mit `Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md` § 4.5). Stand 30.08.2026,
Branch `Pufferspeicher`. Vorgänger: `H4a_Bezugsgroessen_Protokoll.md` (Betriebsseite +
Investitions-/kWh-Rückfälle), dort auch die Abgrenzung der Serien H1–H4.

## 1. Auftrag

Nach H4a fehlten auf der **Investitionsseite** (`LiesInvestitionen`, Kategorie 1) noch:

1. Die **Gerätewelt-Bemessungen** — „je kW Heizleistung", „je kW Leistung",
   „je kW elektrisch", „je kWp", „je kWh Kapazität", „je m² Kollektorfläche" —
   hatten keine automatische Bezugsmenge: ohne gepflegte `Menge` blieb die Zeile
   beim gespeicherten Betrag (bzw. 0).
2. Die **Rasterarten** `PROZENT_ERZEUGERKOSTEN` (Anteil an der Hauptposition der
   Komponente) und `PROZENT_INVESTITION` (Anteil an der Investitionssumme) wurden
   in Kategorie 1 gar nicht abgeleitet — genau die Arten, mit denen der Altdialog
   Planungsnebenkosten/Unvorhergesehenes als Prozentraster aufbaute.

## 2. Umsetzung

### 2.1 `TechnikPlanwertCtrl.BaugroesseSumme` (Controller, +99 Zeilen, rein additiv)

Neuer Abschnitt „ETAPPE H4b" in der bestehenden Datei: `KomponentenName(int)`
übersetzt die festen Kostenkomponenten 1…7 in die Gewerke-Landkarte, und
`BaugroesseSumme(projektID, komponentenID, bemessung, idAnlage)` summiert die **rohe
Baugröße** der verbauten Geräte über die Anlagen-Geräteverweise — mit
Art↔Gewerk-Kreuzprüfung (eine „je kWp"-Zeile an der Kesselkomponente liefert null,
keine Fantasiezahl):

| Bemessung | Komponente | Quelle |
|---|---|---|
| `EUR_PRO_KW_HEIZLEISTUNG` | 1 Wärmepumpe | `Tab_WP.Nennleistung` |
| `EUR_PRO_KW_LEISTUNG` | 2 Heizkessel | `Tab_Heizkessel.Ptherm` |
| `EUR_PRO_KW_ELEKTRISCH` | 7 BHKW | `Tab_BHKW.Pel` |
| `EUR_PRO_KWP` | 3 Photovoltaik | `Tab_Energieanlagen.PV_Leistung` (Anlagenspalte) |
| `EUR_PRO_KWH_KAPAZITAET` | 5 Stromspeicher | `Tab_Stromspeicher.Energie` |
| `EUR_PRO_M2_KOLLEKTOR` | 4 Solarthermie | `Tab_Solarkollektoren.Aperturflaeche` × `Tab_Energieanlagen.Kollektormodulanzahl` |

Optional anlagenscharf (`idAnlage > 0` → `AND a.ID = ?`); Summe ≤ 0 → null.
**Pufferspeicher (6) liefert bewusst null**: ohne definiertes Temperaturpaar gibt es
keine belastbare kWh-Kapazität eines Wärmespeichers (Kommentar an Ort und Stelle).

### 2.2 `WirtschaftlichkeitCtrl.LiesInvestitionen` — Drei-Runden-Kaskade

Der Lesepunkt puffert die Zeilen jetzt in `InvestZeile` (SELECT um
`w.KomponentenID`, `f.IsMainComponent` via `LEFT JOIN Tab_Kostenfaktor f` und — bei
vorhandenen Spalten — `Kostenart/Bemessung/Menge/Einheitpreis`, `ID_Anlage` erweitert)
und rechnet in drei Runden:

1. **Runde 1 — direkte Arten** über `InvestBetrag(z, idProjekt, null)`:
   `BETRAG`/leer → Wert; **VALERI-Vorrang** (Szenariowert ≠ `EingegebenerWert` →
   Szenariowert roh); gepflegte `Menge` hat Vorrang; sonst Rückfall
   `TechnikPlanwertCtrl.BaugroesseSumme` → `BetriebskostenCtrl.Betrag` (der EINE
   Rechenweg, unverändert).
2. **Runde 2 — `PROZENT_ERZEUGERKOSTEN`**: Basis = Σ der Runde-1-Beträge der
   **Hauptpositionen** (`Tab_Kostenfaktor.IsMainComponent = TRUE`) derselben
   Komponente.
3. **Runde 3 — `PROZENT_INVESTITION`**: Basis stufig Anlage → Komponente → Projekt
   über die bereits abgeleiteten **Nicht-Zuschuss**-Beträge der Runden 1–2.

Ausgabe unverändert (Betrag == 0 → continue; Zuschussarten → `zuschuss += |Betrag|`);
Bestandszeilen der Art `BETRAG` laufen exakt den alten Weg.

## 3. Nachweise (Harness `..\dev\h4b\`, Reflection auf `EPOS_Plan.dll`)

Produktiv-DB **nur lesend**; alle Schreibproben auf einer je Lauf **frischen**
Scratchpad-Kopie. Build x64 exit 0 (nur die fünf bekannten Alt-Warnungen
CS0108/CS0109/CS1998), `<<<<<<<`-Sweep repoweit ohne echten Treffer.

### [1] Bestandsneutralität (Produktiv, lesend)

21 Kategorie-1-Zeilen tragen eine abgeleitete Bemessung — **alle 21 ohne Satz**
(H1-Saat der Vorlagenraster). Ohne Satz fällt `Betrag()` auf den gespeicherten Wert
zurück → **kein einziger Bestandsbetrag ändert sich**. Kein Fall „Menge×Satz
inkonsistent", kein Fall „Satz ohne Menge" (der jetzt neu ableiten würde).
Regressionssummen `LiesInvestitionen`: 1018 = 45.312,50 / 1024 = 12.001,00 /
1042 = 13.000,00 € — identisch mit dem Stand vor H4b (per Konstruktion, da nur
BETRAG-Zeilen wirksam sind; über beide Harness-Fassungen stabil gemessen).

### [2] Baugrößen-Direktproben — 6/6 GLEICH

`BaugroesseSumme` gegen die SQL-Handsumme an dynamisch gesuchten Projekten:

| Art | Projekt | Aufloeser == SQL |
|---|---|---|
| kW Heizleistung (WP) | 1012 | 78,00 |
| kW Leistung (Kessel) | 1008 | 22,10 |
| kW elektrisch (BHKW) | 1017 | 10,00 |
| kWp (PV) | 1007 | 20,00 |
| kWh Kapazität (SP) | 1017 | 12,80 |
| m² Kollektor (Solar) | 1011 | 1,60 |

### [3] End-to-End-Kaskade an der 1042-Kopie — EXAKT

Ausgangslage: Invest-Summe 13.000,00 € (eine BETRAG-Zeile der Komponente 1; dazu
H1-Saatzeilen mit Wert 0 ohne Satz), WP-Nennleistung Σ 26,00 kW. Über die
**App-Schreibwege** (`KostenPositionCtrl.SetzeBetrag` + `SetzeBetragMitZusatz`) drei
Zeilen an Komponente 1 angelegt:

| Zeile | Bemessung | Satz | Erwartung |
|---|---|---|---|
| A (Hauptposition, Stamm `IsMainComponent`) | je kW Heizleistung | 653,60 €/kW | 26 × 653,60 = **16.993,60** |
| B | % der Erzeugerkosten | 5 % | 5 % × A = **849,68** |
| C | % der Investition | 10 % | 10 % × (A + B + 13.000) = **3.084,33** |

`LiesInvestitionen` danach: 33.927,61 € — **Delta exakt +20.927,61** („GLEICH").
Damit sind belegt: Hauptpositions-Basis der Runde 2 (nur die `IsMainComponent`-Zeile
zählt — die Bestandszeile Stamm 77 mit Wert 0 stört nicht) und die
**Komponentenstufe** der Runde 3 (Bestand 13.000 rechnet in die Basis ein).

### [3b] VALERI-Vorrang auf der Investseite — EXAKT

`BestCase = 12.345` auf die A-Zeile gesetzt (kein sonstiger BestCase im Projekt,
per COUNT belegt): BEST-Szenario liefert **28.558,47** == Erwartung
33.927,61 + (12.345 − 16.993,60) × 1,155 — der gepflegte Szenariowert verdrängt
die Ableitung roh, die Prozentrunden ziehen exakt nach.

### [4] Betriebsseite unberührt

`LiesBetriebskosten(1024)` = **99,00 €/a** (Bestandsanker, unverändert).

### Lehre aus dem Erstlauf

`SetzeBetrag` ist ein **Upsert** je (Projekt, Kategorie, Komponente, StammID) — der
erste Testlauf kollidierte mit Bestands-StammIDs und überschrieb Zeilen (sichtbar an
identischen Rückgabe-IDs). Der Harness wählt seither garantiert unbenutzte StammIDs
und bricht bei ID-Kollision ab.

## 4. Dokumentierte Grenzen

1. **Puffer-Kapazität bleibt null** (§ 2.1) — kein stillschweigendes kWh-Raten.
2. **Keine %-auf-%-Rekursion**: Runde 3 basiert auf den abgeleiteten Beträgen der
   Runden 1–2; `PROZENT_INVESTITION`-Zeilen zählen einander nicht als Basis
   (kein Zirkel, bewusst).
3. Die **Betriebsseiten**-Rückfälle aus H4a (`InvestSummeFuer`) lesen weiterhin die
   **DB-Summe** der Kategorie 1 — rein rechnerisch abgeleitete Investbeträge (Zeilen
   ohne gespeicherten Wert) fehlen dort, bis der Dialog-Speicherweg die Mengen
   ausweist (offene Etappe H2-1).
4. Die 21 H1-Saatzeilen bleiben wirkungslos, **bis ein Satz gepflegt wird** — genau
   das Konzeptverhalten (Raster anbieten, nichts erfinden).

## 5. Geänderte Dateien

| Datei | Änderung |
|---|---|
| `Controller/TechnikPlanwertCtrl.cs` | +99 Zeilen: `KomponentenName`, `BaugroesseSumme` (Abschnitt „ETAPPE H4b") |
| `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` | `LiesInvestitionen` auf InvestZeile-Puffer + Drei-Runden-Kaskade, Helfer `InvestBetrag`, `IstProzentErzeuger`/`IstProzentInvest` (+146/−14) |
| `Allgemein/Reporting/H4b_Investitionsraster_Protokoll.md` | dieses Protokoll |

Harness `..\dev\h4b\` (gitignored): Programm mit den Proben [1]–[4] wie oben.

# Konzept: Wirtschaftlichkeitsberechnung in EPOS-Plan

**Fassung 7** · Stand 12.08.2026 · Status: entschieden + **Stufen W1–W3 + KWKG-2025-Nachtrag umgesetzt (Phasen 6–9)**
Quellen: `goetz_test.XLS` (BHKW-Plan, alle Blätter), Kostenmodul `Views/Kosten/*`, `Controller/ErgebnisCtrl.cs`, `Views/Varianten/EnergieMengen.cs`, DIN EN 17463 (ValERI), `VALERI_Vorlage_V7.xlsx` (epos-sync, Detailanalyse ausstehend), KWKG 2025 (BGBl. 2025 I Nr. 54; Web-Recherche Rechtsstand 12.08.2026 → Kapitel 8)
Bezug: `Konzept_Berichtserstellung_EPOS-Plan.md` (Kapitel Wirtschaftlichkeit) (Feldmapping-Analyse: archivierter Stand im Claude-Projekt)

> **Was sich gegenüber Fassung 6 geändert hat (Phase 9, 12.08.2026).**
> **Der KWKG-2025-Nachtrag (Kapitel 8.5) ist umgesetzt:** degressive Vbh-Staffel
> als Katalogtabelle **`Tab_KWKG_Staffel`** (vorbefüllt 2020–2030, pflegbar;
> Kalenderjahr-Mapping über das Inbetriebnahmejahr, Deckel-Parameter jetzt
> Override mit 0 = Staffel), **Fristenlogik § 6** (Stichtag Bestellung/Genehmigung
> ≤ 31.12.2026 + 4 Jahre Realisierung, sonst Bonus = 0 mit Hinweis; ohne Datum
> „Förderfähigkeit ungeprüft"), Guards **> 500 kW** (Σ Tab_BHKW.Pel,
> Ausschreibungslücke) und **Heizöl-Neuanlagen**, **Negativpreis-Abschlag** [%]
> (kontingentschonend) sowie die Sensitivitätszeile **„KWKG-Bonus entfällt
> (Regulierungsrisiko Novelle)"**. Damit sind 8.5.1–8.5.7 abgedeckt; nur die
> exakte Spotpreis-Stundenrechnung (8.5.4, zweiter Teil) bleibt offen.
> Umsetzungstiefe laut 7.10: **W2-Nachtrag komplett** — entschieden 12.08.2026.

> **Was sich gegenüber Fassung 5 geändert hat (12.08.2026).**
> **Rechtsstand KWKG aktualisiert — neues Kapitel 8.** Das KWKG 2025 (Novelle
> verkündet 25.02.2025, in Kraft seit 01.04.2025) ersetzt die im Alt-Verfahren
> hinterlegte KWKG-2020-Parametrik in zwei Punkten, die das Rechenmodul betreffen:
> **(1)** Die **Vbh-Staffel endet nicht bei „ab 2025: 3 500"**, sondern läuft
> degressiv weiter (2026: 3 300 → … → ab 2030: 2 500 Vbh/a, Kontingent weiterhin
> 30 000 Vbh) — siehe 8.3. **(2)** An die Stelle der reinen Inbetriebnahmefrist
> tritt die **dreistufige Fristenlogik des § 6** (Dauerbetrieb ODER
> BImSchG-Genehmigung ODER verbindliche Bestellung bis 31.12.2026, danach
> 4 Jahre Realisierungsfrist) — siehe 8.2. Konsequenzen für das Rechenmodul
> in 8.5; Querverweise ergänzt in 2.7, 5.4 (W2) und Kapitel 7 (neuer Punkt 10).
> Die große KWKG-Novelle (Verlängerung über 2026 hinaus) stand am 12.08.2026
> noch aus — Regulierungsrisiko, siehe 8.1/8.5.

> **Was sich gegenüber Fassung 4 geändert hat (Phase 8, 11.08.2026).**
> **Stufe W3 ist umgesetzt:** Strommengen-Matrix Winter/Sommer × HT/NT aus den
> Stundenreihen (`StromMatrix`, persistiert in `Tab_ErgebnisStromMatrix`),
> vereinfachte **Tarifstruktur** je Stamm (`Tab_ProjektTarif`, Dialog
> „Tarifstruktur Strom“; ersetzt bei Aktivierung die Flat-Strompreise inkl.
> **zweistufiger Leistungspreis-Staffel**), **KWKG-Split** Eigenstrom/Einspeisung
> (stundenweise min-Regel) und die **Emissionsbilanz** gekoppelt vs. getrennt mit
> **Kraftwerkspark-Katalog** (`Tab_Kraftwerkspark`, vorbefüllt Strommix/GuD/
> Steinkohle) + Referenzkessel — in Reiter, Word-Baustein und Excel-Blatt.
> Damit sind die Kapitel 2.5/2.8-Inhalte des Alt-Verfahrens in vereinfachter,
> entschiedener Form abgedeckt; offen bleiben Preisszenarien über die
> `energy_price`-Historie und die W2-Restpunkte (positionsbezogene Zins-Overrides).

> **Was sich gegenüber Fassung 3 geändert hat (Phase 7, 11.08.2026).**
> **Stufe W2 ist umgesetzt:** Sensitivitätsanalyse (4 Parameter ±Δ → KW, persistiert
> in `Tab_ErgebnisWirtSensitivitaet`), **interner Zinsfuß** (Bisektion auf der
> nominalen Differenzreihe), **BEHG-CO₂-Abgabe** (€/t auf das Brennstoff-CO₂,
> steigt mit p_E) und **KWKG-Bonus** (ct/kWh auf den BHKW-Strom, Vbh-Jahresdeckel
> + 30.000-Vbh-Kontingent; Näherung Vbh = Betriebsstunden, getrennte Sätze
> Eigenstrom/Einspeisung erst mit der W3-Strommengen-Matrix). Zusätzlich sind die
> Kostenmodul-Befunde **B4–B6** (3.5.4–3.5.6) behoben. Preisszenarien über die
> `energy_price`-Historie sowie HT/NT-Tarife bleiben W3.

> **Was sich gegenüber Fassung 2 geändert hat (11.08.2026).**
> **(1) Die offenen Punkte aus Kapitel 7 sind entschieden:** Referenz =
> **Stammprojekt** (Unterlassensalternative; KW der Variante = Barwert der
> Differenz-Zahlungsströme Variante − Stamm) · Vorgabe **i = 3,0 % / T = 20 a**
> (je Stamm editierbar) · **Restwert linear** · **Strompreise aus der
> Kostenmaske** (energy_project_settings, keine Doppelpflege).
> **(2) Stufe W1 ist implementiert** (Phase 6): `Allgemein/Wirtschaftlichkeit/`
> (`KapitalwertRechner`, `WirtschaftlichkeitCtrl`, DTOs),
> `Views/Wirtschaftlichkeit/` (Reiter + Parameterdialog), Berichts-Baustein und
> Excel-Blatt; Tabellen `Tab_ProjektWirtschaftlichkeit` /
> `Tab_ErgebnisWirtschaftlichkeit` werden beim ersten Start automatisch angelegt.
> Details: `Allgemein\Bericht\LIESMICH_Phase1.md`, Abschnitt Phase 6.
> **(3) DB-Nachverifikation** (Kenndaten.accdb-Kopie 11.08.2026): Emissionsfaktoren
> in **g/kWh** bestätigt (Tab_Brennstoff_Stamm: Erdgas 240, Heizöl 310, Strom 560);
> `energy_carrier`-Kopien tragen fast durchweg co2 = 0 → CO₂-Faktor-Kette
> **Projektwert → Tab_Brennstoff_Stamm → energy_carrier** ist zwingend und so
> im `KostenEmissionRechner` umgesetzt (auch für den Netzbezug; die Konstante
> 380 g/kWh ist nur noch Fallback).

> **Was sich gegenüber Fassung 1 geändert hat.** Zwei Vorgaben sind hinzugekommen:
> **(1) Die Berechnungsmethode ist entschieden** — Kapitalwertmethode angelehnt an
> **DIN EN 17463 (ValERI)**; das Alt-Verfahren (Annuitäten-Systemvergleich) bleibt
> Quelle für das Zahlungsgerüst und Validierungsreferenz, nicht mehr Methodenvorbild
> (neues Kapitel 5). **(2) Die Ergebnisse bekommen einen eigenen Platz im UI**: ein
> weiterer Reiter „Wirtschaftlichkeit" im Bereich **Berichte & Kosten**, mit
> Prüfung/automatischer Ausführung der Vorbedingungen (neues Kapitel 6).
> Die Analyse des Alt-Verfahrens (Kapitel 2), der Bestandsaufnahme (Kapitel 3) und
> der Lückenliste (Kapitel 4) ist unverändert gültig.

> **Einordnung.** Dieses Dokument rekonstruiert das Rechenverfahren der bewährten
> Excel-Mappe (BHKW-Plan, Blattfamilie `Tab_kurz*` / `Tab_Kosten` /
> `Tab_Wirtschaftlichkeit*`), stellt ihm gegenüber, was EPOS-Plan heute an Daten
> bereits führt, benennt die Lücken und definiert das Zielbild: **Kapitalwertmethode
> nach DIN EN 17463**. Es ersetzt nicht die fachliche Vorgabe, sondern bereitet sie
> vor: Kapitel 7 listet die verbleibenden Entscheidungen.
>
> *Hinweis zur Quellenlage:* Analysiert wurden die **Werte und Beschriftungen** aus
> `goetz_test.XLS` (Chat-Upload). Die **Struktur der VALERI-Vorlage** ist über die
> Zell-Dokumentation des INEKON-Skills `valeri-bewertung` vollständig bekannt und in
> **Kapitel 5.3** eingearbeitet. Die Formeldateien auf dem Z:-Netzlaufwerk selbst
> (`BHKW-WP-PLAN.XLSM`, `VALERI_Vorlage_V7.xlsx`, `wirtschaftlichkeit(.2).html`,
> `db_Struktur_Pricing_Modell.sql`, `Energiekosten_Designvorschlag.docx`) ließen sich
> über die Dateibrücke weiterhin nicht übertragen (Auflisten geht, Lesen nicht —
> mutmaßlich die Umlaute im Z:-Pfad); die Annuitätsformel wurde numerisch verifiziert
> (2.2). Workaround bei Bedarf: Dateien in den verbundenen C:-Ordner kopieren oder
> als Chat-Anhang geben.

---

## 1. Das Alt-Verfahren im Überblick (BHKW-Plan / Excel)

Die Mappe rechnet einen **Systemvergleich**: das geplante System (KWK-System bzw.
„Alternatives System" bei der WP) gegen eine **Vergleichsheizung** (Referenz-Heizkessel).
Drei Kurzbericht-Varianten teilen sich dieselbe Kostenbasis:

| Blatt | Variante | Besonderheit |
|---|---|---|
| `Tab_kurz` / `Tab_kurz_KWKG2016` | BHKW nach KWKG 2016 | Bonus bis 50 kW |
| `Tab_kurz_KWKG2020` | BHKW nach KWKG 2020 | Bonus je kWh mit **Vollbenutzungsstunden-Kontingent 30.000 Vbh**, jährlich gedeckelt |
| `Tab_kurz_WP` | Wärmepumpe | eigener Zinssatz/Zinsreduktion, Zuschuss, „Energiekosten" statt „Brennstoffkosten" |

Ergebnisgrößen in allen Varianten: Summe Investitionen (System/Vergleich),
Mehrinvestition, Kapitalkosten (Annuität), Betriebskosten, Brennstoff-/Energiekosten,
Erlöse, Gesamtkosten, **Überschuss gegenüber Vergleichsheizung** (€/a, mit und ohne
Tilgung), **spez. Wärmegestehungskosten** (nach Stromgutschrift),
**spez. Stromgestehungskosten** (nach Wärmegutschrift), **Amortisationszeit**
(„–"/9999 = amortisiert sich nicht; mit und ohne Energiesteuer-Effekte).

---

## 2. Rechenschema des Alt-Verfahrens im Detail

*(Bleibt maßgeblich als **Zahlungsgerüst-Katalog** für die Kapitalwertrechnung —
jede hier aufgeführte Kosten-/Erlöszeile wird in Kapitel 5 zu einem Zahlungsstrom.)*

### 2.1 Investitionen (`Tab_Kosten`, Zeilen 1–75)

Struktur je Block: **Position · Investition [€] · Nutzungsdauer [a] → Kapitalkosten [€/a]**.

| Block | Positionen |
|---|---|
| BHKW-Module | bis 10 Module einzeln; **reduzierter Zins** (Zinssatz − Zinsreduktion BHKW, z. B. 1,5 % − 1,15 %) |
| Spitzenkessel | bis 6 Kessel; voller Zinssatz |
| Heizraum | spez. Kosten €/m³ × Raumbedarf (BHKW, Kessel, Puffer) |
| Nahwärmenetz | Verteilnetz, Hausanschluss, Hausstation (× Anzahl) |
| Heizzentrale | BHKW-Einbindung, Heizungstechnik, Stromeinspeisung, Heizöllagerung, Erdgasanschluss, Schornstein, Abgasanlage, Pufferspeicher, bauliche Maßnahmen, Sonstiges 1–3 |
| Nebenkosten | wahlweise % der Investition oder Betrag |
| Zuschuss | mindert die Investitionssumme |
| Referenzsystem | eigener Block „Heizkessel" (Vergleichsheizung), gleiche Systematik |

### 2.2 Kapitalkosten: Annuität (numerisch verifiziert)

```
a(i, n) = i·(1+i)^n / ((1+i)^n − 1)          Kapitalkosten = Investition · a
```

Verifikation an den Blattwerten: BHKW 17 150 € · a(0,35 %; 13,33 a) = 1 319 €/a
(0,35 % = Zinssatz 1,5 % − Zinsreduktion 1,15 %); Kessel 3 731 € · a(1,5 %; 20 a) = 217 €/a. ✓
Die Nutzungsdauer des BHKW ist abgeleitet (z. B. aus Vbh-Kontingent/Betriebsstunden:
13,33 a), nicht frei eingegeben.

### 2.3 Betriebskosten (`Tab_Kosten`, Zeilen 77–91)

| Position | Bemessung |
|---|---|
| Wartung BHKW | €/h Betriebsstunden **oder** €/kWh_el Erzeugung |
| Instandhaltung BHKW / Spitzenkessel / Wärmezentrale / bauliche Anlagen / Stromeinspeisung | **% der jeweiligen Investition p. a.** (Staffel 2–6 %) |
| Personalkosten, Verwaltungskosten | % der Gesamtinvestition p. a. |
| Hilfsenergiekosten | % der Brennstoffkosten p. a. |
| Reserveleistung, Sonstige | Betrag |

### 2.4 Brennstoff-/Energiekosten (`Tab_Kosten`, Zeilen 96–108)

Je Träger: **Menge (Ho) × Preis** — Erdgas mit Arbeits- **und** Leistungspreis
(€/kWh_Ho, €/kW_Ho), Heizöl in l × €/l, ferner Biogas, Rapsöl, Holz, Flüssiggas,
Bioerdgas (Arbeit + Leistung), Sonstige, Strom (€/kWh). Die Mengen kommen aus der
Simulation (Verbrauch über Heizwert in die Abrechnungseinheit umgerechnet — exakt
das, was `EnergieMengen.Menge()` in EPOS-Plan heute schon tut).

### 2.5 Erlöse (`Tab_Erloese`, `Tab_Tarifstruktur`)

Alle Strommengen werden in einer **Matrix Winter/Sommer × HT/NT** geführt
(Strombedarf, Reststrombezug, Stromeinspeisung, Stromeigenverbrauch):

1. **Vermiedene Strombezugskosten** = Strombedarfskosten (Bedarf × Bezugspreis)
   − Reststrombezugskosten (Restbezug × Reststrompreis), zzgl. eingesparter
   Leistungspreis (max. Bedarf vs. max. Restbezug, kW × €/kW).
2. **Einspeiseerlös** = Einspeisemenge × Einspeise-Arbeitspreis je Tarifzone,
   zzgl. Leistungspreis-Komponente (P_soll, erfüllte Stunden).
3. **KWK-Bonus** nach Einspeisemodell getrennt für *Eigenstromnutzung* und
   *Einspeisung* (ct/kWh; KWKG 2020 mit Vbh-Kontingent, siehe 2.7).
4. **Energiesteuerrückerstattung** (§ 53a EnergieStG-Logik: Rückerstattungssatz
   auf KWK-Brennstoff; Blattwerte 4,4 / Gas 5,5 / Öl 61,35 [€/MWh]).
5. **Eingesparte Stromsteuer** für Eigenerzeugung (Blatt: −20,50 €/MWh,
   Staffel bis/ab 25 MWh).

Die `Tab_Tarifstruktur` definiert dazu **drei Preisregelungen** (Bezug, Einspeisung,
Restbezug) mit Sommer-/Winterzeitraum, HT/NT-Zeiten je Wochentag,
Leistungspreis-Staffeln („für die ersten … kW / die weiteren …") und dem
vereinfachten Modus **„Durchschnitt"** (ein Preis für alle Zonen — so ist die
Beispieldatei gerechnet).

### 2.6 Ergebnisbildung (`Tab_Wirtschaftlichkeit`)

```
Gesamtkosten  = Kapital + Betrieb + Brennstoff (+ Solaranlage)     je System
Gesamterlös   = Energiesteuerrückerstattung + eingesparte Stromsteuer
              + Einspeisung/Boni + vermiedene Strombezugskosten
Nettokosten   = Gesamtkosten − Gesamterlös
Überschuss    = Nettokosten(Vergleichsheizung) − Nettokosten(System)   [€/a]
Wärmegestehungskosten  = Nettokosten nach Stromgutschrift ÷ Nutzwärme   [€/kWh]
Stromgestehungskosten  = Nettokosten nach Wärmegutschrift ÷ KWK-Strom   [€/kWh]
```

Der Überschuss wird zweifach ausgewiesen: *mit Tilgung entsprechend der
Abschreibungszeit* und *ohne Tilgung, nur Zinsen*.

### 2.7 Kapitalisierte Betrachtung (`Tab_Wirtschaftlichkeit_kap`)

Jahresreihe über den Betrachtungszeitraum, Startwert = −Mehrinvestition:

- **Preissteigerung** getrennt für Brennstoffkosten und weitere Kostenarten, **Kapitalzins** zur Abzinsung der jährlichen Einsparung.
- Einnahmen-/Ausgabenzeilen je Jahr: vermiedener Strombezug, Einspeisung, Wärmeerlöse (Betriebs-/Brennstoffkosten der Vergleichsheizung), eingesparte Stromsteuer, Energiesteuerrückerstattung, Brennstoff- und Betriebskosten KWK, **Bonus EEG**, **Bonus KWK-Strom**.
- **KWKG-2020-Bonus mit Vbh-Kontingent:** je Jahr *erreichte* vs. *vergütete* Vollbenutzungsstunden (Jahres-Deckel laut Blatt-Tabelle: 2020–22 je 5 000, 2023/24 je 4 000, ab 2025 3 500 Vbh), **kumuliert bis 30 000 Vbh**, danach entfällt der Bonus. *(Blattstand KWKG 2020 — nach geltendem Recht läuft die Staffel degressiv weiter bis 2 500 Vbh ab 2030, siehe 8.3.)*
- **BEHG/CO₂-Abgabe:** CO₂-Preis in €/t; Abgabe KWK-System, Abgabe Vergleichsheizung, Bilanzzeile.
- Amortisationszeit = erstes Jahr mit kumuliertem Überschuss ≥ 0.

→ Diese Jahresreihe **ist bereits eine Kapitalwertrechnung in Rohform** — das
Alt-Verfahren und die ValERI-Methode treffen sich hier (Kapitel 5).

### 2.8 Emissionsbilanz (`Tab_Energiebilanz`, `Tab_Emissionen`)

Brennstoffbilanz gekoppelte vs. getrennte Erzeugung: die getrennte Referenz erzeugt
dieselbe Wärme im Heizkessel und denselben Strom in einem **Referenz-Kraftwerkspark**
(Name, Wirkungsgrad, Faktoren CO₂/CO/SO₂/NOx/Staub in mg/kWh Brennstoff, inkl.
Netzverluste). Emissionsbilanz je Schadstoff: KWK-Erzeugung (BHKW + Kessel) vs.
getrennte Erzeugung (Heizkessel + Kraftwerk).

---

## 3. Was EPOS-Plan heute schon liefert

### 3.1 Mengengerüst — vollständig (`Tab_Ergebnis*` via `ErgebnisCtrl.Load`)

Wärme-/Strombedarf und Spitzenlasten, Rest-Wärme/Rest-Strom (= Netzbezug), Erzeugung
je Gewerk und **je Modul**, Deckungsgrade, Betriebs-/Vollbenutzungsstunden,
Brennstoffverbräuche je Träger (9 Arten, Aggregat + Modul), PV-/BHKW-**Überschuss**
(= Einspeisemenge), Jahresnutzungsgrad je Kesselmodul. Einheiten MWh/a, kW, %, h/a.
Stundenwerte für eine spätere HT/NT-Zerlegung existieren in der Simulation.

### 3.2 Preisgerüst (`energy_carrier` / `energy_project_settings` / `energy_price`)

Je Projekt-Energieträger: Arbeits-, Grund-, Leistungspreis, Hi/Hs (kWh je Einheit),
Abrechnungseinheit + Umrechnung (`energy_conversion.factor`), Emissionsfaktoren
CO₂/SO₂/NOx, **Preishistorie** mit `valid_from` (tagesgenau, ein Satz je Tag,
Zukunftsdaten möglich — verwendbar als **Preisszenario-Stützstellen** für die
Kapitalwertrechnung). Katalogbasis `Tab_Brennstoff_Stamm` mit Standardpreisen,
`pricing_model` (FUEL, LIQUID_FUEL, SOLID_FUEL, GASEOUS_FUEL, ELECTRICITY, HEAT, …)
steuert, welche Felder gelten. Einzige vorhandene Rechnung: `Preis/kWh = Arbeitspreis ÷ Hi`.

### 3.3 Investitions-/Betriebskosten (`Tab_ProjektWerte` + `Tab_Kostenfaktor`)

Kostenpositionen je Projekt mit `KomponentenID` (WP=1, Heizkessel=2, PV=3, Solar=4,
Stromspeicher=5, Puffer=6, BHKW=7), `KategorieID` (1=Investition, 2=Betrieb,
3=Energie), Gruppe, Betrag, **Nutzungsdauer [a]** sowie **Best-/Worst-Case für
Betrag und Nutzungsdauer** — die geforderte **Sensitivitäts-/Szenariobasis nach
DIN EN 17463 ist damit strukturell schon angelegt**. Technik-Planwerte werden per
`Abfrage_ProjektKostenKomponenten` automatisch als Hauptposition übernommen.

### 3.4 Bindeglied Menge → Preis

`EnergieMengen.Menge()`: `Menge [Einheit] = Verbrauch [MWh] × 1000 ÷ eff. Heizwert
[kWh/Einheit]` über `Abfrage_Energietraeger_Effektiv` (custom-Werte mit
Katalog-Fallback). **Die Multiplikation Menge × Preis existiert nirgends** — genau
hier setzt das neue Rechenmodul an.

### 3.5 Relevante Codebefunde (bei der Umsetzung mit erledigen)

1. **Brennstoff-Identität gebrochen:** `Tab_ErgebnisBHKWModul.Brennstoff` /
   `…HeizkesselModul.Brennstoff` sind freie Strings („Gas", „Öl" …); die Preisseite
   schlüsselt über `energy_carrier.id`. `EnergieMengen.CarrierFor()` überbrückt
   heuristisch über `Tab_BHKW/Tab_Heizkessel.Brennstoff → id_brennstoff → erster
   Treffer in energy_carrier` — ohne Projektbezug; bei mehreren Trägervarianten
   desselben Brennstoffs undefiniert. → **`carrier_id` als echte Spalte in die
   Ergebnis-Modultabellen** (nachrüstbar über das vorhandene
   `StelleXSpaltenSicher()`-Muster in `ErgebnisCtrl`).
2. `ErgebnisCtrl.Delete()` ist funktionsunfähig (Parameter fehlt, kein Commit).
3. Heizkessel-Modul-INSERT: `mo.Waermeproduktion` wird nicht persistiert,
   `mo.Verbrauch` nicht gerundet.
4. `Form_KostenAdmin`: Insert neuer Kostenfaktoren defekt (Variable als
   SQL-Bezeichner), eigener Registry-Connection-String statt `DataRepository`.
5. `energy_price`-Ersteintrag lässt `leistungspreis` leer, obwohl der Default
   ermittelt wird.
6. Kostenmodul speichert Energiepreise nur über den Save-Button im
   `ucFuelSettings`-Control (kein Speichern beim Formular-Schließen).

---

## 4. Lückenliste: Alt-Verfahren vs. EPOS-Plan heute

| Größe | Status | Anker in EPOS-Plan |
|---|---|---|
| Kalkulationszinssatz, Zinsreduktion je Gewerk | **fehlt** | — |
| Betrachtungszeitraum T | **fehlt** | `Nutzungsdauer` je Position vorhanden |
| Preissteigerungsraten (Energie/Betrieb) | **fehlt** | `energy_price.valid_from` nur Stützstellen |
| Kapitalwert-/Barwertrechnung | **fehlt** | Eingaben in 3.2/3.3 vorhanden |
| Restwertansatz (Nutzungsdauer > T) | **fehlt** | `Nutzungsdauer` je Position vorhanden |
| Zuschuss/Förderung (Vorzeichen/Typ) | **fehlt** | nur freie Betragsposition |
| KWKG-Bonus (ct/kWh, Vbh-Kontingent 30 000, Jahresdeckel) | **fehlt** | `Betriebsstunden_Gesamt`, `Stromproduktion` vorhanden |
| EEG-/Einspeisevergütung | **fehlt** | `Ueberschuss` [MWh/a] vorhanden |
| Vermiedener Strombezug (Bedarf − Restbezug) | **fehlt** | beide Mengen vorhanden |
| HT/NT-/Saison-Tarifstruktur | **fehlt** | Stundenwerte in der Simulation vorhanden |
| Energiesteuer/-rückerstattung, Stromsteuer | **fehlt** | — |
| CO₂-Preis (BEHG) €/t | **fehlt** | Emissionsfaktoren je Träger vorhanden |
| Referenz-Vergleichsheizung | **fehlt als Konzept** | Variantenmodell vorhanden (s. 5.6) |
| Wärme-/Stromgestehungskosten, Amortisation | **fehlt** | — |
| MwSt-/Netto-Brutto-Kennzeichen | **fehlt** | `Einheit` ist Freitext |
| Referenz-Kraftwerkspark (Emissionsgutschrift) | **fehlt** | CO₂/SO₂/NOx je Träger vorhanden |

---

## 5. Zielbild: Kapitalwertmethode nach DIN EN 17463 (ValERI)

**Vorgabe (10.08.2026):** Die Wirtschaftlichkeitsberechnung erfolgt mit der
**Kapitalwertmethode, angelehnt an DIN EN 17463** („Bewertung energiebezogener
Investitionen", ValERI). Kern der Norm: der **Kapitalwert (Net Present Value)** als
zentrales Bewertungskriterium; Erfassung **aller relevanten Zahlungsströme** über
den Betrachtungszeitraum; **transparente Dokumentation** der Annahmen; **Szenario-
und Sensitivitätsanalysen** zur Abbildung von Unsicherheiten. (Die Norm wird u. a.
in EnSimiMaV und EnFG referenziert — für Förder-/Nachweiszwecke relevant.)
Als Arbeitsvorlage dient **`VALERI_Vorlage_V7.xlsx`** (epos-sync-Tools; Struktur
wird nachgetragen, sobald die Datei lesbar ist — siehe Quellenhinweis oben).

### 5.1 Rechenkern

```
KW = −I₀ + Σ_{t=1..T} ( E_t − A_t ) / (1+i)^t  +  RW_T / (1+i)^T

I₀    Investitionsauszahlung abzgl. Zuschüsse (Tab_ProjektWerte, Kategorie 1)
E_t   Einzahlungen im Jahr t: Erlöszeilen aus 2.5/2.7 (vermiedener Bezug,
      Einspeisung, KWKG-/EEG-Bonus, Steuererstattungen), preisgesteigert
A_t   Auszahlungen im Jahr t: Betriebskosten (2.3), Energiekosten (2.4),
      CO₂-Abgabe (BEHG), Ersatzbeschaffungen (Position mit Nutzungsdauer < T)
RW_T  Restwert linear: Investition · (Restnutzungsdauer / Nutzungsdauer)
i     Kalkulationszinssatz;  T  Betrachtungszeitraum
```

Abgeleitete Kennzahlen: **Kapitalwert KW** (Hauptkriterium; > 0 = wirtschaftlich),
**Annuität des Kapitalwerts** `KW · a(i,T)` (vergleichbar mit dem €/a-Überschuss des
Alt-Verfahrens), **dynamische Amortisationszeit** (erstes t mit kumuliertem Barwert
≥ 0; „–" wenn nie), optional **interner Zinsfuß** (Nullstelle KW(i)) und
**Kapitalwertrate** KW/I₀. Wärme-/Stromgestehungskosten weiterhin nach 2.6.

**Verhältnis zum Alt-Verfahren:** identisches Zahlungsgerüst (Kapitel 2 = Katalog
der E_t/A_t-Zeilen), aber Barwertsummen statt Annuitätenvergleich. Die
`Tab_Wirtschaftlichkeit_kap`-Jahresreihe des Alt-Excel dient als
**Validierungsreferenz** für die Jahreswerte vor Abzinsung; die Zinsreduktion je
Gewerk (2.2) wird zum positionsbezogenen Zinssatz-Override.

### 5.2 Szenarien und Sensitivität (Normanforderung)

- **Szenarien Best/Real/Worst** direkt aus `Tab_ProjektWerte` (Best-/Worst-Case für
  Betrag und Nutzungsdauer sind vorhanden, 3.3) plus Preisszenarien
  (Preissteigerungsrate ± Delta).
- **Sensitivität** mindestens über: Kalkulationszins, Energiepreissteigerung,
  Investitionshöhe, Vbh/Erzeugungsmengen. Darstellung als Tabelle
  (Parameter · −Δ · Basis · +Δ → KW) im Reiter und im Bericht.
- **Dokumentation:** jeder Rechenlauf persistiert seinen Parametersatz
  (Zeitstempel, Zins, T, Preisannahmen, Szenario) — Nachvollziehbarkeit ist
  Normbestandteil und zugleich der Nachweisblock im Bericht.

### 5.3 Die VALERI-Vorlage als Strukturvorbild *(nachgetragen)*

Die im Haus verwendete **VALERI-Auswertung** (Vorlage V7; Zellstruktur dokumentiert
im INEKON-Skill `valeri-bewertung`) konkretisiert, wie die Norm bei INEKON gelebt
wird — und liefert damit das Vorbild für Datenmodell und Darstellung:

- **Ein Tabellenblatt je Maßnahme** (hier: je **Variante**), dazu ein globales Blatt
  **„Allgemeine Unternehmensdaten"** mit Strompreis, Gaspreis, Zinssatz/WACC,
  Einspeisevergütung und Preis des flexiblen Kostenfaktors — **je Szenario**.
  → Entspricht `Tab_ProjektWirtschaftlichkeit` (5.5): globale Parameter zentral,
  nie im Einzelblatt.
- **Durchgängig drei Szenarien Worst / Erwartet / Best** — jede Eingabezelle
  dreifach; Default-Streuung als Formel um den Erwartungswert (z. B. ±10 %,
  Investition +30/−25 %), je Faktor überschreibbar.
  → Deckt sich mit Best-/Worst-Feldern in `Tab_ProjektWerte` (3.3); die
  faktorweise Schwankungsbreite wird als optionales Feld je Kostenposition geführt.
- **Zahlungsströme als Faktorlisten:** *Nutzen*-Faktoren (Einsparung Strom/Gas/
  flexibler Kostenfaktor in %, Eigennutzung/Einspeisung Strom in kWh/a, eingesparte
  Arbeitskraft, Mehrproduktion, Entsorgungsgewinn, frei belegbare €/a-Zeilen) und
  *Lasten*-Faktoren (Mehrverbräuche, Entsorgungskosten, Jahresbetriebskosten,
  **Investition zum Zeitpunkt t**, Abzahlung, zusätzliche Arbeitskraft,
  Minderproduktion) — je Faktor mit **Start-/Endjahr, Degradation und
  Preisänderungsrate**.
  → Das ist die generalisierte Form des Zahlungsgerüsts aus Kapitel 2; EPOS-Plan
  befüllt diese Faktoren automatisch aus Simulation (`Tab_Ergebnis*`), Preisen
  (`energy_*`) und Kostenpositionen (`Tab_ProjektWerte`), statt sie von Hand
  einzutragen.
- **Betriebszeit-Logik:** erstes Betriebsjahr als **relativer Index** (0 = ab dem
  ersten Betrachtungsjahr) plus Nutzungszeit — wichtige Übernahme für den
  Rechenkern: Cashflows beginnen erst ab dem Startindex.
- **Energiekosten rechnet VALERI selbst** aus kWh × globalen Preisen — übertragen
  werden Mengen, keine €-Beträge. Gleiches Prinzip im EPOS-Rechenmodul
  (`EnergieMengen` liefert Mengen, Preise kommen aus `energy_project_settings`).
- **Ergebniszellen:** Kapitalwert und Amortisationszeit **je Szenario**; darunter
  ein weitgehend formelgenerierter **Bewertungsbericht** mit definierten
  Freitextstellen (qualitative, nicht monetarisierbare Wirkungen).
  → Vorbild für den UI-Reiter (Kapitel 6) und das Berichtskapitel: Kennzahlen je
  Szenario + kurzer Empfehlungstext + Freitextfeld für qualitative Wirkungen.

Für Maßnahmen ohne eigene Energieträger-Zeile (z. B. Heizöl) nutzt die Vorlage
einen **flexiblen Kostenfaktor** mit eigenem €/kWh-Preis — EPOS-Plan braucht diese
Krücke nicht, da alle Träger über `energy_carrier` typisiert sind; das ist ein
struktureller Vorteil der App-Umsetzung gegenüber der Excel-Vorlage.

### 5.4 Ausbaustufen (aktualisiert)

| Stufe | Inhalt | Referenz |
|---|---|---|
| **W1 — Kapitalwert Basis** | Zahlungsgerüst aus Kapitel 2 mit **Durchschnittspreisen**; KW, Annuität des KW, dynamische Amortisation, Restwert; Ergebnis je Projekt (System vs. Vergleichsheizung) und je Variante | DIN EN 17463 Kern; `Tab_kurz*` als Zahlenquelle |
| **W2 — Szenarien/Detail** — **umgesetzt (Phase 7)** | Preissteigerungsraten ✓, KWKG-Vbh-Kontingent jahresscharf ✓ (ein Satz, Näherung Vbh = Betriebsstunden), BEHG-CO₂ ✓, Worst/Erwartet/Best ✓ (seit W1) + Sensitivitätstabelle ✓, interner Zinsfuß ✓; ~~Vbh-Staffel gegen KWKG 2025 prüfen/erweitern~~ **erledigt (Phase 9, 8.5)** | `Tab_Wirtschaftlichkeit_kap`, Norm-Szenarioanforderung |
| **W3 — Tarife/Emission** — **umgesetzt (Phase 8)** | HT/NT-Saisonmatrix aus Stundenwerten ✓ (vereinfachtes Modell, Referenzjahr 2026), Leistungspreis-Staffel ✓ (zweistufig), Emissionsbilanz mit Kraftwerkspark ✓ (Katalog, CO₂/SO₂/NOx), KWKG-Split ✓ | `Tab_Tarifstruktur`, `Tab_Emissionen` |

Schon **W1** füllt den neuen UI-Reiter (Kapitel 6) und das Berichtskapitel
„Wirtschaftlichkeit" des Berichts vollständig.

### 5.5 Neue Datenstrukturen (additiv, Access-kompatibel)

*Umsetzungsstand W1 (Phase 6):* beide Tabellen werden von
`WirtschaftlichkeitCtrl.StelleTabellenSicher()` automatisch angelegt — in W1 mit
dem tatsächlich benötigten Spaltenumfang (Parameter: `Zinssatz`,
`Betrachtungszeitraum`, `Preissteigerung_Energie`, `Preissteigerung_Betrieb`,
`Einspeiseverguetung`; eine Zeile je **Stamm**, gilt für die Gruppe). Die unten
stehende Vollausstattung (KWKG/EEG/Steuern/Tarife) bleibt das W2/W3-Zielbild;
Spalten werden dann additiv nachgerüstet.

**`Tab_ProjektWirtschaftlichkeit`** (1 Zeile je Projekt — Parameter des Rechenlaufs):
`ID_Projekt, Zinssatz, Zinsreduktion_BHKW, Zinsreduktion_WP, Betrachtungszeitraum,
Preissteigerung_Energie, Preissteigerung_Betrieb, CO2_Preis, USt_Satz,
KWKG_Bonus_Eigenstrom, KWKG_Bonus_Einspeisung, KWKG_Vbh_Kontingent,
EEG_Verguetung, Strompreis_Bezug, Strompreis_Restbezug, Strompreis_Einspeisung,
Leistungspreis_Bezug, Stromsteuer_Satz, Energiesteuer_Gas, Energiesteuer_Oel,
Energiesteuer_Rueckerstattung, Inbetriebnahme`
(Strompreise als Vorbelegung aus `energy_project_settings` des Trägers „Strom";
W3 ersetzt die Einzelpreise durch eine Tarifstruktur-Tabelle.)

**`Tab_ErgebnisWirtschaftlichkeit`** *(neu in Fassung 2)* — persistiertes Ergebnis
je Projekt und Szenario, damit der UI-Reiter (Kapitel 6) und der Bericht ohne
Neuberechnung anzeigen können: `ID, ID_Projekt, ID_Ergebnis (FK auf den
Simulationslauf!), Szenario (Best/Real/Worst), Zeitstempel, Kapitalwert,
AnnuitaetKW, AmortisationJahre, IRR, Investition, Restwert, ...` plus die
Jahreskosten-/Erlösfelder aus der Ergebnisklasse (6.1 im Berichtskonzept).
Der FK auf `Tab_Ergebnis.ID` schließt die heutige Lücke „Kosten ohne Bezug zum
Simulationsstand".

**Erweiterung `Tab_ProjektWerte`** (Spalten additiv): `Kostenart`
(Kapital/Betrieb/Bedarf/Erlös nach VDI-2067-Systematik), `Bemessung`
(Betrag | %_Investition | €_pro_h | €_pro_kWh), `IstErloes` (Bool, für Zuschüsse).

**`carrier_id`** in `Tab_ErgebnisBHKWModul` und `Tab_ErgebnisHeizkesselModul`
(Befund 3.5.1), gesetzt beim Speichern des Simulationslaufs.

### 5.6 Vergleichsheizung im Variantenmodell

Das Alt-Verfahren vergleicht **innerhalb einer Rechnung** System vs. Referenzkessel.
EPOS-Plan hat zusätzlich den **Variantenvergleich über Projekte** (`Tab_Variante`).
Vorschlag, beides zu verbinden:

- Die Vergleichsheizung ist **je Projekt** definiert (Referenzkessel: Investition,
  Nutzungsdauer, Wirkungsgrad, Träger) — als Kostenpositionen mit eigener
  Komponente „Vergleichsheizung" oder als Feldgruppe in `Tab_ProjektWirtschaftlichkeit`.
  In der ValERI-Logik ist sie das **Referenzszenario („Unterlassensalternative")**,
  gegen das die Zahlungsströme der Investition differenziell gebildet werden.
- Der Variantenvergleich zeigt dann je Projekt den internen Vergleich (System vs.
  Referenz, ausgedrückt im Kapitalwert) **und** quer über die Varianten die
  Kapitalwerte/Amortisationen.
- Alternative (einfacher): das Stammprojekt selbst ist die Referenz; KW der Variante
  = Barwert der Differenz-Zahlungsströme Variante − Stamm. → Entscheidung Kapitel 7.

### 5.7 Rechenmodul und Ergebnisklasse

`Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` implementiert
`IWirtschaftlichkeitProvider` (Schnittstelle im Berichtskonzept, Kapitel 6):
liest ausschließlich `Tab_Ergebnis*`, `Tab_ProjektWerte`, `energy_*`,
`Tab_ProjektWirtschaftlichkeit`; schreibt `Tab_ErgebnisWirtschaftlichkeit`; keine
UI-Abhängigkeit. Testbar über Referenzprojekte: Jahreswerte gegen die
Excel-Blattwerte (goetz_test: Kapitalkosten 1 536,08 €/a, Vergleich 217 €/a …),
Kapitalwerte gegen die `VALERI_Vorlage_V7.xlsx` (sobald lesbar).
`WirtschaftlichkeitErgebnisModel` wird um die Kapitalwert-Kennzahlen ergänzt
(Kapitalwert, AnnuitaetKW, AmortisationJahre dynamisch, IRR, Restwert, Szenario).

---

## 6. UI-Integration: Reiter „Wirtschaftlichkeit" unter Berichte & Kosten *(neu)*

**Ist-Zustand:** Der Hauptbereich **„Berichte & Kosten"** enthält die Schaltflächen
**[Kosten]** (→ `Form_Kosten`) und **[Varianten]** (→ Dialog „Projektvarianten":
Stamm-Combo, Variantenliste, Bezeichner/Anlegen/Löschen, „Simulation starten",
„Projektvergleich + Bericht").

**Soll:** Ein **weiterer Reiter/Bereich „Wirtschaftlichkeit"** zeigt die
Wirtschaftlichkeits-Ergebnisse der Vergleichsgruppe direkt in der App — vor und
unabhängig von der Berichtserzeugung:

```
┌─ Berichte & Kosten ──────────────────────────────────────────────────┐
│  [Kosten]   [Varianten]   [Wirtschaftlichkeit]                       │
│                                                                      │
│  Wirtschaftlichkeit — Stamm „Wöhler" + 2 Varianten   Szenario:[Real▾]│
│  ┌────────────────────────────┬─────────┬─────────┬─────────┐        │
│  │ Kennzahl                   │ Stamm   │ Test1   │ Test2   │        │
│  ├────────────────────────────┼─────────┼─────────┼─────────┤        │
│  │ Investition [€]            │ 20 881  │ 28 898  │ …       │        │
│  │ Kapitalwert [€]            │ −12 400 │ +3 150  │ …       │        │
│  │ Annuität des KW [€/a]      │ −1 030  │ +262    │ …       │        │
│  │ Amortisation [a]           │ –       │ 11,2    │ …       │        │
│  │ Gestehungskosten [€/kWh]   │ 0,14    │ 0,11    │ …       │        │
│  └────────────────────────────┴─────────┴─────────┴─────────┘        │
│  Parameter: i=1,5 % · T=20 a · Preise Stand 08/2026  [Berechnen]     │
└──────────────────────────────────────────────────────────────────────┘
```

**Ablauf und Vorbedingungen** (Vorgabe: „Varianten müssen zuvor ausgewählt und
berechnet worden sein — Berechnung prüfen und ggf. automatisch ausführen"):

1. **Vergleichsgruppe** = die im Varianten-Dialog gewählte Gruppe (Stamm + angehakte
   Varianten; gleiche Quelle wie der Bericht — `VergleichsDaten`).
2. Beim Öffnen des Reiters läuft die **Prüfkette je Projekt**:
   a) Simulationsergebnis vorhanden und aktuell? (Kriterien wie Aktualitätsprüfung
      im Berichtskonzept 3.4) — sonst Angebot „Jetzt simulieren" (headless
      `SimulationRunner`).
   b) Wirtschaftlichkeits-Ergebnis vorhanden (`Tab_ErgebnisWirtschaftlichkeit`) und
      **auf dem aktuellen Simulationslauf** (FK `ID_Ergebnis`) und Parameterstand? —
      sonst automatisch `WirtschaftlichkeitCtrl.Berechne()` (Sekundenbereich, kein
      Dialog nötig; Fortschritt in der Statuszeile).
   c) Fehlende Eingaben (kein Zinssatz, keine Kostenpositionen, kein Energiepreis
      für einen verwendeten Träger) → Zeile wird mit Begründung gezeigt
      („Wirtschaftlichkeit nicht berechenbar: kein Arbeitspreis für ‚Heizöl'"),
      mit Absprung in `Form_Kosten` bzw. den Parameterdialog.
3. Anzeige: Kennzahlen-Vergleichstabelle (Zeilen wie Skizze, Spalten = Projekte),
   Szenario-Umschalter (Best/Real/Worst, W2), Parameterzeile als Nachweis,
   Schaltfläche **[Berechnen]** für manuelles Neurechnen.
4. **Parameterpflege** (Zins, T, Preissteigerung, Boni): eigener kleiner Dialog
   „Wirtschaftlichkeits-Parameter" (`Tab_ProjektWirtschaftlichkeit`), erreichbar aus
   dem Reiter; Vorbelegung projektübergreifend, Overrides je Projekt.
5. Der **Variantenbericht** (Baustein „Wirtschaftlichkeit") und die Excel-Ausgabe
   lesen dieselben persistierten Ergebnisse — Reiter, Word und Excel zeigen
   garantiert identische Zahlen.

Umsetzungsdateien (Ergänzung zur Dateiliste im Berichtskonzept 7.1):
`Views/Wirtschaftlichkeit/Form_Wirtschaftlichkeit.*` (Reiter-Inhalt),
`Views/Wirtschaftlichkeit/Form_WirtschaftlichkeitParameter.*`,
`Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs`,
`Model/WirtschaftlichkeitErgebnisModel.cs` (erweitert um KW-Kennzahlen und Szenario).

---

## 7. Offene Punkte für die fachliche Vorgabe

1. ~~Methode-Grundsatz~~ **entschieden:** Kapitalwertmethode nach DIN EN 17463.
   ~~Zins/T/Restwert~~ **entschieden (11.08.2026):** Vorgabe **i = 3,0 %**,
   **T = 20 a** (beides je Stamm in `Tab_ProjektWirtschaftlichkeit` editierbar),
   **Restwert linear** (5.1). Offen bleibt die **Zinsreduktion je Gewerk**
   (W1 rechnet mit einheitlichem Projektzins; positionsbezogene Zins-Overrides
   sind W2-Kandidat).
2. ~~VALERI-Vorlagenstruktur~~ **nachgetragen (5.3)** aus der Zell-Dokumentation des
   Skills `valeri-bewertung`. Verbleibend: Formel-Feinheiten der Vorlage (exakte
   Barwert-/Aktivierungslogik, Degradationsverrechnung) direkt in
   `VALERI_Vorlage_V7.xlsx` gegenprüfen, sobald die Datei lesbar ist; Beispiele in
   `60-Tools\20-Sammung_Tools\Wirtschaftlichkeit` sichten (Ordner nicht verbunden).
3. ~~Vergleichsheizung/Referenzszenario~~ **entschieden (11.08.2026): Stammprojekt
   als Referenz** (5.6, einfache Alternative): KW der Variante = Barwert der
   Differenz-Zahlungsströme Variante − Stamm; der Stamm zeigt seinen
   Nettokosten-Barwert. Ein Referenzkessel je Projekt bleibt W2/W3-Option.
4. ~~Parameterherkunft Strompreise~~ **entschieden (11.08.2026): aus der
   Kostenmaske** (`energy_project_settings`, Träger Strom) — keine Doppelpflege.
   In `Tab_ProjektWirtschaftlichkeit` stehen nur Zins, T, Preissteigerungen und
   die Einspeisevergütung.
5. **Steuersätze/Boni als Stammdaten:** Energiesteuer-, Stromsteuer-, KWKG-, EEG-Sätze
   ändern sich gesetzlich — Katalogtabelle mit Gültigkeitsdatum (analog `energy_price`)
   oder Projekteingabe? → **Bekräftigt durch das KWKG 2025** (degressive Vbh-Staffel,
   Stichtagslogik, Sätze je Gesetzesstand): Kapitel 8.5 empfiehlt die
   **Katalogtabelle mit Gültigkeitsdatum**.
6. **Gültigkeit je Erzeugertyp:** Blattfamilie ist BHKW-zentriert; `Tab_kurz_WP` zeigt
   die WP-Variante. Was gilt für PV-Only-/Hybrid-Varianten (EEG statt KWKG,
   Eigenverbrauchsquote)?
7. **MwSt:** Alt-Verfahren rechnet „ohne MwSt" — bleibt Netto verbindlich?
8. ~~Emissionsbilanz W3~~ **entschieden (11.08.2026): Katalogtabelle
   `Tab_Kraftwerkspark`**, beim ersten Start vorbefüllt (Dt. Strommix, Erdgas-GuD,
   Steinkohle) und in den Kenndaten pflegbar; Auswahl je Stamm im Parameterdialog.
9. **Validierungsfälle:** `goetz_test.XLS` (BHKW/Öl), `englmar haus der
   gastlichkeit.XLS` (Realprojekt) für das Zahlungsgerüst; `VALERI_Vorlage_V7.xlsx`
   für die Kapitalwert-Kennzahlen.
10. ~~KWKG-2025-Nachtrag im Rechenmodul~~ **umgesetzt (Phase 9, 12.08.2026)**
    als W2-Nachtrag komplett: Vbh-Staffel bis „ab 2030: 2 500"
    (Katalog `Tab_KWKG_Staffel`), Förderfähigkeits-Stichtag + 4-Jahres-Frist,
    Negativpreis-Abschlag (%-Näherung), > 500-kW- und Heizöl-Guard,
    Novellen-Sensitivität. Offen nur die exakte Spotpreis-Stundenrechnung (8.5.4).


---

## 8. Rechtsstand KWKG 2025/2026 und Nachtrag für das Rechenmodul *(neu, 12.08.2026)*

*(Web-Recherche 12.08.2026. Kapitel 1/2 dokumentieren die Excel-Mappe auf Stand
KWKG 2016/2020 — dieser Abschnitt hält den geltenden Rechtsstand für das
Rechenmodul fest. Keine Rechtsberatung; vor Projektzusagen am Gesetzestext bzw.
BAFA-Merkblatt prüfen.)*

### 8.1 Gesetzesstand

- **KWKG 2025 in Kraft:** Gesetz zur Änderung des KWKG und der KWKAusV vom
  21.02.2025 (**BGBl. 2025 I Nr. 54**, verkündet 25.02.2025), wesentliche Teile
  **in Kraft seit 01.04.2025**; zuletzt geändert durch Art. 24 des Gesetzes vom
  18.12.2025 (BGBl. 2025 I Nr. 347).
- **Große Modernisierungsnovelle steht aus:** Stand 12.08.2026 kein
  Referentenentwurf; BMWE-Zusage (10.07.2026): Vorlage „bis Ende 2026". Verbände
  (BDEW/AGFW/VKU/B.KWK) fordern Inkrafttreten spätestens 01.01.2027 und
  Verlängerung bis 2035/2038.
- **EuGH-Urteil 09.07.2026 (C-242/24 P):** Die KWKG-Förderung ist **keine
  staatliche Beihilfe** → künftige Änderungen brauchen keine EU-Notifizierung
  mehr; der jahrelange Hauptbremsklotz der Novellen entfällt.

### 8.2 Fristenlogik § 6 KWKG 2025 — Stichtag ist nicht mehr die Inbetriebnahme

Zuschlagberechtigt ist eine Anlage, wenn sie **einen** der drei Pfade erfüllt:

| Pfad | Bedingung bis **31.12.2026** | Realisierungsfrist |
|---|---|---|
| 1 | Aufnahme des Dauerbetriebs | — |
| 2 | BImSchG-Genehmigung erteilt | Dauerbetrieb bis Ablauf des 4. Jahres nach Genehmigungserteilung (bis Ende 2030 möglich) |
| 3 | **verbindliche Bestellung** (Anlagen ohne BImSchG-Pflicht — der typische EPOS-Fall kleiner BHKW) | 4 Jahre ab Bestellung |

Anlagen mit Genehmigung/Bestellung **nach dem 31.12.2026** erhalten nach geltendem
Recht **keine Förderung** (änderbar nur durch die ausstehende Novelle).
Wärme-/Kältenetze analog: Investitionsentscheidung + landesrechtliche Genehmigungen
bis 31.12.2026, 4 Jahre Realisierung; Fördersatz einheitlich 40 %, Höchstbetrag
50 Mio. € je Projekt.

**Modellkonsequenz:** Der harte Stichtag für die Förderfähigkeit ist das
**Bestell-/Genehmigungsdatum**, nicht das Inbetriebnahmedatum. Für
Wärmeplanungsprojekte mit Realisierung 2027–2030 entscheidet die Bestellung bis
Ende 2026 über den KWKG-Bonus.

### 8.3 Vbh-Staffel § 8 KWKG — Korrektur der Blatt-Tabelle „ab 2025: 3 500"

Die Blatt-Tabelle des Alt-Verfahrens (2.7) endet mit „ab 2025: 3 500 Vbh"
(KWKG-2020-Stand). Die geltende Staffel der je Kalenderjahr **maximal vergüteten
Vollbenutzungsstunden** läuft degressiv weiter:

| Jahr | 2020–2022 | 2023/2024 | 2025 | 2026 | 2027 | 2028 | 2029 | ab 2030 |
|---|---|---|---|---|---|---|---|---|
| max. Vbh/a | 5 000 | 4 000 | 3 500 | **3 300** | **3 100** | **2 900** | **2 700** | **2 500** |

Gesamtkontingent für neue Anlagen unverändert **30 000 Vbh**. Durch die sinkenden
Jahresdeckel streckt sich die Auszahlung über deutlich mehr Kalenderjahre —
**barwertrelevant**, da spätere Jahre stärker abgezinst werden.

### 8.4 Weitere modellrelevante Punkte des KWKG 2025

- **Zuschlagssätze § 7 unverändert.** Einspeisung je Leistungsanteil: bis 50 kW
  8,0 · 50–100 kW 6,0 · 100–250 kW 5,0 · 250 kW–2 MW 4,4 · über 2 MW 3,4
  (nachgerüstet 3,1) ct/kWh. **Neuanlagen bis 50 kW nach § 7 Abs. 3a: 16 ct/kWh
  Einspeisung / 8 ct/kWh Eigenversorgung**; Eigenversorgung sonst je Klasse
  ca. 1,0–5,41 ct/kWh.
- **Kein Zuschlag bei Spotpreis ≤ 0 €/MWh** (§ 7 Abs. 5) — gilt seit 01.04.2025
  **auch für Anlagen unter 50 kW** (Privilegierung entfallen).
- **Heizöl-BHKW nicht mehr förderfähig** (fossile flüssige Brennstoffe raus; bei
  neuen/modernisierten Anlagen nur noch Erdgas), Emissionsgrenze **unter
  270 g CO₂/kWh** (Strom + Nutzwärme) für Neuanlagen. → Der Validierungsfall
  `goetz_test.XLS` (BHKW/Öl) bleibt fürs Zahlungsgerüst gültig; ein neues
  Öl-BHKW bekäme heute aber KWKG-Bonus = 0.
- **Ausschreibungslücke über 500 kW:** Die KWKAusV regelt Volumina nur bis 2025;
  letzter Gebotstermin 01.12.2025 (Ø-Zuschlag 5,02 ct/kWh KWK, 6,67 ct/kWh iKWK).
  Für Anlagen **über 500 kW bis 50 MW** existiert seit 01.01.2026 **kein
  Förderweg**. Anlagen **bis 500 kW** (EPOS-Zielgruppe) erhalten den Zuschlag
  weiterhin **gesetzlich direkt, ohne Ausschreibung**.

### 8.5 Konsequenzen für das Rechenmodul *(umgesetzt in Phase 9, 12.08.2026 — bis auf die exakte Spotpreis-Stundenrechnung in Punkt 4)*

1. **Vbh-Staffel jahresscharf fortschreiben (8.3):** Der W2-KWKG-Bonus (Phase 7)
   rechnet den Jahresdeckel bereits jahresscharf — Staffelwerte gegen KWKG 2025
   prüfen und bis „ab 2030: 2 500" erweitern. Ablage als **Katalogtabelle mit
   Gültigkeitsdatum** (analog `energy_price`) statt Konstanten; entscheidet
   zugleich Frage 7.5.
2. **Förderfähigkeits-Stichtag als Parameter:** `Tab_ProjektWirtschaftlichkeit`
   (5.5) um **Bestell-/Genehmigungsdatum** ergänzen (das vorhandene Feld
   `Inbetriebnahme` genügt nicht). Prüfregel: Datum ≤ 31.12.2026 **und**
   Inbetriebnahme binnen 4 Jahren → Bonus aktiv, sonst 0.
3. **Gesetzesstand als Kennung:** analog `Tab_kurz_KWKG2016/2020` eine Kennung
   **KWKG 2016 / 2020 / 2025** je Rechnung (Bestandsanlagen rechnen mit ihrem
   historischen Satz weiter) — praktisch abgebildet über die Gültigkeitsdaten
   der Katalogtabelle aus Punkt 1.
4. **Negativpreis-Abschlag:** vergütete Vbh um Stunden mit Spotpreis ≤ 0 kürzen —
   als W2-Näherung ein Prozent-Parameter („Abschlag Negativstunden"), später
   exakt über die W3-Stundenmatrix.
5. **Anlagen über 500 kW: Bonus = 0** ansetzen (Ausschreibungslücke seit
   01.01.2026); optional Sensitivität mit 5,02 ct/kWh (letzter Ausschreibungswert)
   für den Fall einer Wiedereinführung.
6. **Brennstoff-Guard:** KWKG-Bonus nur für zulässige Träger (kein Heizöl bei
   Neuanlagen) — prüfbar über `carrier_id` (3.5.1). *Umsetzung Phase 9: Prüfung
   über die Brennstoff-Kategorie „Öl" (`Tab_Brennstoff_Stamm.ID_Kategorie = 2`) —
   dokumentierte Näherung: sie erfasst auch Bio-Heizöl-Blends (Heizöl Bio 5–20);
   der Guard greift nur bei erkennbaren Neuanlagen (Inbetriebnahme ≥ 2025),
   Bestandsanlagen rechnen weiter. Die Realisierungsfrist wird als „Ablauf des
   4. Jahres nach dem Stichtag" geprüft (§ 6 Pfad 2; für den Bestellungs-Pfad 3
   um maximal ein Jahr großzügig).*
7. **Novellen-Szenarien im Bericht:** Förderung für Bestellung/Genehmigung nach
   2026 als **Regulierungsrisiko** ausweisen; Sensitivität mit 0 % /
   Fortschreibung heutiger Sätze (Verbandsforderung: Verlängerung bis 2035/2038).

**Quellen (Recherche 12.08.2026):** BGBl. 2025 I Nr. 54 (KWKG-ÄndG,
recht.bund.de) · BAFA-Mitteilung „Änderung des KWKG zum 01.04.2025" ·
BNetzA-Pressemitteilung 15.01.2026 (letzte KWK-/iKWK-Ausschreibung) ·
EuGH C-242/24 P (09.07.2026) · BDEW-Pressemitteilung 09.07.2026 ·
ZFK-Energiegesetze-Ticker (Stand 07.08.2026).

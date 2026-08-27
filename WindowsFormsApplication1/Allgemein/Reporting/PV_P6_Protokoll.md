# PV P6 — Gesamtabnahme und Ergebnisausweis (Protokoll)

Etappen **P5-Rest + P6** des Konzepts Photovoltaik-Wirtschaftlichkeit
(Rev. 1 + Nachtrag 1, §§ 6.3/6.4, N.3/N.4), umgesetzt 26.08.2026 auf Branch
`kostenformulare`. Baut auf P1–P5 auf (`PV_P1-P5_Protokoll.md`).

## Umgesetzt

### § 6.4 — Ergebnis-Persistenz und Ausweiszeilen

- `Tab_ErgebnisWirtschaftlichkeit` additiv über `SpalteSicher` (Bestandsmuster):
  `PvVerguetungsform`, `PvAnzulegenderWert` [ct/kWh, Mix], `PvMarktpraemie`
  [€/a, Jahr 1], `PvVerguetungsausfallKwh`, `PvVerguetungsausfall` [€/a],
  `PvKompensation51a` [€], `PvKappungsverlustKwh`, `PvVermiedenerBezug`
  [€/a, informativ]. Gefüllt nur bei aktivem Vergütungsdialog; Übernahme aus
  `PvErloesErgebnis` in `RechneProjekt`, Rückweg über `LadeErgebnisse`.
- **Vermiedener Bezug** = (Erzeugung − Überschuss) × Strom-Arbeitspreis der
  Kostenkette (`custom_price` vor `price`, 0 = nicht gepflegt; Stromträger
  wie `KostenEmissionRechner.FindeStromTraeger`). Reiner AUSWEIS — der
  Kapitalwert rechnet mit den tatsächlichen Reststromkosten (dieselbe
  Begründung wie bei den E5-Zeilen).
- **PV-Block in `WirtschaftlichkeitZeilen`** (die eine Wahrheit für
  Ergebnisreiter, Word-Baustein und Excel-Generator, E7-Muster): Formtext in
  Klartext, AW_mix, Marktprämie/Ausfall/§ 51a/Kappung/vermiedener Bezug nach
  dem Nullzeilen-Muster; ohne aktiven Dialog erscheint KEINE Zeile
  (Bestandsberichte unverändert). 12 `WIRT_ZEILE_PV_*`-Schlüssel de + en.

### N.3 Nr. 3 — Kennzahlen der Dialog-Vorschau

Neuer UI-freier `PvKennzahlenRechner` (+ `lblKennzahlen` in Gruppe 7):

- **LCOE₀** (KZS = 0 — nur dieser Wert ist mit einem Vergütungssatz
  vergleichbar) und **LCOE diskontiert** (pv@now-Definition: Ausgaben UND
  Menge abgezinst) aus PV-Investition/-Betriebskosten der Kostenwelt
  (`LiesKomponentenSummen`, Komponente Photovoltaik), Erzeugung des Laufs
  und Zins/T der Wirtschaftlichkeit. Menge konstant (EPOS führt keine
  Moduldegradation — dokumentierte Abweichung, Sensitivität lt.
  Modellabgleich ~5–7 % auf den LCOE).
- **Eigenverbrauchsquote und Autarkiegrad STETS als Paar** (N.3-Regel);
  mit Speicherlauf kommen beide aus der Speicherrechnung („mit Speicher"),
  sonst aus dem PV-Aggregat.
- **„Vorteil durch PV je Jahr"** (pv@now-Definition: undiskontierter
  Liquiditätsüberschuss über die Laufzeit ÷ Jahre).
- Fehlende Grundlagen werden BENANNT statt still 0 gerechnet („PV-Kosten
  nicht gepflegt — LCOE und Vorteil entfallen", „Strom-Arbeitspreis nicht
  gepflegt — Ersparnisanteil entfällt").

### § 6.3 — Marktwert-Import und Verifikation

- `ProjektPhotovoltaikCtrl.ImportiereMarktwerteCsv` + Knopf „Marktwerte
  importieren…" im Dialog: versteht den netztransparenz-Export (Kopfzeile
  mit „Solar"-Spalte; Datumsformen MM.JJJJ, JJJJ-MM, Monatsname JJJJ) und
  die einfache Liste `Jahr;Monat;Wert`; Dezimalkomma/-punkt, UTF-8/ANSI.
  Je Jahr wird die Stammreihe ERSETZT (CSV = Quelle der Wahrheit);
  angebrochene Jahre nur lückenlos ab Januar.
- **Saat-Verifikation** (extern, 26.08.2026): Monatswerte 2024-06 (4,635),
  2025-04 (3,041), 2025-06 (1,843), 2026-03 (5,455), 2026-04 (1,317),
  2026-06 (6,190) sowie beide Jahresmarktwerte 4,624/4,508 ct/kWh gegen
  pv-magazine/DGS/Photon (Primärquelle netztransparenz) bestätigt — dazu
  die 9 Stichproben aus der Konzepterstellung. Vollabgleich aller 31 Werte
  jederzeit über den Importer möglich (netztransparenz-CSV ist
  registrierungspflichtig, API-Portal).

### N.4 — INEKON-Referenzabnahme „Schulung 01"

Der Fall (30 kWp · 1.078 kWh/kWp · Minderung 5 % in Jahresstufen · Rumpfjahr
3–12/2023 · Netzstrompreis 30 ct +4 %/a · KZS 3,5 % · 250 Monate) wurde mit
jahresscharfen Erlösreihen auf dem EPOS-`KapitalwertRechner` nachgebildet
(T = 21 inkl. Rumpfjahr; Ertrag 92,07 %/Betrieb 10/12 im Jahr 1):

| Plan | pv@now-Soll | EPOS (konventionsangeglichen) | Abweichung |
|---|---|---|---|
| Überschuss | +92.568 € | +91.867 € | **−0,76 %** ✓ |
| Volleinspeisung | −22.979 € | −23.087 € | **−0,47 %** ✓ |

**Abnahmekriterium ±1 % erfüllt.** Dokumentierte Abweichungsquelle
Zeitkonvention: EPOS zinst nachschüssig (Jahresende — Bestandskonvention
ALLER Projekte, unangetastet), pv@now monatlich (≈ Jahresmitte); der
Angleich hebt die laufenden Zahlungen um q^0,5. Rohwerte ohne Angleich:
+89.379 € (−3,44 %) / −23.614 € (−2,76 %) — reine Konventionsdifferenz,
im Smoke mitgeprüft. LCOE₀ der Kennzahlformel: 14,657 ct/kWh gegen pv@now
14,64 (Rest = Minderung/Rumpfjahr, s. o.).

### P1-Vermerk — V1-Realabnahme Projekt 1018 (Produktiv-DB)

Auf einer KOPIE der Produktiv-DB (`%ProgramData%\EPOS_PLAN`, Stand 41)
wurde 1018 headless neu simuliert (`pv6real`-Modus): Das Projekt (BHKW ohne
PV-Gewerk) führt seit P1 **keine PV-Überschussreihe mehr** — der früher
falsch etikettierte BHKW-Anteil (Befund: 24.532 negative Viertelstunden)
ist weg, es existiert keine PV-Erlösbasis. 4/4 PASS; die Produktiv-DB
selbst blieb unberührt.

## Nachweise

- **pv6-Smoke 28/28**: I1–I5 INEKON (Rechenweg bitgenau gegen unabhängige
  Handrechnung, ±1 % nach Angleich, Rohabstand < 4 %), K1–K6 Kennzahlen
  gegen Handrechnungen (EV-Quote 83,37 %/Autarkie 26,96 %, LCOE₀/LCOE,
  Vorteil 1.600 €/a synthetisch, Speicher-Vorrang, Paar-Anzeige), Z1–Z3
  Ausweiszeilen (Vollblock, Klartext-Form, OHNE Dialog keine Zeile),
  P0–P5 Persistenz-Roundtrip auf 1007 (echter Sammler-Lauf mit Zeitreihen,
  DB-Rückweg, Neutralität bei inaktivem Dialog), M1–M6 CSV-Import (beide
  Formate, drei Datumsschreibweisen, Ersetzen statt Verdoppeln,
  Lücken-Ablehnung, Saat unberührt).
- **pv6real 4/4** (Projekt 1018, s. o.).
- **Regression**: pv1 11/11 · pv2 37/37 · pv3 20/20 · pv4 24/24 ·
  pv5 14/14 · kd2–kd6 grün · Layout-Sweep 120 Formulare 0 Befunde.
- **Sichtbeleg**: `pv6_dialog_kennzahlen.png`.

## Bewusst offen

- Word/Excel führen die PV-Zeilen automatisch über
  `WirtschaftlichkeitZeilen`; ein eigener PV-KAPITEL-Baustein (Prosa) ist
  nicht beauftragt.
- Vollabgleich aller 31 Saatwerte gegen die netztransparenz-CSV (Download
  registrierungspflichtig) — per Import-Knopf jederzeit nachholbar.
- Sichtprüfung Philipp (Dialog, Ergebnisreiter, Bericht).

# B3b — Hilfsstrom, Nettostromerzeugung, Eigenstrom-Tatbestand je Anlage (Umsetzungsprotokoll)

Zweiter und letzter Implementierungsteil der Etappe B3
(`Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md` §§ 4.3/4.4/4.5, BW6). Stand
30.08.2026, Branch `Pufferspeicher`; setzt auf B3a (a3c4b35) auf. Kein neuer
Schemaschritt — Schritt 61 trägt alles, `ZIEL_VERSION` bleibt 61, **neue Schritte
weiter ab 62**.

## 1. Der EINE Netto-Ort: `HilfsstromRechner` (neu)

`Allgemein/Wirtschaftlichkeit/HilfsstromRechner.cs`:
- `MengeMWh(anteil, endenergie)` — die einzige Stelle, an der der Anlagen-Anteil
  (`Hilfsenergie_Anteil`, „0/NULL = keine") zur Menge wird (Bemessung an der
  Endenergie der Anlage = Brennstoff, § 4.5).
- `NettoSplit(hilfs, ref eigen, ref einsp)` — die einzige Stelle der Regel
  „**Eigenverbrauch zuerst, dann Einspeisung**" (Hilfsstrom ist physisch
  Eigenverbrauch; nie negativ, Summe stets `max(0, E+F−H)`).
- `JeModul(...)` für den Persistenz-Ausweis (Anteile lesen, Modul→Anlage-Zuordnung,
  Formel).

**Wer nettet, wer bleibt brutto** (jeweils mit Paragraphen-Kommentar im Code):

| Pfad | netto/brutto |
|---|---|
| KWKG-Zuschlagsmengen (`BaueKwkgReihe` → `ReiheJeAnlage`/`ReiheProjektweit`): Anlagen-Strom, Anteilsbildung (Zähler UND Nenner), Eigen-/Einspeisemengen | **netto** — einmal in `BaueKwkgReihe` gebildet |
| `StromMatrix` selbst | unangetastet (Brutto-Welt für Bezugskosten, Einspeiseerlös, Stromsteuer) |
| `SteuerEingabe.KwkEigenMWh` (§ 9 Abs. 1 Nr. 3) | **brutto** — § 4.3: der Hilfsstrom bleibt Teil des befreiten Eigenverbrauchs, er mindert nur die zuschlagsfähige KWKG-Nettoerzeugung |
| `SteuerAnlage.StromMWh` (Anteilsbereinigung, CO₂-Grenzwert je kWh) | **brutto** — ein Abzug erhöhte die spezifischen Emissionen künstlich |
| `VbhElektrisch`/`VbhDerAnlage` | **brutto** — Auslegungsgröße, steht in Zähler und Nenner desselben Bruchs |

## 2. Weitere Bausteine

- **Persistenz-Ausweis** (§ 5.2): `ErgebnisCtrl.Save` füllt `Modul.Hilfsenergie`
  [MWh/a] für BHKW **und** Kessel aus derselben Helferfunktion (Kessel über die
  B3a-Bemessung `KesselBrennstoffMWh`, jetzt `internal`); Kessel-Hilfsstrom ist
  reiner Ausweis ohne Strommengenwirkung (im Code begründet). Die Wirtschaftlichkeit
  liest die Spalte nicht — sie rechnet frisch (H2-Prinzip).
- **Eigenstrom-Tatbestand § 6 Abs. 3 je Anlage** (§ 4.4): `SatzEigenDerAnlage` —
  trägt eine Anlage einen eigenen `KWKG_Satz_Eigen`, braucht sie einen Tatbestand
  (`KWKG_Eigenstromfall` ?? Projektwert); ohne ihn Satz 0 mit Meldung. Bewusst
  strenger als der Projektweg (dort lässt K6 aus Bestandsschutz den Satz stehen) —
  tragfähig, weil `KWKG_Satz_Eigen` im gesamten Bestand NULL ist (0 von 146
  gemessen).
- **Doppelpflege-Warnung** (`KohaerenzPruefung`, neue Zeilenart): Anlagen-Anteil > 0
  UND aktive Hilfsenergie-Kostenposition an derselben Anlage → genau eine WARNUNG
  („verrechnet wird nichts"); läuft vor der Steuerprüfung, auch an reinen
  WP-Projekten; Katalog-Spielarten per Präfixvergleich in C#.
- **`KwkgModulNachweis`** führt jetzt die Mengenkette Brutto − Hilfsstrom = Netto →
  Eigen/Einspeisung je Anlage — die B6-Herleitungstafel findet sie fertig vor.
- **Zusatz (BK1-Anschluss)**: `RechneProjekt` hängt den Strommix-Rückfall der
  CO₂-Bilanz (`VariantenDaten.CO2StrommixRueckfall`) an den bestehenden
  Hinweiskanal.

## 3. Nachweise (Harness `..\dev\b3b\` + Referenz-Worktree `..\dev\b3bref\`)

Build x64 Exit 0. Produktiv-DB unberührt. **Neutralitätsbeweis als A/B**: Die
B3a-Protokollanker waren durch die parallele BK1-Etappe und die Anwender-Datenlage
überholt — deshalb wurde der Codestand **vor B3b** (bb19452) in einem
Wegwerf-Worktree gebaut und gegen **dieselbe** frische Kopie gemessen:

| Größe (Anteil NULL überall) | vorher (bb19452) | nachher (B3b) |
|---|---|---|
| 1030 Vorgabe, KW | −21.875.189,06 | identisch |
| 1030 § 53: EnergieSt / KW | 6.369,49 / −21.780.427,16 | identisch |
| 1030 Hocheffizienz: § 9-Befreiung / KW | 8.854,22 / −21.743.460,61 | identisch |
| 1024 Betrieb / KW | 99,00 / −2.220.322,32 | identisch |
| KWKG 1030 Jahr 1 / Reihen-Barwert / je Anlage / Brutto-Split | 7.322,68 / 59.493,09 / 6.205,62 + 1.117,06 / 431,913211 + 0,392000 | identisch |

**Hilfsstrom-Wirkung** (5 % an beiden 1030-BHKW, Kessel 8 %):
- Persistenz exakt: 862,18 → **43,11**; 186,09 → **9,30**; Kessel 5.403,10 → **432,25** MWh/a.
- Mengenkette exakt: Netto-Split Eigen **379,499711** / Einsp **0,392000** ==
  Handrechnung == Summe der Anlagenzeilen; KWKG-Boni je Anlage exakt.
- Grenzfall Hilfsstrom > Eigen (Anteil 41,22 %): Eigen **0,000000**, Einspeisung
  0,196000 — nie negativ; `NettoSplit` zusätzlich als reine Funktion durchgetestet
  (0/30/100/110/500).
- **§ 9 Nr. 3 unverändert brutto**: 8.854,22 €/a mit und ohne Hilfsstrom, Delta 0,00.
- **KW-Delta == abgezinstes KWKG-Reihen-Delta**: −7.251,15 == −7.251,15
  (Abweichung −0,000000); alle übrigen Größen (Energie-/Betriebskosten,
  Einspeiseerlös, Invest, CO₂, Energiesteuer, beide Stromsteuerzeilen, Vbh,
  Matrix-Bruttomengen) identisch.
- Eigenstromfall je Anlage: ohne Tatbestand Satz 0 + Meldung; mit
  `NR2_KUNDENANLAGE` 18.594,38 == Handrechnung; `KEINER` → 0 mit eigener Meldung;
  Nachbaranlage in allen Fällen unverändert.
- Doppelpflege: nur bei „beides gepflegt" genau 1 WARNUNG.
- Strommix-Hinweis: erscheint genau ohne Stromträger.
- Sweep: kein `<<<<<<<`-Treffer; Kodierung/Zeilenenden aller Dateien unverändert.

## 4. Hinweise zur Ankerlage

Die im B3a-Protokoll genannten 1030-Anker (−22.132.957,00 / 6.330,30) sind durch
BK1 und die laufende Anwenderarbeit an 1030 **überholt**; der stabilste Anker
bleibt **1024 Betrieb = 99,00 / KW −2.220.322,32**. Künftige Regressionsmessungen
an 1030 erst nach der Kaskaden-Wiederherstellung neu verankern.

## 5. Geänderte Dateien

```
Allgemein/Wirtschaftlichkeit/HilfsstromRechner.cs        NEU — der eine Netto-Ort
Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs   Anteil-Lesen, Netto in BaueKwkgReihe,
                                                          SatzEigenDerAnlage, Strommix-Anschluss
Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitDaten.cs  KwkgModulNachweis-Mengenkette
Allgemein/Wirtschaftlichkeit/KohaerenzPruefung.cs        HilfsenergieDoppelpflege
Controller/ErgebnisCtrl.cs                               Hilfsenergie-Ausweis beim Save
Allgemein/Reporting/B3b_Hilfsstrom_Protokoll.md          dieses Protokoll
```

Text-Keys (WIRT_KWKG_*, WIRT_CO2_STROMMIX_RUECKFALL, KOH_HILFSENERGIE_DOPPELT)
laufen über das GetString-Rückfallmuster; resx-Sammelnachtrag folgt gebündelt mit
den B3a-/F-Serien-Keys. Harnesse (gitignored): `..\dev\b3b\`, `..\dev\b3bref\`.

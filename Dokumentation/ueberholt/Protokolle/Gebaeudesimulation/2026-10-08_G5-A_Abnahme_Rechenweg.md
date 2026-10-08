# Protokoll G5-A — Abnahme der Stufe G5 am Rechenweg (08.10.2026)

**Sitzung:** Gebäudesimulation. Zweig `claude/inspiring-bell-b8wq90` auf `da03e5333` (= `ios_migration_september`). Commit der Wache `e75aa6871`.
**Entscheid:** E101; Abstimmung [G5 IFC](../../../aktuell/Gebaeudesimulation/2026-10-07_Abstimmung_G5_IFC.md), Abschnitte 2 (A1–A6), 4, 6 und 8.

## 1 Gegenstand

Abnahme der Stufe G5 (Geometrieableitung aus IFC-Körpern) durch die Sitzung Gebäudesimulation am Rechenweg: A6 für G5-3, A1–A5 als
Stichprobe. Am Import und am Rechenweg ist nichts geändert; was nicht stimmt, steht als Befund an die Sitzung IFC in Abschnitt 4.

## 2 Stand

Abgenommen wird der gebaute Stand der Sitzung IFC: G5-1 Bauteilkörper (#801), G5-2 Öffnungen (#802), G5-0 Schemaschritt 197
`Tab_Bauteil.Flaechenherkunft` (#805), Nachbesserung Körpervergleich (#808), G5-N Nordrichtung, Schritt 199 (#813), G5-3 mit
Farbmodus „Befund“ und G5-3d (#814). Protokolle: [G5-1 und G5-2](2026-10-07_G5-1_G5-2_Bauteilkoerper_Oeffnungen.md),
[G5-0](2026-10-07_G5-0_Flaechenherkunft_Schritt197.md), [Nachbesserung](2026-10-07_G5_Nachbesserung_Koerpervergleich.md),
[G5-N](2026-10-07_G5-N_Nordrichtung_Schritt199.md), [G5-3](2026-10-08_G5-3_Befund_Koerperflaechen.md),
[Körper aus Flächen](2026-10-08_K_Koerper_aus_Flaechen.md).

Gemessen ist mit dem Importweg der Tests der Sitzung IFC (`GebaeudeImportAblauf.Lesen` mit `IfcImportProfil`,
`GebaeudeBauteilvorschlag.BildenMitZonen`, `GebaeudeZonenCtrl.VorschlagSchreiben`) und der Mehrzonenrechnung nach VDI 6007
(`SimulationWaermebedarf.HeizwaermeEinesGebaeudes`) in Arbeitskopien der Testdatenbank, Gebäude, Klimaregion und Einstellungen des
Referenzprojekts 1045. Die Zahlen schreibt die Wache `EPOS.Kern.Tests/G5AbnahmeRechenwegTests` in ihre Ausgabe.

## 3 Ergebnis je Abnahmepunkt

### 3.1 Referenzlauf (A6, erster Teil) — erfüllt

Gate der Orchestrierung auf `cbc5fe835` (enthält G5-3 und G5-3d, #814): 24/24 Projekte PASS gegen
`2026-10-07_R43_Kaelteseite_AK3K`, 759/759 CSV byte-gleich. Hier nicht wiederholt.

### 3.2 Körperfläche gegen Mengensatz (A5, Schwelle 2 %) — erfüllt

Je Bauteil mit Körper die Körperfläche gegen den Mengensatz, maßgeblich die kleinere Abweichung gegen Brutto- und Nettomenge (so
vergleicht der Import). In allen Fällen blieb der Mengensatz Quelle der Fläche (`Flaechenherkunft = Mengensatz` am Abbild; an der
Kleinhausprobe in der Zone `RAUMGRENZE`, weil dort die Raumgrenzen vorgehen).

| Probe | Bauteile verglichen | still (≤ 2 %) | über 2 % | Meldung |
|---|---|---|---|---|
| `ifc4_g5_kleinhaus_mit_mengen.ifc` | 17 | 16 (15 × 0 %, Wand Süd OG 1,27 %) | Wand Nord OG 2,18 % | Info Abweichungen Außenwand (1; 2,2 %) |
| `ifc4_g5_mengen_gegenprobe.ifc` | 4 | 3 (0,79 %, 0 %, 0,50 %) | Wand Nord 4,76 % | Info Abweichungen Außenwand (1; 4,8 %) |
| `ifc4_g5_oeffnungen_mengen.ifc` | 6 | 6 (alle 0 %) | — | keine |
| `ifc4_g5_abweichungen.ifc` | 4 | Wand West 0,99 % | Süd 5,66 %, Ost 4,76 %, Nord 28,57 % | Info (3; Median 5,7 %; größte 28,6 %), Warnung Wand Nord (≥ 25 %) |
| `ifc4_g5_flachdach_teile.ifc` | 2 | Dach A mit zwei Teilen 0 % (netto 228 m²) | Dach B 2,44 % | Info Abweichungen Dach (1; 2,4 %), Info Teile Dach (1) |
| `ifc4_g5_aussparung.ifc` | 1 | — | Wand Nord: Körper 25 m² gegen Teile 23 m² (8,70 %) | Info Körperrest (Wand Nord); die Teile behalten ihren Mengensatz, der Körper trägt nur den Rest |

Keine Abweichung über 2 % bleibt still; die Zahl je Bauteilart stimmt mit der Zusammenfassung des Imports überein, ab 25 % steht das
Bauteil mit Namen in einer Warnung.

### 3.3 Heizwärme ohne Mengensätze (A6) — erfüllt, mit Befund am Bezug

| Variante | Weg | Jahresheizwärme | Heizlast (Spitze) | gegen Mengensatz | opake Fläche | Flächenherkunft |
|---|---|---|---|---|---|---|
| Körperweg (G5-3) | `kleinhaus_ohne_mengen` | 10,090 MWh/a | 6,873 kW | 0,0 % / 0,0 % | 438,99 m² | 16 KOERPER, 3 MENGENSATZ (Fenster) |
| Rückfall ohne Raumzuordnung | dieselbe Probe, `IfcImportProfil.KoerperflaechenAus` | 13,125 MWh/a | 7,432 kW | +30,1 % / +8,1 % | 488,39 m² | 18 KOERPER, 3 MENGENSATZ |
| Mengensatzweg (Bezug) | `kleinhaus_mit_mengen`, Orientierung ergänzt | 10,090 MWh/a | 6,873 kW | — | 438,99 m² | 16 RAUMGRENZE, 3 MENGENSATZ |

- Der Körperweg trifft den Mengensatzweg in jeder Bauteilzeile (Fläche, Neigung, Azimut, Randbedingung) und damit in Heizwärme und
  Heizlast; der Rückfall liegt 30 % darüber. Die Erwartung „Körperweg näher am Mengensatzweg als der Rückfall“ trifft zu.
- Ursache des Rückfalls: Die Kellerwände (77 m² brutto) hängen als Außenluft an der Zone Erdgeschoss, die Bodenplatte (60 m²) ebenso,
  und Wände und Dach tragen Bruttoflächen statt der raumseitigen.
- **Rückfall „schematisch“:** Der Code kennt dafür den Schalter `KoerperflaechenAus` (Rückfall ohne Raumzuordnung); dessen Flächen
  kommen weiter aus dem Bauteilkörper (Herkunft `KOERPER`). Einen schematischen Rückfall im engeren Sinn (Herkunft `SCHEMATISCH`)
  bildet der Import nur für Dach- und Grundfläche aus Vorgaben; für dieses Haus ohne jede gelesene Fläche lehnt der Vorschlag ab
  (`IMP_BAUTEIL_PROT_KEINE_AUSSENBAUTEILE`). Verglichen ist deshalb gegen den Rückfall ohne Raumzuordnung.
- **Bezug:** Die Gegenprobe mit Mengensätzen lehnt der Vorschlag unverändert ab (`IMP_BAUTEIL_PROT_AZIMUT_FEHLT`, 11 Bauteile; Befund
  4.1). Für den Bezug ergänzt die Abnahme im gelesenen Abbild die Orientierung aus dem Bauteilkörper (18 Stellen: Azimut der Wände und
  Fenster, Neigung und Azimut des Dachs); die Flächen bleiben die des Mengensatz- und Raumgrenzenwegs.

### 3.4 Stichprobe A1–A4 — an der Probe ohne Mengensätze erfüllt (A2 mit Mengensätzen: Befund 4.1)

Kleinhaus ohne Mengensätze über den Körperweg, wie gespeichert (`Tab_Bauteil` der Arbeitskopie); Nordwinkel aus der Datei, 0°
(`Nordwinkelherkunft.Datei`).

| Zone (beheizt) | Bauteil | Art | Fläche m² | Neigung | Azimut | Randbedingung | Herkunft |
|---|---|---|---|---|---|---|---|
| Kellergeschoss (nein) | Bodenplatte | Bodenplatte | 50,76 | 180° | — | Erdreich | KOERPER |
| Kellergeschoss | Wand Süd / Nord KG | Außenwand | je 20,68 | 90° | 180° / 0° | Erdreich | KOERPER |
| Kellergeschoss | Wand West / Ost KG | Außenwand | je 11,88 | 90° | 270° / 90° | Erdreich | KOERPER |
| Erdgeschoss (ja) | Kellerdecke | Decke | 50,76 | 180° | — | Zone → Kellergeschoss | KOERPER |
| Erdgeschoss | Wand Süd EG | Außenwand | 19,90 | 90° | 180° | Außenluft | KOERPER |
| Erdgeschoss | Fenster Wohnen / Küche | Fenster | 1,80 / 1,20 | 90° | 180° | Außenluft | MENGENSATZ |
| Erdgeschoss | Wand Nord EG | Außenwand | 22,90 | 90° | 0° | Außenluft | KOERPER |
| Erdgeschoss | Wand West / Ost EG | Außenwand | je 13,50 | 90° | 270° / 90° | Außenluft | KOERPER |
| Erdgeschoss | Geschossdecke | Decke | 50,76 | 0° | — | Zone → Obergeschoss | KOERPER |
| Obergeschoss (ja) | Wand Süd OG | Außenwand | 9,40 | 90° | 180° | Außenluft | KOERPER |
| Obergeschoss | Wand Nord OG | Außenwand | 47,47 | 90° | 0° | Außenluft | KOERPER |
| Obergeschoss | Wand West / Ost OG | Außenwand | 16,335 / 15,135 | 90° | 270° / 90° | Außenluft | KOERPER |
| Obergeschoss | Fenster Schlafen | Fenster | 1,20 | 90° | 90° | Außenluft | MENGENSATZ |
| Obergeschoss | Dachplatte | Dach | 63,45 | 36,87° | 180° | Außenluft | KOERPER |

- **A1:** Nettoflächen ohne Öffnungen (Wand Süd EG 19,90 m² nach 3,0 m² Fenstern, Wand Ost OG 15,135 m² nach 1,2 m²), Fenster als
  eigene Bauteile mit eigener Zone. An `ifc4_g5_oeffnungen.ifc`: Wand Süd 25 → 22 m² (zwei Fenster), Nord 25 → 24 m² (Loch 1 m², die
  Nische zieht nicht ab), West 20 → 18 m² (Tür), Dach 100 → 98,8 m² (Dachfenster mit der Neigung des Dachs); die Gaubenwand mit mehr
  Öffnung als Fläche entfällt (`IMP_BAUTEIL_PROT_NETTO_NULL_ENTFALLEN`). Diese Probe trägt keine U-Werte, ihr Vorschlag lehnt deshalb
  mit `IMP_BAUTEIL_PROT_UWERT_FEHLT` ab — eine Eigenschaft der Probe, kein Befund.
- **A2:** Azimut 0° = Nord, im Uhrzeigersinn, je Wand nach ihrer Außenseite; Wände und Fenster senkrecht; das Pultdach 3 : 4 mit
  36,87° nach Süd (180°); Böden 180°, Decken nach ihrer Seite 0° bzw. 180°.
- **A3:** Kellerwände und Bodenplatte gegen Erdreich, die Kellerwände mit der Richtung ihrer Außenseite; der Keller als unbeheizte
  Zone (`IstBeheizt = 0`), Kellerdecke und Geschossdecke als Trennflächen mit Nachbarzone. Die Randbedingung „unbeheizt“ ohne
  eigene Zone kommt an dieser Probe nicht vor.
- **A4:** Jede gespeicherte Fläche trägt `Tab_Bauteil.Flaechenherkunft`; am Körperweg alle opaken Flächen `KOERPER`, keine
  `SCHEMATISCH`. An `ifc4_g5_oeffnungen.ifc` trägt die Grundfläche aus der Vorgabe `SCHEMATISCH`.

### 3.5 Bleibende Wache — erfüllt

`EPOS.Kern.Tests/G5AbnahmeRechenwegTests` (7 Fälle, rund 10 s): A5 je Probe (Mengensatz bleibt Quelle, keine stille Abweichung über
2 %, Einzelwarnung ab 25 %, Körperrest benannt), A1–A4 an der Arbeitskopie, A6 als Aussage „Körperweg näher am Mengensatzweg als der
Rückfall ohne Raumzuordnung“ — ohne eingefrorene Zahl.

## 4 Befunde an die Sitzung IFC

1. **Gegenprobe mit Mengensätzen und Raumgrenzen ohne Orientierung.** „`ifc4_g5_kleinhaus_mit_mengen.ifc`: Der Zonenvorschlag
   (`GebaeudeBauteilvorschlag.BildenMitZonen`) lehnt mit `IMP_BAUTEIL_PROT_AZIMUT_FEHLT` ab — 11 Bauteile ohne Azimut (die acht
   Außenwände von Erd- und Obergeschoss, die drei Fenster). Die Raumgrenzen tragen weder Azimut noch Neigung, und der Bauteilkörper
   gibt seine Orientierung nur ab, wenn kein Mengensatz vorliegt (`IfcAbbildBauer.Koerperflaechen`: mit Mengensatz nur Vergleich).
   Die Dachplatte steht mit Neigung 0° statt 36,87° und ohne Azimut — B3 (Dachneigung aus dem Körper bei Dach mit Mengensatz) greift
   an dieser Probe nicht. Mit der Orientierung aus dem Körper (18 Stellen) rechnet die Probe in jeder Zeile wie der Körperweg.“
   Vorschlag: Bei Mengensatz bleibt die Fläche aus dem Mengensatz, Azimut und Neigung kommen aus dem Körper, wo die Datei keine nennt.
2. **Hinweis, kein Befund:** „`ifc4_g5_mengen_gegenprobe.ifc` meldet zusätzlich `IMP_IFC_PROT_KOERPERFLAECHEN_ABWEICHUNG_AUSSENWAND`
   (1; 17,5 %; Wand Ost): Die Summe der Raumseiten der Wand Ost weicht 17,5 % von ihrem Körper ab. Gemeldet, also nicht still; bitte
   prüfen, ob die Zuordnung zu den Räumen an der gegliederten Wand vollständig ist.“

## 5 Urteil

**Abgenommen mit Befunden.** A1 und A3–A6 sind am Rechenweg erfüllt, **A2 an Dateien mit Mengensätzen und Raumgrenzen ohne Orientierung nicht** (Befund 4.1: die Probe mit Mengensätzen lehnt der Zonenvorschlag ab, B3 greift dort nicht; Nachabnahme von A2 und B3 an dieser Probe nach der Behebung). Referenzlauf ohne Abweichung, keine stille Abweichung über 2 %, der
Körperweg trifft den Mengensatzweg der Kleinhausprobe in Heizwärme und Heizlast, der Rückfall liegt 30 % darüber. Offen bei der
Sitzung IFC: Befund 4.1 (Orientierung bei Mengensatz und Raumgrenzen); Hinweis 4.2.

## 6 Prüfung

`dotnet build WP-Plan.Kern.slnf -c Release` 0 Fehler; `EPOS.Kern.Tests` mit dem Filter der Abnahme (G5, Ifc, GebaeudeImport,
Koerper, Dokumentationswachen, Ordnungswache) 753 Fälle, 748 grün, 5 übersprungen, 0 rot; die Testdatenbank im Repositorium
unverändert, gerechnet nur in Arbeitskopien.

## 7 Nachabnahme (#823)

Stand: Zweig mit #823 (Befund 4.1 behoben: bei Mengensatz bleibt die Fläche, Azimut und Neigung kommen aus der flächengewichteten
Außennormale der Raumgrenzen, sonst aus dem Körper). Die Wache rechnet die Probe mit Mengensätzen jetzt **ohne** Ergänzung der
Orientierung; die Ergänzung ist aus `G5AbnahmeRechenwegTests` entfernt.

`ifc4_g5_kleinhaus_mit_mengen.ifc`, wie gespeichert (`Tab_Bauteil` der Arbeitskopie); der Zonenvorschlag läuft ohne Ablehnung durch.

| Zone | Bauteil | Art | Fläche m² | Neigung | Azimut | Herkunft |
|---|---|---|---|---|---|---|
| Kellergeschoss | Bodenplatte | Bodenplatte | 50,76 | 180° | — | RAUMGRENZE |
| Kellergeschoss | Wand Süd / Nord KG | Außenwand (Erdreich) | je 20,68 | 90° | 180° / 0° | RAUMGRENZE |
| Kellergeschoss | Wand West / Ost KG | Außenwand (Erdreich) | je 11,88 | 90° | 270° / 90° | RAUMGRENZE |
| Erdgeschoss | Kellerdecke | Decke | 50,76 | 180° | — | RAUMGRENZE |
| Erdgeschoss | Wand Süd EG | Außenwand | 19,90 | 90° | 180° | RAUMGRENZE |
| Erdgeschoss | Fenster Wohnen / Küche | Fenster | 1,80 / 1,20 | 90° | 180° (Wand Süd) | MENGENSATZ |
| Erdgeschoss | Wand Nord EG | Außenwand | 22,90 | 90° | 0° | RAUMGRENZE |
| Erdgeschoss | Wand West / Ost EG | Außenwand | je 13,50 | 90° | 270° / 90° | RAUMGRENZE |
| Erdgeschoss | Geschossdecke | Decke | 50,76 | 0° | — | RAUMGRENZE |
| Obergeschoss | Wand Süd OG | Außenwand | 9,40 | 90° | 180° | RAUMGRENZE |
| Obergeschoss | Wand Nord OG | Außenwand | 47,47 | 90° | 0° | RAUMGRENZE |
| Obergeschoss | Wand West / Ost OG | Außenwand | 16,335 / 15,135 | 90° | 270° / 90° | RAUMGRENZE |
| Obergeschoss | Fenster Schlafen | Fenster | 1,20 | 90° | 90° (Wand Ost OG) | MENGENSATZ |
| Obergeschoss | Dachplatte | Dach | 63,45 | 36,87° | 180° | RAUMGRENZE |

- **A2:** alle Außenwände senkrecht auf den Achsen, die Fenster mit der Richtung ihrer Wand, das Pultdach 3 : 4 mit 36,87° nach Süd —
  erfüllt.
- **B3:** die Dachneigung kommt bei Mengensatz aus den Raumgrenzen und trifft den Körper — erfüllt.
- **Gleich dem Körperweg:** 19 Zeilen, in jeder Zeile Bauteil, Art, Randbedingung, Neigung und Azimut gleich, Fläche bis auf
  1,4e-14 m²; Heizwärme 10,090 MWh/a und Heizlast 6,873 kW gleich dem Körperweg (0,0 %), der Rückfall ohne Raumzuordnung
  +30,1 % / +8,1 %.
- **Restunterschiede im gelesenen Abbild, ohne Ergebniswirkung, benannt in der Wache:** (1) die Dachneigung der Raumgrenzen steht auf
  sechs Stellen gerundet (36,869898° gegen 36,8698976…° des Körpers, 3,5e-7°) — gehalten mit fünf Stellen, gespeichert gleich;
  (2) die Innenwand trägt keinen Azimut (keine Außenseite, wie am Körperweg), ihr Körper 90° — sie wird nicht als Außenbauteil
  gespeichert.
- **Hinweis 4.2 geklärt:** Die Ostwand der Gegenprobe ist ein L mit einem Flügel ohne Raum dahinter; die Abweichung von 17,5 % ist
  echt, die Zuordnung vollständig, die Meldung richtig.

Wache: `G5AbnahmeRechenwegTests` 8 Fälle (neu `B3_Orientierung_im_Abbild_mit_Mengensatz_gleich_Koerper`; der Jahreslauf hält A2, B3
und die Gleichheit mit dem Körperweg je Zeile).

**Urteil: A1–A6 erfüllt, G5 am Rechenweg abgenommen.**

# Konzept: Photovoltaik-Ertragsmodell EPOS-Plan — Nutzung der Modulstammdaten

**Rev. 1 — 02.09.2026 — Prüfung und Vorschlag, zur Entscheidung durch Philipp**

Auftrag: Prüfen, was die PV-Ertragsrechnung aus den vorhandenen Modulstammdaten sinnvoll nutzen könnte
(`Tab_PV(_STAMM)` führt U/I-Kennwerte, Temperaturkoeffizienten `alpha_SC`/`beta_OC`/`gamma_PMP` und
`T_NOCT`, die Simulation nutzt davon wenig), und einen Vorschlag machen.

Grundlage: aktueller Arbeitsstand (Branch `ios_migration`, 02.09.2026) von `SimulationPV.cs`,
`SolarPVGISCalculator.cs`, `PhotovoltaikCtrl.cs`, CEC-/PAN-Import, `Form_Klimadaten.cs`; Kartierungen vom
25.08.2026 (PV-Rechenkern, PV-Datenmodell) nachgeprüft. Gegenstück ist das umgesetzte
`Konzept_Photovoltaik_Wirtschaftlichkeit_EPOS-Plan.md` (P1–P6), das die Ertragsrechnung ausdrücklich
ausgeklammert hat — dieses Papier füllt genau diese Lücke.

---

## 1. Befund: Was der Rechenkern heute vom Modul kennt

`SimulationPV.Berechnung` (`SimulationPV.cs:110-147`) liest je PV-Anlagenzeile genau **vier**
Modulgrößen und rechnet damit (`BerechnePV`, `:219-231`):

```
Fläche      = Breite × Länge × Modulanzahl                       (:114)   Tab_PV.Breite/Laenge, Tab_Energieanlagen.PV_Leistung
η_STC       = Wirkungsgrad / 100                                  (:115)   Tab_PV.Wirkungsgrad
γ           = gamma_PMP / 100                                     (:116)   Tab_PV.gamma_PMP
T_Zelle     = T_amb + G/800 · 25                                  (:222)   ⇐ NOCT = 45 °C FEST VERDRAHTET
η(T)        = η_STC · (1 + γ · (T_Zelle − 25))                    (:223)
P_DC [kW]   = G · Fläche · η(T) / 1000                            (:224)
P_AC        = P_DC · 0,95                                         (:135)   ⇐ Wechselrichter FEST VERDRAHTET
```

`G` ist die Einstrahlung auf die geneigte Ebene aus `SolarCalculator.CalculateHourly`
(`SolarPVGISCalculator.cs:288-332`: isotropes Diffusmodell, Albedo 0,2 fest, Rückgabe W/m²).

### 1.1 Katalog vs. Nutzung

| `Tab_PV(_STAMM)`-Spalte | Einheit | Quelle (CEC / PAN / Hand) | vom Rechenkern genutzt | sinnvoller Nutzen |
|---|---|---|---|---|
| `Leistung` (P_STC) | W | Imp·Vmp / PNom / Hand | **nein** — nur kWp-Anzeige und `KwpDesProjekts` (`PhotovoltaikCtrl.cs:190-203`) | **Bemessungsgröße** statt Fläche×η (Abschnitt 2.1) |
| `Wirkungsgrad` | % | STC/(A_c·1000) / PNom/(L·B·1000) / Hand | ja (:115) | bleibt; wird redundant zu P_STC |
| `Laenge`, `Breite` | m | Length/Width / Height/Width / Hand | ja (:114) | künftig nur Dachflächen-/Belegungsprüfung |
| `gamma_PMP` | %/K | ✓ / muPmpReq / Hand | **ja** (:116) | bleibt (Temperaturgang) |
| `T_NOCT` | °C | ✓ / **0** (PAN schreibt 0) / — (nicht in `Form_AdminPV`) | **nein** | Zelltemperaturmodell statt 45 °C fest (2.2) |
| `U_Mpp`, `U_Leerlauf`, `I_Mpp`, `I_Kurzschluss` | V, A | ✓ / ✓ / Hand | nein | nur mit Ein-Dioden-Modell **oder** Stringauslegung gegen WR-Daten (Stufe E3) |
| `alpha_SC`, `beta_OC` | A/K, V/K | ✓ / **0** / — | nein | dito (Voc bei −10 °C, Isc bei 70 °C) |
| `Modulkosten` | € | Hand | ja (Investition, `TechnikPlanwertCtrl`) | bleibt |

### 1.2 Was die Importe zusätzlich liefern — und verwerfen

- **CEC** (`Allgemein\Import\CEC\CECDataService.cs`): parst `Technology` und `N_s` (`:152`, `:159`), die
  Ein-Dioden-Parameter `a_ref, I_L_ref, I_o_ref, R_s, R_sh_ref, Adjust` **nicht** (Spalten in der CSV
  vorhanden, Properties in `PVModule.cs` leer). Beim Schreiben nach `Tab_PV_STAMM` fallen `Technology`,
  `N_s`, `A_c`, `STC`, `PTC` weg — es gibt keine Spalten dafür. `Leistung` wird als `I_mp·V_mp` geschrieben,
  nicht als `STC`.
- **PAN** (`Allgemein\Import\Pan\PanModule.cs`): liefert die PVsyst-Diodenparameter `RShunt, Rserie,
  Gamma1, muISC, muVocSpec, NCelS, Technol` — alle verworfen; `alpha_SC`, `beta_OC`, `T_NOCT` werden mit
  **0** geschrieben (`Form_CECImport.cs:590-593`).
- **PVGIS** (`SolarPVGISCalculator.cs:78-79`): `WS10m` (Wind) wird geladen, aber **nicht** in
  `Tab_Solar` gespeichert (`SaveTmyData :451-453`).

### 1.3 Was komplett fehlt

Kein Wechselrichtermodell (weder Wirkungsgradkennlinie noch AC-Nennleistung/Clipping), keine
Systemverluste (Verschmutzung, Mismatch, DC-Verkabelung, Reflexion), **keine Degradation** (weder im
Ertrag noch in der Erlösreihe; pv@now rechnet 5 % über die Laufzeit — `PvKennzahlenRechner.cs:53`
vermerkt das als bekannte Abweichung), kein Schwachlichtverhalten (P ∝ G streng linear), keine
Horizontverschattung.

---

## 2. Bewertung der einzelnen Modulgrößen

### 2.1 `Leistung` (P_STC) als Bemessungsgröße — **hoher Nutzen, kleiner Aufwand**

Heute bestimmt `Fläche × η` den Ertrag, während `KwpDesProjekts` (P1/V3) die **kWp aus `Leistung`**
bildet und damit die EEG-Größenklassen, den anzulegenden Wert und die 60-%-Kappung des
Wirtschaftlichkeitsmoduls steuert. Zwei Wahrheiten für dieselbe Anlage: Stimmen Katalog-`Leistung` und
`L·B·η` nicht überein (Handpflege in `Form_AdminPV`, CEC `A_c` ≠ `Length·Width`, PAN-Toleranzen), rechnet
die Simulation mit anderen kWp als die Vergütungsrechnung.

Vorschlag: `P_DC = P_STC · (G/1000) · (1 + γ(T_Zelle − 25))` je Modul — physikalisch identisch zur
Flächenformel, wenn `η = P_STC/(A·1000)`, aber an dieselbe Größe gebunden wie kWp. `Fläche` bleibt
Ausweis (Dachbelegung) und Plausibilitätsprüfung: Warnung im Katalog, wenn `|P_STC − A·η·1000| > 3 %`.

*Ergebnisänderung:* nur bei inkonsistenten Katalogeinträgen (dann ist sie gewollt und die Warnung
erklärt sie); bei CEC-Modulen mit `A_c = L·B` bitgleich. Migration nicht nötig.

### 2.2 `T_NOCT` — **sofort nutzbar, moderater Nutzen**

Die Formel `:222` ist das NOCT-Modell mit eingebranntem NOCT = 45 °C: `T_Zelle = T_amb + (NOCT − 20)/800 · G`.
Die Katalogspalte liegt brach. Vorschlag: `(T_NOCT − 20)/800` mit **Rückfall 45 °C**, wenn `T_NOCT ≤ 0`
oder NULL (PAN-Import, Handeinträge). Wirkung: reale Module liegen bei 42–48 °C → ±3 K Zelltemperatur bei
Vollast → ±1,2 % Leistung dort, **±0,5 % Jahresertrag**. Zusätzlich `T_NOCT` in `Form_AdminPV` pflegbar
machen (heute nicht in der Maske).

Optional in Stufe E3: **Faiman-Modell** `T_Zelle = T_amb + G/(U0 + U1·v_Wind)` (PVGIS-Standard,
U0 = 26,9, U1 = 6,2 für c-Si) — braucht die Windspalte in `Tab_Solar`, die der Import heute verwirft.

### 2.3 `gamma_PMP` — bereits richtig genutzt

Bleibt. Einziger Hinweis: PAN liefert `muPmpReq` in %/°C, CEC `gamma_pmp` in %/K — identisch; Hand-
einträge in `Form_AdminPV` ohne Vorzeichenprüfung (ein positiver Wert würde den Ertrag bei Wärme
*erhöhen*) → Plausibilitätsprüfung `−1,0 ≤ γ ≤ 0`.

### 2.4 U/I-Kennwerte und `alpha_SC`/`beta_OC` — **ohne Diodenmodell kein Ertragsnutzen**

Für die Energieertragsrechnung bringen `U_Mpp`, `U_Leerlauf`, `I_Mpp`, `I_Kurzschluss`, `alpha_SC`,
`beta_OC` allein **nichts**: Die Leistung ist über P_STC und γ vollständig bestimmt. Ihr Nutzen entsteht
erst in zwei Ausbaustufen:

1. **Ein-Dioden-Modell** (De Soto/CEC): physikalisch konsistentes Schwachlicht- und Temperaturverhalten,
   MPP je Stunde. Voraussetzung sind die fünf Referenzparameter `a_ref, I_L_ref, I_o_ref, R_s, R_sh_ref`
   (+ `Adjust`), die die CEC-CSV **direkt liefert** — oder ein Fit aus den Datenblattwerten, der `N_s`
   braucht. Beides wird heute beim Import verworfen (1.2). Für PAN-Module gilt das PVsyst-Modell mit
   eigener Parametrierung (`RShunt, Rserie, Gamma1`) — Umrechnung möglich, aber Aufwand.
2. **Stringauslegung / Wechselrichterabgleich**: `U_oc(−10 °C) = U_oc · (1 + β·(−35))` gegen die maximale
   DC-Eingangsspannung, `U_mpp(70 °C)` gegen das MPP-Fenster, `I_sc` gegen den Eingangsstrom — ein
   Planungs-/Plausibilitätsfeature, das **Wechselrichter-Stammdaten** voraussetzt, die es nicht gibt.

Empfehlung: Beide Ausbaustufen nicht in den ersten Paketen; die Felder bleiben Katalogwissen für später
(Stufe E3). Was jetzt sinnvoll ist: den **Import vervollständigen** (Spalten `Technologie`, `N_s` und die
CEC-Diodenparameter mitschreiben), damit E3 später ohne Neuimport möglich wird — das ist billig und
verlustfrei.

---

## 3. Vorschlag in drei Stufen

### Stufe E1 — „Eine Wahrheit" (klein; ein Paket mit Referenzlauf)

| Nr. | Maßnahme | Ergebnisneutral? |
|---|---|---|
| E1.1 | P_STC-Bemessung statt Fläche×η (2.1) + Katalog-Konsistenzwarnung | nur bei inkonsistentem Katalog Änderung (gewollt) |
| E1.2 | `T_NOCT` im Zelltemperaturmodell, Rückfall 45 °C (2.2); `T_NOCT` in `Form_AdminPV` | bei `T_NOCT` = 0/NULL identisch; sonst gewollte Änderung ≤ 1 % |
| E1.3 | Wechselrichterfaktor und Systemverluste als **Anlagenparameter** in `Tab_Energieanlagen` (`PV_WrWirkungsgrad` Default 0,95; `PV_Systemverluste` [%] Default 0) statt Konstante `:135` | mit Defaults **bitgleich** |
| E1.4 | Tag-Index 1-basiert (`i/24 + 1` in `:127`; `CalculateHourly` erwartet 1…365, Klimadaten-Import nutzt `dt.DayOfYear`) | Änderung ≪ 0,1 % |
| E1.5 | γ-Plausibilität, `Init()` setzt alle Skalare zurück (`MaxPSolar`, `Stromproduktion_Max`, `*_gesamt`) | neutral |

Migrationsschritt für E1.3 (zwei Spalten an `Tab_Energieanlagen`, NULL = Default; nächste freie Nummer
bei Umsetzung prüfen). Abnahme: Referenzlauf mit Katalogdaten, bei denen `L·B·η = P_STC` gilt →
byte-identisch; Projekt mit CEC-Modulen → Differenz erklärt durch die Konsistenzwarnung.

### Stufe E2 — „Anlage statt Modul" (mittel; eigenes Paket)

| Nr. | Maßnahme | Wirkung |
|---|---|---|
| E2.1 | **AC-Nennleistung je Anlage** (`PV_WrNennleistungKw`) → Clipping `P_AC = min(P_DC·η_WR, P_AC,nenn)`; DC/AC-Verhältnis als Kennzahl | Überdimensionierung (heute üblich 1,2–1,4) wird erst damit rechenbar; Wechselwirkung mit 60-%-Kappung korrekt |
| E2.2 | **Wechselrichter-Teillastkennlinie** (3-Punkt: 10/50/100 % oder Euro-η) statt konstant 0,95 | ±1–3 % Jahresertrag, realistischere Morgen-/Abendstunden |
| E2.3 | **Schwachlichtkorrektur** nach Huld/PVGIS (technologieabhängige Koeffizienten k1…k6; c-Si als Default) — braucht Spalte `Technologie` (CEC `Technology`, PAN `Technol`) | −1…−3 % Jahresertrag; ersetzt die streng lineare P∝G-Annahme; γ bleibt modulspezifisch |
| E2.4 | **Degradation [%/a]** als Projekt-/Anlagenparameter (Default 0,5 %/a), angewandt in der jahresscharfen Erlösreihe `PV_VERGUETUNG` und im vermiedenen Bezug — **nicht** in der Stundenreihe des Simulationsjahres | schließt die dokumentierte pv@now-Abweichung (5 % über Laufzeit) |
| E2.5 | Anisotropes Diffusmodell (Hay-Davies: Anisotropieindex aus DNI/Extraterrestrisch, beides vorhanden) statt isotrop | +1…+3 % bei Südausrichtung, genauer bei steilen Neigungen |

### Stufe E3 — „Physikalisches Modulmodell" (groß; nur bei Bedarf)

Ein-Dioden-Modell mit CEC-Parametern (Spalten `a_ref, I_L_ref, I_o_ref, R_s, R_sh_ref, Adjust, N_s` in
`Tab_PV(_STAMM)`; CEC-Import schreibt sie durch, PAN per Fit), Faiman-Zelltemperatur mit Windspalte in
`Tab_Solar` (Neuimport der Klimaregionen), MPP-Suche je Stunde (Newton, 8.760 × Anlagen — unkritisch).
Erst damit werden `U/I`-Kennwerte, `alpha_SC`, `beta_OC` ertragswirksam; zugleich Voraussetzung für eine
spätere Stringauslegung gegen einen Wechselrichterkatalog. **Empfehlung: zurückstellen** — für die
Wirtschaftlichkeitsaussage liefern E1+E2 den wesentlichen Genauigkeitsgewinn; E3 lohnt nur, wenn
Stringauslegung als Feature gewünscht ist.

---

## 4. Begleitbefunde im Ertragspfad (Einordnung nach Wirkung)

Diese Punkte stammen nicht aus den Modulstammdaten, wirken aber stärker auf das Ergebnis als jede
Modulverfeinerung — sie gehören in dasselbe Paket wie E1 oder davor.

| Nr. | Befund | Beleg | Wirkung | Vorschlag |
|---|---|---|---|---|
| **B1** | **Zeitbasis UTC vs. Ortszeit.** PVGIS-TMY kommt in `time(UTC)`; `Form_Klimadaten.cs:266-283` und `SaveTmyData :459-488` schreiben die Stunden unverschoben; `SimulationPV.cs:123-127` stellt Index i dem Lastgang (Ortszeit) gegenüber. `CalculateHourly` rechnet den Sonnenstand korrekt auf UTC-Basis (`:293`: Solarzeit = Stunde + (EoT + 4·Lon)/60) — die **Erzeugungsreihe liegt damit im UTC-Raster**, der Lastgang in MEZ/MESZ ⇒ PV 1 h (Winter) / 2 h (Sommer) zu früh gegen den Verbrauch. | Code schlüssig; **DB-Gegenprobe bestätigt** (Nachtrag 1): Maximumsstunde der Globalstrahlung ≈ 11 in allen deutschen Regionen, ≈ 17,9 in der Texas-Region — folgt dem Längengrad, nicht der Ortszeit | Jahresertrag unverändert, aber **Eigenverbrauchsquote, Autarkie, Speicherfahrweise und § 51-Zuordnung zur Spotreihe** (die in MEZ/MESZ importiert wird) systematisch verschoben; betrifft auch Solarthermie (`SimulationSolarthermie.cs:256`) | Verschiebung um +1 h (MEZ) beim **Lesen** aus `Tab_Solar` in einer zentralen Stelle (`SolardatenCtrl`) statt beim Import — Bestandsregionen bleiben gültig; Sommerzeit: Entscheidung Q2 |
| **B2** | Rasterverlust: Bedarf wird auf Stunden gemittelt, PV stündlich gerechnet, per Wertwiederholung zurückgespreizt (`:96`, `:184-186`) | Kartierung 25.08., Analyse_B5 | Eigenverbrauch/Überschuss geglättet; SpeicherEngine rechnet nativ viertelstündlich | mittelfristig: Erzeugung stündlich, Bilanz viertelstündlich (PV-Stundenwert als konstante Leistung gegen den 15-min-Bedarf) — kleiner Umbau in der Zeitschrittschleife |
| **B3** | Wetterreihe wird je Modulfeld neu aus der DB gelesen (`:118-119` in der Schleife); PV-Rechnung läuft bei WP im PV-Modus komplett doppelt (`SimulationControl.cs:3312-3358`) | Kartierung | Laufzeit | einmal lesen, Ergebnis zwischenspeichern |
| **B4** | `Tab_Solar`-Abfrage filtert nur `ID_Klimaregion`, nicht `ID_Projekt`; keine Zeilenzahlprüfung (≠ 8.760 → Ausnahme oder stilles Teiljahr) | `:119`, `:123` | Robustheit | Prüfung + Protokollwarnung |

---

## 5. Größenordnungen (Erfahrungswerte, keine Messungen an EPOS-Projekten)

| Maßnahme | typ. Wirkung auf Jahresertrag | Wirkung auf Eigenverbrauchsquote |
|---|---|---|
| T_NOCT statt 45 °C (E1.2) | ±0,5 % | ≈ 0 |
| P_STC statt Fläche×η (E1.1) | 0 % bei konsistentem Katalog, sonst bis mehrere % | proportional |
| WR-Kennlinie + Clipping (E2.1/2.2) | −1 … −4 % (je DC/AC) | Clipping senkt nur Einspeisespitzen → EVQ steigt leicht |
| Schwachlicht (E2.3) | −1 … −3 % | Morgen/Abend niedriger → EVQ sinkt leicht |
| Hay-Davies (E2.5) | +1 … +3 % (Süd) | ≈ 0 |
| Degradation (E2.4) | −0,5 %/a kumulativ in der Erlösreihe | — |
| **B1 Zeitbasis** | **0 %** | **mehrere Prozentpunkte** (lastprofilabhängig), dazu falsche § 51-Stunden |

Fazit der Größenordnungen: Die Modulstammdaten allein (E1) bewegen den Jahresertrag um ≈ 1 %; den
größten Einfluss auf die *Wirtschaftlichkeit* haben B1 (Zeitbasis) und E2.1/E2.4 (Clipping,
Degradation) — weil sie Eigenverbrauchsquote und Erlösreihe treffen, nicht nur die Jahressumme.

---

## 6. Empfehlung

1. **Paket A (sofort):** B1 nach Bestätigung der Gegenprobe + E1.1–E1.5 in einem Paket mit
   Referenzlauf-Basis vorher/nachher. Aufwand klein (SimulationPV, SolardatenCtrl, Form_AdminPV, ein
   Migrationsschritt), Wirkung: konsistente kWp, korrekte Zeitbasis, parametrierbare Verluste.
2. **Paket B:** E2.1, E2.2, E2.4 (Wechselrichter-Nennleistung/-Kennlinie, Degradation) — das sind die
   Größen, die Planer tatsächlich eingeben und die pv@now-Vergleichbarkeit herstellen. E2.3/E2.5 als
   Genauigkeitsverbesserung anschließen.
3. **Importe jetzt vervollständigen** (Spalten `Technologie`, `N_s`, CEC-Diodenparameter, `WS10m` in
   `Tab_Solar`), auch wenn E3 offen bleibt — kostet wenig, verhindert spätere Neuimporte.
4. **E3 zurückstellen** bis eine Stringauslegung als Feature gewünscht ist.

---

## 7. Entscheidungsfragen

| Nr. | Frage | Empfehlung |
|---|---|---|
| **Q1** | P_STC als Bemessungsgröße akzeptieren, auch wenn Bestandsprojekte mit inkonsistentem Handkatalog dadurch andere Erträge zeigen? | ja — mit Konsistenzwarnung im Katalog und Hinweis im Simulationsprotokoll |
| **Q2** | B1-Korrektur: feste +1 h (MEZ, Normjahr) oder mit Sommerzeitwechsel (+2 h Ende März–Ende Oktober)? Lastprofile (VDI 4655/Wochenprofile) laufen in Uhrzeit **mit** Sommerzeit; die Spotreihe wird MEZ/MESZ-korrekt importiert | mit Sommerzeit — sonst bleibt im Sommerhalbjahr eine Stunde Versatz gegen Last **und** Spotpreis |
| **Q3** | Korrektur B1 beim Lesen (zentral, Bestandsregionen gültig) oder beim Import (Neuimport aller Klimaregionen nötig)? | beim Lesen (`SolardatenCtrl`, ganze Zeilen verschieben — `Sol_*`/`Sonnenwinkel` sind auf denselben UTC-Stunden gerechnet); Bestand: 25 Projekt- + 32 Stammregionen blieben unverändert gültig |
| **Q4** | Degradation Default 0,5 %/a (E2.4) — als Projekt- oder Anlagenparameter? | Projektparameter in `Tab_ProjektPhotovoltaik` (dort liegt schon die Erlösseite) |
| **Q5** | Wechselrichter als Anlagenparameter (η, P_AC) oder eigener Wechselrichterkatalog (`Tab_Wechselrichter_STAMM`)? | Anlagenparameter; Katalog erst mit E3/Stringauslegung |
| **Q6** | E3 (Ein-Dioden-Modell, Stringauslegung) grundsätzlich gewünscht? | nein für jetzt; Importfelder trotzdem mitschreiben |
| **Q7** | Windspalte in `Tab_Solar` nachrüsten (Neuimport je Klimaregion, Bestandsregionen ohne Wind → NOCT-Rückfall)? | ja, im Zug der Importvervollständigung; Nutzung erst mit Faiman (E3) |

---

## Nachtrag 1 (02.09.2026) — Gegenprobe zu Befund B1 (Zeitbasis)

Read-only-Messung an `C:\ProgramData\EPOS_PLAN\Kenndaten.vor-sqlite.accdb` (jüngster Access-Stand vom
01.09.; die Produktiv-DB ist auf diesem Rechner bereits auf SQLite umgestellt — Struktur der Solartabellen
identisch). Je Region: mittlere Stunde des Tagesmaximums (0-basierter Index im 24er-Block, `ORDER BY ID`)
für Sommer (Jun/Jul) und Winter (Dez/Jan); `Tab_Solar` und `Tab_Solar_STAMM` liefern identische Werte.

| Region | Lon | erwartet bei UTC | erwartet bei MEZ / MESZ | gemessen Sommer | gemessen Winter | argmax `Sonnenwinkel` |
|---|---:|---:|---:|---:|---:|---:|
| Berlin | 13,40 | 11,1 | 12 / 13 | **10,98** | **11,08** | 11,0 |
| Stuttgart | 9,18 | 11,4 | 12 / 13 | **11,20** | **11,00** | 11,0 |
| München | 11,58 | 11,2 | 12 / 13 | **11,25** | **11,02** | 11,0 |
| Bocholt | 6,61 | 11,6 | 12 / 13 | **11,44** | **11,47** | — (Spalte leer) |
| **Texas** | **−99,90** | **18,7** | 12 / 13 | **17,90** | **17,97** | 18,8–19,0 |

**Schlussfolgerung: bestätigt.** Die Maximumsstunde folgt über 113° Längengrad dem Längengrad, nicht der
Ortszeit; die deutschen Regionen treffen die UTC-Erwartung auf 0,1–0,4 h, MEZ läge 1 h, MESZ 2 h daneben.
Unabhängiger Zusatzbeleg: In der Texas-Region liegt Sommerstrahlung in den Stunden 0–1 **und** 12–23
desselben 24er-Blocks — die Blöcke sind UTC-Tage, keine Ortstage.

**Code-Belege (ergänzend zu Abschnitt 4):**
- Solar-Seite UTC, unverschoben: DTO `[JsonPropertyName("time(UTC)")]` (`SolarPVGISCalculator.cs:59-60`);
  `Form_Klimadaten.cs:276-283` reicht `dt.Hour` dieses Stempels weiter; `CalculateHourly :293` rechnet
  `solarHour = hour + (eot + 4·Lon)/60` = Umrechnung **UTC → wahre Ortssonnenzeit** (in sich konsistent,
  deshalb stimmen `Sonnenwinkel`- und `Globalstrahlung`-Maximum überein); `SaveTmyData :459-488` schreibt
  in Eingangsreihenfolge. `CalculateTimeOffset` (`:397-427`) ist **toter Code** (kein Aufruf repoweit).
- Lastgang-Seite Ortszeit: synthetische Profile `BhkwPlan.StromWocheToJahr :214-222` kacheln das
  168-h-Wochenprofil (Wochentag/Uhrzeit-Tabelle des Anwenders, `ProfilBedarf.cs:550-553`) ab dem
  Kalender-Wochentag des 1. Januar; importierte Lastgänge werden ausdrücklich „als Ortszeitreihe gelesen"
  (`SpeicherEngine\GanglinienPruefung.cs:253-263`, `GanglinienDatei.cs:445` ohne Zeitzonenumrechnung).
  Die Spotreihe dagegen wertet CET/CEST aus (`SpotpreisLeser.cs:193-194, 271`) — also Ortszeit.
- Keine Verschiebung existiert: Volltextsuche `UTC|Zeitzone|TimeZone|MEZ|MESZ|Sommerzeit|ToLocalTime`
  im Umfeld von `Tab_Solar`/`SolardatenCtrl`/`SimulationPV`/`SimulationSolarthermie` ohne Treffer.

**Korrekturfläche (für Q3):** stundenscharfe Leser `SimulationPV.cs:118-127`,
`SimulationSolarthermie.cs:235-247`, `SimulationWaermebedarf.cs:832-847` (Stundentemperatur),
`Form_Simulation_Config.Uebersicht.cs:528-545` — alle über `SolardatenCtrl.cs:49/67/100`; indirekt
`ErdreichTemperatur.cs:269, 445-463`, `WaermequelleClass.cs:725/730`, `ErdreichAuswertung.cs:424-437`
(Jahres-Sinus, Stundenversatz praktisch wirkungslos). `Tab_Klimadaten` (365 Tagesmittel) ist nur an den
Tagesgrenzen berührt (Flags `WE`/`TagTyp_*`, `Form_Klimadaten.cs:322-327`) — vernachlässigbar.
Bestand: 25 Projektregionen (219.000 Zeilen) + 32 Stammregionen (280.320 Zeilen).

**Nebenbefund:** Region Bocholt (STAMM 4, Projektkopien 1012004/1020032) hat `Sonnenwinkel` durchweg
0/leer — eigene Datenlücke, unabhängig von B1.

**Konsequenz für die Empfehlung:** B1 wird von „Prüfpunkt" zu **bestätigtem Bestandsfehler mit
Ergebniswirkung** auf alle PV- und Solarthermie-Projekte (Paarung Erzeugung ↔ Bedarf 1–2 h zu früh;
Jahressummen unverändert). Paket A (Abschnitt 6) beginnt damit; Referenzbasis vorher einfrieren, weil
sich Eigenverbrauchs-, Speicher- und Solarthermie-Deckungswerte aller Projekte ändern werden.

---

## Nachtrag 2 (02.09.2026) — Beauftragung: Paket A (B1 + E1) und Paket B (E2 als wählbares Modell)

**Entscheidungen Philipps (02.09.2026):** Paket A wird umgesetzt (B1 + E1, Empfehlungen Q1–Q3 als
Vorgaben: P_STC-Bemessung mit Konsistenzwarnung; B1-Korrektur beim Lesen in `SolardatenCtrl`, ganze
Zeilen, **mit Sommerzeit**). E2 wird ebenfalls umgesetzt, **aber das vereinfachte Modell bleibt
wählbar** — E2 ist kein Ersatz, sondern eine zweite Rechentiefe. E3 bleibt zurückgestellt (Q6), die
Importfelder werden im Zug von E2 mitgeschrieben, soweit E2 sie braucht (Technologie).

### N2.1 Modellschalter je Anlage

Neue Spalte `Tab_Energieanlagen.PV_Modell` TEXT(20), Persistenzwerte (DbWerte, eingefroren):
`PV_MODELL_EINFACH` (auch NULL) und `PV_MODELL_ERWEITERT`. Der Schalter sitzt **je Anlage** (nicht je
Projekt), weil die Wechselrichterdaten je Anlage gelten und ein Projekt gemischt sein darf (ein Feld mit
bekanntem Wechselrichter, eines ohne). Damit liegen alle E1/E2-Parameter in derselben Anlagenzeile und
werden an derselben Stelle gelesen/geschrieben (`WErzeugerCtrl`, `WizardCtrl`, `Form_PV`); `Tab_Einstellungen`
bleibt unberührt (Fehlerklasse „Insert löscht Flag", Frage 23, wird gar nicht erst geöffnet).

| Modell | Transposition | Modul | Wechselrichter | Ergebnis |
|---|---|---|---|---|
| **EINFACH** (Default, NULL) | isotrop (Bestand) | P_STC · G/1000 · (1 + γ(T_Zelle − 25)), T_Zelle nach NOCT (E1.2) | konstant `PV_WrWirkungsgrad` (Default 0,95), kein Clipping; `PV_Systemverluste` | **bitgleich Paket-A-Stand** |
| **ERWEITERT** | Hay-Davies (E2.5) | Huld-Schwachlichtmodell, wenn `Technologie` bekannt, sonst EINFACH-Modulformel + Protokollhinweis (E2.3) | 3-Punkt-Kennlinie + Clipping auf `PV_WrNennleistungKw` (E2.1/E2.2); `PV_Systemverluste` | Kennzahlen DC/AC, Clipping-Verlust, Wechselrichterverlust |

### N2.2 Parameter und Datenmodell (alle NULL = Default, kein DDL-DEFAULT auf Fachwerten)

**`Tab_Energieanlagen`** (Migrationsschritt, Nummer bei Umsetzung frei wählen; Paket A legt bereits
`PV_WrWirkungsgrad` und `PV_Systemverluste` an):

| Spalte | Typ | Bedeutung | NULL bedeutet |
|---|---|---|---|
| `PV_Modell` | TEXT(20) | Schalter N2.1 | EINFACH |
| `PV_WrNennleistungKw` | DOUBLE | AC-Nennleistung des Wechselrichters | kein Clipping |
| `PV_WrEta10`, `PV_WrEta50`, `PV_WrEta100` | DOUBLE | Wirkungsgrad bei 10/50/100 % AC-Nennleistung (0…1) | Vorbelegung 0,94 / 0,975 / 0,97 (typischer String-Wechselrichter; editierbar) — nur in ERWEITERT wirksam |

**`Tab_PV` und `Tab_PV_STAMM`:** `Technologie` TEXT(30), Werte `C_SI` (mono/poly), `CIS`, `CDTE`,
`A_SI`, `SONSTIGE`; Befüllung durch CEC-Import (`Technology`: Mono-c-Si/Multi-c-Si → C_SI, CIGS/CIS →
CIS, CdTe → CDTE, Thin Film/a-Si → A_SI, Rest SONSTIGE), PAN-Import (`Technol`: mtSiMono/mtSiPoly →
C_SI, mtCIS → CIS, mtCdTe → CDTE, mtAmorphous → A_SI) und Dropdown in `Form_AdminPV`. Katalogregistry
(Dublettenprüfung) um die Spalte ergänzen. Für `A_SI`/`SONSTIGE` gibt es keine Huld-Koeffizienten →
Rückfall auf die EINFACH-Modulformel mit Protokollhinweis.

**`Tab_ProjektPhotovoltaik`:** `Degradation` DOUBLE [%/a]. **NULL = 0** (ergebnisneutral, Hausregel
„der Vorgabewert ist der, der nichts ändert"); beim **Anlegen** einer neuen Zeile belegt
`ProjektPhotovoltaikCtrl` mit 0,5 vor (Muster N5/F5-Vorbelegungen). Wirkt in `PvErloesRechner` als
Faktor (1 − d)^(t−1) auf Einspeiseerlös, § 51-Ausfall/-Gutschrift und vermiedenen Bezug des Jahres t —
**nicht** in der Stundensimulation.

### N2.3 Rechenvorschriften ERWEITERT

**Hay-Davies (E2.5):**
```
I_0n  = 1367 · (1 + 0,033 · cos(360° · n / 365))            extraterrestrische Normalstrahlung, n = Tag im Jahr
A_i   = DNI / I_0n                                          Anisotropieindex (0 ≤ A_i ≤ 1)
R_b   = cosθ / max(cosθ_z, cos 85°)                         Geometriefaktor, Klemme gegen Horizontexplosion
G_t   = DNI·cosθ + DHI·[A_i·R_b + (1 − A_i)·(1 + cosβ)/2] + GHI·ρ·(1 − cosβ)/2      ρ = 0,2 (Bestand)
```
Bei DNI = 0 identisch zum isotropen Bestand (Prüfkriterium).

**Huld-Schwachlichtmodell (E2.3), PVGIS-Koeffizienten:**
```
G' = G_t / 1000,  T' = T_Zelle − 25
η_rel = 1 + k1·ln G' + k2·(ln G')² + k3·T' + k4·T'·ln G' + k5·T'·(ln G')² + k6·T'²
P_DC  = P_STC · G' · η_rel                                   (für G_t < 1 W/m²: 0)
```
| Technologie | k1 | k2 | k3 | k4 | k5 | k6 |
|---|---:|---:|---:|---:|---:|---:|
| C_SI | −0,017237 | −0,040465 | −0,004702 | 0,000149 | 0,000170 | 0,000005 |
| CIS | −0,005554 | −0,038724 | −0,003723 | −0,000905 | −0,001256 | 0,000001 |
| CDTE | −0,046689 | −0,072844 | −0,002262 | 0,000276 | 0,000159 | −0,000006 |

(Huld et al. 2010/2011, wie in PVGIS und pvlib `pvarray.huld`; bei Umsetzung gegen die pvlib-Quelle
gegenprüfen.) Prüfkriterium: bei G' = 1, T' = 0 ist η_rel = 1 exakt. In ERWEITERT ersetzt Huld den
linearen γ-Gang vollständig (die Temperaturterme stecken in k3…k6); `gamma_PMP` bleibt für EINFACH.

**Wechselrichter (E2.1/E2.2):**
```
P_DC,sys = Σ P_DC,Modulfelder · (1 − PV_Systemverluste/100)
x        = P_DC,sys / PV_WrNennleistungKw                    Auslastung (ohne Nennleistung: x = P_DC,sys / P_STC,gesamt)
η_WR(x)  = lineare Interpolation über (0,1; η10), (0,5; η50), (1,0; η100); unter 0,1 linear auf η10·x/0,1 → 0
P_AC     = min(P_DC,sys · η_WR(x), PV_WrNennleistungKw)      Clipping; ohne Nennleistung kein Clipping
```
Kennzahlen je Anlage/Projekt: DC/AC-Verhältnis (P_STC,gesamt / P_AC,nenn), Clipping-Verlust [kWh/a],
Wechselrichterverlust [kWh/a], Volllaststunden — ins Simulationsprotokoll und auf die PV-Karte
(`Form_Simulation_Config.Karten.PvDetailchips`); Ergebnistabellen unverändert (kein neuer Ausweis in
`Tab_ErgebnisPhotovoltaik`, Q-Reserve).

### N2.4 Oberfläche

- `Form_PV` (je Anlage, programmatisch unter dem Panel „PV Anlage Eigenschaften"): Dropdown
  „Rechenmodell" (Einfach/Erweitert), Felder „Wechselrichter-Nennleistung [kW]", „Wirkungsgrad bei
  10/50/100 %" (nur in Erweitert aktiviert — Enabled-Umschaltung, nicht Ausblenden), dazu die E1.3-Felder
  „Wechselrichter-Wirkungsgrad" (nur Einfach) und „Systemverluste [%]" (beide). Live-Kennzahl DC/AC.
- `Form_AdminPV`: Dropdown „Technologie" (+ E1.2-Feld `T_NOCT`).
- `Form_PhotovoltaikVerguetung` (Wirtschaftlichkeit): Feld „Degradation [%/a]" in der Anlage-Gruppe.
- Alle Texte `MyResource` de+en (Präfix `PVM_*`), HilfeKontext unverändert (bestehende Formulare).

### N2.5 Abnahme

1. **EINFACH ist bitgleich zum Paket-A-Stand** (Referenzlauf gegen die Basis nach Paket A) — das ist
   das zentrale Kriterium für „vereinfachtes Modell bleibt zulässig".
2. ERWEITERT ohne Wechselrichterdaten und mit `Technologie` = NULL rechnet Hay-Davies + EINFACH-Modul +
   Kennlinie mit Vorbelegung — Protokoll benennt jede Rückfallebene.
3. Prüfwerte: Huld η_rel(1, 0) = 1; Hay-Davies = isotrop bei DNI = 0; Clipping-Verlust = Σ max(0, P_DC·η −
   P_AC,nenn) exakt; Degradation NULL → Erlösreihe identisch zu P6-Stand (Wirtschaftlichkeits-Referenz
   „INEKON Schulung 01" unverändert), d = 0,5 → Jahr 20 Faktor **0,9092** ((1 − 0,005)^19 = 0,909156;
   die Rev.-1-Angabe 0,9088 war ein Rechenfehler — korrigiert 03.09.2026 nach Hinweis aus der Umsetzung).
4. Migration idempotent (Zweitlauf 0), CEC-/PAN-Import schreibt `Technologie`, Katalogdubletten-Prüfung
   berücksichtigt die Spalte.

### N2.6 Reihenfolge

Paket A (B1 + E1) zuerst — eigene Commits, Referenzbasis PA0 vorher, Vergleich PA1 danach mit
erklärten Deltas. Paket B (E2) danach auf dem Paket-A-Stand, weil beide `SimulationPV`,
`SolarPVGISCalculator`, die Anlagenzeile und `Form_PV` berühren; Referenzbasis PA1 wird die
Bitgleichheits-Basis für EINFACH.

---

## Nachtrag 3 (02.09.2026) — Umsetzung Paket A (B1 + E1)

**Paket A ist umgesetzt, verifiziert und mit Referenzlauf belegt.** Branch `ios_migration`,
vier Commits:

| Commit | Inhalt |
|---|---|
| `36c5401` | **PA1a** — Migrationsschritt **62** (`PV_WrWirkungsgrad`, `PV_Systemverluste` an `Tab_Energieanlagen`, erster Schritt des SQLite-Zweigs) und das Datenmodell dazu: `WErzeugerModel`, `WErzeugerCtrl.AusZeile`, `WizardCtrl.SQL_ANLAGE_INSERT`/`AnlagenParameter`, `SchemaKatalog`, `AbweichungsErmittler`, Testfälle der Zugriffsschichtproben |
| `aced014` | **PA1b** — B1 (`SolarZeitbasis`, `SolardatenCtrl.ReadOrtszeit`, `SolardatenModel.TagUtc`/`StundeUtc`, alle vier stundenscharfen Leser) sowie E1.1, E1.2, E1.4 und E1.5 in `SimulationPV` |
| `7c622b1` | **PA1c** — `Form_PV` (WR-Wirkungsgrad, Systemverluste), `Form_AdminPV` (`T_NOCT`), 7 Ressourcenschlüssel de + en |
| (dieser) | **PA1** — Referenzlauf `Referenzlaeufe/2026-09-02_PA1_nach-PaketA/`, Umsetzungsprotokoll, LIESMICH |

Umsetzungsprotokoll mit allen Zahlen, Fundstellen und Nebenbefunden:
`WindowsFormsApplication1/Allgemein/Simulation/PaketA_Zeitbasis_E1_Protokoll.md`.

### N3.1 Wie die Entscheidungen Q1–Q3 umgesetzt sind

* **Q1 (P_STC als Bemessungsgröße): ja, mit Konsistenzhinweis.** `P_DC = P_STC · G/1000 ·
  (1 + γ(T_Zelle − 25))`; Rückfall auf die Flächenformel nur bei `Leistung ≤ 0`. Weichen
  `P_STC` und `L·B·η·1000` um mehr als 3 % voneinander ab, sagt es das Protokoll je Modul.
* **Q2 (mit Sommerzeit): ja.** EU-Regel fest verdrahtet, kein `TimeZoneInfo` — Begründung wie
  in `GanglinienPruefung`. Das Referenzjahr bestimmt nur die Umstelltage: Jahr der
  Spotpreisreihe des Projekts, sonst `DbWerte.SOLAR_REFERENZJAHR_STANDARD = 2025`
  (deterministisch, **kein `DateTime.Today`** — sonst rechnete derselbe Referenzlauf am
  Jahreswechsel andere Zahlen).
* **Q3 (Korrektur beim Lesen): ja.** `SolardatenCtrl.ReadOrtszeit` verschiebt **ganze Zeilen**;
  die Bestandsregionen bleiben unverändert gültig, kein Neuimport. Die UTC-Herkunft reist als
  `TagUtc`/`StundeUtc` an der Zeile mit, damit der Sonnenstand weiter auf UTC-Basis rechnet.

### N3.2 Was das Papier anders erwartet hatte

1. **T_NOCT bleibt in dieser Referenzmenge wirkungslos.** Abschnitt 2.2 hatte den Rückfall bei
   „`T_NOCT ≤ 0` oder NULL" vorgesehen; der PA0-Befund A1 hat gezeigt, dass in allen sechs
   Katalogmodulen der Wert von `I_Kurzschluss` in `T_NOCT` steht (9,014 / 9,34 / 9,42) — positiv
   und damit für dieses Kriterium unsichtbar. Umgesetzt ist deshalb ein **physikalisches
   Fenster 20…60 °C**; es greift überall, und die erwarteten ±0,5 % Jahresertrag entstehen erst
   **nach der Katalogpflege**.
2. **Die Wirkung von B1 auf die Eigenverbrauchsquote ist kleiner als geschätzt.** Abschnitt 5
   nannte „mehrere Prozentpunkte"; gemessen wurden **−0,95 pp (1007) bis +0,12 pp (1029)**. Der
   Grund liegt in den Daten, nicht im Modell: Die Referenzprojekte fahren synthetische
   Wochenprofile, die über den Tag flach sind. Deutlicher zeigen es die Speichergrößen
   (Füllstandssumme bis −2,3 %, PV-Ladung des Pufferspeichers −8,9 %). Bei einem gemessenen
   Lastgang mit Abendspitze ist die Wirkung entsprechend größer.
3. **E1.4 ist messbar, wenn auch klein.** Der 1-basierte Tagindex bewegt die PV-Jahreserzeugung
   um **+0,0013 % (Neigung 30°) bis +0,0115 % (Neigung 0°)** — innerhalb der angesagten
   ≪ 0,1 %, aber geometrieabhängig.
4. **Die Zeilenzahlprüfung aus Befund B4 ist mitgekommen.** `ReadOrtszeit` verschiebt **nicht**,
   wenn die Reihe nicht 8.760 Zeilen führt, und meldet das als Warnung.

### N3.3 Nebenwirkungen und Nebenbefunde

* **Schemastand 62.** `.wpx`-Pakete mit Stand 61 werden abgewiesen
  (`ProjektExportImportCtrl` vergleicht gegen `ZIEL_VERSION`) — systemimmanent, keine
  Regression. **Neue Migrationsschritte beginnen bei 63.**
* **Neue Konstante `SchemaMigration.FREEZE_VERSION_ACCESS = 61`.** Der eingefrorene
  Access-Zweig endet bei 61 und kann 62 nie erreichen. Ohne die Trennung hätten
  `HebeAltbestand` jede Alt-Hebung als Misserfolg gemeldet und der SQLite-Zweig jede Datei auf
  Stand 61 als „nicht erstmigriert" abgewiesen.
* **Bestandsfehler in `Form_AdminPV` geschlossen:** Das Speichern eines Katalogmoduls schrieb
  `alpha_SC`, `beta_OC` und `T_NOCT` mit 0 zurück und löschte damit die Werte des CEC-Imports.
  Das erklärt zugleich den vergifteten Katalog aus Befund A1. Der Schreibweg ist repariert; die
  **vorhandenen Daten** sind es nicht — sie brauchen Neuimport oder Handpflege.
* **`SQL_ANLAGE_INSERT` verliert weiterhin die KWKG-Spalten (Schritt 22) und die drei
  B3-Spalten (Schritt 61).** Beim Löschen + Neuanlegen gehen sie still verloren — dieselbe
  Fehlerklasse, die Paket 1 für die Quellen-/Senken-Konfiguration geschlossen hat. **Nur
  protokolliert, bewusst nicht in Paket A behoben** (Wirtschaftlichkeitsmodul, eigene Abnahme;
  die Anweisung wird an fünf Stellen benutzt).
* **`gamma_PMP = 0`** beim Jinkosolar-Modul: 1011, 1026, 1028, 1029 rechnen ohne
  Temperaturgang. E1.5 meldet es, ändert aber nichts.

### N3.4 Referenzbasis und Abnahmestand für Paket B

`Referenzlaeufe/2026-09-02_PA1_nach-PaketA/` (14 Projekte, 355 CSV, Codestand `7c622b1`,
Schemastand 62) ist die neue **aktuelle Basis** und damit die **Bitgleichheits-Basis für das
Modell EINFACH** (N2.5, Kriterium 1). Selbstvergleich 14/14 PASS, 355/355 byte-/MD5-gleich;
`pruefen` plausibel. Gegen PA0: 391 geänderte Skalare, **jeder zugeordnet**, kein Schlüssel neu
oder entfallen.

**Offen (Sichtabnahme Philipp):**

1. `Form_PV` — dritte Spalte im Panel „PV Anlage Eigenschaften", Panel 308 → 420 px.
2. `Form_AdminPV` — Feld „Zelltemperatur NOCT [°C]" links unten.
3. Katalogpflege `T_NOCT` für die sechs Referenzmodule; danach wird E1.2 wirksam und ein
   neuer Basiswechsel fällig.

Paket B (Stufe E2, Nachtrag 2) kann auf diesem Stand beginnen.

---

## Nachtrag 4 (03.09.2026) — Umsetzung Paket B (Stufe E2 als wählbares Modell)

**Paket B ist umgesetzt, verifiziert und mit Referenzlauf belegt.** Branch `ios_migration`,
fünf Commits:

| Commit | Inhalt |
|---|---|
| `f1d16e3` | **PB1a** — Migrationsschritt **63** (`PV_Modell`, `PV_WrNennleistungKw`, `PV_WrEta10/50/100` an `Tab_Energieanlagen`; `Technologie` an `Tab_PV` und `Tab_PV_STAMM`; `Degradation` an `Tab_ProjektPhotovoltaik`) und das Datenmodell dazu — `WErzeugerModel`, `WErzeugerCtrl.AusZeile`, `WizardCtrl.SQL_ANLAGE_INSERT`/`AnlagenParameter`, `PhotovoltaikModel`/`-Ctrl`/`-StammCtrl`, `ProjektPhotovoltaikCtrl`, `SchemaKatalog`, `KatalogRegistry`, `AbweichungsErmittler`, `DbWerte` |
| `4bd8752` | **PB1b** — Rechenmodell ERWEITERT: `PvErweitertesModell` (neu), Hay-Davies in `SolarCalculator`, Modellweiche und ERWEITERT-Zweig in `SimulationPV` |
| `74f9acf` | **PB1c** — Degradation in `PvErloesRechner` und `WirtschaftlichkeitCtrl`, Feld im PV-Vergütungsdialog |
| `36acbf1` | **PB1d** — `Form_PV` (Modellwahl, Panel-Umbau), `Form_PVModell` (neu), `Form_AdminPV` (Technologie), CEC-/PAN-Import, PV-Karte, 31 Ressourcenschlüssel de + en |
| (dieser) | **PB1** — Referenzlauf `Referenzlaeufe/2026-09-03_PB1_nach-PaketB/`, Umsetzungsprotokoll, LIESMICH |

Umsetzungsprotokoll mit allen Zahlen, Fundstellen und Nebenbefunden:
`WindowsFormsApplication1/Allgemein/Simulation/PaketB_E2_Modellwahl_Protokoll.md`.

### N4.1 Das zentrale Abnahmekriterium ist erfüllt

**Kriterium 1 aus N2.5 — „EINFACH ist bitgleich zum Paket-A-Stand": 355 von 355 CSV
byte-/MD5-gleich** zu `2026-09-02_PA1_nach-PaketA`, Toleranzvergleich 14/14 PASS
(3 882 476 Werte), keine Datei nur auf einer Seite, `pruefen` plausibel.

Der eine Nachweis trägt sechs Umbauten, die alle den PV-Rechenweg berühren: den
Migrationsschritt 63, die Modellweiche in `SimulationPV`, die **Auslagerung der
Sonnengeometrie** in `SolarCalculator` (der heikelste Eingriff — er hätte im letzten Bit
abweichen können), den Degradationsfaktor, fünf zusätzlich gelesene und geschriebene
Anlagenspalten und die neue Katalogspalte `Technologie`.

**Kriterium 3** ist einzeln nachgerechnet (Prüfstand, 58 PASS / 0 FAIL): Huld
`η_rel(1, 0) = 1` **exakt** für alle drei Koeffizientensätze; Hay-Davies gleich isotrop
bei DNI = 0 über **45 792 Fälle mit maximaler Abweichung 0** (nicht 1e-9); Clipping-Verlust
gleich `Σ max(0, P_DC·η − P_AC,nenn)`; Kennlinie an den Stützstellen exakt; die
Wirtschaftlichkeits-Referenz „INEKON Schulung 01" unverändert (28 PASS / 0 FAIL, I3
−0,76 %, I4 −0,47 %). **Kriterium 2** (Rückfallebenen einzeln benannt) und **Kriterium 4**
(Migration idempotent — Zweitlauf ohne DDL; CEC-/PAN-Import schreibt `Technologie`; die
Dublettenprüfung führt die Spalte mit) ebenfalls erfüllt.

### N4.2 Was dieses Papier anders erwartet hatte

1. **Der Prüfwert in N2.5 ist leicht falsch.** Dort steht „d = 0,5 → Jahr 20 Faktor
   0,9088"; nachgerechnet ist `(1 − 0,005)^19 = 0,909156`, also **0,9092**. Die Formel
   stimmt, die Zahl nicht — **N2.5 sollte auf 0,9092 berichtigt werden.**
2. **Der „vermiedene Bezug" brauchte eine Auslegung.** N2.2 verlangt den Faktor auch auf
   ihn, sagt aber nicht, wo er hingehört: Die Stundenreihe und damit der Reststrom des
   Kapitalwerts sind über alle Jahre konstant, und `PvErloesRechner` kannte den
   vermiedenen Bezug gar nicht. Umgesetzt ist er als **negativer Beitrag in der Reihe
   `PV_VERGUETUNG`** — `EV_Basisjahr · (1 − Faktor(t)) · Arbeitspreis` —, die Kostenseite
   bleibt unberührt. Kein Doppelansatz: Die Kostenseite rechnet die Ersparnis des
   Basisjahres, dieser Posten nur ihren Schwund. Ohne Degradation ist er exakt 0.
   **Diese Auslegung gehört fachlich bestätigt.**
3. **Die Oberfläche brauchte mehr als „ein Dropdown und einen Knopf".** Das Panel „PV
   Anlage Eigenschaften" war nach Paket A nicht nur voll, sondern hatte eine
   **Überlappung**: Die dritte Spalte begann bei x = 252, das AutoSize-Label „Anzahl
   Module:" reicht bis x = 282. Horizontal ist ab x = 449 die Modulliste im Weg, vertikal
   der Modulblock. Umgesetzt ist deshalb ein Umbau auf **zwei Spalten und vier Zeilen**
   (420 × 71 → 420 × 128); alles darunter rückt 57 px nach unten, die Maske wächst
   entsprechend. Im Assistenten passt sich der Rahmen selbst an.
4. **`HIT`, `PERC` und `TOPCon` fallen auf `C_SI`**, nicht auf `SONSTIGE`. N2.2 nennt sie
   nicht; es sind kristalline Siliziumzellen, und sie unter „SONSTIGE" zu führen hätte
   ihnen ohne Not den Koeffizientensatz genommen.
5. **Die Koeffizienten sind gegen pvlib gegengeprüft** (Auftrag aus N2.3): Der Satz von
   N2.3 ist zeichengleich mit `pvlib._infer_k_huld` **PVGIS 5**. pvlib führt daneben einen
   neueren **PVGIS-6**-Satz, den dieses Papier nicht vorgibt — er ist bewusst nicht
   umgesetzt.

### N4.3 Was ERWEITERT tatsächlich bewegt

Zwei Smoke-Läufe auf Kopien, Projekt 1026 (5,20 kWp, Neigung 30°, Süd):

| | EINFACH | C_SI + WR 4,16 kW | ohne WR-Daten, Technologie NULL |
|---|---:|---:|---:|
| Jahresertrag | 6,71 MWh | **6,45 (−3,94 %)** | **6,94 (+3,37 %)** |
| Eigenverbrauchsquote | 64,68 % | **66,20 %** | **62,97 %** |
| DC/AC · Clipping | — | **1,25 · 40,1 kWh** | — · 0 |

Die Größenordnungen aus Abschnitt 5 sind getroffen: WR-Kennlinie + Clipping und
Schwachlicht zusammen **−3,9 %** (angesagt −1 … −4 % bzw. −1 … −3 %), Hay-Davies allein
**+3,4 %** (angesagt +1 … +3 %). Auch die Vorzeichen der Eigenverbrauchsquote stimmen:
Clipping trifft die Einspeisespitzen, die EVQ **steigt** um 1,5 Punkte; ohne Schwachlicht
sinkt sie. Die maximale Einstrahlung steigt in beiden Läufen von 1 058,9 auf 1 080,5 W/m²
— der circumsolare Anteil, den das isotrope Modell nicht kennt.

> Ein Teil der −3,9 % ist **nicht** Schwachlicht: Das Jinkosolar-Modul führt
> `gamma_PMP = 0`, im einfachen Modell rechnet die Anlage also ohne Temperaturgang,
> während Huld ihn über k3…k6 zurückbringt. Bei einem Modul mit gepflegtem γ fiele die
> Differenz kleiner aus.

### N4.4 Offen

1. **Sichtabnahme** der drei Masken (`Form_PV`, `Form_PVModell`, `Form_AdminPV`) — die
   Maße sind gerechnet, nicht gesehen.
2. **Bestätigung der Auslegung „vermiedener Bezug"** (N4.2 Punkt 2).
3. **Berichtigung des Prüfwerts in N2.5** auf 0,9092 (N4.2 Punkt 1).
4. **Katalogpflege `Technologie`** für die sechs Referenzmodule; erst danach wird das
   Schwachlichtmodell in der Referenzmenge wirksam. (Die Pflege von `T_NOCT`, `alpha_SC`
   und `beta_OC` steht aus Paket A ohnehin an — Reparaturskript unter `sql/pv_katalog/`.)
5. **Basiswechsel**, sobald eine Anlage produktiv auf ERWEITERT steht —
   `2026-09-03_PB1_nach-PaketB` gilt nur, solange alle Anlagen EINFACH rechnen.
6. **Neue Migrationsschritte beginnen bei 64.**

## Nachtrag 5 (05.09.2026) — Merge 5: Schritte 63/64, Dialogfelder nach Razor

Mit dem fünften Merge von `origin/ios_migration` (Protokoll
`WindowsFormsApplication1/Allgemein/Simulation/Merge5_ios_2026-09-05_Protokoll.md`) sind die
Schemaschritte dieses Konzepts **umnummeriert**: Paket A ist Schritt **63**
(`SCHRITT_63_PV_ANLAGENPARAMETER`, Katalog `Schritt63_PvAnlagenparameter`), Paket B Schritt **64**
(`SCHRITT_64_PV_MODELLWAHL`, Kataloge `Schritt64_PvModellwahl`, `Schritt64_PvStammUndDegradation`).
Schritt 62 gehört seit iU9‑W14c der Altbereinigung der Klimadaten-Waisen; die Zielversion steht
als `SchemaStand.Zielversion = 64` im Kern, der Freeze-Stand heißt `FREEZE_VERSION` (61). Keine
Anwenderdatenbank hatte die alten Nummern gefahren. **Neue Schritte ab 65.**

Die WinForms-Dialogfelder der Pakete (`Form_PV`, `Form_PVModell`, `Form_AdminPV`, `Form_CECImport`,
`Form_PhotovoltaikVerguetung`, Karten der Simulationskonfiguration) sind mit den iU9-Stilllegungen
auf die Razor-Dialoge umgezogen: Baustein `PvModellFelder` (Rechenmodell, WR-Wirkungsgrad,
Systemverluste, Wechselrichterdaten als Überlagerung) im `PhotovoltaikDialog`; Modulkatalog mit
NOCT und Zelltechnologie (Feldart Auswahl), Erhalt von alpha_SC/beta_OC beim Speichern; Import mit
PAN-Koeffizienten, Technologie und Plausibilitätsprüfung; Degradation im Vergütungsdialog;
Modell-Chip auf der Simulationskarte; Erdreich-Temperaturleser (B1) im Kern. Referenzlauf M5
355/355 byte-gleich zu M4 (Modell EINFACH unverändert). Offen: Sichtabnahme der Razor-Felder.

---

## Nachtrag 6 (06.09.2026) — Wechselrichter: eigenes Konzeptpapier `Konzept_Wechselrichter_EPOS-Plan.md` (W6‑E‑2)

Der Anwenderwunsch W6‑E‑2 („Wechselrichter – ausgegraut. Import liegt nicht vor, Admin zum
Anlegen/Bearbeiten liegt nicht vor …") holt die hier zweimal zurückgestellten Punkte nach vorn —
Entscheidungsfrage **Q5** (Wechselrichter als Anlagenparameter statt eigenem Katalog) und Stufe
**E3** (Stringauslegung); sie sind deshalb in ein eigenes Papier
[`Konzept_Wechselrichter_EPOS-Plan.md`](Konzept_Wechselrichter_EPOS-Plan.md) ausgelagert, samt
Mockup `Mockups/Wechselrichter_Mockup_2026-09-06.html`. Es schlägt einen Katalog
`Tab_Wechselrichter_STAMM` mit Projektkopie, die Strangzuordnung `Z_AnlageStrang` (Migrationsschritte
ab 65), eine Kennlinie aus sechs Stützstellen, den CEC-Wechselrichterimport und den Rechenweg
Module → Strang → MPPT → Gerät → Clipping vor; **ohne Strangzuordnung bleibt der Rechenweg dieses
Papiers Zeichen für Zeichen erhalten**, damit die Bitgleichheit gegen
`Referenzlaeufe/2026-09-05_R2_Zeitbasis` bestehen bleibt. Nichts davon ist umgesetzt — zehn
Entscheidungsfragen W6‑E‑2‑Q1…Q10 liegen beim Anwender.

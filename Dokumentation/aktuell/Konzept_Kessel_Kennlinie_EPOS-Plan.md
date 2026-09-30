# Konzept — Teillast- und Brennwertkennlinie des Heizkessels

**Anwenderentscheid 26.09.2026** („Optional Teillast-/Brennwertkennlinie") · Statusnummer **#569** ·
Entscheide F1 bis F5 vom 29.09.2026 (Abschnitt 7).

**Stand 29.09.2026** · Etappe **E1 umgesetzt** (Schemaschritt **156**, `KesselKennlinieSchema`) ·
`SchemaStand.Zielversion` = 156 · Referenzbasis `2026-09-29_R26_Kesselrest` (byte-gleich) · E2 bis E4 offen.
Befund und Optionen (Abschnitte 1 und 2) sind vom 26.09.2026 (Codestand `0a483bd4`, Schemastand 150).

Ziel: Der Heizkessel rechnet heute mit einem festen Wirkungsgrad. Das Papier legt fest, wie eine
**optionale** Kennlinie — Wirkungsgrad über der Last und, beim Brennwertkessel, über der
Rücklauftemperatur — ins Datenmodell und in die Stundenrechnung kommt, **ohne** dass ein Kessel
ohne gepflegte Kennlinie anders rechnet als heute.

---

## 1 Ausgangsbefund

### 1.1 Rechenweg heute (`EPOS.Kern/Allgemein/Simulation/SimulationSPK.cs`)

| Schritt | Stelle | Verhalten |
|---|---|---|
| Einlesen | `Kesseldaten_Einlesen` :237–248 | `Wirkungsgrad_Gas` bzw. `Wirkungsgrad_Öl` des Projektgeräts; Werte > 1,5 gelten als Prozent und werden durch 100 geteilt |
| Bereitschaft | :253–254, `BereitschaftsleistungKw` :303 | `Betriebsbereitschaftverlust` als Leistung in kW (#559); Hinweis ab 2 % der Nennleistung |
| Leistungsgrenze | `Stunde_Start` :1135, `MaxAbgabe` :892 | je Stunde Restleistung = Nennleistung `Ptherm`; **keine Mindestlast, keine Modulationsgrenze** |
| Wärmeabgabe | `Stunde_Bedarf`, `Zweikanalig_Laden` | `_kesselStunde[i]` = brennstoffbasierte Wärme der Stunde, `_kesselAbgabe[i]` = samt Quellpufferanteil |
| Brennstoff | `Stunde_Abschluss` :1305–1351 | Öl nach Brennstoff-ID 6–9/18–22, sonst Gas-Feld; `wirk ≤ 0` → 0,90 (:1315). Läuft der Kessel (`_kesselAbgabe > 0`): **Brennstoff = Q / η** (:1325), Gasspitze Q/η (:1335). Steht er: Brennstoff = Bereitschaftsleistung × 1 h (:1344) |
| Jahresbilanz | `Bilanz_und_Nutzungsgrad` :435 | Jahresnutzungsgrad = Σ Nutzwärme / Σ Brennstoff (:511); Emissionen aus dem Brennstoff über die Emissionsquelle des Energieträgers |

`Stunde_Abschluss` ist die **eine** Stelle, die aus Wärme Brennstoff macht; beide Rechenwege (mit und
ohne Speicherbeteiligung, :1400 und `Kaskadenschleife.cs:1021`) rufen sie genau einmal je Stunde.
**Starts werden nicht gezählt**, eine Laststufe wird nicht geführt — sie ist aber je Stunde als
`_kesselStunde[i] / Ptherm` ablesbar. Die Begrenzung der Bereitschaft auf die
Betriebsbereitschaftsstunden (#568) steht auf diesem Codestand noch nicht; das Papier setzt sie als
vorgelagert voraus (Abschnitt 4).

Die Temperatur, auf die der Kessel anhebt, kennt der Lauf schon: `SimulationControl.KesselKopplungSetzen`
(:4012 ff.) bestimmt das Paar aus dem Senkenspeicher (`VL_eff`/`RL_eff`), der gepflegten Kette
Anlage → `Tab_Heizkessel.Vorlauf/Ruecklauf` (`KesselTemperaturpaarGepflegt`) oder dem Rückfall 70/50 °C.
Bei Anlagenkopplung liefert `HeizkreisErgebnis.RuecklaufC` (`Simulation/Gebaeude/HeizkreisErgebnis.cs:87`)
den gerechneten Rücklauf je Stunde; den Vorlauf reicht der Lauf heute schon an die Wärmepumpe
(`SimulationControl.cs:1311`).

### 1.2 Katalogfelder (`Tab_Heizkessel_STAMM` / `Tab_Heizkessel`, `STRICT`, je 23 Spalten)

Vorhanden: `Ptherm`, `Brennstoff`, `Wirkungsgrad_Gas`, `Wirkungsgrad_Öl`,
`Betriebsbereitschaftverlust` (kW), **`Brennwert`** (`INTEGER CHECK IN (0,1)`, heute nur Bericht:
`AbweichungsErmittler`, in `ParameterVerwendung` als BER geführt), `Vorlauf`, `Ruecklauf` (°C, rechnen
im Temperaturmodus „Fest“ mit). **Nicht vorhanden:** Teillastwirkungsgrad, Mindest- bzw.
Modulationsleistung, Anfahrverlust, Mindestlaufzeit.

### 1.3 VDI 3805 Blatt 3 — was die Dateien tragen

`EPOS.Kern/Allgemein/Import/VDI 3805/Heizkesselmport.cs` liest aus Satz **700** Name, Bauart
(Spalte 14, landet in `Beschreibung`), Nennleistung (5), Wirkungsgrad (26) und Verluste (28,
Bereitschaftsleistung in kW); aus **710.01** Spalte 6 als Rückfall des Wirkungsgrads; aus 710.05
CO₂/CO/NOx; aus 710.11 den Brennstoff. Satz **710.01** steht je **Temperaturpaar** (in den
Importproben 40/30 und 75/60) und trägt außerdem eine **Mindest- und Höchstleistung** (Spalten 3/4)
und **drei Wirkungsgrad-Prozentwerte** (Spalten 5–7: in den Proben einer um 96 %, zwei über 100 % —
heizwertbezogene Brennwertwerte). Nach Stellung und Größe sind das Normnutzungsgrad, Wirkungsgrad
bei Nennlast und Wirkungsgrad bei 30 % Teillast nach Wirkungsgradrichtlinie 92/42/EWG. **In E1 belegt** — nach Stellung und
Größe an allen 1 650 Kesselsätzen der Dateien unter `VDI-3805-Daten/SPK-Daten/` (der Blatttext selbst
liegt nicht vor): Spalte 3/4 kleinste und größte Leistung (in einigen Dateien vertauscht), 5
Normnutzungsgrad, 6 Wirkungsgrad bei Nennlast (86–107 %, Median 97,9 % beim Gas-Brennwertkessel), 7
Wirkungsgrad bei 30 % Last (stets unter Hs/Hi; ein Ausreißer „9.5").
Spalte 26 des Satzes 700 liegt in den Proben um 87–91 % — vermutlich ein brennwertbezogener
Jahresnutzungsgrad (ErP ηs); als heizwertbezogener Wirkungsgrad übernommen, rechnet ein
Brennwertkessel zu schlecht (Nebenbefund N1, Abschnitt 6). Die Bauart „Brennwert-Kessel“ setzt das
Feld `Brennwert` **nicht**.

### 1.4 Was die Testdatenbank trägt (Zählung per SQL, Schemastand 150)

| | `Tab_Heizkessel_STAMM` (63) | `Tab_Heizkessel` (25) |
|---|---|---|
| `Brennwert` = 1 | 6 | 3 (Projekt 1009 zweimal, **1023**) |
| Bauart „Brennwert“ in `Beschreibung` | 46 (davon 6 mit `Brennwert` = 1) | 22 (davon 3) |
| η = 1,0 (Platzhalter des Imports) | 3 | 13 (darunter **1018, 1030, 1040, 1041, 1045, 1049**) |
| η 0,5 … 0,999 | 56 | 11 |
| η > 1 (Brennwert heizwertbezogen) | 0 | 0 |
| η als Prozent gepflegt (> 1,5) | 1 | 1 |
| Bereitschaft > 0 kW | 35 (höchstens 0,12) | 8 (höchstens 0,057) |
| `Vorlauf`/`Ruecklauf` gepflegt | 0 | 0 |
| Elektrokessel (Brennstoff 13) | 8 | 3 |

Befund: Die Brennwertkennzeichnung ist lückenhaft (6 von 46), η > 1 kommt nicht vor, das
Temperaturpaar ist nirgends gepflegt. **Das Referenzprojekt 1023 trägt `Brennwert` = 1** — ein
Rechenweg, der am vorhandenen Schalter hinge, verschöbe 1023 sofort.

---

## 2 Modelloptionen

| | Modell | Daten | Aufwand | Nutzen |
|---|---|---|---|---|
| **A** | **Teillastkennlinie** η(β) aus η₁₀₀ (Katalogwert heute) und η₃₀; zwischen β = 0,3 und 1 linear, darunter η₃₀ (bzw. Taktabschlag nach D) | eine neue Spalte; Werte aus Datenblatt und VDI 3805 Satz 710.01 | klein | Kessel im Teillastbetrieb (Regelfall als Spitzen- und Grundlastkessel) rechnen realistisch; Jahresnutzungsgrad nicht mehr ≈ η₁₀₀ |
| **B** | **Brennwertkennlinie** η(T_RL, Brennstoff): oberhalb des Taupunkts trocken, darunter linear steigender Kondensationsgewinn bis 30 °C Rücklauf | Taupunkt, Kondensationsgewinn und Hs/Hi je Brennstoff (Kerntabelle, keine Katalogpflege), Rücklauf je Stunde | mittel (Rücklauf je Stunde zuführen) | Brennwertnutzung und ihre Abhängigkeit vom Heizkreis werden sichtbar — mit Anlagenkopplung AK1 die eigentliche Aussage |
| **C** | **A + B**: trockene Teillastkurve plus Kondensationsgewinn nach Rücklauf (Formel 4.1) | wie A und B | A + B + klein | vollständige Aussage; die Formel trifft η₃₀ bei 30 °C und η₁₀₀ bei 60 °C exakt — keine Doppelzählung |
| **D** | **Taktverluste**: Starts aus Stillstandswechsel und aus Takten unter der Mindestleistung, Anfahrverlust in kWh je Start | `Mindestleistung` (VDI 710.01 Spalte 3), `Anfahrverlust_kWh`, `Mindestlaufzeit_min` | mittel | Startzahl als Kennzahl (Verschleiß, Überdimensionierung); Energieeffekt klein (einige Promille bis wenige Prozent) |

Quellen der Zahlen (keine Herstellerdaten im Papier, Beispiele neutral): Wirkungsgradrichtlinie
92/42/EWG (η bei Nennlast und bei 30 % Teillast, Prüfrücklauf 30 °C beim Brennwertkessel),
DIN EN 15316-4-1 (Erzeugerverluste nach Last, Takt- und Bereitschaftsverluste), DIN V 4701-10 und
DIN V 18599-5 (Standardkennwerte), DIN 4702-8 (Normnutzungsgrad), VDI 3805 Blatt 3 (Sätze 700 und
710.01), Herstellerdatenblätter. Physik der Brennwertnutzung (Näherungen, vor E3 gegen die Norm zu
belegen): Hs/Hi Erdgas ≈ 1,11, Heizöl ≈ 1,06, Holzpellets ≈ 1,08; Abgastaupunkt Erdgas ≈ 57 °C,
Heizöl ≈ 47 °C, Pellets ≈ 50 °C; Kondensationsgewinn bei 30 °C Rücklauf Δ₃₀ Erdgas ≈ 0,08,
Heizöl ≈ 0,04 (auf Hi bezogen).

**Beispiel (neutral):** Kessel 1, Erdgas, 100 kW, η₁₀₀ = 0,97 und η₃₀ = 1,05 (beide Hi). Nach A gilt
bei β = 0,6: η = 1,05 + (0,97 − 1,05) · (0,6 − 0,3)/0,7 ≈ 1,016; 60 kWh Wärme brauchen 59,1 statt
61,9 kWh Brennstoff.

---

## 3 Datenmodell

### 3.1 Neue Spalten (in `Tab_Heizkessel_STAMM` und `Tab_Heizkessel` gleich)

| Spalte | Typ | Bedeutung | leer heißt (F1, wirksam ab E2 bzw. E4) |
|---|---|---|---|
| `Wirkungsgrad_Teillast30` | `REAL`, NULL erlaubt | η bei 30 % Last, Hi, Faktor | **Normvorgabe** nach Bauart (7.1) |
| `Kennlinie_Brennwert` | `INTEGER NOT NULL DEFAULT 0 CHECK (Kennlinie_Brennwert IN (0,1))` | Brennwertkennlinie rechnen | 0 = aus, auch wenn `Brennwert` = 1 |
| `Mindestleistung` | `REAL` (kW), NULL erlaubt | untere Modulationsgrenze | Normvorgabe (7.1) |
| `Anfahrverlust_kWh` | `REAL`, NULL erlaubt | Brennstoff je Start | Normvorgabe (7.1) |
| `Mindestlaufzeit_min` | `INTEGER`, NULL erlaubt | Startzählung im Takten | Normvorgabe 10 min (7.1) |

Der vorhandene Schalter `Brennwert` bleibt Beschreibung und Filter; **gerechnet wird die
Brennwertkennlinie nur mit `Kennlinie_Brennwert` = 1**, das der Controller nur bei `Brennwert` = 1
zulässt. Grund: 1023 und jeder Anwenderkatalog mit gesetztem Schalter rechneten sonst sofort anders.
Die Katalogwerte sind **heizwertbezogen** und **Faktoren**; die bestehende Prozentregel (> 1,5 → /100)
gilt auch für die neue Spalte. Der Name `Wirkungsgrad_Teillast30` gilt für Gas und Öl gemeinsam, weil
ein Gerät genau einen Brennstoff hat.

### 3.2 Schemaschritt

Schritt **156** (`KesselKennlinieSchema`, gebaut in E1; beim Bau gegen `origin` gemessen — 155 trägt die
Verfahrensvolumina der Füllstandslinie, `TwwFuellstandSchema`). Der Schritt fügt die fünf Spalten per
`ALTER TABLE … ADD COLUMN` an beide Tabellen, setzt **keine** Werte und ist wiederholbar
([ADR-001](ADR-001_Schema-Ausrollung.md)); eine Quelle für Migration, Werkzeug `Testdatenbankschema` und
Testvorrichtung, `Paketanhebung` Stufe 156 (DDL).

### 3.3 Keine Neueinfrierung durch den Datenschritt

In E1 liest kein Rechenweg die Spalten. Keine Projektkopie trägt nach Schritt 156 einen Kennlinienwert
(alle NULL bzw. 0), und `Brennwert` = 1 allein schaltet nichts — die Basis R26 bleibt **byte-gleich**
(fünfzehn Projekte, 460/460 CSV). Die Nachpflege des Katalogs (F2) ändert nur `Tab_Heizkessel_STAMM`, nie
eine Projektkopie. Eine Neueinfrierung fällt mit E2 an: mit dem neuen Referenzprojekt (4.3) und — nach
F1 — mit den Normvorgaben für leere Felder (Abschnitt 7), die jeden Kessel ohne gepflegte Kennlinie
verschieben. Neue Einfrierregel für `Referenzlaeufe/LIESMICH.md`: „gesäte
Kesselkennlinie“ (die fünf Spalten eines Referenzkessels und sein `Brennwert`).

### 3.4 Import, Editor, KI

- **VDI 3805:** Satz 710.01 je Temperaturpaar lesen; η₁₀₀ (Spalte 6, F3) und η₃₀ (Spalte 7) des Paars
  mit dem niedrigsten Rücklauf, `Mindestleistung` als kleinerer Wert der Spalten 3 und 4 (einige Dateien
  führen sie vertauscht); Prozentregel und sechs Nachkommastellen, ein unplausibles η₃₀ und eine
  Mindestleistung über der Nennleistung bleiben leer. Bauart „Brennwert…“ setzt `Brennwert` = 1
  (behebt die Lücke 6 von 46), **nicht** `Kennlinie_Brennwert`. Importproben um Erwartungswerte ergänzt
  (`heizkessel_sonderfaelle.vdi`).
- **Katalogeditor** (`EPOS.UI/Dialoge/Erzeuger/HeizkesselKatalogDialog.razor`, `KatalogBrowserProfil`
  Heizkessel): Gruppe „Kennlinie“ mit den fünf Feldern, Platzhalter „Vorgabe“ und Kurzhinweis „leer =
  Vorgabe“; Texte in `MyResource.Resource.*` (beide Sprachen, ResourceDesigner gezogen);
  `ParameterVerwendung` führt die fünf Spalten in E1 als gepflegt (DLG) — SIM kommt mit dem Rechenweg.
- **KI-Feldtafel** (`KiDialoge.cs`, `HeizkesselKatalogDaten`): dieselben Felder mit Erläuterung.

---

## 4 Rechenweg

### 4.1 Je Stunde und Kessel (in `Stunde_Abschluss`, sonst unverändert)

1. **Läuft der Kessel?** wie heute über `_kesselAbgabe > 0`. Steht er: Bereitschaft wie #559, auf die
   Betriebsbereitschaftsstunden begrenzt (#568). Ende.
2. **Laststufe** β = `_kesselStunde / Ptherm`, auf (0, 1] geklemmt. Elektrokessel (Brennstoff 13)
   rechnen nie mit Kennlinie.
3. **Trockene Teillastkurve.** Mit η₃₀ und ohne Brennwertkennlinie:
   η_tr(β) = η₃₀ + (η₁₀₀ − η₃₀) · (β − 0,3)/0,7 für β ≥ 0,3, darunter η₃₀ (Option A).
   Mit Brennwertkennlinie wird der Kondensationsanteil aus dem Katalogwert genommen, weil η₃₀ bei
   30 °C Rücklauf gemessen ist: η₃₀,tr = η₃₀ − Δ₃₀. Ist η₃₀ leer, gilt die Normvorgabe nach Bauart
   (7.1, Entscheid F1).
4. **Rücklauf der Stunde** (nur mit `Kennlinie_Brennwert` = 1), erste belegte Stufe gilt:
   (a) Anlagenkopplung: `HeizkreisErgebnis.RuecklaufC[h]`, NaN fällt durch;
   (b) Senkenspeicher: `RL_eff`, bei geschichtetem Speicher die Temperatur der untersten Schicht,
   einmal je Stunde gelesen wie `Quelltemperatur_Stunde`; (c) gepflegtes Paar Anlage → Katalog;
   (d) Rückfall `KESSEL_RUECKLAUF_RUECKFALL` (50 °C, Frage F5).
5. **Kondensationsgewinn** g(T) = (T_Tau − T_RL)/(T_Tau − 30), auf [0, 1,2] geklemmt;
   η_eff = η_tr(β) + Δ₃₀ · g(T_RL), höchstens Hs/Hi des Brennstoffs. Probe: β = 0,3 und 30 °C ergibt
   η₃₀, β = 1 und 60 °C ergibt η₁₀₀ — der Katalog wird an beiden Prüfpunkten exakt getroffen.
6. **Brennstoff** = Q/η_eff (+ Anfahrverlust nach 4.2); Gasspitze aus demselben Wert. Emissionen,
   Energieträgerzähler und Jahresnutzungsgrad folgen unverändert aus dem Brennstoff.
7. **Mitschreiben:** Mehrbrennstoff je Verlustart gegenüber η₁₀₀ (Teillast, Bereitschaft, Anfahren),
   Brennwertstunden und -wärme (T_RL < T_Tau), wärmegewichtetes η_eff, Starts.

Die Funktion aus 3. bis 5. steht als reine, zustandslose Funktion `Kesselkennlinie.Eta(…)` im Kern —
Rechenweg, Editorkurve und Tests rufen dieselbe.

### 4.2 Takten (Option D)

Mit der Mindestleistung P_min (gepflegt, sonst Normvorgabe nach 7.1) und 0 < Q < P_min in der Stunde
taktet der Kessel: Starts der Stunde =
min(60/`Mindestlaufzeit_min`, ⌈Q/(P_min · `Mindestlaufzeit_min`/60)⌉); sonst ein Start, wenn der
Kessel in der Vorstunde stand. Brennstoff += Starts × `Anfahrverlust_kWh`. Die Startzahl wird auch
ohne Anfahrverlust ausgewiesen; leere Felder nehmen die Normvorgaben (7.1, F1).

### 4.3 Nachweis

- **Unit-Tests** (neue Klasse `KesselKennlinieTests`): η(β = 0,3) = η₃₀ und η(β = 1) = η₁₀₀; ohne
  Kennlinie η ≡ Katalog; Brennwert Erdgas: 60 °C → kein Gewinn, 30 °C bei β = 0,3 → η₃₀; Heizöl mit
  Taupunkt 47 °C; NaN-Rücklauf fällt auf die nächste Stufe; Elektrokessel nie mit Kennlinie;
  Obergrenze Hs/Hi; Takten mit Startzahl; leere Felder nehmen die Normvorgaben (7.1);
  Elektrokessel in `Stunde_Abschluss` Wert für Wert wie heute.
- **Referenzprojekt:** neues Projekt als Kopie von 1023 (Wärmepumpe + Gaskessel, `Brennwert` = 1)
  mit gepflegter Kennlinie (neutrale Werte η₁₀₀ 0,97, η₃₀ 1,05, `Kennlinie_Brennwert` = 1,
  Mindestleistung 20 % der Nennleistung, Anfahrverlust 0,1 kWh); Skript unter `Referenzlaeufe/Skripte/`,
  dann Neueinfrierung der Basis nach R26 mit Begründung in `Referenzlaeufe/LIESMICH.md`. Das Projekt
  steht nicht in der CI-Auswahl, eine AK1-Variante entfällt (Entscheid F4).

---

## 5 Oberfläche

- **Katalogeditor:** Gruppe „Kennlinie“ (3.4) mit einer kleinen Kurve η(β) bei 30, 50 und 60 °C
  Rücklauf aus derselben Kernfunktion.
- **Ergebnis, Kessel-Reiter** (`EPOS.UI.Daten/Simulation/SimulationErgebnisHuelle.Anzeige.cs`,
  `EPOS.UI/Seiten/Simulation/ErgebnisReiter.razor`): je Kessel η_eff im Jahresmittel, Anteil
  Brennwertbetrieb (Stunden und Wärme), Verluste je Art in MWh, Starts je Jahr; Zeitreihen η_eff
  und T_RL im Export.
- **Kohärenzzeile** (Warnkriterienkatalog `Simulation/Warnkriterien.cs`, weich): „Brennwertkessel mit
  Rücklauf über dem Taupunkt in x % der Betriebsstunden“ ab 50 %; dazu der Hinweis
  „Brennwertkessel ohne Kennlinie“ (`Brennwert` = 1, `Kennlinie_Brennwert` = 0) an der Karte.
- **Bericht:** η_eff, Brennwertanteil und Starts in den Vorlagenfeldkatalog.

---

## 6 Etappen, Aufwand, Empfehlung

| Etappe | Inhalt | Aufwand | Basis |
|---|---|---|---|
| **E1** Daten + Import — **umgesetzt** | Schritt 156, Modelle und Controller, Satz 710.01 lesen (Spalten belegt), Bauart → `Brennwert`, Nachpflege des Bestandskatalogs (F2), Editor- und KI-Felder, Ressourcen | 1,5 Tage | byte-gleich |
| **E2** Teillast | `Kesselkennlinie` im Kern, `Stunde_Abschluss`, Normvorgabe η₃₀ (F1), Tests, Kennzahlen, Reiter, Referenzprojekt | 1 Tag | Neueinfrierung (neues Projekt und jeder Kessel ohne η₃₀, F1) |
| **E3** Brennwert | Rücklaufkette (AK1, Speicher, Paar, Rückfall 50 °C nach F5), Brennstofftabelle Taupunkt/Δ₃₀/Hs/Hi, Kohärenzzeile | 1,5–2 Tage | Neueinfrierung (nur das neue Projekt) |
| **E4** Takten | Startzählung, Anfahrverlust, Mindestlaufzeit, Kennzahl Starts, Normvorgaben (F1) | 1 Tag | Neueinfrierung (Normvorgaben, F1) |

**Empfehlung:** mit **E1 + E2** beginnen — ein kleiner Eingriff an einer Stelle, die Daten liefert
VDI 3805 schon, und E1 behebt nebenbei die Brennwertkennzeichnung des Imports. E3 danach, weil es den
Rücklauf aus AK1 und dem Speicher zuführen muss und erst mit dem Referenzprojekt belastbar wird; E4
zuletzt (kleiner Energieeffekt, Nutzen vor allem als Kennzahl).

**Nebenbefund N1:** Satz-700-Spalte 26 ist vermutlich brennwertbezogen (ErP ηs); das Projektgerät von
1023 rechnet mit 0,874. Der Import nimmt den Nennlastwert seit E1 aus 710.01 (F3); geändert haben sich
neu importierte Geräte und die nachgepflegten Katalogsätze (F2), keine Projektkopie.

### Anwenderfragen

Die fünf Fragen sind am 29.09.2026 entschieden — Wortlaut und Folgen in Abschnitt 7.

---

## 7 Entscheide 29.09.2026

| Frage | Entscheid | Umsetzung |
|---|---|---|
| **F1** Standardwerte | Leere Felder bekommen **Normvorgaben** (Tabelle unten). | Werte hier festgelegt; wirksam erst mit E2 (η₃₀) und E4 (Takten). In E1 wirkungslos. |
| **F2** Bestandskatalog | `Tab_Heizkessel_STAMM` wird aus den VDI-3805-Dateien **nachgepflegt**; die Auslieferungsvorlage folgt. | E1: `KesselkatalogNachpflege` im Kern, auf Zuruf über `Werkzeuge/Testdatenbankschema --kesselkatalog <ordner>` und `Werkzeuge/Auslieferungsvorlage --kesselkatalog <ordner>` (vor jeder Auslieferung mit `VDI-3805-Daten/SPK-Daten`). Nie eine Projektkopie. |
| **F3** Nennlastwert | η₁₀₀ kommt aus **Satz 710.01** (Spalte 6), Satz 700 Spalte 26 nur als Rückfall. | E1: Import und Nachpflege. |
| **F4** Referenzprojekt | **Kopie von 1023**, keine AK1-Variante, **nicht** in der CI-Auswahl. | Mit E2. |
| **F5** Rückfall-Rücklauf | **50 °C** (Rückfallpaar 70/50, Empfehlung). | Mit E3 (`KESSEL_RUECKLAUF_RUECKFALL`). |

**Stand E1.** Schemaschritt 156 an Katalog und Projektkopie; Modelle und beide Controller lesen und
schreiben die Felder (leer = NULL, der Schalter nur mit `Brennwert` = 1); der Import von Blatt 3 liest
Satz 710.01 (η₁₀₀, η₃₀, kleinste Leistung; das Paar mit dem niedrigsten Rücklauf zuerst), setzt
`Brennwert` aus der Bauart und lässt `Kennlinie_Brennwert` aus; Katalogeditor, Aufklapper „Alle Daten“
und KI-Feldtafel führen die fünf Felder mit dem Kurzhinweis „leer = Vorgabe“. Nachpflege der
Testdatenbank: 60 von 63 Katalogsätzen zugeordnet und nachgepflegt, danach **46 Brennwertgeräte**
(vorher 6), η₃₀ in 46 Sätzen; Projektkopien zellgleich; Basis R26 byte-gleich.

### 7.1 Normvorgaben für leere Felder (F1)

Gelten erst mit E2 bzw. E4 und nur, wo das Feld leer ist; ein gepflegter Wert geht immer vor.
Elektrokessel (Brennstoff 13) bekommen keine Kennlinie und kein Taktmodell. Die Bauart eines Kessels
bestimmt E2 so: `Brennwert` = 1 → Brennwertkessel; eine Beschreibung mit der VDI-Bauart „Standard…“ →
Standardkessel; sonst Niedertemperaturkessel.

| Größe | Vorgabe | Herleitung und Quelle |
|---|---|---|
| η₃₀ Brennwertkessel | **η₁₀₀ + 0,06**, höchstens Hs/Hi des Brennstoffs | Abstand der Mindestwirkungsgrade bei 30 % Teillast (Rücklauf 30 °C) und bei Nennlast für Brennwertkessel, Richtlinie 92/42/EWG Art. 5 (EUR-Lex), gerundet; Prüfpunkte 100 % und 30 % auch in Verordnung (EU) 813/2013 Anhang III. Bewusst vorsichtig: Die Herstellerdateien im Repositorium liegen im Median bei +0,107 (Gas) und +0,063 (Öl). |
| η₃₀ Niedertemperaturkessel | **η₁₀₀** (keine Teillastanhebung) | Richtlinie 92/42/EWG Art. 5: gleiche Mindestanforderung bei Nennlast und bei 30 % Teillast; Herstellerdateien im Median +0,03. |
| η₃₀ Standardkessel | **η₁₀₀ − 0,03** | Richtlinie 92/42/EWG Art. 5: Die Mindestanforderung bei 30 % Teillast liegt für kleine Leistungen rund 3 Prozentpunkte unter der bei Nennlast, gerundet. |
| Mindestleistung | **30 %** der Nennleistung beim Gas-Brennwertkessel, **60 %** bei allen übrigen Brennstoffkesseln | Prüfpunkt Teillast 30 % (Verordnung (EU) 813/2013, Richtlinie 92/42/EWG) als untere Grenze der Modulation; eigene Auswertung der Herstellerdateien (Satz 710.01 Spalte 3/4): Median 19 % Gas-Brennwert, 58 % Öl-Brennwert, 71 % Niedertemperatur — gerundet zur vorsichtigen Seite. |
| Anfahrverlust | **0,002 h × Nennleistung** je Start (rund 7 s Volllastbrennstoff; 20 kW → 0,04 kWh) | Eigene physikalische Abschätzung (Vorspülung, Wiederaufheizen von Brennkammer und Wärmetauscher); keine Normquelle. E4 prüft den Wert am Referenzprojekt. |
| Mindestlaufzeit | **10 min** | Konzept 3.1; übliche Werkseinstellung der Taktsperre von Kesselregelungen (Herstellerunterlagen, ohne Produktbezug). |

Keine Tabelle einer kostenpflichtigen Norm (DIN V 4701-10, DIN EN 15316-4-1) ist abgeschrieben; die
Werte sind gerundete Ableitungen aus öffentlichen Rechtsquellen und der eigenen Auswertung.

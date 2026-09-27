# Konzept — Teillast- und Brennwertkennlinie des Heizkessels

**Anwenderentscheid 26.09.2026** („Optional Teillast-/Brennwertkennlinie") · Statusnummer **#569** ·
nur Konzept, keine Codeänderung.

**Stand 26.09.2026** · Codestand `0a483bd4` · `SchemaStand.Zielversion` = **150**
(`SolarkollektorTemperaturen.SCHRITT`) · Referenzbasis `2026-09-26_R22_Solarthermie`.

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
bei Nennlast und Wirkungsgrad bei 30 % Teillast nach Wirkungsgradrichtlinie 92/42/EWG; **die
Zuordnung ist vor E1 gegen den Blatttext zu belegen**, der Parser liest bisher nur Spalte 6.
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

| Spalte | Typ | Bedeutung | leer heißt |
|---|---|---|---|
| `Wirkungsgrad_Teillast30` | `REAL`, NULL erlaubt | η bei 30 % Last, Hi, Faktor | **keine Teillastkennlinie** (A aus) |
| `Kennlinie_Brennwert` | `INTEGER NOT NULL DEFAULT 0 CHECK (Kennlinie_Brennwert IN (0,1))` | Brennwertkennlinie rechnen | 0 = aus, auch wenn `Brennwert` = 1 |
| `Mindestleistung` | `REAL` (kW), NULL erlaubt | untere Modulationsgrenze | kein Taktmodell |
| `Anfahrverlust_kWh` | `REAL`, NULL erlaubt | Brennstoff je Start | 0 |
| `Mindestlaufzeit_min` | `INTEGER`, NULL erlaubt | Startzählung im Takten | 10 min (nur mit `Mindestleistung`) |

Der vorhandene Schalter `Brennwert` bleibt Beschreibung und Filter; **gerechnet wird die
Brennwertkennlinie nur mit `Kennlinie_Brennwert` = 1**, das der Controller nur bei `Brennwert` = 1
zulässt. Grund: 1023 und jeder Anwenderkatalog mit gesetztem Schalter rechneten sonst sofort anders.
Die Katalogwerte sind **heizwertbezogen** und **Faktoren**; die bestehende Prozentregel (> 1,5 → /100)
gilt auch für die neue Spalte. Der Name `Wirkungsgrad_Teillast30` gilt für Gas und Öl gemeinsam, weil
ein Gerät genau einen Brennstoff hat.

### 3.2 Schemaschritt

Nächste freie Nummer **151** (gemessen: `SchemaStand.Zielversion` = `SolarkollektorTemperaturen.SCHRITT`
= 150; kein Papier unter `aktuell/` beansprucht 151). **Nur genannt, nicht angelegt** — die Nummer ist
beim Bau erneut zu messen, weil parallele Aufträge sie belegen können. Der Schritt fügt die fünf
Spalten per `ALTER TABLE … ADD COLUMN` an beide Tabellen, setzt **keine** Werte und ist wiederholbar
([ADR-001](ADR-001_Schema-Ausrollung.md)); nach dem Bau den `SqlDialektPruefer` ziehen.

### 3.3 Keine Neueinfrierung durch den Datenschritt

Die Vorgaben bei leeren Feldern sind exakt das heutige Verhalten: η = Katalogwert, kein
Rücklaufeinfluss, keine Starts. Kein Referenzprojekt trägt nach Schritt 151 einen Kennlinienwert
(alle NULL bzw. 0), und `Brennwert` = 1 allein schaltet nichts — die Basis R22 bleibt **byte-gleich**;
das belegt der Referenzlauf der fünfzehn Projekte im Gate. Eine Neueinfrierung fällt erst mit dem
neuen Referenzprojekt (4.3) an. Neue Einfrierregel für `Referenzlaeufe/LIESMICH.md`: „gesäte
Kesselkennlinie“ (die fünf Spalten eines Referenzkessels und sein `Brennwert`).

### 3.4 Import, Editor, KI

- **VDI 3805:** Satz 710.01 je Temperaturpaar lesen; η₃₀ aus der belegten Spalte (1.3) des Paars mit
  dem niedrigsten Rücklauf, `Mindestleistung` aus Spalte 3. Bauart „Brennwert…“ setzt `Brennwert` = 1
  (behebt die Lücke 6 von 46), **nicht** `Kennlinie_Brennwert`. Importproben um Erwartungswerte ergänzen.
- **Katalogeditor** (`EPOS.UI/Dialoge/Erzeuger/HeizkesselKatalogDialog.razor`, `KatalogBrowserProfil`
  Heizkessel): Gruppe „Kennlinie“ mit den fünf Feldern; Texte in `MyResource.Resource.*` (beide
  Sprachen, ResourceDesigner ziehen); `ParameterVerwendung` erhält SIM-Einträge.
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
   30 °C Rücklauf gemessen ist: η₃₀,tr = η₃₀ − Δ₃₀. Ohne η₃₀: η_tr = η₁₀₀.
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

Mit `Mindestleistung` > 0 und 0 < Q < P_min in der Stunde taktet der Kessel: Starts der Stunde =
min(60/`Mindestlaufzeit_min`, ⌈Q/(P_min · `Mindestlaufzeit_min`/60)⌉); sonst ein Start, wenn der
Kessel in der Vorstunde stand. Brennstoff += Starts × `Anfahrverlust_kWh`. Die Startzahl wird auch
ohne Anfahrverlust ausgewiesen, sobald `Mindestleistung` gepflegt ist.

### 4.3 Nachweis

- **Unit-Tests** (neue Klasse `KesselKennlinieTests`): η(β = 0,3) = η₃₀ und η(β = 1) = η₁₀₀; ohne
  Kennlinie η ≡ Katalog; Brennwert Erdgas: 60 °C → kein Gewinn, 30 °C bei β = 0,3 → η₃₀; Heizöl mit
  Taupunkt 47 °C; NaN-Rücklauf fällt auf die nächste Stufe; Elektrokessel nie mit Kennlinie;
  Obergrenze Hs/Hi; Takten mit Startzahl; `Stunde_Abschluss` ohne Kennlinie Wert für Wert wie heute.
- **Referenzprojekt:** neues Projekt als Kopie von 1023 (Wärmepumpe + Gaskessel, `Brennwert` = 1)
  mit gepflegter Kennlinie (neutrale Werte η₁₀₀ 0,97, η₃₀ 1,05, `Kennlinie_Brennwert` = 1,
  Mindestleistung 20 % der Nennleistung, Anfahrverlust 0,1 kWh); Skript unter `Referenzlaeufe/Skripte/`,
  dann Neueinfrierung **R23** mit Begründung in `Referenzlaeufe/LIESMICH.md`; alle übrigen Projekte
  byte-gleich. Eine AK1-Variante ist offen (Frage F4): 1047 deckt mit einem Elektrokessel.

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
| **E1** Daten + Import | Schritt 151, Modelle und Controller, Satz 710.01 lesen (Spalten belegt), Bauart → `Brennwert`, Editor- und KI-Felder, Ressourcen | 1,5 Tage | byte-gleich |
| **E2** Teillast | `Kesselkennlinie` im Kern, `Stunde_Abschluss`, Tests, Kennzahlen, Reiter, Referenzprojekt | 1 Tag | Neueinfrierung R23 (nur das neue Projekt) |
| **E3** Brennwert | Rücklaufkette (AK1, Speicher, Paar, Rückfall), Brennstofftabelle Taupunkt/Δ₃₀/Hs/Hi, Kohärenzzeile | 1,5–2 Tage | Neueinfrierung (nur das neue Projekt) |
| **E4** Takten | Startzählung, Anfahrverlust, Mindestlaufzeit, Kennzahl Starts | 1 Tag | byte-gleich ohne Pflege |

**Empfehlung:** mit **E1 + E2** beginnen — ein kleiner Eingriff an einer Stelle, die Daten liefert
VDI 3805 schon, und E1 behebt nebenbei die Brennwertkennzeichnung des Imports. E3 danach, weil es den
Rücklauf aus AK1 und dem Speicher zuführen muss und erst mit dem Referenzprojekt belastbar wird; E4
zuletzt (kleiner Energieeffekt, Nutzen vor allem als Kennzahl).

**Nebenbefund N1:** Satz-700-Spalte 26 ist vermutlich brennwertbezogen (ErP ηs); das Projektgerät von
1023 rechnet mit 0,874. Zieht der Import künftig den Nennlastwert aus 710.01 vor, ändern sich neu
importierte Geräte, nicht der Bestand.

### Offene Anwenderfragen

- **F1 Standardwerte:** Soll ein Brennwertkessel ohne gepflegtes η₃₀ Normwerte (DIN V 4701-10,
  DIN EN 15316-4-1) bekommen — dann wandert jeder markierte Bestand, auch 1023 —, oder gilt
  „leer = feste η“ (Empfehlung)?
- **F2 Bestandskatalog:** Wird `Tab_Heizkessel_STAMM` per Neuimport der VDI-Dateien nachgepflegt
  (46 Brennwertgeräte, η₃₀ aus 710.01), oder erhalten nur neu importierte Geräte die Werte? Die
  Auslieferungsvorlage folgt der Entscheidung.
- **F3 Nennlastwert (N1):** Welcher Wert ist künftig η₁₀₀ — Satz 700 Spalte 26 oder 710.01?
- **F4 Referenzprojekt:** Kopie von 1023 allein, oder zusätzlich eine AK1-Variante mit Gaskessel
  (Kopie von 1047 mit getauschtem Kessel)? Gehört das neue Projekt in die CI-Auswahl?
- **F5 Rückfall-Rücklauf** ohne jede Temperaturangabe: 50 °C (Rückfallpaar 70/50, leichter
  Brennwertgewinn) oder 60 °C (konservativ, kein Gewinn)?

# Konzept — Emissionsarten-Katalog und CO₂-Äquivalent (EPOS-Plan)

**Stand:** 28.08.2026 · **Rev. 1.2 — alle Entscheidungsfragen beantwortet**
(Nutzerentscheide 28.08.2026: F3 präzisiert, F4 bestätigt, Luftschadstoffe ohne Vorkette,
Modus global + Projekt-Override, Artenauswahl global, E1 vor E2 — verbleibend offen nur
Umsetzungsbegleitung: Fundstellen für E6, Mapping-Durchsicht bei E2)

Anforderung (28.08.2026): Der Energieträger-Dialog soll seine Emissionsfaktoren aus einem
**pflegbaren Katalog** beziehen (bestehende Faktoren übernehmen, eigene hinzufügen/ändern/löschen),
je Faktor eine **Umrechnung in CO₂-Äquivalent** führen (außer CO₂ selbst), die **Summe der
CO₂-Äquivalente der ausgewählten** Arten anzeigen, die **Feldliste wählbar** machen (CO₂ Pflicht,
SO₂ und NOx Voreinstellung), die CO₂-Berechnung zwischen **CO₂ und CO₂-Äquivalent umschaltbar**
machen (im neuen und im bestehenden Dialog) — und die Drehfeld-Pfeile an den Eingabefeldern
entfernen.

**Verhältnis zu den bestehenden Konzepten:**

- [`Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md`](Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md)
  (Rev. 1, zur Abnahme, **noch nicht umgesetzt** — Messung 28.08.: Erdgas LL, Heizöl EL u. a.
  tragen weiterhin `co2 = 0`) bleibt gültig und läuft als Saat-Migration **vor** diesem Konzept.
- [`Konzept_Emissionsfaktoren_Quellenwahl_EPOS-Plan.md`](Konzept_Emissionsfaktoren_Quellenwahl_EPOS-Plan.md)
  (Rev. 1, zur Abnahme, nicht umgesetzt) bleibt das Modell für **Herkunft und Projektwahl**.
  Dieses Konzept fügt die dritte Dimension hinzu: die **Emissionsart** wird vom festen
  Spaltensatz (`co2`, `so2`, `nox`) zum Katalogobjekt. Wo Rev. 1 der Quellenwahl feste Spalten
  vorsah (`emissionsfaktor.co2/so2/nox/staub`), gilt künftig die generische Form aus § 3 —
  das ist die einzige Änderung an jenem Konzept (dort bei Umsetzung als Rev. 2 nachzuziehen).

---

## 1 Ist-Stand (gemessen 28.08.2026, Produktiv-DB `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`)

**Drei feste Schadstoffspalten, drei Ebenen.** `energy_project_settings.co2/so2/nox` →
`Tab_Brennstoff_Stamm.CO2/SO2/NOx` (+ `Staub`, `PE_Faktor`) → `energy_carrier.co2/so2/nox`.
Gelesen von `KostenEmissionRechner` (nur CO₂) und `EmissionsBilanzRechner.LadeFaktoren`
(alle drei).

**Einheiten-Befund (neu):** Der Rechner führt CO₂ in **g/kWh**, SO₂/NOx in **mg/kWh**
(`EmissionsBilanzRechner.cs:20` „Einheiten: CO₂ g/kWh, SO₂/NOx mg/kWh (Kenndaten-Katalog)";
Umrechnung `MWh × Faktor / 1000 = kg`). Die Bestandswerte bestätigen das (Erdgas E: SO₂ 0,3,
NOx 110; Heizöl S: SO₂ 800 — als g/kWh physikalisch unmöglich, als mg/kWh klassische
Feuerungswerte). Der Dialog `ucFuelSettings` beschriftet aber **alle drei Felder mit
„[g/kWh]"** (`label10`/`label11`). Die Anzeige ist für SO₂/NOx um den Faktor 1000 falsch
beschriftet — ein Anzeigefehler, kein Rechenfehler. Wird mit diesem Konzept behoben (F4).

**Eingabefelder:** `numSO2`, `numCO2`, `numNOx` sind `NumericUpDown` — daher die Pfeile.

**Vorhandene Faktor-Kataloge** (Dialog „Gesetzliche Parameter", Jahreszeilen mit Quelle
und Status, Hausregel „neue Jahreszeile, kein Ändern"):

| Klasse | Inhalt | Charakter |
|---|---|---|
| `EF_BILANZ` | BAFA EEW 3.4 (Holz 27, Klär-/Deponiegas 50, Klärschlamm 10, Pellets 36, Strom 435, Biogas 152, Fernwärme 280 …), EBeV 2030 (Erdgas Hi 200,9 / Ho 181,4, Heizöl EL 266,4, Heizöl S 286,9, Flüssiggas 235,8 …), UBA-Strommix (direkt / THG ohne / mit Vorkette, 2020–2024), Substitution, biogene Verbrennung | reale Bilanz |
| `EF_NACHWEIS` | Träger-Faktoren der GEG-Linie inkl. Fernwärme-Regeln und Verdrängungsstrommix | gesetzlicher Nachweis |

**Fachlich entscheidend:** Die **BAFA-Werte sind bereits CO₂-Äquivalente** (inkl. CH₄/N₂O
und Vorketten, heizwertbezogen — steht so im Merkblatt). Die **EBeV-Werte sind reines CO₂**
(Brennstoff-Emissionshandel). Der UBA-Strommix liegt in beiden Lesarten vor
(`CO2_DIREKT` vs. `THG_MIT_VORKETTE`). Diese Unterscheidung trägt § 2 F3.

---

## 2 Fachliche Festlegungen

### F1 — Die Emissionsart ist ein Katalogobjekt

CO₂, SO₂, NOx sind keine Spaltennamen mehr, sondern Einträge eines Katalogs, der erweiterbar
ist (CH₄, N₂O, Staub, CO, eigene). Jede Art trägt Kürzel, Name, Einheit, CO₂-Äquivalenzfaktor
samt dessen Quelle, und die Flags aus F5.

**CO₂ ist Pflichtart:** immer vorhanden, nicht abwählbar, nicht löschbar, Äquivalenzfaktor
fest 1 (Feld gesperrt — „außer für CO₂").

### F2 — CO₂-Äquivalent heißt GWP₁₀₀, und Nicht-Treibhausgase tragen 0

Die Umrechnung Art → CO₂e ist das Treibhauspotenzial über 100 Jahre. Ausgeliefert werden
belegte Werte (IPCC AR6): **CH₄ fossil 29,8 · CH₄ biogen 27,0 · N₂O 273**. SO₂, NOx, Staub
und CO sind **keine Treibhausgase** — ihr Äquivalenzfaktor wird mit **0** ausgeliefert. Sie
erscheinen dann in der Bilanz als eigene Kennzahl (wie heute), tragen aber nichts zur
CO₂e-Summe bei. Der Faktor bleibt je Art **editierbar** (nur nicht bei CO₂): Wer etwa
indirekte Wirkungen ansetzen will, kann das — sichtbar und mit Quellenangabe, nicht still.

### F3 — Ist der Wert schon ein Äquivalent, wird nicht mehr umgerechnet
*(Nutzerentscheid 28.08.2026)*

Ein CO₂-**Feldwert aus BAFA EEW ist schon CO₂e** (CH₄/N₂O eingerechnet). Regel:

- Jeder Katalogwert trägt das Flag **`ist_co2e`** („Wert ist bereits ein Äquivalent").
  BAFA-Werte: ja. EBeV-Werte: nein. UBA `THG_*`: ja, `CO2_DIREKT`: nein.
- Trägt das CO₂-Feld eines Trägers einen `ist_co2e`-Wert, ist die CO₂e-Summe (F6)
  **genau dieser Wert** — die übrigen ausgewählten Arten werden für die Summe **nicht**
  aufsummiert (CH₄/N₂O stecken schon darin; jede Addition wäre doppelt gezählt).
  SO₂-, NOx- und Staub-Kennzahlen bleiben davon unberührt eigenständig. Die Summenzeile
  sagt es an: „CO₂-Wert ist bereits Äquivalent — Summe = Wert."

### F4 — Einheit je Art, Beschriftung berichtigt, Rechnung unverändert
*(bestätigt per Nutzerentscheid 28.08.2026 — Einheiten und Umrechnung bleiben wie bisher)*

Jede Art führt ihre Anzeigeeinheit: CO₂ (und die Summe CO₂e) **g/kWh**; SO₂, NOx, Staub, CO,
CH₄, N₂O **mg/kWh**. Die Bestandswerte werden **unverändert** übernommen — nur die
Beschriftung wird richtig (Ist-Stand § 1). Intern normiert der Rechner wie bisher auf g/kWh.

### F5 — Auswahl steuert Feldliste und Summe, einmal global

Je Art gibt es **`ausgewaehlt`** (global im Katalog, nicht je Träger): Ausgewählte Arten
erscheinen als Eingabefelder im Emissions-Tab jedes Trägers **und** gehen in die CO₂e-Summe
ein. Auslieferung: CO₂ (Pflicht, nicht abwählbar), SO₂ und NOx vorausgewählt; CH₄, N₂O,
Staub vorhanden, aber abgewählt. Eine Auswahl je Träger wäre eine zweite Wahrheit ohne
erkennbaren Nutzen — bewusst nicht vorgesehen *(bestätigt per Nutzerentscheid
28.08.2026)*. Sonderfälle laufen über die Werte: Eine global ausgewählte Art ohne Wert
(leer/0) trägt beim betreffenden Träger nichts zur Summe bei.

### F6 — Die CO₂e-Summe eines Trägers

```
CO2e [g/kWh] = Σ über ausgewählte Arten:  wert_normiert(g/kWh) × äquivalenzfaktor
```

CO₂ geht mit Faktor 1 ein. **Sonderfall F3:** Trägt das CO₂-Feld einen `ist_co2e`-Wert,
gilt `CO2e = wert(CO₂-Feld)` — ohne Aufsummierung. Mit der Auslieferung (SO₂/NOx-Faktor 0)
ist die Summe zunächst gleich dem CO₂-Wert — sie wird aussagekräftig, sobald CH₄/N₂O
gepflegt oder Faktoren gesetzt werden. Die Summe wird im Emissions-Tab **angezeigt**
(nur Anzeige, kein Speicherfeld — sie ist jederzeit ableitbar).

### F7 — Berechnungsmodus CO₂ oder CO₂-Äquivalent: Vorgabe global, gespeichert je Projekt
*(Nutzerentscheid 28.08.2026)*

Ein Schalter **„CO₂-Berechnung: CO₂ | CO₂-Äquivalent"**, sichtbar an beiden Orten
(Emissions-Tab des Energieträger-Dialogs und Katalog-Dialog). Die **globale Vorgabe**
(Katalogebene) gilt für neue Projekte; jedes Projekt **übernimmt sie beim Anlegen und
speichert sie selbst** (neues Feld in der Projekttabelle). Damit trägt ein Projekt seine
Rechenmethode dauerhaft in sich — es rechnet auch nach Jahren im Modus seiner
Entstehung, gleichgültig wie die Vorgabe inzwischen steht (Hausregel
Reproduzierbarkeit). Der Schalter im Emissions-Tab wirkt im Projektmodus auf das
Projekt, im Katalogmodus — wie der im Katalog-Dialog — auf die Vorgabe.
Bestandsprojekte erhalten bei der Migration den Modus `CO2` (heutiges Verhalten).
Wirkung auf die Emissionsrechnung:

```
Modus CO₂:   t/a = MWh/a × Faktor(Art CO₂) / 1000            (wie bisher)
Modus CO₂e:  t/a = MWh/a × CO2e-Summe (F6) / 1000
```

Betroffen sind `KostenEmissionRechner` (CO2Gesamt, CO2Spezifisch, Netzstrom-Anteil) und der
CO₂-Anteil des `EmissionsBilanzRechner`; SO₂-/NOx-Kennzahlen bleiben eigenständig.
**Unberührt** bleiben die BEHG-Abgabemenge (`CO2Brennstoff` — gesetzlich reines CO₂ nach
EBeV, ein Äquivalent wäre dort falsch) und die Nachweisrechnungen der Klasse `EF_NACHWEIS`.
Jeder Bericht, der die Kennzahl ausweist, nennt den Modus im Beschriftungstext
(„CO₂-Emissionen" vs. „CO₂-Äquivalent (GWP₁₀₀)") — sonst sind zwei Berichte desselben
Projekts nicht vergleichbar. Der Modus wird beim Rechenlauf in die Variantenergebnisse
übernommen, damit ein gespeichertes Ergebnis seinen Entstehungsmodus kennt.

### F8 — Übernehmen heißt kopieren, mit Herkunft

Übernimmt der Anwender einen Katalogwert in einen Träger, wird der **Zahlenwert kopiert**
und die Herkunft (Katalogeintrag) am Trägerwert vermerkt. Eine spätere Änderung des
Katalogs ändert **keinen** Träger rückwirkend — dieselbe Logik, aus der die Jahreszeilen-
Hausregel der Gesetzesparameter kommt. Der Emissions-Tab zeigt die Herkunft an
(„BAFA EEW 3.4, 2026"); wird der Wert von Hand geändert, wechselt sie auf „Eigener Wert".

### F9 — Bestandsschutz

Die Spalten `co2/so2/nox` in `energy_carrier` und `energy_project_settings` bleiben
bestehen und werden als unterste Rückfallebene weiter gelesen. Die Struktur-Etappe (E2)
ändert **kein** Rechenergebnis; erst der vom Anwender betätigte Modus-Schalter (F7) tut es.

---

## 3 Datenmodell

Zwei neue Tabellen, eine erweiterte. Muster: `ReadOnly`-Kennzeichnung wie die
`_STAMM`-Tabellen, Migration idempotent.

### `emissionsart` — der Katalog der Schadstoffe (F1)

| Feld | Typ | Bedeutung |
|---|---|---|
| `id` | Autowert | |
| `kuerzel` | Text | `CO2`, `SO2`, `NOX`, `CH4_FOSSIL`, `CH4_BIOGEN`, `N2O`, `STAUB`, `CO`, eigene |
| `name` | Text | Anzeigename („Methan (fossil)") |
| `einheit` | Text | `g/kWh` oder `mg/kWh` (F4) |
| `co2_aequivalent` | Zahl | GWP₁₀₀; bei CO₂ fest 1 (F2) |
| `aequivalent_quelle` | Text | z. B. „IPCC AR6, GWP100" — leer bei 0 |
| `ist_pflicht` | Ja/Nein | nur CO₂ |
| `ausgewaehlt` | Ja/Nein | Feldliste + Summe (F5) |
| `ist_auslieferung` | Ja/Nein | mitgelieferte Arten sind nicht löschbar, nur abwählbar |
| `sortierung` | Zahl | |

**Auslieferung:** CO₂ (1, Pflicht) · SO₂ (0, ausgewählt) · NOx (0, ausgewählt) ·
CH₄ fossil (29,8) · CH₄ biogen (27,0) · N₂O (273) · Staub (0) — die letzten vier abgewählt.

### `emissionswert` — Katalogwerte und Trägerwerte in einer Tabelle

| Feld | Typ | Bedeutung |
|---|---|---|
| `id` | Autowert | |
| `emissionsart_id` | Zahl | → `emissionsart.id` |
| `carrier_id` | Zahl | → `energy_carrier.id`; **NULL = trägerunabhängige Katalogvorlage** |
| `quelle` | Text | `BAFA_EEW`, `EBEV_2030`, `UBA_STROMMIX`, `STAMM_ALT`, `EIGENER_WERT`, später `GEMIS` |
| `quelle_text` | Text | Anzeigetext mit Stand („BAFA EEW 3.4, 2026") |
| `wert` | Zahl | in der Einheit der Art (F4) |
| `ist_co2e` | Ja/Nein | Wert ist bereits ein Äquivalent (F3) |
| `ist_aktiv` | Ja/Nein | **der** für den Träger geltende Wert (je `carrier_id` + `emissionsart_id` höchstens einer) |
| `herkunft_id` | Zahl | bei kopierten Werten: der Katalogeintrag, aus dem kopiert wurde (F8) |
| `ist_auslieferung` | Ja/Nein | ausgelieferte Katalogzeilen sind nicht löschbar |
| `gueltig_ab` | Datum | Fortschreibung, Muster Jahreszeilen |

Eine Tabelle statt zwei, weil Katalogvorlage und Trägerwert dieselbe Gestalt haben — der
Unterschied ist nur, ob `carrier_id` gefüllt ist. Die Saat der Katalogvorlagen kommt aus
den **vorhandenen** gesetzlichen Parametern (`EF_BILANZ`/`EF_NACHWEIS` — je Schlüssel die
jüngste GESICHERTE Jahreszeile, mit Trägerzuordnung über eine Mapping-Liste im
Migrationsschritt: `EBEV_ERDGAS_HI` → Erdgas E/LL/Stadtgas usw.) und aus
`Tab_Brennstoff_Stamm` (Quelle `STAMM_ALT` — Altliteratur, als unbelegt gekennzeichnet).
Die gesetzlichen Parameter selbst bleiben unangetastet und führend für alles Gesetzliche;
`emissionswert` ist ihre Übernahme in die Trägerpflege, nicht ihr Ersatz.

### Erweiterung Bestand

- `energy_carrier.co2/so2/nox`, `energy_project_settings.co2/so2/nox`: bleiben (F9).
  Migration E2 legt je Träger für CO₂/SO₂/NOx aktive `emissionswert`-Zeilen mit den
  heutigen Zahlen an (Quelle `EIGENER_WERT`, bzw. `STAMM_ALT` wo der Wert erkennbar aus
  dem Stamm kommt). Doppelte Buchführung wird vermieden, indem der Schreibweg des Dialogs
  ab E3 **beide** Orte schreibt (neue Struktur führend, alte Spalten als Spiegel für
  Altleser).
- Globale **Vorgabe** `EMISSION_BERECHNUNGSMODUS` (`CO2` | `CO2E`) in der vorhandenen
  Einstellungs-/Parametertabelle, plus gleichnamiges Feld in der **Projekttabelle**
  (beim Anlegen aus der Vorgabe übernommen; Bestandsprojekte per Migration auf `CO2`) —
  F7.

**Leseweg der Rechner (je Art):** aktive `emissionswert`-Zeile des Trägers → sonst
Altspalten-Kette wie bisher (Projektwert → Stamm → Carrier). Die Projektübersteuerung
bleibt vorerst auf CO₂/SO₂/NOx beschränkt (Altspalten); eine generische
Projektübersteuerung je Art kommt erst mit der Quellenwahl-Umsetzung (deren F4/E3).

---

## 4 Oberfläche

### 4.1 Energieträger-Dialog: Detailbereich bekommt zwei Reiter

Der heutige Detailbereich (eine lange Scrollseite) wird in ein `TabControl` gegliedert:
**„Preise & Umrechnung"** (Bestand unverändert) und **„Emissionen"** (neu — die bisherigen
drei Faktorfelder ziehen dorthin um):

```
┌ Emissionen ────────────────────────────────────────────────────────────┐
│ CO₂-Berechnung:  (•) CO₂   ( ) CO₂-Äquivalent (GWP₁₀₀) [Projekt/Vorgabe] │
│                                                                        │
│  Art            Wert        Einheit   Herkunft                         │
│  CO₂            240,0       g/kWh     STAMM_ALT (unbelegt)   [Katalog…]│
│  SO₂            0,3         mg/kWh    STAMM_ALT (unbelegt)   [Katalog…]│
│  NOx            110,0       mg/kWh    STAMM_ALT (unbelegt)   [Katalog…]│
│                                                                        │
│  CO₂-Äquivalent gesamt (ausgewählte Arten):  240,0 g/kWh               │
│  Hinweis im F3-Fall: „CO₂-Wert ist bereits Äquivalent — Summe = Wert, │
│     weitere Arten werden nicht aufsummiert."                           │
│                                                                        │
│  [Emissionsarten & Katalog verwalten…]                                 │
└────────────────────────────────────────────────────────────────────────┘
```

- Die Feldzeilen entstehen **dynamisch** aus den ausgewählten Arten (F5).
- Eingabe als `TextBox` mit Zahlprüfung — **keine `NumericUpDown` mehr**, die Pfeile
  entfallen (Anforderung 7). Format wie die übrigen Zahlfelder des Dialogs.
- `[Katalog…]` je Zeile öffnet den Katalog-Dialog (4.2) vorgefiltert auf Art + Träger;
  „Übernehmen" dort schreibt den Wert in die Zeile (F8).
- Speichern läuft über den vorhandenen Speichern-Knopf des Dialogs (Katalog- vs.
  Projektmodus wie bisher).

### 4.2 Neuer Dialog „Emissionsfaktor-Katalog"

Ein Dialog, zwei Aufgaben — links die Arten, rechts die Werte der markierten Art:

```
┌ Emissionsfaktor-Katalog ───────────────────────────────────────────────────┐
│ CO₂-Berechnung:  (•) CO₂   ( ) CO₂-Äquivalent (GWP₁₀₀)  [globale Vorgabe]  │
│ ┌ Emissionsarten ──────────────┐ ┌ Werte: NOx — Erdgas E ────────────────┐ │
│ │ ☑ CO₂    g/kWh   (Pflicht)   │ │ Quelle              Wert     CO₂e?    │ │
│ │ ☑ SO₂    mg/kWh  ·GWP 0      │ │ STAMM_ALT           110      nein     │ │
│ │ ☑ NOx    mg/kWh  ·GWP 0      │ │ EIGENER_WERT        95       nein     │ │
│ │ ☐ CH₄ f. mg/kWh  ·GWP 29,8   │ │                                       │ │
│ │ ☐ N₂O    mg/kWh  ·GWP 273    │ │ [Übernehmen]  [Neu] [Bearb.] [Löschen]│ │
│ │ ☐ Staub  mg/kWh  ·GWP 0      │ │                                       │ │
│ │ [Neu] [Bearbeiten] [Löschen] │ │ (Werte ohne Träger = Vorlagen für     │ │
│ └──────────────────────────────┘ │  alle Träger, z. B. Strommix)         │ │
│                                  └───────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────────┘
```

- **Checkbox** = `ausgewaehlt` (F5); CO₂ fest gesetzt und ausgegraut.
- **Arten:** Neu/Bearbeiten (Name, Einheit, GWP + Quelle) für alle; Löschen nur für
  eigene Arten (`ist_auslieferung = falsch`) **und** nur, wenn keine Werte an ihnen
  hängen — sonst Hinweis mit Angebot „abwählen statt löschen".
- **Werte:** die Katalogeinträge der markierten Art für den übergebenen Träger (plus
  trägerunabhängige Vorlagen). Neu/Bearbeiten/Löschen nur für `EIGENER_WERT`-Zeilen;
  Auslieferungszeilen (BAFA/EBeV/UBA/STAMM_ALT) sind unveränderlich — aktualisiert wird
  über neue Jahreszeilen der gesetzlichen Parameter, die die Saat-Logik nachzieht.
- **Übernehmen** kopiert den markierten Wert als aktiven Trägerwert (F8) und schließt
  zurück in den Emissions-Tab.
- Ohne Träger-Kontext geöffnet (aus dem Admin-/Stammdatenmenü) zeigt der Dialog nur die
  Artenverwaltung und die trägerunabhängigen Vorlagen.

---

## 5 Faktoren-Aktualisierung (Auftrag „ggf. mit üblichen Quellen, evtl. GEMIS")

**Geprüft, keine Änderung nötig:** Die Kataloge `EF_BILANZ` und `EF_NACHWEIS` sind auf
Stand (BAFA EEW 3.4, EBeV 2030, UBA CLIMATE CHANGE 16/2026) und GESICHERT. Sie werden
Saat der Katalogvorlagen, nicht geändert.

**Ausstehend, bereits beschlossen:** die CO₂-Saat der Trägerwerte nach
`Konzept_CO2-Faktoren` Rev. 1 (10 Träger von 0 auf BAFA-Werte, 4 Korrekturen). Läuft als
eigener Migrationsschritt **vor** E2, damit die Artenmigration die berichtigten Werte
übernimmt *(Reihenfolge bestätigt per Nutzerentscheid 28.08.2026)*.

**SO₂/NOx/Staub (Luftschadstoffe):** Die Bestandswerte (mg/kWh, Feuerung ohne Vorkette,
Quelle unbekannt → `STAMM_ALT`, „unbelegt") sind in der Größenordnung plausibel, aber
nicht zitierfähig. GEMIS führt Vorketten-Werte, die um Größenordnungen höher liegen
(Beispiel Erdgas-Heizung, GEMIS: SO₂-Äquivalent ≈ 140 mg/kWh inkl. Vorkette gegenüber
0,3 mg/kWh Feuerungswert im Bestand). **Beschlossen (Nutzerentscheid 28.08.2026):**
Luftschadstoffe bleiben Feuerungswerte **ohne** Vorkette (Emissionsschutz-Sicht,
konsistent zum Bestand); als
belegte Quelle wird bei Umsetzung UBA TEXTE 97/2025 („Ermittlung von Emissionsfaktoren",
kleine/mittlere Feuerungsanlagen) bzw. GEMIS 5.x herangezogen und als neue Quelle
(`GEMIS` / `UBA_TEXTE`) eingesät — **erst nach Vorlage der Fundstelle, keine Zahlen aus
dem Gedächtnis** (Hausregel aus dem Quellenwahl-Konzept, § 4).

**CH₄/N₂O-Verbrennungswerte je Träger:** werden **nicht** eingesät (keine belegte
Fundstelle greifbar; zudem F3 — mit BAFA-CO₂e wären sie doppelt). Die Arten sind da,
die Werte trägt ein, wer sie braucht und belegt.

---

## 6 Umsetzung in Etappen

| Etappe | Inhalt | Ergebnis |
|---|---|---|
| **E1** | CO₂-Saat nach `Konzept_CO2-Faktoren` Rev. 1 (eigener Migrationsschritt, Regeln von dort: Sicherung, laccdb-Sperre, ACE-Falle, Idempotenz) | Trägerwerte belegt statt 0 |
| **E2** | Tabellen `emissionsart` + `emissionswert` anlegen und säen (Arten-Auslieferung; Vorlagen aus `EF_BILANZ`/`EF_NACHWEIS` mit Mapping-Liste; Trägerwerte aus Bestandsspalten) | Modell steht, **kein Ergebnis ändert sich** (F9) |
| **E3** | Emissions-Tab im Energieträger-Dialog (dynamische Felder, TextBox statt Spinner, Einheiten richtig, CO₂e-Summe, Herkunft, Warnung F3), Schreibweg beidseitig | Anwender pflegt im neuen Modell |
| **E4** | Katalog-Dialog (4.2): Artenverwaltung, Werteverwaltung, Übernehmen | Katalogpflege vollständig |
| **E5** | Modus-Schalter (F7): globale Vorgabe + Projektfeld, an beiden Orten; `KostenEmissionRechner` + `EmissionsBilanzRechner` modusfähig; Berichte weisen Modus aus; Modus in Variantenergebnisse | CO₂/CO₂e wählbar und wirksam |
| **E6** | Luftschadstoff-Quelle (GEMIS/UBA TEXTE) nach Vorlage der Fundstellen einsäen | Werte zitierfähig |

Prüfstand je Etappe: Smoke der beiden Rechenwege; E2 zusätzlich Vorher/Nachher-Vergleich
aller Emissionskennzahlen über die Referenzläufe (muss identisch sein); E5 gezielter
Vergleich Modus CO₂ vs. CO₂e an einem Handbeispiel.

---

## 7 Abnahmekriterien

1. Der Emissions-Tab zeigt genau die ausgewählten Arten; CO₂ ist nicht abwählbar; die
   Pfeile sind weg; SO₂/NOx sind mit mg/kWh beschriftet und die Zahlen unverändert.
2. Ein Wert lässt sich aus dem Katalog übernehmen; die Herkunft steht am Feld; Handeingabe
   setzt sie auf „Eigener Wert".
3. Eigene Arten und eigene Werte lassen sich anlegen, ändern, löschen; Auslieferung und
   CO₂ nicht.
4. Die CO₂e-Summe entspricht F6 (Handrechnung), reagiert auf Auswahl-Änderung sofort;
   im F3-Fall ist die Summe gleich dem CO₂-Wert und der Tab weist darauf hin.
5. Modus-Umschaltung ändert CO₂-Kennzahlen nachvollziehbar (Handbeispiel), lässt
   BEHG-Abgabe und Nachweisrechnung unverändert, und jeder Bericht nennt den Modus.
   Ein Projekt behält seinen gespeicherten Modus, auch wenn die globale Vorgabe
   wechselt; ein neues Projekt übernimmt die Vorgabe.
6. Nach E2 liefern alle Referenzläufe identische Emissionskennzahlen; Zweitlauf der
   Migrationen ändert nichts.

---

## 8 Offene Punkte

1. ~~Modus-Reichweite~~ — **entschieden 28.08.2026: globale Vorgabe + Projekt-Override**
   (F7).
2. ~~Luftschadstoffe mit oder ohne Vorkette~~ — **entschieden 28.08.2026: ohne** (§ 5).
3. **Fundstellen** für GEMIS/UBA TEXTE 97/2025 zur E6-Saat.
4. ~~Auswahl je Träger statt global~~ — **entschieden 28.08.2026: global** (F5).
5. Mapping-Liste gesetzliche Parameter → Träger (E2) bei Umsetzung zur Durchsicht
   vorlegen (insb. Fernwärme und Strom-Varianten).
6. Übernahme in `Konzept_Emissionsfaktoren_Quellenwahl` als dessen Rev. 2
   (generische Faktor-Zeilen statt fester Spalten), sobald jenes umgesetzt wird.

---

## 9 Verweise

- [`Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md`](Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md) — CO₂-Saat (E1)
- [`Konzept_Emissionsfaktoren_Quellenwahl_EPOS-Plan.md`](Konzept_Emissionsfaktoren_Quellenwahl_EPOS-Plan.md) — Herkunft/Projektwahl, wird Rev. 2
- `Views\Kosten\ucFuelSettings.cs` — heutige Felder `numSO2/numCO2/numNOx`, Schreibwege Katalog/Projekt
- `Allgemein\Bericht\KostenEmissionRechner.cs` — CO₂-Kette, `STROMMIX_CO2_G_JE_KWH`
- `Allgemein\Wirtschaftlichkeit\EmissionsBilanzRechner.cs:20` — Einheiten-Beleg mg/kWh
- `Allgemein\Wirtschaftlichkeit\GesetzKatalog.cs` — Saat `EF_BILANZ`/`EF_NACHWEIS`
- IPCC AR6 GWP₁₀₀ (CH₄ 29,8/27,0 · N₂O 273); BAFA EEW 3.4; EBeV 2030 Anlage 2; UBA CLIMATE CHANGE 16/2026; GEMIS (IINAS); UBA TEXTE 97/2025

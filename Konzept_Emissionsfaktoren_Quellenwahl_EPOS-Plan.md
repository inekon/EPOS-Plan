# Konzept — Emissionsfaktoren mit Quellenwahl (EPOS-Plan)

**Stand:** 22.08.2026 · **Rev. 1 — zur Abnahme**

Ziel: Je Energieträger sollen **mehrere Emissionsfaktoren aus verschiedenen Quellen** hinterlegt
sein (BAFA, UBA, GEG …). Welcher Faktor gilt, entscheidet das **Projekt**.

Dieses Dokument löst [`Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md`](Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md)
nicht ab — jenes wird zum **ersten Datensatz** in diesem Modell (Quelle „BAFA EEW 2025").

---

## 1 Ist-Stand (gemessen 22.08.2026)

**Ein Faktor je Träger, drei Ebenen.** Kette laut `Allgemein\Bericht\KostenEmissionRechner.cs`:

```
energy_project_settings.co2          (Projektwert, frei eingetragen)
  ↓ sonst
Tab_Brennstoff_Stamm.CO2             (über energy_carrier.id_brennstoff)
  ↓ sonst
energy_carrier.co2
```

**Geführte Schadstoffe:** `co2`, `so2`, `nox` in `energy_carrier` **und**
`energy_project_settings`; `Tab_Brennstoff_Stamm` zusätzlich `Staub` und `PE_Faktor`.
`EmissionsBilanzRechner.cs:360` liest alle drei gemeinsam.

**Einheit:** g/kWh (= kg/MWh), verifiziert.

**Die Schwäche:** Es gibt keinen Ort für die Herkunft. Ein Wert von 310 für Heizöl L ist nicht von
einem Wert von 266 zu unterscheiden, außer durch die Zahl selbst — und niemand kann sagen, welche
Quelle recht hat. Wer eine Berechnung verteidigen muss, kann es nicht.

---

## 2 Fachliche Festlegungen

### F1 — Die Quelle ist ein eigenes Objekt, kein Textfeld

Eine Quelle trägt Kürzel, Name, Stand, Bezugsgröße, Fundstelle. Nur so lässt sich im Bericht
belegen, woher eine Zahl stammt, und nur so ist ein Datensatz später als Ganzes aktualisierbar.

### F2 — Ein Faktorsatz je Quelle **und Variante**

Eine Quelle kann für denselben Träger **mehrere** Faktoren führen — das ist kein Sonderfall,
sondern der Regelfall. Beispiel BAFA EEW 2025, Träger Strom:

| Variante | tCO₂/MWh | Anwendungsfall laut Merkblatt |
|---|---|---|
| Effizienzmaßnahme | 0,435 | Bilanzierung von Einsparungen |
| Mehrverbrauch / Wechsel zu Strom | 0,107 | Energieträgerwechsel **hin zu** Strom |
| Wechsel zu erneuerbaren Quellen | 0 | unter den Bedingungen des Merkblatts |

Ebenso Wasserstoff: 0,385 / 0,102 (CO₂-arm) / 0 (erneuerbar).

*Damit ist die offene Frage aus dem Vorgängerkonzept beantwortet:* Nicht „435 **oder** 107", sondern
beide im Katalog, und das Projekt wählt.

### F3 — Die Bezugsgröße gehört zur Quelle, nicht zum Faktor

Das BAFA-Merkblatt hält ausdrücklich fest, dass sich seine Faktoren auf den **Heizwert** beziehen,
und liefert eine eigene Tabelle zur Umrechnung brennwertbezogener Verbräuche. Eine Quelle mit
Brennwertbezug darf deshalb **nicht** einfach eingesetzt werden.

**Regel:** Jede Quelle trägt ihre Bezugsgröße (Hi/Hs). Passt sie nicht zur Rechengrundlage des
Projekts, wird umgerechnet — und wenn die dafür nötige Angabe fehlt, bleibt das Ergebnis leer
statt falsch. Das entspricht der bestehenden Hausregel „keine stillen Teilsummen".

### F4 — Wahl je Projekt: eine Vorgabe, Ausnahmen je Träger

Das Projekt wählt **eine Leitquelle**. Je Träger ist eine abweichende Wahl möglich.

*Begründung:* Eine Wahl je Träger allein wäre bei 21 Trägern unzumutbar; eine Wahl nur je Projekt
verhindert genau die Fälle, um die es geht (Strom nach EEW, Rest nach UBA).

### F5 — Frei eingetragene Werte bleiben, als eigene Quelle

Was heute in `energy_project_settings.co2` steht, ist teils echte Anwendereingabe. Es wird zur
Pseudo-Quelle **„Eigener Wert"** — nichts geht verloren, und der Bericht kann sie als das
ausweisen, was sie ist: nicht belegt.

### F6 — Unvollständige Quellen sind normal

Das BAFA-Merkblatt nennt **nur CO₂**. SO₂, NOx und Staub stehen dort nicht. Der Rückfall gilt
deshalb **je Schadstoff einzeln**: fehlt SO₂ in der gewählten Quelle, greift die bisherige Kette
weiter, und der Bericht weist für diesen Schadstoff die andere Herkunft aus.

### F7 — Der Bericht nennt die Quelle

Jede ausgewiesene Emissionskennzahl trägt ihre Herkunft — Quelle, Stand und gegebenenfalls
Variante. Ohne das ist die Zahl nicht verteidigbar, und der ganze Aufwand wäre vergeblich.

---

## 3 Datenmodell

Zwei neue Tabellen. Keine bestehende Spalte entfällt — die alten Felder bleiben als unterste
Rückfallebene erhalten.

### `emissionsquelle`

| Feld | Typ | Bedeutung |
|---|---|---|
| `id` | Autowert | |
| `kuerzel` | Text | z. B. `BAFA_EEW_2025`, `UBA_2020`, `GEG_2024`, `EIGENER_WERT` |
| `name` | Text | Anzeigename |
| `stand` | Datum/Text | Ausgabejahr bzw. Datenstand |
| `bezug` | Text | `Hi` oder `Hs` (F3) |
| `fundstelle` | Text | URL oder Dokumenttitel mit Tabellennummer |
| `bemerkung` | Memo | Geltungsbereich, Einschränkungen |
| `ist_auslieferung` | Ja/Nein | gehört zur Auslieferung (Muster `ReadOnly` der `_STAMM`-Tabellen) |
| `sortierung` | Zahl | |

### `emissionsfaktor`

| Feld | Typ | Bedeutung |
|---|---|---|
| `id` | Autowert | |
| `quelle_id` | Zahl | → `emissionsquelle.id` |
| `carrier_id` | Zahl | → `energy_carrier.id` |
| `variante` | Text | leer = Regelfall; sonst z. B. `WECHSEL_ZU_STROM` (F2) |
| `variante_text` | Text | Anzeigetext der Variante |
| `co2`, `so2`, `nox`, `staub` | Zahl | g/kWh; **NULL = in dieser Quelle nicht enthalten** (F6) |
| `abgeleitet` | Ja/Nein | kein Originalwert der Quelle, sondern hergeleitet |
| `herleitung` | Text | wenn `abgeleitet`: wie |
| `gueltig_ab` | Datum | für spätere Fortschreibung |

**`NULL` heißt „nicht enthalten", `0` heißt „nachweislich null".** Diese Unterscheidung ist
tragend — der heutige Zustand, in dem zehn Träger `0` tragen, weil niemand einen Wert eingepflegt
hat, ist genau die Verwechslung, die den ganzen Befund ausgelöst hat.

### Projektseite

`energy_project_settings` erhält `emissionsfaktor_id` (Zahl, NULL erlaubt) — die trägerbezogene
Ausnahme aus F4. Die Leitquelle des Projekts kommt in die Projekttabelle als
`emissionsquelle_id`.

**Neue Kette:**

```
energy_project_settings.emissionsfaktor_id      (Ausnahme je Träger)
  ↓ sonst
Leitquelle des Projekts, Regelvariante           (F4)
  ↓ sonst je Schadstoff einzeln (F6)
energy_project_settings.co2/so2/nox              (Quelle „Eigener Wert", F5)
  ↓ sonst
Tab_Brennstoff_Stamm  →  energy_carrier          (Bestandskette, unverändert)
```

---

## 4 Auszuliefernde Quellen

| Kürzel | Inhalt | Stand |
|---|---|---|
| `BAFA_EEW_2025` | CO₂ für 24 Träger inkl. drei Strom- und drei Wasserstoffvarianten | 2025 |
| `UBA_2020` | CO₂, SO₂, NOx, Staub — die Aufstellung, auf die das BAFA selbst verweist | 15.04.2020 |
| `GEG` | Primärenergie- und Emissionsfaktoren nach Gebäudeenergiegesetz | zu klären |
| `BEHG_V` | bereits im BHKW-Dialog sichtbar („Emissionen nach BEHG-V", t CO₂/GJ) | zu klären |
| `EIGENER_WERT` | Pseudo-Quelle für Anwendereingaben (F5) | — |

**Offen:** Von diesen liegt mir nur BAFA EEW 2025 als geprüftes Dokument vor. Für UBA, GEG und
BEHG-V brauche ich die Fundstellen — sonst müsste ich Zahlen erfinden, und das wäre schlimmer als
der jetzige Zustand.

Der BHKW-Dialog zeigt bereits BEHG-V-Faktoren in **t CO₂/GJ** (Heizöl 0,0808, Flüssiggas 0,0663,
Erdgas 0,056). Andere Einheit als g/kWh — Umrechnungsfaktor 3,6; also Heizöl 291, Flüssiggas 239,
Erdgas 202 g/kWh. Diese drei sind belegt und können `BEHG_V` bereits füllen.

---

## 5 Umsetzung in Etappen

| Etappe | Inhalt | Ergebnis |
|---|---|---|
| **E1** | Tabellen anlegen, `BAFA_EEW_2025` und `EIGENER_WERT` befüllen, Bestandswerte als „Eigener Wert" übernehmen | Modell steht, nichts ändert sich am Ergebnis |
| **E2** | Neue Rückfallkette im `KostenEmissionRechner` und `EmissionsBilanzRechner`, je Schadstoff (F6) | Rechnung nutzt das Modell |
| **E3** | Oberfläche: Leitquelle je Projekt, Ausnahme je Träger, Anzeige der Herkunft | Anwender kann wählen |
| **E4** | Bericht weist Quelle, Stand und Variante je Kennzahl aus (F7) | Zahlen belegbar |
| **E5** | Weitere Quellen (UBA, GEG, BEHG-V) nach Vorlage der Fundstellen | Auswahl vollständig |

E1 ist bewusst wirkungsneutral: Erst E2 ändert Ergebnisse. So lässt sich das Modell prüfen, bevor
es rechnet.

---

## 6 Abnahmekriterien

1. Ein Projekt kann eine Leitquelle wählen; die Emissionen ändern sich nachvollziehbar.
2. Für Strom lassen sich die drei BAFA-Varianten auswählen; die Wärmepumpenbilanz ändert sich
   entsprechend.
3. Ein Träger ohne Faktor in der gewählten Quelle fällt je Schadstoff sauber zurück, statt still
   mit null zu rechnen.
4. Der Bericht nennt zu jeder Emissionskennzahl Quelle, Stand und Variante.
5. Bestehende Projekte rechnen nach E1 **unverändert** weiter.
6. Zweitlauf der Migration ändert nichts (Idempotenz).

---

## 7 Offene Punkte

1. **Fundstellen für UBA, GEG und BEHG-V** (Abschnitt 4) — ohne sie keine Datensätze.
2. **„GModG"** aus der Anforderung: Ist das GEG gemeint, oder eine andere Regelung?
3. **Leitquelle für neue Projekte** — welche ist die Vorgabe?
4. **Stromvariante als Vorgabe** — Effizienzmaßnahme (435) oder Wechsel zu Strom (107)?
   Mit F2 ist das keine Grundsatzentscheidung mehr, nur noch eine Vorbelegung.
5. Ob **Staub** und **PE_Faktor** aus `Tab_Brennstoff_Stamm` in dasselbe Modell gehören.

---

## 8 Verweise

- [`Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md`](Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md) — der BAFA-Datensatz
- `Allgemein\Bericht\KostenEmissionRechner.cs` — heutige Kette, Einheit
- `Allgemein\Wirtschaftlichkeit\EmissionsBilanzRechner.cs:360` — liest co2/so2/nox gemeinsam
- `Allgemein\Update\SchemaMigration.cs` — Muster Schritt 33/35

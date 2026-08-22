# Konzept — CO₂-Faktoren der Energieträger (EPOS-Plan)

**Stand:** 22.08.2026 · **Rev. 1 — zur Abnahme**

Anlass: **Zehn von 21 aktiven Katalog-Energieträgern tragen `co2 = 0,00`** — darunter Erdgas LL,
Heizöl EL, Koks und Fernwärme. Ein Projekt, das einen davon verwendet und keine projektbezogene
Einstellung überschreibt, rechnet seine Emissionen still mit null. Das ist kein Anzeigefehler,
sondern ein falsches Ergebnis.

---

## 1 Einheit — geprüft, nicht angenommen

Die Datenbankspalte `energy_carrier.co2` führt **g CO₂ je kWh** (gleichbedeutend kg/MWh). Belegt
durch zwei unabhängige Stellen in `Allgemein\Bericht\KostenEmissionRechner.cs`:

```csharp
public const double STROMMIX_CO2_G_JE_KWH = 380.0;     // Name nennt die Einheit
double t = kv.Value * info.CO2.Value / 1000.0;          // MWh × g/kWh / 1000 = t
```

Das BAFA-Merkblatt gibt **tCO₂/MWh** an. Umrechnung also **× 1000**.

**Quelle:** BAFA, „Informationsblatt CO₂-Faktoren – Bundesförderung für Energie- und
Ressourceneffizienz in der Wirtschaft" (2025), Tabelle 2 auf Seite 10. Die dortigen Werte stammen
laut Merkblatt aus der UBA-Aufstellung vom 15.04.2020 (fossile Brennstoffe) und der UBA-Studie
„Emissionsbilanz erneuerbarer Energieträger", November 2019 (biogene Träger); es sind
CO₂-Äquivalente einschließlich Vorketten, bezogen auf den **Heizwert**.

---

## 2 Belegte Werte

### 2.1 Unmittelbar aus BAFA Tabelle 2

| Katalogträger | BAFA-Zeile | tCO₂/MWh | **neu [g/kWh]** | bisher |
|---|---|---|---|---|
| Biogas | Biogas | 0,152 | **152** | 0 |
| Biogas 2 | Biogas | 0,152 | **152** | 0 |
| Biogas Variante | Biogas | 0,152 | **152** | 140 |
| Fernwärme | Nah-/Fernwärme | 0,280 | **280** | 0 |
| Erdgas LL | Erdgas | 0,201 | **201** | 0 |
| Erdgas E | Erdgas | 0,201 | **201** | — |
| Heizöl EL | Heizöl leicht / Diesel | 0,266 | **266** | 0 |
| Heizöl L | Heizöl leicht / Diesel | 0,266 | **266** | 310 |
| Heizöl L Variante | Heizöl leicht / Diesel | 0,266 | **266** | 310 |
| Heizöl L var | Heizöl leicht / Diesel | 0,266 | **266** | — |
| Heizöl S | Heizöl schwer | 0,288 | **288** | 310 |
| Wasserstoff | Wasserstoff | 0,385 | **385** | 0 |

### 2.2 Strom — Entscheidung erforderlich, hier vorläufig getroffen

Das Merkblatt nennt **drei** Stromfaktoren:

| BAFA-Zeile | tCO₂/MWh | Anwendungsfall laut Merkblatt |
|---|---|---|
| El. Strom (Effizienzmaßnahme) | 0,435 | Bilanzierung von Einsparungen an elektrischer Energie; Strominlandsverbrauch 2021 |
| El. Strom (Mehrverbrauch / Energieträgerwechsel zu Strom) | 0,107 | Mehrverbrauch und Wechsel **hin zu** Strom; Modellrechnung für 2028 |
| El. Strom (Wechsel zu Erneuerbaren Quellen) | 0 | nur unter den Bedingungen des gleichnamigen Abschnitts |

**Vorläufige Festlegung: 435 g/kWh** für „Elektrische Energie", „Elektrische Energie 2" und
„Strom Variante".

*Begründung:* Der konservative, allgemein begründbare Netzfaktor. Er liegt in derselben
Größenordnung wie der bisherige Datenbankwert (560) und der fest verdrahtete Vorgabewert
`STROMMIX_CO2_G_JE_KWH = 380`.

**Fachlicher Einwand, der eine Entscheidung braucht:** Eine Wärmepumpe ist genau der Fall
„Energieträgerwechsel zu Strom", für den das BAFA **0,107** vorschreibt. Mit 435 statt 107
erscheint jede Wärmepumpe rechnerisch rund viermal schlechter. Wer förderkonform nach EEW
bilanzieren will, braucht 107 — oder eine anlagenabhängige Wahl (Wärmepumpe und Elektrokessel
mit 107, sonstiger Strombezug mit 435). Letzteres ist fachlich am saubersten, verlagert den Faktor
aber vom Energieträger auf die Anlage und ist deutlich mehr Arbeit.

### 2.3 Nicht im Merkblatt enthalten — abgeleitet, als solches gekennzeichnet

Diese Werte stehen **nicht** im BAFA-Merkblatt. Sie sind aus dessen eigenen Werten hergeleitet
bzw. dem nächstliegenden dort genannten Träger gleichgesetzt. **Jeder davon ist im
Migrationsprotokoll und im Katalog als abgeleitet zu kennzeichnen** — es sind keine belegten
BAFA-Werte.

| Katalogträger | Herleitung | **neu [g/kWh]** |
|---|---|---|
| Heizöl Bio 10 | 90 % Heizöl leicht (0,266) + 10 % Biodiesel (0,070) = 0,2464 | **246** |
| Heizöl Bio 15 | 85 % Heizöl leicht (0,266) + 15 % Biodiesel (0,070) = 0,2366 | **237** |
| Koks | Analogie Steinkohle (0,335) | **335** |
| Stadtgas | Analogie Erdgas (0,201) | **201** |
| Tierische Fette | Analogie Biodiesel (0,070) | **70** |

**Annahme bei den Bio-Mischungen:** „Bio 10" bzw. „Bio 15" bezeichnet den Bioanteil **energetisch**.
Ist der Anteil volumetrisch gemeint, verschieben sich die Werte leicht.

**Schwächster Punkt: Koks.** Koks entsteht aus Steinkohle unter Verlust flüchtiger Bestandteile;
sein Emissionsfaktor je Energieeinheit liegt real **über** dem der Steinkohle. Die Gleichsetzung
mit 335 unterschätzt ihn eher. Wer Koks tatsächlich einsetzt, sollte den Wert aus der
UBA-Aufstellung nachtragen.

### 2.4 Unverändert

| Katalogträger | Grund |
|---|---|
| Test | Testeintrag, kein realer Energieträger |

---

## 3 Nebenwirkung, die mitentschieden werden muss

`KostenEmissionRechner.STROMMIX_CO2_G_JE_KWH = 380.0` ist ein fest verdrahteter Vorgabewert, der
greift, wenn kein Stromträger gepflegt ist. Er sollte demselben Beschluss folgen wie 2.2 —
sonst rechnet dieselbe Anwendung je nach Datenlage mit 380 oder 435.

---

## 4 Umsetzung

Als **Migrationsschritt** in `Allgemein\Update\SchemaMigration.cs`, Muster wie Schritt 33/35:
erst prüfen, nur bei Bedarf schreiben, danach gegenprüfen, Abschlussprüfung nach der
Schrittschleife, idempotent.

**Regeln:**

1. Gesetzt wird **nur**, wo der Katalogwert `0` oder `NULL` ist, **oder** wo er vom hier
   festgelegten Wert abweicht. Jede Änderung mit altem und neuem Wert protokollieren.
2. `energy_project_settings.co2` bleibt **unangetastet** — das sind projektbezogene Übersteuerungen
   und teils echte Anwendereingaben. Der Katalog ist die Rückfallebene, und nur die wird berichtigt.
3. Vor dem Schreiben datierte Sicherung nach `DB-Backup\`; nicht schreiben, solange
   `Kenndaten.laccdb` existiert.
4. **ACE-Falle:** Kein `?`-Parameter in der Unterabfrage eines UPDATE — trifft still 0 Zeilen.
   IDs zuerst parametrisiert lesen, dann UPDATE mit ganzzahliger IN-Liste. Zweitlauf muss
   0 Änderungen ergeben.

**Erwartete Wirkung:** 10 Träger von `0` auf einen belegten Wert, 4 Korrekturen bestehender Werte
(Heizöl L/L Variante 310 → 266, Heizöl S 310 → 288, Biogas Variante 140 → 152), 1 Eintrag
unverändert.

---

## 5 Offene Punkte

1. **Stromfaktor** — 435, 107 oder anlagenabhängig (2.2). Betrifft jedes Ergebnis mit Wärmepumpe.
2. **`STROMMIX_CO2_G_JE_KWH`** nachziehen (Abschnitt 3).
3. **Koks, Stadtgas, Tierische Fette** — Quelle nachtragen statt Analogie (2.3).
4. **Bio-Mischungen** — energetischer oder volumetrischer Bioanteil (2.3).

---

## 6 Verweise

- BAFA, Informationsblatt CO₂-Faktoren EEW 2025, Tabelle 1 (S. 5) und Tabelle 2 (S. 10)
- `Allgemein\Bericht\KostenEmissionRechner.cs` — Einheit, Rückfallkette, Vorgabewert
- `Allgemein\Update\SchemaMigration.cs` — Muster Schritt 33/35

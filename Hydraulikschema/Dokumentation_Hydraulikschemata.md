# Dokumentation der Hydraulikschema-Bibliothek für Wärmeplan

Gesamtübersicht aller Hydraulikschemata für Wärmeerzeugungsanlagen.
Stand: 17.07.2026 · Alle Darstellungen sind Schemadarstellungen, keine Ausführungsplanung.
Quellen: eigene Auslegungspraxis; BHKW-Schemata 5–9 nach ASUE-Broschüre „Einbindung von
kleinen und mittleren Blockheizkraftwerken / KWK-Anlagen — Hydraulik, Elektrik, Regelung".

## Aufbau der Bibliothek

```
Hydraulik/
├── BHKW mit Spitzenlastkessel/     10 Schemata (S1–S10) + Uebersicht_BHKW_Schemata.md
├── WP mit Booster/                  5 Schemata (V1–V5)  + Beschreibung_WP_Booster_Schemata.md
├── WP ohne Booster/                 3 Schemata (V1–V3)  + Beschreibung_WP_ohne_Booster.md
└── Dokumentation_Hydraulikschemata.md   (diese Datei)
```

## Stilkonventionen (alle Schemata)

- SVG 1500 × 950 px, Titel + Untertitel oben, Legende, Fußzeile mit Symbol­erklärung und
  Hinweis „Schemadarstellung, keine Ausführungsplanung".
- **Vorlauf/Warmwasser rot** (#c81e1e), **Rücklauf/Kaltwasser blau** (#1d6fb8),
  **Solarkreis/Sommer-Bypass orange** (#e8862a, Bypass gestrichelt).
- Temperaturangaben als umrandete Etiketten direkt an den Leitungen = typische
  Auslegungs-Richtwerte, projektspezifisch anzupassen.
- Symbole: Pumpe = Kreis mit Dreieck (Dreieck zeigt Förderrichtung), Rückschlagklappe,
  3-Wege-/Umschaltventil mit Stellmotor „M", Absperrarmatur (Doppeldreieck; gefüllt =
  geschlossen), SV = Sicherheitsventil, MAG = Ausdehnungsgefäß, TIC = Temperatur-
  Messung/Regelung, Speicher = Zylinder, Plattenwärmetauscher (Frischwasserstation),
  Kollektorfeld = schraffiertes Paneel.

---

# Teil 1 · BHKW mit Spitzenlastkessel

**Auslegungsgrundsätze:** BHKW deckt die Grundlast (Ziel ≥ 4.000, besser 5.000–7.000
Vollbenutzungsstunden/Jahr; Dimensionierung häufig 10–30 % der Heizlast ≈ 50–70 % der
Jahreswärmearbeit). Spitzenlastkessel auf volle Heizlast. Puffer 60–100 l/kW_th
(KWKG-Mindestvolumen beachten), niemals vom Kessel geladen. BHKW-Rücklauf < 70 °C,
Vorlauf möglichst hoch (80–90 °C) für gute Grädigkeit. Brennwertkessel immer **parallel**,
nie in Reihe hinter dem BHKW. Pumpenlösungen: kein Festsetzen; Ventillösungen: weniger
Pumpenstrom, aber Festsetzgefahr.

| # | Schema | Puffer | Kessel-Einbindung | ASUE-Bezug |
|---|---|---|---|---|
| S1 | Parallelschaltung über Puffer | zentral, alle durchströmen | über Puffer | – |
| S2 | Kessel direkt parallel | nur BHKW-seitig | direkt ins Netz | V5/V6 |
| S3 | Reihenschaltung, Kessel als Nachheizstufe | in Reihe | in Reihe (Bypass) | V3 |
| S4 | Hydraulische Weiche | BHKW-seitig | parallel auf Weiche | – |
| S5 | Reihenschaltung ohne Puffer | – | in Reihe | V1 |
| S6 | Parallelschaltung ohne Puffer | – | parallel | V2/2a |
| S7 | Umschaltung über Ventile | Nebenschluss (Ventile) | parallel | V3a/4a |
| S8 | Puffer im Nebenschluss (Pumpen) | Nebenschluss (Pumpen) | parallel | V4 |
| S9 | Praxisbeispiel Bestandsanlage | 2×15 m³ in Rücklaufgruppe | Bestand (2 Kessel) | S. 14–17 |
| S10 | Erweiterung Solarthermie | zentral + Solar-WT unten | parallel | – |

## S1 – Parallelschaltung über Pufferspeicher
BHKW und Kessel laden beide den Puffer, alle Verbraucher entnehmen aus dem Puffer.
Einfachste Regelung, robusteste Hydraulik; dafür läuft der volle Kesselvolumenstrom durch
den Speicher (Verluste, Schichtungszerstörung bei Spitzenlast). Für kleinere Anlagen.

## S2 – BHKW mit Puffer, Kessel direkt eingebunden
BHKW arbeitet ausschließlich auf den Puffer (indirekte Einbindung), der Kessel speist über
Rückschlagklappen direkt in den Netzvorlauf. Saubere Schichtung, maximale BHKW-Laufzeit,
schnelle Kesselreaktion — die Standardlösung für mittlere/große Heizzentralen. Nachteil:
träge BHKW-Wärmeübergabe (alles über den Speicher), Volumenstromabgleich nötig.

## S3 – Reihenschaltung: Kessel als Nachheizstufe
Netzvorlauf wird aus dem Puffer entnommen und im Kessel bei Bedarf nachgeheizt
(3-Wege-Bypass). BHKW-Wärme hat systembedingt Vorrang; ideal bei hohen
Netzvorlauftemperaturen. Nicht für Brennwert-/NT-Kessel; Kessel muss den vollen
Netzvolumenstrom vertragen.

## S4 – Einbindung über hydraulische Weiche
Erzeuger und Verbraucher vollständig entkoppelt; unkritisch bei vielen Kreisen und
Erweiterungen. Achtung: schlechter Abgleich hebt die Rücklauftemperatur an (schadet
BHKW-Auskopplung); Kurzschlussströmung in der Weiche führt zum Takten.

## S5 – Reihenschaltung ohne Pufferspeicher (ASUE V1)
Minimalvariante: BHKW hebt den Rücklauf an, konventioneller Kessel heizt in Reihe nach.
Läuft nur bei gleichzeitigem Strom- und Wärmebedarf. Kessel-Vorlaufsollwert mind. 5 K
unter BHKW-Vorlauf; nicht brennwerttauglich.

## S6 – Parallelschaltung ohne Pufferspeicher (ASUE V2/2a)
BHKW und Brennwert-/NT-Kessel parallel direkt ins Netz; Regelung maximiert den
BHKW-Anteil. Brennwerttauglich, aber ohne Puffer kein zeitlicher Ausgleich von Strom- und
Wärmebedarf. Variante 2a: Motorabsperrventile + Kessel-Bypass statt Einzelpumpen.

## S7 – Umschaltung über Ventile (ASUE V3a/4a)
Umschaltventil hinter dem BHKW: direkt ins Netz **oder** Puffer laden (Strombedarf ohne
Wärmebedarf); Entladung über Motorabsperrventil (verhindert Naturumlauf). Nur eine
zentrale Pumpe → weniger Pumpenstrom; dafür Festsetzgefahr der Ventile, Massenströme der
umgeschalteten Zweige müssen ähnlich groß sein.

## S8 – Puffer im Nebenschluss, Laden/Entladen über Pumpen (ASUE V4)
BHKW speist direkt ins Netz (schnelle Wärmeübergabe); Ladepumpe P4 lädt den Puffer bei
Stromvorrang, Entladepumpe P7 entlädt vor Kesselstart (alternativ Motorabsperrventil).
ASUE-Standardempfehlung für Brennwertanlagen mit Puffer.

## S9 – Praxisbeispiel: Nachrüstung in Bestandsanlage (ASUE S. 14–17)
BHKW 520 kW_th/350 kW_el + 2×15 m³ Speicher werden über einen **Einbauverteiler** (zwei
T-Stücke, Absperrung dazwischen geschlossen) in Reihe in den Gesamtrücklauf einer
Zweikesselanlage (2 × 1.750 kW) eingebunden — minimaler Eingriff in Bestand und Regelung.
Regelung: TIC Sp1 < 70 °C (Speicher leer) → BHKW EIN · TIC Sp8 = 90 °C (voll) → BHKW AUS ·
Kesselfreigabe witterungsgeführt (TIC 1, 70–95 °C) erst nach Speicherentladung · Sommer:
Motorklappe im Sommer-Bypass offen, Kessel gesperrt und nicht durchströmt.

## S10 – Erweiterung Solarthermie
Kollektorfeld lädt über internen Wärmetauscher die untere (kalte) Pufferzone; BHKW lädt
oben, Kessel parallel. Solar reduziert im Sommer die Erzeugerlaufzeiten — Konkurrenz
Solar/BHKW über Regelstrategie lösen (Solarvorrang, BHKW-Sperrfenster). Solarkreis mit
Frostschutz, eigener Sicherheitsgruppe und MAG.

---

# Teil 2 · Wärmepumpe mit Booster-WP (Warmwasser-Konzept)

**Prinzip:** Haupt-WP arbeitet nur auf niedrigem Niveau (35–45 °C) → hohe JAZ. Die
Booster-WP (Wasser/Wasser) nutzt den Heizungspuffer als warme Quelle (30–40 °C) und lädt
den WW-Puffer auf 55–60 °C; die Frischwasserstation erwärmt Trinkwasser hygienisch im
Durchfluss (keine Bevorratung → minimales Legionellenrisiko).

| # | Variante | Datei | Besonderheit |
|---|---|---|---|
| V1 | Standard mit Heizkreisen | `..._mit_Heizkreisen.svg` | FBH (Mischer) + HK-Kreis |
| V2 | Nur Warmwasser-Pfad | `..._nur_Warmwasser.svg` | Prinzipdarstellung WW-Kette |
| V3 | Mit Zirkulation | `..._mit_Zirkulation.svg` | Zirkulation ~50 °C → FriWa |
| V4 | Mit Solarthermie | `..._mit_Solarthermie.svg` | Solar-WT in unterer Pufferzone |
| V5 | Puffer nur für Spitzenlast | `..._Puffer_Spitzenlast.svg` | WP direkt, Puffer im Nebenschluss |

Hinweise: Zirkulation (V3) erhöht den Primär-Rücklauf der FriWa → WW-Puffer und Booster
entsprechend auslegen. Solar (V4) entlastet die Haupt-WP und hebt zugleich die
Booster-Quelle an — gute Synergie; Solarkreis eigensicher ausführen. Bei V5 versorgt die
WP die Heizkreise direkt (keine Speicherverluste im Normalbetrieb); der Puffer wird bei
Schwachlast geladen (P7) und nur bei Spitzenlast/Sperrzeit/Abtauung entladen (P8), die
Booster-Quelle kommt aus dem Heizungsrücklauf (alternativ aus dem Puffer).

---

# Teil 3 · Wärmepumpe ohne Booster-WP

Ohne Booster muss die WP das Warmwasser selbst auf Temperatur bringen (55–60 °C) —
zeitweise (V1), dauerhaft (V2) oder gar nicht, weil nur Heizbetrieb dargestellt ist (V3).

| # | Variante | Datei | Besonderheit |
|---|---|---|---|
| V1 | WW über Umschaltventil | `..._WW_Umschaltventil.svg` | WW-Vorrang, Register-Speicher |
| V2 | Kombispeicher mit FriWa | `..._Kombispeicher_FriWa.svg` | WP dauerhaft ~60 °C |
| V3 | Puffer nur für Spitzenlast | `..._Puffer_Spitzenlast.svg` | WP direkt, Puffer im Nebenschluss |

## V1 – WW über Umschaltventil
3-Wege-Umschaltventil bedient wechselweise Heizungspuffer (35–45 °C) und WW-Speicher mit
Register (Ladung 55–60 °C). Einfach und bewährt; Heizung pausiert bei WW-Ladung, Register
groß dimensionieren, Legionellenprogramm (opt. Heizstab).

## V2 – Kombispeicher mit Frischwasserstation
WP hält den Bereitschaftsteil des Kombispeichers auf ~60 °C; FriWa entnimmt direkt.
Hygienisch ohne Bevorratung, aber dauerhafter Hochtemperaturbetrieb → reduzierte JAZ
(ggf. Hochtemperatur-/Propan-WP); Schichtung entscheidend.

## V3 – Puffer nur für Spitzenlast (Nebenschluss)
WP versorgt die Heizkreise direkt (keine Speicherverluste im Normalbetrieb). Der Puffer
liegt im Nebenschluss: Ladepumpe P3 lädt bei Schwachlast, Entladepumpe P4 speist nur bei
Spitzenlast, EVU-Sperrzeit oder Abtauung zu. WP kann knapper dimensioniert werden;
Mindestvolumenstrom der WP über die Kreise sicherstellen (ggf. Überströmventil).
Hydraulisch analog BHKW-S8; auch mit Booster-Konzept kombinierbar.

---

# Auswahlhilfe über alle Anlagentypen

| Anforderung | Empfehlung |
|---|---|
| BHKW, Standardlösung mit max. Laufzeit | BHKW S2 |
| BHKW, hohe Netzvorlauftemperatur | BHKW S3 |
| BHKW, Brennwertkessel + Puffer | BHKW S8 (Pumpen) / S7 (Ventile) |
| BHKW, ohne Puffer / Minimalinvest | BHKW S5 (konv. Kessel) / S6 (Brennwert) |
| BHKW-Nachrüstung im Bestand | BHKW S9 (Einbauverteiler) |
| BHKW + erneuerbare Ergänzung | BHKW S10 (Solarthermie) |
| WP-Neubau mit zentralem WW, beste JAZ | WP mit Booster V1 (+V3 Zirkulation, +V4 Solar) |
| WP, einfaches EFH/MFH | WP ohne Booster V1 |
| WP, Hygiene ohne Booster | WP ohne Booster V2 (FriWa) |
| WP direkt, Puffer nur für Spitzen/Sperrzeiten | WP ohne Booster V3 / WP mit Booster V5 |

# Typische Temperaturniveaus (Kurzreferenz)

| System | Vorlauf | Rücklauf |
|---|---|---|
| BHKW | 80–90 °C | < 70 °C (zwingend) |
| BHKW-Netz | 70–80 °C | 50–60 °C |
| Spitzenlastkessel (Brennwert) | 70–80 °C | möglichst < 55 °C |
| WP Heizbetrieb | 35–45 °C | 28–30 °C |
| WP WW-/Kombibetrieb | 55–60 °C | 28–35 °C |
| Booster-WP Senke | 60–65 °C | 50–55 °C |
| Solarkreis | 60–90 °C | Puffer unten ~30–40 °C |
| FriWa primär / WW / KW | ~60 / 50–55 / ~10 °C | 25–30 °C |

Alle Angaben sind typische Auslegungs-Richtwerte; die konkrete Auslegung (Volumenströme,
Speichergrößen, Regelparameter) erfolgt projektspezifisch in Wärmeplan.

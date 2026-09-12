# `bslib_database.csv` — die Speicherdatenbank der Auslieferung

**Anwenderentscheid W13‑E‑2‑Q2 vom 07.09.2026** („Empfehlung"): `bslib` wird **mitgeliefert**,
die CEC-Speicherliste **nicht**. Eingelesen wird die Datei über
**Administration → Daten & Import → „Stromspeicher (CEC, bslib)…" → „bslib laden"**; der Knopf
findet sie hier von selbst und fragt erst dann nach einer Datei, wenn sie fehlt.

| | |
|---|---|
| **Quelle** | `bslib` — Battery Storage Library, Fassung **0.7** vom **18.01.2023** |
| **Herausgeber** | Forschungszentrum Jülich, IEK‑3 (Tjarko Tjaden, Kai Rösken, Hauke Hoops); Ursprung **HTW Berlin** |
| **Repositorys** | `github.com/FZJ-IEK3-VSA/bslib` und `github.com/RE-Lab-Projects/bslib` (HEAD `0f4e822`, derselbe Stand) |
| **PyPI** | `bslib-0.7.tar.gz`, 30 287 Byte |
| **DOI** | `10.5281/zenodo.6514527` |
| **Abrufdatum** | 07.09.2026 |
| **Größe** | **2 729 Byte**, 8 Zeilen (Kopfzeile und 7 Datensätze), 64 Spalten |
| **Format** | CSV, Komma als Trennzeichen, Punkt als Dezimalzeichen, UTF‑8 |

## Herkunft und Lizenz

Der Quelltext von `bslib` steht unter **MIT** („Copyright (c) 2023 FZ Jülich - IEK 3, Tjarko
Tjaden, Kai Rösken, Hauke Hoops"). Für die Datendatei sagt das LIESMICH des Pakets ausdrücklich:

> *„All resulting database CSV file are under CC BY 4.0."*

Die Weitergabe ist damit erlaubt, solange **Herkunft und Lizenz danebenstehen** — genau das tut
diese Datei. **Weitergegeben wird sie unverändert**, byte-gleich zu der Fassung aus dem
Python-Paket und byte-gleich zur Importprobe
`Referenzlaeufe/Importproben/stromspeicher_bslib_7.csv`, gegen die der Zerleger geprüft wird.

Die Zahlen selbst stammen aus der **PerMod-Datenbank der HTW Berlin** und der
**Stromspeicher-Inspektion** — also aus einer Prüfstandsmessung nach dem Effizienzleitfaden und
nicht aus einem Datenblatt. Darin liegt ihr Wert: `bslib` ist die **einzige** der vier für
EPOS-Plan geprüften Quellen mit einem gemessenen Round-Trip-Wirkungsgrad **und** einem
Standby-Verbrauch.

## Was EPOS-Plan daraus macht

`BslibImport` liest die Datei und füllt `Tab_Stromspeicher_STAMM`
(`Konzept_Stromspeicherimport_EPOS-Plan.md`, Kapitel 2.3):

| bslib | EPOS-Plan | Umrechnung |
|---|---|---|
| `Manufacturer (PE)` + `Model (PE)` (+ `Manufacturer/Model (BAT)`) | `Bezeichner` | `Hersteller: Modell`; bei einem DC-System ist das Gerät das **Paar** aus Leistungselektronik und Batterie |
| `E_BAT_usable [kWh]` | `Energie` | 1:1 — es ist bereits die **nutzbare** Kapazität |
| `P_BAT2AC_out [W]` | `Leistung` | ÷ 1000 |
| `eta_BAT` [%] | `Wirkungsgrad_RT` | ÷ 100 (die Engine setzt je Richtung √η_RT) |
| `P_SYS_SOC0/1_AC/DC [W]` | `Standby_Verbrauch` | **max(voll, leer)**, je AC + DC — der ungünstigere Betriebszustand (Entscheid Q4) |
| — | `Typ` | bleibt **leer**: `bslib` führt keine Zellchemie, weder in der CSV noch in der PerMod-Vorlage |

**Von den sieben Zeilen sind vier Speicher.** Zwei sind reine PV-Wechselrichter
(`Type [-coupled] = PVINV`) und gehören in den Wechselrichterkatalog; eine dritte führt keine
Kapazität. Der Import übergeht sie und **nennt sie** in seiner Meldung, statt sie stillschweigend
zu verlieren.

| Kennung | Hersteller / Modell | Kopplung | kWh | kW | η_RT | Standby |
|---|---|---|---|---|---|---|
| SG1 | Generic / AC-System | AC | 1,00 | 1,000 | 0,9500 | 0,0 W |
| S2 | Siemens / Junelight Smart Battery 9,9 | AC | 8,85 | 3,507 | 0,9687 | 15,0 W |
| S3 | KOSTAL PLENTICORE plus 5.5 / BYD Battery-Box H6.4 | DC | 5,68 | 3,157 | 0,9482 | 9,03 W |
| S4 | KOSTAL PLENTICORE plus 10 / BYD Battery-Box H11.5 | DC | 10,51 | 5,776 | 0,9528 | 9,18 W |

**Kosten liefert die Datei nicht** — `Modulkosten`, `Leistungskosten`, `Investition_Fix` und
`Verschleisskosten` bleiben leer (Entscheid Q3) und sind vor der Wirtschaftlichkeitsrechnung in
der Verwaltung „Stromspeicher" zu ergänzen. Die Maske sagt es in ihrer Herleitungszeile.

**Nachweis:** `EPOS.Kern.Tests/StromspeicherImportTests` liest die Probe feldgenau,
`EPOS.Kern.Tests/StromspeicherUebernahmeTests` schreibt sie gegen die Testdatenbank.

## Warum die CEC-Speicherliste NICHT danebenliegt

Die **CEC Energy Storage System List** ist mit 6 654 Geräten die eigentliche Geräteliste — sie
wird aber **nicht mitgeliefert**. Die Nutzungsbedingungen der California Energy Commission
(abgerufen am 07.09.2026, `https://www.energy.ca.gov/conditions-of-use`) sagen wörtlich:

> „*… **Use or modification of these materials or information for commercial or profit-making
> purposes is prohibited** …*"

Anders als bei den Modul- und Wechselrichterlisten, die EPOS-Plan aus dem NREL-SAM-Bestand unter
**BSD‑3‑Clause** bezieht, gibt es für die Speicherliste **keinen** solchen Umweg: Das
SAM-Bibliotheksverzeichnis führt keine Speicherliste. Der Anwender holt sie deshalb selbst — mit
dem Knopf **„CEC-Liste abrufen"** derselben Maske (`CecSpeicherDienst`, ein GET ohne Anmeldung,
30‑Tage-Zwischenspeicher) oder von Hand über
`solarequipment.energy.ca.gov` und dann **„CEC-Datei laden"**.

## Wird die Datei ausgeliefert?

**Ja.** `Setup/EPOS-Plan.iss` liefert den ganzen Ordner `VDI-3805-Daten` als Komponente
**„Herstellerdaten (VDI 3805, CEC)"** aus — `Flags: recursesubdirs createallsubdirs`, also reist
dieser Unterordner ohne eine Änderung am Setup mit. Ziel ist `{app}\VDI-3805-Daten\Stromspeicher`,
und der Herstellerdatenpfad zeigt ohne Zutun dorthin
(`EinstellungenCtrl.HerstellerdatenpfadOderVorgabe` über `Dienste.Pfade.Herstellerdaten`).
Ein in den Einstellungen eingetragener `VDI3805Path` hat weiterhin Vorrang.

# Vergleichsprotokoll `2026-08-14_B0` → `2026-08-14_Paket7`

Erzeugt mit `Referenzlauf.exe vergleich`, Neuaufnahme nach der Review-Nacharbeit. Exit-Code
1 (= mindestens ein FAIL) ist hier **erwartet**: Paket 7 stellt
`Waermepumpe.Kapazitaet_Pufferspeicher` von `Volumen · 1,16` auf
`SimulationPufferspeicher.Q_max` um (Konzept 6.6) und ergänzt die Einträge der neuen
Pufferspeicher-Persistenz. Bewertung und vollständige Abweichungsliste im
Paket-7-Protokoll.

Verglichen werden die **acht** Projekte, die es in beiden Ständen gibt. Projekt **1021**
ist mit der Nacharbeit neu hinzugekommen (Quellspeicher-Pfad) und in `2026-08-14_B0` nicht
enthalten; es steht deshalb nicht in dieser Gegenüberstellung, sondern nur in
`lauf_protokoll.md`.

**42 Abweichungen, ausnahmslos in `aggregate.csv`, keine einzige in einem Vektor.** Die
Nacharbeit an Quellspeichern, Erdreich-Basis, Frostbedingung und Wirksamkeitsregel
(Fixes 5–8) ändert an diesen acht Projekten **nichts** — keines von ihnen hat einen
Quellspeicher oder eine Erdreichquelle. Die Liste ist ziffernweise identisch zu der vor
der Nacharbeit aufgenommenen.

```
Referenz : C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-14_B0
Vergleich: C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-14_Paket7   (ohne Projekt_1021)
Toleranz : relativ 0.0001 ab Betrag 1, sonst absolut 0.01

Projekt_1007: FAIL (29 Dateien, 324210 Werte, 2 Abweichungen)
    aggregate.csv [Sim.Speicher_Anzahl]: Eintrag nur im Vergleichslauf (neu=0)
    aggregate.csv [Waermepumpe.Kapazitaet_Pufferspeicher]: ref=11.6 neu=0 (10000x Toleranz)
Projekt_1008: FAIL (21 Dateien, 227847 Werte, 16 Abweichungen)
    aggregate.csv [Puffer.SOC_Mittel]: Eintrag nur im Vergleichslauf (neu=3.72670322)
    aggregate.csv [Puffer.SOC_Max]: Eintrag nur im Vergleichslauf (neu=6.87249994)
    aggregate.csv [Puffer.Vollzyklen]: Eintrag nur im Vergleichslauf (neu=3198.32355)
    aggregate.csv [Sim.Speicher_Anzahl]: Eintrag nur im Vergleichslauf (neu=1)
    aggregate.csv [Pufferspeicher[0].ID_Pufferspeicher]: Eintrag nur im Vergleichslauf (neu=1008007)
    aggregate.csv [Pufferspeicher[0].Bezeichner]: Eintrag nur im Vergleichslauf (neu=Vitocell 140-E 600 Liter)
    aggregate.csv [Pufferspeicher[0].Verwendung]: Eintrag nur im Vergleichslauf (neu=Heizung)
    aggregate.csv [Pufferspeicher[0].Q_max]: Eintrag nur im Vergleichslauf (neu=6.96)
    aggregate.csv [Pufferspeicher[0].Ladung_gesamt]: Eintrag nur im Vergleichslauf (neu=22260.33)
    aggregate.csv [Pufferspeicher[0].Entladung_gesamt]: Eintrag nur im Vergleichslauf (neu=21837.93)
    ... und 6 weitere.
Projekt_1010: FAIL (18 Dateien, 201540 Werte, 2 Abweichungen)
    aggregate.csv [Sim.Speicher_Anzahl]: Eintrag nur im Vergleichslauf (neu=0)
    aggregate.csv [Waermepumpe.Kapazitaet_Pufferspeicher]: ref=11.6 neu=0 (10000x Toleranz)
Projekt_1011: FAIL (29 Dateien, 324232 Werte, 2 Abweichungen)
    aggregate.csv [Sim.Speicher_Anzahl]: Eintrag nur im Vergleichslauf (neu=0)
    aggregate.csv [Waermepumpe.Kapazitaet_Pufferspeicher]: ref=11.6 neu=0 (10000x Toleranz)
Projekt_1017: FAIL (20 Dateien, 245378 Werte, 1 Abweichungen)
    aggregate.csv [Sim.Speicher_Anzahl]: Eintrag nur im Vergleichslauf (neu=0)
Projekt_1018: FAIL (19 Dateien, 210343 Werte, 1 Abweichungen)
    aggregate.csv [Sim.Speicher_Anzahl]: Eintrag nur im Vergleichslauf (neu=0)
Projekt_1023: FAIL (25 Dateien, 262917 Werte, 16 Abweichungen)
    aggregate.csv [Puffer.SOC_Mittel]: Eintrag nur im Vergleichslauf (neu=4.46145948)
    aggregate.csv [Puffer.SOC_Max]: Eintrag nur im Vergleichslauf (neu=13.8325005)
    aggregate.csv [Puffer.Vollzyklen]: Eintrag nur im Vergleichslauf (neu=951.475821)
    aggregate.csv [Sim.Speicher_Anzahl]: Eintrag nur im Vergleichslauf (neu=1)
    aggregate.csv [Pufferspeicher[0].ID_Pufferspeicher]: Eintrag nur im Vergleichslauf (neu=1018023)
    aggregate.csv [Pufferspeicher[0].Bezeichner]: Eintrag nur im Vergleichslauf (neu=Vitocell 140-E 600 Ltr)
    aggregate.csv [Pufferspeicher[0].Verwendung]: Eintrag nur im Vergleichslauf (neu=Heizung)
    aggregate.csv [Pufferspeicher[0].Q_max]: Eintrag nur im Vergleichslauf (neu=13.92)
    aggregate.csv [Pufferspeicher[0].Ladung_gesamt]: Eintrag nur im Vergleichslauf (neu=13244.54)
    aggregate.csv [Pufferspeicher[0].Entladung_gesamt]: Eintrag nur im Vergleichslauf (neu=12997.32)
    ... und 6 weitere.
Projekt_1024: FAIL (22 Dateien, 236616 Werte, 2 Abweichungen)
    aggregate.csv [Sim.Speicher_Anzahl]: Eintrag nur im Vergleichslauf (neu=0)
    aggregate.csv [Waermepumpe.Kapazitaet_Pufferspeicher]: ref=11.6 neu=0 (10000x Toleranz)

GESAMT: FAIL
```

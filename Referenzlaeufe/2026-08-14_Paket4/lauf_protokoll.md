# Referenzlauf-Protokoll — Basis Paket 4

**Zeitpunkt:** 14.08.2026 (Abnahme der Paket-4-Review-Nacharbeit)

**Quelle:** `C:\Waermeplan\Paket4_Nacharbeit\DB_Basis\Kenndaten.accdb` — eigene Kopie
**außerhalb des Repos**, entstanden aus der vollständig migrierten Kopie der Etappe 4a
(`Referenzlauf.exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb …`, Schemastand 6).
Die produktive `Kenndaten.accdb` wurde in dieser Etappe **nur gelesen**.

**Modus:** `projekt` je Projekt (kein `lauf`, damit die Arbeitskopie im Repo unberührt
bleibt). Binärstand: `WP-Plan.sln` Debug/x86 mit der uncommitteten Paket-4-Fassung
einschließlich der Review-Nacharbeit vom 14.08.2026.

**Feature-Flag `Kaskade_Zweikanalig`: AUS.** Die Basis bildet bewusst weiter den
**einkanaligen Altpfad** ab — er bleibt die Rückfallebene, bis die Bestandsprojekte
projektweise umgestellt werden. Ein Lauf mit gesetztem Flag ist **kein** Regressionsfall
gegen diese Basis, sondern wird gegen den Flag-aus-Lauf desselben Codes verglichen
(Umsetzungsprotokoll Paket 4, Teil E).

**Projektliste:** 1007, 1008, 1010, 1011, 1017, 1018, 1021, 1023, 1024 — unverändert
gegenüber `2026-08-14_Paket7`.

## Warum die Basis gewechselt wurde

Gegen `2026-08-14_Paket7` bleiben **genau drei** Abweichungen, alle in Projekt 1021 und
alle dokumentiert. Sie sind mit dieser Basis eingefroren:

| Größe | Paket 7 | Paket 4 | Grund |
|---|---|---|---|
| `Pufferspeicher[0].ID_Pufferspeicher` | 8 | 1018014 | ID-Semantik des Quellspeichers: `WaermequelleClass.Quellspeicher()` löst über `WQ_ID_Puffer` auf die **Projektkopie** auf statt auf die Katalogzeile. Alle Rechengrößen des Speichers sind gleich (Bezeichner, Volumen 778 l, Verluste 2,4 kWh/24 h, `Q_max` 4,5124 kWh) — nachgeprüft an der Datenbank |
| `WaermepumpeModul[0].Betriebsstunden` | 6692,41 | 4,41 | **B0-13**: Die Laufzeit wurde im Volllast-Zweig auch dann gezählt, wenn `result[PTHERM] = 0` war (Sperrzeit, begrenzte Quelle, Alternativbetrieb). Der Teillast-Zweig hatte den Guard längst |
| `Waermepumpe.Vollbenutzungsstunden` | 3846,66 | 502,66 | Folgegröße derselben Laufzeit |

Alle übrigen **2.260.920** verglichenen Werte sind byte-genau gleich (Ganglinien im
8760- und 35040-Raster, Skalare, Speicherkennzahlen, Modulnamen).

`Referenzlauf.exe pruefen` meldet für alle neun Projekte „plausibel"; der einzige Hinweis
ist der bekannte (`1007: solar_produktion.csv` Jahressumme 0 — Gewerk aktiviert, kein
Modul zugeordnet).

## Projekte

| ID | CSV-Dateien | Status | Anmerkung |
|---|---|---|---|
| 1007 | 29 | OK | WP + Kessel + Solar + PV, Projekt-Puffer nur aus Alt-Zuordnung |
| 1008 | 21 | OK | Senkenspeicher „Vitocell 140-E 600 Liter", zwei WP-Module |
| 1010 | 18 | OK | Minimalfall „nur Wärmepumpe" |
| 1011 | 29 | OK | 3 WP + 2 Kessel + Solar + PV |
| 1017 | 20 | OK | BHKW + Kessel |
| 1018 | 19 | OK | 2 BHKW + Kessel |
| 1021 | 21 | OK | **Quellspeicher** „allSTOR exclusiv VPS 800/3-7", 3 × `quellspeicher_10361_*.csv` |
| 1023 | 25 | OK | Senkenspeicher „Vitocell 140-E 600 Ltr", zwei WP-Module + Kessel |
| 1024 | 22 | OK | 2 WP + Kessel + BHKW |

## Reproduktion

```powershell
$exe = "C:\Waermeplan\WP_Plan\Referenzlauf\bin\x86\Debug\net8.0-windows\Referenzlauf.exe"

foreach ($id in 1007,1008,1010,1011,1017,1018,1021,1023,1024) {
    & $exe projekt $id "C:\Waermeplan\Paket4_Nacharbeit\Lauf_Aus\Projekt_$id" `
                       C:\Waermeplan\Paket4_Nacharbeit\DB_Basis
}
& $exe vergleich C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-14_Paket4 `
                 C:\Waermeplan\Paket4_Nacharbeit\Lauf_Aus     # -> alles PASS
& $exe pruefen   C:\Waermeplan\Paket4_Nacharbeit\Lauf_Aus
```

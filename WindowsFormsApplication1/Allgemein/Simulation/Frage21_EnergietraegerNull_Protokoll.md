# Frage 21 — Energieträger-Zuordnung NULL-tolerant (Projekt 1011)

Stand: 17.08.2026. Umsetzung und Verifikation ohne Commit (Sync macht Philipp per GitHub_Sync.bat).

## Befund

`Do_Simulation_Intern` brach mit einer unbehandelten `InvalidCastException`
(„Unable to cast object of type 'System.DBNull' to type 'System.Int32'") ab, sobald eine
Kessel- oder BHKW-Anlage des Projekts `ID_Carrier = NULL` trug. Ursache waren die direkten
Casts beim Aufbau der Berichts-Zuordnung Bezeichner → Energieträger:

```csharp
simulation_bhkw.bhkw_carrier.TryAdd((string)rs.Read("Bezeichner"), (int)rs.Read("ID_Carrier"));
simulation_spk.spk_carrier.TryAdd((string)rs.Read("Bezeichner"), (int)rs.Read("ID_Carrier"));
```

Auch der `Bezeichner`-Cast wirft bei NULL. Die Ausnahme wurde nirgends gefangen
(`SimulationRunner.Simuliere` / `btn_Simulation_Click`): .NET-Absturzdialog, kein Protokoll,
kein Ergebnis — der Lauf starb, bevor irgendetwas entstand.

## Datenbefund (Kopie der Produktiv-DB, nur gelesen)

NULL in `ID_Carrier` ist kein Einzelfall, sondern regulärer Bestand:

| Projekt | Anlage (Tab_Energieanlagen) | Typ | Befund |
|---|---|---|---|
| 1011 „test1" | **alle 14 Anlagen** | — | `ID_Carrier = NULL` durchgängig |
| 1011 | 11218, 11219 „GC7000F 22 23 - MX25" | 10 (Kessel) | die beiden absturzauslösenden Zeilen |
| 1017 | 10259 „eloBLOCK VE 28" | 10 (Kessel) | ebenfalls NULL |
| 1017 | 10260 „BHKW EW K 10 S [K] Heizol" | 11 (BHKW) | ebenfalls NULL |

Damit wäre vor dem Fix auch Projekt 1017 — Teil der Referenzmenge — abgestürzt; die
Referenzlauf-Suite war auf diesem Datenstand insgesamt blockiert. `Bezeichner = NULL` kommt
im Bestand nicht vor (dieser Zweig ist nur durch Codelektüre abgesichert).

## Änderung

`SimulationControl.cs`: Die beiden Leseschleifen sind durch einen gemeinsamen, NULL-toleranten
Helfer ersetzt — `EnergietraegerZuordnungLesen(idType, gewerk, ziel)` (bei :574, Aufrufe :426/:427),
DBNull-Prüfung nach dem Hausmuster `WErzeugerCtrl.Belegt`:

- `Bezeichner` NULL → **Warnung** im `SimulationProtokoll` (Anlage mit Tabellen-ID benannt), Zeile übersprungen.
- `ID_Carrier` NULL → **Warnung** („Der …-Anlage „X" ist kein Energieträger zugeordnet (ID_Carrier leer) …"), Zeile übersprungen.
- Beides belegt → unverändert `TryAdd(bezeichner, Convert.ToInt32(carrier))`.

Eine übersprungene Anlage steht im Bericht mit `CarrierId 0` — exakt die bestehende
`TryGetValue`-Vorbelegung im `SimulationRunner` (:512–515 BHKW, :596–599 Kessel) für Anlagen
ohne Treffer; es entsteht also kein neuer Sonderpfad. Warnung statt Hinweis, weil der Lauf mit
einer Ersatzannahme rechnet (Konzept 13.4: kein Ergebnis, das vollständig aussieht — Brennstoff,
Kosten und Emissionen der Anlage fehlen im Bericht). Bei gepflegten Daten ist das Verhalten
bitidentisch zum alten Code.

## Verifikation

- **Build:** `WP-Plan.sln`, Full-MSBuild VS 2022, Debug|x86, `-p:ArtifactsPath=%TEMP%\wpb`
  (mit vorgeschaltetem `-t:Restore`, nötig wegen umgeleitetem `obj\`): **0 Fehler**,
  6 bekannte Bestandswarnungen. Encoding der Datei geprüft: UTF-8-BOM `EF BB BF` und reines
  CRLF erhalten; `git diff` zeigt genau zwei Hunks (54+/15−).
- **Headless-Läufe** (Referenzlauf-Werkzeug, Modus `migration` + `projekt`, gegen migrierte
  Kopie unter `%TEMP%\wpk1011\DB`, Produktiv-DB nur gelesen — SHA256 vor/nach identisch):
  - **Projekt 1011: Exit 0**, 29 CSV inkl. `aggregate.csv`, Ergebnis-Kopf-ID 169. Im Protokoll
    zweimal `Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „GC7000F 22 23 - MX25" …`
    (eine je Datensatz — korrekt, es sind zwei Zeilen).
  - **Projekt 1017: Exit 0**, 20 CSV, Ergebnis-Kopf-ID 170; je eine Warnung für BHKW- und
    Kessel-Anlage → **beide Zweige des Fixes real durchlaufen**.
- Laufordner/Logs: `%TEMP%\wpk1011\Lauf\Projekt_1011`, `…\Projekt_1017`, `…\lauf1011.log`, `…\lauf1017.log`.

## Nebenbefunde (außerhalb des Auftrags, offen)

1. **`Referenzlauf/Ergebnisexport.cs` baut nicht mehr** (Bestandsschaden seit dem
   Stromspeicher-Umbau): `SimulationPV.Speicherfuellstand` (:144) und
   `SimulationControl.simulation_ssp` (:149/:152) existieren nicht mehr. Der Lauf oben nutzte
   eine minimal reparierte Werkzeug-Kopie unter `%TEMP%\wpk1011\ReferenzlaufTool`
   (Zuordnung: `Speicherfuellstand_stuendlich` / `Speicherergebnis != null` /
   `Speicherfuellstand_viertelstuendlich`; baut mit 0 Fehlern). **Erledigt:** Der Fix ist mit
   Commit `e596296` (Sync 17.08.2026, 18:17) im Repo — gleiche Spaltenquellen, aber
   `pv_speicherfuellstand.csv` zieht in den Stromspeicher-Block: Die SoC-Datei entsteht damit
   auch bei Speicher ohne PV (1017: 21 statt 20 CSV; bei der nächsten Basissetzung als
   gewollte Zusatzdatei zu bewerten). Nachweis 18.08.2026: Build 0 Fehler, Projekt 1011
   wertidentisch zur validierten Werkzeug-Kopie (Ganglinien byte-gleich), 1017 Exit 0 mit
   plausibler SoC-Ganglinie.
2. **`migration` endet mit Exit 1** wegen zweier Datenstand-Nachweise (kein Migrationsfehler):
   „PufferHeizung ohne WS_ID_Puffer: 2", „Anlagen ohne Ladeprio-Vorgabe: 1". Kopie und Läufe
   funktionieren; Datenpflege gelegentlich prüfen.
3. Weitere ungeschützte `rs.Read`-Casts in den Lade-Methoden (`BHKW_Liste_Laden`,
   `SPK_Liste_Laden`: `Bezeichner`, `Grenzleistung`, …) sind unverändert — bei NULL in diesen
   Spalten bräche der Lauf dort. Im Bestand aktuell nicht belegt; bei Gelegenheit nach
   demselben Muster härten.

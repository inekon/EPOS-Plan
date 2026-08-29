# Basis 2026-08-29_Booster — Laufprotokoll

**Anlass: Die Booster-Temperaturkopplung ist erstmals im Referenznetz scharf** —
Datenänderung an Anlage 14818 (Projekt 1042): `WQ_Unbegrenzt` von True auf **False**.
Das Häkchen „Quelle unbegrenzt verfügbar" hatte die komplette B1/B2-Kopplung still
abgeschaltet (Booster rechnete mit konstant 45 °C; Befund und Absicherung in
[B3_QuelleUnbegrenzt_Protokoll.md](../../WindowsFormsApplication1/Allgemein/Simulation/B3_QuelleUnbegrenzt_Protokoll.md)).
Gesetzt wurde der Wert am 29.08.2026 00:28 per einzelnem UPDATE **mit ausdrücklicher
Freigabe des Anwenders** (drei UI-Versuche waren an der Bedienabfolge gescheitert — der
Dialog lädt das Häkchen bei jedem Öffnen frisch; der Codepfad selbst ist per Harness
als gesund belegt). Sicherung davor:
`DB-Backup\Kenndaten_2026-08-29_0025_vor_Unbegrenzt-Fix.accdb`.

## Eckdaten

- **Codestand:** `0787aec` (Branch `Pufferspeicher`, Paket B3; rechnerisch identisch mit
  `4be1862`/B2 — per A/B 332/332 belegt), reguläres MSBuild x64 Debug.
- **Datenquelle:** produktive `Kenndaten.accdb`, Zeitstempel **29.08.2026 00:29:39**
  (letzte Schreibung = der Unbegrenzt-Fix), 151 949 312 Bytes, danach **nur gelesen**.
- **Arbeitskopie:** `C:\Waermeplan\_boost\DB`, per `migration`-Modus auf Schemastand
  **55** (No-op-Anteil, Quelle war schon 55); bekannter Schema-Nachweis-Befund der zwei
  1027-Altlastzeilen (außerhalb der Referenzmenge).
- **Lauf:** Weg B, 13 Projekte im `projekt`-Modus (feste Liste), 2 × 13 Läufe, alle
  Exit-Code 0, **332 CSV**.
- **Selbstvergleich:** **332/332 byte-/MD5-gleich** — reproduzierbar.

## Zuordnung gegen `2026-08-28_B2`

**319/332 byte-gleich; alle 13 abweichenden Dateien in `Projekt_1042`** — die zwölf
übrigen Projekte sind unberührt. Die Abweichung ist vollständig der **gewollten**
Datenänderung (Unbegrenzt-Fix → Kopplung scharf) zugeordnet.

## Was die Basis erstmals absichert

Das 1042-Laufprotokoll trägt erstmals in einer Basis:

- „Booster: Die Anlage 14818 bezieht ihre Quellwärme aus Puffer 1054198 (Puffer
  3000Ltr (2)), einem GETEILTEN Puffer. Die Quelltemperatur folgt dem Speicherzustand …"
- **„Booster-Lesepunkt: DAVOR** — die Quelltemperatur wird einmal am Stundenanfang
  gelesen, aus dem Speicherzustand am Ende der Vorstunde …" (der neue Default aus
  Paket B2, Nutzerentscheid).
- Der frühere Kennlinien-Hinweis „8760 Stunden über der oberen Stützstelle" der
  CS7800iLW 16 ist **verschwunden** — der Booster rechnet mit dem echten
  Temperaturband des Speichers.

**Bezifferung der Kopplung (1042, gegen die 45-°C-Fiktion der Vorbasis):**

| Größe | B2-Basis (konstant 45 °C) | Booster-Basis (gekoppelt, DAVOR) |
|---|---|---|
| Booster CS7800iLW 16: Wärme | 6,17 MWh | 6,14 MWh |
| Booster: Strom | 1,34 MWh | **2,01 MWh** |
| Booster: JAZ (Wärme/Strom) | 4,60 | **3,05** |
| Booster: Betriebsstunden | 294 | 407 |
| Haupt-WP CS6800iAW: Wärme | 47,97 MWh | 51,98 MWh (lädt zusätzlich den Quellpuffer) |
| WP-System: Strom | 19,6 MWh | 21,64 MWh |

Die konstant-45-°C-Annahme hatte den Booster systematisch geschönt; die gekoppelte
Rechnung ist die fachlich richtige.

## Was diese Basis nicht absichert

- Lesepunkt „Danach" (nur der Default „Davor" ist eingefroren; die
  Danach-Gegenprobe steht im B2-Protokoll).
- Kessel-Quellkopplung mit Wert ≠ 0 (unverändert; Wirkproben im B2-Protokoll).
- Wirtschaftlichkeit (grundsätzlich, siehe LIESMICH).
- Bekannte 1042-Datenpflege-Warnungen unverändert: Temperaturpaare der drei Speicher
  ungepflegt (Rückfall ΔT = 10 K), Booster-Senke Stora B als „PufferHeizung"
  deklariert bei Klassen-Set Brauchwasser, Altlast-`WS_*`-Reste an 14785/14817.

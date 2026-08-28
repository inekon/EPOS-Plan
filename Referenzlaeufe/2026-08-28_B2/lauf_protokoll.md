# Basis 2026-08-28_B2 — Laufprotokoll

**Anlass: Paket B2 (Kessel-Temperaturmodus, wählbarer Booster-Lesepunkt) + Datenänderung
des Anwenders an Projekt 1042.** Die Vorgängerbasis `2026-08-28_E2` war schon **vor** B2
überholt: Der Anwender hat 1042 am 28.08. mittags umverschaltet (Anlagen 14806/14807 →
14817/14818; Kontrolllauf mit unverändertem Code gegen E2: 321/332, alle 11 Abweichungen
in 1042 — Nachweis im
[B2-Protokoll](../../WindowsFormsApplication1/Allgemein/Simulation/B2_KesselTemperaturmodus_Protokoll.md)).
B2 selbst ist auf gemeinsamer Kopie **332/332 byte-gleich** zum Vorgängercode belegt —
der neue Lesepunkt-Default „Davor" wirkt ausschließlich über die Projekteinstellung.

## Eckdaten

- **Codestand:** `4be1862` (Branch `Pufferspeicher`, Paket B2), reguläres MSBuild x64 Debug,
  0 Fehler. Werkzeug `Referenzlauf` frisch gebaut (Quelldateien seit Sync `2562e2b`
  versioniert).
- **Datenquelle:** produktive `Kenndaten.accdb`, Zeitstempel **28.08.2026 17:19:27**
  (App-Schluss), 151 949 312 Bytes, **nur gelesen** — Kopie byte-identisch (Länge und
  Zeitstempel), Quelle nach dem Lauf unverändert.
- **Arbeitskopie:** `C:\Waermeplan\_b2basis\DB`, per `migration`-Modus auf Schemastand
  **55** migriert (Schritt 55: `WQ_TemperaturModus` = „Berechnet" für den Bestand,
  `Booster_Lesepunkt` = „Davor" für alle Einstellungssätze — die Basis rechnet also mit
  den neuen Vorbelegungen).
- **Lauf:** Weg B, 13 Projekte im `projekt`-Modus (feste Liste 1007, 1008, 1011, 1017,
  1018, 1021, 1023, 1024, 1030, 1039, 1040, 1041, 1042), alle Exit-Code 0, **332 CSV**.
- **Selbstvergleich:** zweiter Durchgang auf derselben Kopie **332/332 byte-/MD5-gleich** —
  die Basis ist reproduzierbar.
- **`pruefen`:** GESAMT plausibel (bekannte Hinweise: PV/Solar aktiviert ohne Modul).

## Zuordnung gegen `2026-08-28_E2`

**321/332 byte-gleich; alle 11 abweichenden Dateien in `Projekt_1042`** — dieselben elf
wie im Kontrolllauf des unveränderten Codes. Die Abweichung ist damit vollständig der
**Datenänderung des Anwenders** zugeordnet; kein Wert eines anderen Projekts hat sich
durch B2 bewegt. Toleranzvergleich: 12 × PASS, 1042 FAIL (98 075 Abweichungen — gewollt,
neuer Datenstand).

## Der Booster-Status dieser Basis — WICHTIG

**Die Booster-Temperaturkopplung ist in dieser Basis NICHT scharf.** Die neue
1042-Verschaltung des Anwenders ist fast vollständig (Booster 14818: `WQ_Typ =
Pufferspeicher`, `WQ_ID_Puffer = 1054198` „Puffer 3000Ltr (2)", geladen von WP 14817
über deren Rang-3-Senke; Booster-Senke Rang 1 → Stora B 1054197) — aber an der Anlage
steht noch **`WQ_Unbegrenzt = True` mit `WQ_Temp = 45`**. Nach Bestandssemantik
(`WaermequelleClass.Quellspeicher`, „unbegrenzt verfügbar → nur die Temperatur wirkt,
keine Bilanz") schaltet das Häkchen den Quellspeicher-Pfad ab: Der Booster rechnet mit
**konstant 45 °C** (sichtbar am Kennlinien-Hinweis „8760 Stunden über der oberen
Stützstelle 25 °C"), es gibt keine Kopplung, keinen Lesepunkt-Eintrag und keine
`QUELLTEMP`-Serie. Die `wp_quellentemperatur.csv` des Projekts gehört zur
Außenluft-WP 14817 (Band −18,2 … +33,5 °C).

**Folge:** Kein Projekt der Referenzmenge sichert die B1/B2-Booster-Rechnung ab
(Fortschreibung B1-O8/B2-O2). Sobald der Anwender das Häkchen „Quelle unbegrenzt
verfügbar" an 14818 entfernt, wird die Kopplung samt Davor-Lesepunkt scharf — dann ist
diese Basis für 1042 überholt und wird erneuert.

Weitere bekannte Warnungen des 1042-Laufs (Datenpflege, unverändert gegenüber E2):
Temperaturpaare der Puffer 1054196/1054197/1054198 ungepflegt (Rückfall ΔT = 10 K),
T_Nutz_BW 55 °C über wirksamem Vorlauf 10 °C (geklemmt), Altlast-`WS_*`-Reste an
14785/14817, Booster-Senke Stora B als „PufferHeizung" deklariert, Klassen-Set aber
Brauchwasser (Kanal Heizung fehlt — was so geladen wird, entlädt der Speicher nie).

## Was diese Basis nicht absichert

- **Booster-Temperaturkopplung und Lesepunkt Davor/Danach** (siehe oben) — die
  A/B-Bezifferung liegt im B2-Protokoll (rekonstruiertes 1042: „Davor" +0,029 % Strom,
  JAZ 2,7500 → 2,7490; „Danach" byte-gleich zum B1-Code).
- **Kessel-Quellkopplung mit Wert ≠ 0** — unverändert kein Kessel der Referenzmenge an
  einem Quellpuffer (Lücke seit B4 dokumentiert); die B2-Bezugskette ist über die
  Wirkproben im B2-Protokoll belegt (1023: Berechnet 12,21 MWh / Fest 65/45 15,47 MWh /
  Senkenspeicher-Bezug 3,26 MWh).
- Wirtschaftlichkeit (grundsätzlich, siehe LIESMICH).

# Referenzlauf-Protokoll — Basis Paket 7

**Zeitpunkt:** 14.08.2026 (Neuaufnahme nach der Review-Nacharbeit)

**Quelle (produktiv, nur gelesen):** `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`

**Gerechnet auf:** `C:\Waermeplan\Paket7_Nach\DB_Basis\Kenndaten.accdb` — eigene Kopie
**außerhalb des Repos**, vollständig migriert (Schema-Nachweis **0 Abweichungen**,
`Tab_ErgebnisPufferspeicher` 13/13 Spalten, Index `idx_ErgPuffer`, `FK_ErgPuffer` mit
`DELETE = CASCADE`). Angelegt mit
`Referenzlauf.exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb C:\Waermeplan\Paket7_Nach\DB_Basis`.

**Modus:** `projekt` je Projekt. Der Modus `lauf` legt `Referenzlaeufe\Arbeitskopie`
im Repo neu an; für diese Nacharbeit war eine Kopie außerhalb des Repos vorgegeben.
`lauf` migriert die Arbeitskopie inzwischen selbst (Schritt 2b) — die Reproduktion in
`../LIESMICH.md` beschreibt beide Wege.

**Projektliste:** 1007, 1008, 1010, 1011, 1017, 1018, **1021**, 1023, 1024 — die acht
Projekte aus `2026-08-14_B0` plus **1021**. 1021 deckt als einziges Projekt den
Quellspeicher-Pfad ab (`WQ_Typ = 'Pufferspeicher'`): Serienschlüssel `QUELLE_<AnlagenID>`,
`quellspeicher_*.csv` und eine Ergebniszeile mit `Verwendung = 'Quelle'`. Die Auswahl
entsteht deterministisch aus der neuen Pflichtkategorie „Wärmepumpe mit Quellspeicher"
(`Referenzlauf.exe liste <dbOrdner>`); die bisherigen acht Wahlen bleiben unverändert.

## Projekte

| ID | CSV-Dateien | Status | Anmerkung |
|---|---|---|---|
| 1007 | 29 | OK | |
| 1008 | 21 | OK | Senkenspeicher „Vitocell 140-E 600 Liter" |
| 1010 | 18 | OK | |
| 1011 | 29 | OK | |
| 1017 | 20 | OK | |
| 1018 | 19 | OK | |
| 1021 | 21 | OK | **Quellspeicher** „allSTOR exclusiv VPS 800/3-7", 3 × `quellspeicher_10361_*.csv` |
| 1023 | 25 | OK | Senkenspeicher „Vitocell 140-E 600 Ltr" |
| 1024 | 22 | OK | |

Die Dateizahl der acht Altprojekte ist unverändert gegenüber `2026-08-14_B0`: Keines von
ihnen hat einen Quellspeicher, deshalb entstehen dort keine `quellspeicher_*.csv`. Neu sind
bei ihnen ausschließlich Einträge **innerhalb** von `aggregate.csv`.

## Quellspeicher-Nachweis (Projekt 1021)

```
Pufferspeicher[0].Bezeichner      allSTOR exclusiv VPS 800/3-7
Pufferspeicher[0].Verwendung      Quelle
Pufferspeicher[0].Q_max           4,51 kWh
Pufferspeicher[0].Ladung_gesamt   0
Pufferspeicher[0].Entladung_gesamt 4,26 kWh
Pufferspeicher[0].SOC_Max         3,72 kWh
Pufferspeicher[0].Vollzyklen      0,94
```

`Vollzyklen` ist rollenabhängig: Ein Quellspeicher startet **voll** und wird entzogen,
Bezugsgröße ist deshalb `Entladung_gesamt / Q_max` = 4,26 / 4,51 = 0,94. Über
`Ladung_gesamt` gerechnet — wie bis zur Nacharbeit — käme hier **0** heraus, obwohl der
Speicher das ganze Jahr gearbeitet hat.

## Automatisch beantwortete Dialoge

Wie in `2026-08-14_B0`: In 1007, 1008, 1011, 1023 und 1024 meldet die WP-Simulation
„Temperatur unterschreitet Kennlinien Untergrenze, soll extrapoliert werden?"; der
Dialogwächter antwortet „Ja". 1021 läuft ohne Rückfrage.

## Plausibilität

`Referenzlauf.exe pruefen` → **GESAMT: plausibel** (9/9 Projekte, Hinweis nur bei
`Projekt_1007/solar_produktion.csv`: Gewerk aktiviert, kein Modul zugeordnet — Bestand).

## Selbstvergleich

Zweiter Lauf derselben neun Projekte auf derselben Datenbank, `vergleich` gegen diesen
Ordner: **GESAMT: PASS (2 260 923 Werte innerhalb der Toleranz)**.

## Verhältnis zur Vorgängerbasis

`2026-08-14_B0` bleibt als historischer Stand liegen. Der Unterschied für die acht
gemeinsamen Projekte ist vollständig in `vergleich_protokoll.md` und in
`../../WindowsFormsApplication1/Allgemein/Simulation/Paket7_Ergebnis_Anzeigen_Protokoll.md`
aufgelistet: eine geänderte Größe (`Waermepumpe.Kapazitaet_Pufferspeicher`) und die neuen
Einträge der Pufferspeicher-Persistenz. Projekt 1021 ist neu und in B0 nicht enthalten.

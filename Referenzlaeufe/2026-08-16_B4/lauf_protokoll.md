# Referenzlauf-Protokoll — Basis B4

**Zeitpunkt:** 16.08.2026, 03:43 Uhr · **Werkzeugprotokoll des Laufs:**
[`lauf_protokoll_werkzeug.md`](lauf_protokoll_werkzeug.md)

**Anlass — genau einer:** Etappe **D4** (KonfigUI) hat die **Ergebnisspalte
`Tab_ErgebnisHeizkessel.Quellwaerme`** eingeführt (Migrationsschritt 10, rein additives DDL,
Schema-Zielstand steigt von 9 auf **10**). Der Export liest `SELECT * FROM Tab_Ergebnis*` —
damit führt `aggregate.csv` je Projekt mit Heizkessel-Ergebniszeile **einen Schlüssel mehr**.
Gegen die eingefrorene Basis B3 meldet der Vergleich das als „Eintrag nur im Vergleichslauf";
seit D4 gibt es dafür die Option `--ohne <schluessel>`.

**B4 friert den D4-Stand einschließlich der neuen Spalte ein** — künftige Vergleiche laufen
damit wieder **ohne Ausschluss**.

**Codestand:** `3fd2787` („KonfigUI D4: Schema-Ansicht, Auswahl-Synchronisation,
Kessel-Quellwaerme als Ergebnisspalte"), unverändert. Gebaut in einem eigenen Export des
Commits außerhalb des Repos (`C:\Waermeplan\_b4`, `git archive`), VS-MSBuild x86/Debug über
`Referenzlauf\Referenzlauf.csproj` (ProjectReference auf die App → Exe und DLL konsistent).
Der Haupt-Checkout und dessen `bin\` wurden **nicht** angefasst. Build: **0 Fehler,
6 Bestandswarnungen** (CS0108 ×2, CS0109 ×2, CS1998, CS4014).

**Feature-Flag `Kaskade_Zweikanalig`:** **AUS** für alle Projekte — wie bei B2 und B3.

**Quelle (produktiv, nur gelesen):** `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`,
Zeitstempel **15.08.2026 23:22**, Schemastand **9**. Keine `Kenndaten.laccdb` vorhanden, die
Anwendung hatte die Datenbank also nicht geöffnet. Gerechnet wurde auf der migrierten
Arbeitskopie, die der Lauf selbst zieht — sie lag unter `C:\Waermeplan\_b4\`, nicht im Repo.
**Migrationsschritt 10 lief auf der Kopie** (`Schemastand vorher: 9 → nachher: 10`,
„Tab_ErgebnisHeizkessel: 1 Spalten angelegt"). Nachkontrolle nach dem Lauf: Die produktive
Datei steht unverändert auf **SchemaVersion 9** und führt in `Tab_ErgebnisHeizkessel`
**keine** Spalte `Quellwaerme`; Zeitstempel und Größe unverändert.

## Projektmenge: unverändert acht

| ID | Projekt | CSV | Werte | Status |
|---|---|---|---|---|
| 1007 | Laurentiuskirche | 29 | 324 210 | OK |
| 1008 | Heinestr 15 | 21 | 227 847 | OK |
| 1011 | test1 | 29 | 324 232 | OK |
| 1017 | WP_PV-Speicher | 20 | 245 379 | OK |
| 1018 | BHKW Test München | 19 | 210 344 | OK |
| 1021 | TestSpeichernUnter | 21 | 227 840 | OK |
| 1023 | Wöhler - Test1 | 25 | 262 918 | OK |
| 1024 | Wöhler - Test2 | 26 | 271 681 | OK |
| **gesamt** | | **190** | **2 094 451** | **8/8 OK** |

Gesamtdauer 00:00:44, Timeout je Projekt 300 s. Die drei Warnungen des Laufs
(`Speicher-Registry: Puffer … hat KEIN Temperaturpaar`, Projekte 1008 und 1011) sind
Bestandshinweise zur Datenpflege und standen wortgleich in B2 und B3.

Plausibilitätsprüfung (`pruefen`): **GESAMT: plausibel**, ein Hinweis wie gehabt
(`Projekt_1007/solar_produktion.csv`: Gewerk aktiviert, kein Modul zugeordnet).

## B3 gegen B4 — welche Abweichung woher kommt

**Vier zusätzliche Werte, sonst nichts.** Byte-Vergleich der 190 CSV-Dateien:

```
186 byte-gleich, 4 abweichend  —  ausschließlich die aggregate.csv der Projekte
                                  1017, 1018, 1023 und 1024
```

Der Zeilendiff dieser vier Dateien besteht aus **genau einer eingefügten Zeile** je Datei:

```
> Heizkessel.Quellwaerme;0
```

| Projekt | Abweichung zu B3 | Ursache |
|---|---|---|
| 1007 | **keine** (byte-/MD5-gleich, 29 Dateien) | kein Heizkessel-Ergebnisdatensatz |
| 1008 | **keine** (byte-/MD5-gleich, 21 Dateien) | kein Heizkessel-Ergebnisdatensatz |
| 1011 | **keine** (byte-/MD5-gleich, 29 Dateien) | kein Heizkessel-Ergebnisdatensatz |
| 1017 | **ein Schlüssel neu** in `aggregate.csv` | `Heizkessel.Quellwaerme` (Schritt 10 / D4) |
| 1018 | **ein Schlüssel neu** in `aggregate.csv` | `Heizkessel.Quellwaerme` (Schritt 10 / D4) |
| 1021 | **keine** (byte-/MD5-gleich, 21 Dateien) | kein Heizkessel-Ergebnisdatensatz |
| 1023 | **ein Schlüssel neu** in `aggregate.csv` | `Heizkessel.Quellwaerme` (Schritt 10 / D4) |
| 1024 | **ein Schlüssel neu** in `aggregate.csv` | `Heizkessel.Quellwaerme` (Schritt 10 / D4) |

Betroffen sind genau die vier Projekte, die das Werkzeug **Heizkessel** aktiviert haben und
deshalb eine Zeile in `Tab_ErgebnisHeizkessel` schreiben. Die Ganglinien sind in **allen acht**
Projekten byte-gleich — die neue Spalte ist ein Skalar und ändert an der Rechnung nichts.

> **Auffällig, aber erwartet:** Der Wert steht in allen vier Projekten auf **0**. Er summiert
> die Wärme, die ein Spitzenkessel aus **seinem Quellpuffer** bezogen hat
> (`SimulationSPK.Quellwaerme_gesamt`); in der Referenzmenge hat kein Kessel einen
> Quellpuffer. Die neue Spalte ist damit **belegt, aber noch nicht mit einem von null
> verschiedenen Wert abgedeckt** — vergleichbar der Lage bei `Erdreich[i].*` seit Paket 7.
> Wer den Pfad regressionsfest haben will, braucht ein Referenzprojekt mit Kessel **an**
> einem Quellpuffer.

**Kein einziger Altwert weicht ab.** Nachweis mit dem dafür geschaffenen Werkzeug:

```
vergleich 2026-08-15_B3  2026-08-16_B4  --ohne Heizkessel.Quellwaerme
AUSGENOMMEN (--ohne): Heizkessel.Quellwaerme

Projekt_1007: PASS (29 Dateien, 324210 Werte)
Projekt_1008: PASS (21 Dateien, 227847 Werte)
Projekt_1011: PASS (29 Dateien, 324232 Werte)
Projekt_1017: PASS (20 Dateien, 245379 Werte)
Projekt_1018: PASS (19 Dateien, 210344 Werte)
Projekt_1021: PASS (21 Dateien, 227840 Werte)
Projekt_1023: PASS (25 Dateien, 262918 Werte)
Projekt_1024: PASS (26 Dateien, 271681 Werte)

GESAMT: PASS (2094451 Werte innerhalb der Toleranz)      Exit-Code 0
```

**8/8 PASS.** Die Differenz zu B3 ist damit vollständig auf den neuen Schlüssel zurückgeführt;
D4 hat **keinen** Rechenweg verändert.

## Nachweise

**Selbstvergleich (Reproduzierbarkeit/Determinismus).** Zweiter `lauf` desselben Codes auf
derselben Quelle, ohne Ausschluss:

```
vergleich 2026-08-16_B4  <lauf2>
GESAMT: PASS (2094451 Werte innerhalb der Toleranz)      Exit-Code 0
Byte-/MD5-Vergleich: 190 von 190 Dateien gleich, 0 abweichend
```

**8/8 PASS, 190/190 byte-gleich** — die Basis ist reproduzierbar.

**Frühere Basis:** `../2026-08-15_B3/` bleibt unangetastet liegen (Codestand `a0a623a` + K-3,
Schemastand 9, ohne die Spalte `Heizkessel.Quellwaerme`).

## Was hier liegt

Nur CSV-Dateien und die beiden Protokolle — **keine `.accdb`**. Die Arbeitskopie lag außerhalb
des Repos und ist gelöscht; eine Datenbankkopie im Basisordner wäre über `.gitignore`
ausgeschlossen und machte die Basis unvollständig übertragbar. Umfang: 190 CSV, 28 MB.

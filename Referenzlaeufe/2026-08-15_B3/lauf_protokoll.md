# Referenzlauf-Protokoll — Basis B3

**Zeitpunkt:** 15.08.2026, 23:04 Uhr · **Werkzeugprotokoll des Laufs:**
[`lauf_protokoll_werkzeug.md`](lauf_protokoll_werkzeug.md)

**Zwei Anlässe, sauber getrennt:**

1. **Ergebnisänderung K-3** — die Bivalenz-Umschaltung des bivalent-alternativen
   Wärmepumpenbetriebs schaltet an der Bivalenztemperatur statt stundenweise nach
   Leistungsunterdeckung, in beiden Rechenwegen. Umsetzung und Verifikation:
   [`../../WindowsFormsApplication1/Allgemein/Simulation/K3_BivalenzTemperatur_Protokoll.md`](../../WindowsFormsApplication1/Allgemein/Simulation/K3_BivalenzTemperatur_Protokoll.md)
2. **Projektlöschung durch den Anwender** — die Referenzmenge schrumpft von neun auf
   **acht** Projekte.

**Codestand:** `a0a623a` + K-3-Änderung in
`WindowsFormsApplication1/Allgemein/Simulation/SimulationWaermepumpe.cs` (gebaut in einem
eigenen git-Arbeitsbaum, Haupt-Checkout unangetastet).

**Feature-Flag `Kaskade_Zweikanalig`:** **AUS** für alle Projekte — wie bei B2.

**Quelle (produktiv, nur gelesen):** `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`,
Zeitstempel **15.08.2026 22:50**. Keine `Kenndaten.laccdb` vorhanden; die produktive Datei
wurde ausschließlich gelesen. Gerechnet wurde wie immer auf einer migrierten Arbeitskopie,
die der Lauf selbst zieht — sie lag im Arbeitsbaum, nicht im Haupt-Checkout.

## Projektmenge: acht statt neun

Der Anwender hat am 15.08.2026 gegen 22:50 Uhr die Projekte **1010, 1016, 1020 und 1025**
aus der produktiven Datenbank gelöscht. Von den neun Referenzprojekten der Basis B2 ist
davon **1010 „Kurs EE"** betroffen; es existiert nicht mehr und kann nicht gerechnet werden.

> **Folgebedarf:** 1010 war in der Referenzmenge die Kategorie **„Wärmepumpe ohne weitere
> Erzeuger"** (`Anlagen: WP`). Fällt sie dauerhaft weg, sollte ein Ersatzprojekt derselben
> Kategorie nachrücken — `Projektauswahl.MAX_PROJEKTE` steht auf 9.

| ID | Projekt | CSV | Werte | Status |
|---|---|---|---|---|
| 1007 | Laurentiuskirche | 29 | 324 210 | OK |
| 1008 | Heinestr 15 | 21 | 227 847 | OK |
| 1011 | test1 | 29 | 324 232 | OK |
| 1017 | WP_PV-Speicher | 20 | 245 378 | OK |
| 1018 | BHKW Test München | 19 | 210 343 | OK |
| 1021 | TestSpeichernUnter | 21 | 227 840 | OK |
| 1023 | Wöhler - Test1 | 25 | 262 917 | OK |
| 1024 | Wöhler - Test2 | 26 | 271 680 | OK |
| **gesamt** | | **190** | **2 094 447** | **8/8 OK** |
| ~~1010~~ | ~~Kurs EE~~ | — | — | **entfällt — vom Anwender gelöscht** |

Die drei Warnungen des Laufs (`Speicher-Registry: Puffer … hat KEIN Temperaturpaar`, Projekte
1008 und 1011) sind Bestandshinweise zur Datenpflege und standen so auch in B2.

## B2 gegen B3 — welche Abweichung woher kommt

| Projekt | Abweichung zu B2 | Ursache |
|---|---|---|
| 1007 | **keine** (byte-/MD5-gleich, 29 Dateien) | — |
| 1008 | **keine** (byte-/MD5-gleich, 21 Dateien) | — |
| 1011 | **keine** (byte-/MD5-gleich, 29 Dateien) | — |
| 1017 | **keine** (byte-/MD5-gleich, 20 Dateien) | — |
| 1018 | **keine** (byte-/MD5-gleich, 19 Dateien) | — |
| 1021 | **keine** (byte-/MD5-gleich, 21 Dateien) | — |
| 1023 | **keine** (byte-/MD5-gleich, 25 Dateien) | — |
| 1024 | **keine** (byte-/MD5-gleich, 26 Dateien) | — |
| 1010 | **Ordner entfällt** (18 Dateien weniger) | **Projektlöschung durch den Anwender**, kein Codeeffekt |

**Kein einziger Wert weicht durch K-3 ab.** Das war vorhergesagt: Der Datenbefund vor dem
Lauf zeigt, dass im gesamten Bestand **keine** Anlage `Bivalenter_Betrieb = TRUE` **und**
`Betriebsart = "Alternativbetrieb"` führt — der geänderte Zweig ist in keinem gespeicherten
Projekt aktiv. Die einzige `Alternativbetrieb`-Zeile (Anlage 10132 in Projekt 1008) trägt
`Bivalenter_Betrieb = False`; die Bedingung ist eine Und-Verknüpfung.

Damit ist die Trennung eindeutig: **die eine Differenz zu B2 ist die fehlende
Projektmenge, nicht die Rechnung.**

## Nachweise

**Selbstvergleich (Reproduzierbarkeit).** Zweiter `lauf` desselben Codes auf derselben
Quelle: **8/8 PASS (2 094 447 Werte)** und **190/190 Dateien byte-/MD5-gleich**.

**A/B gegen den unveränderten Stand `a0a623a`**, beide Flagstellungen, auf **einer**
gemeinsamen migrierten Datenbankkopie. Diese Kopie wurde um **22:26 Uhr** gezogen — also
**vor** der Projektlöschung — und enthält deshalb noch alle **neun** Projekte. Der A/B-Beleg
ist damit umfassender als die Basis:

```
Flag AUS : 9/9 PASS (2 295 987 Werte), 208/208 byte-/MD5-gleich
Flag AN  : 9/9 PASS (2 295 998 Werte), 208/208 byte-/MD5-gleich
```

Auch Projekt 1010 war zum Zeitpunkt der A/B-Probe also noch abgedeckt und zeigte keine
Abweichung.

**Wirkungsnachweis von K-3** an eigens präparierten Kopien der Projekte **1026** und
**1024**; Zahlen im K-3-Protokoll, Abschnitte 6.1 bis 6.3.

**Frühere Basis:** `../2026-08-15_B2/` bleibt unangetastet liegen — dort steht Projekt 1010
weiter zur Verfügung, falls seine Ganglinien noch einmal gebraucht werden.

# `CEC Inverters.csv` — die Wechselrichterliste der Auslieferung

**Anwenderentscheid W6‑O‑3 vom 06.09.2026** („hole die Wechselrichterdaten für den Import";
Bestätigung: „Liste als Datei und dann über Import (aus Admin-Menü)"). Sie liegt hier neben
`CEC Modules.csv`, aus demselben Verzeichnis derselben Quelle, und wird auf demselben Weg
eingelesen: **Administration → Datenimport → „Wechselrichter (CEC, OND)…" → „CEC‑Datei …"**.

| | |
|---|---|
| **Quelle** | NREL SAM, Bibliotheksverzeichnis `deploy/libraries` |
| **URL** | `https://raw.githubusercontent.com/NREL/SAM/develop/deploy/libraries/CEC%20Inverters.csv` |
| **Abrufdatum** | 06.09.2026 |
| **Größe** | 389 761 Byte |
| **Zeilen** | **2 346** — Kopfzeile, Einheitenzeile (`Units`), Variablennamenzeile (`[0]`) und **2 343 Geräte** |
| **Hersteller** | **152** (der Text vor dem ersten Doppelpunkt des Gerätenamens; die Liste führt keine eigene Spalte dafür) |
| **Format** | Originalformat der SAM-Bibliothek: Komma als Trennzeichen, Punkt als Dezimalzeichen, keine Anführungszeichen |
| **Kopfzeile** | `Name, Vac, Pso, Paco, Pdco, Vdco, C0, C1, C2, C3, Pnt, Vdcmax, Idcmax, Mppt_low, Mppt_high, CEC_Date, CEC_hybrid` |

## Herkunft und Lizenz

Die Daten sind die Zulassungsliste der **California Energy Commission (CEC)**; das
**National Renewable Energy Laboratory (NREL)** pflegt sie als Gerätebibliothek seines
Programms **SAM (System Advisor Model)** und veröffentlicht sie dort unter der
**BSD‑3‑Clause**-Lizenz des SAM-Quellbestands. Die Messwerte selbst sind eine öffentliche
Verwaltungsangabe der CEC. **Weitergegeben wird die Datei unverändert**, so wie sie am
Abrufdatum im Verzeichnis stand — dieselbe Handhabung wie bei `CEC Modules.csv`.

## Was EPOS-Plan daraus macht

`CecWechselrichterDienst.AusDatei` liest die Datei und füllt `Tab_Wechselrichter_STAMM`
(Konzept `Konzept_Wechselrichter_EPOS-Plan.md`, Kapitel 5.1). Zwei Punkte zur Ehrlichkeit des
Imports stehen dort und gelten unverändert:

* **Die Liste führt keine MPPT-Zahl.** `Anzahl_Mppt` und `Straenge_Je_Mppt` bleiben NULL; die
  Auslegungsprüfungen P4/P5 rechnen dann auf EINEM Tracker — dem konservativen Fall — und melden
  es (Ampel gelb).
* **`Paco` ist Wirkleistung, nicht Scheinleistung.** `S_AC_Max` bleibt NULL und fällt in den
  Prüfungen auf `P_AC_Nenn` zurück.

Die sechs Stützstellen der Kennlinie (`Eta05…Eta100`) rechnet der Import aus den
Sandia-Koeffizienten bei `U_dc = U_dco` (Konzept 3.3.3); bei Nennlast gilt dabei
`η100 = Paco/Pdco` exakt.

**Nachweis:** `EPOS.Kern.Tests/CecWechselrichterAuslieferungTests` liest genau diese Datei ein,
nennt die Zeilenzahl und zählt die Plausibilität je Gerät (grün/gelb/rot).

## Wird die Datei ausgeliefert?

**Ja, seit dem 06.09.2026** — Anwenderentscheid **W6‑O‑9** („ja"). `Setup/EPOS-Plan.iss` liefert
den ganzen Ordner `VDI-3805-Daten` (rund 186 MB) als eigene Komponente
**„Herstellerdaten (VDI 3805, CEC)"** aus: im Assistenten **vorgewählt** und **abwählbar**. Das
Ziel ist `{app}\VDI-3805-Daten`, also neben dem Programm — dieselbe Lage wie die
Vorlagendatenbank unter `{app}\Vorlage`, aus demselben Grund: Der Auslieferungsbestand wird
**nur gelesen**, ein Update ersetzt ihn, die Deinstallation nimmt ihn mit.

`CEC Modules.csv` ist damit gleich behandelt — beide Listen liegen nach der Installation dort,
wo der Dateiwähler des Imports von selbst aufmacht: Der Herstellerdatenpfad zeigt ohne Zutun auf
den installierten Ordner (`EinstellungenCtrl.HerstellerdatenpfadOderVorgabe` über
`Dienste.Pfade.Herstellerdaten`; im Entwicklungsstand bleibt der Repository-Ordner der Rückfall).
Ein in den Einstellungen eingetragener `VDI3805Path` hat weiterhin Vorrang — wer die Datensätze
im Netz liegen hat, ändert nichts an seiner Gewohnheit und kann die Komponente beim Installieren
abwählen.

# BK1b — die Projektspalte `KWKG_Kostenanteil` entfällt, Schemaschritt 91

Etappe BK1b des Wirtschaftlichkeitskonzepts, Anwenderentscheid vom 18.09.2026
„**BK1-4: (a) Entfernen**". Sie schließt den offenen Punkt `BK1-4` aus § 6.3 des
[Konzepts](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
und bringt den Absatz „doppelte Wahrheiten" in § 6.5 zu Ende.

---

## 1 Der Befund

Schemaschritt 90 (Etappe BK1a) hat sechs der sieben KWKG-Projektspalten entfernt und die
siebte stehen lassen: `Tab_ProjektWirtschaftlichkeit.KWKG_Kostenanteil` samt ihrem
Dialogfeld „Anteil Neuherstellungskosten (Vorgabe)" in Gruppe 2 des
BHKW-Wirtschaftlichkeitsdialogs (Anwenderentscheid `BK1-Q1` c, festgehalten als offener
Punkt `BK1-4`).

**Rechnen tat sie da schon nichts mehr.** § 8 Abs. 2/3 KWKG wählt die Kontingentstufe aus
dem Kostenanteil **der einzelnen Anlage**; Schemaschritt 89 hat den Projektwert einmalig
in jede BHKW-Anlagenzeile geschrieben, die dort leer war, und mit Etappe BK1a ist auch der
letzte projektweite Leser weggefallen (`KontingentDesProjekts` → `KontingentDerAnlage`).
Übrig blieb eine gepflegte Angabe ohne Wirkung — sechs Zeilen über derselben Größe **mit**
Wirkung im selben Dialog. Genau das ist der Zustand, den der Anwender mit „(a) Entfernen"
beendet hat.

Gemessen vor der Arbeit: `grep` auf `KwkgKostenanteil` fand außerhalb von Laden, Speichern,
Hülle und Dialogfeld **keinen** Leser.

---

## 2 Was gemacht wurde

### 2.1 Eine Quelle, drei Leser — zweite Liste statt Nachtrag in Schritt 90

`EPOS.Kern/Allgemein/Update/KwkgProjektaltspalten.cs` führt die Spalte als **eigene
Liste**: Konstante `KOSTENANTEIL`, dazu `Spalten91`, `Anweisungen91`, `Offen91()` und
`Vorhanden91()` neben den Sechser-Listen des Schrittes 90. **Schritt 90 bleibt Zeile für
Zeile unverändert** — ein Migrationsschritt wird nie rückwirkend geändert; er ist auf jeder
bereits gewandelten Datei gelaufen und trägt dort seine Nummer.

Aus derselben Quelle bedienen sich wie bisher drei Leser: der Schemaschritt in
`WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs`, das Werkzeug
`Werkzeuge/Testdatenbankschema` und der Nachweis in `EPOS.Kern.Tests`.

`SchemaKatalog.SPALTE_PW_KWKG_KOSTENANTEIL` entfällt; `Schritt28_KwkgTatbestand` und
`KwkAnlagenwahrheit` nehmen den Namen jetzt von `KwkgProjektaltspalten.KOSTENANTEIL` —
dasselbe Muster, das Schritt 90 für Tatbestand und Anlagenart eingeführt hat.

### 2.2 Schemaschritt 91

`SchemaStand.Zielversion` 90 → **91**. `SCHRITT_91_KWKG_KOSTENANTEIL = 91` steht in
`SCHRITTE_SQLITE` **hinter 90**; `Schritt_91_KwkgKostenanteil` ist wortgleich gebaut wie
der DDL-Teil des Schrittes 90: zählen, die Anweisungen der Quelle fahren, nachzählen,
Protokollnotiz. **Kein DML** — Schritt 89 hat den Wert längst übertragen; ein zweites Mal
übertragen hieße, eine seither gepflegte Anlagenzelle zu überschreiben.

Kein Tabellenneubau nötig: Die Spalte steht unter keinem Index, in keinem Fremdschlüssel,
in keiner generierten Spalte, keinem Trigger, keiner Sicht und keiner
Tabellen-CHECK-Bedingung.

**Das Werkzeug gefahren:** `Schritt 91 — KWKG-Projektspalte Kostenanteil: 1` →
`Spalten offen jetzt 0`, Schemastand 90 → 91, danach `VACUUM`. Der Trockenlauf danach
findet nichts mehr (0 Spalten, 0 Tabellen anzulegen). Die Testdatenbank ist mit aktivem
LFS-Filter committet (Zeiger geprüft: 130 Byte, `size 70782976`).

### 2.3 Leser, Schreiber und Dialogfeld

| Datei | Was entfällt |
|---|---|
| `WirtschaftlichkeitCtrl.cs` | CREATE-Fragment `"KWKG_Kostenanteil" REAL`, die `SpalteSicher`-Zeile, das Lesen in `LadeParameter`, die UPDATE-Spalte samt Parameter, die INSERT-Spalte samt Parameter — **Platzhalterkette 45 → 44** |
| `WirtschaftlichkeitDaten.cs` | `WirtschaftlichkeitParameter.KwkgKostenanteil` |
| `BhkwWirtschaftlichkeitDaten.cs` | `BhkwVorgabenstand.KwkgKostenanteil` und seine drei Stellen (`Aus`, `Gleicht`, `Anwenden`) |
| `BhkwWirtschaftlichkeitDialog.razor` | das Zahlenfeld in Gruppe 2 und der Setter `KostenanteilP` |
| `BhkwWirtschaftlichkeitTexte.cs` | `PKostenanteil` |
| `Resource.resx` / `Resource.en-US.resx` | `BHW_P_KOSTENANTEIL` (beide Sprachen); `Resource.Designer.cs` neu erzeugt, zweiter Lauf +0 |

**Gruppe 2 führt danach fünf Felder:** Einspeisevergütung KWK-Strom, Abschlag
Negativstunden, Pauschale § 9, Stichtag § 6, Förderbeginn. Die leise Zeile unter der Gruppe
(`BHW_P_NUR_PROJEKTWEIT`, beide Sprachen) nennt den Kostenanteil jetzt mit — sonst sucht
ihn ein Anwender, der den Dialog kennt.

**Was bleibt:** das Anlagenfeld `AKostenanteil` in Gruppe 1b, die Spalte
`Tab_Energieanlagen.KWKG_Kostenanteil`, `KontingentDerAnlage` und die weiche Sperre „kein
Kostenanteil" am Kontingentvorschlag.

**iOS nicht berührt:** `IosProjektQuelle` reicht das Parameterobjekt durch; keine Signatur
nennt die entfallene Eigenschaft, unter `EPOS.iOS/` ist keine Datei geändert.

---

## 3 Die Messung — Ergebnisgleichheit

Projekt **1030**, Szenario Erwartet, flache Stundenreihen aus dem gebuchten Lauf,
Modulzuordnung absichtlich verstellt (derselbe Aufbau wie bei BK1a).

| Größe | vor BK1b | nach BK1b |
|---|---|---|
| KWK-Zuschlag Jahr 1 | 7.315,948722 €/a | **7.315,948722 €/a** |
| Kapitalwert | −21.895.377,339395 € | **−21.895.377,339395 €** |

Beide Zahlen stehen jetzt als Anker in `KwkgErsatzwegGewichtetTests` — der Kapitalwert ist
mit dieser Etappe dazugekommen, damit die Ergebnisgleichheit nicht nur gemessen, sondern
gehalten wird.

**Referenzlauf** der fünf CI-Projekte (1030, 1007, 1017, 1045, 1046) gegen
`2026-09-18_R9_Kesselbrennstoff`: **5/5 PASS**, und jede Datei **byte-gleich**. Die Basis
führt keine KWKG-Projektgröße — anders war es nicht zu erwarten, und gerechnet wurde es
trotzdem.

---

## 4 Nachweis

**Neue Prüffälle** in `KwkgProjektaltspaltenTests`:

- Schritt 91 entfernt genau die Kostenanteilspalte (`Offen91` 1 → 0), keine Zeile geht
  verloren, die vier Nachbarn `KWKG_Pauschalmodus`, `KWKG_Abschlag_Negativ`,
  `KWKG_Stichtag`, `KWKG_Inbetriebnahme` stehen, das Anlagenfeld bleibt, ein zweiter Lauf
  gibt nichts mehr heraus;
- die Anweisung ist ein `DROP COLUMN` und steht **nicht** in der Liste des Schrittes 90
  (die führt weiterhin genau sechs Spalten);
- Migrationslauf auf einer Altdatei **89 → 90 → 91**: 89 überträgt den Kostenanteil in die
  Anlage, 90 lässt die Projektspalte stehen, 91 entfernt sie, der Wert steht danach an der
  Anlage;
- Zielstand trägt 91; **Folgeprobe** `Schritt_91_steht_hinter_Schritt_90`.

`KwkgProjektspaltenWacheTests` bekommt einen eigenen Fall für die siebte Spalte (sie kehrt
nach `StelleTabellenSicher` nicht zurück); `TestDatenbank.AltspaltenKwkgProjektWiederherstellen`
stellt sie mit her. Dialogtests (bunit, beide Sprachen): Gruppe 2 hat genau fünf Felder,
der Kostenanteil steht nur noch an der Anlage.

**Gegenproben, gefahren und zurückgebaut:**

| Eingriff | Erwartet | Gemessen |
|---|---|---|
| eine `SpalteSicher`-Zeile für `KWKG_Kostenanteil` stehen lassen | Wache rot | `Die_siebte_Spalte_kehrt_nach_StelleTabellenSicher_nicht_zurueck` **rot** |
| Schritt 91 vor Schritt 90 registriert | Folgeprobe rot | `Schritt_91_steht_hinter_Schritt_90` **rot** |

**Gate:**

| Prüfung | Ergebnis |
|---|---|
| `WP-Plan.Kern.slnf` Build (Release) | 0 Fehler, **5 Warnungen** (Schranke 7) |
| Tests, Kultur `de-DE` | 8.848 grün, 1 übersprungen, 0 rot |
| Tests, `LC_ALL=en_US.UTF-8` | 8.848 grün, 1 übersprungen, 0 rot |
| Windows-Schale (`-p:EnableWindowsTargeting=true`) | 0 Fehler, 5 Warnungen |
| `SqlDialektPruefer` | 1.493 SQL-Texte, **0 Fundstellen** |
| `ChartProben` | 64 Bilder, 0 Verstöße |
| Referenzlauf 1030/1007/1017/1045/1046 | 5/5 PASS, byte-gleich |

---

## 5 Wo es nachzulesen ist

- Konzept § 6.3 Punkt 9l (`BK1-4`) durchgestrichen mit Grund, § 2.2 Gruppe 2 auf fünf
  Felder, § 6.5 „Doppelte Wahrheiten" zu Ende geschrieben;
- `Referenzlaeufe/LIESMICH.md`: Schemastand 91 und der Schrittabsatz 91;
- Wiki-Quelle der Wirtschaftlichkeitsseite: Gruppe 2 ohne den Kostenanteil.

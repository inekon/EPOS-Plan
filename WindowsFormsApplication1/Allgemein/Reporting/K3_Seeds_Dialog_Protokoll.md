# K3 · Einheiten-Seeds und Trägerdialog — HF3/M-B und HF2 § 4.3

**Stand: 20.08.2026.** Umsetzung der Etappe **K3** aus
[`Konzept_Kosten_Energietraeger_EPOS-Plan.md`](../../../Konzept_Kosten_Energietraeger_EPOS-Plan.md)
(§ 5 HF3/M-B, § 4.3 Dialog, § 10). Ausgangsstand `cdf500a`. Vorgängeretappen:
[`K1_Aufraeumung_Protokoll.md`](K1_Aufraeumung_Protokoll.md),
[`K2_Einheitenpruefung_Protokoll.md`](K2_Einheitenpruefung_Protokoll.md).

**Ergebnis in drei Sätzen.** Migrationsschritt **26** stellt die acht gasförmigen
Träger auf **Nm³** um, sät je Gas-Brennstoff den **z-Faktor** `m³ → Nm³` mit Faktor
**1,0** und berichtigt die Namen der Identitätsregeln — alles ohne einen einzigen
geänderten Zahlenwert. Die Abnahmebedingung der Etappe („`PruefeKatalog()` liefert null
Befunde") ist erfüllt, allerdings **nicht** durch die im Konzept vorgesehenen
`Einheit → kWh`-Seeds: Die hätten den Heizwert doppelt gepflegt oder eine falsche
Aussage in die Regeltabelle geschrieben, weshalb stattdessen der **Prüfer** auf die
Semantik aus § 4.2 nachgezogen wurde. `ucFuelSettings` hat jetzt den Umrechnungsblock
aus § 4.3 mit editierbarer Regelliste, Deaktivierungs-Riegel, Live-Effektivanzeige,
rotem Verstoßhinweis und blockierender Speicherprüfung.

---

## 1 Was geändert wurde

| # | Gegenstand | Datei : Zeile |
|---|---|---|
| 1 | Einheiten-Persistenzwerte `kWh` / `m³` / `Nm³` | `Allgemein/DbWerte.cs:1084-1127` |
| 2 | Zielversion 25 → **26** | `Allgemein/Update/SchemaMigration.cs:77` |
| 3 | Schrittnummer 26 samt Begründung der Semantik-Auflösung | `SchemaMigration.cs:674-756` |
| 4 | Registrierung im Schrittregister (hinter Schritt 25) | `SchemaMigration.cs:1229-1241` |
| 5 | Zählwerk `DatenNormkubikTraeger` / `…Codes` / `DatenZFaktorGesaet` + Rücksetzung | `SchemaMigration.cs:1000-1010`, Rücksetzung `:1292-1294` |
| 6 | Der Schritt selbst (26a/26b/26c) | `SchemaMigration.cs:2404-2652` — `Schritt_26_EinheitenSeeds` (`:2437`), `NormkubikUmbenennen` (`:2489`), `ZFaktorSaeen` (`:2570`), `IdentitaetsregelnBerichtigen` (`:2637`) |
| 7 | **Prüfer-Nachschärfung** auf die Semantik aus § 4.2 | `Controller/EnergieEinheitenPruefung.cs:110-133` (Klassendoku), `:530-597` (`Beurteile`) |
| 8 | Dialog-API des Prüfers: Regeltyp, Regel-Leser, kWh-Frage, Riegel | `EnergieEinheitenPruefung.cs:22-56` (`UmrechnungsRegel`), `:230-357` (`RegelnDesBrennstoffs`, `ErreichtKwh`, `DarfAbschalten`) |
| 9 | Umrechnungsblock im Trägerdialog | `Views/Kosten/ucFuelSettings.cs:48-72` (Felder), `:166-490` (Aufbau, Raster, Riegel, Effektivanzeige, Persistenz) |
| 10 | Blockierende Speicherprüfung | `ucFuelSettings.cs:596-640` (`SpeichernErlaubt`, `SaveProjectAndHistory`), `:781-797` (`btn_Save_Click`) |
| 11 | Live-Anbindung an Einheiten- und Heizwertwechsel | `ucFuelSettings.cs:551` (`CmbUnit_SelectedIndexChanged`), `:583-585` (`UpdatePricePerKWh`) |
| 12 | 12 neue UI-Texte, **rein additiv** | `MyResource/Resource.resx` (+36), `Resource.en-US.resx` (+36), `Resource.Designer.cs` (+108) |
| 13 | Dieses Protokoll | `Allgemein/Reporting/K3_Seeds_Dialog_Protokoll.md` (neu) |

**Sieben geänderte und eine neue Datei**, 1.342 eingefügte und 43 geänderte Zeilen.
Kein `DROP`, keine gelöschte Datei.

---

## 2 Schrittnummer 26

`ZIEL_VERSION` stand nach K2 bei **25**; 26 ist die nächste freie Nummer. Die
Parallel-Session hat in der Zwischenzeit keine Nummer vergeben (geprüft: höchste
Konstante im Code war `SCHRITT_25_EINHEITENKONSISTENZ`). Dieselbe Regel wie immer — der
Versionsmarker hält genau eine Zahl fest; zwei Schritte mit derselben Nummer machten den
jeweils anderen unbrauchbar.

---

## 3 Die Semantik-Auflösung — der fachliche Kern dieser Etappe

### 3.1 Der Widerspruch

Das Konzept sagt an zwei Stellen Verschiedenes:

- **§ 5 (Seed-Tabelle)** verlangt für Öl, Flüssiggas, Kohle und Koks eine Regel
  „`l → kWh` **über Hi/Hs**", „`kg → kWh` **über Hs/Hi**".
- **§ 4.2 („Klärung Semantik")** sagt: „`energy_conversion` bleibt **Einheiten**-Umrechnung;
  die Energie-Umrechnung leisten weiterhin Hi/Hs … Die kWh-Bedingung aus L2 gilt als
  erfüllt, wenn die Einheitenkette bei einer Einheit endet, für die Hi/Hs gepflegt ist,
  **oder** direkt bei kWh."

Für den `factor` einer Regel `l → kWh` gäbe es nur zwei mögliche Werte, und **beide sind
falsch**:

| Kandidat | Warum er ausscheidet |
|---|---|
| `factor = Hi` | **Doppelpflege des Heizwerts.** Der Wert stünde in `energy_carrier.hi_kwh_per_unit` UND in `energy_conversion.factor`. Spätestens beim ersten Pflegevorgang driften beide auseinander, und keine Stelle entscheidet, welcher gilt. § 4.2 untersagt genau das. |
| `factor = 1,0` | **Sachlich falsche Aussage** — „1 l = 1 kWh". Sie stünde ab dieser Etappe sichtbar im Regelblock des Trägerdialogs und lüde jeden Anwender zum Fehlschluss ein. |

### 3.2 Die gewählte Auflösung

**Nachgezogen wurde der Prüfer, nicht die Datenlage.** § 4.2 ist die spätere,
ausdrücklich als „Klärung" überschriebene Präzisierung von L2 und damit maßgeblich. Die
K2-Fassung des Prüfers hatte L2 wörtlich gelesen („es existiert eine aktive
Umrechnungsregel `billing_unit → kWh`") und deshalb 17 von 21 Katalogträgern als Verstoß
gemeldet — obwohl **jeder** von ihnen einen gepflegten Heizwert trägt.

Der Prüfer kennt seit K3 **drei** Wege nach kWh:

1. **Identität** — `billing_unit = kWh` (Strom, Fernwärme). Die Menge *ist* die Energie.
2. **Heizwert bzw. Brennwert** — `hi_kwh_per_unit` oder `hs_kwh_per_unit` ist gepflegt.
   Beide sind je **Abrechnungseinheit** definiert (Bestand: Erdgas E 10,50 kWh/Nm³,
   Heizöl L 11,20 kWh/l, Koks 8,00 kWh/kg) und leisten damit genau den Schritt nach kWh,
   den § 4.2 ihnen zuweist.
3. **Ausdrückliche Regelkette** — eine aktive Kette (höchstens zwei Stufen, Faktoren > 0)
   endet buchstäblich bei `kWh`. Der Weg, den ein Anwender sich selbst anlegen kann; er
   bleibt gültig, damit niemand für eine gepflegte Regel bestraft wird.

**Kein Zahlenwert wurde erfunden.** Die Aussage des Prüfers ist seither die des
Konzepts.

### 3.3 Die zwei Befunde bleiben trennscharf

| Code | Bedeutung nach K3 |
|---|---|
| `KWH_UNERREICHBAR` | Der echte L2-Verstoß: **keiner** der drei Wege trägt. Blockiert das Speichern im Dialog. |
| `HEIZWERT_FEHLT` | Der brüchige Sonderfall: Der Träger erreicht kWh **nur** über die Regelkette, ohne dass Hi oder Hs gepflegt wäre. Kein Verstoß — aber schaltet jemand die Regel ab, kippt der Träger, und Abrechnung wie Simulation lesen ohnehin Hi/Hs, nicht die Regel. Erscheint als Hinweis, blockiert **nicht**. |

### 3.4 Messung

Beide Semantiken gegen dieselbe Datenbank gerechnet (Scratch-Kopie der Arbeitskopie):

| Prüfer-Fassung | Befunde |
|---|---|
| **K2**, nur Einheit/Regelkette | **17** |
| **K3**, Hi/Hs zählt mit | **0** |
| **K3**, nach Migrationsschritt 26 | **0** |

Damit ist das Abnahmekriterium der Etappe (§ 10: „`PruefeKatalog()` = 0 Befunde")
erfüllt.

---

## 4 Migrationsschritt 26

| Teil | Wirkung an der Arbeitskopie |
|---|---|
| **26a** Nm³-Umbenennung | **8** Katalogträger (`billing_unit`), **5** `from_unit`, **5** `to_unit`, **12** Zeilen `energy_price.arbeitspreis_unit` |
| **26b** z-Faktor-Seed `m³ → Nm³`, Faktor 1,0, Name „z-Faktor" | **5** Regeln gesät (je ein Gas-Brennstoff: 1 Stadtgas, 2 Erdgas LL, 3 Erdgas E, 14 Biogas, 25 Wasserstoff) |
| **26c** Namensberichtigung der Identitätsregeln | **5** Regeln von „z-Faktor" auf „Umrechnungsfaktor" |

**Warum 26c nötig ist.** Schritt 25c hatte in K2 **alle** Regeln eines Gasträgers
pauschal „z-Faktor" genannt — damals gab es je Gas-Brennstoff nur die eine
Identitätsregel, und die Unterscheidung war ohne Gegenstand. Mit dem Seed aus 26b gibt es
sie: Der z-Faktor ist die Regel `m³ → Nm³`. Eine Identitätsregel `Nm³ → Nm³`, die weiter
so hieße, stünde als zweite gleichnamige Zeile im Regelblock. Berichtigt wird nur, was
drei Bedingungen erfüllt: gleiche Von- und Nach-Einheit, Name noch **exakt** der
K2-Vorgabewert, und `user_edited` nicht gesetzt.

**Das ASCII-`m3` bleibt unangetastet.** Der Bestand kennt zwei Zeichenketten: `m³`
(U+00B3, bei den Gasregeln) und `m3` (in `l → m3` und `kg → m3` der Öl- und
Festbrennstoffträger). Der Vergleich `= 'm³'` trifft nur die erste — richtig so, denn Nm³
ist eine Aussage über Gase, nicht über Heizöl. Die Einschränkung auf die Gas-Brennstoffe
wäre für sich schon ausreichend; die exakte Zeichenkette ist die zweite Sicherung.

**`user_edited = true` wird nie überschrieben** (L5) — jede schreibende Anweisung des
Schritts schließt solche Zeilen aus. Im Bestand trägt keine einzige Zeile das Kennzeichen
(gemessen: 0 von 59), der Riegel gilt trotzdem.

### 4.1 Ein Idempotenz-Defekt, gefunden im Zweitlauf

Die erste Fassung von 26a benannte **jede** Regel eines Gas-Brennstoffs mit
`from_unit = 'm³'` um. Der Zweitlauf zeigte, was das anrichtet:

```
ZWEITLAUF (fehlerhafte Fassung): 26a=0/5/0   26b-fehlend=5   26c=5
```

Der zweite Lauf machte aus dem z-Faktor `m³ → Nm³` die Identität `Nm³ → Nm³`; 26b säte
ihn daraufhin erneut, 26c benannte ihn um — und die Regeltabelle wäre bei **jedem**
weiteren Lauf um fünf Zeilen gewachsen. Da die Hausregel „idempotent unabhängig vom
Marker" verlangt, wäre das ein echter Datenschaden gewesen.

**Behoben** durch `AND [to_unit] <> 'Nm³'` in der `from_unit`-Umbenennung
(`SchemaMigration.cs:2513-2524`): Der z-Faktor trägt das Betriebsvolumen absichtlich
weiter als Von-Einheit und ist der Umbenennung entzogen. Beim **ersten** Lauf ist die
Ausnahme folgenlos — vor 26b gibt es keine Zeile mit `to_unit = Nm³`.

```
ZWEITLAUF (korrigierte Fassung): 26a=0/0/0   26b-fehlend=0   26c=0
```

---

## 5 Berichtigung eines K2-Befunds: es gibt **keine** Faktor-0-Regeln

Das K2-Protokoll führt in Abschnitt 6 einen Nebenbefund, die Regeln `L→m3`, `kg→t`,
`kg→m3`, `kg→rm`, `kg→SRM` und `kWh→MWh` trügen „durchweg Faktor 0", und der Auftrag für
K3 sah eine entsprechende Reparatur vor (Aufgabe 1c).

**Der Befund war falsch.** Er entstand durch die zweistellige Vorgabe-Rundung, mit der
PowerShells `Format-Table` `Double`-Werte anzeigt: 0,001 erscheint dort als „0,00". Die
Nachmessung mit voller Genauigkeit (`.ToString("R")`) ergibt:

| Regel | Faktor | Bewertung |
|---|---|---|
| `L → m3` | 0,001 | korrekt (1 l = 0,001 m³) |
| `kg → t` | 0,001 | korrekt |
| `kWh → MWh` | 0,001 | korrekt |
| `kg → m3` | 0,001 / 0,0010989 | gepflegte Schüttdichten |
| `kg → rm` | 0,0021 | gepflegt |
| `kg → SRM` | 0,0031 | gepflegt |
| `L → kg` | 0,84 / 0,92 | gepflegte Dichten |
| `L → t` | 0,00092 | gepflegt |

Gegenprobe in SQL: `SELECT COUNT(*) FROM energy_conversion WHERE factor <= 0` → **0** von
59. **Aufgabe 1c ist damit gegenstandslos** — es wurde nichts repariert und nichts
abgeschaltet. Insbesondere bleiben die stoffabhängigen Regeln (`kg → m³`, `kg → rm`,
`kg → SRM`) unverändert aktiv; sie zu deaktivieren hätte gepflegte Daten entwertet.

Nach Hausregel werden abgeschlossene Protokolle nicht rückwirkend umgeschrieben (K1,
Abschnitt 2). Der K2-Nebenbefund bleibt deshalb dort stehen und wird **hier** berichtigt.

---

## 6 Trägerdialog `ucFuelSettings` (Konzept § 4.3)

**Programmatisch aufgebaut, Designer unberührt** — dieselbe Hausregel und dieselbe
Bauform wie der Aufschlagsblock aus AP4. Die Bestandssteuerelemente unterhalb
(Speichern-Knopf, „Preishistorie", Historienraster) wandern um die Blockhöhe (196 px)
nach unten, das Control wächst mit. Der Aufschlagsblock dockt an `this.Height` an und
wird deshalb **nach** dem Umrechnungsblock gebaut (`ucFuelSettings.cs:127-129`).

| Anforderung § 4.3 | Umsetzung |
|---|---|
| Tabelle der Regeln, Spalten *Name / von / nach / Faktor / aktiv* | `dgvRegeln`, Spalten programmatisch (`:265-310`) |
| Name und Faktor editierbar | `CellValueChanged` schreibt in den Speicherstand (`:352-...`) |
| `ZahlPruefen`-Validierung des Faktors | `Program.ZahlParsen` (Komma **oder** Punkt) plus Bedingung `> 0`; eine unbrauchbare Eingabe wird nicht übernommen, die Zelle springt auf den letzten gültigen Wert zurück, der rote Hinweis sagt warum |
| Anlegen | `btnRegelNeu` — neue Zeile mit `Von` = aktuelle Einheit, Faktor 1, aktiv; Name je nach Träger „z-Faktor" oder „Umrechnungsfaktor" |
| Deaktivieren möglich | Häkchen `aktiv`, sofort wirksam (`CurrentCellDirtyStateChanged` committet die Zelle) |
| **Riegel**: kWh-Regel bzw. letzte Kette nicht deaktivierbar | `EnergieEinheitenPruefung.DarfAbschalten` — beantwortet die Frage durch **Probieren**: eine Kopie des Bearbeitungsstands mit genau dieser Regel auf „aus" geht durch dieselbe Fachregel. Damit gibt es keine zweite Fassung, die irgendwann abweicht. Bei Ablehnung springt das Häkchen zurück und die Meldung des Prüfers steht im roten Feld |
| Anzeige „effektiv: 1 ⟨Einheit⟩ = X kWh (Hi) / Y kWh (Hs)" | `lblEffektiv`, neu gebildet bei Einheitenwechsel und bei jeder Heizwertänderung; bei kWh-Trägern der Text „Abrechnung unmittelbar in kWh" |
| Gasträger zeigen Nm³ und „z-Faktor" | ergibt sich aus den Daten: `billing_unit` steht nach Schritt 26 auf Nm³, der Seed heißt „z-Faktor". Neue Regeln eines Gasträgers werden mit „z-Faktor" vorbelegt |
| Verstoß-Hinweis **rot statt MessageBox** | `lblVerstoss` (`Color.Firebrick`), gespeist aus `ErreichtKwh` |
| **Blockierende Speicherprüfung** | `SpeichernErlaubt(out grund)`; `SaveProjectAndHistory()` liefert jetzt `bool` und schreibt bei Verstoß **nichts**. Der Speichern-Knopf zeigt zusätzlich eine Meldung |

**Zwei bewusste Feinheiten.**

- Der Hinweis `HEIZWERT_FEHLT` blockiert **nicht**. Er ist kein L2-Verstoß — der Träger
  erreicht kWh ja über die Regelkette. Er steht im roten Feld und hindert niemanden am
  Speichern.
- Geschrieben wird nur, was der Anwender angefasst hat (`UserEdited`). Der Block ist eine
  Pflegemaske, kein Massenschreiber; eine Regel ohne Zieleinheit wird übersprungen statt
  halbfertig gespeichert. Jede Handänderung setzt zugleich `user_edited = true` — ab dann
  fasst sie keine Migration mehr an (L5).

**Ein Randfall bleibt bewusst offen:** `Form_Kosten.OnFormClosing` speichert den
geöffneten Träger beim Schließen mit und wertet den neuen Rückgabewert **nicht** aus. Das
ist Absicht — der dortige Kommentar „Schließen nie am Speichern scheitern lassen" gilt
weiter. Ein verletzender Träger wird beim Schließen also **nicht** gespeichert (die
Prüfung greift), aber ohne Meldung. Der rote Hinweis stand zu diesem Zeitpunkt bereits
sichtbar im Block.

---

## 7 Ressourcen — additiv, drei Hotspot-Dateien

`Resource.resx`, `Resource.en-US.resx` und `Resource.Designer.cs` sind Hotspots der
parallelen KI-Session. **Vor** dem Edit geprüft: `git status` dieser drei Dateien war
**leer** — keine fremden uncommitteten Änderungen. Trotzdem wurde streng additiv
gearbeitet: Alle neuen Einträge stehen **am Ende**, nichts wurde umsortiert oder
umformuliert. Der Diff weist ausschließlich Einfügungen aus (+36 / +36 / +108, **0
Löschungen**).

Zwölf Schlüssel, deutsch und englisch:

`KOSTEN_UMRECHNUNG_TITEL` · `…_SPALTE_NAME` · `…_SPALTE_VON` · `…_SPALTE_NACH` ·
`…_SPALTE_FAKTOR` · `…_SPALTE_AKTIV` · `…_NEU` · `…_EFFEKTIV` · `…_EFFEKTIV_KWH` ·
`…_RIEGEL` · `…_FAKTOR_UNGUELTIG` · `…_SPEICHERN_ABGELEHNT`

---

## 8 Verifikation an der Scratch-Kopie

Ein Lauf, gedeckelt, gegen eine **Kopie** von `Referenzlaeufe\Arbeitskopie\Kenndaten.accdb`
(die Arbeitskopie selbst wurde nur gelesen). Nachgestellt wurden Schritt 25 (als
Vorbedingung) und Schritt 26 in der Codefassung.

| Prüfpunkt | Erwartet | Gemessen |
|---|---|---|
| Prüfer vor 26, K2-Semantik | 17 | **17** |
| Prüfer vor 26, K3-Semantik | 0 | **0** |
| Prüfer **nach** 26 | 0 | **0** |
| `billing_unit` der Gasträger | 8 × Nm³ | **8 × Nm³** |
| `faktor_name`-Verteilung | 59 Standard + 5 z-Faktor | **59 / 5** |
| Zweitlauf 26a / 26b / 26c | 0 / 0 / 0 | **0 / 0 / 0** |
| Regelzahl | 59 → 64 | **59 → 64** |
| `SUM(factor)` | 36,0242189 → 41,0242189 | **Delta exakt 5,0** |

Das Delta von genau **5,0** ist der Beleg der Ergebnisneutralität: Es entspricht Zeile
für Zeile den fünf neuen z-Faktor-Seeds mit Faktor 1,0. **Kein Bestandsfaktor wurde
verändert.**

---

## 9 Ergebnisneutralität — die Belegkette

1. **Die Umbenennung ist reine Semantik.** Die Katalog-Heizwerte der Gasträger sind seit
   jeher Normwerte — Erdgas E mit 10,50 kWh je m³ *ist* der kWh/Nm³-Wert. Der Schritt
   schreibt hin, was gemeint war; er rechnet nichts um. Auch
   `energy_price.arbeitspreis_unit` zieht nur den Einheitentext nach, der Preiszahlenwert
   bleibt.
2. **Der z-Faktor-Seed steht auf 1,0** (Entscheidung E6). Eine Multiplikation mit 1
   verschiebt keine Rechnung.
3. **Es entsteht keine `Einheit → kWh`-Regel** (Abschnitt 3) — also auch kein zweiter,
   konkurrierender Heizwert.
4. **Die neuen Spalten liest weiterhin kein Rechenpfad.** Der K2-Befund gilt unverändert:
   `ucFuelSettings.GetConversions`, `GetConvID`, `GetTargetUnitByConversionId` und
   `WizardCtrl` lesen `energy_conversion` mit ausgeschriebener Spaltenliste; Mengen- und
   Kostenrechnung gehen über `Abfrage_Energietraeger_Effektiv`.
5. **Der Prüfer rechnet nichts.** Seine Nachschärfung ändert nur, welche Befunde er
   meldet — sie kann per Konstruktion kein Ergebnis verschieben.

**Ausstehend wie in K1 und K2:** der tatsächliche Referenzlauf-Vergleich und der
Duplizieren-Smoke. Sie waren nicht Teil des Auftrags.

---

## 10 Encoding-Befund je angefasster Datei

Jede Datei vor dem Schreiben gemessen und danach gegengeprüft. Alle sieben sind gültiges
UTF-8 **mit** BOM und CRLF; die cp1252-Falle des Baums traf keine von ihnen.

| Datei | vorher | nachher |
|---|---|---|
| `Allgemein/DbWerte.cs` | BOM, CRLF, 90.262 B | 92.762 B · BOM · 1.544 CRLF · **0** nackte LF |
| `Allgemein/Update/SchemaMigration.cs` | BOM, CRLF, 279.804 B | 304.938 B · BOM · 5.511 CRLF · **0** |
| `Controller/EnergieEinheitenPruefung.cs` | BOM, CRLF, 28.218 B | 41.186 B · BOM · 820 CRLF · **0** |
| `Views/Kosten/ucFuelSettings.cs` | BOM, CRLF, 29.262 B | 49.999 B · BOM · 1.090 CRLF · **0** |
| `MyResource/Resource.resx` | BOM, CRLF, 283.869 B | 285.433 B · BOM · 6.173 CRLF · **0** |
| `MyResource/Resource.en-US.resx` | BOM, CRLF, 278.992 B | 280.520 B · BOM · 6.167 CRLF · **0** |
| `MyResource/Resource.Designer.cs` | BOM, CRLF, 719.169 B | 723.511 B · BOM · 17.753 CRLF · **0** |
| `Allgemein/Reporting/K3_Seeds_Dialog_Protokoll.md` | neu | UTF-8 **ohne** BOM, CRLF — nach `.editorconfig` `[*.md]` |

**Eine Zwischenkorrektur:** Das Skript, das die Designer-Zugriffe einfügte, schrieb
zunächst 108 nackte LF in eine reine CRLF-Datei. Der Messschritt danach fand sie, und die
Datei wurde vor dem Build wieder vereinheitlicht (17.753 CRLF, 0 nackte LF). Ohne die
Messung wäre das im Diff untergegangen.

---

## 11 Build — Baseline gegen Ende

Baseline gegen `cdf500a` gemessen; HEAD hatte sich seit K2 nicht bewegt.

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    WindowsFormsApplication1\WindowsFormsApplication1.csproj -t:Rebuild -p:Configuration=Debug -p:Platform=x86
```

| Lauf | Ergebnis | Fehler | Warnungen |
|---|---|---|---|
| **Baseline** (`cdf500a`, vor K3) | erfolgreich | **0** | **6** |
| **Ende** (nach K3) | erfolgreich | **0** | **6** |

Dieselben sechs Warnungen wie seit K1, sämtlich aus unberührten Dateien
(`WErzeugerModel.cs`, `KlimaregionStammCtrl.cs` ×2, `StromverbraucherStammCtrl.cs`,
`MDIMainForm.cs` ×2). **Keine neue Warnung aus einer K3-Datei.**

**Konfliktmarker-Sweep** (`*.cs`, `*.md`, `*.resx`; ohne
`mit_Puffer_KI_Lösungsversuch\`, `Tempkib2\`, `WindowsFormsApplication1 - Kopie\`,
`.claude\`, `bin`/`obj`): am Anfang **872 Dateien, 3 Treffer**, am Ende **873 Dateien,
3 Treffer** — unverändert dieselben drei, und alle drei sind der in Backticks
gesetzte Prosa-Verweis auf eben diesen Sweep (Konzept `:268`/`:283`, K1-Protokoll,
K2-Protokoll). Kein echter Marker; dieses Protokoll nennt den Sweep beim Namen, ohne
die Zeichenfolge selbst zu führen.

---

## 12 Nachtrag: KI-Leseaktion `energietraeger_pruefen`

Die in K2 zurückgestellte Leseaktion ist mit dieser Etappe nachgeholt — als **eigener
Commit** unmittelbar nach dem K3-Commit, damit der Umfang trennscharf bleibt.

**Vorabprüfung.** `git status` über `Allgemein\KI\**`, `KiKern\**`, `KiHarnisch\**` und
`MyResource\*` war **leer**: Die Parallel-Session hatte alles committet, es lagen keine
fremden Änderungen im Weg. Der Grund der Zurückstellung („Kollisionsvermeidung mit dem
parallel bearbeiteten Aktionsregister", K2 Abschnitt 10) war damit entfallen.

**Umgesetzt** nach dem Muster der Bestandsaktionen (`KiAktionenWirtschaft.cs`):

- neue Datei `Allgemein/KI/Aktionen/KiAktionenEnergie.cs` mit der Aktion
  `energietraeger_pruefen`, Schutzstufe **Lesen**;
- ein optionaler Parameter `projekt_id` — fehlt er, läuft `PruefeKatalog()`, sonst
  `PruefeProjekt(id)`;
- Rückgabe: Befundanzahl plus je Befund eine Zeile
  (`carrier_id`, `traeger`, `problem_code`, `klartext`);
- Registrierung als **ein** additiver Einzeiler in `KiAktionen.cs`;
- Texte additiv in `KiAktionsTexte.cs` im dortigen Muster.

**Offen:** Der Chat-Livetest steht aus — er gehört Philipp (F1-Chat, Frage „Prüfe die
Energieträger"). Belegt ist die Registrierung per Grep und der grüne Build.

---

## 13 Offene Punkte

1. **Referenzlauf-Vergleich und Duplizieren-Smoke** stehen weiter aus (Abschnitt 9) — wie
   in K1 und K2 nicht Auftragsteil.
2. **Chat-Livetest der KI-Aktion** durch Philipp (Abschnitt 12).
3. **`Form_Kosten.OnFormClosing`** wertet den neuen `bool`-Rückgabewert nicht aus
   (Abschnitt 6, bewusst). Wer dort eine Meldung will, braucht eine Entscheidung darüber,
   ob das Schließen aufgehalten werden darf — das ist eine UI-Frage für K4.
4. **Die Alt-Heizwert-Gegenprobe aus § 5** („Abweichungen > 10 % zum EPOS-Katalog werden
   im Migrationsprotokoll gemeldet") ist **nicht** umgesetzt. Sie hängt an den Werten aus
   `TABELLEN.XLS` (Erdgas 11,48 kWh/m³ · Flüssiggas 13,77 kWh/kg · Öl 10,08 kWh/l ·
   Biogas 6 kWh/m³ · Rapsöl 8,75 kWh/l), die im Repo nicht maschinenlesbar vorliegen;
   eine im Code hinterlegte Kopie wäre eine dritte Heizwert-Wahrheit. Vorschlag: als
   einmalige Sichtprüfung in die Access-Checkliste (K6) statt in den Migrationsschritt.
5. **`Nm³` in der Simulationsseite.** `SimulationWaermebedarf.cs:388` vergleicht auf den
   Anzeigetext `"Gasverbrauch [m³/a]"`. Das ist eine **andere** Zeichenkette (Bezeichner
   einer Bedarfsart, nicht `billing_unit`) und von Schritt 26 unberührt — der Vergleich
   funktioniert weiter. Ob dort mittelfristig „Nm³/a" stehen soll, ist eine
   Beschriftungsfrage für K4.
6. **`energy_unit`/`energy_group`** bleiben unangetastet; ihre Löschung ist M-E in K6.

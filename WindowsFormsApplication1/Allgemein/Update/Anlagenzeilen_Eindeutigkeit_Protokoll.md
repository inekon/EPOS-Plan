# Anlagenzeilen-Eindeutigkeit — eine Zeile je Projekt und Gerät

**Paket ANLAGENZEILEN-EINDEUTIGKEIT.** Nutzerentscheidung vom 18.08.2026: „Prüfung **und** Index".
Umgesetzt am 18.08.2026 auf `main` (Ausgangsstand 83e1b82).

---

## 1  Befundlage

In `Tab_Energieanlagen` konnten mehrere Zeilen desselben Projekts auf dasselbe Gerät zeigen.
**Kein einziger Schreibpfad prüfte das** — sämtliche vorhandenen Prüfungen laufen über den
*Bezeichner*, nicht über den Geräteverweis. Ein Eindeutigkeitsindex fehlte.

Wirkung auf den Rechenlauf: Die Simulation baut ihre Modullisten **je Anlagenzeile** auf
(`SimulationControl.WP_Liste_Laden`, `SPK_Liste_Laden`, `BHKW_Liste_Laden`, `Solar_Liste_Laden` —
kein `DISTINCT`), also zählt dasselbe Gerät doppelt: Wärmepumpe in `SimulationWaermepumpe.cs:300/312`,
Kessel in `SimulationSPK.cs:188/205/238` (dort sogar über den Bezeichner aufgelöst).

Nicht betroffen und bewusst nicht gesperrt:

| Verweis | Warum keine Sperre |
|---|---|
| `ID_PUFFER` | doch gesperrt — geschützt war nur die Registry-Speisung (`SimulationControl.cs:2288-2297`, Geräte-ID), nicht das Anlegen |
| `ID_PV`, `ID_Solar` | mehrere **Felder** desselben Modultyps sind fachlich richtig; die Engine rechnet dort bewusst je Zeile |
| `ID_SP` | eine zweite Zeile ist eine **Variante** desselben Speichers, kein zweiter Speicher (Fachkonzept Stromspeicher 7.3); gerechnet wird ohnehin nur die aktive Variante |

**Bestand zum Zeitpunkt der Umsetzung** (Produktiv-`Kenndaten.accdb`, Schemastand 14, rein lesend
geprüft): 8 doppelt belegte Anlagenzeilen — `ID_WP` in Projekt 1011 (2 Zeilen), `ID_Kessel` in den
Projekten 1009 und 1011 (4 Zeilen), `ID_PUFFER` in Projekt 1018 (2 Zeilen); `ID_BHKW` sauber.
**Diese Bestandsdaten wurden NICHT verändert** — die Bereinigung ist noch nicht entschieden.

---

## 2  Leitgedanke: Engine rechnet je Zeile, Kosten zählen je Gerät

Seit Commit 605dcb8 zählt die **Kostenseite je Gerät** (`TechnikPlanwertCtrl.cs:204-211`,
`GROUP BY` über die Verweisspalte), die **Engine rechnet je Zeile**. Solange Doppelzeilen möglich
sind, widersprechen sich beide Deutungen: Dieselbe Anlage geht zweimal in die Wärmebilanz und
einmal in die Investition ein.

Der hier eingeführte Index beseitigt genau das. **Ist je Projekt und Gerät nur eine Zeile erlaubt,
sind „je Zeile" und „je Gerät" wieder dasselbe** — beide Deutungen fallen zusammen, und keine der
beiden Seiten muss umgebaut werden. Das ist der eigentliche Grund für dieses Paket; die
Doppelzählung in der Simulation ist die sichtbare Folge, nicht die Ursache.

Wer eine Anlage bewusst zweimal will, bekommt sie weiterhin — aber als **zweites Gerät** mit
eigener Projektkopie, eigener Investition und eigener Wartung. Damit ist die Doppelung eine
Aussage des Anwenders und keine stille Datenlage mehr.

---

## 3  Umsetzung

### 3.1  Neue zentrale Klasse

`Allgemein/Update/AnlagenEindeutigkeit.cs` — **eine Wahrheit** für Dialog, Index und Bericht.

| Zeile | Baustein | Aufgabe |
|---|---|---|
| `:15` | `GeraeteSperre` | Beschreibt eine gesperrte Spalte: Verweisspalte, Gerätetabelle, Klartext, Kindtabellen |
| `:79` | `SPERREN` | Die vier Sperren: `ID_WP`→`Tab_WP` (Kinder `Tab_Kenndaten`, `Tab_Kenndaten_Kuehlung`), `ID_Kessel`→`Tab_Heizkessel`, `ID_BHKW`→`Tab_BHKW`, `ID_PUFFER`→`Tab_Pufferspeicher` |
| `:105` / `:120` | `IndexName` / `SqlIndex` | `idx_Anlage_<Spalte>`, `CREATE UNIQUE INDEX … (ID_Projekt, <Spalte>)` |
| `:138` / `:152` | `SqlDublettenGruppen` / `SqlDublettenZeilen` | Vorabprüfung des Index bzw. die Einzelzeilen für die Meldung |
| `:179` / `:182` | `Frage` / `Hinweis` | Austauschbare Dialoghaken (Prüfstand, Engine-Modus) |
| `:238` | `ZweitesGeraetBestaetigen` | Die Rückfrage für die Oberfläche — derselbe Wortlaut wie im Schreibweg |
| `:250` | `BereitsInListe` | UI-Vorprüfung gegen die Auswahlliste |
| `:283` | `Belegung` | Die in EINEM Schreibdurchlauf vergebenen Geräte-IDs und Variantennamen |
| `:333` | `Aufnehmen` | Die Prüfung selbst: erkennen, fragen, Kopie erzwingen oder verwerfen |
| `:392` | `ZeileVorhanden` | Zeigt im Projekt bereits eine Zeile auf dieses Gerät? |
| `:416` | `EindeutigerBezeichner` | „PS 800", „PS 800 (2)", … — Verallgemeinerung von `PufferSpCtrl.EindeutigerBezeichner:458` auf alle vier Gerätetabellen |
| `:465` | `ProjektkopieAnlegen` | **Der Weg, den bisher kein Gewerk hatte** (siehe 3.3) |
| `:609` | `FeldHinweisPruefen` | PV/Solar: Hinweis nur bei identischer Neigung **und** Azimut **und** Modulanzahl |
| `:650` | `SpeichervarianteBenennen` | Namensprüfung der Speichervarianten auf dem Wizard-Weg |

### 3.2  Teil A — Prüfung beim Anlegen

**Zentraler Schreibweg.** `Controller/WizardCtrl.cs`, `Add_WP_Waermeerzeuger`:

- `:777` — `Belegung` für den Durchlauf. **Warum nicht nur ein `SELECT`:** Der Speicherweg aller
  Erzeuger ist Löschen + Neuanlegen (`Del_Projekt_Waermeerzeuger` + `Add_WP_Waermeerzeuger`). Beim
  Eintritt sind die alten Zeilen bereits fort und die neuen noch nicht alle da — die Dublette
  entsteht **innerhalb der Liste** (zwei Einträge gleichen Bezeichners lösen über `CopyFromStamm`
  auf dieselbe Projektkopie auf) und wird genau dort erkannt.
- `:793` / `:799` / `:805` / `:848` — je Gewerk wird die gesperrte Spalte gemerkt.
- `:876-889` — die Prüfung: `Aufnehmen` fragt nach, erzwingt bei „Ja" eine eigene Gerätekopie
  (Bezeichner der Anlagenzeile wandert mit) und liefert `0`, wenn der Anwender verneint — dann
  wird die Zeile übergangen.
- `:929` / `:939` — `Verweis` / `VerweisSetzen`: die einzige Stelle, an der Spaltenname und
  Modellfeld einander zugeordnet werden.
- `:860` / `:868` — PV und Solar: **keine Sperre**, höchstens ein Hinweis je Speichervorgang.
- `:821` — Stromspeicher: **keine Gerätesperre**, aber die Namensprüfung aus
  `StromspeicherKontextMenuCtrl.VarianteAnlegen` (dort `NameVergeben:601`) gilt jetzt auch hier.

**Oberfläche.** `Views/Pufferspeicher/Form_PufferSp.cs:109-113` — die Vorprüfung beim Aufnehmen,
damit der Anwender die Meldung sieht, *während* er den Speicher hinzufügt und nicht erst beim
Speichern. Die Antwort wandert über `WErzeugerModel.GeraetekopieErzwingen`
(`Model/WErzeugerModel.cs:48`, **nicht persistent**) an den Schreibweg; ohne diese Weitergabe käme
dieselbe Frage zweimal.

**Puffer-Nebenwege.** `Controller/PufferSpCtrl.cs:386` (`ProjektPufferAnlegen`) und `:818`
(Pendelspeicher) tragen die Anlagenzeile jetzt nur nach, wenn weder der Bezeichner noch der
**Geräteverweis** schon eine Zeile führt. Die zweite Bedingung ist die belastbarere: Sie prüft
`ID_PUFFER`, also genau das, was der Index sperrt.

### 3.3  Konnte `CopyFromStamm` eine zweite Kopie erzeugen? — Nein, bei keinem Gewerk

Geprüft für alle vier: `PufferSpCtrl.cs:206`, `WPCtrl.cs:244`, `HeizkesselCtrl.cs:188`,
`BHKWCtrl.cs:253`. Alle vier prüfen zuerst `GetProjektId(Bezeichner, Projekt)` und geben bei einem
Treffer die **vorhandene** ID zurück. Eine zweite Kopie kann dieser Weg konstruktionsbedingt nicht
erzeugen; er scheitert außerdem an jedem Projektgerät, das im Katalog nicht (mehr) steht.

**Ergänzt wurde deshalb ein neuer Weg** — `AnlagenEindeutigkeit.ProjektkopieAnlegen:465`. Er
kopiert die **Projektzeile**, nicht die Katalogzeile. Das ist auch fachlich das Richtige: Das
zweite Gerät soll dem ersten gleichen, und das erste kann im Projekt längst bearbeitet worden sein
(Investitionskosten, Vor-/Rücklauf, Schwellen des Puffers) — der Katalog wüsste davon nichts. Die
Spaltenliste kommt aus der Quellzeile selbst, also braucht kein Gewerk eine eigene
INSERT-Anweisung und eine später ergänzte Spalte wandert automatisch mit. Kindtabellen werden
deklarativ mitgeführt; heute betrifft das nur die Kennlinien der Wärmepumpe (`Tab_Kenndaten`,
`Tab_Kenndaten_Kuehlung`) — ohne sie wäre die Kopie rechnerisch wertlos. Nachgewiesen in der
Verifikation (S1, „Kennlinien mitkopiert": 3 von 3).

### 3.4  Teil B — Eindeutigkeitsindex

`Allgemein/Update/SchemaMigration.cs`:

- `:72` — `ZIEL_VERSION` von 15 auf **16**.
- `:277` — `SCHRITT_16_ANLAGEN_EINDEUTIG = 16`.
- `:556` — Registrierung im Schrittregister.
- `:1859` — `Schritt_16_AnlagenEindeutigkeit`: dünne Hülle, die Arbeit steht in
  `EindeutigkeitAbschluss`.
- `:1894` — `EindeutigkeitAbschluss(Lauf, bool indizesAnlegen)`: prüft je Spalte auf Dubletten,
  meldet sie und legt bei sauberem Bestand `CREATE UNIQUE INDEX idx_Anlage_<Spalte> ON
  Tab_Energieanlagen (ID_Projekt, <Spalte>)` an.
- `:1942` — `DublettenMelden`: die Einzelzeilen mit Projekt, Gewerk, Geräte-ID, Anlagen-ID und
  Bezeichner (gedeckelt auf 40 Zeilen je Spalte).
- `:362` / `:369` — Zählwerk `DatenEindeutigIndizes` / `DatenEindeutigDubletten`.
- `:804` — Zusammenfassungszeile; sie meldet **auch die 0**, weil gerade sie die Aussage trägt.

**Warum mehrere NULL zulässig bleiben.** ACE/Jet lässt in einem eindeutigen Index mehrere NULL zu;
die Sperre greift also nur für Zeilen, die den Verweis tatsächlich führen. Darauf ist
`WizardCtrl.AnlagenParameter:218-224` ausgelegt — für nicht passende Anlagentypen schreibt es
durchgehend `DBNull`, nie 0. **In der Verifikation ausdrücklich nachgemessen** (S4: zwei Zeilen mit
NULL im selben Projekt werden angenommen).

**0 zählt als Wert.** Die Vorabprüfung filtert `IS NOT NULL`, nicht `> 0`. Ein als 0 geschriebener
Platzhalter ist für den Index ein Wert und ließe ihn scheitern — die Prüfung muss deshalb genau
das sehen, was den Index scheitern ließe, sonst meldete sie „sauber" und das `CREATE UNIQUE INDEX`
scheiterte danach doch. Solche Zeilen erscheinen im Bericht als „0 (Platzhalter statt leer)".

**Für `ID_PV`, `ID_Solar` und `ID_SP` wird kein Index angelegt.**

### 3.5  Verhalten bei unbereinigtem Bestand

Der Schritt bereinigt **nichts**. Findet er Dubletten, legt er den betroffenen Index nicht an,
nennt Projekt, Gewerk und jede betroffene Zeile im Protokoll und führt ihn als **ÜBERSPRUNGEN**.
Der Schritt liefert trotzdem `true`, der Marker wird auf 16 gesetzt — ein `false` hielte den
gesamten Migrationslauf an, obwohl nichts kaputt ist: Ohne Index verhält sich die Datenbank exakt
wie bisher.

**Nachgezogen wird über die Abschlussprüfung** (`SchemaMigration.cs:742-746`). Sie läuft bei jedem
Lauf, in dem Schritt 16 nicht mehr ausgeführt wird — also auf jeder bereits auf Stand 16 stehenden
Datenbank —, meldet erneut die verbliebenen Dubletten und legt jeden Index an, dessen Spalte
inzwischen sauber ist. Der Anwender bereinigt also, startet das Programm, und der Index steht.
Nachgewiesen in S3 (dritter Lauf: alle vier Indizes).

Auszug aus dem Migrationsprotokoll eines Laufs auf unbereinigtem Bestand:

```
Schritt 16  Anlagenzeilen-Eindeutigkeit: … : OK
        - Wärmepumpe (ID_WP): 2 Anlagenzeilen teilen sich ein Gerät mit einer anderen Zeile desselben Projekts.
        -     Projekt 1011, Wärmepumpe 1011060: Anlagenzeile 10635 "CS6800iAW MB + AW 12 OR-T"
        -     Projekt 1011, Wärmepumpe 1011060: Anlagenzeile 10642 "CS6800iAW MB + AW 12 OR-T"
        - ID_WP: Index idx_Anlage_ID_WP ÜBERSPRUNGEN - erst nach Bereinigung der oben
          genannten Zeilen anlegbar. Der nächste Programmstart zieht ihn nach.
        - Eindeutigkeitsindex idx_Anlage_ID_BHKW (BHKW): angelegt
        …
Anlagenzeilen-Eindeutigkeit (Schritt 16): 1 von 4 Eindeutigkeitsindizes aktiv,
8 doppelt belegte Anlagenzeilen - …
```

### 3.6  Teil C — Prüfung am Ende der Migration

Dieselbe Routine ist der Abschlussbericht. Das ist keine Bequemlichkeit, sondern Bedingung: Der
Bericht muss genau das sehen, was den Index scheitern ließe. Die Migration kann Dubletten selbst
erzeugen: `Regel4_AnlagenzeilenNachtragen` (`SchemaMigration.cs:2866`) und `IdPufferBereinigen`
(`:2409`) lösen den Puffer über den **Bezeichner** auf und setzen damit zwei gleichnamige Zeilen
auf dieselbe Geräte-ID. Weil die Abschlussprüfung **nach** allen Schritten läuft, sieht sie deren
Ergebnis.

---

## 4  Drei-Schichten-Regel

- **Persistenz:** keine neuen DB-Werte. Spalten- und Tabellennamen kommen aus `SchemaKatalog`
  bzw. stehen als Konstanten in `AnlagenEindeutigkeit` (`SPALTE_WP` …).
- **Schlüssel:** die Spaltennamen sind ASCII und sprachneutral; der Klartext `GeraeteSperre.Gewerk`
  ist reiner **Protokolltext** (deutsch, keine Anzeige).
- **Anzeige:** fünf neue Schlüssel in beiden `.resx` und in `Resource.Designer.cs`, nachgetragen in
  [`../Simulation/Lokalisierung_Katalog.md`](../Simulation/Lokalisierung_Katalog.md):
  `ANL_DUBLETTE_TITEL`, `ANL_DUBLETTE_FRAGE`, `ANL_DUBLETTE_KOPIE_FEHLER`, `ANL_FELD_HINWEIS`,
  `ANL_SP_NAME_ANGEPASST`.

**Engine unberührt.** `git diff --stat -- Allgemein/Simulation/` ist leer; ein Nachweis gegen
`Referenzlaeufe/2026-08-16_B4` war damit nicht erforderlich.

**Dialogfreiheit.** Rückfrage und Hinweis laufen über `AnlagenEindeutigkeit.Fragen:195` /
`Melden:212` — dieselbe Entscheidungsstelle wie `DataRepository.FehlerMelden`. Im Engine-Modus
gibt es keinen Dialog: Die Rückfrage wird still mit **Ja** beantwortet und protokolliert. „Nein"
verwürfe die Anlagenzeile — stiller Datenverlust; „Ja" behält sie, legt eine eigene Gerätekopie an
und verletzt den Index nicht. Ein unbeantwortbarer Fall darf nicht die Variante mit dem größeren
Schaden wählen.

---

## 5  Verifikation

Reflection-Harnisch (`net8.0-windows`, x86, `Assembly.LoadFrom`) gegen Wegwerf-Kopien der
Produktivdatenbank, mit Dialogwächter auf unerwartete `MessageBox`en. Die Produktivdatenbank wurde
ausschließlich gelesen. **76 Proben, 0 Fehler, 0 unerwartete Dialoge.**

| # | Szenario | Erwartung | Ergebnis |
|---|---|---|---|
| S1 | WP / Kessel / BHKW / Puffer, jeweils dasselbe Gerät zweimal, Antwort **Ja** | genau eine Rückfrage; 2 Anlagenzeilen; 2 verschiedene Geräte; Gerätekopie mit Suffix „ (2)"; keine Dublette | OK (4 Gewerke × 4 Proben) |
| S1 | dieselben vier Gewerke, Antwort **Nein** | genau eine Rückfrage; 1 Anlagenzeile; keine Gerätekopie; keine Dublette | OK (4 × 3 Proben) |
| S1 | WP: Kennlinien der Kopie | `Tab_Kenndaten` der Kopie = Original | OK (3 = 3) |
| S2 | PV bzw. Solar, gleiches Modul mit **gleicher** Neigung/Azimut/Modulanzahl | keine Sperre, beide Zeilen geschrieben, keine Rückfrage, **1** Hinweis | OK |
| S2 | PV bzw. Solar, **verschiedene** Neigung | beide Zeilen, keine Rückfrage, **0** Hinweise | OK |
| S3 | Migration auf **unbereinigter** Kopie (Stand 14, 8 Dubletten) | läuft durch; Stand 16; nur `idx_Anlage_ID_BHKW` angelegt; Bericht nennt „ÜBERSPRUNGEN", Projekt, Anlagenzeile und alle drei betroffenen Gewerke; kein Datenverlust (96 Zeilen) | OK |
| S3 | Doppelstart | idempotent; Abschlussprüfung meldet auch im zweiten Lauf | OK |
| S3 | Bestand bereinigt, dritter Lauf | alle vier Indizes werden nachgezogen | OK |
| S4 | Migration auf **bereinigter** Kopie | alle vier Indizes; Bericht meldet „je Projekt und Gerät genau eine Zeile" | OK |
| S4 | Anwendungsweg mit aktivem Index, Antwort „Ja" | 2 Zeilen, **kein** Fehlerdialog | OK |
| S4 | Doppelzeile am Anwendungsweg vorbei (direktes `INSERT`) | von der Datenbank abgewiesen: „…would create duplicate values in the index…" | OK |
| S4 | mehrere NULL je Projekt | zulässig (Grundlage des Index) | OK |
| S4 | Zeile auf ein **anderes** Gerät | erlaubt | OK |
| S5 | zwei **verschiedene** Kessel | 2 Zeilen, 2 Geräte, keine Rückfrage, kein Hinweis, kein Dialog | OK |
| S5 | Sprachen | alle fünf Schlüssel in de-DE und en-US belegt und verschieden | OK |

Build: `MSBuild WP-Plan.sln -p:Configuration=Debug -p:Platform=x86` → **0 Fehler, 6
Bestandswarnungen** (unverändert). `bin\` wurde nicht angefasst (`-p:OutDir=` in ein Scratch-Ziel).

---

## 6  Offene Punkte

1. **Bereinigung der Bestandsdubletten ist nicht entschieden** — und wurde deshalb nicht
   ausgeführt. Betroffen sind 8 Anlagenzeilen in den Projekten 1009, 1011 und 1018 (Zahlen in
   Abschnitt 1). Bis dahin bleiben `idx_Anlage_ID_WP`, `idx_Anlage_ID_Kessel` und
   `idx_Anlage_ID_PUFFER` inaktiv; die Prüfung aus Teil A greift trotzdem, denn sie hängt nicht am
   Index. Zwei Wege stehen offen: die überzähligen Zeilen entfernen (Verlust der dort gepflegten
   Betriebsparameter) oder ihnen über denselben Weg wie in Teil A eine eigene Gerätekopie geben.
   Der zweite Weg wäre ein einmaliges DML und ließe sich als Schritt 17 nachziehen.
2. **UI-Vorprüfung nur im Pufferspeicher-Dialog.** `Form_PufferSp` ist der Dialog, in dem sich
   derselbe Katalogtyp am leichtesten zweimal aufnehmen lässt (Doppelklickliste ohne
   Rückmeldung). Die Dialoge für WP, Kessel und BHKW bekommen die Rückfrage heute erst beim
   Speichern — fachlich vollständig, aber später als nötig. Nachrüstbar mit je zwei Zeilen nach
   dem Muster `Form_PufferSp.cs:109-113`.
3. **Speichervarianten werden umbenannt statt zurückgewiesen.** Auf dem Wizard-Weg steht der
   Aufruf hinter einem bereits ausgeführten `DELETE` aller Anlagenzeilen; ein Abbruch wäre
   Datenverlust. Die Namensprüfung vergibt deshalb ein Suffix und sagt es dem Anwender —
   abweichend von `StromspeicherKontextMenuCtrl.VarianteAnlegen`, das die Eingabe zurückweisen
   kann, weil dort noch nichts gelöscht ist.
4. **Der PV/Solar-Hinweis erscheint höchstens einmal je Speichervorgang.** Bei mehreren
   identischen Feldern nennt er nur das erste. Für eine Sammelmeldung fehlt heute der Ort — die
   Schleife schreibt Zeile für Zeile.

# Anlagenzeilen-Eindeutigkeit — eine Zeile je Projekt und Gerät

**Paket ANLAGENZEILEN-EINDEUTIGKEIT.** Nutzerentscheidung vom 18.08.2026: „Prüfung **und** Index".
Umgesetzt am 18.08.2026 auf `main` (Ausgangsstand 83e1b82).

**Nachtrag vom 18.08.2026 — Migrationsschritt 17** (Ausgangsstand 52f2f97): Auf die Frage, ob in
den Projekten 1009 und 1011 wirklich je zwei baugleiche Geräte stehen sollen, hat der Anwender mit
**ja** geantwortet. Die Bestandsdubletten aus Abschnitt 1 sind damit **fachlich gewollte Kaskaden**
und werden nicht gelöscht, sondern **verlustfrei überführt**: Jede überzählige Anlagenzeile bekommt
eine eigene Projektkopie ihres Geräts. Alles dazu in [Abschnitt 7](#7--schritt-17--die-bestandsdubletten-überführen).

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

*(Zeilennummern auf dem Stand nach Schritt 17; ursprünglich `:72`, `:277`, `:556`, `:1859`,
`:1894`, `:1942`, `:362`/`:369`, `:804`.)*

- `:77` — `ZIEL_VERSION` von 15 auf 16, inzwischen auf **17** (Abschnitt 7).
- `:282` — `SCHRITT_16_ANLAGEN_EINDEUTIG = 16`.
- `:624` — Registrierung im Schrittregister.
- `:1950` — `Schritt_16_AnlagenEindeutigkeit`: dünne Hülle, die Arbeit steht in
  `EindeutigkeitAbschluss`.
- `:1985` — `EindeutigkeitAbschluss(Lauf, bool indizesAnlegen)`: prüft je Spalte auf Dubletten,
  meldet sie und legt bei sauberem Bestand `CREATE UNIQUE INDEX idx_Anlage_<Spalte> ON
  Tab_Energieanlagen (ID_Projekt, <Spalte>)` an.
- `:2033` — `DublettenMelden`: die Einzelzeilen mit Projekt, Gewerk, Geräte-ID, Anlagen-ID und
  Bezeichner (gedeckelt auf 40 Zeilen je Spalte).
- `:413` / `:420` — Zählwerk `DatenEindeutigIndizes` / `DatenEindeutigDubletten`.
- `:895` — Zusammenfassungszeile; sie meldet **auch die 0**, weil gerade sie die Aussage trägt.

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

**Nachgezogen wird über die Abschlussprüfung** (`SchemaMigration.cs:822-826`). Sie läuft bei jedem
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
erzeugen: `Regel4_AnlagenzeilenNachtragen` (`SchemaMigration.cs:3146`) und `IdPufferBereinigen`
(`:2689`) lösen den Puffer über den **Bezeichner** auf und setzen damit zwei gleichnamige Zeilen
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

1. ~~**Bereinigung der Bestandsdubletten ist nicht entschieden**~~ — **erledigt am 18.08.2026 mit
   Migrationsschritt 17** (Abschnitt 7). Der Anwender hat den zweiten der beiden hier genannten
   Wege gewählt: eigene Gerätekopie statt Löschen. Betroffen waren die 8 Anlagenzeilen aus
   Abschnitt 1; 4 davon sind überzählig und wurden überführt.
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

---

## 7  Schritt 17 — die Bestandsdubletten überführen

**Nutzerentscheidung 18.08.2026: „Ja, es sollen wirklich je zwei baugleiche Geräte sein."** Damit
ist entschieden, was Abschnitt 6, Punkt 1 offengelassen hatte — und zwar zugunsten des zweiten
Weges: **überführen statt löschen**.

### 7.1  Warum überführen und nicht löschen

Die Doppelzeilen sind keine Fehlbedienung, sondern die einzige Ausdrucksform, die der Anwender
hatte. **Bis Commit 52f2f97 gab es überhaupt keinen Weg, ein zweites baugleiches Gerät sauber
anzulegen** — `CopyFromStamm` gibt bei Namensgleichheit die vorhandene Projekt-ID zurück
(Abschnitt 3.3). Wer zwei gleiche Kessel wollte, bekam zwangsläufig zwei Zeilen auf dasselbe Gerät.
Ein Löschen wäre deshalb kein Aufräumen, sondern der Verlust genau der Aussage, die getroffen
werden sollte.

### 7.2  Was der Schritt tut

`SchemaMigration.cs`:

| Zeile | Baustein | Aufgabe |
|---|---|---|
| `:77` | `ZIEL_VERSION` | 16 → **17** |
| `:328` | `SCHRITT_17_ANLAGEN_DUBLETTEN` | Nummer samt vollständiger Begründung |
| `:429` / `:437` | `DatenDublettenUeberfuehrt` / `DatenDublettenOffen` | Zählwerk |
| `:634` | Registrierung im Schrittregister | **nach** Schritt 16 |
| `:887` | Zusammenfassungszeile | meldet auch die 0 |
| `:2113` | `Schritt_17_AnlagenDubletten` | Schleife über die vier Sperren |
| `:2162` | `GruppeUeberfuehren` | eine Dublettengruppe: kleinste ID behält, Rest bekommt Kopien |
| `:2246` | `KopieVerwerfen` | Rückbau, wenn das Umhängen scheitert |

Je Dublettengruppe (ein Projekt, ein Gerät):

1. **Die Zeile mit der kleinsten ID behält das vorhandene Gerät.** Sie ist die zuerst angelegte
   und trägt den Namen ohne Suffix; jede andere Wahl benannte ausgerechnet das Gerät um, das der
   Anwender kennt.
2. Jede weitere Zeile bekommt über `AnlagenEindeutigkeit.ProjektkopieAnlegen` — **dieselbe Routine
   wie Teil A** — eine eigene Projektkopie. Kindtabellen wandern mit (heute `Tab_Kenndaten` und
   `Tab_Kenndaten_Kuehlung` der Wärmepumpe); der Spaltensatz kommt aus der Quellzeile, die Kopie
   ist also **wertgleich**.
3. Der Name kommt aus `AnlagenEindeutigkeit.EindeutigerBezeichner` — „X", „X (2)", „X (3)" …
4. `UPDATE Tab_Energieanlagen SET <Verweisspalte> = <neu>, Bezeichner = <neuer Name>`.

**Gerätekopie und Anlagenzeile tragen denselben Namen — das ist Bedingung, nicht Kosmetik.** Die
verbliebenen bezeichnerbasierten Lesepfade lösen über ihn auf:

| Fundstelle | Was passierte ohne Mitziehen des Anlagen-Bezeichners |
|---|---|
| `SimulationSPK.cs:188` (`ReadAll("Bezeichner='…' AND ID_Projekt=…")`, `items[0]`) | Die zweite Zeile zeigte auf die Kopie, läse aber die Werte des Originals — ein stiller Fehler, sobald jemand die Kopie bearbeitet |
| `PufferSpCtrl.cs:272` (`ProjektWaisenEntfernen`) | Die Pufferkopie hätte keine gleichnamige Anlagenzeile und würde beim nächsten Speichern **gelöscht** |
| `Form_Simulation_Detail.cs:1020` (`INNER JOIN … ON k.Bezeichner = a.Bezeichner`) | Der Join verlöre die Kopie |
| `Form_WP.cs:369`, `StromspeicherKontextMenuCtrl.cs:604`, `PufferSpCtrl.cs:566` | Löschsperre bzw. Namensprüfung griffen an der falschen Zeile |

**PV, Solarthermie und Stromspeicher werden nicht angefasst** — der Schritt läuft ausschließlich
über `AnlagenEindeutigkeit.SPERREN`, und dort stehen nur die vier Gerätespalten. Nachgemessen: die
Mehrfachgruppen in `ID_PV` (1), `ID_SOLAR` (1) und `ID_SP` (2) sind vorher wie nachher unverändert.

**Ein Platzhalter 0 lässt sich nicht überführen** — es gibt keine Quellzeile zum Kopieren. Solche
Zeilen bleiben stehen, werden gezählt (`DatenDublettenOffen`) und mit Projekt und Zeile gemeldet.
Im geprüften Bestand kommen sie nicht vor.

### 7.3  Reihenfolge zu Schritt 16 — beides in EINEM Lauf

Der Schritt steht **hinter** Schritt 16: Die Schrittnummer ist der Marker, eine frühere Ausführung
ließe 16 dauerhaft aus (`if (s.Nr <= version) continue`). Damit die Indizes trotzdem im **selben**
Lauf entstehen, meldet Schritt 17 die Abschlussprüfung (Teil C) wieder als offen an
(`_eindeutigkeitGeprueft = false`) — aber nur, wenn er tatsächlich etwas überführt hat, sonst
wiederholte sie bloß die Meldungen aus Schritt 16. Teil C läuft nach der Schrittschleife und legt
jeden Index an, dessen Spalte jetzt sauber ist.

Gemessen an einem Lauf von Schemastand **14 → 17** in einem Zug:

```
Schritt 16  …: OK
        - ID_WP:     Index idx_Anlage_ID_WP ÜBERSPRUNGEN …
        - ID_Kessel: Index idx_Anlage_ID_Kessel ÜBERSPRUNGEN …
        - Eindeutigkeitsindex idx_Anlage_ID_BHKW (BHKW): angelegt
        - ID_PUFFER: Index idx_Anlage_ID_PUFFER ÜBERSPRUNGEN …
Schritt 17  Doppelt belegte Anlagenzeilen in eigene Gerätekopien überführen: OK
        - Projekt 1011, Wärmepumpe, Anlagenzeile 10642 "CS6800iAW MB + AW 12 OR-T": eigene
          Gerätekopie in Tab_WP angelegt - ID_WP 1011060 -> 1672023,
          Bezeichner "CS6800iAW MB + AW 12 OR-T (2)".
        - Projekt 1009, Heizkessel, Anlagenzeile 10346 "ecoTEC plus VCI 20/26CS/1-5": …
          ID_Kessel 1009230 -> 1018329, Bezeichner "ecoTEC plus VCI 20/26CS/1-5 (2)".
        - Projekt 1011, Heizkessel, Anlagenzeile 11219 "GC7000F 22 23 - MX25": …
          ID_Kessel 1011231 -> 1018330, Bezeichner "GC7000F 22 23 - MX25 (2)".
        - Projekt 1018, Pufferspeicher, Anlagenzeile 11330 "Stora B 1000-6 ER 1 B": …
          ID_PUFFER 1054168 -> 1054170, Bezeichner "Stora B 1000-6 ER 1 B (3)".
Abschlussprüfung Anlagenzeilen-Eindeutigkeit
        - Eindeutigkeitsindex idx_Anlage_ID_WP (Wärmepumpe): angelegt
        - Eindeutigkeitsindex idx_Anlage_ID_Kessel (Heizkessel): angelegt
        - Eindeutigkeitsindex idx_Anlage_ID_BHKW (BHKW): bereits vorhanden
        - Eindeutigkeitsindex idx_Anlage_ID_PUFFER (Pufferspeicher): angelegt

Dublettenauflösung (Schritt 17): 4 Anlagenzeilen auf eine eigene Gerätekopie überführt …
Anlagenzeilen-Eindeutigkeit (Schritt 16): 4 von 4 Eindeutigkeitsindizes aktiv,
0 doppelt belegte Anlagenzeilen - je Projekt und Gerät genau eine Zeile.
```

„Stora B 1000-6 ER 1 B **(3)**" statt (2): Eine Kopie „(2)" hatte der Anwender in Projekt 1018
bereits von Hand angelegt (Puffer 1054169) — `EindeutigerBezeichner` zählt weiter.

### 7.4  Der Schritt scheitert nie hart

`Schritt_17_AnlagenDubletten` liefert immer `true` — dieselbe Begründung wie bei Schritt 16: Was
nicht überführt werden konnte, bleibt **unverändert** stehen, die Datenbank verhält sich dann exakt
wie bisher, der betroffene Index wird weiter übersprungen und die Zeilen stehen in der
Abschlussprüfung. Ein `false` hielte dagegen den ganzen Migrationslauf an und sperrte über
`SimulationGesperrt` die Simulation — für eine Datenbereinigung, ohne die alles wie zuvor
funktioniert, das falsche Mittel.

Scheitert das `UPDATE` **nach** angelegter Kopie, wird die Kopie samt Kindzeilen zurückgenommen
(`KopieVerwerfen`). Ein Gerät ohne Anlagenzeile wäre sonst eine Karteileiche, die in der
Kostenübernahme als zusätzliches Gerät erschiene.

### 7.5  Verifikation

**Aufbau (A/B auf Wegwerf-Kopien, Produktiv-DB nur gelesen).** Eine Kopie der produktiven
`Kenndaten.accdb` (18.08.2026 17:33, Schemastand 14, keine `Kenndaten.laccdb`) wurde mit dem
**Codestand 52f2f97** auf Stand 16 migriert (`DBv`, Dubletten intakt). Davon eine zweite Kopie
(`DBn`), auf die der neue Schritt 17 angewandt wurde. **Beide Läufe rechnen mit demselben Binary**
(Codestand mit Schritt 17) — es unterscheidet sich ausschließlich die Datenbank.

**Ergebnisgleichheit — 9 Projekte, 213 CSV, 2 339 848 Werte:**

| Prüfung | Ergebnis |
|---|---|
| Byte-/MD5-Vergleich `DBv` gegen `DBn`, alle 9 Projekte | **211 von 213 Dateien byte-gleich** |
| Toleranzvergleich (`vergleich`) | 7 × PASS, **2 × FAIL mit je genau 1 Abweichung** |
| Abweichung 1 | `Projekt_1009/aggregate.csv` `HeizkesselModul[1].Modul`: „ecoTEC plus VCI 20/26CS/1-5" → „… (2)" |
| Abweichung 2 | `Projekt_1011/aggregate.csv` `WaermepumpeModul[1].Modul`: „CS6800iAW MB + AW 12 OR-T" → „… (2)" |
| **Zahlenwerte** | **kein einziger** weicht ab — beide Abweichungen sind Zeichenketten |
| Projekt 1018 (Puffer-Dublette) | **alle 19 Dateien byte-gleich** — die Puffer-Anlagenzeilen bilden kein Modul im Ergebnis |
| Projekte 1007, 1008, 1017, 1021, 1023, 1024 | **alle Dateien byte-gleich** |

**Die beiden Abweichungen sind der ANZEIGENAME des zweiten Moduls, kein Rechenwert.** Er ist
`Tab_Energieanlagen.Bezeichner` (`SimulationWaermepumpe.cs:304` → `WP_Modul[i]`;
`SimulationControl.cs:1441` → `spk_list[i]`) und wandert mit der Umbenennung aus 7.2 mit. Das ist
unvermeidbar und gewollt: Ohne sie zeigte die zweite Anlagenzeile auf die Kopie und läse die Werte
des Originals, und die Pufferkopie fiele `ProjektWaisenEntfernen` zum Opfer (Tabelle in 7.2). Der
Vergleich meldet solche nichtnumerischen Werte als FAIL, weil sie exakt übereinstimmen müssen —
fachlich ist es ein geänderter **Name**, kein geändertes **Ergebnis**.

Ausdrücklich geprüft, dass die Werte **nicht** kippen, obwohl der Kessel-Lesepfad über den Namen
auflöst: In `Projekt_1009` sind `HeizkesselModul[1].Waerme_Gas`, `.Jahresnutzungsgrad`,
`.Verbrauch`, `.carrier_id` und alle vier Kessel-Ganglinien byte-gleich. Grund: Die Kopie ist
feldweise identisch (7.5, „Bestandswerte"), und `spk_carrier` (Zuordnung Name → `ID_Carrier`,
`SimulationControl.cs:669`) trägt für beide Zeilen denselben Wert — im Bestand ist `ID_Carrier`
dort ohnehin leer.

> **Ein Fall, den es hier nicht gibt, aber geben könnte:** Tragen zwei Dublettenzeilen
> **verschiedene** `ID_Carrier`, dann fasst `EnergietraegerZuordnungLesen` sie heute über
> `TryAdd(Bezeichner, …)` zu einer Zuordnung zusammen — die zweite Zeile bekommt den Energieträger
> der ersten. Nach der Umbenennung bekommt jede Zeile ihren eigenen. Das wäre eine **Korrektur**,
> würde aber `…Modul[i].carrier_id` verändern. Im geprüften Bestand ist `ID_Carrier` bei allen vier
> betroffenen Paaren leer bzw. gleich, deshalb ändert sich nichts.

**Bestandswerte unversehrt:**

| Prüfung | Ergebnis |
|---|---|
| Gerätekopien feldweise gegen ihre Quelle (20 / 23 / 23 / 16 Spalten) | abweichend **nur `ID` und `Bezeichner`** |
| Quellzeilen vorher/nachher | **0 Abweichungen** — das Original bleibt unangetastet |
| `Tab_Kenndaten` der WP-Kopie | 165 Zeilen kopiert, **825 Werte verglichen, 0 Abweichungen** |
| `Tab_Energieanlagen` | 96 → 96 Zeilen; **genau 4 Zeilen geändert** (Bezeichner + Verweisspalte) |
| Gerätetabellen | `Tab_WP` +1, `Tab_Heizkessel` +2, `Tab_Pufferspeicher` +1, `Tab_Kenndaten` +165 |
| `Tab_BHKW`, `Tab_PV`, `Tab_Solarkollektoren`, `Tab_Stromspeicher`, `Z_ProjektPufferSp`, `Tab_Projekt` | **unverändert** |

**Kostenwirkung — die beabsichtigte Korrektur.** Abfrage wie `TechnikPlanwertCtrl.LiesAnlagen`
(`GROUP BY` über die Verweisspalte):

| Projekt / Gewerk | vorher | nachher |
|---|---|---|
| **1009 / Heizkessel** (`Investitionskosten`) | 1 Gerät, **2 749,67 €** | 2 Geräte, **5 499,34 €** |
| 1011 / Heizkessel | 1 Gerät, 0,00 € | 2 Geräte, 0,00 € |
| 1011 / Wärmepumpe (`Modulkosten`) | 2 Geräte, 0,00 € | 3 Geräte, 0,00 € |
| 1018 / Pufferspeicher | 2 Geräte, 0,00 € | 3 Geräte, 0,00 € |

Projekt 1009 ist der einzige Fall mit einem gepflegten Betrag — dort ist die Wirkung in Euro
sichtbar: Die Kaskade zählt jetzt als **zwei** Kessel. Bei den übrigen drei stehen die
Investitionskosten auf 0; die Korrektur zeigt sich dort an der **Gerätezahl** und wird in Euro
wirksam, sobald ein Betrag gepflegt ist.

**Weitere Proben:**

| # | Szenario | Erwartung | Ergebnis |
|---|---|---|---|
| M1 | Lauf von Stand 14 in einem Zug | 16 überspringt drei Indizes, 17 überführt, Abschlussprüfung legt alle vier an | OK |
| M2 | Zweiter Lauf auf derselben Datenbank | „bereits erledigt", 0 überführt | OK |
| M3 | Marker von Hand auf 16 zurückgesetzt, Schritt 17 läuft **wirklich** ein zweites Mal | „Keine doppelt belegte Anlagenzeile gefunden", **keine** weitere Kopie | OK |
| M4 | Dublettengruppe mit **drei** Zeilen (eine dritte Zeile künstlich eingefügt) | kleinste ID behält, die beiden anderen bekommen „(2)" und „(3)" | OK, 5 überführt |
| M5 | Gerätename „(2)" bereits vergeben (Projekt 1018) | Kopie heißt „(3)" | OK |
| M6 | Indizes nach dem Lauf aktiv | vier zusammengesetzte eindeutige Indizes auf `Tab_Energieanlagen` | OK |
| M7 | Doppelzeile am Anwendungsweg vorbei (`INSERT`) | Datenbank weist ab | OK („…would create duplicate values in the index…") |
| M8 | Zwei Zeilen ohne Geräteverweis (NULL) im selben Projekt | zulässig | OK |
| M9 | Dialoge während der Migration | keine | OK — der Pfad kennt keine `MessageBox`: Schritt 17 ruft nur `EindeutigerBezeichner` und `ProjektkopieAnlegen`, beide über `StilleDb`; `AnlagenEindeutigkeit.Fragen`/`Melden` werden **nicht** berührt |

**Engine unberührt.** `git diff --stat -- Allgemein/Simulation/` ist leer; geändert ist
ausschließlich `Allgemein/Update/SchemaMigration.cs`.

**Build.** `MSBuild WP-Plan.sln -p:Configuration=Debug -p:Platform=x86` → **0 Fehler, 6
Bestandswarnungen** (unverändert). `bin\` wurde nicht angefasst (`-p:OutDir=` in ein Scratch-Ziel).

**Drei-Schichten-Regel.** Keine neuen Anzeigetexte und keine neuen `MyResource`-Schlüssel — der
Schritt schreibt ausschließlich ins Migrationsprotokoll (deutscher Protokolltext wie
`GeraeteSperre.Gewerk`). Persistenz: keine neuen DB-Werte; das Suffix „ (2)" ist das Muster, das
`AnlagenEindeutigkeit.EindeutigerBezeichner` seit Schritt 16 vergibt.

### 7.6  Befund am Rande: die Referenzbasis 2026-08-16_B4 ist überholt

Der Vergleich beider Läufe gegen die eingefrorene Basis `2026-08-16_B4` meldet in 6 von 8 Projekten
FAIL — **vorher wie nachher identisch**:

```
                       B4 gegen VORHER        B4 gegen NACHHER
Projekt_1007           FAIL (50275)           FAIL (50275)
Projekt_1008           PASS                   PASS
Projekt_1011           FAIL (43805)           FAIL (43806)
Projekt_1017           FAIL (45898)           FAIL (45898)
Projekt_1018           FAIL (74147)           FAIL (74147)
Projekt_1021           PASS                   PASS
Projekt_1023           FAIL     (1)           FAIL     (1)
Projekt_1024           FAIL (56873)           FAIL (56873)
```

**Schritt 17 fügt über die gesamte Basis genau eine Abweichung hinzu** — die eine in Projekt 1011
(43805 → 43806), den Modulnamen. Alles andere steht schon vorher so und stammt aus der Drift
zwischen dem 16.08. und heute: geänderte Projektdaten (in 1018 ein anderes BHKW und ein Modul
weniger, in 1024 ein zusätzlicher Pufferspeicher, geleerte `ID_Carrier` in 1017/1023/1024) und die
Codestände dazwischen (BHKW-Regulär, Parallelverbund, Kessel-Wartungseinheit — der Stromspeicher
rechnet jetzt in 1007/1011/1017 mit).

**Folge:** `B4` taugt nicht mehr als Regressionsbasis für den aktuellen Stand. Vor dem nächsten
Engine-Eingriff sollte eine neue Basis eingefroren werden — sinnvollerweise **nach** dem Einspielen
von Schritt 17, damit sie die endgültigen Modulnamen trägt. Für dieses Paket war sie nicht nötig:
Der A/B-Vergleich auf **derselben** Quelldatenbank mit **demselben** Binary isoliert die Wirkung
von Schritt 17 exakt und ist die belastbarere Probe.

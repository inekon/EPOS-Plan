# Stammdatensätze änderbar machen — Analyse und Vorgehensweise

Stand: 19.08.2026. Betrachtet wurde der gesamte Bestand unter `WindowsFormsApplication1`
(ohne Altkopien und Worktrees).

**Aufgabe.** Die Stammdatensätze der Auslieferungskataloge (BHKW, Heizkessel, Wärmepumpe,
Photovoltaik, Pufferspeicher, Solarkollektoren, Stromspeicher, …) sollen vom Anwender
**geändert** werden können. Das **Löschen** bleibt bei `ReadOnly = TRUE` **gesperrt**.

Dieses Dokument ist reine Analyse und Planung — es wurde noch keine Zeile Code geändert.

---

## 1. Kurzfassung des Befunds

`ReadOnly` ist heute ein **kombinierter Schreib- und Löschschutz**. Er ist an
**41 Stellen** von Hand ausprogrammiert: 17 Schreibsperren und 24 Löschsperren, verteilt
über 13 Controller und 12 Dialoge. Eine gemeinsame Wache gibt es nicht.

Für die Aufgabe sind genau die **17 Schreibsperren** zu entfernen bzw. in eine Rückfrage
umzuwandeln; die 24 Löschsperren bleiben unverändert stehen.

**Das Muster dafür existiert bereits** — im BHKW-Zweig, eingeführt am 18.08.2026:
`BHKWStammCtrl.SchreibschutzUebergehen` plus die bereits zweisprachig gepflegten
Ressourcen `ADM_SCHUTZ_FRAGE` / `ADM_SCHUTZ_TITEL`. BHKW ist damit das einzige Gewerk,
das die geforderte Zielsemantik schon hat. Die Aufgabe reduziert sich im Kern darauf,
dieses Muster auf die übrigen zwölf Kataloge zu ziehen und dabei zu vereinheitlichen.

**Zwei Punkte müssen vor der Umsetzung entschieden werden** (Abschnitt 5):
die Migrations-Kollision (Abschnitt 4.1 — Anwenderänderungen an Katalogsätzen gehen
beim nächsten Datenbank-Update verloren) und der Ort der Rückfrage.

---

## 2. Wie der Schutz heute funktioniert

### 2.1 Zwei Schichten, ungleich verteilt

| Schicht | Rolle | Verlässlichkeit |
|---|---|---|
| **Controller** (`Controller/*StammCtrl.cs`) | prüft `SELECT ReadOnly … WHERE Bezeichner = ?` unmittelbar vor `UPDATE` bzw. `DELETE`, meldet per `MessageBox` und liefert `false` | greift immer, auch wenn die UI-Prüfung fehlt |
| **Dialog** (`Views/**/Form_*.cs`) | verhindert den Aufruf vorab, damit die Meldung früher und sprechender kommt | lückenhaft — nur bei einem Teil der Dialoge vorhanden |

Die Doppelung ist gewollt und dokumentiert (`Form_BHKWAdmin.cs:280` — „prüft ReadOnly
erneut"). Sie bedeutet aber, dass eine Lockerung **an beiden Schichten** erfolgen muss:
Wird nur der Controller geöffnet, meldet weiterhin der Dialog; wird nur der Dialog
geöffnet, meldet weiterhin der Controller.

### 2.2 Der Präzedenzfall BHKW (Zielmuster)

```
Controller/BHKWStammCtrl.cs:157   public bool SchreibschutzUebergehen = false;
Controller/BHKWStammCtrl.cs:165   if (!SchreibschutzUebergehen && DBCommand.Connection == null && IsReadOnly(...))
Views/BHKW/Form_BHKWAdmin.cs:426  if (m.m_bReadOnly) { … ADM_SCHUTZ_FRAGE … }
Views/BHKW/Form_BHKWAdmin.cs:437  schreiber.SchreibschutzUebergehen = true;
Views/BHKW/Form_DBBHKW.cs:147     ctrl.SchreibschutzUebergehen = true;   (Knopf "Überschreiben")
```

Semantik: Der Dialog fragt **einmal** ausdrücklich nach und hebt den Schutz **nur für
genau diesen einen Schreibvorgang** auf. `Delete` bleibt hart gesperrt
(`BHKWStammCtrl.cs:244`, zusätzlich UI-seitig `Form_BHKWAdmin.cs:270`,
`Form_BHKWEing.cs:792`). Der Kommentar in `Form_BHKWAdmin.cs:423` hält fest, dass in der
Auslieferungsdatenbank **alle** Sätze von `Tab_BHKW_STAMM` geschützt sind — die Rückfrage
ist dort also nicht die Ausnahme, sondern der Regelfall.

Die Texte liegen bereits übersetzt vor:

```
MyResource/Resource.resx:4741        ADM_SCHUTZ_TITEL  "Schreibgeschützter Datensatz"
MyResource/Resource.resx:4744        ADM_SCHUTZ_FRAGE  "Der Datensatz ... stammt aus dem
                                     Auslieferungskatalog und ist schreibgeschützt. ..."
MyResource/Resource.en-US.resx:4726  dieselben Schlüssel auf Englisch
```

`Form_DBBHKW.cs:147` verwendet allerdings noch **hart kodierte** deutsche Literale
statt dieser Ressourcen — beim Vereinheitlichen mit erledigen.

---

## 3. Vollständiges Inventar der Sperren

### 3.1 Schreibsperren — **diese 17 Stellen sind Gegenstand der Aufgabe**

| # | Gewerk / Katalog | Schreib-Einstieg | Sperre | Aufrufender Dialog |
|---|---|---|---|---|
| 1 | BHKW · `Tab_BHKW_STAMM` | `BHKWStammCtrl.Update()` | `BHKWStammCtrl.cs:165` **(bereits übergehbar)** | `Form_BHKWAdmin.cs:440`, `Form_DBBHKW.cs:151` |
| 2 | Heizkessel · `Tab_Heizkessel_STAMM` | `HeizkesselStammCtrl.Update()` | `HeizkesselStammCtrl.cs:271` | `Form_Heizkessel_Admin.cs:365`, `Form_Heizkessel_Bearbeiten.cs:323` |
| 3 | Wärmepumpe · `Tab_WP_STAMM` | `WPStammCtrl.Update()` | `WPStammCtrl.cs:83` | `Form_WP.cs:332` |
| 4 | Wärmepumpe (UI-Vorsperre) | — | `Form_WP.cs:299` | `Form_WP.cs` Knopf "Speichern" |
| 5 | Wärmepumpe · Kennlinien | `Tab_Kenndaten_STAMM` | `Form_WP.cs:402` ("nur ansehen") | `Form_WP.cs` Knopf "Kenndaten" |
| 6 | Photovoltaik · `Tab_PV_STAMM` | `PhotovoltaikStammCtrl.Update(szKey)` | `PhotovoltaikStammCtrl.cs:124` | `Form_AdminPV.cs:57` |
| 7 | Pufferspeicher · `Tab_Pufferspeicher_STAMM` | `PufferSpStammCtrl.Update()` / `UpdateFrom(m)` | `PufferSpStammCtrl.cs:115` | `Form_PufferSp_Bearbeiten.cs:322` |
| 8 | Solarkollektoren · `Tab_Solarkollektoren_STAMM` | `SolarkollektorenStammCtrl.UpdateFrom(m)` | `SolarkollektorenStammCtrl.cs:116` | `Form_SolarDB.cs:115` |
| 9 | Stromspeicher · `Tab_Stromspeicher_STAMM` | `StromspeicherStammCtrl.Update(szKey)` | `StromspeicherStammCtrl.cs:112` | `Form_AdminStromspeicher.cs:97` |
| 10 | Klimaregion · `Tab_Klimaregion_STAMM` | `KlimaregionStammCtrl.Update()` | `KlimaregionStammCtrl.cs:127` | `Form_Klimadaten.cs` |
| 11 | Gebäude · `Tab_Gebaeude_STAMM` | `GebaeudeStammCtrl.Overwrite(m)` | `GebaeudeStammCtrl.cs:225` | `Form_Gebaeude.cs`, `Form_Gebaeude1.cs` |
| 12 | Brauchwasser (Kopf) | `BrauchwasserStammCtrl.SaveHead(…, isNew: false)` | `BrauchwasserStammCtrl.cs:256` | `Form_EingDBBrauchwasser.cs` |
| 13 | Brauchwasser (Typ-Profil) | UI-seitiges `UPDATE` | `Form_EingBrauchwasserTyp.cs:143` | `Form_EingBrauchwasserTyp.cs` Knopf "Speichern" |
| 14 | Stromverbraucher (Kopf) | `StromverbraucherStammCtrl.SaveHead(…, isNew: false)` | `StromverbraucherStammCtrl.cs:254` | `Form_EingDBStromverbraucher.cs` |
| 15 | Stromverbraucher (Typ-Profil) | UI-seitiges `UPDATE` | `Form_EingStromTyp.cs:182` | `Form_EingStromTyp.cs` Knopf "Speichern" |
| 16 | Prozesswärme (Kopf) | **`UPDATE` direkt im Dialog**, kein Controller-Weg | `Form_EingDBProzess.cs:88` | `Form_EingDBProzess.cs` Knopf "Überschreiben" |
| 17 | Prozesswärme (Typ-Profil) | UI-seitiges `UPDATE` | `Form_EingProzTyp.cs:171` | `Form_EingProzTyp.cs` Knopf "Speichern" |

Sonderfall ohne Meldung: `Form_EingGebTyp.cs:88` gibt die Bearbeitung des Gebäudetyps
(`Tab_DBTagV_STAMM`) nur frei, wenn `Veraenderbar && !ReadOnly` — siehe 3.3.

### 3.2 Löschsperren — **bleiben unverändert (24 Stellen)**

Controller: `BHKWStammCtrl.cs:244` · `BrauchwasserStammCtrl.cs:84` und `:300` (Typ) ·
`GebaeudeStammCtrl.cs:68` · `HeizkesselStammCtrl.cs:315` · `KlimaregionStammCtrl.cs:147` ·
`PhotovoltaikStammCtrl.cs:163` · `ProzesswaermeStammCtrl.cs:83` · `PufferSpStammCtrl.cs:141` ·
`SolarganglinieStammCtrl.cs:67` · `SolarkollektorenStammCtrl.cs:151` ·
`StromganglinieStammCtrl.cs:66` · `StromspeicherStammCtrl.cs:150` ·
`StromverbraucherStammCtrl.cs:84` und `:298` (Typ) · `WaermebedarfStammCtrl.cs:64` ·
`WPStammCtrl.cs:116`

Dialoge: `Form_BHKWAdmin.cs:270` · `Form_BHKWEing.cs:792` · `Form_EingGebTyp.cs:317` ·
`Form_EingProzTyp.cs:257` · `Form_Klimadaten.cs:139` · `Form_Stromganglinie_Admin.cs:68` ·
`Form_WP.cs:356`

**Diese Liste ist die Abnahme-Checkliste:** nach der Umsetzung muss jede dieser 24 Stellen
unverändert und wirksam sein.

### 3.3 Kataloge ohne Schreibweg — hier ist nichts zu tun

- **Ganglinien** (`Tab_Stromganglinie_STAMM`, `Tab_Solarganglinie_STAMM`,
  `Tab_Waermebedarf_STAMM`): kennen nur `Insert` + `Delete`. Geändert wird durch
  Neu-Einlesen. Kein `Update`, also keine Schreibsperre.
- **Gebäudetyp / Tagesverlauf** (`Tab_DBTagV_STAMM`): führt ein **zweites** Kennzeichen
  `Veraenderbar` neben `ReadOnly` (`Controller/TagVCtrl.cs:43`, Umlaut-Fallback auf
  `Veränderbar` in `:47`). `Form_EingGebTyp.cs:88` verlangt beide. Ob dieser Katalog zur
  Aufgabe gehört, ist zu klären — er folgt einer eigenen Logik.
- **Importwege** (VDI 3805, CEC/PAN): schreiben ausschließlich per `Insert`/`InsertFrom`
  (`Form_Heizkessel_einlesen.cs:273`, `Form_WP_einlesen.cs:254`,
  `Form_SolarKollektoren_einlesen.cs:234`, `Form_PufferSp_einlesen.cs:237`,
  `Form_CECImport.cs:462`). **Wichtig:** eine Rückfrage im Update-Pfad kann dort also
  keine Massen-Dialoge auslösen. Das ist geprüft, nicht angenommen.

### 3.4 Angrenzend, aber ausdrücklich nicht betroffen

`Allgemein/KI/KiSchreibschutz.cs` ist die Wache des KI-Assistenten. Sie weist
Katalogtabellen (Endung `_STAMM`) **pauschal** ab und prüft zusätzlich `ReadOnly` je Satz;
`SchreibschutzUebergehen` wird vom Assistenten nirgends gesetzt — das ist im
Klassenkommentar (`KiSchreibschutz.cs:13`) als Zusage festgehalten.

**Diese Zusage bleibt gültig und darf nicht mitgelockert werden.** Was der Anwender im
Fachdialog nach Rückfrage darf, darf der Assistent weiterhin nicht. Bei der Umsetzung ist
darauf zu achten, dass eine zentrale Wache (Variante C unten) nicht versehentlich auch
von `KiSchreibschutz` verwendet wird.

---

## 4. Was die Lockerung auslöst

### 4.1 Migrations-Kollision — der wichtigste Punkt

`CLAUDE.md` der Repo-Wurzel hält fest:

> Das Feld `ReadOnly` in den `_STAMM`-Tabellen bedeutet faktisch „gehört zur Auslieferung":
> Das Migrationsskript behält `ReadOnly = TRUE` aus der Vorlage und ersetzt alles Übrige
> durch die Anwenderdaten.

Daraus folgt unmittelbar: **Ändert der Anwender einen Satz mit `ReadOnly = TRUE`, wird
seine Änderung beim nächsten Datenbank-Update wieder durch den Vorlagenstand ersetzt.**
Die Änderung ist möglich, aber nicht haltbar.

Das ist kein Nebeneffekt, sondern die eigentliche Konsequenz der Aufgabe: `ReadOnly`
trägt heute *zwei* Bedeutungen — „nicht löschbar" **und** „gehört der Vorlage, wird bei
der Migration überschrieben". Die Aufgabe trennt nur die erste ab; die zweite bleibt und
wirkt weiter.

Vier Wege (Entscheidung siehe 5.1):

| Weg | Wirkung | Kosten |
|---|---|---|
| **a) Hinweis in der Rückfrage** | Anwender weiß, dass die Änderung ein Datenbank-Update nicht übersteht | gering — ein Satz im Ressourcentext |
| **b) `ReadOnly` beim Überschreiben auf `FALSE` setzen** | Satz wird zum Anwendersatz, übersteht die Migration | **verletzt die Aufgabe** — der Satz wäre danach löschbar |
| **c) Drittes Kennzeichen `Geaendert`** | `ReadOnly` bleibt reiner Löschschutz, Migration erkennt geänderte Sätze und behält sie | Schemaänderung + Anpassung der separaten Migrations-Anwendung |
| **d) "Speichern unter" bewerben** | Anwender legt eine Kopie als eigenen Satz an | keine — der Weg existiert schon, aber nur in vier Dialogen |

Empfehlung: **(a) sofort, (c) als Folgepaket.** (b) scheidet aus, (d) ist Ergänzung.

### 4.2 `ReadOnly` wird beim Überschreiben nicht angetastet — geprüft

Kein einziges `UPDATE`-Statement in den `*StammCtrl.cs` enthält die Spalte `ReadOnly`
(nachgesehen in allen 15 Controllern). Das Flag bleibt beim Überschreiben also von selbst
stehen, und der **Löschschutz bleibt automatisch erhalten**. Für Weg (a) ist keinerlei
zusätzliche Absicherung nötig.

### 4.3 Weitere Konsequenzen

- **Kein Weg zurück.** Ist ein Auslieferungssatz einmal überschrieben, gibt es in der
  Anwendung keine Funktion, die den Originalstand wiederherstellt. Ein
  „Auslieferungsstand wiederherstellen" fehlt und wäre ein sinnvolles Folgepaket
  (der Originalstand liegt in der Vorlagendatenbank vor).
- **Bezeichner als Schlüssel.** Die Kataloge verknüpfen über Textfelder, nicht über IDs.
  `Form_Heizkessel_Admin.cs:336` fängt das bereits ab: Bei doppeltem Bezeichner würde ein
  `UPDATE … WHERE Bezeichner = ?` **zwei Sätze zugleich** überschreiben, deshalb bricht
  der Dialog mit `ADM_MEHRDEUTIG_TEXT` ab. Diese Prüfung existiert **nur bei Heizkessel**.
  Mit deutlich mehr Schreibvorgängen auf den Katalogen steigt das Risiko — die Prüfung
  gehört auf alle namensgeschlüsselten Kataloge ausgeweitet.
- **Umbenennen.** `PhotovoltaikStammCtrl.Update(szKey)` und
  `StromspeicherStammCtrl.Update(szKey)` erlauben, den Bezeichner mitzuändern. Wird ein
  Auslieferungssatz umbenannt, findet die Migration ihn nicht mehr wieder und legt den
  Original-Bezeichner zusätzlich neu an — aus einem Satz werden zwei. Umbenennen von
  `ReadOnly`-Sätzen sollte gesperrt bleiben, auch wenn Ändern erlaubt wird.
- **Kein Sicherungspunkt.** Vor dem ersten Überschreiben eines Auslieferungssatzes gibt es
  keine automatische Sicherung der `.accdb`.

### 4.4 Sichtbarkeit im Bestand

Nur **ein** Dialog macht geschützte Sätze überhaupt erkennbar: `Form_WP.cs:105` stellt sie
in der Liste grau dar. In allen übrigen Dialogen erfährt der Anwender vom Schutz erst
beim Speicherversuch. Wenn Ändern künftig erlaubt ist, ist eine Markierung wichtiger als
vorher — der Anwender muss vor dem Tippen sehen, dass er an der Auslieferung arbeitet.

---

## 5. Zu entscheiden, bevor programmiert wird

### 5.1 Umgang mit der Migrations-Kollision (Abschnitt 4.1)

Vorschlag: **Weg (a)** — Rückfrage mit Hinweis, dass die Änderung beim nächsten
Datenbank-Update verloren geht, dazu der Verweis auf "Speichern unter". **Weg (c)** als
eigenes Folgepaket, weil er die separate Migrations-Anwendung berührt.

### 5.2 Wo wird gefragt?

| Variante | Beschreibung | Bewertung |
|---|---|---|
| **A — ersatzlos streichen** | Schreibsperren raus, `UPDATE` läuft kommentarlos durch | wenigste Arbeit, aber der Anwender merkt nicht, dass er die Auslieferung verändert; angesichts 4.1 zu riskant |
| **B — BHKW-Muster kopieren** | je Controller ein `SchreibschutzUebergehen`, je Dialog eine Rückfrage | folgt dem Bestand, aber **12 Dialoge** sind anzufassen und die Rückfrage wird zwölfmal nachgebaut |
| **C — zentrale Wache** | eine neue Klasse `StammSchreibschutz` mit `DarfUeberschreiben(tabelle, bezeichner)`; die Controller ersetzen ihren Sperrblock durch diesen einen Aufruf | Rückfrage genau einmal formuliert, Dialoge bleiben unangetastet, Sperre bleibt an der Stelle, die sie heute schon hat |

Vorschlag: **C.** Die `MessageBox` bleibt damit im Controller — das ist keine neue
Freiheit, sondern genau das, was heute an allen 17 Stellen schon geschieht. Der Gewinn:
ein Text, ein Verhalten, und die zwölf Dialoge müssen nicht angefasst werden. BHKW behält
seine Dialog-Rückfrage zusätzlich (dort wird sie *vor* dem Schreiben gestellt, was besser
ist) — `SchreibschutzUebergehen` bleibt und unterdrückt dann die zweite Frage.

### 5.3 Randfragen

1. Gehört **`Tab_DBTagV_STAMM`** (Gebäudetyp, Sonderlogik `Veraenderbar`, Abschnitt 3.3)
   zur Aufgabe?
2. Sollen die **Typ-Profile** (Brauchwasser, Stromverbraucher, Prozesswärme — die
   Wochen-Stundenprofile mit 168 Werten) ebenso änderbar werden wie die Kopfsätze, oder
   zunächst nur die Kopfsätze?
3. Soll das **Umbenennen** eines `ReadOnly`-Satzes gesperrt bleiben (Empfehlung: ja,
   Begründung 4.3)?
4. Sollen die **Wärmepumpen-Kennlinien** (`Tab_Kenndaten_STAMM`, heute "nur ansehen",
   `Form_WP.cs:402`) mit geöffnet werden? Das ist ein eigener Editor mit Datenmengen,
   nicht nur ein Feld.

---

## 6. Vorgehensweise

Sechs Pakete. P1–P3 liefern die Aufgabe vollständig; P4–P6 sind die Absicherung, die die
Lockerung erst gutartig macht.

### P1 — Zentrale Wache anlegen *(Grundlage)*

- Neue Klasse `Allgemein/StammSchreibschutz.cs`:
  - `static bool IstGeschuetzt(string tabelle, string schluesselSpalte, string wert)`
  - `static bool UeberschreibenBestaetigt(string tabelle, string schluesselSpalte, string wert)`
    — liefert `true`, wenn nicht geschützt **oder** der Anwender die Rückfrage bejaht.
- Rückfrage über `ADM_SCHUTZ_FRAGE` / `ADM_SCHUTZ_TITEL`, `MessageBoxDefaultButton.Button2`
  (Vorbelegung "Nein"), wie in `Form_BHKWAdmin.cs:432`.
- Ressourcentexte um den Migrationshinweis aus 5.1 ergänzen — **beide** Dateien
  `MyResource/Resource.resx` und `Resource.en-US.resx`.
- **Nicht** von `KiSchreibschutz` verwenden (Abschnitt 3.4).

### P2 — Schreibsperren umstellen *(die eigentliche Aufgabe)*

Die 17 Stellen aus 3.1 der Reihe nach: den Sperrblock durch
`if (!StammSchreibschutz.UeberschreibenBestaetigt(...)) return false;` ersetzen.
Reihenfolge nach Aufwand und Risiko:

1. **Heizkessel, Photovoltaik, Pufferspeicher, Solarkollektoren, Stromspeicher, Wärmepumpe** —
   je ein Controller-Block, mechanisch. Bei der Wärmepumpe zusätzlich die UI-Vorsperre
   `Form_WP.cs:299` entfernen (sonst greift der Controller-Weg nie).
2. **Klimaregion, Gebäude** — dito, abweichende Methodennamen (`Update()`, `Overwrite(m)`).
3. **Brauchwasser, Stromverbraucher** — Sperre sitzt im `else`-Zweig von `SaveHead(…)`;
   der `isNew`-Pfad ist nicht betroffen und bleibt unverändert.
4. **Prozesswärme (Kopf)** — Sonderfall: das `UPDATE` steht im Dialog
   (`Form_EingDBProzess.cs:82–100`), nicht im Controller. Entweder die Wache dort direkt
   aufrufen oder — sauberer — vorher einen `Update`-Weg in `ProzesswaermeStammCtrl`
   nachziehen, analog zu `SaveHead` der Nachbargewerke.
5. **Typ-Profile** (`Form_EingBrauchwasserTyp.cs:143`, `Form_EingStromTyp.cs:182`,
   `Form_EingProzTyp.cs:171`) — nur nach Klärung von Randfrage 5.3.2.
6. **BHKW** — auf die zentrale Wache umstellen und die hart kodierten Literale in
   `Form_DBBHKW.cs:147` durch die Ressourcen ersetzen. Fachlich ändert sich nichts.

**Die 24 Löschsperren aus 3.2 werden dabei nicht angefasst.**

### P3 — Abnahme

Je Gewerk beide Fälle prüfen (Prüfliste in Abschnitt 7).

### P4 — Bezeichner-Eindeutigkeit ausweiten *(Absicherung, Begründung 4.3)*

Die Dublettenprüfung aus `Form_Heizkessel_Admin.cs:336` in die zentrale Wache ziehen, so
dass sie für alle namensgeschlüsselten Kataloge gilt. Text `ADM_MEHRDEUTIG_TEXT` liegt
zweisprachig vor. Umbenennen von `ReadOnly`-Sätzen hier mit sperren (Randfrage 5.3.3).

### P5 — Sichtbarkeit *(Absicherung, Begründung 4.4)*

Das Graustellen-Muster aus `Form_WP.cs:105` auf die übrigen Katalogdialoge übertragen,
oder — schlichter und ohne Owner-Draw — eine Statuszeile "Auslieferungsdatensatz" im
Detailbereich. Betrifft nur die Anzeige, keine Persistenz.

### P6 — Migration ehrlich machen *(Folgepaket, Begründung 4.1 Weg c)*

Kennzeichen `Geaendert` in den `_STAMM`-Tabellen, gesetzt beim bestätigten Überschreiben;
die separate Migrations-Anwendung behält solche Sätze. Erst damit sind Anwenderänderungen
an Katalogsätzen dauerhaft. Berührt eine fremde Anwendung — eigenes Paket, eigene Abstimmung.

---

## 7. Prüfliste für die Abnahme

Je Gewerk aus Abschnitt 3.1, an einem Satz mit `ReadOnly = TRUE`:

| # | Prüfung | Erwartung |
|---|---|---|
| 1 | Satz öffnen, Feld ändern, "Speichern"/"Überschreiben" | Rückfrage erscheint |
| 2 | Rückfrage mit **Nein** beantworten | nichts geschrieben, Dialog bleibt offen, alter Wert steht noch in der Datenbank |
| 3 | Rückfrage mit **Ja** beantworten | Wert steht in der Datenbank |
| 4 | Denselben Satz danach löschen | **weiterhin abgelehnt** — `ReadOnly` steht noch auf `TRUE` |
| 5 | Satz mit `ReadOnly = FALSE` ändern | **keine** Rückfrage (Regressionsprüfung: die Lockerung darf keine neue Frage bei Anwenderdaten erzeugen) |
| 6 | Satz mit `ReadOnly = FALSE` löschen | geht wie bisher |
| 7 | VDI-3805-/CEC-Import mit mehreren Sätzen | **keine** Rückfrage je Satz (Insert-Weg, siehe 3.3) |
| 8 | KI-Assistent auf einen `_STAMM`-Satz ansetzen | **weiterhin abgelehnt** (`KiSchreibschutz`, siehe 3.4) |

Zusätzlich einmalig: Anwendung auf Englisch starten und Rückfrage prüfen — die Texte
liegen in beiden Sprachen vor, die Fundstelle in `Form_DBBHKW.cs:147` ist es heute nicht.

**Vor der Abnahme:** `Kenndaten.laccdb` prüfen und eine datierte Kopie der `.accdb` nach
`DB-Backup/` anlegen (Regel aus `CLAUDE.md` der Wurzel). Die Prüfschritte 3 und 4 verändern
die Datenbank, und `.accdb` ist von `.gitignore` ausgeschlossen — ein Rückweg über Git
existiert nicht.

---

## 8. Aufwand

| Paket | Umfang | Aufwand |
|---|---|---|
| P1 zentrale Wache | 1 neue Datei, 2 `.resx` | 2–3 h |
| P2 Schreibsperren | 11 Controller, 4–6 Dialoge | 6–10 h |
| P3 Abnahme | 13 Gewerke × 8 Prüfungen | 4–6 h |
| P4 Eindeutigkeit | Wache + Prüfung | 3–4 h |
| P5 Sichtbarkeit | je Dialog klein | 4–8 h |
| P6 Migration | Schema + fremde Anwendung | separat zu schätzen |

**Aufgabe erfüllt nach P1–P3: rund 12–19 h.** P4/P5 empfohlen im selben Zug, P6 danach.

---

## 9. Fallstricke bei der Umsetzung

- **Kodierung.** `BHKWStammCtrl.cs`, `KlimaregionStammCtrl.cs`, `Form_DBBHKW.cs`,
  `Form_Stromganglinie_Admin.cs`, `Form_EingGebTyp.cs` und weitere sind **nicht UTF-8**
  (erkennbar an den Ersatzzeichen in ihren Meldungstexten). Beim Bearbeiten die
  vorhandene Kodierung beibehalten, sonst zerschießt der Diff die Datei
  (`WindowsFormsApplication1/CLAUDE.md`, Abschnitt „Fallstricke").
- **Nur in `WindowsFormsApplication1` suchen.** `..\WindowsFormsApplication1 - Kopie` und
  `..\mit_Puffer_KI_Lösungsversuch` enthalten fast identische Dateinamen — Treffer daraus
  führen zu Änderungen am falschen Code.
- **Beide Schichten.** Wird nur der Controller geöffnet, meldet weiterhin der Dialog
  (`Form_WP.cs:299` ist der klarste Fall). Die Paare aus 3.1 gehören zusammen.
- **`Form_DBBHKW` schreibt in einer Transaktion.** `BHKWStammCtrl.cs:165` überspringt die
  Prüfung, solange `DBCommand.Connection != null`. Diese Bedingung muss in die zentrale
  Wache mitgenommen werden, sonst erscheint die Rückfrage mitten in einer offenen
  Transaktion — mit einer `MessageBox` auf einer gesperrten Datenbank.
- **Texte über `MyResource.Resource.*`.** Die Drei-Schichten-Regel gilt: `ReadOnly` und die
  Tabellennamen bleiben deutsch und eingefroren (Persistenz), die Rückfrage ist Anzeige
  und gehört in beide `.resx`.
- **Designer-Dateien nicht von Hand editieren.** P5 berührt Anzeigelogik; falls dabei
  Steuerelemente hinzukommen, über den WinForms-Designer.

---

## 10. Verwandte Dokumente

- [`CLAUDE.md`](CLAUDE.md) — Bedeutung von `ReadOnly`, Migration, Umgang mit der Datenbank
- [`WindowsFormsApplication1/CLAUDE.md`](WindowsFormsApplication1/CLAUDE.md) — Architektur,
  Kodierungs-Fallstrick, Drei-Schichten-Regel für Texte
- [`KONTEXT_Brauchwassertypen_VDI6002.md`](KONTEXT_Brauchwassertypen_VDI6002.md) — Datenmodell
  der Brauchwasser-Typprofile (betrifft Zeilen 12/13 aus 3.1)
- `WindowsFormsApplication1/Allgemein/KI/KiSchreibschutz.cs` — die Wache des Assistenten,
  bleibt unverändert

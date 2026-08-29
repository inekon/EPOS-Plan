# Projektdialoge vereinheitlichen — Analyse und Vorgehensweise

Stand: 29.08.2026. Betrachtet wurde der Bestand unter `WindowsFormsApplication1`
(ohne Worktrees) auf Basis einer vollständigen Agenten-Inventur der Projekt-Einstiege
(Wizard, Startmasken-Kacheln, Menü, `FormMain`) samt Wiki-Soll-Abgleich.

**Aufgabe (Nutzerwunsch, 2 Screenshots):** Den Projekt-Wizard optimieren und sämtliche
Projekt-Dialogaufrufe der Startmaske strukturell **vereinheitlichen und besser
bedienbar** machen.

**Verbindliche Vorgabe:** Der **WinForms-Designer bleibt das Pflegewerkzeug** — alle
neuen und umgebauten Oberflächen entstehen als Designer-bearbeitbare
`Form`/`UserControl` mit `.Designer.cs` und Satelliten-`.resx`; vollständig
code-gebaute Oberflächen sind ausdrücklich **nicht** das Muster. Programmatische
Ergänzungen ohne Layoutwirkung (Muster `InfoKnopf`) bleiben zulässig.

Dieses Dokument ist reine Analyse und Planung — es wurde keine Zeile Code geändert.

---

## 1. Kurzfassung des Befunds

Es existieren **drei Oberflächenwelten** für dieselben Projektaufgaben: der alte
Wizard (hellblaue Spalte, Häkchenbatterie, INEKON-Logo), die moderne Kachel-Startmaske
und das Detailformular `FormMain` — mit Doppelungen, toten Datenpfaden und drei
verschiedenen Bedienmustern für dieselbe Information.

Die vier gewichtigsten Einzelbefunde:

1. **Menü „Projekt → Öffnen…" öffnet nicht — es dupliziert.** Der Menüpunkt zeigt
   `Form_ProjektSpeichernUnter`; deren OK verlangt einen neuen Namen und **kopiert**
   das Projekt, danach öffnet `MenueCtrl` das *Ausgangs*projekt (`MenueCtrl.cs:83,91`).
   Spuren der Umwidmung: Handler heißt noch `Form_ProjektOpen_Load`, die `en-US.resx`
   trägt noch „Open Project". Direkt daneben liest `MenueCtrl.cs:158` ein garantiert
   leeres Feld vom falschen Objekt.
2. **Die elf Komponenten-Häkchen des Wizards werden nirgends gespeichert** — sie
   setzen nur `WizardItemClass.aktiv`, alle zehn Getter sind tot
   (`Wizard_Komponenten.cs:106–192`). Ihre einzige echte Wirkung ist destruktiv: Ein
   **abgewähltes** Häkchen **löscht** im Bearbeiten-Modus kommentarlos die
   zugehörigen Anlagen (`entferne_nicht_aktive_elemente`, `:724–739`) — der Kessel
   fehlt in dieser Löschroutine. Dieselbe Information lebt an drei Orten mit drei
   Mustern: Häkchen (Wizard), Kachel-Bitmaske `status` (`Form_Start.cs:1648–1709`),
   `ListView` „Konfiguration Projekt" (`FormMain`).
3. **Die Kachel „Projekt öffnen/bearbeiten" öffnet den Wizard**, nicht die
   Projektverwaltung — ins Detailformular kommt man nur über „Projekt Details".
   Zugleich verspricht die Wiki-Doku bei „Öffnen" eine Projektliste. Die Kacheln
   selbst sind keine Controls, sondern `PictureBox` mit **kompletten JPG-Bildern**
   als Hintergrund plus absolut platzierten Labels (`Form_Start.cs:2003–2064`).
4. **Kuriosum mit Nebenwirkung:** Ein Klick auf das INEKON-Logo im Wizard öffnet
   einen Dateidialog und ändert **dauerhaft das Anwendungs-Icon in der Datenbank**
   (`WizardParent.cs:741–761`).

Dazu: eine massive Sprachlücke (`WizardParent.de-DE.resx` hat **einen** Eintrag — im
deutschen Programm stehen `Next ▶`/`◀ Back`, im englischen `Abbrechen ❌`), die
13-zeilige Seitenliste des Assistenten steht **doppelt** in `MenueCtrl.cs` (`:28–41`,
`:56–69`), die Fach-Seiten finden ihren Rahmen per **String-Suche** nach
„WizardParent" in `Application.OpenForms`, und der dokumentierte Tippfehler
„Projekt-Erstellung**k**onfiguration" steht in `Wizard_Komponenten.de-DE.resx`.

**Die gute Nachricht:** Die Bausteine für die Vereinheitlichung existieren schon —
`EinstiegsKarte` (Views\Kosten) ist ein echtes Karten-`UserControl` mit
Hover-Zustand und `Geklickt`-Ereignis, `KartenStil` (Views\Simulation) die einzige
benannte Token-Sammlung (17 Farben, `RAND=10`, `ECKE=6`). Dagegen liegen sechs Kopien
der Rundeck-Geometrie, 76 gestreute `Color.FromArgb(`-Literale allein in
`Views\Hauptformular\` und ungenutzte Bausteine (`RoundedPanel`, `DrawKPICard`)
herum.

---

## 2. Ist-Zustand (Fundstellen aus der Inventur)

### 2.1 Der Wizard

| Teil | Rolle | Befund |
|---|---|---|
| `WizardParent` | Rahmen (Navigation, linke Spalte) | Projektliste nur im Bearbeiten-Modus auf Seite 0 (`:265–273`); „Neues Projekt…" schaltet den laufenden Assistenten **mitten im Betrieb** um (`:910–942`); dauerhafter Knopf „📂 Öffnen" mit offenem TODO (`:420–428`); Logo-Klick = Icon-Wechsel (`:741–761`); `de-DE.resx` praktisch leer |
| `Wizard_Komponenten` | Seite „Komponenten auswählen" | elf Häkchen ohne Persistenz; Abwahl löscht Anlagen (ohne Kessel, ohne Rückfrage); Titel-Tippfehler; Doppel-Leerzeichen im Balkentext |
| `Wizard_Projekt`, `Wizard_Stromlastgang` | echte Schritte | — |
| `Wizard_WPItem` | **kein** Wizard-Schritt | WP-Bearbeitungsdialog (`BaseForm`), nur Name/Ordner irreführend |
| 9 Fach-Formulare | laufen im „Assistentenbetrieb" | verstecken OK/Abbrechen über `SetControls(..., true)`; finden den Rahmen per String-Suche `"WizardParent"` in `Application.OpenForms` |

Seitenliste (13 Zeilen) doppelt: `MenueCtrl.cs:28–41` und `:56–69`. Der Wizard kennt
weder Brauchwasser noch Pufferspeicher; `PUFFER_ITEM = 13` ist eine Konstante ohne
Seite.

### 2.2 Die Startmasken-Kacheln (Reiter „Projekt")

Sechs Kacheln als `PictureBox` (404×185) mit komplettem **JPG** als
`BackgroundImage` plus zwei absolut platzierten `Label`n, verdrahtet über ein
`CentralControl_Click`-Dictionary (`Form_Start.cs:2003–2064`). Ziele:

| Kachel | tatsächliches Ziel | Soll laut Doku/Erwartung |
|---|---|---|
| Neues Projekt | Wizard (Modus NEU) | ✓ |
| **Projekt öffnen/bearbeiten** | **Wizard (Modus BEARBEITEN)** | Projektliste/-verwaltung |
| Zuletzt geöffnet | Liste zuletzt geöffneter Projekte | ✓ |
| Speichern unter | `Form_ProjektSpeichernUnter` | ✓ |
| Projekt löschen | `Form_ProjektDelete` | ✓ |
| Projekt Details | `MenueCtrl.ProjektOeffnen(true)` → `FormMain` | ✓ (einziger Weg dorthin) |

Die sechs Projektkacheln sind die einzigen ohne Paint-Handler (keine
Statusmarkierung, anders als die Fach-Kacheln der übrigen Reiter).
`FensterEinpassung.Einhaengen` in `Form_Start.cs:69` ist wirkungslos
(`TopLevel == false`).

### 2.3 Menü und `FormMain`

- Menü „Projekt → Öffnen…" = Duplizieren (Befund 1 in Abschnitt 1).
- `FormMain` („Detailformular"/Reiter „Konfiguration Projekt", neun Kontextmenüs)
  ist die dritte Sicht auf den Komponentenbestand; erreichbar nur über die Kachel
  „Projekt Details" bzw. `MenueCtrl.ProjektOeffnen(true)`.

### 2.4 Texte, Sprache, Stil

- Tippfehler/Fundstellen: „Projekt-Erstellungkonfiguration"
  (`Wizard_Komponenten.de-DE.resx`, `label1.Text`); Doppel-Leerzeichen in
  `label3.Text` (neutral + de-DE) sowie `Form_Start.resx` (`label1.Text`,
  `label2_pBox_ProjektOeffnen.Text`); Formulartitel-Relikte `frm1`/`ab1`/`from 1`.
- Sprachlücke: `WizardParent.de-DE.resx` mit einem Eintrag → deutsche Oberfläche
  zeigt `Next ▶`/`◀ Back`, englische `Abbrechen ❌`/`📂 Öffnen`. Die Startmaske hat
  dieselben Begriffe korrekt zweisprachig.
- Stil: kein gemeinsames Kachel-Control; sechs Rundeck-Kopien mit zwei
  Bogen-Semantiken und vier Eckenradien; 76 Farb-Literale in `Views\Hauptformular\`;
  53 Schriftdefinitionen der Startmaske in der `.resx`.
- **Wiederverwendbar:** `EinstiegsKarte` (Hover + `Geklickt`, Designer-tauglich),
  `KartenStil` (Token), `ErzeugerKarte`/`KartenChip`/`SpeicherKarte`; ungenutzt:
  `RoundedPanel`, `ChartManager.Kacheln.DrawKPICard` (einziger Schatten-Baustein).

### 2.5 Wiki-Soll-Abgleich

Die Anwenderdoku (u. a. `Projekt anlegen`, `Erste Schritte`,
`Programm Dokumentation/Kurzanleitung`, `…/Projektverwaltung`) nennt den Wizard
durchgängig **„Assistent"** — das Wort „Wizard" kommt in der Doku nicht vor. Zehn
belegte Ist-↔-Doku-Widersprüche (Inventur Abschnitt 6.2), darunter: „Öffnen" soll
eine Projektliste zeigen (D5); nicht angekreuzte Bausteine sollen „in der
Simulationskonfiguration nicht auftauchen" (D4 — die Häkchen sind gar nicht
persistiert); die Startmaske wird widersprüchlich als geführte Reiterfolge *und*
freie Kachelfläche beschrieben (D8).

### 2.6 Kodierung

Kerndateien UTF-8. **CP1252**: die vier mitlaufenden Assistentenseiten
`Form_Waermebedarf.cs`, `Form_Prozesswaerme.cs`, `Form_Stromverbraucher.cs`,
`Form_SolarKollektoren.cs` sowie `SectionPanel.cs`, `ChartManagerNeu.cs`.

---

## 3. Zielbild

**Eine Formensprache, ehrliche Begriffe, eine Datenwahrheit — alles im Designer
pflegbar:**

1. **Karten-Baustein als gemeinsame Sprache.** Ein Designer-taugliches UserControl
   `AktionsKarte` (Weiterentwicklung von `EinstiegsKarte`: Icon, Titel,
   Beschreibung, Hover, `Geklickt`, optionaler Statuspunkt), gespeist aus einer
   zentralen, erweiterten `KartenStil`-Token-Klasse. Die sechs Projekt-Kacheln der
   Startmaske werden im Designer durch `AktionsKarte`-Instanzen ersetzt (JPG-Kacheln
   entfallen); die Fach-Reiter können später nachziehen.
2. **Der Wizard wird zum „Projektassistenten".** Rahmen bleibt eine Designer-Form,
   bekommt vollständige de-DE/en-US-`.resx` (Weiter/Zurück/Abbrechen), den
   Dokubegriff „Assistent" im Titel, **eine** zentrale Seitenlisten-Definition
   (statt der Doppelung in `MenueCtrl`), eine typisierte Rahmen-Erkennung (Property/
   Schnittstelle statt String-Suche in `OpenForms`), und verliert die Kuriositäten:
   Logo-Klick-Iconwechsel entfällt, der TODO-Knopf „📂 Öffnen" wird entweder echtes
   Öffnen über die neue Projektauswahl oder entfällt.
3. **Eine Projektauswahl für alle.** Ein UserControl `ProjektAuswahl`
   (Liste + Suche + Kennzahlen Spalte „geändert"), verwendet von: Menü
   „Projekt → Öffnen…" (das damit **echtes Öffnen** wird), Kachel
   „Projekt öffnen/bearbeiten", „Zuletzt geöffnet" (gefilterte Sicht) und der
   linken Spalte des Assistenten im Bearbeiten-Modus. Duplizieren heißt überall
   „Speichern unter" und ist nie mehr hinter „Öffnen…" versteckt
   (`MenueCtrl.cs:83,91` wird aufgelöst, `:158`-Bug entfällt mit).
4. **Eine Datenwahrheit für Komponenten.** Der Komponentenschritt des Assistenten
   zeigt und ändert denselben Bestand wie Kachel-Bitmaske und `FormMain`
   (Quelle: die Projekt-Anlagen), im Kachelstil des Energieerzeuger-Reiters statt
   der Häkchenbatterie. Abwählen löscht nur nach **ausdrücklicher Rückfrage** (und
   die Kessel-Lücke der Löschroutine wird geschlossen). Brauchwasser und
   Pufferspeicher werden ehrlich abgebildet (heute fehlen sie im Wizard komplett).
5. **Texte und Sprache**: Tippfehler weg, Relikt-Titel weg, alle beteiligten
   Formulare vollständig zweisprachig; die Wiki-Seiten (Kurzanleitung,
   Projektverwaltung, Projekt anlegen) werden nach der Umsetzung angeglichen
   (Auflösung der Widersprüche D1–D10).

**Nicht Teil der Aufgabe:** die Fach-Reiter der Startmaske (Wärmebedarf …
Berichte & Kosten) inhaltlich, die Kopfzeile (Klimaregion/Projektanzeige), `FormMain`
innen — sie profitieren nur mittelbar (Karten-Baustein, Begriffe).

---

## 4. Entscheidungen (Nutzerentscheid 29.08.2026)

1. **E1 Komponentenschritt — ENTSCHIEDEN: (b)** Kachelauswahl im Stil des
   Energieerzeuger-Reiters, gespeist aus dem Anlagenbestand — eine Optik, eine
   Wahrheit.
2. **E2 Kachel „Projekt öffnen/bearbeiten" — ENTSCHIEDEN: Assistent-Bearbeitenmodus
   bleibt** das Kachelziel. Die neue `ProjektAuswahl` vereinheitlicht trotzdem alle
   übrigen Wege (Menü „Öffnen…", „Zuletzt geöffnet", linke Spalte des Assistenten im
   Bearbeiten-Modus) — der Doku-Anspruch „Öffnen zeigt eine Projektliste" (D5) wird
   damit über Menü und Assistenten-Spalte erfüllt.
3. **E3 Löschverhalten — ENTSCHIEDEN:** Rückfrage mit Klartext („entfernt N
   Anlagen: …"), Vorbelegung **Nein**; die Kessel-Lücke der Löschroutine wird
   geschlossen.
4. **E4 Logo-Klick/Icon-Wechsel — OFFEN** (Erläuterung angefordert). Optionen:
   **(a)** ersatzlos entfernen [Empfehlung]; **(b)** als bewusste Funktion
   „Anwendungs-Logo ändern" mit Rückfrage in die Administration verlagern (falls
   das Umbranden je Kunde eine gewollte Funktion ist); **(c)** belassen. Betrifft
   nur P4 — die Pakete P1–P3 sind davon unabhängig.
5. **E5 Begriff — ENTSCHIEDEN:** app-weit „Projektassistent" statt „Projekt Wizard".
6. **E6 Alt-JPG-Kacheln — ENTSCHIEDEN:** für die übrigen Reiter vorerst behalten,
   Ablösung je Reiter später.

---

## 5. Vorgehensweise (Pakete)

### P1 — Fundament: Karten-Baustein und Stil-Token *(Voraussetzung für alles Sichtbare)*

`AktionsKarte` als Designer-UserControl (aus `EinstiegsKarte` entwickelt, mit
Designer-Properties Icon/Titel/Beschreibung/Status), `KartenStil` zu zentraler
Token-Klasse ausgebaut (Farben/Radien/Abstände — ersetzt die sechs
Rundeck-Kopien schrittweise). Designer-Roundtrip-Beweis: Control in VS platzierbar,
Eigenschaften im Eigenschaftenfenster.

### P2 — Startmaske Reiter „Projekt"

Sechs `AktionsKarte`-Instanzen im Designer statt PictureBox+JPG; Klick-Ziele bleiben
unverändert (E2: „öffnen/bearbeiten" → Assistent-Bearbeitenmodus); Texte/Tippfehler
(inkl. Doppel-Leerzeichen in `Form_Start.resx` und
„Projekt-Erstellungkonfiguration" in `Wizard_Komponenten.de-DE.resx`)/`.resx` beider
Sprachen; toter `FensterEinpassung`-Aufruf weg.

### P3 — Menü- und Begriffsehrlichkeit

Neues UserControl `ProjektAuswahl` + schlanke Designer-Hüllform; Menü
„Projekt → Öffnen…" öffnet wirklich (Duplizieren sauber als „Speichern unter…");
`MenueCtrl.cs:83/91/158` bereinigt; „Zuletzt geöffnet" auf dieselbe Komponente
(sortiert nach „geändert"). Die Kachel „öffnen/bearbeiten" bleibt beim Assistenten
(E2).

### P4 — Projektassistent (Rahmen)

Titel/Begriff, vollständige Satelliten-`.resx`, Seitenliste einmalig definiert,
typisierte Rahmen-Erkennung statt String-Suche, Logo-Klick raus (E4), „📂 Öffnen"
gemäß E2 aufgelöst, linke Spalte = `ProjektAuswahl` im Bearbeiten-Modus.

### P5 — Komponentenschritt und Datenwahrheit

Umsetzung gemäß E1/E3: Anlagenbestand als einzige Quelle, Rückfrage beim Entfernen,
Kessel-Lücke schließen, Brauchwasser/Pufferspeicher abbilden; Abgleich mit
Kachel-Bitmaske und `FormMain`-Ansicht (alle drei zeigen dasselbe).

### P6 — Abnahme und Doku-Angleich

Prüfliste (Abschnitt 6) vollständig; Wiki-Seiten `Programm
Dokumentation/Kurzanleitung`, `…/Projektverwaltung`, `Projekt anlegen`, `Erste
Schritte` an die neue Wirklichkeit angleichen (D1–D10 auflösen);
`help_mapping.txt`/`HilfeKontext` für umbenannte/neue Formulare nachziehen.

**Reihenfolge:** P1 → P2/P3 (parallel möglich) → P4 → P5 → P6. Nach P2+P3 ist die
sichtbarste Vereinheitlichung ausgeliefert; P4/P5 heben den Assistenten nach.

---

## 6. Prüfliste für die Abnahme

| # | Prüfung | Erwartung |
|---|---|---|
| 1 | Jede geänderte/neue Form im VS-Designer öffnen | öffnet ohne Fehler, Eigenschaften pflegbar (Kern-Vorgabe) |
| 2 | Alle sechs Projekt-Kacheln klicken (DE + EN) | einheitliche Kartenoptik, korrekte Ziele gemäß E2 |
| 3 | Menü „Projekt → Öffnen…" | zeigt Projektliste, öffnet das gewählte Projekt — dupliziert nichts |
| 4 | „Speichern unter…" | dupliziert wie bisher, unter ehrlichem Namen |
| 5 | Assistent Neu: kompletter Durchlauf | Weiter/Zurück deutsch/englisch korrekt, Titel „Projektassistent" |
| 6 | Assistent Bearbeiten: Projektwahl links | dieselbe `ProjektAuswahl` wie Menü/Kachel |
| 7 | Komponenten abwählen (E3) | Rückfrage mit Klartext; Nein = keine Änderung; Ja entfernt auch Kessel korrekt |
| 8 | Komponentenstand dreifach vergleichen | Assistent, Kachel-Bitmaske, `FormMain` zeigen identischen Bestand |
| 9 | Brauchwasser/Pufferspeicher im Assistenten | sichtbar und wirksam (heute fehlend) |
| 10 | Logo-Klick im Assistenten | keine Funktion mehr (E4); Anwendungs-Icon unverändert |
| 11 | Info-Buttons/Bereichshilfe der umgebauten Masken | weiterhin aktiv (help_mapping-Namen nachgezogen) |
| 12 | CP1252-Stichprobe | die vier Assistenten-Fachseiten + `SectionPanel`/`ChartManagerNeu` diff-sauber |
| 13 | Referenzlauf | unberührt (keine Engine-Änderung; Lauf trotzdem einmal als Netz) |

---

## 7. Aufwand

| Paket | Umfang | Aufwand |
|---|---|---|
| P1 Karten-Baustein + Token | 2 neue Dateien, Designer-Control | 4–6 h |
| P2 Startmaske Projekt-Reiter | 6 Kacheln, Texte, resx | 6–9 h |
| P3 Öffnen ehrlich + ProjektAuswahl | 1 UserControl, MenueCtrl, 2 Formen | 5–7 h |
| P4 Assistent-Rahmen | resx, Seitenliste, Erkennung, Aufräumen | 6–9 h |
| P5 Komponenten-Datenwahrheit | je nach E1/E3 | 7–11 h |
| P6 Abnahme + Wiki-Angleich | Prüfliste, 4 Wiki-Seiten | 4–6 h |

**Gesamt: rund 32–48 h**, sinnvoll in zwei Auslieferungsschnitten
(P1–P3, dann P4–P6).

---

## 8. Fallstricke bei der Umsetzung

- **Designer-Vorgabe ist Abnahmekriterium:** erzeugte `.Designer.cs` müssen dem
  VS-Serialisierungsmuster exakt folgen (InitializeComponent-Blöcke, Reihenfolge,
  resx-Kopplung) — jede Form wird nach Änderung im Designer geöffnet (Prüfung 1).
- **Kodierung:** die vier CP1252-Fachseiten und `SectionPanel`/`ChartManagerNeu`
  nur byte-schonend anfassen (bewährtes Encoding-1252-Rezept).
- **TabPage-Vierseitenanker-Falle** und **AutoScroll-Verdeckung** (dokumentierte
  Muster im Projektgedächtnis) bei neuen/umgebauten Seiten beachten.
- **`help_mapping.txt` hängt an Formularnamen** (175 Zeilen): Umbenennungen oder
  neue Hüllformen ziehen Zuordnung, Vertragstabelle im Wiki und `HilfeKontext`
  nach sich — in P6 fest eingeplant, nicht vergessen.
- **`InfoKnopf`/`KiAufrufKnopf`-Plätze** bei neuen Layouts prüfen (Kollisionsregel
  oben rechts).
- **Löschverhalten (E3) niemals still ändern** — die heutige stille Löschung ist
  der Fehler, nicht das Vorbild.
- **`.resx` nie über das Bash-Werkzeug** bearbeiten (Backtick-Falle), MyResource-
  Schlüssel beidsprachig, VS regeneriert `Resource.Designer.cs` selbst.
- Menü-/Kachelziele erst umstellen, wenn `ProjektAuswahl` fertig ist — kein
  Zwischenzustand, in dem „Öffnen" ins Leere führt.

---

## 9. Verwandte Dokumente

- Inventur (Arbeitsmaterial dieser Analyse): Scratchpad
  `projektdialoge\Inventar.md` — die tragenden Fakten sind mit Fundstellen in
  dieses Konzept übernommen.
- [`Konzept_Hilfesystem_Wikidokumentation.md`](Konzept_Hilfesystem_Wikidokumentation.md)
  — Hilfesystem/Wiki-Kopplung (help_mapping, Vertragstabelle, Bereichshilfe), von
  P6 berührt.
- [`WindowsFormsApplication1/CLAUDE.md`](WindowsFormsApplication1/CLAUDE.md) —
  Designer-/resx-Regeln, Kodierungs-Fallstrick, Drei-Schichten-Regel.
- Wiki: [`Projekt anlegen`](https://wiki.epos-plan.de/wiki/Projekt_anlegen),
  [`Programm Dokumentation/Kurzanleitung`](https://wiki.epos-plan.de/wiki/Programm_Dokumentation/Kurzanleitung),
  [`Programm Dokumentation/Projektverwaltung`](https://wiki.epos-plan.de/wiki/Programm_Dokumentation/Projektverwaltung)
  — Soll-Beschreibungen, in P6 anzugleichen.

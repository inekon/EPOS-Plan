# Konzept — Hilfesystem und Infobutton (EPOS-Plan)

**Stand:** 22.08.2026 · **Rev. 1 — zur Abnahme**

Anlass: Der Infobutton reagiert nicht, und Hilfe fehlt in nahezu allen Dialogen und Bereichen.
Dieses Dokument hält den gemessenen Ist-Stand fest, benennt die Ursache und legt fest, wie das
Hilfesystem flächendeckend ausgeführt wird.

---

## 1 Ist-Stand (gemessen, nicht vermutet)

### 1.1 Die drei Schichten

| Schicht | Ort | Zustand |
|---|---|---|
| **Inhalt** | WordPress `https://epos-plan.de`, REST `wp/v2/pages` | **vollständig** — 116 Seiten, DE und EN durchgängig paarweise (aber nur 108 eindeutige Slugs, siehe 3.3) |
| **Mechanik** | `Allgemein/Hilfe/HelpCatalog.cs`, `Views/Help/Form_HelpPopup.cs` | **vorhanden und funktionsfähig** |
| **Zuordnung** | `help_mapping.txt` neben der EXE | **existiert nicht — nirgends** |

Die Zuordnungsschicht ist nie geschrieben worden. Inhalt und Mechanik greifen deshalb nie ineinander.

### 1.2 Warum der Infobutton nichts tut

`HelpExtender.RegisterControl()` liest `help_mapping.txt` aus dem EXE-Verzeichnis und steigt
wortlos aus, wenn die Datei fehlt:

```csharp
string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "help_mapping.txt");
if (!File.Exists(filePath)) return;          // <— hier endet jede Registrierung
```

Die Datei liegt weder im Repository noch neben der gebauten EXE. Folge: `_keys` bleibt leer,
`Control_MouseEnter` findet nie einen Schlüssel, es erscheint nie ein Popup. Der Button ist
**überall** tot, nicht nur auf dem Projekt-Reiter.

Ein weiterer Mangel im selben Pfad:

- **Der Button reagiert gar nicht auf Klicks.** `SetHelpKey` verdrahtet ausschließlich
  `MouseEnter`/`MouseLeave`. Ein Steuerelement, das wie eine Schaltfläche aussieht, aber nur auf
  Überfahren reagiert, wirkt auch nach behobener Zuordnung defekt.

**Geprüft und in Ordnung — keine Umlautbeschädigung.** Ein erster Verdacht auf zerstörte Umlaute
hat sich nicht bestätigt: Der Server liefert korrekt escaptes JSON (`Verträge`), und
`help_cache.json` enthält null U+FFFD-Zeichen. Der Fehleindruck entstand allein durch eine
cp1252-Konsole bei der Prüfung. Die Kodierungskette Server → Cache → Anzeige ist intakt.

### 1.3 Vorhandene Infobuttons: sechs Stück in drei Formularen

| Formular | Steuerelement |
|---|---|
| `Views/Hauptformular/Form_Start` | `btn_Help` (Kopfzeile), `btn_Help_Kurzanleitung` (Projekt), `btn_Help_Waermebedarf`, `btn_Help_Strombedarf` |
| `Views/Klimadaten/Form_Klimadaten` | `btn_Help` |
| `Views/Kosten/Form_Kosten` | `btn_Help` |

Nur diese drei Formulare erzeugen überhaupt einen `HelpExtender`. Dem stehen **151 Formulare,
5 UserControls und 25 Bereiche** gegenüber. Das ist die Lücke, die als „fehlendes ausgeführtes
Konzept" sichtbar wird.

### 1.4 Weitere Beobachtungen

- **Wettlauf beim Start.** `MDIMainForm_Load` ruft `HelpCatalog.LoadAllAsync()` bewusst ohne
  `await` (Kommentar an Ort und Stelle). Formulare, die früher öffnen, sehen einen leeren Katalog.
- **`WordPressPrefix = pages` zieht den ganzen Webauftritt.** Neben den Fachseiten landen Shop-
  und Rechtsseiten im Katalog (Kasse, Mein Konto, Widerrufsbelehrung, Refunds, Right of Withdrawal).
  Rauschen, kein Fehler — aber es macht die Slug-Pflege unübersichtlich.

---

## 2 Fachliche Festlegungen

### F1 — Auslöser: Klick führt, Überfahren ergänzt

Klick auf den Infobutton öffnet das Hilfe-Popup und lässt es stehen, bis der Anwender es schließt
oder woanders hinklickt. Überfahren zeigt weiterhin die flüchtige Kurzinfo.

*Begründung:* Eine Schaltfläche muss auf den Klick reagieren. Das reine Hover-Verhalten ist der
Grund, warum der Button selbst nach Behebung der Zuordnung noch als defekt empfunden würde.

### F2 — Zuordnung wird mitgeliefert, nicht vorausgesetzt

Die Zuordnung wird als **eingebettete Ressource** in die Assembly kompiliert. `help_mapping.txt`
neben der EXE bleibt zulässig und **übersteuert** die eingebettete Fassung, damit sich Zuordnungen
ohne Neubau korrigieren lassen.

*Begründung:* Genau die stille Abwesenheit einer optionalen Datei hat das Hilfesystem lahmgelegt.
Hilfe darf nicht daran scheitern, dass eine Textdatei beim Ausrollen vergessen wurde.

### F3 — Fehlende Zuordnung wird sichtbar, nicht verschluckt

Im Debug-Build wird jeder Infobutton ohne Zuordnung und jeder Slug ohne Katalogtreffer als
Warnung protokolliert. Im Release bleibt es still, aber der Button wird **deaktiviert statt
wirkungslos angezeigt** — ein grauer Button ist ehrlicher als ein toter.

### F4 — Sprache über Slug-Paare

Die Zuordnung hinterlegt beide Slugs, getrennt durch `|`:

```
Form_Klimadaten.btn_Help = klimadaten | climate-data
```

Zur Laufzeit entscheidet die eingestellte Oberflächensprache. Fehlt der Slug der aktiven Sprache
im Katalog, greift der andere.

*Begründung:* Der Katalog liefert DE und EN ohnehin paarweise; eine zweite Übersetzungstabelle
wäre eine Fehlerquelle mehr.

### F5 — Registrierung zentral, nicht 151-mal einzeln

Ein anwendungsweiter `HelpExtender` registriert Formulare **automatisch beim Öffnen**, statt in
jedem Formular eigenen Code zu verlangen.

*Begründung:* Der bisherige Weg — drei Zeilen pro Formular — ist der Grund, warum es bei drei von
151 Formularen geblieben ist. Ein Muster, das man 151-mal von Hand anwenden muss, wird nicht
angewendet. Die Zuordnungsdatei bleibt damit die einzige Stelle, an der Hilfe gepflegt wird.

### F6 — Offline-Erstlauf

Ein gepflegter `help_cache.json` wird als Startbestand mitgeliefert. Ohne Netz und ohne
vorherigen Onlinelauf ist die Hilfe sonst beim ersten Start leer.

---

## 3 Slug-Konvention

### 3.1 Die deutsche Dokumentation ist vollständig

Am 22.08.2026 gegen den Live-Bestand geprüft (`wp/v2/pages`, 116 Seiten): **44 englische, 72
deutsche/sonstige Seiten. Es fehlt keine einzige deutsche Fachseite.** Die Baumstruktur ist
1:1 gespiegelt — 15 zu 15 unter Grundlagen/Fundamentals, 7 zu 7 unter Programmablauf/Program
workflow, 17 zu 17 auf oberster Ebene. Deutsch hat mit `beispiele` unter Schulung sogar eine
Seite mehr.

Der frühere Eindruck fehlender deutscher Seiten war ein Trugschluss: Die deutschen Slugs
spiegeln die englischen **nicht**, sondern sind eigenständig benannt. Eine Suche nach dem
englischen Muster findet sie deshalb nicht.

### 3.2 Slug-Paare — vollständig, nicht ableitbar

Die Paare lassen sich weder durch Übersetzung noch durch Muster berechnen (`bhkw` ↔ `chp`,
`solarkollektoren` ↔ `solar-collectors`). Sie müssen explizit hinterlegt werden — genau das
leistet F4.

**Grundlagen** (`/epos-plan/epos-plan-grundlagen/` ↔ `/english/fundamentals/`):

| Bereich | DE-Slug | EN-Slug |
|---|---|---|
| Klimadaten | `klimadaten` | `climate-data` |
| Wärmepumpe | `waermepumpe` | `heat-pump` |
| Kessel und Spitzenlast | `kessel-spitzenlast` | `boilers-peak-load` |
| BHKW | `bhkw` | `chp` |
| Solarkollektoren | `solarkollektoren` | `solar-collectors` |
| Photovoltaik | `photovoltaik` | `photovoltaics` |
| Pufferspeicher | `pufferspeicher` | `buffer-storage` |
| Stromspeicher | `stromspeicher` | `battery-storage` |
| Strombedarf und Lastprofile | `strombedarf` | `electricity-demand` |
| Wärmebedarfsrechnung | `waermebedarfsrechnung` | `heat-demand-calculation` |
| Hydraulikschemata | `hydraulikschemata` | `hydraulic-schemes` |
| Kostenrechnung | `kostenrechnung` | `cost-calculation` |
| Wirtschaftlichkeitsrechnung | `wirtschaftlichkeitsrechnung` | `economic-analysis` |
| Erlösrechnung | `erloesrechnung` | `revenue-calculation` |
| Vergleich Energiebilanz | `vergleich-energiebilanz` | `energy-balance-comparison` |

**Programmablauf** (`/epos-plan/epos-plan-programmablauf/` ↔ `/english/program-workflow/`):

| Bereich | DE-Slug | EN-Slug |
|---|---|---|
| Projektverwaltung | `projektverwaltung` | `project-management` |
| Wärmebedarfsberechnung | `waermebedarfsberechnung` | `heat-demand-calculation` |
| Strombedarfsberechnung | `strombedarfsberechnung` | `electricity-demand-calculation` |
| Schadstoffemissionen | `schadstoffemissionen` | `emissions` |
| Erlösrechnung | `erloesrechnung` | `revenue-calculation` |
| Wirtschaftlichkeitsrechnung | `wirtschaftlichkeitsrechnung` | `economic-analysis` |
| Bericht drucken | `bericht-drucken` | `printing-the-report` |

**Oberste Ebene** (Auswahl): Kurzanleitung `epos-plan-kurzanleitung` ↔ `quick-start`,
Programmablauf `epos-plan-programmablauf` ↔ `program-workflow`, Grundlagen
`epos-plan-grundlagen` ↔ `fundamentals`, Systemvoraussetzungen
`epos-plan-systemvoraussetzungen` ↔ `system-requirements`.

### 3.3 Befund: Slug-Kollisionen zerstören Einträge

116 Seiten teilen sich nur **108 eindeutige Slugs**. `LoadAllAsync` verwendet den Slug als
Schlüssel und verwirft Nachzügler wortlos:

```csharp
if (!string.IsNullOrEmpty(slug) && !tempCache.ContainsKey(slug))   // erster gewinnt
```

Sieben Slugs sind doppelt belegt, acht Seiten gehen dadurch verloren:

| Slug | Kollidierende Seiten |
|---|---|
| `installation` | `/english/installation/installation/`, `/english/installation/`, **`/epos-plan/epos-plan-installation/installation/`** |
| `update` | `/english/installation/update/`, **`/epos-plan/epos-plan-installation/update/`** |
| `economic-analysis` | `/english/program-workflow/…`, `/english/fundamentals/…` |
| `heat-demand-calculation` | `/english/program-workflow/…`, `/english/fundamentals/…` |
| `revenue-calculation` | `/english/program-workflow/…`, `/english/fundamentals/…` |
| `erloesrechnung` | `…/epos-plan-grundlagen/…`, `…/epos-plan-programmablauf/…` |
| `wirtschaftlichkeitsrechnung` | `…/epos-plan-grundlagen/…`, `…/epos-plan-programmablauf/…` |

Zwei Auswirkungen, beide für den Anwender sichtbar:

- **Sprachbruch.** Bei `installation` und `update` gewinnt die englische Seite. Ein deutscher
  Anwender landet dort auf englischem Text — und keine Zuordnung kann das reparieren, weil die
  deutsche Seite gar nicht erst im Katalog ankommt.
- **Falsches Kapitel.** `erloesrechnung` und `wirtschaftlichkeitsrechnung` existieren je zweimal
  in unterschiedlicher Tiefe. Welche gewinnt, hängt an der Reihenfolge der REST-Antwort.

**Festlegung F7 — Schlüssel ist der Pfad, nicht der Slug.** Der Katalog wird zusätzlich zum Slug
über den Link-Pfad adressierbar. Die Zuordnungsdatei darf damit statt eines mehrdeutigen Slugs
einen eindeutigen Pfad angeben, wo nötig. Der Slug bleibt als bequeme Kurzform zulässig, solange
er eindeutig ist; bei Mehrdeutigkeit protokolliert der Katalog eine Warnung.

---

## 4 Ausführung in Etappen

| Etappe | Inhalt | Ergebnis | Stand |
|---|---|---|---|
| **H1** | Zuordnungsdatei anlegen, einbetten, Übersteuerung; die 6 vorhandenen Buttons verdrahten | Infobutton funktioniert | **erledigt** 22.08. |
| **H2** | Klickauslöser (F1), Deaktivierung ohne Zuordnung (F3), Diagnoseprotokoll | Verhalten stimmig | **erledigt** 22.08. |
| **H3** | Zentrale Registrierung (F5), Startbestand (F6), Startwettlauf entschärfen | Fundament trägt | **erledigt** 22.08. |
| **H4** | Infobutton in allen 25 Bereichen ergänzen — Reihenfolge nach Nutzungshäufigkeit: Energieerzeuger, Wärmebedarf, Strombedarf, Simulation, Berichte & Kosten, dann Rest | Flächendeckung | in Arbeit |
| **H5** | Slug-Kollisionen auflösen (F7); Slug-Präfix von `pages` auf einen eigenen Inhaltstyp umstellen | Pflege sauber | F7 **erledigt** 22.08.; Präfix offen (Website) |

**Umsetzungsnotizen zu H1–H3 und F7** (22.08.2026, alles gebaut und getestet, 767 Tests grün):

- Die Zuordnung liegt als `Allgemein\Hilfe\help_mapping.txt`, eingebettet als Ressource, durch eine
  lose Datei neben der EXE übersteuerbar.
- F5 löst `Allgemein\Hilfe\HilfeAutomatik.cs` über `Application.Idle` + `Application.OpenForms` +
  `ControlAdded`. Eine gemeinsame Basisklasse wäre an `Form_Start` vorbeigegangen, das mit
  `TopLevel = false` als Kind des MDI-Fensters läuft. **Neue Formulare brauchen keinen Programmtext
  mehr — eine Zeile in der Zuordnungsdatei genügt.**
- F6: `Allgemein\Hilfe\help_cache.json` mit allen 116 Seiten, reines ASCII. Rangfolge
  Online → AppData-Cache → mitgelieferter Startbestand.
- F7 belegt: 116 Seiten, 116 Pfade, 108 Slugs; **keine** der 116 Seiten geht mehr verloren, beide
  Seiten aller sieben Kollisionen sind einzeln erreichbar.
- Mitgezogen: `Allgemein\KI\HilfeWissen.cs` las den Cache-Schlüssel als Abschnittstitel. Da der
  Schlüssel jetzt ein Pfad ist, hätte die KI-Stichwortsuche bei jeder Frage auf „epos-plan",
  „grundlagen" und „english" angeschlagen.
- Nebenwirkung: CS4014 ist entfallen, weil `LoadAllAsync` Ausnahmen nun selbst abfängt statt sie als
  unbeobachtete Task-Ausnahme laufen zu lassen. Warnungen 6 → 5.

Redaktionelle Arbeit an der deutschen Dokumentation entfällt — sie ist vollständig (3.1).

H1 und H2 beheben den gemeldeten Fehler. H3 bis H5 sind die eigentliche Ausführung des Konzepts.

---

## 5 Abnahmekriterien

1. Klick auf jeden der sechs vorhandenen Infobuttons öffnet das Popup mit passendem Kapiteltitel;
   der Link öffnet die zugehörige Seite auf epos-plan.de.
2. Umschalten auf Englisch führt denselben Button auf die englische Seite.
3. Start ohne Netzverbindung zeigt Hilfe aus dem mitgelieferten Startbestand.
4. Kein Infobutton wirkt anklickbar, ohne zu reagieren (F3).
5. Nach H4 besitzt jeder der 25 Bereiche mindestens einen wirksamen Infobutton.

---

## 6 Verweise

- `Allgemein/Hilfe/HelpCatalog.cs` — Katalog, `HelpExtender`
- `Views/Help/Form_HelpPopup.cs` — Anzeige
- `KONTEXT_Importkodierung_ANSI.md` — Umlautbeschädigung, gleiche Ursachenfamilie
- `EPOSPlan_Dokumentation_DesignSkizze.md` — Gestaltungsrahmen

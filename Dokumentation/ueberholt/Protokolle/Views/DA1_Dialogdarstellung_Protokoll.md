# DA‑1 — Dialogdarstellung: Projektkopfseite, Projektdialoge, Kachel „Zuletzt geöffnet“ (Protokoll, 26.09.2026)

Statuszeilen #542, #545 und #550 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md).
Zweig `ios_migration_september` im Hauptbaum; Commits `af52ed5d1` (Projektkopfseite),
`ad88cd2ec` (Projektdialoge) mit Merge `91869613e`, dazu die Papiere. Kein Schemaschritt,
Testdatenbank unberührt, kein Rechenweg berührt, kein Referenzlauf.

## Anlass

Drei Meldungen des Anwenders aus der Arbeit am Gerät (Windows, 26.09.2026): Die Projektkopfseite
des Assistenten zeigt die Felder weit auseinandergezogen, das dritte Feldpaar läuft aus dem Bild;
„Projekt öffnen“ und „Speichern unter“ öffnen fast bildschirmfüllend, der Inhalt steht im oberen
Drittel; die Kachel „Zuletzt geöffnet“ der Startseite führt nicht mehr auf das zuletzt geöffnete
Projekt.

## 1. Projektkopfseite des Assistenten (`af52ed5d1`)

**Ursache.** Der Hausraster legte auf dem breiten Assistenten drei Feldpaare nebeneinander, jede
Beschriftung in einer festen Spalte von 12 rem (`--epos-beschriftung-breite`): große Lücken
zwischen Beschriftung und Feld, das dritte Paar außerhalb des sichtbaren Bereichs.

**Änderung.**

- `EPOS.UI/Seiten/Assistent/ProjektKopfSeite.razor`: Einleitung, Raster, Hinweise und Beschreibung
  in einem Block `.epos-projektkopf-formular` (höchstens 60 rem, linksbündig unter dem breiten
  Gruppenkopf); Einleitungssatz und Pflichtlegende eng beieinander.
- `EPOS.UI/wwwroot/epos-ui.css`, nur unter `.epos-projektkopf`: genau zwei Spalten
  (Name/Klimaregion, Kunde/Bearbeiter, Änderungs-/Erstelldatum), Beschriftung über dem Feld und
  halbfett, Klappliste der Klimaregion ab linker Feldkante, gesperrte Datumsfelder als Anzeige
  (Seitenfläche, leise Schrift, gestrichelter Rahmen), unter 900 px eine Spalte. Hausraster und
  alle übrigen Masken unverändert.

**Abnahme.** Zwei Tests in `EPOS.UI.Tests/Seiten/ProjektKopfSeiteTests.cs` (Block und
Stilregeln); Gate unten.

## 2. Projektdialoge nach Inhalt (`ad88cd2ec`, Merge `91869613e`)

**Ursache.** `ProjektWahlHuelle` (Wunschmaß 760 × 560) und `ProjektKopieFenster` (940 × 660)
öffneten ohne Dialogart, also als Fachdialog. `Fenstermass.Vorgabe` nimmt für den Fachdialog das
Größere aus Wunschmaß und 85 % × 90 % des Arbeitsbereichs — auf 1920 × 1040 also 1632 × 896.

**Änderung.**

- `EPOS.UI/Dienste/Fenstermass.cs`: neue `Dialogart.Inhaltsmass` (wächst nicht mit dem Schirm,
  nur mit der Skalierung, gedeckelt bei 92 % des Arbeitsbereichs) und
  `Fenstermass.Projektdialog` = 1180 × (342 + 8 × 53) = 1180 × 766 CSS-Pixel, aus dem Layout
  abgeleitet.
- `BlazorDialogForm.Vorgabemass` reicht `DeviceDpi / 96` des aktiven Fensters als Skalierung
  durch.
- `ProjektWahlHuelle.cs` und `ProjektKopieFenster.cs` wünschen `Projektdialog` und öffnen als
  `Inhaltsmass`.
- `epos-ui.css`: eigener Block hinter `.epos-projektkopie-raster` — ist der Dialog Wurzel seines
  Fensters, nimmt er die volle Höhe, die Liste füllt die freie Höhe (mindestens drei Zeilen), die
  Knopfleiste steht unten; die Felder von „Speichern unter“ mit 9 rem Beschriftungsspalte. Die
  iOS-Wurzel (`AppWurzel`) ist unberührt.
- `WindowsFormsApplication1/CLAUDE.md`, Regel (f): Dialogart `Inhaltsmass` für Dialoge, deren
  Inhalt die Größe bestimmt.

**Abnahme.** `EPOS.UI.Tests/FenstermassTests.cs`: Maß, Schirmunabhängigkeit, Skalierung und
Deckel, dazu eine Wache über beide Hüllen; Gate unten.

## 3. Kachel „Zuletzt geöffnet“ — Untersuchung

**Befund: keine Regression.** Die Kachel liest Name und Id aus `Tab_Applikation`
(`ProjektKontextCtrl.ZuletztGeoeffnet`). Gemerkt wird ausdrücklich über
`ProjektKontextCtrl.ZuletztGeoeffnetMerken` durch die Kacheln „Projekt neu“, „Projekt öffnen“
und „Zuletzt geöffnet“ sowie den Rückweg aus dem Assistenten; der Variantenwechsel im Kopfband und
die Menüwege „Neu“/„Bearbeiten“ merken nicht (Bestandsverhalten, `ProjektKontextCtrl.Setzen`).
Wird das gemerkte Projekt gelöscht, leert `ProjektCtrl.LoeschenMitVorarbeiten` in Schritt 3 den
Merker (`Tab_Applikation.ID_Projekt = 0`, Name leer) — bewusst, damit die Kachel nicht auf ein
nicht mehr vorhandenes Projekt zeigt. Die Kachel fällt dann auf die Projektliste zurück
(`StartseiteHuelle.ProjektZuletzt`, zuletzt geänderte zuerst). Keine Codeänderung.

## Nebenbefund: KI-Tests schreiben in das Protokoll des Anwenders

`KiFeldSetzenTests` und `KiMaskenhakenTests` hängen Zeilen an die echte Datei
`%ProgramData%\EPOS_PLAN\ki_aktionen.txt`: `KiAusfuehrung.ProtokollPfad` bildet den Pfad aus
`DataRepository.GetDBPath()`, und die Tests setzen keine Pfadüberschreibung. Die Tests bleiben
grün; die Datei des Anwenders sammelt Testzeilen. Nicht behoben — Folgeauftrag.

## Zahlen und Abnahme

Gate auf `91869613e`, nacheinander:

| Schritt | Ergebnis |
|---|---|
| Kern-Filter Release | 0 Fehler, 56 Warnungen (Bestand) |
| Tests des Kern-Filters, voll | 0 Fehler: EPOS.Kern.Tests 8 208 erfolgreich / 1 übersprungen, EPOS.UI.Tests 6 558, KiKern.Tests 549, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27 / 1 übersprungen — zusammen 15 728 erfolgreich, 2 übersprungen |
| Windows-Schale Debug x64 | kein Übersetzungsfehler; 10 Fehler allein im Kopierschritt (5 × MSB3021, 5 × MSB3027), weil die laufende `EPOS_Plan.exe` und Visual Studio den Ausgabeordner sperren — nicht behoben |
| Ressourcen-Designer (prüfen) | 12 329 Einträge, abweichend 0, neu 0; wiederholbar |

Kein Referenzlauf (kein Rechenweg berührt), kein iOS-Lauf (iOS-Hülle nicht berührt; der
Kern-Lauf auf ubuntu ist der Nachweis nach dem Push).

## Offene Punkte

1. **Rückfragen zur Kachel „Zuletzt geöffnet“ an den Anwender:**
   (a) Was passiert beim Klick — nichts, die Projektliste öffnet sich, oder ein falsches Projekt?
   (b) Wie war das Projekt davor geöffnet worden — Kachel bzw. Menü „Öffnen…“ (merkt) oder
   Variantenwahl, Menü „Neu“/„Bearbeiten“ (merkt bewusst nicht)?
   (c) Trat es nach dem Löschen von „test2“ auf, und auch mit dem Build nach 10:35 (Schema 148)?
   Angebot, falls unerwünscht: nach dem Löschen des gemerkten Projekts auf das zuletzt geänderte
   verbleibende Projekt zurückfallen statt den Merker zu leeren.
2. **KI-Protokoll der Tests:** `KiFeldSetzenTests` und `KiMaskenhakenTests` auf eine
   Pfadüberschreibung (Testordner) umstellen, damit kein Test in `%ProgramData%\EPOS_PLAN`
   schreibt.
3. **Windows-Schale** nach dem Schließen der laufenden Anwendung und von Visual Studio neu bauen
   und die beiden Projektdialoge sowie die Projektkopfseite am Gerät ansehen.
4. **Logbuch-Vorschlag** (Version beim Anwender zu erfragen): „Die Dialoge ‚Projekt öffnen‘ und
   ‚Speichern unter‘ öffnen in der Größe ihres Inhalts.“

## Nachtrag #545: Diagramm-Zoom

Anwendermeldung 26.09.2026: Ziehen mit der Maus im Diagramm markiert Text, statt den Bereich zu
zoomen. Commit `51997a8b3` auf `ios_migration_september`; kein Schemaschritt, Renderer unberührt.

**Ursache.** Zwei Fehler. (1) In allen Diagrammen des SVG-Wegs fehlte an
`.epos-diagramm-svg-flaeche` die Regel `user-select: none`, und `EPOS.UI/wwwroot/epos-diagramm.js`
fing `selectstart` nicht ab — der Browser begann beim Ziehen eine Textauswahl. (2) Nur im
Gebäudedialog: `DiagrammSvg` band die Zeigerhandler einmal je Komponente; nach dem Platzhalter
(unbeheizte Zone) und der Rückwahl stand eine neue Fläche ohne Handler.

**Behebung.** CSS: `user-select`, `-webkit-user-select` und `-webkit-touch-callout: none` an der
Fläche; `.epos-farbwahl` bleibt markierbar. JS: `selectstart` wird abgefangen; `pointerdown` bleibt
bewusst ohne `preventDefault`, damit Fokus und Tastenbedienung erhalten bleiben.
`DiagrammSvg.razor` bindet neu, sobald eine neue Fläche gezeichnet ist, und merkt die Fläche vor
dem ersten `await`, damit derselbe Zeichenlauf nicht doppelt bindet.

**Reproduktion.** Beide Fehler mit Playwright/Chromium nachgestellt und nach der Behebung
gegengeprüft; die Skripte liegen nur im Scratchpad, nicht im Repositorium.

**Tests.** `StilblattTests.Diagrammflaeche_markiert_beim_Ziehen_keinen_Text`,
`DiagrammSvgTests.DS7_Nach_dem_Platzhalter_wird_die_neue_Flaeche_gebunden`,
`DiagrammSvgTests.DS7_Derselbe_Zeichenlauf_bindet_nicht_doppelt`. Gate auf `51997a8b3`:
Kern-Filter Release 0 Fehler; voller Lauf 0 Fehler — EPOS.Kern.Tests 8 239 erfolgreich /
1 übersprungen, EPOS.UI.Tests 6 577, KiKern.Tests 549, SpeicherEngine.Tests 386,
SpeicherPlanung.Tests 27 / 1 übersprungen. Keine ChartProben (Renderer unberührt), kein
Referenzlauf, kein iOS-Lauf.

**Offen.** Abnahme in der Windows-Schale durch den Anwender: ziehen mit und ohne „Bereich“, im
Gebäudedialog Zone wechseln und zurück, erneut ziehen. Logbuch-Satz als Fehlerbehebung (Version
beim Anwender zu erfragen): „Im Diagramm markiert das Ziehen mit der Maus keinen Text mehr,
sondern zoomt den Bereich.“

## Nachtrag #550: Dialogdesign-Nachlese

Drei Anwendermeldungen vom 26.09.2026 aus der Arbeit am Gerät, dazu zwei Analysen ohne
Codeänderung. Zweig `ios_migration_september` im Hauptbaum; kein Schemaschritt, Testdatenbank
unberührt, kein Rechenweg berührt, kein Referenzlauf, iOS-Hülle nicht berührt.

### 1. Speichern-Rückmeldung (`b2f470775`, `9c517d994`, Merge `f508783d9`)

**Ursache.** Die Energiekostenverwaltung (Energieträger-Dialog) schrieb in `BeiSpeichern` den
Vermerk „gespeichert … Uhr“ nur in die Kontextzeile oben; bei gerollter Karte sah der Anwender ihn
nie. Die Durchsicht aller Dialoge mit Speichern-Knopf fand dasselbe Muster an weiteren Stellen:
Ablehnungen der Verwaltungen standen nur im Band oben, „Speichern“ ohne Änderung war hart gesperrt.

**Behebung.** Energieträger: „Gespeichert um …“ in der Statusspanne der `SpeichernLeiste` und
neben dem Knopf der Preishistorie, ein Fehlschlag rot an beiden Stellen; jede Eingabe, jedes
Nachladen, ein Trägerwechsel und „Gültig ab“ nehmen den Vermerk zurück. Die weiche Sperre nach
`EPOS.UI/CLAUDE.md` steht zentral in `SpeichernLeiste` (`aria-disabled` statt `disabled`, ein
Klick ohne Änderung nennt den Grund mit dem Kurztext `ADM_TIP_SPEICHERN_UNVERAENDERT`) und am
Knopf der Preishistorie; sie wirkt in Energieträger, Kostenkomponente, Nutzungsdauer und
Pufferspeicher (Projekt). Acht Verwaltungen mit eigener Fußleiste — Baustoff, Bauteilaufbau,
BedarfAdmin, GebaeudeAdmin, Gebaeudetyp, KatalogBrowser, ModulKatalog, WaermepumpeStamm — zeigen
eine Ablehnung (Schloss, Prüfregel, Schreibfehler) zusätzlich rot in der Statuszeile am Knopf.
Kostenkomponente „Speichern unter…“ meldet den Erfolg, ein Fehlschlag steht rot;
PufferSpProjektDialog meldet „Anlegen“/„Übernehmen“ und ihre Ablehnungen in der Statuszeile. Die
Katalog-Editoren schließen bei Erfolg — dort ist kein Vermerk nötig.

**Tests.** Drei bunit-Tests zum Energieträger (Leiste, Preishistorie, Ablehnung); neue bzw.
erweiterte Tests je Dialog, Nutzungsdauer-, SpeichernLeiste- und Energieträger-Tests auf die
weiche Sperre — 13 UI-Dateien, 13 Testdateien.

### 2. Verlaufsgrafik der Wirtschaftlichkeit (`85a312191`, Merge `ffba06a27`)

**Ursache.** `KapitalwertVerlaufAbschnitt.razor` gab bei jeder neuen Fassung die gewählten Stände
der alten Ansicht als Wahl an die Hülle, die sie mit den angebotenen Ständen schneidet. Ein Stand,
den die alte Ansicht nicht kannte (neu in der Gruppe, frisch simuliert, erste Rechnung nach der
leeren Ansicht mit `GewaehlteStaende = []`), blieb dauerhaft ungehakt; fiel danach die andere
Variante aus der Gruppe, zeigte der Verlauf „Keine berechenbaren Reihen“.

**Behebung.** Abgleich über die Projekt-Id nach dem Zeichnen und nach „Aktualisieren“: gewählt
bleibt, was gewählt war, neu Angebotenes wird angehakt, Entfallenes entfällt, nie leer, solange die
Gruppe Stände anbietet. „Verlauf nach Excel…“ nimmt dieselbe Wahl. Kern und Hülle unberührt.

**Tests.** Vier bunit-Tests, drei davon vorher rot.

### 3. Word-Bericht: Barwertverlauf (`30b35b25d`, Merge `763e22e1c`)

**Ursache.** Der Wortbericht bettet seit DG-E3d das Bildschirm-SVG ein. Dort stehen die Reihen im
inneren `<svg class="epos-flaeche">` in Datenkoordinaten (20 Jahre auf rund 1 090 px,
`preserveAspectRatio="none"`, waagerecht etwa Faktor 50); die Strichstärke hält allein
`vector-effect="non-scaling-stroke"`. Word kennt `vector-effect` nicht und dehnt Strichbreite und
Strichfolge mit — die Barwertlinien werden zu breiten, gestreiften Bändern. Browser und PNG waren
richtig.

**Behebung.** `SvgSchreiber.Druckbaum`/`Drucktext` schreiben die Reihen als die Pixelpfade, die
auch das PNG malt (kein inneres svg, kein `vector-effect`). Wortbericht und Berichtsvorlagenbilder
nehmen `Drucktext`, der Bildschirmweg bleibt unverändert.

**Tests.** `WordBerichtSvgWacheTests` prüft den Drucktext; neue ChartProben-Probe
`svg_kapitalwert_szenarien_druck`, `--svg-alle` schreibt zusätzlich `_druck.svg`. PNG-Hashes und
alle Bildschirm-SVG byte-gleich. Sichtnachweis mit Edge headless (Bilder nur im Scratchpad); im
echten Word nicht geprüft.

### 4. Analyse: ValERI-Bewertung „Test: Wirtschaftlichkeit“ (1071–1073)

Ohne Codeänderung. Die Bewertung rechnet korrekt. Die Spanne ±15 % liegt als Szenariopreis nur am
Träger Stadtgas (`energy_project_settings`, Schritt 117) und hebt sich in ΔKW auf, weil alle Stände
dieselbe Gasmenge haben. Die Restspanne von 120 € bzw. 12 € stammt aus Zins und p_E auf die
Strom-Mehrkosten. „Strombedarf ohne Verwendung“ macht den Gasstamm stromkostenfrei; die PV- und
Speichervarianten erscheinen dadurch als Mehrkosten. **Anwenderentscheid offen:** Regel je
Vergleichsgruppe statt je Stand. **Nebenbefunde:** Gas-Grundpreis 100 gegen 120 €/a, Preisbasis
„kWh“ gegen Nm³, absolute Szenariopreise.

### 5. Analyse: VDI 6007 gegen Tagesbilanz (Projekt 1062)

Ohne Codeänderung. Die Temperaturreihen beider Wege sind identisch; der um rund 31 % höhere
Jahreswärmebedarf nach VDI 6007 ist modellbedingt. Das gemeldete Bild zeigte nur einen Ausschnitt.

### Zahlen und Abnahme

Gate auf `c195d0487`: Kern-Filter Release 0 Fehler; voller Lauf 0 Fehler — EPOS.Kern.Tests
8 284 erfolgreich / 1 übersprungen, EPOS.UI.Tests 6 677, KiKern.Tests 549, SpeicherEngine.Tests 386,
SpeicherPlanung.Tests 27 / 1 übersprungen. ChartProben 219 Bilder, 0 Verstöße. Windows-Schale ohne
Übersetzungsfehler, Kopierschritt gesperrt (MSB3021/MSB3027, laufende `EPOS_Plan.exe` und Visual
Studio). Designer unverändert (12 404 Einträge, +0). Kein Referenzlauf (kein Rechenweg), kein
iOS-Lauf (iOS-Hülle nicht berührt).

### Offen

Siehe „Nach #550“ in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md):
sechs Erzeuger-Projektdialoge („Felder speichern“ im Aufklapper meldet nur im Band oben),
Startseite Klimaregion und WirtschaftlichkeitSeite Bewertung, „Speichern unter“ in
WaermebedarfExtern und StromganglinieDialog, Verwaltungen (Vermerk bis zur nächsten Meldung,
harte Sperre ohne Änderung), `Leer()` der Hülle, Sichtprüfung im echten Word, ValERI-Entscheid
samt Nebenbefunden, Logbuch-Versionen.

## Nachtrag #554: Speichervermerk

Statuszeile #554 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md). Anlass: „Nach #550“ (a)
und (b) — „Felder speichern“ im Aufklapper der Erzeuger-Projektdialoge meldete nur im Band oben, Startseite
Klimaregion und „Bewertung speichern“ der Wirtschaftlichkeit ohne Vermerk am Knopf.

### Baustein `Speichervermerk` (`9a701ee6e`)

Speichern-Knopf außerhalb der Fußleiste mit Statusspanne daneben (`role="status"`): „Gespeichert um …“
(`ADM_STATUS_GESPEICHERT`) oder der Grund rot; `Gespeichert()`, `Fehler(text)`, `Leeren()`, über `InvokeAsync`
auch vom Assistenten aus. Ohne Änderung weich gesperrt (`aria-disabled`, Kurztext
`ADM_TIP_SPEICHERN_UNVERAENDERT`), ein Klick nennt den Grund; eine Fehleingabe sperrt hart. Eine neue Änderung
oder ein Satzwechsel nimmt den Vermerk zurück. `SpeichernLeiste` bezieht Vermerk- und Sperrwortlaut aus dem
Baustein; Stilregeln `.epos-speichervermerk`, `.epos-status--amknopf`. Neun bunit-Tests.

### Einsatz

| Ort | Commit | Verhalten |
|---|---|---|
| BHKW, Heizkessel, Photovoltaik, Pufferspeicher, Stromspeicher — „Alle Daten“ → „Speichern“ | `b2dfbe282` | Erfolg am Knopf statt als Band; Ablehnung Band plus Grund am Knopf; der Assistent erhält die Katalogmeldung (`_felderErfolg`) |
| Solarkollektoren (Projekt) | `5ac5192b6`, Nachzug `3739b934a` | wie oben; der Nachzug gibt dem Assistenten nach erfolgreichem Speichern die Katalogmeldung statt eines leeren Textes |
| Startseite, Klimaregion | `b3d15008f` | Vermerk am Knopf, auch auf dem Weg des Assistenten; weich gesperrt, solange die gewählte der gespeicherten Region entspricht; ohne offenes Projekt hart |
| Wirtschaftlichkeit, „Bewertung speichern“ | `b3d15008f` | Vermerk am Knopf; weich gesperrt bis zur nächsten Eingabe in der Wirkungsliste; Fehlschlag zusätzlich rot am Knopf |

### Zahlen und Abnahme

Gate auf `6ac7f4f91`: Kern-Filter Release 0 Fehler; voller Lauf 0 Fehler — EPOS.Kern.Tests 8 367 erfolgreich /
1 übersprungen, EPOS.UI.Tests 6 713, KiKern.Tests 549, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27 /
1 übersprungen. ChartProben 220 Bilder, 0 Verstöße; Referenzlauf der sechs CI-Projekte gegen R21 PASS; Windows-Schale
0 Fehler; Designer unverändert. Kein iOS-Lauf (iOS-Hülle nicht berührt).

### Offen

Siehe „Nach #554“ in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md): eine Feldänderung über den
Assistenten leert den Vermerk erst beim nächsten Speichern; die weiche Sperre von „Bewertung speichern“ greift erst
nach dem ersten Speichern; „Speichern unter“ in WaermebedarfExtern und StromganglinieDialog sowie die Verwaltungen
aus „Nach #550“ (c)/(d).

## Nachtrag #562: Zoom nach Reihenwechsel

Anwendermeldung 26.09.2026: „Ergebnis-Chart lässt sich nicht zoomen“ (Wärmeganglinie, Heizkessel
abgewählt). Commit `f988eeb43`, Merge `73e550e55` (Betreff „(#558)“, die Nummer war während der Welle
anderweitig belegt); kein Schemaschritt, Renderer unberührt.

**Ursache.** `DiagrammSvg` gibt die Kinder des Bildes positionsbasiert aus (`OpenRegion(6 + i)`);
Legendeneinträge und y-Teilung stehen vor der Datenfläche. Ein anderer Reihen-Haken oder ein neuer
Lauf mit anderer y-Teilung verschiebt die Datenfläche; an ihrer neuen Stelle stand vorher ein anderes
Element, und Blazor setzt ein neues inneres `svg` ein. Die Fläche und ihre Id bleiben, das Binden aus
Nachtrag #545 kommt nicht wieder — `EPOS.UI/wwwroot/epos-diagramm.js` hielt das abgehängte alte
`svg`, Rad, Ziehen und Bereich wirkten ins Leere.

**Behebung.** Das Modul schlägt das innere `svg` je Zugriff nach (Getter mit
`isConnected`/`contains`). Die neue Ausfuhr `nachziehen(flaeche)` legt nach einer neuen Instanz
desselben Bildes den zuletzt gesetzten Ausschnitt wieder auf; ein anderes Bild setzt wie zuvor
zurück. `DiagrammSvg.OnAfterRenderAsync` ruft je nach Fall `nachziehen` oder `Zuruecksetzen`. Gilt für
alle Diagramme des SVG-Wegs.

**Proben.** bunit `DS7_Ein_anderer_Lauf_verschiebt_die_Datenflaeche_im_Bild`,
`DS7_Ein_neuer_Lauf_zieht_den_Ausschnitt_nach`, `DS7_Ein_anderer_Haken_setzt_den_Zoom_zurueck`;
Chromium-Probe mit dem alten Skript 2 von 5 rot, mit dem neuen 5 von 5 grün. ChartProben 220 Bilder,
0 Verstöße (Bilder unverändert). WebView2 und WKWebView sind nicht real geprüft; siehe „Nach #562“ in
[`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md).

## Nachtrag #563: Simulationskonfiguration

Anwenderauftrag 26.09.2026: „Der Dialog ist unübersichtlich. Bringe die Abschnitte Wärmebedarf,
Kühlung, Anlagenkopplung nach unten.“ Commit `653b92842`, Merge `3c625b2b2`; kein Schemaschritt,
Rechenweg unberührt.

**Aufbau.** `SimulationKonfigSeite.razor` zeigt oben die Komponenten der Simulation und die Speicher im
Projekt, darunter die Fußzeile mit Booster und „Konfiguration speichern“ samt Banner. Netzverluste,
Kühlung rechnen und Anlagenkopplung stehen nicht mehr als drei volle Balken über den Komponenten,
sondern im kompakten Block „Weitere Einstellungen“ (`SIMKONF_GRP_WEITERE`, de/en): ein Gruppenkopf,
je Einstellung Formularraster mit Herleitungszeile auf der Feldkante, Klappliste gedeckelt, eigenes
`fieldset` für die Sperre (CSS `.epos-simkonfig-einstellungen`). Bindungen, Schreibwege und die
Anmeldung beim Assistenten sind unverändert.

**Abnahme.** bunit `Komponenten_und_Speicherknopf_stehen_vor_den_weiteren_Einstellungen`; Wiki-Quellen
Simulation und Kühlung nachgezogen. Offen: siehe „Nach #563“ in
[`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md).

## Nachtrag #564: Wechselrichter vorschlagen

Anwenderauftrag 26.09.2026: „Es sollte einen Button ‚Wechselrichter vorschlagen‘ geben, der aus dem
ausgewählten Katalog die geeigneten Wechselrichter vorschlägt, die übernommen werden können; im
Folgenden ‚Auslegung vorschlagen‘.“ Commits `d2819ed6f` (Kern), `3e3e17164` (Hülle und Oberfläche),
`503219d39` (Wiki-Quelle Photovoltaik), `c46996545` (Stromgrenze als eigener Grund), Merge
`b52ddb3d3`; kein Schemaschritt, Simulationsrechenweg unberührt.

**Regeln.** `WechselrichterVorschlag` bewertet jedes Katalogsgerät gegen Modul, Modulzahl und die
Auslegungstemperaturen des Projekts über die vorhandene `StrangAuslegung` (neue Überladung
`Vorschlagen(…, tKalt, tHeiss)`). Stufen: *geeignet* (alle Module untergebracht, alle Grenzen
geprüft, DC/AC 1,0–1,3), *bedingt* (DC/AC bis 1,5, Restmodule, fehlende AC-Nennleistung oder
Spannungsgrenzen), *ungeeignet* (kein Modul oder keine Modulzahl, keine Reihenlänge im
Spannungsfenster, zu klein über DC/AC 1,5 oder `P_DC_Max`, zu groß unter 1,0, Stromgrenze je
Tracker, keine Aufteilung). Restmodule: größte aufgehende Modulzahl darunter, höchstens eine
Reihenlänge weniger.

**Rangfolge.** Stufe → Abstand DC/AC zum Band 1,1–1,2 → wenige Geräte → wenige Restmodule →
Name/Id.

**Beispiel (Testdatenbank).** Modul 21 Philadelphia Solar 530 W, 10 Module: Muster 2500TL
ungeeignet „Strangstrom 13,7 A über Grenze je MPPT 12,0 A“; mit `I_Sc_Max` 15 A geeignet
2 × (1 × 5), DC/AC 1,06. Ablytek 6MN6A275, 10 Module: geeignet 1 × (1 × 10), DC/AC 1,10.

**Bauart.** Kern ohne Datenbank; die Windows-Hülle `PhotovoltaikHuelle` liefert über den Delegaten
`WechselrichterVorschlagen` die Kandidaten des Katalogs (nach Hersteller gefiltert) und die
Projekttemperaturen; „Auslegung vorschlagen“ rechnet jetzt ebenfalls mit den
Auslegungstemperaturen des Projekts. `PhotovoltaikDialog.razor`, `PvStrangDaten.cs`,
`PvStraengeFelder.razor`: Knopf neben der Katalogwahl, weich gesperrt ohne Modul oder Modulzahl;
Überlagerung mit Rangliste und farbigen Bewertungschips, Zeile = Wahl, „Übernehmen“ setzt Gerät und
Herstellerfilter, Abbrechen/✕/Esc ändern nichts, nichts wird geschrieben; „Auslegung vorschlagen“
weich gesperrt mit Grund statt `disabled`; CSS in `epos-ui.css`. 39 Ressourcen `PVS_WRV_*`,
`PVS_BTN_WRVORSCHLAG`, `PVS_VORSCHLAG_SPERRE_GERAET` (de/en).

**Tests.** `WechselrichterVorschlagTests` (10), `PvWechselrichterVorschlagTests` (8),
`PvStraengeFelderTests` angepasst. Konzept: Abschnitt „Wechselrichtervorschlag“ in
[`Doku_PV_Strangauslegung_EPOS-Plan.md`](../../../aktuell/Doku_PV_Strangauslegung_EPOS-Plan.md).

**Offen.** CEC-Liste ohne `Anzahl_Mppt`/`Straenge_Je_Mppt`/`I_Sc_Max` — der Vorschlag rechnet dann
mit einem Tracker und dem Betriebsstrom als Grenze und stuft oft „ungeeignet wegen Strom“ ein; die
alte Klappliste bewertet weiter mit festen Vorgabetemperaturen („passt nicht“); keine Sichtprüfung
in der App. Siehe „Nach #564“ in
[`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md).

## Nachtrag #565: PV-Modulliste, Gerätegrenze, Berichtsseite

Drei Anwenderaufträge vom 26.09.2026 der Dialogdesign-Nachlese; kein Schemaschritt, Testdatenbank
unverändert. Commits `bdc654ecb` (Merge `4c1f7c164`), `097792504` (Merge `52ffad6f5`), `ebeddca0c`
(Merge `3043a5cb0`).

### PV-Stränge: Modulliste nur Projektmodule

**Ursache.** Anwender: „Bei den Modulen (Dropdown) sollten nur die dem Projekt zugeordneten
auswählbar sein.“ Die Klappliste „Modul“ je Strang bot den ganzen Modulkatalog der Hülle
(`PhotovoltaikHuelle`).

**Umsetzung.** `PhotovoltaikDialog.ProjektmoduleFuerStraenge()` filtert den Katalog bei jedem
Zeichnen gegen die Projektliste (Stamm-Id `GeraetId`), in der Reihenfolge der Projektliste und jedes
Modul einmal; neben „(Modul der Anlage)“ stehen nur diese Module. Ein Strangmodul außerhalb des
Projekts bleibt als „… (nicht mehr im Projekt)“ stehen (`PVS_MODUL_NICHT_IM_PROJEKT`, de/en). Die
Liste folgt Zuordnen und Entfernen im selben Arbeitsstand. Wiki-Quelle Photovoltaik ergänzt.

**Tests.** Vier bunit-Fälle in `PhotovoltaikDialogTests`.

**Offen.** Ein Strang mit entferntem Modul fällt nicht von selbst auf „(Modul der Anlage)“ zurück.

### Wechselrichtervorschlag: mehr als vier Geräte bedingt

**Ursache.** Anwender: „Setze um: Geräte mit sehr vielen Einheiten (> 4) als ‚bedingt‘ zu stufen.“
Mikrowechselrichter erreichten bei großen Feldern die Stufe *geeignet*, obwohl die Aufteilung zehn
und mehr Geräte verlangt.

**Umsetzung.** `WechselrichterVorschlag.MAX_GERAETE_GEEIGNET = 4` und Grund `VieleGeraete`
(„n Geräte nötig (mehr als 4)“, `PVS_WRV_GRUND_VIELE_GERAETE`, de/en): eine Aufteilung mit mehr als
vier Geräten ist höchstens *bedingt*; die Rangfolge bleibt. `StrangAuslegung.GeraeteBewerten` kennt
keine Stufen und ordnet schon nach Gerätezahl — unverändert. Konzept
[`Doku_PV_Strangauslegung_EPOS-Plan.md`](../../../aktuell/Doku_PV_Strangauslegung_EPOS-Plan.md),
Abschnitt „Wechselrichtervorschlag“ (Stufentabelle und Absatz Gerätegrenze), und Wiki-Quelle
Photovoltaik ergänzt; die beiden Textkonflikte mit den Papieren von #564 sind inhaltlich
zusammengeführt.

**Tests.** Zwei neue Fälle in `WechselrichterVorschlagTests`: Mikrowechselrichter mit 30 Modulen,
10 × (3 × 1) → *bedingt* mit Grund; Grenze vier/fünf Geräte; Rang hinter einem geeigneten
6,4-kW-Gerät.

**Offen.** Keine Sichtprüfung in der App.

### Berichtsseite: kurze Erfolgszeile, Hinweise einklappbar

**Ursache.** Anwender: „Der blaue Text ist nicht hilfreich. Entweder nicht mehr anzeigen oder nur
optional.“ Nach „Bericht erstellen“ stand ein langer Fließtext mit allen Laufmeldungen.

**Umsetzung.** `BerichtSeite.razor`: grünes Band „Bericht erstellt: <Datei> — Vorlage ‚…‘“ mit
„Öffnen“ (Delegat `DateiOeffnen`, Pfad als Kurztext, verfällt nach 20 s); Warnungen einzeln sichtbar
(gelb); Klappzeile „n Hinweise zum Bericht — anzeigen“ im Stil des Simulations-Laufbands, Vorgabe
eingeklappt, gegliedert nach Berichtslauf / Alle Stände / je Stand / Word / Excel / Vorprüfung;
„Platzhalter ohne Wert“ als eigener Punkt mit Verweis auf den Katalog. Kern:
`EPOS.Kern/Allgemein/Bericht/Berichtshinweise.cs` (`Gruppiere`; „Alle Stände“ nur, wenn ein Hinweis
in jedem Stand und in mindestens zwei Ständen vorkommt), `BerichtsDaten.Hinweisliste` und `Melde`
(Stand, Stufe, Text) neben `Warnungen`, das weiter Bericht, Excel und Vorlagenfelder speist; Quellen
`BerichtsDatenSammler` (Laufmeldungen, Wirtschaftlichkeit) und `BerichtCtrl`. Hülle
`BerichtSeiteGaben.Gliedere`. 12 Ressourcen `BK_BER_ERG_*`/`BK_BER_HINWEIS_*` (de/en); Wiki-Quelle
Berichtsvorlagen ergänzt. Nebenwirkung: fehlende Kostensätze der Speicherflotte meldet der Lauf als
Warnung statt als Hinweis (`SimulationControl.Stromspeicher.cs`), sichtbar auch auf der
Simulationsseite; der Referenzlauf der sieben CI-Projekte gegen R22 ist PASS.

**Tests.** `BerichtSeiteErgebnisTests` (10), `BerichtshinweiseTests` (4), `BerichtsvorlagenHuelleTests`
ergänzt.

**Offen.** Die Rückfrage „Bericht öffnen?“ nach dem Lauf steht doppelt zu „Öffnen“; die Erfolgszeile
nennt den Grund der Vorlagenwahl nicht mehr; die Warnstufe der Flotten-Kostensätze ist vom Anwender zu
bestätigen. Siehe „Nach #565“ in
[`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md).

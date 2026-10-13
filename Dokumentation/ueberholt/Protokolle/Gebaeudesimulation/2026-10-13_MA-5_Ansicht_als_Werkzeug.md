# MA-5 — Ansicht als Werkzeug

Kurzprotokoll, 13.10.2026. Grundlage: [Entwurf Geführter Modellaufbau](../../../aktuell/Gebaeudesimulation/2026-10-12_Entwurf_Gefuehrter_Modellaufbau.md)
Abschnitte 2.2, 2.5 und Wellenplan Zeile MA-5 (E137). Vorläufer: [AB4](2026-10-13_AB4_Ein_Ausblenden.md) (Sichtlegende),
[MA-1a](2026-10-13_MA-1a_Modellpruefung_Kern.md), [MA-1b](2026-10-13_MA-1b_Modellpruefung_Oberflaeche.md) (Ampelblatt, Übernahme
mit Rücknahme), [MA-2](2026-10-13_MA-2_Nutzung_und_Kalender.md) (`ZonenNutzungCtrl`). Ziel: Kontrolle am Körper statt in Listen.

## Befund

| Stand vor MA-5 | Folge für den Bau |
|---|---|
| „Ansicht…“ im Ampelblatt öffnet die Ansicht des Neulesens im Farbmodus des Sprungziels (`FarbmodusVorgabe`), ohne die Befunde zu kennen. | Neuer Parameter `AnsichtGruppe` am Blatt: Der Gebäudedialog bekommt die Gruppe und reicht ihre Befunde als `Pruefung` an die Ansicht. Der alte Weg über das Sprungziel bleibt. |
| Die Befunde tragen im Kern `IdBauteil` und `IdZone`, die Befundzeile des Blatts nur Texte. Die Ansicht kennt Bauteile über die Kennung der Datei, Zonen über den Schlüssel des Zonenplans. | Befundzeile mit `IdZone`, `IdBauteil`. Die Ansicht des Neulesens trägt `BauteilIds` (Kennung → `Tab_Bauteil.ID`, aus den Paaren über `Tab_Importzuordnung`, die BA-3 schon bildet) und `ZonenIds` (Zonenschlüssel → `Tab_Zone.ID`, über den Zonennamen). |
| Der Kern übernimmt je Gruppe, nicht je Auswahl, und kennt kein Verwerfen. | `ModellpruefungCtrl.Uebernehmen(gruppe, auswahl)` (derselbe Weg, die Rücknahme der Gruppe sammelt alle Teilübernahmen) und `Verwerfen(gruppe, auswahl)` (Sitzungsgedächtnis, schreibt nichts: der Befund bleibt, sein Vorschlag fällt aus Zählung und „Alle übernehmen“). |
| Eine Ampelleiste im Reiter „Zonen“ gibt es nicht (MA-1b, offen). | Der Sprung steht am Ampelblatt; die Leiste im Reiter bleibt offen. |
| Der Wirt der Rasterprobe trägt die Gebäudeansicht nicht; eine eingecheckte Probe des Körpermoduls fehlt (AB4). | Browserprüfung des Moduls wie in AB4, mit Gegenprobe gegen das Modul der Basis (unten). |

## Bau

- **Sprung mit Hervorhebung** (`GebaeudeDialog`, `Modellpruefungsblatt`): „Ansicht…“ einer Gruppe öffnet über „Datei erneut lesen“
  im Farbmodus der Gruppe (1 Randbedingung, 2 Aufbau, 3 **Nutzung**, 4 und 5 Befund; ohne gelesene Zonen fällt „Nutzung“ auf
  „Zonen“ zurück) und setzt `GebaeudeAnsicht.Pruefung` (Gruppe, Name, Befundzeilen). „Datei erneut lesen“ am Satz und der alte
  Sprungweg öffnen ohne Prüfung.
- **Hervorhebung** (`GebaeudeAnsichtHervorhebung`): getroffen sind die Bauteile der Befunde und die Zonen der Befunde ohne
  Bauteil; daraus die Räume (Zone getroffen oder ein Dreieck, eine Kante oder eine Öffnung des Bauteils). Grundriss: getroffene
  Räume mit Rand in `--epos-grundriss-hervor` (der bestehende Token der Hervorhebung), übrige gedimmt; Kanten getroffener Bauteile
  breiter mit Schein derselben Farbe. Szene: `hervorhebung`, je Prisma und Dateikörper `hervor`, `hervorBauteile`; das Modul zieht
  die Kanten getroffener Körper dunkel (wie die Zielzone) und zeichnet die übrigen durchscheinend (Deckung 0,18, ohne depthWrite)
  in ihrer Farbe aus der bestehenden Farbtafel. Kein neues Farbsystem, kein neuer Farbwert im Stilblatt.
- **Prüfzeile** über dem Bild: „Hervorgehoben: Gruppe — n Bauteile, m Zonen“ bzw. „Kein Befund dieser Gruppe liegt auf einer
  Fläche oder Zone der Ansicht“, dazu Zahl, „Übernehmen“ und „Verwerfen“ für alle Vorschläge der Gruppe in der Ansicht.
- **Legende**: Spalte „Vorschläge“ in den Legenden Aufbau (je Stufe), Befund (je Befund) und Nutzung (je Profil, „ohne Profil“)
  mit Zahl und den beiden Knöpfen; nur mit Prüfung und mindestens einem Vorschlag, Knöpfe nur mit Delegat des Wirts. Der Wirt führt
  die Auswahl über den Weg des Blatts aus (`ModellpruefungWeg.UebernehmenAuswahl`/`VerwerfenAuswahl`), meldet das Ergebnis, liest
  Stufen, Befunde, Zonen und Nutzung neu (`GebaeudeNeulesestand.AnsichtNeuLesen`, ohne die Datei erneut zu lesen) — die Ansicht
  zeichnet neu —, setzt die neue Prüfung und frischt Ampelblatt und Zeile auf. „Zurücknehmen“ steht im Ampelblatt.
- **Farbmodus „Nutzung“**: fünfter Knopf, nur wenn die Zonen des gespeicherten Gebäudes gelesen sind (`NutzungWaehlbar`; der
  Importvorschlag kennt keine Profile). Zonen nach Profil in der Zonenpalette (Stelle nach Kategorie und Name), ohne Profil in der
  Farbe „ohne Zone“; Legende mit Profil, Kategorie und Zahl der Zonen. Daten aus `ZonenNutzungCtrl.Lesen` über die Hülle
  (`GebaeudeNeulesenHuelle.MitNutzung`). Kein Klick ordnet in diesem Modus zu, die Werkzeugwahl entfällt wie in den Gruppenmodi.
- **Ressourcen**: 13 Schlüssel `GANS_*` und 2 `MP_MSG_*` je Sprache; Designer neu, zweiter Lauf +0.
- **Nachzug MA-2 (fremd, im Bestand rot)**: `VorlagenfeldStilblattTests` fand `var(--epos-rahmen, #ccc)` in den Regeln
  `epos-zonennutzung-*` (Rückfall entfernt); `KiMaskenabdeckungWacheTests` vermisste `ZonenNutzungsblatt` (Zeile in `WIRTE` mit
  Wirt `GebaeudeKatalogDialog`, Zeile in `EINGABESTELLEN` mit 7 Stellen und Grund).

Kein Schemaschritt, keine neue SQL-Anweisung im Produktcode, keine Leseregel des Rechenwegs. Verwerfen schreibt nichts; Übernehmen
schreibt nur über den Bestandsweg der Modellprüfung. Referenzprojekte werden nicht beschrieben.

## Dateien

- neu: `EPOS.UI/Dialoge/Bedarf/GebaeudeAnsichtPruefung.cs`; Tests `EPOS.UI.Tests/Bausteine/GebaeudeAnsichtPruefungTests.cs`,
  `EPOS.UI.Tests/Dialoge/GebaeudeDialogTests.Ansichtwerkzeug.cs`, `EPOS.Kern.Tests/ModellpruefungAuswahlTests.cs`
- geändert: `EPOS.Kern/Controller/ModellpruefungCtrl.cs`, `EPOS.Kern/MyResource/Resource.resx`, `Resource.en-US.resx`,
  `Resource.Designer.cs`; `EPOS.UI.Daten/Bedarf/ModellpruefungHuelle.cs`, `GebaeudeNeulesenHuelle.cs`, `GebaeudeAufbauHuelle.cs`;
  `EPOS.UI/Bausteine/GebaeudeAnsicht.razor`, `EPOS.UI/Dialoge/Bedarf/GebaeudeAnsichtDaten.cs`, `ModellpruefungDaten.cs`,
  `Modellpruefungsblatt.razor`, `GebaeudeDialog.razor`, `GebaeudeNeulesenDaten.cs`, `EPOS.UI/wwwroot/epos-gebaeude-koerper.js`,
  `EPOS.UI/wwwroot/epos-ui.css` (Abschnitt MA-5 vor dem Formularraster, nur Tokens); `EPOS.UI.Tests/Dialoge/Hilfe/KiMaskenabdeckungWacheTests.cs`

## Tests

- Bau: Kern-Filter 0 Fehler; Windows-Schale (`EnableWindowsTargeting=true`) 0 Fehler.
- `EPOS.UI.Tests` mit Filter `GebaeudeAnsicht|Modellpruefung|ZonenNutzung|VorlagenfeldStilblatt|KiMaskenabdeckung|KiKnopfEinmal|InfoknopfSchluessel|GebaeudeDialog|Formularraster|GebaeudeAufbau|GebaeudeBefund|Stilblatt|Ressource|KiDialogaufruf`:
  **446/446** grün, davon 12 neu — Ansicht 10 (Hervorhebung Raum und Kante mit Prüfzeile, Szene mit Hervorhebung, Gegenprobe ohne
  Prüfung, Aufbaulegende mit Zahl und Knöpfen je Stufe, keine Knöpfe ohne Delegat und keine Spalte ohne Vorschlag, Prüfzeile ohne
  Treffer, Farbmodus Nutzung in Grundriss, Legende und Szene, Nutzung ohne Zonen nicht wählbar, Nutzungslegende mit Vorschlägen,
  Auswahl Bauteil/Zone), Gebäudedialog 2 (Sprung Gruppe 2 mit Hervorhebung, Übernahme an der Legende mit Neulesen und Rücknahme im
  Ampelblatt; Sprung Gruppe 3 in „Nutzung“ mit Verwerfen).
- `EPOS.Kern.Tests` mit Filter `Modellpruefung|Dokumentation|Repository|Resource|GebaeudeNeulesen|GebaeudeAufbauHuelle|ZonenNutzung`:
  **66/66** grün, davon 4 neu (Kennungen je Befundzeile; Teilübernahme mit gesammelter Rücknahme; Verwerfen ohne Schreiben, „Alle
  übernehmen“ überspringt; Zuordnung von Zonen und Nutzung über den Namen).
- `designer_neu.py`: geschrieben, zweiter Lauf +0.

## Probe

- **Rasterprobe**: Nach `Proben/Rasterprobe/LIESMICH.md` gibt es für die Gebäudeansicht keine eingecheckte Probe; der Wirt trägt sie
  nicht (Befund AB4), `diagrammprobe.mjs` misst `DiagrammSvg`, nicht das Körpermodul. Kopf und Schlussleiste der Dialoge, Raster und
  Banner sind unberührt, deshalb liefen `fensterprobe`, `rasterprobe` und `bannerprobe` nicht.
- **Browserprüfung des Moduls** wie AB4 (Playwright 1.56 mit Chromium aus `/opt/pw-browsers`, headless, WebGL über SwiftShader;
  das echte `epos-gebaeude-koerper.js` mit three.js aus `wwwroot/three`, zwei Prismen, Zonenfarben als Tokens der Seite): **7/7
  erfüllt** — WebGL zeichnet; `hervorstufe` liefert hervor/gedimmt/ohne; mit Hervorhebung deckt der getroffene Körper 0,8, der
  übrige 0,18 ohne depthWrite; ohne Hervorhebung beide 0,8 wie bisher; dunkle Kanten am getroffenen Körper im Bild (2 gegen 0
  dunkle Pixel); der gedimmte Körper verliert über die Hälfte seiner Farbsumme (683 757 gegen 1 761 243), der getroffene bleibt
  innerhalb 10 %; „Nutzung“ färbt Profil und „fehlt“ verschieden. **Gegenprobe** mit dem Modul der Basis `7b5459b9c`: 4 von 7
  verletzt (alle Prüfpunkte der Hervorhebung), die Prüfpunkte ohne Hervorhebung und „Nutzung“ (Farbstelle aus der Szene) erfüllt.
  Seite und Läufer lagen im Scratchpad und sind nicht eingecheckt.

## Offene Punkte

- Windows-Sichtabnahme: Prüfzeile und Spalte „Vorschläge“ bei schmaler Breite, Schein der Kanten, Dimmung in beiden Farbschemata,
  Kontrastmodus.
- Die Ansicht braucht weiter die Importdatei („Datei erneut lesen“); die Ansicht des gespeicherten Modells ohne Datei bleibt ein
  eigener Auftrag (MA-1b offen).
- Ampelleiste im Reiter „Zonen“ des Editors gibt es nicht (MA-1b offen); der Sprung steht nur am Ampelblatt.
- Gruppe 1 hat bis MA-3 keine Vorschläge; „Bauteil ergänzen“ am Legendeneintrag „ohne Bauteil“ (Entwurf 2.5) folgt mit MA-3.
- Verwerfen gilt für die Sitzung (Instanz des Controllers wie die Rücknahme) und lässt sich nicht einzeln aufheben; ein neues
  Öffnen des Blatts zeigt die Vorschläge wieder.
- Zonen werden über den Namen zugeordnet; eine nach dem Import umbenannte Zone wird weder hervorgehoben noch in „Nutzung“ gefärbt.
- Eine eingecheckte Probe des Körpermoduls im Rasterprobe-Wirt fehlt weiter (Kandidat aus AB4).

## Logbuch-Vorschlag

„Aus ‚Modell prüfen‘ öffnet ‚Ansicht…‘ die Gebäudeansicht mit den betroffenen Flächen und Zonen hervorgehoben, Vorschläge lassen
sich an der Legende übernehmen oder verwerfen, und der neue Farbmodus ‚Nutzung‘ zeigt die Nutzungsprofile der Zonen.“

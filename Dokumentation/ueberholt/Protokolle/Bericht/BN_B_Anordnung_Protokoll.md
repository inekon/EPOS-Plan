# BN-B — Berichtsseite: Anordnung B in vier Karten (Protokoll)

Nachzug zu [`BN_A_Navigation_Protokoll.md`](BN_A_Navigation_Protokoll.md) (Variante A, #590) und
[`BN_Bereichszeile_Protokoll.md`](BN_Bereichszeile_Protokoll.md) (#757): dort stand nur die Reiterzeile selbst,
hier wird der Inhalt der Berichtsseite neu angeordnet. Konzept
[`Konzept_Berichtsseite_VALERI_Anordnung_EPOS-Plan.md`](../../../aktuell/Konzept_Berichtsseite_VALERI_Anordnung_EPOS-Plan.md)
entsteht parallel in einer anderen Sitzung und war beim Schreiben dieses Protokolls noch nicht im Baum — der
Verweis ist von der Orchestrierung nach dem Merge zu prüfen. Statusnummer **#761**. Der gültige Stand steht im Code
und im Stilblatt, hier steht, wie es geworden ist. Siehe [Statusdatei](../../../aktuell/Status_iOS_Migration.md).

Sitzung „Berichterstellung“, 06.10.2026, Opus 5.5 im Worktree `bs-layout`. Kein Schemaschritt, kein Rechenweg, keine
Referenzbasis berührt; `EPOS.iOS/` nicht berührt.

| Commit | Inhalt |
|---|---|
| `7ac2acd9d` | Berichtsseite: Anordnung B in vier Karten, rechte Spalte fest |
| `efdcce634` | Rasterprobe-Wirt: Berichtsseite mit Word-Vorlage und Prüfzeile |
| `cd92c29b9` | Berichtsseite: doppelte Gruppenbeschriftungen unter Kartentiteln entfernt |


## 1 Anlass

Anwenderwunsch 06.10.2026 mit Bildschirmfoto bei rund 1 890 px Breite: „Die Anordnung der Elemente kann optimiert
werden, zum Beispiel Dateiauswahl und Ausgabe auf die rechte Seite, prüfe gute Darstellung.“


## 2 Befund Ist-Stand

Ausgabe, Zielordner und „Erstellen“ standen in eigenen Zeilen *unter* dem Spaltenraster, nicht in einer der beiden
Spalten: Das Raster war so hoch wie die rechte Spalte (Vorlage, acht Häkchen, Szenario), die linke Spalte blieb bei
zwei Varianten darunter leer — die Leerfläche aus dem Foto. Die Ausgabewahl (Word/Excel/Beide) steuerte die Zeile
„Excel-Vorlage“ oben rechts, stand aber unten links: Ursache und Wirkung lagen diagonal. Die Lesereihenfolge war
links oben → rechts oben → rechts unten → zurück nach links unten; die Erklärzeile des Hauptknopfs stand in der
anderen Spalte, die Erfolgszeile erschien ganz oben, weit weg vom Knopf, der sie ausgelöst hatte. Die Hausregel
(`EPOS.UI/CLAUDE.md`) verlangt für einen Zweispalten-Reiter einen Bedienblock fester Breite mit einem Hauptknopf und
leiser Erklärzeile; die Seite hatte keinen — die Spalten liefen `auto-fit, minmax(320px, 1fr)`.


## 3 Varianten und Entscheid

Entwurf mit drei Varianten (Mockups `Bericht_Layout_A|B|C.html`, Bilder bei 1 890/1 280/820 px):

- **A — Lücke schließen:** zwei gleiche Spalten wie heute, links zusätzlich Ausgabe/Zielordner/Erstellen/Ergebnis.
  Billigster Umbau, löst aber die Lesereihenfolge nicht (Knopf vor Vorlage und Bausteinen) und braucht unter 900 px
  einen `order`-Kniff.
- **B — Konfiguration links, Ausgabe rechts:** links Karten „Varianten“ und „Inhalt“, rechts eine Spalte fester
  Breite mit Karten „Vorlage“ und „Ausgabe“ (Format, Zielordner, Erstellen mit Erklärzeile, Erfolgszeile, Hinweise).
  Lesereihenfolge Varianten → Inhalt → Vorlage → Ausgabe → Erstellen → Ergebnis, einzige Variante ohne `order`-Kniff
  unter 900 px, setzt die Hausregel Bedienblock um (Vorbild `epos-simreiter`).
- **C — drei Zonen:** Variantentabelle über die volle Breite, darunter drei gleiche Karten. Gleichwertig im Fluss,
  verliert aber bei vielen Varianten (Tabelle schiebt alle drei Karten nach unten) und bei 1 280 px (drei Spalten
  brechen um).

Bewertung in Kurzform: B gewinnt bei Lesefluss, Leerfläche, Nähe von Entscheidung und Auslösung, vielen Varianten,
schmalem Fenster und der Hausregel Bedienblock; A ist der billigste Sofortschritt, C gleichwertig im Fluss, aber
schwächer bei vielen Varianten. Anwenderentscheid 06.10.2026 „alle Empfehlungen“:

| Frage | Entscheid |
|---|---|
| BL‑Q1 Variante | **B** |
| BL‑Q2 Breite der rechten Spalte | **a** — fest `minmax(420px, 480px)` |
| BL‑Q3 Springende Optionsgruppe | **a** — Zeile „Excel-Vorlage“ in die Karte „Ausgabe“ unter die Optionsgruppe |
| BL‑Q4 Warnungen/Hinweise nach dem Lauf | **a** — in die Karte „Ausgabe“, nur der Seitenfehler bleibt oben |
| BL‑Q5 Kartentitel | **a** — „Varianten“, „Inhalt“, „Vorlage“, „Ausgabe“ |


## 4 Umsetzung

`EPOS.UI/Seiten/Berichte/BerichtSeite.razor` (nur Aufbau, kein Rechenweg): links Karte „Varianten“ (Variantentabelle
mit „Alle“/„Keine“) und Karte „Inhalt“ (Berichtsbausteine, Szenario der Wirtschaftlichkeit); rechts Karte „Vorlage“
(Block `epos-vorlage` unverändert mit Schloss, Leiste, Prüf- und Originalzeile) und Karte „Ausgabe“ (Optionsgruppe
Word/Excel/Beide, bei Excel/Beide die Zeile „Excel-Vorlage“ darunter, Zielordner, Fortschritt, Leiste „Erstellen“ mit
Erklärzeile, danach Laufmeldung, Erfolgszeile, Warnungen, Hinweisklappe). Oben bleibt nur der Seitenfehler und die
Vorbelegungszeile. `EPOS.UI/wwwroot/epos-ui.css`: neues Raster `minmax(0, 1fr) minmax(420px, 480px)`
(`.epos-bericht-raster`, `.epos-bericht-spalte` mit `--links`/`--rechts`), unter 900 px einspaltig in Markup-Folge
ohne `order`; Karten nur über vorhandene Token (`--epos-karte-*`, `--epos-ecke`, `--epos-schriftgroesse-kartentitel`)
mit `.epos-bericht-karte` (`--varianten`/`--inhalt`/`--vorlage`/`--ausgabe`) und `.epos-bericht-kartentitel`,
`forced-colors`-Kartenrahmen über `CanvasText`. Neue Ressourcen `BK_BER_KARTE_VARIANTEN/INHALT/VORLAGE/AUSGABE` in
`EPOS.Kern/MyResource/Resource.resx`, `Resource.en-US.resx`, Designer neu erzeugt.

**Nachzug (`cd92c29b9`):** Unter den Kartentiteln „Vorlage“ und „Ausgabe“ stand deren Name ein zweites Mal als
Gruppenbeschriftung „Vorlage:“/„Ausgabe:“. Die Gruppe der Vorlage trägt ihren Namen nur noch als `aria-label`; der
Baustein `EPOS.UI/Bausteine/Optionsgruppe.razor` bekommt den Schalter `TitelSichtbar` (Vorgabe `true`) und zeichnet
ohne `legend`, wenn er auf `false` steht — das `aria-label` bleibt für die Zugänglichkeit. Die Optionsgruppe kennt
keine waagerechte Anordnung; Word/Excel/Beide bleiben untereinander.

`Proben/Rasterprobe/Wirt/Seiten/Berichtekostenprobe.razor` (`efdcce634`): zwei Word-Vorlagen (eine mitgeliefert) und
eine Prüfzeile als Gaben, ohne Delegaten, damit die Karte „Vorlage“ der Anordnung B in der Sichtprobe realistisch
steht.


## 5 Tests

- `EPOS.UI.Tests/StilblattTests.cs`, neuer Fall
  `BL_B_Die_Berichtsseite_steht_in_zwei_Spalten_mit_fester_rechter_Breite`: Raster, feste rechte Spalte, Umbruch
  unter 900 px, keine Festfarben, `forced-colors`-Zweig.
- `EPOS.UI.Tests/Seiten/BerichtSeiteVorlagenTests.cs`: Fall „Die Gruppe steht über den Bausteinen …“ auf die neue
  Ordnung umgeschrieben; neuer Fall
  `BL_B_Vier_Karten_in_zwei_Spalten_und_die_Excelzeile_unter_der_Ausgabe` (Kartenfolge, Excel-Zeile in der Karte
  Ausgabe unter der Optionsgruppe).
- `EPOS.UI.Tests/Seiten/BerichtSeiteErgebnisTests.cs`, neuer Fall
  `BL_B_Das_Ergebnis_steht_in_der_Karte_Ausgabe_unter_Erstellen`: Erfolgszeile, Warnungen und Hinweisklappe in der
  Karte Ausgabe nach dem Knopf, nur der Seitenfehler bleibt oben.
- `EPOS.UI.Tests` 7 631/7 631 grün vor dem Nachzug, gezielt nach dem Nachzug (`--filter
  BerichtSeite|Stilblatt|Optionsgruppe`) 176/176 grün. `EPOS.Kern.Tests` Filter `Textbündel|Ressourcen` 71/71 grün.
  Kern-Filter Release 0 Fehler. Kein ChartProben-, SQL- oder Referenzlauf nötig (kein Rechenweg, kein SQL, kein
  Renderer); Windows-Schale nicht berührt; kein Schemaschritt.


## 6 Sichtprobe

`Proben/Rasterprobe/berichtekostenprobe.mjs`: Rückgabe 0, kein Verstoß. Eigene Messung bei 1 890 und 1 280 px: keine
Querrolle, rechte Spalte 480 px breit, Kartenfolge Varianten, Inhalt, Vorlage, Ausgabe; bei 820 px einspaltig in
derselben Folge (Markup-Reihenfolge = Lesefolge, kein `order`-Kniff nötig). Fotos liegen beim Anwender außerhalb des
Repos.


## 7 Wiki und Logbuch

Noch nicht nachgezogen: Die Wiki-Quelle „Berichtsvorlagen“ beschreibt die alte Anordnung nicht ausdrücklich, der
Abschnitt „Vorlage auf der Berichtsseite“ nennt aber die Lage der Gruppe und ist mit der neuen Anordnung abzugleichen.
Logbuch-Entwurf (Version beim Anwender erfragen): „Die Berichtsseite ordnet Ausgabe und Erstellen rechts neben der
Auswahl an.“ Beides folgt gebündelt mit der nächsten Sammelwelle (Konzept Hilfesystem 13.3).


## 8 Offen

- Sichtabnahme beim Anwender in der Windows-Anwendung (WebView2): Erfolgszeile nach „Erstellen“ in der Karte
  „Ausgabe“, Zeile „Excel-Vorlage“ bei Ausgabe Excel/Beide, „Durchsuchen…“ des Zielordners, Hochkontrastmodus der
  vier Karten.
- Wiki-Quelle „Berichtsvorlagen“ (Abschnitt „Vorlage auf der Berichtsseite“) und Logbuch-Satz, gebündelt mit der
  VALERI-Welle.
- Push-SHA und CI-Kennung nach dem Push nachtragen.
- Der Verweis auf `Konzept_Berichtsseite_VALERI_Anordnung_EPOS-Plan.md` oben ist zu prüfen, sobald dieses Papier im
  Baum steht — es entstand parallel in einer anderen Sitzung.

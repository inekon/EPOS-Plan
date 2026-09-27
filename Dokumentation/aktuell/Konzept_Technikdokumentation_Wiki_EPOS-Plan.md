# Technikdokumentation im Wiki — Grundlagen, Anwendung in EPOS-Plan, Kopplung an die App

Stand 27.09.2026 · Zweig `claude/wiki-help-assistant-docs-jllq1r` · Statuszeile #588 ·
Fortschreibung von [`Konzept_Hilfesystem_Wikidokumentation.md`](Konzept_Hilfesystem_Wikidokumentation.md)
(dessen Inhaltsregeln, Abschnitt 13, gelten unverändert).

## 1. Auftrag und Entscheide

Auftrag des Anwenders (27.09.2026), wörtlich:

> Ergänzung in Wiki und Kopplung der Dokumentation an die App: Zu jeder Technologie soll es eine
> Grundlagenbeschreibung geben, kurz und knapp: Wärmepumpe, Heizkessel, Solarthermie,
> Pufferspeicher, BHKW, …. Die Funktionsbeschreibung in EPOS-Plan sollte dann für jede Technologie
> mit einem konkreten Bezug erklärt werden. Wie in EPOS-Plan die Technologie eingebunden und
> konfiguriert wird. Konkrete Beispiele mit realen sinnvollen Größen sollten aufgezeigt und die
> konkrete Einstellung in EPOS-Plan erklärt werden. Dann soll der Einfluss von und auf andere
> Technologien dargestellt und erklärt werden sowie die Erklärung, wie diese Einflüsse in EPOS-Plan
> wirken. Erläutere jeweils die Fallstricke und was besonders zu beachten ist.
>
> Nachtrag: Wenn möglich und sinnvoll, erstelle auch veranschaulichende Diagramme und Grafiken.

| Kennung | Entscheid des Anwenders (27.09.2026) | Umsetzung |
|---|---|---|
| TD‑E1 | **Grundlagen getrennt:** `Grundlagen/<Technik>` wird kurz neu gefasst und bekommt eine Repo-Quelle; `Programm Dokumentation/<Technik>` bekommt Einbindung, Beispiel, Zusammenspiel und Fallstricke; die App lernt, Seiten der Rubrik Grundlagen zu öffnen | Abschnitte 3, 4, 7 |
| TD‑E2 | **Nur Sprungziele:** keine Einbettung der Seiten in den Programmkern; Hilfeknöpfe springen auf die Seiten und Abschnitte, der Assistent findet sie online über die Wiki-Suche | Abschnitt 7 |
| TD‑E3 | **Weiterleiten:** die älteren Seiten der Rubrik Programmfunktionen (Wärmepumpe, Heizkessel, Blockheizkraftwerk, Solarthermie, Pufferspeicher, Photovoltaik, Stromspeicher) werden beim nächsten Upload Weiterleitungen auf die Programm-Dokumentation-Seite | Abschnitt 8 |
| TD‑E4 | **Diagramme, wo sie etwas zeigen** (Nachtrag) | Abschnitt 6 |
| TD‑E5 | **SVG-Uploads im Wiki erlauben** (27.09.2026); die Freischaltung in `LocalSettings.php` nimmt der Anwender vor | Abschnitt 10 |
| TD‑E6 | **Grundlagen auch indirekt erreichbar:** über die Beschreibung, die der Reiter „Energieerzeuger" mit den Technik-Kacheln öffnet (`Programm Dokumentation/Energieerzeuger`), und von dort ein Verweis auf die Grundlagen je Technik | Abschnitt 10 |
| TD‑E7 | **Modellwahl:** Sonnet oder Haiku, wo die Aufgabe passt (Recherche, Tafeln, Textpflege, Prüfungen); Opus für Technikseiten mit Codebelegen und für die Kopplung | Abschnitt 10 |

## 2. Ausgangslage (Live-Stand 27.09.2026)

Je Technik gab es bis zu vier Seiten, keine davon beantwortete die vier Fragen des Auftrags
zusammenhängend:

| Seite | Inhalt | Repo-Quelle |
|---|---|---|
| `Grundlagen/<Technik>` | Theorie mit eingestreuten Programmaussagen, Stand 21.08.2026 | keine |
| `<Technik>` (Rubrik Programmfunktionen) | ältere Dialogbeschreibung, Stand 21.08.2026, teils nicht mehr zutreffend | keine |
| `Programm Dokumentation/<Technik>` | Dialoghilfe („Eingaben"), bei Wärmepumpe, Heizkessel, BHKW und Solarthermie nur ein Kurzgerüst | nur Photovoltaik, Pufferspeicher, Stromspeicher, Kühlung |
| `Programm Dokumentation/Berechnung/<Technik>` | Rechenweg mit Formeln | eingebettet im Kern |

Die Hilfeknöpfe der App erreichen nur die Rubrik „Programm Dokumentation"; der Windows-Katalog lädt
ausschließlich deren Unterseiten. Das Wiki lässt keine Datei-Uploads zu und kennt keine
Diagrammerweiterung; Inline-Stile samt der `--epos-*`-Farbwerte des Stilblatts übersteht der
Wiki-Filter (geprüft mit `action=parse`, ohne zu speichern).

## 3. Zielbild je Technik

| Technik | Grundlagen (kurz) | Anwendung in EPOS-Plan | Rechenweg (unverändert) | Dialoge (Razor) | Hilfeschlüssel |
|---|---|---|---|---|---|
| Wärmepumpe | `Grundlagen/Wärmepumpe` | `Programm Dokumentation/Wärmepumpe` (Quelle neu aus Live-Stand) | `…/Berechnung/Wärmepumpe` | `Waermepumpe/WaermepumpeAnlageDialog`, `WaermepumpeKonfiguration`, `WaermepumpenDialog`, `WaermepumpeStammDialog`, `KennlinienEditorDialog` | `Form_WP`, `Wizard_WPItem` |
| Wärmequelle Erdreich | `Grundlagen/Wärmequelle Erdreich` (neu) | `Programm Dokumentation/Wärmequelle Erdreich` (Quelle neu) | `…/Berechnung/Wärmequelle Erdreich` | `Simulation/QuelleErdreichDialog`, `KlimazonenkarteDialog` | `Form_QuelleErdreich` |
| Heizkessel | `Grundlagen/Kessel und Spitzenlast` | `Programm Dokumentation/Heizkessel` (Quelle neu) | `…/Berechnung/Heizkessel` | `Erzeuger/HeizkesselDialog` | `Form_Heizkessel` |
| BHKW | `Grundlagen/BHKW` | `Programm Dokumentation/BHKW` (Quelle neu) | `…/Berechnung/BHKW` | `Erzeuger/BhkwDialog` | `Form_BHKWEing` |
| Solarthermie | `Grundlagen/Solarkollektoren` | `Programm Dokumentation/Solarthermie` (Quelle neu) | `…/Berechnung/Solarthermie` | `Solarthermie/SolarkollektorenDialog` | `Form_SolarKollektoren` |
| Pufferspeicher | `Grundlagen/Pufferspeicher` | `Programm Dokumentation/Pufferspeicher` (Quelle vorhanden) | `…/Berechnung/Pufferspeicher` | `Erzeuger/PufferspeicherDialog`, `Simulation/PufferSpProjektDialog` | `Form_PufferSp`, `Form_PufferSp_Projekt` |
| Photovoltaik | `Grundlagen/Photovoltaik` | `Programm Dokumentation/Photovoltaik` (Quelle vorhanden) | `…/Berechnung/Photovoltaik` | `Erzeuger/PhotovoltaikDialog` | `Form_PV` |
| Stromspeicher | `Grundlagen/Stromspeicher` | `Programm Dokumentation/Stromspeicher` (Quelle vorhanden) | `…/Berechnung/Stromspeicher` | `Erzeuger/StromspeicherDialog` | `Form_Stromspeicher` |
| Kühlung | `Grundlagen/Kühlung` (neu) | `Programm Dokumentation/Kühlung` (Quelle vorhanden) | `…/Berechnung/Wärmepumpe` (Kühlbetrieb) | `Bedarf/GebaeudeStammblattFelder`, `Waermepumpe/WaermepumpeKonfiguration`, `Simulation/KomponentenKonfigurationDialog` | neu (Abschnitt 7) |

Die Titel der vorhandenen Grundlagenseiten bleiben (`Kessel und Spitzenlast`, `Solarkollektoren`),
damit bestehende Verweise und Weiterleitungen tragen. Repo-Quellen liegen flach unter
`Projekte/Wiki/`: `Grundlagen - <Titel>.wiki`, `Programm Dokumentation - <Titel>.wiki`,
`Vorlage - <Name>.wiki`; UTF-8 ohne BOM, Kopfkommentar wie bei den Bedienungsseiten.

## 4. Seitenmuster

### 4.1 `Grundlagen/<Technik>` — kurz und knapp

Eine Bildschirmseite: höchstens rund 4 000 Zeichen Fließtext, dazu eine Kennzahlentafel und
höchstens zwei Diagramme. Die Seite erklärt die Technik unabhängig vom Programm; was EPOS-Plan
daraus macht, steht auf der Anwendungsseite.

1. Einleitung, zwei bis drei Sätze: was die Technik ist und wofür sie eingesetzt wird.
2. `== Funktionsprinzip ==` — ein Absatz, bei Bedarf ein Flussbild.
3. `== Kennzahlen ==` — Tafel *Kennzahl · Bedeutung · typischer Bereich*.
4. `== Typische Größen ==` — Richtwerte je Einsatzfall (Einfamilienhaus, Mehrfamilienhaus, Gewerbe) mit runden Werten.
5. `== Einsatz und Grenzen ==` — wo die Technik passt, wo nicht.
6. `== In EPOS-Plan ==` — zwei Sätze und die Verweise auf die Anwendungsseite (Abschnitte Einbindung, Beispiel, Zusammenspiel, Fallstricke) und die Rechenwegseite.
7. `== Siehe auch ==`, `{{Navigation Grundlagen}}`, `[[Kategorie:Grundlagen]]`.

Anker ASCII-klein ohne Umlaute; die Anker der bisherigen Fassung bleiben als weitere Namen im
`{{Anker|…}}` der passenden Überschrift stehen, soweit es einen passenden Abschnitt gibt.
Programmaussagen der bisherigen Fassung, die noch zutreffen, wandern auf die Anwendungsseite;
nicht mehr zutreffende entfallen.

### 4.2 `Programm Dokumentation/<Technik>` — Anwendung in EPOS-Plan

Reihenfolge der Abschnitte; vorhandene Abschnitte und **alle vorhandenen Anker bleiben**
(`help_mapping.txt` zeigt auf sie, `HelpMappingAnkerWacheTests` hält das):

1. Einleitung (Zweck des Dialogs, wie bisher) und ein Satz mit Verweis auf `Grundlagen/<Technik>`.
2. `== Einbindung in EPOS-Plan ==` `{{Anker|einbindung}}` — wo die Technik angelegt, gewählt und
   eingestellt wird, in der Reihenfolge der Bedienung (Katalog → Projektgerät → Dialog →
   Simulationskonfiguration: Rang, Senke, Speicherzuordnung …); ein Flussbild der Einbindung.
3. `== Beispiel ==` `{{Anker|beispiel}}` — eine Beispielanlage aus Abschnitt 5 mit Tafel
   *Dialog · Feld · Wert · Warum*; welches Ergebnis zu erwarten ist und wo man es prüft.
4. `== Zusammenspiel mit anderen Technologien ==` `{{Anker|zusammenspiel}}` — Tafel
   *Technik · Wirkung auf … · Wirkung von … · So rechnet EPOS-Plan*; ein Diagramm (Kaskade,
   Aufteilung, Dauerlinie).
5. `== Fallstricke ==` `{{Anker|fallstricke}}` — je Punkt: was passiert, woran man es erkennt,
   was zu tun ist; ausdrücklich auch, was EPOS-Plan **nicht** abbildet.
6. `== Eingaben ==` (vorhanden) · `== Berechnung ==` (vorhanden) · `== Siehe auch ==`.

Jede Aussage über das Programm ist am Quelltext belegt (Beleg im Bericht des Bearbeiters, nicht
auf der Seite). Beschriftungen von Feldern und Knöpfen stehen so auf der Seite, wie der Dialog
sie zeigt (deutsche Ressource). Die älteren Anker der weitergeleiteten Programmfunktionen-Seite
(Abschnitt 8) werden als weitere Namen an den passenden Abschnitten gesetzt.

## 5. Beispielanlagen

Die Seiten rechnen mit denselben vier Anlagen, damit das Zusammenspiel seitenübergreifend
nachvollziehbar bleibt. Alle Werte sind rund und keinem Produkt zuzuordnen; Geräte heißen
„Wärmepumpe A", „Kessel 1", „BHKW 1", „Speicher 1".

| | A — Mehrfamilienhaus (Bestand, teilsaniert) | B — Hotel | C — Einfamilienhaus (Neubau) | D — Bürogebäude |
|---|---|---|---|---|
| Größe | 12 Wohnungen, 900 m² | 80 Zimmer | 150 m² | 2 000 m² |
| Heizlast (−12 °C) | 45 kW | 250 kW | 6 kW | 90 kW |
| Heizwärme / Jahr | 80 MWh | 420 MWh | 7,5 MWh | 110 MWh |
| Trinkwarmwasser / Jahr | 20 MWh (mit Zirkulation) | 220 MWh | 2,5 MWh | 5 MWh |
| Kühlung | – | – | 3 kW über den Fußboden (Variante) | 80 kW, Kühldecken 16/19 °C |
| Heizsystem | Heizkörper 55/45 °C | Heizkörper 60/45 °C, Lüftung | Fußbodenheizung 35/28 °C | Flächenheizung 40/30 °C |
| Strom / Jahr | 30 MWh | 450 MWh | 4 MWh | 120 MWh |
| Erzeuger | Luft-Wasser-Wärmepumpe A (rund 25 kW bei A−7/W55), Kessel 1 50 kW Brennwert als Spitzenlast | BHKW 1 30 kW elektrisch / 60 kW thermisch, Kessel 1 und 2 je 150 kW, Solarthermie 50 m² Flachkollektoren (Warmwasser-Vorwärmung) | Sole-Wasser-Wärmepumpe A 6 kW, Erdwärmesonde 1 × 100 m | reversible Luft-Wasser-Wärmepumpe A 100 kW, Kessel 1 60 kW |
| Speicher | Pufferspeicher 1 000 l, Stromspeicher 20 kWh | Pufferspeicher 3 000 l | Pufferspeicher 200 l, Stromspeicher 10 kWh | Pufferspeicher 1 500 l |
| Photovoltaik | 30 kWp | 100 kWp | 10 kWp | 60 kWp |

Ein Bearbeiter passt eine Größe an, wenn EPOS-Plan die Anlage sonst nicht sinnvoll abbilden kann,
und nennt die Abweichung in seinem Bericht; die Tafel wird dann hier nachgezogen.

## 6. Diagramme

Das Wiki nimmt keine Bilddateien an. Diagramme entstehen deshalb aus Vorlagen mit Inline-Stilen
(Repo-Quellen unter `Projekte/Wiki/Vorlage - *.wiki`): Sie folgen der hellen und dunklen
Darstellung, bleiben durchsuchbar und werden vom Übersetzungs-Proxy mitübersetzt.

| Vorlage | Zweck |
|---|---|
| `Diagramm-Anfang` / `Diagramm-Ende` | Rahmen mit Titel und Bildunterschrift |
| `Säulen-Anfang` / `Säule` / `Säulen-Ende` | Säulendiagramm, gestapelt nach Rollen; Höhen in Prozent der Säulenfläche |
| `Legende` | Farbzeichen je Rolle |
| `Fluss-Anfang` / `Flusskasten` / `Flusspfeil` / `Flussspalte-Anfang` / `Flussspalte-Ende` / `Fluss-Ende` | Flussbild aus Kästen und Pfeilen |
| `Diagrammfarbe` | eine Farbe je Rolle: `waerme`, `kessel`, `bhkw`, `solar`, `strom`, `speicher`, `umwelt`, `kaelte`, `netz` |

Regeln: Ein Diagramm steht nur, wo es mehr zeigt als ein Satz (Kennlinie, Dauerlinie mit Anteilen,
Aufteilung, Kaskade, Einbindung). Werte aus einem vereinfachten Modell heißen in der
Bildunterschrift „Prinzipbild" oder „Beispielwerte, gerundet"; kein Diagramm gibt vor, ein
Ergebnis von EPOS-Plan zu sein, das nicht so gerechnet wurde. Das Modell hinter den Säulenwerten
steht als HTML-Kommentar über dem Diagramm (Formel und Annahmen, ohne Kürzel und Datum).
Geprüft wird mit [`Werkzeuge/WikiUpload/vorschau.py`](../../Werkzeuge/WikiUpload/vorschau.py)
(Vorlagen lokal auflösen, Wiki-Filter per `action=parse`, Aufnahmen hell und dunkel).

## 7. Kopplung an die App (Sprungziele)

1. **Zielschreibweise:** Ein Ziel in `help_mapping.txt`, das mit `/wiki/` beginnt, ist ein
   Seitenpfad außerhalb der Rubrik, etwa `/wiki/Grundlagen/Wärmepumpe` oder
   `/wiki/Grundlagen/Kessel_und_Spitzenlast#kennzahlen`. Kurznamen ohne Schrägstrich am Anfang
   bleiben Seiten der Rubrik „Programm Dokumentation".
2. **Windows:** Der Katalog löst solche Pfade auf, auch ohne Netz: Die Grundlagenseiten stehen im
   eingebetteten Startbestand `help_cache.json`, der Abruf lädt die Rubrik Grundlagen dazu und
   verwirft sie nicht; der Kurztext lautet „Grundlagen: <Titel>".
3. **iOS:** Der Hilfedienst öffnet Pfadziele schon heute; der Kurztext wird lesbar statt des Pfads.
4. **Knöpfe:** Jeder Technikdialog bekommt neben dem Knopf „Berechnung" einen Knopf „Grundlagen"
   mit dem Schlüssel `<Formname>.Grundlagen`; der Fensterknopf zeigt weiter auf die
   Anwendungsseite, die nun mit Einbindung, Beispiel, Zusammenspiel und Fallstricke beginnt. Die
   Kühlung bekommt ihre Knöpfe (Kühlabschnitt des Gebäudes, Kühlbetrieb der Wärmepumpe).
5. **Wächter:** Jede `.Grundlagen`-Zeile hat einen Knopf und umgekehrt; jeder Anker eines
   Grundlagen-Ziels steht in der Repo-Quelle.

## 8. Weiterleitungen und Upload

Beim nächsten gebündelten Upload (Regel 13.3, durch den Anwender mit `wiki_upload.py`):

1. zuerst die Vorlagen (`Vorlage:Diagrammfarbe` vor den übrigen), dann die Grundlagenseiten, dann
   die Anwendungsseiten, dann `Grundlagen` und `Vorlage:Navigation Grundlagen` mit den zwei neuen
   Seiten;
2. danach die Weiterleitungen (`Werkzeuge/WikiUpload/weiterleitungen.tsv`):

| Seite | wird Weiterleitung auf |
|---|---|
| Wärmepumpe | Programm Dokumentation/Wärmepumpe |
| Heizkessel, Kessel | Programm Dokumentation/Heizkessel |
| Blockheizkraftwerk, KWK, Kraft-Wärme-Kopplung | Programm Dokumentation/BHKW |
| Solarthermie, Solaranlage, Sonnenkollektor | Programm Dokumentation/Solarthermie |
| Pufferspeicher, Puffer | Programm Dokumentation/Pufferspeicher |
| Photovoltaik, PV, PV-Anlage, Solarstrom | Programm Dokumentation/Photovoltaik |
| Stromspeicher, Akku, Batterie, Batteriespeicher | Programm Dokumentation/Stromspeicher |

Die Synonyme zeigen heute auf die alten Seiten; MediaWiki folgt keiner doppelten Weiterleitung,
deshalb werden sie im selben Zug direkt umgehängt.

## 9. Prüfung

- `WikiProduktdatenWacheTests` hält alle neuen Quellen (liegen unter `Projekte/Wiki/`).
- `HelpMappingAnkerWacheTests` kennt die Grundlagen-Quellen.
- Gegenlese-Muster der Wurzel-`CLAUDE.md` auf jeder neuen und geänderten Quelle ohne Treffer.
- Vorschau jeder Seite mit Diagramm hell und dunkel.

## 10. Stand und Übergabe (27.09.2026, abends)

Die Sitzung ist auf Wunsch des Anwenders angehalten (Fortsetzung Dienstag). Alle Agenten sind
gestoppt; keiner hatte eine Datei geschrieben — die Technikseiten und die Kopplung sind noch
**nicht begonnen**, fertig und gepusht ist die Grundlage.

| Schritt | Stand | Modell beim Fortsetzen |
|---|---|---|
| Diagrammvorlagen (`Projekte/Wiki/Vorlage - *.wiki`), Vorschau `Werkzeuge/WikiUpload/vorschau.py` | fertig, hell/dunkel geprüft | – |
| Upload-Vorbereitung: `seiten.tsv` (Vorlagen vorn, neue Seiten), `weiterleitungen.tsv` (7 Seiten + 12 Synonyme), `--weiterleitungen` | fertig | – |
| Repo-Quellen `Grundlagen` und `Vorlage:Navigation Grundlagen` (mit Wärmequelle Erdreich und Kühlung) | fertig; Kurzbeschreibungen der Listeneinträge nach den neuen Grundlagenseiten nachziehen | Sonnet |
| Technikseiten: je Technik Grundlagen (kurz) und Anwendungsseite (Einbindung, Beispiel, Zusammenspiel, Fallstricke) — 9 Techniken, 18 Quellen | offen; Arbeitspakete wie Abschnitt 3 (WP + Erdreich, Kessel + BHKW, Solarthermie + Puffer, PV + Stromspeicher, Kühlung) | Opus (Codebelege), je Paket ein Agent |
| Kopplung Abschnitt 7 (Zielschreibweise `/wiki/…`, Katalog Windows/iOS, Knöpfe `.Grundlagen`, Kühlungsknöpfe, Wächter) | offen | Opus |
| Kachelweg TD‑E6: Repo-Quelle `Programm Dokumentation/Energieerzeuger` mit Tafel *Technik · Grundlagen · In EPOS-Plan · Rechenweg* | offen | Sonnet |
| Rubrikseite `Programm Dokumentation` als Repo-Quelle, Vertragstafel mit `help_mapping.txt` abgleichen (Live: 23 von 100 Zeilen abweichend — 16 veraltete Ziele, 7 Schlüssel ohne Zeile und ohne Code) | offen, nach der Kopplung | Sonnet |
| SVG TD‑E5: Einstellungen für `LocalSettings.php` recherchieren (`$wgEnableUploads`, `$wgFileExtensions`, `$wgSVGNativeRendering`, Bot-Rechte `uploadfile`/`uploadeditmovefile`), `wiki_upload.py --dateien` mit `dateien.tsv` (SHA-1-Abgleich, Dateien vor Seiten), SVG-Grafiken (Einbindungsschemata, Kennlinien) mit hellem Kartenhintergrund | offen | Sonnet |
| Hilfesystem-Konzept um die Technikdokumentation ergänzen (Rubrik Grundlagen, direkter und indirekter Weg, Diagrammvorlagen, SVG) | offen | Opus |
| Gate (Wächter, Gegenlese-Muster, Vorschau), Statuszeile, Protokoll, Logbuch-Vorschlag (Version beim Anwender erfragen), Push | offen | – |
| Upload ins Wiki | offen — durch den Anwender, gebündelt nach Regel 13.3 | – |

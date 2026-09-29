# Technikdokumentation im Wiki — Grundlagen, Anwendung in EPOS-Plan, Kopplung an die App

Stand 27.09.2026 · Zweig `claude/wiki-help-assistant-docs-jllq1r` · Statuszeile #611 ·
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
| TD‑E8 | **Alle Techniken beschreiben, auch Stromspeicher und Wechselrichter** (27.09.2026): der Wechselrichter bekommt eine eigene Grundlagen- und Anwendungsseite; Heizstab und Lastspitzenkappung bleiben Abschnitte der Seiten Wärmepumpe, Kessel und Stromspeicher | Abschnitt 3 |
| TD‑E9 | **Bot-Upload durch die Orchestrierung erlaubt** (Anlegen und Bearbeiten, Bot-Passwort „Epos@epos"; Zugang nur als Umgebungsvariablen `WIKI_BOT_USER`/`WIKI_BOT_PASS`, nie im Repository): gebündelt erst nach Prüfung (Wächter, Gegenlese-Muster, Vorschau), Reihenfolge Vorlagen → Grundlagen → Anwendungsseiten → Rubrik/Navigation → Weiterleitungen; Bot-Passwort nach Abschluss neu erzeugen | Abschnitt 8 |
| TD‑E10 | **Modellwahl nach Anspruch** (27.09.2026: „Achte darauf, dass die Agenten intelligent genug sind für die Aufgaben. Nutze, wenn sinnvoll, die günstigen Modelle“; Fable nur für die Orchestrierung): Grundlagenseiten (Fachwissen, kurz), Energieerzeuger-Seite, Datei-Upload und Vertragstafel auf Sonnet; Anwendungsseiten, deren Zusammenspiel und Fallstricke das Verhalten der Simulation am Code belegt erklären, und die Kopplung auf Opus; Zählungen und Kodierungsprüfungen per Skript oder Haiku; Abrechnung über die Cloud-Credits | Abschnitt 10 |
| TD‑E11 | **Fable für komplexe Prüf- und Syntheseschritte** (27.09.2026: „Prüfe, wo Fable sinnvoll ist, insbesondere für komplexe Vorhaben“): (1) Querprüfung aller Anwendungsseiten gegen den Code und untereinander — Kaskade, Pufferladung, Rangfolge des PV-Stroms, Kühlung und Anlagenkopplung, Beispielanlagen A–D seitenübergreifend stimmig; Fehler behebt der Prüfer selbst; (2) Review der Kopplung vor der Übernahme (iOS-Hülle ohne Bau, Windows-Katalog, Wächter). Geschrieben wird weiter mit Opus bzw. Sonnet. Nicht auf Fable: Seitentexte, SVG-Grafiken, Vertragstafel, Papiere, Upload | Abschnitt 10 |

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
| Wechselrichter | `Grundlagen/Wechselrichter` (neu) | `Programm Dokumentation/Wechselrichter` (neu) | `…/Berechnung/Photovoltaik#wechselrichter` | `Erzeuger/PhotovoltaikDialog`, `PvStraengeFelder`, Wechselrichterkatalog | `Form_PV`, `Form_AdminWechselrichter` |
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
| Kühlung | – | – | 3 kW über den Fußboden (Variante) | 80 kW, Kühldecken 16/19 °C, Kältebedarf 30 MWh/a, Kühlsollwert 26 °C, Kühlkennlinie mit Kühl-Vorläufen 7 und 18 °C |
| Heizsystem | Heizkörper 55/45 °C | Heizkörper 60/45 °C, Lüftung | Fußbodenheizung 35/28 °C | Flächenheizung 40/30 °C |
| Strom / Jahr | 30 MWh | 450 MWh | 4 MWh | 120 MWh |
| Erzeuger | Luft-Wasser-Wärmepumpe A (rund 25 kW bei A−7/W55), Kessel 1 50 kW Brennwert als Spitzenlast | BHKW 1 30 kW elektrisch / 60 kW thermisch, Kessel 1 und 2 je 150 kW, Solarthermie 50 m² Flachkollektoren (Warmwasser-Vorwärmung) | Sole-Wasser-Wärmepumpe A 6 kW, Erdwärmesonde 1 × 100 m | reversible Luft-Wasser-Wärmepumpe A 100 kW, Kessel 1 60 kW |
| Speicher | Pufferspeicher 1 000 l, Stromspeicher 20 kWh | Pufferspeicher 3 000 l | Pufferspeicher 200 l, Stromspeicher 10 kWh | Pufferspeicher 1 500 l |
| Photovoltaik | 30 kWp | 100 kWp | 10 kWp | 60 kWp |

**Festgelegte Einstellungen (Querprüfung TD‑E11)** — alle Seiten rechnen damit:

- **A:**
  - Wärmepumpe A: Vorlauf 55/45, Heizstab mitrechnen aus, bivalenter Betrieb aus. Betriebsmodus laufzeitoptimiert, Variante PV-optimiert.
  - Wärmesenke: Rang 1 Heizkreis (Beides), Rang 2 Pufferspeicher.
  - Speicher 1 (Puffer 1 000 l): Verwendung Heizung, 55/45 °C, Schwellen 10/95, nachrangig leer, Mindestfüllstand 10, Bereitschaftsverlust 2 kWh/24 h, Ladeprio 20.
  - Kessel 1: Brennwert Erdgas, η 0,98, Bereitschaftsverlust 0,1 kW, nur Direktsenke.
  - Kaskade: 1 Wärmepumpe, 2 Heizkessel. Der Kessel rechnet als eigene Stufe nach der Speicherstufe.
  - Anlagenkopplung aus.
  - PV 30 kWp: 75 × 400 W, Süd 30°, Modell Erweitert, Systemverluste 3 %. Variante mit Wechselrichter: 18 kWp Süd und 12 kWp West an einem Wechselrichter von 25 kW.
  - Stromspeicher 1: 20 kWh.
- **B:**
  - BHKW 1: η 0,30/0,60, Erdgas, 80/50, wärmegeführt, untere Leistungsgrenze 30 %. Senken: Rang 1 Heizkreis, Rang 2 Speicher 1 mit Ladeprio 30, nachrangig bis 30 %.
  - Kessel 1 und 2: nur Direktsenke.
  - Solarthermie: 20 × 2,5 m², 35°/Süd. Senken: Rang 1 Heizkreis (nur Warmwasser), Rang 2 Speicher 1 mit Ladeprio 10.
  - Speicher 1 (3 000 l): Verwendung Brauchwasser, 60/35 °C.
  - Kaskade: 1 Solarthermie, 2 BHKW, 3 Heizkessel.
  - Variante: Stromspeicher 1 mit 100 kWh/50 kW und Lastspitzenkappung.
- **C:**
  - Sole-Wasser-Wärmepumpe: Vorlauf 35/28, Quelle Erdreich (VDI 4640). Sonde 1 × 100 m in Ton/Schluff, wassergesättigt, Spreizung 3 K. Betriebsmodus leistungsoptimiert.
  - Speicher 1: 200 l.
  - PV 10 kWp: 25 × 400 W, Weg vereinfacht, Modell Einfach, WR-Wirkungsgrad 0,96, Systemverluste 2 %.
  - Stromspeicher 1: 10 kWh/5 kW, 10–90 %, lädt nur aus PV-Überschuss.
- **D:**
  - Reversible Wärmepumpe A: Kühl-Vorlauf 18 °C, Hilfsstrom 5 %.
  - Gebäude: Kühlsollwert 26 °C, Kühlübergabe mit AK1.
  - Projektschalter „Kühlung rechnen“ an.
  - Kältebedarf 30 MWh/a.

Ein Bearbeiter passt eine Größe an, wenn EPOS-Plan die Anlage sonst nicht sinnvoll abbilden kann,
und nennt die Abweichung in seinem Bericht; die Tafel wird dann hier nachgezogen.

## 6. Diagramme

Diagramme entstehen aus Vorlagen mit Inline-Stilen (Repo-Quellen unter
`Projekte/Wiki/Vorlage - *.wiki`): Sie folgen der hellen und dunklen Darstellung, bleiben
durchsuchbar und werden vom Übersetzungs-Proxy mitübersetzt. Was die Vorlagen nicht können —
hydraulische und elektrische Einbindungsschemata, echte Kurven wie die Jahresdauerlinie — kommt
als SVG-Grafik (TD‑E5): Quelle unter `Projekte/Wiki/Dateien/`, Zeile in
`Werkzeuge/WikiUpload/dateien.tsv`, heller Kartenhintergrund, Einbindung mit
`[[Datei:<Name>|mini|zentriert|800px|<Bildunterschrift>]]`. Hydraulische Prinzipbilder stehen auf
den Grundlagenseiten, damit sie nicht als Rechenmodell von EPOS-Plan gelesen werden.

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

## 10. Stand und Übergabe (28.09.2026)

**Stand 28.09.2026:**
- Umsetzung, Prüfung und Upload sind abgeschlossen. Die Befunde außerhalb der Wiki-Quellen stehen als Folgeaufträge in [`Folgeauftraege_Technikdokumentation_EPOS-Plan.md`](Folgeauftraege_Technikdokumentation_EPOS-Plan.md).
- Der Zweig `claude/wiki-help-assistant-docs-jllq1r` enthält `origin/ios_migration_september` bis `363096aa` (Merge `ed2dc020`) und die Kopplung (Merge `5afdd6f2`). Sein Stand ist am 29.09.2026 nach `ios_migration_september` zusammengeführt (Merge `851b79cb`); die Statuszeile trägt dabei die Nummer #611 statt #589.
- Statuszeile #611, Protokoll [`H14_Technikdokumentation_Protokoll.md`](../ueberholt/Protokolle/Hilfe/H14_Technikdokumentation_Protokoll.md).
- Einrichtung in einem neuen Container: `dotnet-install.sh --jsonfile global.json`, `apt-get install -y git-lfs`, `git lfs install`, `git lfs pull --include=Referenzlaeufe/Kenndaten_Test.sqlite --exclude=""`.
- Die Routine „Fortsetzung Technikdoku Wiki“ am **Freitag, 2. Oktober 2026, 02:00 Uhr** bleibt. Sie mergt `origin/ios_migration_september`, nimmt die bis dahin ergänzten Funktionen in die Beschreibungen auf und zieht die Seiten nach, wenn eine der Aufgaben aus dem Protokoll, Abschnitt 6, umgesetzt ist.

| Schritt | Stand |
|---|---|
| Diagrammvorlagen, Vorschau `vorschau.py` | fertig |
| Grundlagenseiten (10), Rubrik Grundlagen, Navigation | fertig |
| Anwendungsseiten (7) mit Einbindung, Beispiel, Zusammenspiel, Fallstricke | fertig |
| Kachelweg: Energieerzeuger mit Techniktafel | fertig |
| Rubrikseite Programm Dokumentation mit Vertragstafel (136 Schlüssel = `btn_Help` der Mapping-Datei) | fertig |
| SVG: `LocalSettings.php`, `wiki_upload.py --dateien`, sechs Grafiken | fertig |
| Querprüfung (Fable) und Rechenwegseiten | fertig |
| Kopplung (Opus), Review (Fable), Merge | fertig |
| Hilfesystem-Konzept Abschnitt 14 | fertig |
| Gate, Statuszeile, Protokoll | fertig; CI Kern grün |
| Upload `wiki_upload.py --dateien`, `--seiten --nur`, `--weiterleitungen` | erledigt am 28.09.2026: 6 Dateien, 39 Seiten, 19 Weiterleitungen, Rücklese byte-gleich |
| Korrektur: harte Umbrüche, Formeln als LaTeX (Konzept Hilfesystem 13.5), Rechenwegseiten in der Upload-Liste | erledigt am 28.09.2026, zweiter Upload 35 Seiten; Protokoll H14 Abschnitt 9 |
| Logbuch | Satz zu den Knöpfen „Grundlagen“ mit der Programmversion, die sie enthält; Version beim Anwender erfragen |

## Anhang A — Arbeitsanweisung für die Bearbeiter der Technikseiten

### Ziel des Anwenders
Je Technik (1) eine kurze Grundlagenbeschreibung, (2) die Funktionsbeschreibung in EPOS-Plan mit konkretem Bezug:
wie die Technik eingebunden und konfiguriert wird, ein konkretes Beispiel mit realen, sinnvollen Größen und den
konkreten Einstellungen in EPOS-Plan, (3) Einfluss von und auf andere Technologien und wie diese Einflüsse in
EPOS-Plan wirken, (4) Fallstricke und was besonders zu beachten ist, (5) veranschaulichende Diagramme, wo sinnvoll.

### Lesen (sparsam!)
- Zuerst: `Dokumentation/aktuell/Konzept_Technikdokumentation_Wiki_EPOS-Plan.md` (Seitenmuster §4, Beispielanlagen §5,
  Diagramme §6) und in `CLAUDE.md` nur den Abschnitt „Dokumentation" (Wiki-Regeln).
- Kontext klein halten: große Dateien NIE ganz lesen. Erst Überschriften (`grep -n '^=' datei.wiki`, `grep -n '^#' datei.md`),
  dann gezielt Abschnitte mit Zeilenbereich; im Code mit `grep -n` suchen und nur die Fundstellen lesen.
- Live-Stand der Wiki-Seiten nur lesend holen:
  `curl -sS "https://wiki.epos-plan.de/index.php?title=<Titel URL-kodiert>&action=raw"`.
- Die Rechenwegseiten `EPOS.Kern/Allgemein/Hilfe/Berechnung/*.wiki` sind geprüfte Referenz (Eingangsgrößen mit Stelle im
  Programm, Rechenweg, Grenzen) — nutze sie als Startpunkt, statt den Rechenkern ganz zu lesen.

### Arbeitsweise
- Nicht selbst `origin` mergen — die Orchestrierung hat den Zweig vorher auf den neuesten Stand gebracht.
- Ein Agenten-Worktree kann auf einem älteren Stand starten. Erster Schritt ist deshalb
  `git merge --ff-only claude/wiki-help-assistant-docs-jllq1r`. Lehnt das Berechtigungssystem ihn ab, wird er nicht
  wiederholt; die aktuellen Dateien kommen dann nur lesend über `git show claude/wiki-help-assistant-docs-jllq1r:<pfad>`.
  Geschrieben wird nur im eigenen Worktree.
- Reihenfolge: Schreibt ein Bearbeiter beide Seiten einer Technik, ERST die Grundlagenseite schreiben und sofort
  committen, DANN die Anwendungsseite, sofort committen. Getrennte Bearbeiter verweisen über die festen Titel und die
  Anker aus „Inhaltsregeln“ aufeinander. Jede fertige Datei ist ein eigener Commit — wird die Sitzung unterbrochen,
  bleibt das Fertige erhalten.
- Nur deine genannten Dateien ändern; keine Änderung an help_mapping.txt, Razor, C#, Konzept-, Status-, Indexpapieren,
  seiten.tsv, Vorlagen. Kein Push, kein CI-Lauf, kein Upload, keine Wiki-Bearbeitung. Kein dotnet build/test.
- Dateiform: UTF-8 ohne BOM, LF. Kopfkommentar dreizeilig wie in `Projekte/Wiki/Programm Dokumentation - Pufferspeicher.wiki`;
  Grundlagenseiten: `<!-- EPOS-Plan Grundlagenseite | Wikititel: Grundlagen/<Titel>` / `     Repo-Quelle dieser Seite: Projekte/Wiki/Grundlagen - <Titel>.wiki` /
  `     Pflegeregel: zuerst hier aendern, dann hochladen - nie umgekehrt. -->`; am Ende `{{Navigation Grundlagen}}` und `[[Kategorie:Grundlagen]]`.

### Inhaltsregeln
- Nur der gültige Stand. Keine Hersteller-, Produkt- oder Typnamen, keine Datenblattwerte, keine Katalognamen aus
  Testdatenbank oder Katalogen — Beispiele mit neutralen Namen und runden Werten („Wärmepumpe A, 25 kW", „Speicher 1, 20 kWh").
  Normen, Gesetze und Formate dürfen genannt werden.
- Gegenlese-Muster muss auf jeder deiner Dateien 0 Treffer ergeben (auch in HTML-Kommentaren; keine Auftrags-, Wellen-, Commit-Kürzel):
  `grep -nE 'seit (dem|der|W)|geändert|Entscheid|Befund|W[0-9]+[a-z]?[‑-][A-Z][‑-][0-9]+|Stand:? *[0-9]|bisher|früher|vorher|Bis dahin|Migrationsschritt' <datei>`
- Wahrheit: Jede Aussage darüber, was EPOS-Plan tut (Felder, Beschriftungen, Voreinstellungen, Rechenweg, Reihenfolgen,
  Meldungen, Grenzen), ist am aktuellen Quelltext belegt (EPOS.UI/Dialoge/…, EPOS.UI.Daten/…, EPOS.Kern/…; Beschriftungen in
  `EPOS.Kern/MyResource/Resource.resx`). Was du nicht belegen kannst, schreibst du nicht. Widersprüche Rechenwegseite ↔ Code melden, nicht ändern.
- Anker: alle vorhandenen `{{Anker|…}}`-Namen bleiben. Neue Abschnitte: einbindung, beispiel, zusammenspiel, fallstricke
  (Anwendungsseite); funktionsprinzip, kennzahlen, typische-groessen, einsatz-und-grenzen, in-epos-plan (Grundlagenseite).
  ASCII-klein ohne Umlaute, höchstens drei Namen je `{{Anker}}`, kein Name doppelt auf einer Seite.
- Quelltext nach Konzept Hilfesystem 13.5: ein Absatz, ein Listenpunkt, eine Tabellenzelle je Zeile, kein harter Umbruch;
  Formeln und Formelzeichen mit Index als LaTeX in `<math>`, Einheiten dahinter als Text.
- Anwendungsseite beginnt (nach der Einleitung) mit einem Satz und Verweis auf die Grundlagenseite.

### Diagramme
- Nur mit den Vorlagen `Projekte/Wiki/Vorlage - *.wiki` (Doku im noinclude-Teil lesen; Rollen: waerme kessel bhkw solar strom
  speicher umwelt kaelte netz). Säulenhöhen in Prozent des größten Werts (größter = 100). Über jedem Diagramm mit gerechneten
  Werten ein HTML-Kommentar mit Formel und Annahmen. Bildunterschrift „Prinzipbild" oder „Beispielwerte, gerundet" —
  kein Diagramm darf ein EPOS-Plan-Ergebnis vortäuschen.
- Prüfen: `python3 Werkzeuge/WikiUpload/vorschau.py <datei> <ordner außerhalb des Repositoriums> --bild`
  und das PNG (hell) mit dem Read-Werkzeug ansehen; keine Vorschaudateien ins Repo.

### Umfang
- Grundlagenseite: höchstens rund 4 000 Zeichen Fließtext plus Kennzahlentafel und höchstens zwei Diagramme.
- Anwendungsseite: die vier neuen Abschnitte zusammen etwa 6 000–10 000 Zeichen, mindestens ein Flussbild (Einbindung)
  und ein Diagramm im Beispiel oder Zusammenspiel; Beispieltafel *Dialog · Feld · Wert · Warum* mit den Beschriftungen des Dialogs.
- Zutreffende programmbezogene Inhalte der bisherigen Grundlagenseite wandern auf die Anwendungsseite; nicht mehr
  zutreffende Aussagen entfallen und stehen im Bericht.

### Abschluss
Commits nur mit deinen Dateien (`git add <pfad>`), Betreff höchstens 72 Zeichen deutsch, Trailer-Zeile mit dem
arbeitenden Modell, etwa `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`. Bericht knapp, ohne Dateiabzüge: Commit-SHAs und Zweig; Dateien mit
Zeichenzahl; je Seite die wichtigsten Programmaussagen mit Beleg Datei:Zeile; Abweichungen von den Beispielanlagen; nicht mehr
zutreffende Aussagen der Live-Seiten; Vorschläge für Sprungziele (Schlüssel → Seite#anker); offene Fragen; Gegenlese-Ergebnis.

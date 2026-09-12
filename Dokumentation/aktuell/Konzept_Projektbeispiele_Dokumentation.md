# Konzept: Projektbeispiele als mitwachsende Online-Dokumentation

**Fassung 1** · Stand 16.08.2026 · Status: Entwurf zur Abstimmung
Bezug: `Konzept_Berichtserstellung_EPOS-Plan.md` (Datensammler, `KennzahlenKatalog`,
ScottPlot-Bildpfad), `Konzept_Wirtschaftlichkeit.md` (Kennzahlen der Kapitalwertmethode),
`Konzept_Setup_InnoSetup_EPOS-Plan.md` (Auslieferungsdatenbank, Versionsquelle),
`Umsetzung_mit_ClaudeCode.md` (Arbeitsweise im Repository)
Zielort: `https://epos-plan.de/epos-plan/epos-plan-schulung/beispiele/`
Website verifiziert am 16.08.2026 (WordPress, Avada, WooCommerce, WP Download Manager,
Redirection-Plugin, Login-/Mitgliederbereich vorhanden)

---

## 1. Ausgangslage

### 1.1 Was heute steht

Die Seite **Schulung → Onlineschulung → Beispiele** ist inhaltlich bereits gut: neun
Anwendungsfälle vom Einfamilienhaus bis zum Quartier, jeweils nach demselben Muster —
*Ausgangssituation · Vorgehen · Auslegung · Worauf zu achten ist* —, dazu ein Abschnitt
„Empfohlenes Vorgehen" und eine Liste von elf Beispielrechnungen zum Download, deren
Lösungen erst nach Anmeldung sichtbar sind.

Drei Dinge fehlen:

1. **Kein Bild.** Die Texte beschreiben Masken, Kennzahlen und Diagramme, zeigen aber keine.
   Für eine Schulungsseite ist das die größte Lücke.
2. **Keine Zahlen aus der Rechnung.** Die Werte im Text sind Größenordnungen, nicht
   Ergebnisse eines nachvollziehbaren Laufs. Sie sind damit weder prüfbar noch aktualisierbar.
3. **Keine Verbindung zum Programmstand.** Ändert sich eine Maske, ein Rechenweg oder
   kommt ein Modul hinzu, altert der Text still. Niemand merkt es, bis ein Anwender fragt.

Punkt 3 ist der eigentliche Anlass dieses Konzepts. Beispiele sind die Dokumentationsart
mit der kürzesten Halbwertszeit — sie beschreiben nicht Prinzipien, sondern Klicks und Zahlen.

### 1.2 Was im Programm schon vorhanden ist

Der Aufwand fällt deutlich geringer aus als es zunächst wirkt, weil die Berichtserstellung
den halben Weg bereits gebaut hat:

| Baustein | Stand | Nutzung hier |
|---|---|---|
| `SimulationRunner` (headless) | vorhanden, in `Form_Variantentest` genutzt | erzeugt die Zahlen des Beispiels ohne Oberfläche |
| `BerichtsDatenSammler`, `KennzahlenKatalog` (Phase 1) | umgesetzt | liefert Kennzahlen samt Einheit und Formatierung |
| ScottPlot, off-screen deterministisch | im Projekt, Druckpfad des Berichts | erzeugt **alle** Ergebnisgrafiken ohne Bildschirmfoto |
| Berichtsgenerator Word/Excel (Phasen 1–6) | umgesetzt | liefert je Beispiel einen Musterbericht als Download |
| Variantenmodell `Tab_Variante` | vorhanden | ein Beispiel darf mehrere Varianten vergleichen |
| Zweisprachigkeit `de-DE`/`en-US` | vorhanden | Vorbereitung für englische Beispiele |
| `help_mapping.txt` | vorhanden, aber nicht im Projekt eingetragen (Setup-Konzept S2) | verbindet Programmbereich → Beispielseite |

Neu zu bauen sind im Kern nur drei Dinge: der **Bildaufnehmer** für Programmmasken, der
**Bauvorgang**, der aus Text, Zahlen und Bildern eine Seite macht, und die
**Veröffentlichung** nach WordPress.

---

## 2. Zielbild

### 2.1 Der Grundsatz

> **Geschrieben wird nur der Erzähltext. Zahlen, Diagramme und Bildschirmfotos werden
> bei jedem Bauvorgang neu erzeugt.**

Alles, was von Hand in ein Dokument geschrieben wird, altert. Alles, was aus den
Projektdaten und dem laufenden Programm erzeugt wird, altert nicht — es ändert sich
mit dem Programm. Das ist der ganze Mechanismus hinter der Forderung „zukünftige
Änderungen sollen in das Projektbeispiel eingehen".

Daraus folgt die Trennung:

| Bestandteil | Herkunft | Altert? |
|---|---|---|
| Projektdaten des Beispiels | Datei im Repository, versioniert | nein (bewusst eingefroren) |
| Kennzahlen im Text | erzeugt aus dem Simulationslauf | nein |
| Ergebnisdiagramme | erzeugt über ScottPlot | nein |
| Bildschirmfotos der Masken | erzeugt über den Aufnehmer nach Drehbuch | nein |
| Erzähltext, Drehbuch | von Hand (mit Claude) geschrieben | **ja — deshalb Kapitel 9** |

### 2.2 Ein Beispiel ist ein Ordner

```
Beispiele/
  _vorlage/                              Muster für neue Beispiele
  10-einfamilienhaus-waermepumpe/
    beispiel.yaml                        Stammdaten, Sichtbarkeit, Bezugsliste
    projekt.epx                          Projektdaten — die Quelle aller Zahlen
    text.md                              Erzähltext mit Platzhaltern
    schritte.yaml                        Drehbuch für die Bildaufnahme
    ergebnis.json                        ERZEUGT: Kennzahlen des Referenzlaufs
    bilder/de/*.png                      ERZEUGT: Masken und Diagramme
    ausgabe/                             ERZEUGT: HTML, Musterbericht, Downloads
    veroeffentlichung.json               ERZEUGT: Seiten-ID, Medien-IDs, Prüfsummen
  20-mehrfamilienhaus/
  30-verwaltungsgebaeude/
  …
  build-beispiele.ps1
```

Der Ordner liegt **im Repository neben dem Code** (`C:\Waermeplan\WP_Plan\Beispiele`),
nicht in einem eigenen Projekt. Nur so verändert derselbe Commit, der eine Maske umbaut,
auch das Beispiel — und nur so zeigt `git log` beides nebeneinander.

---

## 3. Entscheidungen

| Nr. | Entscheidung | Begründung |
|---|---|---|
| **E1** | **Ein Beispiel = ein Ordner im Programm-Repository**, versioniert mit dem Code | Beispiel und Programmstand bleiben zwangsläufig zusammen; kein zweiter Ablageort, kein zweiter Abgleich |
| **E2** | **Zahlen im Text nur über Platzhalter** (`{{kz:JAZ}}`), nie eingetippt | Der einzige Weg, wie sich ein Rechenweg-Fix von selbst bis auf die Website durchschlägt |
| **E3** | **Ergebnisdiagramme über ScottPlot, nicht als Bildschirmfoto** | Derselbe Bildpfad wie im Bericht: deterministisch, ohne Fensterrahmen, ohne Bildschirm, in beiden Sprachen |
| **E4** | **Maskenbilder über einen Aufnehmer *in* der Anwendung**, gesteuert per Drehbuch — keine externe UI-Automatisierung | Die Dialoge sind großenteils programmatisch ohne Designer und ohne `AutomationId` aufgebaut; UIA-Werkzeuge (FlaUI o. ä.) fänden zu wenig Halt. Die Anwendung kennt ihre eigenen Formulare |
| **E5** | **Feste Fenstergröße, feste Skalierung, eigenes Beispiel-Benutzerprofil** | Ohne das erzeugt jeder Lauf „geänderte" Bilder, und die Mediathek füllt sich mit Dubletten |
| **E6** | **Eigene Beispieldatenbank**, aus der bereinigten Auslieferungsdatenbank plus Beispielprojekte | Kein Kundendatum auf einem Bildschirmfoto. Nutzt dieselbe Bereinigung wie das Setup (dort Abschnitt 6.1) |
| **E7** | **Übersichtsseite bleibt von Hand, je Beispiel eine Unterseite** | Neun Beispiele mit Bildern sprengen eine Seite. Die vorhandenen Sprungmarken bleiben über das Redirection-Plugin erreichbar |
| **E8** | **Veröffentlichung über die WordPress-REST-Schnittstelle mit Anwendungskennwort** | Standard seit WP 5.6, kein eigenes Plugin nötig, kein FTP, keine Datenbankschreibrechte |
| **E9** | **Erzeugter Bereich zwischen Markern**, Rest der Seite bleibt unberührt | Einleitung, Fusion-Elemente und Handzusätze überleben jede Aktualisierung |
| **E10** | **Neue Seiten als Entwurf; Aktualisierung geht erst mit `-Freigeben` live** | Ein Rechenfehler soll nicht still die öffentlich sichtbaren Zahlen umschreiben |
| **E11** | **Kennzahl-Vergleich mit Abbruchschwelle** bei jedem Bauvorgang | Ein verändertes Beispielergebnis ist entweder ein Fix (dann in den Text) oder ein Regressionsschaden (dann Stopp). Beides muss auffallen |
| **E12** | **Deutsch zuerst**, Struktur zweisprachig angelegt (`bilder/de/`, `bilder/en/`) | Englische Bilder verdoppeln die Aufnahmezeit; die Ablage soll dafür nicht später umgebaut werden |
| **E13** | **Aufgabe öffentlich, Lösung angemeldet** — wie heute schon praktiziert | Der bestehende Zugriffsschutz der Seite bleibt das Vorbild; die Trennung wird nur maschinenlesbar gemacht |

---

## 4. Aufbau eines Beispiels

### 4.1 `beispiel.yaml`

```yaml
slug: einfamilienhaus-waermepumpe
reihenfolge: 10
titel:
  de: Einfamilienhaus mit Wärmepumpe
kurzbeschreibung:
  de: Ölkessel im Bestand auf Luft/Wasser-Wärmepumpe umstellen, zwei Vorlauftemperaturen im Vergleich.
schwierigkeit: einsteiger
dauer_minuten: 25
sichtbarkeit:
  aufgabe: oeffentlich
  loesung: angemeldet
projekt: projekt.epx
varianten: [stamm, "55 Grad Bestand", "40 Grad nach Heizkörpertausch"]
sprachen: [de]

# Bezugsliste — steuert die Prüfpflicht (Kapitel 9.2)
bereiche: [Gebaeude, Brauchwasser, Waermepumpe, Heizstab, Klimaregion]
code_anker:
  - WindowsFormsApplication1/Allgemein/Simulation/SimulationWaermepumpe.cs
  - WindowsFormsApplication1/Views/Simulation/Form_Simulation_Config.cs
  - WindowsFormsApplication1/Allgemein/Bericht/KennzahlenKatalog.cs

stand:
  programmversion: 1.0.3.0
  commit: a1b2c3d4
  geprueft_am: 2026-08-16
  geprueft_von: PE
```

### 4.2 `text.md` — der einzige handgeschriebene Teil

Die Gliederung ist verbindlich und übernimmt das Muster der heutigen Seite:

```markdown
## Ausgangssituation
Ein bestehendes Einfamilienhaus, Baujahr 1985, rund 160 Quadratmeter Wohnfläche …

## Vorgehen
Legen Sie das Projekt mit den Bausteinen Gebäude, Brauchwasser und Wärmepumpe an.

{{bild:projektbaum}}

Statt der Wohnfläche geben Sie als Bezugsgröße den Ölverbrauch an; EPOS-Plan rechnet
ihn mit dem Jahresnutzungsgrad des alten Kessels in Nutzwärme um — im Beispiel ergibt
das einen Jahreswärmebedarf von {{kz:Waermebedarf_gesamt}}.

## Auslegung
{{bild:wp-kennfeld}}

Die Simulation weist für die Bestandsvariante eine Jahresarbeitszahl von
{{kz:JAZ|variante=55 Grad Bestand}} aus, nach Heizkörpertausch
{{kz:JAZ|variante=40 Grad nach Heizkörpertausch}}.

{{tab:vergleich}}

## Worauf zu achten ist
Der Heizstab trägt {{kz:Heizstabanteil}} zur Jahreswärme bei; oberhalb von etwa fünf
Prozent ist die Maschine zu klein oder die Vorlauftemperatur zu hoch.

{{bild:leistung-ueber-aussentemperatur}}
```

Drei Platzhalterarten, mehr nicht:

| Platzhalter | Auflösung |
|---|---|
| `{{kz:<Kennzahl>[\|variante=…]}}` | Wert aus `ergebnis.json`, formatiert nach `KennzahlenKatalog` (Einheit, Nachkommastellen, `CultureInfo` der Zielsprache) |
| `{{bild:<id>}}` | Bild aus `bilder/<sprache>/`, mit Bildunterschrift aus `schritte.yaml` |
| `{{tab:<name>}}` | erzeugte Tabelle — `vergleich` (Variantenvergleich), `kennzahlen`, `komponenten` |

Ein unauflösbarer Platzhalter bricht den Bauvorgang ab. Das ist der Sinn der Sache:
Wird eine Kennzahl umbenannt oder entfällt sie, meldet sich jedes Beispiel, das sie zitiert.

### 4.3 `schritte.yaml` — das Drehbuch

```yaml
- id: projektbaum
  art: maske
  form: Form_Projektbaum
  projekt: stamm
  fenster: [1600, 1000]
  unterschrift:
    de: Projektbaum nach dem Anlegen der Bausteine Gebäude, Brauchwasser und Wärmepumpe

- id: wp-kennfeld
  art: maske
  form: Form_Waermepumpe
  vorbelegung: { reiter: Kennfeld, modul: 1 }
  ausschnitt: inhalt              # ohne Fensterrahmen
  markierung:
    - bereich: [420, 180, 260, 90]
      text: { de: "Kennfeld für 55 °C Vorlauf" }
  unterschrift:
    de: Auswahl des Kennfelds — zwischen 35 und 55 Grad liegen schnell 30 Prozent Leistungszahl

- id: leistung-ueber-aussentemperatur
  art: diagramm                   # ScottPlot, kein Bildschirmfoto
  quelle: bericht:LeistungUeberAussentemperatur
  variante: "55 Grad Bestand"
  unterschrift:
    de: Ab etwa −4 °C deckt die Wärmepumpe den Bedarf nicht mehr allein
```

Markierungen (Rahmen, Pfeile, Beschriftungen) stehen **im Drehbuch, nicht im Bild**.
Ein von Hand in einem Bildbearbeiter gesetzter roter Pfeil überlebt die nächste
Programmversion nicht — eine Koordinatenangabe wird beim nächsten Lauf neu gezeichnet
und, wenn sie nicht mehr passt, wenigstens sichtbar falsch.

---

## 5. Die Bilder

### 5.1 Drei Klassen, drei Verfahren

| Klasse | Anteil (Schätzung) | Verfahren |
|---|---|---|
| **Ergebnisdiagramme** — Jahresganglinie, Jahresdauerlinie, Leistung über Außentemperatur, Ringdiagramm der Deckung, Autarkieanalyse | ca. 60 % | ScottPlot off-screen, derselbe Aufruf wie im Bericht. Kein Fenster, kein Bildschirm, kein Rahmen |
| **Programmmasken** — Projektbaum, Konfigurationsübersicht, Katalogauswahl, Eingabedialoge, Kostenmaske | ca. 35 % | Aufnehmer in der Anwendung (5.2) |
| **Schemata und Skizzen** | ca. 5 % | von Hand gezeichnet, als Datei im Ordner; ändern sich selten und tragen keine Zahlen |

Die erste Zeile ist der Hebel: Der Großteil der Bilder braucht überhaupt kein
Bildschirmfoto, weil die Grafiken für den Bericht ohnehin off-screen erzeugt werden.

### 5.2 Der Aufnehmer in der Anwendung

Neuer Kommandozeilenschalter — er setzt voraus, dass `Program.Main` Argumente entgegennimmt
(heute nicht der Fall, siehe Setup-Konzept Abschnitt 10):

```
EPOS-Plan.exe --beispiel-aufnehmen "C:\Waermeplan\WP_Plan\Beispiele\10-einfamilienhaus-waermepumpe" --sprache de
```

Ablauf je Schritt: Beispieldatenbank verbinden → Projekt bzw. Variante aktiv setzen →
Formular über eine Zuordnungstabelle `Formularname → Erzeuger` öffnen → Vorbelegung
anwenden → Layout abwarten → aufnehmen → schließen.

**Aufnahmetechnik.** `Control.DrawToBitmap` ist der bequeme Weg, versagt aber bei
`WinForms.DataVisualization`-Diagrammen und bei allem, was das Betriebssystem selbst
zeichnet (aufgeklappte Auswahllisten). Deshalb: Fenster in den Vordergrund, feste
Position und Größe, `Graphics.CopyFromScreen` auf den Fensterbereich, Rahmen nach
Bedarf abschneiden. Das verlangt eine **angemeldete, interaktive Windows-Sitzung** —
der Bauvorgang läuft also auf dem Entwicklungsrechner oder einer dedizierten
Windows-VM, nicht in einer Build-Pipeline ohne Desktop. Das ist keine neue
Einschränkung: Die Auslieferungskette ist aus anderen Gründen ohnehin manuell
(Setup-Konzept Abschnitt 8).

**Determinismus (E5).** Ohne diese fünf Festlegungen erzeugt jeder Lauf lauter
„geänderte" Bilder:

1. Bildschirmauflösung und Skalierung fest (1920 × 1200, 100 %), Anzeige-DPI 96
2. Fenstergröße je Schritt fest, keine gespeicherten Fensterpositionen aus `user.config`
3. eigenes Windows-Benutzerprofil für die Aufnahme, damit Einstellungen reproduzierbar sind
4. neutrale Demo-Lizenz — kein Lizenznehmername, keine E-Mail-Adresse im Bild
5. feste Systemschrift und feste Windows-Design-Einstellung

Beim Veröffentlichen wird zusätzlich je Bild eine SHA-256-Prüfsumme geführt; nur
tatsächlich veränderte Bilder gehen in die Mediathek (Kapitel 8.3).

### 5.3 Stufe 2: der Aufzeichnungsmodus

Das Drehbuch von Hand zu schreiben ist beim ersten Beispiel mühsam. Deshalb ein
zweiter Schalter, der die Richtung umdreht:

```
EPOS-Plan.exe --beispiel-mitschneiden "…\10-einfamilienhaus-waermepumpe"
```

Ein Haken an `Form.Activated` legt bei jedem geöffneten Formular ein Bild und einen
Protokolleintrag ab (Formularname, aktives Projekt, aktive Variante, Zeitpunkt). Der
Bearbeiter klickt das Beispiel **einmal normal durch**; aus dem Mitschnitt entsteht der
erste Entwurf von `schritte.yaml`, den Claude anschließend ordnet, benennt und um
Unterschriften ergänzt.

Aufzeichnen macht das Erstellen schnell, das Drehbuch macht es **wiederholbar**. Beides
wird gebraucht — aber das Drehbuch zuerst, sonst hängt die Wiederholbarkeit an einem
Menschen, der sich an die Klickfolge erinnert.

---

## 6. Projektdaten und Beispieldatenbank

### 6.1 Die Beispieldatenbank

`Beispiele\Beispiele.accdb` entsteht wie die Auslieferungsdatenbank (Setup-Konzept 6.1):
Kopie ziehen, alle Projektdaten löschen, Kataloge mit `ReadOnly = TRUE` behalten,
komprimieren. Danach werden ausschließlich die Beispielprojekte eingespielt. Damit ist
ausgeschlossen, dass in einer Projektauswahlliste ein Kundenname erscheint — und die
Bereinigung wird für beide Zwecke nur einmal gebaut.

### 6.2 `projekt.epx` — offener Punkt mit Kostenfolge

Damit ein Beispiel reproduzierbar und zugleich herunterladbar ist, braucht es einen
**Projektexport über Datenbankgrenzen hinweg**. Vorhanden sind heute
`ProjektDuplizierenCtrl` (kopiert innerhalb einer Datenbank) und `CsvExportClass`
(exportiert Ergebnisse, keine Eingaben) — ein Projektexport existiert **nicht**.

Vorschlag: `.epx` als ZIP mit `projekt.json` (alle Zeilen des Projektbaums nach der
`FK_MAP`-Systematik aus `ProjektDuplizierenCtrl`), `manifest.json` (Schemastand,
Programmversion) und optionalen Anlagen (CSV-Lastgänge). Import ordnet Katalogbezüge
über die fachlichen Schlüssel neu zu, nicht über IDs.

Das ist der teuerste Einzelposten des Konzepts (Kapitel 11) — er zahlt aber auf drei
Ziele gleichzeitig ein: Beispiel-Reproduzierbarkeit, Download für Anwender („Beispiel
laden" im Programm) und Projektaustausch zwischen Anwendern, der ohnehin gefragt wird.

**Rückfallebene für Stufe 1:** Das Beispiel lebt nur in `Beispiele.accdb`, der Download
ist der erzeugte Musterbericht. Funktioniert sofort, verschiebt den Export, verliert
aber die Möglichkeit, dass Anwender das Beispiel im eigenen Programm nachvollziehen.

---

## 7. Der Bauvorgang

```powershell
cd C:\Waermeplan\WP_Plan\Beispiele
.\build-beispiele.ps1                          # alle Beispiele, nur bauen
.\build-beispiele.ps1 -Beispiel einfamilienhaus-waermepumpe
.\build-beispiele.ps1 -OhneBilder              # nur Zahlen und Text, schnell
.\build-beispiele.ps1 -Veroeffentlichen        # als Entwurf/Revision hochladen
.\build-beispiele.ps1 -Veroeffentlichen -Freigeben
```

Sieben Schritte je Beispiel:

1. **Prüfen** — `beispiel.yaml` gegen Schema, Platzhalter gegen `KennzahlenKatalog`
2. **Rechnen** — Projekt (und Varianten) über `SimulationRunner` headless, Wirtschaftlichkeit
   nachrechnen → `ergebnis.json`
3. **Vergleichen** — gegen das zuletzt veröffentlichte `ergebnis.json` (7.1)
4. **Zeichnen** — Diagramme über ScottPlot
5. **Aufnehmen** — Masken über den Aufnehmer nach Drehbuch
6. **Setzen** — Platzhalter auflösen, Markdown → HTML, Musterbericht erzeugen
7. **Veröffentlichen** — nur mit Schalter (Kapitel 8)

### 7.1 Der Kennzahl-Vergleich (E11)

```
Beispiel 10-einfamilienhaus-waermepumpe   (1.0.2.0 → 1.0.3.0)
  Wärmebedarf gesamt       18.420 kWh/a → 18.420 kWh/a      —
  JAZ (55 Grad Bestand)              3,41 → 3,29      −3,5 %   ⚠
  Heizstabanteil                      4,2 % → 6,8 %   +2,6 PP  ⚠
  Bivalenzpunkt                     −4,0 °C → −4,0 °C        —
  Kapitalwert                      12.340 € → 11.180 €  −9,4 % ⚠

  3 Kennzahlen über der Schwelle (2 %). Bauvorgang angehalten.
  Ursache prüfen, dann -Uebernehmen setzen und die Änderung in text.md würdigen.
```

Damit wird die Beispielsammlung nebenbei zur **zweiten Referenzlauf-Suite** neben
Paket B1 des Simulationskonzepts — mit dem Unterschied, dass hier vollständige,
fachlich sinnvolle Projekte gerechnet werden statt anonymer Vektoren. Ein Fehler, den
kein Testfall trifft, fällt hier auf, weil das Ergebnis in einem Text steht, den ein
Mensch versteht.

---

## 8. Veröffentlichung auf epos-plan.de

### 8.1 Seitenaufbau

```
Schulung
└── Onlineschulung
    └── Beispiele                     ← Übersicht, Einleitung von Hand
        │                               + erzeugte Kachelliste zwischen Markern
        ├── Einfamilienhaus mit Wärmepumpe      ← vollständig erzeugt
        ├── Mehrfamilienhaus
        ├── Verwaltungsgebäude
        └── …
```

Die heutigen Sprungmarken (`#ab-efh`, `#ab-mfh` …) werden über das vorhandene
Redirection-Plugin auf die neuen Unterseiten geführt; bestehende Verweise und
Suchmaschinentreffer bleiben gültig.

Auf jeder Unterseite: Kopfangaben (Schwierigkeit, Dauer, Programmversion, Stand),
**Aufgabe** (öffentlich), **Lösung** mit Bildern und Zahlen (angemeldet), Downloads
(`projekt.epx`, Musterbericht) über den vorhandenen WP Download Manager, Verweise auf
die zugehörigen Grundlagen- und Programmablauf-Seiten.

### 8.2 Schnittstelle

WordPress-REST mit Anwendungskennwort (`WP-Admin → Profil → Anwendungskennwörter`),
Zugangsdaten im Windows-Anmeldeinformationsspeicher, nie im Repository:

| Zweck | Endpunkt |
|---|---|
| Seite anlegen/ändern | `POST/POST-Update /wp-json/wp/v2/pages` |
| Bild hochladen | `POST /wp-json/wp/v2/media` |
| Vorhandenes finden | `GET /wp-json/wp/v2/pages?slug=…&parent=…` |

### 8.3 Wiederholbarkeit — der wichtigste Teil

Ohne Zustandsführung erzeugt jeder zweite Lauf doppelte Seiten und eine Mediathek voller
identischer Diagramme. Deshalb `veroeffentlichung.json` je Beispiel:

```json
{
  "seiten_id": 2841,
  "eltern_id": 1180,
  "inhalt_hash": "9f2c…",
  "medien": {
    "projektbaum.png": { "id": 3902, "sha256": "a71b…" },
    "wp-kennfeld.png": { "id": 3903, "sha256": "c04e…" }
  },
  "veroeffentlicht_am": "2026-08-16T14:20:00+02:00",
  "programmversion": "1.0.3.0"
}
```

Regeln: Bild nur hochladen, wenn die Prüfsumme abweicht — dann **dieselbe Medien-ID
ersetzen**, nicht neu anlegen. Seite nur schreiben, wenn der Inhalts-Hash abweicht.
Der erzeugte Bereich steht zwischen Markern:

```html
<!-- epos:beispiel:start slug="einfamilienhaus-waermepumpe" version="1.0.3.0" -->
…
<!-- epos:beispiel:end -->
```

Alles außerhalb der Marker bleibt unangetastet — Handzusätze, Fusion-Elemente,
Einleitungen überleben.

### 8.4 Der Umgang mit dem Theme

Avada/Fusion Builder kann eingefügtes HTML umbauen. Deshalb: **erzeugtes HTML bewusst
schlicht** — Überschriften, Absätze, Listen, Tabellen, `<figure>`/`<figcaption>`,
keine eigenen Klassen außer einem Präfix `epos-bsp-`, das Aussehen kommt aus dem Theme
plus wenigen Regeln im Customizer.

Sollte das Theme dennoch stören, ist der Ausweichweg festgelegt und ausdrücklich
**nicht** der erste Weg: ein kleines eigenes Plugin mit einem Kurzbefehl
`[epos_beispiel slug="…"]`, das aus hochgeladenem JSON rendert. Sauberer im Ergebnis,
aber eine selbstgebaute Erweiterung auf einer Produktivseite mit WooCommerce — das
nimmt man erst, wenn Weg eins scheitert.

### 8.5 Zugriffsschutz

Die Trennung „Aufgabe öffentlich, Lösung nach Anmeldung" besteht heute schon. Welches
Plugin sie durchsetzt, ist noch zu ermitteln (Kapitel 12, B3); der Erzeuger klammert
den Lösungsteil in den dort üblichen Kurzbefehl. Bis das geklärt ist: Lösungsteil als
eigene, nicht öffentlich verlinkte Unterseite.

---

## 9. Wie die Beispiele aktuell bleiben

Zwei Richtungen, die zusammen den Kern der Aufgabenstellung abdecken.

### 9.1 Von selbst: was der Bauvorgang mitzieht

Rechenwegänderungen, neue oder korrigierte Kennzahlen, geänderte Diagrammdarstellung,
umgestaltete Masken, neue Beschriftungen — all das erscheint beim nächsten Lauf ohne
Zutun in der veröffentlichten Fassung. Der Kennzahl-Vergleich sagt dabei, **was** sich
geändert hat.

**Empfohlener Takt:** ein vollständiger Lauf vor jeder Auslieferung, gemeinsam mit dem
Setup-Bau. Ein Beispiel, dessen Zahlen nicht mehr zur ausgelieferten Version passen,
ist schädlicher als gar keins.

### 9.2 Mit Nachhilfe: was Text braucht

Kommt ein Modul dazu, wandert ein Feld, ändert sich eine Empfehlung, dann muss der
**Text** nach — und das kann keine Automatik. Der Mechanismus dafür ist die
**Bezugsliste** aus `beispiel.yaml`:

```
$ .\build-beispiele.ps1 -Pruefen

Seit dem Stand der Beispiele geänderte Dateien mit Bezug:

  10-einfamilienhaus-waermepumpe   ⚠ prüfbedürftig
      Views/Simulation/Form_Simulation_Config.cs   (14 Commits, zuletzt 12.08.)
      → betrifft Schritt "projektbaum", "wp-kennfeld"

  40-schwimmbad                    ⚠ prüfbedürftig
      Allgemein/Simulation/SimulationBHKW.cs       (3 Commits)

  20-mehrfamilienhaus              ok
```

Ein neues Modul ohne jedes Beispiel wird ebenfalls gemeldet: Enthält das Verzeichnis
`Allgemein/Simulation/` einen Bereich, den keine Bezugsliste nennt, erscheint er als
**Lücke**. So wächst die Sammlung mit dem Funktionsumfang, statt hinter ihm zurückzubleiben.

### 9.3 Anbindung an die Kontexthilfe

`help_mapping.txt` verbindet heute Programmbereiche mit Kapiteln der Online-Dokumentation
(und ist laut Setup-Konzept S2 nicht einmal im Projekt eingetragen — bei dieser
Gelegenheit zu beheben). Der Bauvorgang schreibt je Beispiel zusätzliche Zuordnungen
zurück: Wer im Wärmepumpendialog steht, bekommt neben dem Grundlagenkapitel den Verweis
„Beispiel: Einfamilienhaus mit Wärmepumpe". Damit findet der Anwender das Beispiel dort,
wo er es braucht — und ein neues Modul bekommt Hilfeverweis und Beispiel im selben Commit.

---

## 10. Die Rolle von Claude

Drei klar getrennte Aufgaben, alle im Repository, alle mit menschlicher Freigabe.

### 10.1 Erstaufnahme

Der Bearbeiter klickt das Beispiel einmal durch (mit `--beispiel-mitschneiden`, Stufe 2)
oder beschreibt es. Auftrag im Plan-Modus:

> Lies `Beispiele/_vorlage/`, `Beispiele/10-einfamilienhaus-waermepumpe/mitschnitt/`
> und die bestehende Beispielseite von epos-plan.de. Erstelle `text.md` nach dem Muster
> *Ausgangssituation · Vorgehen · Auslegung · Worauf zu achten ist*, dazu `schritte.yaml`
> und `beispiel.yaml`. Alle Zahlen als Platzhalter, keine erfundenen Werte. Nenne
> anschließend, welche Kennzahlen der Katalog nicht führt.

### 10.2 Nachführung

> `-Pruefen` meldet `10-einfamilienhaus-waermepumpe` als prüfbedürftig wegen Änderungen
> in `Form_Simulation_Config.cs`. Lies den Diff seit Commit a1b2c3d4, dann `text.md`,
> `schritte.yaml` und die Bilder in `bilder/de/`. Schlage die kleinstmögliche Änderung
> vor: Welche Sätze stimmen nicht mehr, welche Schritte zeigen eine Maske, die es so
> nicht mehr gibt? Noch nichts ändern.

Claude kann die erzeugten Bilder ansehen — die Prüfung „beschreibt der Text noch, was
das Bild zeigt" ist damit tatsächlich leistbar und nicht nur behauptet.

### 10.3 Abnahme vor der Veröffentlichung

> Vergleiche `ausgabe/beispiel.html` mit `ergebnis.json` und `text.md`: fachliche
> Plausibilität, Einheiten, Verweise auf Grundlagenseiten, Verständlichkeit für einen
> Anwender ohne Vorkenntnis. Keine Änderungen, nur eine Mängelliste.

**Was Claude nicht tut:** freigeben. `-Freigeben` bleibt ein bewusster Handgriff eines
Menschen, der die Zahlen gesehen hat.

---

## 11. Stufenplan und Aufwand

Bewusst in drei Stufen — Stufe 1 liefert schon eine bebilderte, gepflegte Seite.

### Stufe 1 — Tragfähiger Kern (ohne Maskenautomatik)

| Paket | Inhalt | PT |
|---|---|---|
| B-1 | Ordnerstruktur, Schemata, Platzhalterauflösung, Markdown → HTML | 2,0 |
| B-2 | Kennzahlausgabe `ergebnis.json` über Datensammler und Kennzahlenkatalog | 1,5 |
| B-3 | Diagramme über den vorhandenen ScottPlot-Bildpfad | 1,5 |
| B-4 | Veröffentlichung nach WordPress: REST, Marker, Idempotenz, Entwurfsmodus | 2,5 |
| B-5 | Beispieldatenbank aufsetzen (mit Setup-Paket S-3 gemeinsam) | 0,5 |
| B-6 | Erstes Beispiel vollständig, Maskenbilder zunächst von Hand | 1,5 |
| | **Summe Stufe 1** | **9,5** |

### Stufe 2 — Bilder werden reproduzierbar

| Paket | Inhalt | PT |
|---|---|---|
| B-7 | Aufnehmer: Kommandozeilenargumente, Drehbuchabspieler, feste Fenstergröße, Markierungen | 3,5 |
| B-8 | Aufzeichnungsmodus als Entwurfshilfe | 2,0 |
| B-9 | Bezugsliste, `-Pruefen`, Lückenmeldung, Claude-Aufträge | 1,5 |
| B-10 | Bestand umstellen: neun Beispiele mit Bildern und echten Zahlen | 4,0 |
| | **Summe Stufe 2** | **11,0** |

### Stufe 3 — Beispiele zum Mitnehmen

| Paket | Inhalt | PT |
|---|---|---|
| B-11 | Projektexport/-import `.epx` | 3,0 |
| B-12 | „Beispiel laden…" im Programm, Auslieferung der Beispiele mit dem Setup | 1,5 |
| B-13 | Englische Fassung: Text, Bilder, zweite Seitenreihe | 3,0 |
| | **Summe Stufe 3** | **7,5** |

**Gesamt 28 PT**, davon 9,5 bis zur ersten sichtbaren Verbesserung. Ein weiteres
Beispiel danach: 0,5 bis 1 PT — der Aufwand liegt dann fast vollständig im Erzähltext.

---

## 12. Was das Verfahren bewusst nicht tut

- **Keine Videos.** Bewegtbild altert schneller als Text und lässt sich nicht teilweise
  erneuern. Wenn Video, dann als eigenständiges Format neben, nicht statt der Beispiele.
- **Kein automatisches Live-Schalten.** Siehe E10.
- **Keine Übersetzung des Erzähltexts durch die Kette.** Fachtext übersetzt sich nicht
  nebenbei; die englische Fassung ist eigene Arbeit (B-13).
- **Kein Ersatz für die Grundlagenseiten.** Beispiele zeigen den Weg, Grundlagen
  begründen ihn. Das Beispiel verweist, es erklärt nicht neu.
- **Keine Beispiele mit Kundendaten** — auch nicht anonymisiert. Objekte werden neu
  konstruiert, mit plausiblen, aber erfundenen Randbedingungen.

---

## 13. Offene Punkte

| Nr. | Punkt | Nächster Schritt |
|---|---|---|
| B1 | `Program.Main` nimmt heute keine Argumente entgegen — Voraussetzung für Aufnehmer und Aufzeichnung (identisch zum offenen Punkt beim Setup) | Argumentbehandlung einbauen, gemeinsam mit der Lizenzdatei-Verknüpfung |
| B2 | Welche der neun Beispielseiten und elf Download-Aufgaben bleiben, welche werden zusammengeführt? Elf Download-Aufgaben und neun Textbeispiele überschneiden sich | Einmal durchsehen und eine Zielliste festlegen, bevor B-10 beginnt |
| B3 | Welches Plugin setzt „Lösung erst nach Anmeldung" heute durch? | Im WP-Backend nachsehen; Kurzbefehl in die Vorlage übernehmen |
| B4 | Verträgt die Avada-Seite eingefügtes HTML unverändert? | Einmal an einer Testseite ausprobieren — entscheidet über 8.4 |
| B5 | Wo läuft der Bauvorgang dauerhaft? Er braucht eine interaktive Windows-Sitzung | Entwicklungsrechner oder kleine Windows-VM festlegen |
| B6 | Sollen die Beispiele mit dem Setup ausgeliefert werden? Das berührt die Setup-Größe (dort E1/E9) | Nach B-11 entscheiden, wenn die Größe von `.epx` bekannt ist |
| B7 | Welche Kennzahlen fehlen im `KennzahlenKatalog`, die die Beispieltexte brauchen (z. B. „minimale Spitzenkesselleistung")? | Beim ersten Beispiel sammeln und den Katalog ergänzen |
| B8 | Zweisprachige Bildunterschriften bei einsprachigen Bildern — bleibt das Bild deutsch mit englischer Unterschrift oder wird neu aufgenommen? | Mit B-13 entscheiden; Aufnahme in beiden Sprachen kostet nur Rechenzeit, kein Handwerk |

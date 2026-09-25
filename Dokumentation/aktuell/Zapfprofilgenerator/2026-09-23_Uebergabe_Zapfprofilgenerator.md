# Übergabe: Zapfprofilgenerator und Brauchwasserauslegung — Stand 23.09.2026

Der Auftrag „Konzept Zapfprofilgenerator mit Mockup darstellen, Excel-Vorlage TWW-Auslegung V4
als Vorlage, Normtabellen zu Testzwecken, Umsetzungskonzept" wechselt auf ein anderes Konto.
Dieses Papier nennt den fertigen Stand, was aussteht, die geltenden Regeln und die lokalen
Bestände, die nicht im Repositorium liegen und deshalb von Hand mitzunehmen sind.

## 1 Was fertig ist

| Ergebnis | Wo | Stand |
|---|---|---|
| Mockup, Fassung 3 | [`../Mockups/Zapfprofilgenerator_Mockup.html`](../Mockups/Zapfprofilgenerator_Mockup.html) (3257 Zeilen, CRLF, ohne externe Abhängigkeiten) | Abschnitte 0–8, dazu 3b „Normtabellen als Testdaten" und 5b „Was es schon gibt"; Überlagerungen Auslegung, Tagesgang bearbeiten, Katalogdialog; Stufen Einfach · Erweitert · Experte; zwei Gegenprüfungen (18 + 12 Befunde) eingearbeitet; Hinweiskasten Anwenderentscheid in Abschnitt 8 |
| Umsetzungskonzept, Fassung 2 mit Nachtrag N1 | [`../Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md`](../Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md) (1545 Zeilen) | Kapitel 0–10, Kapitel 11 Nachträge (N1), Anhang A Auftragsblatt Z0; zwei Gegenprüfungen (28 fachliche, 16 technische Befunde) eingearbeitet; Indexzeile in `Dokumentation/LIESMICH.md` |
| Anwenderentscheid N1 (23.09.2026) | Umsetzungskonzept Kapitel 9 (Spalte „Entscheid") und Kapitel 11 | K1, K8, ZU1–ZU14 und ZU15 (Lizenzfrage der VDI-6002-Kopien) **nach Empfehlung**; K2–K7 samt K3a und A1–A12 bleiben „Empfehlung vorausgesetzt" |
| Vorlagenanalyse der Excel-Mappe TWW-Auslegung V4 | [`2026-09-22_Vorlagenanalyse_TWW-Auslegung_V4.md`](2026-09-22_Vorlagenanalyse_TWW-Auslegung_V4.md) | Aufbau je Blatt, Formelsammlung, Bedienlogik, Abgleich mit dem Konzept, Bestand außerhalb des Repositoriums, Lizenzregeln, Empfehlung; vier Schwächen der Mappe benannt und nicht übernommen |
| Normtabellen als lokale Testdaten | `Referenzlaeufe/Normzahlen/vdi4655/`, `vdi6002/`, `zapfprofil/normtabellen.js` — **gitignoriert, nicht im Repositorium** (Abschnitt 4) | VDI 4655: Typtage, Klimazonen, Typtage je Zone (Bestand und NEH), 450 Faktorwerte F_TWE,TT vollständig gegen die PDF-Textschicht geprüft, vier Werte gegenüber Grundlagen 5 berichtigt; VDI 6002: acht Nutzungsarten, fünfzehn Tagesprofile, Wochenanteile, Monatsfaktoren; Ladedatei für das Mockup |
| Grundlagen 5 berichtigt | [`../Grundlagen_5_VDI-4655_Auswertung.md`](../Grundlagen_5_VDI-4655_Auswertung.md) | vier Faktorwerte, Prüfsummen und Sitzungsreste bereinigt (Commit 40850ee5, eigene Sitzung des Anwenders) |
| Artefakt des Mockups | Veröffentlichung dieses Kontos (privat), Version 2 | ohne Lader, ohne Normzahlen; auf dem anderen Konto aus der Repo-Datei neu erzeugen (Skript in Abschnitt 5) |

Commits auf `ios_migration_september`, alle gepusht: f6971d11 (Sync: Mockup Fassung 2, Index,
gitignore-Block `Referenzlaeufe/Normzahlen/`), 82cc0986 (Umsetzungskonzept Fassung 2, Mockup
Fassung 3, Indexzeile), 4929d432 (Nachtrag N1, Auftragsblatt Z0, Hinweiskasten im Mockup).

## 2 Was aussteht

### 2.1 Beim Anwender (Folgen aus N1)

- **K1:** A100-Profildateien, Weißdruck-Status, DIN 4708-2 und -3 anfragen; Verzicht auf eine
  Verwertungslizenz zur Mitauslieferung bestätigen. Bis dahin rechnen Z1 und Z2 mit dem fiktiven
  Testkatalog.
- **K8:** juristische Prüfung mit Z0 beauftragen: lokale Normkopien, die vier aus VDI-6002-Bildern
  digitalisierten Typprofile im Auslieferungskatalog, Zahlenteile der Grundlagenpapiere.
- **ZU15:** Lizenz der VDI-6002-Kopien in der Ablage des Anwenders (Lizenzstempel einer
  Universität) prüfen oder eigene beschaffen; die extrahierten Tabellen bleiben bis dahin lokal
  und werden nicht weitergegeben.

### 2.2 Stufe Z0 — Grundlagen und Schema (nächster Auftrag)

Das Auftragsblatt steht als **Anhang A des Umsetzungskonzepts** und ist ohne weiteren Kontext
beauftragbar: Ziel, Vorbedingungen, vierzehn Posten P1–P14 mit Abschnittsverweisen (Schemaschritt
T1 mit zehn STRICT-Tabellen als nächster freier Schritt nach `SchemaStand.Zielversion` — nachmessen,
zuletzt 100; `TwwSchema.cs`; `ZapfprofilCtrl` lesend; `TwwNutzungsartCtrl`; Eintrag in der
`KatalogRegistry`; fiktiver Testkatalog; Migration der Testdatenbank über LFS; Posten der
Auslieferungsvorlage; Wache für lokale Normdaten (ZU11); Kopierstellen-Tests; P13 Quellendossier
vor Beginn zuschneiden; P14 A9/A10 ruht bis K8), Reihenfolge, Nicht-Umfang (keine Rechenklassen,
keine Oberfläche, kein Referenzprojekt), Abnahme (Kern-Filter bauen und testen mit den
xUnit-Schaltern, Referenzlauf der fünf CI-Projekte gegen die aktuelle Basis, SqlDialektPruefer,
Wachen, Windows-Schale auf Linux gebaut, Schemastand in `Referenzlaeufe/LIESMICH.md`), Aufwand
7–10 PT. Ausführung: Agent `model: opus` im eigenen Worktree, `AGENT_LAEUFT` im Hauptbaum, Stand
sofort committen, kein Push ohne Auftrag.

### 2.3 Danach

Z1 Bilanz deterministisch mit Weiche → Z2 Auslegung samt Speicherauslegung → Z3 Stochastik →
Z4 Oberfläche (Z4b VDI-4655-Import getrennt; iPad erst nach Umzug der Bedarfsprofil-Hülle, A11)
→ Z5 Kalibrierung und Validierung; Herleitung und Abnahme je Stufe in Kapitel 7 des
Umsetzungskonzepts. Konzeptpapier V2 (A10) und Entfernen der Zahlenteile aus den
Grundlagenpapieren (A9) erst nach K8.

### 2.4 Kleinere offene Punkte

- Die Generatorskripte des Mockups (Scratch `zpg/`) sind überholt; die HTML-Datei ist führend.
  Änderungen direkt in der Datei, danach Artefakt neu erzeugen (Abschnitt 5).
- Die Speichergrößen-Auswahl im Wochendiagramm der Auslegung ist nur ein Bild, nicht bedienbar.
- Die Artefakt-Seite wurde nur per DOM geprüft; eine Sichtprüfung im Browser des Kontos steht aus.
- Das Umsetzungskonzept (Kapitel 1.2) vermerkt eine Abweichung zwischen
  `KONTEXT_Brauchwassertypen_VDI6002.md` (11 Wochenprofile, 13 Monatssätze) und der
  Testdatenbank (13 und 16); nicht geklärt.
- Der Satz „Vorlagenanalyse … nicht im Repositorium" in der Quellentabelle des Umsetzungskonzepts
  ist mit dieser Übergabe überholt; die Analyse liegt jetzt neben diesem Papier.

## 3 Regeln, die für diese Papiere gelten

- Markdown UTF-8 **ohne** BOM, **CRLF**; große Papiere gezielt lesen (`grep -n '^#'`). Das Mockup
  ebenso CRLF, eine Datei, keine externen Abhängigkeiten, heller und dunkler Modus, Fenster hell,
  Telefonbreite ohne waagrechtes Rollen.
- Entscheide nur als Nachtrag N2, N3 … in Kapitel 11 des Umsetzungskonzepts plus Spalte
  „Entscheid" in Kapitel 9; Nachträge nie umschreiben. Im Mockup der Hinweiskasten in Abschnitt 8.
- **Keine Normzahlen im Repositorium, im Artefakt, in der Testdatenbank oder der Auslieferung:**
  weder Faktoren, Typtaganzahlen oder Jahreswerte der VDI 4655 noch Stunden-, Wochen-,
  Monatsanteile oder Bedarfskennwerte der VDI 6002, keine Nachschlagewerte der DIN 4708-2.
  Formeln und Methodik dürfen stehen. Normzahlen nur lokal unter `Referenzlaeufe/Normzahlen/`
  (gitignoriert, Muster U8); das Mockup lädt sie zur Laufzeit und zeigt sonst Beispielzahlen.
- Keine Hersteller-, Produkt- oder Messobjektdaten; die Ablage des Anwenders (Ordner
  „Wärmespeicher") nur lesen, keine Pfade daraus in Papiere; Testdatenbank nur lesend.
- Fiktive, runde Beispielzahlen im Mockup, in sich stimmig und nachrechenbar (Referenzfall
  39,0 + 12,0 = 51,0 MWh/a, N = 18); INEKON-eigene Vorlagenwerte sind zulässig.
- Schemastand nie als feste Zahl behaupten: „nächster freier Schritt nach
  `SchemaStand.Zielversion` (nachmessen)". Bestandsangaben mit Datei und Zeile am Arbeitsbaum
  nachmessen, bevor sie geschrieben werden.
- Agentenarbeit im Hauptbaum nur mit eigener `AGENT_LAEUFT`; eine fremde nie anfassen; solange
  eine fremde liegt, weder bauen noch `dotnet test` im Hauptbaum.
- Kein Commit und kein Push ohne Auftrag; beauftragte Commits gezielt (`git add <pfad>`).
- Modellwahl (`CLAUDE.md`, Abschnitt „Modellwahl und Agenten"): Opus orchestriert und arbeitet;
  Agenten `model: opus` (Papiere, Nachzüge, Implementierung, Konfliktauflösung), `model: sonnet`
  (Suchen, kleine Textpflege), `model: haiku` (Zählungen, Encoding-Prüfungen); Fable nur, wenn
  Opus die Aufgabe nachweislich nicht leisten kann.
- Linkprobe ohne Build: Perl-Skript in der
  [Übergabe Gebäudesimulation](../Gebaeudesimulation/2026-09-22_Uebergabe_Gebaeudesimulation.md),
  Abschnitt 3.

## 4 Lokale Bestände, die mitzunehmen sind

Nicht im Repositorium, deshalb von Hand auf den anderen Rechner beziehungsweise in die andere
Umgebung zu bringen:

| Bestand | Inhalt | Ziel |
|---|---|---|
| `Referenzlaeufe/Normzahlen/vdi4655/` | `typtage.csv`, `klimazonen.csv`, `typtage_je_zone.csv`, `f_twe_tt.csv`, `kennwerte.csv`, `vdi4655.json`, `QUELLE.txt` | gleicher Pfad, gitignoriert |
| `Referenzlaeufe/Normzahlen/vdi6002/` | `bedarfskennwerte.csv`, `tagesprofile.csv`, `wochenanteile.csv`, `saisonfaktoren.csv`, `vdi6002.json`, `QUELLE.txt` | gleicher Pfad, gitignoriert |
| `Referenzlaeufe/Normzahlen/zapfprofil/normtabellen.js` | Ladedatei für das Mockup, aus beiden JSON-Dateien gebaut (`window.EPOS_NORMTABELLEN`) | gleicher Pfad; neu erzeugen mit dem Skript in Abschnitt 5 |
| Ablage des Anwenders, Ordner „Wärmespeicher" | Excel-Vorlage TWW-Auslegung V4, Word-Dokumentation des Wärmespeicher-Tools (Teile A–D), Streamlit-Wärmespeicher-Tool (Python), Grundlagen 1–5 als LF-Fassungen, Normen-PDFs | nur lesen; nichts davon ins Repositorium |

Die Zip-Datei `Normzahlen_zapfprofil_2026-09-23.zip` mit den drei Ordnern wurde dem Anwender im
Chat übergeben; sie ist in die Repowurzel zu entpacken (ergibt `Referenzlaeufe/Normzahlen/…`).
Das Mockup zeigt danach lokal das Abzeichen „Normzahlen lokal geladen"; ohne die Dateien bleibt
es bei den Beispielzahlen, und Abschnitt 3b zeigt den Platzhalter.

Hilfsdateien der Sitzung, die nicht mitzunehmen sind: der Scratch-Ordner der Sitzung
(Vorstände des Mockups, Excel-Abzug, Generatorskripte), das Workflow-Journal.

## 5 Skripte

Beide Skripte laufen aus der Repowurzel mit `py` (Windows) beziehungsweise `python3`; Ablage
außerhalb des Repositoriums (Scratch-Ordner der Sitzung oder ein Werkzeugordner des Anwenders).

**Ladedatei der Normtabellen bauen** (`normtabellen_bauen.py`):

```python
"""Baut Referenzlaeufe/Normzahlen/zapfprofil/normtabellen.js aus vdi4655.json und vdi6002.json."""
import io, json, os, sys

WURZEL = os.getcwd()
Q4655 = os.path.join(WURZEL, "Referenzlaeufe", "Normzahlen", "vdi4655", "vdi4655.json")
Q6002 = os.path.join(WURZEL, "Referenzlaeufe", "Normzahlen", "vdi6002", "vdi6002.json")
ZIELORDNER = os.path.join(WURZEL, "Referenzlaeufe", "Normzahlen", "zapfprofil")
ZIEL = os.path.join(ZIELORDNER, "normtabellen.js")

def laden(pfad):
    if not os.path.isfile(pfad):
        print("fehlt:", pfad); return None
    with io.open(pfad, encoding="utf-8-sig") as h:
        return json.load(h)

v4655, v6002 = laden(Q4655), laden(Q6002)
if v4655 is None and v6002 is None:
    sys.exit("keine Quelle gefunden")
daten = {"hinweis": "Lokale Testdaten (VDI 4655, VDI 6002) - lizenzpflichtig, nie ausliefern, nie committen (gitignore U8).",
         "erstellt": "2026-09-22", "vdi4655": v4655, "vdi6002": v6002}
os.makedirs(ZIELORDNER, exist_ok=True)
with io.open(ZIEL, "w", encoding="utf-8", newline="\r\n") as h:
    h.write("window.EPOS_NORMTABELLEN = " + json.dumps(daten, ensure_ascii=False, indent=1) + ";\n")
print("geschrieben:", ZIEL, os.path.getsize(ZIEL), "Byte")
```

**Artefakt-Fassung des Mockups erzeugen** (`artefakt_bauen.py`; Ergebnis ohne Dokumentgerüst,
ohne Lader, mit `color-scheme` in beiden Dunkelblöcken; danach als Artefakt veröffentlichen,
Titel „Zapfprofilgenerator Mockup", Symbol „chart"):

```python
"""Erzeugt aus dem Repo-Mockup die Artefakt-Fassung (ohne Dokumentgeruest, ohne Normtabellen-Lader)."""
import io, os, re, sys

QUELLE = os.path.join("Dokumentation", "aktuell", "Mockups", "Zapfprofilgenerator_Mockup.html")
ZIEL = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Zapfprofilgenerator_Mockup_artefakt.html")
TITEL = "Zapfprofilgenerator Mockup"

s = io.open(QUELLE, encoding="utf-8").read().replace("\r\n", "\n")
m_style = re.search(r"<style>(.*?)</style>", s, flags=re.S)
m_body = re.search(r"<body[^>]*>(.*)</body>", s, flags=re.S)
if not (m_style and m_body):
    sys.exit("style oder body nicht gefunden")
style, body = m_style.group(1), m_body.group(1)
body, n_lader = re.subn(r"\s*<script[^>]*src=\"[^\"]*normtabellen\.js\"[^>]*>\s*</script>", "", body, flags=re.I)

def ergaenze(block_kopf, css):
    m = re.compile(re.escape(block_kopf) + r"\s*\{").search(css)
    if not m or "color-scheme" in css[m.end():m.end() + 400]:
        return css
    return css[:m.end()] + "\n    color-scheme: dark;" + css[m.end():]

style = ergaenze(':root:not([data-theme="light"])', style)
style = ergaenze(':root[data-theme="dark"]', style)
if re.findall(r"(?:src|href)=\"https?://[^\"]+\"", body):
    sys.exit("externe Verweise im Body")
aus = "<title>%s</title>\n<style>%s</style>\n%s" % (TITEL, style, body.strip() + "\n")
io.open(ZIEL, "w", encoding="utf-8", newline="\n").write(aus)
print("geschrieben:", ZIEL, os.path.getsize(ZIEL), "Byte | Lader entfernt:", n_lader)
```

## 6 Fundstellen

| Was | Wo |
|---|---|
| Methodik (drei Lehren, Schichten S0–S6, Entscheidung D, Stufen der Eingabetiefe, Lizenzstrategie) | [Konzept V1.2](../Konzept_TWW-Zapfprofile_WP-Plan_1.md), Kapitel 1.2, 1.5, 2.4, 3.4 |
| Zielbild von Dialog, Überlagerungen, Katalogdialog; Stufen Z0–Z5; Entscheide K1–K8, A1–A12 | Mockup, Abschnitte 3, 6, 8 |
| Weiche, Klassen, Datenmodell, Rechenwege, Oberfläche, Lizenz, Stufenplan, Fragen, Nachträge, Auftragsblatt Z0 | Umsetzungskonzept, Kapitel 2–7, 9, 11, Anhang A |
| Speicherauslegung nach der Vorlage (Formeln, Bedienmuster, Schwächen) | Vorlagenanalyse, Kapitel 3, 4, 2.7 |
| Bestand des Brauchwasserkatalogs (VDI 6002, 168-h-Profile, Monatssätze) | [`KONTEXT_Brauchwassertypen_VDI6002.md`](../KONTEXT_Brauchwassertypen_VDI6002.md) |
| Lizenzlage VDI 4655 und Datenstrategie | [Grundlagen 5](../Grundlagen_5_VDI-4655_Auswertung.md), Abschnitte 0, 7.6–7.8; Umsetzungskonzept Kapitel 6 |
| Normzahlen der Gebäudesimulation (gleiches Muster U8) | [`Referenzlaeufe/Normzahlen/LIESMICH.md`](../../../Referenzlaeufe/Normzahlen/LIESMICH.md) |

## 7 Nachtrag 23.09.2026 — Stufe Z0 umgesetzt

Nach Abschnitt 2.2 wurde die Stufe Z0 noch in dieser Sitzung ausgeführt; der Übertrag setzt
damit bei Z1 an.

- **Ergebnis:** Posten P1–P13 umgesetzt (P14 ruht bis K8), Zweig `z0` in `ios_migration_september`
  zusammengeführt (Merge `6de8b031`, danach der Merge des GitHub-Stands der Gebäudesimulation),
  Statuszeile #438, Protokoll
  [`2026-09-23_Z0_Grundlagen_und_Schema.md`](../../ueberholt/Protokolle/Zapfprofilgenerator/2026-09-23_Z0_Grundlagen_und_Schema.md),
  Quellendossier [`Quellendossier_Zapfprofilgenerator.md`](Quellendossier_Zapfprofilgenerator.md),
  Nachträge N2–N4 im Umsetzungskonzept (Umsetzungsbefunde, Berichtigungen, Umnummerierung).
- **Schemanummern:** 101 Gebäudespalten (Gebäudesimulation, anderes Konto), 102 KWKG-Anlagenart
  (Wirtschaftlichkeit), **103 Tww-Tabellen (T1)**; `SchemaStand.Zielversion = 103`; Testdatenbank auf
  103 mit fiktivem Testkatalog (Skript `Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py`).
- **Gate auf dem Merge-Stand:** Kern-Filter 0 Fehler; voller Testlauf grün; Werkzeugtests der
  Auslieferungsvorlage 26/26; Windows-Schale 0 Fehler; ChartProben 122 Bilder ohne Verstoß;
  SqlDialektPruefer 0 Fundstellen; Referenzlauf der fünf CI-Projekte gegen
  `2026-09-22_R11_Bestandsbefunde` bestanden und byte-gleich.
- **Gegenprüfungen:** drei Prüfgruppen mit 32 Befunden (2 hoch, 10 mittel, 20 gering) und eine
  Papierprüfung mit 16 Befunden; alle wesentlichen behoben.
- **Lehre für die nächste Stufe:** Vor der Vergabe einer Schemanummer `git fetch origin` und
  `SchemaStand.Zielversion` auf `origin/ios_migration_september` **und** auf allen lokalen Zweigen
  und Worktrees messen; die Nummer erst im Merge festschreiben. Dreimal wurde am 23.09. dieselbe
  Nummer 101 vergeben (Gebäudesimulation, Wirtschaftlichkeit, Zapfprofilgenerator).
- **Entschieden am 23.09.2026 (Nachträge N5 und N6 des Umsetzungskonzepts):** Lizenz vorab zu
  Testzwecken freigegeben (K8 und ZU15 laufen weiter); K1-Unterlagen liegen in der Ablage des
  Anwenders (DIN 4708-2/-3, A100-Entwurf, weiter Entwurfsstand) und werden in Z2 als lokale
  Testdaten erfasst; ZU16 ersetzen; ZU17 und ZU18 nach Empfehlung (Inhaltsvergleich im
  Projektimport in Z1; Testklassen-Umstellung als Folgeposten). Dazu die Hilfeausgabe der
  Auslieferungsvorlage um `--katalogpaket` ergänzen (Protokoll, offene Punkte).
- **Nächster Auftrag:** Stufe Z1 (Bilanz deterministisch mit Weiche) nach Kapitel 7 des
  Umsetzungskonzepts; Voraussetzungen wie in Anhang A, Schemanummer für T2 erst in Z3.

## 8 Nachtrag 23.09.2026 — Stufe Z1 umgesetzt

Nach Abschnitt 7 wurde die Stufe Z1 noch am selben Tag auf dem Zweig `z1` ausgeführt (von
`7062b849`, 40 Commits bis `1f68bac7`); der Übertrag setzt nach Sichtabnahme und Merge bei Z2 an.

- **Ergebnis Gruppe 1 (Rechenweg):** S1 Mengengerüst mit Temperaturumrechnung und
  Messwertgrenzen, S2 Zapfkalender, Kaltwassergang und Formvektor, S5 Zirkulationskanal,
  `Bilanzreihe`, Fassade `ZapfprofilRechner` und Herkunftsprotokoll im Kern, ohne Datenbank und
  Dienste; unabhängiger Referenzfall über 8760 h als Python-Skript mit Abweichung 0; Nachtrag N7.
- **Ergebnis Gruppe 2 (Weiche, Schreibweg, Projekttransfer):** Weiche in `SimulationWaermebedarf`
  exklusiv für Weg `GENERATOR` mit getrennten Monatssummen; Vorschau mit Arbeitsstand;
  `ZapfprofilCtrl` Eingang, Rechnen und Speichern in einem `DbVorgang`; Inhaltsvergleich im
  Projektimport (ZU17); Testkatalog um die fünfzehn Parameter des Rechenwegs in der Testdatenbank
  (LFS); `ZapfprofilWeicheTests`; Nachtrag N8.
- **Ergebnis Gruppe 3 (Oberfläche):** Vorschaubilder über den Kern-Renderer (ChartProben mit elf
  neuen Bildern), DTO und Textbündel, Hülle `ZapfprofilHuelle` mit Naht `Zapfprofilwege`,
  Ressourcen beider Sprachen, `ZapfprofilDialog.razor` Stufe Einfach mit Vorschau-Reitern,
  Einbindung in den Bedarfsprofil-Dialog (Knopf, Optionsgruppe, Leiste mit Arbeitsstand, Schreiben
  im OK), Hüllen der Windows-Schale, bunit-Tests, Wiki-Entwurf „Programm Dokumentation -
  Brauchwasser-Zapfprofil"; Nachtrag N9.
- **Statuszeile und Protokoll:** #443, Protokoll
  [`2026-09-23_Z1_Bilanz_deterministisch.md`](../../ueberholt/Protokolle/Zapfprofilgenerator/2026-09-23_Z1_Bilanz_deterministisch.md).
- **Gate im Worktree:** Kern-Filter 0 Fehler; voller Testlauf mindestens 11 128 grün, 0 rot,
  1 übersprungen; ChartProben 135 Bilder ohne Verstoß; Windows-Schale 0 Fehler; Referenzlauf der
  fünf CI-Projekte gegen `2026-09-22_R11_Bestandsbefunde` byte-gleich (nach Gruppe 2).
- **Gegenprüfungen:** je Gruppe eine, zusammen 38 Befunde (1 hoch zu den Betreffzeilen der
  Commits, 11 mittel, 26 gering); alle wesentlichen behoben.
- **Beim Anwender:** Sichtabnahme unter Windows mit der Prüfliste — Startseite → Kachel
  Brauchwasser → Knopf „Zapfprofil erzeugen…"; Optionsgruppe „Rechenweg Brauchwasser" hin und
  zurück; Überlagerung mit den Reitern Tagesgang, Wochenprofil, Jahresgang und Kennzahlen; OK des
  Bedarfsprofil-Dialogs; Ergebnisdialog mit gestapelten Brauchwassersäulen (Zapfung und
  Zirkulation). Dazu der Wiki-Upload (gebündelt mit den übrigen Seiten), die Versionsnummer für
  den Logbuch-Satz aus 5.8 des Umsetzungskonzepts und die Frage aus N8 (e) 1, ob die Provenienz
  im Inhaltsvergleich mitzählt (vor Z2).
- **Merge und Push:** Merge `c220cbd1` von `origin` (`ac6e65ee`) nach `z1` (Konflikte in
  Statusdatei, `Dokumentation/LIESMICH.md` und beiden Resource-`.resx` inhaltlich
  zusammengeführt); Testdatenbank von `origin` (Schemastand 105) mit erneut eingespieltem
  `Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py` (`7544bb9d`), Nachtrag in
  `Referenzlaeufe/LIESMICH.md`; Push `4971556a` auf `ios_migration_september`.
- **Gate auf dem Merge-Stand:** Kern-Build 0 Fehler; 11 252 Tests grün, 1 übersprungen;
  Werkzeugtests Auslieferungsvorlage 26/26; Windows-Schale mit beiden Baubefehlen 0 Fehler;
  ChartProben 135 Bilder ohne Verstoß; `SqlDialektPruefer` 1 672 Texte ohne Fund; Referenzlauf der
  fünf CI-Projekte gegen `2026-09-23_R12_Gebaeudemodell` byte-gleich.
- **Nachzug Veraltet-Markierung** (`db22001b`): `ZapfprofilCtrl.Speichern` setzt im selben
  `DbVorgang` `Tab_Projekt.Aenderungsdatum` (`MerkmalUebernahmeCtrl.MarkiereProjektGeaendert`,
  Mechanismus aus #441), auch beim bloßen Umstellen der Weiche — ein gespeichertes Zapfprofil
  markiert ein vorhandenes Simulationsergebnis als veraltet; Fall in `ZapfprofilSpeichernTests`.
- **Hinweis:** Budgetgrenze des Anwenders 98 % — die Folgesitzung setzt mit diesem Abschnitt an
  (Sichtabnahme, dann Z2).
- **Regel aus dieser Stufe:** Laufende Workflow-Agenten nie anschreiben — Steuerung nur per
  Auftrag oder Datei. Hintergrund ist der Zwischenfall im Protokoll: Eine Zweitinstanz eines
  Workflow-Agenten überschrieb kurz eine Datei.
- **Vorbedingung Z2 erfasst (23.09.2026, N5/N6):** DIN 4708-2 und -3 sowie A100 und A1 liegen als
  lokale Testdaten unter `Referenzlaeufe/Normzahlen/din4708/` (Belegungszahl, Zapfstellenwertigkeit,
  Kennwerte, Formeln) und `Referenzlaeufe/Normzahlen/din12831a100/` (Bedarfstage, Kennwerte NA.4,
  Profilkennwerte, Summenlinien-Regeln), je mit `QUELLE.txt`; gitignoriert, als Zip an den
  Anwender übergeben (`Normzahlen_z2_din4708_a100_2026-09-23.zip`, in die Repowurzel entpacken).
  **Lücken:** (a) Die Kennwerte der Zapfperiode (u1, u2, K(u), W_z(N)) stehen nur in **DIN 4708-1**,
  die in der Ablage fehlt — nachbeschaffen (K1); ersatzweise sind z_B, W_zB(N) und z_x(N) aus
  Teil 3 erfasst. (b) Die A100-Minutenprofile liegen nur auf der Daten-CD des Entwurfs; erfasst
  sind abgelesene Summenlinien in 5-Minuten-Schritten, die vier DIN-4708-Profile als Zapfblöcke.
  (c) Beide DIN-4708-PDFs haben keine Textlage (alles abgelesen); A100 zeigt Widersprüche, die in
  `QUELLE.txt` benannt sind. A100 und A1 bleiben Entwürfe ohne Weißdruck-Hinweis.
- **Nächster Auftrag:** Stufe Z2 (Auslegung deterministisch) nach Kapitel 7 des
  Umsetzungskonzepts; Vorbedingungen erfüllt bis auf DIN 4708-1 (siehe Lücke a). Das Katalog-
  Einspielen der lokalen Testdaten in `Tab_TwwBedarfstag_STAMM` und `Tab_TwwDin4708Wert_STAMM`
  geschieht in Z2 über ein Skript nach dem Muster von `tww_testkatalog_fiktiv.py`, lokal und nie
  in der Repo-Testdatenbank (Kapitel 6).

## 9 Nachtrag 23.09.2026 — Stufe Z2 umgesetzt

Nach Abschnitt 8 wurde die Stufe Z2 am selben Tag auf dem Zweig `z2` ausgeführt (von `13fff671`,
29 Commits bis `89e46543`) und mit dem Stand von `ios_migration_september` zusammengeführt; der
Übertrag setzt nach Push und Sichtabnahme bei Z3 an.

- **Ergebnis Gruppe 1 (Rechenweg der Auslegung):** `Bedarfstag` mit Vorgaberegel (Konstruktor,
  Referenztag, Normtag), `Wochenreihe`, `Summenlinie` mit Nachweis, Wertepaarkurve, Zeitkonstante
  nach A1 und Schnellpfad, `Din4708Kennzahl` mit Wohnungstabelle, `TwwSpeicherauslegung` nach
  Vorlage V4 mit Verfahrensvergleich, Band und Warnliste, `Grossanlage`, `Auslegungsergebnis` je
  Topologiegruppe mit genau einer Empfehlung, Fassade `ZapfprofilAuslegung` — im Kern, ohne
  Datenbank und Dienste; `ZapfprofilTrennungWacheTests`; unabhängiger Referenzfall als
  Python-Skript mit Fassadenfall, Abweichung 0 auf 1e‑9; Testkatalogskript um Bedarfstage,
  DIN-4708-Werte und die Parameter der Auslegung; Nachtrag N10.
- **Ergebnis Gruppe 2 (Oberfläche der Auslegung):** Datenseite im `ZapfprofilCtrl`,
  `ChartRenderer.SummenlinieModell` mit den Bildern der Auslegung (ChartProben: zehn neue Bilder),
  DTO und Hülle `ZapfprofilHuelle.Auslegung`, 234 neue Schlüssel je Sprache,
  `ZapfprofilAuslegungDialog.razor` samt `BedarfstagKonstruktor.razor`, benannt gesperrte Elemente
  mit Grund, `HuellenTextschluesselWacheTests`, Wiki-Abschnitt „Auslegung" (Repo-Quelle);
  Nachtrag N11.
- **Statuszeile und Protokoll:** #451, Protokoll
  [`2026-09-23_Z2_Auslegung_deterministisch.md`](../../ueberholt/Protokolle/Zapfprofilgenerator/2026-09-23_Z2_Auslegung_deterministisch.md).
- **Gate im Worktree (vor dem Merge):** Kern-Filter 0 Fehler; voller Testlauf 11 455 grün, 0 rot,
  1 übersprungen; ChartProben 145 Bilder ohne Verstoß; Windows-Schale 0 Fehler.
- **Gegenprüfungen:** je Gruppe eine, zusammen 21 Befunde (Gruppe 1: 13 mit 0 hoch, 7 mittel,
  6 gering; Gruppe 2: 8 mit 1 hoch, 3 mittel, 4 gering); der hohe Befund — ein übernommener Punkt
  steuerte über die Großanlagenerkennung den neuen Punkt — ist behoben, alle übrigen behoben oder
  als Folge in N10/N11 benannt.
- **Merge und Nachzug:** Merge `04bf2ae0` von `origin` (`4b2ae6ce`; Konflikte nur in beiden
  Resource-`.resx`, beide Seiten behalten, `Resource.Designer.cs` geprüft) und Merge `169716e8`
  (`39c63361`, #449, ohne Konflikt); Testdatenbank `a228185d` mit
  `Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py` nachgezogen (63 Zeilen angelegt, 2 nachgeführt,
  zweiter Lauf 0/0, Schemastand 113 unverändert, LFS); `TwwKatalogWacheTests` wieder streng
  (`8cc16a62`), Nachtrag in `Referenzlaeufe/LIESMICH.md` (`7bde6800`), zwei Tests auf den
  Katalogstand (`cb347e02`). Nicht gepusht.
- **Gate auf dem Merge-Stand:** Kern-Build 0 Fehler; 11 750 Tests grün, 1 übersprungen (nach der Anpassung zweier Tests an den
  Katalogstand); Werkzeugtests Auslieferungsvorlage 26/26; Windows-Schale mit beiden Baubefehlen
  0 Fehler; ChartProben 145 Bilder ohne Verstoß; `SqlDialektPruefer` 1 726 Texte ohne Fund;
  Referenzlauf der fünf CI-Projekte gegen `2026-09-23_R12_Gebaeudemodell` PASS, byte-gleich.
- **Beim Anwender:** Push von `z2` nach `ios_migration_september` (schneller Vorlauf, solange
  `origin` nicht weiterrückt). Sichtabnahme unter Windows mit der Prüfliste — Überlagerung
  „Auslegung": Zapfprofil → Knopf „Auslegung…"; Wahl des Bedarfstags mit Vorgaberegel, gesperrte
  Quellen (A100-Referenzprofil, Ecodesign, Normtag) nennen ihren Grund; Karten (a) Summenlinie mit
  Wertepaarkurve, (b) Perzentil gesperrt, (c) Normvergleich mit DIN-4708-Kennzahl und gesperrter
  Zeile DIN 1988-300; Verfahrensvergleich mit Band, Füllstand und Warnliste; Banner „Spitzen
  unterschätzt" bei einem Tag aus dem Stundenprofil; OK übernimmt den Punkt, eine Zonenänderung
  danach meldet ihn überholt und das Speichern verwirft ihn. Konstruktor: öffnet sich ohne
  konstruierten Tag von selbst; „Bedarfstag konstruieren…" mit Zeilen hinzufügen und entfernen,
  Name (ein belegter wird mit einem freien Vorschlag abgelehnt), eine Fehleingabe hält das OK an
  und wird benannt, erneutes Öffnen beginnt mit den Zeilen des Entwurfs. Dazu englische Oberfläche
  gegenlesen, Wiki-Upload (gebündelt), Versionsnummer für den Logbuch-Satz (Statuszeile #451) und
  DIN 4708-1 nachbeschaffen (K1, Lücke (a) aus Abschnitt 8).
- **Folgeposten:** zehn Auslegungsbilder und elf Zapfprofilbilder aus Z1 in die Linux-Messlatte der
  ChartProben; ΣV̇_A der Entnahmearmaturen ins Datenmodell für DIN 1988-300 (Schemaschritt);
  Erzeugerart und Werkstoff speichern oder ableiten (Z4); Auslieferungswerte (Speichertemperatur-
  Vorgabe, GLF-Gültigkeitsgrenze, Übertragerpaare, Nenninhaltsliste) ins Katalogpaket.
- **Nächster Auftrag:** Stufe Z3 (Stochastik) nach Kapitel 7 des Umsetzungskonzepts — T2,
  `ZapfZufall` samt Plattformtest, Generator mit gestutztem Mittel, Ensembles der Jahresreihe und
  des Bedarfstags über `Kulturweitergabe`, Perzentil je Topologie, Gleichzeitigkeit als Ergebnis,
  Entkopplung der Urlaube, Rechenweg der Jahresreihe „stochastisch"; dazu die Z3-Folgen aus N10 (d)
  und N11 (e), (f). Vorbedingungen: Z2 gepusht und abgenommen, ZU8. **Schemaschritt T2:** die
  Nummer erst bei der Vergabe messen — `git fetch origin`, dann `SchemaStand.Zielversion` auf
  `origin/ios_migration_september` **und** auf allen lokalen Zweigen und Worktrees; heute steht
  sie überall auf 113, der nächste freie Schritt ist 114; die Nummer im Merge festschreiben.

## 10 Nachtrag 24.09.2026 — Stufe Z3 umgesetzt

Nach Abschnitt 9 wurde die Stufe Z3 in der Nacht vom 23. auf den 24.09.2026 auf dem Zweig `z3`
ausgeführt (von `ffc27d18`, 49 eigene Commits bis `ea6f8608`, gepusht am 24.09.2026, Kern-Lauf 35941477369 grün, Merges von `origin` `7a32b6f3`,
`aab9896e` und `a1df2dbe`) und mit dem Stand von `ios_migration_september` zusammengeführt; der
Übertrag setzt nach Push und Sichtabnahme bei Z4 an.

- **Ergebnis Gruppe 1 (Kern):** `ZapfZufall` (SplitMix64, xoshiro256** mit veröffentlichtem
  Prüfvektor, Lemire, Σ12u−6, von Neumann, Poisson; keine transzendente Funktion, bitgleich),
  `Zapfkategorie`/`Zapfkategoriensatz`/`ZapfStochastikParameter`, `Zapfereignisgenerator` (Poisson je
  Kategorie und Tag, exaktes Irwin-Hall-Mittel, Kappung), `Zapfensemble` (Bedarfstag: Perzentile je
  Topologie, GLF_V/GLF_P, √N, Kennzahlen statt Tage, `Volumenauftrag`), `Jahresensemble` (Bilanz =
  Jahr zum Seed × E_det/E_0, R Jahre für die Konsistenzprobe), `Minutenstatistik`, Obergrenzen;
  unabhängige Referenzfälle für Zufall, Auslegungs- und Jahresensemble; DHWcalc-Vergleich gegen
  OpenDHW (MIT, gz, Attribution) in Verteilungsgrößen.
- **Ergebnis Gruppe 2 (Katalog und Schema):** Schemaschritt **115** `Tab_TwwZapfkategorie_STAMM`
  (16 Spalten, zunächst als 114 aufgesetzt, nach der Kollision mit KU2 W1 umnummeriert); Kategorien als
  Datenblock der Nutzungsart in Transfer, Sperre, Kopierstellen und Auslieferungsvorlage;
  `ZapfprofilCtrl.Eingang` liest Kategorien und Parameter; **ZU19**: Ableitungsskript
  `Referenzlaeufe/Skripte/normzahlen_abgeleitet_bauen.py` (nur mit den lokalen Originalen lauffähig)
  → `tww_katalogwerte_abgeleitet.json` (497 Werte, reproduzierbar), vier abgeleitete Nutzungsarten
  (`FIKTIV`, Quelle „VDI 6002 Blatt n (abgeleitet)"); **freier Paketteil**
  `Referenzlaeufe/Katalogpaket_frei/` (Ecodesign-Profil L, 5 Parameter, 4 Kategorien; `FREI`,
  AUSLIEFERUNG, ReadOnly 1), von der Vorlage immer eingespielt; Wachen Testdatenbank = JSON/Paketteil
  und gegen die lokalen Originale; Testdatenbank neu (LFS `fbc30835…`, 82 Zeilen); Nachtrag N12.
- **Ergebnis Gruppe 3 (Oberfläche):** Zapfprofil-Dialog mit Stufe Experte, Gruppe „Stochastik ·
  Jahresreihe" (Rechenweg, Seed, Realisierungen), Konsistenzprobe im Reiter Kennzahlen; Auslegung mit
  „Stochastisch rechnen", P95/P99, Realisierungen, Karte (b) mit Perzentilwert, Streuband, GLF,
  „nicht belastbar", Konsistenzhinweis, Spitze je Einheit der Wohnungsstation; Vorschau bleibt
  deterministisch, Ensembles rechnen nebenläufig mit Status und Abbruch; Kernschranke der Einheitentage je Projekt, Abbruch bis in den Kern; DTO, Hülle,
  70 neue und 10 geänderte Ressourcenschlüssel je Sprache.
- **Statuszeile und Protokoll:** #453, Protokoll
  [`2026-09-24_Z3_Stochastik.md`](../../ueberholt/Protokolle/Zapfprofilgenerator/2026-09-24_Z3_Stochastik.md).
- **Gate im Worktree (nach dem Merge):** Kern-Filter 0 Fehler; voller Testlauf 12 287 grün, 0 rot, 1 übersprungen; ChartProben 161 Bilder ohne Verstoß; SQL-Dialekt-Prüfer 1 759 Texte ohne Fund; Auslieferungsvorlage 30/30; Windows-Schale 0 Fehler; Referenzlauf der fünf CI-Projekte gegen R13 PASS, byte-gleich.
- **Gegenprüfungen:** je Gruppe eine, zusammen 27 Befunde (Gruppe 1: 9 mit 1 hoch; Gruppe 2: 9 mit
  1 hoch; Gruppe 3: 9 mit 1 hoch); die hohen Befunde — Bilanz als Ensemblemittel, rückrechenbare
  Ableitung, stochastische Vorschau im Renderfaden — sind behoben bzw. vom Anwender entschieden
  (Rückrechenbarkeit zugelassen); alle mittleren Befunde behoben, geringe im Nachtrag N12.
- **Anwenderentscheide dieser Stufe:** ZU19 (abgeleitete VDI-Werte im Repositorium; Rückrechenbarkeit
  zugelassen) — umgesetzt; offen: **ZU20** (Auslieferung der abgeleiteten Werte, nach K8), **ZU21**
  (Setzungen des Paketteils bestätigen: Urlaubsversatz 14 d, Vielfaches 2, Konsistenzschwelle 1,5,
  Quantile, Streuungen nach DHWcalc-Protokoll, keine Kappung), K8/ZU15 (Lizenz), Versionsnummer für
  die drei Logbuch-Sätze.
- **Sichtabnahme unter Windows (Bedarfsprofil Brauchwasser → „Zapfprofil erzeugen…"):**
  1. Stufe „Experte": Gruppe „Stochastik · Jahresreihe" erscheint; „Erweitert" meldet „noch nicht
     verfügbar"; Seed (1) steht auch bei „deterministisch", Realisierungen (10) nur bei „stochastisch".
  2. Rechenweg „stochastisch": Vorschau ohne Hänger, leise Zeile darunter; Zähler „+1 überschrieben";
     Leiste „monatlicher Verlauf" im Bedarfsprofil zeigt sofort an.
  3. Realisierungen „5000" eingeben: Feld rot, OK meldet „Bitte die markierten Felder berichtigen".
  4. „Stochastisch rechnen" in der Fußleiste: Auslegung öffnet sofort mit gesetztem Schalter, Karte (b)
     sagt „rechnet …", Fortschritt mit Abbrechen, Oberfläche bleibt bedienbar; danach Perzentilwert,
     Tabelle P50 … P99 mit Spannweite, GLF_V mit Σ n_E, Konsistenzsatz mit Werten, kein „(K3)".
  5. Zurück im Zapfprofil: Fortschritt „Jahresreihe rechnet …", danach im Reiter Kennzahlen je Zone die
     Konsistenzprobe (±%, ✓) und Status „Stochastik gerechnet · Seed · Jahre".
  6. Abbrechen am Fortschritt: Lauf endet zügig, leise Zeile nennt den Abbruch; eine Eingabe während
     des Laufs verwirft ihn.
  7. Auslegung: Realisierungen „5" → Marke „nicht belastbar" mit Tooltip; P95 wählen → Vorgabe 40;
     Abbrechen schaltet „Stochastisch rechnen" aus; OK während des Laufs übernimmt den Punkt sofort.
  8. „Auslegung…" nach einem OK mit Schalter: Schalter aus, kein sofortiger Lauf; Haken aus → Karte (b)
     wieder „noch nicht gerechnet".
  9. Zone mit Nutzungsart ohne Zapfkategorien (Testnutzung): Banner mit Nutzungsart an der Zone; Zone
     mit „Wohnen groß" rechnet.
  10. Wohnungsstation: Zeile „Spitze je Einheit" in Karte (b); maßgebender Tag als Datum; bei P_p = ∞
     „entfällt" mit Standtext.
  11. OK, OK im Bedarfsprofil: Ergebnis gilt als veraltet; nach Wiederöffnen stehen Rechenweg und Seed.
  12. Englisch dieselben Stellen; bei R = 1000 und großer Zone erscheint der benannte Grund der
     Einheitentage statt eines Hängers.
- **Koordination:** Schrittnummer 115 und Statusnummer #453 mit den Sitzungen Wirtschaftlichkeit
  (E9 ab 116, #454/#455) und Dialog Design (#456–#459; KI-Maskenanmeldung der drei Zapfprofil-Dialoge
  nach dem Z3-Merge in #458) abgestimmt; das Kühlungskonto vergab 114 ohne Abstimmung — künftig wird
  die Nummer unmittelbar vor dem Merge erneut gemessen.
- **Nächster Auftrag:** Stufe Z4 nach Kapitel 7 — Stufe Erweitert der Dialoge, Kategorien als
  Katalogkopie (Status EIGEN) im Experten-Modus, Erzeugerart und Werkstoff, DIN 1988-300; Z4b:
  VDI 4655 (T3 `Tab_TwwTyptag_IMPORT`) unter der Regel ZU19; Z5: Nichtwohn-Kategorien nach
  OpenDHW-Muster, Kategorien als Katalogpflege. **Schemaschritt T3:** Nummer erst bei der Vergabe
  messen (`git fetch origin`, `SchemaStand.Zielversion` auf `origin` und allen lokalen Zweigen); heute
  ist 115 die höchste, Wirtschaftlichkeit nimmt ab 116.

## 11 Nachtrag 24.09.2026 — Stufe Z4 umgesetzt

Nach Abschnitt 10 wurde die Stufe Z4 am 24.09.2026 auf dem Zweig `z4` ausgeführt (von `b7572d42`,
48 eigene Commits bis `8cddb04a`, Papiere `101164f8`, gepusht am 24.09.2026, Kern-Lauf 36026628108 rot: sieben Fälle der Katalogimport-Tests nur auf Linux, weil der Testhelfer Zeilenenden mit festem CRLF mutierte; behoben mit `85da8fa7` (Testhelfer normalisiert auf CRLF); Merges von `origin` `48d8836d`, `4a9d7449` und
`3ff9840b`) und mit dem Stand von `ios_migration_september` zusammengeführt; der Übertrag setzt
nach Push und Sichtabnahme bei Z4b bzw. Z5 an. Der Anwender bestellte Z4 ohne Sichtabnahme der Z3
(„Fahre fort"); ab 16:00 liefen Nachbesserungen und Abschluss mit Sonnet, weil das
Opus-Wochenkontingent erschöpft war (Rücksetzung 29.09.).

- **Ergebnis Gruppe 1 (Kern und Controller):** Schemaschritt **124** (`Tab_TwwProjekt`:
  Erzeugerart, Übertrager-Werkstoff, Personen auto/manuell, Füllstand-Bezug; `Tab_TwwBedarfstag_STAMM`:
  Bezugsart), Schreibwege, Transfer, Paketteil; Kategorien als Katalogkopie im Controller; Warnlogik
  (Warnung/Hinweis, Zirkulation als Hinweis mit Katalogverhältnis), Schätzhilfen; Dauerlinie und
  Auslastungsgang mit neuem Bild; Anzeigetemperatur und Stundenschwelle als Parameter; `ZapfSatz`
  (Kennung und Werte, 385 Muster, Wache); Nachtrag N13.
- **Ergebnis Gruppe 2a (Zapfprofil-Dialog):** Stufen Erweitert und Experte, Zonenliste mit Summenfuß,
  Eingabeblöcke nach 5.3 (Wohnungstabelle, Kalender und Ferien, Jahresmesswert, Schätzhilfen,
  Fachwerte), fünf Reiter mit Dauerlinie, Warnliste, KiSicht und Hilfeschlüssel.
- **Ergebnis Gruppe 2b (Editoren, Auslegung, Konstruktor):** Tagesgang-Editor (Herkunft unveränderter
  Reihen bleibt erhalten), Kategorien-Editor, Verfahrensvergleich-Felder und Erzeugerart/Werkstoff in der
  Auslegung, Konstruktor mit Bezugsart, Hilfe und Wiki.
- **Ergebnis Gruppe 3 (Katalogdialog):** Untermenü Brauchwasser, `TwwNutzungsartAdminDialog` mit
  Editor, Katalogimport (Paketformat N2), Wachen auf Katalogtexte, Rasterprobe (drei Verstöße behoben).
- **Statuszeile und Protokoll:** #464, Protokoll
  [`2026-09-24_Z4_Oberflaeche.md`](../../ueberholt/Protokolle/Zapfprofilgenerator/2026-09-24_Z4_Oberflaeche.md).
- **Gate im Worktree (nach dem Merge):** Kern-Filter 0 Fehler; voller Testlauf 12 893 grün, 0 rot, 1 übersprungen; ChartProben 165 Bilder ohne Verstoß; SQL-Dialekt-Prüfer 1 803 Texte ohne Fund; Auslieferungsvorlage 30/30; Windows-Schale 0 Fehler; Referenzlauf 13/13 gegen R14 PASS, byte-gleich; Rasterprobe 0 Verstöße (Gruppe 3).
- **Gegenprüfungen:** je Gruppe eine, zusammen 31 Befunde (Gruppe 1: 10, drei mittel; 2a: 10, drei
  mittel; 2b: 9, ein hoher; 3: 2, ein hoher); alle hohen und mittleren behoben, die geringen als Folgen
  im Nachtrag N13.
- **Anwenderentscheide dieser Stufe:** keine neuen; offen: Fachentscheid Ecodesign-Profil L nach
  Wohneinheiten skalieren, ZU20, ZU21 (um Zirkulations-Hinweisverhältnis 1,5, Anzeigetemperatur 45 °C,
  Stundenschwelle 0,1 kW erweitert), K8/ZU15, Versionsnummer für die Logbuch-Sätze (Z3 drei, Z4
  drei).
- **Sichtabnahme unter Windows** (Bedarfsprofil Brauchwasser → „Zapfprofil erzeugen…" und
  Administration → Brauchwasser → Nutzungsarten; zusätzlich zu den Listen der Abschnitte 8–10):
  1. Stufenwechsel Einfach → Erweitert → Experte und zurück: Eingaben bleiben, die Zeile „n Werte
     überschrieben" zählt ein auto/manuell-Paar einmal; die leise Zeile verweist auf die nächste Stufe.
  2. Zonenliste ab Erweitert mit Topologie, Anteil und Rechenweg; Zone hinzufügen und entfernen; der
     Summenfuß steht bündig.
  3. Wohnungstabelle nur bei Bezugsart Wohneinheiten/Personen; Zeilen anlegen und entfernen ohne
     Querrollen; die wirksame Menge steht unter der Bezugsgröße; bei anderer Bezugsart hält keine
     verdeckte Zeile das OK an.
  4. Kalender, vier Ferienzeiträume, Bundesland gesperrt mit Grund; Jahresmesswert: bei m³ verschwindet
     die Bilanzgrenze, der Speicherverlust nur bei der passenden Grenze.
  5. Schätzhilfen Tagesbedarf, Zirkulation, Ladeleistung: Vorschlag, „Übernehmen", „angesetzt"; ein
     manueller Wert ohne Zahl färbt sich bei OK rot.
  6. Experte: Fachwerte, zwölf Monatsfelder des Auslastungsgangs, Anzeigetemperatur und
     Stundenschwelle mit Vorgabe.
  7. Reiter Dauerlinie ab Erweitert mit P50/P90/P95/P99 und Schwelle; Warnliste mit Kennzeichen
     Warnung/Hinweis; Hilfeknöpfe öffnen den richtigen Wiki-Anker.
  8. „Tagesgang bearbeiten…": Stundenraster je Tagtyp, Summenzeile, Normieren, Tag kopieren/einfügen,
     Vorlage laden; OK ohne Änderung an einer Auslieferungs-Nutzungsart erzeugt keine Kopie; eine
     Änderung an einer gesperrten Nutzungsart erzeugt „…-E1"; Esc schließt erst den Editor.
  9. „Zapfkategorien und Streuung…": Raster ohne Querrollen, Schloss und Grund bei gesperrtem Satz,
     Hinweis bei Anteilsumme ≠ 1, Kappung nur > 0.
  10. Auslegung: Gruppe „Eingaben des Verfahrensvergleichs", Erzeugerart mit „Vorschlag wählen",
     Werte überdauern das Speichern; Konstruktor mit Bezugsart und Bezugsmenge, Zeilen kommen nach
     Schließen und Wiederöffnen zurück.
  11. Menü Administration → Brauchwasser klappt auf (zwei Punkte); Katalogdialog: Liste ohne
     Querrollen mit Schloss, Stammblatt mit Bildern, „Ändern…" an einer Auslieferungszeile öffnet als
     „Speichern unter", „Löschen" gesperrt mit Grund, „Import…" mit Bericht (angelegt, übersprungen,
     abgelehnt), „Grafik…".
  12. Englische Oberfläche: Menütext „Domestic hot water", alle neuen Texte vollständig.
- **Koordination:** Statusnummer #464 und die Schemanummern mit den Sitzungen Wirtschaftlichkeit und
  Dialog Design abgestimmt; zweimal umnummeriert (120 → 121 → 124); Regel seither: wer zuerst
  pusht, hat die Nummer, der andere rückt, niemand wartet.
- **Nächster Auftrag:** Z4b (VDI-4655-Import mit Typtagzuordnung, T3 `Tab_TwwTyptag_IMPORT`,
  `Normformvektorleser`, `Typtagzuordnung`, Importdialog; Testdaten erfunden, Auslieferungsvorlage
  leert die Tabelle; abgeleitete VDI-4655-Werte nur unter ZU19) und Z5 (Kalibrierung und Validierung,
  Nichtwohn-Kategorien, Katalogausbau, Konstruktorzeilen in der Datenbank, Folgen aus N13).
  **Schemaschritt:** Nummer erst beim Merge messen; heute ist 124 die höchste.

## 12 Nachtrag 24.09.2026 — Stufe Z4b umgesetzt

Nach Abschnitt 11 wurde die Stufe Z4b am 24.09.2026 auf dem Zweig `z4b` ausgeführt (von `6d022f6d`,
34 eigene Commits bis `6989234d`, Merge von `origin` `315665f4`) und mit dem Stand von
`ios_migration_september` zusammengeführt; der Übertrag setzt nach Push und Sichtabnahme bei Z5 an.

- **Ergebnis Gruppe 1 (Kern):** Schemaschritt **131** `Tab_TwwTyptag_IMPORT` (elf Spalten mit
  `Art`, anwenderlokal, nie in Vorlage oder Transfer); `Normformvektorleser` (ZIP oder CSV-Dateien,
  Struktur- und Summenprüfung, 38 benannte Ablehnungen); `TwwTyptagCtrl` (Einspielen ersetzt, Löschen,
  Rollback); `Typtagzuordnung` (365 Tage, Jahreszeit und Bewölkung nach Kennwerten des Pakets,
  Feiertag als Sonntag, Energieerhaltung); Weiche `Zapfprofileingang.Typtage`; abgeleitete Testdaten
  `Referenzlaeufe/Skripte/vdi4655_abgeleitet.json` (ZU19) mit Wache gegen die lokalen Originale;
  Nachtrag N14 mit den Antworten auf die Vorfragen 4.2.
- **Ergebnis Gruppe 2 (Dialog und Projektwahl):** Projektwahl (Typtagweg, Klimazone, Gebäudeart) im
  selben Schritt mit Schreibwegen und Transfer; Importdialog `TwwTyptagImportDialog` (Stand, Prüfbericht,
  Einspielen, Löschen) aus Katalogdialog und Zapfprofil-Experte; Wahl „Typtage nach VDI 4655" im
  Zapfprofil-Dialog; Wiki-Abschnitt und Hilfeanker; KI-Feldkarten.
- **ZU23:** Das Grundlagenpapier `Grundlagen_5_VDI-4655_Auswertung.md` trägt nur noch abgeleitete
  Zahlen (795 ersetzt, Nachweis 0 Originaltreffer); Regel in `Referenzlaeufe/LIESMICH.md` und N14.
- **Statuszeile und Protokoll:** #486, Protokoll
  [`2026-09-24_Z4b_Typtage.md`](../../ueberholt/Protokolle/Zapfprofilgenerator/2026-09-24_Z4b_Typtage.md).
- **Gate im Worktree (nach dem Merge):** Kern-Filter 0 Fehler; voller Testlauf 13 257 grün, 0 rot, 1 übersprungen; ChartProben 165 Bilder ohne Verstoß; SQL-Dialekt-Prüfer 1 836 Texte ohne Fund; Auslieferungsvorlage 31/31; Windows-Schale 0 Fehler; Referenzlauf 5/5 gegen R14 PASS.
- **Gegenprüfungen:** je Gruppe eine (20 Befunde, ein hoher — behoben); geringe Befunde als
  Folgen in N14.
- **Anwenderentscheide dieser Stufe:** ZU23 (umgesetzt); offen: **ZU22** (Auslieferung der abgeleiteten
  VDI-4655-Werte; VDI 4655 untersagt innerbetriebliche Kopien — mit K3a/K8), ZU20, ZU21, Versionsnummer
  für die Logbuch-Sätze (Z3 drei, Z4 drei, Z4b einen).
- **Sichtabnahme unter Windows** (Administration → Brauchwasser → Nutzungsarten → „VDI-4655-Typtage…";
  Zapfprofil → Experte):
  1. Katalog „Brauchwasser-Nutzungsarten" → Fußleiste „VDI-4655-Typtage…" öffnet die Überlagerung mit
     Lizenz- und Formathinweis und „Es sind keine Typtage eingespielt".
  2. „Paket wählen…" öffnet den Dateiwähler im gemerkten Ordner und prüft sofort; ein unvollständiges Paket
     nennt Datei und Zeile, „Einspielen" bleibt gesperrt.
  3. „Einspielen" zeigt danach die Standzeilen samt Hinweisen; eine zweite Runde fragt „ersetzt vollständig";
     „Löschen" fragt zurück, danach Leersatz; der Katalogdialog meldet den neuen Stand.
  4. Zapfprofil → Experte → Fachwerte: Schalter „Typtage nach VDI 4655" grau mit Grund ohne Daten, nach dem
     Einspielen bedienbar, Klimazone und Gebäudeart vorbelegt bei nur einer Wahl.
  5. Vorschau rechnet nach dem Einschalten weiter (Jahresenergie gleich, Monatsverteilung anders) und zeigt
     die Typtag-Hinweise in der Warnliste; Rechenweg „stochastisch" rechnet über die Typtagmengen.
  6. OK → erneut öffnen: die Wahl steht noch; ein Projekt ohne Wahl bleibt unverändert.
  7. Infoknöpfe der Gruppe und des Dialogs springen auf den Wiki-Anker „typtage".
  8. Englische Oberfläche: alle Texte des Importdialogs und der Gruppe vollständig.
- **Nächster Auftrag:** Stufe Z5 nach Kapitel 7 — Messdatenimport, Vergleichsbericht, Validierung gegen
  freie Messreihen und freigegebene INEKON-Projekte (Messspitze im P85–P95-Band der Dauerlinie,
  √N-Skalierung, Formabgleich), Kalibrierung der Nichtwohn-Parameter, Katalogausbau auf 25 bis 27
  Typen, Nichtwohn-Kategorien; dazu die Folgen aus N13 und N14 (Konstruktorzeilen in der Datenbank,
  Herkunftsprotokoll als Sätze, Referenzfall der Wetterkopplung mit TRY-Import). **Schemaschritt:**
  Nummer erst beim Merge messen; heute ist 131 die höchste.

## 13 Nachtrag 25.09.2026 — Stufe Z5 umgesetzt

Nach Abschnitt 12 wurde die Stufe Z5 in der Nacht auf den 25.09.2026 auf dem Zweig `z5` ausgeführt
(von `d6020c05`, 37 eigene Commits bis `a87b783b`, Papiere `b28b133f`, Merge `a0bbc633` von `b5a2e389`, gepusht `a0bbc633` am 25.09.2026, Kern-Lauf 36099321791 grün; Merges von `origin` `45935351`, `ac8a1762`
und `6c5aa139`) und mit dem Stand von `ios_migration_september` zusammengeführt. Damit sind alle Stufen
Z0–Z5 des Umsetzungskonzepts umgesetzt; der Übertrag setzt bei den Folgen (ZU7, ZU24, K5) an.

- **Ergebnis Gruppe 1 (Kern):** Schemaschritt **140** `Tab_TwwMessreihe` (Projektdaten, Transfer
  und Kopie tragen sie, Vorlage leert); `Messreihenleser` (CSV, Auflösung, Lücken, Sommerzeit);
  `TwwMessreihenCtrl`; `Messvergleich` (Energie, P85–P95-Band der Dauerlinie mit Messspitze,
  Spitzenstreuung, √N-Skalierung, Formabgleich je Tagtyp, Monatsanteile — alles Verhältniszahlen);
  `Messkalibrierung` (Jahresmesswert exakt, Hochrechnung mit Jahresgang, Nichtwohn-Vorschlag nach
  kleinsten Quadraten); fünf Parameter im Paketteil; Wache „Schrittnummern lückenlos".
- **Ergebnis Gruppe 2 (Katalog):** Ein- und Zweifamilienhaus (ZU19), Nichtwohn-Vorgabesatz mit
  Steuerspalte `Gruppe`, `VorschlagUebernehmen` (Anwenderkopie aus der Messung); Ziel 25–27 Typen nicht
  aus VDI 6002 erreichbar (ZU24).
- **Ergebnis Gruppe 3 (Oberfläche):** Messdaten-Dialog, Vergleichsbericht im Reiter Kennzahlen (nebenläufig, nur Verhältnisse), Kalibrierknöpfe „Aus Messreihe kalibrieren" und „Vorschlag übernehmen…", 32 Validierungshinweise, Wiki-Abschnitte „Messdaten" und „Vergleich und Kalibrierung"; kein neues Bild.
- **Statuszeile und Protokoll:** #495, Protokoll
  [`2026-09-25_Z5_Kalibrierung.md`](../../ueberholt/Protokolle/Zapfprofilgenerator/2026-09-25_Z5_Kalibrierung.md).
- **Gate im Worktree (nach dem Merge):** Kern-Build 0 Fehler, 13 938 Tests grün, SqlDialektPruefer 1 920/0, ChartProben 174, Vorlage 34/34, Windows-Schale 0 Fehler, Referenzlauf 5/5 gegen R14.
- **Gegenprüfungen:** je Gruppe eine (34 Befunde, zwei hohe — behoben).
- **Anwenderentscheide dieser Stufe:** keine neuen; offen: **ZU24** (Katalogtypen: ZU19 auf A100
  ausdehnen oder externes Katalogpaket), **K5** (Freigabe von INEKON-Messreihen), **ZU7**
  (Referenzprojekt auf dem Generator mit vierter Einfrierregel und neuer Basis — mit den
  Nachbarsitzungen abzustimmen), ZU20–ZU22, K8/ZU15, Versionsnummer für die Logbuch-Sätze
  (Z3 drei, Z4 drei, Z4b einen, Z5 einen).
- **Sichtabnahme unter Windows** (Zapfprofil → Erweitert/Experte, Reiter Kennzahlen; „Messdaten…"):
  1. Zapfprofil → Stufe Erweitert → „Messdaten…": Überlagerung mit Liste und beiden Herleitungszeilen.
  2. „Datei wählen…" merkt den Ordner; der Prüfbericht erscheint sofort; Größe „aus der Kopfzeile" →
     Volumen ändert den Bericht; Zeitstempel Ortszeit: eine Jahresreihe mit Herbstumstellung wird mit
     Hinweis angenommen.
  3. Einspielen einer gleichnamigen Reihe fragt zurück, Löschen ebenso; Esc schließt nur die Überlagerung.
  4. Reiter Kennzahlen: Reihenwahl, „Vergleich rechnen" zeigt Fortschritt, Abbrechen wirkt; die
     Kennzahlen stehen als Verhältnisse, ohne Ensemble ein Strich mit Grund; Teiljahr, Schalttag, Lücken
     und Feiertage als Hinweise.
  5. Eine Eingabe setzt den Veraltet-Vermerk, die Tabelle bleibt stehen.
  6. „Aus Messreihe kalibrieren" füllt Wert, Einheit, Bilanzgrenze, Quelle und Zeitraum; Abbrechen des
     Dialogs verwirft sie; nach OK ist die Jahresenergie der Rechnung gleich dem Messwert.
  7. Nichtwohn-Zone: „Vorschlag übernehmen…" zeigt Vorschau und Rückfrage; danach rechnet die Zone mit
     der Kopie „…-E1", die Warnliste führt die Hinweise.
  8. Katalog „Brauchwasser-Nutzungsarten": Ein- und Zweifamilienhaus vorhanden; Kategorien einer
     Nichtwohn-Nutzungsart zeigen zwei Zeilen (Kurzzapfung, Duschzapfung).
  9. Englische Oberfläche: Messdaten-Dialog und Kennzahlen vollständig.
- **Nächste Aufträge:** (1) ZU7 — ein Referenzprojekt auf den Generator umstellen, vierte
  Einfrierregel, Basis neu einfrieren (Koordination mit allen Sitzungen, Referenzlauf 13 Projekte);
  (2) Folgen aus N13–N15 (Konstruktorzeilen in der Datenbank, Herkunftsprotokoll als Sätze,
  Bedeckungsgrad-Referenzfall mit TRY-Import, redundanter Index, Katalogimport-Reste, Katalogdialog auf
  iOS); (3) Validierungsbericht mit echten Messreihen nach K5; (4) Wiki-Upload aller Zapfprofil-
  Abschnitte. **Schemaschritt:** Nummer erst beim Merge messen; heute ist 140 die höchste.

## 14 Nachtrag 25.09.2026 — Folgeposten ZU20 und ZU24 umgesetzt

Nach Abschnitt 13 sind zwei Folgen der Anwenderentscheide vom 25.09.2026 (Nachtrag N16) auf dem
Zweig `zu` erledigt (von `fec09538`, Commits `c90990e2` und `8b822f98`, Papiere im Folgecommit,
Merge `e4b8d5b8` von `99815b47`; Statuszeile #504, Nachtrag **N17**, Protokoll
[`2026-09-25_Folgeposten_ZU20_ZU24.md`](../../ueberholt/Protokolle/Zapfprofilgenerator/2026-09-25_Folgeposten_ZU20_ZU24.md)).
**Kein Schemaschritt** — Schemastand bleibt 141.

- **ZU20:** Die fünf abgeleiteten VDI-6002-Nutzungsarten samt vier Tagesgangsätzen und sechzehn
  Tagesgängen gehören zur Auslieferung. Träger sind drei neue CSV-Dateien des freien Paketteils
  (`Tab_TwwTagesgangsatz_STAMM.csv`, `Tab_TwwTagesgang_STAMM.csv`, `Tab_TwwNutzungsart_STAMM.csv`),
  Status `AUSLIEFERUNG`, `ReadOnly` 1, Herkunftsart **`VERFAHREN`**, Quelle „abgeleitet aus VDI 6002
  Blatt n"; das Einspielskript erzeugt sie aus der abgeleiteten JSON-Datei und hält sie bei jedem
  Lauf dagegen.
- **ZU24:** `Referenzlaeufe/Katalogpaket_Vorlage_A100/` ist die Paketvorlage für die
  Nichtwohn-Typen des Beiblatts A100 — vier Dateien des Importformats, je eine Platzhalterzeile,
  Anleitung und Typnamenliste in ihrer `LIESMICH.md`, **keine Normzahl**. Die Werte trägt der
  Anwender außerhalb des Repositoriums ein.

**Sichtabnahme unter Windows** (zwei Handgriffe): Im Katalogdialog **Administration → Brauchwasser
→ Nutzungsarten** stehen die fünf Einträge „… (abgeleitet)" mit Stand *Auslieferung*, Herkunft
*Verfahren* und Quelle „abgeleitet aus VDI 6002 Blatt 1" bzw. „… Blatt 2"; sie sind gesperrt, und
*Bearbeiten* legt eine Kopie an. Über **Import…** denselben Dialogs lässt sich der Ordner
`Referenzlaeufe/Katalogpaket_Vorlage_A100` einspielen: Der Bericht nennt eine angelegte
Nutzungsart „Beispieltyp (Vorlage)" mit Stand *Import*, ohne Ablehnung und ohne Hinweis.

**Offen bleibt:** ZU21 (fachliche Durchsicht der Setzungen des freien Paketteils — die neuen Zeilen
liegen im selben Ordner), K5 (Messreihen), ZU7 (Referenzprojekt, vierte Einfrierregel, neue Basis),
der Wiki-Upload der Zapfprofil-Abschnitte und der Logbuch-Eintrag samt Versionsnummer.

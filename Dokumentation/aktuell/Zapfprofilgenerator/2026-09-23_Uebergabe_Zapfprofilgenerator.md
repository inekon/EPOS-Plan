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
- **Nächster Auftrag:** Stufe Z2 (Auslegung deterministisch) nach Kapitel 7 des
  Umsetzungskonzepts. Vorbedingungen: Z1 zusammengeführt; die K1-Unterlagen (A100-Bedarfstage,
  Tabellen der DIN 4708-2) als lokale Testdaten unter `Referenzlaeufe/Normzahlen/` erfasst (N6;
  Skript und `QUELLE.txt`, keine Werte in Papieren).

# Katalogimport nimmt den freien Paketteil an: Auftrag K1 (29.09.2026)

Protokoll des Auftrags **K1** der Sitzung „Zapfprofilgenerator Cloud". Umsetzungskonzept
Zapfprofilgenerator, Kapitel 6 (b), N2, Kapitel 9 ZU30 bis ZU33; Regel 2 der
[LIESMICH des Paketteils](../../../../Referenzlaeufe/Katalogpaket_frei/LIESMICH.md).

**Rahmen.** Worktree `agent-a9b1255f830f1b472`, Zweig `worktree-agent-a9b1255f830f1b472` von
`6a4b4de` (Arbeitszweig `ios_migration_september`, Schemastand 155, Referenzbasis
`2026-09-29_R26_Kesselrest`), Opus 5. Kein Schemaschritt, keine Änderung der Testdatenbank, kein
Basiswechsel, kein Push, kein CI-Lauf.

**Befund (Anwender, Bildschirmfoto aus der Windows-Anwendung).** Dialog *Brauchwasser-Nutzungsarten
→ Import… → Katalog importieren*, gewählt eine Datei aus `Referenzlaeufe/Katalogpaket_frei/`.
Meldung: „Das Paket ist abgelehnt — Tab_TwwBedarfstag_STAMM.csv: Die Spalte „Katalogversion" fehlt
— nichts importiert." Der freie Paketteil führt die Spalte **bewusst nicht**: Seine Zeilen treten
der Katalogversion des Katalogs bei, in den sie kommen. Folge: Eine bestehende Anwenderdatenbank
bekam die Zeilen des Paketteils — Ecodesign-Tage, Parameter der Stochastik und der
Speicherauslegung, abgeleitete Nutzungsarten — über den dafür gebauten Dialog nicht.

---

## 1 Die Regel „Katalogversion des Zielkatalogs" — eine Fassung, im Kern

Bis hierher stand die Regel allein im Werkzeug
(`Werkzeuge/Auslieferungsvorlage/TwwKataloge.PaketteilEinspielen`). Sie steht jetzt in
`EPOS.Kern/Controller/ZapfprofilCtrl.cs` neben dem Lesepfad, an den sie gebunden ist; Werkzeug und
Katalogimport rufen sie auf, eine zweite Fassung gibt es nicht.

| | |
|---|---|
| Regel | `ZapfprofilCtrl.Zielkatalogversion(DbVorgang = null)` |
| Wert | `AktuelleKatalogversion()` — die Katalogversion der **zuletzt angelegten Parameterzeile** (`Tab_TwwParameter_STAMM`, höchste `ID`) |
| Rückfall | `ZapfprofilCtrl.KATALOGVERSION_RUECKFALL` = `FREI-1` |

**Warum genau diese Version.** Der Parametersatz der Stochastik und der Speicherauslegung liest
ausschließlich `AktuelleKatalogversion()` (`ZapfprofilCtrl.Parameter()`, `ZapfprofilCtrl.Verfuegbar()`;
`Parameter(version)` filtert `WHERE Katalogversion = ?`). Träten eingespielte Parameter einer
anderen Version bei, stünden sie in der Tabelle, und kein Rechenweg sähe sie. Die Regel ist deshalb
an den Lesepfad gebunden, nicht an einen Namen.

**Grenzfälle, benannt behandelt.**

| Lage des Zielkatalogs | Zielkatalogversion |
|---|---|
| `Tab_TwwParameter_STAMM` fehlt (älterer Seed) | `FREI-1` |
| Tabelle vorhanden, keine Parameterzeile | `FREI-1` |
| jüngste Parameterzeile mit leerer Katalogversion | `FREI-1` |
| genau eine Katalogversion | diese |
| **mehrere** Katalogversionen | die der zuletzt angelegten Parameterzeile (höchste `ID`); ältere Versionen bleiben unberührt |

Die Textform der Version wird bewusst **nicht** geordnet („V10" gegen „V9" hätte keine sichere
Regel) — es gilt die Reihenfolge der `AUTOINCREMENT`-Schlüssel, dieselbe, die der Parametersatz
nimmt. `AktuelleKatalogversion` nimmt jetzt wahlfrei einen `DbVorgang`, damit ein Einspieler im
laufenden Schreibvorgang denselben Stand sieht; ohne ihn bleibt der Aufruf, wie er war.

## 2 Der Katalogimport

`TwwNutzungsartCtrl.PaketAus` bestimmt die Zielversion einmal je Paket, **vor** dem
Schreibvorgang, über die Kopfdateien, die das Paket mitbringt
(`IMPORT_TABELLEN_MIT_VERSION`: `Tab_TwwBedarfstag_STAMM`, `Tab_TwwParameter_STAMM`,
`Tab_TwwTagesgangsatz_STAMM`, `Tab_TwwNutzungsart_STAMM` — die vier Tabellen mit der Spalte;
Tagesgänge, Ereignisse und Kategorien hängen an ihrem Kopf und führen nie eine eigene).

| Das Paket führt `Katalogversion` … | Folge |
|---|---|
| in **allen** vorhandenen Kopfdateien | unverändert wie bisher: der Wert je Zeile |
| in **keiner** | alle Zeilen treten der Zielkatalogversion bei; Hinweis `KATALOGIMPORT_OHNE_KATALOGVERSION` nennt sie |
| nur in **einem Teil** | Ablehnung des Pakets als Ganzes, `KATALOGIMPORT_VERSION_GEMISCHT` mit beiden Dateilisten; nichts geschrieben |

Eine wegen fehlender Tabelle übergangene Datei zählt nicht mit — sie wird ohnehin nicht
eingespielt. Die Zielversion steht als `Paket.Zielversion`; `Paket.Version(tabelle, zeile)` ist die
eine Stelle, an der die Version einer Zeile entsteht. Die Pflichtspalte `Katalogversion` wird nur
verlangt, wenn das Paket sie führt (vier Stellen: Nutzungsart, Tagesgangsatz, Bedarfstag,
Parameter). **Alles Weitere bleibt, wie es war**: Dublettenscan, Überspringen gleicher Zeilen,
„(Import n)" bei abweichendem Inhalt, Ersetzen am Platz für Bedarfstag und Parameter, die
Auslieferungs- und Wirkungshinweise, der Prüflauf „Nur prüfen, nichts schreiben".

## 3 Weitere Formabweichungen — gesucht, keine gefunden

Gegen den Import geprüft wurden **alle sieben** Dateien des Paketteils, Kopfzeile für Kopfzeile
gegen die Pflichtlisten und die Spaltenkenntnis des Lesers, und danach am laufenden Import
(Abschnitt 4). Die fehlende Spalte `Katalogversion` in vier Dateien war die **einzige**
Abweichung. Im Einzelnen tragen schon heute:

- `Tab_TwwZapfkategorie_STAMM.csv` die Steuerspalte `Gruppe` — der Leser kennt sie
  (`TwwSchema.STEUERSPALTE_GRUPPE`), übernimmt sie nicht und bindet den Vorgabesatz der Gruppe
  nach der Kalenderart; beide Sätze (Wohnen 4, Nichtwohnen 2 Kategorien) werden gefunden;
- `Tab_TwwBedarfstag_STAMM.csv` die Spalte `Bezugsart` (Schemaschritt 124) — bekannt, an einer
  älteren Datenbank benannt übergangen;
- `Status` und `ReadOnly` überall — gelesen und übergangen (`IMPORT_UEBERGANGEN`);
- die Herkunftsarten `FREI`, `VERFAHREN` und `EIGENKONSTRUKTION` — alle drei in der Wertemenge;
  `FREI` bleibt `FREI`, `VERFAHREN` und `EIGENKONSTRUKTION` werden zu `IMPORT` (Regel
  `ImportHerkunft`);
- die 42 Parameterschlüssel — jeder im `TwwParameterkatalog`, in Einheit und Bereich (dieselbe
  Prüfung, die das Werkzeug zieht);
- die Nutzungsarten ohne Spalte `ID` — zulässig, sie binden dann den Vorgabesatz ihrer Gruppe.

**Die Regel der früheren Paketstände bleibt unberührt.** `PaketteilNachfuehrung` greift in
`PaketNutzungsartAus` nach dem Lesen der Zeile und entscheidet über **Bezeichner, Bezugsart,
Status und die rohe Provenienz der Gruppe Bedarf** (`Bedarf_Version`, `Bedarf_Herkunftsart`) —
nicht über die Katalogversion. Die Zielversion steht unabhängig davon fest und wird auf die
nachgeführte Zeile genauso angewandt wie auf jede andere; Reihenfolge und Dublettenscan bleiben,
wie sie sind.

**Der Paketteil selbst ist unverändert geblieben** (er ist Quelle für Werkzeug und Saatskript;
drei seiner Dateien erzeugt `tww_testkatalog_fiktiv.py`).

## 4 Nachweis an zwei Datenbanken

Beide Nachweise stehen als Testfälle in
`EPOS.Kern.Tests/TwwKatalogimportOhneVersionTests` und laufen in der CI mit; die
Arbeitskopien entstehen unter `%TEMP%`, das Repositorium bleibt unberührt.

**(a) Katalog, der die Paketzeilen schon trägt** — Arbeitskopie von
`Referenzlaeufe/Kenndaten_Test.sqlite` (Katalogversion `TEST-1`), Paketteil als **Dateiwahl im
Ordner** und ein zweites Mal als **ZIP-Archiv**:

| Größe | Wert |
|---|---|
| Dateien des Pakets | 7 |
| Zeilen des Berichts | 57 — 6 Nutzungsarten, 9 Bedarfstage, 42 Parameter |
| angelegt / ersetzt / abgelehnt | 0 / 0 / 0 |
| übersprungen | 57 |
| Katalogversion je Berichtszeile | `TEST-1` (die des Katalogs) |
| Hinweis | `KATALOGIMPORT_OHNE_KATALOGVERSION` mit `TEST-1` |
| **Zellvergleich** über alle sieben Tww-Katalogtabellen | **gleich** — keine Zelle geändert, keine zweite Zeile |

**(b) Katalog ohne die Paketzeilen** — leerer Tww-Katalog (`TwwTestdatenbank`, Schema aus
`TwwSchema`, keine Zeile):

| Größe | Wert |
|---|---|
| angelegt | 57 (6 / 9 / 42), übersprungen 0, ersetzt 0, abgelehnt 0 |
| Tagesgangsätze / Tagesgänge | 5 / 20 |
| Zapfereignisse | 161 |
| Nutzungsarten ohne Zapfkategorien | 0 — jede bekommt den Vorgabesatz ihrer Gruppe |
| Katalogversion | `FREI-1` (der Katalog führt keine eigene) |
| Parametersatz danach | `ZapfprofilCtrl.Parameter()` liest `FREI-1` mit `Zapfprofil.Stochastik.Urlaubsversatz` = 14 d und `Speicherauslegung.Speichertemperatur_Vorgabe` = 60 °C |

**Trockenlauf.** „Nur prüfen, nichts schreiben" rechnet denselben Bericht mit derselben
Zielversion und lässt Katalog und Parametertabelle unverändert.

## 5 Tests

| Ort | Inhalt |
|---|---|
| `EPOS.Kern.Tests/TwwKatalogimportOhneVersionTests` (neu, 12 Fälle) | Regel 1 an ihren vier Grenzfällen und im Vorgang; Paket ohne Version → Zielversion samt Hinweis und Lesepfad; am leeren Katalog → Rückfall; Paket mit Version wie bisher; gemischt → benannte Ablehnung, nichts geschrieben; Prüflauf; der echte Paketteil gegen eine Arbeitskopie der Testdatenbank (Ordner und ZIP) mit Zellvergleich; der echte Paketteil am leeren Katalog |
| `EPOS.Kern.Tests/TwwKatalogimportTests.Ohne_Datei_der_Nutzungsarten_ist_das_Paket_benannt_abgelehnt` | fachlich umgeschrieben: Der Paketteil fällt nicht mehr an der fehlenden Spalte, sondern — wie jedes Paket — an der fehlenden Kopfdatei der Nutzungsarten (`KATALOGIMPORT_KEINE_DATEI`) |

Kein Fall wurde übersprungen oder gelöscht.

## 6 Texte

- Zwei neue `ZapfSatz`-Muster in beiden Sprachen: `ZPG_SATZ_KATALOGIMPORT_OHNE_KATALOGVERSION`
  („Das Paket führt keine Katalogversion; die Zeilen treten der Katalogversion „{0}" des Katalogs
  bei.") und `ZPG_SATZ_KATALOGIMPORT_VERSION_GEMISCHT`.
- Der Hinweistext des Importdialogs (`ZPGK_IMPORT_HINWEIS`) nennt in beiden Sprachen, dass die
  Spalte wahlfrei ist und was ohne sie geschieht.
- `Resource.Designer.cs` neu erzeugt (`designer_neu.py schreiben`, wiederholbar: zweiter Lauf ±0).
- Wiki-Quelle `Projekte/Wiki/Programm Dokumentation - Brauchwasser-Zapfprofil.wiki`, Abschnitt
  *Import*: ein Absatz zur wahlfreien Katalogversion (gültiger Stand, Gegenleseregex ohne neuen
  Treffer, keine Produktdaten). **Noch nicht ins Wiki geladen.**
- `Referenzlaeufe/Katalogpaket_frei/LIESMICH.md`, Regel 2 um den Importweg ergänzt.

## 7 Gates

| Gate | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler |
| `dotnet test WP-Plan.Kern.slnf -c Release --no-build` (xUnit-Schalter) | siehe Abschnitt 8 |
| `dotnet test Werkzeuge/Auslieferungsvorlage/Auslieferungsvorlage.sln -c Release` | 38 / 38 grün |
| `dotnet test Werkzeuge/ZapfprofilValidierung/ZapfprofilValidierung.sln -c Release` | 38 / 38 grün |
| `python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite` | 2043 SQL-Texte, **0 Fundstellen** |
| `python3 Werkzeuge/ResourceDesigner/designer_neu.py schreiben` | wiederholbar, kein abweichender Block |
| Windows-Schale (`-p:EnableWindowsTargeting=true`) | 0 Fehler |
| Referenzlauf | **nicht gezogen** — kein Rechenweg berührt: der Import schreibt Katalogzeilen, kein Referenzprojekt ist angefasst, keine Einfrierregel greift |

## 8 Folgen

- **Windows-Sichtabnahme offen:** den Dialog *Brauchwasser-Nutzungsarten → Import…* mit dem
  Ordner `Referenzlaeufe/Katalogpaket_frei/` und mit dem Schalter „Nur prüfen, nichts schreiben"
  ansehen — Bericht, Hinweiszeile und der geänderte Hinweistext.
- **Wiki-Upload offen** (gebündelt, Seite „Programm Dokumentation - Brauchwasser-Zapfprofil";
  Logbuch-Satz im Bericht des Auftrags entworfen, Versionsnummer beim Anwender zu erfragen).

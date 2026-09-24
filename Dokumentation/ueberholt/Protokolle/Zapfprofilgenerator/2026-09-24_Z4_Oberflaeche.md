# Z4 — Zapfprofilgenerator: Oberfläche vollständig (Protokoll, 24.09.2026)

Statuszeile #464 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Zeile Z4
in Kapitel 7 und der Nachtrag N13 im
[Umsetzungskonzept](../../../aktuell/Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md); Abschnitt 11
der [Übergabe](../../../aktuell/Zapfprofilgenerator/2026-09-23_Uebergabe_Zapfprofilgenerator.md);
Vorstufe im Protokoll [Z3](2026-09-24_Z3_Stochastik.md). Zweig `z4` von `b7572d42`,
48 eigene Commits bis `8cddb04a`; Merges von `origin` (`48d8836d` in `d083d9fc`, `4a9d7449`
in `abae7685`, `3ff9840b` in `5ca34e44`). Alle Gates im Worktree, ohne CI-Lauf bis zum Push.

## Auftrag

Stufe Z4 nach Kapitel 7 des Umsetzungskonzepts: Stufen Erweitert und Experte, Zonenliste für
Mischnutzung, Wohnungstabelle, Tagesgang-Editor, Auslastungsgang, Kategorien als Katalogkopie,
Schätzhilfen, Warnlogik, Dauerlinie, Katalogdialog mit Untermenü und Katalogimport, KiSicht,
Hilfeschlüssel, Wiki, beide Sprachen; dazu die Z4-Folgen aus N9, N10, N11 und N12. Schemaschritt
**124** (Laufangaben der Auslegung und Bezugsart des Bedarfstags). Vier Gruppen (Kern und
Controller · Zapfprofil-Dialog · Editoren, Auslegung und Konstruktor · Katalogdialog) durch Agenten mit
`model: opus` im Worktree `z4`, je Gruppe eine Gegenprüfung und eine Nachbesserung; ohne Push und ohne
CI-Lauf bis zum Abschluss. Der Anwender hat die Stufe ohne vorherige Sichtabnahme der Z3 bestellt
(„Fahre fort").

## Gruppen und Commits

| Gruppe | Inhalt | Commits |
|---|---|---|
| 1 | Kategorien als Katalogkopie (Controller), Warnlogik und Schätzhilfen, Dauerlinie und Auslastungsgang mit Bild, Anzeigetemperatur/Stundenschwelle, Stufe im Auslegungslauf, `ZapfSatz` (385 Muster, 64 alte Schlüssel entfernt, Wache), DTO und Hülle | `23908941`, `b2002e88`, `91d3ecbe`, `777697eb` |
| 1 Schema | Merge `48d8836d`; Schritt 120 → 121 → 124 `SCHRITT_124_ZAPFPROFIL_LAUFANGABEN` (sechs Spalten), Schreibwege, Transfer, Paketteil (Ecodesign L Bezugsart 2), Testdatenbank | `d083d9fc`, `8b4ba1da`, `beba7883`, `4fbb5aac`; `abae7685`, `bbefc8bc`, `ff04ee08`; `5ca34e44` (Merge `3ff9840b`, Anlagenkopplung 122/123), `b6e21b23` (121 → 124), `7c9775c4` (Testdatenbank LFS `1d971b1a…`) |
| 1 Nachbesserung | Sätze ohne „Nicht rechenbar —", Zonenvorsatz nur ohne Zone, Tabellennamen als Begriff; Zirkulation als Hinweis mit Katalogverhältnis; Warnstufe in der Auslegung; Anzeige-Reihenfolge und Parameter; Kommentare; Wachen (Platzhalterzahl, `FormatException`, Konstanten, ganzzahliger Rang); Vorgabesatz-Regel; Schätzhilfe nach Kalibrierung; ChartProben-LIESMICH | `f13ce687`, `63fc909d`, `686c229e`, `04473fb4`, `70d47154`, `cc5a13e4`, `1f600a9d`, `8faa1336`, `17750541`, `a991bea5` |
| 2a | Stufen Erweitert und Experte, Zonenliste mit Summenfuß, Eingabeblöcke nach 5.3 (Wohnungstabelle, Kalender und Ferien, Jahresmesswert, Schätzhilfen, Fachwerte), fünf Reiter mit Dauerlinie, Warnliste, Hülle und DTO, `GebaeudeDesProjekts`, KiSicht (43 Felder) und Hilfeschlüssel, 15 bunit- und 14 Hüllentests | `a8544b8d`, `acc456b0`, `1b036cae`, `cde03eb9`, `d9d46bf9`, `03a99930` |
| 2b | Tagesgang-Editor, Kategorien-Editor, Verfahrensvergleich-Felder und Erzeugerart/Werkstoff in der Auslegung, Ladeleistungs-Vorschlag im Hauptdialog, Konstruktor mit Bezugsart, Hilfe und Wiki, Tests | `9a5cf9cd`, `283ccee3`, `cafc7b6d`, `c9ea982c` |
| 3 | Untermenü Brauchwasser, Katalogdialog `TwwNutzungsartAdminDialog` mit Editor, Katalogimport (N2), Wachen, Rasterprobe (drei Verstöße behoben), Tests | `e38b7f48`, `f4143835`, `93f48c8a`, `f8a40a36`, `63ed1c75`, `de3c282e`, `30e1fcb1` |
| 2a/2b Nachbesserung (Sonnet, Opus-Wochenkontingent erschöpft) | Herkunft unveränderter Tagtypen bleibt (Originalanteile im DTO, `Gleich` 1e-12, Vorlage mit Herkunft); Tagtypwechsel räumt Fehleingaben; Expertenwahl erzwingt Kopie; Überschrieben-Zähler nur wirksame Abweichungen; `Mengengeruest.WohnungstabelleWirksam` als ein Kriterium; Pflichtprüfung aus Kernfunktionen | `fa446670`, `6c0a94eb`, `0195aa0e`, `81ecfe9d`, `dbe40792`, `1f78bb6f` |
| 3 Nachbesserung | `MENU_BRAUCHWASSER` englisch, Titel des Kategorien-Editors, Kappungsfeld > 0, Wiki-Tabu (im Abschluss) | `8cddb04a` |
| Abschluss | Merge `origin` (3ff9840b), Schritt → 124, Testdatenbank, Papiere (N13, Protokoll, Statuszeile, Übergabe, Wiki-Quelle), Gate | `5ca34e44`, `b6e21b23`, `7c9775c4`, `8cddb04a`; Papiere danach |

## Gates

| Stand | Kern-Build | Tests | Weiteres |
|---|---|---|---|
| Gruppe 1 (`777697eb`) | 0 Fehler | 12 420 grün (1 übersprungen) | ChartProben 165 Bilder; SqlDialektPruefer 0; Windows-Schale 0 Fehler; Referenzlauf 5/5 byte-gleich |
| Schema (`4fbb5aac`) | 0 Fehler | 12 565 grün | SqlDialektPruefer 1 774/0; Vorlage 30/30; Referenzlauf 13/13, 387/387 CSV byte-gleich |
| Nachbesserung 1 (`a991bea5`) | 0 Fehler | 12 575 grün | ChartProben 165/165 (146 alte gleich, 5 neu); SqlDialektPruefer 1 774/0; Vorlage 30/30; Referenzlauf 5/5 byte-gleich |
| Merge 121 (`ff04ee08`) | 0 Fehler | 12 644 grün | SqlDialektPruefer 1 774/0; Vorlage 30/30; ChartProben 165; Referenzlauf 13/13 byte-gleich |
| Gruppe 2a (`03a99930`) | 0 Fehler | 12 674 grün | ChartProben 165; Windows-Schale 0 Fehler |
| Gruppe 2b (`c9ea982c`) | 0 Fehler | Kern 5 814, UI 5 953, KiKern 542, Engine 386, Planung 27 (+1) | ChartProben 165; Windows-Schale 0 Fehler |
| Gruppe 3 (`30e1fcb1`) | 0 Fehler | 12 783 grün | ChartProben 165; SqlDialektPruefer 1 787/0; Rasterprobe 0/24, Katalogprobe 0/62 |
| Nachbesserung 2a/2b (`1f78bb6f`) | 0 Fehler | 12 796 grün (1 übersprungen): Kern 5 862, UI 5 979, KiKern 542, Engine 386, Planung 27 | Windows-Schale 0 Fehler; Designer ohne Diff |
| Abschluss (8cddb04a) | 0 Fehler | 12 893 grün (1 übersprungen): Kern 5 943, UI 5 988, KiKern 549, Engine 386, Planung 27 | SqlDialektPruefer 1 803/0; Vorlage 30/30; ChartProben 165/165; Windows-Schale 0 Fehler; Referenzlauf 13/13 gegen `2026-09-24_R14_Kaelteerzeuger` PASS, 394/394 CSV byte-gleich; Rasterprobe nicht erneut gemessen (die Messung der Gruppe 3 gilt, die letzte CSS-Änderung hat keine Maßwirkung); Designer ohne Diff |

## Merge und Nachzug

- `d083d9fc`: Merge `48d8836d` (E9b, Schemastand 119 der Kühlung) nach `z4`; Konflikte nur in beiden
  Resource-`.resx`, beide Seiten behalten, Designer neu erzeugt; Testdatenbank von `origin`.
- **Schemanummer dreimal vergeben.** Z4 setzte seinen Schritt als 120 auf (auf `origin` und allen
  lokalen Zweigen gemessen, mit beiden Nachbarsitzungen abgestimmt). Danach belegte E10 (#463) die
  120 (`213e529f`), Z4 nummerierte auf 121 um (`abae7685` Merge `4a9d7449`, `bbefc8bc`, `ff04ee08`);
  dann belegte Dialog Design (#468) die 121 (`a04b7330`), Z4 nummerierte beim Abschluss auf
  124 um. Regel seit dieser Stufe zwischen den Sitzungen: **Wer zuerst pusht, hat die Nummer;
  der andere rückt** — und niemand wartet auf eine lange Welle. Die Umnummerierung ist Routine
  (Konstante, Schrittliste, Zielversion, Kommentare, Tests, Katalogskript, LIESMICH; Testdatenbank aus
  der `origin`-Fassung neu erzeugt, Zellvergleich nur in den erwarteten Stellen).
- 5ca34e44: Merge 3ff9840b nach `z4` vor dem Gate; ; Konflikte in SchemaStand, SchemaMigration, Testdatenbankschema, TestDatenbank.cs und Referenzlaeufe/LIESMICH.md, beide Seiten, resx ohne Konflikt.
- Statusnummer #464 mit beiden Nachbarsitzungen abgestimmt (#463 E10, #465–#470 Dialog Design und
  Wirtschaftlichkeit).

## Gegenprüfungen

- **Gruppe 1** (zehn Befunde, drei mittel): Ablehnungen nannten die Zone doppelt und die Nutzungsart
  als Id (behoben); die feste Warnung „Zirkulation > Zapfung" widersprach Lehre 3 des Mockups (Hinweis
  mit Katalogverhältnis); die Warnstufe ging in der Auslegung verloren (behoben); Anzeigevorgaben
  ohne Setzung (Parameter); Kommentare, Wachen, Vorgabesatz-Regel, Kategorientests, ChartProben-
  LIESMICH (behoben); EFH-Regel vorgemerkt. Bestätigt: Ressourcen (9 287 Schlüssel je Sprache),
  Kultur, Kategorien, Warnlogik, Dauerlinie, Eingang, DTO, Formales.
- **Gruppe 2a** (zehn Befunde, drei mittel): Überschrieben-Zähler zählte Paare doppelt; Wohnungstabelle
  nach unterschiedlichen Kriterien in Hülle und Kern; Pflichtprüfung des OK lückenhaft — alle drei behoben; geringe Befunde (Fehleingaben verschwundener Felder, Zonenliste, KI-Maximum, Rasterprobe der Wohnungstabelle) als Folgen.
  Bestätigt: Feldtabelle 5.3 vollständig, Datenfluss, Stufenlogik, Auslegung ohne Doppelzustand,
  KI-Zählung 55, Hilfe, Ressourcen (187), Formales.
- **Gruppe 2b** (neun Befunde, ein hoher): Der Tagesgang-Editor ließ unveränderte Tagtypen ihre
  Herkunft verlieren (Prozent hin und zurück, bitgenauer Vergleich; 16 von 20 abgeleiteten Reihen) —
  behoben (Originalanteile durchgereicht, Toleranz, Vorlage mit Herkunft, Test mit nicht-rundem Satz); Tagtypwechsel mit hängender Fehleingabe; Expertenwahl überschrieb einen nie gezeigten
  Satz — beide behoben; geringe Befunde als Folgen. Bestätigt: Esc und Titel, Sperre, Kategorienregeln allein im Kern (Summe ≠ 1
  renormiert der Kern), Auslegung ohne Doppelzustand, Konstruktor, KI-Zählungen, Ressourcen (116),
  Formales.
- **Gruppe 3** (Sonnet, zwei Befunde): englischer Text `MENU_BRAUCHWASSER` „Industrial water"
  (hoch, im Abschluss behoben); kein Größenschutz beim ZIP-Import wie im Bestand (Folge). Bestätigt:
  Menü mit zwei Punkten, kein SQL in der Oberfläche, Sperren, Import N2 mit Rollback, Wachen,
  gescoptes CSS, 9 890 Schlüssel je Sprache, Formales.

## Abweichungen vom Papier

Stehen im Nachtrag N13: Satzregeln des Kerns; Zirkulation als Hinweis; Anzeige-Reihenfolge;
Vorgabesatz-Regel; Zonenliste als Haustabelle; Ladeleistungs-Vorschlag aus der Auslegung; Bundesland
gesperrt; Kalender an Gebäude; Ferien als Tag/Monat; Dauerlinie ab Erweitert; Hilfeknopf je Gruppe;
Kategorien-Summe ≠ 1 nur Hinweis; Katalogversion „-E<n>"; Hilfeschlüssel der Berechnung auf die
Bedienseite; Editor schreibt über die Nutzungsart; Katalogdialog als Stammblatt mit anders verteilten
Knöpfen, kein „Typ ändern"; Katalogimport ohne Bedarfstag und Parameter.

## Offene Punkte

- Sichtabnahme unter Windows (Prüfliste in der Übergabe, Abschnitt 11) — auch die der Stufen Z1–Z3.
- Fachentscheid: Ecodesign-Profil L nach Wohneinheiten skalieren; ZU20, ZU21 (erweitert); K8, ZU15.
- Konstruktorzeilen in der Datenbank (Schemaschritt, Z5); Vermerke des Herkunftsprotokolls als Sätze;
  Katalogimport um Bedarfstage und Parameter; Katalogdialog auf iOS; Berechnungsseite
  `Zapfprofil.wiki`; Dauerlinienbild in die Linux-Messlatte.
- Wiki-Upload der Bedienseite und drei Logbuch-Sätze mit Versionsnummer vom Anwender.
- Z4b: VDI-4655-Import mit Typtagzuordnung (T3) unter der Regel ZU19; Z5: Kalibrierung und
  Validierung, Nichtwohn-Kategorien, Katalogausbau.

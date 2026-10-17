# Protokoll K‑F5 — Rückkühlwerk in der Oberfläche (ohne Schemaschritt)

**17.10.2026** · Sitzung Kälteanlagen · Grundlage: [Entwurf Split, VRF und Rückkühlwerk](../../../aktuell/Kälteanlagen/2026-10-10_Entwurf_Split_VRF_Rueckkuehlwerk.md),
Abschnitt 10 (Zeile K‑F5); Vorgänger [K‑F3](2026-10-11_K-F3_Teilfreikuehlung_Reihe.md). Statuszeile #1189.

## Ziel

Das Rückkühlwerk (Katalog, Projektkopie und Rechenweg aus K‑F1 bis K‑F3) wird in der Oberfläche pflegbar und wählbar und
erscheint im Bericht. Die Wasserkosten sind mit der Kostenbasis (#1125) schon gebaut und gehören nicht zu dieser Welle.

## Umsetzung

- **K‑F5a Verwaltung „Rückkühlwerke“** unter „Daten & Import“: Razor-Dialog `EPOS.UI/Dialoge/Erzeuger/RueckkuehlwerkDialog.razor`
  im Gerüst der Verwaltungen (Katalograhmen, Katalogliste, Stammblatt mit fünf Gruppen samt Herkunft, Vorgaben als
  Platzhalter, Hinweis 42. BImSchV), Hülle `EPOS.UI.Daten/Erzeuger/RueckkuehlwerkHuelle.cs`, Windows-Fenster
  `RueckkuehlwerkFenster`, iOS-Wurzel, Seitenschlüssel `Seitenschluessel.Rueckkuehlwerk`, Hilfeziel mit Wiki-Abschnitt,
  KI-Maske. Kern: `RueckkuehlwerkStammCtrl` löscht nur ohne Projektkopie (Verwendungen, `KatalogVerwendung`),
  Katalogfilterzeilen und Profil `FuerRueckkuehlwerk`, Anzeigetexte der Wertemengen, benannte Prüfmeldungen mit Feld und
  Bereich; Ressourcen beider Sprachen samt KI-Texten.
- **K‑F5b Gruppe „Rückkühlung“** in `KaeltemaschineKonfiguration.razor`, nur bei wassergekühlter Kältemaschine (bei
  Rückkühlart Luft ein Hinweis): Klappliste „Rückkühlwerk“ mit „keins“ und den Katalogsätzen, Lesezeilen der Wahl mit
  „Vorgabe: …“, Hinweise Luftfeuchte fehlt, 42. BImSchV, Ventilator ersetzt den Hilfsstrom; Knopf „Rückkühlwerke…“ öffnet
  die Verwaltung über `Dienste.Navigation`. Die Wahl schreibt `KaeltemaschineAnlageHuelle` im OK-Weg über
  `KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen`, nur bei Abweichung; `KaeltemaschineAnlageCtrl.KlimaMitFeuchte` liefert
  den Feuchtehinweis. KI-Feld `rueckkuehlwerk`; der Proben-Wirt (Fenster-, Rollbereichprobe) zeigt die Gruppe.
- **Berichtszeile:** Je Kältemaschine, deren Ergebnis die Größen des Rückkühlwerks trägt, steht unter der Tafel
  „Kälteerzeuger“ eine Zeile mit Name und Bauart, Ventilatorstrom, Wasser, Teil-Freikühlung und mittlerer
  Rückkühltemperatur (de/en). Name und Bauart sammelt der `BerichtsDatenSammler`
  (`EPOS.Kern/Allgemein/Bericht/RueckkuehlwerkBerichtszeile.cs`). Ohne Rückkühlwerk bleibt die Tafel unverändert; kein neues
  Vorlagenfeld, keine neue Berichtskatalogfassung.
- **K‑F5c** (Zweig `b9dcf0e33`, `211978287`) behebt die zwei Befunde aus K‑F5b: (a) Abbrechen nach einer
  Zwischenspeicherung stellt das Rückkühlwerk über einen Schnappschuss exakt wieder her
  (`RueckkuehlwerkSchnappschuss`/`RueckkuehlwerkWiederherstellen`, `Rueckkuehlwerksitzung` in der Hülle) — auch eigene
  Werte und eine Kopie ohne Katalogsatz; (b) eine Regel `KaeltemaschineSchema.IstLuftgekuehlt` (leer oder `LUFT`) gilt für
  Lauf (`Kaeltemaschine`, `SimulationControl.Kaelte`), Hülle und Prüfung (`KaeltemaschineAnlageCtrl.RueckkuehlwerkPruefen`
  lehnt bei leerer Rückkühlart ein Rückkühlwerk ab); Rechenweg unverändert. Befund Testdatenbank: kein Rückkühlwerk an
  einer Anlage, keine Kältemaschine mit leerer Rückkühlart. Im Zweig: Kern-Filter 469/469, UI-Filter 409/409,
  SQL-Prüfer 0 Fundstellen.
- **Wiki-Quellen:** „Kälteerzeugung“ (Abschnitt „Rückkühlwerk“: Wahl, Bauarten, Annäherung, Ventilator, Freikühlung
  parallel/Reihe, Hinweise, Ergebnisse, Kennzahlen, Verweis auf die Verwaltung; Bericht-Punkt) und „Gerätekataloge“
  (Verweis auf die Wahl an der Kältemaschine); zwei Logbuch-Sätze im Entwurf `Wiki_Update_2026-09-26.md` ohne Version.
- **Wachen nachgezogen:** Menüband, KI-Dialogkatalog, KI-Maskenabdeckung, Entwicklerbereiche (Zahl des Anwenderbaums).
- Tests neu: `RueckkuehlwerkStammCtrlTests`, `RueckkuehlungHuelleTests`, `RueckkuehlwerkBerichtTests` (Kern),
  `RueckkuehlwerkDialogTests`, `KaeltemaschineRueckkuehlungTests` (UI), Ergänzungen in `KaeltemaschineAnlageDialogTests`
  und `KaeltemaschineAnlageWpBearbeitenTests`.

## Prüfungen

Zusammengeführt mit KWN‑1a auf `origin/ios_migration_september` `c9df9630c`; Zweig K‑F5a/b `c95bf0d6c`, Zusammenführung
`8c74c9bf4`; K‑F5c `211978287`, Zusammenführung `0e842c00e`. Gate und voller Kerntest liefen auf `d4ef736d3` (K‑F5a/b
mit KWN‑1a); nach K‑F5c der Nachlauf unten.

- Kern-Filter (Release) und Windows-Schale auf Linux: 0 Fehler. Designer unverändert (19 243 Einträge, zweiter Lauf +0).
- Gate `gate_rest_linux.sh`: EPOS.UI.Tests 9 752, KiKern.Tests 553, SpeicherEngine.Tests 397, SpeicherPlanung.Tests 27
  (1 übersprungen), Dokumentationswachen 35, Werkzeugtests 124/63/24/39 grün; SQL-Dialekt 3 062 Texte, 0 Fundstellen;
  BOM und Konfliktmarker keine.
- EPOS.Kern.Tests voll: 14 832 Tests, 14 824 grün, 7 übersprungen, 1 rot — `ZahlungsgliederungTests.Die_Bruecke_stapelt_die_Differenzspalte` (Modellbreite 611 statt 610), auf unberührtem origin ebenso rot (#1181; dieselbe Ursache wie die ChartProben unten): fremd. Die vier übrigen Projekte im selben Lauf: EPOS.UI.Tests 9 752, KiKern.Tests 553, SpeicherEngine.Tests 397, SpeicherPlanung.Tests 27 (1 übersprungen), alle grün.
- Referenzlauf: 38 von 38 Projekten gegen `2026-10-17_R59_Ergebnisreihen` `GESAMT: PASS` (13 025 990 Werte in Toleranz),
  CSV byte-gleich 1 233/1 233 — die neunzehn Projekte der CI-Auswahl eingeschlossen. Basis unverändert.
- Browserproben (Chromium headless, Wirt aus dem gemeinsamen Stand): `rasterprobe.mjs` 29/29 Fälle erfüllt,
  `fensterprobe.mjs --nur kaeltemaschine` erfüllt (Gegenprobe rot), `rollbereichprobe.mjs --nur kaeltemaschine`
  110 Zustände, 0 Verstöße.
- ChartProben: 277 Bilder, 2 Verstöße (`kapitalwert_bruecke`, `kapitalwert_bruecke_unter_referenz` 1240×611 statt
  1240×610) — auf dem unberührten `origin/ios_migration_september` `c9df9630c` derselbe Befund: fremd (Schriftkette des
  Containers, vgl. #1181), nicht Teil dieser Welle.

- **Nachlauf nach K‑F5c** auf `0e842c00e`: Kern-Filter und Windows-Schale 0 Fehler; gefilterte Tests
  (`Kaeltemaschine|Rueckkuehl|KiDialog|KiMasken|KaelteWaermeKopplung`) EPOS.Kern.Tests 494/494, EPOS.UI.Tests 409/409;
  SQL-Dialekt 3 064 Texte, 0 Fundstellen; Referenzlauf der CI-Auswahl 19/19 gegen R59 `GESAMT: PASS` (6 688 103 Werte in
  Toleranz). Der volle Kerntest ist nach K‑F5c nicht wiederholt.

## Abweichungen

- Konflikt im Logbuch-Entwurf: Der Block „Rückkühlung an der Kältemaschine“ steht nach der Veröffentlichung 1.2.0.11 unter
  den Entwürfen ohne Version (Überschrift vierter Ebene), nicht als eigener Versionsblock.
- Wasserkosten nicht in dieser Welle (mit #1125 gebaut).
- `EPOS.iOS/Dienste/IosProjektQuelle.cs` ist ergänzt; die iOS-Hülle baut nur auf macOS und ist hier nicht gebaut (kein
  iOS-Lauf ohne Rückfrage).

## Behoben in der Welle

- **(a) Abbrechen nach Zwischenspeicherung** stellte die Projektkopie über den Katalogsatz wieder her (eigene Werte
  verloren, Kopie ohne Katalogsatz nicht wiederherstellbar) — behoben mit K‑F5c (`211978287`, Schnappschuss).
- **(b) Leere Rückkühlart:** `RueckkuehlwerkPruefen` ließ bei leerer Rückkühlart ein Rückkühlwerk zu, Oberfläche und Lauf
  werteten leer als luftgekühlt — behoben mit K‑F5c (`b9dcf0e33`, Regel `IstLuftgekuehlt`).

## Offen

- Wiki-Seiten „Kälteerzeugung“ und „Gerätekataloge“: Upload ausstehend (Sammel-Upload, Version beim Anwender).
- K‑F4 Referenzprojekt mit Rückkühlwerk und Feuchtesaat bleibt offen.

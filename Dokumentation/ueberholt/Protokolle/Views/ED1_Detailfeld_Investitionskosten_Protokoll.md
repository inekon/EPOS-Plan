# ED‑1 — Detailfeld Investitionskosten in den Erzeugerdialogen (Protokoll, 21.09.2026)

Statuszeile #422 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md).
Zweig `erz-detail`, Commit `07b791d7`; Merge `16182ef3`.

## Anlass

Meldung des Anwenders aus der Abnahme am Gerät (Windows, Bildschirmfoto vom 21.09.2026): In der
Maske „Verwaltung Heizkessel" zeigt der Detailblock unter dem Gruppenkopf „Modul" neben
„Leistung [kW]" ein nur lesbares Feld „Investitionskosten [€]: 0,00". Entscheid: „Die Anzeige der
Investitionskosten an dieser Stelle hat keine Funktion. Feld herausnehmen." Dazu der Auftrag, die
übrigen Erzeugerdialoge der Verwaltung zu prüfen.

## Befund

- Der Detailblock entsteht nicht im Razor-Dialog, sondern in der Windows-Hülle: `DetailZu` in
  `WindowsFormsApplication1/Views/Heizkessel/HeizkesselHuelle.cs` (Brennstoff Typ, Leistung,
  Investitionskosten; Schalter Brennwertkessel) und in
  `WindowsFormsApplication1/Views/Pufferspeicher/PufferspeicherHuelle.cs` (Hersteller, Speichertyp,
  Bereitschaftsverluste, Gesamtvolumen, Investitionskosten). Der Razor-Dialog zeigt die gelieferten
  Felder nur lesbar.
- Der Preis wird seit dem Anwenderentscheid vom 15.09.2026 („Der Dialog über Button Bearbeiten soll
  keine Kosten und Emissionen enthalten") im Aufklapper „Alle Daten anzeigen" desselben Dialogs
  gepflegt: Raster `Katalogfelder` aus `HeizkesselAdminHuelle.Wege().Detail` beziehungsweise
  `PufferSpAdminHuelle.Wege().Detail` mit `KatalogBrowserProfil.FeldInvestitionskosten`,
  bearbeitbar und speicherbar. Die Anzeige darüber war eine Dublette ohne Funktion.
- BHKW, Photovoltaik und Solarkollektor führen kein Kostenfeld im Detailblock. Der Stromspeicher
  zeigt „Modulkosten [€/kWh]" (`SP_LABEL_MODULKOSTEN`), einen Kennwert des Moduls und keine
  Dublette der Investitionssumme; unberührt. Der Wärmepumpen-Anlagendialog hat eine andere Bauart
  und nennt die Summen der Kostenpositionen („Invest … € · Betrieb … €/a"), das ist eine Funktion.

## Änderung

- Beide Tupel entfernt; an ihrer Stelle ein Kommentar im Hausstil (was dort stand, der Entscheid,
  die Pflegestelle). Der Wert bleibt im Datensatz (`KesselDetail.Investitionskosten`,
  `SpeicherDetail.Investitionskosten`) und in den Katalogfeldern.
- Doku-Kommentare nachgezogen: `<summary>` über `HeizkesselHuelle.DetailZu` und der Vergleich der
  vier Detailblöcke in `EPOS.UI/Dialoge/Erzeuger/KatalogBrowserDaten.cs`.
- Die Ressourcenschlüssel `HZK_LBL_INVEST` und `PSPD_LBL_INVEST` bleiben in de und en: Keine Wache
  verlangt ihre Entfernung, und die `HZKK_LBL_*` des Katalogeditors blieben am 15.09.2026 ebenso.
- Wiki-Quelle `Programm Dokumentation - Pufferspeicher.wiki`: Der Aufzählungspunkt
  „Investitionskosten" beschreibt jetzt den Aufklapper „Alle Daten anzeigen", in dem der Betrag
  steht.
- Tests: Die Testdaten in `HeizkesselDialogTests` (Detailblock sechs statt sieben Felder) und
  `PufferspeicherDialogTests` (fünf statt sechs) folgen der Hülle. Neue Wache
  `EPOS.UI.Tests/Dialoge/ErzeugerDetailblockTests.cs` liest die beiden Hüllendateien als Text (der
  Weg von `KostenknopfWegeTests`, weil `EPOS.UI.Tests` die Windows-Schale nicht referenziert und
  `DetailZu` privat ist): Heizkessel führt Brennstoff Typ, Leistung und den Schalter Brennwertkessel,
  Pufferspeicher seine vier Kennwerte, keiner einen `*_LBL_INVEST`-Schlüssel; dazu eine Gegenprobe.
- KI-Assistent: `Form_Heizkessel` und `Form_PufferSp` deklarieren kein Feld `investitionskosten`;
  der Katalog bleibt bei 34 Masken und 330 Feldern.

## Nebenbefund: Ersatzzeichen im Katalognamen

Der Name „Vitocrossal 200 CM2 raumluftabh�ngig" im Bildschirmfoto trägt ein Ersatzzeichen statt
„ä". Der Lesepfad ist sauber (`DataRepository.GetDataTable` über `Microsoft.Data.Sqlite`, keine
eigene Dekodierung; der VDI‑3805-Import liest Windows‑1252). In der Datenbank steht die
UTF‑8-Folge `EF BF BD` (U+FFFD): Der Umlaut ging bei einer älteren Übernahme beim Schreiben
verloren, nicht beim Lesen. Betroffen sind `Tab_Heizkessel_Stamm` ID 251 und acht Projektkopien
in `Tab_Heizkessel` (1018326–1018330, 1018334, 1027445, 1027448), die den Namen geerbt haben.
Nicht geändert; eine Korrektur müsste Katalogsatz und Kopien treffen (Statusdatei, Nach #422).

## Zahlen und Abnahme

Worktree: Kern, UI und Windows-Schale (Debug x64) je 0 Fehler; `EPOS.UI.Tests` 5 063 Tests grün,
0 übersprungen. Gate im Hauptbaum auf `16182ef3`: siehe Statuszeile #422.

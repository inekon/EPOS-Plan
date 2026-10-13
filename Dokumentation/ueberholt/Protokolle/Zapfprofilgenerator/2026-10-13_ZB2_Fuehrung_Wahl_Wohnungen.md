# Zapfprofil: geführte neue Zone, waagerechte Wahl, Wohnungstabelle (W2, 13.10.2026)

Protokoll der Welle **W2** der Sitzung „Zapfprofilgenerator Cloud“ zu den Vorschlägen **ZB2**, **ZB4 (a)**,
**ZB6** und **ZB10** des
[Konzepts Zapfprofil — Benutzerführung](../../../aktuell/Konzept_Zapfprofil_Benutzerfuehrung_EPOS-Plan.md)
(Befunde B2, B4, B7, B11).

**Rahmen.** Worktree `agent-a94f8f24bfddd092f`, Zweig `worktree-agent-a94f8f24bfddd092f` von `f5cb9c391`
(Merge der Welle W1), Opus 5.5. Fünf Commits `2074bd519`, `d75fb4e71`, `0037eedb7`, `99a8ce8e8`,
`ede69dbdc`. Kein Schemaschritt, keine Änderung der Testdatenbank, kein Rechenweg.

---

## 1 Neue Zone geführt (ZB2)

- Der Knopf heißt „Zone hinzufügen“ / „Add zone“ (`ZPG_BTN_ZONE_NEU`), ohne Auslassungspunkte.
- `ZapfprofilHuelle.Pruefen` hängt an jede Meldung einer Zone deren Position (der doppelte Name bleibt
  ohne Position); keine zweite Regelsammlung. Der Dialog liest daraus die **unvollständigen Zonen**: in
  der Liste „unvollständig“ (`ZPG_ZONE_UNVOLLSTAENDIG`) statt „0,0 · trägt 0“, Anteil und Rechenweg „–“;
  am Zonenblock leise der erste Prüfsatz als nächster Schritt. Ihre Vorschaumeldungen entfallen am Block,
  in den allgemeinen Meldungen und in „Hinweise der Bilanz“ (`ZapfprofilWarnDaten.Position`, gesetzt von
  der Hülle). Eine Zone, die die Prüfung besteht und trotzdem abgelehnt wird, bleibt eine Warnung.
- Nach „Zone hinzufügen“ steht der Fokus auf der Klappliste Nutzungsart, ohne zu rollen
  (`feldFokus` in `EPOS.UI/wwwroot/epos-sprung.js`).

## 2 Waagerechte Wahl (ZB4 (a))

`EPOS.UI/Bausteine/Optionsgruppe.razor` trägt den Parameter `Waagerecht`: Einträge nebeneinander und
umbrechend, je 44 px hoch; die Wahl trägt `epos-option--gewaehlt` (Rahmen in `--epos-marke`, unter
`forced-colors` 2 px `Highlight`); Erläuterung und Wirkung der gewählten Option als leise Zeile unter der
Gruppe und im `title` jedes Eintrags; die Tastatur bleibt eine Radiogruppe. Alle acht Optionsgruppen des
Zapfprofils stehen waagerecht (Stufe, Bedarfsniveau, Zirkulation, Einheit des Messwerts, dreimal
auto/manuell, Rechenweg der Jahresreihe).

## 3 Wohnungstabelle (ZB6)

- Überschrift „Wohnungen (wahlweise)“ mit dem Erklärsatz (`ZPG_HINW_WOHNUNGEN`); Spaltenköpfe mit Einheit
  und Hilfe im `title` (`ZPG_SP_WOHNUNG_*`, `ZPG_TIP_WOHNUNG_*`).
- Summenzeile aus der wirksamen Bezugsmenge der Vorschau: bei Bezugsart Personen „ergibt n Wohneinheiten
  · m Personen“, bei Wohneinheiten „ergibt n Wohneinheiten“ (der Kern setzt dort keine Personen an).
- Die Spalte „Ausstattungsklasse“ steht nur, wenn der Katalog Klassen führt oder eine Zeile schon eine
  trägt; sonst eine leise Zeile mit dem Weg über die Paketvorlage `Katalogpaket_DIN4708` und „Import…“
  (`ZPG_HINW_AUSSTATTUNG_OHNE_KATALOG`).
- Befund N13 (u) des Umsetzungskonzepts behoben: feste Spaltenanteile, Felder in Zellenbreite,
  umbrechende Köpfe; im Browser gemessen rollt die Tabelle nicht mehr quer (0 px bei 1 500 und 900 px
  Fensterbreite, vorher 162 px).

## 4 Anzeigeeinstellungen (ZB10)

„Temperatur der Literanzeige“ und „Schwelle der Stundenzählung“ sind aus „Fachwerte · Gebäude“ entfernt;
dort verweist eine leise Zeile auf den Einstellungsdialog, Rubrik „Zapfprofil“
(`ZPG_HINW_ANZEIGE_EINSTELLUNG`). Mit den Feldern entfallen Setzer, KI-Anmeldung, die Eigenschaften der
KI-Sicht und die beiden Felder im KI-Katalog des Kerns; die Eingabebilanz der
`KiMaskenabdeckungWacheTests` steht auf 57 statt 59. Die Felder `AnzeigetemperaturC` und `SchwelleKw` des
Eingabe-DTO bleiben leer stehen; die Rechnung liest die Einstellung.

## 5 Ressourcen

Geändert `ZPG_BTN_ZONE_NEU`, `ZPG_LBL_WOHNUNGSTABELLE`, `ZPG_HINW_WOHNUNGSTABELLE_LEER`; neu
`ZPG_HINW_WOHNUNGEN`, `ZPG_SP_WOHNUNG_ANZAHL`, `…_RAUMZAHL`, `…_PERSONEN`, `ZPG_TIP_WOHNUNG_ANZAHL`,
`…_RAUMZAHL`, `…_PERSONEN`, `…_AUSSTATTUNG`, `ZPG_WOHNUNGEN_SUMME`, `ZPG_WOHNUNGEN_SUMME_WE`,
`ZPG_HINW_AUSSTATTUNG_OHNE_KATALOG`, `ZPG_ZONE_UNVOLLSTAENDIG`, `ZPG_HINW_ANZEIGE_EINSTELLUNG`; entfernt
`ZPG_HINW_WOHNUNGSTABELLE`, `ZPG_LBL_ANZEIGETEMPERATUR`, `ZPG_LBL_STUNDENSCHWELLE`, `ZPG_HINW_ANZEIGE`,
`KI_DLG_ZPG_ANZEIGETEMPERATUR_ERL`, `KI_DLG_ZPG_STUNDENSCHWELLE_ERL`; beide Sprachen, Designer neu
erzeugt.

## 6 Tests und Abnahme

Neu: sechs Fälle `OptionsgruppeTests`, zwei `StilblattTests`, sieben in
`EPOS.UI.Tests/Dialoge/ZapfprofilDialogStufenTests.Fuehrung.cs`, Positionsprüfungen in
`ZapfprofilHuelleTests`. Im Worktree: Bau 0 Fehler, Windows-Schale 0 Fehler, `EPOS.UI.Tests` 9 340 grün,
`KiKern.Tests` 553, `SpeicherEngine.Tests` 397, `SpeicherPlanung.Tests` 27 (1 übersprungen),
`EPOS.Kern.Tests` gefiltert 1 034 grün; der volle Kern-Lauf lief dort wegen der vollen Platte nicht
durch — maßgeblich ist das Gate auf dem Merge-Stand (Statuszeile). Sichtprobe im Browser (echter Dialog,
Kopie der Testdatenbank, Projekt 1045): nach „Zone hinzufügen“ Fokus auf „Nutzungsart“, Liste
„unvollständig“, leise Zeile „Zone „Zone 2“: Bitte eine Nutzungsart wählen.“

## 7 Offen

ZB8 (Texte und Hinweise) folgt mit W4; der überholte Hinweis `ZPG_GRUND_BUNDESLAND` fällt mit W3b.

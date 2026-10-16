# Welle VS: sichtbare Texte „Befund“ umbenannt (E145)

**Auftrag:** Das Wort „Befund“ ist auf Wiki-Fachseiten tabu (Anwenderentscheid 14.10.2026, E145). Nur Ressourcentexte, Wiki-Quellen und feste Zeichenfolgen in Tests; Schlüssel, Bezeichner, Kommentare unverändert.

## Geänderte Ressourcenschlüssel (je Sprache de und en-US)

| Gruppe | Neuer Text | Schlüssel (Zahl) |
|---|---|---|
| Vollständigkeit | Farbmodus, Sperrtexte, Legende, Stufe „vollständig“, Steckbrief, Filter „Nur unvollständige Bauteile“ samt Hinweis | 8 (`GANS_FARBMODUS_BEFUND`, `GANS_BEFUND_OHNE`, `GANS_BEFUND_OHNE_VORSCHLAG`, `GANS_BEFUND_LEGENDE`, `IMP_BEFUND_OHNE`, `BTSB_BEFUND`, `GIMP_DLG_FILTER_BAUTEILBEFUND`, `…_HINWEIS`) |
| Hinweis (en „Note“) | Flächenspalte, Konfliktliste, Flächenfilter samt Hinweisen | 5 (`GIMP_FL_SP_BEFUND`, `IMP_KONFLIKT_SPALTE_BEFUND`, `GIMP_DLG_FILTER_FEHLER`, `…_HINWEIS`, `GIMP_DLG_FLAECHEN_HINWEIS`) |
| Meldung (en „Message“) | Modellprüfung und Hervorhebung | 9 (`MPB_*` 6, `MP_MSG_VERWORFEN_ZAHL`, `GANS_PRUEF_OHNE`, `GANS_PRUEF_VERWERFEN_HINWEIS`) |

Geänderte Einträge: 44 Zeilen je Sprachdatei. `Resource.Designer.cs` neu erzeugt.

## Wiki-Quellen

`Gebäude.wiki` (3 Zeilen), `Gebäudeimport.wiki` (5 Zeilen, Anker `befund` → `vollstaendigkeit`, keine Verweise darauf), `Projektverwaltung.wiki` (2 Zeilen). `grep Befund Projekte/Wiki/*.wiki` leer. Logbuch-Entwurf `Wiki_Update_2026-09-26.md`: ein Satz auf „Vollständigkeit“.

## Tests

Angepasst: `GebaeudeBefundAnsichtTests` (3), `GebaeudeImportZonenDialogTests` (+ `.Befund`, 5 Stellen), `GebaeudeImportZonenHuelleTests` (1). Lauf: UI-Filter 153 Tests, Kern-Filter 336 Tests (u. a. Wiki-Produktdaten, Dokumentationslinks, Ressourcen), danach die zwei geänderten Klassen erneut: alle grün. Kern-Build 0 Fehler.

## Offene Punkte

Nicht umbenannt (im Auftrag nicht genannt): Spalte „Befund“ in `GEBSIM_SP_BEFUND` (Gebäudesimulationsliste) und `ZNB_SPALTE_BEFUND` (Zonen-Nutzungsblatt); Vorlagenprüfer (`BV_VORLAGEN_PRUEFZEILE_*`, `VF_PRUEF_KEINE_BEFUNDE`), Einheiten-Konsistenz (`KI_REG_EINHEITEN_*`), KI-Erläuterungen (`KI_DLG_KSE_STATUS_ERL`, `KI_DLG_IKF_AKTION_ERL`). Die Ausnahme „Befund“ in `BerechnungsHilfeTests` betrifft die Seite Photovoltaik und bleibt.

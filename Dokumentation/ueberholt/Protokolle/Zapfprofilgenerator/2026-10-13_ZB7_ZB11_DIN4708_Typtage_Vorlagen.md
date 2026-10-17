# DIN-4708-Katalogwerte über den Katalogimport und Paketvorlage Typtage VDI 4655 (W5, 13.10.2026)

Protokoll der Welle **W5** der Sitzung „Zapfprofilgenerator Cloud“ zu den Vorschlägen **ZB7** und **ZB11**
des [Konzepts Zapfprofil — Benutzerführung](../../../aktuell/Konzept_Zapfprofil_Benutzerfuehrung_EPOS-Plan.md)
(Befund B8; Rückfragen des Anwenders „Wo wird die Ausstattungsklasse festgelegt?“ und „Typtage nach
VDI 4655: Woher kommen die Daten?“; Entscheide ZB‑Q4 A und ZB11).

**Rahmen.** Worktree `agent-a81cdd7fade0e9b89`, Zweig `worktree-agent-a81cdd7fade0e9b89` von `6deba6557`,
Opus 5.5. Commits `737a709ac` (Kern, Oberfläche, Ressourcen), `fefa5e4f0` (Paketvorlage DIN 4708, Setup,
Wachen, Tests), `3a62d1412` (Wiki, Setup-Konzept), `4dc2059b0` (ZB11). Kein Schemaschritt, keine Änderung
der Testdatenbank, kein Rechenweg; keine Normzahl im Repositorium.

---

## 1 DIN-4708-Katalogwerte im Katalogimport (ZB7)

`EPOS.Kern/Controller/TwwNutzungsartCtrl.ImportDin4708.cs`: wahlfreie Datei `Tab_TwwDin4708Wert_STAMM.csv`.

| | |
|---|---|
| Pflichtspalten | `Art`, `Schluessel`, `Wert`, `Quelle`, `Version`, `Herkunftsart`; Formregeln wie die übrigen Dateien, eine Zahl mit Komma lehnt das Paket mit Datei, Zeile und Spalte ab |
| Katalogversion | Tabelle in `IMPORT_TABELLEN` und `IMPORT_TABELLEN_MIT_VERSION`: ohne Spalte tritt sie der Version des Zielkatalogs bei; die Spalte nur in einem Teil der Kopfdateien lehnt das Paket ab (`VERSION_GEMISCHT`) |
| Inhalt (lehnt nur die Zeile ab) | `Art` BELEGUNG oder AUSSTATTUNG (gespeichert groß); BELEGUNG: Raumzahl > 0 in halben Schritten, gespeichert so, wie der Rechenweg sucht („2.0“ → „2“, „2.5“ bleibt); AUSSTATTUNG: Schlüssel nicht leer; `Wert` > 0 |
| Abgleich über (Art, Schlüssel, Katalogversion) | gleich → übersprungen; abweichend → **am Platz ersetzt** (gleiche ID, danach `IMPORT`, `ReadOnly` 0), Bericht mit altem und neuem Wert; neu → angelegt mit `IMPORT` |
| Bericht | eigene Gruppe „DIN-4708-Katalogwerte“ (`TwwImportbereich.Din4708`) zwischen Parametern und Nutzungsarten; „Nur prüfen“ schreibt nichts; ohne Tabelle in der Datenbank ist die Datei benannt übergangen |
| Kennwertpaket | ein Paket mit der DIN-4708-Datei und ohne Dateien der Nutzungsarten-Kette braucht keine `Tab_TwwNutzungsart_STAMM.csv`; jedes andere Paket ohne Nutzungsarten bleibt abgelehnt (`KEINE_DATEI`) |

**Paketvorlage** `Referenzlaeufe/Katalogpaket_Vorlage_DIN4708/`: `Tab_TwwDin4708Wert_STAMM.csv` (drei
Belegungszeilen Raumzahl 1–3, zwei Klassen „Klasse 1“, „Klasse 2“), `Tab_TwwParameter_STAMM.csv` (elf
Schlüssel `DIN4708.*` mit den Einheiten aus `TwwParameterschluessel`), beide ohne Spalte `Katalogversion`,
die Spalte `Wert` überall leer; `LIESMICH.md` mit der Anleitung („Nur prüfen“ zuerst). **Abweichung von der
A100-Vorlage:** Die unveränderte Vorlage wird Zeile für Zeile benannt abgelehnt
(`KATALOGIMPORT_PFLICHT_FEHLT` „Wert“) und spielt nichts ein — ihre Platzhalter wären Parameter, die
vorhandene Werte der Katalogversion ersetzten. Setup: `#define KatalogVorlageDin4708`, Ziel
`{app}\Vorlage\Katalogpaket_DIN4708`.

Oberfläche: dritte Berichtsgruppe im `TwwNutzungsartAdminDialog`, `ZPGK_IMPORT_HINWEIS` nennt die neue
Datei. Neue Ressourcen `ZPG_SATZ_KATALOGIMPORT_DIN4708_OHNE_TABELLE`, `…_ART`, `…_RAUMZAHL`, `…_WERT`,
`…_ERSETZT`, `ZPGK_IMPORT_GRP_DIN4708`, `ZPGK_IMPORT_SP_DIN4708`. Das Werkzeug `Auslieferungsvorlage`
nutzt nur den unveränderten Paketleser; seine `PAKETREIHENFOLGE` kannte die Tabelle schon (63/63 grün).

## 2 Paketvorlage Typtage VDI 4655 (ZB11)

`Referenzlaeufe/Typtagpaket_Vorlage_VDI4655/` mit den sechs Dateien des `Normformvektorleser`
(`typtage.csv`, `klimazonen.csv`, `typtage_je_zone.csv`, `f_twe_tt.csv`, `kennwerte.csv`,
`tagesgaenge.csv`), Kopfzeilen genau nach dem Leser, Beispielzeilen nur aus Platzhaltern (Codes T01–T06,
`Gebaeudeart_A`, „Zone A“; `anzahl`, `faktor`, `wert`, `anteil` leer). `LIESMICH.md` nennt je Datei die
Fundstelle in der Richtlinie (nur Tabellen- und Abschnittsnummern), Formregeln und den Weg „Paket
wählen…“ → Prüfbericht → „Einspielen“. Unverändert lehnt schon die Prüfung ab (`NORMVEKTOR_FELD_LEER`,
`typtage_je_zone.csv`, Zeile 2, Spalte `anzahl`); die Typtag-Tabelle bleibt unverändert. Setup:
`{app}\Vorlage\Typtagpaket_VDI4655`; der Dialog „Typtage nach VDI 4655“ (`ZPGT_HINWEIS_FORMAT`) und die
Wiki-Quelle nennen die Vorlage.

## 3 Wachen, Tests, Abnahme

Neu: `TwwKatalogimportDin4708Tests` (15 Fälle; am Ende liefern `ZapfprofilCtrl.Ausstattungen` und
`Belegung` die eingespielten Werte), `TwwTyptagVorlageTests` (drei Fälle); erweitert
`KatalogpaketAuslieferungWacheTests` (beide Vorlagen), `RepositoryOrdnungWacheTests` (Probepfade unter
`Normzahlen/`); angepasst `TwwKatalogimportTests.Fremde_Dateien_…` (Beispiel einer unbekannten Datei jetzt
`Tab_TwwWohnungstyp`). Fortgeschrieben: `Referenzlaeufe/LIESMICH.md`, `Normzahlen/LIESMICH.md`,
`Konzept_Setup_InnoSetup_EPOS-Plan.md`, Wiki-Quelle „Brauchwasser-Zapfprofil“ (Import, Typtage).

Im Worktree: Bau 0 Fehler, Windows-Schale 0 Fehler, gezielte Kern-Tests 260 bzw. 167 grün, UI-Tests 9 331
bzw. 44 grün, Designer unverändert, SQL-Prüfer 2 892 Texte ohne Fundstelle. Der volle Kern-Lauf scheiterte
dort an der vollen Platte; maßgeblich ist das Gate auf dem Merge-Stand (Statuszeile).

## 4 Offen

In der Testdatenbank trägt `DIN4708.p_b` die Einheit „Personen“, `TwwParameterschluessel` erwartet „P“ —
ein Import mit der Einheit der Datenbank würde benannt abgelehnt. Nicht geändert (Testdatenbank nur über
Saatskripte); Korrektur mit der nächsten Saat des Testkatalogs.

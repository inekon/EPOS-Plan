# Zapfprofilgenerator: Schnellauslegung nach Vorlage V4 in der Stufe „Einfach“ (16.10.2026)

## Anlass

Anwenderauftrag 16.10.2026 „Fahre fort mit #1096“: Auch mit dem Kaltwasser im freien Paketteil brach die Speicherauslegung der Stufe „Einfach“ ohne Normpaket ab, weil die Werte `A100.Kaltwasser.Auslegung` und die DIN-4708-Kennwerte fehlten.

## Entscheid

Anwenderentscheid „Variante 1“: In der Stufe „Einfach“ gilt ohne die A100-Auslegungswerte der Nenninhalt des V4-Bandes als Empfehlung; mit A100-Werten bleibt die Summenlinie; in „Erweitert“ und „Experte“ bleibt die Empfehlung ohne A100 offen.

## Umsetzung

Sechs Commits `a7e9ff02e` bis `f54ef607f`.

- `A100.Kaltwasser.Auslegung` = 10 °C im freien Paketteil (INEKON-Vorlage V4, Berechnung Z. 389), in bestehende Datenbanken nachgetragen.
- Fehlen die DIN-4708-Kennwerte, bricht die Speicherauslegung nicht ab; der Normvergleich gilt benannt als „nicht rechenbar“.
- Schnellauslegung: Hinweis „Schnellauslegung nach Vorlage V4, ohne Summenlinie“, Eintrag im Herkunftsprotokoll, im Dialog die Marke „Schnellauslegung V4“.
- Beispiele mit Bezugsmenge 3: Einfamilienhaus 200 l, Wohnen groß 200 l, Hotel 400 l, Büro 100 l.
- Konzept Zapfprofilgenerator 4.5 und Wiki-Quelle Brauchwasser-Zapfprofil fortgeschrieben.

## Nachweise

Gate #1101 auf `a000cc6f0` grün (Linux, Exit 0): Tests EPOS.Kern 14 328/0, EPOS.UI 9 562/0, KiKern 553/0, SpeicherEngine 397/0, SpeicherPlanung 27/0, Dokumentationswachen 35/0, ChartProben 225 Hashes gleich, Referenzlauf 37/37 PASS (1045 und 1066 bis 1071 PASS, 1071 unverändert), Testdatenbank byte-gleich. Windows-Schale und `Werkzeuge/Testdatenbankschema` 0 Fehler. Statuszeile #1107.

## Offen

Sichtabnahme unter Windows; Wiki-Upload der Seite Brauchwasser-Zapfprofil und zweier Logbuch-Sätze (Entwurf „Nachtrag zu Version 1.2.0.11“ in `Dokumentation/aktuell/Wiki_Update_2026-09-26.md`), Version beim Anwender erfragen.

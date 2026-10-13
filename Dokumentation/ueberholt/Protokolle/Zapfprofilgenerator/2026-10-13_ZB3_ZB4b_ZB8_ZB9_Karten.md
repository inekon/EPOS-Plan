# Zapfprofil: Zonenblock als Karten, Gebäude und Anlage, ein Feld je Wert (W4, 13.10.2026)

Protokoll der Welle **W4** der Sitzung „Zapfprofilgenerator Cloud“ zu den Vorschlägen **ZB3**, **ZB4 (b)**,
**ZB8** und **ZB9** und dem Entscheid **ZB‑Q1 A** des
[Konzepts Zapfprofil — Benutzerführung](../../../aktuell/Konzept_Zapfprofil_Benutzerfuehrung_EPOS-Plan.md)
(Befunde B3, B4, B5, B9, B10).

**Rahmen.** Worktree `agent-a0be788a1232d9e28`, Zweig `worktree-agent-a0be788a1232d9e28`, Basis ist der Stand
der Welle W3b (`10b319e6a`), Opus 5.5. Zwei Commits `e0b82e755`, `bf13fbc67`. Kein Schemaschritt, kein
Rechenweg.

---

## 1 Zonenblock als Karten (ZB3, ZB‑Q1 A)

Unter der Zonenliste steht der Block der markierten Zone als Karten mit Zusammenfassungszeile der wirksamen
Werte: *Nutzung* (offen), *Belegung*, *Kalender*, *Messwert*, *Tagesbedarf*, in der Stufe Experte *Fachwerte der
Zone*. Auf und Zu je Karte hält der Dialog über Zonen- und Stufenwechsel. Der Kopf des Zonenblocks nennt den
Jahresbedarf der Zone in MWh/a.

## 2 Gruppe „Gebäude und Anlage“

Eigene Gruppe mit Zirkulation, Ladeleistung samt Ladefenster, Leitungsinhalt, Fachwerten des Gebäudes sowie
Stochastik (Jahresreihe) und Typtagweg; der Titel des Typtagwegs steht links.

## 3 Haftende Vorschau

Die Vorschau haftet in der rechten Spalte; die Anordnung folgt der Breite des Dialogs über
`@container zapfprofil (min-width: 800px)`.

## 4 Ein Feld je Wert (ZB4 (b))

Baustein `Einwertfeld`: Tagesbedarf, Ladeleistung und Zirkulation je **ein** Feld — leer = auto, der Vorschlag
steht als Platzhalter, „Vorschlag übernehmen“, der Rechenweg im Aufklapper. Arbeitsstand, Rechenweg und
Speichern bleiben unverändert. Die Moduswahl auto/manuell entfällt als Eingabe; die KI-Felder der drei Modi
entfallen, die Manuell-Felder bedeuten „leer = auto“.

## 5 Texte und Hinweise (ZB8)

Vorgaben und Hinweise stehen im `title` (Auswahlfeld, Optionsgruppe). Die Hinweise der Stufe Einfach zeigen nur
das, was der Anwender dort beheben kann: Die Regel steht einmal in `ZapfprofilHuelle.ErstAbErweitert` (nur der
Hinweis zur Bandbreite des Niveaus bleibt in Einfach), gesetzt als `AbErweitert` an Warn- und Meldungsdaten.

## 6 Katalog als Blatt (ZB9)

Die Katalogtabelle verlässt den Zonenblock; der Knopf „Katalog…“ öffnet sie als Blatt, nur lesend, eine Zeile
wählt.

## 7 Ressourcen und Wiki

Neu 19 Schlüssel (`ZPG_GRP_GEBAEUDE_ANLAGE`, `ZPG_KARTE_*`, `ZPG_SUMME_*` der Zusammenfassungen,
`ZPG_HINW_EINWERT`, `ZPG_BTN_KATALOG`, `ZPG_KATALOG_BLATT_HINWEIS`), entfernt 7 (`ZPG_GRP_SCHAETZHILFEN`,
`ZPG_HINW_SCHAETZHILFEN`, `ZPG_LBL_TAGESBEDARF_MANUELL`, `ZPG_HINW_MANUELL` und drei KI-Erläuterungen der
Modusfelder); beide Sprachen, Designer neu erzeugt. Wiki-Quelle „Brauchwasser-Zapfprofil“: Eingaben in zwei
Ebenen mit Karten, Katalog als Blatt, ein Feld je Wert, Hinweise der Stufe Einfach, Messwert- und
Stochastikkarte.

## 8 Tests und Abnahme

Neu `EPOS.UI.Tests/Dialoge/ZapfprofilDialogStufenTests.Karten.cs` samt Hilfsklasse `ZapfprofilKarten`, ein
Fall in `ZapfprofilHuelleZ4Tests` (`ErstAbErweitert`); angepasst die Stufen-, Editoren- und
Dialogtests sowie die KI-Maskenabdeckung. Ergebnisse im Worktree, soweit die Commit-Botschaften sie nennen;
maßgeblich ist das Gate auf dem Merge-Stand (Statuszeile).

## 9 Offen

Fensterprobe und Sichtprobe der drei Stufen (Höhe der Stufe Erweitert unter der Hälfte von heute) stehen nach
den Commit-Botschaften nicht fest und sind auf dem Merge-Stand nachzuweisen.

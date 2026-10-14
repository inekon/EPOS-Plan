# Zapfprofil: Referenzprojekte 1066 bis 1071 und Basis R53 (W7, 13.10.2026)

Protokoll der Welle **W7** der Sitzung „Zapfprofilgenerator Cloud“ zu dem Vorschlag **ZB12** des
[Konzepts Zapfprofil — Benutzerführung](../../../aktuell/Konzept_Zapfprofil_Benutzerfuehrung_EPOS-Plan.md)
(Regressionsnetz).

**Rahmen.** Worktree `agent-a3a8c2c790f640f78`, Zweig `worktree-agent-a3a8c2c790f640f78` auf `bf13fbc67`
(Welle W4), Opus 5.5. Acht Commits `acf6ed917`, `aa81b4538`, `e0f9d29bf`, `f80b6c009`, `594766e8c`,
`b6cb1b8fa`, `4bf178d93`, `4a24b2aa5`. Kein Schemaschritt, kein Rechenweg. Entscheide des Anwenders: alle sechs
Optionsgruppen, alle sechs in der CI.

---

## 1 Die sechs Referenzprojekte

Je eine Kopie von 1045 (Anlage, Gebäude, Klima), an der Kopie das Zapfprofil einer Optionsgruppe:

| Projekt | Gruppe | Inhalt |
|---|---|---|
| 1066 | Mischnutzung | Zonen Wohnen (Wohnungstabelle, Belegung nach Raumzahl aus dem Katalog), Büro, Hotel, Schule; Zirkulation nach Leitungslänge, aufgeteilt auf die Zonen mit Zirkulation |
| 1067 | Kalender | Zone Schule mit dem Kalender der Nutzungsart und fünf eigenen Ferienzeiträumen (einer über den Jahreswechsel), Zone Büro an das Gebäude gebunden; Feiertagsland und Wochenmaske am Gebäude |
| 1068 | Stochastisch | Zone Wohnen wie 1045, Jahresreihe stochastisch mit festem Seed und Realisierungen |
| 1069 | Typtage | Jahresgang über eingespielte Typtage; der Typtagsatz ist **erfunden** (Quelle „Referenztyptage (fiktiv)“, runde Werte, kein Wert einer Richtlinie) |
| 1070 | Kalibrierung und manuelle Werte | Zone mit Jahresmesswert, Zone mit manuellem Tagesbedarf, eigener Zapftemperatur und Kaltwasser, Zirkulation manuell |
| 1071 | Auslegung | je eine Zone mit Topologie Speicher, Frischwasserstation, Durchfluss und Wohnungsstation; die Auslegung wirkt nicht auf die Simulation |

Alle Katalogzeilen stammen aus dem Testkatalog TEST-1. Die Wochenmaske von 1067 trägt nur den Sonntag (64): Die
Vorgabe Sa + So (96) speichert der Gebäudedialog als leeres Feld, ein gesäter Wert soll aber als Wert stehen; den
Samstag lässt der Wochengang der Nutzungsart Büro ohnehin ohne Zapfung, sichtbar wirkt das Feiertagsland.
1068 und 1069 treffen die Jahresmenge von 1045, ihre Stundenreihen unterscheiden sich davon.

## 2 Saatskript

`Referenzlaeufe/Skripte/referenzprojekte_zapfprofil_optionen.cs` (Dateiskript, `dotnet run`): prüft die
Vorbedingungen, kopiert 1045 sechsmal auf dem Kopierweg des Programms, spielt den erfundenen Typtagsatz über den
Importweg des Kerns ein, speichert die Zapfprofile über den Schreibweg (`ZapfprofilCtrl.Speichern`), rechnet
jedes Projekt über den Brauchwasserweg und 1071 die Auslegung und prüft `integrity_check` und
`foreign_key_check`. Auf derselben Datei nicht wiederholbar (benannter Abbruch, Rückgabe 2); mit `--trocken`
nur die Vorbedingungen. Dafür sind Interna des Kerns für das Skript freigegeben.

## 3 Basis R53

`Referenzlaeufe/2026-10-13_R53_Zapfprofiloptionen`: **36 Projekte** (die dreißig der Basis R52 und 1066 bis
1071). Vergleichsaussage gegen R52 nach den Commit-Botschaften: Die dreißig Projekte von R52 rechnen
**byte-gleich**, die Basis bleibt in allen Reihen byte-gleich, nur ihr Protokoll ist neu; die sechs neuen
Projekte liefern je 32 CSV-Dateien und 201 Skalare, insgesamt 1171 CSV und 7983 Skalare. Erfolgreich 36 von 36
im Lauf des Protokolls. R52 ist nach `Dokumentation/ueberholt/Referenzbasen/` gewandert; `Referenzlaeufe/LIESMICH.md`
beschreibt R53 und die Projekte, die Einfrierregel „gesäte Zapfprofil-Eingaben“ in `CLAUDE.md` und
`LIESMICH.md` umfasst nun auch die Zonenferien und den erfundenen Typtagsatz.

## 4 CI

`kern.yml` rechnet und vergleicht **18 Projekte**: 1030, 1007, 1017, 1045, 1046, 1047, 1049, 1051, 1058, 1060,
1063, 1065 und 1066 bis 1071 (auch im gestörten Lauf); `ios.yml` und die Basisverweise zeigen auf R53.

## 5 Wache und nachgezogene Zählungen

Neue Wache `EPOS.Kern.Tests/ZapfprofilOptionenReferenzprojekteWacheTests`: Projektzeilen, Zonen, Wohnungstypen,
Ferien, Gebäudekalender, der erfundene Typtagsatz, die Generator-Bilanz je Projekt und Zone samt Stundenreihe
der Basis und die Auslegung von 1071.

Nachgezogen (Zählungen und Ausnahmelisten der sechs Kopien): `GebaeudeKatalogverweisTests` (Gebäude 51 statt 45,
Katalogverweis 47 statt 41), `PreisbasisSchrittTests` (Preisbasis „Nm³“ 31 statt 25),
`BaualtersklassenSchemaTests`, `KesselBrennwertNachzugTests`, `ZapfprofilWeicheTests` (Projekte mit Generator),
`ZapfprofilSpeichernTests`, `ZapfprofilTyptagwahlTests`, `ZapfprofilHuelleTyptageTests`, `TwwTyptagCtrlTests`,
`Typtagpaketbauer` (`OhneTyptage`), `ProzessNutzungTests` sowie kleine Nachzüge in den Wachen von 1059, 1062, 1064
und 1065; `Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py` nimmt die sechs Projekte auf.

## 6 Tests und Abnahme

Im Worktree: Kern-Filter 0 Fehler, `SqlDialektPruefer` ohne Fundstelle, `EPOS.UI.Tests` 9 352 grün, die neue Wache
23 Fälle grün; `EPOS.Kern.Tests` in Buchstabenblöcken (`Werkzeuge/Gate/kern_bloecke.sh`) grün bis auf je einen Fall in
den Blöcken AB (Baualtersklassen) und K (Kesselnachzug), beide als Zählung der sechs Kopien nachgezogen und gezielt
nachgeprüft. Zwei frische Läufe aus zwei getrennten Saatläufen sind byte-gleich; der gestörte Lauf (`--stoerung ulp`)
ist GESAMT PASS, keine der neuen Projekte weicht dort ab. Maßgeblich ist das Gate auf dem Merge-Stand (Statuszeile).

## 7 Integration

Die Testdatenbank war auf dem Zweig nicht committet. Auf dem Integrationsstand ist sie aus der Fassung 223
(`05b14e07…`) mit dem Schemawerkzeug auf 224 gehoben und mit dem Saatskript neu gesät (`integrity_check` ok,
`foreign_key_check` leer, 108 412 928 Byte, `6ef7aa68…`); der Lauf der 36 Projekte rechnet gegen R53 GESAMT PASS und
in allen CSV byte-gleich — R53 bleibt, wie eingefroren.

## 8 Offen

Die Sitzung K6 legt mit R55 das Projekt 1072 an; dann sind die festen Listen in `Ak3KReferenzprojektWacheTests`,
`ZonenKuehlkurveReferenzprojektWacheTests` und den Zählungstests um 1072 zu erweitern, und das Saatskript dieser
Welle setzt 1065 als höchste Projekt-ID voraus (auf einer Datenbank mit 1072 läuft es nicht mehr). In
`Referenzlaeufe/LIESMICH.md` endet die Liste „Entfernte Basen“ bei R47.

# Protokoll — Katalogimport der Bedarfstage und Parameter (ZU30 bis ZU33)

**Gegenstand.** Der Anwender-Katalogimport der Brauchwasser-Nutzungsarten nimmt über die vier
Dateien des Nutzungsartkatalogs hinaus drei weitere an: `Tab_TwwBedarfstag_STAMM.csv`,
`Tab_TwwBedarfstagEreignis_STAMM.csv` und `Tab_TwwParameter_STAMM.csv`. Damit ist der letzte
Folgeposten aus N13 (s) und N18 (c) geschlossen.

**Rahmen.** Worktree `zi`, Zweig `zi` von `822ba803`, ein Agent mit `model: opus`. **Kein
Schemaschritt** — die Tabellen stehen seit Schritt 103 (T1) beziehungsweise Schritt 124 (T3, Spalte
`Bezugsart`); die Testdatenbank ist unberührt (Schemastand 143, nächster freier Schritt 144).
Referenzbasis `2026-09-25_R16_Anlagenprio`.

Die ausführliche Fassung mit Regeln, Abweichungen und Folgen steht als **Nachtrag N20** im
[Umsetzungskonzept Zapfprofilgenerator](../../../aktuell/Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md);
dieses Protokoll hält den Ablauf und die Nachweise.

## 1 Anwenderentscheide 25.09.2026

| Nr. | Frage | Entscheid im Wortlaut | Umsetzung |
|---|---|---|---|
| ZU30 | Was ist eine Dublette bei einem Bedarfstag? | „importierter Bedarfstag darf eine vorhandene Auslieferungszeile ersetzen. Hinweis geben." | ersetzt am Platz, gleiche `ID`, zwei Hinweise |
| ZU31 | Was tut der Import mit einem vorhandenen Parameter? | „Import ersetzt den Wert. Hinweis geben" | Wert, Einheit und Provenienz ersetzt, Sammelhinweis zur Wirkung |
| ZU32 | Wie berichtet der Import? | „je Tabelle eigene Zeilen mit Ergebnis und Grund" | drei Gruppen im Bericht und im Dialog, Prüflauf mit „würde …" |
| ZU33 | Welche Prüfung? | „Empfehlung" | Wertemengen, Tagesfenster, Energiesumme, lückenlose Reihenfolge, Parameterschlüssel |

## 2 Ablauf

1. **Liste der gelesenen Parameterschlüssel** (`c8fa1c3d`): `TwwParameterkatalog` in
   `EPOS.Kern/Allgemein/Zapfprofil/TwwParameterschluessel.cs` — 69 Einträge, gespeist aus den
   Konstanten von `ZapfAuslegungParameter`, `ZapfParameter` und `ZapfStochastikParameter`; vier
   Einträge sind Vorsätze einer Familie. Wache
   `EPOS.Kern.Tests/TwwParameterschluesselWacheTests` (fünf Fälle).
2. **Kern des Imports** (`202dccba`): neuer Teil `TwwNutzungsartCtrl.ImportKatalogzeilen.cs`,
   `IMPORT_TABELLEN` um die drei Tabellen erweitert, `TwwImportausgang.Ersetzt`,
   `TwwImportbereich`, Prüflauf `Importieren(dateien, pruefen)`; 17 Muster
   `ZPG_SATZ_KATALOGIMPORT_…` in beiden Sprachen.
3. **Oberfläche und Hülle** (`77035032`): Bericht in Gruppen, Schalter „Nur prüfen, nichts
   schreiben", zwölf Beschriftungen `ZPGK_IMPORT_…` in beiden Sprachen.
4. **Tests** (`692f321a`): Probepaket um die drei Dateien ergänzt, je Regel ein Fall,
   Testhelfer `TwwTestdatenbank.BedarfstagAnlegen`.
5. **Wiki-Quelle und Paketvorlage** (`5750316f`).
6. **Zwei Merges von `origin/ios_migration_september`:** `d323d42f` (`bf129170`) vor den Papieren —
   Konflikte allein in den beiden `.resx`, weil beide Seiten am Ende angefügt hatten (beide Blöcke
   übernommen, Designer neu erzeugt) —, und `8c0f3724` (`9e356b89`) danach, als der parallele Posten
   #516 gepusht war: dort dazu die vier Papiere, alle inhaltlich zusammengeführt (Nachtrag N19 vor
   N20, Kapitel-9-Zeilen ZU25–ZU29 vor ZU30–ZU33, Statuszeile #517 hinter #516, Protokollzahl des
   Index auf 11, Logbuch-Satz hinter dem von #516). Die Wiki-Quelle der Seite führte beide
   Abschnitte von selbst zusammen (35 Anker).

## 3 Gates

| Gate | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler (vor und nach dem Merge) |
| `dotnet test WP-Plan.Kern.slnf -c Release` (voller Lauf, xUnit seriell) | 0 Fehler, 14 643 erfolgreich, 2 übersprungen |
| gefilterte Tests nach beiden Merges (Katalogimport, Tww, Zapfprofil, Parameter, Auslieferung, Vorlage, KiMasken, Doku- und Wiki-Wachen, Nachtzeit, ZapfSätze) | 0 Fehler, 1 876 bzw. 1 932 erfolgreich |
| Windows-Schale `-p:EnableWindowsTargeting=true` | 0 Fehler |
| `Werkzeuge/SqlDialektPruefer` gegen `Kenndaten_Test.sqlite` | 1 932 SQL-Texte, **0 Fundstellen** |
| `Werkzeuge/Auslieferungsvorlage.Tests` | 0 Fehler, 36 erfolgreich |
| Referenzlauf 1030, 1007, 1017, 1045, 1046, 1047 gegen `2026-09-25_R16_Anlagenprio` | **GESAMT: PASS** (2 208 587 Werte) |
| `ResourceDesigner` (nur prüfen) | unverändert, wiederholbar |
| Arbeitsbaum | sauber, keine Konfliktmarker |

## 4 Nachweise der Regeln

Je Regel ein Fall in `EPOS.Kern.Tests/TwwKatalogimportTests` (18 neue Fälle, davon zwei
`[Theory]` mit zusammen 17 Zeilen):

- **angelegt** — Bedarfstage mit Ereignissen in ihrer Reihenfolge, Parameter mit Wert und Einheit,
  Stand `IMPORT`, `ReadOnly` 0, Herkunftsart `IMPORT` außer `FREI` und `FIKTIV`; Reihenfolge des
  Berichts Bedarfstage, Parameter, Nutzungsarten;
- **gleich vorhanden** — dasselbe Paket zweimal, nichts doppelt;
- **ersetzt am Platz** — gleiche `ID` vor und nach dem Import, Ereignisse vollständig ersetzt,
  keine Version „(Import n)";
- **Auslieferungszeile ersetzt** — beide Hinweise, Stand und `ReadOnly` der Zeilen danach;
- **Parameter ersetzt** — alter und neuer Wert im Grund, Wirkungshinweis, nur eine Zeile;
- **Parameter abgelehnt** — unbekannter Schlüssel, abweichende Einheit, Wert außerhalb des
  Bereichs; die übrigen Einträge kommen trotzdem;
- **Ereignis ohne Bedarfstag** — Paket als Ganzes abgelehnt, nichts geschrieben;
- **Formprüfung** — zehn Zeilen am Bedarfstag, sieben an den Ereignissen, Energiesumme, doppelter
  Eintrag im Paket, fehlende Ereignisdatei;
- **Prüflauf** — dieselben Zeilen, der Katalog bleibt Zeile für Zeile, wie er war;
- **fehlende Tabellen** — die drei Dateien benannt übergangen, die Nutzungsarten kommen.

Dazu in `EPOS.Kern.Tests/ZapfprofilHuelleKatalogdialogTests` der Bericht der Hülle in drei Gruppen
samt Prüflauf und in `EPOS.UI.Tests/Dialoge/TwwNutzungsartAdminDialogTests` die drei
Gruppentabellen und der Schalter.

## 5 Folgen

| Gegenstand | Wer | Wann |
|---|---|---|
| Wiki-Seite „Brauchwasser-Zapfprofil" hochladen; Logbuch-Satz „Der Katalogimport für Brauchwasser nimmt auch Bedarfstage und Parameter an und ersetzt vorhandene Werte mit Hinweis." | Anwender (Sammel-Upload) | nächster Upload |
| Sichtabnahme des Katalogdialogs unter Windows (Gruppen des Berichts, Schalter „Nur prüfen") | Anwender | nach dem Push |
| Bereiche der Parameterliste fachlich durchsehen — sie sind Rahmen gegen Zahlendreher, keine Fachgrenzen | Anwender | mit ZU21 |

# V9, V10, Ladefenster und GLF-Grenze, Hotel-Durchsicht: Welle #561 (26.09.2026)

Protokoll des Postens **#561**. Anwenderentscheide vom 26.09.2026, wörtlich „1 bis 3: Bestätigt“
— (1) V10 eigene Quantile der Spitzenstreuung, (2) V9 Zirkulation in den Ensemblespitzen, (3)
Ladefenster-Beginn 22 h und GLF-Gültigkeitsgrenze 30 als Auslieferungswerte — und „5.
Hotel-Modellannahmen: ausführen, keine .eml anfragen“. Umsetzungskonzept Zapfprofilgenerator,
Nachtrag N33; Kapitel 9 ZU21, ZU36, K5; Validierungsbericht Abschnitt 9; Prüfliste ZU21.

**Rahmen.** Worktree `zv910`, Zweig `zv910` von `4d4131ff4` (Schemastand 150, Testdatenbank
`41343bce`), Opus 5.5; die Hotel-Durchsicht als eigener Opus-Agent (nur Analyse, Protokoll, Regeltext).
Kein Schemaschritt, kein Push.

---

## 1 Parameter

| Schlüssel | vorher | nachher | Provenienz nachher |
|---|---|---|---|
| `Zapfprofil.Validierung.Streuung.Unten` | — (die Streuung nahm `…Band.Unten`) | **0,85** (Einheit „-", Bereich 0 … 1) | „Anwenderentscheid 26.09.2026 zu Folge V10 des dritten Validierungslaufs", `EIGENKONSTRUKTION` |
| `Zapfprofil.Validierung.Streuung.Oben` | — (die Streuung nahm `…Band.Oben`) | **0,95** | wie oben |
| `Speicherauslegung.Ladefenster.Beginn` | 22 h, fiktiver Testwert (`FIKTIV`), nicht ausgeliefert | **22 h**, ausgeliefert | „INEKON-Setzung, Anwenderentscheid 26.09.2026 (die Vorlage V4 führt keinen Beginn)", `EIGENKONSTRUKTION` |
| `Speicherauslegung.GLF_Gueltigkeitsgrenze` | 30, fiktiver Testwert, nicht ausgeliefert | **30**, ausgeliefert | „INEKON-Setzung, Anwenderentscheid 26.09.2026 (die Vorlage V4 führt keine Grenze)", `EIGENKONSTRUKTION` |

Quelle aller vier ist `Referenzlaeufe/Skripte/zapfprofil_setzungen_inekon.json` (Kopfregel
fortgeschrieben); die zwei Speicherauslegungszeilen fallen aus der Liste `PARAMETER` des fiktiven
Testkatalogs in `tww_testkatalog_fiktiv.py`. `speicherauslegung_v4.json` führt sie weiter unter
„offen" (V4 hat keinen Wert), ergänzt um den Verweis auf die INEKON-Setzung. Das Verhalten eines
Pakets ohne `…Ladefenster.Beginn` bleibt: `PARAMETER_SCHLUESSEL_FEHLT`.

## 2 Testdatenbank und Paketteil — die Befehlsfolge

Kein Schemaschritt (151 bleibt frei). Die Folge setzt sich auf jede spätere Fassung der Testdatenbank
neu auf (die Nachbarsitzung friert in Kürze R22 auf ihrer Fassung ein):

```
py Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py Referenzlaeufe/Kenndaten_Test.sqlite --paketteil-schreiben
py Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py Referenzlaeufe/Kenndaten_Test.sqlite
```

(mit `PYTHONIOENCODING=utf-8`; Quellen: die beiden JSON-Dateien und das Skript aus Commit
`0cfc33f2e`).

| Schritt | Ergebnis |
|---|---|
| Ausgang | Fassung `41343bce` (70 692 864 Byte, Schemastand 150) |
| erster Lauf | Paketteil: 1 von 4 Dateien neu (`Tab_TwwParameter_STAMM.csv` 38 → 42 Zeilen); Datenbank „2 Zeile(n) angelegt, 2 nachgeführt" |
| zweiter Lauf | „0 Zeile(n) angelegt, 0 nachgeführt" |
| Zellvergleich gegen die Ausgangsfassung | nur `Tab_TwwParameter_STAMM` (94 → 96): ID 55 und 56 (Ladefenster-Beginn, GLF-Grenze) Quelle, Ausgabe, Version, Herkunftsart `FIKTIV` → `EIGENKONSTRUKTION`, Werte 22 und 30 unverändert; ID 95 und 96 neu (Streuungsquantile); dazu `sqlite_sequence` dieser Tabelle. Keine andere Tabelle |
| integrity_check / foreign_key_check | ok / 0 |
| LFS | Zeiger `48da43e5`, Filter aktiv, keine `-shm`/`-wal`; nach dem Merge auf R22 `09b6c523` (Abschnitt 7) |

Kein Referenzprojekt benutzt die vier Zeilen: Projekt 1045 rechnet eine Bilanz, keine Auslegung und
keinen Messvergleich. Die Einfrierregel „gesäte Zapfprofil-Eingaben" ist nicht berührt; `CLAUDE.md`
bleibt unverändert.

## 3 Kern, Hülle, Dialog, Werkzeug

| Teil | Dateien | Änderung |
|---|---|---|
| Kern | `EPOS.Kern/Allgemein/Zapfprofil/Messvergleich.cs`, `Zapfprofileingang.cs`, `TwwParameterschluessel.cs` | V10: `Messvergleichseingang.StreuungUnten/Oben` (0,85 / 0,95), `AusParametern` liest die zwei Schlüssel, `Streuung` rangiert mit ihnen, `Spitzenstreuung.PerzentilUnten/Oben` tragen sie; Abbruch `MESSVERGLEICH_STREUUNG_UNGUELTIG` |
| Kern | `Jahresensemble.cs`, `ZapfprofilErgebnis.cs`, `ZapfprofilRechner.cs` | V9: `TagesstundenspitzenKw` je Realisierung (24 Werte), durchgereicht bis `ZonenErgebnis`; `Jahresensemble.SpitzenMitZuschlag(tagesstundenspitzen, streckung, zuschlag)` = `max_h (a · M_r[h] + Z[h])`, nicht tagesperiodischer Zuschlag → `ArgumentException` |
| Ressourcen | `Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` | `ZPG_SATZ_/ZPG_WARN_MESSVERGLEICH_STREUUNG_UNGUELTIG`; `ZPG_KZ_SPITZENSTREUUNG` mit „(P{0}/P{1})" |
| Hülle | `EPOS.UI.Daten/Bedarf/ZapfprofilHuelle.Messvergleich.cs` | Stichprobe mit `ZapfprofilErgebnis.Zirkulation` als Zuschlag (die verglichene Reihe ist Zapfung plus Zirkulation); DTO-Felder der Quantile; Kennung in `VALIDIERUNGSHINWEISE` |
| Dialog | `ZapfprofilDialog.razor`, `ZapfprofilDaten.cs`, `ZapfprofilTexte.cs` | Zeile „Streuung der Realisierungsspitzen (P85/P95)" |
| Werkzeug | `Objektlauf.cs`, `Objektbefund.cs`, `Bericht.cs`, `Bandanalyse.cs` | `Zirkulationsteil` als eine Stelle für Vergleich, Kalibrierung und Spitzen; `Spitzen(e, faktor, zirkulation)`; Berichtszeile „Quantile der Spitzenstreuung" |
| Tests | `MessvergleichTests` (+2), `ZapfensembleTests` (+2, eine Bitprobe erweitert), `ZapfprofilStochastikTests` (erweitert), `ZapfprofilHuelleMessreihenTests` (erweitert), `ZapfprofilVergleichDialogTests` (erweitert), `Werkzeuge/ZapfprofilValidierung.Tests/SpitzenstreuungTests` (+2) | |
| Wiki | `Projekte/Wiki/Programm Dokumentation - Brauchwasser-Zapfprofil.wiki` | Streuungszeile mit P85/P95 und Zirkulation; Hotel: Bezugsmenge Zimmerzahl, Stufen nach Bedarf je Zimmer; Tabuwortprüfung ohne Treffer |

**Warum die Spitze je Tagesstunde genügt.** Die Zirkulationsreihe entsteht in
`Zirkulationskanal.Reihe` als Leistung mal Laufzeitfenster, an allen 365 Tagen gleich. Für eine
tagesperiodische Reihe Z und a ≥ 0 gilt `max_t (a · s_r[t] + Z[t]) = max_h (a · max_d s_r[d, h] + Z[h])`
— exakt, ohne die 8 760 Stunden jeder Realisierung aufzuheben. Das ist dieselbe Addition wie bei der
verglichenen Jahresreihe; die Probe `ZapfprofilStochastikTests` hält sie bitgleich gegen
`(Zapfung + Zirkulation).GroessterStundenwertKw` der Realisierung zum Seed.

**Die Wahl des Trennzeichens.** Die Bandzeile heißt „(P95–P99,9)"; die Streuungszeile nimmt
„(P85/P95)", weil der Strich in ihr das Zeichen „ohne Wert" ist (bunit-Probe
`Mit_Ensemble_steht_die_Spitzenstreuung_als_Zahl`).

## 4 Hotel-Durchsicht

Ausgeführt von einem Opus-Agenten gegen die Messreihen außerhalb des Repositoriums; Protokoll
[Hotel-Durchsicht](2026-09-26_Hotel_Durchsicht_Modellannahmen.md). Alle fünf Annahmen haltbar,
Kennwerte unverändert; Wortlaut der Regel und der Modellannahmen geschärft (Stufen nach Bedarf je
Zimmer, nicht nach Hotelgröße; Bezugsmenge ist die Zimmerzahl). Offen: der Jahresgang (die Datenbasis
trägt keinen) und der Vorschlag, den Namen um „je Zimmer" zu ergänzen (Paketänderung,
Anwenderentscheid).

## 5 Vierter Validierungslauf (V9, V10)

`dotnet run --project Werkzeuge/ZapfprofilValidierung -c Release -- C:\Waermeplan\Messreihen_extern\konvertiert\objekte --ziel <Scratch> --katalog <Kopie der neu gesäten Testdatenbank>`;
zehn Realisierungen, feste Saat, 21 Objekte, 44 Berichtsdateien, Berichtswache ohne Fund. Der
Paketteil allein taugt nicht als Katalog des Laufs — ihm fehlen die Grundparameter (etwa
`Kaltwasser.Bilanz.MonatMaximum`, `Zirkulation.Laufzeit`), die die Testdatenbank fiktiv führt; wie in
#553 rechnet der Lauf deshalb gegen die gesäte Testdatenbank, die den Paketteil enthält.

| Zählung | dritter Lauf | vierter Lauf |
|---|---|---|
| Objekte grün / gelb / rot | 3 / 0 / 18 | 3 / 0 / 18 |
| Band | 8 / 10 / 3 | 8 / 10 / 3 |
| Form | 6 / 0 / 15 | 6 / 0 / 15 |
| Energie | 21 / 0 / 0 | 21 / 0 / 0 |
| √N (11 belastbare Objekte) | +0,44 | +0,44 |
| Streubreite | überall 1 | 1,001 … 1,152, bei allen 21 über 1 |

Einzelheiten im Validierungsbericht, Abschnitt 9.

## 6 Datenanfragen

Die drei vorbereiteten Anfragen an fremde Datenhalter liegen außerhalb des Repositoriums und sind
**nicht versandt** (Anwenderentscheid 26.09.2026); nicht gelöscht. Vermerkt im Validierungsbericht
(8.6, 9.4) und im Konzept (N33 (f)). K5 bleibt beim Anwender.

## 7 Gates

| Prüfung | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler (vor und nach jedem Merge) |
| gefilterte Tests (Messvergleich, Zapf, Tww, Auslegung, Parameter, Vorlage, Wachen) | erster Lauf: 1 Rot — `ZapfprofilWeicheTests.Der_Testkatalog_traegt_jeden_Parameter_des_Rechenwegs` zählte 24 Schlüssel; auf 26 nachgezogen, dazu der Fall `Ladefenster_Beginn_und_GLF_Grenze_sind_INEKON_Setzungen_des_Paketteils` |
| voller Lauf `WP-Plan.Kern.slnf` (vor dem R22-Merge) | Kern 8407 (+1 übersprungen), UI 6727, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (+1) — 0 Fehler |
| voller Lauf nach dem Merge auf R22 (Testdatenbank neu aufgesetzt) | Kern 8432 (+1), UI 6738, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (+1) — 0 Fehler |
| nach dem Merge #564 (Photovoltaik, keine Testdatenbank) | Kern-Filter und Schale 0 Fehler; gefilterte Tests Kern 659, UI 252 grün; Referenzlauf 7/7 PASS |
| `Werkzeuge/ZapfprofilValidierung.Tests` | 38 / 38 (zwei neue Fälle `SpitzenstreuungTests`) |
| `Werkzeuge/Auslieferungsvorlage` (Paketteil mit 42 Parameterzeilen) | 38 / 38 |
| Windows-Schale `WindowsFormsApplication1` | 0 Fehler |
| `SqlDialektPruefer` | 1997 SQL-Texte, 0 Fundstellen |
| `designer_neu.py` | wiederholbar, +0 (der Designer schreibt LF; auf CRLF zurückgesetzt) |
| Referenzlauf 1030, 1007, 1017, 1045, 1046, 1047 gegen `2026-09-26_R21_BhkwDeckung` | 6 / 6 PASS |
| nach dem Merge: 1030, 1007, 1017, 1045, 1046, 1047, 1049 gegen `2026-09-26_R22_Solarthermie` | 7 / 7 PASS |

**Merge auf R22.** Die Nachbarsitzung hat während der Welle R22 eingefroren (Referenzprojekt 1049,
Testdatenbank `14de1c9b`). Beim Merge wurde deren Fassung genommen und die Befehlsfolge aus
Abschnitt 2 neu aufgesetzt: wieder „2 angelegt, 2 nachgeführt", zweiter Lauf 0 / 0, Zellvergleich
wieder allein `Tab_TwwParameter_STAMM` (dieselben IDs 55, 56, 95, 96) und `sqlite_sequence`;
71 557 120 Byte, LFS `09b6c523`.

**Parallele Testläufe.** Nachbarsitzungen ließen fast durchgehend eigene Testläufe laufen; die
vollen Läufe dieser Welle starteten jeweils erst in einer Lücke. Die zwei kurzen Werkzeugtestläufe
(je rund eine Sekunde) liefen neben einem fremden Testlauf.

## 8 Folgen

| Nr. | Gegenstand | Wer | Wann |
|---|---|---|---|
| Hotel | Name oder Hilfe sagt, dass die Bezugsmenge die Zimmerzahl ist — Paketänderung | Anwenderentscheid | offen |
| Hotel | Jahresgang aus einer eigenen Hotelmessung über ein Jahr | Anwender (K5) | nach der Freigabe |
| K5 | Eigene, freigegebene Objekte | Anwender | nach der Freigabe |
| Merge | Beim Merge auf R22 der Nachbarsitzung die Befehlsfolge aus Abschnitt 2 auf deren Testdatenbank neu aufsetzen | Orchestrierung | mit dem Merge |
| Wiki | Logbuch-Satz unter 1.2.0.5 mit dem Sammel-Upload | Orchestrierung | mit dem Upload |

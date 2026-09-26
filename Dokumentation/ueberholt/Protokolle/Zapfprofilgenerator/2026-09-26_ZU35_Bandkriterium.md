# ZU35 Bandkriterium: Band P95 bis P99,9 ab zehn Einheiten, dritter Validierungslauf (26.09.2026)

Protokoll des Postens **#553**. Anwenderentscheid ZU35 (26.09.2026), wörtlich: „so umsetzen: für
Anlagen ab zehn Einheiten ein Band P95 bis P99,9; darunter „nicht bewertbar" als gelbe Ampel statt
rot. Formprüfung und Energiebilanz bleiben unverändert." Umsetzungskonzept Zapfprofilgenerator,
Nachtrag N32; Kapitel 9 ZU35, ZU21, K5; Validierungsbericht Abschnitt 8.

**Rahmen.** Worktree `zband`, Zweig `zband` von `680b8ff2c` (Schemastand 149), Opus 5.5; vor dem
Säen `origin/ios_migration_september` `6ac7f4f91` (#552, Schemaschritt 150, Testdatenbank
`6ce7ddfa`) hereingeholt, die Testdatenbank aus dieser Fassung gesät. Kein Schemaschritt, kein Push.

---

## 1 Parameter

| Schlüssel | vorher | nachher | Provenienz nachher |
|---|---|---|---|
| `Zapfprofil.Validierung.Band.Unten` | 0,85, `FREI`, „Setzung Zapfprofilgenerator" | **0,95** | „Anwenderentscheid ZU35, aus der Bandanalyse des zweiten Validierungslaufs", `EIGENKONSTRUKTION` |
| `Zapfprofil.Validierung.Band.Oben` | 0,95, `FREI` | **0,999** | wie oben |
| `Zapfprofil.Validierung.Band.MindestEinheiten` | — | **10** (neu, Einheit „-", Bereich 1 … 10 000) | wie oben |

Quelle `Referenzlaeufe/Skripte/zapfprofil_setzungen_inekon.json` (drei Einträge angefügt, Kopfregel
fortgeschrieben); `tww_testkatalog_fiktiv.py <db> --paketteil-schreiben` hat
`Tab_TwwParameter_STAMM.csv` neu erzeugt — die zwei Bandzeilen wandern aus dem FREI-Block ans Ende
hinter die übrigen INEKON-Setzungen, die dritte kommt dazu (38 Zeilen). Der erste Lauf scheiterte an
der Semikolonprüfung des Skripts (ein Semikolon in der Ausgabe der Mindestzahl) und schrieb nichts;
nach der Korrektur lief er durch. Code-Vorgaben des Eingangs: 0,95 / 0,999 / 10.

## 2 Testdatenbank

| Schritt | Ergebnis |
|---|---|
| Ausgang | origin-Fassung `6ce7ddfa` (70 684 672 Byte, Schemastand 150) |
| Saat | `tww_testkatalog_fiktiv.py` — „1 Zeile(n) angelegt, 2 nachgeführt" |
| zweiter Lauf | „0 Zeile(n) angelegt, 0 nachgeführt" |
| Zellvergleich gegen die Ausgangsfassung | nur `Tab_TwwParameter_STAMM` (93 → 94 Zeilen): ID 81 und 82 Wert, Quelle, Ausgabe, Herkunftsart; ID 94 neu; dazu `sqlite_sequence` dieser Tabelle 93 → 94. Keine andere Tabelle |
| integrity_check / foreign_key_check | ok / 0 |
| LFS | Zeiger `41343bce`, Filter aktiv, keine `-shm`/`-wal` |

Die Referenzprojekte lesen die Validierungsparameter nicht; die Einfrierregel „gesäte
Zapfprofil-Eingaben" betrifft die Katalogzeilen, die ein Referenzprojekt **benutzt** — diese nicht.

## 3 Kern, Dialog, Werkzeug

| Teil | Dateien | Änderung |
|---|---|---|
| Kern | `EPOS.Kern/Allgemein/Zapfprofil/Messvergleich.cs`, `Zapfprofileingang.cs`, `TwwParameterschluessel.cs`, `MyResource/Resource(.en-US).resx`, `Resource.Designer.cs` | `Spitzenlage.NichtBewertbar`, `Vergleichsampel`, `Bandabgleich.Ampel/Einheiten/MindestEinheiten`, `Messvergleichsergebnis.Gesamtampel`, `Messvergleichseingang.MindestEinheiten`, `AusParametern` liest die Mindestzahl; Satz `MESSVERGLEICH_BAND_NICHT_BEWERTBAR` und drei Beschriftungen in beiden Sprachen |
| Hülle | `EPOS.UI.Daten/Bedarf/ZapfprofilHuelle.Messvergleich.cs`, `ZapfprofilHuelle.cs` | Kennung in `VALIDIERUNGSHINWEISE`; DTO-Felder befüllt; Texte |
| Dialog | `EPOS.UI/Dialoge/Bedarf/ZapfprofilDialog.razor`, `ZapfprofilDaten.cs`, `ZapfprofilTexte.cs` | Bandzeile gelb mit Grund (`epos-ampel epos-ampel--gelb`), Kopf mit einer Nachkommastelle, `ZapfprofilSpitzenlage.NichtBewertbar`, `ZapfprofilVergleichsampel` |
| Werkzeug | `Werkzeuge/ZapfprofilValidierung/Objektbefund.cs`, `Objektlauf.cs`, `Bericht.cs`, `Bandanalyse.cs`, `LIESMICH.md` | Kriterium gelb mit Grund, Bandregel und Zählung im Sammelbericht, Analyse spricht vom Band (b) |
| Tests | `EPOS.Kern.Tests/MessvergleichTests.cs` (+5, Geometrieproben auf P85–P95 gepinnt), `ZapfprofilWeicheTests.cs` (24 Schlüssel, Werte aus der Testdatenbank), `EPOS.UI.Tests/Dialoge/ZapfprofilVergleichDialogTests.cs` (+1), `Werkzeuge/ZapfprofilValidierung.Tests/BandkriteriumTests.cs` (+2) | |
| Wiki | `Projekte/Wiki/Programm Dokumentation - Brauchwasser-Zapfprofil.wiki`, „Vergleich und Kalibrierung" | Band P95 bis P99,9, Mindestzahl zehn Einheiten, gelb „nicht bewertbar"; Tabuwortprüfung ohne Treffer |

**Ein Rot im ersten Filterlauf:** `ZapfprofilDatenTests.Jede_Beschriftung_steht_mit_ihrem_Schluessel_in_beiden_Sprachen`
verlangt je Beschriftung genau `/// <summary><c>SCHLÜSSEL</c></summary>`; die neue Zeile trug einen
Zusatz im Kommentar. Behoben vor dem Commit.

## 4 Dritter Validierungslauf (V8)

`dotnet run --project Werkzeuge/ZapfprofilValidierung -c Release -- C:\Waermeplan\Messreihen_extern\konvertiert\objekte --ziel <Scratch> --katalog <Kopie der neu gesäten Testdatenbank>`;
zehn Realisierungen, feste Saat, 21 Objekte, 44 Berichtsdateien, Berichtswache ohne Fund.

| Zählung | zweiter Lauf | dritter Lauf | Gegenrechnung: Hoteltyp, altes Band P85–P95 ohne Mindestzahl |
|---|---|---|---|
| Objekte grün / gelb / rot | 0 / 0 / 21 | 3 / 0 / 18 | 0 / 0 / 21 |
| Band | 0 / 0 / 21 | 8 / 10 / 3 | 0 / 0 / 21 |
| Form | 3 / 0 / 18 | 6 / 0 / 15 | 6 / 0 / 15 |
| Energie | 21 / 0 / 0 | 21 / 0 / 0 | 21 / 0 / 0 |
| √N (11 belastbare Objekte) | +0,54 | +0,44 | +0,44 |

Einzelheiten, Band je Objekt, Hotels und V9 im Validierungsbericht, Abschnitt 8.

## 5 Gates

| Prüfung | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler |
| Werkzeug `ZapfprofilValidierung.sln` bauen und testen | 0 Fehler; 36 / 36 |
| gefilterte Tests (Messvergleich, Zapfprofil, Tww, Validierung, Parameter, Auslieferung, Vorlage, Wachen) | Kern 1224 / 1224, UI grün nach der Korrektur (Zapfprofil 171 / 171) |
| voller Lauf `WP-Plan.Kern.slnf` | Kern 8372 (+1 übersprungen), UI 6714, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (+1 übersprungen) — 0 Fehler |
| Windows-Schale `-p:EnableWindowsTargeting=true` | 0 Fehler |
| `SqlDialektPruefer` | 1997 SQL-Texte, 0 Fundstellen |
| Referenzlauf 1030, 1007, 1017, 1045, 1046, 1047 gegen `2026-09-26_R21_BhkwDeckung` | 6 / 6 PASS (Toleranz relativ 1e-4 ab Betrag 1, sonst absolut 0,01) |
| Designer | `designer_neu.py` ohne Diff (CRLF zurückgesetzt) |
| nach dem Merge von origin | origin `64466938` (Berichtsvorlagen-Muster #556, Papiere) gemergt `88f7abd3e`, Testdatenbank unverändert, Saat 0/0, Designer ohne Diff; Kern-Filter 0 Fehler, gefilterte Tests Kern 1231 / 1231, UI 680 / 680, Referenzlauf 6 / 6 PASS |

## 6 Folgen

Siehe N32: V9 (Zirkulation in den Realisierungsspitzen), V10 (eigene Quantile der Spitzenstreuung),
K5 (Bestätigung von ZU35 an eigenen Objekten), Sichtabnahme, Logbuch-Satz unter 1.2.0.5.

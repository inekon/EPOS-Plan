# Katalog-Folgeposten V6, V7, Ecodesign-Bezug, zwei Schwellen (26.09.2026)

Protokoll des Postens **#546**. Auftrag (Anwender 26.09.2026): „Auf Zuruf startbar: Setze um" —
vier Folgeposten des Zapfprofilgenerators: Katalogtyp „Hotel" (Folge V6 des Validierungsberichts),
Spitzenstreuung im Werkzeugbericht (V7), Ecodesign-Profile nach Wohneinheiten skalierbar (N13
Folge (a)), die zwei Schwellen `Zapfprofil.Messwert.Rueckfrageschwelle` und
`Zapfprofil.Formvektor.Warnschwelle` in den freien Paketteil. Umsetzungskonzept Zapfprofilgenerator,
Nachtrag N31; Kapitel 9 ZU21, ZU36, K5.

**Rahmen.** Worktree `zkat`, Zweig `zkat` von `69cf3ced0` (Schemastand 148, Basis R20), Opus 5.5;
vor den Papieren `origin/ios_migration_september` (`eb54960e`: #547, E51 mit Schemastand 149, Basis
R21) hereingeholt. Kein Schemaschritt, kein Push.

---

## 1 Hotel (V6): welcher Weg

Die lokalen Normtabellen der VDI 6002 (nur gelesen, gitignoriert) führen die Nutzungsarten großes
Wohngebäude, Ein- und Zweifamilienhaus, Studentenwohnheim, Seniorenheim, Krankenhaus, zwei
Hallenbäder und Campingplatz; ihre Quellenbeschreibung sagt ausdrücklich, dass die Richtlinie für
Hotel, Büro, Schule und Sportstätte weder Bedarfswerte noch Profile enthält. **Weg 1 (Ableitung nach
ZU19) scheidet damit aus; genommen ist Weg 2:** der Typ „Hotel (aus Messung)" aus den drei
norwegischen Hotelreihen des Validierungslaufs (HO3 übergeht der Konverter mangels Fenster).

| Schritt | Umsetzung |
|---|---|
| Rohdaten | `C:\Waermeplan\Messreihen_extern\norwegen_mendeley\HO1_2.csv`, `HO2_2.csv`, `HO4_2.csv` (außerhalb, CC BY 4.0) |
| Fenster | wie `Konverter/norwegen.py`: Kalenderjahr mit den meisten Werten, negative Stundenwerte auf 0, längstes Fenster mit höchstens 5 % Lücken |
| Kanal | **nur Zapfenergie** (`Q_chw`); die Zirkulation rechnet der Generator selbst, der Katalog führt die Zapfstelle |
| je Hotel | vollständige Tage (42, 45 und 138); Tagesbedarf je Zimmer (Zimmerzahl aus Tabelle 1 der Beschreibung), Wochenanteile aus den Tagesmitteln je Wochentag, Stundenanteile je Tagtyp als kleinste Quadrate — dieselben Formeln wie `Messkalibrierung.Nichtwohnparameter`; Feiertage Norwegens als Sonntag |
| über drei Hotels | ungewichtetes Mittel der Anteile, danach Summe 1; Bedarf mittel = Mittel, niedrig/hoch = kleinstes/größtes Hotel |
| Rundung | Bedarf auf 0,1 kWh je Zimmer und Tag: **3,2 / 3,9 / 4,9**; Anteile auf drei Stellen; Monatsfaktoren 1 |
| Ablage | `Referenzlaeufe/Skripte/tww_hotel_aus_messung.json` (erzeugt von `hotel_aus_messung_bauen.py`), daraus der Paketteil über `tww_testkatalog_fiktiv.py --paketteil-schreiben` |
| Katalogzeile | Bezugsart Bett (ein Zimmer = ein Bett), Kalender Betrieb (Gruppe Nichtwohnen), 60/12 °C, Herkunftsart `EIGENKONSTRUKTION`, Quelle „Mittel aus drei Hotels, Sørensen et al. 2021, doi:10.1016/j.dib.2021.107228", Status `AUSLIEFERUNG` |

**Warum nicht der Vorschlag des Werkzeugs.** `Werkzeuge/ZapfprofilValidierung` bildet den
`Nichtwohnparameter`-Vorschlag aus der verglichenen Reihe, bei den Hotels Zapfung plus Zirkulation
(Bilanzgrenze mit Verteilung). Die Zirkulation glättete den Tagesgang und höbe den Bedarf; das
Skript nimmt deshalb dieselben Formeln auf dem Kanal Zapfenergie.

**Was im Repositorium steht.** Gerundete Kenn- und Verhältniswerte, gemittelt über drei Gebäude —
keine Messreihe, keine Stunden- oder Jahresmenge eines Gebäudes. Die Lizenz erlaubt die Weitergabe
mit Namensnennung; Quelle und Ausgabe der Katalogzeile nennen sie.

**Außerhalb nachgezogen.** Der Konverter ordnet die Hotels dem neuen Typ zu; die `objekt.json` von
NO-HO1, NO-HO2 und NO-HO4 unter `C:\Waermeplan\Messreihen_extern\konvertiert\objekte\` sind neu
geschrieben.

## 2 Spitzenstreuung (V7)

| Datei | Änderung |
|---|---|
| `Werkzeuge/ZapfprofilValidierung/Objektlauf.cs` | `Kalibrieren` gibt den Streckfaktor der Zapfung heraus; `Spitzen(e, faktor)` skaliert die Realisierungsspitzen damit, wenn gegen die kalibrierte Reihe verglichen wird; `Vergleichen` und `Bandanalyse` bekommen dieselbe Stichprobe |
| `Werkzeuge/ZapfprofilValidierung.Tests/BeispiellaufTests.cs` | `Die_Spitzenstreuung_haengt_nicht_am_Kalibrierfaktor`: Messreihe mal drei — Kalibrierfaktor dreifach, Spitzenverhältnis, Streuung und Streubreite gleich |

Der Dialogweg vergleicht ungekalibriert auf beiden Seiten und bleibt unverändert. Neu benannt (V9):
Die Spitzen sind Spitzen der Zapfung; bei Bilanzgrenze 2 oder 3 trägt die verglichene Reihe die
Zirkulation mit — in beiden Wegen gleich.

## 3 Ecodesign nach Wohneinheiten

| Schicht | Datei | Änderung |
|---|---|---|
| Paketteil | `Referenzlaeufe/Skripte/ecodesign_profile_bauen.py`, `Tab_TwwBedarfstag_STAMM.csv` | Bezugsmenge je Profil = Q_ref / Q_ref(L), kaufmännisch auf 0,01, aus der Rohtabelle gerechnet: XXS, XS, S 0,18; M 0,5; L 1; XL 1,64; XXL 2,1; 3XL 4,01; 4XL 8,02; Ereignisse unverändert (L byte-gleich) |
| Kern | `EPOS.Kern/Allgemein/Zapfprofil/ZapfprofilAuslegung.cs` | `Katalogtag`: Ziel die Bezugsmenge der Gruppe in Wohneinheiten, sonst die Wohneinheiten der Wohnungstabellen; Hinweis `ECODESIGN_SKALIERT` über `ECODESIGN_HINWEIS_WOHNEINHEITEN` = 10 |
| Texte | `Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` | `ZPG_AUSHINW_ECODESIGN_SKALIERT`, `ZPG_SATZ_AUSHINWEIS_ECODESIGN_SKALIERT` |
| Hülle | `EPOS.UI.Daten/Bedarf/ZapfprofilHuelle.Auslegung.cs` | Kennung in `AUSLEGUNGSHINWEISE` |
| Tests | `EcodesignTests`, `AuslegungsergebnisTests`, `ZapfprofilSchritt124Tests` | L bei 1 WE unverändert, bei 10 WE ×10, M bei 1 WE ×2; Auslegung mit 8 und 12 WE und über die Wohnungstabelle; Bezugsmengen gegen Q_ref |

`Bedarfstag.AusKatalog` und `Bedarfstag.Skalierung` greifen mit der Bezugsmenge ohne Änderung.
Verhaltensänderung: Eine Zone in Personen ohne Wohnungstabelle bekommt einen Ecodesign-Tag benannt
abgelehnt (`AUSLEGUNG_KATALOGTAG_BEZUGSART`) statt unskaliert.

## 4 Die zwei Schwellen

`Zapfprofil.Messwert.Rueckfrageschwelle` 0,5 und `Zapfprofil.Formvektor.Warnschwelle` 0,01 als
INEKON-Setzung (Quelle „INEKON-Setzung (Konzept 2.2)" bzw. „(Konzept 2.4)", `EIGENKONSTRUKTION`,
`AUSLIEFERUNG`, ReadOnly 1) am Ende von `Tab_TwwParameter_STAMM.csv`, erzeugt aus
`Referenzlaeufe/Skripte/zapfprofil_setzungen_inekon.json`; aus dem fiktiven Testkatalog gefallen.
`TwwParameterschluessel`: Bereich je 0 bis 100 — beide Werte liegen darin, die Prüfposten der
Auslieferungsvorlage (`ParameterDesPaketteilsPruefen`) halten sie.

## 5 Wachen und Werkzeugtests

- `TwwKatalogWacheTests`: die zugelassenen Paare aus Herkunftsart und Quelle je Provenienzgruppe der
  Paketteil-Datei (auch `Bedarf_`, `Jahresgang_`, `Wochengang_`); Tagesgänge und Nutzungsarten des
  Paketteils zählen `VERFAHREN` und `EIGENKONSTRUKTION`.
- `Auslieferungsvorlage.Tests/TwwVorlageTests.T13`: Herkunft je Gruppe wie die Datei — `VERFAHREN`
  mit „abgeleitet aus VDI 6002 Blatt n" oder `EIGENKONSTRUKTION` mit „Mittel aus drei Hotels, …".
- `ZapfprofilHuelleKatalogdialogTests`: neun Nutzungsarten im Katalog der Testdatenbank.

## 6 Testdatenbank

| Stand | LFS-oid |
|---|---|
| Ausgang (`69cf3ced0`) | `22e67400` |
| origin nach Merge (`eb54960e`, Schemastand 149) | `217a519b` |
| nach der Saat #546 | `979fe89c` |

Zellvergleich gegen den Stand von origin: `Tab_TwwBedarfstag_STAMM` 9 Zellen (Bezugsmengen),
`Tab_TwwParameter_STAMM` 8 Zellen (Provenienz der zwei Schwellen, Werte gleich),
`Tab_TwwNutzungsart_STAMM` +1, `Tab_TwwTagesgangsatz_STAMM` +1, `Tab_TwwTagesgang_STAMM` +4,
`Tab_TwwZapfkategorie_STAMM` +2 Zeilen, `sqlite_sequence` 4 Zellen; sonst nichts. Zweiter Lauf 0/0;
keine `-shm`/`-wal`.

## 7 Gates

| Prüfung | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler |
| Werkzeug-Projektmappen Validierung, Auslieferungsvorlage, `EPOS.Referenzlauf` | 0 Fehler |
| Kern-Tests gefiltert (Tww, Zapfprofil, Ecodesign, Bedarfstag, Auslieferung, Vorlage, Parameter, Auslegung, Katalogpflege, Dokumentations-, Wiki- und Ordnungswache) | 1 432, 0 Fehler |
| Validierungswerkzeug | 34, 0 Fehler |
| Auslieferungsvorlage | 38, 0 Fehler |
| voller Lauf `WP-Plan.Kern.slnf` | 0 Fehler: KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 + 1, EPOS.UI.Tests 6 681, EPOS.Kern.Tests 8 316 + 1 |
| `SqlDialektPruefer` | 1 998 Texte, 0 Fundstellen |
| Designer | wiederholbar, zwei neue Einträge |
| Referenzlauf 1030, 1007, 1017, 1045, 1046, 1047 gegen `2026-09-26_R21_BhkwDeckung` | PASS, alle byte-gleich |

Der erste volle Lauf fand zwei Fälle `KatalogpflegeTests.DerScanMeldetDieEingefrorenenZahlen` (Zählung der
Nutzungsarten und Tagesgangsätze der Testdatenbank); nachgezogen in `772ea81c0`.

## 8 Folgen

| Nr. | Gegenstand | Wer |
|---|---|---|
| V8 | dritter Validierungslauf mit dem Hoteltyp (Übertragungsprobe) | Agent eines Folgepostens |
| V9 | Realisierungsspitzen der Zapfung gegen eine Reihe mit Zirkulation (Grenze 2, 3) | mit V8 |
| ZU21 | Durchsicht der Modellannahmen des Hotels | Anwender |
| Sicht | Ecodesign-Tag über zehn Wohneinheiten: Skalierung und Hinweis in der Auslegung | Anwender |
| Wiki | zwei Logbuch-Sätze unter 1.2.0.4 | Orchestrierung mit dem Upload |

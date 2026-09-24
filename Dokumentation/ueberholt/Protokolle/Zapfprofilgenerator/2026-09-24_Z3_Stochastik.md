# Z3 — Zapfprofilgenerator: Stochastik (Protokoll, 24.09.2026)

Statuszeile #453 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Zeile Z3
in Kapitel 7 und der Nachtrag N12 im
[Umsetzungskonzept](../../../aktuell/Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md); Abschnitt 10
der [Übergabe](../../../aktuell/Zapfprofilgenerator/2026-09-23_Uebergabe_Zapfprofilgenerator.md);
Vorstufe im Protokoll [Z2](2026-09-23_Z2_Auslegung_deterministisch.md). Zweig `z3` von `ffc27d18`,
49 eigene Commits bis `1a2ec19e`; Merges von `origin` (`7a32b6f3` in `82002260`, `aab9896e` in
``6ad35b61``, `a1df2dbe` in `1a2ec19e`). Alle Gates im Worktree, ohne CI-Lauf bis zum Push.

## Auftrag

Stufe Z3 nach Kapitel 7 des Umsetzungskonzepts: portabler Zufall samt Plattformtest, Zapfereignisgenerator
mit gestutztem Mittel, Ensembles der Jahresreihe und des Bedarfstags über `Kulturweitergabe`, Perzentil je
Topologiegruppe, Gleichzeitigkeit als Ergebnis, Entkopplung der Urlaube, Rechenweg der Jahresreihe
„stochastisch"; Schemaschritt T2 (115) mit `Tab_TwwZapfkategorie_STAMM`; Katalog der Zapfkategorien,
Ecodesign-Zapfprofil; Oberfläche der Stochastik in Zapfprofil-Dialog und Auslegung. Dazu der
Anwenderentscheid ZU19 (abgeleitete VDI-Werte im Repositorium, reproduzierbar). Drei Gruppen durch Agenten
mit `model: opus` im Worktree `z3`, je Gruppe eine Gegenprüfung durch einen zweiten Agenten und eine
Nachbesserung; ohne Push und ohne CI-Lauf bis zum Abschluss.

## Gruppen und Commits

| Gruppe | Inhalt | Commits |
|---|---|---|
| 1 | `ZapfZufall` (SplitMix64, xoshiro256**, Lemire, Σ12u−6, von Neumann, Poisson), `Zapfkategorie`/`Zapfkategoriensatz`/`ZapfStochastikParameter`, `Zapfereignisgenerator` (Poisson je Kategorie und Tag, exaktes Irwin-Hall-Mittel, Kappung), `Zapfensemble` (Bedarfstag: Perzentile je Topologie, GLF_V/GLF_P, √N), `Jahresensemble` (Bilanz), Rechenweg stochastisch, 49 Tests, Referenzfälle | `df7e235b` … `0d9e7e82` (5) |
| 1 Nachbesserung | Bilanz = Jahr zum Seed × E_det/E_0; Kategorien ohne Anteil; `Minutenstatistik` und Wache; Prüfvektor xoshiro; Referenzfälle mit Volumina, Zirkulation und Jahresensemble; Speichergrenzen und `Volumenauftrag`; DHWcalc-Vergleich gegen OpenDHW | `a9d77ec9`, `1f23527e`, `405c1d19`, `943e31df`, `bfcee5dc`, `764ce214`, `188bcf75`, `3c72fa11`, `6379e3bf` |
| 2 | Merge `7a32b6f3` (R13, Katalog-Generation 9); Schritt 115 `Tab_TwwZapfkategorie_STAMM`; Register, Transfer (Kindzeilen), Auslieferungsvorlage (131 STRICT-Tabellen, Katalogpaket-Datei); `ZapfprofilCtrl.Eingang` liest Kategorien; ZU19: `normzahlen_abgeleitet_bauen.py` → `tww_katalogwerte_abgeleitet.json` (497 Werte), vier abgeleitete Nutzungsarten, Wache gegen die lokalen Originale; Ecodesign-Profil L; Testdatenbank neu (78 Zeilen) | `82002260`, `e391254d`, `43319d63`, `11188fd2`, `caab828c`, `bb97287b`, `aca90102` |
| 3 | Zapfprofil-Dialog: Stufe Experte, Gruppe „Stochastik · Jahresreihe" (Rechenweg, Seed, Realisierungen), Konsistenzprobe im Reiter Kennzahlen; Auslegung: „Stochastisch rechnen", P95/P99, Realisierungen, Karte (b) mit Streuband, GLF, „nicht belastbar", Konsistenzhinweis; DTO, 52 Ressourcenschlüssel je Sprache, Hülle; 12 Hüllen- und 16 bunit-Fälle | `4b364132`, `1cd10ce8`, `48eb0144`, `48bf4ecb` |
| 2 Nachbesserung | Merge `aab9896e`, T2 → 115; Herkunftsart FIKTIV mit Quelle „VDI 6002 (abgeleitet)"; freier Paketteil `Referenzlaeufe/Katalogpaket_frei/` (Kategorien, Parameter, Ecodesign) in Vorlage und Testdatenbank; Kopierstellen und Sperre der Kategorien; Ablehnung sprachfest; Wache Testdatenbank = Paket; Importtest vor 115; Testdatenbank neu | `2b6ec9d4`, `b1491c3c`, `0fbba4c7`, `61c6b976`, `68ab6970`, `7eedbbc7`, Merge `6ad35b61` (`aab9896e`), `f01d5d8c` (T2 → 115), `43da5221`, `85f0381a`, `6a9cf6da` (Testdatenbank LFS `fbc30835…`, 82 Zeilen) |
| 3 Nachbesserung | Kernschranke Einheitentage; Vorschau immer deterministisch; Jahresreihe und Auslegungsensemble nebenläufig mit Fortschritt und Abbruch (`Kulturweitergabe.Starten`, Abbruchmarke bis in den Kern); Seed in Experte immer; Erklärzeile; Spitze je Einheit der Wohnungsstation; Schalter aus `AuslegungGaben`; ∞ entfällt benannt; Konsistenzhinweis aus Werten; Datum; Regeln und Tests; 18 neue, 7 geänderte Schlüssel | `8f1d5635`, `f07bda40`, `f19953f9`, `f0cc7fb1`, `026c2ab3`, `a02e3d45`, `76a3ced1`, `0f71d96c`, `83a45c97`, `e4ff3168`, `9826dc7e`, `f53680f3` (13) |
| Abschluss | Merge `origin` (aab9896e), Papiere (N12, Protokoll, Statuszeile, Übergabe, Wiki-Quelle), Gate | `1a2ec19e` (Merge), Testhärtung und Papiere danach |

## Gates

| Stand | Kern-Build | Tests | Weiteres |
|---|---|---|---|
| Gruppe 1 (`0d9e7e82`) | 0 Fehler | 11 802 grün | Windows-Schale 0 Fehler; Referenzlauf PASS, byte-gleich |
| Gegenprüfung 1 | 0 Fehler, 22 Warnungen (Bestand) | 11 802 grün (Kern 5308) | Windows-Schale 0 Fehler |
| Nachbesserung 1 (`6379e3bf`) | 0 Fehler | Kern 5313, UI 5557, KiKern 524, Engine 386, Planung 27 (+1) | Windows-Schale 0 Fehler; Designer ohne Diff |
| Gruppe 2 (`aca90102`) | 0 Fehler | Kern 5382, UI 5591, KiKern 524, Engine 386, Planung 27 (+1) | `SqlDialektPruefer` 1 742 Texte ohne Fund; Auslieferungsvorlage 27/27; Windows-Schale 0 Fehler; Referenzlauf der fünf CI-Projekte gegen `2026-09-23_R13_Kuehlung` PASS, byte-gleich; Testdatenbank LFS 133 B |
| Gruppe 3 (`48bf4ecb`) | 0 Fehler | 11 938 grün (1 übersprungen) | ChartProben 145 Bilder ohne Verstoß; Windows-Schale 0 Fehler; SQL-Prüfer 0; Designer ohne Diff |
| Nachbesserung 2 (`6a9cf6da`) | 0 Fehler | 12 091 grün (1 übersprungen) | `SqlDialektPruefer` 1 757 Texte ohne Fund; Auslieferungsvorlage 30/30 und Trockenlauf (FREI-Zeilen enthalten, kein FIKTIV, keine Waise); Windows-Schale 0 Fehler; Referenzlauf 5/5 PASS, 153/153 CSV byte-gleich; Designer ohne Diff |
| Nachbesserung 3 (`f53680f3`) | 0 Fehler | 12 113 grün (1 übersprungen): Kern 5510, UI 5666, KiKern 524, Engine 386, Planung 27 | ChartProben 161 Bilder ohne Verstoß, keine neuen; Windows-Schale 0 Fehler; Referenzlauf 5/5 PASS (1 744 067 Werte); Designer ohne Diff |
| Abschluss (`1a2ec19e`) | 0 Fehler | 12 287 grün (1 übersprungen): Kern 5 585, UI 5 764, KiKern 524, Engine 386, Planung 27 | ChartProben 161 Bilder ohne Verstoß; `SqlDialektPruefer` 1 759 Texte ohne Fund; Auslieferungsvorlage 30/30; Windows-Schale 0 Fehler; Referenzlauf 5/5 PASS, byte-gleich; Designer ohne Diff; ein wackliger bunit-Test (nebenläufige Jahresreihe) danach gehärtet |

## Merge und Nachzug

- `82002260`: Merge `7a32b6f3` (#450 samt KU1 Welle 4 und Basis R13) nach `z3`, konfliktfrei; die
  Testdatenbank kam unverändert von `origin` (Katalog-Generation 9) und wurde in Gruppe 2 zunächst als
  Schritt 114 fortgeschrieben.
- **Schemakollision.** Die Nummer 114 war vor der Vergabe auf `origin` (113) und allen lokalen Zweigen
  gemessen und mit den Sitzungen Wirtschaftlichkeit und Dialog Design abgestimmt. Um 22:22 pushte das
  Konto der Gebäudesimulation/Kühlung `24074b3a` (KU2 W1, `SCHRITT_114_KUEHLUNG_ERZEUGER`); `origin`
  (`aab9896e`) stand damit auf Zielversion 114 und einer geänderten Testdatenbank. T2 wurde in der
  Nachbesserung 2 auf **115** umnummeriert, `aab9896e` nach `z3` gemergt (`6ad35b61`: Konflikte in
  `SchemaStand.cs`, `SchemaMigration.cs`, `Werkzeuge/Testdatenbankschema/Program.cs` — beide Seiten —
  und der Testdatenbank — Fassung von `origin`, darauf 115 und der Testkatalog neu) und die Testdatenbank
  aus der `origin`-Fassung neu erzeugt. Lehre: Eine Schrittnummer ist erst mit dem Push vergeben; vor dem
  Merge wird sie erneut gemessen, und alle Konten (auch Gebäudesimulation/Kühlung) melden Schemaschritte.
- 1a2ec19e: Merge a1df2dbe nach `z3` vor dem Gate; ; Konflikte nur in beiden Resource-`.resx`, beide Seiten behalten, Designer neu erzeugt.
- Statusnummer #453 mit beiden Nachbarsitzungen abgestimmt (#454/#455 Wirtschaftlichkeit, #456–#459
  Dialog Design; Wirtschaftlichkeit E9 ab Schritt 116).

## Gegenprüfungen

- **Gruppe 1** (neun Befunde): Bilanz war das Ensemblemittel statt des Jahres zum Seed (hoch, behoben);
  unbenannter Abbruch bei Kategorien ohne Anteil (behoben); Lücke der Trennungswache bei
  Zahlenfeldern in Konstruktoren (behoben); xoshiro nur gegen die eigene Umschrift geprüft (Prüfvektor
  aufgenommen); Referenzfälle unvollständig (ergänzt, Jahresensemble neu); Speicherbedarf ohne Grenze
  (Kennzahlen statt Tage, Obergrenzen benannt). Bestätigt: Zufall, gestutztes Mittel (unabhängig
  rational nachgerechnet, Abweichung ≤ 4e-16), λ-Kalibrierung, Perzentile, √N, Bitgleichheit
  parallel/seriell, keine Normzahl.
- **Gruppe 2** (neun Befunde): Ableitung rückrechenbar (hoch) — vom Anwender am 24.09.2026 ausdrücklich
  zugelassen („reproducible acceptable"), kein Umbau; Herkunftsart EIGENKONSTRUKTION unzutreffend
  (→ FIKTIV mit Quelle „abgeleitet"); Papiere nachziehen (N12); Ecodesign erreichte die Auslieferung
  nicht (→ freier Paketteil); Sperre und Kopierstellen der Kategorien (behoben); Ablehnung mit
  deutschem Argument in en-US (behoben); Wache Testdatenbank = JSON (ergänzt); Skriptkopf zu
  Monatsextrema; Importtest vor 115. Bestätigt: DDL (damals 114, jetzt 115) inhaltsgleich mit der Testdatenbank, zweimal
  ausführbar, CHECK-Proben; Bestandstabellen inhaltsgleich zu `origin`; 497 abgeleitete Werte je
  0,19–5,88 % vom Original, keiner gleich; Ecodesign-Summe 11,655 kWh (8e-15).
- **Gruppe 3** (neun Befunde): stochastische Vorschau und Auslegung synchron im Renderfaden ohne
  Schranke (hoch; Vorschau jetzt deterministisch, Ensembles nebenläufig, Kernschranke); Seed nur bei
  „stochastisch" sichtbar (behoben); Erklärzeile versprach Einzelwerte (behoben); Spitze je Einheit der
  Wohnungsstation fehlte in Karte (b) (behoben); Laufangabe blieb nach OK stehen, Doppellauf (behoben);
  ∞ ohne Grund (behoben); Konsistenz über Textvergleich (Felder); „(K3)" und Jahrestagnummer in Texten
  (behoben); doppelte Regeln und Testlücken (behoben). Bestätigt: Datenfluss hin und zurück,
  Veraltet-Markierung, Vorgaben ohne Abschrift, Karte (b) nur Kernwerte, 52 Schlüssel je Sprache,
  CSS vor dem Rasterblock, Barrierefreiheit. Ein Wettlauf im bunit-Test nach der Nebenläufigkeit
  (einmal rot) ist mit `WaitForAssertion` behoben, danach fünfmal grün.

## Abweichungen vom Papier

Stehen im Nachtrag N12: Realisierungsseed doppelt gesät; Poisson je Kategorie und Tag statt Bernoulli
je Minute; exaktes Irwin-Hall-Mittel; `Kappung_l_min`; Regel für n_E; GLF_V-Skalierung;
Konsistenzhinweis über das Stundenperzentil; Obergrenzen; T2-Spaltenliste (16 Spalten,
Vierergruppe statt „Provenienz"); Kapitel 6 (b)/(c) (Testdatenbank nicht mehr rein fiktiv, Paare
Herkunftsart/Quelle); freier Paketteil im Repositorium; Ecodesign-Dauerregel; Experte statt Erweitert
trägt den Rechenweg; Vorschau rechnet den Rechenweg des Stands; Karte (b) trägt Streuband und
Gleichzeitigkeit; `ZPG_AUS_`-Präfix.

## Offene Punkte

- Sichtabnahme unter Windows (Prüfliste in der Übergabe, Abschnitt 10).
- ZU20: Auslieferung der abgeleiteten VDI-Werte (Anwender, nach K8); K8 und ZU15 offen.
- Profile M und XL der Verordnung (EU) Nr. 814/2013 als weitere Bedarfstage (bei Bedarf).
- KI-Maskenanmeldung der drei Dialoge und die Optionsgruppe „Rechenweg" in der Feldkarte des
  `BedarfsProfileDialog`: Sitzung Dialog Design nach dem Z3-Merge (#458).
- Wiki-Upload der Seite „Brauchwasser-Zapfprofil" (Abschnitt „Stochastik", Auslegung, Ecodesign) und
  drei Logbuch-Sätze mit Versionsnummer vom Anwender.
- Z4: Stufe Erweitert, Kategorien als Katalogkopie (Status EIGEN) im Experten-Modus, Erzeugerart und
  Werkstoff, DIN 1988-300; Z4b: VDI 4655 (T3) unter der Regel ZU19.

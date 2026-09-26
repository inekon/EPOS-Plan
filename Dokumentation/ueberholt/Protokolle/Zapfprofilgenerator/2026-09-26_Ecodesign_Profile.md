# Ecodesign erweitert: alle neun Zapfprofile der Verordnung im Paketteil (26.09.2026)

Protokoll des Postens **#537**. Auftrag: Anwenderentscheid „Abschnitt 1: Ecodesign - erweitere
Profil" (Prüfliste ZU21, N25) umsetzen — der freie Paketteil
(`Referenzlaeufe/Katalogpaket_frei/`) bekommt alle neun Lastprofile der Verordnung (EU)
Nr. 814/2013, Anhang III, Tabelle 1 (XXS bis 4XL, ohne 3XS), statt allein Profil L. Zweig `zeco`,
Worktree `.claude/worktrees/zeco`, Sonnet 5.

---

## 1. Quelle und Abruf

Verordnung (EU) Nr. 814/2013 der Kommission, Anhang III, Tabelle 1 „Lastprofile von
Warmwasserbereitern" (ABl. L 239 vom 6.9.2013, S. 162) — EU-Recht, keine Normzahl. Klartext
abgerufen über die konsolidierte Fassung
`https://www.legislation.gov.uk/eur/2013/814/annexes/data.xht` (EUR-Lex selbst lieferte über
WebFetch keinen Seiteninhalt; die HTML-Fassung von legislation.gov.uk enthielt Tabelle 1
vollständig als drei Spaltengruppen — 3XS/XXS/XS/S, M/L/XL, XXL/3XL/4XL — und wurde mit einem
eingebetteten HTML-Tabellenparser (Python `html.parser`, kein externes Paket) ausgelesen). Profil
3XS ist nicht Teil dieses Auftrags (Anwenderentscheid: neun Profile XXS bis 4XL).

---

## 2. Prüfsummen

Summe der Zapfungen Q_tap je Profil gegen Q_ref der Verordnung, Toleranz relativ 1e-9:

| Profil | Zapfungen | Summe Q_tap [kWh] | Q_ref [kWh] | Treffer |
|---|---|---|---|---|
| XXS | 20 | 2,100 | 2,100 | ja |
| XS | 3 | 2,100 | 2,100 | ja |
| S | 11 | 2,100 | 2,100 | ja |
| M | 23 | 5,845 | 5,845 | ja |
| L | 24 | 11,655 | 11,655 | ja (unverändert) |
| XL | 30 | 19,070 | 19,070 | ja |
| XXL | 30 | 24,530 | 24,530 | ja |
| 3XL | 10 | 46,760 | 46,760 | ja |
| 4XL | 10 | 93,520 | 93,520 | ja |

Alle neun Profile treffen ihr Q_ref genau — kein Übertragungsfehler. Bedarfstage insgesamt 12
(3 fiktiv, 9 Ecodesign), Ereignisse insgesamt 170 (9 fiktiv, 161 Ecodesign).

**Abweichungen.** Keine: kein Profil führt zwei Zapfungen zur gleichen Minute (auch nicht bei
3XL/4XL, wo die hohen Volumenströme das vermuten ließen); jede errechnete Dauer liegt zwischen
1 und 10 Minuten, wie bei Profil L.

**Profil L (ID 1): byte-gleich.** `ecodesign_profile_bauen.py` prüft bei jedem Lauf, dass die 24
Ereignisse der Zeile `ID_Bedarfstag = 1` byte-genau dem bisherigen Bestand von
`Tab_TwwBedarfstagEreignis_STAMM.csv` gleichen (Kontrolle des Verfahrens) — bestätigt, git diff
zeigt für die Bestandszeilen keine Änderung, nur Anfügungen.

---

## 3. Skripte

- **`Referenzlaeufe/Skripte/ecodesign_profile_814_2013.json`** (neu) — Rohtabelle: je Profil und
  Zapfung Uhrzeit, Q_tap, Volumenstrom f, Mindesttemperatur T_m, Spitzentemperatur T_p (wo
  geführt), dazu Q_ref je Profil zur Gegenprobe; Quellenangabe im Kopf.
- **`Referenzlaeufe/Skripte/ecodesign_profile_bauen.py`** (neu) — liest die Rohtabelle, berechnet
  je Zapfung die Dauer (Setzung der Umsetzung, unverändert seit Profil L: Dauer = Volumen /
  Volumenstrom, Volumen = Q_tap · 1000 / (c_w · (Nutztemperatur − 10 °C)), Nutztemperatur =
  Spitzentemperatur, wo angegeben, sonst Mindesttemperatur, c_w = 1,163 Wh/(l·K)
  `Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K`; ganze Minuten kaufmännisch mit `Decimal`/
  `ROUND_HALF_UP`, mindestens 1), prüft die Prüfsummen und die Byte-Gleichheit von Profil L, und
  schreibt (Schalter `--schreiben`) `Tab_TwwBedarfstag_STAMM.csv` (9 Zeilen, Profil L als ID 1,
  die übrigen acht in Größenordnung XXS, XS, S, M, XL, XXL, 3XL, 4XL als ID 2–9) und
  `Tab_TwwBedarfstagEreignis_STAMM.csv` (161 Zapfungen, angehängt an die 24 unveränderten
  Ereignisse von Profil L) neu.
- **`Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py`** — unverändert (liest den Paketteil
  generisch über Bezeichner, keine Codeänderung nötig); zweiter Lauf auf der neu gesäten
  Testdatenbank: „0 Zeile(n) angelegt, 0 nachgeführt".

---

## 4. Testdatenbank

`Referenzlaeufe/Kenndaten_Test.sqlite` (Git LFS) neu gesät aus dem geänderten Paketteil:
`Tab_TwwBedarfstag_STAMM` 12 Zeilen, `Tab_TwwBedarfstagEreignis_STAMM` 170 Zeilen (vorher 4 bzw.
33); alle übrigen Tww-Katalogtabellen unverändert (5 Tagesgangsätze, 20 Tagesgänge,
8 Nutzungsarten, 24 Zapfkategorien, 85 Parameter, 5 DIN-4708-Werte). `integrity_check` ok,
`foreign_key_check` 0 Befunde, kein `AUSLIEFERUNG`/`IMPORT`. Zweiter Lauf idempotent, keine
`-shm`/`-wal`-Beidateien zurückgeblieben.

---

## 5. Geänderte Wachen und Tests

| Datei | Änderung |
|---|---|
| `EPOS.Kern.Tests/EcodesignTests.cs` | von einem Einzeltest auf `[Theory]` über alle neun Profile umgestellt (Bezeichner, Anzahl der Ereignisse, Q_ref je Profil); zusätzlicher Test „genau neun Ecodesign-Profile ohne Dopplung" |
| `EPOS.Kern.Tests/TwwKatalogWacheTests.cs` | Zeile 162 f.: Zähler des Ecodesign-Bedarfstags von `== 1` auf `== 9`; Gegenprobe-INSERT trifft jetzt gezielt nur Profil L (`MIN(ID)` der FREI-Bedarfstage) statt aller neun Zeilen |
| `EPOS.Kern.Tests/ZapfprofilSchritt124Tests.cs` | `SingleOrDefault` auf die Ecodesign-Quelle ersetzt durch `Where(...).ToList()` mit `Assert.All`, da jetzt neun Zeilen dieser Quelle da sind |
| `EPOS.Kern.Tests/ZapfprofilAuslegungHuelleTests.cs` | prüft zusätzlich die Zahl (9) der Ecodesign-Katalogtage; die byte-genaue Summenprobe bleibt an Profil L (Bezeichner-Filter statt reiner Quellenart) |
| `EPOS.Kern.Tests/KatalogpflegeTests.cs` | Zeile der `[Theory]`-Zählprobe „TWW_BEDARFSTAG" von 4 auf 12 Sätze fortgeschrieben |

Keine Codeänderung am Rechenkern, an der Oberfläche oder an `Werkzeuge/Auslieferungsvorlage`
nötig: Der Einspielweg (`TwwKataloge.PaketteilEinspielen`) und die Wache
`TwwKatalogWacheTests.Die_freien_Zeilen_der_Testdatenbank_gleichen_dem_Paketteil` lesen den
Paketteil generisch über seine Zeilen, nicht über eine feste Anzahl.

---

## 6. Papiere

- Prüfliste ZU21 (`Zapfprofilgenerator/2026-09-25_Pruefliste_ZU21_Setzungen.md`): Zeile „Auswahl
  der Ecodesign-Profile", heutiger Wert auf „alle neun Profile (XXS bis 4XL)" fortgeschrieben; die
  Entscheid-Spalte trug bereits der ZU21-Agent ein (N25).
- Umsetzungskonzept Zapfprofilgenerator: Kapitel 9, Zeile ZU21, Vermerk „Ecodesign erweitert
  (N26)"; Nachtrag **N26** (Wortlaut, Quelle, Verfahren, Prüfsummen, Abweichungen, Folgen).
- `Referenzlaeufe/Katalogpaket_frei/LIESMICH.md`: Tabelle „Inhalt und Quellen" (9 statt 1
  Bedarfstag, 161 statt 24 Ereignisse), neuer Absatz „Die neun Ecodesign-Zapfprofile" mit Verweis
  auf das Erzeugerskript, Skript-Aufzählung ergänzt.
- `Referenzlaeufe/LIESMICH.md`: Zählungen der Tww-Katalogtabellen (12 Bedarfstage/170 Ereignisse
  statt 4/33), Absatz „Der freie Paketteil".
- `Projekte/Wiki/Programm Dokumentation - Brauchwasser-Zapfprofil.wiki`: Zeile zum
  Ecodesign-Zapfprofil auf alle neun Profile umgeschrieben, ohne Tabuwörter.
- `Dokumentation/aktuell/Wiki_Update_2026-09-26.md`: Logbuch-Satz unter 1.2.0.4 mit „(#537)".
- Statuszeile **#537** in `Dokumentation/aktuell/Status_iOS_Migration.md`.

---

## 7. Gate

| Gate | Ergebnis |
|---|---|
| Kern-Filter (`WP-Plan.Kern.slnf`, Release) | 0 Fehler |
| gefilterte Tests (Tww, Zapfprofil, Ecodesign, Katalogpflege, Dokumentations-/Wiki-/Ordnungswache) | 631/631 (EPOS.Kern.Tests), 221/221 (EPOS.UI.Tests) |
| voller Testlauf `WP-Plan.Kern.slnf` | 0 Fehler (KiKern 549, SpeicherEngine 386, SpeicherPlanung 27+1 übersprungen, EPOS.UI.Tests 6.531, EPOS.Kern.Tests 8.096+1 übersprungen) |
| `Auslieferungsvorlage.Tests` | 38/38 erfolgreich |
| `SqlDialektPruefer` | 1.988 SQL-Texte, 0 Fundstellen |
| Referenzlauf sechs CI-Projekte gegen `2026-09-26_R20_Zapfprofil` | GESAMT PASS (2.208.587 Werte) |
| Zweiter Merge, Build und gefilterte Tests danach wiederholt | 0 Fehler, unverändert grün |
| Arbeitsbaum | sauber, keine `-shm`/`-wal`-Beidateien |

---

## 8. Merge und Folgen

- `git fetch origin` + Merge vor Beginn der Papierarbeit (Hinweis des Orchestrators): origin war
  mit #533 (ZU21 entschieden, N25, `64b18fbb5`) vorangekommen — Fast-Forward, kein Konflikt.
- Zweiter `git fetch origin` + Merge vor dem Abschluss: origin war mit #534 (Wirtschaftlichkeits-
  Sitzung, `45c35a946`, CI-Wächter-Nachzug: `Quelltextleser.cs`, `StandardkulturEnUs.cs` und
  `xunit.runner.json` für `KiKern.Tests`/`SpeicherEngine.Tests`/`SpeicherPlanung.Tests`)
  vorangekommen — Fast-Forward, kein Konflikt, keine Berührung der geänderten Dateien; Build und
  gefilterte Tests danach erneut grün.
- Kein Schemaschritt, keine Codeänderung am Rechenweg — ergebnisneutral für den Referenzlauf (kein
  Referenzprojekt benutzt ein Ecodesign-Profil).
- Push: noch nicht erfolgt — folgt auf Zuruf.

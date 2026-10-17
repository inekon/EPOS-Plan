# RB1 — Rechenwegbefunde aus dem Papier „Verbesserungen 29.09.2026“: Autarkie ohne Stromspeicher, BHKW-Untergrenze, Profil ohne Typ, Basis R31

Stand: 02.10.2026 · Zweig `rechenweg-befunde` ab `fe0389d1` (Arbeitszweig `ios_migration_september`, Schemastand 159,
Basis R30) · Opus-Agent im Worktree. **Kein Schemaschritt, keine Änderung der Testdatenbank** (`a50f1f49…`). Vorgänger
in diesem Ordner [`SV2_Bedarf_Zuordnung_ID_Protokoll.md`](SV2_Bedarf_Zuordnung_ID_Protokoll.md).

Commits:
- `25840188` Kern, Oberfläche, Ressourcen (de/en, Designer neu), Tests samt nachgezogener Anker, Hilfeseiten
- `9088cabb` Basis R31 eingefroren
- `930863be` R30 aus dem Arbeitsbaum, ihr Protokoll nach `Dokumentation/ueberholt/Referenzbasen/`, Basisname R31 in
  `CLAUDE.md` (samt Einfrierregel), `kern.yml`, `ios.yml`, `Werkzeuge/Gate/LIESMICH.md`, `Referenzlaeufe/LIESMICH.md`
  und den Konzepten
- `8a963701` `RechenrandFahrweisenTests`: Untergrenze je Modul im Reflexionsaufruf als Reihe
- dieses Protokoll samt Indexzeile im Commit danach

## 1 Auftrag und Entscheide

Drei Befunde des Papiers „Verbesserungen 29.09.2026“, je mit Anwenderentscheid:

1. **Autarkie-Analyse ohne Stromspeicher** — Entscheid „Ohne Speicher = 0 kWh!“. Die Analyse nahm einen
   5-kWh-Speicher an, wenn das Projekt keinen führt.
2. **BHKW: „Untere Grenzleistung des ausgewählten Moduls“ wird wirksam, unabhängig von der Betriebsart.** Dazu die
   Rückmeldung, was die Betriebsarten im Code heißen (Abschnitt 5).
3. **Prozesswärme PW6, echter Fehler:** Ein Profil ohne Typbezug brach die Profilschleife per `break` ab; die Profile
   davor blieben aufaddiert, der Wärmezweig wertete die Rückgabe nicht aus und rechnete mit einer
   reihenfolgeabhängigen Teilsumme weiter.

## 2 Behebung

**(1) Autarkie 0 kWh.** `StromspeicherStammCtrl.KapazitaetJeProjekt` liefert ohne Speicher im Projekt
`KAPAZITAET_OHNE_SPEICHER_KWH` = 0 (vorher `KAPAZITAET_RUECKFALL_KWH` = 5); mit Speicher unverändert die Summe der
Speicheranlagen. Die Speicherengine (`Dauernutzung`) rechnet mit 0 kWh ohne Division durch die Kapazität (Vollzyklen
nur für `C_nutz > 0`): Entladung 0, Autarkie = Direktverbrauch / Last. `AutarkieDaten.OhneStromspeicher`
(Kapazität ≤ 0) lässt das Blatt „Ergebnis“ unter dem Feld sagen: „Ohne Stromspeicher gerechnet (0 kWh). Eine
Kapazität im Feld zeigt, was ein Speicher bringen könnte.“ (`SIMERG_LBL_OHNE_STROMSPEICHER`, de/en). **Der Bericht
ist nicht betroffen:** Die Kennzahl `eff.autarkie` und das Bild `stand.bild.strombilanz_monate` lesen den Lauf
(Stromspeicher des Projekts bzw. keiner), nicht die Was-wäre-wenn-Kapazität der Kachel; der 5-kWh-Rückfall stand nur
in der Hülle der Ergebnisseite.

**(2) BHKW-Untergrenze.** Die Ebenen in `SimulationBHKW.Grenzfaktor` (neu, öffentlich, statisch):

| Rang | Quelle | gilt, wenn |
|---|---|---|
| 1 | Anlagenfeld `Tab_Energieanlagen.Grenzleistung` (BHKW-Zeile) | 0 < Wert ≤ 100 |
| 2 | Katalogwert `Tab_BHKW.Grenzleistung` des Projektmoduls | 0 < Wert ≤ 100 |
| 3 | Projektwert `Tab_Einstellungen.Leistungsgrenze` | sonst (0 = keine Untergrenze) |

Vorher lud `SimulationControl.BHKW_Liste_Laden` das Anlagenfeld in `bhkwGrenzL`, und `Moduldaten_Einlesen`
überschrieb es immer mit Katalog- oder Projektwert — das Feld war wirkungslos. Jetzt geht es in Prozent nach
`bhkw_anlagen_grenzleistung`; `Moduldaten_Einlesen` löst je Modul über `Grenzfaktor` auf. **Alle drei Betriebsarten**
nehmen `bhkwGrenzL[motor]`: `Motorlauf_Stromgefuehrt` und `Motorlauf_OhneEinspeisung` führten bis dahin nur den
skalaren Projektwert (`bhkwGrenzleistungAllgemein`). **Werte über 100 %** (Modul liefe nie an) sind ungültig: Warnung
im Simulationsprotokoll mit Modul, Wert, Ebene und der Untergrenze, die stattdessen gilt
(`SIMENG_BHKW_GRENZLEISTUNG_UNGUELTIG`), Rückfall auf die nächste Ebene; ein ungültiger Projektwert rechnet als
„keine Untergrenze“. Wo der Bestand schon galt, ist die Rechnung bitgleich (dieselbe Prozentzahl / 100).

**Verhalten unter der Grenze** (geprüft, unverändert): stündliche Rechnung, kein Takten, keine Mindestlaufzeit. Liegt
der Wärmeraum (wärmegeführt) bzw. der Reststrom (stromgeführt, ohne Einspeisung) unter
`Nennleistung × Untergrenze`, bleibt das Modul in dieser Stunde aus; zwischen Untergrenze und Nennleistung moduliert
es auf den Rest; darüber Volllast. Alle Schwellen tragen den Zahlenrand (`Rechenrand.SchwelleErreicht`).

**Prüfgrenze der Testdatenbank (nur berichtet, nicht geändert):** `Tab_BHKW_STAMM` führt 79 Zeilen, 66 mit
Grenzleistung > 0, davon 4 über 100 % — ID 145 (620 %), 146 (468 %), 147 (1 027 %), 148 (770 %). Kein Projektmodul
(`Tab_BHKW`, 8 Zeilen) und kein Anlagenfeld (8 BHKW-Zeilen) liegt über 100 %. Mit einer der vier Katalogzeilen
lief ein Modul bis RB1 still nur auf Volllast — der Teillastzweig war nie erreichbar; ein Projekt, das eine davon
übernimmt, rechnet jetzt mit Anlagenfeld bzw. Projektwert und bekommt die Warnung.

**(3) PW6.** `ProfilBedarf.Rechnen`: Ein Profil ohne Typbezug wird mit der benannten Warnung übersprungen (`continue`
statt `break`, Zähler `Uebersprungen`), die übrigen rechnen vollständig — wie bei fehlendem Kopfsatz oder Wochenprofil.
Die Rückgabe `false` heißt jetzt „mindestens ein Profil ohne Typ übersprungen“. **Dieselbe Stelle für Brauchwasser und
Stromverbraucher:** Die Profilroutine ist die gemeinsame Fabrik aller drei Bedarfsarten (`ProfilQuelle.Brauchwasser`,
`.Prozesswaerme`, `.Strom`); die Behebung gilt für alle drei. Brauchwasser- und Prozesswärmezweig werteten die
Rückgabe ohnehin nicht aus. Der **Stromzweig** (`SimulationStrombedarf.Stromprofil_Strombedarf_berechnen`) brach bei
`false` den ganzen Lauf ab (Rückgabe `null` → Fehlertext); er übernimmt jetzt dieselbe Regel und rechnet weiter. Die
drei Meldungstexte (`SIMENG_*_TYP_UNDEFINIERT`, de/en) sagen „wird übersprungen … die übrigen werden vollständig
gerechnet“ statt „wurde abgebrochen“.

## 3 Tests

- `EPOS.Kern.Tests/BhkwLeistungsgrenzeTests` (+16 Fälle): Rangfolge Anlage → Katalog → Projekt (sechs Fälle), Werte
  über 100 % benannt übersprungen (vier Fälle), Projektwert über 100 % → 0 mit Warnung, Anlagenfeld schlägt Katalog im
  Lauf, stromgeführt und ohne Einspeisung bleibt das Modul unter dem Anlagenfeld (30 %) aus und moduliert ohne es auf
  2 kW, wärmegeführt bleibt es unter der Grenze aus, die vier Katalogzeilen über 100 % fallen auf den Projektwert.
- `EPOS.Kern.Tests/ProfilOhneTypTests` (neu, 4 Fälle): Prozesswärme 1041 mit einem zweiten Profil ohne Typ vor bzw.
  hinter „Hotel_1“ — in beiden Reihenfolgen dieselbe Summe wie ohne, Warnung mit dem Namen; Stromverbraucher 1047 mit
  einem Profil ohne Typ vor dem eigenen — keine `null`, dieselbe Summe; die Testdatenbank führt keine zugeordnete
  Prozesskopie ohne Typ.
- `EPOS.Kern.Tests/AutarkieOhneStromspeicherTests` (neu, 2 Fälle, Lauf über die Hülle): 1045 (PV, kein Speicher)
  Kapazität 0, `OhneStromspeicher`, Speichernutzen 0, Autarkie endlich und > 0; die alte Vorbelegung 5 kWh liegt
  darüber. 1046 (vier Speicher) behält die Summe als Vorbelegung.
- `EPOS.Kern.Tests/SimulationErgebnisSqlTests`: `KapazitaetJeProjekt` ohne Speicher 0 statt 5 kWh (Projekt 999999,
  1018, ohne Projekt), mit Speicher die Summe (1017: 12,8 kWh).

**Anker alt → neu** (fachlich, Befund (2) an Projekt 1018):

| Test | Größe | alt (R30) | neu (R31) |
|---|---|---|---|
| `BhkwEinspeisungAusweisTests` (Theorie 1018, Stundenformel) | Einspeisung BHKW MWh/a | 27,4575 | 25,5543 |
| `BhkwNetzbezugKlemmeTests` (drei Fälle) | BHKW-Strom bzw. KWK-Einspeisung MWh/a | 27,4575 | 25,5543 |
| `BhkwNetzbezugKlemmeTests` | BHKW-Überschuss der PV kWh/a | 27 457,510347756746 | 25 554,297666056369 |
| `BhkwNetzbezugKlemmeTests` | CO₂ gesamt t/a | 25,116 | 24,5304 |
| `StromStufeneingangKlemmeTests` | BHKW-Überschuss der PV kWh/a | 27 457,510347756746 | 25 554,297666056369 |
| `BhkwStromdeckungTests` | Untergrenze des BHKW-Stroms MWh/a | > 27 | > 25 |

Anker der Autarkie (Befund (1), nicht in der Basis): Projekt 1045 Autarkie PV 10,2204 % → 8,7715 %, Speichernutzen
456,53 → 0 kWh.

## 4 Basis R31 und A/B gegen R30

A/B mit `EPOS.Referenzlauf` (Linux) über alle sechzehn Projekte: **14/16 PASS**, 477/487 CSV byte-gleich. Abgewichen
sind allein **1018** (3 165 Werte) und **1049** (4 338 Werte), je fünf Dateien (`aggregate.csv`, `bhkw_waerme.csv`,
`bhkw_strom.csv`, `bhkw_restwaerme.csv`, `kessel_leistung.csv`) und je 44 Skalare. Ursache allein Befund (2): Beide
pflegen im Anlagenfeld 35 %, im Projekt 30 %, im Katalog nichts — sie rechnen jetzt mit 35 statt 30 %. Befund (1)
steht in keiner CSV, Befund (3) trifft kein Referenzprojekt.

| Projekt | Größe | R30 → R31 | Grund |
|---|---|---|---|
| 1018 | BHKW-Wärme MWh/a | 58,32 → 54,28 | Untergrenze 35 statt 30 %: weniger Teillaststunden |
| 1018 | BHKW-Strom MWh/a | 27,46 → 25,55 | ebenso |
| 1018 | BHKW-Betriebsstunden h/a | 1 893,62 → 1 762,37 | ebenso |
| 1018 | Kesselwärme MWh/a | 11,09 → 15,14 | der Kessel deckt die Stunden, in denen das BHKW aussetzt |
| 1018 | Kesselstarts | 6 937 → 8 149 | mehr Laufstunden des Kessels (5 259 → 5 663) |
| 1018 | CO₂ BHKW / Kessel t/a | 22,34 / 2,78 → 20,79 / 3,74 | Brennstoff folgt der Wärme |
| 1049 | BHKW-Wärme MWh/a | 52,00 → 49,09 | wie 1018 |
| 1049 | BHKW-Strom MWh/a | 24,48 → 23,11 | wie 1018 |
| 1049 | Kesselwärme MWh/a | 6,32 → 9,23 | wie 1018 |
| 1049 | Kesselstarts | 2 372 → 3 242 | wie 1018 |
| 1049 | CO₂ BHKW / Kessel t/a | 19,92 / 1,52 → 18,80 / 2,22 | wie 1018 |

**Gegenprobe:** BHKW- und Kesselwärme zusammen bleiben gleich (1018: 69,41 → 69,42 MWh/a, 1049: 58,32 → 58,32 MWh/a,
Rundung der zweiten Stelle). Byte-gleich bleiben 1017 und 1047 (Anlagenfeld 30 % = Projektwert), 1030 (kein
Anlagenfeld, Katalog 15 %) und 1024 (kein Anlagenfeld, Projekt 10 %). Alle BHKW-Referenzprojekte rechnen wärmegeführt
(`Betriebsart` 0 oder leer) — die stromseitigen Fahrweisen verschieben keine Zahl der Basis.

**Eingefroren:** `Referenzlaeufe/2026-10-02_R31_Rechenwegbefunde`, sechzehn Projekte, 487 CSV, 3 080 Skalare, auf
Linux gegen die Testdatenbank `a50f1f49…` (Schemastand 159). Ein zweiter Lauf 487/487 CSV byte-gleich; gestört
(`--stoerung ulp`) gegen ungestört 16/16 PASS, 480/487 CSV byte-gleich — dieselben sieben Dateien wie mit R30
(Heizstab 1007/1046, Kessel 1024, Quellpuffer 1042, BHKW-Restwärme 1018, Wärmepumpe 1045). Auf `origin` lag keine R31
(geprüft nach `git fetch`); der Entwurf KP3 (`Dokumentation/aktuell/Gebaeudesimulation/2026-10-02_Entwurf_KP3.md`)
plant sein Einfrieren als „R31“ und nimmt nun die nächste Nummer.

## 5 Betriebsarten im Code (Rückmeldung zum Entscheid)

Die Betriebsart ist `Tab_Einstellungen.Betriebsart` (`KonfigurationCtrl.cs:151`), über `SimulationRunner.cs:229`
nach `SimulationControl.modeBHKW` und `SimulationBHKW.modeBHKW`; sie gilt für das ganze Projekt. Die Weiche ist
`SimulationBHKW.Fahrweise_Stunde` (`SimulationBHKW.cs:1846`), je Stunde aus `Stunde_Bedarf` (`:1472`) mit dem offenen
Wärmebedarf der Stufe und dem freien Raum der Puffersenke.

- **0 / leer — wärmegeführt** (`Motorlauf_Waermegefuehrt`, `:669`). Die Module folgen dem Wärmeraum (Bedarf + freier
  Puffer): Volllast, wenn der Raum die thermische Nennleistung trägt (`:702`); Teillast genau auf den Raum, wenn er
  zwischen Untergrenze und Nennleistung liegt (`:722`); sonst aus. Strom ist Koppelprodukt nach der Stromkennzahl. Der
  Strombedarf spielt keine Rolle.
- **1 — stromgeführt** (`Motorlauf_Stromgefuehrt`, `:764`; Aufruf `:1855`). Die Module folgen dem Reststrom der
  Stunde: Volllast, wenn er die elektrische Nennleistung erreicht (`:786`); Teillast genau auf den Reststrom zwischen
  Untergrenze und Nennleistung (`:803`); sonst aus. Wärme ist Koppelprodukt; was über den Wärmebedarf hinausgeht, geht
  in die Ladephase der Puffer und, wenn dort nichts mehr aufnimmt, in den Wärmeüberschuss (verworfen, Brennstoff
  verbraucht). Eine Speichergrenze gibt es in der Zuschaltung nicht.
- **2 — ohne Einspeisung** (`Motorlauf_OhneEinspeisung`, `:839`; Aufruf `:1870`). Zwei Durchläufe: zuerst
  wärmeseitig (W1 `:864`, W2 `:912`), dann stromseitig (S1 `:969`, S2 `:992`, S3 `:1020`). In beiden gilt die
  Zero-Export-Bedingung: Der Strom des Moduls darf den Reststrom nicht übersteigen — reicht der Strombedarf nicht für
  die Untergrenze, bleibt das Modul aus, auch wenn Wärme gebraucht würde; die Wärme darf den Wärmeraum nicht
  übersteigen.

Die Untergrenze (`bhkwGrenzL[motor]`, Abschnitt 2) ist in allen drei die Schwelle zwischen Teillast und „aus“ —
wärmegeführt gegen den Wärmeraum, stromgeführt gegen den Reststrom, ohne Einspeisung gegen beide.

## 6 Einfrierbegründung

Neu eingefroren, weil der Rechenweg der BHKW-Untergrenze sich gewollt ändert (Befund (2)) und die Abnahme der
Vergleich gegen die Basis ist: Die Abweichung von 1018 und 1049 ist der Zweck der Behebung, begründet in
`Referenzlaeufe/LIESMICH.md` (Abschnitt „Aktuelle Basis“). Die Testdatenbank ist byte-gleich; gewechselt hat allein,
welcher gesäte Wert gilt — das Anlagenfeld statt des Projektwerts. **Neue Einfrierregel** in `CLAUDE.md`
(Regressionsnetz) und `Referenzlaeufe/LIESMICH.md`: „gesäte BHKW-Grenzleistungen der Referenzprojekte“ (Anlagenfeld,
Katalogspalte des Projektmoduls, Projektwert).

## 7 Gate

Gate auf dem Merge-Stand: siehe Statuszeile der Hauptsitzung.

Im Worktree: Kern-Filter 0 Fehler, Windows-Schale (`EnableWindowsTargeting`) 0 Fehler, voller Testlauf des
Kern-Filters: UI 7 239, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 grün, Kern 9 898 von 9 910 grün und 11
rot — zehn Fälle `RechenrandFahrweisenTests` (der Reflexionsaufruf reichte die Untergrenze noch skalar; nachgezogen
in `8a963701`) und `BerechnungshilfeEinbettungTests` (Bau vor der Änderung der Hilfeseiten); nach Neubau 83/83 grün.
Abnahmefilter (Bhkw, BHKW, Autarkie, Stromspeicher, Prozess, Profil, Referenz, Anker, Wache, Rechenrand) 2 177/2 177
grün, `SqlDialektPruefer` 0 Befunde, Referenzlauf der sechzehn
Projekte gegen R31 16/16 PASS, 487/487 CSV byte-gleich.

## 8 Wiki und Logbuch

Repo-Quellen nachgezogen, wo eine Aussage falsch geworden wäre: `Projekte/Wiki/Programm Dokumentation - BHKW.wiki`
(„Welche Untergrenze gilt“, Feld „Untere Grenzleistung“ des Katalogs) und `Projekte/Wiki/Programm Dokumentation -
Photovoltaik.wiki` (Autarkie-Analyse ohne Stromspeicher 0 kWh); die Berechnungshilfe `BHKW.wiki` (Gleichung (2) mit
drei Ebenen, alle Betriebsarten, Werte über 100 %), `Stromspeicher.wiki` (Autarkie ohne Speicher),
`Prozesswärme.wiki`, `Brauchwasser.wiki` und `Strombedarf.wiki` (Profil ohne Typ übersprungen). Tabu-Grep ohne Treffer
in den geänderten Absätzen. Upload ins Wiki gebündelt (Konzept Hilfesystem 13.3).

Logbuch (Vorschläge, Version beim Anwender zu erfragen):
- „Die Autarkie-Analyse rechnet ohne Stromspeicher im Projekt mit 0 kWh und sagt es.“
- „Die untere Grenzleistung im Projekt-BHKW gilt vor Katalog- und Projektwert, in allen Betriebsarten.“
- „Ein Bedarfsprofil ohne Typ wird mit Meldung übersprungen; die übrigen Profile werden vollständig gerechnet.“

## 9 Offen und fraglich

- **Stromzweig ohne Abbruch:** Die Behebung von PW6 lässt auch den Stromzweig ein Profil ohne Typ überspringen, statt
  den Lauf abzubrechen (gemeinsame Fabrik, eine Regel). Wer den Abbruch beim Strom behalten will, entscheidet das neu.
- **Vier Katalogzeilen über 100 %** (`Tab_BHKW_STAMM` 145–148) bleiben; sie zu berichtigen wäre eine Katalogpflege mit
  Herstellerangabe.
- **Betriebsart als Einfrierregel:** Die Untergrenze wirkt jetzt in allen Betriebsarten; die Betriebsart eines
  Referenzprojekts ist noch keine Einfrierregel (heute alle wärmegeführt).
- **KP3:** Der Entwurf plant „R31“; die Nummer ist mit dieser Basis vergeben.

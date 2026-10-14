# Protokoll KP3 RP1 — A/B-Protokoll des Referenzprojekts 1051 (03.10.2026)

**Auftrag.** Stufe KP3, Welle RP1: Referenzprojekt 1051 (Konditionierung mit Aufheizoptimierung) in der
Testdatenbank, gesät über die Programmwege (Festlegungen 31–33; Entscheid E58 F5 (a), F7 (c), F8 (a)). Dieses
Protokoll ist die Vorlage für den Entscheid **P14** des Anwenders (Reserve ρ der Aufheizleistung). RP1 friert nicht
ein, die Basis **R34** friert RP2. Die Basis R33 bleibt unverändert. Kein Schemaschritt, keine Änderung am
Rechenweg. Nicht in RP1: der Ordner `Projekt_1051` in der Basis, die CI-Auswahl, die Einfrierregel in der
Wurzel-`CLAUDE.md` (RP2b).

Dieses Protokoll gilt für RP1a (Bauwahl, Saat, Zählnachzüge) und RP1b (Wache, Plattformprobe, ρ_min-Messung).

## 1 Commits

Zweig `kp3-rp1`, bis `5903d3b9e`:

| Commit | Inhalt |
|---|---|
| `8040f68b6` | Bauwahlprobe `KonditionierungBauwahlprobeTests`, Saatskript `referenzprojekt_1051_konditionierung.cs` mit Bauplan `_bauplan.cs` (Klasse `Konditionierungsprojekt1051`), InternalsVisibleTo-Zeile in `EPOS.Kern.csproj` |
| `b5d75b27c` | Testdatenbank mit 1051 und Referenzbau 289 (LFS-OID `0c1e193edc…`, 83 189 760 Byte) |
| `7a5dea79a` | Zählnachzüge, Hilfe `Konditionierungsbestand` |
| `0052db210` | Wache `KonditionierungReferenzprojektWacheTests` mit Attribut `BasisMit1051Fact`; `Referenzlauf/Ergebnisexport.cs` und `Protokoll.cs` im Testprojekt verlinkt |
| `eccf7980c` | Plattformprobe: Fall 1051/10657 in `AufheizDeterminismusTests`; `Aufheizabdruck.Lauf` nimmt die Aufheizvorgabe des Projekts |
| `1c09a7e56`, `5903d3b9e` | Messharness `AufheizReserveMessungTests` (Attribut `MessungFact`, Trait Kategorie=Messung) |

## 2 Bauwahlprobe

Bemessung (b) 2 K, täglich, Reserve leer. Gesucht ist ein Katalogbau, dessen Nachtabsenkung eine Rampe verlangt
und dessen Bemessungsfall erreichbar ist.

| Katalogbau | Fläche m² | Rampentage | längste Rampe h | t_auf,max h | W1/W2/W3 | erreichbar | P_auf kW | Spitze mit/ohne Rampe kW | Heizwärme mit/ohne MWh | Urteil |
|---|---|---|---|---|---|---|---|---|---|---|
| Verw_I_40 | 572 | 100 | 13 | 26 | 0/1/0 | ja | 32,03 | 30,96/42,83 | 25,256/24,718 | tauglich, **gewählt** |
| Verw_I_33 | 905,3 | 132 | 13 | 30 | 0/2/0 | ja | 45,32 | 44,63/62,73 | 37,749/36,683 | tauglich |
| EFH-A-TS-212 (Gegenprobe) | 74 | 5 | 5 | 8 | 0/0/0 | ja | 10,07 | 9,63/11,41 | 11,899/11,894 | nicht tauglich |

Der Rückfall über `Heizleistung_Max` oder die Art „fest“ war nicht nötig.

## 3 Saat und gesäte Zellen

Die Testdatenbank wurde ohne 1052 eingesetzt, 1051 als Kopie von 1007 gesät (fällt auf 1051), danach 1052
erneut (Kopie von 1018). Beide Skripte zeigen danach `--trocken` 0 Änderungen; `integrity_check` ok,
`foreign_key_check` leer; der Abdruck von 1007 und aller übrigen Katalogbauten und Vorlagen ist unverändert.

- **Referenzkatalogbau „Referenzbau Konditionierung“ (ID 289)**, dupliziert aus Verw_I_40 (275 Bestandssätze
  unberührt): `Kuehlung_Aktiv` 1, `Sommerlueftung` 1; Ferien Tag 357–6 und 213–226; Heizperiode SAISON 274–120;
  „Büro“ in allen fünf Größen (Lüftung zuerst „aufteilen“: 0,6 → 0,3 Infiltration + 0,3 Nutzer); Nachtzeile der
  Lüftung 2,0 1/h von 18 bis 7 Uhr mit `Bedingt_K` 2 K, Wochenende und Ferien 0,1 1/h; ein OK über
  `KatalogSchreiben`. Ergebnis: 5 Kalender („aus Vorlage Büro“, Kultur de-DE), 16 Vorgaben, 54 Perioden.
- **Projekt 1051**: ein Gebäude 10657 über die Gebäudeliste aus dem Referenzbau, Zuordnung 572 m²;
  Aufheizvorgabe STUNDE_ABZUG 2 K, Reserve leer, Art und Aufschlag leer, keine manuelle Aufheizzeit;
  Kühlbetrieb aus; `Kosten_Geaendert` NULL; Datum 2026-10-03.

Lauf 1051: Heizwärme 25,256 MWh; Spitze 30,96 kW (Tagesmittel 17,49, 95 % 17,71); Rampentage 100;
t_auf,max 26 h; P_auf 32,03 kW bei −20,17 °C; Aufheizstunden 291; längste Rampe 13 h; W1/W2/W3/W4 0/1/0/1;
Nachtauskühlstunden 589; Sommerlüftungsstunden 1 724.

## 4 Zählnachzüge

32 rote Tests vor den Nachzügen:

- Katalogzahl 275 → 276 (Katalogpflege 2, Katalogverweis, Katalogreparatur 284/276, drei Anschlusslängenreparaturen);
- Projektgebäude 31 → 32, Nachtrag 27 → 28; Kesselkopien 23 → 24 und 24 → 25;
- Investitionspositionen 146 → 154 und 139 → 147;
- Energiestandard NIEDRIGENERGIE 34 → 35, Baualtersklasse der Projektkopien zusätzlich J1;
- „alles aus außer 1051/1052“ (Konditionierung, Nachtzeit, Kühlung, Gebäudespalten, Aufheizvorgabe);
- Katalogkalender: fünf am Referenzbau; Median der Gebäudevorgaben ohne Referenzbau;
- N-AH4: Schranke jetzt n_F > n_max (Lockerung, Befund RP1a-3).

## 5 Wache `KonditionierungReferenzprojektWacheTests`

5 bestanden, 1 übersprungen.

| Fall | Gegenstand |
|---|---|
| (a) | nur 1051/10657 und Referenzbau 289 tragen Gebäude- bzw. Katalogkalender, fünf Größen, bedingte Nachtzeile, `Sommerlueftung` 1, Ferien, Gebäudeherkunft aus 289; Aufheizoptimierung nur in 1051 und 1052 an |
| (b) | `Pruefen` ohne Abweichung; ausdrücklich: 16 Vorgabezeilen (Lüftung NACHT 2,0 1/h 18–7 Uhr 2 K; WOCHENENDE und FERIEN 0,1 1/h; SAISON 274–120), fünf Kalender „aus Vorlage Büro“ in Wochenform, Geräte-Nennwert 3232, Perioden Ferien 357–6 und 213–226, Heizsoll-Betriebspause 121–273, 9 Feiertage „wie Sonntag“ je Größe, 26 Gebäudezellen (`Kuehlung_Aktiv` 1, 572 m², keine manuelle Aufheizzeit), Einstellung Kühlbetrieb aus, Bemessung (b) 2 K, Reserve leer (wirksam 20 %), täglich, kein Aufschlag |
| (c) | Nachbau auf Arbeitskopie (Projekt 1053, Katalogbau 290) bitgleich: Projekt 9 376 Zeilen / 507 526 Zeichen, Katalogbau 80 Zeilen / 6 708 Zeichen |
| (d) | zwei Läufe bytegleich: 36 Dateien, 4 749 843 Byte |
| (e) | ausgelieferte Vorlage „Büro“ unverändert (NACHT 0,1, `ReadOnly` 1, keine Vorlage mit `Bedingt_K`) |
| (f) | Abgleich gegen die Basis übersprungen („Basis R34 noch nicht eingefroren“); Gegenprobe mit eingelegtem `Projekt_1051` grün, mit verfälschtem Wert rot |

## 6 Plattformprobe

Fall 1051/10657 in `AufheizDeterminismusTests`, gestörter Lauf `ulp`: je Sprung gleiches n, alle 146 Sprünge;
Verteilung 1×46, 2×29, 3×22, 4×24, 5×14, 6×2, 7×3, 9×1, 10×1, 11×2, 13×1, 14×1. Ergebniszeile und Export
gleich, die Heizreihe unterscheidet sich im Hash. Gebäude BEMESSEN, Tage 100, W1 0, W2 1, W3 0, W4 1, Σ 291 h,
längste Rampe 13 h; zwei Läufe und de-DE/en-US bitgleich (28 Zeilen).

## 7 Gates

| Prüfung | Ergebnis |
|---|---|
| Build (RP1a) | 0 Fehler |
| gefilterte Tests (RP1a) | 1 755 grün |
| Auslieferungsvorlage.Tests | 47/47 |
| Referenzlauf 16 Projekte gegen R33 | 16/16 PASS, 487/487 byte-gleich |
| Referenzlauf 1051 zweimal | PASS, 36/37 byte-gleich (nur `protokoll.txt` weicht ab) |
| SqlDialektPruefer | 0 Fundstellen |
| RP1b: Filter Konditionierung, AufheizDeterminismus, ZonenReferenzprojekt | 347 bestanden, 1 übersprungen, Build 0 Fehler |

## 8 Messung ρ_min je Gebäude (Vorlage für P14)

Messharness `AufheizReserveMessungTests`: 327 Läufe in 32 s. Aufruf:
`EPOS_MESSUNG=1 dotnet test EPOS.Kern.Tests -c Release --filter "FullyQualifiedName~AufheizReserveMessung"`, Ziel über
`EPOS_MESSUNG_ZIEL`. Kriterium: W1 = 0 und W3 = 0; Bemessung (a) und (b) liefern dasselbe ρ_min. ρ_bem ist die
kleinste Reserve mit erreichbarem Bemessungsfall.

| Projekt/Gebäude | ρ_min % | ρ_bem (a) % | ρ_bem (b) % | t_auf,max (a) h | Rampentage | Spitze mit/ohne kW | Wärme mit/ohne MWh |
|---|---|---|---|---|---|---|---|
| 1007/10614 und 1046/10652 | 1,875 | 1,875 | 6,875 | 38 | 5 | 39,18/44,22 | 68,990/68,974 |
| 1008/10576 | 3,125 | 3,125 | 8,125 | 46 | 15 | 52,73/63,07 | 89,209/89,130 |
| 1008/10577 | 1,875 | 1,875 | 6,875 | 38 | 5 | 8,53/9,62 | 15,015/15,012 |
| 1017/10599 | 3,125 | 3,125 | 7,5 | 43 | 25 | 51,94/63,16 | 90,299/90,194 |
| 1018/10632 und 1049/10655 | 2,5 | 3,125 | 9,375 | – (47 bei ρ_bem) | 120 | 30,49/37,36 | 68,673/68,252 |
| 1023, 1024, 1039/10644, 1050 | 2,5 | 2,5 | 7,5 | 44 | 8 | 273,33/308,51 | 450,111/449,903 |
| 1039/10642 | 1,875 | 1,875 | 6,875 | 43 | 2 | 21,25/22,67 | 48,024/48,017 |
| 1039/10643 | 0,625 | 0,625 | 5,625 | 37 | 1 | 47,34/49,20 | 99,110/99,100 |
| 1041, 1042, 1045 | 1,25 | 1,25 | 6,25 | 28 | 1 | 37,52/39,68 | 75,949/75,941 |
| 1047/10653 (gekoppelt AK1, keine Rampe) | 0 | 0 | 0 | – | 0 | 45,64/45,64 | 82,748/82,748 |
| 1051/10657 | 8,75 | 8,75 | 13,75 | 45 | 134 / 135 (b) | 30,96/42,83 | 25,711 (b: 25,717)/24,718 |
| 1052 Gästezimmer | 4,375 | 4,375 | 10 | 36 | 136 | 17,72/22,09 | 37,354/37,276 |
| 1052 Gastronomie und Verwaltung | 6,875 | 6,875 | 13,125 | 46 | 245 / 246 (b) | 11,67/18,45 | 20,384 (b: 20,391)/19,247 |

Bei 20 % sind W1 und W3 überall 0.

**Empfehlung: 20 % behalten.** Das größte ρ_min liegt bei 8,75 % (1051), mit ρ_bem bei 13,75 % (1051, Bemessung
(b)); beide bleiben unter 20 %.

Entscheid des Anwenders (03.10.2026): keine pauschale Programmvorgabe, die Reserve ist immer Nutzereingabe; bleibt sie leer, gilt 20 % mit Laufhinweis (E64).

## 9 Befunde

**Aus RP1b**

1. **Bemessung ändert P_auf nicht.** P_auf = (1 + ρ)·Φ_stat bei der kältesten Stunde; Bemessung (b) prüft nur die
   Erreichbarkeit bei T_a − 2 K. Darum sind ρ_min, W1 und W3 für (a) und (b) gleich; bei ρ_min ist der Zustand
   für (b) überall UNERREICHBAR, deshalb wurde ρ_bem ergänzt. Für P14 zu klären: Bemessung nach W1/W3 oder nach
   „Bemessungsfall erreichbar“ — beide Maße liegen unter 20 %.
2. **1047 gekoppelt:** 0 % ohne Aussage (keine Rampe).
3. **Gleiche Katalogbauten ergeben gleiche Zahlen** (1007/1046, 1018/1049, 1023/1024/1039/1050,
   1041/1042/1045): weniger unabhängige Gebäude als Projekte.
4. **Monotonie angenommen.** Der Harness nimmt an, dass W1 und W3 in ρ monoton fallen (aufsteigende Suche bis
   zum ersten Treffer).
5. **Heizsoll-Kalender von 1051 ohne Ferienperiode.** Die Ferien-Sollwerte stehen nur in den Gebäudespalten
   (16 °C); die übrigen vier Größen tragen Ferienperioden. So gesät und von der Wache gehalten; zu prüfen, ob
   gewollt.

**Aus RP1a**

1. **InternalsVisibleTo nötig:** `GebaeudeStammCtrl` und `WizardCtrl` sind `internal`; fachlich keine Kernänderung.
2. **Bestandsfelder-Weg** (Feldsatz → Modell) liegt nur in der UI-Hülle; im Bauplan als `Bestandsfelder`
   nachgebildet.
3. **N-AH4 kippte an 1051** (P/Φ_stat 1,005, a = 0, T_a 0 °C erreicht die Stufenzahl nicht in n_max 2 000);
   Schranke auf n_F > n_max gelockert.
4. **Mehrwärme der Rampe:** +0,54 MWh (+2,2 %) Jahresheizwärme; die längste Rampe von 13 h deckt die
   Nachtabsenkung von 13 h ganz ab, W2 „Absenkung weitgehend wirkungslos“ bei Bemessungswetter.
5. **Kandidaten tragen `ReadOnly` = 0;** die Vorprüfung verlangt nur Eindeutigkeit.
6. **Auslieferungsvorlage** löscht alle Projekte; 1051/1052 werden nicht ausgeliefert, der Referenzbau liegt nur
   in der Testdatenbank.

## 10 Festlegungen

- Gewählt: Verw_I_40 als Referenzbau, Bemessung (b) 2 K, Reserve leer (wirksam 20 %), täglich, Kühlbetrieb aus.
- Saatreihenfolge: zuerst 1051, dann 1052.
- Wiki: keine Änderung.

## 11 Offen (RP2b)

- Ordner `Projekt_1051` in die Basis **R34**; Fall (f) der Wache wird damit scharf.
- CI-Auswahl mit 1051 als achtem Projekt (1030, 1007, 1017, 1045, 1046, 1047, 1049, 1051).
- Einfrierregel „gesäte Konditionierungsdaten“ in die Wurzel-`CLAUDE.md` (Regeltext steht in
  `Referenzlaeufe/LIESMICH.md`).
- P14: Reserve ρ (Empfehlung 20 % behalten; Maß für die Bemessung klären, Befund RP1b-1); Heizsoll-Ferien von
  1051 (Befund RP1b-5).

Dateien: `Referenzlaeufe/Skripte/referenzprojekt_1051_konditionierung.cs`,
`Referenzlaeufe/Skripte/referenzprojekt_1051_bauplan.cs`,
`EPOS.Kern.Tests/KonditionierungBauwahlprobeTests.cs`,
`EPOS.Kern.Tests/KonditionierungReferenzprojektWacheTests.cs`,
`EPOS.Kern.Tests/AufheizReserveMessungTests.cs`, `Referenzlaeufe/LIESMICH.md`.

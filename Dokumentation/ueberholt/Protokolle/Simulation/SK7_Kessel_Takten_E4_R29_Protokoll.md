# SK7 — Kessel-Kennlinie, Etappe E4: Takten, Starts nach Konzept 4.2, Hinweis ohne Kennlinie, Basis R29

Stand: 30.09.2026 · Zweig `ios_migration_september` · Opus-Agent im Worktree, Zweig `kessel-e4` ab `f0ac5bfa`
(`kessel-e3` mit E1–E3, Schemastand 158, Basis R28). **Kein Schemaschritt, keine Änderung der Testdatenbank.**
Konzept [`Konzept_Kessel_Kennlinie_EPOS-Plan.md`](../../../aktuell/Konzept_Kessel_Kennlinie_EPOS-Plan.md) (4.2, 4.3, 5,
6, 7.1); Vorgänger [`SK6_Kessel_Brennwert_E3_R28_Protokoll.md`](SK6_Kessel_Brennwert_E3_R28_Protokoll.md).

Commits:
- `21aef314` Kern (Takten, Startzählung, Taktwerte), Ergebnis und Reiter, `aggregate.csv`, Hinweis im Kesseldialog,
  Ressourcen, Tests
- `0e31532b` Basis R29 eingefroren, R28 archiviert
- `39179c7e` gepinnte Anker nachgezogen: Kapitalwerte 1030 (drei Szenarien) und 1048, Zerlegung der Anker 1030,
  CO₂-Summe 1018 (siehe 6)
- Papiere (Basisname, Konzept, Wiki-Quelle, dieses Protokoll, Index) im Commit danach; Merge
  `origin/ios_migration_september` und Gate-Zahlen danach

## 1 Auftrag und Entscheide

Auftrag „Kessel-Kennlinie, Etappe E4 Takten, dazu der Hinweis ‚Brennwertkessel ohne Kennlinie‘ im Kesseldialog, neue
Referenzbasis R29“. Es gelten:

- **Konzept 4.2 und 7.1 (Entscheid F1):** Mit der Mindestleistung P_min (gepflegt, sonst Normvorgabe) und
  0 < Q < P_min taktet der Kessel; Starts der Stunde = min(60/t, ⌈Q/(P_min · t/60)⌉), sonst ein Start nach einer
  Stillstandsstunde; Brennstoff += Starts × Anfahrverlust. Normvorgaben: Mindestleistung 30 % (Gas-Brennwertkessel)
  bzw. 60 %, Anfahrverlust 0,002 h × Nennleistung, Mindestlaufzeit 10 min. Elektrokessel ohne Taktmodell.
- **Anwenderentscheid 30.09.2026 zum Hinweis: „Nur im Kesseldialog“.** Ein ruhiger Hinweis neben dem Schalter
  „Brennwertkennlinie“ (Projekt und Katalog), wenn `Brennwert` = 1 und die Brennwertkennlinie aus ist; keine Warnkarte
  vor dem Lauf.
- Neue Betriebsschwellen über `Rechenrand.SchwelleErreicht`; keine `(int)`-Abschneidung auf einer Rechengröße.

## 2 Rechenweg (`Kesselkennlinie`, `SimulationSPK`)

- **Taktwerte** (`Kesselkennlinie.MindestleistungWirksam`, `.AnfahrverlustWirksam`, `.MindestlaufzeitWirksam`): ein
  gepflegter Wert geht vor, auch eine gepflegte 0 (Mindestleistung 0 = moduliert bis null, Anfahrverlust 0 = Starts
  ohne Brennstoff, wie Konzept 4.2 „auch ohne Anfahrverlust ausgewiesen“); negativ oder nicht endlich nimmt die
  Vorgabe; die Mindestleistung höchstens die Nennleistung, die Mindestlaufzeit 1 … 60 min (Raster einer Stunde).
  „Gas-Brennwertkessel“ ist Bauart Brennwert (dieselbe Regel wie die Normvorgabe von η₃₀) mit einem Gas
  (`Tab_Brennstoff_Stamm` 1–5, 14). `SimulationSPK.TaktwerteBilden` bildet sie einmal je Lauf in
  `Kesseldaten_Einlesen` und meldet sie samt Herkunft im Laufprotokoll (`SIMENG_KESSEL_TAKTWERTE`).
- **Taktet** (0 < Q < P_min): Q ist die brennstoffbasierte Wärme der Stunde (`_kesselStunde`, dieselbe wie für die
  Laststufe). Beide Enden tragen den Zahlenrand: unter `Rechenrand.ABSOLUT` kein Lauf, und wer P_min bis auf den Rand
  erreicht, moduliert (`!Rechenrand.SchwelleErreicht(Q, P_min)`).
- **StartsImTakt:** 60/t wird ganzzahlig gelesen, ⌊60/t⌋ — bei t = 7 min passen acht Mindestläufe in die Stunde,
  min(60/7, 9) wäre keine Startzahl. ⌈·⌉ entsteht als Zähler: der kleinste Start n, dessen Mindestläufe n · P_min · t/60
  die Wärme bis auf den Rand decken (`SchwelleErreicht(n · Mindestlauf, Q)`, Leserichtung vertauscht wie bei einer
  Untergrenze). Gegenprobe im Test: 0,1 + 0,2 kWh bei einem Mindestlauf von 0,1 kWh ergibt blank aufgerundet vier
  Starts, mit dem Rand drei.
- **Wie die Startzählung die bisherige ersetzt:** Außerhalb der Taktstunden bleibt ein Start genau ein Übergang aus →
  an (eine Laufphase). In einer Taktstunde ersetzt die Startzahl des Takts den Übergang; ist die Vorstunde ein
  Stillstand, ist der Übergang ihr erster Start. Damit gilt **Starts = Laufphasen + Σ über die Taktstunden
  (Starts der Stunde − 1, wenn die Vorstunde stand, sonst − 0)** — nie weniger als die Laufphasen, gleich ihnen ohne
  Taktstunde. Die Laufphasen werden weiter gezählt (`Laufphasen_Spk`, `Kessel[i].Laufphasen`).
- **Anfahrverlust:** Starts × Anfahrverlust je Start (`Anfahrverlust_KWh_Spk`) geht in den Brennstoff der Stunde und
  damit in Kesselverbrauch, Brennstoffzähler, Jahresnutzungsgrad und Emissionen; die Gasspitze nimmt denselben Wert
  (Konzept 4.1 Punkt 6). `BrennstoffBetrieb_KWh_Spk`, der mittlere Wirkungsgrad im Betrieb und die Aufteilung
  Teillast/Brennwert bleiben die Kennlinie ohne Anfahrverlust. Eine Laufstunde ohne Start (moduliert nach einer
  Laufstunde) addiert 0 und bleibt bitgleich.
- **Elektrokessel:** Konzept 7.1 („kein Taktmodell“) — `RechnetMitTakten` ist falsch, seine Starts sind seine
  Laufphasen, Anfahrverlust 0, Taktwerte 0.

## 3 Oberfläche, Export, Hinweis

- **Reiter Heizkessel:** Gruppe „Betrieb“ mit „Taktstunden unter der Mindestleistung“ (h/a) und „Anfahrverlust“
  (kWh/a) bei Brennstoffkesseln (Hinweis `SIMERG_TIP_TAKTEN_SPK`); „Starts“ zählt die Starts nach 4.2 (Hinweis
  `SIMERG_TIP_BETRIEB_SPK` nachgezogen); die Kesseltabelle trägt je Kessel die Spalte „Starts [1/a]“, auch beim
  Elektrokessel. Der Hinweis zum Wirkungsgrad im Betrieb nennt „ohne Anfahr- und Bereitschaftsverlust“.
- **`aggregate.csv`** je Kessel: `Kessel[i].Laufphasen`, `.Taktstunden`, `.AnfahrKwh`, `.MindestleistungKw`,
  `.AnfahrverlustJeStartKwh`, `.MindestlaufzeitMin`; `Kessel[i].Starts` behält den Schlüssel und zählt nach 4.2.
- **`ParameterVerwendung`:** Mindestleistung, Anfahrverlust und Mindestlaufzeit sind gerechnet (SIM).
- **Hinweis „Brennwertkessel ohne Kennlinie“** (`HeizkesselKatalogDialog`, der einzige Eingabeweg der Kesseldaten —
  als eigenes Fenster im Katalog und als Überlagerung des Projektdialogs): eine Herleitungszeile unter dem Schalter
  (`epos-herleitung`, über die volle Breite, damit sie nicht neben einem fremden Feld steht), sichtbar bei
  `Brennwert` = 1 und ausgeschalteter Brennwertkennlinie; Text `HZKK_HINT_OHNE_KENNLINIE` über die Windows-Hülle, de/en.
- **Ressourcen** de/en: `SIMENG_KESSEL_TAKTWERTE`, `SIMENG_KESSEL_TAKTEN_BETRIEB`, `KESSEL_WERT_GEPFLEGT`,
  `KESSEL_WERT_VORGABE`, `SIMERG_LBL_TAKTSTUNDEN`, `SIMERG_LBL_ANFAHRVERLUST`, `SIMERG_TIP_TAKTEN_SPK`,
  `SIM_SPALTE_STARTS`, `HZKK_HINT_OHNE_KENNLINIE`; geändert `SIMERG_TIP_BETRIEB_SPK`, `SIMERG_TIP_KENNLINIE_SPK`.
  `Resource.Designer.cs` mit `Werkzeuge/ResourceDesigner/designer_neu.py schreiben` erzeugt.
- **Tests:** `KesselKennlinieTests` (Normvorgaben, gepflegte Werte samt 0, `Taktet` an beiden Rändern, Startzahl
  samt Deckel ⌊60/t⌋ und Zahlenrand, Elektrokessel; im Lauf 1050 Stunde für Stunde nachgerechnet, 1023 mit den
  Vorgaben, Mindestleistung 0), `KesselBereitschaftTests` (Verbrauch mit Anfahrverlust), `ParameterVerwendungTests`,
  `HeizkesselKatalogDialogTests` (Hinweis steht, fällt mit Kennlinie an oder ohne Brennwertkessel, übersetzbar),
  `ErzeugerReiterTests` (Taktzeilen, Spalte Starts).

## 4 Basis R29 und A/B gegen R28

`Referenzlaeufe/2026-09-30_R29_Kesseltakten/` — sechzehn Projekte, 487 CSV, **3 080 Skalare**, auf Linux eingefroren
gegen die Testdatenbank `5d59041f…` (Schemastand 158, unverändert). Zweiter Lauf 487/487 byte-gleich; gestört gegen
ungestört 16/16 PASS, 480/487 byte-gleich (dieselben sieben Dateien wie mit R28; `aggregate.csv` überall
byte-gleich).

A/B gegen R28 mit `--ohne` für die sechs neuen Schlüssel: **3/16 PASS** (1017, 1024, 1047 — Elektrokessel),
471/487 CSV byte-gleich; abgewichen ist allein `aggregate.csv`: `Kessel[0].Starts`, Brennstoff, Jahresnutzungsgrad,
Kesselemissionen, in neun Projekten die Gasspitze (+0,04 bis +1,44 kW). Keine Zeitreihe ändert sich.

| Projekt | P_min kW | Anfahrverlust kWh je Start | Laststufe | Laufphasen | Taktstunden | Starts R28 → R29 | Anfahrverlust kWh/a | Brennstoff MWh/a R28 → R29 | Nutzungsgrad % R28 → R29 |
|---|---|---|---|---|---|---|---|---|---|
| 1007, 1046 | 6,63 | 0,0442 | 0,23 | 259 | 1 323 | 259 → 4 276 | 189,0 | 10,01 → 10,20 (+1,90 %) | 90,43 → 88,75 |
| 1008 | 6,63 | 0,0442 | 0,38 | 260 | 1 385 | 260 → 5 248 | 232,0 | 22,86 → 23,09 (+1,01 %) | 89,89 → 88,99 |
| 1018 | 24,00 | 0,16 | 0,03 | 381 | 5 259 | 381 → 6 937 | 1 109,9 | 10,46 → 11,57 (+10,61 %) | 106,00 → 95,84 |
| 1023 | 5,79 | 0,0386 | 0,82 | 444 | 843 | 444 → 3 269 | 126,2 | 91,02 → 91,15 (+0,14 %) | 87,67 → 87,55 |
| 1030 | 660,00 | 4,40 | 0,36 | 398 | 3 012 | 398 → 8 402 | 36 968,8 | 5 203,20 → 5 240,16 (+0,71 %) | 103,84 → 103,11 |
| 1039 | 24,00 | 0,16 | 0,76 | 188 | 835 | 188 → 2 080 | 332,8 | 293,35 → 293,69 (+0,12 %) | 98,61 → 98,50 |
| 1040 | 24,00 | 0,16 | 0,09 | 252 | 2 113 | 252 → 4 921 | 787,4 | 15,27 → 16,06 (+5,17 %) | 105,96 → 100,77 |
| 1041 | 24,00 | 0,16 | 0,21 | 1 | 5 748 | 1 → 14 213 | 2 274,1 | 142,85 → 145,12 (+1,59 %) | 104,94 → 103,30 |
| 1042 | 36,00 | 0,24 | 0,05 | 208 | 2 954 | 208 → 4 793 | 1 150,3 | 18,21 → 19,36 (+6,32 %) | 104,00 → 97,82 |
| 1045 | 24,00 | 0,16 | 0,09 | 223 | 3 197 | 223 → 7 139 | 1 142,2 | 21,01 → 22,15 (+5,43 %) | 105,97 → 100,51 |
| 1049 | 24,00 | 0,16 | 0,06 | 488 | 1 329 | 488 → 2 372 | 379,5 | 5,96 → 6,34 (+6,38 %) | 106,00 → 99,66 |
| 1050 | 3,86 (gepflegt) | 0,10 (gepflegt) | 0,82 | 444 | 503 | 444 → 2 035 | 203,5 | 80,66 → 80,87 (+0,26 %) | 98,94 → 98,69 |
| 1017, 1024, 1047 | Elektrokessel | – | – | 651, 233, 11 | 0 | unverändert | 0 | unverändert | unverändert |

**Begründung.** Wärme und Laufstunden bleiben; es ändern sich nur die Starts und der Brennstoff dafür. Der
Anfahrverlust wächst mit der Überdimensionierung: Kessel mit einer mittleren Laststufe unter 10 % (1018, 1040, 1042,
1045, 1049) takten in den meisten Laufstunden und brauchen 5 bis 11 % mehr Brennstoff. 1018: 80 kW, im Mittel 2,4 kWh
Wärme je Laufstunde, ein Mindestlauf 24 kW × 10 min = 4 kWh — ein Start je Stunde mit 0,16 kWh, rund 6,7 % der Wärme.
Kessel mit hoher Laststufe (1023, 1039, 1050: 0,76–0,82) takten selten, +0,1 bis +0,3 %. 1041 läuft das ganze Jahr in
einer Laufphase und taktet in 5 748 Stunden. Die Platzhalter-Kessel mit η₁₀₀ = 1,0 fallen von rund 106 % auf 96 bis
101 % Jahresnutzungsgrad. Die Formel nimmt je Taktstunde die Höchstzahl (jeder Lauf genau eine Mindestlaufzeit): im
Mittel der Taktstunden 1,32 (1018) bis 3,75 (1008) Starts. **Gegenprobe:** aus `kessel_leistung.csv` der Basis mit
derselben Regel in Python nachgerechnet — Starts, Laufphasen und Taktstunden aller dreizehn Brennstoffkessel gleich
`aggregate.csv`.

Unter den sieben CI-Projekten ändern sich 1007, 1030, 1045, 1046, 1049 (Starts, Brennstoff, Nutzungsgrad, Emissionen;
1045 und 1049 die Gasspitze); 1017 und 1047 wachsen nur um die neuen Schlüssel.

Archiv: `protokoll.txt` von R28 nach `Dokumentation/ueberholt/Referenzbasen/2026-09-30_R28_Kesselbrennwert/`, dort
Tabellenzeile und Abschnitt „Die Basis R28 im Einzelnen“; Basisname nachgezogen in `CLAUDE.md`, `kern.yml`,
`ios.yml`, `Werkzeuge/Gate/LIESMICH.md`, `Referenzlaeufe/LIESMICH.md`, der Basenhistorie und den Konzepten
Gebäudesimulation VDI 6007, Systementwurf Gebäudesimulation, Zapfprofilgenerator, Wirtschaftlichkeit,
Simulationsablauf (dort zusätzlich der Abschnitt „Takten“) und Kesselkennlinie.

## 5 Einfrierregel

„Gesäte Kesseldaten“ (`CLAUDE.md`, `Referenzlaeufe/LIESMICH.md`): Mindestleistung, Anfahrverlust und Mindestlaufzeit
rechnen; Bauart (Schalter `Brennwert`, Beschreibung) und Brennstoff bestimmen die Normvorgabe der Mindestleistung,
die Nennleistung die Vorgaben von Mindestleistung und Anfahrverlust.

## 6 Gate

GATE_PLATZHALTER

## 7 Wiki und Logbuch

Wiki-Quelle `Projekte/Wiki/Programm Dokumentation - Heizkessel.wiki`: neuer Abschnitt „Takten“ (Formel, Vorgaben,
neutrales Beispiel 100 kW), die Taktfelder der Gruppe „Kennlinie“ rechnen, der Hinweis unter dem Schalter
„Brennwertkennlinie“, Berechnung in der Simulation mit Anfahrverlust, Ergebnisse (Taktstunden, Anfahrverlust, Starts
je Kessel), Fallstrick „Ein zu großer Kessel taktet“ statt „Keine Taktung“. Dabei die drei Steuerzeichen der
Brennwertformel repariert (E3 hatte `\b`, `\f`, `\a` ohne Rohtext eingefügt: `\eta_{\mathrm{tr}}(\beta)`, `\frac`,
`\approx`). Tabu-Grep leer, keine Produktdaten.

Logbuch unter **1.2.0.6** (Vorschlag): „Heizkessel takten unter ihrer Mindestleistung: Die Simulation zählt die
Brennerstarts und rechnet je Start einen Anfahrverlust als Brennstoff; leere Felder nehmen Vorgaben.“

## 8 Offene Punkte

1. Konzept Kesselkennlinie bleibt unter `aktuell/`: aus Abschnitt 5 stehen die kleine Kurve η(β) im Katalogeditor
   und die Vorlagenfelder des Berichts (η_eff, Brennwertanteil, Starts) aus — Anwenderentscheidung, ob sie folgen
   oder entfallen; danach wandert das Konzept nach `ueberholt/`.
2. Die Startformel des Konzepts ist eine Obergrenze (jeder Lauf genau eine Mindestlaufzeit); eine Wärme knapp unter
   der Mindestleistung zählt sechs Starts je Stunde. Ob eine mildere Lesart (z. B. ein Lauf je Taktstunde, verlängert)
   gewollt ist, wäre eine eigene Entscheidung mit Neueinfrierung.
3. Der Anfahrverlust 0,002 h × Nennleistung ist eine eigene Abschätzung ohne Normquelle (7.1); ein Abgleich mit
   DIN EN 15316-4-1 steht aus, wie für die Brennstofftafel (7.2).
4. Windows-Lauf gegen R29 steht aus (erwartet wie in `Werkzeuge/Gate/LIESMICH.md`).
5. Statuszeile in `Status_iOS_Migration.md` beim Zusammenführen; Wiki-Upload der Heizkessel-Seite mit dem nächsten
   Sammel-Upload.

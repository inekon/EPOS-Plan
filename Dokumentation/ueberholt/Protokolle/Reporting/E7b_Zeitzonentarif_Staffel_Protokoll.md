# E7b — Zeitzonentarif entfällt, Leistungspreis-Staffel am Stromträger, Tarifdialog auf das Rollenmodell (Protokoll, 23.09.2026)

Statuszeile #439 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Etappe E7 (Teil b) des
Analysepapiers
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
(§ 5 Zeile E7, § 6); Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 3.5 und § 2.5; der Entscheid im
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
unter R‑Q — Q11 „kein HT/NT" (Anwender 22.09.2026), der Rest nach Empfehlung: die zweistufige
Leistungspreis-Staffel in die Kostenverwaltung neben die Energiepreisstruktur (Weg 2 aus Nach #291), danach
entfällt der Tarifstrukturdialog samt Menüpunkt —, die Fragen dieser Etappe unter R‑E7b; die Messung vorab
[`Messung_Pflegewege_Tarifstruktur_Strom.md`](../../Messung_Pflegewege_Tarifstruktur_Strom.md) (der
Zeitzonentarif zu streichen ist Weg 3 aus Nach #291). Anwender 23.09.2026: „Pushen und Weiter". Zweig `e7b` von
`c4ef252d`, zwei Phasen und ein Nachzug; Phase 1: `1b3797a3` (E7b/1), `23fd7c7e` (E7b/2), `4d0004a1` (E7b/3),
`d263b1cc` (E7b/4), `beeb3f47` (E7b/5), `0780c8a9` (E7b/6), `9a89aa28` (E7b/7); Phase 2: Merge `a9be7581`
(Arbeitszweig `435b9810`) und `e06d7eae` (E7b/8); nach den Anwenderentscheiden `6e722688` (E7b/9); Nachzug: Merge
`d93488ef` (Arbeitszweig `89bf314c` mit den Schemaschritten 101, 102 und 103), `9b9eaaa5` (E7b/10) und
`bfbfbbb9` (E7b/11, Testdatenbank, vom Orchestrator committet). Merge `954d4dcc` in `ios_migration_september`
(Basis `7062b849`; 61 Dateien, +3 451/−1 891). Opus 5.5 im Worktree `.claude/worktrees/e7b`.

## Befund vor der Welle

- **Der Zeitzonentarif rechnete an der Strommatrix.** `StromMatrix` trennte jede Stunde nach Winter/Sommer ×
  HT/NT (vier Zonen „Winter HT", „Winter NT", „Sommer HT", „Sommer NT"), und ein aktiver Tarifsatz im
  Zonenmodell (`Tab_ProjektTarif`, `Tarif_Modus` ≠ `ROLLEN`) bepreiste die vier Zonen mit eigenen Bezugs- und
  Einspeisepreisen und einer zweistufigen Leistungspreis-Staffel auf die höchste Stundenlast; der Zonenbetrag
  ersetzte den ganzen Flat-Anteil des Stromträgers samt Leistungsanteil. Gespeichert wurden vier Zonenzeilen je
  Projekt in `Tab_ErgebnisStromMatrix`.
- **Einziger Pflegeweg war die Sicht „Strombezug".** Zonen-Bezugspreise und Staffel waren nur über den Knopf
  „Strombezug…" der Wirtschaftlichkeitsseite und den gleichnamigen Sprung im BHKW-Dialog erreichbar (Messung vom
  15.09.2026); einen Menüpunkt gab es nicht.
- **Q11:** Der Anwender hat am 22.09.2026 „kein HT/NT" entschieden, den Rest nach Empfehlung: die Staffel in die
  Kostenverwaltung neben die Energiepreisstruktur, danach entfällt der Tarifstrukturdialog. Die Rückführung der
  Strommatrix auf eine Zone ohne HT/NT ändert die Bezugskosten und gehörte deshalb mit A/B-Nachweis zu E7.
- **Im Bestand:** Die Testdatenbank trägt keinen Tarifsatz (`Tab_ProjektTarif` leer), die Live-Datenbank (nur
  gelesen, Stand 100) ebenfalls keinen; gespeicherte Zonenzeilen der Strommatrix gibt es in der Testdatenbank bei
  1018 und 1031, in der Live-Datenbank bei 1062.

## Gebaut — Phase 1 (E7b/1 bis E7b/7)

- **Strommatrix ohne Tarifzonen (E7b/1).** `StromMatrix` führt nur noch Jahressummen (Netzbezug, PV-Einspeisung,
  KWK-Eigenstrom und KWK-Einspeisung, Bedarf ohne jede Eigenerzeugung, PV-Eigennutzung), die höchste Stundenlast
  des Netzbezugs und die zwei Lastbilder; vom Tarif nimmt sie allein die Winterspanne, die Sommer- und
  Wintermaximum des Leistungspreismodells „Staffel" im Rollentarif trennt. `WirtschaftlichkeitCtrl.LadeTarif` und
  `SpeichereTarif` lesen und schreiben die 13 Zonenspalten nicht mehr. Ein aktiver Satz im Zonenmodell rechnet
  nicht mehr: Der Strom geht zu den Preisen des Stromträgers, das Ergebnis trägt den Hinweis
  `WIRT_HINWEIS_ZEITZONENTARIF` („Zeitzonentarif (HT/NT) entfällt …"), der Tarifnachweis
  `WIRT_TARIF_NACHWEIS_ZONEN`. Gespeichert wird je Projekt **eine** Zeile in `Tab_ErgebnisStromMatrix`, Spalte
  `Zone` = „Jahr" (`StromMatrix.ZEILE_JAHR`); der Leser summiert alle Zeilen eines Projekts, damit ein Altstand
  mit vier Zonenzeilen dieselben Jahressummen liefert. Die Matrixtafel in Wort- und Excelbericht ist nachgezogen
  (`WIRT_MATRIX_TITEL`, `_HERKUNFT`, `_ZEITRAUM`, `_JAHR`, `_STUNDENSPITZE`, `_STUNDENLAST`;
  `WIRT_MATRIX_BEDARF_HINWEIS` geändert).
- **Der Schemaschritt (E7b/2; damals 103, heute 104).** DDL: drei DOUBLE-Spalten `Leistungspreis_Staffelgrenze`,
  `Leistungspreis_Staffel1`, `Leistungspreis_Staffel2` an `energy_project_settings`
  (`SchemaKatalog.Schritt104_LeistungspreisStaffel`); der Datenteil in einer Transaktion und wiederholbar
  (`ZeitzonentarifAbloesung` — eine Quelle für Migration, `Werkzeuge/Testdatenbankschema` und Nachweis). In der
  Fassung der Phase 1 ging die Staffel eines aktiven Zonensatzes an den Stromträger jeder Version der Gruppe, nur
  in leere Spalten, aktive Zonensätze bekamen `Aktiv = 0`, und die Zonenzeilen der Matrix wurden je Projekt eine
  Jahreszeile. Die endgültige Fassung steht unter E7b/9.
- **Die Staffel rechnet im Kern (E7b/3).** `LeistungspreisStaffel` (neu): `min(S, G) × P₁ + max(0, S − G) × P₂`
  mit `G = max(0, Grenze)`, in €/(kW·a) auf die Jahresspitze; gepflegt ist sie, sobald einer der beiden Preise
  größer als 0 ist. `KostenEmissionRechner` liest sie über `EnergietraegerPreisCtrl.StaffelLesen` und setzt sie im
  Leistungsanteil des Netzbezugs **vor** Saisonreihe und konstanten Satz, bemessen an der **Viertelstundenspitze**
  des Jahres (`Netzbezugsspitze.JahrKW`), gleich welcher Modus am Träger steht; `StromLeistungspreisGepflegt`
  fragt die Staffel mit, damit der Lauf die Zeitreihen sammelt. Die Speicherauslegung nennt die Quelle
  „Leistungspreis-Staffel des Stromträgers" (`SpeicherAuslegungVorgabenCtrl.StaffelQuelle`,
  `OPT_QUELLE_STAFFEL*`: Stufe 1, Stufe 2 mit dem Preis der oberen Stufe, ohne bekannte Spitze); die
  Variantenkopie (`VariantenCtrl`) nimmt die drei Spalten mit.
- **Die Kostenverwaltung pflegt die Staffel (E7b/4).** Die Trägerkarte des Stromträgers führt im Projektkontext
  im Block „Preis und Heizwert" die Gruppe „Leistungspreis-Staffel (auf die Jahres-Bezugsspitze)" mit
  Staffelgrenze [kW], Preis bis zur Grenze und Preis über der Grenze [€/(kW·a)] und einer Erklärzeile
  (`ETV_STAFFEL_*`); ein geleertes Feld heißt „nicht gepflegt" (NULL), so lässt sich die Staffel wieder
  abschalten. Geschrieben wird über `EnergietraegerPreisCtrl.StaffelSchreiben`, geladen in
  `EnergietraegerHuelle`; der Hilfe-Assistent kennt die drei Felder (`EnergietraegerKiSicht`,
  `KI_DLG_ET_STAFFEL_*_ERL`). Der Katalog führt keine Staffel.
- **Der Tarifdialog kennt nur noch das Rollenmodell (E7b/5).** Zonen, HT-Fenster, Modellwahl, Staffel und die
  Sichten „Strombezug" und „Komplett" entfallen (`TarifstrukturDialog`, `TarifstrukturDaten`,
  `TarifstrukturKiSicht`); gespeichert wird immer als Rollenmodell. Der Knopf „Strombezug…" der
  Wirtschaftlichkeitsseite und der gleichnamige Sprung im BHKW-Dialog entfallen; die Sprünge „BHKW-Tarif…"
  (BHKW-Dialog) und „Einspeise-Tarif…" (PV-Dialog) bleiben. `TARIF_G_ZEITZONEN` heißt jetzt „Winterspanne
  (Sommer- und Wintermaximum des Leistungspreismodells „Staffel")". Einen Menüpunkt, der hätte entfallen können,
  gab es nicht (`Menuetabelle.cs`).
- **Tests (E7b/6, E7b/7).** `StromMatrixOhneZonenTests` (neu); die Ankertests halten im Kommentar fest: alt =
  neu. E7b/7 zieht einen Kommentar in einem Test nach.

## Phase 2 und die Anwenderentscheide (E7b/8, E7b/9)

- **Merge `a9be7581`** holt den Arbeitszweig `435b9810` (zwei Papier-Commits, ohne Konflikt).
- **E7b/8 — zwei Testerwartungen berichtigt:** Ein Zeitstempel wurde als Text verglichen und hing an der Kultur
  des Läufers (jetzt als Zeitpunkt); ein Test suchte das Wort „Staffelgrenze", das zu Recht im Hinweis zu den vier
  Leistungsstufen des Rollenmodells steht (jetzt prüft er die alten Beschriftungen der zweistufigen Staffel).
  Danach gefiltert Kern 130/130, UI 387/387; der volle Lauf grün bis auf die Schemastand-Wache und die
  Auslieferungsvorlage, weil die Repo-Datenbank noch auf dem Stand vor dem Schritt stand.
- **E7b/9 — der Umbau nach E7b‑Q4 und der Nachweis zu E7b‑Q3.** Der Schritt tut jetzt, in einer Transaktion und
  wiederholbar: (1) **die Staffel übernehmen** — aus jedem aktiven Satz im Zonenmodell, der einen
  Zonen-Bezugspreis und einen Staffelpreis trägt, an den Stromträger jeder Version der Gruppe (Stamm und
  Varianten), nur in leere Spalten; fehlt einer Version die Stromträgerzeile, wird nichts angelegt und die Version
  benannt; (2) **die Zonensätze löschen** — jeder Satz, dessen `Tarif_Modus` (getrimmt) nicht `ROLLEN` heißt,
  aktiv oder nicht, fällt aus `Tab_ProjektTarif`; ein Rollensatz bleibt; (3) **die Ergebnisse verwerfen** — eine
  gespeicherte Ergebniszeile mit gefüllter Spalte `StromkostenTarif`, deren Projekt zur Gruppe eines Zonensatzes
  gehört; die Spalte füllt nur der Tarifweg, die Gruppenbedingung schließt Rollenergebnisse aus, Ergebnisse mit
  Flat-Preisen haben sie leer; verworfen wird der ganze gespeicherte Lauf des Projekts — alle drei Szenarien,
  Sensitivität und Strommatrix; (4) **die Matrix zusammenfassen** — die übrigen Zonenzeilen je Projekt zu einer
  Jahreszeile (Summen der Mengen, Maximum der Stundenlast, jüngster Zeitstempel). Die Nachprüfung verlangt, dass
  kein Zonensatz und keine Zonenzeile mehr steht; der Hinweis `WIRT_HINWEIS_ZEITZONENTARIF` bleibt für eine
  Datenbank vor dem Schritt. Zu E7b‑Q3 war kein Umbau nötig: Satz, Saisonreihe und Staffel schlossen einander
  schon aus — Rangfolge Staffel, Saisonreihe, Satz (Saisonreihe vor Satz auch beim Brennstoff), nirgends
  Addition; belegt mit `LeistungspreisStaffelTests.Satz_Saisonreihe_und_Staffel_schliessen_einander_aus` (Satz
  100.550 €, Satz mit Reihe 72.000 € — nicht die Summe —, alles zusammen nur die Staffel 135.990 €; die
  Netzkosten ohne Leistungsanteil in allen Fällen gleich). Der Migrationstest prüft das Löschen (vier Zonensätze,
  einer davon inaktiv; der Rollensatz bleibt) und die Wiederholbarkeit, ein neuer Fall die Ergebnisse (9
  verworfene Zeilen in 1019, 1024 und 1026 samt Sensitivität und Matrix; Flat-Ergebnisse derselben Gruppen, der
  Stamm ohne Stromträger und das Rollenergebnis 1039 bleiben).

## Der Nachzug (Merge `d93488ef`, E7b/10, E7b/11)

- **Die Schemanummern.** `origin/ios_migration_september` trug seit dem 23.09.2026 mit der Gebäudesimulation
  einen Schritt 101 (Gebäudespalten); die Sitzung des Zapfprofilgenerators hat den E7a-Schritt auf 102 und ihren
  eigenen auf 103 umnummeriert (#438). Die Kette lautet damit 101 Gebäudespalten, 102 KWKG-Anlagenart (E7a),
  103 Tww (Z0), **104 Leistungspreis-Staffel (E7b)**, 105 K‑1 (E7c).
- **Merge `d93488ef`** holt den Arbeitszweig `89bf314c`; Konflikte in vier Dateien (`SchemaMigration`,
  `SchemaStand`, `TestDatenbank`, `Werkzeuge/Testdatenbankschema`), aufgelöst jeweils als Tww-Teil plus E7b-Teil;
  Ressourcen und Designer hat git selbst zusammengeführt, der Designer-Prüflauf bestätigt sie.
- **E7b/10 — Schritt 103 wird 104:** `SCHRITT_104_ZEITZONENTARIF_ABLOESUNG`, Methode und Listeneintrag nach 103,
  `SchemaKatalog.Schritt104_LeistungspreisStaffel`, `SchemaStand.Zielversion` = 104 samt eigenem Absatz,
  Kommentare und Tests; die Reihenfolge 101 → 102 → 103 → 104 steht an allen vier Stellen (Migration,
  `SchemaStand`, `TestDatenbank.SchemaNachziehen`, Werkzeug).
- **E7b/11 — die Testdatenbank auf Schemastand 104** (vom Orchestrator committet): LFS-Zeiger, SHA-256
  `044e44db…`, 67 727 360 Byte; der Schritt fand keinen Zonensatz und verwarf kein Ergebnis, die acht Zonenzeilen
  der Strommatrix von 1018 und 1031 sind je eine Jahreszeile (gleiche Summe, −14,471 bzw. −14,723 MWh). Der
  Nachtrag „Schemastand 104" in `Referenzlaeufe/LIESMICH.md` steht mit den Papieren zu #439.

## Fragen aus der Etappe

Der Zwischenbericht nannte vier Fragen an den Anwender; sie stehen im Entscheidungsregister als **R‑E7b**. Alle
vier hat der Anwender am 23.09.2026 entschieden.

| Frage | Stand |
|---|---|
| **E7b‑Q1** — Was wird aus dem Tarifstrukturdialog? (a) wörtlich ganz entfernen — dann fällt auch das Rollenmodell (Differenzmethode, Etappe E5) samt den Sprüngen „BHKW-Tarif…" und „Tarif…" weg; (b) wie gebaut: nur das Zonenmodell fällt, das Rollenmodell bleibt; (c) das Rollenmodell in einem eigenen Auftrag verlegen und den Dialog danach entfernen. Empfehlung b: Q11 hat nur „kein HT/NT" entschieden; zu a gehört ein eigener Entscheid, weil ein ganzer Rechenweg wegfiele (heute ohne einen einzigen gespeicherten Tarifsatz) | **entschieden 23.09.2026: b**, nach Empfehlung — das Rollenmodell bleibt; gebaut mit E7b/5 |
| **E7b‑Q2** — Bemessung der Staffel: Umgesetzt ist die Viertelstundenspitze statt der höchsten Stundenlast (wie beim Leistungspreis des Stromträgers); bei 1030 ist die Zahl gleich, 2.011 kW. Empfehlung: so bestätigen | **bestätigt 23.09.2026:** „Die Viertelstundenspitze wird abgerechnet und ist Maßstab für die Leistungsberechnung"; gebaut mit E7b/3 |
| **E7b‑Q3** — Vorrang der Staffel: Eine gepflegte Staffel ersetzt Leistungspreis und Saisonreihe, statt sich zu addieren; gepflegt wird sie nur am Projekt, nicht im Katalog. Empfehlung: so bestätigen | **bestätigt 23.09.2026:** „Eine gepflegte Staffel ersetzt beides, sie addiert sich nicht. Entweder Leistungspreis gesetzt oder eine Reihe, keine Addition"; gebaut mit E7b/3, der Ausschluss belegt mit E7b/9 |
| **E7b‑Q4** — Gespeicherte Ergebnisse, die noch mit dem Zonentarif gerechnet wurden, bleiben stehen, bis jemand „Berechnen" drückt; die Statuszeile meldet sie nicht als veraltet. (a) so lassen; (b) der Schemaschritt löscht sie; (c) ein Hinweis in der Statuszeile. Empfehlung a, weil keine vorhandenen Daten betroffen sind | **entschieden 23.09.2026, abweichend von der Empfehlung:** „alte Tarife verwerfen, nicht mehr relevant" — gebaut mit E7b/9 als Löschen der Zonensätze und Verwerfen ihrer gespeicherten Läufe |

## A/B-Nachweis

Gemessen an Kopien der Testdatenbank im Scratchpad des Agenten: vorher mit dem Kern `c4ef252d`, nachher mit dem
Stand des Zweigs auf migrierten Kopien; die Proben mit gebauten Tarifsätzen sind nach E7b/9 und nach dem Nachzug
wiederholt — die Zahlen blieben gleich, nur der Zustand der Datenbank ist nach E7b/9 ein anderer.

**Die dreizehn Basisprojekte:**

| Weg | Größe | vorher | nachher | Grund |
|---|---|---|---|---|
| gebuchter Stand (Ankerweg) | alle dreizehn | Kapitalwert 1023 −639.584,90 €, 1024 −2.896.359,13 €, 1030 −21.895.377,28 €; Betriebskosten 99,00 €/a; Kaskade 13.000,00 € | bitgleich, 0 Differenzen | kein Tarifsatz, keine Staffel, keine Stundenreihen |
| frischer Lauf | Kapitalwert 1024 | −2.796.650,73 € | gleich | — |
| frischer Lauf | Kapitalwert 1030 | −31.141.242,71 € | Differenz 1·10⁻⁸ € | Matrixsumme in einem Durchlauf statt aus vier Teilsummen |
| frischer Lauf | Matrixsummen aller dreizehn | — | Abweichung nur in den letzten Nachkommastellen (relativ ≤ 2·10⁻¹³) | dito |
| gespeicherte Matrix | Zeilen je Projekt | vier Zonenzeilen | eine Jahreszeile | keine Zonen mehr |
| gespeicherte Matrix | geladene Werte | 1040 PV 2,272 · 1018 −14,733 · 1017 Bedarf 672,001 MWh | 2,273 · −14,732 · 672,000 MWh | ±0,001 MWh: eine gerundete Summe statt vier gerundeter |
| Altbestand 1018/1031 | Zeilen | vier Zonenzeilen | eine Zeile, gleiche Summe (−14,471 / −14,723 MWh) | der Schritt fasst zusammen |

**Probe 1030 mit gebautem Zonensatz** (Staffel 1.500 kW / 60 / 90 €/(kW·a), Szenario „Erwartet"; die Testdatenbank
selbst trägt keinen Tarifsatz):

| Größe | vorher (Zonentarif) | nachher (nach dem Schritt) | Grund |
|---|---|---|---|
| Tarifsatz 1030 (Zonenmodell, aktiv) | vorhanden | gelöscht | E7b‑Q4 |
| Staffel am Stromträger 1030/60 | — | 1.500 kW / 60 / 90 € | vom Schritt übernommen |
| gespeicherte Ergebnisse 1030 (`StromkostenTarif` 1.299.384,15 €) | 3 Zeilen, 4 Matrixzeilen | verworfen, keine Matrix | E7b‑Q4 |
| Energiekosten | 1.832.155,35 €/a | 1.760.606,20 €/a | Arbeits- und Grundpreis des Stromträgers (1.091.845 €) statt der Zonenpreise (1.163.394,15 €); die Staffel bleibt 135.990 € (1.500 × 60 + 511 × 90 bei 2.011 kW), jetzt als Leistungsanteil des Stromträgers |
| KWK-Einspeiseerlös | 28,56 €/a | 0 | die Zonen-Einspeisepreise entfallen, der KWK-Satz ist in 1030 nicht gepflegt |
| Stromkosten Tarif | 1.299.384,15 € | leer | kein Zonenpfad mehr |
| Kapitalwert | −34.819.801,17 € | −33.551.896,03 € | +1.267.905,14 €, Folge der Zeilen darüber |
| Leistungspreisquelle der Speicherauslegung | 90 €/(kW·a), Quelle „Tarifstruktur" | 90 €/(kW·a), Quelle „Staffel des Stromträgers" | die Staffel ist umgezogen |

**1024 im Rollenmodell** (gebauter Rollensatz): Kapitalwert −2.136.393,15 €, vermiedene Kosten −12.941,70 € —
vorher wie nachher, bis auf die letzten Nachkommastellen. Der Satz bleibt aktiv, seine gespeicherten Ergebnisse
(`StromkostenTarif` 128.541,70 €) bleiben, seine Matrix wird eine Jahreszeile; eine Staffel des Rollensatzes wird
nicht übernommen.

**Kein stiller Rückfall:** Auf einer Kopie vor dem Schritt steht am Ergebnis der Hinweis „Zeitzonentarif (HT/NT)
entfällt …"; gerechnet wird dann mit dem Stromträger ohne Staffel — Energiekosten 1.624.616,20 €, Kapitalwert
−31.141.242,71 €.

**Live-Datenbank** (nur gelesen): Stand 100, keine Tarifsätze; Projekt 1062 trägt vier Zonenzeilen, die der
Schritt zu einer Zeile mit 3,709 MWh zusammenfasst.

## Zahlen und Abnahme

- **Im Worktree `e7b`** nach dem Nachzug (`9b9eaaa5`, ohne anderen Testprozess): Migrationstest
  `ZeitzonentarifAbloesungTests` 4/4; die gefilterten Klassen (E7b sowie Tww-, Gebäude- und KWKG-Schema) Kern
  151/151, UI 389/389; voller Lauf `WP-Plan.Kern.slnf`: KiKern 524, SpeicherEngine 386, SpeicherPlanung 27 und 1
  übersprungen, EPOS.UI 5 278, EPOS.Kern 4 780 von 4 781 — rot allein `TestdatenbankSchemastandWacheTests`, weil
  die Repo-Datenbank noch auf 103 stand; `Auslieferungsvorlage.Tests` 14 von 26 rot, alle „Schemastand 103
  (erwartet 104)"; Gegenprobe mit einer außerhalb des Repos auf 104 migrierten Kopie an Stelle der Repo-Datenbank:
  Auslieferungsvorlage 26/26, Schemastand-Wache 1/1 (danach die Originalbytes zurück, Prüfsumme gleich);
  Formularkarte 124/124; Referenzlauf aller dreizehn Projekte auf der 104-Kopie gegen
  `2026-09-22_R11_Bestandsbefunde` `GESAMT: PASS`, 3 882 737 Werte, 13/13 byte-gleich — die Basis bleibt R11;
  Designer 7 683 Einträge ohne Abweichung; SQL-Prüfer (104-Kopie) Selbsttest 35/0, 1 645 SQL-Texte, 0
  Fundstellen; Kern-Filter 0 Fehler; Windows-Schale (Worktree `e7b`, `bfbfbbb9`) 0 Fehler.
- **Gate #439** auf `954d4dcc` (08:31–08:35): Kern-Filter (Release) 0 Fehler; ChartProben alle grün, 111 Bilder —
  die 108 Bilder der Messlatte unverändert, die drei neuen (`raumtemperatur_gebaeude.png`,
  `raumtemperatur_sollband_wirkt_a.png`, `raumtemperatur_sollband_wirkt_b.png`) stammen aus der
  Gebäudesimulation (Stand von `origin`, nicht E7b), die lokale Windows-Messlatte ist auf 111 nachgezogen; Tests
  0 Fehler, 10 996 bestanden, 1 übersprungen (EPOS.Kern 4 781, EPOS.UI 5 278, KiKern 524, SpeicherEngine 386,
  SpeicherPlanung 27 und 1 übersprungen); Dokumentationswachen 26/26; Windows-Schale (Worktree `e7b`,
  `bfbfbbb9`) 0 Fehler.
- **Ressourcen** (de und en): **21 neu** — `ETV_STAFFEL_TITEL`, `_GRENZE`, `_PREIS1`, `_PREIS2`, `_HINWEIS`,
  `_SPEICHERFEHLER`; `KI_DLG_ET_STAFFEL_GRENZE_ERL`, `_PREIS1_ERL`, `_PREIS2_ERL`; `OPT_QUELLE_STAFFEL`,
  `_OHNE_SPITZE`, `_STUFE1`, `_STUFE2`; `WIRT_MATRIX_TITEL`, `_HERKUNFT`, `_ZEITRAUM`, `_JAHR`, `_STUNDENSPITZE`,
  `_STUNDENLAST`; `WIRT_HINWEIS_ZEITZONENTARIF`, `WIRT_TARIF_NACHWEIS_ZONEN`. **5 geändert** —
  `WIRT_MATRIX_BEDARF_HINWEIS`, `TARIF_G_ZEITZONEN` („Winterspanne …"), `KI_DLG_TAR_WINTERVON_ERL`,
  `KI_DLG_TAR_WINTERBIS_ERL`, `KDLG_ERTRAG_FK7` (verwies auf die Zonen-Einspeisepreise). **38 gestrichen** —
  `TARIF_*` (22: Zonen, HT-Fenster, Modellwahl, Staffel, Titel, darunter die Waise `TARIF_BTN_SPEICHERN`),
  `KI_DLG_TAR_*_ERL` (8), `OPT_QUELLE_TARIF*` (4), `WIRT_BTN_STROM_TARIF`, `BHW_BTN_STROMBEZUG` und die Waisen
  `KDLG_LP_STROM_TARIF`, `KDLG_LP_STROM_TARIF_BTN`. Designer 7 683 Einträge.
- **Schemaschritt 104** (`SCHRITT_104_ZEITZONENTARIF_ABLOESUNG`), `SchemaStand.Zielversion` = 104. Den nächsten
  freien Schritt, **105**, bekommt K‑1 (E7c); danach ist 106 frei.

## Abnahme am Gerät (A‑E7b‑1, Windows und iPad)

(1) Kostenverwaltung, Stromträger eines Projekts: Im Block „Preis und Heizwert" steht die Gruppe
„Leistungspreis-Staffel (auf die Jahres-Bezugsspitze)" mit drei Feldern — Staffelgrenze [kW], Preis bis zur
Grenze, Preis über der Grenze [€/(kW·a)] — und der Erklärzeile darunter; Speichern und Wiederöffnen hält die
Werte, ein geleertes Feld schaltet die Staffel ab; im Katalogkontext und bei einem Brennstoffträger steht die
Gruppe nicht. (2) Die Fußleiste der Wirtschaftlichkeitsseite führt keinen Knopf „Strombezug…"; eine gepflegte
Staffel erscheint in den Energiekosten als Leistungsanteil des Stromträgers. (3) Die Matrixtafel „Strommengen" in
Wort- und Excelbericht zeigt eine Jahreszeile ohne Tarifzonen. (4) Der Dialog BHKW-Wirtschaftlichkeit führt in
der Gruppe Stromsteuer keinen Sprung „Strombezug…"; „BHKW-Tarif…" öffnet die Tarifstruktur im Rollenmodell,
ebenso „Einspeise-Tarif…" aus dem PV-Dialog. (5) Ein Zonensatz, der den Schritt nicht durchlaufen hat (Kopie
einer Datenbank vor der Migration): Das Ergebnis trägt den Hinweis „Zeitzonentarif (HT/NT) entfällt …" und
rechnet mit den Preisen des Stromträgers. (6) Englisch.

## Befunde nebenbei

- **Die Zonenspalten bleiben stehen:** `Tab_ProjektTarif` behält die 13 Spalten des Zonenmodells (`HT_Von`,
  `HT_Bis`, `Bezug_W_HT` … `Bezug_S_NT`, `Einsp_W_HT` … `Einsp_S_NT`, `Staffel_Grenze`, `Staffel_Preis1`,
  `Staffel_Preis2`); der Kern liest sie nicht mehr, und nach dem Schritt trägt sie kein Satz mehr. Kandidat für
  einen Aufräumschritt (DDL); die Selbst-DDL in `WirtschaftlichkeitCtrl` legt sie weiter an (Konzept § 6.5, zwei
  Migrationsmechanismen).
- **`Tab_ErgebnisStromMatrix.Zone`** trägt nur noch „Jahr"; die Spalte heißt weiter nach den Zonen.
- **Altmatrix 1018/1031:** negativer Netzbezug, gespiegelt als KWK-Einspeisung — Bestand vom 21.08.2026, nicht
  E7b.
- **Hilfe-Zuordnung:** `help_mapping.txt` Z. 265 (`Form_Tarifstruktur.btn_Help = Wirtschaftlichkeit#strombezug`)
  zeigt weiter auf den Anker `strombezug`; die Wiki-Quelle behält ihn und beschreibt an dieser Stelle jetzt die
  Tarifstruktur im Rollenmodell. Anker und Zuordnung umzubenennen ist eine eigene kleine Aufgabe (Code und
  Wiki-Quelle zugleich).
- **Wiki:** Die Seite Hilfe-Assistent (Z. 72) nennt die Tarifstruktur unter den Masken des Assistenten — das
  bleibt gültig.
- **Die Messung der Pflegewege** vom 15.09.2026 ist mit dieser Etappe umgesetzt und steht jetzt unter
  `ueberholt/`.

## Offen

- **Abnahme am Gerät** A‑E7b‑1 (sechs Punkte oben).
- **Befunde:** ein Aufräumschritt für die 13 Zonenspalten von `Tab_ProjektTarif` samt der Selbst-DDL; Anker und
  Hilfe-Zuordnung `strombezug` umbenennen.
- **Nächste Etappe: E7c** — alle Fragen aus E7a und E7b sind entschieden (R‑E7, R‑E7b): K‑1 mit Schemaschritt
  **105**, die Kern-Regel und Kohärenzzeile zu Nr. 30 (Lesart b, Anlagenart des 1030-BHKW in der Testdatenbank),
  A20 (Lesart b), S‑2 (A3), V‑2/V‑1 (A4), die Schritte E, F, G, B‑4 Rest, B‑6 und der Kapitalwert 1024
  (−676.036,81 € gegen den früheren Konzeptwert).
- **Push** auf Zuruf; der Merge `954d4dcc` hat noch keinen CI-Lauf.
- **Papiere mit der Statuszeile:** Register (Q11 gebaut #439, neue Familie R‑E7b, die Schrittnummern in A2,
  R‑NR Nr. 30, R‑E7, EZ‑5, dazu EZ‑9 und EZ‑10), Konzept (Kopf mit Schemastand 104, § 2.2, § 2.5, § 2.7,
  § 2.11.6, § 2.13 (4), § 3.5, § 3.6, § 6.1, § 6.3, § 7, Anhang), Protokoll der Entscheidwege (§ 8.5, § 8.6),
  Analysepapier (Kopf, Nachtrag, § 5, § 6), Rechenwege 04, 05 und 07, Mockup (Trägerkarte, Berechnungsgrundlage
  und Schlüsseltafel der Kategorie 4, Knopfzeile und Schlüsseltafel der Kategorie 5, Schlüsseltafeln der
  Kategorien 7 und 8, Fußleiste der Ergebnisseite, U1 und U32, Stand-Absatz), Logbuch-Sätze und die Wiki-Quellen
  der Seiten Wirtschaftlichkeit und Kosten; der Nachtrag „Schemastand 104" in `Referenzlaeufe/LIESMICH.md`; die
  Messung der Pflegewege nach `ueberholt/`; ein Nachsatz zur Umnummerierung im Protokoll E7a.

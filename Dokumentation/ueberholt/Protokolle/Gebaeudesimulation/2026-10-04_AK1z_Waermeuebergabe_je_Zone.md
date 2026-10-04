# Protokoll AK1z — Wärmeübergabe je Zone (04.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Orchestrierung Fable 5.1, Zweig `ak1z` mit den Worktrees `ak1z-ui` (Oberfläche) und `ak1z-doc` (Papiere), Stand der Teile A bis E `b845355`.
**Anlass:** Entscheid E63 (Anwender, 03.10.2026, am Zonenprojekt 1052): „Wärmeübergabe soll für jede Zone einstellbar sein.“ Übergabe [`2026-10-03_Uebergabe_Gebaeudesimulation_Kontowechsel.md`](../../../aktuell/Gebaeudesimulation/2026-10-03_Uebergabe_Gebaeudesimulation_Kontowechsel.md), Abschnitt 3.2. Statuszeile #708, Schemaschritt 181.

## 1 Auftrag

Übergabeart und Auslegung je Zone einstellbar machen: Schema an `Tab_Zone` und `Tab_ErgebnisZone`, Kaskade „wie Gebäude“, Schritt H je Zone im Mehrzonenweg am gemeinsamen Vorlauf, Zonendialog „Übergabe“, Ergebnisse, Bericht und Export je Zone, Einfrierregel „gesäte Auslegungsdaten der Übergabe“ um die Zonenspalten erweitert, ein gekoppeltes Zonen-Referenzprojekt mit neuer Basis. Einzonenweg und die achtzehn Projekte der Basis R34 byte-gleich. Sieben Teile (A Schema und Kaskade, B Rechenweg, C Zonendialog, D Bedarfsdialog, Bericht, Export, E Saat 1054, F Basis R35 als paralleler Auftrag, G Papiere).

## 2 Umsetzung Teil A (Schema, Kaskade, Testdatenbank)

- `d9d9c1b` Schemaschritt 181 `ZonenUebergabeSchema` (`ErdreichVorgabeSchema.SCHRITT + 1`): an `Tab_Zone` `Auslegung_Vorlauf`, `Auslegung_Ruecklauf`, `Auslegung_Raumtemperatur`, `Regler_Proportionalband` (REAL, nullbar, Proportionalband `CHECK (IS NULL OR >= 0)`); an `Tab_ErgebnisZone` `Vorlauf_Mittel_C`, `Ruecklauf_Mittel_C`, `Uebergabe_Begrenzt_H` (`CHECK (BETWEEN 0 AND 8760)`). Art, Exponent und Nennleistung lagen seit S-C an der Zone.
- `b943a8a` Zone: Auslegungspunkt und Regler lesen, schreiben, prüfen (Prüfmeldungen `ZONE_MSG_*`), NULL-erhaltend über die Kopierwege.
- `b4d5fcc` Kaskade `Zonenuebergabevorgaben.Aufloesen` als reine Funktion.
- `ef5ecb7` Testdatenbank auf Schemastand 181 (7 Spalten, alle leer, 83 972 096 Byte).
- `4aacafa` Tests der Übergabe je Zone, Spaltenzähler auf Schritt 181.

## 3 Umsetzung Teil B (Rechenweg)

- `9506d15` Wärmeübergabe je Zone im Mehrzonenweg: Schritt H je Zone je Abschnitt am gemeinsamen Vorlauf, Rücklaufmischung, Begrenzungskennzahlen des Gebäudes als Maximum über die Zonen, Musterfesthaltung im Gauß-Seidel, Ergebnisspalten je Zone.
- `825ad2e` Tests der Wärmeübergabe je Zone (Grenzfallprobe B je Zone bitgleich zur idealen Regelung, Byte-Gleichheit ungekoppelter Mehrzonengebäude).
- `adbb285` Exportverluste: Auslegungspunkt und Proportionalband der Zone eingestuft.

## 4 Umsetzung Teil C (Zonendialog)

- `0077b9f` DTO und Hülle mit der Übergabe je Zone, Ressourcen `ZONDLG_UEBERGABE_*` in beiden Sprachen.
- `318a9e2` Zonendialog: Abschnitt „Übergabe“ (Klappliste mit „wie Gebäude (…)“, sechs Zahlenfelder mit Platzhalter des wirksamen Werts, Herleitungszeile mit Herkunft, Hinweis „ohne Wirkung“ bei Heizkreis oder Projektkopplung aus, unbeheizte Zone ohne Übergabe, KI-Sicht).
- `cf09889` Tests `ZonenUebergabeDialogTests` (16 Fälle), Rundreise über den Zonenweg; Beschriftungen ohne Doppelpunkt.

## 5 Umsetzung Teil D (Bedarfsdialog, Bericht, Export)

- `f446dc9` Bericht: Zonentabelle mit Vorlauf, Rücklauf und begrenzten Stunden, nur bei gekoppelter Zone.
- `22c4473` Bedarfsdialog und KI-Sicht: dieselben drei Spalten in der Gruppe „Zonen“.
- `49f8908` Exportschlüssel `Geb[n].Zone[k].VorlaufMittelC`, `.RuecklaufMittelC`, `.UebergabeBegrenztH` in Anlagenkopplung 10.4.

## 6 Umsetzung Teil E (Referenzprojekt 1054)

- `c91ea01` Testdatenbank: Referenzprojekt 1054 „Zonen mit Heizkreis“ als Kopie von 1052 gesät (Saatskript und Bauplan unter `Referenzlaeufe/Skripte/`).
- `a805914` Wache `ZonenHeizkreisReferenzprojektWacheTests` und Zählnachzüge; `64f503a` LIESMICH: Referenzprojekt 1054 und Einfrierregel der Übergabe.
- `a0ed451` Heizkurve am Gebäude von 1054 gesät, Datenbank neu; `be9c714` Wachen nachgezogen (Heizkurve, begrenzte Stunden, Kopplungsschema); `b1de7b2` LIESMICH mit Heizkurve und Rechenergebnis.
- Zusammenführung: `2c449c6`, `9a0a96a` (`ak1z-ui` in `ak1z`), `b845355` (origin in `ak1z`).

## 7 Umsetzung Teil G (Papiere)

Anlagenkopplung (Nachzug E63, 6.5, 8.1, 10.1, 10.2, 11.4), Mehrzonenkonzept (2.6, 4.2, 9), Rechenschritte 7.4, Status Gebäudesimulation (E63, Stufe AK1z), Übergabepapier Abschnitt 7, Wiki-Quellen „Mehrzonenmodell“ (Abschnitt „Übergabe je Zone“, Grenzen), „Gebäudemodell VDI 6007“ und „Kühlung“ (Mehrzonensatz), dieses Protokoll und die Indexzeile.

## 8 Nachweise

| Prüfung | Ergebnis |
|---|---|
| Teil A | Filter 120/120 |
| Teil B | 672 Tests grün, Referenzlauf 4/4 PASS; Einzonenweg und ungekoppelte Mehrzonengebäude 18/18 byte-gleich gegen R34 |
| Teil C | UI 157 grün |
| Teil D | Kern 1 044, UI 1 544 grün |
| Teil E | 589 / 985 grün nach Zählnachzug; 1054 zwei Läufe byte-gleich, rund 2 s |
| Teil G | Wachen Dokumentation, Repository-Ordnung, Wiki und Wiki-Produktdaten grün |
| Gate (Hauptbaum, Merge-Stand) | Kern-Bau 0 Fehler; ChartProben 208 Hashes gleich der Messlatte; Referenzlauf 19/19 PASS gegen R35, 576/576 CSV byte-gleich; Plattformnachweis `--stoerung ulp` PASS; Dokumentationswachen 60/60; Designer unverändert; SQL-Dialekt 2 300 Texte 0 Fundstellen; BOM nur die zwei Bestandsbefunde; keine Konfliktmarker; Testlauf Kern-Filter Kern-Filter auf 7a5c161 (gepusht b845355): EPOS.Kern.Tests 10 672 grün, 1 rot, 2 übersprungen — der rote `SchrittMusterTests` hielt die alte Regel „Muster lehnt Übergabefälle ab“ und ist mit `b4b9f06` auf die gültige Regel umgeschrieben (Filter SchrittMuster/ZonenuebergabeRechenweg/Zonenschleife 40/40); EPOS.UI.Tests 7 432/7 432, KiKern 549/549, SpeicherEngine 397/397, SpeicherPlanung 27/28 (1 übersprungen), Windows-Schale auf Linux 0 Fehler, Werkzeugtests Formularkarte 124/124, Auslieferungsvorlage 47/47, Gebäudevergleich 24/24, ZapfprofilValidierung 39/39 |
| Basis R35, Push | Basis eingefroren (Teil F); Push-SHA und CI-Vermerk folgen |

**Rechenergebnis 1054:** Heizwärme 47,58 MWh, Spitze 26,05 kW; Vorlauf/Rücklauf im Mittel 39,53/36,08 °C, 1 339 begrenzte Stunden am Gebäude; Gästezimmer 33,63 MWh, 293,9 h begrenzt; Gastronomie und Verwaltung 13,94 MWh, 1 106,5 h begrenzt, im Mittel der Heizzeit 18,7 °C; Keller unbeheizt. Beheizte Zonen im Aufheizzustand GEKOPPELT. Basis **`2026-10-04_R35_Zonenuebergabe`** mit neunzehn Projekten; 1054 nicht in der CI-Auswahl.

## 9 Befunde und Festlegungen

1. **Spaltenwahl:** Die Zone trägt sieben Übergabespalten (Art, Exponent, Nennleistung aus S-C; Auslegungsvorlauf, -rücklauf, -raumtemperatur, Proportionalband aus 181). Am Gebäude bleiben `Heizkreis_Aktiv`, Heizkurve, `Sollwertprofil` und `Auslegung_Aussentemperatur` — ein Heizkreis, ein Vorlauf.
2. **Kaskade:** Art = Zone, sonst Gebäude; Exponent und Auslegung V/R = Zone, sonst ausdrücklicher Gebäudewert, sonst Vorgabe der wirksamen Zonenart; Auslegungsraumtemperatur = Zone, sonst Gebäude, sonst Tagsoll der Zone; Proportionalband = Zone, sonst Gebäude, sonst 1 K; Nennleistung = Zone, sonst Gebäudenennleistung × Nutzflächenanteil der beheizten Zonen. Gebäudenennleistung hergeleitet = Summe der stationären Lasten der beheizten Zonen (Nachbarn fest: beheizte an ihrer Auslegungsraumtemperatur, unbeheizte an der Auslegungsaußentemperatur); Auslegungsraumtemperatur des Gebäudes = Feld, sonst höchste der beheizten Zonen.
3. **Vorlauf:** Die Heizkurve läuft am höchsten Heizsollwert der gekoppelten Zonen, gedeckelt am Auslegungsvorlauf des Gebäudes.
4. **Rücklaufmischung:** θ_R = Σ W_H,z · θ_R,z / Σ W_H,z je Stunde; Begrenzt-Anteil, Stunden an `Heizleistung_Max` und Heizgrenze und größte Unterschreitung des Gebäudes = Maximum über die Zonen.
5. **Musterfesthaltung:** Der Gauß-Seidel hält ab Durchlauf 2 auch die Übergabefälle im Muster fest — Leistung mit der Gleichung des festen Falls neu gelöst, ohne Bisektion; nicht haltbares Muster → Stunde frei gerechnet und gezählt.
6. **Gebäude als Hauptschalter:** Gebäudeart `IDEAL` oder `Heizkreis_Aktiv` = 0 → ungekoppelt, auch wenn eine Zone eine eigene Art trägt. Die Zonenart `IDEAL` nimmt eine Zone im gekoppelten Gebäude aus.
7. **Kühlseite und 4-K-Regel** bleiben je Zone ohne Übergabe; `SIMENG_G6_AK1_IDEAL` meldet nur noch die Kühlübergabe. Die AK2-Verfügbarkeit gibt es im Mehrzonenweg nicht.
8. **1054 statt 1052:** 1052 trägt den Nachweis der Aufheizoptimierung mit Zonen und bleibt ungekoppelt; die gekoppelte Fassung ist die Kopie 1054.
9. **Heizkurve an 1054:** gefahren mit leerem Niveau und leerer Steilheit (Vorgabekurve durch den Auslegungspunkt), damit die Übergabe in kalten Stunden an ihre Grenze kommt und die begrenzten Stunden etwas aussagen.
10. **Umgebung der Sitzung:** `lfs.github.com` durch die Netzsperre nicht erreichbar — die Testdatenbank kam über den Bundle-Weg; SDK 10.0.112 mit Alias statt 10.0.400.

## 10 Offen

- Bundle-Weg und rote CI: Die Netzrichtlinie sperrt `lfs.github.com`; die Commits liegen ohne neue LFS-Zeiger auf origin (Datenbank im Zeiger auf dem Stand #706). Der Datenbank-Commit (Testdatenbank 181 mit 1054, 84 832 256 Byte, `2eea4775…`) folgt als Bundle an den Anwender; die CI ist rot, bis er es gepusht hat (Schemastand-Wache, Zonenwachen, Referenzlauf).
- Sichtproben unter Windows: Zonendialog (Abschnitt „Übergabe“), Bedarfsdialog (Gruppe „Zonen“), Bericht (Tabelle „Zonen“).
- `#:include` der Saatskripte braucht SDK 10.0.400; mit 10.0.112 nicht ausführbar.
- Verteilung der AK2-Verfügbarkeit auf Zonen (zweite Verteilungsstufe, Anlagenkopplung 6.2).
- Entscheid des Anwenders: Soll eine Zone mit eigener Art in einem Gebäude mit Art `IDEAL` rechnen (heute: Gebäude ist Hauptschalter)?
- Wiki-Upload gebündelt (Mehrzonenmodell, Gebäudemodell VDI 6007, Kühlung); Logbuch-Version beim Anwender. Logbuch-Entwurf:
  - „Bei Gebäuden mit mehreren Zonen lässt sich die Wärmeübergabe je Zone einstellen; jede Zone rechnet ihre Heizfläche am gemeinsamen Vorlauf des Heizkreises.“
  - „Wärmebedarf und Bericht zeigen je Zone Vorlauf, Rücklauf und die Stunden mit begrenzter Übergabe.“

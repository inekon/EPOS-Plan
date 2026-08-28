# Basis 2026-08-29_E1E2 — Laufprotokoll

**Anlass: Etappen E1 (CO2-Saat der Katalogträger, Migrationsschritt 56, `933fc97`) und
E2 (Emissionsarten-Katalog, Migrationsschritt 57, `6694c7a`) + Datenänderung des
Anwenders.** Schritt 56 setzt `energy_carrier.co2` für 20 Katalogträger auf die
BAFA-EEW-Werte ([Konzept CO2-Faktoren](../../Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md);
u. a. Erdgas E 240 → 201, Heizöl L 310 → 266, Elektrische Energie 560 → 435,
Fernwärme 0 → 280 — vollständige Liste im maschinellen Protokoll unten), Schritt 57
legt die Tabellen `emissionsart`/`emissionswert` an
([Konzept Emissionsarten](../../Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md)).

## Eckdaten

- **Codestand:** `6694c7a` (Branch `Pufferspeicher`, Etappe E2), MSBuild x64 Debug vom
  29.08.2026 00:45 (unmittelbar nach dem E2-Commit 00:45:37); Werkzeug `Referenzlauf`
  gleicher Buildstand.
- **Datenquelle:** produktive `Kenndaten.accdb`, Zeitstempel **29.08.2026 00:02:08**,
  151 949 312 Bytes, Schemastand **54**, **nur gelesen** — MD5
  `32F836432A2CDCB4083067302F083855` vor und nach dem Lauf identisch (Größe und
  Zeitstempel ebenso).
- **Arbeitskopie:** `Referenzlaeufe\Arbeitskopie` (Weg A, Modus `lauf`), migriert
  **54 → 57**: Schritt 55 (Temperaturmodus-/Lesepunkt-Vorbelegungen wie in der
  B2-Basis), Schritt 56 (CO2-Saat, 20 Träger, Gegenprobe ohne Abweichung), Schritt 57
  (Emissionsarten — der Migrationsbericht selbst vermerkt: „KEIN Rechenergebnis ändert
  sich … die neuen Tabellen hat in dieser Fassung kein Leser").
- **Lauf:** 10 Projekte (feste Liste, siehe Zuordnung), alle Exit-Code 0, **234 CSV**,
  Gesamtdauer 19 s.
- **Selbstvergleich:** zweiter `lauf` von derselben Quelle **234/234 byte-/MD5-gleich** —
  die Basis ist reproduzierbar.
- **`pruefen`:** GESAMT plausibel (bekannter Hinweis 1007 Solar ohne Modul; neu 1039
  WP-Gewerk ohne Modul — siehe 1039-Abschnitt).

## Der zentrale Befund: keine CO2-Abweichung — E1+E2 im Rechenkern wirkungsneutral

Die Erwartung „jeder künftige Vergleich meldet CO2-Abweichungen" hat sich **nicht**
bestätigt — sie kann sich im Referenzumfang auch nicht bestätigen:

- **Kein einziger Schlüssel der eingefrorenen CSVs ist CO2-/emissionsbezogen** — weder
  in B2 noch in E1E2 (Volltextsuche `co2|emission` über alle 332 bzw. 234 CSV: 0
  Treffer). Die Emissionskennzahlen (`em.co2`/`em.co2_spez` aus dem `KennzahlenKatalog`;
  der `KostenEmissionRechner` liest `energy_carrier.co2`) entstehen im
  **Bericht/Wirtschaftlichkeitsteil**, den der Referenzlauf laut LIESMICH ausdrücklich
  nicht abdeckt. Die durch E1 geänderten **Berichts**-CO2-Werte sind hier also weder
  eingefroren noch vergleichbar — sie werden wie die übrige Wirtschaftlichkeit je
  Etappe als A/B gegen den Vorgängerstand gemessen.
- **Alle 216 CSV der neun datenunveränderten Projekte sind byte-/MD5-gleich zu B2**
  (Toleranzvergleich: 9 × PASS). Damit ist die Wirkungsneutralität **empirisch belegt**,
  nicht nur strukturell: Für E2 bestätigt der Lauf die Begründung „kein Leser der neuen
  Tabellen", für E1 zeigt er, dass der Rechenkern `energy_carrier.co2` nicht
  konsumiert. Ein Gegenbeweis wäre eine Abweichung in einem der neun Projekte gewesen —
  es gibt keine.

## Zuordnung gegen `2026-08-28_B2`

Die Quelldatenbank hat sich zwischen den Basen geändert (28.08.2026 17:19:27 →
29.08.2026 00:02:08; die Anwendersitzung ist über den KD6-Nachtrag
„Booster-Bereinigung, Nutzerfreigabe 29.08.2026" belegt — Commit `300f971` um
00:02:20). **Alle Abweichungen sind Datenänderungen des Anwenders; keine ist ein
Codeeffekt:**

| Projekte | Ergebnis | Zuordnung |
|---|---|---|
| 1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024, 1030 | **PASS, alle 216 Dateien byte-/MD5-gleich** | Daten unverändert; Code (E1+E2) nachweislich wirkungsneutral |
| 1039 | **FAIL** (155 051 Abweichungen; 13 Dateien abweichend, 7 nur noch in B2, 5 byte-gleich) | **Umbau durch den Anwender**: Kessel und WP-Puffer entfernt (`Sim.bSimulationKessel` True → False, `Puffer.*` entfällt, `kessel_*`/`puffer_*.csv` entfallen), Bezeichner „Simulation Mehrgebäude" → „Simulation Wärmepumpe WG - BHKW"; Ausstattung jetzt „Tools: Wärmepumpe / Anlagen: BHKW" |
| 1040, 1041, 1042 | **FAIL — im Vergleichslauf nicht vorhanden** | **vom Anwender gelöscht**: die IDs fehlen in `Tab_Projekt` der Quelle (26 Projekte, per Direktabfrage auf der Arbeitskopie geprüft); 1043 ist neu hinzugekommen und nicht Teil der Referenzmenge |

## Der 1039-Zustand dieser Basis — WICHTIG

1039 ist im **Baustellenzustand des Anwenders** eingefroren: Das Wärmepumpen-Tool ist
aktiv, die einzige Anlage ist aber ein BHKW, und das BHKW-Tool ist aus — die
WP-Produktion ist ganzjährig 0 (`pruefen`-Hinweis „Gewerk aktiviert, aber kein Modul
zugeordnet"), `bhkw_*`-Ganglinien gibt es nicht. Sobald der Anwender das Projekt
fertigstellt, ist die Basis für 1039 zu erneuern.

## Was diese Basis nicht mehr absichert

Mit der Löschung von 1040–1042 und dem 1039-Umbau verliert die Referenzmenge die vier
Abdeckungen aus Konzept 11.1:

- **Mehrgebäude** (bisher 1039; 1008 deckt Mehrgebäude rechnerisch weiter ab),
- **zwei Puffer je Kanal / Parallelverbund** (1040),
- **Prozesswärme mit eigenem Puffer** (1041),
- **Booster-Kette mit Kombi-Speicher** (1042; die Booster-Temperaturkopplung war schon
  in B2 nicht scharf).

Die Ganglinien dieser Konstellationen liegen letztmalig in `2026-08-28_B2/`.
Unverändert offen: Kessel an Quellpuffer mit Wert ≠ 0, Wirtschaftlichkeit (kein
`WirtschaftlichkeitCtrl`-Aufruf — dort, und nur dort, wirken die neuen CO2-Werte).

---

## Maschinelles Laufprotokoll des Werkzeugs

**Zeitpunkt:** 29.08.2026 01:03:55

**Quelle (produktiv, nur gelesen):** `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`

**Arbeitskopie (beschrieben):** `C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb`

**Zielordner:** `C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\2026-08-29_E1E2`

**Gesamtdauer:** 00:00:19  |  **Timeout je Projekt:** 300 s

**Warnungen:** 15  |  **Fehler:** 0

## Projekte

| ID | Projekt | Ausstattung | Auswahlgrund | Dauer | CSV | Status |
|---|---|---|---|---|---|---|
| 1007 | Laurentiuskirche | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:01 | 29 | OK |
| 1008 | Heinestr 15 | Tools: Wärmepumpe / Anlagen: WP,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:01 | 21 | OK |
| 1011 | test1 | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:02 | 29 | OK |
| 1017 | WP_PV-Speicher | Tools: BHKW, Heizkessel, Stromspeicher / Anlagen: WP,Batterie,Kessel,BHKW | per --projekte vorgegeben | 00:01 | 21 | OK |
| 1018 | BHKW Test München | Tools: BHKW, Heizkessel / Anlagen: Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:02 | 22 | OK |
| 1021 | TestSpeichernUnter | Tools: Wärmepumpe / Anlagen: WP,Puffer / Quellspeicher(WP) | per --projekte vorgegeben | 00:01 | 21 | OK |
| 1023 | Wöhler - Test1 | Tools: Wärmepumpe, Heizkessel / Anlagen: WP,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:01 | 25 | OK |
| 1024 | Wöhler - Test2 | Tools: Wärmepumpe, Heizkessel, BHKW / Anlagen: WP,Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:01 | 26 | OK |
| 1030 | Referenz BHKW-Kaskade (Regressionstest) | Tools: BHKW, Heizkessel / Anlagen: Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:01 | 22 | OK |
| 1039 | Wärmepumpe WG - BHKW | Tools: Wärmepumpe / Anlagen: BHKW | per --projekte vorgegeben | 00:01 | 18 | OK |

## Ablauf

```
Referenzlauf gestartet.
Projektwurzel: C:\Users\DirkEngelmann\Documents\WP-Plan
Zielordner:    C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\2026-08-29_E1E2
Timeout je Projekt: 300 s

Quelle gefunden (ProgramData): C:\ProgramData\EPOS_PLAN\Kenndaten.accdb
Arbeitskopie angelegt: C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb (144 MB)
DB-Pfad der App verifiziert: C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb

Schema-Migration der Arbeitskopie ...
  C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
  Zeitpunkt: 29.08.2026 01:03:35
  Schemastand vorher: 54   (Zielstand 57)
  Bootstrap Schemamarker Tab_Applikation.SchemaVersion: OK
  Schritt 1  Spalten in Tab_Energieanlagen (Konzept 5.3): bereits erledigt
  Schritt 2  Spalten in Tab_Pufferspeicher, Tab_Klimaregion und Tab_Einstellungen (Konzept 5.1/12): bereits erledigt
  Schritt 3  Ergebnistabelle Tab_ErgebnisPufferspeicher (Konzept 6.6): bereits erledigt
  Schritt 4  Beziehungen der Pufferspeicher (Konzept 5.3 / B0-6b): bereits erledigt
  Schritt 5  Datenmigration Quellen/Senken (Konzept 5.5): bereits erledigt
  Schritt 6  Feature-Flag Kaskade_Zweikanalig in Tab_Einstellungen (Konzept Kapitel 9): bereits erledigt
  Schritt 7  Vorbelegung Extrapolation_erlaubt in Tab_Einstellungen (Konzept 13.4): bereits erledigt
  Schritt 8  Energieträger-Verweis ID_Carrier in Tab_Energieanlagen: bereits erledigt
  Schritt 9  Quellpuffer-Fremdschlüssel WQ_ID_Puffer (Etappe E0, Regel R7): bereits erledigt
  Schritt 10  Ergebnisspalte Quellwaerme in Tab_ErgebnisHeizkessel (Etappe D4): bereits erledigt
  Schritt 11  Stromspeicher: Gerätespalten, Tab_StromspeicherVariante, Tab_ErgebnisStromspeicher, Ladeparameter (AP3): bereits erledigt
  Schritt 12  Preismodell: Aufschlagsspalten, Tab_Preisreihe(Daten), Tab_Kostenprofil, Vorbelegung (AP4): bereits erledigt
  Schritt 13  BHKW-Regulär: Spalte Schwelle_Reserve, Vorbelegung 10 %, Leistungsgrenze 30 %: bereits erledigt
  Schritt 14  Parallelverbund: Tabelle Z_AnlagePufferVerbund samt Index und Beziehungen: bereits erledigt
  Schritt 15  Kessel-Wartungseinheit: Spalte Wartungskosten_Einheit, Vorbelegung €/a: bereits erledigt
  Schritt 16  Anlagenzeilen-Eindeutigkeit: Indizes auf (ID_Projekt, ID_WP | ID_Kessel | ID_BHKW | ID_PUFFER): bereits erledigt
  Schritt 17  Doppelt belegte Anlagenzeilen in eigene Gerätekopien überführen: bereits erledigt
  Schritt 18  BHKW-Vollbenutzungsstunden: VbhElektrisch in Tab_ErgebnisBHKW, VbhThermisch und VbhElektrisch in Tab_ErgebnisBHKWModul (Etappe E2): bereits erledigt
  Schritt 19  Kostenposition: Kostenart, Bemessung, IstErloes, Menge und Einheitpreis in Tab_ProjektWerte, Vorbelegung BETRAG (Etappe E3): bereits erledigt
  Schritt 20  Steuerangaben: Unternehmensart, raeumlicher Zusammenhang, Hocheffizienz, Jahresnutzungsgrad, Wahl der Energiesteuerentlastung und Aufteilungsmethode in Tab_ProjektWirtschaftlichkeit (Etappe E4): bereits erledigt
  Schritt 21  Tarifmodell: Rollen Bezug/Reststrom/Einspeisung mit drei Leistungspreismodellen in Tab_ProjektTarif, Aufschlagsschalter und KWK-Einspeiseverguetung in Tab_ProjektWirtschaftlichkeit, Vorbelegung ZONEN (Etappe E5): bereits erledigt
  Schritt 22  KWK-Zuschlag je Modul: Stichtag, Inbetriebnahme, Anlagenart, Eigenstromfall, Zuschlagssaetze, Vbh-Kontingent und Jahresdeckel in Tab_Energieanlagen (Etappe E6): bereits erledigt
  Schritt 23  Bilanzierung: Bilanzjahr, Bewertungsmethode KWK-Strom, Biomasse-Konvention und Nachhaltigkeitsnachweis in Tab_ProjektWirtschaftlichkeit, Vorbelegung KATALOG / NULLANSATZ / NACHWEIS_JA (L12 und L13): bereits erledigt
  Schritt 24  Doppelte Katalogeinträge aus dem zweiten Importlauf entfernen: bereits erledigt
  Schritt 25  Einheiten-Konsistenz: Tabelle energy_conversion sicherstellen, Spalten faktor_name und aktiv, Vorbelegung z-Faktor / Umrechnungsfaktor und aktiv = WAHR (Etappe K2, HF2/M-A): bereits erledigt
  Schritt 26  Einheiten-Seeds: Nm³ als Abrechnungseinheit der Gasträger, z-Faktor m³ → Nm³ mit 1,0, Namensberichtigung der Identitätsregeln (Etappe K3, HF3/M-B): bereits erledigt
  Schritt 27  Komponentenkatalog: Wärmezentrale, Bauliche Anlagen und Stromeinspeisung in Tab_KostenKomponente, Haupt- und Nebenpositionen in Tab_Kostenfaktor (Etappe K5, HF5/M-C): bereits erledigt
  Schritt 28  KWKG-Angaben: Tatbestand § 6 Abs. 3, Anlagenart § 8, Kostenanteil und Pauschalmodus § 9 in Tab_ProjektWirtschaftlichkeit; CO2-Preispfad ab 2028 auf 80 €/t (Etappe K6, HF6/M-D): bereits erledigt
  Schritt 29  Alttabellen entfernen: Beziehungen, dann DROP von Tab_Brennstoff_Projekt, energy_unit, energy_group, Tab_KostenKategorie, Tab_KWKG_Staffel, Tab_BHKW_neu und Tab_BHKW_Einf; Kategorie-3-Altzeilen in Tab_ProjektWerte loeschen (Etappe K6, HF1/M-E, Entscheidung E3): bereits erledigt
  Schritt 30  Doppelte Katalogeintraege in allen Katalogen der Registry entfernen (Dublettenpruefung D4): bereits erledigt
  Schritt 31  Eindeutiger Index auf die Namensspalte jedes Katalogs (Dublettenpruefung D5): bereits erledigt
  Schritt 32  Gespeicherte Abfragen nachziehen: Abfrage_Kostenfaktoren ohne Tab_KostenKategorie neu schreiben, Abfrage_ProjektKostenInvestBetrieb (Entscheidung E4), Abfrage1 und Tab_BHKW_Einfügen_Test entfernen (Nachzug zu Schritt 29): bereits erledigt
  Schritt 33  Leseprobe auf Abfrage_Kostenfaktoren; bei Bedarf mit ausgeschriebenem Kategorie-Ausdruck im ORDER BY neu schreiben (Nachzug zu Schritt 32): bereits erledigt
  Schritt 34  Verwaiste Geraetezeilen entfernen: Zeilen in Tab_WP, Tab_Heizkessel, Tab_BHKW, Tab_Pufferspeicher, Tab_PV, Tab_Solarkollektoren und Tab_Stromspeicher, auf die keine Anlagenzeile desselben Projekts mehr zeigt (Befund 22.08.2026): bereits erledigt
  Schritt 35  Gespeicherte Abfragen, zweiter Durchgang: Abfrage_Heizkessel_Kosten und Abfrage_Neues_Kosten_Model entfernen (fachlich tot), Abfrage_SST, Abfrage_Kuehlung_MaxLast und Abfrage_KenndatenKuehlung_Max auf die heutigen Spaltennamen bringen (Nutzerentscheid 22.08.2026): bereits erledigt
  Schritt 36  Gespeicherte Abfrage Abfrage_Energietraeger_Effektiv anlegen, falls sie fehlt (K6-Nachtrag, Protokoll Abschnitt 12): bereits erledigt
  Schritt 37  BHKW-Kosten abgleichen: Investition_kwel aus den fuenf Einzelposten nachziehen und, wo nur der spezifische Wert gepflegt ist, daraus Kosten_Modul ableiten - in Tab_BHKW und Tab_BHKW_STAMM (Befund 23.08.2026): bereits erledigt
  Schritt 38  Kostenvorlagen-Strukturen: Tab_KostenVorlage/-Position anlegen, Tab_ProjektWerte um VorlageID/StartJahr und energy_carrier um price_power/price_power_modus ergaenzen (Etappe KD1): bereits erledigt
  Schritt 39  Auslieferungsvorlagen saeen: 20 Standardvorlagen (10 Komponenten x Investition/Betrieb) mit den Positionslisten der Vorlagen-Folien, Saetze bewusst leer (Etappe KD1): bereits erledigt
  Schritt 40  Leistungspreis-Reihen: Tab_Preisreihe um ID_Energietraeger ergaenzen (Etappe KD4, FK6a): bereits erledigt
  Schritt 41  PV-Verguetung: Tab_ProjektPhotovoltaik anlegen und Marktwert-Solar-Monatsreihen 2024/2025/2026 saeen (Etappe P3): bereits erledigt
  Schritt 42  Katalogtraeger Fluessiggas saeen (Nachtrag Ä9): bereits erledigt
  Schritt 43  Fehlende VDI-3805-Katalogtraeger nachsaeen (Nachtrag Ä9): bereits erledigt
  Schritt 44  Strom-Leistungspreis freischalten: pricing_model ELECTRICITY erhaelt has_powerprice (Nachtrag Ä18): bereits erledigt
  Schritt 45  Anlagenkosten: Tab_ProjektWerte.ID_Anlage anlegen und den Bestand der jeweils ersten verbauten Anlage zuordnen (Ä20): bereits erledigt
  Schritt 46  Anlagenkosten-Geräteanker: Tab_ProjektWerte.ID_AnlageGeraet anlegen und aus den bestehenden Zuordnungen befuellen (Ä21): bereits erledigt
  Schritt 47  Anlagenkosten-Geräteanker aus den gültigen Zuordnungen neu ableiten (Ä24: Variantenkopien ankerten am Quellprojekt): bereits erledigt
  Schritt 48  Ganglinienkanal: Z_ProjektWaermebedarf.Kanal anlegen und den Bestand verhaltensneutral auf 'Heizung' vorbelegen (Paket K1, F18): bereits erledigt
  Schritt 49  Klassen-Set: Tab_Pufferspeicher.Nutzung_Heizung/_Brauchwasser/_Prozess anlegen und aus Verwendung befuellen; Tab_Einstellungen.Kanal_Knappheitsreihenfolge anlegen und vorbelegen (Paket K2, F5/F10): bereits erledigt
  Schritt 50  Senkenliste: Z_AnlageSenke anlegen, die Senken-Slots als Raenge uebernehmen (inkl. Regel R-Prozess) und Z_AnlagePufferVerbund um ID_Senke erweitern (Paket S1, L4/L5/F17): bereits erledigt
  Schritt 51  Altpfad-Stilllegung: Betriebstemperaturen aus Z_ProjektPufferSp an die Pufferzeilen uebernehmen und Kaskade_Zweikanalig im Bestand auf WAHR setzen (Paket A1, L1): bereits erledigt
  Schritt 52  Ergebnis je Kanal: Waermebedarf_/Deckung_/Entladung_Heizung, _Brauchwasser, _Prozess anlegen; Tab_ErgebnisPufferspeicher zusaetzlich um die Durchsatzsummen, ID_Anlage und T_oben_* erweitern (Paket E1): bereits erledigt
  Schritt 53  Schichtmodell: Tab_Pufferspeicher um Schichten_Anzahl, Hoehe, Lambda_Eff, T_Nutz_BW, die drei Entnahmehoehen und die beiden Leistungsgrenzen erweitern und verhaltensneutral vorbelegen (Paket P1, L7): bereits erledigt
  Schritt 54  Quellen-Ausbau: Tab_Quellprofil/Tab_QuellprofilDaten anlegen und Tab_Energieanlagen um WQ_Anschlusshoehe und WQ_ID_Quellprofil erweitern (Paket Q1, Konzept 8.1): bereits erledigt
  Temperaturbezug (Schritt 55): 115 Anlagenzeile(n) auf WQ_TemperaturModus = 'Berechnet' vorbelegt - das Bezugspaar des Quellanteils kommt damit aus dem Lauf (Rang-1-Senkenspeicher, sonst die gepflegte Kette, zuletzt 70/50 Grad C) und verlangt keine Datenpflege am Kessel. 23 Projekteinstellung(en) auf Booster_Lesepunkt = 'Davor' vorbelegt - der Booster liest den Speicherzustand ab jetzt am Stundenanfang statt nach der Ladephase der Vorebene; das AENDERT die Ergebnisse jedes Projekts mit gekoppeltem Booster.
  Schritt 55  Temperaturbezug: Tab_Energieanlagen um WQ_TemperaturModus erweitern und auf 'Berechnet' vorbelegen; Tab_Einstellungen um Booster_Lesepunkt erweitern und auf 'Davor' vorbelegen (Paket B2): OK
          - Tab_Energieanlagen: 1 Spalten angelegt, 0 bereits vorhanden
  CO2-Saat (Schritt 56): 20 Traeger gesetzt (davon 5 mit abgeleitetem Wert), 0 bereits auf dem Sollwert, 0 im Katalog nicht vorhanden; Gegenprobe ohne Abweichung. UNANGETASTET: Fluessiggas, Steinkohle, Braunkohlebrikett, Scheitholz, Holzpellets, Holzhackschnitzel (Saat der Schritte 42/43), Test (kein realer Traeger), energy_project_settings (Projektuebersteuerungen) und der Vorgabewert STROMMIX_CO2_G_JE_KWH = 380 (offene Entscheidung).
  Schritt 56  CO2-Saat der Katalogtraeger: energy_carrier.co2 auf die belegten BAFA-EEW-Werte setzen, wo der Katalog 0/NULL oder abweichend gepflegt ist (Etappe E1): OK
          - Biogas (id 49): co2 0 -> 152
          - Biogas 2 (id 61): co2 140 -> 152
          - Biogas Variante (id 66): co2 140 -> 152
          - Fernwärme (id 51): co2 0 -> 280
          - Erdgas LL (id 52): co2 0 -> 201
          - Erdgas E (id 63): co2 240 -> 201
          - Heizöl EL (id 56): co2 0 -> 266
          - Heizöl L (id 62): co2 310 -> 266
          - Heizöl L Variante (id 70): co2 310 -> 266
          - Heizöl L var (id 71): co2 310 -> 266
          - Heizöl S (id 67): co2 310 -> 288
          - Wasserstoff (id 68): co2 0 -> 385
          - Elektrische Energie (id 60): co2 560 -> 435
          - Elektrische Energie 2 (id 58): co2 0 -> 435
          - Strom Variante (id 54): co2 0 -> 435
          - Heizöl Bio 10 (id 53): co2 0 -> 246   [ABGELEITET - kein belegter BAFA-Wert]
          - Heizöl Bio 15 (id 57): co2 0 -> 237   [ABGELEITET - kein belegter BAFA-Wert]
          - Koks (id 59): co2 0 -> 335   [ABGELEITET - kein belegter BAFA-Wert]
          - Stadtgas (id 64): co2 240 -> 201   [ABGELEITET - kein belegter BAFA-Wert]
          - Tierische Fette (id 69): co2 210 -> 70   [ABGELEITET - kein belegter BAFA-Wert]
  Emissionsarten (Schritt 57): 7 Arten gesaet, 0 bereits vorhanden; Vorlagen 139 neu von 146 geplanten (BAFA-Saat 20, Gesetzesparameter 63, Brennstoff-Stamm 63); aktive Traegerwerte 81 neu von 81 geplanten; Berechnungsmodus CO2 in 1 Zeile(n) Tab_Applikation und 26 Projekt(en) vorbelegt. KEIN Rechenergebnis aendert sich: Die Altspalten bleiben unveraendert und fuehrend, die neuen Tabellen hat in dieser Fassung kein Leser.
  Schritt 57  Emissionsarten-Katalog: Tabellen emissionsart/emissionswert anlegen, sieben Arten, Vorlagen aus BAFA-Saat, Gesetzesparametern und Brennstoff-Stamm sowie die aktiven Traegerwerte saeen; Berechnungsmodus in Tab_Applikation und Tab_Projekt (Etappe E2): OK
          - Tabelle emissionsart: angelegt
          - Index idx_emissionsart_kuerzel: angelegt
          - Tabelle emissionswert: angelegt
          - Index idx_emissionswert: angelegt
          - Index idx_emissionswert_aktiv: angelegt
          - Beziehung FK_emissionswert_art (restriktiv): angelegt
          - Tab_Applikation: 1 Spalten angelegt, 0 bereits vorhanden
          - Tab_Projekt: 1 Spalten angelegt, 0 bereits vorhanden
  Abschlussprüfung Anlagenzeilen-Eindeutigkeit
          - Eindeutigkeitsindex idx_Anlage_ID_WP (Wärmepumpe): bereits vorhanden
          - Eindeutigkeitsindex idx_Anlage_ID_Kessel (Heizkessel): bereits vorhanden
          - Eindeutigkeitsindex idx_Anlage_ID_BHKW (BHKW): bereits vorhanden
          - Eindeutigkeitsindex idx_Anlage_ID_PUFFER (Pufferspeicher): bereits vorhanden
  Abschlusspruefung Katalog-Eindeutigkeitsindizes
          - Eindeutigkeitsindex UX_Tab_WP_STAMM_Bezeichner (WP): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Heizkessel_STAMM_Bezeichner (HEIZKESSEL): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Pufferspeicher_STAMM_Bezeichner (PUFFERSPEICHER): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Solarkollektoren_STAMM_Bezeichner (SOLARKOLLEKTOREN): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_PV_STAMM_Bezeichner (PV): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_BHKW_STAMM_Bezeichner (BHKW): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Stromspeicher_STAMM_Bezeichner (STROMSPEICHER): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Gebaeude_STAMM_Bezeichner (GEBAEUDE): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Klimaregion_STAMM_Name (KLIMAREGION): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Brauchwasser_STAMM_Bezeichner (BRAUCHWASSER): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Brauchwassertyp_STAMM_Bezeichner (BRAUCHWASSERTYP): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Stromverbraucher_STAMM_Bezeichner (STROMVERBRAUCHER): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Stromverbrauchertyp_STAMM_Typname (STROMVERBRAUCHERTYP): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Prozesswaerme_STAMM_Bezeichner (PROZESSWAERME): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Prozesstyp_STAMM_Bezeichner (PROZESSTYP): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Stromganglinie_STAMM_Bezeichner (STROMGANGLINIE): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Solarganglinie_STAMM_Bezeichner (SOLARGANGLINIE): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Waermebedarf_STAMM_Bezeichner (WAERMEBEDARF): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_DBTagV_STAMM_Bezeichner (GEBAEUDETYP): bereits vorhanden
  Abschlusspruefung Leseprobe Abfrage_Kostenfaktoren
          - Abschluss: Abfrage_Kostenfaktoren ist lesbar - nichts zu tun.
  Abschlusspruefung gespeicherte Abfragen (zweiter Durchgang)
          - Abschluss 35A: Abfrage_Heizkessel_Kosten: nicht vorhanden - nichts zu tun.
          - Abschluss 35A: Abfrage_Neues_Kosten_Model: nicht vorhanden - nichts zu tun.
          - Abschluss 35B: Abfrage_SST ist lesbar - nichts zu tun.
          - Abschluss 35B: Abfrage_Kuehlung_MaxLast ist lesbar - nichts zu tun.
          - Abschluss 35B: Abfrage_KenndatenKuehlung_Max ist lesbar - nichts zu tun.
          - Abschluss 35: 0 von 2 toten Abfragen entfernt, 0 von 3 Abfragen auf die heutigen Spaltennamen gebracht, 0 offen. Es gab nichts zu tun (Idempotenz-Nachweis: Genau das meldet ein zweiter Lauf).
  Abschlusspruefung BHKW-Kosten (Einzelposten und Investition_kwel)
          - Abschluss 37: Tab_BHKW (7 Zeilen unveraendert): 0 x Investition_kwel nachgezogen, 0 x Kosten_Modul abgeleitet, 5 bereits stimmig, 2 ohne jede Kostenangabe, 0 offen (Pel = 0). Gegenprobe: nichts mehr zu tun.
          - Abschluss 37: Tab_BHKW_STAMM (79 Zeilen unveraendert): 0 x Investition_kwel nachgezogen, 0 x Kosten_Modul abgeleitet, 42 bereits stimmig, 37 ohne jede Kostenangabe, 0 offen (Pel = 0). Gegenprobe: nichts mehr zu tun.
          - Abschluss 37: zusammen 0 angeglichen, 0 abgeleitet, 0 offen; 47 Zeilen waren bereits stimmig, 39 fuehren ueberhaupt keine Kosten. Es gab nichts zu tun (Idempotenz-Nachweis: Genau das meldet ein zweiter Lauf).
  Schemastand nachher: 57   (Zielstand 57)
  Parallelverbund (Schritt 14): 0 Zeilen in Z_AnlagePufferVerbund - kein Projekt führt einen Pufferverbund, der Rechenweg bleibt unverändert.
  Dublettenauflösung (Schritt 17): 0 Anlagenzeilen auf eine eigene Gerätekopie überführt - es gab keine doppelt belegte Anlagenzeile.
  Katalogbereinigung (Schritt 24): 0 doppelte Katalogeinträge entfernt - es gab keinen doppelt vergebenen Katalognamen.
  Katalogbereinigung alle Kataloge (Schritt 30): 0 doppelte Katalogeintraege entfernt - es gab keinen doppelt vergebenen Katalognamen.
  Anlagenzeilen-Eindeutigkeit (Schritt 16): 4 von 4 Eindeutigkeitsindizes aktiv, 0 doppelt belegte Anlagenzeilen - je Projekt und Gerät genau eine Zeile.
  Katalog-Eindeutigkeit (Schritt 31): 19 von 19 Eindeutigkeitsindizes aktiv - jeder Katalogname ist durch einen eindeutigen Index gesichert.
  Kostenarten (Schritt 19): 0 Kostenpositionen auf Bemessung "BETRAG" vorbelegt, 0 nach VDI 2067 eingeordnet - die Bemessung war bereits gesetzt, der Rechenweg bleibt unveraendert.
  Steuerangaben (Schritt 20): 0 Angaben ueber drei Spalten vorbelegt - die Steuerangaben standen bereits, es entsteht keine neue Gutschrift.
  Tarifmodell (Schritt 21): 0 Angaben ueber drei Spalten vorbelegt - es gibt keinen Tarifsatz oder er steht bereits; der Rechenweg bleibt unveraendert.
  Bilanzierung (Schritt 23): 0 Angaben ueber drei Spalten vorbelegt - die Bilanzierungsangaben standen bereits; der Rechenweg bleibt unveraendert.
  Energieträger-Abfrage (Schritt 36): Abfrage_Energietraeger_Effektiv war bereits vorhanden - nichts geändert.
  Gespeicherte Abfragen (Schritt 32): 0 Produktivabfrage erneuert, 0 von 3 Altabfragen entfernt - keine gespeicherte Abfrage verweist mehr auf eine in Schritt 29 gedroppte Tabelle.
  Leseprobe Abfrage_Kostenfaktoren (Schritt 33): bestanden - die Abfrage war bereits in Ordnung und wurde nicht angefasst.
  Verwaiste Geraetezeilen (Schritt 34): 0 Geraetezeilen und 0 Kennlinienzeilen aus 0 Projekten entfernt - auf jede Geraetezeile zeigt eine Anlagenzeile ihres Projekts.
  Gespeicherte Abfragen, zweiter Durchgang (Schritt 35): 0 von 2 toten Abfragen entfernt, 0 von 3 auf die heutigen Spaltennamen gebracht - es gab nichts zu tun.
  BHKW-Kosten (Schritt 37): 0 x Investition_kwel aus den Einzelposten nachgezogen, 0 x Kosten_Modul aus Investition_kwel abgeleitet, 0 offen - beide Seiten stimmen ueberein; es gab nichts zu tun.
Migration: ERFOLG (Zielstand 57).

Projektlandschaft wird gelesen ...
26 Projekte in Tab_Projekt.

Gewaehlte Referenzprojekte (10):
  - Projekt 1007 "Laurentiuskirche"
      Ausstattung: Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher | Anlagen: WP,PV,Batterie,Kessel,Puffer | Puffer(anderer Erzeuger)
      Grund:       per --projekte vorgegeben
  - Projekt 1008 "Heinestr 15"
      Ausstattung: Tools: Wärmepumpe | Anlagen: WP,Kessel,Puffer | Puffer(WP)
      Grund:       per --projekte vorgegeben
  - Projekt 1011 "test1"
      Ausstattung: Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher | Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer | Puffer(anderer Erzeuger)
      Grund:       per --projekte vorgegeben
  - Projekt 1017 "WP_PV-Speicher"
      Ausstattung: Tools: BHKW, Heizkessel, Stromspeicher | Anlagen: WP,Batterie,Kessel,BHKW
      Grund:       per --projekte vorgegeben
  - Projekt 1018 "BHKW Test München"
      Ausstattung: Tools: BHKW, Heizkessel | Anlagen: Kessel,BHKW,Puffer
      Grund:       per --projekte vorgegeben
  - Projekt 1021 "TestSpeichernUnter"
      Ausstattung: Tools: Wärmepumpe | Anlagen: WP,Puffer | Quellspeicher(WP)
      Grund:       per --projekte vorgegeben
  - Projekt 1023 "Wöhler - Test1"
      Ausstattung: Tools: Wärmepumpe, Heizkessel | Anlagen: WP,Kessel,Puffer | Puffer(WP)
      Grund:       per --projekte vorgegeben
  - Projekt 1024 "Wöhler - Test2"
      Ausstattung: Tools: Wärmepumpe, Heizkessel, BHKW | Anlagen: WP,Kessel,BHKW,Puffer
      Grund:       per --projekte vorgegeben
  - Projekt 1030 "Referenz BHKW-Kaskade (Regressionstest)"
      Ausstattung: Tools: BHKW, Heizkessel | Anlagen: Kessel,BHKW,Puffer
      Grund:       per --projekte vorgegeben
  - Projekt 1039 "Wärmepumpe WG - BHKW"
      Ausstattung: Tools: Wärmepumpe | Anlagen: BHKW
      Grund:       per --projekte vorgegeben

--- Projekt 1007 (Laurentiuskirche) ---
      | [01:03:37] DB-Pfad der App verifiziert: C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [01:03:38] Simulation startet fuer Projekt 1007 ...
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoTEC plus VCI 20/26CS/1-5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-15,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [01:03:39] Simulation beendet, Ergebnis-Kopf-ID 208.
      | [01:03:39] Projekt 1007: 29 CSV-Dateien, 99 Skalare.
Projekt 1007: OK, 29 CSV-Dateien, 00:01
--- Projekt 1008 (Heinestr 15) ---
      | [01:03:39] DB-Pfad der App verifiziert: C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [01:03:39] Simulation startet fuer Projekt 1008 ...
      | Simulation Warnung: Speicher-Registry: Puffer 1008007 (Vitocell 140-E 600 Liter) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 6,96 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Anlage „CS7800iLW 16": Die Sole-Wasser-Wärmepumpe hat keine konfigurierte Wärmequelle — gerechnet wird ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt. Die Quelle über den Chip „Quelle" der Erzeugerkarte wählen (Erdreich, konstante Temperatur, Quellprofil oder Pufferspeicher).
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoTEC plus VCI 20/26CS/1-5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur überschreitet in 221 Stunden die obere Stützstelle der Kennlinie (25,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS5800i AW 12 M + AW 5 OR-S': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [01:03:40] Simulation beendet, Ergebnis-Kopf-ID 209.
      | [01:03:40] Projekt 1008: 21 CSV-Dateien, 101 Skalare.
Projekt 1008: OK, 21 CSV-Dateien, 00:01
--- Projekt 1011 (test1) ---
      | [01:03:41] DB-Pfad der App verifiziert: C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [01:03:41] Simulation startet fuer Projekt 1011 ...
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „GC7000F 22 23 - MX25" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „GC7000F 22 23 - MX25 (2)" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-15,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 12 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 12 OR-T (2)': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [01:03:43] Simulation beendet, Ergebnis-Kopf-ID 210.
      | [01:03:43] Projekt 1011: 29 CSV-Dateien, 121 Skalare.
Projekt 1011: OK, 29 CSV-Dateien, 00:02
--- Projekt 1017 (WP_PV-Speicher) ---
      | [01:03:43] DB-Pfad der App verifiziert: C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [01:03:43] Simulation startet fuer Projekt 1017 ...
      | Simulation Warnung: Anlage „WPE-I 59 H 400 Premium": Die Sole-Wasser-Wärmepumpe hat keine konfigurierte Wärmequelle — gerechnet wird ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt. Die Quelle über den Chip „Quelle" der Erzeugerkarte wählen (Erdreich, konstante Temperatur, Quellprofil oder Pufferspeicher).
      | Simulation Warnung: Energieträger-Zuordnung: Der BHKW-Anlage „BHKW EW K 10 S [K] Heizol" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „eloBLOCK VE 28" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Zum Stichtag 01.01.2026 gab es noch keine Preisversion - es gilt die aelteste vorhandene.
      | [01:03:44] Simulation beendet, Ergebnis-Kopf-ID 211.
      | [01:03:44] Projekt 1017: 21 CSV-Dateien, 112 Skalare.
Projekt 1017: OK, 21 CSV-Dateien, 00:01
--- Projekt 1018 (BHKW Test München) ---
      | [01:03:44] DB-Pfad der App verifiziert: C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [01:03:45] Simulation startet fuer Projekt 1018 ...
      | Simulation Warnung: Speicher-Registry: Puffer 1054175 (Stora B 1000-6 ER 1 B) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 11,194 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „Vitocrossal 200 CM2 raumluftabh�ngig" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | [01:03:46] Simulation beendet, Ergebnis-Kopf-ID 212.
      | [01:03:46] Projekt 1018: 22 CSV-Dateien, 139 Skalare.
Projekt 1018: OK, 22 CSV-Dateien, 00:02
--- Projekt 1021 (TestSpeichernUnter) ---
      | [01:03:47] DB-Pfad der App verifiziert: C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [01:03:47] Simulation startet fuer Projekt 1021 ...
      | Simulation Warnung: Anlage „CS7800iLW 12": Der Speicher „allSTOR exclusiv VPS 800/3-7" ist ihre Wärmequelle, wird aber von keiner Anlage dieses Projekts geladen. Nach der Startfüllung liefe die Quelle leer.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [01:03:48] Simulation beendet, Ergebnis-Kopf-ID 213.
      | [01:03:48] Projekt 1021: 21 CSV-Dateien, 94 Skalare.
Projekt 1021: OK, 21 CSV-Dateien, 00:01
--- Projekt 1023 (Wöhler - Test1) ---
      | [01:03:48] DB-Pfad der App verifiziert: C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [01:03:49] Simulation startet fuer Projekt 1023 ...
      | Simulation Warnung: Anlage „CS7800iLW 12": Die Sole-Wasser-Wärmepumpe hat keine konfigurierte Wärmequelle — gerechnet wird ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt. Die Quelle über den Chip „Quelle" der Erzeugerkarte wählen (Erdreich, konstante Temperatur, Quellprofil oder Pufferspeicher).
      | Simulation Warnung: Anlage „CS6800iAW MB + AW 10 OR-T": Der Erzeuger-Vorlauf 45 °C liegt unter dem wirksamen Vorlauf 65 °C des Zielspeichers „Vitocell 140-E 600 Ltr". Der Erzeuger kann den Speicher nie auf Solltemperatur laden.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoVIT VKK 186/5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 12': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 12': Die Quelltemperatur überschreitet in 221 Stunden die obere Stützstelle der Kennlinie (25,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [01:03:49] Simulation beendet, Ergebnis-Kopf-ID 214.
      | [01:03:49] Projekt 1023: 25 CSV-Dateien, 135 Skalare.
Projekt 1023: OK, 25 CSV-Dateien, 00:01
--- Projekt 1024 (Wöhler - Test2) ---
      | [01:03:50] DB-Pfad der App verifiziert: C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [01:03:51] Simulation startet fuer Projekt 1024 ...
      | Simulation Hinweis: Kaskade: Der Heizkessel steht zwischen zwei Erzeugern der Speicherstufe. Er rechnet deshalb als Mitglied der Stundenschleife an seiner Kaskadenposition mit (Phase B) - ohne Puffer-Senke als reine Heizkreis-Stufe.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [01:03:51] Simulation beendet, Ergebnis-Kopf-ID 215.
      | [01:03:51] Projekt 1024: 26 CSV-Dateien, 155 Skalare.
Projekt 1024: OK, 26 CSV-Dateien, 00:01
--- Projekt 1030 (Referenz BHKW-Kaskade (Regressionstest)) ---
      | [01:03:52] DB-Pfad der App verifiziert: C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [01:03:52] Simulation startet fuer Projekt 1030 ...
      | [01:03:53] Simulation beendet, Ergebnis-Kopf-ID 216.
      | [01:03:53] Projekt 1030: 22 CSV-Dateien, 147 Skalare.
Projekt 1030: OK, 22 CSV-Dateien, 00:01
--- Projekt 1039 (Wärmepumpe WG - BHKW) ---
      | [01:03:53] DB-Pfad der App verifiziert: C:\Users\DirkEngelmann\Documents\WP-Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [01:03:54] Simulation startet fuer Projekt 1039 ...
      | [01:03:54] Simulation beendet, Ergebnis-Kopf-ID 217.
      | [01:03:54] Projekt 1039: 18 CSV-Dateien, 60 Skalare.
Projekt 1039: OK, 18 CSV-Dateien, 00:01

Fertig. Gesamtdauer 00:00:19
Erfolgreich: 10 von 10
```

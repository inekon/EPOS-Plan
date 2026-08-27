# Konzept: Kostendialoge EPOS-Plan — Design der Kosten- und Energieträger-Dialoge

**Rev. 1.2 — 25.08.2026 — Abnahme-Entscheidungen Philipps vollständig eingearbeitet, umsetzungsreif** ·
FK1–FK10 und FK6a entschieden (§ 15.1); darunter FK2 als Änderung („+ Position hinzufügen" statt
„Komponente hinzufügen"), FK3 als Verschärfung (Energiekosten erscheinen nie im Betriebskosten-Raster)
und FK6a: saisonale Leistungspreise bereits in KD4. **Keine offenen Fragen.**

Auftrag: Umsetzung der Vorlage „Kosten Desing-V1" (PPTX/PDF, 34 Folien, Stand 25.08.2026): Das Konzept für
Kosten- und Energiedaten wird umgebaut — die Kosten werden direkt mit den Anlagenkomponenten (Wärmepumpe,
BHKW, Heizkessel, Solarthermie, Photovoltaik, Puffer-/Stromspeicher) verknüpft, je Komponente entstehen
bewertete Stammvorlagen mit Varianten, die Dialoge folgen einem einheitlichen Gestaltungsmuster.

Grundlagen: Vorlage „Kosten Desing-V1" (alle 34 Folien inkl. 26 Screenshots ausgewertet);
`Konzept_Kosten_Energietraeger_EPOS-Plan.md` Rev. 2 (Etappen K1–K6 laut Protokollen umgesetzt, Schemastand 36);
`Konzept_Photovoltaik_Wirtschaftlichkeit_EPOS-Plan.md` Rev. 1 (zur Abnahme, inkl. PV-Now-Nachtrag);
`Konzept_Emissionsfaktoren_Quellenwahl_EPOS-Plan.md` Rev. 1 und `Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md`
Rev. 1 (beide zur Abnahme); Code-Prüfung `WindowsFormsApplication1` (Hauptbaum, 25.08.2026).

> **Verhältnis zu den Bestandskonzepten.** Dieses Konzept führt das Kosten/Energieträger-Konzept Rev. 2 fort
> und setzt auf dessen umgesetztem Stand (K1–K6) auf. Es erfindet Beschlossenes nicht neu: Leitentscheidungen
> L1–L9, Beschlüsse E1–E8 und die Hausregeln gelten unverändert weiter, soweit § 13 nicht ausdrücklich eine
> Änderung benennt. Der Ertrag/Bonus-Teil Photovoltaik verweist vollständig auf das PV-Wirtschaftlichkeits-
> konzept; der Emissionsteil der Energieträgerverwaltung ist die Oberflächen-Etappe (E3) des
> Emissionsfaktoren-Quellenwahl-Konzepts.

---

## 1. Auftrag und Delta zum umgesetzten Stand

### 1.1 Kernforderungen der Vorlage

1. **Kosten an die Komponente** (Folie 1): Investitions- und Betriebskosten hängen nicht mehr an einem
   zentralen Kosteneditor mit Komponentenliste, sondern an der Komponente selbst — erreichbar aus deren
   Eigenschaften-Dialog und aus der Verwaltung.
2. **Stammvorlagen mit Varianten** (Folien 2, 3): Je Komponente gibt es eine bewertete Vorlage
   (Positionsliste mit Bemessung und Satz) in einer Stammtabelle; mehrere Einträge je Komponente sind
   möglich (Varianten).
3. **Admin-Menü zweigeteilt** (Folie 3): Administration → Kosten mit den Einträgen
   **1. Kostenverwaltung** (Investitions- und Betriebskosten) und **2. Energieträgerverwaltung**
   (separates Untermenü).
4. **Kostengliederung nach VDI 2067** (Folien 4, 8–24): Je Komponente ein Positionsraster
   Aktionen · Position · Bemessung · Satz ⛓ Betrag netto · Worst/Best, mit Zeilen-Editieren/-Löschen,
   Zeile-Hinzufügen und Summenfuß netto/brutto.
5. **Ertrag/Bonus je Komponente** (Folien 8–17): zweiter Reiter im Kostendialog; BHKW nach
   KWKG/StromStG/EnergieStG (Folien 10–13), Photovoltaik nach dem PV-Wirtschaftlichkeitskonzept (Folie 17).
6. **Energieträgerverwaltung** (Folien 25, 26): je VDI-3805-Energieträger Vorlage + Varianten;
   Leistungspreis ergänzen (Strom, Erdgas, Biogas); CO₂ immer, weitere Emissionen nur falls verfügbar;
   wählbare Emissionsquellen mit Quelle und Datum; Kostenprofil kein eigener Reiter mehr, nur unter Strom.
7. **Übernahme Stamm → Projekt** (Folien 6, 26, 27): Beim Aufruf im Projekt wahlweise Übernahme aus der
   Admin-Stammvorlage (Default oder Variante) oder aus dem eigenen Projekt (Stammprojekt/Projektvariante).
8. **BEHG-/Emissionsblöcke raus aus den Anlagendialogen** (Folie 6): Die Emissionen nach BEHG wandern aus
   allen Komponenten-Eigenschaften (auch Heizkessel) in die Kostenkomponenten der Energieträger.
9. **Berichte & Kosten** (Folie 27): Übersicht zeigt Investitions-, Betriebs- **und** Energiekosten
   (Betriebskosten fehlen heute); „Kostenverwaltung öffnen" führt auf den Kosteneditor, der nur noch
   Investitions- und Betriebskosten enthält.
10. **Worst/Best und Startzeitpunkt** (Folie 34): Der ±-Knopf öffnet einen Dialog (Eingabe in % oder
    absolut, Semantik bester/schlechtester Fall relativ zum Erwartungswert); der Startzeitpunkt der
    Investition wird je Investition festlegbar (Default: aktuell), Betrieb und Energie laufen erst ab
    diesem Zeitpunkt.

### 1.2 Was bereits existiert (und bleibt)

| Baustein | Stand (K1–K6, PVW-Vorarbeit) |
|---|---|
| `Form_Kosten` | 4 Reiter Investition · Wartung · Energie · Kostenprofil; einklappbare Komponentengruppen; Fußsummen |
| Positionsmechanik | `Tab_ProjektWerte` je Position mit `Kostenart`, `Bemessung`, `IstErloes`, `Menge`, `Einheitpreis`, Szenariospalten `EingegebenerWert/BestCase/WorstCase` + 3 Nutzungsdauern |
| Kataloge | `Tab_KostenKomponente` (10 Komponenten), `Tab_Kostenfaktor` (flach: `Bezeichnung`, `StammID`, `IsMainComponent`, `Empfehlung_von/_bis`), `Tab_KostenGruppenKatalog` |
| Betriebskosten | `Form_Betriebskosten` mit VDI-2067-Katalog (12 Positionen `VDI_POS_*`, `DbWerte.cs:413-478`) und Empfehlungsbereichen; Vorbelegungszeile „0,0240 €/kWhel × 57.440 kWhel aus dem Lauf vom …" |
| Worst/Best | `Form_CaseEingabe` („Eingabe Worst/Best Case") je Position |
| Übernahmen | `Form_PlanwertUebernahme`/`TechnikPlanwertCtrl` (Technik → Kosten, mit Herkunftsspalte), `Form_BkUebernahme` (Stammprojekt → Variante), `Form_Kosten_Auswahl` (Trägervariante), `ProjektDuplizierenCtrl` (schema-getrieben) |
| Energieträger | `energy_carrier` ↔ `energy_project_settings` ↔ `Abfrage_Energietraeger_Effektiv`; `ucFuelSettings` mit Umrechnungsregeln (`faktor_name`, z-Faktor, kWh-Pflicht-Prüfer `EnergieEinheitenPruefung`) |
| KWKG/Steuern | `Tab_ProjektWirtschaftlichkeit.KWKG_*` (Tatbestand, Anlagenart, Kostenanteil, Pauschalmodus, Vbh-Kontingent), `KwkgSatzRechner`, einheitenrichtige Steuersätze und CO₂-Preispfad in `Tab_Gesetzesparameter` |
| Gesetzeswerte | ausschließlich `Tab_Gesetzesparameter` (+ `Form_Gesetzesparameter`, `GesetzKatalog.cs`) |

### 1.3 Das eigentliche Delta

| Nr. | Neu gefordert | Heute |
|---|---|---|
| D1 | Bewertete **Stammvorlagen je Komponente mit Varianten** (Positionen inkl. Bemessung + Satz) | Katalog flach und unbewertet; Zuordnung Position↔Komponente entsteht erst je Projekt |
| D2 | **Ein Kostendialog je Komponente** (Reiter Kosten Invest/Betrieb · Ertrag/Bonus), identisch in Admin und Projekt | zentraler Kosteneditor mit Komponentenliste links |
| D3 | Positionsraster mit **Bemessungskatalog** (15 Arten, § 5.3) und **Satz-⛓-Betrag-Kopplung** | 3 Bemessungsarten + Menge×Einheitpreis; keine Kopplungsanzeige |
| D4 | **Ertrag/Bonus-Reiter** je Komponente | Erlöse verteilt (Tarifstruktur, KWKG-Parameter, geplanter PV-Dialog) |
| D5 | **Energieträgerverwaltung als eigenes Untermenü**, Träger-Vorlagen + Varianten, Leistungspreis, Emissionsquellen-Auswahl | Energie-Reiter in `Form_Kosten`; Leistungspreis nur projektseitig für Strom (`custom_price_power`) |
| D6 | **Kostenprofil nur unter Strom**; Kosteneditor ohne Energie/Kostenprofil | eigener Reiter „Kostenprofil" (K4) und Reiter „Energie" in `Form_Kosten` |
| D7 | **BEHG-/Emissionsblöcke aus den Anlagendialogen** in die Energieträgerwelt | BEHG-Kasten + Emissionsfaktoren-Block in BHKW-/Kessel-Eigenschaften |
| D8 | Übernahme-Auswahl **Stamm-Vorlage (Default/Variante) oder gleiches Projekt (Stamm/Variante)** beim ersten Öffnen | Übernahmen existieren, aber nicht als Vorlagen-Übernahme mit Variantenwahl |
| D9 | Berichte & Kosten: **Betriebskosten in der Übersicht** | Übersicht zeigt Investition + Energie |
| D10 | `Form_CaseEingabe` + **%-Eingabe** und **Investitions-Startzeitpunkt** | nur Absolutwerte Best/Worst, Start immer t0 |

---

## 2. Leitentscheidungen dieses Konzepts

| Nr. | Entscheidung | Begründung |
|---|---|---|
| KL1 | **Ein Formular, drei Kontexte.** Der neue Komponenten-Kostendialog (§ 5) ist EIN Formular mit den Kontexten Admin-Stamm (Vorlagenpflege), Projekt (Projektwerte) und Aufruf aus dem Anlagendialog (= Projektkontext, vorgefiltert auf die Komponente). „Der Dialog soll identisch sein zu den unter Admin bereits bestehenden Dialogen" (Folien 6, 26, 27) wird damit wörtlich erfüllt; keine Doppelpflege von Masken. | Vorlage; Wartbarkeit |
| KL2 | **Vorlagen sind Stammdaten mit Werten.** Neue Tabellen `Tab_KostenVorlage`/`Tab_KostenVorlagePosition` (§ 4); der flache Katalog `Tab_Kostenfaktor` bleibt als Positionslexikon bestehen (StammID-Verweis), wird aber nicht mehr um Werte erweitert. | D1; kein Umbau der bestehenden Projektwelt |
| KL3 | **Projektwelt bleibt `Tab_ProjektWerte`.** Die Übernahme materialisiert Vorlagenpositionen als normale Projektpositionen (eine Zeile je Position, `StammID`-Verweis, neu `VorlageID`-Herkunft). Rechenkern, Szenarien, Berichte und KI-Aktionen bleiben unberührt. | Ergebnisneutralität; keine dritte Kostenwahrheit |
| KL4 | **Satz hat Vorrang, Betrag ist Ableitung.** Ist ein Satz gepflegt, wird der Betrag aus Satz × Bezugsgröße gerechnet; das Betragsfeld ist dann gesperrt, aber nicht geleert (Bannertext der Vorlage). Ohne Satz ist der Betrag frei editierbar. Kopplungssymbol ⛓ mit Tooltip „Satz und Betrag netto sind verknüpft und werden bei Eingabe umgerechnet" (Folie 19). | Vorlage, Folien 8/19 |
| KL5 | **Netto bleibt Rechenwahrheit.** Alle Beträge und Bezugsgrößen netto; die Bruttosumme im Fuß ist reine Anzeige. Der Umsatzsteuersatz kommt aus dem **vorhandenen** Katalogwert `UMSATZSTEUER_REGELSATZ` (`Tab_Gesetzesparameter`, Klasse UMSATZSTEUER, seit Etappe E1 gesät: 19,0 % ab 2007, GESICHERT — KD1-Befund: kein neuer Seed nötig). Keine Rückkehr der 1,19-Logik in Rechenwege. | Rev.-2-Abgrenzung „EPOS-Plan rechnet netto" |
| KL6 | **Ertrag/Bonus zeigt vorhandene Wahrheiten, schafft keine neuen.** BHKW-Reiter parametriert die HF6-Größen (`KWKG_*`, Gesetzeskatalog); PV-Reiter bettet den Inhalt des PV-Wirtschaftlichkeitsdialogs ein (eine Vergütungswahrheit, Befund V4/F7 des PV-Konzepts). | Reibungspunkt „doppelte Vergütungswahrheit" |
| KL7 | **Energiekosten erscheinen ausschließlich in der `energy_*`-Welt** (FK3 entschieden 25.08.2026, schärfer als der ursprüngliche Basiszeilen-Vorschlag): Die Folien-Positionen „Brennstoffkosten" (Kessel) und „Stromkosten Verdichter" (WP) entfallen **ersatzlos** aus dem Betriebskosten-Raster — auch nicht als nachrichtliche Zeilen; keinerlei Energiekosten im Betriebskosten-Dialog. Die %-Bemessungen „% der Brennstoffkosten"/„% der Stromkosten" beziehen ihre Basis **direkt aus der Energieträgerwelt** (Trägerkosten der Komponente aus dem letzten Simulationslauf, `KostenEmissionRechner`); die Basis wird als Herkunftstext am Satzfeld ausgewiesen („Basis: Brennstoffkosten 12.345 €/a aus Lauf vom …"), nie als Positionszeile. Vor dem ersten Lauf ist die Basis 0 mit Hinweistext. | Entscheidung Philipp (FK3); Doppelzählungsverbot; Kategorie 3 tot |
| KL8 | **Emissionen nur noch am Energieträger, mit Quellenobjekt.** Der Emissionsteil des Trägerdialogs setzt das Quellen-/Variantenmodell (`emissionsquelle`/`emissionsfaktor`, F1–F7) um: im Trägerdialog sitzt die **Ausnahme je Träger**, die **Leitquelle** liegt auf Projektebene. BEHG-Werte kommen als Quelle `BEHG_V` (Heizöl 291 · Flüssiggas 239 · Erdgas 202 g CO₂/kWh, aus t CO₂/GJ × 3,6). CO₂ ist Pflichtangabe je Träger; übrige Schadstoffe NULL = „in dieser Quelle nicht enthalten" (nie stille 0). | D7/D8; Emissionsfaktoren-Konzept |
| KL9 | **Keine neue Szenariomechanik.** Worst/Best läuft weiter über `Form_CaseEingabe` und die drei Szenariospalten; der Dialog wird erweitert (±% oder absolut, Startzeitpunkt, § 11), nicht ersetzt. Rechenverfahren bleibt Kapitalwertmethode DIN EN 17463/ValERI; VDI 2067 liefert ausschließlich die Kostengliederung. | bindende Festlegung Rev. 2 |

---

## 3. Menüstruktur und Einstiege

### 3.1 Administration (Folie 3)

```
Administration
└── Kosten
    ├── Kostenverwaltung …        (Betriebs- und Investitionskosten: Stammvorlagen je Komponente)
    └── Energieträgerverwaltung … (separates Untermenü: Träger-Vorlagen, Preise, Emissionen)
```

Heutige Einträge „Kosten" / „Kosten Admin" (`MDIMainForm.cs:608-625`) werden ersetzt:
„Kostenverwaltung …" öffnet die Komponentenauswahl (§ 5.1) im **Admin-Kontext**;
„Energieträgerverwaltung …" öffnet den Trägerdialog (§ 7) im **Admin-Kontext**.
`Form_KostenAdmin`/`Form_KostenfaktorItem` (Katalogpflege der Positionsbezeichnungen) bleiben als
Unterfunktion der Kostenverwaltung erreichbar (Knopf „Positionskatalog …"), verschwinden aus dem Menü.

**Umgesetzt 26.08.2026 (Ä7):** Alteinträge „Kosten"/„Kosten Admin" aus dem Menü entfernt,
Menütext „Kostenverwaltung …" (Konzeptwortlaut), Knopf „Positionskatalog…" im Fuß der
Kostenverwaltung öffnet `Form_KostenAdmin`; Komponentenauswahl gemäß Ä7 auf die sieben
Anlagen-Komponenten begrenzt (Dropdown der Kostenverwaltung, Blöcke des Projekt-Kosteneditors).

**Nachtrag KD6a (26.08.2026, Nutzerabnahme):** Der Projekt-Einstieg „Kostenverwaltung öffnen…"
und die Anlagendialog-Knöpfe (§ 9) führen jetzt in den NEUEN Kostendialog im **Projektmodus**
(`Form_KostenKomponente.SetProjekt` — der in KD3 auf KD6 verschobene dritte Kontext des § 5):
gleiche Optik und Bedienung wie der Stammkontext, ohne Variantenzeile; „Aus Vorlage übernehmen…"
= §-8-Mechanik, ± = Worst/Best + Startjahr (§ 11). `Form_Kosten` ist kein Einstieg mehr und bleibt
Logikträger (Summen-/Positionslesewege). Daneben neuer Direkteinstieg „Energieträgerverwaltung…"
auf der Kosten-Seite. Die Wirtschaftlichkeitsübersicht erhielt die Kartensprache der Kosten-Seite
(vier Kennzahl-Kacheln: Kapitalwert ggue. Stamm, Annuität, Amortisation, IRR der besten Variante).

### 3.2 Projekt-Einstiege

| Einstieg | Verhalten |
|---|---|
| Reiter „Berichte && Kosten" → Seite „Kosten" → „Kostenverwaltung öffnen…" (Folie 27; heute `UcBkKosten` „Verwaltung") | öffnet den Kosteneditor im Projektkontext — **nur Investitions- und Betriebskosten** (§ 6.4); arbeitet wie heute auf der markierten Zeile (Stammprojekt oder Variante) |
| Anlagen-Eigenschaften-Dialog (§ 9) | Knöpfe „Investitionskosten…", „Betriebskosten…", „Energiekosten…" öffnen den jeweiligen Dialog im Projektkontext, vorgefiltert auf die Komponente |
| Administration → Kosten | Stammvorlagen (ohne Projektbezug) |

Der tote Einstieg `Form_Start.btn_Kosten_Click` bleibt tot (wird zur Laufzeit entfernt) und wird bei
Gelegenheit mit ausgebaut.

---

## 4. Stammdatenmodell: Vorlagen und Varianten

### 4.1 Neue Tabellen

**`Tab_KostenVorlage`** — eine Zeile je Komponente und Variante:

| Spalte | Typ | Inhalt |
|---|---|---|
| `ID` | LONG (MAX+1-Muster, kein AutoWert) | Schlüssel |
| `KomponentenID` | LONG → `Tab_KostenKomponente.ID` | Komponente |
| `KategorieID` | LONG (1 = Investition, 2 = Betrieb) | Kategorie; Persistenzkonstanten aus `Form_Kosten` |
| `Name` | TEXT(100) | Variantenname; die Auslieferungsvorlage heißt „Standard" |
| `IstStandard` | YESNO | genau eine Standardvariante je Komponente+Kategorie (Prüfregel, kein DB-Constraint) |
| `ReadOnly` | YESNO | Auslieferungs-Seeds analog `Tab_Brennstoff_Stamm.ReadOnly`; Kopie „Speichern unter" erzeugt editierbare Variante |
| `Bemerkung` | MEMO | frei |
| `GeaendertAm` | DATETIME | Pflegestand |

**`Tab_KostenVorlagePosition`** — eine Zeile je Position der Vorlage:

| Spalte | Typ | Inhalt |
|---|---|---|
| `ID` | LONG (MAX+1) | Schlüssel |
| `VorlageID` | LONG → `Tab_KostenVorlage.ID` | Zugehörigkeit |
| `StammID` | LONG → `Tab_Kostenfaktor.StammID`, nullable | Verweis ins Positionslexikon (NULL bei freier Position) |
| `Bezeichnung` | TEXT(255) | Anzeige (bei StammID-Verweis vorbelegt, editierbar) |
| `Kostenart` | TEXT(20) | `DbWerte.KOSTENART_*` inkl. `zuschuss` |
| `Bemessung` | TEXT(30) | `DbWerte.BEMESSUNG_*` (Katalog § 5.3) |
| `Satz` | DOUBLE, nullable | Satz in der Einheit der Bemessung; NULL = nicht gepflegt |
| `BetragNetto` | DOUBLE, nullable | fester Betrag (nur bei absoluten Bemessungen; sonst Ableitung im Projekt) |
| `IstErloes` | YESNO | wie `Tab_ProjektWerte.IstErloes` |
| `Nutzungsdauer` | DOUBLE, nullable | VDI-2067-Nutzungsdauer [a] als Vorbelegung (Folie 7) |
| `Empfehlung_von` / `Empfehlung_bis` | DOUBLE, nullable | Hinweisbereich (übernimmt die Rolle der Katalog-Empfehlungen für Vorlagenpositionen) |
| `Sortierung` | LONG | Reihenfolge im Raster |

Regeln: NULL heißt durchgängig „nicht gepflegt", nie 0. Kein DDL-DEFAULT auf Fachwerten; Seeds per
DML-Migrationsschritt, idempotent, Zweitlauf = 0 Änderungen (Schrittnummer bei Umsetzung am Zähler
prüfen — Stand 36 nach `483b605`). Beide Tabellen zusätzlich in
`WirtschaftlichkeitCtrl.StelleTabellenSicher()`? **Nein** — sie gehören der Kostenwelt, nicht der
Wirtschaftlichkeit; nur Migrationskatalog. `ProjektDuplizierenCtrl` kopiert sie **nicht** (keine
`ID_Projekt`-Spalte — Stammdaten), es ist nichts zu pflegen.

### 4.2 Projektseite

`Tab_ProjektWerte` erhält eine Spalte **`VorlageID`** (LONG, nullable): Herkunftsvermerk der Übernahme
(§ 8). Keine stille Kopplung — nach der Übernahme sind Projektwerte eigenständig; die Herkunft dient
Anzeige („aus Vorlage ‚BHKW Standard', übernommen 25.08.2026") und späterem Abgleich, nie automatischem
Überschreiben. Muster: Abweichungsanzeige der Planwert-Übernahme.

### 4.3 Seeds (Auslieferungsvorlagen)

Je Komponente eine Standardvariante Investition + eine Standardvariante Betrieb mit den Positionslisten
aus § 5.6/§ 5.7, `ReadOnly = true`, Sätze leer (NULL) — die Vorlage liefert Struktur und Bemessung,
keine erfundenen Preise. Wo die Alt-Empfehlungsbereiche existieren (BHKW 3,0–9,0 % usw.), werden sie in
`Empfehlung_von/_bis` der entsprechenden Positionen übernommen. Für Wärmezentrale, Bauliche Anlagen und
Stromeinspeisung (Komponenten ohne eigene Folien-Vorlage) entstehen Minimal-Vorlagen aus den
K5-Positionskatalogen (BHKW-Einbindung, Heizungstechnik, Abgasanlage; Heizraum, Schornstein, …).

---

## 5. Der Komponenten-Kostendialog („Kostenverwaltung ‹Komponente›")

### 5.1 Rahmen und Komponentenauswahl

Neues Formular `Form_KostenKomponente` (Arbeitstitel; programmatische UI, `MyResource`-Präfix `KDLG_*`,
de + en, `HilfeKontext`-Eintrag). Aufbau nach Vorlagen-Mockup:

- **Kopf:** Titel „Kostenverwaltung ‹Komponente›", Untertitel „Investitionskosten nach VDI 2067" bzw.
  „Betriebskosten nach VDI 2067", Info-Knopf.
- **Reiter:** „Kosten Invest/Betrieb" · „Ertrag/Bonus" (§ 6). Innerhalb des ersten Reiters schaltet ein
  Segment/Unterreiter zwischen Investitions- und Betriebskosten (die Vorlage zeigt je Folie eine
  Kategorie; beide Raster sind baugleich).
- **Hinweisbanner** (gelb, schließbar): „Alle Beträge und alle Bezugsgrößen sind NETTO. Der
  Umsatzsteuersatz kommt aus dem Katalog ‚gesetzliche Parameter'. Eine gepflegte Satzangabe hat
  Vorrang — das Absolutfeld wird dann gesperrt, aber nicht geleert."
- **Kontextzeile:** Im Admin-Kontext Variantenwahl (Klappliste der `Tab_KostenVorlage`-Einträge,
  Knöpfe „Neu…", „Speichern unter…", „Löschen" — `ReadOnly`-Vorlagen nur kopierbar); im Projektkontext
  Herkunftszeile (§ 4.2) und ggf. die Vorbelegungszeile aus dem Simulationslauf (Mengenbasis
  der kWh-Bemessungen, § 5.4).
  Variantennamen folgen einheitlich dem Schema „‹Name› — Variante ‹n›" (FK9 entschieden
  25.08.2026; gilt ebenso für Trägervarianten, § 7.1).
- Die **Komponentenauswahl** davor: Aufruf aus dem Admin-Menü öffnet zunächst eine Übersicht der
  Komponenten (Kartenmuster wie `Views\Simulation\ErzeugerKarte`, Folie 1); Aufruf aus dem
  Anlagendialog springt direkt in die Komponente.

### 5.2 Positionsraster

Spalten (Folien 8/19):

| Spalte | Verhalten |
|---|---|
| **Aktionen** | Stift = Zeileneditor (Name, Kostenart, Nutzungsdauer, Empfehlungsbereich), Papierkorb = Zeile löschen (mit Rückfrage) — Anforderung Folie 19 „jede Zeile Symbol zum Editieren und Löschen" |
| **Position** | Bezeichnung |
| **Bemessung** | Klappliste aus dem Bemessungskatalog (§ 5.3); bestimmt die Einheit hinter dem Satzfeld |
| **Satz** | Zahlenfeld mit Einheit (€/kW, €/kWh, %, €/m², €/kWp, …); Validierung `Program.ZahlParsen/ZahlPruefen/ZahlFaerben` |
| **⛓** | Kopplungssymbol zwischen Satz und Betrag; Tooltip s. KL4 |
| **Betrag netto [€]** bzw. **[€/a]** | bei gepflegtem Satz gesperrt und live gerechnet; bei absoluten Bemessungen Eingabefeld |
| **Nutzungsdauer [a]** | sichtbare Spalte im **Investitionsraster** (FK4 entschieden 25.08.2026): vorbelegt aus der Vorlage (VDI 2067), editierbar; schreibt die Nutzungsdauer der Projektposition (Szenarien erben den Wert, Abweichungen je Szenario weiter über `Form_CaseEingabe`). Im Betriebskostenraster entfällt die Spalte |
| **Worst/Best** | ±-Knopf → `Form_CaseEingabe` (§ 11) |

Unter dem Raster: Leerzeile „+ Neue Position hinzufügen…" (Bezeichnung, Bemessung wählen…, Satz…,
Betrag…) und Knopf **„+ Position hinzufügen"** (FK2 entschieden: umbenannt, § 5.5). Fuß:
„Summe Investitionskosten netto: … €" / „Summe Betriebskosten netto: … €/a" und
„Summe brutto: … € (Umsatzsteuer 19,0 % aus dem Katalog)" (KL5). Zuschuss-Zeilen
(`Kostenart = 'zuschuss'`) erscheinen mit negativem Ausweis in der Summe (L7/E7 unverändert).

### 5.3 Bemessungskatalog

Vereinigungsmenge der Vorlagen-Folien; Persistenzwerte nach Drei-Schichten-Regel als neue
`DbWerte.BEMESSUNG_*`-Konstanten (deutsch, eingefroren), Anzeigetexte über `MyResource`:

| Anzeige (de) | Einheit Satz | Bezugsgröße im Projekt | Kategorie |
|---|---|---|---|
| je kW Leistung | €/kW | thermische Nennleistung (Kessel) | I |
| je kW Heizleistung | €/kW | WP-Nennleistung | I |
| je kW elektrisch | €/kW | elektrische Nennleistung (BHKW) | I |
| je kWp Leistung | €/kWp | PV-Leistung (kWp, geteilte Hilfsfunktion aus PV-Konzept Befund V3) | I |
| je kWh Kapazität | €/kWh | Speicherkapazität | I |
| je m² Kollektorfläche | €/m² | Kollektorfläche | I |
| fester Betrag | € | — | I |
| % der Erzeugerkosten | % | Betrag der Hauptposition (erste Position der Vorlage, `IsMainComponent`-Logik) | I |
| % der Investition | % | Summe Investitionskosten der Komponente **vor Zuschussabzug** (Regel Rev. 2) | I + B |
| fester Jahresbetrag | €/a | — | B |
| €/a | €/a | — (freier Jahresbetrag, Altbestand Folie 19) | B |
| je kWh thermisch | €/kWh | erzeugte Wärme aus dem Simulationslauf | B |
| je kWh elektrisch | €/kWh | erzeugter/bezogener Strom aus dem Simulationslauf | B |
| % der Brennstoffkosten | % | Brennstoffkosten des Komponententrägers aus dem letzten Simulationslauf — direkt aus der Energieträgerwelt, Ausweis als Herkunftstext (KL7/FK3) | B |
| % der Stromkosten | % | Stromkosten des Trägerbezugs aus dem letzten Simulationslauf — direkt aus der Energieträgerwelt, Ausweis als Herkunftstext (KL7/FK3) | B |

Bestehende Persistenzwerte (`EUR_PRO_H`, `EUR_PRO_KWH`, `PROZENT_INVESTITION`, Menge×Einheitpreis)
bleiben gültig; die neuen Konstanten kommen hinzu, Altdaten werden nicht migriert. „fester
Jahresbetrag" und „€/a" werden auf **einen** Persistenzwert gelegt (zwei Anzeigetexte wären
Doppelpflege; die Folie 19 zeigt beide nur historisch) — Anzeige einheitlich „fester Jahresbetrag".

### 5.4 Kopplung Satz ⛓ Betrag

- Admin-Kontext (Stammvorlage): Bezugsgrößen existieren noch nicht → bei bezugsgrößen-abhängigen
  Bemessungen bleibt das Betragsfeld leer und gesperrt (Anzeige „—"); nur Satz wird gepflegt.
  Bei absoluten Bemessungen ist der Betrag das Satzfeld (ein Wert).
- Projektkontext: Bezugsgröße kommt aus Technikdaten (`TechnikPlanwertCtrl`-Kette, Klartextanzeige
  „653,60 €/kWel × 250,00 kWel") bzw. Simulationslauf (kWh-Bemessungen, Vorbelegungszeile) bzw.
  Positionssummen (%-Bemessungen). Änderung der Bezugsgröße rechnet den Betrag neu; manueller
  Betrag ohne Satz bleibt stehen.
- %-Kaskaden rechnen in fester Reihenfolge: Hauptposition → % der Erzeugerkosten → Summe →
  % der Investition; zirkuläre Bezüge sind durch den Katalog ausgeschlossen (keine %-Position ist
  Basis einer anderen %-Position).

### 5.5 „+ Position hinzufügen" (FK2 entschieden, 25.08.2026)

Der untere Knopf der Mockups heißt in der Umsetzung **„+ Position hinzufügen"** — bewusste
Umbenennung gegenüber der Vorlagen-Beschriftung „Komponente Hinzufügen" (Folien 8–24). Er legt
einen **weiteren Eintrag (Position) zur bereits geöffneten Komponente** an, gleichwertig zur
gestrichelten Eingabezeile; beide lösen dieselbe Aktion aus (der Knopf ist der gut sichtbare
Zweitweg). Es gibt **kein** Instanzkonzept über diesen Knopf (kein „BHKW 2"-Block, keine
`InstanzNr`-Spalte); Varianten entstehen ausschließlich über die Kontextzeile (§ 5.1), neue
Komponententypen bleiben ein seltener Admin-Katalogvorgang in der Komponentenübersicht (§ 5.1).

### 5.6 Auslieferungs-Positionslisten Investition (Folien 8, 9, 14, 15, 16)

| Komponente | Positionen (Bemessung) |
|---|---|
| **Heizkessel** | Wärmeerzeuger (Kessel) (je kW Leistung) · Zubehör (fester Betrag) · MSR-Technik/Automation (% der Erzeugerkosten) · Abgasanlage/Schornstein (fester Betrag) · Montage und Installation (% der Investition) · Bauliche Anlagen (fester Betrag) · Planung/Baunebenkosten (% der Investition) |
| **BHKW** | BHKW-Modul (Kompaktaggregat) (je kW elektrisch) · Spitzenlastkessel/Zubehör (fester Betrag) · Wärmespeicher (Puffer) (fester Betrag) · MSR-Technik/Schaltanlage (% der Erzeugerkosten) · Abgasanlage/Schalldämpfer (fester Betrag) · Montage und Einbringung (% der Investition) · Bauliche Anlagen (Schallschutz) (fester Betrag) · Planung/Baunebenkosten (% der Investition) |
| **Wärmepumpe** | Wärmepumpe (Aggregat) (je kW Heizleistung) · Erschließung (Sonden/Kollektor/Luft) (fester Betrag) · Zubehör (fester Betrag) · MSR-Technik/Automation (% der Erzeugerkosten) · Montage, Installation & Kältetechnik (% der Investition) · Bauliche Anlagen (Fundament/Bohrung) (fester Betrag) · Planung/Baunebenkosten (% der Investition) |
| **Solarthermie** | Sonnenkollektoren (je m² Kollektorfläche) · Zubehör (Montagesystem/Solarstation) (fester Betrag) · Wärmespeicher (Solarspeicher) (fester Betrag) · MSR-Technik/Solarregler (% der Erzeugerkosten) · Montage und Verrohrung (% der Investition) · Bauliche Anlagen (Gerüst etc.) (fester Betrag) · Planung/Baunebenkosten (% der Investition) |
| **Photovoltaik** | PV-Module (je kWp Leistung) · Wechselrichter (fester Betrag) · Montagesystem/Unterkonstruktion (fester Betrag) · Batteriespeicher (je kWh Kapazität) · Elektrotechnik/Netzanschluss (% der Erzeugerkosten) · Montage und Installation (% der Investition) · Bauliche Anlagen (Gerüst etc.) (fester Betrag) · Planung/Baunebenkosten (% der Investition) |
| **Pufferspeicher / Stromspeicher / Wärmezentrale / Bauliche Anlagen / Stromeinspeisung** | Minimal-Vorlagen aus den K5-Katalogen (§ 4.3); Puffer-/Stromspeicher zusätzlich Hauptposition „Speicher (je kWh Kapazität)" |

### 5.7 Auslieferungs-Positionslisten Betrieb (Folien 19–24)

| Komponente | Positionen (Bemessung) |
|---|---|
| **BHKW** | Vollwartung/Wartung BHKW (je kWh elektrisch) · Instandhaltung BHKW (% der Investition) · Instandhaltung Heizkessel (fester Jahresbetrag) · Instandhaltung Wärmezentrale (% der Investition) · Instandhaltung bauliche Anlagen (% der Investition) · Instandhaltung Stromeinspeisung (% der Investition) · Personalkosten (% der Investition) · Steuern, Versicherung, Verwaltung (% der Investition) · Hilfsenergiekosten (% der Brennstoffkosten) · Reserveleistungskosten (fester Jahresbetrag) · Sonstige Kosten (fester Jahresbetrag) |
| **Heizkessel** | Vollwartung/Wartung Kessel (je kWh thermisch) · Instandhaltung Heizkessel (fester Jahresbetrag) · Instandhaltung bauliche Anlagen (% der Investition) · Hilfsenergiekosten Strom (% der Brennstoffkosten — Basis aus der Energieträgerwelt, KL7/FK3) · Schornsteinfeger/Messung (fester Jahresbetrag) · Personalkosten/Bedienung (% der Investition) · Steuern, Versicherung, Verwaltung (% der Investition) · Sonstige Kosten (fester Jahresbetrag) |
| **Wärmepumpe** | Wartung Wärmepumpe (fester Jahresbetrag) · Instandhaltung Wärmepumpe (% der Investition) · Instandhaltung Umweltwärmequelle (% der Investition) · Instandhaltung bauliche Anlagen (% der Investition) · Hilfsenergiekosten Pumpen (% der Stromkosten — Basis aus der Energieträgerwelt, KL7/FK3) · Dichtheitsprüfung (Kältemittel) (fester Jahresbetrag) · Personalkosten/Bedienung (% der Investition) · Steuern, Versicherung, Verwaltung (% der Investition) · Sonstige Kosten (fester Jahresbetrag) |
| **Solarthermie** | Wartung Solarthermie-Anlage (fester Jahresbetrag) · Instandhaltung Sonnenkollektoren (% der Investition) · Instandhaltung Solarspeicher/Zubehör (% der Investition) · Instandhaltung bauliche Anlagen (% der Investition) · Hilfsenergiekosten (Solarpumpe) (je kWh elektrisch) · Prüfung/Tausch Wärmeträgermedium (fester Jahresbetrag) · Personalkosten/Bedienung (% der Investition) · Steuern, Versicherung, Verwaltung (% der Investition) · Sonstige Kosten (fester Jahresbetrag) |
| **Pufferspeicher** | Wartung/Sichtprüfung Speicher (fester Jahresbetrag) · Instandhaltung Pufferspeicher (% der Investition) · Instandhaltung Dämmung/Isolierung (% der Investition) · Instandhaltung Armaturen/Pumpen (% der Investition) · Hilfsenergiekosten (Speicherladepumpe) (je kWh elektrisch) · Wasserbehandlung/Nachspeisung (fester Jahresbetrag) · Personalkosten/Bedienung (% der Investition) · Versicherung, Steuern, Verwaltung (% der Investition) · Sonstige Kosten (fester Jahresbetrag) |
| **Photovoltaik** | Wartung/Inspektion PV-Anlage (fester Jahresbetrag) · Instandhaltung PV-Module/Gestell (% der Investition) · Instandhaltung Wechselrichter/Speicher (% der Investition) · Reinigung der PV-Module (fester Jahresbetrag) · Zählermiete/Messstellenbetrieb (fester Jahresbetrag) · Telekommunikation/Monitoring (fester Jahresbetrag) · Personalkosten/Bedienung (% der Investition) · Versicherung, Steuern, Verwaltung (% der Investition) · Sonstige Kosten (fester Jahresbetrag) |

**Bewusste Abweichung von den Folien 20/21 (FK3, 25.08.2026):** Die dort gezeigten Zeilen
„Brennstoffkosten" und „Stromkosten (Verdichter)" sind keine Betriebskosten-Positionen und fehlen
deshalb in den Seeds — Energiekosten erscheinen ausschließlich in der Energiekosten-Welt (KL7).

Abgleich mit dem bestehenden 12-Positionen-VDI-Katalog (`VDI_POS_*`): Die Folien-Listen sind dessen
komponentenspezifische Ausprägung; die `VDI_POS_*`-Konstanten bleiben als Persistenzwerte bestehen,
neue Positionen erhalten eigene Konstanten. Die Empfehlungsbereiche der Instandhaltungs-Positionen
(BHKW 3,0–9,0 · Kessel 1,5–2,5 · Wärmezentrale 1,8–2,2 · Bauliche Anlagen 1,0–1,5 · Stromeinspeisung
1,8–2,2 · Personal 1,0–4,0 · Verwaltung 0,8–2,0 %) wandern in die Seed-Vorlagen und erscheinen als
Hinweistext neben dem Satzfeld (bestehendes Muster `Form_Betriebskosten`).

---

## 6. Reiter „Ertrag/Bonus"

### 6.1 BHKW (Folien 9–13)

Der Reiter parametriert die **vorhandenen** HF6-Größen — keine Zweitpflege, keine neuen Tabellen:

- **KWKG-Zuschlag:** Tatbestand (keiner / Anlage ≤ 100 kW / Kundenanlage / stromkostenintensiv),
  Anlagenart (neu / modernisiert / nachgerüstet), Kostenanteil, Pauschalmodus § 9 (≤ 2 kWel:
  Einmalerlös 4 ct × 60.000 Vbh × P_el), Vbh-Kontingent-Override — alles
  `Tab_ProjektWirtschaftlichkeit.KWKG_*`, gerechnet vom `KwkgSatzRechner`. Der Reiter zeigt die
  daraus folgenden Sätze **an** (Anzeigetabelle nach Folie 10: eingespeist 8/6/5/4,4/3,4 ct/kWh nach
  Leistungsanteil; selbst genutzt 4/3/…, Sonderregel neue Anlagen ≤ 50 kWel 16/8 ct/kWh) samt
  Dauer (30.000 Vbh, Jahresdeckel 2026: 3.300 → 2030: 2.500 Vbh/a, Folie 11).
- **Steuervergünstigungen:** Stromsteuerbefreiung § 9 Abs. 1 Nr. 3 StromStG (hocheffizient ≤ 2 MW,
  räumlicher Zusammenhang 4,5 km, ab 2026 CO₂-Kriterium < 270 g/kWh), Entlastung § 53a Abs. 5
  EnergieStG (Erdgas 4,42 €/MWh · Heizöl 40,35 €/1.000 l · Flüssiggas 19,60 €/1.000 kg), § 9b/§ 54
  mit Sockel 250 €/a — die Sätze stehen im Gesetzeskatalog (HF6, einheitenrichtig); der Reiter
  bietet die Schalter/Anzeigen und den Sprungknopf auf `Form_Gesetzesparameter`.
- Statische Hinweise (Formularnummern/Fristen) als `MyResource`-Texte, Muster HF6.

Die Detailtexte der Folien 10–13 (KWKG-/Steuer-Tabellen) sind mit dem umgesetzten HF6-Stand
abgeglichen und decken sich; sie dienen dem Reiter als Anzeige- und Hilfetexte
(`Grundlagen_KWKG_Energiesteuer_Stromsteuer.md` bleibt die Quelle).

FK7 entschieden (25.08.2026): Der **Strompreis-Teil** der Einspeisevergütung bleibt in der
Tarifstruktur (`Einsp_*` ist nach PV-Konzept F12 rein KWK); der Reiter zeigt die KWKG-/Steuer-
Parameter und verlinkt auf die Tarifstruktur — eine Wahrheit je Größe, kein Umbau.

### 6.2 Photovoltaik (Folie 17)

Der Reiter bettet den Inhalt des geplanten PV-Vergütungsdialogs ein
(`Konzept_Photovoltaik_Wirtschaftlichkeit_EPOS-Plan.md`, § B.1: sieben Gruppen Anlage · Vermarktung ·
Anzulegender Wert · § 51/§ 51a · Strompreis/Bezugsbewertung · 60-%-Begrenzung · Vorschau) bzw. ruft
ihn auf — **eine Vergütungswahrheit** (Befund V4, Frage F7 dort: der PV-Dialog wird führende Quelle).
Dieses Konzept übernimmt die Andockung: Der Knopf „Photovoltaik…" der Wirtschaftlichkeit und der
Ertrag/Bonus-Reiter der Komponente öffnen **dasselbe** Formular (`Form_PhotovoltaikVerguetung`,
stammprojektbezogen). Datenmodell, Rechenmodell, Etappen P1–P6: unverändert PV-Konzept.

### 6.3 Übrige Komponenten

Wärmepumpe, Solarthermie, Heizkessel, Puffer-/Stromspeicher haben keine laufenden Erträge im Sinn des
Reiters; Förderungen/Zuschüsse laufen als Positionsart `zuschuss` in den Investitionskosten (L7/E7 —
keine BAFA-Staffeln). Der Reiter ist bei diesen Komponenten ausgeblendet (FK5 entschieden
25.08.2026). Stromspeicher-Erlöse (Peak-Shaving u. a.) bleiben beim Stromspeicher-Konzept.

### 6.4 Rückbau `Form_Kosten` (Folie 27, Ä1)

Der Kosteneditor führt künftig nur noch **Investitionskosten** und **Betriebskosten** (zwei Reiter).
Der Reiter „Energie" und der K4-Reiter „Kostenprofil" entfallen dort; ihre Inhalte wandern in die
Energieträgerverwaltung (§ 7). Die Fußzeile „PROJEKT GESAMT (Energiekosten)" speist sich weiterhin aus
`KostenEmissionRechner` und erscheint in der Übersicht Berichte & Kosten (§ 10), nicht mehr im
Kosteneditor.

---

## 7. Energieträgerverwaltung (Folien 25, 26)

### 7.1 Struktur

Eigenes Untermenü (§ 3.1), eigener Dialog (Nachfolger des Energie-Reiters; Träger `ucFuelSettings`
bleibt Kern). Links Trägerliste (alle VDI-3805-Energieträger aus `energy_carrier`, inkl. Varianten),
rechts der Trägerbereich:

- **Kopf:** Trägername + VDI-3805-Referenz, Gruppenzeile (`group_code`).
- **Preise:** Preisbasis/Basiseinheit (bestehend), Arbeitspreis, Grundpreis, **neu Leistungspreis** —
  Katalogspalte `energy_carrier.price_power` (KD1-Befund: existiert bereits, bisher ohne Leser;
  projektseitig existiert `custom_price_power` ebenfalls);
  angeboten bei Strom, Erdgas, Biogas (`pricing_model` GRID/GAS), bei übrigen Trägern ausgeblendet.
  *Umsetzungsvermerk Ä18 (26.08.2026):* KD4 hatte den STROM abweichend gesperrt („Leistungspreis
  Strom: über die Tarifstruktur“); mit Ä18 gilt der Absatz wie konzipiert — Migrationsschritt 44
  setzt `has_powerprice` für ELECTRICITY, der Stromträger pflegt den Flat-Satz hier, und die
  Tarifstruktur bleibt das Detailmodell (komponentenbezogene Sichten, Ä18-Zeile der Ä-Tabelle).
  **FK6 entschieden (25.08.2026): mit Rechenwirkung**, nicht nur Erfassung:
  - **Modus je Träger** (neue Spalte `price_power_modus`): **Jahresleistungspreis** [€/(kW·a)] ×
    Jahreshöchstlast des Trägerbezugs, oder **Monatsleistungspreis** [€/(kW·Monat)] ×
    Monatshöchstlast, über zwölf Monate summiert. Höchstlasten kommen aus dem Simulationslauf
    (Strom: Lastgang; Erdgas/Biogas: höchste Bezugsleistung der Erzeuger im Lauf). Rechenweg in
    `KostenEmissionRechner` (Etappe KD4); Ausweis getrennt als Leistungsanteil der Energiekosten.
  - **Variable Leistungspreise (FK6a entschieden 25.08.2026: bereits in KD4):** zeitliche
    Änderung der Sätze über die vorhandene Stichtagsversionierung (`energy_price`,
    `valid_from/valid_to`); saisonal unterschiedliche Sätze (Winter-/Sommerfenster, atypische
    Netznutzung) als optionale **Leistungspreis-Reihe** je Träger nach dem Muster
    `Tab_Preisreihe` (Monats- bzw. Zeitfensterwerte), nicht als weitere Katalogspalten. Ist eine
    Reihe gepflegt, gilt sie vor dem konstanten Satz; im Monatsmodus gilt Monatssatz ×
    Monatshöchstlast.
  - Ohne Simulationslauf zeigt der Dialog den Satz, aber keinen Betrag (Muster Vorbelegungszeile).
- **Umrechnungsregeln:** bestehende HF2-Tabelle (Name editierbar, von/nach, Faktor, aktiv,
  z-Faktor-Benennung, kWh-Pflicht-Prüfer, Effektivzeile „1 ‹Einheit› = X kWh (Hi) / Y kWh (Hs)").
- **Emissionen (KL8):** CO₂ [g/kWh] Pflichtanzeige; SO₂/NOx/Staub nur falls in der Quelle enthalten,
  sonst „—" mit Knopf „Hinzufügen" (legt einen Wert der Pseudo-Quelle „Eigener Wert" an). Quellenwahl
  je Träger als **Ausnahme** von der Projekt-Leitquelle: Klappliste der `emissionsquelle`-Einträge
  (BAFA_EEW_2025, BEHG_V, UBA, GEG/GModG, Eigener Wert) mit Anzeige von **Quelle, Stand, Variante,
  Bezug (Hi/Hs)**; Stand/Datum editierbar nur bei „Eigener Wert". Herkunftszeile nach Muster
  „BAFA EEW 2025, Stand 01.06.2026, Bezug Hi". Nachweis- vs. Bilanzfaktoren bleiben getrennte
  Schlüsselgruppen (L11) — dieser Dialog pflegt ausschließlich die **Bilanzwelt**.
- **Kostenprofil (Ä1):** Die beiden K4-Karten („Kostenprofil", „Spotmarktpreise") erscheinen nur beim
  Träger Elektrische Energie — „Kostenprofil kein separater Tab, nur unter Strom, da nur für Strom
  relevant" (Folie 25). Strom-Aufschläge (`ucStromAufschlaege`) bleiben beim Stromträger.
- **Varianten:** „Speichern unter…" erzeugt eine Trägervariante (bestehendes Muster
  `Form_Kosten_Auswahl`: Brennstoffart + Variantenname); Liste zeigt Varianten unter dem Stammträger.

### 7.2 Projektkontext (Folie 26)

Aufruf aus dem Anlagendialog („Energiekosten…", Folie 6) oder aus Berichte & Kosten: derselbe Dialog
im Projektkontext — Werte laufen in `energy_project_settings` (Übersteuerung), Effektivwerte über
`Abfrage_Energietraeger_Effektiv` (unverändert). **Übernahme** beim ersten Öffnen, falls das Projekt
noch keine Einträge hat: Quelle wählbar — Stammkatalog (Default oder Trägervariante) oder anderes
Projekt derselben Gruppe (Stammprojekt/Variante); Mechanik § 8. Die Projekt-**Leitquelle** der
Emissionen (KL8) sitzt nicht hier, sondern auf Projektebene (Wirtschaftlichkeits-/Projektparameter,
Etappe E3 des Emissionsfaktoren-Konzepts).

### 7.3 Verlagerung der BEHG-/Emissionsblöcke (Folie 6, Ä2)

Aus **allen** Anlagen-Eigenschaften-Dialogen (BHKW, Heizkessel, …) entfallen:

- der Kasten „Emissionen nach BEHG-V" (Heizöl 0,0808 · Flüssiggas 0,0663 · Erdgas 0,056 t CO₂/GJ)
  samt „CO2 BEHG"-Knopf → ersetzt durch die Quelle `BEHG_V` im Emissionsmodell (g/kWh-Werte § KL8);
  die BEHG-Kostenwirkung rechnet weiterhin der CO₂-Preispfad des Gesetzeskatalogs;
- der Block „Emissionsfaktoren bezogen auf den Brennstoffverbrauch" (CO₂/SO₂/NOx/CO/Staub g/MWh,
  „mit SCR", „Eintragen") → trägerbezogene Werte liegen am Energieträger; anlagenspezifische
  Abweichungen (SCR-Katalysator) werden zur Ausnahme „Eigener Wert" am Projektträger.
  Altdaten-Migration: vorhandene anlagenbezogene Faktoren werden beim Umstieg einmalig als
  „Eigener Wert"-Ausnahme übernommen, nicht verworfen.

---

## 8. Übernahme-Mechanik Stamm → Projekt

Ein Auswahl-Dialog nach dem Muster `Form_BkUebernahme` (ein Dialog, mehrere Füllungen; Prüf- und
Schreiblogik in einem Controller `KostenVorlagenUebernahmeCtrl`, der Dialog rechnet nicht):

1. **Anlass:** Erstes Öffnen des Komponenten-Kostendialogs im Projekt ohne vorhandene Positionen
   (bzw. Knopf „Übernehmen…" jederzeit).
2. **Quellenauswahl** (oben, Vorgabe = Standardvorlage): Admin-Stammvorlage (Default) · Vorlagen-
   Variante (falls vorhanden) · gleiches Projekt: Stammprojekt · Projektvariante derselben Gruppe.
3. **Wertgegenüberstellung** darunter (Position, Bemessung, Satz, Betrag — vorhanden vs. Quelle),
   Klartext-Zusammenfassung bei leerem Ziel.
4. **Schreiben:** materialisiert Positionen in `Tab_ProjektWerte` (KL3) mit `VorlageID`-Herkunft;
   keine stille Kopplung, spätere Vorlagenänderungen wirken nie automatisch ins Projekt.
5. Gleiches Muster für Energieträger (§ 7.2): Quelle Stammkatalog-Variante oder anderes Projekt,
   Ziel `energy_project_settings`.

Die bestehenden Achsen bleiben unverändert: Planwert-Übernahme (Technik → Kostenposition),
`Form_BkUebernahme` (Merkmale/Komponenten Stamm ↔ Variante), `ProjektDuplizierenCtrl`
(schema-getrieben, `Tab_ProjektWerte.VorlageID` erbt das Duplizieren automatisch).

---

## 9. Anlagen-Eigenschaften-Dialoge (Folien 6, 7)

Jeder Komponenten-Eigenschaften-Dialog (BHKW Eigenschaften, Administration Heizkessel, Datenbank
Wärmepumpen, Administration Pufferspeicher, Administration Stromspeicher, …) erhält den Block
**„Kosten"** mit drei Aufrufen:

| Knopf | Ziel |
|---|---|
| „Investitionskosten…" | Komponenten-Kostendialog, Reiter Invest (Projektkontext; im Admin-Stammkontext der Gerätedatenbank: Stammvorlage) |
| „Betriebskosten…" | Komponenten-Kostendialog, Reiter Betrieb |
| „Energiekosten…" | Energieträgerdialog, vorgefiltert auf den Träger der Komponente (Folie 6 „Energiekosten Dialogaufruf hinzufügen", Beschreibung Folie 26) |

Im Gegenzug werden die eingebetteten Kostenblöcke der Anlagendialoge zurückgebaut
(„Eingabedaten zur Berechnung der Kosten": Investitionskosten €/kWel, Wartungskosten €/kWhel,
Nutzungsdauer, Einzelposten Modul/Montage/Lieferung/Schallschutz/Abgasreinigung …):

- Die Felder wandern als Positionen in die Kostenvorlage (z. B. BHKW: „BHKW-Modul (je kW
  elektrisch)" ← Investitionskosten €/kWel; „Vollwartung (je kWh elektrisch)" ← Wartungskosten).
- Vorhandene Gerätewerte (Modulkosten € u. ä.) bleiben Gerätedaten und speisen die Planwert-
  Übernahme (bestehende Kostenbasen-Mechanik mit Herkunftsspalte) — sie sind Datenquelle, nicht
  zweiter Pflegeort.
- Die Markierung „Nutzungsdauer [a]" (Folie 7) wird als VDI-2067-Nutzungsdauer je Position in der
  Vorlage geführt (§ 4.1) und beim Übernehmen in die drei Nutzungsdauer-Spalten der Projektposition
  vorbelegt.
- BEHG-/Emissionsblöcke: § 7.3.

Der Rückbau erfolgt je Dialog erst, wenn die Vorlagen-Übernahme für die Komponente produktiv ist
(keine Funktionslücke; Etappenplan § 14). FK8 entschieden (25.08.2026): Die alten Kostenblöcke
bleiben eine Version lang schreibgeschützt sichtbar — mit Hinweis „gepflegt wird im
Kostendialog" — und werden in der Folgeversion entfernt.

---

## 10. Berichte & Kosten (Folie 27)

- Übersichtsseite „Kosten": drei Karten **Investition · Betrieb · Energie** — die Betriebskosten-
  Karte kommt hinzu (heute fehlend): „Summe der Betriebskosten p. a. (Erwartungswert)" aus den
  Kategorie-2-Positionen; Energie unverändert aus der gespeicherten Wirtschaftlichkeitsrechnung.
- Tabelle „Investition je Komponente" unverändert; ergänzt um Tabelle „Betriebskosten je
  Komponente" (gleiches Muster). Energieträgertabelle unverändert; ergänzt um Spalte
  **Leistungspreis [€/kW]** (§ 7.1).
- „Kostenverwaltung öffnen…" führt auf den reduzierten Kosteneditor (§ 6.4); die Vorgehensweise
  (Übernahme-Auswahl beim ersten Öffnen) ist dieselbe wie in § 8.
- Der Bericht weist bei Emissionen Quelle, Stand und Variante aus (F7 des Emissionsfaktoren-
  Konzepts; hier nur referenziert).

---

## 11. Worst/Best und Investitions-Startzeitpunkt (Folie 34)

`Form_CaseEingabe` wird erweitert (kein neuer Dialog, KL9):

- **Eingabeart:** absolut [€] (bestehend) **oder relativ [%]** zum Erwartungswert; der eingegebene
  Wert stellt „bester Fall" bzw. „schlechtester Fall" dar, der gepflegte Positionswert ist der
  erwartete Fall. Umschalter %/absolut, Anzeige des resultierenden Betrags.
- **Startzeitpunkt der Investition** (neu, **je Position** — FK10 entschieden 25.08.2026): Default
  „aktuell" (t0). Bei gesetztem Jahr X wird die Investition erst im Jahr X getätigt; Betrieb und
  Energiekosten der Komponente laufen erst ab X. Umsetzung in der Kapitalwertreihe
  (`KapitalwertRechner`/ValERI): Verschiebung der Zahlungsreihen, Diskontierung unverändert über
  den Kalkulationszins; Nutzungsdauer/Ersatzbeschaffung zählen ab X. Neue Spalte
  `Tab_ProjektWerte.StartJahr` (LONG, nullable; NULL = t0). Komfort: Setzen am Gruppenkopf
  befüllt alle Positionen der Komponente — die Wahrheit liegt aber je Position.
- Verzinsung (Kapitalkosten) und Kostensteigerungen (Energie) bleiben in
  `Form_WirtschaftlichkeitParameter` — die Vorlage bestätigt den Bestand („im Parametereingabe").
- Sensitivität und Szenariolauf (3 Szenarien × Varianten) unverändert.

Abgrenzung unverändert gültig: keine Preissteigerung auf Investitionen (der Startzeitpunkt
verschiebt die Zahlung, er indexiert sie nicht), keine Finanzierungsrechnung.

---

## 12. Gestaltungsmuster (folienübergreifend)

**Designentscheidung Philipp (25.08.2026, KD2):** Alle Kostendialoge erscheinen in einem
**einheitlichen Design nach App-Bestand**, nicht in der Web-Optik der Mockups („ähnliches
Design, muss nicht wie Vorlage sein"). Konkret: dunkelblaue Kopfleiste RGB (15, 31, 61) mit
weißem Titel und hellblauem Untertitel (Muster `Form_Kosten.pnlHeader`), Sektions-/
Spaltenkopfleisten RGB (26, 50, 97) mit weißer Schrift, dunkelblauer Summenfuß, gelbes
schließbares Hinweisbanner, Standard-Buttons. Diese Optik gilt für **alle** KD-Dialoge
(auch Zeileneditor, Namensdialog und die kommenden KD3–KD6-Dialoge).

Die Mockups definieren darüber hinaus das Strukturmuster für alle Kostendialoge:

1. Kopfzeile: Titel + Untertitel + Info-Knopf (blau, rund — bestehendes `i`-Muster).
2. Reiterzeile unterhalb des Kopfs; aktiver Reiter farbig.
3. Gelbes, schließbares Hinweisbanner unter den Reitern (Netto-/USt-Hinweis).
4. Tabellenraster mit Aktionsspalte links (Stift/Papierkorb), Eingabespalten mit Einheitensuffix,
   Kopplungssymbol, ±-Knopf rechts.
5. Gestrichelte „Neue Zeile"-Leiste, zentrierter Primärknopf „+ …".
6. Fußbereich mit Netto-/Bruttosumme (fett), Katalog-Herkunftsvermerk in Klammern.
7. Vorbelegungs-/Herkunftszeilen als schmale Infoleisten über dem Raster.

Träger in WinForms: `SectionPanel`/`ucKategorieHeader` (vorgesehen, bisher ungenutzt) plus die
bestehenden Zeilen-Controls (`ucKostenZeile`/`ucKostenItem`) als Ausgangspunkt.

**FK1 entschieden (25.08.2026): Die neuen Kostendialoge werden möglichst für den WinForms-Designer
erstellt**, damit sie ohne KI-Unterstützung nachbearbeitbar sind (Ä6). Umsetzungsregeln:

1. Layout, Controls und Anordnung entstehen im Designer (`.Designer.cs` + `.resx`). Nur das
   dynamische Positionsraster (Zeilenanzahl je Vorlage) wird zur Laufzeit in ein im Designer
   platziertes Panel gefüllt — die Rasterzeile selbst ist ein Designer-fähiges UserControl mit
   fester Spaltenstruktur, damit auch sie im Designer nachbearbeitbar bleibt.
2. Anzeigetexte bleiben zentral in `MyResource` (de + en): der Designer trägt die deutschen Texte
   als Vorgabe ein, der Konstruktor überschreibt aus `MyResource` — Designer-Vorschau und
   zentrale Lokalisierung bleiben beide intakt.
3. Neue Designer-Dateien entstehen als UTF-8; die cp1252-Vorsicht gilt unverändert für
   Bestands-Designer-Dateien (die bleiben unangetastet).
4. Bestehende programmatische Formulare werden nicht rückgebaut; die Regel gilt für die neuen
   Dialoge dieses Konzepts.

---

## 13. Änderungen gegenüber beschlossenem Stand (ausdrücklich)

| Nr. | Änderung | Ersetzt/ergänzt |
|---|---|---|
| Ä1 | Kosteneditor verliert Reiter „Energie" und „Kostenprofil"; Kostenprofil-Karten nur noch beim Stromträger der Energieträgerverwaltung | teilweise Rücknahme HF4/K4 (der Karten-Ansatz bleibt, nur der Ort wandert); Folien 25/27 |
| Ä2 | BEHG-Kasten und Emissionsfaktoren-Block verlassen die Anlagendialoge; Emissionspflege ausschließlich am Energieträger (Quellenmodell) | Folie 6; konkretisiert E3 des Emissionsfaktoren-Konzepts |
| Ä3 | Bewertete Stammvorlagen mit Varianten (`Tab_KostenVorlage*`) ergänzen den flachen Positionskatalog | erweitert K5/HF5; `Tab_Kostenfaktor` bleibt Lexikon |
| Ä4 | Bruttosumme als Anzeige im Dialogfuß (USt aus Gesetzeskatalog) | ergänzt „netto rechnen" um „brutto anzeigen"; keine Rechenwirkung |
| Ä5 | Admin-Menü Kosten zweigeteilt (Kostenverwaltung / Energieträgerverwaltung); `Form_KostenAdmin` verlässt das Menü | Folie 3 |
| Ä6 | Neue Kostendialoge werden WinForms-Designer-fähig erstellt (Nachbearbeitung ohne KI); bewusste Abweichung von der Rev.-2-Hausregel „programmatische UI" für diese neuen Formulare | Entscheidung Philipp 25.08.2026 (FK1); Regeln in § 12 |
| Ä7 | Komponentenauswahl überall nur die SIEBEN Anlagen-Komponenten des Projektbaums (Wärmepumpe, Heizkessel, Photovoltaik, Solarthermie, Stromspeicher, Pufferspeicher, BHKW) — zentrale Wahrheit `KostenVorlagenCtrl.WaehlbareKomponenten`. Die Erfassungsgruppen Wärmezentrale/Bauliche Anlagen/Stromeinspeisung bleiben als Datensätze samt Vorlagenpositionen erhalten (Altpositionen rechnen und berichten weiter), werden aber nirgends mehr angeboten; im Projekt-Kosteneditor erscheinen sie nur noch mit vorhandenen Positionen. Menü Administration→Kosten nur noch „Kostenverwaltung …" + „Energieträgerverwaltung …" (Alteinträge „Kosten"/„Kosten Admin" entfernt; Positionskatalog = Knopf in der Kostenverwaltung, § 3.1) | Entscheidung Philipp 26.08.2026 (Screenshot-Abgleich Komponenten-Wizard); **umgesetzt 26.08.2026** |
| KD6a | Projektmodus der Kostenverwaltung (§ 5 dritter Kontext, aus KD3 verschoben): `SetProjekt` mit Tab_ProjektWerte-Raster über die Bestands-Schreibwege (`KostenProjektPositionenCtrl`), Einstiege § 3.2/§ 9 umgehängt, Energieträger-Direkteinstieg, Nutzungsdauer-Spaltenkopf, Kennzahl-Kacheln der Wirtschaftlichkeitsübersicht; Fehlerbild „erforderlicher Parameter“ an der Wurzel behoben (P6-Preisleser nutzte Testkopie-Spaltennamen `custom_price`/`price` statt `custom_price_work`/`price_work`; stille Spaltenprobe repariert — `ExecuteScalar` wirft nie) | Nutzerabnahme 26.08.2026 (Screenshots); kd6-Smoke 37/37 (B0–B9), Fehlerjagd auf Produktiv-Kopie 0 Befunde, Sweep 120/0 |
| Ä8 | Schreibschutz der Auslieferungsvorlagen AUFGEHOBEN — Investitions- UND Betriebskostenvorlagen sind direkt editierbar (zentral: `KostenVorlagenCtrl.IstNurLesen` = false, Flag bleibt Herkunftsmarker; rotes Banner entfällt). Restschutz: die STANDARD-Vorlage einer Komponente ist unlöschbar (Quelle von „Speichern unter…" und Übernahme — die Saat läuft nicht erneut), Varianten löschbar. Dazu Befundfix: Energieträgerverwaltung im KATALOGkontext war leer (`GetAllCarriers` las nur Projektzuordnungen) — Katalogzweig listet jetzt alle Katalogträger, erster Träger vorgewählt | Nutzerentscheid 26.08.2026 („Schreibschutz entfernen… auch für Betriebskosten“); kd2-Smoke an Ä8 angepasst (Schreiben erlaubt + spurenfrei, Standard unlöschbar), Fehlerjagd 0 Befunde |
| Ä9 | Energieträgerverwaltung wird ECHTE Katalogpflege (der offene KD4-Punkt „Trägervarianten + Katalog-Schreibweg“): Katalogleiste Neu…/Variante/Löschen (`EnergietraegerKatalogCtrl`, UI-frei; Löschen mit Verwendungsschutz und Klartextgrund), editierbarer Stammkopf (Bezeichnung + Gruppen-Klappliste, freie Gruppen erlaubt), „Speichern“ schreibt im Katalogkontext die KATALOGwerte (Preise, Hi/Hs, Emissionsfaktoren) in `energy_carrier` — die Variante ist damit der Weg zu abweichenden Emissionswerten je Träger. Saat: Schritt 42 Flüssiggas (Hi 12,87/Hs 14,0 kWh/kg, CO2 239 g/kWh, Brennstoff-Stammverweis), Schritt 43 VDI-3805-Feststoffe (Steinkohle, Braunkohlebrikett [Kohle], Scheitholz, Holzpellets, Holzhackschnitzel [Holz; biogen CO2 0]). Befundfixe: Migrations-Lookup fragte eine nicht existente Stammspalte an (drei Fehlerboxen beim App-Start, Migration wirkte gescheitert) → fester [Bezeichner]-Lookup, still im EngineModus; `DataRepository`-Fehlermeldungen nennen jetzt die ABFRAGE (Selbstverortung) | Nutzeraufträge 26.08.2026 („löschen/editieren/speichern, Gruppen wählbar“, „Flüssiggas fehlt → VDI-3805-Gruppen“); kd6-Smoke T1–T9+T1b (47/47), Migration 41→43 auf frischer Produktiv-Kopie ohne Boxen, Fehlerjagd (erweitert um Anlagendialoge/Ertragsreiter/Übernahme/Wirtschaftlichkeitsseite/Projektmodus) 0 Befunde |
| Ä10 | Katalog-ÜbernAHMEN in die Projektdialoge: Energieträgerverwaltung im Projektkontext mit „Aus Katalog übernehmen…“ (Mehrfachauswahl der noch freien Katalogträger; Zuordnungszeile mit NULL-customs — es GELTEN die Katalogwerte, damit ist der KD4-Punkt § 7.2 Träger-Übernahme ERLEDIGT) und „Entfernen“ (Anlagen des Projekts schützen ihren Träger, Klartextgrund); Kostenverwaltung im Projektmodus bietet ALLE sieben Anlagen-Komponenten an (auch leere — sonst ließe sich keine Vorlage für eine neue Komponente übernehmen, § 8). Dazu Kopfzeilen-Fix des Rasters („Betrag netto“ überdeckte „Nutzungsdauer“; Kopf jetzt spaltenbündig 688/108 + 800/72, zweizeilig) | Nutzerauftrag 26.08.2026; kd6-Smoke T10–T13 (51/51: zuordnen/lösen/Guard), Fehlerjagd 0 Befunde |
| Ä11 | Übernahme-Dialog (§ 8): Die Quelle „Aus Vorlage/Variante“ ist jetzt eine KLAPPLISTE aller Vorlagen und Varianten des Admin-Katalogs der Komponente/Kategorie (vorher fest die „aktuelle“ Variante — im Projektmodus gab es damit keine Wahl); beim Aufruf aus dem PROJEKTMODUS der Kostenverwaltung steht das Zielprojekt fest (vorbelegt und gesperrt statt Erstprojekt-Vorauswahl) | Nutzerauftrag 26.08.2026; kd6-Smoke B10/B11 (53/53), kd3-Regression grün, Sweep 120/0 |
| Ä12 | Projektmodus der Kostenverwaltung: Zeilen-EDITOR (Stift) aktiv (sichert über den Projekt-Schreibweg), expliziter **Speichern**-Knopf (schreibt alle Zeilen ins aktuelle Projekt, Bestätigung mit Uhrzeit) und **Abbrechen** OHNE Datenübernahme — dafür ist der Autosave im Projektmodus abgeschaltet: Feldänderungen leben bis „Speichern“ nur im Objekt (deferred); Anlegen/Löschen/Editor/± schreiben weiter sofort (eigene Bestätigungen). Gilt für Investitions- UND Betriebskosten; Stammkontext behält das Sofort-Speichern | Nutzerauftrag 26.08.2026; kd6-Smoke B12–B14 (56/56: Stift sichtbar, DB unverändert bis Speichern, Knöpfe nur im Projektmodus), kd2 grün, Sweep 120/0 |
| Ä13 | Feinschliff-Runde 26.08.2026 abends: (1) OK-Knopf im Projektmodus der Kostenverwaltung (= Speichern + Verlassen; Reihe OK/Speichern/Abbrechen). (2) Energieträgerliste GRUPPIERT nach group_code (übergeordnete Köpfe, nicht wählbar — Kopfklick springt zum nächsten Träger; Katalog- und Projektkontext). (3) Crash „No data exists for the row/column" im Energiekosten-Dialog behoben: `GetTargetUnitByConversionId` wertete `Next()` nie aus — verwaiste/leere Umrechnungs-ID (frisch übernommener Träger) las ohne Zeile. (4) Wizard-WP-Kachel: leeres `ReadAll`-Ergebnis (gelöschte WP) warf ArgumentOutOfRangeException — Guard | Nutzeraufträge/Debugger-Befunde 26.08.2026; kd6 56/56 (K6 auf gruppierte Liste), kd2/kd4 grün, Sweep 120/0, Fehlerjagd 0 Befunde |
| Ä14 | Energieträgerverwaltung: einheitliche Fußleiste **OK / Speichern / Abbrechen** (OK = speichern + verlassen; Abbrechen und Trägerwechsel übernehmen NICHTS mehr — der stille Autosave beim Schließen/Wechsel entfällt, dieselbe Ä12-Semantik wie die Kostenverwaltung; gilt für Katalog- und Projektkontext, der Katalogzweig schreibt in energy_carrier). Grundpreis-Einheit korrigiert: „€/a“ am Feld und in der Preishistorien-Spalte (andere Formulare geprüft: Tarifstruktur und Kosten-Seite führten €/a bereits; übrige Eingabedialoge haben OK/Abbrechen im Bestand) | Nutzerauftrag 26.08.2026; kd4/kd6 grün (21/21, 56/56), Fehlerjagd 0 Befunde, Sichtbeleg ae14_et_fussleiste.png |
| Ä15 | Simulationskonfiguration: VERBAUTE Anlagen werden beim Öffnen automatisch vorgewählt (`VerbauteAnlagenVorwaehlen`: Wärmeerzeuger über freie Slots, PV und Stromspeicher über ihre Klapplisten; Grundlage `TechnikPlanwertCtrl.Verbaut` je ERZEUGER_*; Bestands-Handler setzen Häkchen und Karten; Komfort-Guard — die Vorwahl darf das Öffnen nie verhindern). Anlass: definierte Wärmepumpe erschien nicht im Dialog | Nutzerauftrag 26.08.2026; kd6 B15; Commit 2f847b4 |
| Ä16 | Strompreis-BÜNDELUNG im Energieträgerdialog (Strom): Effektivpreis-Zeile „Bezugspreis inkl. Aufschläge“ (Arbeitspreis + wirksamer Aufschlag, live über `WirksamGeaendert`), Anwenden-Haken „Aufschläge in der Wirtschaftlichkeit berücksichtigen“ (schreibt `WirtschaftlichkeitParameter.AufschlaegeAnwenden` — der Parameterdialog ZEIGT den Haken nur noch), Tarifstruktur-Einstieg in den ET-Dialog verlegt; die Wirtschaftlichkeitsseite gibt ihren Tarif-Knopf ab (PV-Knopf rückt auf den Platz). Nachfix: `btnPhotovoltaik.Location` an der Erzeugungsstelle statt in `TexteSetzen` (NullReferenceException — der Knopf entsteht erst danach) | Nutzerauftrag 26.08.2026; Commits e6404cb + ed01c04 (Fix); Sichtbeleg ae16_strom_buendel.png; von Ä18 teilweise überholt (Tarif-Einstieg wandert weiter) |
| Ä17 | Designer-Umwandlung der Kostendialoge, erste Runde: `Form_Kostenprofil` in partial class + Designer.cs zerlegt (statisches Gerüst mit deutschen Vorgabetexten, Chart mit ISupportInitialize, benannte Handler statt Lambdas; Monats-/Stundenraster bleiben Ä6-konforme Laufzeit-Schleifenware, Logik unverändert); `ucStromAufschlaege` war bereits Designer-basiert. Restliste für weitere Runden: EinstiegsKarte, SectionPanel, UcBerichteKosten, UcBkKosten, UcBkUebersicht, Form_Tarifstruktur, Form_WirtschaftlichkeitParameter, Form_Kosten (Form_Betriebskosten = Alt-Pfad, nicht umwandeln) | Nutzerauftrag 26.08.2026; Sweep 115/0/5, kd4 21/21, kd6 57/57, Dump ae17_kostenprofil.png; Commit ed01c04 |
| Ä18 | Tarifstruktur KOMPONENTENBEZOGEN + Strom-Leistungspreis im ET-Dialog: (1) Migrationsschritt 44 setzt `pricing_model.has_powerprice` für ELECTRICITY — der Stromträger pflegt den FLAT-Leistungspreis (Jahres-/Monatssatz + Saisonreihen, FK6/FK6a) im ET-Dialog wie Gas; Sperre („über die Tarifstruktur“) und Ä16-Tarifknopf entfallen dort, die §-10-Trägerspalte zeigt den Strom-Satz (Kurzschluss „—“ entfernt) — damit gilt § 7.1 wie konzipiert. (2) `Form_Tarifstruktur` bekommt KOMPONENTENSICHTEN (`TarifSicht` Strombezug/BHKW/PV; es bleibt EIN Tarifsatz je Stamm — eine Sicht baut nur ihre Blöcke, Speichern übernimmt nur gebaute Felder, geteilte Felder sind beschriftet). (3) Einstiege in der Wirtschaftlichkeit: „BHKW-Tarif…“ (bei BHKW in der Gruppe; Rollenmodell samt Referenzbezug + Einspeisezonen), „Strombezug…“ (bei Wärmepumpe in der Gruppe ODER aktiver Tarifstruktur; Bezugszonen + Staffel + Bezugsrolle; `ErzeugerFlags.Waermepumpe` neu aus Tab_WP), PV-Sicht über Knopf „Einspeise-Tarif…“ im PV-Vergütungsdialog. GRENZE dokumentiert: Der Strom-Flat-Leistungspreis hat in der trägerbezogenen Energiekosten-Schiene noch keine Bezugsgröße (`AnschlussleistungKW` kennt nur BHKW/Kessel-Gerätedaten) — Rechenwirkung wäre eine eigene Etappe (Bezugsspitze aus dem Lauf vs. gepflegte Anschlussleistung) | Nutzerauftrag 26.08.2026; kd6 67/67 (U-Serie: Sichten-Feldpräsenz, Teilsicht-Speichern erhält fremde Felder, ET-Strom aktiv, Seiten-Knöpfe), kd4 21/21 (E3 umgestellt), pv6 28/28, Sweep 115/0/5, Fehlerjagd 0 Befunde (2 neue Stationen), Sichtbelege ae18_tarif_bhkw/pv/strom.png + ae18_et_strom.png |
| Ä19 | Anlagenbezug der Kosten-Seite + Kostenzugang in den Anlagendialogen: (1) §-10-Tabelle heißt „Anlagenkomponenten“ und zeigt EINE ZEILE JE ANLAGE (`ProjektEnergietraegerCtrl.AnlagenMitTraeger`; zwei Wärmepumpen = zwei Zeilen); GEPFLEGT wird weiter je Komponente (FK2) — die Summen stehen an der ersten Anlage der Komponente (Folgeanlagen „—“ mit Tooltip), „Gesamt“ bleibt die Projektsumme. Die Auswahl einer Anlage KENNZEICHNET rechts ihren Energieträger (Trägerzeilen tragen die Carrier-Id; Auflösung wie `Verwendete`: Anlagenverweis vor Geräte-Brennstoff, elektrische Gewerke über den Stromträger). Kostenpositionen OHNE verbaute Anlage (z. B. aus der Variantenkopie — der Befund „Wärmepumpe in der Heizkessel-Variante“) erscheinen als GELBE Warnzeile statt als normale Komponente und stehen in der Fußzeile — sie rechnen weiter mit (Kachel = Tabelle), still verschwinden wäre eine Falschausweisung. „Kostenverwaltung öffnen…“ öffnet die Komponente der GEWÄHLTEN Anlagenzeile. (2) Varianten-Anlegen (Übersicht/Variantentest) zieht die Varianten-Klappliste des Projektkopfs sofort nach (`VariantenAnzeigeAktualisieren` — bisher nur der Menüweg). (3) Anlagendialog Wärmepumpe (Detailansicht `Wizard_WPItem`): Modulkosten-Zeile ENTFERNT (Feld ausgehängt, Wert läuft im Speicherweg mit); an ihrer Stelle Komponentensummen „Invest … · Betrieb …“ + Knopf „Kosten bearbeiten…“ (Projektmodus, Komponente Wärmepumpe); „Bearbeiten…“ heißt „Parameter Bearbeiten…“; WP-Datenbank (`Form_WP`) ohne Modulkosten-Zeile. Lehre: Visible=false reicht im Offscreen-Weg dieser Alt-Dialoge nicht — Controls werden AUSGEHÄNGT. (4) Kostenverwaltung ADMIN-Kontext führt dieselbe Fußleiste OK/Speichern/Abbrechen — die Ä12-deferred-Semantik gilt jetzt in BEIDEN Kontexten (`ZeileBauen` setzt `NurExplizitSpeichern`; Anlegen/Löschen/Editor/± schreiben weiter sofort). OFFEN als Folgerunde: analoge Umstellung der übrigen Anlagendialoge (PV `Form_AdminPV`, Stromspeicher, Solar; BHKW/Kessel führen die §-9-Leiste seit KD6) und ECHTE Instanzkosten je Anlage (Datenmodell-Etappe — heutiger Schnitt ist bewusst Anzeige) | Nutzerauftrag 26.08.2026 (5 Screenshots); kd6 72/72 (V-Serie: V1 Anlagenliste-Ctrl, V2 Seite, V3 Träger-Tags, V4 Admin-Fuß, V5 Stamm-deferred), kd2/kd4/pv6 grün, Sweep 115/0/5, Fehlerjagd 0; Sichtbelege ae19_anlagenliste/wp_detail/wp_datenbank/admin_fuss.png |
| Ä20 | KOSTEN JE ANLAGE (Datenmodell-Etappe, hebt den Ä19-Anzeige-Schnitt auf): `Tab_ProjektWerte.ID_Anlage` (LONG nullable, Verweis auf `Tab_Energieanlagen.ID`; Vorsorge in `KostenPositionCtrl.StelleSpaltenSicher`). MIGRATIONSSCHRITT 45 (ZIEL_VERSION 45) ordnet den Bestand der jeweils ERSTEN verbauten Anlage seiner Komponente zu (MIN(ID) über die Verweisspalten-Landkarte); Positionen ohne verbaute Anlage (Erfassungsgruppen-Altdaten Ä7, Variantenreste) bleiben NULL = „ohne Anlagenzuordnung“. Der PROJEKTDUPLIZIERER schlüsselt ID_Anlage um (FK_MAP-Eintrag → Variantenkopien zeigen auf ihre eigenen Anlagen). Die RECHENKERNE bleiben unberührt (Aggregation je Projekt). UI: Die Klappliste des Kostenverwaltungs-Projektmodus wählt ANLAGEN („Komponente — Bezeichner“; Titel trägt die Anlage; Ä10 bleibt: leere Komponenten als „(keine Anlage im Projekt)“, dazu Pflege-Eintrag „(ohne Anlagenzuordnung)“); Lesen/Neu/Übernahme laufen je Anlage (`Lies/Neu(…, idAnlage)`, `AusVorlage(…, idAnlage)` mit anlagenbezogenem NurAnlegen-Dublettencheck — dieselbe Vorlage darf an einer zweiten Anlage erneut entstehen). Kosten-Seite: Summen JE ANLAGE (`LiesAnlagenSummen`, Gruppierung Komponente+ID_Anlage; gelbe Zeilen „ohne Anlagenzuordnung“ zählen mit — Kachel = Tabelle; rot nur, wenn die ganze Komponente nirgends Positionen hat), „Kostenverwaltung öffnen…“ und der Anlagendialog-Knopf öffnen DIE Anlage (Wizard_WPItem zeigt die Summen seiner Anlagenzeile item.ID). Gilt für alle Anlagen über die bestehenden Einstiege (KostenKnoepfe-§-9-Zweig wählt die erste Anlage der Komponente vor) | Nutzerauftrag 26.08.2026 („die Kostenverwaltung soll einer Anlage jeweils zugeordnet sein … für alle Anlagen“); kd6 79/79 (W-Serie: W1 Migrationszuordnung, W2 Neu nur an Anlage B sichtbar, W3 Übernahme an B trotz Positionen an A, W5 Variantenkopie zeigt auf Kopie-Anlagen, W6 Löschkaskade), kd2/kd4/pv6 grün, Sweep 115/0/5, Fehlerjagd 0 (Produktivkopie migriert 41→45); Sichtbelege ae20_kostenverwaltung_anlage.png (Titel mit Anlage) + ae20_anlagensummen.png |
| Ä21 | Anlagenkosten ROBUST + Übernahme VON einer Anlage: (1) GERÄTEANKER `Tab_ProjektWerte.ID_AnlageGeraet` (Migrationsschritt 46, ZIEL_VERSION 46; `AnlageZuordnen` pflegt beide Anker) und SELBSTHEILUNG `ZuordnungReparieren` — der Anlagen-Wizard baut Anlagenzeilen DESTRUKTIV neu (neue IDs, dokumentiert in `AnlagenEindeutigkeit`), verwaiste Zuordnungen kommen über Komponente+Gerät an die neue Zeile zurück (läuft vor jedem Aufbau von Kosten-Seite und Kostenverwaltung; ohne Gerät → ehrlich „ohne Anlagenzuordnung“). (2) EINZELNE Anlage entfernen (`Del_Projekt_ID_Waermeerzeuger`) LÖSCHT ihre Kostenpositionen mit — der Ä19-Befund „Wärmepumpe in der Heizkessel-Variante“ entsteht so nicht mehr neu; für den BESTAND löscht ein DOPPELKLICK auf die gelbe „ohne Anlagenzuordnung“-Zeile der Kosten-Seite die Positionen nach Rückfrage (`LoseLoeschen` über Einzellöschung). (3) Übernahme-Dialog: Quelle „Aus Projekt/Anlage“ mit ANLAGEN-Klappliste — eine weitere Wärmepumpe übernimmt die Positionen der BEREITS VORHANDENEN Anlage, auch im SELBEN Projekt (`AusProjekt(…, quellAnlage, zielAnlage)`, Dublettencheck und Vorschau je ZIEL-Anlage, Kopien mit Anker); gilt für alle Komponenten. NEUE ACE-LEKTION (gemessen): Parameter in Abfragen MIT Unterabfrage binden in falscher Reihenfolge — auch SELECT trifft still 0 Zeilen (Literal 1 / Parameter 0); alle Subquery-Stellen nutzen Int-Literale. Außerdem gemessen: `Tab_Energieanlagen` erzwingt UNIQUE (ID_Projekt, Gerätespalte) je Gewerk — die Duplikat-Meldung der App beim Zweitverbau desselben Geräts ist diese Bestandsmechanik (regulärer Weg: Gerätekopie nach Rückfrage, `AnlagenEindeutigkeit`) | Nutzerauftrag 27.08.2026 (Screenshots Variante „Wärmepumpe WG - HK“ + Übernahme-Dialog); kd6 85/85 (X-Serie: X1 Anker, X2 Anlage→Anlage feldgleich, X3 Selbstheilung, X4 LoseLoeschen, X5 Del-Hook, X6 Quellliste), kd2/kd4/pv6 grün, Sweep 115/0/5, Fehlerjagd 0 (Produktivkopie 41→46); Beleg ae21_uebernahme_anlage.png |

| Ä22 | WP-Verwaltung ZWEISTUFIG lesen + Kosten-Knopf-Zustand: (1) `Form_WPAuswahl.GeraetedatenFuellen` (Projektgerät `Tab_WP` VOR Stammkatalog `Tab_WP_STAMM`) ersetzt die einstufige Leselogik in Listenaufbau, „Ansicht" und „Übernehmen" — frisch angelegte Einträge tragen bis zum Verwaltungs-OK die STAMM-Id (die Materialisierung Stamm→`Tab_WP` passiert erst beim Wizard-Speichern), der Befund „Der Wärmepumpen-Datensatz (ID 67) wurde nicht gefunden" entfällt; Meldung präzisiert („weder bei den Projektgeräten noch im Stammkatalog"). Vorlauf b0ad94a: die STILLE Vorwahl der Detailansicht (`m_bStilleFuellung`) überschreibt die Projekt-Geräte-Id des Listeneintrags nicht mehr mit der Stamm-Id — nur eine echte Nutzerauswahl wechselt. (2) Detailansicht: „Kosten bearbeiten…" ist bei NEUANLAGE bewusst gesperrt (Kostenpositionen ankern an der GESPEICHERTEN Anlagenzeile `item.ID`, die es erst nach dem Verwaltungs-/Wizard-OK gibt) — jetzt mit erklärendem Tooltip und ehrlicher Summenzeile „Invest — · Betrieb —" statt irreführender Nullen | Nutzerauftrag 27.08.2026 (2 Screenshots: grauer Kosten-Knopf, Fehlerbox ID 67); kd6 87/87 (X7 kein Stamm-Override, X8 zweistufig inkl. Fantasie-Id), Sweep 115/0/5; Commits b0ad94a + a8904e0 |
| Ä23 | Leistungsspalte der WP-Verwaltung („Leistung [kW]" zeigte 0 trotz gepflegter 12 kW): Die Liste baute mit `Nennleistung` auf, AKTUALISIERTE die Zeile nach „Neu"/„Ändern" aber aus `maxPTherm` — das Feld ist am Listenobjekt nie gefüllt und schrieb eine 0 über den korrekten Wert. Vereinheitlicht auf die Nennleistung; der Neu-Fluss füllt das Listenobjekt vor dem Einreihen über `GeraetedatenFuellen` (zweistufig — ID_WP ist dort noch die Stamm-Id, Ä22); die ECHTE WP-Wahl in der Detailansicht übernimmt die Stammdaten (Nennleistung, Regelung, Modulkosten, Baujahr, Beschreibung, Firma, Typ, Heizung) jetzt auch ins Listenobjekt (die stille Ä21-Vorwahl bleibt unverändert außen vor); der Doppelklick-Leser der Verwaltung läuft ebenfalls zweistufig statt über die frühere „nicht gefunden"-Box | Nutzerauftrag 27.08.2026 (Screenshot Verwaltungsliste 0 kW neben 12-kW-Stammdaten); kd6 88/88 (X9: echte Wahl übernimmt die Stamm-Nennleistung ins Listenobjekt), Sweep 115/0/5 |

Alle übrigen Beschlüsse (L1–L9, E1–E8, insbesondere E2 „kein Nahwärmenetz", L7 Zuschuss, L8
Gesetzeswerte, Netto-Prinzip, ValERI) gelten unverändert.

---

## 14. Etappen

| Etappe | Inhalt | Abnahmekriterium |
|---|---|---|
| **KD1** | Datenmodell: Migrationsschritte 38/39 — `Tab_KostenVorlage`/`Tab_KostenVorlagePosition` + Seeds (§ 4.3, § 5.6/5.7), Spalten `Tab_ProjektWerte.VorlageID`/`StartJahr`, `energy_carrier.price_power_modus` (`price_power` existiert bereits — KD1-Befund; USt-Satz ebenso: `UMSATZSTEUER_REGELSATZ` seit E1); neue `DbWerte`-Konstanten. **Umgesetzt 25.08.2026**, Protokoll `Allgemein\Reporting\KD1_Kostenvorlagen_Protokoll.md` | ERFÜLLT: Migration idempotent (Marker-Reset-Lauf 0 Änderungen), 20 Vorlagen/117 Positionen gesät, keine Leser der neuen Spalten (Ergebnisneutralität) |
| **KD2** | Komponenten-Kostendialog (§ 5) im Admin-Kontext: Variantenpflege, Raster (inkl. Nutzungsdauer-Spalte, FK4), Bemessungskatalog, ⛓-Kopplung, Summenfuß; Designer-fähige Umsetzung (Ä6). **Umgesetzt 25.08.2026** (`Form_KostenKomponente`, `ucVorlagenZeile`, `Form_VorlagenPosition`, `Form_VariantenName`, `KostenVorlagenCtrl`/`BemessungKatalog`; Protokoll `KD2_Kostendialog_Protokoll.md`) | Smoke 23/23 PASS (ReadOnly-Schutz, Speichern unter, USt aus Katalog); **offen: UI-Sichtabnahme Philipp** gegen Mockups Folien 8/19 + Designer-Öffnungsprobe in VS |
| **KD3** | Übernahme-Mechanik (§ 8): `KostenVorlagenUebernahmeCtrl` + `Form_VorlagenUebernahme` (Quellenauswahl Vorlage/Variante/anderes Projekt, Klartext-Vorschau, Nur-Anlegen, `VorlageID`-Herkunft); Schreibwege = Bestand (`SetzeBetrag`/`SetzeBetragMitZusatz`). **Umgesetzt 25.08.2026**, Protokoll `KD3_Uebernahme_Protokoll.md`; Befund behoben: `BetriebskostenCtrl.Betrag` kennt jetzt die KD-Bemessungen (JAHRESBETRAG absolut; neue %-/€-je-Einheit-Arten) | Smoke 20/20 PASS (Feldsemantik, Rechenweg-Äquivalenz per Reflection, Idempotenz, Projekt→Projekt feldgleich); offen: UI-Sichtabnahme; Projektmodus des Dialogs folgt mit KD6 |
| **KD4** | Energieträgerverwaltung (§ 7): Menüpunkt, Leistungspreis mit Rechenwirkung (FK6: Jahres-/Monatsmodus) inkl. saisonaler Leistungspreis-Reihen (FK6a), Emissionsquellen-Ausnahme (setzt Etappe E1/E2 des Emissionsfaktoren-Konzepts voraus), Kostenprofil-Verlagerung, Rückbau `Form_Kosten` auf zwei Reiter (Ä1). **Umgesetzt 25.08.2026** (Migrationsschritt 40; `Form_Energietraeger`, `Form_LeistungspreisReihe`, `ucFuelSettings`-Zusätze, Menüpunkt, Ä1 mit Übergangsknopf „Energieträger…" bis KD6; Protokoll `KD4_Energietraeger_Protokoll.md`). Umsetzungsnotiz: Basis der Rechenwirkung ist die VORGEHALTENE Anschlussleistung aus den Gerätedaten (BHKW (Pel+Ptherm)/η, Kessel Ptherm/η), nicht die Simulations-Höchstlast — der Leistungspreis bepreist den Anschluss, und Ergebnis-Zeitreihen werden nicht persistiert. **Bewusst offen:** Emissionsquellen-Ausnahme (KL8/§ 7.3, wartet auf E1/E2), Trägervarianten + Katalogpreis-Schreibweg (Katalogkontext liest Preise nur), Träger-Übernahme § 7.2/§ 8.5 | Smoke 21/21 PASS: Anschlussleistung gegen Handrechnung (Projekt 1030/Erdgas 63: 2967,27 kW), Neutralität bei NULL/0, Projekt-vor-Katalog inkl. 0-Regel, Jahres-/Monats-/Reihenmodus gegen Handrechnung (Reihe vor Satz, Projekt- vor Stammreihe), Spot-Auswahl sieht keine Träger-/Monatsreihen; Kostenprofil/Spot nur unter Strom (Karten im Trägerdialog); Regression kd2 23/23, kd3 20/20, Layout-Sweep 119 Formulare 0 Befunde |
| **KD5** | Ertrag/Bonus (§ 6): BHKW-Reiter auf HF6-Größen; PV-Einbettung (setzt PV-Etappen P2–P5 voraus, gemeinsame Abnahme). **Umgesetzt 26.08.2026** (`ucErtragBonus`, Designer-fähig; Protokoll `KD5_ErtragBonus_Protokoll.md`): BHKW-Anzeige aus GENAU den Katalogschlüsseln des `KwkgSatzRechner` (Tranchen, Sonderregel 16/8, Eigen-Staffel, Dauer + Jahresdeckel-Reihe, Steuerblock, FK7-Vermerk, Sprung auf `Form_Gesetzesparameter`); PV öffnet DASSELBE `Form_PhotovoltaikVerguetung` (Stammprojekt-Klappliste im Admin-Kontext); FK5 entfernt die Reiterseite bei allen übrigen Komponenten | ERFÜLLT: Smoke kd5 14/14 — Rechner↔Anzeige-Konsistenz (50→8,00 · 100→7,00 · 300→5,5667 · Neuanlage 16), eine Vergütungswahrheit per Formular-Identität, FK5-Sichtbarkeit; Sweep 0 Befunde; offen: Sichtprüfung Philipp |
| **KD6** | Anlagendialog-Integration (§ 9: drei Knöpfe, Rückbau Kostenblöcke — FK8: zunächst schreibgeschützt, BEHG-Verlagerung Ä2 mit Altdaten-Übernahme), Berichte & Kosten (§ 10), `Form_CaseEingabe`-Erweiterung (§ 11, Startjahr je Position FK10). **Umgesetzt 26.08.2026** (Serienbaustein `KostenKnoepfe` — Leiste/Sperren/Träger-Vorfilter, je Dialog ein Einzeiler — angebunden an Kessel-Stamm, BHKW-Stamm, Kessel-Projekt, BHKW-Projekt; § 10: Betriebskosten als dritte Spalte der Komponentenübersicht [dokumentierte Abweichung: statt eigener Tabelle] + Leistungspreis-Spalte der Trägertabelle [Jahreswert, Projekt vor Katalog, 0 = nicht gepflegt, Strom „—“]; § 11: %-Eingabe mit Live-Umrechnung + Startjahr je Position mit Rechenwirkung im `KapitalwertRechner` [Erstzahlung/Ersatzkette/Restwert ab Startjahr, Betriebskosten ab Jahr X, Start > T nur Ausweis]; Protokoll `KD6_Protokoll.md`). Umsetzungsnotizen: ENERGIEkosten laufen unabhängig von Startjahren in der Gesamtrechnung weiter (Simulation kennt keine Teiljahre; Ergebnishinweis benennt das); Ä2 wartet auf E1/E2. **Bewusst offen:** § 9-Serie auf die übrigen Gerätedialoge (WP/Solar/Speicher — Einzeiler), zweiter FK8-Schritt (Felder entfernen) | Smoke 19/19 PASS: Kapitalwert-Handrechnung (Invest 10.000 Start 3, Betrieb 500 ab 3, i = 3 %, T = 20), Ersatzkette 3/8/13/18 + Restwert, Start > T, BITGLEICHHEIT bei Startjahr ≤ 1 (Bestandsdaten ergebnisneutral), Lesewege inkl. Alt-DB-Spaltenprobe, CaseEingabe-Roundtrip absolut/%/Startjahr, FK8 + Knöpfe in 4 Dialogen, Träger-Vorwahl; Regression kd2–kd5 grün, Layout-Sweep 120 Formulare 0 Befunde |

Reihenfolge KD1 → KD2 → KD3 sequenziell; KD4 unabhängig ab KD1; KD5/KD6 nach KD3. Erste gewollte
Ergebnisänderungen erst mit KD6 (Startjahr); KD1–KD5 sind ergebnisneutral gegenüber gleich gepflegten
Daten.

---

## 15. Abnahmestand der Fragen (FK)

### 15.1 Entschieden (Philipp, 25.08.2026)

| Nr. | Entscheidung | Eingearbeitet |
|---|---|---|
| FK1 | Dialoge möglichst für den WinForms-Designer erstellen, damit ohne KI nachbearbeitbar | § 12 (vier Umsetzungsregeln), Ä6 (§ 13), KD2 |
| FK2 | **Änderung:** Der Knopf heißt „+ Position hinzufügen" (nicht „Komponente hinzufügen") und legt nur einen weiteren Eintrag zur bereits geöffneten Komponente an — kein Instanzkonzept | § 5.2, § 5.5 |
| FK3 | **Verschärfung:** „Brennstoffkosten" (Kessel) und „Stromkosten Verdichter" (WP) nur in den Energiekosten — Energiekosten erscheinen grundsätzlich nicht im Betriebskosten-Raster, auch nicht nachrichtlich; %-Bemessungen holen ihre Basis direkt aus der Energieträgerwelt | KL7, § 5.3, § 5.7 |
| FK4 | Nutzungsdauer als sichtbare Spalte im Investitionsraster | § 5.2 |
| FK5 | Ertrag/Bonus-Reiter beim Stromspeicher (und den übrigen Komponenten ohne Vergütung) ausgeblendet — keine Verweisseite | § 6.3 |
| FK6 | Leistungspreis mit Rechenwirkung; Jahres- **und** Monatsleistungspreis; variable Sätze | § 7.1, KD4 |
| FK6a | Saisonal variable Leistungspreise **bereits in KD4** (keine spätere Ausbaustufe); Ablage als Leistungspreis-Reihe nach Muster `Tab_Preisreihe` | § 7.1, KD4 |
| FK7 | Strompreis-Teil der BHKW-Einspeisung bleibt in der Tarifstruktur (`Einsp_*` = KWK), der Reiter zeigt KWKG/Steuern und verlinkt | § 6.1 |
| FK8 | Empfehlung bestätigt: Anlagendialog-Kostenblöcke eine Version schreibgeschützt („gepflegt wird im Kostendialog"), dann entfernen | § 9, KD6 |
| FK9 | Empfehlung bestätigt: einheitliches Namensschema „‹Name› — Variante ‹n›" für Kosten-Vorlagen- und Trägervarianten | § 5.1, § 7.1 |
| FK10 | Startjahr **je Position** (Kosten u. Ä. werden ohnehin je Position geführt); Gruppenkopf nur als Komfort-Befüllung | § 11, KD6 |

### 15.2 Noch offen

Keine — alle Fragen sind entschieden (zuletzt FK6a am 25.08.2026: saisonale Leistungspreise
bereits in KD4). Das Konzept ist umsetzungsreif; KD1 startet auf Zuruf.

---

## Anhang A — Folien-Kurzverzeichnis der Vorlage

| Folie | Inhalt |
|---|---|
| 1 | Kernforderung Komponenten-Verknüpfung; Komponentenkarten |
| 2 | Kostenkategorien als Stammvorlage (Muster „Administration Kostenfaktoren"); Kosteneditor-Reiter |
| 3 | Admin-Menü zweigeteilt; Vorlagen/Varianten je Komponente |
| 4/5 | Bestand Investitions-/Betriebskosten (Kosteneditor) |
| 6/7 | Anlagendialoge: Kosten-/Energiekosten-Aufrufe, BEHG-Verlagerung, Nutzungsdauer-Markierung |
| 8/9 | Neuer Invest-Dialog Heizkessel/BHKW (Mockup) |
| 10–13 | BHKW Ertrag/Bonus: KWKG §§ 7–9, StromStG, EnergieStG (Anzeige-/Hilfetexte) |
| 14–16 | Neuer Invest-Dialog Wärmepumpe/Solarthermie/Photovoltaik (Mockup) |
| 17 | PV Ertrag/Bonus → PV-Wirtschaftlichkeitskonzept |
| 18/19 | Bestand Betriebskosten; neuer Betriebs-Dialog BHKW (Mockup, Zeilenaktionen, ⛓-Tooltip) |
| 20–24 | Neuer Betriebs-Dialog Heizkessel/WP/Solarthermie/Puffer/PV (Mockup) |
| 25/26 | Energieträgerverwaltung Admin/Projekt (Leistungspreis, Emissionen, Kostenprofil nur Strom) |
| 27 | Berichte & Kosten (Betriebskosten-Karte, Kosteneditor reduziert) |
| 28–33 | Emissions-/PE-Faktoren GModG (bereits umgesetzt: Methodenwechsel-Protokoll, Schritt 23) + Quellen |
| 34 | Worst/Best-Dialog, %-Eingabe, Investitions-Startzeitpunkt, VALERI-Verweis |

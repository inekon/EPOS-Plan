# Protokoll #539 — Zapfprofilgenerator: zweiter Validierungslauf an offenen Messreihen (K5)

**Datum:** 26.09.2026 · **Zweig:** `zval` (Worktree `.claude/worktrees/zval`) von `45c35a946` ·
**Agent:** Opus 5.5 · **Nachtrag:** N27 im
[Umsetzungskonzept Zapfprofilgenerator](../../../aktuell/Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md) ·
**Bericht:** Abschnitt 7 in
[`2026-09-26_Validierung_offene_Messreihen.md`](../../../aktuell/Zapfprofilgenerator/2026-09-26_Validierung_offene_Messreihen.md)

Kein Schemaschritt, Testdatenbank unberührt, kein Parameter geändert. Die Messreihen liegen außerhalb
des Repositoriums (`Messreihen_extern/` neben dem Klon); hierher kommen nur Verhältniszahlen.

## 1. Auftrag

Anwenderauftrag „führe aus: Validierung an echten Daten": die Folgen V1–V5 des ersten Laufs
abarbeiten (Bezugsmengen, Kalender je Land, Bandkriterium nach Objektgröße als Analyse, mehrere
Zonen, Formschwelle) und einen zweiten Lauf fahren; Ergebnisse als Abschnitt „Zweiter Lauf" im
Validierungsbericht.

## 2. Vorprüfung

| Nr. | Prüfung | Ergebnis |
|---|---|---|
| P1 | Nimmt der Kern die Feiertage aus `objekt.json` an? | nur auf der Seite der Rechnung (`WeBilden`); die Messung ordnete jeden Tag nach dem Wochentag ein (`Messvergleich.Tagtyp`). Eine Liste in `objekt.json` hätte den Formabgleich nicht erreicht → Kernergänzung |
| P2 | Kennt der Kern Feiertagsregionen? | nein — keine Feiertagstabelle; Wochenende und Feiertage kommen als Kennzeichen der Klimaregion. Die Konverter berechnen die Feiertage |
| P3 | Bezugsmengen Norwegen | Tabelle 1 der Beschreibung (PMC8220317): alle zwölf Gebäude mit Wohnungen bzw. Zimmern; Text nennt die Schlafzimmerzahl der Wohnungen, eine Küche mit Restaurant für HO4, zentrale Küchen der Pflegeheime |
| P4 | Wohnungszahl New York | OSTI und NREL sind von diesem Rechner nicht erreichbar; die Building America Case Study DOE/GO-102016-4704 (über `www1.eere.energy.gov`) nennt „approximately 50 apartments" je Haus |
| P5 | Bewohnerzahl Spanien | weder `devs.csv` noch die Zenodo-Beschreibung („10 Spanish homes … two different buildings", „Spain (GMT+1)") |
| P6 | Zeitbezug Spanien | Quelle UTC; die Bewohner leben nach Ortszeit → Umrechnung MEZ/MESZ |
| P7 | Jahresspitzen der spanischen Haushalte | bei mehreren Haushalten ein Artefakt: Nachholwert nach einer Übertragungslücke (bis zwei Wochen) oder eine Ablesung mit dem Zehnfachen des nächstgrößten Durchflusses |
| P8 | Spitzenstreuung im Werkzeugbericht | bezieht die Ensemblespitzen der unkalibrierten Rechnung auf die kalibrierte Spitze — um den Kalibrierfaktor verschoben (V7, offen) |

## 3. Umsetzung

* **Kern** `EPOS.Kern/Allgemein/Zapfprofil/Messvergleich.cs`: `Messvergleichseingang.MessFeiertage`
  und `Tagtyp(DateTime, feiertage)`; ohne Angabe unverändert. Test
  `MessvergleichTests.Genannte_Feiertage_zaehlen_im_Formabgleich_als_Sonntag` mit Gegenprobe,
  Samstagsregel und Schaltjahr.
* **Werkzeug** `Werkzeuge/ZapfprofilValidierung`: `bezugsmenge_herkunft` (Aufzählung
  `Bezugsmengenherkunft`, benannte Ablehnung eines fremden Worts), Herkunft in Bericht und
  Sammelbericht (CSV-Spalten am Ende); √N-Skalierung nur über belastbare Mengen, die Steigung über
  alle steht ohne Ampel daneben; Feiertage als `MessFeiertage`; Analyse `Bandanalyse` (Perzentil der
  Messspitze, Lage gegen die Ensemblespitzen, je Größenklasse), ohne Ampel. Drei Proben in
  `ZweiterLaufTests`.
* **Konverter**: Stammdaten mit Zitat je Quelle, Herkunft, Feiertage NO/ES/US (Osterformel, n-ter
  Wochentag, US-Ersatztag), hihAigua in Ortszeit, zeitanteilige Verteilung, Nachholwerte und
  unplausible Intervalle verworfen und gezählt.
* **LIESMICH** des Werkzeugs und der Konverter nachgezogen.

## 4. Lauf

Konverter über die drei Quellen: 9 norwegische Objekte (drei weiter benannt übergangen), 10 spanische
(zwei bis sieben Nachholwerte, null bis ein unplausibles Intervall je Haushalt), 2 New Yorker.
Werkzeug mit den Vorgaben der `objekt.json` (zehn Realisierungen, feste Saat), Katalog
`Referenzlaeufe/Kenndaten_Test.sqlite` (Stand origin mit #537). Berichtswache ohne Fund. Gegenrechnung
ohne Feiertage: Formmaß je Objekt höchstens 0,005 anders, keine Ampel gewechselt.

| Kriterium | erster Lauf grün/gelb/rot | zweiter Lauf grün/gelb/rot |
|---|---|---|
| Band der Dauerlinie | 0 / 0 / 21 | 0 / 0 / 21 |
| Formabgleich | 3 / 0 / 18 | 3 / 0 / 18 |
| Energie nach Kalibrierung | 21 / 0 / 0 | 21 / 0 / 0 |
| √N-Skalierung | grün (−0,29, 21 Objekte) | rot (+0,54, 11 belastbare Objekte); alle 21: −0,06 |
| Objekte | 0 / 0 / 21 | 0 / 0 / 21 |

Band je Größenklasse: Perzentil der Messspitze N < 10: 0,998 … 1; 10 ≤ N < 100: 0,962 … 0,998;
N ≥ 100: 0,972 … 1 (die drei Hotels über der Rechenspitze).

## 5. Gate

`dotnet build WP-Plan.Kern.slnf -c Release` 0 Fehler; Werkzeug-Projektmappe gebaut, Werkzeugtests 33/33 grün; gefilterte Kern-Tests (Zapfprofil, Messvergleich, Validierung, DokumentationLinkWache, WikiProduktdatenWache, RepositoryOrdnungWache) 322 + 168 grün; voller Lauf des Kern-Filters 15 603 grün, 0 Fehler, 2 übersprungen (Bestand); Berichtswache des Werkzeugs ohne Fund; nach dem Merge von origin die gefilterten Tests wiederholt.

## 6. Offen

ZU35 (Anwenderentscheid, mit K5), V6 Katalogtyp „Hotel", V7 Spitzenstreuung im Werkzeugbericht, K5.

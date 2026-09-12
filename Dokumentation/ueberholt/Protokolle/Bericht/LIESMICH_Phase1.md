# Berichtsmodul — Stand Phase 1 – 11 (12.08.2026)

Umsetzung nach `Allgemein\Reporting\Konzept_Berichtserstellung_EPOS-Plan.md`, Kap. 12.

## Phase 11 (neu): Referenzkessel aus DB + Kapitalwert-Verlaufsdiagramm

Zwei Änderungen (Vorgabe 12.08.2026). Neue Datei:
`Views/Wirtschaftlichkeit/Form_WirtschaftlichkeitVerlauf.cs`. Geändert:
`WirtschaftlichkeitDaten/Ctrl.cs`, `Form_WirtschaftlichkeitParameter.cs`,
`Form_Wirtschaftlichkeit.cs`, `ChartRenderer.cs`,
`BausteineWirtschaftlichkeit.cs`, `ExcelBerichtGenerator.cs`, `BerichtTexte.cs` —
zusätzlich zwei **Literal-Korrekturen aus Altphasen** (Compile-Fehler durch
typografisches Anführungszeichen): `EmissionsBilanzRechner.cs` (Phase 8) und
`Bausteine/BausteineVergleich.cs` (Phase 3/5) — beide Dateien mit ausliefern!

| Baustein | Umsetzung |
|---|---|
| **Referenzkessel aus DB** | Wirkungsgrad und Brennstoff des Referenzkessels (getrennte Erzeugung der Emissionsbilanz) kommen jetzt aus dem **größten Heizkessel des Stammprojekts** (`Tab_Heizkessel`, `WirtschaftlichkeitCtrl.LiesReferenzkessel`): Öl-Kategorie → `Wirkungsgrad_Öl`, sonst `Wirkungsgrad_Gas` (0 → der jeweils andere; Faktor-Schreibweise 0,9 → Prozent; Plausibilitätsband 50–115 %, sonst Fallback auf die gespeicherte Vorgabe). Die beiden Felder sind aus dem Parameterdialog **entfernt**; dort steht eine Info-Zeile mit dem erkannten Kessel. `LadeParameter` übernimmt die DB-Werte automatisch (Träger nur bei gesetztem Brennstoff-FK); Word-Emissionsbilanz weist die Quelle aus |
| **Kapitalwert-Verlauf** | Neuer Dialog **„Verlauf…"** im Wirtschaftlichkeits-Reiter: kumulierte diskontierte Zahlungsströme je Jahr als Liniendiagramm — oben **Differenz zur Stamm-Referenz** (Nulldurchgang = dynamische Amortisation), unten **absolute kumulierte Barwerte** je Projekt. **Zeitraum frei wählbar (2–60 a, auch > T)** — dann wird mit verändertem Horizont neu gerechnet (Ersatzbeschaffungen, KWKG-Staffel und Restwert folgen dem Horizont); gespeicherte Parameter/Ergebnisse bleiben unverändert. Szenario wählbar. Reihen OHNE Restwert (Nettobarwert = Endwert + Restwert-Barwert; Restwerte werden unter dem Diagramm ausgewiesen). Simulationsdaten werden je Dialog EINMAL gesammelt (Zeitraum-/Szenariowechsel rechnen nur den Kapitalwert neu) |
| **Bericht** | Word: Abschnitt „Kapitalwert-Verlauf über den Betrachtungszeitraum" mit beiden Diagrammen (Szenario Erwartet, T aus den Parametern). Excel: Jahresreihen-Datenblock (Jahr 0…T, kum. Barwert je Projekt + Δ je Variante). **Konsistenz-Gate:** sind Tarif/KWKG aktiv, aber der Bericht wurde ohne Stundenreihen gesammelt (Baustein „Ergebnisse je Variante" abgewählt), entfällt das Diagramm mit offener Begründung — keine stillen Widersprüche zur Tabelle |
| **Rechenkern** | `WirtschaftlichkeitCtrl.BerechneVerlauf(daten, p, jahre, szenario)` — nutzt BaueEingabe/RechneBild unverändert (Parameter-Kopie mit Horizont); `ChartRenderer.KapitalwertVerlauf` (vorzeichenfähige Y-Skala, Nulllinie, Legendenumbruch). Numerisch verifiziert: Nulldurchgang der Differenzlinie = `AmortisationDifferenz` (Python-Nachbau) |

**Wichtig beim Simulieren aus dem Verlaufsdialog:** sind Tarif/KWKG aktiv, sammelt
der Dialog Stundenreihen und simuliert dabei neu — die gespeicherten
Wirtschaftlichkeits-Ergebnisse passen dann nicht mehr zum Simulationsstand.
Der Reiter erkennt das nach dem Schließen und fordert zum Neuberechnen auf.

**Testschritte Phase 11:** (1) Parameterdialog öffnen → Brennstoff-Gruppe zeigt
„Referenzkessel (aus Projekt): …" mit η und Träger des größten Stamm-Kessels;
Kessel-Wirkungsgrad in der Kesselmaske ändern → Dialog neu öffnen → Wert folgt.
(2) Reiter → „Verlauf…" → beide Diagramme; Zeitraum 30 a wählen → Neuzeichnung,
Statuszeile weist „abweichend von T" aus; Amortisation = Schnittpunkt der
Differenzlinie mit der Nulllinie (gegen Tabellenwert prüfen). (3) Word/Excel-
Bericht: Verlaufs-Kapitel bzw. Jahresblock vorhanden; Bericht ohne Baustein
„Ergebnisse je Variante" bei aktivem Tarif → Hinweis statt Diagramm.

## Phase 10: kategorisierter Wirtschaftlichkeits-Parameterdialog

Die Parameter des Dialogs „Wirtschaftlichkeits-Parameter" sind jetzt nach
Zugehörigkeit **kategorisiert** (Vorgabe 12.08.2026). Betroffen:
`WirtschaftlichkeitCtrl.cs` (neuer Abschnitt „Erzeuger der Gruppe"),
`Form_WirtschaftlichkeitParameter.cs` (komplett neu aufgebaut) — Rechenkern,
Persistenz und Berichtsstruktur unverändert.

| Baustein | Umsetzung |
|---|---|
| **Gruppen** | **„Allgemein"** (immer sichtbar): Kalkulationszinssatz, Betrachtungszeitraum, Preissteigerung Energie/Betrieb. **„Photovoltaik"**: Einspeisevergütung. **„BHKW — KWKG 2025"**: Boni Eigen/Einspeisung, Vbh-Deckel-Override, Vbh-Kontingent, Negativ-Abschlag, Stichtag, Inbetriebnahme. **„Brennstoff — BEHG und Emissionsbilanz (BHKW/Kessel)"**: CO₂-Preis BEHG (verschoben aus „Allgemein" — wirkt nur auf Brennstoff-CO₂), Referenz-Kraftwerkspark, Referenzkessel-η und -Brennstoff *(seit Phase 11 aus der DB, nicht mehr im Dialog)* |
| **Sichtbarkeitsregel** | Eine Erzeuger-Gruppe erscheint nur, wenn der Erzeugertyp in der **Vergleichsgruppe** (Stamm + alle Varianten) tatsächlich vorkommt — `WirtschaftlichkeitCtrl.ErzeugerDerGruppe(idStamm)` prüft die Eingabetabellen `Tab_BHKW` / `Tab_PV` / `Tab_Heizkessel` je Projekt (eine COUNT-Abfrage je Tabelle). Bei DB-Störungen wird die Gruppe im Zweifel **eingeblendet** (fail-open), damit die Parameter editierbar bleiben |
| **Werte-Erhalt** | Beim Speichern werden nur die Werte **sichtbarer** Gruppen übernommen (Guard = dieselben Erzeuger-Flags wie beim Aufbau); ausgeblendete Parameter bleiben in `Tab_ProjektWirtschaftlichkeit` unverändert — kein stilles Nullen, wenn z. B. ein BHKW vorübergehend aus der Gruppe entfernt wird. Der Referenzkessel-Brennstoff wird nur überschrieben, wenn der gepflegte Wert im Katalog gefunden wurde oder der Nutzer aktiv umgestellt hat |
| **Layout** | Hinweistext wird gemessen statt fix bemaßt (keine abgeschnittene Fristangabe); der KWKG-Absatz erscheint nur bei sichtbarer BHKW-Gruppe. Dialoghöhe wird auf den Bildschirm-Arbeitsbereich gedeckelt — bei kleiner Auflösung greift AutoScroll (Scrollbalken-Breite eingerechnet) |

**Testschritte Phase 10:** Vergleichsgruppe ohne PV → Gruppe „Photovoltaik"
fehlt; PV-Anlage in einer Variante anlegen → Dialog neu öffnen → Gruppe da,
gepflegte Vergütung unverändert. Gruppe nur mit Kessel (ohne BHKW) → KWKG-Gruppe
fehlt, Brennstoff-Gruppe (BEHG/Emissionsbilanz) vorhanden. Werte einer sichtbaren
Gruppe ändern → speichern → in Access prüfen, dass ausgeblendete Spalten
unverändert sind.

## Phase 9: KWKG-2025-Nachtrag (Konzept Kapitel 8, Fassung 6)

Rechtsstand-Update des KWKG-Bonus auf **KWKG 2025** (BGBl. 2025 I Nr. 54; Recherche
12.08.2026, Konzept Kap. 8). Betroffen: `WirtschaftlichkeitDaten/Ctrl.cs`,
`Form_WirtschaftlichkeitParameter.cs` — Rechenkern-Mathematik, Tarifmatrix und
Berichtsstruktur unverändert.

| Baustein | Umsetzung |
|---|---|
| **Degressive Vbh-Staffel (§ 8, Kap. 8.3)** | Neue Katalogtabelle **`Tab_KWKG_Staffel`** (JahrVon, MaxVbh), vorbefüllt 2020: 5 000 / 2023: 4 000 / 2025: 3 500 / 2026: 3 300 / 2027: 3 100 / 2028: 2 900 / 2029: 2 700 / ab 2030: 2 500 — in den Kenndaten pflegbar (künftige Novelle = neue Zeile). Kalenderjahr-Mapping über das **Inbetriebnahmejahr** (ohne Datum: Folgejahr + Hinweis); Betrachtungsjahr t → Deckel(IBN-Jahr + t − 1). Der Parameter „Vbh-Deckel" ist jetzt **Override** (0 = Staffel; Vorgabe geändert 3 500 → 0 — unkritisch, da noch keine produktiven Parameterbestände existieren). Bestandsanlagen erhalten über ihre historischen Jahre automatisch die alten Deckel (erledigt Kap. 8.5.3 und Frage 7.5) |
| **Fristenlogik § 6 (Kap. 8.2)** | Neue Parameter **KWKG-Stichtag** (Bestellung/Genehmigung/Dauerbetrieb, DateTimePicker mit Abwahl) und **geplante Inbetriebnahme**. Regeln: Stichtag > 31.12.2026 → Bonus = 0 („nicht förderfähig — Regulierungsrisiko Novelle"); IBN > Stichtag + 4 Jahre → Bonus = 0 (Realisierungsfrist); kein Stichtag → rechnen + Hinweis „Förderfähigkeit ungeprüft" |
| **Guards (Kap. 8.4/8.5)** | **> 500 kW**: Σ `Tab_BHKW.Pel` je Projekt > 500 kW → Bonus = 0 (Ausschreibungslücke seit 01.01.2026). **Heizöl**: BHKW-Träger der Kategorie Öl bei Förderbeginn ≥ 2025 → Bonus = 0 (Neuanlagen nur noch Erdgas). Beide mit Hinweiszeile |
| **Negativpreis-Abschlag (§ 7 Abs. 5, Kap. 8.5.4)** | Parameter „Abschlag Negativstunden [%]" kürzt die vergüteten Vbh je Jahr; die abgeschlagenen Stunden verbrauchen das **Kontingent nicht** (Auszahlung streckt sich — Barwerteffekt). W2-Näherung; exakte Spotpreis-Stundenrechnung bleibt offen |
| **Novellen-Szenario (Kap. 8.5.7)** | Zusätzliche Sensitivitätszeile **„KWKG-Bonus entfällt (Regulierungsrisiko Novelle)"**: −Δ = Kapitalwert ohne KWKG, Basis/+Δ = Fortschreibung heutiger Sätze — erscheint automatisch in Reiter-Ausgabe, Word und Excel, sobald ein KWKG-Bonus wirkt |

Die Nachweiszeile (Reiter/Word/Excel) weist jetzt Sätze, Staffel bzw. Override,
Kontingent, Abschlag, Stichtag und IBN aus. Numerisch verifiziert (Python-Nachbau,
31 Prüfungen): Staffel-Lookup alle Jahre, Auszahlungsreihe Förderbeginn 2027
(12 Jahre statt 9, Summe = Kontingentanteil), Bestandsanlage 2021, Override,
Abschlag, Fristenregeln.

**Hinweis zur Vorbelegung § 7 KWKG:** Zuschlagssätze unverändert (Einspeisung bis
50 kW: 8,0 ct/kWh; **Neuanlagen ≤ 50 kW nach § 7 Abs. 3a: 16 ct Einspeisung /
8 ct Eigenversorgung**) — Eingabe wie bisher über die beiden Bonus-Felder.

**Testschritte Phase 9:** Parameterdialog → Stichtag/IBN setzen, Deckel-Override
auf 0 lassen → Berechnen → KWKG-Erlösreihe streckt sich (Kapitalwert sinkt leicht
gegenüber konstantem 3 500er-Deckel); Stichtag auf 2027 stellen → Bonus 0 +
Hinweis; Sensitivitätstabelle → neue Zeile „KWKG-Bonus entfällt".
`Tab_KWKG_Staffel` in Access prüfen (8 Zeilen).

## Phase 8 (neu): Wirtschaftlichkeit Ausbaustufe W3 — Tarife und Emissionsbilanz

Entscheidungen (11.08.2026): vereinfachtes Tarifmodell (Monatsspanne Winter, EIN
HT-Fenster Mo–Fr) · Kraftwerkspark als **Katalogtabelle mit Vorbefüllung** ·
**KWKG-Split** Eigenstrom/Einspeisung über die Strommengen-Matrix · Emissionsbilanz
in **Bericht + Excel + Reiter-Zeile**.

| Datei | Inhalt |
|---|---|
| `Allgemein/Wirtschaftlichkeit/StromMatrix.cs` | Strommengen-Matrix Winter/Sommer × HT/NT aus den Stundenreihen (Netzbezug, PV-Einspeisung, KWK-Eigenstrom = stundenweise min(BHKW-Strom, Strombedarf), KWK-Einspeisung; Jahres-Bezugsspitze). Wochentage über Referenzjahr 2026; Kostenrechnung Zonenpreise + **zweistufige Leistungspreis-Staffel** |
| `Allgemein/Wirtschaftlichkeit/EmissionsBilanzRechner.cs` | Emissionsbilanz gekoppelt vs. getrennt (Konzept Kap. 2.8): dieselbe Brennstoff-Wärme im Referenzkessel + derselbe KWK-Strom im **Kraftwerkspark** (neue Katalogtabelle `Tab_Kraftwerkspark`, vorbefüllt: Dt. Strommix / Erdgas-GuD / Steinkohle; Wirkungsgrad, CO₂ g/kWh, SO₂/NOx mg/kWh, Netzverluste). Faktorkette je Träger wie gehabt (Projekt → Tab_Brennstoff_Stamm → energy_carrier); rechnet live aus den Jahresergebnissen |
| `Views/Wirtschaftlichkeit/Form_Tarifstruktur.cs` | Dialog „Tarifstruktur Strom": aktiv/inaktiv, Zeitzonen, je 4 Bezugs-/Einspeisepreise, Staffel — eine Zeile je Stamm in **`Tab_ProjektTarif`** |

Geändert: `WirtschaftlichkeitDaten.cs` (TarifParameter, Kraftwerkspark, EmissionsBilanz,
W3-Felder StromkostenTarif/Hinweis, Parameter KWKG-Einspeisesatz/Kraftwerkspark/Referenzkessel),
`WirtschaftlichkeitCtrl.cs` (Tab_ProjektTarif + **`Tab_ErgebnisStromMatrix`** persistiert
die Matrix je Projekt; Tarifkosten ersetzen die Flat-Stromkosten nur bei vollständiger
Datenlage, sonst Hinweis; KWKG-Reihe mit Split, Fallback W2 ohne Stundenreihen),
`Form_Wirtschaftlichkeit.cs` (Schaltfläche „Tarifstruktur…", **mitZeitreihen** sobald
Tarif oder KWKG aktiv → frische In-Memory-Simulation je Projekt, neue Zeilen
Stromkosten Tarif / CO₂-Vermeidung / Hinweis), `Form_WirtschaftlichkeitParameter.cs`
(KWKG-Einspeisesatz, Kraftwerkspark-Auswahl, Referenzkessel-η),
`BausteineWirtschaftlichkeit.cs` + `ExcelBerichtGenerator.cs` (Abschnitte
Strommengen-Matrix und Emissionsbilanz), `BerichtTexte.cs` (+Übersetzungen).

**Dokumentierte Näherungen (W3):** KWK-Eigenstrom über die stundenweise min-Regel
mit Abzug der PV-Eigennutzung (die Simulation führt keinen getrennten BHKW-Export);
Wochentage der HT-Zuordnung über das Referenzjahr 2026; Kraftwerkspark „Strommix"
mit η = 100 % und Netzverlusten 0 (im Mixfaktor enthalten), GuD/Steinkohle mit
Brennstoff-Faktoren aus dem Kenndaten-Katalog. **Schutzregeln:** Tarifersatz nur
bei gepflegten Bezugspreisen und vollem Stundenjahr (sonst Flat + Hinweis; er
ersetzt Arbeits-, Grund- UND Leistungspreis); Mengenabgleich Stundenreihe vs.
Jahresergebnis (> 5 % → Hinweis); Wirkungsgrade tolerant (0,9 ≙ 90 %) und
geklemmt; Emissionsbilanz nur bei zum Simulationslauf passendem Ergebnis.

**Testschritte Phase 8:** Tarifdialog füllen und aktivieren → Berechnen (simuliert
jetzt mit Stundenreihen) → Zeile „Stromkosten Tarif" und Matrix im Bericht prüfen;
Parameterdialog → Kraftwerkspark wählen → CO₂-Vermeidungszeile + Emissionsbilanz-
Abschnitt; KWKG mit beiden Sätzen gegen den W2-Stand (ein Satz) vergleichen.

## Phase 7 (neu): Wirtschaftlichkeit Ausbaustufe W2 + Kostenmodul-Befunde

**W2 (Konzept_Wirtschaftlichkeit.md Kap. 5.4)** — alles auf Basis der W1-Struktur:

| Baustein | Umsetzung |
|---|---|
| **Sensitivitätsanalyse** | je Variante 4 Parameter (Zins ±1 %-Pkt, Energiepreissteigerung ±1 %-Pkt, Investition ±10 %, Energiekosten ±10 % inkl. CO₂-Abgabe; Ausschläge als Konstanten `SENS_DELTA_*` in `WirtschaftlichkeitCtrl`) → KW vs. Stamm bei −Δ/Basis/+Δ; Zins/Preissteigerung wirken auf beide Projekte, Invest/Energie nur auf die Variante; persistiert in **`Tab_ErgebnisWirtSensitivitaet`**; Tabellen im Word-Baustein + Excel-Blatt |
| **Interner Zinsfuß (IRR)** | Nullstelle der nominalen Differenz-Zahlungsreihe inkl. Restwert (Bisektion, −99 %…1000 %); `KapitalwertRechner.InternerZinsfuss`; „—" wenn kein Vorzeichenwechsel |
| **BEHG-CO₂-Abgabe** | Parameter `CO2_Preis` [€/t] × **Brennstoff-CO₂** (neues Feld `VariantenDaten.CO2Brennstoff` aus dem KostenEmissionRechner; nur abgabepflichtige Träger: Kategorien Gas/Öl/Koks/Kohle/Sonstige ohne Biogas — Netzstrom, Holz/Pellets, Rapsöl, Fernwärme, Wasserstoff sind frei; Bio-Heizöl-Blends zählen näherungsweise voll); steigt mit der Energie-Preissteigerung; 0, solange kein Preis gesetzt oder CO₂-Faktoren unvollständig |
| **KWKG-Bonus** | Parameter `KWKG_Bonus` [ct/kWh] auf die BHKW-Strommenge, je Jahr gedeckelt (`KWKG_Vbh_Jahresdeckel`, Vorgabe 3.500 Vbh), kumuliert bis `KWKG_Vbh_Kontingent` (Vorgabe 30.000 Vbh); Näherung: erreichte Vbh = Betriebsstunden BHKW; getrennte Sätze Eigenstrom/Einspeisung erst mit W3 (Strommengen-Matrix) |

Neue/erweiterte Spalten werden per `SpalteSicher()` automatisch nachgerüstet
(`Tab_ProjektWirtschaftlichkeit`: CO2_Preis, KWKG_Bonus, KWKG_Vbh_*;
`Tab_ErgebnisWirtschaftlichkeit`: IRR, CO2Abgabe, KWKGErloes). Reiter + Bericht
zeigen die neuen Zeilen CO₂-Abgabe, KWKG-Erlös Jahr 1 und Interner Zinsfuß.

**Kostenmodul-Befunde behoben (Konzept Kap. 3.5 / Befunde B4–B6):**

1. **B4** `Form_KostenAdmin`: Insert nutzte den Variablennamen als SQL-Bezeichner
   (schlug immer fehl), Delete verwendete @-Parameter ohne Platzhalter, Zugriff
   lief über einen eigenen Registry-Connection-String → jetzt `DataRepository`,
   Platzhalter-SQL, neue StammID, Löschen nur für `IsMainComponent = False`
   mit Rückfrage.
2. **B5** `Form_Kosten.CreateNewEnergyCarrier`: der `energy_price`-Ersteintrag
   schreibt jetzt auch den ermittelten **Leistungspreis**.
3. **B6** `Form_Kosten`: beim Schließen wird der offene Energieträger im
   Energiekosten-Tab automatisch gespeichert (`ucFuelSettings.SaveProjectAndHistory`).

**Testschritte Phase 7:** Parameter-Dialog → CO₂-Preis (z. B. 45 €/t) und ggf.
KWKG-Bonus setzen → Berechnen → neue Zeilen prüfen; Bericht erzeugen →
Sensitivitätstabellen unter „Wirtschaftlichkeit". Kostenmodul: neuen Kostenfaktor
anlegen/löschen (B4), neuen Energieträger anlegen und `energy_price.leistungspreis`
prüfen (B5), Preis ändern und Formular ohne Speichern-Button schließen (B6).

## Phase 6 (neu): Wirtschaftlichkeit — Kapitalwertmethode (DIN EN 17463, Stufe W1)

Fachliche Entscheidungen (11.08.2026, Konzept_Wirtschaftlichkeit.md Kap. 7):
**Referenz = Stammprojekt** (KW der Variante = Barwert der Differenz-Zahlungsströme
Variante − Stamm) · Vorgabe **i = 3,0 % / T = 20 a** (je Stamm editierbar) ·
**Restwert linear** · **Strompreise aus der Kostenmaske** (keine Doppelpflege).

| Datei | Inhalt |
|---|---|
| `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitDaten.cs` | DTOs (`WirtschaftlichkeitParameter`, `WirtschaftlichkeitErgebnis`, Szenarien Worst/Erwartet/Best) und Schnittstelle `IWirtschaftlichkeitProvider` (Berichtskonzept Kap. 6) |
| `Allgemein/Wirtschaftlichkeit/KapitalwertRechner.cs` | reiner Rechenkern: KW = −I₀ + Σ(E−A)/(1+i)^t + RW/(1+i)^T; Annuität a(i,n); Ersatzbeschaffungen bei Nutzungsdauer < T (nominal konstant, W1); Restwert linear; dynamische Amortisation der Differenzreihe (ohne Restwert, mit Interpolation). Ohne DB/UI — testbar gegen goetz_test.XLS/VALERI |
| `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` | legt **`Tab_ProjektWirtschaftlichkeit`** (Parameter je Stamm) und **`Tab_ErgebnisWirtschaftlichkeit`** (Ergebnis je Projekt × Szenario, **FK ID_Ergebnis auf den Simulationslauf**) bei Bedarf an; liest Tab_ProjektWerte (Kat. 1 = Investitionen mit Best/Worstcase + Nutzungsdauer, Kat. 2 = Betriebskosten), Energiekosten aus dem KostenEmissionRechner, Einspeiseerlös = PV-Überschuss × Vergütung; rechnet alle 3 Szenarien, persistiert, `LadeErgebnisse()`/`ErgebnisAktuell()` für Reiter + Bericht |
| `Views/Wirtschaftlichkeit/Form_Wirtschaftlichkeit.cs` | Reiter/Dialog „Wirtschaftlichkeit" (Berichte & Kosten): Vergleichsgruppe mit Simulationsständen (Stamm fixiert), Szenario-Umschalter, Kennzahlen-Vergleichstabelle, Parameter-Nachweiszeile, [Parameter…]/[Berechnen]; **Prüfkette laut Vorgabe**: fehlende/veraltete Simulationen werden beim Berechnen automatisch headless nachgerechnet (BerichtsDatenSammler); nicht berechenbare Projekte erscheinen mit Begründung |
| `Views/Wirtschaftlichkeit/Form_WirtschaftlichkeitParameter.cs` | Parameterdialog (i, T, Preissteigerung Energie/Betrieb, Einspeisevergütung) — eine Zeile je Stamm, gilt für die ganze Gruppe |
| `Allgemein/Bericht/Bausteine/BausteineWirtschaftlichkeit.cs` | Berichts-Baustein: Methodik + Parameternachweis, Kennzahlentabelle „Erwartet" (Blockaufteilung wie Kap. 5.1), Szenarienübersicht W/E/B, Aktualitäts-/Fehlgrund-Hinweise. **Rechnet nicht selbst** — liest die persistierten Ergebnisse (Reiter, Word, Excel = identische Zahlen) |

Geändert: `WordBerichtGenerator.cs` (Baustein registriert), `Form_Bericht.cs`
(Wirtschaftlichkeit wählbar, Hinweis auf Datenquelle Reiter),
`ExcelBerichtGenerator.cs` (+Blatt „Wirtschaftlichkeit", 3 Szenarioblöcke, echte
Zahlen), `BerichtTexte.cs` (+Übersetzungen), `Form_Variantentest(.Designer).cs`
(+Schaltfläche „Wirtschaftlichkeit (Kapitalwertmethode)…"),
`KostenEmissionRechner.cs` (Netzbezug jetzt über den Emissionsfaktor des
Strom-Trägers — Kette Projekt → Tab_Brennstoff_Stamm → energy_carrier; erst ohne
gepflegten Wert greift die Konstante 380 g/kWh).

**Einstieg für den [Wirtschaftlichkeit]-Button in Form_Start (ein Handgriff im
Designer):** `new Form_Wirtschaftlichkeit(m_ID_Projekt).ShowDialog();`
(Muster btn_Varianten_Click; Varianten werden automatisch auf ihren Stamm aufgelöst).

**Testschritte Phase 6:** App starten (legt beide Tabellen an) → Varianten-Dialog →
„Wirtschaftlichkeit…" → Parameter prüfen/speichern → Berechnen (simuliert fehlende
Ergebnisse automatisch) → Werte je Szenario prüfen. Danach Bericht mit aktivem
Baustein „Wirtschaftlichkeit" erzeugen (Word + Excel-Blatt). Validierung des
Rechenkerns: Kapitalkosten-Annuität gegen goetz_test.XLS (BHKW 17 150 € ·
a(0,35 %; 13,33 a) = 1 319 €/a).

**Offen (W2/W3, Konzept Kap. 5.4):** Preisszenarien Best/Worst für Energie,
KWKG-/EEG-Boni, Steuern, BEHG-CO₂-Preis jahresscharf, Sensitivitätstabelle, IRR,
HT/NT-Tarifstruktur, Emissionsbilanz mit Kraftwerkspark.

## Phase 5 (neu): Kosten/Emissionen, Berichtssprache, Einstiegspunkte

| Datei | Inhalt |
|---|---|
| `Allgemein/Bericht/KostenEmissionRechner.cs` | aktiviert die Kennzahlgruppen **Emissionen** (CO₂ gesamt t/a, spezifisch g/kWh Wärme) und **Kosten einfach** (Energiekosten p. a., Stromkosten Netzbezug): Verbrauch je Erzeuger-Modul (carrier_id aus Befund B1) × Preise aus `energy_project_settings`/`energy_carrier`; **CO₂-Faktor-Quelle (Vorgabe 11.08.2026): Projektwert `energy_project_settings.co2` → Katalog `Tab_Brennstoff_Stamm.CO2` (über `energy_carrier.id_brennstoff`) → `energy_carrier.co2`**; Mengen über `Abfrage_Energietraeger_Effektiv`; Netzbezug × Strommix-Konstante (380 g/kWh, Parameter). Fehlt für einen Träger mit Verbrauch Preis/Faktor → Kennzahl bleibt „—" (keine stillen Teilsummen) |
| `Allgemein/Bericht/BerichtTexte.cs` | Berichtssprache = **UI-Sprache** (`Program.nLanguage`): Kultur für Zahlen/Datum, Wörterbuch-Übersetzung der Kapitel-/Kopftexte; Kennzahl-Labels über den zweisprachigen Katalog |
| `Views/Varianten/Form_AlsVariante.cs` | Dialog **„Als Variante speichern…"** (Konzept Kap. 3.3): Bezeichner eingeben → aktueller Stand wird Variante (`VariantenCtrl.AnlegenAusStamm`); ist das offene Projekt selbst Variante, wird ihr Stamm verwendet |

Geändert: `BerichtsDaten.cs` (Rechnerfelder), `BerichtsDatenSammler.cs` (Rechner-Aufruf),
`KennzahlenKatalog.cs` (Emissions-/Kostenzeilen liefern jetzt Werte; Label
„Stromkosten Netzbezug"), `WordBerichtGenerator.cs` (Kultur = UI-Sprache; Überschriften
und fette Kopfzellen laufen durch die Übersetzung), `BausteineVergleich.cs` /
`ExcelBerichtGenerator.cs` (sprachabhängige Kennzahl-Labels).

**Menü-Einbindung (ein Handgriff in Visual Studio):** im MDI-Menü „Projekt" zwei
Einträge anlegen und verdrahten —
`Form_AlsVariante.Zeige(this, <aktuelleProjektId>, <projektname>);` bzw.
`new Form_Variantentest(<aktuelleProjektId>).ShowDialog(this);` (Varianten/Bericht).
Bewusst nicht automatisch editiert: `MDIMainForm.Designer.cs`/`.resx` werden laut
Projektkonvention nur über den WinForms-Designer gepflegt.

**Zu verifizieren (Phase 5):**
1. ~~CO₂-Faktor-Einheit~~ **verifiziert (Kenndaten.accdb, 11.08.2026)**: die Faktoren
   stehen in g/kWh (Tab_Brennstoff_Stamm: Erdgas 240, Heizöl 310, Strom 560).
   Befund dabei: die `energy_carrier`-Kopien tragen fast durchweg co2 = 0 — die
   Fallback-Kette über `Tab_Brennstoff_Stamm` ist damit zwingend.
2. Strommix-Konstante 380 g/kWh ist seit Phase 6 nur noch **Fallback**: der
   Netzbezug wird vorrangig mit dem Faktor des projektzugeordneten Strom-Trägers
   bewertet (gleiche Kette Projekt → Katalog). Hinweis: der Katalogwert „Elektrische
   Energie" (560 g/kWh) ist ein älterer Strommix — bei Bedarf im Katalog bzw. je
   Projekt in der Kostenmaske aktualisieren.
3. Vollständige Übersetzung der Fließtexte (Hinweis-/Methodiktexte) ist
   Übersetzungsarbeit und bewusst offen; Kapitel, Tabellenköpfe und Kennzahlen
   sind zweisprachig.

## Phase 4: Excel-Ausgabe (ClosedXML)

| Datei | Inhalt |
|---|---|
| `Allgemein/Bericht/ExcelBerichtGenerator.cs` | Excel-Mappe ohne Office: Blatt **Übersicht** (Projektdaten, Variantenliste mit Sim-Zeitstempel, Komponenten-Matrix), Blatt **Vergleich** (Kennzahlen als Zeilen nach 4 Gruppen, Varianten als Spalten, **echte Zahlenwerte** mit Zellformat, fixierte Köpfe, Autofilter, Δ%-Block rechts), **Detailblatt je Variante** (Kennzahlen, Erzeuger-Module, Brennstoffmengen, Monatswerte aus dem frischen Simulationslauf). Fehlende Werte bleiben leer, nie 0 |

Geändert: `Controller/BerichtCtrl.cs` (+`ErzeugeExcel`, gleiche Namens-/Kollisionslogik
wie Word), `Views/Bericht/Form_Bericht.cs` (Excel/Beide erzeugt jetzt wirklich;
Zeitreihen auch für Excel-Monatswerte), `WindowsFormsApplication1.csproj`
(+`ClosedXML 0.105.1`, `SixLabors.Fonts` auf **1.0.1 gepinnt** — Lizenzfalle ab 2.x;
`dotnet list package --include-transitive` in die Release-Checkliste aufnehmen).

**Testschritte Phase 4:** einmal `dotnet restore` (neue Pakete), dann Bericht mit
Ausgabe „Excel" oder „Beide" erzeugen. Prüfen: drei Blatt-Typen, Zahlen sind echte
Werte (weiterrechenbar), Autofilter/Fixierung im Vergleichsblatt, Δ%-Spalten,
Detailblätter nur bei aktivem Baustein „Ergebnisse je Variante" (dann inkl.
Monatswerte-Tabelle).

## Phase 3: Diagramme — Vollbericht Word

| Datei | Inhalt |
|---|---|
| `Allgemein/Bericht/ChartRenderer.cs` | Off-Screen-Diagramme (System.Drawing, PNG in doppelter Auflösung, feste Erzeuger-Palette): Kuchen (Deckung), horizontale Balken je Variante, **4 Ganglinientypen** — Wärme-Jahresverlauf (gestapelte Tagesmittel + Bedarfslinie), Jahresdauerlinie, Strombilanz je Monat (Stapel + Einspeisung + Bedarfslinie), Speicherverlauf (3 charakteristische Wochen). Abweichung vom Konzept: bewusst GDI+ statt ScottPlot (kein API-Risiko, gleiches Muster wie Bestandsbericht; ScottPlot-Umstieg bleibt möglich) |
| `Allgemein/Bericht/ZeitreihenExtraktor.cs` | sammelt nach `SimulationRunner.Simuliere()` die Stundenreihen ein (Membernamen gegen die Engine verifiziert; Aliasing-sicher durch Kopien; ¼h→h per Stundenmittel über `sim.Viertelstunden_zu_Stundenwerte_Mittelwert`) |

Geändert: `BerichtsDaten.cs` (ZeitreihenSatz mit Standard-Schlüsseln), `BerichtsDatenSammler.cs` (bei aktivem Baustein „Ergebnisse je Variante" wird je Projekt **immer frisch in-memory simuliert** — Kennzahlen und Ganglinien stammen aus demselben Lauf; Ergebnis wird dabei wie gehabt persistiert), `WordBerichtGenerator.cs` (`WordKontext.Bild` — Inline-PNG-Einbettung), `BausteineVergleich.cs` (Ganglinien in Baustein 5; Balkendiagramme je Schlüsselkennzahl + Deckungs-Kuchen in Baustein 6), `Form_Bericht.cs` (Zeitreihen-Flag).

**Testschritte Phase 3:** Bericht mit aktiven Bausteinen „Ergebnisse je Variante" + „Variantenvergleich" erzeugen. Erwartung: je Variante die vier Ganglinien mit Beschriftung (fehlt ein Gewerk, entfällt die betreffende Reihe; ohne Puffer-/PV-Speicher entfällt der Speicherverlauf); im Vergleichskapitel horizontale Balken (Stamm dunkel hervorgehoben) und die Deckungs-Kuchen je Projekt. Laufzeit steigt durch die In-Memory-Simulation je Variante (Fortschritt „Simuliere: …", Abbruch möglich).

## Phase 2 (neu): erster vollwertiger Word-Vergleichsbericht

| Datei | Inhalt |
|---|---|
| `Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx` | **Rahmen-/Stylevorlage**: Styles Title, Subtitle, Heading1–3, Normal, Hinweis, Beschriftung; Kopfzeile mit INEKON-Logo, Fußzeile mit Datum- und Seitenfeldern. In Word frei anpassbar (CI ohne Codeänderung) |
| `Allgemein/Bericht/IBerichtsBaustein.cs` | Baustein-Schnittstelle (Konzept Kap. 8.3) |
| `Allgemein/Bericht/WordBerichtGenerator.cs` | OpenXML-Generator + `WordKontext` (Style-Absätze, Tabellenbau, Blocksplitting max. 3 Varianten je Block mit wiederholter Stamm-Spalte, Δ aus Rohwerten, TOC-Feld, UpdateFieldsOnOpen). Fällt die Vorlage aus, werden Ersatz-Styles programmatisch erzeugt |
| `Allgemein/Bericht/ProjektDetails.cs` | lesende Detail-Daten je Projekt (Klimaregion, Gebäude, Tab_Energieanlagen, Komponenten je Gewerk) — Spalten gegen Kenndaten.accdb verifiziert |
| `Allgemein/Bericht/AbweichungsErmittler.cs` | deklarative Feldliste (~50 Merkmale) für Kenndaten-Tabellen **und** Abweichungserkennung Variante↔Stamm (3 Stufen: Bestand, Komponente, Auslegung) |
| `Allgemein/Bericht/Bausteine/BausteineStandard.cs` | Deckblatt, Inhaltsverzeichnis (TOC), Anhang (Simulationsstände, Methodik, Hinweise) |
| `Allgemein/Bericht/Bausteine/BausteineProjekt.cs` | Projektbeschreibung (Stamm inkl. Gebäude), Komponenten & Varianten (Matrix, Kenndaten je Gewerk, Abweichungstabellen) |
| `Allgemein/Bericht/Bausteine/BausteineVergleich.cs` | Ergebnisse je Variante (Kern-Kennzahlen), Variantenvergleich (Gruppen-Tabellen, kompakte Δ%-Tabelle ab 2 Varianten, Erzeuger-Einzellisten, Brennstoffmengen) |

Geändert: `BerichtsDaten.cs` (+`Details`), `BerichtsDatenSammler.cs` (+Details,
+Abweichungen), `Controller/BerichtCtrl.cs` (+`ErzeugeWord` mit Dateinamen
`<Projekt>_Bericht_<JJJJ-MM-TT>.docx`, ohne stilles Überschreiben),
`Views/Bericht/Form_Bericht.cs` (Word-Erzeugung + „Bericht öffnen?"),
`WindowsFormsApplication1.csproj` (Vorlage wird nach `bin\…\Vorlagen\` kopiert).

**Testschritte Phase 2:** bauen (x86) → Varianten-Dialog → „Bericht erstellen…" →
Varianten anhaken → Erstellen. Ergebnis: .docx im Zielordner; beim Öffnen fragt
Word einmal nach der Feldaktualisierung (Inhaltsverzeichnis) — mit Ja bestätigen.
Prüfen: Deckblatt/Logo, Inhaltsverzeichnis, Projektbeschreibung mit Gebäuden,
Komponenten-Matrix + Kenndaten + Abweichungen je Variante, Vergleichstabellen
(bei > 3 Varianten Blocksplitting, bei genau 1 Variante Δ-Spalte), Anhang.

**Bewusst offen:** Wirtschaftlichkeit (Reiter „Wirtschaftlichkeit" +
Kapitalwert-Rechenmodul nach DIN EN 17463) → Phase 6 gemäß
`Konzept_Wirtschaftlichkeit.md`. Der alte Direktbericht
(`ProjektvergleichBericht`, Button „…(alt)") ist mit Phase 3 fachlich abgelöst
und kann nach erfolgreichem Praxistest entfernt werden.

---

## Phase 1 (Fundament, unverändert gültig)

Neue Dateien: `Controller/VariantenCtrl.cs` (Variantenlogik inkl. `AnlegenAusStamm`
für den Menüweg, Waisen-Prüfung), `Controller/BerichtCtrl.cs` (Konfig-Persistenz
in DB-Tabelle `Berichtskonfiguration`, JSON), `Allgemein/Bericht/BerichtsDaten.cs`
(DTOs), `BerichtsKonfiguration.cs` (Baustein-Katalog), `KennzahlenKatalog.cs`
(4 Gruppen), `BerichtsDatenSammler.cs` (lesender Sammler + Statusprüfung +
headless Simulation), `Views/Bericht/Form_Bericht.cs` (Dialog).

Geändert: `Form_Variantentest` (+`.Designer`) — Delegation an `VariantenCtrl`,
Button „Bericht erstellen…"; `ErgebnisCtrl` — Befunde B1 (`carrier_id` in beiden
Modultabellen inkl. Befüllung), B2 (`Delete(int)` funktionsfähig), B3
(Kesselmodul-`Waermeproduktion` persistiert); `ErgebnisModel` (+`CarrierId`).

Nach einer Simulation prüfen: `Tab_ErgebnisBHKWModul`/`Tab_ErgebnisHeizkesselModul`
haben befüllte `carrier_id`; Tabelle `Berichtskonfiguration` entsteht beim ersten
Öffnen des Berichtsdialogs.

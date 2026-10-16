# Konzept Kältebedarfsprofile — Eingabearten, Grafik, Vorlagen und Betriebskalender

Gegenstand: der Dialog „Kältebedarfsprofile“ (Kachel „Kühlung und Kälteanlagen“ bzw. Reiter „Kältebedarf“ und Menü
„Profile & Lastgänge → Kältebedarf“) und seine Rechenvorstufe bis zur 8 760-Stunden-Reihe. Grundlage ist das
[Konzept Kältebedarf](../ueberholt/Konzept_Kaeltebedarf_EPOS-Plan.md), Abschnitte 3.3, 3.4 und 4.1; dieses Papier schreibt den
Profilteil fort und ändert nichts an Deckung, Buchung und Erzeugern. Mockup:
[`Mockups/Kaeltebedarfsprofil_Mockup.html`](Mockups/Kaeltebedarfsprofil_Mockup.html) (Bild daneben, gleicher Name, `.png`).

Anwenderwunsch: „Die Bedarfsprofile sollte ähnlich dem der Wärmebedarfsprofile sein - Funktionalität - Darstellung in
Grafik, Eingabe mit Monatswerte, Tageswerte, Wochenwerte ... Vorlagen für Bedarfsprofile erweitern (Krankenhaus,
Industrieunternehmen,...)“ und die Frage „Warum kann ich keinen Kalender auswählen bei Kachel Kühlung-> Kältebedarfsprofil“.

## 1. Ziel und Abgrenzung

**Ziel.** Ein Kältebedarfsprofil wird so eingegeben, angesehen und mit Kalendern versehen wie ein Wärmebedarfsprofil
(Prozesswärme, Brauchwasser): vier Eingabearten an der Projektzeile, eine Grafik mit Jahresgang, Monatssummen,
Wochenansicht und Dauerlinie, ein breiterer Vorlagenkatalog und ein sichtbarer Weg zum Betriebskalender direkt aus dem
Dialog.

**Feste Randbedingungen** (Bitten anderer Sitzungen, bindend):

| Nr. | Randbedingung | Folge in diesem Konzept |
|---|---|---|
| R1 | Die Feldgruppe „Deckung“ (Konzept Kältebedarf 3.4) und die Naht `SimulationPlattformwege.KaelteDezentralBearbeiten` bleiben unverändert. | Die Gruppe steht in der Bearbeiten-Überlagerung wie heute unter den Projektfeldern; kein Feld, keine Vorgabe, keine Sichtbarkeitsregel ändert sich. Das Mockup zeigt sie ausgegraut mit „bleibt wie heute“. |
| R2 | Neue Vorlagen kommen nur als **neue Katalogzeilen mit neuen IDs**; die bestehenden Zeilen in `Tab_Kaeltebedarf_STAMM` und `Tab_Kaeltetyp_STAMM` (IDs 1–6) bleiben unverändert, weil das Referenzprojekt 1072 daran hängt. | Abschnitt 6 und 8: Saat hängt ab ID 7 an, ändert keine Bestandszeile, auch nicht Bezeichner, Beschreibung oder Prüfsummenfelder. |
| R3 | Der Knopf „Betriebskalender…“ wird ein **Baustein für alle Bedarfsprofile**, auch Wärme (Brauchwasser) und Prozesswärme. | Abschnitt 7: ein Baustein in `EPOS.UI/Bausteine`, eingehängt im `BedarfsProfileDialog` für jede `BedarfsArt` mit Kalenderwahl. |
| R4 | Kältebilder sind Stapel. Bericht und `Proben/ChartProben` bleiben bitgleich. | Abschnitt 5: Die neuen Bilder entstehen nur im Dialog; kein Berichtsbild, kein Renderer und keine Messlatte ändert sich. |
| R5 | `BedarfsProfileHuelle.Oeffnen(...)` mit Pflicht-Schreibweg und `Projektkopievormerkung.Schliessen(...)` bleiben; `BedarfsprofilVormerkungWacheTests` und `BedarfsprofilZuordnungVormerkungTests` bleiben grün. | Abschnitt 3.5: Jede Bearbeitung von Monaten, Woche oder Typtagen läuft über die Projektkopie und die Vormerkung; die Signaturen bleiben. |
| R6 | Neue Diagramme laufen über `DiagrammSvg`. | Abschnitt 5: Jahresgang und Wochenansicht tragen damit die Achswahl „Datum \| Stunde im Jahr“ von selbst. |
| R7 | Schemaschritte nennen Tabellen und Spalten, aber keine Nummer. | Abschnitt 8 verweist auf die Anmeldung im Kopf von [`Status_iOS_Migration.md`](Status_iOS_Migration.md). |
| R8 | Berührte Einfrierregeln werden ausdrücklich benannt. | Abschnitt 9. |

**Nicht Gegenstand:** Erzeuger, Kältefolge, Split-Deckung, Buchung und Ergebnisgrößen (Konzept Kältebedarf 4.2 ff.), der
CSV-Lastgang „Kältebedarf extern“, die Gebäudekühlung über die Gebäudesimulation, Bericht und Wirtschaftlichkeit.

## 2. Heutiger Stand

Der Kältebedarfsprofil-Dialog ist der `BedarfsProfileDialog` mit `BedarfsArt.Kaelte`; er teilt Aufbau, Hülle und Kern mit
Prozesswärme und Brauchwasser. Die Unterschiede liegen in Katalogumfang, in den Eingabewegen der Projektzeile und darin,
wie man zu einem Kalender kommt.

| Merkmal | Wärmebedarfsprofile (Prozesswärme, Brauchwasser) und „Wärmebedarf extern“ | Kältebedarfsprofile heute |
|---|---|---|
| **Eingabeart an der Projektzeile** | Jahressumme `Summe` [MWh] je Zuordnung; Monatsanteile `Monat_1…12` am Katalogkopf, Wochenprofil (168 h) im Typkatalog über `TypProfilDialog` (24 Stundenwerte je Wochentag, Tag kopieren/einfügen, Blatt „Grafik“); Brauchwasser zusätzlich Zapfprofilgenerator mit Tagesgang je Tagtyp (`TagesgangEditor`); „Wärmebedarf extern“ als 8 760-Stunden-Lastgang (Import) | nur Jahressumme `Summe` je Zuordnung; Monatsanteile und 168 Wochenstunden nur über Katalogkopf und Typkatalog (`BedarfAdminDialog`, Art Kälte), nicht an der Projektzeile; dazu Temperaturpaar als Angabe und Art (Raum/Prozess, `Kaelte_Art`) |
| **Grafik** | `BedarfErgebnisDialog`: Jahresgang und Monatsbalken über `DiagrammSvg`; Typprofil-Wochenbild über `DiagrammSvg`; Lastgang mit Jahresverlauf | dieselben Bilder über `BedarfsVorschauCtrl` mit `ProfilQuelle.Kaelte` (Farbrolle Bedarf); keine Wochenansicht der gerechneten Reihe, keine Dauerlinie |
| **Vorlagen** | Prozesswärme 40 Katalogzeilen, 28 Typen (u. a. Krankenhaus, Wäscherei, Ein-/Zwei-/Dreischicht 5 Tage, Durchlauf 7 Tage); Brauchwasser 16 Zeilen nach VDI 6002 | sechs Sätze (IDs 1–6): Raumkühlung Büro, Raumkühlung Handel, Prozesskälte Dauerlast, Kühlraum, Tiefkühlraum, Serverraum |
| **Kalender** | Wahl „Betriebskalender“ je Zuordnung (`ID_Betriebskalender`), Verwaltung unter „Profile & Lastgänge → Betriebskalender“ | dieselbe Wahl und dieselbe Spalte — aber die Liste zeigt nur angelegte Kalender. Ist keiner angelegt (die Testdatenbank hat keinen), steht dort nur „ohne Kalender“, und der Dialog bietet keinen Weg dorthin; der Hinweistext nennt den Menüweg nur in der Überlagerung |

**Antwort auf die Anwenderfrage.** Der Kalender ist wählbar, aber nur aus angelegten Betriebskalendern, und nur für eine
markierte Projektzeile in der Bearbeiten-Überlagerung. Ohne angelegten Kalender ist die Liste leer bis auf „ohne
Kalender“. Abhilfe ist Abschnitt 7: ein Hinweis, der das sagt, und der Knopf „Betriebskalender…“ direkt neben der Wahl.

## 3. Eingabearten

Eine Wahl **„Eingabeart“** mit vier Werten steht in der Bearbeiten-Überlagerung der Projektzeile über den Eingabefeldern.
Jede Art endet in derselben Reihe: 8 760 Stundenwerte [kWh], festes Raster ohne Schaltjahr, Wochentage nach dem
Wochentagsraster der Klimaregion (in der Testdatenbank ist der 1. Januar ein Donnerstag), Feiertage nach der
Gemeinjahr-Konvention des Kerns. Kein neuer Kalenderweg, keine neue Feiertagsregel.

**Gemeinsame Kette** (heute schon für Prozesswärme und Kälte, `ProfilBedarf.Rechnen` mit `Betriebskalenderschicht`):

1. Das Wochenprofil `w[1…168]` wird auf die 365 Tage des Rasters gelegt; Tag `d` nimmt den Wochentag
   `(Wochentag(1. Januar) + d − 1) mod 7`.
2. Mit Betriebskalender ersetzt die Kalenderschicht Feiertage durch den Sonntag (`Feiertag_wie_Sonntag`) und gewichtet
   Ferientage mit dem Ferienfaktor; ohne Kalender bleibt die Woche unberührt.
3. Je Monat `m` werden die Stunden auf `Summe · a_m / 100 · 1000` kWh normiert (`a_m` = Monatsanteil in %).

### 3.1 Jahressumme mit Vorlage (Vorgabe, heutiger Weg)

Eingabe: Vorlage (Katalogsatz) und `Summe` [MWh]. Monatsanteile `a_m` und Wochenprofil kommen unverändert aus der
Projektkopie der Vorlage. Reihe nach der gemeinsamen Kette. **Jede bestehende Zuordnung ist von selbst in dieser Art**
(Migration ohne Rechenänderung, Abschnitt 8.3).

### 3.2 Monatswerte

Eingabe: zwölf Monatswerte `M_m` [MWh] in einer editierbaren Zeile (Vorbelegung aus Vorlage · Summe), Wochenprofil aus der
Vorlage. Die Kette bleibt dieselbe: `Summe = Σ M_m`, `a_m = 100 · M_m / Summe`. Gespeichert wird in den vorhandenen Spalten
— `Z_Projekt_Kaeltebedarf.Summe` und `Tab_Kaeltebedarf.Monat_1…12` der Projektkopie —, also ohne neue Wertespalten.
Ein Monat mit 0 MWh bleibt 0 (keine Stunde dieses Monats trägt Bedarf). Die Eingabe in Prozent bleibt als Umschalter
„MWh | %“ erhalten, weil der Katalog in Prozent spricht.

### 3.3 Wochenprofil

Eingabe: das Wochenprofil der Projektkopie (`Tab_Kaeltetyp."1"…"168"`) in zwei Ansichten desselben Datensatzes:

- **„Werktag | Samstag | Sonntag“** — drei Reihen zu 24 Stunden; Speichern schreibt Montag bis Freitag gleich.
  Die Ansicht ist nur angeboten, wenn Montag bis Freitag in der Projektkopie gleich sind; sonst öffnet die 168-h-Ansicht
  und die Dreitagesansicht ist mit Hinweis gesperrt („Werktage unterscheiden sich — 168-h-Ansicht“).
- **„168 h“** — sieben Reihen zu 24 Stunden wie der `TypProfilDialog` (Tag kopieren, Tag einfügen).

Werte sind relativ (kW, kWh oder %, wie im Typkatalog); die Normierung auf die Monatswerte bleibt Schritt 3 der Kette.
Monatsanteile kommen aus der Vorlage oder aus 3.2, wenn der Anwender die Monatswerte zuvor gesetzt hat (Eingabeart wechselt
ohne Verlust: beide Größen liegen in der Projektkopie).

### 3.4 Typtage (Entscheid OE-1)

Eingabe: bis zu sechs **Typtage** mit je 24 Stundenwerten und einer Tagesmenge (relatives Gewicht), dazu eine
**Zuordnungsmatrix** 12 Monate × 3 Tagesarten (Werktag, Samstag, Sonn-/Feiertag) → Typtag. Damit lassen sich z. B. ein
Sommerwerktag, ein Übergangswerktag und ein Wintertag mit eigener Form abbilden — was ein einziges Wochenprofil nicht kann.

Reihe: Tag `d` mit Monat `m(d)` und Tagesart `t(d)` (Feiertag = Sonn-/Feiertag; Ferien = Werktag mit Ferienfaktor) nimmt
Typtag `T = Matrix[m(d), t(d)]`; Stunde `h` erhält `g_T · f_T[h] / Σ f_T`, mit Tagesmenge `g_T`. Danach wird die ganze Reihe auf
`Summe` normiert. Monatsanteile entfallen in dieser Art, weil die Matrix den Jahresgang trägt; wer zusätzlich Monatswerte
erzwingen will, wählt 3.2 mit Wochenprofil.

Rechenweg: ein neuer Zweig `Typtagschicht` neben der Kalenderschicht in `ProfilBedarf`, nur für Zeilen mit Eingabeart
Typtage. Schemabedarf: Abschnitt 8.2.

### 3.5 Bearbeitung und Projektkopie

Jede Änderung an Monaten, Woche oder Typtagen betrifft die **Projektkopie** der Zuordnung, nie den Katalog. Teilen sich
zwei Zuordnungen dieselbe Projektkopie, legt die erste Bearbeitung eine eigene Projektkopie an (Kopie bei Bearbeitung) und
merkt sie in der `Projektkopievormerkung` vor; geschrieben wird erst mit OK über den Pflicht-Schreibweg von
`BedarfsProfileHuelle.Oeffnen(...)`, abgeschlossen mit `Projektkopievormerkung.Schliessen(...)`, Abbrechen verwirft die
Vormerkung (R5). Der Rückweg Projekt → Katalog bleibt der heutige („Als Vorlage speichern…“ in der Verwaltung).

## 4. Kopplung an Außentemperatur oder Kühlgradstunden

**Weggelassen.** Das Wärme-Muster hat keine Witterungsführung: Prozesswärme und Brauchwasser rechnen Monatsanteile ×
Wochenprofil × Kalender, „Wärmebedarf extern“ ist ein fester Lastgang; eine Gradtag- oder Gradstundenführung gibt es im Kern
nicht. Eine Witterungsführung nur für Kälte widerspräche dem Auftrag „ähnlich dem der Wärmebedarfsprofile“ und brächte einen
zweiten Klimabezug neben der Gebäudesimulation.

Der witterungsabhängige Kältebedarf hat seine zwei Wege schon: die Gebäudesimulation (Kanal „Kühlung“, VDI 6007) und den
Kühllastgang „Kältebedarf extern“. Als kleiner Zwischenschritt ist ein **Vorschlagsknopf** denkbar — „Monatswerte aus
Kühlgradstunden der Klimaregion vorschlagen“ —, der einmalig die zwölf Monatswerte (3.2) aus den Kühlgradstunden über einer
Grenztemperatur füllt und danach nichts mehr tut; der Rechenweg bleibt unberührt (Entscheid OE-2).

## 5. Grafik

Alle Bilder des Dialogs entstehen über `DiagrammSvg` (R6), in der Farbrolle Bedarf wie die heutige Kältevorschau. Bericht,
Diagramm-Renderer des Kerns und ChartProben bleiben unberührt (R4). Die Grafik folgt dem Arbeitsstand der Überlagerung, also
auch ungespeicherten Eingaben, über `BedarfsVorschauCtrl` (`ProfilQuelle.Kaelte`, Arbeitsstand statt Datenbank).

| Blatt | Inhalt | Achse |
|---|---|---|
| **Jahresgang** | Tagesband (Minimum bis Maximum der Stunden je Tag) als Fläche, Tagesmittel als Linie; Ferien- und Feiertage als schmale Marken am Fuß | Zeitachse des Jahresrasters — damit die Wahl „Datum \| Stunde im Jahr“ von selbst |
| **Monate** | zwölf Balken [MWh], Summenzeile darüber; bei mehreren Zuordnungen gestapelt je Zuordnung (Kältebilder sind Stapel, R4) | Monate |
| **Woche** | 168 Stunden einer wählbaren Kalenderwoche der gerechneten Reihe, Hintergrund nach Tagesart (Werktag, Wochenende, Ferien, Feiertag; Farben `--epos-kalb-*`) | Stunden der Woche |
| **Dauerlinie** | die 8 760 Stundenwerte absteigend; Kennzahlen Spitze [kW], Vollbenutzungsstunden [h], Grundlast (Minimum) [kW] | Stunden (sortiert, keine Zeitachse) |

Die Kennzahlenzeile über der Grafik zeigt Jahressumme [MWh], Spitze [kW], Vollbenutzungsstunden [h] und den Anteil der
Grundlast an der Jahresmenge.

## 6. Vorlagen

### 6.1 Bestand (bleibt unverändert, R2)

IDs 1–6 in `Tab_Kaeltebedarf_STAMM`/`Tab_Kaeltetyp_STAMM`, `ReadOnly = 1`: Raumkühlung Büro, Raumkühlung Handel,
Prozesskälte Dauerlast, Kühlraum, Tiefkühlraum, Serverraum (Werte: Konzept Kältebedarf 3.3).

### 6.2 Neue Vorlagen (neue Katalogzeilen ab ID 7)

Je Vorlage ein Kopf (`Tab_Kaeltebedarf_STAMM`, zwölf Monatsanteile, Temperaturpaar als Angabe) und ein Typ
(`Tab_Kaeltetyp_STAMM`, 168 Stunden). Die IDs vergibt die Saat fortlaufend hinter dem Bestand (heute höchste ID 6). Neutrale
Namen, keine Hersteller- oder Produktdaten.

**Belegquellen.** Nutzungszeiten und Nutzungstage nach den Nutzungsprofilen der **DIN/TS 18599-10:2025-10**, Tab. 6/7 (Tabelle der Nutzungsprofile aus DIN/TS 18599-10:2025-10, vom Anwender bereitgestellt am 14.10.2026);
Kühllastgang qualitativ nach **VDI 2078** (Kühllastberechnung, Verlauf aus inneren und äußeren Lasten); Raumnutzungsdaten
nach **SIA 2024**; Kennwerte des Verbrauchs zur Plausibilisierung nach **VDI 3807**. Wo eine Zahl nicht unmittelbar aus
diesen Quellen folgt, steht **„Richtwert, Quelle prüfen“** — die Welle KBP-2 belegt oder ersetzt diese Werte vor der Saat
und hält sie mit einer Saatwache.

| Nr. | Vorlage | Art | Nutzungszeit · Nutzungstage | Wochen- und Tagesgang (Typ) | Monatsverteilung (Kopf) | Grundlast-Anteil | Quelle |
|---|---|---|---|---|---|---|---|
| 7 | Krankenhaus | Raum | 0–24 h · 365 d (Profil 10 Bettenzimmer) | ganztägig; Tag 7–20 Uhr Faktor 1,0, Nacht 0,6; Wochenende wie Werktag | Grundlast ganzjährig, Spitze Juni–August | 40 % | Nutzungszeit DIN/TS 18599-10:2025-10, Tab. 6/7, Profil 10 Bettenzimmer; Gang, Monate, Grundlast: Richtwert, Quelle prüfen |
| 8 | Industrie Prozesskälte einschichtig | Prozess | Mo–Fr 7–16 Uhr · 230 d | Mo–Fr 7–16 Uhr 1,0; sonst Grundlast; Sa/So Grundlast | zwölf Monate fast gleich, leichter Sommeranstieg (+10 % Juli/August) | 10 % | Nutzungszeit DIN/TS 18599-10:2025-10, Tab. 6/7, Hallenprofile 22–24 (gewerbliche und industrielle Hallen); gewählt Mo–Fr; Gang, Monate, Grundlast: Richtwert, Quelle prüfen |
| 9 | Industrie Prozesskälte zweischichtig | Prozess | Mo–Fr 6–22 Uhr | Mo–Fr 6–22 Uhr 1,0; sonst Grundlast | wie Nr. 8 | 15 % | Richtwert, Quelle prüfen (die Norm kennt keinen Mehrschichtbetrieb; Tagesgrenzen auf Grundlage der Hallenprofile 22–24) |
| 10 | Industrie Prozesskälte dreischichtig | Prozess | Mo–Fr 0–24 Uhr | Mo–Fr ganztägig 1,0; Sa/So Grundlast | wie Nr. 8 | 20 % | Richtwert, Quelle prüfen (die Norm kennt keinen Mehrschichtbetrieb; Tagesgrenzen auf Grundlage der Hallenprofile 22–24) |
| 11 | Rechenzentrum | Prozess | 0–24 h · 365 d (Profil 21) | IT-Last nahezu gleich; Mo–Fr 8–18 Uhr Aufschlag 15 % | zwölf Monate gleich | 85 % | Nutzungszeit DIN/TS 18599-10:2025-10, Tab. 6/7, Profil 21 Rechenzentrum; Aufschlag und Grundlast: Richtwert, Quelle prüfen |
| 12 | Büro/Verwaltung | Raum | 7–18 Uhr · 250 d (Profile 1/2) | Mo–Fr 7–18 Uhr, Spitze 13–16 Uhr; Wochenende Grundlast | April–Oktober, Spitze Juli/August | 10 % (Serverräume, Technik) | Nutzungszeit DIN/TS 18599-10:2025-10, Tab. 6/7, Profile 1/2 Einzel-/Gruppenbüro; Gang qualitativ VDI 2078; Monate, Grundlast: Richtwert, Quelle prüfen |
| 13 | Hotel | Raum | Zimmer 21–8 Uhr (Profil 11), Allgemeinflächen tags · 365 d | ganztägig; Spitze 17–23 Uhr; Wochenende wie Werktag | Mai–September | 25 % (Küche, Kühlräume) | Nutzungszeit DIN/TS 18599-10:2025-10, Tab. 6/7, Profil 11 Hotelzimmer; Spitze, Monate, Grundlast: Richtwert, Quelle prüfen |
| 14 | Lebensmitteleinzelhandel mit Gewerbekälte | Prozess | Mo–Sa 8–20 Uhr · 300 d (Verkauf, Profil 7), Kühlmöbel 0–24 h | 168 h Grundlast der Kühlmöbel; Öffnungszeit Mo–Sa Aufschlag 1,0; Sonntag Grundlast | ganzjährig, Sommeranstieg (+20 % Juli/August) | 60 % | Nutzungszeit DIN/TS 18599-10:2025-10, Tab. 6/7, Profil 7 Einzelhandel mit Kühlprodukten; Gewerbekälte, Monate, Grundlast: Richtwert, Quelle prüfen |
| 15 | Schule/Hochschule | Raum | 8–15 Uhr (Klassenraum, 200 d), Hörsaal 8–18 Uhr | Mo–Fr 8–16 Uhr; Wochenende 0 | Mai–September **ohne** Ferienabzug — Ferien kommen über den Betriebskalender | 5 % | Nutzungszeiten DIN/TS 18599-10:2025-10, Tab. 6/7, Profile 8 Klassenzimmer, 9 Hörsaal; Monate, Grundlast: Richtwert, Quelle prüfen |
| 16 | Labor | Raum | 7–18 Uhr · 250 d (Profil 38; Lüftung nach der Norm 24 h/d) | Mo–Fr 7–18 Uhr 1,0; sonst Grundlast (Abzüge, Geräte) | ganzjährig mit Sommerspitze | 40 % | Nutzungszeit DIN/TS 18599-10:2025-10, Tab. 6/7, Profil 38 Labor; Grundlast, Monate: Richtwert, Quelle prüfen |

**Belegstand der Saat (Welle KBP-2).** Die Saat (`KaeltevorlagenSaat`, Schritt `KaeltebedarfsprofilVorlagenSchema`) führt die Werte
der Tafel mit IDs 7–16 und vermerkt je Vorlage ihre Quelle im Kommentar und in der Eigenschaft `Quelle`. Belegt sind allein
die Nutzungszeiten und Nutzungstage: Sie folgen den Nutzungsprofilen der DIN/TS 18599-10:2025-10, Tab. 6/7 — Profil 10 Bettenzimmer
(0–24 Uhr, 365 d), 21 Rechenzentrum (0–24 Uhr, 365 d), 1/2 Einzel- und Gruppenbüro (7–18 Uhr, 250 d), 11 Hotelzimmer
(21–8 Uhr, 365 d), 7 Einzelhandel mit Kühlprodukten (8–20 Uhr, 300 d), 8 Klassenzimmer (8–15 Uhr, 200 d), 9 Hörsaal
(8–18 Uhr, 150 d), 38 Labor (7–18 Uhr, 250 d; die Lüftung läuft nach der Norm 24 h/d) und die Hallenprofile 22–24 für die
Prozesskälte einschichtig (7–16 Uhr, 230 d). Die Norm kennt keinen Mehrschichtbetrieb: Die Nutzungszeit der Prozesskälte
zwei- und dreischichtig (Nr. 9, 10) bleibt Richtwert, die Hallenprofile sind Grundlage der Tagesgrenzen. Abweichend von der
Tafel nimmt Nr. 14 die Normzeit 8–20 Uhr statt 7–21 Uhr. Tagesgänge,
Monatsverteilungen und Grundlasten folgen keiner Normtabelle: VDI 2078 trägt nur den qualitativen Verlauf (Büro, Spitze
nachmittags), VDI 3807 nur die Plausibilisierung des Jahresverbrauchs, SIA 2024 wurde nicht herangezogen. Diese Werte bleiben
ausgewiesene **Richtwerte** mit der Kennung „Richtwert, Quelle prüfen“; bei der Prozesskälte der Schichtbetriebe (Nr. 8–10) sind
Gang, Monate und Grundlast Richtwert, bei Nr. 9 und 10 auch die Nutzungszeit. Der Grundlast-Anteil ist der Wochenwert außerhalb der Nutzungszeit relativ zur Spitze; beim Krankenhaus
(Nachtwert 0,6) und bei den Raumkühlungen mit Jahresgang steht er zugleich als Faktor der Wintermonate. Den Art-Vorschlag
trägt die Katalogspalte `Kaelte_Art` (Werte wie an der Zuordnung, `RAUM`/`PROZESS`; leer an den Zeilen 1–6). Die
Saatwache `KaeltebedarfsprofilVorlagenSchemaTests` hält Zeilen, Monatssummen 100, vollständige Tagesgänge, die Quelle je Vorlage und die
Zeilen 1–6 bytegleich gegen einen eingefrorenen Abdruck.

Temperaturpaare der neuen Köpfe bleiben Angabe ohne Wirkung (E-K3): Raumkühlung 16/19 °C, Prozesskälte 6/12 °C,
Rechenzentrum 18/24 °C, Gewerbekälte −2/+4 °C — runde Ordnungswerte wie im Bestand.

**Doppelzählung vermeiden.** Ferien und Feiertage gehören in den Betriebskalender, nicht in die Monatsanteile; die
Beschreibung jeder Vorlage mit Ferien (Schule, Hochschule) sagt das, und der Dialog zeigt bei diesen Vorlagen den Hinweis
„Ferien über den Betriebskalender wählen“.

**Art vorschlagen.** Die Spalte „Art“ der Tabelle setzt beim Übernehmen die Kälte-Art der Zuordnung (`Kaelte_Art`) vor;
sie ist ein Vorschlag, der Anwender kann sie ändern.

## 7. Kalender

### 7.1 Baustein „Betriebskalender…“ (R3)

Ein Baustein **`Betriebskalenderwahl`** in `EPOS.UI/Bausteine` ersetzt die heutige Gruppe „Kalender“ im
`BedarfsProfileDialog` und gilt für jede Bedarfsart mit Kalenderspalte — Brauchwasser, Prozesswärme, Kälte, und Strom,
sobald dessen Zuordnung eine Kalenderspalte führt. Er besteht aus:

- der **Auswahl** (wie heute: „ohne Kalender“ und die angelegten Kalender),
- dem Knopf **„Betriebskalender…“** rechts daneben — er öffnet den bestehenden `BetriebskalenderDialog` als Blatt
  (Baustein `Blattwechsel`, keine dritte Überlagerungsebene); nach dessen OK liest der Baustein die Liste neu
  (`BetriebskalenderHuelle.WahlEinhaengen`) und wählt einen neu angelegten Kalender für die markierte Zeile vor,
- dem **Hinweis** als Herleitungszeile, je nach Lage:
  - keine Zeile markiert: „Kalender gelten je Projektzeile — erst eine Zeile wählen.“
  - kein Kalender angelegt: „Noch kein Betriebskalender angelegt. Mit „Betriebskalender…“ Feiertage und Ferien anlegen.“
  - sonst der heutige Text (`BPF_HINW_KALENDER`) ohne den Menüweg.

Texte in `MyResource.Resource.*` in beiden Sprachen. Der Baustein trägt keine Datenbank; die Liste und der Dialog kommen
über die Hülle. Auf iOS ist der Kalenderdialog eine freie Ansicht der `AppWurzel` (heute schon), der Baustein öffnet ihn
dort ebenso als Blatt.

### 7.2 Verhältnis zu den Konditionierungskalendern der Gebäude

| | Betriebskalender (Bedarfsprofile) | Konditionierungskalender (Gebäude, `KalenderbedienungSchema`) |
|---|---|---|
| Wofür | Tagesart je Tag für Profilzeilen: normal, Feiertag, Ferien | Sollwerte, Nutzung, Lüftung je Größe, Gebäude und Zone |
| Daten | `Tab_Betriebskalender`: Bundesland, vier Ferienzeiträume, Ferienfaktor, „Feiertag wie Sonntag“, „Ferien kürzen“ | Gemeinschaftsperioden, benannte Wochen, `Wochenendtage`, `Feiertagsland` am Gebäude |
| Verweis | `ID_Betriebskalender` an der Zuordnung | am Gebäude bzw. an der Zone |

Beide bleiben getrennt: ein Profil gehört nicht zu einem Gebäude, und die Gebäudekalender steuern Sollwerte, keine
Mengen. Der Betriebskalender-Dialog bekommt einen Knopf **„Aus Gebäude übernehmen…“**, der Feiertagsland und Ferien eines
Projektgebäudes **als Kopie** in den Kalender schreibt — nie als Verweis (Festlegung KP1b 6: Herkunft nie als ID am Ziel).

### 7.3 Ferien und Feiertage

- Feiertage nach den Regeln des Kerns (`Feiertage`, `Landesfeiertage`) im Gemeinjahr: Ostersonntag ist der Sonntag des
  Wochentagsrasters am nächsten zum Jahrestag 98, Buß- und Bettag der letzte Mittwoch vor Jahrestag 327; eine Preisreihe mit
  Jahr setzt die echten Daten. Keine neue Regel.
- Ferien: die vier Zeiträume des Betriebskalenders. Mehr als vier Zeiträume folgen der Ferienliste der Kalenderbedienung
  (offener Punkt K2-U), nicht diesem Konzept.
- Wochenende: die Profile rechnen mit dem Wochenprofil (Samstag und Sonntag sind Tage des Profils); die Wochenmaske
  `Wochenendtage` der Gebäude wirkt nicht auf Profile.

## 8. Datenhaltung

### 8.1 Schemaschritt A — Eingabeart und Vorlagen (Welle KBP-2)

| Tabelle | Änderung |
|---|---|
| `Z_Projekt_Kaeltebedarf` | neue Spalte `Eingabeart` TEXT NOT NULL DEFAULT 'jahr' CHECK (`Eingabeart` IN ('jahr','monat','woche','typtag')) |
| `Tab_Kaeltebedarf_STAMM`, `Tab_Kaeltebedarf` | neue Spalte `Kaelte_Art` TEXT nullbar CHECK IN die Werte von `Z_Projekt_Kaeltebedarf.Kaelte_Art` — Vorschlag der Art (6.2); leer an den Bestandszeilen 1–6 (R2: Bestandszeilen bleiben unberührt) |
| `Tab_Kaeltebedarf_STAMM`, `Tab_Kaeltetyp_STAMM` | Saat der Vorlagen 7–16 (`INSERT`, nie überschreibend, wiederholbar ohne Hochzählen der `sqlite_sequence` — die Saat setzt die IDs ausdrücklich), `ReadOnly = 1`, Katalogspalten der Fassung |

Monatswerte und Wochenprofil brauchen keine neuen Wertespalten (3.2, 3.3). Mitzuziehen: `KaeltetypSaat`, `Katalogfassung`
(Prüfsumme ändert sich, Katalogfassung + 1, Wachen `KatalogfassungWache` und `AuslieferungsvorlagenWacheTests`),
Katalogregister, `ProjektDuplizierenCtrl`, Projektpaket und `.wpx`, `Werkzeuge/Testdatenbankschema`, `sql/schema`,
SQL-Dialekt-Prüfer.

### 8.2 Schemaschritt B — Typtage (Welle KBP-4, nur nach Entscheid OE-1)

| Tabelle | Spalten |
|---|---|
| `Tab_Kaeltetyptag` (neu, `STRICT`) | `ID`, `ID_Projekt` (FK, CASCADE), `ID_Kaeltebedarf` (FK auf die Projektkopie, CASCADE), `Nr` INTEGER 1…6, `Bezeichner` TEXT NOT NULL, `"1"` … `"24"` REAL, `Tagesmenge` REAL NOT NULL DEFAULT 1 CHECK (`Tagesmenge` ≥ 0) |
| `Tab_Kaeltetyptag_Matrix` (neu, `STRICT`) | `ID`, `ID_Kaeltebedarf` (FK, CASCADE), `Monat` INTEGER 1…12, `Tagesart` TEXT CHECK IN ('werktag','samstag','sonntag'), `ID_Kaeltetyptag` (FK, CASCADE); eindeutig je (`ID_Kaeltebedarf`, `Monat`, `Tagesart`) |

Kein Katalog-Gegenstück (`_STAMM`): Vorlagen bleiben Monate × Woche; Typtage sind eine Projekteingabe. Mitzuziehen:
Duplizieren, Projektpaket, `.wpx`, Projektkopievormerkung, Testdatenbankschema, `sql/schema`, SQL-Dialekt-Prüfer.

### 8.3 Nummern und Migration

Beide Schritte tragen **keine Nummer in diesem Papier**. Die Nummer meldet die bauende Sitzung vor dem Bau in der Zeile
„Schemaschritt angemeldet“ im Kopf von [`Status_iOS_Migration.md`](Status_iOS_Migration.md) an; die Kette hängt über `+ 1`
an der Vorgängerklasse (ADR-001). Beim Schreiben dieses Papiers sind 228 und 229 angemeldet und 230 als frei geführt.

Migration: Jede bestehende Zuordnung erhält `Eingabeart = 'jahr'` über die Vorgabe der Spalte — kein `UPDATE`, keine
Rechenänderung. Bestehende Projektkopien und Katalogzeilen bleiben bytegleich. Prozesswärme und Brauchwasser bekommen die
Spalte erst mit Entscheid OE-5.

## 9. Auswirkung auf Rechenweg und Referenzbasis

- **Jahressumme, Monatswerte, Wochenprofil:** dieselbe Kette `ProfilBedarf` wie heute, dieselben Eingangsgrößen (`Summe`,
  Monatsanteile, 168 Stunden, Betriebskalender). Keine Rechenänderung.
- **Typtage:** ein neuer Zweig nur für Zeilen mit `Eingabeart = 'typtag'`; kein Referenzprojekt nutzt ihn.
- **Referenzbasis** (`2026-10-13_R53_Zapfprofiloptionen`): Kein Referenzprojekt der Basis hat eine Kältebedarfsprofil-
  Zuordnung; das geplante Referenzprojekt 1072 hängt an den Bestandszeilen 1–6, die unverändert bleiben, und rechnet mit der
  Vorgabe `Eingabeart = 'jahr'`. Erwartung: Referenzlauf byte-gleich in allen Wellen.

**Berührte Einfrierregeln** (ausdrücklich, R8):

| Regel | Berührt? | Folge |
|---|---|---|
| „gesäte Bedarfsdaten eines Referenzprojekts“ (Zuordnungen, Projektkopien, Typsätze) | **ja, sobald 1072 eingefroren ist** — die Regel bzw. die Regel „gesäte Kältebedarfsdaten“ aus Konzept Kältebedarf 8.1 umfasst dann `Z_Projekt_Kaeltebedarf`, `Tab_Kaeltebedarf`, `Tab_Kaeltetyp` | Die neue Spalte `Eingabeart` gehört in diese Regel; wer 1072 auf eine andere Eingabeart stellt oder seine Projektkopie ändert, friert neu ein. Typtag-Tabellen eines Referenzprojekts ebenso. |
| „gesäte Kältedaten“ (Kälteerzeuger, Kanal Kühlung) | nein | — |
| Katalogzeilen, die ein Referenzprojekt benutzt | **ja, mittelbar** — die Saat fügt nur Zeilen ab ID 7 an, R2 | Bestandszeilen 1–6 bleiben bytegleich; die Katalogfassung ändert ihre Prüfsumme (Wachen, nicht Basis). |
| Gemeinjahr-Konvention, Feiertagsregeln | nein | Kein neuer Kalenderweg. |
| ChartProben-Messlatte, Berichtsbilder | nein (R4) | Neue Bilder nur im Dialog. |

## 10. Umsetzungsschritte

| Welle | Inhalt | Abnahme |
|---|---|---|
| **KBP-1** Kalenderbaustein | Baustein `Betriebskalenderwahl` (Auswahl, Knopf „Betriebskalender…“ als Blatt, drei Hinweise, Neulesen, Vorwahl) für alle Bedarfsarten mit Kalenderspalte; Ressourcen beider Sprachen; „Aus Gebäude übernehmen…“ im `BetriebskalenderDialog` | bunit: Baustein, `BedarfsProfileDialogTests`, `BetriebskalenderDialogTests`; `BedarfsprofilVormerkungWacheTests` und `BedarfsprofilZuordnungVormerkungTests` grün; Windows-Schale kompiliert (`EnableWindowsTargeting`); `fensterprobe.mjs` der Rasterprobe; Ressourcen-Designer; kein Schemaschritt |
| **KBP-2** Vorlagen | Schemaschritt A (Eingabeart, Art-Vorschlag, Saat 7–16); Quellen der Richtwerte belegt oder ersetzt | Saatwache (Zeilen, Summen der Monatsanteile = 100, Bestandszeilen 1–6 bytegleich gegen eingefrorenen Abdruck); `KatalogfassungWache`, `AuslieferungsvorlagenWacheTests`; SQL-Dialekt-Prüfer; Referenzlauf der CI-Auswahl byte-gleich; `WikiProduktdatenWacheTests` |
| **KBP-3** Eingabearten und Grafik | Eingabeart-Wahl in der Bearbeiten-Überlagerung; Monatswerte (MWh/%), Wochenprofil (Werktag/Sa/So und 168 h); Kopie bei Bearbeitung über die Vormerkung; Blätter Jahresgang, Monate, Woche, Dauerlinie über `DiagrammSvg`; Kennzahlenzeile | bunit; Kern-Tests `BedarfsProfilVorschauTests`, `KaeltebedarfKernwegeTests`; Vormerkungswachen grün; `diagrammprobe.mjs`; ChartProben unverändert; Referenzlauf byte-gleich |
| **KBP-4** Typtage (nur nach OE-1) | Schemaschritt B; `Typtagschicht` in `ProfilBedarf`; Editor (sechs Typtage, Matrix) | Kern-Tests der Schicht (Summe, Monat × Tagesart, Feiertag = Sonntag, Ferienfaktor); SQL-Dialekt-Prüfer; Referenzlauf byte-gleich |
| **KBP-5** Angleichung und Doku | Eingabearten auch für Prozesswärme und Brauchwasser (nur nach OE-5); Wiki-Quellen der Bedienungsseite, Logbuch-Entwurf | Wiki-Gegenlesemuster aus `CLAUDE.md`; `DokumentationLinkWacheTests`; Konzept nach `ueberholt/`, wenn umgesetzt |

## 11. Entscheide

| Nr. | Frage | Empfehlung |
|---|---|---|
| **OE-1** | Typtage als vierte Eingabeart (eigene Tabellen, neuer Rechenzweig)? | **Ja, aber als eigene Welle nach KBP-3.** Monatswerte und Wochenprofil decken die meisten Fälle ohne Schema für Werte; Typtage lohnen, wo Sommer- und Wintertag verschieden aussehen (Raumkühlung mit Lüftung, Gewerbekälte). — *entschieden 14.10.2026: Empfehlung übernommen* |
| **OE-2** | Witterungsführung (Außentemperatur, Kühlgradstunden)? | **Nein im Rechenweg** — das Wärme-Muster hat keine, und witterungsabhängige Kälte rechnet die Gebäudesimulation. Optional der einmalige Vorschlagsknopf „Monatswerte aus Kühlgradstunden“ (Abschnitt 4), ohne Rechenänderung. — *entschieden 14.10.2026: Empfehlung übernommen* |
| **OE-3** | Umfang der Vorlagen: alle zehn neuen (Nr. 7–16) oder ein Kern? | **Alle zehn**, aber nur mit belegten Nutzungszeiten; Werte mit „Richtwert, Quelle prüfen“ werden in KBP-2 belegt oder als Richtwert in der Beschreibung gekennzeichnet. — *entschieden 14.10.2026: Empfehlung übernommen* |
| **OE-4** | Gemeinsamer Kalenderbaustein zuerst (KBP-1 vor allem anderen)? | **Ja.** Er beantwortet die Anwenderfrage sofort, braucht kein Schema, wirkt für alle Bedarfsarten und ändert keinen Rechenweg. — *entschieden 14.10.2026: Empfehlung übernommen* |
| **OE-5** | Eingabearten Monatswerte/Wochenprofil auch für Prozesswärme und Brauchwasser? | **Ja, nach KBP-3**, mit derselben Spalte `Eingabeart` an deren Zuordnungen; der Dialog ist derselbe. — *entschieden 14.10.2026: Empfehlung übernommen* |
| **OE-6** | „Aus Gebäude übernehmen…“ im Betriebskalender (Kopie von Feiertagsland und Ferien eines Projektgebäudes)? | **Ja, als Kopie** (KP1b 6), in KBP-1. — *entschieden 14.10.2026: Empfehlung übernommen* |

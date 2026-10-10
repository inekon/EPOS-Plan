# Konzept: Projektdialoge mit Katalogauswahl übersichtlich ordnen — Variante V1 „Gerahmt und gestapelt“

**Stand: Variante V1 gewählt (08.10.2026), Umsetzung auf Zuruf.** Der Anwender hat die fünf spielbaren
Mockups ausprobiert und V1 „Gerahmt und gestapelt“ gewählt; die Entscheide KA‑E‑1 bis KA‑E‑16 stehen in
Abschnitt 3, das Zielbild in Abschnitt 4, der Rückweg Projekt → Datenbank in Abschnitt 5. Die Varianten
V2 bis V5 bleiben in Abschnitt 6 als Abwägung stehen. Was für die Umsetzung noch zu klären ist, nennt
Abschnitt 7 mit Empfehlung; der Stufenplan steht in Abschnitt 8.

**Anlass:** Anwenderauftrag vom 08.10.2026 mit Bildschirmfoto „Verwaltung BHKW“ (Fensterdialog,
1 546 × 1 000 px): „Dieser Dialog ist sehr unübersichtlich mit Scrollbar in Scrollbar.“ Gefordert sind
Funktionalität wie bisher, eine übersichtlichere Zuordnung von Datenbank zu Projekt, eine klare Zuordnung
der Knöpfe zu den Bereichen Projekt und Datenbank und Eignung für alle ähnlichen Dialoge.

**Geltungsbereich:** die zwölf Projektdialoge auf dem Baustein `Zweispaltenauswahl` — `BhkwDialog`,
`HeizkesselDialog`, `PufferspeicherDialog`, `StromspeicherDialog`, `PhotovoltaikDialog`,
`SolarkollektorenDialog`, `WaermepumpenDialog`, `GebaeudeDialog`, `BedarfsProfileDialog`,
`WaermebedarfExternDialog`, `StromganglinieDialog`, `SolarganglinieDialog` — und die Katalogauswahl des
`KaeltemaschineAnlageDialog`. Damit schließt dieses Papier die Lücke, die
[`Konzept_Administrationsdialoge_Neuordnung_EPOS-Plan.md`](Konzept_Administrationsdialoge_Neuordnung_EPOS-Plan.md)
in Abschnitt 8 offen lässt (AD-Q12: Projektdialoge außerhalb des Schemas der Verwaltungen); die dort
ruhenden Entscheide AD-Q3, AD-Q4, AD-Q5 und AD-Q10 sind mit KA‑E‑5 bis KA‑E‑8 beantwortet.

**Mockups:** maßgeblich ist [`Mockups/Projektdialog_Katalogauswahl_V1.html`](Mockups/Projektdialog_Katalogauswahl_V1.html)
(spielbar, mit ziehbarer Trennlinie) samt `…_V1.png` (1 280 × 800) und `…_V1_klein.png` (1 280 × 720). Die
Mockups V2 bis [`Mockups/Projektdialog_Katalogauswahl_V5.html`](Mockups/Projektdialog_Katalogauswahl_V5.html)
bleiben als Belege der Abwägung liegen. Der Knopf „Randnotiz“ oben rechts zeigt, wie die dialogeigenen
Teile hineinpassen.

## 1 Befund: warum es doppelt rollt

Der heutige Projektdialog hat **drei Rollbereiche, zwei davon im dritten**:

| Nr | Rollbereich | Fundstelle | Wirkung |
|---|---|---|---|
| ① | das Fenster (Dokument) | `EPOS.UI/wwwroot/epos-ui.css:331` — `.epos-dialog` hat keine Höhengrenze; im eigenen Fenster rollt deshalb das Dokument, nur Kopf und Schlussleiste haften (`epos-ui.css:2535`, `:2545`, Rollpolster `:2602`) | die ganze Maske rollt |
| ② | die Projektliste | `epos-ui.css:3489` — `.epos-zweispalten-spalte--oben .epos-raster-huelle { max-height: var(--epos-projektlistenhoehe) }` (12 rem, `:175`) mit `overflow: auto` aus `:1598` | rollt in ① |
| ③ | die Katalogliste | `epos-ui.css:1596` — `.epos-raster-huelle { overflow: auto; max-height: var(--epos-listenhoehe) }` (22 rem, `:131`) | rollt in ① |

**Ursache der Höhe:** `EPOS.UI/Bausteine/Zweispaltenauswahl.razor:63–102` stapelt Projektliste,
Übernahmeleiste und Katalogliste untereinander (Entscheid W14a-E-10-Q2: Katalog über die ganze Breite).
Darunter hängt jeder Wirt seinen Satzblock an — beim BHKW die Gruppe „Modul“ (`BhkwDialog.razor:117`) mit
dem Aufklapper „Alle Daten“ (`:251`, `:261`), der nach `:572` (`_parameterOffen = true`) **aufgeklappt**
startet. Die Maske wird damit 1 152 bis 1 228 px hoch (Konzept Katalogfilter 5.6.5) gegen 624 bis 1 000 px
Fenster. Das Mausrad rollt über einer Liste die Liste, daneben das Fenster — „Scrollbar in Scrollbar“.

**Ursache der unklaren Zuordnung:**

- Die Übernahmeleiste (`Zweispaltenauswahl.razor:70–89`) steht **zwischen** den Listen und gehört keiner;
  „▲ In das Projekt übernehmen“ wirkt auf die Katalogliste darunter, „▼ Aus dem Projekt entfernen“ auf die
  Projektliste darüber. Wer unten im Katalog wählt, rollt zum Übernehmen zurück nach oben.
- Neu…, Schloss setzen…, Löschen stehen unter der Katalogliste (`BhkwDialog.razor:105–113`), Kosten und
  „Bearbeiten…“ im Block „Modul“ in **einer** Leiste (`:124–142`), obwohl die Kosten auf die gewählte
  **Projektzeile** wirken (`BeiKosten`) und „Bearbeiten…“ auf den gewählten **Katalogsatz**.
- Der Block „Modul“ zeigt je nach Wahl einen Projektsatz oder einen Katalogsatz (`ProjektZeileWaehlen`
  `:819`, `KatalogZeileWaehlen` `:829` schließen einander aus) — ohne zu sagen, welchen.

## 2 Funktionsinventar

Bereich: **P** wirkt auf die Projektliste, **K** auf den Katalog (die Datenbank als Liste), **S** auf den
gewählten Satz und seine Daten (**S·P** Projektsatz, **S·K** Katalogsatz).

| Funktion | Bereich | Dialoge |
|---|---|---|
| Projektliste mit Wahl (Radio) | P | alle zwölf |
| Summe der Projektkomponenten (kWth, kWp, kWh, Modulzahl) | P | BHKW, Kessel, Puffer, Stromspeicher, PV, Kollektoren; Bedarfsprofile (Jahressumme) |
| In das Projekt übernehmen (▲) | P (Eingabe aus K) | alle zwölf; Kältemaschine „Hinzufügen“ über eine Katalogüberlagerung |
| Aus dem Projekt entfernen (▼) | P | alle zwölf, Kältemaschine |
| Umstellen (Gerät ersetzen) | P | Wärmepumpe |
| In DB übernehmen (Projekt → Katalog) | P → K | Gebäude (`Leistenzusatz`) |
| Suche (`*`, `?`), Trefferzahl, Filter zurücksetzen | K | alle mit `Katalogliste` |
| Spalten sortieren und filtern (Pfeil, Trichter) | K | alle mit `Katalogliste` |
| Schloss hinter dem Bezeichner, „im Projekt verwendet“ | K | Erzeuger, Gebäude, Bedarfsprofile |
| Vergleichen (`Vergleichsparameter`) | K | BHKW, Kessel, Puffer, Stromspeicher, PV, Kollektoren |
| Neu… (Namensfrage) | K | BHKW, Kessel, Kollektoren, Gebäude, Bedarfsprofile |
| Import…, Speichern unter… | K | Gebäude (Import); Wärmebedarf extern, Stromganglinie |
| Schloss setzen/aufheben… (`Katalogschloss`) | K | alle mit Katalogpflege |
| Löschen (mit Rückfrage) | K | alle mit Katalogpflege |
| Baustoffzuordnungen… | K | Gebäude |
| Typ ändern… / DB ändern… | K | Bedarfsprofile |
| Bearbeiten… (Katalogeditor als Überlagerung) | S·K | alle Erzeuger, Kollektoren, Zeitreihen |
| Alle Daten (Aufklapper, bearbeitbar) · Speichern | S·K | BHKW, Kessel, Puffer, Stromspeicher, PV, Kollektoren |
| Kenndaten des Satzes (nur lesen) | S | alle |
| Energieträger (Trägerwahl als Überlagerung) | S·P | BHKW, Kessel, Stromspeicher, PV |
| Grenzleistung, Vorlauf, Rücklauf, Senken | S·P | BHKW, Kessel |
| Investitions-, Betriebs-, Energiekosten… | S·P | BHKW, Kessel, Puffer, Stromspeicher, PV, Kollektoren |
| Stückzahl (Module, Kollektoren) | S·P | PV, Kollektoren |
| Neigung, Azimut, Albedo, Modellfelder | S·P | PV, Kollektoren |
| Stränge und Wechselrichter (`PvStraengeFelder`) | S·P | PV |
| Auslegen… (Pufferauslegung) | S·P | Puffer |
| eingebetteter Anlagendialog | S·P | Wärmepumpe (`WaermepumpeAnlageDialog`) |
| Verbrauch, Temperatur, Kalender, Simulation… | S·P | Bedarfsprofile; Gebäude (Wohnfläche, Ausrichtung, Simulation, Export, Neu lesen) |
| Ganglinie mit Kennzahlen | S | Wärmebedarf extern, Strom- und Solarganglinie |
| Grundlagen-, Berechnungshilfe (i) | S | Erzeuger |
| Hilfe (i), KI, ✕ im Kopf; Abbrechen · OK im Fuß | Dialog | alle |
| Verwaltungsbetriebsart (nur Katalog, `NurRechts`) | K | Gebäude |

**Allen gemeinsam:** Projektliste, Übernehmen/Entfernen, Katalogliste mit Suche und Spaltenfilter,
Schloss, Satzanzeige, Kopf und Fuß. **Dialogeigen:** Summe, Stückzahl, PV-Stränge, Kosten, Vergleichen,
Auslegen, Umstellen, In DB übernehmen, Import, Ganglinie, eingebetteter Anlagendialog.

## 3 Entscheide des Anwenders

Entschieden am 08.10.2026 nach dem Ausprobieren der fünf Mockups. Die Kennungen ersetzen die Fragen
KA‑Q1 bis KA‑Q9 des Entwurfs (Zuordnung in der letzten Spalte).

| Kennung | Entscheid | Datum | Frage des Entwurfs |
|---|---|---|---|
| **KA‑E‑1** | **Variante V1 „Gerahmt und gestapelt“:** oben der Bereich „Im Projekt“, darunter „Katalog (Datenbank)“, unten die Zeile „gewählter Satz“. Jeder Bereich ist gerahmt und hat eine eigene Kopfleiste mit seinen Knöpfen. Der Dialogkörper rollt nicht, nur die Listen rollen, nie ineinander. | 08.10.2026 | KA‑Q1, KA‑Q2 |
| **KA‑E‑2** | **Geltung:** alle zwölf Projektdialoge mit Katalogauswahl (BHKW, Heizkessel, Pufferspeicher, Stromspeicher, Photovoltaik, Solarkollektoren, Wärmepumpe, Gebäude, Bedarfsprofile, Wärmebedarf extern, Strom- und Solarganglinie) und die Katalogauswahl der Kältemaschine; **ein** gemeinsamer Baustein. | 08.10.2026 | — |
| **KA‑E‑3** | **Höhe:** Die Trennlinie zwischen Projekt- und Katalogbereich ist ziehbar; die Höhe wird je Dialog gemerkt (Einstellungsdienst des Kerns, `Dienste.Einstellungen`). | 08.10.2026 | KA‑Q3 (entfällt mit V1) |
| **KA‑E‑4** | **Detailzeile „gewählter Satz“:** zugeklappt und aufklappbar. Aufgeklappt nimmt sie den Listen Höhe ab, die Listen bleiben bedienbar. Sie enthält „Alle Daten“ und beim Projektsatz die Kostenknöpfe (Investitions-, Betriebs-, Energiekosten). | 08.10.2026 | — |
| **KA‑E‑5** | **Mehrfachauswahl** in beiden Bereichen. Alle Bereichsaktionen wirken auf die Auswahl. Das Kopfhäkchen „alle wählen“ bezieht sich auf die gefilterte Liste. | 08.10.2026 | KA‑Q4 (AD-Q4) |
| **KA‑E‑6** | **Doppelklick und Enter** übernehmen eine Katalogzeile ins Projekt (nur im Projektdialog), Sammelübernahme bei Mehrfachauswahl. | 08.10.2026 | KA‑Q5 (AD-Q3) |
| **KA‑E‑7** | **Katalogpflege bleibt im Projektdialog**, im Katalogbereich: Neu…, Bearbeiten…, Schloss setzen/aufheben…, Löschen, Vergleichen…. | 08.10.2026 | KA‑Q6 (AD-Q10) |
| **KA‑E‑8** | **Bearbeiten… je Bereich:** Projektbereich → Projektkopie, Katalogbereich → Katalogsatz. Die Marke „Projektsatz“/„Katalogsatz“ im Kopf sagt, was gespeichert wird. Ein gesperrter Katalogsatz ist nur lesbar, mit dem Hinweis „Erst Schloss aufheben“. **Mehrfach-Bearbeiten:** Blätterleiste „‹ 1 von n ›“ und je Feld „für alle gewählten setzen“; gesperrte Katalogsätze werden übersprungen und genannt. | 08.10.2026 | KA‑Q7 (AD-Q5) |
| **KA‑E‑9** | **Rückweg „In die Datenbank übernehmen…“** im Projektbereich, auch für eine Mehrfachauswahl: immer mit Rückfrage, ob ein neuer Katalogsatz angelegt oder der ursprüngliche überschrieben wird; Überschreiben nur, wenn der Ursprung noch existiert und nicht gesperrt ist, sonst ausgegraut mit „Erst Schloss aufheben“ bzw. „Ursprung nicht mehr vorhanden“. Es gehen technische Daten **und** Kosten mit — Investitions- und Betriebskosten werden Vorgabe im Katalogsatz. Belegter Name: Vorbelegung mit dem Zusatz „(Projekt)“, in der Rückfrage änderbar; ein doppelter Name wird nie gespeichert. | 08.10.2026 | — |
| **KA‑E‑10** | **Kennfarben** als Rahmenfarbe der Bereiche und Marke im Kopf: Projekt grün, Katalog blau, gewählter Satz orange. Die Knöpfe gehören über ihre Stellung zum Bereich, ohne Farbstreifen. | 08.10.2026 | KA‑Q8 |
| **KA‑E‑11** | **Breite Projektsätze** (PV-Stränge und Wechselrichter, Wärmepumpen-Anlage, Gebäude) öffnen als Überlagerung im selben Fenster. | 08.10.2026 | KA‑Q9 |
| **KA‑E‑12** | **Kostenknöpfe nur beim gewählten Projektsatz:** Investitions-, Betriebs- und Energiekosten stehen in der Detailzeile des Projektsatzes; die Kostenverwaltung ohne Einengung auf einen Satz bleibt über das Menü erreichbar. | 09.10.2026 | — |
| **KA‑E‑13** | **Ein einzelner ungesperrter Katalogsatz öffnet weiter den vollen Katalogeditor** (Kennlinie, Speichern unter); die Satzbearbeitung gilt für mehrere Sätze, gesperrte Sätze und die Projektkopie — auch in Stufe 3. | 09.10.2026 | — |
| **KA‑E‑14** | **Investitionspositionen gehen mit:** Der Rückweg nimmt neben den Betriebs- auch die Investitionspositionen der Anlage in die Satzvorlage des Katalogsatzes; der Planwert reist weiter als Spalte. Ein zweiter Rückweg ersetzt die Positionen der eigenen, ungesperrten Vorlage vollständig (Betrieb und Investition). Bei der Übernahme Katalog → Projekt gilt der Vorrang der Satzvorlage für Investitionspositionen wie für Betriebspositionen: an einer Anlage ohne Positionen alle, danach nur Pflichtpositionen. | 09.10.2026 | — |
| **KA‑E‑15** | **Der Name der Projektkopie bleibt** nach „neu“ unverändert, auch wenn der Katalogsatz einen Zusatz „(Projekt)“ bekommt. | 09.10.2026 | — |
| **KA‑E‑16** | **Satzvorlage beim Löschen mitlöschen:** Wird ein Katalogsatz gelöscht, dessen Kostenvorlage nicht als Standard markiert ist, geht sie mit, wenn kein anderer Katalogsatz auf sie verweist; sonst bleibt sie. Brauchen Kostenpositionen in Projekten sie als Herkunft, bleibt sie ebenfalls, und die Meldung nennt es. | 09.10.2026 | — |

## 4 Zielbild V1

### 4.1 Aufbau

```
┌ Kopf: Titel · Kontext ······························· i  KI  ✕ ┐
│┌[IM PROJEKT] 2 Module · Summe 195 kWth                          ┐│
││        Bearbeiten… · In die Datenbank übernehmen… · ▼ Entfernen ││
││ ☐ │ Projektliste (rollt allein)                                 ││
│└────────────────────────────═══ Trennlinie (ziehbar) ═══─────────┘│
│┌[KATALOG (DATENBANK)] Suche · 20 von 20 · Filter zurücksetzen    ┐│
││                                       ▲ In das Projekt übernehmen ││
││ ☐ │ Katalogliste (rollt allein, Rest der Höhe)                   ││
││ BHKW 70 kW: Vergleichen… · Schloss… · Löschen · Bearbeiten…  Neu… ││
│└─────────────────────────────────────────────────────────────────┘│
│┌▸[KATALOGSATZ | PROJEKTSATZ] Name · Kenndaten in einer Zeile  Details┐│
│└─────────────────────────────────────────────────────────────────┘│
└ Abbrechen · OK ──────────────────────────────────────────────────┘
```

- **Raster:** `.epos-dialog` teilt das Fenster in Kopf, Inhalt und Fuß
  (`grid-template-rows: auto minmax(0, 1fr) auto`); der Inhalt teilt sich in Projektbereich, Trennlinie,
  Katalogbereich und Detailzeile (`auto 8px minmax(…, 1fr) auto`). Der Dialogkörper und das Dokument rollen
  nicht; die Haftregel „Dialog im eigenen Fenster“ ist für diese Dialoge gegenstandslos.
- **Rollbereiche:** genau drei, keiner im anderen — Projektliste, Katalogliste und die aufgeklappte
  Detailzeile. Die Kopfzeilen der Listen haften in ihrer Liste.
- **Rahmen und Marke:** jeder Bereich ist gerahmt; die obere Rahmenkante und die Marke in der Kopfleiste
  tragen die Kennfarbe (KA‑E‑10: Projekt `--epos-schema-versorgung`, Katalog `--epos-quelle-rahmen`, Satz
  `--epos-senke-rahmen`). Knöpfe tragen keine Kennfarbe; ihr Ort sagt, worauf sie wirken.
- **Ein Baustein** (KA‑E‑2): der Umbau sitzt in `Zweispaltenauswahl` (Bereiche, Trennlinie, Detailzeile,
  Mehrfachwahl, Tastatur); die Wirte reichen nur ihre Spalten, Knöpfe und Satzinhalte als Abschnitte hinein.

### 4.2 Zuordnung der Knöpfe

Jede Funktion aus dem Inventar (Abschnitt 2) hat genau einen Ort. **P** = Kopfleiste „Im Projekt“,
**K** = Kopfleiste oder Fußleiste des Katalogs, **S** = Detailzeile „gewählter Satz“, **D** = Kopf oder Fuß
des Dialogs, **Ü** = Überlagerung im selben Fenster.

| Funktion | Ort | Wirkt auf |
|---|---|---|
| Summe der Projektkomponenten, Modulzahl, Jahressumme | P (Kopf, links) | Projektliste |
| Bearbeiten… (Projektkopie, KA‑E‑8) | P | Auswahl der Projektliste |
| In die Datenbank übernehmen… (KA‑E‑9) | P | Auswahl der Projektliste → Katalog |
| ▼ Aus dem Projekt entfernen | P | Auswahl der Projektliste |
| Umstellen (Gerät ersetzen) | P | gewählte Projektzeile (Wärmepumpe) |
| Suche, Trefferzahl, Filter zurücksetzen | K (Kopf, links) | Katalogliste |
| ▲ In das Projekt übernehmen (auch Doppelklick, Enter) | K (Kopf, rechts) | Auswahl des Katalogs → Projekt |
| Spalten sortieren und filtern, Schloss- und „im Projekt“-Spalte | K (Listenkopf) | Katalogliste |
| Vergleichen…, Schloss setzen/aufheben…, Löschen, Bearbeiten… (Katalogsatz) | K (Fuß, nach dem gewählten Namen) | Auswahl des Katalogs |
| Neu…, Import…, Speichern unter… | K (Fuß, rechts) | Katalog |
| Baustoffzuordnungen… (Gebäude), Typ ändern… / DB ändern… (Bedarfsprofile) | K (Fuß) | Katalog |
| Kenndaten des gewählten Satzes (eine Zeile, Marke „Projektsatz“/„Katalogsatz“) | S (zugeklappt) | zuletzt gewählte Zeile |
| Alle Daten (bearbeitbar; gesperrter Katalogsatz nur lesen) · Speichern | S (aufgeklappt) | Projektkopie bzw. Katalogsatz |
| Investitions-, Betriebs-, Energiekosten… | S (aufgeklappt, nur Projektsatz) | Kosten der Projektzeile |
| Energieträger, Grenzleistung, Vorlauf, Rücklauf, Senken | S (aufgeklappt, nur Projektsatz) | Anlagenzeile |
| Stückzahl, Neigung, Azimut, Albedo, Modellfelder | S (aufgeklappt, nur Projektsatz) | Anlagenzeile |
| Auslegen… (Puffer), Simulation…, Export, Neu lesen | S (aufgeklappt, nur Projektsatz) | Projektsatz |
| Ganglinie mit Kennzahlen | S (aufgeklappt) | gewählte Zeitreihe |
| Stränge und Wechselrichter…, Anlage… (Wärmepumpe), Gebäudedaten… | S → Ü (KA‑E‑11) | Projektsatz |
| Grundlagen-, Berechnungshilfe (i) | S | gewählter Satz |
| Hilfe (i), KI, ✕ · Abbrechen, OK | D | Dialog |
| Verwaltungsbetriebsart (Gebäude, nur Katalog) | Projektbereich und Trennlinie entfallen | — |

Bei Mehrfachauswahl nennt die Fußleiste des Katalogs statt des Namens die Zahl („3 Sätze:“); Knöpfe, die
nur einen Satz vertragen (Vergleichen braucht zwei oder mehr, Umstellen genau einen), sind mit Hinweis
ausgegraut. Ein Satzname als erstes Wort der Leiste folgt dem Haus-Muster der Auswahlleiste.

### 4.3 Trennlinie

- Zwischen Projekt- und Katalogbereich liegt eine 8 px hohe Trennlinie mit Griffmarke (`role="separator"`,
  waagrecht, fokussierbar). Ziehen ändert die Höhe der **Projektliste**; der Katalog nimmt den Rest.
- **Grenzen:** oben Kopfzeile plus eine Projektzeile; unten behält die Katalogliste Kopfzeile plus zwei
  Zeilen. Wird das Fenster kleiner oder klappt die Detailzeile auf, klemmt der Baustein die gemerkte Höhe
  auf das, was passt — es entsteht nie ein Überlauf, der den Dialogkörper rollen ließe.
- **Vorgabe:** Kopfzeile plus zwei Projektzeilen; Doppelklick auf die Trennlinie stellt sie wieder her.
- **Tastatur:** Pfeil hoch/runter um eine Zeile, Pos1/Ende an die Grenzen.
- **Merken (KA‑E‑3):** nach dem Loslassen je Dialog über `Dienste.Einstellungen`
  (`IEinstellungen.LiesZahl`/`SchreibZahl`, Schlüssel je Dialog, etwa `Katalogauswahl.Trenner.BHKW`) in
  Pixeln der Projektliste. Ohne Ablage (iOS-Adapter leer, Prüfmodus) gilt die Vorgabe. Speicherort siehe
  Abschnitt 7, Punkt 4.

### 4.4 Detailzeile „gewählter Satz“

- **Zugeklappt** (Vorgabe beim Öffnen, KA‑E‑4): eine Zeile mit Pfeil, Marke „Projektsatz“ oder
  „Katalogsatz“, Name und den wichtigsten Kenndaten. Sie zeigt den zuletzt gewählten Satz, gleich in
  welchem Bereich.
- **Aufgeklappt:** die Zeile wird zur Satzfläche und hat Vorrang: Projektliste und Katalog stehen auf ihren
  Untergrenzen (Projektliste Kopf und eine Zeile, Katalog Kopf und zwei Zeilen samt Kopf- und Fußleiste), die
  Satzfläche nimmt die gesamte übrige Höhe; beide Listen bleiben bedienbar. Inhalt: „Alle Daten“ (bearbeitbar, beim gesperrten Katalogsatz nur lesen
  mit „Erst Schloss aufheben“), beim Projektsatz die Kostenknöpfe und die projektbezogenen Felder
  (Abschnitt 4.2).
- Der Zustand auf/zu wird nicht gemerkt; die Satzfläche des BHKW startet damit nicht mehr aufgeklappt
  (heute `_parameterOffen = true`).

**Präzisiert in DZ1 (gemessen 10.10.2026, Rollbereichprobe):** Aufgeklappt steht die Projektliste auf ihrer
Untergrenze (85 px, Kompaktstufe 74 px), die Katalogliste auf Kopf und zwei Zeilen (147 bzw. 161 px, Kompaktstufe
128 bzw. 140 px); die Satzfläche reicht bis an den unteren Rand des Bausteins. Ihr Inhalt füllt sie — eine Ganglinie
zeichnet sich in dieser Höhe (Abschnitt 4.9) —, und nur was trotzdem nicht passt („Alle Daten“), rollt in der
Satzfläche; sie ist dann der einzige Rollbereich. Zugeklappt gilt die Aufteilung über die Trennlinie (4.3)
unverändert. Gemessene Satzfläche mit Kosten und „Alle Daten“ (Heizkessel): 165 px in 1 280 × 800, 172 px in
1 280 × 720, 152 px in 1 024 × 700, 220 px in 1 024 × 768, 476 px in 768 × 1 024, 97 px in 1 093 × 614.
Trägt die Satzfläche eine Ganglinie, ist ihre Untergrenze der Kopf plus 150 px Kurve (DZ1‑N2, Maße in 4.9).

### 4.5 Mehrfachauswahl

- Beide Listen tragen eine Kästchenspalte; Klick wählt eine Zeile, Strg+Klick schaltet, Umschalt+Klick
  wählt einen Bereich (KA‑E‑5). Das Kopfhäkchen wählt alle Zeilen der **gefilterten** Liste; ein Filter
  hebt die Wahl verborgener Zeilen nicht stillschweigend auf, die Fußleiste nennt „3 gewählt, 1 verborgen“.
- **Bereichsaktionen wirken auf die Auswahl:** Übernehmen, Entfernen, Bearbeiten…, In die Datenbank
  übernehmen…, Schloss, Löschen. Die Rückfrage nennt die Zahl und bei Löschen und Schloss die Namen.
- **Sammelübernahme** (KA‑E‑6): Doppelklick und Enter übernehmen die Zeile bzw. die Auswahl. Die
  Trägerwahl fragt je Brennstoff einmal (AD-Q4). Die Detailzeile zeigt die zuletzt angeklickte Zeile.
- **Die Wahl folgt der Übernahme** (DZ1‑N2): Wählt der Wirt nach Übernehmen, Umstellen oder Neu… die neue
  Projektzeile, steht auch die Mehrfachwahl auf genau dieser Zeile (bei einer Sammelübernahme auf den neuen
  Zeilen); die vorher angekreuzten Kästchen sind abgewählt. Eine Zeile, die aus der Liste verschwindet, verlässt
  die Wahl. Ist nichts angekreuzt, bleibt es so — dann wirken die Aktionen auf die Einzelwahl. „Aus dem Projekt
  entfernen“ trifft damit nach einer Übernahme die eben übernommene Zeile. Geregelt an einer Stelle im Baustein
  (`Bereichswahl.ListeFolgen`, gerufen von der `Zweispaltenauswahl` bei jedem Parametersatz über die
  ungefilterte Projektliste) für alle Wirte; gehalten von bunit-Fällen und der Katalogprobe.

### 4.6 Bearbeiten und Mehrfach-Bearbeiten

- **Bearbeiten…** öffnet den Satzeditor als Überlagerung; im Projektbereich für die Projektkopie, im
  Katalogbereich für den Katalogsatz (KA‑E‑8). Die Marke im Kopf der Überlagerung sagt, was gespeichert
  wird.
- **Mehrere gewählte Sätze:** eine Blätterleiste „‹ 1 von n ›“ wechselt den Satz; je Feld setzt „für alle
  gewählten setzen“ den Wert in allen Sätzen der Auswahl. Gesperrte Katalogsätze werden übersprungen und
  in einer Hinweiszeile genannt; Speichern schreibt alle geänderten Sätze in einer Transaktion.
- Ein gesperrter Katalogsatz allein öffnet nur lesend mit „Erst Schloss aufheben“.
- Ein einzelner ungesperrter Katalogsatz öffnet den vollen Katalogeditor, nicht die Satzbearbeitung (KA‑E‑13).
- Die Kostenknöpfe stehen nur beim gewählten Projektsatz; die Kostenverwaltung ohne Einengung bleibt im Menü (KA‑E‑12).

### 4.7 Tastatur

- Tab-Folge: Dialogkopf → Projekt-Kopfleiste → Projektliste → Trennlinie → Katalog-Kopfleiste (Suche,
  Übernehmen) → Katalogliste → Katalog-Fußleiste → Detailzeile → Dialogfuß. Jede Liste ist **ein**
  Tabulatorhalt; in der Liste wandern Pfeiltasten, Leertaste schaltet die Wahl, Strg+A wählt die gefilterte
  Liste.
- **Enter** in der Katalogliste übernimmt (KA‑E‑6), in der Projektliste öffnet es die Detailzeile;
  **Entf** in der Projektliste entfernt nach Rückfrage. **Esc** schließt zuerst eine Überlagerung, dann die
  Detailzeile, dann den Dialog (Abbrechen).

### 4.8 Kleine Fenster

Gemessen im Mockup (BHKW, Projekt mit zwei Modulen, Detailzeile zu, Vorgabe der Trennlinie):

| Fenster | Projektliste | Katalogbereich | Katalogzeilen à 46 px | Trennlinie ganz oben |
|---|---|---|---|---|
| 1 280 × 800 | 127 px (2 Zeilen) | 399 px | rund 5,5 | rund 6,5 |
| 1 280 × 720 | 127 px | 319 px | rund 3,8 | rund 4,8 |
| 1 024 × 700 | 127 px | 299 px | rund 3,4 | rund 4,4 |

In keinem der drei Fenster und in keiner Stellung der Trennlinie (oben, unten, Tastatur, nach Neuladen,
Fenster verkleinert, Detailzeile auf) liegt ein Rollbereich in einem anderen; das Dokument rollt nicht
(Playwright, 48 Zustände). Bei 1 024 px Breite fallen in der Katalogliste die Spalten mit Rang 2 wie heute
über die Spaltenregel der `Katalogliste`; die Kopfleisten dürfen nicht umbrechen — das hält die neue
Probe (Abschnitt 8).

**Präzisiert in Stufe 2 (Heizkessel, gemessen 09.10.2026, Rollbereichprobe):** Die Suchzeile der
Katalogliste steht in der Kopfleiste des Katalogs (im Baustein, für alle Wirte), die Kontextzeile im
Dialogkopf (Heizkessel; übrige Wirte mit Stufe 3 und 4). Die Katalogliste des Heizkessels führt den
Kästchenmodus mit 46 px je Zeile; der Wirt gibt dem Baustein dieses Zeilenmaß (`KatalogZeile`), und der
Baustein hält dann Kopf und **zwei** Zeilen (53 + 2 × 46 + 2 = 147 px) auch unter 600 px Bausteinhöhe —
nicht eine Zeile als Untergrenze. Vorgabe der Trennlinie: Katalogliste 259 / 179 / 159 px in
1 280 × 800 / 1 280 × 720 / 1 024 × 700 (vorher 178 / 108 / 108); Trennlinie unten und Detailzeile auf
klemmen auf 147 px. Projektliste unverändert: Zeile 53 px, Untergrenze 85 px, Vorgabe 138 px.

**Präzisiert in KB1 (gemessen 10.10.2026, Rollbereichprobe, Anwenderentscheid vom selben Tag):** Auf kleinen
Bildschirmen gilt eine **Kompaktstufe** — unter dem Normalmaß 1 280 × 800, also unter 1 280 px Breite oder 800 px
Höhe (iPad quer und hoch, auch iPad 11 Zoll mit 1 180 bis 1 210 px quer, Laptop bei 125 %, auch 1 280 × 720 und
1 024 × 700) werden Dialograhmen und Baustein rund ein Achtel kleiner: Schrift
13 → 12 px, Kartentitel 16 → 14 px, Knöpfe, Kopfleisten und Detailzeile 44 → 37 px, Projektzeile 53 → 46 px,
Katalogzeile 46 → 40 px (Kästchenmodus) bzw. 53 → 46 px, Untergrenze der Projektliste 85 → 74 px, Vorgabe
138 → 120 px, Katalogliste Kopf und zwei Zeilen 147 → 128 px bzw. 161 → 140 px. Es ist eine Skalenebene über die
Token des Hauses; die Maße im Programm (Trennlinie, gemerkte Höhe, `KatalogZeile`) bleiben Pixel der
Normalstufe. Die Klemme auf eine Katalogzeile unter 600 px Bausteinhöhe gilt nur noch in der Normalstufe.
**Ausnahme von der Rollbereichregel, nur unterhalb der Mindesthöhe:** Reicht das Fenster nicht für Dialogkopf,
Kontextzeile, Projektrahmen mit Untergrenze, Trennlinie, Katalograhmen mit Kopf, zwei Zeilen und Knopfleiste,
Detailzeile und Schlussleiste, steht der Baustein auf seiner gemessenen Mindesthöhe, und der Dialogkörper rollt
senkrecht; die Schlussleiste rollt mit (Kopf und Schlussleiste stehen statisch, wie es die Fensterprobe misst).
Darüber rollt der Dialogkörper nie. Die aufgeklappte Detailzeile hält mindestens 80 px ihres Inhalts (mit
Ganglinie Kopf und 150 px Kurve, 4.9) und hat Vorrang (DZ1, 4.4): Projekt- und Katalogliste stehen auf ihrer Untergrenze, die
Satzfläche nimmt die gesamte übrige Höhe ohne Obergrenze; was trotzdem nicht passt, rollt allein in der
Satzfläche. Gemessen in sechs Fenstern (1 280 × 800, 1 280 × 720, 1 024 × 700, 1 024 × 768,
768 × 1 024, 1 093 × 614), 618 Zustände ohne Verstoß; der Dialogkörper rollt allein in 1 093 × 614 mit
aufgeklappter Detailzeile. Ohne gewählten Satz bleibt die Satzfläche des Gebäudedialogs leer. Die Fensterhöhe zählt
ohne die sicheren Abstände des Geräts (Statusleiste und Home-Anzeige des iPads; Token `--epos-sicher-oben` und
`--epos-sicher-unten` aus `env(safe-area-inset-*)`, unter Windows 0): Wurzel, Überlagerung und jede Regel mit der
vollen Fensterhöhe halten sie frei, gemessen zusätzlich in den iPad-11-Zoll-Fenstern 1 180 × 820, 1 194 × 834,
1 210 × 834 und 834 × 1 194 mit 24 px oben und 20 px unten.

### 4.9 Zuschnitt je Dialog (Randnotiz)

| Dialog | Projekt-Kopfleiste | Katalog-Fußleiste | Detailzeile aufgeklappt | Überlagerung |
|---|---|---|---|---|
| BHKW, Heizkessel | Summe kWth | Vergleichen, Schloss, Löschen, Bearbeiten · Neu | Alle Daten; Projektsatz: Kosten, Träger, Grenzleistung, VL/RL, Senken | — |
| Pufferspeicher | Summe Volumen, Auslegen… | dieselben ohne Neu | Alle Daten; Projektsatz: Kosten, Auslegen… | — |
| Stromspeicher | Summe kWh | dieselben ohne Neu | Alle Daten; Projektsatz: Kosten, Träger | — |
| Photovoltaik | Summe kWp | dieselben ohne Neu | Alle Daten; Projektsatz: Kosten, Stückzahl, Neigung, Azimut, Albedo | Stränge und Wechselrichter… |
| Solarkollektoren | Summe Module | dieselben · Neu | Alle Daten; Projektsatz: Kosten, Stückzahl, Neigung, Azimut, Solarkreis | — |
| Wärmepumpe | Umstellen | Schloss, Löschen, Bearbeiten | Kenndaten; Projektsatz: Kosten | Anlage… (`WaermepumpeAnlageDialog`) |
| Gebäude | — | Neu, Import, Baustoffzuordnungen, Schloss, Löschen | Kenndaten; Projektsatz: Wohnfläche, Ausrichtung, Simulation, Export | Gebäudedaten… |
| Bedarfsprofile | Jahressumme | Neu, Typ ändern, DB ändern, Schloss, Löschen | Verbrauch, Temperatur, Kalender | — |
| Wärmebedarf extern, Strom-, Solarthermie-, PV-Ganglinie | — | Speichern unter, Schloss, Löschen, Bearbeiten (Solarthermie und PV: Vergleichen, Schloss, Löschen, Import) | Ganglinie mit Kennzahlen; Solarthermie und PV davor Name, Beschreibung (PV: Raster, Jahresarbeit, Nennleistung) | — |
| Kältemaschine (Katalogauswahl) | — | Schloss, Löschen, Bearbeiten | Kenndaten | die Anlage bleibt im Anlagendialog |

**Präzisiert in DZ1 (Anwenderentscheid 10.10.2026: „Alle Dialoge mit CSV-Import … sollen unter ‚Gewählter
Satz‘ analog zum Dialog Wärmebedarf extern den Lastgang darstellen“):** Alle vier Ganglinien-Dialoge übergeben der
Detailzeile Marke und Namen des gewählten Satzes (`SatzArt`, `SatzName`) und zeigen aufgeklappt denselben Baustein
`GanglinienGrafik` — Jahresarbeit, Spitzenlast, Vollbenutzungsstunden, Schalter „sortiert“ und Einheit, darunter
die Jahresganglinie. Kennzahlen und Zeichenmodell rechnet der Kern (`GanglinienAuswertungCtrl` mit
`GanglinienQuelle.Solarganglinie` bzw. `PvGanglinie`, `ChartRenderer.GanglinieNormiertModell`); die Gaben baut
`GanglinienGrafikGaben` in `EPOS.UI.Daten`. Eine eben aufgenommene Projektzeile trägt noch keine Projektkopie und
zeigt den Katalogsatz gleichen Namens. Die Zeichenfläche hält das Seitenverhältnis des Zeichenmodells
(`--epos-bild-verhaeltnis`), ihre Höhe kommt aus dem Behälter, auch beim Vergrößern des Fensters und beim Auf- und
Zuklappen; das Bild bekommt keinen eigenen Rollbalken.

**Präzisiert in DZ1-N1 (verdichteter Kopf, gemessen 10.10.2026, Rollbereichprobe):** Der Kopf der Satzfläche hat
höchstens zwei schmale Zeilen. Die **Kopfzeile** (Parameter `Kopfzeile` der `GanglinienGrafik`) trägt Name und
Beschreibung nebeneinander, die Beschriftung vor dem Feld, die Beschreibung doppelt so breit, beide einzeilige
Lesefelder (0,7 Berührungsziel hoch, voller Text als Tooltipp); die PV-Ganglinie stellt Raster, Jahresarbeit und
Nennleistung kurz dazu. Wärmebedarf extern und Stromganglinie haben keine Kopfzeile (der Name steht in der
Detailzeile). Die **Kennzahlenzeile** trägt Kennzahlen, Zoomleiste („×1 · Bereich · 1:1“), Schalter „sortiert“,
Einheit (Beschriftung vor dem Feld) und rechts die Infoknöpfe des Wirts (Parameter `Knoepfe`; ohne Grafik bleibt
die eigene Knopfzeile). Die Zeigerzeile liegt oben rechts über dem Bild. Kopf ohne Polster: 51 px (Kompaktstufe
44 px) ohne, 84 px (72 px) mit Kopfzeile. Die Untergrenze der Satzfläche ist der Kopf plus 90 px Kurve
(`--epos-kurve-min`; Satzfläche 142 bzw. 175 px, Kompaktstufe 135 bzw. 163 px); reicht das Fenster dafür nicht,
rollt nach KB1 der Dialogkörper. Gemessene Kurve, Detailzeile auf:

| Dialog | 1 280 × 800 | 1 280 × 720 | 1 024 × 768 |
|---|---|---|---|
| Wärmebedarf extern | 93 px, Dialogkörper rollt (KB1) | 94 px, rollt (KB1) | 122 px |
| Stromganglinie | 139 px | 148 px | 196 px |
| Solarthermie-, PV-Ganglinie | 96 px | 112 px | 160 px |

Eine Kurve von 180 px ohne Rollen des Dialogkörpers gibt 1 280 × 800 nicht her: Mit Projektliste und Katalogliste
auf ihren Untergrenzen bleiben der Satzfläche 194 px (Solar, PV), 204 px (Strom) und 118 px (Wärmebedarf extern:
Kontextzeile und Knopfleiste unter dem Katalog). Mehr Kurve verlangt eine kleinere Untergrenze der Katalogliste
(Kopf und eine Zeile bei offener Ganglinie) oder ein Rollen des Dialogkörpers — offen zum Entscheid.

**Präzisiert in DZ1-N2 (Kurve in voller Breite, gemessen 10.10.2026, Rollbereichprobe):** Die Kurve nimmt die volle
Breite der Satzfläche und deren übrige Höhe. Die Zeichenfläche nimmt ihre Größe vom Behälter (`contain: size`, kein
Seitenverhältnis mehr); `DiagrammSvg` meldet ihr Maß beim Aufklappen und nach jeder Größenänderung
(`MassGeaendert`, ResizeObserver im Modul, entprellt), und `GanglinienGrafik` lässt das Zeichenmodell über
`BildauftragMass` in genau dieser Größe bauen (`ChartRenderer.GanglinieNormiertModell` mit `breite`/`hoehe`) — das
Bild steht 1:1, Achsen und Schrift sind unverzerrt. Unter 400 px Höhe steht das Modell kompakt: ohne Titel (der Name
steht darüber), Achsentitel und Legende in einer Kopfzeile, die Prozentachse unter 120 px Flächenhöhe in
50-%-Schritten. Ohne Maß bleibt das Bild 1 240 × 560 und byte-gleich (ChartProben-Messlatte unverändert). Alle vier
Ganglinien-Dialoge gehen diesen Weg. Die Untergrenze der Kurve ist 150 px (`--epos-kurve-min`; Satzfläche 202 bzw.
235 px, Kompaktstufe 195 bzw. 223 px); reicht das Fenster nicht, rollt nach KB1 der Dialogkörper. Gemessen,
Detailzeile auf (Kurve Breite × Höhe, Überhang des rollenden Dialogkörpers):

| Dialog | 1 280 × 800 | 1 280 × 720 | 1 024 × 768 | 768 × 1 024 |
|---|---|---|---|---|
| Wärmebedarf extern | 1 110 × 153 px, rollt 100 px | 1 118 × 154 px, rollt 80 px | 982 × 154 px, rollt 32 px | 726 × 377 px |
| Stromganglinie | 1 110 × 153 px, rollt 14 px | 1 118 × 154 px, rollt 6 px | 982 × 196 px | 726 × 451 px |
| Solarthermie-, PV-Ganglinie | 1 110 × 153 px, rollt 57 px | 1 118 × 154 px, rollt 42 px | 982 × 160 px | 726 × 396 px |

Die Kurve ist in allen sechs Fenstern so breit wie die Satzfläche (1 024 × 700: 982 px, 1 093 × 614: 1 051 px, je
154 px hoch, der Dialogkörper rollt dort nach KB1).

### 4.10 Spaltenwahl und Verwendungsmarke

Anwenderentscheid vom 10.10.2026 nach der Sichtabnahme der Kompaktstufe: „Die Spalte ‚im Projekt
verwendet‘ nimmt zu viel Platz weg. Das kann auch durch eine farbliche Kennzeichnung — mit einem
Hinweis — erfolgen.“ und „Die Spalten sollen wählbar sein (mehrere verfügbar, nur ausgewählte
anzeigen).“ Dazu der Befund derselben Abnahme: Die Kopfleiste des Projekts kürzte die Summe
(„Summe [kWth]: 2…“).

**Verwendungsmarke.** Eine Katalogzeile, deren Satz im Projekt verwendet ist, trägt hinter dem
Bezeichner einen Punkt in der Projektfarbe (die Farbe der Plakette „IM PROJEKT“), die Zeile ist leicht
getönt, und der Punkt nennt beim Zeigen „Im Projekt verwendet“ (`KFLT_VERWENDET_HINWEIS`, auch als
`aria-label`). Neben der Trefferzahl in der Kopfleiste des Katalogs steht die Legende „● im Projekt“
(`KFLT_VERWENDET_LEGENDE`), sobald mindestens eine Zeile verwendet ist. Das Kennzeichen ist
`Katalogfilterzeile.ImProjekt`; `Katalogverwendung.Stempeln` setzt es zusammen mit dem Wert der Spalte
`VERWENDET`, einmal je Liste aus der lebenden Projektliste (Entscheid Q12) — die Wirte stempeln wie
zuvor. Die Marke kürzt nie mit; sie steht in der Kompaktstufe und bei 768 px Breite.

**Spaltenwahl.** In der Kopfleiste des Katalogs öffnet der Knopf „Spalten…“ (in der Kompaktstufe nur
das Sinnbild, `title` „Angezeigte Spalten wählen“) eine kleine Auswahl: ein Kästchen je Spalte des
Profils, „Standard“ zum Zurücksetzen. Die Auswahl rollt nie selbst; ab sechs Spalten stehen die
Kästchen in zwei Spalten. Der Bezeichner und die Wahlspalte sind immer an und stehen nicht in der
Auswahl. Wählbar sind alle Spalten, die der Wirt definiert — Rang 1, Rang 2 und „im Projekt
verwendet“; diese ist `Katalogspalte.StandardAus` und fehlt in der Standardanzeige, Sortieren und
Filtern danach bleiben über die Wahl möglich.

- Ohne gemerkte Wahl gilt die Standardanzeige: Rang 1 immer, Rang 2 nach der Breite (4.8), ohne die
  standardmäßig abgewählten Spalten; die Stufen rechnen über die angezeigten Spalten.
- Mit gemerkter Wahl stehen genau die gewählten Spalten, unabhängig von der Breite; reicht sie nicht,
  rollt die Liste in ihrem eigenen Raster waagerecht (keine zweite Rollfläche).
- Gemerkt wird je Dialog über `Dienste.Einstellungen` als Text unter
  `Katalogauswahl.Spalten.<Dialogname>` — die Spaltenschlüssel kommagetrennt in der Folge des
  Profils; der Dialogname ist derselbe wie bei der Trennlinie (4.3). „Standard“ löscht den Schlüssel
  und schließt die Auswahl. Eine Wahl ohne jede Spalte ist eine Wahl (nur der Bezeichner), nicht der
  Standard. Unbekannte Schlüssel fallen still heraus.
- Tastatur: Knopf und Kästchen sind mit Tab erreichbar, Esc schließt die Auswahl (nicht den Dialog)
  und gibt den Fokus an den Knopf zurück; ein Klick daneben schließt sie ebenso.

**Hinweis „bei dieser Breite ausgeblendet“ (DZ1).** In der Standardanzeige stehen Spalten mit Rang 2 in
der Auswahl als angehakt, auch wenn sie bei der aktuellen Breite weichen. Neben einer solchen Spalte steht
dann leise „bei dieser Breite ausgeblendet“ (`KFLT_SPALTE_AUSGEBLENDET`, englisch „hidden at this width“).
Der Hinweis folgt derselben Breite wie die Spalte: Er trägt die Stufe der Spalte (`epos-weicht-ab-N` neben
`epos-spalte-ab-N`) und erscheint unter denselben Containerabfragen — in der Katalogauswahl misst der
Katalogbereich (`zweispaltenbereich`, seine Inhaltsbreite ist die Listenbreite), sonst die Liste selbst. Mit
gemerkter Wahl weicht keine Spalte, und es steht kein Hinweis.

**Trefferzahl (DZ1).** Die Trefferzahl in der Kopfleiste des Katalogs kürzt nie mit Auslassung: Sie steht als
Zahl („40 von 40“, `KFLT_TREFFER_KURZ`) und Hauptwort („ Sätzen“) und schrumpft nicht; fehlt Platz, gibt zuerst
das Suchfeld nach (bis 7rem), und erst in einem Katalogbereich unter 700 px Breite fällt das Hauptwort weg. Der
volle Text steht im `title`. Bei 768 px Fensterbreite steht sie ganz („40 von 40 Sätzen“, gemessen 10.10.2026).

**Summe.** In der Kopfleiste des Projekts hat die Summe Vorrang: Zahl und Einheit werden nie gekürzt
(`flex-shrink: 0`, `white-space: nowrap`); zuerst geben Überschrift und Knöpfe nach (Auslassung, unter
800 px Bereichsbreite ohne Pfeile), die Kopfleiste bleibt einzeilig.

Nachweis: `EPOS.UI.Tests/Bausteine/KataloglisteSpaltenwahlTests`, die Dialogtests der Wirte und die
KS1-Fälle von `Proben/Rasterprobe/katalogprobe.mjs` (Heizkessel und BHKW in 1 280 × 800 und
768 × 1 024, mit Gegenprobe; sie messen auch Trefferzahl und Hinweis), dazu
`EPOS.UI.Tests/Bausteine/KataloglisteTrefferUndHinweisTests`.

## 5 Rückweg Projekt → Datenbank

### 5.1 Befund im Code

**Kosten eines Katalogsatzes — zwei Orte, nur einer am Satz:**

1. **Planwerte als Spalten am Katalog.** Die Gerätetabellen führen Investitions- und teils Wartungswerte
   je Satz, im Katalog (`Tab_*_STAMM`) und in der Projektkopie gleich: BHKW `Kosten_Modul` mit den vier
   Nebenposten `Kosten_Montage`, `Kosten_Lieferung`, `Kosten_Schallschutzhaube`, `Kosten_Abgasreinigung`,
   dazu `Investition_kwel` (abgeleitet) und `Wartungskosten_kwhel`; Heizkessel `Investitionskosten`,
   `Wartungskosten`, `Wartungskosten_Einheit`; Pufferspeicher und Solarkollektoren `Investitionskosten`;
   Wärmepumpe, Photovoltaik und Kältemaschine `Modulkosten`; Stromspeicher `Modulkosten` (€/kWh),
   `Leistungskosten`, `Investition_Fix`, `Verschleisskosten`. Gelesen werden sie als Planwert und
   Vorbelegung des Kostendialogs über `TechnikPlanwertCtrl`
   (`EPOS.Kern/Controller/TechnikPlanwertCtrl.cs:8–40`, Landkarte der Felder `:404–496`).
2. **Kostenpositionen des Projekts** stehen in `Tab_ProjektWerte` je Anlage (`ID_Anlage`, Kategorie,
   Kostenart, Bemessung, Satz, Nutzungsdauer, Ersatz und Restwert). Sie entstehen aus Kostenvorlagen
   `Tab_KostenVorlage`/`Tab_KostenVorlagePosition` (`EPOS.Kern/Controller/KostenVorlagenCtrl.cs:8–25`,
   Übernahme `EPOS.Kern/Controller/KostenVorlagenUebernahmeCtrl.cs:16–36`). Eine Vorlage hängt an
   `KomponentenID` — dem Gewerk aus `Tab_KostenKomponente` (nur `ID`, `Komponente`) —, **nicht an einem
   Katalogsatz**. Betriebskostenpositionen (Wartung als Satz, Versicherung, Ersatz, Nutzungsdauer) haben
   damit heute keinen Ort am einzelnen Katalogsatz.

**Ursprung der Projektkopie.** Den Katalogsatz, aus dem eine Kopie stammt, kennen über eine ID nur vier
Gewerke: Wärmepumpe `Tab_WP.ID_Stamm` (Schemaschritt 80, `EPOS.Kern/Controller/WPCtrl.cs:877`; Rückfall
über den Bezeichner `EPOS.Kern/Controller/WPStammCtrl.cs:858`), Pufferspeicher `Tab_Pufferspeicher.ID_Stamm`
(`EPOS.Kern/Controller/PufferSpCtrl.cs:412–417`), Kältemaschine `Tab_Kaeltemaschine.ID_Stamm`
(`EPOS.Kern/Controller/KaeltemaschineStammCtrl.cs:336`) und Gebäude `Tab_Gebaeude.ID_Gebaeude_Stamm`
(Schemaschritt 121, `EPOS.Kern/Controller/GebaeudeStammCtrl.cs:649`, `:1166`). **Ohne Verweis** sind
`Tab_BHKW`, `Tab_Heizkessel`, `Tab_Stromspeicher`, `Tab_PV`, `Tab_Solarkollektoren` und die Projektkopien
der Bedarfsprofile und Zeitreihen (`Tab_Stromverbraucher`, `Tab_Brauchwasser`, `Tab_Prozesswaerme`,
`Tab_Waermebedarf`, `Tab_Stromganglinie`, `Tab_Solarganglinie`).

**Vorhandener Kernweg.** Nur das Gebäude hat einen Rückweg: „In DB übernehmen“
(`EPOS.Kern/Controller/GebaeudeStammCtrl.cs:1464`, `AusProjektUebernehmen`) legt die Projektkopie als
**neuen** Katalogsatz an — die 95 Fachspalten, die Kopie und Katalog gleich führen (`:1406`), samt Kopie der
Konditionierung; Zonen und Bauteile bleiben im Projekt. Der Namensvorschlag hängt bei Belegung einen Zähler
an („… (2)“, `NamensvorschlagAusProjekt`, `:1418`), die Absagen sind benannt (Name leer, Name vergeben).
Im Dialog sitzt er als `Leistenzusatz` (`EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor:111–117`). Einen Weg
„Ursprung überschreiben“ gibt es nirgends.

**Projektbezogene Felder.** Die Gerätekopien von BHKW, Heizkessel, Photovoltaik, Stromspeicher,
Solarkollektoren und Wärmepumpe führen genau die Spalten ihres Katalogs (zusätzlich nur `ID_Projekt`,
bei der Wärmepumpe `ID_Stamm`); der Katalog hat zusätzlich `ReadOnly`, `Katalog_Schluessel`,
`Katalog_Pruefsumme`, `Katalog_Ausgelaufen`. Die projektbezogenen Felder liegen nicht am Gerät, sondern an
der Anlagenzeile `Tab_Energieanlagen` — Energieträger `ID_Carrier` und `Kuehl_ID_Carrier`, `Grenzleistung`,
`Vorlauf`, `Vorlauf_Max`, `Neigung`, `Azimut`, `Albedo`, `Kollektormodulanzahl`, die Quellfelder `WQ_*` —,
die Senken in `Z_AnlageSenke`, die Stränge in `Z_AnlageStrang`. Ausnahme ist `Tab_BHKW.Grenzleistung`: eine
Katalogspalte (Rangfolge Anlage, Katalog, Projekt), sie geht mit. Der Pufferspeicher trägt in der Kopie 25
Spalten, die der Katalog nicht kennt (`Vorlauf`, `Ruecklauf`, `Verwendung`, die Schwellen, Schichten, Lade-
und Entladeleistung, `Nutzung_*`, `Entnahme_*`, Frischwassermodul, Aufstellraum) — sie sind projektbezogen.
Die Kataloge führen einen eindeutigen Index auf `Bezeichner` (geprüft an BHKW, Heizkessel, Photovoltaik,
Wärmepumpe, Pufferspeicher).

### 5.2 Regel des Rückwegs

1. **Knopf** „In die Datenbank übernehmen…“ in der Projekt-Kopfleiste, wirksam auf die Auswahl (KA‑E‑9).
2. **Rückfrage immer**, als Überlagerung, je gewähltem Satz eine Zeile:
   - Wahl **„als neuen Katalogsatz anlegen“** (Vorgabe) oder **„Ursprung überschreiben“**.
   - „Ursprung überschreiben“ ist ausgegraut mit Grund: „Erst Schloss aufheben“ (Ursprung gesperrt),
     „Ursprung nicht mehr vorhanden“ (Verweis zeigt ins Leere), „Ursprung nicht bekannt“ (Gewerk ohne
     Verweis, Abschnitt 7 Punkt 3).
   - **Name:** Vorbelegung mit dem Namen der Kopie; ist er im Katalog belegt, mit dem Zusatz „(Projekt)“,
     und ist auch der belegt, „(Projekt 2)“ usw. Das Feld ist änderbar; ein belegter Name sperrt „Übernehmen“
     mit Hinweis am Feld — gespeichert wird nie ein doppelter Name (der Index fängt es zusätzlich).
     Beim Überschreiben bleibt der Name des Ursprungs, das Feld ist gesperrt.
   - Bei Mehrfachauswahl gilt eine Wahl für alle mit Abweichung je Zeile; Zeilen, die nicht überschreiben
     können, fallen auf „neu“ zurück und werden genannt.
3. **Was mitgeht:** die Schnittmenge der Spalten von Kopie und Katalog ohne `ID`, `ID_Projekt`, `ID_Stamm`,
   `ReadOnly` und `Katalog_*`; dazu die Kindzeilen der technischen Daten (Abschnitt 7 Punkt 5) und die
   Kosten: die Planwertspalten unmittelbar, die Investitions- und Betriebskostenpositionen der Anlage als
   Vorgabe des Katalogsatzes (KA‑E‑14; Ort: Abschnitt 7 Punkt 3). Projektbezogene Felder der Anlagenzeile, Senken und Stränge gehen nicht mit.
4. **Danach:** Ein neuer Satz ist ungesperrt; die Projektkopie bekommt ihn als Ursprung (wo ein Verweis
   existiert), damit ein zweiter Rückweg überschreiben kann. Die Meldung nennt Zahl und Namen. Alles in
   einer Transaktion je Aufruf; scheitert ein Satz, wird nichts geschrieben.
5. **Kernweg:** ein Controller im Kern je Gewerk nach dem Muster `GebaeudeStammCtrl.AusProjektUebernehmen`
   mit benannten Absagen; der Gebäudeweg wird auf die neue Regel gezogen (Namenszusatz, Überschreiben).
   Die Oberfläche fragt nur.

### 5.3 Wie gebaut (Stufe 2, Heizkessel)

- **Ein Kernweg für alle Gewerke:** `Katalogrueckweg` (`EPOS.Kern/Allgemein/Katalog/`) mit dem Gewerk als
  Beschreibung (`Rueckweggewerk`: Kopie, Katalog, Verweis der Anlagenzeile, Kostenkomponente, Kindtabellen,
  Prüfregel) — `Vorschau` (Ursprung, Sperrgrund, Namensvorschlag) und `Uebernehmen` (ein Vorgang je Aufruf). Der
  Heizkessel reicht sein Gewerk über `HeizkesselStammCtrl.Rueckweg()`, `RueckwegVorschau`, `AusProjektUebernehmen`
  und `RueckwegNameBelegt`; Stufe 3 trägt je Gerät nur das Gewerk ein. Benannte Absagen (`Rueckwegabsage`): Kopie
  fehlt, Name leer, Name belegt (auch doppelt im selben Aufruf), Ursprung gesperrt, Ursprung fehlt, Ursprung nicht
  bekannt, Prüfverstoß (beim Kessel Kennlinie und Bereitschaftsverlust wie beim Speichern), Datenbankfehler.
- **Abweichung Knopfort:** Der Knopf steht im `Leistenzusatz` der Projektleiste unmittelbar nach „Bearbeiten…“.
- **Kindzeilen:** Der Heizkessel hat keine — Kennlinie (η₃₀, Brennwertkennlinie, Mindestleistung, Anfahrverlust,
  Mindestlaufzeit) und Bereitschaft sind Spalten am Satz und gehen mit der Schnittmenge. Der Mechanismus (bei
  „neu“ Kopie, bei „überschreiben“ Ersatz in derselben Transaktion) steht im Kernweg und ist an einem Prüfgewerk
  getestet.
- **Kosten (KA‑E‑14):** Die Planwertspalten (`Investitionskosten`, `Wartungskosten`, `Wartungskosten_Einheit`,
  `Nutzungsdauer`) gehen mit der Schnittmenge. Die **Betriebs- und Investitionspositionen** der ersten Anlage, die
  auf die Kopie zeigt, werden Satzvorlagen. Weil eine Kostenvorlage genau eine Kategorie führt, verweist der Satz mit
  **zwei IDs** auf zwei eigenständige Vorlagen (Gewerk = `KomponentenID`, Name = Satzname, nicht Standard):
  `ID_KostenVorlage` auf die Betriebs-, `ID_KostenVorlageInvestition` (Schritt 209) auf die Investitionsvorlage. Ein
  zweiter Rückweg ersetzt die Positionen der jeweils verwiesenen eigenen, ungesperrten Vorlage; eine Kategorie ohne
  Positionen setzt ihren Verweis auf leer und räumt die Vorlage ab (wie beim Löschen: sie bleibt, wenn ein anderer Satz
  oder Projektzeilen sie brauchen). Hat die Anlage gar keine Position, bleiben die Verweise, wie sie sind. Die
  Bemerkung der Vorlage ist reiner Text.
- **Vorrang bei der Übernahme:** `KostenVorlagenUebernahmeCtrl.PflichtpositionenSicherstellen` fragt je Anlage
  zuerst die Satzvorlagen (`Katalogrueckweg.SatzvorlagenDerAnlage`: Anlage → Kopie → `ID_Stamm` →
  `ID_KostenVorlage` bzw. `ID_KostenVorlageInvestition`), je Kategorie: An einer Anlage ohne Position dieser Kategorie legt die
  Satzvorlage alle Positionen an, danach wie die Standardvorlage nur die Pflichtpositionen — eine gelöschte Position
  kehrt nicht zurück. Ohne Betriebs-Satzvorlage gelten die Pflichtpositionen der Standardvorlage.
- **Löschen eines Katalogsatzes (KA‑E‑16):** `Katalogrueckweg.SatzvorlageBeimLoeschen` läuft im Vorgang des
  Löschens (beim Heizkessel `HeizkesselStammCtrl.KatalogsatzLoeschen`, Stufe 3 verdrahtet die übrigen Gewerke) und
  räumt beide Verweise ab: jede Vorlage geht mit — außer die Vorlage ist Standard oder gesperrt, ein anderer Satz in einem der acht Kataloge
  verweist mit `ID_KostenVorlage` oder `ID_KostenVorlageInvestition` auf sie, oder Projektzeilen tragen sie als Herkunft (`Tab_ProjektWerte.VorlageID`);
  im letzten Fall nennt die Meldung, dass die Vorlage bleibt. Scheitert das Mitlöschen, bleibt auch der Satz.
- **Ursprung:** `HeizkesselCtrl.CopyFromStamm` trägt `ID_Stamm` ein. Bestandskopien bleiben leer („Ursprung nicht
  bekannt“), kein Namensabgleich. Ein gelöschter Katalogsatz leert den Verweis (`ON DELETE SET NULL`); „Ursprung
  nicht mehr vorhanden“ entsteht damit nur bei einem Verweis ohne Fremdschlüsselprüfung. Im Projektpaket reisen die
  neuen Ursprungsverweise nicht (am Ziel leer), damit ein Rückweg nie einen fremden Satz überschreibt.
- **Katalogkopie und Prüfsumme:** `ID_KostenVorlage` und `ID_KostenVorlageInvestition` sind Metaspalten der Katalogfassung (keine Prüfsumme, kein
  Paket); „Duplizieren“ übernimmt sie nicht — die Kopie beginnt mit der Standardvorlage.
- **Name der Projektkopie:** bleibt nach „neu“ unverändert, auch wenn der Katalogsatz einen anderen Namen bekommt
  (KA‑E‑15).
- **Rückfrage:** `Rueckwegfrage` als Überlagerung mit der Marke „Katalogsatz“; Kopfzeile „Für alle“ bei mehreren
  Sätzen, Hinweiszeile für die Zeilen, die auf „neu“ zurückfallen; Hinweise zu Kosten und zu dem, was im Projekt
  bleibt (Energieträger, Temperaturpaar, Senken und Zeitprogramm der Anlage). Die Meldung danach (Banner) nennt
  Zahl und Namen.

### 5.4 Wie gebaut (Stufe 3, BHKW)

Das BHKW folgt dem Heizkessel (5.3); hier steht nur, was abweicht.

- **Kernweg:** `BHKWStammCtrl.Rueckweg()` trägt das Gewerk ein (Kopie `Tab_BHKW`, Katalog `Tab_BHKW_STAMM`, Anlage über
  `ID_BHKW`, Kostenkomponente 7), dazu `RueckwegVorschau`, `AusProjektUebernehmen`, `RueckwegNameBelegt` und
  `KatalogsatzLoeschen`. Prüfregel wie beim Speichern: die zwei Wirkungsgradanteile, Teillast und Takten, die
  Rücklaufgrenze. `Delete` über den Namen läuft über denselben Löschweg samt Satzvorlage. Kein neuer Schemaschritt:
  208 und 209 führen `ID_Stamm` an `Tab_BHKW` und beide Vorlagenverweise an `Tab_BHKW_STAMM`;
  `BHKWCtrl.CopyFromStamm` trägt den Ursprung ein.
- **Nebenposten:** Die Investition hat fünf Posten (`Kosten_Modul`, `Kosten_Montage`, `Kosten_Lieferung`,
  `Kosten_Schallschutzhaube`, `Kosten_Abgasreinigung`); sie stehen in „Alle Daten“ und in der Satzbearbeitung,
  `Investition_kwel` ist dort nur Anzeige und wird vom Kern beim Schreiben aus Posten und `Pel` nachgerechnet, ebenso
  der Gesamtwirkungsgrad aus seinen zwei Anteilen. Mit dem Rückweg gehen Posten, `Investition_kwel` und
  `Wartungskosten_kwhel` als Schnittmenge.
- **Grenzleistung:** Zwei Orte. Das Feld beim Projektsatz (mit Herleitung „0 = Projektvorgabe“) ist die Grenzleistung
  der Anlagenzeile und bleibt im Projekt; `Tab_BHKW.Grenzleistung` ist Spalte des Moduls und geht mit (5.1). Die
  Rückfrage nennt beides.
- **Kindzeilen:** Das BHKW hat keine technischen Kindtabellen — Teillast und Takten, Rücklaufgrenze und die
  Wirkungsgradanteile sind Spalten am Satz. Anlagenbezogen bleiben Energieträger, Grenzleistung und Temperaturpaar der
  Anlage, Senken und Zeitprogramm.
- **Neu…:** fragt zuerst den Namen und öffnet dann den Katalogeditor (`BhkwKatalogDialog` braucht ihn); der Knopf
  steht wie beim Heizkessel rechts im Katalogfuß.

### 5.5 Wie gebaut (Stufe 3, Pufferspeicher)

Der Pufferspeicher folgt dem BHKW (5.4); hier steht nur, was abweicht.

- **Kernweg:** `PufferSpStammCtrl.Rueckweg()` trägt das Gewerk ein (Kopie `Tab_Pufferspeicher`, Katalog
  `Tab_Pufferspeicher_STAMM`, Anlage über `ID_PUFFER`, Kostenkomponente 6), dazu `RueckwegVorschau`,
  `AusProjektUebernehmen`, `RueckwegNameBelegt` und `KatalogsatzLoeschen`; `Delete` über ID und Namen läuft über
  denselben Löschweg samt Satzvorlage. Prüfregel wie beim Speichern: Bereitschaftsverluste, Gesamtvolumen und
  Investitionskosten nicht negativ. Kein Schemaschritt: 208 und 209 decken beide Tabellen; `PufferSpCtrl.CopyFromStamm`
  trägt den Ursprung ein (eine schon vorhandene Kopie gleichen Namens behält ihren Verweis).
- **Volumen:** Die Projekt-Kopfleiste zeigt die Summe der Gesamtvolumina in Litern (je Zeile die Projektkopie, ohne
  Projekt der Katalogsatz). Mit dem Rückweg gehen die fünf Gerätewerte — Hersteller, Speichertyp, Bereitschaftsverluste,
  Gesamtvolumen, Investitionskosten — als Schnittmenge.
- **Projektkopie beim Übernehmen:** Wie bei Heizkessel und BHKW legt „In das Projekt übernehmen" die Kopie sofort an
  (`PufferSpCtrl.CopyFromStamm`, über den Namen idempotent); eine in der Sitzung neu entstandene Kopie merkt die
  `Projektkopievormerkung`, Abbrechen (auch „Auslegen…", Kreuz, Esc) räumt sie wieder ab, OK lässt sie stehen. Eine
  frisch aufgenommene Zeile ist damit sofort bearbeitbar — Bearbeiten…, „In die Datenbank übernehmen…" und „Alle Daten"
  wirken auf sie. Vor dem Aufnehmen steht weiter die Dublettenfrage, in der Sammelübernahme je Satz.
- **Auslegen…:** steht zweimal. In der Knopfzeile des Projektsatzes neben den Kostenknöpfen (Investition, Betrieb; kein
  Energieträger) legt er die gewählte Projektkopie aus; in der Projekt-Kopfleiste öffnet er ohne gewählte Projektzeile
  (auch bei leerer Liste) die Auslegung für einen neuen Speicher, mit gewählter Zeile dieselbe wie beim Projektsatz.
  Beide verlassen den Dialog wie Abbrechen.
- **Kindzeilen und Verwendung:** Technische Kindtabellen hat der Katalog nicht. Im Projekt bleiben die 25 Spalten, die
  nur die Kopie führt (Verwendung und Nutzung, Temperaturpaar, Schwellen, Schichtung, Lade- und Entladeleistung,
  Entnahme, Frischwassermodul, Aufstellraum), und die Kindzeilen der Anlage: Senken (`Z_AnlageSenke`), Verbünde
  (`Z_AnlagePufferVerbund`), die Lade-Prioritäten der Erzeuger, `Z_ProjektPufferSp` und `Tab_PufferAuslegung`. Die Rückfrage nennt es.
- **Ohne Neu…:** Der Katalogfuß führt kein „Neu…" (4.9), die Speicherverwaltung als Überlagerung entfällt. Neue
  Katalogsätze entstehen über die Katalogverwaltung im Menü, über „Speichern unter" im Katalogeditor oder über den
  Rückweg.

### 5.6 Wie gebaut (Stufe 3, Stromspeicher)

Der Stromspeicher folgt Heizkessel und BHKW (5.3, 5.4); hier steht nur, was abweicht.

- **Kernweg:** `StromspeicherStammCtrl.Rueckweg()` trägt das Gewerk ein (Kopie `Tab_Stromspeicher`, Katalog
  `Tab_Stromspeicher_STAMM`, Anlage über `ID_SP`, Kostenkomponente 5), dazu `RueckwegVorschau`, `AusProjektUebernehmen`,
  `RueckwegNameBelegt` und `KatalogsatzLoeschen`; `Delete` über den Namen läuft über denselben Löschweg samt Satzvorlage.
  Prüfregel wie beim Sammelspeichern (`Pruefen`): Kapazität, Leistung, Kosten, Zyklen und Standby nicht negativ,
  Ladezustand, Degradation und Selbstentladung 0 bis 100 %, Round-Trip-Wirkungsgrad 0 bis 1. Kein neuer Schemaschritt:
  208 und 209 führen `ID_Stamm` an `Tab_Stromspeicher` und beide Vorlagenverweise an `Tab_Stromspeicher_STAMM`;
  `StromspeicherCtrl.CopyFromStamm` trägt den Ursprung ein.
- **Projektkopie sofort:** Bisher entstand die Kopie erst beim Speichern des Projekts, und die Detailzeile las stets den
  Katalog. Jetzt legt „In das Projekt übernehmen" die Kopie außerhalb des Assistenten sofort an (Muster BHKW, mit
  `Projektkopievormerkung`); Varianten desselben Speichers teilen sich die Kopie gleichen Namens. Die Projektzeile liest
  ihr Detail deshalb über die Geräte-ID, nicht über ihren Namen (eine Variante heißt „… (2)“). Die Summe kWh ist die
  Kapazität der Kopie je Anlage.
- **Kosten:** Die vier Planwertspalten `Modulkosten` (€/kWh), `Leistungskosten`, `Investition_Fix`, `Verschleisskosten`
  stehen in „Alle Daten“ und in der Satzbearbeitung und gehen mit der Schnittmenge; Betriebs- und Investitionspositionen
  der Anlage werden Satzvorlagen wie beim Heizkessel.
- **Kindzeilen:** keine technischen. Die Betriebsführung (`Tab_StromspeicherVariante`) hängt an der Anlage und bleibt im
  Projekt, ebenso der Energieträger. `Standby_Verbrauch` und `Selbstentladung_Prozent_Monat` sind Gerätespalten und gehen
  mit in den Katalog; an der Kopie bleiben sie unberührt. Das Sammelspeichern lässt eine leere Gerätespalte leer, wenn sie
  als 0 zurückkommt (leer heißt im Rechenweg Fachvorgabe).
- **Katalogeditor ohne Neu…:** Den Knopf „Neu…“ gibt es im Katalogfuß nicht (4.9). „Bearbeiten…“ auf einen einzelnen
  ungesperrten Satz öffnet den Modulkatalog (`ModulKatalogDialog`, Parameter `Vorwahl`) mit diesem Satz; dort stehen
  Neu…, Duplizieren… und Löschen wie bisher hinter „Bearbeiten…“ im Modulbereich. Ein gesperrter Satz allein öffnet nur
  die lesende Satzbearbeitung — Neu… ist dann nur über einen ungesperrten Satz oder die Verwaltung im Menü erreichbar.
- **Löschen:** Der Projektdialog hatte keinen Löschknopf; jetzt steht er im Katalogfuß (Rückfrage, mehrere mit
  Überspringen der gesperrten), geschrieben über `StromspeicherStammCtrl.Loeschen`.

### 5.7 Wie gebaut (Stufe 3, Solarkollektoren)

Die Solarkollektoren folgen Heizkessel und BHKW (5.3, 5.4); hier steht nur, was abweicht.

- **Kernweg:** `SolarkollektorenStammCtrl.Rueckweg()` trägt das Gewerk ein (Kopie `Tab_Solarkollektoren`, Katalog
  `Tab_Solarkollektoren_STAMM`, Anlage über `ID_Solar`, Kostenkomponente 4), dazu `RueckwegVorschau`,
  `AusProjektUebernehmen`, `RueckwegNameBelegt` und `KatalogsatzLoeschen`; `Delete` über den Namen läuft über denselben
  Löschweg samt Satzvorlage. Prüfregel wie beim Speichern: Flächen und Investitionskosten nicht negativ, h0 zwischen 0 und
  1, k1, k2, Kdir und Kdiff nicht negativ, Bezugsfläche aus der Liste. Kein neuer Schemaschritt: 208 und 209 führen
  `ID_Stamm` an `Tab_Solarkollektoren` und beide Vorlagenverweise an `Tab_Solarkollektoren_STAMM`;
  `SolarkollektorenCtrl.CopyFromStamm` trägt den Ursprung ein.
- **Bearbeiten:** `SatzAnzeige` liest Kopie oder Katalogsatz nach ID, `AnzeigefelderSchreibenAlle` schreibt alle Sätze
  einer Mehrfachbearbeitung in einer Transaktion; der Name bleibt. Die Projektzeile liest Kenndaten und Modulfläche aus
  der Projektkopie über die Geräte-ID, weil zwei Zeilen desselben Kollektors sich die Kopie teilen.
- **Projektkopie sofort:** Die Hülle legte die Kopie schon beim Übernehmen an; das bleibt, mit `Projektkopievormerkung`.
- **Felder der Anlage:** Modulanzahl samt gerechneter Aperturfläche, Neigung, Azimut, Albedo und der Solarkreis (Pumpe,
  Verluste, Arbeitstemperatur-Weg, Grädigkeit, Spreizung) stehen beim Projektsatz in der Gruppe „Kollektor“ und gehen
  weiter erst mit „Übernehmen“ in die Anlagenzeile; die Senken stehen als Zeile darunter. Die Summe in der
  Projekt-Kopfleiste zählt die Module der Projektliste.
- **Kosten:** Nur Investitions- und Betriebskosten — Energiekosten gibt es nicht. `Investitionskosten` steht in „Alle
  Daten“ und in der Satzbearbeitung und geht mit der Schnittmenge; Betriebs- und Investitionspositionen der Anlage werden
  Satzvorlagen wie beim Heizkessel.
- **Kindzeilen:** keine. Kennwerte, Flächen und Bezugsfläche sind Spalten am Satz. Anlagenbezogen bleiben die Felder der
  Anlage und die Senken (`Z_AnlageSenke`); die Ergebnistabellen der Simulation verweisen nicht auf die Kopie.
- **Neu…:** fragt zuerst den Namen und öffnet dann den `SolarkollektorKatalogDialog` im Modus Neu; danach ist der neue
  Satz gewählt. „Bearbeiten…“ auf einen einzelnen ungesperrten Satz öffnet denselben Editor im Modus Bearbeiten.

### 5.8 Wie gebaut (Stufe 3, Wärmepumpe)

Die Wärmepumpe folgt dem BHKW (5.4); hier steht nur, was abweicht.

- **Aufbau:** Die Anlagenseite `WaermepumpeAnlageDialog` steht nicht mehr eingebettet unter den Listen, sondern als
  Überlagerung „Anlage…“ beim Projektsatz (KA‑E‑11). Sie bearbeitet die Zeile an Ort und Stelle; OK prüft und übernimmt
  sie ins Modell, Abbrechen setzt sie auf den Stand beim Öffnen zurück. Eine einzeln übernommene Zeile öffnet ihre Anlage
  sofort (Betriebsart, Temperaturen, Sperrzeit, Heizstab); das OK des Dialogs hält bei einer neuen, noch nicht bestätigten
  Zeile an und öffnet deren Anlage. Die Detailzeile zeigt die Kenndaten des Satzes und die Kennlinie (COP, Leistung) wie
  das Stammblatt, beim Projektsatz dazu die Kostenknöpfe und „Anlage…“. Die Überlagerung zeigt die Kostenknöpfe nicht noch einmal
  (`KostenleisteAnzeigen="false"`); nur das eigene Fenster der Anlagenseite behält ihre Kostenleiste.
- **Knöpfe:** Projekt-Kopfleiste „Umstellen“ (genau eine Projektzeile auf genau einen Katalogsatz; Gerät, Gerätefelder und
  Kennlinien wechseln, Betriebsdaten und Kosten der Anlage bleiben), „Bearbeiten…“, „In die Datenbank übernehmen…“,
  Entfernen. Katalogfuß Schloss, Löschen, Bearbeiten — ohne Vergleichen und ohne Neu (beides trägt der Katalogeditor).
  Ein einzelner ungesperrter Katalogsatz öffnet den Katalogeditor `WaermepumpeStammDialog` mit dem Satz vorgewählt
  (Parameter `Vorwahl`). „In Stamm übernehmen…“ der Anlagenseite entfällt in diesem Wirt; es bleibt nur im eigenen
  Fenster der Anlagenseite.
- **Satzbearbeitung:** Hersteller, Beschreibung und Modulkosten (`WPStammCtrl.SammelfelderSchreibenAlle`, eine
  Transaktion). Nennleistung, Heizstab, Kühlleistung, Typ und Regelung hängen an der Kennlinie; sie und die Kennlinien
  bearbeiten Katalogeditor bzw. Anlagenseite, nicht die Sammelbearbeitung.
- **Projektkopie:** entsteht beim Übernehmen und beim Umstellen sofort über `WPCtrl.CopyFromStamm` samt Heiz- und
  Kühlkennlinie (`Projektkopievormerkung`); die Zeile trägt danach die Id der Kopie statt der Katalog-Id. Entfernen und
  die alte Kopie eines Umstellens gehen erst mit OK, wenn keine Zeile mehr auf sie zeigt.
- **Kernweg:** `WPStammCtrl.Rueckweg()` (Kopie `Tab_WP`, Katalog `Tab_WP_STAMM`, Anlage über `ID_WP`, Kostenkomponente 1),
  `RueckwegVorschau`, `AusProjektUebernehmen`, `RueckwegNameBelegt`, `KatalogsatzLoeschen`; `Delete` über den Namen läuft
  über denselben Löschweg. Prüfregel: Nennleistung und Modulkosten nicht negativ. Kein neuer Schemaschritt: Schritt 80
  führt `Tab_WP.ID_Stamm`, 208 und 209 die beiden Vorlagenverweise an `Tab_WP_STAMM`.
- **Kindzeilen:** die ersten zwei technischen Kindtabellen des Kernwegs — Heizkennlinie `Tab_Kenndaten` →
  `Tab_Kenndaten_STAMM` und Kühlkennlinie `Tab_Kenndaten_Kuehlung` → `Tab_Kenndaten_Kuehlung_STAMM`, je über `ID_WP` mit
  allen Vorlauf-Stützstellen; bei „überschreiben“ ersetzen sie die des Ursprungs. Löschen eines Katalogsatzes löscht seine
  beiden Kennlinien im selben Vorgang. Mit der Schnittmenge gehen Kühlkonfiguration, Taktwerte und die acht Gerätespalten
  der Übergabegrenze. Anlagenbezogen bleiben Betriebsart, Temperaturen, Bivalenz, Heizstab, Sperrzeiten und
  Zeitprogramm, die Quellfelder `WQ_*`, Einbindung, Vorwärmbetrieb, `Vorlauf_Max` und die Senken; die Rückfrage nennt es.

### 5.9 Wie gebaut (Stufe 3, Photovoltaik)

Die Photovoltaik folgt Heizkessel, BHKW und Stromspeicher (5.3, 5.4, 5.6); hier steht nur, was abweicht.

- **Kernweg:** `PhotovoltaikStammCtrl.Rueckweg()` trägt das Gewerk ein (Kopie `Tab_PV`, Katalog `Tab_PV_STAMM`, Anlage über
  `ID_PV`, Kostenkomponente 3), dazu `RueckwegVorschau`, `AusProjektUebernehmen`, `RueckwegNameBelegt` und
  `KatalogsatzLoeschen`; `Delete` über ID und Namen läuft über denselben Löschweg samt Satzvorlage. Prüfregel wie beim
  Sammelspeichern (`Pruefen`): Leistung, Spannungen, Ströme, Abmessungen, Modulkosten und NOCT nicht negativ, Wirkungsgrad
  0 bis 100 %; der Temperaturkoeffizient der Leistung ist frei. Kein neuer Schemaschritt: 208 und 209 führen `ID_Stamm` an
  `Tab_PV` und beide Vorlagenverweise an `Tab_PV_STAMM`; `PhotovoltaikCtrl.CopyFromStamm` trägt den Ursprung ein.
- **Projektkopie sofort:** Bisher legte die Dialogzeile die STAMM-Id ab, die Kopie entstand erst beim Speichern des Projekts
  (`WizardCtrl`). Jetzt legt „In das Projekt übernehmen" die Kopie außerhalb des Assistenten sofort an (Muster BHKW, mit
  `Projektkopievormerkung`); Felder desselben Moduls teilen sich die Kopie gleichen Namens. Detail, kWp, Ampel,
  Wechselrichtervorschlag und Auslegungstemperatur lesen die Kopie über die Geräte-ID — der Name der Anlage darf vom Modul
  abweichen. Die Strangtabelle bietet weiter Katalog-Ids an; das Band von der Zeile zum Katalog ist der Name des Moduls
  (`Modulname`). Die Summe kWp ist Anzahl Module mal Modulleistung der Kopie je Anlage.
- **Projektsatz:** Kostenknöpfe, Anzahl Module, Neigung, Azimut, Energieträger und das Ertragsmodell (`PvModellFelder`
  samt Albedo) stehen in der Detailzeile; „Stränge und Wechselrichter…" öffnet die Strangtabelle (`PvStraengeFelder`, in der
  Sache unverändert) als Überlagerung mit der Hilfe zum Wechselrichter (KA‑E‑11). Die Auslegungstemperaturen hält der
  Dialog über das Schließen der Überlagerung hinweg.
- **Kosten und Kindzeilen:** `Modulkosten` steht in „Alle Daten" und in der Satzbearbeitung und geht mit der Schnittmenge;
  Betriebs- und Investitionspositionen der Anlage werden Satzvorlagen. Die Modulkoeffizienten `alpha_SC`, `beta_OC`,
  `gamma_PMP`, `T_NOCT` sind Spalten des Satzes und gehen mit (Punkt 7.5); `alpha_SC` und `beta_OC` führt die Feldliste des
  Modulkatalogs nicht, sie stehen als Lesewerte da und das Sammelspeichern lässt sie stehen. Die Stränge (`Z_AnlageStrang`)
  samt Wechselrichterzuordnung, Neigung, Azimut, Anzahl, Ertragsmodell und Energieträger hängen an der Anlage und bleiben im
  Projekt.
- **Katalogeditor ohne Neu…:** wie beim Stromspeicher — „Bearbeiten…" auf einen einzelnen ungesperrten Satz öffnet den
  Modulkatalog (`ModulKatalogDialog`, Parameter `Vorwahl`) mit Neu…, Duplizieren…, Löschen und Import…; der bisherige Knopf
  „Modul Bearbeiten…" öffnete den Modulkatalog ohne Vorwahl auch ohne gewählten Satz. Ist nur ein gesperrter Satz
  gewählt, ist Neu… allein über einen ungesperrten Satz oder die Verwaltung im Menü erreichbar.

## 6 Abwägung: die fünf Varianten

Zur Wahl standen fünf Varianten mit gemeinsamen Regeln — der Dialogkörper rollt nicht, jeder Bereich trägt
seine Knöpfe selbst und ist gerahmt und beschriftet, Kosten gehören zum Projektsatz, Bearbeiten… und Alle
Daten zum jeweiligen Satz. Alle fünf Mockups sind spielbar und mit Playwright geprüft (kein Rollbereich im
Rollbereich).

- **V1 „Gerahmt und gestapelt“** — dieselbe Reihenfolge wie heute in drei Rahmen; gewählt (Abschnitt 4).
- **V2 „Nebeneinander (Transferliste)“** — Katalog links, Projekt rechts, Pfeilknöpfe dazwischen, Satzband
  darunter. Richtung der Übernahme unmissverständlich; der Katalog verliert aber rund 520 px Breite (Spalten
  mit Rang 2 fallen, Entscheid W14a-E-10-Q2), und das Band ist für PV-Stränge und die Wärmepumpen-Anlage zu
  niedrig.
- **V3 „Projekt im Mittelpunkt, Katalog als Schublade“** — Projekt mit voller Breite, Katalog als Schublade
  von rechts. Schärfste Trennung und viel Platz für breite Projektsätze; ein Klick mehr je Übernahme, das
  Projekt ist verdeckt, Fokusfalle und stufenweises Esc in einem neuen Baustein.
- **V4 „Reiter Projekt | Katalog mit Detailspalte“** — je Reiter eine Liste in voller Höhe, rechts eine feste
  Detailspalte. Längste Liste bei 720 px; Projekt und Katalog aber nie zugleich sichtbar.
- **V5 „Eine Liste mit Zeilenaktionen“** — eine gemeinsame Liste, Projektgruppe angeheftet, Symbole je
  Zeile. Ein Klick ins Projekt; Symbole ohne Wort, viele Tabulatorhalte, Mehrfachwahl passt schlecht,
  größter Umbau an `Katalogliste`.

| Kriterium | V1 gestapelt | V2 Transfer | V3 Schublade | V4 Reiter | V5 eine Liste |
|---|---|---|---|---|---|
| Klarheit der Zuordnung | gut | sehr gut | sehr gut | sehr gut | mittel (Symbole) |
| Katalogzeilen bei 720 / 800 px | 3,8 / 5,5 (Trennlinie oben: 4,8 / 6,5) | 4,8 / 5,9 | 7,8 / 9,5 | 8,3 / 10 | 7 / 8,8 |
| Projekt und Katalog zugleich sichtbar | ja | ja | nein (Schublade) | nein (Reiter) | ja |
| Klicks „Modul ins Projekt“ | 1 (Doppelklick) bis 2 | 1 bis 2 | 3–4 | 2–3 | 1 |
| Umbauaufwand (Tage, Baustein und zwölf Wirte) | 3–4, mit Trennlinie, Mehrfachwahl und Rückweg mehr (Abschnitt 8) | 6–8 | 8–10 | 7–9 | 10–12 |
| Risiko Tests und Proben | gering | mittel | hoch | mittel | hoch |

**Damalige Empfehlung und Wahl.** Der Entwurf empfahl V4 — meiste Listenhöhe bei 1 280 × 720 und dasselbe
Schema wie die Verwaltungen (Liste, Auswahlleiste, Stammblatt). Der Anwender hat nach dem Ausprobieren V1
gewählt: Projekt und Katalog bleiben zugleich sichtbar, die Reihenfolge ist die vertraute, der Umbau der
kleinste. Den Platznachteil von V1 bei kleinen Fenstern gleicht die ziehbare, gemerkte Trennlinie (KA‑E‑3)
aus; breite Projektsätze, für die V1 keine Fläche hat, öffnen als Überlagerung (KA‑E‑11).

## 7 Offene Punkte für die Umsetzung

| Nr | Punkt | Empfehlung |
|---|---|---|
| 1 | **Umsetzung beauftragen** | Auf Zuruf des Anwenders; als eigene Welle mit dem Stufenplan aus Abschnitt 8, Stufe 1 und 2 zuerst. **Beauftragt 09.10.2026 (Welle KA1).** |
| 2 | **Erster Dialog** | **Heizkessel** — Katalogpflege vollständig, Kosten mit Planwert und Wartungseinheit, Senken und Temperaturpaar, keine Überlagerung; danach BHKW (gleiches Muster plus Nebenposten). |
| 3 | **Ort der Katalogkosten** — die Planwerte haben Spalten am Katalog, die Kostenpositionen (Betriebskosten, Nutzungsdauer, Ersatz) nicht; Ursprungsverweis fehlt bei fünf Geräten und den Bedarfs- und Zeitreihenkopien (Abschnitt 5.1) | **Ein Schemaschritt** „Katalogkosten und Ursprung“: an jedem Katalog mit Kosten (`Tab_BHKW_STAMM`, `Tab_Heizkessel_STAMM`, `Tab_Pufferspeicher_STAMM`, `Tab_Stromspeicher_STAMM`, `Tab_PV_STAMM`, `Tab_Solarkollektoren_STAMM`, `Tab_WP_STAMM`, `Tab_Kaeltemaschine_STAMM`) eine Spalte `ID_KostenVorlage` (Verweis auf `Tab_KostenVorlage.ID`, leer = Standardvorlage des Gewerks), an den Kopien ohne Verweis `ID_Stamm`. Der Rückweg schreibt die Planwerte in die vorhandenen Spalten und die übrigen Positionen der Anlage als Kostenvorlage des Satzes (Gewerk = `KomponentenID`, Name = Satzname, nicht Standard); die Übernahme Katalog → Projekt zieht diese Vorlage vor der Standardvorlage. Ohne Schemaschritt gingen nur die Planwerte mit, und „Ursprung überschreiben“ bliebe dort mit „Ursprung nicht bekannt“ ausgegraut — ein Namensabgleich als Ersatz wird nicht empfohlen (Umbenennung trifft den falschen Satz). Die Nummer meldet die Umsetzungssitzung vor dem Bau an. **Umgesetzt (Schritt 208, `KatalogkostenUrsprungSchema`):** `ID_KostenVorlage` an den acht Katalogen, `ID_Stamm` an elf Kopien (fünf Geräte, sechs Bedarfs- und Zeitreihenkopien), je nullbar mit Fremdschlüssel `ON DELETE SET NULL`; dazu (Schritt 209, `KatalogkostenInvestitionSchema`, KA‑E‑14) `ID_KostenVorlageInvestition` an denselben acht Katalogen für die Investitionsvorlage des Satzes, ebenso nullbar mit `ON DELETE SET NULL`; wie gebaut in 5.3. |
| 4 | **Speicherort der Trennlinienhöhe** | `Dienste.Einstellungen` mit `LiesZahl`/`SchreibZahl`, ein Schlüssel je Dialog (`Katalogauswahl.Trenner.<Dialog>`), Wert in Pixeln der Projektliste, je Anwender, nicht je Projekt; ohne Ablage die Vorgabe. Kein Schemaschritt — die Höhe ist Bedienzustand, keine Projektdatum. **Umgesetzt in Stufe 1.** |
| 5 | **Rückweg bei Kindzeilen** — Kennlinien (`Tab_Kenndaten`, `Tab_Kenndaten_Kuehlung`, `Tab_Kenndaten_Kaeltemaschine`), Zeitreihen (`…Daten`), Typsätze der Bedarfsprofile, PV-Stränge, Gebäudezonen | Technische Kindzeilen gehen **vollständig** mit: bei „neu“ als Kopie am neuen Satz, bei „überschreiben“ als Ersatz der Kindzeilen des Ursprungs in derselben Transaktion. Anlagenbezogene Kindzeilen bleiben im Projekt: PV-Stränge und Wechselrichterzuordnung (`Z_AnlageStrang`), Senken; Gebäudezonen und Bauteile wie heute. Die Rückfrage nennt, was im Projekt bleibt. **Umgesetzt in Stufe 2** (Mechanismus im Kernweg; der Heizkessel hat keine Kindtabellen, 5.3). |
| 6 | **Katalogpaket** — ein ungesperrter Satz aus der Auslieferung (`Katalog_Schluessel` gesetzt) wird überschrieben | Schlüssel stehen lassen, die Prüfsumme nicht nachführen, damit die Katalogaktualisierung den Satz als vom Anwender geändert erkennt; beim Bau gegen die Regeln des Katalogpakets prüfen. **Umgesetzt in Stufe 2:** Der Rückweg schreibt `Katalog_*` nie; der Katalogabgleich vergleicht die gespeicherte mit der berechneten Prüfsumme der benannten Fachspalten und erkennt so die Änderung. `ID_KostenVorlage` ist Metaspalte und ändert keine Prüfsumme; die Auslieferungsvorlage bleibt unberührt (`Auslieferungsvorlage.Tests` grün, Tabellenzahl gleich). |
| 7 | **Überlagerung „Simulation…" des Gebäudes** rollt als Ganzes um zwei Listen (Befund der Rollbereichprobe, außerhalb des Bausteins) | Stufe 4 (Überlagerungen nach KA‑E‑11). |

## 8 Stufenplan

| Stufe | Inhalt | Nachweis |
|---|---|---|
| **1 Baustein** (umgesetzt 09.10.2026, Statuszeile #861) | `Zweispaltenauswahl` auf V1: Raster ohne rollenden Dialogkörper, drei Rahmen mit Kopfleisten und Kennfarbe, Trennlinie mit Grenzen, Tastatur und Merken über `Dienste.Einstellungen`, Detailzeile, Mehrfachwahl samt Kopfhäkchen auf der gefilterten Liste, Doppelklick und Enter; Ressourcen in beiden Sprachen | bunit-Tests des Bausteins, Rasterprobe, `fensterprobe.mjs` angepasst, neue Probe „kein Rollbereich im Rollbereich“ |
| **2 Heizkessel** (umgesetzt 09.10.2026, Statuszeile #873; Teil a Knöpfe und Bearbeiten, Teil b Rückweg und Schemaschritt 208) | erster Wirt auf dem neuen Baustein: Knöpfe nach Abschnitt 4.2, Bearbeiten je Bereich und Mehrfach-Bearbeiten, Rückweg in die Datenbank mit Kernweg und — falls Punkt 3 so entschieden — dem Schemaschritt | Kern-Tests des Rückwegs (neu, überschreiben, gesperrt, Ursprung fehlt, belegter Name, Kosten, Transaktion), Dialogtests, SQL-Dialekt-Prüfer, alle Proben |
| **3 Erzeuger** (BHKW umgesetzt 10.10.2026, Statuszeile #887) | BHKW, Pufferspeicher, Stromspeicher, Solarkollektoren, Wärmepumpe (Überlagerung „Anlage…“), Photovoltaik (Überlagerung „Stränge und Wechselrichter…“) | je Gruppe Dialogtests und Proben |
| **4 Bedarf und Zeitreihen** | Gebäude (Überlagerung, Rückweg auf die neue Regel gezogen, Verwaltungsbetriebsart ohne Projektbereich), Bedarfsprofile, Wärmebedarf extern, Strom- und Solarganglinie | Dialogtests, Proben, Gebäude-Rückwegtests |
| **5 Kältemaschine und Abschluss** | Katalogauswahl der Kältemaschine; Wiki-Quellen der Dialoge, Logbuch-Entwurf; Papier nach `ueberholt/` | Dokumentationswachen, Wiki-Gegenlese |

**Proben sind Pflicht in jeder Stufe:** die Rasterprobe (virtualisierte `Katalogliste` mit fester Höhe aus
dem Raster statt `max-height`), `fensterprobe.mjs` (Kopf und Fuß im eigenen Fenster; sie misst für diese
Dialoge „nichts rollt außer den Listen und der Detailzeile“) und eine neue Probe „kein Rollbereich im
Rollbereich“ unter `Proben/Rasterprobe/` nach dem Muster der Mockup-Prüfung: alle zwölf Dialoge und die
Kältemaschinenauswahl in 1 280 × 800, 1 280 × 720 und 1 024 × 700, Trennlinie an beiden Grenzen, Detailzeile
auf und zu, Überlagerung offen — kein Element mit `overflow: auto|scroll` in einem anderen, Dokument und
Dialogkörper rollen nicht, Konsole fehlerfrei, mit Gegenprobe (ein absichtlich verschachtelter Rollbereich
muss rot werden).

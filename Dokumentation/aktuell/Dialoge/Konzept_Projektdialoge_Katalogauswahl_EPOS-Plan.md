# Konzept: Projektdialoge mit Katalogauswahl übersichtlich ordnen — fünf Varianten

**Anlass:** Anwenderauftrag vom 08.10.2026 mit Bildschirmfoto „Verwaltung BHKW“ (Fensterdialog,
1 546 × 1 000 px): „Dieser Dialog ist sehr unübersichtlich mit Scrollbar in Scrollbar.“ Gefordert sind
Funktionalität wie bisher, eine übersichtlichere Zuordnung von Datenbank zu Projekt, eine klare Zuordnung
der Knöpfe zu den Bereichen Projekt und Datenbank, Eignung für alle ähnlichen Dialoge und fünf Mockups
zur Wahl.

**Geltungsbereich:** die zwölf Projektdialoge auf dem Baustein `Zweispaltenauswahl` — `BhkwDialog`,
`HeizkesselDialog`, `PufferspeicherDialog`, `StromspeicherDialog`, `PhotovoltaikDialog`,
`SolarkollektorenDialog`, `WaermepumpenDialog`, `GebaeudeDialog`, `BedarfsProfileDialog`,
`WaermebedarfExternDialog`, `StromganglinieDialog`, `SolarganglinieDialog` — und die Katalogauswahl des
`KaeltemaschineAnlageDialog`. Damit schließt dieses Papier die Lücke, die
[`Konzept_Administrationsdialoge_Neuordnung_EPOS-Plan.md`](Konzept_Administrationsdialoge_Neuordnung_EPOS-Plan.md)
in Abschnitt 8 offen lässt (AD-Q12: Projektdialoge außerhalb des Schemas der Verwaltungen). Die dort
ruhenden Entscheide AD-Q2 bis AD-Q5 und AD-Q10 werden in Abschnitt 6 aufgegriffen.

**Mockups:** [`Mockups/Projektdialog_Katalogauswahl_V1.html`](Mockups/Projektdialog_Katalogauswahl_V1.html)
bis [`Mockups/Projektdialog_Katalogauswahl_V5.html`](Mockups/Projektdialog_Katalogauswahl_V5.html), je mit Bildschirmfoto `…_Vn.png`
(1 280 × 800) und `…_Vn_klein.png` (1 280 × 720). Der Knopf „Randnotiz“ oben rechts in jedem Mockup zeigt,
wie die dialogeigenen Teile hineinpassen.

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

## 3 Gemeinsame Regeln aller fünf Varianten

1. **Der Dialogkörper rollt nicht.** Kopf, Inhalt und Fuß teilen das Fenster in einem Raster
   (`grid-template-rows: auto minmax(0, 1fr) auto`); rollen dürfen nur die Listen und die Satzfläche —
   jede für sich, nie ineinander. Das ersetzt für diese Dialoge die Haftregel „Dialog im eigenen Fenster“.
2. **Jeder Bereich trägt seine Knöpfe selbst** und ist gerahmt und beschriftet: „Im Projekt“
   (Kennfarbe `--epos-schema-versorgung`), „Katalog (Datenbank)“ (`--epos-quelle-rahmen`), „Gewählter
   Satz“ (`--epos-senke-rahmen`) mit der Marke „Projektsatz“ oder „Katalogsatz“ im Kopf.
3. **Die Farbe eines Knopfs sagt, was er ändert; sein Ort, woher seine Eingabe kommt.** „In das Projekt
   übernehmen“ ist grün (ändert das Projekt) und steht beim Katalog (dort wird gewählt).
4. **Zeilenhandlungen nach dem Haus-Muster der Auswahlleiste:** erstes Wort der gewählte Name
   („BHKW 70 kW Erdgas:“), dann Vergleichen · Schloss · Löschen; Neu… rechts.
5. **Kosten gehören zum Projektsatz, Bearbeiten… und Alle Daten zum Katalogsatz** — sie stehen nie mehr in
   derselben Leiste.

Geprüft mit Playwright/Chromium bei 1 280 × 800 und 1 280 × 720 (auch aufgeklappt, Schublade offen,
Randnotiz offen): in allen fünf Mockups **kein Rollbereich in einem rollenden Element**, der Dialog rollt
nicht, das Dokument rollt nicht.

## 4 Die fünf Varianten

### V1 „Gerahmt und gestapelt“ (geringster Umbau)

```
┌ Kopf ─────────────────────────────────────────────── i KI ✕ ┐
│[IM PROJEKT] 2 Module · Summe 190 kWth   [▼ Aus dem Projekt …] │
│ Projektliste (2 Zeilen, rollt)                               │
│[KATALOG] Suche · 79 von 79 · Filter     [▲ In das Projekt …] │
│ Katalogliste (rollt, Rest der Höhe)                          │
│ BHKW 70 kW: Vergleichen · Schloss · Löschen          Neu…    │
│▸[KATALOGSATZ] BHKW 70 kW · 35 kWel · 70 kWth        Details   │
└ Abbrechen · OK ──────────────────────────────────────────────┘
```

Dieselbe Reihenfolge wie heute, aber zwei Rahmen mit eigener Kopfleiste; der Satz ist eine einklappbare
Zeile unten. **Vorzüge:** vertraut, kleinster Umbau (Baustein-CSS, Knöpfe der Wirte wandern), Tests bleiben
fast unberührt. **Nachteile:** bei 720 px bleiben dem Katalog rund **3,8 Zeilen** (gemessen 209 px Liste),
aufgeklappte Details nehmen ihm weitere; große Satzblöcke (Gebäude, Bedarfsprofile, Wärmepumpe) passen nicht
in die Zeile.

### V2 „Nebeneinander (Transferliste)“

```
┌ Kopf ──────────────────────────────────────────────────────┐
│[KATALOG] Suche · Zähler        │  → Hinzu  │[IM PROJEKT] 2  │
│ Katalogliste (☐, rollt)        │  ← Entf.  │ Projektliste   │
│ BHKW 70: Vergleichen·Schloss·Löschen  Neu… │ Summe 190 kWth │
│[KATALOGSATZ] Kenndaten · Bearbeiten… · Alle Daten (rollt)   │
└ Abbrechen · OK ────────────────────────────────────────────┘
```

Klassische Transferliste, Satzband über beide Spalten. **Vorzüge:** Richtung der Übernahme ist
unmissverständlich, Mehrfachwahl in beiden Listen natürlich, Doppelklick = Hinzufügen. **Nachteile:** der
Katalog verliert rund 520 px Breite — Spalten mit Rang 2 fallen (genau der Grund für Entscheid
W14a-E-10-Q2 und offenen Punkt O-5); bei 720 px 4,8 Katalogzeilen; das Band ist für PV-Stränge und den
Wärmepumpen-Anlagendialog zu niedrig.

### V3 „Projekt im Mittelpunkt, Katalog als Schublade“

```
┌ Kopf ───────────────────────────────────────────────────────┐
│[IM PROJEKT] 2 Module · 190 kWth   ┌[KATALOG] Modul hinzuf. ✕┐│
│ Projektliste  € −                 │ Suche · Zähler · Filter ││
│[PROJEKTSATZ] Kosten… Träger VL RL │ Katalogliste (☐, rollt) ││
│                                   │ BHKW 70: ＋Ins Projekt… ││
│                                   │▸[KATALOGSATZ] Bearbeiten ││
└ Abbrechen · OK ───────────────────└─────────────────────────┘┘
```

Die Hauptansicht ist das Projekt; „＋ Aus Katalog hinzufügen…“ öffnet eine Schublade von rechts (790 px)
mit allem, was den Katalog betrifft. **Vorzüge:** schärfste Trennung (Katalog nur bei Bedarf),
Projektsatz mit voller Breite — beste Lösung für PV-Stränge, Gebäude, Bedarfsprofile und den eingebetteten
Wärmepumpendialog; die Kältemaschine ist heute schon so gebaut; 7,8 Katalogzeilen bei 720 px.
**Nachteile:** ein Klick mehr je Übernahme, das Projekt ist hinter der Schublade verdeckt, neuer Baustein
mit Fokusfalle und stufenweisem Esc; größter Eingriff in die Fenster- und Fokusproben.

### V4 „Reiter Projekt | Katalog mit Detailspalte“

```
┌ Kopf ───────────────────────────────────────────────────────┐
│ (Im Projekt · 2 · 190 kWth) (Katalog · 79)  │[KATALOGSATZ]   │
│ Suche · Zähler · Filter               Neu…  │ Kenndaten      │
│ Katalogliste in voller Höhe (☐, rollt)      │ Bearbeiten…    │
│ BHKW 70: ＋ In das Projekt · Vergleichen ·  │ ▸ Alle Daten   │
│          Schloss · Löschen                  │ (rollt allein) │
└ Abbrechen · OK ─────────────────────────────────────────────┘
```

Zwei Reiter mit Zählern (der Projektreiter trägt Modulzahl und Summe), je Reiter eine Liste in voller Höhe
mit eigener Leiste; rechts eine feste Detailspalte (400 px) für den gewählten Satz. **Vorzüge:** längste
Liste bei 720 px (**8,3 Zeilen**), klare Trennung durch den Reiter, Detailspalte = Stammblatt der
Verwaltungen (ein Hausmuster für Verwaltung und Projekt; Verwaltungsbetriebsart = nur Reiter „Katalog“),
nutzt vorhandene Bausteine (`Reiter`, `Stammblatt`, `Auswahlleiste`), deckt sich mit dem ruhenden
Entscheid AD-Q2 („Umschalter Katalog | Im Projekt (n)“). **Nachteile:** Projekt und Katalog nie zugleich
sichtbar (gemildert durch Zähler am Reiter und ✓-Spalte); breite Projektsätze (PV-Stränge) brauchen eine
Überlagerung.

### V5 „Eine Liste mit Zeilenaktionen“

```
┌ Kopf ───────────────────────────────────────────────────────┐
│ Suche · Zähler · Filter · (nur Projekt)  Vergleichen Neu… │[SATZ]│
│ [IM PROJEKT] 2 · 190 kWth   (angeheftet)            € −   │      │
│ [KATALOG] 79                                     ＋ ✎ [S] [L] │      │
│ Katalogzeilen (rollen unter der Projektgruppe)            │      │
└ Abbrechen · OK ─────────────────────────────────────────────┘
```

Eine gemeinsame Liste; die Projektgruppe ist oben angeheftet und grün hinterlegt, Aktionen stehen als
Symbole je Zeile. **Vorzüge:** ein Klick ins Projekt, alles in einer Liste, kompakt. **Nachteile:**
Symbole ohne Wort, fünf Tabulatorhalte je Zeile (braucht wandernden Tabindex), die angeheftete Gruppe frisst
bei vielen Projektzeilen die Liste, Mehrfachwahl passt schlecht zu Zeilenaktionen; die Katalogliste müsste
zwei Zeilenarten tragen — größter Umbau an `Katalogliste` und den Rasterproben.

## 5 Vergleich

Zeilen bei 720 / 800 px: sichtbare Katalogzeilen à 46 px, gemessen in den Mockups. Aufwand grob in
Arbeitstagen für Baustein und zwölf Wirte samt Tests.

| Kriterium | V1 gestapelt | V2 Transfer | V3 Schublade | V4 Reiter | V5 eine Liste |
|---|---|---|---|---|---|
| Klarheit der Zuordnung | gut | sehr gut | sehr gut | sehr gut | mittel (Symbole) |
| Platz, kleiner Bildschirm (Katalogzeilen 720 / 800) | knapp (3,8 / 5,5) | mittel (4,8 / 5,9) | gut (7,8 / 9,5) | sehr gut (8,3 / 10) | gut (7 / 8,8) |
| Klicks „Modul ins Projekt“ (heute 2 + Zurückrollen) | 2 | 2 (Doppelklick 1) | 3–4 | 2–3 | 1 |
| Mehrfachauswahl | nein (nachrüstbar) | ja | ja | ja | schlecht |
| Tastatur | gut (Tab-Folge wie heute) | gut | mittel (Fokusfalle, Esc stufenweise) | gut (Reiter mit Pfeiltasten) | schwach (viele Halte) |
| Eignung für alle Dialoge | mittel (große Satzblöcke) | mittel (Breite, Band) | sehr gut | sehr gut | mittel |
| Umbauaufwand (Tage) | 3–4 | 6–8 | 8–10 | 7–9 | 10–12 |
| Risiko Tests und Proben | gering: `FensterrahmenTests`, `fensterprobe` anpassen | mittel: Rasterprobe (schmale Liste) | hoch: `fensterprobe`, `fokusprobe`, neue Schublade | mittel: `fensterprobe`, Reitertests | hoch: `rasterprobe`/`katalogprobe` (zwei Zeilenarten) |

Für alle Varianten gilt: Die Katalogliste bekommt eine feste Höhe aus dem Raster statt `max-height` —
das hilft der Virtualisierung (Rasterprobe), und die Haftregel des Fensterdialogs wird für diese Dialoge
gegenstandslos (`fensterprobe` misst dann „nichts rollt außer den Listen“).

## 6 Empfehlung

**V4 „Reiter Projekt | Katalog mit Detailspalte“** als Ziel für alle zwölf Projektdialoge, Heizkessel als
Pilot (wie AD-Q8 Stufe 3 vorgesehen):

- Sie löst beide Klagen am gründlichsten: genau **ein** großer Rollbereich je Seite, die Detailspalte rollt
  daneben, nie darin; jeder Knopf steht im Blatt des Bereichs, auf den er wirkt.
- Sie hat bei 1 280 × 720 den meisten Platz für die Liste und den Satz.
- Sie ist das Schema der Verwaltungen (Liste + Auswahlleiste + Stammblatt) mit einem Reiter davor — ein
  Hausmuster statt zweier; die Verwaltungsbetriebsart des Gebäudes ist einfach der Reiter „Katalog“ allein.
- Sie greift die ruhenden Entscheide AD-Q2 (Umschalter mit Zähler) und AD-Q4 (Mehrfachwahl mit
  Sammelübernahme) wieder auf und braucht keinen neuen Baustein außer der Anordnung.

**V3** ist die Ausweichempfehlung für Dialoge mit sehr großem Projektsatz (Gebäude, Wärmepumpe), falls die
Detailspalte dort nicht reicht. **V1** taugt als Sofortmaßnahme gegen das Doppelrollen (2–3 Tage), wäre
aber ein Zwischenstand, der später erneut umgebaut wird.

## 7 Offene Anwenderentscheide

| Kennung | Frage | Empfohlene Antwort |
|---|---|---|
| **KA-Q1** | Welche Variante wird Ziel für die Projektdialoge? | **V4** (Abschnitt 6) |
| **KA-Q2** | Zuerst eine Sofortmaßnahme (V1: Dialog rollt nicht, Rahmen und Knöpfe je Bereich) oder gleich das Ziel? | **Gleich das Ziel**, Heizkessel als Pilot; V1 nur, wenn der Pilot länger als zwei Wochen ausbleibt |
| **KA-Q3** | Welcher Reiter ist beim Öffnen aktiv? | **„Im Projekt“, wenn das Projekt Komponenten hat, sonst „Katalog“**; nach einer Übernahme bleibt „Katalog“ offen |
| **KA-Q4** | Mehrfachwahl mit Sammelübernahme („＋ 3 in das Projekt“), Trägerwahl einmal je Brennstoff (AD-Q4)? | **Ja** |
| **KA-Q5** | Doppelklick auf eine Katalogzeile übernimmt ins Projekt (AD-Q3)? | **Ja**, im Projektdialog; in den Verwaltungen weiter nichts |
| **KA-Q6** | Bleibt die Katalogpflege (Neu…, Schloss, Löschen, Bearbeiten…) im Projektdialog (AD-Q10)? | **Ja, im Reiter „Katalog“** — dort ist sie eindeutig zugeordnet; ein eigener Weg „Katalog verwalten…“ entfällt |
| **KA-Q7** | Ändert „Alle Daten“ beim Projektsatz die Projektkopie und beim Katalogsatz den Katalog (AD-Q5)? | **Ja** — die Marke „Projektsatz“/„Katalogsatz“ im Kopf der Detailspalte sagt, was gespeichert wird |
| **KA-Q8** | Die drei Kennfarben (Projekt grün, Katalog blau, Satz orange) als Hausregel auch in der Anwendung? | **Ja, als Rahmenfarbe der Bereiche und Marke im Kopf**; an den Knöpfen nur die Stellung, kein Farbstreifen |
| **KA-Q9** | Breite Projektsätze (PV-Stränge, Wärmepumpen-Anlage, Gebäude): Detailspalte verbreitern oder Überlagerung? | **Überlagerung im selben Fenster** über „Stränge…“ bzw. „Anlage…“; die Detailspalte bleibt 400 px |

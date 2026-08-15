# Konzept: Hydraulik-Übersicht und Redesign der Simulations-Konfiguration

Stand 15.08.2026 · Ergänzungskonzept zu `Konzept_Simulation_QuellenSenken.md` (Fassung 12).
Mockups: `Entwurf_Hydraulikuebersicht_Konfiguration.html` (v2, im selben Ordner).
Antworten und Bezeichner deutsch; Lokalisierung nach der Drei-Schichten-Regel (CLAUDE.md).

## 1. Ziel und Anforderungen (Nutzervorgaben vom 15.08.2026)

Die Konfigurationsseite der Simulation macht heute nicht sichtbar, was die Engine seit
Paket 4–6 rechnet. Gefordert ist:

1. **Quelle und Senke je Erzeuger auf einen Blick** — inklusive des gewählten
   Pufferspeichers auf beiden Seiten.
2. **Je Pufferspeicher sichtbar:** Verwendung (Heizung und/oder Warmwasser), welche
   Erzeuger ihn laden (wirksame Reihenfolge), was er versorgt, wer ihn als Quelle nutzt.
3. **Temperaturniveaus der Erzeuger** sichtbar, insbesondere im Zusammenspiel mit dem
   Puffer (kann der Erzeuger den Puffer laden?).
4. **Alle Wärmeerzeuger** haben als Senke die Optionen: Heizkreis direkt, Puffer
   Heizung, Puffer Warmwasser, **Puffer Kombi (Heizung + Warmwasser)**.
5. **Die Erdsonde ist ausschließlich Quelle der Wärmepumpe** (keine Verbindung zu
   Solarthermie o. a.).
6. **Ein Pufferspeicher kann Quelle der Wärmepumpe ODER des Spitzenkessels sein**
   (Kaskade: der Puffer liefert die Eintrittstemperatur, der nachgeschaltete Erzeuger
   hebt weiter an). Diese Kaskade muss klar ersichtlich sein.
7. **Kombispeicher decken Heizung und Warmwasser gemeinsam** — ein Wärmevorrat für
   beide Bedarfe; gibt es nur Kombispeicher, laufen beide Kanäle über diesen Vorrat.

## 2. Ist-Befund (Kurzfassung der Aufnahme vom 15.08.2026)

- `Form_Simulation_Config`: doppelte Auswahlmechanik (vier ComboBoxen links + Tabelle
  rechts), 9-Spalten-ListView ohne Temperaturen/Prioritäten/Verwendung, tote Spalte
  „Zuordnung (alt)", unsichtbare Alt-Rubrik, Fenstergeometrie per Pixel-Arithmetik.
- Die Puffer→Nutzer-Sicht existiert nur in `Form_PufferSp_Projekt` (Ladereihenfolge-
  Tabelle) und kennt dort die **Quell**-Nutzer nicht; `Form_Waermesenke` verwirft die
  berechnete Mit-Lader-Liste („Lädt als n von m").
- **Zwei Identitäten für den Quellpuffer:** Senke = FK `WS_ID_Puffer` auf die
  Projektkopie; Quelle = Bezeichner `WQ_Puffer` gegen die **Stamm**tabelle.
  `WQ_ID_Puffer` existiert (Schema, Migration, Engine), wird aber von keinem Dialog
  geschrieben.
- `Ladeordnung` (Allgemein/Simulation) ist public, UI-frei und liefert je Speicher die
  komplette wirksame Lade- und Entladereihenfolge — die UI nutzt sie kaum.

## 3. Leitidee

**Eine Seite, zwei synchronisierte Ansichten, unveränderte Editoren.**

- Ansicht **„Liste"**: Erzeuger als Karten in Kaskadenreihenfolge (▲▼ verschieben),
  je Karte Chips für Quelle (blau), Hauptsenke mit wirksamer Ladepriorität ①②
  (koralle), Zweitsenke, Temperaturpaar, Betriebsmodus. Daneben die Speicher-Spalte:
  je Puffer eine Karte mit Verwendungs-Badges, Volumen, Temperaturpaar (mit Herkunft),
  Schwellen, Lader-Liste (aus `Ladeordnung.Ladereihenfolge`), „Versorgt" und
  „Quelle für" (Kaskade).
- Ansicht **„Schema"**: das Hydraulikschema (Mockup Abschnitt 1 der HTML): Spalten
  Wärmequelle → Erzeuger → Speicher → Abnehmer; Ladeleitungen koralle mit
  Prioritätskreis, Versorgung grün, Quellseite blau; Kaskadenleitung blau gestrichelt;
  darunter die automatisch abgeleitete **Kaskadenkette** als Pillen-Band.
- Doppelklick/✎ öffnet überall die bestehenden Dialoge (`Form_Waermesenke`,
  Quellen-Dialoge, `Form_PufferSp_Projekt`) — die neue Seite ist Lesefläche, keine
  Parallel-Editierwelt.

## 4. Datenmodell

| Änderung | Umfang |
|---|---|
| `Tab_Pufferspeicher.Verwendung` neuer Wert **`Kombi`** | nur neuer Textwert über `DbWerte` (Spalte TEXT vorhanden); Anzeige über Katalogschlüssel |
| `Tab_Energieanlagen.WS_Ziel`/`WS_Ziel2` neuer Wert **`PufferKombi`** | analog; `WaermesenkeClass.Normalisieren`/`IstPufferZiel` erweitern |
| **`WQ_ID_Puffer` wird die einzige Quellpuffer-Identität** | `Form_QuellePufferspeicher` listet PROJEKT-Puffer und schreibt die FK; `WQ_Puffer` (Bezeichner) bleibt als Lese-Altlast mit Migrationsregel (Bezeichner→FK-Auflösung, einmalig) |
| Quelle „Pufferspeicher" auch für **Heizkessel** zulässig | `WQ_Typ`-Freischaltung je `ID_Type` (WP: alle Typen; Kessel: nur `Puffer`); Erdsonde/Erdreich bleibt WP-exklusiv (Anforderung 5, entspricht der Engine-Unwirksamkeitsregel) |

Kein neuer Migrationsschritt nötig außer der einmaligen `WQ_Puffer`→`WQ_ID_Puffer`-
Auflösung (Datenregel im Stil R1–R6, Run-once über den Schema-Marker).

## 5. Engine-Abbildung (alles hinter `Kaskade_Zweikanalig`)

**Kombispeicher (Anforderungen 4/7):** Der Speicher hängt an beiden Kanälen der
Kaskadenschleife. Ladung ist kanalneutral; Entladung bedient je Stunde beide Kanäle aus
demselben SOC („gemeinsame Deckung"). Gemischte Konstellationen laufen über die
bestehende kanalweise Entladereihenfolge (`Entladeprio`) — der Kombispeicher erscheint
in beiden Kanallisten. Herkunftsrechnung und `Tab_ErgebnisPufferspeicher` arbeiten
bereits je Speicher und tragen die Rolle unverändert.

> **Entwurfsentscheidung K-1 (Default gesetzt, kippbar):** Reicht der Inhalt in einer
> Stunde nicht für beide Bedarfe, gilt **Warmwasser zuerst** — konsistent zur
> App-Konvention „Beides (Warmwasser zuerst)". Alternative: anteilige Aufteilung nach
> Bedarf.

**Kessel-Kaskade (Anforderung 6):** Ein Erzeuger mit `WQ_Typ = Puffer` bezieht seine
Eintrittstemperatur aus dem Quellpuffer statt aus dem Systemrücklauf;
`SimulationSPK` reduziert Brennstoff-/Leistungsbedarf um den vom Puffer gelieferten
Hub (Analogie: `SimulationWaermepumpe`-Quellbezug). Die **Rechenreihenfolge** ergibt
sich aus dem Quellbezug: der nachgeschaltete Erzeuger rechnet nach „seinem" Puffer —
das ist die Auflösung der offenen Konzeptfrage 5-2 (Verzahnung Phase B/C nach
Kaskadenposition) für den Anwendungsfall, der sie braucht. Entnahme des
Quell-Erzeugers = Entladung des Puffers (Bilanzraum-Mechanik aus 4b-1 unverändert).

**Temperatur-Kompatibilität (Anforderung 3):** Prüfregel je Ladebeziehung
„Erzeuger-Vorlauf ≥ Puffer-Vorlauf, sonst Warnung" (Anzeige amber; keine harte
Sperre — die Engine kappt ohnehin physikalisch). Herkunft der Puffertemperaturen wird
angezeigt (eigene Werte / Zuordnungszeile / Systemvorgabe — Vorrangkette aus Paket 1).

## 6. Umsetzungsetappen

| Etappe | Inhalt | Aufwand |
|---|---|---|
| **E0** | `WQ_ID_Puffer`-Vereinheitlichung (Dialog schreibt FK, Datenregel Bezeichner→FK, Engine-Leser konsolidieren) | ~1 PT |
| **D1** | Aufräumen: Spalte „Zuordnung (alt)" + tote Rubrik entfernen (Konzept-Etappe B; Spiegel-Brücke `WpSenkeSpiegeln` bleibt bis zur Abnahme), Layout auf `TableLayoutPanel`/`FlowLayoutPanel` statt Pixel-Arithmetik | ~1,5 PT |
| **D2** | `ErzeugerKarte` (UserControl) ersetzt ListView + linke ComboBox-Mechanik; ▲▼-Reihenfolge; Chips inkl. Temperatur-Warnregel | ~2,5 PT |
| **D3** | `SpeicherKarte` (UserControl): Badges, Schwellenband, Lader-/Nutzerlisten aus `Ladeordnung`, „Quelle für" | ~2 PT |
| **D4** | Ansicht „Schema" (GDI+-Panel, Zeichnung wie Mockup) + Umschalter, Auswahl-Synchronisation, Kaskadenketten-Band | ~3 PT |
| **D5** | Kombi-Senke (Dialog `Form_Waermesenke` 4 Optionen + Engine) und Kessel-Kaskade (Quellen-Freischaltung + `SimulationSPK`-Quellbezug + Reihenfolgeauflösung) inkl. Referenzverifikation | ~5–6 PT |

D1–D3 sind ohne Engine-Berührung auslieferbar (Regressionsnachweis: Flag aus
byte-identisch, Pflicht wie in allen Paketen). D5 braucht eigene Referenzszenarien
(Kombi-Projekt; Kaskadenprojekt WP→Puffer→Kessel) und erweitert die Abnahmefälle von
Paket 10.

## 7. Validierungen (Auszug)

- Puffer-Senke ohne existierenden Projekt-Puffer → Rückfall Heizkreis mit Meldung
  (Paket-5-N5-Muster, bleibt).
- Kombi-Senke verlangt Puffer mit `Verwendung = Kombi` (Dialog filtert; Anzeige der
  Karte warnt bei Diskrepanz).
- Quellpuffer = eigener Senkenpuffer derselben Anlage → verboten (Kurzschluss;
  Engine-Guard aus Paket 4 existiert, Dialog verhindert die Wahl).
- Kaskadenzyklen (A lädt B, B ist Quelle von A) → Dialogprüfung über die
  Kaskadenketten-Ableitung, Meldung statt Speichern.
- Temperatur-Warnregel wie Abschnitt 5; keine Untergrenzen (35/28 bleibt gültig).

## 8. Offene Entscheidungen

| # | Frage | Default |
|---|---|---|
| K-1 | Knappheitsregel Kombispeicher | Warmwasser zuerst (Abschnitt 5) |
| K-2 | Stromerzeuger/Batteriespeicher als eigene Kartengruppen unter den Wärmeerzeugern | ja, Anzeige-only in D2 |
| K-3 | Bivalenz-Umschaltung ALTERNATIV schaltet heute je Stunde nach Leistungsunterdeckung, nicht nach Bivalenz-Temperatur (`Abschaltpunkt` wirkt nur TEILPARALLEL) — beobachtet am Sichttest-Projekt; fachlich diskussionswürdig, aber unabhängig von diesem Konzept | separat entscheiden |

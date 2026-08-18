# Analyse B-5 — `SimulationSSP` ist ein Rumpf; wo rechnet der Batteriespeicher wirklich?

Stand 15.08.2026. Herkunft: Bestandsbefund **B-5** aus
[`Paket6_BHKW_Protokoll.md`](Paket6_BHKW_Protokoll.md) (Abschnitt 13.10 c, offener Punkt Nr. 10).
Reine Analyse — **keine Verhaltensänderung** an Rechenwegen. Datenbelege stammen aus den
eingefrorenen Referenzläufen (`Referenzlaeufe/2026-08-14_B1-Fixes`, `…_Paket7` und
`2026-08-16_B4`) und rein lesenden Abfragen gegen die Referenz-Arbeitskopie
`Referenzlaeufe/Arbeitskopie/Kenndaten.accdb`. Code-Zeilenangaben beziehen sich auf den
analysierten Stand `2dc323e` (Referenzbasis nach Paket 6).

> **Status 18.08.2026: HISTORISCH — durch das Stromspeicher-Paket (AP2b) überholt.**
> Dieses Dokument beschreibt den Bestand **vor** dem Paket. In `main` ist seit dem
> 17.08.2026 die `SpeicherEngine` (eigenes Projekt `SpeicherEngine/` mit Teststand) an
> die Stelle des Rumpfs getreten: `SimulationSSP.cs` ist gelöscht, die Batterie-Rechnung
> ist aus `SimulationPV` entfernt, und `SimulationControl.Simulation_Stromspeicher_Ctrl`
> bettet nur noch die **Entladung** der Engine in den Reststromvektor ein. Der bleibende
> Wert dieses Dokuments ist die Bestandsaufnahme des abgelösten Verhaltens — und als
> Abnahme-Checkliste, dass die neue Engine die unten notierten Lücken (Batterie ohne
> PV-Tool wirkungslos; BHKW-Überschuss nur über den PV-Umweg; ungenutzte
> Stammdaten-Spalten) tatsächlich schließt.

Korrektur 18.08.2026 (Nutzerhinweis): Die ursprüngliche Aussage „Laden nur aus
PV-Überschuss" war zu eng — auch **BHKW-Stromüberschuss** lädt die Batterie, weil er als
negativer Reststrombedarf bis in `SimulationPV` durchgereicht wird (Mechanik unter Frage 1).

## Ergebnis in einem Satz

Der Batteriespeicher wird ausschließlich in `SimulationPV` gerechnet — nur wenn das
Photovoltaik-Tool gesetzt ist; geladen wird aus PV-Überschuss und, über den negativ
durchgereichten Reststrombedarf, auch aus BHKW-Stromüberschuss. `SimulationSSP` ist ein
wirkungsloser Rumpf, dessen einziges beobachtbares Artefakt die konstant-0-Ganglinie
`ssp_gespeichert_viertelstunde.csv` im Referenzexport ist. Ein Batteriespeicher ohne
PV-Tool (Referenzprojekt 1017) rechnet **nirgends** — dann bleibt auch ein
BHKW-Überschuss ungespeichert.

## Frage 1 — Wird der Batteriespeicher woanders gerechnet? Ja: in `SimulationPV`

`SimulationPV.Berechnung` (`SimulationPV.cs:63–202`) liest die Speicher selbst und simuliert sie
stündlich mit:

- Kapazität = Summe `Tab_Stromspeicher.Energie` über alle `Tab_Energieanlagen`-Zeilen des
  Projekts mit `ID_Type = 4` (`WizardItemClass.SP_TYP`), je Zeile eine Speichereinheit.
- Stündliche SOC-Simulation mit fester Priorität: 1. Direktverbrauch der PV-Erzeugung,
  2. Laden aus dem Überschuss `Erzeugung − Restbedarf`, 3. Entladen gegen den
  verbleibenden Restbedarf.
- **Auch BHKW-Stromüberschuss lädt die Batterie** (Korrektur 18.08.2026): Der an
  `SimulationPV` übergebene `Strombedarf` ist der Restbedarfsvektor **nach** dem
  BHKW-Abzug, und dieser Abzug klemmt nicht auf 0 (`SubVectors(…, false)` — Altpfad
  `SimulationControl.cs:335`, zweikanalige Kaskade `:523–526` und `:562–565`). Ein
  BHKW-Überschuss steht daher als **negativer Restbedarf** im Vektor und vergrößert in
  der Ladeformel `Überschuss = Erzeugung − Bedarf` das Ladepotential — auch nachts bei
  PV-Erzeugung 0. Voraussetzung bleibt, dass das PV-Tool gesetzt ist, sonst läuft die
  Speichersimulation gar nicht.
- Nebenwirkungen dieser Verbuchung: In Überschussstunden werden `direktVerbrauch` und
  damit `Stromproduktion[i]` negativ, und nicht eingespeicherter BHKW-Überschuss landet
  im PV-`Ueberschuss` — er wird also als **PV**-Überschuss etikettiert (Export
  `pv_ueberschuss.csv`, Ergebnistabellen). Außerdem mittelt `SimulationPV` den
  Viertelstunden-Restbedarf auf Stundenwerte, Überschussspitzen werden dabei verschmiert.
- Modellvereinfachungen: `MaxLadeLeistungKW = SpeicherKapazitaetKWh` (1C-Annahme für Laden
  **und** Entladen); die Spalten `Leistung`, `Degradation`, `Ladezustand` aus
  `Tab_Stromspeicher` werden **nicht** gelesen; Start-SOC = 0; keine Lade-/Entladeverluste.
- Ausgaben: `Speicherfuellstand` (8760 Stundenwerte → Export `pv_speicherfuellstand.csv`)
  und `Speicherfuellstand_viertelstunde` (linear interpoliert, **nur** UI-Chart
  `Form_Simulation_Detail.cs:1687`, wird nicht exportiert).
- Die Entladung steckt in `Stromproduktion[i] = Direktverbrauch + Entladung`; diesen Vektor
  zieht `SimulationControl` vom Reststrombedarf ab (`SimulationControl.cs:342–348`). Die
  Batteriewirkung fließt also **über den PV-Block** in den Reststrom ein, nicht über SSP.

Bedingung: Der PV-Block läuft nur bei `tool[4] == "Photovoltaik"`
(`Tab_Einstellungen.Tool_5`). Ein Projekt mit Batterie, aber ohne PV-Tool hat **keine**
Speichersimulation — dort bleibt auch ein BHKW-Stromüberschuss ungespeichert (er steht
dann lediglich als negative Viertelstunden im Reststromvektor).

Empirischer Beleg für real existierenden BHKW-Überschuss: Projekt 1018 (BHKW, ohne
PV/Batterie) hat im Lauf `2026-08-16_B4` **24.532 negative Viertelstunden** im
`reststrom_viertelstunde.csv` (Minimum −21,2). Kein Projekt der Referenzmenge kombiniert
jedoch BHKW + PV + Batterie — der Ladepfad „Batterie lädt aus BHKW-Überschuss" ist durch
die Referenzläufe daher **nicht abgedeckt** und nur am Code nachgewiesen.

## Frage 2 — Ist `SimulationSSP` toter Altbestand? Funktional ja, mechanisch nein

Aufrufkette: `tool[5] == "Stromspeicher"` (`Tab_Einstellungen.Tool_6`) →
`Simulation_Stromspeicher_Ctrl` (`SimulationControl.cs:2465`) → `SimulationSSP.Berechnung`.
Beide Rechenwege sind gleich betroffen: die PV-/SSP-Blöcke liegen **nach** der Verzweigung
Altpfad / zweikanalige Kaskade (`SimulationControl.cs:341–356`).

Was `Berechnung` tatsächlich tut:

- liest die Speicherkapazität aus `Tab_Stromspeicher` und **benutzt sie nicht**;
- setzt alle 35.040 Werte von `Stromgespeichert` auf 0 und gibt das Array zurück;
- der Aufrufer subtrahiert den Null-Vektor vom Reststrombedarf — ein mathematisches No-Op.

Befüllte, aber nie gelesene Inputs: `stromspeicher_list`, `Strombedarf`, `m_ID_Projekt`.
Konsumenten des Ergebnisses: einzig `Referenzlauf/Ergebnisexport.cs:151`
(`ssp_gespeichert_viertelstunde.csv`). Kein UI-Chart und keine Ergebnistabelle liest
`Stromgespeichert`. Das persistierte Flag `Tab_Ergebnis.Sim_Stromspeicher` entsteht im
`tool[5]`-Block unabhängig vom Rechenergebnis und sagt nur „Stromspeicher war im Lauf dabei".

## Frage 3 — Stimmen `ssp_gespeichert_viertelstunde` und PV-Speicherfüllstand überein? Nein, strukturell unmöglich

`ssp_gespeichert_viertelstunde.csv` ist per Konstruktion in **jedem** Lauf konstant 0
(nachgeprüft: alle 35.040 Werte in allen drei Projekten, Läufe B1-Fixes und Paket7 identisch).
`pv_speicherfuellstand.csv` ist der stündliche SOC. Es sind zudem verschiedene Größen
(je Viertelstunde gespeicherte Energie vs. Füllstand) — ein Abgleich ist nicht sinnvoll möglich,
solange SSP ein Rumpf ist.

Befund je Referenzprojekt (Lauf `2026-08-14_Paket7`, Werte identisch zur Basis
`2026-08-14_B1-Fixes`; Speicherbestand aus der Arbeitskopie-DB):

| Projekt | Batterien (`Tab_Energieanlagen` → `Tab_Stromspeicher`) | Kapazität | Tool_5 / Tool_6 | `pv_speicherfuellstand` | `ssp_gespeichert` |
|---|---|---|---|---|---|
| 1007 | 3× BYD B-Box HVM 11.0 (10,2 kWh) + 1× Vaillant 10030745 (7,9 kWh) | 38,5 kWh | Photovoltaik / Stromspeicher | Summe 51.711,79; **Max 38,5 = Kapazität** — Batterie zykelt real | 35.040 × 0 |
| 1011 | wie 1007 | 38,5 kWh | Photovoltaik / Stromspeicher | **konstant 0** — PV-Überschuss ganzjährig 0 (`Vektor.pv_ueberschuss.Summe = 0`), Batterie lädt nie | 35.040 × 0 |
| 1017 | 1× BYD HVS+ 12.8 (12,8 kWh) | 12,8 kWh | **(leer)** / Stromspeicher | nicht exportiert — `SimulationPV` läuft nicht, Batterie rechnet **nirgends** | 35.040 × 0 |

## Bewertung und Empfehlung

`SimulationSSP` ist funktional toter Altbestand, aber nicht unerreichbar: Der Rumpf läuft in
jedem Lauf mit gesetztem Tool_6 mit und erzeugt die Export-Ganglinie.

**Jetzt nicht entfernen**, aus zwei Gründen:

1. Entfernen änderte die Exportfläche des Referenzlaufs (Datei und
   `Vektor.ssp_gespeichert_viertelstunde.Summe` verschwinden) und damit die eingefrorene
   Vergleichsbasis — das braucht Rücksprache.
2. Die fachliche Frage ist offen, ob die Batterie eine eigene Viertelstunden-Simulation
   bekommen soll: Laden aus beliebigem Überschuss **unabhängig vom PV-Tool** (BHKW-Strom
   lädt heute nur über den PV-Umweg, wird dabei der PV zugeschlagen und geht ohne
   PV-Tool ganz verloren), echte Lade-/Entladeleistung aus `Tab_Stromspeicher.Leistung`,
   Start-SOC aus `Ladezustand`, Wirkungsgrade. Dann wäre `SimulationSSP` der Ort dafür,
   und `SimulationPV` müsste die Speicherrechnung abgeben, sonst rechnet die Batterie
   doppelt.

Nachtrag 18.08.2026: Die Empfehlung oben gibt den Stand vom 15.08. wieder — die
Entscheidung ist inzwischen gefallen. Das Stromspeicher-Paket (AP2b) hat den Rumpf durch
die eigenständige `SpeicherEngine` ersetzt (siehe Status-Kasten am Anfang); `SimulationPV`
hat die Speicherrechnung abgegeben, die befürchtete Doppelrechnung ist damit vom Tisch.
Die hier markierten Punkte (Batterie ohne PV-Tool wirkungslos, dann geht auch
BHKW-Überschuss verloren; BHKW-Ladung nur über den PV-Umweg samt Fehletikettierung als
PV-Überschuss; ungenutzte Stammdaten-Spalten) taugen weiter als Abnahme-Checkliste gegen
die neue Engine.

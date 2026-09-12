# Abnahmebefund 4 — Kartenschalter fehlen beim Dialogaufruf (Protokoll)

Stand: 17.08.2026. Befund Philipp (Abnahme, Runde 4): „Die Funktionen beim
Wärmeerzeuger werden beim Aufruf des Dialogs ‚Simulation Konfiguration'
manchmal nicht angezeigt." Screenshot-Details: keine Schalter ▲▼✎× auf den
Wärmeerzeuger-Karten, außerdem verdeckt das Rang-Label den ersten
Titelbuchstaben („1 ⟨B⟩HKW", „2 ⟨H⟩eizkessel"); Chips vollständig sichtbar,
Speicherkarte rechts mit intaktem ✎.

## Ursache

`ErzeugerKarte.Neuordnen` entscheidet die Kopfzeilen-Platzierung über den
`Visible`-GETTER der Kindsteuerelemente (vier Stellen: `_lblPfeil.Visible`,
`if (!l.Visible) continue;` der Schalterschleife, `_lblRang.Visible ? …`,
`kopfUnten`-Schleife). Der Getter liefert den WIRKSAMEN Zustand — also
`false` für ALLE Kinder, solange das übergeordnete Fenster noch nicht
angezeigt wird. Genau diese Falle ist im Kommentar von
`ErzeugerKarte.HoeheNachfuehren` bereits beschrieben (dort seinerzeit mit
`_aufgeklappt` statt `_detail.Visible` gelöst).

Die Karten entstehen in `SetControls`, also VOR dem ersten Anzeigen
(`Form_Start` ruft `SetControls` vor `ShowDialog`). Folge beim Aufbau:

- Kein Schalter wird platziert — ▲▼✎× bleiben auf Restkoordinaten
  außerhalb der Karte, obwohl ihr gesetztes `Visible` stimmt.
- `titelLinks` fällt auf die Rangposition zurück — das Rang-Label übermalt
  den ersten Titelbuchstaben (simulierte Transparenz zeichnet den
  Parent-Hintergrund).

Selbstheilung nur über ein späteres `OnResize` (BaseForm-Stauchung,
Anwender-Resize). Bleibt die Fenstergröße nach dem Anzeigen unverändert,
steht die Kopfzeile dauerhaft falsch — deshalb „manchmal".

`SpeicherKarte.Neuordnen` liest kein `Visible` und ist nicht betroffen —
deckt sich mit dem Screenshot (Speicherkarten-✎ intakt).

## Messbeleg (Wegwerf-Harness `%TEMP%\wpk5`, Projekt 1018, DB-Arbeitskopie)

Harness nach Referenzlauf-Muster (DB-Arbeitskopie + Pfad-Umbiegung,
Produktiv-DB nur gelesen; App-Initialisierung identisch: DpiUnaware,
VisualStyles, GDI-Text, de-DE). Phase „nach Show + DoEvents", Karte
„BHKW · EC_Power_20kw.el_Gas", Kartenbreite 622 px:

| Schalter | vorher (Fehlbild) | nachher (Fix) |
|---|---|---|
| ▲ | x = −384 | x = 518 |
| ▼ | x = −278 | x = 543 |
| ✎ | x = −172 | x = 568 |
| × | x = +40 (unter dem Titel-Label) | x = 595 |

Alle vier `Visible=True` mit Fensterhandle — sie standen nur außerhalb der
Kartenfläche. Nach Fenster-Resize (Phasen C/D) war der Bestand auch vorher
korrekt (= die „manchmal"-Heilung). Chips in allen Phasen vollständig und
richtig umbrochen.

## Fix

`ErzeugerKarte.OnVisibleChanged` (neu, direkt nach `OnResize`): beim
Sichtbarwerden einmal `Neuordnen()` — dann liefern die Getter die echten
Zustände. Heilt alle vier Getter-Stellen auf einmal; die Alternative
(gemerkte Soll-Sichtbarkeiten je Schalter) hätte die Sichtbarkeitslogik aus
`Setzen` in `Neuordnen` dupliziert.

## Verifikation

- Harness-Gegenmessung: keine Verdikte mehr nach dem Anzeigen (die zwei
  verbleibenden „unsichtbar"-Meldungen stammen aus der Phase VOR dem Show —
  dort ist das wirksame `Visible` systembedingt false, kein Fehler).
- `dotnet test` SpeicherEngine.Tests: 337/337 grün.
- Prüfbuild Hauptprojekt (Full-MSBuild VS 2022, x86, ArtifactsPath
  `%TEMP%\wpb`): 0 Fehler / 6 Bestandswarnungen (Baseline).
- Encoding: UTF-8 mit BOM und reine LF erhalten; Diff = +22 Zeilen
  (Kommentar + Override), keine weiteren Stellen.

Gegenprobe am Programm: Dialog aus dem Wizard öffnen, OHNE das Fenster zu
bewegen — ▲▼✎× müssen sofort auf jeder Wärmeerzeuger-Karte stehen und der
Titel vollständig lesbar sein („BHKW", nicht „HKW").

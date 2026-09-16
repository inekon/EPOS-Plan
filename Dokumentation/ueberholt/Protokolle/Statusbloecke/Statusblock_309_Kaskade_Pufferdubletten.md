# Statusblock #309 — vier offene Punkte der Vorwellen geschlossen (16.09.2026)

Der ausführliche Block zur Statuszeile `#309` in
[`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md). Der Auftrag lautete:
die vier Punkte, die aus den Vorwellen offen und unverändert stehen geblieben waren,
entscheiden und umsetzen. Die Entscheide lagen dem Auftrag bei; hier steht, was daraus
geworden ist.

## 1 Die gepflegte Kaskade wird sichtbar und umkehrbar (#303‑1 und #303‑2)

**Die Lage.** `Tab_Einstellungen.Kaskade_Gepflegt` sperrt nicht nur das Nachziehen des
Heizkessels (`KonfigurationCtrl.HeizkesselNachziehen`), sondern auch die Vorwahl Ä15
(`SimulationKonfigHuelle.VerbauteAnlagenVorwaehlen`) — und damit für jede Erzeugerart. Wer
einmal umgeordnet hat und danach eine Wärmepumpe anlegt, findet sie nicht mehr von selbst
in der Kaskade. Der Anwender erfuhr davon nirgends und konnte es nicht zurücknehmen.

**Umgesetzt.** Die Lesart bleibt — wer die Kaskade anfasst, dem gehört sie —, aber sie wird
sichtbar und umkehrbar.

- **Sichtbar:** Steht die Spalte auf 1, trägt die Simulationskonfiguration über den
  Erzeugergruppen eine ruhige Zeile (`SIMKONF_KASKADE_GEPFLEGT`). Kein Warnbanner, kein
  rotes Band, kein `role="alert"`, nicht die Warnfläche der Hinweisleiste aus #190: Es ist
  ein Zustand, kein Fehler. Steht die Spalte auf 0, steht dort nichts.
- **Umkehrbar:** In derselben Zeile steht `SIMKONF_KASKADE_AUTOMATIK` („Automatik wieder
  übernehmen"). Er setzt die Merkspalte auf 0 — im Modell **und** in der Datenbank, an
  derselben einen Stelle, die sie setzt. Beide Richtungen laufen dafür jetzt durch
  `SimulationKonfigHuelle.KaskadeGepflegtSetzen`; `KaskadeGepflegtMerken` ruft sie mit
  `true`, `AutomatikUebernehmen` mit `false`. Damit bleibt es bei **einer** Wahrheit über
  die Merkspalte.
- **Die Kaskade selbst bleibt unberührt.** Sie hier zu leeren oder neu zu füllen hieße, im
  Namen des Anwenders zu ordnen. Beim nächsten Lesen der Konfiguration greifen Nachziehen
  und Vorwahl wieder, und was dann hineinkommt, sieht der Anwender an Ort und Stelle und
  kann es dort umordnen. Es entsteht darum auch kein ungespeicherter Kaskadenstand — der
  Handgriff meldet nichts als „geändert".
- Ohne eingelegten Weg gibt es den Knopf nicht (Hausregel „kein Delegat, kein Knopf"); die
  Zeile steht dann trotzdem, denn der Zustand gilt auch ohne Rückweg.

**Nachweis.** Drei bunit-Fälle in `EPOS.UI.Tests/Seiten/SimulationKonfigSeiteTests`: Die
Zeile steht nur bei 1; sie ist ruhig (kein `role`, keine Warnklasse); der Handgriff schreibt
genau einmal durch, die Zeile ist danach fort, und weder Verschieben noch Aufnehmen noch
Entfernen wurde gerufen, der Speicherstand bleibt sauber. Dazu ein Hüllenfall an der
Testdatenbank (`EPOS.Kern.Tests/HeizkesselKaskadeTests`): Bei gepflegter Kaskade und leeren
Plätzen fasst nichts an; nach dem Handgriff steht die Merkspalte in der Datenbank auf 0 und
die Kaskade ist weiterhin leer; die nächste Hülle zieht den Heizkessel nach und die Vorwahl
holt die Wärmepumpe.

## 2 Der Knopf an der Laufmeldung bleibt, wie er ist (#303‑3)

**Entschieden: so lassen.** Der Knopf an der Meldung „Erzeuger ohne Kaskadenplatz" führt in
Schritt ① und hebt die Karte hervor, statt unmittelbar aufzunehmen. Der Grund trägt: Der
Arbeitsstand der Kaskade liegt in der Konfigurationshülle und wird erst mit „Konfiguration
speichern" geschrieben; aus Schritt ③ daran vorbeizuschreiben ergäbe zwei Wahrheiten über
dieselbe Kaskade. **Keine Programmarbeit.**

## 3 Die doppelte Pufferzuordnung ist bereinigt

**Gemessen.** `Z_ProjektPufferSp` führte 15 Zeilen, darunter **fünf** wortgleiche
Wiederholungen — gleiches Projekt, gleicher Pufferspeicher (Id und Bezeichner), gleicher
Erzeuger, gleiche Vor- und Rücklauftemperatur, gleiche Priorität, gleiche Schwellen, nur
eine andere Id:

| Projekt | Puffer | Erzeuger | bleibt | fällt |
|---|---|---|---|---|
| 1007 | 1007007 | Solarthermie | 10060 | 10172 |
| 1008 | 1008007 | BHKW | 10057 | 10071 |
| 1008 | 1008007 | Wärmepumpe | 10058 | 10072 |
| 1008 | 1008008 | Heizkessel | 10059 | 10073 |
| 1046 | 1054215 | Solarthermie | 10374 | 10375 |

Der Auftrag nannte 1007 und 1046. Die **drei Paare in 1008** sind beim Messen dazugekommen;
sie sind derselbe Sachverhalt unter derselben Regel („die jeweils zweite Zeile, die mit der
höheren Id"), und die Wache unten wäre mit ihnen rot geblieben. Sie sind deshalb mit
entfernt.

**Was daran hängt — vor dem Schreiben gemessen.** Die Tabelle ist stillgelegt; Senken und
Quellspeicher holt die Simulation aus `Z_AnlageSenke` und `Z_AnlagePufferVerbund`. Es gibt
im ganzen Bestand keinen Leseweg, der sie **je Erzeuger** auswertet. Was noch zugreift, ist
gegen Wiederholungen unempfindlich:

- `GeraeteWaisen.Referenzen` sammelt `ID_Pufferspeicher` in eine **Menge**;
- `Referenzlauf/Projektauswahl` setzt daraus zwei Wahrheitswerte (Puffer vorhanden, Puffer
  für Wärmepumpe);
- `Referenzlauf/Migrationslauf` zählt die Zeilen für eine Protokollzeile.

Auf `Z_ProjektPufferSp.ID` zeigt kein Fremdschlüssel. **Die Wiederholungen waren
wirkungslos:** Der Referenzlauf über alle dreizehn Projekte bleibt gegen `R8` byte-gleich —
`diff -rq` meldet außer `protokoll.txt` keinen einzigen Unterschied, der Vergleich 13/13
PASS.

**Das Werkzeug bleibt im Repo**, weil es angewandt wird:
`sql/tools/Bereinige-Pufferdubletten.sql` und `.py`, nach dem Muster von
`Bereinige-Probierpuffer` aus #302 — Probe auf einer Kopie, Zählungen vorher/nachher gegen
die vorher gelesene Erwartung, Nachweis, dass genau die erwarteten Ids fallen, dass keine
verbliebene Zeile wandert und keine Nachbartabelle kleiner wird, `foreign_key_check`,
`integrity_check`, Wiederholungslauf (der zweite Lauf entfernt 0 Zeilen), dann die echte
Datei. Die Regel steht in der Tabelle selbst (von jeder Gruppe bleibt die kleinste Id); das
Skript ist damit auf jeder Datenbank wiederholbar und kann nichts verlieren, was die
verbleibende Zeile nicht ebenso trägt.

**Eine Wache statt eines Zwangs.** `EPOS.Kern.Tests/PufferzuordnungWacheTests` hält die
Repo-Testdatenbank lesend (`mode=ro`, `immutable=1`, damit keine `-wal`/`-shm` daneben
entstehen) gegen die Bedingung „gleiche Projekt-, Puffer- und Erzeugerangabe mehr als
einmal" und meldet jeden Treffer mit Ids und dem Weg zum Werkzeug. **Kein eindeutiger Index,
kein Schemaschritt:** Ob zwei Zeilen mit demselben Erzeuger je fachlich richtig sein können,
ist nicht belegt, und ein Index würde diese Frage stillschweigend entscheiden.

## 4 Zwei Befunde bleiben, wie sie sind

### (a) Die Waisen in der Ergebnistabelle — nicht bereinigen, nicht verknüpfen

In `Tab_ErgebnisPufferspeicher` zeigen 23 der 36 Zeilen auf einen Pufferspeicher eines
anderen Projekts oder ins Leere; die Spalte trägt keinen Fremdschlüssel. Das bleibt so, und
das ist kein Versehen:

- Diese Tabelle hält **Ergebnisse vergangener Läufe**. Dass ein damals gerechneter Puffer
  heute nicht mehr existiert, ist kein Datenfehler, sondern der normale Lauf der Dinge —
  Ergebnisse sind Geschichte, Stammdaten sind es nicht.
- Ein Fremdschlüssel mit `CASCADE` löschte Ergebnisgeschichte, sobald jemand einen Speicher
  entfernt; einer ohne `CASCADE` blockierte das Löschen eines Speichers. Beides ist
  schlechter als heute.

**Belegt, bevor es festgeschrieben wurde:** Es gibt keinen Leseweg, der diese Zeilen über
`ID_Pufferspeicher` mit den Stammdaten verbindet. Gelesen wird die Tabelle an genau einer
Stelle — `ErgebnisCtrl.PufferZeilenLesenStill` mit
`SELECT * FROM Tab_ErgebnisPufferspeicher WHERE ID_Ergebnis = ? ORDER BY ID`, also über den
Ergebniskopf und ohne jeden Verbund. Aus der Zeile wandert `ID_Pufferspeicher` allein in
`ErgebnisPufferspeicherModel`, und dessen Verbraucher — `KennzahlenKatalog` (T oben Mittel,
T oben Min) und `BausteineProjekt.SpeichertemperaturenSchreiben` (die Speichertabelle des
Berichts) — greifen ausschließlich auf Werte der Ergebniszeile zu; den Namen trägt die
Ergebniszeile selbst (`Bezeichner`). Geschrieben wird das Feld in `SimulationRunner` aus dem
laufenden Speicherobjekt und in `ErgebnisCtrl` in das `INSERT`.

### (b) Die Wärmepumpe in 1017 ohne Kaskadenplatz — keine Automatik

Projekt 1017 führt vier Anlagen (Wärmepumpe, Stromspeicher, Heizkessel, BHKW); die Kaskade
trägt `BHKW` und `Heizkessel`. Dabei bleibt es. Der Entscheid HK‑E‑1 nannte ausdrücklich den
Heizkessel, und der Unterschied ist fachlich: Ein Kessel ist der nachrangige Erzeuger, der
deckt, was die vorderen übrig lassen — ihn hinten anzuhängen ist immer richtig. Wo eine
Wärmepumpe in der Reihenfolge steht, ist dagegen eine Auslegungsentscheidung mit Folgen für
Jahresarbeitszahl und Wirtschaftlichkeit; die trifft der Anwender. Der Weg dafür ist mit
#303 gebaut: Der Lauf meldet den Erzeuger ohne Platz, die Meldung trägt den Knopf in die
Konfiguration. **Nichts am Rechenweg, nichts an 1017.**

## 5 Logbuch-Entwurf (Version 1.2.0.2) — nicht hochgeladen

Ein Satz, und nur zu Schritt 1: Das ist das Einzige, was ein Anwender sieht. Die
Bereinigung der Testdatenbank, die Wache und die zwei geschlossenen Punkte bekommen keinen
Eintrag (Regel: Konzept Hilfesystem 13.4).

> Hat jemand die Reihenfolge der Erzeuger in einem Projekt selbst geordnet, sagt es eine
> ruhige Zeile über den Erzeugerkarten — und der Handgriff „Automatik wieder übernehmen"
> daneben gibt die Reihenfolge wieder dem Programm, ohne die Kaskade zu verstellen.

## 6 Abnahme

- Kern-Filter `WP-Plan.Kern.slnf` Release: 0 Fehler.
- Windows-Schale (`-p:EnableWindowsTargeting=true`): 0 Fehler, 5 Warnungen (Bestand).
- Tests des Filters: **8 544 grün, 1 übersprungen, 0 rot**.
- `SqlDialektPruefer`: 1 460 SQL-Texte, 0 Fundstellen.
- `Proben/ChartProben`: 0 Verstöße.
- Referenzlauf über alle dreizehn Projekte gegen `2026-09-16_R8_Heizkessel_Kaskade`:
  **13/13 PASS und byte-gleich**.
- Testdatenbank: `SQLite format 3` (LFS-Zeiger im Commit), Schemastand **82** unverändert.

**Nicht getan:** keine neue Basis, kein Schemaschritt, kein eindeutiger Index, keine
Automatik für andere Erzeugerarten, kein Aufnehmen aus Schritt ③, keine Änderung an
`Tab_ErgebnisPufferspeicher` und an Projekt 1017, kein Push, kein CI- und kein iOS-Lauf,
kein Wiki-Upload.

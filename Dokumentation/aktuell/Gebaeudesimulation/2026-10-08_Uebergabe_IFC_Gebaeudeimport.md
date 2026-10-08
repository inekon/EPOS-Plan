# Übergabe der Sitzung „IFC / Gebäudeimport“ (08.10.2026)

Übergabe auf ein anderes Konto. Dieses Papier nennt den Stand, die geltenden Entscheide, die
Regeln der Sitzung, die offenen Punkte und am Ende den Einstiegs-Prompt für die neue Sitzung.
Regelquelle bleibt [`CLAUDE.md`](../../../CLAUDE.md); dieses Papier ergänzt nur, was die Sitzung
darüber hinaus vereinbart hat. Dauerhafter Stand: [Statusdatei](../Status_iOS_Migration.md),
Entscheidungsregister in der [Statusdatei Gebäudesimulation](../Status_Gebaeudesimulation_VDI6007.md),
Abstimmung mit der Sitzung Gebäudesimulation: [Abstimmung G5](2026-10-07_Abstimmung_G5_IFC.md).

## 1. Stand

- **Zweige:** Arbeitszweig `ios_migration_september`. Der Sitzungszweig des alten Kontos
  (`claude/dazzling-babbage-jj93zy`) wurde bei jedem Push mitgezogen; die neue Sitzung nimmt
  ihren eigenen Sitzungszweig. Zuletzt gepusht: Zeile #832 mit diesem Papier (davor `8703e704d`, #831).
- **Zeilen dieser Sitzung** (alle mit grünem Gate gepusht; CI-Nachweis in der Zeile):

  | Zeile | Inhalt | CI |
  |---|---|---|
  | #813 | G5-N: Nordrichtung beim Import abfragen und nachträglich ändern, Schemaschritt 199 | grün |
  | #814 | G5-3: Befund je Bauteil, Farbmodus „Befund“, Bauteilflächen je Raum aus Körpern; Korrektur G5-3d (Hülldecke vor Körperdecke, Öffnungen an die Wand ihrer Lage) | grün |
  | #815 | Welle P: Gebäudeimport nur aus der Projektdatei (`.sqproj`), Schemaschritt 200 | grün |
  | #816 | Welle K: Gebäudeviewer für Projektdatei und gbXML wie für IFC (Körper aus Flächen) | grün |
  | #821 | Öffnungen am Wandrand als Kerbe; Meldungen der Flächenklassifikation je Format | grün |
  | #822 | Teilflächen je Richtung, Öffnungen über zwei Räume, Hanglage im Zonenweg | grün |
  | #823 | Befunde der Abnahme G5: Orientierung bei Mengensatz aus Raumgrenzen bzw. Körper, Hanglage im Einzonenweg | grün |
  | #825 | E108: Standprüfung IFC gegen Projektdatei mit Wahl der Aufbauquelle; Fensterrichtung aus eigenem Körper; Einheitenhinweis | grün |
  | #828 | Prüfpunkte #808/#801: Körpervergleich mit fremdnamigen und fremdklassigen Teilgruppen, Dach erbt U-Wert und Aufbau seiner Platten | grün |
  | #831 | Anwenderdatei Sportheim neu exportiert (IFC aus dem Bestandsprojekt), Diagnosetests nachgezogen | Kern-Lauf 37810231619 grün auf `8703e704d` |
  | #832 | Übergabe der Sitzung an ein anderes Konto (dieses Papier) | nur Dokumentation, `[skip ci]` |

- **Abnahme G5 am Rechenweg:** durch die Sitzung Gebäudesimulation erfüllt (#820, Nachabnahme
  A2/B3 #824).
- **Schemakette:** 199 (#813) und 200 (#815) von dieser Sitzung; 201, 202 von der Sitzung
  Gebäudesimulation; nächste freie Nummer laut Kopf der Statusdatei.
- **Referenzbasis:** die in `CLAUDE.md` genannte (beim Schreiben `2026-10-08_R44_Kuehlkurve`).
  Keine Welle dieser Sitzung hat den Rechenweg der Simulation geändert.

## 2. Geltende Entscheide

- **E101 (G5):** Bauteilflächen, Orientierung und Öffnungsabzug aus den Bauteilkörpern der IFC;
  Mengensatz vor Körper; Körper ergänzen nur, was die Datei nicht sagt.
- **Welle P (07.10.):** Import nur aus der Projektdatei als eigene Option; Format und Herkunft
  `SQPROJ`; Nordrichtung wie bei IFC ohne Nordwinkel abfragen, ohne Eingabe Annahme mit Bemerkung.
- **Welle K (07.10.):** Gebäudeviewer für Projektdatei und gbXML im Umfang wie für IFC; Körper aus
  den Flächen der Datei (Konzept Datenaustausch, Kapitel 17), Marke „aus Flächen gebildet“,
  kein Körpervergleich und keine Körperpaare aus solchen Körpern.
- **E98:** Rangfolge der Aufbauten beim Weg „IFC + Projektdatei“ (Aufbau der Projektdatei bei
  U auf 1 % gleich, sonst Katalogaufbau der Projektdatei mit dem U der IFC, sonst IFC-Schichten,
  sonst Ersatzaufbau). Code-Kommentare nennen E98 (früher fälschlich E97 — E97 ist die
  Auslegungsheizlast).
- **E108 (08.10.), schränkt E98 ein:** Weichen die U-Werte von IFC und Projektdatei auf mehr als
  5 % der Hüllfläche um mehr als 10 % ab, fragt der Import jedes Mal im Dialog „Welche Aufbauten
  gelten?“ — „Projektdatei“, „IFC“ oder „Abbrechen und neu exportieren“, ohne Vorgabe; bis zur
  Wahl ist Übernehmen gesperrt; „Datei erneut lesen“ fragt erneut, kein Schemaschritt.
- **Weitere Entscheide vom 08.10.:** Fenster ohne Wandrichtung nehmen die Richtung ihres eigenen
  Körpers (waagerechte Dachfenster bleiben ohne Azimut); der überflüssige Einheitenhinweis bei
  gleichem Wert in einem zweiten Satz entfällt; **kein** Plausibilitätshinweis gegen das Baujahr.

## 3. Regeln der Sitzung (über `CLAUDE.md` hinaus)

- **Wellenfolge:** Agenten im eigenen Worktree → Zusammenführung im Hauptbaum auf dem
  aktuellen origin → Gate → Statuszeile und Protokoll (Platzhalter `GATEZAHLEN`, von einem
  `sonnet-mechanik` parallel zum Gate vorbereitet) → Push beider Zweige → CI-Vermerk.
- **Gate:** `GATE_ABLAGE=<scratchpad>/gate bash Werkzeuge/Gate/gate_linux.sh <Nr>`, danach
  SQL-Dialekt-Prüfer, Windows-Schale auf Linux und `Werkzeuge/Auslieferungsvorlage.Tests`. Das
  Gate dauert rund 80 Minuten (die Kern-Tests allein 75); im Hintergrund mit Zeitgrenze zwei
  Stunden starten, sonst bricht es nach 30 Minuten ab. Ein reiner Nachzug von Testerwartungen
  darf gezielt statt mit vollem Gate geprüft werden, mit Vermerk in der Statuszeile.
- **Statusnummern** werden von mehreren Sitzungen schnell vergeben: unmittelbar vor dem Merge mit
  origin messen; ist die Nummer belegt, **vor** dem Merge umnummerieren (sonst trifft die
  Ersetzung fremde Zeilen); Konflikte in der Statusdatei zeilenweise lösen (fremde Zeilen von
  origin, eigene dahinter).
- **CI-Vermerk:** Kern-Läufe werden oft von Pushes anderer Sitzungen abgebrochen. Den jeweils
  neuesten Stand von origin beobachten, bis ein Lauf grün oder rot endet; jeder Lauf darauf
  enthält den eigenen Stand. Den Vermerk allein mit `[skip ci]` pushen.
- **Agenten:** Ein frischer Worktree steht auf einem alten Commit — erster Schritt im Auftrag
  `git switch -c <zweig> <basis>`. Speicher ist knapp: Agenten löschen `bin/` und `obj/`, fertige
  Worktrees sofort entfernen (`git worktree remove -f -f`), Scratchpad klein halten.
- **Anwenderdateien** unter `Quellen/` (sechs IFC-Dateien, versioniert; die Projektdatei
  `Sportheim_1970_unsaniert.sqproj` ist **nicht** versioniert, `.gitignore`): nur lesen; keine
  Werte, Namen oder Bezeichnungen in Code, Tests oder Protokollen. Die Diagnosetests auf diese
  Dateien laufen nur dort, wo die Dateien liegen — in einem Agenten-Worktree die Projektdatei per
  symbolischem Verweis einhängen, nie committen. In der CI laufen die IFC-Diagnosen mit, die
  Projektdatei-Diagnosen werden übersprungen.
- **Abstimmung mit der Sitzung Gebäudesimulation** (sie nimmt am Rechenweg ab und vergibt
  Schemaschritte und Basen): über das Abstimmungspapier G5 und die Statusdatei. Nachrichten
  direkt in ihre Sitzung gingen nur innerhalb desselben Kontos (einmalige Routine in ihre
  Sitzung).

## 4. Offene Punkte

**Beim Anwender:**

1. **Versionsnummer** für die Logbuch-Einträge (Entwürfe):
   - „Beim IFC-Import ermittelt EPOS-Plan Bauteilflächen und Ausrichtung auch aus den
     Bauteilkörpern und zieht Fenster, Türen und Durchbrüche von der Wand- und Dachfläche ab.“
   - „Nennt eine IFC- oder gbXML-Datei keine Nordrichtung, fragt der Gebäudeimport danach; die
     Ausrichtung eines importierten Gebäudes lässt sich im Gebäudedialog nachträglich ändern.“
   - „Die Gebäudeansicht zeigt im Farbmodus „Befund“, welche Bauteile beim Import auffällig
     sind.“
   - „Gebäude lassen sich jetzt allein aus der Projektdatei (.sqproj) importieren; die Quelle
     wählen Sie im Importdialog.“
   - „Die Gebäudeansicht zeigt Gebäude aus Projektdatei und gbXML jetzt mit Raum- und
     Bauteilkörpern wie bei IFC, mit allen Farbmodi und dem Bauteil-Steckbrief.“
   - „Passen beim Import von IFC und Projektdatei die Aufbauten beider Dateien nicht zusammen,
     fragt EPOS-Plan, welche gelten sollen.“
2. **Wiki-Upload** der Seite „Programm Dokumentation – Gebäudeimport“ (Repo-Quelle fortgeschrieben,
   gebündelt mit dem Logbuch); der WordPress-Connector des alten Kontos verband sich nicht.
3. **Sichtabnahme unter Windows** nach den „Nach“-Blöcken von #813 bis #816, #821 bis #825 —
   besonders die Rückfrage „Welche Aufbauten gelten?“ (Länge, drei Knöpfe, Tabellen bei schmalem
   Fenster), die Quellenwahl im Importdialog und ob sich bei „IFC + Projektdatei“ der zweite
   Dateidialog von selbst öffnen soll.
4. **Anwenderdatei Sportheim** (optional, im CAD): Kellerwänden am Erdreich ihren
   Bestandsaufbau zuweisen, gezeichnete Dicken auf den Bestand setzen, Baujahr auf 1970; danach
   IFC **und** Projektdatei neu exportieren. Die Projektdatei muss der neuen Sitzung direkt
   übergeben werden (nicht versioniert).

**Technisch, ohne Entscheid** (Verfeinerungen, keine Fehler):

- Projektdatei: Boden und Decke tragen je Raumpolygon nur eine Quellfläche; 41 % der
  Raumdreiecke tragen eine Ersatzkennung (Klick öffnet keinen Steckbrief); 5,9 % der
  Bauteilkörper mit angenommener Bezugsebene.
- `KERBE_NICHT_EINFACH` (Öffnungen, die sich nur in einem Punkt berühren): an keiner Datei
  aufgetreten, bleibt als benannte Meldung.
- Aufräumen: Das Übergabepapier [HottCAD-Verbund](2026-10-05_Uebergabe_HottCAD_Verbund.md) ist
  erledigt (HC-1 #740, HC-2 #746, HC-4 #754) und kann nach `Dokumentation/ueberholt/` (relative
  Verweise beim Verschieben nachziehen, Indexzeile entfernen).

## 5. Einstiegs-Prompt für die neue Sitzung

```
Du übernimmst die Sitzung „IFC / Gebäudeimport“ für EPOS-Plan (Repo inekon/EPOS-Plan,
Arbeitszweig ios_migration_september). Lies zuerst CLAUDE.md und
Dokumentation/aktuell/Gebaeudesimulation/2026-10-08_Uebergabe_IFC_Gebaeudeimport.md, dann den
Kopf und die letzten zwanzig Zeilen der Tabelle in Dokumentation/aktuell/Status_iOS_Migration.md
und `git log --format='%h %<(72,trunc)%s' -n 20 origin/ios_migration_september`.

Rolle: du orchestrierst; opus-umsetzung für Import, Körperweg, Tests, Dialoge, Fehlersuche und
Konfliktauflösung mit Fachinhalt; sonnet-mechanik für Merges ohne Fachkonflikt, Statuszeilen,
Protokolle, Wiki-Quellen; haiku-pruefung für Zählungen. Modell bei jedem Agentenaufruf setzen.
Agenten im eigenen Worktree, erster Schritt `git switch -c <zweig> <basis>`, sofort committen,
nicht pushen, bin/ und obj/ löschen. Wellenfolge: Zusammenführung auf aktuellem origin → Gate
(Werkzeuge/Gate/gate_linux.sh, im Hintergrund mit zwei Stunden Zeitgrenze) → Statuszeile und
Protokoll → Push beider Zweige → CI-Vermerk mit [skip ci]. Statusnummer unmittelbar vor dem
Merge gegen origin messen, bei Belegung vor dem Merge umnummerieren. Nie ohne Rückfrage:
iOS-, macOS-, Setup-Läufe, Wiki-Upload, Pull Requests, Force-Push, Tags. Anwenderdateien unter
Quellen/ nur lesen, keine Werte oder Namen daraus in Code, Tests oder Protokolle. Antworten
auf Deutsch, knapp.

Erste Schritte: (1) Prüfen, ob die Zeile #831 einen CI-Vermerk trägt; sonst den Kern-Lauf
37810231619 auf 8703e704d abwarten (bricht ihn ein neuerer Push ab: den Lauf auf dem
neuesten origin) und den Vermerk mit [skip ci] nachtragen. (2) Prüfen, ob die
nicht versionierte Projektdatei Quellen/Sportheim_1970_unsaniert.sqproj vorliegt; sonst den
Anwender darum bitten (die Diagnosetests der Projektdatei werden ohne sie übersprungen).
(3) Die offenen Punkte aus Abschnitt 4 mit dem Anwender abstimmen, zuerst die Versionsnummer
für das Logbuch und den Wiki-Upload.
```

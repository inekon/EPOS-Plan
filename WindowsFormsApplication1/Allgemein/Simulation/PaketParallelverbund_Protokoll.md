# Paket Parallelverbund — Umsetzungsprotokoll

Stand 18.08.2026. Entscheidung des Anwenders vom 17.08.2026, umgesetzt gegen Schemastand 13
(Zielstand 14).

## 1. Die Entscheidung

Im Wärmesenke-Dialog sind je Wärmeerzeuger **mehrere Pufferspeicher** wählbar. Sie rechnen als
**ein gemeinsamer Wärmevorrat** (Parallelverbund): Kapazitäten addiert, ein Füllstand, eine
Schaltschwelle. Einzelbehälter bleiben in Pufferverwaltung und Wirtschaftlichkeit erhalten; im
Simulationsergebnis entsteht **eine Zeile je Verbund**. Gilt für alle Wärmeerzeuger.

Die Zweitsenke bleibt ausdrücklich **ein** Ziel. Sie verwertet Überschuss mit eigener Priorität
und eigener Obergrenze; ein zweiter Vorrat mit eigener Schwellenlogik an dieser Stelle wäre eine
Rechenänderung ohne fachlichen Auftrag.

## 2. Datenmodell

Der bestehende Hauptsenken-Puffer `Tab_Energieanlagen.WS_ID_Puffer` wird zum **Leitspeicher**.
Ordinalketten und beide Senken-Slots bleiben unverändert. Die zusätzlichen Mitglieder liegen in
der neuen Tabelle

    Z_AnlagePufferVerbund (ID LONG PK, ID_Anlage LONG, ID_Puffer LONG)

mit Index `idx_AnlagePufferVerbund (ID_Anlage)` und zwei Beziehungen:

| Beziehung | Ziel | Löschregel | Begründung |
|---|---|---|---|
| `FK_Verbund_Anlage` | `Tab_Energieanlagen.ID` | CASCADE | Muster `FK_SpVariante_Anlage`. Eine Verbundzeile ist ein unselbständiger Anhang der Anlage. Restriktiv würde das Löschen jeder Anlage mit Verbund blockieren — ein bestehendes Bedienverhalten bräche. |
| `FK_Verbund_Puffer` | `Tab_Pufferspeicher.ID` | NO ACTION (restriktiv) | Muster `FkRestriktiv` / `WS_ID_Puffer`. Ein Mitglied ist ein echter Behälter mit Kapazität und Wirtschaftlichkeitszeile; verschwände er stillschweigend, sänke die gerechnete Verbundkapazität unbemerkt. |

**Leere Tabelle ⇒ exakt heutiges Verhalten.** Das ist die Regressionszusage des Pakets und in
Abschnitt 6 belegt.

**Invariante S-1 gewahrt** (Konzept_KonfigUI_Hydraulik, keine Puffer→Puffer-Beziehung): Die
Tabelle hängt an der **Anlage**. Der Verbund ist eine Aussage darüber, WIE EIN ERZEUGER lädt,
nicht eine Eigenschaft der Behälter untereinander — dieselben zwei Puffer können in einem anderen
Projekt völlig unabhängig arbeiten.

### Abweichung vom Auftrag: `ID LONG` statt `AUTOINCREMENT`

Der Auftrag nannte `ID AUTOINCREMENT`. Umgesetzt ist `ID LONG NOT NULL PRIMARY KEY` mit Vergabe
über `MAX(ID)+1`. Grund: Das Hausmuster für NEUE Tabellen ist seit ADR-001 die explizite Vergabe —
so `Tab_Preisreihe`, `Tab_PreisreiheDaten`, `Tab_Kostenprofil`, `Tab_StromspeicherVariante`; bei
`Tab_PreisreiheDaten` ist es ausdrücklich begründet („die Bestandstabellen der Ganglinien führen
COUNTER, das Hausmuster für NEUE Tabellen ist seit ADR-001 aber die explizite Vergabe"). Eine
einzige Tabelle mit COUNTER wäre eine zweite Konvention im selben Schema; der Gewinn wäre null,
weil der Controller die ID ohnehin selbst zieht.

## 3. Migration — Schritt 14

`ZIEL_VERSION` 13 → 14. Registriert in `SCHRITTE` nach Bestandsmuster, zwei Teile:

* **14a** Tabelle und Index — HART (ohne Tabelle gibt es nichts zu beziehen).
* **14b** die beiden Beziehungen — WEICH, wie `FK_SpVariante_Anlage`. Fehlt eine Beziehung auf
  einer fremden Datenbank, bleibt die Ablage benutzbar; das Aufräumen leistet der Anwendungsweg
  ohnehin ausdrücklich. Ein Abbruch würde den Versionsmarker zurückhalten und den ganzen Lauf als
  gescheitert melden, obwohl der Verbund arbeitet.

**Kein DML.** Der Leitspeicher liegt schon richtig, und Mitglieder lassen sich aus Bestandsdaten
nicht erraten. Der Schritt zählt am Ende nur, was in der Tabelle steht
(`DatenVerbundZeilen`) — und meldet ausdrücklich auch die 0, weil sie hier die eigentliche
Aussage ist: „kein Projekt führt einen Pufferverbund, der Rechenweg bleibt unverändert."

**Idempotenz** über `Ddl` („existiert bereits" = Erfolg). Belegt in Abschnitt 6.

## 4. Aggregationsstelle — die eine tragende Entscheidung

`SimulationControl.VerbundAufaddieren(SimulationPufferspeicher leit)`, aufgerufen in **beiden**
Blöcken von `SpeicherRegistryAufbauen` unmittelbar nach `Init(...)` und nach `RueckfallMelden(...)`.

**Warum dort.** An dieser Zeile ist der Leitspeicher fertig initialisiert, aber noch nicht in der
Registry. Alles, was danach kommt — Ladeordnung, Entladereihenfolge, Phase G, Persistenz nach
`Tab_ErgebnisPufferspeicher`, Kennzahlen, Berichtszeitreihen — arbeitet bereits mit dem fertigen
Objekt und braucht **keine** Verbundkenntnis. Ein Verbund ist damit für den gesamten Rechenweg
genau ein Speicher mit größerer Kapazität, und exakt das ist die fachliche Zusage. Jede andere
Stelle hätte den Verbundbegriff in die Kaskadenschleife tragen müssen.

**Nach `RueckfallMelden`**, nicht davor: Die Rückfallmeldung nennt die Kapazität, die aus dem
ΔT-Notnagel des Leitspeichers folgt. Davor gestellt, meldete sie eine Verbundkapazität und wäre
als Aussage über den einen Speicher ohne Temperaturpaar falsch.

**Q_max wird summiert, nicht das Volumen.** Jeder Behälter bringt sein eigenes Temperaturpaar mit,
und für jedes gilt seine Vorrangkette bzw. sein ΔT-Rückfall — genau wie bisher für einen
Einzelpuffer. Zwei mal 1000 l bei 60/40 und 50/40 ergeben nicht 2000 l bei einer der beiden
Spreizungen. Gerechnet wird jedes Mitglied über ein eigenes `SimulationPufferspeicher.Init`, das
die Registry nie sieht — so gibt es keine zweite Kapazitätsformel im Haus.

**Was mitkommt:** `Q_max`, `VerlustProStunde` (Bereitschaftsverluste; jeder Behälter verliert für
sich, der Verbund verliert die Summe).

**Was nicht mitkommt:** Schwellen (Ein/Aus/Nachrang), `SchwelleReserve`, `Entladeprio`,
Verwendung und das angezeigte Temperaturpaar — ausschließlich vom Leitspeicher. Ein gemeinsamer
Vorrat hat genau eine Regelung; zwei Abschaltschwellen an einem Füllstand wären keine Physik,
sondern ein Widerspruch.

Gelesen wird der Verbund **einmal je Lauf** (`_verbuende`, Feld), aufgebaut in
`AnlagePufferVerbundCtrl.VerbuendeDesProjekts` über einen Verbund `Z_AnlagePufferVerbund` ×
`Tab_Energieanlagen` — die Zuordnungszeile hängt an der Anlage, der Projektbezug kommt von dort.

## 5. Konflikt- und Sicherheitsregeln

### Beim Speichern (Dialog)

`WaermesenkeClass.Pruefen` Punkt 5 → `AnlagePufferVerbundCtrl.KonfliktPruefen`. Beanstandet wird
ein Mitglied, das

1. Hauptsenke (Leitspeicher) einer Anlage des Projekts ist,
2. Zweitsenke einer Anlage des Projekts ist,
3. schon zu einem Verbund mit **anderem** Leitspeicher gehört,
4. — bzw. ein Leitspeicher, der selbst Mitglied eines fremden Verbunds ist,
5. Wärmequelle **irgendeiner** Anlage des Projekts ist,
6. nicht zum Projekt gehört oder eine andere Verwendung trägt als der Leitspeicher.

**Ausdrückliche Ausnahme zu 3:** Derselbe Verbund darf von mehreren Erzeugern geladen werden. Das
ist der Fall „gleicher Leitspeicher, gleiche Mitglieder", und er fällt nicht auf, weil dann kein
FREMDER Leitspeicher gefunden wird.

`Normalisieren` räumt zusätzlich still auf, was ohne SQL entscheidbar ist: kein Puffer-Ziel ⇒
keine Mitglieder; der Leitspeicher ist kein Mitglied seiner selbst; keine Doppelnennung; die
eigene Zweitsenke ist kein Mitglied.

### In der Engine (Protokoll-Warnung, kein Absturz)

* **Abweichende Zuschnitte** — zwei Anlagen nennen für denselben Leitspeicher unterschiedliche
  Mitglieder: gerechnet wird die **Vereinigung** (ein Behälter ist hydraulisch entweder Teil des
  Vorrats oder nicht; eine erzeugerabhängige Kapazität desselben Speichers gibt es nicht), mit
  Warnung.
* **Mitglied fehlt / gehört zu fremdem Projekt** — Warnung, Mitglied wird nicht mitgerechnet.
* **Mitglied ist zugleich eigenständige Senke** (`RegistryFuerZweikanaligOeffnen`) — Warnung,
  `ImRechenpfad = false`. Als Anzeigeobjekt bleibt der Speicher zulässig.
* **Mitglied ist Quellspeicher** (`VerbundAufaddieren`) — Warnung, Kapazität geht **nicht** in den
  Verbund ein. Siehe Befund in Abschnitt 7.

## 6. Rechenweg-Pflicht

`_verbundErzwingtSpeicherstufe` als zusätzliches Oder-Glied neben `_bhkwErzwingtSpeicherstufe`,
gespeist aus `AnlagePufferVerbundCtrl.ProjektHatVerbund(idProjekt)`. Begründung: Der einkanalige
Altpfad holt seinen einen Speicher aus `Z_ProjektPufferSp` und kennt keine Ladeaufträge; ohne die
Speicherstufe würde die aufsummierte Kapazität gespeichert, angezeigt — und nicht gerechnet. Das
ist derselbe stille Wirkungsverlust, den Paket BHKW-Regulär für das BHKW beseitigt hat.

Eigener Protokollhinweis, aber nur, wenn nicht schon das BHKW den Weg erklärt — zwei Sätze über
dieselbe Weiche wären Rauschen.

`KonfigurationCtrl.KaskadeNotwendig` um Merkmal **(1b)** erweitert, damit der Schalter im
Konfigurationsdialog mit dem tatsächlichen Rechenweg im Gleichstand bleibt. Der Ersatzdatensatz
des Dialogs zählt wie bei den übrigen Merkmalen mit (`ersatz.HatVerbund`).

## 7. Befund aus dem Wirkungsnachweis: Quellspeicher als Verbundmitglied

Im Nachweislauf zu Projekt 1021 war der als Verbundmitglied eingetragene zweite Heizungspuffer
(1018014) zugleich **Quellspeicher** der zweiten Wärmepumpe. Ergebnis vor dem Fix: seine Kapazität
erschien **doppelt** — 13,537 kWh im Leitspeicher aufaddiert UND 4,51 kWh als eigenes Quellobjekt
in `Tab_ErgebnisPufferspeicher`.

Ursache: Das Sicherheitsnetz in `RegistryFuerZweikanaligOeffnen` lässt Quellspeicher
ausdrücklich als Erstes durch (`sp.IstQuelle ⇒ ImRechenpfad = true; continue`), weil sie einen
eigenen Rechenweg haben — ihre Kapazität folgt der Anlagen-Spreizung `WQ_Spreizung`, nicht dem
Temperaturpaar der Speicherzeile.

Behoben an der Stelle, an der die Kapazität summiert wird: `VerbundAufaddieren` schlägt jedes
Mitglied aus, das in `AnlagePufferVerbundCtrl.QuellPufferDesProjekts` steht, und meldet es. Die
Dialog-Konfliktregel wurde von „Quelle **derselben Anlage**" auf „Quelle **irgendeiner Anlage des
Projekts**" ausgeweitet; der Ressourcentext `SIM_VERBUND_KONFLIKT_QUELLE` folgt dieser Aussage.

Fachlich: Ein Behälter liefert entweder die Wärme oder er bildet den Vorrat, in den sie geladen
wird — beides gleichzeitig ist ein Kurzschluss.

## 8. Oberfläche

**Gewählte Variante:** Leitspeicher bleibt das bestehende Dropdown je Ziel; die zusätzlichen
Speicher kommen in **einer** `CheckedListBox` darunter (`SIM_GB_VERBUND`).

Begründung gegen „erster Haken = Leitspeicher": Die drei Fugen des Dialogs (`FuelleCombo`,
`AktuelleId`, `AusOberflaeche`) bleiben in ihrer Bedeutung unangetastet — das Dropdown ist
weiterhin die Quelle von `Daten.ID_Puffer`, und die gesamte Bestandslogik daran (`PufferWaehlen`,
`AktuellerHauptPuffer`, `PositionsText`, `btnPufferAnlegen_Click`, die Verwendungsfilterung in
`PufferListenLaden`) rechnet unverändert weiter. Ein Hakenmodell hätte den Leitspeicher-Begriff in
eine Liste ohne stabile Reihenfolge verlegt: Beim Abwählen des ersten Hakens wäre der Leitspeicher
stillschweigend ein anderer geworden — und damit die ID, unter der Schwellen, Entladepriorität und
Ergebniszeile laufen. Hinzu kommt die Fachlage: Der Leitspeicher ist kein gleichrangiges Element,
er trägt die Regelung. Zwei Bedienelemente drücken diesen Unterschied aus, eine Hakenliste
verwischt ihn.

**Eine** Liste für alle drei Ziele: Es kann ohnehin nur ein Ziel gewählt sein. Die Liste wird beim
Zielwechsel neu befüllt — dieselbe Mechanik, die `_cbPuffer2` über `Puffer2ListeFuellen` nutzt.
Sie enthält weder den Leitspeicher (er ist schon Teil des Verbunds) noch die Zweitsenke (eigenes
Ladeziel); gesetzte Haken bleiben erhalten, soweit der Puffer noch in der Liste steht.

**Summenzeile** `SIM_VERBUND_SUMME`: „Verbund: n Speicher · Q_max gesamt x kWh", gerechnet über
`WaermesenkeClass.VerbundKapazitaet` — dieselbe Summe über die Einzelkapazitäten, mit der die
Engine rechnet. Der Dialog wiederholt die Formel nicht.

**Layout:** Die Gruppe steht zwischen Hauptsenke und Ladeverhalten (Leserichtung: erst welcher
Speicher, dann welche zusätzlich, dann wie geladen wird). Alles darunter rückt um
`VERBUND_ZUWACHS = 146 px`; die Bestandswerte bleiben als Summanden sichtbar (`176 +
VERBUND_ZUWACHS` usw.), damit an jeder Stelle ablesbar bleibt, was vorher dort stand.

**Positionstext** unverändert: `AktuellerHauptPuffer` liefert den Leitspeicher, und die
Ladeordnung kennt ohnehin nur diese eine ID.

**Persistenz:** Leit → `WS_ID_Puffer` wie bisher; Mitglieder → `Z_AnlagePufferVerbund`, geschrieben
**in `WaermesenkeClass.Schreiben`** und nicht im Dialog. Damit müssen die Aufrufer nichts wissen,
und Leitspeicher und Mitglieder gehen in einem Zug weg. `Form_Simulation_Config.Uebersicht.cs`
blieb unangetastet.

## 9. Anzeigen

* **Senken-Chip** (`Form_Simulation_Config.Karten.SenkenChips`): Zusatz `+n parallel` vor der
  Kreisziffer, Tooltip `SIM_TIP_VERBUND`. Punktueller Griff nur bei Puffer-Senke — die Karten
  bauen ihre Daten aus einer Projektabfrage ohne Verbund-Nachschlag je Zeile.
* **Speicherkarte** (`SpeicherKarteDaten`): Detailzeile `PSP_KARTE_IM_VERBUND` — „Im Parallelverbund
  mit …; dieser Speicher hat im Lauf keinen eigenen Füllstand". Ohne sie suchte der Anwender im
  Ergebnis nach einem Speicher, den es dort nicht gibt.
* **Schema-Ansicht D4:** Die Ladekante zeigt auf den **Leitspeicher**; Mitglieder bekommen
  **keine** eigenen Kanten. Begründung: Die Kante bildet den Ladeauftrag ab, und den gibt es nur
  einmal — auf die Leit-ID. Eigene Kanten suggerierten getrennt geregelte Ziele. **Bewusst nicht
  geändert.**
* **Ergebnis- und Berichtsflächen:** nicht umgebaut. Die Verbundzeile erscheint automatisch unter
  der Leit-ID.

## 10. Löschschutz

* `PufferSpCtrl.ReferenzenAufPuffer` meldet Verbundmitgliedschaften als Referenz („… –
  Verbundmitglied") mit derselben Blockadewirkung wie Haupt-/Zweitsenke.
* `PufferSpCtrl.ReferenzenLoesen` **löscht** die Verbundzeilen (nicht nullen — eine Zuordnungszeile
  ohne Puffer hat keine Bedeutung; dieselbe Behandlung wie `Z_ProjektPufferSp`). Ohne diesen
  Schritt scheiterte das `DELETE FROM Tab_Pufferspeicher` an `FK_Verbund_Puffer`.

Anmerkung: Die Rollennamen in `ReferenzenAufPuffer` („Hauptsenke", „Zweitsenke", „Wärmequelle")
stehen im Bestand als deutsche Literale in der Methode. „Verbundmitglied" folgt diesem Muster;
eine halb lokalisierte Liste wäre schlechter als eine konsistente. Offener Punkt für ein
Lokalisierungspaket.

## 11. Belege

Alle Läufe auf `%TEMP%\wpk10` (Kopie der Produktiv-DB, Stand 13).

**Regression — leere Verbundtabelle:** Projekte 1018 und 1021, 43 CSV-Dateien,
**SHA256 byte-identisch** zur Vorher-Basis. Vergleichsmodus: `GESAMT: PASS (464 479 Werte)`.

**Migration:** 13 → 14 (Tabelle, Index, beide Beziehungen angelegt); zweiter Lauf „bereits
erledigt"; von einer auf 12 zurückgesetzten Kopie 12 → 14 mit `MigrationOk=True`.

**Wirkung, Projekt 1018** (BHKW, Leit 1054168 „Stora B 1000-6 ER 1 B" 965 l 70/50 + Mitglied
1054169 „PS Verbund Test 2" 1000 l 70/50):

| Kennzahl | ohne Verbund | mit Verbund |
|---|---:|---:|
| `Puffer.Q_max` [kWh] | 22,388 | **45,588** (= 22,388 + 23,2) |
| `Puffer.Ladung_gesamt` [kWh] | 33 828,85 | **48 730,80** (+44,1 %) |
| `Puffer.Entladung_gesamt` [kWh] | 32 963,90 | 46 831,23 |
| `Puffer.SOC_Max` [kWh] | 20,02 | 40,79 |
| `Sim.Speicher_Anzahl` | 1 | **1** |
| Zeilen `Tab_ErgebnisPufferspeicher` | 1 | **1** (ID 1054168) |

Energieprobe mit Verbund: 48 730,80 − 46 831,23 − 1 858,78 = **40,79 kWh = SOC_Ende**, exakt
aufgehend — keine Doppelzählung.

**Konfliktnetz:** Mitglied 1054169 zusätzlich als Hauptsenke des Heizkessels eingetragen →
Warnung, `Sim.Speicher_Anzahl` bleibt 1, Q_max bleibt 45,588, eine Ergebniszeile.

**Rechenweg-Pflicht ohne BHKW, Projekt 1021** (zwei Wärmepumpen, `Kaskade_Zweikanalig = False`):
Verbund Leit 1018013 (600 l 55/40) + Mitglied 1018012 (500 l 55/40) → Q_max 10,44 + 8,7 =
**19,14 kWh**, und der Protokollhinweis „Mindestens ein Wärmeerzeuger … rechnet deshalb IMMER über
die Speicherstufe". Energieprobe: 12 204,65 − 11 497,68 − 689,82 = 17,15 = SOC_Ende.

**Plausibilitätsprüfung:** Die einzige Beanstandung (`kessel_leistung.csv` Jahressumme 0) tritt in
BEIDEN 1018-Läufen auf — mit und ohne Verbund. Sie ist Folge der Präparation (das BHKW deckt mit
100 % Wärmebedarfsdeckung alles, der Kessel als Nachrang bleibt aus), nicht des Verbunds.

**Tests:** `dotnet test SpeicherEngine.Tests` — 337/337, unberührt.
**Prüfbuild:** 0 Fehler / 6 Bestandswarnungen (Baseline).

## 12. Offene Punkte

1. **Wirtschaftlichkeit.** Jeder Behälter behält seine Investitionskosten und wird einzeln
   bewertet — das ist die Zusage („Einzelbehälter bleiben in Pufferverwaltung und Wirtschaftlichkeit
   erhalten"). Ob die Wirtschaftlichkeitsfläche den Verbundbezug kenntlich machen soll, ist nicht
   entschieden und wurde nicht angefasst.
2. **Rollennamen in `ReferenzenAufPuffer`** sind deutsche Literale (Bestand), siehe Abschnitt 10.
3. **Zweitsenke ohne Verbund** ist eine bewusste Setzung, keine technische Schranke. Sollte sie
   fallen, wäre `WS_ID_Puffer2` der zweite Leitspeicher und `Z_AnlagePufferVerbund` bräuchte eine
   Rollenspalte.
4. **Schema-Ansicht D4** zeigt bewusst keine Mitgliederkanten (Abschnitt 9). Falls der Anwender
   die Behälter dort sehen will, wäre eine gestrichelte Zugehörigkeitskante zum Leitspeicher die
   naheliegende Form — sie verletzte allerdings die Invariante S-1 als ANZEIGE, nicht als Datum,
   und müsste entsprechend beschriftet sein.
5. **Mehrere Erzeuger am selben Verbund** sind erlaubt und werden bei abweichenden Zuschnitten als
   Vereinigung gerechnet. Ob der Dialog beim Speichern die Zuschnitte der anderen Erzeuger
   automatisch angleichen soll, ist offen — heute meldet die Engine die Abweichung.

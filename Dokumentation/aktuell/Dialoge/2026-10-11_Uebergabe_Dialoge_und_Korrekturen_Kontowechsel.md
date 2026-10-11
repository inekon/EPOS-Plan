# Übergabe der Sitzung „Dialoge und Korrekturen“ auf ein anderes Konto (11.10.2026)

Dieses Papier übergibt die Sitzung „Dialoge und Korrekturen“ an eine neue Sitzung unter einem
anderen Konto. Es nennt den Stand, die geltenden Entscheide, die Regeln der Sitzung über
`CLAUDE.md` hinaus und die offenen Punkte; der Einstiegs-Prompt steht in Abschnitt 5. Regelquelle
bleibt `CLAUDE.md`. Es löst das Papier vom 08.10.2026
([`2026-10-08_Uebergabe_Dialoge_und_Korrekturen_Kontowechsel.md`](../../ueberholt/2026-10-08_Uebergabe_Dialoge_und_Korrekturen_Kontowechsel.md)) ab.

## 1 Stand

- **Zweige:** Arbeitszweig `ios_migration_september`; es gibt keinen Sitzungszweig mehr (der alte
  wurde auf Wunsch gelöscht). Push-Commit dieser Übergabe: der Commit mit dem Betreff „Merge origin vor dem Übergabe-Push (Welle 13)“ (`git log --grep Übergabe-Push`).
- **Statuszeilen dieser Sitzung seit dem Kontowechsel vom 08.10.** in `Status_iOS_Migration.md`:
  - #861 Katalogauswahl V1, Stufe 1: Baustein `Zweispaltenauswahl`.
  - #873 Stufe 2: Heizkessel als erster Wirt.
  - #885 KZ1 Kennzahlenzeile der Übersicht; #886 KB1 Kompaktstufe und Rollbalken der Projektdialoge.
  - #887 KA3 BHKW; #888 KG1/KS1 Gebäude „Bearbeiten…“ und Spaltenwahl.
  - #905 bis #909 KA3: Pufferspeicher, Stromspeicher, Solarkollektoren, Wärmepumpe, Photovoltaik.
  - #910 DZ1 Detailzeile mit Vorrang, Ganglinie in den CSV-Dialogen.
  - #939 ÜS1 Satz-Überlagerung in voller Höhe; #940 ÜS2 Stift, Zusammenfassung, „Bearbeiten“.
  - #942 Wiki-Quellen der Katalogauswahl; #943 Konzept Kältebedarf; #944 K1 Schema 213.
  - #960 KM5 Kältemaschine; #961 ZR1 Wärmebedarf extern und Stromganglinie; #962 ZR2 Solar- und
  - #988 AW1 Knopfleiste am Satz (Heizkessel, Wärmepumpe); #989 AW2 Wärmepumpe Überlagerung mit Anlage.
    PV-Ganglinie; #963 BP Bedarfsprofile; #964 GEB Gebäude (Rückweg, Dialog, Überlagerung);
    #965 diese Übergabe.
  - #998 AW3a Erzeuger, #999 AW3b Stufe 4/5 und Kältemaschine: Knopfleiste am Satz in allen Wirten der Katalogauswahl.
- **Katalogauswahl V1:** Stufen 1 bis 5 umgesetzt (Baustein, Heizkessel, sieben Erzeuger,
  Zeitreihen, Bedarfsprofile, Gebäude, Kältemaschine) samt Satz-Überlagerung (ÜS1) und Stift und
  Zusammenfassung (ÜS2).
- **Kältebedarf:** Konzept
  [`Konzept_Kaeltebedarf_EPOS-Plan.md`](../Konzept_Kaeltebedarf_EPOS-Plan.md) (K0); K1 bis K4 gebaut
  (#944, #980–#982), K5 und K6 offen.
- **Basis:** R51 `2026-10-10_R51_FreieKuehlung` (Sitzung Gebäudesimulation).
- **Schemaschritte:** 213 (unser, K1) gebaut; 214 (K-F1) und 215 (AH-V4) gebaut von anderen
  Sitzungen; 216 (KM4-E1) angemeldet von der Sitzung Kälteanlagen — Kopfzeile der Statusdatei
  lesen, bevor eine Nummer angemeldet wird.
- Es laufen keine Agenten; es gibt keine offenen Worktrees.
- **CI:** Die Kern-Läufe der Pushes e6bbae833 (Welle 12) und 77f4ba259 (Welle 13, Teil 1) wurden
  meist durch fremde Pushes abgebrochen. CI-Vermerke an #905–#910, #939–#944 und #960–#965 stehen
  aus; nachtragen, sobald ein grüner Kern-Lauf existiert, dessen `headSha` den Push-Commit enthält.

## 2 Geltende Entscheide (Auswahl)

**Katalogauswahl** (Konzept
[`Konzept_Projektdialoge_Katalogauswahl_EPOS-Plan.md`](../Konzept_Projektdialoge_Katalogauswahl_EPOS-Plan.md))

- **V1 „Gerahmt und gestapelt“** (08.10.): oben „Im Projekt“, darunter „Katalog (Datenbank)“, unten
  die Zeile „gewählter Satz“; der Dialogkörper rollt nicht, nur die Listen. Katalogpflege bleibt im
  Projektdialog; Rückweg „In die Datenbank übernehmen…“ (neu oder Ursprung überschreiben, nie bei
  gesperrtem oder fehlendem Ursprung).
- **ÜS1** (10.10.) „Überlagerung in voller Höhe“: „Bearbeiten…“ öffnet den Satz als Überlagerung
  über dem ganzen Dialog mit OK und Abbrechen.
- **ÜS2** (10.10.): „Füge jeweils in die Auswahl (DB und Projekt) ein Bearbeiten-Symbol ein. Im
  Bereich Projektsatz kurze Angaben der Anlagendaten und einen Bearbeiten-Button anstelle
  Details“ — gilt für alle umgestellten Dialoge. Abbrechen verwirft auch Anlagenfelder (Merker);
  der Projektsatz ist in der Überlagerung nie nur lesend; die Wärmepumpe zeigt im Kopf „Anlage…“
  statt der Infoknöpfe; Bedarfsprofile tragen „Simulation…“ in der Projekt-Kopfleiste.
- **Gebäude:** der Projektstift öffnet „Hülle und Zonen“ (Bestätigung des Anwenders offen); die
  Gebäude-Verwaltung bleibt im Gerüst der Verwaltungen (Anwender „ja“, 10.10.).

**Kältebedarf** (Konzept
[`Konzept_Kaeltebedarf_EPOS-Plan.md`](../Konzept_Kaeltebedarf_EPOS-Plan.md))

- **E-K1:** siebter Reiter „Kältebedarf“ mit drei Kacheln.
- **E-K2:** Deckungsart je Zuordnungszeile, zentral oder dezentral (Split) mit EER fest oder
  linear, Kühlträger und Zähler; neben der Kaskade gebucht.
- **E-K3:** Vorlauf und Rücklauf der Profile sind nur Angabe.

**Aus dem Papier vom 08.10., weiter gültig:** Löschen aus der Datenbank unabhängig von der
Projektzuordnung, vor Löschen und Bearbeiten wird still gespeichert; Nutzfläche statt Wohnfläche;
Fokusregel `FocusAsync(preventScroll: true)` und Bannerregel (beide in `EPOS.UI/CLAUDE.md`);
Schloss in allen Projektdialogen mit Katalogliste, die Projektverwendung sperrt nichts, gesperrte
Sätze werden nie überschrieben; Komponentenübernahme mit Fachspalten, Kindzeilen und Kosten;
Erdwärme (Claesson–Javed, Betrachtungsjahr 10, Entzug ohne Taktstrom); Kataloge „jetzt nicht
sperren“ bei der Auslieferung. Erledigt und entfallen: Umsetzung der Katalogauswahl V1 (alle
Stufen gebaut) und die Ringlegende der Ergebnisübersicht (#859, #885).

**Arbeitsweise:** kein Wiki-Upload ohne Rückfrage (Anwender: „ohne Wiki upload“); der Push am Ende
jeder Welle ist freigegeben („Nehme Push nach Welle vor“).

## 3 Regeln der Sitzung (über `CLAUDE.md` hinaus)

**Agenten**

- Orchestrierung mit Opus 5.5 (Anwender hat am 11.10. gewechselt); sie arbeitet nur selbst, wenn ein
  Agentenaufruf teurer wäre. Rollen `opus-umsetzung`, `sonnet-mechanik`, `haiku-pruefung`; das
  Modell bei jedem Agentenaufruf ausdrücklich setzen; höchstens zwei Agenten parallel.
- Agenten arbeiten in eigenen Worktrees unter `.claude/worktrees/<name>` und in einem eigenen
  Unterordner des Scratchpads, committen sofort, pushen nicht, löschen `bin/` und `obj/`.
- In jedem Worktree zuerst `git lfs pull --include=Referenzlaeufe/Kenndaten_Test.sqlite`, sonst
  liegt ein 130-Byte-Zeiger und die Wachen sind rot.
- Der Probenwirt (`Proben/Rasterprobe/Wirt`) läuft auf eigenem Port und wird nur über die eigene
  PID beendet, nie mit `pkill -f Rasterprobe.Wirt` (parallele Agenten).
- Agenten laufen die betroffenen Testklassen aller Dialoge und Controller, die sie anfassen, auch
  die dialogübergreifenden: `ProjektdialogSchlossTests`, `ParametersatzTests`,
  `KatalogSatzWirftTests`, `KatalogListeWirftTests`, `BerechnungshilfeTests`,
  `GrundlagenknopfTests`, `KiMaskenabdeckungWacheTests`.
- Ressourcen-Merges: beide Mengen behalten, `</data>` nicht verschlucken, den Designer nur über das
  Werkzeug erzeugen.
- Dialogwünsche mit Probenfotos zur Sichtabnahme (`.proben-fotos/<auftrag>/`, steht in
  `.gitignore`).

**Wellenfolge und Status**

- Merge → Gate → Statuszeile und Logbuchsatz → Push → CI-Vermerk allein mit `[skip ci]`.
  Logbuchsätze in `Dokumentation/aktuell/Wiki_Update_2026-09-26.md` unter 1.2.0.9.
- Statusnummer unmittelbar vor dem Commit gegen origin messen und die eigenen Zeilen hinter die
  höchste fremde einordnen (parallele Sitzungen vergeben Nummern binnen Minuten); Statuszeilen
  haben genau vier Zellen. Schemaschritte vor dem Bau in der Kopfzeile anmelden und allein pushen.
- Stop-Hook-Meldungen „unpushed commits“ sind kein Auftrag.

**Gate**

- `GATE_ABLAGE=<scratchpad>/gate bash Werkzeuge/Gate/gate_linux.sh <Nr> <wurzel>` als ein wartender
  Hintergrundbefehl; Dauer rund 80 Minuten, der volle Kern-Testlauf allein rund 76. Ein mit
  `setsid nohup` losgelöst gestartetes Gate wurde beim Testschritt beendet: im überwachten
  Hintergrund starten. Nie zwei Gates zugleich; auf fremde Gates nur mit
  `pgrep -f "[g]ate_linux.sh"` warten (die Klammer ist Pflicht).

**CI**

- Läufe: `gh run list --repo inekon/EPOS-Plan --branch ios_migration_september --workflow kern.yml`.
  Protokolle lassen sich über den Proxy nicht laden; Läufe werden oft durch fremde Pushes
  abgebrochen.

**Nie ohne Rückfrage:** iOS-, macOS- und Setup-Läufe, Wiki-Upload, Pull Requests, Force-Push, Tags.
Keine Zugangsdaten ins Repository. **Antworten:** auf Deutsch, knapp.

## 4 Offene Punkte (mit Empfehlung)

1. **Kältebedarf K2 bis K6** nach `Konzept_Kaeltebedarf_EPOS-Plan.md` (Stufenplan Abschnitt 7).
   Empfehlung: K2 bis K4 als nächste Welle.
   - K2 Kernwege der Deckungsart: Felder in `Z_Projekt_Kaeltebedarf` und `Z_ProjektWaermebedarf`
     aus Schema 213 sind da; `Z_ProjektKaeltebedarfCtrl` hat `Deckungspruefung` und `Normalisieren`.
   - K3 Rechenweg: zentral über den Kühlkanal, dezentral (Split) neben der Kaskade
     (`Kaeltebedarf_Dezentral`, Strom über Kühlträger und Zähler in
     `Stromverbrauch_Kuehlung_stuendlich`, Ergebnisspalten aus 213).
   - K4 Startseite: siebter Reiter „Kältebedarf“ an Platz 3 mit drei Kacheln (Statusbits
     8192/16384, Hilfeschlüssel, `WinFormsNavigation` Maske `KaeltebedarfAdmin`).
   - K5 Dialoge: Kältebedarf extern = Wärmebedarf extern im Modus „nur Kühlung“ mit dem Kanalfilter
     `WizardCtrl.Del_WaermebedarfExtern(projekt, kanal)`; Kältebedarfsprofile = `BedarfsProfileDialog`
     mit `BedarfsArt.Kaelte` (die Hüllen sind vorbereitet) plus Deckungsfelder; die Kachel
     Gebäudekühlung springt in die Gruppe Kühlung des Projektgebäudedialogs.
   - K6 Ergebnisse, Bericht, Gruppe „Dezentrale Kältedeckung“ im Bereich Kälte der
     Simulationskonfiguration (KB-B der Sitzung Gebäudesimulation ist gepusht), gegebenenfalls ein
     Referenzprojekt mit neuer Basis (Nummer mit der Sitzung Gebäudesimulation abstimmen), Wiki.
   - Abgrenzung: Der Split der Bedarfsebene heißt „dezentral (Split)“; das Raumklimagerät Typ 14
     (K-D) gehört der Sitzung Kälteanlagen.
2. **Nachzüge der Katalogauswahl** (je kurz, nur bei Gelegenheit):
   - Kältemaschine ohne Mehrfachbearbeitung (nur auf Wunsch).
   - Unbenutzte Ressourcen `KMA_BTN_HINZU`, `KMA_BTN_UEBERNEHMEN`, `KMA_TITEL_KATALOG`,
     `KMA_KATALOG_*`, `KMA_FRAGE_LOESCHEN`, `KMA_HINWEIS_NEU` und den Parameter `LeerText` der
     Wärmepumpe entfernen.
   - Ganglinien: Der Stift einer ungesperrten Katalogzeile öffnet die Verwaltung ohne Vorwahl.
   - Bedarfsprofile: `vormerkung.Entfernt` beim Entfernen; eigene Hilfeschlüssel der Kälte (heute
     die der Prozesswärme).
   - Gebäude: Jahreswärmebedarf in der Zusammenfassung; Bilder der Überlagerung „Simulation…“ in
     Behältergröße zeichnen (`BildauftragMass`); Ausrichtung beim Abbrechen in einem Vorgang.
   - PV-Stränge sind nicht im Merker; das Kostenfenster wird vom Merker nicht erfasst.
   - Bildschärfe und Probenbefunde in 1 093 × 614 sind bekannte Befunde unter der Mindesthöhe.
3. **Konzept Katalogauswahl nach `ueberholt/`:** alle Stufen sind umgesetzt; nach den Nachzügen und
   K5 per `git mv` verschieben und die Verweise nachziehen (Wache `DokumentationLinkWacheTests`).
4. **Wiki:** Die Repo-Quellen sind fortgeschrieben. Bilder neu ziehen (`wikibilder.mjs`, unter
   anderem Kältemaschine und Gebäude). Sammel-Upload nur nach Rückfrage und mit der Versionsnummer
   vom Anwender; die 23 Logbuchsätze der unveröffentlichten Liste unter 1.2.0.9 gehen mit.
5. **Sichtabnahme unter Windows** beim Anwender für alle umgestellten Dialoge. Der Wärmepumpen-
   Befund „Kein Satz gewählt“ (Bildschirmfoto 10.10.) ließ sich im Probenwirt nicht nachstellen;
   der Rückfall des Satznamens ist gebaut, bitte unter Windows nachsehen.
6. **CI-Vermerke** an #905–#910, #939–#944 und #960–#965, siehe Abschnitt 1.
7. **Bannerprobe, Fall `gebaeude` rot** (`Proben/Rasterprobe/bannerprobe.mjs`, Fenster 1 088 × 624): Der Auslöser
   `.epos-gebaeude-in-db` öffnet seit dem Gebäudeumbau die Rückfrage „In die Datenbank übernehmen“ statt einer
   Meldung, deshalb erscheint kein Banner. Kein Fehler im Dialog; Empfehlung: den Auslöser der Probe auf eine
   Meldung des Gebäudedialogs umstellen (z. B. Löschen eines gesperrten Satzes) und die Gegenprobe prüfen.
   Die übrigen Fälle (`heizkessel`, `bhkw`) sind erfüllt. — erledigt mit #999: die Probe ist grün.

## 5 Einstiegs-Prompt für die neue Sitzung

```
Du übernimmst die Sitzung „Dialoge und Korrekturen“ für EPOS-Plan (Repo inekon/EPOS-Plan,
Arbeitszweig ios_migration_september). Lies zuerst CLAUDE.md und
Dokumentation/aktuell/Dialoge/2026-10-11_Uebergabe_Dialoge_und_Korrekturen_Kontowechsel.md,
dann den Kopf und die letzten zwanzig Zeilen der Tabelle in
Dokumentation/aktuell/Status_iOS_Migration.md und
`git log --format='%h %<(72,trunc)%s' -n 20 origin/ios_migration_september`.

Rolle: du orchestrierst mit Opus 5.5 und arbeitest selbst nur, wenn ein Agentenaufruf teurer
wäre; opus-umsetzung für Dialoge, Hüllen, Rechenweg, Tests, Fehlersuche und Konfliktauflösung mit
Fachinhalt; sonnet-mechanik für Merge, Gate, Statuszeilen, Logbuchsätze, Push und CI-Vermerk;
haiku-pruefung für Zählungen. Modell bei jedem Agentenaufruf ausdrücklich setzen, höchstens zwei
Agenten parallel. Agenten im eigenen Worktree unter .claude/worktrees/<name>, eigener Unterordner
im Scratchpad, zuerst `git lfs pull --include=Referenzlaeufe/Kenndaten_Test.sqlite`, sofort
committen, nicht pushen, bin/ und obj/ löschen. Probenwirt auf eigenem Port, nur über die eigene
PID beenden. Agenten laufen die betroffenen Testklassen aller angefassten Dialoge und Controller.

Wellenfolge: Merge → Gate (GATE_ABLAGE=<scratchpad>/gate bash Werkzeuge/Gate/gate_linux.sh <Nr>
<wurzel>, rund 80 Minuten, als ein wartender, überwachter Hintergrundbefehl; nie zwei Gates
gleichzeitig; auf fremde Gates nur mit `pgrep -f "[g]ate_linux.sh"` warten) → Statuszeile und
Logbuchsatz (Wiki_Update_2026-09-26.md, 1.2.0.9) → Push → CI-Vermerk allein mit [skip ci]. Der Push
am Ende jeder Welle ist freigegeben; einen Sitzungszweig gibt es nicht, gibt die Umgebung einen
vor, werden beide gepusht. Statusnummer unmittelbar vor dem Commit gegen origin messen, die
eigenen Zeilen hinter die höchste fremde einordnen, genau vier Zellen je Zeile. Schemaschritte
vor dem Bau in der Kopfzeile der Statusdatei anmelden und allein pushen. Nie ohne Rückfrage: iOS-,
macOS-, Setup-Läufe, Wiki-Upload, Pull Requests, Force-Push, Tags. Commit-Trailer:
Co-Authored-By des arbeitenden Modells und Claude-Session mit der URL deiner Sitzung. Vor jedem
Auftrag prüfen, ob der Wunsch schon gebaut ist. Dialogwünsche mit Probenfotos zur Sichtabnahme.
Antworten auf Deutsch, knapp; Zwischenstände nur auf Nachfrage.

Parallel arbeiten andere Sitzungen (Gebäudesimulation/Aufheizung, Kälteanlagen,
Berichterstellung, KI-Assistent, Wiki) auf demselben Arbeitszweig; Statusnummern, Basen und
Schemaschritte werden schnell vergeben.

Erste Schritte: (1) Stand prüfen: `git status` sauber, keine Konfliktmarker, keine liegen
gebliebene AGENT_LAEUFT, die Zeilen #905–#910, #939–#944 und #960–#965 tragen ihren CI-Vermerk.
(2) Den Stand in höchstens fünf Zeilen melden. (3) Mit dem Anwender abstimmen, womit
weitergemacht wird. Empfehlung: Kältebedarf K2 bis K4 als nächste Welle, vorher die offenen
CI-Vermerke.
```

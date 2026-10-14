# Übergabe der Sitzung „Berichterstellung_Wirtschaftlichkeit“ — Stand 08.10.2026

Diese Datei ist der Einstieg für die Fortführung der Sitzung in einem anderen Claude-Konto (Cloud oder Desktop-App).
Sie ersetzt das maschinenlokale Gedächtnis und das Scratchpad der bisherigen Sitzung; alles Fachliche steht in
Statusdatei, Protokollen, Konzepten und Registern. Das Gedächtnis der Desktop-App liegt je Rechner und
Windows-Benutzer (`C:\Users\<Benutzer>\.claude\projects\…\memory\`), nicht je Konto; in der Cloud beginnt jede Sitzung
ohne Gedächtnis und legt es aus dieser Übergabe neu an. Die Regeln des Repositoriums stehen in
[`CLAUDE.md`](../../CLAUDE.md) und gelten unverändert.

## 1 Stand

### 1.1 Wellen dieser Sitzung (alle gepusht auf `ios_migration_september`, CI-Nachweis eingetragen)

| Nr. | Datum | Inhalt | Nachweis |
|---|---|---|---|
| **#757** | 06.10. | Reiterzeile „Übersicht · Kosten · Wirtschaftlichkeit · Bericht“ als Bereichszeile (`epos-reiter--bereich`, Token `--epos-schriftgroesse-bereichsreiter: 15px`); Protokoll `BN_Bereichszeile_Protokoll.md` | Push `31a538492`, Kern-Lauf 37466422318, Nachtrag `f8d101261` |
| **#761** | 06.10. | Konzept Berichtsseite: VALERI-Darstellung und Anordnung ([`Konzept_Berichtsseite_VALERI_Anordnung_EPOS-Plan.md`](Konzept_Berichtsseite_VALERI_Anordnung_EPOS-Plan.md)), Fachvorgabe R‑E32, Mockups `Mockups/Berichtsseite_Anordnung_A.html` bis `_C.html` | Push `ae3a48990` |
| **#762** | 06.10. | Berichtsseite in vier Karten, Anordnung B (`.epos-bericht-raster`, rechte Spalte 420–480 px, `Optionsgruppe.TitelSichtbar`); Protokoll `BN_B_Anordnung_Protokoll.md` | mit #761; Kern-Lauf 37503127843 auf `cc9faeffc`, Nachtrag `b6e15df7f` |
| **#765** | 06.10. | VALERI-Darstellung des Wirtschaftlichkeitsberichts (`BerichtsKonfiguration.Szenariodarstellung`, Tafel „Kennzahlen je Szenario“, Platzhalter `stand.tabelle.wirtschaft_szenarien`, Katalogfassung 15, Klapplisten-Eintrag `SZENARIO_VALERI`, Messlatte `Bericht_Word_1030_Valeri.txt`); Protokoll `VB_VALERI_Darstellung_Protokoll.md`; Wiki-Quellen Wirtschaftlichkeit und Berichtsvorlagen, Logbuchsätze unter 1.2.0.7 | Push `cf0ec4442`, Kern-Lauf 37503127843; Windows-Nachweis 47/47 am 07.10. (`5e5ca9a1e`) |
| **#811** | 07.10. | Konzept Übergabegrenze und Bivalenz der Wärmepumpe, Fassung 1 ([`Konzept_Uebergabegrenze_Bivalenz_EPOS-Plan.md`](../ueberholt/Konzept_Uebergabegrenze_Bivalenz_EPOS-Plan.md)), Mockup `Mockups/Waermepumpe_Bivalenz_Uebergabe.html` | Push `62ba656f3`, Kern-Lauf 37687452447, Nachtrag `8d0454e6e` |
| **#818** | 08.10. | Fassung 2: Rücklaufgrenze für alle Wärmepumpen, R744-Abwertung, Berichtigung Mindestvolumenstrom → Höchstspreizung, Rücklaufgrenzen anderer Erzeuger, Rücklaufstufe „Vorwärmer“, BHKW-Option | Push `02306751f`, Kern-Lauf 37721500910, Nachtrag `b1a34adca` |
| **#819** | 08.10. | Umsetzungskonzept zur Integration ([`Umsetzungskonzept_Uebergabegrenze_Bivalenz_EPOS-Plan.md`](../ueberholt/Umsetzungskonzept_Uebergabegrenze_Bivalenz_EPOS-Plan.md)); danach alle Entscheide eingetragen (`e84e478ec`, `b38397388`, `c7543d00a`) | Push `7ad6be220`, Kern-Lauf 37730470762 auf `e84e478ec`; letzter Push `1e20a22e0`, bestätigt durch Kern-Lauf 37738197716 auf `68a957a50` |

Protokolle unter [`../ueberholt/Protokolle/Bericht/`](../ueberholt/Protokolle/Bericht/); die Recherchen zur
Übergabegrenze (Normen, Kältemittel, Datenblätter, Physik und Hydraulik, andere Erzeuger) und die beiden
Code-Inventare liegen gebündelt unter
[`../ueberholt/Protokolle/Bericht/2026-10-08_Recherche_Uebergabegrenze/`](../ueberholt/Protokolle/Bericht/2026-10-08_Recherche_Uebergabegrenze/)
— mit den Quellennummern der Recherche, nicht denen der Konzepte (Zuordnung in Konzept Abschnitt 11).

### 1.2 Entscheide des Anwenders (alle eingetragen)

- **Berichtsseite (06.10.):** VB‑Q1–Q9 je a, BL‑Q1 = B, BL‑Q2–Q5 je a (Register R‑E32); die ausführliche Excel-Vorlage nimmt
  den Platzhalter `stand.tabelle.wirtschaft_szenarien` nicht auf.
- **Übergabegrenze (08.10.):** UB‑Q1–Q11 nach Empfehlung, UB‑Q3 = b (R744: Abwertung 2,5 %/K ab 30 °C Rücklauf bis zur harten
  Grenze 40 °C). Umsetzungskonzept: U‑1 Opt-in (Rechnung nur bei gesetztem `Einbindung`, Basis R43 bleibt bis auf Projekt 1060
  unverändert), U‑2 Katalogfeld `Kaeltemittel` an `Tab_WP(_STAMM)`, U‑3 `Ruecklauf_Max` am BHKW als anlagenbedingte
  Abschaltgrenze getrennt vom Auslegungsrücklauf, U‑4 Stufe „Vorwärmer“ nur im Protokollhinweis.

### 1.3 Offene Punkte

**Beim Anwender**
- Synchronisation mit `GitHub_Sync.bat` (der lokale Hauptbaum lag am 08.10. weit hinter origin).
- Windows-Sichtabnahmen: Reiterzeile (#757, auch Hochkontrast), Berichtsseite in vier Karten (#762), Word-Bericht 1030 in
  VALERI-Darstellung, Excel-Kopfzeile und Vorbelegung (#765).
- Logbuch-Version 1.2.0.7 bestätigen; Wiki-Upload der Seiten „Wirtschaftlichkeit“ und „Berichtsvorlagen“ nur auf Zuruf
  (Regel: gebündelt, höchstens einmal je Woche).
- Aus der Bestandsaufnahme vom 06.10. (Nach-Blöcke der Statusdatei): Entscheid zur Einspeisungslinie des BHKW (#699/#706),
  iPad-Probe der Berichtsvorlagen, Word-365-Tippprobe.
- Zwei Sekundärquellen des Konzepts Übergabegrenze ([Q43], [Q58]) vor einer Übernahme in Hilfetexte am Primärtext gegenlesen.

**Bei anderen Sitzungen**
- **Gebäudesimulation:** Baubeginn UB‑E1 des Umsetzungskonzepts (zwei Opus-Wellen, erste Welle E1‑a in dessen Abschnitt 10);
  vor UB‑E2 den Schemaschritt `UebergabegrenzeSchema` im Kopf der Statusdatei anmelden (am 08.10. war 203 frei, 202 ist durch
  die Kühlkurve KK4 vergeben); Referenzprojekt 1060 als Kopie von 1056 in die CI-Auswahl, Basis danach als R44 neu einfrieren.
  Übergabetext in Abschnitt 4.
- Nicht gebaut und ohne Auftrag: KP3 Welle O3 (Berichtsabschnitt Konditionierung, Katalogfassung 12), Wiki-Quelle für
  „Berichte & Kosten / Bericht“ (der Index verweist darauf), Kältestrom je Anlage im Bericht (K24), KI-Werkzeug
  `bericht_erstellen` (beschrieben, nicht registriert).

## 2 Arbeitsweise dieser Sitzung

1. **Rolle:** Fachliche Führung der Berichterstellung (Berichtsseite, Vorlagen, Platzhalterkatalog, Messlatten
   `EPOS.Kern.Tests/Messlatten/Bericht_*`) und Konzeptarbeit auf Zuruf des Anwenders (zuletzt Übergabegrenze/Bivalenz).
   Rechenwege der Simulation baut die Sitzung Gebäudesimulation; die Wirtschaftlichkeit (Cloud) führt Konzept und Register
   ihrer Fachseite.
2. **Modelle:** Fable 5.1 orchestriert nur (Aufträge schneiden, Modell wählen, abnehmen, Entscheide vorbereiten, berichten).
   Agenten ausdrücklich mit `model: opus` (Rechenweg, Konzeptabsätze mit Fachinhalt, Hüllen, Fehlersuche), `sonnet`
   (Suchen, Inventare, Textpflege, Protokolle, Indexzeilen, Statuszeilen), `haiku` (Zählungen, Zeilenenden); nie erben
   lassen. Web-Recherchen mit `general-purpose` und `model: sonnet`, nur die Physik-Bewertung mit `opus`. Vor jedem
   Agentenstart die Nutzung prüfen; Stopplinie des Anwenders: 87 % der Wochenmenge, danach Übergabe vorbereiten.
   Erfahrungswerte vom 08.10.: vier Recherche- und Inventaragenten zusammen rund 1 Prozentpunkt, ein Opus-Konzeptagent
   rund 1 Prozentpunkt.
3. **Welle:** Worktree ab origin (`git worktree add .claude/worktrees/<n> -b <n> origin/ios_migration_september`, dann
   `git lfs checkout`, sonst ist die Testdatenbank ein Zeiger) → Agent arbeitet und committet dort, pusht nicht → Merge
   origin → Gate (bei reinen Papieren: `dotnet build EPOS.Kern.Tests -c Release` und die drei Wachen
   `DokumentationLinkWacheTests`, `RepositoryOrdnungWacheTests`, `WikiProduktdatenWacheTests` mit `--filter`) →
   Statuszeile mit Nach-Block, Protokoll → `git fetch`, Nummer gegen origin messen (höchste `| **#…**` plus eins; am 06.10.
   wanderten eigene Nummern dreimal) → Push `git push origin <zweig>:ios_migration_september` ohne Rückfrage nach grünem
   Gate (nie `main`, nie Force) → CI-Nachweis → Platzhalter `{{PUSH}}`/`{{CI}}` der Statuszeile nachtragen (Commit mit
   `[skip ci]`, wenn nur der Vermerk kommt) → Worktree entfernen.
4. **CI-Nachweis:** Jeder fremde Push bricht den laufenden Kern-Lauf ab. Nachweis ist der erste grüne Lauf, dessen Stand
   den eigenen Push enthält (`git merge-base --is-ancestor <eigener> <Lauf-Stand>`); am 08.10. brauchte #819 drei Anläufe.
   Wächter: `gh run list --branch ios_migration_september --workflow kern.yml`, dann `gh run watch <id> --exit-status`
   in einer Hintergrundschleife, die bei „cancelled“ den jeweils jüngsten Lauf nimmt. Die `gh`-Anmeldung ist je Konto.
5. **Papiere:** Markdown UTF-8 ohne BOM, Arbeitsbaum CRLF (die Blobs sind LF-normalisiert, `core.autocrlf=true`); vor dem
   Bearbeiten Zeilenenden messen und beibehalten; Mockups im Markdown nur als Code-Spannen nennen (Wache Fall 4), nie als
   Markdown-Link; relative Verweise aus `aktuell/` auf Protokolle mit `../ueberholt/…`; keine Hersteller- und Produktnamen
   im Fließtext der Konzepte (Herstellerdokumente sind als Quelle zulässig), keine Kundennamen, keine Normtexte;
   Statusnummern-Kollisionen per Skript mit Zählprüfung umnummerieren (Statuszeile, Nach-Block, Protokoll, Index, Register).
6. **Werkzeuge:** Sichtproben der Berichtsseite mit dem Rasterprobe-Wirt und `berichtekostenprobe.mjs`
   ([`../../Proben/Rasterprobe/LIESMICH.md`](../../Proben/Rasterprobe/LIESMICH.md)); Word-Kopien eines Papiers mit
   [`Werkzeuge/MarkdownNachWord`](../../Werkzeuge/MarkdownNachWord/LIESMICH.md) (Windows mit Word; in der Cloud `pandoc`).
   Lange Befehle und Skripte mit dem Write-Werkzeug anlegen (Heredocs über rund 7 KB brechen ab); unter Windows `py` statt
   `python3`, `PYTHONIOENCODING=utf-8` davor; `dotnet test` meldet auf deutschem Windows „Bestanden!“, nicht „Passed!“.
7. **Nachbarn:** Lokale Sitzungen antworten über `SendMessage`; Cloud-Sitzungen (Gebäudesimulation, Dialoge und
   Korrekturen, Wirtschaftlichkeit, IFC/Gebäudeimport, Pufferspeicher) erreicht eine Nachricht nur einseitig, Antworten
   kommen über den Anwender. Schemaschritt-Fragen zwischen Sitzungen (wie 195/196 am 07.10.) über den Anwender klären.
8. **Lehren:** Sonnet-Papieragenten setzen relative Verweise gelegentlich eine Ebene zu kurz — Link-Wache vor dem Push;
   Agenten, die selbst Hintergrund-Teilagenten starten, enden vorzeitig — Teilaufträge selbst schneiden; ein Worktree-Ordner
   ist nach `git worktree remove` oft „busy“ (`dotnet build-server shutdown`, offene Browser-Tabs schließen, dann `rm -rf`);
   `sed -i` unter Git Bash macht aus CRLF LF (die Blobs bleiben gleich, der Arbeitsbaum nicht) — Papiere besser mit dem
   Edit-Werkzeug ändern.

## 3 Startprompt für die Fortsetzung im anderen Konto

```text
Du führst die Sitzung „Berichterstellung_Wirtschaftlichkeit“ von EPOS-Plan fort (Repo inekon/EPOS-Plan, Arbeitszweig
ios_migration_september, Anwender Dirk/Philipp Engelmann, INEKON). Antworten auf Deutsch, knapp, mit Entscheidtafeln.

Lies zuerst CLAUDE.md, dann vollständig Dokumentation/aktuell/Uebergabe_Cloud_Berichterstellung_2026-10-08.md und in
Dokumentation/aktuell/Status_iOS_Migration.md die Zeilen #757, #761, #762, #765, #811, #818, #819 und #826 samt
Nach-Blöcken. Dann: git fetch origin; höchste Statusnummer und die Kopfzeile „Schemaschritt angemeldet“ messen; Nutzung
prüfen (get_usage); aus der Übergabe ein Gedächtnis anlegen (Projektstand, Arbeitsregeln, offene Punkte); mir die
offenen Punkte aus Abschnitt 1.3 der Übergabe mit einem Vorschlag für den nächsten Schritt vorlegen. Nichts bauen oder
pushen, bevor ich den nächsten Auftrag gebe.

Arbeitsregeln: Fable 5.1 orchestriert nur und fasst Dateien nur für Ein-Zeilen-Nachzüge an; Agenten ausdrücklich mit
model opus (Rechenweg, Konzeptabsätze, Hüllen, Fehlersuche), sonnet (Suchen, Inventare, Textpflege, Protokolle,
Statuszeilen, Web-Recherchen), haiku (Zählungen, Zeilenenden); je Agent ein eigener Worktree ab origin mit git lfs
checkout; Builds nie parallel; Reihenfolge Merge → Gate → Statuszeile → Push → CI-Nachweis (erster grüner Kern-Lauf,
dessen Stand den eigenen Push enthält) → Platzhalter nachtragen → Worktree entfernen. Nach grünem Gate ohne Rückfrage
auf ios_migration_september pushen, nie main, nie Force. macOS-, iOS- und Setup-Läufe sowie Wiki-Uploads nur auf
ausdrücklichen Zuruf; Zugangsdaten nie in Dateien; keine Hersteller- und Produktdaten im Wiki, keine Kundennamen und
keine Normtexte im Repositorium. Vor jedem Agentenstart die Nutzung prüfen; Stopplinie 87 % der Wochenmenge, dann
Übergabe vorbereiten.
```

Die Statuszeile #826 ist die Übergabe selbst (siehe Nach-Block). Läuft die Fortsetzung in der Cloud, fehlt dort
Word; Word-Kopien dann mit `pandoc` oder beim Anwender erzeugen.

## 4 Übergabetext an die Sitzung Gebäudesimulation (vom Anwender weiterzureichen)

> Konzept Übergabegrenze und Bivalenz der Wärmepumpe: Fachkonzept `Dokumentation/ueberholt/Konzept_Uebergabegrenze_Bivalenz_EPOS-Plan.md`
> (Fassung 2, #818) und Umsetzungskonzept `Dokumentation/ueberholt/Umsetzungskonzept_Uebergabegrenze_Bivalenz_EPOS-Plan.md`
> (#819) liegen auf origin; alle Entscheide UB‑Q1 bis UB‑Q11 und U‑1 bis U‑4 sind eingetragen. Baubeginn UB‑E1 ist frei
> (zwei Opus-Wellen, erste Welle E1‑a in Abschnitt 10 des Umsetzungskonzepts). Vor UB‑E2 den Schemaschritt
> `UebergabegrenzeSchema` im Kopf der Statusdatei anmelden (203 war am 08.10. frei). Referenzprojekt 1060 als Kopie von
> 1056 in die CI-Auswahl, Basis danach als R44 neu einfrieren. Die neue Rechnung wirkt nur bei gesetzter `Einbindung`;
> Bestandsprojekte der Basis R43 bleiben unverändert.

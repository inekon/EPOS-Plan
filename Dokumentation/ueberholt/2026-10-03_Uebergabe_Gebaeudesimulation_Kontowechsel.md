# Übergabe der Sitzung „Gebäudesimulation“ — Stand 03.10.2026, 23:30 UTC (Kontowechsel)

Dieses Papier hält alles, was eine neue Cloud-Sitzung auf einem anderen Konto braucht, um die Sitzung „Gebäudesimulation“
ohne Verlust fortzuführen. Es ist eine Übergabe, kein Regelpapier: Regeln stehen in [`CLAUDE.md`](../../CLAUDE.md) und den
Projekt-`CLAUDE.md`; der dauerhafte Stand in der [Statusdatei iOS-Migration](../aktuell/Status_iOS_Migration.md) (Zeilen #690–#697
dieser Sitzung) und der [Statusdatei Gebäudesimulation](../aktuell/Status_Gebaeudesimulation_VDI6007.md) (Entscheide E62–E65, Stufentabelle).
Es wandert nach `Dokumentation/ueberholt/`, sobald die Fortsetzung die Wellen EV1 und AK1z abgeschlossen hat.

## 1. Stand beim Wechsel

| Was | Wert |
|---|---|
| Zweig | `ios_migration_september` (Remote `origin`, Standardzweig `main`) |
| zuletzt gepushter Stand dieser Sitzung | `c2a3c6795` (R34, #697, Gebäudevergleich-Nachzug); danach Merges der anderen Sitzungen bis `156b6fbca` |
| Referenzbasis | **R34** `Referenzlaeufe/2026-10-03_R34_Erdreich`, 18 Projekte (16 + 1051 + 1052), 548 CSV; Kopfzeile „Referenzbasis angemeldet: R35 — frei“ |
| Schemastand der Testdatenbank | **179** (83 169 280 Byte, LFS-OID `799da43a…`; die Sitzung „Simulation Pufferspeicher“ hat die Fassung 176 mit 1051 und 1052 auf 179 gehoben, Commit `6d0b5d3c0`), mit 1051 „Referenzprojekt Konditionierung“ und 1052 „Referenzprojekt Zonen“ |
| Schemaschritte | 177–179 (Sitzung „Simulation Pufferspeicher“) liegen auf origin; **180 von dieser Sitzung für EV1 angemeldet** (`Tab_Gebaeude.Erdreich_U_Wirksam`), hängt an 179; 181 frei |
| CI | `kern.yml` grün auf `ede4311f0` (Lauf 37158483778, enthält #695–#697); Windows-Nachtlauf (`windows.yml`) voraussichtlich rot in `GebaeudeEinzonennetzTests` (40 Abdrücke nur unter Linux erfasst, siehe 4) |
| Worktrees | keine; `AGENT_LAEUFT` liegt nicht |
| Parallel laufende Sitzungen | „Dialoge und Korrekturen“ (Schemaschritt 176, Bericht Strom/Kälte, Statuszeilen #693/#694/#698/#699) und „Simulation Pufferspeicher“ (P3/P4, #688/#689/#700–#704). Beide pushen auf denselben Zweig; Statusnummern und Schemaschritte werden über die Kopfzeilen der Statusdatei abgestimmt |

## 2. Was diese Sitzung am 03.10.2026 abgeschlossen hat

| Welle | Statuszeile | Inhalt | Protokoll / Papier |
|---|---|---|---|
| G6d | #690 | Referenzprojekt 1052 (Hotel aus 1018 in drei Zonen), Wache `ZonenReferenzprojektWacheTests`, KP3-Zonentests, Variantenfix | [Protokoll G6d](Protokolle/Gebaeudesimulation/2026-10-03_G6d_Zonenreferenzprojekt.md) |
| IFC-Quellendiagnose | #691 | sechs HottCAD-Dateien bis zur Rechnung, Beheizungsart je Raum, U ≤ 0, Kopplungswächter, Durchgangstests in der CI | Konzept Mehrzonen 6.5 |
| IFC-Vorschläge 2/3/5, RB-Z4 | #692 | Jahreswiderspruch, „getrennt beheizt“ nach Raumtemperatur, Wärmekapazität masseloser Schichten; Rechenwegbefund: Lastumkehr im Innern eines geregelten Abschnitts (`Zonenmodell2K.Schritt`), Fensterzweig A7a | E62 |
| KP3 RP1 (a–c) | #695 | Referenzprojekt 1051 (Bürobau Verw_I_40, Referenzbau 289, Büro-Kalender mit Nachtzeile), Wache `KonditionierungReferenzprojektWacheTests`, Plattformprobe, Messharness ρ_min, A/B-Protokoll; **P14 entschieden (E64)** | [A/B-Protokoll 1051](Protokolle/Gebaeudesimulation/2026-10-03_KP3_RP1_AB-Protokoll_1051.md) |
| RP2a | #696 | Messung der inneren Lastumkehr (0 Stunden in allen Referenzprojekten), allgemeine Innenprüfung (Obergrenze 8), **Erdreichwiderstand nach DIN EN ISO 13370** (`Erdreichwiderstand.cs`; Q −2 bis −23 %); Zusammenführung mit Schemaschritt 176; **Erdreich-Entscheid (E65)** | Referenzlaeufe/LIESMICH.md Abschnitt R34 |
| RP2b | #697 | Basis R34 eingefroren, R33 nach `ueberholt/Referenzbasen/`, Einfrierregeln „gesäte Zonendaten / Konditionierungsdaten / Erdreichdaten“, CI-Auswahl mit 1051, Gebäudevergleich-Tests auf R34 (Kennzeichen U-ERD) | — |

Entscheide des Anwenders vom 03.10.2026 (alle in der Entscheidtabelle der Statusdatei Gebäudesimulation):

- **E62:** Restlücke RB-Z4 (Lastumkehr mit zulässigem Stundenmittel) nur mit Einfrieren → mit RP2a/RP2b umgesetzt und gemessen (keine Wirkung).
- **E63 (angefordert):** Wärmeübergabe je Zone einstellbar → Welle AK1z, siehe 3.
- **E64 (P14):** keine pauschale Aufheizreserve; immer Nutzereingabe; leer = 20 % mit Laufhinweis → Welle EV1.
- **E65:** Erdreichkorrektur ist Standard; optional wirksamer U-Wert je Gebäude als Vorgabe → Welle EV1 (Schemaschritt 180).
- Modellwahl: Fable nur für Orchestrierung und Schwieriges, sonst Opus 5.5 / Sonnet 5.5 / Haiku 4.5 nach Aufgabe; Wochenlimit des bisherigen Kontos erschöpft (Halt bei 93 %).

## 3. Nächste Wellen in Reihenfolge (Aufträge als Wortlaut)

### 3.1 EV1 — wirksamer U-Wert der Bodenplatte als Vorgabe, Hinweis bei leerer Aufheizreserve (Opus, Schemaschritt 180)

Worktree `.claude/worktrees/ev1` (Zweig `ev1`) auf origin. Byte-gleich gegen R34 (Spalte NULL in allen Referenzgebäuden, kein Basiswechsel).

1. Schema `ErdreichVorgabeSchema` (Nummer 180, hängt an der Klasse von 179 — sie liegt auf origin): `Tab_Gebaeude.Erdreich_U_Wirksam` REAL NULL CHECK (> 0), ebenso `Tab_Gebaeude_STAMM`; Testdatenbank anheben und mit LFS committen; Nachtrag in `Referenzlaeufe/LIESMICH.md` (Schemastand); Kopfzeile der Statusdatei auf „181 — frei“ fortschreiben.
2. Kern: `Erdreichwiderstand` nimmt die Vorgabe als U_g (keine B′-Rechnung), `Erdreichkennwerte.Umfangsquelle` = „Vorgabe“, Export `Geb[n].Erdreich_Umfangsquelle = Vorgabe`; Katalogkopie → Projekt und Projektduplikat kopieren den Wert (`GebaeudeStammCtrl.SET_SPALTEN` prüfen).
3. Reservehinweis (E64): Laufhinweis `SIMENG_AUFH_RESERVE_VORGABE` (I, einmal je Projekt), wenn `Aufheizvorgabe.Reserve` leer: „Aufheizreserve nicht vorgegeben; es gelten 20 %.“ Herleitungszeile nennt Quelle „Vorgabe“; Ressourcen de/en, `designer_neu.py schreiben`.
4. Oberfläche: Gebäudedialog (Razor, Hülle in `EPOS.UI.Daten`) Feld „Wirksamer U-Wert Bodenplatte“ neben dem U-Wert der Grundfläche mit Hinweistext „leer = Erdreichkorrektur nach DIN EN ISO 13370“ und Auskunft B′/U_g gerechnet; Projektdialog: Platzhalter „Vorgabe 20 %“ am Feld Reserve mit Hinweistext. Texte in `MyResource.Resource.*`, beide Sprachen.
5. Tests: Schema (Rundreise, CHECK), Kern (Vorgabe greift, NULL rechnet, Kopierwege), Hinweis (leer/gesetzt), bunit beider Felder; Referenzlauf 18 Projekte gegen R34 byte-gleich; Windows-Schale auf Linux 0 Fehler.
6. Papiere: Mehrzonenkonzept Erdreichabschnitt (ein Absatz), Wiki-Quelle Gebäudedaten (nur Funktion), Logbuch-Satz; Statuszeile und Entscheid-Nachtrag macht die Orchestrierung. Aufwand rund 100–150 Agentenaufrufe.

### 3.2 AK1z — Wärmeübergabe je Zone (E63; Opus, Schemaschritt anmelden, 3–5 PT)

**Umgesetzt, siehe 7.** Der Auftrag bleibt hier im Wortlaut stehen.

Ist: Übergabeart, Exponent, Nennleistung, Auslegungspunkt, Proportionalband sind Gebäudefelder (`Tab_Gebaeude.Uebergabe_*`, `Heizkreis_Aktiv`); der Mehrzonenweg behandelt die Kopplung als ideale Last (Zustand GEKOPPELT). Soll: (1) Übergabespalten an `Tab_Zone` (NULL = wie Gebäude), Heizkurve und Vorlauf bleiben am Heizkreis des Gebäudes; (2) Mehrzonenweg rechnet die Übergabe je Zone am gemeinsamen Vorlauf (Schritt H je Zone), Rücklauf massenstromgewichtet, Einzonenweg und Basis byte-gleich; (3) Zonendialog Abschnitt „Übergabe“ je Zone mit Vorgabe „wie Gebäude“; (4) Ergebniszeile, Export E32, Bericht je Zone; (5) Einfrierregel „gesäte Auslegungsdaten der Übergabe“ um die Zonenspalten erweitert, 1052 mit einer abweichenden Zone gesät (dann Basiswechsel R35). Konzept Anlagenkopplung 8 und 10 und Mehrzonenkonzept nachziehen.

### 3.3 Danach, aus dem Teilkonzept Konditionierungsprofile und dem KP3-Entwurf

- **O1b/O2/O3:** Aufschlagfelder im Dialog, manuelle Aufheizzeit mit Vorschlägen (Assistent nutzt die Messharness `AufheizReserveMessungTests` als Vorbild), Abweichungsmerkmal im Bericht.
- **Welle A:** Teilkonzept nachziehen, N1.69, `BETRIEB_SQLITE.md`.
- **G6c-Rest:** Wiki-Quelle (Welle E), Rasterprobe GI/GJ mit dem Grundriss, Windows-Sichtabnahme der Wellen C und D und der sechs IFC-Dateien.
- **KP4:** Papiere, Wiki-Quellen, Logbuch (die Logbuch-Sätze für Gebäudeimport, Konditionierung, Erdreich liegen in den Statuszeilen; Version beim Anwender erfragen).
- **Kleinigkeiten:** Gebäudevergleich — zehn Gebäude stehen nach dem Erdreichwechsel bei „zu prüfen“ (Katalogtreffer 77–86 % unter dem Band 90 %); eine Ampelregel „U-ERD bei Katalogtreffer unter 90 % gilt als erklärt“ braucht den Anwenderentscheid. `Tab_Variante`-Zelle von 1050 steht als Variante von 1046 statt 1019 (Testdatenbank, Einfrierregel beachten). Index `Dokumentation/LIESMICH.md` nennt „36 entfernte Referenzbasen“, es sind 51. Heizsoll-Kalender von 1051 hat keine Ferienperiode (Befund RP1b 5; so gesät, zu entscheiden).

## 4. Offen beim Anwender

- Windows-Prüfsummen der 40 neu erfassten Abdrücke in `GebaeudeEinzonennetzTests` aus einem Windows-Lauf nachtragen (bis dahin `windows.yml` nachts rot).
- Sichtproben unter Windows: KP2/KP3-Dialoge, 1051 und 1052 im Projekt- und Zonendialog (Testdatenbank als `Kenndaten.sqlite` einsetzen, Anwendung aus Stand ≥ 176 bauen), die sechs IFC-Dateien mit den neuen Hinweisen.
- Logbuch-Version; Wiki-Upload gebündelt (Gebäudeimport, Pufferauslegung, Kühlung).
- Entscheid zur Gebäudevergleich-Ampelregel (3.3) und zu den Ferienperioden des Heizsoll-Kalenders 1051.

## 5. Arbeitsweise der Sitzung (bewährt, bitte beibehalten)

- **Rollen:** Fable orchestriert (zerlegen, abnehmen, zusammenführen, Rechenwegentscheide, Antworten); `opus-umsetzung` für Code, Schema, Tests, Hüllen, Konfliktauflösung; `sonnet-mechanik` für Statuszeilen, CI-Vermerke, Protokolle, LIESMICH, Einfrieren nach Rezept; `haiku-pruefung` für Zählungen, BOM, Konfliktmarker. Agentendefinitionen unter `.claude/agents/`; Modell bei jedem Aufruf ausdrücklich setzen.
- **Je Welle ein Worktree** (`git worktree add -b <welle> .claude/worktrees/<welle> HEAD`, dann `git lfs checkout Referenzlaeufe/Kenndaten_Test.sqlite`; 133 Byte = Zeiger). Agenten committen dort, pushen nie. Nach dem Merge Worktree entfernen (`git worktree remove --force`), sonst füllt sich die Platte (rund 15 GB frei; Ausgaben unter `/tmp` nach Auswertung löschen).
- **Reihenfolge:** Merge in den Hauptbaum → Gate → Statuszeile → Push → CI-Vermerk nachtragen. Gate = `Werkzeuge/Gate/gate_linux.sh <Nr> <Repo>` plus Windows-Schale auf Linux, `designer_neu.py` ohne Argument, BOM-Suche in Markdown, Konfliktmarker-Suche, und die drei Werkzeugtests der CI (`Werkzeuge/Formularkarte`, `Werkzeuge/Auslieferungsvorlage`, `Werkzeuge/Gebaeudevergleich`, je `dotnet test <sln> -c Release`) — rund 50 Minuten, im Hintergrund mit einem wartenden Befehl. Vor dem Push `git fetch origin` und mergen; Konflikte in der Statusdatei: beide Seiten behalten, Nummern nach origin ordnen.
- **Ressourcenkonflikte (`.resx`):** Fassung von HEAD plus fehlende `<data>`-Blöcke der anderen Seite vor `</root>`, BOM erhalten, keine Dubletten, dann `python3 Werkzeuge/ResourceDesigner/designer_neu.py schreiben`; `Resource.Designer.cs` nie von Hand mischen.
- **Testdatenbank im Konflikt:** immer die Fassung mit dem höheren Schemastand nehmen und die Saatskripte (`Referenzlaeufe/Skripte/referenzprojekt_1051_konditionierung.cs`, `…_1052_zonen.cs`) in dieser Reihenfolge neu fahren; beide `--trocken` = 0 Änderungen; `integrity_check`, `foreign_key_check`; vor dem Commit `git lfs ls-files` zeigt `*`.
- **Statuszeilen:** Nummer vor dem Schreiben gegen origin prüfen (`git show origin/ios_migration_september:Dokumentation/aktuell/Status_iOS_Migration.md | grep -o '^| \*\*#[0-9]*\*\*' | tail -1`); Zeile mit Betreff, Stand (Commits, Zahlen des Gates), Entscheiden, Befunden, „CI-Vermerk folgt“; Block „Nach #n“ mit nächsten Wellen, Punkten beim Anwender und „CI-Kennung nachtragen (offen)“. CI-Vermerk nach grünem Kern-Lauf (Lauf-ID und SHA); ein Folge-Push der anderen Sitzung bricht den eigenen Lauf ab — dann gilt deren grüner Lauf, wenn er den Stand enthält.
- **Entscheide des Anwenders** sofort als E-Zeile in `Status_Gebaeudesimulation_VDI6007.md` (vor `| Q24 |`), committen, pushen.
- **Rückfragepflicht:** vor jedem macOS-Läufer (`ios.yml`, `kern.yml` mit „macos“) und dem Setup-Lauf; vor Basiswechseln; bei Testbändern, die aufgeweitet werden müssten (nie eine Schwelle biegen, damit eine Zeile grün wird).
- **Aufwandsmaß:** rund 70 Agenten-Werkzeugaufrufe je Prozentpunkt Wochenlimit; eine Opus-Welle 100–200 Aufrufe (1,5–3 Punkte), Sonnet-Mechanik 10–50 (0,2–0,7). Beim Anwender regelmäßig den Zählerstand erfragen und vor einer Welle schätzen.

## 6. Prompt für die Fortsetzung auf dem anderen Konto

```
Du führst die Cloud-Sitzung „Gebäudesimulation“ des Repositoriums inekon/EPOS-Plan auf dem Zweig ios_migration_september fort.
Lies zuerst CLAUDE.md (Wurzel), dann Dokumentation/aktuell/Gebaeudesimulation/2026-10-03_Uebergabe_Gebaeudesimulation_Kontowechsel.md
vollständig, danach den Kopf und die Zeilen #690–#704 von Dokumentation/aktuell/Status_iOS_Migration.md sowie die Entscheide
E62–E65 und die Stufentabelle in Dokumentation/aktuell/Status_Gebaeudesimulation_VDI6007.md. Prüfe mit git log origin/ios_migration_september
-n 20, ob seit 156b6fbca weitere Stände der Sitzungen „Dialoge und Korrekturen“ und „Simulation Pufferspeicher“ liegen, und ob die
Schemaschritte 177–179 auf origin sind (Kopfzeile „Schemaschritt angemeldet“).

Arbeitsweise: Du (Fable) orchestrierst nur — zerlegen, abnehmen, zusammenführen, Rechenwegentscheide, Antworten an mich. Alle Umsetzung
läuft über die Agentendefinitionen unter .claude/agents/: opus-umsetzung (Code, Schema, Tests, Hüllen, Konflikte), sonnet-mechanik
(Statuszeilen, Protokolle, LIESMICH, Einfrieren nach Rezept), haiku-pruefung (Zählungen, BOM, Konfliktmarker); Modell bei jedem Aufruf
ausdrücklich setzen. Je Welle ein Worktree, Agenten committen und pushen nicht, höchstens rund 150 Werkzeugaufrufe je Auftrag. Reihenfolge
je Welle: Merge → Gate (Abschnitt 5 der Übergabe) → Statuszeile → Push → CI-Vermerk. Kein macOS-/Setup-Lauf ohne meine Freigabe. Antworten
knapp, auf Deutsch, Zahlen in Tabellen; keine Zwischenmeldungen ohne Neuigkeit.

Erste Aufgabe: Welle EV1 nach Abschnitt 3.1 der Übergabe (Schemaschritt 180 ist angemeldet; Entscheide E64 und E65). Danach AK1z nach
Abschnitt 3.2 (Schemaschritt vor dem Bau anmelden, Nummer aus der Kopfzeile). Vor jeder Welle den geschätzten Verbrauch nennen; ich gebe
den Stand des Wochenlimits an. Melde dich mit dem Ergebnis von EV1 (Gate-Zahlen, Referenzlauf 18/18 byte-gleich gegen R34, Push-SHA).
```

## 7. Nachtrag 04.10.2026 — EV1 abgeschlossen, Stand für AK1z

| Was | Wert |
|---|---|
| EV1 | umgesetzt, Statuszeile **#705**, Protokoll [`2026-10-04_EV1_Erdreichvorgabe_Reservehinweis.md`](Protokolle/Gebaeudesimulation/2026-10-04_EV1_Erdreichvorgabe_Reservehinweis.md); E64 und E65 stehen auf „umgesetzt“ |
| Schemaschritte | 180 `ErdreichVorgabeSchema` vergeben, Kopfzeile „181 — frei“; AK1z meldet 181 an |
| origin | `91bfdca6` (der Inhalt der Welle liegt als Sync-Commit `3765af5c` des Anwenders auf origin, die Testdatenbank 180 mit `91bfdca6`; die Einzelcommits der Statuszeile #705 stammen aus dem Bundle der Sitzung und sind auf origin nicht einzeln sichtbar); CI-Vermerk zu #705 trägt die Sitzung EV1 nach |
| Testdatenbank | Schemastand 180, 83 169 280 Byte, LFS-OID `0aa88998…` |
| Referenzbasis | R34 unverändert, 18/18 byte-gleich; AK1z friert R35 ein (1052 mit abweichender Zone) |
| AK1z | umgesetzt, Statuszeile **#708**, Protokoll [`2026-10-04_AK1z_Waermeuebergabe_je_Zone.md`](Protokolle/Gebaeudesimulation/2026-10-04_AK1z_Waermeuebergabe_je_Zone.md), Basis R35 `2026-10-04_R35_Zonenuebergabe` (19 Projekte, gesät ist die Kopie 1054 statt 1052, weil 1052 den Zonen-Aufheiznachweis trägt), Schemaschritt 181 `ZonenUebergabeSchema`; E63 steht auf „umgesetzt“ |

**Umgebung der Cloud-Sitzung — zwingend vor dem Start prüfen.** Die Netzrichtlinie der Umgebung muss
`lfs.github.com` (LFS-Verify beim Push), `builds.dotnet.microsoft.com` und `download.visualstudio.microsoft.com`
(`dotnet-install.sh`) erlauben; eine Freigabe gilt erst für neue Sitzungen. Ohne `lfs.github.com` lässt sich
keine Testdatenbank pushen — eine Welle mit Schemaschritt bleibt dann lokal und muss als Bundle an den Anwender
gehen. Fehlt das SDK trotz Setup-Skript, ist das Ubuntu-Paket `dotnet-sdk-10.0` (10.0.112) mit einem
Verzeichnis-Alias `sdk/10.0.400` (`ln -s`) und `DOTNET_ROOT` auf das Installationsverzeichnis ein tragfähiger
Ersatz: Kern-Filter, Tests und Referenzlauf (18/18, 548/548 byte-gleich) laufen damit; der grüne Kern-Lauf der CI
bleibt der Nachweis. Das Gate der Orchestrierung nach Abschnitt 5 (Linux-Gate, Windows-Schale, Designer,
SQL-Dialekt-Prüfer, die vier Werkzeugtests der CI, BOM-Suche, Konfliktmarker) liegt nicht im Repositorium und
wird je Sitzung im Scratchpad neu angelegt. Bestandsbefund des Gates: `Werkzeuge/Formularkarte/LIESMICH.md` und
`EPOS.iOS/CLAUDE.md` tragen ein BOM.

**Beim Anwender offen (zusätzlich zu Abschnitt 4):** Logbuch-Version für die zwei Sätze in #705, Sichtprobe der
EV1-Dialoge unter Windows (Bodenplattenfeld im Gebäudedialog, Reserve in der Projekteinstellung), Wiki-Upload der
Seiten „Gebäude“ und „Simulation“.

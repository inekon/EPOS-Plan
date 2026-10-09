# Übergabe der Sitzung „Gebäudesimulation“ — Stand 08.10.2026 (Kontowechsel)

Dieses Papier hält, was eine neue Cloud-Sitzung auf einem anderen Konto braucht, um die Sitzung „Gebäudesimulation“
ohne Verlust fortzuführen: Stand, offene Punkte, nächste Arbeit, Arbeitsweise und am Ende den Einstiegs-Prompt. Regelquelle
bleibt [`CLAUDE.md`](../../../CLAUDE.md) mit den Projekt-`CLAUDE.md`; der dauerhafte Stand steht in der
[Statusdatei iOS-Migration](../Status_iOS_Migration.md) (eine Zeile je Schritt, Blöcke „Nach #…“ darunter) und in der
[Statusdatei Gebäudesimulation](../Status_Gebaeudesimulation_VDI6007.md) (Entscheidungsregister E1–E108, Stufentabelle).
Es löst die [Übergabe vom 03.10.2026](../../ueberholt/2026-10-03_Uebergabe_Gebaeudesimulation_Kontowechsel.md) ab und wandert
nach `Dokumentation/ueberholt/`, sobald die Fortsetzung die Sichtabnahmen aus Abschnitt 5 und die Welle UB-E1 abgeschlossen hat.

## 1. Stand bei der Übergabe

| Was | Wert |
|---|---|
| Arbeitszweig | `ios_migration_september` (Remote `origin`). Der Sitzungszweig des alten Kontos `claude/inspiring-bell-b8wq90` wurde bei jedem Push mitgezogen; die neue Sitzung nimmt ihren eigenen Sitzungszweig |
| Letzte Welle | **Kühlkurve KK, #827** — gepusht `74129a1cb`, Kern-Lauf 37782520404 grün, CI-Vermerk `a9b138e44`; danach dieses Papier mit zwei Gate-Skripten (#833) |
| Referenzbasis | **R44** `Referenzlaeufe/2026-10-08_R44_Kuehlkurve`, 26 Projekte, 841 CSV; CI-Auswahl 1030, 1007, 1017, 1045, 1046, 1047, 1049, 1051, 1058. Nächste freie Basis **R45** |
| Testdatenbank | Schemastand **202**, 92 979 200 Byte, SHA-256 `19e38bc279c04b52…`; Referenzprojekte bis 1059 sowie 1061 (RP-KK) und 1062 (RP-KKZ); **1060 ist frei** und für die Übergabegrenze vorgesehen (Abschnitt 3) |
| Schemakette | 201 `Ak3KSchema` (#817), 202 `KuehlkurveSchema` (#827); **203 frei** |
| Statuszeilen | höchste vergebene beim Schreiben #833; Entscheide bis E108 (E108 gehört der Sitzung IFC), **E109 frei** |
| Laufende Arbeit | keine: kein Agent, kein Worktree im Repositorium, kein offener CI-Lauf, kein angemeldeter Schemaschritt, keine Routine dieser Sitzung |
| Lokale Zweige des alten Containers | `np5c-zuordnung-hilfe` (inhaltsgleich auf origin), `sicherung/pb1-cloud`, `sicherung/vor-601` (Sicherungen vom 29.09., überholt) — nichts davon wird gebraucht |
| Parallele Sitzungen | „IFC / Gebäudeimport“ und „Berichterstellung_Wirtschaftlichkeit“ sind am 08.10. ebenfalls übergeben worden ([IFC](2026-10-08_Uebergabe_IFC_Gebaeudeimport.md), [Berichterstellung](../Uebergabe_Cloud_Berichterstellung_2026-10-08.md)); „Dialoge und Korrekturen“ arbeitet weiter (#829, #830). Alle pushen auf denselben Zweig |

## 2. Was die Sitzung seit dem 03.10.2026 gebaut hat

Alle Zeilen mit grünem Gate gepusht; der CI-Nachweis steht in der jeweiligen Zeile oder in der Sammelzeile der Welle.

| Welle | Zeilen | Inhalt |
|---|---|---|
| EV1, KP4 | #705, #707 | wirksamer U-Wert der Bodenplatte als Vorgabe (Schritt 180), Reservehinweis; Papiere und Wiki-Quellen Konditionierung |
| AK1z | #708 | Wärmeübergabe je Zone (Schritt 181), Referenzprojekt 1054, R35 |
| G7b–G7e | #709–#712 | gbXML Stufe 2, 3D-Körperansicht, IFC-Export S1/S3, Round-Trip-Anreicherung |
| KU3 | #713–#728, #735 | Kältemaschine (Schritt 182), Zonenkühlung, Kältespeicher, Kältestromabrechnung, Referenzprojekt 1055 (R36), freie Kühlung über die Wärmequelle (Schritt 187) |
| MZ-Rest | #722, #725 | Trenndecke, Importproben 13–18, Kältespitze je Zone |
| AK2 | #732–#734 | Anlagenfahrplan (Schritt 186), Komfortkennzahlen, Referenzprojekt 1056 (R37) |
| VW1, Zonenmodell | #738, #743 | Ausweis der Vorlaufwahl (Schritt 188, R38); Abschnittsregel bei steifer Zone |
| NP1–NP5 | #744–#774 | Nutzungsprofile als Katalog (Schritte 189, 190), Blatt, Zuordnung, Zonenbaum, CSV-Austausch, DIN/TS 18599-10:2025 nur mit Nummer und Name (E96) |
| KP3 | #779–#794 | Aufheizoptimierung: Aufschlag, manuelle Aufheizzeit, Auslegungsheizlast ohne Kopplung (R39, E97), Bericht und Variantenvergleich, Ergebnisspalten (Schritt 194, E99) |
| AK3 | #796–#810 | geschlossener Kreis Gebäude ↔ Erzeuger, Heizungspuffer, Raumeinfluss der Heizkurve, Kennlinie über den Vorlauf interpoliert (Schritt 198), Referenzprojekt 1058 (R42, in der CI) |
| AK3-K | #817 | Zonensperre (nie Heizen und Kühlen am selben Tag), Kälteseite im Kreis, Kälteschranke (Schritt 201), Referenzprojekt 1059 (R43) |
| G5-Abnahme | #820, #824 | Abnahme des IFC-Imports am Rechenweg, A1–A6 erfüllt (E101) |
| KK | #827 | raumgeführte Kühlkurve auf AK3, Kühlübergabe je Zone ab AK1, Auslegungsweg wählbar (E105–E107, Schritt 202), Referenzprojekte 1061/1062 (R44); [Protokoll](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-08_KK_Kuehlkurve.md) |

## 3. Nächste Arbeit: Übergabegrenze und Bivalenz der Wärmepumpe (UB)

Die Sitzung Berichterstellung hat Fach- und Umsetzungskonzept fertig übergeben; den Rechenweg baut diese Sitzung
([Fachkonzept](../../ueberholt/Konzept_Uebergabegrenze_Bivalenz_EPOS-Plan.md), Fassung 2, #818;
[Umsetzungskonzept](../../ueberholt/Umsetzungskonzept_Uebergabegrenze_Bivalenz_EPOS-Plan.md), #819, Abschnitt 10 „Übergabe an die Sitzung
Gebäudesimulation“). Alle Entscheide UB-Q1 bis UB-Q11 und die Abweichungen U-1 bis U-4 sind eingetragen; der Baubeginn ist frei,
aber erst auf Auftrag des Anwenders.

- **UB-E1:** (umgesetzt 09.10.2026, #837) zwei Opus-Wellen — E1-a Kernklassen und Rechenproben (Wortlaut in Abschnitt 10), E1-b Abbildung, Dialog, Ressourcen.
- **UB-E3:** umgesetzt (09.10.2026, #852; Basis R46).
- **UB-E4:** umgesetzt (09.10.2026, #853; Basis R46 unverändert) — Bivalenzdiagramm, Reiter-Kachelzeile, Kennzahlen `wp.bivalenz.*`, Bericht, Export, Vorlagen auf Fassung 17; offen: UB-E5 (Wiki und Logbuch).
- **Vor UB-E2:** Schemaschritt `UebergabegrenzeSchema` im Kopf der Statusdatei anmelden (205, hängt an 204 `ZonenKatalogSchema`) und
  allein diese Zeile sofort pushen (umgesetzt 09.10.2026, #851; Basis R45 eingefroren).
- **Referenzprojekt 1060** als Kopie von 1056 (Fahrplan zurückgesetzt), in die CI-Auswahl, danach Basis **R45** einfrieren —
  der Übergabetext der Sitzung Berichterstellung nennt noch „R44“ und „Bestand R43“; beides ist durch die Kühlkurve überholt.
  Mit der Basis kommt eine neue Einfrierregel in die Wurzel-`CLAUDE.md`.
- Die neue Rechnung wirkt nur bei gesetzter `Einbindung` (U-1, Opt-in); alle Projekte der Basis bleiben byte-gleich.
- **Zähltests der Testdatenbank:** Ein neues Referenzprojekt ändert Bestandszählungen (zuletzt rot: `PreisbasisSchrittTests`
  mit 1061/1062, behoben in `4a4d27a0e`). Vor dem Push laufen deshalb alle `EPOS.Kern.Tests`, nicht nur die gefilterten.

## 4. Technisch offen, ohne Entscheid

- Schemastand-Wache im breiten Lauf nicht reihenfest (#721, #722; nicht untersucht).
- `Umschaltung` als Verfügbarkeitsgrund (Konzept Anlagenkopplung 7.4) und `GebaeudeModellErgebnis.Skaliert` ohne Zonen (#732).
- Komfortwerte je Gebäude ohne Datenbankspalte, der Bericht zeigt „—“ (#733).
- Freie Kühlung ohne Regressionsnetz (kein Referenzprojekt nutzt sie, #728).
- Feste deutsche Texte in `AbweichungsErmittler` (#790, Bestand).
- Sporadisch rot gesehen: `KalenderkarteInhaltTests` (Teppichbild, Zeitsteuerung), `GebaeudeImportZonenDialogTests`.
- [Konzept Heizlastspitzen-Glättung](2026-10-03_Konzept_Heizlastspitzen_Glaettung.md): Umsetzung nicht erkennbar, Entscheid
  beim Anwender klären.
- Papierpflege: Die Statusdatei Gebäudesimulation führt veraltete Statuswörter (E58–E60 „offen“, E67 „in Umsetzung“, Stufe NP
  „offen“, KP3 „teilweise“), obwohl die Wellen gebaut sind — mit der nächsten Statuspflege nachziehen (`sonnet-mechanik`).
- Aufräumen nach den Sichtabnahmen: [Entwurf AK3](2026-10-07_Entwurf_AK3.md), [Entwurf AK3-K](2026-10-07_Entwurf_AK3-K.md),
  [Entwurf KK](2026-10-08_Entwurf_KK_Kuehlkurve.md), [Entwurf KP3](2026-10-02_Entwurf_KP3.md) und
  [Übergabe KP3](2026-10-02_Uebergabe_KP3.md) nach `ueberholt/` (Verweise nachziehen, Indexzeile entfernen; die Konzepte
  Anlagenkopplung und Kühlung verweisen auf die Entwürfe). Die Befund- und Gegenlese-Papiere vom 15.–17.09. sind ebenfalls
  Kandidaten.

## 5. Offen beim Anwender

1. **Sichtabnahme unter Windows** (Testdatenbank als `Kenndaten.sqlite` einsetzen, Anwendung vom aktuellen Stand bauen):
   KP3 (Projekteinstellung, Gruppe „Aufheizung“, Bericht, Kurzbericht, Variantenvergleich), AK3 und AK3-K (Stufe AK3,
   Bedarfsdialog und Berichtstafel des Kreises, Reiter Konditionierung mit Freigabeband), KK (Gruppe Kühlkurve im Gebäudedialog,
   Kühlübergabe im Zonendialog, Tafel Kühlkurve), G6a; dazu die älteren Punkte der Blöcke „Nach #705“ bis „Nach #789“
   (Zonenübergabe 1054, Kältemaschinen- und Speicherdialog, Betriebszeiten, Nutzungsprofile).
2. **Versionsnummer für das Logbuch.** Entwürfe: Entwurf AK3 Abschnitt 10, Entwurf AK3-K Abschnitt 11, Entwurf KK Abschnitt 10,
   Konzept Konditionierungsprofile 10.5, Konzept Anlagenkopplung 12.4, Konzept Nutzungsprofile 6.3 und die Statuszeilen #705,
   #708, #735, #738, #774; für die Auslegungsheizlast (#782) fehlt der Satz noch.
3. **Wiki-Upload** gebündelt (Quellen unter `Projekte/Wiki/`: Gebäude, Gebäudemodell VDI 6007, Mehrzonenmodell,
   Nutzungsprofile, Kühlung, Grundlagen Kühlung, Simulation, Simulationsergebnisse, Energieerzeuger). Der WordPress-Connector
   des alten Kontos verband sich nicht (Fehler 502).
4. **E76 / Q29:** P17 am FZK-Haus −15,9 % statt −3,0 % — Bewertung „Weg 1“ (Mehrzonenkonzept 6.2), einzige offene Frage des
   Registers.
5. Werte-CSV der DIN/TS 18599-10 liegt allein beim Anwender (E96); Normwerte, Normtexte und Tabellen kommen nie ins
   Repositorium (ebenso keine Texte oder Werte aus VDI 4650 und DIN EN 14825). Normdateien unter `Quellen/` löschen und die
   Geschichte bereinigen nur auf ausdrücklichen Zuruf.
6. Windows-Prüfsummen der unter Linux erfassten Abdrücke in `GebaeudeEinzonennetzTests` aus einem Windows-Lauf (#705).

## 6. Arbeitsweise der Sitzung (über `CLAUDE.md` hinaus)

- **Rollen:** Die Sitzung orchestriert; `opus-umsetzung` für Rechenweg, Schema, Tests, Hüllen, Dialoge und Konflikte mit
  Fachinhalt; `sonnet-mechanik` für Statuszeilen, Protokolle, Inventare, Merges ohne Fachkonflikt; `haiku-pruefung` für
  Zählungen. Modell bei jedem Aufruf ausdrücklich setzen. Je Welle ein Worktree
  (`git worktree add -b <welle> .claude/worktrees/<welle> origin/ios_migration_september`, danach
  `git lfs checkout Referenzlaeufe/Kenndaten_Test.sqlite`); Agenten committen dort, pushen nie; fertige Worktrees entfernen.
- **Wellenfolge:** Entwurf mit Fragen → Anwenderentscheid als E-Zeile → Schemaschritt bzw. Basis im Kopf anmelden und allein
  pushen → Wellen → Merge im Hauptbaum auf aktuellem origin → Gate → Statuszeile und Protokoll → Push beider Zweige →
  CI-Vermerk mit `[skip ci]`.
- **Gate in der Cloud:** Ein Container-Neustart beendet den rund 75 Minuten langen Kern-Testlauf. Bewährt: Stand auf den
  Sitzungszweig pushen — die CI fährt dort die ganze `EPOS.Kern.Tests` samt ChartProben — und parallel lokal
  `Werkzeuge/Gate/gate_rest_linux.sh <Repo> <Ablage>` (rund 15 Minuten, Referenzlauf aller Projekte gegen die Basis mit
  Byte-Vergleich). Ist die CI rot, lässt sich das Protokoll über den GitHub-Zugang der Sitzung nicht laden (nur Annotationen);
  dann `Werkzeuge/Gate/kern_bloecke.sh` in zwei parallelen Aufrufen (rund 35 Minuten). Beschreibung in
  [`Werkzeuge/Gate/LIESMICH.md`](../../../Werkzeuge/Gate/LIESMICH.md).
- **Push:** `git -c "lfs.https://github.com/inekon/EPOS-Plan.git/info/lfs.locksverify=false" push origin <sha>:refs/heads/<zweig>`
  (sonst scheitert die LFS-Sperrprüfung); Commits signiert, Trailer nach `CLAUDE.md` und dem Sitzungshinweis.
  Reine Kopfzeilen-Pushes (Schemaschritt, Basis) ohne Checkout: Statusdatei aus `origin` lesen, Zeile ersetzen, mit
  temporärem Index (`GIT_INDEX_FILE`, `read-tree`, `update-index --cacheinfo`, `write-tree`, `commit-tree -S`) auf origin setzen,
  Betreff mit `[skip ci]`.
- **Statusnummern** vergeben drei bis vier Sitzungen gleichzeitig: unmittelbar vor dem Merge mit origin messen; ist die Nummer
  belegt, **vor** dem Merge in den eigenen Papieren umnummerieren (am 08.10. zweimal nötig: #825 → #826 → #827). Konflikte in
  der Statusdatei zeilenweise lösen: fremde Zeilen von origin, eigene dahinter. Ressourcenkonflikte: beide `<data>`-Blöcke
  behalten, keine Dubletten, `designer_neu.py` prüfen.
- **CI-Kontingent:** `kern.yml` bricht den überholten Lauf desselben Zweigs ab. Reine Papier- und Vermerk-Pushes mit
  `[skip ci]`; den doppelten Lauf auf dem Sitzungszweig nach einem Push beider Zweige abbrechen
  (`gh api -X POST repos/inekon/EPOS-Plan/actions/runs/<id>/cancel`). Warten auf Läufe mit einem einzigen Hintergrundbefehl.
- **Abgrenzung:** Der Import (`EPOS.Kern/Allgemein/Import/Ifc/`, `…/Import/Gebaeude/`, `…/Import/Sqproj/`) gehört der Sitzung
  „IFC / Gebäudeimport“; Befunde dorthin über ein Abstimmungspapier (Muster: [Abstimmung G5](2026-10-07_Abstimmung_G5_IFC.md)).
  Anwenderdateien (`.wpx`, `.sqproj`) werden nie committet; Werte und Namen aus ihnen gehören nicht in Code, Tests oder Protokolle.
- **Rückfragepflicht:** vor jedem macOS- oder iOS-Lauf und dem Setup-Lauf (jedes Mal), vor Basiswechseln ohne Auftrag, vor
  dem Aufweiten von Testbändern, vor Wiki-Uploads. Keine Pull Requests, kein Force-Push, keine Tags.
- **Ansprache:** Der Anwender steuert mit kurzen Zurufen — „fahre fort“ heißt: die laufende Welle nach der Wellenfolge zu Ende
  führen, einschließlich Push; „stand“ heißt: Stand in wenigen Zeilen. Antworten auf Deutsch, knapp, Zahlen in Tabellen;
  Entscheide als Fragen mit Empfehlung vorlegen.

## 7. Einstiegs-Prompt für die neue Sitzung

```
Du übernimmst die Sitzung „Gebäudesimulation“ für EPOS-Plan (Repo inekon/EPOS-Plan, Arbeitszweig ios_migration_september).
Lies zuerst CLAUDE.md und Dokumentation/aktuell/Gebaeudesimulation/2026-10-08_Uebergabe_Gebaeudesimulation_Kontowechsel.md
vollständig, dann den Kopf und die letzten zwanzig Zeilen der Tabelle in Dokumentation/aktuell/Status_iOS_Migration.md, die
Entscheide E100–E108 und die Stufentabelle in Dokumentation/aktuell/Status_Gebaeudesimulation_VDI6007.md sowie
`git log --format='%h %<(72,trunc)%s' -n 25 origin/ios_migration_september`.

Rolle: Du orchestrierst — zerlegen, abnehmen, zusammenführen, Rechenwegentscheide, Antworten an mich. Umsetzung über die
Agentendefinitionen unter .claude/agents/: opus-umsetzung (Rechenweg, Schema, Tests, Hüllen, Dialoge, Konflikte mit Fachinhalt),
sonnet-mechanik (Statuszeilen, Protokolle, Inventare, Merges ohne Fachkonflikt), haiku-pruefung (Zählungen); Modell bei jedem
Aufruf ausdrücklich setzen. Je Welle ein Worktree, Agenten committen und pushen nicht, höchstens rund 150 Werkzeugaufrufe je
Auftrag. Wellenfolge: Merge auf aktuellem origin → Gate (Werkzeuge/Gate/gate_rest_linux.sh lokal plus Kern-Lauf der CI auf dem
Sitzungszweig, Abschnitt 6 der Übergabe) → Statuszeile und Protokoll → Push beider Zweige → CI-Vermerk mit [skip ci].
Statusnummer, Schemaschritt und Basis unmittelbar vor dem Merge gegen origin messen. Nie ohne meine Freigabe: macOS-, iOS- und
Setup-Läufe, Wiki-Upload, Pull Requests, Force-Push, Tags. Normwerte und Normtexte nie ins Repositorium. Antworten auf Deutsch,
knapp, Zahlen in Tabellen.

Erste Schritte: (1) Stand prüfen — Zeilen #827 und #833 tragen ihren Vermerk, git status sauber, keine Konfliktmarker, keine
liegen gebliebene AGENT_LAEUFT, Kopf der Statusdatei: Schemaschritt 203 frei, Basis R45 frei, Projekt 1060 frei. (2) Den Stand in
höchstens fünf Zeilen melden. (3) Mit mir die offenen Punkte aus Abschnitt 5 abstimmen, zuerst Sichtabnahme, Versionsnummer für
das Logbuch und Wiki-Upload. (4) Die Übergabegrenze UB-E1 (Abschnitt 3; Umsetzungskonzept Abschnitt 10) mit Aufwandsschätzung
vorschlagen; ohne meinen Auftrag keine neue Welle beginnen.
```

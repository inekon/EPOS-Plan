# E7c2 — Phase‑1-Bericht des Bau-Agenten (Opus), 23.09.2026 14:20 (Zwischenbericht zu Punkten 1–5 siehe e7c2_zwischenbericht.md)

Alle neun Punkte gebaut und committet (Zweig `e7c2`, Worktree `.claude/worktrees/e7c2`, 11 Commits, kein Push). Getestet ist
noch nichts: kein `dotnet test`, kein Referenzlauf, kein SQL-Prüfer (Phase 2 nach „Tests freigegeben"). Kern-Filter und
Windows-Schale 0 Fehler, Designer gezogen, geänderte Dateien UTF-8 mit BOM und CRLF. Arbeitsbaum sauber, Testdatenbank nicht
committet (steht auf 105).

Commits: e924834d /1 Schritt E (107) · 66620b70 /2 Schritt F (108) · 8854ba56 /3 Schritt G (109) · 657abb4a /4 S‑2 · 4354b009 /5
B‑4 Rest · 2f0fc21f /6 V‑1/V‑2 · a339a633 /7 Q2 b · 3b6f54fe /8 Q1 · dcc875ac /9a KI-Feldkatalog und Berichtsspalten · ed4b3395 /9b
Rest der Überlagerung „Sätze und Herkunft" (U22) · bd866b4c /9c Ankertests.

Schema: Schritte 107 (E), 108 (F), 109 (G) an allen vier Stellen; `SchemaStand.Zielversion` 109; 106 bleibt Lücke (bis Nachzug des
Arbeitszweigs, der 106 der Dialog-Design-Welle #444 bringt).

Punkte 6–9:
- V‑1/V‑2: Mix der Einspeisevergütung ungerundet, gerundet nur der Erlös auf Cent; § 51a bei fester Vergütung mit der
  Einspeisevergütung, in der Direktvermarktung weiter mit dem anzulegenden Wert.
- Q2 b: In Fall 2 zählen die Vollbenutzungsstunden aus dem KWK-Strom (KWK-Strom ÷ P_el), auch auf dem Ersatzweg; je eine Hinweiszeile.
- Q1: Kürzungen unter 0,01 MWh nennen ihren Rundungsgrund in der Herleitung.
- Q7/9a KI-Feldkatalog: zwei neue Felder `anlage_abwaermeabfuhr`, `anlage_stromkennzahl`. Berichte: bei Fall 2 fünf Spalten mehr in
  der Modultafel (Word, Excel): Fall, σ, Nutzwärme, KWK-Strom, Kürzung; ohne Fall 2 elf Spalten, Wache mit zehn Word-Tabellen hält.
- Q7/9b Überlagerung: Anlagenart und Tatbestand mit Wirkung je Wahl; Satztafel Einspeisung, Eigenstrom, Kontingent, Deckel mit
  Vorschlag/Herkunft/eigenem Wert (leer = Vorschlag)/„gilt"; „Wirkung Jahr 1"; Energiesteuer (Projekt/nur diese Anlage, Entlastung,
  Aufteilung); Stromsteuer des Projekts; zweiter Knopf „Wahl und Herkunft…" in der Energiesteuer-Gruppe. Kern: Jahresbetrag des
  KWK-Zuschlags aus beiden KWKG-Reihen in die neue Klasse `KwkgJahresbetrag` verlegt; Lauf und „Wirkung Jahr 1" nutzen denselben
  Ausdruck, Ergebnisse bitgleich.

A/B: 13 Basisprojekte nach jedem Punkt 9.195/9.195 Werte gleich; Anker 1024 −2.896.359,13 €, 1030 −21.895.377,28 €.
V‑1/V‑2 (Proben mit fester Vergütung; kein Basisprojekt nutzt den PV-Vergütungsdialog):
| Probe | Größe | vorher | nachher |
|---|---|---|---|
| 1040 | Erlös Jahr 1 | 139,832 | 139,83 |
| 1040 | § 51a | 18,39 | 17,48 |
| 1045 | § 51a | 6,48 | 6,16 |
| 1046 | § 51a | 7,21 | 6,85 |
| Rechner 100 kWp | Jahr 1 | 3.207,96 | 3.209,02 |
| Rechner 100 kWp | § 51a | 427,60 | 401,13 |
Q2 b, Probe 1030 mit σ 0,5:
| Fall | Größe | vorher | nachher |
|---|---|---|---|
| σ gepflegt | Vbh | 7.475,69 h/a | 6.055,2 h/a |
| σ gepflegt | KWKG Jahr 1 | 6.137,94 € | 7.316,03 € |
| σ gepflegt | Kapitalwert | −21.904.948,06 € | −21.895.376,67 € |
| Ersatzweg | KWKG Jahr 1 | 6.395,35 € | 7.316,00 € |
| ohne Deckel | Jahr 5 | 1.057,55 € | 12.458,43 € |
9b: Basis gleich, alle Fall-2-Proben zahlengleich.

Schlüssel: 72 neu (de/en), darunter die sieben im Mockup geplanten `BHW_UEB_*`, 37 weitere `BHW_UEB_*`, fünf Spaltenköpfe
`WIRT_KWKG_SP_*`, vier Texte `WIRT_KWKG_FALL2_VBH*`/`WIRT_KWKG_FALL2_RUNDUNG*`; gestrichen `KOH_FALL5_MISCHLAGE`; kein bestehender
Text geändert (dazu aus Punkten 1–5: ERK_* (8), ND_TAFEL_ERSATZ_AUS/RESTWERT_AUS, KI_DLG_VOP_ERSATZ/RESTWERT_NAME/ERL,
ETV_PREISBASIS_OHNE_SPALTE, KOH_FALL5_MISCHLAGE_SPERRE, STEUER_ENERGIEST_54_MISCHLAGE).

Abweichungen: „Leer = Vorschlag" bei Kontingent und Deckel („Übernehmen" lässt das Feld leer, der Lauf leitet ab; bei den Sätzen
wird der Vorschlag ins Feld geschrieben); ein leeres Satzfeld (= 0, kein Zuschlag) erscheint in der Überlagerung als eigener Wert 0;
Word-Tafel mit Fall 2 hat 16 Spalten in 7 pt (fünfstellige MWh können umbrechen — Abnahme am Gerät); zwei E7c1-Tests auf die
jeweilige Wahlgruppe umgestellt.

Offene Fragen (gebaut ist jeweils a):
- E7c2‑Q1 S‑2: § 53-Seite zählt nur Anlagen mit Stromerzeugung (Kessel mit § 53-Wahl löst keine Sperre aus, alter Fall‑5-Hinweis
  entfällt dort) / b jede § 53/53a-Wahl sperrt. Empfehlung a.
- E7c2‑Q2 B‑4: a Bezugsgröße Arbeitskosten (Menge × Arbeitspreis) / b Gesamtkosten samt Grund-/Leistungspreis. Empfehlung a.
- E7c2‑Q3 F: neue Zuordnungen schreiben keine Preisbasis, leer = Abrechnungseinheit. Empfehlung: so lassen.
- E7c2‑Q4 G: Brennstoff 24 „Sonstige" bleibt m³. Empfehlung: so lassen.
- E7c2‑Q5 V‑1: Satz für die Speicherbewertung (`VpvCtKwh`) nimmt weiter den gerundeten Mix / b auch dort ungerundet (bewegt keine
  Basis). Empfehlung b, klein, in E7c3.
- E7c2‑Q6 V‑1: § 51a-Betrag bleibt ungerundet, nur der EV-Erlös gerundet / b auch § 51a auf Cent. Empfehlung a.
- E7c2‑Q7 Q2 b: bei bindendem Jahresdeckel verschwindet die Fall-2-Kürzung (Zuschlag = Deckel × P_el × Satz, Reihe 12 Jahre) / b Deckel
  auf die Bruttostunden. Empfehlung a (Wortlaut des Entscheids).
- E7c2‑Q8 Überlagerung Energiesteuer: Satz und Betrag nur für die gebuchte Wahl, übrige Wahlen als Text / b Vorschau je Wahl im Kern.
  Empfehlung b in E7c3.

Erledigt-Gründe (Vorschlag): A3 /4; A4 /6; A6 /1; A9 und U‑1 /3 (fünf Gase Nm³, Brennstoff 24 bleibt); ET‑D‑3 Rest und U32 /2;
§ 6.3 Nr. 9h und U39 teilweise (Entkopplung Ersatz/Restwert /1; offen: geräteeigene Dauerspalten, Anschluss der Speicherflotte);
R‑E7c1: Q1 /8, Q2 b /7, Q7 /9a und /9b.

Logbuchsätze (Version 1.2.0.4 beim Anwender bestätigen): (1) Je Investitionsposition lässt sich festlegen, ob eine
Ersatzbeschaffung geführt und ob ein Restwert angesetzt wird. (2) Die gewählte Preisbasis eines Energieträgers bleibt beim
Wiederöffnen erhalten. (3) Die Gase des Brennstoffkatalogs führen ihre Menge in Nm³. (4) Stehen Energiesteuerentlastungen nach
§ 53/§ 53a und nach § 54 nebeneinander, entfällt § 54, und eine Warnung nennt den Grund. (5) Betriebskosten in Prozent der
Brennstoff- oder Stromkosten beziehen sich auf den jüngsten Lauf. (6) Bei fester Einspeisevergütung rechnet die Photovoltaik mit dem
ungerundeten Vergütungssatz und bewertet § 51a EEG mit der Einspeisevergütung. (7) Bei einer Vorrichtung zur Abwärmeabfuhr zählen
die Vollbenutzungsstunden des KWKG-Kontingents aus dem KWK-Strom. (8) „Sätze und Herkunft…" im BHKW-Dialog führt alle Sätze,
Kontingent, Deckel, Energie- und Stromsteuer mit Vorschlag, Herkunft und Wirkung im ersten Jahr. (9) Die KWKG-Modultafel in Word
und Excel zeigt die Angaben zum zweiten Fall.

Phase 2 nach Freigabe: Tests (vorher testhost-Prüfung); rot erwartet nur Schemastand-Wache und Auslieferungsvorlage, solange die
Repo-Testdatenbank auf 105 steht; Referenzlauf 13 Projekte gegen R12 auf der migrierten Kopie, Designer-Prüfung, SQL-Prüfer.
Vorher Nachzug: `git merge ios_migration_september` (bringt 106 + Testdatenbank 106 + #445 mit 12 ADM_*-Schlüsseln; Designer neu
erzeugen; Kette 105 → 106 → 107 → 108 → 109 prüfen).

## Nachtrag 14:55 — E7c2/10 af450ed6 (Anwenderentscheid Q4: Brennstoff 24 „Sonstige" kWh statt m³)
Schritt 109 zieht Brennstoff 24 auf „kWh"/„€/kWh". Messung: Testdatenbank (105 und 109) und Anwenderdatenbank nutzen den Brennstoff
nirgends (0 Träger, 0 Preiszeilen, 0 Projektzuordnungen, 0 Umrechnungsregeln); Stamm Hi = Hs = 0 → keine Preisumrechnung nötig/möglich,
nur Einheitentext wie bei Strom (13) und Fernwärme (23). Fremde Träger/Preise des Brennstoffs 24 bleiben unverändert und werden im
Migrationsprotokoll gezählt; Nachprüfung zählt Brennstoff 24 mit; Tests in `GaseNormkubikmeterTests` ergänzt (nicht gelaufen).
A/B: Migration 105 → 109 meldet Einheit/Preiseinheit geändert, 0/0/0; 13 Basisprojekte 9.195/9.195 gleich; Auslieferungsvorlage baut mit
0 Auffälligkeiten; Kern-Filter, Schale, Testdatenbankschema 0 Fehler. Offener Punkt: Hi = Hs = 1,0 für Brennstoff 24 in einer späteren
Stufe. Phase 1 abgeschlossen; Phase 2 (Tests, Referenzlauf R12, Designer, SQL-Prüfer) offen.

## Phase‑2-Bericht 15:45 — Nachzug 13fff671, Umnummerierung, Tests, Referenzlauf
Merge c3eb2cfe „E7c2: Arbeitszweig 13fff671 in e7c2 zusammengeführt" (Konflikte inhaltlich: SchemaStand, SchemaMigration (4 Blöcke),
TestDatenbank, Werkzeuge/Testdatenbankschema — origin 106 (Dialog Design) und 107 (Gebäudesimulation E30 Ergebnistabelle je Gebäude)
zuerst, dann E7c2; resx beide Seiten, je 8.003 Einträge; Designer neu erzeugt). **Umnummerierung: E 107 → 108, F 108 → 109, G 109 → 110;
Zielversion 110; Kette 105 → 106 → 107 → 108 → 109 → 110 an allen vier Stellen**; `SchemaKatalog.Schritt108_ErsatzRestwertKennzeichen`,
`Schritt109_Preisbasis`; Ressource `ETV_PREISBASIS_OHNE_SPALTE` („vor 109"). E7c2/11 84b1effd: Testfix Brennstoff-24-Test
(`new DbParam("@a", (object)0)` statt Typ-Konstruktor). Builds 0 Fehler (Kern-Filter, Schale). Tests: gefiltert Kern 214, UI 500 grün;
voller Lauf Kern 5.054/5.055 (rot nur Schemastand-Wache, Repo-DB 107), UI 5.403, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27+1;
Auslieferungsvorlage 14/26 rot („107, erwartet 110"); Gegenprobe mit 110-Kopie: Wache 1/1, Auslieferungsvorlage 26/26 (dabei lief ein
fremder Testprozess aus Worktree agent-a79dc93d…, beide Läufe trotzdem grün). Migrationsprotokoll 107 → 110: 108 vier Spalten; 109 fünf
Zeilen kWh, 23 Abrechnungseinheit; 110 fünf Einheiten/Preiseinheiten, eine Preiszeile, Brennstoff 24 Einheit + Preiseinheit (0 Nutzer).
Referenzlauf auf 110-Kopie gegen R12: 13/13 PASS, 4.250.839 Werte, 399/399 CSV byte-gleich. SQL-Prüfer 1.705 Texte, 0 Fundstellen.
**E7c2/12 b943f534 (Orchestrierung): Testdatenbank 107 → 110** (LFS-SHA 8225443a…, 67 739 648 Byte, quick_check ok, 1030-Anlagen
NEUANLAGE erhalten, Gase Nm³, Brennstoff 24 kWh). Hinweis für Papiere: Phase‑1-Bericht und Commit-Betreffs E7c2/1–10 nennen noch 107/108/109.

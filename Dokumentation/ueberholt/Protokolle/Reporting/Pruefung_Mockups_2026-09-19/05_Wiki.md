# Prüfbericht 05 — Wiki-Folgen der Mockups

Stand der Prüfung: 19.09.2026. Reine Prüfung ohne Repo-Änderung, ohne Build/Test, ohne Wiki-Upload.

## 1. Kurzbefund

Die zehn Repo-Quellen unter `Projekte/Wiki/` sind inhaltlich sehr weit: von 39 geprüften Bedienstücken
aus Mockup und Statuszeilen sind 34 bereits mit eigenem Anker beschrieben, und die drei im Auftrag
genannten Verdachtsfälle auf überholte Aussagen — Knopf „Tarifstruktur…"/„Strombezug…", Knopf
„Verlauf…", Klappliste „Stammprojekt:" — erweisen sich bei Gegenprüfung als **nicht überholt**, weil
die zugehörigen Entscheidungen laut Statusdatei noch offen bzw. angehalten sind und die Wiki-Texte den
heutigen Programmstand richtig beschreiben. Fünf Lücken sind belegt und mit Satzvorschlag versehen:
die Doppelpflege-Warnung und die U23-Satzzeilen der Erlösrubrik fehlen teilweise, die beiden
Anlagenwarnungen des PV-Vergütungsdialogs und der Kohärenzhinweis „übernehmen ohne Stammprojekt"
fehlen ganz, und die Seite „Klimadaten" fehlt in der Bedienungsseiten-Tabelle des Konzepts, obwohl ihr
Wiki-Upload für den 28.09.2026 bereits vorgemerkt ist. Die Tabuwörter-Regex liefert 20 Treffer in den
Wiki-Quellen — bei Lektüre durchweg fachlich gemeint, keine Regelverletzung — und 38 Treffer in den
sechs Mockups, davon die meisten in Quellcode-Kommentaren; schwerer wiegt Regel 13.2, denn drei der
sechs Mockups (allen voran `Katalogfilter_Vorschlag.html`) zeigen reale Herstellernamen und Typcodes
aus der festen Wächterliste — Vaillant, Bosch, Buderus, Viessmann, LONGi, Philadelphia Solar, Growatt,
Ablytek, dazu Typcodes wie `CS6800iAW` —, was der Wächter nicht sieht, weil er nur Wiki-Quellen prüft.
Alle zwölf geprüften Dialoge tragen einen Hilfeschlüssel, doch keiner nutzt die im Konzept vorgesehene
ankergenaue Wiki-Zuordnung (`Kurzname#anker`), und der Schlüssel von `BhkwWirtschaftlichkeitDialog`
fehlt in `help_mapping.txt` ganz — seine Hilfe-Taste bleibt damit wirkungslos.

## 2. Methode

Gelesen: Konzept_Hilfesystem_Wikidokumentation.md (§ 6 Teil C, § 13, Abschnitt „Bedienungsseiten mit
Repo-Quelle" bis Dateiende); CLAUDE.md Abschnitt „Dokumentation"; alle elf `Projekte/Wiki/*.wiki`
vollständig oder gezielt gegriffen; Status_iOS_Migration.md Zeilen 264–287 (#343–#366) und 395–427
(„Nach #340" bis „Nach #367", zur Einordnung auch außerhalb des engeren Bereichs mitgelesen);
`Dialog_Formel_Zahlenprobe.txt` (Vorarbeit) und alle sechs Mockup-HTML-Dateien direkt (für Zeilenzahlen
maßgeblich, da die Textabzug-Nummerierung abweicht); `EPOS.Kern.Tests/WikiProduktdatenWacheTests.cs`;
zwölf Dialog-Razor-Dateien; `WindowsFormsApplication1/Allgemein/Hilfe/help_mapping.txt`.
Werkzeuge: `grep`/`perl -ne` (Git Bash) für Überschriften-, Anker- und Regex-Suche; die Tabuwörter-Regex
lief mit `close(ARGV) if eof` je Datei, weil `perl -ne` ohne diesen Kniff die Zeilennummer über alle
Dateien hinweg fortzählt (erst geprüft, dann korrigiert). Kein Datenbankzugriff — die Katalognamen der
Testdatenbank (`Referenzlaeufe/Kenndaten_Test.sqlite`) sind ungeprüft; wo das die Aussage einschränkt,
steht es bei der Zeile. Keine Datei geändert, kein Build, kein Test, kein Upload.

## 3. Wiki-Inventar (Aufgabe 1)

Je Seite die `==`-Überschriften mit Zeile und die Zahl der `{{Anker|…}}`-Marken (das einzige in diesem
Bestand verwendete Ankermuster — `<span id=…>` kommt nicht vor). Volle Namen:
`Projekte/Wiki/Programm Dokumentation - <Spalte>.wiki`.

| Seite | Zeilen | Hauptabschnitte (Zeile) | Anker |
|---|---|---|---|
| Kosten | 168 | Eingaben (6) · Bemessung der Positionen (21) · Energieträgerverwaltung (67) · Nutzungsdauern (AfA) (128) · … Kostendialogen (146) · Kosten in der Speicherauslegung (153) · Siehe auch (162) | 43 |
| Wirtschaftlichkeit | 71 | Eingaben (6) · Erlöse und Vorteile (37) · Nachweisblock und Vorschlag (47) · Bericht (55) · Siehe auch (64) | 34 |
| Stromspeicher | 504 | Eingaben (7) · Berechnungsart der Speichervariante (18) · Stromspeicher-Auslegung (49, mit Schritt 1–5 als Unterabschnitte bis 466) · Beim Verlassen (466) · Auf dem iPad (473) · Siehe auch (478) · Maske Lastspitzenkappung (487) · Berechnung (494) | 34 |
| Simulation | 105 | Simulation durchführen (9) · Reiter „Simulation" der Startseite (26) · Speicherflotte und Auslegung (38) · Ablauf im Überblick (47) · Eingaben (61, mit Simulation Konfiguration/Wärmequelle Erdreich/Wärmesenken) · Siehe auch (93) · Berechnung (102) | 17 |
| Hilfe-Assistent | 102 | Eingaben (6) · Der Assistent aus einem Dialog heraus (15, mit sechs Unterabschnitten 20–92) · Siehe auch (97) | 8 |
| Klimadaten | 79 | Klimaquelle (6) · Standort (15) · Jahr und Szenario (22) · Einlesen (32, mit „Wenn etwas nicht passt" 41) · Was gespeichert wird (47) · Hinweise zu den Quellen (56) · Adressen der Klimaquellen (63) · Siehe auch (74) | 7 |
| Photovoltaik | 49 | Eingaben (6) · Wechselrichter und Stränge (16, mit Die Ampel 25) · Siehe auch (39) · Berechnung (47) | 9 |
| Pufferspeicher | 41 | Eingaben (6, mit Katalog/Projektzuordnung 8, Pufferspeicher im Projekt 17) · Siehe auch (31) · Berechnung (39) | 11 |
| Simulationsergebnisse | 35 | Eingaben (6) · Speicherauslegung mit Kosten und Zeitreihen (16) · Siehe auch (23) · Berechnung (32) | 3 |
| Varianten | 32 | Eingaben (6) · Varianten am Projekt-Auswahlfeld (17) · Siehe auch (26) | 3 |
| Emissionen | 23 | Eingaben (6) · Siehe auch (16) | 2 |

Befund zum Bestand selbst: Alle zehn Seiten tragen den vorgeschriebenen unsichtbaren Kopfkommentar
(Wikititel, Repo-Pfad, Pflegeregel). **Klimadaten ist die einzige der elf Dateien ohne Eintrag in der
Bedienungsseiten-Tabelle** des Konzepts (Zeilen 879–893) und ohne Erwähnung im Revisionsteil (835–970);
das ist kein Fehler der Seite selbst, sondern ein Nachtrag, der laut „Nach #367 (g)" erst mit dem
Sammel-Upload am 28.09.2026 fällig wird („Upload ausstehend: neue Seite ‚Programm Dokumentation/
Klimadaten'"). Empfehlung: die Konzept-Tabelle beim nächsten Editieren um diese Zeile ergänzen.

## 4. Abgleich Bedienung ↔ Wiki (Aufgabe 2)

Kurzform der Seiten in der Tabelle: **K** = Kosten.wiki, **W** = Wirtschaftlichkeit.wiki,
**V** = Varianten.wiki. Schwere: hoch/mittel/gering/keine (kein Handlungsbedarf).

### 4.1 Kostenverwaltung

| Bedienstück | Quelle | Seite · Anker · Zeile | Befund | Änderung | Schwere |
|---|---|---|---|---|---|
| Herleitungszeile | Mockup U28, #345 | K · `herleitung` · Z. 60 | beschrieben | – | keine |
| Dreiteiliger Summenfuß | Mockup U29, #345 | K · `summen` · Z. 18 | beschrieben | – | keine |
| Tafel „Ersatz und Restwert" | Mockup U30, #357 | K · `ersatz-restwert` · Z. 17 | beschrieben | – | keine |
| Knopf „Nutzungsdauern vorbelegen…" | #357 | K · `nutzungsdauer-vorbelegen` · Z. 147–149 | beschrieben | – | keine |
| Positionsart | #357 | K · `nutzungsdauer-vorbelegen` · Z. 150 | beschrieben | – | keine |
| Schloss an Pflichtzeilen | Mockup U31, #347 | K · `betriebspositionen` · Z. 10 | beschrieben | – | keine |
| Empfehlungszeile | Mockup U31, #347 | K · `empfehlung` · Z. 64 | beschrieben | – | keine |
| Laufstand | #347 | K · `betriebsmengen` · Z. 63 | beschrieben | – | keine |
| Endenergie je Komponente | #347, #349 | K · `betriebsmengen` · Z. 63 | beschrieben (auch Elektrokessel-Fall) | – | keine |
| **Doppelpflege-Warnung** | #347 (`KOH_HILFSENERGIE_DOPPELT`) | K · (kein Anker) | **fehlt** | neuer Satz bei `laufgroessen`/`betriebsmengen`: „Bewertet eine Position der Hilfsenergie dieselbe Menge, die eine Anlage mit eigenem Energieträger bereits einpreist, steht über dem Raster ein Warnband, das auf die doppelte Pflege hinweist." | mittel |
| Vorlagenhinweis mit „übernehmen" | #366 | K · `vorlagenhinweis` · Z. 65 | beschrieben | – | keine |
| kWp-Bemessung im Betriebsraster | Mockup U33, #350 | K · Bemessungsabschnitt · Z. 56 | beschrieben | – | keine |
| Kennzahl €/kWp | Mockup U34, #350 | K · `summen` · Z. 18 | beschrieben | – | keine |
| kWp-Herleitung | Mockup U35, #350 | K · Bemessungsabschnitt · Z. 56 | beschrieben | – | keine |
| Übernahme als Katalogansicht | #363 | K · `uebernahme-vorlage` · Z. 15 | beschrieben (mit #363 erweitert) | – | keine |
| Bemessung je kW (7 Gewerke) | #356 | K · Bemessungsabschnitt · Z. 58 | beschrieben | – | keine |
| Elektrokessel-Rangfolge | #349/#353/#356 | K · `stromtraeger` · Z. 85 | beschrieben (schließt die in „Nach #353 (a)" notierte Lücke, ergänzt mit #356) | – | keine |
| Bezugsgrößen aus gespeicherten Läufen + vier Gründe | #364 | K · `ohne-bezugsgroesse`/`betriebsmengen` · Z. 62–63 | beschrieben | – | keine |

### 4.2 Energieträger

| Bedienstück | Quelle | Seite · Anker · Zeile | Befund | Änderung | Schwere |
|---|---|---|---|---|---|
| Preisbestandteile in der Abrechnungseinheit | Mockup Abschn. 4 | K · `preisbestandteile` · Z. 120 | beschrieben | – | keine |
| Schnellwahl | Mockup Abschn. 4 | K · Z. 124 (innerhalb `preisbestandteile`) | beschrieben | – | keine |
| Emissionsblock | Mockup Abschn. 4 | K · `emissionsblock` · Z. 98 | beschrieben | – | keine |
| Preisbasis mit zwei Einträgen | Mockup Abschn. 4 | K · `preisbasis` · Z. 92 | beschrieben | – | keine |
| Preishistorie | Mockup Abschn. 4 | K · `preishistorie` · Z. 96 | beschrieben | – | keine |
| Katalogwerte übernehmen | Mockup Abschn. 4 | K · `katalogwerte-uebernehmen` · Z. 97 | beschrieben | – | keine |

### 4.3 BHKW-Dialog

| Bedienstück | Quelle | Seite · Anker · Zeile | Befund | Änderung | Schwere |
|---|---|---|---|---|---|
| Vorschlagsknöpfe am Feld | Mockup Abschn. 5, #352 | W · `kwk-vorschlag` · Z. 30 | beschrieben | – | keine |
| Satzherkunft mit vier Nachkommastellen | #352 (U26) | W · `kwk-vorschlag` · Z. 30 | beschrieben (Beispiel „6,0000 ct/kWh — Vorschlag 5,5667 ct/kWh") | – | keine |
| Modus § 9 Nr. 3 | Bestand | W · `stromsteuer-modus` · Z. 26 | beschrieben | – | keine |

### 4.4 PV-Vergütung

| Bedienstück | Quelle | Seite · Anker · Zeile | Befund | Änderung | Schwere |
|---|---|---|---|---|---|
| Herkunftszeile | #359 | W · `pv-verguetung` · Z. 32 | beschrieben | – | keine |
| „eigene Werte" | #359 | W · `pv-verguetung` · Z. 32; V · `pv-verguetung` · Z. 12 | beschrieben | – | keine |
| **Beide Anlagenwarnungen** | Mockup U36, #350 | W · `pv-verguetung` (kein Unterpunkt) | **fehlt** | neuer Satz: „Nähert sich die Anlage der Ausschreibungsgrenze oder der Grenze der Stromsteuerbefreiung nach § 9 Abs. 1 Nr. 3 StromStG, warnt der Dialog rechtzeitig vor der jeweiligen Schwelle." | mittel–hoch |
| Kohärenzhinweis „übernehmen ohne Stammprojekt" | #360 (`KOH_PV_STAMM_FEHLT`) | W/V · (kein Anker) | **fehlt** | Ergänzung bei V `pv-verguetung`: „Übernimmt eine Variante die Vergütung, ohne dass ihr Stammprojekt noch besteht oder eine Verknüpfung führt, weist ein Kohärenzhinweis darauf hin." | gering |

### 4.5 Ergebnisseite

| Bedienstück | Quelle | Seite · Anker · Zeile | Befund | Änderung | Schwere |
|---|---|---|---|---|---|
| Referenzwahl | #358 | W · `referenz` · Z. 9 | beschrieben | – | keine |
| Vergleichssicht mit A/B und Tausch | #358 | W · `vergleichssicht` · Z. 12 | beschrieben | – | keine |
| KWKG-Pauschale im Jahr 0 | #346 (U17) | W · `block-a` · Z. 41 | beschrieben | – | keine |
| **Erlösrubrik Block A/B mit Satzzeilen** | #352 (U23) | W · `block-a`/`block-b` · Z. 41–42 | **teilweise** — Block A/B selbst beschrieben, die Satzzeilen „… · Vorschlag/eigener Wert" unter Einspeisung/Eigenverbrauch fehlen | Ergänzung bei `block-a`: „Unter den Positionen Einspeisung und Eigenstrom steht je eine Zeile mit dem angesetzten Satz und seiner Herkunft — Vorschlag oder eigener Wert." | gering–mittel |
| Energiekosten je Anlage | Bestand | W · `energiekosten-je-anlage` · Z. 45 | beschrieben | – | keine |
| Nachweise nach Neuladen | Bestand | W · `herleitungen-gespeichert` · Z. 50 | beschrieben | – | keine |
| Nutzungsdauer-Hinweiszeile | „Nach #357 (a)" (U39) | W · (kein Anker) | fehlt — **zu Recht**: bleibt laut Statusdatei in der Windows-Schale, eigener Auftrag noch offen | keine (erst nach Umsetzung) | keine |
| Empfehlung/Einstufung | Bestand | W · `vorschlag` Z. 53, „Einstufung" in `bericht` Z. 57 | beschrieben | – | keine |
| Annahmen der Szenarien | Bestand | W · `bericht` · Z. 57 | beschrieben | – | keine |
| p_I | Bestand | W · `parameter` · Z. 20 | beschrieben | – | keine |
| Nicht monetäre Wirkungen | Bestand | W · `nicht-monetaer` · Z. 24 | beschrieben | – | keine |
| Bericht mit Deklarationszeile | #358 | W · `bericht-sicht` · Z. 60 | beschrieben (Inhalt vorhanden: „Referenz dieser Bewertung: A · …"; der Begriff „Deklarationszeile" selbst kommt nicht vor) | optional, nur Begriffsschärfe — kein Muss | gering |

### 4.6 Verdachtsfälle auf überholte Aussagen — Ergebnis: keiner überholt

| Aussage | Seite · Zeile | Prüfergebnis |
|---|---|---|
| Knopf „Tarifstruktur…"/„Strombezug…" | W Z. 33 (`strombezug`) | Laut „Nach #291" ist der Rückbauwunsch **angehalten statt umgesetzt** (drei fachliche Wege offen, Entscheid steht aus) — der Knopf „Strombezug…" besteht im Programm unverändert; die Seite trifft den heutigen Stand. Kein Handlungsbedarf. |
| Knopf „Verlauf…" | W Z. 34 (kein eigener Anker) | Laut „Nach #346 (a)" ist der Umbau nach Konzept § 2.13/K8 (Umschalter Kennzahlen/ValERI, Knopf entfällt) **nicht umgesetzt** — `KapitalwertVerlaufDialog` und Seite zeigen weiter beide Bilder. Beschreibung passt; bei Umsetzung von K8 muss die Zeile entfallen (Merkposten für die nächste Wiki-Runde). |
| Klappliste „Stammprojekt:" | K Z. 19 (`ertragbonus`) | Die Wiki-Seite nennt bereits korrekt „Projekt:" (nicht „Stammprojekt:"), passend zu VV‑Q7. Die veraltete Bezeichnung „Stammprojekt:" steckt allein im **Mockup** (offen laut „Nach #355 (b)"), nicht im Wiki. Kein Handlungsbedarf am Wiki. |

Zusätzlich gegengelesen und ohne Befund: alte Fußleisten (keine Fußleisten-Beschreibung in K/W
gefunden, die nicht zur heutigen Fußleiste passt).

## 5. Wächterregeln (Aufgabe 3)

### 5.1 Tabuwörter-Regex

Muster: `seit (dem|der|W)|geändert|Entscheid|Befund|W\d+[a-z]?[‑-][A-Z][‑-]\d+|Stand:? *\d|bisher|
früher|vorher|Bis dahin|Migrationsschritt`, mit `perl -CSD -ne` je Datei einzeln gezählt.

**Wiki-Quellen — 20 Treffer, alle fachlich gemeint, keine Regelverletzung:**

| Datei:Zeile | Fundwort | Bewertung |
|---|---|---|
| Hilfe-Assistent.wiki:76 | „bisher" | Beschreibt das Vorher/Nachher-Feld des Bestätigungsblocks selbst (Programmtext), nicht die Doku-Historie. Fachlich, stilistisch aber leicht vermeidbar (→ „welcher aktuelle, welcher neue Wert"). |
| Hilfe-Assistent.wiki:84 | „vorher" | Ablaufwort: die Sicherungskopie entsteht *vor* dem Speichern. Fachlich. |
| Hilfe-Assistent.wiki:88 | „bisherige" | „der bisherige Ergebnisstand" = der zum Abbruchzeitpunkt vorhandene Stand. Fachlich. |
| Photovoltaik.wiki:31, 32, 35 | „Befund" (3×) | Bezeichnet die Ampel-Diagnosesätze des Programms (technischer Terminus), nicht einen Projektbefund. Fachlich. |
| Simulation.wiki:24 | „vorher" | Ablaufwort vor dem Verlassen der Konfiguration. Fachlich. |
| Simulationsergebnisse.wiki:13 | „Befund" | „Befund nach VDI 4640" — Norm-Fachbegriff. Fachlich. |
| Stromspeicher.wiki:206, 215, 380, 436, 462, 492 | „bisherige"/„vorher"/„Befund" | Durchweg Ablauf-/Diagnosewörter der Bedienung (Prognosezeitpunkt, veraltetes Ergebnis, Vorher/Nachher-Kachel, Ampel-Befund, Rückfrage vor Wechsel). Fachlich. |
| Wirtschaftlichkeit.wiki:4, 11, 53, 57, 58, 66 | „Entscheid…" (6×) | Ausnahmslos Teil der feststehenden Fügung „Vorschlag zur **Entscheidung**" (DIN-EN-17463-Begriff) bzw. „Stammprojekt ist stets gesetzt". Substring-Treffer von „Entscheid", kein „Anwenderentscheid". Fachlich. |

Kosten.wiki, Emissionen.wiki, Klimadaten.wiki, Photovoltaik.wiki (Rest), Pufferspeicher.wiki und
Varianten.wiki liefern **0 Treffer** — sauber.

**Mockups — 38 Treffer über sechs Dateien**, Einordnung Kommentar (nicht sichtbar) / Haupttext
(sichtbar) / Anhang (nur `Dialog_Formel_Zahlenprobe.html`, ab Z. 4344 „Anhang · Umsetzungsstand"):

| Datei | Treffer | Einordnung |
|---|---|---|
| Dialog_Formel_Zahlenprobe.html:2874 | „vorher" (Dialogtext „bitte vorher übernehmen") | Haupttext, sichtbar, Ablaufwort — unproblematisch |
| Dialog_Formel_Zahlenprobe.html:4348 | „Entscheid ausstehend" | **Anhang** — die dort selbst definierte Statusmarke „die einzige Stelle für Nichtgebautes"; bestimmungsgemäß |
| Entwurf_Hydraulikuebersicht_Konfiguration.html:27 | „Stand 15.08.2026 · Änderungen gegenüber v1" | Haupttext, sichtbare Kopfzeile des Entwurfs — echte Stand/Änderungs-Formel, aber ein reines Entwurfsdokument, keine Wiki-Quelle; unkritisch, solange der Satz nicht mit in eine `*.wiki`-Datei wandert |
| Ergebnis_Bandbreite_Herkunft.html:229 | „/* Befund */" | Quellcode-Kommentar (CSS-Klasse), nicht sichtbar |
| Ergebnis_Bandbreite_Herkunft.html:312, 434, 453, 456, 480, 1839 | „Entscheidung" (6×) | Haupttext, sichtbar, durchweg „Vorschlag zur Entscheidung"/„Entscheidungskriterium der Norm" — derselbe DIN-Begriff wie im Wiki; unproblematisch |
| Ergebnis_Bandbreite_Herkunft.html:677 | „bisher" | Haupttext, sichtbar, Bildunterschrift zu einer Diagrammeinschränkung („liest sie in diesem Bild bisher nur nicht"); grenzwertig — eher Werkzeug-/Renderer-Notiz als Dialogtext, für eine Wiki-Übernahme umzuformulieren |
| Katalogfilter_Vorschlag.html: acht Zeilen (8, 36f., 212, 286, 307, 409, 413, 433, 1465) | „Anwenderentscheid W14a-E-10", „vorher", „Entscheid(s)", Wellen-/Fragecodes | **Quellcode-Kommentare** (`<!-- -->`/`/* */`), nicht sichtbar — reine Entwicklernotizen |
| Katalogfilter_Vorschlag.html:752, 754 | „Befund D‑2", „Befund D‑1" | Haupttext, **sichtbar** (`<li><b>Befund …</b>`) — technischer Prüfbefund einer Datenprobe im Mockup selbst, kein Doku-Änderungsvermerk; fachlich vertretbar, aber datiert/dev-nah formuliert |
| Wechselrichter_Mockup_2026-09-06.html: sechs Zeilen (8, 303, 410, 490, 492, 522, 1387) | „Anwenderwunsch W6-E-2", Entscheidungsfrage-Codes | Sieben von sieben **Quellcode-Kommentare**, bis auf Z. 410, die als sichtbarer Klammerzusatz „(Empfehlung zu Entscheidungsfrage W6‑E‑2‑Q5)" im Fließtext steht — bei Wiederverwendung des Texts zu entfernen |
| stromspeicher-optimierung-v2.html:15 | „bisherigen" | Kopfkommentar (`<!-- -->`), nicht sichtbar |
| stromspeicher-optimierung-v2.html:964, 998, 1085 | „bisher"/„vorher" | Haupttext, sichtbar, Ablaufwörter („wie bisher die Kandidatentabelle", „fragt vorher nach") — unproblematisch |
| stromspeicher-optimierung-v2.html:1119 | „Entscheid vom 12.09.2026" | Haupttext, sichtbar, Fußnote mit Entscheidcode SD‑Q15 — Entwickler-Begründung im Mockup selbst, bei Wiederverwendung zu entfernen |

Fazit 3a: Kein Treffer verletzt Regel 13.1 **in den Wiki-Quellen selbst**. In den Mockups sind die
kritischen Formulierungen (Entscheidcodes, „Stand:"-Zeile) fast ausnahmslos Quellcode-Kommentare, die
nie gerendert werden; die wenigen sichtbaren Fälle (Wechselrichter Z. 410, Stromspeicher-Opt. Z. 1119,
Ergebnis_Bandbreite Z. 677) sind Entwickler-Fußnoten im Mockup selbst und dürfen so nicht unverändert
in eine `*.wiki`-Datei übernommen werden.

### 5.2 Produktdatenregel 13.2 — von Hand gegen die sechs Mockups geprüft

Muster aus `EPOS.Kern.Tests/WikiProduktdatenWacheTests.cs`: feste Herstellerliste `FesteHersteller`
(53 Einträge, u. a. Viessmann, Vaillant, Bosch, Buderus, Wolf GmbH, LG Electronics, Growatt, SMA Solar,
Fronius, Huawei, BYD, Tesla, Senec, Kostal, Daikin, Mitsubishi, LONGi, VARTA, Dachs, Vitocal/Vitodens/
Vitobloc/Vitovalor, ecoTEC, aroTHERM, geoTHERM, Ablytek, Philadelphia Solar, Shenzhen …), Typcode-Muster
`[A-Z]{1,3}\d{3,}[A-Za-z0-9+\-]*` (ohne Leerzeichen/Bindestrich vor der Zahl, ohne Normkürzel-Präfix
DIN/EN/ISO/IEC/VDI/TRY), Platzhalter „Muster/test/xxx/meins"/„EPOS-Plan Referenz". Geprüfte Pfade laut
Regel 13.2: `EPOS.Kern/Allgemein/Hilfe/Berechnung/*.wiki` und `Projekte/Wiki/*.wiki` — **die sechs
Mockups unter `Dokumentation/aktuell/Mockups/*.html` liegen außerhalb des Wächter-Geltungsbereichs**,
weshalb dieser Abschnitt sie von Hand prüft. **Ohne Datenbankzugriff**: die Katalognamen der Testdatenbank
`Referenzlaeufe/Kenndaten_Test.sqlite` sind ungeprüft geblieben; was dort zusätzlich als Produktname
geführt wird, kann hier nicht erkannt werden.

| Datei | Feste Hersteller (Treffer) | Typcodes (Beispiele) | Bewertung nach 13.2 |
|---|---|---|---|
| Katalogfilter_Vorschlag.html | Vaillant (~40×), Buderus, Viessmann, Bosch (~8×), STIEBEL ELTRON, LONGi (~50×), Philadelphia Solar, ecoTEC | `CS5800i`, `CS6800iAW`, `CS7800iLW`, `GBH192i-15`, `HTM600MH8-60`, `M144` | **Nicht erlaubt**, wäre es eine Wiki-Seite: eine gerenderte Katalogtabelle mit realen Herstellernamen, realen Typbezeichnungen (z. B. „ecoTEC plus VC 10CS/1-5", „Vaillant Deutschland GmbH & Co. KG") und Datenblattwerten (Nennleistung, Wirkungsgrad bis auf drei Nachkommastellen). Höchste Dichte aller sechs Mockups. |
| Wechselrichter_Mockup_2026-09-06.html | Ablytek (4×) | `CS6800iAW` (Vergleichszeile) | **Überwiegend erlaubt**: fast alle Beispiele nutzen bewusst den Platzhalter „Muster 2500TL"/„Muster 3600TL"/„Muster Solar" (deckt sich mit der Wächter-Freiliste). Eine Ausnahme ist im Text selbst markiert: „Modul Ablytek 6MN6A275 … — aus dem Bildschirmfoto des Anwenders" — ein echter Hersteller- und Typname mit Datenblattwert (275,19 W). |
| stromspeicher-optimierung-v2.html | Growatt (~10×), Shenzhen (2×) | „WIT-M+APX ESS" (kein reiner Buchstaben+Zahl-Typcode, daher vom Regex nicht erfasst) | **Nicht erlaubt**, wäre es eine Wiki-Seite: „Shenzhen Growatt New Energy Co., Ltd.: WIT-M+APX ESS" mit 129 kWh/100 kW, explizit „aus dem Bildschirmfoto des Anwenders" — realer Hersteller, realer Typ, reale Kennwerte. |
| Dialog_Formel_Zahlenprobe.html | 0 | 0 (drei Treffer B22222/D4766B/E77373 sind CSS-Hexfarben, keine Typcodes) | unauffällig |
| Ergebnis_Bandbreite_Herkunft.html | 0 | 0 (dieselben CSS-Hexfarben) | unauffällig |
| Entwurf_Hydraulikuebersicht_Konfiguration.html | 0 | 0 (Treffer wie C160/M140/L180 sind SVG-Pfadbefehle `M…L…C…`, keine Typcodes); eine Ausnahme: „CS6800iAW MB + AW 10" im sichtbaren Anlagentext (Z. 120) | ein echter Typcode im sichtbaren Text |

Schrift-/Softwarenamen (Prüfung nach 13.2 „Was erlaubt bleibt"): In den Mockups und Wiki-Quellen kommen
nur Plattform-/Systembegriffe vor (Windows, WebView2, MediaWiki, Excel, Word) — nach 13.2 ausdrücklich
**erlaubt**, da sie Systemvoraussetzungen und Bedienung betreffen, kein Produkt der EPOS-Plan-Kataloge.
Keine auffälligen Schriftart-Markennamen gefunden.

**Einordnung:** Die drei betroffenen Mockups verletzen Regel 13.2 nicht **formal** (sie sind keine
Wiki-Seite und werden vom Wächter nicht erfasst), sind aber die Vorlage, aus der Kosten.wiki,
Photovoltaik.wiki und Stromspeicher.wiki laufend fortgeschrieben werden. Vor jeder Übernahme von
Beispieldaten aus `Katalogfilter_Vorschlag.html`, `stromspeicher-optimierung-v2.html` oder der
Anlagenzeile in `Entwurf_Hydraulikuebersicht_Konfiguration.html` in eine `*.wiki`-Datei müssen
Herstellername, Typbezeichnung und Kennwerte nach dem Muster „Wärmepumpe A"/„Speicher 1, 100 kWh"
neutralisiert werden.

## 6. Hilfe-Zuordnung (Aufgabe 4)

Alle zwölf Dialoge tragen `<InfoKnopf Schluessel="@HilfeSchluessel" />` mit festem Default in der
Codebehind-Sektion. Die Auflösung läuft — Stand heute — ausschließlich über die **eine** Datei
`WindowsFormsApplication1/Allgemein/Hilfe/help_mapping.txt` (Format laut deren eigenem Kopf bereits
„H2, Konzept Hilfesystem/Wikidokumentation, A3": `Praefix.Controlpfad = Kurzname[#anker]` — die im
Konzept beschriebene Zuordnung ist also im Format schon umgesetzt, nur für die geprüften zwölf Dialoge
noch ohne Anker genutzt). Kein Dialog hat gar keine Hilfe-ID im Code; einer läuft dennoch ins Leere.

| Dialog | Hilfeschlüssel (Code) | Ziel in help_mapping.txt | Anker in der Wiki-Quelle vorhanden? | Befund |
|---|---|---|---|---|
| KostenKomponenteDialog | `Form_KostenKomponente.btn_Help` | `Kosten` (kein Anker) | ja, z. B. `bemessung`, `herleitung`, `summen` | Seitenebene; passende Anker liegen bereit, sind aber nicht verdrahtet |
| EnergietraegerDialog | `Form_Energietraeger.btn_Help` | `Kosten` (kein Anker) | ja, `energietraegerverwaltung`/`energietraeger` (Z. 68) | dito |
| BhkwWirtschaftlichkeitDialog | `Form_BhkwWirtschaftlichkeit.btn_Help` | **kein Eintrag in help_mapping.txt** | — | **Hilfe-Taste wirkungslos** — `IHilfeDienst.Aufloesen` liefert `null`, laut Doku bleibt ein unbekannter Schlüssel folgenlos |
| PhotovoltaikVerguetungDialog | `Form_PhotovoltaikVerguetung.btn_Help` | `Kosten` (kein Anker) | Inhalt liegt tatsächlich auf **Wirtschaftlichkeit** `pv-verguetung` (Z. 32), nicht auf Kosten | Ziel zu grob/falsche Seite — Sprung landet auf der Kosten-Seite, nicht am eigentlichen Vergütungsabschnitt |
| WirtschaftlichkeitParameterDialog | `Form_WirtschaftlichkeitParameter.btn_Help` | `Wirtschaftlichkeit` (kein Anker) | ja, `parameter` (Z. 19) | Seitenebene; Anker bereit, nicht verdrahtet |
| WirtschaftlichkeitSeite | `UcWirtschaftlichkeit.btn_Help` | `Wirtschaftlichkeit` (kein Anker) | Seite selbst ist das Ziel | passt — Gesamtseite braucht keinen Unteranker |
| KostenSeite | `UcBkKosten.btn_Help` | `Kosten` (kein Anker) | Seite selbst ist das Ziel | passt |
| NutzungsdauerDialog | `Form_Nutzungsdauer.btn_Help` | `Kosten` (kein Anker) | ja, `nutzungsdauern`/`afa` (Z. 129) bzw. `nutzungsdauer-vorbelegen` (Z. 147) | Seitenebene; Anker bereit, nicht verdrahtet |
| EmissionskatalogDialog | `Form_Emissionskatalog.btn_Help` | `Emissionen` (kein Anker) | Seite ist ausschließlich diesem Dialog gewidmet | passt — kein Unteranker nötig |
| GesetzeskatalogDialog | `Form_Gesetzesparameter.btn_Help` | `Gesetzesparameter` (kein Anker) | **außerhalb des geprüften Seiteninventars** — keine Repo-Quelle unter `Projekte/Wiki/` | nicht prüfbar ohne Netzzugriff/eigene Quelle; benannt, nicht behoben |
| VorlagenUebernahmeDialog | `Form_VorlagenUebernahme.btn_Help` | `Kosten` (kein Anker) | ja, `uebernahme-vorlage` (Z. 15), gerade erst mit #363 ausgebaut | Seitenebene; passender Anker bereit, nicht verdrahtet |
| KapitalwertVerlaufDialog | `Form_WirtschaftlichkeitVerlauf.btn_Help` | `Wirtschaftlichkeit` (kein Anker) | Zielabschnitt „Verlauf…" (Z. 34) trägt selbst **noch keinen eigenen Anker** | Seitenebene; selbst wenn verdrahtet würde, fehlt der Anker auf der Zielseite noch |

**Welche Dialoge haben keine Hilfe-ID:** keiner der zwölf — alle tragen einen `HilfeSchluessel`.
**Welche laufen dennoch ins Leere:** `BhkwWirtschaftlichkeitDialog` (Schlüssel ohne Zeile in
`help_mapping.txt`). **Welche zeigen auf die falsche Seite:** `PhotovoltaikVerguetungDialog` (zeigt auf
`Kosten` statt auf den tatsächlichen Inhalt bei `Wirtschaftlichkeit#pv-verguetung`). Bei acht der übrigen
neun Dialoge existiert der passende Anker in der Wiki-Quelle bereits und müsste in `help_mapping.txt`
nur als `#anker`-Zusatz ergänzt werden (reine Konfigurationsänderung, kein Code-Umbau) — das Format
trägt es laut Kopfkommentar der Datei bereits.

## 7. Logbuch (Aufgabe 5)

Grundlage: Statuszeilen #343–#366 sowie „Nach #340" bis „Nach #367" (zur Einordnung mitgelesen).
Sammel-Upload laut „Nach #356 (c)" für den **28.09.2026**; Versionsnummer offen, Vorschlag **1.2.0.2**
laut „Nach #353 (b)", zuletzt in „Nach #367 (h)" wiederholt — **beim Anwender noch zu bestätigen**.
Ausgeschlossen (kein Eintrag, jeweils dokumentierte Kleinigkeit oder reine Papierarbeit ohne
Programmwirkung): #343, #344, #346 (KWKG-Pauschale — ausdrücklich als Kleinigkeit markiert), #348,
#351, #354, #355, #362. #349 und #353 gehören zum selben Elektrokessel-Thema und ergeben zusammen
einen Eintrag (Regel 13.4: „Mehrere Aufträge … zum selben Thema ergeben einen Eintrag").

### 7.1 Vorschlag für den Sammel-Upload (je Thema ein Satz, neueste Version oben gruppiert)

| # | Thema (Aufträge) | Vorgeschlagener Satz | Herkunft |
|---|---|---|---|
| 1 | Kostendialog — Herleitung & Summenfuß (#345) | „Der Kostendialog zeigt unter jedem gerechneten Betrag die Herleitung und weist im Summenfuß der Investitionsseite bei einer Zuschussposition Investition brutto, Zuschuss und Anfangsinvestition getrennt aus." | eigener Vorschlag (Sichtbar-Zeile #345) |
| 2 | Kostendialog — Betriebsseite aufgeräumt (#347) | „Die Betriebsseite der Kostenverwaltung kennzeichnet Pflichtpositionen mit einem Schloss statt des Papierkorbs, zeigt eine Empfehlungszeile, den Stand des zugrunde liegenden Simulationslaufs, die Endenergie je Komponente und einen Warnhinweis bei doppelt gepflegter Hilfsenergie." | eigener Vorschlag (Sichtbar-Zeile #347) |
| 3 | Elektrokessel als elektrischer Verbraucher (#349, #353) | „Ein Elektrokessel zählt zur elektrischen Welt, bekommt wie Wärmepumpe und Heizstab einen Stromträger zugeordnet und erscheint in der Kostenverwaltung mit seinem Stromeinsatz als eigene Zeile bei der Endenergie je Komponente." | eigener Vorschlag, zusammengeführt |
| 4 | Kostendialog Photovoltaik — kWp-Bemessung (#350) | „Die Betriebskosten der Photovoltaik lassen sich auch je kWp bemessen, der Summenfuß weist dazu die Kennzahl in Euro je kWp mit ihrer Herleitung aus, und der Vergütungsdialog warnt rechtzeitig vor der Ausschreibungs- und der Stromsteuer-Grenze." | eigener Vorschlag (Sichtbar-Zeile #350) |
| 5 | BHKW-Wirtschaftlichkeit — Satzherkunft (#352) | „Die Erlösrubrik und der BHKW-Wirtschaftlichkeitsdialog zeigen den angesetzten KWK-Satz mit seiner Herkunft, Vorschlag oder eigener Wert, und rechnen ihn einheitlich auf vier Nachkommastellen." | eigener Vorschlag (Sichtbar-Zeile #352) |
| 6 | Betriebskosten je Kilowatt (#356) | „Die Betriebskosten lassen sich bei Wärmepumpe, Heizkessel, Photovoltaik, Solarthermie, Stromspeicher, Pufferspeicher und Blockheizkraftwerk auch je Kilowatt Leistung bemessen." | eigener Vorschlag (Sichtbar-Zeile #356) |
| 7 | Nutzungsdauern & Ersatz/Restwert (#357) | „Die Investitionsseite der Kostenverwaltung bietet einen Knopf, der leere Nutzungsdauern aus der AfA-Tabelle vorbelegt, und zeigt darunter die Tafel ‚Ersatz und Restwert' mit Ersatzbeschaffungen und Restwert je Position." | eigener Vorschlag (Sichtbar-Zeile #357) |
| 8 | Referenzprojekt & Vergleichssicht (#358) | „Die Wirtschaftlichkeit vergleicht wahlweise alle Varianten gegen ein wählbares Referenzprojekt oder zwei Stände gegeneinander." | wörtlich „Nach #358 (c)" |
| 9 | PV-Vergütung je Variante (#359) | „Eine Variante übernimmt die Photovoltaik-Vergütung des Stammprojekts oder führt eigene Werte." | wörtlich „Nach #359 (b)" |
| 10 | PV-Vergütung beim Weitergeben (#360) | „Beim Weitergeben einer Variante reist die Photovoltaik-Vergütung des Stammprojekts mit." | wörtlich „Nach #360 (b)" |
| 11 | Übernahme aus Vorlage — Katalogansicht (#363) | „Die Übernahme ins Projekt zeigt unter ‚Aus Vorlage/Variante' die Auswahl der Kostenverwaltung aus der Administration samt den Positionen der gewählten Variante." | wörtlich „Nach #363 (d)" |
| 12 | Betriebskosten — Bezugsgrößen aus gespeicherten Läufen (#364) | „Die Betriebskosten weisen die Bezugsgrößen aus dem Simulationslauf auch für gespeicherte Läufe aus, und eine Position ohne Bezugsgröße nennt den Grund samt Abhilfe." | wörtlich #364-Zeile |
| 13 | Hilfsenergie der Vorlage „Standard" (#365) | „Die Kostenvorlage ‚Standard' bemisst die Hilfsenergiekosten von Blockheizkraftwerk, Heizkessel und Wärmepumpe nach dem Anteil am Endenergiebedarf statt an den Endenergiekosten." | eigener Vorschlag (bislang nur „Logbuch-Satz bleibt … " vermerkt) |
| 14 | Hilfsstrom je Anlage & Vorlagenhinweis (#366) | „Hilfsenergiekosten nach ‚% des Endenergiebedarfs' werden bei Anlagen, die selbst Strom beziehen, mit dem Arbeitspreis ihres eigenen Energieträgers bewertet; weicht die Bemessung einer Kostenposition von der der Vorlage ‚Standard' ab, zeigt der Kostendialog den Vorlagenwert mit der Möglichkeit, ihn zu übernehmen." | wörtlich #366-Zeile |

Alle 14 Sätze gegen die Tabuwörter-Regex geprüft: **0 Treffer.**

### 7.2 Prüfung gegen Regel 13.4 — Auffälligkeiten

- **Eintrag 14 (#366):** ein Satz mit Semikolon, der zwei an sich eigenständige Aussagen verbindet
  (Trägerpreis je Anlage; Vorlagenhinweis mit „übernehmen"). Beide gehören zum selben Auftrag und
  Thema (Hilfsstrom-Bemessung), die Zusammenlegung ist nach Regel 13.4 vertretbar — die
  Semikolon-Konstruktion ist aber grenzwertig zu „ein Satz". Vorschlag: so belassen, da bereits vom
  Auftrag als **ein** Logbuch-Satz geführt.
- **Eintrag 2 (#347):** bündelt vier sichtbare Einzelpunkte (Schloss, Empfehlungszeile, Laufstand,
  Endenergie je Komponente) plus die Doppelpflege-Warnung in einem Satz. Alle gehören zur selben
  „Betriebsseite aufgeräumt"-Änderung; vertretbar nach derselben Regel, aber am längsten der Liste.
- **#346 (KWKG-Pauschale, Jahr 0):** bewusst **ohne** Logbuch-Eintrag geführt („Kleinigkeit"), obwohl
  sichtbar (neue Zeile in Block A und Mehrjahrestabelle bei Kleinst-BHKW). Kein Regelverstoß — die
  Entscheidung „Kleinigkeit" liegt im Ermessen nach Regel 13.4 —, aber eine Grenzentscheidung, die im
  Bericht als offene Frage vorgelegt wird (siehe 8.1).
- **#361 (Fußzeilen-Zusatz der Rasterkarte):** sichtbare Änderung (Windows-Anwender sehen den Zusatz
  jetzt vollständig), aber **weder** ein Logbuch-Satz noch ein „kein Logbuch-Eintrag (Kleinigkeit)"-Vermerk
  in der Statuszeile — eine Lücke in der Statuspflege selbst, kein Wiki-Befund. Vorschlag als Eintrag 15
  unten, zur Bestätigung vorgelegt.
- Keine der 14 Zeilen nennt Dateien, Tabellen, Feld-, Schlüssel- oder Klassennamen; keine enthält eine
  Begründung („weil …", „damit …") — beides regelkonform.

Zusätzlicher Vorschlag zur Bestätigung (siehe 8.1):

| # | Thema | Vorgeschlagener Satz |
|---|---|---|
| 15 (offen) | Rasterkarte — Fußzeilen-Zusatz (#361) | „Die Rasterkarte der Stromspeicher-Größen-Sicht zeigt den Zusatz zum Feinraster-Ergebnis jetzt vollständig unter dem Hinweistext." |

## 8. Offene Fragen an den Anwender

1. **#346 (KWKG-Pauschale) und #361 (Fußzeilen-Zusatz):** beide sind sichtbare, aber kleine Änderungen
   ohne bisherige Logbuch-Entscheidung bzw. mit „Kleinigkeit"-Vermerk. Empfehlung: #346 wie entschieden
   ohne Eintrag lassen (seltener Rand­fall Kleinst-BHKW); #361 als Eintrag 15 mit aufnehmen, weil er
   jeden Anwender mit Feinraster-Ergebnis betrifft und zuvor unter Windows unsichtbar war.
2. **Versionsnummer 1.2.0.2:** seit „Nach #353" vorgeschlagen, in „Nach #367 (h)" wiederholt, aber laut
   Statusdatei durchgängig „beim Anwender zu bestätigen". Empfehlung: vor dem Upload am 28.09.2026 fix
   bestätigen oder ändern.
3. **Klimadaten-Seite:** Repo-Quelle liegt vor, Upload ist für den 28.09.2026 vorgemerkt, fehlt aber noch
   in der Bedienungsseiten-Tabelle des Konzepts (§ „Bedienungsseiten mit Repo-Quelle", Zeilen 879–893).
   Empfehlung: beim Sammel-Upload die Tabelle um die Zeile `Programm Dokumentation/Klimadaten` ergänzen.
4. **GesetzeskatalogDialog-Ziel „Gesetzesparameter":** ohne Repo-Quelle unter `Projekte/Wiki/` und ohne
   Netzzugriff nicht zu verifizieren. Empfehlung: bei Gelegenheit prüfen (lebt die Seite nur im Wiki,
   oder fehlt sie ganz?).
5. **Fünf Wiki-Lücken (Abschnitt 4):** Doppelpflege-Warnung, beide PV-Anlagenwarnungen, Kohärenzhinweis
   „übernehmen ohne Stammprojekt", die U23-Satzzeilen der Erlösrubrik. Empfehlung: mit dem nächsten
   Kosten-/Wirtschaftlichkeit-Auftrag nachziehen, Sätze liegen in Abschnitt 4 vor.
6. **Produktdaten in drei Mockups (Abschnitt 5.2):** formal kein Regelverstoß, aber eine Vorlage mit
   realen Hersteller-/Typdaten. Empfehlung: vor jeder künftigen Wiki-Ergänzung aus diesen drei Dateien
   die Beispielwerte bewusst neutralisieren (Regel 13.2 „Wie Beispiele geschrieben werden").
7. **Hilfe-Zuordnung (Abschnitt 6):** `BhkwWirtschaftlichkeitDialog` ohne Ziel und `PhotovoltaikVerguetungDialog`
   mit zu grobem Ziel sind konkrete, kleine Korrekturen (eine Zeile in `help_mapping.txt` je Fall); die
   acht Dialoge mit unverdrahteten Ankern wären ein günstiger Sammelauftrag (H2/A4 aus dem Konzept).

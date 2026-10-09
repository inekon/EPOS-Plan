# H14 — Technikdokumentation im Wiki: Grundlagen, Anwendung, Sprungziele (Umsetzungsprotokoll, 27./28.09.2026)

Zweig `claude/wiki-help-assistant-docs-jllq1r` (Cloud-Sitzung), Ausgangsstand `origin/ios_migration_september`
bis `363096aa` (Merge `ed2dc020`), Statuszeile #611 (beim Zusammenführen von #589 gezogen). Konzept:
[`Konzept_Technikdokumentation_Wiki_EPOS-Plan.md`](../../../aktuell/Konzept_Technikdokumentation_Wiki_EPOS-Plan.md)
(Entscheide TD‑E1 bis TD‑E11, Beispielanlagen §5, Arbeitsanweisung Anhang A); Regeln für beide Rubriken:
[`Konzept_Hilfesystem_Wikidokumentation.md`](../../../aktuell/Konzept_Hilfesystem_Wikidokumentation.md), Abschnitte 13 und 14.
Vorgänger: `H13_Berechnungshilfe_Protokoll.md`, `H13b_Berechnungshilfe_Erzeuger_Protokoll.md`.

**Anwenderauftrag vom 27.09.2026, wörtlich:**

> „Ergänzung in Wiki und Kopplung der Dokumentation an die App: Zu jeder Technologie soll es eine
> Grundlagenbeschreibung geben kurz und knapp geben: wärmepumpe, Heizkessel, solartehermie, Pufferspeicher,
> BHKW, .... Die Funktionsbeschreibung in EPOS-Plan sollte dann für jede Technologie mit einem konkreten Bezug
> erklärt werden. Wie in EPOS-Plan die Technologie eingebunden und konfiguriert wird. Konkrete Beispiele mit
> realen sinnvollen Größen sollten aufgezeigt und die konkrete Einstellung in EPOS-Plan erklärt werden. Dann
> soll der Einfluss von und auf andere Technologien dargestellt und erklärt werden sowie die Erklärung wie diese
> Einflüsse in EPOS-Plan wirken. Erläuterr jeweils die Fallstricke und was besonders zu beachten ist."

Nachträge: Diagramme und Grafiken, wo sinnvoll; das Wiki-Konzept ergänzen; Grundlagen auch indirekt über die
Beschreibung der Kacheln erreichbar; das Wiki für SVG befähigen; alle Techniken beschreiben, auch Stromspeicher
und Wechselrichter; Modellwahl nach Anspruch („Achte darauf, dass die Agenten intelligent genug sind für die
Aufgaben. Nutze, wenn sinnvoll, die günstigen Modelle“; „Prüfe, wo Fable sinnvoll ist, insbesondere für komplexe
Vorhaben“); Bot-Upload gebündelt nach Prüfung.

## 1. Ergebnis im Überblick

| Teil | Ergebnis |
|---|---|
| Grundlagen | zehn Seiten `Projekte/Wiki/Grundlagen - <Titel>.wiki` (Wärmepumpe, Wärmequelle Erdreich, Kessel und Spitzenlast, BHKW, Solarkollektoren, Pufferspeicher, Photovoltaik, Stromspeicher, Wechselrichter, Kühlung), Rubrikseite `Grundlagen.wiki`, `Vorlage - Navigation Grundlagen.wiki` |
| Anwendung | sieben Seiten `Programm Dokumentation - <T>.wiki` mit den Abschnitten Einbindung, Beispiel (Tafel *Dialog · Feld · Wert · Warum*), Zusammenspiel, Fallstricke: Wärmepumpe, Wärmequelle Erdreich, Heizkessel, BHKW, Solarthermie (neu als Quelle), Pufferspeicher, Photovoltaik, Stromspeicher (ergänzt), Wechselrichter (neu), Kühlung (ergänzt) |
| Kachelweg (TD‑E6) | `Programm Dokumentation - Energieerzeuger.wiki` mit Tafel „Die Techniken im Überblick“ |
| Diagramme | 13 Vorlagen (`Vorlage - Diagramm*/Säule*/Legende/Fluss*`), Vorschau `Werkzeuge/WikiUpload/vorschau.py`; sechs SVG-Grafiken unter `Projekte/Wiki/Dateien/` auf den Grundlagenseiten |
| Rechenwege | acht Rechenwegseiten und Simulationsablauf unter `EPOS.Kern/Allgemein/Hilfe/Berechnung/` gegen den Code berichtigt |
| App | Zielschreibweise `/wiki/<Titel>`, `Hilfeziel` im Kern, 22 Knöpfe `<Formname>.Grundlagen`, Knöpfe für Kühlung und Wechselrichterwahl, Windows-Katalog mit Grundlagen im Startbestand, iOS-Kurztext, Wächter |
| Upload | `wiki_upload.py --dateien` (SVG), `--weiterleitungen`; `seiten.tsv`, `dateien.tsv`, `weiterleitungen.tsv` |

## 2. Ablauf und Modellwahl

Die Seiten entstanden in Agenten-Worktrees. Die Orchestrierung hat sie dateiweise in den Zweig übernommen und
dabei geprüft: Gegenlese-Muster, Produktdaten-Wache, Anker-Wache, Vorschau, Stichproben am Code. Agentenzweige
wurden nie gemergt, mit Ausnahme der Kopplung (Merge nach dem Review).

| Schritt | Modell | Übernahme im Zweig |
|---|---|---|
| Energieerzeuger-Seite (Kachelweg) | Sonnet | `615ecd88` |
| Upload-Skript `--dateien` | Sonnet | `8b15a3f9` |
| Grundlagenseiten (10) | Sonnet | `187735b6`, `7189bed5` |
| Hilfesystem-Konzept Abschnitt 14 | Opus (Orchestrierung) | `ba6e2abc` |
| Anwendungsseiten Kühlung, Wechselrichter, PV/Stromspeicher, Solarthermie/Puffer, Heizkessel/BHKW, Wärmepumpe/Erdreich | Opus | `dbc9e6ed`, `e354177e`, `1ea40173`, `db7d16be`, `6e44c67d`, `a9adef4e` |
| SVG-Grafiken | Opus | `908e8e53` |
| Querprüfung aller Seiten (TD‑E11) | Fable | `bf5e1fd0`, `ab8bc79a`, `7e796b28`, `922329fc` |
| Kopplung | Opus, Review Fable | Merge `5afdd6f2` (Review-Fix `52cf1410`) |
| Vertragstafel der Rubrikseite | Sonnet | `e8100df3` |

**Modellwahl.**
- Anfangs sollten Sonnet die Seiten schreiben und Fable die Kopplung.
- Auf den Hinweis des Anwenders, die Agenten müssten für ihre Aufgaben intelligent genug sein (TD‑E10), wurden umgestellt:
  - Die Grundlagenseiten blieben bei Sonnet, denn sie verlangen Fachwissen und sind kurz.
  - Die Anwendungsseiten gingen an Opus, denn Zusammenspiel und Fallstricke müssen das Verhalten der Simulation am Code belegt erklären.
  - Die Kopplung ging von Fable an Opus, denn Fable ist nach der Vorgabe des Anwenders für Orchestrierung und Prüfung da.
  - Die Umstellung kam fünf Minuten nach dem Start; keiner der Agenten hatte bis dahin etwas geschrieben.
- Fable prüfte danach die fertigen Seiten und die Kopplung (TD‑E11).

**Worktree-Stand.**
- Die Worktrees der Agenten starteten auf älteren Ständen: dem lokalen `main` (`50fa9e5a`, rund 1 300 Dateien zurück) bzw. `903f9265`. Dort fehlten Konzept, Diagrammvorlagen und `vorschau.py`.
- Die Agenten zogen mit `git merge --ff-only claude/wiki-help-assistant-docs-jllq1r` nach.
- Bei einigen lehnte das Berechtigungssystem das als „Modify Shared Resources“ ab. Sie lasen den aktuellen Stand dann nur lesend (`git show`, sauberer Hauptbaum).
- Bei der Übernahme wurde jeweils geprüft, ob die Quelle im Hauptzweig seit der Basis des Agenten unverändert war.
- Anhang A des Konzepts nennt den Schritt seither ausdrücklich.

## 3. Befunde der Querprüfung

Die Bearbeiter meldeten Widersprüche; die Orchestrierung sammelte sie in 28 Punkten. Fable arbeitete sie ab und
prüfte die Seiten untereinander.

- **Kaskade**, auf allen betroffenen Seiten gleich beschrieben:
  - Eine nie gespeicherte Kaskade füllt die Konfigurationsseite beim Öffnen in der festen Folge BHKW, Heizkessel, Solarthermie, Wärmepumpe. Das steht nur im Speicher; erst „Konfiguration speichern“ schreibt es.
  - Bei gespeicherter, nicht gepflegter Kaskade stellt `HeizkesselNachziehen` einen Kessel ohne Platz hinter den letzten belegten Platz.
  - Jeder Handgriff setzt `Kaskade_Gepflegt`; danach greift keine Automatik. „Automatik wieder übernehmen“ setzt die Marke zurück.
  - Der Kaskadenplatz regelt die Direktdeckung, die Ladepriorität das Laden der Puffer (Vorgaben Solarthermie 10, Wärmepumpe 20, BHKW 30, Kessel 40, sonst 50). Der Platz entscheidet nur bei Gleichstand.
  - Fallstrick auf allen Seiten: In neuen Projekten steht der Kessel vor der Wärmepumpe.
- **Speicherstufe:**
  - Die Stundenfolge mit Vorabentladung, Direktdeckung, Laden, Nachentladung und Heizstab läuft an der Kaskadenposition ihres ersten Mitglieds.
  - Erzeuger ohne Speicherbeteiligung, die nicht zwischen zwei Mitgliedern stehen, rechnen als eigene Stufe davor oder danach (`SimulationControl.cs`, Abgrenzung an `Kaskade_Zweikanalig`).
  - In Anlage A rechnet Kessel 1 mit reiner Direktsenke deshalb NACH Wärmepumpe, Speicher und Heizstab. Die Wärmepumpenseite hatte das Gegenteil gesagt und ist berichtigt; ebenso Bivalenzpunkt und Spitzenkesselleistung.
- **Beispielanlagen:** je Anlage eine Konfiguration, eingetragen in §5 des Konzepts. Widersprüchliche Einstellungen der Bearbeiter wurden verworfen: Kombipuffer gegen Heizungspuffer bei A, 70/45 gegen 60/35 °C bei B.
- **Rechenwegseiten berichtigt:**
  - Wärmepumpe: Kühlbetrieb als Kältekaskade, AK1-Vorlaufstufe, höchstens neun Module, stiller CSV-Rückfall, JAZ nur im Bericht.
  - Photovoltaik: Standby-Verbrauch, keine Felder für α_SC/β_OC, 1 000 W/m², Bewertung der Klappliste.
  - Stromspeicher: Rückfall 10–90 %, Start-Ladezustand.
  - Solarthermie und Pufferspeicher: Nachrang-Automatik 30 %, kein Temperaturpaar am Kollektorfeld, Ergebnisorte.
  - Heizkessel: Bereitschaftsverlust in kW nur in betriebsbereiten Stunden, mit Deckel.
  - BHKW: projektweite Untergrenze.
  - Erdreich: Bodennamen.
  - Simulationsablauf: Speicherstufe und Vektorstufen.
- **Weitere Seiten:** Simulation (Kaskadenabsatz), Gerätekataloge (MPP-Fenster, Einschaltspannung), Energieerzeuger (Profil/Ganglinie wählen nur den Dialog, die Simulation liest die Solarthermieganglinie nicht), Rubrik Grundlagen (Stundenraster Wärme/PV, Viertelstunden Stromspeicher).

## 4. Kopplung

- **Kern:** `EPOS.Kern/Allgemein/Hilfe/Hilfeziel.cs` bildet aus `/wiki/Grundlagen/<Titel>` den Seitentitel und den Kurztext „Grundlagen: <Titel>“ (en „Fundamentals: <Titel>“). Aus einem Kurznamen wird der Kapitelname ohne Anker.
- **Windows:** `HelpCatalog.cs`.
  - Pfadziele lösen über den unveränderten Weg `ZielFuer` → `UeberPfad` auf.
  - Die zehn Grundlagenseiten stehen URL-kodiert im Startbestand `help_cache.json`, ohne Slug. Sonst wäre „wärmepumpe“ mehrdeutig mit der gleichnamigen Rubrikseite.
  - `GrundlagenRueckfallErgaenzen` ergänzt sie nach dem Onlineabruf und beim Laden der Sicherung, idempotent.
- **iOS:** `IosHilfeDienst.Aufloesen` ruft nur `Hilfeziel.Kurztext`. Die Adressbildung ist unverändert. iOS ab 17.0 kodiert rohe Umlaute in der Adresse selbst; im Harnisch stimmen alle 22 Pfadziele mit der Windows-URL überein.
- **Knöpfe:**
  - 22 Schlüssel `<Formname>.Grundlagen`: Projektdialoge, Katalogeditoren, die Knopfpaare Wechselrichter im PV-Dialog und Kühlung an Gebäude und Wärmepumpe.
  - Fünf neue `btn_Help`: Wechselrichter#einbindung, Kühlung#eingaben, Kühlung#kuehlbetrieb, Kühlung#einschalten, Gebäudemodell VDI 6007#kuehluebergabe.
  - Anzeigetexte über `HILFE_*` (de/en).
- **Wächter:** `HilfezielTests` (46 Fälle), `GrundlagenknopfTests` (24 Fälle), Pfadziele in `HelpMappingAnkerWacheTests`.
- **Review (Fable):** übernehmbar. Die Herleitungszeile der Wärmequelle Erdreich hätte die beiden Knöpfe untereinander gesetzt (`white-space: pre-line`) und ist jetzt Flexzeile (`52cf1410`). Katalog-Harnisch mit sechs Szenarien (offline, online, alte Sicherung, Englisch) grün.

## 5. Nachweise

- **Übernahme je Seite:** Gegenlese-Muster der Wurzel-`CLAUDE.md` 0 Treffer; Produktdaten- und Anker-Wache grün; UTF-8 ohne BOM mit LF; Vorschau hell/dunkel mit `vorschau.py`.
- **Querprüfung (Fable):** Filtertests BerechnungsHilfe, BerechnungshilfeEinbettung, WikiProduktdaten, HelpMappingAnker, DokumentationLinkWache, Berechnungsknopf: 393 grün. Kein Anker verloren, alle `[[…#anker]]` auflösbar, kein Verweis auf eine weitergeleitete Altseite.
- **Vertragstafel** (`e8100df3`, Sonnet):
  - 136 Tafelzeilen, das sind genau die `btn_Help`-Schlüssel der `help_mapping.txt`.
  - 7 tote Schlüssel gestrichen (`FormMain`, `WizardParent`, `Form_Variantentest`, `Form_Kosten`, `Form_Betriebskosten`, `Form_KwkgModule`, `Form_WirtschaftlichkeitVerlauf`), 25 Ziele berichtigt, 43 Zeilen ergänzt, 38 Unterseiten.
  - Prüfskript: 0 Schlüssel ohne Zuordnung, 0 Zuordnungen ohne Zeile, 0 abweichende Ziele.
- **Gate auf dem gemergten Stand** (`d5761e9f`):
  - Kern-Build 0 Fehler (64 Warnungen Bestand).
  - Tests: KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (+1 übersprungen), EPOS.UI.Tests 6 865, EPOS.Kern.Tests 8 889 grün, 1 übersprungen, 1 rot.
  - Der rote Test ist `TwwKatalogWacheTests.Das_Einspielskript_ist_wiederholbar`. Er hängt nicht an diesem Zweig: Im Container läuft Python 3.11, wo `sum()` naiv summiert.
  - Windows-Schale 0 Fehler.
- **CI Kern** (ubuntu, Python 3.12): grün auf `072bc7e3` (Lauf 1016) und `d5761e9f` mit der Kopplung (Lauf 1017).

## 6. Befunde außerhalb der Wiki-Quellen

Nicht in diesem Auftrag geändert. Der Anwender hat am 28.09.2026 entschieden: „Worktrees ablegen und in anderer Sitzung ausführen – erinnere am 29.09. daran. Hier nur die Dokumentation.“ Die Befunde stehen deshalb als Folgeaufträge 1 bis 8 in [`Folgeauftraege_Technikdokumentation_EPOS-Plan.md`](../../../aktuell/Folgeauftraege_Technikdokumentation_EPOS-Plan.md), jeweils mit Vorspann, Befund, Aufgabe, Abnahme und den Wiki-Seiten, die danach nachzuziehen sind. Die Aufträge fassen selbst keine Wiki-Quellen an.

1. Kühlkennlinie aus dem Katalog nachholbar machen, wenn die Wärmekennlinie schon im Projekt steht.
2. PV-Dialog: Clipping-Hinweis im Modell „Erweitert“ und Pflegetext zu α_SC/β_OC (`PVS_PFLEGEWEG`).
3. Zusammengesetzte SQL-Texte in `SimulationControl.cs`/`SimulationPV.cs`, „(Befund V1)“ und „(Befund vom 11.09.2026)“ in Anwendertexten, Monatsbild der Autarkie Analyse in zwölf gleichen Blöcken.
4. Solarthermieganglinie: Die Simulation liest sie nicht. Einbinden oder die Wahl entfernen (Anwenderentscheid); dazu der KI-Erklärtext der Nachrang-Schwelle.
5. Kaskaden-Vorwahl: Kessel vor der Wärmepumpe in neuen Projekten (Anwenderentscheid).
6. BHKW: wirkungsloses Feld „Untere Grenzleistung“, Einheit „%“ des Bereitschaftsverlusts im Katalogbrowser, Tippfehler in `SIMERG_INFO_BHKW`, Grenzleistungen über 100 % in der Testdatenbank.
7. Wärmepumpe: faktisch neun statt zehn Module, der Lauf bricht ohne Meldung ab; stiller CSV-Rückfall, veraltete Meldungstexte.
8. `TwwKatalogWacheTests.Das_Einspielskript_ist_wiederholbar` ist unter Python 3.11 rot, weil `sum()` ab 3.12 kompensiert summiert. Abhilfe `math.fsum`.

## 7. Offene Punkte

- **Upload am 28.09.2026 erledigt** (Regel 13.3, TD‑E9), mit dem Bot in drei Aufrufen:
  - `wiki_upload.py --dateien`: 6 Dateien, Rücklese-SHA-1 gleich.
  - `--seiten --nur` mit den Zeilen dieser Welle: 39 Seiten, darunter die 13 Vorlagen, Rücklese byte-gleich, 0 Parse-Warnungen.
  - `--weiterleitungen`: 19.
  - Geprüft: die sechs SVG-Grafiken auf den Grundlagenseiten ohne roten Datei-Link, die Weiterleitungen auf die Anwendungsseiten.
  - Mitgegangen sind Berichtigungen der Hauptlinie auf fünf geteilten Seiten, die noch nicht online waren:
    - Pufferspeicher: Hinweis zu fehlenden Temperaturen;
    - Kühlung und Simulation: Block „Weitere Einstellungen“;
    - Gerätekataloge: Bereitschaftsverlust in kW, Felder je MPPT;
    - Photovoltaik: Knopf „Wechselrichter vorschlagen“.
  - Ein Logbuch-Entwurf lag für diese Seiten nicht vor. Ihre Sätze gehören zur Programmversion der Hauptlinie.
  - Weiter ausstehend (Hauptlinie, mit Logbuch): Simulationsergebnisse, Wirtschaftlichkeit, Gebäude, Brauchwasser-Zapfprofil, Berichtsvorlagen, Mehrzonenmodell, Projekttransfer.
  - Das Bot-Passwort gab der Anwender im Chat an. Es wurde nur als Umgebungsvariable gesetzt und ist nach dem Upload unter `Spezial:BotPasswords` neu zu erzeugen.
- **Logbuch:** Die Knöpfe „Grundlagen“ erreichen den Anwender mit der nächsten Programmversion. Der Satz „Die Technikdialoge haben einen Knopf „Grundlagen“, der die Grundlagenseite der Technik im Wiki öffnet.“ wird mit dieser Version veröffentlicht; die Versionsnummer erfragt die Orchestrierung beim Anwender. Die Wiki-Seiten selbst bekommen keinen Eintrag (Regel 13.4).
- **Anwenderfragen:** Sie stehen bei den Folgeaufträgen (Abschnitt 6), jeweils mit der Entscheidung, die die Sitzung des Auftrags vorlegt. Die Frage nach einer Fable-Regel ist durch die `CLAUDE.md` erledigt.
- **Fortsetzung Freitag, 02.10.2026, 02:00 Uhr (Routine):** `origin/ios_migration_september` mergen und die bis dahin ergänzten Funktionen in die Beschreibungen aufnehmen. Ist einer der Folgeaufträge umgesetzt, werden die Seiten der Spalte „Wiki danach nachziehen“ nachgezogen und die Fallstricke entfernt. Erinnerung an die Folgeaufträge am 29.09.2026.

## 8. Nutzung

Token laut Abschlussmeldung der Agenten, gerundet:

| Modell | Aufgaben | Token |
|---|---|---|
| Sonnet | Energieerzeuger, Upload-Skript, sechs Grundlagen-Pakete, Vertragstafel | rund 3,0 Mio. |
| Opus | sechs Anwendungs-Pakete, SVG-Grafiken, Kopplung | rund 4,3 Mio. |
| Fable | Querprüfung, Review der Kopplung | rund 1,0 Mio. |
| Opus 5 | drei Formel-Agenten der Korrektur (Abschnitt 9) | rund 0,84 Mio. |

Dazu kommt die Orchestrierung (Opus). Abgerechnet wurde über die Cloud-Credits (TD‑E10).

## 9. Korrektur 28.09.2026: Umbruch und Formeln

**Anwenderhinweis vom 28.09.2026, wörtlich:** „1. die formeln sind nicht alle in mathmatischen
Satz/latex gesetzt. 2. Die Absätze sind nicht korrekt umgebrochen. Prüfe den upload und die anderen
Seiten und korrigiere". Beigelegt war ein Bildschirmbild der Rechenwegseite Solarthermie, Abschnitt
„Grenzen und Annahmen".

**Befund.**
- **Harte Umbrüche.** Die Rechenwegseiten waren durchgehend hart umbrochen, mit bis zu 228
  Folgezeilen je Seite. MediaWiki liest eine eingerückte Folgezeile als `<pre>`-Kasten und die nicht
  eingerückte Folgezeile eines Listenpunkts als neuen Absatz. Live standen deshalb graue Kästen in
  Schreibmaschinensatz, zum Beispiel 15 auf BHKW, 13 auf Solarthermie und Wärmepumpe, 11 auf
  Pufferspeicher. Die Listen waren zerrissen.
- **Formelzeichen außerhalb von `<math>`.** Sie standen als HTML-Tiefstellung, mit Unicode-Index, in
  Unterstrich-Schreibweise, als griechisches Zeichen im Fließtext oder als halbe Gleichung. Die
  `<math>`-Formeln selbst setzt das Wiki als MathML (Math-Erweiterung, native Ausgabe).
- **Lücke in der Upload-Liste.** Die Rechenwegseiten standen nicht in `seiten.tsv`. Die Berichtigungen
  der Querprüfung aus Abschnitt 3 waren deshalb nicht online.
- **Leerer Hinweiskasten.** Auf der Seite Wirtschaftlichkeit zeigte ein Hinweiskasten nur seine
  Überschrift: Ein „=" im Text machte den Inhalt zum benannten Vorlagenparameter.

**Behebung.**

| Schritt | Commit |
|---|---|
| Umbruch: 27 Quellen, jeder Absatz, Listenpunkt und jede Tabellenzelle auf einer Quellzeile. Parser-Gegenprobe je Seite: sichtbarer Text gleich, keine ungewollten `<pre>` | `9f2cf7ba` |
| Upload-Liste: 13 Rechenwegseiten, Startseite der Rubrik Berechnung, Emissionen | `6cf92dd6` |
| Formeln der Technikseiten: 13 Seiten, 36 Stellen | `2ac90dc4` |
| Formeln der übrigen Seiten: 9 Seiten, 78 Formeln, 23 Kennungen in `<code>`, Hinweiskasten Wirtschaftlichkeit | `70c5570c` |
| Formeln der Rechenwegseiten: 12 Seiten, 107 Stellen, nur Befehle des Klartext-Umsetzers | `5ec960bc` |

- **Umbruch:** mit dem Werkzeug `Werkzeuge/WikiUpload/entfalten.py`.
- **Formeln:** drei Opus-5-Agenten mit getrennten Dateien.
- **Prüfung durch die Orchestrierung** je Datei gegen den Parser: 0 TeX-Fehler, keine `<pre>`, Anker
  und Zeilenzahl unverändert. Kern-Build 0 Fehler, Hilfe- und Wiki-Tests 537 grün.

**Zweiter Upload am 28.09.2026.**
- 35 Seiten ersetzt, Rücklese byte-gleich, 0 Parse-Warnungen.
- Leseansicht der 47 Seiten dieser Welle: 2 634 Formeln als MathML, 0 Formelfehler, nur die zwei
  gewollten Kästen auf der Seite Stromspeicher.
- Mitgegangen sind auf sechs Rechenwegseiten noch nicht hochgeladene Beschreibungen der Hauptlinie:
  Brauchwasser, Prozesswärme, Strombedarf, Stromspeicher, Wärmebedarf, Wärmequelle Erdreich.
- Nicht hochgeladen sind die sieben Seiten der Hauptlinie aus Abschnitt 7; ihre Quellen sind
  mitkorrigiert.

**Regel und Folgeaufträge.**
- Regel: Konzept Hilfesystem 13.5 und Anhang A des Konzepts Technikdokumentation.
- Folgeauftrag 9: Klartext-Umsetzer des Assistenten.
- Folgeauftrag 10: Wache gegen harte Umbrüche.

**Darstellung.**
- Im Bearbeitungsmodus (VisualEditor) erscheinen Formeln als Formelknoten.
- In der Leseansicht setzt der Browser MathML in seiner Mathematikschrift, sichtbar kleiner als der
  Fließtext. Das Stilblatt `MediaWiki:Common.css` hat dafür keine Regel; der Vorschlag dazu geht an
  den Anwender.

## 10. Fortsetzung 02.10.2026 (Routine)

Auftrag des Anwenders vom 27.09.2026: „starte am Freitag 2. Oktober um 2:00 Uhr. Aktualisiere dann auch die
bis dahin ergänzten Funktionen in der Beschreibung".

- **Merge:** `origin/ios_migration_september` enthielt den Zweig bis `852fac0c` bereits; der Zweig ist
  ohne Konflikt auf `4cfbf017` vorgezogen. Die Statuszeile dieser Welle trägt in der Hauptlinie die Nummer #611.
- **Neue Funktionen seit dem 28.09.** (Statuszeilen #589–#640), soweit sie die Technikseiten berühren:
  - **Heizkessel** (ein Opus-Agent):
    - Rechenwegseite neu: 9 Schritte, 38 Gleichungen. Inhalt: Kesseldaten, Heiztage nach der
      Heizgrenze (#595), Teillastkennlinie η(β) mit η₃₀ nach Bauart, Brennwertkennlinie über die
      Rücklaufkette, Takten mit Anfahrverlust, Bereitschaft nur an Heiztagen plus 24 h, Gasspitze,
      Zahlenrand.
    - Anwendungsseite: Beispiel A auf die heutigen Felder, neue Fallstricke zu Rücklauf und
      Brennwertnutzung, Abschnitt „Betriebsbereitschaft“. Die neuen Abschnitte der Kessel-Sitzungen
      stehen in LaTeX und rechnen einheitlich mit 50 kW.
    - Grundlagenseite angeglichen.
  - **Wärmepumpe und Kühlung** (ein Opus-Agent):
    - Auf dem Rechenweg VDI 6007 nimmt die Simulation Heiz- und Kühlsollwerte aus den
      Konditionierungsprofilen (`Vdi6007Rechenweg.cs`, `GebaeudeModellEingang.cs`).
    - Kühlung beschreibt den Kühlsollwert im Reiter „Konditionierung“ mit Kühlkalender und
      Nachtauskühlung; dazu die Variante „D mit Bürozeiten“.
    - Wärmepumpe: „Auslegung für Verteilung“ (#636), gesperrter Kühlschalter ohne Haken (#631).
    - Rechenwege Wärmebedarf (Herkunft der Sollwerte) und Wärmepumpe (Kennlinie ohne gekoppelten
      Bedarf, Zahlenrand des Quellpuffers #599).
  - **Reiterfolge (#628):** keine Aussage betroffen.
- **Prüfung:**
  - Parser-Gegenprobe je Datei: 0 TeX-Fehler, keine `<pre>`, Anker erhalten.
  - Die Kernaussage zu den Kühlsollwerten ist am Code nachgeprüft.
  - Kern-Build und Hilfe-Tests: siehe Commit.
- **Folgeaufträge:**
  - Nr. 8 ist erledigt (#592).
  - Neu: Nr. 11 (Kessel-Hinweistexte, Stromganglinie des Elektrokessels) und Nr. 12
    (Parameterverwendung der Kühlspalten).
  - Als Punkt ohne Auftrag: die Gebäudeseiten, die die Welle KP3 nachzieht.
- **Logbuch:** Die Sätze zum Kesselmodell stehen bereits unter Version 1.2.0.6 (Statuszeilen #616–#636).
  Die Wiki-Seiten selbst bekommen keinen Eintrag.
- **Upload:** mit dem nächsten Wochen-Upload (Regel 13.3). Der Trockenlauf zeigt die geänderten Seiten.

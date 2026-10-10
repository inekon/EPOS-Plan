# Wiki-Sammel-Upload 10.10.2026 (Auftrag #885) — Protokoll

Ablauf des dritten Sammel-Uploads: Auftrag, Ausgangslage, Prüfung, Logbuch, Upload, Offenes. Statuszeile #885; Revisionstafel im [Hilfesystem-Konzept](../../../aktuell/Konzept_Hilfesystem_Wikidokumentation.md) (Abschnitt „Sammel-Upload 10.10.2026“), Seitenliste im [Update-Papier](../../../aktuell/Wiki_Update_2026-09-26.md).

## Auftrag
Anwenderauftrag 10.10.2026: „nehme den wiki upload vor mit allen änderungen seit dem letzten upload. Prüfe dabei die
Wiki-Seiten auf aktualiät und verbesserungen.“ Zugang (Bot-Passwort, Benutzer Epos) vom Anwender im Chat übergeben, in
der Sitzung nur als Umgebungsvariablen des Laufs aus einer Datei außerhalb des Repositoriums gesetzt, nach dem Lauf
gelöscht; in keiner Repo-Datei, keinem Commit, keinem Agentenauftrag. Anwendervorgabe zur Modellwahl während des
Auftrags: „nutze fable 5 für komplexe aufgaben. Sonst opus 5.5 oder sonnet/haiku je nach aufgabenstellung und nutzung“.
Sitzung lief auf Opus 5.5.

## Ausgangslage
- Letzter Upload: 30.09.2026 04:52 UTC (Revision 727, #622); letzte Logbuch-Revision 723 (#614).
- Vorprüfung Regel 3: Die letzte Revision jeder abweichenden Live-Seite war ein Bot-Upload (Benutzer Epos) vom 28.–30.09.;
  seit Revision 727 keine Bearbeitung im Wiki.
- Trockenlauf vor der Prüfung: 43 Seiten ERSATZ, 12 GLEICH, Dateien und 19 Weiterleitungen GLEICH.
- Drei Repo-Quellen standen nicht in `seiten.tsv` und waren nie im Wiki, obwohl Hilfeknöpfe auf sie zeigen:
  Betriebskalender, Nutzungsprofile, Pufferspeicher auslegen → aufgenommen (Commit 6b515a5cb).
- Update-Papier Abschnitt 3: Anwenderentscheid 09.10.2026 „Wiki-Upload zurückgestellt“ wegen des Bedienbegriffs
  „Befund“; der Auftrag vom 10.10. hebt die Zurückstellung auf. Begriffsfrage weiter offen; das Wiki nennt „Befund“ nur
  dort, wo es eine Beschriftung der Oberfläche wörtlich wiedergibt (Gebäudeimport: Spalte, Farbmodus, zwei Filter).

## Prüfung (Agenten im Hauptbaum, je eigene Dateien, ohne Bau)
- Acht Sonnet-Agenten (Bedienungs- und Grundlagenseiten), drei Opus-Agenten (Rechenwegseiten), ein Sonnet-Agent
  (Logbuch), ein Opus-Agent (Abgleich jeder Logbuch-Änderung gegen die Seiten), ein Fable-Agent (52 Live-Seiten ohne
  Repo-Quelle).
- Commits: f0a0fc0b4 (Wärmepumpe: doppelter Anker `einbindung` → `einbindung-hydraulik`), 300f86c5f (Gebäude #861/#880,
  Gebäudeimport), 7f5aef75d (Simulation, Simulationsergebnisse, Berichtsvorlagen, Projekttransfer, Varianten),
  ebc95522e (Pufferspeicher, Pufferspeicher auslegen, Stromspeicher, Grundlagen Pufferspeicher), 240a2dcd8 (Heizkessel,
  BHKW, Solarthermie), 828ea59fb (Gebäudemodell VDI 6007, Klimadaten, Photovoltaik, Grundlagen Photovoltaik),
  2aaade376 (Zapfprofil, Gerätekataloge, Hilfe-Assistent, Rubrikseite), 15d88767b (Rechenwege Photovoltaik,
  Pufferspeicher, Stromspeicher), e08bfc1cc (Rechenwege Wärmebedarf, Brauchwasser, Prozesswärme, Strombedarf,
  Simulationsablauf, Index), cf9d93bf3 (Rechenwege Wärmepumpe, Erdreich, Heizkessel, BHKW, Solarthermie), 38f61dd10
  (Abgleich: zehn Seiten), 61debe5ab (Formel BHKW, Köpfe, Kältespeicher-Wortlaut; Wache kennt R744), acf011c4f (tote
  Verweise Gerätekataloge, Zapfprofil).
- Wesentliche Befunde und Berichtigungen:
  - Rechenwege: PV-Bilanz je Viertelstunde statt Stundenmittel; Abschnitt PV-Ganglinie; Kältespeicher und Heizungspuffer
    auf AK3; Einspeisegrenze am Einzelspeicher; Flottenvorgabe aus dem laufenden Durchgang; Zonensperre heizen/kühlen;
    Wochentagsraster der Klimaregion; Feiertagsregel der BDEW-Profile; Kühlkanal, Kältekaskade, Anlagenkopplung,
    Komfortstunden im Simulationsablauf; Übergabegrenze, Gerätegrenzen, Betriebsbereiche und Bivalenzpunkte der
    Wärmepumpe; Sperrfenster; Sondenfeld je Anlage; Rücklaufgrenze BHKW; Höchstzahl Wärmepumpenmodule 10 statt 9;
    34 bzw. 11 an Legendenzeilen hängende Absätze gelöst; Umlaute in der Seite Wärmequelle Erdreich.
  - Bedienungsseiten: Sperrzeiten der Wärmepumpe (Gruppe „Sperrzeiten“ statt Einzelfenster), Projektdialoge mit
    Katalogauswahl (oben/darunter statt links/rechts) auf Wärmepumpe, Solarthermie, Photovoltaik, Heizkessel, BHKW,
    Gebäude; Kältemaschine als zweiter Kälteerzeuger; Erdsonde wirkt in der Simulation zurück; Komponentenübernahme
    (neuer Abschnitt Varianten), „ohne Anlagenzuordnung“ und Stromträger der Kältemaschine (Kosten); Abschnitte
    Kältemaschinen und Ganglinien in Gerätekataloge; sechs fehlende Masken im Hilfe-Assistenten; Rubrikseite um
    Mehrzonenmodell, Projekttransfer und acht Hilfezuordnungen ergänzt; Pufferspeicher auslegen: falsche Aussage zur
    Sperre berichtigt, Nachbarstufen, Abgleich mit der Jahressimulation, Kriterium „Aufheizen nach Absenkung“.
  - Abgleich: rund 103 Logbuch-Einträge geprüft — 76 abgedeckt, 8 fehlten (6 ergänzt), 5 widersprachen (alle
    berichtigt), 14 Kleinigkeiten ohne Seitenbezug.
- Prüfungen: Kern-Wachen 291/291 (BerechnungsHilfe, Hilfeziel, HelpMappingAnker, WikiProduktdaten, WikiWissenAuszug,
  BerechnungshilfeEinbettung, DokumentationLink, RepositoryOrdnung, KiWissensdeckung, KiDialogaufruf), Oberfläche
  246/246 (Berechnungshilfe, Berechnungsknopf, Grundlagenknopf); Parse-Prüfung aller Seiten über `action=parse`: keine
  Formelfehler, keine Parse-Warnungen, keine fehlenden Ziele außer den mit dem Upload neu entstehenden Seiten; drei
  Schreibmaschinenkästen sind gewollte Bildschirmzeilen (Stromspeicher, Pufferspeicher auslegen).
- Behobene Prüffehler: Formel der BHKW-Einspeisung beim Schreiben an „\right“ zerrissen (Klartext des Assistenten trug
  LaTeX); Kopfkommentare von vier Rechenwegseiten zu lang (Oberflächentest verlangt Ende in Zeile 6); „Kaltwasserspeicher“
  ist Katalogname; „R744“ traf das Typcode-Muster → als Normbezeichnung (ISO 817) in die Wache, wie A100 und W551, mit
  Gegenprobe.

## Logbuch
- Unveröffentlicht vorgefunden: Blöcke 1.2.0.6 (52 Entwürfe), 1.2.0.7 (2), 1.2.0.8 (2), 1.2.0.9 (41, Version vom Anwender
  bestätigt 09.10.), „Version offen (Vorschlag 1.2.1)“ (9).
- Dazu Lücke: Vom Block 1.2.0.5 des Papiers waren 38 Sätze nie veröffentlicht (51 von 89 live; drei weitere sind mit anderem
  Wortlaut live). Dazu fehlten die Gebäudesimulation (Aufheizung, Konditionierungskalender, AK3, Kühlkurve, Kältemaschine
  als Anlage, Pufferauslegung, Gebäudeexport, Nutzungsprofile) ganz im Live-Logbuch.
- Gestrafft nach Regel 13.4 (ein Satz je Thema, Kleinigkeiten gestrichen, keine Klammern, Kürzel, Produktnamen): 70 Sätze.
- Versionszuordnung (Vorgabe der Orchestrierung, dem Anwender zur Bestätigung genannt): offener Block → 1.2.0.9;
  Pufferauslegung und Sperrfenster (#688, #701, #704) → 1.2.0.7, wie die Statusdatei sie führt; nie veröffentlichte Sätze
  aus 1.2.0.5 → 1.2.0.6. Entwurf: 1.2.0.9 mit 29, 1.2.0.8 mit 1, 1.2.0.7 mit 4, 1.2.0.6 mit 36 Sätzen (nach Anwenderentscheid je +1), eingefügt vor
  „Version 1.2.6 – September 2026“.
- Einleitung des Live-Logbuchs berichtigt: „Hier werden die Versionshistorie.mot.dwn wesentlichen Änderungen beschrieben.“
  → „Hier wird die Versionshistorie mit den wesentlichen Änderungen beschrieben.“
- Anwenderentscheid 10.10.2026: „Versionsnummern im Logbuch: letzte Stelle um eins erhöhen“ — veröffentlicht werden
  1.2.0.10 (29 Sätze), 1.2.0.9 (1), 1.2.0.8 (4), 1.2.0.7 (36). „Version 1.2.6“ (29.09.) bleibt unverändert stehen.

## Offen für den Anwender (aus den Agentenberichten)
- Begriff „Befund“ im Gebäudeimport (Oberfläche und Wiki, sechs Stellen).
- Feldwert „wie bisher“ des Felds „Teillastrechnung“ (Kältemaschine) ist ein Änderungswort in der Oberfläche.
- Protokolltext `SIMENG_BHKW_RUECKLAUF_MAX` sagt „an oder über der Rücklaufgrenze“, `Ruecklaufgrenze.BhkwAus` prüft „>“.
- Rechenweg Wärmepumpe: Kennlinienwahl für Prozesswärme fehlt; Simulationsablauf: Anlagenfahrplan und Übergabegrenze
  fehlen; Wärmebedarf: VDI 6007 ohne eigene Formelstrecke.
- Hilfe-Assistent: Maskenliste in einem Absatz von rund 5 000 Zeichen, besser als Tafel.
- Ältere Teile der Rechenwegseiten (u. a. `_Index`) schreiben ae/oe/ue statt Umlauten.
- Der BHKW-Dialog nutzt die neue Katalogauswahl nur teilweise (kein Rückweg, keine Mehrfachbearbeitung).
- 53 Live-Seiten ohne Repo-Quelle (Ergebnis des Fable-Agenten: siehe unten).

## Übernommene Live-Seiten (Fortsetzung nach Abbruch des Fable-Agenten)
- Fable-Agent vom Anwender gestoppt (Abbruch während eines Werkzeugaufrufs); Teilstand 24 Seiten gesichert als 94c1371eb
  („vor Abnahme“). Anwenderentscheid 10.10.2026: mit Opus fortsetzen. Drei Opus-Agenten auf getrennten Dateien.
- 5f4d074a4: 47 übernommene Seiten in `seiten.tsv`; ohne Quelle bleiben Impressum, Datenschutz, Hauptseite, Programmablauf,
  Systemvoraussetzungen (Systemvoraussetzungen von der Orchestrierung gegen Setup und Code geprüft: zutreffend).
- Push 570efff26..5f4d074a4 nach grünen Wachen (319/319, Kern, `--no-build`, Baum mit allen 47 Quellen).
- eabba89cf (Opus): elf Grundlagen- und Einstiegsseiten — Wärmebedarfsrechnung (VDI 6007 als Vorgabe, Tagesbilanz Wahlweg),
  Strombedarf und Lastprofile (Lastgänge addiert statt ersetzt, BDEW H25/G25/L25, P25/S25), Klimadaten (PVGIS, TRY, Klimazone,
  Wochentagsraster, Anker `wochentage`), Hydraulikschemata (Eisspeicher, Anergienetz nicht nachgebildet; Anker
  `uebersetzung-in-die-simulation`), Vergleich Energiebilanz, Kostenrechnung, Wirtschaftlichkeitsrechnung (VALERI-Tafel),
  Erlösrechnung, Über EPOS-Plan, Beispiele, Industrie und Gewerbe (Quellprofil, Regeneration).
- Offen aus eabba89cf: Über EPOS-Plan nennt Software-Service-Bedingungen und den Produktnamen TeamViewer (Rechtliches/Service,
  nur gemeldet); weder Über EPOS-Plan noch Systemvoraussetzungen nennen die iOS-App; Beispiele „Beispielrechnungen zum
  Download … werden derzeit überarbeitet“ ist ein Standvermerk; Katalogfilter „stetige Regelung“ (Beispiele, Einfamilienhaus)
  ungeprüft.
- 2939fc033 (Opus): zwölf Übersichtsseiten — Simulation (Ansicht mit drei Schritten, Menü Projekt), Ergebnisdarstellung,
  Datenexport (CSV je Diagramm, Abschnitt `weitere-ausgabewege`), Kosten und Energiepreise (Anker
  `nutzungsdauern-und-gesetzliche-parameter`), Varianten und Bericht, Wirtschaftlichkeit (Szenarien Ungünstig/Erwartet/Günstig,
  Strommatrix ohne HT/NT), Stammdaten und Datenimport, Einstellungen Hilfe und Sprache, Hilfe-Assistent (Aufruf über die
  EPOS-Marke), Installation und Update (am Setup belegt; Signaturaussage gestrichen, Signieren ist im Build nur Option),
  Erste Schritte, FAQ (Datenverzeichnisse `WP-Plan`). Aus dem Fable-Entwurf verworfen: sechs nicht belegbare Aussagen.
- Offen aus 2939fc033: Update-ZIP auf dem Server?; wird das Setup signiert?; Preis-, Lizenz- und Rechtsaussagen (Wartungsvertrag,
  Einzelplatzlizenz, Google-Bedingungen, Demoversion, KWKG-2025-Abschnitt) nur gemeldet; FAQ nennt die Online-Lizenzaktivierung
  nicht als Internetbedarf; Registrierungsinhalte (`HKCU\Software\wp-plan`) ungeprüft; PD/Simulationsergebnisse nennt
  „Dialog Detaillierte Simulation“ (heute Reiter der Ergebnisseite).
- 9cba38fd6 (Opus): Abnahme der 24 Seiten aus 94c1371eb — 15 bestätigt, 9 berichtigt: Berichte und Kosten (lose Positionen auch
  über den Papierkorb löschen), Einstellungen (Anwendungsordner in der Rubrik Anwendung, Knopf „Standardwerte“, Firma getrennt vom
  Logo), PD/Programmablauf und Klimadaten festlegen (Klimawahl im Kopf der Startmaske, Herkunftszeile Quelle/Bezeichnung/Standort/
  Importdatum), Prozesswärme (Feld „Einheit:“ MWh/kWh), Erzeuger und Speicher auswählen (BHKW ohne Teillastkennlinie,
  Arbeitstemperatur Solarthermie fest oder aus dem Speicher), Simulation konfigurieren und starten (Bedarfsart wörtlich),
  Ergebnisse auswerten (Bemessung, Staffel nur Leistungspreis), Programmfunktionen (Hilfeknöpfe, EPOS-Marke).
- a5244bee2 + a044fd261 (Orchestrierung): Live-Seite „Programmablauf“ byte-gleich als Repo-Quelle übernommen, dann Schritt 1
  (Komponenten über die Reiter der Startseite), Schritt 3 (Heizen und Kühlen), Schritt 5 (Kältemaschine); in `seiten.tsv`.
- Nach allen Commits: Kern-Wachen 319/319 (`--no-build`); Parse-Prüfung aller Quellen: nur die drei gewollten pre-Kästen und
  Verweise auf die drei mit dem Upload entstehenden Seiten.

## Upload (10.10.2026)
- Seiten: 99 gespeichert (96 ersetzt, 3 neu: Betriebskalender, Nutzungsprofile, Pufferspeicher auslegen), Revisionen 728–826;
  jede Seite byte-gleich zurückgelesen, 0 Parse-Warnungen; 19 Seiten gleich; Dateien und 19 Weiterleitungen gleich.
- Zurückgehalten: Programm Dokumentation/Gebäudeimport (Repo-Quelle fertig) — der Anwender hat zur Frage „Befund“ mit
  „Something else“ ohne Text geantwortet; Upload der Seite nach seiner Antwort.
- Logbuch: Revision 827, 70 Sätze in 1.2.0.10 (29), 1.2.0.9 (1), 1.2.0.8 (4), 1.2.0.7 (36), eingefügt vor „Version 1.2.6 –
  September 2026“; Einleitungssatz berichtigt.
- Verbindungsabbrüche (Connection reset) während der Läufe: Starter `wu_lauf.py` im Scratchpad wiederholt abgebrochene Anfragen
  (zwei Wiederholungen nötig); das Werkzeug im Repository blieb unverändert. Vorschlag: Wiederholung ins Werkzeug übernehmen.
- Revisionstabelle (100 Zeilen): `revisionen_885.md` neben dieser Datei.

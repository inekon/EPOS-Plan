# Wiki-Update 26.09.2026 — Vorbereitung des Sammel-Uploads

**Freigabe des Anwenders 24.09.2026 für den Termin 26.09.2026** — Version 1.2.0.4; hochgeladen
wird durch die Orchestrierung (Konzept Hilfesystem 13.3), nicht durch einen Agenten.

Dieses Papier bereitet den gebündelten Wiki-Upload vor (Regel: Konzept Hilfesystem 13.3). Der
Termin ist der **26.09.2026** (E12‑Q1, entschieden 24.09.2026 nach Empfehlung: a; dieses Papier,
seit dem vorliegenden Auftrag vorgezogen), das Analysepapier nannte zuvor durchgehend den
28.09.2026; der Anwender hat den Upload am 24.09.2026 für diesen Termin freigegeben. Mit E12
sind die Repo-Quellen so weit nachgezogen, dass der Sammel-Upload am Termin laufen kann
(Abschnitt 4 nennt den Ablauf). Es listet die Seiten, deren Repo-Quelle seit dem letzten
Upload (Version 1.2.0.0, Auftrag #252) fortgeschrieben wurde, sammelt die dazu entworfenen
Logbuch-Sätze geordnet nach Version und nennt, was zum Stichtag noch offen ist. Quelle aller
Angaben ist [`Status_iOS_Migration.md`](Status_iOS_Migration.md); die Statusdatei selbst ist
hier nicht geändert. Die Versionsnummer ist **1.2.0.4** für den ganzen Sammel-Upload (E12‑Q3,
entschieden 24.09.2026 nach Empfehlung: a; Regel: Konzept Hilfesystem 13.3), mit der Freigabe
vom 24.09.2026 bestätigt.

## 1 Seiten für den Sammel-Upload

| Wiki-Seite | Repo-Quelle | Was sich geändert hat | Quelle |
|---|---|---|---|
| Programm Dokumentation/Klimadaten | `Projekte/Wiki/Programm Dokumentation - Klimadaten.wiki` | neue Seite: Quellenwahl PVGIS/DWD-Testreferenzjahr, Standort aus dem Dateikopf, Regionsvorschau, durchsuchbare Liste mit Quelle/Bezugsjahr/Szenario, Diagrammzoom; die vier Anker der Live-Seite (`bezeichnung`, `daten-einlesen`, `koordinaten`, `region`) bleiben als Zweitnamen erhalten (#507); mit #527 ein Satz zur Auswahl im Projektassistenten (dieselbe durchsuchbare Liste wie auf der Übersicht) | Statuszeilen #367, #368, #369, #371, #382, #396, #404, #413 |
| Programm Dokumentation/Simulationsergebnisse | `Projekte/Wiki/Programm Dokumentation - Simulationsergebnisse.wiki` | Farbwahl der Diagramme in den Einstellungen und die neue Diagrammbedienung (Zoom, Werteleiste, schaltbare Legende); jede Kurve und Fläche trägt ein Farbfeld, die Reiter zeigen die Farben des Berichts; die Kältedeckung in der Übersicht (Block und dritter Ring, Anker `kaeltedeckung`) und der Block „Kälte" im Reiter Wärme-/Strombedarf; mit E12 eine Tabuwort-Bereinigung (keine inhaltliche Änderung) | Statuszeilen #403, #411, #413, #418; Status der Gebäudesimulation, Stufe KU2; E12 |
| Programm Dokumentation/Stromspeicher | `Projekte/Wiki/Programm Dokumentation - Stromspeicher.wiki` | Beschreibung der Auslegungsbilder auf die neue Diagrammbedienung nachgezogen; neuer Abschnitt „Mögliche Optimierungen" (Betriebsziele, adaptive Lastspitzenkappung, Auslegung, Verteilung, Grenzen); mit E12 eine Tabuwort-Bereinigung (neun Stellen, keine inhaltliche Änderung); mit #483 im schmalen Fenster die Kopfhandlungen „CSV-Export" und „In Variante übernehmen" zusätzlich im Kopf des Stammblatts (Anker `maske-lastspitzenkappung`) | Statuszeilen #411, #413, #417, #483; E12 |
| Programm Dokumentation/Hilfe-Assistent | `Projekte/Wiki/Programm Dokumentation - Hilfe-Assistent.wiki` | Liste der Masken, in denen der Assistent mitarbeitet (Erzeugermasken des Projekts, Katalogmasken, Kostenverwaltung, Simulationskonfiguration, Ansichten); wächst mit den Wellen KI‑F1b bis F6; mit E12 eine Tabuwort-Bereinigung (keine inhaltliche Änderung) | Statuszeilen #416, #419; E12 |
| Programm Dokumentation/Wirtschaftlichkeit | `Projekte/Wiki/Programm Dokumentation - Wirtschaftlichkeit.wiki` | Menüweg und Durchsuchbarkeit der gesetzlichen Parameter, Kohärenzzeile ergänzt; die Ergebnisseite neu beschrieben — Umschalter „Kennzahlen / ValERI-Bewertung", vier Abschnitte, Empfehlungskarten, Bandbreite mit Spannenbild, Verlauf mit drei Szenarien (Anker `verlauf`), Sensitivität mit Steigung, Szenarioabdeckung und Deklarationen, „Bericht erzeugen", „Verlauf nach Excel…", Szenarien Ungünstig/Erwartet/Günstig; die CO₂-Prüfung der Stromsteuerbefreiung brennwertbezogen und die vermiedenen Stromkosten ohne jede Eigenerzeugung mit dem Anteil je Anlage; die Fußleiste ohne „Strombezug…", die Tarifstruktur im Rollenmodell (Anker `strombezug`), die Leistungspreis-Staffel in den Energiekosten; der KWK-Strom nach § 2 Nr. 16 mit Fall 2 und Stromkennzahl in der Überlagerung „Sätze und Herkunft…" (Anker `kwk-abwaermeabfuhr`), die Frist zur Inbetriebnahme bis 31.12.2030 (Anker `kwk-frist`), die Kohärenzzeilen „Anlagenart fehlt" und „Stromkennzahl fehlt"; die ganze Überlagerung „Sätze und Herkunft…" mit Anlagenart, Tatbestand, Satztafel, „Wirkung Jahr 1", Energie- und Stromsteuer (neuer Anker `kwk-saetze-herkunft`), die Vollbenutzungsstunden nach der erzeugten Arbeit in beiden Fällen und der Rundungsgrund (Anker `kwk-abwaermeabfuhr`), die gesperrte Mischlage § 53/§ 53a neben § 54 (Anker `block-a`, `kohaerenz`), § 51a mit der Einspeisevergütung und der ungerundete Vergütungssatz (Anker `pv-verguetung`), die KWKG-Modultafel mit den Spalten zu Fall 2; die Wahlen der Überlagerung als Zeilen mit ihrer Wirkung und Satz und Betrag je Energiesteuerentlastung (Anker `kwk-saetze-herkunft`), die Warnung bei einer nicht ausführbaren Prüfung oder Rechenstufe (Anker `kohaerenz`); die fünf Blöcke der ValERI-Bewertung mit Block 2 „Zahlungsreihen" samt Zahlungsstrombild (neuer Anker `zahlungsreihen`) und Block 4 mit Spannenbild und Verlauf (Anker `valeri`, `spanne`, `verlauf`), die Gliederung des Kapitalwerts mit Nominalsumme und Differenzspalte und das Brückenbild (neue Anker `gliederung`, `bruecke`), die Tafel „Was daraus im Lauf wird" und die Fußzeile (neue Anker `laufwirkung`, `szenariofuss`), Brückenbild und Zahlungsstrombild im Word-Bericht (Anker `bericht`); die Excel-Arbeitsmappe mit Formeln — Parameterblock, Mehrjahrestabellen, Kennzahlen, Betriebskosten Menge × Satz (Anker `bericht-excel`, neuer Anker `formelmappe`) — und die Anhang-E-Checkliste auf der Ergebnisseite und in beiden Berichten (neuer Anker `checkliste`; `darstellung`, `valeri`, `szenariofuss`, `bericht`); die Betriebskostentabelle der Berichte mit der Bemessungsart jeder Position, „ab Jahr …" bei späterem Startjahr und dem Hinweis nur bei einer echten Lücke (neuer Anker `bericht-betriebskosten`); die weiteren Werte je Szenario — Betrachtungszeitraum, Mengenänderung, Energieträgerpreise und Erlössätze, leer wie Erwartet —, gepflegt in den Zeilen 8 und 9 der Szenariotabelle und mit dem Knopf ± an Trägerkarte, Einspeisevergütungen, DV-Entgelt und PPA-Preis samt dem Fenster „Szenariowerte" (neuer Anker `szenariowerte`; `szenarien`, `einspeiseverguetung`, `bhkw-wirtschaftlichkeit`, `pv-verguetung`), dazu Szenariozeile, Annahmentafel, Verlauf, Annahmenzeilen und Parameterblock der Formelmappe mit Zeitraum und Einspeisevergütung (Anker `szenario`, `annahmen`, `verlauf`, `bericht`, `bericht-excel`, `formelmappe`) und unter der Annahmentafel, in Block 4, in beiden Berichten und in Punkt 9 der Checkliste die Szenarioabdeckung „n von m Parametern szenariert" (neuer Anker `szenarioabdeckung`; `valeri`, `checkliste`); mit E12 die Satzherkunft-Zeilen der Erlösrubrik unter Einspeisung/Eigenstrom (U23, Anker `block-a`), beide PV-Anlagenwarnungen (U36, Anker `pv-verguetung`) und ein Verweis auf den nach Kosten verschobenen Abschnitt „Gesetzliche Parameter" (A18, Anker `gesetzliche-parameter` bleibt bestehen); mit #474 Punkt 9 der Anhang-E-Checkliste „erfüllt", sobald Günstig und Ungünstig gerechnet sind (Anker `checkliste`), und der Grund in der Statuszeile und im Dialog BHKW-Wirtschaftlichkeit, wenn gespeicherte Ergebnisse nicht gelesen oder Eingaben nicht gespeichert werden konnten (Anker `nicht-berechnet`, `bhkw-wirtschaftlichkeit`); mit #477 die Formelmappe je Szenario — Mehrjahrestabellen, Kennzahlen, Zinsfuß und Bandbreite für Erwartet, Günstig und Ungünstig mit Formeln auf die Spalte des Szenarios, Jahre nach dem Zeitraum eines Szenarios leer über Schutzformeln, der Zinsfuß „nicht eindeutig" bei mehreren Vorzeichenwechseln (Anker `formelmappe`); mit #478 das Risiko nach DIN EN 17463 in der Gruppe „Risiko" des Parameterdialogs — Zinszuschlag oder Zahlungsstromabzug R_loss × p_loss, Wirkung, Ausweis in Szenariozeile, Annahmentafel, Deklaration, Checkliste, Gliederung und Formelmappe (Anker `risiko`, das Ziel des Hilfeknopfs der Gruppe; `nicht-monetaer`); mit #479 die nicht monetären Wirkungen als Liste mit Kategorie, Dauer, drei Wirkungen und Beurteilung, das Altfeld, die Tabelle in beiden Berichten und die Punkte 2b und 3b der Checkliste (Anker `nicht-monetaer`, das Ziel des Hilfeknopfs der Liste; `checkliste`); mit #484 die Positionen „alle n Jahre" in der Betriebskostentabelle der Berichte und in der Formelmappe (Anker `bericht-betriebskosten`, `formelmappe`); mit #492 im Dialog BHKW-Wirtschaftlichkeit der erfasste Stromsteueranteil unter der Unternehmensart (Punkt „Unternehmensart und erfasster Stromsteueranteil" unter dem Anker `bhkw-wirtschaftlichkeit`); mit #498 im Parameterdialog, Abschnitt Strom, die Unternehmensart für Vergleichsgruppen ohne BHKW samt Anzeige des erfassten Stromsteueranteils (neuer Punkt „Unternehmensart“ unter dem Anker `parameter`; ein Halbsatz bei `bhkw-wirtschaftlichkeit`); mit BV-E1 im Abschnitt „Bericht“ der Punkt „Word-Vorlage“ mit Verweis auf die neue Seite Berichtsvorlagen (neuer Anker `bericht-vorlage`) | Statuszeilen #372, #405, #413, #434, #436, #437, #439, #440, #446, #452, #454, #455, #460, #461, #462, #474, #477, #478, #479, #484, #492, #498, #512; E12 |
| Programm Dokumentation/Kosten | `Projekte/Wiki/Programm Dokumentation - Kosten.wiki` | neuer Punkt „Leistungspreis-Staffel" (Anker `staffel`) beim Stromträger; die Preiswirkung ohne Zonenpreise; neuer Punkt „Ersatzbeschaffung und Restwert je Position" (Anker `ersatz-restwert-kennzeichen`), die gespeicherte Preisbasis (Anker `preisbasis`), Nm³ und kWh im Brennstoffkatalog (Anker `einheiten`), „% der Brennstoffkosten" und „% der Stromkosten" aus dem Simulationslauf (Anker `laufgroessen`); die Knöpfe ± der Trägerkarte (Anker `traegerkarte`); die Sätze für Instandsetzung und Wartung im Dialog Nutzungsdauern (AfA) (neuer Anker `nutzungsdauer-saetze`), der Knopf „Sätze vorbelegen…" der Betriebskostenseite und die Herkunft des Satzes (neuer Anker `saetze-vorbelegen`), Ersatz und Restwert der Einheiten einer Speicherflotte (neuer Anker `flotte-restwert`); mit E12 die Doppelpflege-Warnung der Hilfsenergie (Anker `laufgroessen`) und der volle Abschnitt „Gesetzliche Parameter" (A18, neuer Anker `gesetzliche-parameter`, von der Seite Wirtschaftlichkeit hierher verschoben); mit #474 die Nutzungsdauer einer neu angelegten Speichervariante aus der Zeile „Stromspeicher · Batterie" (Anker `nutzungsdauern`) und die Hilfe des Zeileneditors und der saisonalen Leistungspreise auf ihren Abschnitt (Anker `ersatz-restwert-kennzeichen`, `preiswirkung`; `help_mapping` mit #474); mit #484 das Feld „Zahlung alle n Jahre" im Zeileneditor der Betriebsseite — Zahlungsfolge ab dem Startjahr, Wirkung auf die Wirtschaftlichkeit, Kostenvorlagen und Übernahme (neuer Anker `zahlung-alle-n-jahre`); mit #502 an der Wärmepumpe die Bemessung „je kW elektrisch“ nur bei den Investitionskosten, Bezugsgröße die elektrische Leistungsaufnahme am Normpunkt der Kennlinie — Tafelzeile, Absatz mit Rechenregel und Beispiel, Halbsatz zur Betriebskostenseite (Anker `bemessung`); mit #510 an der Wärmepumpe keine Bemessung der Betriebskosten je kWh — die Wärmepumpe nicht mehr bei „je kWh thermisch“ und „je kWh elektrisch“, „je Stunde“ nur noch am BHKW, dazu der Satz zur vorhandenen Position mit dem Vermerk „Altbestand“ (Anker `laufgroessen`) | Statuszeilen #439, #446, #462, #463, #474, #484, #502, #510; E12 |
| Programm Dokumentation/Pufferspeicher | `Projekte/Wiki/Programm Dokumentation - Pufferspeicher.wiki` | Aufzählung der Erzeugerseite: der Aufklapper „Alle Daten anzeigen" mit den Investitionskosten statt des entfallenen Detailfelds | Statuszeile #422 |
| Programm Dokumentation/Gebäudemodell VDI 6007 | `Projekte/Wiki/Programm Dokumentation - Gebäudemodell VDI 6007.wiki` | neue Seite: stündliches Gebäudemodell nach VDI 6007 als Vorgabe, Tagesbilanz als wählbarer Bestandsweg, Eingaben der Gebäudehülle, Luftwechsel mit Sommerlüftung, Strahlung auf die Außenbauteile, Kennzahlen und Raumtemperatur im Wärmebedarf, Vergleich der Rechenwege; Gebäude ohne Kühlung laufen im Sommer frei; der Abschnitt „Heizkreis und Wärmeübergabe" (Anker `heizkreis`, `sollwert-zeitprogramm`) mit Übergabeart, Auslegungspunkt, Heizkurve, Raumregler und Sollwert-Zeitprogramm, dazu Kacheln und Bild „Vorlauf und Rücklauf" im Wärmebedarf und die Grenzen der Übergabe; der Abschnitt „Kühlübergabe" (Anker `kuehluebergabe`) mit Schalter, Kühlübergabeart, Auslegungspunkt, Nennleistung aus dem Auslegungstag, festem Kaltwasser-Vorlauf und Vorlaufgrenze als Vorgabe, dazu Kacheln und Bild „Kühlvorlauf und Kühlrücklauf" im Abschnitt Kältebedarf und die Grenzen der Kühlübergabe; mit G3 der Abschnitt „Bauteilweg" (Anker `bauteilweg`): Gebäude mit Zone rechnen über ihre Bauteile mit Schichtaufbau, Hochrechnung bei der Übernahme als eine Zone und Flächenschlüssel; mit E43 die Nutzungszeit der mittleren Raumtemperatur als Zeit außerhalb der Nachtzeit des Gebäudes (Verweis auf den Anker `nachtzeit`) | Status der Gebäudesimulation, Stufen G1 + G2, KU1, AK1 und G3 (E27–E32, E36, E37, E40); E43 (N1.48) |
| Programm Dokumentation/Kühlung | `Projekte/Wiki/Programm Dokumentation - Kühlung.wiki` | neue Seite: Kühlung je Projekt und Gebäude, Kühlsollwert und Kühlleistungsgrenze, Programmeinstellung „Neue Projekte mit Kühlung anlegen", Kältebedarf in Dialogen, Ergebnis und Bericht, sensible Kälte ohne Entfeuchtung; die Deckung durch die Wärmepumpe im Kühlbetrieb — Felder der Gruppe „Kühlbetrieb" (Anker `kuehlbetrieb`), Kälteleistung, Kältestrom und Jahresarbeitszahl Kälte (Anker `kaelteerzeugung`), Stromträger und Abrechnung des Kältestroms (Anker `abrechnung`) —, Kältedeckung in Übersicht und Bericht, Kältestrom in der Wirtschaftlichkeit samt Grund- und Leistungspreis eines eigenen Zählers, Grenzen; der Verweis auf den Abschnitt „Kühlübergabe" des Gebäudemodells (E37) | Status der Gebäudesimulation, Stufen KU1, KU2 und AK1 (E27, E31–E35, E37) |
| Programm Dokumentation/Gerätekataloge | `Projekte/Wiki/Programm Dokumentation - Gerätekataloge.wiki` | der Schalter „nur mit Kühlfunktion" filtert nach der Kühlleistung, gerechnet wird mit der Kühlkennlinie; die Importregel der Kühlkennlinien (Heizlage, vertauschte Achsen; Anker `import-kuehlkennlinien`); die Nutzungsdauer von Heizkessel und BHKW als Gerätedaten, maßgeblich ist die Nutzungsdauertabelle; **Neuanlage** (die Seite besteht im Wiki noch nicht, Anwenderentscheid 25.09.2026); Hilfeknöpfe der acht Verwaltungen zeigen mit #509 auf die Seite (`help_mapping.txt`: je Verwaltung der Abschnitt ihres Gerätetyps, Anker `heizkessel`, `bhkw`, `waermepumpen`, `solarkollektoren`, `pufferspeicher`, `pv-module`, `wechselrichter`, `stromspeicher`); mit #511 auch Dubletten- und Import-Dialoge (`Form_KatalogDubletten.btn_Help` → `dubletten`, die Katalogimporte `Form_*_einlesen.btn_Help`, `Main_PV_Test.btn_Help` und `Form_WechselrichterImport.btn_Help` → `import`) | Status der Gebäudesimulation, Stufe KU2; Statuszeilen #463, #509 |
| Programm Dokumentation/Simulation | `Projekte/Wiki/Programm Dokumentation - Simulation.wiki` | neuer Punkt „Kühlbetrieb der Wärmepumpe" in der Simulationskonfiguration (Anker `kuehlbetrieb`); neuer Punkt „Anlagenkopplung" (Anker `anlagenkopplung`) samt der Kälteseite (E37); mit E12 eine Tabuwort-Bereinigung (keine inhaltliche Änderung) | Status der Gebäudesimulation, Stufen KU2 und AK1; E12 |
| Programm Dokumentation/Photovoltaik | `Projekte/Wiki/Programm Dokumentation - Photovoltaik.wiki` | mit E12 eine Tabuwort-Bereinigung der Ampel-Meldungen (drei Stellen „Befund" → „Diagnose"/„Meldung"; keine inhaltliche Änderung) | E12 |
| Programm Dokumentation/Varianten | `Projekte/Wiki/Programm Dokumentation - Varianten.wiki` | mit E12 der Kohärenzhinweis „übernehmen ohne Stammprojekt" am Abschnitt PV-Vergütung der Variante (Anker `pv-verguetung`) | E12 |
| Programm Dokumentation/Gebäude | `Projekte/Wiki/Programm Dokumentation - Gebäude.wiki` | **Repo-Quelle neu angelegt** aus dem Live-Stand (`action=raw`, 3 654 Zeichen, zwölf Anker) und dem Ist-Zustand der Oberfläche: Projektdialog mit Übernahme ins Projekt (Kopie mit Katalogverweis, Neuschreiben der Liste mit „OK"), Katalogeditor mit seinen zwei Reitern, die Verwaltung Gebäude mit Liste, Auswahlleiste (Vergleichen, Duplizieren…, Schloss, Löschen mit Nutzungssperre), Stammblatt (Kenndaten, Hülle, Fenster, Kenngrößen, „Alle Daten"), Speichern/Verwerfen, Neu…, Schloss der Auslieferungssätze, Fußleiste, Hilfe-Assistent und Grenzen (kein Wärmebedarf ohne Projekt); die zwölf Live-Anker bleiben, 25 kommen dazu (u. a. `verwaltung`, `stammblatt`, `katalogeditor`, `gebaeudetypen`, `loeschen`, `schloss`, `assistent`, `grenzen`); `help_mapping.txt` zeigt mit `Form_Gebaeude1`/`Form_Gebaeude2` auf `katalogeditor` und mit `Form_EingGebTyp` auf `gebaeudetypen`; mit G4 der Punkt „Importieren (gbXML, IFC)…" in der Katalogleiste (neuer Anker `import`) und der Verweis im Katalogeditor; mit G3 der Abschnitt „Gebäude im Projekt: Hülle und Zonen" (neue Anker `huelle-und-zonen`, `zone-uebernehmen`, `zonen`, `bauteile`, `zone-katalog`) mit dem Knopf „Hülle und Zonen…", der Übernahme als eine Zone, Zonen- und Bauteildialog, der Skalierungsangabe mit Zone und dem Rechenweg der Hülle im Katalogeditor; mit E43 die Nachtzeit je Gebäude im Reiter „Temperaturen und Ferien" und in „Alle Daten" des Stammblatts (Felder „Nachtabsenkung von … bis", volle Stunde 0 bis 23, leer = 22 bis 6 Uhr, beide oder keine; neuer Anker `nachtzeit`); mit E47 der Abschnitt „Baualtersklasse und Energiestandard“ (neue Anker `baualtersklasse`, `energiestandard`): 13 Bauzeiträume A bis M mit Quelle (IWU 2015, ab 2016 Stein/Loga 2025), das Baujahr führt (Klappliste gesperrt, gespeichert die Klasse zum Jahr), der Energiestandard als freiwilliges Feld nach der Verwendung, die Vorgaben des Imports je Klasse ohne Leihen; die Listenspalte heißt „Baualtersklasse“, der Energiestandard steht in Kenngrößen, Kenndaten und Vergleich; mit G4b der Knopf „Baustoff-Zuordnungen…“ in der Fußleiste (neuer Anker `baustoffzuordnungen`): die Zuordnungen von Materialnamen zu Baustoffen, die sich das Projekt beim Gebäudeimport gemerkt hat, mit Baustoff und Zeitpunkt, einzelne entfernbar | Statuszeilen #465, #468, #473; #476; Status der Gebäudesimulation, Stufen G4, G3 und G4b; E43 (N1.48); E47 (N1.52) |
| Programm Dokumentation/Baustoffe und Bauteilaufbauten | `Projekte/Wiki/Programm Dokumentation - Baustoffe und Bauteilaufbauten.wiki` | **neue Seite**: die Verwaltungen „Baustoffe" (Liste mit Filtern, Kenndaten, Herkunft, Auslieferungssätze mit Schloss) und „Bauteilaufbauten" (Schichtenraster mit Dicke, Stoffwerten und ruhender Luftschicht, Summenfuß mit R, U, Kapazität und Bezugsperiode, Speichern und Neu) unter Administration → Gebäude; neutrale Beispiele, keine Hersteller- oder Produktdaten; Anker `baustoffe`, `bauteilaufbauten`, `schichten`, `summen`, `speichern`, `help_mapping.txt` mit `BaustoffKatalog.btn_Help` und `Bauteilaufbau.btn_Help` | Status der Gebäudesimulation, Stufe G3 (E39); mit #540 ein Satz zum Knopf „Kataloge…“ der Projektliste auf dem iPad |
| Programm Dokumentation/Gebäudeimport | `Projekte/Wiki/Programm Dokumentation - Gebäudeimport.wiki` | **neue Seite**: Einstieg im Gebäudedialog, Dateiarten und Größengrenzen, Baualtersklasse zuerst, Quelle mit dem Hinweis auf eine schon importierte Datei, Raumliste, Zuordnung mit Herkunft je Feld (Datei, Vorgabe, manuell, leer), was nicht aus der Datei kommt (samt den Vorgaben für Luftwechselrate, Fläche je Nutzer und innere Gewinne), vorbelegter Katalogeditor mit den Vorgaben in der Herleitungszeile, Übernahme ins Projekt und Herkunft im Projekt; `help_mapping.txt` zeigt mit `Form_GebaeudeImport.btn_Help` auf den Anker `zuordnung`; mit E43 die Vorgaben, wenn die Datei nichts liefert: innere Gewinne 5 W/m² × Nutzfläche (ohne Nutzfläche 0 W), Soll am Tag 20 °C, Heizsollwert in der Nacht 18 °C (höchstens das Soll am Tag), Nachtabsenkung 22 bis 6 Uhr, alle änderbar; mit G4b der Abschnitt „Bauteile (echte Hülle)“ — Schalter „Als Zone mit Bauteilen übernehmen“ samt Sperre mit Grund, Bauteilliste, Schichtaufbauten und U-Werte, Randbedingung unbeheizt, Vorhangfassaden, innere Masse — und die Zeile Innenflächenfaktor der Zuordnung, Zone und Aufbauten an der Projektkopie, Grenze „eine Zone je Gebäude“; dazu der Abschnitt „Baustoffe“ (Zuordnung der Materialnamen zum Baustoffkatalog über Name und Synonyme, eigene Zuordnung, die das Projekt behält, Luftschichten, verworfene Schraffuren) mit dem Verweis auf die Ansicht der gemerkten Zuordnungen im Gebäudedialog (Knopf „Baustoff-Zuordnungen…“) | Status der Gebäudesimulation, Stufe G4 (E38); E43 (N1.48); Stufe G4b (E44, E45; N1.49) |
| Programm Dokumentation/Brauchwasser-Zapfprofil | `Projekte/Wiki/Programm Dokumentation - Brauchwasser-Zapfprofil.wiki` | **neue Seite** (Repo-Quelle seit der Stufe Z1 des Zapfprofilgenerators, nie hochgeladen): Rechenweg Brauchwasser, Eingaben je Stufe (Einfach, Erweitert, Experte), Vorschau, Hinweise und Prüfung, Stochastik, Auslegung, Katalog der Brauchwasser-Nutzungsarten, Typtage nach VDI 4655, Messdaten, Vergleich und Kalibrierung, Katalog-Import mit Paketvorlage A100 und Steuerspalte „Gruppe“, Karte „Herkunft“ des Ergebnisbereichs; 35 Anker | Statuszeilen #443, #451, #453, #464, #486, #495, #504, #508, #516, #517, #522, #524, #540 |
| Programm Dokumentation/Berichtsvorlagen | `Projekte/Wiki/Programm Dokumentation - Berichtsvorlagen.wiki` | **neue Seite** (Neuanlage beim Upload): die Gruppe „Vorlage“ der Berichtsseite — Auswahl mit Schloss der Standardvorlage, „Neue Vorlage…“, „Hinzufügen…“, „Prüfen“, „Platzhalter…“, Menü „…“ je Plattform —, die Prüfung vor jedem Bericht mit der Rückfrage und ihren drei Wegen, die Schreibweise der Platzhalter, Prüfzeile und Prüfliste, der Platzhalterkatalog, die Standardvorlage und im Abschnitt „Bericht“ der Einstellungen Firma und Vorlagenordner; Anker `vorlage`, `neue-vorlage`, `erstellen`, `schreibweise`, `pruefliste`, `platzhalterkatalog`, `standardvorlage`, `einstellungen`; `help_mapping.txt` zeigt mit `UcBericht.btn_Help_Pruefliste`, `UcBericht.btn_Help_Platzhalterkatalog` und `Form_AdminSettings.btn_Help_Bericht` auf `pruefliste`, `platzhalterkatalog` und `einstellungen`; neutrale Beispiele, keine Hersteller- oder Produktdaten; mit BV-E2 der Abschnitt „Kapitel“ (neue Anker `kapitel`, `haekchen`) — die Kapitelplatzhalter `{{kapitel.…}}` mit `\|ohne titel` und `\|ebene`, der Kapitelkopf im Format „EPOS Kapitelkopf“ und sein Entfall, die Stelle der Anhang-E-Checkliste, ausgegraute Häkchen mit Grund und die Zeile „Den Inhalt bestimmt die Vorlage“ —, das Bild als Platzhalter, die Standardvorlage im vollen Aufbau und in den Einstellungen das Feld „Logo“ (neuer Anker `logo`); die Anker der Hilfeschlüssel bleiben | Statuszeilen #512 (BV-E1), #520 (BV-E2) |

Die Liste folgt der zusammenfassenden Aussage der Statuszeile #413: „Die Seiten Klimadaten,
Simulationsergebnisse, Stromspeicher und Wirtschaftlichkeit sind in den Repo-Quellen
fortgeschrieben — Sammel-Upload 28.09.2026“ (das dort genannte Datum ist durch den
vorliegenden Auftrag auf den 26.09.2026 vorgezogen; das Analysepapier nennt weiterhin durchgehend
den 28.09.2026 — der Anwender hat am 24.09.2026 den 26.09.2026 freigegeben, siehe
Abschnitt 4). Eine gesonderte Seite zu Gerätekatalogen nennt
keine der ausgewerteten Statuszeilen als upload-bereit; die Seite Kosten kommt mit #439 hinzu, die
Seiten Gerätekataloge und Simulation kommen mit der dritten Welle der Stufe KU2 hinzu (die Seite
Gerätekataloge als Neuanlage, Anwenderentscheid 25.09.2026, #509); die Seiten
Photovoltaik und Varianten kommen mit E12 hinzu (Tabuwort-Bereinigung der Ampel-Meldungen
beziehungsweise Kohärenzhinweis am Abschnitt PV-Vergütung); die Seite Baustoffe und Bauteilaufbauten
kommt mit der Stufe G3 der Gebäudesimulation hinzu (Gegenlese-Muster ohne Treffer, Wache der
Produktdaten grün); die Seite Berichtsvorlagen kommt mit BV-E1 (#512) als Neuanlage hinzu, die Seite
Wirtschaftlichkeit dazu um einen Satz im Abschnitt „Bericht“ (Gegenlese-Muster ohne Treffer); mit BV-E2 (#520) wächst die
Seite Berichtsvorlagen um Kapitel, Häkchen und Logo (Gegenlese-Muster ohne Treffer, Wache der Produktdaten grün).

**Hinweis zum Arbeitsstand:** Die Repo-Quelle der Seite Stromspeicher trägt seit diesem
Auftrag zusätzlich einen neuen Abschnitt „Mögliche Optimierungen“ (Aufgabe A desselben
Auftrags) — dafür gibt es noch keine Statuszeile, weil er in derselben Sitzung entsteht. Vor
dem eigentlichen Hochladen der Seite ist er mitzunehmen.

## 2 Logbuch-Einträge für die Wiki-Seite „Update-Logbuch“

Reihenfolge neueste Version oben. Ein Satz je wesentlicher, sichtbarer Änderung, ohne
Einzelheiten und Begründung (Regel: Konzept Hilfesystem 13.4); Kleinigkeiten sind bereits
ausgefiltert (Statuszeilen mit „Kein Logbuch-Satz“).

### Version beim Anwender zu erfragen — Berichtsvorlagen (BV-E1, BV-E2)

Ob die Sätze zu BV-E1 und BV-E2 mit dem Sammel-Upload unter 1.2.0.4 erscheinen oder unter einer eigenen Versionsnummer, ist
beim Anwender zu erfragen (Stichwort `bericht`); das Datum folgt der Veröffentlichung.

- Seit 26.09.2026: Berichtsvorlagen führen Kapitel einzeln als Platzhalter; die Berichtsseite graut Kapitel aus, die die
  gewählte Vorlage nicht führt, und in den Einstellungen lässt sich ein Firmenlogo für die Kopfzeile des Berichts
  hinterlegen. (#520)
- Seit 26.09.2026: Der Word-Bericht wird aus einer Vorlage gefüllt: Auf der Berichtsseite lässt sich eine eigene
  Word-Vorlage mit Platzhaltern wählen, anlegen, hinzufügen und prüfen; den Platzhalterkatalog zeigt die Berichtsseite,
  Firma und Vorlagenordner stehen in den Einstellungen. (#512)

*Zu #520 (Stichwort `bericht`):* Ein Satz — Kapitel als Platzhalter, ausgegraute Häkchen und das Firmenlogo. Ohne eigenen
Satz bleiben die Formatangaben `|ohne titel` und `|ebene`, der Kapitelkopf, die Stelle der Anhang-E-Checkliste aus der
gewählten Vorlage und die Standardvorlage im vollen Aufbau (dieselbe Funktion).

*Zu #512 (Stichwort `bericht`):* Ein Satz — die Word-Vorlage mit Platzhaltern auf der Berichtsseite. Ohne eigenen Satz
bleiben die Rückfrage vor dem Start mit ihren drei Wegen, die Prüfliste, der Weg über „Bericht erzeugen“ der
Wirtschaftlichkeit und die Felder des Hilfe-Assistenten (dieselbe Funktion) sowie die Inhaltsbreite aus der Vorlage
(nicht sichtbar).

### Version 1.2.0.4 — eine Version für den ganzen Sammel-Upload

Anwenderentscheid 22.09.2026 („letzte Nummer erhöhen"; das Programm trägt heute 1.2.0.3 in
`AssemblyInfo.cs`, die Anhebung gehört zur Auslieferung). E12‑Q3 (**entschieden 24.09.2026, nach
Empfehlung: a**): Die früher getrennt entworfenen Versionen 1.2.0.2 (05/§ 7, Statuszeilen
#343–#367) und 1.2.0.3 (Statuszeilen #358–#403) stehen ab dieser Fassung zusammengeführt unter
1.2.0.4 — der Sammel-Upload veröffentlicht **eine** Version, nicht eine je Etappe; die eigene
Versionsüberschrift „1.2.0.3" entfällt entsprechend weiter unten.

Die Diagramm-Umstellung auf Vektorgrafik (DG-E3) ist mit Statuszeile #413 abgeschlossen. Die
folgenden vier Sätze fassen die Zwischenstände aus #404 und #411 zusammen, die dieselbe
Bedienung an einzelnen Diagrammfamilien beschrieben und mit den programmweiten Sätzen aus
#413 ihren Gegenstand verlieren (Regel 13.4: ein Thema, ein Eintrag).

- Seit 26.09.2026: Den Zeitreihen-Diagrammen wurden ein Zoom auf der Zeitachse, eine
  Werteleiste am Mauszeiger und eine schaltbare Legende mit Farbwahl hinzugefügt. (#411)
- Seit 26.09.2026: Jedes Diagramm im Programm ist eine maßstabsfreie Vektorgrafik; die Werte
  an der Stelle, auf die Sie zeigen, stehen unter dem Bild. (#413)
- Seit 26.09.2026: Diagramme ohne Zeitachse — Deckungsringe, Monatsbilder, Rasterkarten,
  Kurven der Stromspeicher-Auslegung, Jahresprojektion, Kennlinien der Wärmepumpe — nennen den
  Wert des Elements, auf das Sie zeigen oder das Sie antippen. (#413)
- Seit 26.09.2026: Der Zeitausschnitt eines Diagramms wird im Bild selbst aufgezogen; das
  Bild wird dabei nicht mehr neu gerechnet. (#413)
- Seit 26.09.2026: Solarthermie und Photovoltaik zeigen ihre Jahreskurven auf Wunsch als
  Dauerlinie (Schalter „sortiert"). (#415)
- Seit 26.09.2026: Der Hilfe-Assistent kann die Erzeugermasken des Projekts lesen, ausfüllen
  und speichern (Heizkessel, BHKW, Pufferspeicher, Stromspeicher, Solarkollektoren, Wärmepumpe,
  Photovoltaik). (#416)
- Seit 26.09.2026: Der Hilfe-Assistent liest und setzt auch die Masken der
  Simulationskonfiguration (Pufferspeicher im Projekt, Erdreich- und Pufferspeicherquelle,
  Quellprofil, Wärmesenke, Komponentenkonfiguration) und die Einstellwerte des
  Stromspeicher-Reiters der Simulation. (#419)
- Seit 26.09.2026: Der Hilfe-Assistent setzt in den freigegebenen Masken jedes Eingabefeld,
  auch Auswahlfelder über den angezeigten Text; Feldnamen dürfen ungefähr sein. (#420)
- Seit 26.09.2026: Der Hilfe-Assistent liest und setzt auch die Masken des Bedarfs (Gebäude,
  Wohnfläche, Gebäudekatalog, Gebäude- und Bedarfstypen, Bedarfsprofile und -verwaltungen,
  Wärmebedarf extern, Solarganglinie) und die Klimadaten. (#421)
- Seit 26.09.2026: Jeder Legendeneintrag eines Diagramms hat ein Farbfeld; die
  Simulationsreiter zeigen dieselben Farben wie der Bericht, und die Einstellungen führen dafür
  54 Größen. (#418)
- Seit 26.09.2026: Die Dialoge Heizkessel und Pufferspeicher zeigen im Modulblock kein Feld
  „Investitionskosten" mehr; der Preis steht bearbeitbar im Aufklapper „Alle Daten anzeigen". (#422)
- Seit 26.09.2026: Der Hilfe-Assistent liest und setzt auch die Masken der Kosten und der
  Wirtschaftlichkeit (Energieträger, Kostenprofil, Kostenfaktoren, Emissions- und
  Nutzungsdauerkatalog, Kostenvorlagen, Wirtschaftlichkeitsparameter, BHKW-Wirtschaftlichkeit,
  Tarifstruktur, Photovoltaik-Vergütung, gesetzliche Parameter) sowie die Seiten Kosten und
  Wirtschaftlichkeit. (#423)
- Seit 26.09.2026: Der Hilfe-Assistent liest und setzt auch die Katalogmasken der Erzeuger
  (BHKW, Solarkollektor, PV‑Module, Stromspeicher, Wechselrichter). (#424)
- Seit 26.09.2026: Der Hilfe-Assistent liest und setzt auch Peak-Shaving, Speicher-Zeitreihen,
  das Zeitintervall der Stromganglinien-Verwaltung, Projektkopie und Projektvariante, die Seiten
  Übersicht und Bericht sowie alle vier Stationen der Stromspeicher-Auslegung; damit arbeitet er
  in allen Masken mit Einstellwerten mit. (#425)
- Seit 26.09.2026: Der Hilfe-Assistent erreicht in der Photovoltaik auch die Auslegungstemperaturen
  und die Anlagenwerte des Wechselrichters sowie in der Kostenverwaltung die Komponentenwahl und die
  Wahl der Photovoltaik-Vergütung; die Modulkosten der Wärmepumpe zeigt er nur noch an. (#427)
- Seit 26.09.2026: Das Übernehmen eines Standard-Stromprofils im Projekt-Assistenten funktioniert
  wieder; scheitert eine Katalogkopie, meldet der Assistent es benannt. (#426)
- Seit 26.09.2026: Der Hilfe-Assistent öffnet unter Windows auch die Kostenverwaltung, die
  Energieträgerverwaltung, die Nutzungsdauern, die gesetzlichen Parameter, die Klimadaten und die
  Projektvariante, führt zu den Erzeugermasken auf den Reiter „Energieerzeuger" und zu den Blättern
  der Ansicht „Berichte und Kosten"; im Speicher-Zeitreihen-Dialog lässt sich die Intervallkonvention
  „automatisch erkennen" wählen. (#428)
- Seit 26.09.2026: Der Hilfe-Assistent nennt Masken und Felder nach einem Sprachwechsel in der
  neuen Sprache. (#430)
- Seit 26.09.2026: Die Knöpfe „BHKW-Tarif…", „Stromtarif…" und „Tarif…" der Wirtschaftlichkeitsseite
  und des PV-Vergütungsdialogs öffnen die Tarifstruktur als Einblendung in der Seite; der
  PV-Vergütungsdialog aus dem Reiter „Ertrag/Bonus" erscheint ebenfalls als Einblendung statt als
  eigenes Fenster. Die zuletzt bearbeitete Vergleichsgruppe der Übersicht wird nach dem Update einmal
  nicht erinnert. (#431)
- Seit 26.09.2026: Die Erlösrubrik der Wirtschaftlichkeit gliedert innerhalb der Blöcke nach Anlage
  — Blockheizkraftwerk, Photovoltaik, Kessel und „projektweit", je mit Zwischensumme — und weist die
  vermiedenen Stromkosten je Anlage aus. Die Energiesteuer-Entlastung steht als zwei Zeilen (§ 53/§ 53a
  beim Blockheizkraftwerk, § 54 beim Kessel) mit Herleitung; eine Nullzeile nennt den Grund des
  Rechenlaufs. (#432)
- Seit 26.09.2026: Die Wirtschaftlichkeitsseite schaltet im Kopf zwischen den Darstellungen
  „Kennzahlen" und „ValERI-Bewertung" um; die Kennzahlen stehen in den vier Abschnitten „Lohnt es
  sich?", „Wie sicher ist das?", „Woraus entsteht die Zahl?" und „Was ist angenommen?". (#434)
- Seit 26.09.2026: Jede Version bekommt eine Empfehlungskarte mit ihrer Einstufung, und die Bandbreite
  zeigt die Szenarien Ungünstig, Erwartet und Günstig nebeneinander mit ihrer Spanne. (#434)
- Seit 26.09.2026: Die Sensitivitätstafel der Seite nennt je Einflussgröße die Steigung. (#434)
- Seit 26.09.2026: Der Knopf „Bericht erzeugen" auf der Wirtschaftlichkeitsseite erzeugt den Bericht für
  die gewählte Vergleichsgruppe. (#434)
- Seit 26.09.2026: Wort- und Tabellenbericht führen die Bandbreite mit Einstufung und die Deklarationen nach
  DIN EN 17463. (#434)
- Seit 26.09.2026: Die Wirtschaftlichkeitsseite zeigt den Kapitalwertverlauf aller drei Szenarien in einem
  Bild — Farbe je Variante, Strichart je Szenario —; der Dialog „Verlauf…" ist nicht mehr vorhanden. (#436)
- Seit 26.09.2026: Der Knopf „Verlauf nach Excel…" speichert den Verlauf als Excel-Mappe. (#436)
- Seit 26.09.2026: Ein Balkenbild zeigt die Bandbreite der Kapitalwertdifferenz je Version, auf der Seite
  und im Wortbericht. (#436)
- Seit 26.09.2026: Wort- und Tabellenbericht führen den Verlauf aller drei Szenarien; ihre Szenarientafeln
  nennen die Szenarien „Ungünstig", „Erwartet" und „Günstig". (#436)
- Seit 26.09.2026: Die Stromsteuerbefreiung nach § 9 Abs. 1 Nr. 3 StromStG prüft den CO₂-Grenzwert von
  270 g/kWh brennwertbezogen; die Herleitung nennt den Wert je Anlage. (#437)
- Seit 26.09.2026: Die vermiedenen Stromkosten im Rollentarif beziehen sich auf den Strombedarf ohne jede
  Eigenerzeugung; Blockheizkraftwerk und Photovoltaik erhalten je ihren Anteil nach dem Eigenverbrauch. (#437)
- Seit 26.09.2026: Der Zeitzonentarif (Hoch- und Niedertarif, Winter und Sommer) ist nicht mehr vorhanden; der
  Strombezug wird mit den Preisen des Stromträgers aus der Kostenverwaltung bewertet. (#439)
- Seit 26.09.2026: Die zweistufige Leistungspreis-Staffel wird in der Kostenverwaltung beim Stromträger des
  Projekts gepflegt und an der Viertelstundenspitze des Netzbezugs bemessen. (#439)
- Seit 26.09.2026: Der Knopf „Strombezug…" auf der Wirtschaftlichkeitsseite und im Dialog
  BHKW-Wirtschaftlichkeit ist nicht mehr vorhanden. (#439)
- Seit 26.09.2026: Im BHKW-Dialog lassen sich unter „Sätze und Herkunft…" je Anlage die Vorrichtung zur
  Abwärmeabfuhr und die Stromkennzahl pflegen; der KWKG-Zuschlag rechnet dann mit Nutzwärme × Stromkennzahl. (#440)
- Seit 26.09.2026: Der KWKG-Zuschlag gilt für Anlagen mit Inbetriebnahme bis zum 31.12.2030 und läuft bis zum
  Ende des Vollbenutzungsstunden-Kontingents. (#440)
- Seit 26.09.2026: Je Investitionsposition lässt sich festlegen, ob eine Ersatzbeschaffung geführt und ob ein
  Restwert angesetzt wird. (#446)
- Seit 26.09.2026: Die gewählte Preisbasis eines Energieträgers bleibt beim Wiederöffnen erhalten. (#446)
- Seit 26.09.2026: Die Gase des Brennstoffkatalogs führen ihre Menge in Nm³, der Brennstoff „Sonstige" in kWh. (#446)
- Seit 26.09.2026: Stehen Energiesteuerentlastungen nach § 53/§ 53a und nach § 54 nebeneinander, entfällt § 54,
  und eine Warnung nennt den Grund. (#446)
- Seit 26.09.2026: Betriebskosten in Prozent der Brennstoff- oder Stromkosten beziehen sich auf den jüngsten
  Lauf. (#446)
- Seit 26.09.2026: Bei fester Einspeisevergütung rechnet die Photovoltaik mit dem ungerundeten Vergütungssatz
  und bewertet § 51a EEG mit der Einspeisevergütung. (#446)
- Seit 26.09.2026: Die Vollbenutzungsstunden des KWKG-Kontingents zählen die erzeugte Arbeit des Moduls geteilt
  durch die Nennleistung. (#452)
- Seit 26.09.2026: „Sätze und Herkunft…" im BHKW-Dialog führt alle Sätze, Kontingent, Deckel, Energie- und
  Stromsteuer mit Vorschlag, Herkunft und Wirkung im ersten Jahr. (#446)
- Seit 26.09.2026: Die KWKG-Modultafel in Word und Excel zeigt die Angaben zum zweiten Fall. (#446)
- Seit 26.09.2026: Die Überlagerung „Sätze und Herkunft…" nennt für jede Energiesteuerentlastung Satz und Betrag
  im ersten Jahr und zeigt die Wirkung jeder Wahl in ihrer Zeile. (#452)
- Seit 26.09.2026: Lässt sich eine Rechenstufe der Wirtschaftlichkeit nicht ausführen, steht der Grund als Warnung
  an der Ergebniszeile. (#452)
- Seit 26.09.2026: Die ValERI-Bewertung zeigt in Block 2 die Zahlungsreihen je Jahr und als Barwert samt
  Zahlungsstrombild, wählbar nach Stand und Szenario. (#454)
- Seit 26.09.2026: Block 4 der ValERI-Bewertung zeigt Spannenbild und Verlauf mit drei Szenarien. (#454)
- Seit 26.09.2026: „Woraus entsteht die Zahl?" zeigt je Bestandteil Barwert, Nominalsumme und die Differenz zur
  Referenz, dazu das Brückenbild zur Kapitalwertdifferenz, das wie das Zahlungsstrombild auch im Wortbericht
  steht. (#454)
- Seit 26.09.2026: „Was ist angenommen?" zeigt die Tafel „Was daraus im Lauf wird" und eine Fußzeile zur Herkunft
  der Annahmen. (#454)
- Seit 26.09.2026: Der Tabellenbericht führt die Parameter der Wirtschaftlichkeit je Szenario in einem eigenen Block
  mit benannten Zellen. (#455)
- Seit 26.09.2026: Die Mehrjahrestabellen des Tabellenberichts rechnen mit sichtbaren Formeln auf diesen Block. (#455)
- Seit 26.09.2026: Kapitalwert, Annuität, interner Zinsfuß und Amortisation stehen im Tabellenbericht als
  Formeln. (#455)
- Seit 26.09.2026: Bemessene Betriebskosten zeigen im Tabellenbericht Menge und Satz; der Betrag ist ihr
  Produkt. (#455)
- Seit 26.09.2026: Wort- und Tabellenbericht schließen mit der Checkliste nach DIN EN 17463, Anhang E; die
  Ergebnisseite zeigt sie über „Anhang-E-Checkliste…". (#455)
- Seit 26.09.2026: Die Betriebskostentabelle des Wort- und Tabellenberichts nennt für jede Position ihre
  Bemessungsart. (#460)
- Seit 26.09.2026: Positionen mit späterem Startjahr tragen dort „ab Jahr …" und lösen keinen Hinweis auf eine
  unvollständige Gliederung mehr aus. (#460)
- Seit 26.09.2026: Die Szenarien Günstig und Ungünstig können einen eigenen Betrachtungszeitraum, eine
  Mengenänderung, eigene Energieträgerpreise sowie eigene Einspeisevergütungen, DV-Entgelte und PPA-Preise führen.
  (#461, #462)
- Seit 26.09.2026: Bericht und Formelmappe nennen je Szenario Betrachtungszeitraum, Mengenänderung,
  Einspeisevergütungen und gepflegte Energieträgerpreise. (#461)
- Seit 26.09.2026: Die Szenariotafel im Dialog ‚Parameter' führt zusätzlich Betrachtungszeitraum und Mengenänderung
  je Szenario. (#462)
- Seit 26.09.2026: Arbeits-, Grund- und Leistungspreis der Energieträger, die Einspeisevergütungen sowie DV-Entgelt
  und PPA-Preis tragen einen ±-Knopf für ihre Werte je Szenario. (#462)
- Seit 26.09.2026: Unter der Annahmentafel, im Wort- und im Excelbericht steht statt des Hinweistexts der Ausweis
  ‚n von m Parametern szenariert' mit den gepflegten Größen. (#462)
- Seit 26.09.2026: Der Dialog Nutzungsdauern (AfA) zeigt je Technik die Sätze für Instandsetzung und Wartung in % der
  Investition je Jahr (VDI 2067 Blatt 1, Tabelle A2). (#463)
- Seit 26.09.2026: Betriebskostenpositionen ‚Instandhaltung …' und ‚Wartung …' mit ‚% der Investition' bekommen den Satz
  der Nutzungsdauertabelle über ‚Sätze vorbelegen…' oder die Übernahme einer Kostenvorlage; die Herkunft steht am
  Satz. (#463)
- Seit 26.09.2026: Die Speicherflotte rechnet den Restwert je Einheit linear aus ihrer Nutzungsdauer; ohne eigenes
  Ersatzintervall gilt die Nutzungsdauer der Nutzungsdauertabelle. (#463)
- Seit 26.09.2026: Die Nutzungsdauer in den Katalogen von Heizkessel und BHKW ist als Gerätedaten gekennzeichnet;
  maßgeblich ist die Nutzungsdauertabelle. (#463)
- Seit 26.09.2026: Die Anhang-E-Checkliste führt die Szenarioanalyse als erfüllt, sobald die Szenarien Günstig und
  Ungünstig gerechnet sind. (#474)
- Seit 26.09.2026: Die Ergebnisseite und der Dialog BHKW-Wirtschaftlichkeit nennen den Grund, wenn gespeicherte
  Ergebnisse nicht gelesen oder Eingaben nicht gespeichert werden konnten. (#474)
- Seit 26.09.2026: Eine neu angelegte Speichervariante übernimmt die Nutzungsdauer der Zeile Stromspeicher · Batterie
  aus der Nutzungsdauertabelle. (#474)
- Seit 26.09.2026: Die Formelmappe des Tabellenberichts rechnet Mehrjahrestabellen, Kennzahlen und Bandbreite für alle
  drei Szenarien mit sichtbaren Formeln auf den Parametersatz des jeweiligen Szenarios. (#477)
- Seit 26.09.2026: Im Parameterdialog der Wirtschaftlichkeit kann ein Risiko nach DIN EN 17463 als Zuschlag auf den
  Kalkulationszins oder als Abzug je Jahr angesetzt werden. (#478)
- Seit 26.09.2026: Die nicht monetären Wirkungen der Wirtschaftlichkeit werden als Liste mit Kategorie und Beurteilung
  nach Dauer und Wirkung (DIN EN 17463) erfasst und erscheinen im Word- und Excel-Bericht als Tabelle. (#479)
- Seit 26.09.2026: Betriebskostenpositionen lassen sich im Zeileneditor mit ‚Zahlung alle n Jahre' führen (z. B.
  Dichtheitsprüfung alle 2 Jahre); die Wirtschaftlichkeit rechnet sie nur in ihren Zahlungsjahren. (#484)
- Seit 26.09.2026: Der Dialog ‚BHKW-Wirtschaftlichkeit' zeigt unter der Unternehmensart den im Strompreis erfassten
  Stromsteueranteil und ob er zur gewählten Unternehmensart passt. (#492)
- Seit 26.09.2026: Ohne BHKW wird die Unternehmensart nach Stromsteuergesetz in den Wirtschaftlichkeits-Parametern
  (Gruppe Strom) gepflegt; darunter steht der im Strompreis erfasste Stromsteueranteil. (#498)
- Seit 26.09.2026: Die Investitionskosten der Wärmepumpe lassen sich auch je kW elektrisch bemessen; Bezugsgröße ist
  die elektrische Leistungsaufnahme am Normpunkt der Kennlinie. (#502)
- Seit 26.09.2026: Die Betriebskosten der Wärmepumpe werden nicht mehr je kWh bemessen; zur Wahl stehen fester
  Jahresbetrag, Prozentbemessungen und je kW. (#510)
- Seit 26.09.2026: Der Wärmebedarf der Gebäude wird stündlich nach VDI 6007 gerechnet; der
  Wärmebedarf eines Gebäudes zeigt die Kennzahlen des Gebäudemodells, den Vergleich der
  Rechenwege und den Verlauf der Raumtemperatur. (Gebäudesimulation G1 + G2)
- Seit 26.09.2026: Der Projektbericht nennt je Gebäude Rechenweg, Wärmebedarf und
  Spitzenwerte. (Gebäudesimulation, E30)
- Seit 26.09.2026: Gebäude lassen sich kühlen — der Kältebedarf wird nach VDI 6007 stündlich
  gerechnet und in Dialogen, Ergebnis und Bericht ausgewiesen. (Kühlung KU1)
- Seit 26.09.2026: Eine Wärmepumpe mit Kühlkennlinie kann auch kühlen; Kältedeckung und
  Kältestrom samt Kosten und Emissionen stehen in Ergebnis und Bericht. (Kühlung KU2)
- Seit 26.09.2026: Der Wärmepumpen-Import übernimmt keine Kühlkennlinien in Heizlage oder mit
  vertauschten Achsen und nennt sie im Leseprotokoll. (Kühlung KU2)
- Seit 26.09.2026: Die Gebäudeverwaltung bearbeitet Hülle, Wohnfläche und alle übrigen
  Gebäudedaten direkt im Stammblatt. (#465; Satz aus der Statuszeile, mit #476 hierher übernommen)
- Seit 26.09.2026: Im Gebäudedialog eines Projekts lässt sich ein Katalog-Gebäude, das
  ein Projekt verwendet oder zur Auslieferung gehört, nicht mehr löschen; der Dialog
  nennt den Grund. (#487)
- Seit 26.09.2026: Ein Speichern im Projektassistenten ohne inhaltliche Änderung lässt
  das Änderungsdatum und das Simulationsergebnis unberührt; gehören die Eingaben zu
  einem anderen als dem gewählten Projekt, speichert er nicht und weist darauf hin.
  (#490, #497)
- Seit 26.09.2026: Im Projektassistenten wählen Sie die Klimaregion aus derselben durchsuchbaren
  Liste wie auf der Übersicht, und die gewählte Region wird in das Projekt übernommen. (#527)
- Seit 26.09.2026: Ein Gebäude kann seinen Heizkreis rechnen — Heizkörper, Flächenheizung oder
  Konvektor mit Heizkurve, Raumregler und Sollwert-Zeitprogramm statt idealer Regelung; Vorlauf,
  Rücklauf und begrenzte Stunden stehen im Wärmebedarf und im Bericht. (Anlagenkopplung AK1)
- Seit 26.09.2026: Die Kühlung eines Gebäudes kann an die Anlage gekoppelt gerechnet werden —
  über eine Kühlübergabe (Kühldecke, Flächenkühlung oder Gebläsekonvektor) mit festem
  Kaltwasser-Vorlauf und einer einstellbaren Vorlaufgrenze. (Anlagenkopplung AK1, E37; Version
  1.2.0.4 nach Anwenderentscheid 25.09.2026, die vierte Welle ist vor dem Upload gepusht)
- Seit 26.09.2026: Gebäudedaten lassen sich aus gbXML- und IFC-Dateien in den Gebäudedialog
  übernehmen. (Gebäudesimulation G4)
- Seit 26.09.2026: Beginn und Ende der Nachtabsenkung lassen sich je Gebäude einstellen.
  (Gebäudesimulation G4, E43)
- Seit 26.09.2026: Die Baualtersklassen folgen der üblichen Einteilung nach Bauzeitraum (bis 1859 bis
  ab 2021) und ergeben sich aus dem Baujahr; ein eigenes Feld nennt den Energiestandard. (Gebäudesimulation, E47)
- Seit 26.09.2026: Baustoffe und Bauteilaufbauten mit Schichten haben eigene Verwaltungen unter
  Administration → Gebäude. (Gebäudesimulation G3)
- Seit 26.09.2026: Ein Gebäude im Projekt kann als Zone aus Bauteilen mit Schichtaufbau gerechnet
  werden. (Gebäudesimulation G3)
- Seit 26.09.2026: Ein importiertes Gebäude lässt sich als Zone mit seinen Bauteilen übernehmen; die
  Materialnamen der Datei werden dabei dem Baustoffkatalog zugeordnet. (Gebäudesimulation G4b, E44,
  E45, Namensabgleich)

Keinen eigenen Satz bekommt E35 (Grund- und Leistungspreis eines eigenen Zählers des Kältestroms):
Die Kühlung erscheint mit dieser Version zum ersten Mal, und der Satz zu KU2 nennt die Kosten des
Kältestroms schon — Regel 13.4, ein Thema, ein Eintrag.

Keinen eigenen Satz bekommen die Folgevorgaben des Gebäudeimports (die inneren Gewinne folgen einer
geänderten Nutzfläche, der Nachtsollwert einem gesenkten Tagsollwert): Sie sind Teil des
Gebäudeimports, der mit derselben Version erscheint — Regel 13.4, ein Thema, ein Eintrag.

Keinen Eintrag bekommen die Fehlerbehebungen aus #414 und
#415 (Farbwähler-Wechsel, Klimaregionenliste, Auswahlfeld) — Kleinigkeiten nach Regel 13.4.

*Vor der Veröffentlichung ersetzt:* Der Satz zu #446 „Bei einer Vorrichtung zur Abwärmeabfuhr zählen die
Vollbenutzungsstunden des KWKG-Kontingents aus dem KWK-Strom." ist gestrichen — er war noch nicht veröffentlicht —
und mit #452 durch den Satz zur erzeugten Arbeit ersetzt (Definition der Vollbenutzungsstunden durch den Anwender vom
23.09.2026). Ohne eigenen Satz bleiben aus #452 der ungerundete Satz der Speicherbewertung, Heizwert und Brennwert
1,0 des Brennstoffs „Sonstige" und der Status „abgekündigt" zweier Zeilen des Gesetzeskatalogs.

*Aus #454 ohne eigenen Satz:* die neu gefasste Hinweiszeile in Block 2 bis zum ersten Rechenlauf. Der Satz zur
Fußzeile gilt unverändert, wenn sie mit #455 in die Knopfreihe rückt.

*Aus #455 ohne eigenen Satz:* die Fußzeile in der Reihe der Knöpfe (der Satz aus #454 gilt), die Neuberechnung der
Mappe beim Öffnen und die Gegenprobe an der Fallstudie des Anhangs D der Norm — ein Prüffall ohne sichtbare
Änderung.

*Aus #460 ohne eigenen Satz:* die Nachweisfassung 9 (das Startjahr je Position reist mit dem Ergebnis) und der
berichtigte Kommentar zum Zahlungsstrombild im Code — beides ohne sichtbare Änderung.

*Vor der Veröffentlichung ersetzt (#462):* Der Satz zum Rechenkern aus #461 („Führt ein Projekt für die Szenarien …
gilt der Wert von Erwartet.") ist durch die zwei mit E9b veröffentlichten Sätze der Welle E9a ersetzt — die Werte je
Szenario und ihr Nachweis in Bericht und Formelmappe (ein Thema, ein Eintrag); dazu kommen die drei Sätze aus E9b
(Stichwörter `szenarien` und `wirtschaftlichkeit`). Aus den Sätzen zu #434 sind die Teile zum Hinweistext unter der
Annahmentafel und in den Berichten gestrichen — beide noch nicht veröffentlicht; an seiner Stelle steht mit #462 der
Ausweis (letzter Satz der Reihe). Ohne eigenen Satz bleiben aus #461 die Nachweiszeile je Szenario, die jetzt
Betrachtungszeitraum und Einspeisevergütung nennt, und die drei Zeilen mehr im Parameterblock der Formelmappe, aus
#462 das Warnzeichen bei einem Szenariopreis ohne Erwartet-Preis und die Zeilen zu Szenariowerten ohne Wirkung.

*Zu #463 (Stichwörter `nutzungsdauern` und `kosten`):* Der Satz zu den Betriebskostenpositionen folgt dem gebauten
Stand — die Sätze der Tabelle kommen über ‚Sätze vorbelegen…' oder die Übernahme einer Kostenvorlage in die Position,
die Tabelle rechnet nicht selbst; der Satz zu den Katalogen sagt ‚maßgeblich' wie der Kurztext am Feld. Ohne eigenen
Satz bleiben die Herkunftszeile in Herleitung und Formelmappe, der Wartungssatz für neue Kesseleinträge in %/a (die
Auslieferung führt keinen) und die Kennzeichnung des festen Restwerts einer Flotteneinheit als Gerätedaten.

*Zu #474 (Stichwörter `bericht`, `wirtschaftlichkeit` und `kosten`):* Drei Sätze — Punkt 9 der Checkliste, die Gründe
auf Ergebnisseite und im BHKW-Dialog und die Nutzungsdauer einer neuen Speichervariante. Ohne eigenen Satz bleiben die
zwei Hilfe-Anker (Zeileneditor und saisonale Leistungspreise der Seite Kosten), das Zurücksetzen der Tabellenvorsorge
vor jedem Lauf und die einmalige Meldung eines Datenbankfehlers — Kleinigkeiten nach Regel 13.4.

*Zu #477 (Stichwort `bericht`):* Ein Satz — die Formelmappe rechnet alle drei Szenarien. Er ergänzt die Sätze zu #455,
die weiter gelten; ohne eigenen Satz bleiben der Text „nicht eindeutig" beim mehrdeutigen Zinsfuß, die Schutzformeln
jenseits des Zeitraums eines Szenarios und der neue Text von Punkt 11 der Checkliste.

*Zu #478 (Stichwort `wirtschaftlichkeit`):* Ein Satz — das Risiko im Parameterdialog. Ohne eigenen Satz bleiben der
Ausweis (Deklaration, Annahmentafel, Punkt 6 der Checkliste, Bestandteil „Risikoabzug" der Gliederung) und die
Risikozeilen der Formelmappe; sie gehören zur selben Funktion.

*Zu #479 (Stichwort `wirtschaftlichkeit`):* Ein Satz — die Liste der nicht monetären Wirkungen samt Tabelle im
Bericht. Ohne eigenen Satz bleiben das lesbare Altfeld des Freitexts, die Punkte 2b und 3b der Anhang-E-Checkliste
(„erfüllt", sobald eine Wirkung beurteilt ist) und die Felder des Hilfe-Assistenten; sie gehören zur selben Funktion.

*Zu #484 (Stichwort `kosten`):* Ein Satz — das Feld „Zahlung alle n Jahre" im Zeileneditor samt seiner Wirkung in der
Wirtschaftlichkeit. Ohne eigenen Satz bleiben die Herleitung „alle n Jahre ab Jahr …" in der Betriebskostentabelle der
Berichte, die Hilfsspalte der Formelmappe, das Feld in den Kostenvorlagen und das Feld des Hilfe-Assistenten; sie
gehören zur selben Funktion.

*Zu #492 (Stichwort `wirtschaftlichkeit`):* Ein Satz — die Anzeige des erfassten Stromsteueranteils im Dialog
„BHKW-Wirtschaftlichkeit". Ohne eigenen Satz bleiben die Anzeige in der Überlagerung „Sätze und Herkunft" (dieselbe
Funktion) und die Wache der Stromsteuer-Rückfallebene gegen den Katalog (nicht sichtbar).

*Zu #498 (Stichwort `wirtschaftlichkeit`):* Ein Satz — die Unternehmensart ohne BHKW in den Wirtschaftlichkeits-Parametern
samt der Anzeige des erfassten Stromsteueranteils. Ohne eigenen Satz bleiben das Feld des Hilfe-Assistenten (dieselbe
Funktion) und die Wache zu Nr. 15 (nicht sichtbar; die Energieträgerverwaltung liest Bilanzjahr und Unternehmensart schon
je Öffnung).

*Zu #502 (Stichwort `kosten`):* Ein Satz — die Bemessung „je kW elektrisch“ bei den Investitionskosten der Wärmepumpe
samt ihrer Bezugsgröße. Ohne eigenen Satz bleiben die Herleitung am Betrag, die Summenform bei mehreren Wärmepumpen und
der Grund bei einem Gerät ohne Normpunkt (dieselbe Funktion); die Betriebskostenseite ist unverändert.

*Zu #510 (Stichwort `kosten`):* Ein Satz — die Betriebskosten der Wärmepumpe ohne Bemessung je kWh. Ohne eigenen Satz
bleiben der Vermerk „Altbestand“ an einer vorhandenen Position (dieselbe Funktion) und das Streichen des Altwerts
„je Stunde“ an der Wärmepumpe in der Quelle (eine Richtigstellung der Beschreibung, keine Funktionsänderung).

*Zurückgestellt gegenüber den Rohentwürfen:* der engere Klimadaten-Satz aus #404 und die
beiden Sätze aus #411 zu „Wärmeproduktion/Stromproduktion“ sowie zu den Bedarfs- und
Quellprofildialogen — sie beschreiben dieselbe Bedienung wie oben, nur an weniger Stellen.

**Die „14+1 Sätze" aus Protokoll 05/§ 7 (Statuszeilen #343–#367, E12‑Q2 entschieden 24.09.2026 nach
Empfehlung: a):** 14 der
15 Sätze decken sich inhaltlich mit den Statuszeilen #358–#366 unten oder mit bereits
veröffentlichten Sätzen; neun Themen hatten hier noch keinen eigenen Satz. Alle neun gegen die
Tabuwort-Regex aus `CLAUDE.md` geprüft: 0 Treffer.

- Seit 26.09.2026: Der Kostendialog zeigt unter jedem gerechneten Betrag die Herleitung und
  weist im Summenfuß der Investitionsseite bei einer Zuschussposition Investition brutto,
  Zuschuss und Anfangsinvestition getrennt aus. (#345)
- Seit 26.09.2026: Die Betriebsseite der Kostenverwaltung kennzeichnet Pflichtpositionen mit
  einem Schloss statt des Papierkorbs, zeigt eine Empfehlungszeile, den Stand des zugrunde
  liegenden Simulationslaufs, die Endenergie je Komponente und einen Warnhinweis bei doppelt
  gepflegter Hilfsenergie. (#347)
- Seit 26.09.2026: Ein Elektrokessel zählt zur elektrischen Welt, bekommt wie Wärmepumpe und
  Heizstab einen Stromträger zugeordnet und erscheint in der Kostenverwaltung mit seinem
  Stromeinsatz als eigene Zeile bei der Endenergie je Komponente. (#349, #353)
- Seit 26.09.2026: Die Betriebskosten der Photovoltaik lassen sich auch je kWp bemessen, der
  Summenfuß weist dazu die Kennzahl in Euro je kWp mit ihrer Herleitung aus, und der
  Vergütungsdialog warnt rechtzeitig vor der Ausschreibungs- und der Stromsteuer-Grenze. (#350)
- Seit 26.09.2026: Die Erlösrubrik und der BHKW-Wirtschaftlichkeitsdialog zeigen den
  angesetzten KWK-Satz mit seiner Herkunft, Vorschlag oder eigener Wert, und rechnen ihn
  einheitlich auf vier Nachkommastellen. (#352)
- Seit 26.09.2026: Die Betriebskosten lassen sich bei Wärmepumpe, Heizkessel, Photovoltaik,
  Solarthermie, Stromspeicher, Pufferspeicher und Blockheizkraftwerk auch je Kilowatt Leistung
  bemessen. (#356)
- Seit 26.09.2026: Die Investitionsseite der Kostenverwaltung bietet einen Knopf, der leere
  Nutzungsdauern aus der AfA-Tabelle vorbelegt, und zeigt darunter die Tafel „Ersatz und
  Restwert" mit Ersatzbeschaffungen und Restwert je Position. (#357)
- Seit 26.09.2026: Die Kostenvorlage „Standard" bemisst die Hilfsenergiekosten von
  Blockheizkraftwerk, Heizkessel und Wärmepumpe nach dem Anteil am Endenergiebedarf statt an
  den Endenergiekosten. (#365)
- Seit 26.09.2026: Die Rasterkarte der Stromspeicher-Größen-Sicht zeigt den Zusatz zum
  Feinraster-Ergebnis vollständig unter dem Hinweistext. (#361)

*Ohne eigenen Satz (Kleinigkeit nach Regel 13.4, wie schon in 05/§ 7 vorgeschlagen):* #346
(KWKG-Pauschale, Jahr 0 — seltener Randfall Kleinst-BHKW).

Die restlichen fünf der 15 Sätze (#358, #359, #360, #363, #364) stehen inhaltsgleich unten unter
den Statuszeilen #358–#403; ein eigener Eintrag entfiele als Dopplung nach Regel 13.4.

- Seit 26.09.2026: Die Wirtschaftlichkeit vergleicht wahlweise alle Varianten gegen ein
  wählbares Referenzprojekt oder zwei Stände gegeneinander. (#358)
- Seit 26.09.2026: Eine Variante übernimmt die Photovoltaik-Vergütung des Stammprojekts oder
  führt eigene Werte. (#359)
- Seit 26.09.2026: Beim Weitergeben einer Variante reist die Photovoltaik-Vergütung des
  Stammprojekts mit. (#360)
- Seit 26.09.2026: Die Übernahme ins Projekt zeigt unter „Aus Vorlage/Variante“ die Auswahl
  der Kostenverwaltung aus der Administration samt den Positionen der gewählten Variante.
  (#363)
- Seit 26.09.2026: Die Betriebskosten weisen die Bezugsgrößen aus dem Simulationslauf auch
  für gespeicherte Läufe aus, und eine Position ohne Bezugsgröße nennt den Grund samt Abhilfe.
  (#364)
- Seit 26.09.2026: Hilfsenergiekosten nach „% des Endenergiebedarfs“ werden bei Anlagen, die
  selbst Strom beziehen, mit dem Arbeitspreis ihres eigenen Energieträgers bewertet; weicht
  die Bemessung einer Kostenposition von der der Vorlage „Standard“ ab, zeigt der Kostendialog
  den Vorlagenwert mit der Möglichkeit, ihn zu übernehmen. (#366)
- Seit 26.09.2026: Klimaregionen lassen sich aus den PVGIS-Daten oder aus einem
  DWD-Testreferenzjahr anlegen — aus einer eigenen Datei oder aus den offenen Regionaldaten.
  (#367)
- Seit 26.09.2026: Beim Einlesen einer Testreferenzjahr-Datei trägt das Programm den Standort
  aus dem Dateikopf ein; er bleibt änderbar. (#368)
- Seit 26.09.2026: Die Regionaldaten zeigen auf Knopfdruck, welche Region ein Standort trifft,
  bevor eingelesen wird; zu jeder Stunde werden zusätzlich Gegenstrahlung, Luftfeuchte und —
  soweit die Quelle sie führt — der Bedeckungsgrad gespeichert. (#369)
- Seit 26.09.2026: Die Klimadaten-Liste hat Suche, Filter und Sortierung und zeigt Quelle,
  Standort und Importdatum; auf der Übersicht ist die Klimawahl durchsuchbar und nennt
  darunter, woher die verwendeten Klimadaten stammen. (#371)
- Seit 26.09.2026: Das Menü Administration ist neu geordnet: Gebäude und Klimadaten stehen
  vorn, die gesetzlichen Parameter unter Kosten, die Dublettenprüfung unter Daten & Import und
  die Einstellungen am Ende. (#372)
- Seit 26.09.2026: Die Lizenz wird nur noch unter Hilfe → Lizenz verwaltet; der Dialog nennt
  vor der Aktivierung, welche Angaben an den Lizenzserver übertragen werden. (#372)
- Seit 26.09.2026: Die gesetzlichen Parameter lassen sich wie die Gerätekataloge durchsuchen,
  filtern und sortieren. (#372)
- Seit 26.09.2026: Projekte lassen sich zu mehreren auf einmal ausgeben — eine gewählte
  Variante nimmt ihr Stammprojekt mit —, und mehrere Paketdateien lassen sich in einem Zug
  einlesen. (#373)
- Seit 26.09.2026: Die Klimadaten-Liste und die Übersicht nennen zur Quelle auch das
  Bezugsjahr und das Szenario der verwendeten Klimadaten. (#382)
- Seit 26.09.2026: Der Gesamtwirkungsgrad eines BHKW wird im Katalog einheitlich als Faktor
  geführt; Gasverbrauch, Emissionen und Brennstoffkosten der betroffenen Module fällt damit
  richtig aus. (#383)
- Seit 26.09.2026: Der BHKW-Katalog führt elektrischen und thermischen Wirkungsgrad; der
  Gesamtwirkungsgrad ergibt sich daraus. (#392)
- Seit 26.09.2026: Die Gerätekataloge haben eine eigene Hilfeseite; die Hilfeknöpfe der
  Verwaltungen führen dorthin. (#509)
- Seit 26.09.2026: Die Farben der Diagramme lassen sich in den Einstellungen je Größe ändern;
  der Bericht nimmt dieselben Farben. (#403)
- Seit 26.09.2026: Brauchwasser-Zapfprofile lassen sich je Nutzungszone erzeugen und als
  Brauchwasserbedarf rechnen. (#443)
- Seit 26.09.2026: Die Speicherauslegung für Trinkwarmwasser zeigt Summenlinie und Normvergleich
  nebeneinander und empfiehlt den Punkt der Summenlinie. (#451)
- Seit 26.09.2026: Das Brauchwasser-Zapfprofil rechnet die Jahresreihe in der Stufe Experte wahlweise
  stochastisch mit Seed und Realisierungen und zeigt die Konsistenzprobe. (#453)
- Seit 26.09.2026: Die Auslegung Brauchwasser rechnet auf Wunsch stochastisch und zeigt das Perzentil
  P95 oder P99 mit Streuband und Gleichzeitigkeit. (#453)
- Seit 26.09.2026: Das Ecodesign-Zapfprofil L steht als Bedarfstag der Auslegung zur Verfügung. (#453)
- Seit 26.09.2026: In der Stufe Erweitert des Zapfprofils stehen Wohnungstabelle, Kalender und Ferien,
  Jahresmesswert, Schätzhilfen und Warnliste; die Stufe Experte trägt die Fachwerte und den
  Auslastungsgang. (#464)
- Seit 26.09.2026: Tagesgang und Zapfkategorien einer Nutzungsart lassen sich in der Stufe Experte
  bearbeiten; die Auslegung speichert Erzeugerart, Werkstoff und die Eingaben des
  Verfahrensvergleichs. (#464)
- Seit 26.09.2026: Der Katalog der Brauchwasser-Nutzungsarten lässt sich unter Administration →
  Brauchwasser pflegen und aus einem Katalogpaket importieren. (#464)
- Seit 26.09.2026: Der Jahresgang eines Brauchwasser-Zapfprofils kann über Typtage nach VDI 4655
  laufen; die Werte der Richtlinie spielt der lizenzierte Anwender aus einem eigenen Paket ein. (#486)
- Seit 26.09.2026: Gemessene Brauchwasser-Reihen lassen sich je Projekt einspielen, mit der gerechneten
  Jahresreihe vergleichen und zur Kalibrierung des Zapfprofils nutzen. (#495)
- Seit 26.09.2026: Der Katalog der Brauchwasser-Nutzungsarten enthält fünf aus VDI 6002 abgeleitete
  Nutzungsarten mit Herkunftsvermerk. (#504)
- Seit 26.09.2026: Für die Nichtwohn-Nutzungsarten nach DIN EN 12831-3 Beiblatt A100 liegt im
  Programmordner eine Paketvorlage zum Ausfüllen und Einspielen bei. (#504)
- Seit 26.09.2026: Der Vergleich einer Messreihe zeigt die Streuung der Realisierungsspitzen, wenn die
  Jahresreihe stochastisch gerechnet ist. (#508)
- Seit 26.09.2026: Das Brauchwasser-Zapfprofil zeigt ab der Stufe Erweitert eine Karte „Herkunft", die je
  Wert der Rechnung nennt, woher er kommt. (#516)
- Seit 26.09.2026: Der Katalogimport für Brauchwasser nimmt auch Bedarfstage und Parameter an und
  ersetzt vorhandene Werte mit Hinweis. (#517)
- Seit 26.09.2026: Die Zapfungen eines selbst konstruierten Bedarfstags bleiben mit der Auslegung
  gespeichert und lassen sich wieder bearbeiten. (#522)
- Seit 26.09.2026: Der Katalog der Brauchwasser-Nutzungsarten steht auch auf dem iPad zur Verfügung und
  nimmt Katalogpakete als ZIP an. (#524)
- Seit 26.09.2026: Die Auslegung Brauchwasser bietet alle Ecodesign-Zapfprofile von XXS bis 4XL als
  Bedarfstag an. (#537)
- Seit 26.09.2026: Auf dem iPad öffnet der Knopf „Kataloge…“ der Projektliste die freigegebenen
  Katalogverwaltungen. (#540)

*Mit E12 ergänzt:* Statuszeile #377 zählte zu dieser Version „die Logbuch-Sätze 1–14 (+ #361)“;
für den Rasterfußzeilen-Befund aus #361 (Fußzeile der Rasterkarte bei hoher Zeilenschrift nicht
mehr abgeschnitten) lag in der Statusdatei kein ausformulierter Satz vor — der Satz oben
(„Zusatz zum Feinraster-Ergebnis … vollständig unter dem Hinweistext") übernimmt den in 05/§ 7
vorgeschlagenen Wortlaut, damit ist die frühere Rückfrage an den Anwender erledigt.
*Zurückgestellt:* der engere Klimadiagramm-Satz aus #396 — er geht in der Sache im Satz zu
#404/#413 (oben) auf.

## 3 Offene Punkte

**Seiten ohne Wiki-Quelle.** Für folgende Bedienung gibt es keine Repo-Quelle unter
`Projekte/Wiki/` und damit keine Bedienungsseite (Statuszeilen #383, #392, #411, #413):

- Wärmepumpe (Kennlinien), Erdreichquelle, Lastspitzenkappung (eigene Maske) — je ein
  fachlicher Dialog ohne eigene Seite;
- Bedarfsergebnis, Quellprofil, Bedarfstyp, Gebäudetyp, Wärmebedarf extern, Stromganglinie,
  Gebäude — die „sieben Dialoge“ aus Statuszeile #411, deren Bedienung unbeschrieben ist
  (Wiki-Runde nötig: eigene Seiten oder Verweis auf „Die Diagramme bedienen“ der Seite
  Simulationsergebnisse); die Seite „Gebäude“ hat mit #476 eine Repo-Quelle (Abschnitt 1) —
  offen bleibt dort nur die Diagrammbedienung des Wärmebedarfs eines Gebäudes;
- Gerätekataloge — die Frage der Statuszeile #383, ob eine eigene Seite „Programm
  Dokumentation/Gerätekataloge“ entsteht, ist **entschieden 25.09.2026: anlegen** — Neuanlage
  beim Upload (Abschnitt 1); die Hilfeknöpfe der acht Verwaltungen zeigen mit #509 auf die Seite.

**Was bis zum 26.09.2026 noch dazukommt.**

- Die drei Gerätemeldungen (Statuszeile #415: Klimaregionenliste, Auswahlfeld, sortiert)
  berühren keine Bedienungsseite; der Schalter „sortiert" steht als Logbuch-Satz oben.
- Die Wirtschaftlichkeits-Umsetzung war mit Statuszeile #405 zurückgestellt und ist am
  22.09.2026 wieder aufgenommen (E4 #432, E5 #434, E6 #436, E7 Teil a #437, E7 Teil b #439, E7 Teil c1
  #440, E7 Teil c2 #446, E7 Teil c3 #452, E8 Teil a #454, E8 Teil b #455, E8c #460, E9 Teil a #461, E9 Teil b #462, E10 #463, E13 #474, E14 #477, E15 #478, E17 #479, E16 #484, E18 #492, E19 #498, E20 #502, E23 #510). Die Repo-Quelle
  `Projekte/Wiki/Programm Dokumentation - Wirtschaftlichkeit.wiki` ist mit den Papieren zu #436 auf
  die Ergebnisseite nach E5 und E6 nachgezogen (Umschalter, vier Abschnitte, Empfehlungskarten,
  Bandbreite mit Spannenbild, Verlauf mit drei Szenarien, „Bericht erzeugen", „Verlauf nach Excel…",
  Szenarien „Ungünstig"/„Günstig"; Tabuwort-Regex 0 Treffer), mit den Papieren zu #437 um die
  brennwertbezogene CO₂-Prüfung der Stromsteuerbefreiung und die vermiedenen Stromkosten ohne jede
  Eigenerzeugung (Tabuwort-Regex 0 Treffer), mit den Papieren zu #439 um den Wegfall von „Strombezug…",
  die Tarifstruktur im Rollenmodell (der Anker `strombezug` bleibt, weil die Hilfe-Zuordnung des Dialogs
  dorthin zeigt) und die Leistungspreis-Staffel in den Energiekosten, mit den Papieren zu #440 um den
  KWK-Strom nach § 2 Nr. 16 mit Fall 2 und Stromkennzahl in der Überlagerung „Sätze und Herkunft…" (Anker
  `kwk-abwaermeabfuhr`), die Frist zur Inbetriebnahme bis 31.12.2030 (Anker `kwk-frist`) und die
  Kohärenzzeilen „Anlagenart fehlt" und „Stromkennzahl fehlt" (Tabuwort-Regex 0 Treffer), mit den Papieren
  zu #446 um die ganze Überlagerung „Sätze und Herkunft…" (neuer Anker `kwk-saetze-herkunft`), die
  Vollbenutzungsstunden aus dem KWK-Strom im Fall 2 und den Rundungsgrund, die gesperrte Mischlage § 53/§ 53a
  neben § 54, § 51a mit der Einspeisevergütung und die Modultafel mit den Spalten zu Fall 2 (Tabuwort-Regex
  0 Treffer), mit den Papieren zu #452 um die Vollbenutzungsstunden nach der erzeugten Arbeit in beiden Fällen
  (Anker `kwk-abwaermeabfuhr` berichtigt — der Satz zum KWK-Strom als Zählgröße ist ersetzt), die Wahlen der
  Überlagerung als Zeilen mit ihrer Wirkung und Satz und Betrag je Energiesteuerentlastung (Anker
  `kwk-saetze-herkunft`) und die Warnung bei einer nicht ausführbaren Prüfung oder Rechenstufe (Anker `kohaerenz`)
  (Tabuwort-Regex 0 Treffer), mit den Papieren zu #454 um die fünf Blöcke der ValERI-Bewertung — Block 2 mit den
  Zahlungsreihen und dem Zahlungsstrombild (neuer Anker `zahlungsreihen`), Block 4 mit Spannenbild und Verlauf —,
  die Gliederung des Kapitalwerts mit Nominalsumme und Differenzspalte, das Brückenbild, die Tafel „Was daraus im
  Lauf wird", die Fußzeile (neue Anker `gliederung`, `bruecke`, `laufwirkung`, `szenariofuss`) und die zwei Bilder im
  Word-Bericht (Anker `bericht`) (Tabuwort-Regex 0 Treffer), mit den Papieren zu #455 um die Excel-Arbeitsmappe mit
  Formeln — Parameterblock mit benannten Zellen, Mehrjahrestabellen, Kennzahlen, Betriebskosten Menge × Satz,
  Neuberechnung beim Öffnen (Anker `bericht-excel`, neuer Anker `formelmappe`) — und die Anhang-E-Checkliste auf der Ergebnisseite, als
  Abschlussseite des Word-Berichts und als letztes Blatt der Mappe (neuer Anker `checkliste`; `darstellung`, `valeri`,
  `szenariofuss`, `bericht`) (Tabuwort-Regex 0 Treffer), mit den Papieren zu #460 um die Betriebskostentabelle der
  Berichte — Bemessungsart je Position wie in der Kostenverwaltung, „ab Jahr …" bei späterem Startjahr, der Hinweis auf
  eine unvollständige Gliederung nur bei einer echten Lücke (neuer Anker `bericht-betriebskosten`) (Tabuwort-Regex
  0 Treffer, 66 Anker eindeutig), mit den Papieren zu #461 um die weiteren Werte je Szenario — Betrachtungszeitraum,
  Mengenänderung, Energieträgerpreise und Erlössätze, leer wie Erwartet, ohne Eingabefeld in den Dialogen (neuer Anker
  `szenariowerte`) — und um Szenariozeile, Annahmentafel, Hinweistext, Verlauf, Annahmenzeilen und Parameterblock mit
  Zeitraum und Einspeisevergütung (Tabuwort-Regex 0 Treffer, 67 Anker eindeutig), mit den Papieren zu #462 um die
  Pflege in den Dialogen — Zeilen 8 und 9 der Szenariotabelle, „Vorgaben" für achtzehn Felder, der Knopf ± an
  Trägerkarte, Einspeisevergütungen, DV-Entgelt und PPA-Preis, das Fenster „Szenariowerte" (Anker `szenariowerte`,
  `szenarien`, `einspeiseverguetung`, `bhkw-wirtschaftlichkeit`, `pv-verguetung`) — und um die Szenarioabdeckung
  „n von m Parametern szenariert" an der Stelle des entfallenen Hinweistexts (der Anker `szenariohinweis` heißt jetzt
  `szenarioabdeckung`, er war noch nicht veröffentlicht; `valeri`, `checkliste`, `bericht`, `bericht-excel`)
  (Tabuwort-Regex 0 Treffer, 67 Anker eindeutig, alle internen Verweise treffen); die Repo-Quelle `Projekte/Wiki/Programm Dokumentation - Kosten.wiki` ist mit #439 um den Punkt
  „Leistungspreis-Staffel" (Anker `staffel`) und mit #446 um den Punkt „Ersatzbeschaffung und Restwert je
  Position" (Anker `ersatz-restwert-kennzeichen`), die gespeicherte Preisbasis, Nm³ und kWh im
  Brennstoffkatalog und die Prozentarten der Brennstoff- und Stromkosten aus dem Lauf ergänzt
  (Tabuwort-Regex 0 Treffer in beiden Quellen), mit den Papieren zu #463 um die Knöpfe ± der Trägerkarte (aus #462),
  die Sätze für Instandsetzung und Wartung im Dialog Nutzungsdauern (AfA), den Knopf „Sätze vorbelegen…" der
  Betriebskostenseite mit der Herkunft des Satzes und Ersatz und Restwert der Einheiten einer Speicherflotte (neue Anker
  `nutzungsdauer-saetze`, `saetze-vorbelegen`, `flotte-restwert`; Tabuwort-Regex 0 Treffer); die Repo-Quelle
  `Projekte/Wiki/Programm Dokumentation - Gerätekataloge.wiki` nennt mit #463 die Nutzungsdauer von Heizkessel und BHKW
  als Gerätedaten (Tabuwort-Regex 0 Treffer); mit den Papieren zu #474 nennt die Quelle Wirtschaftlichkeit Punkt 9 der
  Checkliste mit beiden Szenarien (Anker `checkliste`) und die Gründe in Statuszeile und BHKW-Dialog (Anker
  `nicht-berechnet`, `bhkw-wirtschaftlichkeit`), die Quelle Kosten die Nutzungsdauer einer neuen Speichervariante (Anker
  `nutzungsdauern`) und die Hilfe von Zeileneditor und saisonalen Leistungspreisen (Anker `ersatz-restwert-kennzeichen`,
  `preiswirkung`) — kein neuer Anker, Tabuwort-Regex 0 Treffer; mit den Papieren zu #477 beschreibt die Quelle
  Wirtschaftlichkeit die Formelmappe je Szenario — Tabellen, Kennzahlen, Zinsfuß und Bandbreite für alle drei
  Szenarien, die Schutzformeln jenseits des Zeitraums und der Zinsfuß „nicht eindeutig" (Anker `formelmappe`; kein neuer
  Anker, Tabuwort-Regex 0 Treffer); mit den Papieren zu #478 beschreibt die Quelle Wirtschaftlichkeit das Risiko nach
  DIN EN 17463 — Art, Felder, Wirkung von Zinszuschlag und Zahlungsstromabzug, Ausweis, R_loss als Betrag in € — und
  nennt es in den Deklarationen (Anker `risiko` aus dem Hilfeknopf der Gruppe, `nicht-monetaer`; 68 Anker eindeutig,
  Tabuwort-Regex 0 Treffer); mit den Papieren zu #479 beschreibt die Quelle Wirtschaftlichkeit die Liste der nicht
  monetären Wirkungen — Kategorie, Beschreibung, Dauer, die drei Wirkungen, die Beurteilung Dauer × stärkste Wirkung,
  das lesbare Altfeld —, die Tabelle in Word- und Excel-Bericht und die Punkte 2b und 3b der Checkliste mit
  „erfüllt"/„teilweise"/„offen" (Anker `nicht-monetaer`, das Ziel des Hilfeknopfs der Liste, und `checkliste`; kein neuer
  Anker, 68 eindeutig, Tabuwort-Regex 0 Treffer); mit den Papieren zu #484 beschreibt die Quelle Kosten das Feld „Zahlung
  alle n Jahre" im Zeileneditor der Betriebsseite — Werte, Zahlungsfolge ab dem Startjahr, Zeile unter dem Feld,
  Wirkung auf die Wirtschaftlichkeit, Kostenvorlagen und Übernahme (neuer Anker `zahlung-alle-n-jahre`) — und die
  Quelle Wirtschaftlichkeit die Positionen „alle n Jahre" in der Betriebskostentabelle und in der Formelmappe (Anker
  `bericht-betriebskosten`, `formelmappe`; kein neuer Anker, 68 eindeutig; Tabuwort-Regex 0 Treffer in beiden
  Quellen); mit den Papieren zu #492 beschreibt die Quelle Wirtschaftlichkeit im Dialog „BHKW-Wirtschaftlichkeit" den
  erfassten Stromsteueranteil unter der Unternehmensart — Wert, aktiv oder abgeschaltet, Satzabgleich, Kohärenzzeile
  ohne Sperre, Pflege nur in „Strompreis Details", nur mit BHKW erreichbar (neuer Punkt unter dem Anker
  `bhkw-wirtschaftlichkeit`, dazu ein Halbsatz bei `kwk-saetze-herkunft`; kein neuer Anker, 68 eindeutig;
  Tabuwort-Regex 0 Treffer); mit den Papieren zu #498 beschreibt die Quelle Wirtschaftlichkeit im Parameterdialog,
  Abschnitt „Strom“, die Unternehmensart für Vergleichsgruppen ohne BHKW — die drei Wahlen, § 9b auf den Netzbezug, die
  zwei Zeilen des erfassten Stromsteueranteils, das Bilanzjahr aus dem Abschnitt „Brennstoff“ und ohne ihn die Sätze
  von 2026, mit BHKW der Verweis (neuer Punkt „Unternehmensart“ unter dem Anker `parameter`, dazu ein Halbsatz bei
  `bhkw-wirtschaftlichkeit`; kein neuer Anker, 68 eindeutig; Tabuwort-Regex 0 Treffer); mit den Papieren zu #502 beschreibt
  die Quelle Kosten unter dem Anker `bemessung` die Bemessung „je kW elektrisch“ an der Wärmepumpe — nur bei den
  Investitionskosten, Heizleistung ÷ COP am Normpunkt der Kennlinie (A2/W35, B0/W35, W10/W35), Interpolation ohne
  Extrapolation, neutrales Beispiel 12,00 kW ÷ COP 3,00 = 4,00 kW, auf der Betriebskostenseite nicht an der Wärmepumpe
  (Tafelzeile, Absatz, Halbsatz; kein neuer Anker, 50 eindeutig; Tabuwort-Regex 0 Treffer); mit den Papieren zu #510
  nennt die Quelle Kosten unter dem Anker `laufgroessen` die Wärmepumpe nicht mehr bei „je kWh thermisch“ und „je kWh
  elektrisch“ und „je Stunde“ nur noch am BHKW (an der Wärmepumpe ein Altwert, den die Auswahl nicht anbietet), dazu
  den Satz, dass die Betriebskosten der Wärmepumpe nicht je kWh bemessen werden und eine vorhandene Position mit dem
  Vermerk „Altbestand“ weiterrechnet (kein neuer Anker, 50 eindeutig; Tabuwort-Regex 0 Treffer); die Logbuch-Sätze zu #432, #434, #436, #437, #439,
  #440, #446, #452, #454, #455, #460, #461, #462, #463, #474, #477, #478, #479, #484, #492, #498, #502 und #510 stehen oben (der Satz zu den Vollbenutzungsstunden aus #446 ist mit #452 ersetzt;
  der Satz zum Rechenkern aus #461 ist mit #462 durch die zwei Sätze der Welle E9a ersetzt, dazu drei Sätze aus E9b;
  aus zwei Sätzen zu #434 ist der Hinweistext gestrichen). Der neue Leereintrag „(bitte wählen)" der Anlagenart im BHKW-Dialog (#437) und die
  Kohärenzzeile „Anlagenart fehlt" (#440) sind Kleinigkeiten ohne eigenen Satz; ebenso ohne eigenen Satz
  bleiben aus #446 der Rundungsgrund winziger Kürzungen und der Hinweis bei fehlender Preisbasis-Spalte.
- Der KI-Assistent bekommt die Masken mit Einstellwerten in sechs Wellen (Statuszeile #416,
  KI‑D‑Q5); je Welle wächst die Maskenliste der Seite Hilfe-Assistent, und ein Logbuch-Satz
  kommt dazu.
- DG‑E5 (Statuszeile #418) ist gemergt: Die Seite Simulationsergebnisse sagt jetzt „Jede Kurve
  und jede Fläche trägt ein Farbfeld", der Logbuch-Satz steht oben.
- Ältere, mit „Version offen“ oder „Version 1.2.0.1“/„1.2.0.2“ vorbereitete Logbuch-Sätze aus
  Statuszeilen vor #358 sind für dieses Papier nicht erneut geprüft; vor dem Hochladen klären,
  ob sie in einer früheren Runde schon veröffentlicht wurden oder noch offen sind.
- Die Berichtsvorlagen (BV-E1, #512) bringen die neue Seite „Berichtsvorlagen“ (Neuanlage, Abschnitt 1) und einen Satz
  im Abschnitt „Bericht“ der Seite Wirtschaftlichkeit (Anker `bericht-vorlage`); bis zum Upload führen die Hilfeknöpfe
  von Prüfliste, Platzhalterkatalog und Einstellungen › Bericht ins Leere. Mit BV-E2 (#520) wächst die Seite um Kapitel,
  Häkchen und Logo (neue Anker `kapitel`, `haekchen`, `logo`; die Anker der Hilfeschlüssel bleiben). Die Logbuch-Sätze
  beider Etappen stehen in Abschnitt 2, die Version ist beim Anwender zu erfragen.

## 4 Ablauf des Uploads

Kein Skript und kein Werkzeug im Repository lädt eine Wiki-Seite hoch: Das Hochladen ist eine
manuelle Handlung der Orchestrierung in der MediaWiki-Oberfläche von `wiki.epos-plan.de`, nicht
eines Agenten (Regel 5 des Abschnitts „Bedienungsseiten mit Repo-Quelle unter `Projekte/Wiki/`“
im Hilfesystem-Konzept). Ablauf in Stichworten; die Freigabe (E12‑Q1) liegt seit dem 24.09.2026
vor, für den Termin 26.09.2026:

1. Termin und Versionsnummer beim Anwender bestätigen (E12‑Q1, E12‑Q3).
2. Je Seite dieser Tafel (Abschnitt 1) den Live-Stand lesen (`action=raw`) und mit der
   Repo-Quelle vergleichen; ist der Live-Stand neuer, zuerst ihn in die Repo-Quelle übernehmen
   und erst danach ergänzen (Regel 3 desselben Konzeptabschnitts) — für Photovoltaik und
   Varianten (neu in der Liste) sowie für die drei bereits geführten Seiten mit reiner
   Tabuwort-Bereinigung genügt der einfache Ersatz.
3. Seite im Bearbeitungsformular öffnen (bei einer neuen Seite: `Spezial:Importieren`) und
   durch den vollständigen Text der Repo-Quelle ersetzen; die Zusammenfassungszeile nennt die
   Statuszeile (hier #470; für Wirtschaftlichkeit und Kosten dazu #474, für Wirtschaftlichkeit auch #477, #478, #479, #484, #492, #498 und #512, für Kosten auch #484, #502 und #510; für die neue Seite Berichtsvorlagen #512 und #520).
4. Nach dem Speichern über `action=raw` und `action=parse` zurücklesen: byte-gleich zur
   Repo-Quelle, keine Parse-Fehler, Kategorien unverändert (Muster: „Dokumentationspflege
   Speicherauslegung“, Hilfesystem-Konzept).
5. Für alle Seiten der Tafel wiederholen, danach die gesammelten Logbuch-Sätze aus Abschnitt 2
   (eine Version, s. o.) auf der Live-Seite „Update-Logbuch“ ergänzen.
6. In der Statusdatei die ausstehenden Seiten als hochgeladen vermerken und im
   Hilfesystem-Konzept die neuen Revisionen nachtragen (Regel: Konzept Hilfesystem 13.3).

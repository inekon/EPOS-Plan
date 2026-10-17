# Prüfliste Sichtprüfung Windows — Übersicht Berichte & Kosten

Zweck: Der Anwender geht mit dem Windows-Build die Oberfläche durch, die die Sitzung „Dialoge und Korrekturen (Übersicht Berichte & Kosten)“ gebaut hat und die noch nicht am Bildschirm abgenommen ist.

## Voraussetzungen und Rückmeldeweg

- Windows-Build `EPOS_Plan.exe` mit gültiger Lizenz.
- Datenbank mit einem Beispielprojekt mit Kälte und Kältebedarfsprofilen (Projekt 1072 oder 1055 der Testdatenbank), möglichst mit mindestens einer Variante und einem gerechneten Ergebnis.
- Rückmeldung: Ergebnis in die Spalte „Befund“ eintragen; bei Abweichung Screenshot beilegen und die Liste an die Sitzung zurückgeben. Die Nummern in Klammern sind Zeilen der Statusdatei.

## 1 Übersicht Berichte & Kosten

Ort: Menü „Projekt“, „Varianten und Bericht…“ (#1033, #1067–#1072).

| Nr | Ort | Schritt | Erwartung | Befund |
|---|---|---|---|---|
| 1.1 | Übersicht, Komponentenvergleich | Vergleichstabelle je Gewerk öffnen | Tabelle je Gewerk mit Kennwerten (kW, l, kWh, kWp, m²), Kältemaschine als eigenes Gewerk | |
| 1.2 | Komponentenvergleich | Schalter „nur Unterschiede“ umlegen | Nur Zeilen mit Abweichung zum Stamm bleiben, Abweichungen sind markiert | |
| 1.3 | Kopf und Fußzeile | Kopf und Fußzeile lesen | Kopf in drei Teilen, Spaltenkopf mit Simulationsstand, Fußzeile mit richtiger Einzahl/Mehrzahl | |
| 1.4 | Liste „Detail-Unterschiede für Variante“ | Liste aufklappen | Nur Varianten, Vorgabe „— keine (Komponentenvergleich) —“; ohne Wahl erscheint der Komponentenvergleich | |
| 1.5 | „Variante verwalten“ | Anlegen, Umbenennen, Löschen nacheinander ausführen | Drei Dialoge (Anlegen aus Stammprojekt oder Variante, Umbenennen, Löschen mit Variantenwahl); Umbenennen erzeugt keine Kopie | |
| 1.6 | Abschnitt „Ergebnisse — Energie“ | Abschnitt öffnen | Nur Energie, CO₂ ist zugeklappt, keine Kosten | |
| 1.7 | „Anzeige anpassen…“ | Anzeige ändern, Projekt wechseln | Einstellung gilt je Benutzer für alle Projekte | |
| 1.8 | Ergebnisse | Schalter „Tabelle \| Grafik“ umlegen | Balken je Version, je Gruppe und Einheit ein Diagramm, Abweichung als Randstrich mit Wert und Prozent; Wahl bleibt gemerkt | |

## 2 Diagramme samt PV-Schalter

Ort: Reiter der Simulationsergebnisse (#1073, #1074, #1138, #1139).

| Nr | Ort | Schritt | Erwartung | Befund |
|---|---|---|---|---|
| 2.1 | Jahresdiagramm | Achswahl „Datum \| Stunde im Jahr“ auf „Datum“ stellen | Achse mit `TT.MM.`, im Ausschnitt Tage bzw. `TT.MM. hh:00` | |
| 2.2 | Jahresdiagramm | Achswahl auf „Stunde im Jahr“ stellen | Stundenzahl im gezeigten Ausschnitt mit runden Schritten | |
| 2.3 | Mehrere Jahresdiagramme | Achswahl an zwei weiteren Diagrammen prüfen | Dieselbe Achswahl gilt an allen Jahresdiagrammen | |
| 2.4 | PV-Reiter | Schalter „PV aufteilen“ einschalten | Stapel aus Eigenverbrauch, Einspeisung und Abregelung; ausgeschaltet bleibt das Bild wie zuvor | |
| 2.5 | Diagramme mit überdeckenden Reihen | Diagramm ansehen | Überdeckende Reihen sind lesbar aufgelöst | |
| 2.6 | Strom | Zuschaltbare Reihen einzeln wählen (Netzbezug/Einspeisung, WP/Heizstab, Peak-Shaving Leistung/Schwelle) | Jede Reihe erscheint und verschwindet; ab Werk sind alle aus | |
| 2.7 | Wärmebild, Wärmepumpen-Reiter | Außentemperatur am Raumtemperaturbild zuschalten, Speicher wählen | Außentemperatur erscheint, Speicherauswahl wirkt; Außentemperatur auch im Quellprofil | |

## 3 Zapfprofil-Vorschau in drei Stufen

Ort: Brauchwasser des Projekts, Zapfprofil, Stufen Einfach, Erweitert, Experte (#1043, #1044, #1064).

| Nr | Ort | Schritt | Erwartung | Befund |
|---|---|---|---|---|
| 3.1 | Stufe „Einfach“ | Stufe wählen, Vorschau ansehen | Vorschau haftet sichtbar, Eingaben ohne Abbruch | |
| 3.2 | Stufe „Erweitert“ | Stufe wählen, Zonenliste und Zonenblock öffnen | Linke Spalte trägt Zonenliste und Zonenblock, Vorschau bleibt sichtbar | |
| 3.3 | Stufe „Experte“ | Stufe wählen, Fachwerte öffnen | Alle Felder erreichbar, ein Feld je Wert | |
| 3.4 | Alle Stufen | Vorschau beim Rollen beobachten | Vorschau haftet in jeder Stufe | |
| 3.5 | Zonenblock | Karten auf- und zuklappen | Karten mit Zusammenfassungszeile, Gruppe „Gebäude und Anlage“ getrennt von der Zone | |
| 3.6 | Neue Zone | Neue Zone anlegen | Geführte Anlage, waagerechte Wahlgruppen für Stufe und Niveau | |
| 3.7 | Wohnungstabelle | Wohnungstabelle bei Mischnutzung öffnen | Tabelle bearbeitbar, Belegung folgt der Eingabe | |

## 4 Gesetzeskatalog

Ort: Menü „Kosten“, „Gesetzliche Parameter…“ (#1086, #1075).

| Nr | Ort | Schritt | Erwartung | Befund |
|---|---|---|---|---|
| 4.1 | Gesetzeskatalog | Dialog öffnen | Katalog mit den gesetzlichen Parametern erscheint, Spalten mit Einheiten | |
| 4.2 | Neue Zeile | Zeile anlegen, Schlüssel eines vorhandenen Eintrags wählen | Einheit ist aus dem gleichen Schlüssel vorbelegt | |
| 4.3 | Neue Zeile | Schlüssel ohne Vorgänger wählen | Einheit ist die häufigste des Bereichs | |
| 4.4 | Neue Zeile | Energiesteuer wählen | Einheit ist Pflichtwahl und nicht vorbelegt | |
| 4.5 | Einheitenliste | EEG und Umlagen anlegen | Beide sind in der Einheitenwahl wählbar | |
| 4.6 | Kostenvorlagen | Arbeitspreis und Leistungspreis neu anlegen | Vorbelegung €/kWh bzw. €/(kW·a); gespeicherte Einheiten bleiben | |

## 5 Kalenderbaustein

Ort: Menü „Wärmebedarf & Heizung“, „Profile & Lastgänge“, „Betriebskalender“ und Bedarfsprofil-Dialog (#1088, #1117–#1119).

| Nr | Ort | Schritt | Erwartung | Befund |
|---|---|---|---|---|
| 5.1 | Bedarfsprofil-Dialog | Baustein „Betriebskalenderwahl“ ansehen | Kalender wählbar, Wahl wird übernommen | |
| 5.2 | Betriebskalender-Dialog | „Aus Gebäude übernehmen…“ wählen | Kalender des Gebäudes wird als Kopie übernommen | |
| 5.3 | Betriebskalender-Dialog | Normkalender der Kältevorlage 8 wählen | Normkalender trägt das Feiertagsland im Namen | |
| 5.4 | Betriebskalender-Dialog | Schalter „Ferien wie Sonntag“ umlegen | Ferientage folgen dem Sonntagsprofil, ausgeschaltet nicht | |
| 5.5 | Kalenderkarte des Zapfprofils | Jahresraster ansehen | Jahresraster ohne Achswahl, Ferien und Feiertage sichtbar | |

## 6 Kältebedarfsprofile

Ort: Menü „Wärmebedarf & Heizung“, „Profile & Lastgänge“, „Kältebedarf“ (#1087, #1101–#1104, #1108, #1116, #1130, #1131).

| Nr | Ort | Schritt | Erwartung | Befund |
|---|---|---|---|---|
| 6.1 | Kältebedarf, Vorlagen | Vorlagen 7 bis 16 durchsehen | Zehn Vorlagen mit Art-Vorschlag und Nutzungszeiten | |
| 6.2 | Kältezeile | Eingabeart „Monatswerte“ wählen | Zwölf Monatswerte eingeben, Kennzahlenzeile folgt | |
| 6.3 | Kältezeile | Eingabeart „Wochenprofil“ wählen | Wochenprofil eingeben, Kennzahlenzeile folgt | |
| 6.4 | Grafikblätter der Kältezeile | Jahresgang, Monate, Woche, Dauerlinie öffnen | Alle Blätter zeichnen; Grafik folgt dem Arbeitsstand | |
| 6.5 | Blatt „Woche“ | Kalenderwoche wählen | Wahl wirkt; Tagesarten erscheinen als Hintergrund | |
| 6.6 | Typtag-Editor im Bedarfsprofil-Dialog | Typtage anlegen und ändern | Editor bearbeitbar, Typtage erscheinen in der Zuordnung | |
| 6.7 | Prozesswärme und Brauchwasser | Eingabearten Monatswerte und Wochenprofil wählen | Beide Eingabearten stehen zur Wahl und rechnen | |
| 6.8 | Projekt mit Kälte | Simulation starten, Ergebnis ansehen | Kältebedarf je Profil im Ergebnis, Split-Zeilen einzeln ausgewiesen | |

## 7 Energieflussbild

Ort: Übersicht, „Ergebnisse — Energie“ (#1162–#1165).

| Nr | Ort | Schritt | Erwartung | Befund |
|---|---|---|---|---|
| 7.1 | „Ergebnisse — Energie“ | Umschalter „Tabelle \| Grafik \| Fluss“ auf „Fluss“ stellen | Energieflussbild einer gewählten Version erscheint | |
| 7.2 | Versionswahl | Andere Version der Vergleichswahl wählen | Bild wechselt auf die gewählte Version | |
| 7.3 | Schalter „Kette \| Stufen“ | Beide Stellungen ansehen | Gestraffte Kette bzw. Stufen mit Verbindern | |
| 7.4 | Formfilter | Filter setzen | Hervorhebung folgt dem Filter | |
| 7.5 | Bildschirmbreite | Fenster breit und schmal ziehen | Bild passt sich an, bleibt lesbar | |
| 7.6 | Knoten und Flüsse | Zeiger über Knoten und Flüsse führen | Werte stehen nur im Zeigertext | |
| 7.7 | Bilanz | Knoten mit Verlust oder Restband ansehen | Verlustband, Band „nicht zugeordnet“ und ungedeckter Bedarf erscheinen, wo vorhanden | |
| 7.8 | Wortbericht | Bericht erzeugen | Energieflussbild nach der Kennzahlentafel, immer die Kette, Knoten mit Jahressumme | |

## 8 Katalogauswahl und Ganglinien

Ort: Katalogauswahl der Anlagendialoge, Menü „Profile & Lastgänge“ (#1167–#1169).

| Nr | Ort | Schritt | Erwartung | Befund |
|---|---|---|---|---|
| 8.1 | Katalogauswahl, Kompaktstufe | Fenster verkleinern | Suchplatzhalter bleibt ganz, Knöpfe kürzen sich | |
| 8.2 | Katalogauswahl | Marke „Katalog (DB)“ mit der Maus berühren | Hinweis „Katalog aus der Datenbank – gilt für alle Projekte“ | |
| 8.3 | Kältemaschinen-Auswahl | Titel lesen | Titel lautet „Kältemaschinen“ | |
| 8.4 | Stromganglinie | Zwei Sätze ankreuzen, „Vergleichen…“ wählen | Vergleich mit Jahresarbeit, Spitze und Vollbenutzungsstunden | |
| 8.5 | Solarganglinie | Wie 8.4 | Vergleich öffnet für die Solarganglinie | |
| 8.6 | PV-Ganglinie | Wie 8.4 | Vergleich mit Raster und Nennleistung zusätzlich | |
| 8.7 | Alle drei Ganglinien | Weniger als zwei Kreuze setzen | „Vergleichen…“ ist gesperrt; Ankreuzen wählt nicht | |

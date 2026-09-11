# Speicherauslegung mit Kostenprofilen und Zeitreihen

Entwicklungsstand: 11.09.2026. Die Erweiterung betrifft die Auslegungsoptimierung der aktiven Speicheranlage in EPOS-Plan. Die eigenständige Python-Simulation und ihre HTML-Auswertung sind separate Werkzeuge.

## Bedienung

Unter **Simulation → Detaillierte Simulation → Parameter → Stromspeicher → Auslegung optimieren** werden Suchraum, Kosten und Datenquellen festgelegt. Die technischen Eigenschaften des aktiven Speichers, etwa Wirkungsgrad und SoC-Grenzen, bilden die Grundlage der Suche.

1. **Größenbasis wählen:** Kapazität in kWh oder Leistung in kW; Untergrenze, Obergrenze und optional Schrittweite eingeben. Ohne Größenschritt verwendet die Suche die Anzahl der Stützstellen. Gleiche Grenzen prüfen eine feste Größe.
2. **C-Rate wählen:** Untergrenze, Obergrenze und Schrittweite in 1/h. Bei Kapazitätsvorgabe gilt `P = E · C-Rate`, bei Leistungsvorgabe `E = P / C-Rate`. Gleiche Grenzen prüfen eine feste C-Rate.
3. **Investition und Betrieb getrennt einstellen:** jeweils „Kostenmodul“ oder „Im Dialog“. Die Kostenmodul-Sätze gehören zur aktiven Speicheranlage; ausgeschlossene Positionen erscheinen als Hinweis.
4. **Last, PV und Bezugspreis unabhängig wählen:** EPOS-Projekt oder jeweils eine eigene CSV-Datei; zusätzlich „Keine PV“ beziehungsweise ein editierbares Strompreisprofil.
5. **Profil speichern:** einen Namen vergeben und speichern. Ein geladenes Profil kann bearbeitet oder unter einem neuen Namen dupliziert werden. Bereiche, Kostensätze, Preisprofil, Quellen, CSV-Zuordnung und eingelesene Werte werden gemeinsam gespeichert.
6. **Neu berechnen:** Heatmap und Kennzahlen zeigen die untersuchten Größen und den wirtschaftlich besten Punkt. Nach Eingabeänderungen ist das bisherige Ergebnis als veraltet gekennzeichnet; die Übernahme des alten Bestpunkts bleibt bis zur Neuberechnung gesperrt.

Die Auslegung verändert die Speicheranlage erst bei der bestehenden Aktion zur Übernahme des Bestpunkts. Ein exportiertes Ergebnis gehört zum Eingabestand seines Rechenlaufs.

## Kosten und Einheiten

Alle Kostensätze sind netto. Investition und Betrieb dürfen aus unterschiedlichen Quellen stammen. Explizite Nullwerte sind möglich; fehlende oder ungültige Sätze einer ausgewählten Quelle werden nicht stillschweigend zu Nullkosten.

| Feld | Einheit | Ansatz je Größenkandidat |
|---|---|---|
| Investition Leistung | €/kW | Satz × AC-Nennleistung |
| Investition Kapazität | €/kWh | Satz × Nennkapazität |
| Betrieb Leistung | €/(kW·a) | Satz × AC-Nennleistung pro Jahr |
| Betrieb Kapazität | €/(kWh·a) | Satz × Nennkapazität pro Jahr |
| Betrieb Entladung | €/kWh entladen | Satz × simulierte AC-Entladeenergie pro Jahr |

Im Kostenmodul werden die Bemessungen `EUR_PRO_KW_LEISTUNG`, `EUR_PRO_KW_ELEKTRISCH` und `EUR_PRO_KWH_KAPAZITAET` übernommen. Bei Betriebskosten ist zusätzlich `EUR_PRO_KWH_ELEKTRISCH` die entladene Strommenge. Pauschalbeträge, Prozentsätze, Erlöse, Zuschüsse, negative oder fehlende Sätze sowie erst in späteren Jahren beginnende Positionen werden mit Hinweis ausgeschlossen. Die Auslegung bildet diese Kosten ausschließlich über die genannten spezifischen Sätze ab; feste Investitionsbeträge werden in diesem Modus nicht zusätzlich angesetzt.

`I = c_P · P + c_E · E`

`OPEX_a = o_P · P + o_E · E + o_D · Q_Entladung,a`

Der Optimierer berücksichtigt Betriebskosten bei Jahresüberschuss, Kapitalwert und Amortisation. Der optionale Verschleißterm wird getrennt in der Zielfunktion abgezogen. Er darf nicht zugleich als derselbe Kostenanteil unter den entladeabhängigen Betriebskosten eingegeben werden. Die Suche ist eine vereinfachte Bewertung mit den bestehenden Zins-, Lebensdauer- und Degradationsparametern; die vollständige projektweite Wirtschaftlichkeitsrechnung bleibt ein eigener Auswertungsschritt.

Der Betriebskostenbetrag des simulierten Referenzjahres wird in dieser Bewertung als konstanter Jahresbetrag abgezinst. Der vorhandene Degradationsansatz wirkt auf die Ertragsseite. Eine jährlich neu simulierte Alterung der Fahrweise gehört zum weiterführenden Modell.

## Datenquellen und CSV-Import

| Rolle | Interne Einheit im Datenvertrag | Bedeutung |
|---|---|---|
| `load_kw` | kW | Bruttolast vor Abzug von PV und BHKW |
| `pv_kw` | kW AC | Verfügbare PV-Leistung nach Wechselrichterverlusten und vor zusätzlicher Abregelung |
| `buy_eur_kwh` | €/kWh | Effektiver variabler Bezugspreis, auch negativ |

Die EPOS-Last verwendet den Strombedarf einschließlich der im Lauf ermittelten elektrischen Zusatzverbraucher. Die PV-Quelle ist die theoretisch verfügbare AC-Produktion des PV-Laufs. Bereits um PV bereinigte Nettolast darf nicht nochmals zusammen mit derselben PV-Reihe als Bruttolast eingelesen werden.

Der CSV-Dialog bietet Vorschau, Trennzeichen, Dezimaltrennzeichen, UTF-8 oder Windows-1252, Kopfzeile und übersprungene Zeilen. Zeitstempel und Werte werden über Spaltenauswahl zugeordnet. Datum und Uhrzeit können auch getrennt vorliegen. Zeitformat, Zeitzone und Intervallanfang oder -ende sind ausdrücklich festzulegen.

Last und PV unterstützen kW, MW oder kWh je Intervall. Preise unterstützen €/kWh, ct/kWh und €/MWh. Der Import normalisiert zusammenhängende 15- oder 60-Minuten-Reihen auf Viertelstunden: Leistungswerte bleiben Intervallmittelwerte, Intervallenergie wird durch die Dauer geteilt, Stundenpreise gelten für vier Viertelstunden. Lücken, doppelte UTC-Zeitstempel, unklare Sommerzeitstunden und nicht endliche Werte werden abgewiesen. Negative Last und PV sind unzulässig; negative Preise sind gültig.

Für die Jahreswirtschaftlichkeit ist ein vollständiges Kalenderjahr erforderlich. Alle ausgewählten Dateien müssen dieselbe UTC-Achse besitzen. Eine Datei mit nur einigen Tagen lässt sich zum Prüfen der Spalten importieren, ersetzt aber keinen vollständigen Jahresdatensatz in der Auslegung.

Sobald Dateien verwendet werden, bleibt deren tatsächliche Zeitachse erhalten, einschließlich Schaltjahr und Zeitumstellung. Bei Mischung mit EPOS-Modellreihen muss die Kalenderzuordnung ausdrücklich eingeschaltet werden: EPOS-Werte werden nach Monat, Tag und Uhrzeit zugeordnet. Für den 29. Februar wird der EPOS-28. Februar verwendet; bei einer wiederholten Ortsstunde wird der entsprechende EPOS-Modellwert erneut verwendet. Die CSV-Werte werden dabei nicht gemittelt oder gelöscht. Diese Annahme erscheint im Ergebnis als Hinweis.

Eine importierte Bezugspreisreihe enthält bereits den effektiven Preis. EPOS addiert darauf keine weiteren Tarifaufschläge. Ein Strompreisprofil verwendet dagegen das bestehende EPOS-Format mit zwölf Monatswerten und optional 168 Wochenstundenwerten in ct/kWh; darauf wird der Projektaufschlag einmal angewendet.

Die PV-Einspeisevergütung stammt auch bei PV aus CSV aus dem Projekt und wird bei der Bewertung entgangener Einspeisung berücksichtigt. Eine reine Dateiauslegung verwendet die eingelesene Last und PV; zusätzliche BHKW-Erzeugung aus einem Simulationslauf wird in diesem Modus nicht hinzugemischt.

## Speicherung und technische Anschlussstellen

Migrationsschritt **73** legt `Tab_SpeicherAuslegung` mit Projekt- und Anlagenbezug sowie einem eindeutigen Index je Profilnamen an. `@Aktuell` bezeichnet den letzten Eingabestand; dieser reservierte Name wird nicht als Benutzerprofil angeboten. Ein benanntes Profil ist eine eigenständige Kopie. Die Payload ist versioniertes, komprimiertes JSON (`gz1:`), einschließlich eingelesener Werte, Zeitstempel, Dateiname, SHA-256 und Importoptionen. Die Ursprungsdatei muss beim erneuten Öffnen nicht vorhanden sein.

Die Daten bleiben beim Kopieren des Projekts und beim erneuten Speichern derselben Speicheranlage im Assistenten erhalten. Das Löschen des Projekts entfernt seine Profile über Fremdschlüssel. Die Dateien `-wal` und `-shm` werden nicht einzeln behandelt; Sicherung und Schemapflege folgen [BETRIEB_SQLITE.md](BETRIEB_SQLITE.md).

| Schicht | Anschlussstelle | Aufgabe |
|---|---|---|
| Rechenkern | `SpeicherEngine/OptimiererOptionen.cs`, `SpeicherOptimierer.cs`, `PeakShaving.cs` | Größenraster, Betriebskosten, Netzanschlussbewertung |
| EPOS-Kern | `Controller/SpeicherAuslegungCtrl.cs` | Speicherung und spezifische Kosten aus Projektpositionen |
| EPOS-Kern | `Controller/SpeicherAuslegungCtrl.Rechnung.cs` | Eingabestand einfrieren, Quellen und Zeitachsen zusammenführen |
| EPOS-Kern | `Controller/SpeicherZeitreihenImport.cs` | Vorschau, Spaltenwahl, Prüfung und Normierung |
| EPOS-Kern | `Controller/SpeicherOptimierungCtrl.cs` | Kennzahlen, Diagramme und Exporte |
| Oberfläche | `Dialoge/Strom/SpeicherAuslegungEditor.razor`, `SpeicherZeitreihenDialog.razor` | Bedienung und Rückmeldungen |
| Windows | `SimulationErgebnisHuelle.Optimierung.cs` | Dateiauswahl, Speicherung und Hintergrundlauf |

Die Kandidatenrechnung greift nicht auf die Datenbank zu. Sie erhält einen unabhängigen Eingabestand mit bereits aufgelösten Kostensätzen und ausgerichteten Zeitreihen. Änderungen am Dialog verändern einen laufenden Kandidaten nicht.

Die Umsetzung optimiert die **aktive einzelne Speichervariante**. Eine gemeinsame Größenoptimierung mehrerer physisch parallel betriebener Speicher und nichtlineare Investitionskurven mit Stützstellen gehören weiterhin zum weiterführenden Konzept. Benannte Profile speichern in dieser Umsetzung die spezifischen Kostensätze samt vollständiger Auslegungskonfiguration.

## Prüfung

Die Tests prüfen Rechenwerte, Zeitachsen, Kostenquellen, Speicherung und Bedienung. Die Ergebnisse stehen im begleitenden Umsetzungs- und Prüfbericht.

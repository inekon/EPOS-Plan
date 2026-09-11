# Mehrspeicher: Konzept, Umsetzung und Prüfumfang

Stand: 11.09.2026

Dieses Dokument beschreibt den tatsächlich implementierten Mehrspeicherstand in EPOS-Plan. Grundlage sind Kapitel 4 bis 9 sowie 13 und 14 der [Spezifikation](Projekte/Speichersimulation/Spezifikation.md). Kapitel 14 und die ältere [Dokumentation zur Speicherauslegung](Doku_Speicherauslegung_Kosten_Zeitreihen.md) bleiben als datierter Nachweis der vorangegangenen Einzelspeicherauslegung erhalten.

Eine Flotte ist eine Liste physisch gleichzeitig betriebener AC-Speicher innerhalb **einer** Variante. Vergleichsvarianten werden nicht addiert. Jeder Speicher besitzt einen eigenen Zustand, Richtungsleistungen, Wirkungsgrade, SoC-Grenzen, Reserve, Hilfsverbrauch und Kosten. Der bestehende Einzelspeicherpfad wurde nicht ersetzt.

## Architektur und Konzept-zu-Code-Mapping

`EPOS.UI` bearbeitet Flotte, Ziele und Quellen. `EPOS.Kern` löst Projektquellen und Kosten auf, wandelt ct/kWh einmal in €/kWh um, bildet UTC-Snapshots und hält den freigegebenen Laufstand. `SpeicherEngine` rechnet UI- und DB-frei. `SpeicherPlanung` bindet Google OR-Tools/SCIP ausschließlich hinter `IFlottenPlaner` an. Eingabe und Konfiguration werden vor jedem Lauf tief kopiert.

| Konzept oder Garantie | Umsetzung |
|---|---|
| Serialisierbare Flotten-, Plan-, Ergebnis-, Wirtschafts- und Suchverträge | [`SpeicherEngine/FlottenModel.cs`](SpeicherEngine/FlottenModel.cs) |
| AC-Physik, reaktive Ziele, Verteilung, Snapshotwahl und tiefe Laufkopie | [`SpeicherEngine/FlottenSimulator.cs`](SpeicherEngine/FlottenSimulator.cs) |
| Jahreskonten, CAPEX, OPEX, Ersatz, Restwert und NPV | [`SpeicherEngine/FlottenWirtschaftlichkeit.cs`](SpeicherEngine/FlottenWirtschaftlichkeit.cs) |
| Rainflow-Zyklen und Miner-Schaden | [`SpeicherEngine/FlottenRainflow.cs`](SpeicherEngine/FlottenRainflow.cs) |
| Nullvariante und vollständige begrenzte Rastersuche | [`SpeicherEngine/FlottenOptimierer.cs`](SpeicherEngine/FlottenOptimierer.cs) |
| MILP mit Zeitlimit und unterscheidbaren Status | [`SpeicherPlanung/OrToolsFlottenPlaner.cs`](SpeicherPlanung/OrToolsFlottenPlaner.cs) |
| Projekt-/CSV-Adapter, Prognose- und Jahresimport | [`EPOS.Kern/Controller/SpeicherFlottenStudieCtrl.cs`](EPOS.Kern/Controller/SpeicherFlottenStudieCtrl.cs) |
| Profil, geprüfte Aktivierung und gewöhnlicher Projektlauf | [`EPOS.Kern/Controller/SpeicherAuslegungModel.cs`](EPOS.Kern/Controller/SpeicherAuslegungModel.cs), [`EPOS.Kern/Controller/SpeicherFlottenProjektCtrl.cs`](EPOS.Kern/Controller/SpeicherFlottenProjektCtrl.cs) |
| Flotteneditor, Quellenimport, Ergebnis und CSV | [`EPOS.UI/Dialoge/Strom/SpeicherFlottenDialog.razor`](EPOS.UI/Dialoge/Strom/SpeicherFlottenDialog.razor), [`EPOS.UI/Dialoge/Strom/SpeicherFlottenEditor.razor`](EPOS.UI/Dialoge/Strom/SpeicherFlottenEditor.razor) |
| Einstieg und aktive Flotte in den detaillierten Simulationsergebnissen | [`EPOS.UI/Seiten/Simulation/StromspeicherReiter.razor`](EPOS.UI/Seiten/Simulation/StromspeicherReiter.razor) |

Ein freigegebener Kandidat kann als `@Projektflotte` aktiviert und im gewöhnlichen Projektlauf gefahren oder wieder deaktiviert werden. Dieser reservierte Stand wird mit Anlagenbezug `NULL` gespeichert und gehört dadurch zum Projekt, nicht zur gerade gewählten Einzelanlage. Er bleibt bei Variantenwechsel und beim Löschen einer einzelnen Anlagenzeile erhalten. Arbeitsstände und benannte Auslegungsprofile bleiben davon getrennt.

Die Übernahme prüft Zulässigkeit, vollständige Zeitreihen, Daten-ID, Kandidaten-ID und beste Konfiguration. Die ID enthält je Einheit ID, Kapazität, Lade- und Entladeleistung. Die Nullvariante ist keine aktivierbare physische Flotte. Neue Einheiten ohne belastbare Kostensätze werden nicht als Nullkostenanlage angenommen.

Im Reiter „Stromspeicher“ zeigt die Karte **Speicherflotte und Auslegung** die aktuellen Einheiten, Kapazitäten und Richtungsleistungen. Betriebsziel, Leistungsverteilung und Erzeuger-Reihenfolge sind hier und im Auslegungsdialog über denselben Baustein editierbar. Bedingte Felder für Peak-Ziel, Netzladung und Batterieexport gehören zu diesem gemeinsamen Eingabestand. Die früheren Felder „Graustrom“ und „Dauernutzung“ werden beim Flottenbetrieb durch diese Steuerung ersetzt.

**Simulation starten** liest den aktuellen Arbeitsstand, rechnet Last und Erzeugung frisch und verwendet anschließend genau diese Flotte am Speicherzweig. Eine vorherige Studienrechnung ist dafür nicht erforderlich. Erst nach einem erfolgreichen Gesamtprojektlauf wird der geprüfte Stand als Projektflotte übernommen und die Ergebnisansicht ersetzt. Ein Fehler führt zu einer konkreten Meldung; der letzte erfolgreiche Flottenstand bleibt erhalten. Geänderte Eingaben kennzeichnen bisherige Ergebnisse als veraltet. Eine ausdrücklich deaktivierte Flotte bleibt im Einzelbetrieb, bis die Flotteneinstellungen erneut gespeichert werden.

Nach dem Projektlauf verwendet die Ergebnisansicht denselben freigegebenen Flottenlauf für Bildschirm und CSV. Sie stellt Referenz ohne Speicher und Flotte gemeinsam gegenüber, einschließlich Rechnung, Netzbezug, Bezugsspitze, Einspeisung, Abregelung, Verlusten, individuellen Speicherkennzahlen, Ladezustand und Netzleistung. Damit stammen Karte, Tabellen, Diagramme und Exporte aus demselben Daten- und Konfigurationsstand.

Die Projekt-Netzbilanz führt Netzbezug und Einspeisung als getrennte, nichtnegative Reihen. Nur der Netzbezug fließt in den Reststrombedarf ein. Ergebnis, Excel-Export und Diagramme weisen PV-, BHKW- und Batterieeinspeisung sowie die Gesamteinspeisung ohne Doppelzählung aus; PV-Abregelung bleibt separat. Ein 96-Intervall-Test mit zwei Speichern deckt gleichzeitig Netzbezug, alle drei Einspeisequellen und Abregelung ab.

## Physik, Strategien und Verteilung

Alle Leistungen liegen AC-seitig in kW, Energien in kWh und Preise in €/kWh vor. Für `Δt = 0,25 h` gilt je Speicher:

```text
E(t+1) = E(t) + Δt · η_laden · P_laden - Δt · P_entladen / η_entladen
SocMin · Kapazität <= E(t) <= SocMax · Kapazität
```

Lade- und Entladeleistung werden getrennt begrenzt. Ein Verfügbarkeitsfaktor von 0 bis 1 skaliert beide Grenzen je Intervall. Hilfsverbrauch ist zusätzliche Standortlast. Die Flotte lädt und entlädt nie gleichzeitig; auch Import und Export sind gegenseitig ausgeschlossen. PV-Abregelung ist nur aus verbleibendem PV-Überschuss möglich. Nicht abregelbare Erzeugung und Last bleiben bei verletzten Anschlussgrenzen sichtbar: Es gibt weder versteckten Lastabwurf noch fiktive Erzeugungsvernichtung.

Technische Import- und Exportgrenzen sind hart und machen die Variante bei verbleibender Verletzung unzulässig. Das wirtschaftliche Peak-Ziel ist separat und weich. Die Peak-Reserve erhöht im Normalbetrieb die Untergrenze; bei einer tatsächlichen Peak-Überschreitung darf die Ausführung sie freigeben. Der Planer hält sie im Horizont konservativ vor.

| Betriebsziel | Ausgeführtes Verhalten |
|---|---|
| `PvGreedy` | Reaktiv Überschuss laden und bei Nettobedarf entladen; keine Zukunftsinformation |
| `PeakShaving` | Reaktiv am Netzanschlusspunkt gegen das wirtschaftliche Peak-Ziel |
| `PvPlanung` | Lexikografisch Netzbezug, PV-Abregelung und danach gespeicherte Energie minimieren |
| `Arbitrage` | Prognostizierte Arbeits-, Leistungs-, Export-, Verschleiß- und Endenergiekosten minimieren |
| `MultiUse` | Zuerst größte Peak-Verletzung minimieren, danach wirtschaftliche Kosten |

Planende Ziele liefern je Horizont und Einheit Lade- und Entladeleistung. Der Simulator prüft Planstatus, Zeitachse, Einheitenzahl, Werte und Flottenrichtung und führt gegen den **tatsächlichen** Zustand aus. Nach `NeuplanungAlleIntervalle` plant er mit real erreichter Energie neu. Fallback ist nur nach expliziter Freigabe zulässig und wird je Intervall samt Grund sowie als Anzahl ausgewiesen.

Die reaktive Verteilung begrenzt jede Einheit zuerst durch Verfügbarkeit, Leistung, SoC, Wirkungsgrad und Reserve und verteilt Restleistung deterministisch neu:

- `KapazitaetsProportional` nach nutzbarer beziehungsweise freier Energie;
- `Kaskade` in stabiler Konfigurationsreihenfolge;
- `Grenzkosten` beim Entladen nach Grenzverschleiß und Wirkungsgrad, beim Laden nach Wirkungsgrad.

## MILP-Ziele und Informationssicherheit

Der Solver modelliert je Einheit Energie, Laden und Entladen sowie je Intervall Import, Export, PV-Abregelung und getrennte PV-/BHKW-/Batterieexporte. Binärvariablen sichern Batterie- und Netzrichtung. Nebenbedingungen bilden Energie- und Quellenflüsse, SoC, Verfügbarkeit, Anschlussgrenzen, Erzeugerpriorität und optionale Endenergie ab. Netzladung bei gleichzeitiger Abregelung ist ausgeschlossen.

Das wirtschaftliche Horizontziel ist:

```text
Σ Δt · (Import · Bezugspreis - PV-Export · PV-Preis
       - BHKW-Export · BHKW-Preis - Batterieexport · Batteriepreis
       + Entladung(i) · Grenzverschleiß(i))
+ Peak · Leistungspreis - Endenergie · Ausgleichswert [ohne harte Endbedingung]
```

Lexikografische Ziele werden nacheinander gelöst und das vorherige Optimum jeweils fixiert. `Optimal` gilt nur für das formulierte endliche Horizontproblem. Ein Zeitlimit-Incumbent ist `Zulaessig`; Zeitlimit ohne Lösung, Unzulässigkeit und Fehler bleiben unterscheidbar. Harte Netzgrenzen haben keinen diagnostischen Schlupf. Der rollierende Jahreslauf und das Größenraster werden nicht als global optimal bezeichnet.

Ein `FlottenPrognoseSnapshot` enthält ID, `BekanntSeit`, Entscheidungszeitpunkt, Art und Viertelstundenwerte. Sein Konstruktor kopiert die Daten; Horizontausschnitte teilen nur diesen unveränderten Laufpuffer. Für `VerifiziertBekannt` muss die Art passen, `BekanntSeit <= Entscheidung` gelten und der gewählte, zuletzt bekannte Snapshot den ganzen Horizont lückenlos abdecken. Die Istreihe wird nie still als historische Prognose verwendet.

`Oracle` ist ausdrücklich gewähltes Idealwissen. Der Adapter erstellt einen benannten Vollreihensnapshot aus Istwerten; der Simulator schneidet daraus Horizonte. UI und Ergebnis kennzeichnen ihn als optimistische Vergleichsgrenze, nicht als historisch erreichbaren Fahrplan.

## NPV, Jahresdaten und SoC-Mitnahme

Referenz und Variante verwenden denselben Standortdatensatz und Tarif. Die Referenz hat keine Speicher. Die Rechnung enthält Bezug, getrennte PV-/BHKW-/Batterieerlöse, Jahrespeak und Fixkosten. Batterieexport benötigt einen endlichen eigenen Verkaufspreis; der Bezugspreis wird nicht als Verkaufspreis missbraucht. Die feste Quellenpriorität macht Direktexport reproduzierbar, führt aber keine Energieherkunft im Speicher.

```text
CF(a) = Rechnung_Referenz(a) - Rechnung_Variante(a)
        - OPEX(a) - Durchsatzkosten(a) - Ersatzkosten(a)
        + bewertete Endenergieänderung(a)

NPV = -CAPEX + Σ CF(a)/(1+r)^a + Restwert/(1+r)^n
```

CAPEX umfasst je Einheit Festbetrag, €/kWh und €/kW auf das Maximum beider Richtungsleistungen. OPEX umfasst feste, kapazitäts- und leistungsbezogene Beträge; Durchsatzkosten beziehen sich auf AC-Entladung. Teiljahre sind keine Jahrescashflows. Gelieferte Projektjahre müssen lückenlos und vollständig sein und werden jeweils neu simuliert; der End-SoC jeder Einheit wird zum Start-SoC des Folgejahres.

Die finanzielle Projektlaufzeit ist im Dialog stets sichtbar und editierbar. Ein bereitgestelltes Referenzjahr wird nur bei `ReferenzjahrExplizitWiederholen` über diese Laufzeit vervielfacht und entsprechend gekennzeichnet; ohne Wiederholung kann es mit einer Laufzeit von einem Jahr einmal bewertet werden. Tatsächliche Projektjahre müssen die gewählte Laufzeit vollständig abdecken. Ersatz wird im fälligen Jahr neu gebucht. Ohne Endenergiegleichheit ist ein expliziter Ausgleichswert Pflicht, damit Anfangsenergie kein kostenloser Ertrag wird.

Auf Anwenderwunsch ist **Referenzjahr ausdrücklich wiederholen** bei neuen oder bisher unvollständig eingerichteten Profilen sichtbar vorgewählt, sofern keine tatsächlichen Projektjahre importiert wurden. Eine vorhandene Projektlaufzeit bleibt erhalten; neue Flotten übernehmen die Nutzungsdauer der Speichergrundlage, andernfalls gelten 20 Jahre. Der Energie-Ausgleichswert wird aus dem mittleren effektiven Bezugspreis vorgeschlagen, sofern diese Quelle verfügbar ist. Beide Werte sind editierbar. Eine gespeicherte bewusste Änderung wird nicht erneut durch Vorgaben ersetzt. Die Wiederholung ist eine vereinfachte wirtschaftliche Projektion und erzeugt keine neuen realen Jahresdaten.

Hinweise erläutern Eingaben und Annahmen. Vor dem Vergleich werden fehlende Jahresgrundlagen, ungültige Bereiche, fehlende Ausgleichswerte und widersprüchliche Einstellungen gesammelt angezeigt. Das Ergebnis nennt die tatsächlich berechnete Betriebsführung und stellt Peak-Ziel und erreichte Bezugsspitze gegenüber. Ein nicht erreichtes wirtschaftliches Peak-Ziel wird als Warnung ausgewiesen; es ist von einer verletzten harten Anschlussgrenze zu unterscheiden.

## Nullvariante, Suchraum, Speichergrenze und Abbruch

Eine Suchachse ersetzt über `ErsetztEinheitId` genau ihre Einheit; andere bleiben fest. Anzahl kann auch null sein. Möglich sind Kapazität plus Leistung, Kapazität plus C-Rate (`P=E·C`) oder Leistung plus C-Rate (`E=P/C`). Beim Skalieren bleibt das Lade-/Entladeleistungsverhältnis der Vorlage erhalten; eine Nullrichtung bleibt null.

Das vollständige endliche kartesische Raster wird mit den ausgewählten Zielen kombiniert. Übersteigt es `MaximaleKandidaten`, wird es abgewiesen und nicht gekürzt. Rangfolge: technisch zulässige Varianten nach höchstem NPV, daneben die technisch zulässige Nullvariante mit NPV 0. Ist die Referenz wegen einer harten Netzgrenze unzulässig, kann eine technisch nötige Variante trotz negativem NPV gewinnen. Sind alle Rechnungen fachlich ungültig, entsteht ein Konfigurationsfehler statt einer falschen Null-Empfehlung.

`CancellationToken` wirkt in Kandidaten-, Jahres-, Intervall- und Solverlauf; Fortschritt meldet Anzahl und Kandidaten-ID. Alle Kandidaten halten nur Zusammenfassungen, die vollständige Reihe nur der beste Kandidat beziehungsweise die Nullvariante.

## Datenbankquellen und CSV-Importe

| Rolle | Quelle | Flottenfeld |
|---|---|---|
| Bruttolast | EPOS oder CSV | `LastKw` |
| verfügbare PV-AC-Leistung | EPOS, CSV oder keine | `PvKw` |
| Bezugspreis | EPOS, Preisprofil oder CSV | `BezugspreisEuroProKWh` |
| BHKW und PV-/BHKW-Vergütung | vorbereiteter Projektlauf | `BhkwKw`, getrennte Verkaufspreise |
| Batterieverkauf | Flottentarif | eigener Verkaufspreis |

Der bestehende CSV-Dialog normalisiert 15- oder 60-Minuten-Reihen auf Viertelstunden und behandelt Zeit-/Wertspalten, Zeitzone, Intervalllage, Kodierung und Einheiten. Lücken, doppelte UTC-Stempel und nicht endliche Werte werden abgewiesen; negative Preise sind erlaubt. Eine CSV-Achse bleibt erhalten. Mischung mit EPOS-Modellwerten verlangt eine explizite Kalenderzuordnung. Details: [Datenquellen und CSV-Import](Doku_Speicherauslegung_Kosten_Zeitreihen.md#datenquellen-und-csv-import).

Prognosen und tatsächliche Projektjahre werden ebenfalls als CSV eingelesen. JSON ist kein Anwenderimport. Der CSV-Dialog zeigt die Datei und eine Vorschau; Zeit- und Datenspalten werden frei zugeordnet. Heruntergeladene CSV-Dateien verwenden denselben Import. Kosten- und EPOS-Zeitreihen können direkt aus der Datenbank stammen.

| CSV-Inhalt | Zuordnung |
|---|---|
| Zeitpunkt | Zeitstempel oder Datum und Uhrzeit, Zeitzone und Intervalllage |
| Bruttolast | `load_kw`, Pflicht |
| Bezugspreis | `buy_eur_kwh`, Pflicht; negative Werte erlaubt |
| PV und BHKW | `pv_kw`, `bhkw_kw`; fehlende Erzeuger bewusst als 0 wählen |
| Exportpreise | getrennt für PV, BHKW und Batterie; eine nicht zugeordnete Spalte bedeutet 0 |
| Prognosekennung | Snapshot-ID, Bekanntheitszeitpunkt und Entscheidungszeitpunkt |

Die Headernamen dienen nur der Vorbelegung. Eigene Spaltennamen werden im Dialog zugeordnet. Leistungs- und Preiseinheiten sind auswählbar und werden vor der Rechnung in kW und €/kWh umgerechnet. CSV-Format, Kodierung, Dezimaltrenner, Kopfzeile und übersprungene Zeilen sind einstellbar. Viertelstunden- und Stundenwerte werden geprüft und auf Viertelstunden normalisiert.

Projektjahre werden anhand der echten Zeitstempel in vollständige Kalenderjahre geteilt. Ihre Zahl muss zur sichtbaren Projektlaufzeit passen. Unter **Wirtschaftliche Jahresprojektion** kann ein einzelnes Referenzjahr einmal bewertet oder für eine bewusst eingestellte Laufzeit wiederholt werden. Das ändert die tatsächlichen Zeitreihen nicht.

Archivierte Prognosen enthalten je Snapshot eine ID sowie `known_at` und `decision_at`. Ein Snapshot darf keine Informationen enthalten, die erst nach der jeweiligen Entscheidung bekannt wurden. Die Vollständigkeit des benötigten Planungshorizonts wird im Simulator geprüft. Idealwissen bleibt eine ausdrücklich gewählte Alternative, die die vorhandene Standortreihe verwendet.

Profile speichern Flotte, Quellen, Spaltenzuordnungen und importierte Laufdaten in der Datenbank. Importierte Dateien müssen zum späteren Laden des Profils nicht erneut vorhanden sein.

Die folgenden kompakten Draft-2020-12-Schemas dokumentieren die beiden manuellen JSON-Importe (Prognosen und tatsächliche Projektjahre), die der CSV-Dialog beim Einlesen prüft. Enums werden als Namen gelesen. `row` steht für ein Objekt mit den erforderlichen Feldern `Zeitstempel` (date-time), `LastKw`, `PvKw`, `BhkwKw` (Zahlen ≥ 0) sowie den vier endlichen Preisen `BezugspreisEuroProKWh`, `PvVerkaufspreisEuroProKWh`, `BhkwVerkaufspreisEuroProKWh`, `BatterieVerkaufspreisEuroProKWh`.

**Archivierte Prognosen:** Zusätzlich prüft der Controller bekannte Art, Bekanntheitszeit und Abdeckung.

```json
{
  "$schema":"https://json-schema.org/draft/2020-12/schema",
  "title":"EPOS Flotten-Prognosen","type":"array","minItems":1,
  "items":{"type":"object",
    "required":["Id","BekanntSeit","Entscheidungszeitpunkt","Art","Intervalle"],
    "properties":{
      "Id":{"type":"string","minLength":1},
      "BekanntSeit":{"type":"string","format":"date-time"},
      "Entscheidungszeitpunkt":{"type":"string","format":"date-time"},
      "Art":{"const":"VerifiziertBekannt"},
      "Intervalle":{"type":"array","minItems":1,"items":{"$ref":"#/$defs/row"}}
    }},
  "$defs":{"row":{"type":"object","required":["Zeitstempel","LastKw","PvKw","BhkwKw","BezugspreisEuroProKWh","PvVerkaufspreisEuroProKWh","BhkwVerkaufspreisEuroProKWh","BatterieVerkaufspreisEuroProKWh"]}}
}
```

**Tatsächliche Projektjahre:** Der Controller prüft eindeutige lückenlose Jahre und eine vollständige UTC-Viertelstundenachse. Verfügbarkeitsarrays müssen dieselbe Länge haben.

```json
{
  "$schema":"https://json-schema.org/draft/2020-12/schema",
  "title":"EPOS Flotten-Projektjahre","type":"array","minItems":1,
  "items":{"type":"object","required":["Jahr","Istwerte"],"properties":{
    "Jahr":{"type":"integer"},
    "IstVollstaendigesJahr":{"type":"boolean","default":true},
    "Istwerte":{"type":"array","minItems":35040,"maxItems":35136,"items":{"$ref":"#/$defs/row"}},
    "Prognosen":{"type":"array","default":[]},
    "VerfuegbarkeitsfaktorNachEinheitId":{"type":"object","default":{},"additionalProperties":{"type":"array","items":{"type":"number","minimum":0,"maximum":1}}}
  }},
  "$defs":{"row":{"type":"object","required":["Zeitstempel","LastKw","PvKw","BhkwKw","BezugspreisEuroProKWh","PvVerkaufspreisEuroProKWh","BhkwVerkaufspreisEuroProKWh","BatterieVerkaufspreisEuroProKWh"]}}
}
```

`SpeicherAuslegungKonfiguration.Flotte` speichert Konfiguration, Quellen und importierte Werte im vorhandenen versionierten gzip-JSON-Payload (`gz1:`). Dies ist interne Persistenz, kein dritter Dateiimport. Nullable Grenzen bleiben `null`; unendliche JSON-Zahlen werden nicht verwendet. Die Schemas sind Dokumentation und keine separat ausgelieferten `.schema.json`-Validatoren.

Abgelegt wird der Payload in `Tab_SpeicherAuslegung`. **Migrationsschritt 73** legt diese Tabelle an, **Migrationsschritt 74** (Auftrag #178, 11.09.2026) baut sie als **STRICT**-Tabelle neu auf: Sie war die einzige Fachtabelle des Zielschemas ohne `STRICT`, und SQLite kennt kein `ALTER TABLE … STRICT`. Spalten, Typen, Fremdschlüssel (`ON DELETE CASCADE` an Projekt und Energieanlage) und der eindeutige Index bleiben wortgleich; die Zeilen werden samt ihrer `ID` übernommen, es ändert sich kein Wert. Die Anweisungen stehen in [`EPOS.Kern/Allgemein/Update/SpeicherAuslegungStrict.cs`](EPOS.Kern/Allgemein/Update/SpeicherAuslegungStrict.cs).

## Ergebnisdarstellung (P2, Auftrag #184)

Die Ergebnisseite der Flotte (`EPOS.UI/Dialoge/Strom/SpeicherFlottenErgebnisAnsicht.razor`)
folgt seit dem 11.09.2026 der Hausregel `Doku_Simulationsergebnis_Darstellung.md`. Grundlage
sind die Anwenderentscheide **SD‑Q6** (Vollfassung statt bloß herausgelöster Tabelle) und
**SD‑Q7** (Zeitraumwahl) vom selben Tag; das Zielbild steht in
[`Projekte/Konzept_Stromspeicher_Dialoge_EPOS-Plan.md`](Projekte/Konzept_Stromspeicher_Dialoge_EPOS-Plan.md)
Abschnitt 2.2 und 2.3.

**Was der Anwender sieht.** Von oben nach unten: die Hinweise des Laufs als Warnbanner, eine
Zeile „Berechnete Betriebsführung … · Verteilung …", **vier Kennzahlkacheln** (Kapitalwert
gegenüber „ohne Speicher" samt Investition, Laufzeit und Zins; Bezugsspitze vorher → nachher
mit Peak-Ziel und Status; Stromrechnung mit Ersparnis; Vollzyklen je Einheit mit Lade- und
Entladeenergie), die **Vergleichstabelle mit drei Spalten** „Ohne Speicher · Mit Flotte · Δ",
die **Jahresprojektion als Bild** mit aufklappbarer Tabelle, die Kennzahlen je Einheit, die
Kandidaten der Rastersuche und zuletzt die Diagramme.

**Δ ist eine Fachaussage, keine Formatierung.** Δ = Mit − Ohne; ob ein negatives Δ eine
Verbesserung ist, entscheidet die Kennzahl (Kosten, Netzbezug, Spitze: weniger ist besser;
Einspeisung: mehr). Beides rechnet der Kern in `SpeicherFlottenAnzeigeCtrl.Vergleichszeilen`
(`FlottenVergleichszeile.Delta`, `.NegativIstBesser`, `.IstBesser`) — auf iOS gilt dieselbe
Regel, und eine zweite Fassung in der Oberfläche liefe irgendwann auseinander. Eine Zeile, in
der **beide** Seiten null sind (Einspeisung an einem Standort ohne Erzeugung), ist eine
**Nullzeile** und steht hinter einem Aufklapper: Drei Nullen sind keine Aussage.

**Der Betriebseditor steht nicht mehr im Ergebnis.** Er stand doppelt — bei den Eingaben und
noch einmal daneben. Das Ergebnis NENNT die berechnete Betriebsführung und meldet über den
Rückruf `BetriebsfuehrungAendern` nach außen, dass sie geändert werden soll; der
`SpeicherFlottenDialog` wechselt darauf auf seinen ersten Reiter, die freie Ansicht aus Paket
P3 später auf Schritt 3. Ohne Rückruf gibt es den Knopf nicht („kein Delegat ist kein Knopf").

**Die Diagramme.** Über jedem Bild dieselbe Steuerung: der Schalter **„sortiert"**
(Dauerlinie), **je Reihe ein Schalter** mit derselben Ressource wie die Legende, das Bild mit
**Datenzoom** (`BereichGewaehlt`/`Zurueckgesetzt`) — und dazu die **Zeitraumwahl Jahr / Woche /
Tag** mit Navigator im Ringschluss (Muster W8‑E‑2; Vorgabe ist Woche 1, denn ein Jahr im
Viertelstundenraster legt vierzig Werte auf einen Bildpunkt und zeigt keinen einzigen
Ladezyklus). Die Reihen gibt es **je Einheit** („Speicher A: Entladen + / Laden −") zusätzlich
zu „Speicher gesamt"; der **Ladezustand einer wählbaren Einheit steht als ZWEITE Achse** im
Netzbild (§ 5.3), das zweite Bild mit den Energiegrenzen bleibt wählbar. Jedes Bild führt
seinen eigenen Ausschnitt.

**Neugezeichnet wird ohne Datenbank und ohne zweiten Rechenlauf.**
`SpeicherFlottenAnzeigeCtrl.Bilder(ergebnis, startTag, tage, speicher, reihen, sortiert,
netzbereich, socbereich)` liest allein den gehaltenen Lauf und reicht `ladezustand`,
`sortiert` und `fenster` an den vorhandenen `ChartRenderer.Speicherbetrieb` durch — dessen
Signatur ist unverändert; sie konnte das alles längst, gerufen wurde es nicht (Befund 1.4 des
Konzepts). Die Reihenwahl folgt der Hausregel: `null` heißt ALLE, eine **leere Liste** heißt
KEINE. Gezeichnet wird nur bei einem wirklich geänderten Bildauftrag — der Schlüssel ist der
der Ergebnisseite (`SimulationErgebnisDaten.Bildauftrag.Schluessel`); wer schnell durch die
Wochen blättert, verwirft damit den vorigen Auftrag, statt eine Warteschlange aufzubauen
(Muster W11b‑B‑25).

**Die Jahresprojektion ist ein neues Renderer-Bild.**
`ChartRenderer.Jahresprojektion(titel, jahre, netto, kumuliert, ersatzjahre, weitere, yTitel,
xTitel)` zeichnet 1240 × 560: eine Säule je Projektjahr (negative Jahre rot), die kumulierte
Linie darüber, die Ersatzjahre als senkrechtes Band mit Dreieck, dazu die wählbaren Linien
Betrieb, Durchsatz und Ersatz. Der Schnittpunkt der kumulierten Linie mit der gestrichelten
Nulllinie ist die Amortisation. `Proben/ChartProben` prüft das Bild wie jedes andere auf Maße,
Farben und Determinismus und führt dazu eine **Gegenprobe**, die belegt, dass eine andere
Ersatzjahrmarke das Bild wirklich ändert — 39 Bilder und 7 Gegenproben, zusammen 46 Proben.

**Lokalisierung.** Alle Texte der fünf Flottenkomponenten und der Textbündel
(`Beschriftungen`, `SpeicherAuslegungTexte`, `SpeicherFlottenCsvTexte`) stehen als
`FLOTTE_*`-Ressourcen in `Resource.resx` und `Resource.en-US.resx`; die Reihennamen des
Betriebsbildes bleiben die vorhandenen `OPT_BETRIEB_R_*`, damit Legende und Schalter
denselben Text tragen. Die deutschen Werte sind wörtlich die bisherigen Literale — die
Umstellung beschriftet keine Maske anders.

**Was NICHT in diesem Paket steckt:** das Diagnosebanner der arbeitslosen Flotte (Konzept 2.2
Punkt 2 — die Zähler liefert P1, die Verdrahtung P3), die freie Ansicht samt Ablaufleiste (P3)
und die Größen-Sicht mit Rasterkarte und Schnittkurve (P4); die Kandidatentabelle steht
einstweilen unverändert da.

## Testbelege vom 11.09.2026

| Nachweis | Ergebnis | Beleg |
|---|---:|---|
| gesamtes `SpeicherEngine.Tests` | 382/382 bestanden | `.work/flotte_suite_final.log` |
| gesamtes `SpeicherPlanung.Tests` | 27 bestanden, 1 übersprungen | `.work/flotte_suite_final.log` |
| gesamtes `EPOS.UI.Tests` | 3.434/3.434 bestanden | `.work/flotte_ui_final_complete.log` |
| gesamtes `EPOS.Kern.Tests` | 2.400/2.400 bestanden | `.work/flotte_kern_abnahme.log` |

Die alten, datierten Gesamtzahlen bleiben unverändert im [Prüfnachweis Version 1.2](Doku_Speicherauslegung_Pruefnachweis.md).

## Ehrliche Grenzen

- **Volles Oracle-Jahr:** Der bisherige 35.040-Intervall-Benchmark mit Horizont 192 und Neuplanung 96 wurde nach mehr als 60 s abgebrochen und bleibt übersprungen. Laufzeit und Dialogtauglichkeit sind nicht bestätigt.
- **Rainflow:** Die Auswertung liefert Miner-Schaden innerhalb der gelieferten Kurve, aber keine automatische Kapazitäts-/Leistungsalterung, Kalenderalterung, Temperaturwirkung oder selbsttätigen Ersatz.
- **Windows-Solver:** Der planende Stand verwendet Google.OrTools 9.15.6755 und native SCIP-Laufzeit im Windows-Ziel. Nur `SpeicherEngine` selbst ist plattformneutral.
- **Optimalaussage:** MILP-Optimalität gilt für den einzelnen Horizont; rollierender Jahreslauf und endliches Größenraster sind nicht global optimal.
- **Herkunft und Tarife:** Im Speicher gibt es keine Herkunftsschichten. Monatspeaks, Tarifstaffeln, Steuern und mehrere Abrechnungsperioden sind nicht modelliert.
- **Mehrjahresalterung:** Tatsächliche Jahre und SoC-Mitnahme sind umgesetzt; Rainflow-getriebene Degradation, Ausfall und Reparatur müssen als spätere Zustandsmodelle beziehungsweise explizite Verfügbarkeit ergänzt werden.
- **Wärmekopplung:** Elektrische Zusatzlast und BHKW-Fahrplan sind Eingaben; eine gemeinsame Wärme-/Strom-MILP ist nicht enthalten.

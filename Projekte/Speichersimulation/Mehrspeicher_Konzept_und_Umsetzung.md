# Mehrspeicher: Konzept, Umsetzung und Prüfumfang

Stand: 11.09.2026

Dieses Dokument beschreibt den tatsächlich implementierten Mehrspeicherstand in EPOS-Plan. Grundlage sind Kapitel 4 bis 9 sowie 13 und 14 der [Spezifikation](C:/Users/DirkEngelmann/Documents/WP-Plan/Projekte/Speichersimulation/Spezifikation.md). Kapitel 14 und die ältere [Dokumentation zur Speicherauslegung](C:/Users/DirkEngelmann/Documents/WP-Plan/Doku_Speicherauslegung_Kosten_Zeitreihen.md) bleiben als datierter Nachweis der vorangegangenen Einzelspeicherauslegung erhalten.

Eine Flotte ist eine Liste physisch gleichzeitig betriebener AC-Speicher innerhalb **einer** Variante. Vergleichsvarianten werden nicht addiert. Jeder Speicher besitzt einen eigenen Zustand, Richtungsleistungen, Wirkungsgrade, SoC-Grenzen, Reserve, Hilfsverbrauch und Kosten. Der bestehende Einzelspeicherpfad wurde nicht ersetzt.

## Architektur und Konzept-zu-Code-Mapping

`EPOS.UI` bearbeitet Flotte, Ziele und Quellen. `EPOS.Kern` löst Projektquellen und Kosten auf, wandelt ct/kWh einmal in €/kWh um, bildet UTC-Snapshots und hält den freigegebenen Laufstand. `SpeicherEngine` rechnet UI- und DB-frei. `SpeicherPlanung` bindet Google OR-Tools/SCIP ausschließlich hinter `IFlottenPlaner` an. Eingabe und Konfiguration werden vor jedem Lauf tief kopiert.

| Konzept oder Garantie | Umsetzung |
|---|---|
| Serialisierbare Flotten-, Plan-, Ergebnis-, Wirtschafts- und Suchverträge | [`SpeicherEngine/FlottenModel.cs`](C:/Users/DirkEngelmann/Documents/WP-Plan/SpeicherEngine/FlottenModel.cs) |
| AC-Physik, reaktive Ziele, Verteilung, Snapshotwahl und tiefe Laufkopie | [`SpeicherEngine/FlottenSimulator.cs`](C:/Users/DirkEngelmann/Documents/WP-Plan/SpeicherEngine/FlottenSimulator.cs) |
| Jahreskonten, CAPEX, OPEX, Ersatz, Restwert und NPV | [`SpeicherEngine/FlottenWirtschaftlichkeit.cs`](C:/Users/DirkEngelmann/Documents/WP-Plan/SpeicherEngine/FlottenWirtschaftlichkeit.cs) |
| Rainflow-Zyklen und Miner-Schaden | [`SpeicherEngine/FlottenRainflow.cs`](C:/Users/DirkEngelmann/Documents/WP-Plan/SpeicherEngine/FlottenRainflow.cs) |
| Nullvariante und vollständige begrenzte Rastersuche | [`SpeicherEngine/FlottenOptimierer.cs`](C:/Users/DirkEngelmann/Documents/WP-Plan/SpeicherEngine/FlottenOptimierer.cs) |
| MILP mit Zeitlimit und unterscheidbaren Status | [`SpeicherPlanung/OrToolsFlottenPlaner.cs`](C:/Users/DirkEngelmann/Documents/WP-Plan/SpeicherPlanung/OrToolsFlottenPlaner.cs) |
| Projekt-/CSV-Adapter, Prognose- und Jahresimport | [`EPOS.Kern/Controller/SpeicherFlottenStudieCtrl.cs`](C:/Users/DirkEngelmann/Documents/WP-Plan/EPOS.Kern/Controller/SpeicherFlottenStudieCtrl.cs) |
| Profil, geprüfte Aktivierung und gewöhnlicher Projektlauf | [`EPOS.Kern/Controller/SpeicherAuslegungModel.cs`](C:/Users/DirkEngelmann/Documents/WP-Plan/EPOS.Kern/Controller/SpeicherAuslegungModel.cs), [`EPOS.Kern/Controller/SpeicherFlottenProjektCtrl.cs`](C:/Users/DirkEngelmann/Documents/WP-Plan/EPOS.Kern/Controller/SpeicherFlottenProjektCtrl.cs) |
| Flotteneditor, Quellenimport, Ergebnis und CSV | [`EPOS.UI/Dialoge/Strom/SpeicherFlottenDialog.razor`](C:/Users/DirkEngelmann/Documents/WP-Plan/EPOS.UI/Dialoge/Strom/SpeicherFlottenDialog.razor), [`EPOS.UI/Dialoge/Strom/SpeicherFlottenEditor.razor`](C:/Users/DirkEngelmann/Documents/WP-Plan/EPOS.UI/Dialoge/Strom/SpeicherFlottenEditor.razor) |

Ein freigegebener Kandidat kann als `@Projektflotte` aktiviert und im gewöhnlichen Projektlauf gefahren oder wieder deaktiviert werden. Die Übernahme prüft Zulässigkeit, vollständige Zeitreihen, Daten-ID, Kandidaten-ID und beste Konfiguration. Die ID enthält je Einheit ID, Kapazität, Lade- und Entladeleistung. Die Nullvariante ist keine aktivierbare physische Flotte. Neue Einheiten ohne belastbare Kostensätze werden nicht als Nullkostenanlage angenommen.

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

Ein Referenzjahr wird nur bei `ReferenzjahrExplizitWiederholen` vervielfacht und entsprechend gekennzeichnet; Ersatz wird im fälligen Jahr neu gebucht. Ohne Endenergiegleichheit ist ein expliziter Ausgleichswert Pflicht, damit Anfangsenergie kein kostenloser Ertrag wird.

## Nullvariante, Suchraum, Speichergrenze und Abbruch

Eine Suchachse ersetzt über `ErsetztEinheitId` genau ihre Einheit; andere bleiben fest. Anzahl kann auch null sein. Möglich sind Kapazität plus Leistung, Kapazität plus C-Rate (`P=E·C`) oder Leistung plus C-Rate (`E=P/C`). Beim Skalieren bleibt das Lade-/Entladeleistungsverhältnis der Vorlage erhalten; eine Nullrichtung bleibt null.

Das vollständige endliche kartesische Raster wird mit den ausgewählten Zielen kombiniert. Übersteigt es `MaximaleKandidaten`, wird es abgewiesen und nicht gekürzt. Rangfolge: technisch zulässige Varianten nach höchstem NPV, daneben die technisch zulässige Nullvariante mit NPV 0. Ist die Referenz wegen einer harten Netzgrenze unzulässig, kann eine technisch nötige Variante trotz negativem NPV gewinnen. Sind alle Rechnungen fachlich ungültig, entsteht ein Konfigurationsfehler statt einer falschen Null-Empfehlung.

`CancellationToken` wirkt in Kandidaten-, Jahres-, Intervall- und Solverlauf; Fortschritt meldet Anzahl und Kandidaten-ID. Alle Kandidaten halten nur Zusammenfassungen, die vollständige Reihe nur der beste Kandidat beziehungsweise die Nullvariante.

## CSV-Quellwahl und JSON-Eingaben

| Rolle | Quelle | Flottenfeld |
|---|---|---|
| Bruttolast | EPOS oder CSV | `LastKw` |
| verfügbare PV-AC-Leistung | EPOS, CSV oder keine | `PvKw` |
| Bezugspreis | EPOS, Preisprofil oder CSV | `BezugspreisEuroProKWh` |
| BHKW und PV-/BHKW-Vergütung | vorbereiteter Projektlauf | `BhkwKw`, getrennte Verkaufspreise |
| Batterieverkauf | Flottentarif | eigener Verkaufspreis |

Der bestehende CSV-Dialog normalisiert 15- oder 60-Minuten-Reihen auf Viertelstunden und behandelt Zeit-/Wertspalten, Zeitzone, Intervalllage, Kodierung und Einheiten. Lücken, doppelte UTC-Stempel und nicht endliche Werte werden abgewiesen; negative Preise sind erlaubt. Eine CSV-Achse bleibt erhalten. Mischung mit EPOS-Modellwerten verlangt eine explizite Kalenderzuordnung. Details: [Datenquellen und CSV-Import](C:/Users/DirkEngelmann/Documents/WP-Plan/Doku_Speicherauslegung_Kosten_Zeitreihen.md#datenquellen-und-csv-import).

Die folgenden kompakten Draft-2020-12-Schemas dokumentieren die beiden manuellen JSON-Importe. Enums werden als Namen gelesen. `row` steht für ein Objekt mit den erforderlichen Feldern `Zeitstempel` (date-time), `LastKw`, `PvKw`, `BhkwKw` (Zahlen ≥ 0) sowie den vier endlichen Preisen `BezugspreisEuroProKWh`, `PvVerkaufspreisEuroProKWh`, `BhkwVerkaufspreisEuroProKWh`, `BatterieVerkaufspreisEuroProKWh`.

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

## Testbelege vom 11.09.2026

| Nachweis | Ergebnis |
|---|---|
| `SpeicherEngine.Tests`, gefilterte `Flotten*`-Tests einschließlich asymmetrischer Kandidaten-ID | 22/22 nach der ID-Ergänzung |
| gesamtes `SpeicherEngine.Tests` | 379/379 vor der abschließend gezielt geprüften ID-Ergänzung |
| fokussierte MILP-Planertests | 14/14 |
| Integration aller fünf Ziele, zwei heterogene Speicher, negative Preise, Tarife, Richtung, Endenergie und Snapshotwahl | 12/12 |
| Oracle-Smoke: 2.688 Intervalle, Horizont 192, Neuplanung 96 | bestanden, etwa 8 s |
| reaktives Jahr mit 35.040 Intervallen | bestanden, etwa 0,4 s |
| gefilterte EPOS-Mehrjahresadaptertests | 3/3 |

Die alten, datierten Gesamtzahlen bleiben unverändert im [Prüfnachweis Version 1.2](C:/Users/DirkEngelmann/Documents/WP-Plan/Doku_Speicherauslegung_Pruefnachweis.md).

## Ehrliche Grenzen

- **Volles Oracle-Jahr:** Der bisherige 35.040-Intervall-Benchmark mit Horizont 192 und Neuplanung 96 wurde nach mehr als 60 s abgebrochen und bleibt übersprungen. Laufzeit und Dialogtauglichkeit sind nicht bestätigt.
- **Rainflow:** Die Auswertung liefert Miner-Schaden innerhalb der gelieferten Kurve, aber keine automatische Kapazitäts-/Leistungsalterung, Kalenderalterung, Temperaturwirkung oder selbsttätigen Ersatz.
- **Windows-Solver:** Der planende Stand verwendet Google.OrTools 9.15.6755 und native SCIP-Laufzeit im Windows-Ziel. Nur `SpeicherEngine` selbst ist plattformneutral.
- **Optimalaussage:** MILP-Optimalität gilt für den einzelnen Horizont; rollierender Jahreslauf und endliches Größenraster sind nicht global optimal.
- **Herkunft und Tarife:** Im Speicher gibt es keine Herkunftsschichten. Monatspeaks, Tarifstaffeln, Steuern und mehrere Abrechnungsperioden sind nicht modelliert.
- **Mehrjahresalterung:** Tatsächliche Jahre und SoC-Mitnahme sind umgesetzt; Rainflow-getriebene Degradation, Ausfall und Reparatur müssen als spätere Zustandsmodelle beziehungsweise explizite Verfügbarkeit ergänzt werden.
- **Wärmekopplung:** Elektrische Zusatzlast und BHKW-Fahrplan sind Eingaben; eine gemeinsame Wärme-/Strom-MILP ist nicht enthalten.

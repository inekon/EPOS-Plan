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

Im Reiter „Stromspeicher“ zeigt die Karte **Speicherflotte und Auslegung** die aktuellen Einheiten, Kapazitäten und Richtungsleistungen. Betriebsziel, Leistungsverteilung und Erzeuger-Reihenfolge sind hier und im Auslegungsdialog über denselben Baustein editierbar. Bedingte Felder für Peak-Ziel, Netzladung und Batterieexport gehören zu diesem gemeinsamen Eingabestand. Die früheren Felder „Graustrom“ und „Dauernutzung“ werden beim Flottenbetrieb durch diese Steuerung ersetzt. Die Leistungsverteilung erscheint auch hier erst ab zwei Einheiten (Anwenderentscheid 11.09.2026 „Empfehlung", Auftrag #213, zweiter Wirt von SD‑E‑8) — bis dahin zeigte nur der Auslegungsdialog diesen Schalter, der Reiter die Klappliste unbedingt.

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

## Diagnose und Peak-Ziel (P1, #183)

Anlass ist der Befund **SP‑O‑10** vom 11.09.2026: „Mit Flotte" war byte-gleich „Ohne Speicher", weil die Flotte im ganzen Jahr weder geladen noch entladen hatte — und niemand sagte es. Drei Sperren wirkten zusammen: das fest vorbelegte Peak-Ziel 50 kW bei einer Spitze von 789 kW, das Netzladeverbot an einem Standort ohne Überschuss und ein Start-Ladezustand auf dem SoC-Minimum. Umgesetzt sind die Anwenderentscheide **SD‑Q3**, **SD‑Q4** und **SD‑Q5** (11.09.2026, je „Empfehlung"); Grundlage ist Abschnitt 2.4 des [Konzepts der Stromspeicher-Dialoge](Projekte/Konzept_Stromspeicher_Dialoge_EPOS-Plan.md). **Kein Rechenwert ändert sich**: Der Referenzlauf bleibt 13/13 byte-gleich gegen `2026-09-11_R7_Speicherflotte`.

**Die Diagnose** (`FlottenDiagnose` in [`SpeicherEngine/FlottenModel.cs`](SpeicherEngine/FlottenModel.cs), gefüllt in [`SpeicherEngine/FlottenSimulator.cs`](SpeicherEngine/FlottenSimulator.cs)) hängt am Laufergebnis `FlottenSimulationErgebnis.Diagnose` und damit an der `Variante` der Studie. Sie zählt je Intervall mit und wird nur für den Lauf **mit** Flotte geführt; der Referenzlauf ohne Speicher trägt eine leere Diagnose. Sie steht nicht im Referenzexport.

| Zähler | Bedeutung |
|---|---|
| `IntervalleLastUeberPeakZiel` | Intervalle mit `n > H`. Anteil 1 heißt: Die Last fiel nie unter das Ziel |
| `IntervalleMitEntladeanforderung` / `…MitLadeanforderung` | Intervalle, in denen die Betriebsführung überhaupt etwas verlangt hat |
| `IntervalleLadedeckelNullPeakregel` | `max(0, H − n)` war 0 — Wiederaufladung hätte einen neuen Peak über `H` erzeugt (Spezifikation 5.1) |
| `IntervalleLadedeckelNullNetzladeverbot` | `max(0, −n)` war 0 — ohne `NetzladungErlaubt` gibt es nichts zu laden |
| `IntervalleEntladeanforderungOhneEnergie` | Entladeanforderung an eine Flotte ohne abgebbare Energie |
| `LadeenergieAcKWh` / `EntladeenergieAcKWh`, `Arbeitslos` | beide 0 ⇒ arbeitslose Flotte |

Dieselben Zähler stehen je Einheit in `FlottenEinheitDiagnose`. Daraus bildet der Simulator die Liste `Gruende` — `FlottenDiagnoseBefund` mit sprachneutraler Aufzählung `FlottenDiagnoseGrund`, Intervallzahl und Anteil; ein Grund ohne betroffene Intervalle steht nicht darin. Der Anzeigetext gehört in die Oberfläche, nicht in die Rechenbibliothek.

**Die Vorbelegung des Peak-Ziels** (`FlottenPeakZiel.Vorschlag`, [`EPOS.Kern/Controller/FlottenPeakZiel.cs`](EPOS.Kern/Controller/FlottenPeakZiel.cs)) ersetzt die feste 50 kW:

```
H0 = max( Referenzspitze − Summe Entladeleistung ;  max der Tagesminima der Nettolast )
n  = Last − PV − BHKW + Hilfsverbrauch der Flotte
```

Der erste Term sagt, wie tief die Flotte überhaupt kappen kann; der zweite verhindert ein Ziel, unter das die Last nie fällt — sonst ist Wiederaufladung nach der Regel aus Spezifikation 5.1 ausgeschlossen. Jeder Vorschlag trägt eine Herleitungszeile im Klartext. Liegt keine Zeitreihe vor, gilt ein **benannter** Rückfall: mit bekannter Bezugsspitze `max(Spitze − Σ Entladeleistung; GrundlastAnteilImRueckfall · Spitze)`, ohne sie die Konstante `RueckfallPeakZielKw`. Gespeicherte Stände werden nie überschrieben — die Vorbelegung greift ausschließlich beim Anlegen einer neuen Flotte (`SpeicherFlottenStudieCtrl.BetriebsvorgabenSetzen`).

**Die Vorbelegung der EINHEITEN: eine je Speicheranlage des Projekts** (Anwenderbefund **#210**, 11.09.2026). `SpeicherFlottenStudieCtrl.Vorbelegung` baute eine neu angelegte Flotte bis dahin aus `StromspeicherSimCtrl.LeseParameter(projektId)` — EIN Parametersatz, nämlich der der AKTIVEN Variante (AP9b), im Rückfall die kapazitätsgewichtete Summe über alle `SP_TYP`-Anlagen — und daraus wurde genau EINE Einheit. Ein Projekt mit zwei Speichern bekam so eine Flotte mit einer Einheit, und weil der Projektlauf den gespeicherten Stand rechnet, fehlte die zweite danach überall: im Eingabestand `@Aktuell`, in „Kennzahlen je Speicher" und in der Reihenwahl der Diagramme. Seit #210 liefert `StromspeicherSimCtrl.Speicheranlagen(projektId)` die `SP_TYP`-Anlagenzeilen in Anlagenreihenfolge, und die Vorbelegung liest je Zeile über `LeseParameter(projektId, anlageId)` **ihren eigenen** Satz — Gerätedaten aus der Anlage, SoC-Band und Betriebsführung aus DEREN Variantenzeile. Jede Einheit trägt Anlagennamen und `AnlageId`; der Stromspeicher-Reiter nennt diese Herkunft je Zeile. Die Referenzliste `REF_SP_TYP` bleibt draußen (Vergleichsfall, keine gleichzeitig betriebene Anlage), und wie beim Peak-Ziel gilt: **Gespeicherte Stände werden nie überschrieben** — die Vorbelegung greift ausschließlich beim Anlegen. Fachliche Kante: Das Schema trennt eine parallel betriebene Anlage nicht von einer Vergleichs-Alternative; gewählt ist deshalb der SICHTBARE Fehler — eine Einheit zu viel nimmt der Anwender im Editor heraus, eine Einheit zu wenig erfährt er nirgends.

**„Peak-Ziel bestimmen"** (`FlottenPeakZiel.PeakZielBestimmen`) beantwortet die Frage, die der Anwender an eine Lastspitzenkappung stellt: Wie tief komme ich mit dieser Flotte? Bisektion zwischen der Grundlast (Maximum der Tagesminima) und der Referenzspitze, gesucht ist das kleinste `H`, bei dem die verbleibende Bezugsspitze `≤ H` bleibt. Höchstens `HoechsteLaeufe` = 12 Jahresläufe, je Lauf eine Meldung über `IProgress<FlottenPeakZielFortschritt>`, Abbruch über `CancellationToken`. Gerechnet wird ausdrücklich mit `PeakShaving`; **planende Ziele werden benannt abgewiesen**, weil sie je Lauf den MILP-Planer bräuchten (SP‑O‑1).

**Die Vorprüfung** (`FlottenPlausibilitaet.Pruefe`, [`EPOS.Kern/Controller/FlottenPlausibilitaet.cs`](EPOS.Kern/Controller/FlottenPlausibilitaet.cs)) läuft in `SpeicherFlottenStudieCtrl.Rechnen` vor der Rechnung und nach der Rechnung noch einmal mit der Diagnose. Sie liefert Hinweise mit Stufe (`Warnung`/`Hinweis`) und sprachneutraler Kennung; sie landen in der bestehenden Hinweisliste **und** zusätzlich strukturiert in `SpeicherFlottenErgebnis.Pruefhinweise`, an denen die Oberfläche ihre Abhilfeknöpfe aufhängt (Paket P3).

| Kennung | Anlass |
|---|---|
| `PeakZielUnterTagesminimum` | Peak-Ziel unter dem Maximum der Tagesminima bei `NetzladungErlaubt = false` (Warnung) |
| `PeakZielUeberReferenzspitze` | Peak-Ziel über der Referenzspitze — die Kappung bleibt wirkungslos |
| `BetriebskostenSehrNiedrig` | jährlicher Betriebsaufwand unter 0,1 % der Investition (Befund „Betrieb 1,00 €/a" bei 15.000 € Investition) |
| `StartSoCAufMinimum` | Start-Ladezustand = SoC-Minimum bei Lastspitzenkappung, oder die Diagnose meldet eine arbeitslose Flotte (SD‑Q4) |
| `FlotteArbeitslos` | die Diagnose meldet `Arbeitslos`; der Text nennt die gezählten Gründe |

**Vorgabe „Netzladung erlaubt" je Betriebsziel** (`FlottenVorgaben.NetzladungFuer`, SD‑Q5): Lastspitzenkappung bekommt die Freigabe — Spezifikation 5.1 sagt „Die Wiederaufladung nutzt freie Anschlussleistung unter H", und genau das **ist** Netzladung; PV-Eigenverbrauch und die planenden Ziele bekommen sie nicht. Die Vorgabe wirkt **nur** beim Anlegen einer neuen Konfiguration. Die serialisierte Vorgabe in `FlottenSimulationOptionen.NetzladungErlaubt` bleibt `false`, damit gespeicherte Stände unverändert lesen. Der Stand `@Projektflotte` des Prüfprojekts **1046** trägt die Eigenschaft ausdrücklich (`NetzladungErlaubt = true`, geprüft am 11.09.2026 gegen `Referenzlaeufe/Kenndaten_Test.sqlite`) und ist vom Vorgabenwechsel damit nicht berührt.

**Der Start-Ladezustand bleibt beim Produktivstandard SoC-Minimum** (AP0, SD‑Q4). Geändert wird nichts; ein voller Start würde den Januar-Peak schöner rechnen, als er im Betrieb wäre. Sichtbar gemacht wird die Lage nur über den Hinweis `StartSoCAufMinimum`.

## Adaptive Entladeschwelle — die kausale Ratsche (P7, #215)

Anlass ist der Anwenderbefund vom 11.09.2026: Bei FESTEM Peak-Ziel entlädt die Flotte auch
dann noch bei jeder kleineren Spitze, wenn die Jahresspitze schon verfehlt ist — der Speicher
steht dann leer, wenn die große Spitze kommt (Bildschirmfoto: Ziel 200 kW, Januarwoche sauber
gekappt, Spitze 523 kW ungekappt). Der Anwender hat das kausale Gegenmodell als Excel-Makro
`calc_peakshaving` vorgelegt. Umgesetzt sind die Anwenderentscheide **PS‑Q1 bis PS‑Q4**
(11.09.2026, je „Empfehlung“); Fachgrundlage ist Abschnitt **5.1.1** der
[Spezifikation](Projekte/Spezifikation_Stromspeicher_Optimierung.md).

**Die Regel R.** Die Schwelle `H` ist kein Parameter, sondern ein Zustand des Laufs, der nur
steigen kann:

```
H  <- H0                                  Startwert; Vorgabe: die Grundlast (max der Tagesminima)
je Intervall t:
    D_t <- Summe_j Grenzen(einheit_j, energie_j, release = true).Discharge
    wenn N_t - D_t > H:   H <- N_t - D_t   Spitze nicht haltbar -> Schwelle nachziehen
    danach unveraendert die Regel aus 5.1, aber mit DIESEM H
```

`D_t` ist **genau** die Entladegrenze der Ausführung (`FlottenSimulator.Grenzen`: Leistung mal
Verfügbarkeit, nutzbare Energie über der SoC-Untergrenze, Entladewirkungsgrad). Die
Peak-Reserve ist bei `N > H` definitionsgemäß freigegeben und geht deshalb mit `release = true`
ein. Die Ratsche läuft nur für die zwei Betriebsziele MIT Peak-Ziel (`PeakShaving`,
`MultiUse`) und nur im Lauf MIT Flotte — ohne Einheiten wäre `D_t` stets 0, und die Schwelle
zöge stur auf die Spitze der Referenz.

| Feld | Ort | Bedeutung |
|---|---|---|
| `PeakZielAdaptiv` | `FlottenSimulationOptionen` | Ratsche an oder aus; **serialisierte Vorgabe `false`** |
| `WirtschaftlicherPeakZielwertKw` | `FlottenSimulationOptionen` | bei der Ratsche der **Startwert H₀** |
| `PeakZielKw` | `FlottenIntervallErgebnis` | die geltende Schwelle je Intervall — die **Treppe** |
| `ErreichtesPeakZielKw` | `FlottenSimulationErgebnis` | **H_end**, die kausal erreichte Schwelle; `null` bei festem Ziel |
| `IntervalleSchwelleNachgezogen` | `FlottenDiagnose` | Zahl der **Nachzüge** (`N - D > H`) |

**Die Vorgabe gilt nur für NEUE Stände** — dasselbe Muster wie die Netzladung aus #183:
`FlottenVorgaben.PeakZielAdaptivFuer(ziel)` ist `true` für `PeakShaving` und `MultiUse`, die
serialisierte Vorgabe der Eigenschaft bleibt `false`. Ein gespeicherter Stand — insbesondere
`@Projektflotte` des Prüfprojekts **1046** — liest sich damit als `fest` und rechnet
unverändert; **1030 und 1046 sind byte-gleich gegen `2026-09-11_R7_Speicherflotte`**. Bei
`PeakZielAdaptiv = false` liest der Simulator denselben `double` wie zuvor: Der Rechenweg ist
bit-identisch.

**Die zwei Schwellen nebeneinander** (PS‑Q2). `FlottenPeakZiel.PeakZielBestimmen` bleibt, rechnet
aber ausdrücklich mit `PeakZielAdaptiv = false` auf einer Kopie und heißt in der Anzeige
„mit Vorausschau erreichbar“ (M*). Es gilt **M* ≤ H_end**; die Differenz ist
der **Wert der Vorausschau** und entscheidet, ob sich S‑D (Kurzfristprognose) lohnt. Die
Ergebnisansicht zeigt beide Zeilen, das Netzbild zeichnet `H` als Treppe
(`SpeicherFlottenAnzeigeCtrl`, Reihe „Peak-Ziel“ aus der Ganglinie statt aus
der Konfiguration).

**Der Prüfstand ist der Port des Excel-Makros.** `SpeicherEngine.Tests/FlottenPeakRatscheTests`
rechnet die Referenzregel Zeile für Zeile nach und hält sie gegen den Simulator — auf einem
**synthetischen** Viertelstundenlastgang (der Kundenlastgang bleibt außerhalb des
Repositoriums): sieben Tage, Grundlast 60 kW, fünf Tagesspitzen 250…400 kW, am sechsten
Abend ein 400-kW-Block, der ohne Ladepause in eine Spitze von 740 kW übergeht; eine Einheit
400 kW / 400 kWh, η = 1, SoC 0…100 %, Start leer.

| Fall | Jahresspitze | Bemerkung |
|---|---:|---|
| fest, H = 60 kW (Grundlast) | 740,0 kW | Befund SP‑O‑10 in Reinform: Ladedeckel dauerhaft 0, Flotte arbeitslos |
| **adaptiv, H₀ = 60 kW** | **540,0 kW** | H-Treppe 60 → 250 → 340 → 540 kW, drei Nachzüge |
| M* (Bisektion) | 340,0 kW | mit Vorausschau erreichbar; Wert der Vorausschau 200 kW |
| adaptiv, H₀ = M* | 340,0 kW | null Nachzüge — wortgleich zum festen Ziel (S‑C) |

**Nicht Teil dieses Pakets** (PS‑Q3/PS‑Q4, Spezifikation 5.1.1): kein Monatstarif (S‑B), keine
Kurzfristprognose (S‑D), kein Sicherheitsaufschlag (S‑E), keine stille Netzladung für
bestehende Stände. Die Diagnose benennt die Sperre wie bisher.

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

## Projektlauf ohne Kostensätze (#185)

**Der Befund (Anwender, 11.09.2026).** Ein Projektlauf mit aktivierter Speicherflotte brach
vollständig ab: `System.InvalidOperationException: "Die Speicherflotte für diesen Projektlauf ist
ungültig oder konnte nicht geplant werden: Im Dialog fehlen die Investitionskoeffizienten."`
Die Ursache steht in `SpeicherAuslegungCtrl.KostenAufloesen`: Sie verlangte Investitions- **und**
Betriebskoeffizienten bedingungslos, sobald `Investitionsquelle == Dialog` beziehungsweise
`Betriebsquelle == Dialog` und `DirekteKosten.InvestVorhanden`/`BetriebVorhanden` nicht gesetzt
waren. Genau diesen Stand legt `SpeicherAuslegungCtrl.Vorbelegung` an, wenn das Speichergerät
keine Kostendaten trägt (`InvestVorhanden = CPow > 0 || CCap > 0`, `BetriebVorhanden = false`);
er wandert über „Einstellungen speichern" als `@Aktuell` in `Tab_SpeicherAuslegung` und von dort
als Lauf-Snapshot in jeden Projektlauf.

**Warum das falsch war — zwei Gründe, beide messbar.**

1. **Der Projektlauf braucht die Sätze nicht.** Sie erreichen ausschließlich
   `FlottenWirtschaftlichkeit` (`Investition`, `ErzeugeJahreskonto`); `FlottenSimulator.SimuliereKern`
   liest keinen einzigen von ihnen. Netzleistung, Ladezustand, Lade- und Entladeenergie sind ohne
   sie dieselben. Im Projektlauf entstehen zudem gar keine Jahreskonten
   (`FlottenStudieKonfiguration.Wirtschaftlichkeit.Jahreskonten` ist leer), der Kapitalwert wird
   dort also ohnehin nicht gebildet.
2. **Einheiten mit eigenen Kosten brauchen sie auch im Studienlauf nicht.**
   `SpeicherFlottenStudieCtrl.Konfiguration` überspringt jede Einheit mit `EigeneKosten` und
   überschreibt nur die übrigen mit den aufgelösten Sätzen. Trägt jede Einheit eigene Kosten,
   wurde eine Pflichtangabe verlangt, die anschließend nirgends ankommt.

**Die Behebung, drei Ebenen.**

| Ebene | Was sich ändert |
|---|---|
| `SpeicherAuslegungCtrl` | `Vorbereiten`/`AusQuellenVorbereiten` nehmen eine `KostenPflicht` (`Studienlauf` = Vorgabe, `Projektlauf`). `SpezifischeSaetzeGebraucht` beantwortet, ob überhaupt eine Einheit die Sätze braucht — ohne Flotte (Einzelanlage) und bei jeder Einheit ohne `EigeneKosten` lautet die Antwort ja. Fehlen sie und werden sie nicht gebraucht: Sätze 0 mit der Herkunft „je Einheit (eigene Kosten)". Fehlen sie und werden gebraucht: im Studienlauf weiterhin eine Ausnahme, deren Text den Ausweg nennt; im Projektlauf Sätze 0 mit `SpeicherKostensaetze.NichtBewertbar = true`. `StandKosten` löst die Sätze eines gespeicherten Standes auf, ohne eine Zeitreihe zu beschaffen |
| `SpeicherFlottenProjektCtrl` | `Aktivieren` schreibt `@Projektflotte` seither mit aufgelösten `VerwendeteKosten` (bereits aufgelöste, brauchbare Sätze bleiben eingefroren). Neu ist die Vorprüfung `Pruefe(projektId)` → `FlottenProjektPruefung` mit **Problemen** (Stand fehlt, nicht aktiviert, keine Einheit, unzulässige Projektquelle, planendes Ziel ohne Fahrplan-Löser) und **Hinweisen** (fehlende Kostensätze). Beide `Rechnen`-Wege rufen sie und werfen — wenn überhaupt — mit der **vollständigen Liste** und dem Ausweg statt mit dem jeweils ersten Problem. `SpeicherFlottenProjektLauf.KostenBewertbar` trägt die Bewertbarkeit, `Hinweis` den Klartext |
| `SimulationControl.Stromspeicher` | Der Abbruch **bleibt** — ein Projekt mit aktivierter Flotte darf nicht still ohne sie rechnen. Neu ist allein, dass Warnung und Ausnahme aus `MyResource.Resource.FLOTTE_MSG_LAUF_WARNUNG`/`…_GESCHEITERT` kommen und der Ausnahmetext den Ausweg nennt („die Flotte im Auslegungsdialog deaktivieren oder die Eingaben vervollständigen"). Der Protokolleintrag steht weiterhin **vor** dem Wurf |

**Wie die Ausnahme den Anwender erreicht.** `Do_Simulation` fängt sie nicht; sie verlässt
`SimulationLaufCtrl.Laufen` und damit das `Task.Run` der Windows-Hülle, die sie in
`SimulationErgebnisHuelle.Laufen` (`:889`) als `Rueckmeldung(false, ex.Message)` auf der Ergebnisseite
anzeigt. Sie schlägt also **nicht** als unbehandelte Ausnahme durch; ein zusätzlicher Fang an der
Naht des Laufs wäre eine zweite Fehlerpolitik neben `Abbruchgrund` und unterbliebe deshalb.
Im plattformfreien `EPOS.Referenzlauf` verlässt sie `SimulationRunner.Ausfuehren` — dort ist der
harte Abbruch eines Stapellaufs gewollt.

**Was gleich bleibt.** Der Referenzlauf ist byte-gleich: Projekt 1046 trägt gepflegte Sätze
(`DirekteKosten` mit `InvestVorhanden`/`BetriebVorhanden`), damit sind `investFehlt` und
`betriebFehlt` beide falsch, und `KostenAufloesen` liefert Werte, Flags und Herkunftstext
unverändert. 13/13 Projekte byte-gleich gegen `Referenzlaeufe/2026-09-11_R7_Speicherflotte`,
1046 weiterhin mit 42 `Flotte.*`-Skalaren.

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

## Ansicht Stromspeicher-Auslegung (P3, #192)

Seit dem 11.09.2026 ist die Speicherauslegung eine **freie Ansicht der `AppWurzel`**
(`Seitenschluessel.StromspeicherAuslegung` = `STROMSPEICHER_AUSLEGUNG`,
`EPOS.UI/Seiten/Strom/StromspeicherAuslegungSeite.razor`) und kein modaler Dialog mehr. Damit
sind die zwei letzten Speicher-Dialoge **gefallen**: `SpeicherFlottenDialog.razor` und
`SpeicherOptimierungDialog.razor` sind gelöscht, ihre Prüfstände an der Seite fortgeschrieben.
Die Zielbilder stehen in
[`Projekte/Konzept_Stromspeicher_Dialoge_EPOS-Plan.md`](Projekte/Konzept_Stromspeicher_Dialoge_EPOS-Plan.md)
Abschnitt 1.1, 1.6, 2.1, 2.2 Punkt 2 und 2.4.

**Ein Faden statt zweier Reitersätze.** Über allem steht die **Ablaufleiste**
(`Ablaufleiste.razor`) mit fünf Stationen — **1 Speicher · 2 Daten & Kosten ·
3 Betriebsführung · 4 Berechnen · 5 Ergebnis**. Station 4 ist der Rechenknopf, die übrigen
sind Blätter. Die Blätter sind `SpeicherFlottenEditor`, `SpeicherAuslegungEditor` und
`SpeicherFlottenBetriebEditor` — **keine der drei Bestandskomponenten wurde kopiert**; der
`SpeicherFlottenEditor` bekam allein den Schalter `BetriebZeigen`, damit die Betriebsführung
nur auf Station 3 erscheint. Eine Station, die noch nicht bedienbar ist, bleibt ein
`<button>` mit `aria-disabled` und Grund im `title` (schwache Sperre, W16b‑E‑6) — nie ein
`disabled` ohne Begründung.

**Ein Weg statt zweier Modi (SD‑E‑8, #206, 11.09.2026).** Bis dahin stand rechts in der
Leiste ein **Modusschalter Flotte / Einzelspeicher** (SD‑Q1), und der Einzelspeicher fuhr mit
drei eigenen Blättern eine eigene Rastersuche. Der Anwender hat ihn zurückgegeben: „Es ist
nicht sinnvoll, einen Unterschied zwischen Einzelspeicher und Flotte zu machen." Seither
rechnet die Ansicht **immer die Flotte**; ein Einzelspeicher ist eine Flotte mit genau EINER
Einheit — so bildet die Spezifikation (1.2, Kapitel 11) eine vorhandene Anlage ohnehin ab, und
`SpeicherFlottenStudieCtrl.Vorbelegung` legt seit #210 je Speicheranlage des Projekts eine
Einheit an; ein Projekt mit EINER Anlage bekommt damit genau eine. Die fünf
Betriebsziele, das Peak-Ziel, die Diagnose und die Größen-Sicht gelten für jede Einheitenzahl;
die **Verteilung erscheint erst ab zwei Einheiten** (`SpeicherFlottenBetriebEditor.
VerteilungZeigen`, ausgeblendet statt gesperrt, mit einer Erklärzeile — bei einer Einheit gibt
es nichts zu verteilen). Gefallen sind `AuslegungModus`, der `Modusknopf` der Ablaufleiste,
die drei Blätter `EinzelspeicherSuchraum`/`-Betrieb`/`-Ergebnis` und der Suchraum-Teil des
`SpeicherAuslegungEditor`. **Zwei Dinge des Einzelwegs blieben**, weil der PROJEKTLAUF weiter
zwei Pfade führt (SD‑Q2): das **Rückschreiben in die Projektanlage** in Schritt 5 — ein Knopf
mit Rückfrage für die eine Einheit mit Anlagenbezug, ohne den die ausgelegte Größe beim
klassischen Projektlauf nie ankäme — und der **Leistungspreis** als EINE Eingabe in Schritt 2
(`LeistungspreisBlock`; derselbe Wert für `FlottenTarif.LeistungspreisEuroProKw`, den Suchraum
und die Projektvariante). Der Einzelspeicher-**Optimierer** selbst ist nicht gelöscht: Er
trägt das Betriebsbild des Berichts (`SpeicherBetriebsbild`), die Vorbelegung in
`SpeicherAuslegungCtrl` und die KI-Aktion `speicher_optimieren`.

**Der Weg dorthin und zurück.** Der Reiter „Stromspeicher" der Ergebnisseite **wechselt die
Ansicht**, statt eine Überlagerung aufzuziehen (Muster W16c‑E‑3). Die Ergebnisseite selbst
trägt dafür nur noch `AuslegungOeffnen`; ihre acht Auslegungsdelegaten sind entfallen. Den
Rückweg führt `AppWurzel` nach dem Muster aus #62b: Die Wurzel merkt sich die aufrufende
Ansicht, fragt beim Verlassen über `FrageVerlassen`/`AssistentVerlassen` nach ungespeicherten
Eingaben und kehrt danach dorthin zurück.

**Woher die Seite ihren Simulationslauf bekommt.** Jede EPOS-Zeitreihe stammt aus genau einem
abgeschlossenen Lauf. Die Windows-Hülle der Ergebnisseite reicht deshalb **ihren** Lauf an den
Kern-Controller weiter (`StromspeicherAuslegungHuelle.Anmelden`), bevor sie die Ansicht
öffnet; liegt keine Anmeldung vor — etwa beim Aufruf ohne vorherige Simulation —, baut
`AnsichtGaben()` einen Controller **ohne** Lauf, und die Seite bietet den eigenen
Simulationslauf an (Muster iU9‑W11a). Zwei Läufe nebeneinander wären zwei Wahrheiten.

**Die Datenseite liegt im Kern.** `EPOS.Kern/Controller/StromspeicherAuslegungCtrl.cs` ist
eine **Instanz** je Projekt und führt, was vorher als Delegatenbündel in der WinForms-Hülle
stand: Vorgaben lesen, Einstellungen und Profile speichern (ein Name mit `@` ist für interne
Stände reserviert), die Flotte vorbereiten und rechnen, die Größe einer Einheit in die
Projektanlage übernehmen, Leistungspreis schreiben, Projektflotte aktivieren und deaktivieren,
Vorprüfung, Peak-Ziel-Vorschlag und Peak-Ziel-Bestimmung sowie den eigenen Simulationslauf.
Der Einzelweg (`EinzelVorbereiten`, `EinzelRechnen`, `Betriebsbild`, `RasterCsv`) ist mit #206
entfallen — samt dem gemerkten rohen Raster, an dem das Nachzeichnen des Betriebsbildes hing.
Die Hülle behält nur, was die **Plattform** beisteuert: Dateiwähler, `Task.Run`,
`CancellationTokenSource`, Fensterbesitz. Datenbankarbeit bleibt auf dem Bedienfaden, allein
die reinen Rechnungen gehen in `Task.Run` mit `IProgress<T>` und `CancellationToken`.

**Das Diagnosebanner (Konzept 2.2 Punkt 2).** Über den Kennzahlkacheln des Ergebnisblattes
steht `FlottenDiagnosebanner.razor`. Es liest die Zähler aus P1
(`FlottenBetriebsdiagnose`, `FlottenPlausibilitaet.Gruende`) und sagt im Klartext, **warum**
eine Flotte arbeitslos blieb — und bietet dazu die passende Abhilfe an: „Peak-Ziel
bestimmen…", „Netzladung erlauben" oder „Zur Betriebsführung". Jede Abhilfe ist ein Delegat;
fehlt er, fehlt der Knopf („kein Delegat ist kein Knopf").

**Peak-Ziel: Vorschlag, Bestimmung, Vorprüfung (SD‑Q3/SD‑Q5).** Auf Station 3 steht der
`PeakZielBlock`: Er zeigt den **hergeleiteten Vorschlag** H₀ aus der Referenzzeitreihe samt
Herleitungszeile (die feste 50-kW-Vorgabe ist mit P1 gefallen), lässt ihn übernehmen und
bietet „Peak-Ziel bestimmen…" als Suchlauf an — schwach gesperrt, solange kein Planer für das
gewählte Betriebsziel zur Verfügung steht. Die **Netzladung folgt dem Betriebsziel**
(`FlottenVorgaben.NetzladungFuer`): Wer das Ziel wechselt, bekommt die dazu passende
Vorbelegung, statt eine fremde mitzuschleppen. Die **Vorprüfung** läuft vor jedem Lauf und
ohne ihn: Sie meldet, was das Ergebnis entwerten würde — allen voran ein Peak-Ziel unter dem
Tagesminimum.

**Ressourcen und CSS.** 46 neue Schlüssel (`FLOTTE_SEITE_*`, `FLOTTE_BANNER_*`,
`FLOTTE_PEAK_*` sowie einige `FLOTTE_DLG_*` und `SPAUS_*`) stehen deutsch und englisch am
**Ende** beider `.resx`; `Resource.Designer.cs` ist mit
`Werkzeuge/ResourceDesigner/designer_neu.py` neu erzeugt. Die Gestaltung liegt in
`epos-ui.css` (`.epos-spauslegung*`, `.epos-ablaufleiste*`, vor dem Block FORMULARRASTER) und
`epos-flotte.css` (`.epos-flotte-diagnose*`, `.epos-flotte-peakziel*`); Tokens stehen in
`:root`, es gibt keine CSS-Verschachtelung.

**Was NICHT in diesem Paket steckt:** die Größen-Sicht mit Rasterkarte und Schnittkurve
(P4, #193) — im Ergebnisblatt stand dafür die Marke `@* P4: SpeicherFlottenGroessenAnsicht
(#193) *@`; an ihrer Stelle steht seit **#196** der Baustein selbst (Abschnitt „Einbindung in
die Ansicht").

## Größen-Sicht der Flotte (P4, #193)

Die Rastersuche gab es seit jeher — angezeigt wurde davon eine Texttabelle mit höchstens
50 Zeilen. Wie der Kapitalwert an der GRÖSSE hängt, stand nirgends; für den Einzelspeicher
zeichnet der Renderer Rasterkarte und Schnittkurve seit W11b‑B‑5, für die Flotte nicht
(Konzept 1.5). Und die Kandidaten trugen weder Durchsatz noch Vollzyklen: **ein arbeitsloser
Kandidat war von einem arbeitenden nicht zu unterscheiden** — derselbe Befund, der als
SP‑O‑10 den ganzen Flottenlauf betraf. Zielbild ist
[`Projekte/Konzept_Stromspeicher_Dialoge_EPOS-Plan.md`](Projekte/Konzept_Stromspeicher_Dialoge_EPOS-Plan.md)
Abschnitt 2.5.

**Der Kandidat trägt seine Kennzahlen.** `FlottenKandidatZusammenfassung` führt seither
`DurchsatzKWh` (AC-Entladung des ersten gerechneten Jahres), `Vollzyklen`
(Durchsatz ÷ Kapazität), `BezugsspitzeKw`, `ErsparnisEuroJahr` und `Arbeitslos` aus der
Diagnose (P1) — dazu die Achsenwerte: `CRate` (Entladeleistung ÷ Kapazität), die Größen je
Einheit (`FlottenKandidatEinheit`) und die Stelle im Raster (`Rasterzeile`/`Rasterspalte`,
belegt bei genau einer aktiven Suchachse). **Gerechnet wird nichts nach**: Der
`FlottenOptimierer` füllt die Felder aus dem Kandidatenlauf, den er ohnehin fährt. Die
**Ersparnis ist die Rechnungsdifferenz OHNE Kapitaldienst** — abzüglich Betriebsaufwand und
Durchsatzkosten, aber ohne Investition, Ersatz und Restwert; die stecken im Kapitalwert
daneben. Beide Zahlen zusammen beantworten „lohnt der Betrieb?" und „trägt sich die
Anschaffung?" getrennt.

**Die Karte baut der Kern.** `SpeicherFlottenAnzeigeCtrl.Rasterdaten(ergebnis, einheit)`
liefert Achsen (Kapazität als Zeilen, C-Rate als Spalten), die Wertematrix in EUR, die
Schraffurmatrix und die Stelle des Optimums; `einheit = -1` sind die Summen der Flotte, sonst
die Größen der gewählten Einheit. Eine Stelle ohne Kandidat ist **`double.NaN`** und keine
Null — der Renderer überspringt sie in der Farbskala, und eine Null wäre die Aussage „genauso
gut wie die Nullvariante". Stehen zwei Kandidaten auf derselben Stelle (dieselbe Hardware, zwei
Betriebsziele), besetzt der bessere sie: erst Zulässigkeit, dann Kapitalwert — die Rangfolge
der Spezifikation 9.4. Die Achsen kommen aus den WERTEN und nicht aus dem Rasterindex: Im Modus
`KapazitaetUndLeistung` trüge dieselbe Spalte in jeder Zeile eine andere C-Rate. Dazu die zwei
Schnitte `Schnittdaten(ergebnis, cRate)` (über der Kapazität) und
`SchnittdatenLeistung(ergebnis, kapazitaetKwh)` (dieselbe Kurve über der Entladeleistung,
Achse aus `P = E · C`).

**Zwei Zutaten im Bild.** `ChartRenderer.Optimierungsraster` nimmt seither zwei optionale
Parameter: `bool[][] unzulaessig` schraffiert die gesperrten Zellen ÜBER ihrer Farbe — der Wert
bleibt ablesbar, die Sperre kommt dazu, und sie ist eine MUSTER-Aussage statt einer Farbaussage
(WCAG 1.4.1) — und `fusszeile` setzt den **SP‑O‑4-Hinweis** ins Bild
(`FLOTTE_GROESSEN_ENDLICHES_RASTER`: die Aussage gilt nur für das geprüfte endliche Raster).
Der Hinweis steht IM Bild, weil das Bild exportiert wird. Beide Parameter haben den Standard
`null`; die bestehenden Aufrufer und die 39 Bilder der ChartProben bleiben byte-gleich
(nachgewiesen durch je einen vollständigen Lauf vor und nach der Änderung). `Schnittkurve`
bleibt unverändert — „über der Leistung" ist dieselbe Funktion mit anderer Achsenbeschriftung.
`Proben/ChartProben` führt seither **41 Bilder und 8 Gegenproben, zusammen 49 Proben**: die
Flotten-Rasterkarte mit Schraffur und Fußzeile, den Schnitt über der Leistung und die
Gegenprobe, dass die Schraffur das Bild wirklich ändert.

**Der Baustein** `EPOS.UI/Dialoge/Strom/SpeicherFlottenGroessenAnsicht.razor` zeigt in dieser
Reihenfolge: Aussage und SP‑O‑4-Hinweis, die **Rasterkarte** (Einheitenwahl ab zwei Einheiten),
daneben die **zwei Schnitte** mit je einem Schieber über die STELLEN der Achse (die
Stützstellen einer Rastersuche sind nicht gleichmäßig verteilt — ein Schieber, der dazwischen
stehen bliebe, zeigte eine Kurve, die es nicht gibt; Vorbelegung ist die Stelle des Optimums),
und darunter die **Kandidatentabelle**: Kapazität, Lade-/Entladeleistung, C-Rate, Kapitalwert,
Ersparnis/a, Vollzyklen/a, Bezugsspitze, zulässig mit Grund — sortierbar je Spalte, mit
Spaltenfilter nach dem Katalogfilter-Muster, hervorgehobener Optimum-Zeile, benanntem
Kennzeichen „arbeitslos" und „Übernehmen" je Zeile (`EventCallback<FlottenKandidatZusammenfassung>
KandidatUebernehmen`). **Gefiltert wird VOR der Tabelle** (W14a‑E‑10): Profil und Zeilen baut
`SpeicherFlottenAnzeigeCtrl.Kandidatenprofil()`/`.Kandidatenzeilen(ergebnis)` über
`Katalogfilterprofil.AusSpalten`, eingeschränkt wird mit `Katalogfilter.Anwenden` — damit gelten
für die Kandidaten Zahlenausdruck (`>10`, `10..60`), Verknüpfung und Sortierzyklus der fünfzehn
Katalogwirte.

Die Verfeinerung um interessante Kandidaten (Spezifikation 12.2, „Feinraster") bleibt Paket P5.

### Einbindung in die Ansicht (#196)

Seit dem 11.09.2026 steht der Baustein dort, wofür er gebaut wurde: in **Schritt 5 der Ansicht
`STROMSPEICHER_AUSLEGUNG`**, und zwar **vor** der Ergebnisansicht — so verlangt es Konzept 2.5
(„Rasterkarte und Schnittkurve vor der Kandidatentabelle"). Bedingung ist das
**Rastersuchergebnis** (`SpeicherFlottenErgebnis.Auslegung`) und nicht der Schalter „Größen
optimieren": Der Schalter sagt, was der NÄCHSTE Lauf tut; abgeschaltet nach einem Suchlauf
verschwände die Karte, die gerade entstanden ist. Den Weg dorthin nimmt das Rastersuchergebnis
ohne Umweg — `StromspeicherAuslegungCtrl.FlotteRechnen` reicht es als Feld `Auslegung` des
`SpeicherFlottenErgebnis` an die Seite, die es in `_flottenErgebnis` hält; es gibt keinen zweiten
Kanal und keine zweite Abfrage.

**Mit der Einbindung fällt die einfache Kandidatentabelle** der `SpeicherFlottenErgebnisAnsicht`
(Texttabelle, höchstens 50 Zeilen, ohne Betriebskennzahlen). Zwei Tabellen derselben Kandidaten
wären zwei Wahrheiten — die eine schneidet ab, die andere filtert, und der Anwender sähe nicht,
welche seine Frage beantwortet. Mitgewandert sind die **Empfehlung** des Laufs
(`FLOTTE_EMPF_BESTE` / `…_KEINE_ZULAESSIGE`, den Fall „Nullvariante" trug die Größen-Sicht schon)
und der **CSV-Export** des Variantenvergleichs; den Text baut weiterhin
`SpeicherFlottenAnzeigeCtrl.VergleichCsv` beim Wirt, weil er den ganzen Flottenlauf braucht und
nicht nur das Rasterergebnis.

**„Kandidat übernehmen" geht den Weg, den `BesteKonfiguration` seit jeher nimmt.** Beide Knöpfe
laufen über dieselbe Stelle der Seite (`FlotteSetzen`): Suchachsen leeren, Suchlauf abschalten,
Schritt 5 als **veraltet** markieren; danach steht die Ansicht auf Schritt 1 und meldet im Banner
„Kandidat … übernommen — Flotte neu bewerten". Woher die Konfiguration kommt, entscheidet der
**Kern**: `SpeicherFlottenAnzeigeCtrl.KandidatKonfiguration(ergebnis, arbeitsstand, kandidat)`
gibt für den **besten** Kandidaten die `BesteKonfiguration` des Optimierers zurück — keine zweite
Wahrheit für die Optimum-Zeile — und bildet jeden anderen **zurück**: Kapazität, Lade- und
Entladeleistung je Einheit aus `FlottenKandidatEinheit`, das Betriebsziel aus dem Kandidaten,
alles Übrige (Wirkungsgrade, SoC-Band, Kosten, Wirtschaftlichkeit) unverändert aus dem
Arbeitsstand. Vorlage einer Einheit ist die gleichnamige Einheit des Arbeitsstands, sonst die
Vorlage der ersten aktiven Suchachse — die Rastersuche ERSETZT Einheiten durch Abwandlungen ihrer
Vorlage, und deren erzeugte Kennungen stehen im Kandidaten. Die Liste
`Wirtschaftlichkeit.Einheiten` zieht mit, sonst rechnete der nächste Lauf die Kosten der alten
Größen.

**Ungespeicherte Eingaben werden vorher abgefragt** (Muster 62b‑E‑1): Speichern / Verwerfen /
Bleiben, Esc heißt „Bleiben". Der Kandidat ersetzt die Speicher in Schritt 1, und das ist nicht
rückgängig zu machen.

**Die iOS-Hülle bindet seither `epos-flotte.css` ein** (`EPOS.iOS/wwwroot/index.html`). Das Blatt
trägt seit P2 die Flottenstile und seit P3/P4 Diagnosebanner, Peak-Ziel und Größen-Sicht; die
Windows-Hülle bindet es seit #184 ein, die iOS-Hülle nicht — die Ansicht hätte auf dem iPad
ungestaltet dagestanden. **Der Beleg dafür steht aus**: Er braucht einen iOS-Lauf (Lauf 42), und
den löst nach der Regel vom 09.09.2026 der Anwender aus.

**Der Referenzlauf ist unberührt:** Die Rastersuche liegt nicht im Projektlauf, und die neuen
Felder ändern keinen Rechenwert (1030 und 1046 byte-gleich gegen
`Referenzlaeufe/2026-09-11_R7_Speicherflotte`).

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

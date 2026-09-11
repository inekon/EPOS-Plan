# Simulationsgrundlagen und Algorithmus

Geführte Vorlage für ein implementierungsreifes Zielkonzept

INEKON · Fachplanung und Softwareentwicklung

| Dokumentangabe | Eintrag |
|---|---|
| Projekt und Modul | {{PROJEKT}} · {{MODUL}} |
| Dokumentkennung | {{DOKUMENT_ID}} |
| Konzeptversion und Datum | {{VERSION}} · {{DATUM}} |
| Fachlich verantwortlich | {{FACHVERANTWORTUNG}} |
| Technisch verantwortlich | {{ENTWICKLUNGSVERANTWORTUNG}} |
| Prüfstatus | {{PRUEFSTATUS}} |
| Freigabe durch und am | {{FREIGABE_PERSON}} · {{FREIGABE_DATUM}} |

Dieses Dokument legt das Zielmodell einer zeitdiskreten Energie- oder technischen Simulation fest. Es verbindet die fachliche Fragestellung mit Datenverträgen, Gleichungen, einem ausführbaren Ablauf und messbaren Abnahmekriterien. Die ausgefüllte Fassung ist die gemeinsame Grundlage für Implementierung, fachliche Prüfung und spätere Änderungen.

## Benutzungsanleitung

Diese Datei ist eine geführte Vorlage. Alle Angaben in doppelten geschweiften Klammern sind projektspezifisch zu ersetzen. Mit „Pflicht“ bezeichnete Inhalte müssen vor der Freigabe vollständig sein. Ein optionaler Baustein wird entweder ausgefüllt oder ausdrücklich als „nicht Bestandteil“ begründet. Leere Felder gelten nicht als Entscheidung.

Absätze mit „Beispiel zum Ersetzen“ illustrieren eine mögliche Ausfüllung. Sie sind keine Vorgaben für das spätere Projekt und müssen übernommen, angepasst oder entfernt werden. Insbesondere sind Stromspeicher, C#, ein bestimmter Solver und das Viertelstundenraster keine universellen Anforderungen.

Verbindliche Anforderungen erhalten eindeutige Kennungen REQ-001 usw., Gleichungen EQ-001 usw. und Tests TEST-001 usw. Jede Anforderung muss mindestens einem Abnahmetest zugeordnet sein. Jedes im Algorithmus verwendete Symbol wird in Kapitel 3 definiert. Verweise auf Kapitel und Kennungen werden bei Änderungen gemeinsam gepflegt.

Freigaberegel: Keine ungelösten Pflichtplatzhalter, keine widersprüchlichen Einheiten, keine unentschiedenen fachlichen Alternativen und kein freigegebener Test ohne erwartetes Ergebnis. Beispielwerte werden erst durch ausdrückliche Übernahme zu Projektwerten. Eine ausgefüllte Gliederung allein ist noch keine geprüfte Spezifikation.

## 1 Ziel und Systemgrenze

### 1.1 Entscheidungsfrage und Nutzen

Pflicht: {{ENTSCHEIDUNGSFRAGE}}. Benennen Sie, welche konkrete Entscheidung mit der Simulation getroffen werden soll, welche Varianten verglichen werden und wer das Ergebnis verwendet. Beschreiben Sie die Entscheidung in messbaren Größen statt als „optimales System“.

Beispiel zum Ersetzen: Für einen Standort sollen Speicherkapazität und Leistung so gewählt werden, dass der Kapitalwert gegenüber derselben Anlage ohne Zusatzspeicher maximal wird. Die Auswertung weist technische Zulässigkeit und wirtschaftliche Rangfolge getrennt aus.

| Anforderung | Messbares Erfolgskriterium | Nachweis |
|---|---|---|
| REQ-001 Fachziel | {{FACHZIEL_UND_GRENZWERT}} | {{TEST_ID_FACHZIEL}} |
| REQ-002 Genauigkeit | {{GENAUIGKEIT_UND_BEZUG}} | {{TEST_ID_GENAUIGKEIT}} |
| REQ-003 Laufzeit | {{LAUFZEIT_DATENUMFANG_HARDWARE}} | {{TEST_ID_LAUFZEIT}} |

### 1.2 Bilanzraum und Grenzen

Pflicht: {{SYSTEMGRENZE}}. Zeichnen oder beschreiben Sie alle Quellen, Senken, Speicher und Kopplungspunkte. Benennen Sie für jeden Messwert den Messort, die Richtung und bereits enthaltene Verluste. Legen Sie fest, welche Teile als exogene Eingabe unverändert bleiben und welche durch die Simulation beeinflusst werden.

Beispiel zum Ersetzen: Gemeinsamer AC-Knoten mit Bruttolast, verfügbarer PV-Leistung nach Wechselrichter, Batterie und Netzanschluss. Lade- und Entladeleistungen werden AC-seitig angegeben. Batterieumwandlungsverluste ändern den internen Energiezustand; separat gemessener Hilfsverbrauch erscheint zusätzlich am AC-Knoten.

Pflicht: {{RAEUMLICHE_GRENZE}}, {{ZEITLICHER_HORIZONT}}, {{MODELLGUELTIGKEIT}}. Dokumentieren Sie die kleinste sinnvoll auflösbare Dynamik. Ein Intervallmittelwert erlaubt keine Aussage über kurzzeitige Spitzen innerhalb dieses Intervalls.

## 2 Anwendungsfälle und Betriebsarten

### 2.1 Verhalten aus Anwendersicht

| Fall | Voraussetzung und Auslöser | Ergebnis und Abschluss |
|---|---|---|
| UC-01 Simulation | {{STARTVORAUSSETZUNG}} | {{SIMULATIONSAUSGABE}} |
| UC-02 Variantenvergleich | {{VERGLEICHSBASIS}} | {{VARIANTENENTSCHEIDUNG}} |
| UC-03 Abbruch | Lauf aktiv; Abbruch angefordert | Status Abgebrochen; keine Freigabe unvollständiger Kennzahlen |
| UC-04 Wiederholung | Gleicher freigegebener Eingabesnapshot | Ergebnis gemäß Reproduzierbarkeitsvertrag aus Kapitel 9 |

### 2.2 Strategien und Prioritäten

Pflicht: {{BETRIEBSARTEN}} und {{PRIORITAETEN}}. Unterscheiden Sie die physikalische Anlagenfreigabe, die Betriebsstrategie und gegebenenfalls die Verteilung einer Gesamtanforderung auf mehrere Komponenten. Regeln müssen bei gleichzeitiger Aktivierung eine eindeutige Reihenfolge besitzen.

Beispiel zum Ersetzen: Zuerst technische Schutzgrenzen, danach verpflichtende Reserve, danach wirtschaftliche Fahrplananforderung. Innerhalb einer Priorität erfolgt die Auswahl nach stabiler Komponentenkennung. Eine Reserve darf nur durch eine ausdrücklich definierte Notfallregel unterschritten werden.

Pflicht: {{AUSGESCHLOSSENE_FUNKTIONEN}}. Beispiele für auszuschließende oder gesondert zu modellierende Funktionen sind Inselnetzstabilität, Schutztechnik, Temperaturdynamik und Echtzeitregelung. Beschreiben Sie außerdem, welche behaupteten Ergebnisgrößen ohne diese Funktionen nicht zulässig sind.

## 3 Größen und Zeitmodell

### 3.1 Symbol und Einheitenvertrag

| Symbol oder Feld | Bedeutung | Einheit und Wertebereich | Zeitbezug |
|---|---|---|---|
| t | Intervallindex | Ganzzahl 0 bis N−1 | Beginn des Intervalls |
| dt[t] | Intervalllänge | h; strikt positiv | [timestamp[t], timestamp[t+1]) |
| x[t] | Vollständiger Modellzustand | {{ZUSTANDSEINHEITEN}} | Vor dem Intervall |
| u[t] | Zulässige Stellgrößen | {{STELLGROESSENEINHEITEN}} | Im Intervall |
| w[t] | Exogene Eingaben | {{EINGANGSEINHEITEN}} | Nach Datenvertrag |
| y[t] | Ergebnis des Intervalls | {{ERGEBNISEINHEITEN}} | Mittelwert oder Integral ausdrücklich angeben |

Pflicht: {{VORZEICHENKONVENTION}}. Jeder vorzeichenbehaftete Fluss braucht eine positive Richtung. Prozentwerte werden intern als Anteil oder Prozentpunkte gespeichert; die Entscheidung {{PROZENTDARSTELLUNG}} gilt durchgängig. Dimensionslose Wirkungsgrade sind von dimensionsbehafteten Kennzahlen zu unterscheiden.

### 3.2 Zeitraster und Integration

Pflicht: {{RASTER}}, {{ZEITZONE}}, {{KALENDERREGEL}}, {{INTERVALLBEDEUTUNG}}. Empfohlener Vertragsbaustein: Zeitzonenbewusste Zeitstempel, Berechnung auf einer monotonen UTC-Achse und lokale Kalenderzuordnung ausschließlich für Anzeige und Abrechnung. Bestimmen Sie Start inklusive und Ende exklusiv. Für N Intervalle gibt es N+1 Zustandszeitpunkte.

EQ-001 Umrechnung von Intervallmittelwerten in Energie:

```text
Q = Summe_t(P[t] * dt[t])
```

Bedeutung: Q ist die über alle Intervalle integrierte Energie; P[t] die mittlere Leistung. Einheit: kW mal h ergibt kWh. Gültigkeit: P ist ein Intervallmittelwert, kein Momentanwert. Kontrollfall: 8 kW über 0,25 h ergeben 2 kWh. Bei Momentanwerten ist stattdessen {{INTEGRATIONSVERFAHREN}} festzulegen.

Beispiel zum Ersetzen: Ein vollständiges gewöhnliches Jahr mit Viertelstundenraster hat 35.040 Intervalle, ein Schaltjahr 35.136. Lokale Tage bei Zeitumstellung können 92 oder 100 Intervalle besitzen. Die Software zählt Zeitstempel und leitet Tage nicht durch Division durch 96 ab.

## 4 Eingabedaten und Qualitätsprüfung

### 4.1 Datenvertrag

| Feld | Typ und Einheit | Pflicht und Quelle | Validierung |
|---|---|---|---|
| timestamp | Zeitstempel mit Offset | Pflicht; Zeitachse | Eindeutig, streng aufsteigend |
| {{FELD_LAST}} | Intervallmittelwert in kW | {{LASTQUELLE}} | Endlich; Bezugspunkt und Vorzeichen geklärt |
| {{FELD_ERZEUGUNG}} | Intervallmittelwert in kW | {{ERZEUGUNGSQUELLE}} | Bereits saldierte Anteile nicht erneut abziehen |
| {{FELD_PREIS}} | EUR/kWh | {{PREISQUELLE}} | Fehlend ist nicht null; negative Preise nach Tarifvertrag |
| {{FELD_PARAMETER}} | {{TYP_EINHEIT}} | {{PARAMETERQUELLE}} | {{PARAMETERGRENZEN}} |

Pflicht: {{IMPORTFORMAT}}, {{SCHEMAVERSION}}, {{EINHEITENUMRECHNUNG}}, {{RESAMPLING}}. Beim Wechsel des Rasters muss die Integration einer Leistungsreihe erhalten bleiben. Ein Stundenpreis wird für vier Viertelstunden unverändert übernommen; er wird nicht durch vier geteilt. Bruttolast, Netzlast und Erzeugung dürfen nicht ohne Messpunktabgleich kombiniert werden.

### 4.2 Fehlende Daten und Herkunft

Standard zum bewussten Übernehmen: Leere Reihen, NaN, Unendlich, doppelte Zeitstempel, Rasterlücken und unvereinbare Reihenlängen führen vor dem Lauf zu einem strukturierten Validierungsfehler. Keine stillschweigende Interpolation und keine automatische Nullsetzung.

Abweichende Reparaturen benötigen {{ERSATZWERTVERFAHREN}}, {{MAXIMALE_LUECKE}} und {{REPARATURKENNZEICHNUNG}}. Originaldaten bleiben erhalten; ersetzte Intervalle sind mit Grund, Methode und Version protokolliert. Entscheiden Sie, ob reparierte Daten für die fachliche Abnahme zulässig sind.

Pflicht: {{DATENHERKUNFT_UND_NUTZUNGSRECHTE}}. Speichern Sie Quellenkennung, Abruf- oder Exportzeitpunkt, Originaleinheit, Konvertierungsversion und Prüfsumme. Ein freigegebener Lauf verwendet einen festen Datenstand.

### 4.3 Prognosen und Informationsstand

Optionaler Baustein: {{PROGNOSEMODUS}}. Trennen Sie Istwerte für die physikalische Ausführung von Prognosen für die Entscheidung. Prognosen tragen Entscheidungszeitpunkt, Veröffentlichungszeitpunkt, Gültigkeitsintervall und Modellversion. Später bekannt gewordene Werte dürfen eine frühere Entscheidung nicht beeinflussen.

Perfekte Voraussicht wird als eigener Vergleichsmodus ausgewiesen. Legen Sie für fehlende Prognosen den Ersatzfahrplan in Kapitel 6 fest. Netzabrufe während der eigentlichen Berechnung sind im Referenzvertrag ausgeschlossen; Datenbeschaffung findet vorher statt.

## 5 Physikalisches Modell

### 5.1 Zustandsraum und Parameter

| Zustand oder Parameter | Initialisierung | Grenze und Veränderung | Nachweis |
|---|---|---|---|
| {{ZUSTAND_1}} | {{STARTWERT_1}} | {{ZUSTANDSGRENZE_1}} | {{TEST_ZUSTAND_1}} |
| {{PARAMETER_1}} | {{PARAMETERWERT_1}} | {{PARAMETER_GUELTIGKEIT}} | {{TEST_PARAMETER_1}} |
| {{GEDAECHTNISZUSTAND}} | {{REGLER_INITIALISIERUNG}} | Hysterese, Prognosehistorie oder Peakgedächtnis | {{TEST_GEDAECHTNIS}} |

Pflicht: {{ZUSTANDSVOLLSTAENDIGKEIT}}. Alles, was spätere Ergebnisse beeinflusst, gehört zum expliziten Zustand. Beschreiben Sie Temperatur, Alterung oder andere zeitvariable Parameter entweder mit eigener Zustandsgleichung oder als begründete Vereinfachung. Ein versteckter statischer Reglerzustand verletzt den Wiederholungsvertrag.

### 5.2 Zustandsübergang

EQ-002 Allgemeiner Übergang:

```text
x[t+1] = F(x[t], u[t], w[t], dt[t], theta)
y[t]   = H(x[t], u[t], w[t], dt[t], theta)
```

Bedeutung: F liefert den Folgezustand, H die Intervallergebnisse; theta umfasst die festen Modellparameter. Einheit: Jede Komponente von F besitzt die Einheit des zugehörigen Zustands. Gültigkeit: {{GUELTIGKEIT_UEBERGANG}}. Kontrollfall: {{HANDRECHENFALL_UEBERGANG}}. Die konkreten Funktionen müssen vor der Freigabe vollständig ausgeschrieben sein.

Beispiel zum Ersetzen für einen Batteriespeicher ohne Selbstentladung:

```text
EQ-003: E[t+1] = E[t] + dt * (eta_c*c[t] - d[t]/eta_d)
EQ-004: G[t] = L[t] - PV[t] + K[t] + A[t] + c[t] - d[t]
```

Bedeutung: E ist interne Energie vor dem Intervall; c und d sind nichtnegative AC-Lade- und Entladeleistung. L ist Bruttolast, PV verfügbare AC-Erzeugung, K deren Abregelung und A AC-Hilfsleistung. G ist positiv bei Netzbezug. Einheiten: E in kWh, Leistungen in kW, dt in h, eta_c und eta_d dimensionslos in (0,1]. Gültigkeit: Gemeinsamer AC-Knoten und konstante Wirkungsgrade je Intervall. Kontrollfall: E=2 kWh, c=4 kW, d=0, dt=0,25 h und eta_c=0,9 ergeben E_neu=2,9 kWh. Bei L=5, PV=3, K=A=0 ergibt G=6 kW.

### 5.3 Grenzen und Invarianten

Beispiel zum Ersetzen mit Energiegrenzen E_min und E_max sowie Leistungsgrenzen P_c_max und P_d_max:

```text
EQ-005: c_max = min(P_c_max, (E_max-E)/(eta_c*dt))
EQ-006: d_max = min(P_d_max, (E-E_min)*eta_d/dt)
EQ-007: V = dt*((1-eta_c)*c + (1/eta_d-1)*d)
EQ-008: r_E = E_neu-E-dt*(c-d)+V
```

Bedeutung: c_max und d_max sind zulässige AC-Leistungen; V sind Umwandlungsverluste, r_E der Energiebilanzrest. Einheiten: Leistungen kW, V und r_E kWh. Gültigkeit: E liegt vor dem Schritt im zulässigen Band; Hilfsverbrauch wird gemäß EQ-004 separat bilanziert. Kontrollfall: 1 kWh freier Platz, eta_c=0,95 und dt=0,25 h erlauben höchstens 4,210526 kW Laden. Bei 4 kW Laden beträgt V=0,05 kWh und r_E=0.

Pflicht: {{NEBENBEDINGUNGEN}}. Definieren Sie insbesondere nicht gleichzeitig zulässige Flüsse, Kopplungsgrenzen, Verfügbarkeit und Reserven. Für das Batterie-Beispiel gilt c*d=0; eine Abweichung erfordert ein ausdrücklich anderes Modell. Ein geforderter Stellwert wird vor dem Zustandsupdate auf physikalische Zulässigkeit geprüft. Tatsächlich ausgeführte und angeforderte Leistung werden getrennt gespeichert.

## 6 Chronologischer Algorithmus

### 6.1 Initialisierung und verbindliche Reihenfolge

Pflicht: {{INITIALISIERUNG}}, {{REGELUNG}}, {{KOPPLUNGSREIHENFOLGE}}. Definieren Sie Startzustand, Reglergedächtnis und gegebenenfalls Vorlaufphase. Bei rückgekoppelten Komponenten sind explizite oder iterative Kopplung, Reihenfolge, Konvergenzmaß und maximale Iterationszahl festzulegen.

Der folgende Pseudocode ist der auszufüllende Ablaufvertrag. Die benannten Funktionen dürfen in der Projektfassung keine fachlichen Leerstellen enthalten.

```text
Run(input, parameters, cancellation, progress):
  ValidateSchemaUnitsTimeAndParameters()
  snapshot = FreezeInputsAndParameters()
  state = InitializeState(snapshot)
  ValidateState(state)
  for t = 0 .. N-1:
    CheckCancellation()
    actual = ReadInterval(snapshot, t)
    known = SelectInformationAvailableAtDecisionTime(t)
    request = Strategy.Decide(state, known, parameters)
    controls = ResolvePrioritiesAndPhysicalLimits(request, state)
    step = Physics.Advance(state, controls, actual, dt[t])
    CheckFiniteValuesBoundsAndBalance(step)
    CommitStepAtomically(t, request, controls, step)
    state = step.NextState
    ReportProgressIfDue(t+1, N)
  CheckTerminalCondition(state)
  result = AggregateCommittedSteps()
  ValidateAggregateBalances(result)
  return Complete(result, diagnostics)
```

Bei einer iterativen Lösung wird CheckCancellation auch innerhalb der Iteration ausgeführt. Die Ausgabe eines Schritts wird erst nach erfolgreicher Prüfung übernommen. Zeitpunkt der Stellgrößenentscheidung und Verwendung von Intervallmittelwerten müssen im Informationsvertrag miteinander vereinbar sein.

### 6.2 Numerik und Grenzfälle

Pflicht: {{ZAHLENTYP}}, {{TOLERANZEN}}, {{TOLERANZSKALIERUNG}}, {{KONVERGENZREGEL}}. Empfohlener Baustein ist doppelte Gleitkommagenauigkeit im Rechenkern; Rundung erfolgt erst zur Anzeige. Toleranzen besitzen Einheiten und werden zentral versioniert.

EQ-009 Skalierte Prüfung zweier Größen a und b derselben Einheit:

```text
abs(a-b) <= eps_abs + eps_rel * max(abs(a), abs(b))
```

Bedeutung: eps_abs ist die absolute und eps_rel die relative Toleranz. Einheit: eps_abs wie a und b; eps_rel dimensionslos. Gültigkeit: Beide Werte sind endlich. Kontrollfall: a=1000 kWh, b=1000,0005 kWh, eps_abs=0,000001 kWh und eps_rel=0,000001 ergeben die Grenze 0,0010010005 kWh; die Prüfung besteht.

Beispiel zum Ersetzen: Für kleine Batterie-Handrechenfälle eps_abs=0,000001 kWh und eps_rel=0,000000001. Die Jahressumme erhält eine eigene begründete Toleranz {{JAHRESBILANZTOLERANZ}}. Innerhalb der Toleranz korrigierte Grenzwerte werden gezählt; größere Verletzungen führen zum Fehler. Unkontrolliertes Klemmen darf keinen Bilanzfehler verdecken.

### 6.3 Fehler und Rückfall

| Ereignis | Festgelegte Reaktion | Ergebnisstatus |
|---|---|---|
| Ungültige Eingabe | Vor Rechenbeginn mit Feld, Index und Ursache ablehnen | ValidationFailed |
| Abbruch | Am nächsten Prüfpunkt stoppen; vollständige Teilschritte nur diagnostisch | Cancelled |
| Nichtendlicher Wert oder Bilanzfehler | Betroffenen Schritt nicht übernehmen; Ursache protokollieren | Failed |
| Fehlende Prognose | {{ERSATZFAHRPLAN}} nach denselben physikalischen Prüfungen | CompletedWithWarnings oder Failed |
| Keine zulässige Steuerung | {{REAKTION_UNZULAESSIG}}; keine erfundene Versorgung | Failed oder ausdrücklich modellierte Fehlmenge |

Ein erfolgreich beendeter Rückfalllauf weist Anzahl und betroffene Intervalle aus. Er erhält keine uneingeschränkte Gleichstellung mit einem regulären Lauf, wenn seine Annahmen die Vergleichbarkeit verändern.

## 7 Optimierung und Variantenvergleich

### 7.1 Aufgabe und Suchraum

Optionaler Baustein: {{OPTIMIERUNG_AKTIV}}. Unterscheiden Sie Auslegungsoptimierung über Anlagenparameter und Betriebsoptimierung über zeitabhängige Stellgrößen. Beide besitzen einen eigenen Vertrag. Ein MILP-Solver, eine Rastersuche oder eine Heuristik wird nur mit begründeter Auswahl eingesetzt.

| Variable | Einheit und Typ | Zulässiger Bereich | Abhängigkeit |
|---|---|---|---|
| {{ENTSCHEIDUNG_1}} | {{VARIABLENTYP_1}} | {{SUCHRAUM_1}} | {{KOPPLUNG_1}} |
| {{ENTSCHEIDUNG_2}} | {{VARIABLENTYP_2}} | {{SUCHRAUM_2}} | {{KOPPLUNG_2}} |

Pflicht bei Optimierung: {{ZIELFUNKTION}}, {{MIN_ODER_MAX}}, {{ZIELFUNKTIONSEINHEIT}}, {{ZULAESSIGKEIT}}, {{SUCHVERFAHREN}}. Jede Gewichtung braucht Einheit und Begründung. Bestimmen Sie, welche Kosten nur die Steuerung beeinflussen und welche tatsächlich in die Ergebnisrechnung eingehen.

### 7.2 Ablauf und Auswahlregel

```text
ValidateSearchSpaceAndBudget()
candidates = BuildDeterministicCandidateSet()
for candidate in candidates:
  CheckCancellation()
  run = SimulateSameDataAndBoundaryConditions(candidate)
  feasible = CheckAllHardConstraints(run)
  StoreCandidate(candidate, run, feasible, objective)
RankOnlyComparableFeasibleCompletedCandidates()
ApplyDocumentedTieBreak()
VerifySelectedCandidateWithFullResolution()
```

Pflicht: {{SUCHBUDGET}}, {{VERFEINERUNG}}, {{ABBRUCHKRITERIUM}}, {{GLEICHSTANDSREGEL}}. Ein grobes Raster mit lokaler Verfeinerung darf als bester geprüfter Kandidat bezeichnet werden, nicht ohne Nachweis als globales Optimum. Eine Verfeinerung muss innerhalb der freigegebenen Suchgrenzen bleiben. Gleichstandsgruppen werden relativ zum besten Zielfunktionswert mit festgelegter Toleranz gebildet; innerhalb der Gruppe gilt eine stabile sekundäre Sortierung.

Beispiel zum Ersetzen: Bei wirtschaftlich gleichwertigen Kandidaten niedrigere Investition, danach kleinere Kapazität, danach kleinere Leistung, zuletzt stabile Varianten-ID. Die Referenz ohne Zusatzanlage bleibt im Vergleich enthalten.

### 7.3 Solverstatus und Randbedingungen

Pflicht: {{SOLVER_UND_VERSION}}, {{ZEITLIMIT}}, {{OPTIMALITAETSLUECKE}}, {{ENDZUSTAND}}. Bei zeitlimitierter Lösung wird ein vorhandener zulässiger Kandidat als „zulässig, Optimalität nicht nachgewiesen“ ausgewiesen. Ohne zulässigen Kandidaten gilt „kein Ergebnis“; bei nachgewiesener Unlösbarkeit „unzulässig“. Ein Abbruch liefert keine abgeschlossene Rangfolge.

Anfangs- und Endzustände müssen zwischen Varianten vergleichbar sein. Legen Sie zyklischen Abschluss, fortgeschriebenen Zustand oder explizite Bestandsbewertung fest. Ein gratis gefüllter Anfangsspeicher darf durch Entleerung keinen künstlichen Jahresvorteil erzeugen. Für rollierende Planung sind Horizont, Neuplanungsintervall und terminale Bewertung festzulegen.

## 8 Softwarearchitektur und Schnittstellen

### 8.1 Verantwortlichkeiten

| Baustein | Verantwortung | Vertrag zur nächsten Schicht |
|---|---|---|
| Datenbeschaffung | Import, Umrechnung, Herkunft | Validierbarer Datenstand |
| Orchestrierung | Snapshot, Laufkennung, Abbruch, Ablage | Unveränderliche Eingabe und Parameter |
| Strategie | Stellanforderung aus erlaubter Information | Angeforderte Steuerung |
| Physik und Rechenkern | Grenzen, Zustände, Bilanzen | Geprüfte Schritte und Gesamtergebnis |
| Optimierung | Kandidaten, Aufruf, Bewertung | Rangfolge mit Status und Nachweisen |
| Oberfläche | Eingaben und Ergebnisdarstellung | Kein eigener Berechnungsweg |

### 8.2 Datenrollen und C# Beispiel

| Rolle | Mindestinhalt |
|---|---|
| SimulationInput | Zeitachse, typisierte Reihen, Schema- und Quellenkennung |
| SimulationParameters | Modell-, Strategie- und Numerikparameter mit Version |
| SimulationState | Alle physikalischen Zustände und das notwendige Reglergedächtnis |
| SimulationStepResult | Anforderungen, ausgeführte Flüsse, Folgezustand, Bilanzrest |
| SimulationResult | Status, Zeitreihen, Kennzahlen, Anfangs- und Endzustand |
| SimulationDiagnostics | Meldungen, Laufmetadaten, Datenreparaturen, Solverstatus |

Beispiel zum Ersetzen: Die folgenden Schnittstellen setzen die genannten DTOs als projektspezifisch definierte Typen voraus. Sie sind ein Vertragsmuster und kein vollständiger Rechenkern.

```csharp
public interface ISimulationEngine
{
    SimulationResult Run(
        SimulationInput input,
        SimulationParameters parameters,
        CancellationToken cancellation,
        IProgress<SimulationProgress>? progress = null);
}

public interface ISimulationStrategy
{
    ControlRequest Decide(
        SimulationState state,
        DecisionInformation known,
        SimulationParameters parameters);
}

public interface ISimulationOptimizer
{
    OptimizationResult Optimize(
        SimulationInput input,
        SimulationParameters baseline,
        OptimizationOptions options,
        CancellationToken cancellation,
        IProgress<OptimizationProgress>? progress = null);
}
```

Zusätzliche Rollen: ControlRequest enthält gewünschte Stellwerte; DecisionInformation ausschließlich zum Entscheidungszeitpunkt verfügbare Informationen. OptimizationOptions beschreibt Suchraum und Budget, OptimizationResult die Kandidaten und deren Status. Fortschrittsdaten enthalten erledigte und gesamte Arbeit sowie Laufkennung, keine veränderlichen Rechenobjekte.

Pflicht: {{DTO_FELDER}}, {{API_VERSION}}, {{FEHLERABBILDUNG}}, {{THREADING}}. Ein C#-record allein macht enthaltene Arrays oder Listen nicht unveränderlich. Nutzen Sie echte unveränderliche Sammlungen oder defensive Kopien. Zustände gehören jeweils einem Lauf. Parallelisierung darf weder Ergebnisse noch Gleichstandsregeln verändern.

### 8.3 Integration einer Razor Oberfläche

Beispiel zum Ersetzen: Der Dialog prüft Benutzereingaben und übergibt einen Snapshot an die Orchestrierung. Diese führt CPU-Arbeit außerhalb des UI-Threads aus. Ein CancellationTokenSource gilt pro Lauf. Fortschrittsmeldungen werden auf den UI-Kontext übertragen und gedrosselt; die Abschlussmeldung wird immer übermittelt.

Verbindlich auszufüllen: {{UI_STARTVORAUSSETZUNGEN}}, {{FORTSCHRITTSINTERVALL}}, {{ERGEBNISUEBERNAHME}}. Ein neuer Lauf erhält eine neue Kennung; verspätete Meldungen eines alten Laufs werden verworfen. Bei Schließen oder Abbruch werden Ressourcen freigegeben. Parameteränderungen markieren angezeigte Ergebnisse als veraltet. Ergebnisse dürfen nur mit dem tatsächlich verwendeten Parametersatz übernommen werden.

## 9 Ergebnisse und Reproduzierbarkeit

### 9.1 Ergebnisvertrag

| Ergebnis | Berechnung und Einheit | Gültigkeitsbedingung |
|---|---|---|
| Zeitreihe {{ZEITREIHE}} | {{ZEITREIHENDEFINITION}} | Nur vollständig geprüfte Intervalle |
| Kennzahl {{KENNZAHL}} | {{KENNZAHLFORMEL_UND_EINHEIT}} | {{KENNZAHLVORAUSSETZUNG}} |
| Bilanzrest | Intervall- und Gesamtsaldo | Toleranz aus Kapitel 6 |
| Laufstatus | Completed, CompletedWithWarnings, Cancelled, ValidationFailed, Failed | Ein eindeutig bestimmter Abschluss |
| Vergleichbarkeit | Datenstand, Randzustände, Informationsmodus | Gleiche Vergleichsgrundlage |

Pflicht: {{AGGREGATIONSREGELN}}, {{AUSGABEFORMAT}}, {{ANZEIGERUNDUNG}}. Quotienten benötigen ein definiertes Verhalten bei Nenner null: beispielsweise „nicht definiert“ statt erfundener Null. Ein kurzer Beispielzeitraum darf keine unmarkierte Hochrechnung auf Jahresmaxima liefern. Diagramme zeigen Einheiten, Zeitraum und die Bedeutung eines Vorzeichens.

### 9.2 Laufprotokoll

Jeder Lauf erhält Laufkennung, UTC-Startzeit, Software- und Modellversion, Parameterstand, Eingangshashes, Zeitraster, Strategie, Informationsmodus, Zufallsseed bei stochastischen Verfahren und gegebenenfalls Solverversion und Lösungsstatus. Meldungen besitzen Code, Schweregrad, Komponente, Intervall und betroffene Größe.

Pflicht: {{REPRODUZIERBARKEITSNIVEAU}}, {{AUFBEWAHRUNG}}, {{EXPORTSCHEMA}}. Bestimmen Sie bitgenaue oder toleranzbasierte Wiederholbarkeit einschließlich Plattform- und Parallelitätsgrenzen. Ein fester Seed allein garantiert keine solver- oder plattformübergreifende Identität. Ein Export muss mit seinem Schema eindeutig wieder eingelesen werden können.

## 10 Wirtschaftlichkeit und Sensitivitäten

Optionaler Baustein: {{WIRTSCHAFTLICHKEIT_AKTIV}}. Bei Nichtanwendung Kapitel mit Begründung als nicht Bestandteil kennzeichnen. Bei Anwendung sind Perspektive, Referenzfall, Zeitraum, Preisbasis, Abrechnungsperioden und Zinskonvention verbindlich anzugeben.

EQ-010 Beispiel einer Energiekostenrechnung:

```text
Bill = Summe_t(dt[t]*(Import[t]*p_buy[t]-Export[t]*p_sell[t]))
       + Summe_b(Peak[b]*p_demand[b]) + Fixed
```

Bedeutung: Import und Export sind nichtnegative Netzleistungen; p_buy und p_sell effektive Preise. Peak[b] ist das Bezugsmaximum der Abrechnungsperiode b, p_demand deren Leistungspreis und Fixed der fixe Betrag im Horizont. Einheiten: kW, h, EUR/kWh, EUR/kW je Periode, EUR. Gültigkeit: Tarif mit linearem Arbeitspreis und periodischem Maximum; Staffeln und Zeitfenster erfordern einen eigenen Vertrag. Kontrollfall: 2 kWh Bezug zu 0,30 EUR/kWh, 1 kWh Export zu 0,08 EUR/kWh, 10 kW Peak zu 5 EUR/kW je Periode und Fixed=0 ergeben 50,52 EUR.

EQ-011 Kapitalwert gegenüber der Referenz:

```text
CF[y] = Bill_ref[y] - Bill_variant[y] - OPEX[y] - Replacement[y]
NPV = -CAPEX + Summe_y=1..T(CF[y]/(1+r)^y) + Residual/(1+r)^T
```

Bedeutung: CF ist jährlicher Mehrzahlungsüberschuss, CAPEX Anfangsinvestition, Replacement Ersatzinvestition, Residual Restwert am Ende, r jährlicher Diskontsatz und T Anzahl Jahre. Einheit: Zahlungen und NPV in EUR, r dimensionslos. Gültigkeit: Jahresendzahlungen und konsistente reale oder nominale Preisbasis. Kontrollfall: CAPEX=1000 EUR, CF[1]=CF[2]=600 EUR, r=0,1 und Residual=0 ergeben NPV=41,322314 EUR.

Pflicht: {{TARIFVERTRAG}}, {{KOSTENABGRENZUNG}}, {{PREISBASIS}}, {{ALTERUNGSVERFAHREN}}. Verluste, die bereits in der Netzrechnung enthalten sind, werden nicht nochmals als pauschaler Energieaufwand addiert. Steuerungsinterne Verschleißkosten sind von tatsächlichen Ersatzinvestitionen abzugrenzen. Eigenverbrauchsvorteil und Arbitrage dürfen denselben vermiedenen Bezug nicht doppelt gutschreiben.

Pflicht: {{SENSITIVITAETSPARAMETER}}, {{SZENARIOWERTE}}, {{ROBUSTHEITSKRITERIUM}}. Untersuchen Sie technische Wirkungsgrade, Kosten, Lebensdauer, Prognosefehler und Lastentwicklung mit dokumentierten Szenarien. Die Referenz hat inkrementellen Kapitalwert null; negative Einsparungen bleiben sichtbar.

## 11 Tests und fachliche Abnahme

### 11.1 Handrechenbare Musterfälle

Die folgenden Werte gelten nur für die gekennzeichneten Batterie- und Kostenbeispiele. Jeder Test wird für das Zielmodell ersetzt oder ausdrücklich übernommen. Verglichen werden Rechenergebnisse vor Anzeigerundung.

| Test | Eingabe und Bezug | Erwartung |
|---|---|---|
| TEST-001 Integration | EQ-001; 8 kW, 0,25 h | 2 kWh |
| TEST-002 Zustandsupdate | EQ-003; E=2, c=4, d=0, eta_c=0,9, dt=0,25 | E_neu=2,9 kWh |
| TEST-003 Ladegrenze | EQ-005; freier Platz 1 kWh, eta_c=0,95, dt=0,25; ausreichende Geräteleistung | c_max=4,2105263158 kW |
| TEST-004 Rundlauf | 10 kWh AC laden, anschließend zur Anfangsenergie entladen; eta_c*eta_d=0,9; keine Hilfsverluste | 9 kWh AC zurück; 1 kWh Verlust |
| TEST-005 Netzbilanz | EQ-004; L=5, PV=3, c=4, d=K=A=0 | G=6 kW Bezug |
| TEST-006 Rechnung | EQ-010; Werte aus Kapitel 10 | 50,52 EUR |
| TEST-007 Kapitalwert | EQ-011; Werte aus Kapitel 10 | 41,322314 EUR |

### 11.2 Pflichtszenarien des Zielmodells

| Test | Szenario | Verbindlich zu prüfende Erwartung |
|---|---|---|
| TEST-101 | Normaler chronologischer Lauf | N Schritte, N+1 Zustände, vollständige Bilanzen |
| TEST-102 | Leerer oder lückenhafter Eingang | ValidationFailed vor dem ersten Schritt; genaue Fundstelle |
| TEST-103 | Ungültiger Parameter | Keine Berechnung; Feld und verletzte Grenze ausgegeben |
| TEST-104 | Untere und obere Zustandsgrenze | Kein unzulässiger Fluss; Gegenrichtung bleibt nach Modell zulässig |
| TEST-105 | Gesättigte Leistungsgrenze | Ausführung begrenzt; Restanforderung ausdrücklich ausgewiesen |
| TEST-106 | Bilanz über viele Schritte | Intervall- und Gesamtsaldo innerhalb festgelegter Toleranzen |
| TEST-107 | Abbruch während eines Schritts oder Solverlaufs | Cancelled innerhalb {{ABBRUCHREAKTIONSZEIT}}; kein freigegebenes Teilergebnis |
| TEST-108 | Unlösbare Optimierung | Eindeutiger Status; Rückfall nur gemäß Kapitel 6 |
| TEST-109 | Gleicher Snapshot wiederholt | Identität gemäß Reproduzierbarkeitsniveau |
| TEST-110 | Gleichwertige Kandidaten | Stabile Auswahl gemäß Kapitel 7, auch bei anderer Threadanzahl |
| TEST-111 | Zeitumstellung und Schaltjahr | Vollständige UTC-Achse und korrekte lokale Abrechnung |
| TEST-112 | Geänderte Eingabe nach einem Lauf | Alte Ergebnisse als veraltet markiert; keine Verwechslung der Laufkennung |

Optional bei Prognosen: TEST-113 verändert nur später veröffentlichte Zukunftsdaten; frühere Stellentscheidungen müssen identisch bleiben. Optional bei Wirtschaftlichkeit: TEST-114 prüft den Referenzfall ohne Zusatzanlage sowie negative Einsparungen und Endbestände. Optional bei Zufallsmodellen: TEST-115 prüft Verteilung und Konfidenzintervall nach {{STATISTISCHER_TESTPLAN}}.

### 11.3 Prüfmatrix und Freigabe

| Anforderung oder Gleichung | Testkennung | Sollwert und Toleranz | Verantwortlich und Ergebnis |
|---|---|---|---|
| {{PRUEFGEGENSTAND}} | {{PRUEFTEST}} | {{SOLL_UND_TOLERANZ}} | {{PRUEFER_UND_BEFUND}} |

Pflicht: {{REFERENZDATENSATZ}}, {{REFERENZHASH}}, {{LEISTUNGSTEST}}, {{ABNAHMEVERANTWORTUNG}}. Ein Leistungstest nennt Anzahl Intervalle, Komponenten und Kandidaten, Hardware, Parallelität, Laufzeit- und Speichergrenze. Referenzergebnisse werden unabhängig vom zu prüfenden Algorithmus per Handrechnung oder begründetem Vergleichsmodell bestimmt.

Freigabe erfolgt erst nach bestandenem Schema- und Einheitentest, physikalischer Bilanzprüfung, Randfallprüfung und Anforderungsabdeckung. Ein erfolgreicher Build ersetzt diese fachliche Abnahme nicht. Abweichungen erhalten Befund, Entscheidung und Wiederholungsnachweis.

## 12 Entscheidungen und Quellen

### 12.1 Annahmen und offene Punkte

| Kennung | Annahme oder Entscheidung | Begründung und Auswirkung | Status und Verantwortung |
|---|---|---|---|
| DEC-001 | {{MODELLENTSCHEIDUNG}} | {{ENTSCHEIDUNGSBEGRUENDUNG}} | {{ENTSCHEIDUNGSSTATUS}} |
| OPEN-001 | {{OFFENER_PUNKT}} | {{AUSWIRKUNG_BEI_OFFENHEIT}} | {{VERANTWORTUNG_UND_TERMIN}} |

Vor Freigabe werden alle implementierungsrelevanten offenen Punkte entschieden. Nicht relevante Punkte werden mit Begründung geschlossen. Grenzen des Modells bleiben dokumentiert, auch wenn alle Tests bestehen.

### 12.2 Änderungsverlauf

| Version und Datum | Fachliche Änderung | Betroffene Anforderungen und Tests | Freigabe |
|---|---|---|---|
| {{AENDERUNG_VERSION_DATUM}} | {{AENDERUNG}} | {{AENDERUNG_BEZUEGE}} | {{AENDERUNG_FREIGABE}} |

Eine Änderung an Einheit, Zeitbezug, Vorzeichen, Zustandsgleichung oder Zielfunktion erfordert eine erneute Prüfung der betroffenen Referenzläufe. Schemaänderungen erhalten eine explizite Migrations- oder Ablehnungsregel {{SCHEMAMIGRATION}}.

### 12.3 Quellenregister

| Quelle | Version und Fundstelle | Übernommene Aussage | Prüfung |
|---|---|---|---|
| {{QUELLENTITEL}} | {{QUELLENVERSION_UND_LINK}} | {{QUELLENAUSSAGE}} | {{QUELLENPRUEFUNG_DATUM}} |

Herstellerdaten, Messungen, Normen und technische Veröffentlichungen werden mit genauer Fundstelle geführt. Übernommene Gleichungen sind von eigenen Modellannahmen zu unterscheiden. Für veränderliche Tarife und technische Daten wird der gültige Datenstand angegeben. Ein KI-Chat ist Ausgangsmaterial, kein alleiniger fachlicher Nachweis.

Abschlussprüfung: {{ABSCHLUSSPRUEFUNG}}. Bestätigen Sie vollständige Pflichtangaben, entfernte oder übernommene Beispiele, definierte Symbole, geprüfte Einheiten, eindeutigen Pseudocode, geschlossene Entscheidungen und bestandene Abnahmetests. Dokument und Implementierung beziehen sich auf dieselbe freigegebene Konzeptversion.

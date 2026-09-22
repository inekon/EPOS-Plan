# Normzahlen der Gebäudesimulation — lokal beizustellen, nie versionieren

Dieser Ordner nimmt die **Prüfdaten der zwölf Normtestfälle** der VDI 6007 Blatt 1 auf, gegen
die der 2-K-Löser (`EPOS.Kern/Allgemein/Simulation/Gebaeude/Zonenmodell2K.cs`) geprüft wird.
Die Zahlen gehören **nicht** ins Repositorium und nicht in die Auslieferung: Das Ausliefern der
Normzahlen in Testdateien wäre eine Vervielfältigung (Konzept Gebäudesimulation, Nachtrag
N1.2; Umsetzungskonzept Abschnitt 1.9, Entscheid E27 zu U8). `.gitignore` schließt deshalb
alles unter `Referenzlaeufe/Normzahlen/` aus — bis auf diese Datei.

**Hier steht keine einzige Normzahl, und hier kommt auch keine hinein.**

## Herkunft

- **Richtlinie:** VDI 6007 Blatt 1 (Ausgabe 2015-06), Anhang A1 mit den Testbeispielen 1–12 und
  ihren Ergebnistabellen. Die Richtlinie selbst wird nicht abgelegt.
- **Datenquelle der Prüfung:** die Validierungsmodelle der Modelica-Bibliothek **AixLib**
  (RWTH Aachen University, E.ON Energy Research Center, Institute for Energy Efficient
  Buildings and Indoor Climate), Repositorium `RWTH-EBC/AixLib` auf GitHub. Lizenz: die
  BSD-Lizenz der AixLib mit Zusatzabsatz, Wortlaut in `AixLib/UsersGuide/License.mo` — die Datei
  gehört zu den abgelegten Daten. Die Modelle geben je Testfall eine Programmspalte der
  Ergebnistabellen der Richtlinie wieder.

## Welche Dateien wohin

Die Dateien kommen unverändert, mit ihrer Ordnerstruktur ab `AixLib/`, nach
`Referenzlaeufe/Normzahlen/aixlib/`:

| Quelle in der AixLib | Ziel | Wofür |
|---|---|---|
| `AixLib/ThermalZones/ReducedOrder/Validation/VDI6007/TestCase1.mo` … `TestCase12.mo` (samt `package.mo`, `package.order`, `BaseClasses/`) | `aixlib/AixLib/ThermalZones/ReducedOrder/Validation/VDI6007/` | **Pflicht.** Raumparameter, Eingangs- und Referenztabellen stehen in diesen Dateien (`CombiTimeTable` mit `table=[…]`). |
| `AixLib/UsersGuide/License.mo` | `aixlib/AixLib/UsersGuide/` | Lizenz der Daten. |
| `AixLib/Resources/ReferenceResults/Dymola/…VDI6007_TestCase*.txt` | `aixlib/AixLib/Resources/ReferenceResults/Dymola/` | Nur zur Auskunft: Simulationsergebnisse der AixLib, nicht die Tabellen der Richtlinie. Die Prüfung liest sie nicht. |
| `AixLib/Resources/Scripts/Dymola/ThermalZones/ReducedOrder/Validation/VDI6007/*.mos` | `aixlib/AixLib/Resources/Scripts/…` | Nur zur Auskunft. |

Eine kurze `aixlib/QUELLE.txt` mit Commit und Abrufdatum hilft beim Nachvollziehen.

Gefunden werden die Modelle über ihren Dateinamen `TestCase<n>.mo` irgendwo unter `aixlib/`.
Die Vorrichtung `EPOS.Kern.Tests/Normzahlen.cs` sucht vom Laufordner der Tests aufwärts nach
`Referenzlaeufe/Normzahlen/`; liegt dort kein `aixlib/` mit mindestens einem `TestCase*.mo`,
**schweigen** die zwölf Normfälle (`GebaeudeModellNormfallTests`). So läuft die CI: ohne die
Zahlen, mit den versionierten Gegenwächtern für Prüfband, Vorzeichen und Suche.

## Wie gelesen und geprüft wird

- Die eine Lesestelle ist `EPOS.Kern.Tests/NormfallLeser.cs`. Sie liest Komponenten,
  Parameter und `connect`-Anweisungen der Modelle, bildet die Zone auf den Parametersatz
  `ErsatzparameterRC` ab und die Eingänge auf Blockmittel je Stunde (`Stundenrand`). Die
  Abbildung folgt Befund E, Abschnitt 3
  ([Befund E](../../Dokumentation/aktuell/Gebaeudesimulation/2026-09-15_Befund_E_Prototyp_Normtestfaelle.md)).
- Das Vorzeichen der Last wird aus der Messkette des Modells gelesen; die Last der Richtlinie
  ist positiv beim Heizen.
- Geprüft wird das Blockmittel je Stunde gegen das Band nach Entscheid E10 (Programmspalten
  plus Toleranz plus halbe Druckstelle), an den Tagen, für die das Modell Referenzzeilen führt.
- Der Nachweis ist **lokal**: `dotnet test WP-Plan.Kern.slnf -c Release --filter GebaeudeModellNormfallTests`
  mit abgelegten Daten. Die Testausgabe nennt je Fall die Zahl der Zellen im Band und die
  größte Überschreitung, nie einen Absolutwert.

## Nie committen

`git status` zeigt unter `Referenzlaeufe/Normzahlen/` nur diese Datei. Wer dort etwas anderes
sieht, prüft die `.gitignore`-Regel, bevor er synchronisiert.

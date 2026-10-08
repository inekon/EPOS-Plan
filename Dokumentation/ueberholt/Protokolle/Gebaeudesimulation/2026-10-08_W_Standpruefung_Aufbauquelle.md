# Protokoll W — Standprüfung IFC gegen Projektdatei, Wahl der Aufbauquelle, Fensterrichtung (08.10.2026)

**Sitzung:** Gebäudesimulation, Statuszeile **#825**. Zweige `w1a-standpruefung`, `w1b-dialog`, `w2-fensterrichtung`, zusammengeführt in `39dd31692`.
Commits: W1a `a84c6aee5`, `c96c1a2c5`, `a601171bb`; W1b `005112254`, `1667d4519`; W2 `b727209ca`, `2de053470`, `1a25acc6b`, `e02e086a2`.
**Entscheid:** E108 (Anwender, 08.10.2026) im [Register](../../../aktuell/Status_Gebaeudesimulation_VDI6007.md); er schränkt E98 (Rangfolge der Aufbauten) beim Weg „IFC + Projektdatei“ ein.

## 1 Anlass

Bei einer Anwenderdatei stammen IFC und Projektdatei aus zwei Projektständen desselben CAD-Modells: die IFC aus dem teilsanierten Stand, die Projektdatei aus einer acht Minuten später angelegten Kopie, die auf Bestandsaufbauten umgestellt wurde. Die U-Werte weichen um den Faktor 2 bis 3 ab. EPOS liest beide Dateien richtig (15 von 15 Stichproben); der Fehler liegt nicht im Leser, sondern darin, dass zwei verschiedene Stände ohne Rückfrage zusammengeführt wurden.

## 2 Entscheid E108

- Beim Weg „IFC + Projektdatei“ prüft der Import, ob beide Dateien zum selben Projektstand gehören.
- Schlägt die Prüfung an, fragt er jedes Mal im Dialog, ohne feste Vorgabe: „Aufbauten der Projektdatei verwenden“, „Aufbauten der IFC verwenden“ oder „Abbrechen und neu exportieren“.
- Bis zur Wahl ist Übernehmen gesperrt. Nach „Datei erneut lesen“ wird wieder gefragt; die Wahl wird nicht gespeichert, es gibt keinen Schemaschritt.
- Schlägt die Prüfung nicht an, gilt E98 wie bisher, also der Stand der IFC.

## 3 Gebaut

**W1a — Kern, `EPOS.Kern/Allgemein/Import/Sqproj/Standpruefung.cs`.**
- Vergleich: U je Bauteil über die GUID; Abweichung ab über 10 % (Schwelle `UWERT_ABWEICHUNG_HINWEIS`). Die Prüfung schlägt an, wenn die abweichenden Bauteile mehr als 5 % der Bruttohüllfläche ausmachen.
- Anzeichen eines anderen Stands: COPY-Eintrag im Journal der Projektdatei, jünger als der Modellstand (`StampEdit`) der IFC; Baujahr verschieden; gezeichnete Dicke ungleich Schichtsumme bei mehr als der Hälfte der abweichenden Bauteile.
- Wahl `Aufbauquelle { Offen, Projektdatei, Ifc }`: bei „Projektdatei“ kommen Aufbau und U aus der Projektdatei (`HerkunftU = Sqproj`); bei „Ifc“ gilt E98; bei „Offen“ lautet der Fehler `IMP_SQ_PROT_AUFBAUQUELLE_OFFEN`.
- Tests `SqprojStandpruefungTests` (10), `SqprojStandpruefungDiagnoseTests`.

**W1b — Dialog.**
- Baustein `EPOS.UI/Dialoge/Import/Aufbauquellenwahl.razor`: Abschnitt „Welche Aufbauten gelten?“ mit Anteil, Anzeichen und Tabelle je Bauteilart (Median-U beider Seiten), Beispiele aufklappbar.
- Drei gleichrangige Knöpfe ohne Vorwahl; „Abbrechen“ führt über einen Hinweis zu „Import beenden“ oder „Zurück“.
- Nach der Wahl die Zeile „Aufbauten: Projektdatei“ bzw. „IFC“ mit Wechselknopf; OK ist weich gesperrt.
- 22 Textschlüssel `GIMP_DLG_AQ_*` in beiden Sprachen.
- Tests `GebaeudeImportAufbauquelleDialogTests` (7); Probe `SqprojProbenErzeuger.Standhaus.cs`.

**W2 — Fensterrichtung aus dem eigenen Körper.**
- Fenster und Türen ohne Azimut, deren Wirt auch keinen hat, nehmen die Richtung aus dem eigenen Körper. Senkrechte Öffnung (Seite größer als Oberseite): Azimut und 90°; außen ist die Seite vom Raum der Öffnung weg, sonst gilt der Gebäudeschwerpunkt. Geneigte Öffnung: Neigung und Azimut der Oberseite. Waagerechte Dachfenster bleiben ohne Azimut; der Bauteilvorschlag verlangt bei 0° keinen.
- Die Info `IMP_IFC_PROT_ORIENTIERUNG_ERGAENZT` zählt „aus eigenem Körper“.
- Der Einheitenhinweis `IMP_IFC_PROT_UWERT_EINHEIT` entfällt, wenn ein anderer Satz desselben Bauteils denselben Wert in richtiger Einheit trägt (relativ ≤ 1e-3). Der Wert wird weiter verworfen; die Regel ist an keinen Programmnamen gebunden.
- Tests `IfcFensterrichtungTests` (6); Proben `ifc4_fensterrichtung.ifc`, `ifc4_fensterrichtung_unbestimmt.ifc`.
- Erwartung geändert: `IfcQuelldateienDurchgangTests`, eine Datei mit Z4/Z5 je +0,2 %.

## 4 Prüfung

- An der Anwenderdatei: Anteil 71 % der Hüllfläche, alle drei Anzeichen erkannt; mit der Wahl „Projektdatei“ ist das U der Außenwand gleich dem Weg „nur Projektdatei“ (0,00 %).
- In den lokalen Anwenderdateien bekommt 1 Fenster eine Richtung (Jahresheizwärme einer Datei +0,2 %); 44 Dachfenster sind waagerecht und bleiben ohne Azimut.
- Kein Rechenweg der Referenzprojekte, kein Schemaschritt, Basis unverändert.

## 5 Gate 825

GATEZAHLEN

## 6 Offen

- Sichtabnahme unter Windows: Länge des Abschnitts und Sichtbarkeit der drei Knöpfe mit aufgeklappten Beispielen; Tabellen bei schmalem Fenster; Verständlichkeit der Spalte „Abweichend“; Fokus und Rollposition nach der Wahl; Lage des Hinweises vor dem Beenden; Lesbarkeit der Anzeichen an einer echten Datei.
- Logbuch-Eintrag (Version beim Anwender erfragen); Wiki-Upload mit dem nächsten Sammel-Upload.
- Die Kommentare im Code nennen für die Rangfolge der Aufbauten teils „E97“; im Register führt sie E98 (E97 ist die Auslegungsheizlast).

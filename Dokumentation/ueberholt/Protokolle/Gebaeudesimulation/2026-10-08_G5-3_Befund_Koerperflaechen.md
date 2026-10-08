# Protokoll G5-3 — Befund je Bauteil, Farbmodus „Befund“, Bauteilflächen je Raum aus Körpern, IFC-Korrektur G5-3d (08.10.2026)

**Sitzung:** IFC / Gebäudeimport, Statuszeile **#814**. Zweig `g5-3`: 3a `f48341a14`/`d5edac556`, 3b `b09596272`/`2cfc67a73`, 3c `5606ff5d7` bis `5afafdd29`, Merge-Fix `84327615b`; G5-3d `8df4753be`, `bc0936b99`, `f38e59aa1`. Integration `integration-g5pk` `796afb72c`, im Hauptbaum `08ea6386e`.
**Entscheid:** E101; Abstimmung [G5 IFC](../../../aktuell/Gebaeudesimulation/2026-10-07_Abstimmung_G5_IFC.md). Abschnitt 8.

## 1 Anlass

Die Gebäudeansicht soll Bauteile zeigen, die für die Rechnung unvollständig oder fehlerhaft sind, und Gebäude ohne Raumgrenzen sollen ihre Bauteilflächen je Raum aus den Körpern bekommen statt des Rückfalls „schematisch“. Beim Abgleich zeigten sich zwei ältere Fehler des IFC-Wegs (G5-3d). Referenzprojekte unberührt.

## 2 Gebaut

### Kern

- **Befund je Bauteil:** Körper unlesbar, ohne Eigenschaften, ohne Bauteil; Farbtafel `GebaeudeAnsichtBefundstufen`.
- **Nettofläche 0:** Bauteile ohne Restfläche entfallen mit der Protokollzeile `IMP_BAUTEIL_PROT_NETTO_NULL_ENTFALLEN` (B2).
- **Bauteilflächen je Raum aus Körpern** (`Koerperflaechen.cs`), nur in Gebäuden ohne Raumgrenzen: Ebenenabgleich und Polygonschnitt ohne Geometriekern; Randbedingung aus der Gegenseite; Orientierung je Raumseite; Öffnungen nach Lage; Flächenherkunft `Koerper`; Raumfläche und Volumen aus dem Körper.

### Oberfläche

- Farbmodus „Befund“ in 3D und Grundriss mit Legende (Zahl und Fläche je Befund, Schalter zum Ausblenden); Kontrastmodus über Muster.
- Bauteilsteckbrief mit den Zeilen „Befund“ und „Herkunft der Fläche“.
- Zuordnungsdialog: Spalte „Fläche aus“ und Filter „Nur Bauteile mit Befund“.

### G5-3d — zwei Fehler des IFC-Wegs, die schon vor G5-3 bestanden

- Eine Körperdecke zwischen beheizt und unbeheizt entfällt, wenn die Datei dort eine Platte gegen unbeheizt führt (`IMP_IFC_PROT_KOERPERDECKE_HUELLE`); die Vorlage muss in der Höhe passen.
- Wände, deren Öffnungen ihren Mengensatz übersteigen, hängen die Öffnungen nach Lage um (`OeffnungenNachLage`; `IMP_IFC_PROT_OEFFNUNG_UMGEHAENGT`, `…_OEFFNUNG_OHNE_WAND`, `…_WAND_KLEINER_OEFFNUNGEN`).
- An einer Anwenderdatei gegen die Projektdatei: Grundfläche des Einzonen-Satzes −36 % auf 0,00 %, Außenwand netto −11 % auf +0,05 %.

## 3 Prüfung

- Proben: `ifc4_g5_befund.ifc`, `ifc4_g5_kleinhaus_ohne_mengen.ifc`, `ifc4_g5_kleinhaus_mit_mengen.ifc`; synthetische Proben `IfcProbenErzeuger.G5d.cs`.
- Heizwärme der Kleinhausprobe: Körperweg 10,090 MWh/a gegen 13,125 MWh/a im Rückfall ohne Raumzuordnung (−23 %).
- Sollwerte der lokalen Anwenderdateien nachgezogen: Mehrfamilienhaus (Datei A) Z4 +3,3 % (Körperweg); durch G5-3d Einzonenrechnung −4 bis −16 %, Z4 0 bis −3 %; Mehrfamilienhaus (Datei B) Dach −17 %, Grund −38 %.

## 4 Gate 814

GATEZAHLEN

## 5 Offen

- **Sichtabnahme unter Windows:** 3D-Befundbild mit `ifc4_g5_befund.ifc` (fünf orange Bauteile, Innenfläche halbtransparent); Kontrastmodus; Breite der Spalte „Fläche aus“; Verständlichkeit der Filter „Nur Flächen mit Befund“ und „Nur Bauteile mit Befund“.
- Gegliederte Wände je Teilfläche, Fenster über zwei Räume, Hanglage, mehrere Dachflächen über einem Raum, Öffnungen am Wandrand der Projektdatei ohne Aussparung (21 %).
- Logbuch-Eintrag (Version beim Anwender erfragen) und Wiki-Upload gebündelt.

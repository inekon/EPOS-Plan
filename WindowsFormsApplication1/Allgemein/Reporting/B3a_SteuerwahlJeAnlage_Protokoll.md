# B3a — Steuerwahl je Anlage, § 54 auf Kesselbrennstoff (Umsetzungsprotokoll)

Erster Teil der Etappe B3 (`Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md` §§ 4.2/5.2/8,
BF5/BF6). Stand 30.08.2026, Branch `Pufferspeicher`. Teil 2 (B3b: Hilfsstrom →
Nettostromerzeugung, Eigenstrom-Tatbestand je Anlage) folgt eigenständig.
Vorlauf: Faktenkarte-Erhebung (read-only) mit vollständiger Kartierung des
`SteuerGutschriftRechner` und der Modul-/Anlagenwelt.

## 1. Schema-Schritt 61 (M-2)

- `SchemaKatalog`: `SPALTE_EA_ENERGIESTEUER_WAHL` (TEXT(20)), `SPALTE_EA_AUFTEILUNG_METHODE`
  (TEXT(30)), `SPALTE_EA_HILFSENERGIE_ANTEIL` (DOUBLE) als `Schritt61_SteuerJeAnlage` an
  `Tab_Energieanlagen`; dazu `TAB_ERGEBNISHEIZKESSELMODUL` (neu — die Tabelle hatte nie
  eine Katalogkonstante) und `Schritt61_Hilfsenergie` (`Hilfsenergie` DOUBLE an beiden
  Modul-Ergebnistabellen). **Breiten von der Projektebene gespiegelt** (Schritt 20), nicht
  die TEXT(24) aus § 5.2 — sonst schnitte der Schreibweg. Beide Arrays bewusst nicht in
  `Alle` (Begründungsmuster Schritt 22/18).
- `SchemaMigration`: Schritt 61 = reines DDL (kein DML — NULL ist die Vorbelegung, jede
  Leseseite fällt zurück; exakt das Muster Schritt 22), **danach** `ZIEL_VERSION` 60 → 61,
  **neue Schritte ab 62**.
- Rückfallebenen bedient: `WirtschaftlichkeitCtrl.StelleTabellenSicher` (Anlagen-Spalten)
  und `ErgebnisCtrl.StelleModulSpaltenSicher` (Ergebnisspalten); `KwkgAnlagenCtrl` bleibt
  unverändert (sein generischer Leser hängt am Schritt-22-Array — verifiziert).
- Modelle: `ErgebnisBHKWModulModel`/`ErgebnisHeizkesselModulModel` + Feld `Hilfsenergie`
  (Schreibweg immer bedient, damit „erhoben und 0" von NULL unterscheidbar bleibt); Wert
  wird erst in B3b gebildet.

## 2. Steuerwahl je Anlage (BF6), Rückfall Projektwert

`SteuerAnlage` trägt jetzt `EnergiesteuerWahl`/`AufteilungMethode` (null/leer =
Projektwert, E6-Muster) und `Stromerzeuger` (Vorgabe true). `Energiesteuer(...)` löst die
Wahl **in** der Anlagenschleife auf:

- § 53/§ 53a sind an `Stromerzeuger` gebunden — **Kessel mit Wahl § 53/§ 53a rechnen 0**
  mit klarer Begründung („entlasten nur Anlagen mit Stromerzeugung").
- Die **§ 53a-Nutzungsgradprüfung bricht nicht mehr den ganzen Lauf ab**, sondern
  überspringt nur die betroffene Anlage (der Jahresnutzungsgrad bleibt bewusst
  Projektgröße — ein anlagenscharfer Wert wäre eine eigene Datenfrage).
- Der **Sockel 250 €/a** wird auf eine separat geführte § 54-Summe bezogen — ein
  § 53-Betrag derselben Rechnung zahlt nicht den Sockel einer anderen Anlage; bei reinem
  § 54 zeilengleich zum Bestand. Er bleibt einmal je Lauf (Kalenderjahresbetrag).
- Anlagen-Lesewege generisch: `LiesAnlagen(idProjekt, idType)` mit zwei Fähigkeitsstufen
  (E6 + B3a / nur E6 / ohne); Schreibweg für die neuen Spalten kommt mit dem B5-Dialog.

## 3. § 54 auf Kesselbrennstoff (BF5)

`BaueSteuerEingabe` ergänzt Kesselanlagen (`ID_Type = 10`) als zweite Quelle
(`KesselAnlagenErgaenzen`/`BaueSteuerAnlageKessel`/`KesselModulJeAnlage`), Träger/
Heizwert/Einheit über denselben Weg wie beim BHKW. **Bemessungsmenge leseseitig
abgeleitet**: `ErgebnisHeizkesselModulModel.Verbrauch` ist im gesamten Bestand 0 (der
Rechenkern setzt das Feld nie); die Ableitung `(Waerme_Gas + Waerme_Oel) ÷
(Jahresnutzungsgrad/100)` ist die **exakte Umkehrung** der Vorwärtsrechnung in
`SimulationSPK.Bilanz_und_Nutzungsgrad` (Nutzungsgrad in Prozent, geklemmt 1–108) —
keine Näherung, Herkunft wird ausgewiesen. Der Simulationspfad blieb unangetastet.

**Gate**: Kesselzeilen kommen nur in die Steuereingabe, wenn eine Wahl im Spiel ist
(Projektwahl ≠ KEINE oder Kessel-Anlagenwahl gesetzt) — sonst bekämen 26 von 28
Bestandsprojekten mit Kessel eine neue Hinweiszeile, ohne dass sich rechnerisch etwas
ändert. Die frühere Pauschal-Begründung „§ 54-Bemessung nur BHKW"
(`STEUER_ENERGIEST_54_BEMESSUNG`) wird nicht mehr erzeugt (Ressourcen-Keys bleiben).

`KohaerenzPruefung` Fall 3 versteht „Entlastung gewählt" jetzt als Projektwahl **oder**
irgendeine Anlagenwahl.

## 4. Nachweise (Harness `..\dev\b3a\`, frische Kopie; Handrechnungen exakt)

| Probe | Ergebnis |
|---|---|
| Build x64 | Exit 0, nur bekannte Altwarnungen |
| Migration 60→61 | läuft, idempotent, 5 Spalten da, **kein** DML-Effekt (0 von 145 Zeilen berührt) |
| Ergebnisneutralität | 1030 Vorgabe: KW −22.132.957,00 (B2-Basis exakt); 1030 Projektwahl § 53: EnergieSt **6.330,30** / KW −22.038.778,17 (B2-Messlage exakt); 1024 Betrieb 99,00 |
| Anlagenwahl | A=§ 53 (1.000 MWh), B=KEINE → **6.076,19** = Handrechnung; B=§ 53a (500 MWh, NG 85 %) → **8.517,71**; NG 50 % → 6.076,19 + Anlagen-Begründung statt Totalabbruch |
| Kessel § 54 | 500 MWh Wärme, NG 90 → 555,56 MWh × 1,38 €/MWh − 250 = **596,98** = Handrechnung; Gegenprobe Kessel=§ 53 → 0,00 mit Begründung |
| Verwaiste Modulzeilen | Ersatzweg mit Projektwahl, Modulnamen in der Begründung; § 54-Fall 2.121,56 = Handrechnung |
| Kohärenz Fall 3 | ohne Wahl → Hinweis; nur Anlagenwahl → kein (falscher) Hinweis mehr |
| Sweep | kein `<<<<<<<`-Treffer |

## 5. Randnotizen

- Die Produktiv-DB lief während der Etappe durch einen regulären App-Start des Anwenders
  (frisch gebauter Stand) auf **Schema 61** — Schritt 61 ist reines DDL, wirkungslos ohne
  Pflege; der Harness wies die Migration zusätzlich an einer auf 60 zurückgebauten Kopie
  nach.
- Neue Warn-/Begründungstexte laufen über das GetString-Rückfallmuster; die
  Ressourcen-Keys werden mit dem nächsten resx-Sammelnachtrag eingetragen (Designer bleibt
  wegen der parallelen Fremdarbeit unangetastet).
- Offen für **B3b**: `Hilfsenergie_Anteil`/`Hilfsenergie` existieren und werden
  geschrieben (0) — niemand bildet den Wert; Nettostromerzeugung und Eigenstrom-Tatbestand
  je Anlage unverändert Bestand.

## 6. Geänderte Dateien

```
Allgemein/Update/SchemaKatalog.cs              Schritt-61-Konstanten/Arrays, TAB_ERGEBNISHEIZKESSELMODUL
Allgemein/Update/SchemaMigration.cs            Schritt 61 (DDL), ZIEL_VERSION 61 (neue ab 62)
Allgemein/Wirtschaftlichkeit/SteuerGutschriftRechner.cs   Wahl je Anlage, Stromerzeuger-Bindung, 54-Sockel-Trennung
Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs    LiesAnlagen generisch, Kesselquelle, StelleTabellenSicher
Allgemein/Wirtschaftlichkeit/KohaerenzPruefung.cs         Fall 3: Projekt- ODER Anlagenwahl
Controller/ErgebnisCtrl.cs                     Hilfsenergie-Spalten (INSERT/SELECT/Vorsorge)
Model/ErgebnisModel.cs                         Hilfsenergie an beiden Modulmodellen
Allgemein/Reporting/B3a_SteuerwahlJeAnlage_Protokoll.md   dieses Protokoll
```

Harness (gitignored): `..\dev\b3a\`.

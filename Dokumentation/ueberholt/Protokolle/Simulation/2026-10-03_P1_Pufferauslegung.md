# Protokoll P1 — Pufferspeicher-Auslegung: Schema, Rechenkern, Controller (03.10.2026)

Auftrag des Anwenders vom 30.09./01.10.2026, ausgeführt über die Routine „Pufferspeicher-Auslegung: Konzept und Bau
P1“ am 03.10.2026 ab 06:00 MESZ. Grundlage: [`Konzept_Pufferspeicher_Auslegung_EPOS-Plan.md`](../../../aktuell/Konzept_Pufferspeicher_Auslegung_EPOS-Plan.md)
(Rev. 1, Commit `81a5e4bc`) und die vier Recherchepapiere unter `Dokumentation/aktuell/Pufferspeicher/`. Das
Mockup `Dokumentation/aktuell/Mockups/Pufferspeicher_Auslegung_Mockup.html` ist in derselben Welle auf die Recherche
nachgezogen (`4c7ab4cc`). Statuszeile **#675**.

## 1 Was gebaut ist

| Welle | Commit | Inhalt |
|---|---|---|
| W1 | `1a51ceda` | `PufferAuslegungSchema` (`EPOS.Kern/Allgemein/Update/`): `Tab_PufferAuslegung` (STRICT, je Projektpuffer, NULL = Vorgabe, `ID_Projekt` CASCADE, `ID_Pufferspeicher` SET NULL) und `Tab_PufferAuslegungParameter_STAMM` (STRICT) mit 148 gesäten Vorgabewerten `Pufferauslegung.*` aus `PufferAuslegungVorgaben` (57 Grundwerte, je Vorlage 9 Kriterienschalter und 4 Beispielwerte); Registrierung in `SchemaStand`, `SchemaMigration` (Windows-Schale), `Paketanhebung.STUFEN` (DDL), `TestDatenbank.SchemaNachziehen`, `Werkzeuge/Testdatenbankschema`; `PufferAuslegungParameter.Lesen()` mit Rückfall auf die eingebauten Werte; `PufferAuslegungSchemaTests` |
| W2 | `4bd1c707` | Rechenkern `EPOS.Kern/Allgemein/Pufferauslegung/`: `PufferAuslegungEingang` (record), `HeizzoneRechner` (K1–K4, K4e, K8–K11, Band), `Betriebssimulation` (Durchlauf mit Bisektion auf das Deckungsziel, Zweipunkt mit Schwellen und Modulationsmodus, Bisektion auf Starts je Tag), `BrauchwasserzoneRechner`, `ProzesszoneRechner`, `Nutzungsprofil`, `PufferAuslegungErgebnis`, Fassade `PufferAuslegung.Rechnen`; 44 datenbankfreie Tests (`Betriebssimulation`, Handrechnungen, Fassade) |
| W3 | `5aee63c0` | `EPOS.Kern/Controller/PufferAuslegungCtrl`: `Vorbelegen` (Puffer, Kaskade, Erzeuger, Gebäude, Einstellungen, Zapfprofil über `ZapfprofilCtrl.Auslegung`, Prozessdaten, Konditionierung; Herkunftsliste je Feld; gespeicherte Zeile überschreibt spaltenweise), `Reihen` (Vorprüfen, Bedarf, `KanaeleDrei()`), `Rechnen`, `Durchrechnen`, `Speichern` (UPSERT), `Uebernehmen` (über `PufferSpCtrl`), `Vorlagen`, `Katalog` |
| W4 | `69bec6b7` | `PufferAuslegungCtrlTests` (11 Fälle mit Testdatenbank: 1045 Kombi aus dem Zapfprofil, 1041 Prozesszone, 1030 BHKW, 1047 Übergabeart; Übernahme nur auf einer Projektkopie, Referenzpuffer unberührt; Duplizieren und Export → Import tragen die Zeile mit umgesetzter Puffer-ID) |
| Merge | ``4cec25d6`` | Zusammenführung mit `origin/ios_migration_september` (`3baa73ca`, Statuszeilen #671–#674, Schemaschritte 167 Teillast und 168 Viertelstunden, Basis R33): Umnummerierung des Schritts auf **169**, Testdatenbank aus der Fassung `6e5d24aa…` (168) auf 169 gehoben |

## 2 Schemaschritt 169

`PufferAuslegungSchema.SCHRITT = StromViertelstundenSchema.SCHRITT + 1`. Wiederholbar (`IF NOT EXISTS`, `INSERT OR
IGNORE`), ergebnisneutral: kein Rechenweg liest die Tabellen. `ProjektDuplizierenCtrl.KINDER` braucht keinen Eintrag
(eigene `ID_Projekt`; `ID_Pufferspeicher` steht in `FK_MAP`). Testdatenbank: Schemastand 169, 81 240 064 Byte,
LFS-SHA-256 `5fdc093e38eb755c4d3fc598c57022180409763cdc7a05e6a069d20dd3fb6d29`; `integrity_check` ok,
`foreign_key_check` leer; Nachtrag in `Referenzlaeufe/LIESMICH.md`. Das Konzept nannte den Schritt 167 und die
Basis R32; beides hat der Arbeitszweig am selben Tag vergeben (M4, M5), die Nummern sind 169 und R33.

## 3 Festlegungen beim Bau (ergänzen das Konzept)

- **Zusätzliche Vorgabeschlüssel:** `Sperrzeit.Raumtemperatur_C` 20 (ϑ_R in Gleichung 23), `WP.Mindestleistung_Anteil`
  0,3 (V39), `Puffer.Schwelle_Ein`/`_Aus` 0,10/0,95; Zonenschlüssel `Zonen.Vorgabe_Oben/_MitteOben/_MitteUnten/_Unten`.
- **Nutzbarer Anteil:** K4 nach Gleichung 23 rechnet ohne η_s (die Richtlinie trägt die Reserve in dT_SP); K3, K4e, D1
  und die Volumenkriterien mit η_s; D2 bildet η_s über die Schwellen ab. Deckt sich mit der Festlegung im Mockup.
- **Tabelle 14:** Heizgrenze zwischen zwei Zeilen nimmt die vorsichtige Zeile; Konvektor und Lüfter rechnen wie Radiator.
- **Modulationsmodus:** das Gerät schaltet ab, wenn der Puffer voll ist und die Last unter P_min liegt. Starts je Tag =
  Starts in Bedarfsstunden ÷ (Bedarfsstunden ÷ 24). Heizperiode = Stunden mit Heizbedarf > 0.
- **D1/D2 ohne erreichbares Ziel** (auch an der Praxisgrenze): Kriterium bemisst nicht, `PA-PRAXISGRENZE`.
- **Zirkulation:** Tageswert geht als Q_Z,d · D_max / Q_TWW,d ins Defizit ein; Weg ANTEIL = 0,35 · D_max.
- **Hygiene:** W 551 greift über 400 l, Temperaturgrenze 55 °C (Konstanten im Code).
- **Bestandsweg ohne Generator:** D_max als größtes Tages-Kumulationsdefizit gegen gleichmäßige Ladung im Tagesmittel,
  Topologie Frischwasserstation. Mehrere Topologiegruppen des Generators werden summiert, Topologie aus der ersten.
- **Erzeuger:** mehrere Anlagen des Rang-1-Typs summieren sich zur Nennleistung; alle übrigen Nicht-Solar-Erzeuger
  gelten als Zweiterzeuger; WP gilt als bivalent bei Kessel, Heizstab oder `Bivalenter_Betrieb`; WP moduliert nur bei
  `Regelung = stetig`, Kessel nur bei `Mindestleistung > 0`; Sperrfenster nur mit gesetztem `Sperrung`-Kennzeichen.
- **Konditionierungs-Nutzung:** kein direkter Verweis vom Gebäude auf die Kalendervorlage; die Nutzung wird über den
  Vorlagennamen in der Kalenderbemerkung (`Kalenderherkunft.AusBemerkung`) gefunden (ungetestet, Testdatenbank ohne Zeilen).
- **Übernehmen:** leere Schwellen werden als Vorgabe 10/95 zurückgeschrieben (der Wert, den die Simulation liest);
  `Tab_Pufferspeicher` hat keine Katalogverweis-Spalte, der Katalogvorschlag liefert nur den Bereitschaftsverlust.
- **K11 Klasse C bei 1 000 l:** 148,68 W = 3,568 kWh/d (Konzept nennt gerundet 3,569).

## 4 Befunde

- Ergebnisse auf der Testdatenbank: 1045 Kombipuffer — Brauchwasserzone 500 l aus der Zapfprofil-Auslegung,
  Empfehlung 800 l; 1041 neuer Puffer — Prozesszone ≈ 291 l (D2), Empfehlung 400 l; 1030 — Vorlage BHKW,
  bemessend Volumenkriterium, Empfehlung 10 000 l. Jedes Projekt rechnet unter einer Sekunde.
- Export/Import: nichts nachzuziehen — Duplizieren und Projekttransfer laufen über denselben Plan
  (`ProjektDuplizierenCtrl.ErmittlePlan`, jede Tabelle mit `ID_Projekt`); Test belegt die Mitnahme.
- Welle M4 (Schritt 167) führt jetzt `Mindestleistung_kW` und Taktverlust an der Wärmepumpe und Mindestlaufzeit am
  BHKW: Der Controller kann diese Felder als Vorbelegung lesen (P2); V13 Startzähler ist für die WP damit teilweise erledigt.

## 5 Nachweise

Gate des Agenten vor dem Merge (Stand `69bec6b7`, Basis R32): Kern-Filter 0 Fehler; Tests Kern 10 185 (1 übersprungen),
UI 7 284, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen); Windows-Schale auf Linux 0 Fehler;
Referenzlauf 16/16 PASS, 487/487 CSV byte-gleich; SQL-Prüfer 0 Fundstellen (2 171 Texte).

Gate auf dem Merge-Stand (``4cec25d6``, Basis R33, Schemastand 169): Kern-Filter 0 Fehler; Tests Kern 10 245 (1 übersprungen), UI 7 302, KiKern 549, SpeicherEngine 397, SpeicherPlanung 27 (1 übersprungen), alle grün; Windows-Schale auf Linux 0 Fehler; Referenzlauf 16/16 PASS gegen , 487/487 CSV byte-gleich; SQL-Prüfer 2 201 Texte, 0 Fundstellen.

## 6 Offen (P2/P3)

Ressourcentexte de/en für Herkunftstexte und Warncodes (`PA_*`); `ID_Konditionierungsvorlage` am Gebäude statt
Umweg über die Bemerkung; Katalogverweis am Projektpuffer; Oberfläche mit drei Einstiegen, Hülle in `EPOS.UI.Daten`,
Laufzeitprobe von `Durchrechnen`; Vorbelegung aus den Teillastfeldern der Welle M4; Bericht, Wiki, Logbuch (P3).

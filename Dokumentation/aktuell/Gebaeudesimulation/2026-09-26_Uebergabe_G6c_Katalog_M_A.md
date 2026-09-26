# Übergabe: G6c Zonenimport und Vorgaben der Baualtersklassen M und A — Stand 26.09.2026

**Zweck.** Das Wochenkontingent des Kontos steht bei rund 78 %; bei 80 % übergibt der Anwender, die
Folgesitzung beginnt mit diesem Papier. Es sagt, was fertig ist, was aussteht und welche Regeln gelten. Es
ersetzt kein Konzept; die Sachlage steht in den Papieren selbst.

**Auftrag (Anwender, 26.09.2026, im Wortlaut):**

> „Kleine Nacharbeiten an G4b, dann G6c, Zonenimport mit mehreren Zonen aus IFC/gbXML, U-Wert-Vorgaben für
> Neubauten ab 2021"

## 1 Was fertig ist

- **G4b samt Namensabgleich** ist abgeschlossen und gepusht bis `01f443e0`, CI grün: Schemaschritt 146
  (Synonyme), [Protokoll G4b](../../ueberholt/Protokolle/Gebaeudesimulation/2026-09-25_G4b_Bauteilimport.md),
  [Leitkonzept](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.49.
- **Nacharbeiten G4b:** STAND_NACHARBEITEN
  - Umfang: Azimut runden, vorgemerkte Zuordnungen entfernter Importzeilen verwerfen, Ansicht der gemerkten
    Baustoff-Zuordnungen je Projekt.
- **E50** (26.09.2026, Leitkonzept N1.57): G6c beauftragt; **M7** (a) je Geschoss, Rückfall auf eine Zone,
  wenn die Raumgrenzen fehlen; **M8** (a) Mindestgröße max(2 m², 2 %) mit Zuschlag zum Nachbarn mit der
  größten gemeinsamen Fläche; **M12** (a) 50 Zonen als Vorgabe, Warnung mit Rückfrage und Vorschlag einer
  gröberen Regel; **M13** (a) vollständige Rekonstruktion der Nachbarschaften über die Geometrie. Alle nach
  Empfehlung; vor G6c ist kein Anwenderentscheid mehr offen. Das
  [Register](../Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) zählt nach dem Merge mit G6b (E49: M3, M5
  und M6) 1 offenen Punkt (M11, vor G6d).
- **E51** (26.09.2026, Leitkonzept N1.58): Klassen und Standards ohne Katalogsatz bekommen **freie Werte aus
  Stein/Loga (2025)**, die Klassen **M und A eigene Katalogsätze**; F4 des
  [Konzepts Baualtersklassen](../Konzept_Baualtersklassen_Energiestandard_EPOS-Plan.md) ist aufgehoben, E27
  bei U12 geändert.
- **Nachgezogen:** Leitkonzept N1.57 und N1.58; Register (Kopf, Kapitel 0, U12, M7, M8, M12, M13,
  Kapitel 9); [Statusdatei](../Status_Gebaeudesimulation_VDI6007.md) (Kopf, E50, E51, G4, G6c, Papiere);
  [Mehrzonenkonzept](../Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) (Kopf, 2.9, 6.1, 6.2, 6.5, 6.6, 10); Konzept
  Baualtersklassen (Kopf, 4, F4).
- **G6b** (Sitzung AK1, „Gebäudesimulation EPOS-Plan") ist umgesetzt und in diesen Stand gemergt (W0 bis W5,
  Leitkonzept N1.55, N1.56): Schemaschritt 147 (`ID_Nachbarzone`, `Trennflaeche_Zuordnung`,
  `Tab_Zonenluftstrom`, `Tab_ErgebnisZone`), Randbedingung `ZONE`, Zonenschleife, Ergebnis je Zone in
  Bedarfsdialog, Bericht und Export. Laufgrenze und Freigabeschalter sind gestrichen; eine Grenze
  `GebaeudeZonenregeln.PFLEGEGRENZE` (50) gilt für Pflege und Lauf (N1.56 Nr. 13). **E49** hat M3, M5 und
  M6 entschieden.

## 2 Was aussteht

### 2.1 G6c — Zonenimport mit mehreren Zonen aus IFC und gbXML

**Grundlage:** Mehrzonenkonzept 6 (Zonierung, Grenzflächen, Materialien, Zuordnungsdialog, Rückfälle,
Fehlerbilder, Grundriss) und 9 (Stufe G6c, 16–26 PT ohne X1…X3);
[Datenaustauschkonzept](../Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 3.3 (gbXML-Regeln X1…X4, nach
D16 mit G6c); Register M7, M8, M10, M12, M13 (alle entschieden).

**Abhängigkeit von G6b W5 — erfüllt.** W5 ist gemergt: Der Lauf rechnet bis zu 50 Zonen, gespeichert wird
über die Pflege aus G6a. Proben mit Jahresrechnung mehrerer Zonen gehören damit zur Abnahme der Wellen.

**Vor dem Beginn zu klären.** Kein Anwenderentscheid ist mehr offen; zu klären bleiben:

- **Messungen** (Mehrzonenkonzept 6.1, 6.2): `IfcSpatialZone` mit `PredefinedType = THERMAL` in den
  Messdateien, `ParentBoundary` im DigitalHub, Profiltyp der zwei `IfcSurfaceOfLinearExtrusion`.
- **M10:** Die Lizenz der `…_with_SB`-Fassung ist vor dem ersten Commit der großen Testdatei nachzufragen;
  bis dahin wird nur außerhalb des Repositoriums gemessen, und die Datei kommt nur mit der LFS-Zeile im
  selben Schritt.
- **Aufwand X1…X3** beziffern (Mehrzonenkonzept 9) und dem Anwender mit dem Wellenplan nennen.

**Wellen.**

| Welle | Inhalt | Abnahme |
|---|---|---|
| **A — Zonierung im Kern** | Regelkette Z1…Z5 mit Vorbelegung **Z4 je Geschoss** und Rückfall Z5 ohne Raumgrenzen (M7, 6.1, 6.5); gbXML X1…X3 neben dem vorhandenen X4 (X2 entspricht Z4); Beheizung B1…B6; **Mindestgröße** nach M8: eine Zone unter max(2 m², 2 % der Gebäudegrundfläche) wird dem Nachbarn mit der größten gemeinsamen Grenzfläche zugeschlagen, ohne Nachbarn bleibt sie mit Warnung stehen (6.1, Fehlerbild „zu klein"); **Obergrenze** nach M12: über 50 Zonen Warnung mit Rückfrage und Vorschlag einer gröberen Regel (auf Geschosse zusammenlegen, 6.6), die Rechnung lehnt darüber benannt ab (2.9), Konstante `GebaeudeZonenregeln.PFLEGEGRENZE` aus E46/A3; **Paarbildung der Raumgrenzen über die Geometrie** nach M13 (6.2: Kandidaten über dasselbe Bauteil, eindeutig bei einem Kandidaten, sonst Flächeninhalt innerhalb 1 % und Schwerpunktabstand kleiner als die Bauteildicke in Weltkoordinaten, sonst „unbekannt"); Randbedingung je Grenze (Tabelle 6.2); Öffnungsabzug je Fläche; **Gegenprobe** der Trennflächen A→B gegen B→A ab **2 %** und geschlossene Hülle je Zone; Fehlerbilder 6.6 mit Schlüsseln in beiden `.resx`. Plattformfrei, ohne Oberfläche | Proben 13, 14, 16 und 18 (Mehrzonenkonzept 8.2), 15 mit angereicherter Datei; gbXML-Probe 9 (Datenaustauschkonzept); der Einzonenfall Z5 bzw. X4 bleibt gleich dem Vorschlag aus G4b |
| **B — Vorschlag mehrerer Zonen** | aufbauend auf `GebaeudeBauteilvorschlag` (G4b) und dem Namensabgleich: je Zone Bauteile, Aufbauten und Schichten; Trennflächen mit `ID_Nachbarzone` und Randbedingung `ZONE` aus G6b; `VIRTUAL`-Grenzen als Vorschlag eines Zonen-Luftaustauschs (2.7); Persistenz der Zuordnung Zone ↔ Kennung der Datei (Kapitel 6, Datenaustauschkonzept 7); `GebaeudeZonenabbildung` und `GebaeudeZonensatz` für N Zonen; Speichern über die G6a-Pflege — Abgleich über Ids in einer Transaktion (A6) | Summe der Zonenflächen gegen die Raumflächen (5.3); Rundlauf Import → Pflege → Lauf; Einzonenweg unverändert |
| **C — Zuordnungsdialog mit mehreren Zonen** | Mehrzonenkonzept 6.4: Kopf mit Zonenregel und Bilanz, Zonenliste (zusammenlegen, trennen, beheizt), Flächen je Zone mit Filtern, Baustoffe (aus G4b vorhanden); Listen als virtualisierte `Katalogliste`; Texte in beiden Sprachen, danach `ResourceDesigner` | bunit; **Rasterprobe Pflicht** (neue Spaltenart oder Zeilenhöhe, Hinweis unten); Windows-Schale kompiliert; Sichtabnahme unter Windows durch den Anwender |
| **D — Zonengeometrie-Modell und 2D-Grundriss (E11)** | Kern: `Zonengeometrie` mit `Zonenumriss` (Softwarearchitektur 1.3), Polygone aus den Raumgrenzen, ohne Grenzen die Rechteckherleitung mit sichtbarem „schematisch"; Oberfläche: `GebaeudeAnsicht.razor` als SVG ohne Bibliothek, Klick ordnet einen Raum der gewählten Zone zu (6.7). Dasselbe Modell speist später G7b | bunit der Komponente; Determinismus der Geometrie (6.7) |
| **E — Papiere und Wiki** | Leitkonzept-Nachtrag mit den Festlegungen der Umsetzung G6c; Mehrzonenkonzept (Kopf, 6, 8.2, 9); Datenaustauschkonzept 3.3; Statusdatei, Register (Vermerke M7, M8, M12, M13 fortschreiben), Protokoll unter `ueberholt/Protokolle/Gebaeudesimulation/`; Wiki-Quelle „Gebäudeimport" und Logbuch-Entwurf, veröffentlicht mit der Auslieferung mehrerer Zonen | Linkwache, Wiki-Gegenlesen nach dem Muster aus `CLAUDE.md`, Produktdatenwache |

**Dateien.**

- **Mit G6b W5 geändert** — W5 ist gemergt, die Dateien sind frei; vor dem Anfassen origin holen: `GebaeudeModellErgebnis`, `ErgebnisCtrl`,
  `GebaeudeErgebnisexport`, `Vdi6007Rechenweg`, `GebaeudeZonenregeln`, `GebaeudeBedarf*`,
  `BausteineProjekt`, die `.resx`, die KI-Texte; die Papiere Leitkonzept, Mehrzonenkonzept 8.1, ADR-005,
  Statusdatei, Register und `Dokumentation/LIESMICH.md`.
- **Frei für G6c:** `GebaeudeZonensatz`, `GebaeudeZonenabbildung`, `ZonenSchema`, Zonen- und Bauteildialog,
  der Importcode (`EPOS.Kern/Allgemein/Import/`, `GebaeudeImportHuelle`, `GebaeudeImportDialog`).
- Neue Texte gehören in die `.resx`; Konflikte mit W5 werden als Vereinigung beider Seiten gelöst.

**Abnahme jeder Welle.** `dotnet build WP-Plan.Kern.slnf`, Tests der betroffenen Klassen, der volle Lauf
als Gate; die Windows-Schale kompiliert, sobald eine Hülle oder Naht berührt ist; `SqlDialektPruefer` nach
jeder neuen SQL-Anweisung; **Referenzlauf der 14 Projekte byte-gleich** gegen die aktuelle Basis (kein
Referenzprojekt trägt importierte Zonen; eingefroren wird erst mit G6d); CI `kern.yml` grün. Das
Mehrzonenkonzept nennt für G6c einen iOS-Lauf nach Rückfrage — nach der Regel dieser Sitzung nur auf
ausdrücklichen Zuruf des Anwenders.

### 2.2 E51 — Vorgaben der Klassen ohne Katalogsatz, Katalogsätze für M und A

**Entscheid** (Leitkonzept N1.58): Hat eine Klasse oder ein Energiestandard eigene Katalogsätze, gilt deren
Median (E27 insoweit); nur ohne Katalogsatz gilt der freie Wert aus Stein, B.; Loga, T. (2025): *Das
Typgebäude-Modell zur energetischen Bewertung des Wohngebäudebestands*, IWU im Auftrag des BBSR, Zenodo,
Record 15488271, CC BY 4.0 — sichtbar mit Herkunft und Beleg, nie still. Dazu eigene Katalogsätze für M und
A. Die IWU-Typologie 2015 bleibt unfrei; von ihr nur die Jahresgrenzen.

**Schritte.**

1. **Quelle lesen.** Die PDF (4,38 MB, Download vom Anwender freigegeben) nur lokal ablegen, nie ins
   Repositorium. Festhalten, welche Klassen, Bauteile und Größen (U-Werte, g-Wert, ψ) sie liefert, ob Klasse
   A enthalten ist und wie die Werte eines Wohngebäudemodells für Nichtwohngebäude gelten.
2. **Sätze entwerfen und beim Anwender bestätigen lassen.** M etwa Neubau nach GEG-Mindeststandard (GEG
   Anlage 1 Referenzgebäude Wohngebäude bzw. Anlage 2 Nichtwohngebäude, Fassung und Fundstelle belegen) und
   nach Effizienzhaus 55 (technische Mindestanforderungen der Bundesförderung, belegt); A typische
   Altbauten. Neutrale Namen nach dem Namensmuster des Katalogs, keine Hersteller- oder Produktdaten; festlegen,
   ob die Sätze einen Energiestandard tragen (dann bekommt auch dessen Zeile eine Katalogvorgabe).
3. **Saat.** Schemaschritt mit festen Ids und `ReadOnly = 1`, der nur anlegt, was fehlt — nächster freier
   Schritt heute **149**, spät gegen origin prüfen (G6b vergibt ebenfalls Nummern) —, oder Pflege in der
   produktiven Datenbank vor `Werkzeuge/Auslieferungsvorlage`. Die Testdatenbank braucht die Sätze in jedem
   Fall, weil `GebaeudeVorgabenTests` die Mediane aus ihr nachrechnet.
4. **`GebaeudeVorgaben` mit Vorrang und Rückfall:** Standard → Klasse (Median der Katalogsätze) → freier
   Wert; die Tabelle der Mediane neu schreiben (der Test gibt sie bei Abweichung aus), die Tabelle der freien
   Werte daneben, jeder Wert mit Quelle.
5. **Herkunft und Beleg:** Der freie Wert erscheint in der Feldzeile des Imports mit eigener Herkunft und
   einer Herleitungszeile samt Quellenangabe, dazu eine Meldung; nie ein stiller Wert, nie ein Wert der
   Nachbarklasse.
6. **Tests:** Der Bauteilvorschlag lehnt einen Neubau der Klasse M ohne U-Werte und Schichten nicht mehr ab
   (ebenso Klasse A, soweit die Quelle sie trägt); Vorrang Katalog vor freiem Wert; Herkunft und Beleg; die
   Migration ist idempotent; Prüfbericht der Auslieferungsvorlage; `SqlDialektPruefer`.
7. **Lizenzhinweis:** Quellenangabe nach CC BY 4.0 in `Setup/Vorlage/Lizenzhinweise.txt`.
8. **Wiki:** Seiten „Gebäude" und „Gebäudeimport" (Repo-Quellen) mit Quellenangabe, neutrale Beispiele;
   Logbuch-Entwurf, veröffentlicht mit dem nächsten Sammel-Upload.
9. **Referenzlauf** der 14 Projekte byte-gleich (kein Referenzprojekt nutzt die neuen Sätze, kein Rechenweg
   liest Klasse oder Vorgabe); ob neue Katalogsätze unter die Einfrierregel „gesäte Gebäudedaten" fallen,
   prüfen und in `Referenzlaeufe/LIESMICH.md` vermerken.
10. **Papiere:** Konzept Baualtersklassen 4 (Satzzahlen, Quelle), Umsetzungskonzept 5 (U12) und die übrigen
    Stellen, die U12 zitieren; Statuszeile E51 fortschreiben.

E51 ist von G6c unabhängig und kann in einem eigenen Worktree parallel laufen. Berührungspunkte: die
Testdatenbank (Konflikt über die origin-Fassung und die Skripte lösen) und die Schemanummer.

## 3 Regeln dieser Sitzung

- **Agenten:** Opus-Agenten (`model: opus`, ausdrücklich gesetzt) im eigenen Worktree, Aufträge
  repo-relativ; Agenten committen auf ihrem Zweig, pushen nicht.
- **Push:** nach grünem Gate committen und pushen, ohne Rückfrage (Anwenderregel). Reihenfolge Merge → Gate
  → Statuszeile und Protokoll → Push.
- **Keine iOS-Läufe** und kein macOS-Lauf.
- **Nummern spät prüfen:** Schemaschritte, Entscheide (E…) und Nachträge (N1.…) kurz vor dem Commit gegen
  origin prüfen, eigene umnummerieren, sofort pushen.
- **`.resx`-Konflikte** als Vereinigung beider Seiten lösen, danach `ResourceDesigner`.
- **Wochenlimit:** bei 80 % die Übergabe vorbereiten — committen, pushen, Status und Übergabe schreiben —
  statt weiterzuarbeiten; vor jedem Agenten den Verbrauch prüfen.
- **Rasterprobe mit Playwright:** `npm install playwright-core@1.58.0 --prefix <ordner>`, dann
  `NODE_PATH=<ordner>/node_modules node rasterprobe.mjs --kanal msedge`
  (siehe [`Proben/Rasterprobe/LIESMICH.md`](../../../Proben/Rasterprobe/LIESMICH.md)).

## 4 Fundstellen

| Was | Wo |
|---|---|
| Entscheide E50 und E51 | [Leitkonzept](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.57, N1.58; [Statusdatei](../Status_Gebaeudesimulation_VDI6007.md) Abschnitt 1 |
| Zonierung, Grenzflächen, Rekonstruktion, Dialog, Grundriss | [Mehrzonenkonzept](../Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 6.1–6.7, Proben 8.2, Stufen 9, Fragen 10 |
| gbXML-Zonenregeln X1…X4 | [Datenaustauschkonzept](../Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 3.3; D16 im Register Kapitel 4 |
| Entschiedene Punkte M7, M8, M12, M13 (G6c, E50) und M3, M5, M6 (G6b, E49); offen M11 (G6d) | [Register](../Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) Kapitel 3 |
| Bauteilimport und Namensabgleich (G4b) | [Protokoll G4b](../../ueberholt/Protokolle/Gebaeudesimulation/2026-09-25_G4b_Bauteilimport.md); Leitkonzept N1.49 |
| Zonenpflege (G6a) | [Protokoll G6a](../../ueberholt/Protokolle/Gebaeudesimulation/2026-09-25_G6a_Zonenpflege.md); Leitkonzept N1.50, N1.51 |
| Baualtersklassen, Vorgaben, F4 | [Konzept Baualtersklassen](../Konzept_Baualtersklassen_Energiestandard_EPOS-Plan.md) 4 und 8; [Protokoll G4](../../ueberholt/Protokolle/Gebaeudesimulation/2026-09-24_G4_Importe.md) Abschnitt 16 |
| Vorgaben im Code | `EPOS.Kern/Allgemein/Import/Gebaeude/GebaeudeVorgaben.cs`, `GebaeudeBauteilvorschlag.cs`, `Baujahrregel.cs`; `EPOS.Kern.Tests/GebaeudeVorgabenTests.cs` |
| Zonen im Code | `EPOS.Kern/Allgemein/Simulation/Gebaeude/GebaeudeZonensatz.cs`, `GebaeudeZonenabbildung.cs`; `EPOS.Kern/Allgemein/Update/ZonenSchema.cs`; `EPOS.Kern/Allgemein/GebaeudeZonenregeln.cs` (W5) |

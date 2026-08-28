# Konzept: Projekttransfer — Export und Import zwischen Rechnern (EPOS-Plan)

**Rev. 2 (zur Abnahme)** · 28.08.2026 · Branch-Stand nach Synchronisation

Auftrag: „Ich möchte ein Projekt mit der Option, auch die Varianten davon zu
exportieren, so dass sie auf einem anderen Computer importiert werden können."
**Nutzerhinweis zur Rev. 1:** Es gibt bereits einen Export/Import — dieser soll
genutzt und erweitert werden; der gegenwärtige Import bricht mit einer
Fehlermeldung ab (Befund B1). Rev. 2 ersetzt Rev. 1 vollständig: Das Konzept
baut jetzt auf dem Bestand auf statt neu.

---

## 1. Ziel

Ein Projekt — wahlweise samt seiner Varianten — wird über den **vorhandenen
Dialog** „Projekt exportieren / importieren" in eine Transportdatei (`.wpx`)
exportiert und auf einem anderen Rechner als neues Projekt importiert. Nach dem
Import rechnet und zeigt das Projekt dort identisch. Der bestehende Weg wird
repariert (B1) und um die Varianten-Option, Anker-/Versions-Härtung und
Vorschau/Bericht erweitert — kein Parallelbau.

## 2. Bestand — was schon da ist

`Controller/ProjektExportImportCtrl.cs` (985 Zeilen) und
`Views/Projekt/Form_ProjektExportImport.cs` (Dialog mit Reitern
Exportieren/Importieren, Endung `.wpx`):

- **Paketformat**: ZIP mit `manifest.json` (format „wp-projekt", formatVersion 1),
  `data/<Tabelle>.json` je Projekttabelle, `catalogs/` (Katalogzeilen mit
  Natural-Key-Auflösung über `KATALOG_SPALTE_ZU_TABELLE` /
  `KATALOG_NATURALKEY`) und `fill/` (per Original-ID aufzufüllende
  Referenzzeilen nicht kopierter Katalogtabellen).
- **Exportplan = Duplizierer**: `ProjektDuplizierenCtrl.ErmittlePlan` liefert
  die Tabellenliste (ID_Projekt-/ProjektID-Tabellen, feste `KINDER`,
  FK-Auto-Erkennung) — eine Wahrheit, wie in Rev. 1 gefordert; das ist bereits
  gebaut.
- **Import**: Modi Abbrechen / Überschreiben / Neuer Name; Offsets je Tabelle
  (`MAX(Ziel) − MIN(Paket) + 1`); Umschlüsselung von PK-, FK- und
  Katalogspalten; Spalten-Schnittmenge (tolerant gegen Schemadrift);
  FK-Vorabbehandlung + Selbstheilung (`NulleVerwaisteFks` mit genau einem
  Wiederholungs-INSERT); ausführliche Fehlerdiagnose (Spalten, Typen, Werte,
  FK-Prüfung in der Transaktion); alles in EINER Transaktion; danach
  `ReseedAutoWerte` (AutoWert-Zähler auf MAX+1 — Schutz vor Fehler 3022).

Diese Substanz bleibt; die Etappen unten reparieren und erweitern sie.

## 3. Befunde am Bestand

**B1 — Importabbruch bei der Ergebnisfamilie (Screenshot 28.08.2026):**
`INSERT Tab_ErgebnisEnergiebedarf` scheitert an der erzwungenen Beziehung
`ID_Ergebnis → Tab_Ergebnis[ID]` („>>> FEHLT <<<") — die Kopfzeile ist zum
Einfügezeitpunkt nicht in der Ziel-DB. Die Ergebnisfamilie hängt als
Kopf + Detailtabellen zusammen (`Tab_Ergebnis` mit `ID_Projekt`;
`Tab_ErgebnisEnergiebedarf` u. a. nur über `ID_Ergebnis`). Verdachtslinien,
die T1 hart reproduziert und schließt:
1. Die **Einfügereihenfolge** folgt der `ErmittlePlan`-Reihenfolge des
   Manifests; sie ist nicht garantiert topologisch (Eltern vor Kindern),
   insbesondere wenn die FK-Landkarte (`LiesFremdschluessel` /
   MSysRelationships) auf einem Rechner leer oder unvollständig zurückkommt —
   gegen genau diese Unzuverlässigkeit kämpft der Import heute schon mit einer
   Zweitverbindung.
2. **Kopf ohne Zeilen, Detail mit Zeilen** im Paket (Export überspringt leere
   Tabellen; ohne Offset für `Tab_Ergebnis` bleibt `ID_Ergebnis` unversetzt).
**UMGESETZT (28.08.2026) — die echte Ursache lag tiefer:** Die Analyse der
Nutzerdatei (`Booster-Kette mit Kombi-Speicher.wpx`) zeigte Reihenfolge und
Paketinhalt in Ordnung; der Detail-PK wurde versetzt, der Verweis
`ID_Ergebnis` aber nicht. Wurzel: Die Umschlüsselung fragt
`ErmittleZieltabelle` des Duplizierers, dessen Beziehungswissen
(`_echteFks`) nur `ErmittlePlan` lädt — also nur beim Duplizieren und beim
EXPORT. Ein reiner Import arbeitete auf leerem Wissen, und jede Beziehung
außerhalb der handgepflegten `FK_MAP` (wie `ID_Ergebnis → Tab_Ergebnis`)
blieb unversetzt → erzwungene Beziehung bricht. Fix dreiteilig:
(1) `ProjektDuplizierenCtrl.BeziehungenLaden` (Extrakt aus `ErmittlePlan`)
wird im Import geladen; (2) Namenskonventions-Gürtel in `Umschluessele`:
`ID_<X> → Tab_<X>` wird versetzt, wenn `Tab_<X>` im Paket mitreist —
unabhängig von der FK-Rowset-Lotterie; (3) Randfall „Elterntabelle im
Manifest, aber ohne Zeilen": Verweis wird ehrlich gelöst statt fremd zu
zeigen. Beweis: Prüfstand-Modus `transfer` 15/15 PASS, darunter der Import
der ECHTEN Nutzerdatei mit korrekt verdrahteter Ergebnisfamilie (T3a/T3b)
und der App-Fluss mit frischem Controller (T1-Kern).

**B2 — Schemaversion wirkungslos:** Das Manifest führt `schemaVersion`,
aber als Konstante `SCHEMA_VER = 0`. Import zwischen Rechnern mit
unterschiedlichem Migrationsstand würde still angenommen. T2 koppelt das Feld
an `SchemaMigration.ZIEL_VERSION` (derzeit 47) und prüft beim Import: ungleich
→ klare Meldung („Exportstand 47, dieser Rechner 46 — beide Rechner auf
denselben Stand bringen"), kein Import. Begründung: datenbankweite
Datenmigrationen laufen genau einmal; projektweises Nachziehen älterer Pakete
wäre eine eigene Etappe (TF4). **UMGESETZT (28.08.2026):** Manifest trägt
`SchemaMigration.ZIEL_VERSION`; Import lehnt fremde Stände mit klarer Meldung
ab (Altpakete mit Stand 0 bleiben zugelassen); Beweis T4a.

**B3 — Kostenanker reisen unversetzt:** `Tab_ProjektWerte.ID_AnlageGeraet`
zeigt komponentenabhängig auf `Tab_WP`/`Tab_Kessel`/… — eine generische
FK-Erkennung kann das Ziel nicht kennen, der Import lässt die Spalte heute
unverändert (Quell-IDs). Nach dem Import wären die Anker der Kostenpositionen
falsch, bis die Ä21-Selbstheilung sie beim ersten UI-Aufbau ehrlich löst — und
dann stünden Positionen „ohne Anlagenzuordnung" (dasselbe Bild wie der
Ä24-Befund). T2 ruft nach dem Commit
`KostenProjektPositionenCtrl.AnkerNachziehen(neueProjektId)` — derselbe
Baustein, der das im Duplizierer seit Ä24 löst. Gleiches Muster für künftige
komponentenabhängige Verweise: nach dem Import einmal aus den gültigen
Zuordnungen neu ableiten statt raten. **UMGESETZT (28.08.2026):**
`AnkerNachziehen` läuft nach dem Commit; Beweis T2f (vorher 8 fremde Anker,
jetzt 0).

**B4 — Keine Varianten (die Kernanforderung):** Export nimmt genau ein
Projekt. `Tab_Variante(ID, ID_Projekt, ID_ProjektRef, Variantenname)`-Zeilen
gehören dem Variantenprojekt; beim Export einer einzelnen Variante entstünde
am Ziel eine Verknüpfungswaise. T3 baut die Varianten-Option (§ 4).

## 4. Erweiterung: Varianten exportieren (T3)

- **Export-Reiter**: Nach Wahl des Projekts listet der Dialog dessen Varianten
  (`Tab_Variante.ID_ProjektRef = Stamm`) als Häkchenliste (Vorbelegung TF2).
  Exportiert wird immer **vom Stamm aus**; eine einzelne Variante ohne ihren
  Stamm ist kein Exportfall.
- **Paketformat V2** (formatVersion 2): mehrere Projekte je Paket —
  `projects/<n>/data/<Tabelle>.json` je Projektbaum plus die zugehörigen
  `Tab_Variante`-Zeilen; `catalogs/` und `fill/` bleiben paketweit (einmal
  gesammelt). Das Manifest führt je Projekt Name, Rolle (Stamm/Variante) und
  Tabellenliste. **Import von V1-Paketen bleibt möglich** (ein Projekt, wie
  heute).
- **Import**: Stamm zuerst, dann Varianten (jeweils der bestehende
  Ein-Projekt-Ablauf mit eigenen Offsets), danach die
  `Tab_Variante`-Verknüpfungen mit umgeschlüsselten Projekt-IDs;
  Namenskonflikte je Projekt nach dem gewählten Modus (Bestand:
  Abbrechen/Überschreiben/Neuer Name — beim Variantenpaket gilt der Modus für
  alle enthaltenen Projekte). Abschließend Klapplisten aktualisieren
  (`VariantenAnzeigeAktualisieren`, Ä19).

## 5. Erweiterung: Vorschau und Bericht (T4)

- **Vorschau vor dem Import**: Manifest anzeigen, bevor geschrieben wird —
  Quellprojekt(e), Varianten, Exportdatum, Schemastand, Tabellen-/Zeilenzahlen.
- **Abschlussbericht nach dem Import**: importierte Projekte, Zeilen je
  Bereich, aufgelöste/angelegte Katalogeinträge (aus `LoeseKatalogAuf` /
  `FuelleKatalog`), genullte verwaiste Verweise (Selbstheilung) — heute
  passiert das still; der Bericht macht es sichtbar und kopierbar.
- **Sicherung vor dem Import** (TF5): optionaler Haken; besonders der
  Überschreiben-Modus löscht ein bestehendes Projekt und verdient ein Netz.

## 6. Prüfstand (kd1runner, neuer Modus `transfer`) — T1/T5

1. **Reproduktion B1**: Projekt MIT gespeicherten Ergebnissen (Ergebnisfamilie
   belegt) exportieren, in eine frische Kopie importieren — vor dem Fix rot,
   nach dem Fix grün.
2. **Roundtrip fremde DB**: Zeilenzahlen je Pakettabelle Quelle↔Import gleich;
   FK-Integrität (0 Waisen, 0 lose Kostenpositionen nach AnkerNachziehen);
   Stichproben-Feldvergleich.
3. **Rechenbeweis** (stärkstes Kriterium): Wirtschaftlichkeits-/
   Simulationskennzahlen des importierten Projekts == Quelle.
4. **Variantenpaket**: Stamm + 2 Varianten exportieren/importieren;
   `Tab_Variante`-Verknüpfung zeigt auf die importierten Projekte.
5. **V1-Verträglichkeit**: ein V1-Paket (heutiges Format) importiert weiter.

## 7. Etappen

| Etappe | Inhalt | Abnahmekriterium |
|---|---|---|
| T1 ✔ | **B1-Fix UMGESETZT** (Ursache: ungeladenes Beziehungswissen im Importpfad — § 3) | ERFÜLLT: `transfer` 15/15 PASS inkl. Import der echten Nutzerdatei (T3a/T3b) und App-Fluss mit frischem Controller (T1-Kern) |
| T2 ✔ | **Härtung UMGESETZT**: schemaVersion an `ZIEL_VERSION` + Importprüfung (B2); `AnkerNachziehen` nach Import (B3) | ERFÜLLT: T4a Ablehnung mit Schemastand-Meldung; T2e/T2f Anker sauber; kd6 92/92, Sweep 114/0/5 |
| T3 ✔ | **Varianten-Option UMGESETZT**: Häkchenliste im Export (vorbelegt alle an, TF1), Paketformat V2 (`projects/<i>/data/`, `variants`+`variantLinks` im Manifest; `Tab_Variante` reist NICHT als Tabellenzeile — `ID_ProjektRef` wäre nicht versetzbar, die Verknüpfung wird beim Import neu geschrieben), Import orchestriert Stamm + Varianten in EINER Transaktion (`BaumEinfuegen` je Projektbaum), V1-Pakete bleiben lesbar | ERFÜLLT: T5a/T5b (Wärmepumpe WG + 2 Varianten: Export, Import, 2 wiederhergestellte Verknüpfungen), T3a (V1-Nutzerpaket importiert weiter) |
| T4 ✔ | **Vorschau/Bericht/Sicherung UMGESETZT**: Paketvorschau zeigt Varianten; Abschlussbericht (Projekte, Varianten, Verknüpfungen, Hinweise) in der Erfolgsmeldung und als `<paket>.importbericht.txt` (TF5); Sicherungs-Haken vorbelegt an (`<DB>_vor_Import_<Zeitstempel>.accdb` neben der DB) | ERFÜLLT: Dialog erweitert; Sweep 114/0/5 |
| T5 ✔ | Prüfstand-Modus `transfer` dauerhaft (`kd1runner <ordnerA> transfer <ordnerB>`; Soll 17/17: B1-Kern mit frischem Controller, Nutzerpaket-Realfall, Roundtrip-Zählungen, Anker, Versions-Ablehnung, Variantenpaket) | ERFÜLLT: 17/17 PASS, kd6 92/92, Sweep 114/0/5; offen bleibt die Sichtabnahme durch den Nutzer |

## 8. Abgrenzung (bewusst NICHT in diesem Konzept)

- Kein Teil-Export einzelner Anlagen oder Kostensätze (Einheit ist das Projekt).
- Kein Katalog-Abgleich zwischen Rechnern (der Beipack legt nur an, was dem
  Ziel fehlt; Zielkatalog gewinnt bei Namensgleichheit — Bestandsverhalten).
- Kein Zusammenführen zweier Stände desselben Projekts (Import = neues Projekt
  bzw. bewusstes Überschreiben im Bestandsmodus).
- Kein Netzwerk-/Cloud-Transfer (die `.wpx`-Datei ist der Weg).

**Verzahnung mit dem Konzept DB-Migration (Access→SQL):** Das ZIP/JSON-Format
ist datenbankneutral — nach einem SQL-Umstieg wechselt nur die Provider-Seite
des Controllers. Im DB-Migrationskonzept als D-Punkt nachzutragen, sobald
dieses Konzept beschlossen ist.

## 9. Entscheidungspunkte (mit der Umsetzung gemäß Vorschlag ausgeführt)

Der Nutzer beauftragte am 28.08.2026 die Ausführung des Konzepts — die
Vorschläge unten sind damit umgesetzt; Änderungen jederzeit auf Zuruf.

| Nr. | Frage | Vorschlag |
|---|---|---|
| TF1 | Varianten-Häkchen vorbelegt? | alle an (Kernanforderung „mit Varianten") |
| TF2 | Konfliktmodus beim Variantenpaket einheitlich für alle enthaltenen Projekte? | ja (ein Modus je Importlauf, wie Bestand) |
| TF3 | Sicherungskopie der DB vor dem Import? | Haken, vorbelegt an (Überschreiben-Modus!) |
| TF4 | Ältere Pakete in neuere DB (projektbezogenes Nachmigrieren)? | nicht jetzt; strikt gleiche Schemaversion (B2) |
| TF5 | Bericht auch als Datei neben die `.wpx` schreiben (`<name>-importbericht.txt`)? | ja, zusätzlich zur Anzeige |
| TF6 | Klimadaten (Wetterdaten der Klimaregion) ins Paket? | nein; Referenz über Namen, beide Rechner haben dieselbe Auslieferung — Konfliktfall meldet der Bericht |

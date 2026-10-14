# BK-3d — Wiederimport des Bauteilkatalogs konvergiert in einem Lauf

Sitzung Gebäudesimulation, 14.10.2026, Zweig `bk3d` (von `ios_migration_september`). Kein Schemaschritt.

## Befund

Wiederimport des Bauteilkatalogs beim Anwender (produktive Datenbank, Schemastand 227) über einem Importstand vom
12.10.2026. Spalten der Vorschau: Neu / Mit Katalog zusammengeführt / In der Quelle zusammengeführt / Wiedererkannt /
Abgelehnt.

| Lauf | Baustoffe | Übernahme |
|---|---|---|
| 1 | 14 / 16 / 3 268 / 7 344 / 77 | +14 (7 529 → 7 543) |
| 2 | **6** / **10** / 3 268 / 7 358 / 77 | +6 (7 543 → 7 549) |
| 3 | 0 / 10 / 3 268 / 7 364 / 77 | konvergiert |

Aufbauten, Schichten und Abschnitte: in allen Läufen nur „Wiedererkannt“. Nachzählung in der Datenbank des Anwenders:
`Tab_Baustoff_STAMM` 7 549 (IMPORT 7 417, VORGABE 132); kein VORGABE-Satz trägt eine Quellkennung; IMPORT-Sätze je
Quellkennung eindeutig. Nur 1 IMPORT-Satz teilt Bezeichner und Gruppe mit einem VORGABE-Satz (nicht von Schichten
verwiesen); die 20 jüngsten IMPORT-Sätze (14 + 6) haben keinen VORGABE-Partner. 249 IMPORT-Sätze (87 Schlüssel) teilen
Bezeichner und Gruppe bei verschiedener Quellkennung.

Damit traf „Mit Katalog zusammengeführt“ im Wiederimport **vorhandene Importsätze anderer Quellkennung**, nicht die
Auslieferung; die vermuteten Dubletten gegen VORGABE bestätigen sich nicht.

## Ursache

`BauteilkatalogImport.Planen` prüfte die Dublettenregel (Name nach `Baustoffabgleich.Schluessel`, Gruppe, λ ± 2 %,
ρ ± 5 %, c ± 10 %) gegen den Katalog **vor** dem Lauf. Ein Importsatz, den ein Satz der Quelle im selben Lauf per
Quellkennung aktualisiert (`AKTUALISIEREN`: alle Felder neu), trug dabei noch seine alten Werte. Ein zweiter,
gleichnamiger Satz der Quelle mit diesen alten Werten wurde an ihn gebunden („Mit Katalog zusammengeführt“, Ergänzen
mit `COALESCE`, Quellkennung des Ziels bleibt). Nach der Übernahme trägt der Zielsatz die neuen Werte — im nächsten Lauf
trifft der zweite Satz nichts mehr und ist „Neu“. Erst der dritte Lauf erkennt ihn über seine Quellkennung. Das sind
die sechs Sätze; die übrigen zehn binden an Importsätze, deren Werte sich nicht ändern, und bleiben in jedem Lauf
„Mit Katalog zusammengeführt“ (ihre Quellkennung kann nicht persistiert werden, das Ziel trägt schon eine).

Die Zusammenführung mit der Auslieferung wird persistiert: Ergänzen schreibt die Quellkennung an einen VORGABE-Satz,
wenn sie dort leer ist; ein zweiter Lauf zählt den Satz „Wiedererkannt“. Dass beim Anwender kein VORGABE-Satz eine
Quellkennung trägt, heißt: kein Quellsatz traf die Auslieferung nach der Dublettenregel.

Reproduktion (synthetisch, Testdatenbank) vor der Behebung: Vorschau 1 0 / 4 / 1 / 1 / 0, Übernahme +0; Vorschau 2
**1** / 0 / 1 / 4 / 0, Übernahme +1; Vorschau 3 0 / 0 / 1 / 5 / 0.

## Quell-Zusammenführung

„In der Quelle zusammengeführt“ fasst Sätze mit gleichem Schlüssel (Name nach `Baustoffabgleich.Schluessel`, Gruppe)
**und** Werten nach der Dublettenregel zusammen; die Werte vergleicht sie mit dem ersten Vertreter der Namensgruppe.
Gleichnamige Sätze mit Werten außerhalb der Toleranz bleiben getrennt. Das ist gewollt (BK-3, Dublettenregel: gleicher
Name, andere Kennwerte = verschiedene Stoffe, etwa Rohdichteklassen); die 249 namensgleichen Importsätze sind kein
Nachzug, die Vorschau zählt sie jetzt sichtbar.

## Lösung

- `Planen`: zuerst alle Sätze per Quellkennung zuordnen; die Dublettenregel prüft danach gegen den Stand **nach** dem
  Lauf — für einen Importsatz, den ein Satz der Quelle aktualisiert, mit dessen Bezeichner, Gruppe und Werten. Ein Satz,
  der nur die alten Werte trifft, ist damit schon im ersten Lauf „Neu“; der zweite Lauf legt nichts an.
- Vorschau zählt, ohne etwas zu ändern: `DublettenImKatalog` (Importsätze mit Bezeichner und Gruppe eines
  Auslieferungssatzes) und `NamensgleicheImporte` (Importsätze, deren Bezeichner und Gruppe ein Importsatz anderer
  Quellkennung teilt); Bezeichner ohne Randleerzeichen und Groß/Klein. Beide erscheinen bei > 0 als Hinweiszeile im
  Protokoll der Vorschau (die Maske zeigt das Protokoll bereits; Hülle und Maske unverändert, keine neuen Ressourcen,
  keine neue SQL).

## Tests

`EPOS.Kern.Tests/BauteilkatalogWiederimportTests` (neu, drei Fälle):

- `Wiederimport_konvergiert_nach_einer_Uebernahme` — früherer Importstand, dann Quelle mit Treffer auf die Auslieferung,
  zwei in der Quelle zusammengeführten Zeilen auf die Auslieferung, λ-Skalenfehler auf die Auslieferung, einem
  wiedererkannten Satz mit geändertem λ samt gleichnamigem Satz mit dessen alten Werten und einem Treffer auf einen
  Importsatz anderer Quellkennung. Vorschau 1 1 / 4 / 1 / 1 / 0; Vorschau 2 0 / 1 / 1 / 5 / 0, Übernahme 2 +0; Quellkennung
  an den drei getroffenen Auslieferungssätzen; keine Dublette nach Name, Gruppe und Werten über VORGABE und IMPORT.
- `Ein_geaenderter_Lambda_in_der_Toleranz_fuehrt_mit_dem_Katalog_zusammen_nicht_neu` — λ + 1,5 % ergibt „Mit Katalog
  zusammengeführt“, der Auslieferungswert bleibt; danach λ + 50 % ergibt „Wiedererkannt“, nichts neu.
- `Die_Vorschau_zaehlt_Importsaetze_mit_Name_und_Gruppe_eines_Auslieferungssatzes` — Gegenprobe 0 / 0 ohne Hinweis
  bei sauberem Katalog, 1 / 0 mit Hinweiszeile nach einem namensgleichen Importsatz.

Filter `Bauteilkatalog|Baustoff`: 131 von 131 grün. SQL-Prüfer: 0 Fundstellen. Build `WP-Plan.Kern.slnf` Release:
0 Fehler.

## Empfehlung zur Bereinigung beim Anwender

Die sechs Sätze der zweiten Übernahme sind keine Dubletten nach der Dublettenregel: Sie sind eigene Quellsätze (eigene
Quellkennung), deren Werte von den aktualisierten Namensvettern abweichen. Eine Bereinigung ist nicht nötig. Für den
einen Importsatz mit Name und Gruppe eines Auslieferungssatzes (ohne Verweis aus Schichten) sähe sie so aus: Verweise
`Tab_Bauteilschicht_STAMM.ID_Baustoff` vom Importsatz auf den Auslieferungssatz umhängen, die Quellkennung an den
Auslieferungssatz setzen (wenn leer), dann den Importsatz löschen — als eigener Anwenderentscheid, nicht automatisch.

## Offen

- Die zehn Sätze, die an Importsätze anderer Quellkennung binden, bleiben in jedem Lauf „Mit Katalog zusammengeführt“:
  persistieren ließe sich das nur mit einer Zuordnungstabelle Quellkennung → Baustoff (Schemaschritt) — nicht in dieser
  Welle.
- Die Quell-Zusammenführung vergleicht nur mit dem ersten Vertreter einer Namensgruppe (Toleranz nicht transitiv);
  unverändert.

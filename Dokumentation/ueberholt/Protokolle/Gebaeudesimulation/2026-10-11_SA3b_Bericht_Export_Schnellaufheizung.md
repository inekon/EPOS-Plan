# Protokoll SA3b — Schnellaufheizung in Bericht, Kennzahlen, Export und Variantenvergleich

**11.10.2026 · Sitzung Gebäudesimulation · Welle SA3b der Schnellaufheizung (E128).** Grundlage:
[SA2](2026-10-11_SA2_Schema217.md) (Schritt 217, sechs Ergebnisgrößen je Gebäude und Zone in `Vorheizkennzahlen`, NULL ohne
Wirkung), Muster [AH-V6](2026-10-11_AH_V6_Bericht_Export.md). Keine Oberfläche (SA3a), kein Referenzprojekt.

## Bericht

- **Gebäudetafel** (`Aufheizbericht.Vorheizzeilen` → `Schnellaufzeilen`): nach der Mehrwärme die Zeilen der Schnellaufheizung —
  Überhöhung Δθ (K), Stunden mit Überhöhung (h/a), höchster Vorlauf mit Überhöhung (°C), Stunden an der Vorlaufgrenze,
  Stunden mit wirkungsloser Überhöhung und Stunden mit Überhöhung in einer Sperrzeit (h/a). Nur, wenn die Überhöhung an einer
  Zone gewirkt hat (`UeberhoehungK` gesetzt), jede nur mit Wert; sonst keine Zeile — Vorheizen ohne Schalter und Sollwertrampe
  bleiben, wie sie waren.
- **Tafel „Vorheizen je Zone“** (`Aufheizbericht.Zonentafel`): zwei Spalten mehr (Δθ [K], Stunden überhöht [h/a]) nur, wenn eine
  Zone mit Wirkung gerechnet hat; dann acht Spalten mit neuen festen Breiten, sonst die sechs Spalten des Bestands.
- **Einstellungsteil:** Der Bericht beschreibt die Vorheizvorgabe nicht in einem eigenen Einstellungsabschnitt; Schalter und
  Überhöhung erscheinen als Abweichungsmerkmale im Variantenvergleich (unten) und, mit Wirkung, als Zeilen der Gebäudetafel.
- **Vorlagenfelder, Katalog v21** (`FASSUNG_SCHNELLAUFHEIZUNG`; v19 Vorheizen, v20 Rückkühlwerk waren vergeben):
  `gebaeude.ergebnis.vorheiz_ueberhoehung` (N1, K), `…vorheiz_ueberhoehung_stunden`, `…vorheiz_vorlaufgrenze_stunden`,
  `…vorheiz_ueberhoehung_wirkungslos`, `…vorheiz_ueberhoehung_sperrzeit` (N0, h/a), `…vorheiz_ueberhoehung_vorlauf_max`
  (N1, °C), der Text `…vorheiz_schnellaufheizung` (Halbsatz „, mit Schnellaufheizung (+2,0 K)“, Leerwert leer), der Schalter
  `hat.schnellaufheizung` und die Kennzahlfelder der neuen Kennzahl. Messlatte `Vorlagenfeldkatalog_v21.txt`: 23 Schlüssel mehr
  als v20, keiner entfallen.
- **Kurzbericht:** Absatz 7b endet mit `{{gebaeude.ergebnis.vorheiz_schnellaufheizung}}` vor dem Punkt — ohne Wirkung leer, der
  Satz bleibt, wie er war; Kommentar 9 nennt das Feld in beiden Sprachen. `Werkzeuge/Berichtsvorlage … alle` hat die zehn
  Vorlagen auf Fassung 21 geschrieben, Validator 0 Fehler, ein weiterer Lauf ändert kein Byte.

## Kennzahlen, Export, Variantenvergleich

- **Kennzahl** `geb.vorheiz_ueberhoehung_stunden` (Gruppe „Gebäude“, h/a, N0, mit Δ, ab Fassung 21): Höchstwert der Gebäude wie
  die Aufheizstunden; ohne Wirkung ohne Wert, die Zeile entfällt.
- **Export** (`GebaeudeErgebnisexport.Schnellaufschluessel`, Muster E32): `Geb[n].VorheizUeberhoehungK`, `…VorheizUeberhoehungH`,
  `…VorheizUeberhoehungVorlaufMaxC`, `…VorheizVorlaufgrenzeH`, `…VorheizUeberhoehungWirkungslosH`,
  `…VorheizUeberhoehungSperrzeitH`, je Zone ebenso unter `Geb[n].Zone[k].` — nur mit Wirkung, sonst kein Schlüssel. Die
  siebzehn Schlüssel der Fassung 2 (`Vorheizschluessel`) bleiben unverändert.
- **Abweichungsmerkmale** (Gewerk „Gebäude“): Schnellaufheizung (Ja/Nein, NULL = aus) und Überhöhung Δθ (K, NULL = Kern-Vorgabe
  `Vorheizvorgabe.UEBERHOEHUNG_VORGABE_K` über die Zahlvorgabe); NULL und die Vorgabe sind keine Abweichung.

## Nachweis

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler.
- Kerntests, Filter Bericht, Vorlage, Platzhalter, Kennzahl, Export, Variante, Abweichung, Vergleich, Kurzbericht, Vorheiz,
  Schnellauf, Aufheiz und alle Wachen: **2 417 grün, 2 rot, 1 übersprungen** — rot war der Rumpf des Kurzberichts in
  `BerichtsvorlageDateiWacheTests` (beide Sprachen, nachgezogen). Nachlauf (BerichtsvorlageDateiWache, SchnellaufheizungBericht,
  VorheizBericht, Ressourcen, DokumentationLink, RepositoryOrdnung, Kodierung): **183 grün, 0 rot**. Darin
  `SchnellaufheizungBerichtTests` 10.
- UI-Tests mit demselben Filter: **1 014 grün**.
- `Werkzeuge/Auslieferungsvorlage`: **62 grün**. `Werkzeuge/Berichtsvorlage … alle`: zehn Vorlagen, Validator 0 Fehler,
  wiederholbar (ein weiterer Lauf ändert kein Byte).
- Nachgezogene Pins: Katalogfassung 21 (`VorlagenfeldkatalogWacheTests`), Fassung der Gebäudekennzahlen
  (`AufheizVergleichTests`: Stunden mit Überhöhung 21), Zählung der Vorheizfelder der Fassung 19 (`VorheizBerichtTests`), Rumpf
  des Kurzberichts (`BerichtsvorlageDateiWacheTests`).
- Keine neue SQL-Anweisung (die Merkmale lesen die geladene Zeile von `Tab_Einstellungen`), der SQL-Dialektprüfer ist nicht
  betroffen.
- ChartProben: 254 Bilder, 0 Verstöße (kein Bild geändert).
- Referenzlauf aller 30 Projekte gegen `2026-10-11_R52_Vorheizen`: **GESAMT PASS** (10 422 417 Werte), alle 979 CSV
  byte-gleich — kein Referenzprojekt rechnet mit Schnellaufheizung, kein neuer Schlüssel.
- Ressourcen: 19 Schlüssel je Sprache (`BV_VORH_SA_*`, `BV_VORH_KOPF_UEBERHOEHUNG*`, `VF_GEBAEUDE__ERGEBNIS__VORHEIZ_*`,
  `VF_HAT__SCHNELLAUFHEIZUNG`, `ABW_MERKMAL_VORH_SCHNELLAUF`, `ABW_MERKMAL_VORH_UEBERHOEHUNG`), `Resource.Designer.cs` neu
  geschrieben.

## Offen

- Ein Referenzprojekt mit Schnellaufheizung fehlt; erst mit ihm erscheinen die Exportschlüssel in einer Referenz-CSV.
- Die Oberfläche (Schalter, Überhöhung, Ergebnisseite) kommt mit SA3a.

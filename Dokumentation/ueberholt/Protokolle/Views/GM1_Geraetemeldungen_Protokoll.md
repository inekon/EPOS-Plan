# GM‑1 — Drei Gerätemeldungen (Protokoll, 20.09.2026)

Statuszeile #415 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md).
Zweig `geraet-3`, Commits `b0f941d9`, `ef9624d5`, `7bba4491`; Merge `bcf55154`.

## Anlass

Drei Meldungen des Anwenders aus der Abnahme am Gerät (Windows, Bildschirmfotos vom 20.09.2026):
Eine aus TRY-Daten importierte Klimaregion ließ sich auf der Startseite nicht auswählen; das
Auswahlfeld der Klimaregion lag über dem aufgeklappten Hilfe-Menü; auf den Reitern Solarthermie
und Photovoltaik fehlte der Schalter „sortiert".

## 1. Klimaregionenliste nach dem Klimadaten-Dialog

**Ursache.** `Startseite.razor` liest `_klimaregionen` nur in `Laden()` — beim Aufbau und auf das
Ereignis `SeitenZustand.Geaendert` (Projektwechsel, `Auffrischen()`). Der Klimadaten-Dialog ist
unter Windows ein eigenes Fenster (`KlimadatenHuelle`, `BlazorDialogForm<KlimadatenDialog>`);
sein Rückruf `Geschlossen` schloss nur das Fenster. Der Import selbst war in Ordnung: Die
Auswahlliste liest alle Stammregionen ohne Filter, und das Speichern kopiert Quelle, Szenario und
Bezugsjahr mit ins Projekt.

**Änderung.** `StartseiteHuelle.KlimaregionenAktualisieren()` (ruft `_zustand.Auffrischen()`,
Muster `VariantenAnzeigeAktualisieren`); `KlimadatenHuelle` ruft es nach der Rückkehr aus
`ShowDialog`, immer — ob importiert, gelöscht oder nichts geändert wurde, ist dort nicht bekannt,
und das Neulesen von 30–40 Regionen kostet nichts. Hüllenmethode statt neuem `Ansichten`-Schlüssel:
`INavigation.AnsichtAktualisieren` ist der Weg für Aufrufer, die die Schale nicht kennen dürfen; das
Klimadatenfenster gehört selbst zur Windows-Schale. Test: `StartseiteTests` — der Delegat
`Klimaregionen` liefert nach `Auffrischen()` die neue Region in der `Suchauswahl`.

**iOS-Befund.** `AppWurzel.Zeige` führt keinen Zweig für `Seitenschluessel.Klimadaten`; die
iOS-Navigation gibt `false` zurück, der Dialog geht dort nicht auf. Offen (Statusdatei, Nach #415).

## 2. Auswahlfeld über dem Menüband

**Ursache.** `.epos-suchauswahl { position: relative; z-index: 41; }` galt dauerhaft, auch bei
geschlossener Liste; `.epos-menueband` steht ebenfalls auf 41, seine Klappe auf 40. Bei gleicher
Ebene gewinnt das später im Seitenaufbau stehende Element — das Klimafeld der Startseite.

**Änderung.** Die Wurzel der `Suchauswahl` trägt `epos-suchauswahl--offen`, solange die Liste
offen ist; die Ebene 41 gilt nur unter dieser Klasse (der Deckel liegt auf 39, die Liste auf 40).
`position: relative` bleibt am Grundzustand als Bezugspunkt der absolut stehenden Liste. Tests in
`SuchauswahlTests`: geschlossen keine Klasse, nach Klick ins Feld die Klasse, nach Klick auf die
Schließfläche wieder keine.

## 3. Schalter „sortiert" auf Solarthermie und Photovoltaik

**Ursache.** Beide Reiter übergaben `Bildauftrag(…, false, …)`; `ModellSolar` und `ModellPv`
der Hülle riefen `ErzeugerStapelModell` fest unsortiert — das WinForms-Vorbild hatte dort keinen
Umschalter.

**Änderung.** Schalter `SIM_CHK_SORTIERT` in beiden Reitern an der Stelle des Heizkesselreiters,
`_sortiert` im Bildauftrag, Prüfhilfe `Sortiert`. `ModellSolar` reicht das Kennzeichen durch (die
Achse bleibt Jahresstunden), `ModellPv` wählt `sortiert ? Jahresstunden : Monate`. Die zweite Achse
(Speicherfüllstand) sortiert `ErzeugerStapelModell` selbst; `ModellSpeicherBetrieb` reicht das
Kennzeichen deshalb ebenso durch. Tests in `ErzeugerReiterTests`: Nach dem Umschalten trägt der
Bildauftrag `Sortiert == true`.

## Abnahme

Worktree: Kern-Filter 0 Fehler, Windows-Schale 0 Fehler, 10 031 Tests grün. Gate im Hauptbaum auf
`bcf55154`: Kern-Filter 0 Fehler, ChartProben 106 Bilder / 0 Verstöße, Windows-Messlatte 91/91
gleich mit #414, 10 031 Tests grün (1 übersprungen).

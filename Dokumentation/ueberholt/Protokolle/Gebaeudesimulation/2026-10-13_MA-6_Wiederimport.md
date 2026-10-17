# MA-6 — Wiederimport bewahrt

Kurzprotokoll, 13.10.2026. Grundlage: [Entwurf Geführter Modellaufbau](../../../aktuell/Gebaeudesimulation/2026-10-12_Entwurf_Gefuehrter_Modellaufbau.md)
Abschnitte 2.2, 2.6, Entscheid E137 F7 und Wellenplan Zeile MA-6. Vorläufer: [MA-1a](2026-10-13_MA-1a_Modellpruefung_Kern.md)
(Vorschläge mit Rücknahme), [MA-2](2026-10-13_MA-2_Nutzung_und_Kalender.md) (`ZonenNutzungCtrl`),
[MA-3](2026-10-13_MA-3_Huelle_geschlossen.md) und [MA-4](2026-10-13_MA-4_Aufbauvorschlaege.md) (ergänzte Bauteile, Aufbauvorschläge),
[DA-1](2026-10-13_DA-1_gbXML_Import_Lasten_Zeitplaene.md) (gbXML-Konditionierung). Mehrzonenkonzept:
[Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md](../../../aktuell/Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md).
Ziel: Ergänzungen des Anwenders überleben die CAD-Änderung.

## Befund

| Stand vor MA-6 | Folge für den Bau |
|---|---|
| `MehrzonenuebernahmeCtrl.Ersetzen` löschte alle Zonen des Gebäudes samt Bauteilen, Kalendern (Kaskade) und Quellen und schrieb den Vorschlag neu. Profile, Kalender, Aufbauten und Hand-Zonen gingen verloren, alle Ids wechselten. | Neuer Weg `Zusammenfuehren` (Teildatei `MehrzonenuebernahmeCtrl.Zusammenfuehrung.cs`); die Hülle ruft ihn. `Ersetzen` ist entfernt (kein Aufrufer mehr), sein Test prüft nun `Zusammenfuehren`. |
| Die Hülle `GebaeudeMehrzonenHuelle` ließ nur dieselbe Datei zu (gleiche Prüfsumme). | Eine geänderte Fassung (gleiches Format, andere Prüfsumme) läuft in dieselbe Zusammenführung mit Vorschau (Entwurf 2.6). Fehlende Datei, anderes Format und fehlende Quelle bleiben benannt gesperrt. |
| Wiederfinden: `Tab_Importzuordnung` paart Raum → Zone (mehrere Räume je Zone) und Fläche → Bauteil; `Tab_Zone.Quellkennung`/`Tab_Bauteil.Quellkennung` tragen die Kennung zusätzlich. | Genügt. Je neue Zone die Bestandszone mit den meisten gemeinsamen Kennungen, je Bauteil die gleiche Kennung (zuerst in der gepaarten Zone). |
| Herkunft: `Herkunft` an Zone und Bauteil (`MANUELL`, `KATALOG`, `IFC`, `GBXML`, `VORGABE`, `SQPROJ`), `Flaechenherkunft` am Bauteil (fällt bei Handänderung der Fläche auf `null`), Kalender mit `ID_Zone`. Das Blatt „Nutzung und Kalender“ (MA-2) und die Aufbauvorschläge (MA-4) setzten keine Herkunft. | MA-2 setzt nun an jeder Zone, die eine Handlung des Blatts trifft, `Herkunft = MANUELL` (Rücknahme stellt sie her). Ein von Hand gewählter Aufbau wird ohne Schema erkannt: Er ist keiner der Aufbauten, die die Importzuordnung der alten Quelle führt. |

## Regel der Zusammenführung je Feld (Hand > Datei > Vorgabe)

Wiedergefundene Sätze behalten ihre Id (Kalender, Ergebnisse, Zapfzonen, Verweise bleiben gültig).

| Satz | aus der Datei | bleibt |
|---|---|---|
| Zone | Name, Nutzfläche, Raumhöhe, Volumen, Quellkennung; ohne `MANUELL` jedes weitere Feld, für das die Datei einen Wert trägt, und Kalender, Wochen und Vorgabezeilen der Zone | `MANUELL`: alle übrigen Felder samt Nutzungsprofil, Sollwerten und Kalendern. Sonst jedes Feld, für das die Datei nichts trägt — Übergabe (sieben Spalten), Kühlübergabe (drei), Vorheizzeit, Kühlung, Nutzungsprofil |
| Bauteil | Name, Art, Neigung, Azimut, Fläche samt Flächenherkunft, Quellkennung; Aufbau und U-Wert, wenn der Aufbau aus der Datei stammt; ohne `MANUELL` g-Wert, Rahmen, Verschattung, ψ·L, Randbedingung, Nachbarzone | Von Hand geänderte Fläche (`Flaechenherkunft` `null` bei Datei-Kennung); von Hand gewählter Aufbau samt U-Wert; `MANUELL`: zudem g-Wert, Rahmen, Verschattung, ψ·L, Randbedingung, Nachbarzone, Herkunft |
| Zone/Bauteil ohne Quellkennung | — | unberührt: Hand-Zonen, ergänzte Bauteile (MA-3, Herkunft `VORGABE`) |
| Zone/Bauteil aus der Datei, in der neuen Fassung fehlend | — | bleibt ohne Zustimmung (gilt danach als Ergänzung von Hand); mit Zustimmung entfernt, Nachbarzonen-Verweise auf eine entfernte Zone werden `NULL` |

Ablauf in einem Vorgang (`DbVorgang` + `Vorgangsklammer`): Bestand und Paarung lesen → alte Quelle löschen (Paarungen,
Grundrisse) → Baustoffe → `VorschlagSchreiben(…, bestandZulassen: true)` neben den Bestand → je Paar Felder nach der Regel
auf den Bestandssatz, das neue Duplikat fällt; neue Bauteile ziehen in die bleibende Zone → Entfallendes nach Wahl →
Rangfolge (Datei-Reihenfolge, danach die übrigen) → Gebäudekonditionierung → Herkunft mit den Paarungen auf die bleibenden
Ids → Aufbauten der alten Datei, die kein Bauteil mehr trägt, fallen. Jeder Fehler rollt den ganzen Vorgang zurück.

## Vorschau

`MehrzonenuebernahmeCtrl.Vorschau` liest ohne zu schreiben und liefert je Zone und Bauteil den Stand neu / geändert (Felder) /
unverändert / entfällt / bleibt (Hand) samt der Felder, die von Hand bleiben. Die Hülle gibt sie als Gabe `Wiederimport`
(`GebaeudeWiederimportDaten`) an den Importdialog; ohne Bestand (Gebäude ohne Zonen) gibt es keine Vorschau. Das erste OK
zeigt den Baustein `WiederimportVorschau` (Zahlen je Gruppe, Tabelle, Haken „Entfallende Zonen und Bauteile entfernen“,
Vorgabe: behalten), das zweite merkt vor; eine neue Zuordnung verwirft die Vorschau.

## Dateien

- Kern: `EPOS.Kern/Controller/MehrzonenuebernahmeCtrl.Zusammenfuehrung.cs` (neu; `Vorschau`, `Zusammenfuehren`, Regel je Feld),
  `MehrzonenuebernahmeCtrl.cs` (partial, `Ersetzen` entfernt), `GebaeudeZonenCtrl.cs` (`bestandZulassen`, `ZoneAktualisieren`, `BauteilAktualisieren`),
  `ZonenNutzungCtrl.cs` (Herkunft `MANUELL`, Rücknahme), Ressourcen `GEBWI_*` in beiden Sprachen, `Resource.Designer.cs`.
- Hülle: `EPOS.UI.Daten/Bedarf/GebaeudeMehrzonenHuelle.cs` (geänderte Fassung, Gabe `Wiederimport`, Wahl, `Zusammenfuehren`).
- Oberfläche: `EPOS.UI/Dialoge/Import/WiederimportVorschau.razor`, `GebaeudeWiederimportDaten.cs` (neu),
  `GebaeudeImportDialog.razor` (Parameter `Wiederimport`, zweistufiges OK), `epos-ui.css` (Regeln `epos-wiederimport*`).
- Tests: `EPOS.Kern.Tests/WiederimportTests.cs` (neu), `MehrzonenuebernahmeTests.cs` (Ids bleiben, geänderte Fassung öffnet),
  `EPOS.UI.Tests/Dialoge/WiederimportVorschauTests.cs` (neu), `GebaeudeDialogImportTests.cs` (zweistufiges OK),
  `KiMaskenabdeckungWacheTests.cs` (Wirt und Eingabebilanz der Vorschau).

## Tests

- `EPOS.Kern.Tests/WiederimportTests` (1 Test, Projekt 1045 der Testdatenbank, synthetische Projektdatei in zwei Fassungen):
  Hand-Profil (Profil, Sollwerte, Kalender, Herkunft `MANUELL`) bleibt — Gegenprobe: Nutzfläche +10 m² aus der Datei;
  Hand-Aufbau (Katalogaufbau als Projektkopie, U 0,25) bleibt — Gegenprobe: Fläche dieser Wand und der Wand W2 aus der Datei;
  Hand-Zone samt Bauteil und ein ergänztes Bauteil (MA-3) bleiben; Ids der wiedergefundenen Zonen bleiben; Vorschau zählt
  1 Hand-Zone, 2 Hand-Bauteile, 1 entfallendes Bauteil; die entfallende Fläche bleibt ohne Zustimmung und fällt mit ihr;
  Rückweg: eine scheiternde Herkunft lässt den Stand Satz für Satz unverändert.
- Abnahmefilter Kern (`Mehrzonen|Wiederimport|GebaeudeImport|Gbxml|Sqproj|Importzuordnung|Modellpruefung|ZonenNutzung|
  Modultrennung|Dokumentation|Repository|Resource`): 698 bestanden, 0 rot.
- `EPOS.UI.Tests` (`WiederimportVorschau|GebaeudeDialogImport|VorlagenfeldStilblatt|KiMaskenabdeckung|KiKnopfEinmal|Infoknopf|
  ZonenNutzung`): 102 bestanden, 0 rot.
- Bau `WP-Plan.Kern.slnf` Release 0 Fehler; Windows-Schale (Linux, `EnableWindowsTargeting`) 0 Fehler.
- SQL-Dialekt-Prüfer: 2934 SQL-Texte, 0 Fundstellen.
- Ressourcen: `designer_neu.py schreiben`, zweiter Lauf +0.

## Offene Punkte

- **Schemabedarf: nein (nicht zwingend).** Alles Geforderte trägt der Bestand. Feiner würde es mit zwei Spalten (Schritt 229,
  falls gewünscht): `Tab_Konditionierungskalender.Herkunft TEXT CHECK ("Herkunft" IS NULL OR "Herkunft" IN
  ('MANUELL','KATALOG','IFC','GBXML','VORGABE','SQPROJ'))` und `Tab_Bauteil.U_Herkunft TEXT CHECK ("U_Herkunft" IS NULL OR
  "U_Herkunft" IN ('MANUELL','KATALOG','IFC','GBXML','VORGABE','SQPROJ'))`. Ohne sie gilt: (a) ein Kalender, der allein im
  Kalendereditor der Zone (nicht über das Blatt „Nutzung und Kalender“) geändert wurde, macht die Zone nicht zu `MANUELL` und
  wird von den Kalendern der Datei ersetzt — ohne Schema schließbar, indem auch der Kalendereditor die Zone markiert;
  (b) ein von Hand eingetragener U-Wert ohne Aufbauwechsel ist nicht erkennbar und wird vom Dateiwert überschrieben;
  (c) Zonenfelder, die der Zonendialog von Hand ändert und für die die Datei einen Wert trägt, nimmt die Datei, solange die
  Zone nicht `MANUELL` ist.
- Die Aufbauten der neuen Fassung bekommen bei Namensgleichheit mit noch getragenen Aufbauten der alten den Zusatz „(2)“
  (`BauteilaufbauCtrl.FreierName`); die nicht mehr getragenen fallen.
- Gebäudekalender (ohne Zone) im Einzonenweg mit Projektdatei ersetzt die Datei wie bisher.

## Logbuch-Vorschlag

„Ein erneuter Gebäudeimport führt Datei und Modell zusammen: Geometrie und Werte der Datei werden aktualisiert, von Hand
ergänzte Profile, Kalender, Aufbauten, Zonen und Bauteile bleiben erhalten, und eine Vorschau zeigt die Unterschiede vor dem
Übernehmen.“

## Nachzug N2 (14.10.2026)

**Befund.** Offene Punkte (a) und (c) waren ohne Schema schließbar: Kalendereditor und Zonenfelder schreiben beide über den
OK-Weg des Zonendialogs (`GebaeudeZonenCtrl.Schreiben`, Hülle `GebaeudeKatalogHuelle`).

- **(a)** Schreibt der OK-Weg die Konditionierung einer bestehenden Projektzone und hat sich dabei etwas gegen die Datenbank
  geändert (`StandSchreiben` meldet `geschrieben`: Vorgabezeilen, Kalender, Perioden, Wochen), setzt er an der Zone
  `Herkunft = MANUELL` (derselbe Satz wie `ZonenNutzungCtrl.HandMarkieren`). Keine Rücknahme: der Editor schreibt erst mit OK.
- **(c)** Vor dem Schreiben vergleicht `GebaeudeZonenCtrl.HandfelderGeaendert` jede bestehende Projektzone mit dem gespeicherten
  Stand: Übergabe (sieben Spalten), Kühlübergabe (drei), Vorheizzeit, Heiz- und Kühlleistung höchstens, die vier Heizsollwerte,
  beide Kühlsollwerte und die Höchsttemperatur (Rand 1e-9 relativ). Weicht eines ab, wird die Zone `MANUELL`. Nutzfläche,
  Raumhöhe und Volumen zählen nicht, sie kommen immer aus der Datei. Die Katalogebene wird nicht markiert.
- Offen bleibt (b) (von Hand eingetragener U-Wert ohne Aufbauwechsel). Ein Kalender, der außerhalb des Zonendialogs geschrieben
  wird (Knöpfe `KonditionierungCtrl.Schrittweg`, Vorlagenkopie), markiert die Zone weiterhin nicht.

**Dateien.** `EPOS.Kern/Controller/GebaeudeZonenCtrl.cs` (`Schreiben`, `HandfelderGeaendert`),
`EPOS.Kern.Tests/WiederimportTests.cs`.

**Tests.** `WiederimportTests.Kalendereditor_und_Zonendialog_markieren_die_Zone_und_der_Wiederimport_bewahrt_sie` (Projekt 1045
der Testdatenbank, synthetische Projektdatei): eine Woche im Kalendereditor der Halle und Vorheizzeit und Sollwert am Lager
markieren beide Zonen; der Wiederimport bewahrt Woche, Vorheizzeit und Sollwert. Gegenproben: das Büro mit unverändertem Stand
und geänderter Nutzfläche bleibt ohne Marke; dieselbe Woche am Büro, ohne den Dialog geschrieben, ersetzt die Datei. Abnahme
siehe Protokoll DA-4, Nachzug N2.

**Logbuch-Vorschlag.** „Änderungen an Kalendern und Werten einer Zone im Zonendialog bleiben bei einem erneuten Gebäudeimport
erhalten.“

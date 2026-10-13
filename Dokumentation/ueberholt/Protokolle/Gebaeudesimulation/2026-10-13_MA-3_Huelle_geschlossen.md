# Protokoll MA-3 — Hülle geschlossen

Welle MA-3 aus dem [Entwurf Geführter Modellaufbau](../../../aktuell/Gebaeudesimulation/2026-10-12_Entwurf_Gefuehrter_Modellaufbau.md)
(Abschnitt 2.2 Prüfgruppe 1, Abschnitt 2.3, Wellenplan Abschnitt 4 Zeile MA-3) auf der Modellprüfung aus
[MA-1a](2026-10-13_MA-1a_Modellpruefung_Kern.md). Grundlage ist Entscheid E137 (F1 Ampel je Gruppe, F2 Übernahme mit
Rücknahme) und die Prüfung „geschlossene Hülle je Zone“ des
[Mehrzonenkonzepts](../../../aktuell/Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) Abschnitt 5.3. Leitziel: Eine Zone mit offener
Hülle wird mit einem Klick von rot zu gelb statt durch Handarbeit am Bauteil.

## Befund

- Die Flächenbilanz Σ A·n je Zone stand nur als Hinweis im Reiter „Zonen“ (`Zonenkopplungsregeln.Hinweise`, ab zwei Zonen,
  Schwelle 10 %). Die Modellprüfung kannte die Hülle nur je Gebäude (`HUELLE_OHNE_AUSSENWAND`, `…_DACH`, `…_BODEN`).
- Σ oben ≈ Σ unten ist gegen eine Spiegelung blind, und eine Zone nur aus Wänden (oben 0, unten 0) galt als geschlossen.
- Die Referenzprojekte 1052 und 1054 weichen in den zwei oberen Zonen um 26 % ab: Ihre Dachfläche ist die geneigte Fläche
  (428 bzw. 286 m²) mit Neigung 0°, die Kellerdecke die Grundfläche (319 bzw. 213 m²). Der Keller schließt (0 %), 1062 auch.

## Regel

**Prüfung je Zone** (`Modellpruefung.HuelleJeZone`, Gruppe 1 „Geometrie und Bauteile“). Sie ruft
`Zonenkopplungsregeln.Bilanz` — dieselbe Rechnung wie der Hinweis im Reiter „Zonen“, der bleibt; die gemeinsame Schwelle steht
in `Zonenkopplungsregeln.SenkrechtOffen`.

| Code | Ampel | Bedingung | Vorschlag |
|---|---|---|---|
| `HUELLE_OBEN_FEHLT` | rot | keine Fläche nach oben (Verankerung oben fehlt) | Hüllbauteil oben |
| `HUELLE_UNTEN_FEHLT` | rot | keine Fläche nach unten (Verankerung unten fehlt) | Hüllbauteil unten |
| `HUELLE_OFFEN` | gelb | beide Richtungen tragen Fläche, Abweichung ≥ 10 %, ab zwei Zonen (wie der Hinweis) | Hüllbauteil auf der kleineren Seite |
| `HUELLE_GESPIEGELT` | gelb | Bodenplatte nach oben (Neigung < 60°) oder Dach nach unten (> 120°) | — |

Trägt eine Zone in keiner Richtung Fläche, aber Wände, erscheinen beide Codes; die Körperfläche ist dann ihre Nutzfläche. Eine
Zone ohne Außenfläche (nur Innenbauteile) meldet weiter `ZONE_OHNE_AUSSENBAUTEIL`. Texte mit Zahlen: oben, unten, Abweichung —
`MP_BEFUND_HUELLE_OFFEN` hat den Wortlaut des Hinweises.

**Vorschlag „fehlendes Hüllbauteil aus der Körperfläche“** (`Modellvorschlagsregeln.Huellbauteil`, neue `Vorschlagsart.Huellbauteil`):

- Fläche = Lücke (Gegenrichtung minus Richtung; ohne Gegenrichtung die Nutzfläche), Neigung 0° (oben) bzw. 180° (unten), ohne
  Azimut, Herkunft `VORGABE`, Bezeichner „‹Art› (ergänzt)“.
- Lage: Ist die Zone in dieser Richtung über eine Trennfläche an einer Nachbarzone verankert und fehlt dieser die Gegenrichtung,
  eine **Decke an der Nachbarzone** (`ZONE`, `ID_Nachbarzone`) — sie schließt beide Zonen; die Prüfung läuft über eine
  Arbeitskopie, in der Vorschläge früherer Zonen mitzählen, so dass die Nachbarzone keinen zweiten Vorschlag bekommt. Ist sie
  verankert, ohne dass der Nachbarzone etwas fehlt, liegt die Lücke nicht an der Trennfläche (geneigtes Dach mit Neigung 0°):
  gelb ohne Vorschlag. Sonst oben ein **Dach an Außenluft**, unten eine **Bodenplatte gegen Erdreich** (unter der Zone ist keine
  verankert: unterstes Geschoss).
- U-Wert nach 2.3: (1) das U der Bauteile gleicher Art und Randbedingung des Gebäudes mit der meisten Fläche (U des Bauteils,
  sonst des Projektaufbaus), Herkunft „Projekt“; (2) sonst das U des Typaufbaus (`Ersatzaufbau.Vorgabetyp`, ohne Abgleich; an
  einer Trennfläche wie gegen unbeheizt), Herkunft „Typaufbau“. Kein Normwert im Repositorium — die Typaufbauten sind die
  vorhandene Saat.
- Das Bauteil trägt nur ein U (Stufe C): Nach der Übernahme ist die Zone gelb (`NUR_U_WERT` mit Aufbauvorschlag der Gruppe 2)
  statt rot.

**Übernahme und Rücknahme** über den bestehenden Weg von `ModellpruefungCtrl` („Vorschlag übernehmen“ der Gruppe 1, „Alle
Vorschläge übernehmen“): `GebaeudeZonenCtrl.BauteilEinfuegen` legt das Bauteil mit denselben Spalten wie das Speichern der Zone
an (Rang nach dem letzten Bauteil der Zone); die Rücknahme löscht es (`DELETE … WHERE "ID" = ?`). Die Oberfläche zeigt Befunde
und Vorschläge als Text des Kerns — keine Änderung an Razor oder Hülle.

Keine Leseregel des Rechenwegs berührt, kein Schemaschritt, kein Referenzprojekt beschrieben (die Prüfung liest; geschrieben wird
nur auf Zuruf). Referenzlauf: byte-gleich zu R52 erwartet, fährt die Orchestrierung.

## Dateien

- neu: `EPOS.Kern.Tests/ModellpruefungHuelleTests.cs`
- geändert: `EPOS.Kern/Allgemein/Simulation/Gebaeude/Modellpruefung/Modellpruefung.cs` (vier Codes, `HuelleJeZone`,
  `Gespiegelt`), `Modellvorschlagsregeln.cs` (`Huellbauteil`, `HuellU`, `AlsBauteil`), `Modellpruefungstypen.cs`
  (`Vorschlagsart.Huellbauteil`, Felder `Randbedingung`, `Neigung`, `Flaeche`, `IdNachbarzone` am `Pruefvorschlag`);
  `EPOS.Kern/Allgemein/Zonenkopplungsregeln.cs` (`SenkrechtOffen`); `EPOS.Kern/Controller/ModellpruefungCtrl.cs`
  (`HuellbauteilAnwenden`); `EPOS.Kern/Controller/GebaeudeZonenCtrl.cs` (`BauteilEinfuegen`);
  `EPOS.Kern/MyResource/Resource.resx`, `Resource.en-US.resx` (je 10 Schlüssel `MP_*`), `Resource.Designer.cs` (Designer,
  zweiter Lauf +0)

## Tests

- `ModellpruefungHuelleRegelTests` (10, ohne Datenbank): Gegenprobe geschlossen ohne Befund; fehlendes Dach rot mit Vorschlag
  Dach 100 m², Typaufbau-U, nach Übernahme gelb mit `NUR_U_WERT`; fehlende Bodenplatte gegen Erdreich mit dem U einer
  Bodenplatte des Gebäudes (0,42); offene Trennfläche (40 statt 100 m²) — zwei gelbe Befunde, ein Vorschlag Decke an der
  Nachbarzone 60 m² mit dem U der Geschossdecke, danach geschlossen; verankerte Zone mit geneigtem Dach (Fall 1052) gelb ohne
  Vorschlag; Zone nur mit Wänden → Dach und Bodenplatte je 80 m² aus der Nutzfläche; gespiegelte Bodenplatte benannt, Gegenprobe
  nicht; Befundtext gleich Hinweistext im Reiter „Zonen“; Texte beider Sprachen.
- `ModellpruefungHuelleDatenbankTests` (2, Arbeitskopie der Testdatenbank): 1054 meldet zwei `HUELLE_OFFEN` ohne Vorschlag und
  bleibt „rechnet mit Vorgaben“; ohne Kellerboden ist 1054 rot, „Vorschlag übernehmen“ (Gruppe 1) legt die Bodenplatte an —
  „rechnet mit Vorgaben“ mit `NUR_U_WERT`-Vorschlag —, die Rücknahme löscht sie, wieder rot.
- Build `WP-Plan.Kern.slnf` Release 0 Fehler. `EPOS.Kern.Tests` mit Filter
  `Modellpruefung|Huelle|GebaeudeImport|Zonenplan|Gbxml|Sqproj|Dokumentation|Repository|Resource|Modultrennung|Zonenkopplung`:
  1172 von 1172 grün (darin die bestehenden MA-1a-Tests, die Importproben unverändert). `EPOS.UI.Tests` mit Filter
  `Modellpruefung|GebaeudeDialogImport|ZonenNutzung`: 34 von 34 grün (keine UI-Datei geändert). SQL-Dialekt-Prüfer: 2924 Texte, 0 Fundstellen.

## Offen

- `HUELLE_OFFEN` gilt wie der Hinweis erst ab zwei Zonen (eine übernommene Einzelzone trägt die geneigte Dachfläche des
  Katalogbaus); die Verankerung oben/unten prüft jede Zone.
- Gespiegelte Bauteile ohne Vorschlag; „Neigung spiegeln“ wäre ein eigener Vorschlag (MA-5, an der Ansicht).
- Geneigte Dachflächen mit Neigung 0° (1052/1054) bleiben gelb ohne Vorschlag; die richtige Neigung trägt erst der Import bzw.
  die Pflege.
- Nimmt der Anwender nur Gruppe 1 zurück, nachdem Gruppe 2 dem ergänzten Bauteil einen Typaufbau gab, bleibt dieser Aufbau
  ungenutzt im Projekt (wie die Katalogkopien in MA-1a). Die Rücknahme gilt für die Sitzung.

## Logbuch-Vorschlag

„Die Modellprüfung meldet je Zone eine nicht geschlossene Hülle und schlägt das fehlende Dach, die fehlende Bodenplatte oder
Geschossdecke mit passender Fläche zur Übernahme vor.“

# KIF10 — Sperrgrund je Feld und Freigabe der Ausnahmen im Hilfe-Assistenten (Protokoll, 10.10.2026)

Statuszeile #897 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Konzepte
[`Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md`](../../../aktuell/Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md)
(Abschnitte 3.4 und 4) und [`Konzept_KI-Assistent_Aufgabensteuerung.md`](../../../aktuell/Konzept_KI-Assistent_Aufgabensteuerung.md)
(Abschnitte 4.5 und 5.2); Vorgänger [`KIF9_Setzer_Erzeugermasken_Protokoll.md`](KIF9_Setzer_Erzeugermasken_Protokoll.md).
Zweig `claude/assistent-freigabe` (Opus).

## Anlass

Anwenderauftrag vom 10.10.2026: „Es sollten alle Masken für den Assistenten freigegeben werden zu Werte setzen“ — Folge
der Fehlerwelle KIF9 (#890), deren offene Punkte (a) Energieträgerwechsel und (c) Freigabe der Ausnahmen waren.

## Entscheide

- **KIF9‑Q1 a:** Der Energieträgerwechsel durch den Assistenten wird benannt abgelehnt, nicht als datenbankwirksame Aktion
  gebaut.
- **Entscheidtafel der 24 Ausnahmen (10.10.2026):** 13 freigegeben — davon drei Ansichten als Spalte ihres Wirts und der
  Gebäudeimport nur mit seinem Kopf —, 12 belassen mit Grund, 7 davon aus Sicherheits- oder Sinngründen (Assistent selbst,
  Lizenz und Schlüssel, Rückfragen).

## Umsetzung je Commit

| Commit | Inhalt |
|---|---|
| `0643a893` | Haken `KiMaskenhaken.Sperrgrund` (Accessor `Feldsperre`), erster Schritt von `KiAktionenDialog.Schreibschutz` für `feld_setzen`, `formular_ausfuellen`, `reihe_setzen`; Absage `KI_FELD_GESPERRT` vor Vorschau und Bestätigung; Regel `ErzeugerSperre` (Energieträger von Heizkessel, BHKW, Stromspeicher, Photovoltaik; keine Projektzeile); 8 Tests |
| `04c16647` | Stromspeicher-Auslegung (26 Setzer) melden wie der Handweg (`Geaendert`, Betriebsfolgen), ohne Flotte Absage `KI_STROM_KEINE_FLOTTE`; Komponentenkonfiguration (9 Setzer) mit Sperrgrund ohne Anlage und Meldeweg `Neugezeichnet`; 7 Tests |
| `5b9264fe` | Leistungspreis der Stromspeicher-Auslegung gesperrt (`KI_STROM_LEISTUNGSPREIS_VON_HAND`); 1 Test |
| `68a1ddc9` | Importoptionen: Flotten-CSV (26 Felder), Ganglinien-Importoptionen (9), Spotpreisimport (2), Importkonflikte; Katalog 97; `KiImportoptionenTests` (8) |
| `dae73736` | Vorlagenübernahme, Projekt-Export/-Import, Projektbrennstoffe, Katalogdubletten; `KiUebernahmeWerkzeugTests` (8); Katalog 101 |
| `8755c256` | Ergebnis- und Größenansicht der Speicherflotte als Spalte `anzeige` der Stromspeicher-Auslegung (86 Felder); Helfer `KiSetzweg`; 3 Tests |
| `801b540e` | Gebäudeimport, neun Kopffelder; 23 Eingabestellen bleiben beim Anwender; Sperrgrund je Schritt (`KI_GIMP_*`); `KiGebaeudeImportTests` (3); Katalog 102 |
| `be110ecb` | Merge mit origin `6c637754` (siehe Merge-Befund) |

## Regel: Handweg schreibt sofort → Sperrgrund

Ein Feld, dessen Handweg sofort in die Datenbank schreibt, setzt der Assistent nicht über die Arbeitskopie, sondern lehnt
es vor der Rückfrage mit Feld und Grund ab. Drei Fälle: der Energieträger der vier Erzeugermasken (`TraegerWechseln`),
der Leistungspreis der Stromspeicher-Auslegung (`LeistungspreisSchreiben`, projektweit). Gegenbeispiele: die
Wärmepumpe (Anlage) und die Komponentenkonfiguration — der Energieträger bleibt setzbar, weil erst OK schreibt.

## Zahlen

Katalog 93 → 102 Masken, Ausnahmen 24 → 12 (nach `KiAusnahmegrund`: Assistent 3, Lizenz oder Schlüssel 2, Anlegen oder Entfernen 1
(Namensdialog), Import 1 (Katalogimport), Aktion 3 (Projektwahl, BK-Übernahme, Wärmepumpen-Katalogdialog), Rückfrage 1
(Wertabfrage), Feld des Wirts 1 (Betriebsmodus)). Ressourcen 16 945 Schlüssel. Tests nach dem Merge (Filter): EPOS.Kern.Tests 2 073, EPOS.UI.Tests 1 797,
KiKern.Tests 549, SpeicherEngine.Tests 153, SpeicherPlanung.Tests 27 (+1 übersprungen), 0 rot; Kern-Filter und
Windows-Schale je 0 Fehler. Gate 897: ⟨Zahlen folgen⟩.

## Merge-Befund Bedarfsgrafik

origin #894 entfernte `BedarfGangGrafik.razor` (Grafikreiter mit Bedarfsart und Zeitraster). Das Feld `zeitstufe` des
Wirts `BEDARF_ERGEBNIS` setzt nun das Zeitraster Jahr/Monat/Woche/Tag des Grafikreiters über denselben Weg wie der Klick
(`KI_DLG_BERG_ZEITSTUFE_*`, Sperrgrund ohne Bildquelle). Ressourcen beidseitig vereinigt (origin +33/−3, Welle +98).

## Offen

1. Absagen im Konfliktraster des Herstellerimports kommen erst nach der Bestätigung (`Feldsperre` kennt keine Zeile).
2. Öffnungsziele als Behelf: Importkonflikte → Startseite, Ganglinien-Optionen → Stromganglinien-Verwaltung.
3. Nordrichtung des Gebäudeimports ohne Test; vier Bauteilfilter und `_nurBC` bleiben als Anzeigefilter draußen.
4. PV-Dialog ohne Sicht: `Katalog_`-Felder scheitern erst nach der Bestätigung; ohne Zeile „Feld unbekannt“.
5. Stromspeicher: Veraltet-Marke nur indirekt geprüft; `groessen_optimieren` stellt nur den Schalter.
6. Bedarfsgrafik: `jahresverlauf` und `zeitstufe` sprechen beide das Raster an; Feldkennung `zeitstufe` ggf. `zeitraster`.
7. Die 12 verbleibenden Ausnahmen (Vorschlag beim Anwender: belassen).
8. Wiki-Upload der Seite Hilfe-Assistent und des Logbuchs mit dem nächsten Sammel-Upload.
9. Windows-Sichtabnahme durch den Anwender: „setze Rücklauftemperatur auf 65“ in Verwaltung Heizkessel; „setze Energieträger …“
   wird vor der Rückfrage benannt abgelehnt.

## Verweise

Statuszeile #897 und Nach #897; KIF9 ([`KIF9_Setzer_Erzeugermasken_Protokoll.md`](KIF9_Setzer_Erzeugermasken_Protokoll.md));
Konzepte Dialogintegration 3.4/4 und Aufgabensteuerung 4.5/5.2; Wiki-Quelle
`Projekte/Wiki/Programm Dokumentation - Hilfe-Assistent.wiki`.

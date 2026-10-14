# Protokoll KB-A — Kältefolge im Kern, DTO des Kältebereichs, eigene Projektkopie je Kälteanlage (KB-1)

**10.10.2026 · Sitzung Gebäudesimulation · Zweig `gs-kba`.** Grundlage:
[Entwurf Kältebereich](../../../aktuell/Gebaeudesimulation/2026-10-10_Entwurf_Kaeltebereich.md), Welle KB-A und Festlegung
KB-1; Anwenderentscheid E117 (F1 bis F4). Kein Schemaschritt, keine Seitenänderung.

## Ergebnis

1. **Kern — eine Quelle der Kältefolge:** `EPOS.Kern/Allgemein/Simulation/Kaelte/Kaeltefolge.cs`. Die Stufen stehen fest
   (`Kaeltefolge.Stufen`: freie Kühlung, Kältespeicher, Wärmepumpen, Kältemaschinen), die Ordnungsregeln einmal:
   `ErzeugerOrdnen` (Stufe, dann Platz in der Stufe, stabil), `KaeltemaschinenOrdnen` (nach Anlagen-ID),
   `KaeltespeicherOrdnen` (Entladepriorität, 0 hinten, stabil). Der Lauf ordnet damit
   (`KaelteerzeugerVorbereiten` nach `KaeltemaschinenVorbereiten`, `KaeltemaschinenVorbereiten`, `KaeltespeicherLesen`), das
   Schema ebenso (`SchemaModell.KaelteBahnAnlegen`: Wärmepumpen in Modulfolge, Kältemaschinen, Kältespeicher). Die Liste der
   Wärmepumpen-Module liest `SimulationControl.WaermepumpenanlagenLesen` (aus `WP_Liste_Laden` herausgelöst, dasselbe SQL).
   Der Leser `Kaeltefolge.Lesen(idProjekt)` liefert `KaeltefolgeStand`: Projektschalter, Kälteerzeuger in Folge (Name, Art,
   Nennkälteleistung, Anzahl, Kühlbetrieb, Sperrgrund, in der Wärmekaskade, Vorlauf, Hilfsstrom, Kühlträger, eigener Zähler,
   freie Kühlung, Rückkühlart, Anlagen je Kopie), Kältespeicher in Entladefolge und die freie Kühlung.
2. **DTO und Hülle:** `EPOS.UI/Seiten/Simulation/KaeltebereichDaten.cs` (`KaeltebereichDaten`, `KaelteerzeugerZeile` samt
   fertiger `ErzeugerKachelDaten`, `FreieKuehlungZeile`, Aufzählung `KaelteStufe`); `SimulationKonfigDaten.Kaeltebereich`;
   Schreibweg `SimulationKonfigDienste.KuehlbetriebWpSchreiben` (Kernweg `WaermepumpeGeraeteCtrl.KuehlbetriebUmschalten`,
   sofort); der Projektschalter bleibt `SimulationParameterDienste.KuehlbetriebSchreiben`. Hülle
   `EPOS.UI.Daten/Simulation/KaeltebereichBau.cs`, gerufen aus `SimulationKonfigHuelle.Laden`; die Kältespeicher kommen als
   Speicherkacheln der Speicherspalte. Keine Plattformnaht: alle Wege sind Kernwege und gelten auf beiden Plattformen.
3. **KB-1:** `KaeltemaschineCtrl.EigeneKopieAnlegen` legt immer eine neue Kopie an; `KaeltemaschineAnlageCtrl.Anlegen` nimmt
   sie. `AusKatalogUebernehmen` behält die Wiederverwendung für Bestandsaufrufer. Löschen nimmt die eigene Kopie mit (der
   vorhandene Weg „Kopie weg, wenn keine Anlagenzeile sie mehr führt“); Projekt duplizieren versetzt jede Kopie und hält die
   Zuordnung. **Testdatenbank:** drei Kältemaschinen-Anlagen (1055, 1059, 1063), je eine eigene Kopie — keine geteilte Kopie,
   keine verwaiste Kopie.

## Nachweis

- `EPOS.Kern.Tests/KaeltefolgeTests` (4): Ordnungsregeln; Leser gleich dem Lauf (1055 mit Kältemaschine und Kältespeicher,
  1017 mit Wärmepumpe und zwei Kältemaschinen); Hülle samt Ladestand der Seite und Schreibweg des Kühlbetriebs.
- `KaeltemaschineAbrechnungTests`: der Fall „teilen eine Projektkopie“ ist durch KB-1 ersetzt (zwei Anlagen, zwei Kopien,
  getrennte Felder, Löschen ohne verwaiste Kopie); neu: geteilte Bestandskopie bleibt, Projekt duplizieren hält die
  Zuordnung.
- Filter `Kaelte|Schema|Kuehl` des Kern-Testprojekts grün (1 257 Fälle); bunit `SimulationKonfig|Kaeltemaschine` grün
  (197); Dokumentationswachen grün; `SqlDialektPruefer` ohne Fundstelle. Referenzlauf der Projekte 1017, 1047, 1055, 1058,
  1059, 1061, 1062, 1063, 1064 gegen `2026-10-10_R51_FreieKuehlung`: PASS und byte-gleich. Kern-Filter und Windows-Schale auf
  Linux ohne Fehler.

## Offen

- **KB-B:** Bereich „Kälte“ der Seite zeichnen (Kopf, Schalterumzug, Kacheln aus `KaelteerzeugerZeile.Kachel`, Sperrgrund
  und „nicht in der Wärmekaskade“ am Element, Hinweis „gilt für n Anlagen“ aus `AnlagenJeKopie`, Herleitungszeile aus
  `Folge`); Ressourcen für Folge, freie Kühlung und eigenen Zähler. Die Kältespeicher stehen noch zusätzlich in der
  Speicherspalte der Wärme — herausnehmen, sobald der Bereich sie zeigt (sonst verschwänden sie vorher).
- **KB-D:** Schemaschritt 212 (`Kaelte_Rang`) als Quelle der Platzregel in `Kaeltefolge.ErzeugerOrdnen`; Lauf, Schema und
  Leser ordnen schon darüber.

# Protokoll KB-B — Bereich „Kälte“ der Simulationskonfiguration

**10.10.2026 · Sitzung Gebäudesimulation · Zweig `gs-kbb` (auf KB-A `ad3f4e0d1`).** Grundlage:
[Entwurf Kältebereich](../../../aktuell/Gebaeudesimulation/2026-10-10_Entwurf_Kaeltebereich.md), Abschnitt 3 und Welle KB-B;
Anwenderentscheid E117 (F1 bis F5). Kein Schemaschritt, kein Rechenweg.

## Ergebnis

1. **Seite** (`EPOS.UI/Seiten/Simulation/SimulationKonfigSeite.razor`): Bereich `section.epos-simkonfig-kaelte` unter den
   Spalten Erzeuger/Speicher, im selben `fieldset` (gleiche Sperre). Kopf: `Gruppenkopf` „Kälte“, darunter der Schalter
   „Kühlung rechnen“ (aus „Weitere Einstellungen“ umgezogen; Weg `KuehlbetriebSchreiben`, `@key`-Rückfall, Herleitungszeile,
   InfoKnopf unverändert). Links die Kälteerzeuger aus `KaeltebereichDaten.Erzeuger` als `ErzeugerKachel` (Nummer = Rang,
   keine Pfeile), Parameterbereich mit Sperrgrund, „nicht in der Wärmekaskade“, „gilt für n Anlagen“ und „Konfiguration…“;
   darunter die Lesezeile der freien Kühlung und die Herleitungszeile der Folge aus `Folge`. Rechts die Kältespeicher als
   `SpeicherKachel` (Bearbeiten = Pufferverwaltung). Bei „aus“ zugeklappt mit Hinweiszeile (Anzahl Kältemaschinen,
   Wärmepumpen mit Kühlfunktion, Kältespeicher). Ohne Schalter und ohne Kälteanlagen steht der Bereich nicht.
2. **Wärmepumpe:** Aufnehmen/Entfernen an der Kachel = Kühlbetrieb an/aus, sofort über
   `SimulationKonfigDienste.KuehlbetriebWpSchreiben`; Sperrgrund oder Ablehnung im Banner. „Konfiguration…“ öffnet die
   Gruppe „Kühlbetrieb“ als Überlagerung; die Gruppe ist als Baustein `WaermepumpeKuehlbetriebGruppe` aus
   `WaermepumpeKonfiguration` herausgelöst und steht in beiden. OK schreibt über `WaermepumpeKonfigurationSpeichern`.
3. **Kältemaschine:** neue Komponente `EPOS.UI/Dialoge/Erzeuger/KaeltemaschineKonfiguration.razor` (Name, Anzahl mit
   Folgeschaltung, Kaltwasservorlauf, Hilfsstrom, Kühlträger, eigener Zähler, Hinweis „gilt für n Anlagen“, Gerätezeile auf
   Wunsch), aus `KaeltemaschineAnlageDialog` herausgelöst; der Dialog bindet sie ein und ist sonst unverändert. In der Seite
   als Überlagerung über `KaeltemaschineKonfigurationLaden`/`…Speichern` (Hülle: `KaeltemaschineAnlageHuelle.Liste`,
   `Pruefen`, `Speichern`); eine Ablehnung hält die Überlagerung offen.
4. **Schema:** Doppelklick auf Kältemaschine oder Rückkühlung öffnet die Konfiguration in der Seite; ohne Weg bleibt die
   Navigation zum Dialog. Tooltipptexte `SIM_SCHEMA_DK_KAELTEMASCHINE`/`…_RUECKKUEHLUNG` nachgezogen.
5. **Hülle:** Kältespeicher aus `SimulationKonfigDaten.Speicher` herausgenommen (F4).
6. **KI:** `SimulationKiSicht.Kaelteerzeuger`, `.Kaeltespeicher` (nur lesend), `.KuehlbetriebWaermepumpen` (Namensliste,
   sofort über die Seite `KiKuehlbetriebWpSetzen`); Katalog `Simulation` 72 → 75 Felder. Wache: neue Wirte
   `WaermepumpeKuehlbetriebGruppe`, `KaeltemaschineKonfiguration`, Eingabestellen umgebucht.
7. **Ressourcen:** 32 neue Schlüssel in beiden Sprachen (`SIMKONF_GRP_KAELTE`, `SIMKONF_KAELTE_*`, `KMK_GERAET_ZEILE`,
   `KI_DLG_SIM_KAELTE*`, `KI_DLG_SIM_KUEHL_*`), zwei geänderte; `designer_neu.py` ohne Befund.

## Prüfungen

- Kern-Filter grün; Windows-Schale auf Linux 0 Fehler.
- bunit: 1 067 Fälle der Filter `SimulationKonfig|Kaeltemaschine|Schema|KiSimulation|KiMaskenabdeckung|Waermepumpe|
  KomponentenKonfiguration|KiDialogkatalog|Kuehl|Simulation` grün, darunter neu `SimulationKonfigKaeltebereichTests` (17),
  `KaeltemaschineKonfigurationTests` (6), zwei Doppelklickfälle in `SimulationKonfigKaeltebahnTests`, ein Fall im Dialog.
- Kern: 490 Fälle `Kaelte|SimulationKonfig|KiDialog` grün (`KaeltefolgeTests` um F4 und die Kältemaschinen-Konfiguration).
- Referenzlauf 1017 und 1055 gegen `2026-10-10_R51_FreieKuehlung`: PASS, byte-gleich.
- Rasterprobe: 28 von 29 Fällen; der eine rote (`Z6_kategorien_ueberlagerung_1088x624`, Hülle rollt quer um 4 px) ist mit
  dem Stilblatt von KB-A ebenso rot — Fremdbefund, nicht KB-B. Die Simulationskonfiguration steht in keiner Probe; der
  Probenfall `simkonf-kaelte` ist KB-C. `fensterprobe.mjs` erfüllt (Gegenprobe rot), `rollbereichprobe.mjs` grün (die
  bekannte Ausnahme `kaeltemaschine`, Katalogdialog, bleibt bis Stufe 5).
- `DokumentationLinkWache`, `WikiProduktdatenWache`, `RepositoryOrdnungWache`: 35 Fälle grün.

## Offen

- **KB-C:** Probenfall `simkonf-kaelte` (Rollbereich, 1 280 × 800, 1 280 × 720, 1 210 × 834), `fensterprobe.mjs` für die
  Überlagerungen, Logbuch beim Upload.
- **KB-D:** Pflege der Folge (`Kaelte_Rang`, Schemaschritt 212), Pfeile an den Kacheln.
- **Stufe 5 (Dialoge und Korrekturen):** Dialog auf `Zweispaltenauswahl`; die Betriebsfelder verlassen dann den Dialog,
  die Komponente bleibt. Der Block „Wärmepumpen im Kühlbetrieb“ im Dialog ist unverändert (Schalter, schreibt beim OK).
- Kopf „Wärme“ über dem Wärmeteil (Entwurf 3.1) nicht gebaut; die Wärmepumpe im Kühlbetrieb öffnet im Schema weiter ihren
  Senkendialog (Entwurf 3.6, zweiter Satz).

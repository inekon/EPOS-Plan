# Protokoll KIS — KI-Sicht für Fensterwerte, Abschnitte und U-Herkunft

Nachzug aus der Nachzugsliste der
[Übergabe](../../../aktuell/Gebaeudesimulation/2026-10-12_Uebergabe_Gebaeudesimulation_Kontowechsel.md) („KI-Sicht für
Fensterwerte und Abschnitte“), Muster [Entwurf Kältebereich](../../../aktuell/Gebaeudesimulation/2026-10-10_Entwurf_Kaeltebereich.md)
Abschnitt 5.4. Arbeitsort Worktree `kis`, Zweig `kis`, Basis `836407033` (Runde r6, Schema 233). Kein Schemaschritt, keine
Änderung am Rechenweg.

## Ausgangslage

- Verwaltung „Bauteilaufbauten“ (`KiMaskennamen.BAUTEILAUFBAU`): Die fünf Fensterwerte des Aufbaus (BK-6: U_g, g-Wert,
  Rahmenanteil, U_f, ψ_g) und Abschnitt und Flächenanteil einer inhomogenen Schicht (BK-5) standen in der Maske, aber nicht in
  der Feldkarte; die Eingabebilanz führte beide mit dem Vermerk „meldet die Sicht noch nicht an“. Summenfuß (U, R'_T, R''_T) und
  Ebenensumme waren für den Assistenten nicht lesbar.
- Bauteildialog (`KiMaskennamen.BAUTEIL`): Die Übernahme der Fensterwerte beim Wählen eines Fensteraufbaus lief über den
  Setzweg des Felds `aufbau` bereits mit, die Erläuterung nannte sie nicht. Die Herkunft des U-Werts (Schritt 233,
  `Tab_Bauteil.U_Herkunft`) war weder in `BauteilDaten` noch in der Sicht; der Dialog zeigt sie nicht.

## Umsetzung

- Feldkarte `Bauteilaufbau`: `fenster_ug`, `fenster_g`, `fenster_rahmenanteil`, `fenster_uf`, `fenster_psi_glasrand`
  (schreibend, Grenzen der Zahlenfelder; ein Setzen wirkt wie die Maske nur bei Fenster und Vorhangfassade),
  `schicht_abschnitt` und `schicht_flaechenanteil` (schreibend über `AbschnittSetzen`/`AnteilSetzen` des Arbeitsstands;
  „Abschnitt daneben anlegen“ bleibt ein Klick), `schicht_ebenensumme`, `summe_u`, `summe_rt_oben`, `summe_rt_unten` (nur
  lesend, aus dem Summenfuß des Kerns). Die Maske hatte schon schreibende Felder; berechnete Werte sind nur lesend.
- Feldkarte `Bauteil`: `u_herkunft` (nur lesend). Die Herkunft folgt den Regeln des Schreibwegs über die neue Kernklasse
  `BauteilUHerkunftVorschau` (ruft `GebaeudeZonenCtrl.UHerkunftNeu`/`UHerkunftNachPflege`); `BauteilDaten.UHerkunft` füllt
  `GebaeudeKatalogHuelle` beim Lesen, der Schreibweg bleibt unverändert. Die Erläuterung von `aufbau` nennt die Übernahme von
  g-Wert, Rahmenanteil und U_w.
- Ressourcen: 17 Schlüssel je Sprache neu, `KI_DLG_BT_AUFBAU_ERL` ergänzt; Designer neu erzeugt.
- Eingabebilanz `KiMaskenabdeckungWacheTests`: Zahlen unverändert (`BauteilaufbauDialog` 12, `BauteilschichtenFelder` 8 — die
  Felder standen schon im Markup); die Vermerke „meldet die Sicht noch nicht an“ entfallen, Begründung im Kommentar.

## Abnahme

- Bau `WP-Plan.Kern.slnf` (Release): 0 Fehler. Die Windows-Schale ist nicht berührt.
- `EPOS.UI.Tests`, Filter `.Ki`, `Bauteil`, `Baustoff`, `Ressource`/`Resource`: 698 grün (darin
  `KiMaskenabdeckungWacheTests`, `KiDialogkatalogTests` und fünf neue Fälle in `BauteilaufbauDialogTests` (2) und
  `BauteilDialogTests` (3)).
- `EPOS.Kern.Tests`, derselbe Filter: 973 grün, 1 übersprungen.
- Ein erster Kern-Lauf scheiterte an vollem Datenträger (Kopie der Testdatenbank, `No space left on device`); nach dem
  Aufräumen von `bin/`/`obj/` grün.

## Offen

- Der Bauteildialog zeigt die Herkunft des U-Werts nicht selbst (nur der Steckbrief der Gebäudeansicht); der Assistent liest
  sie als einziger dort. Eine sichtbare Zeile im Dialog wäre ein eigener Auftrag.

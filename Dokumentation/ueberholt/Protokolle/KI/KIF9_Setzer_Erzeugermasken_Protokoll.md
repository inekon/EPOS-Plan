# KIF9 — Setzer der Erzeugermasken im Hilfe-Assistenten (Protokoll, 10.10.2026)

Statuszeile #885 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Konzepte
[`Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md`](../../../aktuell/Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md)
(Abschnitt 3.4) und [`Konzept_KI-Assistent_Aufgabensteuerung.md`](../../../aktuell/Konzept_KI-Assistent_Aufgabensteuerung.md)
(Abschnitte 4.5 und 5.2); Vorgänger [`KIF1_Erzeugermasken_Protokoll.md`](KIF1_Erzeugermasken_Protokoll.md) und
[`KIF8_Oeffnungswege_Protokoll.md`](KIF8_Oeffnungswege_Protokoll.md). Zweig `claude/assistent-freigabe`, Fehlerwelle
`d2160d7b` (Opus).

## Anlass

Anwendermeldung vom 10.10.2026 mit Bildschirmfoto: Im Projektdialog „Verwaltung Heizkessel“
(`EPOS.UI/Dialoge/Erzeuger/HeizkesselDialog.razor`, Maske `Form_Heizkessel`) ließ „setze Rücklauftemperatur auf 65“ im
Hilfe-Assistenten nichts geschehen.

## Befund mit Beleg

- Die Setzer der Sichtklasse `ErzeugerProjektKiSicht` (Heizkessel, BHKW, Stromspeicher, Pufferspeicher) schrieben nur in
  die `ErzeugerZeile`, ohne den Übernahmeweg `Uebernehmen`, den die Eingabe von Hand (`BeiRuecklauf`) ruft. Der Wert
  erreichte die Arbeitskopie der Hülle (`HeizkesselHuelle`, `m.Vorlauf`/`m.Ruecklauf`) nicht und ging beim Speichern von
  Hand verloren.
- Ohne gewählte Projektzeile (etwa eine Katalogzeile angeklickt) verwarf der Setzer den Wert still
  (`if (Zeile is ErzeugerZeile z)`); der Assistent meldete trotzdem „gesetzt“. Mit genau einer Zeile und ohne Wahl blieb
  die Zeile ungewählt.
- `KiMaskenanmeldung` verwarf Werte für jede Maske ohne Daten-Objekt still (`if (stand is not null)`).
- Beleg: rote Tests vor der Behebung — das Setzen des Rücklaufs am gezeichneten `HeizkesselDialog` wirkte nicht wie die
  Eingabe von Hand, das Setzen ohne Zeile lehnte nicht ab, das Setzen mit einer Zeile wählte sie nicht.

## Nicht Ursachen

- `KiMaskenbruecke` ist prozessweit statisch, `Abmelden` läuft nur über `Dispose` beim Schließen; das Chatfenster sieht
  die Anmeldung.
- Die Maskenliste, die der Anwender sah, ist die Wiki-Seite Hilfe-Assistent, keine Absage.

## Änderungen je Datei

- `ErzeugerProjektKiSicht.cs`: alle Setzer über `Schreibe(…)` — gewählte Zeile, sonst die einzige Zeile über den
  Delegaten `Einzelwahl`, sonst benannte Absage (`InvalidOperationException`, bestehende Absage `KI_FELD_SETZEN_FEHLER`,
  wie `BedarfsProfileKiSicht` und `GebaeudeKiSicht`); nach Vorlauf, Rücklauf und Grenzleistung ruft die Sicht den
  Delegaten `Uebernommen` = `Uebernehmen` des Dialogs (Arbeitskopie, nicht Datenbank).
- `PhotovoltaikKiSicht.cs`: dasselbe Muster (`Uebernommen` ruft `Uebernehmen` und `GesamtNeu()`).
- `SolarkollektorenKiSicht.cs`: `Einzelwahl` und Absage; der Übernahmeweg ist nicht nötig, die Hand schreibt dort ebenfalls
  nur in den Arbeitsstand bis „Übernehmen“.
- `WaermepumpeAnlageKiSicht.cs`: Vorlauf über den Delegaten `VorlaufWeg` = `BeiVorlauf`; er zieht den Rücklauf mit, wenn
  er auf der Vorgabe steht.
- `EPOS.UI/Dienste/KiMaskenanmeldung.cs`: ohne Daten-Objekt Absage `KI_FELD_KEIN_SATZ` (Reflection und Feldtafel).
- Dialoge Heizkessel, BHKW, Stromspeicher, Photovoltaik, Solarkollektoren, Wärmepumpe (Anlage) reichen die Wege herein.
- Ressourcen: `KI_ERZ_KEINE_PROJEKTZEILE`, `KI_FELD_KEIN_SATZ` in beiden Sprachen, Designer neu erzeugt.
  `KiMaskenhaken` unverändert.

## Tests

13 neue Fälle in `EPOS.UI.Tests/Dialoge/Hilfe/KiFeldSetzenTests.cs`: drei bunit-Fälle am gezeichneten `HeizkesselDialog`
(`Verwaltung_Heizkessel_Ruecklauf_setzen_wirkt_wie_die_Eingabe_von_Hand`, `…_ohne_gewaehlte_Zeile_lehnt_benannt_ab`,
`…_mit_einer_Zeile_waehlt_sie_wie_beim_Oeffnen`), die Theorien `Erzeugerzeile_Setzen_uebernimmt_wie_die_Hand` (5) und
`Erzeugerzeile_ohne_Zeile_lehnt_benannt_ab` (2) sowie `Photovoltaik_Setzen_uebernimmt_und_ohne_Zeile_lehnt_es_ab`,
`Solarkollektoren_ohne_Zeile_lehnt_benannt_ab`, `Ohne_Daten_Objekt_lehnt_das_Setzen_benannt_ab`.
Build Kern-Filter 0 Fehler; KI-Tests 1 651 grün (EPOS.UI.Tests 626, EPOS.Kern.Tests 475, KiKern.Tests 549,
SpeicherEngine.Tests 1), Dialogtests der betroffenen Masken 549 grün. Gate 885: ⟨Zahlen folgen⟩.

## Offen

1. **KIF9‑Q1** Energieträgerwechsel durch den Assistenten (Heizkessel, BHKW, Stromspeicher, Photovoltaik) erreicht weder
   Arbeitskopie noch Datenbank — von Hand schreibt `TraegerWechseln` sofort in die Datenbank, `feld_setzen` ist als nicht
   datenbankwirksam deklariert. Entscheid beim Anwender: (a) benannt ablehnen, (b) datenbankwirksame Aktion mit
   Sicherungspunkt.
2. Übrige Sichtklassen mit still verwerfenden Setzern im Muster `set { if (…) }`: `GebaeudeKatalogKiSicht` (57),
   `KomponentenKonfigurationKiSicht` (9), `ProjektKopfKiSicht` (3), `KostenprofilKiSicht` (2), je einer in
   `QuellprofilKiSicht`, `TypProfilKiSicht`, `GebaeudetypKiSicht`, `LeistungspreisReiheKiSicht` — Folgewelle.
3. Freigabewelle: von den 24 Ausnahmen in `EPOS.Kern/Allgemein/KI/Dialoge/KiDialogAusnahmen.cs` sollen 17 angemeldet
   werden (Import 6, Aktion/Werkzeug 8, Anzeige 3), 7 bleiben (Lizenz/Schlüssel 2, Assistent selbst 3, Wertabfrage,
   Betriebsmodus) — Vorschlag beim Anwender vom 10.10.2026.
4. Die Absage ohne gewählte Zeile kommt erst nach der Bestätigung; für eine frühere Absage bräuchte `KiFeldzugang` einen
   Sperrgrund.
5. Das Chatprotokoll des Anwenders liegt nicht vor; ob das Modell damals `feld_setzen` rief, bleibt offen.

## Verweise

Statuszeile #885 und Nach #885; Konzepte Dialogintegration 3.4 und Aufgabensteuerung 4.5/5.2 (siehe oben); Wiki-Quelle
`Projekte/Wiki/Programm Dokumentation - Hilfe-Assistent.wiki` (Upload mit dem nächsten Sammel-Upload).

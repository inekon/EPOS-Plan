# P671 — Katalogempfehlung der Hilfsenergie auf Weg B, Satzfeld im Kostenraster (Protokoll, 03.10.2026)

Statuszeile vorläufig #671 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md) (die Orchestrierung prüft
die Nummer beim Push; mit dieser Welle keine Statuszeile); Auftrag
[`P671_Auftrag_2026-10-03.md`](../Auftraege_Wirtschaftlichkeit_2026-09/P671_Auftrag_2026-10-03.md) der Sitzung „EPOS Plan
Wirtschaftlichkeit". Vorgänger: [`P654_Ausweis_9b_Deckel_Protokoll.md`](P654_Ausweis_9b_Deckel_Protokoll.md) (#656). Zweig
`p671` ab `bd5811bf9` (Auftrag auf `1020678cc`, origin/ios_migration_september mit #670).

## Anlass und Entscheid

Von den E30-Resten (Statusdatei Nach #548 (f), Register E30‑Q5, Q7, Q12) waren zwei ohne Anwenderfrage baubar: (1) das
Satzfeld des Kostenrasters blieb an einer Zeile leer, deren Satz aus dem Hilfsenergieanteil der Anlage kommt; (2) die
Katalogempfehlung der Hilfsenergie von BHKW (2–4 %) und Heizkessel (4–8 %) stammte aus Weg A (Anteil der Brennstoffkosten),
die Pflichtzeilen rechnen aber seit Schritt 94 nach Weg B (Anteil des Endenergiebedarfs als Strommenge) — am Kessel von
1030 ergäbe die Spanne 54.000–108.000 €/a. Anwenderentscheid 03.10.2026 (**EZ‑24**): „Setze jetzt fort" nach der
Empfehlung der Wirtschaftlichkeit; N11 (Flotten-Netzeinspeisung) und E30‑Q5 (Emission und Stromsteuer des Hilfsstroms)
bleiben benannt. Ein Katalogschritt (167), kein Rechenweg der Simulation, keine neue Basis (R32).

## Regeln

1. **Spannen in Weg B.** Heizkessel 1,0–2,0 % (Brenner, Gebläse, Regelung, Pumpen des Kesselkreises; nach VDI 2067 Blatt 1
   und üblichen Herstellerangaben rund 1 % des Brennstoffeinsatzes bei Gebläsekesseln, bis 2 % bei Festbrennstoff), BHKW
   0,5–1,5 % (Eigenbedarf 1–3 % der elektrischen Arbeit, auf den Brennstoffeinsatz bezogen 0,4–1 %, zuzüglich Pumpen).
   Gegenprobe als Umrechnung der alten Spanne mit Brennstoffpreis ÷ Strompreis (8 ÷ 30 ct/kWh): Kessel 1,07–2,13 %, BHKW
   0,53–1,07 %; Untergrenze und Mitte liegen in der neuen Spanne, die Obergrenze am Kessel um die Rundung darüber.
2. **Eine Quelle.** Die Werte stehen in der Saat `SchemaKatalog.Schritt39_Vorlagen`; der Schritt liest seine Zielwerte dort.
3. **Katalogschritt 167** (`HilfsenergieEmpfehlungNachzug`, Nummer `BedarfNetzKalenderSchema.SCHRITT + 1`, `Art.Katalog` in
   der Paketanhebung): `UPDATE Tab_KostenVorlagePosition SET Empfehlung_von, Empfehlung_bis` nur an Zeilen mit der
   Bezeichnung der Pflichtzeile, Bemessung `PROZENT_ENDENERGIEBEDARF` und noch der alten Spanne, deren Vorlage
   `ReadOnly = 1`, Kategorie Betrieb und Komponente BHKW bzw. Heizkessel ist. Projektzeilen (`Tab_ProjektWerte` führt keine
   Empfehlung), eigene Vorlagen (`ReadOnly = 0`) und Weg-A-Zeilen bleiben unberührt; wiederholbar.
4. **Satzfeld.** Stammt der Satz einer Nachweiszeile aus dem Anteil (`SatzHerkunft = ANLAGENANTEIL`), trägt die Rasterzeile
   des Kerns ihn als `SatzAusAnlagenanteil` samt Herkunftszeile „2 % · Satz aus dem Hilfsenergieanteil der Anlage"
   (Ressource `HILFS_ANTEIL_HERKUNFT`, beide Sprachen, kein neuer Schlüssel); die Hülle zeigt ihn im gesperrten Satzfeld
   (`KostenPositionZeile.SatzGesperrt`) und schreibt ihn nie in die Position — ein eigener Satz hätte Vorrang (E30‑Q2 a).

## Code

- `EPOS.Kern/Allgemein/Update/HilfsenergieEmpfehlungNachzug.cs` (neu), `SchemaKatalog.cs` (Saat), `SchemaStand.cs`
  (`Zielversion`), `Paketanhebung.cs` (Stufe 167), `EPOS.Kern/Allgemein/DbWerte.cs` (Kommentare beider Wege).
- `WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs`: `SCHRITT_HILFSENERGIE_EMPFEHLUNG`, Registereintrag hinter
  166, `Schritt_HilfsenergieEmpfehlung` nach dem Muster von 158.
- `EPOS.Kern.Tests/TestDatenbank.cs`, `Werkzeuge/Testdatenbankschema/Program.cs`: der Schritt hinter 166.
- `EPOS.Kern/Controller/KostenProjektPositionenCtrl.cs` (`SatzAusAnlagenanteil`, `SatzAusAnteilUebernehmen`),
  `EPOS.UI.Daten/Kosten/KostenKomponenteHuelle.cs`, `EPOS.UI/Dialoge/Kosten/KostenKomponenteDaten.cs`, `VorlagenZeile.razor`,
  `KostenKomponenteDialog.razor`.
- Testdatenbank mit dem Werkzeug auf 167 (Zeilen 62 und 68), `--trocken` danach 0 offen, `integrity_check` ok; LFS-SHA-256
  `8f172db10e401a15c7140a1eedfa85a360f8f244893573c06a6a24c893cf05af`, 81 195 008 Byte.
- Fremder Befund: `Werkzeuge/Auslieferungsvorlage.Tests/VorlageTests.cs` STRICT-Pin 153 → 154 (Schritt 166,
  `Tab_Betriebskalender`); origin hatte ihn nicht nachgezogen.

## Tests

- `HilfsenergieEmpfehlungNachzugTests` (neu, 8): Nummer und Ziel, Register, Saat samt Herleitung, Stand davor und
  Wiederholung (Tab_ProjektWerte zellgleich), eigene Vorlage und Weg A unberührt, Gegenprobe 1030 mit eigenem Satz 1,5 %
  (Betrag, Summe, Kapitalwert vorher = nachher), Repo-Datei, Werkzeug/Migration/Vorrichtung.
- `HilfsenergiekostenAusAnteilTests` +2 (Leseweg des Kostenrasters; Hülle samt Nachziehen und Speichern), bUnit
  `KostenKomponenteDialogTests` +1, `VorlagenZeileTests` +1.
- Gefilterter Lauf (Hilfsenergie, Hilfsstrom, Betriebskosten, Bemessung, Kosten, Wirtschaftlichkeit, Paketanhebung,
  SchemaStand, Zielversion, BerichtVorlagenMesslatte, Lokalisierungs-, Hüllen-, Dokumentations-, Ordnungs- und Wiki-Wachen,
  Anker, StromGruppenregel): Kern 785 → 795, UI 594 → 595, alle grün; `Auslieferungsvorlage.Tests` 43/44 → 44/44.
- Gegenproben: Saat, ReadOnly-Bedingung, Testvorrichtung, Leseweg, Rückschreibungswache und Razor-Sperre je ausgehängt → die
  zugehörigen Tests rot, wieder eingehängt → grün.
- Referenzlauf der CI-Auswahl (1030, 1007, 1017, 1045, 1046, 1047, 1049) gegen R32: 7/7 PASS. SqlDialektPruefer 0
  Fundstellen. Sechs Bericht-Messlatten unverändert. Windows-Schale 0 Fehler.

## Papiere

Statusdatei (Nach #650 (c), Nach #548 (f) (1) und (2)), Register (EZ‑24, E30‑Q12, Familien, Kopf), Konzept (§ 3.4, § 6.1,
Kopf), Übergabepapier, `Referenzlaeufe/LIESMICH.md`, Wiki-Quelle `Programm Dokumentation - Kosten.wiki`
(Empfehlungsbereich, Satzfeld), Logbuch 1.2.0.6 (ein Satz).

## Abweichungen und Lesart

- Die Obergrenze der umgerechneten Kesselspanne (2,13 %) liegt über 2 %; die Wache prüft Untergrenze und Mitte und lässt der
  Obergrenze die Rundung.
- § 6.3 des Konzepts führt keinen Punkt zur Empfehlung und bleibt unverändert.
- Punkt 4 (fremder Befund) war nach dem Abgleich mit origin noch rot und ist deshalb mitgenommen.

## Offen

- Wiki-Upload der Quelle Kosten mit dem nächsten Sammel-Upload; Logbuch-Version beim Anwender.
- E30‑Q5 (Emission und Stromsteuer des Hilfsstroms) und N11 bleiben benannt.

## Gate

offen — volles Gate durch die Orchestrierung.

## Commit

offen — die Commits auf `p671` nennt die Orchestrierung mit dem Merge.

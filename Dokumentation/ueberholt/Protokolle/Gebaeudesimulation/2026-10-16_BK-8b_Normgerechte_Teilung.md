# Protokoll BK-8b — Normgerechte Teilung inhomogener Aufbauten

Auftrag BK-8b (Anwenderentscheid 16.10.2026 „Normgerecht teilen“) zum
[Entwurf Bauteilkatalog](../../../aktuell/Gebaeudesimulation/2026-10-12_Entwurf_Bauteilkatalog_Hottgenroth.md) (Abschnitt 5.3)
und zu den [Rechenschritten 3.1](../../../aktuell/Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md). Arbeitsort Worktree
`bk8`, Zweig `bk8`, Basis `dc81b2588`, davor Merge von `bk7b` (`0ef0123f2`). Kein Schemaschritt; der Rechenweg der Simulation
bleibt unverändert.

## Merge bk7b

Ein Konflikt in `EPOS.UI.Daten/Bedarf/BauteilkatalogImportHuelle.cs` (Aufruf von `BauteilkatalogVorschauDaten`): Die
Teilkatalog-Zeile von BK-7b (acht Zahlen samt Zuordnungsdatei) und der Inhomogen-Hinweis von BK-8 sind beide übernommen. Die
`.resx`-Dateien und der Designer liefen ohne Konflikt zusammen; der Designer ist danach unverändert wiederholbar. Build 0 Fehler,
78 Bauteilkatalog-Tests grün. Commit `89dcbabe3`.

## Befund

- Nach BK-8 teilte der Import Schichten an gemeinsamen Grenzen nur bei verschiedener Schichtzahl. Bei gleicher Schichtzahl mit
  versetzten Grenzen stand Schicht k neben Schicht k — eine Näherung, die DIN EN ISO 6946, 6.7.2.3 nicht trägt: Für den unteren
  Grenzwert ist jede Ebene parallel zur Oberfläche isotherm, eine Schicht wird an den Grenzen aller übrigen Abschnitte geteilt.
  Beispiel der Tests (Abschnitt 1 f 0,8: 100 mm λ 0,04 + 100 mm λ 0,035; Abschnitt 2 f 0,2: 50 mm λ 0,5 + 150 mm λ 0,13): R''_T
  nach der Näherung 2,807 m²K/W, normgerecht 3,263 m²K/W; U ändert sich dadurch um 6,7 %.
- Die Fehlerschranke e = (R'_T − R''_T)/(2 R_T) nach 6.7.2.5 rechnete der Kern nicht; das Verhältnis R'_T/R''_T wurde weder
  geprüft noch gemeldet.
- Die Testdatenbank führt keinen Aufbau mit Abschnitten (`Tab_Bauteilabschnitt_STAMM` und `Tab_Bauteilabschnitt` leer). Kein
  Referenzprojekt rechnet einen inhomogenen Aufbau; die Basis kann sich nicht ändern.
- Die Bauteile eines Projekts verweisen auf Projektkopien (`Tab_Bauteil.ID_Aufbau` → `Tab_Bauteilaufbau`), nicht auf den
  Katalog. Der Wiederimport schreibt allein die Katalogtabellen.

## Entscheide

- **Immer teilen, außer die Grenzen liegen übereinander** (6.7.2.3). `BauteilkatalogImport.Angleichen` läuft für jeden Aufbau
  mit mehr als einem Abschnitt. Grenzen verschiedener Abschnitte, die in der genormten Lage höchstens die BK-8-Dickentoleranz
  max(1 mm; 1 % der größten Gesamtdicke) auseinanderliegen, sind eine gemeinsame Grenze an ihrem Mittel; zwei Grenzen desselben
  Abschnitts bleiben getrennt (eine dünne Folie bleibt eine Schicht). Bilden bei gleicher Schichtzahl alle Grenzen solche
  gemeinsamen Grenzen, bleibt der Aufbau unverändert — keine künstlichen Teilschichten. Die Zusammenfassung naher Grenzen gilt
  auch bei verschiedener Schichtzahl und vermeidet dort Splitterschichten unter der Toleranz; jeder Abschnitt behält Dicke und
  Widerstand, R'_T bleibt gleich.
- **BK-8-Regeln unverändert:** Ruhende Luftschicht wird nicht geteilt (sonst verworfen); Gesamtdicken außerhalb der Toleranz
  werden verworfen, sobald geteilt werden muss — nun auch bei gleicher Schichtzahl mit versetzten Grenzen (die Norm setzt
  Abschnitte gleicher Dicke voraus); eine Schicht ohne Dicke neben verschiedener Schichtzahl wird verworfen. Bei gleicher
  Schichtzahl mit einer Schicht ohne Dicke ist die Lage der Grenzen unbestimmt; der Aufbau bleibt wie unter BK-8.
- **Fehlerschranke** (6.7.2.5): `Abschnittsgrenzen.Fehlerschranke` rechnet e an der Stelle, an der der Kern R'_T und R''_T bildet
  (`InhomogeneSchichten.Rechnen`, B0c); `BauteilaufbauKennwerte` führt `Fehlerschranke` und `Hinweis`, der Summenfuß des
  Aufbaueditors zeigt e und den Hinweis. R_T bleibt das Mittel; die Simulation (`GebaeudeZonenabbildung`) nimmt nur die
  Ersatzschichten und ist unberührt.
- **Warnen ja, ablehnen nein.** Die Norm gibt in 6.7.2.5 zum Verhältnis R'_T/R''_T = 1,5 die größte Abweichung 20 % an und nennt
  dieses Verhältnis als Grenze, bis zu der das vereinfachte Verfahren anwendbar ist; darüber verweist sie auf eine numerische
  Rechnung (DIN EN ISO 10211). Ein Ablehnen würde Aufbauten mit Metall- oder Betonstreifen aus dem Katalog drängen, für die der
  Anwender keinen besseren Wert hat; der Hinweis über `FEHLERSCHRANKE_GRENZE` = 0,20 macht die Ungenauigkeit sichtbar, ohne den
  Weg zu sperren. Die Grenze ist gegen den Normtext beim Anwender nachzuprüfen (Wortlaut 6.7.1/6.7.2.5 der gültigen Ausgabe).
- **Wiederimport-Vergleich nur in der Vorschau.** `BauteilkatalogImport.WiederimportVergleichen` vergleicht jeden geteilten
  Aufbau mit Aktion `AKTUALISIEREN` (Importsatz schon im Katalog) mit dem gespeicherten Katalogsatz; nur wer andere Schichten
  bekäme, zählt (`WiederimportGeteilt`), dazu die mit mehr als 1 % U-Änderung (`WiederimportUGeaendert`) und der größte Betrag
  in % (`WiederimportUMaxProzent`), ohne Namen. Die Vorschau liest dafür die Katalogsätze nach dem Planen; die Übernahme
  rechnet den Vergleich nicht. Was die Übernahme tut, folgt der bestehenden Wiederimport-Regel: `AKTUALISIEREN` ersetzt
  Schichten und Abschnitte, `ERGAENZEN` (Satz nicht aus einem Import) füllt nur leere Felder und behält seine Schichten.
- **Hand-U bleibt unberührt.** Ein Bauteil mit `U_Herkunft` `MANUELL` (MA-6b) behält Aufbau, U-Wert und Herkunft; seine
  Projektkopie behält ihre Schichten — der Import schreibt nur den Katalog (Test).
- Texte: `BTA_SUMME_FEHLERSCHRANKE`, `BTA_HINWEIS_FEHLERSCHRANKE`, `BKI_WIEDERIMPORT` neu, `BKI_INHOMOGEN` nennt beide Teilungsfälle;
  beide Sprachen, Designer neu geschrieben.

## Tests

`EPOS.Kern.Tests/BauteilkatalogInhomogenTests` (13 Fälle, davon 6 neu, einer ersetzt):

- gleiche Schichtzahl mit versetzten Grenzen gegen die Handrechnung nach 6.7: R'_T 3,5063, R''_T 3,2627, R_T, U, e 3,60 %, kein
  Hinweis; Dicke und Widerstand je Abschnitt wie in der Quelle (R'_T je Abschnitt gleich);
- Grenzen genau und innerhalb der Toleranz übereinander: unverändert, keine Teilschicht; versetzt bei Dicken außerhalb der
  Toleranz: benannt verworfen;
- Grenzen dicht beieinander: keine Splitterschicht;
- Fehlerschranke über 20 % (Dämmung neben gut leitendem Streifen): Hinweis im Wortlaut, U weiter aus dem Mittel; Grenze 1,5 ↔ 20 %;
  homogener Aufbau ohne Fehlerschranke;
- Vorschau-Zählung gegen einen Katalogsatz nach der Näherung: 1 geteilt, 1 über 1 %, größte Änderung 6,73 %; gleiche Schichten
  zählen nicht; Hüllentext ohne Namen;
- Wiederimport auf der Testdatenbank: Vorschau zählt, Übernahme teilt den Katalogsatz neu (4 → 6 Schichten), Bauteil mit Hand-U
  und seine Projektkopie unverändert.

## Abnahme

- `df -h /home/user` vor der Abnahme: 15 GB frei.
- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler. Windows-Schale (`EnableWindowsTargeting=true`, Debug x64): 0 Fehler.
- `EPOS.Kern.Tests` mit Filter (Bauteil, Abschnitt, Inhomogen, Uwert, Modellpruefung, ReferenzprojektWache,
  DokumentationLinkWache, WikiProduktdatenWache, RepositoryOrdnungWache, KiMasken): 750 bestanden, 1 übersprungen, 0 rot.
- `EPOS.UI.Tests` mit Filter Bauteil: 108 bestanden, 0 rot.
- SQL-Dialekt-Prüfer: 3009 SQL-Texte, 0 Fundstellen (keine neue SQL-Anweisung im Fachweg; die Tests schreiben über `?`-Parameter).
- Referenzlauf der achtzehn CI-Projekte gegen `2026-10-14_R55_KaeltebedarfDezentral` (ohne
  `Kaelte.DeckungsgradGesamtProzent`, `Kaelte.DeckungsgradKaeltefolgeProzent`): GESAMT PASS, 6 398 669 Werte innerhalb der
  Toleranz. Keine Basis eingefroren.

## Logbuch (Vorschlag)

„Der Bauteilkatalog-Import teilt inhomogene Aufbauten an allen gemeinsamen Schichtgrenzen nach DIN EN ISO 6946, und der
Summenfuß nennt die Fehlerschranke des U-Werts.“

## Offen

- Wortlaut der Anwendungsgrenze (Verhältnis 1,5) gegen die beim Anwender gültige Normausgabe prüfen.
- Der Wiederimport-Vergleich steht nur in der Vorschau; ein Echtlauf auf dem Katalog des Anwenders nennt die Zahlen.

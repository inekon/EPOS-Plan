# E13 — Checkliste Punkt 9, Fehlergründe in der Oberfläche, A8-Halbsatz, zwei Hilfe-Anker (Protokoll, 24.09.2026)

Statuszeile #474 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); kleine Bauwelle nach den
Anwenderentscheiden vom 24.09.2026 zu den Fragen aus E7c3 und E9b
([Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md),
R‑E9b: E9b‑Q5 Lesart b; R‑E7c3: E7c3‑Q6 Lesart a; R‑A: A8), erweitert um den Halbsatz aus A8 zur Speichervariante und
um zwei der vier offenen Anker-Kandidaten aus E12 (Statusdatei, Nach #470 (c)). Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 2.11.2 (V‑G12), § 3.9, § 2.13 (3) und § 6.5; Analysepapier
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
§ 5; Mockup `../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`, Anhangzeile U43 und Ressourcentafel der
Kategorie 8. Anlass: der Anwender am 24.09.2026, 17:20, „Freigabe für: kleine Bauwelle für E9b‑Q5 (b) und E7c3‑Q6 (a)".
Vorgänger: [`E10_Nutzungsdauer_S3_Speicherflotte_Protokoll.md`](E10_Nutzungsdauer_S3_Speicherflotte_Protokoll.md). Zweig
`e13` von `3ff9840b` (`origin` nach dem Anwender-Merge der Anlagenkopplung AK1, Schemastand 123, Basis
`2026-09-24_R14_Kaelteerzeuger`), Opus 5.5 im Worktree `.claude/worktrees/e13`, zwei Phasen: `58e27722` (E13/1),
`f674e839` (E13/2), `a4a38a72` (E13/4), `fa7dfd37` (E13/5) in Phase 1, `ba78f7d2` (E13/6) in Phase 2; eine Nummer E13/3
gibt es nicht (Punkt 3 des Auftrags ist der Nachweis). Erster Merge `4b50b77b` über `origin` = `3ff9840b` (der Baum gleicht
`ba78f7d2`); End-Merge `c71addf5` über `origin` = `526c7951` (Zapfprofil Z4 #464, Schemaschritt 124,
Testdatenbank LFS `1d971b1a`). **Kein Schemaschritt, keine Rechenwirkung, keine neue Basis.**

## Gebaut

- **E13/1 — Punkt 9 der Anhang-E-Checkliste (E9b‑Q5, Lesart b):** `AnhangECheckliste` führt die Szenarioanalyse als
  „erfüllt", sobald Ungünstig und Günstig eine Zahl tragen — auch ohne gepflegten Parameter; „teilweise" bei nur
  Erwartet oder nur einem Szenario (neues Merkmal `ChecklistenLage.EinSzenarioGerechnet`; der Bericht liest es aus der
  Bandbreite, die Seite aus der Bandbreitentafel über `AnhangEChecklisteKnopf.EinSzenario`); „offen" ohne Lauf. Der
  Ausweis „n von m Parametern szenariert" bleibt als Beleg im Punkt; Punkt 10 bleibt unverändert. Tests:
  `AnhangEChecklisteTests` (neu `Punkt_9_ist_offen_teilweise_oder_erfuellt` und
  `Das_Blatt_der_Mappe_zeigt_Punkt_9_mit_demselben_Stand`; angepasst die volle Lage, „ohne Szenarien = teilweise" und die
  Lage mit `EinSzenarioGerechnet`), `SzenarioAbdeckungTests`, bUnit `AnhangEChecklisteKnopfTests` (—/— offen, ein
  Szenario teilweise, beide erfüllt).
- **E13/2 — Ladefehler, Speicherfehler und Vorsorgewarnung in der Oberfläche (E7c3‑Q6, Lesart a):** Im Kern
  `Fehlergrund.Anzeigezeilen(lade, speicher, vorsorge)` — je Grund eine Zeile (Laden, Speichern, Vorsorge), derselbe
  Grund aus zwei Quellen ist eine Zeile, der Text ist der von `Fehlergrund.Text` ohne Stapel, ohne ⚠ und ohne
  „neu berechnen"; `StelleTabellenSicher` setzt `Vorsorgewarnung` zu Beginn zurück. **Statuszeile der Ergebnisseite**
  (`WirtschaftlichkeitSeiteGaben`): beim Laden der `Ladefehler` von `LadeErgebnisse`/`LadeSensitivitaet` (ein werfender
  Ladeweg bringt seinen Fehlergrund) samt `Vorsorgewarnung`; ein Speicherfehler der Referenzwahl erscheint einmal beim
  folgenden Laden; neue Gabe und Seitenparameter `Speicherfehlerzeile` für das Schreiben der nicht monetären Wirkungen.
  **BHKW-Dialog:** Die Hülle liefert `Ladefehler`, `Speicherfehler` (einmal gelesen) und `Vorsorgewarnung`; Lade- und
  Vorsorgegrund stehen als Warnband (es entfällt, wenn eine Kohärenzzeile denselben Grund nennt), ein Speicherfehler
  hängt im OK-Weg an der Fehlermeldung; ein werfender Ladeweg wird nicht mehr still gefangen. Tests: `RobustheitB6Tests`
  +4, bUnit `BhkwWirtschaftlichkeitDialogTests` +5, `WirtschaftlichkeitSeiteTests` +2. Maskenwache unverändert — reine
  Anzeigen, keine Eingabestelle.
- **E13/4 — Halbsatz aus A8:** `StromspeicherVarianteCtrl.NutzungsdauerVorgabe()` liest die Standardzeile
  „Stromspeicher · Batterie" der Nutzungsdauertabelle (Muster E10); ohne Tabelle, Zeile oder brauchbaren Wert (< 1 a)
  gilt die Konstante 20. `NeueVariante()` nimmt den Wert; vorbelegt werden nur neue Einträge — die neue Speicheranlage
  im Anlagendialog (`WizardCtrl`) und „Speichervariante anlegen" des Hilfe-Assistenten. Unverändert bleiben bestehende
  Zeilen, der Rückfall des Rechenwegs (20), `AktiveVarianteSicherstellen`, die Simulationshülle und die
  Komponentenübernahme. Test `SpeichervarianteSicherstellenTests.Eine_neue_Variante_nimmt_die_Nutzungsdauer_der_Tabelle`
  (10 → neu 10; Tabelle 12,5 → Bestand bleibt 10; ohne Wert 20; Rückfall 20).
- **E13/5 — zwei Hilfe-Anker (`help_mapping.txt`):** `Form_VorlagenPosition` → `Kosten#ersatz-restwert-kennzeichen`
  (der Zeileneditor hinter dem Stift), `Form_LeistungspreisReihe` → `Kosten#preiswirkung` (die Saisonreihe mit zwölf
  Monatssätzen). `Form_SpotpreisImport` und `Form_Kostenprofil` bleiben auf der Seitenebene: Die Seite Kosten führt zu
  keinem der beiden Dialoge einen eigenen Abschnitt, und `preishistorie` trifft den Spotpreis-Import nicht. Die
  Kandidaten aus Nach #470 (c) waren `nutzungsdauer-standard` und `aufschlaege`; gesetzt sind die Abschnitte, die den
  Dialog beschreiben. `HelpMappingAnkerWacheTests` grün.
- **E13/6 — Befund und Nachbau in Phase 2:** `DataRepository` wirft beim Schreiben nie, meldet einen Datenbankfehler
  selbst (`FehlerMelden`, Dialog) und liefert −1. Damit war `WirtschaftlichkeitCtrl.Speicherfehler` bei
  Datenbankfehlern immer leer, und `SpeichereParameter`/`SpeichereTarif` versuchten nach dem gescheiterten UPDATE ein
  INSERT — ein zweiter, falscher Grund („UNIQUE constraint failed"). Behoben: bei rows < 0 „nicht gespeichert" ohne
  INSERT; `Speicherfehler` trägt nur Fehler außerhalb der Datenbankanweisung, den Datenbankfehler meldet die Anwendung
  selbst, einmal. Tests: `Ein_Datenbankfehler_der_Referenzwahl_erscheint_einmal` (Dialogmitschrift, keine
  UNIQUE-Meldung, die Statuszeile wiederholt nicht) und der BHKW-Gabentest entsprechend, dazu der `Speichergrund`-Träger
  der Hülle. Das Risiko 7 aus Phase 1 (leere `Vorsorgewarnung` auf der Testdatenbank) ist geprüft: leer, der Test
  bestand.

## Schlüssel

Je Sprache 9.049 → 9.054 Einträge. **Neu (5):** `WIRT_AE_9_ERFUELLT` („drei vollständige Läufe, je Szenario mit eigenem
Parametersatz."), `WIRT_AE_9_TEILWEISE_ABDECKUNG`, `WIRT_STATUS_LADEFEHLER` („Gespeicherte Ergebnisse nicht vollständig
gelesen: {0}"), `WIRT_STATUS_SPEICHERFEHLER` („Speichern gescheitert: {0}"), `WIRT_STATUS_VORSORGE` („Tabellenvorsorge
unvollständig: {0}"). **Neu gefasst (2):** `WIRT_AE_9_TEILWEISE` („nur Erwartet oder nur eines der Szenarien Günstig und
Ungünstig mit Kapitalwert gerechnet."; der alte Text wanderte nach `…_ERFUELLT`) und `WIRT_AE_9_OFFEN` („keine Szenarien
gerechnet — erst berechnen."). Designer wiederholbar.

## Abweichungen und Befunde

1. Der Satz „Betrachtungszeitraum und Mengen bleiben unverändert" stand seit E9b in keinem Text mehr; ein Test sichert,
   dass er nicht wiederkommt.
2. „Einmal" heißt je Anzeigestelle einmal: Auf der Seite kann derselbe Ladegrund zusätzlich in der Kohärenzzeile der
   Tabelle stehen.
3. `StelleTabellenSicher` setzt die `Vorsorgewarnung` zu Beginn zurück — ohne Rechenwirkung, der Wert steuert keinen
   Rechenweg.
4. Die BHKW-Hülle nennt auch den Fehler aus `KwkgAnlagenCtrl.Speichere`.
5. **Befund iOS-Weg:** Der Weg über die Projektliste (`AppWurzel` → `BhkwDialogDaten` → `IosProjektQuelle`) reicht die
   neuen Gründe nicht an den BHKW-Dialog durch — hier nicht baubar; der Weg über die Wirtschaftlichkeitsseite zeigt sie
   auch auf iOS. Ein eigener kleiner Auftrag.
6. Punkt 4 übernimmt den Tabellenwert ungerundet.
7. E13/6 (oben) ändert Abnahme-Schritt 3: Ein Datenbankfehler erscheint einmal als Meldung der Anwendung, nicht als
   zweite Zeile in Statuszeile oder Dialog.

## Nachweis

- **Keine Rechenwirkung:** `WirtschaftlichkeitAnkerTests` unverändert grün, kein Anker neu gesetzt; Referenzlauf 13/13
  gegen `2026-09-24_R14_Kaelteerzeuger` PASS, 394/394 CSV byte-gleich, 4.207.049 Werte; SQL-Prüfer 1.780 Texte,
  0 Fundstellen; Maskenwache und `HelpMappingAnkerWacheTests` grün.
- **Phase 1** (Worktree `e13`): Kern-Filter und Windows-Schale 0 Fehler, Designer wiederholbar; kein `dotnet test`.
- **Phase 2** (auf `ba78f7d2`): Builds 0 Fehler; gefiltert Kern 188/188 und UI 281/281; voller Lauf `WP-Plan.Kern.slnf`
  **12.714 bestanden / 0 Fehler / 1 übersprungen** (EPOS.Kern.Tests 5.834, EPOS.UI.Tests 5.918, KiKern.Tests 549,
  SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27 und 1 übersprungen).
- **Gate auf `4b50b77b`** (Worktree `pm13`, Log `GATE474.log`): Kern-Filter 0 Fehler; ChartProben 146/146 gleich der
  Windows-Messlatte; voller Lauf 12.714 / 0 / 1 (Kern 5.834, UI 5.918, KiKern 549, SpeicherEngine 386, SpeicherPlanung
  27/1); Dokumentationswachen 26/26.
- **Gate auf dem End-Merge:** Gate auf c2a03f75 (Nachzug Z4): Build 0 Fehler, ChartProben 151/151 gleich der auf die fünf Z4-Bilder erweiterten Windows-Messlatte, voller Lauf 12.909 bestanden / 0 Fehler / 1 übersprungen (EPOS.Kern 5.952, EPOS.UI 5.995, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27+1), Dokumentationswachen 29/29; reduziertes Gate auf dem End-Merge c71addf5 (Nachzug #475): Build 0 Fehler, E13-Testklassen und Wachen Kern 136/136, UI 175/175.
- **CI:** steht aus (Push nach dem Gate).

## Abnahme am Gerät (A‑E13‑1, Windows und iPad)

1. **Projekt 1030 berechnen, „Anhang-E-Checkliste…":** Punkt 9 „erfüllt: drei vollständige Läufe …; n von m Parametern
   szenariert" — ebenso auf der Abschlussseite des Wortberichts und im Blatt „Checkliste Anhang E"; ohne Lauf „offen:
   keine Szenarien gerechnet — erst berechnen."
2. **Datenbankkopie mit `Tab_ErgebnisWirtschaftlichkeit.Zeitstempel = 'kaputt'`:** die Statuszeile einmal „Gespeicherte
   Ergebnisse nicht vollständig gelesen: FormatException: …", der BHKW-Dialog dasselbe als Warnband.
3. **Trigger `RAISE(ABORT)` auf `Tab_ProjektWirtschaftlichkeit`:** BHKW-Dialog „OK" und „nicht monetäre Wirkungen
   speichern" — die Anwendung meldet den Datenbankfehler einmal; Statuszeile und Dialog nennen keinen zweiten Grund
   (kein „UNIQUE constraint failed"); die Zeile „Speichern gescheitert: …" steht nur bei einem Fehler außerhalb der
   Datenbankanweisung.
4. **Anlagendialog, neuer Stromspeicher:** Die Speichervariante trägt die Nutzungsdauer 10 a aus der Tabelle; bestehende
   Varianten bleiben unverändert.
5. **Hilfe im Zeileneditor (Stift)** öffnet „Ersatzbeschaffung und Restwert je Position"; die Hilfe bei den saisonalen
   Leistungspreis-Sätzen öffnet „Was die drei Preise bewirken".

## Logbuch

Im Update-Papier (Version 1.2.0.4, Sammel-Upload 26.09.2026) zwei Sätze und der Satz zu `kosten`:

- *bericht:* „Die Anhang-E-Checkliste führt die Szenarioanalyse als erfüllt, sobald die Szenarien Günstig und Ungünstig
  gerechnet sind."
- *wirtschaftlichkeit:* „Die Ergebnisseite und der Dialog BHKW-Wirtschaftlichkeit nennen den Grund, wenn gespeicherte
  Ergebnisse nicht gelesen oder Eingaben nicht gespeichert werden konnten."
- *kosten:* „Eine neu angelegte Speichervariante übernimmt die Nutzungsdauer der Zeile Stromspeicher · Batterie aus der
  Nutzungsdauertabelle."

Die zwei Hilfe-Anker bekommen keinen Satz (Kleinigkeit, Regel 13.4).

## Papiere mit der Statuszeile

Register (Kopf, Familientafel, A8, R‑V zu V‑G12, R‑E7c3 mit E7c3‑Q6, R‑E9b mit E9b‑Q5, EZ‑9, EZ‑10), Konzept (Kopf,
§ 2.11.2 V‑G12, § 2.11.5 Ausweis, § 2.13 (3), § 3.9, § 6.1, § 6.2, § 6.3 Nr. 9h, § 6.5, § 7 und Anhang), Analysepapier
(Kopf, Nachtrag, § 3.1 R10, § 4 A8, § 5 mit der Zeile E13 und dem Stand der Etappen), Protokoll der Entscheidwege (Kopf,
§ 0.5, Kopf von § 8, § 8.25, § 8.26), Nutzungsdauer-Konzept nach `ueberholt/` verschoben (Kopf, § 3, § 6, Verweise,
Indexzeile), Mockup (U43, Ressourcentafel der Kategorie 8, Stand-Absatz), Update-Papier und die Wiki-Quellen
Wirtschaftlichkeit und Kosten, Index (Reporting 127 → 128).

## Offen

- **Abnahme am Gerät** A‑E13‑1 (fünf Schritte oben).
- **iOS-Weg der BHKW-Gründe** über die Projektliste (Befund 5) — eigener kleiner Auftrag.
- **Datenpflege 1030/1026** (aus E10) und der **Wiki-Sammel-Upload** am 26.09.2026 (Version 1.2.0.4, freigegeben).

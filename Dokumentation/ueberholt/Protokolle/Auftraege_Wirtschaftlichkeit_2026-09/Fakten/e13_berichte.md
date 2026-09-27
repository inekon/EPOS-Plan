# E13 — Bericht Phase 1 (Opus, 24.09.2026, Worktree e13 ab 3ff9840b)

Commits: 58e27722 E13/1 Checkliste Punkt 9 erfüllt mit beiden Szenarien (E9b‑Q5 b); f674e839 E13/2 Lade-, Speicherfehler und
Vorsorgewarnung sichtbar (E7c3‑Q6 a); a4a38a72 E13/4 Neue Speichervariante nimmt Nutzungsdauer der Tabelle (A8); fa7dfd37 E13/5
help_mapping — zwei Kostendialoge auf ihren Abschnitt. Builds grün (Kern-Filter 0, Windows-Schale 0), Designer wiederholbar.

## Punkt 1 — E9b‑Q5 (b)
`AnhangECheckliste`: Punkt 9 „erfüllt", sobald Ungünstig und Günstig eine Zahl tragen (auch ohne gepflegten Parameter); „teilweise" bei nur
Erwartet oder nur einem Szenario (neues Merkmal `ChecklistenLage.EinSzenarioGerechnet`; Bericht liest es aus der Bandbreite, Seite aus der
Bandbreitentafel `AnhangEChecklisteKnopf.EinSzenario`); „offen" ohne Lauf. Ausweis „n von m Parametern szenariert" bleibt Beleg. Punkt 10
unverändert. Tests: `AnhangEChecklisteTests` (neu `Punkt_9_ist_offen_teilweise_oder_erfuellt`, `Das_Blatt_der_Mappe_zeigt_Punkt_9_mit_
demselben_Stand`; angepasst volle Lage, ohne Szenarien = teilweise, Lage mit `EinSzenarioGerechnet`), `SzenarioAbdeckungTests`, bUnit
`AnhangEChecklisteKnopfTests` (—/— offen, ein Szenario teilweise, beide erfüllt).

## Punkt 2 — E7c3‑Q6 (a)
Kern: `Fehlergrund.Anzeigezeilen(lade, speicher, vorsorge)` — je Grund eine Zeile (Laden, Speichern, Vorsorge), gleicher Grund aus zwei
Quellen = eine Zeile, Text von `Fehlergrund.Text` ohne Stapel, kein ⚠ (kein „neu berechnen"-Hinweis); `StelleTabellenSicher` setzt
`Vorsorgewarnung` zu Beginn zurück. Statuszeile der Ergebnisseite (`WirtschaftlichkeitSeiteGaben`): `Laden` nennt den `Ladefehler` von
`LadeErgebnisse`/`LadeSensitivitaet` (werfender Ladeweg → dessen Fehlergrund) plus `Vorsorgewarnung`; Speicherfehler der Referenzwahl
einmal beim folgenden Laden; neue Gabe/Seitenparameter `Speicherfehlerzeile` (Schreiben der nicht monetären Wirkungen). BHKW-Dialog: Hülle
liefert `Ladefehler`, `Speicherfehler` (einmal gelesen), `Vorsorgewarnung`; Lade-/Vorsorgegrund als Warnband (entfällt, wenn eine
Kohärenzzeile denselben Grund nennt); Speicherfehler im OK-Weg an die Fehlermeldung angehängt; werfender Ladeweg nicht mehr still gefangen.
Tests: `RobustheitB6Tests` +4, bUnit `BhkwWirtschaftlichkeitDialogTests` +5, `WirtschaftlichkeitSeiteTests` +2. Maskenwache: keine neue
Eingabestelle.

## Punkt 4 — A8-Halbsatz
`StromspeicherVarianteCtrl.NutzungsdauerVorgabe()` liest die Standardzeile „Stromspeicher · Batterie" (Muster E10); ohne Tabelle/Zeile/
brauchbaren Wert (< 1 a) Konstante 20; `NeueVariante()` nutzt den Wert. Vorbelegt nur neue Einträge: neue Speicheranlage im Anlagendialog
(`WizardCtrl`) und „Speichervariante anlegen" des KI-Assistenten. Unverändert: bestehende Zeilen, Rückfall des Rechenwegs (20),
`AktiveVarianteSicherstellen`, Simulationshülle, Komponentenübernahme. Test `SpeichervarianteSicherstellenTests.Eine_neue_Variante_nimmt_
die_Nutzungsdauer_der_Tabelle` (10 → neu 10; Tabelle 12,5 → Bestand bleibt 10; ohne Wert 20; Rückfall 20).

## Punkt 5 — help_mapping
`Form_VorlagenPosition` → `Kosten#ersatz-restwert-kennzeichen` (Zeileneditor hinter dem Stift); `Form_LeistungspreisReihe` →
`Kosten#preiswirkung` (Saisonreihe zwölf Monatssätze); `Form_SpotpreisImport` und `Form_Kostenprofil` bleiben Seitenebene (kein
Abschnitt zum Dialog; `preishistorie` trifft nicht).

## Schlüssel
Neu: `WIRT_AE_9_ERFUELLT`, `WIRT_AE_9_TEILWEISE_ABDECKUNG`, `WIRT_STATUS_LADEFEHLER`, `WIRT_STATUS_SPEICHERFEHLER`, `WIRT_STATUS_VORSORGE`.
Neu gefasst: `WIRT_AE_9_TEILWEISE` („nur Erwartet oder nur eines der Szenarien …"; alter Text nach `…_ERFUELLT`), `WIRT_AE_9_OFFEN`
(„keine Szenarien gerechnet — erst berechnen.").

## Abweichungen
1. „Betrachtungszeitraum und Mengen bleiben unverändert" stand seit E9b nicht mehr in den Texten; Test sichert es. 2. „Einmal" = je
Anzeigestelle einmal; auf der Seite kann derselbe Ladegrund zusätzlich in der Kohärenzzeile der Tabelle stehen. 3. Rücksetzen der
`Vorsorgewarnung` in `StelleTabellenSicher` (ohne Rechenwirkung, Wert wird nirgends gelesen). 4. BHKW-Hülle nennt auch den Fehler aus
`KwkgAnlagenCtrl.Speichere`. 5. **Befund:** der iOS-Weg über die Projektliste (`AppWurzel`/`BhkwDialogDaten`/`IosProjektQuelle`) reicht
die neuen Gründe nicht durch (nicht baubar hier); der Weg über die Wirtschaftlichkeitsseite zeigt sie auch auf iOS. 6. Punkt 4 übernimmt
den Tabellenwert ungerundet. 7. Risiko Phase 2: BHKW-Gabentest erwartet leere `Vorsorgewarnung` auf der Testdatenbank.

## Erledigt-Gründe
Register E9b‑Q5 gebaut b (#474, E13/1); E7c3‑Q6 gebaut a (#474, E13/2); A8-Halbsatz erledigt (#474, E13/4, nur Vorgabe neuer Einträge);
E12 offene Anker: zwei gesetzt, zwei begründet Seitenebene (E13/5); Konzept § 2.11.2 V‑G12 Checkliste Punkt 9; Mockup U43 Punkt 9
„erfüllt"; Mockup U10 Statuszeile nennt Lade-/Speicher-/Vorsorgegründe (Inhalt von U10 gegenprüfen).

## Logbuchsätze
`bericht`: „Die Anhang-E-Checkliste führt die Szenarioanalyse als erfüllt, sobald die Szenarien Günstig und Ungünstig gerechnet sind."
`wirtschaftlichkeit`: „Die Ergebnisseite und der Dialog BHKW-Wirtschaftlichkeit nennen den Grund, wenn gespeicherte Ergebnisse nicht
gelesen oder Eingaben nicht gespeichert werden konnten." Optional `kosten`: „Eine neu angelegte Speichervariante übernimmt die
Nutzungsdauer der Zeile Stromspeicher · Batterie aus der Nutzungsdauertabelle."

## Abnahme A‑E13‑1 (Windows und iPad)
1. Projekt 1030 berechnen, „Anhang-E-Checkliste…": Punkt 9 „erfüllt: drei vollständige Läufe …; n von m Parametern szenariert" — auch
Abschlussseite des Wortberichts und Blatt „Checkliste Anhang E"; ohne Lauf „offen: keine Szenarien gerechnet — erst berechnen." 2. Auf
einer Datenbankkopie `Tab_ErgebnisWirtschaftlichkeit.Zeitstempel = 'kaputt'`: Statuszeile einmal „Gespeicherte Ergebnisse nicht
vollständig gelesen: FormatException: …", BHKW-Dialog dasselbe als Warnband. 3. Trigger `RAISE(ABORT)` auf `Tab_ProjektWirtschaftlichkeit`:
BHKW-Dialog OK → „1 Angabe(n) konnten nicht gespeichert werden. Speichern gescheitert: SqliteException: …"; nicht monetäre Wirkungen
speichern → Band und Statuszeile nennen den Grund. 4. Anlagendialog: neuer Stromspeicher → Speichervariante Nutzungsdauer 10 a, Bestand
unverändert. 5. Hilfe im Zeileneditor (Stift) → „Ersatzbeschaffung und Restwert je Position"; Hilfe bei den saisonalen Leistungspreis-
Sätzen → „Was die drei Preise bewirken".

# E13 — Bericht Phase 2 (Opus, 24.09.2026)

Kein Nachzug (origin blieb 3ff9840b). **E13/6 ba78f7d2 (Befund und Nachbau):** `DataRepository` wirft beim Schreiben nie, meldet den
Datenbankfehler selbst (`FehlerMelden`, Dialog) und liefert −1 → `WirtschaftlichkeitCtrl.Speicherfehler` war bei Datenbankfehlern immer
null; `SpeichereParameter`/`SpeichereTarif` versuchten nach gescheitertem UPDATE ein INSERT (zweiter falscher Grund „UNIQUE constraint
failed"). Behebung: bei rows < 0 „nicht gespeichert" ohne INSERT; `Speicherfehler` trägt nur Fehler außerhalb der Zugriffsschicht;
Datenbankfehler meldet die Anwendung selbst, einmal. Tests: `Ein_Datenbankfehler_der_Referenzwahl_erscheint_einmal` (Dialogmitschrift,
keine UNIQUE-Meldung, Statuszeile wiederholt nicht), BHKW-Gabentest analog + `Speichergrund`-Träger der Hülle. Abnahme Schritt 3 angepasst:
Trigger → Datenbankmeldung einmal, Bänder, kein zweiter Grund in Statuszeile/Dialog. Risiko 7: Vorsorgewarnung leer, Test bestand.
Builds 0 Fehler; gefiltert Kern 188/188, UI 281/281; voller Lauf 12.714/0/1 (Kern 5.834, UI 5.918, KiKern 549, SpeicherEngine 386,
SpeicherPlanung 27/1); Anker unverändert; Maskenwache und HelpMappingAnkerWache grün; Referenzlauf 13/13 gegen R14_Kaelteerzeuger PASS,
4.207.049 Werte, 394/394 CSV byte-gleich; SQL-Prüfer 1.780/0; Designer wiederholbar.

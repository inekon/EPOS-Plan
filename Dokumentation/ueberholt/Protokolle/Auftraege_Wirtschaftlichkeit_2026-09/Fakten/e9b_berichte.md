

===== Bericht 1 (2026-09-24T03:24:32.507Z) =====

E9b Phase 1 ist fertig: Alles ist gebaut und in sechs Commits festgehalten. Die Tests sind geschrieben, aber nicht gelaufen; kein `dotnet test`, kein Referenzlauf, der Stand ist also unbestätigt. Nichts gepusht, nicht gemergt, kein Stash, kein Schemaschritt; Papiere, Mockups und Wiki sind unberührt. Der Arbeitsbaum ist sauber.

## Commits
Zweig `e9b` im Worktree `C:\Waermeplan\EPOS-Plan\.claude\worktrees\e9b`, Basis 0d296ca0.

| Commit | Inhalt |
|---|---|
| `8e3e9357` E9b/1 | Szenariotafel Zeilen 8 (Betrachtungszeitraum) und 9 (Mengenänderung); „Vorgaben" leert alle 18 Felder; KI-Sicht +4 Felder |
| `6a7f738e` E9b/2 | ±-Knopf an den Trägerpreisen; `CaseEingabeDialog` als allgemeiner Baustein; neuer Knopf-Baustein `SzenarioKnopf` |
| `5841cd7c` E9b/3 | ±-Knopf an Einspeisevergütung PV, Einspeisevergütung KWK, DV-Entgelt, PPA-Preis |
| `8ed0f1a5` E9b/4 | Hinweistext `WIRT_SZEN_HINWEIS` entfernt; Ausweis „n von m Parametern szenariert" mit Zählregel im Kern (`SzenarioAbdeckung`) |
| `a4bcb41e` E9b/5 | Zwei Nachbesserungen, siehe Abweichungen 4 und 5 |
| `6d0fef5f` E9b/6 | Tests |

## Bau je Punkt
- **Kern-Filter:** 0 Fehler nach jedem Commit; alle Warnungen stammen aus Dateien, die E9b nicht anfasst.
- **Windows-Schale:** 0 Fehler (13 Warnungen, Bestand).
- **Designer:** neu erzeugt, die Prüfung meldet „unverändert".
- **Neue Tests:** rund 70 Testmethoden.
  - bUnit: `SzenarioKnopfTests` (7), `CaseEingabeSzenariopaarTests` (15), `EnergietraegerSzenarioTests` (10), dazu Ergänzungen in den Tests von Parameterdialog, BHKW- und PV-Dialog.
  - Kern: `SzenarioAbdeckungTests` (15, darunter „Hinweistext weg, Ausweis da" und die Zählung an Projekt 1030) und `EnergietraegerSzenarioHuelleTests` (6, Speichern und Laden in Nm³ und kWh).
  - Die bestehenden `CaseEingabeDialogTests` bleiben unverändert; sie belegen, dass sich die Kostenposition nicht ändert.

## Schlüssel
- **Neu (38, de/en):**
  - Parameterdialog: `WPAR_SZ_ZEITRAUM`, `WPAR_SZ_MENGE`
  - Assistent: `KI_DLG_WPA_SZ_ZEITRAUM_ERL`/`_MENGE_ERL`, `KI_DLG_CSE_GROESSE`/`ERWARTET`/`EINHEIT_NAME`/`_ERL`, `KI_DLG_CSE_NUR_KOSTEN`
  - Knopf-Dialog: 15 × `SZP_*`, dazu `SZP_PV_INAKTIV`, `SZP_PV_DV_OHNE_WIRKUNG`, `SZP_PV_PPA_OHNE_WIRKUNG`
  - Trägerkarte: `ETV_SZ_TITEL`, `_HINWEIS`, `_OHNE_ERWARTET`, `_SPEICHERFEHLER`
  - Ausweis und Bericht: `WIRT_ANN_DV_ENTGELT`, `WIRT_ANN_PPA_PREIS`, `WIRT_SZ_ABDECKUNG`, `WIRT_SZ_ABDECKUNG_LISTE`, `WIRT_AE_9_ABDECKUNG`
- **Entfallen:** `WIRT_SZEN_HINWEIS`.
- **Wert geändert:**
  - `WPAR_SZ_HINWEIS`: nennt jetzt Zeilen 8/9 und dass „Vorgaben" 18 Felder leert.
  - `WIRT_AE_9_TEILWEISE`: Der alte Satz „Betrachtungszeitraum und Mengen bleiben unverändert" stimmt nicht mehr.

## Maskenwache
- `WirtschaftlichkeitParameterDialog` steigt von 26 auf 30 Eingabestellen, im Block „ETAPPE E9b", mit Einträgen in der KI-Feldliste (`best_`/`worst_zeitraum`, `best_`/`worst_menge`).
- Der Knopf-Dialog (`CaseEingabe`) bekommt drei nur lesbare KI-Felder (Größe, Erwartet-Wert, Einheit); seine Eingabezahl bleibt 7.
- Trägerkarte (21), Energieträgerdialog (4), BHKW-Dialog (39) und PV-Dialog (16) bleiben gleich, denn die ±-Knöpfe sind keine Eingaben.

## Formularkarte
- Neue Hilfekennung `Form_WirtschaftlichkeitSzenariowerte.btn_Help` → `Wirtschaftlichkeit#szenariowerte`; der Anker steht schon im Wiki.
- Das Prüfmuster `Form_CaseEingabe` und sein Nachfolgepfad bleiben unverändert.

## Abweichungen vom Auftrag
1. **Zeile 8 ist ein Ganzzahlfeld**, kein Zahlenfeld: Der Zeitraum ist eine ganze Jahreszahl (1–50 wie das Feld T). Zeile 9 zeigt in der Erwartet-Spalte „0 %".
2. **Der ±-Knopf für die Einspeisevergütung KWK steht im Dialog „BHKW-Wirtschaftlichkeit"**, nicht im Parameterdialog. Dort wird ihr Erwartet-Wert seit #325 gepflegt; der Parameterdialog zeigt kein KWK-Feld mehr. Geschrieben wird erst mit OK des BHKW-Dialogs.
3. **Kein neues Prüfmuster:** Prüfmuster sind eingefrorene WinForms-Vorgänger, und die neue Verwendung hat keinen. Ersatz: ein Test, dass jeder Schlüssel des Knopf-Parametersatzes einen `[Parameter]` trifft, dazu die neue Hilfekennung.
4. **Eigene CSS-Klasse für die Knopfzeile** (E9b/5): `epos-szenarioleiste` statt `epos-leiste`. Mit `epos-leiste` wären die OK/Abbrechen-Zugriffe der BHKW-Tests gekippt, und die Fußleistenwache zählt jede `epos-leiste`.
5. **Assistent im Knopf-Dialog** (E9b/5): Nutzungsdauer, Startjahr und Zuschuss lehnt er im Szenariopaar mit Grund ab (`KI_DLG_CSE_NUR_KOSTEN`), statt einen Wert still zu verwerfen.
6. **Die „Rahmen-Gruppe" aus Konzept § 2.11.5** wird über die Szenariotafel gepflegt (Zeilen 1–4 und 8), nicht über einen eigenen ±-Knopf. So verlangt es der Auftrag; die Papiere sollten das nachziehen.

## Fragen E9b‑Q1…Q4 (gebaut ist jeweils a)
- **Q1 Bauform des ±-Knopfs:**
  - a: verallgemeinerter `CaseEingabeDialog` mit Knopf-Baustein und gemeinsamem Parametersatz.
  - b: eigene Dialoge je Ort.
  - **Empfehlung a:** ein Muster, ein Hilfeschlüssel, Kostenposition unverändert.
- **Q2 Zählregel m:**
  - a: 7 Größen der Tafel, Zeitraum, Menge, Einspeisevergütung PV und KWK; DV-Entgelt und PPA-Preis je Vergütungszeile mit PV-Anlage; je Träger mit Verbrauch Arbeits- und Grundpreis, der Leistungspreis beim Stromträger immer, sonst nur, wo einer gepflegt ist.
  - b: nur die projektweiten Größen (m = 11).
  - **Empfehlung a.**
  - Zwei Lesarten zum Mitentscheiden:
    - n zählt nur gepflegte Werte. Die Vorgaben der 7 Tafelgrößen zählen nicht, obwohl sie Günstig und Ungünstig verschieben. Ohne Pflege steht also „0 von m", während die Szenarien trotzdem voneinander abweichen.
    - Die Einspeisevergütung KWK zählt auch ohne BHKW, wie im Auftrag.
- **Q3 Ort des Ausweises:**
  - a: unter der Annahmentafel (Seite: Annahmen und Block 4), in Wort- und Excelbericht und in Punkt 9 der Anhang‑E-Checkliste.
  - b: Kopf der Ergebnisseite.
  - **Empfehlung a.**
- **Q4 Szenariopreis ohne Erwartet-Preis:**
  - a: Warnzeichen am Knopf, Kohärenzzeile, Warnbanner im Dialog; gespeichert wird trotzdem.
  - b: Speichern verweigern.
  - **Empfehlung a.** Grundpreis und Erlössätze warnen nie, weil 0 dort ein gültiger Wert ist.
- **Folgefrage Q5 (vom Bau berührt):** Punkt 9 der Checkliste steht weiter auf „teilweise".
  - a: bleibt so (gebaut).
  - b: „erfüllt", sobald beide Szenarien gerechnet sind.
  - c: „erfüllt" nur, wenn mindestens ein Parameter gepflegt ist.
  - **Empfehlung b:** Der Grund für „teilweise" war der feste Zeitraum und die festen Mengen, und das löst E9 auf.

## Erledigt-Gründe (unter Vorbehalt Phase 2 und Abnahme A‑E9‑1)
- **V‑G5:** Mit E9a (#461) und E9b tragen alle Parameterklassen aus § 2.11.5 Best/Worst-Werte, gepflegt in den Dialogen. Leer oder 0 heißt „wie Erwartet"; gepflegt ist ein Wert nur mit mehr als 1e−9 Abstand zum Erwartet-Wert.
- **V‑4:** Der Zeitpunkt „danach" ist umgesetzt; der Hinweistext aus #434 ist mit E9b entfallen.
- **U15:** Die Pflege in den Dialogen, der Wegfall des Hinweistexts und der Ausweis sind da.
- **U10:** Entfallen: `WIRT_SZEN_HINWEIS` ist aus Ressourcen, Seite, Wort- und Excelbericht entfernt, der Ausweis steht an seiner Stelle. Der U10-Kommentar in `WirtschaftlichkeitBewertung` ist nachgezogen.
- **§ 2.11.7:** „Der Hinweis entfällt mit der Etappe, die ihn überflüssig macht" – das ist E9b.

## Abnahme am Gerät A‑E9‑1
1. **Parameterdialog, Szenariotafel:** neun Zeilen. Zeitraum und Menge sind leer mit Platzhalter T bzw. 0. Einträge erscheinen in der Herleitungszeile. „Vorgaben" leert 18 Felder und lässt die Einspeisevergütungen stehen.
2. **± Einspeisevergütung PV:** Erwartet-Zeile im Dialog; nach OK Kennzeichen ● und Kurztext mit den Werten. Die Kohärenzzeile erscheint bei aktivem Rollenmodell oder aktivem PV-Vergütungsdialog. Esc schließt nur die Überlagerung.
3. **BHKW-Dialog:** ± Einspeisevergütung KWK; geschrieben wird erst mit OK.
4. **Trägerkarte im Projekt:** ± je Preis. Beim Umschalten auf €/kWh folgen die Szenariowerte. Stromträger mit Staffel: Zeile „ohne Wirkung". Ohne Erwartet-Preis: ⚠, gespeichert wird trotzdem. Im Katalog keine Knöpfe.
5. **PV-Dialog:** ± DV-Entgelt nur bei Marktprämie, ± PPA-Preis nur bei sonstiger Direktvermarktung.
6. **Ergebnisseite:** „n von m Parametern szenariert: …" unter der Annahmentafel und in Block 4; Checkliste Punkt 9 nennt den Ausweis.
7. **Wort- und Excelbericht:** derselbe Satz in der Annahmentafel, kein Hinweistext.
8. **Rechnen:** Ein Projekt mit Pflege zeigt geänderte Kapitalwerte Günstig/Ungünstig bei unverändertem Erwartet; nach dem Leeren wieder die alten Werte.

## Logbuchsätze (Datum 24.09.2026, Versionsnummer beim Anwender erfragen)
- **szenarien (E9a):** „Die Szenarien Günstig und Ungünstig können einen eigenen Betrachtungszeitraum, eine Mengenänderung, eigene Energieträgerpreise sowie eigene Einspeisevergütungen, DV-Entgelte und PPA-Preise führen."
- **wirtschaftlichkeit (E9a):** „Bericht und Formelmappe nennen je Szenario Betrachtungszeitraum, Mengenänderung, Einspeisevergütungen und gepflegte Energieträgerpreise."
- **szenarien:** „Die Szenariotafel im Dialog ‚Parameter' führt zusätzlich Betrachtungszeitraum und Mengenänderung je Szenario."
- **szenarien:** „Arbeits-, Grund- und Leistungspreis der Energieträger, die Einspeisevergütungen sowie DV-Entgelt und PPA-Preis tragen einen ±-Knopf für ihre Werte je Szenario."
- **wirtschaftlichkeit:** „Unter der Annahmentafel, im Wort- und im Excelbericht steht statt des Hinweistexts der Ausweis ‚n von m Parametern szenariert' mit den gepflegten Größen."

## Hinweise für Papiere und Merge
- **Wiki `Programm Dokumentation - Wirtschaftlichkeit.wiki` ist veraltet:**
  - Z. 50 „Ein Eingabefeld dafür führt keiner der Dialoge"
  - Z. 84, Anker `szenariohinweis` (der entfallene Hinweistext)
  - die Beschreibung des Parameterdialogs (noch sieben Zeilen)
  - Trägerkarte, PV- und BHKW-Dialog brauchen die ±-Knöpfe.
- **Merge-Vorschau gegen origin:** `origin/ios_migration_september` ist 12 Commits weiter (#458 Stufe 3a/3b). `git merge-tree` meldet keine Konflikte. Sieben Dateien sind auf beiden Seiten geändert (KI-Feldliste, KI-Texte, beide Ressourcendateien, Designer, zwei KI-Tests); in den Ressourcen gibt es keine doppelten Schlüssel. Nach dem Merge Designer-Prüfung und Maskenwache laufen lassen.

## Phase 2
Wartet auf „Tests freigegeben". Dann: testhost-Prüfung als eigener Aufruf, gefilterte Läufe, voller Lauf mit den xUnit-Schaltern, Referenzlauf 13/13, SQL-Prüfer, Designer, Formularkarte-Prüfmuster, Maskenwache.
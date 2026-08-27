# Fehlerrunde 27.08.2026 — nach Anlage der vier Referenzprojekte

Stand: 27.08.2026 · Branch `Pufferspeicher` · Anlass: Anwendermeldungen beim Anlegen der vier
neuen Referenzprojekte (Mehrgebäude; zwei Puffer je Kanal; Prozesswärme mit eigenem Puffer;
Booster-Kette mit Kombi-Speicher). Build x64 Debug: 0 Fehler. **Neutralitätsnachweis:
Referenzlauf gegen Basis `2026-08-27_K1` = 216/216 CSV byte-gleich** — alle Fixes liegen
außerhalb des Rechenkerns bzw. in Vorschau-/Speicher-/Kopierwegen.

## Behobene Fehler

| # | Symptom | Root-Cause | Fix |
|---|---|---|---|
| A | Katalogvorschau „Strombedarf monatlich" zeigt zwölf Nullmonate | K1-Regression: `ProfilQuelle.Strom` ignorierte den Vorschaumodus und las die **Projektkopien** statt `Tab_Stromverbraucher(_typ)_STAMM` (die K1-Annahme „keine STAMM-Fassung" war falsch — 41/40 Katalogeinträge existieren; nur 5 hatten zufällig gleichnamige Projektsätze, 36 lieferten stumm 0) | Modus-Weiche auf die `_STAMM`-Tabellen; `TypSchluesselSpalte` bleibt in beiden Modi `Typname` (der Strom-Typkatalog führt keinen `Bezeichner`). Verifiziert per Kern-Harness: `Hotel_1` → 48,50 MWh = Katalogwert; vorher funktionierende Fälle wertgleich (`ProfilBedarf.cs:127-165`) |
| B | „Kosten bearbeiten…" in der WP-Detailansicht ausgegraut, obwohl die Anlage verbaut ist | Enable-Bedingung hing an `item.ID > 0 && item.ID_Projekt > 0` des Listenobjekts; der Ä24-Speicherweg schreibt nach Del+Add nur die frische Anlagen-ID zurück, **nicht** `ID_Projekt` — der Dialog hielt die gespeicherte Anlage für eine Neuanlage | `AnlagenzeileNachziehen()` (Muster Ä21/Schritt 47): Zeile über Anlagen-ID, ersatzweise über den Geräteanker `ID_WP` desselben Projekts nachziehen; ohne Treffer bleibt der Ä22-Sperr-Zustand (`Wizard_WPItem.cs:437-521`) |
| C | Pufferspeicher-Kosten verschwinden nach „Kostenverwaltung öffnen" | Zwei Ursachen: (1) `FindePosition`/`SetzeBetrag` suchten **anlagenblind** (ohne `ID_Anlage`) — bei mehreren Anlagen derselben Komponente (fünf Puffer!) nullte jedes neue Erfassen die Position der ersten Anlage und hängte sie um; (2) der Geräteanker stirbt mit der Gerätezeile: `GeraeteWaisen.Aufraeumen` läuft bei **jedem** Speichern, die Anker-Heilung lief erst beim nächsten UI-Aufbau — Positionen wurden „ohne Anlagenzuordnung" | (1) anlagenbezogene Suche + `ID_Anlage` im INSERT (`KostenPositionCtrl.cs:330-472`, beide Ä20-Schreibwege umgestellt); (2) Laufzeit-Nachzug `ZuordnungReparieren` + `AnkerNachziehen` **vor** der Waisenbereinigung im Speicherweg (`WizardCtrl.cs:981-1015`). DB-Probe: 14746 behält 1.200 €, 14744 bekommt eigene Zeile |
| D | „Speichern unter": Fremdkomponenten (Solar/PV/Stromspeicher) in der Kopie; Puffer fehlen | (a) Die Kopie vergab ihre Projekt-ID selbst als `MAX(Tab_Projekt.ID)+1` — nach Projektlöschungen (die in ~10 Tabellen Waisen hinterlassen, belegt: 7 tote Projekt-IDs) **erbt** die Kopie die Altreste dieser ID. (b) Puffer werden korrekt kopiert — sie sterben erst im **Del+Add-Speicherweg** der Anlagen + `GeraeteWaisen.Aufraeumen` (→ Folgeticket S1) | `FreieProjektId()`: die Kopie weicht IDs mit Rückständen aus (`ProjektDuplizierenCtrl.cs:207-231, 593-641`). Verifikation: vorher 12/29 Tabellen abweichend, nachher zeilengleiche Kopie |
| E | Vier CS-Fehler in der VS-Fehlerliste (csvReader, page.GetProperty, dt.Rows) | Veraltete IntelliSense-Einträge — VS war während Merge + Commits geöffnet; kompletter MSBuild-**Rebuild** ist fehlerfrei, die Fundstellen sind valide deklariert | Keine Codeänderung. Anwenderweg: VS schließen, `.vs`-Ordner löschen bzw. „Projektmappe neu erstellen", Fehlerliste auf „Nur Buildfehler" |
| F | Prozesswärme im neuen Projekt nicht zuordenbar | Kontextmenü der Zuordnungsliste erschien nur bei Klick **auf ein Element** — in leerer Liste unerreichbar; „Hinzufügen" seit jeher auskommentiert | Menü erscheint jetzt auch auf leerer Fläche (Muster Gebäude-Kontextmenü, `ProzesswaermeKontextMenuCtrl.cs:54-73`). Funktionierender Alternativweg: Startseite → Wärmebedarf → Kachel Prozesswärme |
| G | Booster-WP: Quelle „Pufferspeicher" nicht wählbar | Keine Freischaltungs-Sperre (Flag war in allen vier Projekten AN): die Booster-Modelle sind im Katalog als **`Luft-Wasser`** angelegt — dort ist die Quellenwahl konzeptgemäß fest „Luft" (`Form_Simulation_Config.Uebersicht.cs:749`, Meldung erscheint) | Keine Codeänderung. **Anwenderweg: Administration → Wärmepumpe → Wärmepumpentyp des Booster-Modells auf Sole-Wasser/Wasser-Wasser stellen, WP im Projekt neu auswählen.** Folgeticket: Meldungstext um diesen Weg ergänzen |

## Folgetickets

| # | Punkt | Ziel |
|---|---|---|
| FR-1 | **Puffer-Verlust im Del+Add-Speicherweg** (`WizardCtrl.Del_Projekt_Waermeerzeuger` löscht `ID_Type = 12`, `Add_WP_Waermeerzeuger` schreibt nur Erzeuger zurück, `GeraeteWaisen.Aufraeumen` räumt die Puffer; Feldbeleg: Kopien 1027/1009 mit `WS_Ziel='PufferHeizung'` und `WS_ID_Puffer = NULL`) | **S1, vorrangig** |
| FR-2 | Einheiten-Bruch Ergebnisdialog Stromverbraucher: Vorschau rechnet kWh-Stundenwerte, Projektpfad MWh — beide beschriftet MWh (Altbestand) | Einzelfix |
| FR-3 | `SIMQ_MSG_LUFT_WASSER` um den Bauart-Änderungsweg ergänzen (de+en) | Paket L |
| FR-4 | Altlast-Kostenpositionen mit toten/fremden Ankern in 1040–1042 (u. a. 3.000,50 € Puffer in 1041, 3.775 € Solar mit projektfremdem Anker) — Anwenderentscheid via Kostenverwaltung („ohne Anlagenzuordnung" umhängen) | Anwender |
| FR-5 | `Form_PufferSp.cs:130` vergibt Fantasie-Anlagen-IDs ab 100000 (`startindex`) — kollidiert ab echten IDs ≥ 100000 mit `KostenAnkerUmziehen` | Einzelfix |
| FR-6 | `FindeHauptposition`/`Pruefe` nehmen `MIN(ID)` je Komponente — bei mehreren Anlagen trifft der Planwertvergleich nur die erste | KD-Folge |
| FR-7 | Waisen-Altbestand toter Projekt-IDs (Migrationsschritt 34 räumt nur die 7 Gerätetabellen; `Tab_Brauchwasser`, `Tab_Stromganglinie`, `Tab_Ergebnis*` u. a. bleiben) | Aufräum-Migration |
| FR-8 | `Form_Prozesswaerme.cs:174/:226` wirft bei leerem Katalog (`CurrentCell`/`Rows[0]`) | Einzelfix |

# Protokoll KU3-4c — Erzeugerdialog der Kältemaschine (04.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle KU3-4c (E67, E68), ein Opus-Auftrag (84 Aufrufe), Commits `28279d5`, `f353f00`, `91198ae`, `0e73716`, `b27f786`, `e49e6bf`, Merge `4df19d7`. Statuszeile #717.
**Entscheid:** keiner neu beim Anwender; offen ist Q27 (Abschnitt 5). Kein Schemaschritt, kein Rechenweg, Basis R35 unverändert.

## 1 Auftrag

Den Erzeuger „Kältemaschine“ (Anlagenzeile Typ 13, KU3-4a) als Razor-Komponente bedienbar machen: Anlagen des Projekts anlegen, ändern, löschen, über `KaeltemaschineAnlageCtrl` (Kühlkonzept 8.2).

## 2 Vorgehen

Ein Opus-Auftrag in sechs Commits (Dialog samt Daten und Texten, Hülle, freie Ansicht und Anschluss, Einstieg, KI-Sicht, Tests); ein konfliktfreier Merge; Statuszeile und Papiere aus der Orchestrierung.

## 3 Ergebnis

- **Dialog** `EPOS.UI/Dialoge/Erzeuger/KaeltemaschineAnlageDialog.razor` (mit Daten, Texten, KiSicht): links die Anlagen des Projekts (Name, Anzahl, Nennkälteleistung × Anzahl); rechts Gruppe „Gerät“ (Kennwerte der Projektkopie nur lesend, Hinweis auf die Verwaltung „Kältemaschinen“) und Gruppe „Betrieb“ (Name, Anzahl, Kaltwasservorlauf mit kleinster Stützstelle als Platzhalter und Hinweis `Kaltwasser_Vorlauf_Min`, Hilfsstrom in % als Anteil gespeichert, Kühlträger über die Id mit Platzhalter „Stromträger des Projekts“, Schalter eigener Zähler, Hinweis „rechnet nach den Wärmepumpen im Kühlbetrieb“).
- **Fußleiste** `SpeichernLeiste`: „Hinzufügen…“ (Überlagerung mit `Katalogliste`, Profil `Anlagenart.Kaeltemaschine`, „Übernehmen“), „Löschen“ mit Rückfrage, Abbrechen, OK. Nichts wird ohne OK geschrieben: OK prüft jede Anlage mit `KaeltemaschineAnlageCtrl.Pruefen` (`KM_ANLAGE_*`), löscht Vorgemerktes, legt Neue über `Anlegen` an (Projektkopie und Anlagenzeile Typ 13) und speichert Geänderte je Anlage in einem Vorgang. Hilfeschlüssel `KaeltemaschineAnlage.btn_Help` (Eintrag in `help_mapping.txt`, Wiki in KU3-4b).
- **Hülle und Ansicht:** `EPOS.UI.Daten/Erzeuger/KaeltemaschineAnlageHuelle.cs`; freie Ansicht `Seitenschluessel.KaeltemaschineAnlage` für das offene Projekt (ohne Projekt `KMA_KEINE_ANSICHT`), angeschlossen an `AppWurzel`, `Hauptfenster`, `IProjektQuelle.KaeltemaschineAnlageGaben`, `IosProjektQuelle`, `HauptfensterHuelle`, `WinFormsNavigation`, `TestProjektquelle`.
- **Einstieg:** Knopf „Kältemaschinen…“ im Reiter „Energieerzeuger“ der Startseite (`ErzeugerReiter.razor`) über `Dienste.Navigation`.
- **KI:** `KaeltemaschineAnlageKiSicht`, `KiMaskennamen.KAELTEMASCHINE_ANLAGE`, Feldkarte `KiDialoge.KaeltemaschineAnlage` (7 Felder), `KiMaskenziele`, Abdeckungswache 6 Eingabestellen, Maskenzahl 92.
- **Texte:** 47 Ressourcen `KMA_*`/`KI_DLG_KMA_*`, beide Sprachen.

## 4 Nachweise

bunit `KaeltemaschineAnlageDialogTests` (9), `KaeltemaschineAnlageHuelleTests` (2, davon einer an Projekt 1017 in der Arbeitskopie); UI 1 365/1 365, Kern 242/242, Designer ohne Befund, Schale 0 Fehler.

## 5 Abweichungen und Offenes

- Keine Typweiche in `AssistentSeite`: Die Item-Nummer 13 ist dort `PUFFER_ITEM`; ein Assistentenweg braucht den Umbau von Komponentenauswahl und `AssistentCtrl`.
- Einstieg als freie Ansicht mit Knopf statt Kachel (Kachelzahl 21 samt Tests unverändert); Anlagenliste nur im Dialog.
- OK schreibt in zwei Controlleraufrufen (Anlegen, Speichern), erst nach vollständiger Prüfung.
- **Offen beim Anwender, Q27:** Kältemaschine als Startseitenkachel oder im Assistenten? Dazu die Sichtabnahme des Dialogs unter Windows.
- Folgewellen: KU3-5 Kältespeicher (läuft), KU3-4d Kältestromabrechnung, KU3-4b Referenzprojekt (Basis R36, Wiki, Logbuch, Hilfeanker).

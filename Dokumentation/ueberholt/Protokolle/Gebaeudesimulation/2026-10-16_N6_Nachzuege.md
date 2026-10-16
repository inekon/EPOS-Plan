# N6 — Nachzüge aus MA-6, N4/N5 und VS (16.10.2026)

Auftrag N6 der Sitzung „Gebäudesimulation“: die offenen Nachzüge aus dem
[Übergabepapier](../../../aktuell/Gebaeudesimulation/2026-10-12_Uebergabe_Gebaeudesimulation_Kontowechsel.md), Abschnitt 4
Punkte 2 und 3. Zweig `n6` ab `4de7a38b0`. Kein Schemaschritt, Rechenweg nicht berührt (kein Referenzlauf).

## 1. MA-6 (a): Kalender ohne den Zonendialog

- **Ursache:** Nachzug N2 markierte die Zone nur im OK-Weg des Zonendialogs (`GebaeudeZonenCtrl.Schreiben`). Die Knöpfe des
  Kalendereditors, die sofort schreiben (`KonditionierungCtrl.Vorgabe`, `Anlegen`, `Verwerfen`, `ErneutAnwenden`, „Vorlage
  übernehmen“, Zeitfenster, Feiertagsregeln — alle über `KonditionierungCtrl.Schrittweg`), und `KonditionierungCtrl.Schreiben`
  ließen die Zone ohne `MANUELL`; der Wiederimport ersetzte ihre Kalender durch die der Datei.
- **Lösung:** `KonditionierungCtrl.ZoneVonHandMarkieren` setzt im selben Vorgang `Herkunft = MANUELL`, wenn eine Zone der
  Eigentümer ist und `Schrittweg` etwas geschrieben hat bzw. `Schreiben` einen Kalender schreibt. Markiert wird nur eine Zone
  aus einer Datei (Quellkennung oder Herkunft IFC, gbXML, Projektdatei) — eine Zone ohne Datei übergeht der Wiederimport
  ohnehin; so bleiben die gebauten Referenzprojekte (Bauplan 1052 über „Vorlage übernehmen“) bitgleich. Die Importwege
  (`ZonenplanCtrl`, `RaumnutzungCtrl`, Zusammenführung) schreiben über `StandSchreiben` bzw. eigene Sätze und markieren nicht.
- **Test:** `WiederimportTests.Kalenderknopf_und_uebrige_Zonenfelder_markieren_die_Zone_und_ueberstehen_den_Wiederimport`
  (Projekt 1045, synthetische Projektdatei): eine Vorgabe der Geräte am Wochenende an der Halle markiert, die Vorgabezeilen
  überstehen den Wiederimport; Gegenprobe: dieselbe Vorgabe am Gebäude markiert keine Zone.

## 2. MA-6 (c): übrige Zonenfelder

- **Ursache:** `GebaeudeZonenCtrl.HandfelderGeaendert` verglich nur Übergabe, Kühlübergabe, Vorheizzeit, Höchstleistungen,
  Sollwerte und Höchsttemperatur. Beheizung, Kühlung, Strahlungsanteil, Luftwechsel (Infiltration, Nutzer), interne Gewinne,
  Bewohner und Nutzungsprofil, die der Zonendialog ebenfalls führt, markierten nicht; die Zusammenführung nahm dann den Wert der
  Datei (die Beheizung immer). Außerdem verlor eine Zone ohne Marke beim Wiederimport ihre Profilwerte (Außenluft je Person,
  Licht, Personenwärme), weil `ZoneZusammen` sie aus der Datei übernahm, die sie nie trägt.
- **Lösung:** Der Vergleich umfasst alle Felder des Dialogs außer der Geometrie (Nutzfläche, Raumhöhe, Volumen kommen
  weiter immer aus der Datei) und die drei Profilwerte; `ZoneZusammen` behält die Profilwerte, wo die Datei keinen Wert trägt.
- **Tests:** derselbe Wiederimporttest — Beheizung aus, Luftwechsel und Bewohner am Lager markieren, alle drei überstehen den
  Wiederimport; Gegenprobe: das unberührte Büro nimmt die Beheizung der Datei. Dazu
  `WiederimportTests.Handfelder_erkennen_die_uebrigen_Felder_des_Zonendialogs` (jedes der elf Felder einzeln, Gegenprobe
  unverändert und Leerraum im Profilnamen).

## 3. Herkunftszeile in der Überlagerung „Klimaregion wählen…“

- **Ursache:** Die Überlagerung aus N5 zeigte nur Liste und Hinweis; der Weg `GebaeudeKlimaregionWeg` trug keine Auskunft.
- **Lösung:** `GebaeudeKlimaregionWeg.Herkunftszeile` (Stamm-Id → Satz); die Hülle (`GebaeudeHuelle.KlimaregionWeg`) bildet ihn
  wie Kopfleiste und Projektassistent (`ProjektKopfHuelle.Herkunft`, `KlimaHerkunftGaben.Zeile`, `START_KLIMA_HERKUNFT`,
  `START_KLIMA_HERKUNFT_KURZ`). Der Gebäudedialog zeigt ihn als `Herleitungszeile` unter der Liste und fragt nur bei einem
  Wechsel der Wahl neu. Keine neuen Ressourcen.
- **Tests:** `GebaeudeDialogTests.N6_Klimaregion_zeigt_die_Herkunftszeile_der_Wahl` (Zeile steht, erneutes Zeichnen fragt
  nicht nach, Gegenprobe Region ohne Auskunft ohne Zeile); `GebaeudeKlimaregionHuelleTests` prüft den Satz der Hülle gegen den
  des Assistenten (Projekt 1030).

## 4. Offene Wärmebedarfsansicht nach einem Regionwechsel

- **Ursache:** Die Ansicht (Überlagerung im Gebäudedialog) bekam ihren Parametersatz einmal beim Öffnen; nach einer neuen
  Region las niemand nach.
- **Lösung:** Der Gebäudedialog merkt sich die Zeile der offenen Ansicht und liest ihren Parametersatz neu (`BedarfNeuLesen`,
  Schlüssel `@key` zählt hoch) — nach der eigenen Wahl (`KlimaregionNachziehen`) und beim bestehenden Ereignis
  `IProjektKontext.Gewechselt` (Wechsel über die Kopfleiste), angemeldet in `OnInitialized`, abgemeldet in `Dispose`. Während
  der eigene OK-Weg speichert, übergeht der Dialog das Ereignis (kein doppeltes Lesen). Kein Polling.
- **Test:** `GebaeudeDialogTests.N6_offene_Waermebedarfsansicht_liest_die_neue_Klimaregion` (eigene Wahl und Ereignis lesen je
  einmal neu; Gegenproben: ohne offene Ansicht liest das Ereignis nichts, nach `Dispose` hört der Dialog nicht mehr zu).

## 5. gbXML: Bezeichner mit Zeilenumbruch

- **Ursache:** `GebaeudeRestdaten.MitBlock` schrieb Schlüssel=Wert-Zeilen ungefiltert; ein Name mit Zeilenumbruch (Woche,
  Periode, Partnerzone) teilte die Zeile, `OhneBlock` verwarf den Rest (Zeile ohne `=`).
- **Lösung:** `GebaeudeRestdaten.Einzeilig` setzt beim Schreiben für Zeilenumbrüche (auch U+0085, U+2028, U+2029) und
  Steuerzeichen Leerzeichen; `OhneBlock` hängt eine Zeile ohne Schlüssel mit Leerzeichen an den Wert davor (fremde oder ältere
  Datei). Die Formatgrenze — gbXML kennt für Feiertage nur den Tagtyp `Holiday` — steht nun in der Wiki-Quelle
  `Projekte/Wiki/Datenexport.wiki` (Abschnitt Kalender), dazu ein Satz zu den einzeiligen Namen im Block (Abschnitt Rundlauf).
- **Test:** `RestdatenRundlaufTests.Bezeichner_mit_Zeilenumbruch_bricht_den_Block_nicht` (Rundlauf mit CR/LF, Tab und
  Steuerzeichen in einem Wochennamen und LF in einem Periodennamen: keine Meldung, alle Wochen, Perioden und Zeilen zurück;
  Lesen eines umbrochenen Werts; `Einzeilig` mit U+2028 und ohne Treffer).

## 6. Ressourcentexte mit „Befund“

- **Ergebnis:** bereits erledigt. Im Stand `4de7a38b0` lauten `ADM_DUBLETTEN_BAUM` „Dublettenhinweise“ und `BHW_K_LEER` „Keine
  Auffälligkeit im zuletzt gebuchten Lauf.“; kein `<value>` einer `.resx` enthält „Befund“, der Rückfalltext in
  `BhkwWirtschaftlichkeitTexte.cs` ist gleich, die Treffer in `.razor` sind Bezeichner und Kommentare. Nichts geändert.

## Abnahme

- Kern-Filter Release 0 Fehler; Windows-Schale (Linux, `EnableWindowsTargeting`) 0 Fehler.
- SQL-Dialekt-Prüfer: 3020 Texte, 0 Fundstellen.
- Tests: siehe Bericht des Auftrags (Kern- und UI-Filter).

## Logbuch

- „Kalenderänderungen und alle Werte einer Zone, die von Hand bearbeitet wurden, bleiben bei einem erneuten Gebäudeimport
  erhalten.“
- „Die Auswahl der Klimaregion im Gebäudedialog zeigt die Herkunft der Klimadaten; eine offene Wärmebedarfsansicht rechnet
  sofort mit der neu gewählten Region.“
- Punkt 5 und 6: kein Eintrag.

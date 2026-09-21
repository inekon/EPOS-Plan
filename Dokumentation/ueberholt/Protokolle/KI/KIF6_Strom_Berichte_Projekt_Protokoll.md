# KI‑F6 — Strom, Berichte und Projekt für den Hilfe-Assistenten (Protokoll, 21.09.2026)

Statuszeile #425 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Konzept
[`Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md`](../../../aktuell/Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md)
(Stufe S4, KI‑D‑Q5 und KI‑D‑Q6); Vorgänger [`KIF5_Erzeugerkataloge_Protokoll.md`](KIF5_Erzeugerkataloge_Protokoll.md),
[`KIF4_Kosten_Wirtschaftlichkeit_Protokoll.md`](KIF4_Kosten_Wirtschaftlichkeit_Protokoll.md),
[`KIF2_Simulationskonfiguration_Protokoll.md`](KIF2_Simulationskonfiguration_Protokoll.md).
Zweig `ki-f6`, Commits `87ccdc83` (Strom), `b658cf21` (Berichte und Projekt), `df7cae35` (Ansicht Simulation); Merge `70e28a5a`.
Mit dieser Welle ist die Stufe S4 (alle Masken mit Einstellwerten, sechs Wellen) abgeschlossen.

## Die Masken

| Katalogschlüssel | Dialog oder Seite | Bindung | Felder (gesamt / nur lesbar / Wahl) | Haken | Ziel | dialog_oeffnen |
|---|---|---|---|---|---|---|
| `Form_PeakShaving` | Peak-Shaving | `PeakShavingKiSicht` | 22 / 3 / 2 | Auffrischen, Prüfen | `Masken.PeakShaving` | Windows ja, iOS benannt abgelehnt |
| `Speicherzeitreihen` | Speicher-Zeitreihen | `SpeicherZeitreihenKiSicht` | 18 / 2 / 10 | Auffrischen, Prüfen | `KiMaskenziele.STROMSPEICHER_AUSLEGUNG` | iOS ja, Windows benannt abgelehnt |
| `Form_Stromganglinie_Admin` | Stromganglinien-Verwaltung (Zeitintervall) | `StromganglinieAdminKiSicht` | 2 / 1 / 1 | Auffrischen | `Masken.StromganglinieAdmin` | Windows ja, iOS benannt abgelehnt |
| `Berichtsuebersicht` | Seite Übersicht (Berichte & Kosten) | `UebersichtSeiteKiSicht` | 6 / 2 / 2 | Auffrischen | `Ansichten.BerichteKosten` | iOS ja, Windows benannt abgelehnt |
| `Berichtsseite` | Seite Bericht (Berichte & Kosten) | `BerichtSeiteKiSicht` | 5 / 3 / 1 | Auffrischen | `Ansichten.BerichteKosten` | iOS ja, Windows benannt abgelehnt |
| `Form_ProjektSpeichernUnter` | Projekt speichern unter (Projektkopie) | `ProjektKopieKiSicht` | 5 / 0 / 1 | Auffrischen, Prüfen | `Masken.ProjektSpeichernUnter` | Windows ja, iOS benannt abgelehnt |
| `Projektvariante` | Projektvariante | `ProjektVarianteKiSicht` | 4 / 1 / 1 | Auffrischen, Prüfen | `KiMaskenziele.PROJEKT_VARIANTE` (neu, gegen `Seitenschluessel.ProjektAlsVariante`) | beide benannt abgelehnt |

Katalog 56 → 63 Masken, 603 → 724 Felddeklarationen. Auf Windows kennt `WinFormsNavigation.OeffneMaske`
weder `Ansichten.BerichteKosten` noch das Variantenziel — dieselbe Lage wie bei den Masken der
Welle KI‑F4 auf derselben Ansicht.

## Ergänzungen an bestehenden Masken

Die Ansicht **Stromspeicher-Auslegung** führt 85 statt 27 Felder und damit alle vier Stationen
(offener Punkt aus Statuszeile #420): 23 Spalten je Speichereinheit (Station 1), 17 Felder für
Datenquellen, Kostensätze und Jahresprojektion (Station 2), 10 Felder für Betriebsführung,
Netzgrenzen und Prognose (Station 3), 8 Spalten je Suchachse (Station 4). Setzbar sind 26 flache
Felder und 31 Spalten, 28 flache Felder bleiben abgeleitet. Einheitenzeilen und Suchachsen sind
Sammlungen, weil ihre Zahl erst zur Laufzeit feststeht; zwei Zeilenhüllen rechnen die fünf
Prozentwerte um, die die Engine als Anteil 0…1 führt. Acht feste Klapplisten (Verteilungen,
Erzeugerprioritäten, Prognosearten, Endbedingungen, Projektionsarten, Last‑, PV‑, Preis‑ und
Kostenquellen, Gerätequellen) stehen jetzt öffentlich, damit Maske und Assistent nicht
auseinanderlaufen. Die Ansicht **Simulation** führt 40 statt 39 Felder: `autarkie_speicher`
(kWh), das einzige echte Eingabefeld der neun Reiterblätter.

## Entscheidungen

Die Bausteine der Speicherflotte (Editor, Betrieb, Netz- und Wirtschaftsblock), der
Auslegungseditor sowie Optimierungs- und Leistungspreisblock machen kein eigenes Fenster auf und
sind Felder der Auslegungsansicht auf ihren fünf Blättern — wie der Reiter „Stromspeicher" der
Simulationsansicht. Draußen bleiben: `StromganglinieDialog` (Zuordnung und Auswahl aus zwei
Listen, seine Schalter schalten ein Bild), `NamensDialog` (Überlagerung von achtzehn Wirten, der
Wert ist der Bezeichner des Wirts), Rainflow-Kurve, Preisprofil und Endenergieziele je Einheit,
Auslegungsprofile, Gerätetabelle der Suchkarte, Vergleichswahl der Übersicht, Suche der
Projektliste, das Ganglinien-Protokoll, die zwei Flotten-Ergebnisansichten, Flotten-CSV und
Ganglinien-Importoptionen; nach KI‑D‑Q5 Projektwahl, Projekttransfer, Berichtsübernahme,
Einstellungen, Katalogdubletten, Import, Lizenz, Hilfe, Startseite und Assistentenschritte.
Von den Reiterblättern der Simulation bleiben rund 45 Schalter draußen („sortiert" in zehn
Blättern, Reihenhaken, Streuwolken der Wärmepumpe, Nullzeilen, innere Blattwahlen, Farbwahl je
Reihe): Sie schalten ein Bild, schreiben nichts und liegen in privaten Feldern, die der Reiter
bei jedem Zeichenlauf neu aufbaut; die Ganglinienreiter lesen ihren Sitzungsstand nur einmal beim
Aufbau. Speicherparameterblock und Konfigurationsseite waren bereits vollständig gedeckt.

## Gefundener Fehler

`UebersichtSeite.Auffrischen()` rief `StateHasChanged()` unmittelbar; der Assistent setzt nicht
aus dem Blazor-Verteiler, der Renderer wirft dann „The current thread is not associated with the
Dispatcher". Die drei Wahlwege der Seite laufen jetzt über `InvokeAsync`, ebenso
`SimulationErgebnisSeite.SpeicherkapazitaetSetzen`.

## Zahlen und Abnahme

151 neue Ressourcenschlüssel je Sprache (Anzeigenamen sind die Beschriftungen der Masken; eigene
Schlüssel nur, wo die Maske ein deutsches Literal trägt oder zwei Bedienelemente sich eine
Beschriftung teilen), Designer neu erzeugt und wiederholbar; 25 neue Prüfmethoden (26 Fälle) und
sieben weitere Fälle in der Masken-Theorie. Worktree: Kern-Filter 0 Fehler, 10 296 Tests grün
(1 übersprungen). Fünf `.razor` waren im Bestand ohne BOM und blieben byte-erhaltend so. Gate im
Hauptbaum auf `70e28a5a`: siehe Statuszeile #425.

## Offen

1. Unter Windows führt `dialog_oeffnen` für die vier Blätter der Ansicht „Berichte und Kosten" ins
   Leere (`WinFormsNavigation.OeffneMaske` kennt `Ansichten.BerichteKosten` nicht) — mit einem
   `case` behebbar, aber eine Änderung an der Windows-Schale.
2. `Projektvariante` lehnt auf beiden Plattformen benannt ab; ein Weg für das Variantenziel in
   `WinFormsNavigation` und `AppWurzel` wäre ein eigener Auftrag.
3. Die Beschriftungen von `SpeicherZeitreihenDialog` stehen als deutsche Literale im Markup; der
   Katalog führt sie zweisprachig, die Maske nicht.
4. `IntervallKonvention` bindet in `SpeicherZeitreihenDialog` den Aufzählungswert als Listenindex
   (Anfang = 1, Ende = 2 gegen Listen-Ids 0/1) — ein Bestandsfehler der Maske; die Sichtklasse
   umgeht ihn über die Aufzählungsnamen.

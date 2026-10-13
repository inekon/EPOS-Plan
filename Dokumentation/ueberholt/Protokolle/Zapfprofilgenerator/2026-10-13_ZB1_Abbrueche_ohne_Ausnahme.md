# Abbrüche im Dialog Zapfprofil: Zwischenstände der Bearbeitung ohne Ausnahme (ZB1, 13.10.2026)

Protokoll der Welle **W1** der Sitzung „Zapfprofilgenerator Cloud“ zum Vorschlag **ZB1** des
[Konzepts Zapfprofil — Benutzerführung](../../../aktuell/Konzept_Zapfprofil_Benutzerfuehrung_EPOS-Plan.md)
(Befund B1).

**Rahmen.** Worktree `agent-a561547bc9f713ae1`, Zweig `worktree-agent-a561547bc9f713ae1` von
`930880ca6` (Arbeitszweig `ios_migration_september`, Referenzbasis `2026-10-11_R52_Vorheizen`),
Opus 5.5. Ein Commit `202003f69`, zusammengeführt als `f5cb9c391`. Kein Schemaschritt, keine Änderung
der Testdatenbank, kein Basiswechsel.

**Befund (Anwender, Bildschirmfoto aus Visual Studio).** Über die Kachel Brauchwasserwärme im Dialog
Zapfprofil hält Visual Studio an: `ZapfprofilEingabeException` „Die Nutzungsart der Zone „Zone 1“ steht
nicht (mehr) im Katalog — bitte eine Nutzungsart wählen.“ in `ZapfprofilRechner.Rechnen`. Nachgestellt
im Browser auf einer Kopie der Testdatenbank (Projekt 1045): „Zone hinzufügen…“ legt eine Zone mit
`IdNutzungsart = 0` an, die entprellte Vorschau rechnet sofort, der Kern wirft und fängt im selben
Schleifenrumpf, die Zone trägt 0. Ohne Debugger bleibt eine gelbe Meldung; mit „Bei Auslösen
unterbrechen“ hält Visual Studio bei jeder Eingabe an. Dieselbe Bauart steckt in 59 Wurfstellen des
Kerns, viele davon im Bearbeitungsfluss erreichbar.

---

## 1 Vorprüfung je Zone ohne Ausnahme

Neue Datei `EPOS.Kern/Allgemein/Zapfprofil/Zonenvorpruefung.cs`. `ZapfprofilRechner.Rechnen` und
`ZapfprofilAuslegung.Rechnen` rufen sie je Zone vor dem `try`; eine Zone mit Befund trägt 0 mit derselben
benannten Ablehnung wie vorher (Code und Satzkennung je Fall unverändert, belegt durch einen Lauf der
Wache gegen den alten Code). Geprüft wird in der Reihenfolge des Rechenwegs:

| Gruppe | Fälle |
|---|---|
| Nutzungsart | Id ≤ 0 → neuer Satz `EINGABE_NUTZUNGSART_NICHT_GEWAEHLT`; Id nicht im Katalog → `EINGABE_NUTZUNGSART_FEHLT`, jetzt mit Nummer; beide Code `NutzungsartFehlt` |
| Tagesgangsatz | Id unbekannt |
| Temperaturen | Bezugstemperaturen fehlen, Zapf- oder Kaltwasserwert nicht endlich, Amplitude negativ, Monat des Maximums ungültig, fehlende Parameter (`Parametersatz.FehltSatz`) |
| Mengengerüst | Wohnungstyp ohne Anzahl, Belegung nicht bestimmbar, Bezugsmenge ≤ 0 unter Beachtung der wirksamen Wohnungstabelle, manueller Tagesbedarf leer oder negativ, Spreizung nicht positiv, spezifischer Bedarf negativ, Wohnfläche ≤ 0, Katalogbedarf des Niveaus ungültig |
| Zeitstruktur | Monats- und Wochenfaktoren ungültig, Wochensumme 0, Auslastungsgang negativ, Ferienfaktor negativ, Tagesgangsatz unvollständig oder negativ |
| Kaltwassergang | Zapftemperatur nicht über dem Kaltwassermittel oder unter dem Kaltwasser eines Monats |
| Jahresmesswert | nicht positiv, ohne Einheit, Volumen mit anderer Bilanzgrenze als 1, kWh/a ohne Grenze |

Ergänzend ohne Wurf: die Kalibrierung (`Mengengeruest.KalibrierBefund`, vor Phase 3, weil die Zone bis
dahin zur Zirkulation beiträgt), die Zirkulation des Projekts (`Zirkulationskanal.Befund` vor `Ansetzen`
und vor den Vorschlägen), die internen Vorschläge (`Mengengeruest.Vorschlag`,
`TemperaturfaktorOderNull`, `Zirkulationskanal.Vorschlag`, Zirkulationsvorschlag der Auslegung) und die
DIN-4708-Kennzahl: Schon der gespeicherte Stand von Projekt 1045 warf bei jedem Ladevorschlag und jeder
Auslegung eine `ZapfAuslegungException` („… trägt keine Wohnungstabelle“), geworfen und gefangen in
`Din4708Kennzahl.Rechnen`; jetzt kommt dasselbe Ergebnis direkt zurück.

**Eine Regel je Fall.** Die Wurfstellen werfen aus denselben `…Befund`-Prädikaten, die die Vorprüfung
ruft (`Mengengeruest`, `Formvektor`, `Kaltwassergang`, `Zirkulationskanal`), über
`ZapfAblehnung.Ausnahme()`; sie bleiben Netz für Unerwartetes.

**Bewusst als Netz geblieben:** `Zapfkalender.Pruefen`, fehlende Projektgrößen und Parametersatz,
`NichtVerfuegbar` (Projekt- und Umgebungsebene, die Hülle prüft vorher); der Typtagweg
(`TyptageUngueltig`, nur bei gewählten Typtagen); die stochastischen Wege (nur auf Knopfdruck); das
Innenleben der Auslegungsgruppen; `DeltaPruefen` der Volumenumrechnung; `Parametersatz.Lies`.

**Abweichung vom Auftrag:** Ein halber Ferienzeitraum (nur Beginn) führte schon bisher zu keiner
Ablehnung — es entsteht kein Ferienfenster, die Zone rechnet. Eine Ablehnung hätte das Ergebnis
geändert; die Wache prüft „keine Ausnahme, Zone rechnet“.

## 2 Ressourcen

Neu `ZPG_SATZ_EINGABE_NUTZUNGSART_NICHT_GEWAEHLT` („Für die Zone „{0}“ ist noch keine Nutzungsart
gewählt.“ / “No type of use has been chosen for zone “{0}” yet.”); geändert
`ZPG_SATZ_EINGABE_NUTZUNGSART_FEHLT` („Die Nutzungsart Nr. {1} der Zone „{0}“ steht nicht (mehr) im
Katalog …“), beide Sprachen; `Resource.Designer.cs` neu erzeugt.

## 3 Wache

`EPOS.Kern.Tests/ZapfprofilBearbeitungOhneAusnahmeTests.cs`, 33 Fälle; sie zählt jede Ausnahme des
eigenen Fadens über `AppDomain.FirstChanceException` (ohne `OperationCanceledException`).

- **Rechenweg:** 22 Zwischenstände je neben einer vollständigen Zone — Code und Satzkennung, Zone trägt
  0, die vollständige Zone rechnet Stunde für Stunde wie allein, Gleichlauf Vorprüfung/Rechenweg; fünf
  Zirkulationsfälle; die zwei Nutzungsart-Sätze unter de-DE.
- **Hülle (Testdatenbank, Projekt 1045):** Vorschau, Ladevorschlag, Prüfung und Auslegung für die
  Zwischenstände (a) bis (j) und den unveränderten Stand. Der Fall Wohnungstyp mit Bezugsart
  Wohneinheiten läuft nur auf Ebene Rechenweg (der Testkatalog führt keine Nutzungsart je Wohneinheit).
- `ZapfprofilTrennungWacheTests` kennt die neue Datei (der Bilanz zugeordnet).

## 4 Abnahme

| Prüfung | Ergebnis |
|---|---|
| Bau `WP-Plan.Kern.slnf` | 0 Fehler |
| Tests im Worktree (Basis `930880ca6`) | `EPOS.Kern.Tests` 13 953 grün, 7 übersprungen; `EPOS.UI.Tests` 9 322; `KiKern.Tests` 553; `SpeicherEngine.Tests` 397; `SpeicherPlanung.Tests` 27, 1 übersprungen |
| Designer | 18 056 Einträge, 0 abweichend |
| Referenzlauf gegen `2026-10-11_R52_Vorheizen` | GESAMT PASS, zwölf Projekte der CI-Auswahl; 1045 und 1030 byte-gleich |
| Gate auf dem Merge-Stand `f5cb9c391` | siehe Statuszeile |

## 5 Offen

- Der Typtagweg wirft weiter, wenn ein Projekt Typtage gewählt hat und keine eingespielt sind (Schalter
  ohne eingespielte Typtage gesperrt; nur bei später gelöschten Typtagen erreichbar).
- „Unvollständig“ statt „trägt 0“ in der Zonenliste und der nächste Schritt am Zonenblock folgen mit
  Welle W2 (ZB2).

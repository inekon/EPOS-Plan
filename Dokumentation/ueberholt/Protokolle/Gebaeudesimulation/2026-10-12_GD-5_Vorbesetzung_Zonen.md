# Protokoll GD-5 — Vorbesetzung der Zonen

Welle GD-5 aus dem [Entwurf Gebäudedialog](../../../aktuell/Gebaeudesimulation/2026-10-11_Entwurf_Gebaeudedialog_Mehrzonen.md)
(Abschnitte 1.5 Lücken 1–3, 2.5, 6 F5, 7 Zeile GD-5), Entscheid E130 des Anwenders vom 12.10.2026 nach Empfehlung: F5, die
Vorgaberegel 2.5 greift auch im Einzonenweg der Projektdatei, sichtbar als Vorgabe und abwählbar. Konzeptabsatz NP-F16 in
[Konzept Nutzungsprofile](../../../aktuell/Konzept_Nutzungsprofile_EPOS-Plan.md) auf den gültigen Stand gebracht.

## Umgesetzt

- **Kern, Vorgaberegel:** `Zonenvorbesetzung` (`EPOS.Kern/Allgemein/Import/Gebaeude/`) hält die Vorgabe im Code
  Gebäudeart → Nutzung (Wohnbauten → Wohnen, Schule → Schule, Verwaltung und Verwaltungsgebäude → Büro; ganzer Schlüssel,
  getrimmt, ohne Unterschied der Schreibung). Die Kennung führt über `Raumnutzungsvorbelegung.AusKennung` auf das EPOS-Muster
  gleicher Nutzung. Andere Arten (Hotel, Krankenhaus, Gewerbe, Sonstige …) bekommen keine Vorgabe, weil kein Muster mit
  Kennwerten passt. `Vorgabe` nimmt die DIN-Nummer vor der Gebäudeart. Die Herkunft ist eine `Profilquelle` der Art
  `VORGABE_DIN_NUMMER` bzw. `VORGABE_GEBAEUDEART`. Eine Zeile `GEBAEUDEART` in `Tab_Raumnutzungszuordnung` ist nicht möglich
  (CHECK auf drei Arten); dafür wäre ein Schemaschritt nötig, deshalb bleibt es bei der Vorgabe im Code.
- **Rangfolge je Größe:** `Zonenvorbesetzung.Wirksam` und `JeGroesse` halten Ganglinie vor Wert der Datei vor Profil vor Erbe;
  Heizen und Kühlen einer unbeheizten Zone sind „unbeheizt“. Der Schreibweg (`ZonenplanCtrl.ProjektdateiUebernehmen`) ist
  unverändert in dieser Folge.
- **Import mit Zonen (Lücke 1):** Die Hülle setzt die Vorgabe der Gebäudeart in den Zonenplan, sobald er aus der Regel bzw.
  der Projektdatei entsteht, und vor den Schritten des Anwenders. So wählt „keine“ von Hand sie ab. Betroffen ist jede beheizte
  Zone mit Räumen ohne Profil und ohne Quelle, und nur bei mindestens zwei Zonen. Die Gebäudeart ist die, die das
  importierte Gebäude bekommt (`GebaeudeImportHuelle.ImportGebaeudeart`): die der Datei, sonst die Vorgabe des neuen Satzes,
  „Sonstige“ bei Nichtwohnen. Die Herleitungszeile des Zonenbaums nennt „Vorgabe (Profil der Gebäudeart „…“)“ bzw.
  „Vorgabe (DIN-Nr. n der Projektdatei)“.
- **Einzonenweg der Projektdatei (Lücke 2, F5):** Der Block der Projektdatei nennt die Vorgabe (DIN-Nummer, sonst
  Gebäudeart) mit dem Haken „Profil als Vorgabe setzen“, gesetzt. Die Herkunft des Imports trägt das Profil
  (`GebaeudeImportHerkunft.Gebaeudeprofil`). `WizardCtrl` schreibt es über `ZonenplanCtrl.ProjektdateiUebernehmen` als
  Gebäudekalender vor der Konditionierung der Datei (Datei vor Profil). Abgewählt bleibt das Gebäude ohne Profil.
- **Lücke 3:** Die Herleitungszeile im Zonenbaum hängt „ohne Kennwert, vom Gebäude geerbt: …“ an, wenn das Profil eine
  Größe nicht trägt (`GebaeudeImportZonen.Ohnekennwerttext`). In der Zonenliste des Gebäudeeditors zeigt „geerbt: …“ dieselben
  Größen.
- **Zonenliste im Gebäudeeditor:** neue Spalte „Nutzung/Profil“ (`Zonennutzung`). Sie zeigt das Profil, die vom Gebäude
  geerbten Größen („geerbt: Lüftung, Geräte“ bzw. „geerbt vom Gebäude“) und bei unbeheizten Zonen „ohne Heiz- und
  Kühlkalender“. Ohne Profil zeigt sie „geerbt vom Gebäude“ und die Vorgabe der Gebäudeart (`GebaeudeZonenweg.Vorgabeprofil`,
  nur im Projekt). Die Aktion „Profil/Kalender zuweisen…“ öffnet die Zone im Zonendialog. Dort liegen „Nutzungsprofil
  übernehmen…“ (Generator) und die Kalender je Größe. Bestehende Zonen werden nicht automatisch beschrieben; die Vorgabe
  steht dort als Angabe.

## Rechenweg

Die Regel wirkt nur beim Import (Zonenplan und Einzonenweg). Bestehende Gebäude und die Referenzprojekte werden nicht
beschrieben. Es gibt keinen Schemaschritt und keine Einfrierregel.

## Nachweis

- Kern-Filter und Windows-Schale (Linux-Bau): 0 Fehler.
- Tests mit Filter (Zonen, Raumnutzung, Konditionierung, GebaeudeImport, Sqproj, Wache, Ressource, KiMasken): EPOS.Kern.Tests
  2158, EPOS.UI.Tests 1237. Nach dem Nachziehen von fünf Erwartungen auf das neue Verhalten (Herleitung mit Lücke 3, Plan
  mit Vorgabe, Fußzeile der Zonenliste, Herleitungszeilen im Zonenbaum, Eingabebilanz des Importdialogs 31) sind die
  betroffenen Klassen grün: 29 und 39 Fälle.
- Neu: `ZonenvorbesetzungTests` (Rangfolge je Größe, unbeheizt, Datei vor Profil, Lücke 3, Gebäudeart → Nutzung, DIN vor
  Gebäudeart, Zone ohne Profil bekommt das Gebäudeartprofil und ist abwählbar, Einzonenweg schreibt den Gebäudekalender),
  Einzonenfall in `ZonenbaumHerleitungTests` mit abwählbarer Vorgabe, `GebaeudeZonennutzungTests` (Spalte, „geerbt“,
  Aktion) und der Haken im Importdialog.
- `SqlDialektPruefer`: 0 Fundstellen.
- Referenzlauf der CI-Auswahl (1030, 1007, 1017, 1045, 1046, 1047, 1049, 1051, 1058, 1060, 1063, 1065) gegen
  `2026-10-11_R52_Vorheizen`: alle zwölf PASS; FAIL nur für die nicht gerechneten Projekte.

## Offen

- `SqprojGebaeudecodes` belegt noch keinen Code für die Gebäudeart. Die Gebäudeart des Imports ist deshalb bis auf Weiteres
  die Vorgabe des neuen Satzes (Einfamilienhaus, bei Nichtwohnen Sonstige).
- Mehrfachwahl der Aktion „Profil/Kalender zuweisen…“ über mehrere Zonen (Entwurf 2.5) ist nicht umgesetzt; die Aktion wirkt
  je Zone.

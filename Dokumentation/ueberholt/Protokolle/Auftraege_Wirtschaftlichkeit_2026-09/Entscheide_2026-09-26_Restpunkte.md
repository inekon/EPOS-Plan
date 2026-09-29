# Anwenderentscheide 26.09.2026, ~08:25 (Wirtschaftlichkeits-Sitzung) zu den Restpunkten nach #531

| Punkt | Entscheid | Umsetzung |
|---|---|---|
| E28 (Befund N7 aus E27: Vorab-Überschuss des PV-Modus der Wärmepumpe, Kessel-Vektorstufe hinter dem BHKW) | **Prüfwelle ausführen** | Welle E28, Opus Phase 0 (Befund an den Referenzprojekten, Empfehlung), Bau nur bei Befund nach Freigabe; Statusnummer beim Push messen |
| E27‑Q3 | **b** — eigene Diagnosereihe und Zeile „BHKW-Einspeisung" im BHKW-Reiter | kleine Anzeige-Welle E29 zusammen mit E26‑Q6/N6 |
| E26‑Q6 und N6 (Strombilanz-Diagramm, Excel-Spalte „Strombedarf", Übersicht „Strombedarf mit Eigenverbrauch" ohne Kältestrom) | **in einer kleinen Welle nachziehen** | Welle E29 (Anzeige, kein Kapitalwert) |
| § 6.3 Nr. 21 (Betriebskosten von Referenzprojekt 1030) | **Sichtprüfung** | Prüfbericht durch einen Agenten (Katalog, Bemessung, Zahlen gegen R20), Ergebnis dem Anwender vorlegen; Papier erst nach Befund |
| § 6.3 Nr. 22 (Sichtabnahmen B2, BK1, B4) | **ok** | im nächsten Papierschritt als abgenommen schließen |
| Straffung des Quelltextlesers | **a** (eigene Datei) | erledigt mit #533 (Commit 6e01aed3 im Worktree cwn) |
| Standardkultur für KiKern-, SpeicherEngine-, SpeicherPlanung-Tests | **jetzt vorsorgen** | Nachtrag in #533 (Agent im Worktree cwn, zweiter Commit); Gate #533 dafür neu gestartet |

Reihenfolge: #533 abschließen (Nachtrag, Gate, Papiere mit Nr. 22, Push) → E28 Phase 0 → E29 → Sichtprüfung 1030 parallel (nur Lesen).

**08:45 Nummernkreuzung:** Zapfprofil hat #533 (ZU21) gepusht → CI-Nachlese = **#534** (Kommentar-Commit auf pm26), E28 = #535, E29 = #536 (bei Zapfprofil reserviert), nächste freie #537.

## Anwenderentscheide 26.09.2026, ~09:50 zur Sichtprüfung 1030 (P1030) und N10 (E29)

| Punkt | Entscheid | Umsetzung |
|---|---|---|
| B3 Doppelstruktur Wartung (Altzeile neben leerer Pflichtzeile, 1030) | **bereinigen** | Welle E30: Beträge in die Pflichtzeilen umziehen, Altzeilen entfernen; ergebnisneutral (20.000 €/a, Anker bleibt); wiederholbares Skript wie E24 |
| B5 Bemessungsart der Hilfsenergie-Pflichtzeilen | **„Prozent des Endenergiebedarfs"** (wie Vorlagen und Empfehlungen) | E30: Bemessungsart der fünf Zeilen (1030) und bei 1026 umstellen; ohne Satz ergebnisneutral |
| B4 Hilfsstrom fehlt | **Änderung vornehmen: Wenn Hilfsenergie angegeben ist, müssen die Hilfsenergiekosten daraus ermittelt werden** | E30 Kern: Rechenweg Hilfsenergie → Kosten (Phase 0 klärt Quelle der Hilfsenergie je Anlage, Preis, Ort im Ausweis, Wirkung auf Kapitalwert/Anker/R20) |
| B8 zwei Kapitalwert-Anker 1030 (−21,9 Mio. Lauf 212 vs. −31,1 Mio. Berichtsdaten) | **nach Empfehlung: klären** | E30 Phase 0: Ursache der 9,2 Mio. € Differenz und des abweichenden BHKW-Brennstoffs von Lauf 212 |
| N10 BHKW-Deckungsgrad zählt Einspeisung, teilt durch Projektbedarf | **nach Empfehlung: korrigieren** | E30 Kern: Deckung = Eigenverbrauch BHKW-Strom / Gesamtbedarf; Neueinfrierung R21 (mit B4 gemeinsam, eine Einfrierung) |

Statusnummer E30 = **#541** (Zapfprofil hält #537–#540), nächste freie #542; Schemaschritt 149 frei (E30 braucht voraussichtlich keinen).

**10:30 Nummernkreuzung:** BV‑E5 (Anwender-Sitzung) hat #541 gepusht (origin fce6060f) → E30 = **#542**, nächste freie #543. BV‑E5 erweitert die ChartProben-Messlatte auf 183 Bilder (Gate-Messlatte lokal nachziehen).

**12:25 Nummernkreuzung:** BV‑E6 (Anwender-Sitzung) hat #544 gepusht (origin 69cf3ced) → E30 = **#545** (Kommentare/Papiere auf pm26 umgestellt), nächste freie #546.

**12:45 Nummernkreuzung:** Dialog Design hat #545 gepusht (origin 196f9434), Zapfprofil hält #546/#547 → E30 = **#548**, nächste freie #549.

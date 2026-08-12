# Prüfbericht Rechenkern Wirtschaftlichkeit (W1–W3) + Abnahme-Checkliste

Stand 11.08.2026 · Verifikation vor dem ersten Build der Phasen 6–8.
Methode: Die reine Rechenlogik aus `KapitalwertRechner.cs` und `StromMatrix.cs`
wurde 1:1 in Python nachgebaut und gegen Referenzwerte (goetz_test.XLS,
geschlossene Formeln, Handrechnungen) geprüft. **Ergebnis: 40/40 Prüfungen
bestanden.** Die Prüfskripte liegen der Cowork-Session bei; sie prüfen die
Logik, nicht den kompilierten C#-Code — der Build-Test bleibt der letzte Schritt.

## 1. Ergebnisse der numerischen Verifikation

| Prüfblock | Prüfungen | Referenz | Ergebnis |
|---|---|---|---|
| Annuität a(i,n) | 17 150 € · a(0,35 %; 13,33 a) = **1 319 €/a**; 3 731 € · a(1,5 %; 20 a) = **217 €/a**; Grenzfall i→0 | goetz_test.XLS (Alt-Verfahren) | ✓ |
| Kapitalwert | KW = −I₀ − K·BWF(i,T) (geschlossene Form); wachsende Rente p = i → BW = K·T/(1+i) | Finanzmathematik | ✓ |
| Ersatz + Restwert | n=10/T=20 (Ersatz t=10, RW 0) · n=15/T=20 (RW = ⅔·I) · n=25/T=20 (RW = I/5) · n<1 (wie n=T) · n=6,6 (Rundung, Reviewfall) | Handrechnung linear | ✓ |
| Differenz-Kennzahlen | KW-Diff, dynamische Amortisation mit Interpolation (15,90 a bei I₀ 100 k€, Δ 8 k€/a, i 3 %), IRR = 4,96 % (unabhängige Nullstellensuche), Randfälle ohne Mehrinvestition | Kontrollrechnung | ✓ |
| BEHG + KWKG | BEHG steigt mit p_E; KWKG-Reihe: Deckel 3 500 Vbh/a, Kontingent 30 000 Vbh → 8 Jahre voll + Restjahr, Summe = Kontingentanteil; Split Eigen-/Einspeisesatz | KWKG-2020-Logik (Konzept 2.7) | ✓ |
| Tarifzonen | Referenzjahr 2026: W-HT 2 080 h · W-NT 2 288 h · S-HT 2 096 h · S-NT 2 296 h (Summe 8 760); Winter über Jahreswechsel | unabhängige Kalenderzählung | ✓ |
| Leistungspreis-Staffel | 250 kW, Grenze 100 kW, 60/40 €/kW → 12 000 €/a | Handrechnung | ✓ |

Hinweis zur Rundung der Ersatzjahre: C# `Math.Round(double)` rundet kaufmännisch
zur geraden Zahl („banker's rounding", z. B. 2,5 → 2). Das betrifft nur exakte
x,5-Nutzungsdauern und ist ohne praktische Auswirkung.

## 2. Abnahme-Checkliste für den Build-/Testlauf

**Build (einmalig):**
1. `dotnet restore` bzw. VS-Build — neue Pakete seit Phase 4: ClosedXML 0.105.1,
   SixLabors.Fonts 1.0.1 (gepinnt). Compilerfehler bitte einfach zurückmelden.
2. Erster Start: legt automatisch an — `Berichtskonfiguration`,
   `Tab_ProjektWirtschaftlichkeit`, `Tab_ErgebnisWirtschaftlichkeit`,
   `Tab_ErgebnisWirtSensitivitaet`, `Tab_ProjektTarif`, `Tab_ErgebnisStromMatrix`,
   `Tab_Kraftwerkspark` (vorbefüllt) sowie Spalten `carrier_id`/`Waermeproduktion`
   in den Ergebnis-Modultabellen. In Access gegenprüfen.

**Handgriffe im Designer (einmalig, Snippets im LIESMICH):**
3. Form_Start: Schaltfläche [Wirtschaftlichkeit] →
   `new Form_Wirtschaftlichkeit(m_ID_Projekt).ShowDialog();`
4. MDI-Menü „Projekt": „Als Variante speichern…" und „Varianten/Bericht".

**Funktionstest W1 (Basis):**
5. Varianten-Dialog → „Wirtschaftlichkeit…" → Parameter prüfen (i=3 %, T=20 a) →
   Berechnen. Erwartung: Kennzahlen je Szenario; Stamm = Referenz; fehlende
   Preise erscheinen als Begründung, nie als 0.
6. Kontrollwert: eine Variante mit nur einer Kostenposition (I₀, n=T) und
   bekannten Jahreskosten von Hand nachrechnen (KW = −I₀ − K/a(i,T) + Erlöse).

**Funktionstest W2:**
7. CO₂-Preis (z. B. 45 €/t) setzen → Zeile „CO₂-Abgabe BEHG" = CO₂-Brennstoff × Preis
   (nur fossile Träger; Netzstrom zählt nicht).
8. KWKG-Satz setzen → „KWKG-Erlös Jahr 1"; nach ⌈30 000/min(Vbh, 3 500)⌉ Jahren
   läuft der Bonus aus (im Excel-Blatt an den Szenarioblöcken nicht sichtbar,
   aber im Kapitalwert).
9. Bericht erzeugen → Sensitivitätstabellen (4 Parameter) und IRR-Zeile.

**Funktionstest W3:**
10. Tarifdialog: aktivieren, Preise füllen → Berechnen (dauert jetzt länger:
    In-Memory-Simulation je Projekt). Erwartung: Zeile „Stromkosten Tarif";
    Kontrollwert: Σ(Zonenmenge × Zonenpreis) + Leistungspreis-Staffel aus dem
    Excel-Matrixblock nachrechnen. Zonenstunden-Sollwerte (Standardzeiten):
    Winter-HT 2 080 h, Winter-NT 2 288 h, Sommer-HT 2 096 h, Sommer-NT 2 296 h.
11. Tarif aktiv, aber Preise 0 → Warnung im Dialog, Rechnung fällt mit Hinweis
    auf Flat zurück (kein „kostenloser Strom").
12. Kraftwerkspark wählen → Zeile „CO₂-Vermeidung vs. getrennt" + Bilanzabschnitt
    in Word/Excel. Nach einer Neusimulation ohne Neuberechnung muss die Bilanz
    mit Hinweis entfallen (keine gemischten Rechenstände).
13. Kostenmodul: neuen Kostenfaktor anlegen/löschen (B4); neuen Energieträger
    anlegen → `energy_price.leistungspreis` gefüllt (B5); Preis ändern und
    Formular ohne Speichern-Button schließen → Wert bleibt erhalten (B6);
    Träger löschen und Formular schließen → Träger bleibt gelöscht.

**Sprache:** UI auf Englisch stellen → Kapitel, Tabellenköpfe und Kennzahlen im
Bericht englisch, Zahlenformate en-US.

## 3. Nachtrag Phase 9 — KWKG 2025 (12.08.2026)

Die KWKG-2025-Logik wurde vor der Umsetzung ebenfalls numerisch verifiziert
(Python-Nachbau, 31 Prüfungen bestanden): Staffel-Lookup für alle Kalenderjahre
2019–2035 (5 000 → … → 2 500), Auszahlungsreihe Förderbeginn 2027 (Streckung auf
12 Jahre statt 9, Jahr 12 = Rest 1 300 Vbh, Summe = Kontingentanteil),
Bestandsanlage 2021 (historische Deckel), Deckel-Override, Negativpreis-Abschlag
(kontingentschonend, Summe bleibt erhalten) und die § 6-Fristenregeln
(31.12.2026 + Realisierung bis Ablauf des 4. Folgejahres).

**Zusätzliche Testschritte für den Build:** Parameterdialog → Stichtag/IBN setzen,
Deckel-Override 0 → KWKG-Reihe folgt der Staffel; Stichtag 2027 → Bonus 0 mit
Hinweis; Öl-BHKW mit IBN ≥ 2025 → Bonus 0; Σ Pel > 500 kW → Bonus 0;
Sensitivitätszeile „KWKG-Bonus entfällt (Regulierungsrisiko Novelle)";
`Tab_KWKG_Staffel` in Access: 8 Zeilen, pflegbar.

## 4. Bekannte, bewusst offene Punkte

Preisszenarien aus der `energy_price`-Historie (Stützstellen sind vorhanden),
positionsbezogene Zins-Overrides (Zinsreduktion je Gewerk), getrennte
KWKG-Strommengen aus der Simulation statt der min-Regel-Näherung, sowie die
Formel-Gegenprüfung an `VALERI_Vorlage_V7.xlsx` (Datei über die Z:-Brücke
weiterhin nicht lesbar — bei Bedarf nach C: kopieren oder im Chat anhängen).

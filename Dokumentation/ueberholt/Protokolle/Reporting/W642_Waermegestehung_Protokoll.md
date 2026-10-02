# W642 — Wärmegestehungskosten nur Wärmeerzeuger, Stromgutschrift und § 9b-Abzug, Herleitung „Menge × Preis" (Protokoll, nachgetragen 02.10.2026)

Statuszeile #642 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); die Welle lief ohne eigenes
Auftragspapier in der Sitzung „EPOS Plan Wirtschaftlichkeit" (Aufträge A–C vom 30.09.2026, Entscheidungen 1 und 2 vom
02.10.2026). Dieses Protokoll ist **nachgetragen** mit der Nachlese P646
([`P646_Auftrag_2026-10-02.md`](../Auftraege_Wirtschaftlichkeit_2026-09/P646_Auftrag_2026-10-02.md)); Quellen sind die
Statuszeile #642, die Kommentare in `EPOS.Kern/Allgemein/Wirtschaftlichkeit/Waermegestehung.cs` und `git show --stat 3588ebb38`.

## Anlass und Entscheide

Die Wärmegestehungskosten rechneten `(−Kapitalwert × a(i, T)) ÷ (Wärmebedarf × 1.000)` mit dem Kapitalwert des **ganzen**
Projekts: Haushaltsstrom, Photovoltaik, Stromspeicher und PV-Erlöse verschoben die Kennzahl, sobald ein Strombedarfsprofil
angelegt war (1024: 0,49953 €/kWh, 1030: 0,23979 €/kWh). Anwenderentscheide (Register **EZ‑20**, Wortlaut dort):

1. 30.09.2026: „Die Gestehungskosten sollten nur den Bedarf und die Kosten der den Anlagen zugeordneten enthalten."
2. 02.10.2026, Entscheidung 1: „Arbeitspreis bleibt, Stromgutschriftmethode".
3. 02.10.2026, Entscheidung 2: „Die Stromsteuer-Entlastung nach § 9b StromStG mindert die Stromsteuer-Entlastung durch das
   BHKW. Nur die zusätzliche Entlastung durch das BHKW wird angerechnet."

Dazu (Aufträge B und C vom 30.09.2026): die Nominalsumme der Gliederung mit Vorzeichen und Kurztext, die Herleitung
„Menge × Preis" je Energieträger unter den Energiekosten. Nach Entscheidung 2 belassen: Die Stromeinspeisung zählt nur mit
BHKW zur Wärme, kein Risikoabzug, Rollentarif zum Arbeitspreis, Herleitungszeilen nur auf der Seite (Word, Excel und
Platzhalter unverändert). Kein Schemaschritt, kein Rechenweg der Simulation, keine neue Basis.

## Regel

```
Gestehung [€/kWh] = (−KW_Wärme × a(i, T)) ÷ (Wärmebedarf [MWh/a] × 1.000)

KW_Wärme   = Kapitalwert (KapitalwertRechner) eines Zahlungsgerüsts allein der Wärmeerzeugung:
  Anlagen   Investition nach Zuschuss, Betrieb, Ersatz, Restwert der Positionen mit PositionZaehlt:
            Wärmepumpe, Heizkessel, Solarthermie, Pufferspeicher, BHKW immer; Photovoltaik und
            Stromspeicher nie; allgemeine Positionen nach dem Typ ihrer Anlage, ohne Anlage ja —
            die Stromeinspeisung nur mit BHKW im Projekt
  Energie   Σ Träger (WaermeArbeitEur + (Grundpreis + Leistungsanteil) × Anteil)
            Anteil = Wärmemenge ÷ Verbrauch aller Verbraucher des Trägers (1 ohne fremde Verbraucher)
            Brennstoff: Wärmemenge = Verbrauch; Strom: Wärmestrom (WP + Heizstab + Elektrokessel)
            × Arbeitspreis, ohne Anrechnung von PV-Eigenverbrauch
          + CO₂-Abgabe (ganz)
          − Stromgutschrift = BHKW-Eigenstrom [MWh/a] × 1.000 × Arbeitspreis des Netzträgers
  Erlöse    KWK-Einspeisung, KWKG-Zuschlag und Pauschale, Energiesteuer, Stromsteuer-Befreiung (Modus ERLOES)
          + STROMSTEUER_ENTLASTUNG_ENTGANGEN (negativ, nur bei produzierendem Gewerbe oder Land- und
            Forstwirtschaft): im Jahr t −Eigenstrom × § 9b-Satz(Förderbeginn + t − 1), jahresscharf,
            nicht mit p_E fortgeschrieben
  draußen   Haushaltsstrom und Stromverbraucher, Kältestrom und Kühlung, Photovoltaik, Stromspeicher,
            Risikoabzug
BHKW-Eigenstrom = StromMatrix.KwkEigenGesamtMWh; ohne Stundenreihen min(Erzeugung, Strombedarf − Reststrombedarf)
```

Kapitalwert und alle übrigen Kennzahlen bleiben projektweit und bitgleich. Fehlt dem Eigenstrom der Arbeitspreis, rechnet
die Kennzahl ohne Gutschrift und sagt es (`WIRT_GESTEHUNG_OHNE_STROMGUTSCHRIFT`).

## Code

- `faeae908` Kern und Seite: `Waermegestehung` (neu, die Regel an einer Stelle), `WirtschaftlichkeitCtrl.BaueWaermeEingabe`
  (dieselben Lesewege mit Filter), `KostenEmissionRechner` baut die Aufstellung je Träger (`EnergieTraegerNachweis`: Menge,
  Arbeitspreis, Grund-, Leistungspreis, CO₂-Schlüssel, Wärmemenge, Gesamtverbrauch), `WirtschaftlichkeitZeilen` die leisen
  Herleitungszeilen „Menge × Preis" (`Kennzahlen(…, mitHerleitung)`, nur die Seite), Kurztext `WIRT_GESTEHUNG_KURZTEXT` an
  Kennzahltafel, Word-Tafel und KI-Feld, Nachweisumschlag Fassung 12 (gespeicherte Läufe: „Menge × Preis liegt mit der
  nächsten Rechnung vor").
- `ae3def8d` Gliederung: Nominalsumme mit dem Vorzeichen des Barwerts, Tooltip `WIRT_GL_NOMINAL_TIPP`.
- `f223dcea` Wiki-Quelle Wirtschaftlichkeit (Anker `waermegestehung`).
- `6ae10112` § 9b-Abzug: `Entgangene9bEntlastungEur`, `Entgangene9bReihe`, Erlösreihe
  `KapitalwertRechner.ErloesReihe.STROMSTEUER_ENTLASTUNG_ENTGANGEN`, `Zerlegung` führt ihren Barwert unter Energie.
- `b4e17f97` Kurztext und Wiki nennen Gutschrift und § 9b-Abzug.
- `d405c6e6` Gestehungsformel in Rechenweg 08 und im Konzept (Kennzahltafel § 3.1).
- Merges `ea3cd7f8` (Parkzweig `wirt-gestehung`) und `3588ebb3` (`wirt-merge`, 27 Dateien, +1.975/−31).

## Tests und Zahlen

- `WaermegestehungTests` (neu, 21 Methoden), `WaermegestehungAnkerTests` (neu, 6 Fälle): 1019 alt = neu bitgleich
  (0,140677 €/kWh); 1024 0,49953 → 0,06162 €/kWh (Haushaltsstrom 365 MWh draußen, Wärmestrom 96,02 MWh, Gutschrift
  73,91 MWh × 0,46746 €/kWh = 34.549,97 €/a); 1030 0,23979 → 0,00684 €/kWh; mit umgeschalteter Unternehmensart 1024
  + 73,91 MWh × 20 €/MWh → 0,06541, 1030 + 432,3 MWh × 20 €/MWh → 0,00825 €/kWh; Photovoltaik bewegt die Kennzahl nicht,
  die Wärmezentrale schon. Fassungspins 11 → 12 nachgezogen.
- Gate #642 auf `27159b252` (mit #643): Kern 9 703, UI 7 206 von 7 207 (ein Lauf gestört durch einen Nebenbaum, einzeln
  grün), KiKern 549, SpeicherEngine 386, SpeicherPlanung 27, Wachen 35, Referenzlauf 16/16 PASS, 487/487 CSV byte-gleich,
  Windows-Schale 0 Fehler.

## Papiere

Wiki-Quelle Wirtschaftlichkeit (Anker `waermegestehung`), Rechenweg 08, Konzept § 3.1 (Kennzahltafel), Logbuch-Sätze unter
1.2.0.6. Register, Protokoll und die übrigen Konzeptstellen fehlten — nachgetragen mit P646 (EZ‑20).

## Offen (an P646 übergeben)

§ 9b-Sockel 250 €/a nicht berücksichtigt; Abzug nicht auf den Steueranteil gedeckelt; Sichtabnahme der Seite im
Windows-Build; Wiki-Upload gebündelt. Die fachliche Prüfung vom 02.10.2026 ergab sieben Befunde (Register EZ‑21).

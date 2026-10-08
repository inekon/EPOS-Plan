# Protokoll KK — die raumgeführte Kühlkurve und die Kühlübergabe je Zone (08.10.2026)

**Sitzung:** Gebäudesimulation, Statuszeile **#825**. Zweig der Integration `KK`, letzter Baucommit `7856be854`; Basis **R44**
`2026-10-08_R44_Kuehlkurve` (KK5b). **Entscheide:** E105 (Auftrag), E106 (Q-KK-1 bis Q-KK-7), E107 (Auslegungsweg). Entwurf
und Abschnitt „Wie gebaut“: [Entwurf KK](../../../aktuell/Gebaeudesimulation/2026-10-08_Entwurf_KK_Kuehlkurve.md) 6.2.

## 1 Auftrag

- **E105** (Anwender, 08.10.2026: „Starte im Anschluss die raumgeführte Kühlkurve“): Q-AK3K-3 als eigener Gegenstand KK.
- **E106**: Gebäude und Erzeuger gleiten (Q-KK-1 (a)), Kurve und Raumeinfluss nur AK3 (Q-KK-2 (a)), Kurve über die
  Außentemperatur plus Raumeinfluss (Q-KK-3 (a)), **Mehrzonenweg gehört dazu** (Q-KK-4 (b), abweichend von der Empfehlung),
  Vorgabewert der Stärke per Probe (Q-KK-5 (b)), Referenzprojekt als Kopie von 1058 plus ein Mehrzonen-Referenzprojekt
  (Q-KK-6 (a) erweitert), Kühlübergabe je Zone ab AK1 (Q-KK-7 (a)).
- **E107** (Anwender, 08.10.2026: „1, 2, 3 wählbar, Default 2“): Auslegungs-Außentemperatur der Kühlung über höchste Stunde,
  wärmstes Tagesmittel (Vorgabe) oder Eingabe.

## 2 Wellen

| Welle | Commits | Ergebnis und Zahlen |
|---|---|---|
| KK0 | Papiere | E106 eingetragen, Konzepte nachgezogen, Mehrzonenweg gelesen |
| KK1 | `4b6b63597`, `8d91b253b`, `1012a7af2`, `bc0ca883a` | Klasse `Kuehlkurve`, Kühlvorlauf als Jahresreihe, Rand liest `[h]`; Mindestabstand 2,5 K per Probe (Konvektor bei 30 °C: 2,0 K → 8,7 %, 2,5 K → 10,9 % der Nennleistung); `KuehlkurveTests` 7 |
| KK1b | `90c2175fe`, `0e1bfe747`, `0bbfdd953` | Auslegungsweg wählbar (E107), Mindestspanne 8 K aus acht Klimaregionen der Testdatenbank, Rückfall von Weg 3 auf Weg 2, Fußpunktregel; `KuehlkurveTests` 11/11 |
| KK2 | `07e441319`, `4582e6b84`, `03657f4e7` | Kühlkennlinie als Schar am gebrochenen Vorlauf, Kältemaschine je Stunde, Kälteschranke mit Vorlauf-Argument und Speicherregel, Kältekaskade am Stundenvorlauf; fester Vorlauf bitgleich; Orakel O1kk |
| KZ1 | `9a78570c7`, `da5502b1f`, `9f3c43848` | Kühlübergabe je Zone im Mehrzonenweg, Kühlkreis je Gebäude, Meldung `SIMENG_G6_AK1_IDEAL` neu gefasst; `ZonenKuehluebergabeTests` 15 |
| KK3 | `4ed08d2fb`, `a8c4a99fd`, `ff2941f5c`, `565de0c16` | Raumeinfluss im Kreis, Erzeuger gleitet; O2kk \|Δ\| ≤ 0,0003 K, höchstens 7 Durchläufe; Feldlauf 1058 mit k_K 1: 590 Kühlstunden, Vorlauf Mittel 18,13 °C, 38 h an der Grenze, Durchläufe 3,432 / 8; Vorgabewert 3 K/K (Messreihe k_K 0 bis 5) |
| KK4 | `391d426ba`, `fae5af820`, `7d299d455`, `6b3617bc3`, `ed3a99ebb`, `0c56775fb`, `5b78ba026` | Schemaschritt 202 (fünf Eingabespalten, Sicht mit 110 Spalten, drei Projektkennzahlen), Testdatenbank gehoben (91 303 936 Byte), Datenweg NULL-erhaltend, Katalogübernahme 96 → 101 Fachspalten, Gebäude- und Zonendialog (Eingabebilanz Zone 19 → 22) |
| KZ2 | `62da93155`, `38808ad3c`, `0a90ad33b` | Kühlkurve im Mehrzonenweg am niedrigsten Kühlsollwert; O3kz \|Δ\| ≤ 0,0004 K, höchstens 11 Durchläufe, 1317 Stunden mit Heiz- und Kühlzone; Feldlauf 1058 zweizonig: 594 Kühlstunden, Vorlauf Mittel 17,53 °C, Durchläufe 3,320 / 7 |
| KK5a | `3b3cb6ec8`, `d38dd1295`, `fd98d2581`, `0e08df6f8`, `7856be854` | Kernschalter entfallen, Kennzahlen im Lauf, Einzonengebäude allein mit Kälteseite im Kreis; RP-KK 1061 (EER 4,46 → 5,09, Überschreitung 42 → 32 h) und RP-KKZ 1062 mit Wachen |
| KK5b | Basis | R44 eingefroren (sechsundzwanzig Projekte; 1061 und 1062 neu, nicht in der CI-Auswahl) |
| KK6 | Papiere | Entwurf 6.2 und Logbuch-Entwürfe, Konzepte, Register, Wiki-Quellen, dieses Protokoll |

**Messreihe der Stärke an 1058** (Überschreitungsstunden / Kelvinstunden, EER der Kältekaskade): fester Vorlauf 34 h / 45,9 Kh,
5,238; k_K 1 34 h / 47,9 Kh, 5,145; k_K 3 32 h / 45,1 Kh, 5,089; k_K 5 32 h / 44,7 Kh, 5,048. Gewählt 3 K/K: kleinster runder
Wert mit dem Komfort des festen Vorlaufs.

## 3 Befunde

- **KK1b:** Das wärmste Tagesmittel liegt an vielen Orten unter dem Kühlsollwert; ohne Mindestspanne springt die Kurve am
  Sollwert. Daraus E107 und die Spanne 8 K.
- **KK3:** An 1058 liegt der Vorlauf auf der oberen Stützstelle; der Erzeuger kann nicht wärmer gleiten. RP-KK rechnet deshalb
  mit einem Kühlvorlauf der Wärmepumpe von 12 °C.
- **KZ1:** `Zonenmodell2K` hält Schritt K im festen Muster nicht; Stunden mit Kühlübergabe je Zone rechnen in der
  Gauß-Seidel-Schleife frei.
- **KZ2:** Das Vorlaufangebot der Kälteschranke kappte im Mehrzonen-Kühlkreis den abgesenkten Vorlauf auf den des vorigen
  Durchlaufs; behoben in `38808ad3c`.

## 4 Basis

**R44** `2026-10-08_R44_Kuehlkurve`: sechsundzwanzig Projekte, die vierundzwanzig aus R43 byte-gleich, neu 1061 (RP-KK) und
1062 (RP-KKZ). Einfrierregeln und Herleitung in `Referenzlaeufe/LIESMICH.md`.

## 5 Offen

- Sichtabnahme der Dialogfelder (Gebäude- und Zonendialog) unter Windows.
- Die führende Zone des Raumeinflusses im Mehrzonenweg hat keine Kennzahl.
- Die Rechenzeitreihe des Mehrzonenwegs ist mit der Probenaht entfallen; gemessen bleibt der Feldlauf aus KZ2.
- Ein zweiter Lauf des Werkzeugs Testdatenbankschema meldet Schritt 202 als offen.
- Wiki-Upload mit den zwei Logbuch-Sätzen (Entwurf KK Abschnitt 10) im nächsten gebündelten Upload.

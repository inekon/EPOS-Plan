# 01 · Investitionskosten BHKW

**Dialog:** `Form_KostenKomponente`, Reiter Investition · **Mockup:** `../Mockups/Dialog_Formel_Zahlenprobe.html#invest`
· **Norm:** DIN EN 17463, 6.1 · **Code:** `BetriebskostenCtrl.Betrag`, `InvestSummeFuer`, Lesepunkt
`Tab_ProjektWerte` mit `KategorieID = 1` · **Konzept:** § 3.2, § 3.3

## Was der Dialog zeigt

Raster mit einer Zeile je Position. Die Spalte **Menge** trägt unter dem Wert eine Herleitungszeile
(Monospace): woher die Menge kommt — „Baugrößensumme", „Basis: Hauptpositionen", „Stufe: Anlage
BHKW 1". Die Spalte **Runde** nennt die Kaskadenrunde, in der die Zeile rechnet. Die Summenzeile
nennt drei Beträge: Investition brutto, Zuschuss, I₀.

| Position | Kostenart | Bemessung | Satz | Menge | Betrag | Runde |
|---|---|---|---|---|---|---|
| BHKW-Modul (Hauptposition) | ANSCHAFFUNG | € / kW elektrisch | 653,60 | 26,00 kW — Baugrößensumme | 16.993,60 | 1 |
| Montage und Inbetriebnahme | ANSCHAFFUNG | % der Erzeugerkosten | 5,00 | 16.993,60 € — Basis: Hauptpositionen | 849,68 | 2 |
| Hydraulik und Einbindung | ANSCHAFFUNG | Betrag | — | — | 13.000,00 | 1 |
| Planung und Genehmigung | ANSCHAFFUNG | % der Investition | 10,00 | 30.843,28 € — Stufe: Anlage BHKW 1 | 3.084,33 | 3 |
| Zuschuss BAFA | ZUSCHUSS | Betrag | — | — | 6.000,00 | — |
| **Summe** | | Investition brutto 33.927,61 € · abzüglich Zuschuss 6.000,00 € | | | **27.927,61** | I₀ |

**Warnband:** „Der Zuschuss mindert I₀, nicht die Basis der Prozentpositionen. Ersatzbeschaffung und
Restwert rechnen weiter mit dem Bruttobetrag."
**Infozeile:** Reihenfolge der Mengenermittlung — gepflegter Szenariowert → Baugröße aus der
Gerätewelt → gespeicherte Menge. Passt die Kostenart nicht zum Gewerk, bleibt die Zeile leer; es wird
nie eine Ersatzzahl gebildet.

*Die Investitionszahlen dieses Beispiels sind der belegten Kaskadenprobe des Projekts 1042
nachgebildet (26 × 653,60; 5 %; 13.000; 10 %) — dort ergab dieselbe Kette ein Delta von genau
+20.927,61 €.*

## Berechnungsgrundlage

```
Runde 1 — direkte Arten (Mengenkette)
  Betrag = Menge × Satz
  Menge: Szenariowert (VALERI-Vorrang) → BaugroesseSumme (Gerätewelt) → Tab_ProjektWerte.Menge
  Art ↔ Gewerk gekreuzt geprüft: falsches Paar ⇒ null, keine Fantasiezahl

Runde 2 — PROZENT_ERZEUGERKOSTEN
  Basis  = Σ Betrag der Runde-1-Zeilen mit IsMainComponent = TRUE
           UND Kostenart ≠ ZUSCHUSS UND gleiche KomponentenID
  Betrag = Basis × Satz / 100

Runde 3 — PROZENT_INVESTITION, stufig (erste nicht-leere Stufe gewinnt)
  1. Anlage (ID_Anlage > 0, Summe ≠ 0)
  2. Komponente (KomponentenID > 0, Summe ≠ 0)
  3. Projekt (alle)
  4. Basis 0 → null → Rückfall auf die Mengenkette
  Betrag = Basis × Satz / 100

Zuschuss — NACH der Positionsschleife
  I₀_brutto        = Σ Betrag aller Nicht-Zuschuss-Positionen mit StartJahr = 0
  Zuschuss         = min( Σ Zuschusszeilen , I₀_brutto )      ← Klemme
  Zuschussüberhang = Σ Zuschusszeilen − Zuschuss              ← nur Ausweis + Hinweis
  I₀               = I₀_brutto − Zuschuss
```

Mengenquellen der direkten Arten: `EUR_PRO_KW_ELEKTRISCH` → `Tab_BHKW.Pel` · `EUR_PRO_KW_LEISTUNG`
→ `Tab_Heizkessel.Ptherm` · `EUR_PRO_KW_HEIZLEISTUNG` → `Tab_WP.Nennleistung` · `EUR_PRO_KWP` →
`Tab_Energieanlagen.PV_Leistung` (⚠ I-1, siehe `03`) · `EUR_PRO_KWH_KAPAZITAET` →
`Tab_Stromspeicher.Energie` · `EUR_PRO_M2_KOLLEKTOR` → Aperturfläche × Modulanzahl. Pufferspeicher
liefert immer null (ohne Temperaturpaar keine belastbare kWh).

Zuschuss: Kennzeichen `Kostenart = "ZUSCHUSS"` (getrimmt, ohne Groß-/Kleinschreibung), Erfassung
positiv. Zuschusszeilen erzeugen keine Ersatzbeschaffung, keinen Restwert und stehen in keiner
Kaskadenbasis; `Ergebnis.Investition` bleibt brutto, nur I₀ ist netto.

## Berechnungserläuterung am Beispielprojekt

| Schritt | Rechnung | Ergebnis | Anmerkung |
|---|---|---|---|
| R1 BHKW-Modul | 26,00 × 653,60 | 16.993,60 € | Hauptposition — Basis für Runde 2 |
| R1 Hydraulik | Betrag, fest | 13.000,00 € | keine Hauptposition, zählt nicht zur Erzeugerkosten-Basis |
| R2 Montage 5 % | 16.993,60 × 5 / 100 | 849,68 € | Basis nur die Hauptposition |
| **Basis für Runde 3** | 16.993,60 + 849,68 + 13.000,00 | **30.843,28 €** | alle Zeilen der Anlage, ohne Zuschuss |
| R3 Planung 10 % | 30.843,28 × 10 / 100 | 3.084,33 € | Stufe „Anlage" greift |
| **Investition brutto** | 16.993,60 + 849,68 + 13.000,00 + 3.084,33 | **33.927,61 €** | Ausweis; Basis für Ersatz und Restwert |
| Zuschuss | min(6.000,00 ; 33.927,61) | − 6.000,00 € | Klemme greift nicht |
| **I₀** | 33.927,61 − 6.000,00 | **27.927,61 €** | geht mit negativem Vorzeichen in Periode 0 |

Der Kaskadenfaktor der beiden Prozentpositionen beträgt `1 + 0,05 + 0,10 × 1,05 = 1,155`. Die Kaskade
wirkt multiplikativ — jede Runde vergrößert die Basis der nächsten; deshalb bewegt eine scheinbar
kleine Prozentposition am Ende einen fünfstelligen Betrag.

## Befunde und offene Punkte

| Nr. | Befund | Behandlung im Entwurf |
|---|---|---|
| ⚠ I-2 | Abgeleitete Bemessung ohne Satz ⇒ 0 €, nicht der erfasste Betrag | Zeile zeigt „—" mit Herleitung „Satz fehlt" |
| ⚠ I-3 | Runde 3 ist reihenfolgeabhängig — zwei `PROZENT_INVESTITION`-Zeilen, die zweite rechnet die erste ein; ohne ORDER BY entscheidet ACE | Spalte „Runde" macht die Reihenfolge sichtbar; ORDER BY ist in der Umsetzung nachzuziehen |
| I-5 | Vergleichsstrenge uneinheitlich: ZUSCHUSS ohne, `PROZENT_*` mit Groß-/Kleinschreibung | vereinheitlichen |
| I-6 | Nicht migrierte Datenbank: keine Kaskade, keine Zuschusserkennung | Migrationsprüfung beim Öffnen |

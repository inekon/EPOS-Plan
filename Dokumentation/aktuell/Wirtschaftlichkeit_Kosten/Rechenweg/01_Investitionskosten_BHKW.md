# 01 · Investitionskosten BHKW

**Dialog:** `KostenKomponenteDialog` (`EPOS.UI/Dialoge/Kosten/`), Optionsgruppe „Investitionskosten" ·
**Mockup:** `../../Mockups/Dialog_Formel_Zahlenprobe.html#invest` · **Norm:** DIN EN 17463, 6.1 · **Code:**
`BetriebskostenCtrl.Betrag`, `InvestKaskade`, `InvestSummeFuer`, Lesepunkt `Tab_ProjektWerte` mit
`KategorieID = 1` · **Konzept:** § 3.2, § 3.3

## Was der Dialog zeigt

Ein Fenster für beide Kategorien: Klappliste „Komponente:" (im Projektmodus die Anlage), Optionsgruppe
„Betriebskosten / Investitionskosten", Reiter „Kosten Invest/Betrieb" und — bei Blockheizkraftwerk und
Photovoltaik — „Ertrag/Bonus"; im Stammkontext des Katalogs dazu die Variantenzeile. Das Zeilenraster trägt
je Position eine Zeile mit sieben Spalten: Aktionen (✏️ Zeileneditor, 🗑️ Löschen) · Position (Textfeld) ·
Bemessung (Klappliste, je Gewerk gefiltert — beim Blockheizkraftwerk fester Betrag, % der Investition, % der
Erzeugerkosten, je kW elektr. Leistung, je kW Heizleistung, je kW elektrisch) · Satz (Zahlenfeld, die Einheit
folgt der Bemessung) · Betrag netto [€] (gerechnet, nie eingebbar; 🔗 bei absoluter Bemessung: Satz = Betrag) ·
Nutzungsdauer [a] (Zahlenfeld) · Worst/Best (±, nur im Projektmodus). Der Werkzeugtipp des Betrags nennt Satz
und Bezugsgröße („653,60 €/kW × 300,00 kW"); fehlt die Bezugsgröße, trägt der Betrag ⚠ und unter dem Raster
steht der Grund („kein Gerät mit dieser Baugröße im Projekt", „keine Investitionskosten für diese Anlage
erfasst"). Summenfuß: „Summe Investitionskosten netto: 234.772,40 €" (Erlös- und Zuschusszeilen negativ) und
„Summe brutto: 279.379,16 € (Umsatzsteuer 19 % aus dem Katalog)". Knöpfe „+ Position hinzufügen",
„Aus Vorlage übernehmen…", „Positionskatalog…"; Fußleiste Abbrechen · Speichern · OK. Kostenart,
Erlös-/Zuschusskennzeichen und Empfehlungsbereich pflegt der Zeileneditor „Position bearbeiten". Was das Mockup
darüber hinaus zeigt, steht im Anhang Umsetzungsstand: Herleitungszeile mit Kaskadenrunde (U28), dreiteiliger
Summenfuß (U29), Gruppe „Ersatz und Restwert" (U30), Knopf „Nutzungsdauern vorbelegen…" (U8).

| Position | Kostenart | Bemessung | Satz | Bezugsgröße (Werkzeugtipp) | Betrag | Nutzungsdauer | Runde |
|---|---|---|---|---|---|---|---|
| BHKW-Modul (Hauptposition) | kapitalgebunden | je kW elektr. Leistung | 653,60 €/kW | 300,00 kW — `Tab_BHKW.Pel` | 196.080,00 | 15 | 1 |
| Montage und Inbetriebnahme | kapitalgebunden | % der Erzeugerkosten | 5,00 % | 196.080,00 € — Hauptpositionen | 9.804,00 | 15 | 2 |
| Hydraulik und Einbindung | kapitalgebunden | fester Betrag | 13.000,00 € | — (Satz = Betrag) | 13.000,00 | 20 | 1 |
| Planung und Genehmigung | kapitalgebunden | % der Investition | 10,00 % | 218.884,00 € — Stufe: Anlage BHKW 1 | 21.888,40 | — | 3 |
| Zuschuss | Zuschuss, Erlös/Zuschuss an | fester Betrag | 6.000,00 € | — | 6.000,00 | — | — |
| **Summe** | | Investition brutto 240.772,40 € · abzüglich Zuschuss 6.000,00 € | | | **234.772,40** | | I₀ |

**Zuschuss:** Der Zuschuss mindert I₀, nicht die Basis der Prozentpositionen; Ersatzbeschaffung und Restwert
rechnen weiter mit dem Bruttobetrag. **Mengenermittlung** in Runde 1: gepflegter Szenariowert → Baugröße aus der
Gerätewelt → gespeicherte Menge. Passt die Bemessung nicht zum Gewerk, steht sie nicht in der Klappliste; fehlt
die Größe, bleibt der Betrag mit ⚠ und Grund stehen — es wird nie eine Ersatzzahl gebildet.

*Die Sätze dieses Beispiels sind der belegten Kaskadenprobe des Projekts 1042 nachgebildet
(653,60 €/kW; 5 %; 13.000 €; 10 %); dort ergab dieselbe Kette an einer Baugröße von 26,00 kW ein
Delta von genau +20.927,61 €. Das Beispielprojekt rechnet durchgängig mit 300 kW.*

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
`Tab_Stromspeicher.Energie` · `EUR_PRO_M2_KOLLEKTOR` → Aperturfläche × Modulanzahl ·
`EUR_PRO_KW_LEISTUNG` am Pufferspeicher → `Tab_Pufferspeicher.Gesamtvolumen` [l], der Satz ist
damit ein €/Ltr.-Satz. `EUR_PRO_KWH_KAPAZITAET` liefert am Pufferspeicher null: Ohne
Temperaturpaar gibt es dort keine belastbare kWh.

Zuschuss: Kennzeichen `Kostenart = "ZUSCHUSS"` (getrimmt, ohne Groß-/Kleinschreibung), Erfassung
positiv. Zuschusszeilen erzeugen keine Ersatzbeschaffung, keinen Restwert und stehen in keiner
Kaskadenbasis; `Ergebnis.Investition` bleibt brutto, nur I₀ ist netto.

## Berechnungserläuterung am Beispielprojekt

| Schritt | Rechnung | Ergebnis | Anmerkung |
|---|---|---|---|
| R1 BHKW-Modul | 300,00 × 653,60 | 196.080,00 € | Hauptposition — Basis für Runde 2 |
| R1 Hydraulik | Betrag, fest | 13.000,00 € | keine Hauptposition, zählt nicht zur Erzeugerkosten-Basis |
| R2 Montage 5 % | 196.080,00 × 5 / 100 | 9.804,00 € | Basis nur die Hauptposition |
| **Basis für Runde 3** | 196.080,00 + 9.804,00 + 13.000,00 | **218.884,00 €** | alle Zeilen der Anlage, ohne Zuschuss |
| R3 Planung 10 % | 218.884,00 × 10 / 100 | 21.888,40 € | Stufe „Anlage" greift |
| **Investition brutto** | 196.080,00 + 9.804,00 + 13.000,00 + 21.888,40 | **240.772,40 €** | Ausweis; Basis für Ersatz und Restwert |
| Zuschuss | min(6.000,00 ; 240.772,40) | − 6.000,00 € | Klemme greift nicht |
| **I₀** | 240.772,40 − 6.000,00 | **234.772,40 €** | geht mit negativem Vorzeichen in Periode 0 |

Der Kaskadenfaktor auf die Hauptposition beträgt `1 + 0,05 + 0,10 × 1,05 = 1,155`; auf die übrigen
Runde-1-Zeilen wirkt allein die dritte Runde (× 1,10). Die Kaskade
wirkt multiplikativ — jede Runde vergrößert die Basis der nächsten; deshalb bewegt eine scheinbar
kleine Prozentposition am Ende einen fünfstelligen Betrag.

## Befunde und offene Punkte

| Nr. | Befund | Behandlung im Entwurf |
|---|---|---|
| ⚠ I-2 | Abgeleitete Bemessung ohne Satz ⇒ 0 €, nicht der erfasste Betrag | Zeile zeigt „—" mit Herleitung „Satz fehlt" |
| ⚠ I-3 | Runde 3 ist reihenfolgeabhängig — zwei `PROZENT_INVESTITION`-Zeilen, die zweite rechnet die erste ein; ohne ORDER BY entscheidet ACE | Spalte „Runde" macht die Reihenfolge sichtbar; ORDER BY ist in der Umsetzung nachzuziehen |
| I-5 | Vergleichsstrenge uneinheitlich: ZUSCHUSS ohne, `PROZENT_*` mit Groß-/Kleinschreibung | vereinheitlichen |
| I-6 | Nicht migrierte Datenbank: keine Kaskade, keine Zuschusserkennung | Migrationsprüfung beim Öffnen |

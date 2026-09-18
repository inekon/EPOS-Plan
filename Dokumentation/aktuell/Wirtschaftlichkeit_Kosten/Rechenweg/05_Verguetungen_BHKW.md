# 05 · Vergütungen BHKW

**Dialog:** `BhkwWirtschaftlichkeitDialog` (`EPOS.UI/Dialoge/Wirtschaftlichkeit/`) — BW9 (Konzept § 2.2) · **Mockup:**
`../Mockups/Dialog_Formel_Zahlenprobe.html#bhkw` · **Recht:** § 2 Nr. 16 und Nr. 20, § 6 Abs. 3,
§ 7, § 8 KWKG 2025 · §§ 53, 53a, 54 EnergieStG · § 9 Abs. 1 Nr. 3, § 9b StromStG · **Code:**
`KwkgAnlagenCtrl`, `WirtschaftlichkeitCtrl` (Erlösreihen `KWKG_ZUSCHLAG`, `KWKG_PAUSCHALE`,
`ENERGIESTEUER_GUTSCHRIFT`, `STROMSTEUER_BEFREIUNG`, `STROMSTEUER_ENTLASTUNG`) · **Konzept:** § 2.2,
§ 3.6, § 3.7, § 3.8, § 3.9

Die dichteste Kategorie: vier Rechtsgrundlagen, drei verschiedene Strommengen und eine Staffel, die
marginal rechnet statt klassenweise. Das Formular führt die acht Gruppen des Dialogs in seiner Reihenfolge;
die Sätze für Einspeisung und Eigenstrom sind Zahlenfelder bei der gewählten Anlage, unter jedem steht seine
Herkunft. Wahl und Herleitung stehen in einer Überlagerung, die mit einem Knopf übernimmt. Drei Tiefen, drei
Fragen: Vorschau (wie viel?), Feld mit Herkunftszeile (woher?), Überlagerung (wie entstanden?).

## Was der Dialog zeigt

**Gruppe Anlagen** (`BHW_G1`) — Tabelle je BHKW-Modul mit den Spalten Wahl · Projekt · Anlage · P_el [kW] ·
Brennstoff · Stichtag · Inbetriebnahme · Anlagenart; Warnzeilen nur, wenn sie zutreffen (Ausschreibung § 8a über
500 kW, Stromsteuerbefreiung entfällt über 2.000 kW, Heizöl-Ausschluss ab Inbetriebnahme 2025).

**Gruppe Angaben der gewählten Anlage** (`BHW_G1B`) — die zwölf Felder des Dialogs mit ihren Ressourcentexten;
editierbar sind Zahlen- und Datumsfelder, die vier Wahlfelder stehen als Anzeigezeile und werden in der
Überlagerung gewählt (U22):

| Feld (Ressourcentext) | Baustein | Beispiel | Herkunftszeile darunter |
|---|---|---|---|
| Stichtag (Bestellung/Genehmigung): | Datumsfeld | 14.03.2026 | — |
| Inbetriebnahme: | Datumsfeld | 01.10.2026 | — |
| Anlagenart: | Anzeige (Klappliste im Dialog) | neue Anlage (§ 8 Abs. 1) | — |
| Eigenstrom nach § 6 Abs. 3: | Anzeige (Klappliste im Dialog) | Nr. 2 — Kundenanlage / geschl. Netz | — |
| **Satz Einspeisung [ct/kWh] (0 = kein Zuschlag):** | Zahlenfeld, ct/kWh | **5,5667** | Einspeisung 5,5667 ct/kWh — § 7 Abs. 1 KWKG 2025, Stichtagsjahr 2026: 50 × 8,00 + 50 × 6,00 + 150 × 5,00 + 50 × 4,40 = 1.670 ÷ 300 · Vorschlag gilt |
| **Satz Eigenstrom [ct/kWh] (0 = kein Zuschlag):** | Zahlenfeld, ct/kWh | **2,4167** | Eigenstrom 2,4167 ct/kWh — § 7 Abs. 2 mit § 6 Abs. 3 Nr. 2: 50 × 4,00 + 50 × 3,00 + 150 × 2,00 + 50 × 1,50 = 725 ÷ 300 · Vorschlag gilt |
| Vbh-Kontingent [h] (0 = nach § 8 abgeleitet): | Zahlenfeld, h | 30.000 | Kontingent 30.000 Vbh — § 8 Abs. 1, neue Anlage · Vorschlag gilt |
| Vbh-Jahresdeckel [h/a] (0 = Staffel): | Zahlenfeld, h/a | 0 | Staffel § 8 Abs. 4 · 3.300 (2026) · 3.100 · 2.900 · 2.700 · ab 2030 2.500 (U25) |
| Anteil Neuherstellungskosten [%] (§ 8 Abs. 2/3): | Zahlenfeld, % | 0,0 | nur modernisiert oder nachgerüstet; 0 = nicht gepflegt |
| Energiesteuerentlastung (Anlage): | Anzeige (Klappliste im Dialog) | (Projektwert) → § 53a Abs. 5 | — |
| Brennstoff auf Strom/Wärme (Anlage): | Anzeige (Klappliste im Dialog) | (Projektwert) → voller BHKW-Brennstoff | — |
| Hilfsenergieanteil [% des Endenergiebedarfs] (0 = keine): | Zahlenfeld, % | 2,0 | Vorschlag BHKW 2–4 %. Bemessen wird am Endenergiebedarf (Brennstoff) dieser Anlage — nicht an den Kosten. |

**Das Feld ist der Satz.** Der Rechenkern liest `SatzEinspCt` und `SatzEigenCt` der Anlage ohne Rückfall
(`a.SatzEinspCt ?? 0`); leer oder 0 heißt „kein Zuschlag", die Vorschau zeigt dann 0 €. Wer den Satz ändert, tippt
ihn ins Feld oder wählt ihn in der Überlagerung; weicht das Feld vom Katalog ab, heißt die Zeile darunter
„eigener Wert 6,00 — Vorschlag 5,5667 ct/kWh — § 7 Abs. 1 …" (U23), der Weg zurück ist der Knopf „Vorschlag
übernehmen" in der Überlagerung. Der Knopf „Sätze und Herkunft…" steht am Kopf der Gruppe. Das Warnband zum
Deckelanteil („Die Anlage läuft 5.500 h/a, vergütet werden im ersten Jahr aber nur 3.300 h — 60 % der
Erzeugung; weil der Deckel jährlich fällt, reicht das Kontingent über 12 Kalenderjahre") ist ein Vorschlag (U25),
ebenso die vierstellige Anzeige der Sätze — der Dialog zeigt zwei Nachkommastellen, 5,5667 erscheint als 5,57 (U26).

**Gruppe Projektweite KWK-Angaben** (`BHW_G2`) — Einspeisevergütung KWK-Strom [€/kWh] 0,0500 · Abschlag
Negativstunden [%] 0,0 · Schalter Pauschale § 9 KWKG (nur bis 2 kWel, einmalig) · Stichtag (Bestellung/Genehmigung,
§ 6) 14.03.2026 · Förderbeginn (Startjahr der Reihen) 01.10.2026; darunter die beiden Hinweiszeilen
`BHW_P_EINSP_KWK_HINWEIS` (der Satz stellt auch v_bhkw der Speicherwelt; 0 = nicht gepflegt, dann gilt der PV-Satz)
und `BHW_P_NUR_PROJEKTWEIT` (Satz, Kontingent, Deckel, Anlagenart, Tatbestand und Kostenanteil stehen an der Anlage).

**Gruppe Energiesteuer (Projektvorgabe)** (`BHW_G3`) — Energiesteuerentlastung: § 53a Abs. 5 EnergieStG (1135) ·
Brennstoff auf Strom/Wärme: voller BHKW-Brennstoff (§ 53 Abs. 2) — beide Anzeige, Wahl in der Überlagerung über den
Knopf „Wahl und Herkunft…" — · Jahresnutzungsgrad [%] (0 = nicht erfasst) 83,0 (Zahlenfeld, Projektgröße, Schwelle
des § 53a 70 %); darunter die Herkunftszeile des zuletzt gebuchten Laufs (`ENERGIEST_53A5_ERDGAS = 4,42 €/MWh,
gültig ab 2024 (GESICHERT) — EnergieStG § 53a Abs. 5 …`). Einen Steuersatz von Hand gibt es nicht.

**Gruppe Stromsteuer (Projektvorgabe)** (`BHW_G4`) — Unternehmensart: produzierendes Gewerbe (Anzeige) · Schalter
Räumlicher Zusammenhang (4,5 km) gegeben ✓ · Schalter Hocheffizienz nachgewiesen ✓ · Modus § 9 Abs. 1 Nr. 3: Ausweis
(nicht im Kapitalwert) (Anzeige; Spalte `Stromst_Befreiung_Modus`, Vorgabe AUSWEIS) · Modushinweis · Knöpfe
„Strombezug…" und „BHKW-Tarif…", die nur schreiben, wenn der Arbeitsstand vom geladenen Stand abweicht.

**Gruppe Kohärenzprüfung (Energie- und Stromsteuer)** — die Zeilen des zuletzt gebuchten Laufs, ohne
Rechenwirkung: im Beispiel „✓ Energiesteuer: Wahl und Preisanteil stimmen überein (BHKW 1)."; ohne Auffälligkeit
„Keine Auffälligkeit im zuletzt gebuchten Lauf." Weitere Zeilen nur bei Abweichung (Entlastung ohne Steuer im
Preis, Steuer im Preis ohne Entlastung, Satz gegen Katalog, § 53 neben § 54, Doppelzählung § 9 Nr. 3 im Modus Erlös).

**Gruppe Hilfsstrom** (`BHW_G5`) — Basiszeile, Mengenkette aus dem Lauf („Stromerzeugung brutto 1.650,000 MWh/a −
Hilfsstrom 86,842 MWh/a = Nettostromerzeugung 1.563,158 MWh/a · davon Eigenverbrauch 1.094,211 MWh/a, Einspeisung
468,947 MWh/a") und die Doppelpflege-Warnung („Hilfsenergie doppelt gepflegt … BHKW 1 führt einen Hilfsenergieanteil
von 2,00 % und zugleich eine aktive Hilfsenergie-Kostenposition …").

**Gruppe Vorschau — zuletzt gebuchter Lauf** (`BHW_G6`) — die Erlösrubrik, siehe unten. Fußleiste: Abbrechen ·
Speichern (schreibt erst die Anlagenzeilen, dann die Projektvorgaben; Abbrechen und Esc schreiben nichts).

**Mengentafel** — welche Vorschrift rechnet mit welcher Menge. Das ist die wichtigste neue
Darstellung dieser Kategorie:

| Menge | MWh/a | verwendet von | Grund |
|---|---|---|---|
| Stromerzeugung brutto | 1.650,0 | § 9 Abs. 1 Nr. 3 · CO₂-Grenzwert · Vollbenutzungsstunden | an den Generatorklemmen gemessen |
| − Hilfsstrom (2,0 % × 4.342,1) | − 86,8 | — | Neben- und Hilfsanlagen, § 2 Nr. 20 KWKG |
| **= Nettostromerzeugung** | **1.563,2** | KWKG-Zuschlag § 7 | = KWK-Strom, § 2 Nr. 16 Fall 1 |
| davon Eigenverbrauch (70 %) | 1.094,2 | Zuschlag Abs. 2 | |
| davon Einspeisung (30 %) | 469,0 | Zuschlag Abs. 1 · Einspeiseerlös | |
| Eigenverbrauch brutto (70 %) | 1.155,0 | § 9 Abs. 1 Nr. 3 StromStG | kein Netting — andere Vorschrift |

Das Netting wirkt **ausschließlich** auf die KWKG-Zuschlagsmengen. Stromsteuer, CO₂-Grenzwert und
Vollbenutzungsstunden bleiben brutto — keine Inkonsistenz, sondern Folge davon, dass nur § 7 KWKG
auf „KWK-Strom" im Sinne des § 2 Nr. 16 zahlt.

**Überlagerung „Sätze und Herkunft — BHKW 1"** (U22) — die Knöpfe „Sätze und Herkunft…" (Angaben der gewählten
Anlage) und „Wahl und Herkunft…" (Energiesteuer) öffnen sie über dem Formular; nach dem Hausmuster trägt sie Titel
und Kreuz, der Inhalt hat keinen zweiten Kopf. Drei Gruppen, **ein** Knopf „Übernehmen", der in die Felder des
Formulars schreibt — gespeichert wird erst mit „Speichern":

1. **KWK-Zuschlag — diese Anlage.** Anlagenart (§ 8) mit der Kontingentstufe je Option (neu → 30.000 ·
   modernisiert → 15.000 ab 25 % / 30.000 ab 50 % · nachgerüstet → 10.000 / 15.000 / 30.000) und dem
   Feld Anteil Neuherstellungskosten; Eigenstrom nach § 6 Abs. 3 mit dem Satz je Option (keiner → 0 ·
   Nr. 1 → nicht möglich bei 300 kW · Nr. 2 → 2,4167 · Nr. 3 → 3,9683 ct/kWh); KWK-Strom Fall 1
   (Nettostromerzeugung) oder Fall 2 (Vorrichtung zur Abwärmeabfuhr, Stromkennzahl σ, leer = 0,845 aus
   P_el ÷ P_th). Darunter die Tafel **Größe · Vorschlag · Herkunft · eigener Wert · gilt** — „eigener Wert" ist
   je Größe ein Eingabefeld mit dem Platzhalter „leer = Vorschlag"; neben einem gesetzten Wert steht der Knopf
   „Vorschlag übernehmen", der das Feld leert:

   ```
   Satz Einspeisung  5,5667 ct/kWh  KWKG_ZUSCHLAG_EINSPEISUNG_* · ab 2020 · § 7 Abs. 1:
                                    50 kW × 8,00 + 50 × 6,00 + 150 × 5,00 + 50 × 4,40 = 1.670 ÷ 300
   Satz Eigenstrom   2,4167 ct/kWh  KWKG_ZUSCHLAG_EIGEN_N2_* · ab 2020 · § 7 Abs. 2:
                                    50 × 4,00 + 50 × 3,00 + 150 × 2,00 + 50 × 1,50 = 725 ÷ 300
   Vbh-Kontingent    30.000 h       KWKG_VBH_NEUANLAGE · ab 2020 · § 8 Abs. 1
   Jahresdeckel      Staffel        KWKG_VBH_JAHRESDECKEL · je Kalenderjahr · § 8 Abs. 4
   ```

   Wirkung Jahr 1 (2026): 469,0 MWh × 5,5667 ct × 0,600 + 1.094,2 MWh × 2,4167 ct × 0,600 =
   **31.530,8 €**. Leer heißt: Der Vorschlag gilt und wird beim Übernehmen in das Feld des Formulars
   geschrieben; ein eigener Wert gilt dauerhaft — auch wenn der Katalog später einen anderen Vorschlag liefert —,
   das Feld trägt ihn, und die Herkunftszeile darunter nennt beide Werte („eigener Wert 6,00 — Vorschlag 5,5667").
2. **Energiesteuer.** Projektvorgabe für alle Anlagen oder nur diese Anlage; Entlastung keine → 0 ·
   § 53 (Formular 1131) → 5,50 €/MWh, 26.384,3 €/a · § 53a Abs. 5 (Formular 1135) → 4,42 €/MWh,
   21.203,4 €/a · § 54 (Formular 1450) → 1,38 €/MWh − 250 €, 6.370,1 €/a, nur produzierendes Gewerbe;
   Brennstoffaufteilung nur bei § 53 (voller Brennstoff, § 53 Abs. 2 — oder energetisch × 0,458 →
   12.082 €/a, bewusste Untergrenze). Herkunftszeile `ENERGIEST_53A5_ERDGAS = 4,42 €/MWh, gültig ab 2024
   (GESICHERT) — EnergieStG § 53a Abs. 5`; Menge 4.342,1 MWh (H_i) × 11,6 ÷ 10,5 = 4.797,2 MWh (H_s),
   nur der Brennstoff dieser Anlage. Kein Satz von Hand: Der Katalog liefert ihn jahresscharf.
3. **Stromsteuer — Projekt.** Unternehmensart mit Wirkung je Option (kein produzierendes Gewerbe →
   § 9b und § 54 entfallen · produzierendes Gewerbe → § 9b 20,00 €/MWh · Land- und Forstwirtschaft →
   ebenso), Hocheffizienz, räumlicher Zusammenhang ≤ 4,5 km, Modus § 9 Abs. 1 Nr. 3 Ausweis/Erlös.
   Herkunft `STROMST_ENTLASTUNG_9B = 20,00 €/MWh, ab 2026`, `STROMST_SOCKELBETRAG_9B 250 €/a`,
   `STROMST_REGELSATZ = 20,50 €/MWh, ab 2026`; Wirkung 4.750,0 € (Netzbezug 250,0 MWh, Lauf „Beide
   Anlagen") und 23.677,5 € (Ausweis).

**Steuern des Projekts** — Satz, Menge und Bedingungen je Vorschrift, im Papier als Tafel (die Wahl steht in den
Gruppen Energiesteuer und Stromsteuer, Satz und Herkunft in der Überlagerung):

| Vorschrift | Satz | Menge | Jahr 1 (2026) | Herkunft · Bedingungen |
|---|---|---|---|---|
| Energiesteuer § 53a Abs. 5 | 4,42 €/MWh | 4.797,2 MWh (H_s) | 21.203,4 € | Katalog Erdgas ab 2024, Formular 1135 · Nutzungsgrad 83 % ≥ 70 % ✓ · Brennwertmenge 4.342,1 × 11,6 ÷ 10,5 · nur Brennstoff des BHKW |
| Stromsteuer-Entlastung § 9b | 20,00 €/MWh | 250,0 MWh Netzbezug | 4.750,0 € | Katalog ab 2026, Sockel 250 €/a · produzierendes Gewerbe ✓ · hängt am Restbezug, nicht an der Anlage |
| Stromsteuer-Befreiung § 9 Abs. 1 Nr. 3 | 20,50 €/MWh | 1.155,0 MWh brutto | 23.677,5 € · Ausweis | Regelsatz ab 2026 · hocheffizient ✓ · ≤ 4,5 km ✓ · P_el ≤ 2 MW ✓ · CO₂ 242,1 < 270 g/kWh ✓ · Modus Ausweis |

Die Steuersätze kommen jahresscharf aus dem Katalog; einen Satz von Hand gibt es nicht. Die Unternehmensart spielt
bei §§ 53 und 53a Abs. 5 keine Rolle, sie wirkt nur auf § 54 (Kessel) und § 9b (Netzbezug).

**Vorschau Jahr 1 (2026)** — die Erlösrubrik des zuletzt gebuchten Laufs, gezeigt wird der Block
Blockheizkraftwerk samt der projektweiten Zeile; die vollständige Rubrik mit Photovoltaik und Block B
steht in `07`:

| Position | Menge × Satz | €/a |
|---|---|---|
| Zuschlag Kraft-Wärme-Kopplung (§ 7) | 52.551,3 € × 0,600 | 31.530,8 |
| davon Einspeisung | 469,0 MWh × 5,5667 ct | 15.664,7 |
| davon Eigenstrom | 1.094,2 MWh × 2,4167 ct | 15.866,1 |
| Energiesteuer-Gutschrift Brennstoff (§ 53a Abs. 5) | 4.797,2 MWh × 4,42 € | 21.203,4 |
| Einspeiseerlös Strom | 469,0 MWh × 5,00 ct | 23.450,0 |
| **Summe Blockheizkraftwerk** | | **76.184,2** |
| projektweit: Stromsteuer-Entlastung Netzbezug (§ 9b) | 250,0 MWh, Lauf „Beide Anlagen" | 4.750,0 |
| Stromsteuer-Befreiung Eigenverbrauch (§ 9 Abs. 1 Nr. 3) — Ausweis, in keiner Summe | 1.155,0 MWh | 23.677,5 |

Die § 9b-Zeile hängt am Restbezug des Laufs: allein mit dem Blockheizkraftwerk (335,5 MWh) sind es
6.460,0 €/a, mit beiden Anlagen (250,0 MWh) 4.750,0 €/a.

## Berechnungsgrundlage

```
Mischsatz — marginal über die Leistungsanteile, NICHT klassenweise
  Satz [ct/kWh] = Σ_k Breite_k × Satz_k / P_el
  Breite_k      = min(Obergrenze_k, P_el) − Obergrenze_(k−1)
  Eine Klassenlogik hätte bei 300 kW nur 4,40 ct/kWh ergeben — 21 % zu wenig.
  § 7 Abs. 3a geht Abs. 1 und 2 vor: Neuanlage ≤ 50 kW → 16,00 / 8,00 ct/kWh
  Abs. 2 (Eigenstrom) nur in den drei Tatbeständen des § 6 Abs. 3 ; KEINER ⇒ Satz 0
  Sätze je Anlage: Anlagensatz ?? Projektsatz ; beim Eigenstromsatz verlangt ein gepflegter
  Anlagensatz einen Tatbestand — fehlt er, Satz 0 mit Meldung

Mengenkette (§ 2 Nr. 16 und Nr. 20 KWKG)
  Hilfsstrom(A) = Hilfsenergie_Anteil / 100 × Brennstoff(A) [MWh/a]
  Netto(A)      = max(0, Brutto(A) − Hilfsstrom(A)) ;  Anteil = Netto(A) / Σ Netto
  Eigen/Einsp(A) = Projektmengen_netto × Anteil      (ohne Stundenreihen: alles Eigen)
  Eigen zuerst: Eigen' = max(0, E − H) ;  Einsp' = max(0, F − max(0, H − E))
  Rechtskette: § 7 zahlt auf KWK-Strom → § 2 Nr. 16: bei Anlagen ohne Abwärmeabfuhr ist das
  die Nettostromerzeugung → § 2 Nr. 20: abzüglich Neben- und Hilfsanlagen. Das Netting ist richtig.

Jahresreihe mit Kontingent und Deckel
  Bonus_voll = Eigen × 10 × SatzEigen + Einsp × 10 × SatzEinsp      [€/a bei MWh und ct/kWh]
  je Jahr:  Vergütet  = min(Vbh, Deckel(Kalenderjahr), Restkontingent) × (1 − Abschlag)
            Reihe[t] += Bonus_voll × Vergütet / Vbh
            Rest     −= Vergütet
  Kontingent § 8: Override > 0 gewinnt ; sonst neu 30.000 h · modernisiert ab 50 %/25 % →
  30.000/15.000 · nachgerüstet ab 50/25/10 % → 30.000/15.000/10.000 ; darunter 0 mit Fehlgrund
  Deckelstaffel 5.000 (2021) · 4.000 (2023) · 3.500 (2025) · 3.300 (2026) · 3.100 · 2.900 · 2.700 · 2.500 (ab 2030)
  Prüfkette vorab: Stichtag ≤ 31.12.2026 · Realisierungsfrist 4 Jahre · Ausschreibung > 500 kW ·
  Heizöl-Neuanlage ab 2025
  Pauschale § 9 (≤ 2 kW): 0,04 × 60.000 × P_el, einmalig in Index 0

Energiesteuer, anlagenscharf — je Betrachtungsjahr ein Rechnerlauf, Katalog: jüngste Zeile mit
JahrVon ≤ Jahr ; fehlt der Satz ⇒ 0 € mit Begründung, nie geraten ; Wahl(a) = Anlagenwert ?? Projektwert
  § 53   Gutschrift = Satz_voll(Träger, Jahr) × Menge
         VOLLER_BRENNSTOFF (Vorgabe): Menge = Brennstoff(a) ungeteilt (§ 53 Abs. 2)
         ENERGETISCH: Brennstoff × Strom/(Strom + Wärme) — kein Rechtsverfahren, bewusste Untergrenze
         nur Stromerzeuger ; Kessel mit § 53/53a ⇒ 0 € + Begründung
  § 53a  Gutschrift = Teilsatz × Brennstoff(a), immer Gesamteinsatz
         Nutzungsgradschwelle 70 % (Projektgröße) ; ungepflegt oder unterschritten ⇒ 0 € + Begründung
  § 54   netto = max(0, Σ_{Wahl=54} Teilsatz_54 × Menge(a) − 250 €) ; nur produzierendes Gewerbe
         oder Land-/Forstwirtschaft ; Sockel EINMAL je Lauf
  Einheitenkette:  €/MWh: MWh_Hi × (eff_hs / eff_hi) → Brennwertmenge (Erdgas 11,6/10,5 = 1,1048)
                   €/1.000 l bzw. kg: MWh × 1000 / eff_hi / 1000 — nur mit passender Einheit, keine geratene Dichte
                   €/GJ: MWh × 3,6
  Sätze: Erdgas 5,50 / 4,42 / 1,38 €/MWh · Heizöl EL 61,35 / 40,35 / 15,34 €/1.000 l · Sockel 250 €/a

Stromsteuer
  § 9 Abs. 1 Nr. 3   Betrag = Regelsatz(Jahr) × KwkEigen_BRUTTO [MWh/a] × Anteil
                     Anteil = Σ Strom(a, bestanden) / Σ Strom(a, alle) ; Regelsatz 20,50 €/MWh
                     vier Bedingungen: Hocheffizienz · räumlicher Zusammenhang 4,5 km (Anwenderangaben)
                     P_el ≤ 2 MW je Anlage · CO₂ < 270 g/kWh Energieertrag
                     CO₂-Energieertrag = Faktor_EBeV × Brennstoff / (Strom + Wärme)
                     KwkEigen nur mit Stundenreihen — sonst 0 mit Begründung
  § 9b               Betrag = max(0, 20,00 €/MWh × Netzbezug [MWh/a] − 250 €/a) ; nur produzierendes
                     Gewerbe ; hängt an keiner KWK-Anlage
  Die Mengen beider Vorschriften sind disjunkt (Eigenverbrauch gegen Netzbezug).

Einspeiseerlös   KWK_Einspeisung × 10 × EV_KWK (nur bei gepflegtem Satz) → Zonentarif → Rollentarif ;
                 nominal konstant

Kohärenzprüfung (§ 3.9) — Warnzeilen ohne Rechenwirkung
  2 Entlastung ohne Belastung: Gutschrift gebucht, Preis weist die Steuer nicht aus → Warnung mit Betrag
  3 Belastung ohne Entlastung: Anteil ausgewiesen, keine Wahl bzw. kein § 9b → Hinweis
  4 Satz ≠ Katalogsatz (Toleranz 0,005 ct/kWh) → Hinweis
  Doppelpflege Hilfsenergie: Anlagenanteil > 0 UND aktive Kostenposition derselben Anlage → Warnung
```

## Berechnungserläuterung am Beispielprojekt

### Der Mischsatz — wie ein Steuertarif

| Leistungsanteil | Breite | Satz | Beitrag | Rechtsgrundlage |
|---|---|---|---|---|
| bis 50 kW | 50 kW | 8,00 ct | 400 | § 7 Abs. 1 Nr. 1 |
| > 50 bis 100 kW | 50 kW | 6,00 ct | 300 | Nr. 2 |
| > 100 bis 250 kW | 150 kW | 5,00 ct | 750 | Nr. 3 |
| > 250 kW bis 2 MW | 50 kW | 4,40 ct | 220 | Nr. 4 |
| **Mischsatz Einspeisung** | 300 kW | | **1.670 ÷ 300 = 5,5667 ct/kWh** | belegt am Bestand |

Eigenstrom (Tatbestand Nr. 2 Kundenanlage): 50 × 4,00 + 50 × 3,00 + 150 × 2,00 + 50 × 1,50 = 725 ÷
300 = **2,4167 ct/kWh**. Ohne Tatbestand wäre der Satz 0 und Bonus_voll halbiert.

### Die Jahresreihe

| Schritt | Rechnung | Ergebnis | Anmerkung |
|---|---|---|---|
| 1 Bonus Einspeisung | 469,0 × 10 × 5,5667 | 26.107,8 € | volle Menge, ungedeckelt |
| 2 Bonus Eigenstrom | 1.094,2 × 10 × 2,4167 | 26.443,5 € | nur wegen Tatbestand Nr. 2 |
| **Bonus_voll** | 26.107,8 + 26.443,5 | **52.551,3 €** | Bezugsgröße der Jahresreihe |
| 3 Deckelanteil 2026 | min(5.500 ; 3.300 ; 30.000) ÷ 5.500 | 0,600 | Deckel greift, nicht das Kontingent |
| **Zuschlag Jahr 1** | 52.551,3 × 0,600 | **31.530,8 €** | 15.664,7 Einspeisung + 15.866,1 Eigenstrom |

| t | Jahr | Deckel | Vergütet | Rest danach | Zuschlag |
|---|---|---|---|---|---|
| 1 | 2026 | 3.300 | 3.300 | 26.700 | 31.530,8 |
| 2 | 2027 | 3.100 | 3.100 | 23.600 | 29.620,0 |
| 3 | 2028 | 2.900 | 2.900 | 20.700 | 27.708,9 |
| 4 | 2029 | 2.700 | 2.700 | 18.000 | 25.797,9 |
| 5–11 | 2030–2036 | 2.500 | 2.500 | 15.500 → 500 | 23.887,0 je Jahr |
| 12 | 2037 | 2.500 | **500** (Rest) | 0 | 4.777,4 |
| 13–20 | 2038–2045 | — | 0 | 0 | 0 |
| **Summe** | | | 30.000 | | **286.644 €** = Bonus_voll × 30.000 / 5.500 |

Der volle Jahresbonus wird nie ausgezahlt: Der Jahresdeckel sinkt bis 2030 auf 2.500 h, und das
Gesamtkontingent ist danach erschöpft. Aus einer scheinbaren Dauerförderung wird eine Reihe über
zwölf Jahre mit fallendem Anfang — acht Jahre des Betrachtungszeitraums bleiben ohne Zuschlag. Das
Mockup zeigt die Reihe als Balkendiagramm.

### Energiesteuer

| Schritt | Rechnung | Ergebnis | Anmerkung |
|---|---|---|---|
| Brennwertmenge | 4.342,1 MWh × 11,6 / 10,5 | 4.797,2 MWh (H_s) | Einheitenkette €/MWh |
| § 53a Abs. 5 | 4.797,2 × 4,42 | **21.203,4 €/a** | Nutzungsgrad 83 % ≥ 70 % ✓ |
| Alternative § 53 | 4.797,2 × 5,50 | 26.384,3 €/a | voller Brennstoff, Abs. 2 |
| Alternative § 53 energetisch | × 1.650 / (1.650 + 1.953,9) = × 0,458 | 12.082 €/a | bewusste Untergrenze, kein Rechtsverfahren |

### Stromsteuer

| Schritt | Rechnung | Ergebnis | Anmerkung |
|---|---|---|---|
| CO₂-Energieertrag | 200,9 × 4.342,1 / (1.650,0 + 1.953,9) | 242,1 g/kWh | < 270 ✓ (Heizöl 303 g/kWh würde scheitern) |
| § 9 Abs. 1 Nr. 3 | 1.155,0 MWh (brutto) × 20,50 | 23.677,5 €/a | **Ausweis** (Vorgabe, Schemaschritt 88); Erlösreihe nur bei ausdrücklicher Wahl ERLOES, mit Kohärenzwarnung |
| § 9b | max(0, 250,0 × 20,00 − 250) | 4.750,0 €/a | Netzbezug, produzierendes Gewerbe |

## Befunde und offene Punkte

| Nr. | Befund | Behandlung |
|---|---|---|
| ⚠ **K-1** | **Der zweite Fall des § 2 Nr. 16 fehlt:** bei Anlagen mit Vorrichtung zur Abwärmeabfuhr (Notkühler) ist KWK-Strom = Nutzwärme × Stromkennzahl, nicht die Nettostromerzeugung; EPOS-Plan führt weder Kennzeichen noch Stromkennzahl und rechnet immer Fall 1 — Zuschlag für Notkühler-Anlagen **zu hoch** | **entschieden 18.09.2026 nach Empfehlung**: Kennzeichen und Stromkennzahl je Anlage (Schemaschritt 90, Vorschlag der Stromkennzahl aus P_el/P_th am Feld), Fall 2 in der Mengenbildung je Anlage; kein Referenzprojekt betroffen — Konzept § 3.6 |
| ✔ B-1 | § 9 Abs. 1 Nr. 3 als Erlösreihe gebucht — es entsteht aber gar keine Stromsteuer; gemessen 1.510,84 €/a auf beiden Pfaden (Projekt 1024) | umgesetzt mit B6: Ausweis (`Stromst_Befreiung_Modus`, Vorgabe AUSWEIS, Schemaschritt 88); der Messwert zu 1024 ist am heutigen Stand der Testdatenbank nicht nachstellbar (Hocheffizienznachweis 0) |
| ✔ K3 | Modusfeld § 9 Nr. 3 | erledigt mit B6: Schemaschritt 88, Feld offen, Vorgabe AUSWEIS |
| K4 | Tabellenspalte „Brennstoff" ohne Leseweg | kleiner Leser `CarrierId` → Name in B5 |
| K7 | Schreibweg der drei B5-Spalten fehlt (`KwkgAnlagenCtrl.Speichere` = 8 Spalten) | auf 11 Spalten erweitern — B5-Kernaufgabe |
| R-U1 | § 53 neben § 53a — Entweder-oder | als Auswahl modelliert, mit dem Hauptzollamt zu klären |
| R-U3 | Ausschluss fossiler flüssiger Brennstoffe (nur Sekundärquelle) | als Prüfkette „Heizöl-Neuanlage ab 2025" umgesetzt |
| ⚠ **S-1** | **Hilfsstrom-Netting des Beispiels widerspricht dem Rechenkern:** Die Mengentafel teilt die Nettostromerzeugung 1.563,2 MWh im Verhältnis 70/30 (1.094,2 / 469,0 MWh, `Beispielprojekt.md` § 3); `HilfsstromRechner.NettoSplit` und die Formelkarte ziehen den Hilfsstrom **zuerst vom Eigenverbrauch** ab (Eigen' = 1.155,0 − 86,8 = 1.068,2 · Einsp' = 495,0 MWh — „Physik, keine Konvention"). Wirkung: Zuschlag Jahr 1 32.022,2 statt 31.530,8 €, Einspeiseerlös 24.750,0 statt 23.450,0 € (der Rechenkern bewertet die Einspeisung der Strommatrix, `KwkEinspeisungGesamtMWh`) | **Entscheid ausstehend** (Mockup, Anhang Umsetzungsstand U24): Folgt das Beispiel der Kernregel, sind die Zahlenproben der Abschnitte 5, 7 und 8 und `Beispielprojekt.md` neu zu rechnen; Katalog und Grundlagenpapier sagen zur Reihenfolge nichts, deshalb hier nicht geändert |
| U22 | Überlagerung „Sätze und Herkunft" mit Eingabefeld „eigener Wert" je Größe; die Zahlen-, Datums- und Schalterfelder bleiben im Formular, die sechs Klapplisten werden Anzeigezeilen, die Knöpfe „Vorschlag übernehmen" wandern in die Überlagerung (Anwenderwünsche 18.09.2026) | Mockup Abschnitt 5; Konzept § 2.2 („Der Vorschlag steht am Feld, nicht als Sammelknopf") ist damit überholt und nachzuziehen |
| U25 | Staffelzeile unter dem Jahresdeckel und Warnband zum Deckelanteil — Anzeigen, die der Dialog nicht führt | Mockup Abschnitt 5, Anhang Umsetzungsstand |
| U26 | Die Satzfelder zeigen zwei Nachkommastellen (`Nachkommastellen="2"`): 5,5667 erscheint als 5,57, wer das Feld anfasst, verliert zwei Stellen | Mockup Abschnitt 5, Anhang Umsetzungsstand: vier Nachkommastellen |

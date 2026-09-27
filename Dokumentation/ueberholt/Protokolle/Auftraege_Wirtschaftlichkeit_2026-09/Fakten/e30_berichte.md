# E30 — Bericht Phase 0 (Opus, 26.09.2026, Worktree e30 ab ebf01a90; nur Befund, kein Produktcode)

Testdatenbank 97e56579 (Schemastand 148) nur als Kopie im Scratchpad (`scratchpad/e30/`), gelesen und geprobt über dotnet-Dateiskripte
(`q.cs` Leseabfragen, `b8/b8.cs` beide Kapitalwertwege, `pflege/probe_pflege.cs` + `pflege/bk.cs` Datenpflege-Probe). Kein Test gelaufen,
kein Commit. Die Ankerwerte habe ich auf der Kopie reproduziert: −21.895.377,28 (Kern, Lauf 212) und −31.142.971,06 (Berichtsweg).

## Teil 1 — B4 Hilfsenergiekosten

**Wo Hilfsenergie angegeben wird (zwei Wege, heute getrennt):**

| Weg | Feld / Einheit | Pflege | Wirkung heute |
|---|---|---|---|
| Mengenweg | `Tab_Energieanlagen.Hilfsenergie_Anteil` [% des Endenergiebedarfs = Brennstoff der Anlage] (Schritt 61, `SchemaKatalog.cs:1279`) | nur BHKW-Wirtschaftlichkeitsdialog (`BhkwWirtschaftlichkeitDialog.razor:216-220`, `AnteilPflegbar` :1060 nur BHKW; Kessel nur Hinweis `BhkwWirtschaftlichkeitTexte.cs:207`), gespeichert über `KwkgAnlagenCtrl.cs:319` | Menge `HilfsstromRechner.MengeMWh` (`HilfsstromRechner.cs:58`) = Anteil × Brennstoff; mindert nur die KWKG-Nettomenge (`WirtschaftlichkeitCtrl.cs:2815-2828`, `:5344`) und wird als Ausweis in `Tab_ErgebnisBHKWModul/HeizkesselModul.Hilfsenergie` geschrieben (`ErgebnisCtrl.cs:393-412`). **Keine Kosten.** |
| Kostenweg | Betriebskostenposition „Hilfsenergiekosten…" (Kat. 2, `ID_Anlage`), Bemessung A `PROZENT_ENDENERGIEKOSTEN`, B `PROZENT_ENDENERGIEBEDARF`, C `JAHRESBETRAG`, Satz in `Einheitpreis` | Kostenverwaltung (Pflichtzeilen aus Vorlagen 11/12/13/15) | Weg B: Auflöser liefert Brennstoff-kWh × Arbeitspreis des **Projekt**-Stromträgers (`EndenergieAufloeser.cs:436-445`), `BetriebskostenCtrl.Betrag` × Satz/100 (`BetriebskostenCtrl.cs:118-122`), Endenergie-Topf p_E (`WirtschaftlichkeitCtrl.cs:7250-7300`). Ohne Satz → erfasster Wert 0 (I‑2, `BetriebskostenCtrl.cs:77`). |

Dazu ein dritter, **anderer** Hilfsstrom: `Tab_WP.Kuehl_Hilfsstromanteil` (Kältebetrieb) steckt physisch in der Simulation
(`Kaeltekaskade.cs:350`, `strom = verdichter × (1 + Anteil)`), damit in `Strombedarf_Verbraucher` und im Netzbezug — er ist schon in den
Energiekosten und darf von B4 **nie** erfasst werden.

**Daten:** In der ganzen Testdatenbank trägt **keine** Anlage `Hilfsenergie_Anteil` (0 Zeilen `IS NOT NULL`), und **keine**
Hilfsenergieposition trägt Satz oder Betrag (14 Zeilen, alle 0/leer). Referenzprojekte mit Zeilen: 1018 (1, `PROZENT_BRENNSTOFFKOSTEN`), 1030
(3 Weg A + 1 Jahresbetrag), 1026 (2 Weg A + 3 Jahresbetrag); dazu 1019 (2 Weg A an WP, kein Referenzprojekt). R20 `aggregate.csv`
`BHKWModul[0/1].Hilfsenergie`, `HeizkesselModul[0].Hilfsenergie` = 0 (1030 Z. 81/90/…).

**Simulation/Strommatrix:** `STROMBEDARF_GESAMT` = `Strombedarf_Verbraucher_viertelstuendlich` = Rest nach Kaskade + BHKW-Strom
(`SimulationControl.cs:566-573`, `ZeitreihenExtraktor.cs:34-37`): Gebäude + WP + Heizstab + E-Kessel + Kälte (samt Kälte-Hilfsstrom).
**BHKW-/Kessel-Hilfsstrom ist nicht darin** (Konzept § 4.3: StromMatrix bleibt brutto). Also fehlen die Kosten genau dort, wo ein Anteil
an der Anlage gepflegt ist: Menge vorhanden (KWKG), Kosten nirgends.

**Doppelzählungsgefahr:**
- Energiekosten gegen B4: **keine**, solange B4 nur `Tab_Energieanlagen.Hilfsenergie_Anteil` bepreist (nicht in der Simulation). Den
  Kälte-Hilfsstrom der WP darf B4 nicht anfassen.
- Anteil an der Anlage **und** aktive Position an derselben Anlage: heute nur Warnung „verrechnet wird nichts"
  (`KohaerenzPruefung.cs:372-410`). Wird der Anteil künftig bepreist, zählte beides doppelt → Vorrangregel nötig (Q2).
- Pflichtzeile mit Weg A und einem aus dem Anteil übernommenen Satz: Faktor ≈ 3 falsch (Sätze nicht austauschbar, `DbWerte.cs:612-615`)
  → der abgeleitete Betrag muss immer nach Weg B rechnen; B5 gleicht die Zeilen an.
- KWKG-Netting und Kosten sind **keine** Doppelzählung: verschiedene Größen (Zuschlag auf Nettostrom § 2 Nr. 20 gegen Strombezug).
  Physikalisch: Netzbezug real = G + H − P; Modell = (G − P) × Preis + H × Preis — gleich. Nur im Einspeisefall (P > G) überschätzt der
  Bezugspreis den entgangenen Einspeiseerlös (benannte Näherung; 1030 Einspeisung 0,392 MWh/a).

**Vorschlag Rechenweg (Q1 a):** In `LiesBetriebskostenTopfe` (und gleichlautend `LiesBetriebskostenPositionen`, Summe = Probe) je Anlage mit
Anteil > 0 und Brennstoffmenge:
1. Hat die Anlage eine Hilfsenergie-Pflichtzeile **ohne** eigenen Satz und Betrag → sie trägt den Betrag
   `Hilfsstrom (HilfsstromRechner.MengeMWh, dieselbe Menge wie KWKG) × 1000 × Arbeitspreis Projekt-Stromträger` — rechnerisch Weg B mit
   Satz = Anteil, unabhängig von der gespeicherten Bemessung; Herleitung „Satz aus Anlage (BHKW-Wirtschaftlichkeit) 2,0 %".
2. Hat sie keine solche Zeile → eine abgeleitete Zeile „Hilfsenergiekosten (aus Anlagenanteil)" im Nachweis und im Endenergie-Topf.
3. Hat die Position selbst Satz oder Betrag → die Position gilt, der Anteil wirkt nur auf KWKG, Kohärenzwarnung mit neuem Wortlaut (Q2 a).
4. Szenarien: Mengenfaktor und Strompreis des Szenarios über den vorhandenen Auflöser (E9a), Topf p_E wie Weg B.

**Welche Pflichtzeile trägt es (Wechselwirkung B3/B5):** 1030 „Hilfsenergiekosten" (Stamm 134) an 14920 (101600590) und 14921
(101600593), nach B5 in Weg B; Kessel „Hilfsenergiekosten (Strom)" (Stamm 147, 101600587) an 11334 — für Kessel gibt es keinen
Oberflächenweg zum Anteil; dort bleibt der Satz an der Position der Weg.

**Zahlen je Referenzprojekt:** Weil kein Projekt einen Anteil oder Satz trägt, ändert B4 **keinen** Kapitalwert: 1030 −21.895.377,28 (Kern)
/ −31.142.971,06 (Bericht), 1024 −2.896.359,13 / −2.772.642,27 — alle bitgleich erwartet; 1007/1008/1017/1018/1023/1039–1047 ohne
Kapitalwert oder ohne Hilfsenergie. **R20: keine CSV wandert** (Referenzlauf rechnet keine Wirtschaftlichkeit; Hilfsenergie-Spalten bleiben
0). Hypothetisch 1030 mit 2 % an beiden BHKW: 1.241,55 MWh × 2 % = 24,83 MWh × 0,25 €/kWh = 6.207,75 €/a, ΔKapitalwert ≈ −110.043 €
(Barwertfaktor p_E 2 %, i 3 %, 20 a = 17,7267); `aggregate` `BHKWModul[i].Hilfsenergie` würde 20,42/4,41 MWh.

## Teil 2 — N10 BHKW-Deckungsgrad

**Heute** Produktion (samt Einspeisung) / Projekt-Strombedarf:
- `SimulationRunner.cs:624-625` (persistiert `Tab_ErgebnisBHKW.Strombedarfsdeckung`, `ErgebnisCtrl.cs:367`, aggregate `BHKW.Strombedarfsdeckung`,
  Word-Torte `BausteineVergleich.cs:225-226`),
- `SimulationErgebnisCtrl.cs:752-755` (BHKW-Reiter, „wortgleich"),
- Übersicht: Ring `SimulationErgebnisHuelle.Bilder.cs:331-337` und Stromtabelle `…Anzeige.cs:381` mit `BhkwStromproduktionMwh`
  (`SimulationErgebnisCtrl.cs:187/203-205`), Ringmitte `Anzeige.cs:181`.

**Vorschlag (Q7 a):** Zähler = Stromproduktion − KWK-Einspeisung (die E29-Größe `BhkwEinspeisung…` = `KwkEinspeisungGesamtMWh`);
Nenner = Σ `Strombedarf_Verbraucher` (= `StrombedarfMitEigenverbrauchMwh` nach E29/N6 samt Kälte); an allen fünf Stellen, geklemmt 0–100.
Probe: Σ Netzbezug + Σ Eigen = Σ G stündlich exakt, deshalb G = `Sim.Reststrom` + Eigen (an R20 nachgerechnet).

| Projekt | Produktion | Einspeisung | Eigen | Gesamtbedarf | heute (aggregate) | neu | aggregate wandert |
|---|---|---|---|---|---|---|---|
| 1017 | 36,805 | 0 | 36,805 | 692,681 | 5,48 | **5,31** | ja |
| 1018 | 27,458 | 27,458 | 0 | 0 | 0 | 0 | nein |
| 1024 | 95,701 | 0 | 95,701 | 456,982 | 26,22 | **20,94** | ja |
| 1030 | 432,305 | 0,392 | 431,913 | 4.790,086 | 9,02 (9,025) | 9,02 (9,017) | nein (Rundung) |
| 1047 | 35,867 | 0 | 35,867 | 677,043 | 5,34 | **5,30** | ja |

**R21:** drei `aggregate.csv` (1017, 1024, 1047), je ein Schlüssel; 429/432 CSV byte-gleich; kern.yml hält 1017/1047 → Neueinfrierung nötig.
Übersicht/Reiter/Torte: Anzeige; Messlatten Word/Excel 1030 sind Strukturtexte (ohne Zahlen der Torte) — in Phase 1 prüfen;
ChartProben synthetisch. Gespeicherte Läufe behalten ihre alte Zahl bis zum nächsten Lauf.

## Teil 3 — B3/B5 Datenpflege (1030, 1026)

**Befund Zeilen:** Altzeilen 1030 `101600097` (Stamm 83 „BHKW", Gruppe „Wartung BHKW", `BETRAG` 18.000, Anlage 14920) und `101600098`
(Stamm 79, „Wartung Kessel", `BETRAG` 2.000, 11334); Pflichtzeilen `101600588` (Stamm 126 „Wartung BHKW", `EUR_PRO_KWH_ELEKTRISCH`, 14920)
und `101600585` (Stamm 146 „Vollwartung / Wartung Kessel", `EUR_PRO_KWH_THERMISCH`, 11334), beide Kostenart BETRIEBSGEBUNDEN.
Weg-A-Hilfsenergiezeilen: 1030 `101600587/590/593` (drei, nicht fünf), 1026 `101600570` (WP Pumpen) und `101600576` (Kessel) — zusammen die
fünf. Vorlagen 11/12/13 führen seit Schritt 94 Weg B (`HilfsstromBemessungVorlage.cs`, nur die Saat). Nicht erfasst: 1019 (zwei Weg-A-Zeilen
an WP, kein Referenzprojekt) und 1018 `101600562` (`PROZENT_BRENNSTOFFKOSTEN`, Fixture `ProjektkostenArtenTests.cs:111`). Keine FK auf
`Tab_ProjektWerte`, keine `Tab_ErgebnisWirtschaftlichkeit`-Zeile zu 1030.

**Skriptentwurf** `Referenzlaeufe/Skripte/datenpflege_1030_1026_betriebskosten.cs` (Muster E24/1048: Vorzustand zellgenau prüfen,
eine Transaktion in einer Arbeitsdatei, Nachprüfung, dann ersetzen; Ziel erreicht → 0; Abweichung → 2, Datei unverändert):
1. `101600588` → `Bemessung = JAHRESBETRAG`, `EingegebenerWert = 18000`; `101600585` → `JAHRESBETRAG`, 2000.
2. `DELETE 101600097, 101600098`.
3. Fünf Zeilen `PROZENT_ENDENERGIEKOSTEN` → `PROZENT_ENDENERGIEBEDARF` (nur ohne Satz).

**Probe auf der Kopie:** 1+1+2+5 Zeilen; `LiesBetriebskostenTopfe` 1030/1026/1024/1018 in drei Szenarien **gleich** (1030 Betrieb 20.000,
Endenergie 0); Kern-Kapitalwerte 1030/1024 in drei Szenarien bitgleich; Berichtsweg 1030 −31.142.971,06 / −31.311.485,34 / −31.007.010,80
bitgleich. Summe 18.000 + 2.000 ganzzahlig → reihenfolgeunabhängig. **Anker/R20: keine Wirkung.**
**Folgen:** Tests mit Zeilen-IDs der Altzeilen: `BetriebskostenBemessungsmatrixTests.cs:53/55` (→ 101600585/101600588, gleiche
Komponente/Anlage, `FrischeBasis` gleich), `HilfsstromBemessungVorlageTests.cs:54`; Messlatten `Bericht_Word_1030(_Vorlage).txt`
(Betriebskostentabelle 18 → 16 Zeilen) und `Bericht_Excel_1030.txt` (Z149–Z165 rücken um 2) begründet neu; Testdatenbank-SHA neu,
LIESMICH-Nachtrag.

## Teil 4 — B8 zwei Kapitalwert-Anker 1030

Gemessen mit beiden Wegen auf der Kopie: Kern (`WirtschaftlichkeitAnkerTests.cs:328`, `ErgebnisCtrl.Load` → Lauf 212 vom 30.08.2026, ohne
Zeitreihen) −21.895.377,28; Bericht (`PvAusweisStromMatrixTests.cs:243`, `Sammle(…, neuRechnen: true, mitZeitreihen: true)`)
−31.142.971,06. **Ursache: Lauf 212 ist veraltet.**

| Ursache | Lauf 212 | frisch | Energiekosten |
|---|---|---|---|
| Kessel ohne Brennstoff: vor B‑1, `Verbrauch = 0` (`KesselVerbrauchFehlt = true`, `KostenEmissionRechner.cs:344-390` meldet nur) | 0 MWh | 5.403,1 MWh | +432.248 €/a (× 0,08 €/kWh) |
| BHKW-Brennstoff vor R10 (Wirkungsgrad) | 1.048,27 MWh | 1.241,55 MWh | +15.462 €/a |
| E27-Klemme Netzbezug | 4.357,78 | 4.358,17 | +97,50 €/a |
| **Summe** | 1.176.906,60 | 1.624.713,70 | **+447.807,10 €/a** |
| CO₂-Abgabe (55 €/t × +1.343,1 t) | 13.837,16 | 87.709,25 | +73.872,08 €/a |

(447.807,10 + 73.872,08) × 17,7267 ≈ 9,2476 Mio. € Δ Barwert Ausgaben; KWKG +54,25 € → ΔKW −9.247.593,78 = Differenz der Anker.
Betriebskosten 20.000 in beiden gleich. **Richtig ist −31.142.971,06.** Probe: Lauf 212 auf einer Kopie neu gebucht → Kernweg −31.143.024,30
(Rest −53,24 € = KWKG-Split ohne Zeitreihen 7.316,08 gegen 7.322,63). Dasselbe gilt für 1024 (Lauf 199 vom 26.08.2026, andere Wärme
389,73 gegen 509,90 MWh): −2.896.359,13 gegen −2.772.642,27, nach Neubuchung gleich.

**Neubuchung hätte viele Folgen:** Lauf 212/199 sind Vorrichtungen weiterer Tests (`KostenBetriebsstandTests` 862,18/186,09,
`ElektrokesselStromTests` Lauf 199, `KwkgAnlagenartFehltTests`/`KwkgFall2Tests`/`KwkAnlagenwahrheitTests` 7.315,956634,
`KwkgErsatzwegGewichtetTests`, `EnergiekostenGrundTests` 188.167,18, `ErsatzRestwertKennzeichenTests`, `Co2StromtraegerRueckfallTests`,
`ErgebnisverweisKopieTests` mit Ergebnis-IDs 213–221, `BetriebskostenBemessungsmatrixTests.SpaltenLeeren`). Ein frisch gebuchter Lauf bekäme
neue IDs (1024 bekam auf der Kopie 213). R20 berührt das nicht (Referenzlauf simuliert frisch, aggregate ohne Ergebnis-IDs).

**Vorschlag (Q11 b):** Lauf 212/199 bleiben Vorrichtungen „gespeicherter Altlauf"; der Kernanker wird im Test und in § 6.2 als
„Kapitalwert des gespeicherten Laufs 212 (vor B‑1/R10, ohne Kesselbrennstoff)" benannt; der fachliche Anker ist −31.142.971,06
(Berichtsweg). Neu ein Erklärtest: Lauf 212 → `KesselVerbrauchFehlt`, ΔEnergiekosten = Zerlegung oben. Alternative a (Neubuchung,
4–6 h, zehn Testdateien) nur auf ausdrücklichen Wunsch.

## Fragen

- **E30‑Q1 (B4 Ort):** (a) Pflichtzeile ohne Satz/Betrag trägt den Betrag aus dem Anlagenanteil (Weg B), fehlt sie → abgeleitete Zeile;
  (b) immer eigene abgeleitete Zeile, Pflichtzeile bleibt 0; (c) in die Energiekosten (Netzbezug). **Empfehlung a.**
- **E30‑Q2 (Doppelpflege):** (a) gepflegte Position gilt, Anteil nur KWKG, Warnung neu formuliert; (b) Anteil gilt, Position ruht; (c) beide
  addieren. **Empfehlung a.**
- **E30‑Q3 (Anlagen):** (a) jede Anlage mit Anteil > 0 und Brennstoffmenge (BHKW, Brennstoffkessel); WP/E-Kessel/Kälte ausgenommen; (b) nur BHKW.
  **Empfehlung a.**
- **E30‑Q4 (Preis):** (a) Arbeitspreis Projekt-Stromträger ohne Grund-/Leistungspreis, Szenariopreis folgt; Einspeisefall benannte Näherung;
  (b) Mischpreis Eigen/Bezug. **Empfehlung a.**
- **E30‑Q5 (Emission/Steuer des Hilfsstroms):** nur benennen, eigener Folgepunkt. **Empfehlung: so.**
- **E30‑Q6 (Referenzdaten):** (a) keinen Anteil an 1030 pflegen, B4 auf Kopien testen; (b) 2 % an beiden BHKW (Anker −110 k€, aggregate
  wandert). **Empfehlung a.**
- **E30‑Q7 (N10):** (a) Eigen/Gesamtbedarf an allen fünf Stellen samt Persistenz, Ring und Tabelle zeigen den Eigenanteil, Einspeisung als
  Tooltip; (b) nur persistierte Größe. **Empfehlung a.**
- **E30‑Q8 (B3 Form):** (a) Pflichtzeilen auf `JAHRESBETRAG` 18.000/2.000; (b) Bemessung lassen, Betrag über I‑2-Rückfall. **Empfehlung a.**
- **E30‑Q9 (B3 Kaskade):** (a) 18.000 bleibt an 14920 (Kaskadenbetrag, ergebnisneutral); (b) teilen 14920/14921. **Empfehlung a.**
- **E30‑Q10 (B5 Umfang):** (a) nur die fünf Zeilen 1030/1026; (b) auch 1019 (zwei WP-Zeilen); 1018 bleibt Fixture. **Empfehlung a.**
- **E30‑Q11 (B8):** (a) Läufe 212/199 neu buchen; (b) Anker benennen, Erklärtest, § 6.2 beide Anker mit Weg; (c) Kerntest auf Berichtsweg
  umstellen. **Empfehlung b.**
- **E30‑Q12 (Katalog):** Empfehlung der Vorlagen 11/12 (2–4 %, 4–8 %) stammt aus Weg A (`DbWerte.cs:617`), steht seit Schritt 94 an Weg B;
  Kessel 4–8 % Strom vom Brennstoff ist fachlich zu hoch. Nur benennen (eigener Katalogauftrag). **Empfehlung: so.**

## Reihenfolge, Aufwand, Tests

Nach E29-Merge (Konflikte `SimulationRunner.cs`, `SimulationErgebnisCtrl.cs`, `…Huelle.Anzeige/Bilder.cs`, Messlatte Excel 1030):
E30/1 Datenpflege + Wache + ID-/Messlatten-Nachzug ~1,5 h → E30/2 B4 ~3,5 h (Topf, Positionsliste, Raster-Herleitung, Kohärenztext,
Formelmappe) → E30/3 N10 ~1,5 h → E30/4 B8 (b) ~1 h → E30/5 Tests ~1,5 h → E30/6 A/B, R21 `2026-09-26_R21_BhkwDeckung`, R20 ins Archiv,
LIESMICH ~1,5 h. Zusammen ~10,5 h + Gate. Kein Schemaschritt.

**Tests:** (1) `DatenpflegeBetriebskosten1030Tests` (Wache): Zellen, Altzeilen weg, keine Weg-A-Hilfsenergiezeile in 1030/1026,
`LiesBetriebskosten` 1030 = 20.000 in drei Szenarien, zweiter Skriptlauf 0. (2) `HilfsenergiekostenAusAnteilTests` (Kopie 1030): Anteil 2 %
an 14920 → Endenergie-Topf = `HilfsstromMWh` × 250 €, ΔKW = −Betrag × 17,7267; Position mit Satz 3 % → Position gilt, Warnung; Anlage ohne
Zeile → abgeleitete Zeile; Summe Nachweis = Topf; Szenario Ungünstig folgt dem Preis; Anteil 0/NULL bitgleich; Kälte-Hilfsstrom unberührt;
Anker 1030/1024 beider Wege bitgleich. (3) N10: Theorie (P, E, G) → Deckung; Vorrichtung 1017 5,31 / 1024 20,94 / 1047 5,30 / 1030 9,02 /
1018 0; Übersicht Ring/Tabelle Eigenanteil ≤ 100; bUnit BHKW-Reiter. (4) B8-Erklärtest. (5) Referenzlauf A/B gegen R20: nur 3 aggregate-Werte
(1017/1024/1047 `BHKW.Strombedarfsdeckung`), dann R21 einfrieren, Determinismus, Voll-Lauf nach Testhost-Regel.

# E30 — Abschlussbericht Phase 1 (Opus, 26.09.2026 ~11:50, Worktree e30, Zweig e30, HEAD fb2dcc3f; Statusnummer #542)

Kein Push, kein Merge auf pm26, kein Stash, Papiere nur so weit, wie die Basis und die Linkwache es verlangen.

**Commits** (ab ebf01a90):

| Commit | Inhalt |
|---|---|
| `4dc4a662` | E30/1 Datenpflege 1030/1026 (Skript `Referenzlaeufe/Skripte/datenpflege_1030_1026_betriebskosten.cs`, `DatenpflegeBetriebskosten1030Tests`, Zeilen-IDs in zwei Tests) |
| `cefe1b54` | Kommentare #541 → #542 |
| `b9e35a3e` | E30/2 B4 Hilfsenergiekosten aus dem Anlagenanteil (`HilfsenergieAusAnteil`, Töpfe und Nachweisliste, Kohärenztext, Ressourcen, `HilfsenergiekostenAusAnteilTests`) |
| `44e52518` | E30/4 B8: Kernanker heißt „gespeicherter Altlauf 212", `KapitalwertAnkerZerlegungTests` |
| `2299b8da` | E30/5 `KostenVorlagenhinweisTests` nach B3/B5, B4 am frischen Lauf |
| `bd338f1f` | Merge origin 50fa9e5a (E29 #536, BV‑E5 #541, Zapfprofil #543); Testdatenbank neu gepflegt |
| `fb936e4c` | E30/3 N10 an allen fünf Stellen, `BhkwStromdeckungTests` |
| `9597762f` | E30/5 Messlatten Word/Excel 1030 neu eingefroren |
| `fb2dcc3f` | E30/6 Neueinfrierung R21, R20 archiviert, LIESMICH-Nachtrag, Basisname |

**Testdatenbank:**
- Beim Merge LFS-Konflikt; die origin-Fassung `22e67400` genommen (70 680 576 Byte).
- Darauf das Skript gezogen: Vorzustand passte, 9 Zeilen, `integrity_check` ok; ein zweiter Lauf ändert nichts.
- Ergebnis: **LFS-oid `40df1bf2a9e0544368322e25e9651ee7ac751204773d177d51a4f1a12ef475ee`, 70 680 576 Byte**, Schemastand 148.
- Grün danach: `ZapfprofilReferenzprojektWacheTests`, `DatenpflegeBetriebskosten1030Tests` und alle `Zapf*`/`Zapfprofil*`-Tests samt Speicherauslegung (307/307 im gezielten Lauf).

**B4 Hilfsenergiekosten (Q1 a–Q4 a):**
- Trägt eine BHKW- oder Brennstoffkessel-Anlage einen Hilfsenergieanteil größer 0, rechnet ihre Hilfsenergie-Pflichtzeile nach Weg B mit dem Anteil als Satz. Fehlt die Zeile, entsteht eine abgeleitete Zeile. Alles läuft im Endenergie-Topf (p_E).
- Eine Position mit eigenem Satz oder Betrag hat Vorrang; der Anteil wirkt dann nur auf KWKG, und der Kohärenzhinweis sagt das.
- Ausgenommen: Elektrokessel, Wärmepumpe, Kälte-Hilfsstrom.
- Zahlen an der Kopie von 1030 mit 2 % an beiden BHKW:
  - gespeicherter Altlauf 212: **5.241,35 €/a**
  - frischer Lauf: **≈ 6.207,75 €/a**
- Kein Referenzprojekt trägt einen Anteil: Anker bitgleich, keine CSV-Wirkung.

**N10 BHKW-Stromdeckung (Q7 a):**
- Neue Formel: (Erzeugung − KWK-Einspeisung aus E29) ÷ Σ Strombedarf aller Verbraucher, geklemmt auf 0…100.
- Eine Formel für Lauf/Datenbank/aggregate, Word-Torte, BHKW-Reiter, Ring und Stromtabelle der Übersicht.

**Messlatten (Grund: B3, zwei Positionen weniger):**

| Datei | alt | neu |
|---|---|---|
| `Bericht_Word_1030.txt` Z. 71 | Tabelle 5 Sp. × 18 Z. | 16 Z. |
| `Bericht_Word_1030_Vorlage.txt` Z. 68 | Tabelle 5 Sp. × 18 Z. | 16 Z. |
| `Bericht_Excel_1030.txt`, Blatt 3 (auf der 8-Spalten-Fassung von E29) | 171 Z. | 169 Z. |
| `Bericht_Excel_1030.txt`, Summe der Positionen | Z165 `=SUM(E149:E164)` | Z163 `=SUM(E149:E162)` |
| `Bericht_Excel_1030.txt`, „Bewertung nach DIN EN 17463“ | Z167 | Z165 |

N10 ändert keine Zeile der Messlatten.

**A/B gegen R20 (14 Projekte): 11/14 PASS, 429/432 CSV byte-gleich.** Geändert hat sich allein `aggregate.csv` `BHKW.Strombedarfsdeckung`:

| Projekt | R20 | R21 |
|---|---|---|
| 1017 | 5,48 | 5,31 |
| 1024 | 26,22 | 20,94 |
| 1047 | 5,34 | 5,30 |

- 1030 bleibt 9,02 (ungerundet 9,025 → 9,017), 1018 bleibt 0. Alle Zeitreihen sind byte-gleich.

**Neue Basis R21 `Referenzlaeufe/2026-09-26_R21_BhkwDeckung/`:**
- 432 CSV und protokoll.txt.
- Der Einfrierlauf ist mit dem A/B-Lauf 432/432 byte-gleich.
- R20: protokoll.txt per git mv nach `Dokumentation/ueberholt/Referenzbasen/2026-09-26_R20_Zapfprofil/`, der Rest entfernt.
- Archiv-LIESMICH: 38 Basen, 39 Dateien, Tabellenzeile, Abschnitt „Die Basis R20 im Einzelnen“.
- `Referenzlaeufe/LIESMICH`: Aktuelle Basis R21 mit A/B-Tafel und Nachtrag Datenpflege.
- Basisname ersetzt in CLAUDE.md, kern.yml (7), ios.yml (2), `Dokumentation/LIESMICH.md` und Basenhistorie.
- Verweis im Umsetzungskonzept Zapfprofilgenerator (3.4 d) wegen der Linkwache auf das archivierte Protokoll umgestellt.

**Nachweise:**
- Build `WP-Plan.Kern.slnf` (Release): 0 Fehler.
- Windows-Schale (Debug x64): 0 Fehler.
- **Voller Lauf mit Schaltern** (Testhost-Regel eingehalten): Kern 8.267/8.268 (1 übersprungen), UI 6.564/6.564, KiKern 549/549, SpeicherEngine 386/386, SpeicherPlanung 27/28 (1 übersprungen). **0 Fehler.**
- **Referenzlauf 14/14 gegen R21:** GESAMT PASS, 4.610.207 Werte, 432/432 CSV byte-gleich.
- **1048** (Prüfprojekt ohne Referenzrolle) rechnet: 1/1 erfolgreich, PV 13,43 MWh, Deckung 20,21 %.
- **ChartProben:** 183 Hashes, gleich mit der Gate-Messlatte `messlatte_windows.sha256`: **JA**, alle grün.

**Aufwand Phase 1:** rund 5 h (davon rund 1,5 h Testläufe).

**Abnahme A‑E30‑1:**
1. 1030, Kostenverwaltung Betriebskosten:
   - „Wartung BHKW“ 18.000 €/a und „Vollwartung / Wartung Kessel“ 2.000 €/a stehen als fester Jahresbetrag.
   - Die Altzeilen „BHKW“ und „Heizkessel“ sind weg; die Summe ist 20.000 €/a.
   - Die Hilfsenergiezeilen stehen auf „% des Endenergiebedarfs“.
2. BHKW-Wirtschaftlichkeit von 1030:
   - Hilfsenergieanteil 2 % an beiden BHKW setzen und die Wirtschaftlichkeit rechnen.
   - Erwartet: Betriebskosten rund +6.208 €/a in den Hilfsenergiezeilen, Herleitung „Satz aus dem Hilfsenergieanteil der Anlage“.
   - Danach den Anteil wieder auf 0 setzen.
3. Simulation 1024:
   - BHKW-Reiter „Strombedarfsdeckung“ 20,94 %.
   - Übersicht: Stromring und Stromtabelle zeigen beim BHKW den Eigenverbrauch.
   - 1018 zeigt keine Deckung über 100 %.

**Restpunkte:**
- **Kostenraster-Satzfeld:** Eine Zeile mit Satz aus dem Anteil zeigt Betrag und Basis, aber ein leeres Satzfeld (der Dialog liest den Satz der Zeile). Das ist ein Anzeige-Folgepunkt.
- **Einspeisung in der Stromtabelle:** Einen Tooltip mit der Einspeisung (Q7 a) gibt es nicht. Die Einspeisung zeigen BHKW-Reiter und Excel (E29).
- **Wiki und Logbuch:**
  - Seite „Kosten“, Hilfsenergie: Ist ein Anteil gepflegt, entstehen die Kosten daraus; eine gepflegte Position hat Vorrang.
  - Seite „Simulation“: Definition der Stromdeckung des BHKW.
  - Zwei Logbuchsätze, die Version beim Anwender erfragen.
- **Papiere (Papierauftrag):**
  - Statuszeile #542 und Protokoll.
  - Register R‑E30 (Q1–Q12).
  - Konzept § 6.2: beide Anker mit Weg, der Kernanker als Altlauf 212.
  - Konzept § 6.3 Nr. 21 (B3/B4/B5/B8 erledigt) sowie § 3.4/§ 4.5 (Hilfsenergie aus Anteil).
  - N10 im Konzept § 3.6/§ 6.3 Nr. 34.
  - Diese Papiere nennen R20 noch als aktuell: Konzept_Gebaeudesimulation:1949, Systementwurf_Gebaeudesimulation:179, Konzept_Wirtschaftlichkeit 2928/3100, Analysepapier 592, Umsetzungskonzept Zapfprofilgenerator 1423.
- **Katalog (Q12):** Die Empfehlung 4–8 % für die Kessel-Hilfsenergie in Weg B ist fachlich zu hoch; das braucht einen eigenen Katalogauftrag.
- **SqlDialektPruefer:** nicht gezogen (Python auf der Testdatenbank ist gesperrt); die neuen SQL-Texte sind einfache SELECTs, kern.yml prüft sie.
- **Hilfsstrom:** geht weiterhin nicht in Emissionen und Stromsteuer ein (Q5, nur benannt).

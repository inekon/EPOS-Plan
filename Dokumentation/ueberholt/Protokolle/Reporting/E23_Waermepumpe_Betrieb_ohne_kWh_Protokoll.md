# E23 — Betriebskosten der Wärmepumpe ohne kWh-Bemessung (Protokoll, 25.09.2026)

Statuszeile #510 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Anlass: der Anwenderentscheid
E20‑Q6 vom 25.09.2026 („E20‑Q6: erläutere / fahre fort“ → nach Empfehlung b: „je kWh elektrisch“ aus der Betriebsauswahl
der Wärmepumpe nehmen, Bestandszeilen schützen), während Phase 1 vom Anwender erweitert: „bei Wärmepumpe fixer
Jahresbetrag (oder % von Investitionskosten), nicht nach kWh/a — weder Strom noch Wärme“. Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 3.2 (Tafel der Runde 1, Fußnote ²), § 6.3 Nr. 10;
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
R‑E23 (neu), R‑E20 (E20‑Q6) und R‑Rest (Nr. 10); Analysepapier
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
§ 5; Mockup `../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`, Kategorie 2 (Zone „Dialog — Betriebskosten“,
Erklärliste unter dem Raster; Ressourcentafel) und Stand-Absatz des Anhangs. Vorgänger:
[`E20_Waermepumpe_kW_elektrisch_Protokoll.md`](E20_Waermepumpe_kW_elektrisch_Protokoll.md) (die Investitionsseite von
Nr. 10 und die Frage E20‑Q6). Zweig `e23` von `7b92780d` (`origin` nach den CI-Nachweisen #502/#503/#506), darin
`7799772b` (Merge `origin` `fe32922c`, nur Papiere E41); Opus 5.5 im Worktree `.claude/worktrees/e23`, zwei Phasen:
Phase 0 nur gelesen und gemessen, Phase 1 nach der Freigabe (25.09.2026, 13:15) `d1fa1d8a` (E23/1), `b41b5232` (E23/2),
`d08446a0` (E23/3). Merge `f7823b8e` („Merge e23: Betriebskosten der Waermepumpe ohne kWh-Bemessung (E20-Q6 b, Strom
und Waerme) (#510)“) auf `pm25` ab `origin` `c48de9e0` (#509), ohne Konflikt. Basis `2026-09-25_R16_Anlagenprio`
(vierzehn Projekte). **Kein Schemaschritt, im Bestand keine Rechenwirkung** — Anker und Referenzlauf byte-gleich; der
Schemastand bleibt 142 (Zielversion 142).

## Befund vor der Welle (Phase 0)

1. **Wo die Art als passend geführt wird.** „je kWh elektrisch“ an der Wärmepumpe im Betrieb kommt aus
   `WirtschaftlichkeitCtrl.BasisGrund` (`:7980–7986`, Art LAUF für Wärmepumpe, Photovoltaik, Stromspeicher, BHKW und
   Elektrokessel; der Schalter `investition` spielt dort keine Rolle); `KostenVorlagenCtrl.PasstZuGewerk` (`:841`, alles
   außer GEWERK passt), `Auswahl` (`:878–892`, die Wärmepumpe in `AUSWAHLFILTER_GEWERKE`); der Katalog führt die Art nur
   im Betrieb (`:689`); einziger Aufrufer ist `KostenKomponenteHuelle.BemessungenBauen` (`:895`); die KI-Sicht nimmt
   dieselbe Liste (`KostenKomponenteDaten.cs:232`).
2. **Bestandsschutz.** `Auswahl` nimmt jede Art, die eine Zeile trägt, zuerst auf (`benutzt`, `:885`) — im Projekt- und
   im Vorlagenkontext.
3. **Die Rechnung hängt nicht an der Landkarte.** `RueckfallMenge` (`:7768ff`) und `FrischeBasis` (`:8126ff`) holen die
   Menge über `StromgroesseKwh`; `BasisGrund` wirkt nur bei fehlender Menge (`KostenProjektPositionenCtrl.cs:339/:491`);
   `BetriebskostenCtrl.cs:125` ist unabhängig. Eine Bestandszeile rechnet also weiter, wenn die Landkarte sperrt.
4. **Keine Daten betroffen.** Testdatenbank: die Art nur in 3 Zeilen am BHKW (Kategorie 2, 1018 und 1030), keine an der
   Wärmepumpe; Live-DB (lesend): 6 Zeilen am BHKW (1027, 1032, 1057, 1068), keine an der Wärmepumpe; Vorlagen nur Nr. 54
   „Wartung BHKW“ (Vorlage 11); Saat `SchemaKatalog.cs:3072` nur am BHKW — Referenzlauf byte-gleich erwartet.
5. **Tests, die die Regel festhalten.** `BemessungsauswahlJeGewerkTests.cs:64–95` (die Betriebsliste der Wärmepumpe
   enthält die Art; `PruefeFehlende` `:445ff` verlangt den Grund GEWERK für fehlende Arten — eine Sperre allein in der
   Auswahl bräche diese Wache); `PufferspeicherVolumenbemessungTests.cs:135–202` (Kreuztafel);
   `BetriebskostenBemessungsmatrixTests.cs:367` (1026 Wärmepumpe kWh el. „ja“ — Nachweis, dass eine Bestandszeile
   rechnet).

## Ergänzung des Anwenders (25.09.2026, ~13:20, während Phase 1)

„E20‑Q6: Änderung zu Betriebskosten — bei Wärmepumpe fixer Jahresbetrag (oder % von Investitionskosten), nicht nach
kWh/a (weder Strom noch Wärme).“ Folge für den Bau: Sperre im Betriebsraster der Wärmepumpe für beide kWh-Arten
(`EUR_PRO_KWH_ELEKTRISCH` und `EUR_PRO_KWH_THERMISCH`); erlaubt bleiben fester Jahresbetrag und % der Investition;
„% der Endenergiekosten“ vorerst erlaubt (E23‑Q6, Empfehlung a); der Vermerk allgemein „Betriebskosten der Wärmepumpe
werden nicht je kWh bemessen“ statt „Strom-kWh sind Energiekosten“; das Investitionsraster unverändert (E20).

## Gebaut

- **E23/1 — Landkarte** (`d1fa1d8a`): `WirtschaftlichkeitCtrl.BasisGrund` führt die Wärmepumpe nicht mehr in den Listen
  für `EUR_PRO_KWH_ELEKTRISCH` und `EUR_PRO_KWH_THERMISCH` → Grund GEWERK in beiden Rastern; `BemessungKatalog.Auswahl`,
  die KI-Wahlliste und die Kreuztafel folgen ohne eigenen Eingriff; `KostenVorlagenCtrl.cs` nur Kommentar;
  `RueckfallMenge` und `FrischeBasis` unverändert — eine Bestandszeile bleibt über `benutzt` wählbar und rechnet aus dem
  Lauf (E23‑Q1 a, Q2 a, Q5 a).
- **E23/2 — Vermerk an der Herleitung** (`b41b5232`): `KostenHerleitung.IstAltbestandWpKwh(bemessung, komponentenId)`
  trifft an der Wärmepumpe „je kWh elektrisch“, „je kWh thermisch“ und den Altwert „je kWh“; `Bilde` hängt den Vermerk
  mit „ · “ an die Herleitungszeile oder setzt ihn allein, wenn die Bezugsgröße fehlt — nur im Projektmodus, nicht im
  Investitionsraster, nicht an anderen Gewerken (E23‑Q3 a, Q4 a); Schlüssel und Designer.
- **E23/3 — Tests** (`d08446a0`): neu `WaermepumpeBetriebKwhSperreTests` mit 8 Fällen (Landkarte GEWERK in beiden
  Rastern; die übrigen Gewerke behalten LAUF; die Betriebsliste der Wärmepumpe ohne kWh-Arten, mit Jahresbetrag, % der
  Investition, % der Endenergiekosten und % des Endenergiebedarfs; das Investitionsraster mit „je kW elektrisch“;
  Bestandsschutz über `benutzt`; an der Wärmepumpenzeile 101600568 von 1026 liefert `FrischeBasis` für beide kWh-Arten
  eine Menge; der Vermerk mit und ohne Bezugsgröße, nicht im Stamm, nicht in der Investition, nicht am BHKW, nicht bei
  den erlaubten Arten; Englisch); `BemessungsauswahlJeGewerkTests` (Betriebsliste der Wärmepumpe ohne beide kWh-Arten);
  `BetriebskostenBemessungsmatrixTests` (1026 Wärmepumpe kWh „ja“ bleibt, Kommentar: der Altbestand rechnet).

## Schlüssel

**1 neu** (de/en): `KDLG_HERL_ALTBESTAND_WP_KWH` „Altbestand — Betriebskosten der Wärmepumpe werden nicht je kWh
bemessen“ (en „legacy entry — heat pump operating costs are not measured per kWh“). Der Schlüssel aus dem
Phase‑0-Vorschlag, `KDLG_HERL_ALTBESTAND_STROM_KWH` („Altbestand — Strom-kWh sind Energiekosten“), ist mit der Ergänzung
des Anwenders entfallen. Je Sprache auf dem Merge 11.184 → 11.185 Einträge, der Designer 11.181 → 11.182 Eigenschaften,
wiederholbar (+0).

## Fragen aus der Welle

E23‑Q1…Q5 stellt der Phase‑0-Bericht; entschieden hat sie der Orchestrator am 25.09.2026 (13:15) mit der Baufreigabe,
nach Empfehlung — alle a. E23‑Q6 entstand mit der Ergänzung des Anwenders, E23‑Q7 im Phase‑1-Bericht; **beide sind offen
beim Anwender**, gebaut ist jeweils die Empfehlung a (→ Register R‑E23).

| Frage | Lesarten | Entscheid |
|---|---|---|
| **E23‑Q1** Ort der Sperre | (a) in der Landkarte `BasisGrund` (Grund GEWERK); (b) eine eigene Sperrliste in `Auswahl` | a |
| **E23‑Q2** Grund einer Bestandszeile ohne Lauf | (a) „passt nicht zu diesem Gewerk“ hinnehmen; (b) ein Sonderfall LAUF | a |
| **E23‑Q3** Vermerk | (a) an der Herleitungszeile im Kern; (b) eine eigene Hinweiszeile mit Hüllenfeld; (c) still | a |
| **E23‑Q4** Geltungsbereich des Vermerks | (a) nur im Projektmodus; (b) auch in den Kostenvorlagen | a |
| **E23‑Q5** Umstellungsangebot | (a) keins; (b) die Bestandszeile auf „% des Endenergiebedarfs“ umstellen | a |
| **E23‑Q6** „% der Endenergiekosten“ im Betriebsraster der Wärmepumpe | (a) lassen; (b) sperren | **offen beim Anwender** — Empfehlung a, gebaut a |
| **E23‑Q7** „% des Endenergiebedarfs“ (die Standardvorlage 13 sät sie für die Hilfsenergie der Wärmepumpe, Live-DB 4 Zeilen), „je kW Leistung“ und „je kW Heizleistung“ an der Wärmepumpe | (a) wählbar lassen — kein kWh/a-Satz; (b) auch sperren, dann Vorlage 13 und Schemaschritt 94 umstellen | **offen beim Anwender** — Empfehlung a, gebaut a |

## Abweichungen und Befunde

1. **Der Anwenderentscheid wurde während Phase 1 erweitert.** Der Auftrag sperrte allein „je kWh elektrisch“ mit dem
   Vermerk „Strom-kWh sind Energiekosten“; gebaut ist die Sperre beider kWh-Arten mit dem allgemeinen Vermerk. Die
   Prozent- und Leistungsbemessungen, die an der Wärmepumpe bleiben, stehen als E23‑Q6 und E23‑Q7 zur Bestätigung.
2. **„je Stunde“ an der Wärmepumpe ist ein Altwert.** Die Wiki-Quelle Kosten nannte unter `laufgroessen` „je Stunde an
   den Betriebsstunden der Wärmepumpe“; der Katalog bietet „je Stunde“ in keinem Raster an (`KostenVorlagenCtrl.cs:732`,
   nur für Bestandszeilen), und an der Wärmepumpe trägt keine Zeile die Art. Die Quelle ist mit den Papieren zu #510
   nachgezogen, unabhängig von E23. Am BHKW nennt die Quelle „je Stunde“ weiter — dort ebenso ein Altwert, eine eigene
   Frage, nicht Teil von E23.
3. **Der Altwert „je kWh“** (`EUR_PRO_KWH`) zählt beim Vermerk mit; zur Auswahl steht er ohnehin nicht.
4. **Kein Altbestand.** Lesend geprüft: an der Wärmepumpe keine Zeile je kWh elektrisch, je kWh thermisch, „je kWh“
   oder „je Stunde“ in Testdatenbank, Live-DB und Vorlagen. Die Betriebsvorlage „Standard“ der Wärmepumpe (ID 13) trägt
   JAHRESBETRAG, PROZENT_INVESTITION und für die Hilfsenergie PROZENT_ENDENERGIEBEDARF; die Live-DB trägt an der
   Wärmepumpe in Kategorie 2 JAHRESBETRAG (10), PROZENT_INVESTITION (10), PROZENT_ENDENERGIEBEDARF (4) und
   PROZENT_ENDENERGIEKOSTEN (3). Der Vermerk „Altbestand“ zeigt sich deshalb heute nur an einer Arbeitskopie.
5. Die KI-Sicht nimmt dieselbe Liste und erbt die Sperre; kein Bericht zeigt eine Bemessungsliste.

## Nachweis

- **Im Bestand ohne Rechenwirkung:** Anker unberührt; Referenzlauf 14/14 gegen `2026-09-25_R16_Anlagenprio` PASS,
  4.610.207 Werte, 432/432 CSV byte-gleich; kein SQL; Testdatenbank unverändert.
- **Phase 1** (Worktree `e23`, auf `d08446a0`): Kern-Filter 0 Fehler, Windows-Schale x64 0 Fehler; Designer 11.182
  (+0 im zweiten Lauf); gefiltert 212/212 (13 Klassen); voller Lauf 14.379 bestanden / 0 Fehler / 2 übersprungen
  (EPOS.Kern 7.121 und 1 übersprungen, EPOS.UI 6.296, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 und 1
  übersprungen); Testhost-Regel eingehalten.
- **Merge** `f7823b8e` auf `pm25` über `c48de9e0` ohne Konflikt (Baum gleich `git merge-tree` der beiden Eltern).
- **Gate:** Kern-Filter 0 Fehler, ChartProben 161/161 gleich der Windows-Messlatte, voller Lauf 14.379 bestanden / 0 Fehler / 2 übersprungen (EPOS.Kern 7.121 und 1 übersprungen, EPOS.UI 6.296, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 und 1 übersprungen), Dokumentationswachen 29/29 (`GATE510.log`, 25.09.2026 13:02–13:10 Uhr, auf `f7823b8e`); Referenzlauf 14/14 gegen R16 byte-gleich aus dem Bau (Worktree e23)
- **CI:** steht aus (Beobachtung nach dem Push)

## Abnahme am Gerät (A‑E23‑1, Windows)

1. **Betrieb:** Projekt 1026 → Kostenverwaltung, Wärmepumpe, Betriebskosten — die Klappliste führt „je kWh elektrisch“
   und „je kWh thermisch“ nicht, wohl aber fester Jahresbetrag, % der Investition, % der Endenergiekosten, % des
   Endenergiebedarfs, je kW Leistung und je kW Heizleistung.
2. **Investition:** die Investitionskosten der Wärmepumpe unverändert, mit „je kW elektrisch“.
3. **Bestandszeile:** eine Zeile je kWh thermisch (Arbeitskopie) bleibt in der Liste und rechnet; die Herleitung lautet
   „× … kWh · … · Altbestand — Betriebskosten der Wärmepumpe werden nicht je kWh bemessen“; ohne Lauf steht der Vermerk
   allein, dazu der Grund „passt nicht zu diesem Gewerk“.
4. **Andere Gewerke:** BHKW und Kessel bieten ihre kWh-Arten weiter an, ohne Vermerk.
5. **Englische Oberfläche:** „legacy entry — heat pump operating costs are not measured per kWh“.

## Logbuch

Im Update-Papier (Version 1.2.0.4, Sammel-Upload 26.09.2026), Stichwort `kosten`: „Die Betriebskosten der Wärmepumpe
werden nicht mehr je kWh bemessen; zur Wahl stehen fester Jahresbetrag, Prozentbemessungen und je kW.“

## Papiere mit der Statuszeile

Konzept (Kopf mit Codestand `f7823b8e`, E23 ohne Schritt; Schrittabsatz; § 3.2 Tafel der Runde 1 mit den Zeilen
`EUR_PRO_KWH_ELEKTRISCH` und `EUR_PRO_KWH_THERMISCH` an der Wärmepumpe, „gesperrt (GEWERK), Bestandszeile rechnet aus
dem Lauf, Herleitung ‚Altbestand‘“, Satz „je Raster“ und Fußnote ²; § 6.1 Zeile E23; § 6.3 Nr. 10 erledigt; § 7;
Anhang), Register (Kopf, Familientafel, neue Familie R‑E23, R‑E20 Zeile E20‑Q6, R‑Rest Zeile Nr. 10), Analysepapier
(§ 5 Zeile E23 ohne Schemaschritt), Protokoll der Entscheidwege (Kopf von § 8, § 8.46, § 8.47), Mockup (Kategorie 2:
Erklärliste mit den Bemessungen der Wärmepumpe und dem Vermerk „Altbestand“; Ressourcentafel; Stand-Absatz),
Update-Papier und die Wiki-Quelle Kosten (Anker `laufgroessen`: die Wärmepumpe aus den kWh-Aufzählungen, „je Stunde“ an
der Wärmepumpe gestrichen, Satz zur vorhandenen Position mit dem Vermerk „Altbestand“), Index (Reporting 137 → 138).
`Referenzlaeufe/LIESMICH.md` bleibt ohne Nachtrag (kein Schemaschritt).

## Offen

- **E23‑Q6 und E23‑Q7** — Anwenderentscheid: die Prozent- und Leistungsbemessungen an der Wärmepumpe lassen (a,
  Empfehlung, gebaut) oder sperren (b; bei Q7 mit Umstellung der Vorlage 13 und des Schemaschritts 94).
- **Abnahme am Gerät** A‑E23‑1 (fünf Schritte oben).
- **Gate** und **CI** (Nachweis oben).
- Der **Wiki-Sammel-Upload** am 26.09.2026 (Version 1.2.0.4, freigegeben).

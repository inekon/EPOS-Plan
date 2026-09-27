# E23 — Bericht Phase 0 (Opus, 25.09.2026 ca. 13:10, Worktree e23 ab 7b92780d; origin inzwischen fe32922c, nur Papiere E41)

Befund: „je kWh elektrisch" an der WP im Betrieb kommt aus `WirtschaftlichkeitCtrl.BasisGrund` (`:7980–7986`, Art LAUF für WP, PV, Stromspeicher,
BHKW, Elektrokessel; Schalter `investition` ohne Rolle); `KostenVorlagenCtrl.PasstZuGewerk` (`:841`, alles außer GEWERK = passt), `Auswahl`
(`:878–892`, WP in `AUSWAHLFILTER_GEWERKE`); Katalog `:689` nur Betrieb; einziger Aufrufer `KostenKomponenteHuelle.BemessungenBauen` (`:895`);
KI-Sicht nimmt dieselbe Liste (`KostenKomponenteDaten.cs:232`). Bestandsschutz: `Auswahl` nimmt jede von einer Zeile getragene Art zuerst auf
(`benutzt`, `:885`), im Projekt- und Vorlagenkontext. Rechnung hängt nicht an der Landkarte: `RueckfallMenge` (`:7768ff`), `FrischeBasis`
(`:8126ff`) holen die Menge über `StromgroesseKwh`; `BasisGrund` nur bei fehlender Menge (`KostenProjektPositionenCtrl.cs:339/:491`);
`BetriebskostenCtrl.cs:125` unabhängig. **Keine Daten betroffen:** Testdatenbank nur 3 Zeilen am BHKW (7, Kat. 2, 1018/1030), keine an der WP;
Live-DB (lesend) 6 Zeilen am BHKW (1027, 1032, 1057, 1068), keine an der WP; Vorlagen nur Nr. 54 „Wartung BHKW" (Vorlage 11); Saat
`SchemaKatalog.cs:3072` nur BHKW → Referenzlauf byte-gleich erwartet. Tests: `BemessungsauswahlJeGewerkTests.cs:64–95` (WP-Betriebsliste enthält
die Art; `PruefeFehlende` `:445ff` verlangt Grund GEWERK für fehlende Arten → Sperre nur in der Auswahl bräche die Wache),
`PufferspeicherVolumenbemessungTests.cs:135–202` (Kreuztafel bleibt grün), `BetriebskostenBemessungsmatrixTests.cs:367` (1026 WP kWh el. „ja"
bleibt grün = Nachweis Bestandszeile rechnet), Elektrokessel/KostenHerleitung/Leistungsbemessung unberührt.
Vorschlag: E23/1 `BasisGrund` WP aus der Liste „je kWh elektrisch" → GEWERK (Kommentar E20‑Q6 b); Auswahl, KI-Liste, Kreuztafel folgen;
`RueckfallMenge`/`FrischeBasis` unverändert (Bestandszeile rechnet, über `benutzt` wählbar). E23/2 `KostenHerleitung.Bilde/Zeilentext` Vermerk bei
WP + Betrieb + kWh el., auch ohne Bezugsgröße; Schlüssel `KDLG_HERL_ALTBESTAND_STROM_KWH` de „Altbestand — Strom-kWh sind Energiekosten" / en
„legacy entry — electricity kWh are energy costs", Designer +1. E23/3 Tests (WP-Betriebsliste ohne die Art; Fall Bestandszeile bleibt/rechnet
1026/Vermerk, BHKW ohne Vermerk; Kommentare an Kreuztafel und Matrixfall). Aufwand 1,5–2 h, kein Schema, kein SQL, Risiko null.
Fragen (Entscheid Orchestrator 25.09.2026 nach Empfehlung, alle a): Q1 Sperre in der Landkarte (b eigene Sperrliste in Auswahl); Q2 Grundtext
„passt nicht zu diesem Gewerk" für Bestandszeile ohne Lauf hinnehmen (b Sonderfall LAUF); Q3 Vermerk an der Herleitungszeile im Kern (b eigene
Hinweiszeile mit Hüllenfeld; c still); Q4 nur Projektmodus (b auch Vorlagen); Q5 kein Umstellungsangebot (b auf „% des Endenergiebedarfs").
Bau freigegeben 25.09.2026 13:15 (vorher Merge origin fe32922c).

**Ergänzung Anwender 25.09.2026 ~13:20:** Betriebskosten der WP nur fixer Jahresbetrag oder % der Investition, nicht nach kWh/a (weder Strom noch
Wärme) → Sperre für kWh elektrisch UND kWh thermisch im WP-Betriebsraster; % Endenergiekosten vorerst erlaubt (E23‑Q6 a); Altbestand-Vermerk
allgemein „Betriebskosten der Wärmepumpe werden nicht je kWh bemessen". An den Agenten gesendet (Phase 1 läuft).

# E23 — Bericht Phase 1 (Opus, 25.09.2026 ca. 13:45, e23 = drei Commits auf 7799772b [Merge fe32922c])

Commits: 7799772b Merge origin fe32922c (nur Papiere); d1fa1d8a E23/1 Landkarte `WirtschaftlichkeitCtrl.BasisGrund`: WP nicht mehr in den Listen
für EUR_PRO_KWH_ELEKTRISCH und EUR_PRO_KWH_THERMISCH → GEWERK in beiden Rastern; `KostenVorlagenCtrl.cs` nur Kommentar; Auswahl/KI-Wahlliste folgen;
`RueckfallMenge`/`FrischeBasis` unverändert (Bestandszeile rechnet); b41b5232 E23/2 `KostenHerleitung.IstAltbestandWpKwh(bemessung, komponentenId)`
(trifft kWh elektrisch, kWh thermisch und Altwert „je kWh" an der WP), `Bilde` hängt Vermerk mit „ · " an oder allein ohne Bezugsgröße, nur
Projektmodus, nicht Investition, nicht andere Gewerke; E23/3 Tests (letzter Commit): neu `WaermepumpeBetriebKwhSperreTests` 8 Fälle (Landkarte
GEWERK beide Raster; übrige Gewerke LAUF; Betriebsliste ohne kWh-Arten, mit Jahresbetrag, % Investition, % Endenergiekosten, % Endenergiebedarf;
Investitionsraster mit je kW elektrisch; Bestandsschutz `benutzt`; 1026 WP-Zeile 101600568 rechnet über FrischeBasis; Vermerk mit/ohne Basis, nicht
Stamm/Investition/BHKW/erlaubte Arten; Englisch); `BemessungsauswahlJeGewerkTests` WP-Betriebsliste ohne beide kWh-Arten;
`BetriebskostenBemessungsmatrixTests` 1026 WP kWh „ja" bleibt (Kommentar Altbestand rechnet). Schlüssel `KDLG_HERL_ALTBESTAND_WP_KWH` de „Altbestand —
Betriebskosten der Wärmepumpe werden nicht je kWh bemessen" / en „legacy entry — heat pump operating costs are not measured per kWh"; Designer
11.181 → 11.182 (+0 zweiter Lauf). Prüfungen: Kern-Filter 0, Windows-Schale x64 0; gefiltert 13 Klassen 212/212; voll 14.379 / 0 / 2 (Kern 7.121+1,
UI 6.296, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27+1); Referenzlauf 14/14 gegen R16 PASS 4.610.207 Werte, 432/432 byte-gleich; kein SQL;
Testhost-Regel eingehalten. Bestand (lesend): keine Zeile je kWh elektrisch/thermisch/„je kWh"/„je Stunde" an der WP in Testdatenbank, Live-DB,
Vorlagen (WP-Betriebsvorlage „Standard" ID 13: JAHRESBETRAG, PROZENT_INVESTITION, Hilfsenergie PROZENT_ENDENERGIEBEDARF; Live-DB WP Kat. 2:
JAHRESBETRAG 10, PROZENT_INVESTITION 10, PROZENT_ENDENERGIEBEDARF 4, PROZENT_ENDENERGIEKOSTEN 3) → kein Altbestand.
Fragen (gebaut a): **E23‑Q6** % Endenergiekosten an der WP im Betrieb lassen (a, Empfehlung) / sperren (b); **E23‑Q7 neu:** an der WP bleiben „% des
Endenergiebedarfs" (Standardvorlage 13 sät sie für WP-Hilfsenergie, Live-DB 4 Zeilen), „je kW Leistung", „je kW Heizleistung" wählbar — a lassen
(kein kWh/a-Satz, Empfehlung) / b auch sperren (dann Vorlage 13 und Schemaschritt 94 umstellen).
Erledigt-Gründe: § 6.3 Nr. 10 vollständig (Investition je kW thermisch/elektrisch [E20]; Betrieb der WP nicht je kWh: Jahresbetrag, % Investition,
% Endenergie, je kW [E23]; Satz :2942 „bietet weiter je kWh elektrisch" fällt weg); § 3.2 Tafel Zeilen EUR_PRO_KWH_ELEKTRISCH/THERMISCH × WP
„gesperrt (GEWERK), Bestandszeile rechnet aus dem Lauf, Herleitung Altbestand"; Wiki Kosten Tafel :30–46 WP-Betrieb ohne kWh-Arten; Wiki Kosten :65
(`laufgroessen`) WP aus den Listen kWh thermisch/elektrisch streichen + Satz „Die Betriebskosten der Wärmepumpe werden nicht je kWh bemessen; eine
vorhandene Position … rechnet weiter und trägt den Vermerk ‚Altbestand'"; dort „je Stunde" an der WP = Altwert (Wiki nachziehen, unabhängig von
E23). Logbuchsatz (`kosten`): „Die Betriebskosten der Wärmepumpe werden nicht mehr je kWh bemessen; zur Wahl stehen fester Jahresbetrag,
Prozentbemessungen und je kW." Abnahme A‑E23‑1: (1) 1026 WP Betriebskosten: Liste ohne je kWh elektrisch/thermisch, mit Jahresbetrag, % Investition,
% Endenergiekosten, % Endenergiebedarf, je kW Leistung, je kW Heizleistung; (2) Investition unverändert mit je kW elektrisch; (3) Bestandszeile je
kWh thermisch (Arbeitskopie) bleibt in der Liste, rechnet, Herleitung „× … kWh · … · Altbestand — …", ohne Lauf Vermerk allein + Grund „passt
nicht zu diesem Gewerk"; (4) BHKW/Kessel kWh-Arten weiter ohne Vermerk; (5) Englisch.

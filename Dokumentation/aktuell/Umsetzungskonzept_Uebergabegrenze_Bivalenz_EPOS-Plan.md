# Umsetzungskonzept: Übergabegrenze und Bivalenz der Wärmepumpe in EPOS-Plan

**Auftrag des Anwenders vom 08.10.2026** („Erstelle ein Umsetzungskonzept zur Integration in EPOS-Plan") ·
**Stand 08.10.2026 — Fassung 1, Umsetzungsentwurf, zur Abnahme durch den Anwender; Entscheide U‑1, U‑2 und U‑4
vom 08.10.2026 eingearbeitet** · Codestand `b1a34adca`
(`origin/ios_migration_september`) · Schemastand 201 (`Ak3KSchema`) · Referenzbasis
`2026-10-07_R43_Kaelteseite_AK3K` · Fachkonzept
[`Konzept_Uebergabegrenze_Bivalenz_EPOS-Plan.md`](Konzept_Uebergabegrenze_Bivalenz_EPOS-Plan.md) (Fassung 2) ·
Mockup `Mockups/Waermepumpe_Bivalenz_Uebergabe.html`.

**Geltung und Abgrenzung.** Das Fachkonzept sagt, *was* gerechnet wird (Rechenweg UB‑a bis UB‑d, Datenmodell,
Bedienung, Vorgabewerte, Prüfungen, Fragen UB‑Q1 bis UB‑Q11). Dieses Papier sagt, *wie, wo, in welcher Reihenfolge
und mit welcher Abnahme* es gebaut wird. Es wiederholt keinen Fachinhalt; Verweise der Form „FK 4.3" zeigen auf
Abschnitte des Fachkonzepts. Eigene Ableitungen sind mit „(Abl.)" gekennzeichnet. Wo dieses Papier vom Fachkonzept
abweicht, steht das ausdrücklich in Abschnitt 1 (U‑1 bis U‑4); U‑1, U‑2 und U‑4 sind entschieden, U‑3 liegt mit den
Fragen UB‑Q1 bis UB‑Q11 beim Anwender.

---

## 1 Zielbild und Grundsätze

1. **Der Rechenweg steht einmal — im Kern.** Übergabegrenze, Betriebsbereiche, Hydraulik- und Rücklaufgrenze sind
   Klassen in `EPOS.Kern/Allgemein/Simulation/Bivalenz/`; Profilweg (AK1/AK2), geschlossener Kreis (AK3),
   Herleitungszeile des Dialogs, Bivalenzdiagramm und Bericht rufen dieselben Klassen. Keine Rechnung in Razor,
   Hülle oder Windows-Schale (Regel `EPOS.Kern` in [`CLAUDE.md`](../../CLAUDE.md)).
2. **Leere Felder rechnen wie heute.** Jede neue Spalte ist nullbar; NULL ist der Bestandsweg. Kein
   Referenzprojekt außer dem neuen 1060 ändert sein Ergebnis — der Referenzlauf gegen R43 ist für die 24
   Bestandsprojekte byte-gleich bzw. innerhalb der Toleranz unverändert. Das ist schärfer als FK 5.3 (dort ändern
   1047, 1054, 1056, 1058) und wird über U‑1 erreicht (entschieden 08.10.2026: a, Opt-in).
3. **Offene Entscheide werden zu Umsetzungsvorgaben.** Gebaut wird nach der Empfehlung des Fachkonzepts; jede
   Gegenoption bleibt über ein Feld oder einen Schalter erreichbar, ohne Umbau (Tafel unten).
4. **Plattformfrei und deterministisch.** Feste Iterationszahl, Startwert aus der Vorstunde, keine Parallelsummen;
   iOS erbt den Rechenweg ohne eigene Zeile (die iOS-Hülle wird nicht berührt, kein iOS-Lauf nötig).

**Umsetzungsvorgaben zu den offenen Fragen (FK 10).**

| Frage | gebaut nach | Weg zur Gegenoption ohne Umbau |
|---|---|---|
| UB‑Q1 Vorwärmbetrieb | a: Schalter `Vorwaermbetrieb` je Anlage | b: Schalter bleibt 0 und wird im Dialog ausgeblendet (Ressourcenschalter der Gruppe); c bleibt benannt abgelehnt |
| UB‑Q2 Ort der Gerätegrenzen | a: `Tab_WP`/`Tab_WP_STAMM` | b: die Leser gehen über `Geraetegrenzen.Lesen(wp, anlage)`; ein späteres Anlagenfeld wird dort vorrangig gelesen |
| UB‑Q3 Rücklaufgrenze R744 | b: Abwertung über `Ruecklauf_Abwertung_ProzentJeK` | a: Abwertung leer bzw. 0 und `Ruecklauf_Max` 35 °C — dieselben Felder |
| UB‑Q4 Abschaltpunkt | a: Deckel, maßgebend der wärmere Punkt | b/c: eine Regelfunktion `Bivalenzrechner.Massgebend(...)` mit Aufzählung `AbschaltpunktRegel` (Konstante, kein Feld) |
| UB‑Q5 Einbindung | a: direkt und Weiche gerechnet, Puffer als benannte Näherung | b: `WEICHE` im Auswahlfeld ausblenden; c: eigener Zweig in `Hydraulikgrenze`, Schema unverändert |
| UB‑Q6 Vorgabewerte | a: Tafel FK 6.3 als `Bivalenzvorgaben` (eine Klasse, Quelle je Wert im Kommentar) | b: Einträge auf NULL setzen — die Herleitungszeile sagt „ohne Vorgabe" |
| UB‑Q7 Bericht und Bild | a: Bild, Tafel, Kacheln (UB‑E4) | b/c: UB‑E4 teilen bzw. verschieben; UB‑E1–E3 hängen nicht daran |
| UB‑Q8 Referenzprojekt | a: 1060 als Kopie von 1056, in die CI-Auswahl | b: Projektliste in `kern.yml` ohne 1060; Wache und Basis bleiben |
| UB‑Q9 BHKW-Rücklaufgrenze | a: `Ruecklauf_Max` an `Tab_BHKW(_STAMM)` in UB‑E3 | b: Spalten bleiben leer, Leser entfällt (die Spalte schadet leer nicht) |
| UB‑Q10 Mindestrücklauf Kessel | a: Prüfhinweis im Bericht ohne Feld | b: späterer Schemaschritt |
| UB‑Q11 Stufe „Vorwärmer" | a: in UB‑E2 | b: Stufe bleibt ungenutzt, solange `Vorwaermvorlauf` NaN ist |

**Abweichungen vom Fachkonzept** (U‑1, U‑2, U‑4 entschieden am 08.10.2026; U‑3 offen).

- **U‑1 Wirksamkeit an `Einbindung` gebunden (Abl.). Entschieden 08.10.2026: a (Opt-in).** Die Bereichsrechnung B0–B4, die Hydraulikgrenze und die
  abgeleitete Rücklaufgrenze wirken nur für eine Wärmepumpe, deren `Einbindung` gesetzt ist. `Einbindung` NULL ist
  der Bestandsweg (Kennfeld am Vorlauf wie heute); die Herleitungszeile sagt „Einbindung nicht gesetzt —
  Übergabegrenze ruht". Neue Anlagen bekommen die Einbindung beim Anlegen vorbelegt (FK 5.1: `PUFFER` bei
  Pufferzuordnung, sonst `DIREKT`). **Folgen:** keine Ergebnisänderung bei 1047, 1054, 1056, 1058 und den übrigen
  Bestandsprojekten; ihre Werte in R43 bleiben gültig, die Folgebasis R44 kommt allein um 1060 hinzu (4.4, 8.3).
  Gegenoption (FK 5.3, B4 für alle gekoppelten Anlagen), nicht beauftragt: ein späterer Datenschritt belegt
  `Einbindung` in Bestandszeilen — dann neue Basis mit Erklärung je Projekt nach FK 8.3.
- **U‑2 Katalogfeld `Kaeltemittel`. Entschieden 08.10.2026.** `Tab_WP` trägt `Typ` (Luft-, Sole-, Wasser-Wasser)
  und `Bauart` (Split, Monoblock), bisher kein Kältemittel (Befund an der Testdatenbank, 26 bzw. 28 Spalten). Neue
  Gerätespalte `Kaeltemittel` an `Tab_WP` und `Tab_WP_STAMM`: TEXT, nullbar, **ohne** CHECK-Liste — die Werteliste
  führt der Kern als Klappliste in `Bivalenzvorgaben`, damit ein neues Kältemittel ohne Schemaschritt hinzukommt.
  Codes `R410A`, `R32`, `R290`, `R744`, `R134a`, `R1234ze(E)`, `R407C`, `R454C`, `R455A`, `R1233zd(E)`,
  `SONSTIGES`; Anzeige in beiden Sprachen. Wirkung: (1) Die Schnellwahl speichert die Klasse und füllt **leere**
  Felder mit den Vorgaben der Klasse (Höchstvorlauf, Spreizungen, Rücklaufgrenze, bei R744 Bezugsrücklauf und
  Abwertung) — Herkunft „Vorgabe nach Kältemittel"; gefüllte Felder bleiben. (2) Leeres `Kaeltemittel` rechnet mit
  den allgemeinen (unterkritischen) Vorgaben wie bisher. (3) Katalogabgleich, Projektpaket,
  Auslieferungsvorlage/Katalogpaket und Duplizieren behandeln die Spalte wie die übrigen neuen Gerätespalten (4.2,
  4.3). (4) Der VDI‑3805-Import liest kein Kältemittel; das Feld bleibt beim Import leer (4.3). Denselben
  Spaltennamen trägt `Tab_Kaeltemaschine` (`KaeltemaschineSchema.SPALTE_KAELTEMITTEL`, freier Text) — gleiche
  Bedeutung (Abl.).
- **U‑3 Namensnähe am BHKW. Empfehlung a; Entscheid des Anwenders offen.** `Tab_BHKW` trägt schon `Vorlauf` und
  `Ruecklauf` (Auslegungspaar des Moduls, 8 von 15 Zeilen belegt). **a (Empfehlung):** der Name `Ruecklauf_Max`
  bleibt — gleicher Name und gleiche Bedeutung „höchster zulässiger Rücklauf, Betriebsgrenze" wie an der
  Wärmepumpe; Abgrenzung zum Auslegungspaar `Vorlauf`/`Ruecklauf` über die Dialogbeschriftung „Höchster Rücklauf
  (Betriebsgrenze)" gegenüber „Rücklauf (Auslegung)", die Prüfregel `Ruecklauf_Max ≥ Ruecklauf` (sonst Hinweis)
  und die Vorgabe bei Neuanlage max(70 °C, Auslegungsrücklauf). **b:** eigener Name `Ruecklauf_Betriebsgrenze`.
- **U‑4 Stufenstunden des Kessels. Entschieden 08.10.2026: Empfehlung.** Die Rücklaufstufen des Kessels stehen
  heute nur im Protokollhinweis `SIMENG_KESSEL_BRENNWERT_BETRIEB`, nicht in einer Ergebnisspalte. Die Stufe
  „Vorwärmer" (`Ruecklaufstufe.Vorwaermer`) kommt dort mit ihrer Stundenzahl hinzu (neuer Platzhalter in beiden
  Sprachen); keine neue Ergebnisspalte am Kessel — eine Ergebnisspalte erst, wenn der Bericht sie verlangt.

## 2 Ausgangslage im Code

| Baustein | Datei:Zeile | heutiges Verhalten | Lücke |
|---|---|---|---|
| Stützstellenwahl | `EPOS.Kern/Allgemein/Simulation/SimulationWaermepumpe.cs:1478` `StuetzstelleWaehlen`, Leser `:1518` | Kennfeld am gerechneten Vorlauf `Heizkreisvorlauf[stunde]`, interpoliert (AK3‑I), über der obersten Stützstelle gehalten | keine Kappung bei θ_WP,max, keine Übergabegrenze |
| Vorlauf der Stunde | `SimulationWaermepumpe.cs:49` `Heizkreisvorlauf` | einziger Temperatureingang aus dem Heizkreis | kein `Heizkreisruecklauf` |
| Betriebsart und Abschaltpunkt | `SimulationWaermepumpe.cs:2165` `AlternativAus`, Abschaltpunktprüfung um `:2509` | teilparallel/alternativ allein an der Außentemperatur | Bereiche B0–B4 fehlen |
| Bivalenzpunkt | `SimulationWaermepumpe.cs:801`, `:1933`, `:2434` | Feld `-100`, gesetzt aus beobachteten Stunden | kein berechneter Punkt |
| Vorlaufangebot | `EPOS.Kern/Allgemein/Simulation/Anlagenfahrplan.cs` `VorlaufAngebotC` | höchstes `Vorlauf_Max` der verfügbaren Erzeuger (Rückfall `Vorlauf`) | Höchstvorlauf ist Angebotsdeckel, keine Gerätegrenze |
| Verfügbarkeitsgründe | `EPOS.Kern/Allgemein/Simulation/Anlagenverfuegbarkeit.cs:12` `Verfuegbarkeitsgrund` | acht Gründe (Sperrzeit … KeinErzeuger) | vier neue Gründe (FK 4.5) |
| Angebot im Kreis (AK3) | `EPOS.Kern/Allgemein/Simulation/Stundenangebot.cs:238` `WaermepumpeKapazitaet`, `:494` `Angebotsfunktion.Angebot` | Kapazität = Kennfeld bei V | Grenze aus Übergabe und Hydraulik |
| Kreis | `EPOS.Kern/Allgemein/Simulation/Anlagenkopplung.cs:656` `Kreis` | liefert `(VorlaufC, RuecklaufC)` wärmemengengewichtet | Rücklauf erreicht die Wärmepumpe nicht |
| Übergabegleichung | `EPOS.Kern/Allgemein/Simulation/Gebaeude/Waermeuebergabe.cs:90` `Uebergabekennwerte`, `:259` `LeistungOffenW`, Newton ≤ 8 Schritte (`:161`) | H2 löst die Übergabe bei Sollvorlauf | keine Lösung bei θ_WP,max |
| Heizkreis des Projekts | `EPOS.Kern/Allgemein/Simulation/Gebaeude/HeizkreisErgebnis.cs:487` `HeizkreisProjekt`; `Gebaeudeheizkreis.cs:74` `Mischen` | Vor-/Rücklauf, W_H je Zone | — (wird gelesen) |
| Kesselrücklauf | `EPOS.Kern/Allgemein/Simulation/Kesselkennlinie.cs:345` `Ruecklauf`, `:514` `Ruecklaufstufe`; `SimulationSPK.cs:2020` `RuecklaufDerStunde`, `:2011` `EtaBrennwert` | Kette Heizkreis → Speicher → Paar → Rückfall | Stufe „Vorwärmer" |
| BHKW | `EPOS.Kern/Allgemein/Simulation/SimulationBHKW.cs`, `BhkwTeillast.cs` | kein Temperaturbezug | Rücklaufgrenze (Option) |
| Gerätespalten zuletzt | `EPOS.Kern/Allgemein/Update/ErzeugerTeillastSchema.cs` (`Mindestleistung_kW`, `Taktverlustfaktor_Cd`) | Muster für Spalten an `Tab_WP(_STAMM)` mit vier Lesern | — (Vorbild) |
| Anlagenspalten zuletzt | `EPOS.Kern/Allgemein/Update/AnlagenfahrplanSchema.cs` (`Zeitprogramm`, `Vorlauf_Max`), `VorlaufwahlSchema.cs` (Stundenzähler an `Tab_ErgebnisWaermepumpeModul`) | Muster für Anlagen- und Ergebnisspalten | — (Vorbild) |
| Katalogfassung | `EPOS.Kern/Allgemein/Katalog/Katalogfassung.cs` `STUFE1` (`Tab_WP_STAMM`, Spaltenliste) | Prüfsumme über die gelisteten Spalten; NULL trägt nichts bei | neue Gerätespalten aufnehmen |
| Dialog | `EPOS.UI/Dialoge/Waermepumpe/WaermepumpeKonfiguration.razor` (Gruppen „Betriebszeiten" `:136`, „Betrieb" `:171`), Feldsatz `WaermepumpeAnlageDaten.cs`, Texte `WaermepumpeKonfigurationTexte.cs` (`WPA_*`) | Bivalenz, Betriebsart, Abschaltpunkt, `Vorlauf_Max` | Gruppe „Bivalenz und Übergabe" |
| Hülle | `WindowsFormsApplication1/Views/Wärmepumpe/WaermepumpeAnlageHuelle.cs` (ruft `EPOS.UI.Daten/Erzeuger/BetriebszeitenAbbildung.cs`, `:625`/`:646`) | Hülle der Wärmepumpe liegt in der Windows-Schale, plattformfreie Abbildungen in `EPOS.UI.Daten` | neue Abbildung nach demselben Muster |
| Stammblatt | `EPOS.UI/Dialoge/Waermepumpe/WaermepumpeStammFelder.razor`, `WaermepumpeStammDaten.cs` | Katalogfelder; die Teillastspalten stehen dort nicht | Gerätegrenzen |
| Diagramm | `EPOS.Kern/Allgemein/Bericht/ChartRenderer.cs:5938` `VorlaufRuecklauf` | Muster Methode + Modell | `Bivalenzdiagramm` |
| Kennzahlen und Bild | `EPOS.Kern/Allgemein/Bericht/KennzahlenKatalog.cs` (`wp.vorlauf.*`), `Vorlagen/Vorlagenfeldkatalog.Bilder.cs` (`stand.bild.waermepumpe_streuwolke`) | Vorlaufwahl ausgewiesen | Bereiche, Bivalenzpunkte, Bild |
| Ergebnisreiter | `EPOS.UI/Seiten/Simulation/WaermepumpeReiter.razor`; Lesen `EPOS.Kern/Controller/ErgebnisCtrl.cs` | Kacheln der Wärmepumpe | Kachelzeile „Betriebsbereiche" |

## 3 Zielarchitektur

**3.1 Neue Kernbausteine** (Ordner `EPOS.Kern/Allgemein/Simulation/Bivalenz/`, alle `internal`, Tests über
`InternalsVisibleTo` wie im Bestand).

| Klasse | Aufgabe | Fachkonzept |
|---|---|---|
| `Bivalenzvorgaben` | Vorgabewerte der Tafel FK 6.3 als Konstanten mit Quelle; Klappliste der Kältemittelcodes (U‑2) und Vorgaben je Kältemittelklasse als Liste (Code, Höchstvorlauf, Spreizungen, Rücklaufgrenze, Bezug, Abwertung); unbekannter Code und `SONSTIGES` → allgemeine Vorgabe | 6.3, UB‑Q6, U‑2 |
| `Geraetegrenzen` | liest die acht Gerätespalten (NULL → Vorgabe der Kältemittelklasse, bei leerem `Kaeltemittel` die allgemeine Vorgabe), liefert σ_A, σ_max, σ_min, ṁ_min, θ_R,grenz samt Herkunft je Wert (`Katalog`/`Vorgabe nach Kältemittel`/`Vorgabe`/`abgeleitet`) | 4.4, 5.1, U‑2 |
| `Uebergabegrenze` | Kalibrierung W_H je Zone (UB‑a) und Nullstelle Φ_UE,max bei θ_WP,max (UB‑b) mit der Newton-Routine von H2 (`Waermeuebergabe`), Startwert aus der Vorstunde; Weichenfall mit gemischtem Vorlauf | 4.2, 4.3 |
| `Hydraulikgrenze` | Φ_Hydraulik für `DIREKT`/`WEICHE`/`PUFFER`, σ_max,eff aus Mindestvolumenstrom, Überströmventil, Taktmarke bei σ < σ_min | 4.4, 2.8 |
| `Ruecklaufgrenze` | θ_R,grenz, Abschaltung, R744-Faktor f(h) auf Leistung und COP; BHKW-Prüfung | 4.4, 2.11, 2.12 |
| `Betriebsbereich` (Aufzählung) und `Bivalenzrechner` | Bereich B0–B4 je Stunde, Leistung der Wärmepumpe, Anteil a, Vorwärmvorlauf; statisch die Bivalenzpunkte θ_biv,1/θ_biv,2 und die Regel `Massgebend` gegen den Abschaltpunkt | 4.5 |
| `Bivalenzherleitung` | eine Zeile und ein Diagrammmodell aus Gebäude- und Gerätedaten, ohne Lauf (Dialog, Bericht) | 6.2, 7.2 |

**3.2 Stundenablauf (Profilweg AK1/AK2 und Kreis AK3).**

1. **Vor dem Lauf:** `Geraetegrenzen.Lesen` je Wärmepumpe; `Uebergabegrenze.Kalibrieren` je Zone aus
   `Uebergabekennwerte` (W_H, Δθ_m,N); `Bivalenzrechner.Punkte` für die Ergebnisspalten (Auslegungsraumtemperatur).
   Ist `Einbindung` NULL oder die Kopplung aus, entsteht kein Bivalenzobjekt — Schritte 3 bis 7 entfallen (U‑1).
2. **Heizkreis der Stunde:** `HeizkreisProjekt` liefert θ_V,soll (vor H1), θ_R und W_H; `Anlagenkopplung.Kreis`
   übergibt `(VorlaufC, RuecklaufC)` an `SimulationWaermepumpe` — neu neben `Heizkreisvorlauf` das Feld
   `Heizkreisruecklauf` (`double[]`, NaN = ohne Kopplung).
3. **Verfügbarkeit:** `Anlagenfahrplan` (Sperrzeit, Zeitprogramm) und Abschaltpunkt wie heute; danach
   `Ruecklaufgrenze.Pruefen(θ_R,WP)` → B0 mit Grund `RUECKLAUF_MAX`.
4. **Grenzen:** `Uebergabegrenze.Loesen(stunde, θ_i der Stunde)` → Φ_UE,max; `Hydraulikgrenze` → Φ_Hydraulik;
   Kennfeld bei min(θ_V,soll, θ_WP,max) über die bestehende Interpolation → Φ_WP,grenz (FK 4.4); R744-Faktor.
5. **Bereich:** `Bivalenzrechner.Bereich(...)` → B1/B2/B3/B4 samt Leistung der Wärmepumpe und Grund
   (`UEBERGABE_HOECHSTVORLAUF`, `SPREIZUNG_MAX`, `SPREIZUNG_MIN`). Profilweg: der Wert ersetzt die
   Kennfeldkapazität in der Kaskadenstunde (`Kaskadenschleife.Rechnen`). AK3: `WaermepumpeKapazitaet` bekommt ein
   optionales `Bivalenzobjekt`; `Angebot(h, V)` ruft dieselbe Regel, die Abhängigkeit von θ_R läuft über die
   Abbruchschwellen des Kreises.
6. **Vorwärmvorlauf:** In B3 schreibt die Wärmepumpe `Vorwaermvorlauf[stunde]` = θ_WP,max (sonst NaN). Die
   Kaskade muss die Wärmepumpe vor dem Kessel rechnen; steht sie dahinter, meldet der Lauf das benannt und rechnet
   die Stunde als B4 (Abl.).
7. **Kessel:** `SimulationSPK.RuecklaufDerStunde` liest `Vorwaermvorlauf`; `Kesselkennlinie.Ruecklauf` erhält den
   Parameter `vorwaermerC` und die Stufe `Ruecklaufstufe.Vorwaermer = 4` (Wert hinten angefügt, Rang vor
   `Heizkreis`); `EtaBrennwert` rechnet daran. Heizstab in B3 wie Kessel in Reihe (FK 4.5).
8. **BHKW (Option UB‑Q9):** `SimulationBHKW` prüft `Ruecklauf_Max` gegen den Heizkreisrücklauf bzw. die unterste
   Pufferzone; darüber liefert das Modul nichts (Grund `RUECKLAUF_MAX`).
9. **Zähler:** Bereichsstunden, Wärme je Bereich, Spreizungs- und Rücklaufzähler je Modul; nach dem Lauf in die
   Ergebnisspalten (3.3).

**3.3 Ergebnisse.**

| Größe | Ort | Leser |
|---|---|---|
| `Bereich_WpAllein_h`, `Bereich_Parallel_h`, `Bereich_Vorwaermung_h`, `Bereich_NurKessel_h` | `Tab_ErgebnisWaermepumpeModul`, Summe `Tab_ErgebnisWaermepumpe` | Reiter, Bericht, Export, KI-Sicht |
| `Bereich_WpAllein_MWh` … `Bereich_NurKessel_MWh` (Wärme der Wärmepumpe) | dto. | dto. |
| `Spreizung_Unterschritten_h`, `Ruecklauf_Ueberschritten_h` | dto. | leise Zeile im Reiter, nur > 0 |
| `Bivalenzpunkt_1`, `Bivalenzpunkt_2`, `Uebergabe_Max_kW` | nur `Tab_ErgebnisWaermepumpeModul` | Kacheln, Tafel, Diagramm |
| Stufenstunden „Vorwärmer" des Kessels | Protokollhinweis `SIMENG_KESSEL_BRENNWERT_BETRIEB` (U‑4) | Protokoll |
| Verfügbarkeitsgründe | `Verfuegbarkeitsgrund` + Ausweis wie `Abschaltpunkt` | Protokoll, Reiter |
| Komfort ohne Kessel | bestehende `Komfort_Unterschreitungsstunden`, `Komfort_Kelvinstunden` (AK2) | unverändert |

## 4 Schema

**4.1 Ein Schritt `UebergabegrenzeSchema`** in `EPOS.Kern/Allgemein/Update/UebergabegrenzeSchema.cs`, Bauform
wie `ErzeugerTeillastSchema` und `AnlagenfahrplanSchema`: Namen nur als Argument, eine Liste `SPALTEN`,
`Vollstaendig()`, wiederholbar, kein DML. Nummer: **die nächste freie, zuletzt 202** — vor dem Bau in der Zeile
„Schemaschritt angemeldet" der [Statusdatei](Status_iOS_Migration.md) anmelden und allein diese Zeile sofort pushen;
`SCHRITT = <Vorgängerklasse>.SCHRITT + 1` (heute `Ak3KSchema`), [ADR‑001](ADR-001_Schema-Ausrollung.md). Vier
Leser wie im Bestand: `WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs`,
`Werkzeuge/Testdatenbankschema`, Testvorrichtung in `EPOS.Kern.Tests`, `Paketanhebung` (Art `Ddl`); dazu
`SchemaStand.Zielversion`.

**4.2 Spalten** (alle nullbar; Tabellen bleiben `STRICT`; SQL-Regeln [BETRIEB_SQLITE.md](BETRIEB_SQLITE.md)
Abschnitt 6).

| Tabelle | Spalte | Typ und CHECK | NULL heißt | FK |
|---|---|---|---|---|
| `Tab_WP`, `Tab_WP_STAMM` | `Spreizung_Auslegung_K` | `REAL CHECK (… IS NULL OR … BETWEEN 3 AND 8)` | 5 K | 5.1 |
| dto. | `Spreizung_Max_K` | `REAL … BETWEEN 5 AND 40` | 10 K | 5.1 |
| dto. | `Spreizung_Min_K` | `REAL … BETWEEN 0 AND 8` | 3 K | 5.1 |
| dto. | `Mindestvolumenstrom_Prozent` | `REAL … BETWEEN 20 AND 100` | 60 % | 5.1 |
| dto. | `Ruecklauf_Max` | `REAL … BETWEEN 20 AND 70` | abgeleitet θ_WP,max − σ_min | 5.1 |
| dto. | `Ruecklauf_Bezug` | `REAL … BETWEEN 20 AND 40` | 30 °C, wenn abgewertet | 5.1 |
| dto. | `Ruecklauf_Abwertung_ProzentJeK` | `REAL … BETWEEN 0 AND 5` | keine Abwertung | 5.1 |
| dto. | `Kaeltemittel` | `TEXT` ohne CHECK (Werteliste im Kern, U‑2) | allgemeine (unterkritische) Vorgaben | 5.1, U‑2 |
| `Tab_BHKW`, `Tab_BHKW_STAMM` | `Ruecklauf_Max` | `REAL … BETWEEN 40 AND 90` | keine Grenze | 5.1, UB‑Q9 |
| `Tab_Energieanlagen` | `Einbindung` | `TEXT CHECK (Einbindung IS NULL OR Einbindung IN ('DIREKT','PUFFER','WEICHE'))` | Bestandsweg (U‑1) | 5.1 |
| dto. | `Vorwaermbetrieb` | `INTEGER CHECK (Vorwaermbetrieb IS NULL OR Vorwaermbetrieb IN (0,1))` | 0 | 5.1 |
| `Tab_ErgebnisWaermepumpeModul` | vier `Bereich_*_h` | `INTEGER … BETWEEN 0 AND 8760` | ohne Bereichsrechnung | 5.1 |
| dto. | vier `Bereich_*_MWh` | `REAL … >= 0` | dto. | 5.1 |
| dto. | `Spreizung_Unterschritten_h`, `Ruecklauf_Ueberschritten_h` | `INTEGER … BETWEEN 0 AND 8760` | dto. | 5.1 |
| dto. | `Bivalenzpunkt_1`, `Bivalenzpunkt_2` | `REAL` | nicht berechnet | 5.1 |
| dto. | `Uebergabe_Max_kW` | `REAL … >= 0` | nicht berechnet | 5.1 |
| `Tab_ErgebnisWaermepumpe` | die zehn Stunden-, Wärme- und Zählerspalten | wie oben | dto. | 5.1 |

Zusammen **43 Spalten**: 16 Gerätespalten an `Tab_WP(_STAMM)` (je Tabelle acht), 2 am BHKW, 2 an der Anlage, 13 am Modulergebnis,
10 am Projektergebnis. Hilfsfunktionen `Stunden(...)`, `NichtNegativ(...)` aus `AnlagenfahrplanSchema` übernehmen.
Nach dem Bau: `py Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite`.

**4.3 Katalog und Lieferwege.**

- `Katalogfassung.STUFE1`: die acht Spalten (mit `Kaeltemittel`) an die Liste von `Tab_WP_STAMM`, `Ruecklauf_Max` an `Tab_BHKW_STAMM`.
  NULL trägt nichts zur Prüfsumme bei — leer bleibt jede Prüfsumme gleich, die Katalogfassung steigt erst mit
  gepflegten Werten (Probe in `KatalogabgleichTests`).
- `Katalogabgleich` und Projektpaket führen die Spalten über die Spaltenliste mit (Proben `KatalogabgleichTests`,
  `ProjektpaketAnhebungTests`); die Projektkopie und das Duplizieren eines Geräts laufen über den bestehenden
  Kopierweg `WPCtrl` und nehmen alle acht Gerätespalten mit.
- `Werkzeuge/Auslieferungsvorlage`: keine neue Regel; die Spalten entstehen leer, das Katalogpaket der Fassung
  führt sie über `Katalogfassung.STUFE1` (auch `Kaeltemittel`). Gepflegte Katalogwerte kommen über Katalogpflege
  oder VDI 3805, nie aus einem Datenblatt ins Repositorium.
- VDI‑3805-Import der Wärmepumpen (`EPOS.Kern/Allgemein/Import/VDI 3805/WaermepumpenImport.cs`): Der Leser wertet
  die Satzarten 010, 100, 110, 400, 450 und 700 aus (Firma, Typ, Bauart, Aufstellung, Leistungsdaten) und kennt
  kein Kältemittel-Attribut; im Kern liest allein die Kältemaschine ein Kältemittel. Befund: kein Attribut, das
  Feld `Kaeltemittel` bleibt beim Import leer und rechnet mit der allgemeinen Vorgabe (U‑2).

**4.4 Testdatenbank und Referenzprojekt.** Die Testdatenbank (LFS) wird mit dem Schritt migriert
(`TestdatenbankSchemastandWacheTests`). Neues **Referenzprojekt 1060** als Kopie von 1056 nach FK 5.3: Heizkörper
75/60 °C, `Vorlauf_Max` 55 °C, `Einbindung` `DIREKT`, `Vorwaermbetrieb` 1, Kessel in Reihe hinter der
Wärmepumpe, Nachtsperre und Zeitprogramme zurückgesetzt; als Skript unter `Referenzlaeufe/` wie die
Vorgängerprojekte, Begründung in [`Referenzlaeufe/LIESMICH.md`](../../Referenzlaeufe/LIESMICH.md). Basis **R44**
(`<datum>_R44_Uebergabegrenze`) im selben Schritt; die Bestandsprojekte übernehmen ihre Werte aus R43 unverändert (U‑1); Einfrierregel in [`CLAUDE.md`](../../CLAUDE.md) nach FK 5.3
(Gerätespalten der Wärmepumpe samt `Kaeltemittel`, `Einbindung`, `Vorwaermbetrieb`, `Vorlauf_Max`, `Ruecklauf_Max` am BHKW, Anlegen
oder Entfernen von 1060). CI-Auswahl nach UB‑Q8: Empfehlung aufnehmen (`kern.yml`, Projektliste um 1060).

## 5 Etappen

Je Etappe eine Folge von Agentenwellen, jede mit höchstens rund 150 Werkzeugaufrufen, im eigenen Worktree,
committet auf dem Wellenzweig, ohne Push und ohne vollständiges Gate (das fährt die Orchestrierung nach dem Merge).
Builds laufen nie parallel.

### 5.1 UB‑E1 — Übergabegrenze und Herleitungszeile (sofort baubar)

- **Ziel:** FK 9 UB‑E1; keine Rechenwirkung, kein Schemaschritt.
- **Code:** `Bivalenz/Bivalenzvorgaben.cs`, `Uebergabegrenze.cs`, `Bivalenzrechner.cs` (nur `Punkte`, `Massgebend`),
  `Bivalenzherleitung.cs`; Gerätegrenzen in E1 ausschließlich aus `Bivalenzvorgaben`.
- **Hülle und Dialog:** neue Abbildung `EPOS.UI.Daten/Erzeuger/BivalenzAbbildung.cs` (Muster
  `BetriebszeitenAbbildung`) füllt Lesewerte und Herleitungszeile in `WaermepumpeAnlageDaten`; Aufruf aus
  `WaermepumpeAnlageHuelle` (Windows-Schale) und dem Schreibweg der Simulationskonfiguration. In
  `WaermepumpeKonfiguration.razor` die Herleitungszeile unter „Betrieb" und die Schnellwahl des Höchstvorlaufs in
  „Betriebszeiten" (schreibt `Vorlauf_Max`).
- **Ressourcen:** Herleitungstexte, Schnellwahl (Abschnitt 6.3), beide Sprachen, `designer_neu.py schreiben`.
- **Tests:** `EPOS.Kern.Tests/UebergabegrenzeTests` (Gleichgewicht, 50/60/70 °C, Exponenten, Grenzfälle,
  Flächenheizung, Bivalenzpunkte des Zahlenbeispiels — FK 8.1 Zeilen 1–6 und 9); bunit in `EPOS.UI.Tests`
  (Herleitungszeile mit und ohne Kopplung, Schnellwahl), Kultur de-DE über `Kulturvorrichtung`.
- **Abnahme:** Tests grün; Referenzlauf der CI-Auswahl gegen R43 unverändert; Windows-Schale baut auf Linux.
- **Aufwand:** 3–4 PT. **Abhängigkeit:** keine.
- **Wellen (2):** **E1‑a** `opus` Kernklassen und Rechenproben; **E1‑b** `opus` Abbildung, Dialog, Ressourcen,
  bunit, Linux-Bau der Schale.

### 5.2 UB‑E2 — Betriebsbereiche, Vorwärmbetrieb, Referenzprojekt (frei nach #817)

- **Ziel:** FK 9 UB‑E2. Die Abhängigkeit „nach AK3‑K" ist mit #817 (Basis R43) erfüllt.
- **Schema:** `UebergabegrenzeSchema` mit allen 43 Spalten (auch die Gerätespalten von UB‑E3, damit es bei einem
  Schritt bleibt); vorher Nummer anmelden.
- **Code:** `Bivalenzrechner.Bereich`; `SimulationWaermepumpe` (Bereich statt Kennfeldkapazität im Profilweg,
  `Heizkreisruecklauf`, `Vorwaermvorlauf`, Zähler); `Stundenangebot.WaermepumpeKapazitaet` (optionales
  Bivalenzobjekt); `Anlagenverfuegbarkeit` (vier Gründe); `Kesselkennlinie.Ruecklauf` und
  `SimulationSPK.RuecklaufDerStunde` (Stufe „Vorwärmer"); Schreiben der Ergebnisspalten über den Ergebnisweg,
  Lesen in `ErgebnisCtrl` nach `Vollstaendig()` wie `Ak3KSchema`; `WErzeugerCtrl` liest und schreibt `Einbindung`
  und `Vorwaermbetrieb` mit `?`-Parametern.
- **Hülle und Dialog:** Gruppe „Bivalenz und Übergabe" nach Abschnitt 6 (Einbindung, Vorwärmbetrieb, Lesewerte,
  weiche Sperren); Abbildung um die zwei Anlagenfelder.
- **Tests:** FK 8.1 Zeilen 8, 11, 12, 16 in `UebergabegrenzeTests`/`BetriebsbereichTests`; Schemaprobe mit
  Wiederholung (`UebergabegrenzeSchemaTests`); `KesselKennlinieTests` um die Vorwärmer-Stufe; Wache
  `UebergabegrenzeReferenzprojektWacheTests` (Muster `FahrplanReferenzprojektWacheTests`,
  `Ak3ReferenzprojektWacheTests`) hält Gerätefelder, `Einbindung`, `Vorwaermbetrieb`, `Vorlauf_Max` und die
  Bereichsstunden von 1060.
- **Abnahme:** SQL-Dialekt grün; Referenzlauf gegen R43: 24 Bestandsprojekte unverändert, 1060 neu; Basis R44
  eingefroren und begründet; Windows-Schale baut auf Linux.
- **Aufwand:** 5–7 PT. **Abhängigkeit:** UB‑E1; Entscheide UB‑Q1, UB‑Q4, UB‑Q8, UB‑Q11 (U‑1 entschieden: a).
- **Wellen (4):** **E2‑a** `opus` Schemaschritt, vier Leser, Testdatenbank, SQL-Dialekt; **E2‑b** `opus`
  Bereichsrechnung in Profilweg und Angebot, Gründe, Vorwärmer-Stufe, Ergebnisspalten; **E2‑c** `opus` Dialoggruppe,
  Abbildung, Ressourcen, bunit; **E2‑d** `opus` Referenzprojekt 1060, Wache, Einfrierregel, Basis R44 (Vergleich
  und Protokoll der Basis darf ein `sonnet`-Nachzug übernehmen).

### 5.3 UB‑E3 — ΔT-Restriktionen, Einbindung, Rücklaufgrenzen

- **Ziel:** FK 9 UB‑E3.
- **Code:** `Geraetegrenzen` (liest die acht Spalten, Vorgabe je Kältemittelklasse), `Hydraulikgrenze` (direkt, Weiche, Puffer als Näherung,
  Überströmventil), `Ruecklaufgrenze` (abgeleitet, Datenblattwert, R744-Faktor auf Leistung und COP, Strom
  unverändert), BHKW-Prüfung in `SimulationBHKW`; R744 am Puffer liest die unterste Pufferzone über den
  bestehenden Speicherleser.
- **Katalog:** `Katalogfassung.STUFE1`, Stammblatt `WaermepumpeStammFelder.razor`/`WaermepumpeStammDaten.cs` mit der
  Klappliste „Kältemittel" (U‑2) und BHKW-Stammblatt; Konfiguration zeigt Lesewerte mit Herkunft.
- **Tests:** FK 8.1 Zeilen 7, 10, 13–15, 17; `KatalogabgleichTests`, `ProjektpaketAnhebungTests`.
- **Abnahme:** Referenzlauf: Bestandsprojekte unverändert (U‑1 entschieden: a; die abgeleitete Grenze wirkt nur mit
  `Einbindung`); 1060 ändert sich, wenn die abgeleitete Grenze Stunden zählt → Basis R45 im selben Schritt.
- **Aufwand:** 4–6 PT. **Abhängigkeit:** UB‑E2; Entscheide UB‑Q2, UB‑Q3, UB‑Q5, UB‑Q9 und U‑3.
- **Wellen (3):** **E3‑a** `opus` Kernklassen und Rechenproben; **E3‑b** `opus` Stammblätter, Katalogfassung,
  Abgleich, Projektpaket, Lesewerte; **E3‑c** `sonnet` Referenzlauf, Vergleichstafel, gegebenenfalls Basis R45
  nach Vorlage.

### 5.4 UB‑E4 — Bivalenzdiagramm und Bericht

- **Ziel:** FK 9 UB‑E4 (UB‑Q7 a).
- **Code:** `ChartRenderer.Bivalenzdiagramm` mit `BivalenzdiagrammModell` (Muster `VorlaufRuecklauf`); Modell aus
  `Bivalenzherleitung` plus optional die Stundenpunkte des Laufs; Kachelzeile in `WaermepumpeReiter.razor`;
  Kennzahlen in `KennzahlenKatalog` (`wp.bivalenz.*`); Bild `stand.bild.wp_bivalenz` in
  `Vorlagenfeldkatalog.Bilder.cs`, Tafel „Bivalenz und Übergabe" in `Vorlagenfeldkatalog.Tabellen.cs`,
  Katalogfassung der Vorlage + 1; Word- und Excel-Bausteine; CSV-Export und KI-Sicht des Reiters mit den
  Spaltennamen als Schlüssel; `WaermepumpeAnlageKiSicht` um Felder und Herleitungszeile.
- **Tests:** `Proben/ChartProben` (synthetische Reihe, Maße, Farben, Determinismus, Gegenprobe), neue Zeilen in
  der jüngsten `Proben/ChartProben/Messlatte_*.sha256`; `VorlagenfeldkatalogWacheTests`,
  `BerichtsvorlageDateiWacheTests`, `AuslieferungsvorlagenWacheTests`.
- **Abnahme:** ChartProben grün mit neuer Messlatte (Hash-Vergleich auf ubuntu im Kern-Lauf); Vorlagen über
  `Werkzeuge/Berichtsvorlage` neu gebaut, `OpenXmlValidator` grün.
- **Aufwand:** 3–4 PT. **Abhängigkeit:** UB‑E2 (Ergebnisspalten); UB‑E3 nur für die Grenzlinien.
- **Wellen (3):** **E4‑a** `opus` Diagramm, Modell, ChartProben, Messlatte; **E4‑b** `opus` Reiter, Kennzahlen,
  Bericht, Export, KI-Sicht; **E4‑c** `sonnet` Vorlagen neu bauen und Wachen.

### 5.5 UB‑E5 — Wiki, Logbuch, Abschluss

- **Ziel:** FK 9 UB‑E5; Abschnitt 7.3 dieses Papiers.
- **Wellen (1):** **E5‑a** `sonnet` Wiki-Quellen, Logbuch-Entwurf, Fachkonzept „wie gebaut" und beide Papiere per
  `git mv` nach `ueberholt/`, Indexzeilen.
- **Abnahme:** `DokumentationLinkWacheTests`, `WikiProduktdatenWacheTests`, Gegenlesemuster aus `CLAUDE.md`.
- **Aufwand:** 1–2 PT.

**Summe:** 16–23 PT in **13 Wellen** (E1 2, E2 4, E3 3, E4 3, E5 1); dazu je Etappe Merge und Gate durch die
Orchestrierung.

## 6 Oberfläche

**6.1 Ort und Gruppen** (FK 6.1, Mockup `Mockups/Waermepumpe_Bivalenz_Uebergabe.html`). In
`WaermepumpeKonfiguration.razor` ersetzt die `Formulargruppe` „Bivalenz und Übergabe" die Gruppe „Betrieb"; der
Höchstvorlauf bleibt in „Betriebszeiten" und bekommt die Schnellwahl. Kopf und Schlussleiste des Dialogs bleiben
unberührt — keine Fensterprobe nötig; keine Änderung an `Raster` oder `Katalogliste` — keine Rasterprobe nötig.
Wird doch eine der beiden Leisten angefasst, gilt `Proben/Rasterprobe/fensterprobe.mjs`.

**6.2 Felder und Verhalten.**

| Feld | Baustein | Datenquelle | Regel |
|---|---|---|---|
| Bivalenter Betrieb, Betriebsart, Abschaltpunkt | `Schalter`, `Auswahlfeld`, `Zahlenfeld` | bestehende Felder | ziehen um, Sichtbarkeit wie heute |
| Einbindung | `Auswahlfeld` | `Einbindung` | Vorbelegung bei neuer Anlage; „leer" zeigt „nicht gewählt", die Herleitungszeile „Einbindung nicht gesetzt — Übergabegrenze ruht" (U‑1) |
| Vorwärmbetrieb | `Schalter` | `Vorwaermbetrieb` | sichtbar bei parallel und teilparallel; bei alternativ gesperrt mit Text |
| Höchstvorlauf | Lesewert | `Vorlauf_Max`, sonst `Vorlauf` | Verweis auf „Betriebszeiten" |
| Kältemittel | `Auswahlfeld` (Klappliste) | `Kaeltemittel` des Geräts | elf Codes und „leer"; vor der Schnellwahl in „Betriebszeiten"; die Wahl wirkt nach 6.3 (U‑2) |
| Spreizungen, Mindestvolumenstrom, höchster Rücklauf | Lesewerte | `Geraetegrenzen` | Herkunft „Katalog", „Vorgabe nach Kältemittel", „Vorgabe" oder „abgeleitet" je Wert; änderbar nur im Stammblatt (UB‑Q2) |
| Herleitungszeile | `Herleitungszeile` | `Bivalenzherleitung` | Wortlaut FK 6.2; ohne Kopplung der Hinweistext; Diagramm klappt darunter auf (ab UB‑E4) |
| Weiche Sperren | `Warnbanner` (Warnung) | Dialogprüfung im Kern | FK 6.2; zusätzlich „Wärmepumpe steht in der Kaskade hinter dem Kessel" bei Vorwärmbetrieb (Abl.) |

**6.3 Schnellwahl und Kältemittel** (U‑2). Die Klappliste „Kältemittel" steht vor der Schnellwahl des
Höchstvorlaufs (Konfiguration, „Betriebszeiten") und im Stammblatt. Die Wahl speichert die Klasse in `Kaeltemittel`
des Geräts und füllt nur **leere** Felder mit den Vorgaben der Klasse aus `Bivalenzvorgaben`: `Vorlauf_Max` an der
Anlage, im Stammblatt `Spreizung_*`, `Ruecklauf_Max` und bei R744 `Ruecklauf_Bezug`,
`Ruecklauf_Abwertung_ProzentJeK` am Gerät; gefüllte Felder bleiben. Herkunft der gefüllten Werte „Vorgabe nach
Kältemittel". Leeres `Kaeltemittel` oder `SONSTIGES`: allgemeine (unterkritische) Vorgaben wie bisher.

**6.4 Schichten.** Rechnung und Prüfungen im Kern (`Bivalenzherleitung`, Dialogprüfung); Lesen und Schreiben der
Spalten in `WErzeugerCtrl` (Anlage) und `WaermepumpeGeraeteCtrl`/`WPCtrl` (Gerät); Feldsatz
`WaermepumpeAnlageDaten`; Abbildung `EPOS.UI.Daten/Erzeuger/BivalenzAbbildung.cs`; die Windows-Hülle ruft nur die
Abbildung. Wer die Hülle anfasst, baut die Schale vor der Abnahme auf Linux
(`dotnet build WindowsFormsApplication1/WindowsFormsApplication1.csproj -c Debug -p:Platform=x64
-p:EnableWindowsTargeting=true`).

**6.5 Ressourcenschlüssel (Vorschlag, beide Sprachen, danach `designer_neu.py schreiben`).**

| Schlüssel | Deutsch (Kurzform) |
|---|---|
| `WPA_GRP_BIVALENZ_UEBERGABE` | Bivalenz und Übergabe |
| `WPA_LBL_EINBINDUNG`, `WPA_OPT_EINBINDUNG_DIREKT`, `…_PUFFER`, `…_WEICHE`, `…_LEER` | Einbindung; direkt; Puffer; Weiche; nicht gewählt |
| `WPA_CHK_VORWAERMBETRIEB`, `WPA_HINWEIS_VORWAERMBETRIEB` | Vorwärmbetrieb (Kessel in Reihe); Erläuterung |
| `WPA_LBL_HOECHSTVORLAUF_LESEN`, `WPA_LBL_SCHNELLWAHL_KAELTEMITTEL` | Höchstvorlauf; Schnellwahl nach Kältemittel |
| `WPA_LBL_KAELTEMITTEL`, `WPA_OPT_KAELTEMITTEL_<Code>` (elf Codes), `…_LEER` | Kältemittel; Anzeige je Code in beiden Sprachen; nicht gewählt |
| `WPA_LBL_SPREIZUNG_AUSLEGUNG`, `…_MAX`, `…_MIN`, `WPA_LBL_MINDESTVOLUMENSTROM`, `WPA_LBL_RUECKLAUF_MAX` | Gerätegrenzen |
| `WPA_HERKUNFT_KATALOG`, `…_VORGABE_KAELTEMITTEL`, `…_VORGABE`, `…_ABGELEITET` | Herkunft je Wert |
| `WPA_HERLEITUNG_UEBERGABE`, `WPA_HERLEITUNG_OHNE_KOPPLUNG`, `WPA_HERLEITUNG_NICHT_WIRKSAM`, `WPA_HERLEITUNG_ABSCHALTPUNKT` | Herleitungszeilen (Formatplatzhalter); `…_NICHT_WIRKSAM`: „Einbindung nicht gesetzt — Übergabegrenze ruht" (U‑1) |
| `WPA_WARN_SPREIZUNG`, `…_HOECHSTVORLAUF`, `…_RUECKLAUF_NIE`, `…_GMODG`, `…_VORWAERM_OHNE_KESSEL`, `…_KASKADE` | weiche Sperren |
| `SIMENG_WP_GRUND_UEBERGABE`, `…_SPREIZUNG_MAX`, `…_SPREIZUNG_MIN`, `…_RUECKLAUF_MAX` | Verfügbarkeitsgründe im Protokoll |
| `SIM_KACHEL_BETRIEBSBEREICHE`, `SIM_BEREICH_WP_ALLEIN`, `…_PARALLEL`, `…_VORWAERMUNG`, `…_NUR_KESSEL` | Reiter |
| `BER_TAFEL_BIVALENZ`, `BER_BILD_BIVALENZ`, `BER_HINWEIS_STUNDENMODELL` | Bericht |

## 7 Ergebnisse, Bericht, Export, Wiki

**7.1 Reiter und Bericht.** Kachelzeile „Betriebsbereiche" und leise Zählerzeile nach FK 7.1, gelesen über
`ErgebnisCtrl` (Spalten nur, wenn `UebergabegrenzeSchema.ErgebnisspaltenVorhanden()`); Kesselreiter unverändert
bis auf den Protokollhinweis (U‑4). Bild `stand.bild.wp_bivalenz` mit Platzhaltertext „kein Bivalenzdiagramm —
Kopplung aus"; Tafel und Hinweiszeilen nach FK 7.3; Prüfhinweise UB‑Q10 als Berichtszeilen ohne Feld.

**7.2 Diagramm.** `ChartRenderer.Bivalenzdiagramm(BivalenzdiagrammModell)`: Linien und Marken nach FK 7.2, Farben
aus der Diagrammfarbtafel, keine neue Farbe; deterministische Achsenwahl. ChartProben: neue Probe mit
synthetischem Zahlenbeispiel (FK 4.5) und einer Gegenprobe; die Hash-Zeilen gehen in die jüngste
`Messlatte_*.sha256` (heute `Messlatte_2026-10-05.sha256`) — der Kern-Lauf vergleicht auf ubuntu.

**7.3 Export und KI.** Ergebnisspalten mit ihren Spaltennamen als Schlüssel in CSV-Export und KI-Sicht des
Reiters; Herleitungszeile und Felder in `WaermepumpeAnlageKiSicht`; die Abdeckungswache
`KiMaskenabdeckungWacheTests` bleibt grün.

**7.4 Wiki und Logbuch.** Quellen `Projekte/Wiki/Programm Dokumentation - Wärmepumpe.wiki` (Gruppe, Felder,
Diagramm), `Projekte/Wiki/Grundlagen - Wärmepumpe.wiki` (Übergabegrenze, Vorwärmbetrieb, Spreizungen,
Rücklaufgrenzen; Reihenschaltung als weitere Grafik neben den vorhandenen), Verweise in den Seiten zum BHKW, zum
Heizkessel und zur Anlagenkopplung. Funktion, wie sie ist — keine Änderungsvermerke; keine Hersteller- oder
Produktdaten; Beispiele mit neutralen Namen und runden Werten. Upload gebündelt mit dem nächsten Wochen-Upload.
Logbuch-Entwurf (Version beim Anwender erfragen), je ein Satz:
„Die Wärmepumpe rechnet mit der Leistung der Heizflächen bei ihrem Höchstvorlauf; beide Bivalenzpunkte werden
berechnet und angezeigt." · „Neu ist der Vorwärmbetrieb: Wärmepumpe und Kessel in Reihe." · „Neu sind die
Rücklauf- und Spreizungsgrenzen der Wärmepumpe." · „Neues Berichtsbild Bivalenzdiagramm."

## 8 Prüfungen und Abnahme

**8.1 Rechenproben (FK 8.1) → Testklassen.**

| FK 8.1 Probe | Klasse | Etappe |
|---|---|---|
| Gleichgewicht, 50/60/70 °C, Exponenten, Grenzfälle, Flächenheizung | `UebergabegrenzeTests` | E1 |
| Bivalenzpunkte des Zahlenbeispiels | `UebergabegrenzeTests` | E1 |
| Vorwärmanteil, alternativ, Bestand byte-gleich | `BetriebsbereichTests` | E2 |
| Rücklaufstufe „Vorwärmer" | `KesselKennlinieTests` | E2 |
| Höchstspreizung direkt, Weiche, Mindestvolumenstrom und Überströmventil | `HydraulikgrenzeTests` | E3 |
| abgeleitete Rücklaufgrenze, R744-Abwertung, BHKW | `RuecklaufgrenzeTests` | E3 |
| Vorgabe je Kältemittelklasse: füllt nur leere Felder, leer und `SONSTIGES` → allgemeine Vorgabe (U‑2) | `GeraetegrenzenTests` | E3 |
| ohne `Einbindung` kein Bivalenzobjekt, Herleitungszeile „Einbindung nicht gesetzt — Übergabegrenze ruht" (U‑1) | `BetriebsbereichTests`, bunit | E2 |
| Schema mit Wiederholung, Spalten, CHECK | `UebergabegrenzeSchemaTests` | E2 |
| Dialog: Sichtbarkeit, Schnellwahl, Herleitungszeile, Sperren | `EPOS.UI.Tests/Dialoge/WaermepumpeBivalenzTests` (bunit) | E1, E2 |

Alle Klassen laufen unter en-US; Proben mit deutschen Texten oder Zahlformaten pinnen de-DE über die
`Kulturvorrichtung`. Agenten laufen sie mit `--filter` und den xUnit-Schaltern aus `CLAUDE.md`.

**8.2 Wache 1060.** `UebergabegrenzeReferenzprojektWacheTests` hält die Eingaben (Gerätespalten, `Einbindung`,
`Vorwaermbetrieb`, `Vorlauf_Max`, Kaskadenfolge) und die Ergebnisse (Bereichsstunden, Bivalenzpunkte, Stufe
„Vorwärmer" des Kessels > 0).

**8.3 Referenzlauf.** Vor und nach jeder rechnenden Etappe (E2, E3) gegen R43 bzw. R44:
`dotnet run --project EPOS.Referenzlauf -c Release -- lauf …` und `… vergleich <basis> <neu>`; Toleranz der CI
(Betrag ≥ 1 relativ 1e‑4, sonst absolut 0,01). Erwartung: alle Bestandsprojekte unverändert, auch 1047, 1054, 1056
und 1058 (U‑1 entschieden: a, Opt-in über `Einbindung`), 1060 neu;
Begründung und neue Basis in `Referenzlaeufe/LIESMICH.md`.

**8.4 Weitere Abnahmen.** SQL-Dialekt-Prüfer nach jeder neuen Anweisung; Linux-Bau der Windows-Schale bei jeder
Änderung an Hülle oder Abbildung; ChartProben (E4); Windows-Sichtabnahme durch den Anwender nach E2 (Gruppe,
Vorbelegung, Sperren) und nach E4 (Kacheln, Diagramm, Bericht). Reihenfolge je Etappe: **Merge → Gate →
Statuszeile und Protokoll → Push → Nachweis**; ein iOS-Lauf ist nicht begründet (keine Änderung an der iOS-Hülle).

## 9 Risiken und Festlegungen

| Risiko | Festlegung |
|---|---|
| Rechenzeit: Nullstelle je Stunde × Zone × Wärmepumpe, im AK3-Kreis je Durchlauf | Startwert aus der Vorstunde, höchstens 8 Newton-Schritte wie H2; Φ_UE,max hängt nur von θ_i ab — Zwischenspeicher je Stunde und Zone; Laufzeitmessung an 1058 und 1060 im Wellenbericht |
| Determinismus und Plattform | feste Iterationszahl und Abbruchschwellen, keine Parallelsummen; Vergleich in der CI mit Toleranz, der Byte-Vergleich ist Information |
| Zusammenspiel mit dem AK3-Puffer (1058) und Sperrzeiten | Sperrzeit und Zeitprogramm gehen vor (B0); Puffer als benannte Näherung (FK 4.4); 1058 bleibt unverändert, solange `Einbindung` leer ist (U‑1); Prüforakel des Kreises um die θ_R-Abhängigkeit erweitern |
| Kaskadenfolge im Vorwärmbetrieb | Wärmepumpe vor dem Kessel Pflicht; sonst B4 und benannte Meldung |
| Bestandsprojekte | leere Felder = heutiges Verhalten; kein DML im Schritt |
| Katalogabgleich bei neuen Spalten | leere Spalten ändern keine Prüfsumme; gepflegte Werte heben die Katalogfassung über den gewohnten Weg |
| Enum-Wert der neuen Rücklaufstufe | `Vorwaermer = 4` hinten angefügt, damit gespeicherte oder gezählte Werte stabil bleiben |
| Namensnähe `Ruecklauf`/`Ruecklauf_Max` am BHKW | Empfehlung U‑3 a (offen): Beschriftung „Höchster Rücklauf (Betriebsgrenze)" gegenüber „Rücklauf (Auslegung)", Prüfregel `Ruecklauf_Max ≥ Ruecklauf`, Vorgabe bei Neuanlage max(70 °C, Auslegungsrücklauf) |
| Normzahlen und Messdaten | nie ins Repositorium; Vorgabewerte nur als gerundete Konstanten mit Quellverweis |
| Hersteller- und Produktdaten | nie im Wiki, nicht in Testnamen; Beispiele neutral |

## 10 Übergabe an die Sitzung Gebäudesimulation

**Reihenfolge.** UB‑E1 → UB‑E2 → UB‑E3 → UB‑E4 → UB‑E5; UB‑E4 kann nach UB‑E2 beginnen, wenn die Grenzlinien
von UB‑E3 als Nachzug kommen. Gebaut von der Sitzung Gebäudesimulation (UB‑Q8 a), weil dieselbe Naht
(`Angebot(h, V)`, Schritt H) betroffen ist.

**Erste Welle konkret (E1‑a, `opus`).** Worktree vom aktuellen `origin/ios_migration_september`; Ordner
`EPOS.Kern/Allgemein/Simulation/Bivalenz/` mit `Bivalenzvorgaben`, `Uebergabegrenze`, `Bivalenzrechner`
(`Punkte`, `Massgebend`) und `Bivalenzherleitung`; Klasse `EPOS.Kern.Tests/UebergabegrenzeTests` mit den Proben
der Tafel 8.1 Zeilen 1 und 2; Bau des Kern-Filters, Tests mit `--filter "FullyQualifiedName~Uebergabegrenze"`;
kein Schema, keine Ressourcen, kein Referenzlauf nötig (keine Rechenwirkung). Bericht: Proben n/n, Abweichungen
zum Zahlenbeispiel.

**Vorher beim Anwender zu entscheiden.** Für E1: nichts zwingend (UB‑Q6 a als Vorgabe). Vor E2: UB‑Q1, UB‑Q4,
UB‑Q8, UB‑Q11; **U‑1** ist am 08.10.2026 entschieden (a: Wirksamkeit an `Einbindung` gebunden, Bestandsprojekte
unverändert), ebenso U‑2 (Katalogfeld `Kaeltemittel`) und U‑4 (Protokollhinweis). Vor E3: UB‑Q2, UB‑Q3, UB‑Q5,
UB‑Q9 und U‑3 (Name der BHKW-Rücklaufgrenze). Vor E4: UB‑Q7. UB‑Q10 betrifft
nur Hinweistexte und kann mit E4 fallen.

**Pflichten jeder Welle.** Schemaschritt vor dem Bau anmelden (E2‑a); `AGENT_LAEUFT` nur bei Arbeit im
Hauptbaum; Commits sofort mit genauen Pfaden; kein Push und kein CI-Lauf durch Agenten; Bericht mit Zahlen, ohne
Dateiabzüge.

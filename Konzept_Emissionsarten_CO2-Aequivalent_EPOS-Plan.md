# Konzept — Emissionsarten-Katalog und CO₂-Äquivalent (EPOS-Plan)

**Stand:** 07.09.2026 · **Rev. 1.8 — E1 bis E6 umgesetzt; dazu die Entscheide in § 8
(B1: EINE Emissionsquelle für alle Erzeuger; B2; W11a‑O‑2)** (Rev. 1.7 vom 29.08.2026:
§ 5.2 = Saatvorlage E6)
(Nutzerentscheid 29.08.2026 mittags: Luftschadstoffe als gekennzeichnete GEMIS-LCA-Vorlagen) (Rev. 1.2 vom 28.08.2026:
alle Entscheidungsfragen beantwortet — F3 präzisiert, F4 bestätigt, Luftschadstoffe ohne
Vorkette, Modus global + Projekt-Override, Artenauswahl global, E1 vor E2)

> **Umsetzungsvermerk (29.08.2026).** **E1** läuft als Migrationsschritt 56
> (20 Träger gesetzt), **E2** als Migrationsschritt 57 (`ZIEL_VERSION` 57): sieben
> Emissionsarten, 139 Vorlagen, 81 aktive Trägerwerte, Berechnungsmodus `CO2` in
> `Tab_Applikation` und in allen 26 Bestandsprojekten. Zweitlauf beider Schritte:
> 0 Änderungen. Die **Mapping-Liste** (§ 9 Punkt 5) steht in **§ 5.1** zur Durchsicht.
>
> **E3** gliedert den Detailbereich des Energieträger-Dialogs in die Reiter
> „Preise & Umrechnung" (Bestand, umgehängt statt neu gebaut) und „Emissionen"
> (dynamische Feldliste aus den ausgewählten Arten, **Textfelder ohne Drehpfeile**,
> Einheit je Art, Herkunft am Feld, CO₂e-Summe mit F3-Hinweis, Modus-Schalter).
> **E4** bringt den Dialog `Form_Emissionskatalog` (Artenverwaltung links, Werte der
> markierten Art rechts, Übernehmen/Neu/Bearbeiten/Löschen mit den Schutzregeln).
> Die Regeln stehen UI-frei in `EmissionenCtrl` und `EmissionskatalogCtrl`
> (Hausmuster Ä9); der Prüfstand gegen eine Arbeitskopie der Produktiv-DB meldet
> **65/65**. **Kein Rechenergebnis ändert sich:** Die Rechner lesen weiter die
> Altspalten, und der Schreibweg spiegelt die drei Kernarten dorthin (F9).
>
> **E5 (29.08.2026) — der Modus wird wirksam.** Beide Rechner lesen ihre Faktoren jetzt
> aus EINER Kette (`EmissionsFaktorLader`, § 3 mit den zwei Umsetzungsklärungen), führen
> im Modus `CO2E` das CO₂-Äquivalent nach F6/F3 und vermerken den wirksamen Modus am
> Ergebnis; jede Ausweisstelle beschriftet danach (`EmissionsAusweis`).
> `STROMMIX_CO2_G_JE_KWH` steht auf **435**.
>
> **Messung gegen eine Arbeitskopie der Produktiv-DB (26 Projekte, davon 18 mit
> bestimmbarer CO₂-Kennzahl):** Im Modus `CO2` — dem Stand aller Bestandsprojekte —
> ändert die neue Lesekette **an keinem einzigen Projekt eine Zahl**. Die einzige
> Abweichung ist der angekündigte Strommix-Randfall (11 Projekte ohne gepflegten
> Stromträger, `CO2Gesamt` um `Netzrestbedarf × 55/1000` höher). Referenzlauf
> vorher/nachher **10/10 PASS** (2 567 843 Werte), `pruefen` plausibel.
>
> **Warum die Lesekette nichts verschob, obwohl E1 die Trägerwerte geändert hat:** In
> allen Referenzprojekten trägt jeder Träger MIT Verbrauch eine Projektübersteuerung,
> und die steht weiterhin ganz oben (Umsetzungsklärung 1). Der Unterschied wird erst
> sichtbar, wo ein Projekt KEINE eigene Zahl führt — dort gilt künftig der belegte
> Katalogwert (Erdgas E: 201 g/kWh BAFA) statt der unbelegten Altliteratur aus
> `Tab_Brennstoff_Stamm` (240 g/kWh). Genau das war der Zweck von E1; die Wirkung tritt
> mit E5 ein und ist am Beispiel in § 7 nachgerechnet.
>
> **E6 (29.08.2026) — die Quellen-Saat.** Migrationsschritt 58 (`ZIEL_VERSION` 58, Reihenfolge
> eingehalten: erst Schritt, dann Zielzahl) sät die **85 Vorlagen nach § 5.2** — UBA v2.1
> Blatt 01 als 8 × CO₂ und je 16 × CH₄/N₂O, GEMIS 5.2 als je 15 × SO₂/NOx/Staub, alle
> `ist_aktiv = falsch`. Prüfstand auf einer Arbeitskopie der Produktiv-DB: Erstlauf
> **85/85** angelegt, 0 fehlende Träger, Zweitlauf **0**; die SHA-256-Dumps aller aktiven
> Werte und aller Altspalten sind vorher/nachher identisch, 13 Stichproben wertgenau, die
> Emissionskennzahlen unverändert — **kein aktiver Wert wird berührt**. Der
> Idempotenzschlüssel führt `quelle_text` mit, sonst kollidierten die drei trägerlosen
> UBA-Zeilen. Einzelheiten und Belege:
> [`Allgemein\Update\E6_QuellenSaat_Protokoll.md`](WindowsFormsApplication1/Allgemein/Update/E6_QuellenSaat_Protokoll.md).
>
> **B1 (07.09.2026) — EINE Quelle für alle Erzeuger.** Der Anwenderentscheid
> **W14a‑E‑8‑B1** löst die zwei übrigen Emissionsquellen der SIMULATION ab: Der Heizkessel
> las `Tab_Brennstoff_Stamm` unmittelbar, das BHKW die fünf Gerätespalten seines Katalogs.
> Beide lesen seither über den neuen Kern-Dienst
> `Allgemein\Wirtschaftlichkeit\Emissionsquelle.cs` dieselbe Kette wie die Wirtschaftlichkeit
> — im Berechnungsmodus des Projekts —, die Gerätespalten sind „nur Anzeige", und die
> Autarkie-Kachel folgt derselben Quelle (W11a‑O‑2). Der Referenzlauf der zwölf Projekte
> bleibt **byte-gleich**, weil keine Referenz-CSV eine Emissionsgröße führt (§ 9 Punkt 8);
> der Nachweis steht in `EPOS.Kern.Tests/EmissionsquelleTests.cs`. Einzelheiten in **§ 8**.
>
> Offen bleiben die Sichtabnahme E3–E6 und die Durchsicht der Mapping-Liste § 5.1.

Anforderung (28.08.2026): Der Energieträger-Dialog soll seine Emissionsfaktoren aus einem
**pflegbaren Katalog** beziehen (bestehende Faktoren übernehmen, eigene hinzufügen/ändern/löschen),
je Faktor eine **Umrechnung in CO₂-Äquivalent** führen (außer CO₂ selbst), die **Summe der
CO₂-Äquivalente der ausgewählten** Arten anzeigen, die **Feldliste wählbar** machen (CO₂ Pflicht,
SO₂ und NOx Voreinstellung), die CO₂-Berechnung zwischen **CO₂ und CO₂-Äquivalent umschaltbar**
machen (im neuen und im bestehenden Dialog) — und die Drehfeld-Pfeile an den Eingabefeldern
entfernen.

**Verhältnis zu den bestehenden Konzepten:**

- [`Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md`](Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md)
  (Rev. 1.1) ist **umgesetzt** — die Saat lief am 29.08.2026 als Migrationsschritt 56
  (= Etappe E1 dieses Konzepts); der dort offene STROMMIX-Punkt wurde mit E5
  entschieden und umgesetzt (380 → 435).
- [`Konzept_Emissionsfaktoren_Quellenwahl_EPOS-Plan.md`](Konzept_Emissionsfaktoren_Quellenwahl_EPOS-Plan.md)
  (Rev. 1, zur Abnahme) bleibt das Modell für die **Projektwahl** einer Leitquelle.
  Dieses Konzept fügt die dritte Dimension hinzu: die **Emissionsart** wird vom festen
  Spaltensatz (`co2`, `so2`, `nox`) zum Katalogobjekt. Die **Herkunftsführung** jenes
  Konzepts ist mit § 3 (Felder `quelle`, `quelle_text`, `herkunft_id`, `ist_co2e` in
  `emissionswert`) bereits in vereinfachter Form realisiert; **offen von dort** ist nur
  noch die projektbezogene Quellenwahl (Leitquelle je Projekt, Ausnahme je Träger —
  dessen F4/E3). Wo Rev. 1 der Quellenwahl feste Spalten vorsah
  (`emissionsfaktor.co2/so2/nox/staub`), gilt die generische Form aus § 3 — bei einer
  Umsetzung dort als Rev. 2 nachzuziehen.

---

## 1 Ist-Stand (gemessen 28.08.2026, Produktiv-DB `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`)

*Historischer Befund — beschreibt den Zustand VOR der Umsetzung. Die hier benannten
Mängel (Nullwerte, falsche Einheiten-Beschriftung, Drehfelder, fehlende Herkunft) sind
mit E1–E5 behoben; der Abschnitt bleibt als Begründung und Messreferenz stehen.*

**Drei feste Schadstoffspalten, drei Ebenen.** `energy_project_settings.co2/so2/nox` →
`Tab_Brennstoff_Stamm.CO2/SO2/NOx` (+ `Staub`, `PE_Faktor`) → `energy_carrier.co2/so2/nox`.
Gelesen von `KostenEmissionRechner` (nur CO₂) und `EmissionsBilanzRechner.LadeFaktoren`
(alle drei).

**Einheiten-Befund (neu):** Der Rechner führt CO₂ in **g/kWh**, SO₂/NOx in **mg/kWh**
(`EmissionsBilanzRechner.cs:20` „Einheiten: CO₂ g/kWh, SO₂/NOx mg/kWh (Kenndaten-Katalog)";
Umrechnung `MWh × Faktor / 1000 = kg`). Die Bestandswerte bestätigen das (Erdgas E: SO₂ 0,3,
NOx 110; Heizöl S: SO₂ 800 — als g/kWh physikalisch unmöglich, als mg/kWh klassische
Feuerungswerte). Der Dialog `ucFuelSettings` beschriftet aber **alle drei Felder mit
„[g/kWh]"** (`label10`/`label11`). Die Anzeige ist für SO₂/NOx um den Faktor 1000 falsch
beschriftet — ein Anzeigefehler, kein Rechenfehler. Wird mit diesem Konzept behoben (F4).

**Eingabefelder:** `numSO2`, `numCO2`, `numNOx` sind `NumericUpDown` — daher die Pfeile.

**Vorhandene Faktor-Kataloge** (Dialog „Gesetzliche Parameter", Jahreszeilen mit Quelle
und Status, Hausregel „neue Jahreszeile, kein Ändern"):

| Klasse | Inhalt | Charakter |
|---|---|---|
| `EF_BILANZ` | BAFA EEW 3.4 (Holz 27, Klär-/Deponiegas 50, Klärschlamm 10, Pellets 36, Strom 435, Biogas 152, Fernwärme 280 …), EBeV 2030 (Erdgas Hi 200,9 / Ho 181,4, Heizöl EL 266,4, Heizöl S 286,9, Flüssiggas 235,8 …), UBA-Strommix (direkt / THG ohne / mit Vorkette, 2020–2024), Substitution, biogene Verbrennung | reale Bilanz |
| `EF_NACHWEIS` | Träger-Faktoren der GEG-Linie inkl. Fernwärme-Regeln und Verdrängungsstrommix | gesetzlicher Nachweis |

**Fachlich entscheidend:** Die **BAFA-Werte sind bereits CO₂-Äquivalente** (inkl. CH₄/N₂O
und Vorketten, heizwertbezogen — steht so im Merkblatt). Die **EBeV-Werte sind reines CO₂**
(Brennstoff-Emissionshandel). Der UBA-Strommix liegt in beiden Lesarten vor
(`CO2_DIREKT` vs. `THG_MIT_VORKETTE`). Diese Unterscheidung trägt § 2 F3.

---

## 2 Fachliche Festlegungen

### F1 — Die Emissionsart ist ein Katalogobjekt

CO₂, SO₂, NOx sind keine Spaltennamen mehr, sondern Einträge eines Katalogs, der erweiterbar
ist (CH₄, N₂O, Staub, CO, eigene). Jede Art trägt Kürzel, Name, Einheit, CO₂-Äquivalenzfaktor
samt dessen Quelle, und die Flags aus F5.

**CO₂ ist Pflichtart:** immer vorhanden, nicht abwählbar, nicht löschbar, Äquivalenzfaktor
fest 1 (Feld gesperrt — „außer für CO₂").

### F2 — CO₂-Äquivalent heißt GWP₁₀₀, und Nicht-Treibhausgase tragen 0

Die Umrechnung Art → CO₂e ist das Treibhauspotenzial über 100 Jahre. Ausgeliefert werden
belegte Werte (IPCC AR6): **CH₄ fossil 29,8 · CH₄ biogen 27,0 · N₂O 273**. SO₂, NOx, Staub
und CO sind **keine Treibhausgase** — ihr Äquivalenzfaktor wird mit **0** ausgeliefert. Sie
erscheinen dann in der Bilanz als eigene Kennzahl (wie heute), tragen aber nichts zur
CO₂e-Summe bei. Der Faktor bleibt je Art **editierbar** (nur nicht bei CO₂): Wer etwa
indirekte Wirkungen ansetzen will, kann das — sichtbar und mit Quellenangabe, nicht still.

### F3 — Ist der Wert schon ein Äquivalent, wird nicht mehr umgerechnet
*(Nutzerentscheid 28.08.2026)*

Ein CO₂-**Feldwert aus BAFA EEW ist schon CO₂e** (CH₄/N₂O eingerechnet). Regel:

- Jeder Katalogwert trägt das Flag **`ist_co2e`** („Wert ist bereits ein Äquivalent").
  BAFA-Werte: ja. EBeV-Werte: nein. UBA `THG_*`: ja, `CO2_DIREKT`: nein.
- Trägt das CO₂-Feld eines Trägers einen `ist_co2e`-Wert, ist die CO₂e-Summe (F6)
  **genau dieser Wert** — die übrigen ausgewählten Arten werden für die Summe **nicht**
  aufsummiert (CH₄/N₂O stecken schon darin; jede Addition wäre doppelt gezählt).
  SO₂-, NOx- und Staub-Kennzahlen bleiben davon unberührt eigenständig. Die Summenzeile
  sagt es an: „CO₂-Wert ist bereits Äquivalent — Summe = Wert."

### F4 — Einheit je Art, Beschriftung berichtigt, Rechnung unverändert
*(bestätigt per Nutzerentscheid 28.08.2026 — Einheiten und Umrechnung bleiben wie bisher)*

Jede Art führt ihre Anzeigeeinheit: CO₂ (und die Summe CO₂e) **g/kWh**; SO₂, NOx, Staub, CO,
CH₄, N₂O **mg/kWh**. Die Bestandswerte werden **unverändert** übernommen — nur die
Beschriftung wird richtig (Ist-Stand § 1). Intern normiert der Rechner wie bisher auf g/kWh.

### F5 — Auswahl steuert Feldliste und Summe, einmal global

Je Art gibt es **`ausgewaehlt`** (global im Katalog, nicht je Träger): Ausgewählte Arten
erscheinen als Eingabefelder im Emissions-Tab jedes Trägers **und** gehen in die CO₂e-Summe
ein. Auslieferung: CO₂ (Pflicht, nicht abwählbar), SO₂ und NOx vorausgewählt; CH₄, N₂O,
Staub vorhanden, aber abgewählt. Eine Auswahl je Träger wäre eine zweite Wahrheit ohne
erkennbaren Nutzen — bewusst nicht vorgesehen *(bestätigt per Nutzerentscheid
28.08.2026)*. Sonderfälle laufen über die Werte: Eine global ausgewählte Art ohne Wert
(leer/0) trägt beim betreffenden Träger nichts zur Summe bei.

### F6 — Die CO₂e-Summe eines Trägers

```
CO2e [g/kWh] = Σ über ausgewählte Arten:  wert_normiert(g/kWh) × äquivalenzfaktor
```

CO₂ geht mit Faktor 1 ein. **Sonderfall F3:** Trägt das CO₂-Feld einen `ist_co2e`-Wert,
gilt `CO2e = wert(CO₂-Feld)` — ohne Aufsummierung. Mit der Auslieferung (SO₂/NOx-Faktor 0)
ist die Summe zunächst gleich dem CO₂-Wert — sie wird aussagekräftig, sobald CH₄/N₂O
gepflegt oder Faktoren gesetzt werden. Die Summe wird im Emissions-Tab **angezeigt**
(nur Anzeige, kein Speicherfeld — sie ist jederzeit ableitbar).

### F7 — Berechnungsmodus CO₂ oder CO₂-Äquivalent: Vorgabe global, gespeichert je Projekt
*(Nutzerentscheid 28.08.2026)*

Ein Schalter **„CO₂-Berechnung: CO₂ | CO₂-Äquivalent"**, sichtbar an beiden Orten
(Emissions-Tab des Energieträger-Dialogs und Katalog-Dialog). Die **globale Vorgabe**
(Katalogebene) gilt für neue Projekte; jedes Projekt **übernimmt sie beim Anlegen und
speichert sie selbst** (neues Feld in der Projekttabelle). Damit trägt ein Projekt seine
Rechenmethode dauerhaft in sich — es rechnet auch nach Jahren im Modus seiner
Entstehung, gleichgültig wie die Vorgabe inzwischen steht (Hausregel
Reproduzierbarkeit). Der Schalter im Emissions-Tab wirkt im Projektmodus auf das
Projekt, im Katalogmodus — wie der im Katalog-Dialog — auf die Vorgabe.
Bestandsprojekte erhalten bei der Migration den Modus `CO2` (heutiges Verhalten).
Wirkung auf die Emissionsrechnung:

```
Modus CO₂:   t/a = MWh/a × Faktor(Art CO₂) / 1000            (wie bisher)
Modus CO₂e:  t/a = MWh/a × CO2e-Summe (F6) / 1000
```

Betroffen sind `KostenEmissionRechner` (CO2Gesamt, CO2Spezifisch, Netzstrom-Anteil) und der
CO₂-Anteil des `EmissionsBilanzRechner`; SO₂-/NOx-Kennzahlen bleiben eigenständig.
**Unberührt** bleiben die BEHG-Abgabemenge (`CO2Brennstoff` — gesetzlich reines CO₂ nach
EBeV, ein Äquivalent wäre dort falsch) und die Nachweisrechnungen der Klasse `EF_NACHWEIS`.
Jeder Bericht, der die Kennzahl ausweist, nennt den Modus im Beschriftungstext
(„CO₂-Emissionen" vs. „CO₂-Äquivalent (GWP₁₀₀)") — sonst sind zwei Berichte desselben
Projekts nicht vergleichbar. Der Modus wird beim Rechenlauf in die Variantenergebnisse
übernommen, damit ein gespeichertes Ergebnis seinen Entstehungsmodus kennt.

#### Umsetzungsklärung zu F7 — Ausweisstellen und der Modusvermerk *(festgelegt 29.08.2026, E5)*

**Wo der Modus steht.** `VariantenDaten.EmissionsModus` und `EmissionsBilanz.Modus`
tragen ihn, gesetzt vom jeweiligen Rechner beim Lauf. **Eine Modus-Spalte in der
Ergebnispersistenz wäre falsch am Platz:** Die CO₂-Kennzahlen werden gar nicht
gespeichert — `Tab_Ergebnis*` führt den Simulationslauf (Energiemengen), und die
Emissionsrechnung läuft bei jedem Bericht frisch darüber. Eine Spalte am Ergebniskopf
beschriebe also eine Zahl, die dort nicht liegt, und liefe beim nächsten Bericht mit
geänderter Vorgabe auseinander. Der Vermerk gehört an die Zahl — und die Zahl entsteht
im Rechner. Damit beschriftet jeder Bericht genau das, was er ausrechnet, auch wenn
zwischen Rechenlauf und Druck jemand die Vorgabe umstellt. Kein Migrationsschritt nötig.

**Der wirksame Modus eines Rechenlaufs** (`EmissionenCtrl.ModusFuerRechenlauf`):
Projektfeld → bei leer die globale Vorgabe → bei leer `CO2`. Die mittlere Stufe fehlt
bewusst im Dialog (`ProjektModusLesen`): Dort heißt leer „noch nicht entschieden".

**Die Ausweisstellen — alle über `EmissionsAusweis`, keine stumm:**

| Stelle | Zeile |
|---|---|
| `KennzahlenKatalog.Alle(modus)` | `em.co2` „CO₂-Emissionen gesamt" ↔ „CO₂-Äquivalent gesamt (GWP₁₀₀)"; `em.co2_spez` entsprechend (deutsch **und** englisch) |
| `BausteineVergleich` (Word-Variantenvergleich) | Katalog mit dem Modus des Variantensatzes |
| `ExcelBerichtGenerator` Blatt „Vergleich" / Detailblatt je Variante | dito bzw. Modus der einen Variante |
| `BausteineWirtschaftlichkeit` + `ExcelBerichtGenerator`, Emissionsbilanz | Zeilentitel „CO₂ [t/a]" ↔ „CO₂-Äquivalent (GWP₁₀₀) [t/a]" |
| `UcWirtschaftlichkeit` (Bildschirm) | „CO₂-Vermeidung vs. getrennt [t/a]" ↔ „CO₂-Äquivalent-Vermeidung vs. getrennt (GWP₁₀₀) [t/a]" |
| `ucFuelSettings` / `Form_Emissionskatalog` | Modus-Schalter samt CO₂e-Summe (bereits E3/E4) |

Ein Vergleich über Projekte **verschiedener** Modi trägt den Sammeltitel „Modus je
Variante verschieden" statt stillschweigend den Modus des ersten Projekts.

**Nicht umgestellt, weil nicht modusabhängig:** „CO₂-Abgabe nach BEHG [€/a]" und die
Mehrjahreszeile „CO₂-Abgabe" (`WirtschaftlichkeitZeilen`), sämtliche SO₂-/NOx-Zeilen,
die Stromsteuer-Begründungen (`SteuerGutschriftRechner`, Klasse `EF_BILANZ_EBEV_*` aus
dem Gesetzeskatalog) und die CO₂-Ersparnis des Simulations-Dashboards, die mit festen
Pauschalfaktoren (0,42 / 0,20 kg je kWh) rechnet und den Trägerkatalog gar nicht anfasst.

**Grenze im Modus CO2E:** Die getrennte Referenz der Emissionsbilanz (Referenzkessel aus
`Tab_Brennstoff_Stamm`, Kraftwerkspark aus `Tab_Kraftwerkspark`) hat keine
Emissionsarten und damit keinen belegten Äquivalenzwert; sie bleibt beim reinen CO₂. Die
Vermeidungsspalte ist dann eine Obergrenze — der Bilanzhinweis sagt es an, statt die
Lücke durch einen erfundenen Referenzwert unsichtbar zu machen.

### F8 — Übernehmen heißt kopieren, mit Herkunft

Übernimmt der Anwender einen Katalogwert in einen Träger, wird der **Zahlenwert kopiert**
und die Herkunft (Katalogeintrag) am Trägerwert vermerkt. Eine spätere Änderung des
Katalogs ändert **keinen** Träger rückwirkend — dieselbe Logik, aus der die Jahreszeilen-
Hausregel der Gesetzesparameter kommt. Der Emissions-Tab zeigt die Herkunft an
(„BAFA EEW 3.4, 2026"); wird der Wert von Hand geändert, wechselt sie auf „Eigener Wert".

### F9 — Bestandsschutz

Die Spalten `co2/so2/nox` in `energy_carrier` und `energy_project_settings` bleiben
bestehen und werden als unterste Rückfallebene weiter gelesen. Die Struktur-Etappe (E2)
ändert **kein** Rechenergebnis; erst der vom Anwender betätigte Modus-Schalter (F7) tut es.

---

## 3 Datenmodell

Zwei neue Tabellen, eine erweiterte. Muster: `ReadOnly`-Kennzeichnung wie die
`_STAMM`-Tabellen, Migration idempotent.

### `emissionsart` — der Katalog der Schadstoffe (F1)

| Feld | Typ | Bedeutung |
|---|---|---|
| `id` | Autowert | |
| `kuerzel` | Text | `CO2`, `SO2`, `NOX`, `CH4_FOSSIL`, `CH4_BIOGEN`, `N2O`, `STAUB`, `CO`, eigene |
| `name` | Text | Anzeigename („Methan (fossil)") |
| `einheit` | Text | `g/kWh` oder `mg/kWh` (F4) |
| `co2_aequivalent` | Zahl | GWP₁₀₀; bei CO₂ fest 1 (F2) |
| `aequivalent_quelle` | Text | z. B. „IPCC AR6, GWP100" — leer bei 0 |
| `ist_pflicht` | Ja/Nein | nur CO₂ |
| `ausgewaehlt` | Ja/Nein | Feldliste + Summe (F5) |
| `ist_auslieferung` | Ja/Nein | mitgelieferte Arten sind nicht löschbar, nur abwählbar |
| `sortierung` | Zahl | |

**Auslieferung:** CO₂ (1, Pflicht) · SO₂ (0, ausgewählt) · NOx (0, ausgewählt) ·
CH₄ fossil (29,8) · CH₄ biogen (27,0) · N₂O (273) · Staub (0) — die letzten vier abgewählt.

### `emissionswert` — Katalogwerte und Trägerwerte in einer Tabelle

| Feld | Typ | Bedeutung |
|---|---|---|
| `id` | Autowert | |
| `emissionsart_id` | Zahl | → `emissionsart.id` |
| `carrier_id` | Zahl | → `energy_carrier.id`; **NULL = trägerunabhängige Katalogvorlage** |
| `quelle` | Text | `BAFA_EEW`, `EBEV_2030`, `UBA_STROMMIX`, `STAMM_ALT`, `EIGENER_WERT`, später `GEMIS` |
| `quelle_text` | Text | Anzeigetext mit Stand („BAFA EEW 3.4, 2026") |
| `wert` | Zahl | in der Einheit der Art (F4) |
| `ist_co2e` | Ja/Nein | Wert ist bereits ein Äquivalent (F3) |
| `ist_aktiv` | Ja/Nein | **der** für den Träger geltende Wert (je `carrier_id` + `emissionsart_id` höchstens einer) |
| `herkunft_id` | Zahl | bei kopierten Werten: der Katalogeintrag, aus dem kopiert wurde (F8) |
| `ist_auslieferung` | Ja/Nein | ausgelieferte Katalogzeilen sind nicht löschbar |
| `gueltig_ab` | Datum | Fortschreibung, Muster Jahreszeilen |

Eine Tabelle statt zwei, weil Katalogvorlage und Trägerwert dieselbe Gestalt haben — der
Unterschied ist nur, ob `carrier_id` gefüllt ist. Die Saat der Katalogvorlagen kommt aus
den **vorhandenen** gesetzlichen Parametern (`EF_BILANZ`/`EF_NACHWEIS` — je Schlüssel die
jüngste GESICHERTE Jahreszeile, mit Trägerzuordnung über eine Mapping-Liste im
Migrationsschritt: `EBEV_ERDGAS_HI` → Erdgas E/LL/Stadtgas usw.) und aus
`Tab_Brennstoff_Stamm` (Quelle `STAMM_ALT` — Altliteratur, als unbelegt gekennzeichnet).
Die gesetzlichen Parameter selbst bleiben unangetastet und führend für alles Gesetzliche;
`emissionswert` ist ihre Übernahme in die Trägerpflege, nicht ihr Ersatz.

### Erweiterung Bestand

- `energy_carrier.co2/so2/nox`, `energy_project_settings.co2/so2/nox`: bleiben (F9).
  Migration E2 legt je Träger für CO₂/SO₂/NOx aktive `emissionswert`-Zeilen mit den
  heutigen Zahlen an (Quelle `EIGENER_WERT`, bzw. `STAMM_ALT` wo der Wert erkennbar aus
  dem Stamm kommt). Doppelte Buchführung wird vermieden, indem der Schreibweg des Dialogs
  ab E3 **beide** Orte schreibt (neue Struktur führend, alte Spalten als Spiegel für
  Altleser).
- Globale **Vorgabe** `EMISSION_BERECHNUNGSMODUS` (`CO2` | `CO2E`) in der vorhandenen
  Einstellungs-/Parametertabelle, plus gleichnamiges Feld in der **Projekttabelle**
  (beim Anlegen aus der Vorgabe übernommen; Bestandsprojekte per Migration auf `CO2`) —
  F7.

**Leseweg der Rechner (je Art):** aktive `emissionswert`-Zeile des Trägers → sonst
Altspalten-Kette wie bisher (Projektwert → Stamm → Carrier). Die Projektübersteuerung
bleibt vorerst auf CO₂/SO₂/NOx beschränkt (Altspalten); eine generische
Projektübersteuerung je Art kommt erst mit der Quellenwahl-Umsetzung (deren F4/E3).

#### Umsetzungsklärung zu § 3 — die Reihenfolge der Lesekette *(festgelegt 29.08.2026, E5)*

Umgesetzt in `Allgemein\Wirtschaftlichkeit\EmissionsFaktorLader.cs`; beide Rechner rufen
ausschließlich dorthin.

```
1. Projektwert   energy_project_settings.co2/so2/nox   (nur Kernarten, nur im Projekt)
2. Katalog       aktive emissionswert-Zeile des Trägers (JEDE Art, auch CH₄/N₂O)
3. Stamm         Tab_Brennstoff_Stamm.CO2/SO2/NOx      (nur Kernarten)
4. Carrier       energy_carrier.co2/so2/nox            (nur Kernarten, F9)
```

**(1) Der Projektwert steht VOR dem Katalog.** Der Satz oben — „aktive Zeile → sonst
Altspalten-Kette" — liest sich wörtlich so, als käme der Katalog zuerst und die
Projektspalte als Teil der Rückfallkette danach. Das wäre ein Regressionsfehler: Die
Projektspalte ist seit jeher die oberste Ebene beider Rechner, und jedes Projekt mit
eigenem Faktor verlöre ihn in dem Augenblick, in dem E5 greift. Der Katalog rückt
deshalb an Stelle 2 ein — über die Altliteratur, unter die Anwendereingabe. Für Arten
ohne Altspalte (CH₄, N₂O, Staub) ist Stufe 2 die einzige Ebene.

**(2) Ein Projektwert gilt als reines CO₂** (`ist_co2e = falsch`). Zu einer Zahl in
`energy_project_settings.co2` gibt es keine Herkunft — sie kann Handeingabe, Altkopie
eines Katalogwertes oder ein übernommenes Äquivalent sein. Im Modus CO2E rechnen die
übrigen ausgewählten Arten deshalb dazu. Das ist die konservative Deutung: Sie setzt ein
Äquivalent im ungünstigen Fall geringfügig zu hoch an, während die Gegenannahme
(„Projektwert ist schon ein Äquivalent") CH₄ und N₂O stillschweigend unterschlüge. Der
F3-Sonderfall bleibt damit an die belegte Katalogzeile gebunden, wo er hingehört.

**„Gepflegt" heißt größer als 0** — dieselbe Regel wie bisher in beiden Rechnern. Ohne
sie blockierten die Nullzeilen, die Migrationsschritt 57 aus leeren Altspalten gesät hat,
den Brennstoff-Stamm.

**Wirkung.** Im Modus CO2 ändert sich an keinem der Referenzprojekte eine Zahl (Messung
im Umsetzungsvermerk). Wo ein Projekt KEINE eigene Zahl führt, gilt künftig der belegte
Katalogwert statt der unbelegten Altliteratur — die verzögerte Wirkung von E1.

---

## 4 Oberfläche

### 4.1 Energieträger-Dialog: Detailbereich bekommt zwei Reiter

Der heutige Detailbereich (eine lange Scrollseite) wird in ein `TabControl` gegliedert:
**„Preise & Umrechnung"** (Bestand unverändert) und **„Emissionen"** (neu — die bisherigen
drei Faktorfelder ziehen dorthin um):

```
┌ Emissionen ────────────────────────────────────────────────────────────┐
│ CO₂-Berechnung:  (•) CO₂   ( ) CO₂-Äquivalent (GWP₁₀₀) [Projekt/Vorgabe] │
│                                                                        │
│  Art            Wert        Einheit   Herkunft                         │
│  CO₂            240,0       g/kWh     STAMM_ALT (unbelegt)   [Katalog…]│
│  SO₂            0,3         mg/kWh    STAMM_ALT (unbelegt)   [Katalog…]│
│  NOx            110,0       mg/kWh    STAMM_ALT (unbelegt)   [Katalog…]│
│                                                                        │
│  CO₂-Äquivalent gesamt (ausgewählte Arten):  240,0 g/kWh               │
│  Hinweis im F3-Fall: „CO₂-Wert ist bereits Äquivalent — Summe = Wert, │
│     weitere Arten werden nicht aufsummiert."                           │
│                                                                        │
│  [Emissionsarten & Katalog verwalten…]                                 │
└────────────────────────────────────────────────────────────────────────┘
```

- Die Feldzeilen entstehen **dynamisch** aus den ausgewählten Arten (F5).
- Eingabe als `TextBox` mit Zahlprüfung — **keine `NumericUpDown` mehr**, die Pfeile
  entfallen (Anforderung 7). Format wie die übrigen Zahlfelder des Dialogs.
- `[Katalog…]` je Zeile öffnet den Katalog-Dialog (4.2) vorgefiltert auf Art + Träger;
  „Übernehmen" dort schreibt den Wert in die Zeile (F8).
- Speichern läuft über den vorhandenen Speichern-Knopf des Dialogs (Katalog- vs.
  Projektmodus wie bisher).

#### Umsetzungsklärung zu 4.1 — die KONTEXT-REGEL *(festgelegt 29.08.2026, E3)*

Das Konzept sagt „Katalog- vs. Projektmodus wie bisher". Was das je Art bedeutet,
war offen; umgesetzt ist:

| | **Katalogkontext** (Projekt 0) | **Projektkontext** |
|---|---|---|
| editierbar | **alle** ausgewählten Arten | **nur** CO₂, SO₂, NOx (die Arten mit Altspalte) |
| weitere Arten | — | erscheinen **lesend** mit ihrem Katalogwert, Tooltip „Pflege im Katalogkontext" |
| führender Schreibweg | aktive `emissionswert`-Zeile je Art (UPDATE bzw. INSERT) | `energy_project_settings.co2/so2/nox` wie bisher (NULL = Katalogwert gilt) |
| Spiegel | zusätzlich `energy_carrier.co2/so2/nox` für die drei Kernarten | — (die Altspalte IST hier der Schreibweg) |
| Herkunft (F8) | Übernahme → Katalogquelle mit `herkunft_id`; Handeingabe → `EIGENER_WERT`, `ist_co2e` fällt weg | dito, soweit die Art editierbar ist |

**Warum der Spiegel:** Bis Etappe E5 lesen `KostenEmissionRechner` und
`EmissionsBilanzRechner` ausschließlich die Altspalten (F9). Eine neue Struktur, die
der Altleser nicht sieht, wäre eine zweite Wahrheit — deshalb schreibt der
Katalogkontext beides, und die Zahl bleibt dieselbe. **E3/E4 ändern damit kein
Rechenergebnis.**

**Warum die Projektebene nur die drei Kernarten führt:** Eine generische
Projektübersteuerung je Art gibt es erst mit der Quellenwahl-Umsetzung (§ 3, letzter
Absatz). Bis dahin wäre eine vierte editierbare Art im Projekt eine Eingabe ohne
Speicherort.

**Deferred-Semantik (Ä12/Ä14) gilt unverändert:** Feldänderung, Katalog-Übernahme und
Modus-Umschaltung leben bis zum ausdrücklichen „Speichern" nur im Objekt; Abbrechen
und Trägerwechsel übernehmen nichts. Deshalb reicht der Katalog-Dialog seine
Übernahme an den Reiter ZURÜCK, statt sie selbst zu schreiben — im Verwaltungsmodus
(ohne aufrufenden Reiter) schreibt er sie sofort.

**Bestandsfelder als Wertträger:** Die drei `NumericUpDown` des Designer-Rasters
bleiben unsichtbar erhalten und werden bei jeder Änderung mitgeführt. Der vorhandene
Schreibweg (`ucFuelSettings.SpeichereWerte`) liest sie unverändert weiter — das ist
der Spiegel aus der Tabelle oben, ohne eine zweite Fassung derselben Regel. Fehlt der
Artenkatalog (Migrationsschritt 57 nicht gelaufen), werden dieselben drei Felder
wieder SICHTBAR im Emissionen-Reiter gezeigt; eine leere Maske wäre schlechter als die
alte.

### 4.2 Neuer Dialog „Emissionsfaktor-Katalog"

Ein Dialog, zwei Aufgaben — links die Arten, rechts die Werte der markierten Art:

```
┌ Emissionsfaktor-Katalog ───────────────────────────────────────────────────┐
│ CO₂-Berechnung:  (•) CO₂   ( ) CO₂-Äquivalent (GWP₁₀₀)  [globale Vorgabe]  │
│ ┌ Emissionsarten ──────────────┐ ┌ Werte: NOx — Erdgas E ────────────────┐ │
│ │ ☑ CO₂    g/kWh   (Pflicht)   │ │ Quelle              Wert     CO₂e?    │ │
│ │ ☑ SO₂    mg/kWh  ·GWP 0      │ │ STAMM_ALT           110      nein     │ │
│ │ ☑ NOx    mg/kWh  ·GWP 0      │ │ EIGENER_WERT        95       nein     │ │
│ │ ☐ CH₄ f. mg/kWh  ·GWP 29,8   │ │                                       │ │
│ │ ☐ N₂O    mg/kWh  ·GWP 273    │ │ [Übernehmen]  [Neu] [Bearb.] [Löschen]│ │
│ │ ☐ Staub  mg/kWh  ·GWP 0      │ │                                       │ │
│ │ [Neu] [Bearbeiten] [Löschen] │ │ (Werte ohne Träger = Vorlagen für     │ │
│ └──────────────────────────────┘ │  alle Träger, z. B. Strommix)         │ │
│                                  └───────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────────┘
```

- **Checkbox** = `ausgewaehlt` (F5); CO₂ fest gesetzt und ausgegraut.
- **Arten:** Neu/Bearbeiten (Name, Einheit, GWP + Quelle) für alle; Löschen nur für
  eigene Arten (`ist_auslieferung = falsch`) **und** nur, wenn keine Werte an ihnen
  hängen — sonst Hinweis mit Angebot „abwählen statt löschen".
- **Werte:** die Katalogeinträge der markierten Art für den übergebenen Träger (plus
  trägerunabhängige Vorlagen). Neu/Bearbeiten/Löschen nur für `EIGENER_WERT`-Zeilen;
  Auslieferungszeilen (BAFA/EBeV/UBA/STAMM_ALT) sind unveränderlich — aktualisiert wird
  über neue Jahreszeilen der gesetzlichen Parameter, die die Saat-Logik nachzieht.
- **Übernehmen** kopiert den markierten Wert als aktiven Trägerwert (F8) und schließt
  zurück in den Emissions-Tab.
- Ohne Träger-Kontext geöffnet (aus dem Admin-/Stammdatenmenü) zeigt der Dialog nur die
  Artenverwaltung und die trägerunabhängigen Vorlagen.

---

## 5 Faktoren-Aktualisierung (Auftrag „ggf. mit üblichen Quellen, evtl. GEMIS")

**Geprüft, keine Änderung nötig:** Die Kataloge `EF_BILANZ` und `EF_NACHWEIS` sind auf
Stand (BAFA EEW 3.4, EBeV 2030, UBA CLIMATE CHANGE 16/2026) und GESICHERT. Sie werden
Saat der Katalogvorlagen, nicht geändert.

**Ausstehend, bereits beschlossen:** die CO₂-Saat der Trägerwerte nach
`Konzept_CO2-Faktoren` Rev. 1 (10 Träger von 0 auf BAFA-Werte, 4 Korrekturen). Läuft als
eigener Migrationsschritt **vor** E2, damit die Artenmigration die berichtigten Werte
übernimmt *(Reihenfolge bestätigt per Nutzerentscheid 28.08.2026)*.

**SO₂/NOx/Staub (Luftschadstoffe):** Die Bestandswerte (mg/kWh, Feuerung ohne Vorkette,
Quelle unbekannt → `STAMM_ALT`, „unbelegt") sind in der Größenordnung plausibel, aber
nicht zitierfähig. GEMIS führt Vorketten-Werte, die um Größenordnungen höher liegen
(Beispiel Erdgas-Heizung, GEMIS: SO₂-Äquivalent ≈ 140 mg/kWh inkl. Vorkette gegenüber
0,3 mg/kWh Feuerungswert im Bestand). **Beschlossen (Nutzerentscheid 28.08.2026):**
Luftschadstoffe bleiben Feuerungswerte **ohne** Vorkette (Emissionsschutz-Sicht,
konsistent zum Bestand); als
belegte Quelle wird bei Umsetzung UBA TEXTE 97/2025 („Ermittlung von Emissionsfaktoren",
kleine/mittlere Feuerungsanlagen) bzw. GEMIS 5.x herangezogen und als neue Quelle
(`GEMIS` / `UBA_TEXTE`) eingesät — **erst nach Vorlage der Fundstelle, keine Zahlen aus
dem Gedächtnis** (Hausregel aus dem Quellenwahl-Konzept, § 4).

**CH₄/N₂O-Verbrennungswerte je Träger:** werden **nicht** eingesät (keine belegte
Fundstelle greifbar; zudem F3 — mit BAFA-CO₂e wären sie doppelt). Die Arten sind da,
die Werte trägt ein, wer sie braucht und belegt.

### 5.1 Mapping-Liste gesetzlicher Parameter → Katalogträger (E2, zur Durchsicht)

*Umgesetzt am 29.08.2026 in `SchemaMigration.GESETZ_MAPPING` (Migrationsschritt 57).
Gesät wird je Schlüssel die **jüngste Jahreszeile mit Status GESICHERT**; VORLAEUFIGE
und PROGNOSE-Zeilen bleiben außen vor. Alle Zeilen entstehen als **Vorlagen**
(`ist_aktiv = falsch`, `ist_auslieferung = wahr`) — sie ändern keinen Trägerwert.
Dies ist die Liste, um deren Durchsicht § 9 Punkt 5 bittet.*

| Schlüssel | Wert [g/kWh] | ab | `quelle` | `ist_co2e` | Katalogträger |
|---|---|---|---|---|---|
| `EF_BILANZ_EBEV_ERDGAS_HI` | 200,9 | 2023 | `EBEV_2030` | nein | Erdgas E · Erdgas LL · Stadtgas |
| `EF_BILANZ_EBEV_ERDGAS_HO` | 181,4 | 2023 | `EBEV_2030` | nein | *ohne Träger* (brennwertbezogen) |
| `EF_BILANZ_EBEV_HEIZOEL_EL` | 266,4 | 2023 | `EBEV_2030` | nein | Heizöl EL · Heizöl L · Heizöl L Variante · Heizöl L var |
| `EF_BILANZ_EBEV_HEIZOEL_S` | 286,9 | 2023 | `EBEV_2030` | nein | Heizöl S |
| `EF_BILANZ_EBEV_FLUESSIGGAS` | 235,8 | 2023 | `EBEV_2030` | nein | Flüssiggas |
| `EF_BILANZ_EBEV_PFLANZENOEL` | 266,4 | 2023 | `EBEV_2030` | nein | Tierische Fette |
| `EF_BILANZ_EBEV_BIODIESEL` | 266,4 | 2023 | `EBEV_2030` | nein | *ohne Träger* |
| `EF_BILANZ_EBEV_BIOMASSE` | 0 | 2023 | `EBEV_2030` | nein | Scheitholz · Holzpellets · Holzhackschnitzel |
| `EF_BILANZ_BAFA_BIOGAS` | 152 | 2026 | `BAFA_EEW` | **ja** | Biogas · Biogas 2 · Biogas Variante ¹ |
| `EF_BILANZ_BAFA_PELLETS` | 36 | 2026 | `BAFA_EEW` | **ja** | Holzpellets |
| `EF_BILANZ_BAFA_HOLZ_TROCKEN` | 27 | 2026 | `BAFA_EEW` | **ja** | Scheitholz · Holzhackschnitzel |
| `EF_BILANZ_BAFA_FERNWAERME` | 280 | 2026 | `BAFA_EEW` | **ja** | Fernwärme ¹ |
| `EF_BILANZ_BAFA_STROM` | 435 | 2026 | `BAFA_EEW` | **ja** | Elektrische Energie · Elektrische Energie 2 · Strom Variante ¹ |
| `EF_BILANZ_BAFA_KLAERGAS` | 50 | 2026 | `BAFA_EEW` | **ja** | *ohne Träger* |
| `EF_BILANZ_BAFA_DEPONIEGAS` | 50 | 2026 | `BAFA_EEW` | **ja** | *ohne Träger* |
| `EF_BILANZ_BAFA_KLAERSCHLAMM` | 10 | 2026 | `BAFA_EEW` | **ja** | *ohne Träger* |
| `EF_BILANZ_BAFA_BIODIESEL` | 70 | 2026 | `BAFA_EEW` | **ja** | *ohne Träger* ² |
| `EF_BILANZ_STROMMIX_CO2_DIREKT` | 379 | 2023 | `UBA_STROMMIX` | nein | Elektrische Energie · Elektrische Energie 2 · Strom Variante |
| `EF_BILANZ_STROMMIX_THG_OHNE_VORKETTE` | 387 | 2023 | `UBA_STROMMIX` | **ja** | dieselben drei Stromträger |
| `EF_BILANZ_STROMMIX_THG_MIT_VORKETTE` | 442 | 2023 | `UBA_STROMMIX` | **ja** | dieselben drei Stromträger |
| `EF_NACHWEIS_HEIZOEL` | 310 | 2020 | `GEG_NACHWEIS` | nein | Heizöl EL · L · L Variante · L var · S |
| `EF_NACHWEIS_ERDGAS` | 240 | 2020 | `GEG_NACHWEIS` | nein | Erdgas E · Erdgas LL · Stadtgas |
| `EF_NACHWEIS_FLUESSIGGAS` | 270 | 2020 | `GEG_NACHWEIS` | nein | Flüssiggas |
| `EF_NACHWEIS_STEINKOHLE` | 400 | 2020 | `GEG_NACHWEIS` | nein | Steinkohle ³ |
| `EF_NACHWEIS_BRAUNKOHLE` | 430 | 2020 | `GEG_NACHWEIS` | nein | Braunkohlebrikett |
| `EF_NACHWEIS_HOLZ` | 20 | 2020 | `GEG_NACHWEIS` | nein | Scheitholz · Holzpellets · Holzhackschnitzel |
| `EF_NACHWEIS_STROM_NETZ` | 100 | 2027 | `GEG_NACHWEIS` | nein | Elektrische Energie · Elektrische Energie 2 · Strom Variante |
| `EF_NACHWEIS_BIOGAS` | 80 | 2027 | `GEG_NACHWEIS` | nein | Biogas · Biogas 2 · Biogas Variante |
| `EF_NACHWEIS_BIOOEL` | 80 | 2027 | `GEG_NACHWEIS` | nein | Tierische Fette |
| `EF_NACHWEIS_BIOGAS_GEBAEUDENAH` | 70 | 2027 | `GEG_NACHWEIS` | nein | *ohne Träger* |
| `EF_NACHWEIS_BIOMETHAN` | 80 | 2027 | `GEG_NACHWEIS` | nein | *ohne Träger* |
| `EF_NACHWEIS_BIOGENES_FLUESSIGGAS` | 80 | 2027 | `GEG_NACHWEIS` | nein | *ohne Träger* |
| `EF_NACHWEIS_ABWAERME` | 10 | 2027 | `GEG_NACHWEIS` | nein | *ohne Träger* |

¹ Deckungsgleich mit der BAFA-Saat aus Etappe E1 (gleiche Art, gleicher Träger, gleiche
Quelle, gleicher Wert) — es entsteht **eine** Zeile, nicht zwei. Deshalb tragen alle
BAFA-Vorlagen denselben kurzen Quellentext „BAFA EEW 3.4, 2026".

² Der Wert 70 steht bei „Tierische Fette" bereits als abgeleitete BAFA-Saat (E1); eine
zweite Zeile mit derselben Zahl an demselben Träger sagt nichts Zusätzliches.

³ **Nicht** an Koks: dessen 335 g/kWh sind schon eine Steinkohle-Analogie
(`Konzept_CO2-Faktoren` § 2.3) — eine zweite darüber wäre eine Analogie zur Analogie.

**Bewusst NICHT gesät** — jede Auslassung mit ihrem Grund:

| Schlüssel | Grund |
|---|---|
| `EF_BILANZ_EBEV_UMRECHNUNG_HO` | Umrechnungsgröße Brenn-/Heizwert (GJ/MWh), kein Emissionsfaktor |
| `EF_BILANZ_SUBSTITUTION_STROM` · `EF_BILANZ_BIOGEN_VERBRENNUNG` | Rechenregeln einer methodischen Wahl, keine Trägerfaktoren; beide zudem VORLAEUFIG |
| `EF_NACHWEIS_FW_KWK_*` · `EF_NACHWEIS_FW_HEIZWERK_*` · `EF_NACHWEIS_FW_VORKETTE_*` | Regeln zur **Bildung** eines Fernwärmefaktors aus dem Erzeugungsmix, nicht der Faktor selbst |
| `EF_NACHWEIS_VERDRAENGUNGSSTROMMIX` | Gutschriftregel für KWK-Strom; entfällt zum 01.01.2027 ersatzlos (L12) |
| Klassen `PEF_NACHWEIS`, `KWKG`, `ENERGIESTEUER`, `STROMSTEUER`, `CO2_PREIS`, `UMSATZSTEUER` | keine Emissionsfaktoren |
| UBA-Strommix 2024/2025 | Status VORLAEUFIG bzw. geschätzt — die jüngste GESICHERTE Zeile ist 2023 |

**Träger ohne gesetzliche Vorlage:** `Wasserstoff` (kein Schlüssel im Katalog),
`Koks` (siehe ³), `Heizöl Bio 10`/`Heizöl Bio 15` — die GEG-Linie kennt Heizöl und
Bioöl getrennt, eine Mischungsregel gibt sie nicht her. Alle drei tragen ihre
BAFA-Saat aus E1 und ihre Stammwerte; mehr wäre erfunden.

### 5.2 Saatvorlage E6 — belegte Quellwerte als Vorlagen (UBA v2.1, GEMIS 5.2)

*Geschrieben 29.08.2026 nach Sichtung beider Arbeitsmappen (`Quellen\Emissionsfaktoren\`,
jede Zahl per Zellkoordinate belegt, Doppel-Lesung mit zwei unabhängigen Parsern ohne
Abweichung; Rohextraktion mit allen Koordinaten:
`E6_Sollwerte_Rohextraktion.md` der Analyse-Session). E6 läuft als
**Migrationsschritt 58** — Reihenfolge nach dem Vorfall vom 29.08. 09:25 zwingend:
erst Schrittkonstante + Methode + `SCHRITTE`-Eintrag, **dann** `ZIEL_VERSION` auf 58.*

**Regeln der Etappe:**

1. **E6 sät ausschließlich Vorlagen** (`ist_aktiv = falsch`, `ist_auslieferung = wahr`,
   `herkunft_id` leer): kein aktiver Trägerwert, keine Altspalte, kein Rechenergebnis
   ändert sich. Abnahme: Erst- und Zweitlauf ändern keinen aktiven Wert; die
   Emissionskennzahlen aller Bestandsprojekte sind vorher/nachher identisch; Zweitlauf
   legt 0 Zeilen an.
2. **UBA-Quelle (`UBA_2024`)** = Blatt `01_Stationäre_Verbrennung` der UBA-Liste v2.1
   (Bezugsjahr 2024, veröffentlicht 02/2026, Lizenz CC0 1.0 laut Impressum der Datei):
   Feuerung **ohne Vorkette**, unterer Heizwert (Hu; Beleg Blatt 01, Zelle A4).
   Übernommen werden **nur die Einzelgas-Spalten** `kg CO2`/`kg CH4`/`kg N2O` — nie die
   CO₂e-Spalte, denn die trägt fremde GWP-Gewichte („meist AR5",
   `Allgemeine_Hinweise!A16`), während der Katalog selbst nach AR6 summiert (F2/F6).
   Alle Zeilen `ist_co2e = falsch`. Biogene Träger führen dort kein Verbrennungs-CO₂
   (leere Zelle; biogener Anteil separat „Außerhalb der Scopes") — für sie entstehen
   nur CH₄- und N₂O-Vorlagen, passend zur Konvention `co2 = 0` der Holzträger.
3. **GEMIS-Quelle (`GEMIS_52`)** = IINAS-Ergebnistabelle GEMIS 5.2 (12/2025, aktuellste
   Fassung), Blatt `Wärme-end 2020` (je kWh Endenergie, „inputbezogen", Zelle B1) bzw.
   `Strom-lokal DE 2000-2024` (Niederspannung inkl. Netzverluste, jüngste Zeile 2024).
   Systemgrenze dort ausnahmslos **Lebenszyklus inkl. Vorkette und Anlagenherstellung**
   (Zelle B5) — reine Feuerungswerte gibt die Datei nicht her. **Nutzerentscheid
   29.08.2026:** Diese Werte kommen trotzdem in den Katalog, aber ausschließlich als
   Vorlagen mit Systemgrenze im Anzeigetext (`DbWerte.EMISSIONSWERT_TEXT_GEMIS_52_*`
   nennt „inkl. Vorkette (LCA)"); die aktiven Luftschadstoff-Werte bleiben bei der
   Feuerungssicht aus § 9 Punkt 2. Übernommen werden nur **SO₂ (Spalte C — nicht das
   SO₂-Äquivalent in Spalte B!), NOx (D), Staub (E)**; die GEMIS-THG-Spalten bleiben
   außen vor (CO₂/CO₂e ist BAFA-/EBeV-Territorium, CH₄/N₂O kämen mit fremder
   Systemgrenze). **Zuordnung ausschließlich über Spalte A** — die Kommentarspalte B
   der Datei ist nachweislich verrutscht.
4. **CH₄-Zuordnung:** keine Quelle trennt fossil/biogen — die Zuordnung folgt dem
   Träger: fossile Träger → `CH4_FOSSIL`, biogene → `CH4_BIOGEN`.
5. **Einheiten und Rundung:** UBA liefert kg/kWh (CO₂ ×1000 → g/kWh; CH₄/N₂O ×10⁶ →
   mg/kWh), GEMIS g/kWh (×1000 → mg/kWh). Gesät wird kaufmännisch auf 3 Nachkommastellen
   der Zieleinheit; § 5.2 nennt die gerundeten Saatwerte, die Rohwerte stehen in der
   Rohextraktion.
6. **`gueltig_ab`** = Zeitbezug der Quelle: UBA 01.01.2024; GEMIS Wärme 01.01.2020,
   GEMIS Strom 01.01.2024. Anzeigetexte: die drei `DbWerte`-Konstanten
   `EMISSIONSWERT_TEXT_UBA_2024` / `_GEMIS_52_WAERME` / `_GEMIS_52_STROM`.
   Die drei **trägerlosen** UBA-Zeilen bekommen ihren Betreff an den Anzeigetext
   angehängt („… — Biomethan" / „… — Deponiegas" / „… — Klärgas") — dasselbe Muster,
   mit dem Schritt 57 seine trägerlosen Gesetzesvorlagen kenntlich macht. Dieser Zusatz
   ist zugleich **Teil des Idempotenzschlüssels** (Quelle, Art, Träger, Quellentext) und
   die einzige Unterscheidung wertgleicher trägerloser Zeilen: Deponiegas und Klärgas
   führen dieselben Zahlen und tragen beide `carrier_id = NULL`; ohne den Zusatz fielen
   sie zusammen, und die Saat ergäbe 81 statt 85 Zeilen.

**Tabelle A — UBA-Vorlagen** (Blatt `01_Stationäre_Verbrennung`, kWh-Zeilen; Werte
gerundet, Zeile/ID zur Kontrolle):

| Quellzeile (ID) | Katalogträger | CO₂ [g/kWh] | CH₄ [mg/kWh] | N₂O [mg/kWh] |
|---|---|---|---|---|
| Erdgas (Heizwert), Z. 39 (`01_10_02_004_01`) | Erdgas E · Erdgas LL | 202,396 | 10,8 (fossil) | 0,905 |
| Heizöl leicht, Z. 33 (`01_10_02_002_01`) | Heizöl EL · L · L Variante · L var | 266,472 | 0,165 (fossil) | 1,967 |
| Steinkohle/Kohle, Z. 42 (`01_10_02_006_01`) | Steinkohle | 351,420 | 482,17 (fossil) | 41,393 |
| Braunkohle/Briketts, Z. 31 (`01_10_02_001_01`) | Braunkohlebrikett | 353,124 | 853,632 (fossil) | 18,726 |
| Wald-Scheitholz **Kessel**, Z. 22 (`01_10_01_007_01`) ¹ | Scheitholz | — (biogen) | 20,444 (biogen) | 1,008 |
| Pellets, Z. 28 (`01_10_01_009_01`) | Holzpellets | — (biogen) | 1,79 (biogen) | 1,202 |
| Biogas, Z. 10 (`01_10_01_002_01`) | Biogas · Biogas 2 · Biogas Variante ² | — (biogen) | 1770,3 (biogen) | 5,544 |
| Biomethan, Z. 12 (`01_10_01_003_01`) | *ohne Träger* | — | 978,066 (biogen) | 3,42 |
| Deponiegas, Z. 15 (`01_10_01_004_01`) | *ohne Träger* | — | 1124,208 (biogen) | 5,544 |
| Klärgas, Z. 17 (`01_10_01_005_01`) | *ohne Träger* | — | 1124,208 (biogen) | 5,544 |

¹ Die Liste führt Scheitholz doppelt (Einzelraumfeuerung `01_10_01_006_01` / Kessel,
CO₂e-Differenz ≈ Faktor 18). EPOS-Plan plant Heizzentralen — gesät wird die
**Kessel-Zeile**; die Einzelraumfeuerung bleibt bewusst draußen.
² Biogas-Zeile an alle drei Biogas-Träger — dasselbe Fächerungsmuster wie § 5.1.

**Tabelle B — GEMIS-Vorlagen** (Luftschadstoffe, mg/kWh Endenergie; Blatt
`Wärme-end 2020`, Strom aus `Strom-lokal DE 2000-2024` Zeile 2024):

| Quellzeile (Spalte A wörtlich) | Katalogträger | SO₂ | NOx | Staub |
|---|---|---|---|---|
| `Erdgas-Hzg 100%` (Z. 33) | Erdgas E · Erdgas LL | 6,007 | 137,744 | 5,419 |
| `Heizöl-Hzg 100%` (Z. 32) | Heizöl EL · L · L Variante · L var | 172,411 | 190,137 | 19,919 |
| `Öl-schwer-Kessel-Industrie-100%` (Z. 51) | Heizöl S | 1858,195 | 597,393 | 97,049 |
| `Flüssiggas-Hzg 100%` (Z. 34) | Flüssiggas | 3,168 | 63,618 | 2,589 |
| `StK-Brik-Hzg 100%` (Z. 37) | Steinkohle | 1976,023 | 276,047 | 819,994 |
| `StK-Koks-Hzg 100%` (Z. 38) | Koks | 1973,674 | 514,369 | 71,803 |
| `BrK-Brik-rhei-Hzg 100%` (Z. 36) ³ | Braunkohlebrikett | 307,107 | 335,136 | 406,521 |
| `Fernwärme-mix (KWK: energiealloziert)` (Z. 40) | Fernwärme | 106,592 | 336,757 | 14,803 |
| `Stromnetz-lokal 2024` (Z. 48/71) | Elektrische Energie · Elektrische Energie 2 · Strom Variante | 138,640 | 331,119 | 25,712 |

³ GEMIS führt rheinische und Lausitzer Briketts getrennt; gesät wird **rheinisch**
(größtes Revier, Marktstandard). Lausitz zur Einordnung: SO₂ 1047,768 / NOx 321,759 /
Staub 321,528 — wer Lausitzer Ware einsetzt, pflegt den Wert von Hand nach.

**Erwartete Wirkung:** 85 neue Vorlagenzeilen — UBA 40 (8 × CO₂; je 16 × CH₄ und N₂O:
fossil 8 = Erdgas E + LL, vier Heizöl-Träger, Steinkohle, Braunkohlebrikett, biogen 8 =
Scheitholz, Holzpellets, drei Biogas-Träger, Biomethan/Deponiegas/Klärgas trägerlos)
+ GEMIS 45 (15 Träger × SO₂/NOx/Staub); 0 geänderte aktive Werte, Zweitlauf 0.
*(Zählung berichtigt 29.08.2026 — die zuvor genannte 81 unterschlug die
Biogas-Fächerung aus Fußnote ².)*

**Bewusst NICHT gesät** — jede Auslassung mit Grund:

| Auslassung | Grund |
|---|---|
| UBA-Spalte `kg CO2e` | fremde GWP-Basis („meist AR5") — der Katalog summiert selbst nach AR6 (Regel 2) |
| UBA `Erdgas (Brennwert)` (Z. 41) | Katalog ist Hu-basiert; Ho-Zeile nur Doku (Umrechnung 0,903, Blatt 01 B5) |
| UBA Blatt `07` (Vorketten/Gesamt) | zweite Systemgrenze ohne Auftrag — Blatt 01 ist die beschlossene Feuerungssicht |
| UBA `Altholz/Holzreste` für Holzhackschnitzel | anderer Brennstoff; Hackschnitzel fehlt in der Liste — Analogie wäre erfunden |
| Biogenes CO₂ (Blatt „Außerhalb der Scopes") | Katalogkonvention: biogene Träger tragen CO₂ = 0; Pellets/Biomethan dort ohnehin nur „Platzhalter" |
| GEMIS CO₂/CO₂e/CH₄/N₂O | THG kommen aus BAFA/EBeV/UBA; GEMIS-THG brächten die LCA-Grenze in Arten, die ohne sie belegt sind |
| GEMIS `SO2-Äquivalent` (Spalte B) | Versauerungs-Aggregat (inkl. NOx/NH₃), keine SO₂-Masse |
| GEMIS Holz-Luftschadstoffe (`Holz-Scheit`/`-Pellets`/`-Hackschnitzel`) | stehen nur im Blatt `Heizen (en) 2020` — **je kWh Nutzwärme**, anderer Nenner; ohne belegten Nutzungsgrad keine saubere Umrechnung |
| GEMIS `BrK-Brik-Lau-Hzg 100%` | eine Zeile je Träger; rheinisch gesetzt (³) |
| Wasserstoff · Stadtgas · Tierische Fette | in beiden Quellen nicht vorhanden (Stadtgas-Analogie zu Erdgas wäre die dritte Analogiestufe) |
| Biogas-Luftschadstoffe | GEMIS führt Biogas nur als BHKW-Strom, nicht als Wärmeoption |

**Lizenzlage:** UBA CC0 1.0 (im Impressum der Datei, mit Quellenvermerk-Auflage —
erfüllt durch `quelle_text`). GEMIS: IINAS stellt die Ergebnisse „zur freien Verwendung
unter Quellenangabe" bereit, ohne konkrete CC-Variante; für die Auslieferung als
Vorlagen ausreichend, eine formlose Bestätigung bei IINAS bleibt empfohlen (offener
Punkt 7).

---

## 6 Umsetzung in Etappen

| Etappe | Inhalt | Ergebnis |
|---|---|---|
| **E1** | CO₂-Saat nach `Konzept_CO2-Faktoren` Rev. 1 (eigener Migrationsschritt, Regeln von dort: Sicherung, laccdb-Sperre, ACE-Falle, Idempotenz) | **UMGESETZT (Schritt 56, 29.08.2026)** — Trägerwerte belegt statt 0 |
| **E2** | Tabellen `emissionsart` + `emissionswert` anlegen und säen (Arten-Auslieferung; Vorlagen aus `EF_BILANZ`/`EF_NACHWEIS` mit Mapping-Liste; Trägerwerte aus Bestandsspalten) | **UMGESETZT (Schritt 57, 29.08.2026)** — Modell steht, **kein Ergebnis ändert sich** (F9) |
| **E3** | Emissions-Tab im Energieträger-Dialog (dynamische Felder, TextBox statt Spinner, Einheiten richtig, CO₂e-Summe, Herkunft, Warnung F3), Schreibweg beidseitig | **UMGESETZT (29.08.2026)** — Anwender pflegt im neuen Modell; Kontext-Regel als Umsetzungsklärung in § 4.1 |
| **E4** | Katalog-Dialog (4.2): Artenverwaltung, Werteverwaltung, Übernehmen | **UMGESETZT (29.08.2026)** — `Form_Emissionskatalog` samt Schutzregeln; Katalogpflege vollständig |
| **E5** | Modus-Schalter (F7): globale Vorgabe + Projektfeld, an beiden Orten; `KostenEmissionRechner` + `EmissionsBilanzRechner` modusfähig; Berichte weisen Modus aus; Modus in Variantenergebnisse; `STROMMIX_CO2_G_JE_KWH` 380→435 *(Nutzerentscheid 29.08.2026)* | **UMGESETZT (29.08.2026)** — eine Lesekette für beide Rechner (`EmissionsFaktorLader`), Modus wirksam und am Ergebnis vermerkt, Ausweis über `EmissionsAusweis`; Modus CO2 zahlengleich außer dem Strommix-Randfall |
| **E6** | Quellen-Saat als Vorlagen nach § 5.2: UBA-Liste v2.1 Blatt 01 ohne Vorkette, GEMIS 5.2 als LCA-Vorlagen | **UMGESETZT (Schritt 58, 29.08.2026)** — 85 Vorlagen, kein aktiver Wert berührt; Nachweis `E6_QuellenSaat_Protokoll.md` |

Prüfstand je Etappe: Smoke der beiden Rechenwege; E2 zusätzlich Vorher/Nachher-Vergleich
aller Emissionskennzahlen über die Referenzläufe (muss identisch sein); E5 gezielter
Vergleich Modus CO₂ vs. CO₂e an einem Handbeispiel.

---

## 7 Abnahmekriterien

1. Der Emissions-Tab zeigt genau die ausgewählten Arten; CO₂ ist nicht abwählbar; die
   Pfeile sind weg; SO₂/NOx sind mit mg/kWh beschriftet und die Zahlen unverändert.
2. Ein Wert lässt sich aus dem Katalog übernehmen; die Herkunft steht am Feld; Handeingabe
   setzt sie auf „Eigener Wert".
3. Eigene Arten und eigene Werte lassen sich anlegen, ändern, löschen; Auslieferung und
   CO₂ nicht.
4. Die CO₂e-Summe entspricht F6 (Handrechnung), reagiert auf Auswahl-Änderung sofort;
   im F3-Fall ist die Summe gleich dem CO₂-Wert und der Tab weist darauf hin.
5. Modus-Umschaltung ändert CO₂-Kennzahlen nachvollziehbar (Handbeispiel), lässt
   BEHG-Abgabe und Nachweisrechnung unverändert, und jeder Bericht nennt den Modus.
   Ein Projekt behält seinen gespeicherten Modus, auch wenn die globale Vorgabe
   wechselt; ein neues Projekt übernimmt die Vorgabe.
   **ERFÜLLT (29.08.2026, Handbeispiel Projekt 1030, Erdgas E, 4 423,19 MWh):**
   | Fall | Faktor CO₂ | Faktor CO₂e | Brennstoff-CO₂ Modus CO2 | Modus CO2E | BEHG-Menge |
   |---|---|---|---|---|---|
   | Katalogzeile BAFA 201, `ist_co2e` | 201 | **201** (F3: Summe = Wert) | 889,061 t | 889,061 t | 889,061 t |
   | EBeV-Zeile 200,9 aktiv + CH₄ fossil 100 mg/kWh gewählt | 200,9 | **203,88** = 200,9 + 0,1 × 29,8 | 888,619 t | 901,800 t | 888,619 t |
   | Projektübersteuerung 240 (gilt als reines CO₂) | 240 | **242,98** = 240 + 2,98 | 1 061,566 t | 1 074,747 t | 1 061,566 t |

   Jede Zahl ist die Handrechnung `MWh × Faktor / 1000`. **Die BEHG-Menge ist in
   beiden Modi identisch** (rechte Spalte), ebenso SO₂ (1,327 kg/a), NOx (486,551 kg/a)
   und die getrennte Referenz (2 436,677 t/a). Die Nachweisrechnungen der Klasse
   `EF_NACHWEIS` sind vom Modus nicht einmal erreichbar: `SteuerGutschriftRechner` liest
   seine Faktoren aus dem Gesetzeskatalog und ist unverändert.
6. Nach E2 liefern alle Referenzläufe identische Emissionskennzahlen; Zweitlauf der
   Migrationen ändert nichts. **ERFÜLLT und für E5 wiederholt:** Referenzlauf
   vorher/nachher 10/10 PASS; die Emissionskennzahlen aller 26 Projekte der
   Arbeitskopie sind im Modus CO2 unverändert bis auf den Strommix-Randfall.

---

## 8 Entscheide

*Anwenderentscheide, die dieses Konzept fortschreiben — jeweils mit Wortlaut, Umsetzung und
Stand. Sie stehen hier und nicht in § 2, weil § 2 die fachlichen Festlegungen der Rev. 1
trägt; ein Entscheid, der eine davon ändert, muss als solcher erkennbar bleiben.*

### B1 — EINE Emissionsquelle für alle Erzeuger *(07.09.2026, umgesetzt)*

> „Da CO₂, SO₂, NOₓ, CO und Staub in g/MWh kein CO₂-Äquivalent haben, sind diese Zahlen
> informativ. ‚Beim BHKW ist es umgekehrt, dort liest die Simulation genau diese
> Gerätespalten': Es soll der gepflegte CO₂-Wert herangezogen werden — der an dem
> Energieträger hängt (gilt generell für alle Erzeuger!). Es sollte dazu eine
> Emissionsdatenbank geben (siehe Konzept)."

**Die Lage vor dem Entscheid.** Dieselbe Anwendung führte **drei** Emissionsquellen:

| Stufe | Quelle vorher | Einheit |
|---|---|---|
| Wirtschaftlichkeit (`EmissionsBilanzRechner`), Kennzahlen (`KostenEmissionRechner`) | Emissionskatalog über `EmissionsFaktorLader` (§ 3) | CO₂ g/kWh, SO₂/NOx mg/kWh |
| Simulation **Heizkessel** (`SimulationSPK.Kesseldaten_Einlesen`) | `Tab_Brennstoff_Stamm` **unmittelbar** über die Brennstoff-ID des Geräts | g/kWh bzw. mg/kWh |
| Simulation **BHKW** (`SimulationBHKW.Moduldaten_Einlesen`) | die fünf **Gerätespalten** `Tab_BHKW.CO2/SO2/NOX/CO/Staub` | g/MWh |

Damit trug ein und dasselbe BHKW im Rechenlauf und in der Emissionsbilanz verschiedene
Zahlen, ohne dass irgendeine Anzeige den Unterschied genannt hätte. Der Kessel las die
Altspalte am Katalog vorbei: kein Projektwert, keine aktive `emissionswert`-Zeile, kein
Berechnungsmodus.

**Umgesetzt am 07.09.2026.** Ein Kern-Dienst
`Allgemein/Wirtschaftlichkeit/Emissionsquelle.cs` liefert je Projekt und Energieträger den
**wirksamen** Faktorsatz — CO₂ nach Modus (F7), dazu SO₂, NOₓ und Staub — samt der Ebene, aus
der er stammt. Simulation (Kessel und BHKW), Wirtschaftlichkeit, Kennzahlen und die
Autarkie-Kachel lesen seither über **diese eine Stelle**; die Lesekette selbst ist unverändert
die aus § 3 (`EmissionsFaktorLader`).

**Zwei Ergänzungen an der Kette, beide klein und begründet:**

* **Staub kommt mit.** `EmissionsFaktorSatz` führt jetzt `Staub`. Die Art ist im
  Auslieferungsstand **abgewählt** (F5); dann gilt `Tab_Brennstoff_Stamm.Staub` — dieselbe
  Ebene `STAMM`, die die Kette für die drei Kernarten ohnehin liest. Der Rückfall steht
  bewusst **außerhalb** der Zeilenliste, damit er weder die CO₂e-Summe (F6) noch die
  Vollständigkeitsprüfung der Emissionsbilanz verschiebt.
* **Ein fünftes Glied für Anlagen ohne Energieträger.** `Tab_Energieanlagen.ID_Carrier` ist im
  Bestand vielfach leer (in der Testdatenbank bei fünf von zwölf Referenzprojekten). Ohne
  Träger gibt es keinen Weg in den Katalog; statt einer stillen 0 gilt dann der **Brennstoff
  des Geräts** gegen dieselbe `Tab_Brennstoff_Stamm`, in der die Kette ohnehin endet
  (Ebene `BRENNSTOFF`). Das ist keine zweite Wahrheit — es ist dieselbe Zeile über einen
  anderen Schlüssel — und es ist genau das Verhalten, das der Kessel vorher hatte. Das
  Laufprotokoll nennt jeden solchen Fall, damit die fehlende Zuordnung sichtbar bleibt.

**Die fünf Gerätespalten sind seither „nur Anzeige".** `ParameterVerwendung` stuft sie bei
**Kessel und BHKW** gleich ein (`Verwendung.Dialog`); beide Katalogeditoren tragen über den
Feldern eine Herleitungszeile: „Nur zur Information — die Emissionsrechnung nimmt den Faktor
des Energieträgers aus dem Emissionskatalog." Die Werte bleiben pflegbar; sie sind eine
Herstellerangabe.

**Was sich dadurch ändert — und was nicht.** Die Emissionsgrößen der Simulation
(`Em_CO2_SPK`, `Em_CO2_BHKW` …) ändern sich, beim BHKW erheblich: Ein Modul mit
`Tab_BHKW.CO2 = 0` wies bisher **null** CO₂ aus, obwohl es Erdgas verbrannte. Kein anderes
Ergebnis ist betroffen — die Emissionswerte der Simulation stehen in **keiner**
Referenz-CSV und in keiner `Tab_Ergebnis*`-Spalte; der Referenzlauf der zwölf Projekte bleibt
**byte-gleich** (Nachweis unten, § 9 Punkt 8). Der Nachweis der Änderung steht deshalb in
`EPOS.Kern.Tests/EmissionsquelleTests.cs`, nicht im Referenzlauf.

**Kein Migrationsschritt.** Die Tabellen `emissionsart`/`emissionswert` reichen; die Saat der
Schritte 56 bis 58 bleibt unverändert. Ein neuer Schritt wäre nur nötig, wenn eine Art **CO**
angelegt werden soll — siehe § 9 Punkt 9.

### B2 — Die fünf Maßspalten der Wärmepumpe bleiben *(07.09.2026, Empfehlung angenommen, keine Programmarbeit)*

Zum Befund W14a‑E‑8‑B2 (`Laenge`, `Breite`, `Hoehe`, `Gewicht`, `Raum` in `Tab_WP_STAMM` —
die einzigen fünf Spalten aller sieben Kataloge mit der Stufe `Keine`) lautet der Entscheid
**„Empfehlung"**: Sie bleiben als **Referenzdaten** aus dem VDI‑3805‑Import stehen und sind in
der Parameterübersicht als „nicht verwendet" gekennzeichnet. **Keine Programmarbeit** — der
Entscheid ist hier vermerkt, damit die Stufe `Keine` nicht eines Tages als Versehen gilt und
jemand die Spalten löscht.

### W11a‑O‑2 — Die Autarkie-Kachel nimmt dieselbe Quelle *(07.09.2026, umgesetzt)*

Der offene Punkt aus Welle 11a: Die Kachel „CO₂-Ersparnis" der Ergebnisseite rechnete mit zwei
Literalen aus `DashboardForm.cs:355` — **0,42 kg/kWh** für verdrängten Netzstrom und
**0,20 kg/kWh** für verdrängte Wärme —, die mit keiner anderen Zahl des Hauses abgestimmt
waren. Sie sind durch Faktoren aus dem Emissionskatalog ersetzt:

* **Netzstrom** — der dem Projekt zugeordnete Träger mit `pricing_model = 'ELECTRICITY'`
  (`Emissionsquelle.Netzstrom`). Rückfall ohne Träger: **435 g/kWh**, derselbe Wert wie
  `KostenEmissionRechner.STROMMIX_CO2_G_JE_KWH` (BAFA EEW, „El. Strom (Effizienzmaßnahme)") —
  die Zahl steht seither **einmal**, in `Emissionsquelle.NETZSTROM_RUECKFALL_G_JE_KWH`, und
  wird von beiden Stellen gelesen. Das war der Kern des offenen Punktes: keine zweite
  Wahrheit.
* **Wärme** — der Energieträger des ersten Wärmeerzeugers des Projekts (Kessel vor BHKW,
  `Emissionsquelle.Waerme`). Rückfall ohne solchen Erzeuger: **200 g/kWh**, also die bisherige
  Kachelzahl.

**Bewusst nicht mitgeändert:** Die Wärmeseite rechnet weiterhin ohne Kesselwirkungsgrad
(1 kWh Solarwärme gegen 1 kWh Brennstoff). Ein Wirkungsgrad im Nenner wäre fachlich richtiger,
verschöbe die Kennzahl aber über den Faktortausch hinaus — das ist eine eigene Entscheidung
und keine Beifracht dieses Punktes.

### Em‑9.8 / Em‑9.9 — die sieben Fragen aus § 11.5 *(07.09.2026)*

> „Sieben Fragen: Empfehlung. 9.8 und 9.9: Empfehlung."

Damit gilt zu jeder der sieben Fragen die Empfehlung des Kapitels 11. Sie stehen hier
einzeln, weil zwei davon Regeln setzen, die man später nachschlagen können muss:

| Kennung | Entscheid | Umsetzung |
|---|---|---|
| **Em‑9.8‑Q1** | **Z1** — die Emissionsgrößen kommen JETZT in den Referenzexport | umgesetzt; R4 war beim Entscheid bereits gepusht, die Basis ist deshalb **`2026-09-07_R5_Zahlenrand`** statt R4 (sie friert im selben Zug die Entscheide W8‑O‑5d‑Q1/Q2 ein) |
| **Em‑9.8‑Q2** | **Zehn Skalare** — Kessel und BHKW je fünf Arten, **mit** CO, **ohne** `Em.Gesamt.*` | umgesetzt in `Referenzlauf/Ergebnisexport.cs` (§ 11.2.2). CO steht bewusst dabei, obwohl es 0 ist: So **ändert** ein späterer Trägerwert eine Zahl, statt einen Schlüssel **hinzuzufügen** |
| **Em‑9.8‑Q3** | **Einheit im Namen** — `Em.Kessel.Co2T` in t/a, `…So2Kg`/`…NoxKg`/`…CoKg`/`…StaubKg` in kg/a | umgesetzt; der Export rechnet **nicht** um. Ein Faktor 1 000 dort wäre eine dritte Umrechnungsnaht neben den zwei erlaubten (Hausregel Rechenkern, Punkt 4) |
| **Em‑9.8‑Q4** | **Die Einfrierregel aus § 11.2.6 gilt** | festgeschrieben in `Referenzlaeufe/LIESMICH.md` und im Absatz „Regressionsnetz" der Wurzel-`CLAUDE.md` |
| **Em‑9.9‑Q1** | **Erst mit Quelle bauen** — Schritt 68 ruht, bis eine Quelle mit CO-Faktoren **je Energieträger** vorliegt | keine Programmarbeit. Gemessen (§ 11.1.5): weder UBA v2.1 noch GEMIS 5.2 führt CO |
| **Em‑9.9‑Q2** | **§ 9 Punkt 9 wird geschlossen als „bewusst nicht"** — keine leere Art `CO` | Punkt 9 unten entsprechend geschlossen. `CoMgKwh` bleibt im Rechenweg vorbereitet und kostet nichts, solange es 0 ist |
| **Em‑9.9‑Q3** | **Keine Saat aus den Gerätespalten** `Tab_BHKW_STAMM.CO` / `Tab_Heizkessel_STAMM.CO` | keine Programmarbeit. Sie hängen am Gerät, nicht am Energieträger — das kehrte die Zuordnung um, die B1 gerade hergestellt hat. Sie bleiben Herstellerangabe und „nur Anzeige" |

**Die Regel, die Q4 setzt** (§ 11.2.6, hier im Wortlaut, weil sie ab jetzt für jeden gilt,
der die Testdatenbank anfasst):

> **Wer einen gesäten Emissionsfaktor der Testdatenbank ändert, friert im selben Schritt die
> Referenzbasis neu ein und begründet den Wechsel in `Referenzlaeufe/LIESMICH.md`.**
> Betroffen ist jede Änderung an `emissionsart` (Auswahl, Äquivalenzfaktor), an einem
> **aktiven** `emissionswert` (81 Zeilen), an `Tab_Brennstoff_Stamm.CO2/SO2/NOx/Staub`
> (25 Sätze), an `energy_project_settings.co2/so2/nox` der zwölf Projekte und am
> Berechnungsmodus eines Projekts. **Nicht** betroffen ist die Pflege von **Vorlagen**
> (`ist_aktiv = falsch`, 224 Zeilen) — sie erreichen die Lesekette nicht.

Der Einwand aus § 9 Punkt 8 („sie zöge auch jede Katalogpflege in den Regressionsvergleich")
bleibt berechtigt, trifft aber nicht die Katalogpflege des **Anwenders** — dessen Produktiv-DB
ist nicht die Testdatenbank —, sondern nur die Pflege der **Testdatenbank**, und die geschieht
ohnehin nur über Migrationsschritte oder `Werkzeuge/Testdatenbankschema`.

---

## 9 Offene Punkte

1. ~~Modus-Reichweite~~ — **entschieden 28.08.2026: globale Vorgabe + Projekt-Override**
   (F7).
2. ~~Luftschadstoffe mit oder ohne Vorkette~~ — **entschieden 28.08.2026: ohne** (§ 5).
3. ~~Fundstellen zur E6-Saat~~ — **geliefert 29.08.2026** (Nutzer): UBA-Liste
   „Emissionsfaktoren zur THG-Bilanzierung" v2.1/2024 (CC0 1.0) und GEMIS-5.2-
   Ergebnistabelle (IINAS) — statt des ursprünglich angedachten UBA TEXTE 97/2025.
   Quelldateien archiviert unter `Quellen\Emissionsfaktoren\`; Saat als E6 (§ 5.2).
4. ~~Auswahl je Träger statt global~~ — **entschieden 28.08.2026: global** (F5).
5. ~~Mapping-Liste gesetzliche Parameter → Träger (E2) bei Umsetzung zur Durchsicht
   vorlegen~~ — **vorgelegt 29.08.2026 in § 5.1** (Fernwärme: nur `BAFA_FERNWAERME`,
   die `EF_NACHWEIS_FW_*`-Regeln bleiben außen vor; Strom-Varianten: alle drei
   Stromträger erhalten dieselben fünf Vorlagen). **Durchsicht steht aus.**
6. Übernahme in `Konzept_Emissionsfaktoren_Quellenwahl` als dessen Rev. 2
   (generische Faktor-Zeilen statt fester Spalten), sobald jenes umgesetzt wird.
7. **IINAS-Nutzungsbestätigung für die GEMIS-Vorlagen einholen** (`info@iinas.org`).
   IINAS stellt die GEMIS-5.2-Ergebnisse „zur freien Verwendung unter Quellenangabe"
   bereit, nennt aber keine konkrete CC-Variante. Für die Auslieferung der 45
   GEMIS-Vorlagen aus § 5.2 reicht das; eine formlose Bestätigung schließt die Lücke.
   (Die UBA-Seite ist unkritisch: CC0 1.0 laut Impressum der Datei, Quellenvermerk
   erfüllt durch `quelle_text`.)
8. ~~Die Emissionswerte der Simulation stehen in keiner Referenz-CSV~~ —
   **entschieden 07.09.2026 („Sieben Fragen: Empfehlung"): Z1, umgesetzt in
   `880a9de`, Basis `2026-09-07_R5_Zahlenrand`.** Der Befund war richtig: Weder
   `aggregate.csv` noch eine Vektordatei führte eine Emissionsgröße, und `Tab_Ergebnis*`
   hat bis heute keine Emissionsspalte (Weg B ist in § 11.2.1 ausdrücklich abgelehnt).
   Der Umbau B1 änderte deshalb **kein** Feld der zwölf Referenzprojekte (12/12
   byte-gleich gegen `2026-09-06_R3_Straenge`), und das Regressionsnetz konnte eine
   Änderung an den Emissionsfaktoren nicht bemerken. Seit dem Entscheid schreibt
   `Referenzlauf/Ergebnisexport.cs` die **zehn Skalare** `Em.Kessel.*` / `Em.Bhkw.*`
   (§ 11.2.2, Einheit im Namen, nur wenn die Stufe gelaufen ist); die Basis
   `2026-09-07_R5_Zahlenrand` friert sie ein. Der Preis ist die **Einfrierregel** aus
   § 11.2.6 (§ 8, Em‑9.8‑Q4). Der Nachweis der KETTE bleibt daneben bestehen —
   `EPOS.Kern.Tests/EmissionsquelleTests.cs`, Abschnitt 5 hält die Naht zwischen beiden.
   **→ Kapitel 11** (Messung, Zuschnitt und die Fragen `Em‑9.8‑Q1` bis `Em‑9.8‑Q4`).
9. ~~Keine Emissionsart „CO"~~ — **geschlossen 07.09.2026 als „BEWUSST NICHT"**
   („Sieben Fragen: Empfehlung", § 8/Em‑9.9). Der Artenkatalog führt sieben Arten,
   Kohlenmonoxid ist nicht darunter, und `Tab_Brennstoff_Stamm` hat keine CO-Spalte; seit
   B1 ist die CO-Emission von Kessel **und** BHKW deshalb 0 (beim BHKW kam sie vorher aus
   der Gerätespalte). Die Art wird **nicht** angelegt, solange keine Quelle mit
   CO-Faktoren **je Energieträger** vorliegt (Q1) — gemessen führt weder die UBA-Liste
   v2.1 noch GEMIS 5.2 CO (§ 11.1.5), ein Migrationsschritt hätte also keine einzige
   belegte Zahl zu säen. Eine **leere** Art wird ebenfalls nicht angelegt (Q2): Sie machte
   die Bilanz nicht vollständiger, nur länger. Und die Gerätespalten
   `Tab_BHKW_STAMM.CO` / `Tab_Heizkessel_STAMM.CO` werden **nicht** zur Saatquelle (Q3) —
   sie hängen am Gerät, nicht am Träger.
   **Was offen bleibt, ist die QUELLE, nicht die Entscheidung.** Bringt der Anwender eine
   bei, wird in einem Zug gebaut: Migrationsschritt **68**, die drei Kernstellen
   (`EmissionsFaktorSatz.Co`, der if/else-Zweig in `EmissionsFaktorLader`,
   `Emissionsquelle.Fuer`) und der Nachweis — der Zuschnitt steht fertig in § 11.3.
   Seit Em‑9.8 führt der Referenzexport die zwei CO-Schlüssel bereits mit; ein Trägerwert
   **ändert** dann eine Zahl, statt einen Schlüssel hinzuzufügen.
   **→ Kapitel 11.3** (Quellenlage, Schritt 68, „ein Datenschritt reicht nicht", Aufwand).

---

## 10 Verweise

- [`Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md`](Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md) — CO₂-Saat (E1)
- [`Konzept_Emissionsfaktoren_Quellenwahl_EPOS-Plan.md`](Konzept_Emissionsfaktoren_Quellenwahl_EPOS-Plan.md) — Herkunft/Projektwahl, wird Rev. 2
- `Allgemein\Update\E6_QuellenSaat_Protokoll.md` — Umsetzung und Prüfstand E6 (Schritt 58)
- `Model\EmissionsModelle.cs` — Art, Wert, Reiterzeile, Speicherschritt (E3/E4)
- `Controller\EmissionenCtrl.cs` — Emissions-Reiter UI-frei: Laden, Summe F6/F3, Herkunft F8, Modus F7, Speicherplan (E3)
- `Controller\EmissionskatalogCtrl.cs` — Katalogpflege UI-frei: Arten, Werte, Übernehmen, Schutzregeln (E4)
- `Views\Kosten\Form_Emissionskatalog.cs` — der Katalog-Dialog aus § 4.2 (E4)
- `Views\Kosten\ucFuelSettings.cs` — Reiter „Preise & Umrechnung" / „Emissionen"; die Felder `numSO2/numCO2/numNOx` sind seit E3 unsichtbare Wertträger des Altschreibwegs
- `Allgemein\Wirtschaftlichkeit\Emissionsquelle.cs` — DIE eine Emissionsquelle aller
  Erzeuger samt Herkunft, Brennstoff-Rückfall und den zwei Bezugsgrößen der
  Autarkie-Kachel (§ 8/B1, W11a‑O‑2)
- `Allgemein\Wirtschaftlichkeit\EmissionsFaktorLader.cs` — DIE Lesekette je Träger und
  die CO₂e-Summe für beide Rechner (E5, § 3); seit B1 auch für die Simulation, mit `Staub`
- `Allgemein\Simulation\SimulationSPK.cs` (`Kesseldaten_Einlesen`),
  `Allgemein\Simulation\SimulationBHKW.cs` (`Moduldaten_Einlesen`) — die zwei abgelösten
  Quellen (§ 8/B1)
- `Allgemein\Katalog\ParameterVerwendung.cs` — die zehn Emissionsspalten von Kessel und
  BHKW, seit B1 beide „nur Anzeige"
- `Allgemein\Bericht\EmissionsAusweis.cs` — Beschriftung nach Modus, eine Quelle für
  Bildschirm, Word und Excel (E5, F7)
- `Allgemein\Bericht\KostenEmissionRechner.cs` — CO₂-Kette, `STROMMIX_CO2_G_JE_KWH` = 435
- `Allgemein\Wirtschaftlichkeit\EmissionsBilanzRechner.cs:20` — Einheiten-Beleg mg/kWh
- `Allgemein\Wirtschaftlichkeit\GesetzKatalog.cs` — Saat `EF_BILANZ`/`EF_NACHWEIS`
- IPCC AR6 GWP₁₀₀ (CH₄ 29,8/27,0 · N₂O 273); BAFA EEW 3.4; EBeV 2030 Anlage 2; UBA CLIMATE CHANGE 16/2026; GEMIS (IINAS); UBA TEXTE 97/2025

**Die zwei archivierten Quelldateien und die Blätter, die zählen** (`Quellen\Emissionsfaktoren\`,
Belegstellen aus § 5.2 und Kapitel 11):

- `UBA_Liste_EF_THG_Bilanzierung_v2.1_2024.xlsx` (22 Blätter) — Saatblatt
  **`01_Stationäre_Verbrennung`** (57 Zeilen; Spaltenköpfe in Z. 6: `A` ID … `H` Einheit,
  **`I` kg CO2e · `J` kg CO2 · `K` kg CH4 · `L` kg N2O**, `M` Bezugsjahr, `O` Quelle);
  Feuerung ohne Vorkette, Hu (Z. A3/A4). Nicht gesät: `07_Stat._Verbr. V. & m.V`
  (zweite Systemgrenze), `Außerhalb der Scopes` (biogenes CO₂), Spalte `I` (fremde
  GWP-Basis, `Allgemeine_Hinweise!A16`). Lizenz: `Impressum`. **Kein CO** (Kapitel 11.1.5)
- `IINAS-2025-GEMIS-5.2-Ergebnisse.xlsx` (15 Blätter) — Saatblätter
  **`Wärme-end 2020`** (je kWh Endenergie, B1/B5) und
  **`Strom-lokal DE 2000-2024`** (Niederspannung inkl. Netzverluste). Jedes Ergebnisblatt
  trägt zwei Schadstoffblöcke mit fester Spaltenbelegung: **Luftschadstoffe** (Kopf Z. 30/31 —
  `B` SO₂-Äquivalent, `C` SO₂, `D` NOx, `E` Staub) und **Treibhausgase** (Kopf Z. 53/54 —
  `B` CO₂-Äquivalent, `C` CO₂, `D` CH₄, `E` N₂O); ab Z. 77 KEA/KEV. Zuordnung ausschließlich
  über Spalte A. Nicht gesät: `Heizen (en) 2020` (je kWh **Nutz**wärme, anderer Nenner).
  **Kein CO** (Kapitel 11.1.5)
- `Referenzlaeufe\Kenndaten_Test.sqlite` — die Messgrundlage von Kapitel 11
  (Schemastand 67: sieben Arten, 305 `emissionswert`-Zeilen, davon 81 aktiv; 27 Träger;
  zwölf Referenzprojekte, alle im Modus `CO2`)
- `Referenzlauf\Ergebnisexport.cs` / `Referenzlauf\Vergleich.cs` — der Export der Skalare
  und der Toleranzvergleich; beide tragen § 9.8 (Kapitel 11.2)
- `EPOS.Kern\Allgemein\Update\BhkwLeistungsgrenzeVorgabe.cs` — das Muster eines kleinen
  Datenschritts (Schritt 67), nach dem Kapitel 11.3.2 den Schritt **68** zuschneidet

---

## 11 Umsetzungsvorschlag § 9.8 und § 9.9 (07.09.2026)

*Anwenderwunsch vom 07.09.2026: „Emissionspunkte § 9.8 und § 9.9: betrachte das Konzept und
mache einen Vorschlag zur Umsetzung im jetzigen Zustand."*

**Dies ist ein Vorschlag — nichts davon ist gebaut.** Kein C#, keine Migration, keine
Datenbankänderung. Jede Zahl dieses Kapitels ist am Stand `9d206e5` gemessen (Zweig
`ios_migration`, 07.09.2026) und nennt ihre Fundstelle: Datei mit Zeile, Blatt mit Zelle,
Tabelle mit Abfrage. Wo eine Zahl abgeleitet ist, steht die Rechnung dabei.

### 11.1 Was gemessen wurde

#### 11.1.1 Die zehn Emissionsgrößen der Simulation — und ihre Leser

Kessel und BHKW führen seit B1 je fünf Jahressummen. Sie entstehen als
`Verbrauch [MWh] × Faktor / 1000`; der Teiler ist beide Male derselbe, weil der Katalog CO₂
in g/kWh und die übrigen Arten in mg/kWh führt (F4).

| Feld | Klasse (Zeile) | Typ | Einheit | Bildung |
|---|---|---|---|---|
| `Em_CO2_SPK` | `SimulationSPK.cs:75` | `double` | **t/a** | `SimulationSPK.cs:296`, Teiler `:306` |
| `Em_CO_SPK` | `SimulationSPK.cs:76` | `double` | kg/a | `:299` — **immer 0**, es gibt keine Art `CO` |
| `Em_SO2_SPK` | `SimulationSPK.cs:77` | `double` | kg/a | `:297` |
| `Em_NOX_SPK` | `SimulationSPK.cs:78` | `double` | kg/a | `:298` |
| `Em_Staub_SPK` | `SimulationSPK.cs:79` | `double` | kg/a | `:300` |
| `Em_CO2_BHKW` | `SimulationBHKW.cs:101` | `float` | **t/a** | `SimulationBHKW.cs:483` |
| `Em_SO2_BHKW` | `SimulationBHKW.cs:102` | `float` | kg/a | `:484` |
| `Em_NOX_BHKW` | `SimulationBHKW.cs:103` | `float` | kg/a | `:485` |
| `Em_CO_BHKW` | `SimulationBHKW.cs:104` | `float` | kg/a | `:486` — **immer 0** |
| `Em_Staub_BHKW` | `SimulationBHKW.cs:105` | `float` | kg/a | `:487` |

Die Faktoren kommen bei beiden aus `Emissionsquelle.Fuer(…)` — Kette Projekt → Katalog →
Stamm → Carrier, fünftes Glied Brennstoff, im Modus des Projekts (§ 8/B1).

**Die Leser (repo-weite Suche über alle `.cs`/`.razor`):** Außerhalb der beiden Klassen
selbst nennt **eine einzige Datei** eines dieser zehn Felder —
`EPOS.Kern.Tests/EmissionsquelleTests.cs` (Zeilen 233, 234, 252, 253, 269, 270). Keine
Ergebnistabelle, kein Bericht, keine Kachel, keine Referenz-CSV. Die Ergebnistabellen sind
gegengeprüft: Von den **17** Tabellen `Tab_Ergebnis*` der Testdatenbank führt **keine** eine
Emissionsspalte; die einzige Spalte, deren Name an CO₂ erinnert, ist
`Tab_ErgebnisWirtschaftlichkeit.CO2Abgabe` — die BEHG-Abgabe in €, eine Kostengröße.

> Damit ist § 9 Punkt 8 in beide Richtungen belegt: Die Zahlen entstehen, und sie werden
> von nichts gelesen außer der Probe, die eigens dafür geschrieben wurde. **Der Rechenweg
> hat ein Ergebnis ohne Abnehmer.**

#### 11.1.2 Was `aggregate.csv` heute führt und wie ein Skalar hinzukommt

Gemessen an der Basis `Referenzlaeufe/2026-09-06_R3_Straenge` (312 CSV, zwölf Projekte):

| Projekt | Skalare | CSV-Dateien | | Projekt | Skalare | CSV-Dateien |
|---|---:|---:|---|---|---:|---:|
| 1007 | 99 | 29 | | 1030 | 150 | 22 |
| 1008 | 101 | 21 | | 1039 | 149 | 25 |
| 1017 | 114 | 21 | | 1040 | 164 | 30 |
| 1018 | 141 | 22 | | 1041 | 150 | 27 |
| 1023 | 136 | 25 | | 1042 | 197 | 34 |
| 1024 | 157 | 26 | | 1045 | 164 | 30 |
| | | | | **Summe** | **1 722** | **312** |

Die Schlüssel tragen elf Präfixe (Beispiel 1042): `Vektor.` 33, `Heizkessel.` 22,
`Pufferspeicher[i].` 3 × 19, `Waermepumpe.` 13, `Sim.` 10, `Ergebnis.` 10,
`HeizkesselModul[0].` 9, `Energiebedarf.` 9, `Solarthermie.` 8, `Puffer.` 7,
`Photovoltaik.` 6, `Lauf.` 1. Zwei Herkünfte: `Ergebnis.`/`Energiebedarf.`/`Heizkessel.`/…
entstehen aus `SELECT * FROM Tab_Ergebnis*` (`Ergebnisexport.cs:253 ff.`), die übrigen
schreibt der Export von Hand.

**Ein Skalar kommt mit einer Zeile hinzu** — in `Referenzlauf/Ergebnisexport.cs`,
Block „Skalare" ab `:170`:

```csharp
skalare.Add(Neu("Em.Kessel.Co2T", Zahl(spk.Em_CO2_SPK)));
```

`Zahl(double)` (`Ergebnisexport.cs:361`) formatiert **`G9`, invariant** — dieselbe Fassung,
die jede andere Zahl der Datei trägt; sie liegt weit unter der Vergleichstoleranz. Das
Vorbild für einen bedingten Block ist der Erdreich-Abschnitt (`:201–233`): **Kein Eintrag,
wenn es nichts gibt** — „ein `Erdreich.Anzahl = 0` wäre in jeder `aggregate.csv`
aufgetaucht, ohne etwas auszusagen."

#### 11.1.3 Die sieben Arten und die gesäten Trägerwerte

`Referenzlaeufe/Kenndaten_Test.sqlite` (Schemastand 67), `SELECT * FROM emissionsart`:

| Kürzel | Name | Einheit | GWP₁₀₀ | Quelle des Äquivalents | Pflicht | ausgewählt | aktive Trägerwerte | Vorlagen |
|---|---|---|---:|---|:-:|:-:|---:|---:|
| `CO2` | Kohlendioxid | g/kWh | 1 | — | ja | **ja** | **27** | 84 |
| `SO2` | Schwefeldioxid | mg/kWh | 0 | — | nein | **ja** | **27** | 36 |
| `NOX` | Stickoxide | mg/kWh | 0 | — | nein | **ja** | **27** | 36 |
| `CH4_FOSSIL` | Methan (fossil) | mg/kWh | 29,8 | IPCC AR6, GWP100 | nein | nein | 0 | 8 |
| `CH4_BIOGEN` | Methan (biogen) | mg/kWh | 27,0 | IPCC AR6, GWP100 | nein | nein | 0 | 8 |
| `N2O` | Lachgas | mg/kWh | 273 | IPCC AR6, GWP100 | nein | nein | 0 | 16 |
| `STAUB` | Staub (Gesamtstaub) | mg/kWh | 0 | — | nein | nein | 0 | 36 |

**305 Zeilen** in `emissionswert`, davon **81 aktiv** (27 Träger × drei Kernarten — jeder der
27 Träger führt einen aktiven CO₂-Wert) und **224 Vorlagen**. Nach Quelle:

| Quelle | Zeilen | davon aktiv | | Quelle | Zeilen | davon aktiv |
|---|---:|---:|---|---|---:|---:|
| `STAMM_ALT` | 88 | 25 | | `EIGENER_WERT` | 36 | 36 |
| `BAFA_EEW` | 47 | 20 | | `GEG_NACHWEIS` | 25 | 0 |
| `GEMIS_52` | 45 | 0 | | `EBEV_2030` | 15 | 0 |
| `UBA_2024` | 40 | 0 | | `UBA_STROMMIX` | 9 | 0 |

Die Saat E6 (85 Zeilen, § 5.2) ist damit vollständig als Vorlage vorhanden und rührt keinen
aktiven Wert an — genau wie zugesagt. **Alle zwölf Referenzprojekte und die globale Vorgabe
stehen auf Modus `CO2`** (`Tab_Projekt.Emission_Berechnungsmodus`, 24 Sätze; `Tab_Applikation`,
1 Satz).

#### 11.1.4 Die zwölf Referenzprojekte: wer bekäme überhaupt Werte?

Gemessen aus `Tab_Energieanlagen` (`ID_Type` 10 = Kessel, 11 = BHKW), der Kette aus § 3 und
den Verbrauchszahlen der Basis R3. Der Verbrauch ist die Summe der neun Brennstoffzähler der
Stufe (`Heizkessel.Gasverbrauch` …); die Emissionsspalten sind die Rechnung
`Verbrauch × Faktor / 1000`.

| Projekt | Stufe | Träger bzw. Brennstoff | CO₂ g/kWh (Ebene) | SO₂ | NOₓ | Staub | Verbrauch MWh | **CO₂ t/a** | SO₂ kg/a | NOₓ kg/a | Staub kg/a |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1007 | — | *Kessel- und BHKW-Stufe laufen nicht* | | | | | | — | — | — | — |
| 1008 | — | *Kessel- und BHKW-Stufe laufen nicht* | | | | | | — | — | — | — |
| 1017 | Kessel | Brennstoff „Elektrische Energie" | 560 (BRENNSTOFF) | 200 | 280 | 12 | 8,89 ¹ | 4,978 | 1,778 | 2,489 | 0,107 |
| 1017 | BHKW | Brennstoff „Stadtgas" | 240 (BRENNSTOFF) | 0,3 | 110 | 0,5 | 90,10 | **21,624** | 0,027 | 9,911 | 0,045 |
| 1018 | Kessel | Brennstoff „Erdgas E" | 240 (BRENNSTOFF) | 0,3 | 110 | 0,5 | 16,75 | 4,020 | 0,005 | 1,843 | 0,008 |
| 1018 | BHKW | Erdgas E | 240 (PROJEKT) | 0,3 | 110 | 0,5 | 1,56 | 0,374 | 0,000 | 0,172 | 0,001 |
| 1023 | Kessel | Brennstoff „Erdgas E" | 240 (BRENNSTOFF) | 0,3 | 110 | 0,5 | 78,57 | 18,857 | 0,024 | 8,643 | 0,039 |
| 1024 | Kessel | Brennstoff „Elektrische Energie" | 560 (BRENNSTOFF) | 200 | 280 | 12 | 53,18 ¹ | 29,781 | 10,636 | 14,890 | 0,638 |
| 1024 | BHKW | Heizöl L var | 310 (PROJEKT) | 200 | 150 | 8 | 228,26 | **70,761** | 45,652 | 34,239 | 1,826 |
| 1030 | Kessel | Erdgas E | 240 (PROJEKT) | 0,3 | 110 | 0,5 | 5 403,10 | **1 296,744** | 1,621 | 594,341 | 2,702 |
| 1030 | BHKW | Erdgas E | 240 (PROJEKT) | 0,3 | 110 | 0,5 | 1 048,27 | **251,585** | 0,314 | 115,310 | 0,524 |
| 1039 | Kessel | Erdgas E | 240 (PROJEKT) | 0,3 | 110 | 0,5 | 224,91 | 53,978 | 0,067 | 24,740 | 0,112 |
| 1040 | Kessel | Erdgas E | 240 (PROJEKT) | 0,3 | 110 | 0,5 | 16,16 | 3,878 | 0,005 | 1,778 | 0,008 |
| 1041 | Kessel | Erdgas E | 240 (PROJEKT) | 0,3 | 110 | 0,5 | 133,29 | 31,990 | 0,040 | 14,662 | 0,067 |
| 1042 | Kessel | Erdgas E | 240 (PROJEKT) | 0,3 | 110 | 0,5 | 13,79 | 3,310 | 0,004 | 1,517 | 0,007 |
| 1045 | Kessel | Erdgas E | 240 (PROJEKT) | 0,3 | 110 | 0,5 | 16,16 | 3,878 | 0,005 | 1,778 | 0,008 |

¹ Elektrokessel: Der Brennstoffzähler ist 0, der Verbrauch läuft auf `Heizkessel.Stromverbrauch`.
Die Zeile ist deshalb eine **Näherung** — die drei Zahlen der Zeile sind die einzigen der
Tabelle, die nicht auf die dritte Stelle genau vorhergesagt sind.

**Die Tabelle ist gegen die Wirklichkeit geprüft:** Die drei fett gesetzten BHKW-Werte
stimmen auf die zweite Nachkommastelle mit den Zahlen überein, die der Umbau B1 tatsächlich
erzeugt hat und die in `Referenzlaeufe/LIESMICH.md` und im Statusblock des
Umsetzungskonzepts stehen (1030: 251,58/251,59 · 1024: 70,76 · 1017: 21,62). Die Rechnung
oben ist damit belegt und nicht nur plausibel.

**Befund:** **Zehn** der zwölf Projekte bekämen Werte ≠ 0. **1007 und 1008 bekämen gar
keinen Skalar** — bei ihnen stehen `Sim.bSimulationKessel` und `Sim.bSimulationBHKW` beide
auf `False`, die Stufen laufen nicht. Von den zwölf Projekten führen **zehn** eine
Kesselstufe und **vier** (1017, 1018, 1024, 1030) eine BHKW-Stufe.

#### 11.1.5 CO in den Quellen: die Suche und ihr Ergebnis

**GEMIS 5.2 (`Quellen/Emissionsfaktoren/IINAS-2025-GEMIS-5.2-Ergebnisse.xlsx`, 15 Blätter).**
Jedes Ergebnisblatt führt genau zwei Schadstoffblöcke, beide mit fester Spaltenbelegung —
belegt an `Wärme-end 2020`:

| Block | Kopfzeile | B | C | D | E |
|---|---|---|---|---|---|
| Luftschadstoffe | Z. 30/31 | SO₂-**Äquivalent** | **SO₂** | **NOx** | **Staub** |
| Treibhausgase | Z. 53/54 | CO₂-**Äquivalent** | **CO₂** | **CH₄** | **N₂O** |

Dieselben zwei Blöcke an denselben Zeilen in `Strom-lokal DE 2000-2024` und
`Heizen (en) 2020`. Eine Suche über **alle Textzellen aller 15 Blätter** nach `CO`,
`Kohlenmonoxid` oder `carbon monoxide` findet **keinen einzigen Treffer**. Die Datei kennt
sieben Schadstoffgrößen — Kohlenmonoxid ist keine davon.

**UBA-Liste v2.1 (`UBA_Liste_EF_THG_Bilanzierung_v2.1_2024.xlsx`, 22 Blätter).** Blatt
`01_Stationäre_Verbrennung` führt in Zeile 6 die Spaltenköpfe: `A` ID, `B` Scope,
`C…G` Level 1–5, `H` Einheit, **`I` kg CO2e, `J` kg CO2, `K` kg CH4, `L` kg N2O**, `M`
Bezugsjahr, `N` Veröffentlichungsjahr, `O` Quelle, `P` Anmerkungen. Über **alle 22 Blätter**
kommen als Gas-Spaltennamen ausschließlich `kg CO2e`, `kg CO2`, `kg CH4`, `kg N2O` (und zwei
verkehrsbezogene CO₂e-Spalten) vor. **Kein CO.** Das ist keine Lücke der Datei, sondern ihr
Gegenstand: Sie heißt „Emissionsfaktoren zur **THG**-Bilanzierung", und Kohlenmonoxid ist
kein Treibhausgas.

> **Ergebnis: Beide archivierten Quellen führen CO nicht.** Ein Migrationsschritt „CO säen"
> hätte im jetzigen Zustand **keine einzige belegte Zahl** zu säen.

**Was der Bestand an CO-Zahlen hat** (Testdatenbank, Einheit laut
`ParameterVerwendung.cs:301/381` **g/MWh** — zahlengleich zu mg/kWh):

| Tabelle | Sätze | `CO` gepflegt | `CO ≠ 0` | Spannweite | Häufigste Werte |
|---|---:|---:|---:|---|---|
| `Tab_BHKW_STAMM` | 79 | 79 | **69** | 1 … 1 000 | 214 (21×), 368 (8×), 250 (8×), 150 (8×) |
| `Tab_BHKW` (Projektgeräte) | 6 | 6 | 5 | 70 … 214 | 214, 150, 70 |
| `Tab_Heizkessel_STAMM` | 63 | 62 | **2** | 2 … 10 | 2,0 · 10,0 |
| `Tab_Heizkessel` (Projektgeräte) | 26 | 26 | 13 | 2 … 10 | 10,0 |

**Drei Beispielwerte** aus dem Katalog: `2G 250kw.el Gas` CO 214 · `A-Tron_21_G` CO 150 ·
`Vitocrossal 200 CM2` CO 10,0 — alle in g/MWh. **`Tab_Brennstoff_Stamm` hat keine
CO-Spalte** (25 Sätze; Spalten `CO2`, `SO2`, `NOx`, `Staub`, `PE_Faktor`). Die vorhandenen
CO-Zahlen hängen also durchweg am **Gerät**, nicht am **Energieträger** — und damit an
genau der Stelle, die B1 zur reinen Anzeige erklärt hat.

### 11.2 Vorschlag zu § 9.8 — die Emissionsgrößen ins Regressionsnetz

#### 11.2.1 Wohin: zwei Wege, einer davon abzulehnen

**Weg A — Skalare in `aggregate.csv`** (Vorschlag). Zehn Zeilen in `Ergebnisexport.cs`, sonst
nichts. Kein Schema, keine Migration, keine gespeicherte Zahl.

**Weg B — Spalten in `Tab_ErgebnisHeizkessel` / `Tab_ErgebnisBHKW`.** Der Export nähme sie
über `SELECT *` von selbst mit, und der Bericht könnte sie lesen. **Er ist abzulehnen, und
zwar mit dem Argument, das dieses Konzept bereits geführt hat** (Umsetzungsklärung zu F7):
Eine gespeicherte Emissionszahl beschriebe einen Zustand, der bei jedem Bericht neu gerechnet
wird — sie liefe auseinander, sobald jemand zwischen Lauf und Druck den Modus oder einen
Katalogwert ändert. Genau deshalb trägt `Tab_Ergebnis*` heute keine Emissionsspalte. Weg B
kostete zusätzlich einen Migrationsschritt und zwei Ergebnisschreibwege — für eine Zahl, die
das Konzept bewusst nicht persistiert.

#### 11.2.2 Die zehn Skalare

Je Erzeugerklasse fünf Arten, **kein** Gesamt: Eine Summe `Em.Gesamt.*` wäre die Addition
zweier Zahlen, die beide schon in derselben Datei stehen — der Vergleich prüft ohnehin jeden
Wert einzeln, und eine abgeleitete Größe brächte keine zusätzliche Aussage, aber fünf weitere
Schlüssel.

| Skalar | Quelle | Einheit | in der Testdatenbank |
|---|---|---|---|
| `Em.Kessel.Co2T` | `SimulationSPK.Em_CO2_SPK` | t/a | 10 Projekte, alle ≠ 0 |
| `Em.Kessel.So2Kg` | `Em_SO2_SPK` | kg/a | 10 Projekte, alle ≠ 0 |
| `Em.Kessel.NoxKg` | `Em_NOX_SPK` | kg/a | 10 Projekte, alle ≠ 0 |
| `Em.Kessel.StaubKg` | `Em_Staub_SPK` | kg/a | 10 Projekte, alle ≠ 0 |
| `Em.Kessel.CoKg` | `Em_CO_SPK` | kg/a | 10 Projekte, **alle 0** (keine Art `CO`) |
| `Em.Bhkw.Co2T` | `SimulationBHKW.Em_CO2_BHKW` | t/a | 4 Projekte, alle ≠ 0 |
| `Em.Bhkw.So2Kg` | `Em_SO2_BHKW` | kg/a | 4 Projekte, alle ≠ 0 |
| `Em.Bhkw.NoxKg` | `Em_NOX_BHKW` | kg/a | 4 Projekte, alle ≠ 0 |
| `Em.Bhkw.StaubKg` | `Em_Staub_BHKW` | kg/a | 4 Projekte, alle ≠ 0 |
| `Em.Bhkw.CoKg` | `Em_CO_BHKW` | kg/a | 4 Projekte, **alle 0** |

**Die Einheit steht im Namen** — Hausregel des Rechenkerns, Punkt 3
(`EPOS.Kern/CLAUDE.md`, „Einheiten: die Regel des Rechenkerns", Anwenderentscheid W8‑O‑5c
Q1). Sie steht dort, weil die zehn Felder **nicht** dieselbe Einheit führen: CO₂ in t/a, die
vier übrigen in kg/a. Ein Schlüssel `Em.Kessel.Co2` ohne Einheit wäre genau der Fehler, den
diese Regel verhindern soll — und der Export darf die Zahl nicht umrechnen: Umgerechnet wird
im Haus an genau **zwei** Nähten (`SimulationErgebnisCtrl` und `SimulationRunner`, Regel
Punkt 4); ein Teiler in `Ergebnisexport.cs` wäre eine dritte.

**Eigenes Präfix `Em.` statt `Heizkessel.`/`BHKW.`:** Diese beiden Präfixe stehen für
„Spalte einer `Tab_Ergebnis*`-Zeile" (`SELECT *`). Ein handgeschriebener Skalar darunter
verwischte die Herkunft; `Em.` sagt „Emissionsgröße der Simulation, nicht persistiert".

**Bedingung wie bei den Vektoren:** Der Block läuft nur, wenn die Stufe gelaufen ist
(`sim.bSimulationKessel && sim.simulation_spk != null` bzw. das BHKW-Gegenstück) — dieselbe
Bedingung, unter der schon `kessel_waermebedarf.csv` und `bhkw_strom.csv` entstehen, und
dasselbe Muster wie beim Erdreich-Block. **1007 und 1008 bekommen dadurch keinen einzigen
neuen Schlüssel**, statt zehn Nullen zu tragen.

#### 11.2.3 Wie viel wächst die Basis, und was wird bewacht

| | heute | mit § 9.8 |
|---|---:|---:|
| Skalare über zwölf Projekte | 1 722 | **1 792** (+70, +4,1 %) |
| Skalare je Projekt | 99 … 197 | 99 … 207 |
| CSV-Dateien | 312 | 312 (unverändert) |
| Projekte mit neuen Schlüsseln | — | 10 von 12 (je 5 oder 10) |

Von den 70 neuen Schlüsseln:

* **32** tragen einen Betrag ≥ 1 und fallen damit unter die **relative** Toleranz (1e‑4) —
  sie sind wirksame Wächter: Der Sprung des BHKW-CO₂ von 0 auf 251,585 t/a in Projekt 1030
  wäre ein Vielfaches der Toleranz gewesen.
* **24** liegen unter 1 (SO₂- und Staubwerte der Gasprojekte, z. B. `Em.Kessel.So2Kg` = 0,005
  bei 1040) und fallen unter die **absolute** Toleranz von 0,01 — dort bewacht der Vergleich
  nur grobe Änderungen. Das ist kein Fehler des Vorschlags, sondern die Größenordnung der
  Sache: 0,3 mg/kWh SO₂ auf 16 MWh sind nun einmal 5 Gramm im Jahr.
* **14** sind die CO-Schlüssel und stehen strukturell auf 0 (siehe § 11.3).

**In der CI** (`kern.yml`: 1030, 1007, 1017, 1045) kämen **25** Schlüssel dazu — 1030 und
1017 je zehn, 1045 fünf, 1007 keinen.

#### 11.2.4 Wirkung auf den Toleranzvergleich — der Punkt, an dem es weh tut

`Referenzlauf/Vergleich.cs:248–255` behandelt einen Schlüssel, den nur der neue Lauf trägt,
als **Abweichung mit `Schwere = double.MaxValue`**: „Eintrag nur im Vergleichslauf". Das ist
ein FAIL, kein Hinweis. Gegen die eingefrorene Basis R3 fielen mit § 9.8 also **zehn der
zwölf Projekte durch** — nicht weil sich eine Zahl geändert hätte, sondern weil zehn Dateien
mehr Zeilen haben als vorher.

Es gibt genau zwei saubere Umgänge damit, und `Vergleich.cs` kennt beide:

1. **Neue Basis einfrieren.** Der Regelweg. Danach vergleicht jeder Lauf gegen einen Stand,
   der die Schlüssel kennt.
2. **`--ohne <Schlüsselliste>`** (`Vergleich.cs:44–61`, Etappe D4). Der Ausschluss ist
   ausdrücklich als „Werkzeug für einen ERKLÄRTEN Unterschied" gedacht, nicht als Weg,
   Abweichungen wegzuschalten — er wirkt nur auf benannte Schlüssel und nennt sie in der
   Ausgabe. Für den Übergangslauf „sind die ALTEN Werte unverändert?" ist er das richtige
   Mittel; als Dauerzustand wäre er die Verlängerung der Lücke, die § 9.8 schließen soll.

`Plausibilitaet.Pruefen` (`Referenzlauf/Plausibilitaet.cs`) ist unberührt: Es prüft nur, ob
zu einem vorhandenen Schlüssel die geforderte Vektordatei existiert; neue Skalare fordern
keine.

#### 11.2.5 Wann: drei Varianten

| | **Z1 — jetzt, im Zuge von R4** | **Z2 — eigener Schritt nach R4, Basis R5** | **Z3 — nicht exportieren** |
|---|---|---|---|
| Was geschieht | Der laufende Auftrag W8‑O‑5d (Kern auf `double`) bekommt die zehn Zeilen als Ergänzung, **bevor** er `2026-09-07_R4_Double` einfriert | Nach R4 ein eigener Lauf, zwölf Projekte neu, Basis `R5` einfrieren | Nichts. § 9.8 bleibt offen, der Nachweis bleibt bei `EmissionsquelleTests` |
| Zu ändern | `Referenzlauf/Ergebnisexport.cs` (+10 Zeilen), ein Absatz in `Referenzlaeufe/LIESMICH.md` | dieselben zehn Zeilen **plus** neue Basis, LIESMICH-Abschnitt, `CLAUDE.md` (Wurzel) und `EPOS.Kern/CLAUDE.md`, Prüfung von `kern.yml`/`ios.yml` | — |
| Aufwand | **1–2 Agentenstunden** obendrauf (der R4-Lauf steht ohnehin an) | **3–4 Agentenstunden** | 0 |
| Zahl der Basiswechsel | **einer** (R4) | **zwei** (R4, dann R5) | keiner |
| Risiko | Erweiterung des Auftrags eines laufenden Agenten | Die 312 CSV der R4 sind nach wenigen Stunden schon wieder veraltet | Jede Faktoränderung bleibt unbemerkt; § 9.8 wandert ins nächste Paket |
| Präzision der Zahlen | **richtig auf Anhieb**: `Em_*_BHKW` ist heute `float` (`SimulationBHKW.cs:101–105`); W8‑O‑5d hebt es auf `double`. Wer **nach** der Umstellung exportiert, friert die endgültigen Stellen ein | R5 trägt dieselben Zahlen wie Z1 — R4 hat sie nur nicht | — |

**Ein Einwand, der bei Z1 nicht greift:** „Zwei Ursachen in einer Basis lassen sich nicht
auseinanderhalten." Das gilt für Schlüssel, die auf beiden Seiten stehen — die zehn neuen
stehen in R3 nicht. Es gibt nichts zu verwechseln: Die Umstellung auf `double` zeigt sich an
den 1 722 alten Schlüsseln, die zehn neuen haben keine Vorgeschichte.

> **Empfehlung: Z1.** Sie kostet einen Basiswechsel statt zwei, sie trifft die einzige
> Gelegenheit, an der die Zahlen sofort in ihrer endgültigen Genauigkeit entstehen, und der
> Eingriff ist auf zehn Zeilen einer Datei begrenzt, die derselbe Agent ohnehin in der Hand
> hat (`Referenzlauf/*`). **Nicht Z3:** § 9 Punkt 8 hat den Befund selbst als Lücke des
> Netzes benannt — „das Regressionsnetz kann eine künftige Änderung an den Emissionsfaktoren
> **nicht** bemerken". Eine benannte Lücke, die man offen lässt, wird eines Tages zu einer
> stillen Zahlenänderung, die niemandem auffällt. Der Nachweis in `EmissionsquelleTests`
> prüft die **Kette**; er prüft nicht, ob ein Rechenlauf über 8 760 Stunden am Ende dieselbe
> Jahressumme trägt.

#### 11.2.6 Der Preis von § 9.8: eine neue Regel

Mit § 9.8 wird `Kenndaten_Test.sqlite` an einer Stelle regressionsrelevant, an der sie es
heute nicht ist — die **Emissionsfaktoren**. Daraus folgt eine Regel, die vorher keine war:

> **Wer einen gesäten Faktor der Testdatenbank ändert, friert im selben Schritt die Basis
> neu ein und begründet den Wechsel in `Referenzlaeufe/LIESMICH.md`.** Betroffen ist jede
> Änderung an `emissionsart` (Auswahl, Äquivalenzfaktor), an einem **aktiven**
> `emissionswert` (81 Zeilen), an `Tab_Brennstoff_Stamm.CO2/SO2/NOx/Staub` (25 Sätze), an
> `energy_project_settings.co2/so2/nox` der zwölf Projekte und am Berechnungsmodus eines
> Projekts. **Nicht** betroffen ist die Pflege von **Vorlagen** (`ist_aktiv = falsch`,
> 224 Zeilen) — sie erreichen die Kette nicht. Der Migrationsschritt 58 (E6) hat genau
> deshalb 85 Vorlagen gesät und keinen aktiven Wert angefasst.

Das ist der Einwand aus § 9 Punkt 8 — „sie zöge auch jede Katalogpflege in den
Regressionsvergleich" — und er ist berechtigt. Er trifft aber **nicht** die Katalogpflege
des Anwenders (dessen Produktiv-DB ist nicht die Testdatenbank), sondern nur die Pflege der
**Testdatenbank**; und die geschieht ohnehin nur über Migrationsschritte oder das Werkzeug
`Werkzeuge/Testdatenbankschema`, also an einer Stelle, an der ein Basiswechsel ohnehin
mitgedacht wird (Vorbild: der Nachtrag zu Schritt 67 in `LIESMICH.md`).

### 11.3 Vorschlag zu § 9.9 — die Emissionsart „CO"

#### 11.3.1 Die Quellenlage ist der ganze Punkt

§ 5 dieses Konzepts trägt eine Hausregel, übernommen aus dem Quellenwahl-Konzept: belegte
Quelle, „**erst nach Vorlage der Fundstelle, keine Zahlen aus dem Gedächtnis**". § 11.1.5
hat gemessen, dass die beiden archivierten Quellen **kein CO führen**. Damit ist die Lage
eindeutig:

| Möglicher Ursprung | Trägt CO? | Taugt als Saat? |
|---|---|---|
| UBA-Liste v2.1, Blatt 01 | **nein** — nur `kg CO2e/CO2/CH4/N2O` | nein |
| GEMIS 5.2, alle 15 Blätter | **nein** — SO₂-Äq., SO₂, NOx, Staub, CO₂-Äq., CO₂, CH₄, N₂O | nein |
| `Tab_BHKW_STAMM.CO` (69 Sätze ≠ 0) | ja, g/MWh | **nein** — Geräte-, kein Trägerwert. Ein Katalogwert daraus hinge am Modul, nicht am Energieträger, und kehrte damit genau die Zuordnung um, die B1 hergestellt hat |
| 1. BImSchV / TA Luft | ja, als **Grenzwert** | **nein** — ein Grenzwert ist die Obergrenze einer Genehmigung, kein Emissionsfaktor. Er sagt, was eine Anlage höchstens ausstoßen darf, nicht, was sie ausstößt. Eine Bilanz aus Grenzwerten wäre systematisch zu hoch und trüge doch das Etikett „belegt" |
| UBA TEXTE 97/2025 „Ermittlung von Emissionsfaktoren" | **wahrscheinlich** — die Reihe deckt Luftschadstoffe kleiner und mittlerer Feuerungsanlagen ab und war in § 5 als Quelle vorgesehen, bevor der Nutzer am 29.08.2026 UBA v2.1 und GEMIS lieferte | **zu prüfen** — die Datei liegt **nicht** unter `Quellen/Emissionsfaktoren/`. Ohne sie im Haus wird hier keine Zahl genannt |

> **Damit steht der Vorschlag aus § 9 Punkt 9 auf eigenen Füßen:** „Der Katalog trägt heute
> keinen belegten CO-Wert, und eine Art ohne Werte machte die Bilanz nicht vollständiger,
> nur länger." Das ist jetzt **gemessen** statt vermutet.

#### 11.3.2 Wie der Schritt 68 aussähe — wenn die Quelle da ist

Nummer **68** (`SchemaStand.Zielversion` steht auf **67**, vergeben durch
`SCHRITT_67_BHKW_LEISTUNGSGRENZE` vom 07.09.2026). Reihenfolge zwingend, seit dem Vorfall
vom 29.08. 09:25: **erst** Schrittkonstante, Methode und `SCHRITTE`-Eintrag, **dann**
`Zielversion` auf 68.

**Die eine Zeile für `emissionsart`:**

| Feld | Wert | Begründung |
|---|---|---|
| `kuerzel` | `CO` | § 3 nennt es bereits in der Liste der möglichen Kürzel |
| `name` | „Kohlenmonoxid" | Muster der sechs übrigen (`Schwefeldioxid`, `Lachgas (Distickstoffmonoxid)`) |
| `einheit` | `mg/kWh` | F4 — alles außer CO₂ |
| `co2_aequivalent` | **0** | F2: kein Treibhausgas |
| `aequivalent_quelle` | leer | F2: „leer bei 0" |
| `ist_pflicht` | falsch | nur CO₂ |
| `ausgewaehlt` | **falsch** | siehe 11.3.3 — eine ausgewählte Art ohne Werte verlängerte jeden Emissions-Reiter um eine leere Zeile |
| `ist_auslieferung` | wahr | wie die sieben übrigen: abwählbar, nicht löschbar |
| `sortierung` | **35** | zwischen `NOX` (30) und `CH4_FOSSIL` (40) — bei den Luftschadstoffen, nicht bei den Treibhausgasen |

**Idempotenz** nach dem Muster 57/58: Schlüssel ist `kuerzel`; ein Zweitlauf findet die Zeile
und legt nichts an. **Trägerwerte:** im jetzigen Zustand **keine** — es gibt keine belegte
Zahl (11.3.1). Der Schritt legte also **eine** Zeile an, sonst nichts.

**Drei Leser, eine Quelle** — Muster `BhkwLeistungsgrenzeVorgabe.cs` (Schritt 67): Die
SQL-Texte gehören nach `EPOS.Kern/Allgemein/Update/`, weil sie
`WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs` (Access-Zweig, deshalb nicht
im Kern), `Werkzeuge/Testdatenbankschema` und `EPOS.Kern.Tests` gemeinsam brauchen.
Sicherung, `laccdb`-Prüfung und Protokoll laufen wie bei jedem Schritt mit.

#### 11.3.3 Ein Datenschritt reicht **nicht** — der gemessene Befund

§ 9 Punkt 9 sagt: „das wäre ein Migrationsschritt nach dem Muster 57/58". **Das ist zu
knapp.** Gemessen an `EmissionsFaktorLader.cs`:

1. **`EmissionsFaktorSatz` hat kein Feld `Co`** (`:16–61`: `Co2GKwh`, `Co2eGKwh`, `So2`,
   `Nox`, `Staub`).
2. **Die Zuordnung Art → Feld ist eine feste if/else-Kette über vier Kürzel**
   (`:194–208`). Eine Art `CO` liefe durch die Schleife, landete in der Zeilenliste — und in
   **keinem** Feld.
3. **`Emissionsquelle.Fuer` füllt `CoMgKwh` deshalb nirgends**; das Feld ist im Quelltext
   ausdrücklich als „heute immer 0" dokumentiert (`Emissionsquelle.cs:39–45`).

**Ohne diese drei Stellen bliebe `Em_CO_SPK` und `Em_CO_BHKW` auch nach dem Schritt 68 auf
0** — der Migrationsschritt allein änderte gar nichts. Es sind drei kleine, aber
unvermeidliche Eingriffe im Kern.

**Und ein vierter Punkt, den man leicht übersieht:** Würde die Art `ausgewaehlt = wahr`
gesetzt, träte sie in die Zeilenliste der Summe F6 ein. Ein Träger **ohne** gepflegtes CO₂,
aber **mit** CO-Wert bekäme dann `irgendeinWert = true` und damit eine CO₂e-Summe von **0**
statt `null` — `Wirksam("CO2E")` gäbe 0 zurück, `Co2Gepflegt` würde wahr, und die
Emissionsbilanz hielte ihre CO₂-Spalte für vollständig, obwohl der Wert fehlt. Genau diese
Falle hat der Staub-Rückfall bei B1 umgangen, indem er **außerhalb** der Zeilenliste läuft
(`EmissionsFaktorLader.cs:214–221`). In der Testdatenbank bisse sie nicht — **alle 27 Träger
führen einen aktiven CO₂-Wert** —, aber sie ist da. Deshalb oben `ausgewaehlt = falsch`.

#### 11.3.4 Was sich sonst änderte

| Stelle | Wirkung | gemessen |
|---|---|---|
| Emissions-Reiter (E3) | **automatisch**, sobald `ausgewaehlt` gesetzt wird: `EmissionenCtrl` baut die Zeilen aus `EmissionskatalogCtrl.Arten(true)` (`EmissionenCtrl.cs:147`). Im **Projektkontext** wäre die Zeile **nur lesend** (`:165`, `NurLesend = Projektkontext && !IstKernart`) — CO hat keine Altspalte | ja |
| Katalog-Dialog (E4) | zeigt die Art ohne Zutun; Werte pflegbar als `EIGENER_WERT` | ja |
| Emissionsbilanz der Wirtschaftlichkeit | **keine** — `EmissionsBilanzRechner.cs:206/207` führt SO₂ und NOₓ, sonst nichts. Für eine CO-Zeile im Bericht wäre eine eigene Erweiterung nötig | ja |
| Autarkie-Kachel | **keine** — sie rechnet nur mit CO₂ | ja |
| `EmissionsquelleTests` | heute **10 Fälle**, keiner nennt CO. Zwei kämen dazu: „Art vorhanden, kein Wert → Faktor bleibt 0" und „Wert gepflegt → er erreicht Kessel und BHKW" | ja |
| Referenzlauf | **byte-gleich**, solange § 9.8 nicht umgesetzt ist. **Mit** § 9.8: Die 14 CO-Schlüssel blieben 0, solange kein Trägerwert gesät ist — der Schritt 68 allein änderte auch dann keine Zahl | ja |
| `.wpx`-Projekttransfer | Pakete auf Stand 67 würden abgewiesen — die eingebaute Zusage des Formats, wie bei jedem Schritt | ja |

**Die fünf Gerätespalten bleiben, wie B1 sie hinterlassen hat:** informative Herstellerangabe,
`Verwendung.Dialog`, Herleitungszeile im Katalogeditor. Sie werden **nicht** zur Saatquelle
(11.3.1) und **nicht** gelöscht.

#### 11.3.5 Aufwand und die ehrliche Gegenfrage

| Teil | Agentenstunden |
|---|---:|
| Migrationsschritt 68 (Konstante, Methode, `SCHRITTE`, `Zielversion`, SQL-Texte im Kern) | 1,0 |
| Kern: `EmissionsFaktorSatz.Co`, if/else-Zweig, `Emissionsquelle.Fuer` | 1,0 |
| `Werkzeuge/Testdatenbankschema` nachziehen, Testdatenbank auf Stand 68 | 0,5 |
| `EmissionsquelleTests` um zwei Fälle, Prüflauf | 1,0 |
| Doku: § 3-Tabelle, § 6-Etappenzeile, `Heizkessel.wiki`/`BHKW.wiki` („Kein CO in der Bilanz" fällt), `EPOS.Kern/CLAUDE.md` | 1,0 |
| **Summe** | **4,5** |

Nicht enthalten: die **Beschaffung der Quelle** und die Extraktion der Trägerwerte — das ist
der Teil, den kein Agent leisten kann. Erst mit ihr kämen Saatzeilen und ein Nachweis nach
dem Muster § 5.2 dazu (weitere ~2 Stunden je nach Trägerzahl).

**Die Gegenfrage.** Lohnt eine Art, deren Werte im Bericht nur „informativ" stehen? Der
Entscheid B1 hat sie selbst gestellt: „Da CO₂, SO₂, NOₓ, CO und Staub in g/MWh kein
CO₂-Äquivalent haben, sind diese Zahlen informativ." SO₂ und NOₓ stehen trotzdem im Katalog
und in der Bilanz — weil sie die klassischen Feuerungsgrößen der Immissionsschutz-Sicht sind
und weil belegte Zahlen vorlagen. **Bei CO fehlt der zweite Grund.** Eine Art anlegen, für
die kein Wert existiert, hieße: Der Reiter bekäme eine Zeile mehr, der Katalog einen Eintrag
mehr, die Bilanz eine Null mehr — und die einzige CO-Zahl, die das Haus je hatte, bliebe die
Gerätespalte, die B1 gerade zur Anzeige erklärt hat.

> **Empfehlung: warten, nicht bauen.** § 9 Punkt 9 bleibt als Vorschlag stehen; der Schritt
> 68 wird gebaut, **sobald der Anwender eine Quelle mit CO-Emissionsfaktoren je
> Energieträger beibringt** — dann in einem Zug: Art, Trägerwerte, drei Kernstellen und
> Nachweis. Bis dahin ist das Feld `CoMgKwh` vorbereitet, die Rechenwege führen es mit, und
> es kostet nichts, dass es 0 ist. **Der Weg über die Gerätespalte oder über
> 1.-BImSchV-Grenzwerte wird ausdrücklich nicht empfohlen** — beide brächten eine Zahl mit
> falscher Bedeutung in einen Katalog, dessen ganzer Wert seine Belegbarkeit ist.

### 11.4 Reihenfolge

1. **Antwort auf `Em‑9.8‑Q1`** (Zeitpunkt). Bei Z1 sofort, weil das Zeitfenster mit dem
   Einfrieren von R4 zugeht.
2. § 9.8 bauen und die Basis einfrieren; `LIESMICH.md` bekommt den Absatz mit den zehn
   Schlüsseln und der Regel aus 11.2.6.
3. § 9.9 **ruht**, bis eine Quelle vorliegt (`Em‑9.9‑Q1`). Die Reihenfolge ist nicht
   umkehrbar: Käme CO zuerst, änderte es die Schlüsselmenge des Exports ein zweites Mal.

### 11.5 Fragen an den Anwender — **alle sieben beantwortet am 07.09.2026**

> „Sieben Fragen: Empfehlung. 9.8 und 9.9: Empfehlung."

**Damit gilt zu jeder Frage die Empfehlung dieser Tabelle.** Die Spalte „Antwort" hält
fest, was daraus geworden ist; die Entscheide selbst stehen mit Umsetzungsstand in § 8.
Eine Abweichung gibt es, und zwar keine inhaltliche: **Em‑9.8‑Q1** empfahl Z1 „im Zuge von
R4". R4 war beim Entscheid bereits eingefroren und gepusht — Z1 wurde deshalb auf die
Basis **`2026-09-07_R5_Zahlenrand`** gelegt, die ohnehin anstand (W8‑O‑5d‑Q1/Q2). Es bleibt
bei **einem** zusätzlichen Basiswechsel, und die Zahlen entstehen wie empfohlen sofort in
`double`-Genauigkeit.

| Kennung | Frage | Empfehlung | Antwort (07.09.2026) |
|---|---|---|---|
| **Em‑9.8‑Q1** | **Wann** kommen die Emissionsgrößen in den Referenzexport — **Z1** (jetzt, als Ergänzung des laufenden `double`-Auftrags, eine Basis R4), **Z2** (eigener Schritt danach, zwei Basiswechsel) oder **Z3** (gar nicht, Nachweis bleibt bei `EmissionsquelleTests`)? | **Z1.** Ein Basiswechsel statt zwei, die Zahlen entstehen sofort in `double`-Genauigkeit, Eingriff = zehn Zeilen in einer Datei, die derselbe Agent ohnehin hält | **Z1**, ausgeführt auf der Basis **R5** statt R4 (R4 war bereits gepusht) — ein Basiswechsel, wie empfohlen |
| **Em‑9.8‑Q2** | **Wie viele** Skalare: die zehn aus 11.2.2 — oder zusätzlich fünf `Em.Gesamt.*`, oder ohne die zwei CO-Schlüssel (dann acht)? | **Zehn.** Kein `Gesamt` (ableitbar, keine zusätzliche Aussage); **mit** CO, damit § 9.9 später eine Zahl **ändert** statt einen Schlüssel **hinzuzufügen** — eine Wertänderung meldet der Vergleich mit Zahlen, ein neuer Schlüssel nur als „nur im Vergleichslauf" | **Zehn**, mit CO, ohne `Gesamt` — gebaut |
| **Em‑9.8‑Q3** | **Einheit im Namen** (`Em.Kessel.Co2T` in t/a, `…So2Kg` in kg/a — kein Rechnen im Export) oder **alles in kg/a** mit einheitlichem Namen (`…Co2Kg`, CO₂ × 1000)? | **Einheit im Namen.** Ein Faktor 1 000 in `Ergebnisexport.cs` wäre eine dritte Umrechnungsnaht neben den zwei erlaubten (Hausregel Rechenkern, Punkt 4) | **Einheit im Namen** — gebaut, kein Teiler im Export |
| **Em‑9.8‑Q4** | Wird die **Regel aus 11.2.6** angenommen (wer einen aktiven Faktor der Testdatenbank ändert, friert die Basis im selben Schritt neu ein)? | **Ja.** Ohne sie fällt die CI beim nächsten Katalogschritt rot aus, ohne dass jemand mit dem Zusammenhang rechnet. Vorlagen (`ist_aktiv = falsch`) bleiben ausdrücklich frei | **Ja** — die Regel steht in `Referenzlaeufe/LIESMICH.md` und in der Wurzel-`CLAUDE.md` |
| **Em‑9.9‑Q1** | Gibt es eine **Quelle mit CO-Emissionsfaktoren je Energieträger** (Feuerung ohne Vorkette, mg/kWh)? UBA TEXTE 97/2025 wäre der Kandidat, liegt aber nicht im Haus | **Erst mit Quelle bauen.** Ohne sie hätte Schritt 68 keine einzige Zahl zu säen (gemessen: weder UBA v2.1 noch GEMIS 5.2 führt CO) | **Erst mit Quelle** — Schritt 68 ruht; § 9 Punkt 9 nennt den fertigen Zuschnitt |
| **Em‑9.9‑Q2** | Falls **keine** Quelle beizubringen ist: Art `CO` trotzdem **leer** anlegen (Reiter und Katalog zeigen sie, Werte trägt ein, wer sie hat) — oder § 9 Punkt 9 geschlossen als „bewusst nicht" führen? | **Geschlossen als „bewusst nicht".** Eine Art ohne Werte macht die Bilanz nicht vollständiger, nur länger — und `CoMgKwh` ist im Rechenweg bereits vorbereitet, falls sich die Lage ändert | **Geschlossen als „bewusst nicht"** — § 9 Punkt 9 |
| **Em‑9.9‑Q3** | Sollen die **Gerätespalten** `Tab_BHKW_STAMM.CO` (69 Sätze ≠ 0) und `Tab_Heizkessel_STAMM.CO` als Notbehelf in den Katalog übernommen werden? | **Nein.** Sie hängen am Gerät, nicht am Energieträger — das kehrte die Zuordnung um, die B1 gerade hergestellt hat. Sie bleiben Herstellerangabe und „nur Anzeige" | **Nein** — die Gerätespalten bleiben Herstellerangabe und „nur Anzeige" |

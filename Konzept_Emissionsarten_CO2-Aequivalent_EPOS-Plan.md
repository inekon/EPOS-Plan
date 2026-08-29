# Konzept — Emissionsarten-Katalog und CO₂-Äquivalent (EPOS-Plan)

**Stand:** 29.08.2026 · **Rev. 1.5 — E1 bis E5 umgesetzt** (Rev. 1.2 vom 28.08.2026:
alle Entscheidungsfragen beantwortet — F3 präzisiert, F4 bestätigt, Luftschadstoffe ohne
Vorkette, Modus global + Projekt-Override, Artenauswahl global, E1 vor E2)

> **Umsetzungsvermerk (29.08.2026).** **E1** läuft als Migrationsschritt 56
> (20 Träger gesetzt), **E2** als Migrationsschritt 57 (`ZIEL_VERSION` 57): sieben
> Emissionsarten, 139 Vorlagen, 81 aktive Trägerwerte, Berechnungsmodus `CO2` in
> `Tab_Applikation` und in allen 26 Bestandsprojekten. Zweitlauf beider Schritte:
> 0 Änderungen. Die **Mapping-Liste** (§ 8 Punkt 5) steht in **§ 5.1** zur Durchsicht.
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
> Offen bleiben E6 und die Sichtabnahme.

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
Dies ist die Liste, um deren Durchsicht § 8 Punkt 5 bittet.*

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

---

## 6 Umsetzung in Etappen

| Etappe | Inhalt | Ergebnis |
|---|---|---|
| **E1** | CO₂-Saat nach `Konzept_CO2-Faktoren` Rev. 1 (eigener Migrationsschritt, Regeln von dort: Sicherung, laccdb-Sperre, ACE-Falle, Idempotenz) | **UMGESETZT (Schritt 56, 29.08.2026)** — Trägerwerte belegt statt 0 |
| **E2** | Tabellen `emissionsart` + `emissionswert` anlegen und säen (Arten-Auslieferung; Vorlagen aus `EF_BILANZ`/`EF_NACHWEIS` mit Mapping-Liste; Trägerwerte aus Bestandsspalten) | **UMGESETZT (Schritt 57, 29.08.2026)** — Modell steht, **kein Ergebnis ändert sich** (F9) |
| **E3** | Emissions-Tab im Energieträger-Dialog (dynamische Felder, TextBox statt Spinner, Einheiten richtig, CO₂e-Summe, Herkunft, Warnung F3), Schreibweg beidseitig | **UMGESETZT (29.08.2026)** — Anwender pflegt im neuen Modell; Kontext-Regel als Umsetzungsklärung in § 4.1 |
| **E4** | Katalog-Dialog (4.2): Artenverwaltung, Werteverwaltung, Übernehmen | **UMGESETZT (29.08.2026)** — `Form_Emissionskatalog` samt Schutzregeln; Katalogpflege vollständig |
| **E5** | Modus-Schalter (F7): globale Vorgabe + Projektfeld, an beiden Orten; `KostenEmissionRechner` + `EmissionsBilanzRechner` modusfähig; Berichte weisen Modus aus; Modus in Variantenergebnisse; `STROMMIX_CO2_G_JE_KWH` 380→435 *(Nutzerentscheid 29.08.2026)* | **UMGESETZT (29.08.2026)** — eine Lesekette für beide Rechner (`EmissionsFaktorLader`), Modus wirksam und am Ergebnis vermerkt, Ausweis über `EmissionsAusweis`; Modus CO2 zahlengleich außer dem Strommix-Randfall |
| **E6** | Luftschadstoff-Quelle (GEMIS/UBA TEXTE) nach Vorlage der Fundstellen einsäen | Werte zitierfähig |

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

## 8 Offene Punkte

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

---

## 9 Verweise

- [`Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md`](Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md) — CO₂-Saat (E1)
- [`Konzept_Emissionsfaktoren_Quellenwahl_EPOS-Plan.md`](Konzept_Emissionsfaktoren_Quellenwahl_EPOS-Plan.md) — Herkunft/Projektwahl, wird Rev. 2
- `Model\EmissionsModelle.cs` — Art, Wert, Reiterzeile, Speicherschritt (E3/E4)
- `Controller\EmissionenCtrl.cs` — Emissions-Reiter UI-frei: Laden, Summe F6/F3, Herkunft F8, Modus F7, Speicherplan (E3)
- `Controller\EmissionskatalogCtrl.cs` — Katalogpflege UI-frei: Arten, Werte, Übernehmen, Schutzregeln (E4)
- `Views\Kosten\Form_Emissionskatalog.cs` — der Katalog-Dialog aus § 4.2 (E4)
- `Views\Kosten\ucFuelSettings.cs` — Reiter „Preise & Umrechnung" / „Emissionen"; die Felder `numSO2/numCO2/numNOx` sind seit E3 unsichtbare Wertträger des Altschreibwegs
- `Allgemein\Wirtschaftlichkeit\EmissionsFaktorLader.cs` — DIE Lesekette je Träger und
  die CO₂e-Summe für beide Rechner (E5, § 3)
- `Allgemein\Bericht\EmissionsAusweis.cs` — Beschriftung nach Modus, eine Quelle für
  Bildschirm, Word und Excel (E5, F7)
- `Allgemein\Bericht\KostenEmissionRechner.cs` — CO₂-Kette, `STROMMIX_CO2_G_JE_KWH` = 435
- `Allgemein\Wirtschaftlichkeit\EmissionsBilanzRechner.cs:20` — Einheiten-Beleg mg/kWh
- `Allgemein\Wirtschaftlichkeit\GesetzKatalog.cs` — Saat `EF_BILANZ`/`EF_NACHWEIS`
- IPCC AR6 GWP₁₀₀ (CH₄ 29,8/27,0 · N₂O 273); BAFA EEW 3.4; EBeV 2030 Anlage 2; UBA CLIMATE CHANGE 16/2026; GEMIS (IINAS); UBA TEXTE 97/2025

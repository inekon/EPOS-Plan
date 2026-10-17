# SK-2c — Energieflussbild nach Variante A „Gestraffte Kette“

Welle SK, Schritt SK-2c, 16.10.2026, Zweig `sk2c` (von `sk3`, Kopf `de315c615`). Konzept:
[`2026-10-16_Konzept_Energieflussbild_Sankey.md`](../../2026-10-16_Konzept_Energieflussbild_Sankey.md)
(Abschnitte 2.2, 2.3, 3, 5, 8, 10 mit SK-Q13 bis SK-Q17, Anhang 12 mit der Recherche). Vorgänger:
[`2026-10-16_SK-12_Abbildung_Energiefluss.md`](2026-10-16_SK-12_Abbildung_Energiefluss.md),
[`2026-10-16_SK-3_Oberflaeche_Energiefluss.md`](2026-10-16_SK-3_Oberflaeche_Energiefluss.md).

**Anlass.** Der Anwender fand das Bild des Stands SK-12 (sieben feste Spalten) nicht übersichtlich. Eine Recherche
(Vorbilder AGEB, LLNL, IEA, Eurostat; Regeln aus d3-sankey, floweaver, Zarate et al.; drei bedienbare Varianten an den
Zahlen von 1058 und 1046) endete in den Anwenderentscheiden vom 16.10.2026: **Variante A** für die breite Fassung und den
Bericht (SK-Q13), schmale Fassung nach der Verbinderregel aus Variante B (SK-2d), **Farbe nach Energieform mit Legende**
(SK-Q14), **Verluste unter 1 % nur im Zeigertext** (SK-Q15), Variante C vorgemerkt (SK-Q16), **Variante B zusätzlich als
wählbare Darstellung „Kette | Stufen“** (SK-Q17, gebaut in SK-2d). SK-Q1 bis SK-Q12 gelten weiter, besonders SK-Q12
(Bildschirm ohne Zahlen, Werte im Zeigertext; Bericht mit Jahressummen).

## 1. Umgesetzte Regeln

| Stufe | Regel |
|---|---|
| **Abbildung** `EnergieflussAbbildung.Bild` | Speicher in ihren Bus gefaltet: Entladung als Busband in die Senke (ein direktes Band wird breiter, Zeigertext „davon x MWh/a über Speicher“), Verlust und Füllstandsänderung als Abzweige am Bus, Ladung/Entladung im Zeigertext des Busses, Busname „· Speicher“ (Kurzname unverändert); Kleinstverluste unter 1 % (`KLEINVERLUST_ANTEIL`) nicht gezeichnet, Vermerk im Zeigertext; Nullbänder unter 0,05 MWh/a (`MINDEST_BAND_MWH`) und Knoten ohne Band entfallen; **Spur** je Knoten (`EnergieflussSpur` Strom, Gemischt, Wärme, Kälte: Busse nach Kennung, Wandler nach Abgabeformen, Senken nach größtem Zufluss, Quellen nach größtem Abfluss, Verluste nach Eigentümer); **Rolle = Energieform** (`STROM`, `WAERME`, `KAELTE`, `BRENNSTOFF`, `UMWELTWAERME`, `VERLUST`, `REST`); Legende der vorkommenden Formen in fester Reihenfolge, Texte je Sprache (`EF_LEG_*`). Die Bilanz `EnergieflussBilanz` (SK-1) ist unverändert. |
| **Bilddaten** `EnergieflussBildDaten` | `Spalte` und `Spaltentitel` entfallen; neu `Spur`, `Legende` (`EnergieflussBildLegende`), Aufzählungen `EnergieflussSpur` und `EnergieflussDarstellung { Kette, Stufen }` (SK-Q17). |
| **Renderer** `ChartRenderer.EnergieflussModell` | Spalte = n − 1 − längster Weg zur Senke (`sankeyRight`; 1058 sechs, 1046 fünf, 1018 vier Spalten), keine Spaltentitel; Wegpunkte je Zwischenspalte in der Zielspur, ein Band bleibt **ein** Pfad; Reihenfolge je Spalte: Spur, dann Schwerpunkt-Relaxation (28 Durchläufe, Umordnung nur in der Spur), drei Tauschdurchläufe gegen die gewichtete Kreuzungszahl (auch kombiniert mit der Nachbarspalte), zehn Durchläufe ohne Umordnung; Wandler ab 15 px als Kasten mit Kurzform innen (Bericht „Kurzname · Wert“), Busse dunkle Balken mit fettem Namen, Quellen/Senken helle Balken (Deckung 0,59) mit Namen außen (Kurzform, wenn der Name nicht in den Rand passt); Verluste als Abzweige bis 44 px ohne Balken, sanfter Bogen, oberhalb der Bänder, die tiefer gehen; Deckung 0,9, weißer Saum 0,8 px, Tönungswechsel (0,65) benachbarter Bänder; Legende als Teil des Zeichenmodells (30 px); Beschriftungsplätze mit Ausweichen um Zeilenhöhen, Kästen belegt; Maße breit 1 180 × 460, schmal 680 × 560 (vorläufig verkleinerte Kette), Bericht 1 240 × 490 (620 × 245 pt); Marken `fluss:`/`knoten:` mit Zeigertext unverändert, `legende:<rolle>` ohne Wert; **Kreuzungszahl** `EnergieflussKreuzungen`/`EnergieflussKreuzungspaare` als Messgröße; Parameter `darstellung` (Vorgabe Kette; Stufen bis SK-2d benannt abgelehnt), Naht `Verbinderregel` und `EfBand.Verbinder` für Stufen und schmale Fassung. |
| **Rollen** | `Farbrolle.STROM` (#E9A825) und `WAERME` (#E2573D) neu in `Farbpalette.Vorgabe`, Gruppe „Erzeuger und Bedarf“ von `Diagrammfarben`, Ressourcen `DGF_ROLLE_STROM`/`DGF_ROLLE_WAERME` beider Sprachen; `KAELTE`, `BRENNSTOFF`, `UMWELTWAERME`, `VERLUST`, `REST`, `KNOTEN` weiterverwendet; 62 Rollen. |
| **Ressourcen** | zwölf neue Schlüssel in beiden Sprachen: `DGF_ROLLE_STROM`, `DGF_ROLLE_WAERME`, `EF_LEG_STROM`, `EF_LEG_WAERME`, `EF_LEG_KAELTE`, `EF_LEG_BRENNSTOFF`, `EF_LEG_UMWELT`, `EF_LEG_VERLUST`, `EF_LEG_REST`, `EF_ZEIGER_SPEICHER_AM_BUS`, `EF_ZEIGER_UEBER_SPEICHER`, `EF_ZEIGER_KLEINVERLUST`; `Resource.Designer.cs` über `designer_neu.py schreiben`. `EF_SP_QUELLEN`, `EF_SP_WANDLER`, `EF_SP_BEDARF` sind ohne Verwendung (Spaltentitel entfallen); `EF_SP_SPEICHER` trägt den Buszusatz. |

## 2. Abweichungen von `Varianten.html` (Anschauung der Recherche)

- **Tönung** über die Deckung (0,9 / 0,65) statt über einen 22 % helleren Farbwert — das Zeichenmodell kennt nur Rolle,
  Festfarbe und Deckung, und eine Rolle darf keinen zweiten Farbwert tragen (Rückwärtssuche der Palette).
- **Busse** in `KNOTEN` statt in einem dunkleren Ton der Formfarbe (derselbe Grund); Quellen und Senken in `KNOTEN` mit
  Deckung 0,59 statt eines eigenen Graus.
- **Verlustabzweig** mit sanftem Bogen (Senkung max(4 px; 0,35 · Dicke)) statt max(12 px; Dicke/2) + 6 px und in der
  Ausgangsfolge knapp unter dem Fuß des Knotens eingeordnet statt immer zuunterst — sonst kreuzt er Bänder, die tiefer gehen.
- **Beschriftungsplätze** weichen um Zeilenhöhen (statt 13 px) nach oben und unten aus, Kästen sind belegt; eine Quelle
  oder Senke nimmt die Kurzform, wenn der Name nicht in den Rand passt (statt zu kürzen).
- **Kein weißer Halo** um Texte (das Zeichenmodell kennt keinen Textrand); dafür die Platzregel.
- **Kein Produktname** im Bild ist eine Regel der Bilanz (SK-1, Kurznamen `EF_KK_*`); die Abbildung ändert keine Namen.
- Interaktion (Hervorhebung, Formfilter) und die Verbinder der schmalen Fassung sind nicht Teil von SK-2c (Abschnitt 5).
- **Kreuzungszahl**: gezählt werden Paare sich schneidender Bandmittellinien (geometrisch); die Anschauung zählte nur die
  dicke Kreuzung in 1058. In 1058 kreuzt das 0,7-MWh/a-Band Strom → Wärmepumpe (Kühlen) auf dem Weg von der Stromspur in
  die Kältespur zwei Bänder der Wärmespur (Umweltwärme → Wärmepumpe (Heizen), BHKW → Wärme) — topologisch unvermeidbar,
  weil Umweltwärme als Quelle in der Spalte des Strombusses steht; die dicke Kreuzung der Anschauung (BHKW → Wärme gegen
  Strom → Wärmepumpe (Heizen)) gibt es im Bild nicht mehr, weil BHKW → Wärme unter der Wärmepumpe läuft.

## 3. Kreuzungszahlen (breite Fassung, Paare sich schneidender Bandmittellinien)

| Projekt | Vorher (SK-12) | Nachher (SK-2c) | Wache |
|---|---|---|---|
| 1018 | nicht gemessen — Befund der Recherche §5: Spalte „Wandler II“ leer, Strom → Einspeisung über vier Spalten hinter Knoten | 0 | ≤ 0 |
| 1046 | nicht gemessen — Stromsenken unten rechts kreuzen die Wärmeseite, Nullband „ungedeckt 0,0“ | 0 | ≤ 0 |
| 1058 | nicht gemessen — Strom → Strombedarf/Einspeisung über vier Spalten, Umweltwärme hinter der Wärmepumpe | 2 (beide Strom → Wärmepumpe (Kühlen), s. o.) | ≤ 2 |
| 1063 | nicht gemessen | 2 | ≤ 2 |
| ChartProben-Gaben | — | 3 (alle Strom → Kältemaschine durch die Wärmespur) | genau diese Paare |

Die Messgröße ist mit SK-2c neu; der Stand SK-12 ließ sich damit nicht mehr messen (Renderer ersetzt). Die Bilder der
vier Referenzprojekte (breit, schmal, Bericht) wurden gegen `Variante_A.png`, `Variante_A_1046.png` und
`Variante_A_Bericht.png` der Recherche gesichtet: Aufbau, Spuren, Kästen, Abzweige und Legende entsprechen der Vorlage;
die Bilder sind mindestens so ruhig.

## 4. Nachweise

- `EPOS.Kern.Tests`: `EnergieflussModellTests` (Maße, Kurznamen, Spalten und Spuren, Wegpunkte ohne Kasten dahinter,
  Anschluss der Bänder, Kästen, Verlustabzweige, Mindestband, Saum und gestrichelter Rest, ungültige Bänder, leeres Bild,
  Kreuzungsfreiheit, keine Zahl am Bildschirm, Legende, Berichtswerte, Marken und `data-wert`, Rollen und Tönungen,
  Darstellung Kette/Stufen, Determinismus), `EnergieflussAbbildungTests` (Speicher gefaltet und nichts verloren,
  Kleinstverluste und Nullbänder, Spuren, Rollen und Bandarten, Legende je Sprache, unbekannte Knoten, leere Bilanz,
  Flussbau bis Zeichenmodell), `EnergieflussReferenzprojektWacheTests` (1018, 1046, 1058, 1063 in drei Fassungen samt
  Kreuzungszahl; PNG über `EPOS_ENERGIEFLUSS_PNG_ORDNER`), `ChartRendererRollenTests` (Tauschpalette mit `STROM`,
  `WAERME`, `REST`, Legende), `FarbpaletteTests` (62 Rollen); gefiltert
  `Energiefluss|ChartRenderer|Zeichenmodell|Farb|Wache`: 854 grün.
- `EPOS.UI.Tests` gefiltert `Uebersicht|Stilblatt|Farb|Einstellungen|KiMasken`: 414 grün (die Ansicht Fluss nimmt das
  neue Zeichenmodell ohne Änderung an Hülle und Komponente).
- `Proben/ChartProben`: 269 Bilder, 0 Verstöße; die zwölf `energiefluss*`-Zeilen der `Messlatte_2026-10-16.sha256`
  ersetzt, die übrigen 226 unverändert (`diff` gegen die Hashliste des Laufs: nur Reihenfolge); Begründung in
  `Proben/ChartProben/LIESMICH.md`.
- Ressourcen beider Sprachen, `designer_neu.py schreiben` (+12 Einträge, wiederholbar).

## 5. Offen (Folgeschritte)

- **SK-2d:** Darstellung **Stufen** (Variante B, SK-Q17) über `EnergieflussDarstellung.Stufen` — vier feste Spalten mit
  Titeln, Verbinder über `Verbinderregel`/`EfBand.Verbinder` (Stummel „→ Gerät“ am Bus und „Strom“ am Gerät, gemeinsame
  Marke, Bericht mit Werten an beiden Stummeln), Schalter „Kette | Stufen“ in der Ansicht Fluss (Vorgabe Kette, je Benutzer
  gemerkt), der Bericht zeigt die Kette; dieselbe Verbinderregel für die **schmale Fassung** (unter 900 px, bisher die
  verkleinerte Kette 680 × 560) und als Rückfall über sechs Spalten; **Formfilter** Alle | Strom | Wärme | Kälte;
  **Pfad-Hervorhebung** bis zum nächsten Bus — das Umfeld eines Knotens lässt sich nicht als Attribut mitgeben, weil ein
  `Zeichenbefehl` nur Marke und Wert trägt: entweder ein drittes Attribut (Schreiber, Maler, `ZeichenmodellWacheTests`)
  oder die Rechnung im JS des Bausteins aus den Marken `fluss:<von>:<nach>`; `diagrammprobe.mjs` Fall `fluss`.
- **SK-4 (Bericht):** `k.Bild(m, 620, 245)` statt 620 × 310 (Konzept 5); die Legende steht im Bild.
- **Konzept Diagramme, Abschnitt 9:** die Rollen `STROM` und `WAERME` nachtragen (mit SK-5).
- Die Ressourcen `EF_SP_QUELLEN`, `EF_SP_WANDLER`, `EF_SP_BEDARF` können mit SK-5 entfallen, wenn Stufen sie nicht als
  Spaltentitel nimmt.

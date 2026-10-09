# Kältemaschinen: Importart, CSV-Kennfeldvorlage und eingebaute Typkennfelder (KM1)

Umsetzung der Stufe 1 aus der [Recherche Kälteanlagen](2026-10-08_Recherche_Kaelteanlagen_Herstellerdaten_Rechenmodelle.md)
(Abschnitt „Empfehlung für EPOS-Plan“, Stufe 1). Anlass ist der Anwenderwunsch, mehrere Arten von
Kälteanlagen zur Auswahl zu haben; der Katalog führte bisher drei Beispielgeräte.

## Umsetzung Stufe 1

### Formate

| Form | Erkennung | Inhalt | Rückkühlart |
|---|---|---|---|
| Kurvendatei PNNL Copper `chiller_curves.json` (BSD-2) | Datei beginnt mit `{` | je Satz `ref_cap` (+ Einheit, Tonnen → kW mit 3,51685), `full_eff` (COP; `eer` und `kw/ton` umgerechnet), `min_plr`, `min_unloading`, `compressor_type`, `compressor_speed`, `condenser_type` und die bi-quadratischen Kurven `cap-f-t`, `eir-f-t` samt Gültigkeitsgrenzen (`si` oder `ip`) | `air` → LUFT, sonst NASSKUEHLER (im Katalog änderbar, solange die Kondensatorart passt) |
| CSV-Kennfeldvorlage [`Quellen/Kaeltemaschine_Kennfeldvorlage.csv`](../../../Quellen/Kaeltemaschine_Kennfeldvorlage.csv) | alles andere | Kommentarzeilen `#`; Kopfzeilen `Schlüssel;Wert` (Bezeichner, Firma, Typ, Beschreibung, Rückkühlart, Nennkälteleistung, Nenn-EER, Mindestteillast, Kältemittel); Datenzeilen `Rueckkuehltemperatur;Kaltwassertemperatur;Kaelteleistung_kW;EER`; jede Zeile „Bezeichner“ beginnt ein neues Gerät | aus der Kopfzeile, Vorgabe LUFT |

Gelesen werden aus Copper nur EIR-Sätze mit Kondensator-Eintritt (`model = ect_lwt`); die
„Reformulated“-Sätze (`lct_lwt`) und Sätze ohne Referenzleistung werden mit Grund übergangen und als
Info-Meldung genannt. Die CSV-Vorlage erkennt das Trennzeichen (`;` vor Tabulator vor `,`); bei `;` und
Tabulator gilt Komma oder Punkt als Dezimalzeichen, bei `,` der Punkt. Fehlen Nennkälteleistung oder
Nenn-EER, kommen sie aus dem eigenen Kennfeld am Nennpunkt (bilinear, wie die Simulation).

Geschrieben wird über `KaeltemaschineStammCtrl.Speichern` (Kopf und Kennlinie in einem Vorgang, dieselben
Prüfregeln wie im Katalogdialog). Ein ausgelieferter Satz (`ReadOnly = 1`) wird nie überschrieben; der
Importsatz zählt ihn dann als übersprungen.

### Rasterregel

Aus CAPFT und EIRFT entsteht das bilineare Kennfeld der EPOS-Kältemaschine: Q = Q_ref · CAPFT,
EER = COP_ref / EIRFT.

- **Kaltwasser-Achse** (Vorlauf, Austritt aus dem Verdampfer — dieselbe Achse wie im EIR-Modell und in
  `Kaeltemaschine.cs`): 5, 7, 10, 15 °C. Außerhalb der Gültigkeit der Kurve gilt deren Randwert
  (Copper-Klemmung).
- **Rückkühl-Achse**: sechs gleichabständige Stützstellen im Schnitt der Gültigkeitsgrenzen beider Kurven mit
  einem Vorgabeband — Außenluft 15 … 45 °C, Kühlwasser-Eintritt 15 … 40 °C —, auf 0,1 K gerundet.
- Ergebnis: 24 Punkte je Gerät; Leistung auf 0,01 kW, EER auf 0,001 gerundet.

### Bezug bei Luftkühlung

EPOS wertet das Kennfeld einer luftgekühlten Maschine an Rückkühltemperatur = Außenluft + 5 K aus
(`KaelteFestwerte.GRAEDIGKEIT_LUFT_K`), die Copper-Luftkurven gelten an der Außenluft. Konzeptentscheid
dieser Umsetzung: Jede Rasterzeile T_rk wird an der Kurve bei T_kond = T_rk − 5 K ausgewertet. Die
Simulation trifft damit den Kurvenwert bei der tatsächlichen Außenluft; ohne Versatz rechnete ein
importiertes luftgekühltes Kennfeld um rund 12 bis 15 % zu ungünstig. Wassergekühlt (Trockenkühler,
Nasskühler, Wasser) ist die Rückkühl-Achse der Kondensator-Eintritt selbst, ohne Versatz. Der Rechenweg
der Simulation bleibt unverändert.

### Nennpunkt

Nennkälteleistung und Nenn-EER stehen am Eurovent-Nennpunkt, nicht an der US-Referenz der Kurven:
Kaltwasser 7 °C Austritt; Außenluft 35 °C (im Kennfeld also Rückkühltemperatur 40 °C) bzw.
Kühlwasser-Eintritt 30 °C. Die Mindestteillast kommt aus `min_plr`, ersatzweise `min_unloading`; das
Kältemittel bleibt leer, weil die Quelle keines nennt.

### Eingebaute Typkennfelder

34 Typkennfelder liegen als eingebettete Ressource im Kern
(`EPOS.Kern/Allgemein/Katalog/KaeltemaschinenTypkennfelder.json`, Klasse `KaeltemaschinenTypkennfelder`
mit `Lesen()` und `Einspielen()`). Sie tragen die unveränderten Copper-Sätze; das Kennfeld entsteht zur
Laufzeit nach der Rasterregel und wird so skaliert, dass die Kälteleistung am Nennpunkt genau die
Leistungsklasse ist (der EER bleibt).

| Rückkühlung | Verdichter | Leistungsklassen [kW] | Nenn-EER (Eurovent) |
|---|---|---|---|
| Luft | Scroll | 20, 50, 100, 200, 500 | 2,9 – 3,0 |
| Luft | Schraube | 200, 500, 1 000, 2 000 | 2,8 – 3,1 |
| Luft | Schraube drehzahlgeregelt | 500, 1 000 | 3,3 |
| Nasskühler | Scroll / Hubkolben | 50, 100, 200 / 100 | 4,6 – 4,7 / 3,2 |
| Nasskühler | Schraube | 200, 500, 1 000 | 4,9 – 5,4 |
| Nasskühler | Turbo / Turbo drehzahlgeregelt | 500, 1 000, 2 000 / 1 000, 2 000 | 6,0 – 6,3 |
| Trockenkühler | Scroll / Hubkolben | 50, 100, 200 / 100 | 4,6 – 4,7 / 3,2 |
| Trockenkühler | Schraube | 200, 500, 1 000 | 4,9 – 5,0 |
| Wasser | Scroll, Schraube, Turbo, Turbo drehzahlgeregelt | 100, 500, 1 000, 1 000 | 4,7 – 6,3 |

**Auswahlregel:** je Art (Kondensator, Verdichter, Drehzahl) und Klasse der Copper-Satz, dessen
Referenzleistung höchstens um den Faktor 2 von der Klasse abweicht und dessen Nenn-EER dem Median der Art
am nächsten liegt; ohne Kandidaten im Faktor 2 der leistungsnächste. Plausibel ist ein Satz mit
1,8 < EER_nenn < 9, 0,8 < CAPFT_nenn < 1,25 und mit der Rückkühltemperatur fallendem EER. Für den
Trockenkühler werden Sätze bevorzugt, deren Gültigkeit bis mindestens 35 °C reicht; Turbo-Maschinen
stehen bewusst nicht am Trockenkühler (ihre Kurven enden meist bei 24 bis 30 °C Kühlwasser). Ausgelassen sind
die luftgekühlten Turbo- und Hubkolbensätze (ohne Referenzleistung) und die Wärmerückgewinnungssätze.

**Neutral:** Bezeichner „Typkennfeld &lt;Rückkühlung&gt; &lt;Verdichter&gt; &lt;Klasse&gt; kW“, Firma leer, Typ
„Typkennfeld“, Beschreibung „Typkennfeld aus offenen US-Kurvendaten (PNNL Copper, BSD-2), Datensatz
&lt;Nr.&gt;; Nennpunkt Eurovent“. Lizenzhinweis mit dem BSD-2-Text:
[`Quellen/LIZENZ_Kaeltemaschinen_Typkennfelder.txt`](../../../Quellen/LIZENZ_Kaeltemaschinen_Typkennfelder.txt);
die Hilfe des Importdialogs nennt Quelle und Lizenz.

**Einspielen** schreibt die Sätze als Auslieferungssätze (`ReadOnly = 1`, `Katalog_Schluessel` =
`KM:TYPKENNFELD_…` aus dem Bezeichner, Prüfsumme über `KatalogSchluesselSaat`) und ist idempotent: Ein Satz,
dessen Schlüssel oder Bezeichner schon steht, zählt als übersprungen. Die Testdatenbank trägt die Sätze noch
nicht; das übernimmt der Schemaschritt 206 (`KaeltemaschinenTypkennfelderSchema`), der `Einspielen` aufruft.

### Oberfläche

Die Verwaltung „Kältemaschinen“ führt in der Fußleiste „Import…“ und „Typkennfelder laden…“. „Import…“
öffnet unter Windows den Katalogimport der Art Kältemaschine (Dateiwahl `*.json;*.csv`, Unterordner
`Kaeltemaschine` der Herstellerdaten); ohne Importweg — auf iOS — lehnt der Knopf benannt ab und verweist
auf die Typkennfelder. „Typkennfelder laden…“ fragt nach und meldet neu angelegte und übersprungene Sätze;
er steht auf beiden Plattformen.

## Was Stufe 2 noch braucht

- **Teillast:** Das Kennfeld bleibt zweidimensional, Teillast läuft linear mit dem EER der Stunde. Die
  Copper-Sätze tragen bereits `eir-f-plr`; eine Lastachse (EIRFPLR) und der Taktverlust unter der
  Mindestteillast (C_d nach EN 14825, Vorgabe 0,9) fehlen, ebenso die Auswertung der Ökodesign-Punkte A–D.
- **Ränder:** Außerhalb der Kurvengültigkeit gilt der Randwert; für Kaltwasser über 10 bis 13 °C
  (Kühldecken, Kühlkurve KK) und Rückkühlung über dem Kurvenende fehlt eine Extrapolation mit konstantem
  Gütegrad (Muster EN 15316-4-2).
- **Herkunft:** Die Copper-Herkunftscodes 1 bis 5 sind nicht aufgeschlüsselt; eine Skalierung auf den
  Nennpunkt eines EU-Geräts (`Nenn_EER` aus einem Datenblatt) ist vorbereitet, aber nicht angeboten.

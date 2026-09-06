# H13b — Hilferubrik „Berechnung", Teil B: Erzeuger und Speicher (Umsetzungsprotokoll, 06.09.2026)

Ausgangsstand `0785680` (Zweig `ios_migration`). Vorgänger: `H1H2_Umsetzung_Protokoll.md`,
`H7_InfoButtons_Protokoll.md`, `H11_Sammelpaket_Protokoll.md`, `H12_FeldHilfe_Protokoll.md`.
Teil A desselben Pakets (Rubrikaufbau, Kern-Lader, Bedarfsseiten) läuft parallel und wird getrennt
protokolliert.

**Anwenderwunsch vom 06.09.2026, wörtlich:**

> „Erweiterung Hilfe: Erläutere in der Hilfe jeweils die Berechnungswege, Warmwasser, Brauchwasser,
> Pufferspeicher … PV-Module-Berechnung, optional mit Wechselrichter, Solarthermie. Setze
> entsprechende Hilfen an die Info-Buttons in den relevanten Dialogen."
> „und weitere Komponenten (mit Hilfe Berechnung)."
> „(die Details der Berechnung sollten in einer Separaten Hilferubrik auf der wiki sein und nicht in
> der allgemeine Erklärung der Funktionen. Die Erläuterung sollte aber aufrufbar sein aus den
> allgemeine Erklärungen mit Bezügen)."

**Ergebnis in einem Satz:** Sieben Seiten der neuen Wikirubrik `Programm Dokumentation/Berechnung/`
liegen als MediaWiki-Markup im Rechenkern (**1 512 Zeilen**, eine Datei je Seite, über ein Glob
eingebettet), **zehn** neue Zuordnungszeilen führen aus **zehn** Razor-Dialogen dorthin, und
**53 Testfälle** halten Aufbau, Zuordnung, Einbettung und Knopf. Build `WP-Plan.Kern.slnf`
0 Fehler / 0 Warnungen; `EPOS.UI.Tests` **2 774/2 774**, `EPOS.Kern.Tests` **1 351/1 351**.

---

## 1. Die Bauform

| Punkt | Umsetzung |
|---|---|
| Ort der Texte | `EPOS.Kern/Allgemein/Hilfe/Berechnung/<Seite>.wiki` — MediaWiki-Markup, unverändert in die Wikiseite kopierbar |
| Kopfblock | Wiki-Kommentar in Zeile 1–4: Rubrik, Seite, Stand, die Fundstellen im Rechenkern. Auf der Wikiseite unsichtbar — er gehört dem Entwickler |
| Gliederung | sechs Abschnitte auf jeder Seite: `Was berechnet wird`, `Eingangsgrößen`, `Rechenweg`, `Grenzen und Annahmen`, `Ergebnisse und wo sie stehen`, `Bezüge`. Die Photovoltaikseite führt einen siebten (`Wechselrichter`) |
| Einbettung | `EPOS.Kern.csproj`, ein Glob mit `LogicalName="EPOS.Kern.Hilfe.Berechnung.%(Filename)%(Extension)"` |
| Zuordnung | `help_mapping.txt`, neuer Abschnitt `# H13 — Rubrik Berechnung` am Dateiende, darin `# Teil B (Erzeuger und Speicher)` |
| Schlüssel | `<Formname>.Berechnung` → `Berechnung/<Seitenname>` |
| Knopf | vorhandener Baustein `InfoKnopf`, am Kopf des Abschnitts, der den Rechenweg parametriert. **Der Fensterknopf oben rechts bleibt** |

**Keine Quelltextpfade im sichtbaren Text.** Eine Wikiseite, die auf `.cs`-Dateien zeigt, altert mit
dem nächsten Umbau und hilft dem Anwender nie; die Fundstellen stehen im Kopfkommentar. Ein
Testfall hält das.

---

## 2. Die sieben Seiten

Jede Zahl, jede Formel und jeder Rückfallwert ist aus dem Rechenkern gelesen, nicht aus dem
Gedächtnis. Was der Kern **nicht** tut, steht auf jeder Seite unter „Grenzen und Annahmen" — das
ist der Teil, für den die Rubrik überhaupt angelegt wurde.

### 2.1 `Photovoltaik` (293 Zeilen)

Belegt: der Ortszeit-Lesepfad (UTC + 1 h bzw. + 2 h, Umstellung letzter Sonntag März/Oktober,
Sonnenstand weiter auf der UTC-Zeitmarke); `P_STC` aus Modulleistung mal Anzahl mit dem Rückfall auf
die Flächenformel und der 3‑%‑Konsistenzprüfung; das NOCT-Fenster 20…60 °C mit Rückfall 45 °C und
seiner Begründung (im Altbestand steht dort vielfach der Kurzschlussstrom); isotrope Transposition
mit Albedo 0,2 bzw. Hay-Davies mit `R_b`, `I_0n` und `A_i`; linearer γ‑Gang bzw. Huld mit **allen
drei Koeffizientensätzen** (C_SI, CIS, CdTe) samt der Klemme bei G′ = 0,001; Systemverluste;
Wechselrichter-Vorgaben 0,95 (einfach) und 0,94 / 0,975 / 0,97 (Kennlinie); Clipping; die
Verbrauchsbilanz mit der BHKW-Klemme.

**Der Abschnitt „Wechselrichter"** trägt die zwei Optionen des Anwenderentscheids **W6‑E‑3**:
Option 1 „vereinfacht, ohne Wechselrichter, mit Pauschalen" (der heutige Weg) und Option 2 „mit
Wechselrichter" als **Ausblick, in Umsetzung, Stand 06.09.2026** — Katalogobjekt mit Projektkopie,
Strangzuordnung, sechs Stützstellen samt der η_euro-Wichtung, der fünfstufige Stundenweg
(Strang → MPPT → Gerät → Kennlinie/Clipping/Nacht → Anlage) und die **acht Auslegungsprüfungen**
P1…P8 mit ihrer Rot/Gelb-Bewertung, den Auslegungstemperaturen −10 °C und 70 °C und der Näherung
„β_OC statt eines eigenen MPP-Koeffizienten". Dazu die Zusage, dass eine Anlage ohne Strangzeile
Zeichen für Zeichen weiterrechnet.

Lücken: keine Verschattung, eine Ausrichtung je Anlage, kein Ein-Dioden-Modell — und die **sechs
elektrischen Kenngrößen des Modulkatalogs** (U_MPP, U_Leerlauf, I_MPP, I_Kurzschluss, α_SC, β_OC)
stehen im Dialog und gehen in **keine** Rechnung ein.

### 2.2 `Heizkessel` (210 Zeilen)

Belegt: die Wirkungsgradweiche Öl/Gas über die Brennstoff-IDs 6…9 und 18…22; die Prozentschwelle
**1,5** (Brennwertkessel liefern Hi-basiert bis rund 104 %, die naheliegende Schwelle 1,0 hätte sie
auf 0,01 zerlegt) und der Rückfall **0,90**; der Bereitschaftsverlust mit Schwelle 1,0 und als
Faktor mal Nennleistung in der Stillstandsstunde; die Brennstoffbilanz **genau einmal je Stunde und
Kessel** nach allen Phasen samt der Begründung (je Kanal getroffen fiele der Stillstandsverlust
doppelt an); der Jahresnutzungsgrad aus den Jahressummen mit den Klemmen 110 → 108 und < 1 → 1;
die zehn Brennstoffzähler samt Sammelposten; die Gasspitze und ihre 0,1‑MWh‑Schwelle; die
Kessel-Kaskade mit `Anteil`, `Q_Puffer` und der zweifachen Schranke von `MaxAbgabe`.

**Lücke, ausdrücklich benannt (Befund W14a‑E‑8‑B1):** Die fünf Emissionsspalten des Kesselkatalogs
werden gepflegt, aber **nicht gerechnet** — die Faktoren kommen aus dem Energieträgerkatalog. Und
weil dieser **kein CO** führt, ist die CO-Emission des Kessels immer 0. Weitere Lücken: kein
Teillastwirkungsgrad, keine Taktung, keine Übertragergrenze zwischen Quellpuffer und Kessel,
höchstens zehn Kessel.

### 2.3 `BHKW` (200 Zeilen)

Belegt: die drei Fahrweisen als Stundenschritt mit ihren Zuschaltfällen (wärmegeführt W1/W2,
stromgeführt, Zero-Export mit zwei Durchläufen und der Regel „reicht der Strombedarf nicht, bleibt
das Modul aus"); die Modulationsgrenze als **Faktor** aus Katalog beziehungsweise Anlage
(Prozent/100) mit dem Rückfall **30 %**; die Stromkennzahl `P_el/P_therm`; der Verbrauch
`(Wärme + Strom)/η`; die Emissionen aus den **Gerätewerten** (der Gegenfall zum Kessel); die
Gasspitze `P_therm·(1 + SKZ)/η`; die Energieprobe „Produktion = Direktdeckung + Speicherladung +
Überschuss"; die Vollbenutzungsstunden thermisch und elektrisch samt der leistungsgewichteten
Anlagenkennzahl.

Lücken: die Betriebsart gilt **projektweit**; ein Wirkungsgrad ohne Teillastkennlinie und mit
unveränderter Stromkennzahl in Teillast; keine Taktung, keine Bereitschaftsverluste. Der
**Sommerbetrieb des Vorläufers** (Tagesstunden 11…21 samt zwei Notschaltungen) ist als
unerreichbarer Zweig entfallen — das steht als Grenze auf der Seite, weil es sonst niemand mehr
nachlesen kann. Dazu die **Dublette `Investition_kwel`** ohne Leser (Befund W14a‑E‑8‑B3).

### 2.4 `Wärmepumpe` (240 Zeilen)

Belegt: die **sieben Wege der Quelltemperatur** (Außenluft, konstant, Erdsonde und Kollektor nach
VDI 4640, Quellprofil in drei Betriebsarten, CSV, Pufferspeicher mit Stundenkopplung an der
Entnahmehöhe) samt der Ersatzannahme „Außentemperatur" mit voller Wirkung auf die
Jahresarbeitszahl; die Kennfeldauswertung mit Kappung nach oben, linearer Interpolation und den
**zwei** Wegen nach unten (Kappung beim Booster, sonst Extrapolation nach der Projekteinstellung
oder Abbruch); `P_el = P_therm/COP`; die drei bivalenten Betriebsarten samt ihrer Vergleichsgrenze
(teilparallel ≤, alternativ <) und der **wörtlich geltenden** Vorbelegung 0 °C; die Sperrzeit; die
Quellbegrenzung über `Q_Quelle = P_therm − P_el` mit proportionaler Kürzung; die Laufzeit als
Vollbenutzungsstunde; die drei Leistungssteuerungen mit der Ausnahme für reine Ladeanlagen und der
Regel „das PV-Budget wird erst in der Ladephase verbraucht"; der Heizstab als Phase F mit COP 1;
der **Bivalenzpunkt als Ergebnis** (höchste Außentemperatur mit offenem Rest, gemessen vor dem
Heizstab); die JAZ einschließlich Heizstabstrom.

**Lücke (Befund W14a‑E‑8‑B2):** Länge, Breite, Höhe, Gewicht und Raumbedarf haben im ganzen Bestand
**keinen Leser** — die einzigen fünf Spalten aller sieben Gerätekataloge ohne jede Verwendung.
Weitere Lücken: keine Abtauverluste, keine witterungsgeführte Heizkurve, kein Kühlbetrieb in der
Jahressimulation.

### 2.5 `Pufferspeicher` (201 Zeilen)

Belegt: `Q_max = Volumen · 1,16 · (VL − RL)/1000` mit dem Rückfall-ΔT **10 K** (BHKW-Pendelspeicher
**20 K**, mit Begründung); Verlust je Stunde = Tageswert/24; die Auflösung der Ladeobergrenze
(eigene Ladegrenze → Abschaltschwelle → Abschaltschwelle für Nachrangige) samt Solar-Reservezone und
der PV-Sonderregel; Ladefähigkeit und Bilanzraum; der Speicher als **hydraulische Weiche** mit der
Zerlegung Umsatz/Durchfluss; Lade- und Entladegrenze als **Budget der Stunde**; die Hysterese; der
füllstandsanteilige Bereitschaftsverlust einmal je Stunde; die Vollzyklen; das Schichtmodell
(N = 1…10, Schicht-Invariante, ideale Einschichtung und Verdrängung, Ausgleichskappung **25 %**,
Inversion, Vorbelegungen λ = 1,5 W/(m·K) und H/D = 2,5) samt der Zusage, dass N = 1 wirkungslos
bleibt.

Lücken: Energiebilanz statt Strömung, ideale Schichtung, gleich große Zonen, Bereitschaftsverlust
als Tageswert ohne Umgebungsbezug, statischer eigenständiger Quellspeicher, Durchfluss außerhalb der
Schichten, kein interner Wärmeübertrager.

### 2.6 `Solarthermie` (159 Zeilen)

Belegt: derselbe Ortszeit-Lesepfad wie bei der Photovoltaik samt Begründung; isotrope Transposition
mit Albedo 0,2; die b₀-Näherung des IAM aus K_dir(50°) mit der Klemme gegen die Division durch null;
der Wirkungsgrad nach EN 12975; das Bruttopotenzial über **Aperturfläche mal Anzahl**; die drei Wege
der Wärme und die Regel, dass der Restbedarf aus der **Direktdeckung** gebildet wird.

**Vier fest verdrahtete Annahmen, die im Dialog nirgends stehen**, sind ausdrücklich benannt:
Speichertemperatur **50 °C**, Leitungsverluste **0,92**, isotrope Einstrahlung ohne Hay-Davies,
keine Stagnation und keine Solarkreispumpe. Dazu drei Katalogbefunde: `Kdfu` („K_diff") ohne Leser,
`Modulflaeche` ist Anzeige, Vor- und Rücklauf des Katalogs ohne Leser.

**Der wichtigste Punkt für den Anwender:** Die **Solarthermie-Ganglinien** werden gepflegt, kopiert
und exportiert — der Simulationslauf liest sie **nicht**. Er rechnet den Ertrag aus Klimadaten und
Kollektorkennwerten. Genau deshalb trägt auch der Ganglinien-Dialog einen Knopf auf diese Seite.

### 2.7 `Stromspeicher` (209 Zeilen)

Belegt: das viertelstündliche Raster (35 040 Intervalle, dt = 0,25 h); die kapazitäts- beziehungsweise
leistungsgewichtete Mittelung mehrerer Anlagen; das SoC-Band aus der Variante mit dem Rückfall
10…90 %; η_ch = η_dis = √η_RT mit dem Rückfall 0,90; die Rückfälle 1 C, 0,025 €/(kWh·Zyklus), 3 %
Zins und 20 a; die Investition `c_cap·C + c_pow·P + I_fix`; die Vorverarbeitung mit ihren sechs
Zeilen und der Zusage, dass Überschuss und Defizit einander ausschließen; der Dispatch der drei
Strategien (Dauernutzung, Nachtnutzung, Arbitrage mit Planvorlauf und der Bitgleichheit ohne
Netzpfade); die Bewertung mit der Merit-Order **PV vor BHKW**; Vollzyklen, Annuitätsfaktor,
Rentenbarwertfaktor und `RBF_deg` samt Grenzfällen; die zweistufige Auslegungssuche mit dem
Jahresüberschuss nach Kapitaldienst als Zielfunktion.

Ausdrücklich benannt: Der **Excel-Kompatibilitätsmodus rechnet bewusst wie die Vorlage** (Start bei
0, erstes Intervall ohne Bilanz, Preis nur zur Bewertung, Wirkungsgrad 1) — er ist zum Nachstellen
da, nicht zum Bewerten. Und die **Amortisationszeit ist bewusst nicht die Zielfunktion** der
Auslegung: Sie ignoriert die Nutzungsdauer und liefert systematisch zu kleine Speicher.

---

## 3. Die zehn Zuordnungen

| Schlüssel | Ziel | Razor-Dialog | Ort des Knopfes |
|---|---|---|---|
| `Form_Heizkessel.Berechnung` | `Berechnung/Heizkessel` | `Dialoge/Erzeuger/HeizkesselDialog` | Abschnitt „Modul" (Brennstoffvariante, Vorlauf, Rücklauf) |
| `Form_BHKWEing.Berechnung` | `Berechnung/BHKW` | `Dialoge/Erzeuger/BhkwDialog` | Abschnitt „Modul" (Brennstoffvariante, Grenzleistung, Temperaturen) |
| `Form_WP.Berechnung` | `Berechnung/Wärmepumpe` | `Dialoge/Waermepumpe/WaermepumpeAnlageDialog` | Abschnitt „Spitzenlast und Betrieb" (Heizstab, Sperrzeit, Bivalenz) |
| `Form_Betriebsmodus.Berechnung` | `Berechnung/Wärmepumpe` | `Dialoge/Simulation/BetriebsmodusDialog` | über der Optionsgruppe — die Wahl dort **ist** der Rechenweg |
| `Form_PufferSp.Berechnung` | `Berechnung/Pufferspeicher` | `Dialoge/Erzeuger/PufferspeicherDialog` | Abschnitt „Modul" (Volumen, Bereitschaftsverluste) |
| `Form_PufferSp_Projekt.Berechnung` | `Berechnung/Pufferspeicher` | `Dialoge/Simulation/PufferSpProjektDialog` | Abschnitt „Eigenschaften" (Temperaturen, Schwellen, Grenzen) |
| `Form_SolarKollektoren.Berechnung` | `Berechnung/Solarthermie` | `Dialoge/Solarthermie/SolarkollektorenDialog` | Abschnitt „Kollektor" (Anzahl, Fläche, Neigung, Azimut) |
| `Form_Solarganglinie.Berechnung` | `Berechnung/Solarthermie` | `Dialoge/Solarthermie/SolarganglinieDialog` | über dem Detailblock der Ganglinien |
| `Form_PV.Berechnung` | `Berechnung/Photovoltaik` | `Dialoge/Erzeuger/PhotovoltaikDialog` | Abschnitt „PV Anlage Eigenschaften", vor den Modellfeldern |
| `Form_Stromspeicher.Berechnung` | `Berechnung/Stromspeicher` | `Dialoge/Erzeuger/StromspeicherDialog` | Abschnitt „Modul Eigenschaften" |

**Abweichung A‑1 — der Präfix `Form_WP`.** Die Bauform verlangt `<Formname>.Berechnung` neben dem
Fensterschlüssel; der Fensterschlüssel von `WaermepumpeAnlageDialog` ist aber `Wizard_WPItem.btn_Help`
(die Maske hieß im Bestand `Wizard_WPItem`). Der Auftrag nennt ausdrücklich `Form_WP.Berechnung`, und
das ist auch die tragfähigere Wahl: `Form_WP.btn_Help` zeigt bereits auf die allgemeine Seite
„Wärmepumpe", der Berechnungsschlüssel steht damit unmittelbar daneben. `Wizard_WPItem` wäre ein
Maskenname, den es nicht mehr gibt.

**Zwei Schlüssel auf eine Seite** — bei Wärmepumpe, Pufferspeicher und Solarthermie. Das ist dieselbe
bewusste Zusammenfassung, die `help_mapping.txt` seit H2 für Wärmebedarf, Projektverwaltung und
Kurzanleitung kennt, kein Ersatzziel.

---

## 4. Eine eigene Stilklasse für den Knopf

Der erste Entwurf setzte den Berechnungsknopf in die Knopfleiste `.epos-leiste`. Das ging schief:
`BetriebsmodusDialogTests` zählt „zwei Fußknöpfe" über `.epos-leiste button` und wurde durch den
dritten Knopf rot — zweimal.

Die Lehre steht jetzt als Kommentar im Stilblatt: **Die Knopfleiste des Hauses ist eine AUFZÄHLUNG
von Aktionen.** Ein Hilfeknopf ist keine Aktion der Maske; er gehört dem Abschnitt darunter und darf
dessen Knopfzahl nicht verändern. Alle zehn Wirte tragen deshalb `.epos-berechnungshilfe` —
rechtsbündig, halber Rand nach unten, sonst nichts.

---

## 5. Nachweise

| Nachweis | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | **0 Fehler / 0 Warnungen** |
| `dotnet build EPOS.Kern/EPOS.Kern.csproj -c Release` | 0 Fehler / 5 Warnungen (die bekannten, unverändert) |
| `dotnet test EPOS.UI.Tests -c Release` | **2 774 / 2 774 grün** |
| `dotnet test EPOS.Kern.Tests -c Release` | **1 351 / 1 351 grün** |
| Einbettung | alle sieben Ressourcen `EPOS.Kern.Hilfe.Berechnung.*.wiki` in `EPOS.Kern.dll` |
| Rechenweg | **unberührt** — kein `.cs` des Rechenkerns geändert, kein Referenzlauf nötig |
| SQL | **keine** neue oder geänderte Anweisung |
| Ressourcen | **kein** neuer Schlüssel, `Resource.Designer.cs` unverändert |

**Die neuen Testfälle (53):**

`EPOS.UI.Tests/BerechnungshilfeTests` — 50 Fälle:

* je Seite (7 × 4 = 28): Datei vorhanden; Kopfblock in den ersten vier Zeilen mit Seite, Stand und
  Fundstellen; die sechs Abschnitte der Bauform **in der vorgegebenen Reihenfolge**; kein
  Quelltextpfad im sichtbaren Text.
* die Zuordnungen (2): jedes Ziel `Berechnung/<Seite>` hat eine Datei; jeder Schlüssel steht in
  **genau einem** Razor-Dialog (nicht null — tote Zeile —, nicht zwei — zwei Masken mit demselben
  Ziel).
* je Wirt (10 × 2 = 20): Der Dialog zeichnet auf dem Weg der Windows-Hülle (Wörterbuch →
  Parametersatz, Muster `StartkachelDialogeTests`) und trägt seinen Berechnungsknopf; der
  Fensterknopf steht daneben. Geprüft wird über die **Komponente**, weil der `InfoKnopf` seinen
  Schlüssel nirgends hinzeichnet.

`EPOS.Kern.Tests/BerechnungshilfeEinbettungTests` — 3 Fälle: Der Ordner trägt mindestens sieben
Seiten; zu jeder Datei gibt es eine Ressource unter `EPOS.Kern.Hilfe.Berechnung.<Datei>`; ihr Inhalt
ist zeichengleich zur Datei. **Ohne Lader** — ein Glob ist still, wenn er ins Leere greift.

Der Leser ist gegen sich selbst abgesichert (Mindestzahlen für Zuordnungen und Razor-Dateien), und
geprüft wird **nur** der Abschnitt „Teil B" der Zuordnungsdatei: Teil A hängt seine Zeilen an
derselben Stelle an, und die zwei Teile sollen sich nicht gegenseitig rot färben.

---

## 6. Was der Anwender im Wiki tun muss

Die Dateien sind die **Quelle**, nicht die Wikiseiten. Das Programm kopiert nichts ins Wiki; die
Rubrik entsteht von Hand:

1. **Rubrik anlegen.** Unter `Programm Dokumentation` eine Unterrubrik `Berechnung` — sie entsteht
   im MediaWiki mit der ersten Seite von selbst; ein eigener Anlageschritt ist nicht nötig. Der
   Hilfekatalog lädt die Rubrikseiten über `apprefix=Programm Dokumentation/`, findet die neuen
   Seiten also ohne Zutun.
2. **Sieben Seiten anlegen** unter genau diesen Titeln — die Schreibweise samt Umlauten entscheidet,
   ob der Knopf trifft:
   `Programm Dokumentation/Berechnung/Heizkessel`, `…/BHKW`, `…/Wärmepumpe`,
   `…/Pufferspeicher`, `…/Solarthermie`, `…/Photovoltaik`, `…/Stromspeicher`.
3. **Inhalt einfügen.** Den Inhalt der gleichnamigen `.wiki`-Datei aus
   `EPOS.Kern/Allgemein/Hilfe/Berechnung/` vollständig hineinkopieren — einschließlich des
   Kommentarblocks am Anfang; er ist auf der Seite unsichtbar und sagt dem nächsten Bearbeiter, wo
   die Zahlen herkommen.
4. **Bezüge in den allgemeinen Seiten setzen.** Auf jeder allgemeinen Seite
   (`Programm Dokumentation/Heizkessel` und so fort) eine Zeile ergänzen, etwa:
   `''Wie gerechnet wird:'' [[Programm Dokumentation/Berechnung/Heizkessel|Berechnungsweg Heizkessel]]`.
   Das ist die Hälfte des Anwenderwunsches, die im Wiki liegt: „Die Erläuterung sollte aber
   aufrufbar sein aus den allgemeinen Erklärungen mit Bezügen."
5. **Rubrikseite fortschreiben.** Auf `Programm Dokumentation`, Abschnitt „Hilfeseiten", die sieben
   neuen Seiten aufnehmen — dort sieht man, was an einer Seite hängt, bevor man sie umbenennt.

**Wer eine Seite umbenennt, benennt beides um:** die Wikiseite (mit bleibender Weiterleitung) und
die `.wiki`-Datei im Kern. Der Testfall „jedes Ziel hat eine Datei" fängt die Hälfte davon ab, die
im Repository liegt.

---

## 7. Abnahmepunkte für den Anwender (Windows)

| Nr. | Was zu prüfen ist | Erwartung |
|---|---|---|
| **A‑H13b‑1** | Dialog „Heizkessel", Abschnitt „Modul": der zweite Fragezeichenknopf | öffnet `Programm Dokumentation/Berechnung/Heizkessel`; der Knopf oben rechts öffnet weiterhin die allgemeine Seite |
| **A‑H13b‑2** | Dialog „BHKW", Abschnitt „Modul" | öffnet `…/Berechnung/BHKW` |
| **A‑H13b‑3** | Wärmepumpe → Anlage ändern, Abschnitt „Spitzenlast und Betrieb" | öffnet `…/Berechnung/Wärmepumpe` |
| **A‑H13b‑4** | Simulationskonfiguration → Betriebsmodus einer Wärmepumpe | der Knopf über der Auswahl öffnet `…/Berechnung/Wärmepumpe` |
| **A‑H13b‑5** | Dialog „Pufferspeicher", Abschnitt „Modul" | öffnet `…/Berechnung/Pufferspeicher` |
| **A‑H13b‑6** | Pufferspeicher-Projektverwaltung, Abschnitt „Eigenschaften" | öffnet `…/Berechnung/Pufferspeicher` |
| **A‑H13b‑7** | Dialog „Solarkollektoren", Abschnitt „Kollektor" (nur bei gewählter Projektzeile sichtbar) | öffnet `…/Berechnung/Solarthermie` |
| **A‑H13b‑8** | Dialog „Solarthermieganglinien", über dem Namensfeld | öffnet `…/Berechnung/Solarthermie` |
| **A‑H13b‑9** | Dialog „Photovoltaik Module", Abschnitt „PV Anlage Eigenschaften" (nur bei gewählter Projektzeile) | öffnet `…/Berechnung/Photovoltaik`; die Seite führt den Abschnitt „Wechselrichter" mit beiden Optionen |
| **A‑H13b‑10** | Dialog „Stromspeicher", Abschnitt „Modul Eigenschaften" | öffnet `…/Berechnung/Stromspeicher` |
| **A‑H13b‑11** | alle zehn Knöpfe bei **englischer** Oberfläche | dieselbe Seite durch den Übersetzungs-Proxy — es gibt keine englischen Wikiseiten (Entscheid 7.1a) |
| **A‑H13b‑12** | Fachlicher Gegenlesetest, Seite für Seite | Jede Zahl, jeder Rückfallwert und jede Grenze stimmt mit dem überein, was der Anwender im Betrieb sieht. Besonders zu prüfen sind die Punkte, die im Dialog **nirgends** stehen: Speichertemperatur 50 °C und Leitungsverluste 0,92 der Solarthermie, der Rückfall 0,90 des Kesselwirkungsgrads, die 1‑C‑Annahme des Stromspeichers |
| **A‑H13b‑13** | Bei 125 % und 150 % Skalierung | Der zweite Knopf sitzt rechtsbündig über seinem Abschnitt und verdeckt nichts |

---

## 8. Nicht angefasst

Rechenweg (kein `.cs` im Kern geändert), SQL, `Resource.resx` und `Resource.Designer.cs`,
`help_cache.json`, `HelpCatalog.cs`, `HilfeAutomatik.cs`, `DokuUebersetzung.cs`, `HilfeWissen.cs`,
`WikiWissen.cs`, `Menuetabelle.cs`, `ModulKatalog*`, `PvModulImport*`, `KatalogRegistry`,
`LizenzLage`, `AppWurzel`, `WaermepumpeStammDialog`, `Umsetzungskonzept_iOS_EPOS-Plan.md`.

Ein **Lader** für die eingebetteten Seiten gehört zu Teil A; dieser Teil setzt keinen voraus. Ein
Wissensabschnitt für den KI-Assistenten (`HilfeWissen`) ist ebenfalls Teil A.

---

## 9. Offene Punkte

| Nr. | Punkt |
|---|---|
| **O‑H13b‑1** | Die Seiten liegen im Repository, im Wiki noch nicht. Bis Schritt 6.2 getan ist, laufen die zehn Knöpfe ins Leere — der Katalog kennt das Ziel dann nicht und schaltet den Knopf ab (Verhalten seit H2, kein Fehler). |
| **O‑H13b‑2** | Der Ausblick „Option 2 — mit Wechselrichter" trägt den Stand 06.09.2026. **Wenn die Umsetzung kommt, ist die Photovoltaikseite mitzuführen** — Rechenweg, die acht Prüfungen und die Kennzahlen stehen dort bereits im Wortlaut des Konzepts. |
| **O‑H13b‑3** | Anker je Abschnitt (`{{Anker|…}}`) sind nicht gesetzt. Ein Knopf könnte damit unmittelbar auf „Rechenweg, Schritt 4" springen statt an den Seitenanfang; das Format von `help_mapping.txt` kann es seit H2 (`Slug#Anker`). Lohnt sich, sobald die Seiten im Wiki stehen und ihre Abschnitte stabil sind. |
| **O‑H13b‑4** | Die drei Bedarfsseiten und die Rubrik-Startseite kommen aus Teil A. Die Bezüge dieser sieben Seiten zeigen bereits darauf (`Berechnung/Simulationsablauf`, `Berechnung/Wärmebedarf`, `Berechnung/Strombedarf`) — nach der Zusammenführung ist zu prüfen, dass die Seitennamen wörtlich übereinstimmen. |

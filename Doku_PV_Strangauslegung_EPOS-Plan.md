# Modul, Strang und Wechselrichter — Regeln, Meldungen, Auslegung

Stand 08.09.2026 (Anwenderwunsch: „Wie sind die Regeln, damit Module und Wechselrichter
zusammenpassen; welche Meldungen kann es geben; erstelle eine Dokumentation und eine Methode
zur Unterstützung der passenden Auswahl"). Grundlage ist Kapitel 4.2 des
`Konzept_Wechselrichter_EPOS-Plan.md`; gerechnet wird in
`EPOS.Kern/Allgemein/Import/StrangPlausibilitaet.cs` (die Prüfung) und
`EPOS.Kern/Allgemein/Import/StrangAuslegung.cs` (die Auslegungshilfe, neu).

## 1 Die Begriffe

| Begriff | Bedeutung | Wo es steht |
|---|---|---|
| **Modul** | ein Katalogmodul mit U_oc, U_mpp, I_sc, beta_OC, alpha_SC, P_STC | Administration → Photovoltaik → PV Module |
| **Strang** | n Module **in Reihe** (Spannungen addieren sich), davon p Stränge **parallel** am selben Tracker (Ströme addieren sich) | Strangtabelle im PV-Dialog |
| **MPPT** | ein MPP-Tracker des Wechselrichters; jeder hat sein Spannungsfenster und seine Stromgrenze | Spalte „MPPT" |
| **Gerät** | ein physischer Wechselrichter; Stränge mit derselben **Gerätenummer** hängen am selben Gerät, ein zweites Gerät desselben Typs bekommt Gerät 2 | Spalte „Gerät" |
| **DC/AC** | Σ P_STC der Module am Gerät ÷ AC-Nennleistung des Geräts | Ampel je Gerät |

Zwei Auslegungstemperaturen nach üblicher Praxis: **−10 °C** ist der kalte Fall (höchste
Spannung), **+70 °C Zelltemperatur** der heiße (niedrigste Spannung, höchster Strom). Für die
MPP-Spannung setzt EPOS-Plan `beta_OC` ein; der Katalog führt keinen eigenen Koeffizienten.
Das ist die Näherung im Werkzeugtipp jeder Ampelzeile.

## 2 Die acht Regeln

Ein **fehlender Wert** (0 oder leer) macht die betroffene Prüfung **gelb** und nennt den Wert
(„Werte fehlen: …") — sie schlägt nicht fehl, sie kann nur nichts sagen.

| Nr. | Prüfung | Formel | Farbe bei Verletzung |
|---|---|---|---|
| **P1** | Leerlaufspannung im kalten Fall | n · [U_oc + beta_OC·(−10 − 25)] ≤ U_Dc_Max | **rot** — das Gerät kann bei Frost und Sonne Schaden nehmen |
| **P2** | MPP-Fenster im heißen Fall | n · [U_mpp + beta_OC·(70 − 25)] ≥ U_Mpp_Min | **rot** — der Strang regelt im Sommer ab |
| **P3** | MPP-Fenster im kalten Fall | n · [U_mpp + beta_OC·(−10 − 25)] ≤ U_Mpp_Max | **gelb** — das Gerät regelt an der Grenze |
| **P4** | Eingangsstrom je MPPT | Σ p · [I_sc + alpha_SC·(70 − 25)] ≤ I_Dc_Max | **rot** |
| **P5** | Strangzahl je MPPT | Σ p ≤ Stränge_je_MPPT | **gelb** |
| **P6** | DC/AC-Verhältnis | 1,0 ≤ Σ P_STC ÷ P_AC_Nenn ≤ 1,5 | **gelb**, in beide Richtungen |
| **P7** | DC-Eingangsleistung | Σ P_STC ≤ P_Dc_Max | **gelb** |
| **P8** | Modulsumme | Σ (n · p) = „Anzahl Module" der Anlage | **gelb** |

P1 bis P3 laufen **je Strang**, P4 bis P7 **je Gerät** (P4/P5 je Tracker), P8 je Anlage.
Die Farbe des Abschnitts ist die schlechteste seiner Zeilen.

## 3 Die Meldungen

**Je Strang** (Zeile „Strang n: …" unter der Tabelle, Teile durch „ · " getrennt):

| Text | Bedeutung | Abhilfe |
|---|---|---|
| `10 Module in Reihe, 1 parallel` | die geprüfte Konfiguration | — |
| `U_oc(−10 °C) 537 V ≤ 600 V` | P1 eingehalten | — |
| `U_oc(−10 °C) … V > … V — der Wechselrichter kann bei Frost und Sonne Schaden nehmen` | P1 verletzt | weniger Module in Reihe oder Gerät mit höherer U_Dc_Max |
| `MPP 357…460 V im Fenster 210…500 V` | P2 und P3 eingehalten | — |
| `MPP im Sommer 357 V < 590 V — der Strang regelt ab` | P2 verletzt: im Sommer sinkt die Strangspannung unter das Fenster, das Gerät findet keinen Arbeitspunkt | mehr Module in Reihe oder Gerät mit niedrigerem U_Mpp_Min |
| `MPP im Winter … V > … V — das Gerät regelt an der Grenze` | P3 verletzt | weniger Module in Reihe |
| `Werte fehlen: Leerlaufspannung oder beta_OC des Moduls` (auch: MPP-Spannung, Kurzschlussstrom oder alpha_SC, der Wechselrichter, Module in Reihe, das Modul der Anlage) | nicht prüfbar | Katalogwerte pflegen (Pflegeweg steht im Werkzeugtipp) |
| `passend wären 4…14 Module in Reihe` | **Auslegungshilfe**, steht hinter einem roten oder gelben Strangbefund | die genannte Reihe wählen |
| `keine Reihe passt zu diesem Gerät, anderes Gerät wählen` | Untergrenze aus P2 liegt über der Obergrenze aus P1/P3 | anderes Gerät |

**Je Gerät** (Chip im Kopf „Wechselrichter und Stränge": „Name (Gerät n): …"):

| Text | Bedeutung | Abhilfe |
|---|---|---|
| `I 13,72 A ≤ 30,0 A` | P4 eingehalten (größter Trackerstrom) | — |
| `I … A > … A am MPPT n` | P4 verletzt | weniger Stränge parallel am Tracker, Stränge auf Tracker verteilen (Spalte MPPT), zweites Gerät |
| `3 Stränge am MPPT 1, zulässig sind 2` | P5 verletzt | Stränge auf Tracker verteilen |
| `DC/AC 0,88 liegt außerhalb 1,0…1,5` | P6 verletzt: das Gerät ist zu groß (< 1,0) oder zu klein (> 1,5) für die Module | Modulzahl am Gerät ändern oder anderes Gerät |
| `10,616 kWp über der DC-Eingangsgrenze 9,00 kW` | P7 verletzt | weniger Module am Gerät, zweites Gerät |
| `Angabe fehlt: Zahl der MPP-Tracker — gerechnet wird auf einem` | konservative Annahme | Gerätekatalog pflegen |
| `Kein Wechselrichter zugeordnet` | Strangzeile ohne Gerät | Gerät wählen |
| `passend wären 10…13 Module je Gerät` | **Auslegungshilfe** hinter P6/P7 | Aufteilung entsprechend |
| `Modulsumme 20 weicht von „Anzahl Module" 30 ab` | P8 | Strangtabelle oder Anlagenwert angleichen |

## 4 So wird die Konfiguration passend

1. **Modul wählen** (Projektzeile). Prüfen, dass der Katalog U_oc, U_mpp, I_sc, beta_OC und
   alpha_SC führt — sonst bleibt alles gelb.
2. **Gerät wählen** („Wechselrichter aus dem Katalog"). Faustregel: P_AC ≈ P_STC der Module
   ÷ 1,1…1,3; das MPP-Fenster muss zur Modulspannung mal Reihe passen.
3. **Module in Reihe** so, dass die Strangspannung im Sommer über U_Mpp_Min und im Winter
   unter U_Mpp_Max und U_Dc_Max bleibt. Die Ampel nennt den Bereich, sobald sie rot oder
   gelb ist („passend wären …").
4. **Stränge parallel** je Tracker so, dass der Strom unter I_Dc_Max bleibt (bei den heutigen
   Halbzellenmodulen mit 13…14 A meist ein Strang je Tracker).
5. **Geräte zählen**: mehrere Stränge an einem Gerät teilen sich dessen AC-Leistung. Passt
   die Modulzahl nicht ins Band 1,0…1,5, ein **zweites Gerät desselben Typs** anlegen —
   zweite Strangzeile, Spalte „Gerät" = 2. Leer heißt 1.
6. **Tracker**: zwei Stränge an einem Gerät mit zwei MPP-Trackern bekommen MPPT 1 und 2
   (eine gemeinsame Clipping-Grenze, getrennte Stromgrenzen).
7. **Modulsumme** = „Anzahl Module" der Anlage, sonst meldet P8.

### Beispiel aus der Abnahme (Philadelphia Solar 530 W, 20 Module)

* Ein **SMA Sunny Boy 6.0** mit beiden Strängen (Gerät leer = 1): DC/AC 1,77 und 10,6 kWp über
  der DC-Grenze 9,0 kW → **zwei Geräte**: Strang 1 Gerät 1, Strang 2 Gerät 2, je 5,3 kWp,
  DC/AC 0,88 (gelb: leicht unter 1,0 — mit 12 Modulen je Strang wären es 1,06).
* Ein **SMA Sunny Highpower PEAK3 SHP100** für 5,3 kWp: DC/AC 0,05, und sein MPP-Fenster
  beginnt bei 590 V — zehn Module liefern im Sommer 357 V: „der Strang regelt ab". Die Hilfe
  sagt dazu „passend wären 17…18 Module in Reihe" (aus 53,7 V und 35,7 V je Modul gerechnet);
  für 20 Module geht das nicht auf, und das DC/AC-Band verlangt 190 Module — das Gerät ist
  für Großanlagen, `GeraeteBewerten` reiht es hinter allen passenden ein.
* Zehn Module in Reihe am Sunny Boy 6.0 (Fenster 210…500 V, U_dc 600 V): U_oc(−10 °C) 537 V
  ≤ 600 V, MPP 357…460 V im Fenster — **grün**.

## 5 Die Auslegungshilfe (`StrangAuslegung`)

| Methode | Antwort |
|---|---|
| `Reihe(modul, geraet)` | Bereich für „Module in Reihe" (Min aus P2, Max aus P1/P3) |
| `ParallelJeMppt(modul, geraet)` | Stränge je Tracker (P4, P5) |
| `ModuleJeGeraet(modul, geraet)` | Module je Gerät (P6-Band, P7) |
| `Vorschlagen(modul, geraet, anzahlModule)` | eine Aufteilung: Reihe, Stränge je Gerät, Gerätezahl, DC/AC — wenige Geräte zuerst, dann DC/AC nahe 1,25, dann lange Reihen |
| `GeraeteBewerten(modul, anzahlModule, katalog)` | alle Geräte des Katalogs, passende zuerst |
| `Aufteilen(vorschlag, mppts)` | derselbe Vorschlag als **Tabelle**: je Gerät und belegtem Tracker eine Zeile (Gerät, MPPT, Reihe, parallel) |
| `ReiheEmpfehlung`, `GeraetEmpfehlung` | die Sätze der Ampel |

Die Sätze stehen seit dem 08.09.2026 in der Ampel des PV-Dialogs hinter jedem roten oder
gelben Befund an P1–P3 bzw. P6/P7 („… · passend wären 4…14 Module in Reihe"). Nachweis:
`EPOS.Kern.Tests/StrangAuslegungTests.cs` (Anhang-A-Modul und -Gerät, Zahl für Zahl gegen die
Prüfung) und drei Fälle in `StrangPlausibilitaetTests`.

### In der Oberfläche (seit 08.09.2026, **W6‑B‑8**)

`Vorschlagen`, `Aufteilen` und `GeraeteBewerten` sind im PV-Dialog bedienbar — Abschnitt
„Wechselrichter und Stränge", Weg „mit Wechselrichter":

**1. Die Katalogwahl ist sortiert und beschriftet.** Das Auswahlfeld „Wechselrichter aus dem
Katalog" listet die (nach Hersteller gefilterten) Geräte in der Reihenfolge von
`GeraeteBewerten` — passende zuerst, unter ihnen wenige Geräte vor vielen und DC/AC nahe 1,25
vor entfernterem. Passende Geräte tragen ihre Zahlen im Text:

| Eintrag | Bedeutung |
|---|---|
| `Muster 2500TL — DC/AC 1,10 · 1 Gerät` | passt: ein Gerät, DC/AC 1,10 im Band 1,0…1,5 |
| `Muster 5000TL-2M — DC/AC 1,22 · 2 Geräte` | passt, braucht aber zwei Geräte |
| `Gross 100TL — passt nicht` | keine Aufteilung: Spannungsfenster, Strom oder DC/AC‑Band gehen nicht auf |

Gemessen wird am **Modul der markierten Projektzeile** und an ihrer **Modulzahl** (steht eine
Strangtabelle, gilt deren abgeleitete Summe). Fehlt eines von beiden, bleibt die Liste
alphabetisch und unbeschriftet — ohne Modulfeld gibt es nichts zu bewerten. Die Klapplisten
**je Strangzeile** bleiben in jedem Fall alphabetisch und ohne Zusatz: Dort steht das Gerät
eines einzelnen Strangs.

**2. Der Knopf „Auslegung vorschlagen"** steht neben „Strang anlegen". Er ist frei, sobald ein
Katalogsatz gewählt ist und Modul und Modulzahl feststehen. Sein Klick

* rechnet `Vorschlagen(modul, gerät, modulzahl)` und legt das Ergebnis mit
  `Aufteilen(vorschlag, Anzahl_Mppt)` in Zeilen — je Gerät und belegtem Tracker eine, die
  Stränge so gleichmäßig auf die Tracker verteilt, wie die Zahl es zulässt;
* nimmt den Katalogsatz in das Projekt auf (dieselbe Kopie wie „Strang anlegen");
* **ersetzt** die Strangtabelle durch die Vorgaben (Ränge neu ab 1; Neigung und Azimut bleiben
  leer, also der Anlagenwert) und meldet darunter, was geschehen ist:
  `Vorschlag: 2 Geräte, je 1 Strang mit 10 Modulen in Reihe, DC/AC 1,10 — die Strangtabelle
  wurde ersetzt.`

Gibt es keine Aufteilung, **bleibt die Tabelle stehen**, und der Grund erscheint als Warnung:
`Kein Vorschlag: Die Modulzahl lässt sich nicht in gleich lange Stränge und gleich belegte
Geräte teilen.` (weitere Gründe: „Keine Reihe passt zu diesem Gerät (Spannungsfenster)",
„Spannungswerte des Moduls oder Grenzen des Geräts fehlen", „Modul oder Gerät fehlt",
„Keine Module").

Ersetzt und nicht ergänzt wird, weil ein Vorschlag eine **ganze** Aufteilung des Modulfelds ist
— gleich lange Stränge, gleich belegte Geräte; an bestehende Zeilen angehängt ergäbe er eine
Anlage mit der doppelten Modulzahl.

Gerechnet wird im Kern (`StrangAuslegung`), formatiert in der Windows-Hülle
(`PhotovoltaikHuelle`), gezeigt in `EPOS.UI/Dialoge/Erzeuger/PvStraengeFelder.razor`. Ohne die
beiden Delegaten bleibt alles beim Stand davor — die iOS-Hülle bekommt sie später, ohne dass
die Maske sich ändert. Nachweise: `EPOS.Kern.Tests/StrangAuslegungTests.cs` (`Aufteilen`) und
`EPOS.UI.Tests/Dialoge/PvStraengeFelderTests.cs` (Abschnitt 6).

## 6 Grenzen

Keine Verschattung, keine Kabel- und Anschlussverluste, keine Ost/West-Mischung auf einem
Tracker (die Prüfung rechnet je Tracker die Summe), keine Modul-Mischung in einem Strang, und
die MPP-Spannung mit beta_OC statt eines eigenen Koeffizienten. Für die Simulation gilt
unverändert das Rechenmodell der Anlage (EINFACH oder ERWEITERT); die Ampel ist Prüfung und
Rat, kein Rechenergebnis.

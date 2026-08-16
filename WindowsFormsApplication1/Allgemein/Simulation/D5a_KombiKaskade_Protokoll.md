# Etappe D5a — Kombispeicher und Kessel-Kaskade (Engine + Senken-Dialog)

Stand 16.08.2026 · Umsetzung zu [`Konzept_KonfigUI_Hydraulik.md`](Konzept_KonfigUI_Hydraulik.md),
Etappe **D5a**: der ENGINE-Teil von Kombi-Puffer (Anforderungen 4/7, Entscheidung K-1) und
Kessel-Kaskade (Anforderung 6) sowie die vierte Senken-Option in
`Views/Simulation/Form_Waermesenke.cs`.

Codestand der Verifikation: `6c47a32` + die zwölf Dateien dieser Etappe, gebaut in einem
eigenen git-Arbeitsbaum (`C:\Waermeplan\D5a_wt`); der Haupt-Checkout mit der parallelen
D2/D3-Arbeit blieb unberührt. **Nichts committet.**

> **Stand 16.08.2026 — nach zwei adversarialen Reviews nachgearbeitet.** Sieben
> Engine-Befunde und sieben Integrations-/Oberflächenbefunde sind behoben, die davon
> entwerteten Messungen neu gefahren. **Abschnitt 9 ist der gültige Stand**; die
> Abschnitte 4 und 5 tragen an den betroffenen Stellen einen Ersetzt-Vermerk.
> Codestand jetzt: HEAD `23dd3bc` + der Working Tree des Haupt-Checkouts, 16 Dateien.

---

## 1. Was umgesetzt ist

### Teil 1 — Kombispeicher

Ein Pufferspeicher mit `Tab_Pufferspeicher.Verwendung = "Kombi"` hängt an **beiden** Kanälen
der Kaskadenschleife. Geladen wird kanalneutral in **einen** SOC; entladen wird je Stunde in
beide Kanäle aus demselben Vorrat. Reicht er nicht für beides, gilt **Warmwasser zuerst** —
der Default der Entwurfsentscheidung K-1, umgesetzt als vollständiger Warmwasser-Durchlauf
**vor** dem Heizungs-Durchlauf. Die kanalweise Entladereihenfolge (Konzept 3.6) bleibt
innerhalb jedes Durchlaufs unangetastet; der Kombispeicher steht in **beiden** Kanallisten,
je Kanal an der Stelle seiner `Entladeprio`.

Neues Senkenziel `Tab_Energieanlagen.WS_Ziel/WS_Ziel2 = "PufferKombi"`.

### Teil 2 — Kessel-Kaskade und Rechenreihenfolge

Ein Heizkessel mit `WQ_Typ = "Pufferspeicher"` und gültiger `WQ_ID_Puffer` bezieht seine
**Eintrittstemperatur** aus dem Quellpuffer statt aus dem Systemrücklauf:

```
Anteil   = (T_Quelle − T_Rücklauf) / (T_Vorlauf − T_Rücklauf)          [0 … 1]
Q_Puffer = Anteil · Q_nutz      → ENTLADUNG des Quellpuffers
Q_Kessel = Q_nutz − Q_Puffer    → nur DAS kostet Brennstoff
```

Das ist Zeile für Zeile die Konstruktion des Wärmepumpen-Quellbezugs, nur mit dem
Temperaturhub statt der Leistungszahl als Aufteilungsschlüssel (dort
`Q_Quelle = Q · (1 − 1/COP)`). Liefert der Puffer weniger als `Anteil · Q_nutz`, springt
Brennstoff für den Fehlbetrag ein — die Abgabe an den Kanal bleibt dieselbe.

**Rechenreihenfolge über RECHENEBENEN.** Die Phasen B/C/D der Stundenschleife laufen je
Ebene aufsteigend:

```
A) Vorabentladung (Hysterese)
for Ebene = 0 … maxEbene:
    B) Bedarfsdeckung der Anlagen DIESER Ebene, in Kaskadenreihenfolge
       Durchsatzbudget der Stunde festhalten
    C) Speicherladung (Hauptsenken) DIESER Ebene
    D) Zweitsenken DIESER Ebene
    wenn nicht letzte Ebene: DURCHSATZ beider Kanäle zurückgeben
E) Nachentladung   F) Heizstab   G) StundeAbschliessen je Speicher
```

Ebene 0 = keine Quellpuffer oder ein Quellpuffer, den niemand lädt; Ebene *n+1* = der
Quellpuffer wird von einer Anlage der Ebene *n* geladen. **Bei genau einer Ebene — jedes
Bestandsprojekt — läuft der Rumpf einmal und ist Anweisung für Anweisung die bisherige
Schleife.** Das ist die Regressionszusage dieser Etappe, und sie ist gemessen (Abschnitt 3).

Der Zwischenschritt gibt bewusst nur den **Durchsatz** zurück, nicht den gespeicherten
Inhalt: Der Durchsatz gehört dem Verbraucher, der ihn in dieser Stunde angefordert hat, und
darf von der nächsten Ebene nicht ein zweites Mal gedeckt werden. Der Inhalt bleibt liegen,
bis alle Ebenen ihre Quellentnahme hatten — sonst gäbe es die Kaskade nur auf dem Papier.

**Zyklen** (A lädt B, B ist über weitere Erzeuger Quelle von A) lösen einen Abbruch über den
Fehlerkanal aus, mit Nennung der beteiligten Anlagen.

---

## 2. Änderungen je Datei

| Datei | Stelle | Inhalt |
|---|---|---|
| `Allgemein/DbWerte.cs` | 95, 172 | `WS_ZIEL_PUFFER_KOMBI = "PufferKombi"`, `PSP_VERWENDUNG_KOMBI = "Kombi"` |
| `Allgemein/Simulation/WaermesenkeClass.cs` | 34, 44, 55–120 | `ZIEL_PUFFER_KOMBI`/`VERWENDUNG_KOMBI`; `IstPufferZiel`, `IstKombiVerwendung`, `VerwendungZuZiel`, `ZielAnzeige` erweitert; Festtexte `KOMBI_*` mit TODO |
| " | 407–460 | `ProjektPufferListe(…, bool kanalSicht)` + `PasstZuFilter` — **Senkenziel-Sicht** (exakt) gegen **Kanal-Sicht** (Kombi in beiden Kanälen) |
| " | 566, 800–830, 850 | `VerwendungAnzeige`, `KurzformZuZiel` (eine Fassung statt zweier Kopien), `IstBrauchwasserseitig`, `KanalWarnung` |
| `Allgemein/Simulation/Ladeordnung.cs` | 543, 570 | `Entladereihenfolge` fragt die **Kanal-Sicht** ab |
| `Allgemein/Simulation/SimulationPufferspeicher.cs` | 37, 573–597 | `VERWENDUNG_KOMBI`, `IstKombi`, `BedientKanal(bool)` |
| `Allgemein/Simulation/SimulationKanaele.cs` | 380, 440, 452, 528 | `Senke.PufferKombi` + Abbildung hin/zurück; `Ladeauftrag.Ebene`; Klasse `Quellentnahme` |
| " | 620, 656 | `Kaskadenkontext.QuellpufferJeAnlage`, `HatKombispeicher()` |
| " | 345–370 | Selbsttest Punkt 8 um Kombi-Abbildung und `BedientKanal` erweitert |
| `Allgemein/Simulation/Kaskadenschleife.cs` | 90–150 | Feldkopf Rechenebenen, `_hatKombi`, `_hysteresePhaseA`, `Fehlertext` |
| " | 265–330 | `Anteil_Umbuchen`, `QuellentnahmenVerbuchen` |
| " | 340–395 | `DurchlassBudget` / `DurchlassBuchen` — EINE Fassung für alle vier Erzeugerarten |
| " | 470–560 | Ebenen-Schleife in `Rechnen`, Quellentnahme-Verbuchung, Zwischen-Durchsatz |
| " | 660–740 | `Ladephase(…, ebene)` mit Abzug der umgebuchten Quellwärme |
| " | 745–960 | `EbenenAufloesen`, `EbenenRelaxieren`, `ZyklusMeldung`, `BedarfsordnungJeEbeneBilden`, `ModulmaskenSchreiben` |
| " | 975–1035 | `Entladephase` mit K-1-Reihenfolge, `DurchsatzPhase`, `HystereseDerStunde` |
| " | 1120–1140 | `#if DEBUG` `Entladeprobe` (Prüfhaken, siehe Abschnitt 4) |
| `Allgemein/Simulation/SimulationSPK.cs` | 408–470 | `_kesselStunde` (brennstoffbasiert) / `_kesselAbgabe` (gesamt) getrennt |
| " | 470–620 | Quellbezug: `_quellSpeicher`, `_quellAnteil`, `Quellwaerme_*`, `ModulEbenen`/`AktiveEbene`, `QuellbezugSetzen`, `MaxAbgabe`, `QuellwaermeHolen` |
| " | 700–730 | `Stunde_Bedarf`: Ebenenmaske, Quellanteil, Nennleistung begrenzt den EIGENEN Beitrag |
| " | 770–815 | `Zweikanalig_Laden`: gemeinsames Durchsatzbudget, Quellanteil, `Speicherladung_gesamt` = Eigenanteil |
| " | 840 | `Stunde_Abschluss`: „läuft der Kessel?" entscheidet die ABGABE |
| `Allgemein/Simulation/SimulationWaermepumpe.cs` | 57–125 | `QuellspeicherErsetzen`, `Quellentnahmen`, `ModulEbenen`/`AktiveEbene`, `QuellentnahmeMelden` |
| " | 1060, 1200, 1550 | Ebenenmaske in `Zweikanalig_Bedarfsphase`; Quellentnahme-Meldung an beiden Entnahmestellen; gemeinsames Durchsatzbudget |
| `Allgemein/Simulation/SimulationSolarthermie.cs` | 539 | gemeinsames Durchsatzbudget |
| `Allgemein/Simulation/SimulationBHKW.cs` | 1391, 1428, 1457 | Kombi ist immer „eigener Kanal"; gemeinsames Durchsatzbudget |
| `Allgemein/Simulation/SimulationControl.cs` | 524 | `_kesselInSchleife` auch bei Puffer-QUELLE (sonst rechnete der Kessel außerhalb der Stundenschleife) |
| " | 360 | `AltpfadHinweiseD5a()` im einkanaligen Zweig |
| " | 985 | `QuellbezuegeAufbauen(kontext)` + Fehlertext des Zyklus-Guards |
| " | 1670, 1680 | Entladeordnung über `BedientKanal` (Kombi in beiden Kanälen) |
| " | 2050 | Registry: ein KOMBISPEICHER behält seine Verwendung, auch über die Alt-Zuordnung |
| " | 2215–2290 | `QuellspeicherUebernehmen(bool zweikanalig)` mit Kaskaden-Auflösung, `IstEigenerSenkenPuffer` |
| " | 2300–2450 | `QuellbezuegeAufbauen`, `QuellspeicherInstanz`, `KesselQuellbezugSetzen`, `KesselTemperaturpaar`, `AltpfadHinweiseD5a`, `ErzeugerMitPufferQuelle` |
| `Views/Simulation/Form_Waermesenke.cs` | 54–110 | vierte Option, Kombi-Liste, Festtexte mit TODO |
| " | 130–230 | Layout um eine Zeile (`ClientSize` 592 → 618) |
| " | 400–470 | `_cbZiel2` dritter Eintrag, `SetControls`, `PufferListenLaden` (Senkenziel-Sicht) |
| " | 540–620 | `Zweitsenkenliste`, `ZielWertZweitsenke`, `AnzeigeAktualisieren`, `AktuellerHauptPuffer` |
| " | 700–800 | `AusOberflaeche`, `btnPufferAnlegen_Click`, `BrauchwasserUebergangsHinweis` |

Zeilennummern nach dem Endstand dieser Etappe; sie verschieben sich mit dem D2/D3-Merge.

---

## 3. Regression

Beide Läufe auf **einer** migrierten Datenbankkopie (`Referenzlauf.exe migration` aus
`C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`, **nur gelesen**, keine `Kenndaten.laccdb`
vorhanden), A/B gegen den unveränderten Stand `6c47a32` mit demselben `Referenzlauf.exe` und
getauschter `WindowsFormsApplication1.dll`.

| Lauf | Vergleich | Ergebnis |
|---|---|---|
| **Flag AUS** | HEAD `6c47a32` gegen D5a | **8/8 PASS**, 2 094 447 Werte, **190/190 Dateien byte-/MD5-gleich** |
| **Flag AUS** | eingefrorene Basis `Referenzlaeufe/2026-08-15_B3` gegen D5a | **190/190 byte-/MD5-gleich** — die Quell-DB ist seit B3 **nicht** gedriftet, die Basis wird direkt getroffen |
| **Flag AN**, unpräpariert (alle acht Projekte `Kaskade_Zweikanalig = True`) | HEAD gegen D5a | **8/8 PASS**, 2 094 458 Werte, **190/190 byte-/MD5-gleich** |

Projektmenge: 1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024 (Referenzmenge B3 nach der
Nutzerlöschung von 1010).

Kein Bestandsprojekt trägt einen Kombispeicher oder einen Kessel mit Puffer-Quelle — beide
Erweiterungen sind damit auch mit gesetztem Flag wirkungslos, und genau das zeigt die
Byte-Gleichheit.

---

## 4. Präparierte Szenarien

Alle auf **Projekt 1023** („Wöhler – Test1": WP 11203 Luft-Wasser, WP 11204 Sole-Wasser,
Heizkessel 11205 `ecoVIT VKK 186/5` 19,3 kW, Puffer 1018023 600 l 65/45 → `Q_max` 13,920 kWh,
Warmwasseranteil vorhanden), je eigene DB-Kopie, Flag AN.

Gemessen mit einem Wegwerf-Prüfprogramm (`dev/Probe`), das den Lauf in-process fährt und die
Größen ausliest, die die Ergebnispersistenz nicht führt. Der **Kanal** der Entladung ist eine
davon: `Tab_ErgebnisPufferspeicher` und die Ganglinien führen je Speicher und Stunde EINE
Zahl. Dafür steht in `Kaskadenschleife` ein `#if DEBUG`-Prüfhaken `Entladeprobe` — nach dem
Muster von `Waermekanaele.Selbsttest`, also **kein Prüfcode im Release-Assembly**.

### (a) NUR-Kombi — ein Speicher deckt Heizung und Warmwasser

Puffer 1018023 `Verwendung = Kombi`, WP 11203/11204 → `PufferKombi`.

| Größe | Wert |
|---|---|
| Ladung / Entladung / Verluste / SOC am Jahresende | 92 474,0 / 92 142,3 / 331,7 / **0,000** kWh |
| Durchsatz (Aufnahme / Abgabe) | 45 791,6 / 45 791,6 kWh |
| `StundeAbschliessen` | **8760 / 8760** |
| größter Bilanzrest über alle Speicher | **−1,08 · 10⁻⁹ kWh** |
| bedarfsdeckende Entladung **Warmwasser** | 59 857,7 kWh |
| bedarfsdeckende Entladung **Heizung** | 78 076,2 kWh |
| Summe | 137 933,9 kWh |
| `WP.Speicherentladung_Anteil` (ausgewiesene Deckung) | **137 933,9 kWh — exakt gleich** |
| Stunden, in denen ein Speicher beide Kanäle bediente | 3977 |

Gegenprobe mit derselben Hydraulik, aber `Verwendung = Heizung`: Warmwasser 0 kWh, Heizung
109 638,4 kWh, Heizstab **87 918,6 kWh**. Mit dem Kombispeicher fällt der Heizstab auf
**62 186,8 kWh** — die 25,7 MWh sind genau die Warmwasserdeckung, die der Speicher übernimmt.

### (b) Knappheitsstunde — Warmwasser zuerst (K-1)

Auswertung aller 8760 Stunden aus Szenario (a):

| Prüfung | Ergebnis |
|---|---|
| Stunden mit leergefahrenem Kombispeicher **und** Warmwasserbedarf | 4203 |
| Stunden, in denen der Speicher **Heizung** deckte, obwohl **Warmwasserbedarf offen** war | **0** |

Stundenproben (Ablauf innerhalb der Stunde, `gedeckt → SOC danach`):

```
h = 8   WW-Bedarf 9,2274   E/WW(6,057)→13,920   E/WW(3,171)→10,749   E/HZ(10,749)→0,000
h = 9   WW-Bedarf 14,7639  E/WW(6,694)→13,920   E/WW(8,069)→ 5,851   E/HZ( 5,851)→0,000
```

Der Warmwasserbedarf ist jeweils **vollständig** gedeckt (6,057 + 3,171 = 9,228;
6,694 + 8,069 = 14,763), der Rest geht auf die Heizung, der Speicher wird leergefahren. Die
erste Buchung ist jeweils die Durchsatzrückgabe (Füllstand bleibt bei `Q_max`).

### (c) Gemischt — Kombi + dedizierter Heizungspuffer

Kombi 1018023 (`Entladeprio 2`, WP 11203) und Heizungspuffer 1018022 (500 l, 60/40,
`Q_max` 11,600 kWh, `Entladeprio 1`, WP 11204).

| Größe | Wert |
|---|---|
| Abschlüsse je Speicher | **8760 / 8760** |
| Bilanzreste | −4,10 · 10⁻⁹ / −1,46 · 10⁻¹⁰ kWh |
| Entladung Warmwasser / Heizung | 51 416,2 / 84 871,9 kWh |
| Summe = `WP.Speicherentladung_Anteil` | 136 288,1 / **136 288,2** kWh |
| davon Puffer 1018022 (Heizung) / 1018023 (Kombi) | 54 846,6 / 81 441,5 kWh |
| **Reihenfolgeprobe Heizkanal** (je Stunde und Phase, ohne Durchsatzrückgaben) | 6053 Fälle, **0 Verstöße** — 1018022 (Prio 1) immer vor 1018023 (Prio 2) |
| **Warmwasserkanal** | 6830 Buchungen, davon **0** aus einem anderen als dem Kombispeicher |

### (d) Kessel-Kaskade WP → Puffer → Kessel → Heizkreis

> ⚠️ **DIE ZAHLEN DIESES ABSCHNITTS SIND ERSETZT.** Sie beruhen auf dem Fehler **E-K1-1**
> (Kessel überschreitet seine Nennleistung), den Review 1 gefunden hat. Der gültige Stand
> steht in Abschnitt 9, „Neu gemessen: Szenario (d)". Die Zahlen bleiben hier stehen, damit
> der Unterschied nachvollziehbar bleibt — als Beleg taugen sie nicht mehr.

Kessel 11205 `WQ_Typ = Pufferspeicher`, `WQ_ID_Puffer = 1018023`; `Tab_Heizkessel` 70/50 °C.
Protokollzeile des Laufs: *„der Puffer trägt 75 % der Nutzwärme"* — `(65 − 50)/(70 − 50)`.

| Größe | ohne Quellbezug | **mit Quellbezug** |
|---|---|---|
| Kessel, brennstoffbasierte Nutzwärme | 66,8924 MWh | 200,7783 MWh |
| Kessel, Wärme aus dem Quellpuffer | 0 | **83 554,5 kWh** |
| Kessel, Abgabe an den Kanal gesamt | 66,8924 MWh | 284,3328 MWh |
| **Gasverbrauch** | 78,9886 MWh | 230,3667 MWh |
| **Brennstoff je gelieferter MWh** | **1,1809** | **0,8102 (−31,4 %)** |
| Jahresnutzungsgrad | 84,69 % | 87,16 % (keine Kappung) |
| Heizstab | 87 918,6 kWh | **22 323,9 kWh** |
| WP-Produktion | 109 993,2 kWh | 130 000,3 kWh |

Der nominale Puffer-Anteil ist 75 %, der tatsächliche 83 554,5 / 284 332,8 = **29,4 %**: In
vielen Stunden hat der 13,9-kWh-Puffer die vollen 75 % nicht liefern können, und für den
Fehlbetrag ist Brennstoff eingesprungen — genau wie vorgesehen.

**Energieerhaltung, exakt nachgerechnet:**

```
Puffer-Zufuhr   87 868,6 (Umsatz) + 42 131,7 (Durchsatz) = 130 000,3 kWh = WP-Produktion
Puffer-Abgabe   87 642,6          + 42 131,7             = 129 774,3 kWh
                = 46 219,8 (Heizkanal) + 83 554,5 (Kessel)  ✓
Verluste 226,0 · SOC am Jahresende 0,000 · Abschlüsse 8760 · Bilanzrest 2,75 · 10⁻⁹ kWh
Herkunft:  WP.Speicherentladung_Anteil 129 774,3 = 46 219,8 + 83 554,5  — EXAKT
```

**Reihenfolge belegt:** Der Kessel konnte in denselben Stunden 83,6 MWh aus dem Puffer
ziehen, in denen die Wärmepumpe ihn lädt. Auf Ebene 0 — also vor der Ladephase der
Wärmepumpe — wäre der Puffer aus der Vorstunde bereits leergefahren gewesen.

> **Modellentscheidung zur Kesselleistung.** ⚠️ **ERSETZT** — die Entscheidung bleibt, die
> Begründung war falsch geführt: Sie verglich eine Variante, die die Nennleistung einhielt,
> mit einer, die sie brach (E-K1-1). Der neu gemessene Vergleich steht in Abschnitt 9,
> „Die Variantenentscheidung, neu geführt".
>
> Ursprünglicher Wortlaut: *Die Nennleistung begrenzt den **eigenen,
> brennstoffbasierten** Beitrag; die Pufferwärme kommt obendrauf
> (`Q_nutz(max) = P_nenn / (1 − Anteil)`). Die zuerst umgesetzte Variante „Nennleistung
> begrenzt die ganze Abgabe" ist gemessen und **verworfen**: Sie ersetzt einen Teil der
> eigenen Wärme durch Pufferwärme, die ohnehin in den Kanal geflossen wäre, und die
> Gesamtdeckung des Projekts **sank** durch das Einschalten der Kaskade um **44,6 MWh**
> (Gas 78,99 → 36,76 MWh, aber Heizstab und Deckung entsprechend schlechter). Eine
> Begrenzung nach Massenstrom und Wärmeübertrager kennt das Modell an keiner Stelle — auch
> der Speicher hat keine Lade-/Entladeleistung (vorgemerkter Parameter, Konzept 3.4).*

### 5-2 — was die Verzahnung löst und was nicht

> ⚠️ **DIE DRITTE ZEILE DIESER TABELLE IST ERSETZT.** Die dort gemessene Konfiguration —
> Kessel mit Zweitsenke **und** Quellbezug auf **denselben** Puffer — ist der Kurzschluss
> „Quelle = eigene Senke" aus Konzept 4.6, den Review 1 als **E-K2-1** aufgedeckt hat. Seit
> der Nacharbeit weist die Engine ihn ab. Die gültigen Zahlen stehen in Abschnitt 9,
> „Neu gemessen: 5-2 und 5-2q".

| Konfiguration | WP-Ladung | Δ |
|---|---|---|
| Basis (Kessel ohne Puffer-Senke) | 109 993,2 kWh | — |
| **5-2-Fall**: Kessel mit **Zweitsenke** auf 1018023, kein Quellbezug | **102 381,6 kWh** | **−7,6 MWh** |
| ⚠️ derselbe Kessel **zusätzlich mit Quellbezug** auf 1018023 (Kurzschluss, **entwertet**) | ~~124 087,0 kWh~~ | ~~+21,7 MWh~~ |

Die mittlere Zeile ist **auf die Stelle genau der Wert aus dem Paket-5-Protokoll**
(102 381,6 kWh). Das ist die ehrliche Antwort auf die Frage „löst die Verzahnung 5-2?":

* **Ohne Quellbezug: nein — und zwar mit Absicht.** Der Kessel des 5-2-Falls hat keinen
  Quellpuffer, steht damit auf Ebene 0, die Schleife läuft einmal, und der Rumpf ist
  Anweisung für Anweisung der bisherige. Die Ebenenauflösung greift an den Quellbezügen an,
  nicht an der Kaskadenposition; die vollständige Verzahnung der Phasen B und C nach
  Kaskadenposition bleibt die offene Konzeptfrage, als die Paket 5 sie beschrieben hat.
* ⚠️ **ENTWERTET — „Mit Quellbezug: ja, und mehr als das."** Ursprünglicher Wortlaut: *Der
  Kessel rechnet auf Ebene 1, also **nach** der Ladephase der Wärmepumpe. Er nimmt dem
  Puffer den Durchsatz nicht mehr weg, sondern bezieht seine Wärme aus ihm: Die WP-Ladung
  steigt um 21,7 MWh über den 5-2-Wert und um 14,1 MWh über die Basis, der Heizstab fällt
  von 47 850,8 auf 22 323,9 kWh.* — Die Aussage stimmt der Richtung nach und ist in
  Abschnitt 9 an einer **zulässigen** Konfiguration neu gemessen; die hier genannten Zahlen
  stammen aus dem Kurzschlussfall und aus dem überhöhten Kessel.

⚠️ **ENTWERTETE Herkunftsprobe** (Kurzschlussfall, siehe oben) — die neue steht in
Abschnitt 9:

```
Puffer-Abgabe   111 531,4 + 44 383,8 = 155 915,2 kWh
                = 48 770,7 (Heizkanal) + 107 144,4 (Kessel)          ✓
Kessel-Ladung in den Puffer: 32 107,5 kWh, davon eigen 8 026,9 (25,0 %) — der Rest
                ist umgebuchte Pufferwärme (Anteil_Umbuchen), nicht Kesselwärme
zugerechnete Deckung: WP 123 899,2 + Kessel 7 935,3 = 131 834,5 kWh
                    = 48 770,7 + 83 063,8 (Quellentnahme für Direktdeckung)   ✓
```

### (e) Booster — WP 2 mit Heizungspuffer als Quelle lädt den Warmwasserpuffer

WP 11203 → Puffer 1018023 (Heizung 65/45); WP 11204 (Sole-Wasser) Quelle = 1018023, Senke =
Puffer 1018024 (Brauchwasser 60/45, `Q_max` 13,537 kWh). Quelltemperatur 55 °C
((65 + 45)/2, Vorrangkette aus `WaermequelleClass.Quelltemperatur`).

Protokollzeile: *„Puffer 1018023 ist WÄRMEQUELLE der Anlage 11204 und zugleich Senke eines
anderen Erzeugers. Beide rechnen auf DERSELBEN Speicherinstanz (1 Modulbezug umgestellt)."*

| Größe | ohne Quellbezug | **Booster** |
|---|---|---|
| WP-Produktion gesamt | 117 325,8 kWh | **137 541,4 kWh** |
| Ladung Puffer 1018023 | 63 387,3 kWh | 77 116,2 kWh |
| Quellentnahme der WP 2 aus 1018023 | 0 | **39 199,6 kWh** |
| Deckung Heizkanal (1018023) | 64 006,5 kWh | 40 875,3 kWh |
| Deckung Warmwasser (1018024) | 52 360,1 kWh | **56 594,7 kWh** |
| Abschlüsse je Speicher | 8760 / 8760 | 8760 / 8760 |
| Bilanzreste | −6,04 · 10⁻⁹ / 3,87 · 10⁻⁹ | −4,76 · 10⁻¹⁰ / 4,57 · 10⁻⁹ |

Herkunft exakt: `WP.Speicherentladung_Anteil` 97 470,0 = 40 875,3 + 56 594,7 — die über die
Kaskade weitergereichte Wärme wird **nicht** doppelt gezählt; sie wandert per
`Anteil_Umbuchen` mitsamt ihrer Herkunft in den Warmwasserpuffer und wird dort einmal als
Deckung ausgewiesen.

**Das war vor D5a nicht lauffähig.** `WaermequelleClass.Quellspeicher` baut je Modul eine
EIGENE Instanz auf, die **voll** startet (`SOC = Q_max`). Zeigt `WQ_ID_Puffer` auf einen
Puffer, der schon als Senke in der Registry steht, war der Schlüssel belegt, und die
Quell-Instanz lief als „Zusatzspeicher" mit **getrennter Bilanz** neben dem echten Speicher —
also Wärme, die niemand erzeugt hat, und zwei Zeilen für denselben Puffer in
`Tab_ErgebnisPufferspeicher`. Die Kaskaden-Auflösung in `QuellspeicherUebernehmen` ersetzt
die Instanz; der Kurzschlussfall „Quelle = **eigene** Senke derselben Anlage" (Konzept 4.6)
bleibt davon ausgenommen und warnt weiter wie bisher.

### (f) Zyklus — sauberer Abbruch

WP 11203 lädt Puffer 1018023 und bezieht aus 1018022; WP 11204 lädt 1018022 und bezieht aus
1018023.

```
Ergebnis-Kopf: −1   (kein Ergebnis gespeichert)
FEHLER: Kaskade: Die Quellbezüge der Pufferspeicher bilden einen RING — eine Anlage lädt
        einen Speicher, aus dem sie über weitere Erzeuger wieder ihre eigene Quellwärme
        bezieht. Damit gibt es keine Rechenreihenfolge, in der jeder Erzeuger nach seinem
        Puffer rechnet; der Lauf bricht ab. Beteiligt: Anlage 11204 (Quelle: Puffer 1018023
        „Vitocell 140-E 600 Ltr"). Bitte die Wärmequelle einer dieser Anlagen ändern.
```

Der Abbruch läuft über `SimulationProtokoll.Fehlermeldung` und
`SimulationControl.FehlertextAufnehmen` in denselben Kanal wie alle übrigen Abbrüche; die
Engine bleibt dialogfrei (Konzept 13.4).

---

## 5. Altpfad-Regeln

Der einkanalige Rechenweg bleibt **unverändert** (Byte-Gleichheit, Abschnitt 3) und sagt
beim Lauf, was die beiden Erweiterungen dort bedeuten (`AltpfadHinweiseD5a`, HINWEIS-Kanal,
je Projekt einmalig):

* **Kombispeicher → wie „Heizung".** Der Altpfad kennt nur EINEN Bedarfsvektor; zwischen
  zwei Kanälen ist dort nichts aufzuteilen. Über `IstBrauchwasserkanal = false` bekäme der
  Kombispeicher diese Behandlung ohnehin — der Hinweis macht die **dokumentierte
  Vereinfachung** sichtbar, statt sie stillschweigend geschehen zu lassen. Auf einer
  Bedarfssumme ist „Heizung + Warmwasser aus einem Vorrat" genau das, was passiert.
* **Kessel-Quellbezug → unwirksam.** Eintrittstemperatur aus einem Puffer verlangt eine
  gemeinsame Speicherstufe mit Rechenreihenfolge; beides gibt es nur zweikanalig. Der Kessel
  rechnet mit vollem Brennstoffbedarf wie bisher.
* Ein Ziel `WS_Ziel = PufferKombi` an einer Wärmepumpe nimmt — wie schon
  `PufferBrauchwasser` — über `WpSenkeSpiegeln` die Alt-Zuordnung zurück; der Dialog weist
  darauf hin (`BrauchwasserUebergangsHinweis`, jetzt über `IstBrauchwasserseitig` auch für
  das Kombi-Ziel).

---

## 6. Katalog-Kandidaten (Nachtrag erfolgt zentral)

> ✅ **ERLEDIGT mit der Nacharbeit** (Befund I-K1-2): Alle fünf Texte stehen im
> Ressourcenkatalog, die Festtexte im Code sind entfallen. Siehe Abschnitt 9.1 und den
> D5a-Nachtrag in [`Lokalisierung_Katalog.md`](Lokalisierung_Katalog.md). Die Tabelle
> unten bleibt als Herleitung der Schlüsselnamen stehen.

Die Ressourcendateien (`MyResource/*.resx`, `Resource.Designer.cs`) waren während D5a vom
parallel laufenden D2/D3-Paket belegt. Die folgenden fünf Texte stehen deshalb als deutsche
**Festtexte mit `TODO Katalog-Nachtrag`** im Code und gehören in den Katalog:

| Vorschlag Schlüssel | Text (de) | Fundstelle |
|---|---|---|
| `SIM_ZIEL_PUFFERSPEICHER_KOMBI` | „Pufferspeicher Kombi (Heizung + Warmwasser)" | `WaermesenkeClass.KOMBI_ZIEL_TEXT` |
| `SIM_PUFFER_KOMBI_KURZ` | „Puffer Kombi" | `WaermesenkeClass.KOMBI_KURZ_TEXT` |
| `PSP_VERWENDUNG_KOMBI_ANZEIGE` | „Kombi (Heizung + Warmwasser)" | `WaermesenkeClass.KOMBI_VERWENDUNG_TEXT` |
| `SIM_RB_PUFFER_KOMBI` | „Puffer Kombi (Heizung + Warmwasser)" | `Form_Waermesenke.TXT_RB_PUFFER_KOMBI` |
| `SIM_LBL_HINWEIS_KOMBI` | „Ein Kombispeicher deckt Heizung und Warmwasser aus einem gemeinsamen Wärmevorrat. Reicht er in einer Stunde nicht für beides, wird zuerst Warmwasser bedient." | `Form_Waermesenke.TXT_HINWEIS_KOMBI` |

Die Meldungen der Engine laufen wie in Paket 8 vorgesehen als deutsche Klartexte über
`SimulationProtokoll` (Hinweis-, Warnungs- und Fehlerkanal): Kessel-Kaskade eingerichtet,
Kessel-Kaskade ohne Temperaturpaar, Quelle zu kalt, Kaskade auf gemeinsamer Speicherinstanz,
Ring der Quellbezüge, Altpfad-Kombispeicher, Altpfad-Kessel-Quellbezug.

---

## 7. Offene Punkte für D5b

> ⚠️ **FORTGESCHRIEBEN in Abschnitt 9.16.** Punkt 1 (Kombi-Option der Puffer-Verwaltung)
> ist mit der Nacharbeit **vorgezogen und erledigt**; vier Punkte sind dazugekommen. Die
> Liste unten bleibt als Stand der Etappe stehen.

1. **Puffer-Verwaltung braucht die Kombi-Option.** `Form_PufferSp_Projekt` (D2/D3-Hoheit)
   bietet in der Verwendungs-Auswahl nur „Heizung" und „Brauchwasser". Der Senken-Dialog
   gibt beim Absprung „Pufferspeicher anlegen…" bereits `Verwendung = "Kombi"` vor — die
   Verwaltung kann den Wert noch nicht anbieten. **Ohne diesen Schritt lässt sich ein
   Kombispeicher nur über die Datenbank anlegen.**
2. **Freischaltung der Quellen-Spalte je `ID_Type`** (Konzept Abschnitt 4): WP alle
   Quellentypen, Heizkessel nur `Pufferspeicher`, Erdsonde/Erdreich bleibt WP-exklusiv. Die
   Engine ist vorbereitet (`ErzeugerMitPufferQuelle`, `KesselQuellbezugSetzen`); die
   UI-Freischaltung gehört zu `Form_Simulation_Config*` und damit zu D2/D3/D5b.
3. **Dialog-Zyklusprüfung.** Die Engine bricht bei einem Ring ab (Szenario f). Der Dialog
   soll die Konfiguration gar nicht erst speichern lassen — Ableitung über die
   Kaskadenkette, Konzept Abschnitt 7.
4. **`WQ_ID_Puffer` als einzige Quellpuffer-Identität** (Etappe E0): `Form_QuellePufferspeicher`
   schreibt weiterhin nur den Bezeichner `WQ_Puffer`. Für die Kaskade ist `WQ_ID_Puffer` die
   führende Spalte — sie musste in den Szenarien per SQL gesetzt werden.
5. **Anzeige der Kaskade** (D3/D4): „Quelle für" auf der Speicherkarte, Kaskadenketten-Band,
   Temperatur-Warnregel. Die Engine liefert die Daten (`Kaskadenkontext.QuellpufferJeAnlage`,
   `SimulationSPK.QuellSpeicher/QuellAnteil`, `Ladeauftrag.Ebene`).
6. **Ergebnisspalte für die Quellwärme des Kessels.** `SimulationSPK.Quellwaerme_gesamt` und
   `Quellwaerme_stuendlich` stehen im Rechenkern bereit; `Tab_ErgebnisHeizkessel` hat keine
   Spalte dafür. Gleiche Familie wie die vorgemerkte Spalte `Speicherladung` (5-1d).
7. **Vollzyklen des Kombispeichers.** `KennzahlenBerechnen` bezieht sie auf `Ladung_gesamt`;
   für einen Speicher, der beide Kanäle bedient, ist das richtig, aber die Kennzahl wird
   groß (Szenario a: 6643). Das ist der bekannte Durchsatz-Effekt aus Befund N6 an einem
   kleinen Speicher, kein neuer Befund.

---

## 8. Reproduktion

```powershell
$msb = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

# 1. Arbeitsbaum auf 6c47a32 + die zwoelf D5a-Dateien, Referenzlauf dort bauen
& $msb <Arbeitsbaum>\Referenzlauf\Referenzlauf.csproj -t:Restore -p:Configuration=Debug -p:Platform=x86
& $msb <Arbeitsbaum>\Referenzlauf\Referenzlauf.csproj            -p:Configuration=Debug -p:Platform=x86

# 2. Migrierte Kopie (Produktiv-DB nur LESEN, vorher Kenndaten.laccdb pruefen)
Referenzlauf.exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb <dev>\DB_Basis

# 3. A/B: HEAD-DLL und D5a-DLL gegen DIESELBE Kopie, beide Flagstellungen
foreach ($id in 1007,1008,1011,1017,1018,1021,1023,1024) {
    Referenzlauf.exe projekt $id <Ziel>\Projekt_$id <dev>\DB_Basis      # Flag AUS
    Referenzlauf.exe projekt $id <Ziel>\Projekt_$id <dev>\DB_Flag       # Flag AN
}
Referenzlauf.exe vergleich <Ziel_HEAD> <Ziel_D5A>
# zusaetzlich rekursiver MD5-Vergleich beider Ordner

# 4. Szenarien: dev\szenarien.ps1 <S_a|S_c|S_d|S_d0|S_e|S_e0|S_f|S_52|S_52q>
#    (32-bit PowerShell + ACE, arbeitet ausschliesslich auf DB-Kopien)
#    Auswertung: dev\Probe\ProbeD5a.exe <dbOrdner> 1023 [vonStunde bisStunde]
```

Der Arbeitsbaum, die Datenbankkopien und das Prüfprogramm sind Wegwerf-Material und nach der
Abnahme gelöscht; die Zahlen dieses Protokolls sind der Beleg.

---

## 9. Nacharbeit nach den Reviews (16.08.2026)

Zwei adversariale Reviews haben den uncommitteten D5a-Stand geprüft:
**Review 1 — Engine/Fachlogik** und **Review 2 — Regressions- und Integrationssicherheit**.
Beide Befundlisten sind vollständig abgearbeitet. Dieser Abschnitt führt je Befund die
Fix-Fundstelle, danach **alle neu gemessenen Zahlen**; die durch die Fixes entwerteten
Zahlen der Abschnitte 4 und 5 sind dort als ersetzt markiert und stehen weiter da.

**Codestand der Nacharbeit:** Haupt-Checkout `C:\Waermeplan\WP_Plan`, HEAD **23dd3bc**,
Working Tree = D5a + Nacharbeit. **Nichts committet.** Build: 0 Fehler, exakt die sechs
Bestandswarnungen (`WErzeugerModel` CS0108, `StromverbraucherStammCtrl` CS0108,
`KlimaregionStammCtrl` CS0109 x2, `MDIMainForm` CS4014 + CS1998).

---

### 9.1 Fixes je Befund

Zeilennummern nach dem Stand der Nacharbeit.

#### Engine (Review 1)

| Befund | Fix | Fundstelle |
|---|---|---|
| **E-K1-1** Kessel überschreitet seine Nennleistung | `MaxAbgabe` bildet **zwei** Schranken und nimmt die kleinere: `P_rest/(1−Anteil)` (der Puffer liefert wie gerechnet) und `P_rest + Inhalt` (der Puffer liefert weniger, Brennstoff deckt den Rest). Damit gilt `eigen ≤ _restLeistung` in jedem Fall. Dazu je eine Gleitkomma-Klemmung an den beiden Buchungsstellen. | `SimulationSPK.cs:563-584` (`MaxAbgabe`), `:800-805` (`Stunde_Bedarf`), `:872-877` (`Zweikanalig_Laden`); Blockkommentar `:451-467` |
| **E-K1-2** Flag AUS mit Kombispeicher nicht unverändert | Die Verwendung `Kombi` wird im Registry-Aufbau **nur im zweikanaligen Weg** gesetzt; im Altpfad steht durchgehend `Heizung`, wie `AltpfadHinweiseD5a` es zusagt. Dafür steht die Auswertung des Feature-Flags jetzt **vor** dem Registry-Aufbau (der Protokollhinweis bleibt an seiner Stelle, damit die Reihenfolge im Kanal unverändert ist). | `SimulationControl.cs:294-306` (Flag vorgezogen), `:2143-2160` (`SpeicherRegistryAufbauen`, Block 1) |
| **E-K1-3** WP mit Kaskaden-Quellpuffer weist Deckung doppelt aus | Die Direktdeckung wird um die tatsächlich aus dem Quellpuffer entnommene Wärme gekürzt — genau der Betrag, den `Anteil_Entladen` dem Lader des Puffers gutschreibt. Der klassische Quellspeicher (`IstQuelle`) bleibt ausgenommen: Er trägt keine Herkunftsanteile, dort wäre der Abzug verlorene Deckung. | `SimulationWaermepumpe.cs:1192-1226` |
| **E-K2-1** kein Kurzschluss-Guard für den Kessel | `QuellbezuegeAufbauen` weist „Quelle = eigene Senke" (Konzept 4.6) für **alle** Arten ab, bevor der Quellbezug entsteht — Warnkanal, Bezug wirkungslos. Der WP-Pfad behält zusätzlich seine bisherige Warnung aus `QuellspeicherUebernehmen`. | `SimulationControl.cs:2478-2489` |
| **E-K2-2** Rechenebenen für Arten ohne Modulmaske | `QuellbezuegeAufbauen` nimmt nur noch `TYP_WP` und `TYP_KESSEL` auf; jede andere Art bekommt eine Warnung und bleibt auf Ebene 0. Damit kann `BedarfsordnungJeEbeneBilden` die Art BHKW nicht mehr auf zwei Ebenen setzen. | `SimulationControl.cs:2465-2476` |
| **E-K2-3** `WP.Kanalbedarf` nicht Kombi-fähig | `Kanalbedarf` liefert beim Kombispeicher die **Summe beider Kanäle** — dieselbe Regel wie `Kaskadenschleife.DurchlassBudget`. Wirkt über `Bilanzraum` **und** über `Ladefaehig`, also auf Bezugsgröße und Abbruchbedingung zugleich. | `SimulationWaermepumpe.cs:1296-1315` |
| **E-K2-4** `_kesselInSchleife` breiter als der Quellbezug | `ErzeugerMitPufferQuelle` verlangt `WQ_ID_Puffer > 0` **und** einen existierenden Puffer **dieses Projekts**. Zusätzlich meldet `KesselQuelleOhneWirkungMelden` nach dem Aufbau, wenn der Kessel allein wegen der Quelle in der Schleife steht, aber kein Quellbezug entstanden ist. | `SimulationControl.cs:837-880` (`ErzeugerMitPufferQuelle`), `:552-563` (Schleifenkriterium + `_kesselNurWegenQuelle`), `:2510-2531` (Meldung) |

#### Integration und Oberfläche (Review 2)

| Befund | Fix | Fundstelle |
|---|---|---|
| **I-K1-1** Hinweislabel abgeschnitten | Die Labelhöhe wird mit `TextRenderer.MeasureText` **gerechnet** (gleiche Schrift, gleiche Breite wie beim Zeichnen), Trenner, Knöpfe und `ClientSize` hängen am Ergebnis. Bei einem 56-px-Hinweis kommt exakt das alte Raster heraus. | `Form_Waermesenke.cs:341-424` |
| **I-K1-2** Sprachbruch auf englischer Oberfläche | Die fünf Festtexte sind im Katalog (`Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs`); die `TODO Katalog-Nachtrag`-Konstanten sind ersatzlos entfallen. Nachtragsabschnitt in `Lokalisierung_Katalog.md`. | `WaermesenkeClass.cs:87-88/598/862`, `Form_Waermesenke.cs:190/301/357`, `Form_PufferSp_Projekt.cs:196-207` |
| **I-K2-1** BHKW-Pendelspeicher nur im Heizkanal | Einsortierung je Kanal über `BedientKanal`; `EntladeordnungEinsortieren` bekommt den Kanal als Parameter, statt ihn aus `IstBrauchwasserkanal` abzuleiten. | `SimulationControl.cs:1394-1403`, `:1443-1452` |
| **I-K2-3** doppelter `<summary>`-Block | `ErzeugerMitPufferQuelle` steht mit eigenem Kopf **hinter** `ErzeugerMitPufferSenke`; dessen Dokumentation ist wieder an seiner Signatur. | `SimulationControl.cs:816-880` |
| **I-K2-4** Puffer-Verwaltung setzt Kombi still auf Heizung zurück | Dritte, reguläre `VerwendungItem`-Option mit DB-Wert `DbWerte.PSP_VERWENDUNG_KOMBI` und Anzeige `PSP_VERWENDUNG_KOMBI_ANZEIGE`; die Vorbelegung aus dem Senken-Dialog kommt an; die Positionsanzeige zeigt den Kombi im **Heizkanal**, in dem er ebenfalls steht. **Das ist der erste D5b-Punkt, vorgezogen** (siehe 9.16). | `Form_PufferSp_Projekt.cs:196-207`, `:472-480`, `:690-696` |
| **I-K2-5** Zeilenenden | `DbWerte.cs`, `Ladeordnung.cs`, `WaermesenkeClass.cs`, `Form_Waermesenke.cs` byte-genau auf CRLF zurückgestellt (Kodierung und BOM unangetastet). | — |
| **I-K3** `KesselTemperaturpaar` über den Bezeichner | Stufe 1 der Vorrangkette läuft über `Tab_Energieanlagen.ID_Kessel` → `Tab_Heizkessel.ID`. | `SimulationControl.cs:2553-2578` |

**Mitgenommen aus den K3-Listen:** Review 2 K3-6 (zusätzliche DB-Abfrage in jedem Lauf)
entfällt im Altpfad als Nebenwirkung von E-K1-2 — die Bedingung schließt jetzt kurz, bevor
`PufferLesen` gerufen wird.

---

### 9.2 Regression — Flag AUS und Flag AN

Alle Läufe auf einer eigenen, vollständig migrierten Kopie der produktiven
`Kenndaten.accdb` (**nur gelesen**, keine `Kenndaten.laccdb` vorhanden); Projektmenge
1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024.

| Lauf | Vergleich | Ergebnis |
|---|---|---|
| **Flag AUS** | eingefrorene Basis `Referenzlaeufe/2026-08-15_B3` gegen die Nacharbeit | **8/8 PASS, 2 094 447 Werte, 190/190 Dateien byte-/MD5-gleich** |
| **Flag AN**, unpräpariert (alle acht Projekte `Kaskade_Zweikanalig = True`) | HEAD `23dd3bc` gegen die Nacharbeit, **dieselbe** Kopie | **190/190 byte-/MD5-gleich** |

Die Regressionszusage der Etappe steht damit unverändert — auch nach sieben
Engine-Eingriffen.

---

### 9.3 Flag AUS mit Kombispeicher — der Beleg zu E-K1-2

Zwei identische Wegwerf-Kopien, Projekt 1023, `Kaskade_Zweikanalig = FALSE`:
**Lauf A** mit `Tab_Pufferspeicher.Verwendung = 'Kombi'` an Puffer 1018023,
**Lauf B** mit `'Heizung'` an demselben Puffer.

| Prüfung | Ergebnis |
|---|---|
| A gegen B, alle Ergebnisdateien | **25/25 byte-/MD5-gleich** |
| Altpfad-Hinweis „Kombispeicher … wird wie ein HEIZUNGSPUFFER behandelt" | feuert **genau einmal** |
| Puffer 1018023 in Lauf A | vorhanden, Verwendung `Heizung`, Ladung 13 244,5 / Entladung 12 997,3 / Verluste 247,2 kWh, **8760 Abschlüsse**, Bilanzrest −1,7 · 10⁻¹⁰ kWh |

**Der Zustand vor dem Fix, gemessen.** Eigens gebaute Variante ohne die
`KaskadeZweikanalig`-Bedingung, gleiche Kopie:

| Größe | ohne Fix | **mit Fix** |
|---|---|---|
| Speicher im Lauf (`AlleSpeicher()`) | **keiner** | Puffer 1018023 |
| `Tab_ErgebnisPufferspeicher` | **leer** | eine Zeile |
| WP-Produktion | 137 275,5 kWh | 138 151,1 kWh |
| Heizstab | 62 812,6 kWh | 62 223,2 kWh |

Ohne den Fix rechnet der Altpfad also tatsächlich **ohne Speicher** — genau die
Ergebnisänderung, die Review 1 beschrieben hat.

---

### 9.4 Neu gemessen: Szenario (d) — Kessel-Kaskade

Projekt 1023, Flag AN, Kessel 11205 `WQ_Typ = Pufferspeicher`, `WQ_ID_Puffer = 1018023`,
`Tab_Heizkessel` 70/50 °C → Anteil `(65 − 50)/(70 − 50) = 75 %`. Je eigene Kopie.

| Größe | ohne Quellbezug | **mit Quellbezug (NEU)** | ~~alt, entwertet~~ |
|---|---|---|---|
| Kessel, brennstoffbasierte Nutzwärme | 66 892,4 kWh | **86 235,0 kWh** | ~~200 778,3 kWh~~ |
| Kessel, Wärme aus dem Quellpuffer | 0 | **83 554,5 kWh** | 83 554,5 kWh |
| Kessel, Abgabe an den Kanal gesamt | 66 892,4 kWh | **169 789,5 kWh** | ~~284 332,8 kWh~~ |
| **Gasverbrauch** | 78,9886 MWh | **99,3103 MWh** | ~~230,3667 MWh~~ |
| Jahresnutzungsgrad | 84,69 % | **86,83 %** | ~~87,16 %~~ |
| Heizstab | 87 918,6 kWh | **48 440,1 kWh** | ~~22 323,9 kWh~~ |
| WP-Produktion | 109 993,2 kWh | 130 000,3 kWh | 130 000,3 kWh |
| **Restwärmebedarf des Projekts** | **125 280,31 kWh** | **125 280,31 kWh** | ~~nicht gemessen~~ |

**Die Nennleistung wird eingehalten — stündlich und übers Jahr:**

```
Stundenvektor Kesselleistung_stuendlich (= brennstoffbasierte Nutzwaerme je Stunde):
    Maximum          19,3000 kWh/h      = P_nenn, auf die Stelle
    Stunden > P_nenn 0   von 8760
    Jahressumme      86 234,99 kWh      Obergrenze P_nenn * 8760 = 169 068 kWh  (51,0 %)
_restLeistung wird nie negativ (MaxAbgabe haelt eigen <= _restLeistung; die Klemmung
faengt nur Gleitkommareste).
```

Zum Vergleich: der alte Stand wies **200 778,3 kWh** brennstoffbasierte Nutzwärme aus —
**18,8 % über** der theoretischen Jahresobergrenze eines 19,3-kW-Kessels.

**Energieerhaltung, exakt nachgerechnet** (unverändert gültig — der Fehler saß im
Brennstoffteil, nicht in der Speicherbilanz):

```
Puffer-Zufuhr   87 868,6 (Umsatz) + 42 131,7 (Durchsatz) = 130 000,3 kWh = WP-Produktion
Puffer-Abgabe   87 642,6          + 42 131,7             = 129 774,3 kWh
                = 46 219,8 (Heizkanal) + 83 554,5 (Kessel)  OK
Verluste 226,0 - SOC am Jahresende 0,000 - Abschluesse 8760 - Bilanzrest 2,77e-9 kWh
Herkunft:  WP.Speicherentladung_Anteil 129 774,3 = 46 219,8 + 83 554,5   EXAKT
```

**Und die Probe, die der alte Stand nicht bestanden hätte:** Die *tatsächlich gedeckte*
Wärme ist in beiden Spalten dieselbe, denn der Bedarf ist derselbe.

```
ohne Quellbezug : WP 109 993,2 + Kessel(brennstoff) 66 892,4 + Heizstab 87 918,6
                  = 264 804,3 kWh,  Restwaermebedarf 125 280,31
mit Quellbezug  : WP 130 000,3 + Kessel(brennstoff) 86 235,0 + Heizstab 48 440,1
                  = 264 675,4 kWh,  Restwaermebedarf 125 280,31   (Differenz = Speicherverluste)
```

Der alte Stand kam an derselben Stelle auf **353 102,5 kWh** — er hat durch das Einschalten
der Kaskade rund **88 MWh Wärme erzeugt, die niemand produziert hat.** Genau das ist mit
E-K1-1 weg.

**Kennzahl „Brennstoff je gelieferter MWh": bewusst gestrichen.** Ihr Nenner enthält die
Pufferwärme, die die *Wärmepumpe* mit Strom erzeugt hat (Review 1, K3-1) — sie misst nicht
die Güte des Kessels. Aussagekräftig ist der **gesamte Energieeinsatz bei gleicher Deckung**:

| | ohne Quellbezug | **mit Quellbezug** |
|---|---|---|
| Gas | 78,99 MWh | 99,31 MWh |
| WP-Strom | 45,43 MWh | 51,18 MWh |
| Heizstab (Strom) | 87,92 MWh | 48,44 MWh |
| **Summe** | **212,34 MWh** | **198,93 MWh** (**−13,41 MWh, −6,3 %**) |

---

### 9.5 Die Variantenentscheidung zur Kesselleistung, neu geführt

Die alte Begründung verglich die Variante „Nennleistung begrenzt die ganze Abgabe" mit
einer Fassung, die die Nennleistung **brach** — der Vergleich war damit wertlos. Beide
Varianten sind jetzt **gegen den korrigierten Stand** gemessen: eigens gebaute Variante
(`MaxAbgabe` gibt `_restLeistung` zurück), gleiche Kopie, Szenario (d).

| Größe | **gewählt:** Nennleistung begrenzt den EIGENEN Beitrag | verworfen: Nennleistung begrenzt die GANZE Abgabe |
|---|---|---|
| Kessel, brennstoffbasierte Nutzwärme | 86 235,0 kWh | 31 570,0 kWh |
| **höchste Stundenleistung des Brenners** | **19,300 kWh/h = P_nenn** | **7,586 kWh/h = 39 % von P_nenn** |
| Kessel, Abgabe gesamt | 169 789,5 kWh | 115 124,5 kWh |
| Gasverbrauch | 99,3103 MWh | 36,7645 MWh |
| Heizstab | 48 440,1 kWh | 58 524,8 kWh |
| **Restwärmebedarf des Projekts** | **125 280,31 kWh** | **169 860,62 kWh** |
| **Unterdeckung gegenüber der gewählten Variante** | — | **+44 580,3 kWh (44,58 MWh)** |

Beide Varianten halten die Nennleistung ein und beide schließen ihre Bilanz
(`Deckung + Rest = Bedarf + Speicherverluste`, bis auf 10⁻⁹ kWh). Der Unterschied ist ein
**fachlicher**, und er ist an der Zeile „höchste Stundenleistung" abzulesen: Die verworfene
Variante lässt den Brenner **nie über 39 % seiner Nennleistung** kommen, weil die Pufferwärme
von derselben Hülle abgezogen wird. Der Puffer verdrängt damit Brennerwärme, statt sie zu
ergänzen — und die Wärme, die der Brenner nicht mehr liefert, fehlt: **44,58 MWh** mehr
Restwärmebedarf im selben Projekt. Fachlich ist `P_nenn` die Leistung des **Brenners**, also
der Wärme, die er dem eintretenden Wasser *hinzufügt*; tritt das Wasser vorgewärmt ein,
addiert sich die Vorwärmung, sie ersetzt den Brenner nicht.

Der Zahlenwert der alten Begründung (−44,6 MWh) trifft also zu — die **Grundlage** war
falsch, das Ergebnis nicht. Der Vergleich ist damit erstmals belastbar geführt.

Die naheliegende Gegenfrage „aber die verworfene Variante braucht doch weniger Energie
(146,47 MWh gegen 198,93 MWh)?" beantwortet sich mit derselben Zeile: Sie deckt 44,58 MWh
**weniger** Bedarf. Bei gleicher Deckung verschwindet der Vorteil.

---

### 9.6 Neu gemessen: 5-2 und 5-2q

| Konfiguration | WP-Ladung | Δ zur Basis |
|---|---|---|
| Basis (Kessel ohne Puffer-Senke, ohne Quellbezug) | 109 993,2 kWh | — |
| **5-2-Fall**: Kessel mit **Zweitsenke** auf 1018023, kein Quellbezug | **102 381,6 kWh** | **−7,6 MWh** |
| ~~5-2q alt: derselbe Kessel zusätzlich mit Quellbezug auf **denselben** Puffer~~ | **abgewiesen** | — |
| **5-2q neu**: Kessel mit **Quellbezug** auf 1018023 statt Zweitsenke (= Szenario (d)) | **130 000,3 kWh** | **+20,0 MWh**, **+27,6 MWh** gegenüber 5-2 |

Die zweite Zeile ist weiterhin **auf die Stelle genau der Wert aus dem Paket-5-Protokoll**
(102 381,6 kWh).

**Warum 5-2q alt abgewiesen wird.** Ein Kessel, der Puffer 1018023 als Zweitsenke lädt
**und** aus ihm entnimmt, ist der Kurzschluss „Quelle = eigene Senke" aus Konzept 4.6
(Befund E-K2-1). Seit der Nacharbeit meldet die Engine:

```
WARNUNG: Waermequelle Pufferspeicher: Die Anlage 11205 bezieht ihre Waerme aus Puffer
         1018023, den sie selbst als Senke laedt (Kurzschluss, Konzept 4.6). Sie wuerde
         Waerme im Kreis pumpen; der Quellbezug bleibt deshalb WIRKUNGSLOS. Bitte die
         Waermequelle oder die Waermesenke dieser Anlage aendern.
```

Der Lauf ist danach **Wert für Wert der 5-2-Fall** — der Quellbezug ist folgenlos, wie
angekündigt.

**Die Antwort auf „löst die Verzahnung 5-2?" bleibt dieselbe, jetzt sauber belegt:**

* **Ohne Quellbezug: nein — mit Absicht.** Unverändert gültig (Abschnitt 5-2 oben).
* **Mit Quellbezug: ja.** Der Kessel rechnet auf Ebene 1, also **nach** der Ladephase der
  Wärmepumpe. Er nimmt dem Puffer den Durchsatz nicht mehr weg, sondern bezieht seine
  Wärme aus ihm: Die WP-Ladung steigt von 102 381,6 auf **130 000,3 kWh**. Der Heizstab
  bleibt mit 48 440,1 gegen 47 850,8 kWh nahezu unverändert — die Gesamtdeckung ist
  dieselbe, es verschiebt sich nur ihre Herkunft (Abschnitt 9.4).

---

### 9.7 Neu gemessen: E-K1-3 — keine doppelte Deckung mehr

Die Konstellation, die Review 1 benannt hat und die Szenario (e) **nicht** trifft:
WP 11203 lädt Puffer 1018023; **WP 11204 hat 1018023 als Quelle und den HEIZKREIS als
Hauptsenke** (Projekt 1023, Flag AN, eigene Kopie).

| Größe | ohne Fix | **mit Fix** |
|---|---|---|
| `WP.Direktdeckung_gesamt` | 89 224,6 kWh | **28 146,6 kWh** |
| `WP.Speicherentladung_Anteil` | 78 474,1 kWh | 78 474,1 kWh |
| Heizstab | 71 488,2 kWh | 71 488,2 kWh |
| Kessel-Eigenanteil | 70 692,4 kWh | 70 692,4 kWh |
| **ausgewiesene Deckung gesamt** | **309 879,3 kWh** | **248 801,3 kWh** |
| **tatsächliche Deckung** (Bedarf 389 729,7 − Rest 140 928,4) | 248 801,3 kWh | 248 801,3 kWh |
| **Überzeichnung** | **+61 078,1 kWh (+24,5 %)** | **0,004 kWh** (Gleitkomma) |

Die Überzeichnung ist auf die kWh genau die Quellentnahme der WP 2 aus dem Puffer — sie
stand vorher zweimal in der Bilanz: einmal als Direktdeckung der WP 2 und einmal, über
`Anteil_Entladen`, als Speicherentladung der WP 1. Die Summenprobe schließt jetzt exakt:

```
ausgewiesen  WP (28 146,6 + 78 474,1 + 71 488,2) + Kessel 70 692,4 = 248 801,35 kWh
tatsaechlich Bedarf 389 729,72 - Rest 140 928,37                   = 248 801,35 kWh   OK
Herkunft     WP.Speicherentladung_Anteil 78 474,13
             = 17 396,07 (Heizkanal) + 61 078,06 (Quellentnahme)   = 78 474,13 kWh   OK
```

---

### 9.8 Neu gemessen: E-K2-2 — zwei BHKW, keine Doppelproduktion

Projekt 1018 (zwei BHKW, ein Heizkessel, ein Puffer), Flag AN, Puffer 1018007 auf
`Heizung` 70/50 und als **Senke des Heizkessels**; BHKW 10371 bekommt
`WQ_Typ = Pufferspeicher`, `WQ_ID_Puffer = 1018007` — also einen Quellbezug auf einen
Puffer, den ein anderer Erzeuger lädt. Genau die Konstellation, die vor dem Fix eine Ebene 1
für die Art BHKW erzeugt hätte.

| Prüfung | Ergebnis |
|---|---|
| Lauf **mit** Quellbezug gegen Lauf **ohne** Quellbezug, gleiche Kopie | **22/22 Dateien byte-/MD5-gleich** |
| Warnung „… ist weder Wärmepumpe noch Heizkessel … bleibt WIRKUNGSLOS" | feuert **genau einmal** |
| BHKW-Wärmeproduktion / Direktdeckung | 150 841,76 / 150 841,76 kWh — **gleich**, also kein zweiter Durchlauf |
| Deckungsprobe | Bedarf 185 166,0 − Rest 8,4 = 185 157,6 = BHKW 150 841,8 + Kessel-Speicherentladung 34 315,8 |

---

### 9.9 Neu gemessen: E-K2-4 — Kessel ohne echten Quellbezug rechnet wie HEAD

Projekt 1023, Flag AN, Kessel 11205 mit `WQ_Typ = 'Pufferspeicher'`, aber
`WQ_ID_Puffer = NULL` — der Altdatenrest, der den Kessel vorher still in die
Stundenschleife zog.

| Prüfung | Ergebnis |
|---|---|
| Nacharbeit gegen HEAD `23dd3bc`, gleiche Kopie | **25/25 Dateien byte-/MD5-gleich** |
| Protokollkanal | keine Kaskaden-Meldung, `QuellAnteil = 0` |

Und der Fall, in dem die ID zwar stimmt, der Bezug aber trotzdem nicht zustande kommt
(fehlendes Temperaturpaar) — dort meldet die Engine jetzt beides:

```
WARNUNG: Kessel-Kaskade: ... das Temperaturpaar fuer den Hub ist nicht bestimmbar ...
         Der Quellbezug bleibt WIRKUNGSLOS ...
WARNUNG: Kessel-Kaskade: Die Heizkessel dieses Projekts fuehren einen Pufferspeicher als
         Waermequelle, aber KEIN Quellbezug ist zustande gekommen ... Die Kessel rechnen
         deshalb ohne Kaskade - aber, weil die Quelle konfiguriert ist, innerhalb der
         gemeinsamen Speicherstufe statt als eigene Vektorstufe.
```

---

### 9.10 Neu gemessen: I-K3 — Temperaturpaar über die ID

Kopie von Szenario (d), zusätzlich ein **zweiter** `Tab_Heizkessel`-Satz **desselben
Projekts mit demselben Bezeichner** `ecoVIT VKK 186/5`, aber Temperaturpaar 40/30 und
kleinerer ID (1018252 gegen 1018254).

| Verknüpfung | gelesenes Paar | Anteil |
|---|---|---|
| über den Bezeichner (**vorher**) | 40/30 aus 1018252 | `(65−30)/(40−30) = 3,5` → auf **1,0** geklemmt |
| über `ID_Kessel` → `Tab_Heizkessel.ID` (**jetzt**) | 70/50 aus 1018254 | **0,75** — gemessen: `QuellAnteil = 0,75` |

---

### 9.11 Szenarien (a), (b), (c), (e), (f) — unverändert stabil

Alle mit dem Nacharbeitsstand neu gefahren, je eigene Kopie, Projekt 1023, Flag AN.
**Sämtliche Zahlen der Abschnitte 4(a) bis 4(f) sind Stelle für Stelle reproduziert.**

| Szenario | Prüfgröße | Ergebnis |
|---|---|---|
| **(a)** NUR-Kombi | Ladung / Entladung / Verluste / SOC | 92 473,98 / 92 142,26 / 331,72 / **0,000** kWh |
| | Durchsatz (auf/ab) | 45 791,60 / 45 791,60 kWh |
| | `StundeAbschliessen` | **8760 / 8760** |
| | größter Bilanzrest | **−1,085e−9 kWh** |
| | Entladung WW / HZ / Summe | 59 857,68 / 78 076,18 / **137 933,86 kWh** |
| | `WP.Speicherentladung_Anteil` | **137 933,86 kWh — exakt gleich** |
| | Stunden mit beiden Kanälen | **3977** |
| | Heizstab (gegen 87 918,6 mit `Verwendung = Heizung`) | **62 186,81 kWh** |
| **(b)** K-1 „Warmwasser zuerst" | echte Heizungsentladung vor echter Warmwasserentladung, selber Speicher/Stunde/Phase | **0 Verstöße** (18 745 Buchungen, davon 7862 Durchsatzrückgaben) |
| | Stundenprobe h = 8 | `DS/WW 6,0567 → 13,92 · EL/WW 3,1707 → 10,7493 · EL/HZ 10,7493 → 0` |
| | Stundenprobe h = 9 | `DS/WW 6,6944 → 13,92 · EL/WW 8,0695 → 5,8505 · EL/HZ 5,8505 → 0` |
| **(c)** Kombi + Heizungspuffer | Abschlüsse | 8760 / 8760 |
| | Bilanzreste | −4,101e−9 / −1,455e−10 kWh |
| | Entladung WW / HZ | 51 416,22 / 84 871,93 kWh |
| | Summe = `WP.Speicherentladung_Anteil` | 136 288,15 / **136 288,15 kWh** |
| | davon 1018022 (Heizung) / 1018023 (Kombi) | 54 846,64 / 81 441,51 kWh |
| | Reihenfolgeprobe Heizkanal (Prio 1 vor Prio 2) | 3476 Stunden/Phasen mit zwei Speichern, **0 Verstöße** |
| | Warmwasserkanal | ausschließlich aus dem Kombispeicher |
| **(e)** Booster | WP-Produktion ohne / mit Quellbezug | 117 325,77 → **137 541,43 kWh** |
| | Ladung 1018023 | 63 387,27 → 77 116,15 kWh |
| | Quellentnahme WP 2 aus 1018023 | 0 → **39 199,7 kWh** |
| | Deckung Heizkanal / Warmwasser | 40 875,30 / **56 594,74 kWh** |
| | Herkunft exakt | `WP.Speicherentladung_Anteil` **97 470,04 = 40 875,30 + 56 594,74** |
| | Abschlüsse / Bilanzreste | 8760 / 8760 · −4,76e−10 / 4,57e−9 |
| **(f)** Zyklus | Ergebnis-Kopf | **−1**, kein Ergebnis gespeichert |
| | Fehlertext | unverändert, über `SimulationProtokoll.Fehlermeldung` **und** `FehlertextAufnehmen` |

Die Stundenproben zu (b) lesen sich als `Phase/Art Menge → SOC danach`; `DS` ist die
Durchsatzrückgabe, `EL` die echte Entladung. Der Warmwasserbedarf ist jeweils vollständig
gedeckt (6,0567 + 3,1707 = 9,2274; 6,6944 + 8,0695 = 14,7639), der Rest geht auf die
Heizung, der Speicher wird leergefahren.

---

### 9.12 E-K2-3 — was gemessen ist und was nicht

Der Fix ist umgesetzt und in allen erreichbaren Läufen **ergebnisneutral**: Szenario (a)
und eine Variante mit einem 5000-l-Kombispeicher (`Q_max` 116 kWh) liefern mit und ohne
Fix **dieselben Zahlen** — geprüft gegen einen eigens gebauten Vergleichsstand ohne den
Kombi-Zweig in `Kanalbedarf`.

| Szenario | mit Fix | ohne Fix |
|---|---|---|
| (a), 600-l-Kombi: WW-only-Stunden / davon mit WP-Produktion | 2074 / **2074** | 2074 / 2074 |
| (a): Entladung WW / HZ | 59 857,68 / 78 076,18 | 59 857,68 / 78 076,18 |
| 5000-l-Kombi: WP-Produktion / Heizstab / Restwärmebedarf | 140 127,20 / 60 496,21 / 125 280,31 | identisch |

**Ehrlich benannt:** Die von Review 2 beschriebene *auslösende* Konstellation
(`rest_heiz = 0`, offener Warmwasserbedarf **und** Ladefähigkeit 0) ließ sich in Projekt 1023
nicht erzwingen — Phase E fährt den Kombispeicher in jeder Stunde mit offenem Bedarf leer,
und ein leerer Speicher ist wieder ladefähig. Der Fix bleibt trotzdem richtig und
notwendig: Er beseitigt die **innere Inkonsistenz**, die D5a selbst erzeugt hatte (Phase B
rechnete mit „nur Heizbedarf", Phase C über `DurchlassBudget` mit „Heiz + Warmwasser" — auf
demselben Speicher), und er wirkt über `Ladefaehig` auch auf die Abbruchbedingung. Ein
Beleg über eine gemessene Ergebnisänderung steht aus; er wäre mit einer Ladegrenze
(`WS_Ladegrenze`) unterhalb des Füllstands zu konstruieren und ist als Prüfpunkt für D5b
vermerkt.

---

### 9.13 Oberfläche — Geometrie und Sprache

Gemessen am **instanziierten Dialog** (`Form_Waermesenke`), nicht geschätzt:

| Prüfung | de-DE | en-US |
|---|---|---|
| Hinweistext | 273 Zeichen | 240 Zeichen |
| Labelfläche | 390 x **79** px | 390 x **79** px |
| benötigt (`TextRenderer.MeasureText`, gleiche Schrift/Breite) | 379 x **75** px | 381 x **75** px |
| Ergebnis | **vollständig sichtbar** | **vollständig sichtbar** |
| `ClientSize` | 620 x **641** px | 620 x 641 px |
| Fenstergröße (`FixedDialog`) | 636 x **680** px | 636 x 680 px |
| OK / Abbrechen | oben 595, unten 618 — **im Fenster** | dito |

Vor D5a waren es 592 px `ClientSize`, im ungefixten D5a-Stand 618 px mit abgeschnittenem
Text. **Auf einer 768-px-Arbeitsfläche passt der Dialog** (680 px Fensterhöhe gegen rund
728 px nutzbar). Unterhalb von etwa 690 px Arbeitsfläche wären OK/Abbrechen nicht mehr
erreichbar — dieselbe Grenze wie vorher, nur um 23 px verschoben (Review 2, K3-4).

Sprachprobe der fünf neuen Schlüssel und des Verwendungs-Dropdowns:

```
de-DE : Puffer Kombi (Heizung + Warmwasser) - Pufferspeicher Kombi (Heizung + Warmwasser)
        Puffer Kombi - Kombi (Heizung + Warmwasser)
        Dropdown: Heizung | Brauchwasser | Kombi (Heizung + Warmwasser)
en-US : Buffer combined (heating + DHW) - Buffer storage combined (heating + DHW)
        Buffer combined - Combined (heating + DHW)
        Dropdown: Heating | Domestic hot water | Combined (heating + DHW)
```

Roundtrip Anzeige ↔ DB-Wert der Puffer-Verwaltung (I-K2-4), über
`VerwendungWaehlen` / `GewaehlteVerwendung` gemessen:

```
'Heizung'      -> 'Heizung'
'Brauchwasser' -> 'Brauchwasser'
'Kombi'        -> 'Kombi'        verlustfrei
'kombi'        -> 'Kombi'        auf den Persistenzwert normalisiert
'Unsinn'       -> 'Heizung'      unveraendertes Bestandsverhalten (Rueckfall auf Index 0)
```

---

### 9.14 Kodierung und Zeilenenden

Alle 16 geänderten `.cs`/`.resx`-Dateien: **UTF-8 mit BOM, 0 x U+FFFD, ausschließlich
CRLF**. `git status` meldet keine Zeilenendenwarnung mehr; `git diff --stat` gegen
`23dd3bc` zeigt nur echte Änderungen.

---

### 9.15 Release-Notiz — Rückwärtskompatibilität (Review 2, K3-3)

> **Ein Projekt mit Kombispeicher darf nicht mit einer Fassung vor D5a geöffnet werden.**
>
> D5a legt **keine neue Spalte** an (Schemastand bleibt 9), sondern nur neue **Werte** in
> vorhandene Textspalten: `Tab_Pufferspeicher.Verwendung = "Kombi"` und
> `Tab_Energieanlagen.WS_Ziel`/`WS_Ziel2 = "PufferKombi"`. Damit fehlt jeder Marker, an dem
> eine ältere Installation merken könnte, dass sie die Datei nicht mehr versteht.
>
> Konkret in einer Vor-D5a-Fassung: `WaermesenkeClass.IstPufferZiel("PufferKombi")` ist
> dort `false`; `Normalisieren` setzt daraufhin still `Ziel = Heizkreis` und
> `ID_Puffer = 0`. Öffnet der Anwender den Senkendialog und bestätigt mit OK, wird das
> **so zurückgeschrieben** — die Zuordnung ist weg. Der Kombi-Puffer selbst überlebt
> (`WirksameVerwendung` reicht unbekannte Werte durch), seine Verwendung wird aber von der
> älteren Puffer-Verwaltung beim nächsten „Übernehmen" auf `Heizung` gesetzt.
>
> Für die Freigabe heißt das: Die Kombi-Option erst ausliefern, wenn die Fassung im Feld
> ist; Bestandsprojekte, die auf Kombi umgestellt werden, nicht mehr mit älteren
> Installationen öffnen. Ein Schemastand 10 als Riegel ist für D5b/E0 vorzumerken.

---

### 9.16 Restpunkte

> ✅ **ERLEDIGT mit Etappe D5b** (16.08.2026): Die sieben Punkte der D5b-Liste sind
> abgearbeitet — Freischaltung der Quellenwahl je `ID_Type`, Dialog-Zyklus- und
> -Kurzschlussprüfung, `WQ_ID_Puffer` als führende Identität, Groß-/Kleinschreibung der
> Verwendung, Kanalposition des Kombispeichers und der ausstehende Beleg zu E-K2-3.
> Umsetzung, Zahlen und die fortgeschriebene Restliste stehen in
> [`D5b_DialogFreischaltung_Protokoll.md`](D5b_DialogFreischaltung_Protokoll.md); die
> Punkte 8–11 (D3/D4) sind dort in Abschnitt 8 fortgeführt. Die Liste unten bleibt als
> Stand der Etappe D5a stehen.

**Für D5b:**

1. ~~Puffer-Verwaltung braucht die Kombi-Option~~ — **erledigt, vorgezogen** (I-K2-4,
   Abschnitt 9.1). Offen bleibt daran nur die **Positionsanzeige**: Ein Kombispeicher steht
   in beiden Entladereihenfolgen, angezeigt wird die Position im **Heizkanal**; die
   Warmwasserposition fehlt noch (zwei zusätzliche Katalogschlüssel für das Kanalwort).
2. **Freischaltung der Quellen-Spalte je `ID_Type`** (Konzept 4) — unverändert offen. Die
   Engine weist einen Quellbezug an Solarthermie und BHKW seit E-K2-2 mit Warnung ab; die
   Oberfläche sollte ihn gar nicht erst anbieten.
3. **Dialog-Zyklusprüfung** — unverändert offen (Engine bricht ab, Szenario (f)).
4. **Dialog-Kurzschlussprüfung für den Kessel** — neu aus E-K2-1: Die Engine weist „Quelle =
   eigene Senke" jetzt ab; der Dialog sollte die Konfiguration nicht speichern lassen.
5. **`WQ_ID_Puffer` als einzige Quellpuffer-Identität** (Etappe E0) — unverändert offen.
6. **Prüfpunkt zu E-K2-3** (Abschnitt 9.12): auslösende Konstellation über `WS_Ladegrenze`
   konstruieren und die Ergebnisänderung belegen.
7. **Groß-/Kleinschreibung der Verwendung** (Review 1, K3-2): `IstKombiVerwendung` vergleicht
   `OrdinalIgnoreCase`, `SimulationPufferspeicher.IstKombi` ordinal. Ein DB-Wert `"kombi"`
   stünde damit in beiden Entladereihenfolgen, verhielte sich aber wie ein Heizungspuffer.
   Heute nur über direkte Datenbankeingriffe erreichbar; sauber wäre eine Normalisierung in
   `WirksameVerwendung`. **Bewusst nicht in dieser Nacharbeit angefasst** — sie berührt auch
   `Heizung`/`Brauchwasser` und damit den Bestandspfad.

**Für D3/D4:**

8. **Kessel-Quellbezug in der Kartenansicht unsichtbar** (Review 2, K3-1):
   `Form_Simulation_Config.Karten.cs:946` erzeugt den Quelle-Chip nur für Wärmepumpen.
9. **Anzeige der Kaskade** — unverändert offen (Kaskadenketten-Band, Temperatur-Warnregel).
10. **Ergebnisspalte für die Quellwärme des Kessels** — unverändert offen
    (`SimulationSPK.Quellwaerme_gesamt` steht bereit, `Tab_ErgebnisHeizkessel` hat keine
    Spalte dafür).
11. **Vollzyklen des Kombispeichers** — unverändert (bekannter Durchsatz-Effekt, Befund N6).

**Aus den K3-Listen der Reviews, bewusst nicht angefasst:**

* `SimulationBHKW.ZweitsenkenRaum` behandelt den Kombispeicher als „eigenen Kanal" und lässt
  den Durchsatzterm weg (Review 1, K3-4). Das ist konservativ — das BHKW startet weniger
  Motoren, als der Kombi durchreichen könnte — und deshalb kein Fehler, steht aber im
  Gegensatz zu `DurchlassBudget`. **Als Modellentscheidung hiermit dokumentiert.**
* `Ladeordnung.Entladereihenfolge` fragt bedingungslos die Kanalsicht ab (Review 1, K3-3).
  Ohne Kombispeicher identisch; die Anzeige ist damit inhaltlich richtig.
* Freistehender Quellpuffer, `Anteil_Umbuchen` ohne bekannte Herkunft, kesselseitige
  Quelltemperatur als Puffer-Vorlauf (Review 1, K3-5/6/7) — unverändert offen bzw. nach dem
  E-K1-1-Fix unkritisch.
* `Kaskadenschleife.Entladeprobe` bleibt `public static` ohne Rücksetzung (Review 2, K3-5) —
  `#if DEBUG`, kein Release-Code.
* Repo-Hygiene (Review 2, K3-7) — vom Anwender mit `23dd3bc` erledigt.

---

### 9.17 Reproduktion der Nacharbeit

```powershell
$msb = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

# 1. Anwendung und Referenzlauf bauen (Ausgabe NIE nach bin\)
& $msb C:\Waermeplan\WP_Plan\WP-Plan.sln -t:Rebuild `
       -p:Configuration=Debug -p:Platform=x86 -p:OutDir=<scratch>\app\
& $msb C:\Waermeplan\WP_Plan\Referenzlauf\Referenzlauf.csproj `
       -p:Configuration=Debug -p:Platform=x86 -p:OutDir=<scratch>\ref\

# 2. HEAD-Vergleichsstand aus einem eigenen Arbeitsbaum
git worktree add <scratch>\wt_head HEAD --detach
& $msb <scratch>\wt_head\WindowsFormsApplication1\WindowsFormsApplication1.csproj `
       -p:Configuration=Debug -p:Platform=x86 -p:OutDir=<scratch>\head\
#    ref-Ordner kopieren, WindowsFormsApplication1.dll durch die HEAD-Fassung ersetzen

# 3. Migrierte Wegwerf-Kopie (Produktiv-DB nur LESEN, vorher Kenndaten.laccdb pruefen)
<scratch>\ref\Referenzlauf.exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb <scratch>\DB

# 4. Regression Flag AUS gegen die eingefrorene Basis
foreach ($id in 1007,1008,1011,1017,1018,1021,1023,1024) {
    <scratch>\ref\Referenzlauf.exe projekt $id <scratch>\Lauf_neu\Projekt_$id <scratch>\DB
}
<scratch>\ref\Referenzlauf.exe vergleich `
    C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-15_B3 <scratch>\Lauf_neu
#    zusaetzlich rekursiver MD5-Vergleich beider Ordner

# 5. Szenarien: je eine Kopie von <scratch>\DB, praepariert per 32-bit-PowerShell + ACE
#    (Flag, Verwendung, WS_Ziel, WQ_Typ/WQ_ID_Puffer, Tab_Heizkessel.Vorlauf/Ruecklauf)

# 6. In-process-Probe: eigenes Konsolenprojekt mit Projektreferenz auf die App,
#    DB-Pfad ueber DbUmgebung.AufArbeitskopieUmschaltenUndPruefen umgebogen,
#    Kanaldaten ueber den #if-DEBUG-Haken Kaskadenschleife.Entladeprobe.
#    Modus --form <kultur> instanziiert Form_Waermesenke und misst die Geometrie.

# 7. Vergleichsbauten der Varianten: dieselben Quellen in <scratch>\wt_head,
#    je EINE Stelle zurueckgedreht, eigener OutDir, Probe mit -p:AppProj darauf gebaut.
```

Arbeitsbaum, Datenbankkopien, Vergleichsbauten und Prüfprogramm sind Wegwerf-Material und
nach der Abnahme gelöscht; die Zahlen dieses Abschnitts sind der Beleg.

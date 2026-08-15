# K-3 — Bivalenz-Umschaltung des WP-Alternativbetriebs an der Bivalenztemperatur

**Stand 15.08.2026 · Status: umgesetzt und verifiziert · ERGEBNISÄNDERUNG (vom Anwender
ausdrücklich freigegeben) · Für die Referenzprojekte ergebnisneutral nachgewiesen
(9/9 PASS, Flag aus UND Flag an, 208/208 byte-/MD5-gleich)**

Bezug: [`Konzept_KonfigUI_Hydraulik.md`](Konzept_KonfigUI_Hydraulik.md) Abschnitt 8, Zeile **K-3** ·
Regressionsbasis [`../../../Referenzlaeufe/2026-08-15_B3/`](../../../Referenzlaeufe/2026-08-15_B3/)

---

## 1. Was geändert wurde

Für eine Wärmepumpe mit `Bivalenter_Betrieb = TRUE` und `Betriebsart = "Alternativbetrieb"`
gilt ab sofort:

| Bedingung | Verhalten |
|---|---|
| `T_außen < Abschaltpunkt` | Wärmepumpe **aus** (`PTHERM = 0`, `PEL = 0`); der zweite Erzeuger übernimmt über die normale Kaskade allein |
| `T_außen >= Abschaltpunkt` | Wärmepumpe **an** mit ihrer Leistung; was sie in einer Stunde nicht deckt, geht regulär an die nächste Kaskadenstufe |

`Abschaltpunkt` ist `Tab_Energieanlagen.Abschaltpunkt` [°C] — dieselbe Spalte, die der
teilparallele Zweig als Abschalttemperatur auswertet. `T_außen` ist
`SimulationWaermepumpe.Temperatur[stunde]`, die Stunden-Außentemperatur, die
`SimulationControl` in **beiden** Rechenwegen setzt (Zeilen 874 und 2297).

**Bisher** stand an beiden Stellen eine Leistungsprüfung:
`if (result[PTHERM] < Rest_waerme) { PTHERM = 0; PEL = 0; }`. Die Wärmepumpe fiel damit in
**jeder** Stunde aus, die sie nicht vollständig deckte — unabhängig von der
Außentemperatur. Der gepflegte `Abschaltpunkt` blieb wirkungslos.

---

## 2. Datenbefund (produktive `Kenndaten.accdb`, nur gelesen)

Abgefragt am 15.08.2026 über 32-bit-PowerShell + ACE OLEDB, `Mode=Read`; keine
`Kenndaten.laccdb` vorhanden.

### 2.1 Betriebsarten aller Wärmepumpen-Anlagen (`ID_WP > 0`, 26 Zeilen)

| Betriebsart | `Bivalenter_Betrieb` | Anzahl |
|---|---|---|
| *(leer)* | False | 20 |
| NULL | False | 1 |
| `Alternativbetrieb` | **False** | 1 |
| `Parallelbetrieb` | True | 3 |
| `Teilparallelbetrieb` | True | 1 |

**Es gibt im gesamten Bestand keine einzige Anlage mit
`Bivalenter_Betrieb = TRUE` UND `Betriebsart = "Alternativbetrieb"`.** Der geänderte Zweig
ist heute in keinem gespeicherten Projekt aktiv — weder in den neun Referenzprojekten noch
sonst wo.

Die eine `Alternativbetrieb`-Zeile (Anlage 10132, Projekt 1008, `CS7800iLW 16`,
`Abschaltpunkt = 19`) trägt `Bivalenter_Betrieb = False`; die Bedingung ist eine
**Und-Verknüpfung**, der Zweig greift dort nicht — vorher wie nachher.

### 2.2 Wärmepumpen in den neun Referenzprojekten

| Projekt | Anlage | Betriebsart | bivalent | `Abschaltpunkt` | K-3 betroffen |
|---|---|---|---|---|---|
| 1007 | 10353 | *(leer)* | False | 0 | nein |
| 1008 | 10132 | `Alternativbetrieb` | **False** | 19 | nein |
| 1008 | 10133 | *(leer)* | False | 0 | nein |
| 1010 | 10000 | *(leer)* | False | 0 | nein |
| 1011 | 10635 / 10642 / 10643 | *(leer)* | False | 0 | nein |
| 1017 | 10211 | `Teilparallelbetrieb` | True | -10 | nein (anderer Zweig) |
| 1018 | — | keine WP | | | nein |
| 1021 | 10360 / 10361 | *(leer)* | False | 0 | nein |
| 1023 | 11203 / 11204 | *(leer)* | False | 0 | nein |
| 1024 | 11262 | *(leer)* | False | 0 | nein |

**Vorhersage vor dem A/B-Lauf: null Abweichungen in allen neun Projekten.** Genau das ist
eingetreten (Abschnitt 5).

> **Anmerkung zum Datenstand.** Die Tabelle gibt den Stand der Datenbankkopie von 22:26 Uhr
> wieder, auf der die A/B-Probe lief. Gegen 22:50 Uhr hat der Anwender die Projekte 1010,
> 1016, 1020 und 1025 gelöscht; **Projekt 1010 gehört seither nicht mehr zur Referenzmenge**
> (Abschnitt 7). Für den Befund ändert das nichts — es entfällt eine Zeile ohne
> bivalent-alternative Anlage.

### 2.3 Belegung von `Abschaltpunkt`

* **82 Zeilen** in `Tab_Energieanlagen` gesamt, davon **0 mit `Abschaltpunkt IS NULL`** —
  auch bei den WP-Zeilen keine einzige.
* Vorkommende Werte im Bestand: **0** (die Mehrheit), **-10** (1009, 1017), **19** (1008).
* Der Wizard schreibt das Feld über `double.Parse(textBox_Abschalttemp.Text)`
  (`Views/Wizard/Wizard_WPItem.cs:220`) — **immer**, nie NULL. Die Vorbelegung des
  Eingabefelds ist **`0`** (`Wizard_WPItem.resx`, `textBox_Abschalttemp.Text`).
* Zusätzlich fällt `WErzeugerCtrl.ReadAllFilter` bei NULL still auf den Feld-Vorgabewert
  `0` zurück (`if (Belegt(dt, row, "Abschaltpunkt")) …`, Zeile 189) — NULL und 0 kommen im
  Modell also ohnehin identisch an.

---

## 3. Regelentscheidung: der Spaltenwert gilt **immer wörtlich**, auch 0 °C

**Entscheidung.** `Abschaltpunkt` wird unverändert als Bivalenztemperatur verwendet. Es gibt
keine Ersatzregel, keinen Rückfall auf das alte Verhalten und keinen Sonderfall für 0.

**Begründung aus dem Datenbefund.**

1. **„Ungepflegt" ist im Datenmodell nicht darstellbar.** Die Spalte ist in keiner Zeile
   NULL, und der einzige Schreibweg (Wizard) kann gar kein NULL erzeugen. Ein Programm
   könnte „nie gepflegt" also nur an der **0** erkennen — und 0 °C ist eine gängige,
   fachlich plausible Bivalenztemperatur.
2. **Eine Ersatzregel würde die falschen Anlagen treffen.** Wer 0 °C bewusst einträgt,
   bekäme mit einer „0 = ungepflegt"-Regel stillschweigend ein anderes Modell gerechnet als
   eingegeben — ein Fehler, den niemand am Bildschirm sieht. Der umgekehrte Fall (jemand hat
   den Vorbelegungswert nie angefasst) ist sichtbar und korrigierbar.
3. **Kein Bestandsprojekt ist heute betroffen** (Abschnitt 2.1). Die Regel wirkt
   ausschließlich auf Anlagen, die ab jetzt bewusst auf bivalent-alternativ gestellt werden —
   und dabei durchläuft der Anwender genau die Maske, in der das Feld steht.
4. Sie ist zudem **identisch mit der Regel des teilparallelen Zweigs**, der denselben Wert
   seit jeher wörtlich nimmt. Zwei Auslegungen derselben Spalte im selben Modul wären die
   sichere Quelle künftiger Verwirrung.

**Flankierend: ein Hinweis, keine Umdeutung.** Trifft der Modulaufbau eine
bivalent-alternative Anlage mit `Abschaltpunkt == 0`, meldet
`AlternativHinweisPruefen` **einmal je Anlage** über `SimulationProtokoll.HinweisEinmal`:

> Wärmepumpe: Die Anlage '…' rechnet bivalent-alternativ mit einer Bivalenztemperatur von
> 0 °C — dem Vorbelegungswert des Eingabefelds. Unterhalb von 0 °C bleibt die Wärmepumpe aus
> und der zweite Wärmeerzeuger übernimmt allein. Ist das nicht beabsichtigt, die
> Abschalttemperatur der Anlage pflegen.

Die Meldung ist **rein informativ** — sie ändert keine Zahl. Nachgewiesen im Lauf: bei
`T_biv = 0` erscheint sie, bei `T_biv = -2` nicht.

> **Katalog-Kandidat Lokalisierung.** Der Text steht als deutscher Festtext im Code, wie die
> übrige Protokollmeldung dieses Moduls (`QuellspeicherZusammenfuehren`). Aufnahme in
> `MyResource` mit `de-DE`/`en-US`-Satelliten steht aus; im Code ist die Stelle als
> `KATALOG-KANDIDAT (Lokalisierung)` markiert. Nachzutragen in
> [`Lokalisierung_Katalog.md`](Lokalisierung_Katalog.md).

### 3.1 Vergleichsgrenze `<` statt `<=` — bewusst

Abgeschaltet wird bei **echter Unterschreitung** (`T_außen < Abschaltpunkt`); bei genau der
Bivalenztemperatur läuft die Wärmepumpe noch. Das folgt der fachlichen Formulierung
(„bei **Unterschreitung** des Bivalenzpunktes springt der zweite Erzeuger ein") und der
Vorgabe der Aufgabenstellung.

Der **teilparallele** Zweig schaltet dagegen bei `<=` ab (`Temperatur[stunde] <= Abschaltpunkt`).
Der Unterschied betrifft ausschließlich Stunden mit exakter Gleichheit. Er bleibt bestehen,
weil der Teilparallel-Zweig **byte-unverändert** bleiben musste (Verifikationsauflage 3) und
nicht Gegenstand von K-3 ist.

> **Offener Punkt (klein):** Angleichung der beiden Vergleichsgrenzen wäre eine eigene,
> ebenfalls ergebniswirksame Entscheidung — mit derselben Methodik zu behandeln.

---

## 4. Änderungen im Code

Alles in **einer** Datei: `WindowsFormsApplication1/Allgemein/Simulation/SimulationWaermepumpe.cs`
(UTF-8 mit BOM, CRLF — beides beibehalten).

| Stelle | Zeile | Was |
|---|---|---|
| **Altpfad** `Berechnung_Stundenschleife` | **493–503** | Leistungsprüfung `result[PTHERM] < Rest_waerme` → `AlternativAus(model, stunde)` |
| **Zweikanalig** `Zweikanalig_Bedarfsphase` | **1023–1032** | Leistungsprüfung `result[PTHERM] < AlternativBezug(…)` → `AlternativAus(model, stunde)` |
| neu: `AlternativAus` | **1260–1263** | gemeinsames Umschaltkriterium beider Rechenwege, `Temperatur[stunde] < model.Abschaltpunkt` |
| neu: `AlternativHinweisPruefen` | **1275–1290** | einmaliger Hinweis bei `Abschaltpunkt == 0` |
| Aufruf des Hinweises in `ModuleAufbauen` | **259** | je Anlage genau einmal, gilt für beide Rechenwege |
| entfallen: `AlternativBezug` | — | wurde toter privater Code; die Frage „gegen welchen Bedarf wird verglichen" stellt sich nicht mehr |

Dass **eine** Methode beide Zweige bedient, ist der Kern der Auflage „identische Semantik in
beiden Rechenwegen": Die Regel steht genau einmal im Code.

Die Zweige **Parallelbetrieb** und **Teilparallelbetrieb** sind unangetastet.

---

## 5. Verifikation 1 — A/B gegen den unveränderten Stand `a0a623a`

**Aufbau.** Eigener git-Arbeitsbaum auf `a0a623a` (Haupt-Checkout unangetastet). Dort
zweimal `Referenzlauf.csproj` gebaut — einmal HEAD, einmal HEAD + K-3 —, beide Binärstände
gesichert. Eine gemeinsame, migrierte Kopie der produktiven `Kenndaten.accdb`
(Zeitstempel 15.08.2026 22:26), jedes Projekt im Modus `projekt` mit beiden Ständen.

> Die Kopie stammt von **22:26 Uhr** und enthält deshalb noch alle **neun** Projekte der
> Basis B2 — auch das gegen 22:50 Uhr vom Anwender gelöschte Projekt **1010**. Der A/B-Beleg
> ist damit umfassender als die neue Basis B3 (acht Projekte, Abschnitt 7). Für den A/B-Zweck
> ist das unkritisch und sogar günstig: Beide Stände rechnen auf **derselben** Kopie, die
> Löschung kann das Ergebnis nicht verfälschen.

### 5.1 Flag `Kaskade_Zweikanalig` = AUS

| Projekt | Werte | Status |
|---|---|---|
| 1007 / 1008 / 1010 / 1011 | 324 210 / 227 847 / 201 540 / 324 232 | PASS |
| 1017 / 1018 / 1021 | 245 378 / 210 343 / 227 840 | PASS |
| 1023 / 1024 | 262 917 / 271 680 | PASS |
| **gesamt** | **2 295 987** | **9/9 PASS** |

**Byte-/MD5-Gegenprobe: 208 von 208 CSV-Dateien identisch, 0 Abweichungen.**

### 5.2 Flag `Kaskade_Zweikanalig` = AN

Auf derselben Kopie für alle neun Projekte `Kaskade_Zweikanalig = TRUE` gesetzt, danach
wieder zurückgesetzt.

| Projekt | Werte | Status |
|---|---|---|
| 1007 / 1008 / 1010 / 1011 | 324 210 / 227 847 / 201 540 / 324 232 | PASS |
| 1017 / 1018 / 1021 | 245 378 / 210 343 / 227 840 | PASS |
| 1023 / 1024 | 262 917 / 271 691 | PASS |
| **gesamt** | **2 295 998** | **9/9 PASS** |

**Byte-/MD5-Gegenprobe: 208 von 208 CSV-Dateien identisch, 0 Abweichungen.**

### 5.3 Abweichungstabelle je Projekt

| Projekt | WP-Produktion | Kessel-Produktion | Deckung | WP-Ein/Aus-Wechsel | Abweichung |
|---|---|---|---|---|---|
| 1007 | unverändert | unverändert | unverändert | unverändert | **keine (byte-gleich)** |
| 1008 | unverändert | unverändert | unverändert | unverändert | **keine (byte-gleich)** |
| 1010 \* | unverändert | unverändert | unverändert | unverändert | **keine (byte-gleich)** |
| 1011 | unverändert | unverändert | unverändert | unverändert | **keine (byte-gleich)** |
| 1017 | unverändert | unverändert | unverändert | unverändert | **keine (byte-gleich)** |
| 1018 | keine WP | unverändert | unverändert | — | **keine (byte-gleich)** |
| 1021 | unverändert | unverändert | unverändert | unverändert | **keine (byte-gleich)** |
| 1023 | unverändert | unverändert | unverändert | unverändert | **keine (byte-gleich)** |
| 1024 | unverändert | unverändert | unverändert | unverändert | **keine (byte-gleich)** |

\* Projekt **1010** war auf der A/B-Kopie (22:26 Uhr) noch vorhanden und ist mitgeprüft; der
Anwender hat es gegen 22:50 Uhr gelöscht. Es gehört seither nicht mehr zur Referenzmenge —
die neue Basis B3 umfasst die verbliebenen **acht** Projekte (Abschnitt 7).

Das deckt sich mit der Vorhersage aus Abschnitt 2.2: Kein Referenzprojekt führt eine
bivalent-alternative Wärmepumpe, der geänderte Zweig kann dort nicht greifen.

### 5.4 Auflage 3 — Teilparallel und Parallel byte-unverändert

* **Teilparallel:** Projekt **1017** (Anlage 10211, `Teilparallelbetrieb`, bivalent,
  `Abschaltpunkt = -10`) ist Teil der Referenzmenge und in beiden Flagstellungen
  **byte-gleich** (245 378 Werte, 20 Dateien).
* **Parallel:** kein Referenzprojekt führt `Parallelbetrieb`. Deshalb eigens geprüft an einer
  Kopie von **Projekt 1026** im Ausgangszustand (Anlage 11267, `Parallelbetrieb`, bivalent,
  `Abschaltpunkt = 0`): HEAD gegen K-3 **byte-gleich über alle 25 CSV-Dateien**.

---

## 6. Verifikation 2 — Wirkungsnachweis an präparierten Kopien

Nur auf **Kopien** der Datenbank, nie produktiv. Zwei Projektmuster, weil sie
unterschiedliche Seiten der Änderung zeigen.

### 6.1 Projekt 1026 „Beispiel WP WG 1" — Wärmepumpe + Kessel + Pufferspeicher

Anlage 11267 auf `Alternativbetrieb` + `Bivalenter_Betrieb = TRUE` gestellt; das
Nutzerprojekt-Muster aus dem Sichttest.

| Größe | ALT (Leistungsregel) | NEU (K-3), `T_biv = 0 °C` | NEU, `T_biv = -2 °C` |
|---|---|---|---|
| WP-Wärmeproduktion | 28 278 kWh | **40 151 kWh** (+42,0 %) | 44 973 kWh (+59,0 %) |
| Kessel-Wärmeproduktion | 36 441 kWh | **24 552 kWh** (−32,6 %) | 19 742 kWh (−45,8 %) |
| WP-Deckungsanteil am Bedarf | 44,0 % | **62,4 %** | 69,9 % |
| WP-Betriebsstunden | 2 614 h | 3 562 h | — |
| **WP-Ein/Aus-Wechsel** | **2 962** | **2 524** (−14,8 %) | 2 598 |
| **Kessel-Ein/Aus-Wechsel** | **478** | **266** (−44,4 %) | 310 |
| Stunden unterhalb `T_biv` mit WP an | **330** von 1 127 (`T < 0`) | **0** von 1 127 | **0** von 625 (`T < -2`; ALT: 209) |
| Stunden ab `T_biv` mit WP an | 2 284 | **3 562** | 4 011 (ALT: 2 405) |
| Stunden, in denen die alte Regel die WP zwangsweise abschaltete (`T >= 0`, Bedarf > 0, WP aus) | **5 349** | — | — |
| Stunden, in denen die WP **nur** im NEU-Stand läuft | — | **1 334** | 1 662 |
| Stunden, in denen die WP **nur** im ALT-Stand lief | — | **386** (alle `T < T_biv`) | 265 |

Die Zeile „Stunden `T < 0 °C` mit WP an: 330 → 0" ist der Kern: Die alte Regel ließ die
Wärmepumpe **bei Frost** laufen (immer dann, wenn der Bedarf zufällig klein genug war) und
schaltete sie **bei milden Temperaturen** ab (immer dann, wenn der Bedarf ihre Leistung
überstieg) — also genau umgekehrt zum Bivalenzgedanken. Nach K-3 gilt: unterhalb der
Bivalenztemperatur läuft **ausschließlich** der Kessel.

**Zweikanaliger Rechenweg, dasselbe Projekt, `T_biv = 0 °C`:**

| Größe | ALT | NEU (K-3) |
|---|---|---|
| WP-Wärmeproduktion | 32 842 kWh | **36 982 kWh** (+12,6 %) |
| Kessel-Wärmeproduktion | 27 298 kWh | **23 093 kWh** (−15,4 %) |
| WP-Deckungsanteil am Bedarf | 51,1 % | **57,5 %** |
| **WP-Ein/Aus-Wechsel** | **1 126** | **140** (**−87,6 %**) |
| Kessel-Ein/Aus-Wechsel | 606 | 512 |
| Stunden `T < 0 °C` mit WP an | **598** von 1 127 | **0** von 1 127 |
| Stunden `T >= 0 °C` mit WP an | 6 933 von 7 633 | **7 633** von 7 633 |
| Zwangsabschaltungen der alten Regel | 700 | — |

Im zweikanaligen Weg fällt das Pendeln drastisch: **1 126 → 140 Wechsel**. Dass die
Reduktion im einkanaligen Weg mit −15 % kleiner ausfällt, hat einen eigenen Grund: Dort
taktet die Wärmepumpe zusätzlich mit der **Hysterese des Senken-Pufferspeichers**
(Ein-/Ausschaltschwelle), und dieses Takten bleibt von K-3 unberührt. Die Zahl misst also
beide Ursachen zusammen; der Anteil des Bivalenz-Pendelns verschwindet vollständig.

### 6.2 Projekt 1024 „Wöhler - Test2" — der Sommer-Warmwasserfall

1026 zeigt den Sommerausfall nicht, weil dort die Wärmepumpe (≈10 kW) jede Sommerstunde
allein trägt. Für den vom Anwender beobachteten Effekt wurde deshalb **1024** präpariert
(Anlage 11262 auf `Alternativbetrieb` + bivalent, `T_biv = 0 °C`): 15 125 kWh
Brauchwasserbedarf im Sommer und 699 Sommerstunden, in denen der Bedarf die maximale
WP-Stundenleistung übersteigt.

| Größe | ALT (Leistungsregel) | NEU (K-3) |
|---|---|---|
| WP-Wärmeproduktion | 11 886 kWh | **71 545 kWh** (6,0-fach) |
| Kessel-Wärmeproduktion | 59 214 kWh | **47 063 kWh** (−20,5 %) |
| **WP-Ein/Aus-Wechsel** | **652** | **414** (−36,5 %) |
| Zwangsabschaltungen der alten Regel (`T >= 0`, Bedarf > 0, WP aus) | **4 879** | — |
| Stunden, in denen die WP **nur** im NEU-Stand läuft | — | **4 879** |
| **davon im Sommer (Jun–Aug)** | — | **714 h** mit zusammen **10 103 kWh Brauchwasserbedarf** |
| Stunden, in denen die WP **nur** im ALT-Stand lief | — | **0** |
| Stunden `T < 0 °C` mit WP an | 0 von 1 127 | 0 von 1 127 |

**714 Sommerstunden**, in denen die Wärmepumpe bisher an Warmwasserspitzen ausfiel und der
Kessel im Juli einsprang, laufen jetzt wieder mit der Wärmepumpe. Das ist der vom Anwender
gemeldete Effekt, quantifiziert. Unterhalb von 0 °C ändert sich in diesem Projekt nichts —
dort war die Wärmepumpe schon vorher aus, weil der Bedarf ihre Leistung stets überstieg.

### 6.3 Bilanzproben

Stundengenau über alle 8 760 Stunden, für jede präparierte Variante:

| Variante | Probe | max. Abweichung | Stunden > 0,01 kWh |
|---|---|---|---|
| 1026, `T_biv = 0`, einkanalig | WP + Kessel + Puffer-Entladung ≡ Bedarf + Puffer-Ladung − Rest | 0,000001 kWh | **0** |
| 1026, `T_biv = -2`, einkanalig | dieselbe | 0,000001 kWh | **0** |
| 1026, `T_biv = 0`, zweikanalig | dieselbe | 0,000003 kWh | **0** |
| 1024, `T_biv = 0`, einkanalig (ALT) | WP + Kessel + BHKW + Heizstab + Rest ≡ Wärmebedarf | 0,000001 kWh | **0** |
| 1024, `T_biv = 0`, einkanalig (NEU) | dieselbe | 0,000007 kWh | **0** |

Zusätzlich die Plausibilitätsprüfung der Suite (`Referenzlauf.exe pruefen`) über alle vier
präparierten Varianten: **plausibel**, 4/4 OK (insgesamt 1 059 960 Werte).

Die Restwärme bleibt in allen Varianten auf 0 MWh — die Kaskade schließt den Bedarf; K-3
verschiebt ausschließlich die Aufteilung zwischen Wärmepumpe und zweitem Erzeuger.

---

## 7. Neue Regressionsbasis B3

[`Referenzlaeufe/2026-08-15_B3/`](../../../Referenzlaeufe/2026-08-15_B3/) — **acht**
Projekte, 190 CSV-Dateien, 2 094 447 Werte, Flag `Kaskade_Zweikanalig` aus, produktive
`Kenndaten.accdb` vom 15.08.2026 22:50 (nur gelesen).

* **Selbstvergleich:** 8/8 PASS, 190/190 byte-/MD5-gleich → reproduzierbar.
* **Gegen B2, Projekt für Projekt:** die acht gemeinsamen Projekte sind **190/190
  byte-/MD5-gleich**; **Projekt 1010 entfällt** — der Anwender hat es gegen 22:50 Uhr aus der
  produktiven Datenbank gelöscht (zusammen mit 1016, 1020, 1025). Das ist eine **Datenlage,
  kein Codeeffekt**: Der A/B-Lauf auf der 22:26-Kopie hatte 1010 noch dabei und zeigte auch
  dort keine Abweichung.
* Der Basiswechsel hat damit zwei Gründe — die geschrumpfte Projektmenge und die Zuordnung
  (ab hier ist die Basis mit dem K-3-Code gerechnet); **keiner davon heißt „geänderte
  Zahlen"**.
* B2 bleibt unangetastet liegen und ist die einzige verbliebene Quelle für die Ganglinien
  von Projekt 1010.
* **Folgebedarf:** 1010 deckte in der Referenzmenge die Kategorie „Wärmepumpe ohne weitere
  Erzeuger" ab; fällt sie dauerhaft weg, braucht die Menge ein Ersatzprojekt
  (`Projektauswahl.MAX_PROJEKTE` = 9). Details im
  [Laufprotokoll der Basis](../../../Referenzlaeufe/2026-08-15_B3/lauf_protokoll.md).

---

## 8. Build

VS-MSBuild x86, zweimal geprüft:

| Build | Ergebnis |
|---|---|
| Eigener Arbeitsbaum (`a0a623a` + K-3-Datei), `Referenzlauf.csproj` mit `ProjectReference` auf die Anwendung | **0 Fehler, 6 Warnungen** |
| Haupt-Checkout, `WindowsFormsApplication1.csproj`, Neukompilierung nach `-p:OutDir=<Wegwerfordner>` | **0 Fehler, 6 Warnungen** |

Es sind exakt die sechs Bestandswarnungen (`CS0108` ×2, `CS0109` ×2, `CS4014`, `CS1998`) —
derselbe Satz wie beim HEAD-Build ohne die Änderung. Kodierung (UTF-8 mit BOM) und
Zeilenenden (CRLF) der geänderten Datei sind erhalten. Das `bin\`-Verzeichnis des
Haupt-Checkouts wurde nicht angefasst.

> **HEAD-Drift während der Sitzung.** Der Haupt-Checkout stand beim Start auf `a0a623a` und
> ist durch parallele Sitzungen inzwischen auf `6d811bd` gewandert. Keiner dieser Commits
> berührt eine der hier geschriebenen Dateien
> (`git log a0a623a..HEAD -- SimulationWaermepumpe.cs Referenzlaeufe/LIESMICH.md
> Konzept_KonfigUI_Hydraulik.md` ist leer), die A/B-Basis `a0a623a` bleibt für die
> Wirkungsanalyse dieser Änderung also gültig.

---

## 9. Folgeschritt: Sichtbarkeit des Eingabefelds

`Views/Wizard/Wizard_WPItem.cs` blendet die Abschalttemperatur-Controls
(`textBox_Abschalttemp`, `label_Abschalttemperatur`, `label_AbschalttemperaturEinheit`) nur
bei `Betriebsart == "Teilparallelbetrieb"` ein (Zeilen 339–376). Mit K-3 ist der Wert auch
für **`Alternativbetrieb`** rechenwirksam — der Anwender kann ihn dort aber gar nicht
sehen oder pflegen.

**Nachzuziehen:** Sichtbarkeitsregel um `Alternativbetrieb` erweitern, Beschriftung
sinnvollerweise auf „Bivalenztemperatur" abstimmen.

> **Nicht in diesem Paket umgesetzt.** `Wizard_WPItem.*` gehört zu einer laufenden
> Nutzer-Chip-Sitzung; die Datei wurde bewusst nicht angefasst. Der Schritt steht nach
> Freigabe dieser Sitzung an. Bis dahin fängt der Hinweis aus Abschnitt 3 den häufigsten
> Fall ab (Vorbelegung 0 °C bleibt unbemerkt wirksam).

---

## 10. Was nicht angefasst wurde

`Views/Simulation/*`, `MyResource`, `Allgemein/Bericht`, `Allgemein/Wirtschaftlichkeit`,
`Views/Bericht`, `Views/Wirtschaftlichkeit`, `Views/Varianten`, `Views/Wizard/Wizard_WPItem.*`
— alles in der Hand paralleler Sitzungen. Geschrieben wurden ausschließlich:
`Allgemein/Simulation/SimulationWaermepumpe.cs`, dieses Protokoll,
`Konzept_KonfigUI_Hydraulik.md` (nur die K-3-Zeile in Abschnitt 8),
`Referenzlaeufe/2026-08-15_B3/` und `Referenzlaeufe/LIESMICH.md`. **Nichts committet.**

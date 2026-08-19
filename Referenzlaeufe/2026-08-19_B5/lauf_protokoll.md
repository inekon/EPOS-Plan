# Referenzlauf-Protokoll — Basis B5

**Zeitpunkt:** 19.08.2026, 03:38 Uhr · **Werkzeugprotokoll des Laufs:**
[`lauf_protokoll_werkzeug.md`](lauf_protokoll_werkzeug.md)

**Anlass: die Basis B4 ist als Vergleichsmaßstab unbrauchbar geworden.** Ein Lauf des
unveränderten Codes gegen B4 endete zuletzt in **7 von 8 Projekten mit FAIL** — Ursache sind
Datenänderungen des Anwenders, nicht der Code. Dazu kommen die Codeetappen seit dem
16.08.2026 (Migrationsschritte 17 und 18, Katalog gesetzlicher Parameter E1,
Vollbenutzungsstunden-Korrektur E2, 500-kW-Grenze je Anlage, Heizöl-Ausschluss je Anlage) und
mit **Projekt 1030** ein neues Referenzprojekt für Mehrmodul-Kaskaden. B5 friert diesen Stand
ein; künftige Vergleiche laufen wieder ohne Ausschluss gegen diesen Ordner.

**Codestand:** `ef8e537` („KWKG: Heizoel-Ausschluss je Anlage und ueber die installierten
Anlagen"), unverändert, Arbeitsbaum sauber. Gebaut in einem eigenen Export des Commits
außerhalb des Repos (`C:\Waermeplan\_b5`, `git archive HEAD`), VS-MSBuild x86/Debug über
`Referenzlauf\Referenzlauf.csproj` (ProjectReference auf die App → Exe und DLL konsistent).
Der Haupt-Checkout und dessen `bin\` wurden **nicht** angefasst. Build: **0 Fehler,
6 Bestandswarnungen** (CS0108 ×2, CS0109 ×2, CS1998, CS4014).

**Quelle (produktiv, nur gelesen):** `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`,
Zeitstempel **19.08.2026 02:51:17**, 96 436 224 Byte, MD5
`66F4806A3B89074B52344F39D477F151`, Schemastand **17**. Keine `Kenndaten.laccdb` vorhanden,
die Anwendung hatte die Datenbank also nicht geöffnet. Gerechnet wurde auf der Arbeitskopie,
die der Lauf selbst zieht — sie lag unter `C:\Waermeplan\_b5\Referenzlaeufe\Arbeitskopie\`,
nicht im Repo. **Die Migration 17 → 18 lief ausschließlich auf der Kopie**
(`Schemastand vorher: 17 → nachher: 18`). Nachkontrolle nach beiden Läufen: Zeitstempel,
Größe und MD5 der produktiven Datei sind **unverändert**, weiterhin keine `Kenndaten.laccdb`.

## Projektmenge: neun Projekte, **fest vorgegeben**

Die Auswahl ist mit `--projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030` **fest
vorgegeben**, nicht automatisch ermittelt. Grund: Die automatische Auswahl ist datengetrieben
und wandert mit dem Projektbestand. Mit den neuen Beispielprojekten 1026–1029 wählt sie
inzwischen 1007, 1011, **1012**, 1017, 1021, 1023, 1024, **1026**, 1030 — **1008 und 1018
fallen heraus, 1012 und 1026 kommen hinzu**. Für eine über die Zeit vergleichbare Basis ist
das untauglich; B5 friert deshalb die acht Projekte der Basis B4 **plus** das neue
Kaskadenprojekt 1030 ein. Jeder Folgelauf muss dieselbe Liste mitgeben.

| ID | Projekt | Ausstattung (Kurzform) | CSV | Werte | Status |
|---|---|---|---|---|---|
| 1007 | Laurentiuskirche | WP, PV, Batterie, Kessel, Puffer | 29 | 324 210 | OK |
| 1008 | Heinestr 15 | WP, Kessel, Puffer(WP) | 21 | 227 847 | OK |
| 1011 | test1 | WP, Solar, PV, Batterie, Kessel, Puffer | 29 | 324 232 | OK |
| 1017 | WP_PV-Speicher | WP, Batterie, Kessel, BHKW | 21 | 254 143 | OK |
| 1018 | BHKW Test München | Kessel, BHKW, Puffer | 22 | 236 642 | OK |
| 1021 | TestSpeichernUnter | WP, Quellspeicher(WP) | 21 | 227 840 | OK |
| 1023 | Wöhler - Test1 | WP, Kessel, Puffer(WP) | 25 | 262 918 | OK |
| 1024 | Wöhler - Test2 | WP, Kessel, BHKW, Puffer | 26 | 271 695 | OK |
| **1030** | **Referenz BHKW-Kaskade (Regressionstest)** | **Kessel, BHKW ×2, Puffer** | **22** | **236 650** | **OK** |
| **gesamt** | | | **216** | **2 366 177** | **9/9 OK** |

Gesamtdauer 00:00:53, Timeout je Projekt 300 s, 13 Warnungen, 0 Fehler.
Plausibilitätsprüfung (`pruefen`): **GESAMT: plausibel**, ein Hinweis wie gehabt
(`Projekt_1007/solar_produktion.csv`: Gewerk aktiviert, kein Modul zugeordnet).

Die 13 Warnungen sind ausschließlich Bestandshinweise zur **Datenpflege**, keine Rechenfehler:
**fünf** Puffer ohne gepflegtes Temperaturpaar (Rückfall ΔT = 10 K; Projekte 1008, 1011, 1018)
und **acht** Anlagen ohne zugeordneten Energieträger (`ID_Carrier` leer; Projekte 1007, 1008,
1011, 1017, 1018, 1023). **Projekt 1030 läuft warnungsfrei.**

### Feature-Flag `Kaskade_Zweikanalig`

Anders als bei B2 bis B4 lässt sich die Basis nicht mehr pauschal als „Flag AUS" beschreiben:

- Für die **vier BHKW-Projekte** (1017, 1018, 1024, 1030) ist das Flag **wirkungslos**. Die
  Engine meldet je Projekt `Simulation Hinweis: Das Projekt enthält ein BHKW — dieser Lauf
  rechnet deshalb IMMER über die Speicherstufe mit herausgelöster Ladephase (Konzept 6.3),
  unabhängig von der Projekteinstellung Kaskade_Zweikanalig. Der einkanalige BHKW-Altpfad ist
  entfallen (Paket BHKW-Regulär).`
- Für die **fünf übrigen Projekte** steht das Flag auf **AUS**.
- Im Datenbestand steht es inzwischen bei **1018 auf WAHR** (in B4 noch bei allen acht auf
  FALSCH) — folgenlos, weil 1018 ein BHKW-Projekt ist.

## Projekt 1030 — der neue Anker für Mehrmodul-Kaskaden

Zwei BHKW-Module in Kaskade, Spitzenkessel, ein Pufferspeicher, gepflegter KWKG-Satz und
gepflegte Energiepreise. Genau dafür fehlte der Referenzmenge bisher ein Projekt.

**Vollbenutzungsstunden je Modul** (Migrationsschritt 18, Etappe E2 — erstmals persistiert):

| Modul | P_el | P_therm | Stromproduktion | Wärmeproduktion | VbhElektrisch | VbhThermisch |
|---|---|---|---|---|---|---|
| `BHKWModul[0]` — BHKW EW M 50 S [K] Erdgas | 50 kW | 81 kW | 373,78 MWh | 605,52 MWh | **7 475,69 h/a** | **7 475,59 h/a** |
| `BHKWModul[1]` — Agenitor 306 (250 kW el) Gas | 250 kW | 290 kW | 1 346,29 MWh | 1 561,69 MWh | **5 385,16 h/a** | **5 385,13 h/a** |

Thermische und elektrische Vbh sind je Modul praktisch identisch — der Motor fährt im festen
Verhältnis P_el zu P_therm. Genau das hatte E2 als Befund zur Modellstruktur festgehalten; das
Kaskadenprojekt bestätigt es an gepflegten Daten.

**Die drei Aggregatgrößen** — alle drei sind in `aggregate.csv` belegt:

| Größe | Schlüssel | Wert | Rechenweg |
|---|---|---|---|
| Summe thermisch über alle Module | `BHKW.Betriebsstunden_Gesamt` | **12 860,72 h/a** | 7 475,59 + 5 385,13 |
| ungewichtetes Mittel | `BHKW.Betriebsstunden_Durchschnitt` | **6 430,36 h/a** | 12 860,72 / 2 |
| leistungsgewichtet, elektrisch | `BHKW.VbhElektrisch` | **5 733,59 h/a** | 1 720,08 MWh / 300 kW |

Die Summe liegt erwartungsgemäß **über** 8 760 h — sie ist eine Summe über zwei Module und
keine Stundenzahl eines Jahres. Genau deshalb hat E2 die KWKG-Deckelung von ihr auf die
leistungsgewichtete elektrische Größe umgestellt, die die Jahresstundenzahl konstruktiv nicht
überschreiten kann. **Das Projekt zeigt den Unterschied erstmals an echten Daten: 12 860,72 h
gegenüber 5 733,59 h, Faktor 2,24.**

**KWKG-Erlös Jahr 1 mit Deckelung.** Die Wirtschaftlichkeit ist nicht Teil des CSV-Exports;
die Werte stammen aus einem Lauf von `BerichtsDatenSammler.Sammle` +
`WirtschaftlichkeitCtrl.Berechne` auf einer **Wegwerf-Kopie** derselben migrierten
Arbeitskopie (Sonde nach dem Lauf gebaut und wieder gelöscht):

| Größe | Wert |
|---|---|
| Parameter | Zins 3 %, T = 20 a, Bonus **4 ct/kWh** (Eigenverbrauch) / **8 ct/kWh** (Einspeisung), Kontingent 30 000 Vbh, Stichtag 01.09.2026, IBN 01.03.2027 |
| KWK-Strom, Eigenverbrauch (W3-Split) | 1 393,396 MWh |
| KWK-Strom, Einspeisung (W3-Split) | 326,681 MWh |
| ungedeckelter Zuschlag Jahr 1 | ≈ 81 870 € (aus dem auf 3 Nachkommastellen persistierten Split nachgerechnet) |
| Jahresdeckel § 8 Abs. 4 (Katalog `KWKG_VBH_JAHRESDECKEL`, Kalenderjahr 2027) | 3 100 Vbh |
| erreichte Vbh (Bemessungsgrundlage) | **5 733,59 h/a** — identisch mit `BHKW.VbhElektrisch` |
| Deckelungsfaktor | 3 100 / 5 733,59 = 0,5407 |
| **KWKG-Erlös Jahr 1** | **44 265,13 €** |
| Kontingentreichweite (30 000 Vbh, degressive Staffel 3 100 / 2 900 / 2 700 / 2 500 …) | rund 12 Jahre, liegt damit innerhalb T = 20 a |

Die Deckelung greift also und ist **bindend** — das Projekt deckt den Pfad ab, der in der
bisherigen Referenzmenge unbesetzt war (im Bestand steht `KWKG_Bonus` überall auf 0, damit war
E2 dort ergebnisneutral). Zur Einordnung, **rechnerisch abgeleitet** und nicht gemessen: Mit
der alten Bezugsgröße `Betriebsstunden_Gesamt` = 12 860,72 h ergäbe derselbe Fall einen
Deckelungsfaktor von 0,2410 und damit rund 19 734 € statt 44 265 € — die von E2 beschriebene
Unterschätzung bei Kaskaden, hier erstmals an einem gepflegten Projekt sichtbar.

**Wirtschaftlichkeit vollständig, keine „nicht bestimmbar"-Meldung.** Alle drei Szenarien
(Erwartet / Best / Worst) rechnen durch:

```
Fehlgrund        : <keiner>
Hinweis          : <keiner>
Investition      : 410.000,00 €
Energiekosten/a  : 1.124.957,70 €
Betriebskosten/a :    20.000,00 €
CO2AbgabeJahr    :    58.386,11 €
KwkgErloesJahr1  :    44.265,13 €
KwkgVbhElektrisch:     5.733,59 h/a
Kapitalwert      : -21.443.873,43 €
Gestehungskosten :         0,2348 €/kWh
```

Kein KWKG-Hinweis heißt: Beide Module liegen unter der Ausschreibungsgrenze von 500 kW und
beide fahren Erdgas — weder der 500-kW-Guard noch der Heizöl-Ausschluss greift. Damit deckt
1030 auch die **Positivseite** beider Guards ab, die bisher nur an präparierten Fällen belegt
war. Der Kapitalwert ist ein **absoluter** Nettobarwert (nahezu reine Kostenreihe über 20
Jahre) und deshalb erwartungsgemäß stark negativ; aussagekräftig sind die Differenzkennzahlen
gegen eine Variante, die dieses Projekt (noch) nicht führt.

**Weitere Kennwerte von 1030 aus `aggregate.csv`:** Wärmebedarf 6 137,56 MWh (vollständig als
`waermebedarf_extern`, ohne Gebäude-/Brauchwasser-/Prozessanteil), Wärmelast max. 2 206 kW,
Strombedarf 4 790,09 MWh; BHKW-Wärmedeckung 35,3 %, Stromdeckung 35,91 %, Gasverbrauch BHKW
4 423,19 MWh; Kessel-Wärmedeckung 64,7 %, Gasverbrauch 3 972,09 MWh; Restwärmebedarf **0**.

### Zwei Auffälligkeiten, die gemeldet und nicht geglättet werden

1. **`HeizkesselModul[0]` führt `Waermeproduktion = 0`, `Verbrauch = 0` und einen leeren
   `Brennstoff`**, während der Kessel als Gewerk 3 972,09 MWh produziert und
   `HeizkesselModul[0].carrier_id = 63` gepflegt ist. Der Modulblock der Kessel ist also
   **nicht** befüllt, obwohl der Träger zugeordnet ist. Das ist kein Effekt von 1030 —
   dasselbe Muster steht in B4 und B5 bei allen Kesselprojekten. Für die modulscharfe
   Kostenrechnung (E3/E6) ist das eine offene Baustelle.
2. **Der Pufferspeicher steht das ganze Jahr auf demselben Füllstand**:
   `SOC_Mittel = SOC_Max = SOC_Ende = 550,86 kWh` bei `Q_max = 580 kWh`, dabei
   `Ladung_gesamt = 2 947 361 kWh` und `Entladung_gesamt = 2 945 596 kWh`. Laden und Entladen
   fallen in dieselbe Stunde; die stündliche Momentaufnahme sieht deshalb konstant aus und
   `Vollzyklen = 5 081,66` ist faktisch ein Durchsatzverhältnis, keine Zyklenzahl. Auch das
   ist **kein 1030-Effekt**: Projekt 1018 zeigt dasselbe Muster (SOC konstant 21,00 kWh,
   Vollzyklen 1 714,06). Eine Kennzahl, die man so nicht berichten sollte — vermerkt, nicht
   korrigiert.

## Warum **nicht** gegen B4 verglichen wird

Zwischen B4 und B5 haben sich **Code und Daten gleichzeitig** geändert. Ein Toleranzvergleich
liefert deshalb keine verwertbare PASS/FAIL-Aussage und wird bewusst nicht als Kriterium
geführt. Zur Einordnung trotzdem der Byte-Vergleich über die acht gemeinsamen Projekte:

| Projekt | byte-gleich | abweichend | neue Dateien | wesentliche Ursache |
|---|---|---|---|---|
| 1008 | 21/21 | 0 | 0 | — |
| 1021 | 21/21 | 0 | 0 | — |
| 1023 | 24/25 | 1 (`aggregate.csv`) | 0 | genau **ein** geänderter Wert: `HeizkesselModul[0].carrier_id` — Datenpflege |
| 1011 | 25/29 | 4 | 0 | Batteriepfad (siehe unten) + `WaermepumpeModul[1].Modul` (Datenpflege) |
| 1007 | 22/29 | 7 | 0 | Batteriepfad und PV-Überschuss |
| 1017 | 11/20 | 9 | 1 | BHKW-Speicherstufe + Batteriepfad + E2-Kennzahlen |
| 1024 | 11/26 | 15 | 0 | BHKW-Speicherstufe, Pufferpersistenz, E2-Kennzahlen |
| 1018 | 6/19 | 13 | 3 | **zweites BHKW-Modul entfernt**, Puffer neu zugeordnet — Datenpflege |

Die Abweichungen ordnen sich vier Ursachen zu:

1. **Neue Kennzahlspalten aus E2 / Migrationsschritt 18.** In jedem BHKW-Projekt kommen
   `BHKW.VbhElektrisch`, `BHKWModul[i].VbhThermisch` und `BHKWModul[i].VbhElektrisch` hinzu —
   rein additiv, ohne Rechenwirkung.
2. **Der einkanalige BHKW-Altpfad ist entfallen** (Paket BHKW-Regulär). BHKW-Projekte rechnen
   jetzt immer über die Speicherstufe mit herausgelöster Ladephase; damit ändern sich in
   1017, 1018, 1024 und 1030 die BHKW- und Kesselganglinien, und die Pufferpersistenz
   (`Pufferspeicher[0].*`, `puffer_*.csv`) tritt hinzu, wo vorher keine stand.
3. **Der Stromspeicher hängt an der SpeicherEngine.** Das Werkzeug hat mit `e596296`
   nachgezogen: `ssp_gespeichert_viertelstunde.csv` kommt jetzt aus
   `SimulationControl.Speicherfuellstand_viertelstuendlich` und `pv_speicherfuellstand.csv`
   aus `…_stuendlich` — statt aus dem abgelösten `SimulationSSP`-Stub bzw. dem PV-Modul. In
   1007, 1011 und 1017 ändern sich dadurch Speicher-, Reststrom- und PV-Überschussreihen; in
   1017 wandert `pv_speicherfuellstand.csv` vom PV- in den Speicherblock und erscheint dort
   erstmals.
4. **Datenpflege des Anwenders.** In 1018 fehlt das zweite BHKW-Modul und ein Puffer ist neu
   zugeordnet; in 1011 trägt das zweite WP-Modul einen anderen Namen; in 1023 ist ein
   Energieträger nachgetragen. Diese Änderungen sind der Grund, warum B4 als Maßstab
   ausgefallen ist.

**B4 bleibt unverändert liegen** — als Vergleichsmaßstab abgelöst, als Beleg des Standes vom
16.08.2026 weiter gültig.

## Nachweise

**Selbstvergleich (Reproduzierbarkeit/Determinismus).** Zweiter `lauf` desselben Codes auf
derselben Quelle, Ziel außerhalb des Repos, ohne Ausschluss:

```
vergleich 2026-08-19_B5  <lauf2>
Projekt_1007: PASS (29 Dateien, 324210 Werte)
Projekt_1008: PASS (21 Dateien, 227847 Werte)
Projekt_1011: PASS (29 Dateien, 324232 Werte)
Projekt_1017: PASS (21 Dateien, 254143 Werte)
Projekt_1018: PASS (22 Dateien, 236642 Werte)
Projekt_1021: PASS (21 Dateien, 227840 Werte)
Projekt_1023: PASS (25 Dateien, 262918 Werte)
Projekt_1024: PASS (26 Dateien, 271695 Werte)
Projekt_1030: PASS (22 Dateien, 236650 Werte)

GESAMT: PASS (2366177 Werte innerhalb der Toleranz)      Exit-Code 0
Byte-/MD5-Vergleich: 216 von 216 Dateien gleich, 0 abweichend
```

**9/9 PASS, 216/216 byte-gleich** — die Basis ist reproduzierbar.

**Produktive Datenbank unberührt.** Vor dem ersten Lauf und nach allen Läufen und Proben:

| | vorher (03:36) | nachher (03:57) |
|---|---|---|
| Größe | 96 436 224 Byte | 96 436 224 Byte |
| Zeitstempel | 19.08.2026 02:51:17.069 | 19.08.2026 02:51:17.069 |
| MD5 | `66F4806A3B89074B52344F39D477F151` | `66F4806A3B89074B52344F39D477F151` |
| `Kenndaten.laccdb` | nicht vorhanden | nicht vorhanden |

Die Migration 17 → 18 lief ausschließlich auf der Arbeitskopie; die produktive Datei steht
weiter auf Schemastand 17.

**Frühere Basis:** `../2026-08-16_B4/` bleibt unangetastet liegen (Codestand `3fd2787`,
Schemastand 10, acht Projekte, 190 CSV).

## Was hier liegt

Nur CSV-Dateien und die beiden Protokolle — **keine `.accdb`**. Die Arbeitskopie lag außerhalb
des Repos und ist gelöscht; eine Datenbankkopie im Basisordner wäre über `.gitignore`
ausgeschlossen und machte die Basis unvollständig übertragbar. Umfang: 216 CSV, 32 MB.

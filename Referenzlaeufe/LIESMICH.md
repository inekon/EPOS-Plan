# Referenzlauf-Suite (Paket B1)

Regressionsbasis für den Simulationskern von EPOS-Plan.

Vor jedem Umbau an der Engine wird der aktuelle Stand als CSV eingefroren; nach dem Umbau
läuft derselbe Satz Projekte erneut und wird mit Toleranz gegen den eingefrorenen Stand
verglichen. Was sich dabei ändert, ist entweder gewollt — dann wird die Referenz neu
gesetzt — oder ein Fehler.

Grundlage: `WindowsFormsApplication1/Allgemein/Simulation/Konzept_Simulation_QuellenSenken.md`,
Paket B1, Kapitel 9.

## Aktuelle Basis

**`2026-08-19_B6/`** — seit dem 19.08.2026, 17:16 Uhr die gültige Referenz,
**neun Projekte** (1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024, **1030**), **216 CSV,
2 366 177 Werte**. Jeder neue Vergleich läuft gegen diesen Ordner.

> **B6 ist mit B5 byte-identisch** (9/9 PASS, 216/216 gleich). Der Basiswechsel erfolgt allein wegen
> der **Zuordnung**: Gerechnet ist B6 mit dem Codestand nach Abschluss der Ausbaustufe W4
> (`e94be10`, Etappen E3 bis E7 plus den zusammengeführten KI-Strang) und auf einer Quelle mit
> **Schemastand 21** statt 17. Damit lässt sich eine spätere Abweichung zweifelsfrei einer
> Folgeänderung zuschreiben. Einzelheiten und — wichtiger — die Liste **„Was diese Basis nicht
> absichert"** stehen im [Laufprotokoll der Basis](2026-08-19_B6/lauf_protokoll.md).

> **Der Referenzlauf deckt den Rechenkern ab, nicht die Wirtschaftlichkeit.** Er ruft
> `WirtschaftlichkeitCtrl` nicht auf. Kapitalwert, KWK-Zuschlag, Steuergutschriften, Tarife und
> Betriebskosten stehen in **keiner** eingefrorenen Basis; sie werden je Etappe als A/B gegen den
> Vorgängerstand gemessen. Ein grünes 216/216 sagt über die Ausbaustufe W4 deshalb nur, dass sie den
> Rechenkern nicht berührt hat.

> **Die Projektliste ist FEST VORGEGEBEN und muss bei jedem Folgelauf mitgegeben werden:**
>
> ```powershell
> & $exe lauf --ziel <ordner> --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030
> ```
>
> Ohne `--projekte` wählt die Suite datengetrieben — und diese Wahl **wandert mit dem
> Projektbestand**. Mit den Beispielprojekten 1026–1029 zieht sie inzwischen 1012 und 1026
> herein und lässt 1008 und 1018 fallen. Für eine über die Zeit vergleichbare Basis ist das
> untauglich: Die Ordner ließen sich nicht mehr gegeneinander stellen. B5 friert deshalb die
> acht Projekte von B4 **plus** das neue Kaskadenprojekt ein. Die automatische Auswahl bleibt
> als Werkzeug erhalten (`liste`), taugt aber nur zum Sichten der Projektlandschaft, nicht
> zum Einfrieren einer Basis.

> **Projekt 1030 „Referenz BHKW-Kaskade (Regressionstest)" ist der Anker für
> Mehrmodul-Kaskaden.** Zwei BHKW-Module (50 kW el / 250 kW el), Spitzenkessel,
> Pufferspeicher, gepflegter KWKG-Satz und gepflegte Energiepreise. Es deckt als **einziges**
> Projekt der Referenzmenge ab: die drei Vollbenutzungsstunden-Aggregate aus Etappe E2
> (Summe thermisch 12 860,72 h, ungewichtetes Mittel 6 430,36 h, leistungsgewichtet
> elektrisch 5 733,59 h), eine **bindende** KWKG-Deckelung (Erlös Jahr 1 44 265,13 € bei
> Jahresdeckel 3 100 Vbh) und die Positivseite beider KWKG-Guards (beide Module unter der
> 500-kW-Ausschreibungsgrenze, beide Erdgas). Im übrigen Bestand steht `KWKG_Bonus` auf 0 —
> ohne 1030 wäre dieser ganze Pfad ungetestet. **Wird 1030 verändert oder gelöscht, verliert
> die Referenzmenge ihre einzige Abdeckung dieses Pfades.**

> **Das Feature-Flag `Kaskade_Zweikanalig` beschreibt die Basis nicht mehr pauschal.** Für
> **BHKW-Projekte** (1017, 1018, 1024, 1030) ist es seit Paket BHKW-Regulär **wirkungslos** —
> sie rechnen immer über die Speicherstufe mit herausgelöster Ladephase, der einkanalige
> BHKW-Altpfad ist entfallen; die Engine meldet das je Projekt als `Simulation Hinweis`. Für
> die fünf übrigen Projekte steht das Flag auf **AUS**. Im Datenbestand steht es bei 1018
> inzwischen auf WAHR — folgenlos, weil 1018 ein BHKW-Projekt ist.

### Warum die Basis auf B6 gewechselt wurde

**Nicht wegen geänderter Zahlen — es hat sich keine geändert.** Der Vergleich B5 → B6 ist
**9/9 PASS und 216/216 byte-gleich**. Gewechselt wird wegen der **Zuordnung**: Zwischen beiden Ständen
liegen die Etappen **E3 bis E7** der Ausbaustufe W4 (Migrationsschritte 19 bis 22) und der
zusammengeführte Strang **KI-Assistent-Aufgabensteuerung**; die Quelldatenbank ist außerdem durch die
Sitzung des Anwenders von Schemastand **17 auf 21** gewandert und einmal komprimiert worden. Ab B6
ist die gültige Basis mit diesem Code- und Schemastand gerechnet, und eine spätere Abweichung lässt
sich zweifelsfrei einer Folgeänderung zuschreiben statt W4.

**Codestand:** `e94be10`, unverändert, gebaut aus einem `git archive HEAD`-Export außerhalb des Repos
(`C:\Waermeplan\_e8`; 0 Fehler, 6 Bestandswarnungen). **Datenquelle:** produktive `Kenndaten.accdb`,
Zeitstempel **19.08.2026 14:46**, Schemastand **21**, nur gelesen (keine `Kenndaten.laccdb`). Die
Migration **21 → 22** lief ausschließlich auf der Arbeitskopie. **Selbstvergleich:** zweiter Lauf
desselben Codes auf derselben Quelle **9/9 PASS, 216/216 byte-gleich** — die Basis ist reproduzierbar.
Vollständige Angaben und die Liste **„Was diese Basis nicht absichert"** im
[Laufprotokoll der Basis](2026-08-19_B6/lauf_protokoll.md).

### Warum die Basis auf B5 gewechselt wurde

**B4 war als Maßstab ausgefallen.** Ein Lauf des unveränderten Codes gegen B4 endete zuletzt
in **7 von 8 Projekten mit FAIL** — Ursache waren Datenänderungen des Anwenders, nicht der
Code. Dazu kamen die Codeetappen seit dem 16.08.2026: Migrationsschritte **17 und 18**, der
Katalog gesetzlicher Parameter (**E1**), die Vollbenutzungsstunden-Korrektur (**E2**), die
500-kW-Grenze **je Anlage** und der Heizöl-Ausschluss **je Anlage**.

**Codestand:** `ef8e537`, unverändert, gebaut aus einem `git archive HEAD`-Export außerhalb
des Repos (`C:\Waermeplan\_b5`; 0 Fehler, 6 Bestandswarnungen). **Datenquelle:** produktive
`Kenndaten.accdb`, Zeitstempel **19.08.2026 02:51**, Schemastand **17**, nur gelesen (keine
`Kenndaten.laccdb`). Die Migration **17 → 18** lief ausschließlich auf der Arbeitskopie; die
produktive Datei steht nachweislich weiter auf Schemastand 17 (Zeitstempel, Größe und MD5 vor
und nach dem Lauf identisch).

**Ein Toleranzvergleich B4 → B5 wird bewusst NICHT als PASS/FAIL geführt** — Code und Daten
haben sich gleichzeitig geändert, das Ergebnis wäre nicht zuordenbar. Die Einordnung, welche
Größen sich unterscheiden und warum (neue E2-Kennzahlspalten, entfallener einkanaliger
BHKW-Altpfad, Stromspeicher über die SpeicherEngine, Datenpflege des Anwenders), steht im
[Laufprotokoll der Basis](2026-08-19_B5/lauf_protokoll.md). Zwei Projekte (1008, 1021) sind
gegenüber B4 unverändert byte-gleich.

**Selbstvergleich der neuen Basis:** Ein zweiter Lauf desselben Codes auf derselben Quelle
ergibt **9/9 PASS (2 366 177 Werte)** und **216/216 byte-/MD5-gleich** — die Basis ist
reproduzierbar.

## Frühere Stände

`2026-08-19_B5/` bleibt als **vorheriger Stand** liegen (Codestand `ef8e537`, Quelle mit Schemastand
17, neun Projekte, 216 CSV, 2 366 177 Werte) — **für alle 216 Dateien byte-gleich mit B6** und damit
der Beleg des Standes vor den Etappen E3 bis E7. Warum B5 seinerzeit gesetzt wurde, steht im
Abschnitt „Warum die Basis auf B5 gewechselt wurde" darüber.

`2026-08-16_B4/` bleibt als **älterer Stand** liegen (Codestand `3fd2787`, Schemastand 10,
acht Projekte, 190 CSV, 2 094 451 Werte, Feature-Flag `Kaskade_Zweikanalig` durchgehend AUS).
Warum B4 seinerzeit gesetzt wurde:

**Ein Anlass: die neue Ergebnisspalte aus Etappe D4.** Vollständige Zuordnung je Projekt im
[Laufprotokoll der Basis](2026-08-16_B4/lauf_protokoll.md).

Etappe **D4** hat `Tab_ErgebnisHeizkessel.Quellwaerme` eingeführt — **Migrationsschritt 10**,
rein additives DDL, Schema-Zielstand **9 → 10**. Weil der Export `SELECT * FROM Tab_Ergebnis*`
liest, führt `aggregate.csv` je Projekt mit Heizkessel-Ergebniszeile einen Schlüssel mehr.
Gegen B3 meldete der Vergleich das als „Eintrag nur im Vergleichslauf" — fachlich richtig,
aber dauerhaft erklärungsbedürftig. **B4 friert den D4-Stand einschließlich der neuen Spalte
ein; künftige Vergleiche laufen wieder ohne `--ohne`.**

**Codestand:** `3fd2787`, unverändert, gebaut aus einem `git archive`-Export außerhalb des
Repos (0 Fehler, 6 Bestandswarnungen). **Datenquelle:** produktive `Kenndaten.accdb`,
Zeitstempel **15.08.2026 22:50** (Datei 23:22), Schemastand **9**, nur gelesen (keine
`Kenndaten.laccdb`). Schritt 10 lief ausschließlich auf der Arbeitskopie — die produktive
Datei steht nachweislich weiter auf Schemastand 9.

**Zuordnung B3 → B4, Projekt für Projekt:**

| Projekt | Abweichung zu B3 | Ursache |
|---|---|---|
| 1007, 1008, 1011, 1021 | **keine — byte-/MD5-gleich** | kein Heizkessel-Ergebnisdatensatz |
| 1017, 1018, 1023, 1024 | **je ein neuer Schlüssel** in `aggregate.csv` (`Heizkessel.Quellwaerme;0`) | Migrationsschritt 10 / Etappe D4 |

Byte-Vergleich: **186 von 190 gleich**, die vier Abweichungen sind ausschließlich die
`aggregate.csv` der vier Heizkessel-Projekte, Zeilendiff je genau eine eingefügte Zeile. Alle
Ganglinien sind in allen acht Projekten byte-gleich.

```
vergleich 2026-08-15_B3 2026-08-16_B4 --ohne Heizkessel.Quellwaerme
  → 8/8 PASS (2 094 451 Werte)
```

**Kein Altwert weicht ab** — D4 hat keinen Rechenweg verändert.

> **Auffällig:** `Heizkessel.Quellwaerme` steht in allen vier Projekten auf **0** — kein Kessel
> der Referenzmenge hängt an einem Quellpuffer. Die Spalte ist damit im Vergleich enthalten,
> aber noch nicht mit einem Wert ungleich null abgedeckt (wie `Erdreich[i].*` seit Paket 7).
> Für einen belastbaren Regressionstest dieses Pfades fehlt ein Referenzprojekt mit Kessel an
> einem Quellpuffer.

**Selbstvergleich von B4 seinerzeit:** Ein zweiter Lauf desselben Codes auf derselben Quelle
ergab **8/8 PASS (2 094 451 Werte)** und **190/190 byte-/MD5-gleich**.

`2026-08-15_B3/` bleibt als **vorvorheriger Stand** liegen (Codestand `a0a623a` + K-3,
Schemastand 9, acht Projekte, 190 CSV) — für alle Werte außer der neuen Spalte
byte-gleich mit B4. Warum B3 seinerzeit gesetzt wurde:

**Zwei Anlässe — beide getrennt nachgewiesen.** Vollständige Zuordnung je Projekt im
[Laufprotokoll der Basis](2026-08-15_B3/lauf_protokoll.md).

**(1) Ergebnisänderung K-3.** Die Bivalenz-Umschaltung des bivalent-alternativen
Wärmepumpenbetriebs schaltet ab jetzt an der **Bivalenztemperatur**
(`Tab_Energieanlagen.Abschaltpunkt`) statt stundenweise nach Leistungsunterdeckung — in
beiden Rechenwegen. Umsetzung, Datenbefund, Regelentscheidung und alle Zahlen:
[`../WindowsFormsApplication1/Allgemein/Simulation/K3_BivalenzTemperatur_Protokoll.md`](../WindowsFormsApplication1/Allgemein/Simulation/K3_BivalenzTemperatur_Protokoll.md).

**Davon betroffene Referenzprojekte: keines.** Der Datenbefund vor dem Lauf zeigt, dass im
gesamten Bestand **keine einzige** Anlage `Bivalenter_Betrieb = TRUE` **und**
`Betriebsart = "Alternativbetrieb"` führt — der geänderte Zweig ist in keinem gespeicherten
Projekt aktiv. (Die eine `Alternativbetrieb`-Zeile, Anlage 10132 in Projekt 1008, trägt
`Bivalenter_Betrieb = False`; die Bedingung ist eine Und-Verknüpfung.) Dementsprechend:

```
A/B gegen a0a623a, Flag AUS : 9/9 PASS (2 295 987 Werte), 208/208 byte-/MD5-gleich
A/B gegen a0a623a, Flag AN  : 9/9 PASS (2 295 998 Werte), 208/208 byte-/MD5-gleich
```

Der A/B-Lauf umfasst noch **neun** Projekte: Er lief auf einer gemeinsamen Datenbankkopie
vom 22:26 Uhr — also vor der Löschung unten — und deckt Projekt 1010 damit mit ab.

Wirksam ist K-3 sehr wohl — nachgewiesen an eigens präparierten Kopien der Projekte **1026**
(WP + Kessel + Puffer, auf `Alternativbetrieb` gestellt: WP-Produktion 28,3 → 40,2 MWh,
Kessel 36,4 → 24,6 MWh, WP-Ein/Aus-Wechsel einkanalig 2 962 → 2 524 und zweikanalig
**1 126 → 140**, Frostbetrieb der WP 330 h → 0 h) und **1024** (Sommer-Warmwassermuster:
**714 Sommerstunden**, in denen die WP bisher an Warmwasserspitzen ausfiel, laufen wieder mit
der Wärmepumpe). Stundengenaue Bilanzproben schließen in allen Varianten (max. Abweichung
7·10⁻⁶ kWh, 0 Stunden über 0,01 kWh).

**(2) Projektlöschung durch den Anwender.** Am 15.08.2026 gegen 22:50 Uhr hat der Anwender
die Projekte **1010, 1016, 1020 und 1025** aus der produktiven Datenbank gelöscht. Von der
Referenzmenge trifft das **1010 „Kurs EE"** — es existiert nicht mehr. **B3 umfasst deshalb
acht Projekte, B2 hatte neun.**

> **Folgebedarf:** 1010 war in der Referenzmenge die Kategorie **„Wärmepumpe ohne weitere
> Erzeuger"** (`Anlagen: WP`). Fällt sie dauerhaft weg, sollte ein Ersatzprojekt derselben
> Kategorie nachrücken (`Projektauswahl.MAX_PROJEKTE` steht auf 9).

**Zuordnung B2 → B3, Projekt für Projekt:**

| Projekt | Abweichung zu B2 | Ursache |
|---|---|---|
| 1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024 | **keine — alle 190 Dateien byte-/MD5-gleich** | — |
| 1010 | **Ordner entfällt** (18 Dateien) | **Projektlöschung**, kein Codeeffekt |

Für die acht verbliebenen Projekte ist B3 also wertgleich mit B2 bis auf das Byte; **kein
einziger Wert weicht durch K-3 ab**. Der Basiswechsel erfolgt damit aus zwei Gründen, von
denen keiner „geänderte Zahlen" heißt: die geschrumpfte Projektmenge und die **Zuordnung** —
ab hier ist die gültige Basis mit dem K-3-Code gerechnet, und eine spätere Abweichung lässt
sich zweifelsfrei einer Folgeänderung zuschreiben statt K-3.

**Selbstvergleich der neuen Basis:** Ein zweiter Lauf desselben Codes auf derselben Quelle
ergibt **8/8 PASS (2 094 447 Werte)** und **190/190 byte-/MD5-gleich** — die Basis ist
reproduzierbar.

**Datenquelle:** produktive `Kenndaten.accdb`, Zeitstempel **15.08.2026 22:50**, nur gelesen
(keine `Kenndaten.laccdb`).

`2026-08-15_B2/` bleibt als **älterer Stand** liegen (Codestand `925c37f`, Datenstand
15.08.2026 11:58, **neun** Projekte) — für die acht gemeinsamen Projekte byte-gleich mit B3
und die einzige verbliebene Quelle für die Ganglinien des gelöschten Projekts 1010. Warum B2
seinerzeit gesetzt wurde:

Gerechnet auf Codestand **`925c37f`** (Paket 9, Etappe 2) und auf der produktiven
`Kenndaten.accdb` mit Zeitstempel **15.08.2026 11:58**. Ein Codeeffekt liegt dem Wechsel
**nicht** zugrunde — die Ursache sind **geänderte Projektdaten**:

Der Anwender hat am 15.08.2026 um 11:58 in **Projekt 1024** das **zweite Wärmepumpenmodul**
(`CS7800iLW 12`) entfernt. Damit fehlt im `aggregate.csv` der komplette Block
`WaermepumpeModul[1]`, und die davon abhängigen Ganglinien (BHKW, Kessel, WP, Heizstab,
Restwärme, Reststrom) verschieben sich. Der Vergleich der alten gegen die neue Basis zeigt
das sauber abgegrenzt:

```
2026-08-14_B1-Fixes vs 2026-08-15_B2 : 193 byte-/MD5-gleich, 15 abweichend
                                       (alle 15 in Projekt_1024)
Toleranzvergleich                    : 8 x PASS, Projekt_1024 FAIL (75.575 Abweichungen)
```

Der Nachweis, dass das **nicht** vom Code kommt, steht in
`../WindowsFormsApplication1/Allgemein/Simulation/Paket9_Lokalisierung_Protokoll.md`,
Abschnitt 12.2: Ein Baselinelauf aus einem eigenen git-Arbeitsbaum auf `d49075e` — also
**ohne** die Änderungen der Etappe 2 — zeigt gegen `B1-Fixes` **dieselben 15 Dateien**;
gegen den Etappe-2-Lauf auf demselben Datenstand sind alle 208 Dateien byte-gleich.

Solange `B1-Fixes` die Basis bliebe, schleppte jede Folgeprüfung diese eine
erklärungsbedürftige Abweichung mit und Projekt 1024 wäre dauerhaft FAIL — der Regressionstest
verlöre für dieses Projekt seine Aussagekraft. Deshalb der Basiswechsel.

**Selbstvergleich der neuen Basis:** Ein zweiter Lauf desselben Codes auf derselben Quelle
ergibt **9 von 9 PASS (2.295.987 Werte)** und **208 von 208 Dateien byte-/MD5-gleich** — die
Basis ist reproduzierbar.

Die Anwendung des Anwenders lief während des Laufs, hatte die Datenbank aber **nicht**
geöffnet (keine `Kenndaten.laccdb`). Die produktive Datei wurde ausschließlich gelesen.

`2026-08-14_B1-Fixes/` bleibt als **älterer Stand** liegen (Datenstand vom 14.08.2026,
neun Projekte). Gegenüber `2026-08-14_Paket4` weichen dort **drei Projekte** ab, vollständig
zugeordnet in
`2026-08-14_B1-Fixes/vergleich_protokoll.md`: **1008** und **1011** durch die
Bestandsfehler-Fixes **B1-F1/B1-F2** (Stromganglinien fließen erstmals in den Strombedarf
ein; Prozesswärme war still 0 — B0-Protokoll, Nachtrag B1-F1/B1-F2), **1024** durch
**geänderte Projektdaten** (Heizkessel nach dem Paket4-Snapshot in die Kaskade
aufgenommen; Alt- vs. Neu-Code auf identischer DB ist für 1024 vollständig PASS —
kein Code-Effekt). Die übrigen sechs Projekte: PASS.

`2026-08-14_Paket4/` bleibt als **älterer Stand** liegen. Gegenüber
`2026-08-14_Paket7` waren dort genau **drei** Werte neu, alle in Projekt 1021 und alle
begründet in `2026-08-14_Paket4/lauf_protokoll.md`: die ID-Semantik des Quellspeichers
(`Pufferspeicher[0].ID_Pufferspeicher` 8 → 1018014) und die beiden laufzeitbasierten
Skalare aus dem Bestandsfehler **B0-13** (`WaermepumpeModul[0].Betriebsstunden`
6692,41 → 4,41; `Waermepumpe.Vollbenutzungsstunden` 3846,66 → 502,66). Alle übrigen
2.260.920 Werte sind byte-genau gleich.

`2026-08-14_Paket7/` und `2026-08-14_B0/` bleiben als **historische Stände** liegen
(Paket7: vor Paket 1/2/4, B0-12/13 und B1-Fixes; B0: vor Paket 1/3/7, acht Projekte).
Ein Vergleich gegen B0
meldet zwangsläufig FAIL — der Basiswechsel ist gewollt und in
`2026-08-14_Paket7/vergleich_protokoll.md` sowie in
`../WindowsFormsApplication1/Allgemein/Simulation/Paket7_Ergebnis_Anzeigen_Protokoll.md`
begründet:

| Was | Alt (B0) | Neu (Paket 7) |
|---|---|---|
| Projektmenge | acht | neun — **1021** kommt hinzu und deckt als einziges den Quellspeicher-Pfad ab |
| `Waermepumpe.Kapazitaet_Pufferspeicher` | `Volumen · 1,16` aus dem WP-Datensatz (in allen Projekten 11,6) | `SimulationPufferspeicher.Q_max` des zugeordneten Puffers; 0 ohne Puffer |
| Pufferspeicher-Persistenz | gab es nicht | `Pufferspeicher[i].*` je Speicher in `aggregate.csv` (aus `Tab_ErgebnisPufferspeicher`) |
| Speicher-Kennzahlen | gab es nicht | `Puffer.SOC_Mittel`, `Puffer.SOC_Max`, `Puffer.Vollzyklen`, `Sim.Speicher_Anzahl` |
| Quellspeicher-Ganglinien | gab es nicht | `quellspeicher_<AnlagenID>_{soc,ladung,entladung}.csv` (nur in 1021) |
| Erdreich-Auslegungsprüfung | gab es nicht | `Erdreich[i].*` in `aggregate.csv` — **nur** bei Projekten mit `WQ_Typ = 'Erdreich'`, in der Referenzmenge also nirgends |

Gerechnet wurde die neue Basis auf einer **eigenen, vollständig migrierten Kopie außerhalb
des Repos** im Modus `projekt` (siehe `2026-08-14_Paket7/lauf_protokoll.md`).

## Was hier liegt

| Pfad | Inhalt |
|---|---|
| `<yyyy-MM-dd>_<Marke>/` | Ein eingefrorener Lauf: je Projekt ein Unterordner `Projekt_<ID>/`, dazu `lauf_protokoll.md` |
| `<...>/Projekt_<ID>/aggregate.csv` | Alle Skalare des Laufs: `Tab_Ergebnis*`-Zeilen, Restgrößen aus `SimulationControl`, Jahressumme jedes Vektors |
| `<...>/Projekt_<ID>/*.csv` | Die Ganglinien: 8760 Stundenwerte bzw. 35040 Viertelstundenwerte, `Index;Wert` |
| `Arbeitskopie/` | Die Kopie der Datenbank, auf der gerechnet wird. Wird bei jedem `lauf` neu angelegt. Nicht im Git (`Kenndaten.accdb` ist in `.gitignore`) |

Der Werkzeugcode liegt in `../Referenzlauf/`.

## Die wichtigste Regel

**Die produktive `Kenndaten.accdb` wird nie beschrieben.**

Die Suite kopiert sie nach `Referenzlaeufe/Arbeitskopie/`, biegt den DB-Pfad der Anwendung
per Reflection auf diesen Ordner um und prüft anschließend über
`DataRepository.GetDBPath()` nach, dass die Anwendung wirklich auf der Kopie arbeitet.
Zeigt der Pfad woanders hin — oder auf eine der bekannten produktiven Ablagen — bricht der
Lauf sofort ab. Auch jeder Kindprozess prüft das für sich noch einmal.

Liegt neben der Quelle eine `Kenndaten.laccdb`, ist die Datenbank gerade geöffnet. Kopiert
wird trotzdem (lesend), aber das Protokoll weist darauf hin: die Kopie kann dann Änderungen
der laufenden Sitzung noch nicht enthalten. Für einen belastbaren Referenzlauf die
Anwendung vorher schließen.

## Bauen

Nur über das MSBuild von Visual Studio — `dotnet build` scheitert an MSB4803
(COM-Referenzen des App-Projekts).

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    C:\Waermeplan\WP_Plan\Referenzlauf\Referenzlauf.csproj `
    -p:Configuration=Debug -p:Platform=x86
```

Beim allerersten Mal davor einmal `-t:Restore` mit denselben Parametern. Das Projekt ist
bewusst **nicht** Teil von `WP-Plan.sln`.

Ergebnis: `Referenzlauf\bin\x86\Debug\net8.0-windows\Referenzlauf.exe`

## Bedienung

```powershell
$exe = "C:\Waermeplan\WP_Plan\Referenzlauf\bin\x86\Debug\net8.0-windows\Referenzlauf.exe"
```

### `lauf` — Stand einfrieren

```powershell
& $exe lauf                                  # Ziel: Referenzlaeufe\<heute>_B0
& $exe lauf --ziel D:\Temp\NachUmbau         # anderer Zielordner
& $exe lauf --projekte 1010,1023             # feste Projektliste statt Automatik
& $exe lauf --timeout 600                    # Zeitlimit je Projekt in Sekunden (Standard 300)
```

Kopiert die Datenbank, **migriert sie auf den Zielstand des Schemas**, wählt die Projekte,
rechnet und schreibt CSVs plus `lauf_protokoll.md`. Exit-Code 0, wenn alle Projekte
durchgelaufen sind.

Die Migration (Schritt 2b) gehört seit der Paket-7-Nacharbeit dazu. Vorher rechnete `lauf`
auf einer Kopie im Stand der Quelldatenbank: fehlende Spalten und eine fehlende
`Tab_ErgebnisPufferspeicher` wurden nur von den Rückfallebenen im Anwendungscode
notdürftig ausgeglichen, und das Ergebnis war mit einem Lauf auf einer migrierten
Datenbank nicht vergleichbar. Die Migration ist idempotent — auf einer aktuellen Kopie
ist sie ein No-op.

### `vergleich` — gegen die Referenz prüfen

```powershell
& $exe vergleich <refOrdner> <neuOrdner>
& $exe vergleich <refOrdner> <neuOrdner> --ohne Heizkessel.Quellwaerme,Weiterer.Schluessel
```

Exit-Code 0 = alles PASS, 1 = mindestens ein FAIL. Je Projekt werden die zehn größten
Abweichungen ausgegeben, sortiert nach dem Vielfachen der erlaubten Toleranz.

`--ohne` (seit Etappe D4) nimmt **ausdrücklich benannte** Schlüssel vom Vergleich aus und
nennt sie in der Ausgabe. Der Zweck ist eng: Führt eine Etappe eine neue **Ergebnisspalte**
ein, wächst `aggregate.csv` zwangsläufig um einen Schlüssel, und gegen die eingefrorene Basis
verdeckt diese Meldung die eigentliche Frage — *sind die Altwerte unverändert?* Genau dafür
ist die Option da, **nicht** um Abweichungen wegzuschalten. Sobald die Basis neu gesetzt ist
(zuletzt: B6), laufen die Vergleiche wieder ohne Ausschluss.

### `pruefen` — Plausibilität eines Laufs

```powershell
& $exe pruefen <ordner>
```

Prüft Rasterlänge (8760 oder 35040 Zeilen), NaN/Inf und Jahressummen größer null dort, wo
dem Projekt ein Modul zugeordnet ist. Ein aktiviertes Gewerk ohne Modul ergibt zwangsläufig
null und wird nur als Hinweis gemeldet.

### `liste` — Projektlandschaft ansehen

```powershell
& $exe liste                                 # legt die Arbeitskopie neu an
& $exe liste C:\Waermeplan\Paket7_Nach\DB_Basis   # liest eine vorhandene Kopie
```

Zeigt alle Projekte mit Ausstattung und die automatische Auswahl samt Begründung, ohne zu
rechnen. Mit Ordnerargument wird **nichts kopiert** — so lässt sich die Auswahl auf einer
eigenen Kopie außerhalb des Repos nachprüfen, ohne die `Arbeitskopie` eines laufenden
Vergleichs zu überschreiben.

## Toleranzen

Für Skalare und für jedes einzelne Vektorelement gilt dieselbe Regel:

| Wertebereich | Toleranz |
|---|---|
| Betrag ≥ 1 | relative Abweichung bis **1e-4** |
| Betrag < 1 | absolute Abweichung bis **0,01** |

Nichtnumerische Werte (Modulnamen, Schalter wie `Sim_Waermepumpe`) müssen exakt
übereinstimmen. Fehlende oder zusätzliche Dateien und Einträge gelten als FAIL.

Volatile Größen sind bewusst nicht Teil des Vergleichs: die Autowert-IDs der
`Tab_Ergebnis*`-Zeilen und der Zeitstempel des Laufs.

## Ablauf vor einer Änderung an der Engine (Paket 1 ff.)

Zwei gleichwertige Wege. **Weg B** ist der, mit dem die aktuelle Basis entstanden ist; er
ist zwingend, wenn parallel gearbeitet wird oder die Kopie außerhalb des Repos liegen soll.

### Weg A — mit `lauf` (bequem, benutzt `Referenzlaeufe\Arbeitskopie`)

1. **Sauberen Ausgangszustand herstellen.** Anwendung schließen, Arbeitsverzeichnis auf dem
   Stand, gegen den verglichen werden soll.
2. **Änderung umsetzen** und die Anwendung neu bauen (`WP-Plan.sln` **und**
   `Referenzlauf.csproj`).
3. **Neu rechnen und vergleichen** — Referenz ist die aktuelle Basis, seit dem
   19.08.2026 also `2026-08-19_B6`. **`--projekte` ist Pflicht** (siehe „Die
   Projektauswahl"):
   ```powershell
   & $exe lauf --ziel C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-20_Paket10 `
               --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030
   & $exe vergleich C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-19_B6 `
                    C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-20_Paket10
   ```
   `lauf` kopiert **und migriert** die Arbeitskopie selbst.

### Weg B — eigene Kopie außerhalb des Repos (`migration` + `projekt`)

```powershell
# 1. Eigene, vollständig migrierte Kopie anlegen (schreibt NIE in die produktive DB)
& $exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb C:\Waermeplan\MeinTest\DB

# 2. Auswahl kontrollieren (rein lesend, kopiert nichts)
& $exe liste C:\Waermeplan\MeinTest\DB

# 3. Die NEUN Referenzprojekte einzeln rechnen (feste Liste, nicht die Automatik)
foreach ($id in 1007,1008,1011,1017,1018,1021,1023,1024,1030) {
    & $exe projekt $id "C:\Waermeplan\MeinTest\Lauf\Projekt_$id" C:\Waermeplan\MeinTest\DB
}

# 4. Gegen die aktuelle Basis vergleichen und plausibilisieren
& $exe vergleich C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-19_B6 C:\Waermeplan\MeinTest\Lauf
& $exe pruefen   C:\Waermeplan\MeinTest\Lauf
```

Der Modus `projekt` migriert **nicht** — er erwartet eine fertige Kopie aus Schritt 1.
Ohne Schritt 1 rechnet er auf einem unvollständigen Schema.

> **Schritt 1 ist keine Bequemlichkeit.** Er ist der Grund, warum die Anwendung auf der Kopie
> dieselben Werte rechnet wie auf der gepflegten Datenbank. Eine Datenlücke, die dabei besonders
> leicht zuschlägt, ist die Projekteinstellung `Extrapolation_erlaubt` (Paket 8): Die **Spalte**
> entsteht schon in Migrationsschritt 2 und wird von der stillen Rückfallebene
> `WaermequelleClass.SchemaSicherstellen` ebenfalls angelegt — Access belegt sie dabei in allen
> bestehenden Zeilen mit `False`, also „Extrapolation verboten". Ihre **Vorbelegung auf WAHR** setzt
> erst Schritt 7. Auf einer Kopie ohne Schritt 1 stünde die Einstellung damit überall auf „verboten",
> und jeder Lauf mit einer unterschrittenen Wärmepumpen-Kennlinie bräche ab.
>
> Seit der Paket-8-Nacharbeit (Befund N8) fängt der Leser das ab: Solange
> `Tab_Applikation.SchemaVersion` **unter 7** steht, gilt ein `False` in dieser Spalte als
> Datenlücke und nicht als Anwenderentscheidung — es wird als „erlaubt" gelesen. Ab Schemastand 7
> zählt der gespeicherte Wert. Ein Lauf im Modus `projekt` auf einer nicht migrierten Kopie bricht
> also nicht mehr fälschlich ab; wer die Einstellung wirklich prüfen will, braucht eine migrierte
> Kopie (Schritt 1).

### Danach

**Abweichungen bewerten.** Jede gemeldete Abweichung ist entweder gewollt — dann im
Umsetzungsprotokoll begründen und den neuen Ordner zur Referenz erklären — oder ein
Fehler.

Wichtig: Beide Läufe müssen von derselben Quelldatenbank ausgehen. Ändern sich zwischendurch
die Projektdaten, vergleicht man Äpfel mit Birnen. Die Quelle steht im Kopf von
`lauf_protokoll.md`.

## Die Projektauswahl

**Für jeden Vergleichslauf gilt die feste Liste. `--projekte` ist Pflicht:**

```powershell
& $exe lauf --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030
```

Neun IDs, seit Basis B5 (19.08.2026): die acht Projekte von B4 plus das Kaskadenprojekt
**1030**. Wer sie wegläßt, bekommt einen Ordner, der sich mit der Basis nicht vergleichen
läßt — der Vergleich meldet dann fehlende und zusätzliche Projekte, nicht Rechenabweichungen.

### Warum nicht die Automatik

Ohne `--projekte` wählt die Suite selbst, deterministisch und aus der Arbeitskopie heraus.
Sie deckt zuerst **sieben** Pflichtkategorien ab — Wärmepumpe mit Pufferspeicher,
Heizkessel, BHKW, Solarthermie, den Minimalfall „nur Wärmepumpe", (seit Paket 7)
Wärmepumpe mit **Quellspeicher** und (seit `62322d1`) **BHKW-Kaskade mit mehreren Modulen** —
und füllt dann auf neun Projekte auf: erst mit neuen Erzeugerkombinationen, danach mit
abweichender Anlagenausstattung. Übergangen werden Projekte ohne Eintrag in
`Tab_Einstellungen` und ohne Klimaregion; die stehen mit Begründung im Protokoll.

Die Kategorie „Quellspeicher" steht bewusst **hinter** den fünf ursprünglichen: so bleiben
deren Wahlen unverändert und es kommt nur ein Projekt hinzu (1021).

**Diese Auswahl ist datengetrieben und wandert mit dem Projektbestand.** Das ist kein
Schönheitsfehler, sondern der Grund für die feste Liste: Mit den Beispielprojekten 1026–1029
zieht die Automatik seit dem 19.08.2026 **1012** („nur Wärmepumpe") und **1026**
(Pflichtkategorie Heizkessel) herein und läßt **1008** und **1018** fallen — eine Basis, die
so entstanden wäre, ließe sich mit keiner früheren mehr vergleichen. Die Automatik bleibt
nützlich, um die Projektlandschaft zu sichten (`liste`), nicht um eine Basis einzufrieren.

> **Das Kaskadenprojekt 1030 ist der Anker für Mehrmodul-BHKW.** Es ist das einzige Projekt
> der Referenzmenge mit zwei BHKW-Modulen, gepflegtem KWKG-Satz und gepflegten Energiepreisen
> und deckt damit als einziges die drei Vollbenutzungsstunden-Aggregate aus E2, die bindende
> KWKG-Deckelung und die Positivseite der beiden KWKG-Guards (500-kW-Grenze, Heizöl) ab.
> Zahlen im [Laufprotokoll der Basis](2026-08-19_B6/lauf_protokoll.md).

> **Seit dem 15.08.2026 fehlt Projekt 1010 „Kurs EE"** — vom Anwender gelöscht, es war die
> Kategorie **„nur Wärmepumpe"**. In der festen Liste ist diese Kategorie damit unbesetzt;
> die Automatik füllt sie inzwischen mit 1012. Ein Nachrücken in die feste Liste wäre ein
> bewußter Basiswechsel und kein Nebenbei-Schritt.

## Dialoge der Engine

**Seit Paket 8 zeigt die Engine keine MessageBoxen mehr** (Konzept Kapitel 13.4). Grenz- und
Fehlerfälle laufen über den Protokollkanal `SimulationProtokoll`; jeder Eintrag geht zusätzlich auf
die Konsole und steht damit im `lauf_protokoll.md`:

```
Simulation Hinweis:  vollwertig gerechnet, Randbedingung erwähnenswert
Simulation Warnung:  gerechnet, aber mit einer Ersatzannahme
Simulation FEHLER:   Lauf abgebrochen, es wird kein Ergebnis gespeichert
```

Die frühere Rückfrage „Temperatur unterschreitet Kennlinien-Untergrenze, soll extrapoliert werden?"
ist zur **Projekteinstellung** `Extrapolation_erlaubt` geworden — Vorbelegung WAHR, also genau die
Antwort, die in jedem dokumentierten Lauf gegeben wurde. Statt eines weggeklickten Dialogs steht
jetzt eine `Simulation Hinweis:`-Zeile im Protokoll: derselbe Rechenweg, nur sichtbar.

Der **Dialogwächter läuft trotzdem weiter mit**: Er findet Dialogfenster des eigenen Prozesses und
drückt den bejahenden Knopf (Ja vor OK vor Ignorieren). Er hat nach Paket 8 nichts mehr zu drücken —
und ist genau deshalb wertvoll: Er ist die Messsonde, mit der sich jede künftig neu eingeschleppte
MessageBox im Rechenpfad sofort im Lauf-Protokoll zeigt. Taucht dort ein Eintrag auf, ist das ein
Befund.

Der Zähler des Protokolls wertet die Konsolenausgabe der Kindprozesse aus und kennt beide
Schreibweisen — `WARNUNG:` (Suite) und `Simulation Warnung:` (Engine, seit der Paket-8-Nacharbeit,
Befund N13b). Hinweise werden bewusst nicht mitgezählt: Sie melden einen vollwertig gerechneten
Grenzfall, und den gab es in jedem bisherigen Referenzlauf.

Bleibt ein Projekt trotzdem hängen — etwa an einem Dialog, den der Wächter nicht bedienen
kann — greift das Zeitlimit. Jedes Projekt läuft in einem eigenen Kindprozess, der nach
Ablauf abgeräumt wird; die halbfertige Ausgabe wird gelöscht, das Projekt im Protokoll als
übersprungen vermerkt, und die übrigen Projekte laufen weiter.

## Aufräumen

Ein Lauf belegt rund 30 MB (neun Projekte). Die CSVs gehören ins Git — sie sind die Referenz —, alte
Laufordner dagegen nicht auf Dauer. Nicht mehr benötigte Ordner löschen, statt sie
anzusammeln. `Arbeitskopie/` bleibt ohnehin außen vor: `Kenndaten.accdb` steht in
`.gitignore`.

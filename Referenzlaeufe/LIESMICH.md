# Referenzlauf-Suite (Paket B1)

Regressionsbasis für den Simulationskern von EPOS-Plan.

Vor jedem Umbau an der Engine wird der aktuelle Stand als CSV eingefroren; nach dem Umbau
läuft derselbe Satz Projekte erneut und wird mit Toleranz gegen den eingefrorenen Stand
verglichen. Was sich dabei ändert, ist entweder gewollt — dann wird die Referenz neu
gesetzt — oder ein Fehler.

Grundlage: `WindowsFormsApplication1/Allgemein/Simulation/Konzept_Simulation_QuellenSenken.md`,
Paket B1, Kapitel 9.

## Aktuelle Basis

**`2026-08-30_B3-Kaskade/`** — **dreizehn Projekte** (1007, 1008, 1011, 1017, 1018,
1021, 1023, 1024, 1030, 1039, 1040, 1041, 1042), **332 CSV**. **Die Datenbestände sind
wieder zusammengeführt:** 1040–1042 stehen auf diesem Rechner wieder im Bestand, die
Zweiteilung vom 29.08. (`Booster` für den Zweitstand, `E1E2` für diesen Stand) ist
damit erledigt — es gilt wieder **eine** Basis.

> **Anlass: Datenänderungen des Anwenders an 1030 und 1042.** Das zweite BHKW von
> Projekt 1030 (Anlage 14921, „EC-POWER XRGI 9") hatte keine Senkenzeile und lief nicht
> mehr in der Kaskade mit; der Anwender hat es in `Z_AnlageSenke` wieder eingehängt
> (Rang 1 Heizkreis, Rang 2 PufferHeizung auf Puffer 1054170, Ladeprio 2) und dabei das
> Gerät gewechselt — **die Zwei-Modul-Kaskade rechnet wieder, `aggregate.csv` führt
> `BHKWModul[0]` und `BHKWModul[1]`**. Projekt 1042 ist nach der Löschung vom 28./29.08.
> auf diesem Bestand neu aufgebaut (anderer Kessel, andere Speicher) und deshalb nicht
> mehr mit dem 1042 der Booster-Basis vergleichbar.
>
> **Codestand:** `bad41f8` (Branch `Pufferspeicher`, B3-Serie), gebaut aus einem
> `git archive HEAD`-Export außerhalb des Repos (`C:\Waermeplan\_basisbuild`; 0 Fehler).
> **Datenquelle:** produktive `Kenndaten.accdb`, Zeitstempel **30.08.2026 06:13:43**,
> Schemastand **61**, nur gelesen (keine `laccdb`); die Migration der Arbeitskopie war
> ein No-op. **Selbstvergleich 13/13 PASS (3 558 333 Werte), 332/332 byte-/MD5-gleich**;
> `pruefen` 13/13 plausibel.
>
> Gegen `2026-08-29_Booster`: **297/332 byte-gleich**. Die 35 Abweichungen sind
> vollständig zugeordnet — sieben `aggregate.csv` weichen **ausschließlich** um die neue
> Ergebnisspalte `Hilfsenergie` ab (Migrationsschritt 61, Etappe B3 Paket a; Wert überall
> 0), die übrigen 28 liegen in **1030** (7) und **1042** (21). Der Vergleich mit
> `--ohne BHKWModul[0].Hilfsenergie,BHKWModul[1].Hilfsenergie,HeizkesselModul[0].Hilfsenergie`
> meldet **elf Projekte PASS** und FAIL nur in 1030 und 1042 — der Rechenkern ist
> unverändert. Zahlen und Zuordnung im
> [Laufprotokoll der Basis](2026-08-30_B3-Kaskade/lauf_protokoll.md).
>
> **ACHTUNG bei 1030:** Das zweite Modul ist jetzt „EC-POWER XRGI 9" (9 kW el) statt
> „Agenitor 306 (250 kw.el) Gas". Die Pfade bleiben abgedeckt (beide Module Erdgas, beide
> unter 500 kW, alle drei Vollbenutzungsstunden-Aggregate belegt), **die Zahlen der
> Vorbasen gelten aber nicht mehr** — u. a. ist die Summe thermisch nicht mehr
> 12 860,72 h. Der Lauf trägt neu die Warnung, dass 14921 eine andere Senke führt als die
> führende Anlage 14920 und deshalb die Senke der führenden Anlage gilt.
>
> Der Bestand führt inzwischen auch **1043** und **1044** (weitere Booster-Varianten).
> Sie sind **bewusst nicht** Teil der Basis — ihre Aufnahme wäre ein eigener,
> bewusster Basiswechsel.
>
> **Die feste Projektliste (dreizehn IDs):**
>
> ```powershell
> & $exe lauf --ziel <ordner> --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030,1039,1040,1041,1042
> ```

### Frühere Fassung: die beiden Basen vom 29.08.2026

*(Sie bleiben liegen; die Zweiteilung ist mit `B3-Kaskade` aufgehoben.)*

**Zwei Basen vom 29.08.2026 — die beiden Produktiv-Datenbestände waren
auseinandergelaufen.** Die Referenzarbeit lief am 29.08. parallel auf zwei Ständen:
Auf dem **Zweitstand** (13 Projekte, 1042 mit scharfer Booster-Kopplung) entstand
`2026-08-29_Booster/`; auf **diesem Stand** (1040–1042 vom Anwender gelöscht, 1039
umgebaut) entstand `2026-08-29_E1E2/`. Für die **neun gemeinsamen, unveränderten
Projekte (1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024, 1030) sind beide Basen
byte-gleich zueinander** — jede ist dort byte-gleich zu B2; der Rechenkern-Kern ist
EIN Stand.

**`2026-08-29_Booster/`** — **dreizehn Projekte** (1007, 1008, 1011, 1017, 1018, 1021,
1023, 1024, 1030, **1039, 1040, 1041, 1042**), **332 CSV** — die Referenz für den
13-Projekte-Bestand (Zweitstand), abgelöst durch `2026-08-30_B3-Kaskade`.

> **Anlass: Booster-Temperaturkopplung erstmals scharf im Referenznetz** (Codestand
> `0787aec` = B3, rechnerisch identisch B2). Datenänderung: `WQ_Unbegrenzt = False` an
> Anlage 14818 (mit Anwender-Freigabe per UPDATE gesetzt; Sicherung in `DB-Backup\`) —
> das Häkchen hatte die B1/B2-Kopplung still abgeschaltet (Warnkriterium und Dialogrot
> dazu: Paket B3). Die Basis trägt erstmals die Zeilen „Booster … GETEILTER Puffer …"
> und **„Booster-Lesepunkt: DAVOR"** (Default aus Paket B2). Gegen `2026-08-28_B2`:
> **319/332 byte-gleich, alle 13 Abweichungen in 1042** (gewollt — Booster-JAZ
> 4,60 → 3,05, die 45-°C-Fiktion ist weg; Zahlen im
> [Laufprotokoll der Basis](2026-08-29_Booster/lauf_protokoll.md)).
> **Selbstvergleich 332/332 byte-/MD5-gleich** (zwei `projekt`-Läufe auf EINER festen
> Quellkopie, Datenstand 29.08.2026 00:29).
>
> **Die feste Projektliste (dreizehn IDs):**
>
> ```powershell
> & $exe lauf --ziel <ordner> --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030,1039,1040,1041,1042
> ```

**`2026-08-29_E1E2/`** — **zehn Projekte** (1007, 1008, 1011, 1017, 1018, 1021, 1023,
1024, 1030, 1039), **234 CSV** — die Referenz für den Bestand dieses Rechners.

> **Etappe E5 (29.08.2026) — Basis bleibt, kein Wechsel.** Vorher-/Nachher-Lauf der
> Emissionsetappe E5 (Modus CO₂/CO₂e, neue Faktor-Lesekette, `STROMMIX_CO2_G_JE_KWH`
> 380 → 435): **10/10 PASS, 2 567 843 Werte**, und der Vorher-Lauf war zuvor gegen
> diese Basis ebenfalls 10/10 PASS. Der Rechenkern ist unberührt — E5 sitzt
> ausschließlich im Bericht-/Wirtschaftlichkeitsteil, den der Referenzumfang nicht
> abdeckt. Die Emissionskennzahlen selbst wurden deshalb mit einem eigenen
> Konsolentreiber gegen eine Arbeitskopie gemessen (26 Projekte): im Modus CO2
> unverändert bis auf den angekündigten Strommix-Randfall bei 11 Projekten ohne
> gepflegten Stromträger. Die beiden Beweisordner sind nach der Auswertung verworfen —
> sie hätten nur die geltende Basis verdoppelt.

> **Anlass: Etappen E1 (CO2-Saat der Katalogträger, Migrationsschritt 56) und E2
> (Emissionsarten-Katalog, Migrationsschritt 57) + Datenänderung des Anwenders**
> (`6694c7a`). Der Vergleich gegen B2 meldet **keine einzige CO2-bezogene Abweichung —
> der Referenzumfang des Rechenkerns führt gar keine Emissionskennzahl** (kein
> CSV-Schlüssel enthält CO2/Emission; die CO2-Rechnung liegt im
> Bericht/Wirtschaftlichkeitsteil, den der Referenzlauf nicht abdeckt). E1+E2 sind für
> den Rechenkern **empirisch wirkungsneutral belegt**: alle **216 CSV der neun
> datenunveränderten Projekte byte-/MD5-gleich** zu B2. Die einzigen Abweichungen sind
> **Datenänderungen des Anwenders** zwischen dem B2-Datenstand (28.08. 17:19) und dem
> 29.08. 00:02: **1039 umgebaut** (Kessel und Puffer entfernt, „Simulation Mehrgebäude"
> → „Wärmepumpe WG - BHKW", WP-Gewerk ohne Modul), **1040, 1041, 1042 gelöscht**.
> Arbeitskopie migriert **54 → 57**; **Selbstvergleich 234/234 byte-/MD5-gleich**;
> `pruefen` plausibel. Details im
> [Laufprotokoll der Basis](2026-08-29_E1E2/lauf_protokoll.md).
> **ACHTUNG: Mit der Löschung von 1040–1042 und dem 1039-Umbau verliert dieser
> Datenbestand die vier Konzept-11.1-Abdeckungen** (Mehrgebäude, zwei Puffer je
> Kanal/Parallelverbund, Prozesswärme mit eigenem Puffer, Booster-Kette) — ihre
> Ganglinien liegen in `2026-08-28_B2/` und (1042, scharf) in `2026-08-29_Booster/`.
>
> **Die feste Projektliste (zehn IDs):**
>
> ```powershell
> & $exe lauf --ziel <ordner> --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030,1039
> ```

> *(Fassung vor Booster/E1E2 — Begründung der früheren Basis `2026-08-28_B2`:)*
>
> **Anlass: Paket B2 (Kessel-Temperaturmodus, wählbarer Booster-Lesepunkt, Schema 55) +
> Datenänderung des Anwenders an 1042** (`4be1862`). E2 war durch die
> 1042-Umverschaltung des Anwenders bereits überholt (Kontrolllauf des unveränderten
> Codes: 321/332, alle 11 Abweichungen in 1042). B2 selbst ist per A/B auf gemeinsamer
> Kopie **332/332 byte-gleich** belegt; gegen E2 ist die Basis für die zwölf übrigen
> Projekte byte-gleich. Arbeitskopie auf Schemastand **55** (Vorbelegungen
> „Berechnet"/„Davor" wirksam). **Selbstvergleich 332/332 byte-/MD5-gleich** (zwei
> `projekt`-Läufe auf EINER festen Quellkopie, Datenstand 28.08.2026 17:19). Die
> Booster-Temperaturkopplung war in dieser Basis **nicht scharf** (an Anlage 14818
> stand noch `WQ_Unbegrenzt = True`, konstant 45 °C) — Details im
> [Laufprotokoll der Basis](2026-08-28_B2/lauf_protokoll.md).
>
> **Die feste Projektliste (dreizehn IDs):**
>
> ```powershell
> & $exe lauf --ziel <ordner> --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030,1039,1040,1041,1042
> ```

> *(Fassung vor B2 — Begründung der früheren Basis `2026-08-28_E2`:)*
>
> **Anlass: Booster-Kette produktiv scharf + Codestand E2/D-Check** (`babab27`). Der
> Anwender hat in 1042 die Booster-Verschaltung konfiguriert (CS6800iAW + Kessel →
> „Puffer 3000Ltr" → Booster-WP → Stora B) — die Basis trägt damit erstmals die
> stundengekoppelte Booster-Rechnung samt `wp_quellentemperatur.csv` (drei neue Dateien,
> alle 1042). Die Codepakete seit P1 (E2 Kanal-Ganglinien, D-Check-Layoutfixes) waren per
> A/B byte-gleich belegt; alle CSV-Unterschiede zur P1-Basis sind die 1042-Datenänderung.
> **Selbstvergleich 332/332 byte-gleich** (zwei `projekt`-Läufe auf EINER festen
> Quellkopie, Datenstand 28.08.2026 09:05). Details:
> [Laufprotokoll der Basis](2026-08-28_E2/lauf_protokoll.md).
>
> **Die feste Projektliste (dreizehn IDs):**
>
> ```powershell
> & $exe lauf --ziel <ordner> --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030,1039,1040,1041,1042
> ```

> *(Fassung vor E2 — Begründung der früheren Basis `2026-08-28_P1`:)*
>
> **Anlass: Paket P1 (Schichtspeichermodell, Migrationsschritt 53).** Das Multi-Node-Modell
> ist eingebaut; die gesamte Referenzmenge rechnet mit N = 1 und ist **konstruktiv
> byte-gleich** zum E1-Stand: alle 316 Ganglinien byte-/MD5-identisch, die einzige Änderung
> sind die jetzt gefüllten Kennzahlen `Pufferspeicher[i].T_oben_Mittel`/`T_oben_Min`
> (28 Einträge in 9 `aggregate.csv`; die Quellspeicherzeile von 1021 bleibt leer).
> Toleranzvergleich mit `--ohne` dieser Schlüssel: **13/13 PASS (3 532 029 Werte)**.
> Details und N>1-Wirkproben im
> [P1-Protokoll](../WindowsFormsApplication1/Allgemein/Simulation/P1_Schichtmodell_Protokoll.md).
> Datenquelle: produktive `Kenndaten.accdb` (27.08.2026 20:45, nur gelesen), Arbeitskopie
> migriert auf Schemastand **53**.
>
> **Die feste Projektliste (dreizehn IDs):**
>
> ```powershell
> & $exe lauf --ziel <ordner> --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030,1039,1040,1041,1042
> ```

> *(Fassung vor P1 — Begründung der früheren Basis `2026-08-27_E1`:)*
>
> **Anlass: Paket E1 (Ergebnis je Kanal, Migrationsschritt 52) — Meilenstein Z3: Bedarf →
> Kaskade → Ergebnis → Bericht durchgängig dreikanalig.** Gegenüber A1 wachsen die
> `aggregate.csv` um **39 neue Kanal-Schlüssel** (Bedarf/Deckung/Entladung je Kanal,
> Durchsatzsummen, `ID_Anlage`, `T_oben_*`-Vorgriff); geänderte Bestandswerte sind allein
> die vier `Kapazitaet_Pufferspeicher` der dokumentierten `puffer_wp`-Ablösung (S-1) —
> alle Ganglinien der zwölf unveränderten Projekte byte-gleich, Kanal-Summenprobe 54/54
> (Details im [E1-Protokoll](../WindowsFormsApplication1/Allgemein/Simulation/E1_ErgebnisJeKanal_Protokoll.md)).
> Projekt **1042** trägt zusätzlich eine **Datenänderung des Anwenders** (WP-Module 3 → 2,
> Kombi-Speicher 1054195 entfernt — die Basis friert den neuen Stand ein; das
> Warnkriterium `QUELLE_FEHLT` der unkonfigurierten Booster-Quelle steht im Protokoll).
>
> **Codestand:** Paket E1 auf `Pufferspeicher`. **Datenquelle:** produktive
> `Kenndaten.accdb`, Zeitstempel **27.08.2026 20:45**, nur gelesen (keine `laccdb`, kein
> App-Prozess); Arbeitskopie migriert auf Schemastand **52**. **Selbstvergleich:** zweiter
> Lauf **329/329 byte-/MD5-gleich** — reproduzierbar. `pruefen`: 13/13 plausibel.
>
> **Die feste Projektliste umfasst dreizehn IDs:**
>
> ```powershell
> & $exe lauf --ziel <ordner> --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030,1039,1040,1041,1042
> ```

> *(Fassung vor A1 — Begründung der früheren Basis `2026-08-27_K1`:)*
>
> **Anlass: Paket K1 (Dreikanal-Bedarf) — Entscheidungen F2 und F3.** Gegenüber V0 ändern sich
> **sieben** Projekte gewollt: die Netzverluste werden je Stunde **anteilig** auf die Kanäle
> Heizung/Brauchwasser/Prozess verteilt statt vollständig auf Heizung (F2), und alle
> Profil-Bedarfe (Brauchwasser, Prozesswärme, Strom) kacheln ihr Wochenprofil am
> **Klimadaten-Kalender** statt fest „1. Januar = Sonntag" (F3; produktiv ist der 1. Januar ein
> Donnerstag — die Wochengänge verschieben sich um drei Tage). **Die Jahressummen sind in allen
> neun Projekten exakt unverändert** (`Waermebedarf_Gesamt`, `Strombedarf_Gesamt` je Projekt
> identisch — nur zeitliche Verteilung und Kanalzuordnung ändern sich). **PASS gegen V0: 1018
> und 1030** — die einzigen Projekte ohne Profil-/Brauchwasser-/Prozessanteil. Die neue
> **Energieprobe** (Kanalsumme gegen unabhängige Gesamtsumme, je Stunde, 8760 × 9 Projekte)
> meldet **null Verletzungen**; das Laufprotokoll trägt 12 bekannte Bestandswarnungen
> (Energieträger-Zuordnung, Rückfall-ΔT), 0 Fehler.
>
> **Codestand:** Paket K1 auf `Pufferspeicher` (Details
> [K1-Protokoll](../WindowsFormsApplication1/Allgemein/Simulation/K1_Dreikanal_Protokoll.md)).
> **Datenquelle:** produktive `Kenndaten.accdb`, Zeitstempel 26.08.2026 23:39, nur gelesen;
> Arbeitskopie migriert auf Schemastand **48**. **Selbstvergleich:** zweiter Lauf
> **216/216 byte-/MD5-gleich** — reproduzierbar.

> **Anlass: Paket V0 (Bestandsfehler) des Konzepts Brauchwasser/Heizung/Pufferspeicher.**
> Drei Projekte ändern sich durch die Fixes **gewollt** gegenüber B6 — **1008**
> (Mehrgebäude-Doppelzählung V0-1: Wärmebedarf 98,26 → 54,88 MWh), **1007** und **1011**
> (Stromprofile werden summiert statt überschrieben, V0-2: 12 → 24 bzw. 5 462 → 6 806 MWh) —
> vollständig zugeordnet im
> [V0-Protokoll](../WindowsFormsApplication1/Allgemein/Simulation/V0_Bestandsfehler_Protokoll.md)
> über einen Vorher/Nachher-Lauf auf **gemeinsamer** Datenbankkopie (dort 6/9 byte-stabil).
> Ein PASS/FAIL-Vergleich B6 → V0 wird bewusst **nicht** geführt: Zwischen beiden Ständen
> liegen neben V0 auch der Merge des Branches `kostenformulare` (Migrationsschritte bis 44),
> die x64-Umstellung und Datenpflege des Anwenders — Code und Daten haben sich gleichzeitig
> geändert.
>
> **Codestand:** `2409996` (Branch `Pufferspeicher`), x64 Debug, 0 Fehler.
> **Datenquelle:** produktive `Kenndaten.accdb`, Zeitstempel **26.08.2026 23:39**, nur
> gelesen; vor dem Lauf wurde eine **verwaiste** `Kenndaten.laccdb` dreier fensterloser
> Access-Prozesse bereinigt. Die Migration auf den Schema-Zielstand lief ausschließlich auf
> der Arbeitskopie. **Selbstvergleich:** zweiter Lauf desselben Codes auf derselben Quelle
> **9/9 PASS (2 366 177 Werte), 216/216 byte-/MD5-gleich** — die Basis ist reproduzierbar.
> `pruefen`: alle neun Projekte plausibel, keine NaN/Inf.
>
> **Neu sichtbar im Protokoll:** der V0-9-Hinweis zur oberen Kennlinienkappung meldet in
> Projekt 1024 real **957 Kappungsstunden** (WP „CS6800iAW MB + AW 10 OR-T", oberste
> Stützstelle 20,0 °C) — der Booster-relevante Fall existiert bereits im Bestand.
>
> **Was diese Basis weiterhin nicht absichert:** unverändert die Lücken der B6-Liste (u. a.
> kein Projekt mit zwei Puffern je Kanal, kein Kessel an Quellpuffer mit Wert ≠ 0, keine
> Wirtschaftlichkeit). Die **vier neuen Referenzprojekte** aus Konzept 11.1 (Mehrgebäude;
> zwei Puffer je Kanal; Prozesswärme mit eigenem Puffer; Booster-Kette mit Kombi-Speicher)
> stehen noch aus — 1008 deckt Mehrgebäude seit V0 immerhin rechnerisch korrekt ab.

> **Der Referenzlauf deckt den Rechenkern ab, nicht die Wirtschaftlichkeit.** Er ruft
> `WirtschaftlichkeitCtrl` nicht auf. Kapitalwert, KWK-Zuschlag, Steuergutschriften, Tarife und
> Betriebskosten stehen in **keiner** eingefrorenen Basis; sie werden je Etappe als A/B gegen den
> Vorgängerstand gemessen. Ein grünes 216/216 sagt über die Ausbaustufe W4 deshalb nur, dass sie den
> Rechenkern nicht berührt hat.

> **Die Projektliste ist FEST VORGEGEBEN und muss bei jedem Folgelauf mitgegeben
> werden** — seit dem 30.08.2026 wieder **eine** Liste für einen Bestand (siehe
> „Aktuelle Basis"):
>
> ```powershell
> # Vergleich gegen 2026-08-30_B3-Kaskade:
> & $exe lauf --ziel <ordner> --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030,1039,1040,1041,1042
> ```
>
> *(29.08.2026 bis 30.08.2026 galten zwei Listen je Datenbestand — 13 Projekte gegen
> `Booster`, 10 Projekte gegen `E1E2`. Diese Zweiteilung ist aufgehoben.)*
>
> Ohne `--projekte` wählt die Suite datengetrieben — und diese Wahl **wandert mit dem
> Projektbestand**. Mit den Beispielprojekten 1026–1029 zieht sie inzwischen 1012 und 1026
> herein und lässt 1008 und 1018 fallen. Für eine über die Zeit vergleichbare Basis ist das
> untauglich: Die Ordner ließen sich nicht mehr gegeneinander stellen. B5 friert deshalb die
> acht Projekte von B4 **plus** das neue Kaskadenprojekt ein. Die automatische Auswahl bleibt
> als Werkzeug erhalten (`liste`), taugt aber nur zum Sichten der Projektlandschaft, nicht
> zum Einfrieren einer Basis.

> **Projekt 1030 „Referenz BHKW-Kaskade (Regressionstest)" ist der Anker für
> Mehrmodul-Kaskaden.** Zwei BHKW-Module, Spitzenkessel, Pufferspeicher, gepflegter
> KWKG-Satz und gepflegte Energiepreise. Es deckt als **einziges** Projekt der
> Referenzmenge ab: die drei Vollbenutzungsstunden-Aggregate aus Etappe E2, eine
> **bindende** KWKG-Deckelung und die Positivseite beider KWKG-Guards (beide Module unter
> der 500-kW-Ausschreibungsgrenze, beide Erdgas). Im übrigen Bestand steht `KWKG_Bonus`
> auf 0 — ohne 1030 wäre dieser ganze Pfad ungetestet. **Wird 1030 verändert oder
> gelöscht, verliert die Referenzmenge ihre einzige Abdeckung dieses Pfades.**
>
> **Genau das ist zwischenzeitlich passiert und seit `2026-08-30_B3-Kaskade` geheilt.**
> Das zweite Modul war ohne Senkenzeile aus der Kaskade gefallen; der Anwender hat es
> wieder eingehängt und dabei gewechselt: statt „Agenitor 306 (250 kw.el) Gas" fährt jetzt
> **„EC-POWER XRGI 9"** mit. Die abgedeckten Pfade bleiben, **die Zahlen sind neue**:
> Summe thermisch **13 977,95 h** (vorher 12 860,72 h), ungewichtetes Mittel **6 988,98 h**
> (vorher 6 430,36 h), leistungsgewichtet elektrisch **7 327,17 h** (vorher 5 733,59 h).
> Die frühere Zahl der KWKG-Deckelung (Erlös Jahr 1 44 265,13 € bei Jahresdeckel
> 3 100 Vbh) gilt entsprechend nicht mehr; sie stammt ohnehin aus der
> Wirtschaftlichkeit, die der Referenzlauf nicht abdeckt.

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

`2026-08-29_Booster/` bleibt als **vorheriger Stand** liegen (Codestand `0787aec`,
Schemastand 55, dreizehn Projekte, 332 CSV) — die letzte Basis **vor** der
Wiederherstellung der 1030-Kaskade und dem Neuaufbau von 1042 und damit die einzige
Quelle der Ganglinien des früheren zweiten Kaskadenmoduls „Agenitor 306 (250 kw.el) Gas"
sowie des alten 1042. Gegen `2026-08-30_B3-Kaskade` ist sie in **elf von dreizehn
Projekten wertgleich** (die sieben `aggregate.csv`-Byte-Unterschiede sind allein die neue
Spalte `Hilfsenergie`). Begründung im Abschnitt „Frühere Fassung: die beiden Basen vom
29.08.2026" darüber.

`2026-08-29_E1E2/` bleibt als **älterer Stand** liegen (zehn Projekte, 234 CSV) — die
Basis des zwischenzeitlichen 10-Projekte-Bestands dieses Rechners, überholt seit
1040–1042 wieder im Bestand stehen.

`2026-08-28_B2/` bleibt als **älterer Stand** liegen (Codestand `4be1862`, Schemastand
55, dreizehn Projekte, 332 CSV) — der gemeinsame Ausgangspunkt beider 29.08.-Basen: auf
dem Zweitstand der Stand unmittelbar vor dem Unbegrenzt-Fix (1042 dort mit
abgeschalteter Kopplung, konstant 45 °C; für die zwölf übrigen Projekte byte-gleich mit
der Booster-Basis), auf diesem Stand der letzte Stand **vor** der Anwender-Löschung von
1040–1042 und dem 1039-Umbau — und damit die letzte Quelle der Ganglinien von
Mehrgebäude-1039, Parallelverbund-1040 und Prozesswärme-1041. Begründung in den
Abschnitten „(Fassung vor Booster/E1E2)" darüber.

`2026-08-28_E2/` bleibt als **älterer Stand** liegen (Codestand babab27, Schemastand
54, dreizehn Projekte, 332 CSV) — die erste Basis, in der die
**Booster-Temperaturkopplung in 1042 scharf** war (frühere Verschaltung: geteilter
Puffer 1054196, Lesepunkt implizit „Danach"); für die zwölf übrigen Projekte
byte-gleich mit B2. Begründung im Abschnitt „(Fassung vor B2)" darüber.

`2026-08-28_P1/` bleibt als **älterer Stand** liegen (Codestand P1, Schemastand 53,
dreizehn Projekte, 329 CSV) — die Basis der Pakete B1/Q1/P2/L/E2 (alle je byte-gleich
dagegen); 1042 dort noch mit unkonfigurierter Booster-Quelle. Begründung im Abschnitt
„(Fassung vor E2)" darüber.

`2026-08-27_E1/` bleibt als **älterer Stand** liegen (Codestand E1, Schemastand 52,
dreizehn Projekte, 329 CSV) — die Basis des Meilensteins Z3; P1 war gegen sie in allen
316 Ganglinien byte-gleich (einzige Änderung: T_oben-Füllung). Begründung im Abschnitt
„(Fassung vor P1)" darüber.

`2026-08-27_A1/` bleibt als **älterer Stand** liegen (Codestand A1, Schemastand 51,
dreizehn Projekte, 329 CSV) — die erste Basis mit den vier Konzept-11.1-Projekten,
Meilenstein „ein Rechenweg". A/B-Zuordnung des Altpfad-Abrisses im
[Laufprotokoll](2026-08-27_A1/lauf_protokoll.md); 1042 dort noch mit drei WP-Modulen und
Kombi-Speicher 1054195 (vor der Datenänderung des Anwenders).

`2026-08-27_K1/` bleibt als **älterer Stand** liegen (Codestand K1 auf `Pufferspeicher`,
Schemastand 48, neun Projekte, 216 CSV, 2 366 177 Werte) — die Basis der Pakete K1 bis S2;
K2, S1 und S2 waren gegen sie jeweils **216/216 byte-gleich**, sie blieb deshalb bis A1
unverändert gültig. Warum K1 seinerzeit gesetzt wurde, steht im Abschnitt darunter
(Fassung vor A1) bzw. im K1-Protokoll.

`2026-08-27_V0/` bleibt als **älterer Stand** liegen (Codestand `2409996`, Schemastand 47,
neun Projekte, 216 CSV, 2 366 177 Werte) — der Stand nach den V0-Bestandsfehler-Fixes und vor
der Dreikanal-Umstellung. Warum V0 seinerzeit gesetzt wurde, steht im Abschnitt darüber
(Fassung vor K1) bzw. im V0-Protokoll.

`2026-08-19_B6/` bleibt als **älterer Stand** liegen (Codestand `e94be10`, Schemastand 21,
neun Projekte, 216 CSV, 2 366 177 Werte) — die letzte Basis **vor** dem `kostenformulare`-Merge
und Paket V0. Warum B6 seinerzeit gesetzt wurde, steht im Abschnitt „Warum die Basis auf B6
gewechselt wurde" darüber.

`2026-08-19_B5/` bleibt als **älterer Stand** liegen (Codestand `ef8e537`, Quelle mit Schemastand
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
$msb = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
        -latest -prerelease -products * -requires Microsoft.Component.MSBuild `
        -find 'MSBuild\**\Bin\MSBuild.exe' | Where-Object { $_ -notmatch '\\amd64\\' } | Select-Object -First 1
& $msb `
    C:\Waermeplan\WP_Plan\Referenzlauf\Referenzlauf.csproj `
    -p:Configuration=Debug -p:Platform=x64
```

Beim allerersten Mal davor einmal `-t:Restore` mit denselben Parametern. Das Projekt ist
bewusst **nicht** Teil von `WP-Plan.sln`.

Ergebnis: `Referenzlauf\bin\x64\Debug\net8.0-windows\Referenzlauf.exe`

## Bedienung

```powershell
$exe = "C:\Waermeplan\WP_Plan\Referenzlauf\bin\x64\Debug\net8.0-windows\Referenzlauf.exe"
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

**Für jeden Vergleichslauf gilt die feste Liste. `--projekte` ist Pflicht** — seit dem
30.08.2026 wieder eine einzige Liste (siehe „Aktuelle Basis"):

```powershell
# Vergleich gegen 2026-08-30_B3-Kaskade:
& $exe lauf --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030,1039,1040,1041,1042
```

Kern der Liste sind die neun Projekte der B5-Linie (acht von B4 plus Kaskadenprojekt
**1030**) plus **1039** und die drei Konzept-11.1-Projekte **1040 (zwei Puffer je
Kanal), 1041 (Prozesswärme mit eigenem Puffer) und 1042 (Booster-Kette mit
Kombi-Speicher)**. Die Löschung von 1040–1042 auf diesem Rechner am 28./29.08.2026 ist
rückgängig — die Projekte stehen wieder im Bestand (1042 neu aufgebaut). Die weiteren
Booster-Varianten **1043** und **1044** gehören **nicht** zur festen Liste; sie
aufzunehmen wäre ein bewusster Basiswechsel, kein Nebenbei-Schritt. Wer die Liste
wegläßt, bekommt einen Ordner, der sich mit der Basis nicht vergleichen läßt — der
Vergleich meldet dann fehlende und zusätzliche Projekte, nicht Rechenabweichungen.

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

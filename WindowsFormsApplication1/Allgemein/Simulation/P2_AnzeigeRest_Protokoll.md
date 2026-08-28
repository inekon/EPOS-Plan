# Paket P2 — Anzeige-Nachzügler des Schichtmodells: Umsetzungsprotokoll

Stand: 28.08.2026 · Branch `Pufferspeicher` (HEAD `69baf0b`, Paket Q1) · Bezug:
[`Konzept_Brauchwasser_Heizung_Pufferspeicher.md`](Konzept_Brauchwasser_Heizung_Pufferspeicher.md)
Paketzeile P2 („Schicht-Konfiguration UI"), Kapitel 5.1 (`Z_AnlageSenke.Anschlusshoehe`),
7.4/7.5, 10. Build `WP-Plan.sln` + `Referenzlauf.csproj` x64 Debug: **0 Fehler**
(5 Bestandswarnungen).

> **Der Hauptteil von P2 lag bereits in P1.** Die Paketzeile nennt vier Stücke —
> `Form_PufferSp_Projekt`, `SpeicherKarte`, Ergebnis-Kennzahlen, Kombi-Zonen. Die
> Schichtgruppe im Pufferdialog (N, Höhe, λ_eff, T_Nutz_BW mit 55-°C-Vorschlag,
> Entnahmehöhen, Lade-/Entladeleistung), das Karten-Badge „N Schichten" und die
> `T_oben`-Kennzahl auf der Speicherkarte sind mit P1 geliefert und dort protokolliert.
> **P2 holt den Rest nach:** die Einspeisehöhe je Senkenzeile (P1-O2/S1-O3), das
> Temperaturdiagramm (P1-O5) und die `T_oben`-Kennzahl im Bericht.

## 1. Umfang

Reine **Anzeige- und Eingabeschicht**. Kein Engine-Pfad ist angefasst; der
Einspeisehöhen-Pfad `Z_AnlageSenke.Anschlusshoehe` → `Ladeauftrag.Einspeisehoehe` →
`SimulationPufferspeicher.EinspeisehoeheAktuell` steht seit P1 vollständig — es fehlte
allein die Pflege im Dialog. Dazu die erste Sichtbarmachung der Schichttemperaturen: als
Diagrammseite in der Detailansicht und als Kennzahl plus Ganglinie im Bericht.

## 2. Bausteine

| Baustein | Inhalt | Stellen |
|---|---|---|
| **Einspeisehöhe je Senke (P1-O2/S1-O3)** | Vierte Zeile der Gruppe „Ladeverhalten": Haken „eigene Einspeisehöhe" + Zahlenfeld + Einheitentext, Muster der Ladeobergrenze darüber. **Der Haken trennt „nicht gesetzt" (= oben, die Vorgabe) von der GÜLTIGEN Höhe 0** (ganz unten) — ein leeres Feld darf nicht auf 0 hinauslaufen. Nur an Puffer-Zielen scharf; an einer Direktsenke wird der Wert wie Ladepriorität und Obergrenze gelöscht (−1). Prüfung 0…1 im OK-Klick mit Nennung des Rangs (Ersatzwerte −2/−3, weil −1 bereits „nicht gesetzt" heißt). ToolTip an Haken, Feld und Einheitentext erklärt die Wirkung und ihre Bedingung (nur bei geschichtetem Zielspeicher, N > 1). Sechste Listenspalte „Höhe" zeigt den Wert je Zeile | `Views/Simulation/Form_Waermesenke.cs` |
| **Temperaturdiagramm (P1-O5)** | Dritte Seite von `tabControl2` auf der Wärmepumpen-Registerkarte („Wärmproduktion \| Stromverbrauch \| **Speichertemperaturen**"), programmatisch nach dem Muster `InitKesselChart`; Designer und `.resx` unangetastet. Je Senkenspeicher zwei Serien `PUFFER_<ID>_TOBEN`/`_TUNTEN` (untere gestrichelt in derselben Farbe), dazu `QUELLTEMP_<AnlagenID>` je temperaturgekoppeltem Erzeuger. **Eigene °C-Achse, die beim kleinsten Wert beginnt** — eine bei 0 startende Achse drückte das Band Rücklauf…Vorlauf in den oberen Rand. Die Seite hängt sich nur ein, wenn der Lauf eine Temperaturreihe trägt, und wieder aus, wenn ein Folgelauf keine mehr hat (Regel des Kessel-Diagramms). `Dock.Fill` + `TabPage.Padding` statt fester Bounds — Fixmuster der TabPage-Vierseitenanker-Falle | `Views/Simulation/Form_Simulation_Detail.cs` |
| **Serienschlüssel als Konstanten** | `ZeitreihenSatz.SUFFIX_T_OBEN`/`_T_UNTEN`/`QUELLTEMP_PRAEFIX` — die beiden neuen Verbraucher (Detailansicht, `ChartRenderer`) binden daran statt an Zeichenketten | `Allgemein/Bericht/BerichtsDaten.cs` |
| **`T_oben` im Bericht** | `KennzahlenKatalog`: `eff.t_oben_mittel` (ungewichtetes Mittel über die Speicher mit Wert) und `eff.t_oben_min` (kleinstes Minimum) in der Gruppe Effizienz — je Variante EIN Wert, „—" solange keiner einen trägt. `BausteineProjekt`: Abschnitt „Speichertemperaturen (Schichtmodell)" mit der Aufschlüsselung **je Speicher** (Tabelle Speicher · T oben Mittel · T oben Minimum) und der Ganglinie; entfällt vollständig, wenn kein Speicher einen Wert trägt | `KennzahlenKatalog.cs`, `Bausteine/BausteineProjekt.cs` |
| **Ganglinientyp 5 im Bericht** | `ChartRenderer.Speichertemperaturen(z)` — dieselben drei charakteristischen Wochen, dieselbe Panelaufteilung und dieselbe Farbfolge je Speicher wie `Speicherverlauf`; unterschieden allein durch die °C-Achse und die halbtransparente untere Schicht. Quelltemperatur-Reihen werden **sortiert** eingehängt (Dictionary-Reihenfolge ist nicht zugesichert; die Legende darf zwischen zwei Berichten nicht umspringen) | `Allgemein/Bericht/ChartRenderer.cs` |
| **Wörterbuch** | Fünf Einträge für den neuen Berichtsabschnitt (Überschrift, drei Tabellenköpfe, Bildunterschrift) | `Allgemein/Bericht/BerichtTexte.cs` |

**Neue Ressourcen: 10** (`SIM_SPALTE_EINSPEISEHOEHE`, `SIM_CHK_EINSPEISEHOEHE`,
`SIM_LBL_EINSPEISEHOEHE_EINHEIT`, `SIM_TIP_EINSPEISEHOEHE`, `SIM_MSG_EINSPEISEHOEHE_ZAHL`,
`SIM_MSG_EINSPEISEHOEHE_BEREICH`, `SIM_TAB_SPEICHERTEMPERATUR`,
`CHART_TITEL_SPEICHERTEMPERATUR`, `SIM_REIHE_T_OBEN`, `SIM_REIHE_T_UNTEN`), DE/EN/Designer
deckungsgleich — Bestand danach **2600** Schlüssel. Dazu fünf `BerichtTexte`-Einträge
(kein Ressourcenkatalog: der Bericht übersetzt über sein eigenes Wörterbuch).

## 3. Verifikation

### 3.1 Referenzlauf — die zugesagte Messlatte

Dreizehn Projekte (1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024, 1030, 1039, 1040,
1041, 1042) gegen die Basis **`2026-08-28_P1`**, gerechnet auf dem Endstand des Pakets:

```
Projekt_1007 … Projekt_1042 : 13 × PASS   (3 532 029 Werte innerhalb der Toleranz)
MD5-Vergleich                : 329 von 329 CSV byte-gleich, 0 abweichend,
                               0 fehlend, 0 zusätzlich
pruefen                      : GESAMT plausibel (keine NaN/Inf)
Laufprotokoll                : 45 verschiedene Meldungen vorher, 45 nachher,
                               Mengenvergleich leer
```

Konstruktiv erklärbar: P2 fasst keine rechnende Zeile an, und keine Zeile der
Referenzmenge trägt eine gepflegte Anschlusshöhe. Datenquelle: produktive
`Kenndaten.accdb` (Zeitstempel 27.08.2026 23:34:25, Größe 151 949 312 Bytes, **nur
gelesen** — beides vor dem ersten und nach dem letzten Schritt identisch, keine
`Kenndaten.laccdb`, kein Access- oder Anwendungsprozess); Arbeitskopie migriert auf
Schemastand **54**. Der Probeordner ist nach dem Vergleich gelöscht. **Die Basis
`2026-08-28_P1` bleibt unverändert gültig** (330 Dateien, jüngster Zeitstempel unverändert
27.08.2026 22:58:41).

### 3.2 Wirkprobe Einspeisehöhe — Dialog → Datenbank → Engine

Wegwerf-Kopie (`Referenzlauf.exe migration`, Schemastand 54), Projekt **1023**, Zielspeicher
**1018023** („Vitocell 140-E 600 Ltr", VL_eff 65 °C / RL_eff 45 °C) auf **N = 5** gestellt;
beide Wärmepumpen (11203, 11204) laden ihn auf Rang 1. Der Dialog wurde headless über seine
eigenen Methoden gefahren (`SetControls` → Haken/Feld setzen → `btnOk_Click`).

| Probe | Ergebnis |
|---|---|
| Dialog speichert „0,4" | `Z_AnlageSenke.Anschlusshoehe` = **0,4** in beiden Zeilen (vorher NULL), `SpeichernOk = true` |
| Engine-Wirkung bei N = 5 | `T_oben_Mittel` 55,291 → **55,200 °C** (−0,091 K), `T_unten_Mittel` 51,813 → **52,168 °C** (+0,355 K), Stunden mit `T_oben < VL_eff − 1 K` **4 263 → 4 487**; Ladung/Entladung −45,6 / −45,5 kWh (0,06 %) |
| Monotonie-Gegenprobe „0" (ganz unten) | `T_oben_Mittel` **54,852 °C** (−0,439 K), `T_unten_Mittel` **53,495 °C** (+1,682 K), Stunden `T_oben` kalt **8 054**. Reihenfolge oben ≥ 0,4 ≥ 0,0 eingehalten |
| Plausibilität | „1,5" und „−0,2" → „Die Einspeisehöhe der Senke auf Rang 1 muss zwischen 0 und 1 liegen."; „abc" → „… muss eine Zahl sein." Jeweils `DialogResult = None`, **Datenbank unverändert** |

Die Richtung ist die des Modells: Einspeisung bei 0,4 beginnt an Schicht 3 von 5
(`SchichtIndex` = ⌊(1 − 0,4)·5⌋), füllt abwärts und steigt erst danach nach oben — die
oberen Schichten werden zuletzt warm. Dass der Betrag klein bleibt, ist ebenfalls
modellkonform: Inversionsmischung und vertikaler Ausgleich sortieren die Wärme innerhalb
weniger Stunden wieder nach oben (Konzept 7.4 Punkt 3).

### 3.3 Diagramme und Berichtsgrößen

Auf derselben Wegwerf-Kopie, über die neuen Methoden selbst:

| Probe | Projekt 1023 | Projekt 1042 (Booster, Quelle gekoppelt) |
|---|---|---|
| Reihen der Detailansicht | 2 (`PUFFER_1018023_TOBEN` 45,0…64,9 °C, `_TUNTEN` 45,0…63,9 °C) | 7 (3 Speicher × 2 + `QUELLTEMP_14807`) |
| Diagrammseite | eingehängt, Reiter „Wärmproduktion \| Stromverbrauch \| Speichertemperaturen" | ebenso |
| Chart | 2 Serien, Y-Achse 44…66, Titel „Temperatur [°C]" | 7 Serien, Y-Achse 0…10 |
| `ZeitreihenSatz` | `PUFFER_1018023_TOBEN/_TUNTEN` | + `QUELLTEMP_14807` |
| `ChartRenderer.Speichertemperaturen` | 19 778 Bytes PNG | 34 392 Bytes PNG |

Beide PNG gesichtet: drei Wochenfenster, Legende mit Umbruch, plausible Kurven (1042 zeigt
den Kombi-Speicher zwischen 8 und 9,5 °C zyklieren, die beiden 3000-l-Speicher flach bei
9,5 °C). `Tab_ErgebnisPufferspeicher` trägt für 1023 `T_oben_Mittel = 54,85`,
`T_oben_Min = 45` — die Größen, aus denen der Katalog seine zwei Kennzahlen bildet.

Die Quelltemperatur-Reihe entsteht nur bei einem temperaturgekoppelten Erzeuger; für die
Probe wurde `WQ_Typ`/`WQ_ID_Puffer` der WP 14807 auf den Kombi-Speicher gesetzt — **nur auf
der Wegwerf-Kopie**.

### 3.4 Layout

`Form_Waermesenke` misst danach 620 × **825** px Client (vorher 789; die Gruppe
„Ladeverhalten" wächst von 140 auf 176 px, alles darunter hängt an dieser Konstanten und
rückt mit). Haken, Feld und Einheitentext liegen innerhalb der Gruppe; die sechs
Listenspalten summieren sich auf **560 von 560** px, es entsteht keine Bildlaufleiste.

### 3.5 Kombi-Zonen (Konzept 7.5) — nichts offen

Geprüft gegen die fünf Zusagen des Kapitels:

| Zusage | Wo eingelöst |
|---|---|
| BW-Bereitschaftszone oben (Entnahme 1,0), Heizzone darunter (Default 0,5) | Engine-Vorgabe hängt am Klassen-Set: `vorgabeUnten = BedientKanal(BRAUCHWASSER) ? 0,5 : 1,0`, BW fest 1,0 (`SimulationControl.cs:2919-2929`) |
| Entnahmehöhen bedienbar | `Form_PufferSp_Projekt.EntnahmezeileOrdnen` blendet je Kanal des Klassen-Sets ein Paar Label/Feld ein — ein Kombi-Speicher zeigt Heizung **und** Brauchwasser (P1) |
| `T_Nutz_BW` mit 55 °C vorschlagen, sobald N > 1 | `Schichten_Geaendert` → `T_NUTZ_VORSCHLAG = 55`, nur bei leerem Feld und nicht während des Befüllens (P1) |
| Knappheitsregel 4.3 statt K-1 | Engine seit K2; der Hinweistext des Senkendialogs (`SIM_LBL_HINWEIS_KOMBI`) nennt sie |
| Bei N = 1 wie bisher | P1-Byte-Nachweis, hier erneut bestätigt (329/329) |

**Ergebnis: UI-seitig ist zu 7.5 nichts offen.** Zusätzlich geschlossen: **P1-O6** — die
Engine liest die drei `Entnahme_*`-Spalten inzwischen kanalweise aus der Projektkopie
(`SimulationControl.Entnahmehoehe(...)`, mit Klemmung auf 0…1 und klassenset-abhängiger
Vorgabe); die Konzept-Defaults gelten nur noch bei NULL. Neu sichtbar wird die Zonung im
Temperaturdiagramm dieses Pakets.

## 4. Offene Punkte / Notizen

| # | Punkt | Ziel |
|---|---|---|
| P2-O1 | Der `ZeitreihenExtraktor` schreibt die Temperaturschlüssel weiter als Zeichenketten (`"_TOBEN"`) und die Legendentexte deutsch inline; die Konstanten aus `ZeitreihenSatz` und `SIM_REIHE_T_OBEN`/`_UNTEN` stehen bereit. Die Datei lag außerhalb des P2-Umfangs | nächster Griff / Paket L |
| P2-O2 | Die Diagrammseite hängt an `tabControl2` und ist damit nur erreichbar, wenn die **Wärmepumpen**-Registerkarte im Projekt aktiv ist — dieselbe Bindung, die die Speicher-Ergebnistabelle (`listView_SimPuffer`) seit Paket 7 hat. Ein Projekt mit Kessel und Schichtspeicher, aber ohne WP, sieht beides nicht | mit der Speichertabelle gemeinsam lösen |
| P2-O3 | Der Berichts-Temperaturverlauf steht im Baustein **Projektbeschreibung** (Stammprojekt). `ErgebnisseBaustein.ZeichneGanglinien` (`BausteineVergleich.cs`) zeichnet die vier Bestands-Ganglinien je VARIANTE — dort fehlt die fünfte. Die Datei lag außerhalb des P2-Umfangs | Variantenbericht |
| P2-O4 | `ChartRenderer` beschriftet weiterhin deutsch hart (Titel, Wochennamen); das gilt für alle fünf Ganglinientypen und ist Bestandslage | Paket L |
| P2-O5 | `Form_Waermesenke` misst zugeklappt 825 px Client-Höhe — dieselbe Lage wie beim Pufferdialog (P1-O8): auf kleinen Schirmen rollt er, `FensterEinpassung` fängt es ab | kosmetisch |
| P2-O6 | Beim Messen aufgefallen, **nicht von P2 verursacht**: der Einheitentext der Ladeobergrenze (`SIM_LBL_LADEGRENZE_EINHEIT`, 350 px ab x = 262) ragt 16 px über die Gruppenbreite hinaus und wird am Rahmen beschnitten. Der neue Einheitentext der Einspeisehöhe ist dafür kurz gehalten (261 px), die Erklärung steht im ToolTip | Bestandsbefund |
| P2-O7 | `Tab_Einstellungen.Kanal_Knappheitsreihenfolge` (F10, Schritt 49) hat **keine Oberfläche** — erreichbar ist nur die Vorbelegung `BRAUCHWASSER;PROZESS;HEIZUNG`. Solange das so bleibt, stimmt der Kombi-Hinweis „zuerst Warmwasser" des Senkendialogs immer | K2-Restpunkt |
| P2-O8 | Die Einspeisehöhe hat **kein Warnkriterium**: ein gepflegter Wert an einem Ein-Zonen-Speicher (N = 1) bleibt wirkungslos, ohne dass der Katalog es meldet. Der ToolTip sagt es, eine Prüfzeile in `Warnkriterien` wäre die schärfere Form (die Datei lag außerhalb des P2-Umfangs) | S2-Nachtrag |

# Konzept: Wechselrichter EPOS-Plan — Katalog, Strangzuordnung und Rechenweg

**Rev. 5 — 06.09.2026 — Stufen S1, S2 und S3 UMGESETZT; dazu W6‑O‑1 (ein Importwirt und der
OND-Import), W6‑O‑3 (die CEC-Wechselrichterliste als Auslieferungsdatei) und W6‑O‑4, W6‑O‑5,
W6‑O‑6 (Modul je Strang, Herstellerfilter)**
(Rev. 1 war Befund und Vorschlag zur Entscheidung durch den Anwender;
Rev. 2 trug Stufe S1, Rev. 3 die Stufe S2, Rev. 4 die Stufe S3)

Auftrag (Anwenderwunsch **W6‑E‑2**, Windows-Abnahme vom 06.09.2026, im Wortlaut):

> „Wechselrichter – ausgegraut. Import liegt nicht vor, Admin zum Anlegen/Bearbeiten liegt nicht
> vor. Berechnungsvorschrift zur Berücksichtigung der Wechselrichter (wenn vorhanden, dann muss
> der Wechselrichter einem Strang mit PV-Modulen zugeordnet werden). Mockup und Konzept vor
> Umsetzung."

Anlass ist der Dialog „Verwaltung Photovoltaik Module"
(`EPOS.UI/Dialoge/Erzeuger/PhotovoltaikDialog.razor` mit dem Baustein `PvModellFelder.razor`):
Neigung 30°, Azimut 0°, Anzahl Module 10, darunter „Rechenmodell: Einfach", ein leeres Feld
„WR-Wirkungsgrad", „Systemverluste [%]: 12,00" und der **ausgegraute** Knopf „Wechselrichter…".

Dieses Papier ist die Fortsetzung von `Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md` an genau
der Stelle, die dort zweimal offengelassen wurde: Entscheidungsfrage **Q5** („Wechselrichter als
Anlagenparameter oder eigener Wechselrichterkatalog?" — Empfehlung damals: Anlagenparameter,
Katalog erst mit E3) und Stufe **E3** („Stringauslegung gegen einen Wechselrichterkatalog —
zurückstellen"). Der Anwenderwunsch W6‑E‑2 holt beides nach vorn. **Rev. 1 war ein Konzept- und
Mockup-Papier; seit dem Entscheid vom 06.09.2026 ist Stufe S1 umgesetzt (Kapitel 8), S2 und S3
sind es nicht.**

Mockup: `Mockups/Wechselrichter_Mockup_2026-09-06.html` (vier Ansichten M1–M4;
in Stufe S1 unverändert, mit **S2** um die zwei Optionen aus W6‑E‑3 und den
S3-Hinweis in M1 ergänzt; in **S3 unverändert** — die Stufe fasst den Rechenweg
an, nicht die Maske; mit dem **Nachtrag W6‑O‑4/O‑6** trägt M1 die Filterzeile
über der Tabelle, die Spalte „Modul" und die Herleitungszeile darunter, und der
S3-Hinweis ist dort fort).

> **Entscheid des Anwenders vom 06.09.2026.** „Setze Vorschlag fuer Wechselrichter um“;
> zu den zehn Entscheidungsfragen: „W6‑E‑2‑Q1 bis Q10: Empfehlung, ja alle". Damit ist
> jede Empfehlung aus Kapitel 11 angenommen. **Alle drei Stufen sind umgesetzt**:
> S1 (Commit `40fc542`, Zweig `ios_migration`), S2 (`c02cd99`) und **S3** (`d88243e`,
> siehe Kapitel 8). Dazu kam der neue Anwenderwunsch **W6‑E‑3** — zwei
> sichtbare Optionen im PV-Dialog (Kapitel 7.1), umgesetzt in S2.4.
>
> **Zwei offene Punkte sind mit `9ef8ca5` dazu geschlossen** (Anwenderentscheide vom
> 06.09.2026): **W6‑O‑1** — „der OND-Import soll umgesetzt werden. baue daher den
> Modulimport schon jetzt um (Modulimport und Wechselrichter Import zwei Masken)" — und
> **W6‑O‑3** — „hole die Wechselrichterdaten für den Import", bestätigt als „Liste als
> Datei und dann über Import (aus Admin-Menü)". Beides steht in Kapitel 5.2, 5.5 und 12.
>
> **Zur Auslieferungsregel (Kapitel 8, „Reihenfolge"): S2 und S3 werden zusammen
> AUSGELIEFERT, aber getrennt entwickelt und abgenommen.** Der Zwischenzustand ist
> vorbei: Der sichtbare Satz „Die Strangrechnung folgt mit Stufe S3 — bis dahin
> rechnet die Anlage vereinfacht" (Ressource `PVS_HINWEIS_S3`) ist mit S3 aus
> Maske und beiden `.resx` **entfernt**; die Oberfläche verspricht seither genau
> das, was der Kern tut.

---

## 1. Befund

### 1.1 Warum der Knopf ausgegraut ist

Der Knopf trägt eine einzige Sperrbedingung
(`EPOS.UI/Dialoge/Erzeuger/PvModellFelder.razor:46`):

```razor
<button type="button" class="epos-knopf epos-pvmodell-wechselrichter"
        disabled="@(!Zeile.ModellErweitert)" @onclick="Oeffnen">@Texte.BtnWechselrichter</button>
```

`Zeile.ModellErweitert` (`EPOS.UI/Dialoge/Erzeuger/ErzeugerAuswahlDaten.cs:65`) ist der
Modellschalter `Tab_Energieanlagen.PV_Modell` der Anlage. Im Bildschirmfoto steht „Rechenmodell:
Einfach" — **damit ist der Knopf gesperrt, und zwar bestimmungsgemäß**. Er ist kein Rest einer
unfertigen Baustelle: Dahinter liegt eine funktionierende Überlagerung mit AC-Nennleistung und
drei Wirkungsgradpunkten (`PvModellFelder.razor:49-74`), die nur im Modell **Erweitert** rechnet.

Die Regel stammt wörtlich aus der abgelösten WinForms-Maske (`Form_PV.ModellUmschalten`,
Z. 118‑124, nachgezogen mit Merge 5 am 05.09.2026) und steht im Kopfkommentar des Bausteins:
„Modell EINFACH sperrt den Knopf und lässt den Wirkungsgrad frei; ERWEITERT umgekehrt."

**Was der Anwender sieht, ist also richtig, aber unerklärt.** Der Sperrgrund steht nur im
Werkzeugtipp („nur im Modell Erweitert"); im Bild ist er unsichtbar. Das ist der erste, kleinste
Befund: Ein gesperrter Knopf ohne sichtbaren Grund liest sich als Fehler.

### 1.2 Was das Modell EINFACH heute mit dem Wechselrichter macht

Genau **einen konstanten Faktor**. `SimulationPV.Berechnung` liest je Anlagenzeile
(`EPOS.Kern/Allgemein/Simulation/SimulationPV.cs:194-195`):

```csharp
double etaWr        = ctrl.items[n].PV_WrWirkungsgrad ?? WR_WIRKUNGSGRAD_VORGABE;   // 0,95
double systemFaktor = 1.0 - (ctrl.items[n].PV_Systemverluste ?? SYSTEMVERLUSTE_VORGABE) / 100.0;
```

und wendet beide unverändert auf jede Stunde an (`:248` und `:250`):

```csharp
pvPotentialGesamt_stuendlich[i] += (float)(erg.potenzielleErzeugung * etaWr * systemFaktor);
prodSummeMod                    +=          erg.potenzielleErzeugung * etaWr * systemFaktor;
```

`WR_WIRKUNGSGRAD_VORGABE = 0.95` (`:85`) ist der bis Paket A fest verdrahtete Faktor; Paket A hat
ihn nur aus dem Code in die Anlagenzeile geholt (Stufe E1.3). Im Bildschirmfoto ist das Feld
**leer** — die Anlage rechnet also mit 0,95, und die 12 % Systemverluste ziehen weitere 12 % ab.

**Damit gibt es in EINFACH kein Clipping, keine Teillastkennlinie, keine AC-Nennleistung und
keinen Nachtverbrauch.** Eine 10‑kWp-Anlage an einem 2,5‑kW-Wechselrichter rechnet in EINFACH
denselben Sommermittag wie an einem 12‑kW-Gerät.

### 1.3 Was das Modell ERWEITERT heute macht

ERWEITERT (Paket B, Stufe E2) kennt den Wechselrichter als **fünf Zahlen an der Anlagenzeile**
(`SimulationPV.cs:261-312`):

| Größe | Spalte | NULL bedeutet |
|---|---|---|
| AC-Nennleistung [kW] | `PV_WrNennleistungKw` | kein Clipping; Auslastung bezieht sich auf die DC-Nennleistung |
| η bei 10 % | `PV_WrEta10` | 0,94 |
| η bei 50 % | `PV_WrEta50` | 0,975 |
| η bei 100 % | `PV_WrEta100` | 0,97 |
| Modellschalter | `PV_Modell` | EINFACH |

Der Rechenweg je Stunde (`:299-312`):

```
P_DC,sys = P_DC · (1 − Systemverluste/100)
x        = P_DC,sys / P_AC,nenn                     (ohne Nennleistung: / P_STC,gesamt)
η_WR(x)  = lineare Interpolation über (0,1; η10), (0,5; η50), (1,0; η100)
P_AC     = min(P_DC,sys · η_WR(x), P_AC,nenn)
```

Die Kennlinie steht in `PvErweitertesModell.EtaWechselrichter` — drei Stützstellen, unter 10 %
linear auf null, über 100 % konstant η100, weil dahinter das Clipping greift. Sie ist geprüft
(58 PASS / 0 FAIL, Nachtrag 4 des PV-Ertragsmodells) und trägt.

**Was ERWEITERT nicht kann:** Die fünf Zahlen gelten für die **ganze Anlage**. Es gibt keine
Zuordnung eines Geräts zu einem Strang, keinen zweiten Wechselrichter, kein MPPT, keine
Spannungsgrenzen und keinen Nachtverbrauch. Und die Zahlen werden **von Hand** eingetippt — es
gibt keinen Ort, an dem sie herkommen könnten.

### 1.4 Was es nicht gibt

Nachgeprüft am 06.09.2026 gegen den Stand `ios_migration`:

| Was | Prüfung | Ergebnis |
|---|---|---|
| Wechselrichter-Tabelle | `grep -i wechselrichter sql/schema/*.sql` | **kein Treffer** |
| Wechselrichter-Katalog | `KatalogRegistry._alle` (`EPOS.Kern/Allgemein/Katalog/KatalogRegistry.cs:110-354`), 19 Definitionen | **nicht dabei** |
| Verwaltungsmaske | `ModulKatalogArt` (`ModulKatalogProfil.cs:16-23`) — zwei Ausprägungen: Stromspeicher, Photovoltaik; `KatalogBrowserArt` (`KatalogBrowserProfil.cs:16-29`) — vier: Heizkessel, BHKW, Solarkollektoren, Pufferspeicher | **nicht dabei** |
| Import | `KatalogImportArt` (`KatalogImportProfil.cs:16-29`) — vier VDI‑3805-Ausprägungen; dazu der eigenständige `PvModulImportDialog` (CEC/PAN, 771 Z.) | **nicht dabei** |
| Strang, Modulfeld, MPPT | `grep -i strang` im Kern | nur „erstrangig" — **kein Fachbegriff Strang** |
| Menüpunkt | `Menuetabelle.cs:93-116` (Administration → Energiesysteme, Administration → Datenimport) | **nicht dabei** |

**Die Wechselrichterdaten sind reine Anlagenwerte** — fünf Spalten in `Tab_Energieanlagen`
(Migrationsschritt 64, `SchemaKatalog.cs:1287-1296`), gepflegt in einer Überlagerung, ohne
Herkunft und ohne Prüfung. Der Anwender hat den Zustand korrekt beschrieben.

### 1.5 Was der Modulkatalog schon führt — die Grundlage der Auslegung

`Tab_PV_STAMM` (`sql/schema/001_grundschema.sql:2115`) und die Projektkopie `Tab_PV` (`:2094`)
führen 19 bzw. 18 Spalten, darunter genau die, die eine Strangauslegung braucht:

| Spalte | Einheit | heute genutzt für |
|---|---|---|
| `Leistung` (P_STC) | W | Bemessungsgröße der Ertragsrechnung (E1.1), kWp, Stückpreis |
| `U_Mpp`, `U_Leerlauf` | V | **nichts** |
| `I_Mpp`, `I_Kurzschluss` | A | **nichts** |
| `alpha_SC` | A/K | **nichts** |
| `beta_OC` | V/K | **nichts** |
| `gamma_PMP` | %/K | Temperaturgang (EINFACH) |
| `T_NOCT` | °C | Zelltemperatur (E1.2) |
| `Technologie` | — | Huld-Koeffizientensatz (E2.3) |

Die vier U/I-Kennwerte und die zwei Temperaturkoeffizienten sind seit W6‑E‑1 im PV-Dialog hinter
dem Aufklapper „Alle Parameter" **sichtbar** und im Katalogdialog **pflegbar**
(`ModulKatalogProfil.cs:267-296`) — sie liegen also gepflegt und geprüft
(`PvModulPlausibilitaet.cs`) bereit und werden von nichts gelesen. Das PV-Ertragsmodell hat das
schon am 02.09.2026 so festgehalten (Abschnitt 2.4): Ihr Nutzen entsteht „erst mit der
Stringauslegung / dem Wechselrichterabgleich — ein Planungs-/Plausibilitätsfeature, das
Wechselrichter-Stammdaten voraussetzt, die es nicht gibt."

**Genau diese Lücke schließt dieses Papier.**

### 1.6 Neigung und Azimut hängen an der Anlage, nicht am Strang

`Tab_Energieanlagen.Neigung` und `.Azimut` sind Ganzzahlspalten der Anlagenzeile
(`sql/schema/001_grundschema.sql:730-731`); die Simulation liest sie einmal je Anlage
(`SimulationPV.cs:236` und `:283`: `ctrl.items[n].m_Neigung`, `m_Azimut`). Ein Ost/West-Dach ist
heute nur als **zwei getrennte PV-Anlagen** abbildbar — mit zwei Modulzeilen, zwei Anlagennamen
und zwei Wechselrichterparametersätzen. Das ist möglich, aber es ist nicht das, was der Planer im
Kopf hat, und es verhindert genau die Aussage, für die ein Ost/West-Wechselrichter gebaut wird:
**ein** Gerät, **zwei** MPPT, **eine** Clipping-Grenze über beiden Dachhälften.

---

## 2. Bewertung

**Der Kern der Sache ist nicht der Knopf, sondern die Ebene.** Ein Wechselrichter ist ein Gerät,
kein Anlagenparameter. Er hat eine Herkunft (Datenblatt, Katalog), eine Stückzahl, einen Preis,
Eingangsgrenzen und eine Kennlinie — alles Eigenschaften, die im Haus sonst ein `_STAMM`-Katalog
trägt (Heizkessel, BHKW, Wärmepumpe, Pufferspeicher, Solarkollektoren, PV-Modul, Stromspeicher).
Die Wechselrichter sind die **einzige** Gerätefamilie in EPOS-Plan, die es nicht als Katalog gibt.

Drei Folgen davon:

1. **Die Zahlen sind unbelegt.** Die drei Stützstellen der Kennlinie tippt der Anwender ein oder
   lässt sie leer; leer bedeutet 0,94 / 0,975 / 0,97 — einen „typischen Stringwechselrichter". Das
   ist ehrlich benannt (`SimulationPV.KennlinieMelden`, `:630-646`), aber es ist eine Annahme, wo
   eine Messgröße möglich wäre.
2. **Die Auslegung ist nicht prüfbar.** Ohne Spannungsfenster und Eingangsstrom kann niemand
   sagen, ob 10 Module in Reihe an dieses Gerät passen. Die Modulwerte dafür liegen bereit (1.5),
   die Gerätewerte fehlen.
3. **Ost/West und Mehrgerätigkeit sind nicht abbildbar** (1.6). Damit fehlt die
   Eigenverbrauchsaussage, für die Ost/West-Anlagen überhaupt gebaut werden.

Dem gegenüber steht der Aufwand: eine Katalogtabelle mit Projektkopie, eine Zuordnungstabelle,
eine Verwaltungsmaske, ein Import und ein Umbau der Stundenschleife in `SimulationPV`. Das ist
nicht klein — aber jeder Baustein hat im Haus bereits ein Vorbild, das man ausprägt statt neu zu
bauen (Abschnitte 5 bis 7). **Der wirklich neue Code entsteht fast nur im Rechenweg
(Abschnitt 4).**

---

## 3. Datenmodell

Hausregeln, die überall in diesem Abschnitt gelten:

* **IDs statt Textfeldern** bei jeder neuen Beziehung (CLAUDE.md, „Namenskonventionen im Schema").
* **Kein DDL-DEFAULT auf Fachwerten**; NULL ist der Vorgabewert, und der Vorgabewert ist der, der
  nichts ändert (PV-Ertragsmodell N2.2).
* **Katalog und Projektkopie im selben Schritt**, Spalte für Spalte gleich — eine Spalte nur auf
  einer Seite ist beim `CopyFromStamm` sofort ein Datenverlust (`SchemaKatalog`, Begründung zu
  `Schritt11_Stromspeicher`).
* **Neue Migrationsschritte beginnen bei 65** (`SchemaStand.Zielversion = 64`,
  `EPOS.Kern/Allgemein/Update/SchemaStand.cs:55`; PV-Ertragsmodell Nachtrag 5).

### 3.1 Der Katalog `Tab_Wechselrichter_STAMM`

Aufbau nach dem Muster `Tab_PV_STAMM` (`sql/schema/001_grundschema.sql:2115`): ganzzahliger
Schlüssel, `Bezeichner`, `Firma`, `Beschreibung`, `ReadOnly`, danach die Fachwerte.

| Spalte | Typ | Einheit | Bedeutung | NULL bedeutet |
|---|---|---|---|---|
| `ID` | INTEGER PK | | Schlüssel | — |
| `Bezeichner` | TEXT | | Gerätename | — |
| `Firma` | TEXT | | Hersteller | unbekannt |
| `Beschreibung` | TEXT | | Freitext | — |
| `ReadOnly` | INTEGER 0/1 | | gehört zur Auslieferung | 0 |
| `P_AC_Nenn` | REAL | kW | AC-Nennwirkleistung (CEC `Paco`, OND `PNomConv`) | Pflichtfeld |
| `S_AC_Max` | REAL | kVA | maximale AC-Scheinleistung | = `P_AC_Nenn` |
| `P_DC_Max` | REAL | kW | maximale DC-Eingangsleistung (OND `PNomDC`) | keine Grenze |
| `U_Mpp_Min` | REAL | V | untere Grenze des MPP-Fensters (CEC `Mppt_low`) | keine Prüfung |
| `U_Mpp_Max` | REAL | V | obere Grenze des MPP-Fensters (CEC `Mppt_high`) | keine Prüfung |
| `U_Dc_Max` | REAL | V | maximale DC-Eingangsspannung (CEC `Vdcmax`) | keine Prüfung |
| `U_Start` | REAL | V | Einschaltspannung | keine Prüfung |
| `I_Dc_Max` | REAL | A | maximaler DC-Strom **je MPPT** (CEC `Idcmax`) | keine Prüfung |
| `Anzahl_Mppt` | INTEGER | | Zahl der MPP-Tracker | 1 |
| `Straenge_Je_Mppt` | INTEGER | | zulässige Stränge je MPPT | keine Prüfung |
| `Eta05`, `Eta10`, `Eta20`, `Eta30`, `Eta50`, `Eta100` | REAL | — | Stützstellen der Kennlinie (3.3) | Stützstelle unbekannt |
| `Eta_Euro` | REAL | — | europäischer Wirkungsgrad (0…1) | aus den Stützstellen gerechnet |
| `Eta_Max` | REAL | — | Maximalwirkungsgrad (0…1), nur Ausweis | — |
| `P_Standby` | REAL | W | Einschaltschwelle/Eigenverbrauch (CEC `Pso`) | 0 |
| `P_Nacht` | REAL | W | Nachtverbrauch (CEC `Pnt`) | 0 |
| `Kosten` | REAL | € | Gerätepreis (Anwenderfeld, wie `Modulkosten`) | 0 |
| `Sandia_Pdco`, `Sandia_Vdco`, `Sandia_Pso`, `Sandia_C0…C3` | REAL | — | optionaler Sandia-Block (3.3.2) | nicht importiert |

Bemerkungen:

* **`I_Dc_Max` ist je MPPT gemeint**, nicht je Gerät — so führt es die CEC-Liste, und so braucht es
  die Prüfung (4.2). Das gehört in den Feldkommentar des Schemakatalogs, sonst wird es beim
  Handpflegen falsch gefüllt.
* **`S_AC_Max` getrennt von `P_AC_Nenn`**, weil das Clipping an der Wirkleistung hängt, die
  Netzanschlussbewertung aber an der Scheinleistung. Rechnerisch benutzt Abschnitt 4 nur
  `P_AC_Nenn`; `S_AC_Max` ist Ausweis und Prüfgröße.
* **`ReadOnly = 1`** für den Auslieferungsbestand, wie in allen `_STAMM`-Tabellen (CLAUDE.md:
  „gehört zur Auslieferung").
* Die **Investitionskosten** liegen im Katalog, nicht an der Anlage — dieselbe Bauart wie
  `Tab_PV_STAMM.Modulkosten`, und aus demselben Grund in `AusschlussSpalten` der
  Dublettenprüfung (5.4).

### 3.2 Die Projektkopie `Tab_Wechselrichter`

Spaltengleich zum Katalog, zusätzlich `ID_Projekt INTEGER NOT NULL`, ohne `ReadOnly` — wörtlich
das Verhältnis `Tab_PV_STAMM` ↔ `Tab_PV` (`sql/schema/001_grundschema.sql:2094` gegen `:2115`).
Der Controller `WechselrichterCtrl.CopyFromStamm(stammId, idProjekt)` legt die Kopie an, wie
`PhotovoltaikCtrl.CopyFromStamm` (`EPOS.Kern/Controller/PhotovoltaikCtrl.cs:299-364`).

**Warum überhaupt eine Kopie?** Weil das ganze Haus so gebaut ist: „Projekte KOPIEREN
Katalogsätze, alle persistierten Verweise zeigen auf die Projektkopie, nie auf die
`_STAMM`-Tabelle" (`KatalogRegistry.cs:81-86`). Ein Projekt, das vor drei Jahren gerechnet wurde,
rechnet damit heute noch mit den Gerätedaten von damals — auch wenn der Katalog inzwischen
gepflegt wurde. Die Zuordnungstabelle (3.4) verweist deshalb auf `Tab_Wechselrichter.ID`, nicht
auf den Katalog.

### 3.3 Die Wirkungsgradkennlinie — zwei Formen, eine Empfehlung

#### 3.3.1 Form A: Stützstellen (PVsyst-/OND-Muster) — **empfohlen**

Sechs Spalten `Eta05`, `Eta10`, `Eta20`, `Eta30`, `Eta50`, `Eta100` (Wirkungsgrad als Faktor 0…1
bei 5, 10, 20, 30, 50 und 100 % der AC-Nennleistung), dazu `Eta_Euro` als Ausweis:

```
η_euro = 0,03·η5 + 0,06·η10 + 0,13·η20 + 0,10·η30 + 0,48·η50 + 0,20·η100
```

(die europäische Wichtung, in jedem Datenblatt zu finden; die kalifornische CEC-Wichtung nutzt
zusätzlich 75 % — dafür wäre eine siebte Spalte `Eta75` nötig, siehe Entscheidungsfrage
**W6‑E‑2‑Q1**).

**Umrechnung auf die vorhandene Dreipunkt-Kennlinie:** Die Stützstellen 10, 50 und 100 % sind
identisch mit `PV_WrEta10/50/100`. Ein Katalogsatz füllt die drei Anlagenspalten also **ohne
Rechnung** — der bestehende Rechenweg (`PvErweitertesModell.EtaWechselrichter`) bleibt gültig und
wird nur um die drei zusätzlichen Stützstellen 5, 20 und 30 % erweitert. An den gemeinsamen
Punkten ist das Ergebnis unverändert; dazwischen wird die Interpolation genauer, weil der steile
Kurvenast unter 20 % jetzt zwei Punkte hat statt keinen.

Fehlt eine Stützstelle (NULL), fällt die Interpolation auf den nächsten vorhandenen Punkt
zurück — dieselbe Rückfallregel und dieselbe Protokollmeldung wie heute (`KennlinieMelden`).

#### 3.3.2 Form B: Sandia-Koeffizienten (CEC-/SAM-Muster)

Das Sandia-Wechselrichtermodell (King u. a. 2007; in `pvlib.inverter.sandia`, in SAM und in der
CEC-Liste) rechnet:

```
A    = P_dco · (1 + C1·(U_dc − U_dco))
B    = P_so  · (1 + C2·(U_dc − U_dco))
C    = C0    · (1 + C3·(U_dc − U_dco))
P_AC = [ P_aco/(A − B) − C·(A − B) ] · (P_DC − B) + C·(P_DC − B)²
```

mit `Paco` (AC-Nennleistung), `Pdco` (DC-Leistung bei AC-Nennleistung), `Vdco` (Bezugsspannung),
`Pso` (Einschaltschwelle), `C0…C3` und `Pnt` (Nachtverbrauch). Alle acht Größen stehen in der
CEC-Liste.

**Das Modell ist genauer als jede Stützstellenkurve — und in EPOS-Plan nicht rechenbar.** Es
braucht `U_dc`, die **MPP-Spannung des Strangs in dieser Stunde**. Die entsteht erst mit einem
Ein-Dioden-Modell (Stufe E3 des PV-Ertragsmodells), das nach Entscheidungsfrage Q6 vom
02.09.2026 ausdrücklich zurückgestellt ist. Ohne E3 bliebe nur, `U_dc = U_dco` einzusetzen — dann
verschwinden C1…C3, und übrig bleibt eine Parabel, also wieder eine Kennlinie mit drei
Freiheitsgraden. **Der Genauigkeitsgewinn wäre null, der Aufwand nicht.**

#### 3.3.3 Empfehlung und der Brückenschlag

**Empfohlen ist Form A für die Rechnung und Form B als mitgeschriebenes Katalogwissen.** Die acht
Sandia-Spalten kosten im Katalog nichts, gehen beim CEC-Import verlustfrei mit und machen E3
später ohne Neuimport möglich — dieselbe Begründung, mit der das PV-Ertragsmodell (Abschnitt 6,
Punkt 3) schon für die Modulseite „Importe jetzt vervollständigen" empfohlen hat.

Der Import rechnet die Stützstellen aus den Sandia-Koeffizienten **bei `U_dc = U_dco`** aus. Dort
gilt `A = Pdco`, `B = Pso`, `C = C0`, und die Gleichung wird ein geschlossener Ausdruck:

```
P_AC(P_DC) = [ Paco/(Pdco − Pso) − C0·(Pdco − Pso) ] · (P_DC − Pso) + C0·(P_DC − Pso)²
```

Für jede Stützstelle x ∈ {0,05; 0,10; 0,20; 0,30; 0,50; 1,00} wird `P_DC` so gesucht, dass
`P_AC = x·Paco` ist (die Gleichung ist quadratisch in `P_DC`, also in einem Schritt lösbar);
`η(x) = x·Paco / P_DC`. **Prüfwert:** bei x = 1 ist `P_DC = Pdco` und damit `η100 = Paco/Pdco`
exakt — das ist der Wirkungsgrad, den auch das Datenblatt bei Nennlast nennt.

### 3.4 Die Strangzuordnung `Z_AnlageStrang`

**Name nach Hausregel.** Der Auftrag nennt `Tab_PVStrang`; die Hausregel sagt: `Tab_*` sind Stamm-
und Projektdaten, `Z_*` ist die **Zuordnung**. Eine Strangzeile ist genau das — sie verbindet eine
Energieanlage mit einem Wechselrichter und legt fest, wie viele Module in welcher Verschaltung
daran hängen. Das nächste Vorbild im Bestand ist `Z_AnlageSenke`
(`sql/schema/001_grundschema.sql`, Block `Z_AnlageSenke`): ID, `ID_Anlage`, `Rang`, Fachfelder,
`FOREIGN KEY … ON DELETE CASCADE`. **Empfohlen ist `Z_AnlageStrang`** in genau dieser Bauart.

| Spalte | Typ | Bedeutung | NULL bedeutet |
|---|---|---|---|
| `ID` | INTEGER PK | Schlüssel | — |
| `ID_Anlage` | INTEGER NOT NULL | → `Tab_Energieanlagen.ID`, `ON DELETE CASCADE` | — |
| `Rang` | INTEGER NOT NULL | Reihenfolge in der Tabelle der Oberfläche | — |
| `Bezeichner` | TEXT | Freitext („Dach Süd", „Ostseite") | Rang als Anzeige |
| `ID_Wechselrichter` | INTEGER | → `Tab_Wechselrichter.ID` (Projektkopie) | kein Gerät zugeordnet |
| `Geraetenummer` | INTEGER | welches physische Gerät dieses Typs (1…n) | 1 |
| `Mppt` | INTEGER | MPPT-Eingang dieses Geräts (1…n) | 1 |
| `Module_Reihe` | INTEGER | Module in Reihe | Pflichtfeld |
| `Straenge_Parallel` | INTEGER | parallel geschaltete Stränge | 1 |
| `Neigung` | INTEGER | Neigung dieses Teilfelds [°] | Anlagenwert |
| `Azimut` | INTEGER | Azimut dieses Teilfelds [°] | Anlagenwert |
| `ID_PV` | INTEGER | abweichender Modultyp (→ `Tab_PV.ID`) | Modul der Anlage |

Drei Entwurfsentscheidungen, die eine Begründung verdienen:

1. **Eine Tabelle statt zweier.** Die saubere Normalform wären zwei Ebenen —
   `Z_AnlageWechselrichter` (welches Gerät wie oft) und darunter `Z_AnlageStrang` (welcher Strang
   an welchem MPPT). Der Gewinn wäre gering: Alles, was das Gerät ausmacht, steht im Katalog, und
   die Gerätezahl ist `COUNT(DISTINCT Geraetenummer)` je Anlage und Wechselrichtertyp. Die Kosten
   wären eine zweite Tabelle, ein zweiter Controller, ein zweiter Migrationsschritt und eine
   zweistufige Maske. **Empfohlen ist die eine Tabelle** mit `Geraetenummer` als
   Gruppierungsmerkmal; das Clipping rechnet über `(ID_Anlage, ID_Wechselrichter,
   Geraetenummer)`.
2. **`Neigung` und `Azimut` je Strang, NULL = Anlagenwert.** Das ist der ganze Ost/West-Fall
   (1.6), und der Vorgabewert ändert nichts: Ohne Eintrag rechnet der Strang mit der
   Anlagenausrichtung, also exakt wie heute.
3. **`ID_PV` je Strang, NULL = Anlagenmodul.** Kostet eine Spalte, ist im Bestand oft nötig
   (Erweiterung einer Anlage mit einem anderen Modultyp) und wurde in Stufe S2 **nicht** in der
   Oberfläche gezeigt — sie stand bereit, bis sie gebraucht wurde. Wer sie damals weggelassen
   hätte, bräuchte jetzt einen Migrationsschritt für ein Feld, das ohnehin absehbar war.

   > **Seit dem Anwenderentscheid W6‑O‑6 vom 06.09.2026 wird sie gezeigt und gerechnet**
   > (umgesetzt in `35a48eb`), wörtlich: „jeder Strang mit nur einem Modultyp, unterschiedliche
   > Stränge können jeweils einen anderen Modultyp haben." Die Strangtabelle führt dafür eine
   > Spalte **„Modul"** als Klappliste über dem Modulkatalog; leer heisst „das Modul der Anlage"
   > — dieselbe Rückfallregel wie bei Neigung und Azimut, nur als Klapplisteneintrag statt als
   > Klammer, weil ein Modulname kein Zahlenfeld ist. Die Zeile trägt die **Projektkopie**
   > (`Tab_PV.ID`), übernommen wird beim Wählen (`PhotovoltaikCtrl.CopyFromStamm`) wie beim
   > Wechselrichter.

### 3.5 Was an der Anlagenzeile bleibt

**Nichts wird gelöscht.** Die fünf Spalten aus Migrationsschritt 64 (`PV_Modell`,
`PV_WrNennleistungKw`, `PV_WrEta10/50/100`) bleiben und behalten ihre Bedeutung als
**Rückfallebene**:

```
Führt die Anlage mindestens eine Zeile in Z_AnlageStrang MIT ID_Wechselrichter,
   dann rechnet die Strangzuordnung, und die fünf Anlagenspalten werden ignoriert.
Sonst rechnet der Weg von heute, Zeichen für Zeichen.
```

Diese Vorrangregel ist der Grund, warum die Umstellung ergebnisneutral sein kann (4.3): Kein
Bestandsprojekt hat eine Strangzeile, also rechnet kein Bestandsprojekt anders.

> **Ergänzt durch W6‑E‑3 (06.09.2026).** Vor der Vorrangregel steht seit diesem Anwenderwunsch
> ein SICHTBARER Schalter: `Tab_Energieanlagen.PV_Wechselrichterweg` (NULL = „vereinfacht").
> Gerechnet wird die Strangzuordnung erst, wenn der Schalter auf „mit Wechselrichter" steht
> **und** eine Strangzeile vorliegt. Zwei Bedingungen statt einer machen die Bitgleichheit
> stärker, nicht schwächer — Begründung und Datenmodell in **7.1**.

`PV_Systemverluste` bleibt ein **Anlagenwert** — Verschmutzung, Leitungsverluste und Mismatch
gelten für das Feld, nicht für den Strang. `PV_WrWirkungsgrad` bleibt der Faktor des einfachen
Modells ohne Zuordnung.

> **Umgesetzt (S3, 06.09.2026).** Die VORRANGREGEL steht in
> `SimulationPV.GeraeteDerAnlage`, und sie steht **vor** dem Datenbankzugriff:
> Gelesen wird erst, wenn `SimulationPV.IrgendeineAnlageMitKatalogweg` an den
> ohnehin geladenen Anlagenzeilen einen Schalter `KATALOG` findet — ein
> Bestandsprojekt kostet damit **keine einzige zusätzliche Abfrage**. Dann sind es
> zwei für das ganze Projekt: `AnlageStrangCtrl.LesenJeProjekt` und
> `WechselrichterCtrl.ReadAll`. In der Stundenschleife wird nichts nachgeladen.
>
> Die Anlagenschleife hat damit einen DRITTEN Zweig; die zwei vorhandenen bleiben
> Zeichen für Zeichen stehen (`if (mitStrang) … else if (!erweitert) … else …`).
> Vier Rückfallebenen melden sich einzeln im Protokoll: Anlage auf `KATALOG` ohne
> Strangzeile, Strang ohne Gerät, Gerät ohne Kennlinie, Gerät ohne
> AC-Nennleistung.

### 3.6 Migrationsschritte

Zielversion steht auf 64; neue Schritte beginnen bei **65**
(`EPOS.Kern/Allgemein/Update/SchemaStand.cs:55`).

| Schritt | Inhalt | in `SchemaKatalog.Alle`? |
|---|---|---|
| **65** | `CREATE TABLE Tab_Wechselrichter_STAMM` + `Tab_Wechselrichter` (spaltengleich, plus `ID_Projekt`) | die **Projektkopie** ja, die **Stammtabelle** nein |
| **66** | `CREATE TABLE Z_AnlageStrang` mit den zwei Fremdschlüsseln | ja |

Beide Schritte sind **reines DDL ohne DML**: Nach der Migration ist die Katalogtabelle leer und
kein Projekt hat eine Strangzeile. Damit ist die Migration selbst ergebnisneutral; die Idempotenz
(Zweitlauf ohne DDL) ist wie in Schritt 63/64 nachzuweisen.

> **Umgesetzt (S1, 06.09.2026).** Schritt 65 legt beide Tabellen mit
> `CREATE TABLE IF NOT EXISTS … STRICT` an; die zwei Anweisungen stehen EINMAL, in
> `EPOS.Kern/Allgemein/Update/WechselrichterSchema.cs`, und werden von
> `SchemaMigration.Schritt_65_Wechselrichterkatalog`, von
> `Werkzeuge/Testdatenbankschema` und von der Testvorrichtung `EPOS.Kern.Tests/TestDatenbank`
> gleichermaßen gefahren. `SchemaStand.Zielversion` steht auf **65**.
>
> **Zwei Abweichungen gegenüber Rev. 1, beide bewusst:**
>
> * **`sql/schema/001_grundschema.sql` bleibt unberührt.** Rev. 1 nennt die Datei in
>   Stufe S1.1. Sie ist aber der EINGEFRORENE Access-Zielstand 61 („NICHT VON HAND
>   AENDERN — neu erzeugen"), eingebettete Ressource des `EposSqliteMigrator` und über
>   `sql/schema/inventar.json` auf 114 Tabellen gezählt (`sql/tools/baue_leere_db.py`
>   prüft das). Die Schritte 62, 63 und 64 haben sie aus demselben Grund nicht angefasst.
>   Der Anwender hat am 06.09.2026 festgehalten, dass die Access-Datenbank nicht mehr
>   relevant ist — die zwei Tabellen entstehen deshalb ausschließlich im SQLite-Zweig.
> * **Kein Fremdschlüssel auf `Tab_Wechselrichter.ID_Projekt`** — spaltengleich zum
>   Zwilling `Tab_PV`, der ebenfalls keinen führt. Ein Fremdschlüssel wäre hier eine
>   stille Verhaltensänderung am Löschweg eines Projekts
>   (`ProjektCtrl.LoeschenMitVorarbeiten`), und S1 ändert kein Verhalten. Für
>   `Z_AnlageStrang` (Schritt 66) bleibt der `ON DELETE CASCADE` aus 3.4 dagegen
>   vorgesehen: Dort ist die Kaskade der Zweck.
>
> Der **Typkatalog** `sql/schema/SchemaTypKatalog.g.cs` brauchte keinen Eintrag:
> `ReadOnly` steht dort bereits als Boolean-Spalte, und einen mehrdeutigen Namen legt
> Schritt 65 nicht an.

> **Umgesetzt (S2, 06.09.2026).** Schritt 66 legt `Z_AnlageStrang` mit
> `CREATE TABLE IF NOT EXISTS … STRICT` an und stellt die Spalte
> `Tab_Energieanlagen.PV_Wechselrichterweg` sicher. `SchemaStand.Zielversion` steht
> auf **66**; `Referenzlaeufe/Kenndaten_Test.sqlite` ist nachgezogen.
>
> **Zwei Ablagen statt einer, und das mit Absicht.** Die TABELLE steht in
> `EPOS.Kern/Allgemein/Update/AnlageStrangSchema.cs` — einer EIGENEN Klasse neben
> `WechselrichterSchema`, nicht als dritter Eintrag darin: Deren `Anweisungen` sind
> genau die Liste, die Migration und `Werkzeuge/Testdatenbankschema` für SCHRITT 65
> abarbeiten und zählen („0 von 2 Tabelle(n) angelegt"). Ein dritter Eintrag machte
> beide Zählungen falsch. **Eine Klasse je Schritt.** Die SPALTE dagegen steht dort,
> wo alle additiven Spalten stehen: `SchemaKatalog.Schritt66_PvWechselrichterweg` —
> und damit in `SchemaKatalog.Alle`.
>
> **Warum die Spalte in `Alle` steht.** Das Kriterium des Hauses ist „der LESER";
> hier ist der Grund stärker, es ist der SCHREIBER. `AnlagenSql.SQL_ANLAGE_INSERT`
> nennt `PV_Wechselrichterweg` seit S2 namentlich (64 Spalten statt 63), und auf
> einer Datenbank ohne sie scheiterte JEDES Speichern einer Anlage. Die
> Rückfallebene `WaermequelleClass.SchemaSicherstellen` legt sie deshalb an. Sie
> steht in `Alle` **zwischen** Schritt 63 und 64, weil beide ebenfalls an
> `Tab_Energieanlagen` hängen und die Rückfallebene das Schema sonst zweimal läse.
> Die TABELLE kann dort nicht stehen — `Alle` kennt nur Spalten; ihre Rückfallebene
> ist `AnlageStrangCtrl.TabelleVorhanden`, wörtlich nach `Z_AnlageSenkeCtrl`.
>
> **Zwei Fremdschlüssel, und nur zwei.** `ID_Anlage` mit `ON DELETE CASCADE` (die
> Kaskade ist der Zweck) und `ID_Wechselrichter` restriktiv auf die PROJEKTKOPIE
> `Tab_Wechselrichter` — wörtlich das Verhältnis `Z_AnlageSenke.ID_Puffer` →
> `Tab_Pufferspeicher`. Ein dritter auf `Tab_PV` bleibt weg: `ID_PV` steht bereit
> (3.4, Entwurfsentscheidung 3), wird in S2 weder gezeigt noch geschrieben, und eine
> erzwungene Beziehung wäre eine stille Verhaltensänderung am Löschweg der
> Modul-Projektkopien — dieselbe Zurückhaltung wie bei Schritt 65 und `ID_Projekt`.
>
> `sql/schema/001_grundschema.sql` bleibt auch hier unberührt, aus der Begründung
> oben.

Das Kriterium für die Aufnahme in `SchemaKatalog.Alle` ist im Haus „der LESER, nicht die Tabelle"
(`SchemaKatalog.cs:1276-1285`). Der Rechenkern liest die Projektkopie und die Zuordnung; die
**Katalogtabelle** liest er nicht — sie gehört, wie `Schritt64_PvStammUndDegradation`, in einen
zweiten Teil außerhalb von `Alle`.

---

## 4. Berechnungsvorschrift

Die Notation folgt N2.3 des PV-Ertragsmodells. Alle Leistungen in kW, Einstrahlung in W/m²,
Temperaturen in °C.

### 4.1 Der Stundenweg

Je Anlage und Stunde *i*:

**Schritt 1 — je Strang s die Gleichstromleistung.**

```
β_s, γ_s  = Neigung_s / Azimut_s, sonst Neigung/Azimut der Anlage
G_t,s     = Transposition(β_s, γ_s, GHI_i, DNI_i, DHI_i, Tag, Stunde)
              — isotrop in EINFACH, Hay-Davies in ERWEITERT (unverändert)
n_s       = Module_Reihe_s · Straenge_Parallel_s
P_STC,s   = n_s · Leistung_Modul / 1000                        [kWp]
T_Zelle,s = T_amb + (G_t,s / 800) · (T_NOCT − 20)
P_DC,s    = EINFACH:   P_STC,s · G_t,s/1000 · (1 + γ_PMP·(T_Zelle,s − 25))
            ERWEITERT: P_STC,s · G' · η_rel(G', T_Zelle,s − 25)   (Huld, sonst wie EINFACH)
```

**Der Transpositionsaufruf wird je (β, γ)-Paar zwischengespeichert**, nicht je Strang: Ein
Ost/West-Feld mit acht Strängen hat zwei Ausrichtungen. Ohne den Zwischenspeicher rechnete die
Sonnengeometrie achtmal dasselbe (Befund B3 des PV-Ertragsmodells zur Laufzeit gilt sinngemäß).

**Das Modul ist das des STRANGS** (Anwenderentscheid **W6‑O‑6** vom 06.09.2026, umgesetzt in
`35a48eb`): `Leistung_Modul`, `γ_PMP`, `T_NOCT`, Fläche und Wirkungsgrad in den Zeilen oben kommen
aus der Projektkopie, auf die `Z_AnlageStrang.ID_PV` zeigt — und nur ohne diese Angabe aus dem
Modul der Anlage. Auch der Huld-Koeffizientensatz des erweiterten Modells folgt der
Zelltechnologie DIESES Moduls. Gelesen wird **je Modultyp einmal**, vor der Stundenschleife;
in der Schleife steht keine Abfrage. Eine `ID_PV`, die `Tab_PV` nicht (mehr) führt, ist derselbe
Fall wie keine — der Strang rechnet mit dem Anlagenmodul, und der Lauf meldet es.

**Schritt 2 — Summe je MPPT.**

```
P_DC,mppt = Σ_{s ∈ MPPT}  P_DC,s
```

Führt der Katalog eine MPPT-Eingangsleistungsgrenze, wird hier geklemmt und der Verlust gezählt
(optional, siehe **W6‑E‑2‑Q7**). Ohne Grenze ist Schritt 2 eine reine Summe.

**Schritt 3 — Summe je Gerät und Systemverluste.**

```
P_DC,ger = ( Σ_{mppt ∈ Gerät} P_DC,mppt ) · (1 − PV_Systemverluste/100)
```

Die **DC-Nennleistung eines Geräts** ist entsprechend die Summe seiner Stränge, jeder mit seiner
eigenen Modulleistung (W6‑O‑6): `Σ_s Leistung_Modul(s)/1000 · n_s`. An ihr hängen das
DC/AC-Verhältnis und die Rückfallebene der Auslastung, wenn der Katalog keine AC-Nennleistung
führt.

**Schritt 4 — Kennlinie, Clipping, Nachtverbrauch.**

```
x        = P_DC,ger / P_AC,nenn                       Auslastung (Definition wie heute)
η_WR(x)  = stückweise lineare Interpolation über die vorhandenen Stützstellen
             (5/10/20/30/50/100 %); unter der kleinsten linear auf (0; 0);
             über 100 % konstant η100 — dahinter greift das Clipping
P_AC,roh = P_DC,ger · η_WR(x)
P_AC,ger = min(P_AC,roh, P_AC,nenn)                   Clipping
falls P_DC,ger < P_Standby/1000:  P_AC,ger = −P_Nacht/1000     Nachtverbrauch
```

Zur **Definition von x**: Sie ist bewusst dieselbe wie heute (`SimulationPV.cs:302-303`) — das
Verhältnis der DC-Eingangsleistung zur AC-Nennleistung. Streng genommen bezieht sich eine
Datenblattkennlinie auf die **abgegebene** Leistung, also auf `P_AC/P_AC,nenn`; der Unterschied
liegt bei 2–4 % der Auslastung und damit im Zehntelprozentbereich des Wirkungsgrads. Die
bestehende Definition bleibt, damit der Dreipunkt-Pfad ohne Zuordnung Zeichen für Zeichen
unverändert rechnet. **Das gehört ins Umsetzungsprotokoll**, nicht in eine stille Änderung.

**Schritt 5 — Summe je Anlage.**

```
P_AC,Anlage,i = Σ_{Geräte} P_AC,ger
```

Das ist der Wert, der in `pvPotentialGesamt_stuendlich[i]` läuft — die Schnittstelle zur
Verbrauchsbilanz bleibt unangetastet.

> **Umgesetzt (S3, 06.09.2026).** Die Schritte 1 und 2 stehen in
> `SimulationPV.StraengeRechnen` (samt dem **Transpositions-Zwischenspeicher** je
> Ausrichtungspaar), die Schritte 3 bis 5 dort und in
> `PvStrangModell.Stunde` — einer Klasse **ohne Datenbank und ohne Oberfläche**,
> Bauart `PvErweitertesModell`.
>
> **Drei Festlegungen, die der Text oben offenließ:**
>
> * **Die Nennleistung eines Strangs wird wie die der Anlage gebildet** —
>   `Modul-Nennleistung / 1000 · Modulzahl`, in genau dieser Reihenfolge.
>   `StrangPlausibilitaet.StrangKwp` rechnet `Modulzahl · Leistung / 1000`, was
>   algebraisch dasselbe und im letzten Bit etwas anderes ist. Die Ampel darf das,
>   der Rechenweg nicht: An dieser Reihenfolge hängt die Abnahme S3 (2).
> * **Der Nachtfall greift bei `P_DC,ger ≤ P_Standby`**, nicht `<`. Ohne gepflegte
>   Einschaltschwelle ist die Schwelle 0 — und dann sind es genau die Stunden ohne
>   Einstrahlung, die dieselbe 0 liefern wie zuvor.
> * **Ein Strang ohne Gerät rechnet nicht mit** und wird gezählt; der Lauf meldet
>   die Zahl. Ebenso ein Strang, dessen Gerät die Projektkopie nicht (mehr) kennt.
>
> **Und eine, die offen bleibt:** `Z_AnlageStrang.ID_PV` (der abweichende
> Modultyp je Strang) wird weiterhin **nicht gerechnet** — jeder Strang rechnet
> mit dem Modul der Anlage. Neuer offener Punkt **W6‑O‑6** (Kapitel 12).

### 4.2 Plausibilitätsprüfungen der Zuordnung

Sie laufen **beim Bearbeiten der Strangzeile** (Ampel in der Oberfläche, Abschnitt 7) und
**beim Simulationsstart** (Protokollmeldung), nicht in der Stundenschleife. Grundlage sind die
Modulwerte aus 1.5 und die Gerätewerte aus 3.1.

Auslegungstemperaturen nach üblicher Praxis: **−10 °C** für den kalten Fall (höchste Spannung),
**+70 °C** Zelltemperatur für den heißen Fall (niedrigste Spannung, höchster Strom).

| Nr. | Prüfung | Formel | Verletzung |
|---|---|---|---|
| **P1** | Leerlaufspannung im kalten Fall | `Module_Reihe · [ U_Leerlauf + β_OC·(−10 − 25) ] ≤ U_Dc_Max` | **rot** — das Gerät kann zerstört werden |
| **P2** | MPP-Fenster im heißen Fall | `Module_Reihe · [ U_Mpp + β_OC·(70 − 25) ] ≥ U_Mpp_Min` | **rot** — der Strang regelt im Sommer ab |
| **P3** | MPP-Fenster im kalten Fall | `Module_Reihe · [ U_Mpp + β_OC·(−10 − 25) ] ≤ U_Mpp_Max` | **gelb** — das Gerät regelt an der Grenze |
| **P4** | Eingangsstrom je MPPT | `Σ_{s∈MPPT} Straenge_Parallel_s · [ I_Kurzschluss + α_SC·(70 − 25) ] ≤ I_Dc_Max` | **rot** |
| **P5** | Strangzahl je MPPT | `Σ_{s∈MPPT} Straenge_Parallel_s ≤ Straenge_Je_Mppt` | **gelb** |
| **P6** | DC/AC-Verhältnis | `1,0 ≤ Σ P_STC,ger / P_AC_Nenn ≤ 1,5` | **gelb** außerhalb |
| **P7** | DC-Eingangsleistung | `Σ P_STC,ger ≤ P_Dc_Max` | **gelb** |
| **P8** | Modulsumme gegen Anlagenwert | `Σ_s (Module_Reihe_s · Straenge_Parallel_s) = PV_Leistung` | **gelb** (siehe Q9) |

`β_OC` ist negativ (V/K), `α_SC` positiv (A/K) — die Vorzeichen stehen so in
`PvModulPlausibilitaet.cs:20-24` und werden dort schon geprüft. **Fehlt ein Modulwert (0 oder
NULL), entfällt die Prüfung und wird als „nicht prüfbar" gemeldet — sie schlägt nicht fehl.** Der
Katalogbestand ist an dieser Stelle nachweislich vergiftet (Paket-A-Befund A1: in allen sechs
Referenzmodulen steht der Kurzschlussstrom in `alpha_SC`, `beta_OC` und `T_NOCT`), und eine
Prüfung, die auf schlechten Daten rot leuchtet, wird weggeklickt statt gelesen.

**Eine Näherung ist zu benennen:** Der Katalog führt keinen eigenen Temperaturkoeffizienten für
die MPP-Spannung. P2 und P3 setzen dafür `β_OC` ein — die Auslegungspraxis tut dasselbe, der
Fehler liegt bei wenigen Prozent und auf der sicheren Seite. Das gehört in den Werkzeugtipp der
Ampel, nicht nur ins Protokoll.

> **Umgesetzt (S2, 06.09.2026):** `EPOS.Kern/Allgemein/Import/StrangPlausibilitaet.cs`.
> Sie steht bei ihren zwei Geschwistern — `PvModulPlausibilitaet` prüft einen Modulsatz,
> `WechselrichterPlausibilitaet` einen Gerätesatz, diese hier die ZUORDNUNG beider; alle
> drei laufen beim BEARBEITEN, nicht beim Rechnen. Die Näherung steht als
> `Befund.NaeherungMpp` im `title` jeder Ampelzeile.
>
> **Drei Festlegungen, die der Text oben offenließ:**
>
> * **Ein fehlender Wert macht GELB**, nicht rot und nicht grün. „Die Prüfung entfällt"
>   allein reichte nicht: Eine Ampel, die auf fehlenden Daten grün leuchtet, behauptet
>   etwas. Der Satz sagt, WELCHE Angabe fehlt. Fehlt `Anzahl_Mppt` (W6‑O‑2), rechnen P4
>   und P5 auf EINEM Tracker — dem konservativen Fall — und sagen es.
> * **P6 gilt in BEIDE Richtungen.** Eine zu klein ausgelegte Modulfläche an einem großen
>   Gerät ist ebenso ein Hinweis wie eine zu große; das Band 1,0…1,5 ist ein Band.
> * **Die Zahlen stehen im BEFUND, nicht nur im Satz** (`UocKalt`, `UmppHeiss`,
>   `UmppKalt`, `Strom`, `DcAc`, `Kwp`). Ein Prüfstand, der Text vergleicht, prüft die
>   Sprache; Anhang A ist so Zahl für Zahl nachrechenbar.
>
> **Die Kultur ist die des ANWENDERS** — anders als bei den zwei Geschwistern, deren
> Meldungen Import- und Speicherprotokolle sind und invariant formatieren. Dieser Satz
> steht im PV-Dialog unter der Strangzeile und wird GELESEN (Muster
> `PhotovoltaikStammCtrl.Parameterzeilen`).

### 4.3 Verhalten ohne Zuordnung — und in EINFACH

**Ohne Strangzeile ändert sich nichts.** Die Vorrangregel aus 3.5 greift vor der Stundenschleife;
ohne Zuordnung läuft der Code von heute unverändert — dieselbe Schleife, dieselben Konstanten,
dieselbe Reihenfolge der Gleitkommaoperationen.

**Das ist das zentrale Abnahmekriterium:** Der Referenzlauf gegen
`Referenzlaeufe/2026-09-05_R2_Zeitbasis` (elf Projekte, 282 CSV) muss **byte-gleich** bleiben.
Kein Referenzprojekt hat eine Strangzeile; wäre auch nur ein CSV verschieden, wäre die
Vorrangregel verletzt. Dieselbe Zusage hat Paket B eingelöst (355/355 byte-gleich, N4.1) — der
Nachweis ist eingeübt.

**In EINFACH:** Die Empfehlung ist, den Wechselrichter auch dort wirken zu lassen, sobald eine
Zuordnung besteht (Entscheidungsfrage **W6‑E‑2‑Q5**). Begründung: Ein Wechselrichter ist ein
**Gerät**, keine Modellverfeinerung. Der Modellschalter unterscheidet die *Rechentiefe der
Physik* (isotrop gegen Hay-Davies, linearer γ-Gang gegen Huld) — das Clipping an einem
2,5‑kW-Gerät ist dagegen keine Feinheit, sondern eine harte Grenze, die auch in einer
Überschlagsrechnung gilt. Wer einen Wechselrichter zuordnet, will ihn berücksichtigt sehen, egal
in welchem Modell.

Die Bitgleichheit bleibt davon unberührt: Sie gilt für Anlagen **ohne** Zuordnung, und das sind
alle Bestandsanlagen. Und der ausgegraute Knopf, der den Anwenderwunsch ausgelöst hat,
**verschwindet damit ersatzlos** — der Abschnitt „Wechselrichter und Stränge" ist in beiden
Modellen bedienbar.

Was in EINFACH **nicht** gilt: Hay-Davies und Huld bleiben ERWEITERT vorbehalten. Ein Strang in
EINFACH rechnet seine Gleichstromleistung mit der Modulformel des einfachen Modells — nur eben je
Strang und mit der Ausrichtung des Strangs.

### 4.4 Kennzahlen

Wie N2.3 es für Paket B vorgesehen hat: **ins Simulationsprotokoll und auf die PV-Karte**
(`PhotovoltaikReiter`, `PvDetailchips`), **nicht** in die Ergebnistabellen —
`Tab_ErgebnisPhotovoltaik` bleibt unverändert.

| Kennzahl | Formel | Einheit |
|---|---|---|
| DC/AC-Verhältnis je Gerät | `Σ P_STC,ger / P_AC_Nenn` | — |
| Clipping-Verlust | `Σ_i max(0, P_AC,roh − P_AC,nenn)` | kWh/a |
| Wechselrichterverlust | `Σ_i (P_DC,ger − P_AC,roh)` | kWh/a |
| Nachtverbrauch | `Σ_i (Stunden mit P_DC < P_Standby) · P_Nacht/1000` | kWh/a |
| Volllaststunden AC | `Erzeugung / Σ P_AC_Nenn` | h/a |
| gewichteter Jahreswirkungsgrad | `Σ P_AC / Σ P_DC,ger` | — |

Der letzte ist der interessanteste: Er ist die Zahl, mit der sich `η_euro` des Datenblatts gegen
das tatsächliche Betriebsverhalten dieser Anlage vergleichen lässt. Er gehört auf die PV-Karte.

Kennzahlen **je Strang** (Ertrag, Vollbenutzungsstunden) sind nicht vorgesehen — sie wären eine
neue Ergebnisebene mit eigener Speicherung. Wer sie will, bekommt sie in einer späteren Stufe.

> **Umgesetzt (S3, 06.09.2026), an drei Stellen und mit einer Ergänzung.** Die
> Kennzahlen stehen an `PvStrangModell.Geraetegruppe` und laufen von dort ins
> **Simulationsprotokoll** (eine Zeile je Gerät, eine je Anlage), auf die
> **PV-Karte** der Simulationskonfiguration (Zahl der Geräte und Stränge, DC/AC —
> beides Stammdaten, ohne Lauf ablesbar) und in den **Ergebnisreiter
> „Photovoltaik"** als zweite Tabelle mit einer Zeile je Gerät.
> `Tab_ErgebnisPhotovoltaik` bleibt unverändert.
>
> **Ergänzt ist der Clipping-ANTEIL [%]** — `Clipping / (Ertrag + Clipping)`.
> Bezugsgröße ist bewusst der ungeklippte WECHSELSTROMertrag und nicht die
> Gleichstromseite: Gefragt ist „wieviel der möglichen Einspeisung bleibt am
> Wechselrichter hängen".
>
> **Ohne Zuordnung entsteht keine einzige Zeile** — auch nicht im Protokoll. Der
> Referenzlauf schreibt es mit, und eine zusätzliche Zeile wäre schon ein
> Unterschied.

---

## 5. Import

### 5.1 Die CEC-Wechselrichterliste (SAM/NREL) — die Hauptquelle

Sie liegt **im selben Verzeichnis wie die Modulliste**, die EPOS-Plan schon lädt
(`EPOS.Kern/Allgemein/Import/CEC/CECDataService.cs:35-37`):

```
https://raw.githubusercontent.com/NREL/SAM/develop/deploy/libraries/CEC%20Inverters.csv
```

Damit ist der ganze Apparat bereits vorhanden und wörtlich wiederverwendbar: drei URLs als
Rückfallkette, 45 Sekunden Zeitgrenze je Versuch, ein 30‑Tage-Zwischenspeicher (`:72-79`), ein
Fortschrittsmelder mit Abbruch (iU9‑W13.0j) und die mehrzeilige Kopf-/Einheitenzeile, die der
CSV-Leser bereits kennt.

**Feldzuordnung:**

| CEC-Spalte | Einheit | → `Tab_Wechselrichter_STAMM` | Umrechnung |
|---|---|---|---|
| `Name` | | `Bezeichner` | der Herstellername steht als Präfix darin |
| (aus `Name`) | | `Firma` | Text vor dem ersten Doppelpunkt — wie beim Modulimport |
| `Paco` | W | `P_AC_Nenn` | **/1000** → kW |
| `Pdco` | W | `Sandia_Pdco` | — |
| `Vdco` | V | `Sandia_Vdco` | — |
| `Pso` | W | `P_Standby`, `Sandia_Pso` | — |
| `C0…C3` | 1/W, 1/V | `Sandia_C0…C3` | — |
| `Pnt` | W | `P_Nacht` | — |
| `Vdcmax` | V | `U_Dc_Max` | — |
| `Idcmax` | A | `I_Dc_Max` | je MPPT |
| `Mppt_low` | V | `U_Mpp_Min` | — |
| `Mppt_high` | V | `U_Mpp_Max` | — |
| `CEC_Type` | | `Beschreibung` | Text anhängen |
| — | | `Eta05…Eta100`, `Eta_Euro` | **gerechnet** nach 3.3.3 |
| — | | `Anzahl_Mppt`, `Straenge_Je_Mppt` | **nicht in der Liste** → NULL |

> **Gemessen bei der Umsetzung (06.09.2026).** Der Netzabruf hat funktioniert; die Liste
> führt **2 343 Geräte von 152 Herstellern** in 2 346 Zeilen (Kopf-, Einheiten- und
> `[0]`-Zeile). Sie liegt damit eine Größenordnung unter der Modulliste (20 746), bekommt
> aber dieselbe Behandlung — ein virtualisiertes Raster ab 120 Zeilen.
>
> **Zwei Korrekturen an der Tabelle oben:**
>
> * Eine Spalte **`CEC_Type` gibt es nicht.** Die Kopfzeile lautet
>   `Name, Vac, Pso, Paco, Pdco, Vdco, C0…C3, Pnt, Vdcmax, Idcmax, Mppt_low, Mppt_high,
>   CEC_Date, CEC_hybrid`. Die `Beschreibung` entsteht deshalb aus Herkunft, `CEC_Date`
>   und `Vac`.
> * Einen **Herstellernamen führt die Liste ebenfalls nicht** (anders als die Modulliste
>   mit `Manufacturer`). Er ist der Text vor dem ersten Doppelpunkt des Gerätenamens —
>   wie in der Tabelle beschrieben, aber ohne Rückfallspalte: Ein Gerät ohne Doppelpunkt
>   bekommt eine leere `Firma`.
>
> Die Umrechnung nach 3.3.3 wurde über **alle 2 343 Geräte** nachgerechnet: Jede der
> sechs Stützstellen liegt in (0; 1], und `η100 = Paco/Pdco` trifft auf zwölf Stellen.
> `Eta_Max` füllt der Import mit dem Maximum der sechs Stützstellen — die Liste führt
> keinen Maximalwirkungsgrad, und der wahre Scheitel liegt zwischen zwei Stützstellen;
> der Ausweis ist damit eine untere Schranke und keine erfundene Zahl.

Zwei Punkte zur Ehrlichkeit des Imports:

* **Die CEC-Liste führt keine MPPT-Zahl.** Sie bleibt NULL und ist von Hand zu pflegen; die
  Prüfungen P4/P5 rechnen dann auf **einem** MPPT — dem konservativen Fall — und melden es.
* **`Paco` ist Wirkleistung, nicht Scheinleistung.** `S_AC_Max` bleibt NULL und fällt in den
  Prüfungen auf `P_AC_Nenn` zurück.

Die Zeilenzahl der Liste ist bei der Umsetzung zu messen. Die Modulliste hat 20 746 Zeilen und
wird in einem virtualisierten Raster gezeigt (`PvModulImportDialog`, iU9‑W13.0l); die
Wechselrichterliste liegt in derselben Größenordnung und braucht dieselbe Behandlung.

### 5.2 PVsyst `.OND` — **UMGESETZT in `9ef8ca5`**

> **Anwenderentscheid W6‑O‑1 vom 06.09.2026, im Wortlaut:** „der OND-Import soll umgesetzt
> werden. baue daher den Modulimport schon jetzt um (Modulimport und Wechselrichter Import
> zwei Masken)". Umgesetzt ist beides in einem Schritt: der OND-Zweig **und** der eine
> Importwirt (5.5).

Das Gegenstück zur `.PAN`-Datei, die EPOS-Plan schon liest
(`EPOS.Kern/Allgemein/Import/Pan/PanDataService.cs`). Dasselbe Format: Abschnitte mit
`Schlüssel=Wert`, geschachtelt über Einrückung. Die interessanten Marken:

| OND-Schlüssel | → Katalog |
|---|---|
| `PNomConv` | `P_AC_Nenn` (kW) |
| `PMaxOUT` | `S_AC_Max` |
| `PNomDC` | `P_DC_Max` |
| `VAbsMax` | `U_Dc_Max` |
| `VMppMin`, `VMPPMax` | `U_Mpp_Min`, `U_Mpp_Max` |
| `VMppNom` | Bezugsspannung der Kennlinie |
| `IMaxDC` | `I_Dc_Max` |
| `NbInputs` / `NbMPPT` | `Anzahl_Mppt` |
| `PSeuil` | `P_Standby` |
| `Night_Loss` | `P_Nacht` |
| `EfficEuro` | `Eta_Euro` |
| `EfficMax` | `Eta_Max` |
| `ProfilPIO` (Wertepaare P_in/η) | `Eta05…Eta100` durch **Interpolation auf die Stützstellen** |

**Der OND-Import ist der einzige, der die Kennlinie direkt liefert** — `ProfilPIO` ist eine
Wertetabelle, aus der die sechs Stützstellen durch lineare Interpolation entstehen, ohne
Modellumweg. Er ist damit die *bessere* Quelle, aber die seltenere: OND-Dateien kommen vom
Hersteller oder aus PVsyst, nicht aus einem offenen Verzeichnis.

PVsyst führt `ProfilPIO` in drei Fassungen (untere, nominale, obere MPP-Spannung). **Empfohlen
ist die nominale Fassung**; die anderen zwei brauchte erst ein spannungsabhängiges Modell (E3).

> **Umgesetzt (06.09.2026, `9ef8ca5`) — mit vier Nachträgen zur Tabelle oben.**
> Der Dienst ist `EPOS.Kern/Allgemein/Import/OND/OndWechselrichterDienst.cs`, der Satz
> `OndWechselrichter.cs`; die Stützstellen rechnet
> `WechselrichterKennlinie.AusProfil`. Nachweis: `EPOS.Kern.Tests/OndImportTests` (20 Fälle)
> gegen die zwei synthetischen Proben `Referenzlaeufe/Importproben/ond_muster_2500tl.ond`
> (die Zahlen des Anhangs A: 2,50 kW, 1 MPPT, 80…500 V, 600 V, 12,0 A,
> η 0,900 / 0,940 / 0,962 / 0,970 / 0,975 / 0,970) und
> `ond_muster_10000tl_3profile.ond` (drei ProfilPIO-Fassungen).
>
> 1. **Die Einheiten der Datei sind nicht die des Katalogs.** PVsyst schreibt die
>    Leistungen des Wandlers (`PNomConv`, `PMaxOUT`, `PNomDC`, `PMaxDC`) in **kW**, die
>    Schwellen (`PSeuil`, `Pnight`) und die Punkte der Kennlinie in **W** und die
>    Wirkungsgrade (`EfficMax`, `EfficEuro`) in **Prozent**. Umgerechnet wird im
>    Import — dort, wo beide Konventionen nebeneinanderstehen.
> 2. **`ProfilPIO` führt Paare `P_in / P_out`, nicht `P_in / η`.** Der Wirkungsgrad ist
>    `P_out / P_in`; interpoliert wird **über `P_out`**, weil eine Stützstelle nach
>    3.3.1 an einem Anteil der AC-**Nenn**leistung hängt. Außerhalb des Bereichs, den die
>    Tabelle abdeckt, bleibt die Stützstelle NULL — eine fortgeschriebene Kurve wäre eine
>    erfundene Zahl.
> 3. **Der Bezeichner trägt den Hersteller.** Die Tabelle nennt nur „`Model` →
>    `Bezeichner`"; genommen wird `Manufacturer Model` — wörtlich das Muster des
>    PAN-Imports (`PanDataService.Aufnehmen`). Ein bloßes „2500TL" stünde im Katalog neben
>    CEC-Sätzen der Form „Hersteller: Modell" und wäre zwischen zwei Herstellern nicht
>    unterscheidbar. `Firma` bleibt der reine `Manufacturer`.
> 4. **Nachtverbrauch unter drei Namen.** Gelesen werden `Pnight`, `PNight` und
>    `Night_Loss` — je nach PVsyst-Stand steht das eine oder das andere in der Datei.
>
> **Was der OND-Import kann und der CEC-Import nicht** (offener Punkt W6‑O‑2): Er füllt
> `Anzahl_Mppt`, `S_AC_Max`, `P_DC_Max` und `U_Start`. Umgekehrt bleiben die
> `Sandia_*`-Spalten leer — eine OND-Datei führt kein Sandia-Modell; allein `VMppNom`
> steht als Bezugsspannung in `Sandia_Vdco`. **`Eta_Euro` und `Eta_Max` kommen aus der
> DATEI** (`EfficEuro`, `EfficMax`) und nicht aus der Rechnung: Anders als bei CEC nennt
> das Datenblatt sie selbst. Fehlen sie, wird gewichtet bzw. das Maximum der Stützstellen
> genommen — dieselbe untere Schranke wie bei CEC.
>
> **Bei drei Fassungen gilt die nominale** (`ProfilPIOV2`), und **welche es war, steht im
> Katalog**: Die Beschreibung des Satzes nennt Herkunft, Fassung, Baujahr und die
> Bemerkung der Datei. Der Import trifft hier eine Entscheidung — der Anwender soll sie
> nachlesen können.

### 5.3 Datenblatt von Hand

Über die Verwaltungsmaske (Abschnitt 6). Ein Datenblatt nennt in aller Regel: AC-Nennleistung,
maximale DC-Leistung, MPP-Fenster, maximale DC-Spannung, Eingangsstrom je Tracker, Zahl der
Tracker, Maximal- und Euro-Wirkungsgrad. Die Stützstellen der Kennlinie stehen **nicht** darin;
sie sind entweder aus dem Diagramm abzulesen oder leer zu lassen.

**Rückfall, wenn nur `Eta_Euro` bekannt ist:** Aus einem einzigen Wert lassen sich sechs
Stützstellen nicht rekonstruieren. Empfohlen ist, in diesem Fall die Kurvenform der Vorgabe
(0,94 / 0,975 / 0,97) so zu skalieren, dass ihr gewichteter europäischer Wirkungsgrad genau
`Eta_Euro` trifft — ein Faktor, eine Zeile, und das Protokoll sagt „Kennlinie aus dem
europäischen Wirkungsgrad geformt".

### 5.4 Dubletten

Der Import läuft über den vorhandenen Weg: `KatalogImportAblauf.PruefeKandidaten` →
`DublettenPruefung.PruefeKandidaten(Profil.Katalog, kandidaten)`
(`EPOS.Kern/Allgemein/Import/KatalogImportAblauf.cs:240`), Konfliktdialog
`ImportKonflikteDialog.razor`. Dafür braucht der Katalog eine Definition in `KatalogRegistry`
(`EPOS.Kern/Allgemein/Katalog/KatalogRegistry.cs:110-354`), nach dem Muster des PV-Eintrags
(`:163-176`):

```csharp
new KatalogDefinition
{
    Schluessel        = "WECHSELRICHTER",
    Tabelle           = SchemaKatalog.TAB_WECHSELRICHTER_STAMM,
    AusschlussSpalten = new[] { "Kosten" },          // Anwenderfeld, wie Modulkosten bei PV
    ImportSpalten     = new[] { "Firma", "P_AC_Nenn", "P_DC_Max", "U_Mpp_Min", "U_Mpp_Max",
                                "U_Dc_Max", "I_Dc_Max", "Anzahl_Mppt",
                                "Eta05", "Eta10", "Eta20", "Eta30", "Eta50", "Eta100",
                                "Eta_Euro", "P_Standby", "P_Nacht" }
    // VerwendungsPruefungen: LEER - Kopiersemantik, Projekte verweisen auf Tab_Wechselrichter
}
```

Die `Sandia_*`-Spalten stehen bewusst **nicht** in `ImportSpalten`: Zwei Katalogsätze, die sich
nur in `C3` unterscheiden, rechnen in EPOS-Plan identisch (3.3.2) — sie als verschieden zu melden
wäre falscher Alarm. Dieselbe Abwägung hat der PV-Eintrag mit `Technologie` in die andere Richtung
getroffen: Dort *gehört* die Spalte hinein, weil sie den Koeffizientensatz wählt.

### 5.5 Wo der Import in der Oberfläche sitzt — **EIN WIRT, UMGESETZT in `9ef8ca5`**

**Nicht** als fünfte Ausprägung von `KatalogImportProfil`: Dessen vier Ausprägungen sind
VDI‑3805-Dateiimporte (`KatalogImportProfil.cs:16-29`) mit gemeinsamem Parser und gemeinsamer
Einlesemaske. Der Wechselrichterimport ist ein CSV-/OND-Import und gehört zum Zwilling des
Modulimports.

**Empfohlen ist eine zweite Ausprägung des vorhandenen `PvModulImportDialog`** — dieselbe Bauart,
mit der `ModulKatalogDialog` „EINE Komponente, ZWEI Ausprägungen" löst. Gemeinsam sind Netzabruf
mit Fortschritt und Abbruch, Zwischenspeicher, virtualisiertes Raster, Filterleiste,
Detailfeldblock, Vorprüfung und Konfliktdialog; verschieden sind nur Spaltensatz, Zieltabelle und
Beschriftungen — also genau das, was ein Profil trägt. Die Alternative (ein eigener
`WechselrichterImportDialog`) verdoppelte 771 Zeilen für zwei Unterschiede.

Menüpunkt: **Administration → Datenimport → „Wechselrichter (CEC)…"**, neben
`MenuItem_PV_Import_CEC` (`EPOS.UI/Bausteine/Menuetabelle.cs`).

> **Umgesetzt (S1, 06.09.2026) — mit einer benannten Abweichung.** Geteilt ist alles,
> was sich teilen lässt, und zwar dort, wo es hingehört: der **Abrufapparat** im Kern
> (`CecWechselrichterDienst`, Zwilling von `CECDataService` mit denselben
> `CecFortschritt`-Schlüsseln), die **Vorprüfung** (`DublettenPruefung` über die
> Registry-Definition „WECHSELRICHTER"), das **Ergebnis der Vorprüfung**
> (`PvVorpruefung` — sie kennt keinen Satztyp und trägt unverändert) und **sämtliche
> Bausteine** (`Fortschritt` mit Abbrechen, virtualisiertes `Raster`, `Zeilenwahl`,
> `Formularraster`, `Warnbanner`, `Rueckfrage`, `ImportKonflikteDialog` in einer
> `Ueberlagerung`).
>
> **Nicht geteilt ist die `.razor`-Datei.** `PvModulImportDialog` ist auf 771 Zeilen
> typisiert gegen `UnifiedModule`: Spalten, Detailfelder und Filterrechnung stehen als
> Markup gegen konkrete Eigenschaften. Ihn auf eine neutrale Zeilenform umzubauen hieße,
> eine getestete Maske samt 594 Zeilen bunit-Fällen anzufassen, ohne dass S1 dadurch
> etwas könnte. Der Einwand von Rev. 1 („ein eigener Dialog verdoppelte 771 Zeilen für
> zwei Unterschiede") trifft die umgesetzte Fassung nicht: `WechselrichterImportDialog`
> hat **rund 300 Zeilen**, weil alles Teilbare im Kern und in den Bausteinen liegt.
> Die Zusammenlegung beider Wirte auf EINE profilgetriebene Komponente ist als **offener
> Punkt W6‑O‑1** festgehalten (Kapitel 12).

> **Zusammengelegt (06.09.2026, `9ef8ca5`) — W6‑O‑1 ist geschlossen.** Der Zeitpunkt ist genau
> der, den der offene Punkt selbst als den sinnvollen benannt hat: „der OND-Zweig, der
> ohnehin in denselben Wirt kommt".
>
> **`EPOS.UI/Dialoge/Photovoltaik/ModulImportDialog.razor`** (669 Z. statt 771 + 655) ist
> der eine Wirt mit zwei Ausprägungen — **Modul (CEC, CEC-Datei, PAN)** und **Wechselrichter
> (CEC, CEC-Datei, OND)**. Beide alten `.razor` und ihre `Daten.cs` sind gelöscht; es gibt
> nie zwei Fassungen derselben Maske.
>
> **Was den Umbau möglich gemacht hat, ist die neutrale Zeilenform.** Der Einwand von
> Rev. 3 („`PvModulImportDialog` ist auf 771 Zeilen typisiert gegen `UnifiedModule`") traf
> zu; er ist ausgeräumt, indem Spalten, Detailfelder, Reiter, Filter und Quellen **DATEN**
> geworden sind: `EPOS.Kern/Allgemein/Import/ModulImportProfil.cs`, Zwilling zu
> `ModulKatalogProfil` und nach demselben Muster wie `ModulFeldwert` im Modulkatalog. Eine
> Zeile ist eine `ImportZeile` mit Zellwerten, Detailwerten und den drei Größen, nach
> denen die Filterleiste einengt; **der Dialog kennt weder `UnifiedModule` noch
> `CecWechselrichter` noch `OndWechselrichter`**. Welchen Satztyp eine Quelle liefert,
> entscheidet die QUELLE und nicht die Ausprägung — der Wechselrichterimport bekommt aus
> der CEC-Liste einen `CecWechselrichter` und aus einer `.OND`-Datei einen
> `OndWechselrichter`, und beide füllen dieselbe Zeilenform.
>
> **Die zwei Hüllen sind eine** (`WindowsFormsApplication1/Views/Photovoltaik/ModulImportHuelle.cs`);
> `WinFormsNavigation` führt beide Maskenschlüssel darauf, `HilfeKontext` kennt den neuen
> Typnamen. **`PvVorpruefung` heißt jetzt `ImportVorpruefung`** — sie kannte nie einen
> Satztyp, und der alte Name war schon in S1 zu eng.
>
> **Beide Hilfeschlüssel bleiben gültig und keiner wandert:** Das Profil trägt
> `Main_PV_Test.btn_Help` bzw. `Form_WechselrichterImport.btn_Help`; `help_mapping.txt`
> ist unverändert.
>
> **Die 594 Zeilen bunit-Fälle des Modulimports sind mitgewandert** und stehen als
> Abschnitte 1 bis 5 in `EPOS.UI.Tests/Dialoge/ModulImportDialogTests` — Fall für Fall
> mit denselben Erwartungswerten, samt der Feldkarte von `Form_CECImport`. Daneben stehen
> die Fälle der Wechselrichterausprägung (Abschnitt 6, aus `WechselrichterDialogTests`
> hierher geholt), der OND-Zweig (7) und der Dateiweg der Auslieferungsliste (8).
>
> **Eine Zeilenwahl, kein Mehrfachimport.** Beide Vorläufer hatten `MultiSelect = false`
> und schrieben genau EINEN Satz; `ImportVorpruefung` trägt deshalb einen Befund und keine
> Liste. Geteilt ist der Baustein `Zeilenwahl` (der einen Mehrfachmodus kann), die
> Semantik bleibt die des Bestands — ein Mehrfachimport wäre eine Fachänderung und kein
> Zusammenlegen.
>
> **Eine benannte Abweichung bleibt:** Das Zeichen für „führt die Quelle nicht" ist
> BITGLEICH aus dem Bestand übernommen — der Modulimport zeigt den Bindestrich seines
> Vorläufers (`ShowDetail` :425‑427), der Wechselrichterimport den Gedankenstrich des
> Hauses (`ParameterVerwendung.LEER`). Beides steht als Profildatum `Strich`; sie
> anzugleichen ist eine Anzeigefrage und keine Portentscheidung.

---

## 6. Verwaltung „Wechselrichter"

**Empfohlen ist eine dritte Ausprägung von `ModulKatalogDialog`** (`ModulKatalogProfil.cs`, heute
`Stromspeicher` und `Photovoltaik`). Die Maske ist „Familie C" der Vermessung: Browser **und**
Editor in einem — Liste links, Formularraster rechts, „Speichern" schreibt unmittelbar in die
Stammtabelle, „Löschen" mit Rückfrage. Genau das braucht ein Gerätekatalog, den man auch von Hand
pflegt.

Die Alternative `KatalogBrowserDialog` (Familie B, vier Ausprägungen) trennt Browser und Editor
und ist für Kataloge gedacht, deren Sätze in einem eigenen Bearbeitungsdialog stehen — mehr
Aufwand ohne Gewinn.

Das Profil trägt (nach dem Muster `ModulKatalogProfil.cs:254-297`):

* **Stammtabelle** `Tab_Wechselrichter_STAMM`, Titel „Verwaltung Wechselrichter", Listentitel
  „Wechselrichter", **Herstellerfilter ja** — die Photovoltaik hat keinen, aber bei einigen
  tausend Geräten braucht es ihn.
* **Gruppe 0 „Gerät":** Bezeichner (beim Bearbeiten gesperrt), Firma, Beschreibung,
  AC-Nennleistung [kW], max. AC-Scheinleistung [kVA], max. DC-Leistung [kW], Kosten [€].
* **Gruppe 1 „Eingang":** MPP-Fenster von/bis [V], max. DC-Spannung [V], Einschaltspannung [V],
  max. DC-Strom je MPPT [A], Anzahl MPPT, Stränge je MPPT.
* **Gruppe 2 „Wirkungsgrad":** die sechs Stützstellen [–], Euro-Wirkungsgrad [–],
  Maximalwirkungsgrad [–], Standby [W], Nachtverbrauch [W].
* Pflichtfeld ist allein **`P_AC_Nenn`** — wie bei der Photovoltaik allein die Nennleistung
  (`ModulKatalogProfil.cs:278-279`); alles andere darf leer bleiben und schaltet dann die
  zugehörige Prüfung ab.

Plausibilitätsprüfung beim Speichern, nach dem Vorbild `PvModulPlausibilitaet.cs`: Wirkungsgrade
in (0; 1], `U_Mpp_Min < U_Mpp_Max ≤ U_Dc_Max`, `Anzahl_Mppt ≥ 1`, `P_AC_Nenn > 0`. Ein
Kopierfehler wie der von 2026 in `alpha_SC`/`beta_OC`/`T_NOCT` soll hier gar nicht erst entstehen
können.

Menüpunkt: **Administration → Energiesysteme → „Wechselrichter"**, unmittelbar nach
„Photovoltaik Module". (Rev. 1 nannte hier ein Untermenü „Photovoltaik → Bearbeiten"; das
ist mit dem Anwenderentscheid **W16c‑E‑6** vom selben Tag aufgelöst — `MenuItem_PV` trägt
sein Ziel seither selbst.) Seitenschlüssel `WechselrichterAdmin`.

> **Umgesetzt (S1, 06.09.2026).** Die dritte Ausprägung von `ModulKatalogProfil` führt
> **25 Felder in drei Gruppen**; die dritte Gruppe (`GruppeDrei`) ist neu — ein Block mit
> zwanzig Feldern wäre nicht lesbar. Der Feldschlüssel ist hier der SPALTENNAME der
> Stammtabelle: Es gibt keinen WinForms-Vorläufer, dessen Feldnamen zu erben wären, und
> eine zweite Schreibweise neben `WechselrichterSchema` wäre eine zweite Wahrheit.
>
> Der **Herstellerfilter** ist ebenfalls neu und hängt an zwei Delegaten
> (`ModulKatalogWege.Hersteller`/`.ListeGefiltert`); ohne sie zeichnet der Dialog die
> Filterzeile nicht — die Hausregel „kein Delegat, kein Bedienelement". Damit hat
> `ModulKatalogProfil.HatHerstellerfilter`, seit W14a.0a eine Eigenschaft ohne Leser,
> endlich einen.
>
> **Ein leeres Zahlenfeld bleibt NULL**, nicht 0. Die zwei älteren Ausprägungen weichen
> bei jedem leeren Feld auf 0 aus; hier wäre das falsch — eine 0 bei `U_Dc_Max` hieße
> „Grenze null Volt" und sperrte jeden Strang, während NULL „keine Prüfung" heißt (3.1).
>
> Die **sieben Sandia-Spalten führt die Maske nicht** (von Hand sind sie nicht pflegbar);
> beim Ändern eines importierten Satzes kommen sie aus dem BESTAND, damit sie nicht mit
> NULL überschrieben werden — dieselbe Überlegung wie bei `alpha_SC`/`beta_OC` in
> `PhotovoltaikStammCtrl.SpeichernAus`. `Herkunft` steht als gesperrtes Feld daneben und
> sagt, woher der Satz kommt (`CEC` / `OND` / `HAND`).
>
> Der Aufklapper **„Alle Parameter und ihre Verwendung"** (W14a‑E‑8) trägt die achte
> Anlagenart. In S1 hat **keine** Spalte einen Leser im Rechenweg: 27 stehen als
> „Dialog", die sieben Sandia-Werte als „nicht verwendet".

---

## 7. Die Oberfläche im PV-Dialog

Der ausgegraute Knopf entfällt. An seine Stelle tritt im Abschnitt „PV Anlage Eigenschaften" ein
neuer Abschnitt **„Wechselrichter und Stränge"** (Mockup M1).

**Aufbau:**

```
Neigung [°]    30        Azimut [°]    0        Anzahl Module   10   (abgeleitet)
Rechenmodell   Einfach ▾      Systemverluste [%]   12,00
─────────────────────────────────────────────────────────────────────────────
Wechselrichter und Stränge                              DC/AC 1,10   ● grün
Filtern nach Hersteller:  Alle ▾
┌──────┬──────────────────────┬──────────────────┬──────┬──────┬───────┬──────┬───────┬────────┐
│ Rang │ Modul                │ Wechselrichter   │ Ger. │ MPPT │ Reihe │ Par. │ Neig. │ Azimut │
├──────┼──────────────────────┼──────────────────┼──────┼──────┼───────┼──────┼───────┼────────┤
│  1   │ (Modul der Anlage) ▾ │ Muster 2500TL  ▾ │  1   │  1   │  10   │  1   │ (30)  │  (0)   │
└──────┴──────────────────────┴──────────────────┴──────┴──────┴───────┴──────┴───────┴────────┘
Leer heisst: der Strang rechnet mit dem Modul der Anlage.
● grün  Strang 1: 10 Module in Reihe · U_oc(−10 °C) 425 V ≤ 600 V · MPP 261…355 V im
        Fenster 80…500 V · I 9,55 A ≤ 12,0 A
                                              [ Strang anlegen ]  [ Entfernen ]
```

**Die Regeln dahinter:**

* **Die Tabelle ist leer, solange niemand einen Strang anlegt.** Dann steht dort die Zeile
  „Kein Wechselrichter zugeordnet — die Anlage rechnet mit dem Wirkungsgrad *0,95* und ohne
  Clipping" (bzw. mit den gepflegten Anlagenwerten), und dahinter der Knopf „Wechselrichter der
  Anlage…", der die heutige Überlagerung öffnet. **Der bestehende Weg bleibt also erreichbar** —
  er ist nur nicht mehr der einzige und nicht mehr gesperrt.
* **Die Wechselrichterspalte ist eine Klappliste aus dem Katalog**, mit demselben
  Herstellerfilter wie die Modulliste. Ein Gerät, das im Projekt noch nicht liegt, wird beim
  Übernehmen kopiert (`CopyFromStamm`) — genau wie ein Modul. **Der Filter steht als eigene Zeile
  ÜBER der Tabelle** (W6‑O‑4) und wirkt auf die Klappliste aller Zeilen; ein bereits gewähltes
  Gerät bleibt in SEINER Zeile sichtbar, auch wenn der Filter es ausschliesst. Er ist vom
  Modulfilter unabhängig — der Gerätehersteller kann ein anderer sein als der Modulhersteller.
* **Die Modulspalte ist eine Klappliste aus dem Modulkatalog** (W6‑O‑6), mit
  „(Modul der Anlage)" als Vorgabe. Sie wird gebraucht, wenn eine Anlage mit einem zweiten
  Modultyp erweitert wurde; ohne Eintrag rechnet der Strang mit dem Modul der Anlage.
* **Die Ampelzeile steht unter jeder Strangzeile**, nicht in einer eigenen Spalte: Sie trägt einen
  Satz mit Zahlen, und ein Satz braucht Breite. Grün = alle Prüfungen bestanden, gelb =
  P3/P5/P6/P7 verletzt oder Werte fehlen, rot = P1/P2/P4 verletzt. **Rot verhindert das Speichern
  nicht** — ein Planer darf einen Zwischenstand ablegen; rot verhindert nichts, es sagt etwas.
* **Die DC/AC-Anzeige** steht im Kopf des Abschnitts, je Gerät eine Zeile, wenn es mehrere gibt.
* **„Anzahl Module" wird abgeleitet**, sobald ein Strang existiert: `Σ (Reihe × Parallel)`, und
  das Feld schaltet auf nur-lesend mit dem Hinweis „aus der Strangtabelle". Ohne Strang bleibt es
  ein Eingabefeld wie heute (Entscheidungsfrage **W6‑E‑2‑Q9**).
* **Neigung und Azimut je Strang stehen in Klammern, solange sie leer sind** — die Klammer zeigt
  den geerbten Anlagenwert. Wer hineinschreibt, macht das Teilfeld eigenständig; wer das Feld
  leert, erbt wieder.

**In EINFACH ist der Abschnitt vollständig bedienbar** (Empfehlung zu Q5, Begründung in 4.3). Was
sich zwischen den Modellen unterscheidet, sagt eine Zeile unter dem Rechenmodell: „Einfach:
isotrope Einstrahlung, linearer Temperaturgang. Der Wechselrichter rechnet in beiden Modellen."

**Was entfällt:** der gesperrte Knopf „Wechselrichter…" in seiner heutigen Form und mit ihm die
Sperrregel `disabled="@(!Zeile.ModellErweitert)"`. Was bleibt: die Überlagerung dahinter, als
Anlagenrückfall ohne Zuordnung.

> **Umgesetzt (S2, 06.09.2026):** `EPOS.UI/Dialoge/Erzeuger/PvStraengeFelder.razor` mit
> `PvStrangDaten.cs` (Zeilentyp, Ampelzeile, Textbündel); die Hülle ist
> `WindowsFormsApplication1/Views/Photovoltaik/PhotovoltaikHuelle.cs`.
>
> **Eine Abweichung gegenüber dem Aufbau oben, und sie ist eine Vereinfachung:** Die
> Überlagerung mit den fünf Anlagenwerten ist aus `PvModellFelder` HIERHER gezogen. Der
> Knopf „Wechselrichter der Anlage…" steht damit in der Leiste des Abschnitts, wie im
> Mockup — und alles, was den Wechselrichter betrifft, steht an einer Stelle.
> `PvModellFelder` trägt seither nur noch Rechenmodell, Wirkungsgrad, Systemverluste und
> die neue Modellzeile.
>
> **Die Klappliste zeigt den KATALOG, die Zeile trägt die PROJEKTKOPIE.** Beim Wählen wird
> übernommen (`WechselrichterCtrl.CopyFromStamm`), genau wie ein Modul; das Band zwischen
> Zeile und Liste ist der `Bezeichner` — denselben Schlüssel benutzt `CopyFromStamm`, um
> eine vorhandene Kopie wiederzufinden. Ist der Katalogsatz später gelöscht, steht in der
> Liste nichts gewählt, die Zeile behält ihr Gerät, und die Ampel rechnet weiter damit.
>
> **Der Herstellerfilter ist seit dem 06.09.2026 gebaut** (Anwenderentscheid **W6‑O‑4**,
> umgesetzt in `35a48eb`) — und zwar so, wie S2 es vorgezeichnet hatte: als eigene Zeile
> **ÜBER** der Tabelle, nicht in ihr. Er nimmt dieselben zwei Gaben wie der Filter über der
> Modulliste (`Hersteller` und ein `Filtern`-Delegat, hier auf
> `WechselrichterStammCtrl.Hersteller`/`Filtern`) und wirkt auf die Klappliste **aller**
> Strangzeilen. **Ein bereits gewähltes Gerät bleibt sichtbar**, auch wenn der Filter es
> ausschliesst — sonst stünde in der Zeile nichts, und der Anwender hielte die Zuordnung für
> verloren. Ohne Herstellerliste zeichnet die Komponente keine Filterzeile.
>
> **Die Modulspalte je Strang steht seit demselben Tag daneben** (**W6‑O‑6**): Klappliste über
> den Modulkatalog, „(Modul der Anlage)" als Id 0 voran, die Zeile trägt die Projektkopie, und
> unter der Tabelle sagt eine Herleitungszeile, was „leer" bedeutet.
>
> **Neigung und Azimut stehen als PLATZHALTER in Klammern** — dafür hat `Ganzzahlfeld`
> eine Gabe `Platzhalter` bekommen (Gegenstück zu `Textfeld` und `Auswahlfeld`). Eine
> geschriebene 0 wäre eine gültige Ausrichtung (Süden) und von einem geerbten Wert nicht
> mehr zu unterscheiden.

### 7.1 W6‑E‑3 — zwei SICHTBARE Optionen statt einer stillen Vorrangregel

**Anwenderwunsch vom 06.09.2026, im Wortlaut:**

> „Gebe zwei Optionen - vereinfacht -> Berechnung ohne Wechselrichter (Pauschalen) 2. mit
> Wechselrichter (wie Vorschlag)"

Er ergänzt Abschnitt 3.5. Dort steht die Vorrangregel **still**: „Führt die Anlage mindestens
eine Zeile in `Z_AnlageStrang` mit `ID_Wechselrichter`, dann rechnet die Strangzuordnung."
W6‑E‑3 verlangt stattdessen eine **Wahl, die der Anwender sieht** — im Abschnitt „Wechselrichter
und Stränge", über der Strangtabelle:

```
Wechselrichter   ( ) vereinfacht — Pauschalen ohne Wechselrichter
                 (•) mit Wechselrichter — Katalog, Stränge, Kennlinie, Clipping
```

* **„vereinfacht"** ist der Weg von heute: Wirkungsgrad 0,95 bzw. der gepflegte Anlagenwert,
  `PV_Systemverluste`, und im Modell ERWEITERT die fünf Anlagenspalten aus Migrationsschritt 64.
* **„mit Wechselrichter"** ist der Weg aus Kapitel 4: Katalog, Strangzuordnung, Kennlinie mit
  sechs Stützstellen, Clipping je Gerät, Nachtverbrauch.

**Umgesetzt in Stufe S2** (Punkt S2.4, Commit `c02cd99`): als `Optionsgruppe` über der
Strangtabelle, mit `Tab_Energieanlagen.PV_Wechselrichterweg` als Ablage und den zwei
Persistenzwerten `DbWerte.PV_WR_WEG_VEREINFACHT` / `…_KATALOG`. In S1 stand er nur hier —
S1 fasste weder den PV-Dialog noch den Rechenweg an.

#### Wo der Schalter im Datenmodell liegt — Empfehlung

Zwei Möglichkeiten stehen zur Wahl:

| | **(a) eigene Spalte an der Anlagenzeile** | **(b) abgeleitet aus der Strangtabelle** |
|---|---|---|
| Ablage | `Tab_Energieanlagen.PV_Wechselrichterweg` TEXT(20); NULL = „vereinfacht" | keine; die Frage lautet „gibt es eine Strangzeile mit `ID_Wechselrichter`?" |
| Migration | eine Spalte, in Schritt 66 zusammen mit `Z_AnlageStrang` | keine |
| Abschalten | ein Klick — die Strangzeilen bleiben liegen | nur durch **Löschen** der Strangzeilen |

**Empfohlen ist (a): eine eigene Spalte.** Fünf Gründe, der erste ist der entscheidende:

1. **Eine Option, die man nicht abwählen kann, ist keine Option.** Bei (b) heißt „zurück auf
   vereinfacht": die Strangzuordnung löschen. Der Planer verlöre damit genau die Arbeit, die er
   vergleichen will — und ein Vergleich der zwei Wege ist der Zweck des Wunsches („gebe zwei
   Optionen"). Mit (a) parkt die Zuordnung und rechnet nicht mit.
2. **Es ist dieselbe Bauart wie `PV_Modell`** (Schritt 64): ein sichtbarer Schalter an derselben
   Anlagenzeile, mit NULL als dem Wert, der nichts ändert. Ein zweiter Schalter derselben Art
   gehört daneben, nicht in eine abgeleitete Abfrage.
3. **Die Ergebnisneutralität wird zur Aussage statt zum Zufall.** Bei (a) ist „NULL = vereinfacht"
   eine Zusage über die Spalte; bei (b) hinge sie daran, dass keine Bestandsanlage je eine
   Strangzeile bekommt — eine versehentlich angelegte Zeile änderte still ein Ergebnis.
4. **Der Rechenweg liest die Spalte dort, wo er ohnehin liest.** `SimulationPV.Berechnung` holt
   an einer Stelle bereits fünf PV-Spalten derselben Zeile; die sechste kostet nichts. (b)
   verlangte je Anlage eine Abfrage auf `Z_AnlageStrang`, bevor überhaupt feststeht, welchen Weg
   sie geht.
5. **Die Oberfläche kann den Grund sagen.** „mit Wechselrichter" ohne Strangzeile ist ein
   erklärbarer Zustand: Die Option bleibt anklickbar, trägt `aria-disabled` und den Satz „Es ist
   noch kein Strang zugeordnet" — die weiche Sperre aus **W16b‑E‑6**. Bei (b) gäbe es diesen
   Zustand gar nicht, und damit auch keinen Ort für den Hinweis.

**Was sich dadurch an Abschnitt 3.5 ändert.** Die Vorrangregel bleibt, sie bekommt nur eine
Bedingung davor:

```
PV_Wechselrichterweg = KATALOG  UND  mindestens eine Strangzeile mit ID_Wechselrichter
   -> die Strangzuordnung rechnet
sonst
   -> der Weg von heute, Zeichen für Zeichen
```

Die Bitgleichheit des Referenzlaufs ist damit unverändert zugesichert: Kein Bestandsprojekt hat
eine Strangzeile, und keines hat die Spalte gefüllt. **Zwei Bedingungen statt einer machen die
Zusage stärker, nicht schwächer.**

Die zwei Persistenzwerte gehören nach `DbWerte` (Drei-Schichten-Regel), die Beschriftungen nach
`MyResource`; die Spalte entsteht in **Schritt 66** zusammen mit `Z_AnlageStrang`, damit S2 einen
Migrationsschritt hat und nicht zwei.

> **Umgesetzt (S2), mit einer Verschärfung.** NULL und `PV_WR_WEG_VEREINFACHT` rechnen
> beide „vereinfacht", sind aber NICHT dasselbe: NULL heißt „nie gewählt". Der Dialog
> schreibt `VEREINFACHT` nur, wenn der Bestand vorher `KATALOG` trug — ein Speichern
> ohne Entscheidung erfindet keine Entscheidung, und der Roundtrip macht aus einer nie
> gepflegten Zeile keine gepflegte (dieselbe Regel wie bei `PV_Modell`).
>
> Die **weiche Sperre** aus W16b‑E‑6 steht wie beschrieben: „mit Wechselrichter" ohne
> Strangzeile bleibt anklickbar, trägt `aria-disabled="true"` und den Grund als `title`,
> und der Versuch MELDET sich über ein `Warnbanner` mit `Verfaellt` = 3 s — er handelt
> nicht. Dafür hat der Baustein `Optionsgruppe` zwei neue Gaben bekommen
> (`WeichGesperrt`, `Verweigert`), wörtlich nach `Reiterblatt.Sperrgrund` /
> `Reiter.Verweigert`; die Stilregel nennt `:disabled` und `[aria-disabled="true"]` in
> EINEM Selektor, weil ein Anwender „geht nicht" nicht nach der Bauart unterscheidet.
>
> Und der Schalter **parkt**, er löscht nicht: Zurück auf „vereinfacht" lässt die
> Strangzeilen stehen — genau der Grund 1 der Empfehlung.

---

## 8. Vorschlag in drei Stufen

### Stufe S1 — Katalog, Verwaltung, Import (**ohne jede Rechenwirkung**) — **UMGESETZT in `40fc542`**

| Nr. | Inhalt | Stand |
|---|---|---|
| S1.1 | Migrationsschritt **65**: `Tab_Wechselrichter_STAMM` + `Tab_Wechselrichter`, DDL in `EPOS.Kern/Allgemein/Update/WechselrichterSchema.cs`, Tabellennamen in `SchemaKatalog`, `SchemaStand.Zielversion` = 65, `Werkzeuge/Testdatenbankschema` und `Referenzlaeufe/Kenndaten_Test.sqlite` nachgezogen | **umgesetzt** (`ee1dc44`); `001_grundschema.sql` und der Typkatalog blieben unberührt, Begründung in 3.6 |
| S1.2 | `WechselrichterModel` (alle Fachwerte `double?`/`int?` — NULL heißt „keine Prüfung"), `WechselrichterStammCtrl`, `WechselrichterCtrl.CopyFromStamm` | **umgesetzt** (`33ecede`) |
| S1.3 | `KatalogRegistry`-Definition „WECHSELRICHTER" (5.4) — der zwanzigste Katalog | **umgesetzt** (`6a6426c`) |
| S1.4 | Dritte Ausprägung von `ModulKatalogProfil`/`ModulKatalogDialog` (25 Felder, drei Gruppen, Herstellerfilter), Menüpunkt, `Masken`/`Seitenschluessel`, `WinFormsNavigation`, `WechselrichterAdminHuelle`, Parameterübersicht (achte `Anlagenart`) | **umgesetzt** (`d7d25f3`) |
| S1.5 | `CecWechselrichterDienst`, `CecWechselrichter`, `WechselrichterKennlinie` (Sandia→Stützstellen), `WechselrichterImportDialog` + Hülle, Menüpunkt, Importprobe `cec_wechselrichter_21.csv` | **umgesetzt** (`119537f`); Abweichung zur „zweiten Ausprägung" in 5.5 benannt, offener Punkt W6‑O‑1 |
| S1.6 | `WechselrichterPlausibilitaet` (Katalogsatz) + Proben | **umgesetzt** (`33ecede`, Proben in `40fc542`) |
| S1.7 | Ressourcenschlüssel de + en, Präfix `WRK_` | **umgesetzt** (`33ecede`): **82 Schlüssel** in beiden Sprachen, `Resource.Designer.cs` neu erzeugt |
| S1.8 | Proben: Kern und bunit | **umgesetzt** (`40fc542`): 36 Kern-Fälle, 17 bunit-Fälle |
| S1.9 | Fortschreibung dieses Papiers | **dieses Dokument (Rev. 2)** |

**Abnahme S1 — nachgewiesen am 06.09.2026:**

| Kriterium | Beleg |
|---|---|
| Migration idempotent (Zweitlauf ohne DDL) | `Werkzeuge/Testdatenbankschema` meldet im zweiten Lauf „vorhanden / 0 Tabelle(n) angelegt"; dazu der Kern-Fall `Der_Migrationsschritt_65_ist_idempotent` |
| Katalog und Projektkopie spaltengleich | `Katalog_und_Projektkopie_sind_spaltengleich` — 34 zu 34, Unterschied nur `ReadOnly` gegen `ID_Projekt` |
| Anlegen, kopieren, löschen | `Der_Katalogsatz_laesst_sich_anlegen_lesen_aendern_und_loeschen`, `Die_Projektkopie_uebernimmt_jede_Fachspalte` |
| Dublettenprüfung meldet den Zweitimport | `Die_Dublettenpruefung_erkennt_den_Zweitimport`, `Ein_anderer_Preis_macht_keinen_anderen_Wechselrichter` |
| CEC-Import | `Die_Importprobe_liest_einundzwanzig_Geraete`, `Die_Feldzuordnung_des_Imports_stimmt`, `Jedes_Geraet_der_Importprobe_ist_plausibel` |
| Sandia → Stützstellen | `Die_Stuetzstelle_bei_Nennlast_ist_Paco_durch_Pdco` (zwölf Stellen), `Die_Kennlinie_steigt_bis_zur_halben_Last` |
| **Referenzlauf byte-gleich** | 1030 / 1007 / 1017 gegen `Referenzlaeufe/2026-09-05_R2_Zeitbasis` — **byte-gleich**, vor und nach dem Nachziehen der Testdatenbank |
| SQL-Dialekt | `Werkzeuge/SqlDialektPruefer` gegen die nachgezogene Testdatenbank: **0 Fundstellen** |

**Was auf Windows noch abzunehmen ist**, steht als Abnahmepunkte A‑W6‑E‑2‑S1‑1 ff. im
Umsetzungsprotokoll: Die zwei Menüpunkte, der Netzabruf am echten Gerät und die Anmutung der
Verwaltung sind ohne Windows nicht prüfbar.

### Stufe S2 — Strangzuordnung, Plausibilität, Oberfläche — **UMGESETZT in `c02cd99`**

| Nr. | Inhalt | Stand |
|---|---|---|
| S2.1 | Migrationsschritt **66**: `Z_AnlageStrang` — **und** die Spalte `Tab_Energieanlagen.PV_Wechselrichterweg` des Schalters aus **W6‑E‑3** (7.1) | **umgesetzt** (`ef99eed`); DDL in `AnlageStrangSchema` (eigene Klasse je Schritt), Spalte in `SchemaKatalog.Schritt66_PvWechselrichterweg` und damit in `Alle`; `Zielversion` = 66, Testdatenbank nachgezogen; Begründungen in 3.6 |
| S2.2 | `AnlageStrangModel`, `AnlageStrangCtrl` (Lesen/Schreiben je Anlage), Anbindung an `AnlagenSql`/`WizardCtrl` — **Falle N3.3** | **umgesetzt** (`a354132`); Bauart `Z_AnlageSenkeCtrl`, alle Fachfelder `int?` (NULL trägt eine Aussage, und Azimut 0 ist Süden). Die Falle löst **Block ST1** in `WizardCtrl`: `StraengeSichern` vor beiden DELETE-Wegen, `StraengeWiederherstellen` nach dem Add — wörtlich nach der Senkenrettung (Block S1) |
| S2.3 | `StrangPlausibilitaet` mit P1–P8 (4.2) + Proben mit gerechneten Grenzfällen | **umgesetzt** (`01cff2a`); Befund je Strang, je MPPT und je GERÄT, mit den ZAHLEN im Ergebnis statt nur im Satz |
| S2.4 | Abschnitt „Wechselrichter und Stränge" im PV-Dialog (Abschnitt 7) samt der **zwei sichtbaren Optionen** aus W6‑E‑3 (7.1); die Sperrregel des Knopfes entfällt | **umgesetzt** (`c02cd99`): `PvStraengeFelder.razor` mit Optionsgruppe, Strangtabelle, Ampelzeilen, DC/AC-Chips und der aus `PvModellFelder` herübergezogenen Überlagerung; abgeleitete Modulzahl (Q9), weiche Sperre (W16b‑E‑6), S3-Hinweis |
| S2.5 | Projekttransfer (`.wpx`): die zwei neuen Tabellen in Export und Import, Zielversion 66 | **umgesetzt** (`822c0e5`); `Tab_Wechselrichter` kommt über `ID_Projekt` generisch mit, `Z_AnlageStrang` braucht einen festen `KINDER`-Eintrag und `ID_Wechselrichter` einen in `FK_MAP` |
| S2.6 | Ressourcenschlüssel de + en (≈ 40) | **umgesetzt**: **53 Schlüssel** mit Präfix `PVS_` in beiden Sprachen (28 für die Ampelsätze, 25 für die Maske), `Resource.Designer.cs` neu erzeugt |
| S2.7 | Mockup, Konzeptfortschreibung | **umgesetzt**: M1 um die zwei Optionen und den S3-Hinweis ergänzt; dieses Dokument (Rev. 3) |

**Abnahme S2 — nachgewiesen am 06.09.2026:**

| Kriterium | Beleg |
|---|---|
| Migration 66 idempotent (Zweitlauf ohne DDL) | `Werkzeuge/Testdatenbankschema` meldet im zweiten Lauf „0 Spalte(n) angelegt, 0 Tabelle(n) angelegt"; dazu der Kern-Fall `Der_Migrationsschritt_66_ist_idempotent` |
| Schema wie im Konzept 3.4 | `Die_Strangtabelle_fuehrt_die_zwoelf_Spalten_des_Konzepts`, `Die_Strangtabelle_fuehrt_genau_zwei_Fremdschluessel` (`ID_Anlage` CASCADE, `ID_Wechselrichter` restriktiv, `ID_PV` ohne) |
| Rundweg je Anlage | `Die_Straenge_einer_Anlage_reisen_unveraendert_hin_und_zurueck`, `Neigung_und_Azimut_ohne_Eintrag_bleiben_NULL`, `Die_Raenge_werden_beim_Schreiben_neu_vergeben`, `Eine_leere_Liste_loescht_die_Straenge` |
| **Die Falle N3.3 ist zu** | `Mit_der_Anlage_fallen_ihre_Straenge` (die Kaskade) und `Der_Speicherweg_rettet_die_Straenge_ueber_Loeschen_und_Neuanlegen` (der ganze Weg `Del_Projekt_Waermeerzeuger` + `Add_WP_Waermeerzeuger`) |
| Der Schalter reist NULL-treu | `Der_Wechselrichterweg_reist_unveraendert_hin_und_zurueck`, `Der_Wechselrichterweg_ist_keine_Fachspalte` |
| **Die Ampel gegen Anhang A** | `Anhang_A_zehn_Module_in_Reihe_sind_gruen` — 425,3 V / 260,9 V / 355,3 V / 9,5515 A / DC/AC 1,10076, Zahl für Zahl; dazu die drei Gegenproben `…_vierzehn_Module_sind_gelb_ueber_P6`, `…_fuenfzehn_Module_sind_rot_ueber_P1`, `…_zwei_Straenge_parallel_sind_rot_ueber_P4` |
| Grenzfälle je Prüfung | `P1_genau_auf_der_Grenze_bleibt_gruen`, `P2_unter_dem_MPP_Fenster_ist_rot`, `P3_ueber_dem_MPP_Fenster_ist_gelb`, `P5_zu_viele_Straenge_je_MPPT_sind_gelb`, `P6_unter_dem_Band_ist_ebenfalls_gelb`, `P7_ueber_der_DC_Grenze_ist_gelb`, `P8_eine_abweichende_Modulsumme_ist_gelb` |
| Fehlende Werte (W6‑O‑2) | `Ein_fehlender_Modulwert_macht_gelb_und_nicht_pruefbar`, `Ohne_MPPT_Zahl_rechnet_die_Pruefung_auf_einem_Tracker`, `Ein_Strang_ohne_Geraet_ist_gelb` |
| Ost/West | `Ost_West_ist_ein_Geraet_mit_zwei_Trackern`, `Zwei_Geraetenummern_sind_zwei_Befunde` |
| Oberfläche | 18 bunit-Fälle `PvStraengeFelderTests` (Optionswahl, weiche Sperre samt `aria-disabled`/`title`, Strang anlegen/entfernen mit Rangvergabe, Katalogsatz übernehmen, Ampelzeilen und -farben, geerbte Werte in Klammern, S3-Hinweis in beiden Wegen, Überlagerung ohne Sperre); dazu `PvModellFelderTests.Der_gesperrte_Wechselrichterknopf_ist_fort` |
| Projekttransfer | `P6_Wechselrichter_und_Straenge_reisen_mit_und_zeigen_auf_die_eigene_Kopie` (mit Versatz-Prüfung), `P7_Ein_Paket_ohne_Wechselrichter_und_Straenge_laedt_weiter` |
| **Referenzlauf byte-gleich** | 1030 / 1007 / 1017 gegen `Referenzlaeufe/2026-09-05_R2_Zeitbasis` — **byte-gleich**, vor und nach dem Nachziehen der Testdatenbank |
| SQL-Dialekt | `Werkzeuge/SqlDialektPruefer` gegen die nachgezogene Testdatenbank: **0 Fundstellen** |
| Keine Rechenwirkung | `Der_Wechselrichter_rechnet_in_S2_noch_nicht` (umbenannt von `…_in_S1_…`): Keine Katalogspalte ist `Simulation` oder `Wirtschaftlichkeit`. S2 hat acht Spalten einen zweiten LESER gegeben (`StrangPlausibilitaet`, die Ampel des Dialogs) und trotzdem keinen RECHNER |

**Was auf Windows noch abzunehmen ist**, steht als Abnahmepunkte A‑W6‑E‑2‑S2‑1 ff. im
Umsetzungsprotokoll: Die Anmutung des Abschnitts, das Zusammenspiel von Klappliste und
Katalog am echten Bestand und die Ampel an einem gepflegten Modul sind ohne Windows nicht
prüfbar.

### Stufe S3 — Rechenweg, Kennzahlen, Kosten — **UMGESETZT in `d88243e`**

| Nr. | Inhalt | Stand |
|---|---|---|
| S3.1 | `PvStrangModell` (neu, ohne Datenbank und Oberfläche — Bauart `PvErweitertesModell`): Kennlinie mit sechs Stützstellen, Gerätegruppierung, Clipping, Nachtverbrauch | **umgesetzt** (`df03234`); die NULL-Rückfallregel überspringt eine fehlende Stützstelle, ohne jede gilt die Dreipunkt-Vorgabe samt Protokollmeldung |
| S3.2 | Umbau der Anlagenschleife in `SimulationPV` auf die Strangebene, **mit** Vorrangregel und Transpositions-Zwischenspeicher | **umgesetzt** (`2734c1d`); ein DRITTER Zweig, die zwei vorhandenen Zeichen für Zeichen unverändert. Die Vorrangregel steht VOR dem Datenbankzugriff (3.5) |
| S3.3 | Kennzahlen (4.4) ins Simulationsprotokoll, auf die PV-Karte und in den Ergebnisreiter | **umgesetzt** (`2734c1d` Protokoll, `1c87d19` Karte und Reiter); zwölf Ressourcenschlüssel in beiden Sprachen |
| S3.4 | **Kosten (Q8):** `Kosten` je Gerät × Gerätezahl als eigener Posten der PV-Investition | **umgesetzt** (`4da44f9`); `TechnikPlanwertCtrl.Wechselrichteranlagen`, gezählt wird `COUNT(DISTINCT Gerätenummer)` JE ANLAGE und nur für Anlagen auf dem Weg `KATALOG` |
| S3.5 | **Aufräumen aus S2:** `PVS_HINWEIS_S3` entfernen, `ParameterVerwendung` nachziehen, den Merkposten zum Zeugen machen | **umgesetzt** (`2cc33d0`); zehn Spalten auf `Simulation`, `Kosten` auf `Wirtschaftlichkeit`, `Der_Wechselrichter_rechnet_ab_S3` |
| S3.6 | Prüfstand: Kennlinie, Clipping, Bitgleichheit ohne Zuordnung, Ein-Strang-Fall, Ost/West, Nachtverbrauch | **umgesetzt** (`c04a8cf`, `f3915c2`): **15 Fälle** in `EPOS.Kern.Tests/PvStrangRechnungTests` |
| S3.7 | Referenzbasis | **unverändert** — `2026-09-05_R2_Zeitbasis` bleibt; kein Referenzprojekt führt Stränge, und ein neues Prüfprojekt wäre ein eigener Anwenderentscheid (Kapitel 12, **W6‑O‑7**) |
| S3.8 | Hilfeseite `Berechnung/Photovoltaik.wiki` | **umgesetzt** (`1bc2e3b`); Option 2 vom Ausblick auf „umgesetzt", neuer Unterabschnitt „Der Stundenweg" mit den Schritten A bis D, dazu Kennzahlen und Kosten |
| S3.9 | Fortschreibung dieses Papiers | **dieses Dokument (Rev. 4)** |

> **Ein BEFUND aus S2, gefunden beim Bau des Prüfstands und hier behoben**
> (`0f46dd4`): `DbWerte.PV_WR_WEG_VEREINFACHT` trug den NAMEN der Konstanten als
> Wert — `"PV_WR_WEG_VEREINFACHT"`, **21 Zeichen**. Die Spalte
> `Tab_Energieanlagen.PV_Wechselrichterweg` ist `TEXT(20)`, in der
> STRICT-Datenbank also eine CHECK-Bedingung. **Jedes Speichern einer Anlage mit
> der Wahl „vereinfacht" scheiterte** an
> `CHECK constraint failed: length("PV_Wechselrichterweg") <= 20` — und weil die
> Spalte in `AnlagenSql.SQL_ANLAGE_INSERT` steht, scheiterte nicht der Schalter,
> sondern die ganze ANLAGE. Der Wert heißt jetzt `"VEREINFACHT"`, symmetrisch zu
> `"KATALOG"`. **Eine Migration braucht es nicht:** Der alte Wert kann in keiner
> Datenbank stehen, denn er ließ sich nie schreiben. Warum S2 es nicht sah: Der
> Rundweg schrieb NULL und `KATALOG`, nie `VEREINFACHT`. Er schreibt jetzt alle
> drei, und der neue Fall `Beide_Wechselrichterwege_passen_in_die_Spalte` prüft
> die LÄNGE beider Persistenzwerte.

> **Was S2 für S3 bereitgelegt hat** (06.09.2026) — die drei Einstiege, die S3.2 ruft:
>
> * **Der Schalter:** `WErzeugerModel.PV_Wechselrichterweg` gegen
>   `DbWerte.PV_WR_WEG_KATALOG`. Er steht an der Anlagenzeile, die
>   `SimulationPV.Berechnung` ohnehin liest — kein zusätzlicher Zugriff.
> * **Die Strangzeilen:** `AnlageStrangCtrl.LesenJeProjekt(idProjekt)` liefert die
>   Zeilen ALLER Anlagen eines Projekts in EINER Abfrage, sortiert nach
>   (`ID_Anlage`, `Rang`) — der Weg, den auch die Rettung im Speicherweg nimmt. Je
>   Anlage einzeln geht über `LesenJeAnlage(idAnlage)`; für einen Lauf mit fünf
>   PV-Feldern wären das fünf Rundreisen für dieselbe Information.
> * **Die Gerätewerte:** `WechselrichterCtrl.ReadAll(idProjekt)` bzw.
>   `ReadSingle(id)` — die PROJEKTKOPIEN, auf die `Z_AnlageStrang.ID_Wechselrichter`
>   zeigt.
>
> Dazu die Auslegungsgrößen: `AnlageStrangModel.Modulzahl`,
> `.GeraetenummerOderEins`, `.MpptOderEins`, `.ParallelOderEins` und
> `StrangPlausibilitaet.StrangKwp(strang, modul)` rechnen die Vorgabewerte aus 3.4
> genau einmal aus.
>
> **Und zwei Dinge, die S3 mit erledigen muss:** Der Hinweis `PVS_HINWEIS_S3` wird aus
> `PvStraengeFelder` (und aus beiden `.resx`) wieder ENTFERNT, und die Einstufungen in
> `ParameterVerwendung.Wechselrichter` wandern von `Dialog` auf `Simulation` bzw.
> `Wirtschaftlichkeit` — der Fall `Der_Wechselrichter_rechnet_in_S2_noch_nicht` fällt
> dann rot aus und ist genau dafür der Merkposten.

**Abnahme S3 — nachgewiesen am 06.09.2026:**

| Kriterium | Beleg |
|---|---|
| **(1) Referenzlauf byte-gleich** | 1030 / 1007 / 1017 gegen `Referenzlaeufe/2026-09-05_R2_Zeitbasis` — **byte-gleich** (`diff -rq` je Projekt, dazu `GESAMT: PASS`, 815 043 Werte) |
| Gegenprobe zur Vorrangregel | `Ohne_Strangzeile_rechnet_der_Schalter_nichts` — dieselbe Anlage mit Weg NULL, mit `VEREINFACHT` und mit `KATALOG` **ohne** Strangzeile rechnet **bitgleich** denselben Jahresertrag. Der Schalter allein ändert nichts; es braucht beide Bedingungen |
| **(2) Ein Strang, ohne Clipping** | `Ein_Strang_ohne_Clipping_rechnet_wie_die_Anlage_vereinfacht` — **bitgleich** zur selben Anlage auf dem vereinfachten Weg im Modell ERWEITERT. Beide laufen im SELBEN Simulationslauf und werden über `Modul_Ergebnisse` auseinandergehalten |
| … und warum das geht | `Die_Dreipunkt_Kennlinie_rechnet_zeichengleich_zum_Anlagenweg` — ein Gerät mit nur 10 / 50 / 100 % rechnet über **1 601** Auslastungen zeichengleich zu `PvErweitertesModell.EtaWechselrichter`, ohne Toleranz |
| **(3) Ost/West** | exakt auf synthetischen Stunden (`Ost_West_an_einem_Geraet_kostet_genau_das_gemeinsame_Clipping`, zwölf Stellen) und am Jahreslauf als **Zerlegung** statt als Toleranz: `(Ost + West) − gemeinsam = Gleichstromversatz − Kennliniengewinn + gemeinsames Clipping`, auf sechs Nachkommastellen. **Gemessen: 478,6 kWh = 554,5 kWh − 75,9 kWh + 0,000000 kWh** |
| Kennlinie | `Die_Kennlinie_trifft_die_sechs_Stuetzstellen_exakt` (ohne Toleranz), `…_interpoliert_dazwischen_linear`, `Eine_fehlende_Stuetzstelle_wird_uebersprungen`, `Ohne_jede_Stuetzstelle_gilt_die_Dreipunkt_Vorgabe` |
| Clipping | `Der_Clipping_Verlust_ist_die_Summe_der_Kappungen` — jede Kappung einzeln nachgerechnet, dazu Ertrag, Kennlinienverlust, Anteil, Jahresnutzungsgrad und Volllaststunden; `Ohne_AC_Nennleistung_wird_nicht_geklippt` |
| Nachtverbrauch | `Der_Nachtverbrauch_faellt_nur_unter_der_Einschaltschwelle_an` (negative Erzeugung, Nachtstunden gezählt), `Ohne_gepflegten_Nachtverbrauch_bleibt_die_Nacht_bei_null` |
| Gruppierung | `Die_Gruppierung_trennt_nach_Geraet_und_Nummer` — zwei Gerätenummern sind zwei Geräte, zwei MPP-Tracker EINE Clipping-Grenze (Q7), ein Strang ohne Gerät fällt heraus und wird gezählt |
| Kappung nicht zweimal (Anhang B) | `Nach_dem_Clipping_sieht_die_EEG_Kappung_nur_noch_P_AC_Nenn` — keine Stunde über der AC-Nennleistung; `PvErloesRechner` rechnet die 60-%-Kappung auf DIESER Stundenreihe |
| Einstufungen folgen dem Rechenweg | `Der_Wechselrichter_rechnet_ab_S3` — neun Spalten `Simulation` (namentlich), `Kosten` `Wirtschaftlichkeit`, MPPT- und Spannungsspalten NICHT gerechnet, die sieben Sandia-Spalten ohne Leser |
| Der S3-Hinweis ist fort | `Der_S3_Hinweis_ist_fort` (bunit, beide Wege) — die Gegenprobe zum S2-Fall gleichen Namens |
| SQL-Dialekt | `Werkzeuge/SqlDialektPruefer`: **0 Fundstellen** in 1 243 Texten |

**Was auf Windows noch abzunehmen ist**, steht als Abnahmepunkte A‑W6‑E‑2‑S3‑1 ff. im
Umsetzungsprotokoll: die zwei Chips der PV-Karte, die zweite Tabelle des
Ergebnisreiters, die Wechselrichterzeile in der Kostenübernahme und das
Simulationsprotokoll an einem gepflegten Katalog sind ohne Windows nicht prüfbar.

### Nachtrag zu S3 — das Modul je Strang und der Herstellerfilter — **UMGESETZT in `35a48eb`**

Drei offene Punkte, EINE Frage: *Welches Modul gilt für diesen Strang?* Der Anwender hat sie am
06.09.2026 an allen drei Stellen beantwortet, an denen sie gestellt wird — im Rechenweg, in der
Ampel und in der Maske.

| Nr. | Anwenderentscheid (Wortlaut) | Umsetzung |
|---|---|---|
| **W6‑O‑6** | „jeder Strang mit nur einem Modultyp, unterschiedliche Stränge können jeweils einen anderen Modultyp haben." | `SimulationPV` bündelt die Modulgrößen als **Modulsatz** je Modultyp (Nennleistung, Fläche, Wirkungsgrad, `γ_PMP`, `T_NOCT`, Huld-Satz) und wählt ihn über `Z_AnlageStrang.ID_PV`; gelesen wird je Modultyp EINMAL vor der Stundenschleife, und nur, wenn überhaupt eine Strangzeile ein eigenes Modul führt. Die kWp je Gerät ist die Summe der Stränge mit ihrer jeweiligen Modulleistung. Die Strangtabelle bekommt die Spalte „Modul" |
| **W6‑O‑5** | „Modul der gewählten Zeile" | Der Delegat `Pruefen` bekommt die gewählte Projektzeile mit; `StrangPlausibilitaet.Gaben` trägt zusätzlich die Strangmodule je `Tab_PV.ID`. P1 bis P4 prüfen je Strang gegen SEIN Modul — auch der Strom je MPP-Tracker ist die Summe der Stränge mit ihrem jeweiligen Kurzschlussstrom. **P8 bleibt eine Anlagenprüfung** |
| **W6‑O‑4** | „Hersteller kann vom Modul verschieden sein. Herstellerfilter etc. wie in Modulliste einfügen." | Eine Filterzeile **ÜBER** der Strangtabelle mit denselben zwei Gaben wie über der Modulliste; ein bereits gewähltes Gerät bleibt in SEINER Zeile sichtbar, auch wenn der Filter es ausschliesst |
| **W6‑O‑2** | „Empfehlung" | Kein Programm: Die MPPT-Zahl und die Scheinleistung werden **nur für die eingesetzten Geräte** von Hand nachgepflegt. Der Umgang mit der Lücke steht seit S2 (gelb, Satz „Angabe fehlt") |

**Abnahme des Nachtrags:**

| Kriterium | Beleg |
|---|---|
| **Referenzlauf byte-gleich** | 1030 / 1007 / 1017 gegen `2026-09-05_R2_Zeitbasis` — **byte-gleich**, `GESAMT: PASS` (815 043 Werte). Ohne `ID_PV` ändert sich nichts, ohne Strangzuordnung gar nichts |
| Ohne `ID_PV` derselbe Rechenweg | `Ein_Strang_ohne_ID_PV_rechnet_mit_dem_Anlagenmodul` — dieselbe Anlage einmal mit `ID_PV = NULL` und einmal mit dem AUSDRÜCKLICH eingetragenen Anlagenmodul: **bitgleich** |
| Zwei Module an einem Gerät | `Zwei_Module_an_einem_Geraet_kosten_das_gemeinsame_Clipping` — dieselbe Zerlegung wie im Ost/West-Fall, `(A + B) − gemeinsam = Gleichstromversatz − Kennliniengewinn + gemeinsames Clipping` auf sechs Nachkommastellen; der **Gleichstromversatz ist der Zeuge**: Er ist nur dann null, wenn Strang 2 wirklich mit SEINEM Modul gerechnet hat (Gegenprobe mit abgeschaltetem Strangmodul: 5,5038 statt 6,7519 kWp) |
| Ampel je Strang | `Die_Ampel_prueft_gegen_das_Modul_der_gewaehlten_Zeile`, `Ein_Strang_mit_eigenem_Modul_prueft_gegen_dieses` (U_oc 425,3 V gegen 495,5 V, Strom 9,5515 A gegen 11,225 A je Tracker, Geräte-kWp 6,7519), `Eine_unbekannte_Modul_Id_faellt_auf_das_Anlagenmodul_zurueck` |
| Filter und Modulspalte | vier bunit-Fälle zum Filter (verengt, „Alle", gewähltes Gerät bleibt, ohne Herstellerliste keine Zeile) und vier zur Modulspalte samt dem Fall, dass der Prüfstand die gewählte Projektzeile bekommt — **26 Fälle** in `PvStraengeFelderTests` |
| SQL-Dialekt | `Werkzeuge/SqlDialektPruefer`: **0 Fundstellen** in 1 251 Texten |

**Was auf Windows abzunehmen bleibt:** die Anmutung der Filterzeile über der Tabelle, die
Breite der zusätzlichen Modulspalte auf schmalem Schirm und das Zusammenspiel von Modulwahl
und abgeleiteter „Anzahl Module" an einem gepflegten Katalog.

### Reihenfolge

**S1 zuerst und allein** — **erledigt am 06.09.2026.** Sie war ohne Rechenwirkung, ohne
Referenzlauf-Risiko und liefert schon den halben Anwenderwunsch („Import liegt nicht vor, Admin
zum Anlegen/Bearbeiten liegt nicht vor").

**S2 und S3 zusammen ausliefern.** S2 allein hinterließe eine Strangtabelle, die die Oberfläche
zeigt und der Rechenkern ignoriert — eine zweite Wahrheit, also genau der Zustand, den das
PV-Ertragsmodell mit E1.1 („Eine Wahrheit") beseitigt hat. Getrennt entwickeln ja, getrennt
abnehmen ja, getrennt ausliefern nein.

> **S2 und S3 sind beide abgenommen (06.09.2026) und gehen zusammen hinaus.** Der
> Zwischenzustand ist damit vorbei: Der Satz „Die Strangrechnung folgt mit Stufe S3"
> (`PVS_HINWEIS_S3`) ist mit S3 aus Maske und beiden `.resx` entfernt, und der
> bunit-Fall `Der_S3_Hinweis_ist_fort` hält ihn draußen.

---

## 9. Größenordnungen

Erfahrungswerte, **keine Messungen an EPOS-Projekten**. Sie sagen, wofür sich der Aufwand lohnt.

| Wirkung | typ. Änderung Jahresertrag | Wirkung auf die Eigenverbrauchsquote |
|---|---|---|
| echte Kennlinie statt konstant 0,95 (EINFACH) | **+1 … +3 %** — moderne Geräte liegen gewichtet bei 96–98 % | ≈ 0 |
| echte Kennlinie statt der Vorgabe 0,94/0,975/0,97 (ERWEITERT) | ±0,5 % | ≈ 0 |
| Clipping bei DC/AC 1,1 | −0,1 … −0,5 % | +0,1 pp |
| Clipping bei DC/AC 1,25 | −0,5 … −2 % | +0,5 … +1,5 pp |
| Clipping bei DC/AC 1,4 | −2 … −5 % | +1 … +3 pp |
| Nachtverbrauch (0,5–2 W über ≈ 4 400 Nachtstunden) | **< 0,1 %** | ≈ 0 |
| Ost/West statt einer Südfläche gleicher kWp | −5 … −15 % | **+5 … +15 pp** |
| Teilfeldrechnung statt zweier getrennter Anlagen | 0 % bei getrennten Geräten; −1 … −3 % bei gemeinsamem Gerät (gemeinsames Clipping) | leicht steigend |

Die Zeile, auf die es ankommt, ist die vorletzte. **Ost/West-Anlagen werden nicht wegen des
Jahresertrags gebaut, sondern wegen der Eigenverbrauchsquote** — und genau diese Aussage kann
EPOS-Plan heute nur über den Umweg zweier getrennter Anlagen treffen, dann aber ohne gemeinsames
Clipping und ohne die Erkenntnis, dass ein Ost/West-Gerät gerade deshalb kleiner ausgelegt werden
darf.

Die erste Zeile ist die zweitwichtigste: Wer heute im Modell EINFACH mit 0,95 rechnet, rechnet den
Ertrag **systematisch zu niedrig** — moderne Geräte sind besser. Ein gepflegter Katalog korrigiert
das nach oben; ein gepflegter Katalog **mit** realistischer Überdimensionierung korrigiert es
teilweise wieder nach unten. Beide Wirkungen sind einzeln mehrere Prozent und heben sich zufällig
teilweise auf — das ist kein Argument, beide wegzulassen.

Der Vergleichsmaßstab: Das PV-Ertragsmodell hat für Paket B gemessen, dass Kennlinie und Clipping
zusammen mit dem Schwachlichtmodell **−3,94 %** ausmachten (Projekt 1026, DC/AC 1,25, Nachtrag 4,
N4.3). Die hier genannten Größenordnungen sind damit verträglich.

---

## 10. Empfehlung

> **Angenommen am 06.09.2026** („Q1 bis Q10: Empfehlung, ja alle"). Punkt 1 ist eingelöst;
> Punkt 5 hat der Anwender mit **W6‑E‑3** noch einmal geschärft — nicht die Zuordnung allein
> entscheidet, sondern ein sichtbarer Schalter (7.1).

1. ~~**S1 sofort und allein umsetzen**~~ — **erledigt am 06.09.2026** (`40fc542`): Katalog,
   Verwaltung, CEC-Import. Kein Rechenweg, kein Referenzlauf-Risiko, und der Anwenderwunsch ist
   damit zur Hälfte erfüllt.
2. **Kennlinie als Stützstellen** (5/10/20/30/50/100 %) rechnen, **Sandia-Koeffizienten
   mitschreiben** und beim Import in die Stützstellen umrechnen (3.3.3). Der Sandia-Rechenweg
   selbst bleibt liegen, bis es ein Ein-Dioden-Modell gibt.
3. **CEC-Liste als Leitquelle**, OND als zweite Quelle in derselben Maske, Handpflege über die
   Verwaltung. Die CEC-Liste kostet fast nichts, weil der ganze Abrufapparat schon steht.
4. **Strangebene statt Anlagenebene**, mit `Neigung`/`Azimut` je Strang (NULL = Anlagenwert) und
   mehreren Geräten je Anlage über `Geraetenummer` — eine Tabelle, keine zwei.
5. **Der Wechselrichter wirkt in beiden Modellen**, sobald eine Zuordnung besteht; die
   Bitgleichheit hängt an der Zuordnung, nicht am Modellschalter. Damit fällt der ausgegraute
   Knopf, der diesen Wunsch ausgelöst hat.
6. ~~**S2 und S3 zusammen ausliefern**~~ — **beide erledigt am 06.09.2026** (`c02cd99`
   und `d88243e`). Der Referenzbasiswechsel bleibt wie empfohlen AUS: Kein
   Referenzprojekt führt Stränge, die Basis `2026-09-05_R2_Zeitbasis` bleibt
   byte-gleich gültig, und ob überhaupt ein Prüfprojekt mit Strängen in die
   Testdatenbank soll, ist ein eigener Anwenderentscheid (**W6‑O‑7**).
7. **Die Katalogpflege nicht vergessen.** Die Prüfungen P1–P4 hängen an `U_Leerlauf`, `U_Mpp`,
   `I_Kurzschluss`, `alpha_SC` und `beta_OC` der **Module**. Diese Werte sind im Bestand
   nachweislich verdorben (Paket-A-Befund A1; Reparaturskript unter `sql/pv_katalog/`). **Ohne
   Katalogpflege leuchtet die Ampel grau, nicht grün** — das ist einzuplanen, sonst wirkt S2 wie
   eine Funktion, die nicht funktioniert.

---

## 11. Entscheidungsfragen — **alle entschieden am 06.09.2026**

> **Anwenderentscheid, im Wortlaut:** „W6‑E‑2‑Q1 bis Q10: Empfehlung, ja alle". Jede Empfehlung
> dieser Tabelle ist damit angenommen; die Zeilen tragen die Entscheidung und, wo Stufe S1 sie
> schon eingelöst hat, den Umsetzungsstand.

| Nr. | Frage | Empfehlung und Entscheidung |
|---|---|---|
| **W6‑E‑2‑Q1** | Kennlinienform: sechs Stützstellen (5/10/20/30/50/100 %) oder Sandia-Koeffizienten als Rechenmodell? Und braucht es die siebte Stützstelle 75 % für die CEC-Wichtung? | **Stützstellen**, Sandia nur mitschreiben — Sandia braucht `U_dc` je Stunde, die es ohne Ein-Dioden-Modell (E3, zurückgestellt) nicht gibt. **Ohne** 75 %: `Eta_Euro` ist der Ausweis, den Datenblätter nennen; die kalifornische Wichtung ist in Europa ohne Belang — **Entschieden 06.09.2026: Empfehlung angenommen.** Umgesetzt in S1: `Eta05…Eta100`, `Eta_Euro`, keine 75-%-Stützstelle. |
| **W6‑E‑2‑Q2** | Import-Quellen: CEC-Liste, PVsyst `.OND`, Handpflege — alle drei, oder gestaffelt? | **Alle drei, gestaffelt.** CEC und Handpflege in S1 (der Abrufapparat steht), OND anschließend — er ist die genauere, aber seltenere Quelle — **Entschieden 06.09.2026: Empfehlung angenommen.** CEC und Handpflege sind in S1 umgesetzt, OND folgt (S2). |
| **W6‑E‑2‑Q3** | Zuordnung auf **Strang**- oder auf **Anlagen**ebene? | **Strangebene.** Die Anlagenebene gibt es schon und kann Ost/West, Mehrgerätigkeit und Auslegungsprüfung grundsätzlich nicht — **Entschieden 06.09.2026: Empfehlung angenommen.** `Z_AnlageStrang` steht seit S2 (`ef99eed`). |
| **W6‑E‑2‑Q4** | Neigung und Azimut je Strang (Teilfelder)? | **Ja**, NULL = Anlagenwert. Das ist der Ost/West-Fall, und der Vorgabewert ändert nichts — **Entschieden 06.09.2026: Empfehlung angenommen.** Umgesetzt in S2: Spalten `Neigung`/`Azimut` in `Z_AnlageStrang`, in der Maske als geerbter Wert in KLAMMERN (Platzhalter) — eine geschriebene 0 wäre Süden. |
| **W6‑E‑2‑Q5** | Wirkt der Wechselrichter auch im Modell **EINFACH**? | **Ja**, sobald eine Zuordnung besteht. Ein Gerät ist keine Modellverfeinerung; die Bitgleichheit hängt an der Zuordnung. Damit entfällt der ausgegraute Knopf — **Entschieden 06.09.2026: Empfehlung angenommen** — und durch **W6‑E‑3** ergänzt: Ob der Wechselrichter rechnet, sagt seit dem der sichtbare Schalter, nicht mehr allein die Zuordnung (7.1). |
| **W6‑E‑2‑Q6** | Mehrere Wechselrichter je Anlage? | **Ja**, über `Geraetenummer` in **einer** Tabelle. Clipping je Gerät, Gerätezahl für die Kosten aus `COUNT(DISTINCT …)` — **Entschieden 06.09.2026: Empfehlung angenommen.** Umgesetzt in S2: `Geraetenummer` in `Z_AnlageStrang`; `StrangPlausibilitaet` gruppiert über (Wechselrichter, Gerätenummer) und liefert je Gerät einen Befund. |
| **W6‑E‑2‑Q7** | MPPT-Granularität: nur für die Auslegungsprüfung, oder auch mit eigener Eingangsleistungsgrenze im Rechenweg? | **Zunächst nur Prüfung** (P4/P5). Eine MPPT-Leistungsgrenze ist bei üblicher Auslegung wirkungslos und kostet eine Klemmstelle mehr in der Stundenschleife; nachrüstbar — **Entschieden 06.09.2026: Empfehlung angenommen.** S1 legt `Anzahl_Mppt` und `Straenge_Je_Mppt` an; **seit S2 lesen P4/P5 sie** — fehlt die MPPT-Zahl, wird auf EINEM Tracker gerechnet und im Satz gesagt. **S3 hat die Empfehlung eingelöst**: Der Rechenweg summiert über die Tracker eines Geräts und klemmt NUR am Gerät; beide Spalten bleiben deshalb in `ParameterVerwendung` auf `Dialog`. |
| **W6‑E‑2‑Q8** | Wechselrichterkosten in der Wirtschaftlichkeit? | **Ja**, `Kosten` im Katalog, Summe über die Geräte als eigener Posten. Heute trägt die PV nur den Modulstückpreis (`TechnikPlanwertCtrl.cs:348-349`) — der Wechselrichter fehlt in der Investition, und das ist bei 10–20 % der Anlagenkosten spürbar — **Entschieden 06.09.2026: Empfehlung angenommen.** Die Spalte `Kosten` steht seit S1 im Katalog; **seit S3 rechnet sie** (`TechnikPlanwertCtrl.Wechselrichteranlagen`) — je Wechselrichtertyp eine eigene Kostenzeile mit `Kosten × COUNT(DISTINCT Gerätenummer)` je Anlage, und NUR für Anlagen auf dem Weg `KATALOG`. |
| **W6‑E‑2‑Q9** | „Anzahl Module": aus den Strängen **abgeleitet** (Feld wird nur-lesend) oder nur **geprüft** (P8 als Warnung)? | **Abgeleitet**, sobald ein Strang besteht — „eine Wahrheit" (E1.1). Der Anlagenwert wird mitgeschrieben, damit kWp, Stückpreis und Wirtschaftlichkeit unverändert weiterlesen — **Entschieden 06.09.2026: Empfehlung angenommen. Umgesetzt in S2.4** (`c02cd99`): Ohne Strang bleibt es ein Eingabefeld, mit Strang steht dort die Summe mit dem Zusatz „aus der Strangtabelle"; P8 bleibt als Wache für Bestände, die von Hand auseinandergelaufen sind. |
| **W6‑E‑2‑Q10** | Importmaske: zweite **Ausprägung** des vorhandenen `PvModulImportDialog` oder eigener Dialog? | **Zweite Ausprägung.** Netzabruf, Zwischenspeicher, virtualisiertes Raster, Fortschritt, Dubletten- und Konfliktweg sind identisch; verschieden sind nur Spalten und Zieltabelle — genau das, was ein Profil trägt — **Entschieden 06.09.2026: Empfehlung angenommen** — in S1 **teilweise** eingelöst: Abrufapparat, Vorprüfung, Konfliktweg und alle Bausteine sind geteilt, die `.razor`-Datei nicht (Begründung in 5.5). Offener Punkt **W6‑O‑1**. |
| **W6‑E‑3** | Wo liegt der SICHTBARE Schalter „vereinfacht" / „mit Wechselrichter" im Datenmodell — eigene Spalte an der Anlagenzeile oder abgeleitet aus der Strangtabelle? | **Eigene Spalte** `Tab_Energieanlagen.PV_Wechselrichterweg` (NULL = vereinfacht), angelegt in Schritt 66 zusammen mit `Z_AnlageStrang`. Begründung in 7.1: Eine Option, die man nur durch Löschen der Strangzeilen abwählen kann, ist keine Option. **Neuer Anwenderwunsch vom 06.09.2026; UMGESETZT in S2.4** (`c02cd99`) — samt der weichen Sperre aus W16b‑E‑6 für „mit Wechselrichter" ohne Strang. |

---

## 12. Offene Punkte

**Stand 06.09.2026:** Von den sieben Punkten sind vier geschlossen — **W6‑O‑2** durch
Anwenderentscheid („Empfehlung": nur die eingesetzten Geräte von Hand nachpflegen, keine
Programmarbeit), **W6‑O‑4**, **W6‑O‑5** und **W6‑O‑6** durch Umsetzung. Offen bleiben der
Importwirt (W6‑O‑1), der leere Auslieferungskatalog (W6‑O‑3) und die Frage nach einem
Referenzprojekt mit Strängen (W6‑O‑7).

| Nr. | Punkt | Stand |
|---|---|---|
| **W6‑O‑1** | **Ein Importwirt statt zwei.** `PvModulImportDialog` (771 Z.) und `WechselrichterImportDialog` (655 Z.) teilten Abrufapparat, Vorprüfung, Konfliktweg und sämtliche Bausteine, aber nicht die `.razor`-Datei (5.5). Die Zusammenlegung verlangte eine neutrale Zeilen- und Detailform (Spalten und Felder als DATEN, wie `ModulFeldwert` im Modulkatalog) und damit den Umbau einer getesteten Maske samt 594 Zeilen bunit-Fällen. | **UMGESETZT in `9ef8ca5`** — Anwenderentscheid vom 06.09.2026: „der OND-Import soll umgesetzt werden. baue daher den Modulimport schon jetzt um (Modulimport und Wechselrichter Import zwei Masken)". Es ist genau der Zeitpunkt, den dieser Punkt selbst benannt hatte. `ModulImportDialog` (669 Z. statt 771 + 655) ist der eine Wirt, `ModulImportProfil` im Kern trägt die Daten, `ImportZeile` die neutrale Zeilenform; die zwei Hüllen sind eine, die beiden alten `.razor` samt `Daten.cs` sind gelöscht, die 594 Zeilen bunit-Fälle sind mitgewandert und grün. Details in 5.5 |
| **W6‑O‑2** | **MPPT-Zahl und Scheinleistung fehlen im CEC-Bestand.** Die Liste führt weder `Anzahl_Mppt` noch `S_AC_Max`; beide bleiben nach dem Import NULL, und die Prüfungen P4/P5 rechnen dann auf EINEM MPPT. Ob der Auslieferungskatalog von Hand nachgepflegt wird (und für welche Geräte), ist eine Anwenderfrage. | **entschieden am 06.09.2026 — „Empfehlung"**: Nachgepflegt werden **nur die eingesetzten Geräte**, von Hand im Katalogeditor (MPPT-Zahl, Scheinleistung). **Kein Programm nötig** — der Umgang mit der Lücke steht seit S2: Die Prüfung rechnet auf EINEM Tracker, die Ampel wird GELB, und der Satz sagt „Angabe fehlt: Zahl der MPP-Tracker — gerechnet wird auf einem" (Fall `Ohne_MPPT_Zahl_rechnet_die_Pruefung_auf_einem_Tracker`) |
| **W6‑O‑3** | **Der Auslieferungsbestand ist leer.** Schritt 65 legt die Tabellen ohne DML an (das ist die Ergebnisneutralität). Ob EPOS-Plan künftig mit einem vorbefüllten Wechselrichterkatalog ausgeliefert wird — und wenn ja, mit welchen Geräten und mit `ReadOnly = 1` — ist ein eigener Entscheid. | **ENTSCHIEDEN und UMGESETZT in `9ef8ca5`** — Anwenderentscheid vom 06.09.2026: „hole die Wechselrichterdaten für den Import", bestätigt als „Liste als Datei und dann über Import (aus Admin-Menü)". **Damit ist die Frage anders beantwortet als gestellt:** Der Katalog bleibt bei der Auslieferung LEER (Schritt 65 unverändert, kein DML, keine `ReadOnly`-Sätze) — ausgeliefert wird die LISTE als Datei, `VDI-3805-Daten/PV/CEC Inverters.csv` neben `CEC Modules.csv` (2 346 Zeilen, 2 343 Geräte, 152 Hersteller; Quelle, Abrufdatum und Lizenz in `LIESMICH_CEC_Inverters.md`). Eingelesen wird sie über **Administration → Datenimport → „Wechselrichter (CEC, OND)…" → „CEC-Datei laden"**; der Dateiwähler macht im Herstellerdatenordner auf. Nachweis: `EPOS.Kern.Tests/CecWechselrichterAuslieferungTests` liest die volle Datei — alle 2 343 Geräte bekommen sechs Stützstellen, Plausibilität **2 040 grün / 303 gelb / 0 rot** |
| **W6‑O‑4** | **Der Herstellerfilter über der Wechselrichter-Klappliste der Strangtabelle fehlte.** Kapitel 7 sieht ihn vor („mit demselben Herstellerfilter wie die Modulliste"); S2 hat ihn nicht gebaut. | **umgesetzt in `35a48eb`** — Anwenderentscheid vom 06.09.2026, wörtlich: „Hersteller kann vom Modul verschieden sein. Herstellerfilter etc. wie in Modulliste einfügen." Gebaut als eigene Zeile **ÜBER** der Tabelle (`PvStraengeFelder.Hersteller`/`GeraeteFiltern` → `WechselrichterStammCtrl.Hersteller`/`Filtern`), unabhängig vom Modulfilter; ein bereits gewähltes Gerät bleibt trotz Filter in seiner Zeile sichtbar. Vier bunit-Fälle |
| **W6‑O‑6** | **Der abweichende Modultyp je Strang rechnete noch nicht.** `Z_AnlageStrang.ID_PV` steht seit S2 in der Tabelle und reist im Controller hin und zurück; der Rechenweg der Stufe S3 rechnete jeden Strang jedoch mit dem Modul der ANLAGE. | **umgesetzt in `35a48eb`** — Anwenderentscheid vom 06.09.2026, wörtlich: „jeder Strang mit nur einem Modultyp, unterschiedliche Stränge können jeweils einen anderen Modultyp haben." `SimulationPV` rechnet jeden Strang mit seinem Modulsatz (Nennleistung, Fläche, Wirkungsgrad, γ_PMP, NOCT, Huld-Satz); gelesen wird je Modultyp EINMAL vor der Stundenschleife, und ohne `ID_PV` ändert sich nichts. Die Strangtabelle führt dafür eine Spalte „Modul". Nachweise: `Ein_Strang_ohne_ID_PV_rechnet_mit_dem_Anlagenmodul` (bitgleich) und `Zwei_Module_an_einem_Geraet_kosten_das_gemeinsame_Clipping` (Zerlegung wie S3 (3)), Referenzlauf 1030/1007/1017 byte-gleich |
| **W6‑O‑8** | **303 von 2 343 CEC-Geräten sind GELB — und alle aus demselben Grund.** Die Prüfung `WechselrichterPlausibilitaet` meldet „Die Kennlinie fällt im Teillastast" (η30 > η50) für 13 % des Auslieferungsbestands. Gemessen am 06.09.2026 über die volle Liste: Es ist kein Datenfehler, sondern die Modellparabel aus 3.3.3 — bei Geräten mit hohem Wirkungsgrad liegt ihr Scheitel zwischen 30 und 50 %, und genau das ist bei einem guten Stringwechselrichter auch physikalisch richtig. Die Warnung fragt jedes dieser Geräte beim Übernehmen zurück; das ist lästig und sagt nichts. **Empfehlung: Die Regel auf einen Schwellwert heben** (etwa: melden erst, wenn η30 − η50 > 0,01) oder sie auf handgepflegte Sätze beschränken. | **offen** — neu mit W6‑O‑3, Anwenderfrage. Bis dahin: gelb heißt hier „nachsehen", nicht „falsch" |
| **W6‑O‑7** | **Referenzbasis mit Strängen?** Die Basis `2026-09-05_R2_Zeitbasis` bleibt gültig und byte-gleich: Kein Referenzprojekt führt eine Strangzeile, und genau das ist der Nachweis der Vorrangregel. Ein PRÜFPROJEKT mit Strängen in `Kenndaten_Test.sqlite` würde den Strangweg dagegen in jedem Referenzlauf mitrechnen — und wäre damit die Wache gegen eine spätere stille Änderung am Strangweg, wie sie die elf Projekte heute für den Anlagenweg sind. Kosten: eine neu einzufrierende Basis. **Empfehlung: ja, aber als eigener Schritt** — ein zwölftes Projekt mit einer Ost/West-Anlage an einem knapp ausgelegten Gerät (DC/AC ≈ 1,3), damit Clipping, Kennlinie und Nachtverbrauch alle drei wirken. Solange er aussteht, hält der Prüfstand `PvStrangRechnungTests` den Strangweg. | **offen** — Anwenderentscheid |
| **W6‑O‑5** | **Das Modul der Ampel war das der ERSTEN Projektzeile.** `StrangPlausibilitaet` prüfte gegen EIN Modul; die Hülle nahm dafür das erste, das der Katalog kennt. Führt ein Projekt mehrere PV-Zeilen mit VERSCHIEDENEN Modulen, prüfte die Ampel gegen das falsche. | **umgesetzt in `35a48eb`** — Anwenderentscheid vom 06.09.2026, wörtlich: „Modul der gewählten Zeile." Der Delegat `Pruefen` bekommt die gewählte Projektzeile mit, `PhotovoltaikHuelle.ModulDer(zeile)` liest deren Modul, und `StrangPlausibilitaet.Gaben` trägt zusätzlich die Strangmodule je `Tab_PV.ID` (W6‑O‑6): Jeder Strang prüft gegen SEIN Modul — Spannung, Strom und Nennleistung —, P8 bleibt eine Anlagenprüfung. Drei Kernfälle, ein bunit-Fall |

---

## Anhang A — Prüfbeispiel für die Ampel (die Zahlen des Mockups)

Modul **Ablytek 6MN6A275** (aus dem Bildschirmfoto): P_STC = 275,19 W. Für das Beispiel
angenommene Katalogwerte eines typischen 60‑zelligen Moduls — im Bestand sind sie zu pflegen
(Abschnitt 10, Punkt 7):

```
U_Leerlauf = 38,4 V     U_Mpp = 31,4 V     I_Kurzschluss = 9,34 A
beta_OC    = −0,118 V/K  alpha_SC = +0,0047 A/K
```

Wechselrichter **Muster 2500TL**: P_AC_Nenn = 2,50 kW, 1 MPPT, U_Mpp 80…500 V, U_Dc_Max 600 V,
I_Dc_Max 12,0 A, η bei 5/10/20/30/50/100 % = 0,900 / 0,940 / 0,962 / 0,970 / 0,975 / 0,970,
η_euro = 0,968.

Strang: **10 Module in Reihe, 1 Strang parallel** — die 10 Module des Bildschirmfotos.

| Prüfung | Rechnung | Ergebnis |
|---|---|---|
| **P1** U_oc(−10 °C) | `10 · [38,4 + (−0,118)·(−35)] = 10 · 42,53 = 425,3 V ≤ 600 V` | **grün** |
| **P2** U_mpp(70 °C) | `10 · [31,4 + (−0,118)·45] = 10 · 26,09 = 260,9 V ≥ 80 V` | **grün** |
| **P3** U_mpp(−10 °C) | `10 · [31,4 + (−0,118)·(−35)] = 10 · 35,53 = 355,3 V ≤ 500 V` | **grün** |
| **P4** I_sc(70 °C) | `1 · [9,34 + 0,0047·45] = 9,55 A ≤ 12,0 A` | **grün** |
| **P6** DC/AC | `10 · 275,19 W = 2,752 kWp / 2,50 kW = 1,10` | **grün** (1,0…1,5) |
| **P8** Modulsumme | `10 · 1 = 10 = „Anzahl Module"` | **grün** |

**Gegenprobe:** 14 Module in Reihe ergäben `14 · 42,53 = 595,4 V` — noch unter 600 V, aber
`14 · 275,19 = 3,853 kWp` gegen 2,50 kW ist DC/AC 1,54 und damit **gelb** (P6); 15 Module ergäben
`637,9 V > 600 V` und damit **rot** (P1). Zwei Stränge parallel ergäben `19,1 A > 12,0 A` und
damit ebenfalls **rot** (P4). Diese drei Fälle gehören in den Prüfstand von S2.

> **Nachgerechnet (S2, 06.09.2026).** Alle sechs Zeilen und alle drei Gegenproben stehen als
> Fälle in `EPOS.Kern.Tests/StrangPlausibilitaetTests.cs` und kommen Zahl für Zahl heraus —
> gegen die WERTE des Befunds, nicht gegen den Satz: 425,3 V, 260,9 V, 355,3 V, 9,5515 A,
> 2,7519 kWp und DC/AC 1,10076; die Gegenproben bei 595,42 V (P1 hält) mit DC/AC 1,541064
> (P6 gelb), 637,95 V (P1 rot) und 19,103 A (P4 rot). Die Tabelle oben rundet auf zwei
> Stellen, die Fälle prüfen auf sechs.

---

## Anhang B — Was dieses Papier nicht behandelt

* **Ein-Dioden-Modell und MPP-Suche je Stunde** (Stufe E3 des PV-Ertragsmodells). Ohne sie bleibt
  die Kennlinie spannungsunabhängig; das ist die bewusst in Kauf genommene Ungenauigkeit.
* **Verschattung und Mismatch je Strang.** Beides wäre auf Strangebene erst richtig darstellbar,
  ist aber ein eigener Gegenstand — heute pauschal in `PV_Systemverluste` enthalten.
* **Blindleistung, cos φ, Netzanschlussbewertung.** `S_AC_Max` liegt als Ausweis im Katalog; eine
  Blindleistungsrechnung findet nicht statt.
* **Leistungsbegrenzung nach § 9 EEG (60/70 %)** — sie liegt im Wirtschaftlichkeitsmodul
  (`Konzept_Photovoltaik_Wirtschaftlichkeit_EPOS-Plan.md`, P1/V3) und wirkt auf die Einspeisung,
  nicht auf den Wechselrichter. Sie wechselwirkt allerdings mit dem Clipping — beide kappen
  dieselbe Mittagsspitze. **Mit S3 ist das im Prüfstand** (`f3915c2`):
  `PvErloesRechner` rechnet die Kappung auf der STUNDENREIHE DER EINSPEISUNG, und
  die entsteht aus `P_AC` des Strangwegs. Der Fall
  `Nach_dem_Clipping_sieht_die_EEG_Kappung_nur_noch_P_AC_Nenn` hält die Bedingung
  fest, unter der beide nacheinander statt doppelt greifen: Nach dem Clipping liegt
  keine Stunde über der AC-Nennleistung — was das Gerät bereits gekappt hat, kann
  der EEG-Deckel nicht noch einmal kappen.
* **Batteriewechselrichter und Hybridgeräte.** Der Stromspeicher hat eigene Kenngrößen
  (`Tab_Stromspeicher_STAMM`); eine gemeinsame AC-Grenze von PV und Speicher ist hier nicht
  vorgesehen.

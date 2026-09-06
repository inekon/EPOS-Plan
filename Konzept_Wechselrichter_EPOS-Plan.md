# Konzept: Wechselrichter EPOS-Plan — Katalog, Strangzuordnung und Rechenweg

**Rev. 1 — 06.09.2026 — Befund und Vorschlag, zur Entscheidung durch den Anwender**

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
zurückstellen"). Der Anwenderwunsch W6‑E‑2 holt beides nach vorn. **Es ist ein Konzept- und
Mockup-Papier; nichts davon ist umgesetzt.**

Mockup: `Mockups/Wechselrichter_Mockup_2026-09-06.html` (vier Ansichten M1–M4).

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
   (Erweiterung einer Anlage mit einem anderen Modultyp) und wird in Stufe S2 **nicht** in der
   Oberfläche gezeigt — sie steht bereit, sobald sie gebraucht wird. Wer sie jetzt weglässt,
   braucht später einen Migrationsschritt für ein Feld, das ohnehin absehbar ist.

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

`PV_Systemverluste` bleibt ein **Anlagenwert** — Verschmutzung, Leitungsverluste und Mismatch
gelten für das Feld, nicht für den Strang. `PV_WrWirkungsgrad` bleibt der Faktor des einfachen
Modells ohne Zuordnung.

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

Zwei Punkte zur Ehrlichkeit des Imports:

* **Die CEC-Liste führt keine MPPT-Zahl.** Sie bleibt NULL und ist von Hand zu pflegen; die
  Prüfungen P4/P5 rechnen dann auf **einem** MPPT — dem konservativen Fall — und melden es.
* **`Paco` ist Wirkleistung, nicht Scheinleistung.** `S_AC_Max` bleibt NULL und fällt in den
  Prüfungen auf `P_AC_Nenn` zurück.

Die Zeilenzahl der Liste ist bei der Umsetzung zu messen. Die Modulliste hat 20 746 Zeilen und
wird in einem virtualisierten Raster gezeigt (`PvModulImportDialog`, iU9‑W13.0l); die
Wechselrichterliste liegt in derselben Größenordnung und braucht dieselbe Behandlung.

### 5.2 PVsyst `.OND`

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

### 5.5 Wo der Import in der Oberfläche sitzt

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
`MenuItem_PV_Import_CEC` (`EPOS.UI/Bausteine/Menuetabelle.cs:113`).

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

Menüpunkt: **Administration → Energiesysteme → Photovoltaik → „Wechselrichter bearbeiten…"**
(`Menuetabelle.cs:93-99` — der Kopf „Photovoltaik" führt heute genau einen Unterpunkt
„Bearbeiten"; der zweite gehört dazu). Seitenschlüssel `WechselrichterAdmin`.

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
┌──────┬──────────────────┬──────┬──────┬────────┬───────┬────────┬─────────┐
│ Rang │ Wechselrichter   │ Ger. │ MPPT │ Reihe  │ Par.  │ Neig.  │ Azimut  │
├──────┼──────────────────┼──────┼──────┼────────┼───────┼────────┼─────────┤
│  1   │ Muster 2500TL  ▾ │  1   │  1   │  10    │   1   │  (30)  │   (0)   │
└──────┴──────────────────┴──────┴──────┴────────┴───────┴────────┴─────────┘
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
  Übernehmen kopiert (`CopyFromStamm`) — genau wie ein Modul.
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

---

## 8. Vorschlag in drei Stufen

### Stufe S1 — Katalog, Verwaltung, Import (**ohne jede Rechenwirkung**)

| Nr. | Inhalt | Umfang |
|---|---|---|
| S1.1 | Migrationsschritt **65**: `Tab_Wechselrichter_STAMM` + `Tab_Wechselrichter`; `SchemaKatalog`-Einträge, Typkatalog, `sql/schema/001_grundschema.sql` | 1 Schritt, ~2 Dateien |
| S1.2 | `WechselrichterModel`, `WechselrichterStammCtrl`, `WechselrichterCtrl` (mit `CopyFromStamm`) nach dem Muster `PhotovoltaikStammCtrl`/`PhotovoltaikCtrl` | 3 Dateien |
| S1.3 | `KatalogRegistry`-Definition „WECHSELRICHTER" (5.4) | 1 Datei |
| S1.4 | Dritte Ausprägung in `ModulKatalogProfil` + Menüpunkt + Seitenschlüssel + Navigation | 4 Dateien |
| S1.5 | CEC-Wechselrichterimport: `CecWechselrichterDienst` nach dem Muster `CECDataService`, Sandia→Stützstellen-Umrechnung, zweite Ausprägung des Importdialogs | 4–5 Dateien |
| S1.6 | `WechselrichterPlausibilitaet` (Katalogsatz) + Proben | 2 Dateien |
| S1.7 | Ressourcenschlüssel de + en (≈ 60), Hilfetexte | 2 Dateien |

**Abnahme S1:** Katalog anlegen, CEC-Liste importieren, ein Gerät von Hand anlegen, kopieren,
löschen; die Dublettenprüfung meldet den Zweitimport; Migration idempotent (Zweitlauf ohne DDL);
**Referenzlauf byte-gleich** — S1 fasst den Rechenweg nicht an. Das ist trivialerweise erfüllt und
gerade deshalb wertvoll: **S1 ist ohne Risiko lieferbar.**

### Stufe S2 — Strangzuordnung, Plausibilität, Oberfläche

| Nr. | Inhalt | Umfang |
|---|---|---|
| S2.1 | Migrationsschritt **66**: `Z_AnlageStrang` | 1 Schritt |
| S2.2 | `AnlageStrangModel`, `AnlageStrangCtrl` (Lesen/Schreiben je Anlage), Anbindung an `AnlagenSql`/`WizardCtrl` — **Achtung:** `SQL_ANLAGE_INSERT` verliert beim Löschen und Neuanlegen bekanntlich Spalten (Nachtrag 3, N3.3); die Strangzeilen dürfen nicht in dieselbe Falle laufen | 3 Dateien |
| S2.3 | `StrangPlausibilitaet` mit P1–P8 (4.2) + Proben mit gerechneten Grenzfällen | 2 Dateien |
| S2.4 | Abschnitt „Wechselrichter und Stränge" im PV-Dialog (Abschnitt 7); die Sperrregel des Knopfes entfällt | 3 Dateien |
| S2.5 | Projekttransfer (`.wpx`): die zwei neuen Tabellen in Export und Import, Zielversion 66 | 1–2 Dateien |
| S2.6 | Ressourcenschlüssel de + en (≈ 40) | 2 Dateien |

**Abnahme S2:** Ein Strang lässt sich anlegen, ändern, löschen; die Ampel zeigt an einem
gerechneten Beispiel grün, gelb und rot; Modulsumme und „Anzahl Module" stimmen überein;
Projektexport und -import tragen die Stränge; **Referenzlauf byte-gleich** (S2 rechnet noch
nicht).

### Stufe S3 — Rechenweg, Kennzahlen, neue Referenzbasis

| Nr. | Inhalt | Umfang |
|---|---|---|
| S3.1 | `PvStrangModell` (neu, ohne Datenbank und Oberfläche — Bauart `PvErweitertesModell`): Kennlinie mit sechs Stützstellen, Gerätegruppierung, Clipping, Nachtverbrauch | 1 Datei |
| S3.2 | Umbau der Anlagenschleife in `SimulationPV` auf die Strangebene, **mit** Vorrangregel und Transpositions-Zwischenspeicher | 1 Datei, der heikelste Eingriff |
| S3.3 | Kennzahlen (4.4) ins Simulationsprotokoll und auf die PV-Karte | 2–3 Dateien |
| S3.4 | Prüfstand: Kennlinie an den Stützstellen exakt, Clipping-Verlust als Summe nachgerechnet, Bitgleichheit ohne Zuordnung, Ost/West-Fall gegen zwei getrennte Anlagen | 1 Datei |
| S3.5 | Neue Referenzbasis, sobald ein Referenzprojekt produktiv Stränge führt | Referenzlauf |

**Abnahme S3:** (1) **Byte-gleich** gegen `2026-09-05_R2_Zeitbasis` für alle elf Projekte — kein
Projekt hat eine Zuordnung. (2) Ein Prüfprojekt mit einem Strang und ohne Clipping rechnet
gleich wie dieselbe Anlage mit `PV_WrNennleistungKw`/`PV_WrEta*`. (3) Ein Ost/West-Prüfprojekt mit
zwei Strängen an **einem** Gerät rechnet dieselbe Jahressumme wie zwei getrennte Anlagen mit je
einem Gerät **minus** dem gemeinsamen Clipping — diese Differenz ist die Aussage, für die die
Stufe gebaut wird.

### Reihenfolge

**S1 zuerst und allein.** Sie ist ohne Rechenwirkung, ohne Referenzlauf-Risiko und liefert schon
den halben Anwenderwunsch („Import liegt nicht vor, Admin zum Anlegen/Bearbeiten liegt nicht
vor").

**S2 und S3 zusammen ausliefern.** S2 allein hinterließe eine Strangtabelle, die die Oberfläche
zeigt und der Rechenkern ignoriert — eine zweite Wahrheit, also genau der Zustand, den das
PV-Ertragsmodell mit E1.1 („Eine Wahrheit") beseitigt hat. Getrennt entwickeln ja, getrennt
abnehmen ja, getrennt ausliefern nein.

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

1. **S1 sofort und allein umsetzen** — Katalog, Verwaltung, CEC-Import. Kein Rechenweg, kein
   Referenzlauf-Risiko, und der Anwenderwunsch ist damit zur Hälfte erfüllt.
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
6. **S2 und S3 zusammen ausliefern**, mit Referenzbasiswechsel erst, wenn ein Referenzprojekt
   produktiv Stränge führt.
7. **Die Katalogpflege nicht vergessen.** Die Prüfungen P1–P4 hängen an `U_Leerlauf`, `U_Mpp`,
   `I_Kurzschluss`, `alpha_SC` und `beta_OC` der **Module**. Diese Werte sind im Bestand
   nachweislich verdorben (Paket-A-Befund A1; Reparaturskript unter `sql/pv_katalog/`). **Ohne
   Katalogpflege leuchtet die Ampel grau, nicht grün** — das ist einzuplanen, sonst wirkt S2 wie
   eine Funktion, die nicht funktioniert.

---

## 11. Entscheidungsfragen

| Nr. | Frage | Empfehlung |
|---|---|---|
| **W6‑E‑2‑Q1** | Kennlinienform: sechs Stützstellen (5/10/20/30/50/100 %) oder Sandia-Koeffizienten als Rechenmodell? Und braucht es die siebte Stützstelle 75 % für die CEC-Wichtung? | **Stützstellen**, Sandia nur mitschreiben — Sandia braucht `U_dc` je Stunde, die es ohne Ein-Dioden-Modell (E3, zurückgestellt) nicht gibt. **Ohne** 75 %: `Eta_Euro` ist der Ausweis, den Datenblätter nennen; die kalifornische Wichtung ist in Europa ohne Belang |
| **W6‑E‑2‑Q2** | Import-Quellen: CEC-Liste, PVsyst `.OND`, Handpflege — alle drei, oder gestaffelt? | **Alle drei, gestaffelt.** CEC und Handpflege in S1 (der Abrufapparat steht), OND anschließend — er ist die genauere, aber seltenere Quelle |
| **W6‑E‑2‑Q3** | Zuordnung auf **Strang**- oder auf **Anlagen**ebene? | **Strangebene.** Die Anlagenebene gibt es schon und kann Ost/West, Mehrgerätigkeit und Auslegungsprüfung grundsätzlich nicht |
| **W6‑E‑2‑Q4** | Neigung und Azimut je Strang (Teilfelder)? | **Ja**, NULL = Anlagenwert. Das ist der Ost/West-Fall, und der Vorgabewert ändert nichts |
| **W6‑E‑2‑Q5** | Wirkt der Wechselrichter auch im Modell **EINFACH**? | **Ja**, sobald eine Zuordnung besteht. Ein Gerät ist keine Modellverfeinerung; die Bitgleichheit hängt an der Zuordnung. Damit entfällt der ausgegraute Knopf |
| **W6‑E‑2‑Q6** | Mehrere Wechselrichter je Anlage? | **Ja**, über `Geraetenummer` in **einer** Tabelle. Clipping je Gerät, Gerätezahl für die Kosten aus `COUNT(DISTINCT …)` |
| **W6‑E‑2‑Q7** | MPPT-Granularität: nur für die Auslegungsprüfung, oder auch mit eigener Eingangsleistungsgrenze im Rechenweg? | **Zunächst nur Prüfung** (P4/P5). Eine MPPT-Leistungsgrenze ist bei üblicher Auslegung wirkungslos und kostet eine Klemmstelle mehr in der Stundenschleife; nachrüstbar |
| **W6‑E‑2‑Q8** | Wechselrichterkosten in der Wirtschaftlichkeit? | **Ja**, `Kosten` im Katalog, Summe über die Geräte als eigener Posten. Heute trägt die PV nur den Modulstückpreis (`TechnikPlanwertCtrl.cs:348-349`) — der Wechselrichter fehlt in der Investition, und das ist bei 10–20 % der Anlagenkosten spürbar |
| **W6‑E‑2‑Q9** | „Anzahl Module": aus den Strängen **abgeleitet** (Feld wird nur-lesend) oder nur **geprüft** (P8 als Warnung)? | **Abgeleitet**, sobald ein Strang besteht — „eine Wahrheit" (E1.1). Der Anlagenwert wird mitgeschrieben, damit kWp, Stückpreis und Wirtschaftlichkeit unverändert weiterlesen |
| **W6‑E‑2‑Q10** | Importmaske: zweite **Ausprägung** des vorhandenen `PvModulImportDialog` oder eigener Dialog? | **Zweite Ausprägung.** Netzabruf, Zwischenspeicher, virtualisiertes Raster, Fortschritt, Dubletten- und Konfliktweg sind identisch; verschieden sind nur Spalten und Zieltabelle — genau das, was ein Profil trägt |

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
  dieselbe Mittagsspitze. **Das gehört bei S3 in den Prüfstand**, damit die Kappung nicht zweimal
  gerechnet wird.
* **Batteriewechselrichter und Hybridgeräte.** Der Stromspeicher hat eigene Kenngrößen
  (`Tab_Stromspeicher_STAMM`); eine gemeinsame AC-Grenze von PV und Speicher ist hier nicht
  vorgesehen.

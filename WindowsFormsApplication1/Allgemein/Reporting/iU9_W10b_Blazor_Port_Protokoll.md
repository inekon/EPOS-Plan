# iU9 Welle 10b — Simulationskonfiguration II: die Seite mit Karten und Schema — Portprotokoll

> Umsetzung 03./04.09.2026 im Arbeitsbaum `agent-ad9f1ddc5853ec526`, Basis
> `427fd59` (nach dem Merge der Welle 10a). Vorbild in Aufbau und Tiefe: die
> Protokolle der Wellen 9 und 10a im selben Ordner. Regeln: Arbeitsanweisung
> `iU9_W10b_Arbeitsanweisung.md`, Vermessung `iU9_W10_Vermessung.md` §8, §8‑Z
> und die Befunde W10‑B1, B33–B40, dazu `EPOS.UI/CLAUDE.md`,
> `EPOS.Kern/CLAUDE.md`, `WindowsFormsApplication1/CLAUDE.md`.

---

## 1. Auftrag und Ergebnis

**Eine Maske mit vier Teildateien, drei Steuerelement-Klassen und einem
Zeichenmodell** — `Form_Simulation_Config` (4 558 Zeilen `.cs`, 337 Zeilen
Designer, 297 + 25 Ressourceneinträge) — ist **eine Razor-Seite**, **drei neue
Bausteine** und **ein Kern-Umzug**. Die WinForms-Fassung ist im selben Schritt
gelöscht (Regel M1).

| Was | ersetzt | Zeilen |
|---|---|---:|
| `EPOS.UI/Seiten/Simulation/SimulationKonfigSeite.razor` | `Form_Simulation_Config.{cs,Karten.cs,Schema.cs,Uebersicht.cs,Designer.cs,resx,en-US.resx}` | 4 558 + 337 |
| `EPOS.UI/Bausteine/Schema.razor` | `Views/Simulation/SchemaAnsicht.cs` | 789 |
| `EPOS.UI/Bausteine/ErzeugerKachel.razor` | `Views/Simulation/ErzeugerKarte.cs` | 781 |
| `EPOS.UI/Bausteine/SpeicherKachel.razor` | `Views/Simulation/SpeicherKarte.cs` | 551 |
| `EPOS.UI/Dialoge/Simulation/WertAbfrage` (aus W10a) | `Views/Simulation/Eingabefrage.cs` (letzter Nutzer) | 49 |

**Neu im Kern:** `SchemaModell.cs` (verschoben, unverändert), `SchemaLayout.cs`
(neu, 520 Z.), `Kaskade.cs` (neu), `Model/AnlagenInfo.cs` (neu),
`WaermequelleClass.Quellenwahl.cs` (neu: `QuelleErgebnis` +
`QuelleSchreiben`), `WErzeugerCtrl.Konfigseite.cs` (drei Abfragen) und je eine
Methode in `ErgebnisCtrl`, `KlimaregionCtrl` (drei) und `KonfigurationCtrl`.

**Neu auf der Windows-Seite:** `Views/Simulation/SimulationKonfigHuelle.cs` —
die ganze Datenseite der Seite (Kachelaufbau, Chips, Speicherdaten, Schema,
Editoren-Parametersätze).

### Commits

| Hash | Betreff |
|---|---|
| `2e75393` | iU9-W10b.0a: `SchemaModell` in den Kern, `SchemaLayout` neu |
| `93bd88f` | iU9-W10b.0b: inline-SQL in Controller, `Kaskade` und Quellenwahl in den Kern |
| `cac6eb4` | iU9-W10b.0c: Baustein `Schema` — das Hydraulikbild als SVG |
| `4caee3f` | iU9-W10b.0d: Bausteine `ErzeugerKachel` und `SpeicherKachel` |
| `dd132ff` | iU9-W10b.1: die Simulationskonfiguration als Seite, vier Teildateien gelöscht |
| `d75908c` | iU9-W10b.2: Befund W10b‑B42 — `DatenzugriffTests` gehört in die Testdatenbank-Sammlung |
| `6bea64e` | iU9-W10b.3: Formularkarte auf den Stand nach W10b |

**Zwei Abweichungen von der Schrittfolge der Arbeitsanweisung**, beide
begründet:

* **W10b.1 ist EIN Commit statt drei.** Die Anweisung teilte ihn in
  „Kartenspalten", „Schema-Blatt" und „Überlagerungen + Löschen" — eine
  Vorsichtsmaßnahme gegen den XL-Umfang (Risiko R‑W10b‑6), keine fachliche
  Vorgabe. Geteilt hätten die ersten beiden Commits einen Zustand
  festgeschrieben, in dem die Seite bereits die einzige Fassung ist (die
  Aufrufer sind laut Anweisung in Schritt 1 umgestellt), ihre Editoren aber
  noch nicht öffnen: eine Maske, deren Chips und deren ✎ ins Leere laufen. Das
  Anbinden des Schemablatts sind außerdem sechs Zeilen Markup und ein Delegat.
  Der eine Commit hält stattdessen **Regel M1 wörtlich**: Derjenige Commit, der
  die WinForms-Fassung entfernt, ist derjenige, der ihren vollständigen Ersatz
  an die Aufrufstelle hängt.
* **W10b.2 trägt nicht die Ressourcen, sondern Befund W10b‑B42.** Die sieben
  neuen Schlüssel mussten in W10b.1 liegen — ohne sie übersetzt die Hülle
  nicht. An der Stelle des Ressourcenschritts steht deshalb der Befund, der
  beim zweisprachigen Nachweis aufgeschlagen ist (§ 8.2).

---

## 2. Die Hosting-Entscheidung R‑W10b‑1

**Die Komponente ist eine SEITE. Unter Windows steht sie bis W16 in einer
modalen Dialoghülle.**

Der Wellenplan nennt `Simulation_Config` eine Seite, und als solche ist sie
gebaut: `EPOS.UI/Seiten/Simulation/SimulationKonfigSeite.razor` mit
`SeitenZustand` für den Projektwechsel, einem Eintrag
`Seitenschluessel.SimulationKonfiguration` und einem Zweig in `AppWurzel` —
damit ist sie **die erste Fachseite, die iOS über die Wurzelkomponente
erreicht**.

Unter Windows zeigt `SimulationKonfigHuelle.Oeffnen` sie in
`BlazorDialogForm<SimulationKonfigSeite>` (Wunschgröße 1 120 × 620, `Sizable`).
Das ist kein Rückschritt, sondern der Zustand der beiden Aufrufer:
`Form_Start.btn_SimKonfig_Click` und
`Form_Simulation_Detail.btn_Konfiguration_Click` erwarten die **modale
Rückkehr** — der zweite springt danach auf seinen gemerkten Reiter zurück
(`mainTabPageIndex`). Die W5-Seitenhülle `BlazorSeite<T>` ist ein eingebettetes
`UserControl` in einem Reiter der Startmaske; für eine per Knopf geöffnete
Maske passt sie nicht.

**Ein späteres Umhängen ändert nur die Hülle.** Die Seite schließt sich über
„Beenden" (`Geschlossen`), Speichern schreibt sofort — beides unabhängig davon,
ob ein Fenster oder ein Reiter darum herum steht.

---

## 3. Bauweise

### 3.1 Der Kern zuerst — vier Vorabschritte

| Schritt | Was in den Kern ging | Befund |
|---|---|---|
| W10b.0a | `SchemaModell` (verschoben), `SchemaLayout` (neu) | R‑W10b‑4 |
| W10b.0b | fünf Controller-Wege, `AnlagenInfo`, `Kaskade`, `WaermequelleClass.QuelleSchreiben` | W10‑B35, B15 |
| W10b.0c | Baustein `Schema` (SVG) samt `SchemaBild` | — |
| W10b.0d | Bausteine `ErzeugerKachel`, `SpeicherKachel` samt `KachelDaten` | — |

### 3.2 `SchemaLayout` — die Anordnung wird headless prüfbar

`SchemaModell` (1 015 Z.) war schon oberflächenfrei: **was** gezeichnet wird,
stand ohne GDI+ fest. **Wo** es steht, stand in `SchemaAnsicht.Neuordnen`
(:203–351) und war nur über Pixel prüfbar. `SchemaLayout.Anordnen(modell,
breite)` trägt die Regeln wörtlich: Spaltenbreiten `{150, 214, 190, 132}`,
Abstand 56, Erzeuger von oben nach unten in Kaskadenreihenfolge, Quellen auf
Erzeugerhöhe, Speicher und Abnehmer auf die mittlere Höhe ihrer eingehenden
Kanten, Auflösung der Überschneidungen mit Abstand 14, Knotenhöhe
`2 × 8 + 19 + Zeilen × 15 (+ Badge 17 + 3) (+ Warnzeile 15)`, Kaskadenband,
Legende, der Bézierbogen samt Rückwärtskante (`tief = max(a.Y, b.Y) + 26`) und
der Prioritätspunkt bei t = 0,5.

**Eine Abweichung, begründet (A‑1).** Der Vorläufer maß Textbreiten mit
`TextRenderer.MeasureText`, also mit GDI+. Ohne Oberfläche gibt es keine
Messung; die Breite einer Bandpille und eines Legendeneintrags wird aus der
Zeichenzahl geschätzt (`SchemaLayout.ZEICHEN_BREITE = 6,0`). Das betrifft
**ausschließlich den Umbruch von Band und Legende** — Knoten und Kanten sind
zeichenunabhängig, und im SVG steht der Text zentriert in seiner Pille.

**R‑W10b‑4 beantwortet.** `git grep SchemaModell` vor dem Umzug: zwei
Aufrufer, beide in der Maske selbst (`Form_Simulation_Config.Schema.cs`,
`SchemaAnsicht.cs`). Die Dialogprüfung `WaermesenkeClass.Pruefen` benutzt es
**nicht** — sie liest `Hydraulikbild` unmittelbar. Der Umzug brauchte keine
Namespace-Anhebung: Der Kern führt denselben Namensraum
`WindowsFormsApplication1`.

### 3.3 `Kaskade` — sechs unsichtbare Steuerelemente werden ein Kernmodell

Bis W10b bedienten sechs **dauerhaft unsichtbare** Auswahlfelder samt Haken das
Persistenzmodell `Tab_Einstellungen.Tool_1..6`; die Karten waren eine Ansicht
auf sie, und `_kaskadeSetzen` war die Sperre gegen die Rückkopplung ihrer
Ereignisse. Ohne WinForms gibt es diese Steuerelemente nicht. `Kaskade` rechnet
deshalb unmittelbar auf dem `KonfigurationModel` — dem Satz, den
`KonfigurationCtrl` liest und schreibt:

* `Lesen`/`Schreiben`/`Belegt` — die vier Plätze,
* `Verschieben` — **Platzinhalte tauschen, nicht verdichten**: Eine
  geschlossene Lücke änderte Positionen, die niemand angefasst hat, und
  `Ladeordnung.Kaskadenpositionen` liest die Spaltennummer als Sortierkriterium,
* `Aufnehmen` — erster freier Platz **hinter** dem letzten belegten,
* `Entfernen`, `StromAuswahl`/`StromWert`, `Erzeugerliste`, `TypZuAnlagentyp`.

Die Rückkopplungssperre entfällt ersatzlos: Ein Modellfeld löst kein Ereignis
aus.

### 3.4 Die Chips: aus GDI-Karten werden Bausteine

`ErzeugerKachel` und `SpeicherKachel` übernehmen die **sechs Chipstile**, die
**sechs Chipziele**, die **acht Ereignisse** und alle Sichtbarkeitsregeln des
Vorläufers. Entfallen ist die Pixelarithmetik: `Neuordnen` (46 Z.),
`Innenbreite` (`MaximumSize` gegen einen `FlowLayoutPanel`, der ohne
Breitenvorgabe alles in **eine** Zeile legte) und `HoeheNachfuehren`. Entfallen
ist auch `Melden(…)` mit seinem `BeginInvoke` — der Empfänger entsorgte die
Karte aus ihrem eigenen Klick-Ereignis heraus; in Blazor wird kein
Steuerelement entsorgt.

Das `SchwellenBand` (ein eigenes `Control` mit `OnPaint`) ist ein Inline-SVG
mit derselben Bahn, derselben Reservezone zwischen Nachrang- und
Abschaltschwelle und denselben drei Marken.

### 3.5 Drei Ebenen Überlagerung, ein Fenster

```
SimulationKonfigSeite                                   (Ebene 0)
 ├─ Betriebsmodus | WP-Priorität (WertAbfrage)          (Ebene 1)
 ├─ Wärmequelle  → Auswahlüberlagerung                  (Ebene 1)
 │                 ├─ QuellePufferspeicherDialog        (Ebene 2)
 │                 │    └─ PufferSpProjektDialog        (Ebene 3, aus W10a)
 │                 ├─ QuellprofilDialog                 (Ebene 2)
 │                 │    └─ WertAbfrage                  (Ebene 3, aus W10a)
 │                 └─ QuelleErdreichDialog              (Ebene 2)
 │                      └─ KlimazonenkarteDialog        (Ebene 3, aus W10a)
 ├─ Wärmesenke   → WaermesenkeDialog                    (Ebene 1)
 │                      └─ PufferSpProjektDialog        (Ebene 2, aus W10a)
 └─ Pufferverwaltung → PufferSpProjektDialog            (Ebene 1)
```

**Die Seite führt zwei Ebenen selbst** (`_ebene1`, `_ebene2`, je mit ihrem
`Gaben`-Wörterbuch); die tieferen bringen die Dialoge der Welle 10a **selbst
mit** — sie waren dort schon als Überlagerung gebaut. Damit steht die vom
Auftrag genannte Kette Seite → Quelle/Senke → Pufferverwaltung →
Klimazonenkarte vollständig in **einem** Fenster und in **einer** WebView
(Risiko R2). Esc schließt immer nur die oberste Ebene, weil jeder Wirt seinen
eigenen Schalter prüft (Muster `WaermepumpenDialog`, W7.5).

**Der Quellen-Inlineeditor wird eine Auswahlüberlagerung.** Der Vorläufer
klappte an der Karte eine `ComboBox` auf (`_wqCombo`, `DroppedDown = true`) und
rechnete dafür Kartenkoordinaten in Formularkoordinaten um (`KarteAlsZelle`,
`SchemaElementAlsZelle`). Eine an ein Pixelrechteck geheftete Klappliste hat im
Browser kein Gegenstück; an ihre Stelle tritt eine Überlagerung mit **einem
Knopf je Zweig**, der gespeicherte Typ hervorgehoben. Die beiden
Umrechnungsmethoden entfallen ersatzlos.

### 3.6 `SeitenZustand` — einmal gebraucht, nicht doppelt

**Nein, doppelt gebraucht wird er nicht.** Die Seite hängt an **einem**
`SeitenZustand`; die Hülle legt ihn an, setzt das Projekt vor dem ersten
Zeichnen und reicht ihn als Parameter herein. Die sieben Überlagerungen sind
**Dialoge** und keine Seiten: Sie bekommen ihre Parameter einmal beim Öffnen
und melden über `Geschlossen` zurück — genau wie in W10a. Ein zweiter
`SeitenZustand` hätte nichts, was er tragen könnte.

Der Weg ist damit: `SeitenZustand.Geaendert` → `InvokeAsync` →
`Neuladen()` → `Dienste.Laden(idProjekt)`. Unter Windows löst ihn heute
niemand aus (der Dialog ist modal und lebt kurz); auf iOS und nach W16 ist er
der Projektwechsel.

### 3.7 Neun Auffrischungsstellen werden eine

`AktualisiereErzeugerUebersicht` hatte **neun** Aufrufstellen (Befund W10‑B40 —
der Quelltextkommentar sprach von acht): `.ctor`, `SetControls`, `AddErzeuger`,
`StromAuswahlSetzen`, `PufferVerwaltungOeffnen`, `BetriebsmodusBearbeiten`,
`WpPrioritaetBearbeiten`, `WaermesenkeBearbeiten` und
`WqCombo_SelectedIndexChanged`. In der Seite ist es **ein** `Neuladen()`; jede
Handlung, die schreibt, ruft es am Ende.

Der Zwischenspeicher bleibt: Warnbefunde, Booster-Anlagen, Quellnutzer,
geladene Puffer, Systemtemperaturen, Schichtenzahlen und `T_oben` werden
**einmal je Auffrischung** geholt, nicht je Kachel — dieselbe Begründung wie im
Vorläufer (Projekt 1023 der Arbeitskopie führt 79 Puffer-Zeilen).

---

## 4. Feldkarten-Abgleich

Die Karte von `Form_Simulation_Config` wurde vor Wellenbeginn **neu gezogen**
(Befund W10‑B1: die Karte vom Stand `aef9509` nannte zwei mit iU9‑W0 gelöschte
Aufrufer). Maßgeblich ist der heutige Stand.

| Vorläufer | Karte | Seite | Anmerkung |
|---|---|---|---|
| `Form_Simulation_Config` | 29 Designer-Steuerelemente, **davon 24 dauerhaft unsichtbar**; 5 sichtbar (`btn_Help`, `lblStatus`, `btn_OK`, `btn_Speichern`, `label11`); 13 Laufzeit-Steuerelemente + n Karten; 791 × 427 Entwurf, 1 120 × 620 Laufzeit | Kopfzeile mit Titel und `InfoKnopf`, Umschalter mit zwei Knöpfen, zwei Spalten (61,5 % / 38,5 %) mit drei Gruppen und n Kacheln, Fußzeile mit zwei `Schalter`, `Warnbanner` und `SpeichernLeiste` | **−24**: Die unsichtbaren Steuerelemente sind das abgelöste Persistenzmodell (`Kaskade`) bzw. seit Etappe D1 stillgelegt (`groupBox_PufferSp`, `listView1`, `btn_Hinzu`, `btn_Loeschen`, `checkBox_PufferSp`, `label12`, `label21`). |

**Der Feldbestand der Seite ist gezählt, nicht geschätzt**: 1 Kopfzeile
(Titel + Infoknopf + Label + 2 Umschaltknöpfe), 2 Spaltenköpfe, 3
Gruppenüberschriften, n Erzeugerkacheln, 1 Textschalter „nicht gewählte
anzeigen", 1 Hinweiszeile, m Speicherkacheln, 1 Knopf „Pufferspeicher anlegen /
verwalten…", 2 Schalter, 1 Statusbanner, 2 Knöpfe. Die Kartenspalten, der
Umschalter und die Fußzeile entstanden im Vorläufer zur **Laufzeit** — ihre
Feldliste stand nie im Designer und ist aus `Karten.cs`:200–410 und
`Schema.cs`:62–137 abgeglichen.

---

## 5. Abweichungen (mit Begründung)

| # | Was | Warum |
|---|---|---|
| **A‑1** | Textbreiten im Layout werden **geschätzt** statt gemessen | § 3.2. `TextRenderer.MeasureText` ist GDI+; betroffen sind nur Band- und Legendenumbruch |
| **A‑2** | Der Quellen-Inlineeditor wird eine **Auswahlüberlagerung** | § 3.5. Eine an ein Pixelrechteck geheftete Klappliste hat im Browser kein Gegenstück; `KarteAlsZelle` und `SchemaElementAlsZelle` entfallen |
| **A‑3** | Die Statuszeile bleibt **stehen**, statt nach 3 s zu verschwinden | Der Vorläufer blendete sie über einen Timer aus (`statusTimer`, 3 000 ms). Eine Meldung, die von selbst verschwindet, ist keine — sie steht jetzt als `Warnbanner` bis zur nächsten Handlung. Dieselbe Entscheidung wie A‑11 aus Welle 10a (grüne Statuszeilen → Warnstufe `Erfolg`) |
| **A‑4** | Die **neun `MessageBox`** werden Warnbanner und Rückfragen | Wie A‑10 aus den Wellen 9 und 10a: Die Sperrmeldung (ADR‑001), die drei Vorprüfungen („nur WP", „nur WP", „Quellenart"), die Luft-Wasser-Sperre, der PV-Hinweis und die CSV-Fehlermeldung bleiben **wörtlich** — nur der Träger ist ein anderer. Die CSV-Rückfrage bleibt eine Rückfrage (`Rueckfrage`, OK/Abbrechen) |
| **A‑5** | Die **Kaskadenkette** und der Schemaknoten sind mit der **Tastatur** erreichbar | Der Vorläufer war ein reines Mausziel (`OnMouseDown`/`OnMouseDoubleClick` auf einem `Panel`). Jeder Kasten und jedes Bandglied trägt jetzt `tabindex`; Eingabe wählt, Eingabe auf dem bereits gewählten Element öffnet den Editor. Dasselbe gilt für einen **Chip mit Editorziel**: Er ist ein Knopf, nicht ein Label |
| **A‑6** | „Sichtbar machen" läuft über den **Fokus** statt über `scrollIntoView` | `SchemaAnsicht.SichtbarMachen` setzte `AutoScrollPosition`. Der Fokus scrollt den Kasten im Browser selbst herbei — und EPOS.UI braucht dafür keine JS-Schicht (dieselbe Überlegung wie bei der Fokusfalle der `Ueberlagerung`) |
| **A‑7** | `SpeichernLeiste` trägt „Speichern" **immer aktiv** | Der Baustein sperrt den Knopf ohne markierten Satz und ohne Änderung (`SatzMarkiert && Geaendert`). Der Vorläufer hatte diese Regel nicht: `btn_Speichern` war immer bedienbar, und „Speichern" schreibt hier den ganzen Satz `Tool_1..6`, nicht eine Zeile. Beide Merkmale stehen deshalb fest auf `true` |
| **A‑8** | Der **Fensterweg** der sechs W10a-Hüllen entfällt, ihr **Parametersatz** bleibt | Ihr einziger Aufrufer war `Form_Simulation_Config`. `Gaben`/`Dienste` waren in W10a ausdrücklich „ohne `Geschlossen`" getrennt gehalten — für genau diesen Tag. Mit dem Fensterweg fällt je Hülle das Maß, der `BlazorDialogForm`-Aufruf und der Titelhelfer; die Überlagerung trägt ihre Überschrift selbst |
| **A‑9** | Der Modul-Ausweis liest **direkt** aus `MyResource` | Befund W10‑B36: Das `T(schluessel, rueckfall)`-Muster mit deutschem Rückfall war tot — `SIM_KARTE_MODUL` und `SIM_KARTE_TIP_MODUL` stehen längst in beiden `.resx` **und** in `Resource.Designer.cs` |
| **A‑10** | Drei Gruppenköpfe bekommen **eigene** Ressourcenschlüssel | Befund W10‑B34: Sie kamen aus `label1/2/3.Text` — den Texten **unsichtbarer** Steuerelemente in `groupBox_Tools`. Ein sichtbarer Text, der aus einem unsichtbaren Steuerelement stammt, ist eine Falle für die nächste Übersetzung |
| **A‑11** | `ErzeugerKatalog.Liste` und der Typ `LanguageItem` sind gelöscht | Sie bauten ausschließlich die `DisplayMember`/`ValueMember`-Listen der sechs unsichtbaren ComboBoxen. `Anzeige`/`DbWert` und die drei Katalogfelder bleiben — sie sind die EINE Quelle der Zuordnung (Paket 9 / L4) |
| **A‑12** | Kein KI-Aufrufknopf, keine Laufzeit-Steuerelemente, keine Pixelarithmetik | Wie A‑16 aus Welle 10a: Der KI-Einstieg hat in `EPOS.UI` noch keinen Baustein (W15b); die 13 Laufzeit-Steuerelemente und die fünf Layoutmethoden erledigen Hülle und CSS |
| **A‑13** | `IProjektQuelle` bekommt `SimulationKonfigGaben` **mit Standardumsetzung** | Die Schnittstelle wird von `EPOS.iOS/Dienste/IosProjektQuelle` umgesetzt, und `EPOS.iOS` steht bewusst weder in `WP-Plan.sln` noch im Solution-Filter — eine Pflichtmethode hätte den iOS-Job stumm gebrochen. Die Standardumsetzung liefert `null`: „Wer sie nicht umsetzt, kennt die Seite eben nicht" |

---

## 6. Texte

**Sieben neue Schlüssel** in `EPOS.Kern/MyResource/Resource.resx`,
`Resource.en-US.resx` und — von Hand, weil hier kein Visual Studio läuft —
`Resource.Designer.cs`:

| Schlüssel | de | en | Herkunft |
|---|---|---|---|
| `SIM_KONFIG_TITEL` | Simulation Konfiguration | Simulation configuration | `$this.Text` |
| `SIM_KONFIG_KOPF` | Erzeuger definieren, Pufferspeicher zuordnen: | Define generator, assign buffer storage: | `label11.Text` |
| `SIM_KONFIG_BTN_SPEICHERN` | Konfiguration speichern | Save configuration | `btn_Speichern.Text` |
| `SIM_KONFIG_BTN_BEENDEN` | Beenden | Finish | `btn_OK.Text` |
| `SIM_KARTE_GRUPPE_WAERME` | Wärmeerzeuger | Heat generator | `label1.Text` (unsichtbar, W10‑B34) |
| `SIM_KARTE_GRUPPE_STROM` | Stromerzeuger | Electricity generator | `label2.Text` (unsichtbar) |
| `SIM_KARTE_GRUPPE_SPEICHER` | Energiespeicher | Energy storage | `label3.Text` (unsichtbar) |

**Der achte sichtbare en-Text braucht keinen Schlüssel:** `lblStatus.Text`
(„✔ Konfiguration erfolgreich gespeichert" / „✔ Configuration saved
successfully") ist der Entwurfswert einer Statuszeile, deren Text zur Laufzeit
**immer** gesetzt wird — er stand längst als `SIM_STATUS_KONFIG_GESPEICHERT` im
Kern-Katalog.

**Alle übrigen 122 Schlüssel lagen schon im gemeinsamen Katalog** (`SIM_*`,
`SIMQ_*`, `PSP_*`, `SIMWARN_*`, `SP_*`); die Hülle greift auf **129**
verschiedene zu. Hartcodierte deutsche Strings im Vorläufer: 0 — bis auf das
`HilfeKontext`-Literal, das die Hülle wörtlich übernimmt.

**Probe:** `Resource.resx` und `Resource.en-US.resx` führen **3 831 Einträge
(XML-Zählung) bzw. 3 835 `data`-Zeilen** — je Sprache dieselbe Zahl, **0
Dubletten**, **0 Schlüssel nur in einer Sprache**. Die Designer-Lücke (Schlüssel
ohne erzeugte Eigenschaft, ausschließlich über `ResourceManager.GetString`
erreichbar) bleibt bei **139** — alle sieben neuen Schlüssel sind
eingetragen.

**Nicht übersetzt sind die Steuerwerte:** die sechs Quellentypen
(`WaermequelleClass.TYP_*`), die drei Betriebsmodi (`MODUS_*`), die
Erzeuger-DB-Werte (`DbWerte.ERZEUGER_*`) und die beiden Lesepunktwerte. Sie
stehen in der Datenbank und gehen als eigene Parameter in die Komponente —
Drei-Schichten-Regel, Persistenzschicht.

**`help_mapping.txt`** verliert seine **vier Feldzeilen** (`label11`,
`flow_Erzeuger`, `flow_Speicher`, `label_Ansicht`): Der `HelpExtender` hängt
sich an `Control.Name` und findet in einer WebView keinen. Die Zeile
`Form_Simulation_Config.btn_Help = Simulation` **bleibt** — der Schlüssel
benennt die Wikiseite, nicht die Klasse (dieselbe Regel wie in Welle 10a); die
Seite trägt ihn als `HilfeSchluessel` ihres `InfoKnopf`.

**`Allgemein/KI/HilfeKontext.cs`:** der Eintrag der gelöschten Maske entfernt
(Regel F10). Den Bereich meldet jetzt die Hülle beim Aktivieren des Fensters —
derselbe Text, nur ohne Formularklasse dahinter.

---

## 7. WinForms-Seite

**Gelöscht** (11 Dateien):

```
Views/Simulation/Form_Simulation_Config.cs             678 Z.
Views/Simulation/Form_Simulation_Config.Karten.cs    2 248 Z.
Views/Simulation/Form_Simulation_Config.Schema.cs      433 Z.
Views/Simulation/Form_Simulation_Config.Uebersicht.cs 1 199 Z.
Views/Simulation/Form_Simulation_Config.Designer.cs    337 Z.
Views/Simulation/Form_Simulation_Config.resx         (297 data)
Views/Simulation/Form_Simulation_Config.en-US.resx    (25 data)
Views/Simulation/ErzeugerKarte.cs                      781 Z.
Views/Simulation/SpeicherKarte.cs                      551 Z.
Views/Simulation/SchemaAnsicht.cs                      789 Z.
Views/Simulation/Eingabefrage.cs                        49 Z.
```

**Verschoben:** `Allgemein/Simulation/SchemaModell.cs` (1 015 Z.) →
`EPOS.Kern/Allgemein/Simulation/`. Damit führt `Allgemein/Simulation/` **keine
Datei mehr**; die letzte Ausnahme des Kern-Umzugs ist abgetragen.

**Getrimmt:** `Views/Simulation/ErzeugerKatalog.cs` (`Liste` und
`LanguageItem`, A‑11) und die **sechs W10a-Hüllen** (Fensterweg, A‑8).

**Bleiben:** `Allgemein/GrafikTools/KartenStil.cs` (Hüllen, `Kreisziffer`,
`WarnStufe`-Farben, `AktionsKarte`), `Views/Simulation/SchluesselEintrag.cs`
(`NavigatorWaerme`) und `Views/Simulation/ErzeugerKatalog.cs` in seinem Kern.

**Neu:** `Views/Simulation/SimulationKonfigHuelle.cs`.

**Aufrufer umgestellt (2):** `Form_Start.cs:1228` und
`Form_Simulation_Detail.cs:4105`. Beide verlieren das Lesen des
Einstellungssatzes (das macht `KonfigurationCtrl.LiesProjekt` in der Hülle) und
die **halbierten Bildschirmkoordinaten** (`p1.Y /= 2; p1.X /= 2;`, Befund
W10‑B37).

**Keine Typverwendung ist übrig:**

```
grep -rnE "(new|typeof|:)\s*(Form_Simulation_Config|ErzeugerKarte|SpeicherKarte|
    SchemaAnsicht|Eingabefrage)\b" --include=*.cs --include=*.razor .
→ 0 Treffer
```

Restfundstellen der alten Namen sind ausschließlich (a) die
`HilfeSchluessel`-Zeichenkette `"Form_Simulation_Config.btn_Help"`, (b)
Kommentare, die die Herkunft einer Regel nennen, und (c) der datierte
Erreichbarkeitsbericht.

---

## 8. Nachweise

### 8.1 Build

```
dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental
→ 0 Fehler, 12 Warnungen
```

**17 → 12.** Die fünf entfallenen sind WFO1000-Fundstellen der gelöschten
Steuerelement-Klassen (`ErzeugerKarte` 2, `SpeicherKarte` 2, `SchemaAnsicht` 1);
damit steht WFO1000 bei **6**. Die übrige Aufteilung ist unverändert: 2 CS0108,
2 CS0109, 1 WFO0003, 1 CA2255. **Keine neue Warnung.**
`dotnet build EPOS.UI -c Release` → 0 Fehler, **0** Warnungen.

### 8.2 Tests

```
dotnet test WP-Plan.Kern.slnf -c Release
→ KiKern.Tests          450 grün
  SpeicherEngine.Tests   337 grün
  EPOS.Kern.Tests        265 grün   (+37 aus Welle 10b)
  EPOS.UI.Tests        1 327 grün   (+58 aus Welle 10b)
  zusammen             2 379 grün, 0 rot
```

**Beide Sprachen.** Die Regel seit Welle 8 verlangt einen zweiten Lauf mit
englischer Umgebung:

```
LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8 dotnet test WP-Plan.Kern.slnf -c Release
→ dieselben 2 379 grün, 0 rot
```

**95 neue Fälle:** `SchemaLayoutTests` 14 (Spaltenzuordnung, Ordnung, keine
Überschneidung, Knotenhöhe, Kantengeometrie vorwärts und rückwärts,
Prioritätspunkt, Band, Treffer, Determinismus, Leermodell, Wunschbreite),
`KaskadeTests` 15 (die sieben Wege plus die Regeln „tauschen statt
verdichten" und „hinter dem letzten belegten"), `SimulationKonfigDatenTests` 8
(die fünf Controller-Wege und `QuelleSchreiben` je Zweig gegen eine
Arbeitskopie von `Kenndaten_Test.sqlite`), `SchemaTests` 13,
`ErzeugerKachelTests` 10, `SpeicherKachelTests` 9,
`SimulationKonfigSeiteTests` 26.

**Befund W10b‑B42 — ein reproduzierbarer Testausfall gefunden und behoben.**
Beim zweisprachigen Nachweis fiel in **einem von zehn** Läufen ein ganzer Block
aus: 12 Fälle in `EnergietraegerVarianteCtrlTests` und `BedarfProfilTests`.
Ursache: `DatenzugriffTests.Pfadueberschreibung_schlaegt_die_Einstellungen`
**setzt** `DataRepository.PfadUeberschreibung` — das statische Feld, um
dessentwillen es die Sammlung „Testdatenbank" gibt —, trug die
`[Collection]`-Marke aber nicht und lief damit **neben** den
datenbankgestützten Klassen. Traf sie deren Zeitfenster, zeigten die
Arbeitskopien plötzlich auf `/tmp/probe/Kenndaten.sqlite`; die
Wiederherstellung am Ende machte es schlimmer, weil sie den Pfad der gerade
laufenden Arbeitskopie festschrieb. Mit der Marke sind **achtzehn Läufe
hintereinander** grün (zehn unter der Vorgabekultur, acht mit `LANG=en_US`).

### 8.3 Formularkarte

```
dotnet test Werkzeuge/Formularkarte.Tests -c Release
→ 123 grün
```

Drei Zähler bewegt: **50** Designer-Dateien (51), **49** Masken (50), **28**
lokalisiert (29), **48 von 49** erreichbar (49 von 50).

### 8.4 Stapellauf

```
dotnet run --project Werkzeuge/Formularkarte -- --alle WindowsFormsApplication1 --erreichbarkeit
→ Designer-Dateien 50, davon Masken 49, lokalisiert 28, Kartenzeilen 887,
  erreichbar 48, unerreichbar 0, verwaist 0, unklar 1
```

**49 = 50 − 1.** `ErzeugerKarte`, `SpeicherKarte`, `SchemaAnsicht` und
`Eingabefrage` hatten nie einen Designer und standen deshalb nie in dieser
Liste — dieselbe Lage wie Befund W10‑B38 in Welle 10a.
`Form_Simulation_Config` war zugleich die **einzige lokalisierte Maske** der
ganzen Welle; damit ist auch Befund W10‑B39 erledigt.
`Werkzeuge/Formularkarte/Erreichbarkeit_2026-09-03.md` ist neu erzeugt und um
den Abschnitt „Stand nach iU9‑W10b" ergänzt.

### 8.5 SQL-Dialektprüfer

```
python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite
→ 1 239 SQL-Texte geprueft: 0 Fundstellen, 171 dynamisch, 1 068 in Ordnung
python3 … --selbsttest
→ 32 Anweisungen, 0 Abweichungen
```

**1 240 → 1 239.** Die fünf inline-SQL der Anzeigeschicht (Befund W10‑B35) sind
nicht verschwunden, sondern in vier Controller umgezogen — dort werden sie
weiter geprüft; die eine Zeile weniger ist die entfallene Doppelung der
`MAX(ID)`-Kopfabfrage.

### 8.6 ChartProben

```
dotnet run --project Proben/ChartProben -c Release
→ 16 Bilder geprueft, 0 Verstoesse. ERGEBNIS: alle gruen.
```

Unverändert **16** — diese Welle bringt kein neues Renderer-Bild. Das Schema
ist SVG in der Oberfläche, kein PNG aus dem Kern.

### 8.7 Referenzlauf

**Pflicht in dieser Welle**, weil `SchemaModell`, `SchemaLayout`, `Kaskade`,
fünf Controller-Methoden und `WaermequelleClass.QuelleSchreiben` unmittelbar am
Simulationseingang liegen.

```
dotnet run --project EPOS.Referenzlauf -c Release -- \
  lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte 1030,1007,1017 \
  --ziel artifacts/reflauf/w10b
→ Erfolgreich: 3 von 3

dotnet run --project EPOS.Referenzlauf -c Release --no-build -- \
  vergleich artifacts/reflauf/ref artifacts/reflauf/w10b
→ Projekt_1007: PASS (29 Dateien, 324 219 Werte)
  Projekt_1017: PASS (21 Dateien, 254 154 Werte)
  Projekt_1030: PASS (22 Dateien, 236 670 Werte)
  GESAMT: PASS (815 043 Werte innerhalb der Toleranz)

diff -rq je Projekt
→ BYTE-GLEICH: Projekt_1030, Projekt_1007, Projekt_1017
```

`artifacts/reflauf/ref` sind die drei Projektordner der eingefrorenen Basis
`Referenzlaeufe/2026-08-30_B3-Kaskade`. **Byte-gleich, nicht nur innerhalb der
Toleranz.**

### 8.8 Veröffentlichung

```
dotnet publish WindowsFormsApplication1 -c Release -p:Platform=x64 -o <ordner>
→ wwwroot/index.html, wwwroot/_framework, wwwroot/_content/EPOS.UI/epos-ui.css
  (94 Fundstellen der neuen Klassen epos-schema*/epos-simkonfig*/epos-erzeugerkachel*)
```

### 8.9 Alles noch einmal auf dem zusammengeführten Stand

`origin/ios_migration` ist seit der Basis `427fd59` um drei Commits gewachsen —
die ausdrückliche Frist im `KapitalwertVerlaufDialogTests` (W10a‑O‑8), der
W10a-Statusblock im Umsetzungskonzept und die Nachweisliste iU9. **Kein
Konflikt.** Nach dem Merge ist das ganze Netz neu gezogen:

| Tor | Ergebnis |
|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64` | 0 Fehler, 12 Warnungen |
| `dotnet test WP-Plan.Kern.slnf -c Release` | 2 379 grün, 0 rot |
| dasselbe mit `LANG=en_US.UTF-8` | 2 379 grün, 0 rot |
| `dotnet test Werkzeuge/Formularkarte.Tests` | 123 grün |
| Stapellauf Formularkarte | 50 Designer, 49 Masken, 28 lokalisiert, 48 erreichbar, 0 unerreichbar, 0 verwaist, 1 unklar |
| SQL-Dialektprüfer | 1 239 Texte, 0 Fundstellen; Selbsttest 32 / 0 |
| ChartProben | 16 Bilder, 0 Verstöße |
| Referenzlauf 1030/1007/1017 | PASS, 815 043 Werte, **alle drei byte-gleich** |

---

## 9. Grenzen

* **Am Gerät ungeprüft.** Alles hier ist ohne Windows entstanden. Die
  Abnahmeliste in § 10 ist der Prüfplan.
* **Das Schema ist ohne Bildvergleich gebaut** (Risiko R‑W10b‑3). Der
  GDI+-Bildvergleich ist mit iF23 gelöscht, und ein Bildschirmfoto des Bestands
  war ohne Windows nicht zu bekommen. Die Anordnung ist stattdessen im Kern
  geprüft (14 Fälle), die Zeichnung im Baustein (13 Fälle) — Knotenzahl,
  Kantenzahl, Farbklasse je Kantenart, gestrichelte Kaskadenkante,
  Prioritätskreis, Warnrahmen, Band und Legende. **Was daraus nicht folgt: ob
  es genauso AUSSIEHT.** Das ist Abnahmepunkt 4.
* **Die Seite ist der erste Blazor-Wirt mit sieben eigenen Kindern.** Bis W10a
  war `Form_Simulation_Config` ein WinForms-Wirt, der je Kind eine eigene
  WebView aufmachte; jetzt ist es **eine** WebView für alles. Ob das auf einem
  älteren Gerät flüssig bleibt, ist Abnahmepunkt 1.
* **Der Projektwechsel über `SeitenZustand` ist unter Windows unbenutzt.** Die
  modale Hülle setzt das Projekt einmal. Erst iOS und W16 fahren den Weg
  wirklich; die bunit-Probe deckt ihn ab, das Gerät noch nicht.
* **`ErzeugerKatalog` ist auf zwei Methoden zusammengeschrumpft** und hat nur
  noch die Hülle als Nutzer. Ob er langfristig in den Kern gehört
  (`DbWerte.ERZEUGER_*` liegt dort schon), ist eine Frage für W15.

---

## 10. Abnahmeliste Windows (iZ5)

Grundsätzlich je Ansicht: öffnet mittig, kein weißes Aufblitzen, ziehbar und
maximierbar, de **und** en (`HKCU\Software\wp-plan\Language`), Hochkontrast,
125 % und 150 % scharf, Maus **und** Finger (44 px), Tab-Zyklus bleibt im
Fenster, Esc schließt die oberste Ebene, Infoknopf zeigt die Wikiseite.

| # | Aufrufweg | Was besonders zu prüfen ist |
|---|---|---|
| 1 | **Startbild → Simulationskonfiguration** | Die Seite öffnet in einem modalen Fenster (R‑W10b‑1), 1 120 × 620; drei Gruppen mit ihren Köpfen, je Kaskadenplatz die Anlagen des Typs, ▲▼/× **nur auf der ersten Kachel je Typ**; die Zeit bis zum ersten Bild messen — eine WebView für alles |
| 2 | **Chips einer Wärmepumpenkachel** | Feste Folge: Modul (nur m > 1) · Quelle · Booster · Senke · Zweitsenke · Senke weiter (Rang ≥ 3) · Temperatur · Warnung · WP-Priorität · Betriebsmodus. Kessel ohne Prio und Modus, Solarthermie und BHKW **ohne Quellchip** |
| 3 | **Luft-Wasser-Wärmepumpe** | Der Quellchip steht **fest** (graue Fläche, kein Handzeiger); der Mouseover nennt die Bauart. Doppelklick meldet die Sperre als Banner, statt einen Editor zu öffnen |
| 4 | **Umschalter „Schema"** | Vier Spalten mit Köpfen, Kästen in Kaskadenreihenfolge, Ladeleitungen koralle mit Prioritätskreis, Versorgung grün, Prozess violett, Kaskade blau **gestrichelt**, darunter das Pillenband und fünf Legendeneinträge. **Gegen ein Bildschirmfoto des Bestands halten** (Risiko R‑W10b‑3) |
| 5 | **Auswahl in beiden Ansichten** | Kachel anklicken → im Schema hervorgehoben; Schemaknoten anklicken → Kachel hervorgehoben; umschalten → die Auswahl bleibt. Tab und Enter erreichen jeden Kasten (A‑5) |
| 6 | **Drei Überlagerungsebenen**: Chip „Quelle" → Erdreich → „…" neben der Klimazone | Alle drei stehen **im selben Fenster**; Esc schließt immer nur die oberste; nach dem Schließen steht die Seite mit frischen Karten da |
| 7 | **Chip „Senke" / ✎ / Doppelklick auf die Kachel** | Alle drei führen in denselben Senkendialog; danach steht die Statuszeile mit der Kurzform der Rang-1-Senke, und Karte **und** Schema zeigen denselben Text |
| 8 | **Speicherkachel** | Zugeklappt eine Zeile mit Badges und Kurzbilanz; Klick klappt auf **und** wählt aus; höchstens eine offen; Detailzeilen in der Reihenfolge Lader · Versorgt · Im Parallelverbund · Quelle für · Entladeprio · Temperaturen · Oberste Schicht (nur mit Lauf) · Schwellenband |
| 9 | **„+ Nicht gewählte Komponenten anzeigen (n)"** | Standardmäßig verborgen, die Zahl stimmt; eingeblendet stehen sie gestrichelt mit „+ aufnehmen"; die Vorliebe wird **nicht** gespeichert |
| 10 | **Kaskade umsortieren** | ▲▼ tauschen Platzinhalte und **verdichten nicht**: Ein leerer Platz zwischen zwei belegten bleibt leer. Danach „Konfiguration speichern" und in `Tab_Einstellungen` nachsehen |
| 11 | **Extrapolation und Booster-Lesepunkt** | Beide schreiben **sofort** (ohne „Speichern"), der Banner sagt was; der Lesepunkt ist **unsichtbar**, solange das Projekt keinen gekoppelten Booster führt |
| 12 | **Sperrzustand** | Mit einem Projekt auf halb migriertem Schema öffnen: Der Grund steht als Warnbanner, alles ist gesperrt — „Beenden" muss trotzdem gehen |
| 13 | **Ohne Projekt** | Karten leer, „Pufferspeicher anlegen / verwalten…" gesperrt, Extrapolationsschalter gesperrt, Vorbelegung bleibt „an" |
| 14 | **`Form_Simulation_Detail` → Konfiguration → Beenden** | Rücksprung auf den gemerkten Reiter (`mainTabPageIndex`) — der Grund, warum die Hülle modal bleibt |
| 15 | **Sprache auf en umstellen** und 1–14 stichprobenartig wiederholen | Alle 129 Textschlüssel liegen in beiden Sprachen; die Steuerwerte (Quellentyp, Modus, Erzeuger, Lesepunkt) dürfen sich **nicht** mit übersetzen |
| 16 | **iOS-Job** (`Actions → iOS → Run workflow`) | Die Seite ist die erste Fachseite, die `AppWurzel` erreicht. Der Job baut `EPOS.iOS` gegen die erweiterte `IProjektQuelle`; die Standardumsetzung (A‑13) muss ihn tragen, ohne dass `IosProjektQuelle` angefasst wurde |

---

## 11. Offene Punkte

| # | Was | Vorschlag |
|---|---|---|
| **W10b‑O‑1** | **Das Schema ist ohne Bildvergleich portiert.** Der GDI+-Stand ist mit iF23 gelöscht, ein Bildschirmfoto des Bestands war ohne Windows nicht zu bekommen (Risiko R‑W10b‑3 sah genau das vor) | Abnahmepunkt 4: am Gerät gegen ein Foto der letzten WinForms-Fassung halten. Die Anordnung selbst ist im Kern geprüft und ändert sich dabei nicht |
| **W10b‑O‑2** | **Die geschätzte Textbreite** (A‑1) bestimmt, wann das Kaskadenband und die Legende umbrechen. Bei sehr langen Anlagennamen kann eine Pille schmaler oder breiter ausfallen als der Text darin | Am Gerät mit einem Projekt mit langen Bezeichnern ansehen. Rückweg wäre, die Pillenbreite im SVG über `textLength` zu erzwingen |
| **W10b‑O‑3** | **`SpeichernLeiste` trägt „Speichern" fest aktiv** (A‑7). Der Baustein kennt die Regel „markierter Satz UND Änderung"; diese Seite kennt beides nicht | **Frage an den Anwender:** Soll der Knopf erst nach einer Änderung aktiv werden? Das wäre eine neue Regel, keine übernommene. **Entscheid (Anwender, 04.09.2026): Empfehlung angenommen — der Knopf bleibt wie im Vorläufer immer aktiv; keine Änderung.** |
| **W10b‑O‑4** | **Der Umschalter Liste/Schema ist kein `Reiter`.** Die Arbeitsanweisung ließ beides zu; gebaut sind zwei Knöpfe rechts oben — die Stelle und die Form des Vorläufers | Wörtlich übernommen. Wenn der Wizard nach iL5 kommt, wäre ein `Reiter` die einheitlichere Form |
| **W10b‑O‑5** | **`ErzeugerKatalog` steht noch in der Anwendung**, hat aber nur noch zwei Methoden und einen Nutzer (die Hülle) | Kandidat für den Kern-Umzug in W15; `DbWerte.ERZEUGER_*` liegt dort schon |
| **W10b‑O‑6** | Der KI-Aufrufknopf fehlt (A‑12) | Mit W15b, wenn `Gespraechsverlauf` steht — wie W6‑O‑6 bis W10a‑O‑6 |
| **W10b‑O‑7** | **Ein einzelner, nicht reproduzierbarer Testausfall bleibt.** Nach der Behebung von W10b‑B42 fiel in EINEM von zweiundzwanzig Läufen ein Fall in `GebaeudeKatalogTests` aus (`Gebaeudetypen_liefert_die_Sicht_Abfrage_Gebaeudetypen`); achtzehn Läufe unmittelbar danach sind grün | Verdacht: `TestDatenbank` kopiert je Testfall 77 MB nach `%TEMP%`; bei vier gleichzeitig laufenden Testprojekten sind das über 3 GB Durchsatz. **Vorschlag:** die Arbeitskopie als `IClassFixture` je KLASSE statt je Fall anlegen — ein eigener Schritt, keine Welle‑10b-Frage |

### Neue Befunde dieser Welle

* **W10b‑B41 (neu):** `Form_Simulation_Config.listErzeuger` hatte **keinen
  Leser.** `AddErzeuger` baute die Liste bei jedem Haken und jeder Auswahl neu
  auf (Zeilen 457–500); gelesen wurde sie im ganzen Repository nirgends. Die
  Ableitung bleibt trotzdem als `Kaskade.Erzeugerliste` erhalten — sie ist die
  dokumentierte Bedeutung von „welche Technologien rechnet dieses Projekt", und
  ohne Oberfläche ist sie erstmals prüfbar. Was mit dem Feld gegangen ist, ist
  die stille Verdopplung.
* **W10b‑B42 (neu, behoben):** `DatenzugriffTests` fehlte die
  `[Collection("Testdatenbank")]`-Marke, obwohl die Klasse
  `DataRepository.PfadUeberschreibung` setzt (§ 8.2).

### Entfallene Befunde

* **W10‑B33:** das tote Programm — `btn_Hinzu_Click`/`btn_Loeschen_Click` als
  No‑ops, `checkBox_PufferSp_CheckedChanged` und die drei `listView1_Draw*` an
  unsichtbaren Steuerelementen. Ersatzlos.
* **W10‑B34:** die drei Gruppenköpfe aus unsichtbaren Controls — sie haben
  eigene Schlüssel (A‑10).
* **W10‑B35:** die fünf inline-SQL der Anzeigeschicht — in vier Controllern.
* **W10‑B36:** das tote `T()`-Rückfallmuster — ersatzlos (A‑9).
* **W10‑B37:** die halbierten Bildschirmkoordinaten beider Aufrufer —
  ersatzlos.
* **W10‑B39:** `Form_Simulation_Config` ohne `de-DE.resx` — mit der Maske
  gegangen; `Views/Simulation` führt keine lokalisierte Maske mehr.
* **W10‑B40:** neun statt acht Auffrischungsstellen — es ist eine.

---

## 12. Geänderte und neue Dateien

**Neu in `EPOS.UI`** (6): `Bausteine/Schema.razor`, `Bausteine/SchemaBild.cs`,
`Bausteine/ErzeugerKachel.razor`, `Bausteine/SpeicherKachel.razor`,
`Bausteine/KachelDaten.cs`, `Seiten/Simulation/SimulationKonfigSeite.razor`,
`Seiten/Simulation/SimulationKonfigDaten.cs`.
**Geändert in `EPOS.UI`** (4): `Seiten/AppWurzel.razor`,
`Seiten/Seitenschluessel.cs`, `Dienste/IProjektQuelle.cs`,
`wwwroot/epos-ui.css`.

**Neu im Kern** (5): `Allgemein/Simulation/SchemaModell.cs` (verschoben),
`Allgemein/Simulation/SchemaLayout.cs`, `Allgemein/Simulation/Kaskade.cs`,
`Allgemein/Simulation/WaermequelleClass.Quellenwahl.cs`,
`Controller/WErzeugerCtrl.Konfigseite.cs`, `Model/AnlagenInfo.cs`.
**Geändert im Kern** (4 + Ressourcen): `Allgemein/Simulation/WaermequelleClass.cs`
(`partial`), `Controller/ErgebnisCtrl.cs`, `Controller/KlimaregionCtrl.cs`,
`Controller/KonfigurationCtrl.cs`; dazu die drei Ressourcendateien.

**Neu in der Anwendung** (1): `Views/Simulation/SimulationKonfigHuelle.cs`.
**Geändert in der Anwendung** (10): `Views/Hauptformular/Form_Start.cs`,
`Views/Simulation/Form_Simulation_Detail.cs`,
`Views/Simulation/ErzeugerKatalog.cs`, die sechs W10a-Hüllen,
`Allgemein/KI/HilfeKontext.cs`, `Allgemein/Hilfe/help_mapping.txt`.

**Neu in den Tests** (4): `EPOS.Kern.Tests/SchemaLayoutTests.cs`,
`EPOS.Kern.Tests/SimulationKonfigKernTests.cs`,
`EPOS.UI.Tests/Bausteine/SchemaTests.cs`,
`EPOS.UI.Tests/Bausteine/KachelnTests.cs`,
`EPOS.UI.Tests/Seiten/SimulationKonfigSeiteTests.cs`.
**Geändert in den Tests** (3): `EPOS.Kern.Tests/DatenzugriffTests.cs`
(W10b‑B42), `Werkzeuge/Formularkarte.Tests/StapelTests.cs`,
`Werkzeuge/Formularkarte.Tests/ErreichbarkeitTests.cs`; dazu
`Werkzeuge/Formularkarte/Erreichbarkeit_2026-09-03.md`.

## Windows-Abnahme 05.09.2026 — Formularraster, Paket P3 (iU8‑E‑2)

**Der Wortlaut** (Anwender, 05.09.2026): „Darstellung der Dialoge kompakter und
übersichtlicher — Parameterblöcke rechts. Genauso für andere Dialoge prüfen."
Aufgabe #90 hat daraus die hausweite Regel gemacht (Bausteine
`Formularraster`/`Formulargruppe`, Regel in `epos-ui.css`, Bestandsaufnahme aller
92 Dateien im Protokoll `iU9_W14a`); Paket **P3** hängt Bedarf, Simulation und
Projekt ein. **Kein Feld umbenannt, kein Text geändert, keine Regel je Dialog** —
ein Dialog stellt nur seinen vorhandenen Feldlauf in den Raster.

| Datei | Felder | Raster | Einspaltig | Klasse‑B‑Entscheid |
|---|---|---|---|---|
| `Dialoge/Simulation/QuelleErdreichDialog.razor` | 7 | 2 | Standort: **ja** | Klasse A/B gemischt. „Quellsystem": der Kasten `epos-erdreich-zweig` (ohne eigene Regel im Stilblatt) entfällt, die vier Felder werden Rasterkinder, die Optionsgruppe spannt wie gehabt über alle Spalten. „Standort" **einspaltig** — Bodentyp → Klimazone → Spreizung tragen eine Reihenfolge, und unter jedem Wert steht seine Herleitungszeile. **Nicht** umgestellt: „Vorschau" (ein Diagramm) und „Auslegungsprüfung" (zwei Warnbanner und ein Knopf) — beides sind keine Formularblöcke. |
| `Dialoge/Simulation/QuellprofilDialog.razor` | 7 | 3 | nein | Klasse A. Der lose Kopf (Profil, Betriebsart, Bezeichner, Beschreibung), die zwölf Monatswerte und der Altweg mit Wochentag und **24 Stundenwerten** — die standen bisher untereinander über die ganze Breite. **Nicht** umgestellt: die Werteseite (ein `Raster` mit zwei Spalten) und der Grafikreiter. |
| `Dialoge/Simulation/WaermesenkeDialog.razor` | 9 | 2 | **ja, beide** | **Klasse B.** Umgestellt sind „Die gewählte Zeile" und „Ladeverhalten", beide **einspaltig**: Dort schaltet jede Wahl bzw. jeder Schalter das Feld UNTER sich frei (Speicher/Bedarfsart, Ladegrenze, Einspeisehöhe) — nebeneinander verlöre das Paar seinen Zusammenhang. **Nicht** umgestellt: die Senkenliste (ein `Zeilenraster` mit sieben Spalten) und der Parallelverbund (eine `Mehrfachauswahl`); eine Liste ist kein Formularblock, und der Raster darf dort nicht hinein. |

**Probe.** Drei Fälle: `Quellsystem_und_Standort_stehen_im_Formularraster`,
`Kopf_und_Werte_stehen_im_Formularraster` und
`Senkenzeile_und_Ladeverhalten_stehen_im_einspaltigen_Formularraster` — der letzte
prüft ausdrücklich, dass **jeder** Raster des Dialogs einspaltig ist.

**Eine Zeile Stilblatt kam dazu** — der Unterblock „Formularraster — Paket P3" in
`epos-ui.css`: Eine `Herleitungszeile` als Rasterkind spannt über **alle** Spalten.
Sie gehört zu dem Feld ÜBER ihr („Vorgabe 0,6", „aus dem Kesselwirkungsgrad");
als gewöhnliches Rasterkind fiele sie im zweispaltigen Raster **neben** ein fremdes
Feld und läse sich wie dessen Erläuterung. Sonst kein CSS, keine Inline‑Stile.

# iU9 Welle 4 — Port der Kostenverwaltung und des Energieträgerkatalogs (Umsetzungsprotokoll)

> Muster: [`iU9_W3_Blazor_Port_Protokoll.md`](iU9_W3_Blazor_Port_Protokoll.md),
> [`iU9_W2_Blazor_Port_Protokoll.md`](iU9_W2_Blazor_Port_Protokoll.md) und
> [`iU9_W1_Blazor_Port_Protokoll.md`](iU9_W1_Blazor_Port_Protokoll.md) —
> Feldkarten-Abgleich je Maske, Abweichungsliste A‑n, Entscheidungen,
> Windows-Abnahmepunkte.
>
> Basis `ae1af82` (Branch `ios_migration`), Arbeitsstand 03.09.2026.
> Plan: Wellenplan iU9, Abschnitt C Zeile W4, E Priorität 6–8 und 11, F, G (R2/R3/R11).

---

## 1. Auftrag und Ergebnis

**Sieben WinForms-Masken → sieben Razor-Komponenten**, jede WinForms-Fassung im
selben Schritt gelöscht (Regel M1). Dazu **vier neue Bausteine**, **ein neuer
Kern-Controller**, **zwei Hüllen mit Datenseite** und die Umstellung von **sieben
Unterdialogen** aus den Wellen 1 bis 3 auf die neue Überlagerung.

Es ist die größte Welle bisher: Die beiden **Hosts** der Kostenseite fallen mit
ihren fünf Unterbausteinen auf einmal — 5 216 Zeilen WinForms.

| # | Maske (Zeilen) | Komponente | Hülle | Aufrufer nach dem Umbau |
|---|---|---|---|---|
| W4.1 | `ucVorlagenZeile` (377) | `EPOS.UI/Dialoge/Kosten/VorlagenZeile.razor` | im Wirt | `KostenKomponenteDialog` |
| W4.1 | `ucErtragBonus` (217) | `EPOS.UI/Dialoge/Kosten/ErtragBonus.razor` | `Views/Kosten/ErtragBonusGaben.cs` | `KostenKomponenteDialog` |
| W4.2 | `Form_KostenKomponente` (918) | `EPOS.UI/Dialoge/Kosten/KostenKomponenteDialog.razor` | `Views/Kosten/KostenKomponenteHuelle.cs` | `MDIMainForm.cs:72`, `UcBkKosten.cs:1165`, `KostenKnoepfe.cs` (Invest/Betrieb), `Wizard_WPItem.cs:577` |
| W4.3 | `ucStromAufschlaege` (705) | `EPOS.UI/Dialoge/Kosten/StromAufschlaege.razor` | im Wirt | `EnergietraegerEinstellungen` |
| W4.3 | `ucBrennstoffBestandteile` (863) | `EPOS.UI/Dialoge/Kosten/BrennstoffBestandteile.razor` | im Wirt | `EnergietraegerEinstellungen` |
| W4.4 | `ucFuelSettings` (2 103) | `EPOS.UI/Dialoge/Kosten/EnergietraegerEinstellungen.razor` | `Views/Kosten/EnergietraegerHuelle.cs` | `EnergietraegerDialog` |
| W4.4 | `Form_Energietraeger` (535) | `EPOS.UI/Dialoge/Kosten/EnergietraegerDialog.razor` | dieselbe Hülle | `MDIMainForm.cs:88`, `UcBkKosten.cs:1187`, `KostenKnoepfe.cs:56` |

**Ohne Nutzer geblieben und mitgelöscht:** `Views/Kosten/EinstiegsKarte.cs`
(Nachfolge `Kachel`) und `Views/Kosten/SectionPanel.cs` (Nachfolge
`Gruppenkopf`). **`Views/Kosten` führt seither keine Designer-Maske mehr.**

**Commits** (ein Commit je Nummer, Reihenfolge des Plans):

```
6c3cbc5  iU9-W4.0  Bausteine Ueberlagerung, Rueckfrage und Zeilenraster
3db98e0  iU9-W4.1  VorlagenZeile und ErtragBonus
e0b63be  iU9-W4.2  KostenKomponenteDialog und seine Huelle
4527d66  iU9-W4.3  StromAufschlaege und BrennstoffBestandteile
b43e8fd  iU9-W4.4  EnergietraegerDialog samt Traegerkarte
09ecd37  iU9-W4.5  Ressourcen-Sammelnachtrag (50 Schluessel, de + en + Designer)
45246be  iU9-W4.6  Formularkarte-Tests (zwei Pruefmuster, neue Zaehler)
(dieses Protokoll)  iU9-W4.7
```

---

## 2. Bauweise

### 2.1 Die drei neuen Bausteine (W4.0)

**`EPOS.UI/Bausteine/Ueberlagerung.razor`** (Bausteinlücke 7) — der modale
Bereich INNERHALB der Komponente. Er ist der eigentliche Ertrag dieser Welle.

Bis hierher wich jeder Blazor-Dialog, der einen zweiten braucht, aus: Der
Kostenfaktor-Katalog legt inline an (W1.5, A‑13), der Emissionskatalog zeigt
seine beiden Untereditoren als eingerückte Blöcke (W3.3, A‑10). Grund war beide
Male derselbe — ein zweites Fenster hieße unter Windows eine zweite
`BlazorWebView` über der ersten: 60–120 MB, 100–300 ms Aufbau und eine
Fokusreihenfolge, die niemand mehr erklären kann (Risiko R2 des Wellenplans).

Die Überlagerung löst das ohne ein Fenster: Abdunkelung, `role="dialog"`,
`aria-modal`, Esc schließt, und eine **Fokusfalle aus zwei leeren, fokussierbaren
Feldern** hält den Tabulatorzyklus im Bereich — ohne dass `EPOS.UI` eine
JS-Schicht bräuchte. Ein Klick auf die Abdunkelung schließt **nicht**: Wo ein
Unterdialog schreibt, wäre ein versehentlicher Klick daneben kein Abbrechen,
sondern ein Verlust (dieselbe Überlegung wie A‑7 aus B5b). Auf iOS ist diese
Bauform ohnehin die einzige — dort gibt es keine zweiten Fenster (iL5).

**`EPOS.UI/Bausteine/Rueckfrage.razor`** (Bausteinlücke 8) — Ja/Nein/Abbrechen
über der Überlagerung, der Ersatz für die rund fünfhundert MessageBox-Rückfragen
des Bestands. **Drei** Antworten, weil `MessageBoxButtons` sowohl `YesNo` als
auch `YesNoCancel` kennt: `true`, `false`, `null`. Enter ist nicht belegt, Esc
antwortet wie der Abbrechen- bzw. Nein-Knopf, und der Erstfokus liegt auf dem
Bereich, nicht auf „Ja".

**`EPOS.UI/Bausteine/Zeilenraster.razor`** (Bausteinlücke 6, dritter Teil) —
Spaltenkopf, Bearbeitungszeilen, Abschlusszeile und Summenfuß. Es ist bewusst
**kein zweites QuickGrid**: Im Positionsraster der Kostenverwaltung IST jede
Zeile eine kleine Maske mit sieben einander bedingenden Feldern (im Bestand
`ucVorlagenZeile`, 377 Zeilen). Die Zeile bleibt deshalb eine Komponente; das
Raster liefert nur die gemeinsamen Spuren. Kopf und Zeilen fluchten über ein
CSS-Raster mit `display:contents` statt über gerechnete Pixelbreiten
(`z.Width = pnlZeilen.ClientSize.Width - 4`, mindestens 928).

**`EPOS.UI/Bausteine/Mehrfachauswahl.razor`** (Bausteinlücke 11, W4.4) — Liste
mit Haken samt „Alle"/„Keine", der Ersatz für die `CheckedListBox`. Bei zwanzig
Trägern ist das der Unterschied zwischen einem Klick und zwanzig.

### 2.2 Der Nachzug aus Welle 3 (A‑10)

Die beiden Untereditoren des Emissionskatalogs stehen seit W4.0 in einer
Überlagerung statt in einem eingerückten Block — also wieder da, wo der
WinForms-Vorläufer sie hatte (je ein zur Laufzeit gebautes `Form`), nur ohne die
zweite WebView. Ihre neun Tests greifen auf `.epos-ueberlagerung-inhalt` statt
`.epos-editorblock` zu; am geprüften Verhalten ändert sich nichts, und `Esc`
schließt weiterhin zuerst den Editor.

**Nicht nachgezogen: A‑17 aus Welle 3.** Der Auftrag nennt sie neben A‑10; sie
betrifft aber die **drei Abschnitte statt dreier Reiter** im Kostenprofil. Dafür
braucht es den Baustein `Reiter` (Bausteinlücke 10, Welle 5), nicht die
Überlagerung — ein modaler Bereich ist kein Reiter. Die Welle hat denselben
Verzicht deshalb zweimal mehr geleistet: Auch `KostenKomponenteDialog` und
`EnergietraegerEinstellungen` stellen ihre Reiterinhalte untereinander
(A‑2, A‑12). Mit Welle 5 fallen alle drei Stellen gemeinsam.

### 2.3 Der neue Kern-Controller (W4.4)

`EPOS.Kern/Controller/EnergietraegerPreisCtrl.cs` — **neun SQL-Anweisungen**, die
bis Welle 4 in `ucFuelSettings` standen (Regel F5):

| Methode | Vorläufer | Was sie tut |
|---|---|---|
| `Umrechnungen` | `GetConversions` | die Preisbasen eines Brennstoffs |
| `ProjektpreisLesen` | `GetProjectPrice` | die Übersteuerung; jede Spalte darf NULL sein (Ä‑BK3) |
| `Zieleinheit` | `GetTargetUnitByConversionId` | die Zieleinheit einer Umrechnungszeile |
| `UmrechnungsId` | `GetConvID` | Brennstoff + von + nach → Id |
| `Historie` | `LoadHistory` | die Preisstände, jüngster zuerst |
| `ImProjekt` | `Form_Energietraeger.SpeichereOffenes` | ist der Träger dem Projekt zugeordnet? |
| `LeistungsModus`/`-Schreiben` | `LiesLeistungsModus`/`SchreibeLeistungsModus` | Katalogsache je Träger (KD4 § 7.1) |
| `RegelnSpeichern` | `SpeichereRegeln` | die Umrechnungsregeln (K3), MAX+1 nach ADR‑001 |
| `Katalogwerte` / `HistorieSchreiben` / `Projektwerte` | `SpeichereWerte`, drei Zweige | Ä9, Historienstand, Upsert |

**Wortgleich übernommen** — dieselben Spalten, dieselbe Rundung auf vier
Nachkommastellen, dieselbe Reihenfolge (erst Historie, dann Projekt-Settings),
damit der Referenzlauf sie nicht bemerkt. Zwei Änderungen an der Bauform: Der
`dynamic`-Rückgabewert von `GetProjectPrice` ist ein benannter Typ geworden, und
die eine `RecordSet`-Abfrage mit Zeichenkettenverkettung hat einen Parameter
bekommen (SQL-Prüfer, `BETRIEB_SQLITE.md` § 6).

### 2.4 Die beiden Hüllen

Muster durchweg `BhkwWirtschaftlichkeitHuelle`/`KostenprofilHuelle`: laden mit
denselben Controllern und in derselben Reihenfolge wie zuvor der
Maskenkonstruktor, rechnen mit denselben Rechnern und schreiben über Rückrufe.

| Hülle | Lädt / rechnet | Delegaten |
|---|---|---|
| `KostenKomponenteHuelle` | `KostenVorlagenCtrl`, `KostenProjektPositionenCtrl`, `ProjektEnergietraegerCtrl`, `KostenSummenCtrl`, `BemessungKatalog` | `Laden` (Kontext → Stand), `Nachziehen`, `Summen`, `Speichern`, `PositionNeu`/`-Loeschen`, `IstPflicht`, dazu je Unterdialog `…Gaben` und `…Fertig` |
| `EnergietraegerHuelle` | `EnergietraegerPreisCtrl`, `EnergietraegerKatalogCtrl`, `KostenSummenCtrl`, `EnergieEinheitenPruefung`, `StromAufschlagCtrl`, `BrennstoffBestandteilCtrl`, `EmissionenCtrl`, `GesetzKatalog`, `EmissionsFaktorLader` | `TraegerLaden` (Träger → Ansicht), `Nachrechnen`, `PreisbasisGewechselt`, `RegelNeu`, `RegelAbschalten`, `Speichern`, die Katalogpflege und fünf `…Gaben` |

**Eine Antwort statt fünf Methoden.** `Kontext_Geaendert` mit seinen
Folgemethoden (`KopfAnzeigen`, `VariantenLaden`, `RasterAufbauen`,
`SummenAnzeigen`, `ErtragReiterSteuern`) wird zu einem
`KostenKomponenteStand`, den die Hülle zu einem `KostenKomponenteKontext`
liefert. Dasselbe beim Träger: `EnergietraegerAnsicht` trägt Stand,
Summenzeilen der Preisblöcke, Arbeitspreis in ct/kWh, Schnellwahlsätze und
Kartenstatus.

**Warum überhaupt ein Bündel.** Eine `BlazorDialogForm` setzt die Parameter
**einmal**, beim Aufbau. Alles, was sich während des Dialogs ändert, muss die
Komponente deshalb selbst halten und über einen Delegaten nachfragen.

### 2.5 Sieben Unterdialoge, ein Fenster

Damit steht jeder Unterdialog der Kostenseite im selben Fenster wie sein Wirt:

| Wirt | Unterdialog | Herkunft | vorher |
|---|---|---|---|
| Kostenverwaltung | Worst/Best | W1.3 | eigene `BlazorDialogForm` |
| Kostenverwaltung | Zeileneditor | W1.1 | eigene `BlazorDialogForm` |
| Kostenverwaltung | Namensabfrage | W1.2 | eigene `BlazorDialogForm` |
| Kostenverwaltung | Übernahme | W1.4 | eigene `BlazorDialogForm` |
| Kostenverwaltung | Kostenfaktor-Katalog | W1.5 | eigene `BlazorDialogForm` |
| Energieträger | Kostenprofil | W3.4 | eigene `BlazorDialogForm` |
| Energieträger | Spotpreis-Import | W3.2 | eigene `BlazorDialogForm` |
| Trägerkarte | saisonale Sätze | W3.1 | eigene `BlazorDialogForm` |
| Trägerkarte | Emissionskatalog | W3.3 | eigene `BlazorDialogForm` |

Die Hüllen der Wellen 1 bis 3 liefern dafür statt eines Fensters ihren
**Parametersatz**: `VorlagenUebernahmeHuelle.Gaben`,
`KostenfaktorKatalogHuelle.Gaben`, `NamensDialogHuelle.Gaben`,
`KostenprofilHuelle.Gaben`, `SpotpreisImportHuelle.Gaben`,
`LeistungspreisReiheHuelle.Gaben` und `EmissionskatalogHuelle.Gaben`. Der
Emissionskatalog gibt zusätzlich seine **Auswertung** mit (`Aufruf.Auswerten`),
weil sein Ergebnis drei Eigenschaften trägt und beim Schließen die globale
Modus-Vorgabe schreibt.

`NamensDialogHuelle` behält daneben seine drei Fensterwege — die 28 Aufrufer aus
Welle 2 öffnen weiterhin ein eigenes Fenster, weil ihre Wirte WinForms-Masken
sind.

---

## 3. Feldkarten-Abgleich

Der Abgleich ist **als Test ausgeführt** (`EPOS.UI.Tests/Dialoge/*Tests.cs`),
nicht als einmalige Messung: Je Komponente prüft ein Test den Feldbestand, ein
zweiter die Beschriftungen. Fällt ein Feld weg, wird der Test rot. Die Karten
wurden vor Beginn frisch gezogen (`Werkzeuge/Formularkarte`, Stand `ae1af82`);
die Laufzeitfelder sind von Hand aus der `.cs` ergänzt (Regel F1).

| Maske | Soll (Feldkarte) | Ist (Komponente) | Deckung |
|---|---|---|---|
| `ucVorlagenZeile` | 8 Zeilen: ✏️, 🗑️, Bezeichnung, Bemessung, Satz, Betrag (🔗), Nutzung, ± | 2 Knöpfe · `Textfeld` · `Auswahlfeld` · 2 `Zahlenfeld` · Betragszelle mit Kette · ±-Knopf | **8/8** |
| `ucErtragBonus` | 12 Zeilen: Leersatz, 4 KWKG-Zeilen, PV (Liste, Knopf, Erklärung), Dauer, Steuern, Pflegeorte (Knopf, FK7) | 4 `Gruppenkopf` für BHKW, 1 für PV, `Herleitungszeile`n, 2 Knöpfe, `Auswahlfeld` | **12/12** |
| `Form_KostenKomponente` | 28 Zeilen: Titel, Untertitel, ReadOnly-Hinweis, Komponente, Invest/Betrieb, Variante + 3 Knöpfe, Banner + ✕, 7 Spaltenköpfe, 3 Knöpfe, 2 Summen, OK/Speichern/Abbrechen, Ertragshinweis | dieselben; `Optionsgruppe` für die Kategorie, `Zeilenraster` für den Spaltenkopf, `Warnbanner` für Banner und ReadOnly | **28/28** |
| `ucStromAufschlaege` | 27 Zeilen: Modus (2), 5 × (Schalter + Feld + Einheit), 2 Schnellwahlknöpfe, Summe, Override + Einheit, Rest, Vergütung PV/BHKW + Einheiten | 1 `Optionsgruppe`, 5 `epos-preiszeile`, 2 Schnellwahlknöpfe, 3 `Zahlenfeld`, Summen- und Restzeile | **27/27** |
| `ucBrennstoffBestandteile` | 26 Zeilen: Modus (2), 4 × (Schalter + Feld + Einheit), 4 Schnellwahlknöpfe + Beschriftung, Summe, Arbeitspreis + Wert + Einheit, Rest, Übernahmeknopf, Quelle | dieselben; die Quellzeile als `Herleitungszeile`, der Arbeitspreis als `Kohaerenzzeile` | **26/26** |
| `ucFuelSettings` | 26 Kartenzeilen **+ 21 Laufzeitfelder** | vier Abschnitte: Preise (7 Felder + Formel), Umrechnung (`Raster` bearbeitbar + Knopf + 2 Zeilen), Emissionen (Modus, `Raster`, Summe, Hinweis, Knopf), Historie (Datum, Speichern, `Raster`) | **26/26 + 21** |
| `Form_Energietraeger` | 7 Zeilen: Kopftitel, Kontext, Listentitel, Liste, Schließen/OK/Speichern | dieselben; dazu die zur Laufzeit gebauten Leisten | **7/7 + 7** |

**Die 21 Laufzeitfelder von `ucFuelSettings`** stehen in keiner Feldkarte — der
Designer kennt sie nicht:

| Feld im Vorläufer | Herkunft | Ziel in der Komponente |
|---|---|---|
| `cmbLeistungsModus`, `btnSaisonSaetze`, `lblLeistungsHinweis` | `BaueLeistungspreisZusatz` (KD4/FK6a) | `Auswahlfeld` + Knopf + `Herleitungszeile` in der Leistungspreiszeile |
| `dgvRegeln` (5 Spalten), `btnRegelNeu`, `lblEffektiv`, `lblVerstoss`, Titel | `BaueUmrechnungsblock` (K3) | `Raster` mit `Bearbeitbar`, Knopf, Statuszeile, `Warnbanner` |
| `_lblEffektivpreis`, `_chkAufschlagAnwenden` | `BaueAufschlagsblock` (Ä16) | `Kohaerenzzeile` + `Schalter` |
| `tabDetails`/`tabPreise`/`tabEmissionen` | `BaueEmissionsReiter` (E3) | vier `Gruppenkopf` (A‑12) |
| Modusgruppe, 4 Spaltenköpfe, Zeilenfelder je Art, Summe, Hinweis, Verwaltenknopf | `BaueEmissionsInhalt` (E3 § 4.1) | `Optionsgruppe`, `Raster` mit `Zahlenfeld` je Zeile, `Kohaerenzzeile`, `Warnbanner`, Knopf |

**Die 7 Laufzeitfelder von `Form_Energietraeger`**: die beiden Einstiegskacheln
(Ä1), der editierbare Stammkopf (Bezeichnung, Gruppe, Übernehmen — Ä9) und die
zwei Leisten (Katalog: Neu/Variante/Löschen; Projekt: Übernehmen/Entfernen —
Ä10).

**Kein Feld einer Karte fehlt.**

---

## 4. Abweichungen (mit Begründung)

| # | Abweichung | Begründung |
|---|---|---|
| **A‑1** | W4.1: Die Abschlusszeile legt auf **Knopfdruck** an, nicht beim Verlassen des Namensfeldes | `ucVorlagenZeile.Feld_Leave` legte an, sobald das Feld mit gefülltem Namen verlassen wurde. In einer WebView gibt es kein „Leave" mit derselben Bedeutung (`@oninput` meldet jede Taste), und ein Anlegen, das beim Weiterklicken nebenbei geschieht, sieht niemand kommen. Der ＋-Knopf ist gesperrt, solange kein Name steht |
| **A‑2** | W4.2/W4.4: **Abschnitte statt Reitern** — „Kosten"/„Ertrag/Bonus" bzw. „Preise & Umrechnung"/„Emissionen" stehen untereinander | Einen Reiter-Baustein gibt es erst in Welle 5 (Bausteinlücke 10); dieselbe Entscheidung wie A‑17 in Welle 3. Der Ertragsabschnitt erscheint weiterhin nur bei BHKW und Photovoltaik (FK5) — der Vorläufer ENTFERNTE die Reiterseite, hier wird sie gar nicht erst gebaut |
| **A‑3** | W4.2: Die Zeile **rechnet und schreibt nicht mehr selbst** | Im Bestand stand `KopplungAnwenden` in `ucVorlagenZeile`, seine Wahrheit (die Bemessung) kam aber vom Wirt; und seit Ä19 gilt in beiden Kontexten die deferred-Semantik. Die Zeile zeigt an und meldet, der Wirt entscheidet — eine Wahrheit statt zweier |
| **A‑4** | W4.2: Die **Summen kommen aus dem Stand**, nicht aus der Datenbank | `SummenAnzeigen` las die Zeilenobjekte, nicht die DB — das bleibt so. Würde der Dialog nach jeder Eingabe neu laden, verlöre er genau die ungespeicherte Änderung, die Ä12/Ä19 schützen |
| **A‑5** | W4.2: Die drei MessageBox-Rückfragen werden **Rückfrage** bzw. **Warnbanner** | Hausregel `EPOS.UI/CLAUDE.md`. Die zwei echten Ja/Nein-Fragen (Position löschen, Variante löschen) laufen über den neuen Baustein, die zwei Hinweise (Pflichtposition, Standardvorlage) über ein Banner — sie waren nie Fragen, sondern Erklärungen |
| **A‑6** | W4.2: Der Kostenfaktor-Katalog fragt weiterhin über **`Dienste.Dialog`** vor dem Löschen | Sein `Rueckfrage`-Delegat stammt aus W1.5 und ist dort mit dreizehn Tests belegt. Ihn jetzt auf den neuen Baustein zu ziehen, hieße den Dialog der Welle 1 mitten in Welle 4 umzubauen — er zieht beim nächsten Anfassen nach (offener Punkt W4‑O2) |
| **A‑7** | W4.1: Der Sprung in den Gesetzeskatalog läuft über die **Sprungbrücke** (W2.2) | `ucErtragBonus.btnGesetze_Click` öffnete `Form_Gesetzesparameter` direkt. Damit hat `Sprungziel.Gesetzesparameter` seinen ersten Aufrufer — offener Punkt **W2‑O6 erledigt** |
| **A‑8** | W4.1: Der **PV-Vergütungsdialog** bleibt ein zweites Fenster | Er ist selbst eine Blazor-Hülle (W2.4). Zwei WebViews übereinander sind Risiko R2; der Sprung bleibt deshalb nachgelagert, bis Welle 5 den Wirtschaftlichkeitsreiter anfasst (W4‑O3) |
| **A‑9** | W4.3: Summen-, Rest- und Effektivzeile kommen als **fertiger Text** aus der Hülle | Die Formeln stehen in der Engine (`StromAufschlagCtrl.AlsAufschlagssatz`, `BrennstoffBestandteilCtrl`), die Einheitenketten der Schnellwahl im Katalogleser. Im Bestand rechnete jeder Block seine Texte selbst — es soll sie nur einmal geben |
| **A‑10** | W4.3: Der empfohlene Stromsteuersatz bleibt **fett, nicht farbig** | Unverändert zum Bestand (BW4, Befund B3) und aus demselben Grund: Die Warnfarbe ist im selben Block für den negativen Rest belegt |
| **A‑11** | W4.4: Die **Trägerliste ist eine Knopfliste**, kein Listenfeld; Gruppenköpfe sind Absätze | Eine `ListBox` mit nicht wählbaren Einträgen gibt es im Web nicht; der Vorläufer sprang deshalb beim Klick auf einen Kopf zum nächsten Träger weiter (`lstTraeger_SelectedIndexChanged`). Ein Kopf, der gar kein Bedienelement ist, braucht diesen Sprung nicht — und die Sprachausgabe liest ihn als Überschrift statt als Option |
| **A‑12** | W4.4: Die Preishistorie ist ein **Raster mit fertigen Texten** | `dgvHistory` formatierte über `DefaultCellStyle` (`dd.MM.yyyy`, `N2`). Ein QuickGrid formatiert nicht; die Hülle liefert die sechs Spalten deshalb als Zeichenketten — dieselbe Darstellung, eine Schicht weiter unten |
| **A‑13** | W4.4: Der **Modus-Schalter der Emissionen** wirkt erst mit „Speichern" | Der Vorläufer schrieb ihn in `_emissionen.Modus` und ließ ihn dort bis `EmissionenSpeichern` liegen — dieselbe Reihenfolge, nur ohne den Zwischenschritt über zwei `RadioButton.CheckedChanged` |
| **A‑14** | W4.4: Eine unbrauchbare Zahl im **Faktorfeld** wird nicht übernommen und **meldet nicht** | Der Vorläufer setzte die Zelle zurück und schrieb einen roten Hinweis. Ein `Zahlenfeld` färbt sich und meldet nichts nach außen (Hausregel, A‑8 aus W1); geblieben ist die zweite Bedingung: ein Faktor muss größer null sein |
| **A‑15** | W4.4: Der **Riegel** (§ 4.3) sitzt jetzt in der Hülle | `EnergieEinheitenPruefung.DarfAbschalten` entscheidet unverändert; neu ist nur, dass die Komponente ihn über einen Delegaten fragt, statt ihn selbst zu rufen. Es gibt keine zweite Fassung der Fachregel, nur einen zweiten Leser |
| **A‑16** | W4.4: Der **Kurztext der Herkunftsspalte** steht am Text, nicht in einem Tooltip-Objekt | `AutoEllipsis` + `ToolTip` gibt es im Web nicht; `title` am `<span>` tut dasselbe, und CSS kürzt mit Auslassungspunkten |
| **A‑17** | W4.4: Ein **abgelehntes Speichern** (K3-Riegel) meldet als Warnbanner statt als MessageBox | Hausregel; der Text ist derselbe (`KOSTEN_UMRECHNUNG_SPEICHERN_ABGELEHNT`), und der rote Hinweis im Regelblock steht ohnehin schon |
| **A‑18** | W4.4: Die **Kataloguebernahme** ist eine Mehrfachauswahl mit „Alle"/„Keine" | Der Vorläufer baute dafür ein `Form` zur Laufzeit mit einer `CheckedListBox`. Die beiden Sammelknöpfe sind neu — die `CheckedListBox` konnte das nicht, und bei zwanzig Trägern ist es der Unterschied zwischen einem Klick und zwanzig |
| **A‑19** | W4.4: „Löschen" und „Entfernen" nennen ihren Grund als **Warnbanner** | Vier MessageBox-Stellen werden zwei Rückfragen und zwei Banner. Die Rückfragen behalten ihren Wortlaut |
| **A‑20** | Alle: **Enter ist nicht belegt, Esc schließt** | A‑7 aus B5b. In beiden Großdialogen schreiben fast alle Knöpfe sofort; Esc geht zuerst an eine offene Überlagerung, die die Taste selbst abfängt |

**Ein Befund am Rand.** `Form_Energietraeger.KatalogUebernahme` und der
Emissionsteil von `ucFuelSettings` lasen 27 Ressourcenschlüssel, die es im
Katalog **nie gab** (`KDLG_EM_*`, `KDLG_ANLAGE_*`, `KDLG_ET_TAB_*`,
`KDLG_BTN_OK`, `KDLG_ET_GRUPPE_SONSTIGE`). Gezeigt wurde jedes Mal der deutsche
Rückfall — der Emissionsteil der Trägerkarte und die beiden Anlagenvermerke des
Projektmodus waren auf Englisch nie zu sehen. Sie sind jetzt übersetzt (§ 5).

---

## 5. Texte

**50 neue Schlüssel** in `EPOS.Kern/MyResource/Resource.resx`,
`Resource.en-US.resx` und — von Hand, weil hier kein Visual Studio läuft —
`Resource.Designer.cs` (alphabetisch zwischen den Nachbarn, im Muster der
erzeugten Datei; die Änderung ist in allen drei Dateien rein additiv):

| Präfix | Zahl | Wofür |
|---|---|---|
| `KKOMP_*` | 7 | Kostenverwaltung: Bannerkreuz, drei Kurztexte, Ja/Nein, Rastername |
| `ETV_*` | 22 | Trägerkarte: Feld- und Spaltenbeschriftungen, Historie, Leersatz, Übernahmefrage |
| `KDLG_EM_*` | 13 | der Emissionsteil — Modus, Ortsvermerk, vier Spalten, Summe, F3-Hinweis, Katalogknöpfe |
| `KDLG_ET_*`, `KDLG_ANLAGE_*`, `KDLG_BTN_OK` | 8 | die zwei Reiterüberschriften, „Sonstige", die zwei Anlagenvermerke (Ä20), das OK der Kostenverwaltung |

**Wiederverwendet statt neu angelegt:** alle `KDLG_*` der Kostendialoge (131
Schlüssel), die 13 `PREIS_*` des Aufschlagsblocks, die 14 `BB_*` der
Preiszerlegung, die 11 `PREIS_ST_*` der Schnellwahl, alle
`KOSTEN_UMRECHNUNG_*`, `KDLG_LP_*`/`KDLG_LPR_*`, `KDLG_ERTRAG_*`,
`KDLG_EFFEKTIVPREIS`, `KDLG_AUFSCHLAG_ANWENDEN`, `KCASE_*`, `VPOS_*`, `KUEB_*`,
`KFAK_*`, `NAMD_*`, `KPROF_*`, `PREIS_PROFIL_*`, `PREIS_IMPORT_*`, `EMK_*`,
`ALLG_BTN_OK`/`_ABBRECHEN`.

**Die Texte der beiden Preisblöcke reicht die Hülle als Satz durch.**
`EnergietraegerEinstellungen` nimmt `AufschlagTexte` und `BestandteilTexte` und
splattet sie auf die verschachtelten Bausteine (`@attributes`). Einzeln
durchgereicht wären es dreißig Parameter auf zwei Ebenen; so bleiben die
`PREIS_*`- und `BB_*`-Schlüssel des Bestands in Gebrauch, ohne dass die
Komponente sie kennt.

**Zugriff** über `Resource.ResourceManager.GetString` mit deutschem Rückfall im
Code (B5b‑O4) — die Hülle setzt die Texte, die Komponente trägt den deutschen
Literaltext als Parametervorgabe.

**Keine Übersetzung ist verloren gegangen.** Keine der sieben Masken war
lokalisiert (`ApplyResources`); `Form_KostenKomponente`, `Form_Energietraeger`
und `ucFuelSettings` führten zwar eine `.resx`, die trägt aber ausschließlich
Designer-Standardeinträge. Die Zahl der lokalisierten Masken bleibt deshalb bei
**59**.

**`help_mapping.txt` bleibt unverändert.** Die zwei Zeilen
`Form_KostenKomponente.btn_Help` und `Form_Energietraeger.btn_Help` gelten
weiter — der Schlüssel benennt die Wikiseite, nicht die Klasse (dasselbe
Vorgehen wie seit iU8‑9).

**`Allgemein/KI/HilfeKontext.cs`:** die zwei Einträge der gelöschten Hosts
entfernt — jeweils im Commit ihrer Maske (Regel F10). Die fünf Unterbausteine
standen dort nie.

---

## 6. WinForms-Seite

**Gelöscht** (17 Dateien):

```
Views/Kosten/Form_KostenKomponente.{cs,Designer.cs,resx}
Views/Kosten/ucVorlagenZeile.{cs,Designer.cs}
Views/Kosten/ucErtragBonus.{cs,Designer.cs}
Views/Kosten/Form_Energietraeger.{cs,Designer.cs,resx}
Views/Kosten/ucFuelSettings.{cs,Designer.cs,resx}
Views/Kosten/ucStromAufschlaege.{cs,Designer.cs}
Views/Kosten/ucBrennstoffBestandteile.{cs,Designer.cs}
Views/Kosten/EinstiegsKarte.cs      (ohne Nutzer; Nachfolge Kachel)
Views/Kosten/SectionPanel.cs        (ohne Nutzer; Nachfolge Gruppenkopf)
```

**Kopiert** (5 Dateien) — `Form_KostenKomponente.*` und `ucVorlagenZeile.*` nach
`Werkzeuge/Formularkarte.Tests/Pruefmuster/Kosten/`: An der ersten hingen acht
Testbezüge des Werkzeugs (darunter der Stapellauf-Anker), die zweite ist die
einzige kleingeschriebene Maske, die der Bestand je geführt hat (§ 7.3).

**Neu** auf der Windows-Seite: `Views/Kosten/KostenKomponenteHuelle.cs`,
`Views/Kosten/ErtragBonusGaben.cs`, `Views/Kosten/EnergietraegerHuelle.cs`.

**Umgebaut** (Fenster → Parametersatz): `VorlagenUebernahmeHuelle`,
`KostenfaktorKatalogHuelle`, `KostenprofilHuelle`, `SpotpreisImportHuelle`,
`LeistungspreisReiheHuelle`, `EmissionskatalogHuelle`; `NamensDialogHuelle`
bekommt `Gaben` **zusätzlich** zu seinen drei Fensterwegen.

**Keine Typverwendung ist übrig:**

```
git grep -nE "(new|typeof|:)\s*(Form_KostenKomponente|Form_Energietraeger|ucFuelSettings|
    ucBrennstoffBestandteile|ucStromAufschlaege|ucVorlagenZeile|ucErtragBonus)\b" \
    -- 'WindowsFormsApplication1/*.cs' 'EPOS.UI/*.razor' 'EPOS.Kern/*.cs'
→ 0 Treffer (ohne Kommentare und Prüfmuster)
```

Restfundstellen der alten Namen sind ausschließlich (a) `HilfeSchluessel`-Zeichen­
ketten (`"Form_X.btn_Help"` — Schlüssel des Hilfekatalogs, § 5), (b) Kommentare,
die die Herkunft nennen, und (c) die Prüfmusterbezüge der Formularkarte-Tests.

---

## 7. Nachweise

### 7.1 Build

```
dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental
→ 0 Fehler, 22 Warnungen
```

Basis (`ae1af82`): 26. **WFO1000 sinkt von 20 auf 16** — die vier Fundstellen der
gelöschten Karten-Controls (`EinstiegsKarte`, `SectionPanel` und die zwei
Eigenschaften von `ucFuelSettings`) sind weg; der Rest ist unverändert
(2 × CS0108, 2 × CS0109, 1 × WFO0003, 1 × CA2255).

### 7.2 Tests

```
dotnet test WP-Plan.Kern.slnf -c Release
→ EPOS.Kern.Tests       37 grün
  KiKern.Tests         450 grün
  SpeicherEngine.Tests 337 grün
  EPOS.UI.Tests        528 grün   (393 vorher, 135 neu)
  ────────────────────────────────
  1 352 grün, 0 rot    (1 217 vorher)
```

Die 135 neuen bunit-Tests:

| Datei | Tests | Prüft |
|---|---|---|
| `Bausteine/UeberlagerungTests.cs` | 12 | geschlossen leer, `role`/`aria-modal`, Inhalt, Titel, ✕ meldet beides, ohne Titel keine Kopfzeile, Esc, Hintergrundklick (mit und ohne Schalter), zwei Fokusfallen, Bereich fokussierbar |
| `Bausteine/RueckfrageTests.cs` | 9 | geschlossen leer, Frage als `role="alert"`, zwei bzw. drei Knöpfe, Ja/Nein/Abbrechen, Esc in beiden Fassungen, Titel ohne Kreuz |
| `Bausteine/ZeilenrasterTests.cs` | 9 | Spaltenkopf, Wahlspalte, Spaltenmaß als Rasterspur, Zeilen, Abschlusszeile (mit und ohne), Summenfuß samt Hervorhebung, `role="table"` |
| `Dialoge/VorlagenZeileTests.cs` | 18 | Feldbestand (7 Zellen), drei Knöpfe je Kontext, Werte, Nutzungsdauerspalte, Kette, Betrag nie eingebbar, zwei Kurztexte, drei Meldungen, Feldänderungen, Schreibschutz, Abschlusszeile (Platzhalter, Anlegesperre, gesperrte Felder) |
| `Dialoge/ErtragBonusTests.cs` | 11 | vier BHKW-Gruppen in Reihenfolge, die fünf fertigen Sätze, PV-Gruppe, Leersatz, Vorwahl (mit und ohne), Knopfmeldung, Sperre ohne Projekte, Katalogknopf nur mit Brücke, Sprung samt Neulesen |
| `Dialoge/KostenKomponenteDialogTests.cs` | 31 | Feldbestand, sieben Spaltenköpfe, Banner mit Kreuz, Variantenzeile je Kontext, Auslieferungsvorlage, Summenfuß, Zeilenzahl, drei Kontextwechsel, Vorwahl, Feldänderung ohne Schreiben, Speichern/OK/Abbrechen, Anlegen (Zeile und Knopf), Löschrückfrage (ja/nein), Pflichtposition, fünf Überlagerungen, Variantenanlage samt Namensmeldung, Standardvorlage, Ertragsabschnitt, Esc/Enter, Hilfeschlüssel |
| `Dialoge/PreisbloeckeTests.cs` | 18 | Strom: Feldbestand, Beschriftungen, Override-Modus, Modusmeldung, Wertänderung, Schnellwahl samt Hervorhebung und Herkunft, Summen-/Restzeile (negativ und positiv). Brennstoff: Feldbestand, gesperrter Satz mit Grund, Nullsemantik, zwei Schnellwahlwege, Übernahmeknopf je Modus, Arbeitspreiszeile, beidseitige Schreibbarkeit |
| `Dialoge/EnergietraegerDialogTests.cs` | 27 | Feldbestand, Liste mit Gruppenköpfen, Vorwahl, Knopfleiste je Kontext, Stammkopf, Stromkarten (mit und ohne Kostenprofil), vier Abschnitte der Karte, Träger ohne Heizwert bzw. ohne Leistungspreis, Verstoßbanner, Rückfall ohne Artenkatalog, nur lesende Emissionszeile, Trägerwechsel, Stammkopfmeldung, Löschrückfrage mit Grund, Kataloguebernahme (leer, Wahl, Weitergabe), zwei Unterdialoge, Speichern/Ablehnung/Abbrechen, Esc/Enter, Hilfeschlüssel |

Die Tests mit Zahlen in der Anzeige pinnen `de-DE` wie `SpeichernLeisteTests` —
die CI-Läufer laufen englisch.

### 7.3 Formularkarte

```
dotnet build Werkzeuge/Formularkarte/Formularkarte.sln -c Release  → 0 Fehler, 0 Warnungen
dotnet test  Werkzeuge/Formularkarte/Formularkarte.sln -c Release  → 122 grün (121 vorher)
```

Acht Testbezüge hingen an `Form_KostenKomponente` (Risiko R8), darunter der
**Stapellauf-Anker**. Umgehängt auf `Form_Heizkessel` — eine über die Startseite
erreichbare Maske, die bis Welle 6 im Bestand bleibt. Auch der Stapellauf über
einen Fachordner läuft dorthin: **`Views/Kosten` führt keine Designer-Maske
mehr.**

Zwei neue Prüfmuster: `Form_KostenKomponente` (sechstes; Beleg für ein
`TabControl`, das eine Reiterseite ZUR LAUFZEIT entfernt) und `ucVorlagenZeile`
(siebtes; die einzige kleingeschriebene Maske des Bestands und damit der einzige
Beleg dafür, dass der Razor-Schreiber den Anfangsbuchstaben groß zieht, RZ10011).
Das zweite wandert ausnahmsweise mit **zwei** Dateien: Ein `UserControl`, dessen
Texte im Code stehen, führt keine `.resx`. `PruefmusterTests` bekommt dafür eine
dritte `Theory`.

Die zwei Erreichbarkeitstests der Kostenmasken drehen sich um: Ihre Nachfolge
sind Hüllen, keine Formulare — der Graph darf sie nicht mehr kennen. Der neue
Test sichert genau das für alle sieben Masken der Welle.

### 7.4 Stapellauf

```
dotnet run --project Werkzeuge/Formularkarte -c Release -- --alle WindowsFormsApplication1 --erreichbarkeit
```

| Kennzahl | nach W1 | nach W0 | nach W2 | nach W3 | **nach W4** |
|---|---:|---:|---:|---:|---:|
| Designer-Dateien (Repo) | 114 | 108 | 105 | 101 | **92** |
| davon Masken | 111 | 105 | 102 | 98 | **91** |
| lokalisiert | 62 | 61 | 59 | 59 | **59** |
| Kartenzeilen | 2 322 | 2 231 | 2 188 | 2 128 | **1 994** |
| Felder ohne Beschriftung | 172 | 168 | 168 | 165 | **151** |
| Öffner erreichbar („ja") | 104 | 103 | 100 | 96 | **89** |
| unerreichbar / verwaist / unklar | 4/1/2 | 0/0/2 | 0/0/2 | 0/0/2 | **0/0/2** |

### 7.5 SQL-Dialektprüfer

```
python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite
→ 1 301 SQL-Texte geprüft: 0 Fundstellen, 149 dynamisch, 1 152 in Ordnung
```

Zwei Texte weniger als nach W3 (1 303): Die neun Anweisungen sind aus der Maske
in den Kern gewandert, dabei sind die drei Zweige des `SpeichereWerte`-Upserts
und die `RecordSet`-Abfrage zusammengefasst worden. **Neu geprüft** ist die
`Zieleinheit`-Abfrage — sie war als Zeichenkettenverkettung im `RecordSet` für
den Prüfer bisher nicht sichtbar.

### 7.6 ChartProben

```
dotnet run --project Proben/ChartProben -c Release
→ 10 Bilder geprueft, 0 Verstoesse.  ERGEBNIS: alle gruen.
```

Unverändert zu W3 — die Welle hat den Renderer nicht angefasst.

### 7.7 Referenzlauf

```
dotnet run --project EPOS.Referenzlauf -c Release -- lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
    --projekte 1030,1007,1017 --ziel <ordner>
dotnet run --project EPOS.Referenzlauf -c Release -- vergleich Referenzlaeufe/2026-08-30_B3-Kaskade <ordner>
```

| Projekt | Ergebnis |
|---|---|
| 1007 | **PASS** (29 Dateien, 324 219 Werte) |
| 1017 | **PASS** (21 Dateien, 254 154 Werte) |
| 1030 | **PASS** (22 Dateien, 236 670 Werte) |

`diff -rq` gegen die Basis meldet für diese drei Ordner **keinen** Unterschied.
Der Lauf ist **Pflicht**: Die Kostenpositionen fließen über
`KostenProjektPositionenCtrl` in die Wirtschaftlichkeit, die Trägerpreise über
`energy_project_settings` in den Rechenweg, und mit
`EnergietraegerPreisCtrl` ist ein Kern-Controller entstanden. Der Nachweis
bestätigt, dass die neun umgezogenen SQL-Anweisungen wortgleich geblieben sind.

### 7.8 Veröffentlichung

```
dotnet publish WindowsFormsApplication1 -c Release -r win-x64 --self-contained -p:Platform=x64 -o <ordner>
```

`wwwroot` vollständig: `index.html`, `_framework/blazor.webview.js`,
`_framework/blazor.modules.json`, `_content/EPOS.UI/{epos-ui.css,help_icon.png}`
(samt `.br`/`.gz`),
`_content/Microsoft.AspNetCore.Components.QuickGrid/QuickGrid.razor.js`. Die 24
neuen CSS-Klassen (`epos-ueberlagerung*`, `epos-fokusfalle`, `epos-rueckfrage*`,
`epos-zeilenraster`, `epos-zr-*`, `epos-preisblock*`, `epos-preiszeile`,
`epos-schnellwahl*`, `epos-kontextleiste`, `epos-bannerzeile`,
`epos-traeger*`, `epos-feldpaar`, `epos-kachelreihe`, `epos-mehrfachauswahl*`,
`epos-ertrag*`) sind in der ausgelieferten `epos-ui.css` enthalten.

---

## 8. Grenzen

* **Keine Windows-Sicht.** Alles hier ist auf Linux gemessen: Build, Tests,
  Referenzlauf, ChartProben, Veröffentlichung. Ob die beiden Großdialoge in der
  WebView2 richtig aussehen — und vor allem, **ob die Überlagerung ihre
  Fokusfalle trägt** —, sagt erst die Abnahme (§ 9).
* **Die beiden Großdialoge sind lang.** `EnergietraegerEinstellungen` stellt
  vier Abschnitte untereinander, die der Vorläufer auf zwei Reiter verteilte;
  `KostenKomponenteDialog` hängt den Ertragsabschnitt unter das Positionsraster.
  Ob das in einem Fenster noch übersichtlich ist, ist eine Sichtfrage (W4‑O1) —
  Welle 5 bringt den Baustein `Reiter`.
* **Der PV-Vergütungsdialog bleibt ein zweites Fenster** (A‑8), ebenso der
  Kostenfaktor-Katalog in seiner Rückfrage (A‑6).
* **Der Fortschritt fehlt weiterhin** (A‑17 aus W1) — der Spotpreis-Import
  meldet seine 8 760 Werte über die Statuszeile, bis Welle 11 den Baustein
  `Fortschritt` bringt.

---

## 9. Abnahmeliste Windows (iZ5) für diese sieben Masken

Wege: **Menü → Kostenverwaltung** (`KostenKomponenteHuelle`, Stammkontext) ·
**Berichte & Kosten → Kosten → „Kostenverwaltung öffnen…"** (Projektmodus) ·
**Assistent → Wärmepumpe → „Kosten…"** (Anlagenkontext) · **Menü →
Energieträgerverwaltung** (Katalogkontext) · **Berichte & Kosten → Kosten →
„Energieträger…"** (Projektkontext) · **jede Anlagenmaske → „Energiekosten…"**
(vorgewählter Träger).

| # | Punkt | W4.2 | W4.4 |
|---|---|:--:|:--:|
| 1 | Öffnet mittig, kein weißes Aufblitzen | ☐ | ☐ |
| 2 | Fenster ziehbar **und** maximierbar | ☐ | ☐ |
| 3 | Tabellen ohne Umbruch (Befund 03.09.) | ☐ | ☐ |
| 4 | Deutsch **und** Englisch (`HKCU\Software\wp-plan\Language`) | ☐ | ☐ |
| 5 | Hochkontrast: Abdunkelung, Warnbanner und Fehleingabe bleiben unterscheidbar | ☐ | ☐ |
| 6 | 125 % und 150 % scharf (DPI-Insel greift) | ☐ | ☐ |
| 7 | Maus **und** Finger (44 px), Optionsgruppen mit den Pfeiltasten | ☐ | ☐ |
| 8 | **Überlagerung: Tab bleibt im Bereich**, Esc schließt zuerst ihn | ☐ | ☐ |
| 9 | Infoknopf zeigt die Wikiseite „Kosten" | ☐ | ☐ |

**Fachliche Proben:**

| # | Probe |
|---|---|
| **W4‑1** | W4.2 Stammkontext: Komponente wechseln → Titel, Variantenliste und Raster folgen; „Betriebskosten" blendet die Nutzungsdauerspalte aus und ändert die Betragsüberschrift auf `[€/a]` |
| **W4‑2** | W4.2: Satz einer absoluten Position ändern → der Betrag spiegelt mit, die Kette steht, die Nettosumme zählt neu — und in der Datenbank steht noch der alte Wert, bis „Speichern" gedrückt ist (Ä12/Ä19) |
| **W4‑3** | W4.2: „Abbrechen" nach einer Feldänderung → beim erneuten Öffnen steht der alte Wert |
| **W4‑4** | W4.2: In der Abschlusszeile einen Namen eintippen und ＋ drücken → die Position steht im Raster; ohne Namen ist ＋ gesperrt (A‑1) |
| **W4‑5** | W4.2: ✏️, ± und „Übernahme" öffnen ihren Dialog **in demselben Fenster** (Abdunkelung, kein zweiter Eintrag in der Taskleiste); nach OK steht der Wert im Raster |
| **W4‑6** | W4.2 Projektmodus: „🗑️" auf einer Pflichtposition → Erklärung statt Rückfrage; auf einer gewöhnlichen → Rückfrage, „Nein" lässt sie stehen |
| **W4‑7** | W4.2 Stammkontext: „Neu…" schlägt „‹Komponente› — Variante n" vor; ein belegter Name meldet sich; die Standardvorlage lässt sich nicht löschen |
| **W4‑8** | W4.2 BHKW oder Photovoltaik wählen → der Abschnitt „Ertrag/Bonus" erscheint; bei jeder anderen Komponente nicht (FK5). „Gesetzesparameter…" öffnet den Katalog **modal über** dem Dialog (Sprungbrücke, wie W2‑7) |
| **W4‑9** | W4.4 Katalogkontext: Träger wählen → Kopfzeilen, Preise, Umrechnungsblock, Emissionen und Historie stehen; Bezeichnung ändern und „Übernehmen" schreibt die Katalogzeile und ordnet die Liste neu |
| **W4‑10** | W4.4: Preisbasis wechseln (z. B. Nm³ → kWh) → Arbeitspreis, Heizwert, Brennwert und Leistungspreis rechnen um, die Einheitenbeschriftungen folgen, die Formelzeile stimmt |
| **W4‑11** | W4.4: Die Regel, die den Träger nach kWh trägt, abschalten → sie bleibt an, und der rote Hinweis nennt den Grund (Riegel § 4.3). „Speichern" mit verletzter kWh-Bedingung schreibt **nichts** und meldet |
| **W4‑12** | W4.4 Stromträger: Aufschlagsblock steht; „Gesamtwert" sperrt die fünf Komponenten, lässt sie aber lesbar; der empfohlene Stromsteuersatz steht fett und trägt seine Herkunft im Kurztext |
| **W4‑13** | W4.4 Brennstoffträger: Zerlegung steht; ein gesperrter Schnellwahlknopf nennt den Grund; „In Arbeitspreis übernehmen" trägt den Wert in das Arbeitspreisfeld ein und schreibt **nicht** |
| **W4‑14** | W4.4: „Katalog…" einer Emissionszeile öffnet den Emissionskatalog **in demselben Fenster**; „Übernehmen" trägt den Wert in die Zeile ein (nicht in die Datenbank), „Speichern" schreibt ihn |
| **W4‑15** | W4.4 Projektkontext, Stromträger: die Karten „Kostenprofil" und „Spotmarktpreise" stehen; beide öffnen ihren Dialog in demselben Fenster, und die Statuszeile der Karte stimmt danach |
| **W4‑16** | W4.4 Projektkontext: „Aus Katalog übernehmen…" zeigt die freien Träger mit Haken samt „Alle"/„Keine"; nach „Übernehmen" steht der letzte gewählte Träger in der Liste. Sind alle zugeordnet, meldet der Dialog statt zu öffnen |
| **W4‑17** | W4.4: „Entfernen" bzw. „Löschen" fragt nach; ein benutzter Träger bleibt erhalten und nennt den Grund |
| **W4‑18** | W4.4: Ein Träger ohne Heizwert (Fernwärme) zeigt weder Heizwertfeld noch Formelgruppe; einer ohne Leistungspreis keine Saisonzeile |

---

## 10. Offene Punkte

| # | Punkt |
|---|---|
| **W4‑O1** | **A‑2 sichtprüfen:** Vier Abschnitte untereinander machen die Trägerkarte zu einem langen Fenster, und die Kostenverwaltung hängt den Ertragsabschnitt unter das Raster. Wenn der Anwender die Reiterform vermisst, bringt Welle 5 den Baustein `Reiter` — beide Dialoge (und das Kostenprofil aus W3) bekommen ihn dann nachträglich |
| **W4‑O2** | **A‑6:** Der Kostenfaktor-Katalog fragt weiterhin über `Dienste.Dialog`, obwohl er jetzt in einer Überlagerung steht. Auf iOS gibt es diese Rückfrage nicht. Er zieht beim nächsten Anfassen auf den Baustein `Rueckfrage` nach — dasselbe gilt für den Emissionskatalog (zwei Rückfragen) |
| **W4‑O3** | **A‑8:** Der PV-Vergütungsdialog bleibt ein zweites Fenster, weil er selbst eine Blazor-Hülle ist. Mit Welle 5 (Wirtschaftlichkeitsseite) wird daraus eine Überlagerung |
| **W4‑O4** | **Die Fokusfalle am Gerät prüfen** (Abnahmepunkt 8). Zwei leere `tabindex="0"`-Felder, die den Fokus auf die Bereichswurzel zurückholen, sind die JS-freie Fassung. Fällt sie durch, braucht `EPOS.UI` doch eine JS-Schicht — dieselbe, die W1‑O4 für `SelectAll()` und W3‑O2 für das Zoomen erwägt |
| **W4‑O5** | **A‑13 dem Anwender vorlegen:** Der Modus-Schalter der Emissionen wirkt jetzt erst mit „Speichern". Im Bestand schrieb ihn `EmissionenSpeichern` ebenfalls erst dort — die Änderung war aber sofort im Objekt und wirkte auf die angezeigte Summe. Das ist unverändert; erklärungsbedürftig bleibt es |
| **W4‑O6** | **Die 27 nie vorhandenen Ressourcenschlüssel** (§ 4, Befund) sind jetzt übersetzt. Ob die englischen Fassungen fachlich passen — besonders „Species" für Emissionsart und „Demand charge" für Leistungspreis —, entscheidet der Anwender |
| **W4‑O7** | `EnergietraegerHuelle` hält den Bearbeitungsstand **je geöffnetem Träger neu**. Ein Trägerwechsel verwirft ungespeicherte Änderungen (Ä14, unverändert) — es gibt aber keine Rückfrage davor. Ob eine gewünscht ist, sagt die Abnahme |
| **W4‑O8** | Der Umrechnungsblock übernimmt eine Regeländerung erst beim nächsten `RegelnUebernehmen` in die Speicherkopie (Riegel, Anlegen, Speichern). Das genügt für alle drei Wege; ein vierter Weg müsste es mitrufen |

---

## 11. Geänderte und neue Dateien

```
NEU
  EPOS.UI/Bausteine/Ueberlagerung.razor                          131 Zeilen
  EPOS.UI/Bausteine/Rueckfrage.razor                              78
  EPOS.UI/Bausteine/Zeilenraster.razor                           110
  EPOS.UI/Bausteine/Mehrfachauswahl.razor                         96
  EPOS.UI/Dialoge/Kosten/VorlagenZeile.razor                     190
  EPOS.UI/Dialoge/Kosten/ErtragBonus.razor                       178
  EPOS.UI/Dialoge/Kosten/KostenKomponenteDaten.cs                140
  EPOS.UI/Dialoge/Kosten/KostenKomponenteDialog.razor            600
  EPOS.UI/Dialoge/Kosten/EnergietraegerPreisDaten.cs             120
  EPOS.UI/Dialoge/Kosten/StromAufschlaege.razor                  160
  EPOS.UI/Dialoge/Kosten/BrennstoffBestandteile.razor            185
  EPOS.UI/Dialoge/Kosten/EnergietraegerDaten.cs                  250
  EPOS.UI/Dialoge/Kosten/EnergietraegerEinstellungen.razor       420
  EPOS.UI/Dialoge/Kosten/EnergietraegerDialog.razor              620
  EPOS.Kern/Controller/EnergietraegerPreisCtrl.cs                460
  WindowsFormsApplication1/Views/Kosten/KostenKomponenteHuelle.cs 790
  WindowsFormsApplication1/Views/Kosten/ErtragBonusGaben.cs      210
  WindowsFormsApplication1/Views/Kosten/EnergietraegerHuelle.cs 1200
  EPOS.UI.Tests/Bausteine/UeberlagerungTests.cs                  155  (12 Tests)
  EPOS.UI.Tests/Bausteine/RueckfrageTests.cs                     125  (9)
  EPOS.UI.Tests/Bausteine/ZeilenrasterTests.cs                   120  (9)
  EPOS.UI.Tests/Dialoge/VorlagenZeileTests.cs                    250  (18)
  EPOS.UI.Tests/Dialoge/ErtragBonusTests.cs                      190  (11)
  EPOS.UI.Tests/Dialoge/KostenKomponenteDialogTests.cs           560  (31)
  EPOS.UI.Tests/Dialoge/PreisbloeckeTests.cs                     330  (18)
  EPOS.UI.Tests/Dialoge/EnergietraegerDialogTests.cs             490  (27)
  Werkzeuge/Formularkarte.Tests/Pruefmuster/Kosten/Form_KostenKomponente.{cs,Designer.cs,resx}
  Werkzeuge/Formularkarte.Tests/Pruefmuster/Kosten/ucVorlagenZeile.{cs,Designer.cs}
  WindowsFormsApplication1/Allgemein/Reporting/iU9_W4_Blazor_Port_Protokoll.md  dieses Protokoll

GEÄNDERT
  EPOS.UI/Dialoge/Kosten/EmissionskatalogDialog.razor  Untereditoren → Ueberlagerung (A-10)
  EPOS.UI/wwwroot/epos-ui.css                          + 24 Klassen
  EPOS.Kern/MyResource/Resource.resx                   + 50 Schlüssel
  EPOS.Kern/MyResource/Resource.en-US.resx             + 50
  EPOS.Kern/MyResource/Resource.Designer.cs            + 50 (von Hand)
  WindowsFormsApplication1/Allgemein/Blazor/NamensDialogHuelle.cs   + Gaben
  WindowsFormsApplication1/Views/Kosten/VorlagenUebernahmeHuelle.cs  Oeffnen → Gaben
  WindowsFormsApplication1/Views/Kosten/KostenfaktorKatalogHuelle.cs dito
  WindowsFormsApplication1/Views/Kosten/KostenprofilHuelle.cs        dito
  WindowsFormsApplication1/Views/Kosten/SpotpreisImportHuelle.cs     dito
  WindowsFormsApplication1/Views/Kosten/LeistungspreisReiheHuelle.cs dito
  WindowsFormsApplication1/Views/Kosten/EmissionskatalogHuelle.cs    Oeffnen → Gaben + Auswerten
  WindowsFormsApplication1/MDIMainForm.cs                            2 Aufrufstellen
  WindowsFormsApplication1/Views/BerichteKosten/UcBkKosten.cs        2
  WindowsFormsApplication1/Views/Kosten/KostenKnoepfe.cs             3
  WindowsFormsApplication1/Views/Wizard/Wizard_WPItem.cs             1
  WindowsFormsApplication1/Allgemein/KI/HilfeKontext.cs             − 2 Einträge
  WindowsFormsApplication1/Allgemein/GrafikTools/KartenStil.cs       3 Kommentarverweise
  WindowsFormsApplication1/Views/GemeinsameBausteine/AktionsKarte.cs 1
  WindowsFormsApplication1/Views/Wirtschaftlichkeit/UcWirtschaftlichkeit.cs 1
  EPOS.UI.Tests/Dialoge/EmissionskatalogDialogTests.cs               9 Selektoren
  Werkzeuge/Formularkarte.Tests/{Stapel,Erreichbarkeit,RazorSchreiber,Pruefmuster}Tests.cs
  Werkzeuge/Formularkarte/{LIESMICH.md,Erreichbarkeit_2026-09-03.md}

GELÖSCHT
  17 Dateien der sieben WinForms-Masken und der zwei nutzerlosen Karten-Controls
  (Regel M1) — Liste in § 6
```

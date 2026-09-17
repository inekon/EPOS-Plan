# BK1 — Trägerzuordnung: Wizard-Automatik, Fehlzeilen, Emissionsspalten (Umsetzungsprotokoll)

Anlass: Sichtabnahme-Befunde des Anwenders auf der Kosten-Seite (30.08.2026, Projekt
„Beispiel WP WG 1"/1026): „Energieträger des Projekts unvollständig — Strom fehlt" und
„Emissionswerte nicht enthalten". Erhebung ergab: **Datenlücke** (die Wizard-Automatik
ordnete Stromträger nie zu — 19 Projekte betroffen; Anzeige selbst verschluckt nichts,
30/30 Projekte deckungsgleich) plus **Anzeigelücke** (keine Emissionsspalten).
Anwenderentscheid: „WP, PV und Stromspeicher **müssen** dem Energieträger Strom
zugeordnet sein" → Automatik statt bloßer Warnung. Stand 30.08.2026, Branch
`Pufferspeicher`.

## 1. Standard-Stromträger als eine Wahrheit

Der Katalog führt **drei** ELECTRICITY-Träger (54 „Strom Variante", 58, 60 „Elektrische
Energie"); die alte Rückfallregel „kleinste ID" traf 54. Erhebung der Kennungen:
`is_active` überall TRUE, `sort_order` NULL, kein ReadOnly-Kennzeichen — tauglich ist
allein **`code`**, der beim Umbenennen stehen bleibt (Verweisanker; er benennt die
*Familie*: dieselbe Kollision existiert bei „Biogas" und „Heizöl L").

Neu: `ProjektEnergietraegerCtrl.StandardStromTraeger(projektID)` — **eine Funktion für
Anzeige und Automatik**: (1) ein bereits zugeordneter Stromträger gewinnt immer
(Anwenderentscheidung wird nie überstimmt); (2) sonst Katalograngfolge: +2 für
`code = DbWerte.ENERGIETRAEGER_CODE_STROM` (neue Persistenzkonstante „Elektrische
Energie"), +1 für „nicht umbenannt" (`name == code`), Gleichstand → kleinste ID.
Ergebnis: **60** (gemessen; Altregel lieferte 54). Der Anzeigename bleibt Anzeige —
gesteuert wird über `code` (Drei-Schichten-Regel).

## 2. Wizard-Automatik

`WizardCtrl.Add_Projekt_Energietraeger`: nach der bestehenden Brenner-Schleife prüft
`BrauchtStromTraeger` auf Erzeuger der Typen WP (1) / PV (3) / Stromspeicher (4) und
stellt die Zuordnung des Standard-Stromträgers sicher — über das herausgezogene
`TraegerSatzAnlegen` (dieselbe Mechanik wie die Brennerzuordnung), idempotent.
Bestandsprojekte heilen beim nächsten Wizard-Speichern.

## 3. Kosten-Seite

- **Rote Fehlzeilen** (`ZeigeFehlendeTraeger`): je verwendetem, aber nicht zugeordnetem
  Träger eine rot markierte Zeile „{Träger} — nicht zugeordnet" mit Verursacher-Tooltip
  (Muster der Komponenten-Fehlzeilen); die Fußzeile nennt jetzt den **richtigen** Träger.
- **Drei Emissionsspalten** (CO₂ g/kWh, SO₂ mg/kWh, NOₓ mg/kWh) aus
  `EmissionsFaktorLader` (dieselbe Lesekette wie die Rechner, Herkunftsebene als
  Hinweis); Spaltengewichte neu austariert, Listenteilung 38/62 → 30/70.
- **Strommix-Rückfall sichtbar**: `KostenEmissionRechner` setzt
  `VariantenDaten.CO2StrommixRueckfall`, der Berichtspfad meldet es
  (`BerichtsDatenSammler` → Warnungen); der Reiter-Anschluss (eine Zeile in
  `RechneProjekt`) läuft mit der parallelen B3b-Arbeit ein.
- 6 neue `BK_`-Texte de/en (GetString-Rückfallmuster, Designer unangetastet).

## 4. Nachweise (Harness `..\dev\bk1\`, frische Kopie)

| Probe | Ergebnis |
|---|---|
| Build x64 | Exit 0, nur bekannte Altwarnungen |
| Standardregel | `KatalogStromTraeger()` = **60** (Altregel MIN(id) = 54) |
| 1026 vorher | rote Fehlzeile „Elektrische Energie — nicht zugeordnet" (Verursacher: BYD-Speicher, Jinkosolar-PV, CS6800iAW-WP); Fußzeile nennt den richtigen Namen; `CO2StrommixRueckfall = True`, CO₂ 8,30 t/a |
| Automatik | genau **eine** neue Zeile (Träger 60), Zweitlauf legt keine weitere an, Erdgas-Zeile feldweise identisch |
| 1026 nachher | Fehlzeile weg, Emissionsspalten gefüllt (Erdgas 240/0,30/110), `CO2StrommixRueckfall = False`; Energiekosten ehrlich weiter `null` (beide Träger ohne Arbeitspreis — die Fußzeile benennt es) |
| Regressionsanker | Betrieb 1024 = 99,00; Invest 1018/1024/1042 = 45.312,50/12.001,00/13.000,00 — exakt |
| Sweep | kein `<<<<<<<`-Treffer |

## 5. Emissions-Stammkopie (Altbefund) — **entschieden am 30.08.2026**

> **Nachtrag (Etappe BK2).** Der Anwender hat entschieden: **Die Katalogwahrheit
> gilt.** `TraegerSatzAnlegen` schreibt die drei Emissionsspalten seither
> ausdrücklich als NULL; die Lesekette liefert damit die aktive Katalogzeile
> (Strom 435, Erdgas 201), danach wie bisher Stamm und Carrier. Die Zuordnung ist
> emissionsneutral (1026 gemessen: 8,30 t/a vor **und** nach der Automatik, statt
> 10,68 mit Kopie), der Brennerweg wechselt gewollt von 240 auf 201. Preise,
> Heizwerte und Umrechnungssatz bleiben Bestandsverhalten. Bestandszeilen wurden
> nicht angefasst (die 7 Zeilen 560/200/280 stehen unverändert). Der manuelle Weg
> `EnergietraegerKatalogCtrl.InsProjekt` kopierte nie Emissionen und blieb in BK2
> unverändert — er schrieb allerdings, was BK2 gleich mitmaß, auf ALLE nicht
> genannten Spalten den Access-Default 0 statt NULL; das hat Etappe BK3 gerichtet
> (`BK3_InsProjekt_Defaultspalten_Protokoll.md`). Nachweise: Harness `..\dev\bk2\`.

Der ursprüngliche Befund, unverändert:

`TraegerSatzAnlegen` kopiert seit jeher `Tab_Brennstoff_Stamm.CO2/SO2/NOx` in die
Projektzeile — und der **Projektwert übersteuert den Katalog** (oberste Ebene der
E5-Lesekette). Für Strom stehen im Stamm **560 g/kWh**, der aktive Katalogwert
(BAFA_EEW, Schritt-56-Saat) ist **435**: Die Automatik hebt 1026 dadurch von 8,30 auf
10,68 t CO₂/a (+28,7 %). Sieben Bestandsprojekte tragen dieselben 560/200/280 aus dem
alten Weg. **Alternative:** die neue Zeile ohne Emissionskopie anlegen (NULL) → die
Lesekette liefert die Katalogwahrheit 435, die Zuordnung wäre emissionsneutral — das
änderte aber auch das Verhalten für künftige Brennerzuordnungen (Erdgas: Kopie 240 vs.
Katalog 201). Mechanik in dieser Etappe **bewusst unverändert** (Bestandsverhalten);
Entscheidung beim Anwender, Bestandszeilen bleiben in jedem Fall unangetastet.

## 6. Geänderte Dateien

```
Allgemein/DbWerte.cs                          ENERGIETRAEGER_CODE_STROM
Controller/ProjektEnergietraegerCtrl.cs       Traeger.Code, StandardStromTraeger, KatalogStromTraeger
Controller/WizardCtrl.cs                      Automatik, BrauchtStromTraeger, TraegerSatzAnlegen
Views/BerichteKosten/UcBkKosten.cs            Fehlzeilen, 3 Emissionsspalten, Gewichte, Prüfhilfen
Allgemein/Bericht/KostenEmissionRechner.cs    setzt CO2StrommixRueckfall
Allgemein/Bericht/BerichtsDaten.cs            Feld CO2StrommixRueckfall
Allgemein/Bericht/BerichtsDatenSammler.cs     Anschluss an die Berichtswarnungen
MyResource/Resource.resx / .en-US.resx        6 BK_-Schlüssel
Allgemein/Reporting/BK1_Traegerzuordnung_Protokoll.md   dieses Protokoll
```

Harness (gitignored): `..\dev\bk1\`. Grundlagen-Erhebung: siehe Befundkarte im
Sitzungs-Memory (Anzeige-Vollständigkeit 30/30, Lückenliste 21/29 Projekte).

---

## 7. Windows-Abnahme 08.09.2026 — Energieträgerverwaltung im Projektkontext (ET‑1 bis ET‑4)

**Wortlaut des Anwenders (Bildschirmfotos Projekt 1026 und Katalogkontext):** „Die
Energieträgerverwaltung funktioniert nicht. Auch sind der Wärmepumpe und PV keine Energieträger
(Strom) zugeordnet. Es muss a. der Energieträger (übergeordnet) und b. die untergeordnete Art
dargestellt und auch ausgewählt werden können (die Emissionswerte sollen am Energieträger hängen,
PV ist zum Beispiel default 0 Emissionen). Alle Anlagen mit Energieträgern (Wärmepumpe, PV,
Stromspeicher — default Strom) müssen einem Energieträger zugeordnet sein. Es sollte eine
Übersicht über alle verwendeten Energieträger in der Auswahl Energieträger geben (analog
Katalogkontext)."

### 7.1 Befund

| Nr. | Befund | Ursache |
|---|---|---|
| **ET‑1** | „Aus Katalog übernehmen…" meldete immer „Alle Katalogträger sind dem Projekt bereits zugeordnet" — im Projektkontext ließ sich kein Träger hinzufügen, also auch kein Strom | `EnergietraegerHuelle.GabenIntern` setzte den Razor-Parameter `Freie` nie; `EnergietraegerKatalogCtrl.NichtZugeordnete` hatte keinen Aufrufer. Die Dialogtests setzten `Freie` selbst — deshalb fiel es dort nicht auf |
| **ET‑2** | Wärmepumpe, PV und Stromspeicher ohne Stromträger (1026) | Die Automatik (BK1 § 2) lief nur beim Speichern des Assistenten; Anlagen, die außerhalb hinzukamen, bekamen keine Zuordnung |
| **ET‑3** | Die Liste zeigte nur `energy_project_settings`-Zeilen (Zuordnung), nicht die Verwendung; „Speichern" auf einem nicht zugeordneten Träger schrieb nichts und meldete trotzdem „gespeichert" | `KostenSummenCtrl.GetAllCarriers(projekt)`; `EnergietraegerHuelle.Speichern` :1001 |
| **ET‑4** | Der Stromträger eines Wärmepumpenprojekts ließ sich widerspruchsfrei entfernen — danach fiel die Rechnung still auf 435 g/kWh zurück | `AusProjektEntfernen` prüfte nur `Tab_Energieanlagen.ID_Carrier`, den WP/PV/SP nicht führen |

### 7.2 Umsetzung

- **ET‑1** Hülle liefert `Freie` und `FreieLaden` (frisch bei jedem Öffnen der Übernahme, mit
  Gruppe: „Strom › Elektrische Energie"); der Dialog liest über `FreieLaden`, `Freie` bleibt
  für Aufrufer ohne Hülle.
- **ET‑2** `ProjektEnergietraegerCtrl.StromTraegerSicherstellen(projekt)` (Kern): braucht das
  Projekt Strom (`BrauchtStromTraeger`: WP/PV/SP/Heizstab in `Tab_Energieanlagen`) und ist kein
  ELECTRICITY-Träger zugeordnet, wird der Auslieferungsträger (`StandardStromTraeger`, BK1) über
  `WizardCtrl.TraegerSatzAnlegen` (jetzt `internal`) zugeordnet — dieselbe Mechanik wie im
  Assistenten, keine Emissions-Stammkopie. Gerufen aus `WErzeugerCtrl.Insert/Update` (nach dem
  Schreiben einer elektrischen Anlage), aus `EnergietraegerHuelle.ListeLaden` (Projektkontext)
  und aus `KostenSeiteGaben.Laden` — die rote Fehlzeile der Kostenseite heilt sich damit selbst.
- **ET‑3** Die Liste im Projektkontext führt zusätzlich die VERWENDETEN Träger
  (`ProjektEnergietraegerCtrl.Verwendete`): je Eintrag der Kurztext „verwendet von: Wärmepumpe
  „CS6800iAW", Photovoltaik „Jinkosolar"", ein verwendeter, nicht zugeordneter Träger steht
  markiert („⚠ nicht zugeordnet", `.epos-traeger-eintrag--offen`) und lässt sich wählen;
  **Speichern ordnet ihn zu** (`InsProjekt`, dann Werte) statt still nichts zu schreiben.
  `EnergietraegerListe` trägt dafür `Kurztext` und `Zugeordnet`.
- **ET‑4** `AusProjektEntfernen` lehnt einen verwendeten Träger ab: „verwendet von …".
- Texte `KDLG_ET_VERWENDET_VON`, `KDLG_ET_NICHT_ZUGEORDNET`, `KDLG_ET_ZUORDNUNG_FEHLGESCHLAGEN`
  (de/en, Designer).

### 7.3 Nachweise

| Was | Wo | Ergebnis |
|---|---|---|
| 1026 (WP, PV, Speicher, keine Stromzuordnung) bekommt genau eine Stromzeile (Träger 60), zweiter Aufruf legt keine zweite an; 1017 (Strom Variante 54) wird nicht überstimmt; ohne elektrische Welt geschieht nichts | `EPOS.Kern.Tests/ProjektEnergietraegerCtrlTests.cs` | **4** neue Fälle |
| Übernahme holt die freien Träger über `FreieLaden`; verwendeter, nicht zugeordneter Träger steht markiert mit Kurztext und lässt sich wählen | `EPOS.UI.Tests/Dialoge/EnergietraegerDialogTests.cs` | **2** neue Fälle |
| Sandbox-Bau, `EPOS.Kern.Tests`, `EPOS.UI.Tests` | `K:\imp\src` | 0 Fehler, **2 055/2 055 (+4)**, **3 260/3 260 (+2)** |
| Referenzlauf | — | unberührt: keine Zeile eines Rechenwegs; die Zuordnung ist emissionsneutral (BK2, Katalogwahrheit) |

### 7.4 Abnahmepunkte — A‑ET

1. Projekt 1026 öffnen → Berichte & Kosten → Kosten → „Energieträgerverwaltung…": Die Liste
   führt „Gas › Erdgas E" UND „Strom › Elektrische Energie"; der Werkzeugtipp des Stromträgers
   nennt Wärmepumpe, Photovoltaik und Stromspeicher.
2. „Aus Katalog übernehmen…" öffnet die Mehrfachauswahl mit den noch freien Katalogträgern
   (Fernwärme, Biogas, …); nach der Übernahme steht der Träger in der Liste.
3. „Entfernen" auf dem Stromträger wird abgelehnt: „Der Träger wird verwendet und bleibt
   erhalten: verwendet von Wärmepumpe „…", …".
4. Kostenseite: die Trägertabelle führt Strom mit Preis/Emissionen, keine rote Fehlzeile mehr.

### 7.5 Offen — Entscheid des Anwenders (ET‑5): „Energieträger (übergeordnet) und Art (untergeordnet) wählbar"

Das Datenmodell kennt heute **eine** Ebene: `energy_carrier` mit `group_code` (Text: Gas,
Strom, Fernwärme, …) als Anzeigegliederung und `name` als Träger; „Variante" ist eine
Vollkopie der Katalogzeile, keine Unterart; Emissionen hängen an der einzelnen Zeile (Kette
Projekt → Katalog → Stamm → Carrier). Ein Träger „Photovoltaik/Solarstrom mit 0 g/kWh" existiert
nicht — PV-Strom wird als verdrängter Netzstrom mit dem Faktor des Stromträgers gutgeschrieben,
die Wärmepumpe rechnet über denselben Stromträger. Damit sind zwei Lesarten möglich:

- **(a) Anzeige und Wahl je Anlage aus der vorhandenen Gliederung:** In den Dialogen
  Wärmepumpe, Photovoltaik, Stromspeicher ein Feld „Energieträger: [Gruppe ▾] › [Träger ▾]"
  (Gruppe = übergeordnet, Träger = Art), gespeichert als `Tab_Energieanlagen.ID_Carrier` wie
  bei Kessel und BHKW; Vorgabe der Stromträger des Projekts. Folge: Die Rechenwege
  (`KostenEmissionRechner`, `Emissionsquelle.StromTraeger`) müssten den Träger je Anlage lesen
  statt den einen Stromträger des Projekts — sonst ist die Wahl nur Anzeige.
- **(b) Ein echter zweistufiger Katalog** (Energieträger → Arten, Emissionen an der Art, neue
  Saat „Solarstrom 0 g/kWh", Schemaschritt ≥ 70, Migration der Bestandsdaten) — ein eigenes
  Konzept mit Auswirkung auf Referenzläufe.

Empfehlung: (a) als nächste Etappe, die PV mit dem Stromträger „Elektrische Energie" belegt
und im Emissionsausweis weiter als Verdrängung rechnet; (b) nur, wenn die Emissionen wirklich
je Art gepflegt werden sollen.

### 7.6 ET‑5 umgesetzt — Entscheid des Anwenders: „es handelt sich um diese Struktur, keine neue Struktur"

Die gemeinte Zweistufigkeit ist die Gliederung des Katalogs, wie sie die Energieträgerverwaltung
listet: **Energieträger = Gruppe** (`group_code`: Fernwärme, Gas, Holz, Strom …) › **Art =
Träger** (`name`: Erdgas E, Biogas, Elektrische Energie …). Kein neues Datenmodell, keine
Migration. Umgesetzt wie bei Kessel und BHKW — der Träger hängt an der Anlage:

| Wo | Was |
|---|---|
| Baustein `EPOS.UI/Bausteine/EnergietraegerWahl.razor` | zwei Auswahlfelder „Energieträger:" (Gruppe) und „Art:" (Träger der Gruppe), ein Wert (die Träger-Id); ein Gruppenwechsel wählt den ersten Träger der Gruppe |
| Dialoge Photovoltaik, Stromspeicher (Anlagenblock der markierten Projektzeile), Wärmepumpe (Detailansicht, Gruppe „Energieträger") | Parameter `Traegerkatalog`, `TraegerWechseln` bzw. `Daten.CarrierId`; ohne Katalog steht kein Feld |
| Hüllen (`ErzeugerTraegerHuelle`, neu) | `Katalog()` aus `KostenSummenCtrl.GetAllCarriers(0)` nach Gruppe und Name; `Standard(projekt)` = Stromträger des Projekts (Vorgabe „default Strom" für neue Anlagen und für Bestandszeilen ohne Träger); `Zuordnen(projekt, wizard, träger)` = Zuordnungszeile im Projekt (`InsProjekt`, NULL-Werte → Katalogwerte gelten); im Assistenten trägt `Add_Projekt_Energietraeger` die `ID_Carrier` beim Speichern nach |
| Speichern | `Tab_Energieanlagen.ID_Carrier` über den Anlagen-Schreibweg (`SQL_ANLAGE_INSERT` führt die Spalte) |
| Kern `ProjektEnergietraegerCtrl` | `Verwendete`/`AnlagenMitTraeger` lesen den Träger der elektrischen Anlage, sonst wie bisher den Stromträger des Projekts; neu `StromTraegerDerAnlagen`: der an der Anlage gewählte, dem Projekt zugeordnete ELECTRICITY-Träger — Verbraucher zuerst (Wärmepumpe, Heizstab, Speicher), dann Photovoltaik |
| Kern `StromAufschlagCtrl.StromCarrierId`, `Emissionsquelle.StromTraeger` | lesen zuerst `StromTraegerDerAnlagen` — Preis, Aufschläge und Emissionen folgen damit derselben Wahl; ohne Anlagenwahl (aller Bestand vor ET‑5) bleibt die bisherige Regel (kleinste Id der Zuordnungen) |

**Was die Wahl bedeutet.** Die Emissionswerte hängen am Träger (Art), wie der Anwender es
will: Wer der Wärmepumpe „Strom › Elektrische Energie 2" (etwa ein Ökostromtarif mit eigenem
Faktor) gibt, bekommt Strompreis und Emissionsfaktor dieser Zeile. PV mit „0 Emissionen" ist
ein Katalogträger der Gruppe Strom mit CO₂ 0 (Katalogkontext → „Variante" von „Elektrische
Energie", Emissionen auf 0) — die Photovoltaik erzeugt, sie bezieht nichts; für die Gutschrift
des verdrängten Netzstroms gilt weiter der Träger der Verbraucher (Rangfolge oben).

**Nachweise.** `ProjektEnergietraegerCtrlTests` +2 (1026: die an der Wärmepumpe gewählte
„Elektrische Energie 2" gewinnt in `StromCarrierId`, `Emissionsquelle.StromTraeger` und
`Verwendete`; ein nicht zugeordneter Anlagenträger zählt nicht), `EnergietraegerWahlTests` (neu,
4), `PhotovoltaikDialogTests` +2, `StromspeicherDialogTests` +1, `WaermepumpeAnlageDialogTests` +2.
Sandbox-Bau 0 Fehler, `EPOS.Kern.Tests` **2 056/2 057 (+2; der eine Fehlschlag `LizenzWarnstufenTests.Kulanz_und_Nachpruefung…` ist das bekannte Kulturflackern unter Last — englischer statt deutscher Text —, isoliert 27/27 grün)**, `EPOS.UI.Tests` **3 269/3 269 (+9)**.
Referenzlauf: aus der Sandbox gegen `P:\pa0\Quelle\Kenndaten.sqlite`, 14 Projekte, **355/355 MD5-gleich zur Basis `2026-09-07_M7_nach-Merge7`** — die Referenzdaten führen keine elektrische Anlage mit `ID_Carrier`, und der Lauf deckt zugleich Merge 8 und alle Commits vom 08.09.2026 ab.

**Abnahmepunkte A‑ET‑5.** (1) Projekt 1026 → Wärmepumpe → Ändern: Gruppe „Energieträger" zeigt
„Strom › Elektrische Energie"; Gruppe auf „Gas" stellen → Art springt auf den ersten Gasträger;
OK speichert, die Energieträgerverwaltung führt den Träger als „verwendet von: Wärmepumpe".
(2) Photovoltaik: markierte Anlage zeigt im Anlagenblock dieselbe Wahl; Wechsel auf
„Elektrische Energie 2" → in Kosten und Wirtschaftlichkeit rechnet der Strombezug mit dessen
Preis und Emissionsfaktor. (3) Stromspeicher: Detailblock der markierten Projektzeile zeigt die
Wahl; beim Katalogsatz nicht. (4) Neue Anlage aus dem Katalog: Vorgabe Strom.

## 8. Windows-Abnahme 16.09.2026 — die Trägerliste frischt sich auf (ET‑5 bis ET‑7)

**Wortlaut des Anwenders (Projektkontext, Bericht & Kosten → Kosten → Energieträgerverwaltung):**
„Ein entfernter Energieträger verschwindet nicht aus der Liste."

**Zur Kennung:** ET‑5 ist am 08.09.2026 schon einmal vergeben worden — für die Frage nach der
Zweistufigkeit (Abschnitt 7.5/7.6, umgesetzt und geschlossen). Die drei Befunde dieses
Abschnitts stammen aus dem Anwenderbefund vom 16.09.2026 und heißen zur Unterscheidung
**ET‑5/S2a**, **ET‑6/S2a** und **ET‑7/S2a**.

### 8.1 Befund

| Nr. | Befund | Ursache |
|---|---|---|
| **ET‑5/S2a** | Ein entfernter Träger blieb in der linken Liste stehen, solange der Dialog offen war — und mit ihm sieben weitere Wege: „Löschen", „Neu…", „Variante", das Stamm-„Übernehmen" (der neue Name stand weiter alt in der Liste), „Aus Katalog übernehmen…" (Liste ohne Markierung, Karte mit dem neuen Träger) und „Speichern" (die Marke „⚠ nicht zugeordnet" blieb nach der Zuordnung stehen) | **Die Liste war ein eingefrorener WERT.** `EnergietraegerHuelle.Gaben` setzte `["Liste"] = Listeneintraege()` einmal beim Bau; die Wirte halten den Gabensatz über die ganze Dialoglaufzeit, und die Komponente las den Parameter nur in `OnInitialized`. Kern und Hülle arbeiteten richtig — sie riefen nach jedem Schreiben `ListeLaden()` —, aber **es fehlte der Rückweg** in die Komponente |
| **ET‑6/S2a** | Nach dem Entfernen eines Stromträgers stand plötzlich wieder einer da; der Anwender hielt das für den Fehler, der gerade behoben wird | `ListeLaden()` ruft vor jedem Lesen `ProjektEnergietraegerCtrl.StromTraegerSicherstellen`. Führt das Projekt eine elektrische Anlage und verliert es dabei seinen letzten zugeordneten Stromträger, ordnet dieser Aufruf den Auslieferungsträger wieder zu — **still**. Die Wiederzuordnung selbst ist Anwenderentscheid ET‑2 vom 08.09.2026 und bleibt |
| **ET‑7/S2a** | Drei Knöpfe desselben Dialogs trugen „übernehmen": der Bestätigungsknopf der Kataloguebernahme, „Katalogwerte übernehmen" und der Knopf am Stammkopf | `KDLG_ET_STAMM_SPEICHERN` trug den Wert „Übernehmen" / „Apply" |
| **Testloch** | Kein einziger Prüffall rührte „Entfernen" an; im Kern hatten `AusProjektEntfernen`, `Umbenennen`, `Neu`, `Variante` und `Loeschen` **keine** Probe | Der Referenzlauf rechnet einen bestehenden Projektstand nach — er legt keinen Träger an und entfernt keinen; die bunit-Fälle reichten Attrappen herein |

### 8.2 Umsetzung

- **ET‑5/S2a — ein Nachlade-Weg, im Muster von `FreieLaden` (ET‑1) und
  `KostenfaktorKatalogHuelle.NeuLaden`.** Der Gabensatz führt neben `["Liste"]` den Delegaten
  `["ListeNeuLaden"] = new Func<…>(Listeneintraege)`; **kein zweites `ListeLaden()` darin** — die
  Schreibwege der Hülle (`AusProjekt`, `InsProjekt`, `TraegerNeu`, `TraegerVariante`,
  `TraegerLoeschen`, `StammSchreiben`) rufen es selbst, und ein weiterer Aufruf zöge
  `StromTraegerSicherstellen` ein zweites Mal.
  Die Komponente hält eine eigene `_liste`, füllt sie in `OnInitialized` aus dem Parameter und
  liest ausschließlich daraus (`GefilterteListe`, `ErsterTraeger`, `TraegerName`; `Treffer` und
  `BeiListenTaste` hängen an `GefilterteListe`). `ListeNachziehen()` zieht sie an **sieben**
  Stellen nach: Ja‑Zweig von `EntfernenFragen` und `LoeschenFragen`, `UebernahmeAusfuehren`,
  `NeuFertig`, `VarianteAnlegen`, `StammUebernehmen` und `BeiSpeichern`. Steht der gewählte
  Träger danach nicht mehr in der Liste, fällt die Markierung weg und die Karte wird leer.
  **Reihenfolge:** Erst nachziehen, dann markieren — sonst nähme das Nachziehen die eben
  gesetzte Markierung gleich wieder weg. Nach Entfernen und Löschen bleibt es bei „nichts
  markiert, Karte leer", auch wenn der Träger wieder auftaucht (ET‑6).
  Die Unterdialoge (Kostenprofil, Spotpreis, saisonale Sätze, Emissionskatalog) ziehen **nicht**
  nach: Keiner von ihnen ändert Bestand, Name oder Zuordnung eines Trägers, und die Hülle liest
  ihre Trägerliste dort auch nicht neu.
- **ET‑6/S2a — der stille Wiederzuordner wird benannt; der Kern bleibt unberührt.**
  `AusProjekt` merkt sich vor dem Entfernen die ZUGEORDNETEN Träger (ohne den zu entfernenden)
  und sucht nach dem `ListeLaden()` den ersten zugeordneten, der vorher nicht dabei war; sein
  Name steht in der Gabe `["StromZugeordnet"]`. **Nicht** über die Rückgabe von
  `StromTraegerSicherstellen`: Sie nennt auch dann eine Id, wenn der Träger längst zugeordnet
  war (idempotenter Fall), und taugt deshalb nicht als Ereignismelder.
  Der Dialog zeigt daraufhin ein Hinweisbanner (`WarnStufe.Hinweis`, kein Fehler) mit dem neuen
  Schlüssel `KDLG_ET_STROM_ZUGEORDNET` in beiden Sprachen: „Das Projekt führt elektrische
  Anlagen; der Stromträger „{0}" wurde zugeordnet." / „The project has electrical equipment; the
  electricity carrier „{0}" has been assigned."
- **ET‑7/S2a — der Stammkopf-Knopf heißt „Bezeichnung speichern" / „Save name"** (Wert von
  `KDLG_ET_STAMM_SPEICHERN` geändert, kein neuer Schlüssel; der Rückfalltext der Komponente und
  der der Hülle mitgezogen). Kein Prüffall hielt den alten Text fest.

**Unverändert:** `Katalogkontext` (Anwenderentscheid Ä9/Ä10), der projektlose
Administrationsweg, die Einengung (`EnergietraegerZulaessigkeit`, `Freie()`, `Eingeengt`), die
Wiederzuordnung selbst und jeder Rechenweg. Kein Schemaschritt, keine neue Referenzbasis.

### 8.3 Nachweise

| Was | Wo | Ergebnis |
|---|---|---|
| Je Schreibweg ein Fall: Entfernen, Löschen, Neu, Variante, Kataloguebernahme, Stamm-Speichern, Speichern — Liste, Markierung und Karte danach; dazu der Hinweis zum Stromträger in beiden Sprachen, sein Ausbleiben ohne Wiederzuordnung und der Fall ohne Delegat | `EPOS.UI.Tests/Dialoge/EnergietraegerDialogTests.cs` | **11** neue Fälle (65 → 76) |
| `Umbenennen`, `Neu`, `Variante`, `Loeschen`, `AusProjektEntfernen` gegen die Testdatenbank, einschließlich beider Verweigerungen (Anlage hält den Träger; die elektrische Welt hält ihn) | `EPOS.Kern.Tests/EnergietraegerKatalogCtrlTests.cs` (neu) | **11** Fälle |
| Der Delegat liefert den frischen Stand, der eingefrorene Wert nicht; ohne Wiederzuordnung bleibt der Hinweis leer; mit Wiederzuordnung nennt die Hülle den Träger; Hinweistext in beiden Sprachen; Knopftext de/en | `EPOS.Kern.Tests/EnergietraegerHuelleTests.cs` | **5** neue Fälle (24 → 29) |
| **Gegenprobe 1:** `ListeNachziehen` ausgehängt | `EnergietraegerDialogTests` | **7** Fälle rot — genau die sieben Schreibwege; wieder eingehängt, 76/76 grün |
| **Gegenprobe 2:** die Erkennung in `AusProjekt` ausgehängt | `EnergietraegerHuelleTests` | **1** Fall rot (`Das_Entfernen_nennt_den_wieder_zugeordneten_Stromtraeger`); wieder eingehängt, 29/29 grün |
| `Resource.Designer.cs` neu erzeugt | `Werkzeuge/ResourceDesigner` | 6 293 → 6 294 Einträge, +401 Zeichen, zweiter Lauf +0 (wiederholbar) |
| Referenzlauf | fünf Projekte gegen `2026-09-16_R8_Heizkessel_Kaskade` | byte-gleich — kein Rechenweg berührt |

### 8.4 Abnahmepunkte — A‑ET‑S2a (Windows, beide Kontexte)

1. **Projektkontext**, Projekt mit mehreren Trägern öffnen: „Entfernen" auf einem Träger, den
   keine Anlage hält → der Träger ist sofort aus der Liste, nichts ist markiert, die Karte
   rechts ist leer.
2. **Projektkontext**, Projekt mit Wärmepumpe/PV/Stromspeicher: Wird beim Entfernen ein
   Stromträger nachgezogen, steht über der Liste der ruhige Hinweis mit seinem Namen — und er
   steht in der Liste.
3. **Projektkontext:** „Aus Katalog übernehmen…", einen Träger wählen und bestätigen → er steht
   in der Liste UND ist markiert, die Karte gehört ihm.
4. **Katalogkontext** (Administration → Kosten → Energieträgerverwaltung…): „Neu…" und
   „Variante" → der neue Eintrag steht in der Liste und ist markiert; „Löschen" → er ist weg,
   nichts markiert, Karte leer.
5. **Katalogkontext:** Bezeichnung ändern, „Bezeichnung speichern" → der neue Name steht in der
   Liste und im Kartenkopf, die Markierung bleibt beim Träger.
6. **Beide Sprachen:** Der Knopf heißt „Bezeichnung speichern" bzw. „Save name" und damit
   anders als die beiden Übernahmeknöpfe.

### 8.5 Logbuch-Entwurf (Version beim Anwender offen) und Wiki

Zwei Sätze, je einer für eine sichtbare Änderung — mehr trägt der Eintrag nicht (Regel: Konzept
Hilfesystem 13.4):

> - In der Energieträgerverwaltung zeigt die Trägerliste nach jedem Schritt sofort den neuen
>   Stand: Ein angelegter oder übernommener Träger steht darin und ist ausgewählt, ein
>   umbenannter trägt seinen neuen Namen, ein entfernter oder gelöschter ist verschwunden.
> - Trägt das Programm beim Entfernen eines Trägers den Stromträger eines Projekts mit
>   elektrischen Anlagen wieder nach, sagt das jetzt ein Hinweis mit dessen Namen.

Der neue Knopftext „Bezeichnung speichern" bekommt **keinen** eigenen Satz — eine Beschriftung
ist eine Kleinigkeit im Sinne derselben Regel.

**Wiki-Quelle** `Projekte/Wiki/Programm Dokumentation - Kosten.wiki`, Abschnitt
„Energieträgerverwaltung": drei Stellen fortgeschrieben — der Hinweis zum nachgezogenen
Stromträger am Punkt ''Strom für elektrische Anlagen'', die Folge des Entfernens am Punkt
''Entfernen'', dazu zwei neue Punkte ''Katalogkontext'' (mit dem Knopf ''Bezeichnung
speichern'') und ''Die Liste folgt jedem Schritt''. Upload gebündelt und ausstehend.

### 8.6 Ohne Auftrag beim Lesen gefunden

- **Die Kennung ET‑5 ist doppelt vergeben.** Sie steht seit dem 08.09.2026 für die Frage nach der
  Zweistufigkeit (Abschnitt 7.5/7.6, erledigt); der Auftrag vom 16.09.2026 vergibt sie ein
  zweites Mal. Beide Reihen sind hier als `/S2a` unterschieden — für künftige Befunde dieses
  Dialogs ist **ET‑8** die nächste freie Nummer.
- **Der Wiederzuordner greift durch ein enges Tor.** `AusProjektEntfernen` lehnt einen Träger ab,
  den die elektrische Welt hält (ET‑4) — der Stromträger eines Wärmepumpenprojekts lässt sich
  also gar nicht entfernen, solange die Anlagen ihn beitragen. ET‑6 tritt deshalb nur ein, wenn
  die Anlagen ihren Träger SELBST wählen (ET‑5 vom 08.09.2026) und dieser Träger dem Projekt
  nicht zugeordnet ist: Dann hält niemand den zugeordneten Stromträger fest, sein Entfernen ist
  erlaubt, und `StromTraegerSicherstellen` legt ihn sofort wieder an. Genau dieser Stand ist im
  Prüffall hergestellt; ohne ihn bleibt das Banner aus — was der Gegenfall belegt.

## 9. ET‑8 (17.09.2026) — ohne gewählte Anlagenzeile engt das PROJEKT die Katalogübernahme ein

**Anwenderentscheid 17.09.2026 (ET‑E‑3, „Empfehlung") auf den Wunsch vom 16.09.2026:** „Die
Auswahl-Liste der Energieträger könnte auf die möglichen Energieträger (nach verfügbaren
Anlagen) eingeschränkt werden (Stromspeicher nur Strom, Heizung Heizöl nur Heizöl, …)."

### 9.1 Befund

| Nr. | Befund | Ursache |
|---|---|---|
| **ET‑8** | Die Einengung aus Abschnitt 7 greift nur, wenn in „Anlagenkomponenten" eine Zeile gewählt ist. Der Normalfall — Knopf ohne gewählte Zeile — öffnete die Verwaltung ohne jede Einengung: „Aus Katalog übernehmen…" bot den ganzen Katalog an, auch Träger, die keine Anlage des Projekts je beziehen kann | `KostenSeiteGaben.TraegerGaben` reicht ohne Zeile `Gaben(0, null, 0)`; `EnergietraegerZulaessigkeit` kannte bis dahin nur den EINZELfall (Erzeugerart + Gerät) und antwortet auf eine leere Erzeugerart mit „keine Einengung". Eine Aussage über die Anlagen eines ganzen Projekts gab es nicht |

### 9.2 Umsetzung

- **Kern — ein zweiter Weg, dieselbe Wahrheit.**
  `EnergietraegerZulaessigkeit.ZulaessigeGruppenFuerProjekt(projektId)` bildet die
  **Vereinigung** von `ZulaessigeGruppen(erzeugerart, geraeteId)` über alle Anlagen des
  Projekts. Anlagenquelle ist `ProjektEnergietraegerCtrl.AnlagenMitTraeger` (Komponente und
  Gerätezeile je Anlagenzeile); der Heizstab ist dort kein eigener Eintrag und kommt über
  `ProjektEnergietraegerCtrl.BrauchtStromTraeger` hinzu — dieselbe Bedingung, die auch die
  Stromträger-Automatik stellt. **Keine zweite Regel**: Die Kategorien je Anlage rechnet
  weiterhin `Kategoriecodes`, die Übersetzung in Gruppen `GruppenZuCodes`.
  Die Regeln stehen als Kommentar am Kopf des Weges:
  - Projekt ohne Anlage mit Träger (kein Projekt, keine Anlagen, nur Solarthermie und
    Puffer) → `null`, keine Einengung.
  - Liefert EINE Anlage `null` (Kessel, dessen Gerät keinen auswertbaren Brennstoff trägt) →
    die Vereinigung ist `null`: Was für eine Anlage offen ist, ist für das Projekt offen.
  - Anlagen ohne Träger (Solarthermie, Pufferspeicher) tragen nichts bei.
  - Sonst die Vereinigung der Gruppennamen, ohne Dubletten, in Katalogreihenfolge.
  - Bliebe nach der Einengung kein Katalogträger übrig → `null`, wie im Einzelfall.
  - Eine LEERE Liste gibt der Weg nie zurück: „das Projekt bezieht keine Energie" ist kein
    eigener Fall, sondern „keine Anlage mit Träger".
- **Hülle — die Einengung gilt der ÜBERNAHME, nicht der Liste.** `KontextSetzen` holt bei
  leerer Erzeugerart und `_projektId > 0` die Projektvereinigung nach `_projektGruppen`;
  `Freie()` filtert über `UebernahmeGruppen` (Komponente vor Projekt). Die linke Liste bleibt
  unberührt: Sie führt im Projektkontext die Träger des PROJEKTS
  (`KostenSummenCtrl.GetAllCarriers(projektId)`), und einen zugeordneten Träger zu verstecken,
  hieße eine vorhandene Zuordnung zu verschweigen — genau das, was Abschnitt 7 für den
  Einzelfall schon ausschließt. Mit Erzeugerart bleibt alles beim Einzelfall, im
  Katalogkontext (`_projektId <= 0`) gilt keine Einengung.
- **Anzeige — dieselbe Kopfzeile wie im Einzelfall.** `KontextText()` sagt jetzt auch ohne
  Erzeugerart, worauf eingeengt ist: „Kontext: Projekt 1030 — Übernahme eingeengt auf die
  Anlagen des Projekts: Gas, Wasserstoff". Ein neuer Schlüssel
  `KDLG_ET_KONTEXT_PROJEKTANLAGEN` in beiden Sprachen, kein neuer Baustein im Dialog.
- **Ohne Einengung bleibt die Kopfzeile, wie sie war** („Kontext: Projekt 19"), und die
  Übernahme vollständig.

### 9.3 Nachweise

| Was | Wo | Ergebnis |
|---|---|---|
| Je Regel ein Fall gegen die Testdatenbank: Projekt 19 (ohne Anlagen) und `projektId = 0` → `null`; Projekt 1024 (Elektrokessel, Öl‑BHKW, Wärmepumpe mit Heizstab) → Strom und Öl, sonst nichts; Projekt 1009 (drei Wärmepumpen, zwei Gaskessel) → genau die Vereinigung der beiden Einzelfälle, jede Gruppe einmal; Projekt 1027 mit Kessel ohne Brennstoff (in der Arbeitskopie gesetzt) → `null`; Projekt 19 mit einer Solarthermie-Zeile (in der Arbeitskopie angelegt) → `null` | `EPOS.Kern.Tests/EnergietraegerZulaessigkeitTests.cs` | **5** neue Fälle (14 → 19) |
| Ohne Erzeugerart ist `Freie()` auf die Vereinigung eingeengt und kleiner als der freie Katalog; der zugeordnete Träger außerhalb der Vereinigung (Strom in 1030) bleibt in der Liste und fehlt in der Übernahme; Katalogkontext bleibt frei; mit Erzeugerart bleibt der Einzelfall; Kopfzeile de/en; Projekt ohne Anlagen bleibt ohne Einengung | `EPOS.Kern.Tests/EnergietraegerHuelleTests.cs` | **7** neue Fälle (29 → 36) |
| Die Kopfzeile der Projekteinengung im Dialog, beide Sprachen aus derselben Ressourcenzeile | `EPOS.UI.Tests/Dialoge/EnergietraegerDialogTests.cs` | **1** Theorie, 2 Fälle |
| **Gegenprobe:** `ZulaessigeGruppenFuerProjekt` auf `null` festgenagelt | Kern- und Hüllenfälle | **7** Fälle rot (3 Kern, 4 Hülle) — die „keine Einengung"-Fälle blieben grün; wieder eingehängt, alles grün |
| `Resource.Designer.cs` neu erzeugt | `Werkzeuge/ResourceDesigner` | 6 294 → 6 295 Einträge, +377 Zeichen, zweiter Lauf +0 (wiederholbar) |
| Gate | Kern-Filter Release | 0 Fehler, 5 Warnungen (Bestand, keine neue; Schranke 7) |
| Gate | Windows-Schale (`EnableWindowsTargeting`) | 0 Fehler, 5 Warnungen (Bestand) |
| Gate | Tests, beide Kulturen (normal und `LC_ALL=en_US.UTF-8`) | `EPOS.Kern.Tests` 3 142/3 142 (+12), `EPOS.UI.Tests` 4 548/4 548 (+2), SpeicherEngine 370/370, KiKern 499/499, SpeicherPlanung 27/28 (1 übersprungen) |
| Gate | `SqlDialektPruefer` | 1 460 Texte, 0 Fundstellen |
| Referenzlauf | fünf Projekte gegen `2026-09-16_R8_Heizkessel_Kaskade` | 5/5 PASS — kein Rechenweg berührt, kein Schemaschritt, keine neue Basis |

### 9.4 Abnahmepunkte — A‑ET‑E3 (Windows)

1. **Projektkontext ohne gewählte Anlagenzeile** (Bericht & Kosten → Kosten →
   „Energieträgerverwaltung…", in „Anlagenkomponenten" nichts gewählt): Die Kopfzeile nennt
   „Übernahme eingeengt auf die Anlagen des Projekts: …" mit den Gruppen des Projekts.
2. Im selben Stand „Aus Katalog übernehmen…": Angeboten werden nur Träger dieser Gruppen —
   ein Gasprojekt bekommt keinen Stromträger zur Übernahme.
3. Die Trägerliste links bleibt vollständig: Ein dem Projekt zugeordneter Träger außerhalb
   der Gruppen steht weiter darin und lässt sich wählen und bearbeiten.
4. **Mit gewählter Anlagenzeile** ändert sich nichts: Kopfzeile und Übernahme folgen der
   Komponente („für Wärmepumpe: nur Gruppe Strom").
5. **Katalogkontext** (Administration → Kosten → Energieträgerverwaltung…): Kopfzeile
   „Kontext: Katalog (Stammdaten)", keine Einengung.
6. **Projekt ohne Anlage mit Energieträger** (nur Puffer/Solarthermie): Kopfzeile ohne
   Zusatz, Übernahme vollständig.
7. **Beide Sprachen:** Die Kopfzeile steht auch auf Englisch („Catalogue transfer limited to
   the project's systems: …").

### 9.5 Logbuch-Entwurf (Version beim Anwender offen) und Wiki

Ein Satz — mehr trägt der Eintrag nicht (Regel: Konzept Hilfesystem 13.4):

> - Ohne gewählte Anlagenzeile bietet die Energieträgerverwaltung unter „Aus Katalog
>   übernehmen…" nur noch die Energieträger an, die die Anlagen des Projekts beziehen
>   können; die Kopfzeile nennt sie.

**Wiki-Quelle** `Projekte/Wiki/Programm Dokumentation - Kosten.wiki`, Abschnitt
„Energieträgerverwaltung": der Einleitungssatz zum Knopf ohne gewählte Zeile und ein neuer
Punkt ''Einengung auf die Anlagen des Projekts'' (Anker `einengung-projekt`). Upload gebündelt
und ausstehend.

### 9.6 Stand des Restpunkts „Nach #268"

`KostenOeffnen`/`EnergiekostenOeffnen` in `HeizkesselHuelle`/`BhkwHuelle` sind **weiterhin
unbelegt** — diese Aufgabe berührt sie nicht: Sie ändert allein, was die Verwaltung ohne
Komponentenkontext anbietet, nicht, wer sie öffnet. Der Punkt bleibt offen. Ebenso bleiben
offen: „Tierische Fette" für das BHKW ohne Gerät, der UNIQUE-Index auf
`energy_project_settings` und der Anlegedialog der iOS-Variante.

### 9.7 Ohne Auftrag beim Lesen gefunden

- **Eine Gaskomponente engt auf ZWEI Gruppen ein.** `GASEOUS_FUEL` führt im Katalog „Gas" und
  „Wasserstoff"; ein Gaskessel bekommt beide angeboten. Das ist die gewollte Folge der Regel
  „gerechnet wird über den Kategoriecode, angezeigt über die Gruppe" (Abschnitt 7) und fällt
  in der Kopfzeile nun stärker auf, weil sie die Gruppen nennt.
- **Die nächste freie Befundnummer dieses Dialogs ist ET‑9.**

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

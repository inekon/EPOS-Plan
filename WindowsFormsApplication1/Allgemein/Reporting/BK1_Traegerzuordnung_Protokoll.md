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

## 5. Offener Anwenderentscheid: Emissions-Stammkopie (Altbefund)

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

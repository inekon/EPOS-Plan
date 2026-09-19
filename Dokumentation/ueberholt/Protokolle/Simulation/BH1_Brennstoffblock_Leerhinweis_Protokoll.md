# Auftrag BH-1 — BHKW rechnet 0 h/a, der Brennstoffblock meldet den falschen Grund

Stand: 19.09.2026 · Zweig `bh1` · Anlass: Anwenderbefund vom 19.09.2026 (Bildschirmbild des
Reiters BHKW der Simulationsseite, Projekt „Beispiel WP WG 1"): Wärmeproduktion 0,00,
Vbh thermisch 0 h/a, Modulzeile „EC-POWER XRGI 15 0,00 / 0,00", darunter das Warnbanner
**„Kein Brennstoff für dieses BHKW definiert."** — gelesen als „der zugeordnete Energieträger
Erdgas wird nicht erkannt".

**Kein Schemaschritt, kein Eingriff in den Rechenweg.** Referenzlauf byte-gleich.

## 1. Nachbau

Arbeitskopie der Testdatenbank ausserhalb des Repositoriums; im Projekt **1026** („Beispiel WP
WG 1", dort ohne BHKW) wurde das Katalog-BHKW **153** („EC-POWER XRGI 15", `Brennstoff` = 3
Erdgas E, Ptherm 30,8 kW, Pel 14,5 kW) auf dem Weg des Programms aufgenommen:
`EnergietraegerVarianteCtrl.Anlegen` (Variante „Erdgas" → `energy_carrier` 78),
`BHKWCtrl.CopyFromStamm` (Projektkopie `Tab_BHKW` 1018153), Anlagenzeile über
`WizardCtrl.SQL_ANLAGE_INSERT` — dieselbe Reihenfolge wie `BhkwHuelle.Aufnehmen`. Gerechnet
mit `SimulationRunner.SimuliereUndSpeichere`.

Zwei Läufe, weil das Bildschirmbild des Anwenders den BHKW-Reiter mit **einer** Modulzeile
zeigt — das setzt das BHKW in der Kaskade voraus:

| Lauf | Kaskade (`Tab_Einstellungen.Tool_1..4`) | Ergebnis |
|---|---|---|
| A | WP, Kessel, Solarthermie, — | `bSimulationBHKW` = false, Modulzeilen 0, **7** Laufmeldungen, darunter die Warnung „BHKW … ist im Projekt angelegt, aber nicht in der Kaskade" |
| B | WP, Kessel, Solarthermie, **BHKW** | `bSimulationBHKW` = true, **1** Modulzeile 0,00/0,00, 0 h/a, **6** Laufmeldungen — das Bild des Anwenders |
| C (Gegenprobe) | **BHKW**, Kessel, Solarthermie, WP | 1.725 h/a, 53,13 MWh Wärme, Gasverbrauch gebucht |

Lauf B, die sechs Meldungen: Wärmesenke ohne `Z_AnlageSenke`-Zeile für Anlage 14943
(Vorbelegung Heizkreis/Beides); Zeitbasis der Klimadaten; Kennlinienrand der Wärmepumpe in
957 Stunden; T_NOCT des PV-Moduls unplausibel; kein Temperaturgang am PV-Modul (gamma_PMP = 0);
kein Strom-Energieträger am Projekt samt drei Rückfallpreisen.

## 2. Befund

**(a) Der Brennstoff kommt an.** `BHKWCtrl.CopyFromStamm` überträgt die Spalte `Brennstoff`
1 : 1 in die Projektkopie (`EPOS.Kern/Controller/BHKWCtrl.cs`, Parameter `@brenn`); im Nachbau
trägt `Tab_BHKW` 1018153 `Brennstoff = 3`. Träger und Gerätebrennstoff sind **zwei Wahrheiten,
aber keine widersprüchlichen**: Die Auswahlliste der Variante ist auf die Brennstoffkategorie
des Stammsatzes eingeengt (`BhkwHuelle.TraegerVorbereiten` → `EnergietraegerVarianteCtrl
.KategorieZu`), gekoppelt ist also die *Kategorie*, nicht die Brennstoffnummer. Gelesen wird
getrennt: die **Brennstoffart** der Verbrauchsbuchung aus `Tab_BHKW.Brennstoff`
(`SimulationBHKW.cs`, `bhkwBrennstoffart`), **Emissionen** und **Wirtschaftlichkeit** vorrangig
aus `Tab_Energieanlagen.ID_Carrier` mit dem Gerätebrennstoff als Rückfall
(`Emissionsquelle.Fuer`, `WirtschaftlichkeitCtrl.AnlagenTabelle`/`BrennstoffKategorie`).
Kein Bruch.

**(b) Die 0 h/a sind fachlich richtig.** Das BHKW steht auf Kaskadenplatz 4 hinter Wärmepumpe
und Heizkessel. Die beiden decken den Wärmebedarf von 64,35 MWh/a vollständig (Lauf B: WP
51,28 MWh, Kessel 13,71 MWh, Restwärme 0,00); der Stufeneingang des BHKW ist 0
(`WaermebedarfGesamtKwh` = 0), damit gibt es nichts zu decken und nichts zu verbrauchen. Die
Gegenprobe C zeigt dasselbe Modul mit 1.725 h/a. Kein Rechenwegfehler, kein Eingriff.

**(c) Das Banner war der falsche Text.** Der Brennstoffblock des BHKW-Reiters führt nur Zeilen
mit Verbrauch > 0 (`SimulationErgebnisHuelle.Anzeige.cs`, `Bhkwbrennstoffe`). Bei 0 h/a bleibt
keine übrig, und `BhkwReiter.razor` meldete dafür den einen Text, den es gab. Er behauptet
einen Pflegefehler, den es nicht gibt.

## 3. Umsetzung

| Baustein | Inhalt |
|---|---|
| **Kern** | `BHKWStammCtrl.BrennstoffartenJeProjekt(idProjekt)` — die Schwester von `HeizkesselStammCtrl.BrennstoffartenJeProjekt`, derselbe Verbund über den Bezeichner, dialogfrei über `StilleDb`; leere Menge bei Lesefehler |
| **Hülle** | `SimulationErgebnisHuelle` belegt `KesselBrennstoffDefiniert` und `BhkwBrennstoffDefiniert` — „mindestens ein Modul des Projekts trägt einen gepflegten Brennstoff (> 0)". Die gepflegte 0 zählt nicht als Brennstoff |
| **Oberfläche** | `BhkwReiter.razor` und `HeizkesselReiter.razor` trennen bei leerem Block zwei Zustände: mit gepflegtem Brennstoff `WarnStufe.Hinweis` und der neue Text, ohne ihn der Bestandstext in `WarnStufe.Warnung` |
| **Ressourcen** | `SIM_MSG_BHKW_NICHT_GELAUFEN`, `SIM_MSG_KESSEL_NICHT_GELAUFEN` (de und en); `SIM_MSG_KEIN_BRENNSTOFF` und `SIM_MSG_KEIN_BRENNSTOFF_SPK` unverändert |
| **Tests** | `EPOS.Kern.Tests/SimulationErgebnisSqlTests` +2 (Projekt 1030 führt Erdgas H und Erdgas E), `EPOS.UI.Tests/Seiten/ErzeugerReiterTests` +4 (je Reiter beide Zustände) |

## 4. Offen — Befunde ohne Eingriff

**BH1-O1 — `Tab_BHKW.Wirkungsgrad` trägt zwei Einheiten** (zweiter Anwenderbefund 19.09.2026,
Projekt „BHKW Test München — groesserer Puffer“: Wärme 31,28 MWh, Strom 14,73 MWh,
Vbh 1 016 h/a, aber „Gasverbrauch (Hu): 1,56 MWh/a“).

*Die Formel.* `SimulationBHKW.Auswertung` bucht je Modul

```
ModulVerbrauch [MWh] = (s_waerme_MWh + s_strom_MWh) / bhkwWirkungsgrad
```

und legt ihn nach `bhkwBrennstoffart` (aus `Tab_BHKW.Brennstoff`; 1–5 und 14 → Gas) auf
`GasverbrauchBhkwMwh`. `bhkwWirkungsgrad` ist der **rohe Katalogwert**
(`bhkwWirkungsgrad[i] = ctrl.m_Wirkungsgrad`, `SimulationBHKW.cs` im Ladeteil) — ohne die
Division durch 100, die derselbe Ladeteil für `Grenzleistung` ausdrücklich vornimmt.

*Die Probe geht auf.* (31,28 + 14,73) / 29,5 = **1,56** — genau die Zahl des Bildes; im Nachbau
(Gegenprobe C) ebenso: (53,131 + 25,013) / 29,5 = **2,65**. Die Einheiten stimmen sonst
überall: Vbh 1 016 h × 30,8 kW = 31,3 MWh und × 14,5 kW = 14,7 MWh — das Modul läuft Volllast,
die Produktionszahlen sind richtig. Falsch ist allein der Teiler.

*Was 29,5 wirklich ist.* Der Katalogsatz nennt das Feld „Ges. Wirkungsgrad“, der Hinweis in der
Maske „(z. B. 0,85)“ — gemeint ist ein **Faktor** zwischen 0 und 1. 29,5 passt dazu nicht,
wohl aber zum **elektrischen** Wirkungsgrad in Prozent: 14,5 kW / 0,295 = 49,15 kW Brennstoff,
damit thermisch 30,8 / 49,15 = 62,7 % und gesamt 92,2 % — der Wert, den ein Erdgas-BHKW hat.
Die Nachmessung des ganzen Katalogs zeigt zwei sauber getrennte Gruppen: von 79 Sätzen mit
gepflegter Leistung tragen **40 einen Faktor** (0,827 bis 0,989, Median 0,909) und **38 einen
Prozentwert** (bis 44,3); rechnet man für die 38 `(Ptherm + Pel) × W/100 / Pel`, liegen 29 im
Band 0,80–1,00 (Median 0,876). Der Heizkessel kennt das Problem nicht: `Wirkungsgrad_Gas` ist
dort durchweg ein Faktor (im Nachbau 1,0 — die 100 % Jahresnutzungsgrad des Anwenderbildes sind
also die gepflegte Angabe, kein Rechenfehler).

*Das Ausmass.* Der Teiler ist 29,5 statt 0,9218 — der Verbrauch fällt um den Faktor **32,0** zu
klein aus, nicht um 100. Erwartbar wären im Bild des Anwenders 46,01 / 0,9218 = **49,9 MWh/a**
(gleichbedeutend 1 016 h × 49,15 kW). Mit dem Verbrauch hängen `Gasspitze_BHKW`, die fünf
Emissionssummen und jede Brennstoffkostenzeile an derselben Grösse.

*Warum hier nichts geändert wird.* Jede Berichtigung — Einheit im Katalog vereinheitlichen,
beim Lesen an der Grössenordnung erkennen, oder die Spalte als elektrischen Wirkungsgrad
ausweisen und den Gesamtwirkungsgrad daraus rechnen — ändert den Rechenweg. Projekt **1030**
der Referenzbasis fährt ein betroffenes Modul (XRGI 9, 29,3), Projekt 1017 ein unbetroffenes
(0,915); die Basis wäre neu einzufrieren. **Anwenderentscheid.**

**BH1-O1b — Kessel: Produktion 16,79 gegen Deckung 15,97 MWh/a.** Die beiden Zahlen messen
Verschiedenes, und der Unterschied ist genau benannt:

```
Deckung [MWh] = EigenanteilKesselMwh
              = SWaermeSpkMwh − SpeicherladungGesamtKwh/1000 + Speicherentladung_Anteil/1000
```

(`SimulationRunner.EigenanteilKesselMwh`; `SimulationErgebnisCtrl.Heizkessel` setzt
`WaermeproduktionMwh = SWaermeSpkMwh` und `DeckungProzent` auf den Eigenanteil gegen den
Projektbedarf — 15,97 / 46,88 = 34,07 %). Die 0,82 MWh sind also der Teil der Kesselproduktion,
der in den Puffer ging und nicht als Bedarfsdeckung zurückkam: Speicherverluste, der am
Jahresende im Puffer verbliebene Rest, und der Anteil der Entladung, den die Herkunftsrechnung
dem **BHKW** gutschreibt (Interimsregel „Vermischung im Speicher“,
`SimulationSPK.Speicherentladung_Anteil`, gefüllt von der `Kaskadenschleife`). Dass der Befund
gerade am Projekt „groesserer Puffer“ auffällt, passt dazu: Ein grösserer Speicher verliert
mehr und bindet am Jahresende mehr. Ohne Puffer-Senke sind beide Summanden exakt 0 und
Deckung = Produktion — so im Nachbau (Kessel 11,223 = 11,223). Kein Fehler.

**BH1-O2 — das Kesselbanner ist unerreichbar.** `Kesselbrennstoffe` blendet bei völlig
unbekanntem Brennstoff **alle zehn** Zeilen ein (Rückfall „nichtsBekannt"), sonst mindestens
eine; `SichtbareBrennstoffe == 0` tritt nur ein, wenn der Reiter gar keine Zahlen hat — und
dann steht er nicht. Der Kesselzweig ist mitgeführt, damit beide Reiter dieselbe Aussage
treffen, nicht weil er heute greift.

**BH1-O3 — die Senkenwarnung des aufgenommenen BHKW.** Ein frisch aufgenommenes BHKW bekommt
keine Zeile in `Z_AnlageSenke`; der Lauf meldet das und rechnet die Vorbelegung
Heizkreis/Beides. Das ist Bestand und war hier ohne Wirkung (das Modul lief ohnehin nicht).

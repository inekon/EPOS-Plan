# FX1 — Befund-Fixes der Rechenwege-Formelkarte: I-1 (kWp) und B-1/N1 (Kesselmeldung)

Anlass: Anwenderdurchsicht der Rechenwege-Formelkarte (Artifact „Rechenwege der
Wirtschaftlichkeit", 30.08.2026) vor der Dialog-Etappe B5. Zwei Entscheide:
**I-1** „EUR_PRO_KWP ist die spezifische Leistung eines Moduls; PV_Leistung =
Modulanzahl × Modulleistung — korrigiere" und **B-1/N1** „wenn Kesselbrennstoff
fehlt, Meldung". Stand 02.09.2026, Branch `ios_migration`-Ära (SQLite-Betrieb).

## 1. I-1 — `EUR_PRO_KWP` bemisst jetzt echte kWp

Die eine kWp-Wahrheit liegt in `PhotovoltaikCtrl.KwpSumme(idProjekt, idAnlage)`
(neuer Kern, `internal`; `KwpDesProjekts` delegiert mit `idAnlage = 0`):
**kWp = Σ(Tab_PV.Leistung [W] × Modulanzahl) / 1000**. `TechnikPlanwertCtrl.
BaugroesseSumme` ruft für `EUR_PRO_KWP` (Komponente 3) diesen Kern; der alte
Zweig, der die rohe Spalte `Tab_Energieanlagen.PV_Leistung` summierte (faktisch
die **Modulanzahl**), ist entfernt.

**Nachweise** (Harness `..\dev\i1n1\`, frische SQLite-Kopie): Projekt 1007
(Ablytek 6MN6A270, 270,64 W × 20): Handrechnung **5,4128 kWp** ==
`KwpDesProjekts` == `BaugroesseSumme` neu (vorher **20** = Modulanzahl, Faktor
3,695); anlagenscharf identisch; präparierte Zwei-Anlagen-Probe trennscharf
(5,4128 + 5,2000 = 10,6128 projektweit, fremde Anlage null); Kreuzprüfungen und
die übrigen fünf Gerätewelt-Arten zeilengleich. **Bestandsneutral**: 0 Zeilen
mit `EUR_PRO_KWP` und Satz > 0 (Kat. 1 und 2); Anker Betrieb 1024 = 99,00,
Invest 1018/1024/1042 exakt. Nebenentscheid: Der Kern filtert `ID_Type = 3`
(PV) statt `ID_PV > 0` — im Bestand wirkungsgleich, dokumentiert.

## 2. B-1/N1 — fehlender Kesselbrennstoff wird gemeldet (nur Meldung)

`KostenEmissionRechner` setzt `VariantenDaten.KesselVerbrauchFehlt` +
`KesselOhneVerbrauch` (Namensliste), wenn ein Heizkessel-Modul
`Waerme_Gas + Waerme_Oel > 0` bei `Verbrauch <= 0` trägt (der Rechenkern füllt
das Feld nie — 20 Modulzeilen in 13 Projekten betroffen). Meldung an beiden
Kanälen nach dem Strommix-Muster: Berichtswarnung (`BerichtsDatenSammler`) und
Wirtschaftlichkeits-Hinweiszeile (`RechneProjekt`, `Anhaengen` +
GetString-Rückfall `WIRT_KESSELBRENNSTOFF_FEHLT`). `kostenVollstaendig` bleibt
unangetastet — **kein** Abbruch, **keine** Zahlenänderung.

**Nachweise**: 1024 nachher Fahne True, Text an beiden Kanälen wörtlich belegt;
**alle Kennzahlen zeichengleich vorher/nachher** (1024 KW −2.219.863,7615;
1030 KW −21.875.243,6757; 51 Diffzeilen der Protokolle restlos aus Kopf-,
Baugrößen- und Meldezeilen); Gegenproben: Projekt ohne Kessel keine Fahne;
Kopie mit gefülltem Verbrauch keine Fahne — und als Größenordnung der Lücke:
1042 CO₂ 8,30 → 28,75 t/a, BEHG 0 → 1.329,12 €/a, wenn der Verbrauch vorläge.

## 3. Umgebungshinweise (SQLite-Ära, 02.09.2026)

- Build: `dotnet build ..\WP-Plan.sln -c Debug -p:Platform=x64` (Standard seit
  iU1-P1.1); Exit 0, Warnungsbild unverändert (39).
- Produktiv ist `Kenndaten.sqlite`; Harnesse: **ProjectReference statt
  DLL-HintPath** (sonst SqliteConnection-/OleDb-RID-Fehler) und
  `DataRepository.EngineModus()` gegen modale Dialoge — im Harness dokumentiert.
- Anker-Aktualisierung: 1024 KW = **−2.219.863,76** (Datenstand; vorher==nachher
  bewiesen), 1042 ohne KW („Energiekosten nicht bestimmbar") — 1024/1030 als
  KW-Anker verwenden.
- Modulnamen tragen teils U+FFFD aus der Access→SQLite-Migration (Datenlage; die
  Meldung reicht Namen nur durch).

## 4. Geänderte Dateien

```
Controller/PhotovoltaikCtrl.cs             KwpSumme-Kern (eine kWp-Wahrheit)
Controller/TechnikPlanwertCtrl.cs          EUR_PRO_KWP über KwpSumme, alter Zweig entfernt
Allgemein/Bericht/BerichtsDaten.cs         KesselVerbrauchFehlt + KesselOhneVerbrauch
Allgemein/Bericht/KostenEmissionRechner.cs Fahne in der Mengensammlung
Allgemein/Bericht/BerichtsDatenSammler.cs  Berichtswarnung
Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs  Hinweis-Anschlusszeile
Allgemein/Reporting/FX1_Rechenwege_Befunde_Protokoll.md dieses Protokoll
```

Text-Key `WIRT_KESSELBRENNSTOFF_FEHLT` (de+en) → nächster resx-Sammelnachtrag.
Folgepaket FX2 (Anwenderentscheide I-2, I-3, I-6, B-4) schließt an.

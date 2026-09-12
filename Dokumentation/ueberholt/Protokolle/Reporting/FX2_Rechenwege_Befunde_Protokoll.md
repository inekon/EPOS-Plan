# FX2 — Befund-Fixes I-2, I-3, I-6, B-4 (Umsetzungsprotokoll)

Fortsetzung von FX1 (Anwenderdurchsicht der Rechenwege-Formelkarte). Vier
Entscheide vom 30.08./02.09.2026. Stand 02.09.2026 spät, Branch `ios_migration`
(SQLite-Betrieb). **Enthält die Heilung des Sync-Zwischenfalls 20a0636** (§ 5).

## 1. I-2 — „wenn eine Bemessungsart 0 ergibt, nimm den erfassten Wert"

`BetriebskostenCtrl.Betrag`: Der Nicht-rechenbar-Zweig (Menge ODER Satz fehlt)
liefert statt 0 jetzt den **erfassten `EingegebenerWert`** (Erlösklemme läuft
weiter). Eine **echte** Ableitung mit Menge 0 bleibt 0 („ermittelt und null").

Nachweise: Bestand hat **0** Zeilen mit abgeleiteter Art und Wert > 0 bei
fehlender Basis (alle 10 Artengruppen gezählt) — **keine Bestandsdifferenz**,
Projekt-für-Projekt-Vergleich zeichengleich. Proben: Endenergie-Zeile ohne Lauf
mit Wert 1.234 → vorher 0, nachher **1.234,00** (gewollt); Investzeile
„je kW Heizleistung" ohne Satz, Wert 13.000 → vorher 0, nachher **13.000,00**;
Gegenprobe Satz 653,60 mit Menge 0 → **0 in beiden Ständen**. Konsequenz für
das Konzept: § 4.5-Satz „ohne Lauf keine Menge, kein Betrag" gilt nicht mehr
uneingeschränkt — vom Anwenderentscheid gedeckt, Konzeptnachzug offen.

## 2. I-3 — %-der-Investition: eingefrorene Basis, deterministisch

`LiesInvestitionen` Runde 3 ist zweiphasig: Die Stufen-Basen (Anlage/
Komponente/Projekt über die abgeleiteten Nicht-Zuschuss-Zeilen der Runden 1+2)
werden **vor** der Schleife eingefroren; `PROZENT_INVESTITION`-Zeilen rechnen
einander **nie** ein. Jede Zeile bleibt eigene ValERI-Position mit eigener
Nutzungsdauer (unverändert).

Nachweise: H4b-Kaskadenanker exakt (A 16.993,60 + B 849,68 + C 3.084,328 =
Delta 20.927,608, vorher == nachher); Zwei-%-Zeilen-Probe: zweite 10 %-Zeile
rechnete vorher die erste ein (D = 3.392,76), jetzt beide auf der eingefrorenen
Basis (je **3.084,328**, Differenz −308,43 = die beseitigte %-auf-%-Einrechnung);
Reihenfolgeprobe (20 %/10 % vertauscht): Differenz **0,0000** in beiden
Richtungen — deterministisch.

## 3. I-6 — Kaskade auf nicht migrierter DB: bereits gelöst, jetzt bewiesen

Befund überholt: `LiesInvestitionen` ruft die tolerante Vorsorge
`KostenPositionCtrl.StelleSpaltenSicher()` seit K5 (918f8f5f, 20.08.2026) im
selben Muster wie die Betriebsseite. Nachweis per DROP COLUMN an der Kopie:
Vorsorge legt die fünf Spalten wieder an, Kaskadenzeile rechnet (+1.300,00),
Anker unverändert. Restnotiz: Der Prozess-Cache `_spaltenBereit` heilt keinen
Spaltenverlust **während** eines Programmlaufs (kein Bestandsfall).

## 4. B-4 — „je Stunde": Satz fest, Menge frisch aus dem Lauf

Anwenderentscheid wörtlich: der Satz [€/h] ist fester Wert, nur die Menge
kommt aus dem Lauf (Muster `EUR_PRO_KWH_ELEKTRISCH`). `EUR_PRO_H` ist jetzt
Rückfall-ermittelbar; `EndenergieAufloeser.BetriebsstundenH(komponente,
idAnlage)` liefert:

| Komponente | Quelle | Charakter |
|---|---|---|
| Wärmepumpe (1) | `Tab_ErgebnisWaermepumpeModul.Betriebsstunden` | **echte Bh** (56 Zeilen, 800…6.938 h/a) |
| BHKW (7) | `VbhThermisch` | **benannte Näherung** (keine Bh im Modell; dieselbe Größe wie `BEZUG_VBH_BHKW` seit E3) |
| Heizkessel (2), übrige | — | **null** (kein Stundenfeld — nichts geraten) |

Nachweise (P 1019): anlagenscharf 6.158,14 h × 50 €/h = **307.907,00** exakt;
Komponentensumme 11.190,66 × 50 = **559.533,00** exakt; Konserve 1,00 h wird
von frisch verdrängt; `MengeAusweisen` schreibt 6.158,14; BHKW-Probe 997,98
(VbhThermisch); Kessel unverändert. Bestand: **0** `EUR_PRO_H`-Zeilen →
keine Bestandsdifferenz; Anker zeichengleich (Betrieb 1024 = 99,00, KW 1024 =
−2.219.863,7615, KW 1030 = −21.875.243,6757, Invest ×3). `MengenEinheit`
liefert für die Art bereits „h/a", `SatzEinheit` „€/h" — nichts zu ändern.

## 5. Sync-Zwischenfall 02.09. abends — und seine Heilung

Während der FX2-Arbeit lief `GitHub_Sync.bat`: a8b8013 + Merge des
iU6-Stands 981cb84 (`OleDbParameter` → `DbParam`) mit **zwei Konflikten in den
FX1-Dateien** `PhotovoltaikCtrl.cs`/`TechnikPlanwertCtrl.cs`; Commit
**20a0636** nahm die **Konfliktmarker im Quelltext** und den halbfertigen
FX2-Stand per `add -A` mit (Baum unübersetzbar; gepusht nur nach
`origin/lokal_dirk`, `origin/ios_migration` blieb sauber). Die Konflikte
wurden im Arbeitsbaum aufgelöst — FX1-Semantik (anlagenscharfer
`KwpSumme`-Kern) mit den `DbParam`-Trägern von iU6, im Code als
Konfliktauflösung kommentiert und per Build + `BaugroesseSumme = 26,0000 kW`
belegt; **dieser Commit heilt 20a0636** (Marker-frei, Sweep 0 Treffer). Die
Auflösung wurde stichprobengeprüft; formale Gegenlese durch die
iU6-Session bleibt empfohlen. Erneuter Beleg für die bekannte
`add -A`-Falle des Syncs.

## 6. Geänderte Dateien (dieser Commit)

```
Controller/BetriebskostenCtrl.cs            I-2 (Nicht-rechenbar → erfasster Wert)
Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs  I-3 (Basis eingefroren), B-4 (EUR_PRO_H)
Allgemein/Wirtschaftlichkeit/EndenergieAufloeser.cs     B-4 (BetriebsstundenH)
Allgemein/DbWerte.cs                        B-4-Doku an BEMESSUNG_EUR_PRO_H
Controller/PhotovoltaikCtrl.cs              Konfliktheilung 20a0636 (FX1 × iU6)
Controller/TechnikPlanwertCtrl.cs           Konfliktheilung 20a0636 (FX1 × iU6)
Allgemein/Reporting/FX2_Rechenwege_Befunde_Protokoll.md dieses Protokoll
```

Hinweis: Teile von I-2/B-4 stecken bereits im Sync-Commit 20a0636 (halbfertig
eingesammelt) — dieser Commit stellt den vollständigen, gemessenen Stand her.
Offen: B-4 für `EUR_PRO_KWH`/`PROZENT_BRENNSTOFF-`/`STROMKOSTEN` (Konserve),
`MengeAusweisen`-Riegel für `EUR_PRO_H` in Kategorie 1 (Bestand 0, Katalog
sperrt die Art für Invest — Anwenderentscheid bei Bedarf), Konzeptnachzug
§ 4.5 (I-2). Harness (gitignored): `..\dev\fx2\`.

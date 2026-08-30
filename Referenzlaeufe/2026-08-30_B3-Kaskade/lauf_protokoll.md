# Basis 2026-08-30_B3-Kaskade — Laufprotokoll

**Anlass: Datenänderungen des Anwenders an 1030 und 1042 — die Zwei-Modul-Kaskade in
Projekt 1030 ist wiederhergestellt.** Das zweite BHKW von 1030 (Anlage 14921) hatte keine
Senkenzeile und lief deshalb nicht mehr in der Kaskade mit; der Anwender hat es in
`Z_AnlageSenke` wieder eingehängt und dabei zugleich das Gerät gewechselt. Projekt 1042
ist auf diesem Datenbestand nach der Löschung vom 28./29.08.2026 neu aufgebaut worden und
trägt eine andere Ausstattung als das gleichnamige Projekt der Vorbasis. Die Vorbasis
`2026-08-29_Booster` stammt vom **Zweitstand** und passt damit nicht mehr auf diesen
Rechner.

## Eckdaten

- **Codestand:** `bad41f8` (Branch `Pufferspeicher`, B3-Serie), unverändert, gebaut aus
  einem `git archive HEAD`-Export außerhalb des Repos (`C:\Waermeplan\_basisbuild`);
  MSBuild x64 Debug, **0 Fehler**, 5 bekannte Bestandswarnungen (CS0108/CS0109/CS1998).
- **Datenquelle:** produktive `Kenndaten.accdb`, Zeitstempel **30.08.2026 06:13:43**,
  151 949 312 Bytes, **Schemastand 61**, ausschließlich gelesen (keine `Kenndaten.laccdb`,
  kein Anwendungsprozess).
- **Arbeitskopie:** `C:\Waermeplan\_basislauf\DB`, per `migration`-Modus erzeugt; die
  Migration war ein reines No-op (Quelle stand bereits auf 61 = Zielstand 61). Bekannter
  Schema-Nachweis-Befund: zwei Altlastzeilen `WS_Ziel = PufferHeizung` ohne
  `WS_ID_Puffer` in **Projekt 1027** — außerhalb der Referenzmenge, unverändert
  gegenüber den Vorbasen.
- **Lauf:** Weg B, 13 Projekte im `projekt`-Modus (feste Liste), 2 × 13 Läufe, alle
  **Exit-Code 0**, **332 CSV**, keine `FEHLER`-Zeile.
- **Selbstvergleich:** `vergleich` **13/13 PASS (3 558 333 Werte)** und **332/332
  byte-/MD5-gleich** — die Basis ist reproduzierbar.
- **`pruefen`:** 13/13 plausibel, keine NaN/Inf; vier Hinweise „Gewerk aktiviert, aber
  kein Modul zugeordnet" (1007 Solar, 1041 PV, 1042 PV + Solar) — Bestand.

## Die feste Projektliste (dreizehn IDs)

```powershell
& $exe lauf --ziel <ordner> --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030,1039,1040,1041,1042
```

Der Bestand führt inzwischen auch **1043 „Booster-Kette mit Kombi-Speicher (2)"** und
**1044 „Booster-Kette mit Kombi-Speicher - Schichtspeicher"**. Sie sind **bewusst nicht**
Teil dieser Basis: Ihre Aufnahme wäre ein eigener, bewusster Basiswechsel und hätte den
Vergleich gegen `2026-08-29_Booster` unmöglich gemacht.

## Zuordnung gegen `2026-08-29_Booster`

**297 von 332 byte-/MD5-gleich, 35 abweichend.** Die 35 zerfallen in drei sauber
getrennte Gruppen:

| Gruppe | Dateien | Ursache |
|---|---|---|
| Sieben `aggregate.csv` (1017, 1018, 1023, 1024, 1039, 1040, 1041) | 7 | **neue Ergebnisspalte `Hilfsenergie`** (Migrationsschritt 61, Etappe B3 Paket a). Zeilendiff je Datei: **ausschließlich** die eingefügten Zeilen `BHKWModul[i].Hilfsenergie;0` und/oder `HeizkesselModul[0].Hilfsenergie;0` — kein Altwert weicht ab. |
| `Projekt_1030` | 7 | **Datenänderung des Anwenders** (zweites BHKW-Modul) |
| `Projekt_1042` | 21 | **Datenänderung des Anwenders** (Projekt neu aufgebaut) |

Der Toleranzvergleich, der die neue Spalte ausdrücklich ausnimmt, trennt das eindeutig:

```
vergleich 2026-08-29_Booster <neu> --ohne BHKWModul[0].Hilfsenergie,BHKWModul[1].Hilfsenergie,HeizkesselModul[0].Hilfsenergie
  -> 1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024, 1039, 1040, 1041 : PASS  (elf Projekte)
  -> 1030 : FAIL (55 338 Abweichungen)
  -> 1042 : FAIL (155 035 Abweichungen)
```

**Elf der dreizehn Projekte sind wertgleich** — der Rechenkern ist zwischen `0787aec`
(B3) und `bad41f8` (B3-Serie) unverändert. Die beiden FAIL sind vollständig
Datenänderungen zugeordnet.

### Projekt 1030 — die Kaskade rechnet wieder zweimodulig

`Modul[0]` ist **wertgleich zur Vorbasis** (605,52 MWh / 373,78 MWh / Vbh 7 475,59 /
7 475,69). Verändert hat sich ausschließlich `Modul[1]`:

| Größe | Booster-Basis | diese Basis |
|---|---|---|
| `BHKWModul[1].Modul` | Agenitor 306 (250 kW el) Gas | **EC-POWER XRGI 9** |
| `BHKWModul[1].Waermeproduktion` | 1 561,69 | 130,70 |
| `BHKWModul[1].Stromproduktion` | 1 346,29 | 58,52 |
| `BHKWModul[1].VbhThermisch` | 5 385,13 | 6 502,36 |
| `BHKWModul[1].VbhElektrisch` | 5 385,16 | 6 502,04 |
| `BHKW.Waermeproduktion` (Stufe) | 2 167,21 | 736,22 |
| `BHKW.Stromproduktion` (Stufe) | 1 720,08 | 432,30 |
| `BHKW.Waermebedarfsdeckung` | 35,3 % | 11,99 % |

Die Senkenzeilen des zweiten Moduls (Anlage 14921) stehen in `Z_AnlageSenke` als
**Rang 1 = Heizkreis** (ohne Puffer) und **Rang 2 = PufferHeizung, Puffer 1054170,
Ladeprio 2**; die führende Anlage 14920 trägt Rang 1 = PufferHeizung. Die Engine meldet
das im Lauf ausdrücklich:

> `Simulation Warnung: BHKW: Die Anlage 14921 hat eine andere Wärmesenke als die führende
> Anlage 14920. Die Fahrweisen schalten alle Module gemeinsam zu; für die gesamte
> BHKW-Stufe gilt deshalb die Senke der führenden Anlage.`

**Was die Kaskadenabdeckung angeht:** beide Module bleiben Erdgas und unter der
500-kW-Ausschreibungsgrenze, die drei Vollbenutzungsstunden-Aggregate bleiben belegt —
die Pfade aus Etappe E2 und die KWKG-Guards sind weiter abgedeckt. Die **Zahlen** dieser
Abdeckung sind durch den Gerätewechsel 250 kW el → 9 kW el aber andere als in allen
Vorbasen; die früheren Werte (u. a. Summe thermisch 12 860,72 h) gelten nicht mehr.

### Projekt 1042 — auf diesem Bestand neu aufgebaut

Das 1042 dieser Basis ist nicht das 1042 der Booster-Basis: Der Heizkessel ist ein
anderer (`Vitocrossal 200 CM2` → `ecoTEC plus VC 1206/5-5`), und die drei Speicher sind
andere Geräte in anderer Rolle (`Stora B 1000-6 ER 1 B`/Brauchwasser → `Puffer
3000Ltr`/Heizung an Position 0; an Position 2 steht jetzt `allSTOR exclusiv VPS
1000/3-7`/Brauchwasser). Die beiden Wärmepumpenmodule (`CS7800iLW 16`, `CS6800iAW MB +
AW 10 OR-T`) sind dieselben. Die Booster-Kette bleibt damit als Kategorie besetzt, ihre
eingefrorenen Zahlen sind aber neu.

## Was diese Basis nicht absichert

- Wirtschaftlichkeit (grundsätzlich, siehe LIESMICH) — `WirtschaftlichkeitCtrl` wird
  nicht aufgerufen; die neue Spalte `Hilfsenergie` steht in allen Projekten auf **0** und
  ist damit im Vergleich enthalten, aber nicht mit einem Wert ungleich null abgedeckt.
- Lesepunkt „Danach" (nur der Default „Davor" ist eingefroren).
- Kessel-Quellkopplung mit Wert ≠ 0 (unverändert).
- Die Projekte **1043** und **1044** (Booster-Varianten mit Kombi- bzw. Schichtspeicher)
  stehen im Bestand, sind aber nicht Teil der Referenzmenge.
- Die früheren 1030-Kaskadenzahlen mit dem 250-kW-Modul; letzte Quelle dafür ist
  `2026-08-29_Booster/Projekt_1030/`.

## Bekannte Bestandswarnungen des Laufs

Alle Warnungen stammen aus den eingeführten Familien: Energieträger-Zuordnung
(`ID_Carrier` leer, 7 ×), Speicher-Registry ohne Temperaturpaar (Rückfall ΔT = 10 K,
10 ×), Sole-Wasser-Wärmepumpe ohne konfigurierte Quelle (4 ×), Erzeuger-Vorlauf unter
dem Vorlauf des Zielspeichers (4 ×), Schichtmodell/`T_Nutz_BW` über wirksamem Vorlauf
(2 ×), Zweitsenke ohne Puffer (1 ×), Quellspeicher wird von keiner Anlage geladen (1 ×).
**Neu gegenüber der Vorbasis ist genau eine Warnung** — die oben zitierte
Senkenabweichung 14921/14920 in Projekt 1030.

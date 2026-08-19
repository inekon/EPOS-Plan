# Referenzlauf-Protokoll — Basis B6

**Zeitpunkt:** 19.08.2026, 17:16 Uhr · **Werkzeugprotokoll des Laufs:**
[`lauf_protokoll_werkzeug.md`](lauf_protokoll_werkzeug.md)

**Anlass: der Abschluss der Ausbaustufe W4 (Etappe E8, Abnahme).** Seit der Basis `2026-08-19_B5`
sind die Etappen **E3 bis E7** entstanden (Migrationsschritte 19 bis 22, Kostenarten und
Betriebskosten nach VDI 2067, Energie- und Stromsteuergutschriften, Tarif-Rollenmodell, KWK-Zuschlag
je Modul, Mehrjahrestabelle im Bericht) und der parallele Entwicklungsstrang **KI-Assistent-
Aufgabensteuerung** ist zusammengeführt worden. Keine dieser Etappen hat den Rechenkern angefasst;
B6 friert denselben Zahlenstand wie B5 unter dem neuen Code- und Schemastand ein, damit eine
künftige Abweichung zweifelsfrei einer Folgeänderung zugeschrieben werden kann.

**Codestand:** `e94be10` („Merge origin/main: KI-Assistent-Aufgabensteuerung neben Ausbaustufe W4
(E4 bis E7)"), unverändert, Arbeitsbaum sauber. Gebaut in einem eigenen Export des Commits außerhalb
des Repos (`C:\Waermeplan\_e8`, `git archive HEAD`), VS-MSBuild x86/Debug über
`Referenzlauf\Referenzlauf.csproj` (ProjectReference auf die App → Exe und DLL konsistent). Der
Haupt-Checkout und dessen `bin\` wurden **nicht** angefasst. Build: **0 Fehler, 6 Bestandswarnungen**
(CS0108 ×2, CS0109 ×2, CS1998, CS4014) — die drei mit dem Merge hinzugekommenen Projekte
(`KiKern`, `KiKern.Tests`, `KiHarnisch`) bringen keine eigene Warnung mit.

**Quelle (produktiv, nur gelesen):** `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`,
Zeitstempel **19.08.2026 14:46:27**, 92 700 672 Byte, MD5
`0873B892ADFEE0DC266DBC0814EB93A7`, Schemastand **21**. Keine `Kenndaten.laccdb` vorhanden, die
Anwendung hatte die Datenbank also nicht geöffnet. Gerechnet wurde auf der Arbeitskopie, die der Lauf
selbst zieht — sie lag unter `C:\Waermeplan\_e8\Referenzlaeufe\Arbeitskopie\`, nicht im Repo.
**Die Migration 21 → 22 lief ausschließlich auf der Kopie** (`Schemastand vorher: 21 → nachher: 22`;
Schritt 22 legt acht Spalten an `Tab_Energieanlagen` an, ohne DML). Nachkontrolle nach allen Läufen
und Proben: Zeitstempel, Größe und MD5 der produktiven Datei sind **unverändert**, weiterhin keine
`Kenndaten.laccdb`.

> **Die Quelldatenbank ist gegenüber B5 nicht dieselbe Datei — die Ergebnisse sind es trotzdem.**
> B5 rechnete auf dem Stand vom 19.08.2026 02:51 (96 436 224 Byte, MD5 `66F4806A…`, Schemastand
> **17**). Zwischen beiden Läufen hat die Sitzung des Anwenders die Migrationsschritte **18 bis 21**
> auf der produktiven Datei ausgeführt und sie um 14:46 über „Komprimieren und reparieren"
> neu geschrieben. Der Vergleich unten zeigt, dass davon **kein einziger Wert** der Referenzmenge
> berührt ist: Die Schritte 18 bis 21 sind additives DDL mit ergebnisneutraler Vorbelegung, und der
> Rechenkern liest keine der neuen Spalten.

## Projektmenge: neun Projekte, **fest vorgegeben**

Unverändert gegenüber B5: `--projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030`. Die Begründung
für die feste Liste steht in [`../LIESMICH.md`](../LIESMICH.md) — die automatische Auswahl ist
datengetrieben und wandert mit dem Projektbestand.

| ID | Projekt | Ausstattung (Kurzform) | CSV | Werte | Status |
|---|---|---|---|---|---|
| 1007 | Laurentiuskirche | WP, PV, Batterie, Kessel, Puffer | 29 | 324 210 | OK |
| 1008 | Heinestr 15 | WP, Kessel, Puffer(WP) | 21 | 227 847 | OK |
| 1011 | test1 | WP, Solar, PV, Batterie, Kessel, Puffer | 29 | 324 232 | OK |
| 1017 | WP_PV-Speicher | WP, Batterie, Kessel, BHKW | 21 | 254 143 | OK |
| 1018 | BHKW Test München | Kessel, BHKW, Puffer | 22 | 236 642 | OK |
| 1021 | TestSpeichernUnter | WP, Quellspeicher(WP) | 21 | 227 840 | OK |
| 1023 | Wöhler - Test1 | WP, Kessel, Puffer(WP) | 25 | 262 918 | OK |
| 1024 | Wöhler - Test2 | WP, Kessel, BHKW, Puffer | 26 | 271 695 | OK |
| 1030 | Referenz BHKW-Kaskade (Regressionstest) | Kessel, BHKW ×2, Puffer | 22 | 236 650 | OK |
| **gesamt** | | | **216** | **2 366 177** | **9/9 OK** |

Gesamtdauer 00:00:53, Timeout je Projekt 300 s, **13 Warnungen, 0 Fehler** — dieselben 13 wie in B5:
fünf Puffer ohne gepflegtes Temperaturpaar (1008, 1011, 1018) und acht Anlagen ohne zugeordneten
Energieträger (1007, 1008, 1011, 1017, 1018, 1023). **Projekt 1030 läuft warnungsfrei.**

## Nachweise

**Vergleich gegen die abgelöste Basis B5** — ohne Ausschluss, dieselbe Projektliste:

```
Projekt_1007: PASS (29 Dateien, 324210 Werte)
Projekt_1008: PASS (21 Dateien, 227847 Werte)
Projekt_1011: PASS (29 Dateien, 324232 Werte)
Projekt_1017: PASS (21 Dateien, 254143 Werte)
Projekt_1018: PASS (22 Dateien, 236642 Werte)
Projekt_1021: PASS (21 Dateien, 227840 Werte)
Projekt_1023: PASS (25 Dateien, 262918 Werte)
Projekt_1024: PASS (26 Dateien, 271695 Werte)
Projekt_1030: PASS (22 Dateien, 236650 Werte)

GESAMT: PASS (2366177 Werte innerhalb der Toleranz)
Byte-/MD5-Vergleich: 216 von 216 Dateien gleich, 0 abweichend
```

**9/9 PASS, 216/216 byte-identisch.** B6 ist mit B5 wertgleich bis aufs Byte; der Basiswechsel
erfolgt allein wegen der **Zuordnung** — ab hier ist die gültige Basis mit dem Code nach E7 und dem
Schemastand 22 gerechnet.

> **Der von E6 angekündigte Gleitkommarest von rund 9 Cent bei Projekt 1030 taucht hier nicht auf,
> und das ist richtig so.** Er betrifft den **KWK-Zuschlag** der Wirtschaftlichkeitsrechnung
> (44 265,13 → 44 265,22 €/a). Die Wirtschaftlichkeit ist **nicht Teil des CSV-Exports** — der
> Referenzlauf vergleicht ausschließlich Simulationsergebnisse. Für die Wirtschaftlichkeit gibt es
> weiterhin **keine eingefrorene Basis**; sie wird je Etappe als A/B gegen den Vorgängerstand
> gemessen.

**Selbstvergleich (Reproduzierbarkeit/Determinismus).** Zweiter `lauf` desselben Codes auf derselben
Quelle, Ziel außerhalb des Repos, ohne Ausschluss:

```
GESAMT: PASS (2366177 Werte innerhalb der Toleranz)      Exit-Code 0
Byte-/MD5-Vergleich: 216 von 216 Dateien gleich, 0 abweichend
```

**Produktive Datenbank unberührt.** Vor dem ersten Lauf und nach allen Läufen und Proben:

| | vorher | nachher |
|---|---|---|
| Größe | 92 700 672 Byte | 92 700 672 Byte |
| Zeitstempel | 19.08.2026 14:46:27.810 | 19.08.2026 14:46:27.810 |
| MD5 | `0873B892ADFEE0DC266DBC0814EB93A7` | `0873B892ADFEE0DC266DBC0814EB93A7` |
| `Kenndaten.laccdb` | nicht vorhanden | nicht vorhanden |
| Schemastand | 21 | 21 |

## Was diese Basis **nicht** absichert

Diese Liste gehört zur Basis. Ein grünes 216/216 bedeutet **nicht**, dass die Ausbaustufe W4
regressionsgesichert wäre — der Referenzlauf deckt den **Rechenkern** ab, und den hat W4 nicht
angefasst.

1. **Die Wirtschaftlichkeitsrechnung insgesamt.** Der Referenzlauf ruft sie nicht auf
   (`Referenzlauf\*.cs` enthält keinen Treffer auf „Wirtschaftlichkeit" oder „KWKG"). Kapitalwert,
   KWK-Zuschlag, Steuergutschriften, Tarife und Betriebskosten sind in **keiner** eingefrorenen Basis
   enthalten.
2. **Der Wirkungsfall der Etappe E6.** Kein Projekt der Referenzmenge hat Module, die den
   KWKG-Jahresdeckel **unterschiedlich** treffen; bei 1030 liegen beide darüber, wo die alte
   projektweite Rechnung algebraisch dasselbe liefert. Ein Rückschritt auf den projektweiten
   Rechenweg fiele hier **nicht** auf. Gemessen ist der Fall nur an präparierten Wegwerfkopien
   (E8-Protokoll, Lücke 1: **−25,04 %**).
3. **Die gesamte Kette der Etappen E4 und E5.** Kein Referenzprojekt pflegt Steuerangaben
   (Unternehmensart, Hocheffizienz, räumlicher Zusammenhang, Nutzungsgrad, § 53/§ 53a) oder einen
   Tarifsatz im Rollenmodell. Im Regelbetrieb sind alle Gutschriften vorschriftsgemäß 0 € und die
   neuen Berichtsblöcke leer; gemessen ist die volle Kette nur auf einer Wegwerfkopie
   (E8-Protokoll, Lücke 2).
4. **Die VDI-2067-Betriebskosten der Etappe E3.** Kein Referenzprojekt pflegt eine Bemessungsart
   ungleich `BETRAG`.
5. **Der mehrspaltige Variantenpfad des Berichts.** Kein Referenzprojekt führt eine Variantengruppe;
   `VariantenBloecke` ist nur auf einer Wegwerfkopie gemessen (E8-Protokoll, Lücke 4).
6. **`Heizkessel.Quellwaerme`** steht weiterhin in allen vier Kesselprojekten auf 0 — der Pfad ist im
   Vergleich enthalten, aber nicht mit einem Wert ungleich null belegt (unverändert seit B4).
7. **Der Modulblock der Heizkessel** ist nicht befüllt (`HeizkesselModul[0].Waermeproduktion = 0`,
   `Verbrauch = 0`, leerer `Brennstoff`), obwohl das Gewerk produziert. Bestandsmuster seit B4,
   für eine modulscharfe Kostenrechnung eine offene Baustelle.

Vorschläge, welche Referenzprojekte die Punkte 2 bis 5 dauerhaft schließen könnten, stehen im
[`W4_E8_Abnahme_Protokoll.md`](../../WindowsFormsApplication1/Allgemein/Reporting/W4_E8_Abnahme_Protokoll.md),
Abschnitt „Vorschlag zur Referenzmenge" — **nicht ausgeführt**, weil sie die produktive Datenbank
verändern würden.

## Was hier liegt

Nur CSV-Dateien und die beiden Protokolle — **keine `.accdb`**. Die Arbeitskopie lag außerhalb des
Repos und ist gelöscht. Umfang: 216 CSV, 32 MB.

**Frühere Basis:** `../2026-08-19_B5/` bleibt unangetastet liegen (Codestand `ef8e537`, Schemastand
17 der Quelle, neun Projekte, 216 CSV) — als Vergleichsmaßstab abgelöst, als Beleg des Standes vor
E3 weiter gültig.

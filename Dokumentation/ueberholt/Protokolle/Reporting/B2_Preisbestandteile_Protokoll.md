# B2 — Preisbestandteile Brennstoff, `ucBrennstoffBestandteile`, Kohärenzprüfung (Umsetzungsprotokoll)

Etappe B2 des Konzepts `Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md` (§ 8: „M-1
Preisbestandteile Brennstoff + `ucBrennstoffBestandteile` + Kohärenzprüfung als
**reine Warnzeile**; Ergebniswirkung **keine** — Vorbelegung GESAMTWERT, alle Flags
aus"). Stand 30.08.2026, Branch `Pufferspeicher`. Umsetzung in drei parallel
beauftragten Paketen (Schema/Controller · Dialogbaustein · Kohärenzprüfung) mit
gemeinsamer Abnahme; BF2 („nur warnen") und BF1 (BW3-Vorgabe „Ausweis", betrifft
B6) waren zuvor entschieden.

## 1. Paket A — Migrationsschritt 60 (M-1) und Leseschicht

**`SchemaKatalog.cs`**: Konstanten `SPALTE_BB_ENERGIESTEUER/_CO2/_NETZENTGELT/
_VERTRIEB/_MODUS` + Liste `Schritt60_BrennstoffBestandteile` (9 Spalten an
`energy_project_settings`; `_Aktiv`-Suffix wiederverwendet). Bewusst nicht in
`SchemaKatalog.Alle` (Tabelle gehört dem Kostenmodul — gleiche Begründung wie
Schritt 12).

**`SchemaMigration.cs`**: `SCHRITT_60_BRENNSTOFF_BESTANDTEILE`, Methode nach dem
Muster von Schritt 55 (SpaltenAnlegen → Leseprobe → DML), **erst danach**
`ZIEL_VERSION` 59 → 60 (E6-Reihenfolgeregel im Kommentar; **neue Schritte ab 61**).
Das DML beschränkt sich auf `Anteil_Modus = 'Gesamtwert'` für Bestandszeilen —
**ausdrücklich KEINE Wertsaat** für die Anteile und keine `_Aktiv`-Setzung: NULL
heißt „kein Anteil" (Konzept § 5.1; die E5-Messung — Projekt 1030, 11,746 ct/kWh
trotz fünf abgeschalteter Flags — steht als Begründung am Schritt).

> **Persistenzwert-Klarstellung:** Der Modus wird als `Gesamtwert` /
> `Aufgeschluesselt` gespeichert — die Werte der bestehenden Konstanten
> `DbWerte.SP_AUFSCHLAG_MODUS_*`. Die Großschreibweise `GESAMTWERT` in der
> Konzepttabelle § 5.1 ist Dokumentationsschreibweise; ein zweites
> Persistenzvokabular hätte die Modusabbildung still auf „Aufgeschlüsselt"
> fallen lassen. Im Konzept per Fußnote nachgezogen.

**`Model/BrennstoffBestandteilModel.cs` + `Controller/BrennstoffBestandteilCtrl.cs`**
(neu): API wie beim Strom-Muster, aber mit dem entscheidenden Unterschied an drei
Stellen — `double?` statt `double`, **keine Vorschlagswerte im Modell**, `Read`
lässt NULL null (der Vorgabe-Wächter aus `StromAufschlagCtrl.Komponente` ist
bewusst nicht übernommen). `Update` schreibt null als `DBNull`.
`AlsAufschlagssatz` übergibt `override = 0`: Dieser Block ist eine **Zerlegung**
des Arbeitspreises, kein Aufschlag — im Modus Gesamtwert ist `WirksamCtKwh` 0,
die aussagekräftige Größe ist `SummeAktivCtKwh`. Nichts addiert die Zerlegung auf
den Preis.

**Nachweise** (Harness `..\dev\b2a\`, frische DB-Kopie; Produktiv nur gelesen,
Zeitstempel unverändert):

| Probe | Ergebnis |
|---|---|
| Migration Lauf 1 | 59 → 60, Schritt 60 OK, 9 Spalten angelegt, 34 Zeilen Modus `Gesamtwert`, 0 Fehlerzeilen |
| Lauf 2 (Idempotenz) | kein Schritt ausgeführt, Stand bleibt 60 |
| Saatfreiheit | alle vier Anteile: NOT NULL = 0, `_Aktiv=TRUE` = 0 |
| `Read` Bestandszeile | Energiesteuer = null (kein Vorschlag!), Modus `Gesamtwert`, `AusDatenbank=true` |
| Roundtrip | 0,61/aktiv geschrieben und identisch gelesen; null zurückgeschrieben → Spalte wieder NULL |

## 2. Paket B — `ucBrennstoffBestandteile` (BW1) und Einbau

**`Views/Kosten/ucBrennstoffBestandteile.cs/.Designer.cs`** (neu): exakte
Stil-Nachbildung von `ucStromAufschlaege` (Raster, Designer mit
ASCII-Platzhaltern ohne eigene `.resx`, `TexteSetzen()`, `_laden`-Sperre,
`Program.ZahlParsen/ZahlFaerben`, `WirksamGeaendert`). Inhalt:

- **Vier Komponentenzeilen** Energiesteuer · CO₂ (BEHG) · Netz-/Messentgelt ·
  Vertrieb (das Datenmodell § 5.1 führt keine Konzessionsabgabe; sie bleibt Teil
  des benannten Rests).
- **Modusumschalter** Gesamtwert (Vorgabe) / aufgeschlüsselt. Gesamtwert: der
  Arbeitspreis gilt, Restzeile = Arbeitspreis − Σ aktive Anteile (negativ in
  Firebrick). Aufgeschlüsselt: Summenzeile „Preis aus Bestandteilen" + Knopf
  **„In Arbeitspreis übernehmen"**, der nur das Arbeitspreis-Eingabefeld des
  Wirts setzt — das Control schreibt den Preis **nie selbst** (eine
  Preiswahrheit: `energy_price` bleibt allein beim Bestands-Speicherweg).
- **Schnellwahl Energiesteuer** aus dem **Katalog** (A7-Doppelwahrheit von
  vornherein vermieden): drei Knöpfe „§ 2" / „§ 53a Abs. 5" / „§ 54" mit
  Jahressatz-Beschriftung über `GesetzKatalog.WertMitHerkunft` und die
  bestehenden Schlüsselbildner (`WirtschaftlichkeitCtrl.EnergiesteuerSchluessel`/
  `Energiesteuer54Schluessel`, dafür `private → internal`). Einheitenkette
  €/MWh·€/GJ·€/1.000 l|kg → ct/kWh; Handprobe Erdgas E § 2 = **0,6076 ct/kWh**
  (Konzept § 6.3 nennt 0,61), Heizöl EL = 0,6135.
- **Schnellwahl CO₂**: Preis aus `GESETZ_CO2_PREIS_NEHS` (Projektwert
  `CO2Preis > 0` hat Vorrang — dieselbe Regel wie `BaueCo2Reihe`) ×
  `EmissionsFaktorLader.Co2GKwh`. **Heizwertbezogen** (Erdgas 1,31 ct/kWh):
  Der Arbeitspreis des Dialogs ist `Preis ÷ Hi`; die Zerlegung zerlegt genau
  diesen Preis, also müssen alle Anteile dieselbe Basis tragen — sonst wäre die
  Restzeile systematisch schief. Das brennwertbezogene 1,18-Beispiel in
  Konzept § 6.3 ist per Fußnote richtiggestellt.
- **Nichts wird geraten**: Träger ohne Katalogschlüssel, ohne Jahressatz, mit
  1.000-l/kg-Satz ohne Dichte (`density` im Bestand durchgängig leer, Konzept
  § 10/4) oder ohne CO₂-Faktor (Holz: biogen = 0, BEHG-korrekt) sperren den
  jeweiligen Knopf und nennen den Grund als Tooltip.

**Einbau `ucFuelSettings.BaueBrennstoffblock()`** (programmatisch, Designer der
Wirtsdatei unberührt): bei den vier Brennstoff-`pricing_model`-Codes
`GASEOUS_FUEL`/`LIQUID_FUEL`/`SOLID_FUEL`/`ANIMAL_FAT`; `HEAT` begründet
ausgenommen (Steuer/BEHG entstehen beim Erzeuger, der Träger führt weder
Brennstoff-ID noch Heizwert), `ELECTRICITY` hat den Strom-Block. Arbeitspreis
für die Restzeile aus den **normierten Basiswerten** `_baseWorkPrice/_baseHi`
(nicht aus den Eingabefeldern — beim Einheitenwechsel wäre der Feldquotient
momentweise falsch). `Uebernehmen()` in `SpeichereWerte()` direkt hinter dem
Strom-Block, nach dem Upsert.

**Texte**: 30 `BB_`-Schlüssel de + en; Zugriff ausschließlich über das
TKd4-Muster (GetString mit deutschem Rückfall) — `Resource.Designer.cs` bleibt
unangetastet (Visual Studio lief während der Etappe und regeneriert selbst; die
Datei trägt parallele Fremdarbeit).

## 3. Paket C — Kohärenzprüfung (BW2, BF2 = nur warnen)

**`Allgemein/Wirtschaftlichkeit/KohaerenzPruefung.cs`** (neu, 471 Zeilen, reine
Leselogik): `Pruefe(idProjekt, KohaerenzLauf)` → `List<KohaerenzHinweis>`
(Schwere `WARNUNG`/`HINWEis` als ASCII-Schlüssel, formatierter Text, Betrag).
Beträge sind die **gebuchten Jahr-1-Werte des einen Rechenwegs**
(`SteuerErgebnis` → `EnergiesteuerJahr1`/`StromsteuerBefreiungJahr1`/
`StromsteuerEntlastungJahr1`) — keine Zweitrechnung.

**Ausgabeort**: neues, **nicht persistiertes** Feld
`WirtschaftlichkeitErgebnis.KohaerenzHinweise` (exakt der E7-Rückgabekanal wie
`KwkgModule`/`Betriebskosten`), gerendert von `UcWirtschaftlichkeit` als eigene
Gridzeilen „Kohärenzprüfung" mit ⚠/·-Marke hinter den bestehenden
Hinweiszeilen. Word/Excel bleiben B6 (Herleitungstafel). Aufruf in
`RechneProjekt` try/catch-gesichert — die Prüfung kann den Lauf nie kippen.

**Die vier Fälle** (§ 4.1) mit dokumentierten Auslegungen:

| Fall | Umsetzung |
|---|---|
| 1 konsistent | bewusst **keine** Zeile (positive Nennung kommt mit der Herleitungstafel, B6) |
| 2 Entlastung ohne Belastung | WARNUNG **mit gebuchtem Betrag**. Energiesteuer: eine Zeile mit Trägerliste (der Betrag ist eine Projektgröße — eine Aufteilung wäre eine Rechnung, die es nicht gibt). Strom: § 9b und § 9 Nr. 3 je eigene Zeile; vier Grundtexte sagen, *warum* der Preis die Steuer nicht ausweist (Schalter aus / Gesamtwert-Modus / Komponente aus / 0) |
| 3 Belastung ohne Entlastung | HINWEIS; Energiesteuer hängt an der **Wahl** `KEINE` (eine gewählte, aber gescheiterte Norm begründet der `SteuerGutschriftRechner` schon selbst); Strom nur bei produzierendem Gewerbe und Netzbezug > 0; ohne Betrag, wenn er nur per Zweitrechnung zu haben wäre |
| 4 Satz ≠ Katalogsatz | HINWEIS mit beiden Sätzen, Toleranz 0,005 ct/kWh; Vergleich gegen den **Regelsatz § 2** bzw. `STROMST_REGELSATZ` (im Preis steckt der volle Satz — Teilsätze sind die Erstattung); Einheitenkette identisch zur Schnellwahl. Stromseite liest die **rohe Spalte** — der Lese-Rückfall des Strom-Blocks (NULL → 2,05) ist für Fall 2 maßgeblich (er wirkt real im Preis), für Fall 4 zirkelfrei ausgeschlossen |

**Nachweise** (Harness `..\dev\b2c\`, frische Kopie, Präparationsprojekt 1030):

- Bestandslage 1018/1024/1030: **keine** Kohärenzzeilen (heute pflegt niemand die
  Anteile) — produktiv bestätigt: 0 Projekte mit gepflegtem Anteil.
- Fall 2 Energiesteuer: Wahl `PARAGRAF_53` → WARNUNG mit **6.330,30 €/a**
  (gebuchter Betrag) und Grund „kein Anteil erfasst"; nach Abschalten des
  gepflegten Anteils Grund „Anteil abgeschaltet".
- Fall 4: 0,4500 aktiv → Hinweis „…weicht vom Katalogsatz 5,50 EUR/MWh des
  Jahres 2027 ab — das sind 0,6076 ct/kWh"; exakter Katalogwert und +0,004 →
  still; +0,006 → Hinweis (Toleranzband bewiesen).
- Fall 3: Hinweis ohne Betrag, korrekt ausgelöst.
- Stromseite: § 9b 88.075,98 und § 9 Nr. 3 7.656,81 €/a je eigene WARNUNG bei
  Schalter aus; Schalter an + 2,05 aktiv → still; 0,05 → Fall-4-Hinweis;
  Komponente aus → beide Warnungen mit Grund.
- **Ergebnisneutralität dreifach gemessen**: Kapitalwert/Energie/Betrieb über
  alle Präparationen identisch (u. a. KW −22.038.778,17 konstant über
  Fall-2/4-Varianten; 1030-Basis −22.132.957,00; 1024-Anker Betrieb 99,00).

## 4. Referenzlauf-Neutralität — byte-genau belegt

Nachweis nach dem Hausrezept (`Referenzlauf.csproj` per ProjectReference; die
Produktiv-DB stand bereits auf Schema 60, die `migration` bestätigte „bereits
erledigt"; der bekannte Altbefund „PufferHeizung ohne WS_ID_Puffer: 2" ist für
den Vergleich unkritisch, beide Seiten nutzen dieselbe Kopie):

- **A/B auf derselben DB-Kopie** — HEAD (acf2e30, ohne B2, per `git archive`
  isoliert gebaut) gegen den B2-Arbeitsstand, alle 13 Projekte
  (1007–1042 inkl. 1039–1044-Bestand): `GESAMT: PASS (3.558.311 Werte)`,
  **MD5 332 von 332 CSV byte-gleich, 0 Abweichungen** →
  die Etappendefinition „Referenzläufe byte-gleich" ist erfüllt.
- Gegen die **eingefrorene Basis `2026-08-29_Booster`**: 11 × PASS, 2 × FAIL
  (1030: 48.095, 1042: 155.035 Abweichungen). Beide FAIL entstehen mit dem
  HEAD-Stand in **identischer Abweichungszahl** — es sind reine
  **Datenänderungen des Anwenders**, kein Codeeffekt: 1030 führt nur noch ein
  BHKW-Modul (der `Agenitor 306` der Basis ist entfernt — damit verliert die
  Referenzmenge ihre einzige Mehrmodul-BHKW-Abdeckung!), 1042 hat den Kessel
  getauscht (`Vitocrossal` → `ecoTEC plus`) und einen Brauchwasserspeicher
  ergänzt.
- Läufe sauber: 13/13 Exit 0, keine Fehlerzeile, nur bekannte
  Warnungsfamilien. Belege im Sitzungs-Scratchpad
  (`vergleich_*.txt`, `md5_*.txt`, `datenbefund_1030_1042.txt`); der B2-Lauf
  (332 CSV) liegt als Kandidat für einen **Basiswechsel** bereit —
  Anwenderentscheid, siehe B2-O8.

## 5. Offene Punkte (Fortschreibung)

| Nr. | Punkt |
|---|---|
| B2-O1 | **Doppelmeldung § 9b**: Der Bestandstext in `RechneAufschlaege` meldet den Widerspruch zusätzlich (ohne Betrag). Bewusst stehengelassen — B2 nimmt keine Zeilen weg; Streichung in B6 mit der Herleitungstafel |
| B2-O2 | Träger mit 1.000-kg-Satz bei Literabrechnung bleiben ganz stumm (keine Gutschrift, keine Zeile — `density` leer, § 10/4); Kandidat für eine eigene Hinweiszeile in B6 |
| B2-O3 | Fall 4 ohne Katalogsatz des Bilanzjahres (JahrVon später) bleibt bewusst still statt geraten |
| B2-O4 | Kohärenzzeilen sind nicht persistiert — nach Reiter-Öffnen mit gespeicherten Ergebnissen erscheinen sie erst beim nächsten „Berechnen" (dieselbe Eigenschaft wie die E7-Nachweise) |
| B2-O5 | **Sichtabnahme des neuen Blocks** im Energieträgerdialog durch den Anwender steht aus (548 px breit, Höhenlogik wie der Strom-Block) |
| B2-O6 | Projekt 1018 rechnet keine Energiekosten und hat keinen Stromträger — als B7-Referenzprojekt ungeeignet (Notiz für B7) |
| B2-O7 | Die Produktiv-DB lief **während** der Etappe durch einen Fremdstart der gebauten App auf Schema 60 — neutral (0 gepflegte Anteile, Vorbelegung korrekt), aber der Reihenfolge halber vermerkt |
| B2-O8 | **Basiswechsel der Referenzläufe entscheiden**: `2026-08-29_Booster` beschreibt 1030/1042 nicht mehr (Anwender-Datenänderungen, § 4) — jeder Folgelauf schleppt zwei FAIL mit. Kandidat liegt bereit (B2-Lauf, byte-identisch zum HEAD-Lauf); einzupreisen ist der Verlust der Mehrmodul-BHKW-Abdeckung in 1030 |

## 6. Geänderte/neue Dateien

```
Allgemein/Update/SchemaKatalog.cs                        Schritt-60-Konstanten + Liste
Allgemein/Update/SchemaMigration.cs                      Schritt 60, ZIEL_VERSION 60 (neue ab 61)
Model/BrennstoffBestandteilModel.cs                      NEU — double?, keine Vorschläge
Controller/BrennstoffBestandteilCtrl.cs                  NEU — Read/Update/AlsAufschlagssatz
Views/Kosten/ucBrennstoffBestandteile.cs/.Designer.cs    NEU — der Dialogbaustein (BW1)
Views/Kosten/ucFuelSettings.cs                           BaueBrennstoffblock + Speicherkette
Allgemein/Wirtschaftlichkeit/KohaerenzPruefung.cs        NEU — Vier-Fall-Prüfung (BW2)
Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs   2× internal, SteuerEingabe-Durchreichung, Aufruf
Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitDaten.cs  KohaerenzHinweise am Ergebnis
Allgemein/Wirtschaftlichkeit/SteuerGutschriftRechner.cs  SteuerAnlage.CarrierId (Ausweis)
Views/Wirtschaftlichkeit/UcWirtschaftlichkeit.cs         KohaerenzZeilen im Reiter
MyResource/Resource.resx / Resource.en-US.resx           30 BB_- + 16 KOH_-Schlüssel
Allgemein/Reporting/B2_Preisbestandteile_Protokoll.md    dieses Protokoll
Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md             Fußnoten §5.1/§6.3, Vermerk §8
```

Harnesse (gitignored): `..\dev\b2a\`, `..\dev\b2c\`.

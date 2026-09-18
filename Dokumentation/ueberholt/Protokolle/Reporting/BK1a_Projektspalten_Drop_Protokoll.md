# BK1a — KWK-Ersatzweg auf Anlagenbasis, Schemaschritt 90

Etappe BK1a des Wirtschaftlichkeitskonzepts, Anwenderentscheid `BK1-1` („Empfehlung") vom
18.09.2026 mit den Zusatzentscheiden `BK1-Q1` = (c) und `BK1-Q2` = (a), dazu der
Zusatzauftrag `K-WZ-1` = (a) desselben Tages. Sie löst die offenen Punkte `BK1-1` und
`BK1-2` aus § 6.3 des
[Konzepts](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
und schließt den Absatz „Aufräumkandidaten" in § 6.5.

---

## 1 Der Befund

Nach Schemaschritt 89 (Etappe BK1) gehört der KWK-Zuschlag der Anlage. Die elf
KWKG-Spalten von `Tab_ProjektWirtschaftlichkeit` waren damit rechnerisch tot — mit einer
Ausnahme, und die war die eigentliche Sperre: **Der projektweite Ersatzweg las sie noch,
und zwar alle sechs.**

Der Ersatzweg greift, wenn sich Anlagen- und Ergebnismodulzeilen nicht paaren lassen
(`KwkgAnlagenauswahl.Bestimmbar = false`): keine Modulzeilen, oder Namen und Anzahl passen
nicht zusammen. Der Konzeptsatz „vier Spalten ganz ungelesen" traf deshalb nicht zu; alle
sechs hatten genau einen Leser, und ein Drop hätte ihn stumm auf 0 gesetzt.

Der zweite Befund betrifft die Kostenseite und kam vom Anwender: Unter der Überschrift
„Anlagenkomponenten" stand eine Zeile „Wärmezentrale 0,00" ohne Kennzeichnung und ohne
Papierkorb — weder einzuordnen noch zu entfernen.

---

## 2 Was gemacht wurde

### 2.1 Der Ersatzweg rechnet mit einer leistungsgewichteten Gesamtanlage

`ReiheErsatzGewichtet` ersetzt `ReiheProjektweit`. Aus den BHKW-Anlagen entsteht **eine**
virtuelle Gesamtanlage; Gewicht ist die elektrische Nennleistung, `g_i = P_el,i`,
`G = Σ g_i`:

```
SatzEigen   = Σ g_i × SatzEigenDerAnlage(a_i)                       / G
SatzEinsp   = Σ g_i × (SatzEinsp(a_i) ?? 0)                         / G
Kontingent  = Σ g_i × (VbhKontingent(a_i) > 0 ? VbhKontingent(a_i)
                       : KontingentDerAnlage(a_i, Beginn(a_i)))     / G
Deckel(t)   = Σ g_i × (Deckel(a_i) > 0 ? Deckel(a_i)
                       : StaffelDeckel(Beginn(a_i) + t − 1))        / G     JE JAHR
Beginn(a_i) = Inbetriebnahme(a_i).Jahr ?? Förderbeginn des Projekts
```

**Der Jahresdeckel wird je Jahr neu gemischt.** Anlagen mit verschiedenem Förderbeginn
stehen im selben Kalenderjahr auf verschiedenen Stufen der Staffel des § 8 Abs. 4; ein
einmal gebildeter Mittelwert hätte den Verlauf eingeebnet.

**Alles Übrige bleibt Zeile für Zeile:** `bonusVoll`, der Negativpreis-Abschlag, der
Fallback ohne Stundenreihen (W2), die projektweiten Vollbenutzungsstunden und die
Jahresschleife mit `rest` und `verguetet`. Nur die vier Eingangsgrößen wechseln die
Herkunft.

**`G ≤ 0`** — keine Anlage führt eine Nennleistung: arithmetisches Mittel und ein
benannter Hinweis (`WIRT_KWKG_ERSATZ_OHNE_LEISTUNG`) statt einer stillen 0. Die Hinweise
des Weges entstehen je Anlage und werden **lokal** ordinal entdoppelt.

Entfallen sind damit die projektweite Tatbestandsprüfung in `BaueKwkgReihe` und
`KontingentDesProjekts` — beide Rechenwege prüfen jetzt je Anlage
(`SatzEigenDerAnlage`, `KontingentDerAnlage`).

### 2.2 Die Messung — vor und nach dem Umbau, auf demselben Zwischenstand

Projekt **1030** auf einer Arbeitskopie, Modulnamen in `Tab_ErgebnisBHKWModul` verstellt
und eine Zeile zusätzlich, sodass `ModulJeAnlage` `null` liefert und zwangsläufig der
Ersatzweg läuft. Szenario Erwartet, flache Stundenreihen aus dem gebuchten Lauf.

| Lauf | Größe | vor dem Umbau | nach dem Umbau |
|---|---|---|---|
| **A** — 1030 unverändert (Kontingent gepflegt) | Zuschlag Jahr 1 | 7.315,948722 €/a | **7.315,948722 €/a** |
| | Kapitalwert | −21.895.377,339395 € | **−21.895.377,339395 €** |
| | Reihe t = 1…20 | 7.315,95 · 6.843,95 · 6.371,96 · 5.899,96 (×8) · 3.067,98 · 0 (×8) | **zahlengleich** |
| **B** — `KWKG_Vbh_Kontingent` = NULL an Projekt UND Anlagen | Zuschlag Jahr 1 | 7.315,948722 €/a | **0,00 €/a** |
| | Kapitalwert | −21.895.377,339395 € | **−21.954.815,753214 €** |

**Lauf A ist die Ergebnisgleichheit:** Beide Anlagen tragen seit Schritt 89 dieselben
Werte, und das leistungsgewichtete Mittel gleicher Werte ist dieser Wert.

**Lauf B ist die abgenommene Ausnahme** (`BK1-Q2` a): `LadeParameter` machte aus einem
leeren Kontingent den Feldvorgabewert 30.000 h, und `KontingentDesProjekts` gab ihn ohne
Anlagenart zurück — still. Jetzt geht der Weg über `KontingentDerAnlage` →
`KwkgKontingentRechner.Ableiten("", …)` → 0 h **mit Begründung**; das ist dieselbe
Antwort, die der Regelweg seit BK1 gibt.

### 2.3 Schemaschritt 90 — zwei Teile, zwei Quellen

**DDL — `KwkgProjektaltspalten`** (Muster Schritt 85): `KWKG_Bonus`,
`KWKG_Bonus_Einspeisung`, `KWKG_Vbh_Kontingent`, `KWKG_Vbh_Jahresdeckel`,
`KWKG_Tatbestand`, `KWKG_Anlagenart` fallen aus `Tab_ProjektWirtschaftlichkeit`. Kein
Index, kein Fremdschlüssel, keine Sicht, kein Trigger; die beiden CHECK-Bedingungen sind
Spaltenbedingungen und fallen mit ihrer Spalte. STRICT ist unerheblich, kein
Tabellenneubau nötig.

**Was stehen bleibt:** `KWKG_Kostenanteil` samt Dialogfeld (`BK1-Q1` c, als offener Punkt
`BK1-4` im Konzept), `KWKG_Stichtag`, `KWKG_Inbetriebnahme`, `KWKG_Pauschalmodus`,
`KWKG_Abschlag_Negativ`.

**DML — `KostenErfassungsgruppenAltzeilen`** (`K-WZ-1` a): Aus `Tab_ProjektWerte` fallen
die Nullzeilen der drei nicht anlagenfähigen Erfassungsgruppen. Eine Zeile fällt nur,
wenn **alle vier** Bedingungen zutreffen: Komponente ist eine der drei, `StammID` ist eine
Hauptkomponente (`Tab_Kostenfaktor.IsMainComponent = 1`), alle fünf Wertfelder sind 0 oder
NULL — **und die Gruppe (Projekt, Kategorie, Komponente) führt nirgends eine Position mit
Wert.** Sonst bleibt sie vollständig stehen.

**Reihenfolge zwingend: 90 hinter 89.** Schritt 89 liest die sechs Spalten als Quelle
seiner Übertragung. Innerhalb von Schritt 90 läuft erst das DML, dann das DDL — nicht aus
einer Abhängigkeit, sondern damit ein abgebrochener Lauf die Datenzeilen nicht in einer
Datenbank zurücklassen kann, deren Spalten schon fehlen.

**Getroffen in der Testdatenbank:** 6 Spalten; 11 Zeilen — Projekt 1018 eine, 1019 sechs,
1031 eine, 1032 drei (Kategorien 1 und 2), ausnahmslos 0,00.

### 2.4 Die Kostenseite

Die drei Erfassungsgruppen erscheinen wie die gelben Zeilen: Kennzeichnung
„{0} — Erfassungsgruppe (ohne Anlage)", Kurztext, Papierkorb, derselbe Löschweg
(`_loseKomponenten`). Der Text bleibt vom gelben getrennt, weil die Lage eine andere ist:
Eine Erfassungsgruppe **hat** keine Anlage, eine gelbe Zeile hat ihre **verloren**.

**Die Regel steht im Kern:** `KostenVorlagenCtrl.IstErfassungsgruppe` — das Gegenstück zu
`IstWaehlbar`. Einen plattformfreien oder iOS-Zeilenaufbau derselben Seite gibt es nicht;
`Komponenten()` lebt allein in der Windows-Schale, `EPOS.UI/Seiten/Berichte/KostenDaten.cs`
führt nur die Zeilenart.

**Nebenbefund `_ohnePosition` / `_nichtVerbaut` geprüft:** Die Erfassungsgruppen gehen
bewusst **nicht** in die Statuszeile „Kostenpositionen ohne verbaute Anlage: {0}" — sie
haben nie eine gehabt, und die Meldung wäre eine Fehlanzeige.

---

## 3 Entfernte Fundstellen

| Datei | Zahl |
|---|---|
| `WirtschaftlichkeitCtrl.cs` | 5 CREATE-Fragmente, 4 + 2 `SpalteSicher`, 1 Altdaten-Fix (Jahresdeckel 3500 → 0), 4 + 2 Lesezeilen, je 6 Spalten und 6 Parameter in UPDATE und INSERT, Platzhalterkette 51 → 45; dazu `ReiheProjektweit` und `KontingentDesProjekts` |
| `WirtschaftlichkeitDaten.cs` | 6 Felder von `WirtschaftlichkeitParameter` |
| `BhkwWirtschaftlichkeitDaten.cs` | 6 Rundtrip-Felder in `Aus` / `Gleicht` / `Anwenden` |
| `KiAktionenWirtschaft.cs` | 1 (`kwkg_bonus_ct_kwh`, ersatzlos) |
| `SchemaKatalog.cs` | 6 Konstanten; `Schritt28_KwkgTatbestand` holt zwei Namen jetzt von `KwkgProjektaltspalten` und bleibt im Übrigen unverändert |

**Ressourcen:** −3 (`WIRT_KWKG_TATBESTAND_OFFEN`, `_KEINER`,
`WIRT_KWKG_KONTINGENT_ABGELEITET`), +4 (`WIRT_KWKG_ERSATZ_GEWICHTET`,
`WIRT_KWKG_ERSATZ_OHNE_LEISTUNG`, `BK_KOSTEN_ERFASSUNGSGRUPPE`, `…_HINT`), beide Sprachen;
`Resource.Designer.cs` neu erzeugt, 6 358 → **6 359** Einträge, zweiter Lauf +0.

**Nebenbefund:** Die drei Anlagenfassungen `WIRT_KWKG_TATBESTAND_ANLAGE_OFFEN`, `_KEINER`
und `WIRT_KWKG_KONTINGENT_ANLAGE` standen entgegen der Bestandsaufnahme bereits in beiden
`.resx` — sie leben nicht mehr nur als `T("…", "Rückfall")` im Quelltext.

---

## 4 Nachweis

**Neu:** `KwkgErsatzwegGewichtetTests` (6), `KwkgProjektaltspaltenTests` (6, darunter der
Migrationslauf auf einer Altdatei und die Reihenfolgeprobe),
`KwkgProjektspaltenWacheTests` (1), `KostenErfassungsgruppenAltzeilenTests` (4), dazu
`TestDatenbank.AltspaltenKwkgProjektWiederherstellen()`.

**Umgestellt:** `KwkAnlagenwahrheitTests` (die Zusicherung liegt jetzt an
`Tab_Energieanlagen` und misst gegen die Werte, die 1030 vor dem Schritt trug — die Fälle
bleiben, sie sind der Nachweis von 89), `WirtschaftlichkeitParameterDialogTests`,
`BhkwWirtschaftlichkeitDialogTests`, `VerguetungUmzugTests`.

**Vier Gegenproben gefahren und zurückgebaut:**

| Gegenprobe | Ergebnis |
|---|---|
| Gewichtung durch arithmetisches Mittel ersetzt | 3 von 6 Fällen rot |
| eine `SpalteSicher`-Zeile stehen gelassen | Wache rot |
| Schritt 90 vor Schritt 89 registriert | Reihenfolgeprobe rot |
| eine der sechs Spalten in der Testdatenbank belassen | Wache rot |

**Gate:** Kern-Filter 0 Fehler / 5 Warnungen (Bestand, Schranke 7), Windows-Schale
0 Fehler / 5 Warnungen, `EPOS.Kern.Tests` 3 309/3 309 (+17), `EPOS.UI.Tests` 4 621/4 621,
SpeicherEngine 378/378, KiKern 499/499, SpeicherPlanung 27/28 (1 übersprungen) — **beide
Kulturen** —, SqlDialektPrüfer 1 492 Texte / **0 Fundstellen**, ChartProben 64 Bilder /
0 Verstöße, Referenzlauf 1030, 1007, 1017, 1045, 1046 **5/5 PASS und byte-gleich** gegen
`2026-09-16_R8_Heizkessel_Kaskade` (1 656 417 Werte). Testdatenbank auf Schemastand 90
gezogen, Trockenlauf danach leer, LFS-Zeiger geprüft. Keine neue Referenzbasis, kein Push,
kein CI- und kein iOS-Lauf.

---

## 5 Abnahmepunkte (Windows)

| Punkt | Was zu sehen ist |
|---|---|
| `A-BK1a-1` | Projekt „BHKW Test München": Der KWK-Zuschlag der Ergebnisse ist unverändert; Gruppe 2 des BHKW-Dialogs zeigt weiterhin „Anteil Neuherstellungskosten (Vorgabe)" |
| `A-BK1a-2` | Bestandskopie einer Anwenderdatenbank: Die Migration meldet im Protokoll „90: 6 KWKG-Projektspalte(n) entfernt"; der zweite Start meldet 0 |
| `A-BK1a-3` | Eine Anlage ohne Vbh-Kontingent und ohne Anlagenart: Der Hinweis nennt „abgeleitet 0 Vbh" mit Begründung statt still 30.000 h zu rechnen |
| `A-KWZ-1` | Projekt des Anwenders nach der Migration: Die Zeile „Wärmezentrale 0,00" unter „Anlagenkomponenten" ist fort |
| `A-KWZ-2` | Eine Erfassungsgruppe **mit** Wert trägt die Kennzeichnung „— Erfassungsgruppe (ohne Anlage)" und einen Papierkorb; der Papierkorb fragt nach und löscht |
| `A-KWZ-3` | Eine anlagenfähige lose Gruppe erscheint weiterhin gelb mit „— ohne Anlagenzuordnung"; Anlagenzeilen unverändert |

Beide Sprachen.

---

## 6 Offene Punkte

- `BK1-3` (Jahr-0-Ausweis der KWKG-Pauschale) bleibt — Entscheid steht aus.
- `BK1-4` **neu**: `KWKG_Kostenanteil` des Projekts hat keinen Rechenleser mehr, das Feld
  bleibt (`BK1-Q1` c).

---

## 7 Logbuch-Entwurf (Wiki „Update-Logbuch", Version 1.2.0.2)

> Der KWK-Zuschlag wird ausschließlich an der Anlage gepflegt und gerechnet; auf der
> Kostenseite tragen Wärmezentrale, Bauliche Anlagen und Stromeinspeisung den Zusatz
> „Erfassungsgruppe (ohne Anlage)" und einen Papierkorb.

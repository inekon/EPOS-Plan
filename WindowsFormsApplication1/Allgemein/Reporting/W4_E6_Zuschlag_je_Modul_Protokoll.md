# W4 · Etappe E6 — KWK-Zuschlag je BHKW-Modul

**Stand: 19.08.2026.** Umsetzung der Etappe E6 aus
[`Konzept_BHKW_Kosten_Erloese.md`](Konzept_BHKW_Kosten_Erloese.md) (Nutzerentscheidung vom
18.08.2026: „**Je BHKW-Modul** — erst damit sind die gesetzlichen Leistungsklassen
abbildbar"). Ausgangsstand `9ed551d`. Faktenbasis für jeden Zahlenwert:
[`Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`](../../../Grundlagen_KWKG_Energiesteuer_Stromsteuer.md),
Abschnitt 1.

**Ergebnis in vier Sätzen.** Der KWK-Zuschlag wird ab hier **je Anlage** gerechnet und
jahresweise summiert — mit eigenem Stichtag, eigenem Inbetriebnahmedatum, eigenem
Zuschlagssatz, eigenen Vollbenutzungsstunden, eigenem Jahresdeckel und eigenem Kontingent;
damit sind die vier „Grenzen der Zwischenlösung" aus dem E2-Nachtrag und der als
*gravierendster Restbefund der Reihe* geführte Punkt „ein Datum je Projekt" aufgelöst. Für
**Einmodulprojekte ist die Rechnung ergebnisneutral** (24 von 27 Wirtschaftlichkeitszeilen
zeichengleich), für das einzige Mehrmodulprojekt der Referenzmenge ändert sie den Zuschlag um
**+0,09 €/a von 44 265,13 auf 44 265,22 €** — mathematisch null, der Rest ist
Gleitkommarundung, weil dort **beide** Module über dem Jahresdeckel liegen. Die eigentliche
Wirkung liegt in den Fällen, die der Bestand nicht enthält: an präparierten Kopien reicht sie
von **−25,0 %** (Module beiderseits des Jahresdeckels) über **−9,7 %** (verschiedene
Inbetriebnahmejahre) bis **−83,3 %** (eine Anlage scheitert am eigenen Stichtag). Der
Rechenkern ist unberührt: **216 von 216 CSV byte-identisch** gegen den A-Stand *und* gegen die
eingefrorene Basis `2026-08-19_B5`.

---

## 1 Was E6 auflöst

### 1.1 Die vier Grenzen der Zwischenlösung

Der Nachtrag 1 zu Etappe E2 hat vier Grenzen ausdrücklich benannt (E2-Protokoll, Abschnitt N3).
Alle vier sind hier erledigt:

| # | Grenze der Zwischenlösung (Zitat E2) | Auflösung in E6 |
|---|---|---|
| 1 | „Jahresdeckel und 30.000-h-Kontingent laufen weiter über **EINE gemeinsame Vbh-Größe**. […] liegt ein Teil darüber und ein Teil darunter, weicht es ab." | `ReiheJeAnlage` führt Deckel und Kontingent je Anlage. Wirkung an Fall **H3** gemessen: −8 751,27 €/a = **−25,04 %** |
| 2 | „Der Zuschlagssatz bleibt einer je Projekt. Nach § 7 hängt er von der Leistungsklasse der **Anlage** ab." | Überschreibwert je Anlage (`KWKG_Satz_Einspeisung` / `KWKG_Satz_Eigen`) plus Katalogvorschlag mit Herleitung. Wirkung an Fall **H8**: +6 199,79 €/a = **+16,67 %** |
| 3 | „Der Bonus wird über die STROMMENGE gekürzt […] der Anteilsfaktor unterstellt, dass sich dieser Split auf die verbleibenden Anlagen gleich verteilt." | **Bleibt eine Näherung**, jetzt aber ausdrücklich benannt und je Anlage angewandt — siehe Abschnitt 3.4. Modulscharfe Stundenreihen gibt es im Modell weiterhin nicht. |
| 4 | „Fristen und Heizöl-Ausschluss bleiben projektweit." | Stichtag, Realisierungsfrist, Ausschreibungsgrenze und Heizöl-Ausschluss laufen je Anlage. Wirkung an Fall **H7**: −31 000,14 €/a = **−83,33 %** |

### 1.2 Der gravierendste Restbefund der Reihe

Das E2-Protokoll führt in Abschnitt N2-7 unter „Verbleibende Restbefunde der Reihe ‚Projekt
gegen Anlage'" an erster Stelle:

> **Der neue gravierendste Restbefund.** Bei gemischten Inbetriebnahmen ist die Prüfung
> entweder zu streng oder zu großzügig, und ein einziges Datum entscheidet für alle Anlagen
> zugleich über Neuanlage/Bestandsanlage. Fachlich zu klären, gehört zu E6.

`Tab_Energieanlagen` führt seit Migrationsschritt 22 `KWKG_Stichtag` und
`KWKG_Inbetriebnahme` je Anlage. Beide sind NULL-fähig, und **NULL heißt „es gilt der
Projektwert"** — genau dieser Rückfall macht den Schritt für Bestandsprojekte
ergebnisneutral. Das Datum der Anlage entscheidet ab hier über

- die Frist des § 6 Abs. 1 (Dauerbetrieb bis 31.12.2026),
- die Realisierungsfrist der Novelle 2025 (vier Jahre),
- das Stichtagsjahr des Zuschlagssatzes,
- den Beginn der degressiven Jahresdeckel-Staffel des § 8 Abs. 4,
- die Ausschreibungsgrenze des § 8a (Katalogschlüssel mit dem Jahr **dieser** Anlage) und
- Neuanlage gegen Bestandsanlage und damit den **Heizöl-Ausschluss**.

---

## 2 Was umgesetzt wurde

| Gegenstand | Datei : Zeile |
|---|---|
| Acht Spalten je Anlage, Migrationsschritt 22 (nur DDL) | `Allgemein/Update/SchemaKatalog.cs:1108`, `…/SchemaMigration.cs:510`, `:906`, `:1720` |
| Schema-Zielstand 21 → 22 | `Allgemein/Update/SchemaMigration.cs:77` |
| Tolerante Vorsorge unmittelbar vor dem Zugriff | `WirtschaftlichkeitCtrl.StelleTabellenSicher` |
| Reihe **je Anlage**, jahresweise summiert | `WirtschaftlichkeitCtrl.ReiheJeAnlage:1708` |
| Projektweiter **Ersatzweg**, Zeile für Zeile der Stand vor E6 | `…ReiheProjektweit:1645` |
| Vbh **einer** Anlage (Modulzeile aus E2, sonst Strom / P_el) | `…VbhDerAnlage:1796` |
| § 6, § 8a und Heizöl **je Anlage**, mit Projektwert als Vorgabe | `…Anlagenauswahl:2523` |
| Anlagenzeile mit acht neuen Feldern, Rückfall auf die Abfrage ohne sie | `…BhkwAnlage:2302`, `…LiesBhkwAnlagen:2695`, `…AnlagenTabelle:2751` |
| Meldungen bei Teilausschluss, ausgelagert und um zwei Gründe erweitert | `…Ausschlussmeldungen:1621` |
| Zuschlagssatz aus dem Katalog, **marginale Tranchen** statt Klassen | `Allgemein/Wirtschaftlichkeit/KwkgSatzRechner.cs` (neu, 310 Zeilen) |
| Lese- und Schreibweg der acht Angaben | `Allgemein/Wirtschaftlichkeit/KwkgAnlagenCtrl.cs` (neu, 248 Zeilen) |
| Dialog „KWK-Zuschlag je BHKW-Modul" mit Vorschlag und Herleitung | `Views/Wirtschaftlichkeit/Form_KwkgModule.cs` (neu, 397 Zeilen) |
| Einstieg und umbenannte Projektvorgaben im Parameterdialog | `Views/Wirtschaftlichkeit/Form_WirtschaftlichkeitParameter.cs` |
| **Generationsweise Nachsaat** des Katalogs | `Allgemein/Wirtschaftlichkeit/GesetzKatalog.cs:84`, `:388`, `:455`, `:513`, `:554` |
| Zwei neue Katalogschlüssel (Generation 3), einer nachgereicht (Generation 2) | `GesetzKatalog.Vorbelegung`, `Allgemein/DbWerte.cs` |
| Steuerwerte für Anlagenart und Eigenstromfall | `Allgemein/DbWerte.cs` |
| Neun Anzeigetexte in beiden `.resx` und im Designer | `MyResource/Resource*.resx`, `Resource.Designer.cs` |
| Nachtrag im Lokalisierungskatalog | `Allgemein/Simulation/Lokalisierung_Katalog.md` |

**13 Quelldateien** geändert oder neu (drei neu), dazu zwei Dokumente. Der Rechenkern
(`Allgemein/Simulation/`, `Allgemein/BhkwPlan.cs`) ist **nicht angefasst**.

---

## 3 Entwurfsentscheidungen

### 3.1 „Leistungsanteil" heißt Staffel, nicht Klasse — und das ist keine Feinheit

§ 7 Abs. 1 und 2 KWKG überschreiben ihre Wertetabelle mit *Leistungsanteil* und meinen damit
**marginale Tranchen**, nicht eine Klasse, in die die Anlage als Ganzes fällt. Eine
300-kW-Anlage bekommt deshalb nicht durchgehend 4,40 ct/kWh, sondern:

```
 50 kW × 8,00 + 50 kW × 6,00 + 150 kW × 5,00 + 50 kW × 4,40
 = 400 + 300 + 750 + 220 = 1 670 ct·kW/kWh  ÷ 300 kW = 5,5667 ct/kWh
```

Die naheliegende Umsetzung „Klasse suchen, Satz anwenden" hätte 4,40 ct/kWh geliefert und
damit **21 % zu wenig**. Der Fehler wächst nicht monoton, sondern ist an den Klassengrenzen am
größten; bei 3 MW beträgt er noch 18,7 % (3,40 statt 4,1833 ct/kWh). Die angezeigte Herleitung
nennt deshalb **die Tranchen**, nicht eine Klasse.

> **Dieser Punkt kam von außen und hat die Umsetzung vergrößert.** Der ursprüngliche
> Umsetzungsplan sah eine Klassensuche vor. Der Mehraufwand sind rund 120 Zeilen in
> `KwkgSatzRechner` (Staffeltabellen je Fall, Tranchendurchlauf, Herleitungstext); die
> Ergebniswirkung ist der oben genannte Faktor und betrifft **jeden** Vorschlag oberhalb von
> 50 kW.

### 3.2 Einspeisung und Eigennutzung sind nicht symmetrisch

Auf **eingespeisten** Strom besteht der Zuschlag ohne weitere Voraussetzung (§ 7 Abs. 1). Auf
**selbst genutzten** Strom besteht er nach § 7 Abs. 2 **nicht generell**, sondern nur in den
drei Tatbeständen des § 6 Abs. 3 — mit drei verschiedenen Satzreihen. Über allem steht die
Sonderregel des § 7 Abs. 3a für **neue** Anlagen bis 50 kW (16 bzw. 8 ct/kWh), die Abs. 1
*und* 2 vorgeht.

Umgesetzt als zwei Angaben je Anlage:

| Angabe | Werte | Wirkung |
|---|---|---|
| `KWKG_Anlagenart` | `NEUANLAGE` · `MODERNISIERT` · `NACHGERUESTET` | § 7 Abs. 3a nur für neue Anlagen; über 2 MW 3,10 statt 3,40 ct/kWh nur für nachgerüstete |
| `KWKG_Eigenstromfall` | `KEINER` · `NR1_BIS100KW` · `NR2_KUNDENANLAGE` · `NR3_STROMINTENSIV` | entscheidet, **ob** es überhaupt einen Eigenstromzuschlag gibt und nach welcher Satzreihe |

**`KEINER` ist der Regelfall und die Vorgabe** — der Vorschlag lautet dann 0 ct/kWh mit der
Begründung „kein Tatbestand des § 6 Abs. 3 erfasst". Das ist keine Lücke, sondern die
Rechtslage. Beide Spalten haben **keine unmittelbare Rechenwirkung**: Sie steuern
ausschließlich den Vorschlag und die Herleitung. Gerechnet wird mit dem Überschreibwert der
Anlage, ersatzweise mit dem Projektsatz. Deshalb bekommen sie in Schritt 22 auch **keine**
DML-Vorbelegung — eine Vorbelegung könnte den Vorschlag verschieben, ohne dass jemand sie
getroffen hätte.

### 3.3 Der Vorschlag ersetzt den Projektsatz nicht von selbst

Der Katalogvorschlag erscheint im Dialog **als Text mit Herleitung**; erst die Schaltfläche
„Vorschlag in die Satzfelder übernehmen" schreibt ihn in `KWKG_Satz_Einspeisung` und
`KWKG_Satz_Eigen`. Ohne diesen Griff bleibt die Anlage beim Projektsatz.

*Begründung:* Die Alternative — „der Katalog gilt, sofern nichts anderes eingetragen ist" —
hätte jede gespeicherte Altrechnung mit gepflegtem KWKG-Satz still auf einen anderen Satz
umgestellt. Genau davor warnt die Reihe seit E5 (Aufschlagsschalter) und E4 (Steuerwahl). Die
Nutzerentscheidung lautet „**Vorschlag** aus dem Katalog, überschreibbar, Herleitung wird
angezeigt" — ein Vorschlag, der ungefragt gilt, ist kein Vorschlag.

### 3.4 Der Split Eigenstrom/Einspeisung bleibt eine benannte Näherung

`StromMatrix` liefert die Aufteilung des KWK-Stroms in Eigenverbrauch und Einspeisung nur für
das **ganze Projekt**; modulscharfe Stundenreihen gibt es im Modell nicht. `ReiheJeAnlage`
verteilt sie deshalb im Verhältnis der Stromerzeugung auf die Anlagen:

```
Eigen_i     = KwkEigenGesamtMWh       × Strom_i / Σ Strom
Einsp_i     = KwkEinspeisungGesamtMWh × Strom_i / Σ Strom
bonusVoll_i = Eigen_i × 1000 × Satz_Eigen_i/100 + Einsp_i × 1000 × Satz_Einsp_i/100
```

Bei genau **einer** Anlage ist das exakt (der Anteil ist 1). Bei mehreren ist es eine Annahme —
dieselbe, die der E2-Nachtrag für die Kürzung schon getroffen hat und dort als Grenze 3 der
Zwischenlösung benannt hat. **Sie wird hier weder stillschweigend übernommen noch
stillschweigend ersetzt:** Sie steht im Code, in der Klassendokumentation und in diesem
Abschnitt.

Dazu kommt die zweite, ältere Näherung, die das E4-Protokoll festhält: Fehlt die
Strombedarfsreihe, gilt „alles ist Eigenverbrauch", und die Rechnung setzt den Eigenstromsatz
auf die Gesamtmenge an. `StromMatrix.StrombedarfFehlt` weist das aus. E6 ändert daran nichts —
es reicht die Näherung unverändert an die Anlagen durch. **Für die Handrechnungen in
Abschnitt 5 ist genau dieser Zweig aktiv** (die präparierten Kopien rechnen ohne
Stundenreihen), was sie überhaupt erst von Hand nachvollziehbar macht.

### 3.5 Der projektweite Weg bleibt vollständig erhalten

`ReiheProjektweit` ist der Rechenweg vor E6, Zeile für Zeile unverändert. Er greift, wenn sich
Anlagen- und Ergebnismodulzeilen **nicht paaren** lassen (`Bestimmbar = false`): kein
Anlagenbestand, keine Modulzeilen, oder Namen und Anzahl passen nicht zusammen. Im Bestand vom
19.08.2026 trifft das die Projekte **1023** (elf Gerätezeilen, null Anlagenzeilen) und alle
Projekte ohne BHKW. Der Weg ist konservativ und wird als Ersatz ausgewiesen.

*Begründung:* Ihn zugunsten einer „besseren" Näherung zu entfernen hätte einen zweiten
Rechenweg geändert, ohne dass ihn eine Probe abdeckt. Er bleibt, damit die Aussage „E6 ändert
nur, was E6 ändern soll" prüfbar ist.

### 3.6 Die § 6-Prüfung bleibt projektweit, solange keine Anlage ein eigenes Datum trägt

`BaueKwkgReihe` prüft zuerst, ob **irgendeine** Anlage ein eigenes Stichtags- oder
Inbetriebnahmedatum trägt. Ist das nicht so — der Zustand jeder Datenbank vor
Migrationsschritt 22 —, läuft der Bestandsblock unverändert: gleiche Bedingung, gleicher
früher Ausstieg, **gleicher Meldungstext**. Erst wenn mindestens eine Anlage ein eigenes Datum
hat, entscheidet die Prüfung je Anlage, und eine ausgefallene Anlage reißt die übrigen nicht
mit.

*Begründung:* Die Prüfung je Anlage ist bei durchgängiger Projektvorgabe **rechnerisch
identisch** — aber nicht **textgleich**: Sie würde die Anlage benennen. Auf jedem
Bestandsprojekt entstünde damit eine neue Meldung, und die Zusage „zeichengleich" wäre
verloren. Dasselbe Motiv steht hinter der Bedingung, unter der die Meldung
`WIRT_KWKG_JE_MODUL` erscheint: nur bei mehr als einer Anlage oder bei mindestens einer
eigenen Angabe.

### 3.7 Markerzeile statt Spalte für die generationsweise Nachsaat

Offener Punkt 4 des Umsetzungsstands („Nachsaat fehlender Katalogschlüssel") wird mit E6
fällig. Die Umsetzung:

- Jede Zeile der `GesetzKatalog.Vorbelegung` trägt im **Code** eine Generationsnummer.
- Eine **Markerzeile** in `Tab_Gesetzesparameter` (`Schluessel = KATALOG_GENERATION`,
  `Klasse = SYSTEM`) hält fest, bis zu welcher Generation diese Datenbank gesät wurde.
- Beim Start werden nur Zeilen mit einer **höheren** Generation nachgesät. Eine leere Tabelle
  gilt als Generation 0 (alles wird gesät, wie bisher), eine gefüllte Tabelle **ohne** Marker
  als Generation 1 (der E1-Bestand).

| Generation | Inhalt |
|---|---|
| 1 | Etappe E1, erster Seed (18.08.2026) — 182 Zeilen |
| 2 | Nachtrag zu E2 (19.08.2026): `KWKG_AUSSCHREIBUNG_GRENZE_KW` |
| 3 | Etappe E6: `KWKG_ZUSCHLAG_NEU_GRENZE_KW`, `KWKG_EIGEN_N1_GRENZE_KW` |

**Warum Markerzeile und nicht Spalte.** Eine Spalte `Generation` bräuchte DDL und wirkte damit
nicht auf Datenbanken, deren Tabelle vom E1-`CREATE TABLE` mit fester Spaltenliste angelegt
wurde — genau die Datenbanken, die die Nachsaat braucht. Zweitens ist die Generation eine
Eigenschaft des **Seeds**, nicht der Zeile: Eine vom Anwender angelegte oder geänderte Zeile
soll gar keine tragen, und `MAX(Generation)` über die Zeilen wäre eine falsche Wahrheit
(löscht der Anwender alle Zeilen der jüngsten Generation, kämen sie zurück). Eine Zeile kostet
nichts; die Pflegemaske blendet die Klasse `SYSTEM` aus.

**Das ist kein theoretischer Fall.** In der produktiven `Kenndaten.accdb` vom 19.08.2026 fehlt
`KWKG_AUSSCHREIBUNG_GRENZE_KW` — 49 KWKG-Zeilen, dieser Schlüssel nicht darunter. Der Schlüssel
kam mit dem E2-Nachtrag hinzu, der Katalog war da schon gesät. Bis hierher fing das die
Code-Konstante `WirtschaftlichkeitCtrl.KWKG_MAX_LEISTUNG_KW` auf; ein Schlüssel ohne
Rückfallebene wäre ausgefallen.

### 3.8 Migrationsschritt 22 hat kein 22b

Die Schritte 19, 20 und 21 brauchten je eine DML-Zeile, die den Bestandsrechenweg festschrieb
(`BETRAG`, `KEINE`, `ZONEN`). Schritt 22 braucht keine: **NULL selbst ist die Vorbelegung**,
weil jede Leseseite bei NULL auf den Projektwert zurückfällt, den es seit W2 gibt. Belegt:
nach der Migration stehen in allen **97** Anlagenzeilen alle acht Spalten auf NULL
(Verifikation V4).

`Tab_Energieanlagen` hat **kein `_STAMM`-Gegenstück** — sie ist eine reine Projekttabelle, die
ein Projekt mit einem Gerät verbindet; die Katalogtabellen sind `Tab_BHKW_STAMM` und
Verwandte, und die führen Gerätetechnik, keine Projektzuordnung. Die Regel „neue Spalten immer
in Projekt- **und** `_STAMM`-Tabelle" greift hier nicht.

**Textspaltenlängen großzügig** (die Lehre aus E3, Probe C2): `NACHGERUESTET` (13 Zeichen) und
`NR3_STROMINTENSIV` (17 Zeichen) bekommen beide **TEXT(24)**.

### 3.9 Zwei neue Katalogschlüssel — und warum es nicht mehr sind

Der Auftrag verlangt „neue Katalogschlüssel in `Tab_Gesetzesparameter` (Klasse `KWKG`), Werte
belegt aus dem Grundlagendokument". Der ehrliche Befund: **Etappe E1 hat die Satztabelle des
§ 7 bereits vollständig eingesät** — alle sechs Sätze des Abs. 1, beide des Abs. 3a, alle elf
des Abs. 2 (Nr. 1/2/3) und die vier Leistungsstufen. E6 braucht davon jeden einzelnen und legt
keinen davon neu an.

Neu sind zwei **Anlagengrenzen**, die von den Tranchengrenzen zu unterscheiden sind:

| Schlüssel | Wert | Quelle | Warum nicht die vorhandene Leistungsstufe |
|---|---|---|---|
| `KWKG_ZUSCHLAG_NEU_GRENZE_KW` | 50 kW | § 7 Abs. 3a | `KWKG_LEISTUNGSSTUFE_1_KW` ist eine **Tranchen**grenze des § 7 Abs. 1/2, dieser Wert eine **Anlagen**grenze der Sonderregel |
| `KWKG_EIGEN_N1_GRENZE_KW` | 100 kW | § 6 Abs. 3 Nr. 1 | dito gegenüber `KWKG_LEISTUNGSSTUFE_2_KW` |

Beide betragen heute dasselbe wie die gleichnamige Leistungsstufe. Sie stehen in verschiedenen
Normen und können sich unabhängig ändern; sie zusammenzulegen hieße, eine Novelle an der einen
Stelle stillschweigend an der anderen mitzumachen. **Der eigentliche Ertrag der Nachsaat für
Bestandsinstallationen ist der Schlüssel der Generation 2**, nicht diese beiden.

---

## 4 Wirkung auf den Bestand — mit Zahlen

**Gemeinsame Grundlage.** Eine Wegwerf-Kopie der produktiven `Kenndaten.accdb` vom
19.08.2026 13:58 (96 436 224 Byte), mit dem B-Stand von Schemastand 21 auf 22 migriert; daraus
je eine eigene Kopie für den A- und den B-Lauf, beide byte-gleich
(MD5 `6F35A55315D103D58E4F6A03DBB2F1FB`). Neun Projekte, drei Szenarien, Feature-Flag
`Kaskade_Zweikanalig` **AUS**.

### 4.1 Acht von neun Projekten sind zeichengleich

| Projekt | BHKW-Anlagen | KWKG-Satz gepflegt | Weg in E6 | Abweichung A → B |
|---|---|---|---|---|
| 1007, 1008, 1011, 1021 | keine | — | kein KWKG-Zweig | **keine — zeichengleich** |
| 1017 | 1 × 10,0 kW | nein (Bonus 0) | Reihe entfällt (`aktiv = false`) | **keine — zeichengleich** |
| 1018 | 1 × 14,5 kW | nein | dito | **keine — zeichengleich** |
| 1023 | 0 Anlagen­zeilen / 11 Gerätezeilen | nein | Ersatzweg (`Bestimmbar = false`) | **keine — zeichengleich** |
| 1024 | 1 × 21,0 kW | nein | Reihe entfällt | **keine — zeichengleich** |
| **1030** | **2 × (50 + 250 kW)** | **ja (4,00 / 8,00 ct/kWh)** | **Reihe je Anlage** | **KWKG +0,09 €/a, Kapitalwert +0,74 €** |

**24 von 27 Zeilen zeichengleich** in allen 18 Zahlen- und beiden Textspalten (Fehlgrund,
Hinweis). Die drei abweichenden Zeilen sind die drei Szenarien des Projekts 1030.

### 4.2 Projekt 1030 — die Abweichung ist Gleitkommarest, nicht Rechenweg

| Größe | A-Stand (vor E6) | B-Stand (E6) | Δ |
|---|---|---|---|
| KWKG-Erlös Jahr 1 | 44 265,127177 €/a | 44 265,218500 €/a | **+0,09 € = +0,0002 %** |
| Kapitalwert | −21 443 873,4315 € | −21 443 872,6895 € | +0,74 € = +0,000003 % |
| Gestehungskosten | 0,234843346 €/kWh | 0,234843338 €/kWh | −3 · 10⁻⁸ |

**Warum die Abweichung so klein ist — und warum das kein Zufall ist.** Beide Module liegen mit
7 475,69 und 5 385,16 Vbh **über** dem Jahresdeckel von 3 100 h (Inbetriebnahme 2027). Dann
gilt algebraisch:

```
Anteil_i / Vbh_i = (Strom_i / ΣStrom) / (Strom_i · 1000 / P_el,i) = P_el,i / (ΣStrom · 1000)
Σ_i (Anteil_i / Vbh_i) = ΣP_el / (ΣStrom · 1000) = 1 / Vbh_Projekt
```

Die Summe der Modulreihen **ist** die Projektreihe, solange jedes Modul gedeckelt wird. Der
Rest von 0,09 € entsteht aus zwei gespeicherten Rundungen: das BHKW-Aggregat führt
1 720,08 MWh, die Summe der beiden Modulzeilen 1 720,07 MWh, und die gespeicherte
Vbh des ersten Moduls (7 475,69 h) weicht von `Strom/P_el` (7 475,60 h) um 0,09 h ab, weil sie
aus dem `float`-Lauf vor der Rundung der Strommenge stammt.

> **Ein Befund, den erst diese Prüfung zutage gefördert hat.** Die Aussage „E6 ändert die
> Mehrmodulrechnung" gilt **nicht pauschal**. Sie gilt genau dann, wenn die Module den
> Jahresdeckel **unterschiedlich** treffen, wenn sie unterschiedliche Stichtage,
> Inbetriebnahmejahre, Sätze oder Kontingente haben — oder wenn eine von ihnen ausfällt.
> Liegen alle Module über dem Deckel (der wirtschaftlich häufige Fall bei Grundlastauslegung),
> ist die alte Rechnung nicht falsch, sondern zufällig richtig. Das Referenzprojekt 1030 ist
> genau dieser Fall, und deshalb reicht es als Wirkungsbeleg **nicht** aus.

---

## 5 Wirkung an präparierten Kopien — sieben Fälle, alle von Hand nachgerechnet

**Verfahren** (Muster E2-Nachtrag): je Fall eine eigene Wegwerf-Kopie, Präparation
ausschließlich **auf Datenebene**, **nicht neu simuliert** — der Rechenkern ist von dieser
Etappe nicht berührt, und die geprüfte Kette hängt an (P_el je Anlage, Strom je Modul,
Vbh je Modul, den acht neuen Feldern). Ohne Stundenreihen gilt „alles ist Eigenverbrauch"
(Abschnitt 3.4), also `bonusVoll = Strom × 1000 × 0,04 €/kWh`.

**Ausgangsdaten Projekt 1030** aus dem gespeicherten Lauf:

```
Modul 1  „BHKW EW M 50 S [K] Erdgas"   P_el  50 kW   Strom    373,78 MWh   Vbh_el 7 475,69 h
Modul 2  „Agenitor 306(250kw.el) Gas"  P_el 250 kW   Strom  1 346,29 MWh   Vbh_el 5 385,16 h
Aggregat                               P_el 300 kW   Strom  1 720,08 MWh   Vbh_el 5 733,59 h
Projekt: Satz 4,00/8,00 ct/kWh · Kontingent 30 000 h · Deckel aus der Staffel
         Stichtag 01.09.2026 · Inbetriebnahme 01.03.2027 ⇒ Förderbeginn 2027, Deckel 3 100 h
```

| # | Präparation | KWKG Jahr 1 A → B | Δ | Kapitalwert A → B | Handrechnung B |
|---|---|---|---|---|---|
| **H1** | **Einmodulprojekt 1018** (14,5 kW, 14,96 MWh, Vbh 1 031,63 h), KWKG-Satz 4,00/8,00 neu gepflegt, IBN 2026 | 598,40 → **598,40** | **0,00** | — (Energiekosten nicht bestimmbar) | 14,96 · 1000 · 0,04 = 598,40; min(1 031,63; 3 300; 30 000) = 1 031,63 ⇒ voller Betrag ✓ |
| **H2** | 1030 **unverändert** | 37 200,06 → **37 199,93** | −0,14 € = −0,0004 % | −21 501 273,52 → −21 501 274,65 | 14 951,20 · 3 100/7 475,69 = 6 199,93 · 53 851,60 · 3 100/5 385,16 = 31 000,00 ⇒ **37 199,93** ✓ |
| **H3** | 1030, **Modul 2 unter den Jahresdeckel**: Strom 500 MWh, Vbh 2 000 h; Aggregat 873,78 MWh / 2 912,6 h | 34 951,20 → **26 199,93** | **−8 751,27 € = −25,04 %** | −21 501 880,08 → −21 514 375,40 (−0,058 %) | A: 34 951,20 · min(2 912,6; 3 100)/2 912,6 = 34 951,20 (**ungedeckelt**) · B: 6 199,93 + 20 000,00 = **26 199,93** ✓ |
| **H4** | 1030, **Modul 2 auf 600 kW** (über der Ausschreibungsgrenze) | 6 200,04 → **6 199,93** | −0,11 € = −0,002 % | −21 753 133,20 → −21 753 134,10 | nur Modul 1: 14 951,20 · 3 100/7 475,69 = **6 199,93** ✓ |
| **H5** | 1030, **Kontingent je Anlage**: Modul 1 = 10 000 h, Modul 2 = 30 000 h | 37 200,06 → **37 199,93** | −0,14 € (Jahr 1) | −21 501 273,52 → **−21 532 907,86** (**−31 634,34 € = −0,147 %**) | Modul 1 erschöpft nach 3 100 + 2 900 + 2 700 + 1 300 = 10 000 h, also im **vierten** Jahr; A führt für beide zusammen 30 000 h ✓ |
| **H6** | 1030, **verschiedene Inbetriebnahmejahre**: Modul 1 = 2026, Modul 2 = 2029 | 37 200,06 → **33 599,92** | **−3 600,14 € = −9,68 %** | −21 501 273,52 → −21 503 380,39 | Deckel 2026 = 3 300, 2029 = 2 700: 14 951,20 · 3 300/7 475,69 = 6 599,92 · 53 851,60 · 2 700/5 385,16 = 27 000,00 ⇒ **33 599,92** ✓ |
| **H7** | 1030, **Stichtag je Anlage**: Modul 2 = 01.06.2027 (nach dem 31.12.2026) | 37 200,06 → **6 199,93** | **−31 000,14 € = −83,33 %** | −21 501 273,52 → **−21 753 134,10** (**−251 860,58 € = −1,17 %**) | Modul 2 fällt an § 6 aus, Modul 1 bleibt: **6 199,93** ✓ |
| **H8** | 1030, **Satz je Anlage**: Modul 1 auf 8,00/16,00 ct/kWh (§ 7 Abs. 3a, neue Anlage ≤ 50 kW) | 37 200,06 → **43 399,85** | **+6 199,79 € = +16,67 %** | −21 501 273,52 → **−21 450 903,37** (+50 370,15 € = +0,234 %) | 373,78 · 1000 · 0,08 = 29 902,40; · 3 100/7 475,69 = 12 399,85 + 31 000,00 = **43 399,85** ✓ |

**Alle acht Handrechnungen treffen den gerechneten Wert auf den Cent.**

**Vier Aussagen der Tabelle:**

1. **H3 ist der Kern der Etappe.** Der Altstand zahlte Modul 1 seinen Zuschlag **ungekürzt**,
   obwohl es 7 476 Vbh läuft — weil der leistungsgewichtete Projektmittelwert (2 912,6 h) unter
   dem Deckel von 3 100 h blieb. Das ist genau der Fall, den Restbefund 1 aus E2 beschreibt.
2. **H4 zeigt, dass E6 die 500-kW-Grenze nicht neu erfindet.** Der E2-Nachtrag hat sie bereits
   je Anlage geprüft; E6 ändert nur die Buchführung. Die verbleibenden 0,11 € sind derselbe
   Gleitkommarest wie in H2.
3. **H5 wirkt erst über die Jahre.** Im Jahr 1 ändert sich nichts; über 20 Jahre kostet das
   kleinere Kontingent des ersten Moduls 31 634 € Kapitalwert. Ein Blick allein auf „Jahr 1"
   hätte diesen Fall für wirkungslos gehalten.
4. **H7 ist der teuerste Fall und der eigentliche Anlass.** Ein einziges Datum an der falschen
   Anlage kostete im Altstand nichts oder alles — je nachdem, in welche Richtung man es
   pflegte. Jetzt trägt jede Anlage ihres.

### 5.1 Die Meldungen

| Fall | Meldung (deutsch, gekürzt) |
|---|---|
| H2, H5, H6, H8 | „KWKG: Zuschlag je BHKW-Modul gerechnet — BHKW EW M 50 S [K] Erdgas (50 kW, 7.476 h/a, 4,00/8,00 ct/kWh, 30.000 h), Agenitor 306(250kw.el) Gas (250 kW, 5.385 h/a, 4,00/8,00 ct/kWh, 30.000 h)." |
| H4 | „KWKG: Über der Ausschreibungsgrenze von 500 kW und deshalb ohne Zuschlag: Agenitor 306(250kw.el) Gas (600 kW) … Die übrigen Anlagen mit zusammen 50 kW rechnen weiter." (Text aus dem E2-Nachtrag, **unverändert**) |
| H7 | „KWKG: Agenitor 306(250kw.el) Gas (250 kW) — Bestellung/Genehmigung nach dem 31.12.2026 und damit nach geltendem Recht nicht förderfähig (§ 6 KWKG 2025, Regulierungsrisiko Novelle); für diese Anlage kein Zuschlag." **und** die Modulzeile nur noch mit Modul 1 |
| H1 | **keine neue Meldung** — Einmodulprojekt ohne eigene Angaben |

---

## 6 Der Zuschlagsvorschlag — acht geprüfte Fälle

Gerechnet mit dem Katalog der migrierten Kopie, Stichtagsjahr 2026, alle Sätze aus
`Tab_Gesetzesparameter`:

| P_el | Anlagenart | Eigenstromfall | Einspeisung | Eigenstrom | Herleitung Einspeisung |
|---|---|---|---|---|---|
| 50 kW | neu | Nr. 1 | **16,0000** | **8,0000** | § 7 Abs. 3a, geht den Leistungsanteilen vor |
| 300 kW | neu | keiner | **5,5667** | **0,0000** | 50 × 8,00 + 50 × 6,00 + 150 × 5,00 + 50 × 4,40 |
| 300 kW | neu | Nr. 2 | 5,5667 | **2,4167** | Eigen: 50 × 4,00 + 50 × 3,00 + 150 × 2,00 + 50 × 1,50 |
| 300 kW | neu | Nr. 1 | 5,5667 | **0,0000** | Eigen: „gilt nur bis 100 kW; diese Anlage hat 300,0 kW" |
| 300 kW | neu | Nr. 3 | 5,5667 | **3,9683** | Eigen: 50 × 5,41 + 200 × 4,00 + 50 × 2,40 |
| 3 000 kW | neu | keiner | **4,1833** | 0,0000 | … + 1 750 × 4,40 + 1 000 × **3,40** |
| 3 000 kW | nachgerüstet | keiner | **4,0833** | 0,0000 | … + 1 000 × **3,10** (§ 7 Abs. 1, letzte Zeile) |
| 250 kW | modernisiert | Nr. 2 | **5,8000** | **2,6000** | 50 × 8,00 + 50 × 6,00 + 150 × 5,00 (die 250-kW-Grenze wird genau getroffen) |

Nachgerechnet: 400 + 300 + 750 + 220 = 1 670 ÷ 300 = 5,5667 ✓ · 270,5 + 800 + 120 = 1 190,5 ÷ 300
= 3,9683 ✓ · 400 + 300 + 750 + 7 700 + 3 400 = 12 550 ÷ 3 000 = 4,1833 ✓ · mit 3 100 statt 3 400
⇒ 12 250 ÷ 3 000 = 4,0833 ✓ · 1 450 ÷ 250 = 5,8000 ✓

---

## 7 Verifikation

### 7.1 Referenzlauf A/B

Beide Stände aus einem Export **außerhalb des Repos** gebaut (`git archive 9ed551d` für A,
Überlagerung der 13 Arbeitsbaum-Dateien für B), jeweils mit dem mitgelieferten
`Referenzlauf.csproj` (ProjectReference auf die App ⇒ Exe und DLL konsistent). Eine gemeinsame,
mit dem B-Stand von 21 auf 22 migrierte Wegwerf-Kopie, daraus je eine eigene Datei für A und B.
Feste Projektliste, Feature-Flag `Kaskade_Zweikanalig` **AUS**.

```
Referenzlauf.exe migration <ScratchDB> <Scratch> --nokopie        (21 -> 22)
A = 9ed551d      -> projekt <id> <Scratch>\RA\Projekt_<id>  <ScratchDB_A>
B = 9ed551d + E6 -> projekt <id> <Scratch>\RB\Projekt_<id>  <ScratchDB_B>
Referenzlauf.exe vergleich RA RB
Referenzlauf.exe vergleich Referenzlaeufe\2026-08-19_B5 RB
```

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

GESAMT: PASS (2 366 177 Werte innerhalb der Toleranz)
```

**Byte-Vergleich: 216 von 216 CSV identisch**, gegen den A-Stand **und** gegen die eingefrorene
Basis `2026-08-19_B5`. Kein neuer Ergebnisschlüssel — E6 fasst den Rechenkern nicht an.
`pruefen`: GESAMT plausibel.

### 7.2 Verifikationstabelle

| # | Prüfung | Methode | Ergebnis |
|---|---|---|---|
| V1 | Engine unberührt | `git status` über `Allgemein/Simulation/*.cs` und `Allgemein/BhkwPlan.cs` | **leer** (im Ordner ist nur `Lokalisierung_Katalog.md` geändert — Dokumentation) |
| V2 | Simulationsergebnisse gegen die Basis B5 | `Referenzlauf.exe vergleich`, gemeinsame DB-Kopie, Flag AUS | **9/9 PASS**, 2 366 177 Werte |
| V3 | dito A gegen B | dito | **9/9 PASS** |
| V4 | Byte-Identität gegen B5 und gegen A | `cmp` je Datei | **216/216 gleich**, 0 Abweichungen, beide Male |
| V5 | Plausibilität des B-Laufs | `Referenzlauf.exe pruefen` | **plausibel** (alle Projekte OK) |
| V6 | Wirtschaftlichkeit A gegen B | Harnisch auf `BerichtsDatenSammler.Sammle` + `WirtschaftlichkeitCtrl.Berechne`, gemeinsame Kopie, 9 Projekte × 3 Szenarien | **24/27 Zeilen zeichengleich** in allen 18 Zahlen- und beiden Textspalten; die 3 abweichenden sind Projekt **1030** (Mehrmodul, **gewollt**) |
| V7 | Größe der gewollten Abweichung | dieselbe Probe | KWKG **+0,09 €/a (+0,0002 %)**, Kapitalwert **+0,74 €**; Begründung Abschnitt 4.2 |
| V8 | Migration 21 → 22 | `Referenzlauf.exe migration` auf Wegwerf-Kopie | **Schemastand 22**, Schritt 22 „OK" |
| V9 | Spalten korrekt angelegt | `GetOleDbSchemaTable` | `Tab_Energieanlagen` 57 → **65** Spalten (8 neu, Positionen 58–65, alle **angehängt**), TEXT(24) für beide Textspalten |
| V10 | **Kein DML** — NULL ist die Vorbelegung | `SELECT COUNT(*) , COUNT(<spalte>) …` über alle Anlagenzeilen | 97 Zeilen, **0** belegte Werte in **allen acht** Spalten |
| V11 | Doppelstart idempotent | zweiter Migrationslauf `--nokopie` | „Schritt 22 …: bereits erledigt", Stand bleibt 22, weiterhin 65 Spalten |
| V12 | **Rundprobe** des Schreibwegs | `KwkgAnlagenCtrl.Speichere` → `LadeGruppe`, alle acht Felder, zwei Anlagen | **16/16 Felder wertgleich**, 0 Abweichungen |
| V13 | Rundprobe **zurück auf NULL** | dieselben Felder wieder geleert und gelesen | **2/2 Zeilen wieder vollständig leer** — der Weg zurück zum Projektwert funktioniert |
| V14 | Nachsaat sät die fehlenden Schlüssel | `StelleKatalogSicher` auf der migrierten Kopie | 182 → **186** Zeilen: `KWKG_AUSSCHREIBUNG_GRENZE_KW` (Gen. 2), `KWKG_ZUSCHLAG_NEU_GRENZE_KW` und `KWKG_EIGEN_N1_GRENZE_KW` (Gen. 3) plus Markerzeile |
| V15 | Nachsaat ist idempotent | zweiter Aufruf | `ZuletztNachgesaet = 0`, Zeilenzahl bleibt 186 |
| V16 | **Bewusst gelöschte Zeile bleibt gelöscht** | `DELETE` von `KWKG_PAUSCHALE_BIS2KW` (Generation 1), dann `StelleKatalogSicher` | 185 → **185**, der Schlüssel kommt **nicht** zurück |
| V17 | A-Stand sät nicht nach (Gegenprobe) | dieselbe Kopie im A-Lauf | **182 Zeilen**, unverändert |
| V18 | Tranchenrechnung § 7 | acht Fälle gegen Handrechnung (Abschnitt 6) | **8/8 getroffen**; „Klasse statt Tranche" wäre bei 300 kW 21 % zu niedrig |
| V19 | Eigenstrom ohne Tatbestand | Fall „300 kW / keiner" | **0,00 ct/kWh** mit Begründung statt einer Lücke |
| V20 | § 7 Abs. 3a geht vor | Fall „50 kW / neu" | **16,00 / 8,00 ct/kWh**, Herleitung nennt die Vorrangregel |
| V21 | Wirkung je Anlage | sieben präparierte Kopien H2–H8 | −25,04 % / −9,68 % / −83,33 % / +16,67 % / −0,147 % Kapitalwert; **alle = Handrechnung** |
| V22 | Einmodulprojekt unverändert | H1 (1018 mit gepflegtem Satz) | A = B = **598,40 €/a**, keine neue Meldung |
| V23 | Ersatzweg unverändert | Projekt 1023 (0 Anlagenzeilen) in V6 | zeichengleich |
| V24 | **Sprachgleichheitsprobe** | derselbe Fall H7 mit `CurrentUICulture = en-US` | **Zahlen und Fehlgrund identisch**, Meldungen englisch — kein Anzeigetext ist Steuerwert |
| V25 | Ressourcen in beiden `.resx` und im Designer | `grep` je Schlüssel | **9/9** in `Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` |
| V26 | Build | `MSBuild WP-Plan.sln -p:Platform=x86`, Ausgabe in den Scratch-Ordner | **0 Fehler, exakt 6 Warnungen** (CS0108 ×2, CS0109 ×2, CS1998, CS4014) |
| V27 | Kodierung und Zeilenenden | `file`, CR-Zählung gegen Zeilenzahl, Suche nach U+FFFD je Datei | 13 Dateien, **alle UTF-8**, **alle durchgehend CRLF** (CR-Zahl = Zeilenzahl), **0 Ersatzzeichen** |
| V28 | Diff additiv | `git diff --stat` | 1 220 Zeilen zugefügt, 146 entfernt — die 146 sind der umgebaute Rumpf von `BaueKwkgReihe` und die alte Fassung von `StelleKatalogSicher` |
| V29 | Produktivdatenbank nur gelesen | Schemastand und Katalogzeilen der produktiven Datei nach allen Läufen | **Schemastand 21** (nicht 22) und **182 Katalogzeilen** (nicht 186) — kein E6-Artefakt hat sie erreicht. Siehe Kasten unten |
| V30 | `bin\` des Repos unberührt | jeder Build ausschließlich mit `-p:OutDir=<Scratch>`; A und B außerhalb des Repos | **erfüllt** |

> **Zu V29, offen gesagt.** Ein MD5-Vergleich „vor und nach" ist hier **nicht** möglich: Die
> produktive `Kenndaten.accdb` war während der gesamten Etappe von der laufenden Anwendung
> geöffnet (`Kenndaten.laccdb` vorhanden), `Get-FileHash` konnte sie deshalb gar nicht öffnen,
> und ihr Zeitstempel und ihre Größe haben sich zwischenzeitlich geändert (96 436 224 Byte /
> 13:58 → 92 700 672 Byte / 14:46) — durch die Sitzung des Anwenders, nicht durch diese
> Etappe. Der belastbare Nachweis ist deshalb ein inhaltlicher: Die Datei steht weiterhin auf
> **Schemastand 21** und führt weiterhin **182** Katalogzeilen. Hätte irgendein Lauf dieser
> Etappe sie berührt, stünden dort 22 und 186. Gelesen wurde sie genau einmal, um die
> Wegwerf-Kopie anzulegen.

---

## 8 Welche Größen sich ändern — und welche nicht

**Unverändert:**

- alle Simulationsergebnisse (216/216 CSV byte-identisch, gegen A **und** gegen B5),
- alle Wirtschaftlichkeitswerte der acht Einmodul- und BHKW-losen Projekte (zeichengleich),
- der projektweite Rechenweg als Ersatzweg (`Bestimmbar = false`),
- die neun Meldungstexte der E2-Nachträge (wortgleich),
- die § 6-Prüfung und ihre Meldungen, solange keine Anlage ein eigenes Datum trägt,
- alle 182 Katalogzeilen der Generation 1.

**Verändert:**

- der KWK-Zuschlag bei **mehr als einer** BHKW-Anlage: eine Reihe je Anlage, jahresweise
  summiert, mit eigenem Deckel und eigenem Kontingent,
- § 6, § 8a und der Heizöl-Ausschluss: je Anlage, mit dem Datum **dieser** Anlage,
- `Tab_Energieanlagen`: acht neue Spalten (Schemastand 22),
- `Tab_Gesetzesparameter`: drei nachgesäte Schlüssel und eine Markerzeile,
- der Parameterdialog: zwei umbenannte Beschriftungen („Stichtag, **Vorgabe je Anlage**") und
  eine neue Schaltfläche,
- der Ergebnishinweis bei Mehrmodulanlagen: eine Zeile je gerechnetem Modul.

---

## 9 Offene Punkte

### Für Etappe E7 (Bericht)

1. **Die Modulaufzählung im Hinweisfeld ist eine Notlösung.** Sie nennt je Anlage Leistung,
   Vbh, beide Sätze und das Kontingent — bei drei oder mehr Modulen wird die Zeile lang. In
   den Bericht gehört das als **Tabelle** mit einer Zeile je Modul; die Daten liegen alle vor.
2. **Der KWK-Zuschlag wird weiterhin nur als Jahr-1-Wert persistiert**
   (`Tab_ErgebnisWirtschaftlichkeit.KWKGErloes`). Die Reihe je Anlage entsteht bei jedem Lauf
   neu und wird nicht gespeichert; für eine Mehrjahrestabelle je Modul wäre eine
   Ergebnistabelle nötig.
3. **Die angezeigte Kennzahl `KWKGVbhElektrisch` bleibt die projektweite, leistungsgewichtete
   Größe** — auch dann, wenn die Rechnung modulscharf läuft. Das ist Absicht (der
   Regressionsnachweis bleibt sauber, dieselbe Entscheidung wie im E2-Nachtrag), heißt aber:
   Reiter und Bericht zeigen eine Zahl, mit der die Rechnung nicht mehr unmittelbar rechnet.

### Fachlich zu entscheiden

4. **Der Split Eigenstrom/Einspeisung je Modul ist eine Verteilungsannahme** (Abschnitt 3.4).
   Modulscharf wäre er nur mit Stundenreihen je Anlage — das ist eine Änderung am Rechenkern
   und gehört nicht in W4.
5. **Die Anlagenart bestimmt das Kontingent nicht automatisch.** § 8 Abs. 2 und 3 knüpfen
   6 000 / 15 000 / 30 000 Vbh an die Kostenschwelle und den Anlagenabstand; die Schwellen und
   Mindestalter stehen seit E1 im Katalog, gelesen wird noch keiner. Der Dialog führt das
   Kontingent deshalb als Zahl, nicht als Ableitung. Das wäre die nächste Ausbaustufe des
   Vorschlags.
6. **Der Heizöl-Ausschluss hängt weiterhin an der Sekundärquelle** (Grundlagen, Abschnitt 6,
   Punkt 3) und an der Näherung „Neuanlage = Inbetriebnahme ≥ 2025". Seit E6 entscheidet das
   Datum **der Anlage**, die Rechtsgrundlage ist dieselbe geblieben.

### Aus dieser Etappe entstanden

7. **`Tab_ErgebnisBHKW.VbhElektrisch` und die Summe der Modulzeilen weichen um Rundungen ab**
   (1 720,08 gegen 1 720,07 MWh; 7 475,69 gegen 7 475,60 h). Beide Größen entstehen im
   `float`-Lauf und werden getrennt auf zwei Nachkommastellen gerundet. Das ist die Ursache
   der 0,09 € in Abschnitt 4.2 und **kein Fehler dieser Etappe** — aber es heißt, dass „Summe
   der Module" und „Aggregat" nie exakt gleich sind. Wer künftig eine Bilanzprobe darauf
   aufsetzt, muss eine Toleranz vorsehen.
8. **Nicht getroffene Ergebnis-Modulzeilen fallen weiterhin aus der Summe.** `ModulJeAnlage`
   liefert eine Zuordnung nur, wenn **jede Anlage** eine Modulzeile bekommt; überzählige
   Modulzeilen bleiben unbeachtet, und ihre Strommenge fehlt dann in `Σ Strom`. Das Verhalten
   stammt aus dem E2-Nachtrag und ist unverändert; in der Referenzmenge tritt es nicht auf.
9. **Der Modul-Dialog speichert sofort.** Er hat eigene Schaltflächen „Speichern" und
   „Abbrechen"; ein „Abbrechen" im aufrufenden Parameterdialog nimmt die bereits gespeicherten
   Modulwerte **nicht** zurück. Das ist bei einem eigenen Dialog üblich, sollte aber bei der
   nächsten Überarbeitung der Maske erwähnt werden.

### Referenzbasis

10. **Ein neuer Basis-Freeze steht weiterhin aus** (E8). `2026-08-19_B5` bleibt gültig:
    216 von 216 CSV sind byte-identisch, E6 hat den Rechenkern nicht angefasst. Für die
    **Wirtschaftlichkeit** gibt es keine eingefrorene Basis; sie wird bei jeder Etappe als A/B
    gegen den Vorgängerstand gemessen.
11. **Die Referenzmenge deckt den Kaskadenfall nur zur Hälfte ab.** Projekt 1030 hat zwei
    Module, aber beide liegen über dem Jahresdeckel — genau der Fall, in dem die alte Rechnung
    zufällig richtig war (Abschnitt 4.2). Für einen belastbaren Regressionstest der
    modulscharfen Deckelung fehlt ein Referenzprojekt, dessen Module den Deckel
    **unterschiedlich** treffen.

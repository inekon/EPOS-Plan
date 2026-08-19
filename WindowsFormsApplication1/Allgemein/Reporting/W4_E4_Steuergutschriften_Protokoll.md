# W4 · Etappe E4 — Energiesteuer- und Stromsteuergutschrift

**Stand: 19.08.2026.** Umsetzung der Etappe **E4** aus
[`Konzept_BHKW_Kosten_Erloese.md`](Konzept_BHKW_Kosten_Erloese.md), Abschnitt 4.2.
Ausgangsstand `20958be` (= E3). Faktenbasis:
[`Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`](../../../Grundlagen_KWKG_Energiesteuer_Stromsteuer.md),
Abschnitte 2, 3 und 4, ergänzt um die Recherche vom 19.08.2026 (Abschnitt 2 dieses
Protokolls).

**Ergebnis in vier Sätzen.** Die Anwendung rechnet ab jetzt die Energiesteuerentlastung
nach § 53 beziehungsweise § 53a Abs. 5 EnergieStG, die Stromsteuerbefreiung nach
§ 9 Abs. 1 Nr. 3 StromStG und die Stromsteuerentlastung nach § 9b StromStG als
**jahresscharfe Gutschriftreihen** im Kapitalwert mit. Die gesetzlichen Bedingungen
werden **erfasst statt angenommen** — sechs neue Projektangaben (Migrationsschritt 20),
und jede nicht erfüllte Bedingung führt zu 0 € **plus Begründung im Klartext**, nie zu
einer stillen Null. Für Bestandsprojekte ändert sich **nichts**: Die Vorbelegung ist
jeweils der Wert, der keine Gutschrift auslöst (9/9 PASS, 216/216 CSV byte-identisch,
alle 54 Wirtschaftlichkeitswerte A gegen B wertgleich). An präparierten Kopien des
Projekts 1030 entstehen bis zu **26.876 € Energiesteuer**, **28.565 €
Stromsteuerbefreiung** und **61.150 € Stromsteuerentlastung** im Jahr 1.

> **Hinweis zum Arbeitsstand.** Während der Umsetzung hat das automatische
> Synchronisationsskript des Repos (`GitHub_Sync.bat`, `git add -A` und Push nach
> `origin/main`) den jeweiligen Zwischenstand **zweimal von sich aus committet**:
> `aac55ca` um 07:13 und `71cb94e` um 07:35, beide betitelt „Synchronisation vom
> 19.08.2026". Der Auftrag lautete „nicht committen"; diese beiden Commits stammen
> **nicht** aus der Umsetzung, sondern aus dem laufenden Skript. Wer den Stand prüfen
> will, vergleicht deshalb gegen **`20958be`** (Ende E3), nicht gegen `HEAD~1`.
>
> **Nebenbefund:** `71cb94e` enthält außerdem die **Löschung von `api_gemini.txt`** im
> Repo-Wurzelverzeichnis. Diese Datei gehört nicht zum Änderungsumfang der Etappe E4 und
> wurde von der Umsetzung nicht angefasst; sie verschwand zwischen 07:16 und 07:20 aus
> dem Arbeitsbaum und wurde vom Skript mit `git add -A` aufgenommen. Wiederherstellbar
> mit `git checkout 20958be -- api_gemini.txt`.

---

## 1 Was umgesetzt wurde

| # | Gegenstand | Datei : Zeile |
|---|---|---|
| 1 | **Rechenkern der Gutschriften** als reine Funktion über DTOs (L9), ohne Datenbankzugriff | `Allgemein/Wirtschaftlichkeit/SteuerGutschriftRechner.cs:1-616` (neu) |
| 2 | Energiesteuer § 53 / § 53a Abs. 5 mit Einheitenumrechnung und Nutzungsgradprüfung | `SteuerGutschriftRechner.cs:216-296` (`Energiesteuer`), `:341-397` (`MengeInGesetzlicherEinheit`) |
| 3 | Aufteilung des Brennstoffs auf Strom und Wärme, zwei Verfahren | `SteuerGutschriftRechner.cs:317-333` (`Stromanteil`) |
| 4 | Stromsteuerbefreiung § 9 Abs. 1 Nr. 3, vier Bedingungen **je Anlage** | `SteuerGutschriftRechner.cs:428-521` (`StromsteuerBefreiung`) |
| 5 | CO₂ je kWh **Energieertrag** aus dem EBeV-Faktor (L11) | `SteuerGutschriftRechner.cs:523-545` (`Co2JeEnergieertrag`) |
| 6 | Stromsteuerentlastung § 9b mit Sockelbetrag | `SteuerGutschriftRechner.cs:551-590` (`StromsteuerEntlastung`) |
| 7 | Herkunftszeile je verwendetem Satz aus `WertMitHerkunft` | `SteuerGutschriftRechner.cs:596-612` (`Herkunft`) |
| 8 | **Benannte Erlösreihen** statt einer Reihe (L1) — Signaturänderung | `Allgemein/Wirtschaftlichkeit/KapitalwertRechner.cs:47-102` (`ErloesReihe`), `:141-166` (`Rechne`), `:196-200` (Summenbildung) |
| 9 | Jahresscharfe Gutschriftreihen bauen und anhängen | `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:1247-1327` (`BaueSteuerReihen`, `Reihe`) |
| 10 | Mengen und Projektangaben einsammeln, Anlagenliste wie beim KWKG-Guard | `WirtschaftlichkeitCtrl.cs:1329-1420` (`BaueSteuerEingabe`, `BaueSteuerAnlage`) |
| 11 | Abrechnungseinheit und Heizwerte je Träger — Projektwert vor Katalogwert | `WirtschaftlichkeitCtrl.cs:1422-1489` (`TraegerEinheit`, `Traeger`) |
| 12 | Zuordnung Brennstoff → Energiesteuersatz und → CO₂-Faktor, fossil ja/nein | `WirtschaftlichkeitCtrl.cs:1491-1590` (`EnergiesteuerSchluessel`, `Co2Schluessel`, `FossilerBrennstoff`) |
| 13 | Förderbeginn an **einer** Stelle (KWKG-Reihe und Steuerreihen) | `WirtschaftlichkeitCtrl.cs:1234-1243` (`Foerderbeginn`) |
| 14 | Modulzuordnung liefert die Zeile statt nur der Strommenge | `WirtschaftlichkeitCtrl.cs:1673-1712` (`ModulJeAnlage`, `StromVon`) |
| 15 | Brennstoff-ID der Anlage aus derselben zweistufigen Auflösung wie der Öl-Guard | `WirtschaftlichkeitCtrl.cs:1790-1826` (`BrennstoffKategorie`, `BrennstoffId`) |
| 16 | Parameter lesen, schreiben und die sechs Spalten vorsorglich anlegen | `WirtschaftlichkeitCtrl.cs:329-350` (Spalten), `:404-419` (Lesen), `:539-604` (Schreiben) |
| 17 | Ergebnisspalten `EnergiesteuerErloes`, `StromsteuerBefreiung`, `StromsteuerEntlastung`, `SteuerHerkunft` | `WirtschaftlichkeitCtrl.cs:66-95` (Konstanten), `:322-327` (Anlage), `:2379-2404` (INSERT), `:2540-2545` (Lesen) |
| 18 | **Migrationsschritt 20** — Spaltenkatalog und Begründung | `Allgemein/Update/SchemaKatalog.cs:740-855` |
| 19 | **Migrationsschritt 20** — Schrittnummer, Ausführung, Zählwerk, `ZIEL_VERSION 19 → 20` | `Allgemein/Update/SchemaMigration.cs:77`, `:396-431`, `:747-757`, `:1085-1096`, `:1341-1409` |
| 20 | Persistenzwerte der sechs Angaben (drei Unternehmensarten, drei Entlastungswahlen, zwei Aufteilungsmethoden) | `Allgemein/DbWerte.cs:311-407` |
| 21 | DTO-Felder der Parameter und des Ergebnisses samt Nachweiszeile | `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitDaten.cs:57-104`, `:131-146`, `:333-357` |
| 22 | Eingabeblock „BHKW — Energie- und Stromsteuer" im Parameterdialog | `Views/Wirtschaftlichkeit/Form_WirtschaftlichkeitParameter.cs:84-113`, `:262-311` (Helfer), `:390-405` (Speichern) |
| 23 | Vier neue Zeilen im Wirtschaftlichkeitsreiter | `Views/Wirtschaftlichkeit/UcWirtschaftlichkeit.cs:548-563` |
| 24 | Dieselben Zeilen im Word-Baustein und im Excel-Blatt | `Allgemein/Bericht/Bausteine/BausteineWirtschaftlichkeit.cs:245-256`, `Allgemein/Bericht/ExcelBerichtGenerator.cs:260-266`, `:277-279` |
| 25 | 27 Ressourcenschlüssel in beiden Sprachen samt Designer und Katalognachtrag | `MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs`, `Allgemein/Simulation/Lokalisierung_Katalog.md` |

**14 Quelldateien** (13 geändert, 1 neu) **und vier Dokumente**: dieses Protokoll (neu),
`Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`, `W4_Umsetzungsstand.md`,
`Allgemein/Simulation/Lokalisierung_Katalog.md`.

**Engine unberührt:** `git diff --name-only 20958be -- Allgemein/Simulation/` liefert
**null Treffer**.

---

## 2 Die Recherche zur Brennstoffaufteilung — und was sie umgeworfen hat

Der Auftrag verlangte zu klären, ob das Energiesteuerrecht ein Verfahren vorschreibt,
mit dem der Brennstoff eines BHKW auf Strom- und Wärmeerzeugung aufgeteilt wird.

### 2.1 Das Ergebnis: **Für ein Motor-BHKW gibt es diese Aufteilung nicht**

Die Frage geht von einer Prämisse aus, die der Gesetzeswortlaut nicht trägt.

**§ 53 Abs. 2 Satz 1 EnergieStG** (Primärquelle, gesetze-im-internet.de, abgerufen
19.08.2026):

> „Energieerzeugnisse gelten nur dann als zur Stromerzeugung verwendet, soweit sie in der
> Stromerzeugungsanlage unmittelbar am Energieumwandlungsprozess teilnehmen."

Beim Verbrennungsmotor nimmt der **gesamte** zugeführte Brennstoff unmittelbar am
Umwandlungsprozess teil; die Abwärme fällt gekoppelt an. Satz 2 nimmt ausdrücklich nur
**Komponenten** aus — Dampferzeuger ohne Stromerzeugungsnutzen, nachgeschaltete
Abluftbehandlung, Zusatzfeuerungen, deren Wärme vor der Wärmekraftmaschine ausgekoppelt
wird. Das ist eine **anlagentechnische** Abgrenzung, kein energetischer Quotient.

Die **Dienstvorschrift Energieerzeugung** der Zollverwaltung sagt zum Schaubild des
§ 53 Abs. 1 wörtlich: „Wärme – genutzt oder ungenutzt – wird nicht betrachtet".

Der einzige gesetzlich angeordnete „Anteil" steht in **§ 53 Abs. 1 Satz 2** und meint
etwas anderes: Dient die **mechanische** Energie an der Welle neben der Stromerzeugung
auch anderen Zwecken (Generator **und** Luftverdichter am selben Motor), wird nur der auf
die Stromerzeugung entfallende Anteil entlastet. Für ein Standard-BHKW ist dieser
Tatbestand gegenstandslos — Formular 1131 fragt ihn in Zeile 9 als Ankreuzfeld ab.

**Vorgeschrieben ist nicht Rechnen, sondern Messen.** § 98 Abs. 1 Satz 1 EnergieStV:

> „Zur Ermittlung der entlastungsfähigen Mengen sind die zur Stromerzeugung oder zur
> gekoppelten Erzeugung von Kraft und Wärme eingesetzten Energieerzeugnisse und die
> weiteren eingesetzten Brennstoffe und Hilfsenergie zu messen."

Satz 2 lässt andere Ermittlungsmethoden zu, wenn Messung nicht oder nur mit
unvertretbarem Aufwand möglich ist. Das amtliche **Formular 1131** verlangt in seinem
Berechnungsteil ausschließlich Art und **Menge** des Energieerzeugnisses (Liter, kg, MWh,
GJ) — **kein** Feld für Strommenge, Wärmemenge, elektrischen Wirkungsgrad, Stromkennzahl
oder Nutzungsgrad. Die einzige geforderte Abgrenzung nennt die Anleitung zu Zeile 12:
Aus Erdgasrechnungen ist herauszurechnen, was in **Heizkesseln, Spitzenlastkesseln,
Kochstellen und Abluftanlagen** eingesetzt wurde — also BHKW **gegen** Kessel, nicht
Strom gegen Wärme.

**Die Stromkennzahl existiert im Energiesteuerrecht nicht**; der Begriff kommt in der
Dienstvorschrift kein einziges Mal vor. Er stammt aus dem KWKG und der
Richtlinie 2012/27/EU. Der **Nutzungsgrad** ist in § 3 Abs. 3 EnergieStG legaldefiniert
(genutzte mechanische und thermische Energie ÷ zugeführte Energie, heizwertbezogen) und
gilt für § 3 und § 53a — **nicht** für eine Mengenaufteilung nach § 53.

**Finnische Methode, Stromverlustmethode und AGFW FW 308 sind Negativbefunde**: In keiner
geprüften Primärquelle des Energiesteuerrechts kommen sie vor. Sie gehören in die
Emissionsbilanzierung, nicht in die Steuerentlastung.

Die einzige verordnungsrechtliche Aufteilungsformel überhaupt ist § 98 Abs. 2 EnergieStV
und betrifft **Dampfmengen** an mehreren Entnahmestellen.

### 2.2 Belastbarkeit

| Aussage | Einstufung |
|---|---|
| § 53 Abs. 1 Satz 2, Abs. 2 EnergieStG; § 3 Abs. 3 EnergieStG; § 98 EnergieStV | **Primärquelle**, gesichert |
| § 99a EnergieStV verlangt einen Nutzungsgrad-**Nachweis**, enthält **keine** Formel; §§ 99b, 99c „(weggefallen)" | **Primärquelle**, gesichert |
| Formular 1131: Feldstruktur ohne Wirkungsgrad- und Strommengenabfrage | **Amtlicher Vordruck**, aber Fassung 2019 über einen Drittspiegel — zoll.de führt aktuell 1131 und 1131_25. Die Feldstruktur 2026 kann abweichen: **markierte Lücke** |
| Dienstvorschrift Energieerzeugung: „Wärme … wird nicht betrachtet", Mikro-KWK-Formeln, Abs. 110 | **Amtliche Verwaltungsvorschrift**, Fassung 31.01.2014 (E-VSF N 09 2014 Nr. 29), gespiegelt. Eine neuere Fassung (16.12.2016) war nicht frei abrufbar: **markierte Lücke**. Die Kernaussagen decken sich wörtlich mit der Anleitung zu Formular 1131 (2019) |
| Ein Formular „1131a" existiert **nicht** | gesichert — das Konzept und die Grundlagen nannten es; die Angabe ist berichtigt |
| Finnische Methode / Stromverlustmethode / FW 308 im Energiesteuerrecht | **Negativbefund** |

**Quellen (abgerufen 19.08.2026):** gesetze-im-internet.de (`energiestg/__53`,
`__53a`, `__3`; `energiestv/__98`, `__99`, `__99a`, `__99b`, `__99c`), buzer.de
(`53_EnergieStG`, `98 EnergieStV`), zoll.de (Steuerentlastung für die Stromerzeugung,
Steuerentlastungsvoraussetzungen, vollständige Steuerentlastung für KWK-Anlagen),
Dienstvorschrift Energieerzeugung als PDF über bhkw-infozentrum.de, Formular 1131
(Fassung 2019) als PDF über energiewerkstatt.de; ergänzend BBH-Blog und Deloitte Tax News.

### 2.3 Was daraus im Code wurde

**Die Aufteilungsmethode ist eine Projektangabe** (so verlangt es der Auftrag), aber die
**Vorgabe ist das rechtlich belegte Verfahren**:

| Steuerwert | Rechenweg | Grundlage |
|---|---|---|
| **`VOLLER_BRENNSTOFF`** (Vorgabe) | gesamter BHKW-Brennstoff | § 53 Abs. 2 Satz 1 EnergieStG i.V.m. der Dienstvorschrift; keine Aufteilung |
| `ENERGETISCH` | Brennstoff × Strom / (Strom + Wärme) | **kein** Rechtsverfahren — die Auslegung, von der die Grundlagen bis zu dieser Recherche ausgingen; zeigt die Untergrenze der Gutschrift |

An Projekt 1030 gemessen ist der Unterschied **Faktor 2,27** (26.876 € gegen 11.832 €).
Wer die energetische Aufteilung wählt, verschenkt nach heutigem Erkenntnisstand mehr als
die Hälfte der Entlastung — deshalb steht sie zur Wahl, aber nicht als Vorgabe.

### 2.4 Die Hi/Ho-Falle beim Erdgas — welche Bezugsgröße die Anwendung führt

**Die Anwendung führt durchgängig Heizwerte (Hi).** Der Rechenkern bildet den
Brennstoffeinsatz aus Wirkungsgraden, die heizwertbezogen sind;
`Tab_ErgebnisBHKWModul.Verbrauch` steht in MWh **Hi**, und
`energy_carrier.hi_kwh_per_unit` ist die Umrechnung auf die Abrechnungseinheit.

**Die Energiesteuer für Erdgas wird dagegen brennwertbezogen bemessen** — die
Dienstvorschrift rechnet dafür mit dem Faktor 1,11 = Hs/Hi nach DIN V 18599-1. Wer den
Satz von 5,50 €/MWh auf eine Hi-Menge anwendet, weist rund 10 % zu wenig aus.

Umgesetzt ist deshalb: **Sätze in `EUR/MWh` werden auf die Brennwertmenge angewendet**,
umgerechnet über die **gepflegten Werte des Trägers** (`eff_hs / eff_hi`, Projektwert vor
Katalogwert) und **nicht** über einen pauschalen Faktor. Bei Erdgas E der produktiven
Datenbank ergibt das 11,6 / 10,5 = **1,1048** — nahe am Vorschriftenwert 1,11, aber aus
den Daten des Projekts. Der verwendete Faktor steht in der Herkunftszeile des
Ergebnisses. Fehlt ein Brennwert, wird heizwertbezogen gerechnet und **das Ergebnis mit
dem Hinweis versehen, dass die Entlastung rund 10 % zu niedrig liegt** — die konservative
Richtung.

Je MWh besteuert werden ausschließlich gasförmige Energieerzeugnisse (§ 2 Abs. 3 Satz 1
Nr. 4 EnergieStG); die Regel greift damit genau dort, wo sie hingehört.

---

## 3 Entwurfsentscheidungen

### 3.1 § 53 und § 53a sind eine Auswahl, keine Automatik

Beide Normen schließen einander aus (§ 53a Abs. 1 „Vorbehaltlich des § 53";
Dienstvorschrift Abs. 15), und ob sie sich anteilig kombinieren lassen — § 53 auf den
Strom-, § 53a auf den Wärmeanteil —, ist ungeklärt
(`Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`, Abschnitt 6 Punkt 1). Der Anwender wählt
deshalb die Norm, unter der er den Antrag stellt: `KEINE` (Vorgabe), `PARAGRAF_53`,
`PARAGRAF_53A`. Eine Automatik hätte an einer ungeklärten Rechtsfrage entschieden.

### 3.2 Ohne Angabe keine Gutschrift — und dieselbe Regel in der Migration

Jeder Vorgabewert ist der, der **keine** Gutschrift auslöst:

| Angabe | Vorbelegung (Schritt 20b) | Folge |
|---|---|---|
| `Unternehmensart` | `KEIN_PROD_GEWERBE` | keine § 9b-Entlastung |
| `Energiesteuer_Wahl` | `KEINE` | keine Energiesteuer-Gutschrift |
| `Raeumlicher_Zusammenhang` | `False` (Access, YESNO) | keine Befreiung |
| `Hocheffizienz_Nachweis` | `False` (Access, YESNO) | keine Befreiung |
| `Jahresnutzungsgrad` | **NULL** | § 53a scheitert mit „nicht erfasst" |
| `Aufteilung_Methode` | `VOLLER_BRENNSTOFF` | ohne Rechenwirkung, solange die Wahl `KEINE` ist |

Die **Leseseite behandelt leer und NULL genauso wie den Vorgabewert** — eine nie
migrierte Datenbank rechnet deshalb ebenfalls wie bisher. Das ist derselbe Bauplan, mit
dem Etappe E3 ihre Ergebnisneutralität getragen hat.

### 3.3 Der Jahresnutzungsgrad bleibt NULL, nicht 0

`DOUBLE` lässt Bestandszeilen auf NULL, und das ist die ehrliche Wahl. Beide Fälle führen
zu keiner Gutschrift, aber die **Begründung** unterscheidet sich, und die soll stimmen:
„kein Jahresnutzungsgrad erfasst (Schwelle 70 %)" gegen „Jahresnutzungsgrad 60,0 % unter
der Schwelle von 70 %". Im Dialog bedeutet der Wert 0 „nicht erfasst"; ein Nutzungsgrad
von null ist fachlich kein Wert.

### 3.4 Die 2-MW-Grenze wird **je Anlage** geprüft

Restbefund 3 aus dem E2-Protokoll (Nachtrag 1, Abschnitt N7) war ausdrücklich für diese
Etappe vermerkt: `GESETZ_STROMST_GRENZE_BEFREIUNG` (2.000 kW) ist eine **Anlagen**-Nenn­leistung.
Die Prüfung läuft deshalb über dieselbe Anlagenliste wie der KWKG-Guard
(`Tab_Energieanlagen` ⋈ `Tab_BHKW`, gepaart mit den Ergebnis-Modulzeilen), und die
Befreiung wird nach demselben Muster über den **Stromanteil der verbleibenden Anlagen**
bereinigt. Nachgewiesen in Probe P3: 500 kW + 2.500 kW ergeben 8.200 € statt 20.500 € —
exakt der Anteil 400/1.000 der Stromerzeugung.

Eine Anlage, auf die **mehrere** Ausschlussgründe zutreffen (zu groß **und** über dem
CO₂-Grenzwert), fehlt in den Summen genau einmal — dieselbe Bilanzregel wie in Nachtrag 2
zu E2.

### 3.5 Der CO₂-Grenzwert bezieht sich auf den **Energieertrag**, nicht auf den Brennstoff

§ 2 StromStG verlangt bei fossilen Anlagen direkte CO₂-Emissionen unter **270 g je kWh
Energieertrag**. Energieertrag ist die nutzbare Ausbeute, also Strom **plus** Wärme:

```
CO₂ je kWh Energieertrag = Faktor [g/kWh Brennstoff] × Brennstoff / (Strom + Wärme)
```

Das ist der Grund, aus dem die Grundlagen sagen „Erdgas erfüllt das in der Regel, Heizöl
eher nicht": Der reine Brennstofffaktor liegt bei Erdgas bei 200,9 und bei Heizöl EL bei
266,4 g/kWh — **beide unter 270**. Erst der Kehrwert des Nutzungsgrades hebt Heizöl
darüber. Nachgerechnet an Projekt 1030 mit Heizöl EL: 266,4 × 4.423,19 / 3.887,29 =
**303,1 g/kWh > 270** ⇒ keine Befreiung; mit Erdgas: 200,9 × 4.423,19 / 3.887,29 =
**228,6 g/kWh < 270** ⇒ Befreiung.

**Verwendet wird der EBeV-Faktor (`EF_BILANZ_EBEV_*`), nicht Anlage 9 des GModG.** Gefragt
sind die tatsächlichen direkten Emissionen, nicht ein Nachweiswert des Gebäuderechts —
genau die Trennung, für die Leitentscheidung L11 die beiden Katalogklassen führt. Die
Anlage-9-Werte (Erdgas 240, Heizöl 310) hätten hier ein anderes Ergebnis geliefert und
die Trennung aufgehoben.

### 3.6 Ohne Stundenreihen keine Stromsteuerbefreiung

`StromMatrix` teilt die BHKW-Erzeugung stundenweise in Eigenverbrauch und Einspeisung;
ohne Bedarfsreihe fällt sie auf „alles ist Eigenverbrauch" zurück und weist das über
`StrombedarfFehlt` aus (`StromMatrix.cs:49-51`, `:109`). Für den KWK-Zuschlag ist das
eine vertretbare, dokumentierte Näherung — für eine gegenüber dem Hauptzollamt geltend
gemachte **Steuerbefreiung** nicht. Fehlt die Reihe, gibt es deshalb 0 € mit der
Begründung „der KWK-Eigenverbrauch ist nicht bestimmbar". Der **Netzbezug** des § 9b ist
davon unberührt: Er ist eine gerechnete Jahressumme des Laufs
(`Energiebedarf.Stromrestbedarf`), keine Näherung, und dient als Ersatzweg, wenn keine
Matrix vorliegt.

**Belastbarkeit der beiden Größen** — wie vom Auftrag verlangt, ausdrücklich benannt:

| Größe | Quelle | Bewertung |
|---|---|---|
| `NetzbezugMWh` | `StromMatrix.BezugGesamtMWh`, sonst `Energiebedarf.Stromrestbedarf` | **belastbar** — Jahressumme der Bezugsreihe des Laufs; beide Wege stimmen auf 0,01 MWh überein (3.070,0086 gegen 3.070,01) |
| `KwkEigenMWh` | `StromMatrix.KwkEigenGesamtMWh` | **Näherung** — die Simulation führt den BHKW-Strom nicht getrennt nach Eigennutzung und Einspeisung; die stundenweise `min(Erzeugung, Restbedarf)`-Regel bildet die Gleichzeitigkeit ab (`StromMatrix.cs:20-23`, `:107-120`). Ohne Bedarfsreihe **nicht** verwendet |

### 3.7 Die Zuordnung Brennstoff → Steuersatz ist bewusst unvollständig

Zugeordnet wird nur, was die Grundlagen namentlich führen: Erdgas (LL, E), Flüssiggas
(Propan, Butan), Schweröl (Heizöl S, M, L), Gasöl (Heizöl EL, EL schwefelarm und die vier
Bio-Blends). **Ohne Zuordnung bleiben** Stadtgas, Wasserstoff, Kohle und Koks nach § 2,
Biogas, Holz, Pellets, Rapsöl, tierische Fette und Fernwärme — sie liefern 0 € plus die
Begründung „dem Energieträger ist kein Steuersatz zugeordnet". Eine geratene Einordnung
wäre genau der Fehlertyp, den L3 verhindern soll.

**Heizöl L und M zählen als Schweröl** (§ 2 Abs. 3 Satz 1 Nr. 2, je 1.000 kg); nur
Heizöl EL ist Gasöl im Sinne der Nr. 1 Buchst. a (je 1.000 l). Die Bio-Blends folgen dem
Heizöl EL — dieselbe Näherung, mit der die BEHG-Einstufung schon arbeitet.

### 3.8 Einheitendisziplin: lieber keine Gutschrift als eine geratene Dichte

Die Sätze stehen in vier verschiedenen gesetzlichen Einheiten. Umgerechnet wird
ausschließlich über die gepflegten Heizwerte:

| Gesetzliche Einheit | Voraussetzung | Rechnung |
|---|---|---|
| `EUR/MWh` | — | Brennwertmenge = MWh<sub>Hi</sub> × eff_hs / eff_hi (siehe 2.4) |
| `EUR/1000l` | Abrechnungseinheit `L`, eff_hi > 0 | (MWh × 1000 / eff_hi) / 1000 |
| `EUR/1000kg` | Abrechnungseinheit `kg`, eff_hi > 0 | (MWh × 1000 / eff_hi) / 1000 |
| `EUR/GJ` | — | MWh × 3,6 |

**`energy_carrier.density` ist im gesamten Bestand leer** (21 von 21 Trägern). Ein je
Liter abgerechnetes Schweröl lässt sich deshalb **nicht** in Kilogramm umrechnen; die
Gutschrift entfällt mit einer Begründung, die die Lücke benennt (Probe P5, Fall F7). Das
ist genau der Öl-Fehler der Altanwendung — nur diesmal als sichtbare Lücke statt als
falscher Wert um den Faktor 10.

### 3.9 Benannte Reihen statt einer Reihe (L1)

`KapitalwertRechner.Rechne` nimmt jetzt `IList<ErloesReihe>` statt `double[]`. Die
Rechnung selbst ändert sich nicht: Abgezinst wird die **Summe** der Reihen je Jahr, und
eine Liste mit genau der KWKG-Reihe liefert Wert für Wert dasselbe wie vorher (belegt
durch die A/B-Probe, Abschnitt 5.3). Die Namen sind sprachneutrale Schlüssel
(`KWKG_ZUSCHLAG`, `ENERGIESTEUER_GUTSCHRIFT`, `STROMSTEUER_BEFREIUNG`,
`STROMSTEUER_ENTLASTUNG`) mit Anzeigetexten in `MyResource.Resource.WIRT_REIHE_*` — die
Etappe E7 braucht sie, um die Gutschriften einzeln auszuweisen.

**Der Aufruferkreis war klein**, wie L1 vorhergesagt hat: genau **eine** Fundstelle
(`WirtschaftlichkeitCtrl.RechneBild`). Das Novellen-Szenario der Sensitivität streicht
weiterhin **nur** die KWKG-Reihe (`OhneKwkg`) — die Steuergutschriften hängen an anderen
Gesetzen und bleiben stehen.

### 3.10 Jahresscharf, obwohl die Reihen heute flach sind

Für jedes Betrachtungsjahr wird mit dem Satz **dieses** Jahres gerechnet
(`Förderbeginn + t − 1`, dieselbe Regel wie beim KWKG-Jahresdeckel). Auf dem heutigen
Rechtsstand sind alle Sätze ab 2026 konstant, die Reihen also flach. Die Mechanik trägt
aber jede künftige Novelle, **ohne dass eine Altrechnung ihre Zahlen ändert** — eine
Novelle ist eine neue Jahreszeile im Katalog, kein Ändern der alten.

Die **Begründungen** entstehen ausschließlich aus dem ersten Jahr; sonst stünde derselbe
Satz zwanzigmal im Hinweisfeld.

### 3.11 Ergebnisspalten über `SpalteSicher`, Parameterspalten über die Migration

Die vier **Ergebnis**spalten gehen den Weg, den dieses Modul seit W1 für
`Tab_ErgebnisWirtschaftlichkeit` geht (zuletzt `KWKGVbhElektrisch` in E2): additiv über
`SpalteSicher`. Ein Migrationsschritt dafür wäre der dritte Mechanismus für **eine**
Tabelle.

Die sechs **Parameter**spalten gehen dagegen über **Migrationsschritt 20** — so verlangt
es der Auftrag, und es ist auch fachlich richtig: Das sind Eingabedaten des Anwenders mit
einer inhaltlichen Vorbelegung, keine wiederherstellbaren Rechenergebnisse. Zusätzlich
legt `WirtschaftlichkeitCtrl.StelleTabellenSicher` dieselben Spalten vorsorglich an
(Muster `KostenPositionCtrl.StelleSpaltenSicher` aus E3) — die **Werte**vorbelegung bleibt
allein bei Schritt 20b, ein zweiter schreibender Weg auf Anwenderdaten wäre eine Wahrheit
zu viel.

### 3.12 Was „Kennzahlen" hier heißt

Der Auftrag verlangt die neuen Zeilen „im Wirtschaftlichkeitsreiter und in den
Kennzahlen". Gemeint ist die **Kennzahlentabelle der Wirtschaftlichkeit** — der Reiter
führt sie mit der Spaltenüberschrift „Kennzahl", Word und Excel zeigen dieselbe Tabelle.
Alle drei sind ergänzt.

**Nicht** ergänzt ist `KennzahlenKatalog.cs`: Der Katalog rechnet aus `VariantenDaten`
(Simulationsergebnis plus `KostenEmissionRechner`) und läuft **vor** und unabhängig von
der Wirtschaftlichkeitsrechnung. Steuergutschriften stehen dort nicht zur Verfügung; sie
dort einzuhängen hieße, den Lebenszyklus zweier Module zu vermischen. Festgehalten als
offener Punkt für E7.

### 3.13 Die neuen Zeilentitel laufen über `MyResource` — anders als ihre Nachbarn

Der Wirtschaftlichkeitsnachweis führt seine vierzehn Zeilentitel als deutsche Literale;
Etappe E2 hatte ihre eine neue Zeile deshalb bewusst genauso angelegt (E2-Protokoll,
Abschnitt 3.5). Der Auftrag zu E4 verlangt ausdrücklich `MyResource`, und danach ist
umgesetzt: Die vier neuen Zeilen stehen in beiden Sprachen im Katalog, in Reiter, Word
und Excel **gleichlautend**.

**Das ist eine bewusste Abweichung von der E2-Konvention und macht den Bereich vorerst
uneinheitlicher**, nicht einheitlicher: vier lokalisierte Zeilen zwischen fünfzehn
deutschen. Der saubere Abschluss wäre, den ganzen Block umzustellen — als eigener Vorgang
(offener Punkt 6 in Abschnitt 7).

---

## 4 Bedingungsmatrix

**Energiesteuer** (nur BHKW-Brennstoff, nie Kessel — die Anlagenliste enthält
ausschließlich BHKW-Anlagen, und der Rechenkern führt den Kesselbrennstoff getrennt):

| Bedingung | Prüfung | Bei Verstoß |
|---|---|---|
| Entlastungsnorm gewählt | `Energiesteuer_Wahl ≠ KEINE` | 0 € · „keine Entlastung gewählt — § 53 oder § 53a im Parameterdialog festlegen" |
| Jahresnutzungsgrad ≥ 70 % (**nur § 53a**) | Projektangabe gegen `ENERGIEST_53A_MINDESTNUTZUNGSGRAD` | 0 € · „Jahresnutzungsgrad {0} % unter der Schwelle von {1} %" bzw. „kein Jahresnutzungsgrad erfasst" |
| Energieträger einem Satz zugeordnet | Abschnitt 3.7 | 0 € · „dem Energieträger ist kein Steuersatz zugeordnet" |
| Satz im Katalog gepflegt | `GesetzKatalog.WertMitHerkunft` | 0 € · „der Katalogsatz {1} ist nicht gepflegt (Administration → Gesetzliche Parameter)" |
| Menge in die gesetzliche Einheit umrechenbar | Abschnitt 3.8 | 0 € · „lässt sich nicht in die gesetzliche Einheit {1} umrechnen … für Kilogramm fehlt die Dichte" |
| Brennwert gepflegt (nur `EUR/MWh`) | `eff_hs > 0` | rechnet heizwertbezogen weiter · Hinweis „die Entlastung fällt rund 10 % zu niedrig aus" |

**Stromsteuerbefreiung § 9 Abs. 1 Nr. 3:**

| Bedingung | Ebene | Bei Verstoß |
|---|---|---|
| Hocheffizienz nachgewiesen | Projekt | 0 € · „Hocheffizienz nicht nachgewiesen — Angabe im Parameterdialog" |
| Räumlicher Zusammenhang (4,5 km, § 12b StromStV) | Projekt | 0 € · „räumlicher Zusammenhang (bis {0} km) nicht bestätigt" |
| Eigenverbrauch bestimmbar | Lauf | 0 € · „der KWK-Eigenverbrauch ist nicht bestimmbar — im Lauf fehlen die Stundenreihen" |
| Elektrische Nennleistung ≤ 2.000 kW | **je Anlage** | anteilig 0 € · „über der elektrischen Nennleistung von {0} kW je Anlage und deshalb ohne Befreiung: {1}. Die übrigen Anlagen mit zusammen {2} kW rechnen weiter." |
| < 270 g CO₂ je kWh Energieertrag (**nur fossil**) | **je Anlage** | anteilig 0 € · „über dem CO₂-Grenzwert von {0} g je kWh Energieertrag …" |
| CO₂-Faktor bestimmbar | **je Anlage** | anteilig 0 € · „die direkten CO₂-Emissionen je kWh Energieertrag sind nicht bestimmbar ({0})" |
| Regelsatz im Katalog gepflegt | Katalog | 0 € · „der Katalogsatz {0} ist nicht gepflegt" |

**Stromsteuerentlastung § 9b:**

| Bedingung | Bei Verstoß |
|---|---|
| Produzierendes Gewerbe **oder** Land- und Forstwirtschaft | 0 € · „weder produzierendes Gewerbe noch Land- und Forstwirtschaft — Unternehmensart im Parameterdialog erfassen" |
| Entlastungssatz im Katalog gepflegt | 0 € · „der Katalogsatz {0} ist nicht gepflegt" |
| Entlastung über dem Sockelbetrag | 0 € · „{0} € Entlastung erreichen den Sockelbetrag von {1} € je Kalenderjahr nicht" |

---

## 5 Wirkung — mit Zahlen

**Gemeinsame Grundlage:** Wegwerf-Kopien der produktiven `Kenndaten.accdb` vom
19.08.2026 (Zeitstempel 02:51:17, 96.436.224 Byte, MD5
`66F4806A3B89074B52344F39D477F151`, Schemastand 17), mit dem E4-Stand auf Schemastand 20
migriert. Alle Proben auf Kopien, die Produktivdatei ausschließlich gelesen.

### 5.1 Bestandsprojekte — Δ = 0,00 € in jeder Zeile

| Projekt | Energiesteuer | Stromst.-Befreiung | Stromst.-Entlastung | Kapitalwert A | Kapitalwert B |
|---|---|---|---|---|---|
| 1007, 1008, 1011, 1021 | 0,00 € | 0,00 € | 0,00 € | *(kein Wert)* | *(kein Wert)* |
| 1017 | 0,00 € | 0,00 € | 0,00 € | *(kein Wert)* | *(kein Wert)* |
| 1018 | 0,00 € | 0,00 € | 0,00 € | *(kein Wert)* | *(kein Wert)* |
| 1023 | 0,00 € | 0,00 € | 0,00 € | **−613.034,9384 €** | **−613.034,9384 €** |
| 1024 | 0,00 € | 0,00 € | 0,00 € | *(kein Wert)* | *(kein Wert)* |
| 1030 | 0,00 € | 0,00 € | 0,00 € | **−21.443.873,4315 €** | **−21.443.873,4315 €** |

54 Ergebniszeilen (9 Projekte × 3 Szenarien), **alle Kennzahlen wertgleich**. Die
Projekte ohne Kapitalwert melden in **beiden** Ständen „Energiekosten nicht bestimmbar" —
eine Datenlücke der Projekte, keine Wirkung dieser Etappe.

**Die vier BHKW-Projekte zeigen ab jetzt die Begründung**, statt schweigend 0 zu liefern:

> 1030: „Energiesteuer: keine Entlastung gewählt — § 53 oder § 53a EnergieStG im
> Parameterdialog festlegen; Gutschrift = 0. | Stromsteuer § 9 Abs. 1 Nr. 3 StromStG:
> Hocheffizienz nicht nachgewiesen — Angabe im Parameterdialog; Befreiung = 0. |
> Stromsteuer § 9b StromStG: weder produzierendes Gewerbe noch Land- und Forstwirtschaft
> — Unternehmensart im Parameterdialog erfassen; Entlastung = 0."

Projekt 1018 bekommt die § 9b-Meldung nicht: Sein Netzbezug ist **negativ**
(−14,96 MWh, das BHKW überproduziert), und ohne Bezug gibt es nichts zu entlasten und
nichts zu melden.

### 5.2 Präparierte Kopien von Projekt 1030 — die Wirkung

Projekt 1030 „Referenz BHKW-Kaskade": zwei Erdgas-Module, **50 kW** (Wärme 605,52 MWh,
Strom 373,78 MWh, Brennstoff 1.235,85 MWh) und **250 kW** (1.561,69 / 1.346,29 /
3.187,34). Summen: Strom 1.720,08 MWh, Brennstoff 4.423,19 MWh (Hi), Netzbezug
3.070,0086 MWh, Inbetriebnahme 01.03.2027 ⇒ Stichtagsjahr **2027**. Träger Erdgas E
(eff_hi 10,5 · eff_hs 11,6 kWh/m³).

Nur die Steuerangaben (und in F6/F7/F10 zusätzlich Leistung beziehungsweise Träger) sind
präpariert; alles Übrige ist der Bestand.

| Fall | Angaben | Energiesteuer | Befreiung | Entlastung | Kapitalwert |
|---|---|---|---|---|---|
| **Bestand** | Vorgabewerte | 0,00 € | 0,00 € | 0,00 € | −21.443.873,43 € |
| **F1** | § 53, voller Brennstoff, prod. Gewerbe, räumlich ✓, hocheffizient ✓, η 85 % | **26.876,1450 €** | **28.564,6172 €** | **61.150,1723 €** | −19.709.294,73 € |
| **F2** | wie F1, aber § 53a Abs. 5 | **21.598,6474 €** | 28.564,6172 € | 61.150,1723 € | −19.787.810,57 € |
| **F3** | wie F2, η **60 %** | **0,00 €** + Grund | 28.564,6172 € | 61.150,1723 € | −20.109.143,91 € |
| **F4** | wie F1, Aufteilung **energetisch** | **11.832,3105 €** | 28.564,6172 € | 61.150,1723 € | −19.933.109,00 € |
| **F5** | wie F1, **kein räumlicher Zusammenhang** | 26.876,1450 € | **0,00 €** + Grund | 61.150,1723 € | −20.134.264,11 € |
| **F8** | wie F2, **η nicht erfasst** | **0,00 €** + Grund | 28.564,6172 € | 61.150,1723 € | −20.109.143,91 € |
| **F9** | wie F1, **kein produzierendes Gewerbe** | 26.876,1450 € | 28.564,6172 € | **0,00 €** + Grund | −20.619.054,89 € |
| **F10** | wie F1, Träger **Heizöl EL** (Carrier 56) | **27.136,2706 €** | **0,00 €** (CO₂) | 61.150,1723 € | *(kein Wert)* ¹ |
| **F7** | wie F1, Träger **Heizöl L** (Carrier 62) | **0,00 €** (Einheit) | **0,00 €** (CO₂) | 61.150,1723 € | *(kein Wert)* ¹ |
| **F6** | wie F1, 250-kW-Modul auf **2.500 kW** | 112.484,2002 € ² | **0,00 €** (> 2 MW) | 0,00 € ² | +10.378.482,29 € ² |

¹ Für Heizöl ist im Projekt kein Arbeitspreis gepflegt ⇒ „Energiekosten nicht
bestimmbar"; die Steuergutschriften werden trotzdem gerechnet und ausgewiesen.
² F6 ändert die **Leistung** und damit die Simulation selbst; die Beträge sind deshalb
nicht mit F1 vergleichbar. Der Fall belegt ausschließlich das **Greifen der 2-MW-Grenze**
— sauber isoliert ist er in Probe P2/P3.

**Von Hand nachgerechnet:**

| Größe | Rechnung | Ergebnis | Harnisch |
|---|---|---|---|
| F1 Energiesteuer | 4.423,19 MWh<sub>Hi</sub> × 11,6/10,5 = 4.886,5718 MWh<sub>Ho</sub> × 5,50 €/MWh | 26.876,145 € | **26.876,1450 €** |
| F2 Energiesteuer | 4.886,5718 × 4,42 €/MWh | 21.598,647 € | **21.598,6474 €** |
| F4 Energiesteuer | (1.235,85 × 373,78/979,30 + 3.187,34 × 1.346,29/2.907,98) = 1.947,3 MWh<sub>Hi</sub> × 11,6/10,5 × 5,50 | ≈ 11.832,3 € | **11.832,3105 €** |
| F1 Befreiung | 20,50 €/MWh × 1.393,3959 MWh Eigenverbrauch | 28.564,617 € | **28.564,6172 €** |
| F1 Entlastung | 20,00 €/MWh × 3.070,0086 MWh − 250 €/a | 61.150,172 € | **61.150,1723 €** |
| F10 Energiesteuer | 4.423,19 MWh / 10 kWh je Liter = 442.319 l = 442,319 × 1.000 l × 61,35 €/1.000 l | 27.136,27 € | **27.136,2706 €** |
| F10/F7 CO₂-Prüfung | 266,4 g/kWh × 4.423,19 / (1.720,08 + 2.167,21) | 303,1 g/kWh > 270 | Befreiung 0 |
| F1 CO₂-Prüfung | 200,9 g/kWh × 4.423,19 / 3.887,29 | 228,6 g/kWh < 270 | Befreiung gewährt |

Die Herkunftszeile des Falls F1 (so steht sie im Ergebnis und im Reiter):

> „Energiesteuer: Erdgasmenge brennwertbezogen bemessen — Ho/Hi = 1,1048 aus den
> gepflegten Werten des Energieträgers. | ENERGIEST_ERDGAS = 5,50 EUR/MWh, gültig ab 2003
> (GESICHERT) — EnergieStG § 2 Abs. 3 Satz 1 Nr. 4 — Erdgas | STROMST_REGELSATZ = 20,50
> EUR/MWh, gültig ab 2026 (GESICHERT) — § 3 StromStG, Fassung vom 22.12.2025 (BGBl. 2025
> I Nr. 340) | STROMST_ENTLASTUNG_9B = 20,00 EUR/MWh, gültig ab 2026 (GESICHERT) — § 9b
> StromStG — Entlastung für das produzierende Gewerbe (Formular 1453) |
> STROMST_SOCKELBETRAG_9B = 250,00 EUR/a, gültig ab 2026 (GESICHERT) — § 9b StromStG —
> Sockelbetrag je Kalenderjahr (entspricht 12,5 MWh/a)"

Der Fall F7 zeigt drei Guards zugleich:

> „KWKG: Jede BHKW-Anlage des Projekts wird mit Heizöl betrieben (…) — als Neuanlage
> nicht mehr förderfähig …; Bonus = 0. | Energiesteuer: BHKW EW M 50 S [K] Erdgas
> (50 kW) — die Brennstoffmenge lässt sich nicht in die gesetzliche Einheit EUR/1000kg
> umrechnen (Abrechnungseinheit L, Heizwert 11,20 kWh je Einheit; für Kilogramm fehlt die
> Dichte); Gutschrift = 0. | … | Stromsteuer § 9 Abs. 1 Nr. 3 StromStG: über dem
> CO₂-Grenzwert von 270 g je kWh Energieertrag und deshalb ohne Befreiung: … "

### 5.3 Reine Funktionsprobe — die Guards isoliert

`SteuerGutschriftRechner.Rechne` ohne Datenbank, mit von Hand gesetzten Mengen
(eine Erdgasanlage 500 kW: 1.000 MWh Brennstoff, 400 MWh Strom, 500 MWh Wärme;
Eigenverbrauch 1.000 MWh, Netzbezug 500 MWh; Sätze wie im Katalog):

| Probe | Fall | Energiesteuer | Befreiung | Entlastung | von Hand |
|---|---|---|---|---|---|
| **P1** | alles erfüllt, § 53 | 6.076,1905 € | 20.500,0000 € | 9.750,0000 € | 1.000 × 11,6/10,5 × 5,50 = 6.076,1905 · 20,50 × 1.000 · 20 × 500 − 250 ✓ |
| **P2** | Anlage **2.500 kW** | 6.076,1905 € | **0,0000 €** | 9.750,0000 € | Grenze 2.000 kW je Anlage ✓ |
| **P3** | 500 kW **+** 2.500 kW | 12.152,3810 € | **8.200,0000 €** | 9.750,0000 € | 20.500 × 400/1.000 = 8.200 ✓ |
| **P4** | Heizöl EL, § 53 | **6.135,0000 €** | **0,0000 €** (CO₂ 296 > 270) | 9.750,0000 € | 1.000/10 = 100.000 l → 100 × 61,35 ✓ |
| **P5** | Heizöl S (€/1.000 kg gegen Liter) | **0,0000 €** + Grund | 0,0000 € | 9.750,0000 € | keine Dichte ✓ |
| **P6** | § 9b, Netzbezug **10 MWh** | 6.076,1905 € | 20.500,0000 € | **0,0000 €** + Grund | 20 × 10 = 200 < 250 ✓ |
| **P7** | § 9b, Netzbezug **13 MWh** | 6.076,1905 € | 20.500,0000 € | **10,0000 €** | 20 × 13 − 250 = 10 ✓ |
| **P8** | Eigenverbrauch nicht bestimmbar | 6.076,1905 € | **0,0000 €** + Grund | 9.750,0000 € | keine Stundenreihen ✓ |
| **P9** | **Vorgabewerte** (Bestandsprojekt) | **0,0000 €** | **0,0000 €** | **0,0000 €** | drei Begründungen ✓ |
| **P10** | § 53a Abs. 5, η 85 % | **4.883,0476 €** | 20.500,0000 € | 9.750,0000 € | 1.104,7619 × 4,42 ✓ |
| **P11** | § 53, energetisch | **2.700,5291 €** | 20.500,0000 € | 9.750,0000 € | 1.000 × 400/900 × 11,6/10,5 × 5,50 ✓ |

**11 von 11 Proben stimmen auf die vierte Nachkommastelle mit der Handrechnung überein.**

---

## 6 Verifikation

### 6.1 Referenzlauf

Beide Stände aus eigenen Exporten außerhalb des Repos gebaut (`git archive 20958be` für
A, Arbeitsbaum für B; Unterschied nachgewiesen: **exakt 14 Dateien**), jeweils mit dem
mitgelieferten `Referenzlauf.csproj` (ProjectReference auf die App ⇒ Exe und DLL
konsistent). **Eine gemeinsame Wegwerf-Kopie** der produktiven Datenbank, mit dem
B-Stand auf Schemastand 20 migriert, danach von beiden Ständen gelesen. Neun Projekte,
feste Liste `--projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030`, Feature-Flag
`Kaskade_Zweikanalig` **AUS**.

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

**Byte-Vergleich: 216 von 216 CSV identisch**, gegen die eingefrorene Basis
`2026-08-19_B5` **und** gegen den A-Stand. Nicht ein einziger neuer Ergebnisschlüssel —
E4 fasst den Rechenkern nicht an.

### 6.2 Verifikationstabelle

| # | Prüfung | Methode | Ergebnis |
|---|---|---|---|
| V1 | Engine unberührt | `git diff --name-only 20958be -- Allgemein/Simulation/` | **leer** |
| V2 | Simulationsergebnisse gegen die Basis B5 | `Referenzlauf.exe vergleich`, gemeinsame DB-Kopie, Flag AUS | **9/9 PASS**, 2 366 177 Werte |
| V3 | dito A gegen B | dito | **9/9 PASS** |
| V4 | Byte-Identität gegen B5 | `cmp` je Datei | **216/216 gleich**, 0 Abweichungen |
| V5 | Byte-Identität A gegen B | `cmp` je Datei | **216/216 gleich** |
| V6 | Wirtschaftlichkeitswerte unverändert | Harnisch auf `BerichtsDatenSammler.Sammle` + `WirtschaftlichkeitCtrl.Berechne`, A gegen B, gemeinsame Kopie, 9 Projekte × 3 Szenarien | **54/54 Zeilen wertgleich** in Investition, Betriebskosten, Energiekosten, CO₂-Abgabe, KWKG, Vbh, Kapitalwert und Gestehungskosten |
| V7 | Die drei Steuergutschriften sind auf dem Bestand null | dieselbe Probe | **Summe über alle 54 Zeilen: 0,0000 € / 0,0000 € / 0,0000 €** |
| V8 | Begründung statt stiller Null | dieselbe Probe, Hinweisspalte | alle vier BHKW-Projekte führen die Begründung; die fünf übrigen schweigen (kein BHKW ⇒ nichts zu melden) |
| V9 | Migration 19 → 20 | `Referenzlauf.exe migration` auf Wegwerf-Kopie (Quelle stand auf 17) | **Schemastand 20**, Schritt 20 „OK" |
| V10 | Spalten korrekt angelegt | `GetOleDbSchemaTable` | `Tab_ProjektWirtschaftlichkeit` 19 → **25** Spalten, alle sechs **angehängt** (Positionen 20–25), Typen TEXT(24)/YESNO/YESNO/DOUBLE/TEXT(20)/TEXT(30) |
| V11 | Bestandswerte unversehrt | Vorher/Nachher-Dump derselben Datei | beide Parametersätze (1019, 1030) wertgleich in Zins, Zeitraum, CO₂-Preis, KWKG-Sätzen und Kontingent |
| V12 | Vorbelegung wie vorgesehen | `SELECT` nach der Migration | `KEIN_PROD_GEWERBE` / `False` / `False` / **NULL** / `KEINE` / `VOLLER_BRENNSTOFF` in beiden Zeilen |
| V13 | Doppelstart idempotent | zweiter Migrationslauf mit `--nokopie` | „Schritt 20 …: bereits erledigt", **0 Angaben vorbelegt**, Stand bleibt 20, weiterhin 25 Spalten |
| V14 | Wirkung § 53 / § 53a / energetisch | präparierte Kopien F1, F2, F4 | 26.876,1450 / 21.598,6474 / 11.832,3105 €, jeweils = Handrechnung |
| V15 | Wirkung Stromsteuerbefreiung | F1 | 28.564,6172 € = 20,50 × 1.393,3959 MWh |
| V16 | Wirkung § 9b mit Sockelabzug | F1 | 61.150,1723 € = 20,00 × 3.070,0086 − 250 |
| V17 | § 9b **ohne** Wirkung wegen Sockel | Probe P6 (10 MWh) und P7 (13 MWh) | **0,00 €** mit Grund bzw. **10,00 €** |
| V18 | Verletzte Bedingung: über 2 MW | Proben P2/P3, Fall F6 | Befreiung 0 bzw. anteilig 8.200 €, Meldung nennt die Anlage |
| V19 | Verletzte Bedingung: kein räumlicher Zusammenhang | F5 | Befreiung **0,00 €**, Energiesteuer und § 9b unberührt |
| V20 | Verletzte Bedingung: Nutzungsgrad unter 70 % | F3 (60 %) und F8 (nicht erfasst) | beide **0,00 €**, **zwei verschiedene** Begründungen |
| V21 | Verletzte Bedingung: Heizöl über dem CO₂-Grenzwert | F10 (Heizöl EL) und Probe P4 | Befreiung **0,00 €**; Energiesteuer bleibt (27.136,2706 € bzw. 6.135,00 €) |
| V22 | Einheitenfalle: €/1.000 kg gegen Liter | F7 (Heizöl L) und Probe P5 | Gutschrift **0,00 €**, Begründung nennt Einheit, Heizwert und die fehlende Dichte |
| V23 | Herkunft je Satz | F1, Reiterzeile „Herkunft der Steuersätze" | fünf Zeilen mit Schlüssel, Wert, Einheit, Gültigkeitsjahr, Status und Fundstelle |
| V24 | Ressourcen in beiden `.resx` und im Designer | `grep` je Schlüssel | **27/27** in `Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` |
| V25 | Build | `MSBuild WindowsFormsApplication1.csproj -t:Rebuild -p:Platform=x86`, Ausgabe in den Scratch-Ordner | **0 Fehler, exakt 6 Warnungen** (CS0108 ×2, CS0109 ×2, CS1998, CS4014) |
| V26 | Kodierung und Zeilenenden | `file`, CR-Zählung, Suche nach U+FFFD je Datei | unverändert (8 × UTF-8 mit BOM, 6 × ohne), jede Datei behält ihre Zeilenenden, **0 Ersatzzeichen** |
| V27 | Produktivdatenbank nur gelesen | `Kenndaten.laccdb` vor jedem Zugriff geprüft (nie vorhanden); alle Proben auf Wegwerf-Kopien; MD5 vor und nach allen Läufen | **unverändert** (`66F4806A3B89074B52344F39D477F151`) |
| V28 | `bin\` des Repos unberührt | jeder Build ausschließlich mit `-p:OutDir=<Scratch>` | **erfüllt** |

---

## 7 Offene Punkte

### Für Etappe E5

1. **§ 9b greift nur bei Projekten mit BHKW.** `BaueSteuerEingabe` liefert ohne
   BHKW-Modulzeilen `null`, und damit entfällt auch die Entlastung auf den Netzbezug —
   obwohl § 9b StromStG an keiner KWK-Anlage hängt. Für die Ausbaustufe W4 ist das
   folgerichtig (die Etappe heißt „BHKW-Kosten und -Erlöse") und hält E4 ergebnisneutral;
   fachlich richtig wäre die Entlastung für **jedes** Projekt eines produzierenden
   Gewerbes. Gehört zur Erlösseite, also zu E5.
2. **§ 54 EnergieStG ist gepflegt, aber ungelesen.** Die vier Katalogschlüssel
   `ENERGIEST_54_*` (Heizstoffe im produzierenden Gewerbe, Sockelbetrag 250 €/a) liegen
   seit E1 bereit; sie betreffen den **Kessel**brennstoff und damit den Kesselteil der
   Erlösrechnung. Zu beachten: § 53a Abs. 5 und § 54 schließen einander aus (Grundlagen,
   Abschnitt 4).
3. **Der Eigenverbrauchsanteil je Anlage ist eine Näherung.** Die Befreiung wird über den
   Stromanteil der zulässigen Anlagen bereinigt; modulscharfe Stundenreihen gibt es
   nicht. Dieselbe Näherung wie beim KWKG-Guard (E2-Protokoll, N3 Grenze 3).

### Für Etappe E6

4. **Ein Datumspaar je Projekt.** `KwkgInbetriebnahme` bestimmt jetzt auch das
   Stichtagsjahr der Steuersätze. Bei gemischten Inbetriebnahmen gilt für alle Anlagen
   dasselbe Jahr — derselbe Restbefund, der im E2-Protokoll (N2-7) als „der neue
   gravierendste" geführt wird.
5. **Die Aufteilung der Befreiung ist projektweit.** Wie beim KWK-Zuschlag wird über den
   Stromanteil bereinigt statt je Anlage gerechnet. Solange alle Anlagen denselben Träger
   fahren, ist das identisch.

### Für Etappe E7

6. **Der Wirtschaftlichkeitsnachweis mischt jetzt Sprachen.** Vier lokalisierte Zeilen
   stehen zwischen fünfzehn deutschen Literalen (Abschnitt 3.13). Der Block gehört als
   Ganzes über `MyResource` gezogen — ein eigener, klar abgrenzbarer Vorgang.
7. **`KennzahlenKatalog` führt die Gutschriften nicht** (Abschnitt 3.12). Wer sie dort
   braucht, muss den Katalog an die Wirtschaftlichkeitsergebnisse anschließen — eine
   Lebenszyklusfrage, keine Textfrage.
8. **Die benannten Reihen sind noch nirgends einzeln sichtbar.** `ErloesReihe.Name`
   existiert samt Anzeigetexten (`WIRT_REIHE_*`), wird aber von keiner Ausgabe gelesen;
   die Mehrjahrestabelle der Etappe E7 ist der vorgesehene Ort.

### Rechtlich zu klären

9. **Formular 1131a existiert nicht.** Konzept (Abschnitt 4.2 / 5) und Grundlagen
   (Abschnitte 3.2 und 4) nennen eine „Betriebserklärung 1131a/1131az". Auf zoll.de sind
   1131 und 1131_25 gelistet, für § 53a das Formular 1135. Die Grundlagen sind berichtigt;
   das Konzept trägt die Angabe noch.
10. **Die Dienstvorschrift Energieerzeugung lag nur in der Fassung von 2014 vor.** Eine
    neuere (16.12.2016) ist laut Sekundärquelle in Kraft, war aber nicht frei abrufbar.
    Die zitierten Kernaussagen decken sich wörtlich mit der Anleitung zu Formular 1131
    (2019); vor produktivem Einsatz mit dem Hauptzollamt gegenlesen.
11. **§ 53 neben § 53a bleibt ungeklärt** (Grundlagen, Abschnitt 6 Punkt 1) — als
    Auswahl modelliert, nicht als Kombination.

### Betrieb

12. **Ein neuer Basis-Freeze steht weiterhin aus** (E8). `2026-08-19_B5` bleibt gültig:
    216 von 216 CSV sind byte-identisch, E4 hat den Rechenkern nicht angefasst.
13. **Die Commits `aac55ca` und `71cb94e` stammen aus dem Synchronisationsskript**, nicht
    aus der Umsetzung (siehe Kopfhinweis) — einschließlich der dort mitgenommenen Löschung
    von `api_gemini.txt`. Wer den Stand prüfen will, vergleicht gegen `20958be`.

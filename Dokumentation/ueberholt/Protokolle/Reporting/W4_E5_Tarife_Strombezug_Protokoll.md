# W4 · Etappe E5 — Tarife, vermiedener Strombezug und Aufschläge

**Stand: 19.08.2026.** Umsetzung der Etappe **E5** aus
[`Konzept_BHKW_Kosten_Erloese.md`](Konzept_BHKW_Kosten_Erloese.md), Abschnitt 4.3 und
Leitentscheidungen L7/L10. Ausgangsstand `4537bae` (= E4). Faktenbasis:
[`Analyse_Altanwendung_BHKW-Plan.md`](Analyse_Altanwendung_BHKW-Plan.md),
Abschnitte 2.3, 7.1 und 8, sowie die offenen Punkte aus
[`W4_E4_Steuergutschriften_Protokoll.md`](W4_E4_Steuergutschriften_Protokoll.md),
Abschnitt 7.

> **Der wichtigste Befund dieser Etappe steht in Abschnitt 4 und muss vor der Abnahme
> entschieden werden.** Die gepflegten Aufschläge auf den Strombezug erreichen die
> Wirtschaftlichkeit heute nicht. Würden sie es, stiegen die Energiekosten um **32 bis
> 34 %** und der Kapitalwert verschlechterte sich um **30 bis 33 %** — an Projekt 1030
> um **6,39 Mio. €**. Die Etappe setzt sie deshalb **hinter einen ausdrücklichen
> Schalter je Projekt, Vorgabe AUS**, und ändert an keiner Bestandsrechnung etwas.

**Ergebnis in fünf Sätzen.** `Tab_ProjektTarif` führt ab jetzt neben dem Zonenmodell der
Stufe W3 ein **Rollenmodell** mit den drei Tarifen der Altanwendung — Bezug (ohne BHKW),
Reststrom (mit BHKW) und Einspeisung — und für die beiden Bezugsrollen **alle drei
Leistungspreismodelle** (Migrationsschritt 21, 36 neue Spalten). Die Rechenkette bildet
die belegte **Differenzmethode** ab und weist den regelmäßig **negativen
Leistungsanteil** der vermiedenen Kosten als eigene Zeile aus. Die vier Fallen des
Altkatalogs sind strukturell vermieden. Zwei Bestandsmängel sind behoben: eingespeister
BHKW-Strom bekommt einen eigenen Preis (bisher gar keinen ohne Photovoltaik im Projekt),
und die § 9b-Entlastung gilt jetzt für jedes Projekt eines produzierenden Gewerbes statt
nur für Projekte mit BHKW. Für Bestandsprojekte ändert sich **nichts**: 9/9 PASS,
216/216 CSV byte-identisch, alle 27 Wirtschaftlichkeitszeilen A gegen B wertgleich.

> **Hinweis zum Arbeitsstand.** Das Synchronisationsskript des Repos (`GitHub_Sync.bat`,
> `git add -A` und Push nach `origin/main`) läuft beim Nutzer zeitgesteuert. Wird der
> Zwischenstand dieser Etappe dadurch committet, stammt der Commit **nicht** aus der
> Umsetzung. Wer den Stand prüfen will, vergleicht gegen **`4537bae`** (Ende E4).
> Bis zum Abschluss dieser Umsetzung ist kein solcher Commit aufgetreten.

---

## 1 Was umgesetzt wurde

| # | Gegenstand | Datei : Zeile |
|---|---|---|
| 1 | **Rechenkern der Tarifrollen** als reine Funktionen über DTOs (L9), ohne Datenbankzugriff | `Allgemein/Wirtschaftlichkeit/StromTarifRechner.cs` (neu, 315 Zeilen) |
| 2 | Die drei Leistungspreismodelle in einer Funktion | `StromTarifRechner.cs` (`Leistungskosten`) |
| 3 | Vierstufige Staffel mit **kumulierten Obergrenzen** und offener letzter Stufe | `StromTarifRechner.cs` (`Staffelbetrag`) |
| 4 | Differenzmethode mit Herleitungstext | `StromTarifRechner.cs` (`Rechne`) |
| 5 | **Bezugsgröße „Bedarf ohne Anlage"** je Zone — sie fehlte im Modell vollständig | `Allgemein/Wirtschaftlichkeit/StromMatrix.cs` (`Zone.BedarfMWh`, `BedarfGesamtMWh`) |
| 6 | **Lastbilder** (Jahres-, Sommer-, Winter- und zwölf Monatsmaxima) für Bedarf und Restbezug, in EINEM Stundendurchlauf | `StromMatrix.cs` (`Lastbild`, `LastBedarf`, `LastBezug`) |
| 7 | Rollenmodell laden, speichern und rechnen | `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` (`LadeTarif`/`LiesRolle`, `SpeichereTarif`/`TarifSpalten`/`TarifWerte`/`RolleWerte`, `RechneRollentarif`) |
| 8 | **Aufschläge auf den Strombezug** hinter dem Projektschalter, mit Ausweis und Abgleich zur Stromsteuer aus E4 | `WirtschaftlichkeitCtrl.cs` (`RechneAufschlaege`) |
| 9 | **Eingespeister KWK-Strom bekommt einen Preis** (Bestandsmangel) | `WirtschaftlichkeitCtrl.cs` (`BaueEingabe`, Block „Eingespeister KWK-Strom") |
| 10 | **§ 9b StromStG auch ohne BHKW** (offener Punkt 1 aus E4) | `WirtschaftlichkeitCtrl.cs` (`BaueSteuerEingabe`) |
| 11 | Ergebnisspalten `VermiedenArbeit`, `VermiedenLeistung`, `VermiedenGesamt`, `AufschlagBetrag`; Matrixspalte `BedarfMWh` | `WirtschaftlichkeitCtrl.cs` (Konstanten, `StelleTabellenSicher`, `Persistiere`, `LadeErgebnisse`, `LadeStromMatrix`) |
| 12 | **Migrationsschritt 21** — Spaltenkatalog und Begründung | `Allgemein/Update/SchemaKatalog.cs` (`Schritt21_Tarifmodell` und die vier neuen Spaltenkonstanten) |
| 13 | **Migrationsschritt 21** — Schrittnummer, Ausführung, Zählwerk, `ZIEL_VERSION 20 → 21` | `Allgemein/Update/SchemaMigration.cs` (`SCHRITT_21_TARIFMODELL`, `Schritt_21_Tarifmodell`, `DatenTarifmodusVorbelegt`) |
| 14 | Persistenzwerte (zwei Tarifmodi, drei Leistungsmodelle) samt Begründung der vier vermiedenen Fallen | `Allgemein/DbWerte.cs` (`TARIF_MODUS_*`, `LEISTUNGSMODELL_*`) |
| 15 | DTO-Felder von Tarif, Parametersatz und Ergebnis samt Nachweiszeile | `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitDaten.cs` |
| 16 | Tarifdialog mit Modusumschaltung, Rollenblöcken und Staffelraster | `Views/Wirtschaftlichkeit/Form_Tarifstruktur.cs` (neu aufgebaut) |
| 17 | Parameterdialog: Gruppe „Strom — Einspeisung und Bezug" **immer sichtbar**, KWK-Vergütung, Aufschlagsschalter | `Views/Wirtschaftlichkeit/Form_WirtschaftlichkeitParameter.cs` |
| 18 | Vier neue Zeilen im Wirtschaftlichkeitsreiter | `Views/Wirtschaftlichkeit/UcWirtschaftlichkeit.cs` |
| 19 | Dieselben Zeilen im Word-Baustein und im Excel-Blatt | `Allgemein/Bericht/Bausteine/BausteineWirtschaftlichkeit.cs`, `Allgemein/Bericht/ExcelBerichtGenerator.cs` |
| 20 | Vier Ressourcenschlüssel in beiden Sprachen samt Designer | `MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` |

**15 Quelldateien** (14 geändert, 1 neu) und **vier Dokumente**: dieses Protokoll (neu),
`W4_Umsetzungsstand.md`, `Konzept_BHKW_Kosten_Erloese.md` und
`Allgemein/Simulation/Lokalisierung_Katalog.md`.

**Engine unberührt:** `git diff --name-only 4537bae -- 'WindowsFormsApplication1/Allgemein/Simulation/*.cs'`
liefert **null Treffer**; im Ordner ist ausschließlich der Lokalisierungskatalog
(Dokumentation) fortgeschrieben.

---

## 2 Das Tarifmodell

### 2.1 Zwei Modelle nebeneinander, eines schaltet

`Tab_ProjektTarif` wird **additiv** erweitert. Die 16 Spalten der Stufe W3 (vier
Bezugs- und vier Einspeisepreise über Winter/Sommer × HT/NT, HT-Fenster, zweistufige
Staffel) bleiben unverändert stehen und werden weiter gelesen. Die neue Spalte
`Tarif_Modus` entscheidet, welcher Rechenweg gilt:

| Modus | Bedeutung |
|---|---|
| `ZONEN` | **Vorbelegung.** Das Zonenmodell der Stufe W3 rechnet wie bisher. |
| `ROLLEN` | Bezugstarif, Reststromtarif und Einspeisetarif mit der Differenzmethode. |

Ein leerer Modus wird wie `ZONEN` behandelt — eine nicht migrierte Datenbank verhält
sich dadurch wie eine migrierte. **An dieser einen Spalte hängt die
Ergebnisneutralität der ganzen Etappe.**

### 2.2 Die drei Rollen

| Rolle | Spalten | Bedeutung |
|---|---|---|
| **Bezug** | `Bezug_Arbeit`, `Bezug_Grundpreis`, `Bezug_Leistungsmodell`, `Bezug_Monatspreis`, `Bezug_Stufe1…4_KW/_Sommer/_Winter` | Tarif **ohne** BHKW — die Referenz der vermiedenen Kosten |
| **Reststrom** | dieselben 16 Spalten mit Präfix `Rest_` | Tarif **mit** BHKW — kleinere Abnahme, meist teurer |
| **Einspeisung** | `Einsp_Arbeit`, `Einsp_Grundpreis` | Vergütung der eingespeisten Menge |

Je Rolle gilt **ein Durchschnitts-Arbeitspreis**; HT/NT entfällt nach Leitentscheidung
L10. Genau das tut die Altanwendung in `Durchschitt_eintragen` bereits, obwohl sie vier
Preise führt.

**Warum die Einspeisung keine Leistungsstaffel bekommt** — und das ist die einzige
Abweichung vom Auftragswortlaut („je Rolle … plus Leistungspreismodell"): Im Altkatalog
sind Sollleistung und Reduktionsfaktoren des Einspeiseblatts **leer oder 0**, es gibt
**keinen aktiven Lesepfad** mehr, und der Leistungserlös der Einspeisung war fest 0
(Befund 11 der Analyse, von der Datenseite bestätigt in Abschnitt 7.1). 16 Spalten für
eine nachweislich tote Funktion anzulegen wäre Ballast, der später zu pflegen wäre. Die
Rolle führt Arbeits- und Grundpreis; wird der Leistungserlös der Einspeisung je
gebraucht, ist er additiv nachrüstbar wie alles Übrige.

### 2.3 Die vier Fallen des Altkatalogs — und wie sie vermieden sind

`DB-TARIF.XLS` (Analyse, Abschnitt 7.1) enthält vier Konstruktionsfehler. Alle vier sind
im neuen Modell **strukturell** unmöglich, nicht bloß im Code umgangen:

| # | Falle im Altkatalog | Lösung in E5 | Fundstelle |
|---|---|---|---|
| 1 | **Stufen*breiten* statt Obergrenzen.** „500/1500/6000" bedeutet dort Grenzen bei 500, 2.000 und 8.000 kW, weil die Staffelroutine kumulativ aufsummiert. | `Stufe1…4_KW` sind **kumulierte Obergrenzen**. Die Maske sagt es im Klartext und warnt, dass alte Zahlenreihen umzurechnen sind. | `DbWerte.LEISTUNGSMODELL_STAFFEL`, `LeistungsStufe.ObergrenzeKW` |
| 2 | **Stufe 4 nie befüllt** — die Speicherzeile ist auskommentiert, die vierte Stufe stumm der unbegrenzte Rest. | Die vierte Stufe wird **geführt und gespeichert**. Eine Obergrenze ≤ 0 heißt ausdrücklich „nach oben offen" und beendet die Staffel. | `StromTarifRechner.Staffelbetrag` |
| 3 | **Sommerpreis 0 als versteckter Modellschalter** — dann wird nur das Jahresmaximum mit Winterpreisen gestaffelt; bei 22 von 28 Tarifsätzen der Fall. | Das Modell ist eine **sichtbare Auswahl** (`MONATLICH` / `STAFFEL` / `JAHRESHOECHSTLAST`). Ein Preis von 0 ist ein Preis von 0. | `DbWerte.LEISTUNGSMODELL_*`, Auswahlliste im Dialog |
| 4 | **Währungsfalle** — Kopftexte sagen „DM/kW", die Werte sind Euro (142,139 = 278 DM ÷ 1,95583); der Preisstand steht nur im Beschreibungstext („Stand 1.1.96") und wird beim Speichern ersatzlos überschrieben. | Alle Preise sind ausdrücklich in €; das neue Feld **`Tarif_GueltigAb`** hält den Preisstand fest. Es hat keine Rechenwirkung — es wird ausgewiesen, nicht ausgewertet. | `SchemaKatalog.SPALTE_TARIF_GUELTIGAB` |

**Eine fünfte Abweichung**, die keine „Falle", aber ebenso eine stille Regel war: Die
Altanwendung ließ den monatlichen Leistungspreis die Staffel **überstimmen**
(Dialogbeschriftung „Neue Eingabe Leistungspreis pro Monat (hat Vorrang)"). Hier
entscheidet allein das gewählte Modell — kein Vorrang, keine Überraschung.

### 2.4 Textlängen (die Lehre aus Etappe E3)

Der längste Steuerwert dieser Gruppe ist `JAHRESHOECHSTLAST` mit **17 Zeichen**. Die
Spalten `Bezug_Leistungsmodell` und `Rest_Leistungsmodell` sind deshalb **TEXT(24)**
(Auftragsvorgabe; das Konzept nannte TEXT(20), was ebenfalls gereicht hätte).
`Tarif_Modus` ist **TEXT(12)** für den längsten Wert `ROLLEN` (6 Zeichen).

Der Grund für die Sorgfalt: Ein zu kurzes Feld lässt das `UPDATE` **still** scheitern —
`DataRepository.ExecuteSQL` fängt die Ausnahme, der Anwender sieht keinen Fehler, und der
Wert steht danach nicht in der Datenbank. Genau das war Probe C2 aus Etappe E3. Der
Nachweis, dass es diesmal trägt, steht in Abschnitt 6 (V14/V15): `JAHRESHOECHSTLAST`
kommt mit `LEN() = 17` aus der Datenbank zurück, und eine Rundprobe über den echten
Schreibweg vergleicht alle 52 Felder.

---

## 3 Die Rechenkette

### 3.1 Die Differenzmethode ist belegt

```
Bezugskosten ohne BHKW = Arbeit(Bedarf,    Bezugstarif)      + Leistung(Bedarf,    Modell)
Reststromkosten        = Arbeit(Restbezug, Reststromtarif)   + Leistung(Restbezug, Modell)
Vermiedene Kosten      = Bezugskosten ohne BHKW − Reststromkosten
Einspeiseerlös         = Einspeisemenge × Einspeisepreis
```

In der Altanwendung stehen zwei Wege nebeneinander: `py_einsparung_arbeit` bewertet den
Eigenverbrauch mit **einem** Preis, der VBA-Code bildet die **Differenz zweier Tarife**
— und überschreibt die Python-Werte dreißig Zeilen später mit
`einsparung_arbeit(0) = KostenArbeitStrombezug − KostenArbeitReststrombezug`. Genau
dieser Index füllt den Ergebnisdialog (Analyse, Abschnitt 8). **Die Differenzmethode ist
damit belegt** und hier umgesetzt.

### 3.2 Die fehlende Bezugsgröße

„Bedarf ohne Anlage" gab es im Datenmodell nicht — `StromMatrix.Zone` führte nur
Netzbezug, PV-Einspeisung, KWK-Eigenstrom und KWK-Einspeisung. Sie wird jetzt im selben
Stundendurchlauf gebildet:

```
Bedarf ohne Anlage(h) = max(0, Strombedarf(h) − PV-Eigennutzung(h))
```

Das ist derselbe Wert, der schon bisher den KWK-Eigenanteil begrenzt hat — er wird jetzt
zusätzlich als Menge und als Lastbild geführt, statt nach der Stunde weggeworfen zu
werden. Die PV-Vorabverrechnung entspricht der Altanwendung: „Photovoltaik wird vorab vom
Strombedarf abgezogen; das im Ergebnisdialog gezeigte ‚Strombedarf − PV' ist bereits
bereinigt" (Analyse, Abschnitt 2.2).

**Ohne Strombedarfsreihe gibt es keine Referenz.** Dann bleiben die vermiedenen Kosten 0,
und der Hinweis sagt warum — statt eine Einsparung in Höhe der gesamten Reststromkosten
zu behaupten.

### 3.3 Die drei Leistungspreismodelle

Alle drei bemessen sich am **Lastbild** derselben Bezugsgröße, das in einem einzigen
Stundendurchlauf entsteht (`StromMatrix.Lastbild`) — die Wahl des Modells darf keine
zweite Wahrheit erzeugen:

| Modell | Bemessung |
|---|---|
| `MONATLICH` | Σ über zwölf Monatsmaxima × Monatspreis [€/kW·Monat] |
| `STAFFEL` | Sommer- **und** Wintermaximum getrennt durch die vierstufige Staffel, je mit dem Saisonpreis der Stufe |
| `JAHRESHOECHSTLAST` | nur das Jahresmaximum, mit den **Winter**preisen der Staffel |

Zur letzten Zeile: Die Jahresspitze fällt in der Regel in die Winterspanne; ein davon
abweichender Sommerpreis wäre keiner Menge zuordenbar. Die Wahl ist im Code benannt und
begründet, nicht stillschweigend.

### 3.4 Der negative Leistungsanteil ist die Kernaussage

Der Leistungsanteil der vermiedenen Kosten ist **regelmäßig negativ**, weil der
Reststrom-Leistungspreis über dem Bezugs-Leistungspreis liegt (im Beispiel der
Altanwendung −341 €). Das ist kein Fehler. Konsequenzen im Code:

- Er wird als **eigene Zeile** ausgewiesen (Reiter, Word, Excel), nicht in die Summe
  hineingerechnet.
- Die Sichtbarkeitsbedingung der Zeilen prüft auf **„ungleich 0"**, nicht auf
  „größer 0" — eine Zeile, die nur bei positiven Werten erschiene, verschwiege genau
  diese Aussage.
- Der Herleitungstext benennt ihn: „der negative Leistungsanteil ist der Regelfall: der
  Reststromtarif ist teurer als der Bezugstarif".

### 3.5 Was in den Kapitalwert geht — und was nicht

Die vermiedenen Kosten sind eine **Aussage, kein zweiter Zahlungsstrom**. Die Einsparung
steckt bereits darin, dass die Anlage die Bezugsmenge senkt; wer sie zusätzlich als Erlös
bucht, zählt sie doppelt. Deshalb ersetzt — wie im Zonenmodell — der **Reststrom**-Betrag
den Flat-Netzanteil der Energiekosten, und die drei Differenzzeilen werden nur
ausgewiesen. Das ist bewusst und im Code an der Stelle vermerkt.

### 3.6 Bestandsmangel behoben: eingespeister KWK-Strom ohne Preis

Bis E5 las der Erlösposten ausschließlich den **PV-Überschuss**
(`e.Erloes = pvUeberschussMWh × 1000 × p.Einspeiseverguetung`). Eingespeister BHKW-Strom
bekam gar keinen Strompreis, sondern nur den KWK-Zuschlag — und das zugehörige Feld war
im Parameterdialog ohne Photovoltaik-Gruppe **nicht einmal sichtbar**
(`Form_WirtschaftlichkeitParameter.cs:62-66`). Ökonomisch ist das grob falsch.

Behoben in drei Schritten:

1. Neue Projektangabe **`Einspeiseverguetung_KWK`** [€/kWh] (Migrationsschritt 21),
   `NULL` = nicht gepflegt.
2. Die Gruppe im Parameterdialog heißt jetzt **„Strom — Einspeisung und Bezug"** und ist
   **immer sichtbar**, mit beiden Vergütungen. Real liegt der KWK-Preis meist über dem
   PV-Preis — deshalb ein eigenes Feld statt einer gemeinsamen Zahl.
3. Im Rollenmodus bewertet der **Einspeisetarif** beide Mengen (PV-Überschuss und
   KWK-Einspeisung) und ersetzt die Parameterwerte.

**Ergebnisneutral:** Ohne gepflegte KWK-Vergütung bleibt der Beitrag 0. Nachweis
Abschnitt 5, Fall F4: An Projekt 1030 entstehen bei 0,09 €/kWh **29.401,33 €/a**, wo
vorher 0,00 € standen.

### 3.7 Offener Punkt aus E4 erledigt: § 9b ohne BHKW

`BaueSteuerEingabe` lieferte ohne BHKW-Modulzeilen `null` — damit entfiel auch die
Entlastung nach § 9b StromStG, obwohl diese an **keiner** KWK-Anlage hängt: Sie entlastet
den Netzbezug jedes Unternehmens des produzierenden Gewerbes und jedes Betriebs der
Land- und Forstwirtschaft.

Die Erweiterung ist **ergebnisneutral konstruiert**: Sie greift nur, wenn die
Unternehmensart ausdrücklich auf `PROD_GEWERBE` oder `LAND_FORSTWIRTSCHAFT` steht. Die
Vorbelegung aus Migrationsschritt 20b ist `KEIN_PROD_GEWERBE` — ein Bestandsprojekt ohne
BHKW liefert deshalb weiterhin `null` und **meldet auch nichts**. Das ist Absicht: Stünde
an jedem Wärmepumpenprojekt eine Begründung, warum es keine Entlastung gibt, die niemand
beantragt hat, wäre das Rauschen.

Nachweis Abschnitt 5, Fall F8: Projekt 1023 (Wärmepumpe, kein BHKW) mit
`PROD_GEWERBE` — A-Stand 0,00 €, E5-Stand **2.069,25 €/a** (= 20,00 €/MWh ×
115,96 MWh − 250 €).

---

## 4 Die Aufschläge — erst gemessen, dann entschieden

### 4.1 Der Bestandszustand

Netzentgelt, Umlagen, Stromsteuer, Konzessionsabgabe und Vertrieb sind seit dem
Stromspeicherpaket je Energieträger in `energy_project_settings` gepflegt, mit eigenen
Aktiv-Flags und Vorschlagswerten, die sich auf **11,746 ct/kWh** summieren. Sie wirken
aber **ausschließlich in der Speichersimulation**; die Jahreskostenrechnung
(`KostenEmissionRechner.cs:106-123`) rechnet den Netzbezug ohne jeden Aufschlag:

```csharp
stromKosten = netzbezugMWh * 1000.0 * strom.PreisArbeit.Value;
```

**Erhebung über alle neun Referenzprojekte** (produktive Datenbank, nur gelesen; Stand
19.08.2026 02:51):

| Projekt | Strom-Träger | Zeile in `energy_project_settings` | Aktiv-Flags | wirksamer Aufschlag |
|---|---|---|---|---|
| 1007 | — | **keine Zeile** | — | 0 ct/kWh |
| 1008 | — | keine Zeile | — | 0 ct/kWh |
| 1011 | — | 2 Zeilen (Heizöl L, Erdgas E), **kein** Strom-Träger | alle FALSE | 0 ct/kWh |
| 1017 | 54 „Strom Variante", 58 „Elektrische Energie 2" | **gepflegt** (6,44 / 2,946 / 2,05 / 0,11 / 0,20) | **alle TRUE** | **11,746 ct/kWh** |
| 1018 | — | 1 Zeile Erdgas E | alle FALSE | 0 ct/kWh |
| 1021 | — | keine Zeile | — | 0 ct/kWh |
| 1023 | 60 „Elektrische Energie" | **gepflegt** | **alle TRUE** | **11,746 ct/kWh** |
| 1024 | 60 „Elektrische Energie" | **gepflegt** (7 weitere Träger ohne Aufschlag) | **alle TRUE** | **11,746 ct/kWh** |
| 1030 | 60 „Elektrische Energie" | **Zeile vorhanden, Werte NULL** | alle FALSE | **11,746 ct/kWh** ⚠ |

> **Eine Falle, die die Erhebung sichtbar gemacht hat.** Bei Projekt 1030 stehen alle
> fünf Aktiv-Flags auf `False` und alle fünf Werte auf `NULL` — trotzdem liefert der
> Leseweg **den vollen Vorschlagssatz von 11,746 ct/kWh**. Grund ist die
> ausdrückliche Regel in `StromAufschlagCtrl.Komponente`: Ein `NULL`-Wert heißt „nicht
> gepflegt", und dann bleibt der Vorgabewert **samt Vorgabe-Aktivschalter** stehen. Die
> Regel ist sinnvoll (Access legt eine neue `YESNO`-Spalte in jeder Bestandszeile mit
> `False` an — ohne sie wäre jeder Aufschlag stillschweigend 0), aber sie bedeutet:
> **Die Flags in der Datenbank sind kein verlässliches „Aus".** Deshalb nennt der
> Ergebnishinweis den tatsächlich angesetzten Satz mit seiner Zerlegung, statt nur den
> Betrag auszuweisen.

### 4.2 Die gemessene Wirkung

**Messverfahren.** Auf einer Wegwerf-Kopie der produktiven Datenbank wurde der wirksame
Aufschlag (0,11746 €/kWh) bei allen vier Projekten mit Strom-Träger auf
`custom_price_work` aufgeschlagen und die Wirtschaftlichkeit erneut gerechnet — das
reproduziert exakt, was eine Berücksichtigung täte. **Gegenprobe:** Die spätere
Umsetzung mit dem echten Schalter liefert an Projekt 1030 **auf den Cent dieselben
Zahlen** (1.485.561,0746 € Energiekosten, −27.836.179,6439 € Kapitalwert) — das
Messverfahren war korrekt.

| Projekt | Netzbezug [MWh/a] | Aufschlagsbetrag [€/a] | Energiekosten vorher → nachher | Δ | Kapitalwert vorher → nachher | Δ | Gestehungskosten |
|---|---:|---:|---|---:|---|---:|---|
| **1023** | 115,96 | **13.620,66** | 40.636,00 → 54.256,66 € | **+33,52 %** | −613.034,94 → −815.675,99 € | **−33,06 %** | 0,1057 → 0,1407 €/kWh (**+33,11 %**) |
| **1030** | 3.070,01 | **360.603,37** | 1.124.957,70 → 1.485.561,07 € | **+32,05 %** | −21.443.873,43 → −27.836.179,64 € | **−29,81 %** (−6,39 Mio. €) | 0,2348 → 0,3048 €/kWh (**+29,81 %**) |
| 1017 | 652,45 | 76.636,78 | *nicht bestimmbar* (Stromkosten 247.981 → 324.618 €, **+30,90 %**) | — | *nicht bestimmbar* | — | — |
| 1024 | 387,12 | 45.471,12 | *nicht bestimmbar* (Stromkosten 135.542 → 181.013 €, **+33,55 %**) | — | *nicht bestimmbar* | — | — |
| 1007, 1008, 1011, 1018, 1021 | 38,8 / 9.964,2 / 5.535,1 / −15,0 / 5.140,6 | **0,00** | unverändert | 0 | unverändert | 0 | unverändert |

*„Nicht bestimmbar" heißt: Bei 1017 und 1024 fehlt einzelnen Erzeugeranlagen die
Energieträger-Zuordnung, deshalb bleibt `Energiekosten` und mit ihr der Kapitalwert
`null` (Befund D5-Regel). Der Aufschlagsbetrag selbst ist trotzdem exakt bestimmbar und
oben ausgewiesen.*

**Einordnung.** Vier von neun Referenzprojekten sind betroffen, in zweien lässt sich die
Wirkung bis zum Kapitalwert durchrechnen. Sie liegt bei **rund einem Drittel** — weit
jenseits der „paar Prozent", ab denen der Auftrag eine ausdrückliche Meldung verlangt.
An Projekt 1030 sind das **6,39 Mio. € Kapitalwert**.

### 4.3 Die Entscheidung: ein Schalter je Projekt, Vorgabe AUS

Umgesetzt ist der im Auftrag vorgeschlagene Weg — er ist auch der einzige, der die
Ergebnisneutralität hält:

- Neue Projektangabe **`Aufschlaege_Anwenden`** (`YESNO`) in
  `Tab_ProjektWirtschaftlichkeit`, Migrationsschritt 21. Access legt eine `YESNO`-Spalte
  in jeder Bestandszeile mit `False` an — die gewollte Vorbelegung entsteht damit ohne
  eigenen DML-Schritt.
- Im Parameterdialog als Ankreuzfeld „Aufschläge (Netzentgelt, Umlagen, Stromsteuer,
  Konzession, Vertrieb) berücksichtigen", mit dem Größenordnungshinweis im Fließtext.
- Der Aufschlag wird **in der Wirtschaftlichkeit** angesetzt
  (`WirtschaftlichkeitCtrl.RechneAufschlaege`), nicht im `KostenEmissionRechner`. Grund:
  Dort liegen beide Größen vor (Netzbezug und Parametersatz), die Kennzahl „Kosten
  (einfach)" des Berichts bleibt unberührt, und es ist genau derselbe Weg, den der
  W3-Tarifersatz seit Phase 8 geht.
- Der Betrag steht als **eigene Ergebnisgröße** (`AufschlagBetrag`) und als
  Hinweiszeile mit Satz, Zerlegung, Menge und Ergebnis:

  > *Aufschläge berücksichtigt: 11,746 ct/kWh (Netzentgelt 6,440 + Umlagen 2,946 +
  > Stromsteuer 2,050 + Konzession 0,110 + Vertrieb 0,200) auf 3.070,0 MWh Netzbezug =
  > 360.603,37 €/a.*

**Warum kein globaler Schalter und keine automatische Übernahme.** Ein Drittel der
Energiekosten ist keine Feinjustierung. Eine stille Übernahme hätte jede gespeicherte
Altrechnung entwertet, ohne dass der Anwender die Ursache sähe. Ein Schalter je Projekt
passt außerdem zur Sache: Ob die Aufschläge im gepflegten Arbeitspreis schon enthalten
sind, ist eine Eigenschaft des Projekts (mancher pflegt einen Endpreis, mancher den
Beschaffungspreis) — nicht der Installation.

### 4.4 Zusammenspiel mit der Stromsteuer aus Etappe E4

Die Frage war, ob der Steuerbestandteil der Aufschläge und die Stromsteuer-Entlastung aus
E4 sich **doppelt** auswirken. Die Antwort:

| Größe | Betrag | Rolle |
|---|---|---|
| `Aufschlag_Stromsteuer` | 2,050 ct/kWh ≙ **20,50 €/MWh** | **Belastung** im Bezugspreis (Regelsatz § 3 StromStG) |
| § 9b StromStG (E4) | **20,00 €/MWh** abzüglich 250 €/a Sockel | **Entlastung** auf denselben Netzbezug |

**Kein Doppelansatz — die zwei Seiten derselben Vorschrift.** Der Regelsatz wird erhoben,
und das produzierende Gewerbe bekommt ihn bis auf eine Restbelastung von 0,50 €/MWh
erstattet. Genau diese Differenz ist der Grund, warum L4 („Steuersatz und Entlastungssatz
getrennt, nie eine Differenz raten") beide Sätze einzeln im Katalog führt.

**Der Widerspruch liegt im umgekehrten Fall, und den gibt es im Bestand.** Steht der
Aufschlagsschalter auf AUS, während § 9b greift, enthält der Kapitalwert eine **Entlastung
ohne die zugehörige Belastung** — die Rechnung ist dann um 20,00 €/MWh zu günstig. Das
Ergebnis meldet es im Klartext:

> *Hinweis: Die Stromsteuer-Entlastung nach § 9b wird gutgeschrieben, obwohl die
> Stromsteuer im Bezugspreis nicht angesetzt ist (Schalter „Aufschläge in der
> Wirtschaftlichkeit berücksichtigen" aus). Der Kapitalwert enthält damit eine Entlastung
> ohne die zugehörige Belastung.*

Steht der Schalter dagegen an und ist die Stromsteuer-Komponente aktiv, erscheint die
Einordnung:

> *Stromsteuer: Belastung 2,050 ct/kWh im Bezugspreis und Entlastung nach § 9b als
> Gutschrift — kein Doppelansatz, sondern die zwei Seiten derselben Vorschrift.*

**Bewusst nicht gekoppelt.** Die naheliegende Alternative — § 9b nur zulassen, wenn der
Schalter an ist — wurde verworfen: Sie hätte E4 nachträglich verändert und
Bestandsergebnisse gekippt, und sie würde eine fachliche Entscheidung (welche Steuer
beantragt wird) an eine Darstellungsfrage (welcher Preis gepflegt ist) binden. Sichtbar
machen statt stillschweigend korrigieren ist die Linie dieses Moduls.

---

## 5 Wirkungsnachweis an präparierten Kopien von Projekt 1030

Projekt 1030 „Referenz BHKW-Kaskade (Regressionstest)": zwei BHKW-Module (50 / 250 kW
el), Spitzenkessel, Pufferspeicher, **keine Photovoltaik**. Gemessene Mengen und
Lastbilder aus dem Lauf (Grundlage aller Handrechnungen):

```
Bedarf ohne Anlage 4.790,086000 MWh    Lastbild Bedarf : Jahr 2.070,0  Sommer 1.335,0  Winter 2.070,0  Σ Monatsmaxima 15.965,0
Restbezug (Netz)   3.070,008616 MWh    Lastbild Bezug  : Jahr 1.770,0  Sommer 1.320,7  Winter 1.770,0  Σ Monatsmaxima 13.904,5829
KWK-Einspeisung      326,681416 MWh
KWK-Eigenstrom     1.393,395963 MWh    PV-Einspeisung 0,000 MWh
```

Tarifsatz der Fälle F1–F3: Bezug 0,25 €/kWh, Reststrom 0,28 €/kWh, Einspeisung
0,09 €/kWh. Staffel (F2/F3), kumulierte Obergrenzen 500 / 2.000 / 8.000 / offen:
Bezug Sommer 60/50/40/30, Winter 80/70/60/50 €/kW·a; Reststrom Sommer 70/60/50/40,
Winter 95/85/75/65 €/kW·a.

| Fall | Präparation | Ergebnis | Handrechnung |
|---|---|---|---|
| **F1** | Rollenmodell, Leistungsmodell **`MONATLICH`**, Monatspreis Bezug 5, Reststrom 7 €/kW·Monat | Leistung Bezug 79.825,00 €; Reststrom 97.332,08 €; **vermiedene Kosten Arbeit 337.919,09 €, Leistung −17.507,08 €, gesamt 320.412,01 €**; Reststromkosten 956.934,49 €; Kapitalwert −24.321.915,11 € | 15.965 × 5 = 79.825 ✓ · 13.904,5829 × 7 = 97.332,08 ✓ · 4.790,086 × 1000 × 0,25 − 3.070,0086 × 1000 × 0,28 = 337.919,09 ✓ |
| **F2** | wie F1, Modell **`STAFFEL`** | Leistung Bezug 220.950,00 €; Reststrom 239.693,01 €; **Leistung −18.743,02 €**, gesamt 319.176,07 €; Reststromkosten 1.099.295,43 €; Kapitalwert −26.845.504,04 € | Bezug Sommer 500·60 + 835·50 = 71.750; Winter 500·80 + 1.500·70 + 70·60 = 149.200 ⇒ 220.950 ✓ · Reststrom Sommer 500·70 + 820,7169·60 = 84.243,01; Winter 500·95 + 1.270·85 = 155.450 ⇒ 239.693,01 ✓ |
| **F3** | wie F2, Modell **`JAHRESHOECHSTLAST`** | Leistung Bezug 149.200,00 €; Reststrom 155.450,00 €; **Leistung −6.250,00 €**, gesamt 331.669,09 €; Reststromkosten 1.015.052,41 €; Kapitalwert −25.352.153,77 € | Bezug 500·80 + 1.500·70 + 70·60 = 149.200 ✓ · Reststrom 500·95 + 1.270·85 = 155.450 ✓ |
| **F4** | Tarif **inaktiv**, `Einspeiseverguetung_KWK` = 0,09 €/kWh | **Einspeiseerlös 0,00 → 29.401,33 €/a**; Energiekosten unverändert; Kapitalwert −21.443.873,43 → −21.006.455,92 € (**+2,04 %**) | 326,681416 × 1000 × 0,09 = 29.401,33 ✓ |
| **F5** | `Aufschlaege_Anwenden` = WAHR | **Aufschlagsbetrag 360.603,37 €/a**; Energiekosten 1.124.957,70 → 1.485.561,07 €; Kapitalwert −27.836.179,64 € (**−29,81 %**) | 3.070,01 × 1000 × 0,11746 = 360.603,37 ✓; identisch zur Messung aus 4.2 |
| **F6** | `Unternehmensart` = `PROD_GEWERBE`, Aufschläge **AUS** | § 9b **61.150,17 €/a**; Kapitalwert −20.534.113,28 € (**+4,24 %**) · **Warnung** „Entlastung ohne die zugehörige Belastung" | 20,00 × 3.070,0086 − 250 = 61.150,17 ✓ (deckungsgleich mit E4/V16) |
| **F7** | beides | Aufschlag 360.603,37 € **und** § 9b 61.150,17 €; Kapitalwert −26.926.419,49 € · **Einordnung** „kein Doppelansatz" | Summe beider Wirkungen ✓ |
| **F8** | *Projekt 1023* (Wärmepumpe, **kein BHKW**), `Unternehmensart` = `PROD_GEWERBE` | A-Stand **0,00 €**, E5-Stand **2.069,25 €/a**; Kapitalwert −613.034,94 → −582.249,70 € | 20,00 × 115,96258 − 250 = 2.069,25 ✓ |

**Die drei Leistungspreismodelle unterscheiden sich am selben Lastbild um bis zu
12.493 €/a im Leistungsanteil** (−6.250 bis −18.743 €) — in allen drei Fällen negativ,
wie erwartet. Der Modellwechsel ist damit keine Kosmetik, sondern eine
ergebnisrelevante Entscheidung, und genau deshalb muss er sichtbar getroffen werden
statt aus einem Sommerpreis von 0 zu folgen.

---

## 6 Verifikation

### 6.1 Referenzlauf

Beide Stände aus eigenen Exporten außerhalb des Repos gebaut (`git archive 4537bae` für
A, Arbeitsbaum-Überlagerung für B; Unterschied nachgewiesen: **exakt 15 Dateien**),
jeweils mit dem mitgelieferten `Referenzlauf.csproj` (ProjectReference auf die App ⇒ Exe
und DLL konsistent). **Eine gemeinsame Wegwerf-Kopie** der produktiven Datenbank, mit dem
B-Stand von Schemastand 17 auf 21 migriert, danach von beiden Ständen gelesen. Neun
Projekte, feste Liste `--projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030`,
Feature-Flag `Kaskade_Zweikanalig` **AUS**.

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
`2026-08-19_B5` **und** gegen den A-Stand. Kein neuer Ergebnisschlüssel — E5 fasst den
Rechenkern nicht an.

### 6.2 Verifikationstabelle

| # | Prüfung | Methode | Ergebnis |
|---|---|---|---|
| V1 | Engine unberührt | `git diff --name-only 4537bae -- 'WindowsFormsApplication1/Allgemein/Simulation/*.cs'` | **leer** (im Ordner ist nur `Lokalisierung_Katalog.md` geändert — Dokumentation, kein Code) |
| V2 | Simulationsergebnisse gegen die Basis B5 | `Referenzlauf.exe vergleich`, gemeinsame DB-Kopie, Flag AUS | **9/9 PASS**, 2 366 177 Werte |
| V3 | dito A gegen B | dito | **9/9 PASS** |
| V4 | Byte-Identität gegen B5 | `cmp` je Datei | **216/216 gleich**, 0 Abweichungen |
| V5 | Byte-Identität A gegen B | `cmp` je Datei | **216/216 gleich** |
| V6 | Wirtschaftlichkeitswerte unverändert | Harnisch auf `BerichtsDatenSammler.Sammle` + `WirtschaftlichkeitCtrl.Berechne`, A gegen B, gemeinsame Kopie, 9 Projekte × 3 Szenarien | **27/27 Zeilen wertgleich** in allen 14 Zahlenspalten (Investition, Betrieb, Energie, Erlös, CO₂, KWKG, Vbh, drei Steuergutschriften, Kapitalwert, Differenz, Gestehungskosten, Tarifkosten) |
| V7 | Auch die Texte unverändert | dieselbe Probe, Spalten Fehlgrund und Hinweis | **27/27 zeichengleich** — E5 erzeugt auf dem Bestand keine einzige neue Meldung |
| V8 | Vermiedene Kosten und Aufschlag sind auf dem Bestand null | dieselbe Probe | Summe über alle Zeilen: **0,0000 € / 0,0000 € / 0,0000 € / 0,0000 €** |
| V9 | Migration 17 → 21 | `Referenzlauf.exe migration` auf Wegwerf-Kopie der Produktiv-DB | **Schemastand 21**, Schritt 21 „OK" |
| V10 | Migration 20 → 21 | Kopie des A-Laufs (Stand 20), `--nokopie` | **Schemastand 20 → 21**, Schritt 21 „OK" |
| V11 | Spalten korrekt angelegt | `GetOleDbSchemaTable` | `Tab_ProjektTarif` 19 → **55** Spalten (36 neu, Positionen 20–55, alle **angehängt**); `Tab_ProjektWirtschaftlichkeit` **+2** |
| V12 | Vorbelegung wie vorgesehen | Kopie mit einer gepflegten Tarifzeile, `SELECT` nach der Migration | `Tarif_Modus` = **`ZONEN`**, `Bezug_/Rest_Leistungsmodell` = **`MONATLICH`**, `Aufschlaege_Anwenden` = **False**, `Einspeiseverguetung_KWK` = **NULL**, `Tarif_GueltigAb` = **NULL**, alle 34 Preisspalten **NULL** |
| V13 | Bestandswerte unversehrt | Vorher/Nachher-Dump derselben Datei | Tarifzeile (Aktiv, Winter 10–3, HT 7–21, vier Bezugspreise, Staffel 1500/95,5/62,25) **wertgleich**; beide Parametersätze (1019, 1030) wertgleich in Zins, Zeitraum und allen sechs E4-Steuerangaben |
| V14 | **Textlänge trägt** (Lehre aus E3) | `SELECT LEN(...)` nach dem Schreiben über den echten Weg | `Tarif_Modus` „ROLLEN" **LEN 6**, `Bezug_Leistungsmodell` „JAHRESHOECHSTLAST" **LEN 17** — vollständig, kein stiller Abschnitt |
| V15 | **Rundprobe des Schreibwegs** | `WirtschaftlichkeitCtrl.SpeichereTarif` → `LadeTarif`, alle 52 Felder verglichen, je einmal über den INSERT- und den UPDATE-Zweig, zwei Projekte | **52/52 wertgleich**, 4 Läufe ohne Abweichung |
| V16 | Doppelstart idempotent | zweiter Migrationslauf mit `--nokopie` | „Schritt 21 …: bereits erledigt", **0 Angaben vorbelegt**, Stand bleibt 21, weiterhin 55 Spalten |
| V17 | Wirkung der drei Leistungspreismodelle | präparierte Kopien F1, F2, F3 | −17.507,08 / −18.743,02 / −6.250,00 € Leistungsanteil, jeweils = Handrechnung |
| V18 | Vermiedene Kosten mit **negativem** Leistungsanteil | F1–F3 | in allen drei Fällen negativ, als eigene Zeile ausgewiesen, im Hinweis als Regelfall benannt |
| V19 | Einspeiseerlös **ohne PV** im Projekt | F4 | **29.401,33 €/a** statt 0,00 €; Kapitalwert +2,04 % |
| V20 | Aufschläge mit und ohne Schalter | F5 gegen Basis | 360.603,37 €/a; Energiekosten +32,05 %, Kapitalwert −29,81 % |
| V21 | Gegenprobe des Messverfahrens | F5 gegen die Vorab-Messung aus 4.2 | **auf den Cent identisch** (1.485.561,0746 € / −27.836.179,6439 €) |
| V22 | § 9b **ohne BHKW** (offener Punkt E4) | F8, Projekt 1023, A gegen B | A **0,00 €**, B **2.069,25 €** = Handrechnung |
| V23 | § 9b bleibt ohne ausdrückliche Angabe wirkungslos | V6/V7 (alle neun Projekte auf `KEIN_PROD_GEWERBE`) | **0,00 €**, keine neue Meldung |
| V24 | Widerspruchs-Hinweis Stromsteuer | F6 (§ 9b an, Aufschläge aus) | Warnung „Entlastung ohne die zugehörige Belastung" erscheint |
| V25 | Einordnungs-Hinweis Stromsteuer | F7 (beides an) | „kein Doppelansatz, sondern die zwei Seiten derselben Vorschrift" erscheint |
| V26 | Ressourcen in beiden `.resx` und im Designer | `grep` je Schlüssel | **4/4** in `Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` |
| V27 | Build | `MSBuild WindowsFormsApplication1.csproj -p:Platform=x86`, Ausgabe in den Scratch-Ordner | **0 Fehler, exakt 6 Warnungen** (CS0108 ×2, CS0109 ×2, CS1998, CS4014) |
| V28 | Kodierung und Zeilenenden | `file`, CR-Zählung, Suche nach U+FFFD je Datei | unverändert (7 × UTF-8 mit BOM, 8 × ohne), jede Datei behält ihre Zeilenenden (5 × LF, 10 × CRLF), **0 Ersatzzeichen** |
| V29 | Produktivdatenbank nur gelesen | `Kenndaten.laccdb` vor jedem Zugriff geprüft (nie vorhanden); alle Proben auf Wegwerf-Kopien; MD5 vor und nach allen Läufen | **unverändert** (`66F4806A3B89074B52344F39D477F151`, 96 436 224 Byte, 19.08.2026 02:51) |
| V30 | `bin\` des Repos unberührt | jeder Build des Arbeitsbaums ausschließlich mit `-p:OutDir=<Scratch>` | **erfüllt** |

---

## 7 Offene Punkte

### Zur Entscheidung durch den Nutzer

1. **Die Aufschläge sind gemessen, aber nicht Vorgabeverhalten** (Abschnitt 4). Ob der
   Schalter für neue Projekte künftig auf AN stehen soll — und ob Bestandsprojekte
   einmalig umgestellt werden —, ist eine fachliche Entscheidung mit einer Wirkung von
   rund einem Drittel des Kapitalwerts. Sie wird hier ausdrücklich **nicht** getroffen.
2. **Die Aktiv-Flags sind kein verlässliches „Aus"** (Abschnitt 4.1, Kasten). Ein
   Trägersatz ohne gepflegte Werte liefert den vollen Vorschlagssatz. Das ist im
   Speicherpaket bewusst so gebaut, überrascht hier aber. Ein sauberer Weg wäre ein
   ausdrückliches Kennzeichen „Aufschlagsblock gepflegt" an der Zeile — eine Änderung
   im Stromspeicher-Modul, nicht in W4.

### Für Etappe E6

3. **Der Tarifsatz gilt für die ganze Vergleichsgruppe.** `LadeTarif(daten.IdStamm)` —
   eine Zeile je Stammprojekt. Varianten mit abweichendem Tarif sind damit nicht
   abbildbar. Dasselbe Muster wie beim Parametersatz und bewusst so; erwähnt, weil das
   Rollenmodell die Frage schärfer stellt als das Zonenmodell.
4. **Kein Katalog für Tarifsätze.** Der Altbestand hatte `DB-TARIF.XLS` mit 28
   Bezugs- und 15 Einspeisesätzen. Übernommen wurde nur die Struktur (die Werte sind
   Preisstand 1996); eine Katalogtabelle `Tab_Tarif_STAMM` mit Übernahme ins Projekt
   gibt es nicht. Für den produktiven Einsatz mit mehreren Standorten wäre sie der
   nächste Schritt.
5. **Sommer/Winter kommt aus der Monatsspanne des Tarifs**, nicht aus einem eigenen
   Feldpaar des Rollenmodells. Das ist bewusst (eine Wahrheit für beide Modelle), heißt
   aber: Wer im Rollenmodus die Saisongrenze verschiebt, verschiebt sie auch für das
   Zonenmodell derselben Zeile.

### Für Etappe E7

6. **Die vermiedenen Kosten sind Ausweis, nicht Zahlungsstrom** (Abschnitt 3.5). Die
   Mehrjahrestabelle der Etappe E7 muss das kenntlich machen, sonst liest sich die Zeile
   wie ein Erlös.
7. **Der Aufschlagsbetrag steckt in den Energiekosten und steht zusätzlich als eigene
   Zeile.** Wer beide addiert, zählt doppelt. Im Reiter ist die Reihenfolge eindeutig
   (Energiekosten oben, Aufschlag als Nachweis unten); im Bericht sollte es
   beschriftet werden.
8. **`Tab_ErgebnisStromMatrix` führt jetzt `BedarfMWh`**, aber keine Lastbilder. Die
   zwölf Monatsmaxima entstehen bei jedem Lauf neu und werden nicht persistiert — für
   eine Ausgabe „Leistungspreis je Monat" wären sie zu speichern.

### Betrieb

9. **Ein neuer Basis-Freeze steht weiterhin aus** (E8). `2026-08-19_B5` bleibt gültig:
   216 von 216 CSV sind byte-identisch, E5 hat den Rechenkern nicht angefasst.

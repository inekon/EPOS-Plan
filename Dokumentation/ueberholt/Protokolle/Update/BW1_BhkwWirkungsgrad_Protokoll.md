# Schemaschritt 98 — der BHKW-Wirkungsgrad ist ein Faktor

Protokoll zum Auftrag BW‑1 (19.09.2026). Der gültige Stand steht im Quelltext
(`EPOS.Kern/Allgemein/Update/BhkwWirkungsgradFaktor.cs`), in `Referenzlaeufe/LIESMICH.md`
und in der Statusdatei; dieses Papier hält fest, was gefunden, entschieden und gemessen
wurde.

## 1 Der Befund

`Tab_BHKW[_STAMM].Wirkungsgrad` ist der **Gesamtwirkungsgrad als Faktor**. Drei Belege, die
in dieselbe Richtung zeigen:

- Die Maske nennt das Feld „Ges. Wirkungsgrad" und schreibt daneben den Hinweis „z. B. 0,85".
- `SimulationBHKW.Auswertung` rechnet `Verbrauch = (Wärme + Strom) / Wirkungsgrad` — eine
  Division, die nur mit einem Faktor eine Brennstoffmenge ergibt.
- 41 der 79 Katalogsätze der Testdatenbank tragen genau das: Werte zwischen 0,827 und 1,03.

**38 Sätze taten es nicht.** Sie trugen einen Prozentwert zwischen 29,3 und 44,3 — und zwar
den des **elektrischen** Wirkungsgrads. Nachgerechnet am Modul, an dem der Anwender es
gesehen hat (EC‑POWER XRGI 15, `Ptherm` 30,8 kW, `Pel` 14,5 kW, Katalogwert 29,5):

```
Brennstoff  = Pel / eta_el      = 14,5 / 0,295              = 49,15 kW
eta_gesamt  = (Ptherm + Pel) / Brennstoff = 45,3 / 49,15     = 0,9216
```

Geteilt wurde stattdessen durch 29,5. Der Brennstoff fiel damit um Faktor
`29,5 / 0,9216 = 32,0` zu klein aus — im Anwenderbild 1,56 statt 49,9 MWh/a bei 1 016
Volllaststunden. An derselben Größe hängen **Gasspitze, Emissionen und Brennstoffkosten**
des BHKW.

Der Befund stammt aus Auftrag BH‑1
([`BH1_Brennstoffblock_Leerhinweis_Protokoll.md`](../Simulation/BH1_Brennstoffblock_Leerhinweis_Protokoll.md),
Abschnitt 4, Punkt BH1‑O1). Der Anwender hat am 19.09.2026 entschieden: **„Katalog
vereinheitlichen + Basis neu"**.

## 2 Die Umrechnung und ihr Band

Schemaschritt **98** ist ein reiner Datenschritt (kein DDL). Je Zeile in
`Tab_BHKW_STAMM` und `Tab_BHKW`:

```
Wirkungsgrad > 1  und  Pel > 0  und  Ptherm > 0
    ->  Wirkungsgrad_neu = ROUND((Ptherm + Pel) * Wirkungsgrad / 100 / Pel, 4)
```

**Übernommen wird nur, was in [0,5; 1,05] fällt.** Alles andere bleibt stehen und wird mit
Id, Bezeichner, altem und gerechnetem Wert **benannt ausgewiesen** — nie still umgedeutet.
Werte bis 1 bleiben unberührt; sie sind der Faktor, den der Rechenweg erwartet.

**Warum die Obergrenze bei 1,05 liegt.** Ein Brennwert-BHKW hat einen Gesamtwirkungsgrad
über 1, wenn er auf den Heizwert bezogen wird. Die Testdatenbank führt acht solcher Sätze:
vier mit 1,023 bis 1,03, die schon vorher Faktoren waren, und vier, die der Schritt auf
1,024 bis 1,048 rechnet. 1,05 lässt sie alle zu und weist jeden Prozentwert ab. Die
Untergrenze 0,5 fängt den anderen Fehlgriff: Ein Gesamtwirkungsgrad unter 50 % ist bei
keinem BHKW zu erklären.

**Das Band hat gehalten.** Die vier Sätze mit 1,023 bis 1,03 fallen unter die Regel
`Wirkungsgrad > 1` — ihre Rechnung ergibt aber rund 0,033 und damit weit unter das Band.
Der Schritt lässt sie deshalb stehen und weist sie aus; ohne das Band hätte er vier
gepflegte Faktoren zerstört.

**Wiederholbar:** Nach dem Lauf trifft die Bedingung keine Zeile mehr (`Offen` = 0), und die
Anweisungsfolge bleibt leer.

**Die Bestandsaufnahme wird VOR dem Schreiben gezogen** (`Bestandsaufnahme`). Das ist keine
Bequemlichkeit: Eine gerade umgerechnete Brennwertzeile trägt danach selbst einen Faktor
über 1 und stünde in einer nachträglichen Abfrage als „ausgewiesen" da, obwohl der Schritt
sie eben erst gesetzt hat.

**Eine Quelle, drei Leser** — dasselbe Muster wie bei Schritt 94:
`EPOS.Kern/Allgemein/Update/BhkwWirkungsgradFaktor.cs` trägt SQL, Parameter, Zählung,
Ausweisung und Berichtstext; daraus bedienen sich `SchemaMigration.Schritt_98_BhkwWirkungsgrad`,
`Werkzeuge/Testdatenbankschema` und `EPOS.Kern.Tests/TestDatenbank`.

## 3 Zählung an der Testdatenbank

| Tabelle | Zeilen | umgerechnet | ausgewiesen | unverändert |
|---|---:|---:|---:|---:|
| `Tab_BHKW_STAMM` | 79 | **34** | 4 | 41 |
| `Tab_BHKW` | 6 | **3** | 1 | 2 |

**Die fünf ausgewiesenen Zeilen** — alle mit einem Wert, der schon ein Faktor ist:

| Tabelle | Id | Bezeichner | Wert bleibt | gerechnet hätte |
|---|---:|---|---:|---:|
| `Tab_BHKW_STAMM` | 67 | A-Tron_21_F | 1,03 | 0,0329 |
| `Tab_BHKW_STAMM` | 68 | A-Tron_21_G | 1,03 | 0,0329 |
| `Tab_BHKW_STAMM` | 97 | EC_Power_20kw.el Gas Brennwert | 1,023 | 0,0318 |
| `Tab_BHKW_STAMM` | 104 | EC_Power_9kw.el Gas Brennwert | 1,03 | 0,0347 |
| `Tab_BHKW` | 1018146 | A-Tron_21_F | 1,03 | 0,0329 |

Von den 34 umgerechneten Katalogsätzen fallen **29** ins Band 0,80–1,00, einer auf 0,7999
(ein reines Wasserstoffmodul) und **vier** auf 1,024 bis 1,048 (Brennwert). Keine Zeile ohne
`Pel` oder ohne `Ptherm`.

**Nach dem Lauf liegt in keiner der beiden Tabellen ein Wert über 1,05** — das ist die Probe
des Schrittes, und sie ist auch die Grenze, ab der die Pflege ablehnt.

## 4 Der Schutz vor Wiederholung

`KatalogFeldPruefung.WirkungsgradFaktor` lehnt jeden Wert außerhalb **(0; 1,05]** benannt ab;
die Obergrenze kommt aus derselben Quelle wie die Umrechnung
(`BhkwWirkungsgradFaktor.BAND_BIS`). Gezogen wird die Regel an beiden Schreibwegen des
Katalogs:

- `BHKWStammCtrl.FelderUebernehmen` — der Aufklapper „Alle Daten anzeigen"; dort stand bis
  hierher nur „nicht negativ", und das ließ 29,5 durch.
- `BhkwKatalogDialog.EingabenPruefen` — der Katalogeditor; der Hinweis am Feld nennt jetzt
  „(Faktor 0–1, z. B. 0,90)" statt „(z. B. 0,85)".

Zwei Ressourcenschlüssel in beiden Sprachen (`KBROW_MSG_WIRKUNGSGRAD_FAKTOR`,
`BHKWK_MSG_WIRKUNGSGRAD`), dazu der geänderte Hinweis `BHKWK_HINT_WIRKUNGSGRAD`;
`Resource.Designer.cs` neu erzeugt.

**Einen Importweg für BHKW-Katalogsätze gibt es nicht.** Geprüft wurden
`EPOS.Kern/Allgemein/Import/` (VDI 3805 liegt dort für Puffer- und Solarkataloge,
`KatalogImportProfil`/`ModulImportProfil` führen kein BHKW), der Projekttransfer und die
Katalogübernahme: `KomponentenUebernahmeCtrl` und `BHKWCtrl.CopyFromStamm` **kopieren** eine
Katalogzeile in das Projekt, sie erfassen keinen Wert. Ein Prozentwert kann damit nur über
die beiden geprüften Dialogwege entstehen — und beide weisen ihn jetzt ab. Die Projektkopien
(`Tab_BHKW`) haben keinen eigenen Pflegeweg für den Wirkungsgrad; sie tragen, was der
Katalog beim Übernehmen hatte.

**Nicht angefasst:** `Katalogfeld.WirkungsgradAlsFaktor` (teilt für die ANZEIGE der
Katalogliste durch 100, wenn ein Wert über 2 steht) bleibt stehen — sie greift nach dem
Schritt auf keine BHKW-Zeile mehr, trägt aber weiterhin den Heizkessel und einen Altbestand,
der noch nicht migriert ist.

## 5 Was der Schritt nicht tut

- **Er fasst den Rechenweg nicht an.** `SimulationBHKW` steht unverändert; die Formel war
  richtig, die Daten waren es nicht.
- Er rechnet **keine Zeile mit einem Wert bis 1** um.
- Er **erfindet keinen Wert**, wo `Pel` oder `Ptherm` fehlt.
- Er rührt `Tab_BHKW`-Zeilen nicht an, die zu keinem Projekt gehören — die gibt es seit
  Schritt 96 nicht mehr.

## 6 Nachweise

**Neue Fälle** `EPOS.Kern.Tests/BhkwWirkungsgradFaktorTests` (9): Zielstand und Bandgrenzen;
die Rechnung 29,5 → 0,9216 aus der Herleitung; Umrechnung, Ausweisung und Wiederholbarkeit
an drei synthetischen Zeilen (Prozentwert im Band, Brennwertfaktor außerhalb, Satz ohne
`Pel`); die Arbeitskopie ohne offene Zeile; kein Wert über der Pflegegrenze; die
Bestandsaufnahme; die Projektkopien des XRGI 15 mit 0,9216; **A‑BW1‑2** — 1 016
Volllaststunden ergeben 49,9 MWh/a, gerechnet mit der einen Zeile des Laufs und dem Wert aus
dem Katalogsatz der Arbeitskopie, dazu die Gegenprobe 1,56 MWh/a mit dem Prozentwert;
**A‑BW1‑3** — der Katalogweg weist 29,5 ab und lässt 0,92 durch.

**Neue Fälle** `EPOS.UI.Tests/Dialoge/BhkwKatalogDialogTests` (5): Der Dialog lehnt 29,5
benannt ab („Faktor", Feldname) und schreibt nicht; 1,05 geht durch, 1,06 und 0 nicht.

**`KlimaSzenarioTests`** prüfte den Zielstand auf **genau** 97 und wurde auf „mindestens 97"
gestellt — die Nummer des Schrittes bleibt 97, der Zielstand wandert weiter.

**Gate beide Kulturen grün:** EPOS.Kern 3 757, EPOS.UI 4 798, KiKern 499, SpeicherEngine 378,
SpeicherPlanung 27 (1 übersprungen). Windows-Schale mit
`-p:EnableWindowsTargeting=true` **0 Fehler**. SQL-Dialekt-Prüfer **0 Fundstellen** (1 540
Texte). ResourceDesigner wiederholbar. Schemawerkzeug-Trockenlauf nach der Migration:
**Stand 98, 0 Spalten offen, 0 Zeilen umzurechnen, 0 über der Pflegegrenze**.

**Referenzbasis R10** — Einzelheiten, Abweichungstabelle und Gegenprobe im Abschnitt
„Aktuelle Basis" von [`Referenzlaeufe/LIESMICH.md`](../../../../Referenzlaeufe/LIESMICH.md).
Kurz: 13 Abweichungen von 3 882 737 Werten, alle am BHKW, alle in den Projekten **1018** und
**1030**; elf Projekte byte-gleich, alle 357 Vektordateien byte-gleich, Determinismus 13/13.
Referenzlauf der fünf CI-Projekte gegen R10: **GESAMT PASS** und byte-gleich.

## 7 Offene Punkte

- **Die Bestandsdatenbank des Anwenders** rechnet ab dem nächsten Programmstart anders:
  Jedes Projekt mit einem betroffenen BHKW bekommt einen um bis zu Faktor 32 höheren
  Brennstoff samt Emissionen und Brennstoffkosten. Das ist die Berichtigung, die der
  Entscheid verlangt — sie sollte dem Anwender trotzdem angekündigt werden, und der
  Migrationsbericht (`migration_protokoll.txt`) nennt die Zahlen je Tabelle.
- **Eine eigene Wikiseite zum Gerätekatalog oder zum BHKW gibt es nicht** — die zwölf
  Repo-Quellen unter `Projekte/Wiki/` beschreiben Simulation, Ergebnisse, Kosten,
  Wirtschaftlichkeit, Speicher, Photovoltaik, Emissionen, Klimadaten, Varianten,
  Projekttransfer und den Hilfe-Assistenten, aber keine Katalogpflege. Der Satz „Der
  Gesamtwirkungsgrad eines BHKW wird als Faktor zwischen 0 und 1 erfasst" hat damit keinen
  Ort; eine Seite „Programm Dokumentation/Gerätekataloge" wäre ein eigener Auftrag.
- **`AbweichungsErmittler`** führt `Tab_BHKW.Wirkungsgrad` im Variantenvergleich mit der
  Einheit `%` (`Allgemein/Bericht/AbweichungsErmittler.cs`). Das war schon vor dem Schritt
  falsch und ist es danach sichtbar: Der Vergleich zeigt jetzt „0,92 %" statt „0,92". Eine
  Zeile Einheitentext — nicht Teil dieses Auftrags, damit die Messung des Schrittes sauber
  bleibt.

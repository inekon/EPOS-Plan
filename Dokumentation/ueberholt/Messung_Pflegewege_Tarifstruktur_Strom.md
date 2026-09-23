# Pflegewege der Tarifstruktur Strom — Deckungsmessung

**Stand 15.09.2026** · Gegenstand: `EPOS.UI/Dialoge/Wirtschaftlichkeit/TarifstrukturDialog.razor`,
Tabelle `Tab_ProjektTarif` · Anlass: der Wunsch, den Einstieg „Strombezug…" aus der
Wirtschaftlichkeitsseite zu entfernen, weil dieselben Größen unter *Verwaltung →
Energiekosten* gepflegt würden.

Dieses Papier misst, **welche Größe der Sicht „Strombezug" in den Rechenweg geht und wo
sie sonst noch gepflegt werden kann.** Abschnitt 5 nennt, was daraus geworden ist.

**Ergebnis in einem Satz:** Zwei Feldgruppen — die **Bezugspreise des Zonenmodells** und
die **zweistufige Leistungspreis-Staffel** — gehen in den Rechenweg und sind ausschließlich
über die Sicht „Strombezug" pflegbar. Fällt der Einstieg, fällt ihr einziger Pflegeweg.

---

## 1. Ein Tarifsatz, vier Sichten, drei Wirte

Es gilt **ein** Tarifsatz je Stammprojekt (`Tab_ProjektTarif`, eine Zeile je `ID_Projekt`).
Die `TarifSicht` bestimmt nur, welche Blöcke der Dialog baut — und damit auch, welche Werte
er beim Speichern überschreibt (`TarifstrukturDialog.razor:463-503`).

| Sicht | Wirt | erreichbar |
|---|---|---|
| `Komplett` | `WindowsFormsApplication1/Views/Wirtschaftlichkeit/TarifstrukturHuelle.cs:42` | **nein** — die zweistellige Überladung `Oeffnen(besitzer, idStamm)` hat keinen Aufrufer im Bestand; es gibt auch keinen Menüeintrag (`EPOS.UI/Bausteine/Menuetabelle.cs`) |
| `Strombezug` | `WindowsFormsApplication1/Views/Wirtschaftlichkeit/WirtschaftlichkeitSeiteGaben.cs` (Knopf „Strombezug…", `EPOS.UI/Seiten/Berichte/WirtschaftlichkeitSeite.razor:216-220`) **und** `WindowsFormsApplication1/Views/Wirtschaftlichkeit/BhkwWirtschaftlichkeitHuelle.cs:209-210` (Sprungknopf im BHKW-Dialog) | ja, solange der Tarifsatz des Projekts **aktiv** ist (`stand.MitStrombezug = tarifAktiv`) |
| `Photovoltaik` | `WindowsFormsApplication1/Views/Wirtschaftlichkeit/PhotovoltaikVerguetungHuelle.cs:59` | ja, nur mit Photovoltaik in der Vergleichsgruppe |
| `Bhkw` | `WindowsFormsApplication1/Views/Wirtschaftlichkeit/BhkwWirtschaftlichkeitHuelle.cs:209-210` | ja, nur mit BHKW in der Vergleichsgruppe |

**Der Dialog ist der einzige Schreibweg der Tabelle.** `WirtschaftlichkeitCtrl.SpeichereTarif`
(`EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:1288`) hat genau einen
Aufrufer: den Rückruf, den `TarifstrukturHuelle.cs:96` dem Dialog mitgibt. Kein anderer
Dialog, kein Import und keine KI-Aktion schreibt `Tab_ProjektTarif`
(`EPOS.Kern/Allgemein/KI/Aktionen/KiAktionenWirtschaft.cs:102` liest nur).

---

## 2. Feldkarte der Sicht „Strombezug"

Gebaut werden in dieser Sicht: der Kopfblock, die Zeitzonen, die Bezugspreise des
Zonenmodells, die Leistungspreis-Staffel und der Rollenblock *Bezug*
(`TarifstrukturDialog.razor:273-277`).

### 2.1 Kopfblock und Zeitzonen — in **jeder** Sicht gebaut

| Feld | geht in den Rechenweg? | sonst pflegbar wo? |
|---|---|---|
| Aktiv (`:49`) | ja — `WirtschaftlichkeitCtrl.cs:1801` (Rollenpfad) und `:1809` (Zonenpfad) | Sicht `Photovoltaik` (`PhotovoltaikVerguetungHuelle.cs:59`), Sicht `Bhkw` (`BhkwWirtschaftlichkeitHuelle.cs:209-210`) |
| Modell / Modus (`:51`) | ja — `TarifParameter.RollenModus`, ausgewertet `WirtschaftlichkeitCtrl.cs:1801` | dieselben zwei Sichten |
| Gültig ab (`:54`) | nein — reiner Ausweis (`WirtschaftlichkeitDaten.cs:693-695`) | dieselben zwei Sichten |
| Winter von / bis (`:70-71`) | ja — `StromMatrix.cs:202` über `IstWinter` | dieselben zwei Sichten |
| HT von / bis (`:73-76`) | ja — `StromMatrix.cs:206` (Zonenmodell; im Rollenmodell gesperrt) | dieselben zwei Sichten |

### 2.2 Bezugspreise des Zonenmodells — **nur** `Komplett` und `Strombezug`

| Feld | geht in den Rechenweg? | sonst pflegbar wo? |
|---|---|---|
| Winter HT (`:91`) → `PreisBezugWinterHT` | ja — `StromMatrix.cs:225` (`Bezugskosten`), Weiche `WirtschaftlichkeitCtrl.cs:1811`, Ersatz der Flat-Kosten `:1818` | **nirgends** |
| Winter NT (`:93`) → `PreisBezugWinterNT` | ja — `StromMatrix.cs:226` | **nirgends** |
| Sommer HT (`:95`) → `PreisBezugSommerHT` | ja — `StromMatrix.cs:227` | **nirgends** |
| Sommer NT (`:97`) → `PreisBezugSommerNT` | ja — `StromMatrix.cs:228` | **nirgends** |

### 2.3 Leistungspreis-Staffel — **nur** `Komplett` und `Strombezug`

| Feld | geht in den Rechenweg? | sonst pflegbar wo? |
|---|---|---|
| Staffelgrenze (`:123`) → `StaffelGrenzeKW` | ja — `StromMatrix.cs:273-276` (`Leistungspreis`, Teil der Bezugskosten) und `SpeicherAuslegungVorgabenCtrl.cs:309-311` (Leistungspreisangebot der Speicherauslegung) | **nirgends** |
| Staffelpreis 1 (`:125`) → `StaffelPreis1EurKW` | ja — dieselben zwei Stellen | **nirgends** |
| Staffelpreis 2 (`:127`) → `StaffelPreis2EurKW` | ja — dieselben zwei Stellen | **nirgends** |

### 2.4 Rollenblock *Bezug* — in jeder Sicht außer `Photovoltaik`

| Feld | geht in den Rechenweg? | sonst pflegbar wo? |
|---|---|---|
| Arbeitspreis (`:357`) | ja — `StromTarifRechner.cs:236`, gespeist aus `WirtschaftlichkeitCtrl.cs:2040-2041` | Sicht `Bhkw` (`BhkwWirtschaftlichkeitHuelle.cs:209-210`) |
| Grundpreis (`:360`) | ja — `StromTarifRechner.cs:238` | Sicht `Bhkw` |
| Leistungsmodell (`:363`) | ja — `StromTarifRechner.cs:182-197` | Sicht `Bhkw` |
| Monatspreis (`:367`) | ja — `StromTarifRechner.cs:186` und `:197` | Sicht `Bhkw` |
| Stufe 1–4: Obergrenze, Sommer, Winter (`:374-382`) | ja — `StromTarifRechner.cs:217-223` | Sicht `Bhkw` |

---

## 3. Die Lücke

Die Zeilen mit **nirgends** sind der Kern: sieben Werte — vier Zonen-Bezugspreise und drei
Staffelwerte — gehen in die Bezugskosten des Zonenmodells ein
(`StromMatrix.Bezugskosten`, aufgerufen in `WirtschaftlichkeitCtrl.cs:1818`) und haben
außerhalb der Sicht „Strombezug" keinen Pflegeweg. Das Zonenmodell ist dabei die
**Vorbelegung** jedes Tarifsatzes (`TarifParameter.Modus = DbWerte.TARIF_MODUS_ZONEN`,
`WirtschaftlichkeitDaten.cs:691`), nicht der Ausnahmefall.

Der Knopf „Strombezug…" erscheint, **solange der Tarifsatz des Projekts aktiv ist**
(`WirtschaftlichkeitSeiteGaben.cs`, `stand.MitStrombezug = tarifAktiv`). Damit bleibt der
Pflegeweg genau dort offen, wo die sieben Werte rechnen — und er verschwindet, wo sie es
nicht tun. Die Kehrseite: In einer Gruppe ohne BHKW und ohne Photovoltaik — dem typischen
Wärmepumpenprojekt — gibt es bei **inaktivem** Tarifsatz keinen Zugang mehr zum Kopfblock
und damit keinen Weg, ihn einzuschalten; die Sichten `Bhkw` und `Photovoltaik` sind dort
nicht erreichbar, und `Komplett` hat keinen Wirt.

---

## 4. Warum die Kostenverwaltung die Lücke nicht schließt

Die Trägerkarte der Energieträgerverwaltung (`EPOS.UI/Dialoge/Kosten/EnergietraegerEinstellungen.razor:54-84`)
pflegt Preisbasis, **Arbeitspreis**, **Leistungspreis**, Grundpreis, Heiz- und Brennwert des
Trägers; `StromAufschlaege.razor` pflegt die Aufschläge auf den Arbeitspreis,
`LeistungspreisReiheDialog.razor` die zwölf saisonalen Monatssätze. Das sind **andere
Größen auf anderen Spalten**:

- Der Tarifsatz ist die Alternative zu diesen Flat-Preisen, nicht ihre Wiederholung: Ist er
  inaktiv, gelten die „Flat-Preise der Kostenmaske" (`WirtschaftlichkeitDaten.cs:655`); ist
  er aktiv und gepflegt, **ersetzt** er sie samt Arbeits-, Grund- und Leistungspreis
  (`WirtschaftlichkeitCtrl.cs:1805-1820`).
- Die Speicherauslegung bietet beide Quellen nebeneinander an und benennt den Unterschied
  ausdrücklich: „die Tarifstruktur den der Wirtschaftlichkeitsrechnung, der Energieträger
  den des Kostenmoduls" (`EPOS.Kern/Controller/SpeicherAuslegungVorgabenCtrl.cs:52-64`,
  Quellen im Einzelnen `:190-215`).
- Eine Zeitzonenstruktur (Winter/Sommer × HT/NT) und eine zweistufige Leistungspreis-Staffel
  auf die Bezugsspitze haben in der Trägerkarte kein Gegenstück.

---

## 5. Was daraus geworden ist

Der Einstieg „Strombezug…" **bleibt** — Weg 1 der drei gemessenen Möglichkeiten, weil die
sieben ungedeckten Werte sonst ohne Pflegeweg weiterrechneten. Er hängt allein am
Tarifsatz: aktiv → Knopf, inaktiv → kein Knopf. Die Erzeugerlage der Gruppe ankert ihn
nicht mehr.

Die beiden anderen Wege stehen weiterhin offen, falls die Lücke einmal geschlossen werden
soll:

1. **Zonen-Bezugspreise und Staffel in die Kostenverwaltung verlegen** — neue Felder auf der
   Trägerkarte des Stromträgers oder ein eigener Reiter, der denselben Tarifsatz schreibt.
   Danach könnte der Einstieg ohne Lücke fallen.
2. **Zonenmodell abkündigen** und alle Projekte auf das Rollenmodell heben. Dann verlieren
   die sieben Werte ihren Rechenweg, und der Rollenblock *Bezug* bleibt über die Sicht
   `Bhkw` erreichbar — allerdings nur mit BHKW in der Gruppe. Das ist ein Eingriff in den
   Rechenweg mit neuer Referenzbasis, kein Aufräumen einer Maske.

Der zweite Knopf gleichen Namens im BHKW-Dialog (`BhkwWirtschaftlichkeitHuelle.cs:209-210`)
ist davon unabhängig: Er öffnet dieselbe Sicht auf dieselben Werte und ist damit der
zweite von zweien.

**Offen bleibt der Kopfblock.** Wer den Tarifsatz eines Wärmepumpenprojekts ohne BHKW und
ohne Photovoltaik erst **einschalten** will, findet dafür keinen Weg mehr: Der Schalter
`Aktiv` steht im Kopfblock, den jede Sicht baut — aber keine Sicht ist dort erreichbar,
solange der Satz inaktiv ist.

---

## 6. Nebenbefunde

- **Die Sicht `Komplett` hat keinen Wirt.** Die zweistellige Überladung
  `TarifstrukturHuelle.Oeffnen(besitzer, idStamm)` (`:40-43`) wird nirgends gerufen. Sie ist
  der Rest des Sammel-Einstiegs „Tarifstruktur…", der mit Ä16 von der
  Wirtschaftlichkeitsseite genommen wurde (`WirtschaftlichkeitSeite.razor:26-29`), und
  zugleich die einzige Sicht, die alle Blöcke zeigt.
- **Die Fußleiste der Seite** trägt „Photovoltaik…", „BHKW-Wirtschaftlichkeit…",
  „Strombezug…", „Parameter…", „Verlauf…" und „Berechnen"
  (`WirtschaftlichkeitSeite.razor:205-232`); die Wiki-Quelle
  `Projekte/Wiki/Programm Dokumentation - Wirtschaftlichkeit.wiki` nennt „Strombezug…"
  samt seiner Bedingung.

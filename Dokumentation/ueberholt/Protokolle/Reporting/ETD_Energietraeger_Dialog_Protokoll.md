# ET-D · Energieträger-Dialog nach dem Mockup — Protokoll

**Anlass.** Anwenderwunsch 18.09.2026: „Der Dialog im Mockup ist sehr übersichtlich und besser als
der vorhandene Dialog Energieträgerverwaltung. Der Dialog Energieträger soll an der bisherigen
Stelle bleiben (nicht innerhalb der Wirtschaftlichkeit) — und verbessert werden wie im Mockup."
Vorlage: `Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/Mockups/Dialog_Formel_Zahlenprobe.html`,
Anker `#energie`.

**Entscheide.** `ET-D-1` (a) Preisbestandteile in der Abrechnungseinheit, `ET-D-2` (a)
Emissionsblock = Arten dieses Trägers ohne Primärenergiefaktor, `ET-D-3` (a) Preisbasis nur
Abrechnungseinheit/kWh mit Faktor = Heizwert, Umrechnungsregeln als zugeklappter Prüfblock.
Mitgenommen: Befund `UR-1`.

---

## 1 Der Befund UR-1

Das Anwenderfoto zeigte drei Zahlen über denselben Preis: **0,07 €/kWh eingegeben**,
**0,04 €/Nm³ gespeichert**, Formelzeile **0,0033 €/kWh**.

Die Ursache stand in `EnergietraegerPreisCtrl.Preisbasen`. Die Klappliste „Preisbasis" entstand aus
den **Zieleinheiten der Umrechnungsregeln**, und **der `factor` dieser Regeln rechnete den
Arbeitspreis um**. Ein Regelfaktor ist aber kein Heizwert: In der Testdatenbank trägt die Regel 67
(`Nm³ → kWh`, Brennstoff 3) den Faktor **0,5**, während H_i bei **10,5 kWh/Nm³** steht. Der Fehler
war zudem als Zusicherung festgeschrieben — `EnergietraegerHuelleTests` maß den Wechsel gegen
„Regel 67 Faktor 0,5".

Eine zweite, stillere Seite desselben Befunds: Die kWh-Basis gab es **nur für Brennstoff 3** — er
ist der einzige der Testdatenbank mit einer Regel nach kWh. Alle anderen Träger konnten ihren
Arbeitspreis gar nicht in €/kWh eingeben.

**Die Regel dagegen:** Eine Energiemenge wird über ihren **Heizwert** in Kilowattstunden getragen,
nie über eine Einheitenregel.

### Was jetzt gilt

`Preisbasen(abrechnungseinheit, aktive Regeln, heizwert)` liefert **genau zwei** Einträge:

| # | Einheit | Faktor | Regel (nur für `ID_Umrechnung`) |
|---|---|---|---|
| 0 | Abrechnungseinheit | 1 | Identitätsregel `from = to`, sonst erste Regel auf diese Einheit |
| 1 | kWh | **H_i** | Regel `Abrechnungseinheit → kWh`, sofern der Brennstoff eine führt |

Rechnet der Träger ohnehin in kWh ab (Strom, Fernwärme) oder führt er keinen Heizwert, bleibt der
eine Eintrag. `Umrechnungen` liest seither nur noch **aktive** Regeln (`aktiv = 1`) — eine
abgeschaltete Regel ist keine Regel.

`EnergietraegerHuelle.Faktor()` nimmt den Heizwert, **der gerade im Feld steht**: Wer H_i ändert,
ändert damit die Umrechnung des Arbeitspreises. Dafür setzt `Nachziehen()` jetzt erst `_baseHi`
und `_baseHs` und erst danach `_baseWork` — vorher fiel der Basispreis aus dem alten Heizwert.

### Zahlen

| | vorher (Regelfaktor 0,5) | jetzt (H_i 10,5) |
|---|---|---|
| 0,07 €/kWh eingegeben → gespeichert | 0,035 €/Nm³ | **0,735 €/Nm³** |
| 0,735 €/Nm³ → angezeigt bei Basis kWh | 1,47 €/kWh | **0,07 €/kWh** |

### Bestandsprojekte

**Es wird nichts stillschweigend umgerechnet.** Ein Projekt, dessen `custom_price_work` mit dem
falschen Faktor entstanden ist, trägt weiterhin die gespeicherte Zahl. Erkennbar ist der Fall an
der **Formelzeile der Trägerkarte**: Sie nennt den Preis je kWh („0,50 €/Nm³ ÷ 10,50 kWh/Nm³ =
0,0476 €/kWh"). Steht dort ein Preis, der nicht zum Vertrag passt, gibt der Anwender den
**Arbeitspreis neu ein** — in der Abrechnungseinheit oder, mit der Preisbasis kWh, je
Kilowattstunde. Ein Wandlungsschritt wäre eine Behauptung darüber, welche der drei Zahlen der
Anwender gemeint hat.

**Offener Punkt `UR-1 Schritt 2`.** `energy_project_settings.ID_Umrechnung` merkt die Preisbasis
als **Regelkennung**. Führt ein Brennstoff keine Regel nach kWh, wird beim Speichern −1 abgelegt,
und beim nächsten Öffnen steht die Abrechnungseinheit da. Verloren geht dabei nichts — gespeichert
ist ohnehin der Basiswert je Abrechnungseinheit —, wohl aber die Eingabegewohnheit. Eine saubere
Lösung wäre eine eigene, kleine Spalte (Kartenzustand statt Regelkennung); sie braucht einen
Schemaschritt und steht nicht in diesem Auftrag.

---

## 2 Die Karte in Blöcken

Reiter verstecken die Hälfte der Karte: Wer den Arbeitspreis pflegte, sah die Emissionen nicht.
Seit ET-D steht alles auf einmal da.

| Block | Inhalt | Lage |
|---|---|---|
| **A** Preis und Heizwert | Arbeitspreis (Einheit folgt der Preisbasis), Grundpreis, Leistungspreis samt Modus, H_i, H_s; Effektivzeile, Herleitung „→ … €/kWh · Umrechnungsfaktor H_s/H_i = …", Formelzeile; saisonale Sätze, Katalogübernahme, Preislücken | links |
| **B** Preisbestandteile bzw. Strompreis Details | vier bzw. neun Anteile, Herleitungen, Summenzeile, Schnellwahl | rechts, Umbruch < 900 px |
| **C** Emissionen | Bilanzierungsmethode, Arten dieses Trägers, Summe, Fußnote, Katalogknopf | volle Breite |
| **D** Einheiten und Umrechnung | Preisbasis, Basiseinheit, Regelblock, Verstoßbanner | volle Breite, **aufklappbar, Vorgabe zu** |
| | Preishistorie | volle Breite |

Neu im Stilblatt: `.epos-blockspalten` / `.epos-blockspalte` (Flex, Umbruch über
`--epos-zweispalten-umbruch` = 900 px, `min-width: 0` an der Spalte), dazu
`.epos-preiszeile-herleitung` und drei Klassen der Schnellwahl-Überlagerung.

Die **Effektivzeile** („1 Nm³ = 10,50 kWh (H_i) / 11,60 kWh (H_s)") ist von den Regeln zu H_i und
H_s gewandert: Sie fällt aus ihnen, nicht aus dem Regelblock.

Der **Dialogkopf** nennt den Träger („Energieträger — Erdgas E"), die Kontextzeile Projekt und
Preisstellung („Musterprojekt · Preise netto", im Katalog „Katalog · Preise netto"). Die
Rückfragen behalten den Maskentitel — ein Titel, der mit der Auswahl wechselt, taugt nicht als
Überschrift einer Ja/Nein-Frage.

---

## 3 Die Anzeigekante der Preisbestandteile

**Gerechnet wird in ct/kWh, angezeigt in der Abrechnungseinheit.** Kern (`Preisanteile`,
`BrennstoffBestandteilCtrl`), Speicherweg und Kohärenzprüfung bleiben unverändert; die Einheit
wechselt **genau einmal**, in der Komponente:

```
€ je Abrechnungseinheit = ct/kWh ÷ 100 × H_i     EnergietraegerPreiskarte.AnteilJeEinheit
ct/kWh                  = € je Einheit × 100 ÷ H_i                        …AnteilCtKwh
```

Ohne Heizwert bleibt ct/kWh, und eine leise Zeile nennt den Grund. Probezahl: 0,6076 ct/kWh sind
bei H_i 10,5 kWh/m³ genau **0,063798 €/m³** (angezeigt 0,0638). Hin und zurück verliert keine
Stelle und ist nach der ersten Runde ein Festpunkt.

**Je Zeile eine Herleitung:**

| Zeile | Herleitung | Quelle |
|---|---|---|
| Energiesteuer | „5,50 €/MWh (H_s)" | Gesetzeskatalog, § 2 EnergieStG des Bilanzjahres |
| CO₂-Bestandteil (BEHG) | „65 €/t × 2,109 kg/m³" | CO₂-Preis × CO₂-Masse je Einheit |
| Netz- und Messentgelt | — | |
| Beschaffung und Vertrieb | Rest, als `Vorschlagszeile` am Feld | Arbeitspreis − übrige aktive Anteile |

Die CO₂-Masse rechnet der Kern (`EnergietraegerPreiskarte.Co2MasseJeEinheit`): 200,9 g/kWh ×
10,5 kWh/m³ ÷ 1000 = **2,10945 kg/m³**. Der Umzug war nicht Geschmack — in der Hülle stand damit
ein Faktor 1000 auf einer Zeile, die der Wächter `EinheitenWacheTests` zu Recht meldete.

**Eine Summenzeile statt dreier.** Die `Kohaerenzzeile` trägt jetzt die Summe **und** die Aussage,
ob sie zum Arbeitspreis passt — Toleranz **0,0001 €/kWh** (0,01 ct/kWh). Darüber steht sie auf „≠"
und nennt den Abstand. Bis ET-D stand sie ausnahmslos auf „✓": Sie zeigte den Arbeitspreis, sie
prüfte ihn nicht.

**Ein Schnellwahlknopf statt vier.** „Schnellwahl aus Katalog…" öffnet eine `Ueberlagerung` im
selben Fenster; darin steht je Satz Herkunft, Jahr, Katalogwert **und** der Satz in der
Abrechnungseinheit — vor der Übernahme, nicht danach. Die vier Direktknöpfe trugen ihre Herkunft
nur im `title`.

---

## 4 Emissionsblock

Die `Optionsgruppe` „CO₂-Berechnung:" ist eine Klappliste **Bilanzierungsmethode** geworden — eine
Wahl aus zweien kostete zwei Zeilen für dieselbe Aussage. Einträge: „CO₂ direkt — reale Bilanz,
heizwertbezogen" und „CO₂-Äquivalent (GWP₁₀₀)"; Quelle und Schreibweg sind unverändert
(`Tab_Projekt.Emission_Berechnungsmodus`). Im **Katalogkontext** ist sie gesperrt, und der Grund
steht darunter: Die Methode ist eine Projektvorgabe.

Die Tabelle zeigt weiter die Arten **dieses** Trägers; die Spalte „Herkunft" heißt „Quelle". Die
Fußnote sagt, dass nur die im Projekt gewählte Größe gezeigt wird und SO₂/NO_x weitergeführt, aber
nicht gezeigt werden. Entscheide D-1 und E-1 bleiben unberührt: kein stiller Rückfall, der Tooltip
benennt den Fall. **Kein Primärenergiefaktor, keine Trägerübersicht.**

---

## 5 Nachweis

**Neue und umgestellte Prüffälle**

| Ort | Fall |
|---|---|
| `PreisbasenTests` | neu gefasst (16 Fälle): kWh-Basis trägt H_i; Anwenderfall 0,07 ↔ 0,735; Regel stellt nur die `ID_Umrechnung`; ohne kWh-Regel bleibt kWh wählbar; Zieleinheiten sind keine Preisbasen mehr; ohne Heizwert nur die Abrechnungseinheit; kWh-Abrechner bekommt kWh einmal; kein Träger der Testdatenbank führt mehr als zwei; nur aktive Regeln |
| `EnergietraegerPreiskarteTests` | Anzeigekante hin und zurück (0,6076 ct/kWh ↔ 0,063798 €/m³), Festpunkt, ohne Heizwert, Anzeigeeinheit, H_s/H_i = 1,1048, CO₂-Masse 2,10945 kg/Nm³, Preisbasis kWh gegen H_i |
| `EnergietraegerHuelleTests` | **umgestellt:** „Preisbasis rechnet nur den Arbeitspreis um" (maß gegen Regel 67 Faktor 0,5) → „… mit dem Heizwert um"; **neu:** Bestandteile in der Abrechnungseinheit, Kohärenzzeile meldet Abweichung und fällt auf „deckungsgleich" zurück, BEHG-Herleitung, Herleitungszeile mit H_s/H_i, Bilanzierungsmethode im Katalog nur lesbar; Kontextzeile (vier Fälle) auf „… · Preise netto" |
| `EnergietraegerDialogTests` | **umgestellt:** „zwei Reiter" → „vier Blöcke"; **neu:** Block D zu und mit `aria-expanded` auf, Effektivzeile in Block A, Kopf nennt den Träger; Verstoßhinweis und Preisbasis über Block D erreicht |
| `PreisbloeckeTests` | **umgestellt:** vier Schnellwahlknöpfe → einer mit Überlagerung; Summen-/Kohärenzzeile; Blocktitel beide Sprachen; **neu:** Überlagerung nennt Herkunft und Satz in der Abrechnungseinheit, Bestandteile in €/m³, ohne Heizwert ct/kWh, Herleitungszeilen, Rest als Vorschlagszeile, Kohärenz ok und abweichend |

**Gegenproben** (gesetzt, gemessen, zurückgebaut)

| Gegenprobe | Wirkung |
|---|---|
| Preisbasis wieder aus dem Regelfaktor | 6 Fälle in `PreisbasenTests` rot |
| Anzeigekante ein zweites Mal durch H_i geteilt | 3 Fälle in `EnergietraegerPreiskarteTests` rot |
| Kohärenz konstant „ok" | `Die_Kohaerenzzeile_meldet_eine_Abweichung` rot |

---

## 6 Was nicht angefasst wurde

Kein Schemaschritt (`SchemaStand.Zielversion` bleibt 91), kein Rechenwegwechsel — der Kern rechnet
die Anteile weiter in ct/kWh, der Referenzlauf ist byte-gleich. Keine Vergütungsgruppe (SP-E-5),
keine Trägerübersicht, kein Primärenergiefaktor, kein Block „Aufschläge Netzbezug Strom" (er ist
durch die Zerlegung ersetzt). Die iOS-Hülle ist unberührt; die Energieträgerverwaltung ist dort
ohnehin nicht erreichbar (`IProjektQuelle.BerichteKostenGaben` = null).

---

## 7 Logbuch-Entwurf (Wiki, Version 1.2.0.2)

Zwei Sätze, je eine sichtbare Änderung; die Versionsnummer ist beim Anwender zu bestätigen.

> * Der Dialog Energieträgerverwaltung zeigt die Werte eines Trägers ohne Reiter in vier
>   Blöcken; die Preisbestandteile eines Brennstoffs stehen in seiner Abrechnungseinheit
>   und tragen ihre Herleitung, ein Knopf „Schnellwahl aus Katalog…" öffnet die
>   Katalogsätze mit Herkunft.
> * Die Preisbasis „kWh" rechnet den Arbeitspreis mit dem Heizwert des Trägers um.

Die Wiki-Quelle `Projekte/Wiki/Programm Dokumentation - Kosten.wiki` ist fortgeschrieben
(Abschnitt Energieträgerverwaltung: Trägerkarte, Preisbasis, Einheiten und Umrechnung,
Preisbestandteile, Schnellwahl, Emissionsblock); der Upload ist gebündelt und steht aus.

---

## 8 Bildschirmmaße (Chromium, gemessen)

| Fensterbreite | Blöcke A/B | Kartenbreite | Überlauf |
|---|---|---|---|
| 1400 px | nebeneinander, je 418 px | 852 px | nein |
| 1240 px | nebeneinander, je 418 px | 852 px | nein |
| **1140 px** (Fenstermaß der Windows-Schale) | **nebeneinander**, je 401 px | 817 px | nein |
| 1000 px | untereinander, 677 px | 677 px | nein |
| 760 px | untereinander, 713 px | 713 px | nein |
| 500 px (Chromium-Mindestmaß) | untereinander, 453 px | 453 px | nein, 16 px Rand |

**`FENSTER_BREITE` bleibt bei 1140 × 840** — das Maß trägt die zwei Blockspalten. Der Umbruch
fällt bei einer Kartenbreite unter rund 780 px (zwei Spalten à 380 px Grundbreite plus Abstand);
er liegt damit innerhalb der 900-px-Schwelle des Hauses, ohne sie zu unterschreiten, wo die Karte
noch Platz hat.

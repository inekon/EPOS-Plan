# Offene Entscheide der Gebäudesimulation — Register mit Erläuterung

**Stand 25.09.2026, nach den Entscheiden E16–E38 sowie der Prüfung vom 17.09.2026; mit dem Abschluss
von G3 (25.09.2026) die Vermerke unter A1, A14 und F-M1. E39 und E40 (Konzept N1.44, N1.45) berühren
keinen Registerpunkt. E48 (26.09.2026, Konzept N1.53) ist unter D2 und D17 vermerkt.**

**Zweck.** Dieses Register ist die **eine Stelle, an der jede offene Frage der Gebäudesimulation
mit ihrer Erläuterung steht** — Frage, Hintergrund, Optionen, Empfehlung des jeweiligen Papiers,
Folge bei Nichtentscheid und der Zeitpunkt, bis zu dem sie beantwortet sein muss. Anlass ist die
Bitte des Anwenders vom 16.09.2026: „erläutere alle offenen Punkte".

**Was dieses Register nicht ist.** Es entscheidet nichts und hält nichts fest. **Entschieden wird
weiterhin im Konzept-Nachtrag und in der Statusdatei**: der ausführliche Entscheid als Nachtrag
N1.x in [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md),
die Zeile je Entscheid in [`Status_Gebaeudesimulation_VDI6007.md`](Status_Gebaeudesimulation_VDI6007.md),
der Architekturentscheid im zugehörigen ADR. Dieses Register **zeigt nur auf sie** und wird beim
Entscheid um den betroffenen Punkt gekürzt; die mit **E27** und **E28** (22.09.2026), die mit
**E31** und **E33** (23.09.2026) und die mit **E38** (24.09.2026) entschiedenen Punkte stehen ausnahmsweise mit
Entscheidvermerk weiter in ihren Kapiteln (Kapitel 9).

**Lesehinweis.**

- **Kapitel 0** nennt die 36 Punkte, die **vor dem Start von G0/G1 oder vor der Beauftragung einer
  Stufe** fällig waren — alles, was Schema, Referenzbasis, Datenmodell oder eine Fremdbibliothek
  unwiderruflich festlegt —, seit **E27** (22.09.2026) mit ihrem Entscheid, dazu die 8 Punkte, die
  noch offen sind, nach Fälligkeit; seit **E28** ist vor G0, GB und G1 keiner mehr offen, seit **E31**
  auch vor KU1 keiner, seit **E33** auch vor KU2 keiner, seit **E38** auch vor G4 keiner. Wer wenig
  Zeit hat, liest nur dieses Kapitel.
- **Kapitel 1 bis 6** führen je Papier alle Punkte einzeln aus (Kapitel 1 trägt Q24, Q25 und Q26),
  immer im selben Aufbau; die mit E27, E28, E31, E33 oder E38 entschiedenen tragen unter der Überschrift
  den Vermerk „**Entschieden: E27 (22.09.2026, Konzept N1.32)**", „**Entschieden: E28 (22.09.2026,
  Konzept N1.33)**", „**Entschieden: E31 (23.09.2026, Konzept N1.36)**", „**Entschieden: E33
  (23.09.2026, Konzept N1.38)**" bzw. „**Entschieden: E38 (24.09.2026, Konzept N1.43)**"; **Kapitel 7** hält
  den Stand der zwölf Fragen H1 bis H12 der Anlagenkopplung fest, die mit E24 entschieden sind:
  Frage, Hintergrund, Optionen, Empfehlung des Papiers, Folge bei Nichtentscheid, Fällig vor.
- **Kapitel 8** nennt die technischen Festlegungen, denen nur zu widersprechen ist — darunter in
  8.4 die Festlegungen F-Ü1 bis F-D1 aus der Prüfung vom 17.09.2026 (Widerspruch bis zur
  Beauftragung von G1) —, **Kapitel 9** den Weg, auf dem ein Entscheid festgehalten wird.
- Zahlen und Empfehlungen stehen im Wortlaut der Papiere. Wo zwei Papiere zu derselben Frage
  Verschiedenes sagen, sind **beide** genannt.
- Die Nummern sind die der Papiere und werden nicht umnummeriert: **Q** Konzept, **U**
  Umsetzungskonzept, **M** Mehrzonenmodell, **D** Datenaustausch, **A** Softwarearchitektur, **H** Anlagenkopplung,
  **K** Kühlung.

**Umfang in Zahlen.** **5 offene Punkte** (Stand E49, 26.09.2026), alle im Mehrzonenkonzept
(M7, M8, M11–M13); Konzept, Umsetzungskonzept, Datenaustauschkonzept, Softwarearchitektur und
Kühlkonzept haben keinen offenen Punkt mehr. **E49** (26.09.2026, Konzept N1.55) hat die vor G6b
fälligen **M3, M5 und M6** nach Empfehlung entschieden.
**E38** (24.09.2026) hat die drei vor G4 fälligen Punkte **U13, U14 und U15** nach Empfehlung
entschieden — für beide Importwege, gbXML (G4c) und IFC (G4a) —, für G4a genau einen iOS-Lauf
festgelegt, nur nach ausdrücklicher Rückfrage bei der Abnahme, und die Stufe G4 beauftragt (zuerst
G4c, dann G4a); vor G4 ist damit kein Anwenderentscheid mehr offen.
**E33** (23.09.2026) hat die vier vor KU2 fälligen Kühlpunkte **K8, K21 und K23** nach Empfehlung
und **K9 abweichend von der Empfehlung** entschieden; vor KU2 ist damit kein Anwenderentscheid mehr
offen. **E34** (23.09.2026) ergänzt K9 um die Regel für einen abweichenden Kühlträger (anteilig am
Netzbezug oder eigener Zähler, je Anlage wählbar) und lässt die Zählung unverändert; **E35**
(24.09.2026) ergänzt E34: Ein eigener Zähler trägt auch Grund- und Leistungspreis seines Kühlträgers —
die Zählung bleibt.
**E31** (23.09.2026) hat die fünf vor KU1 fälligen Kühlpunkte **K4, K5, K6, K7 und K12** nach
Empfehlung entschieden; vor KU1 ist damit kein Anwenderentscheid mehr offen.
**E27** hat 44 der bisher 66 Punkte entschieden — 3 im Konzept (Q24, Q25, Q26), 9 im
Umsetzungskonzept (U1, U3, U5–U8, U10, U12, U17), 4 im Mehrzonenkonzept (M2, M9, M10, M14), 8 im
Datenaustauschkonzept (D1, D2, D4, D5, D6, D11, D16, D17), 15 in der Softwarearchitektur (A1–A6,
A9–A15, A17, A18; A4, A5 und A9 über U1, U3 und U5) und 5 im Kühlkonzept (K10, K11, K19, K22,
K24) — und die Festlegung K2 (8.2) bestätigt; alle bis auf **K10** nach Empfehlung. Drei
entschiedene Punkte tragen eine Folgeaufgabe: **U6** (Endwahl nach der Messung in G1 — mit **E29**
am 23.09.2026 erledigt: Stundenanfang, Konzept N1.34), **K22**
(Prüfung vor KU2 — am 23.09.2026 erledigt, Glossar), **D6** (Gegenüber benennen vor der Stufe über die semantische hinaus).
**E28** (22.09.2026) hat danach **U4** und **U9** nach Empfehlung entschieden; vor G0, GB und G1
ist damit kein Anwenderentscheid mehr offen.
Davor: Die Entscheide E16–E25 vom 16.09.2026 haben Q10, Q11a, H1–H12, U2, U11, U16, M1, M4, A7,
A8, A16, A19 und K1 aus diesem Register genommen und Q26 hinzugefügt; **E26** (17.09.2026) hat Q24
und Q25 wieder geöffnet; die Prüfung vom 17.09.2026 hat U17 hinzugefügt, die Reste D17 (aus D3)
und K24 (aus K18a) als eigene Punkte gezählt und K20 als durch die Umsetzung erledigt gekürzt
(F-K1). ADR-004 und ADR-005 sind angenommen; dazu drei Listen zur Kenntnis und die Festlegungen
F-Ü1 bis F-D1 der Prüfung (8.4).

---

## 0. Die vor dem Start fälligen Punkte — mit E27 entschieden

Kriterium dieser Liste: Der Punkt legt **Schema, Referenzbasis, Datenmodell oder eine
Fremdbibliothek unwiderruflich** fest, oder er ist für den Anwender in der Oberfläche sichtbar und
lässt sich nachträglich nicht stillschweigend ändern. Alles Übrige steht in den Kapiteln 1 bis 7
und darf mit der Stufe entschieden werden, zu der es gehört.

**Stand 22.09.2026: Alle 36 Punkte dieser Liste sind mit E27 entschieden**
([Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.32,
[Statusdatei](Status_Gebaeudesimulation_VDI6007.md) Abschnitt 1) — 35 gezählte Punkte und die
Festlegung K2 (8.2). Bis auf **K10** folgen alle der Empfehlung; bei D1 gilt mangels Präferenz des
Anwenders der Vorschlag des Papiers. Die Liste bleibt in Reihenfolge und Fälligkeit stehen; statt
Frage und Empfehlung nennt sie den Entscheid in einem Satz und, wo eine bleibt, die Folgeaufgabe.
Die Erläuterung steht weiter im jeweiligen Abschnitt der Kapitel 1 bis 6.

| Nr. | Entscheid (E27) in einem Satz | Spätestens vor | Folgeaufgabe |
|---|---|---|---|
| **U8** | Normzahlen lokal und gitignoriert; der Normfallnachweis läuft lokal, die Lücke im Gate steht im Protokoll. | **G0** | — |
| **U5** | Die zwei Gebäudespalten-Schemaschritte werden zu einem verschmolzen (M3, ein Sichtneubau). | **G1** | — |
| **U1** | Ein Schreibweg im Katalogeditor ab G1; „Speichern unter…" bleibt als nicht schließender Zweitknopf. | **G1** | — |
| **U3** | `Platzhalter` am `Zahlenfeld`, rein additiv. | **G1** | — |
| **U7** | Ortszeit-Kalender (Option (a)); die Probe gegen `Tab_Klimadaten.WE` bleibt. | **G1** | — |
| **A15** | Option (a): ein Referenzprojekt mit `Gebaeude_Modell = TAGESBILANZ` bis GA in der jeweils aktuellen Basis; GB-Arbeitskopie nur bis zum Merge G1 + G2; Rückweg-Test nur dieses Projekt, endet mit GA. | **G1 + G2** | — |
| **U6** | Verfahren: in G1 beide Zeitbezüge an der einen Stelle (Klimaklasse, A18) messen, Entscheid vor dem Einfrieren. | **Einfrieren von G1 + G2** | **Endwahl erledigt** (E29, 23.09.2026): Stundenanfang |
| **A18** | Der Klimaweg bleibt eigene Klasse, ausschließlich vom Eingangsbauer gerufen. | **G1** | — |
| **A10** | Der Gebäudedialog zieht mit G1 nach `EPOS.UI.Daten`. | **G1** | — |
| **A12** | Produktausweis in Wiki und Berichtskopf, im Wortlaut von E10. | **G1** (Berichtskopf), G2 (Wiki) | — |
| **K2** | Der Kältekanal führt positive Kältemengen. | **KU1** | — |
| **K10** | **Abweichend von der Empfehlung:** eine Programmeinstellung legt fest, ob **neue** Projekte mit eingeschalteter Kühlung angelegt werden (Vorgabe aus); bestehende Projekte samt Referenzprojekten bleiben aus, bis die je Projekt schaltbare Projekteinstellung ausdrücklich eingeschaltet wird; Umsetzung über `Dienste.Einstellungen`. | **KU1** | — |
| **K11** | Eigener Kühlsollwert und eigene Kühlleistungsgrenze in KU1 nach Empfehlung (a); das Zeitprofil nach deren Wortlaut in KU3. | **KU1** | — |
| **K19** | KU2 bekommt einen eigenen, kleinen Einfrierschritt. | **KU2** | — |
| **K22** | Vor KU2 prüfen, ob die COP-Spalte das Kälteverhältnis führt, und im Glossar festhalten. | **KU2** | **Prüfung erledigt** (23.09.2026): die Spalte führt den EER; Importregel für Kühlblöcke in Heizlage mit KU2 |
| **A11** | Schemaschrittnummern erst bei Beauftragung; verbindlich sind Reihenfolge und Inhalt. | **erste Auslieferung** eines Schemaschritts | — |
| **A1** | Die Kaskade bleibt; Schreibweg vor G3 messen, Rettung an der Löschstelle. | **G3** | — |
| **A14** | Umschalter Klassenweg → Bauteilweg nach Datenlage, Übergang benannt. | **G3** | — |
| **A2** | IFC-Paket am Kern, Naht `IGebaeudeLeser` von Anfang an. | **G4** | — |
| **A3** | Der formatfreie Name des Zuordnungsdialogs. | **G4** | — |
| **A13** | Keine Herkunftsspalten an der Gebäudetabelle. | **G4** (Herkunftsschritt) | — |
| **A17** | Kein eigener Maskenschlüssel; Überlagerung im Gebäudedialog. | **G4** | — |
| **U10** | Lizenzhinweisseite mit der ersten IFC-Stufe, für alle Fremdanteile. | **G4** | — |
| **U12** | Vorgaben je Baualtersklasse aus dem eigenen EPOS-Gebäudekatalog. | **G4** | — |
| **D1** | gbXML-Import vor IFC-Import. | **Beauftragung G4c/G4a** | — |
| **D16** | Ja — die gbXML-Zonenbildung erweitert E7. | **Beauftragung G4c** | — |
| **M9** | Synonymtabelle in der Auslieferung. | **G4b** (Schema 146, umgesetzt 26.09.2026; aus G6a übernommen, Konzept N1.49) | — |
| **M14** | Beides behalten — Wertekopie an der Schicht und Projektkopie der Baustoffe. | **G6a** (Schema) | — |
| **A6** | Ein Aggregat, Ändern statt Löschen, in einer Transaktion. | **G6b** | — |
| **M2** | Raumseitenmaß beim Import. | **G6c** | — |
| **M10** | Große Testdatei ins Repositorium, nur mit LFS-Eintrag im selben Schritt. | **G6c** (vor dem ersten Commit der Datei) | Lizenz der Fassung mit Raumgrenzen nachfragen (Wortlaut der Empfehlung) |
| **D4** | Export in kWh mit ausdrücklicher Einheit. | **vor der ersten Zeile Quelltext (G7c)** | — |
| **D5** | Deterministische Kennungen in beiden Exportformaten. | **vor der ersten Zeile Quelltext (G7a/G7c)** | — |
| **D2** | gbXML-Export erst mit der zweiten Stufe (synthetische Geometrie). | **Beauftragung G7** | — |
| **D6** | Semantische Stufe (G7c) zuerst bauen. | **G7e** | **Gegenüber** (Werkzeug, Zweck) benennen, vor der Stufe über die semantische hinaus |
| **D11** | Rückgabe angereicherter fremder IFC-Dateien zulässig, mit Kennung in der Datei und Beipackzettel. | **G7d** | — |

**Was noch offen ist — 5 Punkte, keiner erfüllt das Kriterium dieser Liste.** U9 (GB) und U4
(G1) sind mit **E28** (22.09.2026, Konzept N1.33) nach Empfehlung entschieden; vor G0, GB und G1
ist damit kein Anwenderentscheid mehr offen. K4, K5, K6, K7 und K12 (KU1) sind mit **E31**
(23.09.2026, Konzept N1.36) nach Empfehlung entschieden; vor KU1 ist keiner mehr offen. K8, K21
und K23 (KU2) sind mit **E33** (23.09.2026, Konzept N1.38) nach Empfehlung entschieden, K9
abweichend davon; vor KU2 ist keiner mehr offen. U13, U14 und U15 (G4) sind mit **E38**
(24.09.2026, Konzept N1.43) nach Empfehlung entschieden; vor G4 ist keiner mehr offen. M3, M5 und
M6 (G6b) sind mit **E49** (26.09.2026, Konzept N1.55) nach Empfehlung entschieden. Nach
Fälligkeit: **G6c** M7, M8, M12, M13; **G6d** M11.
Dazu die Festlegungen F-Ü1 bis F-D1 (8.4), denen bis zur Beauftragung von G1 zu widersprechen ist.

**Was hier nicht steht, ist nicht unwichtig** — es ist nur an seine Stufe gebunden und kann mit
ihr entschieden werden. Die vollständige Erläuterung jedes Punktes steht in den folgenden
Kapiteln.

---

## 1. Konzept Gebäudesimulation VDI 6007 — Q24, Q25, Q26 (entschieden)

Quelle: [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md),
Kapitel 13 und Nachträge N1.25, N1.28 und N1.31. Alle 23 ursprünglichen Fragen des Konzepts sind
entschieden (E1 bis E19; zuletzt **Q10** mit E18 und **Q11a** mit E19 am 16.09.2026). Mit **E20** —
Trennung der Rechenwege, der Altweg als getrennter Bestandsweg — sind zwei Fragen entstanden (Q24,
Q25 aus Befund X); **E23** („der Altweg bleibt", N1.28) hatte sie geschlossen, **E26** (17.09.2026,
N1.31) stellt klar, dass der Altweg Übergang ist und der VDI-Weg ihn später vollständig ablöst, und
öffnet beide wieder; mit **E22** (Anlagenkopplung, N1.27) kam Q26 hinzu. Die Fragen, die aus E15
entstanden sind, führt das Kühlkonzept als K20 bis K23 (Kapitel 6). Mit **E27** (22.09.2026,
N1.32) sind **Q24, Q25 und Q26 entschieden**, jede nach Option (a); die Abschnitte tragen den
Vermerk und bleiben als Begründung stehen.

### Q24 — wann ist der VDI-Weg bewährt genug, dass GA beauftragt wird?

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — (a), das Ablösekriterium — GA wird beauftragbar und fällig, sobald alle vier Bedingungen erfüllt sind: (1) alle Referenz- und Bestandsprojekte des Anwenders sind auf VDI 6007 gerechnet und die Abweichung zum Altweg ist je Projekt erklärt, (2) eine Feldphase von mindestens einer Heizperiode ohne offenen Fehler am VDI-Weg, (3) KU1 und, falls beauftragt, AK1 sind abgenommen, (4) die Ausbauprobe ist grün. Geprüft wird mit jeder Abnahme; der Stand steht in der [Statusdatei](Status_Gebaeudesimulation_VDI6007.md), Abschnitt 2, Zeile GA.

- **Frage:** Wann ist der VDI-Weg so bewährt, dass die Stufe **GA — Altweg ablösen** beauftragt
  wird — die letzte Stufe des Plans, ohne Termin und in keiner Summe (5–8 PT)?
- **Hintergrund:** E20 hat den Tagesbilanz-Weg Zeichen für Zeichen in ein eigenes Modul `Altweg/`
  verschoben; E23 lässt ihn als Bestandsweg bestehen, und E26 stellt klar: als Übergang, nicht auf
  Dauer — das Neumodell löst das alte später komplett ab und muss eigenständig arbeiten. Bis GA
  leben zwei Rechenwege nebeneinander (Weiche, `IGebaeudeRechenweg`, Modultrennungswache,
  Referenzprojekt auf dem Altweg, Rückweg-Test), und das Risiko dieser Doppelung gilt bis GA; jede
  Stufe, die einen Altweg-Sonderfall einführt, verlängert die Löschliste (ADR-006). Ohne prüfbare
  Bedingung gäbe es keinen Zeitpunkt, an dem die Ablösung fällig wird.
- **Optionen:**
  - **(a) Ablösekriterium aus prüfbaren Bedingungen** — GA wird beauftragt, sobald sie erfüllt
    sind; die Bewährung, nicht der Kalender, entscheidet.
  - **(b) Fester Anlass** — GA an eine Stufe koppeln (etwa nach KU2 oder AK1); einfach, aber blind
    für den Feldbefund.
  - **(c) Offen lassen** — ohne Kriterium bleibt die Doppelung unbegrenzt, und der Altweg wird zum
    Dauerzustand, den E26 gerade ausschließt.
- **Empfehlung des Papiers (N1.31):** **(a)** — GA ist beauftragbar, wenn (1) alle Referenz- und
  Bestandsprojekte des Anwenders einmal auf VDI 6007 gerechnet sind und die Abweichung zum Altweg
  je Projekt erklärt ist, (2) eine Feldphase von mindestens einer Heizperiode ohne offenen Fehler
  am VDI-Weg vorliegt, (3) KU1 und, falls beauftragt, AK1 abgenommen sind und (4) die
  **Ausbauprobe** grün ist — statisch in der Modultrennungswache (außer Weiche, Rückweg-Test und
  Wache nennt keine Datei des Kerns `Altweg/`) und als Gate von GA ein Bau mit umbenanntem Ordner
  `Altweg/` nach Entfernen der Weiche, bei dem der Referenzlauf aller Projekte ohne Altweg-Gebäude
  byte-gleich bleibt.
- **Folge bei Nichtentscheid:** Nichts blockiert G0 bis G7, KU0 bis KU3 oder AK0 bis AK3; aber die
  Doppelung bleibt ohne Ende, und jeder neue Altweg-Sonderfall wird teurer zu entfernen.
- **Fällig vor:** der **Beauftragung von GA**; die Bedingungen werden mit jeder Abnahme (G1 + G2,
  KU1, AK1) geprüft und in der Statusdatei festgehalten.
- **Quelle:** [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.31, N1.25 Punkt 4, 11
  (Stufe GA) und 13 (Q24); [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
  4 und 6 (Löschliste); [Statusdatei](Status_Gebaeudesimulation_VDI6007.md) Abschnitte 1 und 2;
  [Prüfprotokoll](Gebaeudesimulation/2026-09-17_Pruefung_Konsistenz_Umsetzbarkeit.md) Kapitel 1.

### Q25 — Umfang der Stufe GA

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — (a) — vollständige Ablösung nach der Löschliste ([Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) Kapitel 6).

- **Frage:** Was entfernt die Stufe GA — welche Module, Nähte, Dialogteile, Spalten, Tabellen und
  Prüfmittel stehen auf der Löschliste, und was bleibt?
- **Hintergrund:** Mit E26 ist GA wieder die letzte Stufe des Plans. Ihre Löschliste führt das
  Umsetzungskonzept in Kapitel 6; jede Stufe, die einen Altweg-Sonderfall einführt (Hinweistext,
  Ressourcenschlüssel, Sonderweg in Verteilung, Deckung oder Bericht), trägt ihn im selben Auftrag
  dort ein (ADR-006). Die nur vom Altweg gelesenen Spalten stehen in Befund X Kapitel 4, der
  Grundlage der Stufe GA. Fassade und Vorbereitungsschritt bleiben über die Ablösung hinaus — sie
  sind die Gliederung der Gebäudebedarfsrechnung; Weiche, `IGebaeudeRechenweg` und Wache leben bis
  GA.
- **Optionen:**
  - **(a) Vollständige Ablösung nach der Löschliste** — Code, Dialog, Schema, Referenzbasis in
    einem Auftrag; danach gibt es einen Rechenweg.
  - **(b) Teilablösung** — Modul, Weiche und Dialogteile entfernen, die Altweg-Spalten und
    `Tab_DBTagV` als Altlast belassen: kein Schemaschritt, dafür tote Spalten in Tabellen und Sicht.
  - **(c) Nur die Oberfläche** — Schalter, Abschnitt und Ausweis entfernen, das Modul bleibt als
    unerreichbarer Rechenpfad; die Doppelung bliebe im Kern.
- **Empfehlung des Papiers (N1.25 Punkt 4, N1.31):** **(a)** — Modul `Altweg/`, Weiche,
  `IGebaeudeRechenweg`, Modultrennungswache, Schalter „Rechenweg", Abschnitt „Tagesbilanz
  (Bestandsweg)", Spalte `Gebaeude_Modell` und die nur vom Altweg gelesenen Spalten (`DROP COLUMN`
  je Tabelle und Sichtneubau; dabei `Fensterflaeche_Ost`/`_West` einmalig aus
  `Fensterflaeche_Ost_West` füllen), `Tab_DBTagV`/`Tab_DBTagVDaten`, Vergleich alt/neu samt
  `modellErzwungen`, Ausweis, Kältebedarf-0-Hinweis, AK-Sonderfall „feste Last", Schreibstellen der
  Flags `Wochenende`/`Ferien`, Referenzprojekt des Altwegs auf VDI 6007 umstellen, Rückweg-Test
  einstellen, Basis neu einfrieren; 5–8 PT, in keiner Summe.
- **Folge bei Nichtentscheid:** GA kann nicht beauftragt werden; die Löschliste wächst mit jeder
  Stufe weiter, ohne dass jemand ihren Umfang bestätigt hat.
- **Fällig vor:** der **Beauftragung von GA**, zusammen mit Q24.
- **Quelle:** [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.25 Punkt 4, N1.31, 11
  und 13 (Q25); [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 4 und 6;
  [Befund X](Gebaeudesimulation/2026-09-16_Befund_X_Feldzuordnung_Altweg_VDI6007.md) Kapitel 4;
  [ADR-006](ADR-006_Trennung_Altweg_VDI6007.md).

### Q26 — Stufenplan der Anlagenkopplung

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — (a), nach Empfehlung — AK1 nach G2; AK2 nach abgenommenem AK1 und Feldphase; AK3 danach, nach H6 (E24) weiter erst nach der Feldphase von AK1 und AK2 zugesagt. Aufwand AK0 1–2, AK1 10–15, AK2 11–15, AK3 23–38, zusammen 45–70 PT.

- **Frage:** Welche Stufen der Anlagenkopplung werden beauftragt und wann — **AK1** (Heizkreis als
  Randbedingung: Heizkurve, Übergabe, Rücklauf; Einbahnstraße Anlage → Gebäude) nach G2, **AK2**
  (Erzeugerfahrplan als Verfügbarkeit je Stunde, Komfortstunden) nach abgenommenem AK1 und einer
  Feldphase, **AK3** (geschlossener Kreis mit Iteration je Stunde) danach?
- **Hintergrund:** E22 macht die bisher ausgeschlossene Kopplung von Vorlauftemperatur und
  Erzeugerfahrplan an die Raumtemperatur zur benannten EPOS-Erweiterung mit eigenem Papier
  ([Anlagenkopplung](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md)). Das Raummodell bleibt das der Richtlinie, die Normtestbeispiele rechnen
  weiter mit idealer Regelung; jede Stufe ist je Gebäude oder Projekt wählbar mit Vorgabe aus und
  eigenem Einfrierschritt. AK2 und AK3 ändern den Grundsatz „erst Bedarf, dann Deckung" für die
  Gebäude des VDI-Wegs; Gebäude auf dem Altweg gehen bis zu dessen Ablösung (Stufe GA, Zeitpunkt
  offen, Q24) als feste Last ein (E23, E26).
- **Optionen:**
  - **(a) AK1 nach G2; AK2 nach abgenommenem AK1 und einer Feldphase, AK3 danach** — der Nutzen (Aufheizspitzen,
    Vorlauf für die Wärmepumpen-Kennlinien) kommt früh, das Risiko für die Deckungsrechnung spät.
  - **(b) Nur AK1** — Heizkreis als Randbedingung, kein Fahrplan; Unterdeckung bleibt eine Zahl.
  - **(c) Alles in einem Auftrag nach G3** — ein Einfrierschritt; der frühe Nutzen entfällt.
  - **(d) Gar nicht** — der Ausschluss in Konzept 15 bliebe in voller Breite.
- **Empfehlung des Papiers:** **(a)**; Aufwand nach Bestandsaufnahme AK0 1–2 PT, AK1 10–15 PT,
  AK2 11–15 PT, AK3 23–38 PT, zusammen 45–70 PT zuzüglich rund 0,5 PT je Einfrierschritt
  (Anlagenkopplung Rev. 2, 12.1 und 12.2).
- **Folge bei Nichtentscheid:** Das Papier bleibt Konzept ohne Stufe; nichts blockiert G0 bis G7.
- **Fällig vor:** **Beauftragung von G2** (für AK1); AK2 und AK3 mit der Abnahme von AK1.
- **Quelle:** [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.27 und 13 (Q26);
  [Anlagenkopplung](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) 12 und 13.

---

## 2. Umsetzungskonzept — U1 bis U17

Quelle: [`Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md),
Kapitel 5 („Fragen mit Empfehlung"; das Papier steht in Rev. 4) und 1.5 (U17). Von den
siebzehn Fragen ist seit **E38** (24.09.2026, N1.43) keine mehr offen: neun sind mit **E27**
(22.09.2026, N1.32) entschieden und tragen den Vermerk (U1, U3, U5–U8, U10, U12, U17), **U4** und **U9** mit **E28** (22.09.2026, N1.33), **U13, U14** und **U15** mit **E38**; **U2** ist durch E20 überholt, **U11** und **U16** sind mit E18
(16.09.2026) beantwortet — alle drei hier gekürzt, U16 trägt die Ergänzung aus E38; **U17** ist mit der Prüfung vom 17.09.2026
hinzugekommen (F-Ü6).
Vier von ihnen führt die Softwarearchitektur unter eigener Nummer als Sperrpunkt: **U1 = A4**,
**U2 = A19**, **U3 = A5**, **U5 = A9** — entschieden werden sie unter der U-Nummer, damit nicht
zwei Register zwei Antworten bekommen.

### U1 — ein Schreibweg im Katalogeditor

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — ja, nach Empfehlung — ein Schreibweg im Katalogeditor ab G1; „Speichern unter…" bleibt als nicht schließender Zweitknopf. Gilt zugleich für A4.

- **Frage:** Bekommt der Gebäude-Katalogeditor **einen** Schreibweg (OK/Abbrechen) statt der
  heutigen mehreren Aus- und Schreibwege? Das ist eine für den Anwender **sichtbare** Änderung.
- **Hintergrund:** Der Editor trägt heute mehrere nebeneinanderstehende Schreib- und
  Ausstiegswege. Das Konzept verlangt für den VDI-6007-Weg zehn harte Prüfregeln (Q18,
  entschieden). Hängen diese Regeln an mehreren Schreibstellen, ist jede einzeln zu bedienen und
  zu prüfen; zusätzlich macht der Zwischenstand des zweiten Reiters die U·A-Summe zeitweise
  falsch, die der Dialog nach E2 offen ausweisen soll.
- **Optionen:**
  - **(a) Ein Schreibweg (OK/Abbrechen)** — die Prüfregeln stehen an einer Stelle; der Anwender
    muss sich an eine geänderte Bedienung gewöhnen, und die Änderung gehört ins Wiki-Logbuch.
  - **(b) Alles lassen** — keine sichtbare Änderung; zehn Prüfregeln an mehreren Schreibstellen,
    dauerhaft mehrfach zu pflegen und zu testen.
- **Empfehlung des Papiers:** **Ja**, ein Schreibweg. „Speichern unter…" bleibt als **nicht
  schließender Zweitknopf** erhalten, wie es die Hausregel der Oberfläche vorsieht.
- **Folge bei Nichtentscheid:** G1 kann nicht sauber abgenommen werden — die Softwarearchitektur
  führt U1 als A4 ausdrücklich als **Sperrpunkt für G1** („sonst hängen zehn Prüfregeln an drei
  Schreibstellen").
- **Fällig vor:** **G1**.
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U1); [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A4) und 3.3.

### U2 — sieben Modellparameterfelder verstecken oder gesperrt zeigen

**Durch E20 überholt (16.09.2026).** Der Gebäudedialog ist in VDI-6007-Struktur aufgebaut, die
Modellparameter sind immer sichtbar und bearbeitbar; Felder, die nur der Altweg liest, stehen in einem
eingeklappten Abschnitt „Tagesbilanz (Bestandsweg)", der nur bei einem Gebäude auf dem Altweg
erscheint (für die Dauer des Übergangs bis zur Stufe GA, E23, E26). Festlegung in Kapitel 8; Konzept
N1.25, N1.28, N1.31, ADR-006.

### U3 — `Platzhalter` am Standardbaustein `Zahlenfeld`

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — ja, nach Empfehlung — `Platzhalter` am `Zahlenfeld`, rein additiv; die Stilblatt-Tests ziehen nach. Gilt zugleich für A5.

- **Frage:** Wird der Standardbaustein `Zahlenfeld` um einen `Platzhalter` ergänzt (für Texte wie
  „Vorgabe 0,3" im leeren Feld)? Das ist ein Eingriff in einen Baustein, den **alle** Dialoge
  benutzen.
- **Hintergrund:** Der dritte Reiter des Gebäudedialogs bekommt rund zehn Felder, die leer
  bleiben dürfen und dann eine Vorgabe bedeuten (NULL = Vorgabe). Ohne Platzhalter muss jedes
  dieser Felder seine Vorgabe in einer eigenen Herleitungszeile nennen — zehn Zeilen statt zehn
  Platzhalter.
- **Optionen:**
  - **(a) Platzhalter ergänzen** — ein `[Parameter] string` und ein `placeholder`-Attribut, rein
    additiv; alle Dialoge erben die Möglichkeit, und die Stilblatt-Tests sind nachzuziehen.
  - **(b) Je Feld eine Herleitungszeile** — kein Eingriff in den Baustein, dafür zehn zusätzliche
    Zeilen im Dialog und eine unruhigere Maske.
- **Empfehlung des Papiers:** **Ja**, rein additiv; zieht die Stilblatt-Tests nach sich.
- **Folge bei Nichtentscheid:** Der dritte Reiter kann nicht gebaut werden, ohne sich für einen
  der beiden Wege zu entscheiden; die Softwarearchitektur führt den Punkt als A5 und sperrt
  damit G1.
- **Fällig vor:** **G1**.
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U3) und 2.4; [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A5).

### U4 — Glossarabschnitt „Gebäudehülle und Gebäudemodell" vor den Übersetzungen

**Entschieden: E28 (22.09.2026, Konzept N1.33)** — ja, nach Empfehlung — der Abschnitt „13. Gebäudehülle und Gebäudemodell" im Glossar entsteht, bevor die englischen Werte geschrieben werden; ohne ihn entstünden zwei Übersetzungen desselben Begriffs. Fällig vor G1 (Ressourcen des Gebäudedialogs).

- **Frage:** Entsteht ein Abschnitt „13. Gebäudehülle und Gebäudemodell" im
  [`Glossar_Lokalisierung.md`](Glossar_Lokalisierung.md), **bevor** die 63 englischen Werte
  geschrieben werden?
- **Hintergrund:** Das Glossar kennt heute weder Wärmebrücke noch Verschattung, Rahmenanteil,
  Bodenplatte, Randbedingung oder operative Temperatur. Wer die Ressourcen ohne diesen Abschnitt
  schreibt, legt die englischen Begriffe implizit fest — und der nächste Auftrag legt sie anders
  fest.
- **Optionen:**
  - **(a) Glossarabschnitt zuerst** — eine überschaubare Vorarbeit; alle folgenden Masken,
    Meldungen und Berichtstexte greifen auf dieselben Begriffe.
  - **(b) Ohne Abschnitt übersetzen** — spart den Schritt und erzeugt zwei Übersetzungen desselben
    Begriffs, die später zusammenzuführen sind.
- **Empfehlung des Papiers:** **Ja** — ohne den Abschnitt entstehen zwei Übersetzungen desselben
  Begriffs.
- **Folge bei Nichtentscheid:** Die Vorgabe „ohne Abschnitt" greift stillschweigend, sobald der
  erste Auftrag Ressourcen schreibt; die Bereinigung kostet später mehr als der Abschnitt jetzt.
- **Fällig vor:** **G1** (Ressourcen des Gebäudedialogs).
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U4) und 2.9.

### U5 — die zwei Gebäudespalten-Schemaschritte verschmelzen

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — ja, nach Empfehlung — die zwei Gebäudespalten-Schemaschritte werden zu einem (M3) verschmolzen; der Klimaspalten-Schritt bleibt getrennt (mit Schemaschritt 95 vorweggenommen). Gilt zugleich für A9.

- **Frage:** Werden die beiden Schemaschritte für die Gebäudespalten (G1-Spalten und G2-Spalten)
  zu **einem** Schritt verschmolzen — 15 Spalten je Tabelle, **ein** Sichtneubau?
- **Hintergrund:** Entscheid **E1** liefert G1 und G2 **gemeinsam** aus. Zwei Sichtneubauten
  hintereinander sind zwei Gelegenheiten, die Sichtdefinitionen auseinanderlaufen zu lassen — und
  der Sichtneubau ist die Stelle, an der der Namensleser hängt. Der Klimaspalten-Schritt
  (Papiername M4, `Tab_Solar`) hat eine andere Wirkung, anderen Mitläufercode und ein anderes
  Risiko; er ist mit Schemaschritt 95 (Anwenderentscheid 19.09.2026) bereits ausgerollt und steht
  hier nicht mehr zur Wahl (F-S3, [Konzept Klimadatenquellen](Konzept_Klimadatenquellen_TMY_TRY_EPOS-Plan.md)).
- **Optionen:**
  - **(a) Verschmelzen** — ein Schritt, ein Sichtneubau, eine Prüfung; der Schritt wird größer und
    ist im Fehlerfall als Ganzes zurückzunehmen.
  - **(b) Getrennt lassen** — zwei kleinere, je für sich prüfbare Schritte; zwei Sichtneubauten
    hintereinander und zwei Gelegenheiten für abweichende Definitionen.
- **Empfehlung des Papiers:** **Ja, verschmelzen** — der `Tab_Solar`-Schritt bleibt **getrennt**;
  er ist mit Schemaschritt 95 vorweggenommen.
  Die Softwarearchitektur bestätigt das unter A9 und weist darauf hin, dass die Schrittnummern,
  die das Konzept nennt, anderweitig vergeben sind (siehe A11).
- **Folge bei Nichtentscheid:** Die Vorgabe „zwei Schritte" greift; nach der Auslieferung des
  ersten Schritts ist das Verschmelzen nicht mehr möglich.
- **Fällig vor:** **G1** — vor dem ersten der beiden Schritte.
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U5), 1.6 und 1.7; [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6
  (A9) und 2.4.

### U6 — Zeitbezug der Sonnengeometrie: Stundenanfang oder Stundenmitte

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — Verfahren nach Empfehlung — in G1 werden beide Zeitbezüge (Stundenanfang und Stundenmitte) an der einen Stelle (Klimaklasse, A18) gemessen und die Wirkung auf Ost und West beziffert; der Entscheid fällt vor dem Einfrieren. **Offen bleibt als Folgeaufgabe die Endwahl:** Sie folgt mit der Messung in G1, vor dem Einfrieren von G1 + G2.

**Folgeaufgabe erledigt: E29 (23.09.2026, Konzept N1.34)** — Endwahl **Stundenanfang**, wie Photovoltaik und Solarthermie. Die Messung in G1 ergab bei Stundenmitte Ost −10,1 %, West +10,5 %, Süd +0,2 % und für die Jahresheizwärme +0,04 bis +0,10 % — für den Heizbedarf ohne Belang; dafür rechnen Gebäude, PV und Solarthermie denselben Sonnenstand. Eine Umstellung auf die Stundenmitte nur für alle drei gemeinsam.

- **Frage:** Rechnet das Gebäudemodell die Sonnengeometrie auf den **Stundenanfang** wie der
  Bestand — oder auf die **Stundenmitte** wie Blatt 3 der Richtlinie?
- **Hintergrund:** Der Unterschied sind 7,5° Stundenwinkel und trifft genau **Ost und West**; die
  Ost/West-Trennung ist im Konzept gemessen und einer der Gründe für das Stundenmodell. Der
  Bestandsweg über die vorhandenen Strahlungsspalten bleibt in jedem Fall unberührt — er gehört
  zur Referenzbasis.
- **Optionen:**
  - **(a) Stundenanfang** — gleich wie der Bestand, keine Erklärung nötig, weicht vom Blatt-3-Weg
    ab.
  - **(b) Stundenmitte** — folgt der Richtlinie, verlangt eine benannte Abweichung gegenüber dem
    Bestandsweg im selben Programm.
  - **(c) Messen und dann entscheiden** — die Empfehlung: an der **einen** Stelle im Eingangsbauer
    beide Wege rechnen und die Wirkung auf Ost und West beziffern.
- **Empfehlung des Papiers:** **In G1 messen und dann entscheiden**, an der einen Stelle im
  Eingangsbauer. Die Softwarearchitektur präzisiert dazu unter A18, dass diese eine Stelle die
  eigene Klimaklasse ist, damit die Messung ohne den vollen Eingangsbau möglich bleibt.
- **Folge bei Nichtentscheid:** Es greift, was der erste Auftrag baut. Da die Wahl in die
  einzufrierende Basis eingeht, ist eine spätere Umstellung ein Einfrieranlass.
- **Fällig vor:** **dem Einfrieren von G1 + G2** — die Messung ist Teil von G1, der Entscheid fällt
  vor dem Neu-Einfrieren, weil die Wahl in die Basis eingeht (F-Ü8); steht deshalb in Kapitel 0.
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U6) und 1.4; [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.10 und 5.12;
  [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A18);
  [Prüfprotokoll](Gebaeudesimulation/2026-09-17_Pruefung_Konsistenz_Umsetzbarkeit.md) 3.1 (F-Ü8).

### U7 — Wochenendmaske aus dem Ortszeit-Kalender oder aus der Klimatabelle

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — (a) Ortszeit-Kalender, nach Empfehlung — der Vorbereitungsschritt bildet die Maske aus dem Wochentag des 1. Januar des Referenzjahres; die Probe gegen `Tab_Klimadaten.WE` derselben Region bleibt (F-Ü8).

- **Frage:** Woher nimmt das Stundenmodell die Wochenendmaske `WE[365]` — aus dem Ortszeit-Kalender
  des Referenzjahres (Konzept 4.4, Entscheid Q21) oder aus der Spalte `WE` der Klimatabelle, die
  der Altweg liest?
- **Hintergrund:** Die Spalte `WE` entsteht im Klimaimport aus dem Kalenderjahr der TMY-Zeitmarke;
  das Referenzjahr der Zeitbasis kommt aus der aktiven Preisreihe. Das Gebäudemodell rechnet nach
  Q21 in einer Zeitbasis (Ortszeit) und braucht `Tab_Klimadaten` sonst nicht; der Altweg liest
  `WE` weiter und bleibt unberührt. Die beiden Quellen können verschiedene Kalender liefern, und
  welche die Maske trägt, geht in die einzufrierende Basis ein. Nach E26 muss der VDI-Weg
  eigenständig arbeiten — eine Abhängigkeit von einer Altweg-Datenquelle ist keine Begründung mehr.
- **Optionen:**
  - **(a) Ortszeit-Kalender** — der Vorbereitungsschritt bildet `WE[365]` aus dem Wochentag des
    1. Januar des Referenzjahres; eine Probe hält die Maske gegen `Tab_Klimadaten.WE` derselben
    Region und meldet Abweichungen benannt. Der VDI-Weg hängt an keiner Altweg-Datenquelle.
  - **(b) Aus `WE`** — derselbe Kalender wie im Altweg, aber eine Abhängigkeit des VDI-Wegs von einer
    Tabelle, die er sonst nicht braucht, und ein Kalender, der vom Referenzjahr der Preisreihe
    abweichen kann.
- **Empfehlung des Papiers:** **(a) Ortszeit-Kalender** (Konzept 4.4; Prüfung 17.09.2026, F-Ü8).
  Die frühere Empfehlung des Umsetzungskonzepts „aus `WE`, dieselbe Quelle wie der Bestand" trägt
  nach E26 nicht mehr; Umsetzungskonzept 1.10 und Rechenschritte E8 folgen der Festlegung.
- **Folge bei Nichtentscheid:** Der bauende Auftrag entscheidet faktisch, und die Wahl geht in die
  Basis ein — deshalb steht der Punkt in Kapitel 0.
- **Fällig vor:** **G1**.
- **Quelle:** [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 4.4 und 13 (Q21);
  [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5 (U7) und 1.10;
  [Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) Schritt E8;
  [Prüfprotokoll](Gebaeudesimulation/2026-09-17_Pruefung_Konsistenz_Umsetzbarkeit.md) 3.1 (F-Ü8).

### U8 — Normzahlen lokal statt im Repositorium

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — (a) ja, nach Empfehlung — Normzahlen lokal und gitignoriert; der Normfallnachweis läuft lokal, die Lücke im Gate steht im Protokoll.

- **Frage:** Werden die Normreferenzwerte als **gitignorierte, lokal beizustellende** Datei
  geführt, deren Testfälle ohne sie schweigen — mit der Folge, dass der Normfallnachweis **lokal**
  und nicht in der CI läuft?
- **Hintergrund:** Entscheid **E6** und der Nachtrag zu Q2 halten fest, dass die Normzahlen nicht
  ausgeliefert werden; ihr Ablegen im Repositorium wäre eine Vervielfältigung. Git LFS ist keine
  Zugriffsbeschränkung, sondern nur eine andere Ablage. Zugleich ist der Normfallnachweis das
  Abnahmekriterium der Stufe G0.
- **Optionen:**
  - **(a) Lokale Datei, schweigende Tests** — kein Normwert im Repositorium; die Normfalltests
    laufen nur dort, wo die Datei beigestellt ist, die CI überspringt sie benannt. Die Lücke im
    Gate gehört ins Protokoll, der Laufauszug (Abweichung je Fall, **ohne** Absolutwerte) in die
    Dokumentation.
  - **(b) Normzahlen ins Repositorium** — vollständiges Gate in der CI, aber eine Vervielfältigung
    der Richtlinienwerte; scheidet nach E6 aus.
  - **(c) Kein Normfallnachweis in Tests** — nur der außerhalb geführte Prototypnachweis; dann ist
    die Abnahme von G0 nicht wiederholbar.
- **Empfehlung des Papiers:** **(a) Ja** — mit Protokolleintrag über die Lücke im Gate.
- **Folge bei Nichtentscheid:** Der erste Auftrag zu G0 muss die Frage selbst beantworten, und
  jede andere Antwort als (a) berührt die Lizenzlage.
- **Fällig vor:** **G0** — es ist die Abnahmebedingung dieser Stufe.
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U8) und 1.9; [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.2 und N1.11 (E6).

### U9 — die Grenze von 100 Gebäuden im Bestandsweg

**Entschieden: E28 (22.09.2026, Konzept N1.33)** — (a) ja, in GB, nach Empfehlung — die feste Grenze von 100 Gebäuden im Bestandsweg wird in GB behoben, wo die Schleife ohnehin angefasst wird: das ungelesene Feld wird gelöscht, das andere auf die tatsächliche Zeilenzahl dimensioniert. Ergebnisneutral, also ohne Einfrieranlass.

- **Frage:** Wird die feste Grenze von 100 Gebäuden im Bestandsweg behoben, und in welchem
  Schritt?
- **Hintergrund:** Zwei Felder fester Länge begrenzen heute die Gebäudeschleife; oberhalb der
  Grenze bricht der Lauf mit einem Indexfehler ab. Eines der beiden Felder wird nirgends gelesen.
  Die Schleife wird in der Stufe GB (Bestandsbefunde vor G1) ohnehin angefasst.
- **Optionen:**
  - **(a) In GB beheben** — das ungelesene Feld löschen, das andere auf die tatsächliche Zeilenzahl
    dimensionieren; **ergebnisneutral**, also ohne Einfrieranlass.
  - **(b) Später beheben** — ein eigener Auftrag, der dieselbe Schleife ein zweites Mal anfasst.
  - **(c) Lassen** — die Grenze bleibt; ein Projekt mit mehr Gebäuden bricht mit einem Indexfehler
    ab statt mit einer Meldung.
- **Empfehlung des Papiers:** **Ja, in GB**, wo die Schleife ohnehin angefasst wird. Ergebnisneutral.
- **Folge bei Nichtentscheid:** Die Grenze bleibt, und der Fehler tritt beim Anwender als
  Indexfehler auf — genau das Fehlerbild, das die Stufe GB sonst beseitigt.
- **Fällig vor:** **GB**.
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U9); [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 11 (Stufe GB).

### U10 — Lizenzhinweisseite im Installationspaket, und zwar für alle Fremdanteile

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — ja, nach Empfehlung — Lizenzhinweisseite mit der ersten IFC-Stufe, für alle ausgelieferten Fremdanteile.

- **Frage:** Kommt eine Lizenzhinweisseite ins Installationspaket — und dann gleich für **alle**
  ausgelieferten Fremdanteile, nicht nur für das IFC-Paket?
- **Hintergrund:** Die Lizenz des IFC-Pakets ist ein Datei-Copyleft: Ihr Abschnitt 3.1 verlangt,
  dass der Quelltext aller ausgelieferten Dateien dieser Lizenz verfügbar ist und dem **Empfänger**
  mitgeteilt wird; der dauerhafte Verweis auf die Quellen genügt. Ohne diese Seite ist der
  IFC-Import nicht auslieferbar. Die übrigen Fremdanteile sind ohnehin fällig — darunter die
  Bibliothek des Gebäudebetrachters aus E11.
- **Optionen:**
  - **(a) Eine Seite für alle Fremdanteile** — einmal gebaut, danach je Paket eine Zeile; erfüllt
    die Auflage und räumt zugleich die übrigen Anteile auf.
  - **(b) Nur für das IFC-Paket** — kleinster Aufwand, erfüllt die Auflage für dieses Paket; die
    übrigen Anteile bleiben unbelegt und die Seite wird ein zweites Mal angefasst.
- **Empfehlung des Papiers:** **Ja, mit der ersten IFC-Stufe und für alle.**
- **Folge bei Nichtentscheid:** Die Stufe G4 kann gebaut, aber **nicht ausgeliefert** werden.
- **Fällig vor:** **G4** (erste IFC-Stufe, gemeinsam mit dem Setup-Schritt).
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U10) und 3.6; [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 7.4 und N1.7 (E3);
  [ADR-003](ADR-003_IFC_xBIM_ohne_Geometriekernel.md);
  [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 8.1.

### U11 — Größenlimit für IFC-Dateien

**Mit E18 beantwortet (16.09.2026, Empfehlung angenommen): benannt ablehnen, nicht versuchen; die
iOS-Zahl wird in G4 gemessen, nicht geschätzt.** Konzept N1.23, Statusdatei Abschnitt 1; hier gekürzt.

### U12 — Herkunft der Vorgaben je Baualtersklasse

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — (a), nach Empfehlung — die Vorgaben je Baualtersklasse werden aus dem eigenen EPOS-Gebäudekatalog abgeleitet, (b) bleibt benannter Rückfallweg.

- **Frage:** Woher kommen die Vorgabewerte je Baualtersklasse, die der Import setzt?
- **Hintergrund:** Die naheliegende öffentliche Quelle hat weder eine dauerhafte Kennung noch eine
  Datensatzlizenz; ihre Werte zu übernehmen wäre rechtlich ungeklärt. Die Testdatenbank führt
  dagegen Gebäude je Klasse, aus denen sich eigene Werte ableiten lassen.
- **Optionen:**
  - **(a) Eigene Werte aus dem EPOS-Gebäudekatalog ableiten** — lizenzfrei, hausgemacht, passt zu
    den übrigen EPOS-Vorgaben; die Herleitung ist zu dokumentieren.
  - **(b) Nur die Klassen vorbelegen und den Rest leer lassen** — nichts geraten, dafür mehr
    Handarbeit für den Anwender nach jedem Import.
- **Empfehlung des Papiers:** **(a)** — mit (b) als benannter Rückfallweg.
- **Folge bei Nichtentscheid:** Der Import setzt entweder ungeklärte Fremdwerte oder gar nichts;
  beides ist beim Anwender sichtbar.
- **Fällig vor:** **G4** (Vorgaben je Baualtersklasse sind Bestandteil dieser Stufe).
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U12); [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 13 (Q11) und N1.17 (E13).

### U13 — mehrere Gebäude in einer IFC-Datei

**Entschieden: E38 (24.09.2026, Konzept N1.43)** — (a) eines je Lauf, nach Empfehlung — bei mehreren Gebäuden in einer Datei bietet der Dialog eine Klappliste an, übernommen wird je Lauf eines; kein Anlegen aller auf einmal, also keine erzeugten Katalognamen und keine Dublettenlogik. Gilt für beide Importwege (IFC G4a, gbXML G4c).

- **Frage:** Legt der Import bei mehreren Gebäuden in einer Datei **eines je Lauf** an (Klappliste)
  — oder alle auf einmal?
- **Hintergrund:** „Alle auf einmal" müsste Katalognamen selbst erzeugen und zöge damit die
  Dublettenlogik des Katalogimports nach sich. Das ist ein eigener Arbeitsschritt mit eigenem
  Fehlerbild.
- **Optionen:**
  - **(a) Eines je Lauf** — der Anwender wählt; der Import bleibt überschaubar und
    nachvollziehbar.
  - **(b) Alle auf einmal** — bequemer bei großen Dateien; erzeugt Namen automatisch und braucht
    die Dublettenbehandlung.
- **Empfehlung des Papiers:** **Eines je Lauf.**
- **Folge bei Nichtentscheid:** Der Umfang der Stufe G4 ist unbestimmt — der Unterschied ist ein
  ganzer Arbeitsschritt.
- **Fällig vor:** **G4**.
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U13) und 3.5.

### U14 — Fensterabzug an der Wandfläche

**Entschieden: E38 (24.09.2026, Konzept N1.43)** — (a) abziehen, nach Empfehlung — die Wandfläche wird um Fenster und Außentüren vermindert. Wird die Differenz negativ, greift beim IFC-Weg `NetSideArea`, sonst Warnung und 0; beim gbXML-Weg A = 0, Zeile rot, `IMP_GBXML_PROT_NETTOFLAECHE_NEGATIV`. Gilt für beide Importwege (IFC G4a, gbXML G4c).

- **Frage:** Wird die Wandfläche beim Import um Fenster und Außentüren **vermindert** — oder
  bleiben die Öffnungen in der Bruttofläche, so wie die Datei sie liefert?
- **Hintergrund:** Das Modell führt Wand und Fenster **getrennt** mit je eigenem U-Wert. Ohne
  Abzug zählt die Öffnungsfläche zweimal, einmal als Wand und einmal als Fenster — der Fehler ist
  systematisch und in der Jahressumme sichtbar.
- **Optionen:**
  - **(a) Abziehen** — physikalisch richtig; wird die Differenz negativ, ist die Nettofläche der
    Datei zu nehmen, sonst eine Warnung und der Wert 0.
  - **(b) Belassen** — nichts zu rechnen; die Hülle wird systematisch zu groß.
- **Empfehlung des Papiers:** **Abziehen**, mit dem beschriebenen Rückfall.
- **Folge bei Nichtentscheid:** Der Import liefert eine zu große Hülle — ein stiller Fehler, der
  im Ergebnis wie ein schlechter Dämmstandard aussieht.
- **Fällig vor:** **G4**.
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U14) und 3.4; [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 3.4
  (dieselbe Regel für gbXML).

### U15 — Wärmebrückenwerte und Anschlusslängen beim Import

**Entschieden: E38 (24.09.2026, Konzept N1.43)** — (a) Kennwerte als Vorgabe, Längen leer, nach Empfehlung — die drei ψ-Werte werden als Vorgabe je Baualtersklasse gesetzt, die drei Anschlusslängen bleiben leer; beide tragen die Herkunftsmarke `Vorgabe` bzw. `Leer`. Gilt für beide Importwege (IFC G4a, gbXML G4c).

- **Frage:** Werden die drei Wärmebrücken-Kennwerte und die drei Anschlusslängen beim IFC-Import
  als **Vorgabe je Baualtersklasse** gesetzt oder **leer** gelassen?
- **Hintergrund:** IFC liefert weder das eine noch das andere. Eine geratene Anschlusslänge sähe
  im Dialog aus wie eine gemessene; ein Klassen-Kennwert ist als Klassenwert erkennbar. Beide
  Felder tragen ohnehin eine Herkunftsmarke.
- **Optionen:**
  - **(a) Kennwerte als Vorgabe, Längen leer** — der Anwender sieht, was geschätzt und was
    unbekannt ist.
  - **(b) Beides als Vorgabe** — das Gebäude rechnet sofort durch; die Längen sind erfunden.
  - **(c) Beides leer** — nichts erfunden; der Wärmebrückenanteil fehlt vollständig.
- **Empfehlung des Papiers:** **Kennwerte als Vorgabe, Längen leer**, beide mit Herkunftsmarke
  „Vorgabe" bzw. „Leer".
- **Folge bei Nichtentscheid:** Der bauende Auftrag entscheidet; im ungünstigen Fall trägt eine
  erfundene Länge eine Herkunft, die sie als gemessen ausweist.
- **Fällig vor:** **G4**.
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 5
  (U15) und 3.4.

### U16 — Gerätebau im iOS-Workflow oder einmaliger Nachweis

**Mit E18 beantwortet (16.09.2026, Empfehlung angenommen): einmaliger Nachweis in G4 von Hand,
nach Rückfrage; kein dauerhafter Gerätebau im Workflow.** Konzept N1.23, Statusdatei Abschnitt 1;
hier gekürzt.

**Ergänzt: E38 (24.09.2026, Konzept N1.43)** — genau ein iOS-Lauf (`ios.yml`) als Trimming- und Gerätenachweis für G4a, ausschließlich nach ausdrücklicher Rückfrage beim Anwender zum Zeitpunkt der Abnahme von G4a; sonst keine iOS- oder macOS-Läufe, G4c also ohne iOS-Lauf. E38 öffnet keinen Punkt.

### U17 — Altweg-Gebäude ohne Tagesverteilung: Lauf abbrechen oder Gebäude benannt ablehnen

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — (c), nach Empfehlung — ein Altweg-Gebäude ohne Tagesverteilung wird benannt abgelehnt, mit GA; bis dahin gilt (a).

- **Frage:** Bricht ein Altweg-Gebäude ohne Tagesverteilung wie heute den **gesamten** Lauf ab —
  auch für die VDI-Gebäude desselben Projekts — oder wird das Gebäude künftig benannt abgelehnt,
  während die übrigen Gebäude rechnen?
- **Hintergrund:** Der Abbruch steht heute im Rumpf der Bedarfsrechnung (Warnung
  `SIMENG_TAGESVERTEILUNG_FEHLT`, `return false`, danach endet die ganze Bedarfsrechnung); er
  wandert mit dem Tagesbilanz-Weg nach `Altweg/` und bleibt Bestandsverhalten. Das VDI-Modul
  braucht keine Tagesverteilung und wirft keine Ausnahme: Es legt eine Meldung der Stufe Fehler im
  Protokollkanal ab und gibt `false` zurück, der Lauf endet an derselben Stelle wie heute (F-Ü6).
  Im gemischten Projekt ist die Wirkung hart: ein Altweg-Gebäude reißt die VDI-Gebäude mit.
- **Optionen:**
  - **(a) Wie heute** — der ganze Lauf endet; byte-gleich zum Bestand, kein Eingriff in den Altweg.
  - **(b) Gebäude benannt ablehnen** — der Lauf geht für die übrigen Gebäude weiter, das Gebäude
    erscheint mit Meldung; das ändert Altweg-Verhalten, bricht den byte-gleichen Rückweg-Test und
    ist ein Einfrieranlass am Altweg.
  - **(c) (b), aber erst mit GA** — nach der Ablösung gibt es kein Altweg-Gebäude mehr; ein Gebäude
    ohne Modelldaten wird dann wie jede fehlende Eingangsgröße benannt abgelehnt.
- **Empfehlung des Papiers:** **(c)** — benannt ablehnen, aber erst mit GA, weil jede frühere
  Änderung am Altweg-Verhalten den Rückweg-Test bricht und einen eigenen Einfrieranlass verlangt;
  bis dahin gilt (a), als Wirkung benannt in Systementwurf 6 (K4) und Umsetzungskonzept 1.5.
- **Folge bei Nichtentscheid:** Es bleibt bei (a); nichts blockiert G1 — die Wirkung im gemischten
  Projekt muss dem Anwender aber bekannt sein.
- **Fällig vor:** der **Beauftragung von GA**.
- **Quelle:** [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 1.5;
  [Systementwurf](Systementwurf_Gebaeudesimulation_EPOS-Plan.md) 3 und 6 (K4);
  [Prüfprotokoll](Gebaeudesimulation/2026-09-17_Pruefung_Konsistenz_Umsetzbarkeit.md) 3.1 (F-Ü6).

---

## 3. Mehrzonenmodell aus IFC — M1 bis M14

Quelle: [`Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md`](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md),
Kapitel 10 (das Papier steht in Rev. 2). Die Stufen sind G6a (Datenmodell und Pflege), G6b
(Zoneneingabe und Rechenweg), G6c (Zonenimport) und G6d (Referenzprojekt und Einfrieren). **M1** und
**M4** sind mit [ADR-005](ADR-005_Zonenkopplung_Mehrzonenmodell.md) am 16.09.2026 entschieden (E17)
und hier gekürzt. Mit **E27** (22.09.2026, N1.32) sind **M2, M9, M10 und M14** entschieden
(Vermerk je Abschnitt). Mit **E49** (26.09.2026, N1.55) sind **M3, M5 und M6** entschieden; offen
bleiben M7, M8 und M11–M13.

### M1 — Kopplungsweg der Zonen

**Entschieden am 16.09.2026 mit ADR-005 (E17): Weg B — Gauß-Seidel je Stunde über die
Nachbarraum-Randbedingung**, A als Vergleichsrechnung, C für zwei Zonen als Prüforakel; die Wahl
wird gemessen bestätigt (Probe 6, Gate „eine Zone bitgleich"). Konzept N1.22, Statusdatei
Abschnitt 1; hier gekürzt.

### M2 — Raumseitenmaß oder Bruttomaß beim Import

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — nach Empfehlung — Raumseitenmaß beim Import durchhalten und im Dialog benennen; eine eigene Probe beziffert den Abstand zum Einzonenweg vorher.

- **Frage:** Wird beim Zonenimport durchgehend das **Raumseitenmaß** verwendet oder das
  **Bruttomaß**?
- **Hintergrund:** Das Einzonenmodell bemisst nach einer anderen Regel; ein Wechsel auf das
  Raumseitenmaß ist deshalb ein Entscheid und keine Selbstverständlichkeit. Der Unterschied
  schlägt auf alle Hüllflächen durch und damit auf die Jahressumme.
- **Optionen:**
  - **(a) Raumseitenmaß, im Dialog benannt** — passt zu den Raumgrenzen, die die Dateien liefern;
    weicht von der Bemaßungsregel des Einzonenmodells ab, und dieser Abstand ist vorher zu
    beziffern.
  - **(b) Bruttomaß** — gleiche Regel wie im Einzonenmodell; die importierten Raumgrenzen müssten
    umgerechnet werden, was ohne Wanddicken nicht zuverlässig geht.
- **Empfehlung des Papiers:** **Raumseitenmaß durchhalten und im Dialog benennen**; eine eigene
  Probe beziffert den Abstand zum Einzonenweg vorher.
- **Folge bei Nichtentscheid:** Einzonen- und Mehrzonenweg desselben Gebäudes rechnen verschieden,
  ohne dass gesagt ist, warum — genau die Art von stillem Unterschied, die das Regressionsnetz
  nicht auffängt.
- **Fällig vor:** **G6c**.
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 6.2 und 10 (M2).

### M3 — die Temperaturregel für unbeheizte Nachbarzonen

**Entschieden: E49 (26.09.2026, Konzept N1.55, A1)** — nach Empfehlung — Vorgabe mit Übersteuerung je Trennfläche (`Tab_Bauteil.Trennflaeche_Zuordnung`, NULL = 4-K-Regel); gemessen am adiabaten Vorlauf, eine Überschreitung im gekoppelten Lauf wird benannt. Umgesetzt mit G6b.

- **Frage:** Gilt die Temperaturdifferenz-Regel gegenüber Nachbarzonen als **feste Vorgabe** oder
  ist sie **je Trennfläche übersteuerbar**?
- **Hintergrund:** Die Zuordnung einer Trennfläche zur Außen- oder Innenwandgruppe hängt davon ab,
  wie weit die Nachbartemperatur von der eigenen abweicht. Gemessen wird das an den **gerechneten**
  Raumkonditionen eines adiabaten Vorlaufs, nicht an den Sollwerten, und die Zuordnung fällt
  **einmal vor dem Lauf**, nie während des Laufs.
- **Optionen:**
  - **(a) Feste Vorgabe** — eine Regel für alle, keine Bedienung; ungewöhnliche Fälle (Keller,
    Treppenhaus) fallen möglicherweise falsch.
  - **(b) Vorgabe mit Übersteuerung je Trennfläche** — der Anwender kann eingreifen, sieht die
    gemessene Differenz als Beleg, und eine Überschreitung wird nach dem Lauf benannt.
- **Empfehlung des Papiers:** **Vorgabe mit Übersteuerung je Trennfläche**, Anzeige der Differenz
  als Beleg, Messung am adiabaten Vorlauf, Benennung einer Überschreitung nach dem Lauf.
- **Folge bei Nichtentscheid:** Der Zonendialog bekommt ein Bedienelement, das niemand bestellt
  hat — oder die Zuordnung ist in Grenzfällen nicht korrigierbar.
- **Fällig vor:** **G6b**.
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 2.2 und 10 (M3).

### M4 — Zonen-Luftaustausch in G6 oder später

**Entschieden am 16.09.2026 mit ADR-005 (E17): in G6b**, als Paare mit der Prüfregel
`CHECK (ID_ZoneA < ID_ZoneB)`, Tabelle im Schemaschritt S-G. Konzept N1.22, Statusdatei
Abschnitt 1; hier gekürzt.

### M5 — unbeheizte Zonen im Bedarfsdialog

**Entschieden: E49 (26.09.2026, Konzept N1.55, A2)** — nach Empfehlung — ja: eigene Zeilen ohne Heizwärme und Last, mit Temperatur und Überhitzungsstunden gegen `Maximaleraumtemperatur`. Umgesetzt mit G6b.

- **Frage:** Bekommen unbeheizte Zonen eigene Zeilen im Bedarfsdialog?
- **Hintergrund:** Unbeheizte Zonen tragen keine Heizlast, aber eine **Temperatur** und
  Überhitzungsstunden. Genau das ist der fachliche Gewinn der Mehrzonenrechnung: Ohne eigene Zeile
  bleibt etwa die Kellertemperatur unsichtbar.
- **Optionen:**
  - **(a) Ja, mit eigener Zeile** — die Temperatur wird sichtbar; die Zeile trägt in der
    Energiespalte nichts und muss das erklären.
  - **(b) Nein** — die Liste bleibt kurz und zeigt nur Energie; der fachliche Gewinn bleibt
    verborgen.
- **Empfehlung des Papiers:** **Ja.**
- **Folge bei Nichtentscheid:** Der Umfang des Bedarfsdialogs in G6b ist unbestimmt.
- **Fällig vor:** **G6b**.
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 2.5, 2.8 und 10 (M5).

### M6 — Länge des Vorlaufs

**Entschieden: E49 (26.09.2026, Konzept N1.55, A3)** — nach Empfehlung — 30 Tage mit Probe, Verlängerung auf 90 Tage über 0,05 K benannt, nur ab zwei Zonen. Umgesetzt mit G6b.

- **Frage:** Rechnet der Vorlauf 30 Tage mit Konvergenzprobe — oder fest 90 Tage?
- **Hintergrund:** Der Vorlauf bringt die Speichermassen in einen eingeschwungenen Zustand, bevor
  die Jahresrechnung zählt. Bei leichten Gebäuden ist er nach kurzer Zeit erreicht; eine feste
  lange Dauer kostet dann Rechenzeit ohne Aussage.
- **Optionen:**
  - **(a) 30 Tage mit Probe** — der Vorlauf endet, wenn er eingeschwungen ist, und meldet, wenn
    nicht; braucht eine Konvergenzprüfung.
  - **(b) Fest 90 Tage** — keine Prüfung nötig, dafür dreifache Vorlaufzeit auch dort, wo sie
    nichts ändert.
- **Empfehlung des Papiers:** **30 Tage mit Probe.**
- **Folge bei Nichtentscheid:** Die Rechenzeitplanung der Mehrzonenrechnung steht auf einer
  ungeprüften Annahme; die Wahl geht in die eingefrorenen Ergebnisse ein.
- **Fällig vor:** **G6b**.
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 2.9 und 10 (M6).

### M7 — Zonenregel als Vorgabe beim Import

- **Frage:** Welche Zonierungsregel ist beim Import die Vorgabe — Zusammenfassung je Geschoss oder
  stets die gröbste Regel?
- **Hintergrund:** Die Dateien liefern die Zonentopologie in aller Regel nicht mit; sie muss aus
  den Raumgrenzen gebildet werden. Die geschossweise Regel trägt in allen vier gemessenen
  Dateien; fehlen die Grenzen, bleibt zwingend die gröbste Regel.
- **Optionen:**
  - **(a) Geschossweise, mit der gröbsten Regel als Rückfall** — ein brauchbares Ergebnis in den
    gemessenen Fällen, ein definierter Rückfall sonst.
  - **(b) Stets die gröbste Regel** — immer ein Ergebnis, nie eine sinnvolle Zonierung.
- **Empfehlung des Papiers:** **Geschossweise, Rückfall auf die gröbste Regel**, weil die
  geschossweise Regel in allen vier Messdateien trägt.
- **Folge bei Nichtentscheid:** Der Import erzeugt eine nicht festgelegte Zahl von Zonen; die
  Mindestgrößenregel (M8) müsste hinterher aufräumen.
- **Fällig vor:** **G6c**.
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 6.1, 6.5 und 10 (M7).

### M8 — Mindestgröße einer Zone

- **Frage:** Gilt eine Mindestgröße je Zone (der größere Wert aus einer Mindestfläche und einem
  Mindestanteil), und was geschieht mit einer zu kleinen Zone?
- **Hintergrund:** Ohne Mindestgröße entstehen aus einer der gemessenen Dateien 78 Zonen — jede
  Abstellkammer eine eigene. Der Zuschlag zur Nachbarzone mit der größten gemeinsamen Grenzfläche
  ist die physikalisch naheliegende Zusammenlegung.
- **Optionen:**
  - **(a) Mindestgröße mit Zuschlag zum größten Nachbarn** — wenige, sinnvolle Zonen; die Regel
    ist zu dokumentieren, damit der Anwender die Zusammenlegung versteht.
  - **(b) Keine Mindestgröße** — jede Raumgrenze wird eine Zone; die Obergrenze (M12) greift
    sofort.
- **Empfehlung des Papiers:** **Ja**, mit Zuschlag zum Nachbarn mit der größten gemeinsamen
  Grenzfläche.
- **Folge bei Nichtentscheid:** Importe erzeugen unbrauchbar viele Zonen; die Rechenzeit steigt,
  ohne dass das Ergebnis besser wird.
- **Fällig vor:** **G6c**.
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 6.1 und 10 (M8).

### M9 — Synonymtabelle in der Auslieferung oder je Projekt

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — nach Empfehlung — die Synonymtabelle steht in der Auslieferung.

- **Frage:** Steht die Synonymtabelle, die Namen der Autorensysteme auf EPOS-Begriffe abbildet, im
  **Auslieferungskatalog** oder **je Projekt**?
- **Hintergrund:** Die Namen der Autorensysteme wiederholen sich projektübergreifend — dieselben
  Bauteil- und Materialbezeichnungen tauchen in jeder Datei desselben Programms wieder auf. Eine
  je Projekt gepflegte Zuordnung wäre in jedem neuen Projekt erneut zu leisten.
- **Optionen:**
  - **(a) Auslieferung** — einmal gepflegt, überall wirksam; je Projekt gepflegte Zuordnungen
    ergänzen sie.
  - **(b) Je Projekt** — keine Auslieferungsdatenpflege; jeder Import beginnt bei null.
- **Empfehlung des Papiers:** **Auslieferung.**
- **Folge bei Nichtentscheid:** Die Tabelle landet dort, wo der bauende Auftrag sie hinlegt; ein
  späterer Umzug ist ein Schemaschritt samt Migration und berührt die Auslieferungsvorlage.
- **Fällig vor:** **G6a** (Schema).
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 6.3, 4.2 und 10 (M9).

### M10 — große Testdatei im Repositorium und ihre Lizenz

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — (a) ja, nach Empfehlung — die große Testdatei kommt ins Repositorium, nur zusammen mit dem LFS-Eintrag im selben Schritt. Nach dem Wortlaut der Empfehlung ist die Lizenz der Fassung mit Raumgrenzen vor dem ersten Commit der Datei nachzufragen.

- **Frage:** Kommt die 17,6 MB große Importprobe ins Repositorium — und ist die Lizenz der Fassung
  mit Raumgrenzen zu klären?
- **Hintergrund:** Es ist die **einzige** gemessene Datei mit Schichten **und** echten
  Raumgrenzenpaaren, also die einzige, die den Zonenimport vollständig belastet. Sie ist zugleich
  groß genug, um dauerhaft in der Git-Geschichte zu liegen, wenn sie ohne LFS-Eintrag committet
  wird. Für die Fassung mit Raumgrenzen ist die freizügige Lizenz **nicht** belegt — belegt ist
  sie nur für eine andere Ablage desselben Modells.
- **Optionen:**
  - **(a) Ja, mit LFS-Zeile im selben Schritt** — Datei verfügbar, Geschichte sauber; dazu ein
    Vermerk im Abschnitt „Git LFS" der Referenzlauf-Liesmich. Die Lizenz der Fassung mit
    Raumgrenzen ist **vorher nachzufragen**; bis dahin wird nur außerhalb des Repositoriums
    gemessen.
  - **(b) Test holt die Datei zur Laufzeit** — nichts im Repositorium; der Test schweigt ohne die
    Datei, und die CI prüft den Fall nicht.
- **Empfehlung des Papiers:** **(a), aber nur zusammen mit der LFS-Zeile im selben Schritt** —
  ohne sie liegt ein 17,6-MB-Blob dauerhaft in der Geschichte. Lizenz der Fassung mit Raumgrenzen
  **nachfragen**. Alternative ist (b).
- **Folge bei Nichtentscheid:** Entweder fehlt die einzige vollständige Probe — oder sie landet
  ohne LFS-Eintrag unwiderruflich in der Geschichte des Repositoriums.
- **Fällig vor:** **G6c**, genauer: **vor dem ersten Commit der Datei**.
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 8.2 und 10 (M10);
  Hausregel zu Git LFS in [`CLAUDE.md`](../../CLAUDE.md) und
  [`Referenzlaeufe/LIESMICH.md`](../../Referenzlaeufe/LIESMICH.md).

### M11 — Referenzprojekt mit Zonen: umstellen oder neu anlegen

- **Frage:** Wird für die Mehrzonenrechnung ein **bestehendes** Referenzprojekt umgestellt — oder
  ein vierzehntes angelegt?
- **Hintergrund:** Die Basis führt heute dreizehn Projekte; jedes zusätzliche verlängert **jeden**
  CI-Lauf dauerhaft. Eine Umstellung bewegt dagegen die Zahlen eines bestehenden Projekts und
  verlangt einen begründeten Einfrierschritt — der für G6d ohnehin vorgesehen ist.
- **Optionen:**
  - **(a) Bestehendes umstellen** — keine zusätzliche Laufzeit; die Zahlen eines Projekts ändern
    sich und sind zu begründen.
  - **(b) Vierzehntes Projekt** — die bestehenden Zahlen bleiben unberührt; jeder Lauf wird
    dauerhaft länger.
- **Empfehlung des Papiers:** **Bestehendes umstellen**, im Einfrierschritt G6d.
- **Folge bei Nichtentscheid:** Der Einfrierschritt von G6d hat keinen Gegenstand, und die
  Mehrzonenrechnung bleibt im Regressionsnetz unsichtbar.
- **Fällig vor:** **G6d**.
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 8.3, 9 und 10 (M11).

### M12 — Obergrenze der Zonenzahl je Gebäude

- **Frage:** Gilt eine Obergrenze von 50 Zonen je Gebäude — und wie hart?
- **Hintergrund:** Die Zahl ist eine **Setzung aus der Rechenzeit, kein Messergebnis**; das Papier
  sagt das ausdrücklich und hält sie offen, bis die Laufzeit an einem echten Mehrzonengebäude
  gemessen ist. Der Systementwurf führt denselben Punkt in seiner Wiedervorlage.
- **Optionen:**
  - **(a) 50 als Vorgabe, im Import Warnung mit Rückfrage** — der Import schlägt „auf Geschosse
    zusammenlegen" vor; die **Rechnung** lehnt darüber benannt ab.
  - **(b) Harte Grenze überall** — einfach, aber der Import bricht ab, wo eine Zusammenlegung
    genügt hätte.
  - **(c) Keine Grenze** — die Rechenzeit ist nach oben offen.
- **Empfehlung des Papiers:** **(a)**; die Zahl selbst bleibt offen, bis sie gemessen ist.
- **Folge bei Nichtentscheid:** Der Import kennt keine Schranke, und die zugesagte Rechenzeit ist
  nicht zu halten.
- **Fällig vor:** **G6c** (Import) — die Messung gehört zur Abnahme von G6b.
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 2.9, 6.6 und 10 (M12);
  [Systementwurf](Systementwurf_Gebaeudesimulation_EPOS-Plan.md) 11.

### M13 — Umfang der Rekonstruktion für ein Autorensystem ohne Raumgrenzenpaare

- **Frage:** Wie weit soll die Rekonstruktion der Nachbarschaften gehen, wenn ein Autorensystem
  keine Raumgrenzenpaare schreibt?
- **Hintergrund:** Ohne Paarbildung über die Geometrie ließe sich Mehrzonigkeit nur dort anbieten,
  wo die Datei echte Paare enthält. Genau die einzige lizenzfreie **kleine** Referenzdatei käme
  dann nicht mehr in Betracht.
- **Optionen:**
  - **(a) Vollständig** — Paarbildung über die Geometrie; alle gemessenen Dateien sind nutzbar.
  - **(b) Mager** — Mehrzonigkeit nur bei echten Paaren, sonst die gröbste Regel; spart 2–3 PT und
    schließt die kleine Referenzdatei aus.
- **Empfehlung des Papiers:** **Vollständig.**
- **Folge bei Nichtentscheid:** Der Umfang von G6c ist unbestimmt; im magereren Fall fehlt eine
  kleine, lizenzfreie Probe für die CI.
- **Fällig vor:** **G6c**.
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 6.2 und 10 (M13).

### M14 — Projektkopie der Baustoffe neben dem Auslieferungskatalog

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — nach Empfehlung — beides behalten: der Auslieferungskatalog mit der Wertekopie an der Schicht und die Projektkopie der Baustoffe.

- **Frage:** Wird eine **Projektkopie** der Baustoffe gebraucht, oder genügt der
  Auslieferungskatalog zusammen mit der Wertekopie an der Schicht?
- **Hintergrund:** Die Wertekopie an der Schicht schützt **gerechnete Ergebnisse**: Ändert jemand
  später einen Katalogwert, bleiben die Zahlen des Projekts, wie sie waren. Die Projektkopie
  erlaubt darüber hinaus **projekteigene Stoffe**, die es im Katalog nicht gibt.
- **Optionen:**
  - **(a) Beides behalten** — Ergebnisse geschützt **und** eigene Stoffe möglich; eine Tabelle
    mehr.
  - **(b) Nur Katalog und Wertekopie** — eine Tabelle weniger; der Weg „eigener Stoff ohne
    Katalogeintrag" entfällt.
- **Empfehlung des Papiers:** **Beides behalten** — „wer die Projektkopie streicht, spart eine
  Tabelle und verliert den Weg ‚eigener Stoff ohne Katalogeintrag'".
- **Folge bei Nichtentscheid:** Der Tabellenbestand von G6a ist unbestimmt; eine später
  nachgezogene Projekttabelle ist ein eigener Schemaschritt samt Kopier- und Transportwegen.
- **Fällig vor:** **G6a** (Schema).
- **Quelle:** [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 3.5, 4.2 und 10 (M14).

---

## 4. Datenaustausch gbXML und IFC — D1, D2, D4, D5, D6, D11, D16, D17 (entschieden)

Quelle: [`Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md`](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md),
Kapitel 11.1 („Jetzt zu entscheiden"). Das Papier sagt selbst: **Drei Antworten braucht es, um zur
Beauftragung zu werden** — die Reihenfolge (**D1**), ob der gbXML-Export ohne synthetische
Geometrie lohnt (**D2**) und wer das Gegenüber des IFC-Exports ist (**D6**). Die übrigen vier sind
technische Festlegungen, die **unwiderruflich** sind und deshalb einen Zeitpunkt tragen. Der
zweite Block (11.2) ist zur Kenntnis und steht in Kapitel 8 dieses Registers; sein Rest — der
Ablageort der gbXML-Schemakopie aus **D3** — ist seit der Prüfung vom 17.09.2026 der eigene Punkt
**D17**.
Der Leseweg selbst ist mit [ADR-004](ADR-004_gbXML_LINQ_to_XML.md) am 16.09.2026 entschieden
(E16). Mit **E27** (22.09.2026, N1.32) sind **alle acht Punkte entschieden**; D6 trägt die
Folgeaufgabe, das Gegenüber des IFC-Exports zu benennen.

### D1 — Reihenfolge: gbXML-Import vor IFC-Import?

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — ja — der gbXML-Import kommt vor dem IFC-Import. Der Anwender hat keine Präferenz genannt; es gilt der Vorschlag des Papiers.

- **Frage:** Wird der gbXML-Import **vor** dem IFC-Import gebaut?
- **Hintergrund:** gbXML ist die kleinere Aufgabe: kein Fremdpaket, keine Lizenzauflage, kein
  Ersatz für einen Geometriekernel. Jede Fläche trägt dort die Fläche samt Ausrichtung und
  Neigung, sodass der Import ohne Geometriekernel auskommt. Das Format liefert die thermische
  Topologie zuverlässiger — alle vier ausgezählten Dateien schreiben sie, während die
  IFC-Raumgrenzen eine selten exportierte Sicht voraussetzen. Beide Wege benutzen dasselbe
  Zuordnungsgerüst. **Dagegen** spricht die Praxislage: Die deutsche Normungsarbeit läuft auf IFC.
- **Optionen:**
  - **(a) gbXML zuerst** — schneller zum ersten funktionierenden Import; das Zuordnungsgerüst
    entsteht am billigeren Format.
  - **(b) IFC zuerst** — die Reihenfolge des Umsetzungskonzepts; passt zur Normungslage, kostet
    Paket, Lizenzarbeit und Geometrieersatz sofort.
- **Empfehlung des Papiers:** **Vorschlag ja, Entscheid beim Anwender.** „Die Frage hängt allein
  daran, **welche Dateien im Feld ankommen** — und das weiß nur der Anwender."
- **Folge bei Nichtentscheid:** Es bleibt bei der Reihenfolge des Umsetzungskonzepts, also
  IFC zuerst.
- **Fällig vor:** **der Beauftragung** von G4c bzw. G4a.
- **Quelle:** [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 10 und 11.1
  (D1).

### D2 — lohnt der gbXML-Export nur mit der zweiten Stufe?

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — ja, nach Empfehlung — der gbXML-Export kommt nur mit der zweiten Stufe (synthetische Geometrie).

**Vermerk E48 (26.09.2026, Konzept N1.53):** G7a ist vorab gebaut — eine **Bauabweichung**, keine Auslieferungsabweichung. Der Export steht hinter dem Freigabeschalter `GebaeudeExportRegeln.GbxmlExportFreigegeben`, der vor jeder Auslieferung aus ist, und wird samt Wiki und Logbuch erst mit G7b ausgeliefert; D2 bleibt als Auslieferungsregel stehen. Nutzen im Entwicklungsstand: das Rundlauf-Regressionsnetz Export ↔ Import und der Beleg- und Archivexport ([Protokoll G7a](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-26_G7a_gbXML-Export.md)).

- **Frage:** Lohnt sich der gbXML-Export nur zusammen mit der zweiten Stufe (synthetische
  Geometrie) — oder genügt die erste?
- **Hintergrund:** Ohne synthetische Geometrie ist der Export **ein Datenblatt in XML-Form** —
  legitim als Beleg-, Archiv- und Rundlaufformat, aber keine Interoperabilität: Die untersuchten
  Zielwerkzeuge lesen die geometriearme Form nicht, verlangen Polygonzüge oder reparieren zwar
  Polygone, erfinden aber keine. **Ausnahme:** Ist das benannte Gegenüber eines der beiden
  deutschsprachigen Programme, genügt die erste Stufe — dann ist aber vorher zu **belegen**, dass
  deren Importe eine geometriearme Datei annehmen; belegt ist das nicht.
- **Optionen:**
  - **(a) Beide Stufen zusammen** — der Export ist ein Simulationsmodell; höherer Aufwand.
  - **(b) Nur die erste Stufe** — Beleg- und Archivformat; nur sinnvoll, wenn das Gegenüber es
    annimmt, und das ist zu belegen.
  - **(c) Gar nicht** — kein gbXML-Export.
- **Empfehlung des Papiers:** **Ja** — beide Stufen zusammen oder gar nicht.
- **Folge bei Nichtentscheid:** Die Stufe G7 ist nicht beauftragbar; im ungünstigen Fall entsteht
  ein Export, den kein Zielwerkzeug liest.
- **Fällig vor:** **der Beauftragung** von G7.
- **Quelle:** [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 5.1, 10 und
  11.1 (D2).

### D4 — Ergebnisgrößen in kWh mit ausdrücklicher Einheit

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — ja, nach Empfehlung — Ergebnisgrößen im Export in kWh mit ausdrücklicher Einheit.

- **Frage:** Werden Ergebnisgrößen im Export in **kWh mit ausdrücklichem Einheitenattribut**
  geschrieben — oder in der Grundeinheit mit einem Hinweis?
- **Hintergrund:** Ohne ausdrückliche Einheitenangabe **behauptet die Datei die Grundeinheit**;
  der Empfänger liest dann Zahlen, die um Größenordnungen falsch sind. Die ehrliche Angabe kostet
  eine einmalige Helferfunktion und eine vollständige Einheitenzuweisung in der Datei.
- **Optionen:**
  - **(a) kWh mit ausdrücklicher Einheit** — der Empfänger sieht, was er liest; einmaliger
    Aufwand.
  - **(b) Grundeinheit mit Hinweis** — kein Umrechnen; der Hinweis wird beim maschinellen Lesen
    nicht gelesen.
- **Empfehlung des Papiers:** **kWh mit explizitem Einheitenattribut.** „**Unwiderruflich** — eine
  spätere Umstellung entwertet alte Exporte."
- **Folge bei Nichtentscheid:** Der erste gebaute Export legt es fest; jede Umstellung danach
  macht alle vorher erzeugten Dateien uneindeutig.
- **Fällig vor:** **der ersten Zeile Quelltext** des IFC-Exports (G7c).
- **Quelle:** [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 6.4 und 11.1
  (D4).

### D5 — deterministische Kennungen in beiden Formaten

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — ja, nach Empfehlung — deterministische Kennungen in beiden Exportformaten, von Anfang an.

- **Frage:** Werden die Kennungen der exportierten Objekte **deterministisch** aus dem
  Schlüsselpfad der IDs gebildet?
- **Hintergrund:** Nur mit gleichbleibenden Kennungen kann der Empfänger zwei Exportstände
  desselben Modells vergleichen, und nur so funktioniert der Rundlauf Export → Import. Kennungen
  aus **Namen** zu bilden bricht, sobald jemand einen Namen ändert — deshalb die Regel „aus den
  IDs, nie aus Namen". Sie deckt sich mit der Hausregel „neue Beziehungen über IDs, nicht über
  Textfelder".
- **Optionen:**
  - **(a) Deterministisch aus den IDs** — Modellvergleich und Rundlauf funktionieren;
    Voraussetzung ist ein festgelegter Schlüsselpfad.
  - **(b) Zufällig je Export** — einfacher; jeder Export ist für den Empfänger ein neues Modell.
- **Empfehlung des Papiers:** **Ja, von Anfang an**, aus dem Schlüsselpfad der IDs, nie aus Namen.
  „Nachträglich nicht mehr einzuführen."
- **Folge bei Nichtentscheid:** Der erste Export erzeugt Kennungen; alles, was danach kommt, kann
  nicht mehr damit verglichen werden.
- **Fällig vor:** **der ersten Zeile Quelltext** der Exporte (G7a und G7c).
- **Quelle:** [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 5.4, 6.4 und
  11.1 (D5).

### D6 — wer ist das Gegenüber des IFC-Exports?

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — nach Empfehlung — die semantische Stufe (G7c) wird zuerst gebaut. **Offen bleibt als Folgeaufgabe:** Das Gegenüber des IFC-Exports (Werkzeug, Zweck) benennt der Anwender, fällig vor der Stufe, die über die semantische hinausgeht.

- **Frage:** Welches Werkzeug, welcher Anwender, welcher Zweck ist das Gegenüber des IFC-Exports?
- **Hintergrund:** Das Papier sagt ausdrücklich: „**Diese Frage geht an den Anwender, nicht an die
  Technik.**" Ohne benannten Empfänger ist zwischen einem rein semantischen Export (das
  Betrachterfenster des Empfängers bleibt leer) und einem Export mit schematischen Körpern (der
  Betrachter zeigt etwas, und die Verwechslungsgefahr mit einem echten Gebäudemodell ist hoch)
  nicht sinnvoll zu wählen.
- **Optionen:**
  - **(a) Semantischer Export (G7c)** — Daten und Ergebnisse, keine Körper; der Empfänger sieht im
    Betrachter nichts und weiß, dass es kein Geometriemodell ist.
  - **(b) Zusätzlich schematische Körper (G7e)** — der Betrachter zeigt Quader und Platten; ohne
    deutliche Kennzeichnung hält der Empfänger sie für das Gebäude.
- **Empfehlung des Papiers:** **Zwischenweg: G7c bauen, G7e zurückstellen**, bis ein Empfänger
  benannt ist.
- **Folge bei Nichtentscheid:** Der Zwischenweg greift; G7e bleibt liegen. Das ist die vom Papier
  gewollte Vorgabe — kein Schaden, aber auch kein Fortschritt für die Körperstufe.
- **Fällig vor:** **G7e**; G7c geht auch ohne die Antwort.
- **Quelle:** [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 6.1, 10 und
  11.1 (D6).

### D11 — vertragliche Zulässigkeit der Rückgabe fremder Dateien

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — nach Empfehlung — die Rückgabe angereicherter fremder IFC-Dateien ist zulässig, mit Kennung in der Datei und Beipackzettel.

- **Frage:** Ist es zulässig, eine **fremde** IFC-Datei anzureichern und zurückzugeben?
- **Hintergrund:** Die einschlägige Austauschsicht sagt ausdrücklich, dass der Empfänger das
  Modell **nicht verändern** soll. Technisch ist der Weg billig — die Zuordnungstabellen stehen
  aus der Importstufe bereits. Rechtlich ist genau das der Grund, ihn zu lassen.
- **Optionen:**
  - **(a) Vorher klären und mit Auflagen bauen** — mindestens Beipackzettel, eigene
    Anwendungskennung, neuer Dateiname und ein Hinweis im Dialog, den der Anwender bestätigt.
  - **(b) Nicht bauen** — der Round-Trip entfällt; die Zuordnungstabellen bleiben trotzdem, weil
    sie auch die Herkunft tragen.
- **Empfehlung des Papiers:** **Vor der Round-Trip-Stufe zu klären, nicht danach** — mit den
  genannten Mindestauflagen.
- **Folge bei Nichtentscheid:** G7d ist nicht abnehmbar; das Papier macht die geklärte
  Vertragsfrage ausdrücklich zur Abnahmebedingung dieser Stufe.
- **Fällig vor:** **G7d**.
- **Quelle:** [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 6.6, 10 und
  11.1 (D11).

### D16 — erweitert die gbXML-Zonenbildung den Entscheid E7?

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — ja, nach Empfehlung — die gbXML-Zonenbildung erweitert E7 auf ein zweites Format.

- **Frage:** Wird der Entscheid **E7** (Mehrzonenmodell über den IFC-Import) auf ein **zweites**
  Format erweitert, sodass auch gbXML Zonen bildet?
- **Hintergrund:** E7 nennt für das Mehrzonenmodell den IFC-Import. Die Stufe G6c führt heute nur
  die IFC-Zonenregeln. Die gbXML-Zonenregeln sind **dieselbe Bauform auf denselben Tabellen** und
  laufen sinnvoll mit G6c mit; der Einzonen-Rückfall für gbXML gehört ohnehin in G4c. Der Zuwachs
  für G6c ist mit dem Entscheid zu beziffern — in den heute genannten 16–26 PT stecken die drei
  Regeln noch **nicht**.
- **Optionen:**
  - **(a) Ja** — gbXML wird mehrzonenfähig; G6c wächst um einen zu beziffernden Betrag.
  - **(b) Nein** — gbXML bleibt **dauerhaft einzonig**, und der Zuwachs von G6c entfällt.
- **Empfehlung des Papiers:** **Vorschlag ja.**
- **Folge bei Nichtentscheid:** Der Umfang von G6c ist unbestimmt, und G4c müsste offenlassen, ob
  es die Zonenregeln des Formats überhaupt vorbereitet.
- **Fällig vor:** **der Beauftragung** von G4c.
- **Quelle:** [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 3.3 und 11.1
  (D16); [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 9 (G6c);
  [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.12 (E7).

### D17 — Ablageort der gbXML-Schemakopie

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — (b), nach der Empfehlung dieses Registers — die gbXML-XSD liegt außerhalb des Repositoriums (`.gitignore`, Einrichtungshinweis, LIESMICH-Zeile mit Herkunft, Abrufdatum und Lizenzstand „keine"); der Validierungstest wird benannt übersprungen, wenn die Datei fehlt (Muster U8).

**Vermerk E48 (26.09.2026, Konzept N1.53):** Die Schemakopie ist beigestellt (F2) — `Referenzlaeufe/Schemakopien/GreenBuildingXML_Ver8.01.xsd`, 387 450 Byte, abgerufen am 26.09.2026, per `.gitignore` ausgeschlossen, daneben die versionierte `LIESMICH.md` mit Herkunft, Abrufdatum und Lizenzstand „keine"; Probe 3 läuft lokal, in der CI meldet sie den Verzicht ([Protokoll G7a](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-26_G7a_gbXML-Export.md), Abschnitt 4).

- **Frage:** Wo liegt die lokale Kopie des gbXML-Schemas (XSD), gegen die der Validierungstest des
  Exports läuft — im Testprojekt mit ausgeschriebener Begründung oder außerhalb des Repositoriums
  mit benannt übersprungenem Prüftest?
- **Hintergrund:** Das gbXML-Schema hat keine ausdrückliche Lizenz; es wird nicht ausgeliefert und
  nie aus dem Netz geladen, sondern dient allein der Validierung des Exports im Test (D3; E16:
  Schemaprüfung nur im Test gegen eine nicht ausgelieferte XSD-Kopie). Die vier gbxml.org-Beispiel-
  dateien kommen nicht ins Repositorium, die Prüfdateien erzeugt der eigene Exporteur. Offen ist
  allein, ob die XSD-Kopie im Repositorium liegen darf — die Vorsicht, die Befund R für die
  Beispieldateien anmeldet, gilt dem Buchstaben nach auch für ein Schema ohne Lizenz. In beiden
  Fällen gehört an den Ablageort eine LIESMICH-Zeile mit Herkunft, Abrufdatum und Lizenzstand
  „keine".
- **Optionen:**
  - **(a) Kopie unter `EPOS.Kern.Tests/`** mit ausgeschriebener Begründung, warum die Ablage im
    Repositorium keine Weitergabe an Kunden ist — der Validierungstest läuft in der CI.
  - **(b) Außerhalb des Repositoriums** (`.gitignore`, Einrichtungshinweis) — der Validierungstest
    wird benannt übersprungen, wenn die Datei fehlt; der Nachweis läuft lokal wie der
    Normfallnachweis (U8).
- **Empfehlung des Papiers:** Das Papier stellt beide Wege nebeneinander und verlangt den Entscheid,
  bevor die Kopie committet wird; dieses Register empfiehlt **(b)** nach dem Muster von U8 — kein
  lizenzloses Fremdschema in der Git-Geschichte, die Lücke im Gate benannt im Protokoll.
- **Folge bei Nichtentscheid:** Die Kopie darf nicht committet werden; die XSD-Validierung (Probe 3
  in Kapitel 8 des Papiers) läuft bis dahin nur lokal.
- **Fällig vor:** dem **ersten Commit der Schemakopie** — mit dem Testaufbau von G4c.
- **Quelle:** [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 1 (Punkt 10),
  8.2, 8.3 und 11.2 (D3); [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.21 (E16);
  [ADR-004](ADR-004_gbXML_LINQ_to_XML.md);
  [Prüfprotokoll](Gebaeudesimulation/2026-09-17_Pruefung_Konsistenz_Umsetzbarkeit.md) 5.1.

---

## 5. Softwarearchitektur — A1 bis A19 (entschieden)

Quelle: [`Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md`](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md),
Kapitel 6 („Offene Architekturentscheide"; das Papier steht in Rev. 4). **Neunzehn Fragen, davon
A7 und A8 (E16, E17) sowie A16 und A19 (E20) am 16.09.2026 entschieden und hier gekürzt.** Drei
davon — **A4 = U1**, **A5 = U3**, **A9 = U5** — liegen bereits unter einer Nummer des
Umsetzungskonzepts beim Anwender und stehen dort **nur als Sperrpunkt dieses Papiers**; sie sind
hier als Verweis geführt und werden unter ihrer U-Nummer entschieden (Kapitel 2). Ebenso stellt
das Papier **D1** und **D2** ausdrücklich nicht neu — sie stehen im Datenaustauschkonzept
(Kapitel 4).

Die Spalte „Ohne Entscheid blockiert" des Papiers ist hier die Zeile **Fällig vor**.

Mit **E27** (22.09.2026, N1.32) sind **alle fünfzehn Punkte entschieden** — zwölf unmittelbar,
A4, A5 und A9 über U1, U3 und U5 (Kapitel 2).

### A1 — Kaskade der Gebäudekinder gegen einen löschenden Schreibweg

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — (a) ja, nach Empfehlung — die Kaskade (Löschen/Neuanlegen) bleibt; der Schreibweg wird vor G3 gemessen, die Rettung an der Löschstelle eingebaut.

**Gemessen mit G3 (Welle B, 25.09.2026; Konzept N1.46): keine Rettung nötig.** Kein gewöhnlicher
Speicherweg löscht ein Gebäude und legt es neu an — die Gebäudeliste wird abgeglichen; Zonen fallen
nur beim Entfernen oder Tauschen des Gebäudes und beim Löschen des Projekts. Die Probe hält Löschen
(Zonen weg) und Speichern (Zonen unverändert) fest.

- **Frage:** Bleibt das kaskadierende Löschen der Gebäudekinder bestehen, obwohl der
  Gebäude-Schreibweg möglicherweise löscht und neu anlegt? Bei der Luftstromtabelle greift die
  Falle **doppelt**, weil sie zwei Eltern hat.
- **Hintergrund:** Löscht der Schreibweg das Gebäude und legt es neu an, nimmt die Kaskade alle
  Kinder mit — Zonen, Bauteile, Luftströme. Ob der Bestandsweg das tatsächlich tut, ist **nicht
  gemessen**; es ist ein begründeter Verdacht, und die Messstelle steht fest.
- **Optionen:**
  - **(a) Kaskade behalten, Schreibweg vor G3 messen** — die Rettung wird dort eingebaut, wo das
    Löschen steht; eine Probe prüft beide Fälle.
  - **(b) Ohne Kaskade, Waisen über ein Werkzeug entfernen** — verlagert die Verantwortung in ein
    Werkzeug, das beim Anwender nie läuft.
  - **(c) Schreibweg auf „Ändern statt Löschen" umbauen** — sauber, aber ein Eingriff in den
    Bestandsweg mitten in der Einfrierkette.
- **Empfehlung des Papiers:** **(a)** — Kaskade behalten, messen, die Rettung an der Löschstelle
  einbauen.
- **Folge bei Nichtentscheid:** G3 ist blockiert; die erste Zone kann nicht angelegt werden, ohne
  zu wissen, ob das nächste Speichern sie löscht.
- **Fällig vor:** **G3**.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A1),
  2.2 und 3.3.

### A2 — bleibt das IFC-Paket am Kern?

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — (a), nach Empfehlung — das IFC-Paket bleibt am Kern, die Naht `IGebaeudeLeser` wird von Anfang an gezogen.

- **Frage:** Bleibt das IFC-Paket am Rechenkern, oder zieht der Leser hinter eine Schnittstelle in
  ein eigenes Projekt?
- **Hintergrund:** [ADR-003](ADR-003_IFC_xBIM_ohne_Geometriekernel.md) bindet das Paket
  unverändert an den Kern. Das Risiko ist der iOS-Gerätebau (Q10 und U16 sind mit E18 entschieden, Nachweis in G4): Vermisst das
  Trimming dort Typen, muss der Leser umziehen. Mit einer von Anfang an gezogenen Naht kostet
  dieser Umzug **eine Fabrikzeile statt eines Umbaus**.
- **Optionen:**
  - **(a) Am Kern bleiben, Naht von Anfang an ziehen** — heute keine Mehrarbeit außer der
    Schnittstelle; der Ausweg bleibt billig.
  - **(b) Sofort eigenes Projekt** — iOS verlöre den Import von vornherein.
  - **(c) Ohne Naht binden** — spart eine Schnittstelle und macht jeden Umzug zum Umbau.
- **Empfehlung des Papiers:** **(a)** — „Am Kern bleiben (ADR-003), aber `IGebaeudeLeser` **von
  Anfang an** ziehen."
- **Folge bei Nichtentscheid:** Das Papier sagt: blockiert **nichts**; die Messung steht in G4 an.
  Wird die Naht aber nicht gezogen, ist der Ausweg später teuer.
- **Fällig vor:** **G4** (spätestens) — die Naht selbst gehört in den ersten Bauauftrag.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A2) und
  1.5; [ADR-003](ADR-003_IFC_xBIM_ohne_Geometriekernel.md);
  [Systementwurf](Systementwurf_Gebaeudesimulation_EPOS-Plan.md) 11.

### A3 — Name und Ordner des Zuordnungsdialogs

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — (a), nach Empfehlung — der formatfreie Name.

- **Frage:** Wie heißt der Zuordnungsdialog und wo liegt er? **Zwei geltende Papiere nennen
  verschiedene Namen und Ordner.** Dass es **ein** Dialog ist, ist im Datenaustauschkonzept
  entschieden.
- **Hintergrund:** Der Dialog trägt **zwei Formate** (IFC und gbXML) und erbt den Ablauf des
  Katalogimports. Ein Format im Namen einer Maske, die beide trägt, wäre eine Unwahrheit — und der
  Name wandert in Maskenschlüssel, Tests, Hilfe und Wiki, ist also später teuer zu ändern.
- **Optionen:**
  - **(a) Formatfreier Name im Importordner** (Vorschlag der Softwarearchitektur) — trägt beide
    Formate; der Ordner folgt der Sache.
  - **(b) Der formatgebundene Name im Bedarfsordner** (Vorschlag des Datenaustauschkonzepts) —
    behält den dort geführten Namen und führt die Zweiformatigkeit nur im Profil.
- **Empfehlung des Papiers:** **(a)**, der formatfreie Name.
- **Folge bei Nichtentscheid:** Zwei geltende Papiere nennen weiterhin zwei Namen; der bauende
  Auftrag entscheidet, und der andere Name bleibt in einem Papier stehen.
- **Fällig vor:** **G4**.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A3) und
  3.4; [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 2.4.

### A4 — ein Schreibweg im Katalogeditor (= U1)

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — über **U1** — ein Schreibweg im Katalogeditor ab G1.

- **Frage und Erläuterung:** siehe **U1** in Kapitel 2.
- **Empfehlung des Papiers:** „**= U1, Empfehlung dort: ja**, mit G1; ‚Speichern unter…' bleibt als
  nicht schließender Zweitknopf."
- **Folge bei Nichtentscheid:** **G1 blockiert** — „sonst hängen zehn Prüfregeln an drei
  Schreibstellen".
- **Fällig vor:** **G1**.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A4);
  entschieden wird unter U1.

### A5 — Platzhalter am Zahlenfeld (= U3)

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — über **U3** — `Platzhalter` am `Zahlenfeld`.

- **Frage und Erläuterung:** siehe **U3** in Kapitel 2.
- **Empfehlung des Papiers:** „**= U3, Empfehlung dort: ja**, rein additiv; zieht `StilblattTests`
  nach sich."
- **Folge bei Nichtentscheid:** **G1 blockiert** — der dritte Reiter bekommt rund zehn
  Vorgabefelder.
- **Fällig vor:** **G1**.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A5);
  entschieden wird unter U3.

### A6 — ein Aggregat je Gebäude, und Ändern statt Löschen

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — (a) ja, nach Empfehlung — ein Aggregat, Ändern statt Löschen, in einer Transaktion.

- **Frage:** Werden Zonen, Bauteile und Luftströme als **ein Aggregat je Gebäude** geschrieben oder
  je Zone einzeln — und schreibt das Aggregat durch **Löschen und Neuanlegen** oder durch
  **Abgleich über die Ids**?
- **Hintergrund:** Nur mit einem Aggregat bleibt der eine Schreibweg über vier
  Überlagerungsebenen **einer**; und nur mit dem Abgleich über die Ids bleiben die Ids stehen, an
  denen die Importzuordnung (ohne Kaskade) und die Luftstromtabelle (mit Kaskade an **beiden**
  Zonen) hängen. Ein Schreibweg, der löscht und neu anlegt, zerstört bei **jedem** gewöhnlichen
  Speichern die Importherkunft — und damit den Round-Trip.
- **Optionen:**
  - **(a) Ein Aggregat, Ändern statt Löschen** — Abgleich über die Ids in der Reihenfolge
    Entfernen → Ändern → Anlegen, alles in **einer** Transaktion.
  - **(b) Löschen und Neuanlegen je Gebäude** — einfacher zu schreiben, zerstört aber bei jedem
    Speichern die Importherkunft.
  - **(c) Je Zone schreiben** — der Bauteildialog müsste selbst schreiben, und „Abbrechen" auf der
    Zonenebene ließe geschriebene Bauteile stehen.
- **Empfehlung des Papiers:** **(a).**
- **Folge bei Nichtentscheid:** **G6b blockiert.** Wird (b) stillschweigend gebaut, geht die
  Importherkunft beim ersten Speichern verloren, ohne dass ein Test anschlägt.
- **Fällig vor:** **G6b**.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A6),
  1.4, 2.7 und 3.3.

### A7 — wird ADR-004 angenommen?

**Angenommen am 16.09.2026 (E16).** Der gbXML-Leseweg ist LINQ to XML mit handgeschriebenem
Modell; G4c hat seinen Leseweg. Konzept N1.21, Statusdatei Abschnitte 1 und 3; hier gekürzt.

### A8 — wird ADR-005 angenommen?

**Angenommen am 16.09.2026 (E17), mit Messpflicht.** Zonenkopplung über die
Nachbarraum-Randbedingung mit Durchlauf je Stunde, Zonen-Luftaustausch als Paare (M1, M4); G6b hat
sein Lösungsschema. Konzept N1.22, Statusdatei Abschnitte 1 und 3; hier gekürzt.

### A9 — zwei Gebäudespalten-Schritte zu einem (= U5)

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — über **U5** — ein Gebäudespalten-Schemaschritt.

- **Frage und Erläuterung:** siehe **U5** in Kapitel 2.
- **Zusatz dieses Papiers:** Die Schrittnummern, die das Konzept dafür nennt, sind **anderweitig
  vergeben**; die Papiere führen bis zur Beauftragung Buchstabenkürzel statt Zahlen (siehe A11).
- **Empfehlung des Papiers:** „**= U5, Empfehlung dort: ja** — 15 Spalten je Tabelle, ein
  Sichtneubau; der Klimaschritt bleibt getrennt."
- **Folge bei Nichtentscheid:** Der Schemaschritt der Gebäudespalten ist blockiert.
- **Fällig vor:** **G1**.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A9) und
  2.4; entschieden wird unter U5.

### A10 — zieht der Gebäudedialog mit G1 nach `EPOS.UI.Daten`?

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — ja, nach Empfehlung — der Gebäudedialog zieht mit G1 nach `EPOS.UI.Daten`.

- **Frage:** Zieht der Gebäudedialog schon mit G1 in die plattformfreie Hüllenschicht — oder erst
  mit G6?
- **Hintergrund:** Die Hülle wird mit G1 ohnehin neu geschnitten, weil der Dialog auf U·A je
  Bauteil umgebaut wird (E2). Der Importweg setzt einen **plattformfreien Schreibweg** voraus;
  ohne ihn bleibt der Import auf iOS benannt abgelehnt.
- **Optionen:**
  - **(a) Mit G1** — ein Schnitt statt zwei; der Importweg findet den Schreibweg vor.
  - **(b) Erst mit G6** — spart im ersten Schritt Arbeit, verdoppelt sie aber, und der Import
    bleibt auf iOS bis dahin benannt abgelehnt.
- **Empfehlung des Papiers:** **Mit G1.**
- **Folge bei Nichtentscheid:** **G4 blockiert** (plattformfreie Importhülle).
- **Fällig vor:** **G1**.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A10) und
  1.4; [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 2.8.

### A11 — Schemaschrittnummern jetzt vergeben oder erst bei Beauftragung?

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — (a), nach Empfehlung — Schemaschrittnummern erst bei Beauftragung.

- **Frage:** Werden die Nummern der Schemaschritte jetzt verbindlich vergeben — oder erst bei
  Beauftragung?
- **Hintergrund:** Der Schemastand steht im Transportmanifest eines Projektpakets. **Jede vergebene
  Nummer entwertet ältere Projektpakete**, weil sie einen Stand ankündigt, den es noch nicht gibt.
  Verbindlich sind schon heute **Reihenfolge und Inhalt** der Schritte; die Papiere führen bis zur
  Beauftragung Buchstabenkürzel.
- **Optionen:**
  - **(a) Erst bei Beauftragung** — die Nummern entstehen in der Reihenfolge, in der wirklich
    ausgeliefert wird; die Papiere führen bis dahin Kürzel.
  - **(b) Jetzt fest vergeben** — gibt allen Papieren feste Zahlen, erzwingt aber genau diese
    Auslieferungsreihenfolge.
- **Empfehlung des Papiers:** **(a) Erst bei Beauftragung.**
- **Folge bei Nichtentscheid:** Das Papier sagt: blockiert **nichts**; die Papiere führen bis dahin
  ihre Kürzel. Wird versehentlich (b) praktiziert, sind die Zahlen in mehreren Papieren
  nachzuziehen.
- **Fällig vor:** **der ersten Auslieferung** eines Schemaschritts der Gebäudesimulation.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A11) und
  2.4; [`ADR-001`](ADR-001_Schema-Ausrollung.md).

### A12 — wo erscheint der Produktausweis?

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — nach Empfehlung — der Produktausweis steht auf der Wiki-Seite und im Berichtskopf, im Wortlaut von E10.

- **Frage:** Erscheint der Produktausweis (der Satz, mit welchem Rechenkern und in welchem Prüfband
  gerechnet wurde) auf der **Wiki-Seite** und im **Berichtskopf**?
- **Hintergrund:** In den Exportdateien ist der Ausweis mit dem Datenaustauschkonzept bereits
  entschieden, im Mehrzonenfall je Zone. Offen ist die Anzeige im Produkt. Der Ausweis wechselt,
  sobald die Stufe G0 den einen noch offenen Testfall löst — sein Wortlaut ist also nicht ewig,
  aber er ist zu jedem Zeitpunkt eindeutig.
- **Optionen:**
  - **(a) Beides** — wer einen Bericht liest, erfährt ohne Umweg, womit die Zahlen entstanden sind.
  - **(b) Nur im Wiki** — der Leser eines Berichts erführe es nicht.
- **Empfehlung des Papiers:** **Beides**, im Wortlaut von E10, **unverändert und ohne
  Umschreibung**.
- **Folge bei Nichtentscheid:** Berichte gehen ohne Ausweis hinaus; die Wiki-Seite von G2 ist ohne
  ihn unvollständig.
- **Fällig vor:** **G1** (Berichtskopf) und **G2** (Wiki-Seite).
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A12) und
  4.3; [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 6.5;
  [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.15 (E10).

### A13 — Herkunftsspalten an der Gebäudetabelle?

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — (a) nein, nach Empfehlung — keine Herkunftsspalten an der Gebäudetabelle.

- **Frage:** Bekommt `Tab_Gebaeude` die Spalten für **Herkunft** und **Quellkennung**, wie Zone,
  Bauteil, Aufbau und Baustoff sie tragen?
- **Hintergrund:** Das Gebäude hat einen Katalogzwilling, an dem eine Importherkunft nichts
  bedeutet; die Regel der Spaltengleichheit zwischen Projekt- und Katalogtabelle zwänge die Spalten
  aber dorthin. Jede weitere Gebäudespalte ist außerdem ein weiterer Sichtneubau.
- **Optionen:**
  - **(a) Nein — Gebäudeherkunft nur in der Importzuordnung** — 15 neue Spalten, ein Sichtneubau.
  - **(b) Beide Spalten an beide Tabellen** — dann 17 statt 15 neue Spalten und ein **zweiter**
    Sichtneubau.
- **Empfehlung des Papiers:** **(a) Nein**, Regel „Gebäudeherkunft nur in `Tab_Importzuordnung`".
- **Folge bei Nichtentscheid:** **G4 blockiert** (der Herkunftsschritt); der Spaltenumfang des
  Schemaschritts ist unbestimmt.
- **Fällig vor:** **G4** (Herkunftsschritt).
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A13) und
  2.7; [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 7.3.

### A14 — was trägt den Umschalter Klassenweg → Bauteilweg?

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — (a), nach Empfehlung — der Umschalter Klassenweg → Bauteilweg folgt der Datenlage, der Übergang wird benannt.

**Umgesetzt mit G3 (Wellen W und D2, 25.09.2026):** Ein Gebäude mit genau einer Zone rechnet über
seine Bauteile, ohne Zone den Klassenweg; die Herleitungszeile „Rechenweg der Hülle" sagt in beiden
Stellungen, was gilt, und „Gebäude als eine Zone übernehmen" fragt vorher nach (mit E40 samt
Hochrechnung, Konzept N1.45).

- **Frage:** Woran erkennt der Rechenkern, ob er den Klassenweg oder den Bauteilweg rechnet — an
  der **Datenlage** (leere Zonentabelle heißt Klassenweg) oder an einem eigenen Wert?
- **Hintergrund:** Nach der Datenlagenregel ändert das Anlegen der **ersten** Zone die Zahlen eines
  Projekts, ohne dass jemand einen Schalter umgelegt hätte. Das ist für den Anwender überraschend
  und für die Referenzbasis ein Einfrieranlass genau dort, wo ein Referenzprojekt Zonen bekommt.
- **Optionen:**
  - **(a) Datenlage behalten, den Übergang benennen** — Rückfrage vor der ersten Zone,
    Herleitungszeile am Modellschalter in **beiden** Stellungen; Einfrieranlass nur dort, wo ein
    Referenzprojekt Zonen bekommt.
  - **(b) Dritter Persistenzwert am Modellfeld** — der Weg steht ausdrücklich in der Datenbank,
    kostet aber einen Wert, der beim Löschen der letzten Zone wieder falsch wäre.
- **Empfehlung des Papiers:** **(a).**
- **Folge bei Nichtentscheid:** **G3 blockiert** — der Übernahmeknopf entsteht dort, und ohne den
  Entscheid weiß niemand, was er auslöst.
- **Fällig vor:** **G3**.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A14);
  [Mehrzonenkonzept](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 4.3.

### A15 — was geschieht mit der letzten reinen Bestandsbasis?

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — (a), nach Empfehlung — ein Referenzprojekt mit `Gebaeude_Modell = TAGESBILANZ` bis GA in der jeweils aktuellen Basis; die GB-Arbeitskopie nur bis zum Merge G1 + G2; der Rückweg-Test umfasst nur dieses Projekt und endet mit GA.

- **Frage:** Was geschieht mit der letzten Referenzbasis, die noch ohne Stundenmodell entstanden
  ist und gegen die der Nachweis des Rückwegs (Tagesbilanz) läuft?
- **Hintergrund:** Ein Befund will sie aufheben; die Hausregel sagt dagegen: „frühere Basen liegen
  nicht mehr im Repositorium, gerechnet wird ausschließlich gegen die aktuelle Basis". Der
  Systementwurf empfiehlt denselben Ausweg wie die Softwarearchitektur.
- **Optionen:**
  - **(a) Ein Referenzprojekt, das bis zur Stufe GA auf dem Altweg steht** und in der jeweils
    **aktuellen** Basis mitgefroren wird — dann prüft jeder Lauf **beide** Wege gegen dieselbe,
    aktuelle Basis, und die neuen Reihen entstehen für dieses Projekt gar nicht erst. Die GB-Basis
    bleibt nur bis zum Merge von G1 und G2 und wandert dann mit ihrem Protokoll in die Geschichte.
  - **(b) Zwei Basen nebeneinander** — gegen die Hausregel; jeder Lauf müsste sagen, gegen welche
    er misst.
  - **(c) Ein Dateiausschluss im Vergleich** — löste es technisch, kostet aber einen neuen Schalter
    an einem Werkzeug, an dem die ganze Nachweiskette hängt.
- **Empfehlung des Papiers:** **(a)** — so auch der Systementwurf 8.4, dessen Fassung nach E26 gilt
  (F-Ü7): kein zweiter Basisordner; die gitignorierte Arbeitskopie gegen die GB-Basis nur bis zum
  Merge G1 + G2, danach ein Referenzprojekt mit `Gebaeude_Modell = TAGESBILANZ` in der jeweils
  aktuellen Basis; Umfang des Rückweg-Tests allein dieses Referenzprojekt, nicht alle Gebäude; der
  Test endet mit GA, dann geht das Projekt auf VDI 6007 über und die Basis wird neu eingefroren.
  **E23 (16.09.2026), präzisiert durch E26:** das Projekt bleibt bis zur Stufe GA auf dem Altweg
  (Konzept N1.28, N1.31). A15 ist mit E27 nach dieser Empfehlung entschieden.
- **Folge bei Nichtentscheid:** **G1 + G2 blockiert**: Der Einfrierschritt kann nicht abgenommen
  werden, weil unklar ist, wogegen der Rückweg künftig gemessen wird.
- **Fällig vor:** **G1 + G2** (der gemeinsame Einfrierschritt).
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A15) und
  2.8; [Systementwurf](Systementwurf_Gebaeudesimulation_EPOS-Plan.md) 8.4; Hausregel
  „Regressionsnetz" in [`CLAUDE.md`](../../CLAUDE.md);
  [Prüfprotokoll](Gebaeudesimulation/2026-09-17_Pruefung_Konsistenz_Umsetzbarkeit.md) 3.1 (F-Ü7).

### A16 — Zuschnitt des zweiten Verzweigungspunkts

**Durch E20 entschieden (16.09.2026): Es gibt keinen zweiten Verzweigungspunkt mehr.** Der
Tagesbilanz-Weg wird Zeichen für Zeichen in ein eigenes Modul `Altweg/` verschoben, der VDI-Weg ruft
nichts daraus; eine Weiche am Eingang der Gebäudebedarfsrechnung wählt das Modul, und was beide
brauchen (Bewohner, Skalierungsfaktor, Klimareihen), liefert ein modellfreier Vorbereitungsschritt.
Konzept N1.25, [ADR-006](ADR-006_Trennung_Altweg_VDI6007.md).

### A17 — Maskenschlüssel und Menüzeile für Import und Export?

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — (a) nein, nach Empfehlung — kein eigener Maskenschlüssel; Überlagerung im Gebäudedialog.

- **Frage:** Bekommen Gebäudeimport und ‑export einen **eigenen Maskenschlüssel und eine
  Menüzeile**? **Zwei geltende Papiere widersprechen sich:** Das Datenaustauschkonzept sagt nein,
  ein Bestandsbefund sagt ja.
- **Hintergrund:** Das Menü ist Daten, und ein Untermenü mit nur einem Punkt ist verboten. Ein
  eigener Maskenschlüssel kostet je Schlüssel mehrere Pflegestellen (Navigation, Hilfe, Wiki,
  Tests). Dagegen steht, dass ein Menüeintrag auch ohne geöffnetes Gebäude ein sichtbarer Einstieg
  wäre.
- **Optionen:**
  - **(a) Nein — Überlagerung im Gebäudedialog**, aufgemacht über Knöpfe in der Katalogleiste, je
    Format ein Profil; spart die Pflegestellen und macht die Untermenü-Regel gegenstandslos.
  - **(b) Zwei flache Menüzeilen auf **einen** Schlüssel mit Argument** (Muster des vorhandenen
    PV-Imports) — sichtbarer Einstieg auch ohne geöffnetes Gebäude.
- **Empfehlung des Papiers:** **(a) Nein.**
- **Folge bei Nichtentscheid:** **G4 blockiert**; zwei geltende Papiere behalten zwei Antworten.
- **Fällig vor:** **G4**.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A17) und
  3.1; [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 11.2 (D14).

### A18 — bleibt der Klimaweg eine eigene Klasse?

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — (a) ja, nach Empfehlung — der Klimaweg bleibt eigene Klasse, ausschließlich vom Eingangsbauer gerufen.

- **Frage:** Bleibt der Klimaweg des Gebäudemodells eine **eigene Klasse**, oder fällt er in den
  Eingangsbauer zurück?
- **Hintergrund:** Das Umsetzungskonzept sagt: „hier — und nur hier — fällt die Entscheidung über
  den Zeitbezug, die Azimutzuordnung und die Erdreichtemperatur", und U6 sagt „an der einen Stelle
  im Eingangsbauer". Genau diese Entscheidungen muss der Prüfmodus umschalten können; sie ohne den
  ganzen Eingangsbau prüfen zu können, spart in G1 die Messung, die U6 verlangt.
- **Optionen:**
  - **(a) Eigene Klasse behalten**, aber **ausschließlich** vom Eingangsbauer gerufen — „an einer
    Stelle" gilt dann als Aufrufstelle.
  - **(b) In den Eingangsbauer zurückfalten** — hält den Wortlaut buchstäblich, macht die Messung zu
    U6 aber teurer, weil jeder Probefall den vollen Eingang bauen muss.
- **Empfehlung des Papiers:** **(a).**
- **Folge bei Nichtentscheid:** **G1 blockiert** — die Klassenliste des Rechenkerns steht sonst
  nicht fest.
- **Fällig vor:** **G1**.
- **Quelle:** [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 6 (A18) und
  1.3; [Umsetzungskonzept](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 1.4 und 5 (U6).

### A19 — sieben Modellparameterfelder verstecken oder sperren (= U2)

**Durch E20 überholt (16.09.2026), mit U2.** Der Dialog ist in VDI-6007-Struktur aufgebaut, die
Modellparameter sind immer sichtbar; Altweg-Felder stehen im eingeklappten Abschnitt „Tagesbilanz (Bestandsweg)" eines
Altweg-Gebäudes. Festlegung in Kapitel 8; Konzept N1.25, ADR-006.

---

## 6. Kühlung — K1, K4 bis K12, K19 bis K24

Quelle: [`Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md`](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md),
Kapitel 12.1 („Jetzt zu entscheiden"). Das Papier entsteht aus Entscheid **E12** (Kühlung als
vierter Kanal) und trägt inzwischen auch **E15** (Wärmepumpen mit Kühlfunktion: Auswahl im Katalog
und Konfiguration je Anlage). Die Stufen sind **KU0** (Papiere), **KU1** (der Kanal, gemeinsam mit
G1 + G2), **KU2** (der Erzeuger) und **KU3** (das Umfeld).

Der zweite Block (12.2) sind technische Festlegungen, denen nur zu widersprechen ist; sie stehen in
Kapitel 8. **K20 bis K23** sind die vier Fragen, die mit **E15** und dem Gegenlesen vom
16.09.2026 hinzugekommen sind; das Papier führt sie in Kapitel 12.1 neben K1–K19. **K1** ist mit **E21** (Simulation Kältebedarf analog zur Wärmeseite, 16.09.2026) entschieden und hier gekürzt;
**K20** ist durch die Umsetzung erledigt (Prüfung 17.09.2026, F-K1) und hier gekürzt. **K24** ist
der Rest aus der Festlegung K18a (12.2) — die Ergebnisspalte des Kältestroms je Anlage —, seit der
Prüfung vom 17.09.2026 als eigener Punkt mit der Nummer aus dem Prüfprotokoll gezählt; das
Kühlkonzept selbst vergibt K24 in 12.2 für die Festlegung der Symmetrie (E21), die in Kapitel 8.2
dieses Registers steht.

Mit **E27** (22.09.2026, N1.32) sind **K10, K11, K19, K22 und K24** entschieden — **K10
abweichend von der Empfehlung** —, und die Festlegung **K2** (Kapitel 0 und 8.2) ist bestätigt.
Mit **E31** (23.09.2026, N1.36) sind **K4, K5, K6, K7 und K12** nach Empfehlung entschieden, mit
**E33** (23.09.2026, N1.38) **K8, K21 und K23** nach Empfehlung und **K9 abweichend von der
Empfehlung**; in diesem Kapitel ist kein Punkt mehr offen.

### K1 — vierter Kanal oder eigene Kältestruktur

**Durch E21 entschieden (16.09.2026): vierter Kanal in der bestehenden Kanalstruktur mit getrennter
Deckungsseite** — die Simulation des Kältebedarfs wird analog zur Simulation des Wärmebedarfs
gebaut (Fassade `SimulationKaeltebedarf`, Kanal `KUEHLUNG`, Kennzahlen, Bedarfsdialog, Deckung,
Bericht mit denselben Mustern). Konzept N1.26, Kühlkonzept Rev. 3, Statusdatei Abschnitt 1; hier gekürzt.

### K4 — Stellung der Kühlung in der Knappheitsreihenfolge

**Entschieden: E31 (23.09.2026, Konzept N1.36)** — nach Empfehlung (a): Die Kühlung steht zuletzt in der Knappheitsreihenfolge, und ihr Rang wird in der Oberfläche nicht zur Bearbeitung angeboten.

- **Frage:** Wo steht die Kühlung in der Reihenfolge, nach der bei Knappheit gedeckt wird?
- **Hintergrund:** Die Kälteseite hat eigene Erzeuger und eine eigene Deckungswelt; ein Rang in der
  Wärme-Knappheitsreihenfolge steuert für sie **nichts**. Ein Rang, der nichts steuert, darf in der
  Oberfläche nicht aussehen, als täte er es.
- **Optionen:**
  - **(a) Zuletzt, und in der Oberfläche nicht zur Bearbeitung angeboten** — ehrlich; der Anwender
    sieht keinen Regler, der nichts bewirkt.
  - **(b) Zuletzt, aber bearbeitbar** — gleiches Verhalten, aber ein Bedienelement ohne Wirkung.
  - **(c) Eingeordnet wie ein Wärmekanal** — vermischt die Deckungswelten.
- **Empfehlung des Papiers:** **Zuletzt**, und in der Oberfläche nicht zur Bearbeitung angeboten.
- **Folge bei Nichtentscheid:** Der Dialog zeigt entweder einen wirkungslosen Rang oder verschweigt
  ihn ohne Begründung.
- **Fällig vor:** **KU1**.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 4.5 und 12.1 (K4).

### K5 — bleibt Feuchte ausgeschlossen?

**Entschieden: E31 (23.09.2026, Konzept N1.36)** — nach Empfehlung (a): Die Feuchte bleibt ausgeschlossen, gerechnet wird sensible Kälte ohne Entfeuchtung, und die Grenze steht an jeder Kältezahl — Dialog, Bericht, Wiki und Export.

- **Frage:** Bleibt die Feuchte ausgeschlossen — also **sensible Kälte ohne Entfeuchtung**?
- **Hintergrund:** Entfeuchtung ist ein eigener Rechenweg mit eigener Datenlage. Ohne sie ist der
  ausgewiesene Kältebedarf in feuchtebelasteten Gebäuden zu klein — und diese Grenze muss an
  **jeder** Zahl stehen: Bericht, Dialog, Wiki und Export. Ein exportierter Kältebedarf ohne diesen
  Hinweis ist in fremder Hand eine falsche Zahl.
- **Optionen:**
  - **(a) Ja, ausgeschlossen, und die Grenze steht an jeder Zahl** — ehrlicher Umfang, überall
    kenntlich.
  - **(b) Entfeuchtung aufnehmen** — vollständiger, aber ein eigener Rechenweg mit eigener
    Datenlage; kein Bestandteil dieses Vorhabens.
- **Empfehlung des Papiers:** **Ja**, und die Grenze steht an jeder Zahl.
- **Folge bei Nichtentscheid:** Es bleibt beim Ausschluss — aber ohne die verlangte Kennzeichnung,
  und dann ist die Zahl missverständlich.
- **Fällig vor:** **KU1** (der Hinweistext gehört zu den Ressourcen dieser Stufe).
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 1.3, 9.2 und 12.1 (K5).

### K6 — Zonen, die gleichzeitig heizen und kühlen

**Entschieden: E31 (23.09.2026, Konzept N1.36)** — nach Empfehlung (a): nicht saldieren; Heiz- und Kühlkanal tragen je ihren Betrag, und eine Kennzahl weist den Fall aus.

- **Frage:** Wie werden mehrere Zonen auf **einen** Kanalwert geführt, wenn Zonen in derselben
  Stunde heizen und kühlen?
- **Hintergrund:** Eine Saldierung („Heizen minus Kühlen") **erfindet eine Wärmerückgewinnung**,
  die es im Gebäude nicht gibt: In Wirklichkeit laufen zwei Anlagen gegeneinander. Der Fall ist
  real (Südzone kühlt, Nordzone heizt) und tritt erst mit dem Mehrzonenmodell auf.
- **Optionen:**
  - **(a) Nicht saldieren** — beide Kanäle tragen ihren Betrag, und eine Kennzahl weist den Fall
    aus.
  - **(b) Saldieren** — eine einzige Zahl je Stunde; sie behauptet eine Rückgewinnung.
- **Empfehlung des Papiers:** **Nicht saldieren.**
- **Folge bei Nichtentscheid:** Im Mehrzonenfall entsteht eine stille, physikalisch falsche
  Gutschrift.
- **Fällig vor:** **KU1** (die Regel gehört in den Kanal, auch wenn der Fall erst mit Zonen
  auftritt).
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 3.5 und 12.1 (K6).

### K7 — Kältespeicher ja oder nein?

**Entschieden: E31 (23.09.2026, Konzept N1.36)** — nach Empfehlung (a): Der Kältespeicher wird nach KU3 vertagt, gemeinsam mit der Kältemaschine; bis dahin gibt es keinen Persistenzwert ohne Rechenweg (kein `VERWENDUNG_KAELTE`, keine Eingabe eines Kältespeichers). Die Ergebnisspalte `Entladung_Kuehlung` aus `KU-S4` bleibt leer, bis ein Kältespeicher rechnet.

- **Frage:** Bekommt die Kälteseite einen **Kältespeicher**?
- **Hintergrund:** Ein Speicher ohne Rechenweg wäre ein Persistenzwert, der nichts tut — genau das,
  was die Hausregeln vermeiden. Der Speicher gehört sachlich zur Kältemaschine, die ohnehin erst in
  der letzten Stufe entsteht.
- **Optionen:**
  - **(a) Vertagen nach KU3**, gemeinsam mit der Kältemaschine — und bis dahin **kein
    Persistenzwert ohne Rechenweg**.
  - **(b) Jetzt aufnehmen** — 3–5 PT, ein Pufferverwendungswert und ein Eintrag in der Klassenliste;
    ein Speicher, der erst später etwas tut.
- **Empfehlung des Papiers:** **Vertagen nach KU3.**
- **Folge bei Nichtentscheid:** Der Schemaumfang von KU1 ist unbestimmt, und es droht ein Wert ohne
  Rechenweg.
- **Fällig vor:** **KU1** (Schemaumfang) — gebaut würde er ohnehin erst in KU3.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 4.6 und 12.1 (K7).

### K8 — freie Kühlung und Rückkühlung: bauen oder benannt ablehnen?

**Entschieden: E33 (23.09.2026, Konzept N1.38)** — nach Empfehlung (a): bauen, aber keine als eigener Erzeuger. Die Rückkühlung ist Bestandteil der Kältemaschine — bei der reversiblen Wärmepumpe steckt sie in Maschine und Kennlinie, bei der Kältemaschine kommt sie mit dieser in KU3 —; die freie Kühlung ist ein Betriebsfall der vorhandenen Maschine (KU3); die Nachtlüftung ist eine Gebäudemaßnahme (G2).

- **Frage:** Werden freie Kühlung und Rückkühlung gebaut — und wenn ja, als eigene Erzeuger?
- **Hintergrund:** Ohne Rückkühlung ist eine Kältemaschine energetisch unvollständig: Die Abwärme
  muss irgendwohin, und der Aufwand dafür gehört zur Strombilanz. Die Nachtlüftung ist dagegen eine
  **Gebäudemaßnahme** und steht im Rechenweg **vor** der Kühlung; die freie Kühlung über die Quelle
  ist ein **Betriebsfall** der vorhandenen Maschine, kein eigener Erzeuger.
- **Optionen:**
  - **(a) Bauen, aber keine als eigener Erzeuger** — Nachtlüftung in G2, freie Kühlung als
    Betriebsfall, Rückkühlung als Bestandteil der Kältemaschine.
  - **(b) Als eigene Erzeugertypen führen** — mehr Einträge in Erzeugerliste, Deckungsreihenfolge
    und Wirtschaftlichkeit für Dinge, die keine Maschinen sind.
  - **(c) Benannt ablehnen** — die Kältemaschine bleibt energetisch unvollständig.
- **Empfehlung des Papiers:** **Bauen, aber keine als eigener Erzeuger.**
- **Folge bei Nichtentscheid:** Entweder fehlt der Rückkühlaufwand in der Strombilanz, oder die
  Erzeugerliste füllt sich mit Einträgen, die keine Erzeuger sind.
- **Fällig vor:** **KU2** (Rückkühlung) bzw. **KU3** (freie Kühlung); die Nachtlüftung gehört zu
  **G2**.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 3.4, 5.4 und 12.1 (K8).

### K9 — Tarif und Stromträger des Kältestroms

**Entschieden: E33 (23.09.2026, Konzept N1.38)** — **abweichend von der Empfehlung.** Der Kältestrom läuft per Vorgabe über denselben Stromträger und Tarif wie die Wärmepumpe im Heizbetrieb; wahlweise kann je Anlage ein anderer Stromträger des Projekts gewählt werden; ein leeres Feld (NULL) heißt „wie Heizbetrieb". Die Wahl steht an der Anlagenzeile neben dem Stromträger des Heizbetriebs (`Tab_Energieanlagen.Kuehl_ID_Carrier`, Verweis über die Kennung des Trägers) — dort, wo die Wärmepumpe ihren Stromträger im Bestand wählt (ET-5). Gegenüber der Empfehlung (a) kommt die Wahl je Anlage hinzu; wie ein abweichender Kühlträger bepreist und in den Emissionen bewertet wird, legt die Welle fest, die den Kältestrom in Kosten und Emissionen bringt (der Netzbezug wird heute einmal, mit dem Stromträger des Projekts, bepreist).

**Ergänzt: E34 (23.09.2026, Konzept N1.39)** — die Regel für einen abweichenden Kühlträger, je Anlage wählbar: **(1) anteilig am Netzbezug (Vorgabe)** — ein Netzanschluss, der Netzbezug jedes Zeitschritts wird nach dem Anteil des Kältestroms am Stromverbrauch aufgeteilt, dieser Anteil trägt Arbeitspreis und CO₂-Faktor des Kühlträgers; PV-Eigenverbrauch bleibt gemeinsam, der Leistungspreis beim Stromträger des Projekts; **(2) eigener Zähler** — der Kältestrom wird vollständig mit dem Kühlträger bepreist und bewertet und nicht aus PV-Eigenstrom gedeckt. Umgesetzt mit der dritten Welle von KU2; E34 öffnet und schließt keinen Punkt.

**Ergänzt: E35 (24.09.2026, Konzept N1.40)** — bei **eigenem Zähler** trägt der Kältestrom zusätzlich Grund- und Leistungspreis seines Kühlträgers: je Zähler (je Anlage — zwei Anlagen mit demselben Kühlträger sind zwei Zähler) einen Grundpreis und, wenn der Träger einen führt, den Leistungspreis auf die eigene Spitze des Kältestroms der Anlage, nach der Leistungspreisregel des Trägers (Staffel, Saisonreihe, Satz); anteilig am Netzbezug bleibt es bei E34. Umgesetzt mit der vierten Welle von KU2; E35 beantwortet die offen benannte Frage aus Kühlkonzept 6.2 und öffnet keinen Punkt.

- **Frage:** Trägt der Kältestrom denselben Tarif und denselben Stromträger wie der
  Wärmepumpenstrom?
- **Hintergrund:** Es ist physisch dieselbe Maschine an derselben Steckdose. Ein eigener Tarif wäre
  „eine zweite Wahrheit für dieselbe Steckdose" und müsste in Wirtschaftlichkeit und Emissionen
  getrennt geführt werden.
- **Optionen:**
  - **(a) Ja, derselbe Tarif und Träger** — eine Tarifzeile, eine Emissionszuordnung.
  - **(b) Eigener Tarif** — erlaubt getrennte Betrachtung, verlangt eine zweite Tarifzeile und
    verdoppelt die Pflege.
- **Empfehlung des Papiers:** **Ja.**
- **Folge bei Nichtentscheid:** Die Wirtschaftlichkeitsrechnung der Kälteseite ist nicht
  festgelegt; KU2 ist nicht abnehmbar.
- **Fällig vor:** **KU2**.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 6.1, 6.3 und 12.1 (K9).

### K10 — Kühlbetrieb als ausdrückliche Projekteinstellung

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — **abweichend von der Empfehlung.** Der Anwender legt in den Programmeinstellungen fest, ob **neue** Projekte mit eingeschalteter Kühlung angelegt werden; Vorgabe dieser Einstellung: aus. Bestehende Projekte (Bestandsprojekte, Referenzprojekte der Testdatenbank) bleiben aus, bis die Projekteinstellung ausdrücklich eingeschaltet wird; der Schutz der Referenzprojekte bleibt damit bestehen. Die Projekteinstellung selbst bleibt, je Projekt schaltbar. Umsetzung der Programmeinstellung über die vorhandene Schnittstelle `Dienste.Einstellungen`; fällig mit KU1. Gegenüber der Empfehlung („Ja — Vorgabe 0", allein als Projekteinstellung) kommt die Programmeinstellung für neue Projekte hinzu.

- **Frage:** Bleibt der Kühlbetrieb so lange aus, bis eine **ausdrückliche Projekteinstellung** ihn
  einschaltet?
- **Hintergrund:** Ein Kanal, der in jedem Projekt sofort rechnet, bewegt **alle** Referenzprojekte
  — Kanalsummen, Deckung, Wirtschaftlichkeit. Das Papier stellt ausdrücklich klar: Die Einstellung
  ist **nicht** dazu da, das Einfrieren zu vermeiden (der Einfrierschritt kommt ohnehin), sondern
  um die zwölf übrigen Projekte und jedes Bestandsprojekt des Anwenders zu schützen.
- **Optionen:**
  - **(a) Ja, Vorgabe „aus"** — Bestandsprojekte rechnen unverändert; wer Kühlung will, schaltet
    sie ein.
  - **(b) Immer an** — kein Schalter; jedes Bestandsprojekt ändert beim nächsten Lauf seine Zahlen.
- **Empfehlung des Papiers:** **Ja** — Vorgabe 0.
- **Folge bei Nichtentscheid:** Die Rückwärtsverträglichkeit aller Bestandsprojekte hängt in der
  Luft; der Einfrierschritt von KU1 würde weit mehr bewegen als das eine Referenzprojekt.
- **Fällig vor:** **KU1**.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 7.2, 10.5 und 12.1
  (K10).

### K11 — eigener Kühlsollwert und eigene Leistungsgrenze

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — ja, nach Empfehlung (a) — eigener Kühlsollwert mit Zeitprofil und eigene Kühlleistungsgrenze; nach dem Wortlaut der Empfehlung kommen Sollwert und Grenze in KU1, das Zeitprofil in KU3.

- **Frage:** Bekommt das Gebäude einen **eigenen Kühlsollwert** mit Zeitprofil und eine eigene
  **Kühlleistungsgrenze** — oder bleibt die vorhandene Maximaltemperatur die einzige Kühleingabe?
- **Hintergrund:** Die vorhandene Maximaltemperatur ist eine Überhitzungsschwelle, kein Sollwert
  einer Anlage; ohne Leistungsgrenze rechnet das Modell jede Stunde auf den Sollwert herunter, ganz
  gleich, wie groß die Maschine ist. Das Zeitprofil (Nachtwert) ist dagegen eine Verfeinerung.
- **Optionen:**
  - **(a) Eigener Sollwert und eigene Grenze in KU1, Zeitprofil erst in KU3** — der Kanal ist
    brauchbar, der Schemaumfang bleibt klein.
  - **(b) Alles sofort** — vollständiger; mehr Spalten und mehr Dialogfelder in derselben Stufe.
  - **(c) Nur die vorhandene Maximaltemperatur** — kein Schemaschritt; die Kälterechnung kennt
    keine Anlagengrenze.
- **Empfehlung des Papiers:** **(a)** — die übrigen Felder erst bei Bedarf.
- **Folge bei Nichtentscheid:** Schema- und Dialogumfang von KU1 sind unbestimmt.
- **Fällig vor:** **KU1**.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 7.1 und 12.1 (K11).

### K12 — gilt Kühlung auf iOS?

**Entschieden: E31 (23.09.2026, Konzept N1.36)** — nach Empfehlung (a): Die Kühlung gilt auch auf iOS; einen eigenen iOS-Lauf gibt es für KU1 und KU2 nicht, der Nachweis ist der Kern-Lauf.

- **Frage:** Gilt die Kühlung auch auf der iOS-Schale?
- **Hintergrund:** Der Rechenkern ist plattformfrei, und es entsteht **kein neuer
  Maskenschlüssel** — die Kühlung erscheint als Gruppe in vorhandenen Dialogen. Damit ist ein
  iOS-Lauf für KU1 und KU2 **nicht begründet**; die Hausregel verlangt für jeden macOS-Läufer
  ohnehin eine Rückfrage, und der Lauf zählt zehnfach.
- **Optionen:**
  - **(a) Ja, ohne eigenen iOS-Lauf für KU1/KU2** — die Kühlung ist auf beiden Plattformen da; der
    Nachweis führt der Kern-Lauf.
  - **(b) Ja, mit iOS-Lauf** — zusätzlicher Nachweis, Rückfragepflicht, zehnfaches Kontingent.
  - **(c) Auf iOS ausblenden** — eine Sonderbehandlung ohne technischen Grund.
- **Empfehlung des Papiers:** **(a) Ja.**
- **Folge bei Nichtentscheid:** Es besteht die Gefahr, dass ein iOS-Lauf ohne Anlass angefordert
  wird — entgegen der Hausregel zum Läufer-Kontingent.
- **Fällig vor:** **KU1**.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 8.6, 10.6 und 12.1
  (K12).

### K19 — eigener Einfrierschritt für KU2

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — ja, nach Empfehlung — eigener, kleiner Einfrierschritt für KU2.

- **Frage:** Wird KU2 mit einem **eigenen, kleinen Einfrierschritt** abgenommen — oder bleibt die
  Kältedeckung im Regressionsnetz unsichtbar?
- **Hintergrund:** Mit KU2 bewegt sich **ein** Projekt: das Referenzprojekt mit Kälteerzeuger. Ohne
  Einfrierschritt wird die Deckung nie gegen eine Basis gerechnet — dieselbe Begründung, mit der
  Q14 ein Referenzprojekt für das Gebäudemodell verlangt hat. Das Papier sieht dafür eine **fünfte
  Einfrierregel „gesäte Kältedaten"** vor, die an beide Orte gehört: in die Liesmich der
  Referenzläufe und in den Abschnitt „Regressionsnetz" der Wurzel-CLAUDE.md.
- **Optionen:**
  - **(a) Eigener Einfrierschritt** — ein Projekt bewegt sich, begründet und geprüft.
  - **(b) Kein eigener Schritt** — die Kältedeckung bleibt ungeprüft; jede spätere Änderung an ihr
    ist nicht messbar.
- **Empfehlung des Papiers:** **Eigener Einfrierschritt.**
- **Folge bei Nichtentscheid:** Die Kältedeckung ist dauerhaft ohne Regressionsnachweis.
- **Fällig vor:** **KU2**.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 10.4, 10.5 und 12.1
  (K19).

### K20 — Filter „nur Wärmepumpen mit Kühlfunktion" in der Katalogliste

**Durch die Umsetzung erledigt (Prüfung 17.09.2026, F-K1).** Der Katalog führt die Zahlenspalte
„Kühlleistung [kW]" filterbar und den Schalter „nur mit Kühlfunktion" über
`AUSDRUCK_MIT_KUEHLUNG`; weder die benannte Ausnahme noch eine eigene Filterart am Katalogfilter
ist mehr zu entscheiden, das Katalogfilter-Konzept stellt auf die Zahlenspalte um. Kühlkonzept
5.0.3 und 8.2, Prüfprotokoll 3.4; hier gekürzt.

### K21 — Kühl-Vorlauf: Auswahl aus den Stützstellen oder freie Eingabe

**Entschieden: E33 (23.09.2026, Konzept N1.38)** — nach Empfehlung (a): Auswahl aus den Stützstellen der Kühlkennlinie, keine Interpolation über den Vorlauf, ein leeres Feld heißt kleinster Stützwert, eine Extrapolation wird gewarnt wie auf der Heizseite, eine Stützstellenprobe je Vorlauf sichert es ab.

- **Frage:** Wird die Kaltwasser-Vorlauftemperatur der Anlage aus den **Stützstellen der
  Kühlkennlinie ausgewählt** — oder **frei eingegeben**, mit Interpolation zwischen zwei Vorläufen?
- **Hintergrund:** Der Wert wählt die Kennlinie, so wie der Heizvorlauf es auf der Wärmeseite tut;
  die Kennlinie ist über Vorlauf, Außentemperatur und Laststufe aufgespannt. **Ohne diesen Wert
  wählt die Rechnung die Kennlinie zufällig** — zwei Vorläufe ergeben zwei verschiedene
  Kälteverhältnisse bei derselben Außentemperatur; das Papier führt diesen Fall unter seinen
  Risiken. In der Testdatenbank stehen zwei Stützstellen. Die Vorgabe ist definiert: leeres Feld
  bedeutet den kleinsten Stützwert der Kennlinie.
- **Optionen:**
  - **(a) Auswahl aus den Stützstellen** — es kann nur ein Wert gewählt werden, den die Kennlinie
    trägt; keine Interpolation über den Vorlauf, und Kühl- und Heizseite teilen dieselbe Regel.
  - **(b) Freie Eingabe mit Interpolation** — vertraut wie andere Zahlenfelder; verlangt eine
    Interpolationsregel zwischen zwei Vorläufen und eine Regel für Werte außerhalb — beides hat die
    Heizseite heute nicht.
- **Empfehlung des Papiers:** **Auswahl.** „Eine Interpolation über den Vorlauf hat die Heizseite
  ebenfalls nicht, und **eine** Regel für beide Seiten ist mehr wert als ein Sonderweg." Dazu eine
  Extrapolationswarnung wie auf der Heizseite und eine Stützstellenprobe je Vorlauf.
- **Folge bei Nichtentscheid:** Dialogumfang und Extrapolationsregel stehen nicht fest, und es ist
  offen, ob Kühl- und Heizseite denselben Kennlinienleser teilen.
- **Fällig vor:** **KU2** (Erzeugerdialog und Kennlinienleser).
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 5.1, 8.2, 10.2, 12.1
  (K21) und 13.

### K22 — führt die COP-Spalte der Kühltabelle wirklich den EER?

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — (a), nach Empfehlung — vor KU2 wird an den vorhandenen Kühlkennlinien geprüft, ob die COP-Spalte das Kälteverhältnis führt, und das Ergebnis im Glossar festgehalten. **Offen bleibt als Folgeaufgabe die Prüfung selbst**, fällig vor KU2.

**Prüfung erledigt (23.09.2026, KU2 Welle 1; Ergebnis im [Glossar](Glossar_Lokalisierung.md),
Abschnitt 6):** Die Spalte `COP` der Kühlkennlinie führt das Kälteverhältnis (EER), nie das
Wärmeverhältnis. Am Importweg ist die Kennzahl der Kühlblöcke in 99,1 % der Wertzeilen der
Herstellerdateien die Kälteleistung durch die elektrische Leistungsaufnahme; die sieben
Katalogsätze der Testdatenbank (174 Zeilen) liegen in Kaltwasserlage und führen den EER des
Kühlbetriebs. **Befund:** 826 von 2 641 Kühlblöcken der Herstellerdateien liegen in Heizlage und
beschreiben die Kälteleistung am Verdampfer im Heizbetrieb — kein EER; der Import übernimmt sie
heute ungeprüft. Nach der Empfehlung (a) wird eine solche Lage beim Import benannt abgelehnt —
eine Aufgabe der Stufe KU2, kein neuer Entscheid.

- **Frage:** Führt die Spalte `COP` der Kühlkennlinie **wirklich das Kälteverhältnis (EER)** — oder
  in manchen Datensätzen das Wärmeverhältnis eines Heizbetriebs bei Kühlvorlauf?
- **Hintergrund:** Der Herstellerdaten-Import trennt Heiz- und Kühlblock, **die Herstellerangaben
  dahinter sind aber nicht gegengelesen**. Die Spalte heißt im Schema wie das Wärmeverhältnis und
  ist eine eingefrorene Spalte; das Papier legt dazu bereits fest, dass sie **nicht umbenannt**,
  aber in Kern, Dialog und Bericht als EER geführt und beschriftet wird — der eine Fall, in dem
  Spaltenname und Anzeigename bewusst auseinandergehen, und er gehört ins Glossar. Offen ist
  nicht die Beschriftung, sondern die **Datenlage**.
- **Optionen:**
  - **(a) Vor der Erzeugerstufe prüfen** — die vorhandenen Kühlkennlinien durchsehen (sieben
    Katalogsätze, 174 Zeilen) und das Ergebnis im Glossar festhalten; ist die Lage uneinheitlich,
    wird die Größe beim Import **benannt** umgerechnet oder der Satz abgelehnt.
  - **(b) Ungeprüft als EER lesen** — kostet nichts und macht aus einer verwechselten Kennzahl
    einen stillen Faktor in jeder Kältekennzahl.
- **Empfehlung des Papiers:** **(a) Vor KU2 an den vorhandenen Kühlkennlinien prüfen** und das
  Ergebnis im Glossar festhalten — „nie stillschweigend als EER gelesen".
- **Folge bei Nichtentscheid:** Jede Kältekennzahl und jede Wirtschaftlichkeitszahl der Kühlseite
  hängt an einer ungeprüften Annahme; „eine verwechselte Kennzahl ist hier ein Faktor, kein
  Rundungsfehler".
- **Fällig vor:** **KU2**.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 5.1, 6.4 und 12.1
  (K22); [Glossar_Lokalisierung.md](Glossar_Lokalisierung.md).

### K23 — Hilfsstrom je Anlage oder pauschal je Projekt

**Entschieden: E33 (23.09.2026, Konzept N1.38)** — nach Empfehlung (a): Der Hilfsstromanteil des Kältekreises steht je Anlage (`Kuehl_Hilfsstromanteil` an der Wärmepumpe); NULL heißt kein Zuschlag.

- **Frage:** Wird der Anteil Hilfsstrom des Kältekreises (Pumpen, Ventilatoren) **je Anlage**
  geführt — oder **pauschal je Projekt** in den Einstellungen?
- **Hintergrund:** Der Kältestrom einer Stunde ist die Kältemenge geteilt durch das
  Kälteverhältnis **zuzüglich** der Hilfsantriebe. Der Anteil hängt an der Hydraulik der Maschine,
  nicht am Projekt: Luft- und Solemaschinen haben verschiedene Hilfsantriebe. Die Vorgabe ist so
  gesetzt, dass ein leeres Feld **keinen** Zuschlag bedeutet — damit keine geratene Zahl entsteht.
- **Optionen:**
  - **(a) Je Anlage** — eine Spalte je Wärmepumpentabelle; wer den Wert kennt, trägt ihn ein, wer
    nicht, bekommt keine erfundene Zahl.
  - **(b) Pauschal je Projekt** — eine Spalte in den Einstellungen; ein Wert für alle Maschinen,
    der für jede gleich ungenau ist und wie eine Kenngröße aussieht.
- **Empfehlung des Papiers:** **Je Anlage** — „er hängt an der Hydraulik der Maschine, nicht am
  Projekt; NULL = kein Zuschlag, damit keine geratene Zahl entsteht".
- **Folge bei Nichtentscheid:** Der Ablageort der Spalte ist offen — und damit auch, ob eine
  Kältemaschine der letzten Stufe später dieselbe Eingabe erbt.
- **Fällig vor:** **KU2**.
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 5.0.5, 6.1, 7.3 und
  12.1 (K23).

### K24 — Ergebnisspalte des Kältestroms je Anlage

**Entschieden: E27 (22.09.2026, Konzept N1.32)** — nach Empfehlung: (a) der Kältestrom bleibt Skalar in der Kennzahlendatei, bis der Bericht ihn je Anlage verlangt; dann (b) im selben Schemaschritt wie `KU-S4`, nicht nachträglich (K18a).

- **Frage:** Reist der Kältestrom nur als Skalar in der Kennzahlendatei (`aggregate.csv`) — oder
  bekommt er eine Ergebnisspalte je Anlage in `Tab_ErgebnisWaermepumpe`, und wenn ja, in welchem
  Schemaschritt?
- **Hintergrund:** Der Kältestrom entsteht in KU2 in einer eigenen Reihe
  (`Stromverbrauch_Kuehlung_stuendlich`, F-K4) und geht an der benannten Stelle in die
  Stufenrechnung des Reststroms ein; Bericht und Wirtschaftlichkeit brauchen heute die Jahressumme
  je Projekt. Eine Ergebnisspalte je Anlage ist erst nötig, wenn der Bericht den Kältestrom je
  Anlage ausweist. Das Papier sagt: Eine siebte Ergebnisspalte gehört in denselben Schemaschritt wie
  `KU-S4` oder gar nicht — nachträglich kostet sie einen eigenen Schritt.
- **Optionen:**
  - **(a) Skalar bleibt** — kein Schemaanteil; der Bericht weist die Projektsumme aus.
  - **(b) Ergebnisspalte je Anlage mit `KU-S4`** — von Anfang an, im selben Schemaschritt wie die
    übrigen Ergebnisspalten der Kälte; ein Feld, das der Bericht dann auch trägt.
  - **(c) Später als eigener Schemaschritt**, wenn der Bericht die Spalte verlangt — ein weiterer
    Schritt und ein weiterer Einfrieranlass.
- **Empfehlung des Papiers:** **(a)**, bis der Bericht die Spalte verlangt; wird sie gebraucht, dann
  **(b)** im selben Schemaschritt wie `KU-S4`, nicht nachträglich (K18a).
- **Folge bei Nichtentscheid:** Der Kältestrom bleibt Skalar; ein späterer Berichtswunsch kostet
  einen eigenen Schemaschritt (c).
- **Fällig vor:** **KU2** (mit dem Schemaschritt `KU-S4`).
- **Quelle:** [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 7.6 und 12.2 (K18a);
  [Prüfprotokoll](Gebaeudesimulation/2026-09-17_Pruefung_Konsistenz_Umsetzbarkeit.md) 3.4 (F-K4)
  und 5.1.

---

## 7. Anlagenkopplung — H1 bis H12 (entschieden)

Quelle: [`Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md`](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md), Kapitel 13.1. Die zwölf Fragen sind am 16.09.2026 mit **E24**
(„H-Fragen sind entschieden — ok") sämtlich **nach Empfehlung des Papiers** entschieden, H1 mit **E25**
um die Wahl des Bandes ergänzt; die technischen
Festlegungen H-F1 bis H-F12 (13.2) sind damit zur Kenntnis genommen. Der Stufenplan ist **Q26**
(Kapitel 1, mit E27 entschieden; H6 gilt unverändert). Die Nummer dieses Kapitels bleibt stehen, damit Verweise gelten; die ausführlichen
Erläuterungen stehen im Papier, hier nur der Entscheid je Frage.

| Nr. | Entscheid (E24, 16.09.2026) | Wirkt in |
|---|---|---|
| **H1** | P-Regler mit Proportionalband, Vorgabe 1 K; `Xp = 0` fällt bitgleich auf die ideale Regelung zurück; **E25:** Band wählbar 0,5 K, 1 K, 2 K oder frei (0 bis 5 K) | AK1 |
| **H2** | Heizkurve in AK1 außentemperaturgeführt; die raumgeführte Korrektur erst in AK3 | AK1 |
| **H3** | Übergabe je Zone ab G6, je Gebäude davor; ein Vorlauf je Gebäude; mehrere Heizkreise benannt abgelehnt | AK1, Wirkung ab G6 |
| **H4** | Profilweg in AK2, der Speicher als Vorrat über die Sperrdauer; echte Kopplung erst AK3 | AK2 |
| **H5** | Komfortschwelle 1,0 K in der Nutzungszeit (Eingabe mit Vorgabe); drei Zahlen: Unterschreitungsstunden, Kelvinstunden, längste Strecke | AK2 |
| **H6** | AK3 wird jetzt nicht zugesagt; der Entscheid fällt nach einer Feldphase von AK1 und AK2 | AK3 |
| **H7** | Die Kopplung wirkt in beiden Läufen der Verhältnisrechnung (E8); die Nennleistung der Übergabe folgt bei NULL der skalierten Auslegungslast; feste Nennleistung wird im Bericht benannt | AK1 |
| **H8** | Wochenprofil als Spalte je Gebäude (Sollwertprofil) bzw. je Anlage (Zeitprogramm), 168 Werte, strenger Parser; keine Profiltabelle | AK1, AK2 |
| **H9** | Kälteseite in AK1, wenn KU2 den Kühl-Vorlauf liefert; sonst benannt vertagt. **E37** (24.09.2026, Konzept N1.42): Die Kälteseite bekommt eigene Spalten der Kühlübergabe am Gebäude samt Schalter `Kuehluebergabe_Aktiv`, gebaut mit der vierten Welle von AK1 | AK1 |
| **H10** | Auslegungs-Außentemperatur aus der Klimareihe hergeleitet als Vorgabe, ein Feld überschreibt; Herleitung steht im Dialog | AK1 |
| **H11** | Die Dialoggruppe heißt „Wärmeübergabe"; die Wärmesenke behält „Heizkreis" | AK1 |
| **H12** | Leerer `Heizung_Strahlungsanteil` bedeutet künftig „Vorgabe der Übergabeart"; Glossar, Herleitungszeile, Datenbankfall | AK1 |

---

## 8. Zur Kenntnis — technische Festlegungen und Wiedervorlage

Die folgenden Punkte **verlangen keinen Entscheid**. Die Papiere beantworten sie selbst; sie stehen
hier, damit der Anwender **widersprechen** kann, nicht damit er entscheiden muss. Nur die Nummer,
die Festlegung in einem Satz und die Stufe, bis zu der ein Widerspruch noch billig ist — die
Begründung steht im jeweiligen Papier (Datenaustauschkonzept 11.2, Kühlkonzept 12.2, Wiedervorlage
des Systementwurfs).

**Festlegung aus E20 (16.09.2026), zu widersprechen bis zur Beauftragung von G1:** Der Gebäudedialog und
seine Nachbarn (Katalog-, Skalierungs- und Bedarfsdialog) sind in **VDI-6007-Struktur** aufgebaut, die
Modellparameter immer sichtbar und bearbeitbar; Felder, die nur der Altweg liest, stehen in einem
eingeklappten Abschnitt „Tagesbilanz (Bestandsweg)", der nur bei einem Gebäude auf dem Altweg erscheint
(für die Dauer des Übergangs bis zur Stufe GA, E23, E26); der Schalter heißt „Rechenweg" mit Vorgabe
„VDI 6007" und Wert „Tagesbilanz"; ein
Gebäude auf dem Altweg trägt im Bericht „Tagesbilanz (Bestandsweg)" statt des Produktausweises
(Konzept N1.25, [ADR-006](ADR-006_Trennung_Altweg_VDI6007.md)).

**Aus der Anlagenkopplung (E22, E24):** Die zwölf technischen Festlegungen **H-F1 bis H-F12** stehen im
[Papier](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) Kapitel 13.2 und sind mit E24 zur Kenntnis genommen. Dasselbe Papier
fand das Feld `Nutzungszeit` der Erzeuger ohne einen einzigen Leser im Rechenkern und legt fest, dass
AK2 es nicht wiederbelebt; ob es entfällt, ist ein gewöhnlicher Aufräumpunkt. Die leserlosen Spalten
`WW_Bedarf` und `Waermebedarf` aus Befund X stehen dagegen in der Löschliste der Stufe GA
(Umsetzungskonzept 6, Q25).

### 8.1 Datenaustausch, technische Festlegungen (11.2)

Fundstelle: [`Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md`](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md),
Kapitel 11.2.

| Nr. | Gegenstand der Festlegung |
|---|---|
| **D3** | Schemakopie und Beispieldateien ohne Lizenz: Schema nicht ausliefern, nur als lokale Kopie im Test; die fremden Beispieldateien nicht ins Repositorium, Prüfdateien selbst erzeugen. Der Rest — wo die Schemakopie liegt — ist der offene Punkt **D17** (Kapitel 4) |
| **D7** | Die Round-Trip-Stufe zurückstellen, nicht streichen; die Persistenz der Zuordnung wird trotzdem in der Importstufe gebaut, weil sie auch die Herkunft trägt |
| **D8** | Je Format ein eigener Herkunftswert, nicht ein gemeinsamer mit dem Format eine Tabelle weiter |
| **D9** | Die Kennungsspalten formatfrei benennen, bevor sie entstehen; den Herkunftswertebereich erweitern und die Länge festlegen |
| **D10** | Ersatzschichtung beim Export mit Kennzeichnung, samt ausdrücklichem Vorbehalt: Sie trifft U-Wert und Gesamtwärmekapazität, **nicht die Lage der Masse im Aufbau** — wörtlich in der Bauteilbeschreibung der Datei und in der Meldung an den Anwender |
| **D12** | Der eine Luftwechselwert des Formats wird auf die Infiltrationsspalte gelegt, die Nutzerlüftungsspalte bleibt leer (= Wert des Gebäudes) |
| **D13** | Die erste gbXML-Zonenregel greift nur, wenn es weniger Zonen als Räume gibt — sonst entstünden aus einer Bürodatei über neunzig EPOS-Zonen |
| **D14** | Die Exporte erscheinen als Überlagerung im Gebäudedialog — kein neuer Menüpunkt, kein neuer Maskenschlüssel; zusätzlich ein Einstieg dort, wo die Ergebnisse liegen. **Siehe A17**, wo diese Festlegung einem Bestandsbefund widerspricht |
| **D15** | Größengrenze für gbXML, benannt abgelehnt statt versucht; **die iOS-Zahl ist zu messen** |

### 8.2 Kühlung, technische Festlegungen (12.2)

Fundstelle: [`Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md`](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md),
Kapitel 12.2.

| Nr. | Gegenstand der Festlegung |
|---|---|
| **K2** | Vorzeichen: Norm innen, Betrag außen — der Kanal führt positive Kältemengen. **Steht zusätzlich in Kapitel 0**, weil die Festlegung unwiderruflich in Persistenz und Kanalrechnung eingeht; mit **E27** (22.09.2026) bestätigt |
| **K3** | Eine externe Ganglinie darf den Kühlkanal tragen — Kältebedarf ohne Gebäudemodell |
| **K8a** | Umschaltung Heizen ↔ Kühlen **je Tag**, Mindestverweildauer ein Tag |
| **K8b** | **Eine** Teillastlogik für Wärme und Kälte, nicht zwei; die Kühlkennlinie zunächst über die höchste Laststufe — eine bewusste, benannte Vereinfachung |
| **K8c** | Keine Erdreichregeneration durch sommerliche Rückkühlung in der Erzeugerstufe — benannt vertagt |
| **K13** | Die Ergebnisspalten folgen dem Bestandsmuster der Kanalspalten |
| **K14** | Der Parser der Knappheitsreihenfolge wird tolerant, statt die gespeicherten Werte zu migrieren — ergebnisneutral |
| **K15** | **Eine** neue Kennzahlgruppe für die Erzeugergrößen der Kälte; die Kanalgrößen bleiben in der Kanalgruppe |
| **K16** | Die maximale Wärmelast bleibt **unberührt**; die Kälteseite bekommt ihre eigene Spitzen- und Bedarfsgröße. Die zwei Kanallisten bekommen eigene, nicht mit einer vorhandenen Kernklasse kollidierende Namen, und der Deckungsgrad der Kälteseite entsteht in einem **eigenen** Zweig statt als vierter Fall im Bestandszweig — „sonst legt der Kühlkanal jeden Wärmeerzeuger neu aus", und ein Deckungsgrad mit dem Wärmenenner lieferte eine Zahl statt eines Fehlers |
| **K17** | Eigener Reihenname für den Referenzlauf-Export, **bedingt** geschrieben: Der Vergleich kennt einen Schlüsselausschluss, aber keinen Dateiausschluss — eine Datei, die nur im neuen Lauf liegt, ist ein roter Vergleich ohne Schalter dagegen. Die bewusste Abweichung vom Bestandsmuster spart zwölf Reihen voller Nullen |
| **K18** | „—" statt 0 in Dialogen, Kacheln und Bericht; ein Projekt ohne Kühlung zeigt keine Kühlgruppe |
| **K18a** | Der Kältestrom reist zunächst als Skalar in der Kennzahlendatei, nicht als Ergebnisspalte je Anlage; ob und in welchem Schemaschritt die Spalte kommt, ist der offene Punkt **K24** (Kapitel 6) |
| **K24** (Nummer des Kühlkonzepts 12.2; nicht der offene Punkt K24 in Kapitel 6) | Symmetrie als Bauvorschrift (E21): jede Größe der Wärmeseite hat ein benanntes Gegenstück auf der Kälteseite oder steht in der Abweichungsliste; geprüft wird über die Liste, nicht über Zahlen |

### 8.3 Systementwurf, Wiedervorlage (Kapitel 11)

Fundstelle: [`Systementwurf_Gebaeudesimulation_EPOS-Plan.md`](Systementwurf_Gebaeudesimulation_EPOS-Plan.md),
Kapitel 11 („Was später zu überdenken ist"). Jeder Gegenstand trägt dort die Bedingung, an der er
hängt.

| Gegenstand | Hängt an |
|---|---|
| Auslegungsheizlast im Stundenmodell — eine Norm-Auslegungsheizlast ist eine andere Rechnung mit anderen Randbedingungen | einem eigenen Papier |
| Kühlung als eigener Kanal — **durch E12 überholt**, die Folgen regelt das Kühlkonzept | Erzeuger-, Speicher- und Wirtschaftlichkeitsrechnung |
| Die geerbten Textvergleiche im Umfeld des Gebäudemodells — benannter Bestand, **kein Umbauauftrag** | eine Altlastbehebung gehört nicht in einen Einfrierschritt, der dreizehn Projekte bewegt |
| Die zwei Projektbindungen des Gebäudes — wird benannt und dokumentiert; die Regel dazu ist festgeschrieben | — |
| Parallelität bei sehr großen Zonenprojekten | einem gemessenen Fall, der es verlangt |
| Ein eigenes Projekt für die Formatleser | dem iOS-Gerätebau (E18, **A2**) |
| Ob der Bauteilweg den Klassenweg vollständig ablöst | der Datenlage im Feld |
| Die dritte Umrechnungsnaht — benannt und in die Regel aufgenommen statt verschwiegen | der Einheitenregel des Kerns |
| Zwei Festlegungen von ADR-Gewicht ohne ADR: Ergebnisreihen als Datei statt als Tabelle, und ein Zuordnungsgerüst für beide Formate | Beauftragung von G4c bzw. des Ergebnisexports |
| Die Obergrenze von 50 Zonen — eine Setzung aus der Rechenzeit, kein Messergebnis (siehe **M12**) | einer Laufzeitmessung an einem echten Mehrzonengebäude |

### 8.4 Festlegungen aus der Prüfung vom 17.09.2026 (Widerspruch bis zur Beauftragung von G1 möglich)

Fundstelle: [Prüfprotokoll](Gebaeudesimulation/2026-09-17_Pruefung_Konsistenz_Umsetzbarkeit.md),
Kapitel 3 — dort steht je Festlegung die Herleitung aus den Befunden. Die 31 Festlegungen sind am
17.09.2026 getroffen worden, damit die Papiere in sich stimmen; **verbindlich ist die Fassung hier**,
Widerspruch bis zur Beauftragung von G1. Besonders zu prüfen sind die Festlegungen mit fachlicher
Wirkung: F-P4, F-K4, F-A3 und F-Ü7. Zahlen der Richtlinien stehen nicht in diesem Register.

| Nr. | Festlegung in einem Satz |
|---|---|
| **F-Ü1** | Der Vertrag des Vorbereitungsschritts steht an einer Stelle (Softwarearchitektur 1.3, Baustein `GebaeudeVorbereitung`): modellfrei sind allein Klimakalender, `VerbrauchNeu` je Einheit, `FlaecheAlt = Wohnflaeche_gesamt`, `Flaeche_Nutzer`, `Einheit` und `Jahresnutzungsgrad`; Bewohnerzahl und Skalierungsfaktor nach E8 entstehen je Modul aus dessen erstem Lauf, die Fassade führt die Schleife |
| **F-Ü2** | `IGebaeudeRechenweg.Rechnen` liefert aus einem Aufruf die Reihe und den unskalierten Jahreswert `VerbrauchAltKwh`; das VDI-Modul läuft einmal mit Skalierung als Nachmultiplikation in der Fassade, den Altweg ruft die Fassade im Verbrauchsfall zweimal wie im Bestand, und die Rückrechnung liegt hinter der Weiche |
| **F-Ü3** | Der Klimakalender hat zwei Teile — `Klimakalender.Gemeinsam` (`WE[365]`, `Stundentemperatur[8760]`, `WochentagJan1`, Monatsgrenzen) und `Klimakalender.Altweg` (`Sol_*`, `A_Temp`, `TagTyp_W/NW`) —, und die Weiche reicht dem VDI-Modul nur den gemeinsamen Teil, dem Altweg beide |
| **F-Ü4** | Die Wache heißt `Modultrennungswache` (Test `EPOS.Kern.Tests/ModultrennungswacheTests.cs`) und prüft, dass keine Datei unter `Gebaeude/` einen Bezeichner aus `Altweg/` oder eine Altweg-Datenquelle nennt, keine Datei unter `Altweg/` einen Bezeichner aus `Gebaeude/`, die Kältefassade `Altweg/` nicht nennt und die statische Ausbauprobe grün ist |
| **F-Ü5** | Die NULL-Vorgabe von `Fensterflaeche_Ost`/`_West` (je die Hälfte von `Fensterflaeche_Ost_West`) bildet der Vorbereitungsschritt, nicht das VDI-Modul, und nur für den Übergang; die Stufe GA füllt beide Spalten einmalig, bevor sie das Bestandsfeld entfernt |
| **F-Ü6** | Das VDI-Modul wirft keine Ausnahme, sondern legt eine Meldung der Stufe Fehler im Protokollkanal ab und gibt `false` zurück, der Lauf endet an derselben Stelle wie heute; ein Altweg-Gebäude ohne Tagesverteilung bricht wie heute den ganzen Lauf ab — als Wirkung benannt und offener Punkt **U17** |
| **F-Ü7** | Für den Rückweg-Test gilt Systementwurf 8.4: kein zweiter Basisordner, die Arbeitskopie gegen die GB-Basis nur bis G1 + G2, danach ein Referenzprojekt mit `Gebaeude_Modell = TAGESBILANZ` in der jeweils aktuellen Basis als einziger Gegenstand des Tests, der mit GA endet (Empfehlung zu **A15**, A15 bleibt offen) |
| **F-Ü8** | Die Wochenendmaske `WE[365]` bildet der Vorbereitungsschritt aus dem Wochentag des 1. Januar des Referenzjahres (Ortszeit-Kalender, Konzept 4.4, Q21), eine Probe hält sie gegen `Tab_Klimadaten.WE` derselben Region; **U7** bleibt offen und steht mit **U6** in Kapitel 0 |
| **F-Ü9** | `Werkzeuge/Auslieferungsvorlage` weist im Prüfbericht jede Zeile von `Tab_Gebaeude_STAMM` mit gesetztem `Gebaeude_Modell` aus; Vorgabe ist NULL |
| **F-Ü10** | Der Ergänzungsvermerk in ADR-002 beschränkt sich auf Entscheidung 3, Satz 3, und ersetzt diesen Satz durch den Hinweis auf E20 und ADR-006 |
| **F-S1** | Kein Papier nennt feste Schemaschrittnummern mehr: der Gebäudespalten-Schritt heißt **M3** (G1), der Klimaspalten-Schritt **M4** (G2); der Zielstand wird bei der Beauftragung an `SchemaStand.Zielversion` abgelesen (Stand 22.09.2026: 100, nächste freie 101), jede Zahl im Papier ist eine datierte Momentaufnahme |
| **F-S2** | M3 läuft in der Reihenfolge aus N1.24 (je Tabelle `RENAME COLUMN Wohnflaeche → Nutzflaeche`, neue Spalten, Sicht neu); `GebaeudeSchema.SQL_VIEW_NEU` ist ab M3 die einzige Quelle der Sichtdefinition, `sql/schema/002_views.sql` bleibt der eingefrorene Stand 61; zwölf (G1) plus drei (G2) Spalten je Tabelle, 30 Einträge; Bezugsfläche ist `Nutzflaeche` |
| **F-S3** | M4 legt keine Spalte `Windgeschwindigkeit` an (kein Leser, der äußere Wärmeübergang bleibt beim festen Vorgabewert); bei Gegenstrahlung NULL gilt Δθ_lw = 0 und α_str,A auf dem Vorgabewert (E5), die Schätzung nach Blatt 3 entfällt. **Stand 22.09.2026:** M4 (Stufe G2) ist durch Schemaschritt 95 vorweggenommen (Anwenderentscheid 19.09.2026) — `Tab_Solar` und `Tab_Solar_STAMM` führen `Gegenstrahlung`, `Luftfeuchte` und `Bedeckungsgrad`, `Tab_Klimaregion(_STAMM)` führen `Quelle` und `Importdatum`, Schemaschritt 97 bringt Szenario und Bezugsjahr; eine Windspalte gibt es nicht; die NULL-Regel gilt weiter, eine Schätzung aus dem Bedeckungsgrad wäre möglich, wird aber nicht gerechnet ([Konzept Klimadatenquellen](Konzept_Klimadatenquellen_TMY_TRY_EPOS-Plan.md)) |
| **F-S4** | Je Gebäude gibt es acht Kennzahlen, die achte `Ueberhitzungsstunden` [h] (Stunden der Nutzungszeit mit θ_op über `Maximaleraumtemperatur`, auch mit Kühlung — nicht über `Kuehl_Sollwert`; ohne wirksame Kühlung im freien Lauf, E32, Konzept N1.37); `JahresheizwaermeMwh` ist die Summe nach der Skalierung, `VerbrauchAltKwh` der unskalierte Wert des ersten Laufs; Temperaturreihen reisen im Referenzlauf-Export in °C, nur der Kühlbedarf in kWh; `GebaeudeModellErgebnis` bleibt `internal` mit `InternalsVisibleTo` für beide Referenzläufe und liegt in einem je Lauf gehaltenen Träger, den beide Fassaden lesen |
| **F-S5** | `IGebaeudeRechenweg` ist Vertrag **V16** des Systementwurfs (4.1, Bild 5) mit den Ausprägungen `Vdi6007Rechenweg` und `TagesbilanzRechenweg`; Softwarearchitektur 1.5 nennt die Naht |
| **F-S6** | Die Rechenzeit wird in G0 mit Kappung an θ_kuehl, gesetztem Φ_h,max und Φ_c,max getrennt ohne und mit Kühlung neu gemessen; Abnahmekriterium (4) in Konzept 10.4 wird in G0 aus der wiederholten Messung neu bestimmt und ist bis dahin informativ |
| **F-S7** | Kapitel 0 dieses Registers nimmt U6 und U7 auf; der Q26-Aufwand folgt Anlagenkopplung Rev. 2, 12.1/12.2 (AK0 1–2, AK1 10–15, AK2 11–15, AK3 23–38 PT, zusammen 45–70 PT); die A11-Liste der Papiernamen lautet GB, M2–M4, S-A bis S-G, KU-S1 bis KU-S4, AK-S1 bis AK-S3 |
| **F-S8** | Umsetzungskonzept Kapitel 4 ist die Quelle der verbindlichen Aufwände, Konzept 11 und 12 verweisen darauf (G1 10–16 PT, G2 3–5 PT); Kühlkonzept 11.1 nennt 16–22 PT, Mehrzonen 9 die Summe 40–62 PT ohne X1…X3 (D16) |
| **F-P0** | Die Rev.-1-Kapitel des Konzepts sind nachgezogen: Keller als θ_NR,eq mit `Kellertemperatur`, langwelliger Term als geometrischer Sichtfaktor mit Bewölkung nur über E_A, Validierung auf das Prüfband nach E10, Vorzeichen Heizen positiv und Kühlen negativ (allein die AixLib-Reihe von Fall 6 wird beim Einlesen gespiegelt) |
| **F-P1** | Fensterzweig nach E14: Schritt C bleibt bei fünf Knoten, Wand- und Fensterzweig der Außenwandgruppe werden zu einem R_Rest,AW/R_1,AW zusammengefasst (Gl. (27)/(28) mit den Klemmfällen der Richtlinie), A7a bildet R_AF mit benanntem Abbruch bei R_AF ≤ 0, E7 gewichtet θ_eq über alle Außenflächen einschließlich Fenster, A_AW = A_AW,opak + A_w geht in R_conv,AW, A_rad, Strahlungsverteilung und θ_op ein, die stationäre Probe 10.4 enthält den Fensterzweig; Wärmebrückenterm im masselosen Zweig, A_rad = min(A_AW,opak, A_IW) und die flächenproportionale Verteilung des Fenstersolareintrags sind benannte Abweichungen mit Messung in G0 |
| **F-P2** | Die internen Lasten werden im Eingangsbauer auf Luft und Oberflächen aufgeteilt (E3/E4); Schritt E gibt `ThetaOut`, `ThetaEq`, `PhiRadAW`, `PhiRadIW`, `PhiConv`, `ThetaSoll` und `ThetaMax`/`ThetaKuehl` aus |
| **F-P3** | Im Stundenschritt ist der Abschnittsdeckel (60) ein benannter Fehler mit Rechenprobe, der freie Lauf wird zweiseitig geprüft, Heiz- und Kühlanteil werden je Abschnitt getrennt akkumuliert, Schritt F kennt die fünf Betriebsfälle des Kühlkonzepts 3.2 mit θ_kuehl (NULL = `Maximaleraumtemperatur`) und Φ_c,max = 1 000 · `Kuehlleistung_Max`; die Kühlleistung wirkt in KU1 rein konvektiv am Luftknoten, ein Strahlungsanteil der Kühlübergabe kommt erst mit AK1 |
| **F-P4** | Die Sommerlüftung wird einmal je Stunde am Stundenbeginn mit θ_air und θ_out der Vorstunde ausgewertet und mit Hysterese 1 K und Mindestverweildauer 1 h über die ganze Stunde gehalten; die Schwelle bleibt 23 °C bis KU1, danach θ_kuehl − 3 K |
| **F-P5** | Projektläufe haben 30 Tage Vorlauf; die Normtests in G0 brechen deterministisch ab (Zustandsunterschied zweier Vorlaufwochen < 0,01 K, höchstens 12 Wochen, benannter Fehler); die Erdreichtemperatur bekommt in G1 eine Überladung von `ErdreichTemperatur.JahresprofilKollektor` mit ausdrücklicher Temperaturleitfähigkeit |
| **F-P6** | Ferien und Wochenende folgen Rechenschritte E8: 0 und 366 sind Aus-Marker, der Ferienfahrplan wirkt nur bei Ferien > 0,9 und θ_soll,Fer ≥ 1, benannt abgelehnt werden allein Tage außerhalb 1…365 in einem aktiven Fahrplan |
| **F-P7** | Die Rechenschritte bekommen ein Kapitel „Schritt H — Einschub der Anlagenkopplung (ab AK1)": vierter Betriebsfall „Übergabe begrenzt" mit Sekantenleitwert, Verletzungsmaß zweiseitig je Sättigungszustand, Leitwert im Regelbereich G = Φ_ue,max/Xp + y·G_H, θ_R und θ_m nach der Begrenzung neu gebildet, Verzweigung nach „Φ_verlangt ≤ Φ_ue,max und Xp = 0", Rechnung in W und W/K mit Umrechnung einmal im Eingangsbauer |
| **F-K1** | K20 ist durch die Umsetzung erledigt (Spalte „Kühlleistung [kW]" filterbar, Schalter „nur mit Kühlfunktion" über `AUSDRUCK_MIT_KUEHLUNG`); die Belege in Kühlkonzept 5.0 und 8.2 sind am Arbeitsbaum nachgemessen, das Katalogfilter-Konzept stellt auf die Zahlenspalte um |
| **F-K2** | Kühlkonzept 4.2 nennt die Stellen, die den Stundenzustand selbst aufbauen, und den Bivalenzpunkt (`RestSumme` über `KANAELE_WAERME`); `rest` wird über `KANAELE_WAERME` gefüllt; die Kacheln bleiben bei drei Wärmekanälen und die Kälte bekommt eine eigene Tabelle, `SchemaModell.cs` führt Badges und Kanten namentlich, `BerichtsDaten.KANAL_SCHLUESSEL` wird um `"KUEHLUNG"` erweitert |
| **F-K3** | Kältebeiträge gehen in `probeKaelte`, nicht in den Referenzakkumulator der Energieprobe; es gibt eine Bedarfsprobe Kälte in `SimulationKaeltebedarf` (Muster `Energieprobe`: Verletzungen zählen, größte Abweichung, eine Meldung der Stufe Fehler je Lauf, Lauf fehlgeschlagen) und eine Deckungsprobe Kälte in `SimulationControl.KanalganglinienProbe()`; „nie beide größer null" gilt je Gebäude bzw. Zone aus `GebaeudeModellErgebnis`, nicht auf Kanalebene, und zwar je **Abschnitt**: in keinem Abschnitt Heiz- und Kühlanteil zugleich; je **Stunde** ist beides bei einem Fallwechsel möglich, weil die Rechenschritte (7.1) beide Anteile je Abschnitt getrennt akkumulieren (F-P3); die Probe prüft die Abschnittsregel scharf und zählt die Stunden mit beidem als Hinweis, nicht als Fehler (Ergänzung 22.09.2026) |
| **F-K4** | Die Tagesbetriebsart der reversiblen Wärmepumpe gilt für den Heizkanal, der Brauchwasserkanal bleibt am Kühltag bedienbar (zuerst Brauchwasser, Rest Kälte); die Kältedeckung läuft in einer eigenen Stundenschleife `Kaeltekaskade` nach der Wärmekaskade in der Reihenfolge der Kaskadenplätze, gefiltert auf kühlfähige Erzeuger; Kälteerzeugung und Kältestrom in eigenen Reihen (`Kaelteproduktion_stuendlich`, `Stromverbrauch_Kuehlung_stuendlich`), der Kältestrom geht an der benannten Stelle in `SimulationControl` in die Stufenrechnung; Anlagen mit Quellspeicher werden in KU2 für Kühlung benannt abgelehnt; KU2 rechnet mit der Kennlinie der höchsten Laststufe (`MAX(Last)`), linear bei Teilauslastung, EER konstant; `KenndatenKuehlungCtrl` bekommt `Last`, zwei Prüfungen `HatKenndatenStamm`/`HatKenndatenProjekt`, `Kuehl_Vorlauf` als INTEGER, `KU-S3` mit `Kuehlbetrieb`, `Kuehl_Vorlauf`, `Kuehl_Hilfsstromanteil` (`Kuehl_Umschaltung` entfällt); `Kaeltebedarf_Gesamt` bleibt und ist heute wertgleich mit `Waermebedarf_Kuehlung` |
| **F-K5** | Der Ergebnisdialog zeigt die Kälte als dritten Block unter „Wärme | Strom", nur bei Kältebedarf > 0; `Konzept_Simulationsablauf` kommt in die Schwesterpapiertabelle |
| **F-K6** | Der Auslegungspunkt der Kühlübergabe kommt aus der Anlage (`Tab_WP.Kuehl_Vorlauf`, feste Spreizung 5 K) — als vierte benannte Abweichung in Anlagenkopplung 7.4, kein gebäudeseitiges Spaltenpaar |
| **F-A1** | Die Softwarearchitektur Rev. 4 kennt die Anlagenkopplung: `Waermeuebergabe` als Baustein in `Gebaeude/` (AK1), `Anlagenfahrplan` samt Naht `Anlagenverfuegbarkeit` neben den Fassaden (AK2), `Stundenrand` um Vorlauf, Übergabekennwerte und Reglerband erweitert, Stufen AK0–AK3 und KU0–KU3 in Kapitel 5, Einfrieranlässe in 2.8 |
| **F-A2** | Die AK2-Verteilung ist ein Zweipass (Pass 1 unbegrenzter Bedarf je Gebäude als Schlüssel je Stunde, Pass 2 mit verteilter Verfügbarkeit) mit den Randfällen aus `Kanalsatz.NetzverlusteVerteilen` (Summe ≤ 0: volle Schranke je Gebäude; Rundungsrest nicht dem letzten Gebäude), einer zweiten Verteilungsstufe auf die Zonen nach demselben Schlüssel und benannter Pfadabhängigkeit bei Altweg-Gebäuden |
| **F-A3** | In AK3 ist das Produkt Zonendurchläufe × Anlagendurchläufe je Stunde auf 120 begrenzt; darüber gilt die Stunde als nicht konvergiert (benannte Meldung, letzter Stand); der Grenzfall 50 × 50 × 20 ist in 6.3 ausgerechnet |
| **F-A4** | Zwei Aufzählungen im ganzen Papier gleich: `Verfuegbarkeitsgrund` (Anlagenseite) und `Begrenzungsgrund` (Gebäudeseite: KEINE_BEGRENZUNG, UEBERGABE, HEIZLEISTUNG_MAX, VERFUEGBARKEIT, UMSCHALTUNG); `Tab_Einstellungen.Anlagenkopplung TEXT(4)` mit `CHECK IN ('AUS','AK1','AK2','AK3')`, NULL = AUS; AK-S1 = 13 Gebäudespalten (26) + 1 Projektspalte = 27 Einträge; in `Tab_Zone` allein `Uebergabe_Art`, `Uebergabe_Exponent`, `Uebergabe_Leistung_Nenn`; die Kennlinienwahl der Wärmepumpe nimmt je Stunde den gerechneten Vorlauf des versorgten Gebäudes (mehrere Gebäude: bedarfsgewichtetes Mittel, +1–2 PT); die Kälteseite von AK1 setzt KU1 und KU2 voraus |
| **F-M1** | `Tab_Zone` entsteht mit G3 (S-A bis S-C), `Tab_Zonenluftstrom` und `ID_Nachbarzone` mit S-G in G6b, G6a legt `Tab_Bauteilaufbau(_STAMM)` an; die Zonenspaltentabelle in Mehrzonen 4.2 bekommt einen Block „Spalten aus KU-S1 und AK-S1, sofern diese Stufen stehen", den S-C dann anlegt; Einfrierregeln werden benannt, nicht durchgezählt. *Umgesetzt mit G3 (25.09.2026) mit einer Abweichung:* auch `Tab_Bauteilaufbau(_STAMM)` entsteht mit G3 (Schritt 133, Softwarearchitektur W1), G6a legt keine Tabelle mehr an (Konzept N1.46) |
| **F-M2** | Bei N = 1 entfallen adiabater Vorlauf und Konvergenzprobe, Probe 10 bleibt bitgleich; der Schreibweg ist ein Abgleich über die Ids in einer Transaktion (Entfernen → Ändern → Anlegen, Muster A6), nicht Löschen und Neuanlegen; ADR-005 nennt die Rechenzeit „rund 1,1 bis 2,0 s je Gebäude und Jahr" mit Verweis |
| **F-D1** | Konzept 7.4 nennt den Paketzuschnitt aus ADR-003 ohne die Klammer mit dem Metapaket; ADR-003 bekommt die fünfte Kraft und die Aufgabe „Paketgröße der iOS-App messen (mit und ohne Schema-Assemblies, ios-arm64, getrimmt)"; der Basis-Name im Zustandsbild lautet ohne Nummer „aktuelle Basis nach `Referenzlaeufe/LIESMICH.md`" |

---

## 9. Wie ein Entscheid festgehalten wird

**Zwei Orte, immer beide.** Der ausführliche Entscheid kommt als **Nachtrag N1.x** in
[`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
— mit dem Wortlaut des Anwenders, dem Entscheid, den berührten Stellen und, wo nötig, der
Aufhebung des bisherigen Stands; die **eine Zeile** je Entscheid kommt in Abschnitt 1 von
[`Status_Gebaeudesimulation_VDI6007.md`](Status_Gebaeudesimulation_VDI6007.md); die
Konzeptfragen Q24, Q25 und Q26 stehen dort in derselben Tabelle, seit E27 mit dem Stand „entschieden". Betrifft der Entscheid ein
Architekturpapier, wird dessen Fragentabelle im selben Schritt nachgezogen; betrifft er einen
**ADR**, wechselt dort die Kopfzeile **Status** von „Vorgeschlagen" auf „Angenommen" mit Datum,
und die Papiertabelle der Statusdatei sowie die Indexzeile in
[`Dokumentation/LIESMICH.md`](../LIESMICH.md) folgen.

**Dieses Register wird im selben Schritt gekürzt** — der entschiedene Punkt verschwindet hier und
steht fortan im Nachtrag und in der Statusdatei; ist der letzte Punkt eines Kapitels entschieden,
entfällt das Kapitel. **Abweichend davon** stehen die mit **E27** und **E28** (22.09.2026), die mit
**E31** und **E33** (23.09.2026) und die mit **E38** (24.09.2026) entschiedenen Punkte mit dem Vermerk
„Entschieden: E27 (22.09.2026, Konzept N1.32)", „Entschieden: E28 (22.09.2026, Konzept N1.33)",
„Entschieden: E31 (23.09.2026, Konzept N1.36)", „Entschieden: E33 (23.09.2026, Konzept N1.38)" bzw.
„Entschieden: E38 (24.09.2026, Konzept N1.43)" weiter in ihren Kapiteln, weil ihre
Erläuterung die Begründung der Stufenaufträge trägt und drei von ihnen eine Folgeaufgabe haben
(U6 — mit E29 erledigt —, K22 — mit der Prüfung vom 23.09.2026 erledigt —, D6). Führt der Entscheid zu einer für den Anwender sichtbaren Funktionsänderung,
wird nach der Hausregel ein Eintrag im Wiki-Update-Logbuch **entworfen** und die Versionsnummer
beim Anwender erfragt; die Veröffentlichung läuft gebündelt.

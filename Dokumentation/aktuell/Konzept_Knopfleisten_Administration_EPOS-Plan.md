# Konzept: Knopfleisten der Administrationsdialoge nach der Hausregel (DL-2)

**Stand:** Mockups abgenommen, alle sechs Fragen entschieden (Abschnitt 3); welcher Schritt
umgesetzt ist, steht in der Tabelle der Umsetzungsreihenfolge (Abschnitt 4).
**Anlass:** Anwenderentscheid 19.09.2026 — „Prüfe alle Dialoge [des Administrationsmenüs] auf
Überlappung und Übersichtlichkeit/Anordnung Buttons." Entscheid: alle abweichenden Dialoge an die
Hausregel angleichen, je Dialog ein Mockup zur Abnahme; umgebaut wird erst danach. Die Überlappung
ist getrennt behoben (KL-5, gemeinsame Ursache im Katalograhmen).
**Mockup:** `Dokumentation/aktuell/Mockups/Knopfleisten_Administration_DL2.html` — je Komponente
„heute" und „Vorschlag" als schematische Leisten.
**Geltungsbereich:** nur die Anordnung der Knöpfe. Kein Rechenweg, kein Schema, kein Kern.


## 1 Die Hausregel in drei Sätzen

Quelle: [`EPOS.UI/CLAUDE.md`](../../EPOS.UI/CLAUDE.md), Abschnitt „Bedienung".

1. **Jeder Dialog trägt genau eine Fußleiste** unter dem Inhalt, in der Reihenfolge
   **Aktionen · Füller · Abbrechen · OK (primär)** als `SpeichernLeiste`: OK prüft, speichert und
   schließt; Abbrechen, Esc und ✕ verwerfen ohne Prüfung; ein nicht schließender Knopf
   „Anlegen"/„Übernehmen" läuft über `MitSpeichern="true"` in derselben Leiste.
2. **Geschrieben wird nur im OK-Weg:** Bis dahin führt der Dialog einen Arbeitsstand — sonst wäre
   Abbrechen eine Behauptung, die nicht stimmt.
3. **Ein Katalogdialog ohne Arbeitsstand** (die Bearbeitung schreibt sofort in den Katalog) bietet
   kein Abbrechen an, sondern einen primären **„Beenden"**-Knopf am Ende:
   Speichern · Füller · Neu · Löschen · Beenden (`ModulKatalogDialog`) bzw.
   Speichern · Füller · Neu · Bearbeiten · Löschen · OK (`KatalogBrowserDialog`).

**Leseregel der Fußleiste,** aus den konformen Vorbildern (`KlimadatenDialog`, `ModulKatalogDialog`,
`KatalogBrowserDialog`) abgelesen: **links vom Füller** stehen die Knöpfe, die auf den Eingabeblock
oder eine Ansicht wirken (Speichern, Import, Grafik, Kennlinien), **rechts vom Füller** die Knöpfe, die
auf die Liste wirken (Neu, Bearbeiten, Löschen), und ganz rechts der eine Schlussknopf. Der Schlussknopf
ist der einzige primäre Knopf der Maske. Eine Knopfzeile **in einer Spalte oder einem Reiterblatt**
(die Regionsknöpfe des Klimadatendialogs, die Pfeile der `Zweispaltenauswahl`) gilt nicht als zweite
Fußleiste, solange sie keinen primären und keinen schließenden Knopf trägt. Ein Knopf, der zu einem
Feld gehört („Minimale haltbare Schwelle ermitteln" neben der Zielschwelle, „Neu" der Eingabezeile
in `NutzungsdauerDialog`), bleibt im Blatt.

**Baustein-Voraussetzung:** `SpeichernLeiste` kennt heute nur Status · [Speichern] · Abbrechen · OK.
Für „Aktionen · Füller · Abbrechen · OK" bekommt sie einen linken Aktionsschlitz (`RenderFragment
Aktionen`, vor dem Statustext). Das ist ein Baustein, einmal gebaut, mit bunit-Test; drei der zehn
Komponenten brauchen ihn (Gebäude im Projektmodus, Einstellungen, Kosten).


## 2 Die zehn Komponenten

| # | Komponente (Menüpunkte) | Heute | Vorschlag | Risiko | Aufwand | Logbuch-Satz |
|---|---|---|---|---|---|---|
| 1 | `Bedarf/GebaeudeDialog.razor` (Gebäude → Bearbeiten) | Katalogspalte: Ändern… · Neu… · Löschen · Gebäudetyp…; Detailblock: Ändern · Simulation… (nur Projekt); Fuß: OK · Abbrechen | **Verwaltung:** Katalogspalte Neu… · Ändern… · Löschen · **Gebäudetyp in DB ändern…** (bleibt, Listenleiste); Fuß **Füller · Beenden**. **Projekt:** Fuß **Ändern… · Simulation… · Gebäudetyp in DB ändern… · Füller · Abbrechen · OK**; Detailblock ohne Knöpfe. Der Typknopf bleibt an **beiden** Stellen (DL-Q1) | OK/Abbrechen der Verwaltung waren eine Behauptung (jede Katalogaktion schreibt sofort); im Projekt wandern drei Knöpfe in den Fuß | M | Die Gebäudeverwaltung schließt mit „Beenden"; im Projektdialog stehen „Ändern" und „Simulation" in der Schlussleiste. |
| 2 | `Bedarf/GebaeudetypDialog.razor` (Gebäudetypen) | Typ hinzufügen · Typ Löschen · **Typ Speichern** (primär) · OK | **Typ speichern · Füller · Typ hinzufügen · Typ löschen · Beenden** (primär) | Die Hervorhebung wandert von Speichern zu Beenden; Speichern bleibt gesperrt, solange kein änderbarer Typ gewählt ist | S | Die Gebäudetypen-Verwaltung ordnet ihre Knöpfe wie die übrigen Kataloge: Speichern links, Beenden rechts. |
| 3 | `Bedarf/BedarfAdminDialog.razor` (Brauchwasser, Prozesswärme, Stromverbraucher) | Aktionsleiste Ändern · Neu · Typ ändern · Löschen · Grafik ohne Primär; darunter OK · Abbrechen | **Grafik… · Typ ändern… · Füller · Neu… · Ändern… · Löschen · Beenden** (primär); keine zweite Leiste | OK/Abbrechen waren eine Behauptung (Stammkopf, Profil und Löschen schreiben sofort); der Weg „Abbrechen" entfällt, Esc/✕ schließen wie Beenden | S | Die Verwaltungen Brauchwasser, Prozesswärme und Stromverbraucher schließen mit „Beenden"; OK und Abbrechen entfallen. |
| 4 | `Waermepumpe/WaermepumpeStammDialog.razor` (Wärmepumpen) | Kenndaten · Speichern · Neu · Löschen · Beenden, ohne Füller | **Speichern · Kennliniendaten… · Füller · Neu · Löschen · Beenden** | Nur die Anordnung ändert sich; kein Knopf fällt weg | S | Die Wärmepumpen-Verwaltung ordnet ihre Knöpfe wie die übrigen Kataloge. |
| 5 | `Strom/PeakShavingDialog.razor` (Lastspitzenkappung) | „Berechnen" primär allein im Blatt; Fuß CSV-Export · In Variante übernehmen · Füller · Schließen | **Variante (a):** „Berechnen" bleibt im Blatt zwischen Parametern und Ergebnis, ohne Primärfarbe; Fuß **CSV-Export · In Variante übernehmen · Füller · Beenden** (primär). Variante (b) siehe DL-Q2 | Die Hervorhebung wandert vom Rechenknopf zum Schlussknopf; „In Variante übernehmen" schreibt weiter sofort (mit Rückfrage), wie heute | S | Die Lastspitzenkappung trägt einen Beenden-Knopf; „Berechnen" steht ohne Hervorhebung im Blatt. |
| 6 | `Kosten/KostenKomponenteDialog.razor` (Kosten → Kostenvorlagen) | Reiter Kosten: Position anlegen · Übernahme… · Positionskatalog… · Nutzungsdauern vorbelegen…; Fuß Status · Abbrechen · Speichern · OK | Reiterleiste bleibt (Blattleiste des Reiters Kosten); Fuß als `SpeichernLeiste` **Status · Speichern · Abbrechen · OK** (`MitSpeichern`). Voller Arbeitsstand → DL-Q3 | Abbrechen verwirft nur die ungespeicherten Eingaben; Zeilenaktionen schreiben weiter sofort (Ids) — das steht dann in der Leiste selbst (Kurztext) | S (Variante a) / L (b) | Die Kostenverwaltung ordnet ihre Schlussleiste: Speichern, Abbrechen, OK. |
| 7 | `Kosten/EnergietraegerDialog.razor` (Kosten → Energieträger) | Listenspalte Neu… · Variante · Löschen bzw. Übernehmen… · Entfernen; Kartenspalte „Stammwerte speichern"; Fuß Abbrechen · Speichern · OK | Listenspalte bleibt (Listenleiste); **„Stammwerte speichern" entfällt** — Bezeichnung und Gruppe gehören zur Karte und werden mit Speichern/OK geschrieben (DL-Q5); Fuß als `SpeichernLeiste` **Status · Speichern · Abbrechen · OK** | Ein Knopf fällt weg; die Hülle schreibt Stammwerte im Speichern-Weg mit; Abbrechen wie bei 6 (DL-Q3) | M | Die Energieträgerverwaltung speichert Bezeichnung und Gruppe mit „Speichern"; der eigene Knopf entfällt. |
| 8 | `Photovoltaik/ModulImportDialog.razor` (PV Module, Wechselrichter) | Kopf Quellenwahl; „Zurücksetzen" im Blatt; Fuß Füller · **Auswahl übernehmen** (primär) · OK | Quellenwahl und Zurücksetzen bleiben im Blatt; Fuß **Füller · Auswahl übernehmen · Beenden** (primär) — DL-Q4 | Die Hervorhebung wandert von Übernehmen zu Beenden; Doppelklick übernimmt weiter eine Zeile sofort | S | PV-Modul- und Wechselrichter-Import schließen mit „Beenden". |
| 9 | `Import/KatalogImportDialog.razor` (Import Heizkessel, Wärmepumpen, Solarthermie, Pufferspeicher, Stromspeicher) | Fuß Füller · **Speichern DB** (primär) · OK | Fuß **Füller · Auswahl übernehmen · Beenden** (primär); der Knopf heißt wie im Zwilling 8 — DL-Q4 | Wie 8; dazu ein neuer Knopftext | S | Die fünf Katalogimporte übernehmen mit „Auswahl übernehmen" und schließen mit „Beenden". |
| 10 | `Admin/EinstellungenDialog.razor` (Einstellungen) | Leiste Standardwerte · Füller ohne Primär; darunter Abbrechen · Speichern | **Standardwerte · Füller · Abbrechen · OK** (primär) als eine `SpeichernLeiste`; Knopftext des OK → DL-Q6 | Standardwerte setzt weiter nur die Felder zurück und speichert nicht; das sagt die Statuszeile wie heute | S | Der Standardwerte-Knopf der Einstellungen steht in der Schlussleiste. |

### 2.1 Je Knopf geprüft

- **Ersetzbar durch Zeilenwahl oder Doppelklick?** Kein „Ändern…"/„Bearbeiten…" wird ersetzt. Die
  Hausregel verlangt sichtbare Aktionsknöpfe (Berührungsziele, nie `:hover` allein); ein Doppelklick
  ist auf dem Tablet nicht auffindbar. Der Doppelklick auf eine Katalogzeile darf **zusätzlich**
  „Ändern…" öffnen (die `Katalogliste` kennt ihn seit den Importdialogen) — kein Teil von DL-2,
  je Komponente eine Zeile im Umbauauftrag.
- **Sind „Ändern" und „Speichern" dasselbe?** Nein. „Ändern…" öffnet den Editor der markierten Zeile
  (Überlagerung); „Speichern" schreibt den Eingabeblock der Maske. Beide bleiben, aber nach der
  Leseregel: Speichern links vom Füller, Ändern rechts. Einzige Doppelung: „Stammwerte speichern"
  neben „Speichern" im Energieträgerdialog (7, DL-Q5).
- **Ist „Standardwerte" eine Aktion der Leiste oder gehört sie ins Blatt?** Sie wirkt auf alle fünf
  Rubriken zugleich (elf Werte und den KI-Schalter) — also auf den ganzen Dialog, nicht auf ein Blatt.
  Sie gehört in die Fußleiste, links vom Füller.
- **Navigierende Knöpfe** (Grafik…, Kennliniendaten…, Typ ändern…, Simulation…, Gebäudetyp…) öffnen
  eine Überlagerung und stehen links vom Füller (Ansicht bzw. Nachbarkatalog). „Gebäudetyp in DB
  ändern…" hat in der Verwaltung einen Zwilling im Menü (Gebäude → Gebäudetypen) und bleibt
  trotzdem an beiden Stellen (DL-Q1): der Weg aus der Gebäudemaske heraus ist der kürzere.
- **Quellenwahl der Importe** (CEC-Netz, CEC-Datei, PAN/OND) ist ein Umschalter, keine Aktionsleiste;
  die gewählte Quelle trägt heute die Primärfarbe und ist damit ein zweiter dunkler Knopf in der
  Maske. Hinweis für den Umbauauftrag 8/9: Umschalter als Umschalter zeichnen (Muster `Auswahlpaar`),
  nicht als primären Knopf. Kein Entscheid nötig, keine Bedienänderung.


## 3 Die Anwenderentscheide

Alle sechs Fragen sind am **20.09.2026** entschieden: nach Empfehlung, mit einer Abweichung
bei DL-Q1.

| Kennung | Frage | Variante (a) | Variante (b) | Empfehlung | Entscheid |
|---|---|---|---|---|---|
| **DL-Q1** | Gebäudeverwaltung: bleibt „Gebäudetyp in DB ändern…" in der Katalogspalte? | Nur im **Projektdialog** behalten (dort gibt es kein Menü daneben); in der Verwaltung streichen, der Menüpunkt „Gebäudetypen" steht direkt darunter | In beiden Betriebsarten behalten, links vom Füller | **(a)** — ein Weg je Ziel; die Verwaltung wird um einen Knopf leichter | **20.09.2026: (b)** — der Knopf bleibt an **beiden** Stellen: in der Verwaltung in der Listenleiste der Katalogspalte, im Projekt in der Fußleiste |
| **DL-Q2** | Lastspitzenkappung: Rechenmaske oder OK-Dialog? | **Rechenmaske:** „Berechnen" bleibt im Blatt zwischen Parametern und Ergebnis, ohne Primärfarbe; Fuß CSV-Export · In Variante übernehmen · Füller · Beenden (primär) | **OK-Dialog:** Fuß Berechnen · CSV-Export · Füller · Abbrechen · OK, wobei OK das Ergebnis in die aktive Speichervariante schreibt und schließt; ohne Projekt nur Beenden — zwei Gestalten einer Maske, Aufwand M | **(a)** — der Lesefluss Parameter → Rechnen → Ergebnis bleibt, nur die Farbe wandert; Aufwand S | **20.09.2026: (a)** |
| **DL-Q3** | Kosten und Energieträger: Reihenfolge jetzt oder voller Arbeitsstand? | **Reihenfolge nach Hausregel** (`SpeichernLeiste` mit `MitSpeichern`); Zeilenaktionen (Position anlegen, Übernahme, Katalog, Neu, Variante, Löschen) schreiben weiter sofort, weil sie Ids brauchen; Abbrechen verwirft die ungespeicherte Karte bzw. die ungespeicherten Eingaben, und der Kurztext des Knopfes sagt das | **Voller Arbeitsstand:** alle Zeilenaktionen sammeln bis OK, die Hüllen (1 336 und 2 773 Zeilen) bauen ihren Schreibweg um — Aufwand L je Dialog, eigenes Konzept | **(a)** für DL-2; (b) als eigenes Konzept vormerken, wenn die Kostendialoge ohnehin angefasst werden | **20.09.2026: (a)**; (b) bleibt als eigenes Konzept vorgemerkt |
| **DL-Q4** | Importdialoge (8, 9): Wer trägt die Primärfarbe? | **Beenden** (Hausregel: der Schlussknopf ist der einzige primäre); „Auswahl übernehmen" steht davor, gesperrt bis eine Zeile gewählt ist | **Auswahl übernehmen** bleibt primär (heute) — Ausnahme von der Regel für Importmasken | **(a)** — eine Regel ohne Ausnahme; die Übernahme ist über die Sperre und den Doppelklick auffindbar genug | **20.09.2026: (a)** |
| **DL-Q5** | Energieträger: „Stammwerte speichern" in Speichern/OK aufgehen lassen? | **Ja:** Bezeichnung und Gruppe sind Felder der Karte; Speichern/OK schreiben sie mit, der Knopf entfällt | Nein, der Knopf bleibt in der Kartenspalte | **(a)** — zwei Speichern-Knöpfe in einer Maske sind die einzige echte Doppelung des Inventars | **20.09.2026: (a)** — der Knopf entfällt |
| **DL-Q6** | Einstellungen: Der OK-Knopf heißt „Speichern" (heute) oder „OK"? | **„OK"** — ein Wort für einen Weg (prüft, speichert, schließt), wie in jedem anderen Dialog | „Speichern" bleibt (Wortlaut des Vorläufers) | **(a)**; Aufwand ein Ressourcentext | **20.09.2026: (a)** — „OK" |


## 4 Umsetzungsreihenfolge

Je Komponente ein Auftrag (DL-2a … DL-2k), kleine und risikolose zuerst; die Mockups sind
abgenommen, alle Schritte sind damit frei.

| Schritt | Auftrag | Aufwand | Voraussetzung |
|---|---|---|---|
| 0 | **umgesetzt (DL-2a)** — `SpeichernLeiste`: Aktionsschlitz links (`Aktionen`), bunit-Test; dazu die Wache **`EPOS.UI.Tests/KnopfleistenWacheTests`**: Die Fußleiste jeder `*Dialog.razor` unter `EPOS.UI/Dialoge/` steht allein (keine zweite Leiste unmittelbar darüber), trägt genau einen `epos-knopf--primaer`, und er ist der letzte Knopf; wo `Abbrechen` steht, steht es unmittelbar vor ihm. Acht der zehn stehen mit Grund in `AUSNAHMEN` — **jeder Folgeauftrag streicht seinen Eintrag**; `WaermepumpeStammDialog` und `GebaeudeDialog` halten diese vier Regeln schon ein und stehen nicht darin. Sechs Dialoge außerhalb des Administrationsmenüs verletzen dieselben Regeln und stehen als Befund in `AUSNAHMEN_BEFUND` (Abschnitt 5) | S | — |
| 1 | GebaeudetypDialog (2) | S | — |
| 2 | WaermepumpeStammDialog (4) | S | — |
| 3 | BedarfAdminDialog (3) | S | — |
| 4 | **umgesetzt (#384)** — KatalogImportDialog (9) und ModulImportDialog (8) in einem Auftrag: beide Füße laufen Füller · Auswahl übernehmen · Beenden (primär), beide Übernahmeknöpfe tragen denselben Ressourcentext (`PVIMP_BTN_UEBERNEHMEN`; „Speichern DB" ist entfallen), und die Quellenwahl beider Masken ist als Umschalter gezeichnet (`epos-knopf--gewaehlt`, `aria-pressed`) statt als primärer Knopf | S | DL-Q4 |
| 5 | **umgesetzt (#384)** — EinstellungenDialog (10): eine `SpeichernLeiste` mit „Standardwerte" im Aktionsschlitz, Schlussknopf „OK" | S | Schritt 0, DL-Q6 |
| 6 | PeakShavingDialog (5) | S | DL-Q2 |
| 7 | GebaeudeDialog (1), beide Betriebsarten | M | Schritt 0, DL-Q1 |
| 8 | KostenKomponenteDialog (6) | S | Schritt 0, DL-Q3 |
| 9 | EnergietraegerDialog (7) | M | DL-Q3, DL-Q5 |

**Abnahme je Auftrag:**

- bunit: die Leistenreihenfolge der Komponente (Knopftexte in Reihenfolge, letzter Knopf primär,
  Füller vorhanden), der Schließweg (Beenden bzw. OK/Abbrechen liefern das erwartete `Geschlossen`),
  bei 1, 6, 7 der Arbeitsstand (Abbrechen schreibt nichts).
- Wache aus Schritt 0 grün über den ganzen Dialogbestand — **und der Eintrag der umgebauten
  Komponente in `AUSNAHMEN` gestrichen.** Die Wache sagt selbst, wenn eine Ausnahme tot ist
  (`Jede_Ausnahme_verletzt_heute_wirklich` wird rot), der Auftrag ist also erst mit dem
  gestrichenen Eintrag fertig.
- Katalogprobe (`Proben/Rasterprobe`, Seite `/katalogprobe`) für die Komponenten im Katalograhmen
  (3, Vorbilder aus KL-5): die Fußleiste liegt unter der Liste, keine Überdeckung.
- Windows-Schale auf Linux gebaut, wenn eine Hülle angefasst wird (1, 7, 10).
- Logbuch-Einträge aus Abschnitt 2 gesammelt mit dem nächsten Wiki-Upload (Regel Hilfesystem 13.3).

**Kein Rechenweg ist betroffen**, deshalb kein Referenzlauf; die Testdatenbank bleibt unverändert.


## 5 Befund außerhalb der zehn

Die Wache aus Schritt 0 liest den **ganzen** Dialogbestand, nicht nur das Administrationsmenü.
Dabei sind sechs weitere Dialoge aufgefallen, die dieselben Regeln aus denselben Gründen
verletzen. Sie stehen in der zweiten, eigenen Liste `AUSNAHMEN_BEFUND` der Wache — **kein Teil
von DL-2, kein Auftrag zugeordnet**; die Liste macht den Befund sichtbar, bis der Anwender
entscheidet.

| Dialog | Fuß heute | Verletzt |
|---|---|---|
| `Bedarf/GebaeudeKatalogDialog.razor` | Überschreiben · **Speichern** · Beenden | der primäre Knopf steht nicht zuletzt |
| `Bedarf/TypStammDialog.razor` | Überschreiben · Speichern unter · **Speichern** · Beenden | dieselbe Stellung |
| `Bedarf/TypProfilDialog.razor` | Speichern unter · **Speichern** · Löschen · Neu · Schließen | dieselbe Stellung |
| `Waermepumpe/WaermepumpenKatalogDialog.razor` | **Übernehmen** · Abbrechen | dieselbe Stellung — der Fall der Importdialoge 8/9 |
| `Kosten/VorlagenUebernahmeDialog.razor` | **OK** · Abbrechen | dieselbe Stellung |
| `Strom/GanglinieImportOptionenDialog.razor` | Abbrechen · Aktualisieren · **OK** | Abbrechen steht nicht unmittelbar vor OK — der Fall der Kostendialoge 6/7 |

Die ersten drei sind Geschwister der schon umgebauten Katalogdialoge (`BhkwKatalogDialog`,
`HeizkesselKatalogDialog`, `PufferSpKatalogDialog`, `SolarkollektorKatalogDialog` laufen bereits
„… · Abbrechen · Speichern (primär)"); ihr Umbau wäre je Dialog Aufwand S und derselbe Griff wie
dort.

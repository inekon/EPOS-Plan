# Übernahme der Komponentenkosten in die Kostenverwaltung

Stand 18.08.2026 · Umsetzung der vier Nutzerentscheidungen zur Meldung „Komponentenkosten
werden nicht in die Kostenverwaltung übernommen".

Codestand: Haupt-Checkout `main`, HEAD `605dcb8` + der Working Tree dieser Etappe
(8 geänderte und 3 neue Codedateien, dazu dieses Protokoll und der Nachtrag in
[`../Simulation/Lokalisierung_Katalog.md`](../Simulation/Lokalisierung_Katalog.md)).
**Nichts committet.** Die Engine (`Allgemein/Simulation/*.cs`) ist unberührt —
`git diff --stat -- 'WindowsFormsApplication1/Allgemein/Simulation/*.cs'` ist leer (dort
änderte sich nur die Dokumentationsdatei `Lokalisierung_Katalog.md`), deshalb kein
Referenzlauf.

---

## 1. Ausgangslage

Die Übernahme funktionierte grundsätzlich, war aber **einfeldrig und einmalig**:

* `Form_Kosten.GetModulKosten` zog je Gewerk **genau ein Feld** — BHKW `Tab_BHKW.Kosten_Modul`,
  Kessel/Puffer/Solarthermie `Investitionskosten`, WP/PV/Stromspeicher `Modulkosten`.
* Geschrieben wurde nur beim **ersten** Anwählen der Komponente
  (`EnsureMainComponentExists`) und über den Knopf „🔄 Planwert übernehmen…", der ohne
  Rückfrage denselben einen Wert setzte.
* Die vier Nebenkostenfelder des BHKW (`Kosten_Montage`, `Kosten_Lieferung`,
  `Kosten_Schallschutzhaube`, `Kosten_Abgasreinigung`) blieben vollständig unbeachtet.
* Betriebskosten wurden gar nicht vorbelegt: Positionen der Kategorie 2 entstanden mit 0.
* Wich der Technik-Planwert von der erfassten Position ab, war das nirgends sichtbar.

Die sechs Defekte drumherum (Kategoriefilter in den Summen, DISTINCT gegen doppelt gezählte
Geräte, Betriebskosten-Position je Kategorie, projektfreie StammID-Quelle, Arbeitspreis 0 als
ungepflegt, Update über den Primärschlüssel) waren mit `605dcb8` bereits behoben und sind hier
Voraussetzung, nicht Gegenstand.

**Zahlenbeispiel, das die Meldung ausgelöst hat.** Das Katalogmodul „2G 250kw.el Gas" trägt
`Kosten_Modul` = 16.666 € **und** `Investition_kwel` = 653,60 €/kWel bei `Pel` = 250 kWel,
also 163.400 €. Beide Zahlen sind gepflegt, keine ist „richtiger" — der bisherige Code nahm
kommentarlos die kleinere.

---

## 2. Die vier Entscheidungen und ihre Umsetzung

### Entscheidung 1 — „Anwender wählt je Anlage"

Beide Werte werden ermittelt und **je Anlage** zur Wahl gestellt; erkennbar bleibt, woher der
Wert stammt.

| Was | Wo |
|---|---|
| Ermittlung aller Kostenbasen je Anlage, Entdoppelung je Gerät | `Controller/TechnikPlanwertCtrl.cs:144` (`LiesAnlagen`) |
| Feld-Landkarte je Gewerk (welches Feld bedeutet was) | `Controller/TechnikPlanwertCtrl.cs:207` (`BasenFuellen`) |
| Auswahlmaske mit Spalte „Herkunft" | `Views/Kosten/Form_PlanwertUebernahme.cs:1` |
| Knopf „Planwert übernehmen…" öffnet die Maske und schreibt das Ergebnis | `Views/Kosten/Form_Kosten.cs:959` (`btnTest_KostenUebernahme_Click`) |
| Vorbelegung beim ersten Anwählen nur bei Eindeutigkeit | `Views/Kosten/Form_Kosten.cs:1071` (`GetModulKosten`) → `TechnikPlanwertCtrl.Hauptsumme` |

Die Herkunftsspalte zeigt bei der spezifischen Basis die Rechnung im Klartext
(`653,60 €/kWel × 250,00 kWel`), beim Modulpreis den Feldnamen. Ein Betrag von 0 oder ein
leeres Feld erzeugt **keine** Basis — wo nur ein Wert gepflegt ist, gibt es also keine
Scheinauswahl, sondern den vorhandenen Wert
(`TechnikPlanwertCtrl.Anlage.EindeutigerWert`, `TechnikPlanwertCtrl.cs:97`).

**Prüfung der übrigen Gewerke** (Nutzerauftrag: „prüfe, ob dieselbe Zweideutigkeit bei anderen
Gewerken existiert"):

| Gewerk | Kostenfelder der Gerätetabelle | Ergebnis |
|---|---|---|
| BHKW | `Kosten_Modul` (€), `Investition_kwel` (€/kWel) | **zwei konkurrierende Basen** → Auswahl |
| Stromspeicher | `Modulkosten` (**€/kWh**), `Leistungskosten` (€/kW), `Investition_Fix` (€) | drei Felder, aber **eine** Formel → eine Basis (s. u.) |
| Wärmepumpe | `Modulkosten` | einfeldrig, unverändert |
| Heizkessel | `Investitionskosten` | einfeldrig, unverändert |
| Photovoltaik | `Modulkosten` | einfeldrig, unverändert |
| Solarthermie | `Investitionskosten` | einfeldrig, unverändert |
| Pufferspeicher | `Investitionskosten` | einfeldrig, unverändert |

Beim **Stromspeicher** ist `Modulkosten` entgegen dem Namen ein spezifischer Preis in €/kWh —
belegt durch `Views/Stromspeicher/Form_AdminStromspeicher.cs:40`
(`EinheitenBeschriftungKorrigieren`, AP0-Entscheid vom 16.08.2026, `label11.Text = "€/kWh"`) und
durch die Rechenweise von `Controller/StromspeicherSimCtrl.cs:1147` (`cCap = Σ Modulkosten·E / ΣE`).
Der alte Weg summierte diesen Wert als Euro-Betrag und war damit dimensional falsch. Jetzt gilt
`Modulkosten × Energie + Leistungskosten × Leistung + Investition_Fix` — dieselbe Formel wie in
der Speicher-Wirtschaftlichkeit (`TechnikPlanwertCtrl.cs:236`). Das ist keine Auswahl, sondern
eine Korrektur; überschrieben wird durch sie nichts (Entscheidung 4).

### Entscheidung 2 — Nebenkosten als eigene Zeilen

Jeder Nebenkostenposten mit Wert **> 0** wird eine eigene Zeile in der Gruppe der Komponente.

| Was | Wo |
|---|---|
| Persistenzwerte „Montage", „Lieferung", „Schallschutzhaube", „Abgasreinigung" | `Allgemein/DbWerte.cs:106` |
| Zusammenfassung je Bezeichnung über alle Anlagen | `Controller/TechnikPlanwertCtrl.cs:301` (`Nebensummen`) |
| Katalogeintrag anlegen, falls er fehlt | `Controller/KostenPositionCtrl.cs:63` (`StammIdNeben`) |
| Zeilen schreiben (Modus `NurAnlegen` / `Abgleichen`) | `Controller/KostenPositionCtrl.cs:240` (`SchreibeNebenkosten`) |
| Anlegen beim Anwählen der Komponente | `Views/Kosten/Form_Kosten.cs:780` (`NebenkostenAnlegen`) |

**Das Datenmodell trägt Unterpositionen ohne Schemaänderung.** `Tab_ProjektWerte` führt keinen
eigenen Bezeichnungstext; der Name steht in `Tab_Kostenfaktor.Bezeichnung`, die Zugehörigkeit
zur Komponente in `KomponentenID`, die Rolle in `Tab_Kostenfaktor.IsMainComponent` und die
Bündelung in `Tab_ProjektWerte.Gruppe`. Eine Nebenkostenzeile ist damit schlicht eine weitere
Zeile mit derselben `KomponentenID`, derselben Gruppe („Allgemein") und einem eigenen
Katalogeintrag mit `IsMainComponent = False`. `Tab_Kostenfaktor.StammID` ist ein **AutoWert**
(per DAO geprüft: `Attributes` = 17, also `dbAutoIncrField`), fehlende Einträge entstehen daher
beim ersten Bedarf — dasselbe „Lern"-Muster, das `Form_Kosten.AddKostenItem` für
`Tab_KostenGruppenKatalog` schon verwendet. **Kein SchemaMigration-Schritt nötig, keine
gespeicherte Access-Abfrage geändert.**

Die Sortierung von `Abfrage_Kostenfaktoren` (`ORDER BY IsMainComponent, KategorieName,
Komponente, Gruppe, Bezeichnung`) stellt die Hauptposition an den Anfang; die Nebenzeilen mit
Gruppe „Allgemein" folgen unmittelbar darunter in derselben Gruppenüberschrift.

Beim bloßen Anwählen wird nur **angelegt**, nie aktualisiert (`Nebenmodus.NurAnlegen`) — sonst
hätte jedes Öffnen der Maske eine Anwenderkorrektur zurückgesetzt. Aktualisiert wird
ausschließlich über „Planwert übernehmen…" (`Nebenmodus.Abgleichen`). Gelöscht wird nie.

### Entscheidung 3 — Betriebskosten erst nach vorliegendem Simulationsergebnis

| Was | Wo |
|---|---|
| Vorbelegung aus Wartungssatz × gerechneter Jahresmenge | `Controller/TechnikPlanwertCtrl.cs:389` (`LiesBetriebsplanwert`) |
| Einbau in das Anlegen der Betriebs-Hauptposition | `Views/Kosten/Form_Kosten.cs:710` (`EnsureMainComponentExists`) |
| Hinweiszeile über der Positionsliste | `Views/Kosten/Form_Kosten.cs:1007` (`HinweiszeileAnlegen`) |

**BHKW.** `Tab_BHKW.Wartungskosten_kwhel` × Stromerzeugung des jüngsten Laufs. Die Menge kommt
aus `Tab_ErgebnisBHKWModul.Stromproduktion` (Einheit MWh/a laut Konzept 3.1, deshalb × 1000).
Verknüpft wird über `Tab_ErgebnisBHKWModul.Modul` = `Tab_Energieanlagen.Bezeichner` — so schreibt
`SimulationControl.BHKW_Liste_Laden` den Modulnamen. Lässt sich ein Modulname nicht zuordnen,
gilt der Wartungssatz nur dann, wenn **alle** BHKW des Projekts denselben führen; das ist keine
Schätzung, sondern die einzige Möglichkeit. Sonst: keine Vorbelegung und ein Hinweis.

**Ohne Lauf keine Zahl.** Fehlt `Tab_Ergebnis` für das Projekt, bleibt die Position bei 0 und
die Hinweiszeile sagt „Vorbelegung der Betriebskosten erst nach einem Simulationslauf
verfügbar." Es wird nichts geschätzt und keine Vollbenutzungsstundenzahl unterstellt.

**Betrag 0 gilt als ungepflegt.** Bestandsprojekte tragen ihre Betriebskosten-Hauptposition
längst — mit 0, weil sie vor dem ersten Simulationslauf angelegt wurde. Eine Vorbelegung, die
nur bei brandneuen Positionen greift, liefe an genau diesen Projekten vorbei. Steht die
vorhandene Position auf 0 und liegt ein Ergebnis vor, wird sie deshalb gefüllt
(`Form_Kosten.cs:743`). Das folgt der Hausregel aus `605dcb8` („Arbeitspreis 0 gilt als
ungepflegt"). Ein **gepflegter** Wert wird nie angefasst — geprüft als T6.13.

**Heizkessel: keine Vorbelegung, offene Rückfrage.** Ergebnis der Einheiten-Recherche in
Abschnitt 4.

**Übrige Gewerke.** Wärmepumpe, PV, Solarthermie und Pufferspeicher führen kein Wartungsfeld in
der Gerätetabelle. Der Stromspeicher führt `Verschleisskosten`, die
`StromspeicherSimCtrl` bereits in seine eigene Wirtschaftlichkeit einrechnet — eine zusätzliche
Kostenposition wäre Doppelzählung. In allen Fällen erscheint ein Hinweis statt einer Zahl.

### Entscheidung 4 — Abweichungen sichtbar statt still überschreiben

| Was | Wo |
|---|---|
| Vergleich erfasste Position ↔ Technik-Planwerte | `Controller/KostenPositionCtrl.cs:314` (`Pruefe`) |
| Hinweiszeile in der Kostenverwaltung | `Views/Kosten/Form_Kosten.cs:1007` |
| Spalte „Technik-Planwert" + Markierung auf der Kostenseite | `Views/BerichteKosten/UcBkKosten.cs:307` (`LadeKomponenten`) |
| Zähler in der Statuszeile der Kostenseite | `Views/BerichteKosten/UcBkKosten.cs:292` |
| Meldung nach einem Modulwechsel | `Controller/KomponentenUebernahmeCtrl.cs:405` (`KostenabweichungMelden`) |

**Der Vergleich fragt „passt der erfasste Wert zu IRGENDEINER angebotenen Kostenbasis?"** —
nicht „zu der einen richtigen". Deshalb muss die getroffene Auswahl **nicht gespeichert** werden:
Hat der Anwender den Modulpreis gewählt, stimmt die Position mit genau dieser Summe überein und
gilt als angeglichen; wählt er später den spezifischen Preis, ebenso. Erst wenn sich die Technik
ändert (Katalogpflege, Modulwechsel) oder der Betrag von Hand gesetzt wurde, passt keine Summe
mehr — und genau dann erscheint die Abweichung. Eine gespeicherte Auswahl hätte eine neue Spalte
und damit einen Migrationsschritt gebraucht.

Ausdrücklich **nicht** als „angeglichen" gilt der Zustand „noch nichts gewählt" (Hauptposition 0
bei zwei angebotenen Basen) — sonst bekäme der Anwender nie den Hinweis, dass er entscheiden
muss (`KostenPositionCtrl.cs:365`, `MoeglicheSummen`).

Geschrieben wird ausschließlich auf ausdrückliche Handlung. `KomponentenUebernahmeCtrl` rührt
die Kostenposition weiterhin nicht an — meldet den Zustand jetzt aber im Hinweistext der
Übernahme, statt einen stillen Altwert zu hinterlassen.

---

## 3. Neue und geänderte Dateien

| Datei | Art | Inhalt |
|---|---|---|
| `Controller/TechnikPlanwertCtrl.cs` | **neu** | Die eine Leseschicht „was kostet die verbaute Technik": Kostenbasen und Nebenkosten je Anlage, Betriebskosten-Planwert |
| `Controller/KostenPositionCtrl.cs` | **neu** | Schreib- und Prüfschicht für `Tab_ProjektWerte`/`Tab_Kostenfaktor`, Abweichungsprüfung |
| `Views/Kosten/Form_PlanwertUebernahme.cs` | **neu** | Auswahlmaske je Anlage (ohne Designer-Datei, wie `UcBkKosten`) |
| `Views/Kosten/Form_Kosten.cs` | geändert | `GetModulKosten` auf den Controller umgestellt, `EnsureMainComponentExists` um Betriebskosten und Nebenkosten erweitert, Übernahmeknopf, Hinweiszeile |
| `Views/Kosten/ucKostenItem.cs` | geändert | Obergrenze des Betragsfeldes, Wertklemmung |
| `Views/BerichteKosten/UcBkKosten.cs` | geändert | Spalte „Technik-Planwert", Markierung, Zähler in der Statuszeile |
| `Controller/KomponentenUebernahmeCtrl.cs` | geändert | Meldung der unveränderten Kostenposition nach dem Bestandsaustausch |
| `Allgemein/DbWerte.cs` | geändert | Persistenzwerte der Nebenposten, Gruppe „Allgemein", Einheit „€" |
| `MyResource/Resource*.resx`, `Resource.Designer.cs` | geändert | 30 neue Schlüssel, de + en |

**Nebenbefund mit erledigt:** `ucKostenItem.Designer.cs` setzte `numBetrag.Maximum = 99999`.
Ein Betrag von 163.400 € hätte beim Setzen eine `ArgumentOutOfRangeException` geworfen und den
Aufbau der Positionsliste abgerissen. Die Grenze wird jetzt programmatisch angehoben
(`ucKostenItem.cs:28`), die Designer-Datei bleibt unberührt; zusätzlich klemmt `Klemme(...)`
Werte aus Altbestand in den gültigen Bereich.

---

## 4. Einheiten-Recherche `Tab_Heizkessel.Wartungskosten` — Ergebnis

**Die Einheit lässt sich nicht belegen. Der Kessel wird deshalb NICHT vorbelegt.**

Beleg, jeweils vollständige Suche über `WindowsFormsApplication1` (ohne die Altkopien):

| Fundstelle | Befund |
|---|---|
| `Views/Heizkessel/Form_Heizkessel*.cs` und deren Designer/`.resx` | **kein Vorkommen** von „Wartung" — der Kessel-Dialog zeigt das Feld nicht |
| `Views/Heizkessel/Form_Heizkessel_Admin.*`, `…_Bearbeiten.*` (Katalog-Editor) | **kein Eingabefeld, keine Beschriftung, kein Einheitensuffix** |
| `Views/Heizkessel/Form_Heizkessel_Bearbeiten.cs:142` | reicht den Wert nur durch (`ctrl.Wartungskosten = model.Wartungskosten`) |
| `Views/Heizkessel/Form_Heizkessel_einlesen.cs:313/331` | einzige schreibende Stelle: VDI-3805-Import |
| `Tab_Heizkessel` (44 Zeilen) und `Tab_Heizkessel_STAMM` (21 Zeilen) | durchgehend **0**, Maximum 0 |

Zum Vergleich das BHKW, wo die Einheit **eindeutig belegt** ist:
`Views/BHKW/Form_DBBHKW.designer.cs:520` beschriftet `textBox_Wartungskosten` (Position
124/79) mit „Wartungskosten:", und `Label19` auf gleicher Höhe (187/**81**) trägt
`"€ / kWhel"` (`Form_DBBHKW.designer.cs:602-608`).

Da die Oberfläche dem Anwender für den Kessel **nichts** zusagt, gibt es nichts, wonach man sich
richten könnte. Ob €/a, €/kWh oder €/kW gemeint ist, wäre geraten. Die Kostenverwaltung meldet
das im Klartext: „Die Einheit von Tab_Heizkessel.Wartungskosten ist nicht belegt — keine
Vorbelegung (offene Rückfrage)." → **offene Nutzerfrage, siehe Abschnitt 7.**

---

## 5. Verifikation

Reflection-Harnisch (net8.0-windows, x86) gegen eine **Wegwerf-Kopie** der
`Kenndaten.accdb` unter `C:\Waermeplan\_ka`; Produktiv-DB ausschließlich lesend kopiert
(`Kenndaten.laccdb` vorher geprüft — nicht vorhanden). Build in ein Scratch-`OutDir`,
`bin\` unberührt: **0 Fehler, exakt 6 Bestandswarnungen**
(CS0108 ×2, CS0109 ×2, CS4014, CS1998).

Testdaten in der Kopie: Nebenkosten am Katalogmodul „2G 250kw.el Gas" (Gerät 1018110) auf
Montage 2.500 €, Lieferung 800 €, Schallschutzhaube **0 €**, Abgasreinigung 1.200 € gesetzt und
eine Anlagenzeile in Projekt 1023 darauf angelegt — Projekt 1023 führte in der
Ausgangsdatenbank keine BHKW-Anlagenzeile.

| Nr. | Prüfung | Erwartet | Ergebnis |
|---|---|---|---|
| T1.1–1.7 | 1023/BHKW: zwei Basen, Herleitung, Mehrdeutigkeit | 16.666 € und 163.400 €, „653,60 €/kWel × 250,00 kWel", `EindeutigerWert` = 0 | OK |
| T1.8–1.10 | Nebenposten nur > 0 | 3 Posten (ohne Schallschutzhaube), Summe 4.500 € | OK |
| T1.11–1.12b | 1018/Heizkessel einfeldrig | 1 Basis, 12.000 €, nicht mehrdeutig | OK |
| T1.13–1.14 | 1024/BHKW einfeldrig (nur spezifisch) | 42.000 € (2.000 €/kWel × 21 kWel) | OK |
| T2.1–2.8 | Auswahlmaske | beide Basen + „nicht ansetzen", Summe folgt der Wahl (16.666 / 163.400 / 0) | OK |
| T3.1–3.5 | erstes Anwählen 1023/BHKW | 1 Hauptposition (0 €) + 3 Nebenkostenzeilen mit den richtigen Beträgen | OK |
| T3.6 | Hinweis bei offener Auswahl | „…zwei Kostenbasen zur Wahl…" | OK |
| T3.7–3.8 | Kachel == Tabelle (D1-Fix hält) | Komponentensumme BHKW 4.500 €, Kategorie-1-Summe getrennt | OK |
| T4.1–4.2 | **erneutes Öffnen** | keine zusätzlichen Zeilen in `Tab_ProjektWerte` (4 → 4) | OK |
| T5.1–5.3 | Übernahme des gewählten Werts | Hauptposition 163.400 € gespeichert, 3 Nebenzeilen abgeglichen | OK |
| T5.4–5.6 | Summen nach der Übernahme | UI 167.900 € == `LiesKomponentenSummen` 167.900 €, keine Abweichung mehr | OK |
| T5.7 | Betragsfeld | 163.400 € darstellbar (Designer-Grenze war 99.999) | OK |
| T6.1–6.3 | Betriebskosten 1018 mit Lauf 171 | 529,20 €/a = 0,04 €/kWhel × 13.230 kWhel, Herleitung genannt | OK |
| T6.4–6.5 | Betriebskosten 1017 **ohne** Lauf | keine Zahl, Hinweis „erst nach einem Simulationslauf" | OK |
| T6.6–6.7 | Heizkessel | keine Vorbelegung, Hinweis auf die unbelegte Einheit | OK |
| T6.8 | Wärmepumpe | Hinweis „keine Wartungsangaben hinterlegt" | OK |
| T6.9–6.12 | Ende-zu-Ende über das Formular | 1018: Position 529,20 € + Hinweiszeile; 1017: 0 € + Hinweis | OK |
| T6.13 | gepflegter Wert (111 €) | bleibt beim erneuten Öffnen unangetastet | OK |
| T7.1–7.5 | Vitocrossal 1018 | erfasst 15.000 €, Technik 12.000 €, als Abweichung gemeldet, Öffnen ändert nichts | OK |
| T7.6–7.7 | Kostenseite „Berichte & Kosten" | Statuszeile meldet 2 abweichende Komponenten, Kachel 51.000,00 € unverändert | OK |
| T8.1–8.3 | Modulwechsel 1023 → 1024 über `KomponentenUebernahmeCtrl` | Übernahme läuft, Hinweis „Kostenposition … nicht verändert", Abweichung im Ziel sichtbar | OK |
| T9.1–9.3 | Projekt 1024 (vollständige Daten) | WP-Position 6.001 € unverändert, keine Nebenzeilen, Kostenseite liefert Werte | OK |
| T10.1–10.5 | de/en | Knopf, Basisname, Betriebs- und Abweichungshinweis in beiden Sprachen | OK |

**72 Prüfungen, 0 Fehlschläge.**

Der Harnisch fängt modale Dialoge über einen Wächter-Thread ab (`EnumThreadWindows` auf
`#32770`, Rumpftext aus dem längsten `Static`-Kind). Dabei ist der Nebenbefund in Abschnitt 6
aufgefallen.

---

## 6. Nebenbefund (nicht Gegenstand dieser Etappe)

Beim Öffnen der Kostenverwaltung erscheinen **drei modale Fehlerdialoge**:

```
Datenbankfehler: Field 'ID_Preisreihe' already exists in table 'Tab_StromspeicherVariante'.
(ebenso 'ID_Kostenprofil' und 'Aufschlag_Anwenden')
```

Ursache: `Controller/StromAufschlagCtrl.cs:64` (`StelleSpaltenSicher`) liest das Schema **einer**
Tabelle (`energy_project_settings`) und prüft damit auch die drei Einträge aus
`SchemaKatalog.Schritt12_Preismodell`, die zu `Tab_StromspeicherVariante` gehören
(`SchemaKatalog.cs:510-512`). Die Existenzprüfung greift für sie nie, das `ALTER TABLE` läuft
immer, und `DataRepository.ExecuteSQL` zeigt den Dialog, bevor das umschließende `try/catch`
greifen kann. Reproduzierbar auf einer Kopie der Produktivdatenbank, je Konstruktion von
`Form_Kosten` genau dreimal. **Nicht angefasst** — gehört nicht zu den vier Entscheidungen; als
eigener Arbeitsauftrag hinterlegt.

---

## 7. Offene Punkte

1. **Einheit von `Tab_Heizkessel.Wartungskosten`** — nicht belegbar (Abschnitt 4). Solange die
   Rückfrage offen ist, gibt es für den Kessel keine Betriebskosten-Vorbelegung. Sobald die
   Einheit feststeht, ist die Erweiterung ein Zweig in
   `TechnikPlanwertCtrl.LiesBetriebsplanwert` mit
   `Tab_ErgebnisHeizkessel.Waermeproduktion` (MWh/a) als Bezugsgröße.
2. **Modulanzahl bei PV und Solarthermie.** `Tab_PV.Modulkosten` und
   `Tab_Solarkollektoren.Investitionskosten` sind Preise **je Modul**; die Stückzahl steht in
   `Tab_Energieanlagen` (`PV_Leistung` bzw. `Kollektormodulanzahl`). Der Technik-Planwert
   bleibt hier bewusst der Modulpreis — beide Gewerke führen in der Gerätetabelle nur **ein**
   Kostenfeld, und die Entscheidung lautete „falls ein Gewerk nur ein Feld hat, bleibt es
   einfeldrig". Ob stattdessen der Anlagenpreis (Modulpreis × Stückzahl) gelten soll, ist eine
   eigene Nutzerentscheidung.
3. **Korrigierter Stromspeicher-Planwert.** Bisher wurde `Modulkosten` (€/kWh) als Euro-Betrag
   übernommen, jetzt gilt die vollständige Formel. Für Bestandsprojekte ändert sich dadurch
   nichts automatisch — die Abweichung wird angezeigt, angeglichen wird nur auf Knopfdruck.
   In den Testdaten steht `Modulkosten` durchgehend auf 0, der Unterschied konnte deshalb nicht
   an echten Zahlen gemessen werden.
4. **Reiterbeschriftungen von `Form_Kosten`** („Investitionskosten" / „Betriebskosten" /
   „Energiekosten") sind weiterhin deutsche Designer-Literale und dienen zugleich als
   Steuerwert und als SQL-Vergleichswert gegen `Tab_KostenKategorie.KategorieName` — ein
   Altbestand, der auch mit dieser Etappe nicht angefasst wurde. Der neue Code arbeitet
   durchgehend über `kategorieID`.
5. **Nebenbefund aus Abschnitt 6** — Fehlerdialoge beim Öffnen der Kostenmaske.

---

# Nachtrag 18.08.2026 — die beiden offenen Entscheidungen sind beantwortet

Der Anwender hat die Punkte 1 und 2 aus Abschnitt 7 entschieden. Codestand: Haupt-Checkout
`main`, HEAD `87483b4` + der Working Tree dieser Etappe (12 geänderte Codedateien, dazu dieses
Protokoll und der Nachtrag in
[`../Simulation/Lokalisierung_Katalog.md`](../Simulation/Lokalisierung_Katalog.md)).
**Nichts committet.** Die Engine (`Allgemein/Simulation/*.cs`) ist unberührt —
`git diff --stat -- 'WindowsFormsApplication1/Allgemein/Simulation/*.cs'` ist leer (dort änderte
sich nur die Dokumentationsdatei `Lokalisierung_Katalog.md`), deshalb kein Referenzlauf.

---

## N1. Entscheidung zu Punkt 1 — die Wartungseinheit des Kessels ist wählbar

Abschnitt 4 hatte belegt, dass sich die Einheit von `Tab_Heizkessel.Wartungskosten` **nicht**
belegen lässt: kein Eingabefeld, keine Beschriftung, kein Einheitensuffix, in allen 44 Projekt-
und 21 Katalogzeilen der Wert 0. Der Anwender hat entschieden, sie **nicht** zu erraten, sondern
**wählbar** zu machen.

### N1.1 Die drei Einheiten in den drei Schichten

| Schicht | Werte | Ort |
|---|---|---|
| **Persistenz** (deutsch, eingefroren) | `€/a`, `€/kWh`, `%/a` | `Allgemein/DbWerte.cs:177/185/194` |
| **Schlüssel** (sprachneutral, ASCII) | `EUR_JAHR`, `EUR_KWH`, `PROZENT_INV` | `Controller/TechnikPlanwertCtrl.cs:60-70` |
| **Anzeige** (lokalisiert) | „€/a Jahresbetrag" … | `TechnikPlanwertCtrl.WartungName`, `:795` |

Umrechnung ausschließlich über `WartungSchluessel` (`TechnikPlanwertCtrl.cs:773`) und
`WartungDbWert` (`:784`). Die ComboBox trägt als Item den Typ `EinheitItem`
(`Views/Heizkessel/Form_Heizkessel_Bearbeiten.cs:214`), der den **Schlüssel** hält und den
**lokalisierten Namen** anzeigt — kein Anzeigetext ist je Steuerwert. Belegt in Abschnitt N4,
Zeile E: auf englischer Oberfläche liefert `WartungDbWert("PROZENT_INV")` weiterhin `%/a`.

### N1.2 Migrationsschritt 15

**Ermittlung des Ausgangsstands.** `SchemaMigration.ZIEL_VERSION` stand im Code auf **14**, die
höchste registrierte Schrittnummer war `SCHRITT_14_PARALLELVERBUND`, und die Produktivdatenbank
meldete in `Tab_Applikation.SchemaVersion` ebenfalls **14** (gelesen aus einer Wegwerf-Kopie mit
32-bit-PowerShell + ACE OLEDB, rein lesend). Alle drei Quellen stimmten überein — die nächste
freie Nummer ist damit **15**.

| Was | Wo |
|---|---|
| Spaltenname (EINE Wahrheit) | `Allgemein/Update/SchemaKatalog.cs:534` (`SPALTE_KESSEL_WARTUNG_EINHEIT`) |
| Katalogeintrag, beide Tabellen, `TEXT(20)` | `SchemaKatalog.cs:573` (`Schritt15_KesselWartungseinheit`) |
| Schrittnummer + Begründung | `SchemaMigration.cs:243` (`SCHRITT_15_KESSEL_WARTUNGSEINHEIT`) |
| Zielversion 14 → 15 | `SchemaMigration.cs:68` |
| Registrierung in `SCHRITTE` | `SchemaMigration.cs:491` |
| Ausführung (15a DDL + 15b DML) | `SchemaMigration.cs:1704` (`Schritt_15_KesselWartungseinheit`) |
| Zählwerk | `SchemaMigration.DatenKesselWartungseinheitVorbelegt` |

**Beide Tabellen im selben Eintrag** — dieselbe Begründung wie bei `Schritt11_Stromspeicher`:
`HeizkesselCtrl.CopyFromStamm` kopiert Feld für Feld aus dem Katalog in die Projekttabelle, eine
Spalte nur auf einer Seite wäre sofort ein Datenverlust beim Übernehmen in ein Projekt.

**NICHT in `SchemaKatalog.Alle`** — dieselbe Begründung wie bei `Schritt12_Preismodell`: `Alle`
ist ausdrücklich der Umfang der SIMULATIONS-Eingabespalten, den die stille Rückfallebene
`WaermequelleClass.SchemaSicherstellen` bei jedem Simulationsstart sicherstellt. Der Rechenkern
liest die Wartungseinheit nirgends; sie gehört allein dem Kostenmodul. Ihre eigene, tolerante
Vorsorge steht in `Controller/HeizkesselStammCtrl.cs:77` (`StelleSpaltenSicher`), aufgerufen aus
dem einzigen Dialog, der die Spalte schreibt (`Form_Heizkessel_Bearbeiten.cs:33`). Sie folgt der
**korrigierten** Fassung von `StromAufschlagCtrl.StelleSpaltenSicher` aus `87483b4`: Schema je
Tabelle (sonst greift die Existenzprüfung für die zweite Tabelle nie und das `ALTER TABLE` läuft
bei jedem Aufruf erneut) und DDL über eine eigene `OleDbConnection` statt über
`DataRepository.ExecuteSQL`, das seine Fehler selbst als Dialog zeigt.

### N1.3 Warum die Vorbelegung „€/a" lautet

Rechnerisch sind **alle drei** Einheiten neutral, solange der Betrag 0 ist: 0 €/a,
0 €/kWh × Menge und 0 %/a ergeben gleichermaßen 0 €. „Unschädlich" allein entscheidet also
nicht. Den Ausschlag geben drei andere Gründe:

1. **Einzige selbsttragende Einheit.** `€/a` braucht weder einen Simulationslauf noch eine
   erfasste Investitionsposition. Bei jeder anderen Vorbelegung bekämen **alle** Bestandsprojekte
   sofort einen Hinweis auf eine fehlende Bezugsgröße — für einen Wert, den nie jemand gepflegt
   hat. Das ist der konkrete Schaden, den die anderen beiden Einheiten anrichten würden.
2. **Geringster Schaden bei der ersten Eingabe.** Trägt jemand später eine „50" ein, ohne auf die
   Einheit zu achten, sind das 50 €/a. Unter `€/kWh` wären daraus bei 22.430 kWh Jahreswärme
   1.121.500 €/a geworden, unter `%/a` die Hälfte der Investition. `€/a` ist die Lesart, die
   nicht stillschweigend um Größenordnungen danebenliegt.
3. **Der VDI-3805-Import gibt keine Gegenprobe her.** Er schreibt kein importiertes
   Wartungsentgelt, sondern den Modell-Vorgabewert 0 (`Form_Heizkessel_einlesen.cs:313/331`
   setzen `Wartungskosten` aus einem frisch erzeugten `HeizkesselModel`); der Parser liest gar
   kein Wartungsfeld. Es gibt also keine importierte Semantik, die für eine andere Einheit
   spräche.

Zusätzlich gilt weiterhin die Hausregel aus `605dcb8`: **Betrag 0 ist ungepflegt.** Trägt kein
Kessel eines Projekts einen Betrag > 0, gibt es keine Zahl und keinen Rechenweg, sondern
denselben Hinweis wie bei den Gewerken ohne Wartungsfeld. Für den gesamten Bestand ändert sich
durch Schritt 15 damit **nichts** außer einer nun vollständigen Angabe.

### N1.4 Oberfläche — `Form_Heizkessel_Bearbeiten`

**Warum dieser Dialog.** Er ist der einzige Eingabeweg für Kesseldaten: Sowohl der Katalogbrowser
`Form_Heizkessel_Admin.cs:141/155` als auch der Projektdialog `Form_Heizkessel.cs:628` öffnen für
„Bearbeiten" bzw. „Neu" **dieses** Formular. Die übrigen Kostenfelder (`Investitionskosten`,
`Raumbedarf`, `Nutzungsdauer`) stehen bereits hier in der Rubrik „Eingabedaten zur Berechnung der
Kosten". Damit ist er das genaue Gegenstück zu `Form_DBBHKW`, wo die BHKW-Wartungskosten mit dem
Suffix „€ / kWhel" sitzen — die Analogie, nach der der Auftrag gefragt hat. In die Projektkopie
`Tab_Heizkessel` gelangen die Werte auf demselben Weg wie alle übrigen: über
`HeizkesselCtrl.CopyFromStamm`.

**Warum zur Laufzeit statt im Designer.** Projektregel: Designer- und `.resx`-Dateien werden
nicht von Hand editiert. Der Designer scheidet hier zusätzlich praktisch aus, weil dieses
Formular seine Koordinaten in **zwei** Ressourcendateien führt
(`Form_Heizkessel_Bearbeiten.resx` und `…en-US.resx`) — ein von Hand ergänztes Control müsste in
beiden stehen, sonst springt es beim Sprachwechsel. Der gewählte Weg ist derselbe, den die
neueren Masken dieser Session gegangen sind: `Form_PlanwertUebernahme` kommt ganz ohne
Designer-Datei aus, `ucKostenItem.cs:28` hebt seine Betragsgrenze programmatisch an.

**Maße relativ, Breite gedeckelt.** Alle Positionen leiten sich aus den vorhandenen Controls der
Rubrik ab (`Label17.Right`, `tb_Investitionskosten.Top/Size`, `tb_Raumbedarf.Top`,
`tb_Nutzungsdauer.Top`), nicht aus abgeschriebenen Designer-Koordinaten — zur Laufzeit hat
`AutoScaleMode.Font` die Rubrik von 344/129 auf 304/114 gestaucht, feste Pixel wären falsch
gewesen. Wie weit die Rubrik nach rechts wachsen darf, ermittelt `FreieBreite()`
(`Form_Heizkessel_Bearbeiten.cs:192`) aus den **tatsächlichen Geschwistern** des Formulars. Der
erste Entwurf mit fester Breite hätte `groupBox5` um 30 Pixel überdeckt; das ist im Harnisch
aufgefallen und mit dem Deckel behoben (Endstand: Rubrik `{X=17,Y=304,W=511,H=114}`, keine
Kollision).

| Was | Wo |
|---|---|
| Feldaufbau, Zeilen und Spalten | `Form_Heizkessel_Bearbeiten.cs:110` (`WartungsfeldAufbauen`) |
| Breitendeckel aus den Nachbarn | `Form_Heizkessel_Bearbeiten.cs:192` (`FreieBreite`) |
| Auswahleintrag Schlüssel/Anzeige | `Form_Heizkessel_Bearbeiten.cs:214` (`EinheitItem`) |
| Laden aus dem Katalog | `Form_Heizkessel_Bearbeiten.cs:283` |
| Zahlenprüfung am Knopf | `Form_Heizkessel_Bearbeiten.cs:455` |
| Schreiben ins Modell | `Form_Heizkessel_Bearbeiten.cs:496` |

**Nebenbefund mit erledigt — hier lag die Ursache der lauter Nullen.** `InitDatensatzUpdate`
setzte `Wartungskosten` **nie**. Das Modell entstand mit dem Vorgabewert 0, und jedes Speichern
im Katalog-Editor schrieb den Wert damit auf 0 zurück. Genau deshalb stand das Feld in allen 21
Katalog- und 44 Projektzeilen auf 0 — es war nicht „nie gepflegt", sondern **wiederholt
gelöscht**. Behoben mit `Form_Heizkessel_Bearbeiten.cs:495-496`.

### N1.5 Kostenübernahme — `TechnikPlanwertCtrl.KesselPlanwert`

Neu in `Controller/TechnikPlanwertCtrl.cs:637`; Einstieg über
`LiesBetriebsplanwert(projektID, komponente, komponentenID)` (`:487`). Die Signatur hat einen
dritten Parameter bekommen, weil die Einheit `%/a` die **erfasste Investitionsposition** der
Komponente als Bezugsgröße braucht; beide Aufrufer reichen ihn durch
(`Views/Kosten/Form_Kosten.cs:733` und `:1033`).

**Die Bezugsgrößen sind GEWERKgrößen, keine Gerätegrößen.** `Tab_ErgebnisHeizkessel` führt genau
**eine** Zeile je Lauf — die Wärme aller Kessel zusammen, anders als beim BHKW, wo
`Tab_ErgebnisBHKWModul` je Modul aufschlüsselt. Die Investitionsposition ist ohnehin eine Zahl
für das ganze Gewerk. Eine Aufteilung auf einzelne Kessel gibt die Datenlage nicht her. Daraus
folgt die Rechenweise:

| Einheit | Rechnung | Braucht | Bei mehreren Kesseln |
|---|---|---|---|
| `€/a` | Summe der Jahresbeträge | nichts weiter | Beträge **addieren** sich |
| `€/kWh` | Satz × `Waermeproduktion` × 1000 des jüngsten Laufs | Simulationslauf | Satz muss **eindeutig** sein, dann **einmal** auf die Gesamtmenge |
| `%/a` | Satz/100 × Investitions-Hauptposition | erfasste Investition | Satz muss **eindeutig** sein, dann **einmal** auf die Gesamtinvestition |

Bei einem uneindeutigen Satz wäre Σ Satzᵢ × Q die vierfache Wartung für vier Kessel — deshalb in
diesem Fall keine Zahl, sondern `KOSTEN_BETRIEB_NICHT_ZUORDENBAR`. Führen die Kessel eines
Projekts **unterschiedliche Einheiten**, gibt es ebenfalls keinen rechenbaren Gesamtwert; dafür
gibt es den neuen Hinweis `KOSTEN_BETRIEB_EINHEIT_GEMISCHT`.

**Die Hinweisregel des Auftrags ist damit erfüllt:** Ohne Simulationsergebnis bleibt es bei der
bestehenden Regel (Hinweis statt Zahl) **nur** für die mengenabhängige Einheit `€/kWh`; `€/a`
liefert auch ohne Lauf eine Zahl, und `%/a` braucht keinen Lauf, sondern die
Investitionsposition — fehlt sie, gibt es den eigenen Grund `KOSTEN_BETRIEB_OHNE_INVESTITION`.

Der bisherige Sammelhinweis `KOSTEN_BETRIEB_KESSEL_UNKLAR` („Die Einheit … ist nicht belegt")
ist damit gegenstandslos und aus Code und beiden `.resx` entfernt.

### N1.6 Übrige Gewerke — geprüft, bewusst unverändert

Der Auftrag verlangte, das Muster **nur** dort zu übertragen, wo die Einheit heute ebenfalls
unbelegt ist.

| Gewerk | Wartungsfeld | Einheit belegt? | Ergebnis |
|---|---|---|---|
| BHKW | `Wartungskosten_kwhel` | **ja** — `Form_DBBHKW.designer.cs:602-608` beschriftet `Label19` neben dem Feld mit „€ / kWhel" | unverändert |
| Heizkessel | `Wartungskosten` | **nein** | wählbar gemacht |
| Stromspeicher | `Verschleisskosten` | eigene Wirtschaftlichkeit in `StromspeicherSimCtrl` | unverändert (eine zweite Position wäre Doppelzählung) |
| WP, PV, Solarthermie, Pufferspeicher | — | kein Wartungsfeld in der Gerätetabelle | unverändert |

---

## N2. Entscheidung zu Punkt 2 — PV und Solarthermie: Preis × Stückzahl

### N2.1 Die Semantikprüfung: `PV_Leistung` ist eine STÜCKZAHL, keine Leistung

Der Auftrag verlangte ausdrücklich, das vor dem Multiplizieren zu klären. Der Name legt eine
Leistung nahe, die Datenlage widerlegt ihn:

| Beleg | Befund |
|---|---|
| `Allgemein/Simulation/SimulationPV.cs:100` | `nFlaecheGesamt = ctrlsol.m_Breite * ctrlsol.m_Laenge * (long)ctrl.items[n].PV_Leistung` — der Wert wird mit den **Modulmaßen** multipliziert. Das ergibt nur als Anzahl eine Fläche. |
| `SimulationPV.cs:130` | derselbe Wert wird als `Anzahl` in `PVModulErgebnis` geschrieben |
| Oberfläche | das Eingabefeld ist mit „Anzahl Module" beschriftet |
| Rohdaten | Werte 20/30 bei Modulen mit 260–290 **W** Nennleistung — als kWp wären das 77–115 Module, als Anzahl ergibt sich mit `Tab_PV.Leistung` eine plausible Anlage |
| `Tab_PV` | führt `Leistung` (W je Modul), `Laenge`, `Breite`, `Modulkosten` — ein Katalog **je Modul** |

`Kollektormodulanzahl` ist ebenso eindeutig: `SimulationSolarthermie` multipliziert sie mit der
**Aperturfläche eines** Kollektors, und die Spalte ist ein LONG.

Beide Kostenfelder (`Tab_PV.Modulkosten`, `Tab_Solarkollektoren.Investitionskosten`) stehen im
jeweiligen **Modul**-Katalog neben Modulmaßen bzw. Modulfläche und sind mit „€" beschriftet —
also ein Betrag je Modul. **Die Multiplikation ist damit belegt, nicht vermutet**, und wurde
umgesetzt.

### N2.2 Umsetzung

| Was | Wo |
|---|---|
| Stückzahlspalte je Gewerk in der Landkarte | `TechnikPlanwertCtrl.cs:149` (`Plan.Mengenspalte`) |
| Stückzahl je Gerät ermitteln und **aufsummieren** | `TechnikPlanwertCtrl.cs:201` (`LiesAnlagen`) |
| Feld an der Anlage | `TechnikPlanwertCtrl.cs:108` (`Anlage.Menge`) |
| Kostenbasis Preis × Stückzahl | `TechnikPlanwertCtrl.cs:352` (`Stueckpreis`), aufgerufen `:304` (PV) und `:308` (Solarthermie) |
| Herleitung in der Übernahmemaske | Ressource `KOSTEN_PLANWERT_HERL_MENGE` |

**Die Stückzahl wird über die Anlagenzeilen SUMMIERT, nicht verworfen.** Die Entdoppelung je
Gerät aus Befund D2 bleibt bestehen — aber sie darf die Menge nicht wegwerfen: Mehrere
Anlagenzeilen auf dasselbe PV-Modul sind kein Fehler, sondern der Regelfall (jede Zeile ist ein
eigenes Feld mit eigener Neigung, Ausrichtung und Modulzahl). Genau so rechnet die Engine:
`SimulationPV` läuft über die Anlagenzeilen und nimmt je Zeile deren `PV_Leistung`. Die
Kostenseite muss dieselbe Anlage beschreiben wie der Rechenkern. Aus `SELECT DISTINCT` wurde
deshalb `GROUP BY` mit `SUM(<Mengenspalte>)`; für die fünf Gewerke ohne Stückzahl ist die Abfrage
verhaltensgleich geblieben (belegt durch die unveränderten Regressionsproben D1…D16).

**Ganzzahlig abgeschnitten wie in der Engine.** `SimulationPV` castet mit `(long)`; eine
eingegebene 10,5 wird dort als 10 Module gerechnet. `Math.Truncate` in `LiesAnlagen` hält Kosten
und Ertrag auf derselben Anlage.

**Kostenbasis `SPEZIFISCH` statt `MODULPREIS`.** Der Wert ist jetzt „spezifischer Preis ×
Baugröße" — dieselbe Bauform wie beim BHKW (€/kWel × kWel) und beim Stromspeicher (€/kWh × kWh).
Anzeigename und Herkunftsspalte des Übernahmedialogs stimmen damit ohne Sonderfall, und die
Rechnung steht dort im Klartext: **„468,89 €/Modul × 20 Module"**. Eine echte Auswahl entsteht
dadurch **nicht** — beide Gewerke führen weiterhin genau ein Kostenfeld, es bleibt bei einer
Basis je Anlage.

**Verhaltensänderung, die benannt sein will.** Eine Anlage mit **0 Modulen** trägt jetzt nichts
mehr bei, statt wie bisher den nackten Modulpreis anzusetzen. Das ist die richtige Aussage —
0 Module kosten 0 —, aber es ist eine Änderung. Überschrieben wird dadurch nichts: Weicht eine
erfasste Position ab, erscheint sie in der Abweichungsanzeige und wird nur auf Knopfdruck
angeglichen (Entscheidung 4 der Vorgängeretappe).

### N2.3 Nachgerechnete Zahlenproben gegen die Rohdaten

| Projekt | Gewerk | Rohdaten | Planwert neu | Bisher |
|---|---|---|---|---|
| 1007 | PV, Gerät 1007005 | 468,89 €/Modul × 20 Module | **9.377,80 €** | 468,89 € |
| 1026 | Solarthermie, Gerät 1011013 | 3.775,00 €/Modul × 10 Module | **37.750,00 €** | 3.775,00 € |
| 1011 | PV, Gerät 1011008 über **zwei** Anlagenzeilen (30 + 30) | 100,00 €/Modul × 60 Module | **6.000,00 €** | 100,00 € |
| 1011 | Solarthermie, Gerät 1011001 über **zwei** Zeilen (1 + 1) | 500,00 €/Modul × 2 Module | **1.000,00 €** | 500,00 € |
| 1007 | PV, Gerät 1007006 | 300,00 €/Modul × **0** Module | **keine Basis** | 300,00 € |

(Die Preise der drei unteren Zeilen sind Testwerte in der Wegwerf-Kopie — in der
Ausgangsdatenbank stehen diese Kostenfelder auf 0 und ergäben ohnehin keine Basis.)

---

## N3. Geänderte Dateien

| Datei | Inhalt |
|---|---|
| `Allgemein/DbWerte.cs` | drei Persistenzwerte der Wartungseinheit samt Begründung der Vorbelegung |
| `Allgemein/Update/SchemaKatalog.cs` | Tabellennamen, Spaltenname, `Schritt15_KesselWartungseinheit`, Begründung für das Fernbleiben aus `Alle` |
| `Allgemein/Update/SchemaMigration.cs` | `ZIEL_VERSION` 14 → 15, Schritt 15 (DDL + DML), Zählwerk, Registrierung |
| `Model/HeizkesselModel.cs` | Feld `Wartungskosten_Einheit`, Vorgabe „€/a" |
| `Controller/HeizkesselCtrl.cs` | Spalte in `Insert`/`Update`/`CopyFromStamm`/`FillModelFromRow`, Rückfallebene `Einheit(...)` |
| `Controller/HeizkesselStammCtrl.cs` | dieselbe Spalte in `Insert`/`Update`/`FillModelFromRow`, neue Vorsorge `StelleSpaltenSicher` |
| `Controller/TechnikPlanwertCtrl.cs` | Einheitenkatalog, `KesselPlanwert`, dritter Parameter an `LiesBetriebsplanwert`, Stückzahl in `LiesAnlagen`, `Stueckpreis` für PV/Solarthermie |
| `Views/Heizkessel/Form_Heizkessel_Bearbeiten.cs` | Wartungskostenfeld + Einheitenauswahl (zur Laufzeit), Laden/Prüfen/Speichern, Behebung des stillen Nullsetzens |
| `Views/Kosten/Form_Kosten.cs` | `komponentenID` an beide Aufrufe von `LiesBetriebsplanwert` |
| `MyResource/Resource*.resx`, `Resource.Designer.cs` | 11 neue Schlüssel (de + en), 1 entfallener |

**Kodierung.** `Form_Heizkessel_Bearbeiten.cs` ist CP1252 (eine der 93 Nicht-UTF-8-Dateien des
Projekts) und wurde über den Hin-/Rückweg mit `iconv` und Rundprobe (`cmp`) bearbeitet; alle
zwölf Dateien behalten ihre Ausgangskodierung und CRLF, kein U+FFFD.

---

## N4. Verifikation

Reflection-Harnisch (net8.0-windows, x86) gegen eine **Wegwerf-Kopie** der `Kenndaten.accdb`
unter `C:\Waermeplan\_ke`; Produktiv-DB ausschließlich lesend kopiert (`Kenndaten.laccdb` vorher
geprüft — nicht vorhanden). Build in ein Scratch-`OutDir`, `bin\` unberührt: **0 Fehler, exakt 6
Bestandswarnungen** (CS0108 ×2, CS0109 ×2, CS1998, CS4014). Modale Dialoge fängt ein
Wächter-Thread (`EnumThreadWindows` auf `#32770`).

Datengrundlage: Projekt 1018 (Kessel 1018328, Lauf 171 mit `Waermeproduktion` 22,43 MWh/a =
22.430 kWh/a, erfasste Investitions-Hauptposition Heizkessel 15.000 €), Projekt 1017 (Kessel
1017237, **kein** Simulationslauf), Projekte 1007/1011/1026 für PV und Solarthermie.

| Nr. | Prüfung | Erwartet | Ergebnis |
|---|---|---|---|
| A1–A3 | Migration 14 → 15 | Marker in `Tab_Applikation` steht auf 15 | OK |
| A4–A6 | Vorbelegung | 65 Zeilen (44 Projekt + 21 Katalog), alle „€/a" | OK |
| A7–A9 | Bestandswerte unversehrt | Wartungsbeträge 0, Investitionen 66.494,06 € / 34.080,67 € unverändert | OK |
| A10–A12 | Doppelstart (Marker zurückgesetzt) | wieder Stand 15, **0** weitere Vorbelegungen, ein von Hand gesetztes „%/a" bleibt stehen | OK |
| B1–B2 | Einheit `€/a` | 480 € → **480,00 €/a**, Herleitung nennt den Betrag | OK |
| B3–B4 | Einheit `€/kWh` | 0,02 × 22.430 = **448,60 €/a**, Herleitung nennt Menge und Lauf | OK |
| B5–B6 | Einheit `%/a` | 3 % von 15.000 = **450,00 €/a**, Herleitung nennt die Investition | OK |
| B7–B8 | Betrag 0 | keine Zahl, Hinweis „keine Wartungsangaben" | OK |
| B9–B10 | ohne Lauf, `€/kWh` | keine Zahl, Hinweis „erst nach einem Simulationslauf" | OK |
| B11 | ohne Lauf, `€/a` | **300,00 €/a** trotzdem — kein Lauf nötig | OK |
| B12 | `%/a` ohne Investitionsposition | keine Zahl, eigener Hinweis | OK |
| B13–B14 | zwei Kessel | `€/a` addiert (200 + 150 = **350,00**); gemischte Einheiten → Hinweis | OK |
| B15–B16 | zwei Kessel, `%/a` | gleicher Satz → **einmal** 450,00 €; verschiedene Sätze → keine Zahl | OK |
| C1–C7 | PV 1007 | 1 Basis `SPEZIFISCH`, **9.377,80 €**, Herleitung „468,89 €/Modul × 20 Module", nicht mehrdeutig | OK |
| C8–C10 | 0 Module bei gepflegtem Preis | keine Basis, Gewerksumme bleibt 9.377,80 € | OK |
| C11 | Solarthermie 1026 | 3.775 × 10 = **37.750,00 €** | OK |
| C12–C16 | zwei Anlagenzeilen auf ein Gerät | 1 Gerät, Stückzahl **summiert** (60 bzw. 2), 6.000,00 € / 1.000,00 € | OK |
| C17 | Gewerk ohne Stückzahl | Wärmepumpe unverändert | OK |
| D1–D5 | **Regression** BHKW-Auswahl | zwei Basen 33.000 € und 32.750 €, mehrdeutig, kein stiller Wert | OK |
| D6–D7 | **Regression** Nebenkostenzeilen | 3 von 4 Posten (nur > 0), Summe 4.500 € | OK |
| D8–D9 | **Regression** kein Doppelanlegen | 1 → 4 Zeilen, zweiter Aufruf 4 → 4 | OK |
| D10–D13 | **Regression** Abweichungsanzeige | erfasst 15.000 €, Technik 12.000 €, gemeldet; BHKW meldet offene Auswahl | OK |
| D14 | **Regression** Kachel gleich Tabelle | 55.500,00 € == 55.500,00 € | OK |
| D15–D16 | **Regression** BHKW-Betriebskosten | 0,04 €/kWhel × 13.230 kWhel = **529,20 €/a**; WP-Hinweis unverändert | OK |
| E (21 Proben) | de / en | alle 11 Schlüssel in beiden Sprachen belegt und übersetzt; Persistenz- und Steuerwerte bleiben auf englischer Oberfläche unverändert | OK |
| F1–F5 | Maske: Aufbau und Lage | Feld + Auswahl vorhanden, in der Rubrik, **keine Kollision** mit Nachbarcontrols | OK |
| F6–F10 | Maske: Laden und Speichern | 123,45 € + „%/a" gespeichert und zurückgelesen | OK |
| F11–F13 | `CopyFromStamm` | Betrag **und** Einheit landen in der Projektkopie | OK |
| F14 + Wächter | Dialoge | genau **ein** Dialog (die Speicherbestätigung), sonst **keiner** | OK |

**101 Prüfungen, 0 Fehlschläge.**

Während der Verifikation aufgefallen und behoben: Der erste Layoutentwurf machte `groupBox3`
30 Pixel zu breit und hätte `groupBox5` überdeckt. Die Rubrikbreite wird seither aus den
tatsächlichen Nachbarn gedeckelt (`FreieBreite`), nicht mehr aus einer geschätzten Zahl.

---

## N5. Offene Punkte (Stand nach diesem Nachtrag)

Von den fünf Punkten aus Abschnitt 7 sind **1 und 2 erledigt**. Es bleiben:

1. **Korrigierter Stromspeicher-Planwert** (bisher Punkt 3) — die Formel
   `Modulkosten × Energie + Leistungskosten × Leistung + Investition_Fix` gilt, in den Testdaten
   steht `Modulkosten` aber durchgehend auf 0; an echten Zahlen ist der Unterschied weiterhin
   nicht gemessen.
2. **Reiterbeschriftungen von `Form_Kosten`** (bisher Punkt 4) — „Investitionskosten" /
   „Betriebskosten" / „Energiekosten" sind weiterhin deutsche Designer-Literale und zugleich
   SQL-Vergleichswert gegen `Tab_KostenKategorie.KategorieName`. Unverändert.
3. **Nachtrag zu Punkt 5:** Die drei Fehlerdialoge beim Öffnen der Kostenmaske sind mit `87483b4`
   behoben; der Harnisch dieser Etappe bestätigt es (der Wächter meldet außerhalb der
   Speicherbestätigung **keinen** Dialog).
4. **Neu: Die Wartungseinheit ist nur im Katalog pflegbar.** Eine bereits in ein Projekt kopierte
   `Tab_Heizkessel`-Zeile bekommt eine geänderte Einheit erst über einen erneuten
   `CopyFromStamm` bzw. über `KomponentenUebernahmeCtrl`. Das ist das Verhalten **aller** übrigen
   Kesselfelder (auch `Investitionskosten`) und damit konsistent — sollte der Anwender die
   Einheit je Projekt abweichend pflegen wollen, wäre das eine eigene Entscheidung mit einem
   eigenen Eingabeweg im Projektdialog.
5. **Neu: PV-Anlagen mit Stückzahl 0.** Sie tragen jetzt 0 € statt eines Modulpreises bei. Wo
   eine Kostenposition bereits auf dem alten Wert steht, erscheint sie als Abweichung. Das ist
   gewollt (Entscheidung 4: melden statt überschreiben), heißt aber, dass betroffene Projekte
   einmal über „Planwert übernehmen…" nachgezogen werden sollten.

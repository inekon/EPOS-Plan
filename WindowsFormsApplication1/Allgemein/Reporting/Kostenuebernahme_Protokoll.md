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

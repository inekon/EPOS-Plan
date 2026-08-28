# Konzept: Projekttransfer — Export und Import zwischen Rechnern (EPOS-Plan)

**Rev. 1 (zur Abnahme)** · 27.08.2026 · Branch `kostenformulare`

Auftrag: „Ich möchte ein Projekt mit der Option, auch die Varianten davon zu
exportieren, so dass sie auf einem anderen Computer importiert werden können."

---

## 1. Ziel

Ein Projekt — wahlweise samt seiner Varianten — wird in **eine einzelne
Transportdatei** exportiert und auf einem anderen Rechner mit EPOS-Plan als
**neues Projekt** importiert. Nach dem Import rechnet das Projekt dort
identisch: gleiche Anlagen, gleiche Kosten, gleiche Simulations- und
Wirtschaftlichkeitsergebnisse. Der Transfer läuft über eine Datei (USB-Stick,
E-Mail, Netzlaufwerk) — ohne Server, ohne gemeinsame Datenbank.

## 2. Bestandsaufnahme — worauf gebaut wird

Der schwierige Teil existiert bereits: **`ProjektDuplizierenCtrl`** kopiert
heute einen kompletten Projektbaum innerhalb der Datenbank (Varianten-Anlegen).
Seine Bausteine sind exakt die, die ein Transfer braucht:

| Baustein | Leistung |
|---|---|
| `ErmittlePlan` | ermittelt **generisch aus dem Schema**, welche Tabellen zum Projektbaum gehören (`Spec`: Tabelle, PK, Namensspalte) — eine neue Projekttabelle ist automatisch dabei |
| `KATALOG_TABELLEN` / `KATALOG_SPALTEN` | wissen, was **globaler Katalog** ist (wird nie kopiert) und welche Spalten auf Kataloge zeigen (werden nie versetzt) |
| `FK_MAP` / `FK_OVERRIDE` / `_echteFks` | die Fremdschlüssel-Landkarte (deklarierte Access-Beziehungen zuerst, Namenskonvention als Rückfall) |
| `KINDER` | Tabellen ohne `ID_Projekt`, die über Eltern-FK gefiltert werden (Kennlinien `Tab_Kenndaten` über `ID_WP`, Ganglinien-`*Daten` über `ID_Ganglinie`) |
| Offset-Verfahren | `offset(T) = MAX(T) − MIN(Quellzeilen) + 1`; jede ID-Spalte wird mit dem Offset **ihrer Zieltabelle** versetzt |
| `AnkerNachziehen` (Ä24) | leitet die komponentenabhängigen Kostenanker (`ID_AnlageGeraet`) nach dem Kopieren aus den Zuordnungen neu ab |

Weitere vorhandene Bausteine: `Tab_Variante(ID, ID_Projekt, ID_ProjektRef,
Variantenname)` verknüpft Varianten mit ihrem Stamm; `Form_ImportKonflikte` ist
das etablierte Muster für Einlese-Konflikte (Heizkessel-/Puffer-/CEC-Import);
`SchemaMigration.ZIEL_VERSION` (derzeit 47) stempelt den Schemastand;
`IProgress<Fortschritt>` liefert die Fortschrittsanzeige des Duplizierers.

**Wichtig:** Der Projektbaum enthält die **Gerätekopien** (`Tab_WP`, `Tab_BHKW`,
… samt Kennlinien-Kindern). Ein importiertes Projekt simuliert deshalb auch
dann korrekt, wenn der Gerätekatalog des Zielrechners abweicht.

## 3. Grundentscheidungen

**G1 — Transportbehälter: eine Access-Containerdatei.** Die Exportdatei ist
eine kleine `.accdb` mit eigener Endung (Vorschlag `.eposproj`, TF1). Begründung:

- `SELECT … INTO [Tabelle] IN '<datei>'` **erzeugt** die Zieltabellen samt
  Spaltentypen; `INSERT INTO … SELECT … FROM [Tabelle] IN '<datei>'` liest sie
  zurück — der bewährte SQL-Weg des Duplizierers funktioniert quer über
  Dateigrenzen. Keine Serialisierung, keine Typtreue-Fehlerklasse (Datum,
  Double, NULL, Umlaute), kein neues Parser-Risiko.
- ACE x64 ist auf jedem EPOS-Plan-Rechner vorhanden (App-Voraussetzung).
- Der Katalog-Beipack (§ 4) fährt als gewöhnliche Tabellen mit.
- Die Original-IDs bleiben beim Export unverändert; **nur der Import versetzt**
  (gleiches Offset-Verfahren wie beim Duplizieren).

*Alternative geprüft und verworfen (Rev. 1):* JSON/ZIP wäre diff-freundlicher,
kauft das aber mit einer eigenen Serialisierungsschicht und Typtreue-Risiken.

**G2 — Eine Wahrheit: der Transferkern.** Planermittlung, Offset-Rechnung und
SQL-Bau werden aus `ProjektDuplizierenCtrl` in einen gemeinsamen Kern gehoben
(Arbeitsname `ProjektTransferKern`), den drei Wege teilen: (1) Duplizieren in
derselben DB (Bestand, Verhalten unverändert), (2) Export in die Containerdatei,
(3) Import aus der Containerdatei. Keine zweite FK-Landkarte, kein zweiter
Tabellenplan.

**G3 — Import erzeugt immer ein NEUES Projekt.** Kein Zusammenführen zweier
Stände desselben Projekts (Abgrenzung § 10). Namenskonflikt → Namensvorschlag
mit Suffix (TF8), im Import-Dialog änderbar.

**G4 — Schemaversion: in Rev. 1 strikt gleich.** Das Manifest trägt
`ZIEL_VERSION` und App-Version. Import nur bei gleicher Schemaversion; sonst
klare Meldung („Exportstand 47, dieser Rechner 46 — bitte zuerst beide Rechner
auf denselben Stand bringen"). Begründung: Datenmigrationsschritte laufen
datenbankweit genau einmal — ein projektbezogenes Nachziehen älterer Exporte
wäre eine eigene Etappe (TF6). Da beide Rechner ohnehin über Git denselben
App-Stand beziehen, ist die strenge Regel im Alltag keine Hürde.

## 4. Exportumfang

1. **Projektbaum des Stammprojekts**: alle `Spec`-Tabellen mit ihren Zeilen
   (inkl. Gerätekopien, Kennlinien, Ganglinien-Kindern, Kostenpositionen,
   gespeicherten Ergebnissen — alles, was der Duplizierer heute kopiert).
2. **Optional Varianten** (die Kernanforderung): Der Export-Dialog listet alle
   Varianten des Projekts (`Tab_Variante.ID_ProjektRef = Stamm`) mit Häkchen
   (Vorbelegung TF2). Je gewählter Variante wandert ihr kompletter Projektbaum
   mit; die `Tab_Variante`-Verknüpfungszeilen fahren mit und werden beim Import
   wiederhergestellt. Exportiert wird immer **vom Stamm aus** — eine einzelne
   Variante ohne ihren Stamm ist kein Exportfall (sie wäre am Ziel ein
   verwaistes „Variantenprojekt ohne Stamm").
3. **Katalog-Steckbriefe (Beipack)**: Für jede von `KATALOG_SPALTEN` berührte
   Katalogzeile (Energieträger `energy_carrier`, `Tab_KostenKomponente`,
   Kostenvorlagen, `pricing_model`, Brennstoffe, Klimaregion, …) schreibt der
   Export die **vollständige Katalogzeile** in eine Spiegel-Tabelle
   (`_Kat_<tabelle>`). Sie dient dem Import als Auflösungs- und Anlagequelle
   (§ 5) — der Zielkatalog wird dadurch NIE überschrieben.
4. **Manifest** (`_Manifest`-Tabelle): Schemaversion, App-Version, Exportdatum,
   Quellrechner, Projektname, Variantenliste, Zeilenzahlen je Tabelle. Der
   Import zeigt es als Vorschau, bevor irgendetwas geschrieben wird.
5. **Klimadaten**: Referenz läuft über den Klimaregion-Namen. Die (großen)
   Wetterdaten fahren nur mit, wenn der Haken „Klimadaten einschließen" gesetzt
   ist (Vorbelegung TF3) — im Regelfall haben beide Rechner dieselbe
   Auslieferung.

**Nicht exportiert:** andere Projekte, komplette Kataloge, Nutzereinstellungen,
Datenbank-Systemtabellen.

## 5. Importablauf

1. **Vorschau**: Datei öffnen (nur lesend), Manifest anzeigen (Name, Varianten,
   Exportdatum, Schemastand, Umfang). Schemaversion prüfen (G4). Zielname
   vorschlagen; bei Namenskonflikt Suffix (TF8), editierbar.
2. **Katalogauflösung** (vor dem Schreiben, je `_Kat_`-Eintrag über den
   **natürlichen Schlüssel** — Name/Bezeichner, dieselbe Philosophie wie
   `CopyFromStamm`):
   - Treffer am Ziel → Ziel-ID wird in den betroffenen Spalten eingesetzt.
   - Kein Treffer → Katalogeintrag wird aus dem Beipack **angelegt** (MAX+1,
     ADR-001) und im Bericht ausgewiesen.
   - Namensgleich, aber inhaltlich abweichend → **der Zielbestand gewinnt**
     (kein stiller Katalog-Überschrieb); der Bericht nennt die Abweichung.
   - Rev.-1-Vorschlag: still mit Abschlussbericht statt blockierendem
     Konfliktdialog (TF4); das `Form_ImportKonflikte`-Muster bleibt die
     Rückfalloption für spätere Runden.
3. **Schreiben in einer Transaktion** (wie der Duplizierer): je `Spec`-Tabelle
   `INSERT INTO … SELECT` aus der Containerdatei mit Offset-Umschlüsselung
   aller ID-Spalten und den aufgelösten Katalog-IDs; Stamm zuerst, dann
   Varianten, dann `Tab_Variante`-Verknüpfungen. Fehler → Rollback, DB
   unverändert.
4. **Nacharbeiten** (bestehende Bausteine): `AnkerNachziehen` (Ä24) für die
   Kostenanker, `GeraeteWaisen`-Kontrolle, Varianten-Klapplisten aktualisieren
   (`VariantenAnzeigeAktualisieren`, Ä19).
5. **Abschlussbericht**: importierte Projekte/Varianten, Zeilen je Bereich,
   angelegte Katalogeinträge, Abweichungshinweise. Optional davor eine
   Sicherungskopie der Datenbank (TF5).

## 6. Oberfläche (Ä6: Designer-Dialoge, resx de/en, App-Design Navy)

- **Export**: Menü *Projekt → Projekt exportieren…* (zusätzlich Kontextmenü der
  Projektkarte, TF7). Dialog: Projekt-Klappliste (vorbelegt: aktuelles
  Projekt), Variantenliste mit Häkchen, Haken „Klimadaten einschließen",
  Zielpfad (`SaveFileDialog`, `.eposproj`), Zusammenfassungszeile („1 Projekt,
  2 Varianten, ~4,2 MB"), OK/Abbrechen, Fortschritt wie beim Duplizieren.
- **Import**: Menü *Projekt → Projekt importieren…*. Dialog: Dateiwahl →
  Manifest-Vorschau → Zielname(n) → Start → Abschlussbericht (kopierbar).
- Alle Texte dreischichtig (Persistenzwerte ≠ `MyResource` de+en).

## 7. Fehlerbilder und Meldungen

| Fall | Verhalten |
|---|---|
| Schemaversion ungleich | Import verweigert, Meldung nennt beide Stände und den Weg (App aktualisieren) |
| Datei kein gültiger Export (kein `_Manifest`) | Meldung „keine EPOS-Plan-Exportdatei" |
| Projektname existiert | Namensvorschlag mit Suffix, editierbar |
| Katalogeintrag fehlt am Ziel | wird aus dem Beipack angelegt, Bericht |
| Katalogeintrag weicht inhaltlich ab | Zielbestand gewinnt, Bericht |
| Klimaregion fehlt und Klimadaten nicht eingeschlossen | Import läuft; Projekt verlangt beim Öffnen die Regionwahl (Bestandsverhalten); Bericht weist darauf hin |
| Fehler beim Schreiben | Rollback, Datenbank unverändert |

## 8. Prüfstand (kd1runner, neuer Modus `transfer`)

1. **Roundtrip fremde DB**: Testprojekt (mit Varianten) aus der kd6-Kopie
   exportieren, in eine **zweite, frische Kopie** importieren; Abnahme:
   Zeilenzahlen je `Spec`-Tabelle gleich, FK-Integrität (0 Waisen, 0 lose
   Kostenpositionen), Stichproben-Feldvergleich.
2. **Rechenbeweis** (das stärkste Kriterium): Wirtschaftlichkeits-/
   Simulationskennzahlen des importierten Projekts == Quelle (kd2/pv6-artige
   Vergleichsmessung).
3. **Roundtrip gleiche DB**: Import in die Quell-DB verhält sich wie
   Duplizieren mit neuem Namen (W5/W6-Niveau).
4. **Verhaltensgleichheit T1**: Nach der Kernextraktion laufen kd6/Sweep/
   Fehlerjagd unverändert grün (Duplizierer-Verhalten unangetastet).

## 9. Etappen

| Etappe | Inhalt | Abnahmekriterium |
|---|---|---|
| T1 | Transferkern aus `ProjektDuplizierenCtrl` herauslösen (Plan/Offsets/SQL-Bau als gemeinsame Basis; Duplizieren nutzt ihn) | kd6 komplett grün, Sweep 115/0/5, Fehlerjagd 0 Befunde — Verhalten des Varianten-Anlegens unverändert |
| T2 | Export: Containerdatei mit Projektbaum, Varianten, Katalog-Beipack, Manifest | Zählvergleich Quelle↔Datei je Tabelle; Manifest vollständig |
| T3 | Import: Vorschau, Katalogauflösung, Transaktion, Umschlüsselung, Nacharbeiten, Bericht | Prüfstand § 8 (1)–(3) grün, insbesondere Rechenbeweis |
| T4 | Export-Dialog + Menü/Kontextmenü | Sichtbeleg, Sweep grün |
| T5 | Import-Dialog + Vorschau/Bericht | Sichtbeleg, Sweep grün |
| T6 | Prüfstand-Modus `transfer` dauerhaft, Doku (Konzept-Vermerke, Protokoll), Sichtabnahme | Runner-Modus im Soll, Abnahme durch Nutzer |

## 10. Abgrenzung (bewusst NICHT in diesem Konzept)

- Kein Teil-Export einzelner Anlagen oder Kostensätze (Einheit ist das Projekt).
- Kein Katalog-Abgleich zwischen Rechnern (eigenes Thema; der Beipack legt nur
  an, was dem Ziel fehlt).
- Kein Zusammenführen/Aktualisieren eines vorhandenen Projekts (Import = neues
  Projekt, G3).
- Kein Netzwerk-/Cloud-Transfer (die Datei ist der Weg).

**Verzahnung mit dem Konzept DB-Migration (Access→SQL):** Die Containerdatei
bleibt auch nach einem SQL-Server-Umstieg eine Access-Datei (ACE liest/schreibt
sie weiter); nur die Datenbankseite des Imports/Exports läuft dann über die
dortige Provider-Weiche. Im DB-Migrationskonzept als D-Punkt nachzutragen,
sobald dieses Konzept beschlossen ist.

## 11. Offene Entscheidungspunkte (zur Abnahme)

| Nr. | Frage | Vorschlag |
|---|---|---|
| TF1 | Dateiendung und Standard-Ablageort | `.eposproj`, zuletzt genutzter Ordner |
| TF2 | Varianten-Häkchen vorbelegt? | alle an (Kernanforderung „mit Varianten") |
| TF3 | Klimadaten-Beipack vorbelegt? | aus (beide Rechner haben dieselbe Auslieferung) |
| TF4 | Katalogauflösung still mit Bericht oder blockierender Konfliktdialog? | still + Bericht; Dialog als spätere Runde |
| TF5 | Sicherungskopie der DB vor dem Import? | Haken, vorbelegt an |
| TF6 | Ältere Exporte in neuere DB (projektbezogenes Migrieren)? | nicht in Rev. 1; strikt gleiche Schemaversion |
| TF7 | Menüorte | *Projekt*-Menü + Kontextmenü der Projektkarte |
| TF8 | Namenskonvention bei Konflikt | „<Name> (Import)", bei Bedarf „(Import 2)" |

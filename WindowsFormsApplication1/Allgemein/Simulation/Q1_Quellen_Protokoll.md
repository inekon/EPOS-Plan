# Paket Q1 — Quellen-Ausbau: Umsetzungsprotokoll

Stand: 28.08.2026 · Branch `Pufferspeicher` · Bezug:
[`Konzept_Brauchwasser_Heizung_Pufferspeicher.md`](Konzept_Brauchwasser_Heizung_Pufferspeicher.md)
Kapitel 8.1 (Quellen je Bauart), 8.2/8.4 (Quell-Entnahmehöhe), 9 (Schritt 54), 10
(`Form_Quellprofil`), 15 (Stillgelegtes), Paketzeile **Q1**. Vorleistungen
[`P1_Schichtmodell_Protokoll.md`](P1_Schichtmodell_Protokoll.md) (Schichtebene) und
[`B1_Booster_Protokoll.md`](B1_Booster_Protokoll.md) (Ticket B1-O1).
Build `WP-Plan.sln` + `Referenzlauf.csproj` x64 Debug: **0 Fehler**.
**Schema-Schritt 54** — `ZIEL_VERSION` 53 → **54**. Der letzte Schema-Schritt des Vorhabens.

> **Der Nummernblock 48–54 ist abgearbeitet.** Ein Quellprofil ist ab hier ein eigener
> Gegenstand der Datenbank statt zweier delimitierter Zeichenketten und eines Dateipfads,
> und die Quell-Entnahmehöhe des Boosters ist eine Anwenderangabe statt einer Annahme.

## 1. Umfang

Vier Dinge, die zusammengehören, weil sie alle an derselben Stelle hängen — der Wärmequelle
einer Anlage:

1. **Quellprofile in die Datenbank** (8.1 Punkt 2/3). `Tab_Quellprofil`/`Tab_QuellprofilDaten`
   mit den Betriebsarten **Monat (12)**, **Tag (365)** und **Stunde (8760)**. Die Tagesvariante
   kachelt **kalenderunabhängig**: Wert *i* gilt für die 24 Stunden des Tages *i*, ohne jeden
   Wochentagsbezug.
2. **Schlüssel- statt Indexkopplung** (8.1 Punkt 4). Die Anlage verweist über
   `WQ_ID_Quellprofil` auf ihr Profil; die Wärmequellen-Auswahl trägt ihre Steuerwerte am
   Eintrag statt über `SelectedIndex` in einer zweiten Liste.
3. **Quell-Entnahmehöhe** `WQ_Anschlusshoehe` (8.2/8.4). Sie löst Ticket **B1-O1** ab: Die
   Booster-Quelltemperatur kommt nicht mehr fest von oben, sondern von der gepflegten Höhe.
4. **Bauart-Bindung sichtbar** (8.1 Punkt 1). Bei Luft-Wasser steht die Quelle „Außenluft" als
   fester, nicht klickbarer Chip mit erklärendem Mouseover; die Abbruchmeldung bleibt als
   zweite Sicherung.

Dazu der Beifang **K1-O6**: Der additive Wochengang des Altwegs rechnete seinen Wochentag aus
`DateTime.Now.Year` — dasselbe Projekt hätte 2027 ein anderes Ergebnis geliefert als 2026. Der
Wert ist jetzt fest.

## 2. Bausteine

| Baustein | Inhalt | Stellen |
|---|---|---|
| **Schritt 54** | Zwei Tabellen (`Tab_Quellprofil` mit `ID_Projekt`/`Bezeichner`/`Betriebsart`/`Einheit`/`Beschreibung`, `Tab_QuellprofilDaten` mit `ID_Quellprofil`/`[Index]`/`Wert`), zwei Indizes, die Beziehung `FK_QuellprofilDaten_Kopf` **mit** Löschweitergabe, zwei Spalten an `Tab_Energieanlagen` (`WQ_Anschlusshoehe` DOUBLE, `WQ_ID_Quellprofil` LONG) und die **restriktive** Beziehung `FK_Anlage_Quellprofil`. **Kein DML** — der Schritt ändert keinen gespeicherten Wert | `SchemaMigration.Schritt_54_Quellen`, `SchemaKatalog` |
| **Positionsspalte statt ID-Reihenfolge** | Das Vorbild `Tab_StromganglinieDaten` hat keine — dort IST die Reihenfolge die ID-Reihenfolge, und ein nachträglich eingefügter Wert verschöbe die Zuordnung Wert → Stunde stillschweigend. `[Index]` macht sie ausdrücklich. `Index` ist in Access-SQL ein **reserviertes Wort**: jede Nennung steht in eckigen Klammern, im Migrationsschritt wie im Controller; die Projektkopie klammert ohnehin jeden Spaltennamen | `SchemaKatalog.SPALTE_QPD_INDEX` |
| **Betriebsart als Steuerwert** | `DbWerte.WQ_PROFIL_BETRIEBSART_MONAT/_TAG/_STUNDE` (deutsch, eingefroren) und `DbWerte.QuellprofilWerteanzahl` als **eine** Wahrheit für 12/365/8760 — die drei Zahlen stehen sonst nirgends im Quelltext | `DbWerte.cs` |
| **Kachelung** | `QuellprofilCtrl.Jahresprofil(betriebsart, werte)` ohne Datenbank, damit sie prüfbar ist: Monat über die Monatslängen des Nicht-Schaltjahres, **Tag über `profil[h] = werte[h/24]`**, Stunde unmittelbar. Passt die Wertzahl nicht zur Betriebsart, ist die Antwort `null` statt eines mit Nullen aufgefüllten Jahres | NEU `Controller/QuellprofilCtrl.cs` (415 Z.) |
| **Lesekette der Engine** | `WaermequelleClass.Quelltemperatur`, Zweig `TYP_PROFIL`: **Stufe 1** `WQ_ID_Quellprofil` → `QuellprofilCtrl.Jahresprofil`; **Stufe 2** die Lese-Altlast `WQ_Monatswerte`/`WQ_Wochenwerte`. Ein Profil, das fehlt oder unvollständig ist, erzeugt eine Protokollwarnung und fällt auf Stufe 2 bzw. die Außentemperatur zurück — nie stumm | `WaermequelleClass.cs` |
| **Schreibweg** | `QuellprofilCtrl.Speichern` schreibt Kopf, Löschen der Altwerte und bis zu 8760 Zeilen in **einer** Transaktion mit einem wiederverwendeten Befehl (1,1 s für 8760 Zeilen, gemessen). `@@IDENTITY` wird auf derselben Verbindung IN der Transaktion gelesen | `QuellprofilCtrl.cs` |
| **`WQ_Anschlusshoehe`** | `SimulationPufferspeicher.QuellEntnahmeTemperatur` ist von einer Eigenschaft zu einer **Methode mit Höhenparameter** geworden (Begründung in Abschnitt 3). Die Höhe liest die Engine **einmal je Lauf** je gekoppeltem Modul (`SimulationWaermepumpe.AnschlusshoeheLesen`); NULL, fehlende Spalte und ein Wert außerhalb 0…1 ergeben „oben" und damit exakt das B1-Verhalten | `SimulationPufferspeicher.cs`, `SimulationWaermepumpe.cs`, `SimulationSPK.cs`, `SimulationControl.cs` |
| **K1-O6 erledigt** | `WaermequelleClass.WOCHENTAG_JAN1_ALTWEG = 3` statt `DateTime.Now.Year`. **3 ist genau der Wert, den die Altfassung heute liefert** (2026 ist kein Schaltjahr, der 1. Januar 2026 ein Donnerstag) — für den Bestand ergebnisgleich und ab sofort unveränderlich. Bewusst NICHT auf den Klimadaten-Kalender (F3) umgestellt: Der Altweg ist Lese-Altlast, und die richtige Änderung an einer Altlast ist die kleinstmögliche | `WaermequelleClass.cs` |
| **`WQ_TYP_OHNE` konsistent** | Der Leerwert wurde an drei Stellen dreimal anders geprüft (`IsNullOrEmpty` in Engine und Anzeige, `Trim().Length > 0` im Warnkatalog) — und die Konstante `DbWerte.WQ_TYP_OHNE` kam an keiner davon vor. Jetzt: `WaermequelleClass.OhneQuelle(typ)` als eine Wahrheit, gebaut über die Konstante | `WaermequelleClass.cs`, `Warnkriterien.cs` |
| **Dialog `Form_Quellprofil`** | Betriebsart-Umschalter, Profilauswahl, Bezeichnung/Beschreibung; Monat mit den zwölf Feldern wie bisher, Tag und Stunde als **Tabelle mit CSV-Import** (kein 365-Felder-Formular). Der Import liest **ANSI** über `WaermequelleClass.WerteAusCsv`, mit denselben Trenn- und Dezimalregeln wie der Bestandsimport und BOM-Vorrang | `Views/Simulation/Form_Quellprofil.cs` |
| **Wochengang als Anzeige** | Die Seite „Wochenwerte" erscheint nur noch, wenn die Anlage einen Wochengang trägt, und ist **nicht bearbeitbar**: Ein gespeichertes Quellprofil setzt sie außer Kraft (Konzept 8.1 Punkt 2 — die Tagesvariante ist ihr Nachfolger). Eingabefelder ohne Wirkung wären eine Zusage ohne Wirkung; sie ganz wegzulassen ließe gepflegte Daten stumm verschwinden | `Form_Quellprofil.cs` |
| **Bauart-Bindung (F-Anzeigeregel)** | Bei Luft-Wasser (und bei fehlender Bauart, die Engine rechnet sie ebenso) bekommt der Quellen-Chip **Flächenstil statt Quellrahmen**, `ChipZiel.Keines` (kein Handzeiger) und einen Mouseover, der den Grund nennt. Die Abbruchmeldung in `WaermequelleBearbeiten` bleibt — sie deckt Schema und Tastatur ab | `Form_Simulation_Config.Karten.cs` |
| **Quell-Entnahmehöhe im Dialog** | Programmatisch angehängte Zeile unter der Verdampfer-Rubrik in `Form_QuellePufferspeicher` (Designer-Dateien werden nicht von Hand bearbeitet). Bewusst **außerhalb** der Rubrik: Die Höhe gilt für Wärmepumpe **und** Heizkessel (8.4), die Rubrik ist am Kessel ausgeblendet. Leer = oben; ein Wert außerhalb 0…1 wird abgewiesen statt geklemmt | `Form_QuellePufferspeicher.cs`, `Form_Simulation_Config.Uebersicht.cs` |
| **Schlüsselkopplung der Auswahl** | `SchluesselEintrag` (neue Klasse) trägt Steuerwert und Anzeigetext; die Wärmequellen-ComboBox und die drei Auswahllisten des Quellprofil-Dialogs benutzen ihn. Die Warnung an `WaermequelleClass.TypWerte` („nie einfügen oder umsortieren") hat damit keinen Angriffspunkt mehr | NEU `Views/Simulation/SchluesselEintrag.cs` |
| **Projektkopie** | `Tab_Quellprofil` kommt über die `ID_Projekt`-Regel mit; `Tab_QuellprofilDaten` steht ausdrücklich in `KINDER` (die Auto-Erkennung braucht eine deklarierte Beziehung, und Schritt 54 legt sie bewusst WEICH an); `WQ_ID_Quellprofil` und `ID_Quellprofil` stehen in `FK_MAP`, damit die Kopie auf ihr **eigenes** Profil zeigt | `ProjektDuplizierenCtrl.cs` |
| **Eingabedialog geteilt** | `Form_Simulation_Config.EingabeDialog` war privat; der Quellprofil-Dialog braucht denselben Baustein. Herausgezogen nach `Eingabefrage.Fragen` statt kopiert — die sechs Aufrufstellen im Formular sind unverändert | NEU `Views/Simulation/Eingabefrage.cs` |

**Neue Ressourcenschlüssel: 28**, **1 neu formuliert** (`SIMQ_QUELLPROFIL_INFO`), **8 entfernt**
(die Wochengang-Bedienung, vor dem Entfernen auf Fundstellen geprüft); Bestand danach **2594
`data`-Knoten** je `.resx` (DE/EN deckungsgleich) und **2590 Designer-Eigenschaften**.
Einzelnachweis in [`Lokalisierung_Katalog.md`](Lokalisierung_Katalog.md), Abschnitt
„Nachtrag Paket Q1".

## 3. Die drei Entscheidungen, die der Auftrag offengelassen hat

### 3.1 Das 8760er-Profil kommt in die Datenbank

Konzept 9 verlangt, die Ganglinien-Ablage gegen die 2-GB-Grenze von Access zu bemessen, und
nennt `Tab_StromganglinieDaten` als Vergleichsmaßstab. Gemessen am 28.08.2026 auf einer Kopie
der produktiven Datenbank:

| Befund | Zahl |
|---|---|
| Datenbank gesamt | 151 949 312 Bytes = **7,4 %** der 2-GB-Grenze |
| `Tab_StromganglinieDaten` im Bestand | **718 321** Zeilen (23 Ganglinien, teils viertelstündlich) |
| Zehn Stundenprofile nachgelegt (87 600 Zeilen) | Zuwachs der Dateigröße **0 Bytes** |
| Nutzlast eines Stundenprofils | 8 760 × 20 Bytes ≈ 175 KiB, mit beiden Indizes grob das Doppelte |
| Einfügedauer 8 760 Zeilen (eine Transaktion) | **1,1 s** |

Das Muster trägt die Menge nachweislich — es tut es im Bestand bereits achtmal so oft, wie ein
Stundenprofil verlangt. Die Grenze liegt bei Tausenden von Profilen, nicht bei Dutzenden.
**Entscheidung: `Stunde` ist eine vollwertige dritte Betriebsart**, Engine und Dialog bedienen
sie. Damit ist der eigentliche Mangel aus Konzept 8.1 Punkt 3 behoben: `WQ_CSV` speichert nur
einen Dateipfad, und eine Projektweitergabe verliert die Quelle still.

`WQ_CSV` selbst bleibt **funktionierender Lese-Altbestand**: Der Zweig `TYP_CSV` ist Zeichen für
Zeichen unverändert, und Schritt 54 übernimmt nichts automatisch. Eine automatische Übernahme
wäre eine stille Datenänderung an Bestandsprojekten — und bei `WQ_CSV` nicht einmal
durchführbar: Dort steht ein Dateipfad, dessen Datei zur Migrationszeit nicht vorliegen muss.

### 3.2 Die Anschlusshöhe ist ein Parameter, kein Feld am Speicher

Ticket B1-O1 erwartete, dass Q1 die Höhe in `QuellEntnahmeTemperatur` einsetzt und „die Aufrufer
in WP und Kessel unverändert bleiben". Das trägt nicht: **`WQ_Anschlusshoehe` steht an der
ANLAGE, nicht am Behälter.** Zwei Erzeuger dürfen denselben geteilten Puffer als Quelle führen
und ihn auf unterschiedlicher Höhe anzapfen; ein Feld am Speicher könnte nur eine der beiden
Höhen halten und die zweite still überschreiben. Aus der Eigenschaft ist deshalb eine Methode
`QuellEntnahmeTemperatur(double anschlusshoehe)` geworden; die drei Aufrufstellen reichen die je
Modul **einmal** gelesene Höhe durch. Die Skala und die Auslegung von Werten außerhalb sind
dieselben wie bei den Entnahme- und Einspeisehöhen aus P1 (`SchichtIndex`).

### 3.3 Der Wochengang wird Anzeige, nicht Eingabe

Konzept 8.1 Punkt 2 lässt den additiven Wochengang „als Option der Monatsvariante (Bestandsdaten)"
bestehen. Im neuen Datenmodell hat er keinen Platz — eine Betriebsart ist eine Wertreihe, und
12 + 168 in einer Tabelle wäre ein Zwitter. Umgesetzt ist deshalb: Das **Profilmodell** kennt
ihn nicht, die **Engine liest ihn weiter** (Stufe 2, solange keine Profil-ID gesetzt ist), und
der **Dialog zeigt ihn**, wenn die Anlage einen trägt — mit dem Hinweis, dass ein gespeichertes
Quellprofil ihn außer Kraft setzt und die Werte in den Altspalten erhalten bleiben. Damit
verschwindet nichts still, und nichts verspricht eine Wirkung, die es nicht hat. Ein NEUER
Wochengang ist nicht mehr anlegbar; sein Nachfolger ist die kalenderunabhängige Tagesvariante.

## 4. Verifikation

### 4.1 Build und Referenzmenge

`WP-Plan.sln` und `Referenzlauf.csproj`, Debug × x64: **0 Fehler**; die verbliebenen fünf
Warnungen (CS0108/CS0109/CS1998) sind Bestand und liegen außerhalb dieses Pakets. Zwei
CS1690-Warnungen, die der erste Bauversuch am `double?`-Feld des Dialogs erzeugt hatte, sind
durch lokale Zwischenvariablen wieder verschwunden — die Warnungsmenge ist unverändert.

Referenzlauf über die feste Dreizehnermenge (1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024,
1030, 1039, 1040, 1041, 1042) gegen die Basis **`2026-08-28_P1`**:

```
Projekt_1007 … Projekt_1042 : 13 × PASS   (3 532 029 Werte innerhalb der Toleranz)
MD5-Vergleich                : 329 von 329 CSV byte-gleich, 0 Abweichungen,
                               0 fehlende, 0 zusätzliche Dateien
pruefen                      : GESAMT plausibel (keine NaN/Inf)
Laufprotokoll                : 45 verschiedene Meldungen vorher, 45 nachher,
                               Mengenvergleich leer — keine neue, keine entfallene
```

Das ist die zugesagte Messlatte, und sie ist konstruktiv erklärbar: **Kein Projekt der
Referenzmenge führt ein Quellprofil, keine Anlage eine gepflegte Anschlusshöhe, kein Puffer mehr
als eine Schicht.** In der produktiven Datenbank steht `WQ_Typ` 125-mal auf NULL, 5-mal leer,
einmal auf `Erdreich` und einmal auf `Pufferspeicher`; `WQ_Monatswerte`, `WQ_Wochenwerte` und
`WQ_CSV` sind in **allen** 131 Anlagenzeilen leer. Der Kalenderfix aus K1-O6 kann den Bestand
damit gar nicht erreichen, und selbst wenn er es könnte, wäre er für 2026 wertgleich.

*Anmerkung zum Werkzeug:* Zwei Zwischenläufe meldeten „12 von 13" — jeweils ein **anderes**
Projekt (1041, dann 1021) lief in den 300-s-Timeout des Kindprozesses. Beide rechnen einzeln in
1,2 bis 1,4 s durch und liefern byte-gleiche Dateien; die Läufe fielen mit den parallel laufenden
Rebuilds zusammen. Das oben protokollierte Ergebnis stammt aus dem projektweisen Lauf
(`Referenzlauf.exe projekt …`) auf dem endgültigen Bauzustand, alle dreizehn mit Rückgabewert 0.

Der Probeordner ist nach dem Vergleich **gelöscht**. **Die Basis `2026-08-28_P1` bleibt
unverändert gültig** (330 Dateien, jüngster Zeitstempel unverändert 27.08.2026 22:58:41).

### 4.2 Schritt 54 auf einer Wegwerf-Kopie

`Referenzlauf.exe migration` auf einer Kopie der produktiven Datenbank (Schemastand 53):

```
Schemastand vorher: 53   (Zielstand 54)
  - Tabelle Tab_Quellprofil: angelegt
  - Tabelle Tab_QuellprofilDaten: angelegt
  - Index idx_Quellprofil: angelegt
  - Index idx_QuellprofilDaten: angelegt
  - Beziehung FK_QuellprofilDaten_Kopf (mit Loeschweitergabe): angelegt
  - Tab_Energieanlagen: 2 Spalten angelegt, 0 bereits vorhanden
  - Beziehung FK_Anlage_Quellprofil (restriktiv): angelegt
Schemastand nachher: 54
```

**Idempotenz** (Marker-Rücksetz-Probe auf 53, zweiter Lauf): „0 Spalten angelegt", alle vier
Objekte und beide Beziehungen „bereits vorhanden", Schemastand wieder 54. `Tab_Energieanlagen`
wächst von 65 auf 67 Spalten (Access-Grenze 255).

Die beiden Beziehungen sind gegen ihre Zusage geprüft:

| Probe | Ergebnis |
|---|---|
| Profil löschen, an dem noch eine Anlage hängt | **abgewiesen** — „Der Datensatz kann nicht gelöscht oder geändert werden, da die Tabelle 'Tab_Energieanlagen' in Beziehung stehende Datensätze enthält" |
| unbenutztes Profil mit 365 Wertzeilen löschen | 1 Kopfzeile gelöscht, danach **0** Wertzeilen (Löschweitergabe greift) |
| `[Index]` als reserviertes Wort | CREATE, INSERT, `SELECT … ORDER BY [Index]`, DELETE — alles fehlerfrei gegen ACE 12.0 |

### 4.3 Wirkprobe Tagesprofil (Projekt 1042, Anlage 14807, Sole-Wasser)

Profil mit 365 Werten `T(i) = 5,0 + i · 0,05`, über `WQ_ID_Quellprofil` an die Anlage gekoppelt;
gelesen über den echten Engine-Weg `WaermequelleClass.Quelltemperatur` mit einem
Außentemperaturvektor aus lauter −99 °C, damit ein Rückfall unverwechselbar wäre.

| Größe | vorher (ohne Profil) | nachher (Tagesprofil) |
|---|---|---|
| Rückfall auf Außentemperatur | **ja** (durchweg −99) | **nein** |
| Wertebereich | — | 5,00 … 23,20 °C, **365 verschiedene Werte** |
| Stunde 0 / 23 (Tag 0) | −99 / −99 | **5,00 / 5,00** |
| Stunde 24 / 47 (Tag 1) | −99 / −99 | **5,05 / 5,05** |
| Stunde 1000 (Tag 41) | −99 | **7,05** = 5 + 41 · 0,05 |
| Stunde 4380 (Tag 182) | −99 | **14,10** = 5 + 182 · 0,05 |
| Stunde 8759 (Tag 364) | −99 | **23,20** = 5 + 364 · 0,05 |
| erste Stunde, die vom Tageswert abweicht | — | **keine** (alle 365 Tage über 24 h konstant) |

Die letzte Zeile ist der Kern: Tag *i* gilt für **genau** die Stunden 24·*i* … 24·*i*+23, über
das ganze Jahr, ohne jede Verschiebung. Das ist Kalenderunabhängigkeit als Messwert, nicht als
Zusage.

### 4.4 Wirkprobe Monatsprofil und Stundenprofil (dieselbe Anlage)

| Betriebsart | Werte | Bereich | Stichstunden |
|---|---|---|---|
| **Monat** (12 Werte `1…12`) | 12 verschiedene | 1 … 12 | h 0 = 1; h 1000 (11. Februar) = **2**; h 4380 (2. Juli) = **7**; h 8759 = 12 |
| **Stunde** (8760 Werte `i·0,001`) | 8760 verschiedene | 0 … 8,759 | h 0 = 0; h 23 = 0,023; h 24 = 0,024; h 8759 = 8,759 |

Monatsanfänge des Monatsprofils: 1 2 3 4 5 6 7 8 9 10 11 12 — die Kachelung trifft die
Monatsgrenzen des Nicht-Schaltjahres auf die Stunde.

Zusätzlich ohne Datenbank geprüft: eine Wertreihe, deren Länge **nicht** zur Betriebsart passt,
ergibt `null` und kein mit Nullen aufgefülltes Jahr.

### 4.5 Robustheit der Lesekette

| Fall | Verhalten |
|---|---|
| Profil vorhanden, aber **5 Wertzeilen fehlen** (360 statt 365) | Protokollwarnung *„Quellprofil 1 der Anlage 14807 ist nicht lesbar oder unvollständig (Zahl der Werte passt nicht zur Betriebsart) - es gilt der Altweg bzw. die Außentemperatur."*, danach Rückfall — kein halbes Jahr |
| **keine** Profil-ID, aber `WQ_Monatswerte` gepflegt | Altweg greift: 7 verschiedene Werte, Monatsanfänge 3 3 4 5 6 7 8 9 8 6 4 3 — genau die eingetragene Reihe |
| Datenbank **vor** Schritt 54 | `QuellprofilCtrl` liefert leer/`null`, die Engine nimmt den Altweg; kein Aufruf wirft |

### 4.6 Wirkprobe Quell-Entnahmehöhe (B1-Muster, Projekt 1042)

Konstellation wie in B1 §4.2: Der Kombi-Speicher **1054197** wird von Heizkessel 14785 und der
Luft-Wasser-WP 14806 geladen; die Sole-Wasser-WP **14807** bekommt ihn als Wärmequelle
(`WQ_Typ='Pufferspeicher'`, `WQ_ID_Puffer=1054197`) — ein **geteilter** Puffer. Neu: der Speicher
rechnet mit **N = 5 Schichten**.

**Gegenprobe zum B1-Stand.** Mit gepflegtem Paar 60/40 und Ladeleistung 3 kW liefert Q1 bei
`WQ_Anschlusshoehe = NULL` exakt die B1-Zahlen: Produktion **7 375,5818 kWh**, Strom
**1 596,4463 kWh**, JAZ **4,6200** (B1 §4.2, Runde A2: 7 375,582 / 1 596,446 / 4,6200). Die
Höhe NULL ist damit belegt wertgleich mit „fest oben".

**Der Höhennachweis** braucht ein Temperaturband innerhalb der Kennlinie und einen Speicher, der
nicht ständig voll steht — deshalb Puffer ohne Temperaturpaar (Band 0…10 °C, dieselbe Bauart wie
B1-Runde A1) und Ladeleistung 1,0 kW:

| `WQ_Anschlusshoehe` | Quelltemperatur | verschiedene Werte | Mittel | Strom | JAZ |
|---|---|---|---|---|---|
| **NULL** (= oben, B1) | 4,4667 … 10,00 °C | 9 | 9,9559 | 2 335,0788 kWh | **3,1529** |
| **0,5** | 0,00 … 10,00 °C | 20 | 9,9160 | 2 338,0543 kWh | 3,1490 |
| **0,3** | 0,00 … 10,00 °C | 26 | 9,9123 | 2 338,1481 kWh | 3,1489 |
| **0,0** (ganz unten) | 0,00 … 7,50 °C | **275** | 7,2123 | **2 514,8852 kWh** | **2,9276** |

Ablesbar:

- **Dieselbe Stunde, andere Temperatur.** In Stunde 49 liest die Anlage bei Höhe „oben"
  4,4667 °C, bei Höhe 0,3 dagegen 0,00 °C. Die Höhe wirkt, und sie wirkt an der Stelle, an der
  Konzept 8.2 sie verlangt.
- **Je tiefer, desto kälter und desto bewegter.** Die Zahl verschiedener Werte wächst monoton
  von 9 (oben) über 20 und 26 auf 275 (unten) — genau die Physik: Der obere Anschluss sieht fast
  immer die volle Schicht, der untere folgt jedem Ladezustand.
- **Ergebniswirkung.** Ganz unten steigt der Strombedarf um **179,8 kWh (+7,7 %)** und die
  Modul-JAZ fällt von 3,1529 auf 2,9276.
- **Das Laufprotokoll sagt es.** Die Booster-Zeile trägt den Zusatz „…, 5 Schicht(en), Entnahme
  auf Höhe 0.3 (0 = unten, 1 = oben))". Er erscheint **nur** bei N > 1 und einer Höhe unterhalb
  von oben — sonst bliebe die Meldungsmenge des Bestands nicht unverändert (4.1).

**Direkt an der Schichtebene** nachgerechnet (Puffer 40…50 °C, Füllgrad 50 %):

| N | Schichttemperaturen | Höhe 1,0 | Höhe 0,5 | Höhe 0,3 | Höhe 0,0 |
|---|---|---|---|---|---|
| **5** | 50 / 50 / 45 / 40 / 40 °C | **50** | **45** | **40** | **40** |
| **1** | 45 °C | 45 | 45 | 45 | 45 |

Die N=1-Zeile ist der Grund, warum Q1 auf jedem Bestandsprojekt konstruktiv wirkungslos ist:
Ein Vorrat hat nur eine Zone, und `SchichtTemperatur(0)` ist dort die Ein-Zonen-Ersatztemperatur
`RL_eff + A/Q_max · (VL_eff − RL_eff)` = 40 + 0,5 · 10 = 45 °C — unabhängig von der Höhe.

### 4.7 Projektkopie

Projekt 1042 mit drei Quellprofilen (Tag/Monat/Stunde) und einer Anlage, die auf das
Monatsprofil zeigt, über `ProjektDuplizierenCtrl.Duplizieren` kopiert:

| Befund | Ergebnis |
|---|---|
| Profile im Zielprojekt 1043 | **3** (neue IDs 4, 5, 6) |
| Wertzeilen je Kopie | **365 / 12 / 8760**, `[Index]` lückenlos 0…364, 0…11, 0…8759 |
| Anlage der Kopie (14835) | zeigt auf Profil **5** — das der KOPIE, nicht auf das Quellprofil 2 des Ausgangsprojekts |
| `WQ_Anschlusshoehe` | 0,3 mitgekommen |

Ohne die drei Einträge in `KINDER`/`FK_MAP` wäre die Variante mit leerem Profil oder — schlimmer
— mit einem Zeiger auf das Profil des Quellprojekts gefahren; eine Änderung dort hätte
stillschweigend in beiden Projekten durchgeschlagen.

### 4.8 Dialog

Ohne `ShowDialog`, über die öffentliche Schnittstelle und Reflection auf die Steuerelemente:

| Probe | Ergebnis |
|---|---|
| Betriebsart **Monat** | Reiter „Monatswerte \| Grafik" |
| Betriebsart **Tag** | Reiter „Tageswerte \| Grafik", Raster **365** Zeilen, Kennzahlen „365 Werte / von 10,0 bis 10,0 °C / Mittel 10,0 °C" |
| Betriebsart **Stunde** | Reiter „Stundenwerte \| Grafik", Raster **8760** Zeilen |
| gespeichertes Profil laden (Stunde, ID 3) | Auswahl steht auf „Abwaerme Stundenwerte", 8760 Werte 0,0 … 10,0 °C |
| Anlage **ohne** Wochengang | Reiter „Monatswerte \| Grafik" — die Altweg-Seite bleibt weg |
| Anlage **mit** Wochengang | Reiter „Monatswerte \| **Wochenwerte** \| Grafik" |
| Fenstergeometrie | ClientSize 700 × 612 (vorher 700 × 540; +72 px für die vier Kopfzeilen) |

### 4.9 Bauart-Bindung (Codebeleg)

Eine UI-Automatisierung wäre hier unverhältnismäßig; der Beleg ist die eine Verzweigung in
`Form_Simulation_Config.Karten.QuellenChip`:

```csharp
bool bauartGebunden = string.IsNullOrEmpty(info.WpTyp) ||
                      info.WpTyp == DbWerte.WP_BAUART_LUFT_WASSER;
…
Stil    = bauartGebunden ? ErzeugerKarte.ChipStil.Flaeche : ErzeugerKarte.ChipStil.Quelle,
Hinweis = bauartGebunden ? string.Format(…SIMQ_TIP_QUELLE_BAUART, …) : …SIMQ_TIP_QUELLE,
Ziel    = bauartGebunden ? ErzeugerKarte.ChipZiel.Keines  : ErzeugerKarte.ChipZiel.Quelle
```

`ChipZiel.Keines` ist in `ErzeugerKarte.Anzeigen` genau der Fall **ohne** `Cursors.Hand` und
**ohne** `ChipBearbeiten` — der Chip ist damit nicht mehr als Auswahl gezeichnet und öffnet die
Quellenwahl nicht. Sole-/Wasser-Wasser-Anlagen behalten Quellstil, Handzeiger und Ziel; ihre
`QUELLE_FEHLT`-Warnfassung aus Paket A1 ist unberührt (Referenzlauf 4.1: dieselbe Meldungsmenge).
Die Abbruchmeldung `SIMQ_MSG_LUFT_WASSER` in `WaermequelleBearbeiten` steht unverändert als
zweite Sicherung.

### 4.10 Umgang mit den Daten

Alle Wirkproben liefen auf **Wegwerf-Kopien außerhalb des Repos**
(`Referenzlauf.exe migration`, Schemastand 54); nach Abschluss wurden sie samt der
Probeordner gelöscht.

**Die produktive `Kenndaten.accdb` wurde nicht beschrieben** — Zeitstempel **27.08.2026
23:34:25.224** und Größe **151 949 312 Bytes** vor dem ersten und nach dem letzten Schritt
identisch, keine `Kenndaten.laccdb`, kein Access- oder Anwendungsprozess während der Arbeit.

## 5. Was NICHT geändert wurde

- **Die Rechnung des Bestands.** Kein Bestandsprojekt hat ein Quellprofil, eine Anschlusshöhe
  oder mehr als eine Schicht; jede neue Codestelle ist an genau diese Merkmale gebunden. Der
  Byte-Vergleich weist es nach.
- **`WQ_CSV` und der Zweig `TYP_CSV`.** Unverändert, einschließlich `ProfilAusCsv` mit seinem
  UTF-8-Leseweg. Der neue ANSI-Leser `WerteAusCsv` steht daneben und wird nur vom
  Quellprofil-Import benutzt.
- **Die Booster-Mechanik aus B1.** Lesezeitpunkt, Kopplungsentscheidung, F13-Kappung, Badge und
  die Mengenrechnung sind unangetastet; Q1 tauscht allein die HÖHE, an der die Temperatur
  abgegriffen wird.
- **Die Altspalten.** `WQ_Monatswerte`, `WQ_Wochenwerte`, `WQ_CSV`, `WQ_Puffer` bleiben stehen
  und lesbar (Konzept 15). Der Dialog schreibt die ersten beiden nicht mehr — sie stehenzulassen
  ist der Rückweg: Wer das Quellprofil wieder entfernt, rechnet wie zuvor.
- **Der Aufräumauftrag 8.3** (B1-O6): unverändert offen, kein Restposten wird durch Q1 tot.

## 6. Offene Punkte / Notizen

| # | Punkt | Ziel |
|---|---|---|
| Q1-O1 | **Der Quellprofil-Fall ist in der Referenzmenge nicht abgedeckt** — dieselbe Linie wie B1-O8. Kein Projekt führt ein Profil, keine Anlage eine Anschlusshöhe. Solange das so bleibt, sichert kein Referenzlauf die Q1-Rechnung ab, nur die Wirkproben dieses Protokolls. Empfehlung: 1042 in der produktiven Datenbank konfigurieren (WP 14807 → Puffer 1054197 mit Temperaturpaar, N > 1 und einer Anschlusshöhe; ein Tagesprofil an einer zweiten Anlage) und die Basis danach neu setzen | Anwender / Orchestrator |
| Q1-O2 | Das **Referenzlauf-Werkzeug** exportiert weder `QUELLTEMP`- noch Profil-Ganglinien (B1-O3, P1-O1, E1-O3): Es ist das Messinstrument, geänderte Dateinamen machten jeden Vergleich mit älteren Ständen unmöglich. Aufnahme nur mit einem Basiswechsel | Basiswechsel |
| Q1-O3 | **`WQ_CSV` ist noch nicht stillgelegt.** Der Weg in die Datenbank steht (Betriebsart `Stunde`), die einmalige Übernahme „beim ersten Öffnen" aus Konzept 8.1 Punkt 3 ist bewusst NICHT gebaut — sie wäre eine Datenänderung an Bestandsprojekten ohne Anwenderentscheidung. Vorschlag für Paket L: Beim Öffnen einer Anlage mit `WQ_Typ='CSV'` ein Angebot „Datei jetzt als Quellprofil übernehmen?" statt einer Automatik. Im produktiven Bestand ist derzeit **keine** Anlage betroffen (0 von 131) | Paket L |
| Q1-O4 | Der **additive Wochengang** ist nicht mehr anlegbar (3.3). Falls der Produktverantwortliche ihn weiter als Eingabe will, wäre die saubere Form eine vierte Betriebsart mit 168 Werten — das Datenmodell trägt sie ohne Schemaänderung, `DbWerte.QuellprofilWerteanzahl` und der Dialog bräuchten je einen Zweig | Rückfrage |
| Q1-O5 | `Tab_Quellprofil.Einheit` wird geschrieben, aber nicht ausgewertet — die Engine rechnet in °C. Sie dokumentiert, sie steuert nicht; das ist so gewollt und hier nur festgehalten, damit es niemand später für einen Fehler hält | dokumentiert, bleibt |
| Q1-O6 | Der **Spaltenname `Index`** ist ein reserviertes Wort in Access-SQL. Im Programm ist jede Nennung geklammert und geprüft; eine Ad-hoc-Abfrage in der Access-Oberfläche ohne Klammern scheitert aber. Der Name folgt dem Konzept-Auftrag; die Falle steht im Katalogkommentar | dokumentiert |
| Q1-O7 | Der **Kalenderwert 3** des Altwegs (`WOCHENTAG_JAN1_ALTWEG`) ist heute ergebnisgleich zur Altfassung und für den produktiven Bestand ohnehin folgenlos (kein gepflegter Wochengang). Sollte je ein Bestandsprojekt mit Wochengang auftauchen, ist zu entscheiden, ob es auf den Klimadaten-Kalender (F3) wandern soll — das wäre dann eine bewusste Ergebnisänderung | dokumentiert |
| Q1-O8 | Das **Werteraster** hält beim Wechsel Stunde → Monat seine 8760 Zeilen (unsichtbar, weil der Reiter dann fehlt) und füllt beim Wechsel Tag → Stunde den Überhang mit der Vorgabe 10 °C. Beides ist dokumentiertes Verhalten, aber ein Hinweis beim Betriebsartwechsel wäre freundlicher | kosmetisch |
| Q1-O9 | Die Protokollzeile der Anschlusshöhe (`SimulationControl.AnschlusshoeheText`) ist inline deutsch wie ihr Nachbarbestand (P1-O7 / S2-O9 / B1-O9) | Paket L |
| Q1-O10 | `P1-O2` (**`Z_AnlageSenke.Anschlusshoehe` ist überall NULL, die Dialogpflege je Senke fehlt**) war für P2/Q1 vorgemerkt und bleibt offen: Q1 pflegt die QUELL-Entnahmehöhe an der Anlage, nicht die EINSPEISEhöhe je Senke. Der Pfad dahin ist der Senkendialog, nicht der Quellendialog | P2 |

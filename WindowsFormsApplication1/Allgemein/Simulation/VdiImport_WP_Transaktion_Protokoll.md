# Aufräumklammer für den VDI-3805-Import der Wärmepumpen

Stand 18.08.2026. Nicht committet.

Bestandsbefund aus dem Protokoll `VdiImport_FilterMehrfach_Protokoll.md` (Abschnitt 4,
letzter Punkt): scheitert die Kennlinienübernahme mitten in der Schleife, bleibt der bereits
geschriebene Satz in `Tab_WP_STAMM` ohne vollständige Kennlinien stehen. Der Eintrag zählt
beim Mehrfachladen als „fehlgeschlagen" — der halbe Datensatz bleibt aber in der Datenbank
und taucht anschließend in allen Auswahllisten als scheinbar gültige Wärmepumpe auf.

Betroffen ist nur der Wärmepumpen-Dialog. Die drei Geschwister-Dialoge schreiben je Eintrag
in **eine** Tabelle (Solarkollektoren, Pufferspeicher) bzw. bringen ihr Insert bereits in
einer eigenen Transaktion unter (Heizkessel).

---

## 1. Befund

`Views\Wärmepumpe\Form_WP_einlesen.cs`, Methode `UebernehmeEintrag(int index)` (ab Zeile 201).

Ablauf im Bestand:

1. Duplikatprüfung über `RecordSet` auf `Tab_WP_STAMM` — ein bereits vorhandener Bezeichner
   führt zum sofortigen Rücksprung mit `VdiUebernahmeErgebnis.Duplikat`.
2. Felder des Stammsatzes aus `ctrl._list[index]` besetzen.
3. `wpctrl.Insert()` (Zeile 237) schreibt den Stammsatz. Die ID kommt aus dem Access-AutoWert
   und wird über `SELECT @@IDENTITY` in `wpctrl.ID` zurückgelesen.
4. Kennlinienschleife über `ctrl._list[index].x`: je Zeile `InsertKenndatenStamm` (Heizen)
   bzw. `InsertKenndatenKuehlungStamm` (Kühlen).

Ab Schritt 3 existiert der Stammsatz. In Schritt 4 gibt es zwei Abbruchwege, die ihn stehen
lassen:

| Abbruchweg | Auslöser | Bestandsverhalten |
|---|---|---|
| geordnet | `InsertKenndatenStamm` / `InsertKenndatenKuehlungStamm` liefert `false` (DB-Fehler) | `return VdiUebernahmeErgebnis.Fehler;` mitten in der Schleife |
| ungeordnet | zu kurze VDI-Zeile: `token[5]` bzw. `token[7]` wirft `IndexOutOfRangeException` | Ausnahme verlässt die Methode; der Mehrfachpfad in `btn_Uebernehmen_Click` fängt sie und zählt „Fehler" |

In beiden Fällen bleibt der Stammsatz samt der bis dahin geschriebenen Kennlinienzeilen
zurück. Eine Transaktionsklammer über Schritt 3 und 4 gibt es nicht.

## 2. Umsetzung

### 2.1 Warum Aufräumklammer und nicht Transaktion

`Controller\WPStammCtrl.cs`:

* `Insert()` (Zeile 137) öffnet eine **eigene** `OleDbConnection`, schreibt den Stammsatz in
  einer eigenen `OleDbTransaction` und **committet sie sofort**, bevor `@@IDENTITY` gelesen
  wird. Die Verbindung wird danach geschlossen.
* `InsertKenndatenStamm` (Zeile 184) und `InsertKenndatenKuehlungStamm` (Zeile 195) laufen
  über `DataRepository.ExecuteScalar` (`SELECT Max(ID)`) und `DataRepository.ExecuteSQL`.
  **Jeder dieser Aufrufe öffnet und schließt seine eigene Verbindung.**

Eine gemeinsame Transaktion über Stammsatz und alle Kennlinienzeilen setzt deshalb voraus,
dass Verbindung und `OleDbTransaction` durch den Controller durchgereicht werden — ein
Umbau an einer Klasse, die von `Form_WP`, `Wizard_WPItem`, `Form_WPAuswahl` und `WPDataCtrl`
mitbenutzt wird. Das war für diesen Befund nicht gewollt; die Änderung sollte chirurgisch
und auf das Formular begrenzt bleiben.

Stattdessen wird der bereits geschriebene Stammsatz beim Scheitern wieder entfernt. Dafür
gibt es bereits genau das passende Werkzeug — `WPStammCtrl.Delete()` (Zeile 114):

* löscht per **Bezeichner**, nicht per ID; die ID der Kennlinientabellen wird intern über
  `DataRepository.GetIdByName` aufgelöst,
* räumt `Tab_Kenndaten_STAMM` **und** `Tab_Kenndaten_Kuehlung_STAMM` mit ab,
* arbeitet parameterisiert,
* fängt eigene Ausnahmen ab (`return false`) und **wirft nie** — damit im `finally` einer
  laufenden Ausnahme unbedenklich aufrufbar,
* die ReadOnly-Sperre am Methodenanfang greift nicht: `Insert()` schreibt `ReadOnly = false`.

Der Bezeichner ist zum Zeitpunkt des Aufräumens eindeutig, weil die Duplikatprüfung am
Anfang von `UebernehmeEintrag` sonst schon abgebrochen hätte. `Delete()` per Bezeichner
trifft also genau den gerade angelegten Satz.

### 2.2 Die Änderung

Nur `Views\Wärmepumpe\Form_WP_einlesen.cs`, nur `UebernehmeEintrag`. Der Erfolgsweg bleibt
anweisungsgleich; der Abschnitt zwischen `wpctrl.Insert()` und
`return VdiUebernahmeErgebnis.Gespeichert;` wird umschlossen:

```csharp
            if(!wpctrl.Insert()) return VdiUebernahmeErgebnis.Fehler;

            // Ab hier existiert der Stammsatz. Die Kennlinien-Inserts laufen im
            // Controller ueber getrennte Verbindungen, eine gemeinsame Transaktion
            // ist ohne Controller-Umbau nicht moeglich. Deshalb Aufraeumklammer:
            // scheitert die Kenndaten-Uebernahme (false oder Exception), wird der
            // gerade angelegte Stammsatz samt Kennlinien wieder geloescht, damit
            // kein unvollstaendiger Datensatz stehen bleibt (Befund 17.08.2026).
            bool bVollstaendig = false;
            try
            {
                ... bisheriger Rumpf, wortgleich, eine Ebene weiter eingerueckt ...

                bVollstaendig = true;
                return VdiUebernahmeErgebnis.Gespeichert;
            }
            finally
            {
                // Delete() loescht per Bezeichner (die Duplikatpruefung oben stellt
                // Eindeutigkeit sicher), faengt eigene Fehler und wirft nie.
                if (!bVollstaendig && !wpctrl.Delete())
                    Console.WriteLine("Unvollstaendiger WP-Stammsatz '" + wpctrl.WPName + "' (ID " + wpctrl.ID + ") konnte nicht aufgeraeumt werden!");
            }
```

Beide `return VdiUebernahmeErgebnis.Fehler;` **innerhalb** der Schleife bleiben unverändert
stehen: sie verlassen den `try`-Block, ohne `bVollstaendig` zu setzen, das `finally` räumt
also auch auf diesem Weg auf. Ausnahmen laufen nach dem Aufräumen unverändert weiter nach
oben und werden im Mehrfachpfad wie bisher gefangen und als „Fehler" gezählt.

Nicht angefasst: `WPStammCtrl` und alle anderen Controller, Designer und `.resx`, die drei
anderen Einlese-Dialoge. Kein neues `using`, kein `RecordSet` im neuen Code.

### 2.3 Diff-Umfang

| Datei | Stellen | Umfang |
|---|---|---|
| `Views\Wärmepumpe\Form_WP_einlesen.cs` | 239–247 (Kommentar, `bVollstaendig`, `try {`), 248–300 (Bestandsrumpf, wortgleich, +4 Leerzeichen), 302–311 (`bVollstaendig = true;`, `return`, `finally`-Block) | 299 → 317 Zeilen (+18) |

Zeilen 1–237 und der Methodenabschluss ab dem alten `return` sind byteweise unverändert;
die 53 Rumpfzeilen wurden maschinell um vier Leerzeichen eingerückt und danach zeichenweise
gegen den Vorher-Stand verglichen (0 Abweichungen).

## 3. Verifikation

### 3.1 Build

Full-MSBuild von VS 2022 Community (17.14.40), `dotnet build` ist wegen der COM-Referenzen
des App-Projekts (MSB4803) vorbestehend nicht verwendbar.

```
MSBuild WindowsFormsApplication1.csproj -p:Configuration=Debug -p:Platform=x86 -p:ArtifactsPath=%TEMP%\wpb -v:m
```

| Lauf | Zeit | Ergebnis |
|---|---|---|
| Baseline (vor dem Edit) | 18.08.2026 10:06:32–10:06:38 | 0 Fehler, 6 Warnungen |
| Prüfbuild (nach dem Edit) | 18.08.2026 10:14:27–10:14:32 | 0 Fehler, 6 Warnungen |

Die sechs Warnungen sind die bekannten Bestandswarnungen (`StromverbraucherStammCtrl.items`,
`WErzeugerModel.ID_Projekt`, `KlimaregionStammCtrl.rows`/`.items`, `MDIMainForm` CS4014 und
CS1998) und in beiden Läufen identisch. Störungen durch parallel bearbeitete Dateien
(`SimulationControl.cs`, `SchemaMigration.cs`, …) sind in keinem der beiden Läufe
aufgetreten.

### 3.2 Headless-Smoke A/B

Wegwerf-Harness `%TEMP%\wpk12` (`VdiImportHarness.csproj` mit ProjectReference auf das
App-Projekt, x86, `net8.0-windows`; Muster wie `%TEMP%\wpk8`). Es gibt weiterhin keine
VDI-Beispieldatei im Repo, deshalb wird `ctrl._list` per Reflection besetzt — der Weg ab der
Liste ist derselbe wie nach einem echten Dateiimport. MessageBoxen klickt der
`DialogWaechter` weg und protokolliert Titel und Text. Geschrieben wird ausschließlich in die
Arbeitskopie `%TEMP%\wpk12\db\Kenndaten.accdb`; der Lauf bricht ab, wenn
`DataRepository.GetDBPath()` nicht darauf zeigt.

**Testdaten** (zwei Einträge, beide markiert, `btn_Uebernehmen.PerformClick()` — also der
Mehrfachpfad):

| Eintrag | Position | `x`-Datenzeilen | erwartetes Verhalten |
|---|---|---|---|
| `HRN Klammer Gift 8` | 0 | `710.09;x;1;35` · `710.91;a;35;5,5;b;4,2` · `710.91;x;y` | die dritte Zeile ist zu kurz → `token[5]` wirft `IndexOutOfRangeException`, und zwar **nach** einer bereits erfolgreich geschriebenen Kenndatenzeile |
| `HRN Klammer Gut 6` | 1 | `710.09;x;1;35` · `710.91;a;35;5,5;b;4,2` · `710.91;a;35;7,1;b;3,9` | vollständig, zwei Kenndatenzeilen |

Der Giftsatz steht bewusst **vorn**: er verbraucht damit den AutoWert vor dem Gutsatz, was
den Aufräumnachweis liefert (siehe unten).

Beide Läufe starten auf einer **frischen** Kopie der Produktiv-DB (`Tab_WP_STAMM` 51,
`Tab_Kenndaten_STAMM` 1960, `Tab_Kenndaten_Kuehlung_STAMM` 174, 0 verwaiste
Kennlinienzeilen). Lauf A um 10:12:07 mit dem Stand **vor** dem Edit, Lauf B um 10:14:47 mit
dem Stand **nach** dem Edit; die A-Kopie liegt als `%TEMP%\wpk12\db\Kenndaten_A.accdb`,
die B-Kopie als `Kenndaten_B.accdb` daneben.

| Messung | A — vor dem Edit | B — nach dem Edit |
|---|---|---|
| Meldung (Dialogwächter) | „1 von 2 Einträgen geladen." + „Fehlgeschlagen: 1" | **identisch** |
| Konsolenzeile des Mehrfachpfads | `Fehler beim Einlesen von 'HRN Klammer Gift 8': Index was outside the bounds of the array.` | identisch |
| `Tab_WP_STAMM` | 51 → 53 (**+2**) | 51 → 52 (**+1**) |
| `Tab_Kenndaten_STAMM` | 1960 → 1963 (**+3**) | 1960 → 1962 (**+2**) |
| `Tab_Kenndaten_Kuehlung_STAMM` | 174 → 174 (±0) | 174 → 174 (±0) |
| Stammsatz `HRN Klammer Gift 8` | **1 Zeile** (ID 73) — der halbe Satz | **0 Zeilen** |
| Kennlinien des Giftsatzes | **1 Zeile** (ID 102979) | **0 Zeilen** |
| Stammsatz `HRN Klammer Gut 6` | 1 Zeile (ID 74) | 1 Zeile (ID 74) |
| Kennlinien des Gutsatzes | 2 Zeilen | 2 Zeilen |
| verwaiste Kennlinienzeilen (beide Kurventabellen, `LEFT JOIN` ohne Stammsatz) | 0 → 0 | 0 → 0 |

Damit ist beides belegt:

* **A belegt den Befund.** Der Giftsatz scheitert erst, nachdem eine Kenndatenzeile
  geschrieben ist (`Kenndaten GIFT: 1 Zeile`), und hinterlässt einen Stammsatz mit
  unvollständiger Kennlinie. Die Meldung nennt ihn trotzdem nur als „fehlgeschlagen".
* **B belegt die Wirkung.** Der Giftsatz ist restlos verschwunden, die Zeilenzahlen wachsen
  nur noch um den Gutsatz, und es entstehen keine verwaisten Kennlinienzeilen.

**Nachweis, dass wirklich erst geschrieben und dann aufgeräumt wurde** (und nicht etwa der
Insert unterblieben ist): der Gutsatz bekommt in **beiden** Läufen den AutoWert **74**. In A
liegt der Giftsatz sichtbar auf 73; in B ist 73 ebenso verbraucht, obwohl dort kein Satz mit
dieser ID mehr existiert. Access gibt einen einmal vergebenen AutoWert nicht zurück — der
Stammsatz muss also angelegt und anschließend gelöscht worden sein. (Die Kennlinien-IDs
werden dagegen über `MAX(ID)+1` vergeben und nach dem Löschen wieder verwendet; in B trägt
die erste Zeile des Gutsatzes deshalb die 102979, die in A dem Giftsatz gehörte.)

**Erfolgsweg unverändert.** Die Stammfelder des Gutsatzes sind in A und B feldgleich:

```
ID=74  Bezeichner=HRN Klammer Gut 6  Firma=Viessmann  Typ=Luft/Wasser  Bauart=Kompakt
Aufstellung=innen  Nennleistung=6  Regelung=einstufig  Heizung=0  Kuehlleistung=0  ReadOnly=False
```

ebenso seine beiden Kennlinienzeilen (`Vorlauf=35 Temperatur=35 COP=42 Ptherm=55` und
`Vorlauf=35 Temperatur=35 COP=39 Ptherm=71`).

**Einzelfall-Regression** (Bestandsweg, `markiert.Count == 1`, eigener Bezeichner
`HRN Klammer Einzel 6`): in A und B gleich — Meldung „Daten gespeichert!", `Tab_WP_STAMM`
**+1**, `Tab_Kenndaten_STAMM` **+2**.

**Produktiv-DB unberührt.** `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` hat vor und nach
allen Läufen den Änderungszeitstempel 18.08.2026 09:26:52, also vor dem ersten Harness-Lauf
(10:11:23). Weder dort noch im Harness-Ordner ist eine `.laccdb`-Sperrdatei zurückgeblieben.

### 3.3 Encoding

| Datei | Messung vorher | Nachweis nachher |
|---|---|---|
| `Views\Wärmepumpe\Form_WP_einlesen.cs` | 12580 Bytes, kein BOM, 0 Bytes > 0x7F, 298 CRLF, 0 einzelne LF | 13825 Bytes, kein BOM, **0 Bytes > 0x7F**, 316 CRLF, **0 einzelne LF, 0 einzelne CR** |
| `Allgemein\Simulation\VdiImport_FilterMehrfach_Protokoll.md` | 16226 Bytes, kein BOM, UTF-8 (434 High-Bytes, strikt dekodierbar), 241 LF, **kein CRLF** | unverändert kodiert, um einen Satz ergänzt |
| `Allgemein\Simulation\VdiImport_WP_Transaktion_Protokoll.md` (neu) | — | UTF-8 ohne BOM, LF — wie die Geschwisterdatei |

Alle neuen Kommentare und Zeichenketten in `Form_WP_einlesen.cs` sind umlautfrei
(„ueber", „moeglich", „geloescht", „Aufraeumklammer"), weil die Datei rein ASCII ist und es
auch bleiben soll. Die beiden Protokolldateien sind UTF-8 und dürfen Umlaute führen.

## 4. Offene Punkte

* **Grenze der Lösung: harter Prozessabbruch.** Die Klammer greift im Programmablauf — bei
  `false` aus den Kenndaten-Inserts und bei jeder Ausnahme. Sie greift **nicht**, wenn der
  Prozess zwischen `Insert()` und dem Aufräumen hart endet (Absturz, Task-Kill, Stromausfall).
  In diesem Zeitfenster bleibt weiterhin ein unvollständiger Stammsatz stehen. Abdecken würde
  das nur eine echte Transaktion über Stammsatz und alle Kennlinienzeilen, und die setzt den
  in Abschnitt 2.1 beschriebenen Controller-Umbau voraus (Verbindung und `OleDbTransaction`
  durch `WPStammCtrl` durchreichen).
* **Scheiterndes Aufräumen wird nur protokolliert.** Liefert `Delete()` selbst `false` (DB
  nicht erreichbar), geht eine Zeile auf die Konsole; dem Anwender wird nichts zusätzlich
  gemeldet. Das ist bewusst so: das Meldeverhalten des Mehrfachladens sollte unverändert
  bleiben. Sobald es einen regulären Protokollkanal in diesem Dialog gibt, gehört die Zeile
  dorthin.
* **Kein Test mit echter VDI-Datei.** Wie im Geschwisterprotokoll: der Weg
  `btn_VDI3805_Click` → `ctrl.Import(datei)` ist unverändert, mangels `.vdi`-Beispieldatei
  aber nicht von der Datei aus gefahren.
* **Am Rande beobachtet, nicht angefasst:** `Program.convertTxt2Double` parst mit
  `InvariantCulture` und `Convert.ToDouble`, also inklusive `AllowThousands`. Ein Wert mit
  Dezimalkomma wird deshalb **nicht** abgewiesen, sondern als Tausendertrennzeichen gelesen:
  aus „5,5" wird 55, aus „4,2" wird 42. Im Smoke ist das sichtbar (COP 42, Ptherm 55). Für
  den A/B-Vergleich ist es unerheblich, weil beide Läufe dieselben Eingaben verwenden. Ob
  echte VDI-3805-Dateien Komma oder Punkt liefern, ist hier nicht geprüft — falls Komma,
  wären die importierten Kennlinien um Größenordnungen falsch.

---

## Nachtrag 18.08.2026 abends (unabhängige Nachprüfung)

* **Restlücke der Klammer geschlossen** (`Form_WP_einlesen.cs:241-254`): Der
  Stammsatz-Insert stand VOR dem `try` — weil `WPStammCtrl.Insert()` intern committet
  (`:170`) und erst danach `@@IDENTITY` liest, konnte ein `false` mit bereits
  geschriebenem Satz am Aufräumen vorbeispringen. Der Insert steht jetzt innerhalb der
  Klammer; scheitert er echt (kein Satz), läuft `Delete()` folgenlos ins Leere.
  Smoke `%TEMP%\wpk14`: sieben Szenarien, in allen Fehlerfällen Δ = 0 Zeilen in allen
  drei Tabellen, Waisenzahl durchgehend 0, Erfolgs- und Meldeverhalten unverändert.
* **Gravierender Bestandsbefund:** `WPStammCtrl.InsertKenndatenKuehlungStamm`
  (`WPStammCtrl.cs:195-203`) kann nie gelingen — `Last` ist unmaskiertes reserviertes
  Wort, und die Anweisung schreibt eine in `Tab_Kenndaten_Kuehlung_STAMM` nicht
  existierende Spalte `ReadOnly`. Jede VDI-Wärmepumpe MIT Kühlkennlinien scheitert
  deshalb beim Import vollständig (seit der Klammer sauber verworfen statt als Torso).
  Als eigener Task vorgemerkt; zu klären ist dort auch, woher die 174 Bestandszeilen
  der Kühltabelle stammen.
* Die Randnotiz zu `convertTxt2Double` ist inzwischen teilweise adressiert (Zahlprüfung
  `Program.ZahlParsen`/`ZahlPruefen` durch einen Parallelvorgang vom 18.08. vormittags);
  die Frage Komma vs. Punkt in echten VDI-Dateien bleibt offen.

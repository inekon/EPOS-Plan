# Roundtrip-Datenverlust im zentralen Erzeuger-Speicherweg

Stand 15.08.2026, Codebasis `0d52caa`. Nicht committet.

Der Speicherweg **aller** Erzeuger ist Löschen + Neuanlegen:
`WizardCtrl.Del_Projekt_Waermeerzeuger` gefolgt von `WizardCtrl.Add_WP_Waermeerzeuger`.
Jede Spalte, die die Einfügeanweisung nicht führt, ist damit bei **jedem** Speichern
verloren — über den Wizard, über die Karten der Startseite und über die Kontextmenüs des
Projektbaums gleichermaßen. `Add_WP_Waermeerzeuger` führte 29 der 57 Spalten von
`Tab_Energieanlagen`; die komplette Quellen-/Senken-Konfiguration aus Paket 1 fehlte.

---

## 1. Spalteninventur `Tab_Energieanlagen`

Schema-Dump über 32-bit-ACE auf einer DB-Kopie (`OleDbSchemaGuid.Columns`), Datenprobe über
dieselbe Verbindung. **57 Spalten.** `ID` ist AutoWert und wird nie geschrieben — 56 Spalten
sind schreibbar.

Legende: **M** = im `WErzeugerModel`, **L** = von `WErzeugerCtrl.ReadAllFilter`/`ReadSingle`
gelesen, **S** = von `WizardCtrl.Add_WP_Waermeerzeuger` geschrieben.

| # | Spalte | Typ | vorher M/L/S | nachher M/L/S |
|---|---|---|---|---|
| 1 | ID | LONG (AutoWert) | ✓ / ✓ / – | ✓ / ✓ / – |
| 2 | ID_Projekt | LONG | ✓ / ✓ / ✓ | unverändert |
| 3 | Bezeichner | TEXT | ✓ / ✓ / ✓ | unverändert |
| 4 | ID_Type | LONG | ✓ / ✓ / ✓ | unverändert |
| 5 | ID_WP | LONG | ✓ / ✓ / ✓ | unverändert |
| 6 | Betriebsart | TEXT | ✓ / ✓ / ✓ | unverändert |
| 7 | Sperrung | YESNO | ✓ / ✓ / ✓ | unverändert |
| 8 | Sperrzeit_von | LONG | ✓ / ✓ / ✓ | unverändert |
| 9 | Sperrzeit_bis | LONG | ✓ / ✓ / ✓ | unverändert |
| 10 | Vorlauf | LONG | ✓ / ✓ / ✓ | unverändert |
| 11 | Rücklauf | LONG | ✓ / ✓ / ✓ | unverändert |
| 12 | Bivalenter_Betrieb | YESNO | ✓ / ✓ / ✓ | unverändert |
| 13 | Abschaltpunkt | DOUBLE | ✓ / ✓ / ✓ | unverändert |
| 14 | Nutzungszeit | LONG | ✓ / ✓ / ✓ | unverändert |
| 15 | ID_SP | LONG | ✓ / ✓ / ✓ | unverändert |
| 16 | ID_PV | LONG | ✓ / ✓ / ✓ | unverändert |
| 17 | ID_Solar | LONG | ✓ / ✓ / ✓ | unverändert |
| 18 | Heizstab | YESNO | ✓ / ✓ / ✓ | unverändert |
| 19 | Volumen | DOUBLE | ✓ / ✓ / ✓ | unverändert |
| 20 | rendeMix | YESNO | ✓ / ✓ / ✓ | unverändert |
| 21 | Solaranteil | LONG | ✓ / ✓ / ✓ | unverändert |
| 22 | ID_Kessel | LONG | ✓ / ✓ / ✓ | unverändert |
| 23 | ID_BHKW | LONG | ✓ / ✓ / ✓ | unverändert |
| 24 | Grenzleistung | DOUBLE | ✓ / ✓ / ✓ | unverändert |
| 25 | Kollektormodulanzahl | LONG | ✓ / ✓ / ✓ | unverändert |
| 26 | PV_Leistung | DOUBLE | ✓ / ✓ / ✓ | unverändert |
| 27 | Neigung | LONG | ✓ / ✓ / ✓ | unverändert |
| 28 | Azimut | LONG | ✓ / ✓ / ✓ | unverändert |
| 29 | ID_PUFFER | LONG (FK) | ✓ / ✓ / ✓¹ | ✓ / ✓ / ✓ (Fix 3) |
| 30 | ID_Carrier | LONG | ✓ / ✓ / ✓² | ✓ NULL-treu / ✓ / ✓ |
| 31 | Prioritaet | LONG | – / – / – | **✓ / ✓ / ✓** |
| 32 | BM_Typ | TEXT | – / – / – | **✓ / ✓ / ✓** |
| 33 | WQ_Typ | TEXT | – / – / – | **✓ / ✓ / ✓** |
| 34 | WQ_Temp | DOUBLE | – / – / – | **✓ / ✓ / ✓** |
| 35 | WQ_Monatswerte | TEXT | – / – / – | **✓ / ✓ / ✓** |
| 36 | WQ_Wochenwerte | TEXT/MEMO | – / – / – | **✓ / ✓ / ✓** |
| 37 | WQ_CSV | TEXT | – / – / – | **✓ / ✓ / ✓** |
| 38 | WQ_Puffer | TEXT | – / – / – | **✓ / ✓ / ✓** |
| 39 | WQ_ID_Puffer | LONG (FK) | – / – / – | **✓ / ✓ / ✓** |
| 40 | WQ_Spreizung | DOUBLE | – / – / – | **✓ / ✓ / ✓** |
| 41 | WQ_Regeneration | DOUBLE | – / – / – | **✓ / ✓ / ✓** |
| 42 | WQ_Unbegrenzt | YESNO | – / – / – | **✓ / ✓ / ✓** |
| 43 | WQ_Tiefe | DOUBLE | – / – / – | **✓ / ✓ / ✓** |
| 44 | WQ_Flaeche | DOUBLE | – / – / – | **✓ / ✓ / ✓** |
| 45 | WQ_Anzahl | LONG | – / – / – | **✓ / ✓ / ✓** |
| 46 | WQ_Bodentyp | TEXT | – / – / – | **✓ / ✓ / ✓** |
| 47 | WQ_Quellsystem | TEXT | – / – / – | **✓ / ✓ / ✓** |
| 48 | WS_Typ | TEXT | – / – / – | **✓ / ✓ / ✓** |
| 49 | WS_Ziel | TEXT | – / – / – | **✓ / ✓ / ✓** |
| 50 | WS_ID_Puffer | LONG (FK) | – / – / – | **✓ / ✓ / ✓** |
| 51 | WS_Ladeprio | LONG | – / – / – | **✓ / ✓ / ✓** |
| 52 | WS_Ladegrenze | DOUBLE | – / – / – | **✓ / ✓ / ✓** |
| 53 | WS_Ladeprio_PV | LONG | – / – / – | **✓ / ✓ / ✓** |
| 54 | WS_Ziel2 | TEXT | – / – / – | **✓ / ✓ / ✓** |
| 55 | WS_ID_Puffer2 | LONG (FK) | – / – / – | **✓ / ✓ / ✓** |
| 56 | WS_Ladeprio2 | LONG | – / – / – | **✓ / ✓ / ✓** |
| 57 | WS_Ladegrenze2 | DOUBLE | – / – / – | **✓ / ✓ / ✓** |

¹ geschrieben, aber bei gescheiterter Katalogauflösung als NULL — siehe Fix 3.
² geschrieben, aber `int`: ein NULL in der Datenbank wurde zur 0.

**Bilanz: 57 Spalten. Modell 30 → 57. Leseseite 30 → 57. Schreibseite 29 → 56**
(alles außer dem AutoWert `ID`).

### Beziehungen (DAO `Relations`, Attributes = 0 = erzwungen)

`ID_PUFFER`, `WQ_ID_Puffer`, `WS_ID_Puffer`, `WS_ID_Puffer2` → `Tab_Pufferspeicher.ID`;
dazu die Komponenten-FK auf `Tab_WP`, `Tab_BHKW`, `Tab_Heizkessel`, `Tab_PV`,
`Tab_Solarkollektoren`, `Tab_Stromspeicher`, `Tab_Typ_Energieanlagen`, `Tab_Projekt`.
**0 ist bei keiner dieser Spalten eine gültige ID** — „nicht gesetzt" ist NULL.

### Datenbefund zur NULL-Semantik (79 Anlagen)

| Spalte | NULL | = 0 | > 0 |
|---|---|---|---|
| `WS_ID_Puffer` / `WS_ID_Puffer2` / `WQ_ID_Puffer` | 73 / 78 / 78 | **0** | Rest |
| `WS_Ladeprio`, `WS_Ladeprio2`, `WS_Ladeprio_PV`, `WS_Ladegrenze`, `WS_Ladegrenze2` | **2** | **77** | – |
| `ID_Carrier` | 74 | 3 | 2 |

Die Ladeprio-Spalten führen **beide** Zustände im Bestand. Das ist der Grund für nullable
Typen im Modell: 0 heißt „nach Vorgabe" (Konzept 3.4), NULL heißt „unbelegt", und ein
Speichern darf keinen der beiden in den anderen umschreiben.

---

## 2. Die Fixe

### Fix 1 — Symmetrie Modell / Leseseite / Schreibseite

* `Model/WErzeugerModel.cs:63-201` — 27 neue Felder mit den DB-Spaltennamen.
  Nullable dort, wo NULL eine eigene Aussage ist (`int?`, `double?`); Text als `string`
  mit Vorbelegung `null`, damit NULL und Leerstring unterscheidbar bleiben.
  `WErzeugerModel.cs:210-222` — Vorbelegung: die fünf Ladeprio-/Ladegrenzenfelder auf **0**
  (identisch zu `ProjektPuffer.AnlagenzeileParameter` und zur bisherigen
  `WErzeugerCtrl.Insert`, Paket-4-Review Punkt 9), alles Übrige NULL.
* `Controller/WErzeugerCtrl.cs:174-244` — **eine** Leseabbildung `AusZeile` für
  `ReadAllFilter` **und** `ReadSingle`. Die Bestandsspalten behalten das Muster „vorhanden
  und nicht NULL ⇒ übernehmen"; die 27 neuen werden **ausdrücklich** zugewiesen, auch mit
  `null` — sonst würde die 0-Vorbelegung ein NULL aus der Datenbank überschreiben.
  Nebenbei behoben: In `ReadAllFilter` stand bei `Azimut` ein nicht kurzschließendes `&`,
  das bei fehlender Spalte geworfen hätte.
* `Controller/WizardCtrl.cs:153-187` — `SQL_ANLAGE_INSERT` mit allen 56 schreibbaren
  Spalten; `WizardCtrl.cs:189-287` — `AnlagenParameter`. Die neuen Parameter laufen
  durchgehend über `ProjektPuffer.Par` mit **ausdrücklichem** `OleDbType` (aus `DBNull`
  allein leitet der Provider keinen Typ ab — dieselbe Regel wie in
  `WaermequelleClass.WertSchreiben`).

### Fix 2 — NULL-Semantik der Fremdschlüssel

`WizardCtrl.cs:305-320` `PufferFkOderNull`: `null` oder ≤ 0 ⇒ `DBNull`, **nie 0**.
Zusätzlich fällt eine ID weg, die auf keine Speicherzeile mehr zeigt
(`WizardCtrl.cs:339-350` `PufferVorhanden`, Trefferzwischenspeicher je Durchlauf).
Grund: `Add_WP_Waermeerzeuger` läuft **immer nach einem DELETE**. Ein an der erzwungenen
Beziehung scheiterndes INSERT würde die Anlagen gelöscht zurücklassen — mehr Schaden als
der behobene Datenverlust. Die verwaiste Referenz wird protokolliert und als leer
gespeichert; das ist dieselbe Normalisierung, die `WaermesenkeClass.Normalisieren` beim
Lesen ohnehin vornimmt.

### Fix 3 — `ID_PUFFER` überlebt eine gescheiterte Katalogauflösung

`WizardCtrl.cs:395-410`. `PufferSpCtrl.CopyFromStamm(bezeichner, …)` sucht den Bezeichner in
`Tab_Pufferspeicher_STAMM`. Ein Projekt-Puffer, den es dort nicht gibt — umbenannt oder frei
angelegt, etwa **„Vitocell 140-E 600 Liter"** gegenüber dem Katalognamen
**„Vitocell 140-E 600 Ltr"** — liefert −1, und die Anlage verlor ihren Speicher bei jedem
Speichern. Gemessen: **drei von sechs** Puffer-Anlagen in 1023/1024.
Die vorhandene `ID_PUFFER` bleibt jetzt stehen, wenn sie auf eine **Projektkopie dieses
Projekts** zeigt (`PufferGehoertZuProjekt`, `WizardCtrl.cs:322-337`). Genau diese Bedingung
schließt den Fall aus, vor dem der 0-Rückfall schützen sollte: eine STAMM-ID trägt kein
`ID_Projekt` dieses Projekts.

### Fix 4 — `ID_Carrier` NULL-treu

`Model/WErzeugerModel.cs:36-62`. Aus dem Feld wurde `int? ID_CarrierRoh` plus eine
Eigenschaft `int ID_Carrier` darüber. Alle Aufrufstellen (`Form_BHKWEing`,
`Form_Heizkessel`, `WizardParent`, `Add_Projekt_Energietraeger`) bleiben unverändert;
gelesen wird weiterhin `int`. Die Leseseite (`WErzeugerCtrl.cs:211`) und die Schreibseite
(`WizardCtrl.cs:283`) benutzen den Rohwert. 0 und NULL heißen beide „kein Energieträger"
(SchemaKatalog, Schritt 8), aber der Bestand führt beide Schreibweisen — und ein Speichern
soll keine in die andere umschreiben. Eine frisch angelegte Anlage bekommt weiterhin 0.

### Konsistenz — keine zwei Halbwahrheiten

* `WErzeugerCtrl.Insert` (`WErzeugerCtrl.cs:101-114`) benutzt jetzt
  `WizardCtrl.SQL_ANLAGE_INSERT` und `WizardCtrl.AnlagenParameter`. Die frühere eigene
  Fassung führte 21 Spalten und schrieb `WS_Ladeprio` & Co. mit hartkodierter 0; diese 0
  steckt jetzt in der Modellvorbelegung und gilt damit für **jeden** Einfügeweg. (Die
  Methode hat im Anwendungscode keinen Aufrufer — sie wird trotzdem mitgeführt, damit kein
  zweiter Spaltensatz stehen bleibt.)
* `WErzeugerCtrl.Update` (`WErzeugerCtrl.cs:18-30`) bleibt bewusst auf den Grunddaten: Ein
  UPDATE lässt nicht genannte Spalten stehen, ist also nicht verlustbehaftet. Die
  Quellen-/Senken-Spalten pflegen `WaermesenkeClass.Schreiben` und
  `WaermequelleClass.WertSchreiben` gezielt je Anlage. Der Kommentar an der Methode hält
  das fest.
* Wiederverwendet statt dupliziert: `ProjektPuffer.Par` (typisierte Parameter),
  `PufferSpCtrl.CopyFromStamm`, `WaermesenkeClass`/`WaermequelleClass` bleiben die
  Schreibwege für Einzelfelder.

---

## 3. Verifikation

Alles headless, x86. Die produktive `Kenndaten.accdb` wurde **ausschließlich gelesen**
(`Kenndaten.laccdb` war nicht vorhanden); gerechnet und geschrieben wurde auf Wegwerf-Kopien
außerhalb des Repos bzw. unter `dev\`. Nichts committet.

### 3.1 A/B-Aufbau

Zwei Builds derselben Quelle, Unterschied nur in den drei geänderten Dateien:
`dev\outbase` (Stand `0d52caa`, Dateien per `git checkout --` zurückgeholt und danach
wiederhergestellt) und `dev\out` (mit den Fixen). Ein Reflection-Harness
(Muster `ui-pfad-test-reflection-harness`) biegt `Properties.Settings.Default.DBPath` auf
die Kopie um, liest über `WErzeugerCtrl.ReadAllFilter` wie die Oberfläche, ruft
`Del_Projekt_Waermeerzeuger` + `Add_WP_Waermeerzeuger` und vergleicht **alle 57 Spalten**
vorher/nachher (Zuordnung über `Bezeichner|ID_Type`, `ID` ausgenommen — AutoWert).
**Je Speicherweg eine frische DB-Kopie**, damit sich die Wege nicht gegenseitig
überdecken.

Probenzustände vorher gesetzt (Projekt 1023): Anlage 11203 `WS_Ladeprio`/`WS_Ladeprio2`/
`WS_Ladegrenze` = NULL, `WQ_Typ='Pufferspeicher'`, `WQ_ID_Puffer=1018023`; Anlage 11204
`WS_Ladeprio=0`, `WS_Ladeprio2=7`, `WS_Ladegrenze=55,5`, kompletter Erdreich-Satz
(`WQ_Quellsystem='Sonde'`, `WQ_Tiefe=90`, `WQ_Anzahl=3`, `WQ_Bodentyp='SandFeucht'`,
`WQ_Spreizung=5,5`, `WQ_Regeneration=1,25`, `WQ_Unbegrenzt=True`, `WQ_Temp=8,5`,
168 Wochenwerte), `BM_Typ='Laufzeit'`, `Prioritaet=2`, `WS_Typ='Heizung'`.

### 3.2 57-Spalten-Roundtrip je Speicherweg

| Speicherweg | Projekt | Feldvergleiche | vorher | nachher |
|---|---|---|---|---|
| Wizard-Bearbeiten (`Del(Projekt)` + `Add`) | 1024 | 280 | **28 Verluste** | **0** |
| Wizard-Bearbeiten (`Del(Projekt)` + `Add`) | 1023 | 392 | **63 Verluste** | **0** |
| Karte `Form_Start` (BHKW, `ID_Type=11`) | 1024 | 280 | **9 Verluste** | **0** |
| Kontextmenü Pufferspeicher (`ID_Type=12`) | 1024 | 280 | **14 Verluste** | **0** |
| Kontextmenü Wärmepumpe (`ID_Type=1`) | 1023 | 392 | **30 Verluste** | **0** |

Am realen BHKW **„A-Tron_21_F" (Projekt 1024)** — der dokumentierte Beleg — verlor der
Karten-Weg vorher `WS_Typ='Beides'`, `WS_Ziel='Heizkreis'`,
`WS_Ziel2='PufferBrauchwasser'`, `WS_ID_Puffer2=1054164` sowie die fünf Ladeprio-Nullen;
nachher überleben alle 56 Werte unverändert.

Wizard-Neu (frisches Modell, `ID_Type=12`): `WS_Ladeprio` = `WS_Ladeprio2` =
`WS_Ladeprio_PV` = 0, `WS_Ladegrenze` = `WS_Ladegrenze2` = 0, `WS_ID_Puffer` =
`WS_ID_Puffer2` = `WQ_ID_Puffer` = NULL, `ID_Carrier` = 0. Vorher standen die fünf
Ladeprio-Felder auf NULL — die Änderung ist gewollt (Fix 9 gilt jetzt für alle
Einfügewege) und rechnerisch neutral: `StilleDb.Zahl(NULL)` liefert 0, und
`WaermesenkeClass.VorbelegungNachziehen` zieht NULL beim Engine-Einstieg ohnehin auf 0.

### 3.3 NULL-Semantik-Proben

Zustand nach dem Roundtrip von Projekt 1023, identisch zum Zustand davor:

```
CS6800iAW MB + AW 10 OR-T : WS_Ziel='PufferHeizung'  WS_ID_Puffer=1018023
                            WS_Ladeprio=<NULL>  WS_Ladeprio2=<NULL>  WS_Ladegrenze=<NULL>
                            WQ_Typ='Pufferspeicher'  WQ_ID_Puffer=1018023  ID_Carrier=<NULL>
CS7800iLW 12              : WS_Ladeprio=0  WS_Ladeprio2=7  WS_Ladegrenze=55,5
                            WS_Ladegrenze2=0  WS_ID_Puffer=1018023  WQ_Unbegrenzt=True
                            Prioritaet=2  WQ_Tiefe=90  WQ_Anzahl=3  WQ_Bodentyp='SandFeucht'
                            BM_Typ='Laufzeit'  WS_Typ='Heizung'
ecoVIT VKK 186/5          : WS_Ziel='Heizkreis'  WS_ID_Puffer=<NULL>
                            WS_ID_Puffer2=<NULL>  WQ_ID_Puffer=<NULL>
```

Damit belegt: NULL bleibt NULL (nie 0), 0 bleibt 0, ein Wert ≠ 0 bleibt erhalten, und
`WS_Ziel='PufferHeizung'` mit gesetzter Puffer-ID übersteht den Roundtrip unverändert.

### 3.4 Engine-Regression, Flag AUS

`Referenzlauf.exe lauf --projekte 1007,1008,1010,1011,1017,1018,1021,1023,1024` mit dem
gefixten Build, gegen `Referenzlaeufe/2026-08-15_B2`:

```
Projekt_1007 … 1024 : 9 x PASS
GESAMT: PASS (2 295 987 Werte innerhalb der Toleranz)
Byte-/MD5-Gegenprobe: 208 von 208 CSV gleich, 0 abweichend, 0 fehlend
```

Damit ist die Leseseite, die die Simulation speist (`ReadAllFilter`), bitgenau
unverändert.

### 3.5 Engine-Regression, Flag AN

Eine migrierte Kopie außerhalb der Referenzordner, `Kaskade_Zweikanalig = TRUE` für 1023
und 1024, jedes Projekt zweimal im Modus `projekt` gerechnet — einmal mit dem Baseline-,
einmal mit dem gefixten Build:

```
vergleich flag_base flag_fix : Projekt_1023 PASS, Projekt_1024 PASS
                               GESAMT: PASS (534 608 Werte)
Byte-/MD5:                     51 von 51 CSV gleich, 0 abweichend
```

Kennzahlen aus `aggregate.csv` (Flag AN):

| Projekt | Deckungsanteile | Summe | dokumentiert |
|---|---|---|---|
| 1023 | WP 50,69 % + Kessel 17,16 % | **67,85 %** | 67,854567 % |
| 1024 | WP 21,43 % + BHKW 46,35 % + Kessel 12,10 % | **79,88 %** | 79,877292 % |

`aggregate.csv` führt zwei Nachkommastellen; die feineren Stellen der Referenz stammen aus
der Probe-Harness aus Paket 6, die nicht im Repo liegt. Der schärfere Nachweis ist der
A/B-Byte-Vergleich darüber: Baseline und Fix rechnen mit gesetztem Flag **bitgleich**.

### 3.6 Build

```
MSBuild WindowsFormsApplication1.csproj -t:Rebuild -p:Configuration=Debug -p:Platform=x86
  -> 0 Fehler, 6 Warnungen
```

Dieselben sechs Bestandswarnungen wie bisher (`WErzeugerModel.cs` CS0108,
`StromverbraucherStammCtrl.cs` CS0108, `KlimaregionStammCtrl.cs` 2 × CS0109,
`MDIMainForm.cs` CS4014 und CS1998) — keine neue. Kodierung (UTF-8 mit BOM) und
Zeilenenden (CRLF) der drei Dateien unverändert, kein Mojibake.

---

## 4. Nachtrag 15.08.2026 — `WizardParent.LoadWEFromDB` (Restbefund **erledigt**)

Der Restbefund war eine Teilkopie **vor** dem Speicherweg: `Views/Wizard/WizardParent.cs`
legte für jede gelesene Anlage ein **neues** `WErzeugerModel` an und kopierte 28 Felder
einzeln. Nicht kopiert wurden `ID_PUFFER` **und alle 27 Spalten der
Quellen-/Senken-Konfiguration**. `list_werzmodel` aus dieser Schleife
(`WizardParent.cs:233`) ist genau die Liste, die der Bearbeiten-Zweig von
`btnSpeichern_Click` nach `Del_Projekt_Waermeerzeuger` an `Add_WP_Waermeerzeuger`
übergibt — der Verlust entstand also, bevor der seit Fix 1–4 verlustfreie Speicherweg
überhaupt anlief.

### Fix 5 — vollständige Modelle durchreichen statt umkopieren

`Views/Wizard/WizardParent.cs:511-554`. Die Schleife lautet jetzt
`list_werzmodel.Add(werzctrl.items[n])` — dieselbe Umstellung, die `0d52caa` in den sechs
Kontextmenü-Controllern und `3b3ea26` auf den Karten der Startseite vorgenommen hat. Damit
kommt jede künftige Spalte automatisch mit, sobald `WErzeugerCtrl.AusZeile` sie liest.

**Erhalten geblieben ist die einzige bewusste Zuweisung der Teilkopie:**
`item.ID_Projekt = projctrl.m_ID` (`WizardParent.cs:549`). Sie ist durch den Filter
`ID_Projekt=<m_ID>` wertgleich mit der gelesenen Zeile und greift nur, falls die Spalte in
einer alten Datenbank fehlt — `AusZeile` ließe `ID_Projekt` dann auf 0 stehen. Alle
übrigen 27 Zuweisungen der alten Schleife waren reine 1:1-Kopien und entfallen; der
unbenutzte `ListViewItem lvitem` ebenfalls.

### Verifikation des Nachtrags

Zwei Builds derselben Quelle (`8596564` mit und ohne diese Änderung), Reflection-Harness
über den **echten** Weg: `new WizardParent()` → `LoadWEFromDB(<Projektname>)` →
`Del_Projekt_Waermeerzeuger(projektID)` + `Add_WP_Waermeerzeuger(projektID,
list_werzmodel)`. Je Lauf eine frische DB-Kopie mit den Probenzuständen aus 3.1
(Anlage 11203 NULL-Ladeprios + `WQ_Typ='Pufferspeicher'`/`WQ_ID_Puffer=1018023`,
Anlage 11204 kompletter Erdreich-Satz, `WS_Ladeprio2=7`, `WS_Ladegrenze=55,5`,
`BM_Typ='Laufzeit'`, `Prioritaet=2`, `WS_Typ='Heizung'`).

| Projekt | Anlagen | Feldvergleiche | vorher | nachher |
|---|---|---|---|---|
| 1023 | 7 | 392 | **34 Abweichungen** | **0** |
| 1024 | 5 | 280 | **13 Abweichungen** | **0** |

Verloren gingen vorher u. a.: `WQ_Typ`, `WQ_Temp`, `WQ_Wochenwerte` (168 Werte),
`WQ_Spreizung`, `WQ_Regeneration`, `WQ_Unbegrenzt`, `WQ_Tiefe`, `WQ_Anzahl`,
`WQ_Bodentyp`, `WQ_Quellsystem`, `WQ_ID_Puffer`, `BM_Typ`, `Prioritaet`, `WS_Typ`,
`WS_Ziel`, `WS_ID_Puffer`, `WS_Ladegrenze` 55,5 → 0, `WS_Ladeprio2` 7 → 0, die
NULL→0-Umschrift der fünf Ladeprio-Felder — und `ID_Carrier` NULL → 0, weil die Teilkopie
über die `int`-Sicht statt über `ID_CarrierRoh` lief.

Am realen BHKW **„A-Tron_21_F" (Projekt 1024)** verlor der Wizard-Weg vorher
`WS_Typ='Beides'`, `WS_Ziel='Heizkreis'`, `WS_Ziel2='PufferBrauchwasser'` und
`WS_ID_Puffer2=1054164`; nachher überleben alle 56 Werte. Ebenso überlebt jetzt
`ID_PUFFER` der nicht katalogisierten Projekt-Puffer: 1018022 („test"), 1024050
(„Vitocell 140-E 600 Liter", 1023), 1036082 und 1054164 (1024) — vorher fielen sie
sämtlich auf NULL.

**Gegenprobe Wizard-NEU-Zweig.** `Next()` ruft `LoadWEFromDB` auch im NEU-Modus, dort mit
einem Projektnamen, den es noch nicht gibt. Beide Builds liefern 0 Modelle ohne Ausnahme,
und die anschließend über ein frisches `WErzeugerModel` (`ID_Type=12`) geschriebene Zeile
ist in **allen 56 Spalten** zeichengleich zwischen Baseline und Fix.

**Gegenprobe Energieträger-Kette (512b904).** In Projekt 1024 überlebt `ID_Carrier=71`
Laden und Speichern; nach dem Leeren von `energy_price`/`energy_Project_settings` legt
`Add_Projekt_Energietraeger` **genau ein Paar** an (1 / 1 bei einem verschiedenen Träger
> 0). Projekt 1023 führt keinen Träger > 0 — dort bleibt es erwartungsgemäß bei 0 / 0.

**Engine-Regression.** `Referenzlauf.exe lauf --projekte 1007,1008,1010,1011,1017,1018,
1021,1023,1024` mit dem Fix-Build gegen `Referenzlaeufe/2026-08-15_B2`:
9 × PASS, GESAMT PASS (2 295 987 Werte); Byte-/MD5-Gegenprobe **208 von 208 CSV gleich**,
0 abweichend, 0 fehlend. Erwartungsgemäß — `WizardParent` liegt nicht im Rechenpfad.

**Build.** `MSBuild WindowsFormsApplication1.csproj -t:Rebuild -p:Configuration=Debug
-p:Platform=x86 -p:OutDir=…` → **0 Fehler, 6 Warnungen**, dieselben sechs Bestandswarnungen
wie oben. Kodierung (UTF-8 mit BOM) und Zeilenenden (CRLF) von `WizardParent.cs`
unverändert.

Damit sind **alle fünf** gemessenen Speicherwege verlustfrei. Der zugehörige Restbefund in
`Konzept_Simulation_QuellenSenken.md` („`WizardParent.LoadWEFromDB` liest `ID_PUFFER` nicht")
ist im selben Zug als behoben markiert worden.

## 5. Restrisiken

* **Fehlende Spalten in einer nicht migrierten Datenbank.** `SQL_ANLAGE_INSERT` nennt alle
  56 Spalten; fehlt eine, scheitert das INSERT nach dem DELETE. Dieselbe Eigenschaft hat
  `ProjektPuffer.SQL_ANLAGENZEILE_INSERT` seit Paket 1. Abgesichert ist das über
  `SchemaMigration` beim Programmstart und `WaermequelleClass.SchemaSicherstellen` als
  Rückfallebene; ein zusätzlicher Schutz wurde hier nicht eingebaut.
* **`WQ_Wochenwerte`** wird als `OleDbType.VarWChar` gebunden (wie in
  `WaermequelleClass.WertSchreiben`). Mit 168 Werten (~500 Zeichen) im Roundtrip geprüft —
  byte-gleich. Der Schema-Dump meldet für die Spalte `adWChar`, obwohl `SchemaKatalog` sie
  als `MEMO` anlegt; bei deutlich längeren Inhalten wäre das nachzumessen.
* **Verwaiste Puffer-Referenzen** werden beim Speichern still auf NULL gesetzt (nur
  Konsolenzeile). Das ist gewollt (siehe Fix 2), aber der Anwender bekommt es nicht zu
  sehen.
* **`WErzeugerCtrl.Insert`** hat keinen Aufrufer und ist mit dieser Änderung erstmals
  gegen die erzwungenen Komponenten-Beziehungen tragfähig (vorher schrieb sie `ID_WP = 0`).
  Sie ist nicht end-to-end geprüft, weil kein Pfad sie ruft.
* Der Flag-AN-Nachweis vergleicht Baseline gegen Fix, nicht gegen die in Paket 6
  dokumentierten sechsstelligen Deckungswerte — die zugehörige Probe-Harness liegt nicht im
  Repo.

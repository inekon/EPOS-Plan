# FR-1 — Puffer-Verlust im Del+Add-Speicherweg (behoben)

Stand: 27.08.2026 · Branch `Pufferspeicher` · Folgeticket FR-1 aus
[`Fehlerrunde_2026-08-27_Protokoll.md`](Fehlerrunde_2026-08-27_Protokoll.md) ·
Feldbeleg: Projekte 1027 („Beispiel WP WG 1 - Andere WP") und 1009 („Heinestr 15A") ohne
`Tab_Pufferspeicher`-Zeilen und ohne `ID_Type`-12-Anlagenzeilen, aber mit
`WS_Ziel = 'PufferHeizung'` und `WS_ID_Puffer = NULL` an den Erzeugern.

## Mechanismus

Der Speicherweg aller Erzeuger ist Löschen + Neuanlegen. Drei Glieder ergeben zusammen den
Verlust:

1. `WizardCtrl.Del_Projekt_Waermeerzeuger(int)` — die **typlose** Überladung — löschte ALLE
   Anlagenzeilen des Projekts, auch die Pufferspeicher (`ID_Type = 12`).
2. `Add_WP_Waermeerzeuger` schreibt nur die Liste des Dialogs zurück. Jede Liste ohne die
   12er-Modelle ließ die Pufferzeilen verschwinden. Und selbst eine Liste MIT den 12ern war
   kein Schutz: Verwirft der Anwender die `AnlagenEindeutigkeit.Aufnehmen`-Nachfrage, wird
   das Item übergangen (`WizardCtrl.cs`, `if (idNeu <= 0) continue;`) — die Zeile war dann
   gelöscht und nicht zurückgeschrieben.
3. `GeraeteWaisen.Aufraeumen` (läuft am Ende jedes `Add_WP_Waermeerzeuger`) räumt
   Gerätezeilen ab, auf die nichts mehr zeigt. Ohne die 12er-Anlagenzeile hängen die
   `Tab_Pufferspeicher`-Zeilen nur noch an Sekundärverweisen (`WS_ID_Puffer*`-Spalten der
   Erzeuger, Alt-Zuordnung `Z_ProjektPufferSp`, Verbund) — sobald auch die kippen, sind die
   Gerätezeilen weg. `PufferFkOderNull` räumt beim nächsten Schreiben die tote Referenz zu
   NULL ab und erzeugt genau das Feldbild `WS_Ziel = 'PufferHeizung'` + `WS_ID_Puffer = NULL`.

Die typlose Überladung hat projektweit **genau einen** Aufrufer: den Bearbeiten-Zweig des
Wizards (`WizardParent.btnSpeichern_Click`, Zeile ~669). Alle übrigen 16 Del-Stellen
(Startseiten-Karten, Kontextmenüs, Simulationsdetail) rufen seit dem Typfilter-Umbau
([`WPKontextMenu_Typfilter_Protokoll.md`](WPKontextMenu_Typfilter_Protokoll.md)) die
typisierte Überladung mit einer typreinen Liste — sie waren bereits sicher.

## Entscheidung: Löschen darf die 12er nicht anfassen

Zur Frage „darf `Del_Projekt_Waermeerzeuger(int)` die ID_Type-12-Zeilen löschen, oder muss
`Add_WP_Waermeerzeuger` sie mitschreiben?": **Ersteres wird verboten.** Der Wizard hat keine
Puffer-Seite (`PUFFER_ITEM` = 13 ist nirgends registriert), der Anwender kann Puffer dort
weder anlegen noch entfernen — also hat der Wizard-Rundumschlag an den Pufferzeilen nichts
zu suchen. Das Durchschleifen über die Liste (seit dem Roundtrip-Umbau lädt `LoadWEFromDB`
alle Typen) wäre als Schutz zufällig und bleibt über den `Aufnehmen`-Pfad (oben, Glied 2)
verlustanfällig. Die Sperre liegt zentral in der Überladung selbst — damit ist auch jeder
KÜNFTIGE Aufrufer pufferfest, egal was seine Liste führt.

## Fix (zwei Hälften, die zusammengehören)

| Stelle | Änderung |
|---|---|
| `Controller/WizardCtrl.cs`, `Del_Projekt_Waermeerzeuger(int)` | `DELETE … WHERE ID_Projekt = ? AND ID_Type <> 12` — die typlose Überladung verschont die Pufferzeilen (Konstante `WizardItemClass.PUFFER_TYP` fest im SQL, Muster `SP_TYPEN`) |
| `Views/Wizard/WizardParent.cs`, `entferne_nicht_aktive_elemente` | 12er-Modelle fliegen vor dem Speichern aus `list_werzmodel` — sonst legte `Add_WP_Waermeerzeuger` die stehen gebliebenen Zeilen doppelt an (die 12er kommen im Bearbeiten-Modus ausschließlich über `LoadWEFromDB` in die Liste) |

Nicht geändert, absichtlich:

- **Typisierte Überladung** `Del_Projekt_Waermeerzeuger(int, int)`: Puffer-Karte
  (`Form_Start`, `pBox_Pufferspeicher_Click`) und `PufferSpKontextMenuCtrl` rufen sie mit
  `ID_Type = 12` — das ist der legitime Lösch+Neuschreib-Weg der Pufferverwaltung.
- **Einzelzeilen-Löschung** `Del_Projekt_ID_Waermeerzeuger` (Kontextmenü „Löschen").
- **Projekt-Löschweg** `WErzeugerCtrl.Delete()` (eigenes DELETE ohne Typfilter): Beim
  Projektlöschen MÜSSEN alle Zeilen fallen, die Gerätezeilen räumt der Aufräumlauf.
- `Add_Projekt_Energietraeger` braucht keine Anpassung: Puffer-Items führen kein
  `ID_Carrier` und wurden dort schon immer übersprungen.

## Beleg (Wegwerf-Kopie der Produktiv-DB, Migrationsstand 49)

Kopie über `Referenzlauf.exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb <Ziel>`,
Harness `dev/harness_fr1` (Reflection, stellt den Wizard-Bearbeiten-Zweig nach:
Liste = `ReadAllFilter("ID_Projekt=…")` minus 12er-Filter, dann `Del(int)` + `Add`).
Zeilenzählungen je Tabelle, identische Sequenz vor und nach dem Fix:

| Projekt | Messung | ungefixt (1eff065) | gefixt |
|---|---|---|---|
| 1008 | Anlagenzeilen gesamt | 5 → 3 | 5 → 5 |
| 1008 | davon `ID_Type = 12` | **2 → 0** | **2 → 2** (IDs 11240/11241 unverändert) |
| 1008 | `Tab_Pufferspeicher` (Projekt) | 2 → 2 ¹ | 2 → 2 |
| 1026 | Anlagenzeilen gesamt | 6 → 5 | 6 → 6 |
| 1026 | davon `ID_Type = 12` | **1 → 0** | **1 → 1** (ID 11264 unverändert) |
| 1026 | `Tab_Pufferspeicher` (Projekt) | 1 → 1 ¹ | 1 → 1 |

¹ Im ungefixten Lauf überlebten die Gerätezeilen nur über Sekundärverweise (1008007 über
`WS_ID_Puffer` der Erzeuger und `Z_ProjektPufferSp`, 1008008 nur über `Z_ProjektPufferSp`,
1054165 nur über `WS_ID_Puffer2`) — der Zustand, aus dem die Feldprojekte 1027/1009 beim
nächsten Konfigurationsschritt in den Totalverlust gekippt sind. Nach dem Fix sind die
Zeilen wieder regulär über die Anlagenzeile referenziert.

Nebengewinn: Die Puffer-Anlagenzeilen behalten über Wizard-Speicherungen ihre IDs — die
anlagenbezogenen Kostenpositionen (`ID_Anlage`, Ä20/Ä25) verlieren ihren Anker nicht mehr.

## Neutralität

Kein Berührungspunkt mit dem Rechenkern: Geändert sind ein DELETE-Filter im UI-Speicherweg
und ein Listenfilter im Wizard. Simulation, Referenzlauf und SchemaMigration rufen keine der
beiden Stellen (SchemaMigration enthält kein einziges DELETE auf `Tab_Energieanlagen`).
Solution-Build x64 Debug: 0 Fehler.

## Grenze

Bereits beschädigte Bestandsprojekte (1027, 1009) werden nicht repariert — ihr Puffer ist
weg, der Widerspruch `WS_Ziel = 'PufferHeizung'` ohne `WS_ID_Puffer` bleibt, bis der
Anwender den Speicher neu zuordnet (Puffer-Karte → Speicher anlegen/wählen, dann Senkenziel
neu setzen). Eine automatische Reparatur wäre Raterei über verlorene Gerätedaten.

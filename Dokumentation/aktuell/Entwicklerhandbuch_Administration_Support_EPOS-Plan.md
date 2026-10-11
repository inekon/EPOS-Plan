# Entwicklerhandbuch: Administration und Support in EPOS-Plan

**Für wen.** INEKON — Entwicklung und Support. Nicht für Anwender; nichts aus diesem Handbuch kommt
ins Wiki, und die Entwicklerbereiche werden in der Anwenderdokumentation nicht erwähnt.

**Stand.** 11.10.2026, Stufen 1 und 2 umgesetzt (11.10.2026, #974 und #975), Stufe 3 (3a Über und Diagnose, 3b Supportpaket) umgesetzt (11.10.2026, #977), Nachzug Zapfprofil-Rubrik umgesetzt (11.10.2026, #976); Konzeptstand nach den Anwenderentscheiden AS-Q1 bis AS-Q10
([`Konzept_Administration_Support_EPOS-Plan.md`](Konzept_Administration_Support_EPOS-Plan.md)).
Jeder Abschnitt trägt die Stufe, ab der er gilt: **[heute]** vorhanden, **[Stufe 1]**
Entwicklermodus, **[Stufe 2]** Fernwartung, **[Stufe 3]** Dialog „Über EPOS-Plan" und
Diagnosedaten. Wer eine Stufe umsetzt, zieht dieses Handbuch und die Word-Fassung daneben nach
(Abschnitt 8).

**Quelle und Word-Fassung.** Diese Markdown-Datei ist die Quelle; die Datei
`Entwicklerhandbuch_Administration_Support_EPOS-Plan.docx` im selben Ordner ist daraus erzeugt und
wird mit jeder Änderung neu erzeugt (Abschnitt 8).


## 1 Entwicklermodus ein- und ausschalten [heute]

Der Entwicklermodus ist eine Sichtbarkeitsstufe, kein Rechtemodell: Er zeigt Bereiche, die ein
Anwender nicht sieht. Was er zeigt, steht in Abschnitt 2.

### 1.1 Die vier Wege

| Weg | Wann | So geht es | Wieder aus |
|---|---|---|---|
| **Tastenfolge** | am Kundenrechner oder am eigenen Rechner, dauerhaft | *Hilfe → Version* öffnen; im Dialog „Über EPOS-Plan" **Strg + Umschalt + Alt + E**, Rückfrage „Entwicklermodus einschalten? Er gilt bis zum Ausschalten." bestätigen. Im Hauptfenster wirkt die Tastenfolge nicht. Der Diagnoseblock des Dialogs zeigt danach „Entwickler (Einstellung)". iPad: nicht gebaut (geplant: fünfmal auf die Versionszeile tippen). | Menü *Entwicklung → Entwicklermodus ausschalten* oder dieselbe Tastenfolge im Dialog |
| **Startargument** | ein einzelner Lauf, Vorführung, Fehlersuche in einer Fernwartungssitzung | `EPOS_Plan.exe --entwickler`. Praktisch: eine Verknüpfung „EPOS-Plan (Entwickler)" mit diesem Ziel selbst anlegen; das Setup legt sie absichtlich nicht an. Nur Windows. | nächster Start ohne Argument |
| **Umgebungsvariable** | Entwicklerrechner dauerhaft, CI, Rasterprobe, Referenzlauf, iOS-Simulator | `EPOS_ENTWICKLER=1` als Benutzer- oder Systemvariable. iOS-Simulator wie beim Prüfmodus: `SIMCTL_CHILD_EPOS_ENTWICKLER=1`. | Variable entfernen |
| **Sperre des Administrators** | Kundeninstallation, deren Verwaltung den Modus verbietet | Registrywert `HKLM\SOFTWARE\wp-plan\EntwicklermodusGesperrt = 1` (in beiden Registry-Sichten gelesen, wie der KI-Abschalter). Die Sperre schlägt alle drei anderen Wege. | Wert löschen; aus der Anwendung heraus nie |

Rangfolge beim Start: erst die Sperre, dann Umgebungsvariable, Startargument, Einstellung. Die
Einstellung der Tastenfolge liegt unter Windows in `HKCU\Software\wp-plan\Entwicklermodus` (Wert 1)
und auf iOS in den `Preferences` unter `wp-plan.Entwicklermodus`. Der Kern-Baustein
`Entwicklermodus` (`EPOS.Kern/Allgemein/Freigabe/`) liest alles einmal beim Start und bietet
`Stufe`, `Aktiv`, `Quelle` und `Gesperrt`; niemand sonst liest Registry, Umgebung oder Startzeile.

### 1.2 Woran man den Modus erkennt

- Die Kopfleiste trägt rechts neben der Versionsnummer die Marke **„Entwickler"** in der Warnfarbe;
  der Tooltip nennt die Quelle (Einstellung, Startargument, Umgebung).
- Der Fenstertitel trägt den Zusatz „(Entwicklermodus)".
- Vor dem Menükopf *Sprache* steht der Kopf **„Entwicklung"** (Abschnitt 2.2).

### 1.3 Ausschalten

*Entwicklung → Entwicklermodus ausschalten* oder die Tastenfolge im Dialog „Über EPOS-Plan" löscht
die Einstellung. Läuft der Modus über
Startargument oder Umgebungsvariable, sagt der Punkt das und tut nichts: dann ohne Argument neu
starten beziehungsweise die Variable entfernen.

### 1.4 Regeln am Kundenrechner

- Vor dem Verlassen eines Kundenrechners den Modus ausschalten und die Marke prüfen. Eine
  Kundeninstallation läuft nie unbemerkt im Entwicklermodus.
- Die Sperre des Administrators wird nicht umgangen. Braucht der Support den Modus dort, klärt er
  es mit der Verwaltung des Kunden.
- Was im Modus angelegt wird (Projektdatei-Importe, Profile aus einer Projektdatei), bleibt ohne
  Modus lesbar und trägt seine Herkunft; nur das Anlegen neuer Importe ist dann verborgen (AS-Q5).


## 2 Die Entwicklerbereiche [heute]

Das Register `Entwicklerbereiche.Alle` im Kern führt jeden Bereich mit einem Schlüssel; die Wache
(Abschnitt 5) prüft, dass jeder Schlüssel eine Fundstelle hat und dass der Anwenderbaum keinen davon
enthält.

### 2.1 Die Liste

| Schlüssel | Bereich | Wo | Wozu |
|---|---|---|---|
| `PROJEKTDATEI_IMPORT` | Projektdatei des CAD-Programms (`.sqproj`) im Gebäudeimport: Quellenwahl „Format mit Projektdatei" und „nur Projektdatei", Knopf „Projektdatei dazuladen (.sqproj)…", „Projektdatei entfernen", Zonierungswahl DIN/Simulation | `EPOS.UI/Dialoge/Import/GebaeudeImportDialog.razor`, Hülle `EPOS.UI.Daten/Bedarf/GebaeudeImportHuelle.cs` (Parameter `ProjektdateiFreigegeben`), Kern `EPOS.Kern/Allgemein/Import/Sqproj/` (Datenaustauschkonzept Kapitel 16) | Zonen, Nutzung, Ganglinien und Kalender aus HottCAD zur IFC-Datei dazuladen |
| `PROJEKTDATEI_PROFILE` | „Aus Projektdatei…" im Blatt Nutzungsprofile und „Nutzungsprofile der Datei übernehmen…" im Import | `EPOS.UI/Dialoge/Bedarf/ProjektdateiProfile.razor`, Wirt `RaumnutzungBlatt.razor`, Hülle `ProjektdateiProfileHuelle.cs`; Parameter `RaumnutzungWeg.ProjektdateiFreigegeben` | Profile je DIN-Nummer in einer eigenen Katalogkategorie anlegen |
| `PROJEKTDATEI_IOS` | Dateiendung `.sqproj` in der iOS-Dateiwahl | `EPOS.iOS/Dienste/Dateifilter.cs` (ohne Modus ohne `.sqproj`; nicht gebaut) | nur im Modus wählbar |
| `PROJEKTDATEI_KI` | Feldkarten und Aktionen der Projektdatei für den Hilfe-Assistenten | `EPOS.Kern/Allgemein/KI/Dialoge/KiDialoge.cs` (`KiDialoge.NUR_ENTWICKLER`), Absage `KI_ABSAGE_NUR_ENTWICKLER` in `KiAktionenDialog.FeldzugangGrund` | ohne Modus benannte Absage des Assistenten |
| `KATALOG_WIEDERHERSTELLEN` | „Auslieferungsstand wiederherstellen…" des Katalogabgleichs | `KatalogabgleichDialog.razor` (Parameter `Entwickler`), Kern `Katalogabgleich.Wiederherstellen`; im Modus über *Entwicklung* | Reparatur eines behaltenen Katalogsatzes |
| `KI_WERKZEUGE` | KI-Chat: „Werkzeuge…", „Protokoll anzeigen", Modellwahl und „Weg B" in den KI-Einstellungen | `EPOS.UI/Dialoge/Hilfe/KiChatDialog.razor`, `KiEinstellungenDialog.razor` (je Parameter `Entwickler`) | Prüf- und Fehlersuchwerkzeuge des Assistenten; „Was wird gesendet?" bleibt für alle |
| `EXPORT_IDS` | „Exportzusage (IDS) öffnen…" im Gebäudeexport | `EPOS.UI/Dialoge/Export/GebaeudeExportDialog.razor` (Parameter `ZusageFreigegeben`) | Prüfartefakt des IFC-Exports |
| `WEBVIEW_WERKZEUGE` | Entwicklerwerkzeuge der WebView2 (Strg + Umschalt + I) | `WindowsFormsApplication1/Allgemein/Blazor/BlazorDienste.cs` (`WebviewWerkzeugeFreigegeben`: Debug-Build oder Modus; `AddBlazorWebViewDeveloperTools`) | DOM, Stilblatt, Konsole, Netz der Razor-Oberfläche im Release beim Kunden |

Bewusst **kein** Entwicklerbereich: Katalog-Dublettensuche, Schloss eines Auslieferungssatzes,
Zapfprofil-Stufe „Experte", Gebäude-Rechenweg „Tagesbilanz", „Was wird gesendet?" im KI-Chat.
Bewusst **Build-Schalter** bleiben die elf Prüfhaken `#if DEBUG` unter
`EPOS.Kern/Allgemein/Simulation/` (sie verändern den Rechenweg).
Ebenso kein Entwicklerbereich sind die fünf Rechenvorgaben des Zapfprofils (`Zapfprofil.Anzeigetemperatur`,
`Zapfprofil.Stundenschwelle`, `Zapfprofil.Nenninhalte`, `Zapfprofil.Katalogordner`, `Zapfprofil.Importordner`): Sie
stehen für alle Anwender in der Rubrik „Zapfprofil" des Einstellungsdialogs (`EinstellungenDialog.razor`, Reiterblatt
`ZAPFPROFIL`; Felder `Zapf*` in `EinstellungenCtrl`).

### 2.2 Der Menükopf „Entwicklung"

| Punkt | Tut | Hinweis |
|---|---|---|
| Ausnahmeprotokoll öffnen | öffnet `Logs\Ausnahmen.txt` mit dem Standardprogramm | erster Handgriff bei einem wortlosen Absturz (Abschnitt 3.2) |
| Protokollordner öffnen | Explorer auf `Logs\` | Ausnahmen, Migration, KI-Aktionen |
| Datenbankordner öffnen | Explorer auf `%ProgramData%\EPOS_PLAN` | Datenbank, `DB-Backup`, Vorlage |
| Diagnosedaten kopieren | Textblock aus Abschnitt 3.3 in die Zwischenablage | heute schon der Kern-Baustein `Diagnosedaten`; derselbe Block erscheint später im Dialog „Über EPOS-Plan" |
| Entwicklerwerkzeuge der Oberfläche | WebView2-Entwicklerwerkzeuge | ersetzt `#if DEBUG` |
| Auslieferungsstand wiederherstellen… | Reparaturzweig des Katalogabgleichs | aus *Daten & Import* hierher verlegt |
| Entwicklermodus ausschalten | löscht die Einstellung | bei Startargument oder Umgebung benannte Absage |

### 2.3 Einen neuen Entwicklerbereich anlegen

1. Schlüssel in `Entwicklerbereiche.Alle` eintragen, mit einem Satz, wozu der Bereich dient.
2. Menüpunkt: Zeile in `Menuetabelle.cs` mit `stufe: Freigabestufe.Entwickler`. Knopf oder Feld in
   einem Dialog: Kennzeichen in den Gaben der Hülle (Muster `GebaeudeImportGaben.ProjektdateiFreigegeben`,
   gesetzt aus `Entwicklermodus.Aktiv`), im Razor nur dann rendern, Element mit
   `data-stufe="entwickler"` kennzeichnen. Keine Razor-Komponente fragt den Kern selbst (Regel S‑2).
3. Hat der Bereich eine Feldkarte oder Aktion des Hilfe-Assistenten: `nurEntwickler` setzen.
4. iOS: Ziel in `AppWurzel.FuehrtZiel` nur im Modus; Dateiendungen in `Dateifilter` je Stufe.
5. Wache laufen lassen (Abschnitt 5). Kein Wiki-Eintrag, kein Logbuch.


## 3 Protokolle und Diagnose

### 3.1 Die Dateien [heute]

| Datei oder Tabelle | Ort | Verhalten |
|---|---|---|
| `Ausnahmen.txt`, `Ausnahmen.alt.txt` | `%ProgramData%\EPOS_PLAN\Logs\` (Rückfall `%TEMP%\EPOS_PLAN\Logs\`) | jede ausgelöste .NET-Ausnahme sofort mit Aufrufstapel (`AppDomain.FirstChanceException`), dazu unbehandelte Ausnahmen und unbeobachtete Aufgaben; höchstens 20 Einträge je Sekunde, Wechsel bei 200 KB, nie mehr als zwei Dateien; Kopfzeile je Lauf mit Version und Prozess-Id. Nur Windows (`Program.Main`). |
| `migration_protokoll.txt` | neben der Datenbank | je Start überschrieben; bei Fehlschlag nennt die Startmeldung den Pfad |
| `ki_aktionen.txt` | neben der Datenbank (das Supportpaket sucht zuerst in `Logs\`, dann daneben) | Aktionen des Hilfe-Assistenten, wächst ohne Rotation |
| `Tab_Katalogabgleich` | in der Datenbank | eine Zeile je Abgleichaktion |
| `<paket>.importbericht.txt` | neben dem Projektpaket | nur beim Projektimport |

### 3.2 Absturz ohne Meldung [heute]

Ereignisanzeige mit `KERNELBASE.dll` und `0xc000041d`: Eine Ausnahme ist aus einem Rückruf der
WebView2 herausgelaufen. Die Ursache steht in `Logs\Ausnahmen.txt` — der letzte Eintrag vor dem
Kopf des nächsten Laufs. Diese Datei anfordern, nicht den Weg raten (Fallstricke in
`WindowsFormsApplication1/CLAUDE.md`).

### 3.3 Diagnosedaten und Supportpaket [heute]

Den Diagnoseblock findet der Anwender unter *Hilfe → Version* im Dialog „Über EPOS-Plan" (unter dem
Kopf mit Produktname, Claim, Version mit Baudatum und INEKON) als nur lesbares Textfeld; ebenso in
*Hilfe → Fernwartung…* (Zone „Ihre Angaben") und für Entwickler unter *Entwicklung → Diagnosedaten
kopieren*.

Der Dialog „Über EPOS-Plan" zeigt den Diagnoseblock aus dem Kern-Baustein
`Diagnosedaten` (`EPOS.Kern/Allgemein/Diagnose/`): Programmversion und Baudatum, Plattform und
Betriebssystemfassung, Sprache, Datenbankpfad, Schemastand (`Tab_Applikation.SchemaVersion` gegen
`SchemaStand.Zielversion`), Katalogfassung, Lizenztyp, Lizenznummer, Firma, Status, gültig bis,
Gerätekennung-Kurzform (erste acht Zeichen des Abdrucks), Protokollordner, Entwicklermodus mit
Quelle. Knöpfe unter dem Block: „Diagnosedaten kopieren" (Zwischenablage) und „Protokollordner
öffnen" (Windows; der Explorer im Ordner `Logs` neben der Datenbank, `Diagnosedaten.ProtokollordnerPfad`;
fehlt der Ordner, sagt eine Meldung den Pfad). Darunter der Kontakt (Telefon, E-Mail als
`mailto:`-Link, Erreichbarkeit) mit dem Knopf „Fernwartung…", der den Fernwartungsdialog öffnet.

**Supportpaket.** Unter dem Diagnoseblock stehen der Haken „KI-Protokoll beilegen" (Vorgabe aus,
mit dem Hinweis, dass das Protokoll Eingaben des Anwenders an den Assistenten enthalten kann) und
der Knopf „Supportpaket speichern…". Unter Windows öffnet er eine Nachricht später den
Speicherdialog im Dokumentenordner mit dem Vorgabenamen `EPOS-Plan_Supportpaket_<yyyy-MM-dd_HHmm>.zip`
und dem Filter ZIP; Abbruch schreibt nichts. Danach zeigt der Dialog eine Ergebniszeile mit Pfad,
enthaltenen Einträgen und den Quellen, die es nicht gab — oder im Banner die Meldung des
Schreibfehlers. Der Anwender hängt die Datei an seine E-Mail; das ist der Supportweg ohne
Fernwartung und steht auch der Testversion offen.

| Eintrag im Paket | Quelle |
|---|---|
| `Diagnosedaten.txt` | der Diagnoseblock, wie der Dialog ihn zeigt |
| `Ausnahmen.txt`, `Ausnahmen.alt.txt` | Protokollordner `Logs\` neben der Datenbank (`Diagnosedaten.ProtokollordnerPfad`) |
| `migration_protokoll.txt` | neben der Datenbank (Rückfall `Logs\`) |
| `Katalogabgleich.txt` | die jüngsten 2 000 Zeilen von `Tab_Katalogabgleich`, neueste zuerst, tabulatorgetrennt mit Spaltenköpfen (über `Katalogabgleich.Protokoll`, kein eigenes SQL) |
| `ki_aktionen.txt` | nur mit Haken; `Logs\`, sonst neben der Datenbank |

Fehlt eine Quelle (kein Absturz bisher, keine Migration, Tabelle nicht vorhanden), ist das kein
Fehler: Sie steht in der Ergebniszeile unter „Nicht vorhanden". **Datenschutz:** Das Paket nimmt
nur diese benannten Einträge auf — nie Projektdaten, nie die Datenbank oder eine andere
`.sqlite`-Datei; die Diagnosedaten nennen Lizenznummer, Firma und Datenbankpfad, wie der Block im
Dialog. Das KI-Protokoll kann Eingaben des Anwenders enthalten und kommt deshalb nur mit dem Haken
hinein. EPOS-Plan versendet das Paket nicht selbst.

### 3.4 Wo der Support sonst nachsieht [heute]

| Ort | Inhalt |
|---|---|
| `HKLM\SOFTWARE\INEKON\EPOS-Plan` | `InstallDir`, `Version` (vom Setup) |
| `HKCU\Software\wp-plan` | Sprache, KI-Werte, Einwilligung, Lizenzanker, `Entwicklermodus` |
| `HKLM\SOFTWARE\wp-plan` | maschinenweite Sperren: `KiDeaktiviert`, `EntwicklermodusGesperrt` |
| `user.config` der Anwendung | `DBPath`, `DBName`, Web-Adressen, Pfade (Einstellungsdialog) |
| `%APPDATA%\wp-plan\` | `lizenz.dat`, `lizenz-zeit.dat`, `ki-schluessel.dat` (DPAPI) |
| `%ProgramData%\EPOS_PLAN\` | `Kenndaten.sqlite`, `DB-Backup\`, `Logs\` |
| `%LOCALAPPDATA%\WP-Plan\WebView2` | Daten der WebView2 |

Nachstellen eines Falls ohne Fernwartung: Projektpaket (`.wpx`) vom Kunden erbitten, bei INEKON
importieren, mit dem Referenzlauf rechnen.


## 4 Fernwartung aus Supportsicht [Stufe 2]

### 4.1 Das Werkzeug

RustDesk mit einem Vermittlungsserver, den INEKON selbst betreibt (Entscheid AS-Q7). Platzhalter in
diesem Handbuch: Server `fernwartung.epos-plan.de`, Fernwartungsseite
`https://epos-plan.de/support/fernwartung`, Client-Datei `EPOS-Plan-Fernwartung.exe`. Die echten
Werte, den Serverschlüssel und die Zugangsdaten der Web-Konsole verwahrt INEKON außerhalb des
Repositoriums; sie stehen nie in diesem Handbuch und nie im Quelltext.

### 4.2 Was der Anwender tut

1. *Hilfe → Fernwartung…* öffnet den Dialog: Kontakt, die Angaben für den Support (Abschnitt 3.3),
   der Erklärtext und der Knopf „Fernwartungsseite öffnen". Ohne bezahlte Lizenz ist der Knopf
   gesperrt, der Grund steht dabei (Abschnitt 4.4). Telefon und E-Mail (Link) stehen im Dialog immer.
2. Auf der Fernwartungsseite lädt er `EPOS-Plan-Fernwartung.exe` und startet sie; keine
   Installation, keine Administratorrechte. Bis INEKON ein Codesignaturzertifikat hat, erscheint der
   SmartScreen-Hinweis „Unbekannter Herausgeber"; die Seite erklärt „Weitere Informationen →
   Trotzdem ausführen".
3. Das Programm zeigt eine Kennung und ein Einmalkennwort. Der Anwender nennt beides dem Support am
   Telefon und nimmt die eingehende Verbindung im Programm an.
4. Er beendet die Sitzung im Programm oder schließt es. Es bleibt nichts installiert.

### 4.3 Was der Support tut

1. RustDesk in der Vollfassung am Support-Arbeitsplatz, auf INEKONs Server eingestellt; Kennung des
   Kunden eingeben, Einmalkennwort eintragen, auf die Annahme warten.
2. In der Sitzung bei Bedarf den Entwicklermodus einschalten: `--entwickler` über eine Verknüpfung
   oder die Tastenfolge aus Abschnitt 1.1. **Vor dem Ende der Sitzung wieder ausschalten.**
3. Soll ein Update eingespielt werden, braucht das Setup Administratorrechte. Die Windows-Abfrage
   sieht der Support nur, wenn der Fernwartungs-Client mit Administratorrechten läuft; sonst muss
   der Anwender sie selbst bestätigen. Vorher mit dem Anwender absprechen.
4. Ob der Anwender die Fernwartung nutzen darf, prüft der Support an `LizenzCtrl.FernwartungErlaubt()` (Erlaubt, Grund) oder am Menüpunkt: ist er gesperrt, steht der Grund im Tooltip. Ist die Lage unklar, lässt er sich den Diagnoseblock aus dem Dialog (Knopf „Kopieren“) per E-Mail schicken. Führt der Knopf ins Leere, fehlt die Fernwartungsseite `Fernwartung.SEITE` (`EPOS.Kern/Allgemein/Support/Fernwartung.cs`) bei INEKON: Seite und Client prüfen (4.6) und bis dahin den Weg Telefon und E-Mail nehmen.
5. Nach der Sitzung: kein Eintrag in EPOS-Plan; das Sitzungsprotokoll der Web-Konsole (wer wann mit
   wem) ist die einzige Aufzeichnung.

### 4.4 Die Lizenzbedingung (AS-Q8, Lesart b)

Der Knopf ist frei, wenn ein Lizenz-Token vom Typ `person` oder `firma` vorhanden ist — gleich ob
gültig, in Kulanz oder abgelaufen (Lesemodus). Gesperrt bleibt er in der Testversion (`demo`, Grund `FW_SPERRE_TEST`), ohne
Aktivierung (`NichtAktiviert`, Grund `FW_SPERRE_LIZENZ`), bei manipulierter Uhr (`UhrManipuliert`), bei unbekanntem Lizenztyp und bei einem Token eines fremden Geräts. Für diese Fälle gilt
Telefon und E-Mail mit Diagnosedaten; eine Testversion bekommt keine Fernwartung. Die Frage stellt
`LizenzCtrl.FernwartungErlaubt()` (ohne Nebenwirkung, liefert `Fernwartungsfreigabe(Erlaubt, Grund)`); ein Supportvertrag im Token (`support_bis`) ist Stufe 4
und ändert die Regel erst dann.

### 4.5 iPad

Zurückgestellt (Anwender 11.10.2026: „wird später bearbeitet"). Stufe 2 baut die Fernwartung für
Windows; auf dem iPad gibt es weder Menüpunkt noch Dialog, bis der iPad-Weg entschieden ist.
Befund für dann: Ein iPad lässt sich mit RustDesk weder fernsteuern noch ansehen. Der Supportweg
vom iPad ist bis dahin Telefon und E-Mail.

### 4.6 Betrieb bei INEKON

| Aufgabe | Wie |
|---|---|
| Server prüfen | `hbbs` und `hbbr` laufen (Docker), Ports 21115 bis 21119 TCP und 21116 UDP erreichbar; nach einem Serverupdate eine Probesitzung zwischen zwei INEKON-Rechnern |
| Clientfassung tauschen | neuen Client im Generator der Web-Konsole bauen, mit INEKONs Zertifikat signieren, Datei auf epos-plan.de ersetzen, Fernwartungsseite prüfen; der alte Client auf Kundenrechnern ist eine Einzeldatei und wird beim nächsten Fall neu geladen |
| Schlüssel oder Servername ändern | neuer Client nötig (Server und Schlüssel sind fest eingebaut); alte Clients verbinden sich dann nicht mehr |
| Ohne Pro-Tarif | quelloffener Client mit Serverangabe im Dateinamen `rustdesk-host=<server>,key=<schlüssel>.exe` oder Startargument `--config`; ohne Logo, Adressbuch und Sitzungsprotokoll |


## 5 Wachen und Tests

| Wache | Hält | Stufe |
|---|---|---|
| `EPOS.UI.Tests/Bausteine/EntwicklerbereicheWacheTests` (Teil 1) | Anwenderbaum ohne Entwicklerpunkt, keine leeren Untermenüs, keine doppelten Trenner; jeder Schlüssel des Registers hat eine Fundstelle; jedes `data-stufe="entwickler"` hängt an einem Gaben-Kennzeichen | heute |
| `EPOS.UI.Tests/Bausteine/EntwicklerbereicheQuellenWacheTests` (Teil 2), `KiEntwicklerbereichTests` | Quellen der Elemente tragen `data-stufe`/`data-bereich` und hängen am Gaben-Kennzeichen; KI-Feldkarten und Absage ohne Modus | heute |
| `MenuebandTests` | Zählungen beider Bäume, Regel „kein Untermenü mit nur einem Punkt" | heute |
| bunit `GebaeudeImportDialog`, `RaumnutzungBlatt`, `HauptfensterTests` (Marke, Menü) | ohne Kennzeichen kein Projektdatei-Element, mit Kennzeichen alle | heute |
| `EntwicklermodusTests` (Kern) | Sperre schlägt alles; Quelle richtig benannt | heute |
| `LizenzCtrlTests` | `FernwartungErlaubt` für die vier Lesarten | 2 |
| bunit `FernwartungDialog` | gesperrt und frei; Knopf ruft die Adresse | 2 |
| `DiagnosedatenTests`, bunit `UeberDialog` | Textblock vollständig; Geste schaltet | 3 |
| `SupportpaketTests` (Kern), bunit `UeberDialog` | Paket mit Diagnosedaten, vorhandene Protokolle drin, fehlende benannt, KI-Protokoll nur mit Haken, nie `.sqlite`, Schreibfehler benannt; ohne Rückruf kein Knopf, Klick übergibt den Haken, Ergebniszeile | 3 |

Laufen wie üblich über den Kern-Filter:

```powershell
dotnet build WP-Plan.Kern.slnf -c Release -nologo -v q -clp:ErrorsOnly
dotnet test  WP-Plan.Kern.slnf -c Release --no-build --filter "FullyQualifiedName~Entwicklerbereiche" -- xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2
```

Die Windows-Schale (Startargument, Fenster, Explorer-Hüllen) baut auch auf Linux:

```powershell
dotnet build WindowsFormsApplication1/WindowsFormsApplication1.csproj -c Debug -p:Platform=x64 -p:EnableWindowsTargeting=true
```


## 6 Fundstellen im Code

| Baustein | Pfad | Stufe |
|---|---|---|
| Entwicklermodus, Freigabestufe, Register | `EPOS.Kern/Allgemein/Freigabe/Entwicklermodus.cs`, `Entwicklerbereiche.cs` | heute |
| Menüzeile mit Stufe, Zuschnitt | `EPOS.UI/Bausteine/Menuepunkt.cs`, `Menuetabelle.cs` (`Sichtbar`) | heute |
| Marke und Fenstertitel | `EPOS.UI/Seiten/Hauptfenster.razor`, `WindowsFormsApplication1/Views/Hauptformular/` | heute |
| Startargument | `WindowsFormsApplication1/Program.cs` (`Main(string[] args)`) | heute |
| Projektdatei-Einstiege | siehe Abschnitt 2.1 | heute |
| Ausnahmeprotokoll | `EPOS.Kern/Allgemein/Diagnose/Ausnahmeprotokoll.cs`, eingeschaltet in `Program.Main` | heute |
| Lizenzstand | `EPOS.Kern/Allgemein/Lizenz/LizenzManager.cs` (`LizenzStatus`), `LizenzToken.cs` (`Typ`), `EPOS.Kern/Controller/LizenzCtrl.cs` | heute |
| Adresse öffnen | `EPOS.Kern/Allgemein/Dienste/IDateiDienst.cs` (`AdresseOeffnen`), `WindowsFormsApplication1/Dienste/WindowsDateiDienst.cs`; der iOS-Adapter liefert heute die Vorgabe `false` | heute |
| Fernwartungsdialog | `EPOS.UI/Dialoge/Hilfe/FernwartungDialog.razor`, `FernwartungDaten.cs` (mit `FernwartungTexte`), Hülle `EPOS.UI.Daten/Hilfe/FernwartungHuelle.cs`, Fenster `WindowsFormsApplication1/Views/Help/FernwartungFenster.cs` | 2 |
| Fernwartungsregel, Adresse | `EPOS.Kern/Allgemein/Support/Fernwartung.cs` (`SEITE`, `Fernwartungsfreigabe`), `EPOS.Kern/Controller/LizenzCtrl.cs` (`FernwartungErlaubt`), `LizenzManager.AnkerOhneSchreiben` | 2 |
| Menüsperre | `EPOS.UI/Bausteine/Menuepunkt.cs` (`Sperrgrund`), `Menuetabelle.cs` (Punkt `MenuItem_Fernwartung`), `Menueband.razor`, `HauptfensterHuelle.cs` (Fall `Fernwartung`) | 2 |
| Tests der Fernwartung | `EPOS.Kern.Tests/FernwartungFreigabeTests.cs`, `EPOS.UI.Tests/Dialoge/FernwartungDialogTests.cs`, `MenuebandTests` | 2 |
| Wiki-Quelle | `Projekte/Wiki/Programm Dokumentation - Fernwartung.wiki` | 2 |
| Über-Dialog | `EPOS.UI/Dialoge/Hilfe/UeberDialog.razor`, `UeberDaten.cs` (mit `UeberAnstoss`, `UeberTexte`), Hülle `EPOS.UI.Daten/Hilfe/UeberHuelle.cs`, Fenster `WindowsFormsApplication1/Views/Help/UeberFenster.cs`, Fall `Version` in `HauptfensterHuelle.cs` | heute |
| Geste des Entwicklermodus | `UeberFenster` (Abfang über `BlazorDialogForm.Befehlstaste`), `WindowsFormsApplication1/Views/Entwicklung/EntwicklungHuelle.cs` (`GesteUmschalten`) | heute |
| Diagnosedaten | `EPOS.Kern/Allgemein/Diagnose/Diagnosedaten.cs` (`Textblock`, `ProtokollordnerPfad`) | heute |
| Tests des Über-Dialogs | `EPOS.UI.Tests/Dialoge/UeberDialogTests.cs`, `EPOS.Kern.Tests/DiagnosedatenTests.cs` | heute |
| Supportpaket | `EPOS.Kern/Allgemein/Support/Supportpaket.cs` (`Erstellen`, `Vorgabename`, `Supportpaketergebnis`), Anzeigeform `SupportpaketAnzeige` in `EPOS.UI/Dialoge/Hilfe/UeberDaten.cs`, Hülle `UeberHuelle.SupportpaketSpeichern`, Speicherdialog in `UeberFenster.SupportpaketSpeichern`, Tests `EPOS.Kern.Tests/SupportpaketTests.cs`; der Haken steht in `KiDialogAusnahmen` (Grund Export) | heute |
| Maschinenweite Werte | `EPOS.Kern/Allgemein/Dienste/IEinstellungen.cs` (`LiesMaschine`), `WindowsFormsApplication1/Dienste/RegistryEinstellungen.cs` | heute |


## 7 Stufenstand

| Stufe | Inhalt | Stand |
|---|---|---|
| 1 | Entwicklermodus, Entwicklerbereiche, Menükopf „Entwicklung", Projektdatei verborgen | umgesetzt (11.10.2026, #974); die Zapfprofil-Vorgaben sind Anwenderrubrik „Zapfprofil" statt Entwicklerbereich (Nachzug 11.10.2026, #976) |
| 2 | Fernwartung unter Windows: Menüpunkt, Dialog, Lizenzbedingung, Fernwartungsseite; Zuarbeit INEKON: Server, Client, Seite, Signatur. Das iPad ist zurückgestellt | umgesetzt (11.10.2026, #975); offen beim Anwender: Fernwartungsseite, Server, signierter Client |
| 3 | Dialog „Über EPOS-Plan", Diagnosedaten, Supportpaket | umgesetzt (11.10.2026, #977): 3a (Dialog, Diagnoseblock, Geste im Dialog), 3b (Supportpaket) |
| 4 | Supportvertrag im Lizenz-Token (`support_bis`) | offen, auf Zuruf |


## 8 Pflege dieses Handbuchs

- Die Markdown-Datei ist die Quelle. Wer eine Stufe umsetzt, trägt die Stufe in Abschnitt 7 als
  umgesetzt ein, ersetzt in den betroffenen Abschnitten „[Stufe n]" durch „[heute]" und berichtigt
  Namen und Pfade nach dem gebauten Stand.
- Danach wird die Word-Fassung neu erzeugt (Agent mit dem docx-Skill, Vorlage: dieses Dokument,
  A4, Überschriften und Tabellen wie hier) und im selben Commit abgelegt.
- Nichts aus diesem Handbuch wird ins Wiki übernommen; die Anwenderseite der Fernwartung steht in
  den Wiki-Quellen unter `Projekte/Wiki/` ohne Produktnamen.

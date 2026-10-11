# Entwicklerhandbuch: Administration und Support in EPOS-Plan

**Für wen.** INEKON — Entwicklung und Support. Nicht für Anwender; nichts aus diesem Handbuch kommt
ins Wiki, und die Entwicklerbereiche werden in der Anwenderdokumentation nicht erwähnt.

**Stand.** 11.10.2026, Konzeptstand nach den Anwenderentscheiden AS-Q1 bis AS-Q10
([`Konzept_Administration_Support_EPOS-Plan.md`](Konzept_Administration_Support_EPOS-Plan.md)).
Jeder Abschnitt trägt die Stufe, ab der er gilt: **[heute]** vorhanden, **[Stufe 1]**
Entwicklermodus, **[Stufe 2]** Fernwartung, **[Stufe 3]** Dialog „Über EPOS-Plan" und
Diagnosedaten. Wer eine Stufe umsetzt, zieht dieses Handbuch und die Word-Fassung daneben nach
(Abschnitt 8).

**Quelle und Word-Fassung.** Diese Markdown-Datei ist die Quelle; die Datei
`Entwicklerhandbuch_Administration_Support_EPOS-Plan.docx` im selben Ordner ist daraus erzeugt und
wird mit jeder Änderung neu erzeugt (Abschnitt 8).


## 1 Entwicklermodus ein- und ausschalten [Stufe 1]

Der Entwicklermodus ist eine Sichtbarkeitsstufe, kein Rechtemodell: Er zeigt Bereiche, die ein
Anwender nicht sieht. Was er zeigt, steht in Abschnitt 2.

### 1.1 Die vier Wege

| Weg | Wann | So geht es | Wieder aus |
|---|---|---|---|
| **Tastenfolge** | am Kundenrechner oder am eigenen Rechner, dauerhaft | *Hilfe → Version* öffnen (ab Stufe 3 heißt der Dialog „Über EPOS-Plan"), dann **Strg + Umschalt + Alt + E**, Rückfrage „Entwicklermodus einschalten? Er gilt bis zum Ausschalten." bestätigen. iPad: fünfmal auf die Versionszeile tippen. | Menü *Entwicklung → Entwicklermodus ausschalten* |
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

*Entwicklung → Entwicklermodus ausschalten* löscht die Einstellung. Läuft der Modus über
Startargument oder Umgebungsvariable, sagt der Punkt das und tut nichts: dann ohne Argument neu
starten beziehungsweise die Variable entfernen.

### 1.4 Regeln am Kundenrechner

- Vor dem Verlassen eines Kundenrechners den Modus ausschalten und die Marke prüfen. Eine
  Kundeninstallation läuft nie unbemerkt im Entwicklermodus.
- Die Sperre des Administrators wird nicht umgangen. Braucht der Support den Modus dort, klärt er
  es mit der Verwaltung des Kunden.
- Was im Modus angelegt wird (Projektdatei-Importe, Profile aus einer Projektdatei), bleibt ohne
  Modus lesbar und trägt seine Herkunft; nur das Anlegen neuer Importe ist dann verborgen (AS-Q5).


## 2 Die Entwicklerbereiche [Stufe 1]

Das Register `Entwicklerbereiche.Alle` im Kern führt jeden Bereich mit einem Schlüssel; die Wache
(Abschnitt 5) prüft, dass jeder Schlüssel eine Fundstelle hat und dass der Anwenderbaum keinen davon
enthält.

### 2.1 Die Liste

| Schlüssel | Bereich | Wo | Wozu |
|---|---|---|---|
| `PROJEKTDATEI_IMPORT` | Projektdatei des CAD-Programms (`.sqproj`) im Gebäudeimport: Quellenwahl „Format mit Projektdatei" und „nur Projektdatei", Knopf „Projektdatei dazuladen (.sqproj)…", „Projektdatei entfernen", Zonierungswahl DIN/Simulation | `EPOS.UI/Dialoge/Import/GebaeudeImportDialog.razor`, Hülle `EPOS.UI.Daten/Bedarf/GebaeudeImportHuelle.cs`, Kern `EPOS.Kern/Allgemein/Import/Sqproj/` (Datenaustauschkonzept Kapitel 16) | Zonen, Nutzung, Ganglinien und Kalender aus HottCAD zur IFC-Datei dazuladen |
| `PROJEKTDATEI_PROFILE` | „Aus Projektdatei…" im Blatt Nutzungsprofile und „Nutzungsprofile der Datei übernehmen…" im Import | `EPOS.UI/Dialoge/Bedarf/ProjektdateiProfile.razor`, Wirt `RaumnutzungBlatt.razor`, Hülle `ProjektdateiProfileHuelle.cs` | Profile je DIN-Nummer in einer eigenen Katalogkategorie anlegen |
| `PROJEKTDATEI_IOS` | Dateiendung `.sqproj` in der iOS-Dateiwahl | `EPOS.iOS/Dienste/Dateifilter.cs` | nur im Modus wählbar |
| `PROJEKTDATEI_KI` | Feldkarten und Aktionen der Projektdatei für den Hilfe-Assistenten | `EPOS.Kern/Allgemein/KI/Dialoge/KiDialoge.cs`, Kennzeichen `nurEntwickler` | ohne Modus benannte Absage des Assistenten |
| `KATALOG_WIEDERHERSTELLEN` | „Auslieferungsstand wiederherstellen…" des Katalogabgleichs | `KatalogabgleichDialog.razor`, Kern `Katalogabgleich.Wiederherstellen`; im Modus über *Entwicklung* | Reparatur eines behaltenen Katalogsatzes |
| `KI_WERKZEUGE` | KI-Chat: „Werkzeuge…", „Protokoll anzeigen", Modellwahl und „Weg B" in den KI-Einstellungen | `EPOS.UI/Dialoge/Hilfe/KiChatDialog.razor`, `KiEinstellungenDialog.razor` | Prüf- und Fehlersuchwerkzeuge des Assistenten; „Was wird gesendet?" bleibt für alle |
| `EXPORT_IDS` | „Exportzusage (IDS) öffnen…" im Gebäudeexport | `EPOS.UI/Dialoge/Export/GebaeudeExportDialog.razor` | Prüfartefakt des IFC-Exports |
| `ZAPFPROFIL_VORGABEN` | Rubrik „Entwicklung" im Einstellungsdialog mit den fünf Rechenvorgaben `Zapfprofil.Anzeigetemperatur`, `Zapfprofil.Stundenschwelle`, `Zapfprofil.Nenninhalte`, `Zapfprofil.Katalogordner`, `Zapfprofil.Importordner` | `EinstellungenDialog.razor`; gelesen in `ZapfprofilCtrl.Eingang.cs`, `ZapfprofilCtrl.Auslegung.cs`, `ZapfprofilHuelle.*` | heute nur per Registry setzbar; ungültige Werte werden mit Hinweis verworfen |
| `WEBVIEW_WERKZEUGE` | Entwicklerwerkzeuge der WebView2 (Strg + Umschalt + I) | `WindowsFormsApplication1/Allgemein/Blazor/BlazorDienste.cs` (`AddBlazorWebViewDeveloperTools`, heute `#if DEBUG`) | DOM, Stilblatt, Konsole, Netz der Razor-Oberfläche im Release beim Kunden |

Bewusst **kein** Entwicklerbereich: Katalog-Dublettensuche, Schloss eines Auslieferungssatzes,
Zapfprofil-Stufe „Experte", Gebäude-Rechenweg „Tagesbilanz", „Was wird gesendet?" im KI-Chat.
Bewusst **Build-Schalter** bleiben die elf Prüfhaken `#if DEBUG` unter
`EPOS.Kern/Allgemein/Simulation/` (sie verändern den Rechenweg).

### 2.2 Der Menükopf „Entwicklung"

| Punkt | Tut | Hinweis |
|---|---|---|
| Ausnahmeprotokoll öffnen | öffnet `Logs\Ausnahmen.txt` mit dem Standardprogramm | erster Handgriff bei einem wortlosen Absturz (Abschnitt 3.2) |
| Protokollordner öffnen | Explorer auf `Logs\` | Ausnahmen, Migration, KI-Aktionen |
| Datenbankordner öffnen | Explorer auf `%ProgramData%\EPOS_PLAN` | Datenbank, `DB-Backup`, Vorlage |
| Diagnosedaten kopieren | Textblock aus Abschnitt 3.3 in die Zwischenablage | derselbe Block wie im Dialog „Über EPOS-Plan" |
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
| `ki_aktionen.txt` | neben der Datenbank, ab Stufe 3 in `Logs\` | Aktionen des Hilfe-Assistenten, wächst ohne Rotation |
| `Tab_Katalogabgleich` | in der Datenbank | eine Zeile je Abgleichaktion |
| `<paket>.importbericht.txt` | neben dem Projektpaket | nur beim Projektimport |

### 3.2 Absturz ohne Meldung [heute]

Ereignisanzeige mit `KERNELBASE.dll` und `0xc000041d`: Eine Ausnahme ist aus einem Rückruf der
WebView2 herausgelaufen. Die Ursache steht in `Logs\Ausnahmen.txt` — der letzte Eintrag vor dem
Kopf des nächsten Laufs. Diese Datei anfordern, nicht den Weg raten (Fallstricke in
`WindowsFormsApplication1/CLAUDE.md`).

### 3.3 Diagnosedaten und Supportpaket [Stufe 3]

Der Dialog „Über EPOS-Plan" (*Hilfe → Version*) zeigt den Diagnoseblock aus dem Kern-Baustein
`Diagnosedaten` (`EPOS.Kern/Allgemein/Diagnose/`): Programmversion und Baudatum, Plattform und
Betriebssystemfassung, Sprache, Datenbankpfad, Schemastand (`Tab_Applikation.SchemaVersion` gegen
`SchemaStand.Zielversion`), Katalogfassung, Lizenztyp, Lizenznummer, Firma, Status, gültig bis,
Gerätekennung-Kurzform (erste acht Zeichen des Abdrucks), Protokollordner, Entwicklermodus mit
Quelle. Knöpfe: „Diagnosedaten kopieren", „Protokollordner öffnen", „Supportpaket speichern…",
„Fernwartung…".

Das Supportpaket ist eine ZIP-Datei mit `Diagnosedaten.txt`, `Ausnahmen.txt`,
`Ausnahmen.alt.txt`, `migration_protokoll.txt` und dem Auszug aus `Tab_Katalogabgleich` — ohne
Projektdaten; `ki_aktionen.txt` nur mit eigenem Haken, weil es Eingaben des Anwenders enthalten
kann. Der Anwender hängt die Datei an seine E-Mail. Das ist der Supportweg ohne Fernwartung und
steht auch der Testversion offen.

### 3.4 Wo der Support sonst nachsieht [heute]

| Ort | Inhalt |
|---|---|
| `HKLM\SOFTWARE\INEKON\EPOS-Plan` | `InstallDir`, `Version` (vom Setup) |
| `HKCU\Software\wp-plan` | Sprache, KI-Werte, Einwilligung, Lizenzanker, ab Stufe 1 `Entwicklermodus` |
| `HKLM\SOFTWARE\wp-plan` | maschinenweite Sperren: `KiDeaktiviert`, ab Stufe 1 `EntwicklermodusGesperrt` |
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
   gesperrt, der Grund steht dabei (Abschnitt 4.4).
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
4. Nach der Sitzung: kein Eintrag in EPOS-Plan; das Sitzungsprotokoll der Web-Konsole (wer wann mit
   wem) ist die einzige Aufzeichnung.

### 4.4 Die Lizenzbedingung (AS-Q8, Lesart b)

Der Knopf ist frei, wenn ein Lizenz-Token vom Typ `person` oder `firma` vorhanden ist — gleich ob
gültig, in Kulanz oder abgelaufen (Lesemodus). Gesperrt bleibt er in der Testversion (`demo`), ohne
Aktivierung (`NichtAktiviert`) und bei manipulierter Uhr (`UhrManipuliert`). Für diese Fälle gilt
Telefon und E-Mail mit Diagnosedaten; eine Testversion bekommt keine Fernwartung. Die Frage stellt
`LizenzCtrl.FernwartungErlaubt(LizenzLage)`; ein Supportvertrag im Token (`support_bis`) ist Stufe 4
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
| `EPOS.UI.Tests/EntwicklerbereicheWacheTests` | Anwenderbaum ohne Entwicklerpunkt, keine leeren Untermenüs, keine doppelten Trenner; jeder Schlüssel des Registers hat eine Fundstelle; jedes `data-stufe="entwickler"` hängt an einem Gaben-Kennzeichen | 1 |
| `MenuebandTests` | Zählungen beider Bäume, Regel „kein Untermenü mit nur einem Punkt" | 1 |
| bunit `GebaeudeImportDialog`, `RaumnutzungBlatt` | ohne Kennzeichen kein Projektdatei-Element, mit Kennzeichen alle | 1 |
| `EntwicklermodusTests` (Kern) | Sperre schlägt alles; Quelle richtig benannt | 1 |
| `LizenzCtrlTests` | `FernwartungErlaubt` für die vier Lesarten | 2 |
| bunit `FernwartungDialog` | gesperrt und frei; Knopf ruft die Adresse | 2 |
| `DiagnosedatenTests`, bunit `UeberDialog` | Textblock vollständig; Geste schaltet | 3 |

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
| Entwicklermodus, Freigabestufe, Register | `EPOS.Kern/Allgemein/Freigabe/Entwicklermodus.cs`, `Entwicklerbereiche.cs` | 1 |
| Menüzeile mit Stufe, Zuschnitt | `EPOS.UI/Bausteine/Menuepunkt.cs`, `Menuetabelle.cs` (`Sichtbar`) | 1 |
| Marke und Fenstertitel | `EPOS.UI/Seiten/Hauptfenster.razor`, `WindowsFormsApplication1/Views/Hauptformular/` | 1 |
| Startargument | `WindowsFormsApplication1/Program.cs` (`Main(string[] args)`) | 1 |
| Projektdatei-Einstiege | siehe Abschnitt 2.1 | heute |
| Ausnahmeprotokoll | `EPOS.Kern/Allgemein/Diagnose/Ausnahmeprotokoll.cs`, eingeschaltet in `Program.Main` | heute |
| Lizenzstand | `EPOS.Kern/Allgemein/Lizenz/LizenzManager.cs` (`LizenzStatus`), `LizenzToken.cs` (`Typ`), `EPOS.Kern/Controller/LizenzCtrl.cs` | heute |
| Adresse öffnen | `EPOS.Kern/Allgemein/Dienste/IDateiDienst.cs` (`AdresseOeffnen`), `WindowsFormsApplication1/Dienste/WindowsDateiDienst.cs`; der iOS-Adapter liefert heute die Vorgabe `false` | heute |
| Fernwartungsdialog | `EPOS.UI/Dialoge/Hilfe/FernwartungDialog.razor`, Hülle `EPOS.UI.Daten/Hilfe/FernwartungHuelle.cs` | 2 |
| Über-Dialog, Diagnosedaten | `EPOS.UI/Dialoge/Hilfe/UeberDialog.razor`, `EPOS.Kern/Allgemein/Diagnose/Diagnosedaten.cs` | 3 |
| Maschinenweite Werte | `EPOS.Kern/Allgemein/Dienste/IEinstellungen.cs` (`LiesMaschine`), `WindowsFormsApplication1/Dienste/RegistryEinstellungen.cs` | heute |


## 7 Stufenstand

| Stufe | Inhalt | Stand |
|---|---|---|
| 1 | Entwicklermodus, Entwicklerbereiche, Menükopf „Entwicklung", Projektdatei verborgen | offen (Konzept entschieden 11.10.2026) |
| 2 | Fernwartung unter Windows: Menüpunkt, Dialog, Lizenzbedingung, Fernwartungsseite; Zuarbeit INEKON: Server, Client, Seite, Signatur. Das iPad ist zurückgestellt | offen |
| 3 | Dialog „Über EPOS-Plan", Diagnosedaten, Supportpaket | offen |
| 4 | Supportvertrag im Lizenz-Token (`support_bis`) | offen, auf Zuruf |


## 8 Pflege dieses Handbuchs

- Die Markdown-Datei ist die Quelle. Wer eine Stufe umsetzt, trägt die Stufe in Abschnitt 7 als
  umgesetzt ein, ersetzt in den betroffenen Abschnitten „[Stufe n]" durch „[heute]" und berichtigt
  Namen und Pfade nach dem gebauten Stand.
- Danach wird die Word-Fassung neu erzeugt (Agent mit dem docx-Skill, Vorlage: dieses Dokument,
  A4, Überschriften und Tabellen wie hier) und im selben Commit abgelegt.
- Nichts aus diesem Handbuch wird ins Wiki übernommen; die Anwenderseite der Fernwartung steht in
  den Wiki-Quellen unter `Projekte/Wiki/` ohne Produktnamen.

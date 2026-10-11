# Konzept: Administration und Support — Entwicklerbereiche schalten, Fernwartung aus dem Menü

**Stand:** Entwurf vom 11.10.2026; **neun der zehn Fragen sind am 11.10.2026 entschieden, AS-Q10
ist zurückgestellt** (Abschnitt 7): AS-Q1 bis AS-Q6 und AS-Q8 nach Empfehlung, **AS-Q7 mit
RustDesk** statt TeamViewer (Lesart d), AS-Q9 passend dazu mit dem Link als Weg (Lesart c); die
Fernwartung vom iPad aus (AS-Q10) wird später bearbeitet — Stufe 2 baut Windows. Wortlaut des
Anwenders: „AS-Q1 bis AS-Q6 und AS-Q8 wie vorgeschlagen. AS-Q7 mit RustDesk. AS-Q9 passend zu
RustDesk und mit AS-Q10 iOS abstimmen — evtl nur (c) aus AS-Q9." und danach „stelle iPad-Support
(remote) zurück, wird später bearbeitet." Dazu der Entscheid „Änderung in der Wiki vornehmen: kein
TeamViewer als Support" — die beiden Wiki-Quellen nennen seither keinen Produktnamen mehr (1.1).
Abschnitt 4 ist auf RustDesk umgeschrieben; die Tabelle 4.1 hält den Vergleich als Begründung fest.
**Die Stufen 1 und 2 sind umgesetzt** (11.10.2026, #974 und #985; Protokolle [`2026-10-11_AS1_Entwicklermodus.md`](../ueberholt/Protokolle/Administration_Support/2026-10-11_AS1_Entwicklermodus.md) und [`2026-10-11_AS2_Fernwartung.md`](../ueberholt/Protokolle/Administration_Support/2026-10-11_AS2_Fernwartung.md)); Stufe 3 (3a Dialog „Über EPOS-Plan“, Diagnose, Geste im Dialog; 3b Supportpaket) ist umgesetzt (11.10.2026, #987; Protokoll [`2026-10-11_AS3_Ueber_Diagnose_Supportpaket.md`](../ueberholt/Protokolle/Administration_Support/2026-10-11_AS3_Ueber_Diagnose_Supportpaket.md)), die übrigen Stufen aus Abschnitt 8 sind offen. **Offen:** der Betrieb der Fernwartung bei INEKON (Fernwartungsseite, Server, signierter Client; 4.4, 4.8). Die Entwicklerseite
(Tastenfolgen, Schlüssel, Pfade, Supportablauf) steht im
[`Entwicklerhandbuch_Administration_Support_EPOS-Plan.md`](Entwicklerhandbuch_Administration_Support_EPOS-Plan.md),
mit Word-Fassung daneben.

**Auftrag des Anwenders (11.10.2026, wörtlich):** „Konzeption Administration und Support. 1. Es gibt
in EPOS-Plan Bereiche, die für den Benutzer nicht sichtbar sein sollen, aber für den Entwickler. Wie
kann das ein- und ausgeschaltet werden und einfach gehandhabt werden. Gebe Vorschläge. Ein Beispiel
ist der Import einer sqproj-Datei. 2. Es soll einen Remote Support geben, der aus dem Menü
zugänglich ist und bei vorhandener Lizenz aufgerufen werden kann."

**Geltungsbereich.** Die Windows-Anwendung (`EPOS_Plan.exe`) mit der plattformfreien Oberfläche;
die iOS-Hülle wird je Vorschlag benannt (was sie kann, was sie benannt ablehnt). Nicht Gegenstand:
die Administrationsdialoge der Kataloge (eigenes Konzept
[`Konzept_Administrationsdialoge_Neuordnung_EPOS-Plan.md`](Konzept_Administrationsdialoge_Neuordnung_EPOS-Plan.md)),
das Lizenzmodell selbst ([`EPOS-Plan_Konzept_Lizenzierung.md`](EPOS-Plan_Konzept_Lizenzierung.md)),
die Setup-Kette ([`Konzept_Setup_InnoSetup_EPOS-Plan.md`](Konzept_Setup_InnoSetup_EPOS-Plan.md)).

**Die drei Begriffe dieses Papiers.** *Anwender* ist, wer EPOS-Plan mit einer Lizenz benutzt.
*Administrator* ist, wer eine Kundeninstallation verwaltet (Rechner, Rechte, maschinenweite
Vorgaben) — heute schon über den maschinenweiten KI-Abschalter unter `HKLM` bekannt. *Entwickler*
ist INEKON: wer Rechenwege, Importe und Werkzeuge baut und prüft und dafür in der ausgelieferten
Anwendung Bereiche braucht, die ein Anwender nicht sieht. *Support* ist INEKON am Telefon, per
E-Mail und — das ist der zweite Auftragspunkt — per Fernwartung.


## 1 Befund

### 1.1 Administration und Support heute

**Das Menü** (`EPOS.UI/Bausteine/Menuetabelle.cs`) hat vier Köpfe: *Projekte*, *Administration*
(Gebäude, Klimadaten, Anlagen, Kosten, Daten & Import, Einstellungen), *Hilfe* und *Sprache*. Unter
*Hilfe* stehen Version, Lizenz, Dokumentation und der Hilfe-Assistent. Es gibt keinen Punkt für
Support, Fernwartung, Protokoll, Diagnose, Update oder Logbuch. „Version" ist ein Meldungsfeld
(`HauptfensterHuelle.Versionsmeldung`: Produktname, Claim, Versionsnummer, Firmenname) — ohne
Datenbankpfad, Schemastand, Lizenznummer, Kontakt.

**Der Supportweg liegt außerhalb der Anwendung.** Die Wiki-Seiten „Installation und Update →
Unterstützung" und „Über EPOS-Plan → Software-Service" (Repo-Quellen unter `Projekte/Wiki/`)
nennen Telefon, Fax, E-Mail `info@inekon.de`, die Zeiten werktags 9–16 Uhr und: „In Problemfällen
bieten wir mit TeamViewer eine Fernwartung an." (Befund vom 11.10.2026; nach dem Anwenderentscheid
vom selben Tag nennen beide Seiten nur noch „Fernwartung" ohne Produktnamen.) Der Anwender soll
Kundennummer, Windows-Version und EPOS-Plan-Version bereithalten. In der Anwendung steht davon nichts; das Setup trägt nur
`AppSupportURL = https://epos-plan.de/support` in die Windows-Softwareliste ein
(`Setup/EPOS-Plan.iss`), und die Rechtstexte nennen das Impressum.

**Der Supportvertrag** ist ein WooCommerce-Produkt des Lizenzservers (`Lizenzserver/epos-lizenz-1.4.1/includes/class-epos-woocommerce.php`:
200 € netto je Stunde, Mindestabnahme eine Stunde im Jahr, nachträglich im Kundenkonto
abschließbar). Er liegt als Meta `_epos_supportvertrag` an Lizenz und Benutzer — **nicht im
Lizenz-Token** (`includes/class-epos-token.php`) und in keiner REST-Antwort an die Anwendung außer
den Vertragsfassungen unter `/vertrag`. Die Anwendung weiß heute nicht, ob ein Kunde einen
Supportvertrag hat.

**Der Lizenzstand** in der Anwendung (`EPOS.Kern/Allgemein/Lizenz/`): sechs Zustände
`LizenzStatus` (NichtAktiviert, Gueltig, NachpruefungFaellig, Kulanz, Lesemodus, UhrManipuliert),
drei Typen im Token (`demo`, `person`, `firma`; `Edition` wird gelesen, aber nirgends ausgewertet),
Felder Firma, Benutzer, LizenzId, GeraeteId, GueltigAb/Bis, KulanzTage, TokenBis. Die Oberfläche
fragt den Kern nie selbst (Regel S‑2 in `LizenzCtrl`): Hüllen reichen `LizenzLage` (Banner) und
`LizenzGaben` (Dialog) herein. Der Lesemodus wird an einer Stelle durchgesetzt (`Schreibnaht`).

**Diagnosequellen** gibt es, aber ohne Oberfläche: das Ausnahmeprotokoll `Logs\Ausnahmen.txt` neben
der Datenbank (`EPOS.Kern/Allgemein/Diagnose/Ausnahmeprotokoll.cs`, seit 30.09.2026, nur Windows,
zwei Dateien zu je 200 KB), `migration_protokoll.txt` (je Start überschrieben), `ki_aktionen.txt`,
die Tabelle `Tab_Katalogabgleich`, der Importbericht des Projekttransfers; dazu
`HKLM\SOFTWARE\INEKON\EPOS-Plan` mit `InstallDir` und `Version` („für Support", Setup). Ein
allgemeines Logging gibt es nicht (rund 280 `Console.Write`, rund 88 `Trace`/`Debug` ohne Hörer).
Keine Seite zeigt Datenbankpfad, Schemastand (`Tab_Applikation.SchemaVersion`), Katalogfassung,
Lizenz, Gerätekennung, Sprache und Betriebssystem zusammen.

**Nach außen öffnen** kann die Anwendung über `IDateiDienst` (`EPOS.Kern/Allgemein/Dienste/`):
`AdresseOeffnen` (http/https, Windows ja, iOS liefert die Vorgabe `false`) und `MitSystemOeffnen`
(Datei mit Standardprogramm; iOS: Teilen-Blatt). Ein Fremdprogramm startet heute nur die
Berichtsnaht (`WindowsFormsApplication1/Dienste/WindowsBerichtsvorlagenwege.cs`: Word über
`App Paths`, `Process.Start`, Explorer mit `/select`). Es gibt keine Stelle, die ein mitgeliefertes
Programm aus `{app}` startet, und keinen Ordner dafür im Setup. Proxy, Zertifikate und Firewall
sind nirgends konfiguriert; `HttpClient` läuft mit den .NET-Vorgaben (Lizenzserver, Wiki,
Gemini, PVGIS, CEC-Listen, Hugging Face, Übersetzungs-Proxy).

### 1.2 Das Beispiel: die Projektdatei des CAD-Programms (`.sqproj`)

Seit #731 (05.10.2026) lädt der Gebäudeimport zur IFC-Datei aus HottCAD die Projektdatei des
CAD-Programms dazu (Datenaustauschkonzept Kapitel 16; Kern `EPOS.Kern/Allgemein/Import/Sqproj/`,
fünfzehn Klassen). Sie ist in der Oberfläche an **drei Stellen** erreichbar, alle für jeden Anwender:

| Einstieg | Ort | Was er tut |
|---|---|---|
| Quellenwahl des Gebäudeimports: „Format mit Projektdatei", „nur Projektdatei" | `EPOS.UI/Dialoge/Import/GebaeudeImportDialog.razor` (Konzept 16.1), `GebaeudeImportDaten.GebaeudeImportWeg` | setzt den Weg des Dialogs |
| Knopf „Projektdatei dazuladen (.sqproj)…" neben der Dateizeile, „Projektdatei entfernen", Zonierungswahl DIN/Simulation | `GebaeudeImportDialog.razor` (16.4), Hülle `EPOS.UI.Daten/Bedarf/GebaeudeImportHuelle.cs` (`ProjektdateiWaehlen`) | liest Zonen, Nutzung, Ganglinien, Kalender als Konditionierung je Zone |
| Kopfknopf „Aus Projektdatei…" im Blatt Nutzungsprofile und „Nutzungsprofile der Datei übernehmen…" im Import | `EPOS.UI/Dialoge/Bedarf/ProjektdateiProfile.razor`, Wirt `RaumnutzungBlatt.razor`, Hülle `ProjektdateiProfileHuelle.cs` (NP4b) | legt Profile je DIN-Nummer in einer eigenen Katalogkategorie an |

Dazu: der Schemawert `SQPROJ` in `Tab_Importzuordnung.Format` und in jeder Herkunftsspalte
(Schemaschritt 200), die KI-Feldkarten des Gebäudeimports (`EPOS.Kern/Allgemein/KI/Dialoge/KiDialoge.cs`),
die Dateiendung in der iOS-Dateiwahl (`EPOS.iOS/Dienste/Dateifilter.cs`), „Neu lesen" am
Gebäude (`GebaeudeNeulesenHuelle`), die Herkunftszeile im Aufbau (`GebaeudeAufbauHuelle`). Der
Knopf ist heute nur dann *gesperrt*, wenn die Gebäudedatei nicht aus dem passenden CAD-Programm
stammt — nicht, weil ein Anwender ihn nicht sehen soll. Genau das ist der Fall des Auftrags: ein
Bereich, der für Entwickler und Pilotkunden gedacht ist, aber heute jedem offensteht.

### 1.3 Weitere Bereiche, die heute jeder sieht

Aus der Sichtung der Oberfläche (Befund der Suchagenten 11.10.2026), ohne Wertung — die Wertung
trifft Abschnitt 3.4:

| Bereich | Ort | Charakter |
|---|---|---|
| Katalog aktualisieren… → „Auslieferungsstand wiederherstellen…" | `KatalogabgleichDialog.razor` (Administration → Daten & Import) | Reparatur; schreibt Katalogsätze zurück |
| Katalog-Dubletten prüfen | `KatalogDublettenDialog.razor` | Pflege; löscht und benennt um, schreibt sofort |
| Schloss eines Auslieferungssatzes aufheben | `Bausteine/Schlossumschaltung.cs`, Auswahlleiste aller Verwaltungen | macht Auslieferungssätze beschreibbar (AD-Q15, mit Rückfrage) |
| KI-Chat: „Werkzeuge…", „Was wird gesendet?", „Protokoll anzeigen", Einstellungen mit Modellwahl und „Weg B" | `KiChatDialog.razor` | Entwickler- und Prüfwerkzeuge des Assistenten |
| Zapfprofil-Stufe „Experte" (Stochastik, Seed, Realisierungen, Konsistenzprobe) | `ZapfprofilDialog.razor` | Fachlich für Anwender gedacht (Stufenwahl ist Teil des Konzepts Zapfprofil) |
| Gebäude-Rechenweg „Tagesbilanz" (Altweg) | `GebaeudeStammblattFelder.razor` | Wahlweg bis zur Ablösung (ADR-006) |
| gbXML-Export und „Exportzusage (IDS) öffnen…" | `GebaeudeExportDialog.razor`, Konstante `GebaeudeExportRegeln` | seit G7b dauerhaft frei |
| Fünf Rechenvorgaben des Zapfprofils (`Zapfprofil.*`) | nur per Registry, keine Oberfläche | verdeckte Entwicklerschalter ohne Weg |
| WebView-Entwicklerwerkzeuge (Strg+Umschalt+I) | `BlazorDienste.cs`, nur Debug-Build | im Release nicht erreichbar |
| Prüfhaken der Simulation (elf `#if DEBUG`) | `EPOS.Kern/Allgemein/Simulation/` | nur Debug-Build |

### 1.4 Welche Schaltmechanismen es schon gibt

Das Menü kennt **keine Bedingung**: `Menuepunkt` trägt Name, Textschlüssel, Ziel, Argument, Bild,
Kürzel, Trenner, RechtsBuendig, Katalog und Untereinträge — kein Feld für Plattform, Lizenz,
Einstellung oder Rolle; `Menueband.razor` zeichnet jede Zeile, einen ausgegrauten Zustand gibt es
nicht. Die einzige Positivliste ist `AppWurzel.FuehrtZiel` (iOS): sie hängt am `Seitenschluessel`,
nicht am Menüpunkt, und dient zugleich als Annahmeliste von `OeffneMaske` und als Filter des
Katalogeinstiegs. Kein Punkt ist je nach Lizenz, Einstellung oder Rolle verborgen.

Vier Bauarten für „an/aus" gibt es im Bestand, jede mit einem Gebrauch:

| Bauart | Beispiel | Setzen | Vorrang | Taugt als Entwicklerschalter? |
|---|---|---|---|---|
| Quelltextkonstante | `GebaeudeExportRegeln.FREIGABE_GBXML_EXPORT` | nur mit neuem Programmstand | — | nein: nicht schaltbar |
| Build-Konfiguration `#if DEBUG` | WebView-Werkzeuge, Prüfhaken | nur Debug-Build | — | nein: der Entwickler braucht es in der ausgelieferten Release-Anwendung beim Kunden |
| Umgebungsvariable / `AppContext` | `EPOS_PRUEFLAUF` (iOS-Prüfmodus), `EPOS.Plattformrundung.UlpStoerung` | vor dem Start, von außen | — | ja für Entwicklerrechner und CI; keine Oberfläche |
| Einstellung mit maschinenweiter Sperrebene | `KiDeaktiviert` (`HKCU`), `AbschalterMaschine` (`HKLM` über `IEinstellungen.LiesMaschine`) | Registry/Einstellung, Verwaltung setzt HKLM | **HKLM schlägt HKCU** | ja: die einzige Bauart, in der ein Administrator dem Benutzer vorgeht |

`Program.Main` nimmt heute **keine Startargumente** (`static void Main()`); der frühere Schalter
`/ki-feldsicherung-aus` ist am 14.09.2026 abbestellt worden und nur noch Kommentar.


## 2 Zielbild

1. **Eine Stufe, eine Quelle.** Es gibt genau zwei Sichtbarkeitsstufen, `Anwender` und
   `Entwickler`, und einen Kern-Baustein, der sie ermittelt. Niemand fragt Registry, Umgebung oder
   Startzeile selbst.
2. **Verbergen, wo der Anwender nichts verliert; sperren, wo er etwas verstehen soll.**
   Entwicklerbereiche werden *verborgen* — ein Anwender sieht keinen Knopf, den er nicht bedienen
   darf. Lizenzgebundene Supportfunktionen werden *gesperrt, nicht ausgeblendet* (Hausregel
   `EPOS.UI/CLAUDE.md`: `Aktiv` sperrt, der Grund steht dabei) — der Anwender soll sehen, dass es
   Fernwartung gibt und was ihm dazu fehlt.
3. **Wer eingeschaltet hat, sieht es.** Ein aktiver Entwicklermodus ist in der Kopfleiste
   gekennzeichnet und über das Menü wieder ausschaltbar; nie läuft eine Kundeninstallation unbemerkt
   im Entwicklermodus.
4. **Die Liste ist Daten, die Wache hält sie.** Welche Bereiche Entwicklerbereiche sind, steht an
   einer Stelle; ein Test beweist, dass der Anwenderbaum keinen davon enthält.
5. **Support beginnt in der Anwendung.** Unter *Hilfe* stehen Kontakt, die Angaben, die der Support
   braucht, und der Weg zur Fernwartung — mit Annahme je Sitzung im Fernwartungsprogramm und ohne
   eigene Speicherung.
6. **Nichts Fachliches in der Schale.** Stufe, Lizenzfrage, Diagnosedaten und die Regeln der
   Fernwartung liegen im Kern beziehungsweise in den Hüllen; die Schale startet Prozesse und öffnet
   Adressen, mehr nicht. iOS lehnt benannt ab, was es nicht kann.


## 3 Entwicklerbereiche schalten (Auftragspunkt 1)

### 3.1 Der Baustein `Entwicklermodus`

Ein statischer Kern-Baustein `EPOS.Kern/Allgemein/Freigabe/Entwicklermodus.cs` mit der
Aufzählung `Freigabestufe { Anwender, Entwickler }`, einmal beim Start ermittelt:

| Reihenfolge | Quelle | Setzt | Schlüssel | Wirkung |
|---|---|---|---|---|
| 1 | maschinenweite Sperre | Administrator der Kundeninstallation | `HKLM\Software\wp-plan\EntwicklermodusGesperrt = 1` (über `IEinstellungen.LiesMaschine`, wie der KI-Abschalter) | Stufe bleibt `Anwender`, alle folgenden Quellen werden ignoriert; der Grund ist abfragbar („durch die Verwaltung gesperrt") |
| 2 | Umgebungsvariable | Entwickler, CI, Prüfstände | `EPOS_ENTWICKLER=1` | `Entwickler`; beide Plattformen (iOS wie `EPOS_PRUEFLAUF`) |
| 3 | Startargument | Entwickler, Verknüpfung | `EPOS_Plan.exe --entwickler` (`Program.Main(string[] args)`) | `Entwickler` für diesen Lauf; nur Windows |
| 4 | Einstellung | Entwickler über die Geste (3.2) | `HKCU\Software\wp-plan\Entwicklermodus = 1` (`IEinstellungen.Lies`; iOS: `Preferences`) | `Entwickler` dauerhaft bis zum Ausschalten |

Der Baustein bietet `Stufe`, `Aktiv`, `Quelle` (Umgebung, Start, Einstellung, keine) und
`Gesperrt`. Er wird in `Program.Main` nach `EinstellungenUebernehmen()` einmal gelesen; Hüllen
und Kern fragen nur ihn. Kein `#if DEBUG`: der Entwickler arbeitet in der ausgelieferten
Release-Anwendung, auch beim Kunden.

### 3.2 Einfach gehandhabt — die drei Wege des Entwicklers

| Weg | Wann | Handhabung | Wieder aus |
|---|---|---|---|
| **Verknüpfung „EPOS-Plan (Entwickler)"** mit `--entwickler` | Entwicklerrechner, Vorführung, Fehlersuche beim Kunden über Fernwartung | Verknüpfung einmal anlegen (das Setup legt sie **nicht** an — sie bleibt dem Entwickler vorbehalten); Start über sie | nächster Start ohne Argument |
| **Geste im Dialog „Über EPOS-Plan"** (Abschnitt 5) | am Kundenrechner ohne Verknüpfung, am iPad | Dialog öffnen, **Strg+Umschalt+Alt+E** (iOS: fünfmal auf die Versionszeile tippen), Rückfrage „Entwicklermodus einschalten? Er gilt bis zum Ausschalten.", wahlweise mit Kennwort (AS-Q2) | Menü *Entwicklung* → „Entwicklermodus ausschalten" oder dieselbe Geste im Dialog (löscht die Einstellung) |
| **Umgebungsvariable** `EPOS_ENTWICKLER=1` | Entwicklerrechner dauerhaft, CI, Rasterprobe, Referenzlauf | einmal im System setzen | Variable entfernen |

Die Geste wirkt nur im geöffneten Dialog „Über EPOS-Plan“, nicht im Hauptfenster. Unter Windows fängt
das Fenster (`UeberFenster`) die Tastenfolge über `BlazorDialogForm.Befehlstaste` ab, bevor die WebView
sie sieht, und schaltet eine Nachricht später: Ist der Modus aus, nach der Rückfrage ein (bei Sperre
des Administrators eine Meldung), ist er an, aus — läuft er über Umgebung oder Startargument, nennt
die Meldung den Grund. Danach zeigt der Dialog den neuen Diagnoseblock, und das Hauptfenster versorgt
Menü, Marke und Titel neu (`HauptfensterHuelle.FreigabeNeuVersorgen`). Das Tippen auf die
Versionszeile am iPad ist nicht gebaut.

Was eingeschaltet ist, sieht man: die Kopfleiste trägt rechts neben der Versionsnummer die
**Marke „Entwickler"** (Warnfarbe `--epos-warn`, Tooltip mit der Quelle), der Fenstertitel den
Zusatz „(Entwicklermodus)", und am Ende der obersten Menüebene — vor *Sprache* — erscheint der
Kopf **„Entwicklung"** (3.5). Ist die Quelle Umgebung oder Startargument, sagt der Ausschaltpunkt
benannt, warum er nichts tut („eingeschaltet durch Startparameter — beim nächsten Start ohne
`--entwickler` aus").

### 3.3 Wo die Stufe wirkt — drei Orte, ein Muster

**(a) Menüpunkt.** `Menuepunkt` bekommt den Konstruktorparameter `stufe: Freigabestufe.Entwickler`
(Vorgabe `Anwender`). `Menuetabelle.Eintraege` bleibt der vollständige Baum; neu ist
`Menuetabelle.Sichtbar(Freigabestufe)`, das den Baum für die Stufe zuschneidet — ohne leere
Untermenüs, ohne doppelte oder randständige Trenner, und mit der bestehenden Regel „kein Untermenü
mit nur einem Punkt" auch im zugeschnittenen Baum. Die Windows-Hülle reicht
`Menuetabelle.Sichtbar(Entwicklermodus.Stufe)` als `Eintraege` an `Hauptfenster.razor`; das
`Menueband` bleibt unverändert. Auf iOS wirkt dieselbe Stufe auf `Menuetabelle.Kataloge` und den
Katalogeinstieg der Projektliste.

**(b) Knopf oder Feld in einem Dialog.** Die Hülle reicht das Kennzeichen in den Gaben
(`GebaeudeImportGaben.ProjektdateiFreigegeben`, aus `Entwicklermodus.Aktiv`), und die Komponente
zeigt die Zeile nur dann — nach Regel S‑2 fragt keine Razor-Komponente den Kern. Ein kaskadierender
Wert aus `AppWurzel` taugt dafür nicht: Unter Windows laufen Dialoge in eigenen Fenstern mit eigener
Wurzel (`Fensterwurzel<T>`), die Kaskade käme dort nicht an. Verborgene Elemente tragen
`data-stufe="entwickler"`, damit die Wache (3.6) sie findet.

**(c) Hilfe-Assistent.** Die Feldkarten und Aktionen der verborgenen Elemente (für die Projektdatei
in `KiDialoge.cs`) bekommen das Kennzeichen `nurEntwickler`; ohne Modus lässt der Assistent sie aus
dem Aktionsregister und antwortet benannt („diese Funktion ist in Ihrer Fassung nicht freigegeben"),
statt einen Knopf zu drücken, den die Maske nicht zeigt (Konzept KI-Dialogintegration: benannte
Absage statt stillem Übergehen).

**(d) Daten bleiben Daten.** Was ein Entwickler im Modus angelegt hat — Importzuordnungen mit
`Format = 'SQPROJ'`, Profile der Kategorie „Projektdatei …" — bleibt ohne Modus lesbar: die
Herkunftszeile zeigt „aus Projektdatei", „Neu lesen" bleibt möglich (es braucht die Datei und
ändert keine Zuordnung), nur das **Anlegen neuer** Projektdatei-Importe ist verborgen (AS-Q5).

### 3.4 Die Liste: was Entwicklerbereich wird

**Stufe 1 — gesetzt durch den Auftrag:** die Projektdatei (`.sqproj`) an allen drei Einstiegen
(1.2), die Dateiendung in der iOS-Dateiwahl, die KI-Feldkarten.

**Kandidaten — je Zeile ein Anwenderentscheid (AS-Q4), mit Empfehlung:**

| Bereich | Empfehlung | Begründung |
|---|---|---|
| Katalogabgleich „Auslieferungsstand wiederherstellen…" | **Entwickler** | Reparaturweg; im Supportfall führt INEKON ihn per Fernwartung aus |
| Katalog-Dubletten prüfen | Anwender (bleibt) | Pflege eigener Importe, schreibt mit Rückfrage; Auslieferungssätze sind geschützt |
| Schloss aufheben | Anwender (bleibt) | AD-Q15 vom 23.09.2026 hat es bewusst allen Verwaltungen gegeben |
| KI-Chat „Werkzeuge…", „Protokoll anzeigen", Modellwahl, „Weg B" | **Entwickler** | Prüf- und Fehlersuchwerkzeuge; „Was wird gesendet?" bleibt Anwender (Datenschutz-Transparenz) |
| Zapfprofil „Experte" | Anwender (bleibt) | fachliche Stufe des Zapfprofilkonzepts, kein Entwicklerwerkzeug |
| Gebäude-Rechenweg „Tagesbilanz" | Anwender (bleibt) | Wahlweg laut ADR-006 bis zur Ablösung; sein Ende ist ein eigener Entscheid |
| „Exportzusage (IDS) öffnen…" | **Entwickler** | Prüfartefakt des Exports; der Export selbst bleibt Anwender |
| Zapfprofil-Rechenvorgaben (`Zapfprofil.*`) | **Anwender** — Rubrik „Zapfprofil" im Einstellungsdialog | die Rubrik zeigt die fünf Vorgaben allen Anwendern und schreibt sie für alle; leer gilt die Vorgabe des Programms, ungültige Werte verwirft der Leser mit Hinweis |
| WebView-Entwicklerwerkzeuge | **Entwickler** statt `#if DEBUG` | im Release beim Kunden nutzbar |
| Prüfhaken der Simulation (`#if DEBUG`) | bleiben Build-Schalter | sie verändern den Rechenweg; ein Laufzeitschalter wäre ein Risiko für den Referenzlauf |

### 3.5 Der Menükopf „Entwicklung"

Nur im Entwicklermodus, vor *Sprache*; alle Punkte tragen `stufe: Entwickler`. Vorschlag für den
Erstbestand (jeder Punkt eine kleine Hülle, kein Fachwissen in der Schale):

| Punkt | Ziel | Bemerkung |
|---|---|---|
| Ausnahmeprotokoll öffnen | `Logs\Ausnahmen.txt` mit `MitSystemOeffnen` | der häufigste Handgriff bei einem wortlosen Absturz (`WindowsFormsApplication1/CLAUDE.md`, Fallstricke) |
| Protokollordner öffnen | Explorer auf `Logs\` | Ausnahmen, Migration, KI-Aktionen an einem Ort — dazu wandert `ki_aktionen.txt` nach `Logs\` (AS-Q6) |
| Datenbankordner öffnen | Explorer auf `%ProgramData%\EPOS_PLAN` | Datenbank, `DB-Backup`, Vorlage |
| Diagnosedaten kopieren | Textblock aus Abschnitt 5 in die Zwischenablage | derselbe Block wie im Dialog „Über EPOS-Plan" |
| Entwicklerwerkzeuge der Oberfläche | WebView2-DevTools | löst `#if DEBUG` ab |
| Auslieferungsstand wiederherstellen… | `KatalogabgleichDialog`, Reparaturzweig | aus *Daten & Import* hierher (3.4) |
| Entwicklermodus ausschalten | löscht `Entwicklermodus`; bei Umgebung/Startargument benannte Absage | letzter Punkt |

„Diagnosedaten kopieren“ ist mit Stufe 1 gebaut (Kern-Baustein `Diagnosedaten` aus Welle 0).

Was **nicht** ins Menü kommt: Referenzlauf, Proben, Werkzeuge — sie bleiben eigene Programme
außerhalb der Auslieferung; eine SQL-Konsole gibt es nicht und soll es nicht geben (Schreibnaht,
Schema-Migration und Auslieferungskennzeichen wären umgangen).

### 3.6 Die Wache

`EPOS.UI.Tests/EntwicklerbereicheWacheTests`:

1. `Menuetabelle.Sichtbar(Anwender)` enthält keinen Punkt mit `stufe == Entwickler`, keine leeren
   Untermenüs, keine doppelten Trenner und hält „kein Untermenü mit nur einem Punkt";
   `Sichtbar(Entwickler)` enthält den Kopf *Entwicklung* vollständig. Die Zählwächter in
   `MenuebandTests` zählen künftig beide Bäume.
2. Ein Register `Entwicklerbereiche.Alle` (Kern, Schlüssel je Bereich: `PROJEKTDATEI_IMPORT`,
   `PROJEKTDATEI_PROFILE`, `KATALOG_WIEDERHERSTELLEN`, …) — jeder Schlüssel hat eine Fundstelle in
   Menü, Gaben oder Feldkarte, und jedes `data-stufe="entwickler"` in den Razor-Quellen hängt an
   einem Gaben-Kennzeichen (Quelltextprobe wie `KiDialogAusnahmen`).
3. bunit: `GebaeudeImportDialog` ohne `ProjektdateiFreigegeben` rendert weder die beiden
   Quellenwahl-Einträge noch den Knopf; mit Kennzeichen alle drei. Dasselbe für `RaumnutzungBlatt`.
4. Kern: `Entwicklermodus` mit HKLM-Sperre bleibt `Anwender`, gleich was Umgebung, Argument und
   Einstellung sagen; die Quelle wird richtig benannt.


## 4 Fernwartung aus dem Menü (Auftragspunkt 2)

### 4.1 Werkzeugwahl

Vier Werkzeuge kommen in Frage; Angaben nach Herstellerseiten und Wissensdatenbanken vom
11.10.2026 (Preise und Lizenzstufen sind beim Hersteller zu bestätigen):

| Kriterium | TeamViewer, personalisiertes QuickSupport-Modul | AnyDesk Custom Client (nur eingehend) | Windows Quick Assist | RustDesk (selbst gehostet, Custom Client) |
|---|---|---|---|---|
| Bestand bei INEKON | **ja** — das Wiki nennt TeamViewer als Fernwartung des Wartungsvertrags | nein | nein | nein |
| Lizenz des Helfers | Business, Premium, Corporate oder Tensor (Modul selbst kostenlos) | Standard, Advanced oder Ultimate-Cloud | kostenlos; Helfer braucht ein Microsoft-Konto | Server quelloffen, Custom-Client-Generator ab dem Pro-Tarif |
| Anwenderseite | Modul läuft ohne Installation, nur eingehend, mit Logo, Begrüßungstext, Haftungshinweis | Custom Client mit Logo, Haftungshinweis, Richtung „nur eingehend" | Store-App; Anwender tippt einen Code ein, den der Helfer nennt | gestalteter Client aus dem Pro-Generator (Name, Logo, nur eingehend, Server und Schlüssel fest), läuft ohne Installation |
| Sitzungsaufbau | Sitzungscode erscheint in der Service-Warteschlange von INEKON (Benachrichtigung in App und E-Mail); oder ID + Kennwort | ID + Kennwort; Kommandozeile `anydesk.exe <ID> --with-password` | Helfer erzeugt Code, Anwender gibt ihn ein | Kennung und Einmalkennwort, die der Anwender dem Support nennt; der Anwender nimmt die Verbindung an |
| Start aus EPOS-Plan | Modul herunterladen und starten | wie TeamViewer | App starten, Code von Hand | Link auf die Fernwartungsseite von INEKON (4.4) |
| iOS | QuickSupport-App: nur Bildschirm *ansehen* (Apple lässt keine Fernsteuerung zu) | Android ja, iOS keine Angabe | nein | die iOS-App steuert nur; ein iPad kann weder gesteuert noch angesehen werden (Herstellerangabe) |
| Betrieb und Risiko | Link bricht, wenn das Modul im Management-Konto gelöscht wird oder die Lizenz ausläuft (neu anlegen) | Lizenzmodell 2025 geändert, Preissteigerungen gemeldet | abhängig von Microsoft-Konto und Store; keine Anpassung | eigener Server (VPS, Ports, Pflege); die Herstellersignatur der Windows-Datei ist im Dezember 2025 zurückgezogen worden, eine eigene Signatur ist nötig (SmartScreen) |

Die Empfehlung des Entwurfs lautete TeamViewer (Bestand im Wiki, Service-Warteschlange, keine
eigene Infrastruktur). **Entschieden ist RustDesk** (AS-Q7, Anwender 11.10.2026): Die Sitzungen
laufen über einen Server, den INEKON selbst betreibt, es fallen keine Lizenzkosten je Helfer an,
der Client ist quelloffen, und der Kunde lädt ein kleines Programm ohne Installation. Was dieser
Weg verlangt, steht in 4.4 und 4.8: einen eigenen Vermittlungsserver, den Pro-Tarif für den
gestalteten Client und eine eigene Codesignatur. Quick Assist bleibt als **Ausweichweg ohne eigene
Infrastruktur** erwähnenswert (auf der Fernwartungsseite als Hinweis „Alternativ: Quick Assist mit
einem Code vom Support"), nicht als Automatik.

### 4.2 Der Bedienweg

**Menü.** *Hilfe* → Version | — | Lizenz | Dokumentation | **Fernwartung…** | — | Hilfe-Assistent.
Neuer `Seitenschluessel.Fernwartung`, Zeile in `Menuetabelle.cs`, Fall in `HauptfensterHuelle.Ablauf`;
Textschlüssel `MENU_FERNWARTUNG` („Fernwartung…" / „Remote support…"). Der Punkt ist **immer
sichtbar** und ohne Lizenz **gesperrt** (Zielbild 2): dafür bekommt `Menuepunkt` neben der Stufe
ein zweites, kleines Datum — `sperrgrund` als Textschlüssel —, das die Hülle beim Zuschnitt setzt
(`Menuetabelle.Sichtbar(stufe, sperrgrund)`); das `Menueband` zeichnet einen gesperrten Punkt
ausgegraut mit dem Grund als Tooltip und löst keinen Sprung aus. Das ist die erste Sperre im Menü;
sie bleibt Daten (die Hülle entscheidet, die Komponente zeigt).

**Dialog `Dialoge/Hilfe/FernwartungDialog.razor`** (Hülle `EPOS.UI.Daten/Hilfe/FernwartungHuelle.cs`,
Fenster in der Schale wie `KatalogabgleichFenster`), vier Zonen von oben nach unten:

1. **Kontakt** — Telefon (in en-US mit Landesvorwahl), E-Mail, Zeiten (werktags 9–16 Uhr; Ressourcen `SUP_KONTAKT_*`, beide Sprachen; die E-Mail-Adresse ist ein `mailto:`-Link, auch im gesperrten Zustand). Ohne Lizenz oder ohne Internet ist das der Teil, der immer
   nützt.
2. **Ihre Angaben für den Support** — der Diagnoseblock aus Abschnitt 5 (Version, Schemastand,
   Katalogfassung, Lizenztyp und -nummer, Firma, Gerätekennung-Kurzform, Windows-Fassung, Sprache)
   mit dem Knopf „Kopieren". Der Support fragt genau das heute am Telefon ab.
3. **Fernwartung** — Erklärtext (was geschieht, wer den Bildschirm sieht, dass EPOS-Plan nichts
   aufzeichnet, dass der Anwender jede Verbindung im Fernwartungsprogramm selbst annimmt und
   jederzeit beendet), die drei Schritte in Kurzform (Programm von der Fernwartungsseite laden und
   starten, dem Support die angezeigte Kennung und das Einmalkennwort nennen, die Anfrage im
   Programm annehmen) und der primäre Knopf **„Fernwartungsseite öffnen"**, der
   `https://epos-plan.de/support/fernwartung` im Browser öffnet (AS-Q9 Lesart c, 4.4). Kein Haken
   für die Einwilligung: Die Einwilligung ist die Annahme der Verbindung im Fernwartungsprogramm,
   das sie je Sitzung abfragt; der Dialog sagt das. Fehler werden benannt (kein Browser, Adresse
   nicht erreichbar).
4. **Fußleiste** nach Hausregel: Füller · „Schließen" (primär). Kein OK/Abbrechen — der Dialog hat
   keinen Arbeitsstand.

Der Knopf „Fernwartungsseite öffnen“ ist nur frei, wenn die Lizenzbedingung (4.3) erfüllt ist und die Schale den Aufruf zum Öffnen der Adresse mitgibt. Sonst bleibt Zone 3 **sichtbar, der Knopf gesperrt**, mit dem Grund
in einer Hinweiszeile über ihm („Die Fernwartung gehört zum Wartungsvertrag. Aktivieren Sie zuerst Ihre
Lizenz unter Hilfe → Lizenz." / für die Testversion: „Die Fernwartung steht in der Testversion nicht zur Verfügung. Sie erreichen uns per E-Mail.").

### 4.3 „Bei vorhandener Lizenz" — die Bedingung

Vier Lesarten, in `LizenzCtrl` als eine Frage `FernwartungErlaubt()` (ohne Nebenwirkung; die Regel selbst als `internal FernwartungErlaubt(token, status)`) gebaut, damit die
Hülle sie stellt und die Komponente nur das Ergebnis sieht:

| Lesart | Bedingung | Folge |
|---|---|---|
| (a) schreibfähige Lizenz | `LizenzStatus` ∈ {Gueltig, NachpruefungFaellig, Kulanz} | auch die Testversion; ein abgelaufener Kunde (Lesemodus) bleibt außen vor, obwohl er oft gerade Support braucht |
| (b) bezahlte Lizenz, Laufzeit egal | Token vorhanden und `Typ` ∈ {person, firma}; Status beliebig außer NichtAktiviert und UhrManipuliert | Testversion außen vor; abgelaufene Kunden können Support holen (Verlängerung, Reparatur) |
| (c) jede Lizenz | Token vorhanden | auch Testversion |
| (d) Supportvertrag | neues Token-Feld `support_bis` (4.6) | fachlich die richtige Grenze, braucht Server und Token |

**Empfehlung: (b) jetzt, (d) als Ausbau** (AS-Q8). (b) deckt den Wortlaut „bei vorhandener Lizenz"
und lässt die Kunden hinein, die Support am nötigsten haben; der Supportvertrag ist heute nicht im
Token, und die Grenze „nur mit Vertrag" wäre ohne Server-Erweiterung nicht prüfbar. Der
`UhrManipuliert`-Fall bleibt gesperrt (der Zustand ist verdächtig; der Kontaktblock bleibt). Das Lesen des Zeitankers ohne Schreiben (`LizenzManager.AnkerOhneSchreiben`) macht ihn erkennbar, ohne die Lizenzablage zu verändern. Zusätzlich gesperrt sind ein unbekannter Lizenztyp und ein Token, das zu einem fremden Gerät gehört; die Gründe sind `FW_SPERRE_LIZENZ` (ohne Lizenz: Wartungsvertrag, zuerst die Lizenz aktivieren) und `FW_SPERRE_TEST` (Testversion).

### 4.4 Bereitstellung des Fernwartungsprogramms

Der Entwurf stellte drei Wege zur Wahl (AS-Q9): (a) Laden bei Bedarf mit Manifest, Prüfsumme und
Signaturprüfung, (b) im Setup mitliefern, (c) nur ein Link. **Entschieden ist (c)** (Anwender
11.10.2026, abgestimmt mit AS-Q10): EPOS-Plan öffnet einen Link, mehr nicht. Die
**Fernwartungsseite** `https://epos-plan.de/support/fernwartung` auf INEKONs WordPress trägt den
Download des Windows-Clients, die drei Schritte, den Hinweis auf Telefon und Zeiten und den Satz
für das iPad (4.5). In der Anwendung bleibt damit allein `IDateiDienst.AdresseOeffnen` — kein
Download, keine Prüfsumme, keine Signaturprüfung, keine zehnte Schnittstelle. Stufe 2 baut den Weg
für Windows; das iPad ist zurückgestellt (4.5).

**Der Client.** Ein gestalteter RustDesk-Client aus dem Generator des Pro-Tarifs (4.8): Name und
Logo von INEKON, Vermittlungsserver und Schlüssel fest voreingestellt, Richtung „nur eingehend",
Einstellungen gesperrt, Einmalkennwort je Sitzung, Annahme der Verbindung durch den Anwender. Er
läuft ohne Installation als einzelne Datei unter einem festen Namen (`EPOS-Plan-Fernwartung.exe`)
und liegt auf epos-plan.de — nicht beim Hersteller, damit INEKON die Fassung bewusst tauscht. Ohne
Pro-Tarif ginge der quelloffene Client mit Serverangabe im Dateinamen
(`rustdesk-host=<server>,key=<schlüssel>.exe`) oder per Startargument `--config`; ihm fehlen Logo,
die Sperre der Einstellungen und „nur eingehend".

**Signatur.** Die Herstellersignatur der RustDesk-Windows-Dateien ist im Dezember 2025 von der
Zertifizierungsstelle zurückgezogen worden; ein unsignierter oder so signierter Client löst beim
Kunden den SmartScreen-Hinweis „Unbekannter Herausgeber" aus. Das Setup-Konzept (§ 9) sieht ein
Codesignaturzertifikat auf INEKON ohnehin vor; dasselbe Zertifikat signiert den
Fernwartungs-Client. Bis es da ist, erklärt die Fernwartungsseite den Hinweis und den Weg
„Weitere Informationen → Trotzdem ausführen".

**Ausbau (nicht entschieden, nicht Teil der Stufe 2):** Die Lesarten (a) und (b) bleiben möglich,
weil die Datei INEKON gehört — (b) im Setup unter `{app}\Support\` mitliefern, dann startet der
Dialog sie unmittelbar; (a) bei Bedarf laden mit Manifest, SHA‑256 und Signaturprüfung gegen
INEKONs Zertifikat. Beides lohnt erst, wenn der Link-Weg im Support stört.

### 4.5 iOS

**Zurückgestellt** (Anwender 11.10.2026: „stelle iPad-Support (remote) zurück, wird später
bearbeitet"). Stufe 2 baut die Fernwartung für Windows; auf dem iPad gibt es den Menüpunkt ohnehin
nicht (die Kopfleiste ist dort leer), und der Fernwartungsdialog wird dort nicht angeboten, bis der
iPad-Weg entschieden ist. Für diesen späteren Entscheid liegt der Befund bereit: Apple lässt keine
Fernsteuerung zu, und die RustDesk-App für iOS kann nur *steuern* — ein iPad lässt sich mit
RustDesk weder fernsteuern noch ansehen (Herstellerangabe, Stand 2025/2026). Denkbar sind dann
Kontakt und Diagnosedaten auf dem iPad (Stufe 3, Einstieg in der Startansicht neben der
Versionszeile) mit benannter Ablehnung der Fernwartung, oder ein anderes Werkzeug allein für das
Ansehen des iPad-Bildschirms.

### 4.6 Supportvertrag ins Token (Ausbau)

Der Server kennt den Vertrag (`_epos_supportvertrag`), die Anwendung nicht. Mit einem Feld
`support_bis` (Datum oder leer) im Token, gesetzt in `class-epos-token.php` aus dem Meta, kann die
Anwendung später (d) prüfen; der Parser `LizenzToken` liest Felder tolerant (`TryGetProperty`),
ein älteres Token ohne das Feld bleibt gültig, die Signatur deckt das Feld mit. Dasselbe Feld
taugt für künftige Vertragsleistungen (Herstellerdaten-Downloads laut Wiki „Software-Service").
Kein Teil der Stufe 2; eigener Schritt auf Zuruf.

### 4.7 Datenschutz

Die Fernwartung ist eine Sitzung, die der Anwender selbst startet, annimmt und beendet; EPOS-Plan
speichert nichts davon (kein Protokoll, keine gemerkte Einwilligung — die Annahme gilt je Sitzung).
Aus der Anwendung geht allein der Aufruf der Fernwartungsseite nach außen (Adresse ohne
Kundendaten). Die Sitzung selbst läuft Ende-zu-Ende verschlüsselt über INEKONs eigenen
Vermittlungsserver (4.8), nicht über einen Fremdanbieter; der Server sieht Kennungen und
Verbindungsdaten, keine Bildschirminhalte. Das Sitzungsprotokoll des Pro-Tarifs (wer wann mit wem)
ist die einzige Aufzeichnung und liegt bei INEKON. Das steht im Dialog (Zone 3) und wird in zwei
Wiki-Seiten nachgezogen: „Installation und Update → Unterstützung" (Weg aus dem Menü, was
übertragen wird) und „Über EPOS-Plan → Software-Service". Der Rechtstext `LIZR_RH_A6`
(Datenverarbeitung der Lizenz) bleibt unberührt — die Fernwartung ist keine Lizenzverarbeitung.
Nebenbefund, nicht Teil dieses Konzepts: `LIZR_RH_A6` und Konzept Lizenzierung § 7 nennen den
Gerätenamen und den Windows-Anmeldenamen nicht, die `activate` und `trial` heute senden
(`WindowsGeraeteId.Anzeige`); das gehört in die nächste Pflege der Lizenzpapiere.

### 4.8 Was INEKON dafür betreibt

| Baustein | Inhalt | Einmalig / laufend |
|---|---|---|
| Vermittlungsserver | ein kleiner VPS mit den beiden RustDesk-Diensten `hbbs` (Kennungen, Durchstich) und `hbbr` (Relais) aus dem Docker-Abbild des Herstellers, eigener Name wie `fernwartung.epos-plan.de`, Ports 21115 bis 21119 TCP und 21116 UDP nach außen; das Schlüsselpaar entsteht beim ersten Start | einmalig einrichten; laufend Updates und Erreichbarkeit — ohne Server keine Sitzung, sonst nichts |
| Pro-Tarif | Web-Konsole, Generator des gestalteten Clients, Adressbuch, Sitzungsprotokoll; jährliche Abrechnung, kleinste Stufe genügt (Stand 2026 rund 20 bis 25 US-Dollar je Monat, beim Hersteller prüfen) | laufend |
| Gestalteter Client | aus dem Generator: Name, Logo, Server und Schlüssel, nur eingehend, gesperrte Einstellungen, Einmalkennwort; signiert mit INEKONs Zertifikat; abgelegt auf epos-plan.de | einmalig, dann je Clientfassung |
| Fernwartungsseite | `https://epos-plan.de/support/fernwartung`: Download, drei Schritte, SmartScreen-Hinweis, Telefon und Zeiten, Satz für das iPad | einmalig; Pflege mit dem Client |
| Support-Arbeitsplatz | der RustDesk-Client in der Vollfassung auf den Rechnern des Supports, auf denselben Server eingestellt, mit Adressbuch | einmalig je Arbeitsplatz |
| Codesignatur | das Zertifikat aus Setup-Konzept § 9 (Hardwaretoken); es signiert Setup, Anwendung und Fernwartungs-Client | einmalig beschaffen, jährlich verlängern |

Ohne Pro-Tarif geht es mit dem quelloffenen Server und dem Client mit Serverangabe im Dateinamen
(4.4); dann fehlen Logo, Adressbuch und Sitzungsprotokoll, und der Anwender sieht die Serverangabe
im Client. Der Vergleich der Kosten gehört zur Zuarbeit des Anwenders vor Stufe 2.


## 5 Der Dialog „Über EPOS-Plan" und die Diagnosedaten

Der Menüpunkt *Version* öffnet den Razor-Dialog `Dialoge/Hilfe/UeberDialog.razor`
(Hülle `EPOS.UI.Daten/Hilfe/UeberHuelle.cs`, Windows-Fenster `Views/Help/UeberFenster.cs`), denn drei Dinge brauchen denselben Ort: die Diagnosedaten (4.2 Zone 2), der
Kontakt und die Geste des Entwicklermodus (3.2). Inhalt:

- Kopf: Produktname, Claim, Version mit Baudatum (Schreibzeitpunkt der Programmdatei), INEKON.
- **Diagnoseblock** (Kern-Baustein `EPOS.Kern/Allgemein/Diagnose/Diagnosedaten.cs`, plattformfrei;
  gibt einen Textblock und Einzelwerte): Programmversion, Plattform und Betriebssystemfassung,
  Sprache, Datenbankpfad, Schemastand (`Tab_Applikation.SchemaVersion` gegen `SchemaStand.Zielversion`),
  Katalogfassung, Lizenztyp, Lizenznummer, Firma, Status, gültig bis, Gerätekennung-Kurzform
  (erste acht Zeichen des Abdrucks — genug zum Wiederfinden im Portal), Protokollordner,
  Entwicklermodus (Quelle). Knöpfe „Diagnosedaten kopieren" und „Protokollordner öffnen" (Windows;
  der Ordner kommt aus `Diagnosedaten.ProtokollordnerPfad`). Ohne Rückruf der Plattform kein Knopf.
- Kontakt (wie 4.2 Zone 1) und der Knopf „Fernwartung…" (öffnet den Fernwartungsdialog).
- Fußleiste: Füller · „Schließen".

**Supportpaket (Stufe 3b, AS-Q6):** „Supportpaket speichern…" legt eine ZIP-Datei ab mit
`Diagnosedaten.txt`, `Ausnahmen.txt` und `Ausnahmen.alt.txt`, `migration_protokoll.txt`,
dem Auszug aus `Tab_Katalogabgleich` (`Katalogabgleich.txt`, die jüngsten 2 000 Zeilen) — **ohne
Projektdaten und ohne `ki_aktionen.txt`** (es kann Eingaben des Anwenders enthalten; nur mit eigenem
Haken „KI-Protokoll beilegen", Vorgabe aus). Fehlende Quellen nennt die Ergebniszeile, ohne Fehler
(Kern `Allgemein/Support/Supportpaket.cs`). Die Datei hängt der Anwender an seine
E-Mail; das ist der Supportweg ohne Fernwartung, auch für die Testversion.

Zwei Nebenbefunde aus der Sichtung, die hier nur vermerkt sind (eigene Aufträge): die iOS-Version
wird von Hand gepflegt (`EPOS.iOS.csproj` 1.2.0 gegen `AssemblyInfo.cs` 1.2.0.3); der
Projekttransfer sichert vor dem Import mit `File.Copy` ohne `-wal` statt mit `VACUUM INTO`
(`ProjektTransferHuelle.SicherungAnlegen`), entgegen `BETRIEB_SQLITE.md` § 3.2.


## 6 Was sich ändert — je Projekt

| Projekt | Stufe 1 (Entwicklermodus, Projektdatei) | Stufe 2 (Fernwartung) | Stufe 3 (Über, Diagnose) |
|---|---|---|---|
| `EPOS.Kern` | `Allgemein/Freigabe/Entwicklermodus.cs`, `Entwicklerbereiche.cs`; `KiDialoge` mit `nurEntwickler` | `LizenzCtrl.FernwartungErlaubt`; Adresse der Fernwartungsseite als Konstante neben `LizenzManager.PORTAL_URL` | `Allgemein/Diagnose/Diagnosedaten.cs`; Supportpaket |
| `EPOS.UI` | `Menuepunkt.stufe`, `Menuetabelle.Sichtbar`; Marke in der Kopfleiste; Kopf *Entwicklung*; `GebaeudeImportDialog`, `RaumnutzungBlatt` mit Kennzeichen; Ressourcen de/en | `Menuepunkt.sperrgrund`, gesperrter Punkt im `Menueband`; `Dialoge/Hilfe/FernwartungDialog.razor` + Texte | `Dialoge/Hilfe/UeberDialog.razor` löst `Versionsmeldung` ab; Geste |
| `EPOS.UI.Daten` | Gaben der beiden Importhüllen | `Hilfe/FernwartungHuelle.cs` | `Hilfe/UeberHuelle.cs` |
| `WindowsFormsApplication1` | `Program.Main(string[] args)`, `--entwickler`; Zuschnitt des Menüs; Explorer- und DevTools-Hüllen | Fenster; die Adresse öffnet der vorhandene `WindowsDateiDienst.AdresseOeffnen` | Fenster; Zwischenablage |
| `EPOS.iOS` | `EPOS_ENTWICKLER`, `Preferences`; `Dateifilter` je Stufe | nichts — das iPad ist zurückgestellt (4.5) | Einstieg in der Startansicht |
| `Setup/` | nichts (keine Entwickler-Verknüpfung) | nichts; `AppSupportURL` zeigt auf die Support-Seite, die auf die Fernwartungsseite verweist | nichts |
| Lizenzserver | nichts | nichts (Stufe 4: `support_bis`) | nichts |
| INEKON-Betrieb (außerhalb des Repositoriums) | nichts | Vermittlungsserver, Pro-Tarif, gestalteter Client, Fernwartungsseite, Codesignatur (4.8) | nichts |
| Tests | `EntwicklerbereicheWacheTests`, `MenuebandTests` (zwei Bäume), bunit-Proben | `LizenzCtrlTests` (vier Lesarten), bunit `FernwartungDialog` gesperrt/frei und „Fernwartungsseite öffnen" ruft die Adresse | `DiagnosedatenTests`, bunit `UeberDialog` |
| Wiki | kein Eintrag (Entwicklerbereiche werden nicht dokumentiert) | „Installation und Update", „Über EPOS-Plan", neue Hilfeseite „Fernwartung" mit Infoknopf-Anker; Logbuch | „Über EPOS-Plan"; Logbuch |

Kein Schemaschritt, kein Rechenweg, Referenzlauf nicht betroffen. Die iOS-Hülle wird von Stufe 1
(Dateifilter, Umgebungsvariable) berührt, von Stufe 2 nicht (das iPad ist zurückgestellt) — ein
`ios.yml`-Lauf wäre mit Stufe 1 begründet und wird, wie immer, nur nach Rückfrage gestartet.


## 7 Entscheide

Neun Fragen sind am 11.10.2026 entschieden, AS-Q10 ist zurückgestellt (Wortlaut im Stand-Block). Die Spalte „Entscheid"
nennt die gewählte Lesart; die Spalte davor hält die Empfehlung des Entwurfs mit ihrer Begründung
fest, auch dort, wo der Entscheid abweicht.

| Nr. | Frage | Lesarten | Empfehlung des Entwurfs | Entscheid 11.10.2026 |
|---|---|---|---|---|
| **AS-Q1** | Quellen des Entwicklermodus | (a) Einstellung + Startargument + Umgebungsvariable mit HKLM-Sperre (3.1); (b) nur Startargument und Umgebungsvariable, nichts Dauerhaftes; (c) Lizenztyp „intern" als einzige Quelle | (a) — ein Baustein, vier Quellen, der Administrator geht vor; (c) scheitert am Kundenrechner, auf dem der Entwickler mit der Kundenlizenz arbeitet | **(a)** |
| **AS-Q2** | Geste zum Einschalten | (a) Tastenfolge Strg+Umschalt+Alt+E im Dialog „Über EPOS-Plan" mit Rückfrage; (b) wie (a) zusätzlich mit Kennwort (fest in den Ressourcen, kein Geheimnis vor einem Entwickler); (c) sichtbarer Haken im Einstellungsdialog | (a) — die Bereiche sind kein Geheimnis, nur kein Anwenderweg; ein Kennwort hält niemanden auf und kostet Pflege; (c) widerspricht dem Auftrag | **(a)** |
| **AS-Q3** | Wo verborgene Elemente stehen | (a) am Fachort (Knopf im Importdialog bleibt dort, nur verborgen) und eigener Kopf *Entwicklung* für Werkzeuge ohne Fachort; (b) alles in den Kopf *Entwicklung* | (a) — der Projektdatei-Knopf gehört neben die Dateizeile, sonst bricht der Bedienweg des Imports | **(a)** |
| **AS-Q4** | Welche Kandidaten aus 3.4 Entwicklerbereich werden | je Zeile ja/nein | wie die Spalte „Empfehlung" in 3.4 — fünf werden Entwicklerbereich (Wiederherstellen im Katalogabgleich, KI-Werkzeuge, IDS-Zusage, Zapfprofil-Rechenvorgaben, WebView-Entwicklerwerkzeuge), vier bleiben Anwender, die Prüfhaken bleiben Build-Schalter | **wie empfohlen, mit einer Ausnahme (Entscheid 11.10.2026):** die Zapfprofil-Rechenvorgaben werden Anwenderrubrik „Zapfprofil" („sollte enthalten sein ohne Entwickler-Abhängigkeit") — vier werden Entwicklerbereich, fünf bleiben Anwender; im Code umgesetzt |
| **AS-Q5** | Bestand aus dem Modus ohne Modus | (a) lesbar und „Neu lesen" möglich, nur Neuanlage verborgen; (b) alles zur Projektdatei verborgen | (a) — verborgene Daten wären verwaiste Daten; die Herkunft bleibt sichtbar | **(a)** |
| **AS-Q6** | Protokolle und Supportpaket | (a) `ki_aktionen.txt` wandert nach `Logs\`, Supportpaket als ZIP ohne Projektdaten (Stufe 3b); (b) nur Diagnosedaten kopieren, kein Paket | (a) — ein Ordner, eine Datei an die E-Mail; der KI-Auszug nur mit eigenem Haken | **(a)** |
| **AS-Q7** | Fernwartungswerkzeug | (a) TeamViewer personalisiertes QuickSupport-Modul; (b) AnyDesk Custom Client; (c) Windows Quick Assist; (d) RustDesk | (a) — Bestand bei INEKON, ohne Installation beim Kunden, Service-Warteschlange; (c) als Textzeile | **(d) RustDesk** — eigener Server, keine Lizenzkosten je Helfer, quelloffener Client; Folgen in 4.4 und 4.8; dazu „kein TeamViewer als Support" im Wiki |
| **AS-Q8** | Lizenzbedingung | (a) schreibfähige Lizenz; (b) bezahlte Lizenz, Laufzeit egal; (c) jede Lizenz; (d) Supportvertrag im Token | (b), (d) als Ausbau (Stufe 4) | **(b)** |
| **AS-Q9** | Bereitstellung des Fernwartungsprogramms | (a) Laden bei Bedarf mit Manifest, Prüfsumme, Signaturprüfung, 30-Tage-Ablage; (b) im Setup; (c) nur Link | (a) mit (c) als Zweitweg | **(c)** — Link auf die Fernwartungsseite, für Windows und iOS derselbe Knopf; (a) und (b) bleiben als Ausbau möglich (4.4) |
| **AS-Q10** | iOS | (a) Kontakt und Diagnosedaten, Fernwartung benannt abgelehnt; (b) zusätzlich die App des Fernwartungsdienstes öffnen | (a) — erst, wenn Bildschirm-Ansehen am iPad als Supportweg angeboten wird, kommt (b) | **zurückgestellt** — „stelle iPad-Support (remote) zurück, wird später bearbeitet"; der Befund für dann steht in 4.5 (mit RustDesk ist (b) gegenstandslos) |

Noch nicht gefragt, weil sie keinen Entscheid brauchen: der Name des Menüpunkts („Fernwartung…",
en „Remote support…"), die Lage im Hilfemenü (nach Dokumentation), die Fußleisten (Hausregel).


## 8 Stufenplan und Abnahme

| Stufe | Inhalt | Abnahme |
|---|---|---|
| **1 — Entwicklermodus und Projektdatei** | `Entwicklermodus`, `Freigabestufe` am Menüpunkt, `Menuetabelle.Sichtbar`, Marke, Kopf *Entwicklung* mit den sieben Punkten (3.5), die drei Einstiege der Projektdatei, iOS-Dateifilter, KI-Feldkarten, `Program.Main(args)` | Kern-Filter grün; `EntwicklerbereicheWacheTests`, `MenuebandTests` mit zwei Bäumen; Windows-Build auf Linux (Schale berührt); Sichtprobe: ohne Modus kein Projektdatei-Knopf und kein Kopf *Entwicklung*, mit `--entwickler` beides und die Marke; HKLM-Sperre schlägt alles — **umgesetzt** (11.10.2026, #974) |
| **2 — Fernwartung** | `Menuepunkt.sperrgrund`, gesperrter Punkt im `Menueband`, `FernwartungDialog` mit vier Zonen, `LizenzCtrl.FernwartungErlaubt`, Adresse der Fernwartungsseite im Kern, Ressourcen, Wiki-Quellen (Hilfeseite „Fernwartung" ohne Produktnamen), Logbuch-Entwurf — Windows; das iPad ist zurückgestellt | Kern-Filter grün; `LizenzCtrlTests` mit den vier Lesarten; bunit gesperrt/frei; Windows-Sichtprobe einer echten Sitzung gegen INEKONs Server — **umgesetzt** (11.10.2026, #985, Commit `bb594b7a`; Protokoll [`2026-10-11_AS2_Fernwartung.md`](../ueberholt/Protokolle/Administration_Support/2026-10-11_AS2_Fernwartung.md)); **offen beim Anwender (4.8):** Vermittlungsserver, Pro-Tarif oder quelloffener Weg, gestalteter Client, Fernwartungsseite, Codesignatur, Sichtprobe |
| **3 — Über und Diagnose** | `UeberDialog` löst `Versionsmeldung` ab, `Diagnosedaten`, „Kopieren", „Protokollordner öffnen", Geste; 3b Supportpaket | Kern-Filter grün; `DiagnosedatenTests`, `UeberDialogTests`; Sichtprobe: Textblock vollständig, Geste schaltet — **umgesetzt** (11.10.2026, #987, Commits `fbb0d2c1`, `98b15126`; Protokoll [`2026-10-11_AS3_Ueber_Diagnose_Supportpaket.md`](../ueberholt/Protokolle/Administration_Support/2026-10-11_AS3_Ueber_Diagnose_Supportpaket.md)): 3a Dialog und Geste im Dialog, 3b Supportpaket |
| **4 — Supportvertrag im Token** (auf Zuruf) | `support_bis` im Server, tolerantes Lesen im Client, Lesart (d) wählbar | Serverseite durch den Anwender ausgerollt; Clienttest mit Token ohne und mit Feld |

Reihenfolge: 1 → 2 → 3; Stufe 3 kann mit Stufe 2 zusammen laufen, weil der Diagnoseblock beiden
gehört. Jede Stufe ist eine Welle mit Statuszeile, Protokoll unter `ueberholt/Protokolle/` und —
wo die Schale berührt ist — dem Windows-Build auf Linux als Teil des Gates.

**Logbuch-Entwürfe** (Veröffentlichung mit dem nächsten Sammel-Upload, Versionsnummer beim
Anwender erfragen): „Unter Hilfe → Fernwartung… führt ein Weg zur Fernwartung durch den
INEKON-Support; Voraussetzung ist eine aktivierte Lizenz." — „Hilfe → Version zeigt jetzt
die Angaben, die der Support benötigt, mit ‚Diagnosedaten kopieren'." — „Unter Hilfe → Version speichert ‚Supportpaket speichern…' die Protokolle für den Support als ZIP-Datei." — „Die Rechenvorgaben des Zapfprofilgenerators stehen im Einstellungsdialog in der Rubrik ‚Zapfprofil'." Stufe 1 bekommt keinen
Eintrag (Entwicklerbereiche sind nicht Teil der Anwenderdokumentation).


## 9 Außerhalb des Geltungsbereichs

- Ein Rollen- oder Benutzermodell in der Anwendung (Konzept KI-Aufgabensteuerung: „die Lizenzstufe
  ist die einzige Rechtedimension") — dieses Papier fügt eine *Sichtbarkeitsstufe* hinzu, kein
  Rechtemodell.
- Programm-Update-Prüfung, Datenbanksicherung und -wiederherstellung aus dem Menü, Aufbewahrung
  von `DB-Backup`, Sicherung vor der Schemamigration, Integritätsprüfung im Betrieb — alles im
  Befund gesehen, alles eigene Aufträge; `BETRIEB_SQLITE.md` bleibt die Regelquelle.
- Die Lizenzverwaltung im Portal und im wp-admin (Konzept Lizenzierung § 3) und das Lesemodus-Verhalten.
- Ein allgemeines Logging-Rahmenwerk; das Ausnahmeprotokoll genügt dem Supportfall, mehr wäre ein
  eigener Entscheid.


## 10 Verwandte Papiere

- [`EPOS-Plan_Konzept_Lizenzierung.md`](EPOS-Plan_Konzept_Lizenzierung.md) — Lizenzmodell, Token, Server, § 7 Datenschutz.
- [`Konzept_Setup_InnoSetup_EPOS-Plan.md`](Konzept_Setup_InnoSetup_EPOS-Plan.md) — Auslieferung, Katalogpaket (6.5), `AppSupportURL`.
- [`Konzept_Hilfesystem_Wikidokumentation.md`](Konzept_Hilfesystem_Wikidokumentation.md) — Hilfeseiten, Infoknöpfe, Logbuchregel 13.4.
- [`Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md`](Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md) — Feldkarten, benannte Absage.
- [`Konzept_Administrationsdialoge_Neuordnung_EPOS-Plan.md`](Konzept_Administrationsdialoge_Neuordnung_EPOS-Plan.md) — Schloss (AD-Q15), Auswahlleiste.
- [`Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md`](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) — Kapitel 16, die Projektdatei des CAD-Programms.
- [`BETRIEB_SQLITE.md`](BETRIEB_SQLITE.md) — Ablage, Sicherung, Wiederherstellung.

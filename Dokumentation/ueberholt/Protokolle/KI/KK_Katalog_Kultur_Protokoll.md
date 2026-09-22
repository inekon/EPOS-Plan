# KK — KI-Katalog je Kultur (Protokoll, 22.09.2026)

Statuszeile #430 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Vorgänger
[`KIF8_Oeffnungswege_Protokoll.md`](KIF8_Oeffnungswege_Protokoll.md) (Befund „`KiDialoge.Katalog` friert die Anzeigenamen
in der Sprache des ersten Zugriffs ein", Nach #428 e). Zweig `ki-katalog-kultur`, Commit `59a4b577`, Merge `df15b5e7`.
Anlass: Der Windows-Lauf 35717429094 auf `main` (Push des Stands #429 per Fast-Forward, Anwender 22.09.2026) war rot mit
drei Fällen `KiDialogkatalogTests.Der_Modulkatalog_deklariert_genau_die_Felder_seines_Profils` (Photovoltaik,
Stromspeicher, Wechselrichter); der Kern-Lauf 35717424395 auf Ubuntu war grün. Kein Anwenderentscheid nötig —
Bestandsfehler seit #424, orchestriert behoben.

## Ursache

`KiDialoge.Katalog` hielt einen Katalog je Prozess (`_katalog`, doppelt geprüftes `lock`). Die `KiDialog`-Objekte
tragen fertige, übersetzte Anzeigenamen aus `MyResource.Resource`, gelesen in der Kultur des ersten Zugriffs und danach
eingefroren. Zwei Folgen: Im Programm folgte der Katalog des Hilfe-Assistenten keinem Sprachwechsel zur Laufzeit; im
Testlauf entschied die Reihenfolge der Fälle über die Sprache des Katalogs. `KiDialogkatalogTests` pinnte keine Kultur
und verglich Profilbeschriftungen aus `Resource.ResourceManager.GetString` (aktuelle Kultur) mit Katalognamen der
eingefrorenen Kultur — auf dem Windows-Läufer (en-US) rot, auf Ubuntu grün. Die Welle KI‑F8 hatte dieselbe Falle lokal
mit einer Vorwärmung des Katalogs unter `de-DE` im Konstruktor von `SpeicherZeitreihenDialogEnglischTests` abgefangen.

## Änderung

- `EPOS.Kern/Allgemein/KI/Dialoge/KiDialoge.cs`: `_katalog`/`_sperre` ersetzt durch
  `ConcurrentDictionary<string, KiDialogKatalog> _kataloge` (`StringComparer.OrdinalIgnoreCase`); `Katalog` liefert
  `_kataloge.GetOrAdd(Kulturschluessel(), _ => Erzeuge())`. Der Schlüssel ist
  `(MyResource.Resource.Culture ?? CultureInfo.CurrentUICulture).Name` — genau die Kultur, mit der `Resource.Designer.cs`
  seine Texte auflöst (`ResourceManager.GetString(name, resourceCulture)`, `Resource.Culture` wird im ganzen Baum nie
  gesetzt). Katalog und Ressourcentext können damit nicht auseinanderlaufen; dieselbe Kultur liefert dieselbe Instanz.
  Öffentliche Signatur und `Erzeuge()` unverändert, Aufrufer unangetastet.
- `EPOS.UI.Tests/Dialoge/Hilfe/KiDialogkatalogTests.cs`: `IDisposable` mit `Kulturvorrichtung` (`de-DE`) — die Hausregel
  aus `EPOS.Kern/CLAUDE.md`; nicht auf `EposBunitContext` umgestellt, weil die Klasse nichts rendert.
- `EPOS.UI.Tests/Dialoge/SpeicherZeitreihenDialogTests.cs`: die Vorwärmung unter `de-DE` samt Kommentar entfernt; die
  Klasse `SpeicherZeitreihenDialogEnglischTests` bleibt der Zeuge gegen deutsche Literaltexte.
- Neu `EPOS.Kern.Tests/KiDialogkatalogKulturTests.cs` (drei Fälle): je Kultur ein Katalog mit den Namen seiner Sprache
  (festgemacht an `KI_DLG_MASKE_HEIZKESSEL` und `KI_DLG_HK_LEISTUNG_NAME`), dieselbe Kultur liefert dieselbe Instanz,
  Kulturwechsel hin und zurück liefert wieder die deutschen Namen und denselben Katalog.

## Wirkung im Programm

`StandardSprache.KulturUebernehmen` (`EPOS.Kern/Allgemein/Dienste/StandardSprache.cs`) setzt bei jedem
`Dienste.Sprache.Setzen` `CultureInfo.DefaultThreadCurrentUICulture` und die Fadenkultur auf `de-DE` bzw. `en-US`; der
nächste Zugriff auf `KiDialoge.Katalog` bildet den anderen Schlüssel und bekommt den Katalog der neuen Sprache — beim
ersten Mal gebaut, danach aus dem Zwischenspeicher. Der Assistent nennt Masken und Felder damit nach einem Sprachwechsel
in der neuen Sprache (Logbuch-Satz im Update-Papier).

## Nachweis

- Gefilterter Lauf (`KiDialogkatalog|KiRegister|KiFeldSetzen|SpeicherZeitreihen|KiDialoge|Kultur`) unter `de-DE`:
  298 grün; derselbe Lauf mit UI-Kultur `en-US`: 298 grün, zahlengleich. Die Prozesskultur ließ sich nicht über die
  PowerShell-Fadenkultur setzen (`dotnet test` startet `testhost` als eigenen Prozess, der die UI-Kultur aus den
  Benutzereinstellungen liest); gewirkt hat ein Startaufhänger über `DOTNET_STARTUP_HOOKS` (kleine `Kulturhaken.dll`
  im Scratchpad, nicht im Repository), der vor jedem Testcode alle vier Kulturwerte auf `en-US` setzt — belegt durch
  „Bestanden!" → „Passed!" in der Testausgabe.
- Voller Lauf im Worktree: 10 371 grün, 1 übersprungen (Bestand), 0 rot.
- Gate #430 auf `df15b5e7` und Bau der Windows-Schale auf dem Merge-Stand: siehe Statuszeile #430.

## Offen

- **Dieselbe Falle eine Ebene weiter:** `KiAusfuehrung.Register` (`EPOS.Kern/Allgemein/KI/KiAusfuehrung.cs`, ~137–148)
  baut das Aktionsregister einmal je Instanz, und die Windows-Schale hält die Instanz prozessweit
  (`KiAusfuehrungWindows.Aktuell`); die Aktionen tragen übersetzte Anzeigenamen und Erläuterungen (`KiAktionsTexte`).
  Der Assistent nennt seine Aktionen nach einem Sprachwechsel weiter in der Startsprache — eigener Auftrag.
- Ein en-US-Lauf im Gate wäre der dauerhafte Wächter gegen Kulturfallen; der Startaufhänger liegt heute außerhalb des
  Repositoriums. Ob er als Werkzeug unter `Werkzeuge/` einzieht, ist ein Entscheid des Anwenders.

# KI‑F8 — Öffnungswege für den Assistenten und Speicher-Zeitreihen-Dialog (Protokoll, 22.09.2026)

Statuszeile #428 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Konzept
[`Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md`](../../../aktuell/Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md)
(§ 7 KI‑D‑Q8 und KI‑D‑Q9); Vorgänger [`KIF6_Strom_Berichte_Projekt_Protokoll.md`](KIF6_Strom_Berichte_Projekt_Protokoll.md)
(Befunde „Öffnen unter Windows" und „Speicher-Zeitreihen-Dialog", Nach #425 a und b) und
[`KIF4_Kosten_Wirtschaftlichkeit_Protokoll.md`](KIF4_Kosten_Wirtschaftlichkeit_Protokoll.md) (Kostenkataloge ohne
eigenes Ziel, Nach #423 b). Zwei Zweige: `ki-f8`, Commits `67f0eca1` (Windows) und `5de081b9` (iOS und Wurzel),
Merge `d5cee9f3`; `ki-f8-q9`, Commit `306e9729` (Zeitreihen-Dialog), Merge `7db61e32`. Anwenderentscheid
21.09.2026 (Empfehlung zu Q8 und Q9), Auftrag 22.09.2026: „beginne mit KI‑F8 nach Empfehlung".

## Befund vor der Welle

- `WinFormsNavigation.OeffneMaske` kannte 28 Fälle, alle `Masken.*`; die Zielschlüssel `KOSTENVERWALTUNG`,
  `ENERGIETRAEGER_VERWALTUNG`, `NUTZUNGSDAUER_VERWALTUNG`, `GESETZESKATALOG`, `KLIMADATEN`, `PROJEKT_ALS_VARIANTE`,
  `STARTSEITE` und `BERICHTE_KOSTEN` fielen auf `default` — `dialog_oeffnen` lehnte unter Windows benannt ab
  („kein Weg"), obwohl der Menüweg der Hauptfensterhülle (`HauptfensterHuelle.Ablauf`) jede dieser Masken öffnet.
- Auf iOS sind alle Seitenschlüssel der Katalog-, Strom- und Projektmasken Verweise auf `Masken.*` — es gab in
  `IosNavigation.Uebersetze` nichts zu übersetzen. Die Lücke lag in `AppWurzel.OeffneMaske`, deren Positivliste
  weder Klimadaten, Projektvariante, Projektkopie, Peak-Shaving noch die Stromganglinien-Verwaltung kannte.
  Die Katalogverwaltungen fehlen auf iOS bewusst (kein Menü, Haken `Katalogwege` nur unter Windows belegt).
- Der Speicher-Zeitreihen-Dialog band die Intervallkonvention über eine eigene Liste (0 „Anfang", 1 „Ende") an
  eine Aufzählung mit `Anfang = 1`: beim ersten Zeichnen stand „Ende", obwohl `Anfang` galt; „Automatisch"
  fehlte in Maske und KI-Sicht, und der Kern (`SpeicherZeitreihenImport.PruefeGrundangaben`) wies den Wert hart
  ab. Die Erkennungsregel lag privat in `GanglinienPruefung.KonventionAnwenden`.

## Öffnungswege Windows (Commit `67f0eca1`)

- `WinFormsNavigation.OeffneMaske`: acht ausdrückliche Fälle, kein Sammelfall. `Seitenschluessel.Klimadaten`,
  `.Kostenverwaltung`, `.EnergietraegerVerwaltung`, `.NutzungsdauerVerwaltung`, `.Gesetzeskatalog` und
  `.ProjektAlsVariante` rufen `HauptfensterHuelle.Aktuelle?.Springe(maske)` — denselben Ablauf wie der Menüpunkt,
  verzögert über `Blazorsprung.Verzoegert(Blazorsprung.Wirtsfenster(besitzer), …)` (Regel (b) der Hüllenschicht;
  `true` = behandelt, `false` = das Hauptfenster steht noch nicht). `Ansichten.BerichteKosten` und
  `Seitenschluessel.Startseite` gehen über `AnsichtZeigen` an die gezeichnete Wurzel (`Navigationsziel.Aktuell`),
  wörtlich der Weg von `SimulationZeigen`. Kein Öffnungscode wurde abgeschrieben.
- `HauptfensterHuelle`: `internal static Aktuelle` (Muster `StartseiteHuelle`) und `internal bool Springe(string ziel)`.
- `KiMaskenziele`: zweite Datenspalte `ARGUMENTE` mit `Argument(maskenname)` (leer = kein Argument, wie `Ziel`),
  Konstanten `REITER_ERZEUGER`, `REITER_WAERMEBEDARF`, `BLATT_KOSTEN`, `BLATT_WIRTSCHAFT`, `BLATT_UEBERSICHT`,
  `BLATT_BERICHT`; die Bemerkungen der sieben Ziele beschreiben den gültigen Stand. Argumente: die sechs
  Erzeugermasken des Projekts und `SOLARGANGLINIE` → `ERZEUGER`; `GEBAEUDE_WOHNFLAECHE`, `GEBAEUDE_BEDARF`,
  `WAERMEBEDARF_EXTERN` → `WAERMEBEDARF`; `KOSTENSEITE` → `KOSTEN`; `WIRTSCHAFTLICHKEITSSEITE`,
  `WIRTSCHAFTLICHKEIT_PARAMETER`, `TARIFSTRUKTUR`, `PV_VERGUETUNG` → `WIRTSCHAFT`; `BERICHTE_UEBERSICHT` →
  `UEBERSICHT`; `BERICHTSEITE` → `BERICHT`; `BEDARFSPROFILE` bewusst ohne Argument (dieselbe Maske geht aus
  Kacheln zweier Reiter auf).
- `KiAktionenDialog.DialogOeffnen` reicht das Argument durch; ohne Argument bleibt der Ruf einstellig.
- `AppWurzel.OeffneMaske` liest ein Textargument: bei `Startseite` als Reiterwunsch (`_reiterwunschOffen`, derselbe
  Verbrauch wie beim Rückweg), bei `BerichteKosten` als Blattwunsch für den bestehenden Parameter `Startseite`
  der `BerichteKostenSeite` — die Naht durch drei Schichten, die KI‑D‑Q8 entschieden hat.
- Wächter: `KiRegisterS3Tests` (Argumenttabelle, Weitergabe an ein Fake-`INavigation`), `KiDialogkatalogTests`
  (Zielschlüssel gegen `Seitenschluessel`, `ERZEUGER` gegen `Reiterschluessel.Erzeuger`), neue `AppWurzelTests`
  (Reiterwunsch, Blattwunsch) mit `TestProjektquelle`. Die Windows-Schale hat kein Testprojekt; Abnahme dort ist der
  Bau (0 Fehler) und die Abnahme am Gerät (A‑KI‑8).

## Öffnungswege iOS und Wurzel (Commit `5de081b9`)

- `IosNavigation.Uebersetze` bleibt ohne neuen Fall — der Klassenkommentar sagt jetzt, dass die Schlüssel unübersetzt
  durchlaufen und die Wurzel entscheidet.
- `AppWurzel` öffnet fünf Masken: Klimadaten, Projektvariante, Projektkopie, Peak-Shaving und Stromganglinien-Verwaltung
  (Positivliste, Fall in `Zeige`, Markup-Zweig, Rückweg wie bei der Energieträger-Variante). Jede holt ihren Satz über
  eine neue Naht in `IProjektQuelle` (Vorgabe `null`) und nennt bei `null` ihren eigenen Grund; `IosProjektQuelle`
  belegt alle fünf.
- Vier Hüllen sind plattformfrei nach `EPOS.UI.Daten` umgezogen (`Klimadaten/KlimadatenHuelle.cs`,
  `Projekt/ProjektKopieHuelle.cs`, `Strom/PeakShavingHuelle.cs`, `Strom/StromganglinieAdminHuelle.cs`, dazu
  `Allgemein/Katalogtexte.cs` als plattformfreier Filtertext-Übersetzer); Windows behält vier Fenster-Adapter
  (`KlimadatenFenster`, `ProjektKopieFenster`, `PeakShavingFenster`, `StromganglinieAdminFenster`) und ruft dieselbe
  Stelle. `ProjektVarianteHuelle` lag schon plattformfrei. `Kulturweitergabe` bekommt `StartenAsync<T>` für
  asynchrone Aufrufe mit Ergebnis (Wächter `ParallelitaetWacheTests` verbietet nacktes `Task.Run`).
- Die neun Katalogverwaltungen bleiben auf iOS benannt abgelehnt (`KI_AKTION_OEFFNEN_KEIN_WEG`) — die
  Katalogverwaltung fehlt auf iOS bewusst; ob sie kommen soll, ist ein eigener Entscheid (KI‑D‑Q10).

## Speicher-Zeitreihen-Dialog (KI‑D‑Q9, Commit `306e9729`)

- Alle sichtbaren Texte der Maske kommen aus Ressourcen: 32 neue Schlüssel je Sprache (`SZR_LBL_*` 16,
  `SZR_GRP_*` 3, `SZR_BTN_*` 2, `SZR_OPT_*` 11) über ein Texte-Bündel `SpeicherZeitreihenTexte` mit einem
  `[Parameter]`; die Konventionstexte sind die vorhandenen `IMPORT_KONV_*`. `KI_DLG_SZR_INTERVALL_ERL` nennt
  „Automatisch (aus der Reihe erkennen)" samt Regel.
- Die Klappliste kommt aus `GanglinienOptionenModell.Konventionswerte` und `KonventionTexte()` — Aufzählungsreihenfolge,
  „automatisch erkennen" zuerst, Id = Aufzählungswert, Setzer als reiner Cast; die Vorauswahl ist der wirkliche Wert.
  Die Vorgabe `Anfang` der `SpeicherZeitreihenOptionen` bleibt.
- Kern: neuer Helfer `SpeicherEngine/IntervallKonventionErkennung` (`Erkenne`, `Aufloesen`) trägt die Regel „eine
  Reihe mit Intervallende beginnt genau ein Intervall nach Mitternacht des 01.01." an einer Stelle;
  `GanglinienPruefung.KonventionAnwenden` ruft ihn (Verhalten unverändert, Referenzläufe unberührt),
  `SpeicherZeitreihenImport.Lesen` löst „Automatisch" am ersten Zeitstempel in der Ortszeit der Quelldatei auf
  (`ZeitzoneId`; ohne Zeitzone UTC) und schreibt den aufgelösten Wert in `SpeicherZeitreihe.Optionen`;
  `PruefeGrundangaben` weist nur noch Werte außerhalb der Aufzählung ab. `Vorschau` berührt die Konvention nicht.
- KI-Sicht: `KonventionEintraege` mit drei Einträgen in Aufzählungsreihenfolge (Eigenschaft statt Feld, weil die
  Sprache wechselt).
- Zeugen: `SpeicherZeitreihenDialogTests` (Beschriftungen aus Ressourcen, Klapplisten-Einträge, drei Konventionseinträge,
  Vorauswahl `Anfang`, Wahl setzt den Aufzählungswert samt `Automatisch`, Übernehmen mit `Automatisch`) und
  `SpeicherZeitreihenDialogEnglischTests` (unter `en-US` kein deutscher Literaltext); `SpeicherZeitreihenImportTests`
  (Ende an der ersten Viertelstunde, Anfang um Mitternacht, nur undefinierte Werte abgewiesen);
  `IntervallKonventionErkennungTests`; `LokalisierungWirtschaftlichkeitWacheTests` führt die Maske als benannte Datei.
- Ein Rot beim ersten Lauf, behoben im Test: `KiDialoge.Katalog` wird einmal je Prozess gebaut und friert seine
  Anzeigenamen in der Sprache des ersten Zugriffs ein; die en-US-Testklasse wärmt den Katalog deshalb unter `de-DE`
  vor. Produktionscode unberührt.

## Abweichungen von der Empfehlung, mit Grund

- Q8 iOS: Die Empfehlung nahm eine Übersetzung in `IosNavigation` an; die Schlüssel sind textgleich, die Arbeit lag in
  der Wurzel. Statt zwei Masken (Klimadaten, Projektvariante) öffnet die Wurzel fünf, weil auch Projektkopie,
  Peak-Shaving und Stromganglinien-Verwaltung nur Kern und belegte Dienste brauchen. Die Katalogverwaltungen bleiben
  zu — sie sind auf iOS kein Öffnungsweg, sondern eine fehlende Funktion (KI‑D‑Q10).
- Q8 Windows: Das Blatt der Berichtsansicht ging mit, weil `BerichteKostenSeite` den Parameter schon führte; die
  Fallkonstanten sind `Seitenschluessel.*`, gehalten gegen `KiMaskenziele.*` durch einen Wächter. Die fünf
  Ablehnungstexte der Wurzel sind `[Parameter]` mit deutschem Rückfall wie alle Nachbartexte der `AppWurzel`
  (Bestand: keine Schale belegt sie); der Designer blieb in diesem Teil unverändert.
- Q9: ein 32. Schlüssel für „kWh je Intervall" (Prosa, die Literal-Wache griffe sonst); Erkennung in der Ortszeit statt
  UTC (eine Berliner Reihe ab 00:15 stünde in UTC am Vortag); nur die eine Datei in die Literal-Wache, weil
  `SpeicherFlottenCsvDialog.razor` zwei nackte Ausnahmetexte führt.

## Zahlen und Abnahme

- Bau: Kern-Filter 0 Fehler; Windows-Schale (`WindowsFormsApplication1`, Debug x64) 0 Fehler; die zwei geänderten
  iOS-Dateien in einer Übersetzungsprobe gegen `EPOS.UI.Daten` fehlerfrei.
- Tests je Zweig: `ki-f8` voller Lauf 10 350 grün (1 übersprungen); `ki-f8-q9` voller Lauf 10 344 grün
  (1 übersprungen).
- Gate #428 auf `7db61e32`: siehe Statuszeile #428.
- Designer: 7 460 → 7 492 Einträge, nach beiden Merges unverändert und wiederholbar.
- Abnahme am Gerät (A‑KI‑8, Windows): siehe Nach #428.

## Offen

- KI‑D‑Q10: Katalogverwaltungen auf iOS (neun Masken) — Umzug der Windows-Hüllen wäre mechanisch nach dem Muster der
  vier umgezogenen; fachlich ein Entscheid des Anwenders.
- `SpeicherFlottenCsvDialog.razor`: derselbe Bestandsfehler bei der Zeitbasis-Konvention (Liste 0/1, „Automatisch"
  nicht wählbar, Vorauswahl „Ende") und zwei nackte deutsche Ausnahmetexte.
- Die Texte der `AppWurzel` (Meldungen und Ablehnungsgründe) belegt keine Schale — in der englischen Oberfläche deutsch.
- `KiDialoge.Katalog` friert die Anzeigenamen in der Sprache des ersten Zugriffs ein — zu prüfen, ob das auch den
  Sprachwechsel im laufenden Programm trifft; `KiDialogkatalogTests` pinnt keine Kultur.
- Klimadaten-Nachzug auf iOS: Windows frischt nach dem Schließen die Regionsliste der Startseite auf, die Wurzel lädt
  nur ihre Liste neu (fällt mit iU11 an).
- `BEDARFSPROFILE` ohne Reiterwunsch (eine Maske, zwei Reiter) — bevorzugter Platz wäre ein Entscheid.
- Soll „Automatisch" auch die Vorgabe des Zeitreihen-Dialogs werden? In dieser Welle bleibt `Anfang` die Vorgabe.

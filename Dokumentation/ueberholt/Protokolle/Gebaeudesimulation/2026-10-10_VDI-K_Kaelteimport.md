# Protokoll VDI-K — Import Kälteanlagen aus VDI 3805: nur Wärmepumpen mit Kühlfunktion (10.10.2026)

**Sitzung:** Gebäudesimulation, Zweig `gs-vdik`. Code-Commit `465c81fe6`.
**Entscheid:** E119 (10.10.2026), Wortlaut: „Für Kälteanlagen gibt es keine VDI-3805-Daten: Ändere den VDI-3805-Import im Administrationsmenü für Kälteanlagen so, dass nur kälteerzeugungsfähige Wärmepumpen aus VDI-3805-Dateien importiert werden können.“ Kühlfähigkeit nach E15 (Kühlkonzept 5.0), Kühlblockregel nach E27 (Kühlkonzept 5.1).

## 1 Gebaut

- **Menü:** „Administration → Daten & Import → Import Kälteanlagen VDI 3805 (Wärmepumpen mit Kühlfunktion)“ (`MeniItem_VDI3805_Kaelte`, Ressource `MENU_VDI3805_KAELTE`, beide Sprachen) unmittelbar hinter dem Wärmepumpenimport; eigenes Ziel `Masken.WpKaelteImport` = `Form_WP_Kaelte_einlesen` (Seitenschlüssel `WpKaelteImport`). Kein Untermenü, kein Argument.
- **Maske:** dieselbe Komponente `KatalogImportDialog` in der neuen Ausprägung `KatalogImportArt.WaermepumpeKuehlung`; die Windows-Hülle `KatalogImportHuelle` öffnet sie aus `WinFormsNavigation`. Titel „Kälteanlagen Einlesen – Wärmepumpen mit Kühlfunktion“, Zahlenspalte Kühlleistung statt thermischer Leistung, Hinweiszeile zum Filter, Hilfeschlüssel `Form_WP_Kaelte_einlesen.btn_Help` → `Gerätekataloge#import-kaelteanlagen`.
- **Kern:** `KuehlfaehigkeitsPruefung` (`EPOS.Kern/Allgemein/Import/VDI 3805/`) mit dem Befund je Satz (`KaelteimportBefund`) und dem Filter; `KatalogImportAblauf` liest im Kältemodus mit demselben Leser `WaermepumpenImport` und reicht nur die angebotenen Sätze weiter. Katalog `WP`, Ordner, Dubletten- und Aktualisierungsregel wie beim Wärmepumpenimport; Schreibweg unverändert (`Tab_WP_STAMM`, Kennlinien, `Tab_Kenndaten_Kuehlung_STAMM` mit den von `KuehlblockPruefung` angenommenen Blöcken). Im Kältemodus setzt `WaermepumpeImportSatz.NachStamm` die Kühlleistung auch ohne Zuheizung — sonst stünde das Gerät ohne Kühlleistung im Katalog und wäre nach E15 nicht kühlfähig; der Wärmepumpenimport bleibt beim Bestand.

## 2 Filterregel

Angeboten wird ein Gerät, wenn es **beide** Kriterien von E15 erfüllt: Nennkühlleistung (`700`, Feld 20) größer null **und** mindestens ein Kühlblock (`710.09`, Betriebsart 2), den `KuehlblockPruefung` annimmt. Prüfreihenfolge und Gründe der übergangenen (Protokollstufe Info, je Gerät eine Zeile, dazu eine Bilanzzeile):

1. kein Kühlblock → „keine Kühlkennlinie“;
2. alle Kühlblöcke abgelehnt → „Kühlblöcke nur in Heizlage“, „Kühlblöcke nur mit vertauschten Achsen“ oder, gemischt, „kein gültiger Kühlblock“;
3. gültige Blöcke, aber keine Nennkühlleistung → „keine Nennkühlleistung angegeben“.

Ein Gerät mit Nennkühlleistung, aber ohne Kühlblock ist nach dem Katalogkriterium von E15 kühlfähig, nach dem Rechenkriterium nicht; es wird **übergangen**, weil es keine Kälte rechnen kann. Die Kühlblockwarnungen des Lesers (`IMP_KAT_PROT_KUEHLBLOCK_*`) bleiben nur für angebotene Geräte stehen.

## 3 Zählprobe über `VDI-3805-Daten/WP-Daten/` (nicht als Test)

24 `.vdi`-Dateien von 12 Herstellern, eine davon mit Lesefehler (bestehender Leser), fünf ohne Sätze nach Blatt 22 (Proben- und Sonderdateien). 1 845 Geräte:

| Befund | Geräte |
|---|---:|
| kühlfähig mit Kühlkennlinie — angeboten | 325 |
| keine Kühlkennlinie | 1 257 |
| davon mit Nennkühlleistung > 0 | 677 |
| Kühlblöcke nur in Heizlage | 161 |
| Kühlblöcke nur mit vertauschten Achsen | 16 |
| kein gültiger Kühlblock (gemischt) | 0 |
| gültige Kühlblöcke, keine Nennkühlleistung | 86 |

Angebotene Geräte stammen von 7 der 12 Hersteller. Die Dateien liegen in mehreren Fassungen je Hersteller vor; die Zahlen zählen jede Datei für sich.

## 4 Tests

- `EPOS.Kern.Tests/WaermepumpeKaelteImportTests` (6): synthetische Blatt-22-Probe mit sieben Phantasiegeräten — je Befund eines, dazu eines ohne Zuheizung; Befund, Ablauf (2 angeboten, Bilanz 2/7/5, Gründe je Gerät, Kühlblockwarnung nur für angebotene), Wärmepumpenimport ungefiltert, Kühlleistung ohne Zuheizung, Profil.
- Nachgezogen: `KatalogImportAblaufTests` (Rückfallordner in beiden Wärmepumpenausprägungen), `StromspeicherUebernahmeTests` (Hinweis), `KatalogfilterImportTests` (sieben Importschlüssel), `DiensteTests` (Maskenschlüssel), `MenuebandTests` (69 Punkte, 55 Handlungen, Reihenfolge, Zielmenge, neuer Test zum Punkt), `KatalogImportDialogTests` (Titel, Kühlleistungsspalte, Hinweis).
- Läufe: Kern-Filter und Windows-Schale (Linux) 0 Fehler; Ressourcendesigner wiederholbar (+10 Schlüssel); betroffene Kern-Tests 148 grün, UI-Tests (Menü, Import, Seitenschlüssel, Parametersatz, Hauptfenster, Hilfe) 949 grün, `DokumentationLinkWache`, `WikiProduktdatenWache`, `HelpMapping`-Wache grün. Rechenweg unberührt.

## 5 Wiki

Repo-Quellen fortgeschrieben (nicht hochgeladen): „Programm Dokumentation – Gerätekataloge“ (Anker `import-kaelteanlagen`), „Stammdaten und Datenimport“, Hilfezuordnung in „Programm Dokumentation“. Logbuch-Entwurf: „Unter Administration → Daten & Import liest der neue Punkt ‚Import Kälteanlagen VDI 3805‘ nur Wärmepumpen mit Kühlfunktion und gültiger Kühlkennlinie aus VDI-3805-Dateien ein.“

## 6 Nachtrag: Nennkühlleistung aus der Kennlinie (VDI-K2)

**Entscheid (Anwender, 10.10.2026):** Die Geräte mit gültiger Kühlkennlinie, aber ohne Nennkühlleistung im Satz `700` (Feld 20) werden im Kälteimport aufgenommen; ihre Nennkühlleistung wird aus der Kennlinie abgeleitet.

- **Regel:** Fehlt die Nennkühlleistung (leer oder nicht größer null), gilt die **größte Kälteleistung (`Pkuehl`) der von `KuehlblockPruefung` angenommenen Kühlblöcke** als Nennkühlleistung — ohne Normbedingungen, ohne Normwerte im Code. Neuer Befund `KaelteimportBefund.KuehlfaehigAbgeleitet`; `KuehlfaehigkeitsPruefung.AbgeleiteteNennkuehlleistung` liefert den Wert. `Filtern` trägt ihn in den gelesenen Satz ein (`szKuehlleistung`), sodass Zahlenspalte, Detailfeld und Katalogwert (`WaermepumpeImportSatz.NachStamm` im Kältemodus) ihn tragen; damit erfüllt das Gerät das Katalogkriterium von E15 (`Kuehlleistung > 0`) und landet mit Kühlkennlinie im Wärmepumpenkatalog. Der Wärmepumpenimport bleibt unverändert.
- **Protokoll:** je Gerät eine Info-Zeile `IMP_KAT_PROT_KAELTE_ABGELEITET` („Nennkühlleistung aus der Kühlkennlinie abgeleitet: x kW“), dazu die Bilanzzeile `IMP_KAT_PROT_KAELTE_ABGELEITET_BILANZ` mit der Zahl (nur, wenn mindestens ein Gerät betroffen ist). Der Grund „keine Nennkühlleistung“ bleibt für den Restfall gültiger Blöcke ohne Kälteleistung größer null und heißt nun „keine Nennkühlleistung angegeben und keine Kälteleistung in der Kühlkennlinie“. Hinweiszeile der Maske in beiden Sprachen ergänzt.
- **Zählprobe** über `VDI-3805-Daten/WP-Daten/` (24 Dateien, eine mit Lesefehler, 1 845 Geräte): angeboten **325 → 411**, davon 86 mit abgeleiteter Nennkühlleistung; „gültige Kühlblöcke, keine Nennkühlleistung“ 86 → 0; die übrigen Gründe unverändert (keine Kühlkennlinie 1 257, nur Heizlage 161, nur vertauschte Achsen 16). Abgeleitete Werte 4,8 bis 12,8 kW (Median 8,8 kW), das 0,9- bis 1,9-Fache der Heizleistung (Median 1,6) — die größte Kälteleistung liegt am günstigsten Kennlinienpunkt, nicht an einem Normpunkt.
- **Tests:** `WaermepumpeKaelteImportTests` 9 (Probe um ein achtes Phantasiegerät erweitert, ohne Nennkühlleistung mit Kühlblöcken nur in Heizlage; neu: Aufnahme mit abgeleiteter Leistung samt Protokoll und unverändertem Wärmepumpenimport, Maximum nur aus angenommenen Blöcken, Übergehen bei nur Heizlage; Ablauf jetzt 3 angeboten, Bilanz 3/8/5). `KatalogImportDialogTests` (Hinweis) nachgezogen. Kern-Filter 0 Fehler; Kern-Tests (Kälteimport, Ablauf, Kühlblock, Ressourcen, `DokumentationLinkWache`, `WikiProduktdatenWache`) 152 grün, UI-Tests (Katalogimport, Ressourcen) 86 grün. Ressourcendesigner wiederholbar (+2 Schlüssel).
- **Wiki:** „Programm Dokumentation – Gerätekataloge“, Anker `import-kaelteanlagen`, um die Ableitung ergänzt (nicht hochgeladen). Logbuch-Entwurf: „Unter Administration → Daten & Import liest der neue Punkt ‚Import Kälteanlagen VDI 3805‘ nur Wärmepumpen mit Kühlfunktion und gültiger Kühlkennlinie aus VDI-3805-Dateien ein; fehlt die Nennkühlleistung, gilt die größte Kälteleistung der Kennlinie.“

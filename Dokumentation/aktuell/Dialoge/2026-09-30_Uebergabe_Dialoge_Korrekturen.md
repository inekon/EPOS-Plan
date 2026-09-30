# Übergabe der Sitzung „Dialoge und Korrekturen“ auf ein anderes Konto (30.09.2026)

Dieses Papier übergibt die Cloud-Sitzung „Dialoge und Korrekturen“ an eine neue Sitzung unter einem anderen
Claude-Konto (Anlass: Wochenbudget des abgebenden Kontos erschöpft). Es löst das Papier
[`2026-09-27_Uebergabe_Dialoge_Korrekturen_Cloud.md`](2026-09-27_Uebergabe_Dialoge_Korrekturen_Cloud.md) ab.
Regelquelle bleibt die [`CLAUDE.md`](../../../CLAUDE.md); der dauerhafte Stand steht in der
[Statusdatei](../Status_iOS_Migration.md). Bei Widerspruch gilt die Statusdatei.

## 1 Stand

- **Zweig:** Gearbeitet wird auf `ios_migration_september`; zuletzt gepusht mit #638 (Kopf und Schlussleiste der Fensterdialoge). Der
  Sitzungszweig der abgebenden Sitzung, `claude/elegant-cerf-qdr6kp`, ist damit gleich und trägt nichts
  Eigenes. Die neue Sitzung legt ihren eigenen Zweig an und pusht nach grünem Gate nach
  `ios_migration_september` (und spiegelt auf ihren Sitzungszweig).
- **Arbeitsgebiet:** Befunde des Anwenders aus der Oberfläche (Dialoge, Reiter, Diagramme), die
  Kessel-Kennlinie (abgeschlossen), Anzeige und Herleitung der Kosten und der Wirtschaftlichkeit,
  Erzeugerdialoge.
- **Basis:** `Referenzlaeufe/2026-09-30_R29_Kesseltakten` (sechzehn Projekte). Auf dem Parkzweig
  `stromverbraucher-summe` liegt eine eingefrorene R30, noch nicht übernommen (Abschnitt 3).
- **Erledigte Wellen dieser Sitzung am 30.09.2026** (alle gepusht, Einzelheiten in Statuszeile und Protokoll):

| Nr. | Gegenstand | Protokoll |
|---|---|---|
| #610 | doppelter KI-Knopf im Zapfprofil-Blatt; Regel „ein KI-Knopf je Dialog“ | — |
| #616 | Kessel-Kennlinie E1: Daten, Import Satz 710.01, Schema 156 | [`SK4`](../../ueberholt/Protokolle/Simulation/SK4_Kessel_Kennlinie_E1_Protokoll.md) |
| #617 | freier Zapfprofil-Paketteil als Ordner, selbst nachladen | — |
| #624 | Stapeldiagramme „Spitzenstunde je Stufe“ | — |
| #625 | Kessel E2 Teillast, Referenzprojekt 1050, Basis R27 | [`SK5`](../../ueberholt/Protokolle/Simulation/) |
| #627 | Kessel E2b (Brennwert in Projekten, Schritt 158) und E3 Brennwertkennlinie, Basis R28 | [`SK6`](../../ueberholt/Protokolle/Simulation/SK6_Kessel_Brennwert_E3_R28_Protokoll.md) |
| #628 | Reiter „Ergebnis“ der Simulation ganz rechts (`Reiter.Reihenfolge`) | — |
| #630 | Kessel E4 Takten, Basis R29 | [`SK7`](../../ueberholt/Protokolle/Simulation/) |
| #631 | WP-Konfiguration: gesperrter Kühlschalter ohne Haken | — |
| #635 | Kessel E5: Kurve η(β) im Editor, Kesseltafel im Vorlagenfeldkatalog v11 | [`SK8`](../../ueberholt/Protokolle/Simulation/SK8_Kessel_Kennlinie_Abschluss_Protokoll.md) |
| #636 | Heizkessel/WP: Vorlauf/Rücklauf als Gruppe mit Vorbelegung, Nutzungsdauer nur im Kostendialog | — |
| #637 | Kosten-Reiter: Knopf „Neu berechnen“ | — |
| #638 | Fensterdialoge: Kopf und Schlussleiste haften (Kopf+Fuß fest), Fensterprobe in `Proben/Rasterprobe` | — |

## 2 Entscheide des Anwenders, die weiter gelten (30.09.2026)

- **Kessel:** F1/E4 Normvorgaben für leere Felder; F2 Katalog nachpflegen; F3 η₁₀₀ aus Satz 710.01; F4
  Referenzprojekt 1050 nicht in der CI; **F5 Rückfall-Rücklauf 50 °C**; B-1 „Auch Projekte nachziehen“;
  Hinweis „Brennwertkessel ohne Kennlinie“ nur im Kesseldialog; Anfahrverlust bleibt 0,002 h × Nennleistung.
- **Vorbelegung Vorlauf/Rücklauf:** Kessel mit dem Paar der Simulation ohne Eintrag (Kessel-Datensatz, sonst
  70/50 °C); **im Temperaturbezug „fest vorgegeben“ keine Vorbelegung**; WP-Rücklauf = Vorlauf − 10 K.
- **Logbuch:** neue Sätze unter Version 1.2.0.6 (Entwurf `Dokumentation/aktuell/Wiki_Update_2026-09-26.md`);
  veröffentlicht wird nur mit dem Sammel-Upload auf Freigabe.
- **OK-Knopf langer Dialoge:** Kopf und Schlussleiste haften im eigenen Fenster, nur der Inhalt rollt — kein
  zweiter OK-Knopf, kein Tastenkürzel.
- **Wärmegestehungskosten „nur Wärmeerzeuger“:** Zähler = Annuität aus Investition (nach Zuschuss), Betrieb,
  Ersatz, Restwert der Wärmeanlagen und allgemeinen Wärmepositionen + Brennstoff samt CO₂-Abgabe + Strom von
  Wärmepumpe, Heizstab, Elektrokessel × Arbeitspreis (ohne PV-Anrechnung) − Erlöse der Wärmeerzeuger (BHKW
  inkl. Stromgutschrift für Eigenstrom); draußen Haushaltsstrom, Kühlung, PV, Stromspeicher; **Grund- und
  Leistungspreis eines geteilten Trägers anteilig nach Jahresmenge (Ja)**; Kapitalwert und übrige Kennzahlen
  bleiben projektweit.
- **Anzeige:** „nominal“ mit dem Vorzeichen des Barwerts und Kurztext; unter „Energiekosten“ je Träger
  „Menge × Preis“.
- **Weitere Aufträge:** Stromverbraucher-Mängel beheben (mit neuer Basis); Änderungszeitpunkt für Kosten und
  Preise, damit das Band „bitte neu berechnen“ auch Preisänderungen erkennt; Kalendervorlagen „Kopieren
  nach …“ bauen (Geräte ↔ Personen direkt; Heizen → Kühlen nur Zeitstruktur und Aus-Zeiten, Sollwerte neu,
  Vorgabe Komfort 26 °C).
- **Zonen beim Kopieren eines Gebäudes:** Auftrag an die Gebäudesimulation (Umfang „Alles“), in der
  Statusdatei vermerkt.
- **Budget:** Parkzweige unfertiger Arbeit dürfen unter ihrem Namen nach `origin` gepusht werden.

## 3 Offen, in dieser Reihenfolge

1. **Parkzweig `wirt-gestehung`** (HEAD `f223dcea`, über `ecd78c22`) — `faeae908` Wärmegestehungskosten nur
   Wärmeerzeuger (neue Klasse `EPOS.Kern/Allgemein/Wirtschaftlichkeit/Waermegestehung.cs`) und Herleitung
   „Menge × Preis“ je Träger auf der Seite, `ae3def8d` Nominalsumme mit Vorzeichen und Kurztext, `f223dcea`
   Wiki-Quelle. Abgenommen: Kern-Filter und Windows-Schale 0 Fehler, SQL-Prüfer 0, sieben CI-Projekte gegen R29
   byte-gleich (Gestehungskosten stehen in keiner CSV); neun rote Tests des ersten Laufs (Fassungspins,
   Report-Messlatten, Excel-Ankerzeilen) nachgezogen. **Fehlt nur der volle Testlauf auf HEAD** (ohne parallele
   Builds rund 50 Minuten; im abgebrochenen zweiten Lauf war nur `GebaeudeDialogImportTests` rot, fremde Datei,
   im ersten Lauf grün — nachprüfen). Dann mit `origin/ios_migration_september` zusammenführen, Gate,
   Statuszeile, Logbuch-Satz (Vorschlag: „Die Wärmegestehungskosten enthalten nur noch die Kosten der
   Wärmeerzeugung; Haushaltsstrom, Photovoltaik und Stromspeicher zählen nicht mehr mit. Unter den
   Energiekosten steht je Energieträger die Herleitung Menge × Preis.“), Push.
   Zahlen (€/kWh, alt → neu): 1024 0,3655 → 0,0308; 1030 0,3381 → 0,1052; reine Wärmeanlagen (1019, 1023,
   1050) unverändert; Kapitalwert unverändert. **Vor dem Merge dem Anwender vorlegen:** Die Stromgutschrift für
   BHKW-Eigenstrom zum Arbeitspreis (1024: 95,7 MWh × 0,4675 €/kWh = 44 736 €/a) drückt BHKW-Projekte stark
   nach unten — Gutschrift zum Arbeitspreis lassen oder niedriger ansetzen (z. B. Einspeisevergütung)? Weitere
   Fragen des Agenten: Stromeinspeisung (Komponente 10) zählt nur mit BHKW zur Wärme; kein Risikoabzug;
   Stromsteuer-Entlastung § 9b nicht angerechnet (BHKW-Stromsteuerbefreiung schon); bei aktivem Rollentarif
   rechnen Wärmestrom und Gutschrift zum Arbeitspreis; Herleitungszeilen bisher nur auf der Seite, nicht in
   Word/Excel (dort verschöben sie Ankerzeilen und Messlatten).
2. **Kopf und Schlussleiste (#638, gepusht)** — Marker `Fenstermarke`, den allein `BlazorDialogForm` setzt;
   30 Fensterdialoge betroffen. Offen: im Wärmepumpendialog kann die Mangelmeldung nach OK mitten im Rollweg
   außer Sicht sein (sie steht oben, nicht in der Statusspanne); schreibgeschützte Textbereiche rollt Chromium
   beim Fokus nicht ins Bild (mit und ohne Regel); Sichtprobe in der WebView2 am Windows-Gerät.
3. **Parkzweig `stromverbraucher-summe`** (`ed8d3b5b`) — Ursachen: die gepflegte Jahressumme wurde über den
   Bezeichner gesucht (1017/1047 rechneten 672 statt 15 MWh/a), der Stammsatz ohne Projektfilter gelesen.
   Behoben über `ID_Stromverbraucher`, kein Schemaschritt; Basis `2026-09-30_R30_Stromverbraucher` eingefroren
   (nur 1017/1047 anders). Weiter laut Rumpf von `ed8d3b5b`: R29 per `git rm` samt Protokoll nach
   `Dokumentation/ueberholt/Referenzbasen/`, Basisname R29 → R30 in `CLAUDE.md`, `kern.yml`, `ios.yml`,
   `Werkzeuge/Gate/LIESMICH.md`, `Dokumentation/LIESMICH.md` und den aktuell-Konzepten, Protokoll
   `SV1_Stromverbraucher_Summe_R30_Protokoll.md`, volles Gate. Vorher mit `origin` zusammenführen (andere
   Sitzungen können die Basis inzwischen berührt haben). Offene Fragen: dieselbe ID-Regel für Brauchwasser
   und Prozesswärme? neue Einfrierregel „gesäte Stromverbraucherdaten“?
4. **Änderungszeitpunkt Kosten/Preise** — noch kein Code; Plan:
   - `ErgebnisAktuell` vergleicht heute nur die Nummer des letzten Simulationslaufs; die Ergebniszeile trägt
     `Tab_ErgebnisWirtschaftlichkeit.Zeitstempel` (Ortszeit `yyyy-MM-dd HH:mm:ss`) — Trigger stempeln mit
     `datetime('now','localtime')`; verglichen wird der höchste Stempel der Vergleichsgruppe (Stamm und
     Varianten über `Tab_Variante.ID_ProjektRef`).
   - Projektstempel (Spalte `Tab_Projekt.Kosten_Geaendert`): `Tab_ProjektWerte`, `Tab_ProjektWirtschaftlichkeit`,
     `Tab_ProjektTarif`, `energy_project_settings`, `energy_price`, `Tab_ProjektPhotovoltaik`, `Tab_Preisreihe`,
     `Tab_PreisreiheDaten`, `Tab_Variante`; `Tab_Energieanlagen` INSERT/DELETE und UPDATE nur auf
     `ID_Carrier`, `KWKG_*`, `Energiesteuer_Wahl`, `Aufteilung_Methode`, `Hilfsenergie_Anteil`,
     `Kuehl_ID_Carrier`, `Kuehl_EigenerZaehler`; `Tab_Projekt` UPDATE nur auf `Emission_Berechnungsmodus`.
   - Globaler Stempel (Spalte `Tab_Applikation.Kostenkatalog_Geaendert`): `energy_carrier`, `pricing_model`,
     `energy_conversion`, `Tab_Brennstoff_Stamm`, `Tab_BrennstoffKategorien`, `emissionswert`, `emissionsart`
     (nur `co2_aequivalent`), `Tab_Gesetzesparameter`, `Tab_Kostenfaktor`, `Tab_KostenKomponente`,
     `Tab_Nutzungsdauer`, `Tab_Applikation` (nur `Emission_Berechnungsmodus`).
   - Ohne Stempel: Geräte-, Gebäude-, Einstellungstabellen (über `Tab_Projekt.Aenderungsdatum` erfasst),
     `Tab_Kraftwerkspark`, `Tab_ProjektWirkung`, Kostenvorlagen, `Tab_KostenGruppenKatalog`, alle
     Ergebnistabellen.
   - Schemaschritt mit `CREATE TRIGGER IF NOT EXISTS` (Nummer im Code messen); verdrahten in
     `SchemaStand.Zielversion`, `SchemaMigration`, `Paketanhebung`, `Werkzeuge/Testdatenbankschema`,
     `EPOS.Kern.Tests/TestDatenbank.cs`; Kernklasse für den Gruppenvergleich, in `ErgebnisAktuell` nur ein
     Aufruf; prüfen, dass `Berechne` keine gestempelte Tabelle schreibt (bisher nur
     `GesetzKatalog.StelleKatalogSicher`); Testdatenbank mit LFS; `BETRIEB_SQLITE.md`, Wiki-Quelle.
   - Offene Frage: Projektpaket einspielen und Variante anlegen setzen den Stempel — Empfehlung: so lassen.
5. **Kalendervorlagen „Kopieren nach …“** — noch nicht begonnen. Vorlagen sind je Größe
   (`KonditionierungsvorlageCtrl`, Größengleichheit „P11“); Einheiten: Heizen/Kühlen °C, Lüftung 1/h,
   Geräte/Personen Anteil 0 … 1. Die Konditionierung gehört fachlich der Gebäudesimulation (KP2) — mit deren
   Übergabepapier [`2026-09-30_Uebergabe_KP2_Abschluss.md`](../Gebaeudesimulation/2026-09-30_Uebergabe_KP2_Abschluss.md)
   abstimmen.
6. **Beim Anwender offen:** Windows-Lauf gegen die aktuelle Basis; auf Windows neue Zeilen in der Messliste der
   ChartProben (Kessel-Bilder aus #635); Sichtabnahme im Windows-Build (#628, #631, #635–#638 und die
   Parkzweige); Wiki-Sammel-Upload samt Logbuch 1.2.0.6; alte Zweige `d433600a` (#574) und
   `plattform-schwelle`; Nebenbefund `Farbpalette.Ton`.

## 4 Arbeitsweise

- **Einrichtung im neuen Container:** SDK aus `global.json` (`dotnet-install.sh --jsonfile global.json`),
  `git lfs install`, `git lfs pull --include=Referenzlaeufe/Kenndaten_Test.sqlite --exclude=""` (kein
  130-Byte-Zeiger).
- **Gate:** `Werkzeuge/Gate/gate_linux.sh` ([`LIESMICH`](../../../Werkzeuge/Gate/LIESMICH.md)), dazu die
  Windows-Schale mit `-p:EnableWindowsTargeting=true` und der `SqlDialektPruefer` bei SQL-Änderungen. Der
  volle Kern-Testlauf dauert 35–45 Minuten; lange Läufe losgelöst starten
  (`setsid nohup bash <skript> > /dev/null 2>&1 &`) und die Protokolldatei abfragen — der Container wird
  gelegentlich neu gestartet und nimmt Vordergrundläufe mit. Während das Gate läuft, nichts im Hauptbaum
  ändern.
- **Reihenfolge einer Welle:** Merge → Gate → Statuszeile (Nummer unmittelbar vorher auf `origin` messen —
  andere Sitzungen vergeben laufend Nummern) → `origin` holen und zusammenführen → Push auf
  `ios_migration_september` und den Sitzungszweig. Konflikte in `Resource*.resx` am Dateiende: beide Seiten
  vereinigen, XML prüfen, `designer_neu.py schreiben`; in `Dokumentation/LIESMICH.md`: Zählungen nach dem
  Dateistand.
- **Agenten:** Arbeitsbäume von Agenten entstehen auf `origin/main` (weit hinter dem Arbeitszweig) — im Auftrag
  als ersten Schritt `git checkout -B <zweig> <SHA des Arbeitszweigs>` verlangen. Ein Arbeitsbaum mit Build
  belegt bis 3,6 GB, der Container hat rund 25 GB — höchstens zwei bis drei Agenten gleichzeitig, fertige
  Arbeitsbäume sofort entfernen (`git worktree remove --force --force`), `bin/`/`obj/` der Werkzeuge sind neu
  baubar. Modellwahl nach `CLAUDE.md` ausdrücklich setzen. Ein Agent darf nie `/dev/null` löschen; ist es
  eine gewöhnliche Datei geworden, mit Freigabe des Anwenders `rm /dev/null && mknod -m 666 /dev/null c 1 3`.
- **Rückfragepflicht:** vor jedem macOS-, iOS- oder Setup-Lauf; kein Wiki-Upload ohne Freigabe; Push nach
  grünem Gate ohne Rückfrage.

## 5 Einstieg für die neue Sitzung

1. `CLAUDE.md` lesen, dann dieses Papier.
2. Umgebung nach Abschnitt 4 einrichten; `git fetch origin ios_migration_september wirt-gestehung
   stromverbraucher-summe`, eigenen Zweig von `origin/ios_migration_september` anlegen.
3. Stand nachlesen: `git log --format='%h %<(72,trunc)%s' -n 30 origin/ios_migration_september`, die jüngsten
   Zeilen der Statusdatei, die Rümpfe der letzten Commits der drei Parkzweige.
4. Dem Anwender Stand und offene Punkte (Abschnitt 3) melden; mit Punkt 1 beginnen, sobald er zustimmt.

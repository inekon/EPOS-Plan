# MN‑1 — Administrationsmenü ordnen, Lizenz unter Hilfe, Gesetzeskatalog mit Katalogliste

Stand: 19.09.2026 · Zweig `mn1` · Anwenderauftrag vom 19.09.2026

## Auftrag (Wortlaut des Anwenders)

> „Es gibt einen Lizenz‑Dialog für Lizenz aktivieren. Diese ist so 1. vermutlich nicht
> rechtskonform und 2. unter Hilfe→Lizenz bereits vorhanden. Evtl. kann der Dialog unter
> Hilfe übersichtlicher dargestellt werden. Generell ist die Reihenfolge der Menü‑Kategorien
> und der Unterkategorien nicht optimal (u.a. fehlt bei Kosten ein Symbol). […]
> ‚Gesetzliche Parameter‘ → kann evtl. hier raus? Gibt es einen Bezug in der Anwendung und
> Berechnung, wird das genutzt; Einstellungen ans Ende, Katalogdoubletten an andere Stelle.
> Bei der Auswahl der ‚Gesetzlichen Parameter‘ soll das gleiche Schema (Filter, Sortieren …)
> verwendet werden.“

**Entscheide MN‑Q1…Q4 (19.09.2026):**

| Kennung | Inhalt |
|---|---|
| MN‑Q1 | Ein Lizenzdialog unter Hilfe; der Administrations‑Eintrag entfällt |
| MN‑Q2 | Gesetzliche Parameter ins Untermenü **Kosten** — sie werden in der Wirtschaftlichkeit gerechnet (KWKG, EEG, Steuern, CO₂‑Preis, USt, Bilanzkonvention), nicht in Simulation oder Referenzlauf |
| MN‑Q3 | Katalog‑Dubletten ans Ende von „Daten & Import“ |
| MN‑Q4 | Reihenfolge Gebäude, Klimadaten · Kataloge · Kosten · Daten & Import · Einstellungen mit Trennstrichen; Untermenüs Bedarf vor Erzeugern, Importe in Katalogreihenfolge |

---

## Teil A — das Menü

`EPOS.UI/Bausteine/Menuetabelle.cs`. Der Kopf „Administration“ steht in **fünf Blöcken mit
vier Trennstrichen**; die Blöcke lesen sich wie der Gang eines Projekts:

| Block | Punkte |
|---|---|
| **Ort** | Gebäude (`Menue6`) → Bearbeiten, Gebäudetypen; Klimadaten (`Menu4`) |
| **Anlagen** | Wärmebedarf & Heizung (`Menu1`) → Brauchwasser, Profile & Lastgänge, Heizkessel, BHKW, Wärmepumpen, Solarkollektoren; Strombedarf & Speicher (`Menue2`); Energiesysteme (`Menu3`) |
| **Kosten** | Kosten (**neues Bild `kosten_32`**) → Kostenverwaltung…, Energieträgerverwaltung…, Nutzungsdauern (AfA)…, **Gesetzliche Parameter…** |
| **Daten & Import** | Heizkessel VDI 3805, Wärmepumpen VDI 3805, Solarthermie VDI 3805, Pufferspeicher VDI 3805, Photovoltaik (PV Module, Wechselrichter), Stromspeicher; — Trennstrich — **Katalog‑Dubletten prüfen…** |
| **Einstellungen** | Einstellungen (`einstellungen_32`) |

**Es wandert die Lage, nicht die Kennung:** Kein Name, kein Textschlüssel und kein
`Seitenschluessel` eines Bestandspunkts ändert sich. Zwei Blätter verlieren ihr eigenes Bild
(`gesetzliche_parameter_32` mit dem Umzug unter eine Rubrik), eines fällt ganz weg.

**`MenuItem_LizenzVerwaltung` entfällt ersatzlos** — der erste handelnde Punkt, den ein
Entscheid je gestrichen hat. Damit fällt auch `case Seitenschluessel.LizenzVerwaltung` in
`WindowsFormsApplication1/Views/Hauptformular/HauptfensterHuelle.cs`; der Schlüssel selbst
bleibt (er ist die Kennung der Komponente in `help_mapping.txt`), `MENU_LIZENZ_VERWALTUNG`
bleibt ungelesen im Sprachkatalog stehen.

**Zahlen (gemessen, nicht geschätzt):** 59 Punkte (46 handelnd, 13 aufklappend),
**13 Trennstriche** (8 aus dem Bestand + 4 im Kopf „Administration“ + 1 in „Daten & Import“),
10 Bilder. Die Arbeitsanweisung nannte 12 Trennstriche; der fünfte neue — der vor der
Dublettenprüfung — war darin nicht mitgezählt.

**Das Bild `kosten_32.png`** (32 × 32 RGBA) ist erzeugt, nicht gezeichnet: eine Münze mit
ausgespartem Euro‑Zeichen in der Farbe der Nachbarbilder (`#4F6A86`, gemessen aus
`einstellungen_32.png` und `lizenzen_32.png`), achtfach überabgetastet, 387 Byte. Das
Erzeugerskript lag im Scratchpad und gehört nicht ins Repository. **Es ist ein Platzhalter im
Hausstil — ein gestaltetes Symbol kann es jederzeit ersetzen, ohne dass sich eine Zeile Code
ändert.** Die zwei verwaisten PNG `gesetzliche_parameter_32` und `lizenzen_32` sind aus
`EPOS.UI/wwwroot/bilder/menue/` entfernt (kein Verwender mehr; die gleichnamigen Dateien unter
`WindowsFormsApplication1/Resources/` sind andere Dateien und bleiben).

---

## Teil B — ein Lizenzdialog unter Hilfe

**Vorher zwei Wege zur Lizenzverwaltung:** „Administration → Lizenz…“ (eigenes Fenster) und
der Knopf „Lizenz aktivieren…“ in der Fußzeile des Lizenzdialogs, der sie als Überlagerung
zeigte (Entscheid W15c‑E‑11). **Jetzt ein Weg:** Hilfe → Lizenz, **vier Registerkarten**.

| Karte | Inhalt |
|---|---|
| **Status & Aktivierung** (`LIZR_REITER_STATUS`) | der `LizenzVerwaltungDialog` als Reiterblatt |
| Lizenzvereinbarung | der verbindliche Vertragstext |
| Rechtliche Hinweise | sieben Abschnitte samt **Datenverarbeitung** |
| Komponenten | Fremdkomponenten mit Lizenzart |

- **Ein Fenster, ein Kreuz, eine Fußzeile.** Die Überlagerung entfällt; der Knopf „Lizenz
  aktivieren…“ der Fußleiste und der Knopf „Schließen“ des Blattes fallen weg
  (`LIZR_BTN_AKTIVIEREN` und `LIZ_BTN_SCHLIESSEN` bleiben ungelesen im Katalog, wie
  `LIZR_BTN_DATEI`). Der Hilfeknopf des Blattes bleibt — er hängt am eigenen Hilfeschlüssel.
- **Die drei Gruppen des Blattes stehen im Hausbaustein `Gruppenkopf`**, die zwei Eingaben im
  `Formularraster` (Hausregel „ein Parameterblock steht im Formularraster“).
- **Unmittelbar über „Jetzt aktivieren“** steht der feste Hinweis `LIZ_HINWEIS_UEBERTRAGUNG`
  samt zwei Sprüngen auf „Lizenzvereinbarung“ und „Rechtliche Hinweise“
  (`LIZ_LINK_VEREINBARUNG`, `LIZ_LINK_DATENVERARBEITUNG`). Der Sprung ist ein
  `EventCallback<string>` des Blattes (`ReiterGewuenscht`) — das Blatt kennt seinen Wirt nicht.
- **Vorwahl der Karte durch die Hülle** (`StartReiter`): ohne Token „Status & Aktivierung“,
  sonst die Vereinbarung; der Erststart (Zustimmungsmodus) immer die Vereinbarung.
- `LizenzVerwaltungHuelle.Oeffnen` entfällt, `Gaben()` bleibt.

### Die Rechtstexte (Entwurf — vom Anwender zu prüfen)

- **`LIZR_RH_A6` „Datenverarbeitung“** nennt jetzt die Übertragung an den Lizenzserver von
  epos-plan.de: Anlass (Aktivierung, Testversion, Gerätefreigabe), Daten (Lizenzschlüssel,
  E‑Mail‑Adresse, Geräte‑Hash, Programmversion, Zeitpunkt), Zweck, Rechtsgrundlage
  Art. 6 Abs. 1 lit. b DSGVO und die Feststellung, dass keine Projekt‑, Kunden‑ oder
  Simulationsdaten mitgehen. Der Rechtstext ist in **beiden** Katalogen deutsch — verbindlich
  ist die deutsche Fassung (W15c‑E‑7).
- **`LIZ_ST_NICHTAKTIVIERT`** verweist auf „Hilfe → Lizenz“ statt auf „Administration → Lizenz“.
- **`LIZ_HINWEIS_AKTIVIERUNG`** ist auf die technische Voraussetzung gekürzt („Die Aktivierung
  benötigt einmalig eine Internetverbindung.“); was übertragen wird, sagt jetzt der Hinweis
  über dem Knopf.

### Die Komponentenliste

`LIZR_KO_A2` und `LIZR_KO_A5` nennen jede ausgelieferte Fremdkomponente mit ihrer Lizenzart.
**Die Lizenzart ist gelesen, nicht geraten:** aus dem `<license>`-Element der `.nuspec` im
lokalen NuGet‑Zwischenspeicher.

| Lizenzart | Pakete |
|---|---|
| MIT | SkiaSharp, SkiaSharp.HarfBuzz, HarfBuzzSharp.NativeAssets.Win32, Microsoft.AspNetCore.Components.QuickGrid, …WebView.WindowsForms, Microsoft.Data.Sqlite, Microsoft.ML.OnnxRuntime (Lizenzdatei), Microsoft.ML.Tokenizers, System.Security.Cryptography.ProtectedData, BouncyCastle.Cryptography, ClosedXML, DocumentFormat.OpenXml, MathNet.Numerics, Humanizer.Core, WinForms.DataVisualization, JsonSchema.Net |
| Apache‑2.0 | Google.OrTools, SixLabors.Fonts 1.0.1, SQLitePCLRaw.bundle_e_sqlite3, Mscc.GenerativeAI |
| BSD‑3‑Clause | Google.Protobuf (Bestandteil der ONNX‑Laufzeit) |

---

## Teil C — Gesetzeskatalog mit der Katalogliste

`EPOS.UI/Dialoge/Wirtschaftlichkeit/GesetzeskatalogDialog.razor`: Aus dem nackten `Raster`
wird die eine `Katalogliste` des Hauses — Suche über alle Spalten, Trichter und Sortierpfeil
im Spaltenkopf, Trefferzahl, Rücksetzer.

Drei Dinge sind dabei zu wissen:

1. **Die Klasse bleibt ein Auswahlfeld davor.** Sie ist kein Spaltenfilter, sondern die Menge,
   aus der die Liste schöpft: Der Zeilendialog belegt eine neue Zeile mit ihr vor, und
   „Ändern“ lässt sie unangetastet.
2. **Der Wahlschlüssel ist die ID als Text**, nicht der Gesetzesschlüssel. Der kommt je
   Gültigkeitsjahr mehrfach vor — das ist die Kernregel dieser Maske —, und zwei Zeilen mit
   demselben Schlüssel wären für die Katalogliste **eine** Wahl.
3. **Kein „Vergleichen“.** `Katalogliste` bekommt dafür den neuen Parameter `Vergleichbar`
   (Vorgabe `true`); der Gesetzeskatalog ist der einzige Wirt, der ihn auf `false` setzt.
   Zwei Jahreszeilen desselben Satzes nebeneinanderzustellen sagt nichts, was die Liste nicht
   schon untereinander zeigt.

**Zeilenlieferung im Kern:** `GesetzKatalog.Katalogfilterzeilen(klasse)` samt den sechs
sprachneutralen Spaltenschlüsseln (`SpSchluessel`, `SpJahrVon`, `SpWert`, `SpEinheit`,
`SpStatus`, `SpQuelle`) und `KATALOG = "GESETZ"` für das `Katalogfilterregister`. Jahr und
Wert tragen **Text und Zahl** — sonst sortierte die Spalte als Zeichenkette („9“ > „10“). Der
Record `GesetzeskatalogDialog.Zeile` entfällt; `Ändern` und `Löschen` lesen aus der
`Katalogfilterzeile`, die Klasse aus der Auswahl.

Die zwei Steuermeldungen `STEUER_ENERGIEST_SATZ_FEHLT` und `STEUER_SATZ_FEHLT` nennen den Weg
jetzt als „Administration → Kosten → Gesetzliche Parameter“ (beide Sprachen).

---

## Nachweise

| Prüfung | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler, 0 Warnungen |
| Volle Suite de‑DE | 9 316 grün (EPOS.Kern 3 665, EPOS.UI 4 747, SpeicherEngine 378, SpeicherPlanung 27, KiKern 499; 1 übersprungen) |
| Volle Suite en‑US | dieselben Zahlen grün |
| Windows-Schale (`EnableWindowsTargeting=true`) | 0 Fehler |
| `ResourceDesigner` | wiederholbar, unverändert |
| `SqlDialektPruefer` | 1 505 SQL‑Texte, 0 Fundstellen |
| Referenzlauf 1030, 1007, 1017, 1045, 1046 gegen `2026-09-18_R9_Kesselbrennstoff` | 5/5 PASS, byte‑gleich |

**Nicht gelaufen:** `Proben/Rasterprobe` — Playwright ist installiert, der Chromium‑Browser
fehlt im Zwischenspeicher und ließe sich nur über das Netz holen. Die Änderung an
`Katalogliste.razor` ist ein zusätzlicher `@if`-Zweig um einen Knopf der Suchzeile; sie rührt
weder `Raster` noch `ItemSize` noch eine `.epos-raster*`-Regel an. **Die Probe ist auf einem
Rechner mit Browser nachzuholen**, bevor der Gesetzeskatalog beim Anwender ankommt.

## Offene Punkte

- **Die Rechtstexte sind Entwurf** (`LIZR_RH_A6`, `LIZ_HINWEIS_UEBERTRAGUNG`) — anwaltlich
  nicht geprüft.
- **`kosten_32.png` ist ein Platzhalter im Hausstil**; ein gestaltetes Symbol ersetzt es ohne
  Codeänderung.
- **`Katalogliste.Vergleichbar`** kommt auch mit Auftrag KL‑4; beim Merge zusammenführen.
- **Rasterprobe nachholen** (siehe oben).

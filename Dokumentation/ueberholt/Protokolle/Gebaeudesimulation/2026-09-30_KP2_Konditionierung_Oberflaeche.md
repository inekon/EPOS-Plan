# Protokoll KP2 — Konditionierungsprofile, Oberfläche und erste Kernwellen

**Datum** 29.09.2026 ff. · **Sitzung** Gebäudesimulation in der Cloud, Arbeitszweig `claude/inspiring-bell-b8wq90` ·
**Stufe** KP2 der [Konditionierungsprofile](../../../aktuell/Konzept_Konditionierungsprofile_EPOS-Plan.md) ·
**Entwurf** [`2026-09-29_Entwurf_KP2.md`](../../../aktuell/Gebaeudesimulation/2026-09-29_Entwurf_KP2.md) ·
**Entscheid** E56 ([N1.65](../../../aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)) · **Festlegungen der
Umsetzung** Entwurf Abschnitt 5, nach dem Abschluss als N1.66 · **Statuszeilen** #618 (K1, U0a, U0b)

Vorausgegangen: [Protokoll KP1b](2026-09-29_KP1b_Konditionierung_zweite_Haelfte.md) (Schemaschritt 152, Kopierwege,
Vorlagen, Nachtauskühlung). Das Protokoll wächst mit jeder Welle; jede Welle bekommt einen Abschnitt unter 2 und eine
Zeile unter 5.

## 1. Verfahren

Entwurf vor Bau wie bei KP1b: zwei Leser (Sonnet), zwei unabhängige Entwürfe — vom Arbeitsablauf des Anwenders her und
von den Lücken in Kern und Daten her — und eine Gegenprüfung (Opus); die Synthese steht im Entwurfspapier, der Entscheid
E56 beantwortet dessen fünf Fragen (alle nach Empfehlung). Umsetzung in zehn Wellen, zwei Spuren: **K** = Kern und
Daten, **U** = Oberfläche. Jede Welle arbeitet ein Agent im eigenen Worktree ab (K und U0b Opus 5, U0a Sonnet), mit
Abnahme im Worktree und einem Gate der Orchestrierung nach dem Merge.

## 2. Was gebaut ist

### K1 — Befunde „Speichern unter" und erste Kernwelle

- **„Speichern unter" (B1, B2, E27):** Der Gebäudedialog übergab den neuen Namen als Quelle der Konditionierung — die
  Hülle fand keine — und schloss danach. Jetzt ist die Quelle der Ursprungssatz, der Dialog bleibt offen und meldet
  „Katalogsatz „…" angelegt." in der Erfolgsstufe. Im Katalogmodus arbeitet er danach am neuen Satz (das nächste OK trifft
  nicht das Original), im Projektmodus bleibt der Projektzustand unberührt. Hinweistext und Wiki-Quelle „Gebäude" nennen
  das Verhalten.
- **gbXML `SollHeizenC` (Festlegung 9):** Mit angelegtem Heizkalender (Zone vor Gebäude) der häufigste endliche Wert der
  Standardwoche Mo–Fr außerhalb der Nachtzeit (E55), bei Gleichstand der höhere; „Kalender" in der Verlustliste; ohne
  Kalender wörtlich der Bestand.
- **Ferien des Zapfprofils (Festlegung 10):** Mit Heizkalender des gebundenen Gebäudes dessen FERIEN-Perioden, höchstens
  vier nach Rang, mehr mit Vorhinweis; sonst der Bestandszweig (Projekt 1045 unverändert).
- **„Zeitstruktur übernehmen" (Festlegung 11):** reine Methode `Kalenderwerkzeuge.ZeitstrukturUebernehmen`, „wie
  Heizung" oder „wie Anwesenheit", ersetzt nur die Standardwoche; Fehlfälle benannt abgelehnt.
- **B13 (Festlegung 12):** `Ferienzeit` rechnet im Gemeinjahr über `Feiertage.Gemeinjahrestag/Datum`; der 29.02. ist eine
  Fehleingabe; der falsche Kommentar in `Feiertage` ist berichtigt.
- Proben: bunit über den Dialogweg samt Hüllenfall, `GebaeudeExportKalenderTests`, `ZapfprofilFerienKalenderTests`,
  `ZeitstrukturTests`, `FerienzeitTests` — jede am Ausgangsstand rot.

### U0a — Glossar und Texte

- Glossar § 13, Block „Konditionierungsprofile" mit 63 Zeilen (Deutsch, Englisch, Anmerkung).
- Textbündel `KonditionierungTexte` mit 139 Beschriftungen (`KOND_LBL_*` 74, `KOND_BTN_*` 24, `KOND_TXT_*` 41) in beiden
  Sprachen, `KonditionierungTexteHuelle` (nicht verdrahtet), Probe `KonditionierungTexteTests` (14 Fälle, Glossartreue,
  mit fünf Störungen gegengeprobt).

### U0b — Grundlagen der Oberfläche

- **Vertrag:** DTO `KonditionierungDaten` ohne Kerntypen und Delegatenbündel `KonditionierungWeg` (je Handlung ein
  Delegat, „kein Delegat, kein Knopf"); tief kopiert, `Fassung` im Abdruck des Arbeitsstands. Nicht verdrahtet — das
  trägt K2.
- **Wochenraster:** abschaltbare Zusätze „aus" (NaN als eigener Zustand) und Umbruch nach Behälterbreite (4 × 6,
  2 × 12 ab 600 px, 7 × 24 ab 1 150 px); ohne Zusätze ist das Markup Zeichen für Zeichen das des Bestands.
- **Standardfeld `Gemeinjahrdatum`** (TT.MM. ↔ Jahrestag 1…365, 29.02. als Fehleingabe).
- **Breite Überlagerung** für den Katalogeditor in allen Modi (Festlegung 7).
- **`Modus.Admin` entfernt** (B11); die Regel „aus der Verwaltung kein Zapfprofil-Behälter" steht jetzt in
  `GebaeudeAdminHuelle`.
- **Schloss im Editor (B11):** Ein ausgelieferter Satz steht im Modus Bearbeiten mit Grundzeile, OK ist weich gesperrt
  (auch für den Assistenten), „Speichern unter" bleibt frei.
- **Wirtseite `/konditionierungsprobe`** im Rasterprobe-Wirt samt Browserprobe (390, 820, 1 180, 1 300 px). Sie fand
  drei Befunde, die bunit nicht sieht, alle behoben: Querrollen des Reiters 1 bei 390 px (Klappliste als Flexkind),
  Überlagerung beim Öffnen gerollt (Fokus ohne `preventScroll`), Zellmaß an der Umbruchschwelle (Tagesspalte 46 px).

## 3. Schemaschritte

K1, U0a und U0b tragen keinen. Die Saat der 14 Vorlagen folgt mit K3 als Schemaschritt 157 — 156 hat die Kessel-Kennlinie (#616) belegt.

## 4. Befunde der Umsetzung

- **Merge der Ressourcen:** Parallele Wellen hängen ihre Schlüssel als Block an das Ende beider `.resx`; beim Merge
  fehlte an der Naht jeweils das gemeinsame `</data>`. Nach jedem Merge: Blöcke beider Seiten übernehmen, `</data>`
  ergänzen, XML-Prüfung, Doppelprüfung der Namen, `designer_neu.py schreiben`.
- **Präfixregel mit Ausnahme:** Die Wache `ZapfprofilHuelleZ4Tests` verlangt je Hinweiskennung den Titel
  `ZPG_WARN_<Kennung>`; K1 trägt deshalb einen `ZPG_WARN_*`-Schlüssel im eigenen Block.
- **K1 × U0b — das Schloss blieb nach „Speichern unter" stehen:** K1 führt „Speichern unter" über einen eigenen Weg
  und lässt den Dialog offen; U0b hob das Schloss nur im gemeinsamen Schreibweg auf. Beide Wellen waren je für sich
  grün, erst das Gate nach dem Merge zeigte den roten Test: An der eigenen Kopie eines ausgelieferten Satzes wäre OK weich
  gesperrt geblieben, auch für den Assistenten. Behoben mit einer Zeile im Katalogzweig von `SpeichernUnterSchreiben`;
  die U0b-Probe erwartet jetzt den Ursprungssatz als Quelle und hält fest, dass das nächste OK die Kopie trifft.
- **Nebenbefunde für U1:** Hilfepille 28 × 26 px (unter 44 px); Deckel der `.epos-dialog` bei 1 160 px, Vorbild
  `.epos-wp-anlage`; `Zahlen.ZahlParsen` nimmt „NaN" und „Infinity" an (neun Aufrufer).

## 5. Nachweise

| Nachweis | Ergebnis |
|---|---|
| Abnahme K1 im Worktree | EPOS.Kern.Tests 9 127, EPOS.UI.Tests 6 927, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 grün, zusammen 2 übersprungen; Wachen 35/35; Referenzlauf GESAMT PASS, 460/460 CSV byte-gleich gegen R26, gestörter Lauf PASS; Windows-Schale 0 Fehler |
| Abnahme U0b im Worktree | EPOS.Kern.Tests 9 108, EPOS.UI.Tests 6 989, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 grün, zusammen 2 übersprungen; Wachen 35/35; Referenzlauf GESAMT PASS, 460/460 byte-gleich gegen R26, gestörter Lauf PASS; Konditionierungsprobe 20 Läufe ohne Verstoß |
| Kern-Läufe auf dem Arbeitszweig | U0a-Merge grün (36635134183), K1-Merge grün (36640302824) |
| Gate KP2a auf dem Merge mit `origin` (#611–#615) und U0b | Kern 9 210, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 grün; **UI 7 015 von 7 016 — rot der Befund K1 × U0b** (Abschnitt 4); Wachen 35/35; Referenzlauf GESAMT PASS, 460/460 byte-gleich gegen R26, gestörter Lauf PASS; ChartProben gleich der Messlatte. Nach der Behebung: UI 7 016/7 016, Windows-Schale 0 Fehler, SqlDialektPruefer 0 Fundstellen, Auslieferungsvorlage 41/41 |
| Gate KP2b auf dem Merge mit `origin` (#616, #617: Kessel-Kennlinie mit Schemaschritt 156, neue Testdatenbank) | Kern 9 269, UI 7 019, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 grün, zusammen 2 übersprungen; Wachen 35/35; ChartProben gleich der Messlatte; Referenzlauf GESAMT PASS, 460/460 CSV byte-gleich gegen R26, gestörter Lauf PASS; Windows-Schale 0 Fehler; SqlDialektPruefer 0 Fundstellen (2 135 Texte); Auslieferungsvorlage 43/43 |
| Kern-Lauf des Arbeitszweigs auf dem U0b-Merge | rot (36647131587), derselbe Test wie im Gate KP2a |
| Kern-Lauf des Schlussstands | in der Statuszeile #618 |

## 6. Offen

- **Wellen K2** (Arbeitsstand), **K3** (Saat, Schemaschritt 157), **U1** (Reiter und Matrix), **U2** (Vorlagen je Karte, danach SA1),
  **K4** (Teppichbild, Auskünfte), **U3** (Karte), **U4** (Zonen, Katalog, Verwaltung), **U5** (Abschluss, SA2).
- **Windows-Sichtprobe** der schon sichtbaren Änderungen, mit SA1: „Speichern unter" bleibt offen (Katalog und Projekt),
  breite Überlagerung des Katalogeditors, Grundzeile und weich gesperrtes OK an einem ausgelieferten Satz.
- **Wiki:** Quelle „Gebäude" nachgezogen („Speichern unter"); Upload mit dem nächsten Sammel-Upload, Logbuch-Satz in der
  Statuszeile.

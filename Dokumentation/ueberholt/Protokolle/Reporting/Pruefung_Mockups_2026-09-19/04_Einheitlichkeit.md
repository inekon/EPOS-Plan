# Prüfpunkt „Einheitliche Dialoge im Administrations-Menü und in der Wirtschaftlichkeit"

Prüfung vom 19.09.2026 · nur gelesen, nichts geändert, nicht gebaut, keine Probe gelaufen.
Alle Repo-Pfade sind relativ zu `C:\Waermeplan\EPOS-Plan`.

---

## 1 Kurzbefund

Die 21 geprüften Dialoge der Kostenverwaltung, der Wirtschaftlichkeit und der Administration
tragen zwar durchweg denselben Kopfaufbau (`epos-dialog-kopf` + `h1` + `InfoKnopf` +
`Schliesskreuz`) und dieselbe Esc-Regel, aber sie bauen die **Fußleiste auf sieben verschiedene
Arten** — nur zehn von ihnen nehmen die `SpeichernLeiste`, elf bauen eine eigene `.epos-leiste`,
und vier davon führen **kein Abbrechen**, obwohl die Hausregel es ausnahmslos verlangt.
Der Titelschalter des Kopfs existiert in **vier Bauarten** (`TitelAnzeigen`, leerer `TitelText`,
fest gezeichnet, gar kein Kopf); die drei fest gezeichneten Köpfe (Nutzungsdauern, Einstellungen,
Katalog-Dubletten) lassen sich deshalb nie in eine betitelte `Ueberlagerung` einbetten, und
`GesetzeskatalogZeileDialog` hat als einziger Dialog **keinen `InfoKnopf`**.
Gegen die Mockups fallen drei Dinge auf: Das **dunkle Kopfband `#0F1F3D`** des Hausstils § 2.7
gibt es im Stilblatt nicht (der Kopf ist unbunt, nur der Titeltext trägt die Farbe), die
**Kontextzeile „Projekt · netto"** fehlt in **allen** Wirtschaftlichkeits- und Admin-Dialogen,
und die Mockups selbst widersprechen sich: Kat. 1–4 zeigen „Abbrechen · Speichern · OK",
Kat. 5 „Abbrechen · Speichern", Kat. 6 und 8 „Abbrechen · Übernehmen", der Katalogfilter-Entwurf
„Speichern … Löschen · OK" ohne Abbrechen und an zweiter Stelle sogar „OK · Abbrechen" verdreht.
Schwerster Einzelbefund: `PhotovoltaikVerguetungDialog` verlässt sich über den Sprungknopf
„Tarif…" **ohne zu schreiben und ohne Rückfrage**, und der begleitende Hinweistext rät „bitte
vorher übernehmen" — was den Dialog schlösse; dazu kommen die verlorenen Langtitel der
Überlagerungen (aus „Kostenverwaltung BHKW 1 — Musterprojekt" wird „Kostenverwaltung").

---

## 2 Methode und geprüfte Dialoge

### 2.1 Methode

1. Menüquelle `EPOS.UI/Bausteine/Menuetabelle.cs` gelesen (die Datei ist laut ihrem eigenen Kopf
   **die** Quelle des Menüs; der WinForms-Designer ist gelöscht).
2. Zuordnung Menüpunkt → Razor-Komponente über
   `WindowsFormsApplication1/Views/Hauptformular/HauptfensterHuelle.cs:210–285` (`Ablauf`) und
   `EPOS.UI/Seiten/AppWurzel.razor` (nur `BerichteKosten` und `BhkwWirtschaftlichkeit` laufen
   über die plattformfreie Wurzel).
3. Je Komponente gezielte Greps nach den Bausteinen (`Ueberlagerung`, `Schliesskreuz`,
   `InfoKnopf`, `SpeichernLeiste`, `Rueckfrage`, `Warnbanner`, `Raster`, `Zeilenraster`,
   `Katalogliste`, `Formularraster`, `Herleitungszeile`, `Gruppenkopf`, `epos-leiste`,
   `epos-kontextzeile`) und nach den Knopftexten; Stichproben in den Quelltext.
4. Hausstil aus `Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`
   § 2.7 (Z. 460–473) gegen `EPOS.UI/wwwroot/epos-ui.css` gemessen.
5. Mockups aus `Dokumentation/aktuell/Mockups/` über die HTML-Klassen `f-band`/`f-fuss`/`f-knopf`
   bzw. `epos-dialog-kopf`/`epos-leiste` erhoben.
6. Statusliste `Dokumentation/aktuell/Status_iOS_Migration.md` § 4 (Z. 342–371) gegengelesen; wo
   ein dort genannter Befund inzwischen behoben ist, steht das unten ausdrücklich.

**Nicht geprüft:** Laufzeitverhalten (kein Start, kein Test), Farbkontraste im Browser, die
englische Ressourcenfassung Zeile für Zeile.

### 2.2 Menüwege

| # | Menüweg | Seitenschlüssel | Hülle | Razor-Komponente |
|---|---------|-----------------|-------|------------------|
| 1 | Administration → Kostenverwaltung → Kostenvorlagen | `Kostenverwaltung` | `Views/Kosten/KostenKomponenteHuelle.cs` (WinForms-Fenster) | `Dialoge/Kosten/KostenKomponenteDialog.razor` |
| 2 | Administration → Kostenverwaltung → Energieträger | `EnergietraegerVerwaltung` | `Views/Kosten/EnergietraegerFenster.cs` → `EPOS.UI.Daten/Kosten/EnergietraegerHuelle.cs` | `Dialoge/Kosten/EnergietraegerDialog.razor` |
| 3 | Administration → Kostenverwaltung → Nutzungsdauern (AfA) | `NutzungsdauerVerwaltung` | `Views/Kosten/NutzungsdauerFenster.cs` → `EPOS.UI.Daten/Kosten/NutzungsdauerHuelle.cs` | `Dialoge/Kosten/NutzungsdauerDialog.razor` |
| 4 | Administration → Gesetzliche Parameter | `Gesetzeskatalog` | `Views/Admin/GesetzeskatalogHuelle.cs` | `Dialoge/Wirtschaftlichkeit/GesetzeskatalogDialog.razor` |
| 5 | Administration → Katalog-Dubletten | `KatalogDubletten` | `Views/Admin/KatalogDublettenHuelle.cs` | `Dialoge/Admin/KatalogDublettenDialog.razor` |
| 6 | Administration → Einstellungen | `Einstellungen` | `Views/Admin/EinstellungenHuelle.cs` | `Dialoge/Admin/EinstellungenDialog.razor` |
| 7 | Projekt → Varianten und Bericht… → Reiter „Kosten" | `BerichteKosten` | `AppWurzel.razor:111–127` + `Views/BerichteKosten/…Gaben.cs` | `Seiten/Berichte/BerichteKostenSeite.razor` → `KostenSeite.razor` |
| 8 | … → Reiter „Wirtschaftlichkeit" | (Reiterblatt) | `Views/Wirtschaftlichkeit/WirtschaftlichkeitSeiteGaben.cs` | `Seiten/Berichte/WirtschaftlichkeitSeite.razor` |

**Kein eigener Menüweg** (nur über einen Wirt erreichbar):
`VorlagenPositionDialog`, `VorlagenUebernahmeDialog`, `CaseEingabeDialog`,
`KostenfaktorKatalogDialog` (Knopf im Kostendialog, `:399`),
`EmissionskatalogDialog`, `KostenprofilDialog`, `LeistungspreisReiheDialog`,
`SpotpreisImportDialog`, `EnergietraegerVarianteDialog` (alle als `Ueberlagerung` im
Energieträgerdialog, `:274–286`), `GesetzeskatalogZeileDialog`,
sowie die fünf Dialoge der Wirtschaftlichkeitsseite
(`PhotovoltaikVerguetungDialog`, `BhkwWirtschaftlichkeitDialog`, `TarifstrukturDialog`,
`WirtschaftlichkeitParameterDialog`, `KapitalwertVerlaufDialog` — Fußleiste
`WirtschaftlichkeitSeite.razor:363–392`, Überlagerungen `:395–465`).

Weiterer Einstieg: `KostenKnoepfeLeiste.razor:28–41` („Investitionskosten…",
„Betriebskosten…", „Energiekosten…") in zehn Erzeugerdialogen.

---

## 3 Merkmalsmatrix Code

Eine Zeile je Dialog, Zeilenverweis in Klammern beim ersten Auftreten des Merkmals.

| Dialog (Datei in `EPOS.UI/Dialoge/…`) | Kopf | Ktx | ⓘ | ✕ | Esc | Fuß | Knöpfe (links → rechts) | St | Rf | Raster | Filt | Hülle |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Kosten/KostenKomponenteDialog (55) | a | ✓ (67) | ✓ (60) | Ab (63) | ✓ (1270) | H (338) | Abbrechen · Speichern · **OK** | – | ✓ (419) | ZR (134) + T (217,260) | – | W |
| Kosten/VorlagenPositionDialog (29) | b | – | ✓ (34) | Ab (37) | ✓ (278) | SL (81) | Abbrechen · **OK** | – | – | F (43) | – | W |
| Kosten/VorlagenUebernahmeDialog (54) | b | ✓ (66) | ✓ (59) | Ab (62) | ✓ (570) | H (163) | **OK** · Abbrechen ⚠ | – | – | F (72) + ZR (119) | – | W |
| Kosten/EnergietraegerDialog (48) | a | ✓ (60) | ✓ (53) | Ab (56) | ✓ (1139) | H (206) | Abbrechen · Speichern · **OK** | – | ✓ (294) | F (155) | – | **D** |
| Kosten/EnergietraegerVarianteDialog (31) | b | – | ✓ (36) | Ab (39) | ✓ (181) | SL (57) | Abbrechen · **OK** | – | – | – | – | D |
| Kosten/NutzungsdauerDialog (33) | **c** | ✓ (39) | ✓+D (35) | Ab (36) | ✓ (463) | SL (155) | Speichern · Abbrechen · **OK** | ✓ | ✓ (160) | T (57) | – | **D** |
| Kosten/EmissionskatalogDialog (41) | b | ✓ (53) | ✓ (46) | Ab (49) | ✓ (973) | H (164) | Abbrechen · **OK** | – | – | R (70,115) | – | D |
| Kosten/KostenfaktorKatalogDialog (28) | b | – | ✓ (33) | Schl (36) | ✓ (241) | H (66) | Löschen · **OK** ⚠ kein Abbrechen | – | – | R (52) | – | W |
| Kosten/KostenprofilDialog (36) | b | – | ✓ (41) | Ab (44) | ✓ (484) | H (135) | Abbrechen · **OK** | – | – | F (53) | – | D |
| Kosten/LeistungspreisReiheDialog (37) | b | ✓ (49) | ✓ (42) | Ab (45) | ✓ (275) | H (73) | Löschen · Abbrechen · **Übernehmen** | – | – | – | – | D |
| Kosten/SpotpreisImportDialog (29) | b | – | ✓ (34) | Schl (37) | ✓ (311) | H (71) | Abbrechen¹ · **Übernehmen** | – | – | F (46) | – | D |
| Kosten/CaseEingabeDialog (31) | b | – | ✓ (36) | Ab (39) | ✓ (328) | SL (107) | Abbrechen · **OK** | – | – | F (58) | – | W |
| Wirt/WirtschaftlichkeitParameterDialog (51) | a | – | ✓ (56) | Ab (59) | ✓ (662) | SL (398) | Abbrechen · **Speichern** | – | – | F (68) + T (123) | – | W |
| Wirt/BhkwWirtschaftlichkeitDialog (63) | a | – | ✓ (68) | Verw (71) | ✓ (1292) | SL (430) | Abbrechen · **Speichern** | – | – | R (78) + F (131) | – | W |
| Wirt/PhotovoltaikVerguetungDialog (43) | a | – | ✓ (48) | Ab (51) | ✓ (694) | SL (230) | Abbrechen · **Übernehmen** | – | – | F (80) | – | W |
| Wirt/TarifstrukturDialog (41) | a | – | ✓ (46) | Ab (49) | ✓ (540) | SL (192) | Abbrechen · **Speichern** | – | – | F (56) | – | W |
| Wirt/KapitalwertVerlaufDialog (31) | a | – | ✓ (36) | Schl (39) | ✓ (263) | H (68) | **Schließen/Abbrechen**² ⚠ einziger Knopf | – | – | – | – | W |
| Wirt/GesetzeskatalogDialog (54) | b | – | ✓ (59) | Ab (62) | ✓ (510) | H (107) | Neu · Ändern · Löschen ⟶ **Schließen** ⚠ | – | ✓✓ (136,142) | R (85) | – | W |
| Wirt/GesetzeskatalogZeileDialog (47) | **d** | – | **–** ⚠ | – | ✓ (316) | SL (96) | Abbrechen · **OK** | – | – | F (63,83) | – | (eingebettet) |
| Admin/EinstellungenDialog (65) | **c** | – | ✓ (67) | Ab (68) | ✓ (403) | SL (187) | Abbrechen · **Speichern** | – | ✓ (190) | Reiter (85) + F (102) | – | W |
| Admin/KatalogDublettenDialog (56) | **c** | Leiste (70) | ✓ (58) | Ab (61) | ✓ (582) | H (88) | Bereinigen · Löschen · Umbenennen · Protokoll ⟶ **Schließen** ⚠ | – | ✓✓✓ (107–115) | **B** (81) | – | W |
| Seiten/Berichte/KostenSeite (50) | Seite | – | – | – | – | – | Kopfleiste oben (53,58) | – | ✓ (243) | T (77,119) | – | W |
| Seiten/Berichte/WirtschaftlichkeitSeite (363) | Seite | – | – | – | – | H (363) | PV… · BHKW… · Strombezug… · Verlauf… · **Berechnen** (+ Abbrechen im Lauf) | – | – | T | – | W |

**Legende**

- **Kopf** — Bauart des Titelschalters: `a` = `bool TitelAnzeigen`; `b` = leerer `TitelText`
  schaltet ab; `c` = **fest gezeichnet, kein Schalter**; `d` = **gar kein Kopf**; `Seite` = Seite
  ohne Dialogkopf (regelkonform, `EPOS.UI/CLAUDE.md`, Ordner `Seiten/`).
- **Ktx** — `<p class="epos-kontextzeile">`; „Leiste" = `epos-kontextleiste` mit Bedienelementen
  statt einer Textzeile.
- **ⓘ** — `<InfoKnopf Schluessel="…">`; `+D` = zusätzlich `Dialogname` gesetzt.
- **✕** — `Schliesskreuz`, Wirkung: `Ab` = Abbrechen, `Schl` = Schließen (kein Verwerfen zu
  verwerfen), `Verw` = eigener `Verwerfen`-Weg.
- **Fuß** — `SL` = Baustein `SpeichernLeiste`, `H` = handgebaute `<div class="epos-leiste">`.
- **Knöpfe** — **fett** = `epos-knopf--primaer`. ¹ `SpotpreisImportDialog:142` nennt den Parameter
  `SchliessenText`, sein Vorgabewert ist aber „Abbrechen". ² `KapitalwertVerlaufDialog:69`
  wechselt die Beschriftung im laufenden Rechenlauf.
- **St** — Statuszeile „gespeichert hh:mm" (`SpeichernLeiste.Gespeichert()`), nur wo
  `MitSpeichern="true"`.
- **Rf** — `Rueckfrage`, Anzahl der Fundstellen.
- **Raster** — `ZR` = `Zeilenraster`, `R` = `Raster<T>` (QuickGrid), `T` = handgeschriebene
  `<table class="epos-raster">`, `F` = `Formularraster`, `B` = `Baumansicht`.
- **Filt** — Suchfeld / Spaltenfilter (`Katalogliste`, `Spaltenfilter`): **in keinem** der
  geprüften Dialoge.
- **Hülle** — `D` = Datenseite plattformfrei in `EPOS.UI.Daten/Kosten/`, `W` = nur in
  `WindowsFormsApplication1/Views/`.
- ⚠ = Abweichung von der Hausregel in `EPOS.UI/CLAUDE.md` (Abschnitt „Bedienung").

**Gleich in allen Dialogen** (nicht als Spalte geführt): `epos-dialog-kopf` + `epos-dialog-titel`
als Kopfbaustein; Esc auf der Wurzel (`tabindex="-1" @ref="_wurzel" @onkeydown`), bei
`KostenKomponenteDialog:1270` und `EnergietraegerDialog:1139` zusätzlich mit der Prüfung
`UeberlagerungOffen`; `Warnbanner` als Meldungsweg; `Herleitungszeile` unter gerechneten Werten;
**keine nackten deutschen Literale im Markup** (Stichprobe über alle 30 Dateien: 0 Treffer) —
die Texte kommen über `[Parameter]`-Vorgaben bzw. `*Texte`-Bündel und werden in der Hülle mit
`Resource.*` bzw. `T(schlüssel, rückfall)` belegt.

---

## 4 Merkmalsmatrix Mockups

| Mockup / Kategorie | Kopfband | Kontext im Kopf | ⓘ | ✕ | Fußleiste | Statuszeile |
|---|---|---|---|---|---|---|
| `Dialog_Formel_Zahlenprobe.html` Kat. 1 Invest (665, 773) | `f-band` dunkel `#0F1F3D` | „Kostenverwaltung BHKW 1 — Musterprojekt Gewerbepark" | ✓ „ⓘ Hilfe" | ✓ Text „× schließt ohne zu schreiben, wie Esc" | Abbrechen · Speichern · **OK** | – (Text nennt „gespeichert {0} Uhr", 983) |
| Kat. 2 Betrieb (1025, 1118) | dito | dito | ✓ | ✓ | Abbrechen · Speichern · **OK** | – |
| Kat. 3 PV (1310/1416; 1437/1531) | dito | „Kostenverwaltung PV-Anlage 1 — …" | ✓ | ✓ | Abbrechen · Speichern · **OK** | – |
| Kat. 3 Reiter Ertrag/Bonus (1551) | dito | dito | ✓ | **–** ⚠ | **keine** ⚠ | – |
| Kat. 4 Energieträger (1691, 1783) | dito | „Energieträger — Erdgas" | **–** ⚠ | **–** ⚠ | Abbrechen · Speichern · **OK** | – |
| Kat. 4 Preishistorie (1776) | (im Leib) | — | – | – | **„💾 Speichern" in der Gruppe** ⚠ | Satz statt Statuszeile (1830) |
| Kat. 5 BHKW (2031, 2182) | dito | „BHKW-Wirtschaftlichkeit — Musterprojekt Gewerbepark" | ✓ | ✓ | Abbrechen · **Speichern** | ✓ Satz links (2183) |
| Kat. 5 Überlagerung „Sätze und Herkunft" (2320) | eigener Titel | — | – | – | Abbrechen · **Übernehmen** | ✓ (2320) |
| Kat. 6 PV-Vergütung (2761, 2876) | dito | **nur Dialogname**, kein Projekt ⚠ | ✓ | ✓ | Abbrechen · **Übernehmen** | ✓ (2877) |
| Kat. 7 Erlösrubrik / Ergebnisseite (3123) | — | — | – | – | **keine Regel** ⚠ | – |
| Kat. 8 Parameterdialog (3307, 3332) | dito | „Musterprojekt Gewerbepark" | **–** ⚠ | **–** ⚠ | Abbrechen · **Übernehmen** | ✓ (3333) |
| Kat. 8 Ergebnisseite (3340 ff.) | — | Umschalter im Kopf | – | – | vier Knöpfe: PV · BHKW · Strombezug · … | – |
| `Katalogfilter_Vorschlag.html` m1 (520, 727) | **kein Band**, heller Kopf | – | ✓ als **„?"**, nicht ⓘ ⚠ | **–** ⚠ | Speichern ⟶ Neu… · Kopieren… · Vergleichen · Löschen · **OK** — **kein Abbrechen** ⚠ | – |
| `Katalogfilter_Vorschlag.html` m2 (807, 1081) | dito | „Projekt: … · Variante: …" | ✓ „?" | – | **OK** · Abbrechen ⚠ verdreht | – |
| `Katalogfilter_Vorschlag.html` m3 (1393) | dito | – | ✓ „?" | – | wie m1 | – |
| `Wechselrichter_Mockup_2026-09-06.html` (708) | – | – | – | – | **OK** · Abbrechen ⚠ verdreht | – |
| dito (959) | – | – | – | – | **Speichern** · Schließen ⚠ | – |
| dito (1124) | – | – | – | – | **Übernehmen** · Schließen ⚠ | – |
| `stromspeicher-optimierung-v2.html` | Auslegungs**seite**, kein Dialograhmen | – | – | – | – (Seitenknöpfe) | – |
| `Entwurf_Hydraulikuebersicht_Konfiguration.html` | reines SVG-Schema | – | – | – | – | – |

**Hausstil § 2.7** (Konzept, Z. 460–473) gegen `EPOS.UI/wwwroot/epos-ui.css`:

| Vorgabe § 2.7 | Stilblatt | Befund |
|---|---|---|
| Kopfband `#0F1F3D`, Titel weiß | `.epos-dialog-kopf` (315) ohne `background`; `.epos-dialog-titel` (321) `color: var(--epos-karte-titel)` = `#0F1F3D` (90) | **kein Band** — die Farbe trägt die Schrift, nicht die Fläche |
| Vorschau-/Kennzahlstreifen `#1A3261` | kein Token mit diesem Wert | fehlt (Mockup: `--marine-hell`, 18) |
| Warnung amber `#C88A00` auf `#FFF6E0` | `--epos-warn-rahmen: rgb(200,138,0)` (47), `--epos-warn-flaeche: rgb(255,246,224)` (48) | **erfüllt** |
| Fehlerzeile Firebrick `#B22222` | `.epos-status--fehler` (581) → `--epos-senke-text: #993C1D` (42); `--epos-stufe-fehler: rgb(176,0,32)` (69) | **zwei andere Rottöne**, keiner ist Firebrick |
| Fußknöpfe 110 × 30 | `.epos-knopf` (598): `min-width: 88px`, `min-height: var(--epos-touchziel)` = 44px (110) | abweichend — 44 px ist aber die **stärkere** Hausregel (Berührungsziel) |
| `InfoKnopf` 28 × 28 | `--epos-infoknopf: 28px` (107), Höhe 666 / Mindestbreite 676 | **erfüllt** |
| `SpeichernLeiste` mit Statuszeile | Baustein vorhanden (`Bausteine/SpeichernLeiste.razor:16–33`) | **erfüllt, aber nur in 10 von 21 Dialogen genutzt** |
| Segoe UI 9 pt, Eckenradius 6 | `--epos-ecke`, `--epos-schriftgroesse-*` | nicht gemessen (bunit misst keine Maße; Browserprobe nötig) |

---

## 5 Befundtabelle

| Nr | Merkmal | Ort | Befund | Vorgeschlagene Regel / Änderung | Ziel | Schwere |
|----|---------|-----|--------|-------------------------------|------|---------|
| B01 | Fußleiste: Baustein | `Kosten/KostenKomponenteDialog.razor:338–342`, `Kosten/EnergietraegerDialog.razor:206–209`, `Kosten/VorlagenUebernahmeDialog.razor:163–166`, `Kosten/EmissionskatalogDialog.razor:164–167`, `Kosten/KostenfaktorKatalogDialog.razor:66–69`, `Kosten/KostenprofilDialog.razor:135–137`, `Kosten/LeistungspreisReiheDialog.razor:73–77`, `Kosten/SpotpreisImportDialog.razor:71–73`, `Wirt/KapitalwertVerlaufDialog.razor:68–70`, `Wirt/GesetzeskatalogDialog.razor:107–116`, `Admin/KatalogDublettenDialog.razor:88–100` | Elf Dialoge bauen die Abschlusszeile als eigene `<div class="epos-leiste">` mit `<button>`, obwohl `EPOS.UI/CLAUDE.md` sagt: „Jeder Dialog trägt OK und Abbrechen — als `SpeichernLeiste`, nie als eigene Knopfzeile." | Abschlusszeile **immer** `SpeichernLeiste`. Zusätzliche Aktionsknöpfe (Neu/Löschen/Protokoll) kommen als `ChildContent` **vor** die Statuszeile, nicht als eigene Leiste; dafür bekommt `SpeichernLeiste` einen `RenderFragment Aktionen`. | Code | **hoch** |
| B02 | Fußleiste: fehlendes Abbrechen | `Kosten/KostenfaktorKatalogDialog.razor:66–69`, `Wirt/KapitalwertVerlaufDialog.razor:68–70`, `Wirt/GesetzeskatalogDialog.razor:107–116`, `Admin/KatalogDublettenDialog.razor:88–100`, (formal) `Kosten/SpotpreisImportDialog.razor:71–73` | Vier Dialoge bieten **kein Abbrechen**, nur „Schließen"/„OK". Bei `GesetzeskatalogDialog` und `KatalogDublettenDialog` ist das bewusst, weil sie sofort schreiben — genau das verbietet die Regel („Wer vorher schreibt, darf kein Abbrechen anbieten … und deshalb schreibt keine Maske mehr vorher"). | Zwei zulässige Bauformen benennen und je Dialog eine wählen: **(K) Katalogpflege**, die sofort schreibt → ein einziger Knopf „Schließen", und **jede** ändernde Aktion trägt eine `Rueckfrage`; **(E) Eingabemaske** mit Arbeitsstand → `Abbrechen` + `OK`. Die Wahl steht im Dateikopf. | Code + Konzept | **hoch** |
| B03 | Fußleiste: Knopfreihenfolge | `Kosten/VorlagenUebernahmeDialog.razor:163–166` (Code); Mockups `Katalogfilter_Vorschlag.html:1081–1082`, `Wechselrichter_Mockup_2026-09-06.html:708–709` | Primärknopf steht **links** vom Abbrechen — umgekehrt zu `SpeichernLeiste.razor:25–32` und zu allen anderen Masken. | Reihenfolge fest: `Status ⟶ [Aktionen] · Speichern · Abbrechen · OK`, OK ganz rechts und primär. | Code + Mockup | mittel |
| B04 | Fußleiste: Beschriftung des OK-Knopfs | `Wirt/WirtschaftlichkeitParameterDialog.razor:398` („Speichern"), `Wirt/TarifstrukturDialog.razor:192` („Speichern"), `Wirt/BhkwWirtschaftlichkeitDialog.razor:430–432` („Speichern"), `Wirt/PhotovoltaikVerguetungDialog.razor:230` („Übernehmen"), `Kosten/LeistungspreisReiheDialog.razor:77` („Übernehmen"), `Kosten/SpotpreisImportDialog.razor:73` („Übernehmen"), übrige „OK" | Vier verschiedene Wörter für denselben Weg (prüfen, schreiben, schließen): **OK · Speichern · Übernehmen** — und in den Mockups zusätzlich Kat. 5 „Speichern", Kat. 6/8 „Übernehmen", Kat. 1–4 „OK". | **„OK"** überall, wo geschrieben und geschlossen wird. „Übernehmen" bleibt dem **nicht schließenden** Knopf vorbehalten (`MitSpeichern="true"`), „Speichern" dem gleichnamigen Zwischenknopf. Ausnahme nur mit begründetem Eintrag. | Code + Mockup | **hoch** |
| B05 | Ressourcenschlüssel derselben Beschriftung | `Views/Kosten/KostenKomponenteHuelle.cs:278` (`KDLG_BTN_ABBRECHEN`), `EPOS.UI.Daten/Kosten/EnergietraegerHuelle.cs:323` (`KDLG_ET_ABBRECHEN`), `EPOS.UI.Daten/Kosten/EmissionskatalogHuelle.cs:328` (`PVW_ABBRECHEN`), `EPOS.UI.Daten/Kosten/KostenprofilHuelle.cs:115` (`SIM_BTN_ABBRECHEN`), `EPOS.UI.Daten/Kosten/LeistungspreisReiheHuelle.cs:131` (`KDLG_LPR_ABBRECHEN`), `Dialoge/Wirtschaftlichkeit/GesetzeskatalogZeileDialog.razor:182` (`GESETZ_BTN_ABBRECHEN`), `Admin/KatalogDublettenDialog.razor:216` (`IMP_KONFLIKT_ABBRECHEN`) | **Sieben** Schlüssel für „Abbrechen", **vier** für „OK" (`ALLG_BTN_OK`, `KDLG_BTN_OK`, `SIM_BTN_OK`, `IMP_KONFLIKT_OK`). Jeder kann in der englischen Fassung getrennt driften. | `ALLG_BTN_OK` / `ALLG_BTN_ABBRECHEN` / `ADM_BTN_SPEICHERN` sind die **einzigen** Schlüssel der drei Standardknöpfe; die übrigen bleiben im Katalog stehen, werden aber nicht mehr gelesen (wie `MENU_PV`). | Code | mittel |
| B06 | Kopf: vier Bauarten des Titelschalters | `a`: `Kosten/KostenKomponenteDialog.razor:55`, `Kosten/EnergietraegerDialog.razor:48`, `Wirt/WirtschaftlichkeitParameterDialog.razor:51`, `Wirt/BhkwWirtschaftlichkeitDialog.razor:63`, `Wirt/PhotovoltaikVerguetungDialog.razor:43`, `Wirt/TarifstrukturDialog.razor:41`, `Wirt/KapitalwertVerlaufDialog.razor:31` — `b`: zehn Dialoge über `string.IsNullOrEmpty(TitelText)` — `c`: `Kosten/NutzungsdauerDialog.razor:33`, `Admin/EinstellungenDialog.razor:65`, `Admin/KatalogDublettenDialog.razor:56` — `d`: `Wirt/GesetzeskatalogZeileDialog.razor:47` | Vier Bauarten für dieselbe Entscheidung. Bauart `c` kann **nie** in eine betitelte `Ueberlagerung` — der Titel stünde doppelt; Bauart `d` hat gar keinen Kopf und damit keinen Hilfeknopf. Die Wache `UeberlagerungstitelTests` (`RestbefundC` ist inzwischen **leer**) sieht `c` und `d` nicht. | **Eine** Bauart: `[Parameter] bool TitelAnzeigen` (Vorgabe `true`) in **jedem** Dialog, `TitelText` bleibt die Beschriftung. Der Kopfblock wird ein Baustein `Dialogkopf` (Titel, Kontextzeile, `InfoKnopf`, `Schliesskreuz`), den jeder Dialog als eine Zeile einsetzt. Wache um `c`/`d` erweitern. | Code | **hoch** |
| B07 | Hilfeknopf fehlt | `Wirt/GesetzeskatalogZeileDialog.razor:47–57` | Einziger Dialog **ohne** `InfoKnopf` — gegen „Jeder Dialog bietet den Hilfe-Assistenten an". Der Wirt (`GesetzeskatalogDialog:119`) trägt den Titel, aber der ⓘ der Überlagerung fehlt ebenfalls. | Der Baustein `Ueberlagerung` bekommt einen optionalen `HilfeSchluessel` und zeichnet den `InfoKnopf` neben ihrem Titel; eingebettete Dialoge behalten keinen eigenen. Damit erbt jeder Unterdialog den Hilfeweg. | Code | mittel |
| B08 | `Dialogname` am Hilfeknopf | `Kosten/NutzungsdauerDialog.razor:35` (gesetzt) und `Seiten/Berichte/BerichteKostenSeite.razor:81` (gesetzt) gegen 19 weitere Fundstellen ohne | Nur zwei von 21 geben dem KI-Assistenten den Dialognamen für seine Kontextzeile mit; die übrigen lassen ihn leer. | `Dialogname` ist Pflicht und ist **derselbe Ausdruck wie das `h1`**; eine Wache prüft, dass beide aus derselben Eigenschaft kommen. | Code | gering |
| B09 | Kontextzeile „Projekt · netto" | vorhanden: `Kosten/KostenKomponenteDialog.razor:67`, `Kosten/EnergietraegerDialog.razor:60`, `Kosten/VorlagenUebernahmeDialog.razor:66`, `Kosten/NutzungsdauerDialog.razor:39`, `Kosten/EmissionskatalogDialog.razor:53`, `Kosten/LeistungspreisReiheDialog.razor:49` — **fehlt in allen sechs** Wirtschaftlichkeitsdialogen und in `Admin/EinstellungenDialog`, `Admin/KatalogDublettenDialog` | Die Mockups Kat. 5 und Kat. 8 zeigen die Projektangabe im Kopfband; im Code hat kein Wirtschaftlichkeitsdialog eine Kontextzeile. Wer aus der Wirtschaftlichkeitsseite in „BHKW…" springt, sieht nicht mehr, welches Projekt er bearbeitet. | Pflichtzeile `<p class="epos-kontextzeile">` unter jedem Dialogkopf mit dem Muster **„{Projekt} · {Variante} · netto"**; wo nichts davon zutrifft, entfällt die Zeile ersatzlos (nicht leer gezeichnet). | Code | mittel |
| B10 | Netto-Aussage als Banner statt als Kontext | `Kosten/KostenKomponenteDialog.razor:72` (`Warnbanner` Hinweis „Alle Beträge … sind NETTO." mit Schließkreuz `:73–74`) gegen Mockup Kat. 4 (1691: Kontextzeile „… · Preise netto") | Dieselbe Aussage ist einmal ein wegklickbares Banner, einmal Kopfkontext — und in den übrigen Kostendialogen gar nicht. `EPOS.UI/CLAUDE.md` verbietet dauerhafte Banner für etwas, das der Anwender nicht beheben muss. | „netto" gehört **in die Kontextzeile** (B09), das Banner fällt. Damit entfallen auch `BannerZuKurztext` und der Ausblendzustand. | Code + Mockup | mittel |
| B11 | Sprungknopf verwirft still | `Wirt/PhotovoltaikVerguetungDialog.razor:217–219` (`TarifKlick` → `:652` → `Schliessen(PvSprung.Tarif)` ohne `Speichern()`), Hinweistext `PVV_SPRUNG_HINWEIS` in `EPOS.Kern/MyResource/Resource.resx:10662` | Der Knopf „Tarif…" schließt **ohne zu schreiben** (`_gespeichert` bleibt `false`), also wie Abbrechen — der begleitende Satz sagt aber „bitte vorher übernehmen", und „Übernehmen" ist der OK-Knopf, der den Dialog seinerseits schließt. Der Anwender hat keinen Weg, der beides tut. Gegenbeispiel: `BhkwWirtschaftlichkeitDialog:1261–1263` nimmt für beide Sprünge den OK-Weg (`nurBeiAenderung: true`). | Sprungknöpfe nehmen **immer** den OK-Weg wie im BHKW-Dialog (`Schreiben(sprung, nurBeiAenderung: true)`), und die Erklärzeile sagt das („Speichert und wechselt in den Tarifdialog"). Alternative, falls der Anwender das Verwerfen will: Knopf verhält sich wie Abbrechen **und** der Text sagt „verwirft Eingaben". Der jetzige Zwischenstand ist beides nicht. | Code | **hoch** |
| B12 | Verlorene Langtitel in Überlagerungen | `Seiten/Berichte/KostenSeite.razor:213–217` (`Titel="@VerwaltungTitel"` = „Kostenverwaltung") gegen `Views/Kosten/KostenKomponenteHuelle.cs:450–455` (`KDLG_TITEL_PROJEKT` = „Kostenverwaltung {0} — {1}"); ebenso `Seiten/Berichte/WirtschaftlichkeitSeite.razor:395–465` (Titel = Knopftext „Photovoltaik…", „BHKW-Wirtschaftlichkeit…", „Parameter…", „Verlauf…") gegen die Dialogtitel („PV-Vergütung (EEG)", Mockup 2761) | Folge des Entscheids „kein doppelter Titel" (Status „Nach #295"): eingebettet zeigt die Überlagerung nur den knappen Knopftext; Komponente, Projekt und der richtige Fachname fallen weg. Die Mockups zeigen durchweg den **langen** Titel. | Die `Ueberlagerung` bekommt ihren `Titel` aus **derselben Quelle wie der Dialog** (`_stand.Titel` bzw. `_t.Titel`), nicht aus dem Knopftext. Wo der Wirt den Stand nicht kennt, reicht der Dialog seinen Titel über einen `TitelGemeldet`-Rückruf heraus. | Code | mittel |
| B13 | Kopfband fehlt (Hausstil § 2.7) | `EPOS.UI/wwwroot/epos-ui.css:315–327` gegen Konzept § 2.7 (Z. 460) und **alle** neun `f-band` der Mockups | Der Hausstil verlangt ein dunkles Kopfband `#0F1F3D` mit weißer Schrift; das Stilblatt zeichnet einen unbunten Kopf, in dem nur die Schrift `#0F1F3D` trägt. Jedes Mockup zeigt das Band. | Entweder **(A)** `.epos-dialog-kopf` bekommt `background: var(--epos-karte-titel)` und `color: var(--epos-gruppenkopf-text)` (die Tokens stehen bereits, 90/96) — dann gilt der Hausstil auch im Code; oder **(B)** § 2.7 wird auf den heutigen hellen Kopf umgeschrieben und die Mockups werden nachgezogen. **Empfehlung (A)** — das Band trennt Kopf von Leib und trägt Kontextzeile und ⓘ sichtbar zusammen; die Kontrastpaarung weiß auf `#0F1F3D` hält 4,5:1 mit Reserve. Änderung läuft durch `StilblattTests`. | Code **oder** Konzept | mittel |
| B14 | Fehlerfarbe ≠ Firebrick | `EPOS.UI/wwwroot/epos-ui.css:581–583` (`--epos-senke-text: #993C1D`), `:69` (`--epos-stufe-fehler: rgb(176,0,32)`) gegen § 2.7 („Fehlerzeile Firebrick `#B22222`") | Zwei Rottöne im Haus, keiner ist der im Konzept genannte. Der Kommentar an `:582` behauptet `Color.Firebrick`, der Wert ist es nicht. | Ein Token `--epos-fehler-text` für **alle** Fehlertexte (Statuszeile, Warnbanner-Fehler, Feldfehler); § 2.7 nennt denselben Wert. Ob `#B22222` oder `#B00020`, ist Anwenderentscheid (siehe § 7 F3). | Code + Konzept | gering |
| B15 | Knopfmaß 110 × 30 | `EPOS.UI/wwwroot/epos-ui.css:598–606` (`min-width: 88px`, `min-height: 44px`) gegen § 2.7 | Widerspruch zwischen Konzept (30 px hoch) und Hausregel (Berührungsziel 44 px). | § 2.7 nachziehen: „Fußknöpfe mindestens 88 × 44 (Berührungsziel), Breite wächst mit dem Text". Der Code bleibt. | Konzept | gering |
| B16 | Warnstufe für dieselbe Lage | `Kosten/VorlagenPositionDialog.razor:78` (`Warnung`), `Kosten/EnergietraegerVarianteDialog.razor:54` (`Warnung`), `Kosten/KostenfaktorKatalogDialog.razor:49` (`Fehler`), `Kosten/SpotpreisImportDialog.razor:64` (`Fehler`), `Wirt/WirtschaftlichkeitParameterDialog.razor:395` (`Fehler`), `Wirt/BhkwWirtschaftlichkeitDialog.razor:423` (`Fehler`); dagegen `_meldungStufe` variabel in sechs weiteren | Die Meldung „konnte nicht gespeichert werden" erscheint je nach Dialog amber oder rot. | Feste Zuordnung: **Fehler** = Vorgang ist gescheitert; **Warnung** = Vorgang lief, das Ergebnis ist fragwürdig; **Hinweis** = reine Erklärung. Prüfmeldungen am OK-Weg sind immer `Fehler`. | Code | gering |
| B17 | Rückfrage beim Löschen fehlt | `Kosten/KostenfaktorKatalogDialog.razor:68` → `:201` (`BeiLoeschen` ohne `Rueckfrage`), `Kosten/LeistungspreisReiheDialog.razor:75` → `:251`, `Kosten/EmissionskatalogDialog.razor:107` → `:759` und `:157` → `:892` | Vier Löschwege ohne Rückfrage; dagegen fragen `NutzungsdauerDialog:160`, `GesetzeskatalogDialog:142`, `KatalogDublettenDialog:107–115`, `EnergietraegerEinstellungen:392`, `KostenSeite:243`. | **Jedes** Löschen einer Zeile fragt über `Rueckfrage` mit `VorgabeNein="true"`; einzige Ausnahme ist das Löschen im reinen Arbeitsstand, das ein Abbrechen ohnehin zurücknimmt — dann sagt es der Werkzeugtipp. | Code | mittel |
| B18 | Kontextwechsel verwirft still | `Kosten/KostenKomponenteDialog.razor` (Komponente/Kategorie/Variante, Status „Nach #263", Z. 342) | Bekannt und offen: Ungespeicherte Eingaben gehen beim Wechsel der Komponente, der Kategorie oder der Variante ohne Frage verloren. | Vor jedem Kontextwechsel dieselbe `Rueckfrage` wie beim Verwerfen („Ungespeicherte Eingaben verwerfen?"), Vorgabe „Nein". Gilt für jeden Dialog mit Kontextleiste (`KostenKomponenteDialog:84,94`, `NutzungsdauerDialog:46,142`). | Code | mittel |
| B19 | Zeilenraster vs. QuickGrid vs. HTML-Tabelle vs. Baum | `ZR`: `Kosten/KostenKomponenteDialog.razor:134`, `Kosten/VorlagenUebernahmeDialog.razor:119` — `R`: `Kosten/EmissionskatalogDialog.razor:70,115`, `Kosten/KostenfaktorKatalogDialog.razor:52`, `Wirt/GesetzeskatalogDialog.razor:85`, `Wirt/BhkwWirtschaftlichkeitDialog.razor:78` — `T`: `Kosten/NutzungsdauerDialog.razor:57`, `Wirt/WirtschaftlichkeitParameterDialog.razor:123`, `Seiten/Berichte/KostenSeite.razor:77,119` — `B`: `Admin/KatalogDublettenDialog.razor:81` | Vier Listenbauarten in einem Fachbereich. Nur das `Zeilenraster` kennt die Abschlusszeile „+ Neue …" (`.epos-zr-neuzeile`, CSS 1921) und die graue Gerechnet-Zelle (`.epos-zr-text`, 1900), die das Mockup Kat. 1 beschreibt („editierbar ist, was weiß mit dunklem Rand steht; gerechnet ist grau"). | **Eine eingebbare Liste** ist ein `Zeilenraster`; **eine reine Auswahlliste** ist ein `Raster<T>` mit `Virtualisiert`; eine handgeschriebene `<table>` nur, wo die Spalten **zur Laufzeit** entstehen (das erlaubt `EPOS.UI/CLAUDE.md` ausdrücklich) — dann mit einem Satz Begründung im Dateikopf. | Code | mittel |
| B20 | Kein Suchfeld / Spaltenfilter | keine der 21 Dateien nutzt `Katalogliste`, `Spaltenfilter` oder `Katalograhmen`; dagegen 24 Dialoge in `Bedarf/`, `Erzeuger/`, `Waermepumpe/`, `Strom/`, `Solarthermie/`, `Klimadaten/` | Die Katalogverwaltungen der Kostenseite (Emissionsarten, Kostenfaktoren, gesetzliche Parameter, Nutzungsdauern) haben **weder Suche noch Filter noch Sortierung** — der Katalogfilter-Vorschlag (`Katalogfilter_Vorschlag.html`) ist für sie nirgends umgesetzt. Bei wachsenden Katalogen ist das der schmerzhafteste Punkt für den Anwender. | Der Spaltenfilter-Entwurf (Trichter im Spaltenkopf, **eine** Zeile darüber mit Suchfeld und Trefferzahl) wird für jede Liste mit mehr als ~20 Zeilen verbindlich; Filterstand über `Katalogfilterregister.Stand(art)` wie in `EPOS.UI/CLAUDE.md` beschrieben. Reihenfolge: Gesetzeskatalog (`GesetzeskatalogDialog:85`), Emissionskatalog (`:70`), Kostenfaktoren (`:52`), Nutzungsdauern (`:57`). | Code | **hoch** |
| B21 | Aktionsspalte: zwei Pluszeichen, Emoji im Knopftext | `Kosten/VorlagenZeile.razor:35` („＋", Vollbreite) gegen `Kosten/KostenfaktorKatalogDialog.razor:127` („➕", Emoji); `KostenfaktorKatalogDialog:130` („🗑️ Löschen"), `EnergietraegerEinstellungen.razor:581` („💾 Speichern"), `EnergietraegerHuelle.cs:2699` (`ETV_BTN_SPEICHERN` = „💾 Speichern") gegen alle übrigen Knöpfe ohne Symbol | Drei Zeichensätze für dieselbe Geste, und zwei Knöpfe tragen ein Emoji **im Ressourcentext** — das übersetzt sich nicht und ist in der Sprachausgabe Lärm. | Symbole sind `aria-hidden`-Zeichen **neben** dem Text, nie im Ressourcentext (dieselbe Regel gilt in `Zweispaltenauswahl` für ▲/▼). Ein Satz Zeichen im Haus: `✏️` ändern, `🗑️` löschen, `🔒` gesperrt, `＋` neu. | Code | gering |
| B22 | Plattformfreiheit der Datenseite | `EPOS.UI.Daten/Kosten/` führt nur `EnergietraegerHuelle`, `NutzungsdauerHuelle`, `EmissionskatalogHuelle`, `KostenprofilHuelle`, `LeistungspreisReiheHuelle`, `SpotpreisImportHuelle`; **kein** Ordner `Wirtschaftlichkeit`, **kein** Ordner `Admin` — deren Hüllen liegen in `WindowsFormsApplication1/Views/Wirtschaftlichkeit/` (6 Dateien) und `…/Views/Admin/` (5 Dateien), dazu `Views/Kosten/KostenKomponenteHuelle.cs` | 15 von 21 Dialogen haben ihre Datenseite nur in der Windows-Schale; auf iOS gibt es für sie keinen Parametersatz. `EPOS.UI/CLAUDE.md` verlangt die Hülle in `EPOS.UI.Daten`. Besonders auffällig: der **größte** Dialog der Kostenverwaltung (`KostenKomponenteHuelle.cs`) ist windowsgebunden. | Reihenfolge des Umzugs nach Nutzen: `KostenKomponenteHuelle` → `WirtschaftlichkeitParameterHuelle` → `BhkwWirtschaftlichkeitHuelle` → `PhotovoltaikVerguetungHuelle` → `GesetzeskatalogHuelle`. Der WinForms-Adapter behält nur Fenster, Titel und Größe (Vorbild: `Views/Kosten/EnergietraegerFenster.cs:38–65`). | Code | mittel |
| B23 | Doppelter Titel Fenster ↔ Dialogkopf | `Views/Kosten/EnergietraegerFenster.cs:57` (`EnergietraegerHuelle.Titel()` = „Energieträgerverwaltung") **und** `EPOS.UI.Daten/Kosten/EnergietraegerHuelle.cs:274` (`["TitelText"] = "Energieträgerverwaltung"`); ebenso `Views/Kosten/NutzungsdauerFenster.cs:46` mit `NutzungsdauerDialog.razor:34` | Im Fenstertitel **und** im `h1` steht derselbe Text. Beim Energieträgerdialog entschärft sich das, sobald ein Träger gewählt ist (`EnergietraegerDialog.razor:507–517` liefert dann „Energieträger — Erdgas"); ohne Auswahl und beim Nutzungsdauerdialog steht es wörtlich zweimal. Die Wache `UeberlagerungstitelTests` sieht nur Razor-in-Razor, nicht Fenster-in-Razor. | Die WinForms-Hülle setzt den **Fenstertitel**, der eingebettete Dialog zeichnet dann **keinen** eigenen (`TitelAnzeigen="false"`) — dieselbe Regel wie bei der `Ueberlagerung`, nur eine Ebene höher. Dann ist auch das ✕ eindeutig: es ist das des Fensters. Wache um den Fensterfall erweitern. | Code | mittel |
| B24 | Mockup widerspricht sich in der Fußleiste | `Dialog_Formel_Zahlenprobe.html`: 775–777 / 1120–1122 / 1418–1420 / 1533–1535 / 1783 (Abbrechen · Speichern · OK) gegen 2184–2185 (Abbrechen · **Speichern**) gegen 2321, 2878, 3334 (Abbrechen · **Übernehmen**); `Katalogfilter_Vorschlag.html`: 727–734 (Speichern … Löschen · OK, ohne Abbrechen) gegen 1081–1082 (OK · Abbrechen); `Wechselrichter_Mockup_2026-09-06.html`: 708–709 / 959–960 / 1124–1125 | Fünf verschiedene Fußleisten in den Entwürfen desselben Hauses. | Alle Mockups auf die Regel aus B02/B03/B04 nachziehen; im Kopf jeder Mockup-Datei steht künftig die Bauform (K oder E). | Mockup | mittel |
| B25 | Mockup: ⓘ und ✕ nur in manchen Köpfen | vorhanden: `Dialog_Formel_Zahlenprobe.html:665, 1025, 1310, 1437, 2031, 2761` — nur ⓘ: `:1551` — **keines von beiden**: `:1691` (Energieträger), `:3307` (Parameter); `Katalogfilter_Vorschlag.html:522, 809` zeigt ⓘ als **„?"** und nirgends ein ✕ | Derselbe Dialogtyp trägt den Hilfe- und Schließhinweis mal, mal nicht; und der Katalogfilter-Entwurf nutzt ein anderes Zeichen als alle übrigen. | Jeder Mockup-Kopf trägt **immer** beides, und das Zeichen ist **ⓘ** (so wie `InfoKnopf` es im Code zeichnet). Der Zusatz „× schließt ohne zu schreiben, wie Esc" gehört in den Werkzeugtipp, nicht sichtbar ins Band — er wiederholt sich sonst in jedem Fenster. | Mockup | gering |
| B26 | Mockup: Überlagerung ohne Fußleiste | `Dialog_Formel_Zahlenprobe.html:1551–1675` (Kat. 3, Reiter „Ertrag/Bonus") — Kopfband, aber keine `f-fuss` | Ein Fenster ohne Abschlusszeile; der Anwender sähe keinen Weg hinaus. | Jeder gezeichnete Fensterrahmen im Mockup bekommt seine Fußleiste, auch wenn der Text sie nicht bespricht. | Mockup | gering |
| B27 | Ergebnisseite ohne Regel | `Dialog_Formel_Zahlenprobe.html:3123 ff.` (Kat. 7) und `:3340 ff.` (Kat. 8, Ergebnisseite) gegen `Seiten/Berichte/WirtschaftlichkeitSeite.razor:363–392` | Die Ergebnisseite wird im Mockup nicht als Fenster gezeichnet und hat keine Fußleistenregel; im Code führt die Seite fünf Knöpfe plus „Berechnen" (primär) plus, im Lauf, ein zusätzliches „Abbrechen" — also bis zu **sieben**. Konzept § 2.7 nennt genau dafür die „Lücke K8" (Fußleiste voll) und entscheidet den Umschalter im Kopf. | **Eine Seite ist kein Dialog** (`EPOS.UI/CLAUDE.md`, Ordner `Seiten/`): Ihre Leiste ist eine **Aktionsleiste**, kein Abschluss — kein OK, kein Abbrechen, genau **ein** Primärknopf. Das steht so weder im Konzept noch im Mockup und gehört in beide. | Konzept + Mockup | mittel |
| B28 | Nachkommastellen ohne Regel | `Wirt/WirtschaftlichkeitParameterDialog.razor` (12× „2", 6× „1", je 1× „4"/„0"), `Wirt/TarifstrukturDialog.razor` (10× „4", 6× „2", 2× „0", 1× „3"), `Wirt/BhkwWirtschaftlichkeitDialog.razor` (4× „1", 3× „4", 2× „0"), `Kosten/NutzungsdauerDialog.razor` (4× „1") — 15 der 21 Dialoge setzen **gar keine** und nehmen die Vorgabe (höchstens vier) | Fünf verschiedene Stellenzahlen, kein erkennbares Muster je Größenart; Tausenderpunkte gibt es nirgends (bewusst, `Standards/Zahlen.cs`). | Stellenzahl hängt an der **Größe**, nicht am Dialog: Geldbeträge 2, Preise in ct/kWh 2, Faktoren und Wirkungsgrade 4, Prozentsätze 2, Jahre und Stückzahlen 0, Leistungen 1. Als Tabelle ins Konzept, im Code als benannte Konstanten in `Standards/Zahlen.cs`. | Code + Konzept | mittel |
| B29 | Tarifdialog nur über Umwege erreichbar | Status „Nach #291"/„Nach #292" (Z. 367–368); `Wirt/TarifstrukturDialog.razor` (Sicht `Komplett` ohne Wirt), `Seiten/Berichte/WirtschaftlichkeitSeite.razor:376` (Knopf nur bei `_stand.MitStrombezug`) | Bekannt und offen: In einem Wärmepumpenprojekt ohne BHKW und ohne PV ist der Tarifsatz bei inaktivem Schalter nicht mehr erreichbar — der Schalter „Aktiv" sitzt in genau dem Dialog, den man nicht öffnen kann. | Entscheid steht aus (§ 7 F5). Technisch genügt ein Menüpunkt „Kostenverwaltung → Tarifstruktur" auf die bereits vorhandene zweistellige Überladung `TarifstrukturHuelle.Oeffnen`. | Code | mittel |
| B30 | BHKW-Dialog schreibt im OK-Weg unbedingt | `Wirt/BhkwWirtschaftlichkeitDialog.razor:1248` (`BeiErgebnis` ruft `Schreiben(Keiner)` ohne `nurBeiAenderung`) gegen `:1261–1263` (die Sprungwege mit `nurBeiAenderung: true`); Status „Nach #289" (4) | Derselbe Dialog schreibt auf dem OK-Weg auch ohne Änderung, auf dem Sprungweg nur bei Änderung — zwei Regeln in einer Datei. | Eine Regel: **schreiben nur bei Änderung**, auf jedem Weg. Berührt `Gespeichert` und damit das Nachrechnen der Seite — deshalb zusammen mit einer Probe. | Code | gering |
| B31 | Ausnahme „Speichern/Schließen" ist eingeschlafen | `Wirt/BhkwWirtschaftlichkeitDialog.razor:430–432` (`MitSpeichern="false" MitAbbrechen="true"`, `OkText="Speichern"`); Status „Nach #282" (Z. 356) | Die im Auftrag genannte Ausnahme („Speichern/Schließen", `MitAbbrechen="false"`) gibt es **nicht mehr** — der Dialog trägt heute „Abbrechen · Speichern". Die Regelzeile in `EPOS.UI/CLAUDE.md` nennt ebenfalls keine Ausnahme; nur der XML-Kommentar der `SpeichernLeiste` führt sie noch. | Kommentar in `Bausteine/SpeichernLeiste.razor:51–61` bereinigen; nach B04 heißt der Knopf ohnehin „OK". | Code | gering |

---

## 6 Regelvorschlag „Hausstil Dialoge"

| Merkmal | Regel | Ausnahmen |
|---|---|---|
| Bauform | Jeder Dialog ist entweder **(E) Eingabemaske** mit Arbeitsstand und `Abbrechen`/`OK` oder **(K) Katalogpflege**, die sofort schreibt und nur `Schließen` trägt; die Bauform steht als erster Satz im Dateikopf. | keine — ein Dialog ohne erklärte Bauform ist ein Befund |
| Kopfblock | Ein Baustein `Dialogkopf`: Titel (`h1`), Kontextzeile, `InfoKnopf`, `Schliesskreuz` — in dieser Reihenfolge, auf dunklem Band `#0F1F3D` mit weißer Schrift. | — |
| Titelschalter | **Eine** Bauart: `[Parameter] bool TitelAnzeigen` (Vorgabe `true`). Trägt ein Wirt (Überlagerung **oder** Plattformfenster) den Titel, steht hier `false`, und dann fällt auch das ✕. | keine; die Ausnahmeliste der Wache bleibt leer |
| Titeltext | Der Titel des Wirts ist **derselbe Ausdruck** wie der des Dialogs, nie ein Knopftext. | — |
| Kontextzeile | `{Projekt} · {Variante} · netto`, weggelassen statt leer gezeichnet. | Dialoge ohne Projektbezug (Katalogpflege im Administrationsmodus) zeigen stattdessen `Katalog` |
| Hilfe | `InfoKnopf` mit `Schluessel` **und** `Dialogname` in jedem Kopf; eingebettete Dialoge erben ihn vom Wirt. | keine |
| Schließen | ✕ = Esc = Abbrechen (Bauform E) bzw. = Schließen (Bauform K). Der Wirt schließt, nie die Komponente. | keine |
| Fußleiste | Immer `SpeichernLeiste`. Reihenfolge `Status ⟶ [Aktionen] · Speichern · Abbrechen · OK`; OK ist der einzige Primärknopf und steht ganz rechts. | Bauform K: nur `Schließen`, ebenfalls über `SpeichernLeiste` (`MitAbbrechen="false"`, `OkText` = „Schließen") |
| Knopftexte | `ALLG_BTN_OK`, `ALLG_BTN_ABBRECHEN`, `ADM_BTN_SPEICHERN` — drei Schlüssel im ganzen Haus, keine Emoji im Ressourcentext. | „Übernehmen" nur am **nicht schließenden** Knopf, der in den Arbeitsstand übernimmt |
| Schreibweg | Geschrieben wird im OK-Weg, je Schritt benannt, **nur bei Änderung**. Ein Knopf, der anders hinausführt, nimmt den OK-Weg und sagt es in einer Zeile darunter. | Bauform K schreibt je Aktion, dann fragt jede ändernde Aktion vorher |
| Rückfragen | `Rueckfrage` mit `VorgabeNein="true"` vor jedem Löschen **und** vor jedem Kontextwechsel, der ungespeicherte Eingaben verwirft. | Löschen im reinen Arbeitsstand, das Abbrechen zurücknimmt — dann sagt es der Werkzeugtipp |
| Meldungen | `Fehler` = gescheitert, `Warnung` = fragwürdig, `Hinweis` = Erklärung. Ein dauerhaftes Banner nur für einen Zustand, den der Anwender beheben muss. | — |
| Listen | eingebbar → `Zeilenraster` (mit Abschlusszeile „+ Neue …", grauer Gerechnet-Zelle); auswählend → `Raster<T>` mit `Virtualisiert`; handgeschriebene `<table>` nur bei Laufzeitspalten, mit Begründung. | `KostenSeite` und `WirtschaftlichkeitSeite` (Spalten je Variante) |
| Filter | Jede Liste mit mehr als 20 Zeilen trägt Suchfeld über alle Spalten, Trefferzahl und Spaltentrichter; Filterstand über `Katalogfilterregister`. | kurze feste Kataloge (Positionsarten, Einheiten) |
| Zahlen | Stellenzahl nach Größenart (Geld 2 · ct/kWh 2 · Faktor 4 · Prozent 2 · Jahr/Stück 0 · Leistung 1), keine Tausendertrennzeichen, komma- und punkttolerant. | — |
| Seiten | Eine Seite trägt **keine** Abschlussleiste, kein OK, kein Abbrechen — nur eine Aktionsleiste mit genau einem Primärknopf. | — |
| Datenseite | `Gaben()` liegt in `EPOS.UI.Daten`; die Plattformhülle trägt nur Fenster, Titel, Größe und den Ergebnisrückruf. | — |

---

## 7 Offene Fragen an den Anwender

| # | Frage | Empfehlung |
|---|---|---|
| **F1** | Soll der **Knopf, der schreibt und schließt, überall „OK" heißen** — auch dort, wo heute „Speichern" (Wirtschaftlichkeitsparameter, Tarifstruktur, BHKW, Einstellungen) oder „Übernehmen" (PV-Vergütung, Leistungspreisreihe, Spotpreisimport) steht? | **Ja.** Ein Wort für einen Weg. „Speichern" und „Übernehmen" bleiben den **nicht schließenden** Knöpfen vorbehalten — dann sagt der Text, was der Knopf tut, statt zu raten. Betroffen sind sieben Dialoge, je eine Zeile. |
| **F2** | Die vier **Katalogdialoge, die sofort schreiben** (Gesetzliche Parameter, Katalog-Dubletten, Kostenfaktoren, Emissionskatalog-Teile): sollen sie auf **Arbeitsstand + OK/Abbrechen** umgebaut werden (wie #282 beim Pufferspeicher), oder bleiben sie Sofortschreiber mit **einem** Knopf „Schließen" und Rückfrage vor jeder Änderung? | **Sofortschreiber mit „Schließen"** — sie sind Pflegewerkzeuge für Stammdaten, ein Arbeitsstand über hunderte Katalogzeilen wäre teuer und schwer zu erklären. Wichtig ist nur, dass sie **kein** „Abbrechen" anbieten (heute tun sie es auch nicht) und dass **jede** Änderung fragt. |
| **F3** | Bekommt der Dialogkopf das **dunkle Band `#0F1F3D`** aus § 2.7 und den Mockups, oder wird § 2.7 auf den heutigen hellen Kopf umgeschrieben? | **Band einführen.** Es trennt Kopf von Leib, trägt Kontextzeile und ⓘ sichtbar zusammen und ist das, was der Anwender in jedem Entwurf gesehen hat. Die Tokens stehen bereits (`--epos-karte-titel`, `--epos-gruppenkopf-text`); die Änderung läuft durch `StilblattTests` und braucht eine Browserprobe für den Kontrast. |
| **F4** | Der **Spaltenfilter** (Trichter im Spaltenkopf, eine Suchzeile darüber) ist für die Erzeugerkataloge entworfen, aber in **keinem** Kostendialog gebaut. Soll er für Gesetzliche Parameter, Emissionsarten, Kostenfaktoren und Nutzungsdauern nachgezogen werden? | **Ja, in dieser Reihenfolge**, beginnend mit den gesetzlichen Parametern — dort wächst die Liste je Jahr und Klasse am schnellsten, und dort sucht der Anwender am häufigsten einen einzelnen Satz. |
| **F5** | Der **Tarifstrukturdialog** ist in einem Wärmepumpenprojekt ohne BHKW und ohne PV nicht erreichbar, sobald der Tarifsatz inaktiv ist (Status „Nach #291"/„Nach #292"). Menüpunkt, Schalter in der Kostenverwaltung, oder Zonenmodell abkündigen? | **Menüpunkt „Administration → Kostenverwaltung → Tarifstruktur"** als kleinster Schritt — die Überladung `TarifstrukturHuelle.Oeffnen` steht und wartet auf einen Aufrufer. Ob HT/NT und Leistungspreis fachlich gebraucht werden, bleibt davon unberührt und kann später entschieden werden. |
| **F6** | Der Sprungknopf **„Tarif…" im PV-Vergütungsdialog** verwirft heute still, und sein Hinweistext rät zu etwas Unmöglichem. Soll er wie im BHKW-Dialog **schreiben und springen**, oder wie Abbrechen **verwerfen und es sagen**? | **Schreiben und springen**, genau wie `BhkwWirtschaftlichkeitDialog` es seit #286 tut — sonst gibt es im selben Fachbereich zwei Sprungarten. Der Hinweistext `PVV_SPRUNG_HINWEIS` wird in beiden Sprachen neu gefasst. |
| **F7** | Beim Öffnen aus dem **Menü** steht der Titel heute im Fenstertitel **und** als `h1` (Energieträger, Nutzungsdauern). Soll das Fenster den Titel tragen und der Dialog keinen — wie bei der `Ueberlagerung`? | **Ja**, dieselbe Regel eine Ebene höher. Achtung auf die Folge aus #295: Der **Fenstertitel muss dann der lange sein** („Energieträger — Erdgas"), sonst verliert der Kopf wieder Information. Der Dialog meldet ihn der Hülle über einen Rückruf. |
| **F8** | Sollen die **15 windowsgebundenen Hüllen** (alle Wirtschaftlichkeits- und Admin-Dialoge, dazu die Kostenverwaltung) nach `EPOS.UI.Daten` umziehen? | **Ja, aber nach Nutzen gestaffelt.** Zuerst `KostenKomponenteHuelle` — sie ist die größte und der Einstieg, den iOS zuerst braucht. `EnergietraegerFenster.cs` ist die Vorlage für den dünnen Adapter, der danach bleibt. |

---

### Anhang: was aus der Statusliste inzwischen erledigt ist

- **„Nach #286"** — die vier Geschwisterdialoge der Wirtschaftlichkeitsseite zeichnen ihren Titel
  nicht mehr unbedingt: `WirtschaftlichkeitSeite.razor:404, 418, 432, 446, 460` setzen
  `TitelAnzeigen="false"`. `WPAR_SPRUNG_HINWEIS` steht nicht mehr in `Resource.resx`.
- **„Nach #289" (1)** — der Sprungknopf „BHKW-Wirtschaftlichkeit…" im Parameterdialog ist gefallen;
  an seiner Stelle steht eine Erklärzeile (`WirtschaftlichkeitParameterDialog.razor:324`).
- **„Nach #289" (2)** — `RestbefundC` in `EPOS.UI.Tests/UeberlagerungstitelTests.cs:582` ist
  **leer**; die 37 doppelten Titel sind nachgezogen.
- **„Nach #282"** — die Ausnahme „Speichern/Schließen" gibt es nicht mehr (siehe B31).

**Noch offen und hier bestätigt:** „Nach #263" (stiller Kontextwechsel, B18), „Nach #289" (4)
(unbedingtes Schreiben im OK-Weg, B30), „Nach #291"/„Nach #292" (Tarifdialog, B29),
„Nach #295" (verkürzte Überlagerungstitel, B12).

# Protokoll Etappe KD2 — Komponenten-Kostendialog, Admin-Kontext (Designer-fähig)

Stand: 25.08.2026 · Branch `kostenformulare` · Konzept: `Konzept_Kostendialoge_EPOS-Plan.md` Rev. 1.2, § 5/§ 14 · setzt auf KD1 (Schemastand 39) auf

## 1. Umfang

| Baustein | Inhalt |
|---|---|
| `Controller\KostenVorlagenCtrl.cs` | UI-freie Datenschicht (Hausmuster „der Dialog rechnet nicht"): DTOs `KostenVorlageKopf`/`KostenVorlagenPosition`, Lesen (Komponenten, Varianten, Positionen), Pflege (VorlageNeu, SpeichernUnter mit Positionskopie und Rücknahme bei Teilfehler, VorlageLoeschen, PositionNeu/-Speichern/-Loeschen), durchgängiger **ReadOnly-Schutz** der Auslieferungsvorlagen, `GeaendertAm`-Fortschreibung, USt-Satz aus `GesetzKatalog` (`UMSATZSTEUER_REGELSATZ`). Dazu `BemessungKatalog`: die 14 Bemessungen des § 5.3 + 2 Altwerte (nur Anzeige) mit Einheit, Invest/Betrieb-Zuordnung, Absolut-Kennzeichen und MyResource-Anzeigetext — EINE Wahrheit, auch für KD3 |
| `Views\Kosten\ucVorlagenZeile` (.cs/.Designer.cs) | Designer-fähige Rasterzeile (Ä6-Regel 1): Stift/Papierkorb, Bezeichnung, Bemessungs-Klappliste (gefiltert je Kategorie), Satz + Einheit, 🔗-Kopplung mit Tooltip (KL4: absolut ⇒ Satz=Betrag gespiegelt; bezugsabhängig ⇒ „—" mit Hinweis „Bezugsgröße erst im Projekt"), Betrag (im Stammkontext nie direkt editierbar), **Nutzungsdauer-Spalte** (FK4, nur Invest), Worst/Best-Knopf deaktiviert mit Tooltip (Szenarien sind Projektsache), Empfehlungsbereich als Satz-Tooltip; **Neu-Modus** = gestrichelte Abschlusszeile der Mockups (FK2) |
| `Views\Kosten\Form_KostenKomponente` (.cs/.Designer.cs) | Hauptdialog: Kopf (Titel „Kostenverwaltung ‹Komponente›" + Untertitel je Kategorie), Reiter „Kosten Invest/Betrieb" / „Ertrag/Bonus" (Platzhalter bis KD5), Kontextzeile (Komponenten-Klappliste, Invest/Betrieb, Variantenwahl, Neu…/Speichern unter…/Löschen), gelbes Netto-Banner (schließbar), ReadOnly-Hinweiszeile, Spaltenkopf, Laufzeit-Raster in `pnlZeilen`, Fuß mit „+ Position hinzufügen" (FK2) und Netto-/Bruttosumme (KL5) |
| `Views\Kosten\Form_VorlagenPosition` (.cs/.Designer.cs) | Zeileneditor (Stift, § 5.2): Bezeichnung, Kostenart (VDI 2067), Erlös-Kennzeichen, Empfehlungsbereich |
| `Views\Kosten\Form_VariantenName` (.cs/.Designer.cs) | Namensabfrage Neu/Speichern unter, Vorbelegung nach FK9-Schema „‹Name› — Variante ‹n›" |
| `MDIMainForm.cs` | `InitKostenvorlagenMenue()` — programmatischer Eintrag Administration → Kosten → „Kostenvorlagen (Komponenten)…" (Muster `InitGesetzeMenue`); der vollständige Menü-Umbau (Ä5) folgt mit KD4/KD6 |
| `Allgemein\KI\HilfeKontext.cs` | drei neue Formulare dem Bereich „Kosten und Preise" zugeordnet |
| `MyResource\Resource.resx` + `.en-US.resx` | 55 neue Schlüssel (`KDLG_*`, `BM_*`), de + en |

**Ä6 umgesetzt:** Alle vier neuen UI-Bausteine haben `.Designer.cs`-Dateien im
Standard-InitializeComponent-Muster (UTF-8 mit BOM, deutsche Vorgabetexte); der Konstruktor
überschreibt die Texte aus MyResource. Nur die Zeilenliste wird zur Laufzeit in das im Designer
platzierte Panel gefüllt.

## 2. Prüfungen (Testkopie mit KD1-Stand 39)

Runner-Smoke `kd2` — **23/23 PASS**: 10 Komponenten · BHKW/Betrieb Standard (ReadOnly, 11
Positionen, Vollwartung = `EUR_PRO_KWH_ELEKTRISCH`, Empfehlung 3–9) · ReadOnly-Schutz
(Speichern/Löschen/Neu auf Standard abgelehnt) · „Speichern unter" kopiert 11 Positionen
editierbar · Satz 0,05 gespeichert · Position anlegen/löschen · Namensdublette abgelehnt ·
USt 19,0 aus dem Katalog · Formular baut headless (Konstruktor + `SetControls("BHKW")`) ·
Kaskadenlöschen der Kopie · Standard danach unverändert (Satz weiterhin NULL).

Offscreen-Renderings (`DrawToBitmap`, BHKW Invest + Betrieb) belegen Aufbau und
Kopplungslogik (Einheiten je Zeile, 🔗 nur bei absoluten Bemessungen, Summenfuß mit
Katalog-USt, gesperrte Pflege bei ReadOnly). Bekannte Artefakte des Offscreen-Modus:
TextBox-Inhalte und Emojis erscheinen erst im echten Fenster.

## 2b. Design-Nachtrag (Entscheidung Philipp, 25.08.2026)

Alle Dialoge in **einheitlichem App-Design** statt Web-Mockup-Optik: Navy-Kopfleiste
RGB (15, 31, 61) mit weißem Titel + hellblauem Untertitel (Muster `Form_Kosten.pnlHeader`),
Spaltenkopf RGB (26, 50, 97) weiß, Navy-Summenfuß — umgesetzt in `Form_KostenKomponente`,
`Form_VorlagenPosition` und `Form_VariantenName`; als Designregel für KD3–KD6 im Konzept
§ 12 festgehalten. Spaltentitel „Nutzungsdauer" auf „Nutzung [a]" gekürzt.

## 3. Umsetzungsentscheidungen (für die Sichtabnahme)

1. **Komponentenwahl als Klappliste** im Kopf statt der Kartenübersicht aus § 5.1 — für die
   Admin-Pflege der schnellere Weg; die Kartenübersicht (Folie 1) bleibt für den
   Projekteinstieg (KD3/KD6) vorgesehen.
2. Kategorie-Umschaltung Invest/Betrieb als Radiogruppe in der Kontextzeile (die Mockups
   zeigen je Kategorie ein eigenes Blatt; beide Raster sind baugleich).
3. Worst/Best-Knopf im Stammkontext sichtbar, aber deaktiviert mit Tooltip — mockup-treu und
   ehrlich (Szenariospalten liegen an der Projektposition, `Form_CaseEingabe`).
4. Zeilenspeicherung beim Verlassen des Felds (kein separater Speichern-Knopf), Muster der
   bestehenden Kostenmasken.

## 4. Offen / Übergabe

- **UI-Sichtabnahme durch Philipp** (Abnahmekriterium KD2): Mockup-Abgleich Folien 8/19 im
  echten Fenster; Formulare in VS im Designer öffnen (Ä6-Nachweis).
- Menüpunkt ist bewusst ZUSÄTZLICH (Bestandseinträge „Kosten"/„Kosten Admin" unangetastet);
  Umbau nach Ä5 mit KD4/KD6.
- Weiter mit **KD3** (Projektkontext + Übernahme-Mechanik, § 8).

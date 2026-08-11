# Berichtsmodul — Stand Phase 1 – 6 (11.08.2026)

Umsetzung nach `Allgemein\Reporting\Konzept_Berichtserstellung_EPOS-Plan.md`, Kap. 12.

## Phase 6 (neu): Wirtschaftlichkeit — Kapitalwertmethode (DIN EN 17463, Stufe W1)

Fachliche Entscheidungen (11.08.2026, Konzept_Wirtschaftlichkeit.md Kap. 7):
**Referenz = Stammprojekt** (KW der Variante = Barwert der Differenz-Zahlungsströme
Variante − Stamm) · Vorgabe **i = 3,0 % / T = 20 a** (je Stamm editierbar) ·
**Restwert linear** · **Strompreise aus der Kostenmaske** (keine Doppelpflege).

| Datei | Inhalt |
|---|---|
| `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitDaten.cs` | DTOs (`WirtschaftlichkeitParameter`, `WirtschaftlichkeitErgebnis`, Szenarien Worst/Erwartet/Best) und Schnittstelle `IWirtschaftlichkeitProvider` (Berichtskonzept Kap. 6) |
| `Allgemein/Wirtschaftlichkeit/KapitalwertRechner.cs` | reiner Rechenkern: KW = −I₀ + Σ(E−A)/(1+i)^t + RW/(1+i)^T; Annuität a(i,n); Ersatzbeschaffungen bei Nutzungsdauer < T (nominal konstant, W1); Restwert linear; dynamische Amortisation der Differenzreihe (ohne Restwert, mit Interpolation). Ohne DB/UI — testbar gegen goetz_test.XLS/VALERI |
| `Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs` | legt **`Tab_ProjektWirtschaftlichkeit`** (Parameter je Stamm) und **`Tab_ErgebnisWirtschaftlichkeit`** (Ergebnis je Projekt × Szenario, **FK ID_Ergebnis auf den Simulationslauf**) bei Bedarf an; liest Tab_ProjektWerte (Kat. 1 = Investitionen mit Best/Worstcase + Nutzungsdauer, Kat. 2 = Betriebskosten), Energiekosten aus dem KostenEmissionRechner, Einspeiseerlös = PV-Überschuss × Vergütung; rechnet alle 3 Szenarien, persistiert, `LadeErgebnisse()`/`ErgebnisAktuell()` für Reiter + Bericht |
| `Views/Wirtschaftlichkeit/Form_Wirtschaftlichkeit.cs` | Reiter/Dialog „Wirtschaftlichkeit" (Berichte & Kosten): Vergleichsgruppe mit Simulationsständen (Stamm fixiert), Szenario-Umschalter, Kennzahlen-Vergleichstabelle, Parameter-Nachweiszeile, [Parameter…]/[Berechnen]; **Prüfkette laut Vorgabe**: fehlende/veraltete Simulationen werden beim Berechnen automatisch headless nachgerechnet (BerichtsDatenSammler); nicht berechenbare Projekte erscheinen mit Begründung |
| `Views/Wirtschaftlichkeit/Form_WirtschaftlichkeitParameter.cs` | Parameterdialog (i, T, Preissteigerung Energie/Betrieb, Einspeisevergütung) — eine Zeile je Stamm, gilt für die ganze Gruppe |
| `Allgemein/Bericht/Bausteine/BausteineWirtschaftlichkeit.cs` | Berichts-Baustein: Methodik + Parameternachweis, Kennzahlentabelle „Erwartet" (Blockaufteilung wie Kap. 5.1), Szenarienübersicht W/E/B, Aktualitäts-/Fehlgrund-Hinweise. **Rechnet nicht selbst** — liest die persistierten Ergebnisse (Reiter, Word, Excel = identische Zahlen) |

Geändert: `WordBerichtGenerator.cs` (Baustein registriert), `Form_Bericht.cs`
(Wirtschaftlichkeit wählbar, Hinweis auf Datenquelle Reiter),
`ExcelBerichtGenerator.cs` (+Blatt „Wirtschaftlichkeit", 3 Szenarioblöcke, echte
Zahlen), `BerichtTexte.cs` (+Übersetzungen), `Form_Variantentest(.Designer).cs`
(+Schaltfläche „Wirtschaftlichkeit (Kapitalwertmethode)…"),
`KostenEmissionRechner.cs` (Netzbezug jetzt über den Emissionsfaktor des
Strom-Trägers — Kette Projekt → Tab_Brennstoff_Stamm → energy_carrier; erst ohne
gepflegten Wert greift die Konstante 380 g/kWh).

**Einstieg für den [Wirtschaftlichkeit]-Button in Form_Start (ein Handgriff im
Designer):** `new Form_Wirtschaftlichkeit(m_ID_Projekt).ShowDialog();`
(Muster btn_Varianten_Click; Varianten werden automatisch auf ihren Stamm aufgelöst).

**Testschritte Phase 6:** App starten (legt beide Tabellen an) → Varianten-Dialog →
„Wirtschaftlichkeit…" → Parameter prüfen/speichern → Berechnen (simuliert fehlende
Ergebnisse automatisch) → Werte je Szenario prüfen. Danach Bericht mit aktivem
Baustein „Wirtschaftlichkeit" erzeugen (Word + Excel-Blatt). Validierung des
Rechenkerns: Kapitalkosten-Annuität gegen goetz_test.XLS (BHKW 17 150 € ·
a(0,35 %; 13,33 a) = 1 319 €/a).

**Offen (W2/W3, Konzept Kap. 5.4):** Preisszenarien Best/Worst für Energie,
KWKG-/EEG-Boni, Steuern, BEHG-CO₂-Preis jahresscharf, Sensitivitätstabelle, IRR,
HT/NT-Tarifstruktur, Emissionsbilanz mit Kraftwerkspark.

## Phase 5 (neu): Kosten/Emissionen, Berichtssprache, Einstiegspunkte

| Datei | Inhalt |
|---|---|
| `Allgemein/Bericht/KostenEmissionRechner.cs` | aktiviert die Kennzahlgruppen **Emissionen** (CO₂ gesamt t/a, spezifisch g/kWh Wärme) und **Kosten einfach** (Energiekosten p. a., Stromkosten Netzbezug): Verbrauch je Erzeuger-Modul (carrier_id aus Befund B1) × Preise aus `energy_project_settings`/`energy_carrier`; **CO₂-Faktor-Quelle (Vorgabe 11.08.2026): Projektwert `energy_project_settings.co2` → Katalog `Tab_Brennstoff_Stamm.CO2` (über `energy_carrier.id_brennstoff`) → `energy_carrier.co2`**; Mengen über `Abfrage_Energietraeger_Effektiv`; Netzbezug × Strommix-Konstante (380 g/kWh, Parameter). Fehlt für einen Träger mit Verbrauch Preis/Faktor → Kennzahl bleibt „—" (keine stillen Teilsummen) |
| `Allgemein/Bericht/BerichtTexte.cs` | Berichtssprache = **UI-Sprache** (`Program.nLanguage`): Kultur für Zahlen/Datum, Wörterbuch-Übersetzung der Kapitel-/Kopftexte; Kennzahl-Labels über den zweisprachigen Katalog |
| `Views/Varianten/Form_AlsVariante.cs` | Dialog **„Als Variante speichern…"** (Konzept Kap. 3.3): Bezeichner eingeben → aktueller Stand wird Variante (`VariantenCtrl.AnlegenAusStamm`); ist das offene Projekt selbst Variante, wird ihr Stamm verwendet |

Geändert: `BerichtsDaten.cs` (Rechnerfelder), `BerichtsDatenSammler.cs` (Rechner-Aufruf),
`KennzahlenKatalog.cs` (Emissions-/Kostenzeilen liefern jetzt Werte; Label
„Stromkosten Netzbezug"), `WordBerichtGenerator.cs` (Kultur = UI-Sprache; Überschriften
und fette Kopfzellen laufen durch die Übersetzung), `BausteineVergleich.cs` /
`ExcelBerichtGenerator.cs` (sprachabhängige Kennzahl-Labels).

**Menü-Einbindung (ein Handgriff in Visual Studio):** im MDI-Menü „Projekt" zwei
Einträge anlegen und verdrahten —
`Form_AlsVariante.Zeige(this, <aktuelleProjektId>, <projektname>);` bzw.
`new Form_Variantentest(<aktuelleProjektId>).ShowDialog(this);` (Varianten/Bericht).
Bewusst nicht automatisch editiert: `MDIMainForm.Designer.cs`/`.resx` werden laut
Projektkonvention nur über den WinForms-Designer gepflegt.

**Zu verifizieren (Phase 5):**
1. ~~CO₂-Faktor-Einheit~~ **verifiziert (Kenndaten.accdb, 11.08.2026)**: die Faktoren
   stehen in g/kWh (Tab_Brennstoff_Stamm: Erdgas 240, Heizöl 310, Strom 560).
   Befund dabei: die `energy_carrier`-Kopien tragen fast durchweg co2 = 0 — die
   Fallback-Kette über `Tab_Brennstoff_Stamm` ist damit zwingend.
2. Strommix-Konstante 380 g/kWh ist seit Phase 6 nur noch **Fallback**: der
   Netzbezug wird vorrangig mit dem Faktor des projektzugeordneten Strom-Trägers
   bewertet (gleiche Kette Projekt → Katalog). Hinweis: der Katalogwert „Elektrische
   Energie" (560 g/kWh) ist ein älterer Strommix — bei Bedarf im Katalog bzw. je
   Projekt in der Kostenmaske aktualisieren.
3. Vollständige Übersetzung der Fließtexte (Hinweis-/Methodiktexte) ist
   Übersetzungsarbeit und bewusst offen; Kapitel, Tabellenköpfe und Kennzahlen
   sind zweisprachig.

## Phase 4: Excel-Ausgabe (ClosedXML)

| Datei | Inhalt |
|---|---|
| `Allgemein/Bericht/ExcelBerichtGenerator.cs` | Excel-Mappe ohne Office: Blatt **Übersicht** (Projektdaten, Variantenliste mit Sim-Zeitstempel, Komponenten-Matrix), Blatt **Vergleich** (Kennzahlen als Zeilen nach 4 Gruppen, Varianten als Spalten, **echte Zahlenwerte** mit Zellformat, fixierte Köpfe, Autofilter, Δ%-Block rechts), **Detailblatt je Variante** (Kennzahlen, Erzeuger-Module, Brennstoffmengen, Monatswerte aus dem frischen Simulationslauf). Fehlende Werte bleiben leer, nie 0 |

Geändert: `Controller/BerichtCtrl.cs` (+`ErzeugeExcel`, gleiche Namens-/Kollisionslogik
wie Word), `Views/Bericht/Form_Bericht.cs` (Excel/Beide erzeugt jetzt wirklich;
Zeitreihen auch für Excel-Monatswerte), `WindowsFormsApplication1.csproj`
(+`ClosedXML 0.105.1`, `SixLabors.Fonts` auf **1.0.1 gepinnt** — Lizenzfalle ab 2.x;
`dotnet list package --include-transitive` in die Release-Checkliste aufnehmen).

**Testschritte Phase 4:** einmal `dotnet restore` (neue Pakete), dann Bericht mit
Ausgabe „Excel" oder „Beide" erzeugen. Prüfen: drei Blatt-Typen, Zahlen sind echte
Werte (weiterrechenbar), Autofilter/Fixierung im Vergleichsblatt, Δ%-Spalten,
Detailblätter nur bei aktivem Baustein „Ergebnisse je Variante" (dann inkl.
Monatswerte-Tabelle).

## Phase 3: Diagramme — Vollbericht Word

| Datei | Inhalt |
|---|---|
| `Allgemein/Bericht/ChartRenderer.cs` | Off-Screen-Diagramme (System.Drawing, PNG in doppelter Auflösung, feste Erzeuger-Palette): Kuchen (Deckung), horizontale Balken je Variante, **4 Ganglinientypen** — Wärme-Jahresverlauf (gestapelte Tagesmittel + Bedarfslinie), Jahresdauerlinie, Strombilanz je Monat (Stapel + Einspeisung + Bedarfslinie), Speicherverlauf (3 charakteristische Wochen). Abweichung vom Konzept: bewusst GDI+ statt ScottPlot (kein API-Risiko, gleiches Muster wie Bestandsbericht; ScottPlot-Umstieg bleibt möglich) |
| `Allgemein/Bericht/ZeitreihenExtraktor.cs` | sammelt nach `SimulationRunner.Simuliere()` die Stundenreihen ein (Membernamen gegen die Engine verifiziert; Aliasing-sicher durch Kopien; ¼h→h per Stundenmittel über `sim.Viertelstunden_zu_Stundenwerte_Mittelwert`) |

Geändert: `BerichtsDaten.cs` (ZeitreihenSatz mit Standard-Schlüsseln), `BerichtsDatenSammler.cs` (bei aktivem Baustein „Ergebnisse je Variante" wird je Projekt **immer frisch in-memory simuliert** — Kennzahlen und Ganglinien stammen aus demselben Lauf; Ergebnis wird dabei wie gehabt persistiert), `WordBerichtGenerator.cs` (`WordKontext.Bild` — Inline-PNG-Einbettung), `BausteineVergleich.cs` (Ganglinien in Baustein 5; Balkendiagramme je Schlüsselkennzahl + Deckungs-Kuchen in Baustein 6), `Form_Bericht.cs` (Zeitreihen-Flag).

**Testschritte Phase 3:** Bericht mit aktiven Bausteinen „Ergebnisse je Variante" + „Variantenvergleich" erzeugen. Erwartung: je Variante die vier Ganglinien mit Beschriftung (fehlt ein Gewerk, entfällt die betreffende Reihe; ohne Puffer-/PV-Speicher entfällt der Speicherverlauf); im Vergleichskapitel horizontale Balken (Stamm dunkel hervorgehoben) und die Deckungs-Kuchen je Projekt. Laufzeit steigt durch die In-Memory-Simulation je Variante (Fortschritt „Simuliere: …", Abbruch möglich).

## Phase 2 (neu): erster vollwertiger Word-Vergleichsbericht

| Datei | Inhalt |
|---|---|
| `Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx` | **Rahmen-/Stylevorlage**: Styles Title, Subtitle, Heading1–3, Normal, Hinweis, Beschriftung; Kopfzeile mit INEKON-Logo, Fußzeile mit Datum- und Seitenfeldern. In Word frei anpassbar (CI ohne Codeänderung) |
| `Allgemein/Bericht/IBerichtsBaustein.cs` | Baustein-Schnittstelle (Konzept Kap. 8.3) |
| `Allgemein/Bericht/WordBerichtGenerator.cs` | OpenXML-Generator + `WordKontext` (Style-Absätze, Tabellenbau, Blocksplitting max. 3 Varianten je Block mit wiederholter Stamm-Spalte, Δ aus Rohwerten, TOC-Feld, UpdateFieldsOnOpen). Fällt die Vorlage aus, werden Ersatz-Styles programmatisch erzeugt |
| `Allgemein/Bericht/ProjektDetails.cs` | lesende Detail-Daten je Projekt (Klimaregion, Gebäude, Tab_Energieanlagen, Komponenten je Gewerk) — Spalten gegen Kenndaten.accdb verifiziert |
| `Allgemein/Bericht/AbweichungsErmittler.cs` | deklarative Feldliste (~50 Merkmale) für Kenndaten-Tabellen **und** Abweichungserkennung Variante↔Stamm (3 Stufen: Bestand, Komponente, Auslegung) |
| `Allgemein/Bericht/Bausteine/BausteineStandard.cs` | Deckblatt, Inhaltsverzeichnis (TOC), Anhang (Simulationsstände, Methodik, Hinweise) |
| `Allgemein/Bericht/Bausteine/BausteineProjekt.cs` | Projektbeschreibung (Stamm inkl. Gebäude), Komponenten & Varianten (Matrix, Kenndaten je Gewerk, Abweichungstabellen) |
| `Allgemein/Bericht/Bausteine/BausteineVergleich.cs` | Ergebnisse je Variante (Kern-Kennzahlen), Variantenvergleich (Gruppen-Tabellen, kompakte Δ%-Tabelle ab 2 Varianten, Erzeuger-Einzellisten, Brennstoffmengen) |

Geändert: `BerichtsDaten.cs` (+`Details`), `BerichtsDatenSammler.cs` (+Details,
+Abweichungen), `Controller/BerichtCtrl.cs` (+`ErzeugeWord` mit Dateinamen
`<Projekt>_Bericht_<JJJJ-MM-TT>.docx`, ohne stilles Überschreiben),
`Views/Bericht/Form_Bericht.cs` (Word-Erzeugung + „Bericht öffnen?"),
`WindowsFormsApplication1.csproj` (Vorlage wird nach `bin\…\Vorlagen\` kopiert).

**Testschritte Phase 2:** bauen (x86) → Varianten-Dialog → „Bericht erstellen…" →
Varianten anhaken → Erstellen. Ergebnis: .docx im Zielordner; beim Öffnen fragt
Word einmal nach der Feldaktualisierung (Inhaltsverzeichnis) — mit Ja bestätigen.
Prüfen: Deckblatt/Logo, Inhaltsverzeichnis, Projektbeschreibung mit Gebäuden,
Komponenten-Matrix + Kenndaten + Abweichungen je Variante, Vergleichstabellen
(bei > 3 Varianten Blocksplitting, bei genau 1 Variante Δ-Spalte), Anhang.

**Bewusst offen:** Wirtschaftlichkeit (Reiter „Wirtschaftlichkeit" +
Kapitalwert-Rechenmodul nach DIN EN 17463) → Phase 6 gemäß
`Konzept_Wirtschaftlichkeit.md`. Der alte Direktbericht
(`ProjektvergleichBericht`, Button „…(alt)") ist mit Phase 3 fachlich abgelöst
und kann nach erfolgreichem Praxistest entfernt werden.

---

## Phase 1 (Fundament, unverändert gültig)

Neue Dateien: `Controller/VariantenCtrl.cs` (Variantenlogik inkl. `AnlegenAusStamm`
für den Menüweg, Waisen-Prüfung), `Controller/BerichtCtrl.cs` (Konfig-Persistenz
in DB-Tabelle `Berichtskonfiguration`, JSON), `Allgemein/Bericht/BerichtsDaten.cs`
(DTOs), `BerichtsKonfiguration.cs` (Baustein-Katalog), `KennzahlenKatalog.cs`
(4 Gruppen), `BerichtsDatenSammler.cs` (lesender Sammler + Statusprüfung +
headless Simulation), `Views/Bericht/Form_Bericht.cs` (Dialog).

Geändert: `Form_Variantentest` (+`.Designer`) — Delegation an `VariantenCtrl`,
Button „Bericht erstellen…"; `ErgebnisCtrl` — Befunde B1 (`carrier_id` in beiden
Modultabellen inkl. Befüllung), B2 (`Delete(int)` funktionsfähig), B3
(Kesselmodul-`Waermeproduktion` persistiert); `ErgebnisModel` (+`CarrierId`).

Nach einer Simulation prüfen: `Tab_ErgebnisBHKWModul`/`Tab_ErgebnisHeizkesselModul`
haben befüllte `carrier_id`; Tabelle `Berichtskonfiguration` entsteht beim ersten
Öffnen des Berichtsdialogs.

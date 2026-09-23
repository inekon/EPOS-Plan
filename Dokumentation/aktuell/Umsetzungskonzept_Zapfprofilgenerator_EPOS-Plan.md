# Umsetzungskonzept: Zapfprofilgenerator und Brauchwasserauslegung in EPOS-Plan

**Stand 2026-09-23 — Fassung 2 — Umsetzungsentwurf, zur Abnahme durch den Anwender — Nachträge N1–N3 (Kapitel 11)**

Auftrag (Anwender, im Wortlaut): „starte das Umsetzungskonzept".

**Quellen.**

| Quelle | Gegenstand |
|---|---|
| [`Konzept_TWW-Zapfprofile_WP-Plan_1.md`](Konzept_TWW-Zapfprofile_WP-Plan_1.md) (Fassung V1.2) | Methodik: drei Lehren, Schichten S0–S6, Entscheidung D (Hybrid), Eingabe- und Vorgabekonzept, Risiken |
| [`Mockups/Zapfprofilgenerator_Mockup.html`](Mockups/Zapfprofilgenerator_Mockup.html) | Zielbild von Dialog, Überlagerungen, Katalogdialog, Einbindung, Stufen Z0–Z5 und offene Entscheide K1–K8, A1–A12 |
| [`Zapfprofilgenerator/2026-09-22_Vorlagenanalyse_TWW-Auslegung_V4.md`](Zapfprofilgenerator/2026-09-22_Vorlagenanalyse_TWW-Auslegung_V4.md) (Vorlagenanalyse der Excel-Vorlage „TWW-Auslegung V4") | Speicherauslegung nach der INEKON-Vorlage: Blattfolge, Formelsammlung, Bedienmuster, vier Schwächen |
| [`Grundlagen_1_Normen_Regelwerke_TWW-Zapfprofile.md`](Grundlagen_1_Normen_Regelwerke_TWW-Zapfprofile.md), [`Grundlagen_2_Modelle_Generatoren_Daten_TWW.md`](Grundlagen_2_Modelle_Generatoren_Daten_TWW.md), [`Grundlagen_3_DIN-EN-12831-3_A1_A100_Auswertung.md`](Grundlagen_3_DIN-EN-12831-3_A1_A100_Auswertung.md), [`Grundlagen_4_WP-Plan_Repo-Analyse.md`](../ueberholt/Grundlagen_4_WP-Plan_Repo-Analyse.md) (überholt), [`Grundlagen_5_VDI-4655_Auswertung.md`](Grundlagen_5_VDI-4655_Auswertung.md) | Normen, Generatoren, Summenlinienverfahren, frühere Repo-Analyse, VDI 4655 |
| [`KONTEXT_Brauchwassertypen_VDI6002.md`](KONTEXT_Brauchwassertypen_VDI6002.md) | Herkunft des heutigen Brauchwasserkatalogs |

**Geltung.** Dieses Papier plant die **Umsetzung**: Klassen, Tabellen, Dialoge, Tests, Reihenfolge
und Abnahme. Die **Methodik** — warum Bilanz und Auslegung getrennt sind, woher die Kennwerte
kommen, wie der Generator parametriert ist — bleibt im Konzept; wo dieses Papier dem Konzept
widerspricht, steht die Stelle in Kapitel 1.6. Jede Aussage über den Bestand trägt Datei und Zeile
des Arbeitsbaums; was nicht belegt ist, steht als offen. Das Papier enthält keine Normzahl: Formeln
und Verfahren stehen, Kennwerte, Formvektoren, Nachschlagewerte und Regelwerksgrenzen bleiben
gekapselte Katalogparameter (Kapitel 6). Alle Beispielzahlen sind fiktiv und rund; Zahlen, die eine
INEKON-Setzung sind, tragen den Vermerk „INEKON-Setzung" mit ihrer Herkunft.

**Leseweg.** Wer entscheiden will: Kapitel 0 und 9. Wer eine Stufe beauftragt: Kapitel 7, dann die
Abschnitte, auf die die Stufe verweist. Wer baut: Kapitel 2 bis 5 in dieser Reihenfolge, dazu die
Regeln in Kapitel 6.

---

## 0. Das Ergebnis in sechs Punkten

1. **Der Generator hat genau eine Weiche, und sie sitzt im Brauchwasserkanal.**
   `SimulationWaermebedarf.Brauchwasserwaerme_berechnen`
   (`EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs:994`) ist der einzige Ort, an dem die
   Brauchwasserreihe entsteht; der Lauf ruft sie in `:334`, die Projektvorschau des
   Bedarfsprofil-Dialogs in `EPOS.Kern/Controller/BedarfsVorschauCtrl.cs:130` und `:165`. Die Weiche
   wählt im Modus `Projektrechnung` je Projekt **exklusiv** zwischen Bestandsweg
   (`ProfilBedarf.Rechnen`, Vorgabe) und Generator (`ZapfprofilRechner.Rechnen`); was die Vorschau
   bei gewähltem Generator zeigt, regeln 2.2 und 5.2. Alles dahinter — Kanalbuchung (`:341`),
   Energieprobe (`:342`, `:390`), Netzverlustverteilung (`:374`), Wärmepumpe, Kaskade,
   Pufferspeicher — bleibt unberührt. Ohne Eintrag in der neuen Projekttabelle rechnet ein Projekt
   wie heute; **kein Referenzprojekt setzt die Weiche**, der Referenzlauf bleibt in jeder Stufe gegen
   die aktuelle Basis unter `Referenzlaeufe/` grün innerhalb der Toleranz.

2. **Der Rechenweg ist ein Ordner im Kern, kein eigenes Projekt.**
   `EPOS.Kern/Allgemein/Zapfprofil/` trägt die Schichten S0–S6 als Klassen ohne Datenbank und ohne
   Oberfläche, durchgehend `double`, Stundenreihen in kWh je Stunde, Jahreswerte mit Einheit im Namen.
   Die Datenseite ist ein Controller (`EPOS.Kern/Controller/ZapfprofilCtrl.cs`), die Katalogpflege ein
   zweiter (`TwwNutzungsartCtrl.cs`). Das Klassenbibliotheksprojekt des Konzepts (Teil 3.1) entfällt.
   Normkonstanten stehen nie im Quelltext; die Klassen bekommen sie als Parametersatz aus dem Katalog.

3. **Bilanz und Auslegung sind zwei Produkte — auch im Quelltext.** Die Bilanz liefert eine
   8760-Reihe der Zapfenergie und getrennt eine Zirkulationsreihe, beide als eigener Typ
   `Bilanzreihe`. Die Auslegung liest Mengengerüst, Tagesgang, Wochenfaktoren, einen Bedarfstag und
   eine eigens gebildete Wochenreihe, **nie die Jahresreihe**. Sie rechnet je **Anlagentopologie**:
   bei Speicher die Summenlinie (V, Φ_N), bei Durchfluss, Frischwasser- und Wohnungsstation die
   Minutenspitze der superponierten Last. Sie liefert immer die Dreiergruppe Summenlinie · Perzentil
   · Normvergleich und **genau eine Empfehlung: den Punkt der Summenlinie**. Der Verfahrensvergleich
   der Speicherauslegung nach der Vorlage V4 (Lindley-Bilanz, DIN 4708, Gleichzeitigkeit, klassischer
   Faustwert) ist nachrichtlich und erscheint als Plausibilitätsband. Eine Wache verbietet, dass eine
   Auslegungsklasse die Bilanzreihe annimmt.

4. **Das Datenmodell ist neu und hängt über IDs.** Zehn STRICT-Tabellen im Schemaschritt T1: der
   Katalog mit Provenienz je Wertgruppe (`Tab_TwwNutzungsart_STAMM`, `Tab_TwwTagesgangsatz_STAMM`,
   `Tab_TwwTagesgang_STAMM`, `Tab_TwwBedarfstag_STAMM`, `Tab_TwwBedarfstagEreignis_STAMM`,
   `Tab_TwwParameter_STAMM`, `Tab_TwwDin4708Wert_STAMM`) und die Projektdaten (`Tab_TwwZone`,
   `Tab_TwwWohnungstyp`, `Tab_TwwProjekt`); die Zapfkategorien folgen mit Z3 (T2), die
   VDI-4655-Typtage mit Z4b (T3). Die Nummern sind die nächsten freien Schritte nach
   `SchemaStand.Zielversion` — beim Beauftragen nachmessen. Der heutige Katalog `Tab_Brauchwasser*`
   bleibt unverändert beim Bestandsweg.

5. **Die Oberfläche ist eine Überlagerung im bestehenden Bedarfsprofil-Dialog** plus ein
   Katalogdialog in der Administration. `EPOS.UI/Dialoge/Bedarf/ZapfprofilDialog.razor` öffnet aus
   `BedarfsProfileDialog.razor` (Ausprägung Brauchwasser) über einen Knopf, den es nur gibt, wenn
   die Hülle den Delegaten reicht; die Delegaten tragen nur DTO aus `ZapfprofilDaten.cs`. Die Hülle
   `EPOS.UI.Daten/Bedarf/ZapfprofilHuelle.cs` ist plattformfrei; der **Einstieg** ist es nicht, weil
   die Bedarfsprofil-Hülle nur in der Windows-Schale liegt
   (`WindowsFormsApplication1/Views/Bedarf/BedarfsProfileHuelle.cs`). Auf dem iPad ist der Generator
   deshalb erst mit dem Umzug dieser Hülle erreichbar (Z4 oder iU11).

6. **Reihenfolge und Aufwand:** Z0 Grundlagen und Schema → Z1 Bilanz deterministisch mit Weiche →
   Z2 Auslegung deterministisch samt Speicherauslegung → Z3 Stochastik → Z4 Oberfläche vollständig
   (Z4b VDI-4655-Import getrennt) → Z5 Kalibrierung und Validierung; **74–96 PT** (±30 %, Annahme),
   dazu 3–5 PT für Z4b und 2–3 PT für die iPad-Voraussetzung, falls iU11 sie nicht mitbringt
   (Herleitung in Kapitel 7). Z1 ist der Nutzen für die Jahresarbeitszahl, Z2 behebt vor jeder
   Stochastik den Überschätzungsfehler der Bilanzspitze. Beschaffung (K1) und juristische Prüfung
   (K8) laufen mit Z0 an; bis dahin arbeiten Z1 und Z2 mit einem fiktiven Testkatalog.

---

## 1. Ausgangslage und Bestand

### 1.1 Der Brauchwasserkanal heute

Die Brauchwasserreihe entsteht aus **zwölf Monatswerten mal einem 168-Stunden-Wochenprofil**. Der Weg,
Schritt für Schritt:

| Schritt | Ort | Was geschieht |
|---|---|---|
| 1 | `SimulationWaermebedarf.Waermebedarf_berechnen` (`SimulationWaermebedarf.cs:128`), gerufen aus `SimulationRunner.cs:184`, `Controller/SimulationLaufCtrl.cs:120` und für die Kennzahlen der Startseite aus `WindowsFormsApplication1/Views/Hauptformular/StartseiteHuelle.cs:603` | Einstieg je Projekt und Klimaregion; alle drei rechnen bei gesetzter Weiche den Generator |
| 2 | `KlimakalenderLesen` (`:513`) | Wochenendkennzeichen in das Feld `WE` (`:21`, gefüllt in `:524`), daraus `WochentagJan1` (`:100`, gesetzt in `:534`); Stundentemperatur (`:51`) |
| 3 | `Brauchwasserwaerme_berechnen` (`:994-1012`) | `VectorInit` (`:999`), `ProfilBedarf.Vorschaumodus` (`:1001`, `ProfilBedarf.cs:315-320`), dann `ProfilBedarf.Rechnen` mit `ProfilQuelle.Brauchwasser(modus)` (`ProfilBedarf.cs:128-151`) |
| 4 | `ProfilBedarf.Rechnen` (`ProfilBedarf.cs:467-594`) | Namen aus der Sicht `Abfrage_Monatswaerme_Brauchwasser` (`Z_Projekt_Brauchwasser` ⋈ `Tab_Brauchwasser`); je Name `KopfLesen` (`:597`), `ProjektJahressumme` (`:617`) mit linearer Skalierung `pjv/jv`, Typ über den **Textbezug** `Typ`, `WochenprofilLesen` (`:633`, erste Zeile zu Typname und Projekt) |
| 5 | `BhkwPlan.StromWocheToJahr` (`EPOS.Kern/Allgemein/BhkwPlan.cs:221`) | kachelt das Wochenprofil ab dem Wochentag des 1. Januar auf 8760 h und normiert je Monat auf den Monatswert × 1000 — **Monatswerte in MWh, Ausgabe kWh je Stunde** |
| 6 | `BhkwPlan.VectorenAddieren` (`:82`), `MonatsSumme` (`:127`) | Addition auf `brauchwasserwerte[8760]` (`SimulationWaermebedarf.cs:92`), Monatssummen |
| 7 | `BrauchwassersummeUebernehmen` (`:1036`) | Jahresmenge in MWh über `Energieeinheit` |
| 8 | `:341-342` | Buchung in `_kanaele.Brauchwasser` und in die unabhängige Energieprobe `probe` (`:172`) |
| 9 | `:374`, `SimulationKanaele.cs:686` | Netzverluste anteilig auf die drei Kanäle (F2); danach trägt `brauchwasserwerte` den Netzverlustanteil (`:380`) |
| 10 | `Energieprobe` (`:390`, `:458`) | Kanalsumme je Stunde gegen die Probe |
| 11 | Abnehmer | `Kanal.BRAUCHWASSER` (`SimulationKanaele.cs:432`); Wärmepumpe `Warmwasserbedarf_stuendlich` (`SimulationWaermepumpe.cs:901`), Kaskade (`Kaskadenschleife.cs:220`), Mindest-Nutztemperatur (`SimulationControl.cs:3162`), Pufferspeicher (`SimulationPufferspeicher.cs:770`) |

Zwei Eigenheiten, die der Generator nicht erbt: Ein leerer Typ **bricht die ganze Bedarfsart ab**, und
der Kopf findet sein Wochenprofil über Text statt ID. Die Ausgabe des Generators ist wie im Bestandsweg
**kWh je Stunde**; die Monatssummen entstehen über denselben Baustein `MonatsSumme`.

### 1.2 Katalog und Datenmodell

Gelesen in der Testdatenbank `Referenzlaeufe/Kenndaten_Test.sqlite` (Schemastand = Zielversion):

| Tabelle | Inhalt | Zeilen |
|---|---|---|
| `Tab_Brauchwasser` (STRICT) | Kopf je Projekt: `Bezeichner`, `Typ` (Text), `Monat_1..12` REAL | 71 |
| `Tab_Brauchwasser_STAMM` | Katalogkopf, dazu `ReadOnly` mit CHECK | 16, davon 6 ReadOnly |
| `Tab_Brauchwassertyp` | Wochenprofil je Projekt, Spalten `"1".."168"` | 71 |
| `Tab_Brauchwassertyp_STAMM` | Katalog der Wochenprofile | 13, davon 4 ReadOnly |
| `Z_Projekt_Brauchwasser` | Zuordnung mit `Summe` (Projektjahressumme) | 17 |

Der Katalog ist in `EPOS.Kern/Allgemein/Katalog/KatalogRegistry.cs:250-264` als `BRAUCHWASSER` und
`BRAUCHWASSERTYP` registriert; die Filterspalten liefert `Katalogfilterprofil.FuerBedarf`
(`Katalogfilterprofil.cs:719`). Die Projektkopie aus dem Katalog macht
`BrauchwasserStammCtrl.CopyFromStamm` (`:108`) in einer `DbVorgang`-Transaktion, gerufen aus
`WizardCtrl.Add_Projekt_Brauchwasser` (`EPOS.Kern/Controller/WizardCtrl.cs:2763`, nimmt einen
`DbVorgang` an). Die Herkunft der Katalogtypen (vier aus VDI-6002-Bildern digitalisiert, die übrigen
generisch INEKON) beschreibt [`KONTEXT_Brauchwassertypen_VDI6002.md`](KONTEXT_Brauchwassertypen_VDI6002.md);
ihre Zählung dort bezieht sich auf den Auslieferungskatalog und weicht von der Testdatenbank ab.

Projekte mit Brauchwasser in der Testdatenbank: **mit Zuordnung** in `Z_Projekt_Brauchwasser` (sie
rechnen Brauchwasser über die Sicht) 17 Projekte — 1007, 1009, 1019, 1023, 1024, 1026–1029,
1039–1046; **nur Köpfe ohne Zuordnung** (rechnen kein Brauchwasser): 1031, 1032. Mehrere Köpfe haben
1019 (3), 1023 (43), 1024 (8) und 1043 (2). **Von den fünf CI-Projekten tragen 1007, 1045 und 1046
Brauchwasser**, 1030 und 1017 keins. Brauchwasser steht unter keiner der drei Einfrierregeln
([`Referenzlaeufe/LIESMICH.md`](../../Referenzlaeufe/LIESMICH.md), Abschnitte ab `:57`, `:79`, `:100`);
eine Änderung an `Tab_Brauchwasser*` dieser drei Projekte änderte trotzdem Ergebnisse — der Generator
fasst diese Tabellen nicht an.

### 1.3 Dialoge und Einstiege

| Maske | Ort | Rolle für den Generator |
|---|---|---|
| Bedarfsprofile des Projekts | `EPOS.UI/Dialoge/Bedarf/BedarfsProfileDialog.razor` (907 Z.), eine Komponente für drei Ausprägungen, `Zweispaltenauswahl` (`:73`), Katalogleiste (`:117`), Leiste „Simulation · monatlicher Verlauf" (`:171`, rechnet über `BedarfsVorschauCtrl.ProjektVorschau`), `SpeichernLeiste` nur ohne `Wizard` (`:177-179`) | Ort des Knopfs „Zapfprofil erzeugen…" und der Weiche |
| Wochen-Stundenprofil | `TypProfilDialog.razor` (702 Z.), 24 Zahlenfelder je Tag, Tag kopieren/einfügen, Grafik | Vorbild des Tagesgang-Editors |
| Katalogverwaltung | `BedarfAdminDialog.razor` (615 Z.), `Katalograhmen Gestapelt` (`:71`), Fuß Grafik · Typ ändern · Neu · Ändern · Löschen · Beenden | Vorbild des Katalogdialogs „Brauchwasser-Nutzungsarten" |
| Hülle des Projektdialogs | `WindowsFormsApplication1/Views/Bedarf/BedarfsProfileHuelle.cs` (568 Z.) — nur Windows; `Oeffnen` gibt nur `bool` zurück (`:43-67`) | reicht den Zapfprofil-Behälter durch (5.2) |

**Einstiege (Windows):** Startseite, Reiter Wärmebedarf, Kachel Brauchwasser
(`WindowsFormsApplication1/Views/Hauptformular/StartseiteHuelle.cs:669`, `:754`, `:900`; geschrieben
wird dort mit `Del/Add_Projekt_Brauchwasser`, `:909-910`), und Gebäudekatalog → „Brauchwasser…"
(`GebaeudeKatalogHuelle.cs:122-125`, `:252`), erreichbar aus Verwaltung, Projekt und Assistent. Im
Assistenten liegt das Zapfprofil damit in der dritten Überlagerung. Menü: Administration →
`MenuItem_Brauchwasser` (`EPOS.UI/Bausteine/Menuetabelle.cs:236`) →
`Seitenschluessel.BrauchwasserAdmin` (`EPOS.UI/Seiten/Seitenschluessel.cs:264`, Liste `:401`),
Maskenname `Masken.BrauchwasserAdmin` (`EPOS.Kern/Allgemein/Dienste/Masken.cs:63`), Windows-Fall
`WindowsFormsApplication1/Dienste/WinFormsNavigation.cs:91`.

**iOS:** Kachel ohne Wirkung, Verwaltung benannt abgelehnt, Bedarfsprofil-Hülle nur in der
Windows-Schale; ihr Umzug gehört zu iU11 ([Umsetzungskonzept iOS](Umsetzungskonzept_iOS_EPOS-Plan.md)).

### 1.4 Regressionsnetz

`EPOS.Referenzlauf` schreibt `waermebedarf_brauchwasser.csv` aus `wb.brauchwasserwerte`
(`Referenzlauf/Ergebnisexport.cs:60`) und `wp_warmwasserbedarf.csv`; gerechnet wird gegen die
aktuelle Basis unter `Referenzlaeufe/` ([`Referenzlaeufe/LIESMICH.md`](../../Referenzlaeufe/LIESMICH.md),
Abschnitt „Aktuelle Basis"). Die Abnahme ist der Vergleich innerhalb der Toleranz; der
Byte-Vergleich ist Information. Tests: `BedarfsProfilVorschauTests.cs`, `BedarfProfilTests.cs`,
`ReferenzprojektTests.cs` (Sammlung `Testdatenbank`). Einen Unit-Test der Energieprobe gibt es nicht;
`Energieprobe_Verletzungen` (`SimulationWaermebedarf.cs:425`) und `Energieprobe_MaxAbweichung`
(`:428`) taugen als Abnahme.

### 1.5 Außerhalb des Repositoriums: Wärmespeicher-Tool und Vorlage V4

Zwei INEKON-Arbeiten liegen in der Ablage des Anwenders und werden **benannt, nicht portiert**:

- **Wärmespeicher-Tool** (Python, Streamlit): Trinkwarmwasser mit drei Verfahren, Pufferspeicher nach
  vier Kriterien mit Betriebssimulation, synthetische Lastgänge über demandlib und lpagg (beide MIT)
  oder gemessene. Für EPOS-Plan methodischer Abgleich, keine Codequelle; kein Python zur Laufzeit.
- **Excel-Vorlage „TWW-Auslegung V4"** (reine Formelmappe, sechs Blätter): Wochen-Zapfprofile je
  Nutzungstyp (Werktag/Samstag/Sonntag × 24 h, Wochenfaktoren), Zweiwochenbilanz mit Lindley-Rekursion,
  DIN 4708, Faustwert mit Gleichzeitigkeit (Blatt „Berechnung", Faktor nach der lpagg-Funktion
  `calc_GLF`), klassischer Faustwert nachrichtlich, Listengröße mit Kriterium N_L ≥ N,
  Klartext-Warnungen. Sie ist **die Vorlage der Speicherauslegung** (4.7) und des Bedienmusters
  auto/manuell mit Rechenweg (5.3).

**Vier Schwächen der Vorlage, die nicht übernommen werden:**

1. **Fest eingetragene „angesetzte" Werte.** Ladeleistung und Zirkulation stehen dort als feste Zahl,
   der Umschalter auto/manuell wirkt nicht. Hier folgt der angesetzte Wert immer aus dem Umschalter
   (`Schaetzwert.Angesetzt`, 4.7).
2. **Gemischtes Mengengerüst.** Die Verfahren rechnen dort mit zwei Personenzahlen. Hier liest jedes
   Verfahren dasselbe Mengengerüst samt derselben Wohnungstabelle (3.1); was ein Verfahren nicht
   abdeckt, meldet seine Gültigkeitsprüfung.
3. **Saisonfaktor als einziger Jahresgang.** Hier tragen zwölf Monatsfaktoren, Kaltwasser-Saisonalität
   und Kalender den Jahresgang (S2).
4. **336-Stunden-Raster statt Jahr.** Die Bilanz rechnet 8760 Stunden; die Zweiwochenbilanz bleibt nur
   als Verfahren der Speicherauslegung, mit einer eigens gebildeten Wochenreihe (4.7).

Dazu drei kleine Korrekturen: Der maßgebende Zeitpunkt wird in Woche 2 gezählt, jedes Verfahren
trägt eine Gültigkeitsprüfung, und das Ergebnis des Verfahrensvergleichs ist keine Empfehlung mehr
(4.7).

### 1.6 Was dieses Papier am Konzept und am Mockup korrigiert

| Stelle | Konzept bzw. Mockup | Dieses Papier |
|---|---|---|
| Konzept Teil 3.0–3.2, 3.5 | WinForms-Tripel, Access, `UpdateDB.ini`, `float`, native DLL, Spaltenfehler `M1…M12` | Kern + Razor + SQLite mit `SchemaMigration`, `double`, C#-Port `BhkwPlan`; der Spaltenfehler ist behoben (`BrauchwasserCtrl.cs:78-102`), die Access-Hygiene entfällt |
| Konzept 3.1 | eigenes Klassenbibliotheks- und Testprojekt | Ordner `EPOS.Kern/Allgemein/Zapfprofil/`, Tests in `EPOS.Kern.Tests` (A1) |
| Konzept 3.2 | A100- und Ecodesign-Profile als Katalogeinträge `Tab_Zapfprofil` | Bedarfstage der Auslegung als eigener Katalog `Tab_TwwBedarfstag_STAMM` schon in T1 (Normprofile erst nach K1/K8); die Bilanzreihe wird **nicht gespeichert**, sondern je Lauf gerechnet (A2) |
| Konzept 3.4 | Normkennwerte „im Code als gekapselte Parameter" | Normkonstanten nie im Quelltext, sondern als Parametersatz aus `Tab_TwwParameter_STAMM`; Tests mit erfundenen Parametern (Kapitel 6) |
| Konzept 3.6, V1.2 Punkt 1(c) | VDI 4655 als Validierungsanker mit Zahlen im Regressionstest | kein VDI-4655-Wert in Repository, Tests oder CI (A12, Kapitel 6) |
| Konzept 3.6 | Schaltjahrprüfung im Kalender | entfällt — der Kern rechnet 365 Tage ohne Schaltjahr |
| Konzept S5 | Zirkulation als eigener, additiver Lastkanal | Teilreihe im Brauchwasserkanal, getrennt ausgewiesen (A4); eigene Monatssummen `Waermebedarf_Brauchwasser_Zirkulation_Monat` (2.2) |
| Konzept 2.4 | Stufen Schnellauslegung · Standard · Experte | Namen des Mockups **Einfach · Erweitert · Experte**; die Auslegungswerte der Stufe Einfach tragen den Vermerk „Schnellauslegung" (5.1). Auslastungsgang als Experten-Überschreibung der zwölf Monatsfaktoren je Zone, Zapfkategorien und σ im Experten-Modus als Katalogkopie bearbeitbar (Z4) |
| Mockup A4 | generierte Zirkulation nimmt den Brauchwasseranteil aus der Netzverlustverteilung (F2) | die Verteilung bleibt unverändert: `Netzverluste` sind eine Projektgröße mit anderer Bilanzgrenze; der Dialog warnt, wenn beide gesetzt sind (Frage ZU5) |
| Mockup A9 | die vier aus VDI-6002-Bildern digitalisierten Bestandstypen durch Eigenkonstruktionen ersetzen | **offen bis K8:** der neue Katalog übernimmt sie nicht; ob der Bestandskatalog sie weiter ausliefert, entscheidet K8. Sagt K8 „ersetzen", folgt ein eigener Schritt mit Einfrierprüfung der Referenzprojekte 1007, 1045, 1046 (Kapitel 7, Risiko in Kapitel 8) |
| Mockup „bitgleich auf Windows und iOS" | fester Seed genügt | ein Seed garantiert Gleichheit nur mit ganzzahligem Zufall **und** ohne plattformabhängige Mathematik-Bibliothek; Lösung in 4.2 und 4.4, Frage ZU8 |

---

## 2. Zielbild und Einbindung in den Kern

### 2.1 Die Schichten als Klassen

Ort: `EPOS.Kern/Allgemein/Zapfprofil/`, Namensraum `WindowsFormsApplication1` wie der übrige Kern
(`PvErweitertesModell.cs`, `SimulationKanaele.cs`). Keine Datei kennt `DataRepository`, `Dienste` oder
eine Oberfläche; alle Methoden sind rein. Vorbild der Bauart ist `PvErweitertesModell.cs`.
**Sichtbarkeit:** alle Typen des Ordners sind `internal`; die Schalen, die Hülle und die Tests sehen
sie über die bestehenden `InternalsVisibleTo`-Einträge des Kerns (`EPOS.Kern/EPOS.Kern.csproj:67-93`:
`EPOS_Plan`, `EPOS.Kern.Tests`, `EPOS.iOS`, `EPOS.UI.Daten`). `EPOS.UI` steht dort nicht und bekommt
**keinen** Kern-Typ des Zapfprofils zu sehen — es arbeitet mit den DTO aus `ZapfprofilDaten.cs` (5.1).
**Namen:** Die Aufzählungen tragen das Präfix `Zapf`, damit sie im gemeinsamen Namensraum nicht mit
Bestandsnamen verwechselt werden (`TagTyp_W` in `SimulationWaermebedarf.cs:23`); die Klasse der
Speicherauslegung heißt `TwwSpeicherauslegung`, getrennt vom Stromspeicher-Bestand
`SpeicherAuslegungCtrl` und `Tab_SpeicherAuslegung`.

| Schicht | Datei | Kern der Klasse |
|---|---|---|
| S0 Katalog | `Nutzungsart.cs` | `sealed record Nutzungsart(int Id, string Name, ZapfBezugsart Bezug, double[] BedarfJeNiveauKwhJeEinheitTag /*3*/, Temperaturbezug Bezugstemperaturen, ZapfBilanzgrenze Grenze, ZapfKalenderart Kalender, double? Ferienfaktor, double[] Monatsfaktoren /*12*/, double[] Wochenfaktoren /*7*/, Tagesgangsatz Tagesgaenge, Katalogherkunft Herkunft)`; `sealed record Tagesgangsatz(int Id, double[,] Anteile /*4×24*/, Provenienz[] JeTagtyp /*4*/)`; Aufzählungen `ZapfBezugsart`, `ZapfBilanzgrenze`, `ZapfKalenderart`, `ZapfNiveau`, `ZapfTopologie` |
| Provenienz | `Provenienz.cs` | `sealed record Provenienz(string Quelle, string? Ausgabe, string Version, Herkunftsart Art)` je Wertgruppe; `Katalogherkunft` bündelt die Gruppen Bedarf (mit Bandbreite Min/Max je Niveau), Jahresgang, Wochengang, Tagesgang je Tagtyp; `enum Wertstatus { Vorgabe, Ueberschrieben, Kalibriert, Umgerechnet }`; `Herkunftsprotokoll` (Liste je Feld und Zone) |
| Parameter | `Parametersatz.cs` | Normkonstanten und Regelwerksgrenzen als `IReadOnlyDictionary<string, Parameterwert>` aus `Tab_TwwParameter_STAMM`; die Schlüssel stehen im Code, die Werte nie. Fehlt ein Parameter, meldet das Verfahren „nicht rechenbar — Parameter fehlt" statt eines Rückfallwerts |
| Eingang | `Zoneneingang.cs`, `Zapfprofileingang.cs` | Zone: Nutzungsart-ID, Bezugsmenge, Niveau, Topologie, Wohnungstabelle und **nullbare** Überschreibungen (`null` = Vorgabe); Eingang: Zonen, Kalender (`WochentagJan1`, `bool[365] We`), Gebäudegrößen (Zirkulation, Ladeleistung), `RechenwegJahresreihe`, `Seed`, `Realisierungen`, `Parametersatz` |
| Arbeitsstand | `ZapfprofilStand.cs` | `internal sealed record ZapfprofilStand(BrauchwasserWeg Weg, IReadOnlyList<ZonenStand> Zonen, ProjektStand Projekt)` — die Übergabeform zwischen `ZapfprofilCtrl` und Hülle, ohne Oberflächenbezug |
| S1 | `Mengengeruest.cs` | `static double JahresenergieKwh(Zoneneingang z, Nutzungsart n, Parametersatz ps, Herkunftsprotokoll p)`; `static double Temperaturfaktor(...)`; `static Kalibrierergebnis Kalibrieren(Messwert m, double zapfungKwh, double zirkulationKwh)` |
| S2 Kalender | `Zapfkalender.cs` | `enum ZapfTagtyp { Werktag = 1, Samstag = 2, SonnFeiertag = 3, Ruhetag = 4 }`; `static ZapfTagtyp[] Bilden(int wochentagJan1, bool[] we, Ferienfenster[] ferien)` — 365 Einträge; `static Ferienfenster? AusJahrestagen(int beginn, int ende)` |
| S2 Formvektor | `Formvektor.cs` | `static double[] Tagesmengen(double jahresKwh, Nutzungsart n, ZapfTagtyp[] kalender, int wochentagJan1, double[] kaltwasserfaktor)`; `static double[] Stundenreihe(double[] tagesmengen, Nutzungsart n, ZapfTagtyp[] kalender)` — 8760, nur für die Bilanz; `static Wochenreihe Wochenreihe(double[] tagesmengen, Nutzungsart n, ZapfTagtyp[] kalender, int ersterTag)` — 168 h, nur für die Auslegung |
| S2 Kaltwasser | `Kaltwassergang.cs` | `static double[] Monatswerte(double mittelC, double amplitudeK, int monatMaximum)` — zwölf Werte, einmal gerechnet und auf neun Stellen gerundet (4.2); `static double[] Monatsfaktoren(double zapfC, double[] monatswerteC, double mittelC)` |
| S3 | `ZapfZufall.cs`, `Zapfkategorie.cs`, `Zapfereignisgenerator.cs`, `Zapfensemble.cs` | portabler Zufall; Kategorien; Minutenreihe je Zone aus `n_E` Einheiten; Ensemble mit Perzentilen je Topologie (4.4) |
| S4 | `Bedarfstag.cs`, `Wochenreihe.cs`, `Summenlinie.cs`, `Din4708Kennzahl.cs`, `TwwSpeicherauslegung.cs`, `Grossanlage.cs`, `Auslegungsergebnis.cs` | Dreiergruppe, Verfahrensvergleich, Großanlagenerkennung (4.5–4.7) |
| S5 | `Zirkulationskanal.cs` | `static Bilanzreihe Reihe(ZirkulationEingang e, IReadOnlyList<Zonenanteil> zonen, Parametersatz ps, Herkunftsprotokoll p)` — 8760, eigene Teilreihe |
| S6 | `Kalibrierung.cs` | Faktor, Dauerlinienvergleich, Bericht synthetisch gegen gemessen |
| Fassade | `ZapfprofilRechner.cs`, `ZapfprofilErgebnis.cs`, `Bilanzreihe.cs` | `static ZapfprofilErgebnis Rechnen(Zapfprofileingang e, IReadOnlyList<Nutzungsart> katalog)`; `sealed record Bilanzreihe(double[] StundenKwh)`; Ergebnis: `Bilanzreihe Zapfung`, `Bilanzreihe Zirkulation`, `JeZone`, Kennzahlen mit Einheit im Namen (`JahresbedarfZapfungKwh`, `JahresverlustZirkulationKwh`, `GroessterStundenwertKw`), `Herkunftsprotokoll`, `Hinweise` |
| Import | `Normformvektorleser.cs`, `Typtagzuordnung.cs` (Z4b) | liest anwendereigene VDI-4655-Typtage aus einem `Stream` (Muster `TryPaketLeser.AusStrom`, `EPOS.Kern/Allgemein/Import/TryPaketLeser.cs:362`); ordnet sie dem Kalender zu (4.2) |

Die Auslegung hat eine eigene Fassade, damit sie strukturell keinen Weg zur Bilanzreihe hat:

```csharp
internal static class ZapfprofilAuslegung
{
    // bekommt Katalog, Zonen, Bedarfstag und Wochenreihe — ausdrücklich keine Bilanzreihe
    internal static Auslegungsergebnis Rechnen(Zapfprofileingang e,
        IReadOnlyList<Nutzungsart> katalog, Bedarfstag tag, Wochenreihe woche,
        Auslegungseingang a, Zapfensemble? ensemble);
}
```

### 2.2 Die Weiche

Ort: `SimulationWaermebedarf.Brauchwasserwaerme_berechnen` (`:994`), **nach** `:1001`, weil der
Einschub `modus` braucht, das dort entsteht; nur im Modus `ProfilQuellmodus.Projektrechnung`:

```csharp
if (modus == ProfilQuellmodus.Projektrechnung
    && ZapfprofilCtrl.Weg(m_ID_Projekt) == BrauchwasserWeg.Generator)
{
    ZapfprofilErgebnis e = ZapfprofilRechner.Rechnen(
        ZapfprofilCtrl.Eingang(m_ID_Projekt, WochentagJan1, WE), ZapfprofilCtrl.Katalog());
    BhkwPlan.VectorenAddieren(e.Zapfung.StundenKwh, brauchwasserwerte);
    BhkwPlan.VectorenAddieren(e.Zirkulation.StundenKwh, brauchwasserwerte);
    Brauchwasser_Zirkulation_Mwh = Energieeinheit.MWh.AusKWh(e.Zirkulation.StundenKwh.Sum());
    BhkwPlan.MonatsSumme(brauchwasserwerte, Waermebedarf_Brauchwasser_Monat, mo_anfang, mo_ende);
    BhkwPlan.MonatsSumme(e.Zirkulation.StundenKwh, Waermebedarf_Brauchwasser_Zirkulation_Monat,
                         mo_anfang, mo_ende);
    return;
}
```

**Regeln der Weiche:**

- **Exklusiv (A3).** Je Projekt genau ein Weg. Die Bestandszeilen in `Z_Projekt_Brauchwasser` bleiben
  liegen, rechnen aber nicht mit, solange der Generator gewählt ist; der Dialog sagt das (5.2).
- **Vorgabe Bestandsweg.** Ohne Zeile in `Tab_TwwProjekt` oder bei `Weg = 'BESTAND'` läuft der
  heutige Code Zeichen für Zeichen. Damit ändert keine Stufe ein Referenzprojekt.
- **Kein stiller Rückfall.** Fehlt dem Generator eine Eingabe (Zone ohne Bezugsmenge, Nutzungsart
  gelöscht, Parameter fehlt), meldet `ZapfprofilCtrl.Eingang` den Grund über
  `SimulationProtokoll.Aktuell.Warnung` und die Zone trägt 0 — derselbe Ausgang wie der Bestandsweg
  bei Nullprofil, aber mit benannter Zone.
- **Kalender und Probe.** Die Wochenendkennzeichen liegen schon im Feld `WE` (`:21`), gefüllt in
  `KlimakalenderLesen` (`:524`); ein neues Feld gibt es nicht. Weil die Reihe vor `:341` auf
  `brauchwasserwerte` liegt, bucht `:342` Zapfung **und** Zirkulation in die Energieprobe. Neue
  Anzeigefelder `Brauchwasser_Zirkulation_Mwh` und `Waermebedarf_Brauchwasser_Zirkulation_Monat[12]`
  (getrennte Monatssummen für den Monatsstapel, 5.6).
- **Vorschau im Bedarfsprofil-Dialog.** Die Leiste „Simulation · monatlicher Verlauf" rechnet über
  `BedarfsVorschauCtrl.ProjektVorschau` → `Brauchwasserwaerme_berechnen(liste)`
  (`BedarfsVorschauCtrl.cs:130`, `:165`) im Modus `Projektvorschau` (`ProfilBedarf.cs:318`) und ginge
  an der Weiche vorbei. Deshalb: Steht der Arbeitsstand des Dialogs auf `GENERATOR`, zeigt die Leiste
  die Monatssummen aus `ZapfprofilRechner.Rechnen` über den Arbeitsstand (Delegat der Hülle, 5.2) —
  derselbe Aufruf wie im Lauf; bei `BESTAND` bleibt der heutige Weg. So gilt „Vorschau gleich Lauf"
  (2.4) auch für diese Leiste.

Die feinere Weiche je Kopf in `ProfilBedarf.Rechnen` (Einhängepunkt B der Erkundung) wird **nicht**
gebaut: Sie kollidierte mit dem Abbruch bei leerem Typ und mit der Aussage, die drei `ProfilQuelle`-
Fabriken seien die vollständige Liste der Bedarfsarten (`ProfilBedarf.cs:67-71`). `BedarfsArt`
(`EPOS.Kern/Model/BedarfsArt.cs:20`) bekommt kein viertes Mitglied.

### 2.3 Datenfluss in Sätzen

1. `KlimakalenderLesen` liefert `WochentagJan1` und die 365 Kennzeichen `WE` der Klimaregion.
2. `ZapfprofilCtrl.Weg` entscheidet; beim Generator bauen `ZapfprofilCtrl.Eingang` (aus `Tab_TwwZone`,
   `Tab_TwwWohnungstyp`, `Tab_TwwProjekt`, `Tab_TwwParameter_STAMM`) und `ZapfprofilCtrl.Katalog` den
   Eingang.
3. `Mengengeruest` bildet je Zone die Jahres-Nutzenergie auf die Projekttemperaturen umgerechnet;
   `Zirkulationskanal` bestimmt die Zirkulationsanteile der Zonen; erst danach kalibriert
   `Mengengeruest.Kalibrieren` gegen Messwerte (4.1). `Zapfkalender` und `Kaltwassergang` liefern
   Tagtypen und Monatsfaktoren; `Formvektor` verteilt auf 8760 Stunden — oder `Zapfensemble` liefert
   die Realisierung zum Seed, wenn die Jahresreihe auf „stochastisch" steht.
4. `ZapfprofilRechner` summiert die Zonen zu `Zapfung` und `Zirkulation`; `VectorenAddieren` und
   `MonatsSumme` übergeben. Ab `:339` läuft alles wie heute.
5. Die Auslegung läuft **nur im Dialog** über `ZapfprofilAuslegung`; ihr gewählter Punkt steht in
   `Tab_TwwProjekt` und wird vom Lauf nicht gelesen.

### 2.4 Invarianten

| Invariante | Prüfung | Test |
|---|---|---|
| Energieerhaltung S1→S2: Σ Tagesmengen = Jahresmenge, Σ Stundenwerte = Jahresmenge | relativ 1e-12 | `FormvektorTests.Die_Stundenreihe_erhaelt_die_Jahresmenge` |
| Formvektor-Summe: jeder Tagesgang Σ = 1, Wochenfaktoren Σ = 1 (Katalog), Warnung ab einer Abweichung über der Warnschwelle (INEKON-Setzung, Parameter), Normierung vor dem Rechnen | exakt nach Normierung | `NutzungsartTests.Tagesgang_und_Woche_summieren_zu_eins` |
| Kalender: genau 365 Tage, kein Schaltjahr, Wochentag aus `WochentagJan1`, Gewicht je Tagtyp (4.2) | exakt | `ZapfkalenderTests.Das_Jahr_hat_365_Tage_und_beginnt_am_Wochentag_des_Januars`, `…Ein_Feiertag_am_Montag_erhaelt_die_Sonntagsmenge` |
| Zirkulation: Σ_h q_zirk,h = Jahresverlust der gewählten Methode | relativ 1e-12 | `ZirkulationskanalTests.Die_Reihe_erhaelt_den_Jahresverlust_jeder_Methode` |
| Konsistenz der Pfade: E[Jahresenergie stochastisch] = deterministisch | Toleranz max(1 %, 3·s_R/√R) (4.4) | `ZapfensembleTests.Der_Erwartungswert_trifft_den_deterministischen_Pfad` |
| Vorschau gleich Lauf: Dialogvorschau, Leiste „monatlicher Verlauf" und Lauf liefern für denselben Stand dieselbe Reihe | byte-gleich | `ZapfprofilWeicheTests.Vorschau_und_Lauf_rechnen_dieselbe_Reihe`, Fall in `BedarfsProfileDialogTests` |
| Energieprobe greift auch im Generatorweg | `Energieprobe_Verletzungen == 0` | `ZapfprofilWeicheTests.Die_Energieprobe_zaehlt_Zapfung_und_Zirkulation` |
| Bestandsweg unverändert, wenn keine Weiche gesetzt | Referenzlauf grün innerhalb der Toleranz; Byte-Gleichheit erwartet und als Information ausgewiesen | Referenzlauf der fünf CI-Projekte |
| Auslegung ohne Bilanzreihe | Quelltext-Wache: `Summenlinie`, `Din4708Kennzahl`, `TwwSpeicherauslegung`, `Zapfensemble`-Auswertung und `ZapfprofilAuslegung` referenzieren weder `Bilanzreihe`, `ZapfprofilErgebnis`, `ZapfprofilRechner` noch `Formvektor.Stundenreihe`; Minutenwerte kommen nur über `Bedarfstag`, Stundenwerte nur über `Wochenreihe` (168 h); kein `double[]` in ihren Signaturen außerhalb dieser beiden Typen | `ZapfprofilTrennungWacheTests` |
| Genau eine Empfehlung: das Auslegungsergebnis trägt je Topologiegruppe genau einen empfohlenen Punkt, den der Summenlinie bzw. der Minutenspitze | exakt | `AuslegungsergebnisTests.Es_gibt_genau_eine_Empfehlung` |
| Gleichzeitigkeitsfaktor der Vorlage: GLF(1) = 1, GLF fällt monoton in N, V_GLF steigt monoton in N | exakt bzw. Vorzeichen der Differenzen | `SpeicherauslegungTests.GLF_ist_eins_bei_einer_Einheit_und_V_steigt_mit_N` |
| Provenienz: jede Katalogzeile trägt Quelle, Version und Herkunftsart je Wertgruppe | NOT NULL im Schema, Prüfung über alle `Tab_Tww*_STAMM` | `TwwKatalogWacheTests.Jede_Katalogzeile_hat_Provenienz_je_Wertgruppe` |
| Testdatenbank ohne Auslieferungswerte: keine Zeile einer `Tab_Tww*_STAMM` mit `Status = 'AUSLIEFERUNG'` | exakt | `TwwKatalogWacheTests.Die_Testdatenbank_traegt_nur_fiktive_Werte` |

### 2.5 Schnittstellen zu `Dienste.*`

Der Rechenweg braucht keine Umgebung. Zwei Stellen berühren die Dienste (`EPOS.Kern/Allgemein/Dienste/Dienste.cs:42-67`):

- **Import anwendereigener VDI-4655-Daten (Z4b, nach K3a und K8):** Datei über
  `IDateiDienst.DateiOeffnen`, Startordner aus `IEinstellungen.Lies("Zapfprofil.Importordner", "")`
  (Muster `VDI3805Path`); der Kern liest einen `Stream` (`Normformvektorleser.AusStrom`), prüft Struktur
  und Summen und legt die Werte **im eigenen Datenmodell** `Tab_TwwTyptag_IMPORT` (T3, 3.2) ab — nicht
  in den Katalogtabellen, deren vier Tagtypen die VDI-Typtagsystematik nicht fassen; auf iOS derselbe
  Weg.
- **Lokale Testdaten:** Tests finden sie unter `Referenzlaeufe/Normzahlen/` relativ zur Repowurzel und
  **schweigen** ohne Ordner (Muster `TestDatenbank`, `LfsZeigerProbe`); kein Dienst, kein `IPfade`.

---

## 3. Datenmodell und Schema

### 3.1 Die neuen Tabellen

Alle Tabellen `STRICT`, Beziehungen über IDs mit Fremdschlüssel (`REFERENCES … ON DELETE …`),
Booleans als `INTEGER NOT NULL DEFAULT … CHECK ("spalte" IN (0,1))`, Aufzählungen als INTEGER mit
CHECK der Wertemenge, **NULL = Vorgabe** bei jeder Überschreibung. Jede `ID` ist `INTEGER PRIMARY
KEY AUTOINCREMENT`, damit eine gelöschte ID nie wieder vergeben wird (N2). Umlautregel und Verbotsliste nach
[`BETRIEB_SQLITE.md`](BETRIEB_SQLITE.md) § 6. Die Projekttabellen heißen `Tab_Tww…`, nicht `Z_…`: sie
tragen eigene Fachdaten, keine reine Zuordnung Projekt ↔ Katalog.

**Provenienzgruppe.** Wo unten „Provenienz G" steht, trägt die Tabelle vier Spalten:
`G_Quelle` TEXT NOT NULL (nur Norm, Verfahren oder Eigenkonstruktion und Ausgabe — **nie** ein
Hersteller- oder Produktname, nie Zahlen), `G_Ausgabe` TEXT, `G_Version` TEXT NOT NULL (Katalogversion,
in der die Gruppe zuletzt gesetzt wurde), `G_Herkunftsart` TEXT NOT NULL CHECK (IN
('VERFAHREN','EIGENKONSTRUKTION','FREI','IMPORT','FIKTIV')). Eine Sekundärquelle, falls nötig, steht
in der internen Spalte `Beleg` TEXT je Zeile; Oberfläche, Bericht und KiSicht zeigen `Beleg` nie.

**`Tab_TwwNutzungsart_STAMM`** — der Katalog (S0), Auslieferung und Anwenderkopien in einer Tabelle:

| Spalte | Typ | Bemerkung |
|---|---|---|
| `ID` | INTEGER PRIMARY KEY AUTOINCREMENT (N2) | |
| `Bezeichner`, `Katalogversion` | TEXT NOT NULL, TEXT NOT NULL; UNIQUE (`Bezeichner`, `Katalogversion`) | neutraler Name; der Schlüssel ist zugleich der natürliche Schlüssel für Projektexport und -import (3.2) |
| `Bezugsart` | INTEGER NOT NULL CHECK (`Bezugsart` IN (1,2,3,4,5,6,7)) | Personen, Wohneinheiten, Betten, Duschplätze, Sitzplätze, Beschäftigte, Fläche (nur Rückfall) |
| `Bedarf_Niedrig`, `Bedarf_Mittel`, `Bedarf_Hoch` | REAL NOT NULL | kWh je Einheit und Tag bei den Bezugstemperaturen |
| `Bedarf_Niedrig_Min` … `Bedarf_Hoch_Max` | REAL | Bandbreite je Niveau (sechs Spalten) |
| Provenienz `Bedarf` | vier Spalten | |
| `Bezug_Zapftemperatur`, `Bezug_Kaltwasser` | REAL NOT NULL | °C; Temperaturen, auf die sich die Bedarfswerte beziehen (4.0) |
| `Bilanzgrenze` | INTEGER NOT NULL CHECK (`Bilanzgrenze` IN (1,2,3)) | 1 Zapfstelle; 2 mit Verteil- und Zirkulationsverlust; 3 mit Speicherverlust |
| `Kalenderart` | INTEGER NOT NULL CHECK (`Kalenderart` IN (1,2,3,4,5)) | Wohnen, Arbeitstage, Schulferien, Betrieb, Auslastungsgang |
| `Ferienfaktor` | REAL | Gewicht eines Ruhetags relativ zum mittleren Wochentag (4.2); NULL = wie Sonntag |
| `Monat_1` … `Monat_12` | REAL NOT NULL | Jahresgang, Mittel 1 |
| Provenienz `Jahresgang` | vier Spalten | |
| `Woche_1` … `Woche_7` | REAL NOT NULL | Wochenfaktoren Mo–So, Σ 1 |
| Provenienz `Wochengang` | vier Spalten | |
| `ID_Tagesgangsatz` | INTEGER NOT NULL REFERENCES `Tab_TwwTagesgangsatz_STAMM(ID)` | Vorgabesatz der Tagesgänge |
| `ID_Vorlage` | INTEGER NULL REFERENCES `Tab_TwwNutzungsart_STAMM(ID)` ON DELETE SET NULL | Kopie bzw. Nachfolgeversion von … |
| `Status` | TEXT NOT NULL CHECK (`Status` IN ('AUSLIEFERUNG','EIGEN','IMPORT')) | |
| `Beleg` | TEXT | intern (siehe oben) |
| `Freigabe` | TEXT | Vier-Augen-Vermerk (K7) |
| `ReadOnly` | INTEGER NOT NULL DEFAULT 0 CHECK (`ReadOnly` IN (0,1)) | 1 = gehört zur Auslieferung |

**`Tab_TwwTagesgangsatz_STAMM`** — ein Satz von vier Tagesgängen, eigenständig geschlüsselt, damit
eine Zone einen anderen Satz als den ihrer Nutzungsart wählen kann (Eigenkonstruktion, Ecodesign,
Anwenderkopie): `ID` INTEGER PRIMARY KEY AUTOINCREMENT, `Bezeichner` TEXT NOT NULL, `Katalogversion` TEXT NOT NULL,
UNIQUE (`Bezeichner`, `Katalogversion`), `Status` wie oben, `Beleg`, `ReadOnly` wie oben.

**`Tab_TwwTagesgang_STAMM`** — Tagesgangsatz × Tagtyp × 24:

| Spalte | Typ | Bemerkung |
|---|---|---|
| `ID` | INTEGER PRIMARY KEY AUTOINCREMENT | |
| `ID_Tagesgangsatz` | INTEGER NOT NULL REFERENCES `Tab_TwwTagesgangsatz_STAMM(ID)` ON DELETE CASCADE | |
| `Tagtyp` | INTEGER NOT NULL CHECK (`Tagtyp` IN (1,2,3,4)) | Werktag, Samstag, Sonn-/Feiertag, Ruhetag |
| `Anteil_01` … `Anteil_24` | REAL NOT NULL | Σ 1; die Spaltennamen entstehen im Controller aus einer Schleife, nie aus einer Eingabe |
| Provenienz (ohne Präfix) | `Quelle`, `Ausgabe`, `Version`, `Herkunftsart` | je Tagtyp, damit ein angepasster Tagesgang nachvollziehbar bleibt |
| | UNIQUE (`ID_Tagesgangsatz`, `Tagtyp`) | |

**`Tab_TwwBedarfstag_STAMM`** und **`Tab_TwwBedarfstagEreignis_STAMM`** — Bedarfstage der Auslegung
(4.5), schon in T1, damit Konstruktor-, Ecodesign- und später Normprofile dieselbe Ablage haben:
Kopf `ID`, `Bezeichner`, `Katalogversion`, `Quelle_Art` INTEGER NOT NULL CHECK (IN (2,3,4,5)) (A100-Referenz,
DIN-4708-Profil, Konstruktor, Ecodesign), `Bezugsmenge` REAL (für die Skalierung, etwa N oder Personen),
Provenienz ohne Präfix, `Status`, `Beleg`, `ReadOnly`; Zeilen `ID_Bedarfstag` REFERENCES … ON DELETE
CASCADE, `Minute_Beginn` INTEGER CHECK (0..1439), `Dauer_min` INTEGER CHECK (≥ 1), `Energie_Kwh` REAL
NOT NULL, `Reihenfolge`. Eine Minutenreihe ist eine Ereignisliste mit Dauer 1; eine
1440-Spalten-Tabelle gibt es nicht.

**`Tab_TwwParameter_STAMM`** — gekapselte Normkonstanten, Regelwerksgrenzen und INEKON-Setzungen, die
kein Quelltext enthält: `ID`, `Schluessel` TEXT NOT NULL (Name im Code, etwa `DIN4708.a1`,
`W551.Mindesttemperatur`, `A100.Vereinfachung.Anwendungsgrenze`, `A100.Zeitkonstante.Koeffizient`,
`Zirkulation.Kennwert.Lage1`), `Wert` REAL NOT NULL, `Einheit` TEXT, `Katalogversion` TEXT NOT NULL,
UNIQUE (`Schluessel`, `Katalogversion`), Provenienz ohne Präfix, `Status`, `Beleg`, `ReadOnly`.

**`Tab_TwwDin4708Wert_STAMM`** — die Katalogwerte der DIN-4708-Kennzahl, nie abgedruckt: `ID`, `Art`
TEXT NOT NULL CHECK (`Art` IN ('BELEGUNG','AUSSTATTUNG')), `Schluessel` TEXT NOT NULL (Raumzahl bzw.
neutraler Name der Ausstattungsklasse), `Wert` REAL NOT NULL (Belegung p bzw. Σ v·w_v der Klasse),
`Katalogversion`, UNIQUE (`Art`, `Schluessel`, `Katalogversion`), Provenienz ohne Präfix, `Status`,
`Beleg`, `ReadOnly`.

**`Tab_TwwZone`** — die Zonen eines Projekts:

| Spalte | Typ | Bemerkung |
|---|---|---|
| `ID` | INTEGER PRIMARY KEY AUTOINCREMENT | |
| `ID_Projekt` | INTEGER NOT NULL REFERENCES `Tab_Projekt(ID)` ON DELETE CASCADE | |
| `ID_Nutzungsart` | INTEGER NOT NULL REFERENCES `Tab_TwwNutzungsart_STAMM(ID)` | Katalogverweis auf eine unveränderliche Version (3.2); Löschen einer benutzten Nutzungsart ist gesperrt |
| `ID_Tagesgangsatz` | INTEGER NULL REFERENCES `Tab_TwwTagesgangsatz_STAMM(ID)` | Experte; NULL = Satz der Nutzungsart |
| `ID_Gebaeude` | INTEGER NULL REFERENCES `Tab_Gebaeude(ID)` ON DELETE SET NULL | optionale Bindung (A8); belegt nur vor. Ziel ist `Tab_Gebaeude.ID`, nicht `ID_ProjektGebaeude` |
| `Reihenfolge`, `Name` | INTEGER NOT NULL, TEXT NOT NULL | |
| `Bezugsmenge` | REAL NOT NULL CHECK (`Bezugsmenge` > 0) | Menge in der Bezugsart der Nutzungsart; bei vorhandener Wohnungstabelle aus ihr gebildet |
| `Niveau` | INTEGER NOT NULL DEFAULT 2 CHECK (`Niveau` IN (1,2,3)) | |
| `Personen_je_WE`, `Wohnflaeche_je_WE` | REAL | NULL = Vorgabe bzw. aus der Wohnungstabelle |
| `Topologie` | INTEGER NOT NULL DEFAULT 1 CHECK (`Topologie` IN (1,2,3,4)) | Speicher, Frischwasserstation, Durchfluss, Wohnungsstation — wirkt in der Auslegung (4.5) |
| `Zirkulation` | INTEGER NOT NULL DEFAULT 1 CHECK (`Zirkulation` IN (0,1)) | 0 nimmt die Zone aus dem Zirkulationsanteil (4.3) |
| `Ferienbeginn_1`, `Ferienende_1` … `_4` | INTEGER | Jahrestag 1–365 wie `Tab_Gebaeude`; NULL, 0 und 366 = keine Angabe (4.2) |
| `Jahresmesswert` | REAL | S6 |
| `Jahresmesswert_Einheit` | INTEGER CHECK (`Jahresmesswert_Einheit` IN (1,2)) | 1 kWh/a, 2 m³/a (Umrechnung 4.1) |
| `Jahresmesswert_Bilanzgrenze` | INTEGER CHECK (`Jahresmesswert_Bilanzgrenze` IN (1,2,3)) | wie im Katalog; ein Volumenmesswert ist immer 1 |
| `Jahresmesswert_Quelle`, `Jahresmesswert_Zeitraum` | TEXT | Herkunft des Messwerts („überschrieben durch Messdaten vom …", Konzept S6) |
| `Speicherverlust_Kwh_a` | REAL | nur bei Messwert-Grenze 3: abzuziehender Speicherverlust |
| `Tagesbedarf_Auto` | INTEGER NOT NULL DEFAULT 1 CHECK (`Tagesbedarf_Auto` IN (0,1)) | Umschalter auto/manuell |
| `Tagesbedarf_Manuell_Kwh` | REAL | |
| `Bedarf_Spez`, `Zapftemperatur`, `Kaltwasser_Mittel`, `Kaltwasser_Amplitude` | REAL | Experte; NULL = Vorgabe |
| `Auslastung_01` … `Auslastung_12` | REAL | Experte: Auslastungsgang als Überschreibung der Monatsfaktoren; NULL = Katalog |

**`Tab_TwwWohnungstyp`** — die Wohnungstabelle der DIN-4708-Kennzahl und des Mengengerüsts Wohnen:
`ID` INTEGER PRIMARY KEY AUTOINCREMENT, `ID_Zone` INTEGER NOT NULL REFERENCES `Tab_TwwZone(ID)` ON DELETE CASCADE,
`Anzahl` INTEGER NOT NULL CHECK (`Anzahl` > 0), `Raumzahl` REAL, `Personen` REAL (NULL = Belegung p
nach Raumzahl aus `Tab_TwwDin4708Wert_STAMM`), `ID_Ausstattung` INTEGER NULL REFERENCES
`Tab_TwwDin4708Wert_STAMM(ID)` (Art AUSSTATTUNG; NULL = Vorgabeklasse), `Reihenfolge` INTEGER NOT NULL.
Ist die Tabelle belegt, bildet sie Bezugsmenge (WE bzw. Personen) und `Personen_je_WE` der Zone —
Mengengerüst, DIN 4708 und Gleichzeitigkeit lesen dieselben Zahlen.

**`Tab_TwwProjekt`** — Weiche und gebäudeweite Größen, höchstens eine Zeile je Projekt:

| Spalte | Typ | Bemerkung |
|---|---|---|
| `ID`, `ID_Projekt` | INTEGER PRIMARY KEY AUTOINCREMENT, INTEGER NOT NULL UNIQUE REFERENCES `Tab_Projekt(ID)` ON DELETE CASCADE | |
| `Weg` | TEXT NOT NULL DEFAULT 'BESTAND' CHECK (`Weg` IN ('BESTAND','GENERATOR')) | Muster `Tab_Projekt.Emission_Berechnungsmodus` |
| `Jahresreihe_Stochastisch` | INTEGER NOT NULL DEFAULT 0 CHECK (`Jahresreihe_Stochastisch` IN (0,1)) | Rechenweg der Jahresreihe |
| `Seed`, `Realisierungen` | INTEGER NOT NULL DEFAULT 1 / 10; `Realisierungen` CHECK (≥ 1) | Realisierungen der Jahresreihe |
| `Realisierungen_Auslegung` | INTEGER CHECK (≥ 1) | Realisierungen des Bedarfstags; NULL = Vorgabe nach 4.4 (N2) |
| `Perzentil` | INTEGER NOT NULL DEFAULT 99 CHECK (`Perzentil` IN (95,99)) | Vorgabe nach K3 |
| `Zirk_Auto` | INTEGER NOT NULL DEFAULT 1 CHECK (`Zirk_Auto` IN (0,1)) | |
| `Zirk_Methode` | INTEGER NOT NULL DEFAULT 3 CHECK (`Zirk_Methode` IN (1,2,3)) | 1 Leitungslänge, 2 Anteil, 3 Flächenkennwert (Vorgabe, 4.3) |
| `Zirk_Lage` | INTEGER CHECK (`Zirk_Lage` IN (1,2)) | innerhalb/außerhalb der Hülle; NULL = Vorgabe |
| `Zirk_Laenge_m`, `Zirk_Verlust_W_m`, `Zirk_Anteil`, `Zirk_Kennwert`, `Zirk_Flaeche_m2`, `Zirk_Laufzeit_h`, `Zirk_Manuell_Kw` | REAL | NULL = Vorgabe |
| `Leitungsinhalt_l` | REAL | Großanlagenerkennung (4.7); NULL = aus `Zirk_Laenge_m` und dem Parameter Inhalt je Meter |
| `Lade_Auto` | INTEGER NOT NULL DEFAULT 1 CHECK (`Lade_Auto` IN (0,1)) | nur Auslegung |
| `Ladefenster_h`, `Ladefenster_Beginn_h`, `Lade_Manuell_Kw` | REAL | Länge und Lage des Ladefensters (4.7) |
| `Speicher_C`, `Kaltwasser_Auslegung_C` | REAL | Speichertemperatur; Kaltwasser der Auslegung (NULL = Parameter, 4.0) |
| `Erzeuger_Kw`, `Uebertrager_Kw`, `Uebertrager_UA_W_K`, `Uebertrager_Flaeche_m2` | REAL | Übertrager als Leistung, als U·A oder als Fläche (U aus dem Parameter); NULL = Schätzformel des Verfahrens |
| `Speicherart` | INTEGER NOT NULL DEFAULT 1 CHECK (`Speicherart` IN (1,2)) | 1 Ladespeicher, 2 gemischter Speicher (Summenlinie, 4.5) |
| `Sensorhoehe_Anteil`, `Nachweis_Volumen_l`, `Speicherverlust_W` | REAL | h_sensor/h_sto; Speichervolumen im Nachweis; Bereitschaftsverlust (NULL = 0 mit Hinweis) |
| `Nutzanteil`, `Zuschlag` | REAL | f_nutz, z_S der Vorlage V4 |
| `Bedarfstag_Quelle` | INTEGER CHECK (`Bedarfstag_Quelle` IN (1,2,3,4,5)) | Stundenprofil, A100-Referenz, DIN-4708-Profil, Konstruktor, Ecodesign; NULL = Vorgaberegel (4.5) |
| `ID_Bedarfstag` | INTEGER NULL REFERENCES `Tab_TwwBedarfstag_STAMM(ID)` ON DELETE SET NULL | gewählter Bedarfstag der Quellen 2–5 |
| `Auslegung_Volumen_l`, `Auslegung_Leistung_Kw` | REAL | gewählter Punkt der Summenlinie |
| `Aenderungsdatum` | TEXT | |

Die **Reihe wird nicht gespeichert** (A2): gespeichert sind Zonen, Parameter, Seed und Herkunft; der
Lauf rechnet sie neu. Die Liste der Speicher-Nenninhalte ist keine Tabelle, sondern eine Einstellung
(`IEinstellungen`, Schlüssel `Zapfprofil.Nenninhalte`) mit einer neutralen Vorgabe im Code.

**Später:** `Tab_TwwZapfkategorie_STAMM` (T2, Z3: `ID_Nutzungsart`, `Kategorie`, `Volumenstrom_l_min`,
`Dauer_min`, `Anteil`, `Sigma`, Provenienz, `Status`), `Tab_TwwTyptag_IMPORT` (T3, Z4b: `Klimazone`,
`Gebaeudeart`, `Typtag`, `Aufloesung_min`, Werte als Zeilen, `Quelle`; nie `ReadOnly`, nie in der
Auslieferungsvorlage) und der Feiertags-/Ferienkalender **erst nach Entscheid A6**.

### 3.2 Schemaschritte

Drei Schritte, jeweils der **nächste freie Schritt nach `SchemaStand.Zielversion`**
(`EPOS.Kern/Allgemein/Update/SchemaStand.cs:341`) — beim Beauftragen nachmessen, weil andere Aufträge
(Wirtschaftlichkeit, Gebäudesimulation) dieselbe Zählung benutzen. Papiernamen:

| Papiername | Stufe | Inhalt |
|---|---|---|
| **T1 — Katalog, Zonen, Projekt** | Z0 | die zehn Tabellen aus 3.1; der Katalog der Testdatenbank nur fiktiv (Kapitel 6) |
| **T2 — Zapfkategorien** | Z3 | `Tab_TwwZapfkategorie_STAMM` |
| **T3 — Typtage** | Z4b, nach K3a/K8 | `Tab_TwwTyptag_IMPORT` |

T1 ist im Bestand Schritt 102 (N2 (a)); T2 und T3 bekommen den dann nächsten freien Schritt.

Bauweise nach [`ADR-001`](ADR-001_Schema-Ausrollung.md) und den Regeln in
`WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs` (Kommentar über `SCHRITTE_SQLITE`): erst
Konstante, Methode und Eintrag in `SCHRITTE_SQLITE` (`:4404`, Vorbilder die letzten Einträge der Liste),
**dann** die Zielversion anheben; nur
`SqliteDdl`/`SqliteTabelleVorhanden`. **Die DDL-Texte stehen in `EPOS.Kern/Allgemein/Update/TwwSchema.cs`
nach dem Muster `WechselrichterSchema.cs`** (Katalog plus Projekttabelle mit
`CREATE TABLE IF NOT EXISTS`, `:169-222`); `SchemaKatalog.Schritt99_BhkwWirkungsgradAnteile`
(`SchemaKatalog.cs:3722`) ist ein Satz von Spaltenergänzungen und taugt dafür nicht. Im selben Schritt:

- `EPOS.Kern.Tests/TestDatenbank.cs` und `Werkzeuge/Testdatenbankschema/Program.cs` nachziehen (Wache
  `TestdatenbankSchemastandWacheTests`); die Testdatenbank migrieren, den **fiktiven** Testkatalog
  einspielen, **mit aktivem LFS-Filter** committen und den Schemastand in `Referenzlaeufe/LIESMICH.md`
  nachtragen.
- `KatalogRegistry` um `TWW_NUTZUNGSART` (Verwendungsprüfung gegen `Tab_TwwZone.ID_Nutzungsart`),
  `TWW_TAGESGANGSATZ` und `TWW_BEDARFSTAG` sowie `Katalogfilterprofil.FuerTwwNutzungsart` ergänzen.
- **Unveränderliche Katalogversionen statt Projektkopie.** Die Zone verweist auf die Katalogzeile,
  eine Projektkopie wie `BrauchwasserStammCtrl.CopyFromStamm` oder `Tab_Wechselrichter` gibt es nicht.
  Damit eine Katalogänderung keine Projektergebnisse rückwirkend ändert, ist **eine benutzte Zeile
  unveränderlich**: `TwwNutzungsartCtrl` sperrt Ändern wie Löschen, sobald eine Zone sie benutzt, und
  bietet stattdessen „Speichern unter" (neue Zeile, `ID_Vorlage` → Vorgänger). Eine neue
  Auslieferungsversion kommt als neue Zeilen mit neuer `Katalogversion`; der Dialog bietet je Zone
  „Auf neue Katalogversion umstellen" mit Hinweis. Dasselbe gilt für Tagesgangsätze, Bedarfstage und
  Parameter. Die Katalogversion steht so mittelbar in jedem Projekt fest (Frage ZU12).
- **Die zwei Kopierstellen.** `ProjektDuplizierenCtrl` kopiert die Projekttabellen generisch (Spalte
  `ID_Projekt`, kein `_STAMM`; Zielbestimmung `ErmittleZieltabelle`, `ProjektDuplizierenCtrl.cs:931`;
  `ID_Gebaeude` versetzt die `FK_MAP`, `:109`, der deklarierte Fremdschlüssel hat Vorrang).
  `Tab_TwwWohnungstyp` hängt über `ID_Zone` und muss als abhängige Tabelle mitkommen — Probe über
  `ProjekttransferTests` und `ProjektpflegeTests`. `ProjektExportImportCtrl` (.wpx) findet
  Katalogverweise nur über `KATALOG_SPALTE_ZU_TABELLE` und `KATALOG_NATURALKEY`
  (`EPOS.Kern/Controller/ProjektExportImportCtrl.cs:83`, `:95`); dort kommen `ID_Nutzungsart →
  Tab_TwwNutzungsart_STAMM`, `ID_Tagesgangsatz → Tab_TwwTagesgangsatz_STAMM`, `ID_Bedarfstag →
  Tab_TwwBedarfstag_STAMM` und `ID_Ausstattung → Tab_TwwDin4708Wert_STAMM` mit den natürlichen
  Schlüsseln (`Bezeichner`, `Katalogversion`) bzw. (`Art`, `Schluessel`, `Katalogversion`) hinein. Ob
  der Import eine in der Zieldatenbank fehlende Katalogzeile mitbringt oder ablehnt, ist in Z0
  nachzumessen; ohne Mitnahme wird die Zone benannt abgelehnt, nie still umgehängt. Je Kopierstelle
  ein Test in der Abnahme von Z0. Löschen läuft über `ON DELETE CASCADE`.
- **Auslieferungsvorlage.** `Werkzeuge/Auslieferungsvorlage` behält als Vorgabe jede Katalogzeile
  (`--kataloge alle`, `Argumente.cs:25-31`, `:64`); die ReadOnly-Regel greift nur mit
  `--kataloge readonly`, eine Tabelle ohne Spalte `ReadOnly` bleibt vollständig
  (`Vorlagenbau.cs:120`). Deshalb gilt für die Tww-Kataloge eine **eigene, von `--kataloge`
  unabhängige Regel** (Schritt 3c, `Werkzeuge/Auslieferungsvorlage/TwwKataloge.cs`, N2): bei
  eingeschaltetem Fremdschlüssel (`PRAGMA foreign_keys = ON`) bleibt in allen `Tab_Tww*_STAMM` nur,
  was `Status = 'AUSLIEFERUNG'` trägt und in keiner Provenienzgruppe die Herkunftsart `FIKTIV` oder
  `IMPORT`; Zeilen mit `IMPORT` und `EIGEN` fallen, `Tab_TwwTyptag_IMPORT` wird geleert, und jede
  verbleibende Auslieferungszeile bekommt `ReadOnly = 1`. Danach prüft der Bericht: keine Zeile mit
  Status `IMPORT` (mit Nennung des Beispielpakets, das sie mitgebracht hat), keine mit `EIGEN`, jede
  Auslieferungszeile `ReadOnly = 1`, keine verwaiste Zeile in `Tab_TwwTagesgang_STAMM`,
  `Tab_TwwBedarfstagEreignis_STAMM` und später `Tab_TwwZapfkategorie_STAMM`, keine Herkunftsart
  `FIKTIV` oder `IMPORT`, keine Eingabe des Laufs unter `Referenzlaeufe/Normzahlen/` (ZU11). Die
  Auslieferungswerte selbst kommen aus einem Katalogpaket außerhalb des Repositoriums, das das
  Werkzeug mit `--katalogpaket` einspielt (Kapitel 6 (b)).
- Nach jedem neuen SQL-Text den `SqlDialektPruefer` ziehen.

**iOS.** Die iOS-Schale migriert nicht, sie kopiert eine Seed-Datenbank
(`EPOS.iOS/Datenbankbereitstellung.cs`); neue Tabellen kommen nur mit einem neuen Seed an. Eine ältere
Datenbank ohne sie rechnet den Bestandsweg, der Knopf ist benannt gesperrt (ZU9).

### 3.3 Controller

**`EPOS.Kern/Controller/ZapfprofilCtrl.cs`** (`internal static`, Muster `TypProfilCtrl.cs:55`):

| Methode | Zweck |
|---|---|
| `BrauchwasserWeg Weg(int idProjekt)` | Weiche; `Bestand` ohne Zeile oder ohne Tabelle |
| `IReadOnlyList<Nutzungsart> Katalog()` | Nutzungsarten samt Tagesgangsätzen, eine Abfrage je Tabelle |
| `Parametersatz Parameter()` | gekapselte Parameter der aktuellen Katalogversion |
| `ZapfprofilStand Lies(int idProjekt)` | Weg, Zonen, Wohnungstabelle und Projektgrößen für den Dialog |
| `Zapfprofileingang Eingang(int idProjekt, int wochentagJan1, bool[] we)` | Eingang für den Lauf |
| `void Speichern(int idProjekt, ZapfprofilStand stand, DbVorgang vorgang)` | im übergebenen `DbVorgang`: Zonen und Wohnungstypen löschen und neu schreiben, `Tab_TwwProjekt` per Upsert, **`Weg` immer**, auch ohne Zonenänderung; idempotent — ein zweites OK wiederholt nichts |
| `bool Verfuegbar()` | Tabellen vorhanden (iOS-Seed, 3.2) |

**`EPOS.Kern/Controller/TwwNutzungsartCtrl.cs`** (Muster `BedarfStammCtrl.cs:44`, `TypProfilCtrl.Neu`
mit `TypAnlageErgebnis`): `Neu`, `SpeichernUnter`, `Loeschen` (gesperrt bei `ReadOnly` und bei
Verwendung), `IstReadOnly`, `IstBenutzt`, `TagesgangSpeichern` (Tagesgangsatz und Wochenfaktoren in
einer Transaktion; bei benutztem Satz nur als neue Zeile), `Katalogfilterzeilen`. Alle Zugriffe über
`DataRepository` mit `?`-Parametern.

### 3.4 Einfrierregeln

Solange **kein Referenzprojekt** die Weiche setzt, berührt keine Stufe die Basis; es gibt keine
neue Einfrierregel. Stellt Z5 ein Referenzprojekt auf den Generator (Frage ZU7), kommt im selben
Schritt eine **vierte Einfrierregel** in `Referenzlaeufe/LIESMICH.md` und `CLAUDE.md`: die
`Tab_Tww*`-Zeilen, die ein Referenzprojekt benutzt, sein Seed und seine Realisierungszahl — mit
Neueinfrieren und Begründung. Ersetzt ein Schritt nach K8 die vier Digitalisate des Bestandskatalogs
(1.6), gilt die Einfrierprüfung für 1007, 1045 und 1046.

---

## 4. Rechenwege im Detail

### 4.0 Einheiten und Temperaturen

Energie in kWh (Nutzenergie an der Zapfstelle), Leistung in kW, Stundenwerte in kWh je Stunde,
`c_w` in Wh/(l·K) (physikalische Konstante des Konzepts). **Jede Volumenformel** hat die Form
`V [l] = Q [kWh] · 1000 / (c_w · Δθ)` — auch in 4.5 und 4.7. Die **Bilanzgrenze** der Reihe ist die
Zapfstelle ohne Speicher- und Zirkulationsverluste; die Zirkulation ist eine eigene Teilreihe,
Speicherverluste bleiben beim Speicher. Kulturpinnung in jeder Testklasse (Muster
`BhkwKostenTests.cs:35`).

| Temperatur | Rolle | Herkunft |
|---|---|---|
| `θ_Bezug`, `θ_KW,Bezug` | Temperaturen, auf die sich ein Katalog- oder Importwert bezieht | Katalog (`Bezug_Zapftemperatur`, `Bezug_Kaltwasser`) |
| `θ_Zapf` | Nutzungstemperatur an der Zapfstelle; bestimmt die Nutzenergie aus Volumen | Zone (Experte), Vorgabe `θ_Bezug` |
| `θ̄_KW`, `A`, `m_max` | Kaltwasser-Jahresgang **nur der Bilanz** (K4) | Katalogkonvention, Zone (Experte) |
| `θ_KW,Auslegung` | feste Kaltwassertemperatur **der Auslegung**, unabhängig von K4 | Parameter `A100.Kaltwasser.Auslegung`, Projekt (Experte) |
| `θ_Speicher` | Speicher-Solltemperatur; `Δθ_Speicher = θ_Speicher − θ_KW,Auslegung` für alle Speichervolumina | Projekt, Vorgabe Parameter `W551.Mindesttemperatur` bei Großanlage |
| `θ_Anzeige` | Temperatur der Literanzeige in Kennzahlen | Einstellung, INEKON-Setzung |

### 4.1 S1 — Mengengerüst

```
Q_a,Zone [kWh/a] = n_Bezug · q_spez(Niveau) · 365 · f_θ
  q_spez: Katalogwert des Niveaus; Experte: Bedarf_Spez (Status Ueberschrieben)
  f_θ = (θ_Zapf − θ̄_KW) / (θ_Bezug − θ_KW,Bezug)      Umrechnung auf die Projekttemperaturen,
                                                     nach A1 verpflichtend; f_θ ≠ 1 -> Status Umgerechnet,
                                                     Faktor im Herkunftsprotokoll
  Wohnen, Bezug Fläche: Q = max(a − b · A_WE ; c) · A_WE · n_WE   (Verfahren der DIN V 18599-10,
                        a, b, c Parameter)
  Tagesbedarf manuell: Q_a = Q_d,manuell · 365
Messwert (S6), Reihenfolge: erst Mengengerüst und Zirkulationsanteil (4.3), dann Kalibrieren
  Einheit m³:        Q_Mess [kWh] = V_Mess [m³] · c_w [Wh/(l·K)] · (θ_Zapf − θ̄_KW)   (immer Grenze 1)
  Grenze 1:          f_kal = Q_Mess / Q_a,Katalog ;  Q_a = Q_Mess
  Grenze 2:          f_kal = Q_Mess / (Q_a,Katalog + Q_zirk,Zone) ;  Q_a = f_kal · Q_a,Katalog,
                     Q_zirk,Zone = f_kal · Q_zirk,Zone — die Zirkulation der Zone wird nicht zusätzlich
                     aufgeschlagen
  Grenze 3:          wie 2 mit Q_Mess,netto = Q_Mess − Speicherverlust_Kwh_a (Hinweis)
  Status Kalibriert, Faktor, Quelle und Zeitraum des Messwerts im Herkunftsprotokoll
```

Import- und Katalogwerte mit anderem Temperaturbezug werden **zwingend** über `f_θ` umgerechnet; ein
Wert ohne Bezugstemperaturen wird nicht angenommen. Eingaben: Zone, Nutzungsart, Wohnungstabelle,
Parametersatz. Ausgabe: `JahresenergieKwh`, Herkunft je Feld. Vorgabe: Niveau „mittel". Hinweise
(nicht blockierend): Bedarf außerhalb der Bandbreite des Niveaus; Messwert weicht vom Katalogwert mehr
ab als die Rückfrageschwelle (Konzept 2.2, Parameter). Tests:
`MengengeruestTests.Bezugsmenge_mal_Bedarf_ergibt_die_Jahresmenge`,
`…Der_Messwert_skaliert_mit_ausgewiesenem_Faktor`, `…Ueberschreibung_setzt_den_Status`,
`…Die_Flaechenformel_folgt_dem_Verfahren` (erfundene a, b, c; Knick und Untergrenze),
`…Messwert_mit_Zirkulation_zaehlt_nicht_doppelt` (Grenze 2: Zapfung + Zirkulation = Messwert),
`…Der_Volumenmesswert_wird_ueber_die_Temperaturen_umgerechnet`,
`TemperaturTests.Umrechnung_auf_Bezugstemperatur` (Relation `V_neu = V_Tab · Δθ_Tab / Δθ_neu`, erfundene
Temperaturen). Toleranz exakt bzw. relativ 1e-12.

### 4.2 S2 — Kalender, Jahresgang, Tagesgang

```
Tagtyp(d), d = 1..365:
  Wochentag(d) = (WochentagJan1 + d − 1) mod 7
  Ferienfenster der Zone enthält d            -> Ruhetag
  sonst We[d] und Wochentag = Samstag         -> Samstag
  sonst We[d]                                 -> SonnFeiertag   (Feiertag wie Sonntag)
  sonst                                       -> Werktag
Tagesgewicht nach Tagtyp:
  w_T(d) = w(Wochentag(d))          Werktag
         = w(Samstag)               Samstag
         = w(Sonntag)               SonnFeiertag (auch ein Feiertag am Montag)
         = f_F · w̄,  w̄ = Σ w / 7   Ruhetag; Ferienfaktor NULL -> w_T = w(Sonntag)
Kaltwasser (Bilanz):  θ_KW(m) = Runden₉( θ̄_KW + A · cos(2π · (m − m_max) / 12) ),  m = 1..12
                      f_KW(m) = (θ_Zapf − θ_KW(m)) / (θ_Zapf − θ̄_KW)
Kaltwasser (Auslegung): f_KW,A = (θ_Zapf − θ_KW,Auslegung) / (θ_Zapf − θ̄_KW), für alle Tage gleich
Gewicht:     g(d) = f_Monat(m(d)) · f_KW(m(d)) · 7 · w_T(d)
Tagesmenge:  Q_d = Q_a · g(d) / Σ_d g(d)            -> Σ_d Q_d = Q_a
Stundenwert: q_h = Q_d · φ_Tagtyp(d)(h),  Σ_h φ = 1 -> Σ_h q_h = Q_a
```

`Runden₉` ist `Math.Round(x, 9)`: Die zwölf Monatswerte entstehen einmal je Lauf und sind danach
Zahlen ohne Abhängigkeit von der Mathematik-Bibliothek der Plattform (4.4). Der Faktor der Monate
`f_Monat` ist bei gesetztem Auslastungsgang der Zone dessen Wert, sonst der Katalogwert.

**Ferienfenster.** Die Fenster kommen aus der Zone, bei gebundener Zone vorbelegt aus den vier
Ferienzeiten des Gebäudes (`Tab_Gebaeude.Ferienbeginn_1` … `Ferienende_4`, Rechnung in
`EPOS.Kern/Allgemein/Ferienzeit.cs`). Jahrestage werden **unverändert übernommen**; 0 und 366 gelten
als „keine Angabe" (Bestandskonvention, `Ferienzeit.cs:22-25`: ein Winterferienbeginn 0 wird beim
Speichern auf 366 gehoben), Werte über 365 werden auf 365 gekappt. Beginn leer und Ende gesetzt ergibt
das Fenster 1 … Ende (Winterferien über den Jahreswechsel), Beginn > Ende das Fenster Beginn … 365 und
1 … Ende, Ende leer kein Fenster. Die Schaltjahrverschiebung um höchstens einen Tag ist Bestand —
`Ferienzeit.Jahrestag` rechnet im laufenden Jahr, gespeichert wird nur die Zahl (`:16-20`, `:44-47`,
Regel F3) — und wird nicht korrigiert. Bundesländer und Schulferien gibt es erst mit A6. Vorgaben:
Kaltwasser θ̄ und Amplitude als Konvention im Katalog, fester Jahresgang (K4).

Tests: `ZapfkalenderTests` (Wochentag, `…Ein_Feiertag_am_Montag_erhaelt_die_Sonntagsmenge`,
`…Jahrestag_366_ist_keine_Angabe`, Ferienfenster über den Jahreswechsel, Ruhetag mit und ohne
Ferienfaktor), `KaltwassergangTests.Das_Jahresmittel_des_Faktors_ist_eins`,
`…Die_Monatswerte_sind_gerundet`, `FormvektorTests` (Summen, Wochenfaktor Null, Tagesgang Null ergibt 0
mit Hinweis, `…Die_Wochenreihe_entsteht_ohne_Stundenreihe`). Toleranz relativ 1e-12.

**Wochenreihe der Auslegung.** `Formvektor.Wochenreihe` bildet die 168 Stunden der maßgebenden Woche
aus `Tagesmengen` (mit `f_KW,A` statt `f_KW`) mal Tagesgang — über dieselben Bausteine, aber **ohne**
`Stundenreihe` und ohne `ZapfprofilRechner`. Maßgebend ist die Woche mit der größten Summe der
Tagesmengen (4.7).

**VDI-4655-Typtage (Z4b).** Der Import legt die Typtage im eigenen Modell `Tab_TwwTyptag_IMPORT` ab;
die Klasse `Typtagzuordnung` ordnet jedem Kalendertag einen Typtag zu (Jahreszeit aus der
Tagesmitteltemperatur, Wochentag, Bedeckung) und setzt die Tagesmenge nach der Methodik der
Richtlinie: `Q_TT = Q_a · (1/365 + N_Pers · F_TT)` (N_Pers Personen bzw. WE der Zone), mit Klemmung auf
`Q_TT ≥ 0` und Hinweis; `F_TT` stammt aus den eingespielten Daten, nie aus dem Produkt. Vorfragen: `Tab_Solar.Bedeckungsgrad` ist in
der Testdatenbank überall leer, `Tab_Klimadaten.TagTyp_W` nur eine Näherung heiter/bewölkt
(`KlimaImportAblauf.cs:982-995`), `Tab_Klimaregion` ohne TRY-Zone. Die Wetterkopplung ist deshalb ein
eigener Unterpunkt Z4b mit Vorbedingung K3a/K8 (Kapitel 7).

### 4.3 S5 — Zirkulation

```
Zonen mit Zirkulation:  Z1 = Zonen mit Katalog-Bilanzgrenze 1 und Zirkulation = 1
Anteil:                 α = Σ_{z∈Z1} Q_a,z / Σ_z Q_a,z   (mengengewichtet; flächengewichtet,
                        wenn jede Zone eine Fläche trägt)
Methode Leitungslänge:   P_zirk = α · L · q' / 1000                          [kW]
Methode Anteil:          P_zirk = a · Q̄_d,Z1 / t_Lauf,  Q̄_d,Z1 = Σ_{z∈Z1} Q_a,z / 365   [kW]
Methode Flächenkennwert: P_zirk = α · k_A(Lage) · A_N / (365 · t_Lauf)      [kW]  (Verfahren nach
                                                                             DIN V 4701-10)
manuell:                 P_zirk = Zirk_Manuell_Kw   (gebäudeweit, ohne α)
Reihe:                   q_zirk,h = P_zirk in den Laufzeitstunden des Tages, sonst 0; t_Lauf ≤ 24 h
Jahresverlust:           Q_zirk = P_zirk · t_Lauf · 365 = Σ_h q_zirk,h
Anteil der Zone z ∈ Z1:  Q_zirk,z = Q_zirk · Q_a,z / Σ_{Z1} Q_a      (für die Kalibrierung, 4.1)
```

Die Laufzeitstunden liegen zusammenhängend um die Tagesmitte der Zapfung; `t_Lauf` folgt dem Rahmen
nach DVGW W 551 (Parameter). **Vorgabe** ist die Methode Flächenkennwert: `A_N` aus `Zirk_Flaeche_m2`,
sonst aus Wohnfläche je WE × WE der Wohnzonen oder aus dem gebundenen Gebäude (A8); Kennwert `k_A` und
Lage sind Parameter. Fehlt jede Fläche, fällt die Vorgabe auf die Methode Anteil mit dem Parameter
`a` zurück, mit Hinweis. So liefert schon die Stufe Einfach eine Zirkulation (Lehre 3).

**Bilanzgrenze der Quelle.** Eine Zone mit Katalog-Grenze 2 (Kennwert schließt Verteil- und
Zirkulationsverlust ein, etwa VDI-4655-Daten) oder 3 (schließt zusätzlich den Speicherverlust ein)
gehört nicht zu Z1: Sie trägt keinen Zirkulationsanteil, der Speicherverlust wird nicht zusätzlich
angesetzt, und ein Hinweis nennt die Zone — keine Doppelzählung. Tragen alle Zonen Grenze 2 oder 3,
ist α = 0. Zirkulation „nein" bei erkannter Großanlage erzeugt einen Hinweis. `Q̄_d` meint immer den
Zapfbedarf der Zonen in Z1.

Tests: `ZirkulationskanalTests.Die_Reihe_erhaelt_den_Jahresverlust_jeder_Methode`, drei Methoden
gegeneinander bei gleichem Jahresverlust (die Eingaben so gewählt, dass die Jahreswerte gleich sind),
Laufzeitfenster, `…Grenze_zwei_und_drei_tragen_keinen_Anteil`, `…Einfach_liefert_eine_Zirkulation_ungleich_null`
(erfundene Parameter), `…Kennwertreproduktion` (die Methode Flächenkennwert gibt `k_A · A_N` als
Jahresverlust exakt zurück).

### 4.4 S3 — Zapfereignisse und Ensemble

```
je Einheit i = 1..n_E, je Tag d, je Kategorie k:
  Zahl der Ereignisse   Bernoulli je Minute t mit p_k(t) = λ_k(d) · Dichte(t)   (Poisson-Näherung)
                        λ_k so, dass Σ_k λ_k · E_k = Q_d,Zone / n_E
  Erwartete Energie     E_k = Dauer_k · c_w · (θ_Zapf − θ_KW(m)) / 1000 · E[max(0, μ_k + σ_k · z)]
                        E[max(0, μ + σz)] = μ · F(μ/σ) + σ · f(μ/σ)   (F, f: Verteilungs- und Dichte-
                        funktion der Standardnormalverteilung; einmal je Kategorie gerechnet, Runden₉)
  Zeitpunkt             Dichte(t) ∝ φ_Tagtyp(d)(h(t))
  Volumenstrom          V̇ = max(0, μ_k + σ_k · z),  z = Σ_{j=1..12} u_j − 6
  Energie               E = V̇ · Dauer_k · c_w · (θ_Zapf − θ_KW(m)) / 1000
q_min(t) = Σ_i Σ_Ereignisse E / Dauer (auf die Minuten des Ereignisses verteilt)
q_h      = Σ_{t ∈ h} q_min(t)
```

Kategorien und Parameter nach der frei dokumentierten Jordan/Vajen-Parametrik (IEA SHC Task 26), für
Nichtwohnen zwei Kategorien nach dem OpenDHW-Muster; gekennzeichnet als Modellannahme bis Z5. Im
Experten-Modus sind Kategorien und σ als Katalogkopie bearbeitbar (Status EIGEN). Superposition
unabhängiger Einheiten lässt die Gleichzeitigkeit **entstehen** — es gibt keinen Eingabefaktor.
Urlaube werden je Einheit versetzt gezogen (Entkopplung). Weil `z` aus zwölf Gleichverteilten nur
näherungsweise normal ist, prüft der Generatortest das empirische Mittel von `max(0, μ + σz)` gegen
`E_k` (±1 %).

**Portabler Zufall (`ZapfZufall.cs`).** Ganzzahliger Generator (SplitMix64 zum Säen, xoshiro256** zum
Ziehen); Realisierung r bekommt den Seed `SplitMix64(Seed ⊕ r)`, unabhängig von der Thread-Reihenfolge.
Ereignisse als Bernoulli-Ziehung je Minute, Normalwerte als Summe von zwölf Gleichverteilten minus 6.
Transzendente Funktionen kommen nur in Werten vor, die einmal je Lauf entstehen und auf neun Stellen
gerundet werden (Kaltwassergang 4.2, `E_k`); in der Ziehung selbst gibt es keine
`Math.Log`/`Math.Exp`/`Math.Cos`. Das Ensemble läuft über `Kulturweitergabe.For`
(`SpeicherEngine/Kulturweitergabe.cs:101`, vom Kern referenziert) und summiert danach in fester Folge.

**Zwei Ensembles.** Die **Jahresreihe** („stochastisch", Bilanz) zieht `Realisierungen` Jahre. Die
**Auslegung** zieht nur den Bedarfstag, dafür `Realisierungen_Auslegung` Mal: Ein empirisches
p-Perzentil braucht mindestens `1/(1 − p)` Stichproben; die Vorgabe von `Realisierungen_Auslegung`
liegt deshalb bei einem Vielfachen davon (INEKON-Setzung in Z3, auf dem iPad kleiner, A11). Liegt die
Zahl darunter, trägt das Perzentil den Vermerk **„nicht belastbar"**.

**Gleichzeitigkeit je Topologie.** Das Ensemble weist sie als Ergebnis aus, aber größengleich zur
Auslegungsgröße der Topologie: bei Speicher `GLF_V = P_p(V_Summe) / Σ_i P_p(V_i)` über das
erforderliche Volumen, bei Durchfluss, Frischwasser- und Wohnungsstation `GLF_P = P_p(P_Summe) /
Σ_i P_p(P_i)` über die Minutenspitze. Ein GLF nur als f(N) gibt es nicht (Lehre 2).

Ausgaben: Minutenreihe je Zone für die Auslegung (nie in die Bilanz), Stundenreihe zum Seed (nur bei
„stochastisch"), P50/P90/P95/P99 mit Streuband, Gleichzeitigkeit je Topologie, Konsistenzprobe.
Vorgaben: Seed 1, zehn Realisierungen der Jahresreihe. Tests:
`ZapfZufallTests.Die_Folge_ist_auf_jeder_Plattform_dieselbe`, `ZapfereignisgeneratorTests` (Momente je
Kategorie ±5 %, gestutzter Mittelwert ±1 %), `ZapfensembleTests.Der_Erwartungswert_trifft_den_deterministischen_Pfad`
(feste Einheitenzahl und Realisierungszahl; Toleranz `max(1 %, 3 · s_R / √R)` mit der empirischen
Standardabweichung `s_R` der Jahresenergie über die R Realisierungen, damit die Probe bei kleinem n_E
nicht zufällig rot wird), Reihenfolgeunabhängigkeit, `…Die_Spitze_je_Einheit_faellt_mit_Wurzel_N`
(Relation: die Spitze je Einheit fällt mit wachsendem N, das Verhältnis liegt im Korridor um 1/√N;
erfundene Kategorien), `…Die_Gleichzeitigkeit_haengt_von_der_Topologie_ab` (Relation: GLF_V > GLF_P
bei demselben Ensemble); lokal gegen die DHWcalc-Referenzdateien aus OpenDHW (MIT, nur Testumfang,
Attribution im Testordner).

### 4.5 S4 — Die Dreiergruppe der Auslegung

**Topologie.** Die Auslegung rechnet je **Topologiegruppe** (Zonen gleicher Topologie). Bei
**Speicher** ist die Auslegungsgröße der Punkt der Summenlinie (V, Φ_N); bei **Durchfluss,
Frischwasser- und Wohnungsstation** ist sie die Minutenspitze der superponierten Last [kW] (bei der
Wohnungsstation je Einheit und für die Summe). Das Ergebnis trägt die Topologie. DIN 4708 und das
GLF-Verfahren der Vorlage sind nur bei Speicher gültig, sonst Gültigkeitshinweis.

**Bedarfstag (`Bedarfstag.cs`).** 1440 Minutenwerte in kWh, aus Mengengerüst und Formvektor, nie aus
der Jahresreihe; Energie bei `θ_KW,Auslegung` (Faktor `f_KW,A`, 4.2), nicht beim winterlichen
Kaltwasser des Kalendertags. Quellen: (1) Stundenprofil der Zonen am Tag des größten Tagesbedarfs,
gleichmäßig auf Minuten expandiert — **nur mit Warnbanner „Spitzen unterschätzt"** (VDI-6002-Warnung
zu Einzeltagesspitzen) und **nie als Empfehlung ohne Rückfrage**; (2) A100-Referenzprofil aus dem
Katalog, erst nach K1/K8; (3) DIN-4708-Profil, nur Wohnen, ebenfalls ein Normdatensatz und damit
K1/K8-pflichtig; (4) manuell konstruiert nach dem Verfahren der A100 (Konstruktor, Z2, Ablage als
Katalogeintrag Status EIGEN); (5) Ecodesign-Zapfprofil, nur Einfamilienhaus, zur Plausibilisierung.
**Vorgaberegel:** Wohnen → (3), sobald nach K1/K8 zulässig, sonst (4); Nichtwohnen → (4); ohne
konstruierten Tag öffnet die Auslegung den Konstruktor statt still (1) zu nehmen.

**(a) Summenlinie (`Summenlinie.cs`)** nach DIN EN 12831-3 mit den Rechenregeln des Entwurfs A100/A1
(Formeln nach Grundlagen 3, Koeffizienten als Parameter):

```
Φ_Ü     = U·A · Δθ_Ü / 1000; U·A aus Uebertrager_UA_W_K, sonst U (Parameter) · A_HE,
          A_HE eingegeben oder aus der Schätzformel des Verfahrens (Plausibilitätswächter)  [kW]
Φ_N     = min(Φ_Erzeuger, Φ_Ü)                                                           [kW]
Q_sto,max = V · c_w · Δθ_Speicher · f_l / 1000       f_l Ladungsfaktor (Parameter)       [kWh]
Q_sto,on  = Q_sto,max · (1 − h_sensor/h_sto)         Einschaltpunkt
Q_sto,min = 0 (Ladespeicher);  gemischter Speicher nach dem Verfahren mit dem Term
            (1 − h_sensor/(2 · h_sto)) (Parameter)
Verluste: Φ_V(i) = Φ_Speicher + Φ_zirk(i),  Φ_zirk(i) = P_zirk in den Laufzeitminuten (4.3), sonst 0
Start:    Q_sto(0) = Q_sto,max, Erzeuger aus
Minute i = 0..1439, Δt = 1/60 h:
  Erzeuger ein, sobald Q_sto(i) ≤ Q_sto,on (t_on = i); aus, sobald Q_sto(i) ≥ Q_sto,max
  Φ_eff(i) = Φ_N − Φ_V(i)   wenn ein und (i − t_on) ≥ t_lag;   sonst −Φ_V(i)   (darf negativ sein)
  Q_sto(i+1) = min(Q_sto,max, Q_sto(i) − q_min(i) + Φ_eff(i) · Δt)                    (NA.3)
Nachweis(V, Φ_N):  Q_sto(i) − q_min(i) ≥ Q_sto,min für alle i
τ = m · c_w / (U·A) · k_τ   nur informativ (Anzeige, Dimensionsprobe); k_τ Parameter
Wertepaarkurve:    für Φ_N auf einem Raster das kleinste V mit Nachweis
Ladezeit je Tag:   Σ t_power,on = Minuten mit Erzeuger ein / 60   [h/d]
```

**Bisektion mit Monotonieprüfung.** Vor der Bisektion prüft ein grobes Raster über V, dass der
Nachweis monoton ist (bei geschätzter Übertragerfläche nicht gesichert). Bei Verstoß rechnet ein
feiner Rasterlauf und nimmt das kleinste V mit Nachweis, mit Hinweis. Die Wertepaarkurve ist eine
eigene Erweiterung des Nachweisverfahrens und als solche beschriftet. Schnellpfad für Wohnen bis zur
**Anwendungsgrenze des Vereinfachungsverfahrens der A100** (Parameter) mit dessen Setzungen
(Parameter). Plausibilitätswächter: die Schätzformel der Übertragerfläche wird für kleine Speicher
unplausibel — dann Hinweis statt negativer Fläche. Ergebnis trägt den Vermerk „Entwurfsstand,
Anwendung besonders zu vereinbaren".

**(b) Perzentil.** Aus dem Auslegungsensemble (4.4), je Realisierung r des Bedarfstags:
bei **Speicher** das erforderliche Volumen `V_r` aus der Summenlinie beim gewählten `Φ_N` [l]; bei
**Durchfluss, Frischwasser- und Wohnungsstation** die größte Minutenleistung `P_r` [kW] (dazu die
größte Stundenleistung nachrichtlich). Das Perzentil `P_p` (p = 95 oder 99) läuft über die R
Realisierungen; das Streuband ist die Spannweite min–max über die Realisierungen. Leer bis
„Stochastisch rechnen"; bei zu kleinem R „nicht belastbar" (4.4). Dazu der Vergleich gegen
`μ + z · σ/√N` aus der Einzelstatistik der Einheiten als Hinweis (Konzept S4c).

**(c) Normvergleich (`Din4708Kennzahl.cs`).** Nur Zonen mit Nutzungsart Wohnen und Topologie Speicher,
sonst ausdrücklich „außerhalb des Gültigkeitsbereichs". Die Kennzahl liest die Wohnungstabelle
(`Tab_TwwWohnungstyp`) und die Katalogwerte (`Tab_TwwDin4708Wert_STAMM`); ohne Wohnungstabelle ist
sie „nicht rechenbar", nie geschätzt:

```
N    = Σ_Wohnungstypen (n · p · Σ v·w_v) / (p_b · w_b)     p, Σ v·w_v: Katalogwerte, nie abgedruckt
u_i  = a_i · z · (1 + √N) / √N,   i = 1, 2
K(u) = erf(u) bis zur Kappung der Norm
W_z  = W_b · [ N · K(u_1) + √N · K(u_2) ]                   [kWh]
V_DIN = W_z · 1000 / (c_w · Δθ_Speicher) / f_nutz           [l]   (ohne Zuschlag)
```

`a_i`, `z`, `p_b`, `w_b`, `W_b` und die Kappung sind Parameter aus `Tab_TwwParameter_STAMM`, nie
Konstanten der Klasse. Dazu der Hinweis, dass die Kennzahl für Vorlauftemperaturen einer Wärmepumpe
kaum aussagefähig ist, und nachrichtlich der Rohrnetz-Spitzendurchfluss nach dem Verfahren der
DIN 1988-300 (Parameter gekapselt).

**Die Empfehlung.** Die drei Werte stehen nebeneinander, nie zu einer Zahl gemischt. **Empfohlen wird
genau ein Punkt je Topologiegruppe:** bei Speicher der gewählte Punkt der Summenlinie, dazu der
nächste Nenninhalt der Liste ≥ V als Anzeige; bei den anderen Topologien die Minutenspitze des
Bedarfstags. In der Stufe Einfach trägt der Punkt den Vermerk **„Schnellauslegung"**. Der
Verfahrensvergleich (4.7) ist nachrichtlich.

**Plausibilitätsprüfung, größengleich.** Speicher, in Litern beim selben Φ_N: erwartet
`V_Perzentil ≤ V_Summenlinie < V_DIN`; Durchfluss, in kW: erwartet `P_Perzentil ≤ P_Bedarfstag`,
der DIN-1988-300-Wert nachrichtlich. Liegt die stochastische Spitze deutlich über der Leistung des
Summenlinienpunkts, folgt der Konsistenzhinweis des Konzepts 2.5 (Schwelle als Parameter). Eine
Abweichung ist ein Hinweis, kein Fehler.

Tests: `SummenlinieTests` (Handrechnung eines konstruierten Tags, Φ_N = min, Φ_eff negativ in
Ladepausen, Einschaltpunkt, Zirkulation als Minutenlast, Bisektion trifft den Nachweis,
`…Die_Monotoniepruefung_faellt_auf_den_Rasterlauf_zurueck`, `…Die_Zeitkonstante_hat_die_Dimension_Minuten`,
`…Schnellpfad_und_Vollverfahren_stimmen_im_Gueltigkeitsbereich`), `Din4708KennzahlTests` (Formel gegen
eine Handrechnung mit **erfundenen** Parametern und einer erfundenen Wohnungstabelle, Gültigkeit je Zone
und Topologie, `…Ohne_Wohnungstabelle_nicht_rechenbar`), `AuslegungsergebnisTests.Es_gibt_genau_eine_Empfehlung`,
`…Die_Reihenfolge_wird_groessengleich_geprueft`, `…Perzentil_bei_kleinem_R_nicht_belastbar`,
`ZapfensembleTests.Die_Gleichzeitigkeit_haengt_von_der_Topologie_ab`, `EcodesignTests.Die_Tagessummen_sind_exakt`
(freie Daten). Toleranz relativ 1e-9.

### 4.6 Kennzahlen der Bilanz

`ZapfprofilErgebnis` weist aus: Jahresbedarf Zapfung und Jahresverlust Zirkulation (kWh/a, Liter als
Anzeige bei `θ_Anzeige`), Zirkulationsanteil, Tagesmittel, spezifischer Wert je Zone, Volllaststunden,
größter Stundenwert mit dem Vermerk **„Bilanzwert, keine Auslegungsgröße"**, Stunden über einer
wählbaren Schwelle. Keine dieser Zahlen geht in die Auslegung.

### 4.7 Speicherauslegung nach der Vorlage V4

Klasse `TwwSpeicherauslegung.cs`, Eingang `Speicherauslegungseingang` (Personen und Wohnungstabelle
aus demselben Mengengerüst, N aus 4.5, Ladeleistung und Zirkulation als `Schaetzwert`, `f_nutz`,
Zuschlag `z_S`, `Δθ_Speicher` nach 4.0). Nur für Topologie Speicher. Muster für jede
auto/manuell-Größe:

```csharp
internal readonly record struct Schaetzwert(bool Auto, double Vorschlag, double? Manuell)
{
    internal double Angesetzt => Auto || Manuell is null ? Vorschlag : Manuell.Value;
}
```

Die Rechenwege, jeweils mit einem Rechenweg-Satz als Text im Ergebnis:

```
Ladefenster:              Länge t_F [h], Beginn t_B [h] (Parameter der Eingabe)
                          P_lade(t) = P_lade in den Fensterstunden, sonst 0
Ladeleistung, Vorschlag:  P_lade = (Q_d,max,Zapfung + P_zirk · t_Lauf) / t_F
Woche:                    Wochenreihe der maßgebenden Woche (4.2), zweimal hintereinander
Lindley (profilbasiert):  D(t) = max(0, D(t−1) + Z(t) + C(t) − P_lade(t) · 1 h),  D(0) = 0,  t = 1..336
                          C(t) = P_zirk · 1 h in den Laufzeitstunden, sonst 0
                          D_max = max über Woche 2 (t = 169..336), Zeitpunkt in Woche 2 gezählt
                          V_profil = D_max · 1000 / (c_w · Δθ_Speicher) / f_nutz · (1 + z_S)
                          Vermerk: Stundenbilanz — Zapfspitzen unter einer Stunde deckt DIN 4708
                          D_max = 0: Anzeige „–" und Satz statt 0 l (Logik an der Zahl, nie am Text)
DIN 4708:                 V_DIN aus 4.5
Gleichzeitigkeit (GLF):   Definition der Vorlage (Blatt „Berechnung", lpagg calc_GLF):
                            GLF(N) = W_z(1) / W_z(N),   W_z nach 4.5,   GLF(1) = 1
                          Einzelbedarf je Person der Einheitswohnung: W_z(1) / p_b
                          V_GLF = P · W_z(1) / p_b · 1000 / (c_w · Δθ_Speicher) / f_nutz · GLF(N) · (1 + z_S)
klassisch (nachrichtlich): V_klass = P · v_klass · Δθ_ref / Δθ_Speicher · (1 + z_S)  (v_klass, Δθ_ref Parameter)
Ergebnis des Verfahrensvergleichs (nachrichtlich):
                          Band [V_min ; V_max] über die gültigen Verfahren (profilbasiert, DIN 4708, GLF);
                          Hinweis, wenn der empfohlene Summenlinienpunkt (4.5) außerhalb des Bands liegt;
                          Nenninhalt = kleinster Listenwert ≥ V_max, über dem Listenende gerundet auf das
                          Raster der Vorlage (INEKON-Setzung aus V4, Parameter), Hinweis
                          „Mehrspeicheranlage prüfen"; Kriterium: Speicher mit N_L ≥ N (kein Produktwert)
Füllstand:                C_sp = V · f_nutz · c_w · Δθ_Speicher / 1000;  SOC(t) = max(0, C_sp − D(t));
                          Reserve = min SOC / C_sp
Plausibilität Ladung:     P_lade · t_F ≥ Q_d,Zapfung + P_zirk · t_Lauf, sonst Mindestleistung nennen
```

**Zum GLF-Verfahren.** Der Faktor ist die Definition der Vorlage, keine DIN-4708-Größe, sondern aus
ihr abgeleitet; die Formel ist gegen die Vorlage (Blatt „Berechnung") nachgemessen. Mit
`P ≈ N · p_b` wird `V_GLF ∝ N · W_z(1)² / W_z(N) = W_z(1)² / (W_b · [K(u_1) + K(u_2)/√N])`: V_GLF
steigt mit N monoton, strebt aber gegen eine Schranke — für große N hängt es kaum noch von der
Gebäudegröße ab. Die übliche Form `W_z(N) / (N · W_z(1))` ergäbe `V_GLF = V_DIN · P / (N · p_b)` und
damit kein eigenes Verfahren. Folge: Das GLF-Verfahren ist nur Teil des Plausibilitätsbands, gilt
bis zu einer Obergrenze N_GLF (INEKON-Setzung, Parameter, in Z2 an Vorlage und Summenlinie
festzulegen) und trägt darüber einen Gültigkeitshinweis; ohne Wannen ist es eingeschränkt.

**Großanlage (`Grossanlage.cs`).** Erkennung aus Speichervolumen (`Nachweis_Volumen_l`, sonst gewählter
Auslegungspunkt) und Leitungsinhalt (`Leitungsinhalt_l`, sonst `Zirk_Laenge_m` × Parameter Inhalt je
Meter) gegen die Schwellen nach DVGW W 551 als Parameter — keine Schwelle im Text oder Code. Die
Erkennung steuert die Vorgaben (Mindesttemperatur, Zirkulation ja) und Hinweise.

Die Warnliste (`Auslegungshinweis`, Klartext mit eingesetzten Zahlen, nie blockierend):
Summenkontrollen, Ladeleistung × Fenster gegen größten Tagesbedarf, maßgebender Tag am Wochenende,
Großanlage nach DVGW W 551, Speichertemperatur unter der Mindesttemperatur nach DVGW W 551, klassischer
Faustwert weit über dem Band, Summenlinienpunkt außerhalb des Bands, stochastische Spitze deutlich über
der Summenlinienleistung (Konsistenzhinweis, Schwelle als Parameter), **Gültigkeit je Verfahren** (DIN
4708 und GLF nur Wohnen mit Speicher; GLF über N_GLF und ohne Wannen eingeschränkt). OK übernimmt den
Punkt der Summenlinie; der Verfahrensvergleich ist nachrichtlich, kein Schreibvorgang. Tests:
`SpeicherauslegungTests` mit einem fiktiven, von Hand nachgerechneten Referenzfall („Wohnhaus 1, 20 WE
× 2 P", erfundene Parameter), `…Dmax_null_zeigt_den_Strich`, `…Der_Zeitpunkt_liegt_in_Woche_zwei`,
`…Ein_Mengengeruest_fuer_alle_Verfahren`, `…Der_manuelle_Wert_wirkt_nur_bei_manuell`,
`…Das_Ladefenster_begrenzt_die_Nachladung`, `…Die_Einheiten_stimmen_mit_der_Anzeigeformel`,
`…GLF_ist_eins_bei_einer_Einheit_und_V_steigt_mit_N`, `GrossanlageTests` (Schwellen aus dem
Parametersatz); ein Vergleich mit der Mappe bleibt lokal.

### 4.8 S6 — Kalibrierung

`Mengengeruest.Kalibrieren` (4.1) wirkt nur auf das Mengengerüst und beachtet die Bilanzgrenze des
Messwerts. Mit Z5: gemessene Reihe als CSV über `IDateiDienst` und `Stream`, Dauerlinienvergleich
(P50/P90/P99, Spitze), Tagesgangform, Bericht „synthetisch gegen gemessen"; jede Kalibrierung im
Herkunftsprotokoll mit Quelle und Zeitraum, keine Objektdaten im Repository (K5).

---

## 5. Oberfläche

### 5.1 Der Dialog `ZapfprofilDialog.razor`

Ort `EPOS.UI/Dialoge/Bedarf/`, **vier Teile nach `EPOS.UI/CLAUDE.md`**:

| Teil | Datei | Inhalt |
|---|---|---|
| Daten | `EPOS.UI/Dialoge/Bedarf/ZapfprofilDaten.cs` | DTO ohne Fachklasse des Kerns: `ZapfprofilZoneDaten`, `ZapfprofilEingabeDaten` (Arbeitsstand als DTO), `ZapfprofilVorschauDaten` (Reihen und Kennzahlen zum Zeichnen), `ZapfprofilAuslegungDaten`, `ZapfprofilErgebnisDaten` (Rückgabe an den Bedarfsprofil-Dialog, samt Weg) |
| Texte | `ZapfprofilTexte.cs` | Textbündel, ab zehn Texten Pflicht |
| Dialog | `ZapfprofilDialog.razor`, `TagesgangEditor.razor`, `ZapfprofilAuslegungDialog.razor`, `BedarfstagKonstruktor.razor` | `EventCallback<ZapfprofilErgebnisDaten?> Geschlossen`, `null` bei Abbruch |
| Hülle | `EPOS.UI.Daten/Bedarf/ZapfprofilHuelle.cs` (5.5) | bildet `ZapfprofilStand` (Kern) und die DTO aufeinander ab, reicht Delegaten mit DTO-Signaturen, etwa `Func<ZapfprofilEingabeDaten, ZapfprofilVorschauDaten>` |

Dazu `ZapfprofilKiSicht.cs` (Muster `BedarfsProfileKiSicht.cs`, Eintrag in `KiMaskennamen` und
`KiMaskenziele`; zeigt Quelle und Ausgabe, nie `Beleg`). Der Dialog ruft **keinen** Kern-Typ: Die
Vorschau kommt über den Delegaten der Hülle, der `ZapfprofilRechner.Rechnen` ruft — denselben Aufruf
wie der Lauf. Nur Hausbausteine: `Formularraster`, `Zahlenfeld`, `Auswahlfeld`, `Optionsgruppe`,
`Reiter`, `DiagrammSvg`, `Ueberlagerung`, `Raster`, `SpeichernLeiste` mit Aktionsschlitz,
`Warnbanner`, `Rueckfrage`.

Aufbau nach dem Mockup:

- **Kopf**: Titel, zwei `InfoKnopf` (Bedienung, Rechenweg), Schließkreuz; eingebettet nur der Titel der
  Überlagerung („ein Titel, eine Stelle"). **Kontextzeile**: Projekt, Klimaregion, Kalender,
  Bilanzgrenze; rechts der Stufenumschalter **Einfach · Erweitert · Experte** und der Zähler „n Werte
  überschrieben" — die Stufe blendet nur ein und aus, Überschreibungen bleiben.
- **Links**: Zonenliste (`Raster`, Spalten Zone, Nutzungsart, Bezugsmenge, Topologie, MWh/a,
  Summenzeile) mit eigener Listenleiste Zone hinzufügen… · Duplizieren · Entfernen; darunter der
  Eingabeblock der gewählten Zone in Gruppen je Stufe, bei Wohnen ab Erweitert die Wohnungstabelle.
- **Rechts**: Vorschau, live über den deterministischen Pfad, Kurzkennzahlen und fünf Reiter
  **Tagesgang · Wochenprofil · Jahresgang · Dauerlinie · Kennzahlen**; Auswahl „Anzeigen für" (Zone
  oder Summe).
- **Fuß**: `SpeichernLeiste` mit Aktionen **Stochastisch rechnen · Auslegung…**, Status als Füller,
  Abbrechen, OK; „Stochastisch rechnen" nebenläufig mit Fortschritt, in jeder Stufe frei. Esc schließt
  nur, wenn keine Überlagerung offen ist; Enter bleibt unbelegt.

**Überlagerung „Tagesgang bearbeiten"** (`TagesgangEditor.razor`, Muster `TypProfilDialog.razor`):
Tagesgangsatz mit Herkunftsmarke, Tagtyp als `Optionsgruppe` (vier Tagtypen), 24 `Zahlenfeld` in
Prozent, Balkengrafik nur zur Anzeige, Wochenfaktoren Mo–So, zwei Summenkontrollen im Klartext
(grün „summiert zu 100 %", rot „Summe … — korrigieren", nicht blockierend), Quelle je Tagtyp. Fuß: Tag
kopieren · Tag einfügen · Als Benutzerdefiniert kopieren · Zurücksetzen auf Vorgabe · Füller ·
Abbrechen · OK. Auslieferungssätze und benutzte Sätze nur als Kopie; OK schreibt über
`TwwNutzungsartCtrl.TagesgangSpeichern` **eine** Transaktion — der Editor gehört zum Katalog, nicht
zum Arbeitsstand des Projekts.

**Überlagerung „Auslegung"** (`ZapfprofilAuslegungDialog.razor`): oben die Eingaben der Summenlinie
(Bedarfstag mit Quelle und Vermerk, „Bedarfstag konstruieren…" als weitere Überlagerung, Speicherart,
Übertrager, Sensorhöhe), darunter je Topologiegruppe die drei Karten der Dreiergruppe nebeneinander
(4.5) und **der eine empfohlene Punkt**; getrennt davon der Verfahrensvergleich der Speicherauslegung
als Plausibilitätsband mit Wochendiagramm und Warnliste (4.7), überschrieben „nachrichtlich". Fuß:
Status · Abbrechen · OK; OK übernimmt den Punkt in den Arbeitsstand. Schmal stehen die Karten
untereinander. Wählt der Anwender die Bedarfstagquelle (1), fragt eine `Rueckfrage` vor dem OK.

### 5.2 Einbindung in `BedarfsProfileDialog.razor`

- **Neue Parameter** `ZapfprofilGaben` (`Func<IReadOnlyDictionary<string,object>>?`) und
  `ZapfprofilUebernommen` (`Action<ZapfprofilErgebnisDaten>?`, nur DTO); ohne Delegat kein Knopf
  (Muster `Katalogwege`, `EPOS.UI.Daten/Katalogwege.cs:23`), nur bei `Art == BedarfsArt.Brauchwasser`.
- **Knopf „Zapfprofil erzeugen…"** im Aktionsschlitz der `SpeichernLeiste` (`:179`), nicht in der Leiste
  „Simulation · monatlicher Verlauf" (`:171`), die sonst zum zweiten Fuß würde (ZU6).
- **Weiche sichtbar:** `Optionsgruppe` „Rechenweg Brauchwasser: Bestandsprofile · Zapfprofil"; das OK
  des Zapfprofils stellt sie um, Zurückschalten behält die Zonen, ein Hinweis sagt, dass die
  Bestandsprofile dann nicht mitrechnen (ZU4).
- **Leiste „monatlicher Verlauf":** bei Weg `GENERATOR` im Arbeitsstand die Monatssummen von Zapfung
  und Zirkulation aus dem Vorschau-Delegaten (2.2), beschriftet „rechnet den Zapfprofilweg"; bei
  `BESTAND` wie heute. Fall in `BedarfsProfileDialogTests`.
- **Rückweg und Schreibweg:** `BedarfsProfileHuelle.Oeffnen` gibt heute nur `bool` zurück
  (`BedarfsProfileHuelle.cs:43-67`). `Oeffnen` und `Gaben` bekommen deshalb einen
  **Zapfprofil-Behälter** (Arbeitsstand plus Weg), den der Dialog über `ZapfprofilUebernommen` und die
  Optionsgruppe füllt — auch wenn der Anwender nur die Optionsgruppe umschaltet. Das OK des Zapfprofils
  prüft (jede Zone mit Nutzungsart und Bezugsmenge) und übergibt; geschrieben wird erst mit dem OK des
  Bedarfsprofil-Dialogs. Die Startseite (`StartseiteHuelle.Brauchwasser`, `:900-911`) schreibt
  `Del/Add_Projekt_Brauchwasser` **und** `ZapfprofilCtrl.Speichern(idProjekt, stand, vorgang)` in
  **einem gemeinsamen `DbVorgang`** (`Add_Projekt_Brauchwasser` nimmt ihn an, `WizardCtrl.cs:2763`);
  der Gebäudekatalog ebenso in `GebaeudeKatalogHuelle.BrauchwasserSchreiben`. `Speichern` schreibt
  `Weg` immer. Ein Esc der vierten Überlagerung schließt nur sie.
- **Ohne Projekt** (Gebäudekatalog aus der Verwaltung) reicht die Hülle keinen Delegaten; im Assistenten
  mit noch ungespeichertem Projekt ebenso nicht (Frage ZU10).

### 5.3 Feldtabelle

Präfix der Ressourcen **`ZPG_`** (frei, 0 Treffer im Bestand); Katalogdialog `ZPGK_`. Herkunft wird
als Kurztext aus `Quelle` und `Ausgabe` gezeigt (`ZPG_HERKUNFT_*`) — nur Norm, Verfahren oder
Eigenkonstruktion, nie ein Hersteller- oder Produktname, nie Tabellenwerte; `Beleg` erscheint nirgends.

| Feld | Stufe | Typ | Vorgabe / Herkunft | Ressourcenschlüssel |
|---|---|---|---|---|
| Zonenname | Einfach | Text | „Zone n" | `ZPG_LBL_ZONENNAME` |
| Nutzungsart | Einfach | Auswahlfeld (Katalog; ab Erweitert aufgeklappt mit Bezugsart, Herkunft, Status, Katalogversion) | — Pflicht | `ZPG_LBL_NUTZUNGSART` |
| Bezugsmenge | Einfach | Zahlenfeld, Einheit aus der Bezugsart | — Pflicht, > 0; bei Wohnungstabelle aus ihr | `ZPG_LBL_BEZUGSMENGE` |
| Bedarfsniveau | Einfach | Optionsgruppe niedrig/mittel/hoch | mittel, Katalogwert | `ZPG_LBL_NIVEAU` |
| Wohnungstabelle | Erweitert (nur Wohnen) | `Raster`: Anzahl, Raumzahl, Personen, Ausstattungsklasse | Belegung nach Raumzahl aus dem Katalog | `ZPG_LBL_WOHNUNGSTABELLE`, `ZPG_LBL_ANZAHL`, `ZPG_LBL_RAUMZAHL`, `ZPG_LBL_AUSSTATTUNG` |
| Personen je WE | Erweitert | Zahlenfeld P/WE | Belegungslogik nach DIN 4708-2, Katalog | `ZPG_LBL_PERSONEN_JE_WE` |
| Wohnfläche je WE | Erweitert | Zahlenfeld m² | Annahme, Flächenformel DIN V 18599-10 | `ZPG_LBL_WOHNFLAECHE_JE_WE` |
| Anlagentopologie | Erweitert | Auswahlfeld | Speicher; bestimmt Auslegungsgröße und Gültigkeit der Verfahren | `ZPG_LBL_TOPOLOGIE` |
| Zirkulation vorhanden | Erweitert | Optionsgruppe ja/nein | ja bei Großanlage nach DVGW W 551 | `ZPG_LBL_ZIRKULATION_VORHANDEN` |
| Kalender / Ferien | Erweitert | Auswahlfeld + vier Zeiträume | Kalenderart der Nutzungsart, Gebäude (A8) | `ZPG_LBL_KALENDER` |
| Bundesland | Erweitert | Auswahlfeld, gesperrt bis A6 | — | `ZPG_LBL_BUNDESLAND` |
| Jahresmesswert | Erweitert | Zahlenfeld + Einheit kWh/m³ + Bilanzgrenze + Quelle + Zeitraum | leer | `ZPG_LBL_JAHRESMESSWERT`, `ZPG_LBL_MESSWERT_EINHEIT`, `ZPG_LBL_MESSWERT_GRENZE`, `ZPG_LBL_MESSWERT_QUELLE`, `ZPG_LBL_MESSWERT_ZEITRAUM` |
| Rechenweg der Jahresreihe | Erweitert | Optionsgruppe deterministisch/stochastisch | deterministisch | `ZPG_LBL_RECHENWEG_JAHRESREIHE` |
| Tagesbedarf (auto/manuell) | Erweitert | Optionsgruppe + Vorschlag + Zahlenfeld + angesetzt + Rechenweg | auto, Katalog | `ZPG_LBL_TAGESBEDARF`, `ZPG_LBL_VORSCHLAG`, `ZPG_LBL_MANUELL`, `ZPG_LBL_ANGESETZT`, `ZPG_LBL_RECHENWEG` |
| Ladeleistung (Gebäude) | Erweitert | wie oben, dazu Ladefenster h/d und Beginn | auto, Schätzhilfe | `ZPG_LBL_LADELEISTUNG`, `ZPG_LBL_LADEFENSTER`, `ZPG_LBL_LADEFENSTER_BEGINN` |
| Zirkulation (Gebäude) | Erweitert | Methode, Länge, W/m, Anteil, Lage, manuell | Flächenkennwert mit A_N aus Wohnfläche × WE bzw. Gebäude | `ZPG_LBL_ZIRK_METHODE`, `ZPG_LBL_ZIRK_LAENGE`, `ZPG_LBL_ZIRK_VERLUST`, `ZPG_LBL_ZIRK_ANTEIL`, `ZPG_LBL_ZIRK_LAGE` |
| Leitungsinhalt | Erweitert | Zahlenfeld l | aus Leitungslänge (Parameter) | `ZPG_LBL_LEITUNGSINHALT` |
| Spezifischer Bedarf | Experte | Zahlenfeld kWh/(Einheit·d) | Katalogwert mit Bandbreite | `ZPG_LBL_BEDARF_SPEZ` |
| Zapftemperatur | Experte | Zahlenfeld °C | Bezugstemperatur des Katalogs; bei Großanlage Mindesttemperatur nach DVGW W 551 (Parameter) | `ZPG_LBL_ZAPFTEMPERATUR` |
| Kaltwasser Mittel / Amplitude | Experte | Zahlenfeld °C / K | Konvention, sinusförmig (nur Bilanz) | `ZPG_LBL_KALTWASSER_MITTEL`, `ZPG_LBL_KALTWASSER_AMPLITUDE` |
| Kaltwasser der Auslegung, Speichertemperatur | Experte | Zahlenfeld °C | Parameter | `ZPG_LBL_KALTWASSER_AUSLEGUNG`, `ZPG_LBL_SPEICHERTEMPERATUR` |
| Zirkulation Kennwert / Fläche / Laufzeit | Experte | Zahlenfeld | Verfahren DIN V 4701-10, Gebäude, DVGW W 551 | `ZPG_LBL_ZIRK_KENNWERT`, `ZPG_LBL_ZIRK_FLAECHE`, `ZPG_LBL_ZIRK_LAUFZEIT` |
| Auslastungsgang | Experte | zwölf Zahlenfelder, leer = Katalog | Jahresgang der Nutzungsart | `ZPG_LBL_AUSLASTUNGSGANG` |
| Tagesgangsatz | Experte | Auswahlfeld | Satz der Nutzungsart; Eigenkonstruktion, Ecodesign, Anwenderkopie; VDI 4655 nur über Z4b | `ZPG_LBL_TAGESGANGSATZ` |
| Tagesgang je Tagtyp | Experte | Knopf „Tagesgang bearbeiten…" | — | `ZPG_BTN_TAGESGANG` |
| Seed / Realisierungen | Experte | Zahlenfeld | 1 / 10; Auslegung nach 4.4 | `ZPG_LBL_SEED`, `ZPG_LBL_REALISIERUNGEN`, `ZPG_LBL_REALISIERUNGEN_AUSLEGUNG` |
| Zapfkategorien / Streuung | Experte | `Raster`, als Katalogkopie bearbeitbar | Jordan/Vajen | `ZPG_LBL_KATEGORIEN`, `ZPG_LBL_STREUUNG` |
| Auslegungsperzentil | Experte | Optionsgruppe P95/P99 | nach K3 | `ZPG_LBL_PERZENTIL` |

Dazu Titel, Knöpfe, Reiter, Kennzahlen und Hinweise: `ZPG_TITEL`, `ZPG_BTN_STOCHASTIK`,
`ZPG_BTN_AUSLEGUNG`, `ZPG_BTN_ZONE_NEU`, `ZPG_BTN_ZONE_DUPLIZIEREN`, `ZPG_BTN_ZONE_ENTFERNEN`,
`ZPG_REITER_*` (5), `ZPG_KZ_*` (Kennzahlen), `ZPG_HINW_*` (Warnliste), `ZPG_AUS_*` (Auslegung, samt
`ZPG_AUS_SCHNELLAUSLEGUNG`, `ZPG_AUS_NACHRICHTLICH`, `ZPG_AUS_NICHT_BELASTBAR`),
`BPF_BTN_ZAPFPROFIL_BW`, `BPF_LBL_RECHENWEG_BW`, `BPF_HINW_ZAPFPROFILWEG` (Bedarfsprofil-Dialog).
Geschätzt 180–220 Schlüssel je Sprache.

**Ressourcen:** `EPOS.Kern/MyResource/Resource.resx` und `Resource.en-US.resx` (UTF-8 mit BOM, CRLF),
deutscher Rückfall als Parameter-Vorgabe; danach den Designer ziehen: `py
Werkzeuge/ResourceDesigner/designer_neu.py schreiben` (Windows, mit `PYTHONIOENCODING=utf-8`; auf
Linux `python3`). Fachbegriffe (Zapfprofil, Zirkulation, Summenlinie, Bedarfskennzahl, Nenninhalt)
vor den en-US-Werten im Glossar festlegen.

### 5.4 Katalogdialog „Brauchwasser-Nutzungsarten"

`EPOS.UI/Dialoge/Bedarf/TwwNutzungsartAdminDialog.razor` nach dem Muster `BedarfAdminDialog.razor`:
`epos-katalog-dialog`, `Katalograhmen Gestapelt="true"`, `Katalogliste` über die volle Breite (Spalten
Nutzungsart, Bezugsart, Kalender, Herkunft, Katalogversion, Status), darunter ein lesendes
`Formularraster` mit Kennwerten je Niveau, Bilanzgrenze, Quelle je Wertgruppe, Version und Freigabe.
Katalog **ohne Arbeitsstand**: jede Aktion schreibt sofort, kein Abbrechen; Esc und ✕ wirken wie
Beenden. Fuß nach DL-2
([`Konzept_Knopfleisten_Administration_EPOS-Plan.md`](Konzept_Knopfleisten_Administration_EPOS-Plan.md)):
**Tagesgang… · Grafik… · Füller · Neu… · Ändern… · Löschen · Beenden**. „Ändern…" öffnet bei einem
Auslieferungs- oder benutzten Eintrag den Editor mit „Speichern unter" (K7, 3.2); „Löschen" ist dort
gesperrt, der Grund steht im Kurztext. Das Schema der
[Administrationsdialoge](Konzept_Administrationsdialoge_Neuordnung_EPOS-Plan.md) (Variante B) ist noch
nicht umgesetzt; der Dialog folgt dem heutigen Muster und zieht mit dessen Stufe 3 nach.

**Menü:** In `Menuetabelle.cs:236` wird `MenuItem_Brauchwasser` zum Untermenü mit zwei Punkten —
„Brauchwasserprofile" (`MENU_BRAUCHWASSERPROFILE`, bestehend `Seitenschluessel.BrauchwasserAdmin`) und
„Brauchwasser-Nutzungsarten" (`MENU_BRAUCHWASSER_NUTZUNGSARTEN`). Kette: `Masken.BrauchwasserNutzungsarten
= "Form_Brauchwasser_Nutzungsarten"`, `Seitenschluessel.BrauchwasserNutzungsarten` samt Liste `Alle`,
Fall in `WinFormsNavigation.cs`, Tests `MenuebandTests`, `HauptfensterTests`, `SeitenschluesselTests`.
Auf iOS benannt abgelehnt wie `BrauchwasserAdmin` (fehlt in der Positivliste von `AppWurzel.OeffneMaske`).

### 5.5 Hülle und Naht

**`EPOS.UI.Daten/Bedarf/ZapfprofilHuelle.cs`** (`internal static`, Muster `BedarfErgebnisHuelle.cs`)
mit `Gaben(int idProjekt, ZapfprofilStand? arbeitsstand, Action<ZapfprofilStand> uebernommen)`,
`AuslegungGaben(ZapfprofilStand)` und `KatalogGaben()`, jeweils `IReadOnlyDictionary<string,object>`:
Katalogzeilen, Zonen und Vorschau als DTO aus `ZapfprofilDaten.cs`, Texte aus `MyResource.Resource`,
Delegaten mit DTO-Signaturen auf `ZapfprofilRechner` und `ZapfprofilAuslegung`, Zeichenmodelle aus
`ChartRenderer`. Die Hülle ist die einzige Stelle, die `ZapfprofilStand` in DTO übersetzt und zurück.
**Windows:** `BedarfsProfileHuelle.Gaben` (`:215`) setzt nur die zwei Delegaten aus der plattformfreien
Hülle und führt den Zapfprofil-Behälter (5.2); die Verwaltung ruft `KatalogGaben` über eine schlanke
`TwwNutzungsartAdminHuelle`. Danach die Windows-Schale auf Linux bauen
(`-p:EnableWindowsTargeting=true`), `ParametersatzTests` grün. **iOS:** erreichbar erst mit einer
plattformfreien Bedarfsprofil-Hülle in `EPOS.UI.Daten/Bedarf/` (iU11, sonst in Z4 vorgezogen, A11);
der Katalogdialog bleibt dort geschlossen.

### 5.6 Diagramme

Alle als `Zeichenmodell` aus dem Kern-Renderer `EPOS.Kern/Allgemein/Bericht/ChartRenderer.cs`, kein neuer
Renderer: Tagesgang und Wochenprofil `StundenprofilModell` (`:1877`), Jahresgang gestapelt Zapfung +
Zirkulation `MonatsStapelModell` (`:3048`, aus den getrennten Monatssummen, 2.2), Dauerlinie
`JahresverlaufModell`-Familie bzw. `DauerlinieWaermeModell` (`:363`) mit Perzentillinien,
Wertepaarkurve `KennlinienModell` (`:1467`), Wochendiagramm mit Füllstand `SpeicherbetriebModell`
(`:3244`). **Neu** ist nur ein `SummenlinieModell` (kumulierter Bedarf und Versorgung über 1440
Minuten mit markiertem Abstand); dafür ein Fall in `Proben/ChartProben` samt neuer Messlatte.
`ZeichenmodellWacheTests` und `DiagrammfarbenWacheTests` gelten.

### 5.7 Tests der Oberfläche

bunit in `EPOS.UI.Tests/Dialoge/` mit `EposBunitContext` (Kulturpinnung), Regeln aus
`EPOS.UI/CLAUDE.md`: je Stufe der Feldbestand, Rückweg mit Ergebnis und mit Abbruch, Fall ohne Gaben.

| Klasse | Fälle (Auswahl) |
|---|---|
| `ZapfprofilDialogTests` | `Einfach_zeigt_vier_Felder_je_Zone`, `Erweitert_zeigt_die_Schaetzhilfen`, `Erweitert_zeigt_die_Wohnungstabelle_nur_bei_Wohnen`, `Die_Stufe_behaelt_Ueberschreibungen`, `Ok_ohne_Bezugsmenge_zeigt_den_Banner`, `Ok_uebergibt_den_Stand`, `Abbrechen_verwirft`, `Esc_schliesst_nur_die_oberste_Ueberlagerung`, `Angesetzt_folgt_dem_Umschalter` |
| `TagesgangEditorTests` | Summenkontrolle grün/rot, Tag kopieren/einfügen, Auslieferung und benutzter Satz nur als Kopie |
| `ZapfprofilAuslegungDialogTests` | drei Karten getrennt, genau ein empfohlener Punkt, Verfahrensvergleich als Band „nachrichtlich", Perzentil leer vor dem Lauf, Strich bei D_max = 0, Vermerk „Schnellauslegung" in der Stufe Einfach, Rückfrage bei Bedarfstagquelle (1), OK übernimmt den Punkt |
| `TwwNutzungsartAdminDialogTests` | Fuß nach DL-2, Löschen einer Auslieferung gesperrt, Ändern eines benutzten Eintrags nur als „Speichern unter", Esc wie Beenden |
| `BedarfsProfileDialogTests` (Nachzug) | Knopf nur mit Delegat und nur bei Brauchwasser; Optionsgruppe der Weiche; Leiste „monatlicher Verlauf" zeigt bei Generator die Generatorreihe |

Wachen: Knopfleisten, Schließkreuz, Überlagerungstitel, Parametersatz, Hüllenweg, Fußleiste,
Katalogdialog. **Rasterprobe** vor dem Katalogdialog (neue `Katalogliste`).

### 5.8 Hilfe, Wiki und Logbuch

- Hilfeschlüssel in `WindowsFormsApplication1/Allgemein/Hilfe/help_mapping.txt`: `Form_Zapfprofil.btn_Help`,
  `Form_Zapfprofil_Berechnung`, `Form_Brauchwasser_Nutzungsarten.btn_Help`.
- Berechnungsseite `EPOS.Kern/Allgemein/Hilfe/Berechnung/Zapfprofil.wiki` neben der bestehenden
  `Brauchwasser.wiki`.
- Bedienseite `Projekte/Wiki/Programm Dokumentation - Brauchwasser-Zapfprofil.wiki`; Regeln nach
  [`Konzept_Hilfesystem_Wikidokumentation.md`](Konzept_Hilfesystem_Wikidokumentation.md) 13.3/13.4:
  nur die Funktion, keine Normtabellen, keine Hersteller- und Produktdaten
  (`WikiProduktdatenWacheTests`), Gegenleseprüfung mit dem Suchmuster aus `CLAUDE.md`.
- Logbuch-Entwurf (nur Vorschlag, Versionsnummer beim Anwender erfragen): „Brauchwasser-Zapfprofile
  lassen sich je Nutzungszone erzeugen und als Brauchwasserbedarf rechnen." (Z1) und „Die
  Speicherauslegung für Trinkwarmwasser zeigt Summenlinie, Perzentil und Normvergleich nebeneinander
  und empfiehlt den Punkt der Summenlinie." (Z2).

---

## 6. Lizenz- und Datenstrategie

**Grundsatz:** Verfahren werden umgesetzt, Normzahlen werden gekapselt. Formeln und Rechenregeln
stehen im Code und in diesem Papier; Kennwerte, Nachschlagewerte, Normkonstanten, Regelwerksgrenzen,
Formvektoren und Referenzprofile stehen nur als Katalogparameter mit Quellenverweis — nie als Tabelle
in Oberfläche, Wiki, Handbuch, Mockup, **Quelltext**, Code-Kommentar oder Testdaten des
Repositoriums.

**Regeln für Normkonstanten und Katalogwerte:**

- **(a) Nie im Quelltext.** Die Klassen bekommen Normkonstanten (DIN-4708-Parameter, Parameter der
  DIN 1988-300, Koeffizienten der A100, Grenzen nach DVGW W 551) über den `Parametersatz` aus
  `Tab_TwwParameter_STAMM` (3.1). Tests arbeiten mit erfundenen Parametern und prüfen Formeln und
  Relationen, nie Normzahlen.
- **(b) Testdatenbank nur fiktiv.** Die Testdatenbank enthält nur fiktive Nutzungsarten, Tagesgangsätze,
  Bedarfstage und Parameter mit runden Werten, `Status = 'EIGEN'`, Herkunftsart `FIKTIV`, Quelle
  „Testkatalog (fiktiv)". Die Auslieferungswerte (Erstbefüllung nach K2/K8) kommen aus einem
  Katalogpaket **außerhalb des Repositoriums**, das `Werkzeuge/Auslieferungsvorlage` beim Bau der
  Vorlage mit `--katalogpaket <ordner>` einspielt; Bestandsinstallationen erhalten es über einen
  Katalogimport in der Verwaltung (Z4, Muster Herstellerdaten-Import), nicht über den Schemaschritt
  (Frage ZU14). **Paketformat (N2):** je Tww-Katalogtabelle eine Datei `<Tabelle>.csv` (UTF-8,
  Kopfzeile mit den Spaltennamen, Trenner `;` oder `,`, Felder nach RFC 4180, Zahlen mit Punkt);
  jede Zeile einer Kopftabelle (Nutzungsart, Tagesgangsatz, Bedarfstag, Parameter, DIN-4708-Wert)
  trägt `Status = 'AUSLIEFERUNG'`; `ReadOnly` ist dort 1 oder fehlt (dann 1). Tagesgänge und
  Ereignisse führen keine dieser Spalten (N3). Das Paket vergibt die `ID` der Köpfe selbst;
  Kindzeilen und Nutzungsarten verweisen über `ID_Tagesgangsatz` bzw. `ID_Bedarfstag` auf diese
  IDs. Ein leeres Feld ist NULL. Das Paket ersetzt den Tww-Katalog der Quelle in einer Transaktion; ein Fehler nennt Datei und Zeile, rollt zurück und
  bricht ab. Jede Auslieferungszeile der Vorlage trägt `ReadOnly = 1` (3.2).
- **(c) Wache.** `TwwKatalogWacheTests`: keine Zeile einer `Tab_Tww*_STAMM` der Testdatenbank mit
  `Status = 'AUSLIEFERUNG'`, jede Zeile `EIGEN` mit Herkunftsart `FIKTIV` und Quelle „Testkatalog
  (fiktiv)" in jeder Provenienzgruppe, Katalogversion nie leer, Skript des Testkatalogs wiederholbar
  (N2); dazu Posten im Prüfbericht der Auslieferungsvorlage (3.2).
- **(d) Z1 und Z2 brauchen keinen echten Katalog.** Der fiktive Testkatalog genügt für Abnahme und
  Sichtprobe; bleibt der Katalog nach K8 leer, bleiben die Stufen trotzdem abnehmbar.
- **(e) Quelle ohne Hersteller.** Die Spalte `Quelle` nennt nur Norm, Verfahren oder Eigenkonstruktion
  samt Ausgabe. Wo eine Kennwert-Bandbreite oder ein DIN-4708-2-Kennwert nur über eine Sekundärquelle
  (etwa frei publizierte Herstellerunterlagen, Konzept 3.4) zitierfähig ist, steht diese in der
  internen Spalte `Beleg`, die weder Oberfläche, Bericht noch KiSicht zeigen.
  `WikiProduktdatenWacheTests` wird sinngemäß auf die Katalogtexte der `Tab_Tww*_STAMM` der
  Testdatenbank (ohne `Beleg`) und auf die `ZPG_`/`ZPGK_`-Ressourcen erweitert — offen, Stufe Z4
  (Kapitel 7, N3).

| Quelle | Umgang |
|---|---|
| **VDI 4655** | nur Methodik (Typtagsystematik, Wetterzuordnung, Gleichungen). Die Datensätze spielt der lizenzierte Anwender über die Import-Schnittstelle (2.5, Z4b) selbst ein, lokal, in `Tab_TwwTyptag_IMPORT`; nie im Produkt, in der Auslieferungsvorlage, im Repository oder in der CI. Vor der Codierung mit VDI/Beuth klären (K3a) |
| **VDI 6002 Bl. 1/2** | Kennwert-Bandbreiten nur mit Quellenangabe nach Regel (e); eigene Formvektoren statt Tagesgang-Tabellen. Die aus VDI-Bildern digitalisierten Typen des Bestandskatalogs übernimmt der neue Katalog nicht; ob der Bestandskatalog sie weiter ausliefert, bleibt bis K8 offen (1.6, Risiko in Kapitel 8) |
| **DIN EN 12831-3 samt A100/A1** | Verfahren frei; Referenzprofile und das DIN-4708-Profil erst nach Beschaffung und Klärung mit DIN Media (K1/K8), nur im Katalog; Ergebnisse mit Entwurfsvermerk |
| **DIN V 18599-10, DIN 4708, DIN 1988-300, DIN V 4701-10, DVGW W 551** | Kennwerte und Grenzen als Parameter nach Regel (a); die Oberfläche nennt Verfahren und Ausgabe, druckt keine Tabelle ab |
| **frei** | Ecodesign-Zapfprofile (VO (EU) 814/2013 und 812/2013), Jordan/Vajen-Parametrik, DOE/ASHRAE-Schedules; OpenDHW (MIT) nur als Testorakel mit Attribution im Testordner, nicht im Auslieferungspaket |
| **Hinweis** | lpagg und demandlib (MIT) sind Grundlage des Wärmespeicher-Tools; hier nur als Quellenhinweis, kein Code, keine Daten |

**Lokale Testdaten.** `Referenzlaeufe/Normzahlen/` mit `vdi4655/`, `vdi6002/` und
`zapfprofil/normtabellen.js` (Ladedatei des Mockups) ist per `.gitignore` ausgeschlossen (Zeilen
`Referenzlaeufe/Normzahlen/*` und `!Referenzlaeufe/Normzahlen/LIESMICH.md` — allein das LIESMICH
der obersten Ebene ist versioniert, N2; Muster U8 der Gebäudesimulation): nie im Repository, in einem
CI-Artefakt, in der Testdatenbank oder in der Auslieferung. Tests schweigen ohne Ordner; der Nachweis
ist **lokal**, sein Auszug nennt Abweichungen, keine Absolutwerte. Ob die lokalen Kopien zulässig
sind, klärt K8 — VDI 4655 untersagt schon innerbetriebliche Vervielfältigung (Risiko in Kapitel 8).

**Wache.** `RepositoryOrdnungWacheTests` führt den Fall `Normzahlen_stehen_im_gitignore`: die
Zeilen sind vorhanden, `git check-ignore` schließt erfundene Pfade darunter aus und lässt das
`LIESMICH.md` frei, und außer ihm steht dort kein Pfad im Index oder unversioniert vorgemerkt (`git
ls-files --cached --others --exclude-standard`); ohne Git schweigen die Git-Prüfungen (N2).

**Keine Hersteller- und Produktdaten.** Nutzungsarten tragen neutrale Namen, die Nenninhaltsliste ist
neutral, N_L erscheint nur als Kriterium. **Keine Messobjektdaten** vor der Freigabe nach K5.

---

## 7. Stufenplan, Reihenfolge und Abnahme

| Stufe | Inhalt | Vorbedingung | Abnahme | Aufwand (Annahme) |
|---|---|---|---|---|
| **Z0 — Grundlagen und Schema** | K1 und K8 anstoßen; Schemaschritt T1 (3.1, 3.2) mit `TwwSchema.cs`, `ZapfprofilCtrl` (lesend), `TwwNutzungsartCtrl`, `KatalogRegistry`, fiktiver Testkatalog, `Parametersatz`; Auslieferungsregel und Katalogpaket-Option der Auslieferungsvorlage; Einträge in `ProjektExportImportCtrl`; Wachen `TwwKatalogWacheTests` und `Normzahlen_stehen_im_gitignore`; Quellendossier; **Konzept V2 (A10)** mit Verschiebung von V1.2 nach `ueberholt/`; **Bereinigung der Zahlenteile der Grundlagenpapiere nach K8 (A9)**; bei K8 „ersetzen": Ersatz der vier Digitalisate als eigener Schritt mit Einfrierprüfung 1007/1045/1046 | Entscheide K2, K7, A1, A2, ZU12; Schemanummer nachgemessen | Kern-Filter grün; `TestdatenbankSchemastandWacheTests`, `TwwKatalogWacheTests` grün; `SqlDialektPruefer` grün; Testdatenbank über LFS; je ein Test für `ProjektDuplizierenCtrl` und `ProjektExportImportCtrl`; Referenzlauf der fünf CI-Projekte grün innerhalb der Toleranz; Windows-Schale auf Linux gebaut | 7–10 PT |
| **Z1 — Bilanz deterministisch** | S1 samt Temperaturumrechnung und Messwertgrenzen, S2 mit Tagtypgewicht und Ferienregel, S5 mit Zonenanteil und Vorgabe Flächenkennwert, Fassade mit `Bilanzreihe`, Weiche (2.2) samt getrennter Monatssummen, `ZapfprofilCtrl.Speichern/Eingang`, `Tab_TwwProjekt`; Dialog Stufe Einfach mit Vorschau (Tagesgang, Wochenprofil, Jahresgang, Kennzahlen) als Überlagerung unter Windows; Knopf, Optionsgruppe, Leiste „monatlicher Verlauf" und gemeinsamer `DbVorgang` im Bedarfsprofil-Dialog; Wiki-Entwurf | Z0; A3, A4/ZU5, A6, K4; fiktiver Testkatalog genügt | Tests aus 2.4, 4.1–4.3; **ein unabhängig per Tabellenkalkulation gerechneter fiktiver Referenzfall über 8760 h mit Abweichung 0** (Testdaten erfunden, Konzept 3.6 P1); `ZapfprofilWeicheTests` auf einer Projektkopie der Testdatenbank; Referenzlauf grün innerhalb der Toleranz (kein Referenzprojekt setzt die Weiche); `EinheitenWacheTests` mit den neuen Dateien; bunit; Sichtabnahme Windows | 14–18 PT |
| **Z2 — Auslegung deterministisch** | Bedarfstag mit Vorgaberegel und Konstruktor, Wochenreihe, Summenlinie mit Speicherart, Übertrager, Einschaltpunkt, Wertepaarkurve, Monotonieprüfung und Ladezeit, Schnellpfad, Wohnungstabelle und DIN-4708-Kennzahl, DIN 1988-300 nachrichtlich, Speicherauslegung nach V4 mit Ladefenster, GLF, Plausibilitätsband und Warnliste, Großanlagenerkennung, Topologiegruppen; Überlagerung „Auslegung"; `SummenlinieModell` | Z1; K1/K8 für A100- und DIN-4708-Profil (ohne: Konstruktor) | `SummenlinieTests`, `Din4708KennzahlTests`, `SpeicherauslegungTests`, `AuslegungsergebnisTests`, `GrossanlageTests`, `ZapfprofilTrennungWacheTests`; `ChartProben` mit neuem Fall; Referenzlauf unberührt | 16–20 PT |
| **Z3 — Stochastik** | T2, `ZapfZufall` samt Plattformtest, Generator mit gestutztem Mittel, Ensembles der Jahresreihe und des Bedarfstags über `Kulturweitergabe`, Perzentil je Topologie, Gleichzeitigkeit als Ergebnis, Entkopplung der Urlaube, Rechenweg der Jahresreihe „stochastisch" | Z2; ZU8 | `ZapfZufallTests`, `ZapfereignisgeneratorTests`, `ZapfensembleTests` (Toleranz nach 4.4, √N, Topologie); lokal gegen DHWcalc-Referenzdateien; Referenzlauf unberührt | 16–22 PT |
| **Z4 — Oberfläche vollständig** | Stufen Erweitert und Experte, Zonenliste für Mischnutzung, Wohnungstabelle, Tagesgang-Editor, Auslastungsgang, Kategorien als Katalogkopie, Schätzhilfen, Warnlogik, Dauerlinie, Katalogdialog mit Untermenü und Katalogimport, KiSicht, Hilfeschlüssel, Wiki, beide Sprachen | Z3; ZU3 (iU11) | alle Oberflächenwachen; Rasterprobe; `MenuebandTests`; erweiterte `WikiProduktdatenWacheTests`; Wiki gegengelesen; iOS-Lauf nur nach Rückfrage und nur, wenn die Bedarfsprofil-Hülle umgezogen ist | 11–14 PT (+2–3 PT iPad-Voraussetzung) |
| **Z4b — VDI-4655-Import mit Typtagzuordnung** | T3, `Normformvektorleser`, `Typtagzuordnung` mit Wetterkopplung (Vorfragen 4.2), Importdialog | Z4; K3a, K8 | Tests mit erfundenen Typtagen; Auslieferungsvorlage leert `Tab_TwwTyptag_IMPORT`; kein VDI-Wert in Repository oder CI | 3–5 PT |
| **Z5 — Kalibrierung und Validierung** | Messdatenimport, Vergleichsbericht, Validierung gegen freie Messreihen und freigegebene INEKON-Projekte, Kalibrierung der Nichtwohn-Parameter, Katalogausbau auf 25–27 Typen; gegebenenfalls Referenzprojekt auf dem Generator (ZU7) | Z4; K5, K6 | Validierungsbericht mit messbaren Kriterien: Messspitze im P85–P95-Band der synthetischen Dauerlinie (Konzept 3.6), √N-Skalierung der Überschätzung, Formabgleich des Tagesgangs mit einer Schwelle (Parameter), Energie nach Kalibrierung exakt; bei Referenzprojekt: vierte Einfrierregel, Neueinfrieren mit Begründung, grüner CI-Lauf | 10–12 PT |

**Umsetzungsstand und Abweichungen:** N2 und N3 (Kapitel 11).

**Herleitung des Aufwands (Annahme, ±30 %).** Grundlage sind die Phasen P0–P5 des Konzepts (3.5),
angepasst an die Architektur und um den Mehrumfang dieses Papiers ergänzt:

| Stufe | Grundlage | Mehrumfang | Ergebnis |
|---|---|---|---|
| Z0 | P0 ohne Access-Hygiene: 5–7 | Parameter-, Bedarfstag- und DIN-4708-Tabellen, Provenienzgruppen, Kopierstellen, zwei Wachen: +2–3 | 7–10 |
| Z1 | P1 in Kern und Razor: 12–15 | Temperaturumrechnung, Messwertgrenzen, Zirkulationsanteil, Referenzfall 8760 h, Vorschauleiste und gemeinsamer `DbVorgang`: +2–3 | 14–18 |
| Z2 | P2: 12–14 | Speicherauslegung nach V4 (Lindley, GLF, Band, Warnliste, Wochendiagramm) +3–4; Wohnungstabelle DIN 4708 +1–2 | 16–20 |
| Z3 | P3: 15–20 | portabler Zufall samt Plattformtest, Auslegungsensemble und Topologie: +1–2 | 16–22 |
| Z4 | P4 ohne Wizard-Umbau und ohne VDI-Import: 11–14 | Katalogdialog mit Untermenü, KiSicht und Katalogimport sind darin enthalten | 11–14 |
| Z4b | — | VDI-4655-Import mit Typtagzuordnung: +3–5 | 3–5 |
| Z5 | P5: 10–12 | — | 10–12 |

**Summe:** 74–96 PT (±30 %) für Z0–Z5 ohne Z4b; dazu 3–5 PT für Z4b und 2–3 PT für die
iPad-Voraussetzung, die entfallen, wenn iU11 die Bedarfsprofil-Hülle vorher umzieht. Die Aufwände
sind Größenordnungen für Entwicklung und Nachweis; Agentenarbeit verkürzt die Kalenderzeit, nicht die
Prüfzeit.

**Abnahme in jeder Stufe** (Befehle aus `CLAUDE.md`, „Bauen und prüfen"): Kern-Filter bauen und
testen mit den xUnit-Schaltern; Referenzlauf 1030, 1007, 1017, 1045, 1046 gegen die aktuelle Basis
unter `Referenzlaeufe/` — **grün innerhalb der Toleranz**; Byte-Gleichheit wird bis Z5 erwartet und
im Protokoll als Information ausgewiesen, eine Byte-Abweichung ist ein zu erklärender Befund, kein
Rot; `ChartProben` bei neuem Modell; Dokumentations- und Ordnungswachen; die Windows-Schale auf
Linux gebaut, sobald eine Hülle oder Naht berührt ist.

**Agentenzuschnitt.** Je Stufe ein Worktree und ein Zweig, innerhalb der Stufe getrennte Dateien
(Rechenweg, Controller, Oberfläche); `AGENT_LAEUFT` vor Arbeit im Hauptbaum, nach der Abnahme löschen;
sofort committen, nicht pushen, keinen CI-Lauf auslösen; Modell ausdrücklich setzen (`opus` für
Implementierung und Tests, `sonnet` für Suchen). Kein Schemaschritt parallel zu einem anderen Auftrag,
der `SchemaStand.Zielversion` hebt. **Statuszeile:** je Stufe eine Zeile in
[`Status_iOS_Migration.md`](Status_iOS_Migration.md), der Block als Protokoll unter
`Dokumentation/ueberholt/Protokolle/`; dieses Papier bekommt keine Statusvermerke.

---

## 8. Risiken mit Gegenmaßnahme

| Risiko | Wirkung | Gegenmaßnahme |
|---|---|---|
| A100-Profildateien kommen spät oder nicht (K1) | Summenlinie ohne Normprofil | Konstruktor als Vorgabe, Stundenprofil nur mit Warnbanner und Rückfrage; Profile nur im Katalog, Austausch ohne Release |
| A100/A1 bleiben Entwürfe oder der Weißdruck ändert die Rechenregeln (Konzept 3.7) | Nacharbeit an der Summenlinie | Koeffizienten und Setzungen als Parameter mit Versionskennung der Norm; Rechenregeln in einer Klasse; Entwurfsvermerk im Ergebnis |
| K1/K8 blockieren die Erstbefüllung | Z1 wäre fachlich leer | fiktiver Testkatalog für Abnahme und Sichtprobe (Kapitel 6 (d)); Auslieferungskatalog als Paket außerhalb des Repositoriums |
| Urheberrecht an Normtabellen | Vertriebsrisiko | Kapselung nach Kapitel 6, keine Normkonstante im Quelltext, Wachen, juristische Prüfung mit Z0 (K8) |
| Lokale Normtabellen unter `Referenzlaeufe/Normzahlen/` im Arbeitsbaum, den `GitHub_Sync.bat` mit `add -A` synchronisiert; VDI 4655 untersagt schon innerbetriebliche Kopien | Normdaten landen im Repository | `.gitignore`-Zeilen bestehen; die Wache `Normzahlen_stehen_im_gitignore` hält die Zeilen und prüft auch unversioniert vorgemerkte Dateien (N2); lokale Kopien nur nach K8 |
| Digitalisate der VDI-6002-Bilder bleiben im Bestandskatalog (Abweichung vom Mockup A9) | Lizenzrisiko der Auslieferung bleibt | offen bis K8; bei „ersetzen" eigener Schritt in Z0 mit Einfrierprüfung 1007/1045/1046 und Begründung in `Referenzlaeufe/LIESMICH.md` |
| Zwei Rechenpfade laufen auseinander | Inkonsistenz Bilanz/Stochastik | `ZapfensembleTests` in der CI, gemeinsame Schichten S0–S2 |
| Akzeptanz der Stochastik bei Prüfern (Konzept 3.7) | Ergebnis wird angezweifelt | deterministischer Pfad als Nachweisebene, dokumentierter Seed, genau eine Empfehlung aus der Summenlinie, Normvergleich immer daneben |
| Nicht bitgleich auf Windows und iOS | Vorschau und Lauf weichen je Plattform ab | ganzzahliger Zufall, gerundete Einmalwerte statt transzendenter Funktionen in der Ziehung (4.2, 4.4); Vergleich mit Toleranz |
| Weiche ändert unbemerkt ein Referenzprojekt | Basis bricht | Vorgabe Bestandsweg ohne Zeile; Referenzlauf in jeder Stufe, Byte-Abweichung als zu erklärender Befund |
| Doppelzählung über die Bilanzgrenze des Messwerts | zu hoher Bedarf | Bilanzgrenze und Einheit des Messwerts je Zone, Kalibrierung auf Zapfung + Zirkulation bei Grenze 2 (4.1), Test |
| Kennwerte mit Bilanzgrenze 3 enthalten Speicherverluste | Speicherverlust doppelt oder falsch zugeordnet | Zone außerhalb des Zirkulationsanteils, Hinweis, kein zusätzlicher Speicherverlust (4.3); Abzug beim Messwert als Parameter |
| Doppelzählung Zirkulation / Netzverluste / Bilanzgrenze der Quelle | zu hoher Bedarf | Bilanzgrenze je Nutzungsart, Zonenanteil (4.3), Hinweis bei Netzverlust > 0 (ZU5) |
| Auslegung aus der Jahresspitze | Überschätzung | eigene Fassade ohne Bilanzreihe, Typen `Bedarfstag`/`Wochenreihe`, `ZapfprofilTrennungWacheTests`, Vermerk am größten Stundenwert |
| Katalogänderung ändert rückwirkend Projektergebnisse | Ergebnisse nicht reproduzierbar | benutzte Katalogzeilen unveränderlich, neue Versionen als neue Zeilen (3.2) |
| Nichtwohn-Parameter unvalidiert | falsche Spitzen | Kennzeichnung als Modellannahme, Kalibrierung Z5 |
| Rechenzeit Stochastik im Dialog | Oberfläche blockiert | Vorschau deterministisch, Ensemble nebenläufig mit Fortschritt, kleinere Vorgabe auf dem iPad |
| Schemanummer kollidiert mit parallelem Auftrag | Migration bricht | Nummer beim Beauftragen nachmessen, Schritte nie parallel |
| iOS-Seed ohne neue Tabellen | Fehler auf dem iPad | `ZapfprofilCtrl.Verfuegbar`, Bestandsweg, Knopf benannt gesperrt |
| Katalogpflege ohne Quelle | Nachvollziehbarkeit geht verloren | Provenienz je Wertgruppe NOT NULL, Vier-Augen-Vermerk, ReadOnly-Auslieferung |

---

## 9. Fragen mit Empfehlung

**Stand der Entscheide.** K1, K8, ZU1–ZU14 und die Lizenzfrage zu den VDI-6002-Kopien in der Ablage
des Anwenders (ZU15) sind am 23.09.2026 nach Empfehlung entschieden (Nachtrag N1, Kapitel 11).
Die Fragen ZU16–ZU18 sind mit den Umsetzungsbefunden der Stufe Z0 hinzugekommen (Nachtrag N2) und
offen. K2–K7 (samt K3a) und A1–A12 waren nicht Gegenstand dieses Entscheids; das Papier setzt ihre
Empfehlung weiterhin voraus (Mockup Abschnitt 8), entschieden sind sie damit nicht. Die Spalte
„Entscheid" zeigt den Stand je Punkt.

**K1–K8 und A1–A12 aus dem Mockup** (dort ausführlich), zusammengefasst mit der Empfehlung, die dieses
Papier voraussetzt:

| Nr. | Frage | Empfehlung | Entscheid |
|---|---|---|---|
| K1 | Beschaffung A100-Profildateien, Weißdruck-Status, DIN 4708-2/-3 | sofort anfragen; Verzicht auf Verwertungslizenz zur Mitauslieferung bestätigen | nach Empfehlung, 23.09.2026 (N1) |
| K2 | Typenumfang v1.0 | rund 15 neue Typen neben dem Bestandskatalog; Ablösung erst mit K6 | Empfehlung vorausgesetzt |
| K3 | Auslegungsperzentil | P99 Vorgabe, P95 wählbar; Brauchwasser-Auslegung nur aus der Dreiergruppe, Empfehlung der Summenlinienpunkt | Empfehlung vorausgesetzt |
| K3a | VDI-4655-Datenstrategie | Import-Schnittstelle (Z4b), gleichrangig, Vorgabe Eigenkonstruktion | Empfehlung vorausgesetzt |
| K4 | Kaltwasser in der Bilanz | fester Jahresgang in Z1, Kopplung an die Klimaregion als Option in Z4; die Auslegung rechnet unabhängig davon mit `θ_KW,Auslegung` | Empfehlung vorausgesetzt |
| K5 | Messdaten | Freigabe vor Z5, bis dahin nur Verhältniszahlen | Empfehlung vorausgesetzt |
| K6 | Bestandsweg | Koexistenz bis nach Z5 | Empfehlung vorausgesetzt |
| K7 | Katalogpflege | Auslieferung ReadOnly, Vier-Augen-Freigabe, Anwenderkopie „eigen", benutzte Zeilen unveränderlich | Empfehlung vorausgesetzt |
| K8 | Juristische Prüfung | mit Z0 beauftragen; umfasst auch die lokalen Normkopien und die Digitalisate des Bestandskatalogs | nach Empfehlung, 23.09.2026 (N1) |
| A1 | Ort des Rechenwegs | Ordner im Kern | Empfehlung vorausgesetzt |
| A2 | Übergabeform | eigene Projekttabelle mit Weiche; Reihe je Lauf neu gerechnet | Empfehlung vorausgesetzt |
| A3 | Weiche exklusiv oder additiv | exklusiv | Empfehlung vorausgesetzt |
| A4 | Zirkulation und Netzverluste | eigene Teilreihe im Brauchwasserkanal; zur F2-Verteilung siehe ZU5 | Empfehlung vorausgesetzt |
| A5 | Einstieg und Menü | Knopf im Bedarfsprofil-Dialog, Untermenü mit zwei Punkten | Empfehlung vorausgesetzt |
| A6 | Kalender | Z1 mit Kennzeichen der Klimaregion und Ferienfenstern; Feiertags-/Ferientabelle später | Empfehlung vorausgesetzt |
| A7 | Feinauflösung | Stunde in der Bilanz, Minuten nur in der Auslegung | Empfehlung vorausgesetzt |
| A8 | Zone und Gebäude | optionale Bindung, belegt nur vor (auch die Ferienzeiten) | Empfehlung vorausgesetzt |
| A9 | Lizenz im Bestand | Digitalisate im neuen Katalog nicht übernehmen; Ersatz im Bestandskatalog nach K8 (Abweichung vom Mockup, 1.6); Zahlenteile der Grundlagenpapiere vorbehaltlich K8 entfernen (Posten in Z0) | Empfehlung vorausgesetzt |
| A10 | Konzeptpapier | Fassung V2 auf die heutige Architektur nach den Entscheiden dieser Liste; V1.2 dann nach `ueberholt/` (Posten in Z0) | Empfehlung vorausgesetzt |
| A11 | iOS | Bedarfsprofil-Hülle in Z4 vorziehen, falls iU11 nicht steht; kleinere Ensemble-Vorgabe auf dem iPad | Empfehlung vorausgesetzt |
| A12 | VDI 4655 als Validierungsanker | kein Anker in Repository und CI; lokale Probe nur nach K8 | Empfehlung vorausgesetzt |

**Neue Fragen dieses Papiers:**

| Nr. | Frage | Empfehlung | Entscheid |
|---|---|---|---|
| **ZU1** | Speicherauslegung als Überlagerung im Zapfprofil oder als eigener Dialog? | **Überlagerung „Auslegung"** wie im Mockup; die Kernklassen `TwwSpeicherauslegung` und `Summenlinie` sind so geschnitten, dass ein späterer eigener Dialog (etwa aus dem Pufferspeicher) sie ohne Änderung ruft | nach Empfehlung, 23.09.2026 (N1) |
| **ZU2** | Pufferspeicher-Auslegung aus dem Wärmespeicher-Tool mitnehmen? | **Nein, Folgeauftrag** mit eigenem Konzept nach Z2; er braucht Taktung, Sperrzeiten und Abtauung aus der Wärmepumpe, nicht das Zapfprofil | nach Empfehlung, 23.09.2026 (N1) |
| **ZU3** | Reihenfolge Z-Stufen gegenüber iU11 | Z0–Z3 unabhängig von iU11; Z4 nach iU11 oder mit vorgezogener Bedarfsprofil-Hülle (A11); keine Stufe wartet auf den iOS-Lauf | nach Empfehlung, 23.09.2026 (N1) |
| **ZU4** | Wie wählt der Anwender den Weg? | sichtbare **Optionsgruppe** im Bedarfsprofil-Dialog; das OK des Zapfprofils setzt sie, der Anwender kann zurückschalten, die Zonen bleiben | nach Empfehlung, 23.09.2026 (N1) |
| **ZU5** | Netzverlustverteilung F2 bei Generator-Zirkulation (Mockup A4: Brauchwasseranteil ausnehmen) | **F2 unverändert lassen**: `Netzverluste` sind eine Projektgröße (Verteilnetz), die Zirkulation liegt im Gebäude; Doppelzählung ist nur ein Eingabefehler, dafür ein Hinweis im Dialog, wenn beide gesetzt sind. Die Stelle `SimulationKanaele.cs:686` bleibt unberührt | nach Empfehlung, 23.09.2026 (N1) |
| **ZU6** | Ort des Knopfs „Zapfprofil erzeugen…" | Aktionsschlitz der `SpeichernLeiste`; nicht in der Leiste „Simulation · monatlicher Verlauf", die sonst zum zweiten Fuß würde | nach Empfehlung, 23.09.2026 (N1) |
| **ZU7** | Deckt ein Referenzprojekt den Generatorweg ab? | bis Z4 **nein** — der Generator wird in Kern-Tests auf einer Projektkopie der Testdatenbank geprüft; in Z5 ein Referenzprojekt umstellen und die vierte Einfrierregel einführen (3.4) | nach Empfehlung, 23.09.2026 (N1) |
| **ZU8** | Bitgleichheit Windows/iOS | ganzzahliger Zufall, Normalverteilung ohne transzendente Funktionen, gerundete Einmalwerte, feste Summationsfolge (4.2, 4.4); plattformübergreifend zusätzlich Vergleich mit Toleranz | nach Empfehlung, 23.09.2026 (N1) |
| **ZU9** | Neue Tabellen auf iOS | über einen neuen Seed; ältere Datenbank ohne Tabellen rechnet den Bestandsweg, der Knopf ist benannt gesperrt | nach Empfehlung, 23.09.2026 (N1) |
| **ZU10** | Zapfprofil ohne gespeichertes Projekt (Gebäudekatalog aus Verwaltung oder Assistent vor dem Speichern) | Knopf nur mit gespeichertem Projekt; im Assistenten erst nach dem Speichern des Projekts | nach Empfehlung, 23.09.2026 (N1) |
| **ZU11** | Wache für lokale Normdaten | ja, als Fall in `RepositoryOrdnungWacheTests` und Posten der Auslieferungsvorlage (Kapitel 6) | nach Empfehlung, 23.09.2026 (N1) |
| **ZU12** | Projektkopie der Nutzungsart oder Katalogverweis? | **Katalogverweis auf unveränderliche Versionen** (3.2): kein Kopieren je Projekt, keine rückwirkende Änderung; Umstellen auf eine neue Version nur ausdrücklich je Zone | nach Empfehlung, 23.09.2026 (N1) |
| **ZU13** | Topologie je Zone oder je Gebäude? | je Zone wie im Konzept 2.2; die Auslegung rechnet je Topologiegruppe (4.5) | nach Empfehlung, 23.09.2026 (N1) |
| **ZU14** | Wie kommt der Auslieferungskatalog in Bestandsinstallationen? | Katalogpaket außerhalb des Repositoriums, eingespielt von der Auslieferungsvorlage (neue Installation) bzw. über einen Katalogimport in der Verwaltung (Z4); nie über den Schemaschritt | nach Empfehlung, 23.09.2026 (N1) |
| **ZU15** | Nutzung der VDI-6002-Kopien in der Ablage des Anwenders, deren Exemplare den Lizenzstempel einer Universität tragen? | **eigene Lizenz prüfen oder beschaffen**; bis dahin bleiben die daraus extrahierten Tabellen lokal (Kapitel 6, „Lokale Testdaten") und werden nicht weitergegeben — nicht an Dritte, nicht ins Repository, nicht in Testdatenbank, CI oder Auslieferung | nach Empfehlung, 23.09.2026 (N1) |
| **ZU16** | Ersetzt `--katalogpaket` auch die Zeilen mit `Status = 'AUSLIEFERUNG'`, die die Quelle schon führt? | **ja** — das Paket ist die Quelle der Wahrheit für den Auslieferungskatalog; so ist das Werkzeug gebaut (N2 (j)) | offen |
| **ZU17** | Der Projektimport ordnet eine namensgleiche `EIGEN`-Zeile (gleicher Bezeichner und Katalogversion) mit anderem Inhalt ohne Inhaltsvergleich der Zielzeile zu — soll er vergleichen? | **ja, in Z1:** Inhaltsvergleich über die Wertgruppen; bei Abweichung Mitnahme als neue Version mit Zusatz im Bezeichner, nie stilles Umhängen | offen |
| **ZU18** | Eine oder mehrere Testklassen (noch aufzuspüren, N3 (d)), die die Repo-Testdatenbank direkt öffnen (danach liegen `-shm`/`-wal` daneben), auf eine Arbeitskopie oder `immutable` umstellen? | **ja**, als kleiner Folgeposten außerhalb der Z-Stufen | offen |

---

## 10. Abgrenzung

**Dieses Papier behandelt nicht:**

- **Den Port des Wärmespeicher-Tools.** Das Streamlit-Tool bleibt ein Werkzeug der Ablage; übernommen
  werden Verfahren und Bedienmuster, kein Code und keine Daten.
- **Die Pufferspeicher-Auslegung** (Abtauung, Taktung, Sperrzeit, Betriebssimulation) — Folgeauftrag
  nach ZU2.
- **Die Ablösung des Bestandswegs.** Zwölf Monatswerte mal Wochenprofil bleiben Vorgabe der Weiche;
  `Tab_Brauchwasser*`, `ProfilBedarf` und die Dialoge des Bestands bleiben unverändert. Über eine
  Abkündigung wird nach Z5 entschieden (K6); über die Digitalisate im Bestandskatalog nach K8.
- **Eine Viertelstunden- oder Minutenreihe in der Bilanz** (A7) und eine Änderung der Wärmepumpe.
- **Speicherverluste in der Bilanz** — sie bleiben beim Speicher; in der Summenlinie erscheinen sie
  nur als Verlustterm des Nachweises (4.5).
- **Den Feiertags- und Ferienkalender je Bundesland** — erst nach A6.
- **Die Methodik selbst** — sie steht im [Konzept](Konzept_TWW-Zapfprofile_WP-Plan_1.md); dieses Papier
  sagt nur, wo sie im Quelltext andockt.

---

## 11. Nachträge

Nachträge halten Entscheide des Anwenders und Umsetzungsbefunde einer Stufe fest, die nach der
Fassung 2 anfallen. Ein Nachtrag wird nie umgeschrieben; spätere Entscheide und Befunde kommen als
N2, N3 … hinzu. Wo ein Nachtrag eine Empfehlung bestätigt, bleibt der Text des Papiers stehen;
Kapitel 9 zeigt den Stand in der Spalte „Entscheid". Ein Befund berichtigt den Hauptteil mit Verweis
„(Nn)".

### N1 (23.09.2026) — offene Punkte nach Empfehlung

**Frage** an den Anwender: die offenen Punkte dieses Umsetzungskonzepts — K1, K8, ZU1 bis ZU14 und die
Lizenzfrage zu den VDI-6002-Kopien in der Ablage des Anwenders.

**Entscheid** (Philipp, im Wortlaut): „nach Empfehlung".

**Entschieden**, jeweils mit der Empfehlung aus Kapitel 9:

- **K1** — A100-Profildateien, Weißdruck-Status und DIN 4708-2/-3 sofort anfragen; auf eine
  Verwertungslizenz zur Mitauslieferung wird verzichtet, Profile stehen nur im Katalog (Kapitel 6).
- **K8** — juristische Prüfung mit Z0 beauftragen; sie umfasst die Kapselung nach Kapitel 6, die lokalen
  Normkopien und die Digitalisate des Bestandskatalogs (1.6).
- **ZU1 bis ZU14** — wie in der zweiten Tabelle von Kapitel 9.
- **ZU15** (in Kapitel 9 neu aufgenommen) — die Exemplare der VDI 6002 in der Ablage des Anwenders tragen
  den Lizenzstempel einer Universität; die eigene Lizenz wird geprüft oder beschafft, bis dahin bleiben
  die daraus extrahierten Tabellen lokal und werden nicht weitergegeben.

**Nicht Gegenstand:** K2–K7 (samt K3a) und A1–A12. Das Papier setzt ihre Empfehlung weiterhin voraus
(Mockup Abschnitt 8); sie gelten nicht als entschieden.

**Folgen:**

| Punkt | Folge | Verantwortlich | Stufe |
|---|---|---|---|
| K1 | Bestellung bzw. Anfrage der A100-Profildateien, des Weißdruck-Status und der DIN 4708-2/-3; der Verzicht auf die Verwertungslizenz zur Mitauslieferung ist bestätigt | Anwender | vor Z0 angestoßen; wirkt in Z2 (A100- und DIN-4708-Profil, ohne sie der Konstruktor) |
| K8 | Beauftragung der juristischen Prüfung; Gegenstand wie die Empfehlung (Kapselung, lokale Normkopien, Digitalisate des Bestandskatalogs) | Anwender | mit Z0 |
| ZU15 | Lizenzprüfung der VDI-6002-Kopien, gegebenenfalls eigene Lizenz beschaffen; bis zum Ergebnis keine Weitergabe der extrahierten Tabellen | Anwender | sofort, unabhängig von den Stufen |
| ZU11 | Wache für lokale Normdaten (`Normzahlen_stehen_im_gitignore` in `RepositoryOrdnungWacheTests`) und Posten der Auslieferungsvorlage (Kapitel 6) | Agent der Stufe Z0 | Z0 (Anhang A, P1 und P10) |
| A9, A10 | Die Posten „Bereinigung der Zahlenteile der Grundlagenpapiere" (A9) und „Konzept V2" (A10) bleiben in Z0, vorbehaltlich K8; ohne Ergebnis von K8 ruhen sie | Agent der Stufe Z0 nach dem Ergebnis von K8 | Z0 (Anhang A, P14) |
| ZU1–ZU14 | wirken so, wie das Papier sie voraussetzt; keine Textänderung nötig | — | Z0–Z5 laut Kapitel 7 |

### N2 (23.09.2026) — Umsetzungsbefunde Z0

**Anlass.** Die Stufe Z0 ist auf dem Zweig `z0` umgesetzt (Anhang A, P1–P13; Commits `6628e386` bis
`4ddebed9`, dazu das Quellendossier; Protokoll
[`2026-09-23_Z0_Grundlagen_und_Schema.md`](../ueberholt/Protokolle/Zapfprofilgenerator/2026-09-23_Z0_Grundlagen_und_Schema.md)).
Dieser Nachtrag hält fest, wo die Umsetzung vom Papier abweicht oder es genauer fasst; der Hauptteil
ist an den betroffenen Stellen mit Verweis „(N2)" berichtigt. Er enthält **keinen Entscheid** des
Anwenders; die neuen Fragen ZU16–ZU18 stehen mit Empfehlung in Kapitel 9 und sind offen.

**Befunde und Abweichungen:**

- **(a) Schrittnummer.** T1 ist Schritt 102; Schritt 101 gehört der Wirtschaftlichkeit (leere
  `KWKG_Anlagenart` wird NULL). Die Nummer ist eine datierte Momentaufnahme, keine Regel (3.2).
- **(b) IDs.** Alle zehn `ID`-Spalten sind `INTEGER PRIMARY KEY AUTOINCREMENT` (Muster
  `WechselrichterSchema`): Eine gelöschte ID wird nie wieder vergeben — Voraussetzung der
  unveränderlichen Katalogversionen (3.1).
- **(c) `Tab_TwwProjekt.Realisierungen_Auslegung`** ist `INTEGER` ohne DDL-Vorgabe, NULL = Vorgabe
  nach 4.4, CHECK (≥ 1); 3.1 nannte NOT NULL mit Vorgabe. `Realisierungen` trägt ebenfalls
  CHECK (≥ 1).
- **(d) Festlegungen, wo 3.1 offen war.** UNIQUE (`Bezeichner`, `Katalogversion`) am Bedarfstag
  (natürlicher Schlüssel des Projektimports); NOT NULL an `Minute_Beginn`, `Dauer_min` und
  `Reihenfolge` der Ereignisse; CHECK 0–366 an den acht Ferienspalten der Zone; vier Indizes auf
  Kindspalten (`TwwSchema.Indizes`: Zone → Projekt, Zone → Nutzungsart, Wohnungstyp → Zone,
  Ereignis → Bedarfstag).
- **(e) `.gitignore`.** Die Zeilen lauten `Referenzlaeufe/Normzahlen/*` und
  `!Referenzlaeufe/Normzahlen/LIESMICH.md`; Kapitel 6 nannte `Referenzlaeufe/Normzahlen/`. Das
  LIESMICH der obersten Ebene bleibt versioniert; die Wache prüft auch Unterordner und fremde
  Endungen.
- **(f) `TwwNutzungsartCtrl`.** `TagesgangSpeichern` schreibt vier Tagesgänge und die Wochenfaktoren
  in einem Vorgang; ein gesperrter Satz (ReadOnly oder von einer Zone oder einer anderen Nutzungsart
  benutzt) entsteht als neue Zeile, der eigene Satz einer freien Nutzungsart gilt nicht als gesperrt.
  Ändern und „Speichern unter" führen die Provenienz je Wertgruppe nach — Bedarf samt Bandbreite,
  Bezugstemperaturen und Bilanzgrenze; Jahresgang samt Kalenderart und Ferienfaktor; Wochengang —:
  Eine geänderte Gruppe bekommt eine neue Katalogversion und `EIGENKONSTRUKTION` mit neutraler Quelle.
  Neu und Ändern lehnen ungültige Raster benannt ab (`RasterUngueltig`: Werte endlich und nicht
  negativ, Wochenfaktoren und Tagesgänge mit Summe 1, Monatsfaktoren mit Mittel 1). Bedarfstage und
  Parameter haben in Z0 keinen Schreibweg.
- **(g) Katalogpflege.** `KatalogDefinition` hat die Schalter `VerwendungSperrt`, `ImDublettendialog`
  (für die drei Tww-Kataloge `false`) und `SchluesselZusatzSpalten` (Katalogversion).
  `KatalogBereinigung.SatzLoeschen` löscht Blöcke und Kopf in einem `DbVorgang` mit Rollback; der
  Dublettendialog sperrt benutzte Zeilen; der Scan gruppiert nach Bezeichner und Katalogversion —
  zwei Versionen eines Namens sind keine Dublette.
- **(h) Fiktiver Testkatalog.** `Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py` spielt 19 Zeilen
  wiederholbar ein (Status `EIGEN`, Herkunftsart `FIKTIV`, Katalogversion `TEST-1`); die
  Kaltwasser-Bezugstemperatur ist so gewählt, dass sie mit keinem normativen Wert zusammenfällt.
- **(i) Projektimport (`.wpx`), Befund zu 3.2.** Eine am Ziel fehlende Katalogzeile wird über den
  natürlichen Schlüssel (`Bezeichner`, `Katalogversion`; beim DIN-4708-Wert `Art`, `Schluessel`,
  `Katalogversion`) mit Status `IMPORT` mitgenommen, ein Tagesgangsatz nur, wenn eine mitgenommene
  Nutzungsart ihn braucht; Tagesgänge und Ereignisse reisen im Paketordner `catalogchildren/`,
  `Beleg` und `Freigabe` reisen nicht mit. Fehlt eine benötigte Zeile im Paket, lehnt der Import
  benannt ab und ändert nichts. Die Projektkopie nimmt `Tab_TwwProjekt`, `Tab_TwwZone` und
  `Tab_TwwWohnungstyp` mit.
- **(j) Auslieferungsvorlage, Schritt 3c.** Die Tww-Regel gilt unabhängig von `--kataloge`: Es bleibt
  nur `Status = 'AUSLIEFERUNG'` ohne Herkunftsart `FIKTIV` oder `IMPORT`, auch `EIGEN` fällt;
  Fremdschlüssel eingeschaltet; verbleibende Auslieferungszeilen bekommen `ReadOnly = 1`. Prüfposten:
  kein `IMPORT`, kein `EIGEN`, keine Waisen, kein `FIKTIV`, `ReadOnly = 1`, und der Posten ZU11
  prüft, dass keine Eingabe des Laufs unter `Referenzlaeufe/Normzahlen/` liegt. Option
  `--katalogpaket <ordner>` im Format aus Kapitel 6 (b), außerhalb des Repositoriums, ersetzt den
  Tww-Katalog der Quelle in einer Transaktion; Rückgabe 2 bei falschem Aufruf, 5 bei fachlichem
  Fehler. Ein Beispielpaket, das `IMPORT`-Zeilen mitbringt, nennt der Bericht.
- **(k) Wache `TwwKatalogWacheTests`.** Keine Zeile `AUSLIEFERUNG`; jede Zeile `EIGEN` und in jeder
  Herkunftsspalte `FIKTIV` und in jeder Quellenspalte „Testkatalog (fiktiv)"; Katalogversion nie
  leer; das Skript ist wiederholbar (Aufruf über `py`, Frist 120 s).
- **(l) Vorlagenprobe.** Die Werkzeugprobe P6 der Auslieferungsvorlage zählt 129 STRICT-Tabellen.
- **(m) Vorbestehend.** Nach Testläufen liegen `-shm`/`-wal` neben der Testdatenbank (gitignoriert);
  eine Testklasse öffnet die Repo-Testdatenbank direkt und ist noch aufzuspüren (ZU18).

**Neue Fragen** (Kapitel 9, Entscheid offen):

- **ZU16** — Ersetzt `--katalogpaket` auch vorhandene `AUSLIEFERUNG`-Zeilen der Quelle? Empfehlung:
  ja, das Paket ist die Quelle der Wahrheit.
- **ZU17** — Namensgleiche `EIGEN`-Zeilen mit anderem Inhalt werden beim Import ohne Inhaltsvergleich
  der Zielzeile zugeordnet. Empfehlung: Inhaltsvergleich über die Wertgruppen in Z1, bei Abweichung
  Mitnahme als neue Version mit Zusatz.
- **ZU18** — Testklassen, die die Repo-Testdatenbank direkt öffnen, auf Arbeitskopie oder `immutable`
  umstellen? Empfehlung: ja, kleiner Folgeposten.

**Folgen:**

| Punkt | Folge | Verantwortlich | Stufe |
|---|---|---|---|
| ZU16 | bis zum Entscheid gilt die gebaute Lesart (das Paket ersetzt) | Anwender | vor dem ersten Katalogpaket |
| ZU17 | bei Entscheid nach Empfehlung: Inhaltsvergleich im Projektimport | Agent der Stufe Z1 | Z1 |
| ZU18 | bei Entscheid nach Empfehlung: Testklasse aufspüren und umstellen | Agent eines Folgepostens | unabhängig von den Stufen |
| P14 | ruht bis zum Ergebnis von K8 (N1) | Agent nach K8 | Z0, eigener Schritt |

### N3 (23.09.2026) — Nachbesserung der Abschlusspapiere Z0

**Anlass.** Die Prüfung der Abschlusspapiere der Stufe Z0 (N2, Quellendossier, Setup-Konzept 6.1,
Protokoll) hat Stellen gefunden, die den gebauten Stand ungenau wiedergeben. N2 bleibt unverändert;
dieser Nachtrag berichtigt ihn, der Hauptteil ist mit Verweis „(N3)" nachgezogen. Er enthält
**keinen Entscheid** des Anwenders.

**Befunde:**

- **(a) Provenienz nach einer Änderung (zu N2 (f)).** `TwwNutzungsartCtrl` setzt eine geänderte
  Wertgruppe nur dann auf `EIGENKONSTRUKTION` mit neutraler Quelle „Eigenkonstruktion", wenn der
  Entwurf die Provenienz der Vorlage unverändert mitbringt (Quelle, Ausgabe und Herkunftsart gleich);
  eine im Entwurf ausdrücklich gesetzte andere Provenienz bleibt mit der neuen Katalogversion stehen.
  Regelquelle ist das Quellendossier, § 2 Nr. 4.
- **(b) Paketformat (zu Kapitel 6 (b) und N2 (j)).** `Status` und `ReadOnly` stehen nur in den fünf
  Kopftabellen; `Tab_TwwTagesgang_STAMM` und `Tab_TwwBedarfstagEreignis_STAMM` führen keine dieser
  Spalten, eine solche Spalte in ihrer Datei bricht mit Rückgabe 5 ab. Das Paket vergibt die IDs der
  Köpfe selbst; ein leeres Feld ist NULL.
- **(c) Erweiterung der `WikiProduktdatenWacheTests` (Kapitel 6 (e)) nicht in Z0.** Die Wache nennt
  die Tww-Kataloge nicht; `TwwKatalogWacheTests` prüft nur die Quellentexte des Testkatalogs, nicht
  Bezeichner oder andere Katalogtexte. Die Erweiterung gehört zur Abnahme der Stufe Z4 (Kapitel 7),
  zusammen mit den `ZPG_`/`ZPGK_`-Ressourcen.
- **(d) ZU18 (zu N2 (m)).** Wie viele Testklassen die Repo-Testdatenbank direkt öffnen, ist nicht
  festgestellt: eine oder mehrere, noch aufzuspüren.

**Folgen:**

| Punkt | Folge | Verantwortlich | Stufe |
|---|---|---|---|
| (c) | `WikiProduktdatenWacheTests` auf die Katalogtexte der `Tab_Tww*_STAMM` (ohne `Beleg`) erweitern | Agent der Stufe Z4 | Z4 |
| (d) | bei Entscheid nach Empfehlung zu ZU18: jede gefundene Testklasse umstellen | Agent eines Folgepostens | unabhängig von den Stufen |

---

## Anhang A — Auftragsblatt Stufe Z0 (Grundlagen und Schema)

Dieses Blatt ist unmittelbar an eine Agentensitzung beauftragbar. Es fasst die Zeile Z0 aus Kapitel 7
mit dem Stand aus N1 zusammen; die Einzelheiten stehen in den genannten Abschnitten und werden hier nicht
wiederholt.

**Ziel.** Das Datenmodell des Zapfprofilgenerators steht in Kern und Testdatenbank, lesbar über
Controller, geschützt durch Wachen — ohne dass sich ein Rechenergebnis ändert. Kein Referenzprojekt setzt
die Weiche; die Basis unter `Referenzlaeufe/` bleibt unberührt (3.4).

**Vorbedingungen.**

- K1 und K8 sind durch den Anwender angestoßen (N1).
- K2, K7, A1 und A2 gelten als Empfehlung vorausgesetzt, ZU12 ist entschieden (Kapitel 9).
- Schemanummer nachgemessen: T1 wird der nächste freie Schritt nach `SchemaStand.Zielversion` in
  `EPOS.Kern/Allgemein/Update/SchemaStand.cs`; kein anderer Auftrag hebt die Zielversion parallel (3.2,
  Kapitel 7 „Agentenzuschnitt").
- Eigener Worktree und Zweig; `AGENT_LAEUFT` vor jeder Arbeit im Hauptbaum anlegen, nach der Abnahme
  löschen.
- Modellwahl nach `CLAUDE.md`: Agent mit `model: opus` für Implementierung und Tests, `model: sonnet`
  nur für Suchen; das Modell wird ausdrücklich gesetzt.
- Git LFS eingerichtet: die Testdatenbank liegt als Datei vor, nicht als Zeiger.

**Umfang.**

| Nr. | Posten | Abschnitt |
|---|---|---|
| P1 | Wache `Normzahlen_stehen_im_gitignore` in `EPOS.Kern.Tests/RepositoryOrdnungWacheTests.cs` (ZU11), vor jeder neuen Datei unter `Referenzlaeufe/Normzahlen/` | 6, N1 |
| P2 | `EPOS.Kern/Allgemein/Update/TwwSchema.cs` mit der DDL der zehn Tabellen aus 3.1: `Tab_TwwNutzungsart_STAMM`, `Tab_TwwTagesgangsatz_STAMM`, `Tab_TwwTagesgang_STAMM`, `Tab_TwwBedarfstag_STAMM`, `Tab_TwwBedarfstagEreignis_STAMM`, `Tab_TwwParameter_STAMM`, `Tab_TwwDin4708Wert_STAMM`, `Tab_TwwZone`, `Tab_TwwWohnungstyp`, `Tab_TwwProjekt`; Muster `WechselrichterSchema.cs` | 3.1, 3.2 |
| P3 | Schemaschritt T1 in `WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs`: Konstante, Methode, Eintrag in `SCHRITTE_SQLITE`, **dann** `SchemaStand.Zielversion` anheben; nur `SqliteDdl`/`SqliteTabelleVorhanden` | 3.2 |
| P4 | `EPOS.Kern.Tests/TestDatenbank.cs` und `Werkzeuge/Testdatenbankschema/Program.cs` nachziehen; Testdatenbank migrieren, fiktiven Testkatalog einspielen (`Status = 'EIGEN'`, Herkunftsart `FIKTIV`, runde Werte), mit aktivem LFS-Filter committen; Schemastand in `Referenzlaeufe/LIESMICH.md` nachtragen | 3.2, 6 (b) |
| P5 | `Parametersatz` aus `Tab_TwwParameter_STAMM` der aktuellen Katalogversion; keine Normkonstante im Quelltext, Tests mit erfundenen Parametern | 2.1, 3.3, 6 (a) |
| P6 | `EPOS.Kern/Controller/ZapfprofilCtrl.cs` **lesend**: `Weg`, `Katalog`, `Parameter`, `Lies`, `Verfuegbar`; `Speichern` und `Eingang` folgen in Z1 | 3.3 |
| P7 | `EPOS.Kern/Controller/TwwNutzungsartCtrl.cs` mit Sperre benutzter und ReadOnly-Zeilen, „Speichern unter" als neue Zeile | 3.2, 3.3 |
| P8 | `KatalogRegistry` um `TWW_NUTZUNGSART`, `TWW_TAGESGANGSATZ`, `TWW_BEDARFSTAG`; `Katalogfilterprofil.FuerTwwNutzungsart` | 3.2 |
| P9 | Kopierstellen: `ProjektDuplizierenCtrl` (abhängige `Tab_TwwWohnungstyp` über `ID_Zone`) und `ProjektExportImportCtrl` (`KATALOG_SPALTE_ZU_TABELLE`, `KATALOG_NATURALKEY`); Mitnahme oder benannte Ablehnung einer fehlenden Katalogzeile nachmessen; je ein Test | 3.2 |
| P10 | `Werkzeuge/Auslieferungsvorlage`: Tww-Regel unabhängig von `--kataloge`, Fremdschlüssel eingeschaltet, Prüfberichtposten (keine Zeile mit `Status = 'IMPORT'`, keine verwaiste Zeile), Option Katalogpaket von außerhalb des Repositoriums, Posten lokale Normdaten (ZU11) | 3.2, 6 (b), (c) |
| P11 | Wache `TwwKatalogWacheTests`: keine Zeile mit `Status = 'AUSLIEFERUNG'` in der Testdatenbank | 6 (c) |
| P12 | `SqlDialektPruefer` nach jedem neuen SQL-Text | 3.2 |
| P13 | Quellendossier nach der Zeile Z0 in Kapitel 7; Zuschnitt vor Beginn mit der Orchestrierung klären, Quellenangaben nur nach Regel (e) | 7, 6 (e) |
| P14 | vorbehaltlich K8 (N1): Konzept V2 (A10) samt `git mv` von V1.2 nach `Dokumentation/ueberholt/`; Bereinigung der Zahlenteile der Grundlagenpapiere (A9); bei K8 „ersetzen" der Ersatz der vier Digitalisate als eigener Schritt mit Einfrierprüfung 1007/1045/1046 und Begründung in `Referenzlaeufe/LIESMICH.md` | 1.6, 3.4, 9 |

**Reihenfolge.** P1 zuerst; dann P2 und P3 zusammen, danach P4; dann P5, P6, P7 und P8; dann P9; dann
P10 und P11; P13 zum Schluss. P12 läuft nach jedem neuen SQL-Text mit. P14 ist kein Teil der Abnahme von
Z0: er folgt als eigener Schritt, sobald das Ergebnis von K8 vorliegt. Jeder zusammenhängende Schritt
wird sofort auf dem Zweig committet.

**Nicht-Umfang.** Keine Rechenklassen (S1–S6, Weiche, Summenlinie, Speicherauslegung, Generator — ab
Z1); keine Oberfläche (kein Dialog, kein Knopf, kein Menüeintrag, keine Ressourcen, kein Wiki); kein
Referenzprojekt auf dem Generator und keine neue Einfrierregel (3.4, ZU7); keine Schritte T2 und T3;
keine Auslieferungswerte und keine Normzahl im Repository, in der Testdatenbank oder in der CI
(Kapitel 6); keine neue `Dienste.*`-Schnittstelle (2.5); kein neuer iOS-Seed und kein iOS-Lauf (ZU9).

**Abnahme** (Befehle aus `CLAUDE.md`, „Bauen und prüfen"):

- `dotnet build WP-Plan.Kern.slnf -c Release` grün.
- `dotnet test WP-Plan.Kern.slnf -c Release --no-build -- xUnit.ParallelizeTestCollections=false
  xUnit.MaxParallelThreads=2` grün, darin `TestdatenbankSchemastandWacheTests`, `TwwKatalogWacheTests`,
  `RepositoryOrdnungWacheTests` mit dem neuen Fall, `DokumentationLinkWacheTests` und je ein Test für
  `ProjektDuplizierenCtrl` und `ProjektExportImportCtrl`.
- `EPOS.Referenzlauf` ausdrücklich bauen, dann `lauf` für 1030, 1007, 1017, 1045 und 1046 und `vergleich`
  gegen die aktuelle Basis unter `Referenzlaeufe/` — grün innerhalb der Toleranz; eine Byte-Abweichung
  ist ein zu erklärender Befund (Kapitel 7).
- `SqlDialektPruefer` gegen `Referenzlaeufe/Kenndaten_Test.sqlite` grün.
- Windows-Schale auf Linux gebaut (`-p:EnableWindowsTargeting=true`), 0 Fehler — der Schemaschritt liegt
  in der Schale.
- Testdatenbank über LFS committet; Schemastand in `Referenzlaeufe/LIESMICH.md` nachgezogen, falls die
  Testdatenbank migriert ist.
- Statuszeile in `Status_iOS_Migration.md` und Protokoll unter `Dokumentation/ueberholt/Protokolle/`
  nach dem Gate (Reihenfolge nach `CLAUDE.md`); dieses Papier bekommt keinen Statusvermerk.

**Aufwand.** 7–10 PT (Annahme, ±30 %; Herleitung in Kapitel 7: Grundlage P0 ohne Access-Hygiene, dazu
Parameter-, Bedarfstag- und DIN-4708-Tabellen, Provenienzgruppen, Kopierstellen und zwei Wachen).

**Bericht und Übergabe.** Der Agent committet auf seinem Zweig, pusht nicht und löst keinen CI-Lauf aus.
Sein Bericht enthält Befund und Ergebnis, keine Dateiabzüge: Zweig und Commits, gemessene Schemanummer,
angelegte Tabellen, neue Tests und Testergebnis, Referenzlauf-Vergleich samt Byte-Information, Ergebnis
des `SqlDialektPruefer`, Build der Windows-Schale, den Befund zur Mitnahme fehlender Katalogzeilen beim
Projektimport (3.2), die an K8 hängenden offenen Posten und jede Abweichung von diesem Blatt mit Grund.
Die Orchestrierung nimmt ab, führt zusammen und löscht `AGENT_LAEUFT`.

**Umsetzungsstand und Abweichungen:** N2 und N3 (Kapitel 11).

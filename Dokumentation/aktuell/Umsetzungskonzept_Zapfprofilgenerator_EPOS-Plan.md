# Umsetzungskonzept: Zapfprofilgenerator und Brauchwasserauslegung in EPOS-Plan

**Stand 2026-09-24 — Fassung 2 — Umsetzungsentwurf, zur Abnahme durch den Anwender — Nachträge N1–N29 (Kapitel 11)**

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
| Eingang | `Zapfprofileingang.cs`; die Zone ist `ZonenStand` aus `ZapfprofilStand.cs`, ein `Zoneneingang.cs` entfällt (N7) | Zone: Nutzungsart-ID, Bezugsmenge, Niveau, Topologie, Wohnungstabelle und **nullbare** Überschreibungen (`null` = Vorgabe); Eingang: Zonen, Kalender (`WochentagJan1`, `bool[365] We`), Gebäudegrößen (Zirkulation, Ladeleistung), `RechenwegJahresreihe`, `Seed`, `Realisierungen`, `Parametersatz` |
| Arbeitsstand | `ZapfprofilStand.cs` | `internal sealed record ZapfprofilStand(BrauchwasserWeg Weg, IReadOnlyList<ZonenStand> Zonen, ProjektStand Projekt)` — die Übergabeform zwischen `ZapfprofilCtrl` und Hülle, ohne Oberflächenbezug |
| S1 | `Mengengeruest.cs` | `static Mengenergebnis JahresenergieKwh(ZonenStand z, Nutzungsart n, Zonentemperaturen t, Parametersatz ps, IReadOnlyDictionary<string, double> belegungJeRaumzahl, Herkunftsprotokoll p, ICollection<ZapfHinweis> hinweise)` — Jahresenergie, Bezugsmenge, f_θ und Zonenfläche (N7); `static double Temperaturfaktor(...)`; `static Messwert MesswertAus(ZonenStand z, Zonentemperaturen t, Herkunftsprotokoll p)`; `static Kalibrierergebnis Kalibrieren(Messwert m, double zapfungKwh, double zirkulationKwh, string zone)` |
| S2 Kalender | `Zapfkalender.cs` | `enum ZapfTagtyp { Werktag = 1, Samstag = 2, SonnFeiertag = 3, Ruhetag = 4 }`; `static ZapfTagtyp[] Bilden(int wochentagJan1, bool[] we, IReadOnlyList<Ferienfenster> ferien)` — 365 Einträge; `static IReadOnlyList<Ferienfenster> AusJahrestagen(int? beginn, int? ende)` — null, ein oder zwei Fenster (N7) |
| S2 Formvektor | `Formvektor.cs` | `static Zeitstruktur Bilden(ZonenStand z, Nutzungsart n, Tagesgangsatz satz, Parametersatz ps, Herkunftsprotokoll p, ICollection<ZapfHinweis> hinweise)` — normierte Monate, Woche, Tagesgänge (N7); `static double[] Tagesmengen(double jahresKwh, Zeitstruktur s, ZapfTagtyp[] kalender, int wochentagJan1, double[] kaltwasserfaktor, string zone)`; `static double[] Stundenreihe(double[] tagesmengen, Zeitstruktur s, ZapfTagtyp[] kalender)` — 8760, nur für die Bilanz; `static Wochenreihe Wochenreihe(double[] tagesmengen, Nutzungsart n, ZapfTagtyp[] kalender, int ersterTag)` — 168 h, nur für die Auslegung |
| S2 Kaltwasser | `Kaltwassergang.cs` | `static double[] Monatswerte(double mittelC, double amplitudeK, int monatMaximum)` — zwölf Werte, einmal gerechnet und auf neun Stellen gerundet (4.2); `static double[] Monatsfaktoren(double zapfC, double[] monatswerteC, double mittelC, string zone)`, dazu `Monatsfaktoren(Zonentemperaturen t, string zone)` (N7) |
| S3 | `ZapfZufall.cs`, `Zapfkategorie.cs`, `Zapfereignisgenerator.cs`, `Zapfensemble.cs` | portabler Zufall; Kategorien; Minutenreihe je Zone aus `n_E` Einheiten; Ensemble mit Perzentilen je Topologie (4.4) |
| S4 | `Bedarfstag.cs`, `Wochenreihe.cs`, `Summenlinie.cs`, `Din4708Kennzahl.cs`, `TwwSpeicherauslegung.cs`, `Grossanlage.cs`, `Auslegungsergebnis.cs` | Dreiergruppe, Verfahrensvergleich, Großanlagenerkennung (4.5–4.7) |
| S5 | `Zirkulationskanal.cs` | zweigeteilt (N7): `static Zirkulationsansatz Ansetzen(ProjektStand p, IReadOnlyList<Zonenanteil> zonen, Parametersatz ps, Herkunftsprotokoll prot, ICollection<ZapfHinweis> hinweise)` — Methode, Leistung, α, Jahresverlust und Zonenanteile vor der Kalibrierung; `static Bilanzreihe Reihe(double jahresverlustKwh, double laufzeitH, double[] fenster)` — 8760, eigene Teilreihe, Fenster aus `Laufzeitfenster(laufzeitH, tagesmitteH)` |
| S6 | `Kalibrierung.cs` | Faktor, Dauerlinienvergleich, Bericht synthetisch gegen gemessen |
| Fassade | `ZapfprofilRechner.cs`, `ZapfprofilErgebnis.cs`, `Bilanzreihe.cs` | `static ZapfprofilErgebnis Rechnen(Zapfprofileingang e, IReadOnlyList<Nutzungsart> katalog)`; `sealed class Bilanzreihe` mit `IReadOnlyList<double> StundenKwh`, `KopieStundenKwh()`, Monats- und Jahressumme (unveränderlich, N7); Ergebnis: `Bilanzreihe Zapfung`, `Bilanzreihe Zirkulation`, `JeZone`, Kennzahlen mit Einheit im Namen (`JahresbedarfZapfungKwh`, `JahresverlustZirkulationKwh`, `GroessterStundenwertKw`), `Herkunftsprotokoll`, `Hinweise` |
| Import | `Normformvektorleser.cs`, `Typtagzuordnung.cs` (Z4b) | liest anwendereigene VDI-4655-Typtage aus einem `Stream` (Muster `TryPaketLeser.AusStrom`, `EPOS.Kern/Allgemein/Import/TryPaketLeser.cs:362`); ordnet sie dem Kalender zu (4.2) |

Die Auslegung hat eine eigene Fassade, damit sie strukturell keinen Weg zur Bilanzreihe hat:

```csharp
internal static class ZapfprofilAuslegung
{
    // bekommt Eingang und Katalog; Bedarfstag und Wochenreihe bildet sie je Topologiegruppe selbst
    // — ausdrücklich keine Bilanzreihe (N10); das Ensemble kommt mit Z3
    internal static Auslegungsergebnis Rechnen(Zapfprofileingang e,
        IReadOnlyList<Nutzungsart> katalog, Auslegungseingang a);
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
    double[] zirkulation = e.Zirkulation.KopieStundenKwh();
    BhkwPlan.VectorenAddieren(e.Zapfung.KopieStundenKwh(), brauchwasserwerte);
    BhkwPlan.VectorenAddieren(zirkulation, brauchwasserwerte);
    Brauchwasser_Zirkulation_Mwh = Energieeinheit.MWh.AusKWh(e.Zirkulation.JahressummeKwh);
    BhkwPlan.MonatsSumme(brauchwasserwerte, Waermebedarf_Brauchwasser_Monat, mo_anfang, mo_ende);
    BhkwPlan.MonatsSumme(zirkulation, Waermebedarf_Brauchwasser_Zirkulation_Monat,
                         mo_anfang, mo_ende);
    return;
}
```

Die Bilanzreihe gibt ihre Stunden nur lesend heraus; die Weiche übergibt deshalb Kopien
(`KopieStundenKwh`, N7). Der Codeblock gibt die Wirkung wieder: Umgesetzt liest die Weiche den
gespeicherten Stand (`ZapfprofilCtrl.Lies`) und rechnet ihn über `ZapfprofilCtrl.Rechnen` in
`SimulationWaermebedarf.BrauchwasserAusGenerator` — derselben Methode, die die Vorschau ruft (N8).

**Regeln der Weiche:**

- **Exklusiv (A3).** Je Projekt genau ein Weg. Die Bestandszeilen in `Z_Projekt_Brauchwasser` bleiben
  liegen, rechnen aber nicht mit, solange der Generator gewählt ist; der Dialog sagt das (5.2).
- **Vorgabe Bestandsweg.** Ohne Zeile in `Tab_TwwProjekt` oder bei `Weg = 'BESTAND'` läuft der
  heutige Code Zeichen für Zeichen. Damit ändert keine Stufe ein Referenzprojekt.
- **Kein stiller Rückfall.** Fehlt dem Generator eine Eingabe (Zone ohne Bezugsmenge, Nutzungsart
  gelöscht, Parameter fehlt), meldet der Generatorweg (`BrauchwasserAusGenerator`, N8) den Grund über
  `SimulationProtokoll.Aktuell.Warnung` und die Zone trägt 0 — derselbe Ausgang wie der Bestandsweg
  bei Nullprofil, aber mit benannter Zone. Kann der Generator für das Projekt gar nicht rechnen
  (Tww-Tabellen oder Katalogversion fehlen, unerwarteter Fehler), bricht der Lauf benannt ab, und die
  Bedarfsfelder stehen auf 0 (N8).
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
| Formvektor-Summe: jeder Tagesgang Σ = 1, Wochenfaktoren Σ = 1 (Katalog), Warnung ab einer Abweichung über der Warnschwelle (INEKON-Setzung, Parameter), Normierung vor dem Rechnen | exakt nach Normierung | `FormvektorTests.Tagesgang_und_Woche_summieren_zu_eins` (N7) |
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
  und Summen und legt die Werte **im eigenen Datenmodell** `Tab_TwwTyptag_IMPORT` (T3, 3.2; Paketformat
  in N14 (b)) ab — nicht
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
| `Jahresmesswert_Einheit` | INTEGER CHECK (`Jahresmesswert_Einheit` IN (1,2)) | 1 kWh/a, 2 m³/a (Umrechnung 4.1); ein Messwert ohne Einheit wird benannt abgelehnt (N7) |
| `Jahresmesswert_Bilanzgrenze` | INTEGER CHECK (`Jahresmesswert_Bilanzgrenze` IN (1,2,3)) | Kodierung wie im Katalog; ein Volumenmesswert ist immer 1; ein Messwert in kWh/a ohne Grenze wird benannt abgelehnt (N7) |
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
(`IEinstellungen`, Schlüssel `Zapfprofil.Nenninhalte`) mit einer neutralen Vorgabe aus dem Parametersatz
(`Speicherauslegung.Nenninhalt.Liste.{k}`), nicht aus dem Code (N11).

**Später:** `Tab_TwwZapfkategorie_STAMM` (T2 = Schritt 115, umgesetzt in Z3 mit 16 Spalten, N12 (l); Erstfassung: `ID_Nutzungsart`, `Kategorie`, `Volumenstrom_l_min`,
`Dauer_min`, `Anteil`, `Sigma`, Provenienz, `Status`) und der Feiertags-/Ferienkalender
**erst nach Entscheid A6**.

`Tab_TwwTyptag_IMPORT` (T3) ist **umgesetzt** — Schritt 131 mit elf Spalten, Einzelheiten in
**N14 (c)**; nie `ReadOnly`, nie in der Auslieferungsvorlage — samt der drei Projektspalten an
`Tab_TwwProjekt` für die Wahl des Anwenders (Typtagweg, Klimazone, Gebäudeart) im selben
Schritt (N14, Ergänzung).

### 3.2 Schemaschritte

Drei Schritte, jeweils der **nächste freie Schritt nach `SchemaStand.Zielversion`**
(`EPOS.Kern/Allgemein/Update/SchemaStand.cs:341`) — beim Beauftragen nachmessen, weil andere Aufträge
(Wirtschaftlichkeit, Gebäudesimulation) dieselbe Zählung benutzen. Papiernamen:

| Papiername | Stufe | Inhalt |
|---|---|---|
| **T1 — Katalog, Zonen, Projekt** | Z0 | die zehn Tabellen aus 3.1; der Katalog der Testdatenbank nur fiktiv (Kapitel 6) |
| **T2 — Zapfkategorien** | Z3 (Schritt 115, N12 (l)) | `Tab_TwwZapfkategorie_STAMM` |
| **T3 — Typtage** | Z4b (Schritt 131, N14 (a)) | `Tab_TwwTyptag_IMPORT` |

T1 ist im Bestand Schritt 103 (N2 (a), N4), T2 Schritt 115 (N12 (l)), T3 „Typtage" Schritt 131
(N14 (a)) — den Papiernamen T3 trägt dort auch Schritt 124 (die Laufangaben der Auslegung, N11);
gemeint ist bei 124 die Spaltenerweiterung, bei 131 die Tabelle.

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
| `Zapfprofileingang Eingang(int idProjekt, int wochentagJan1, bool[] we)` | Eingang für den Lauf; Überladung mit Arbeitsstand, `Rechnen(idProjekt, stand, wochentagJan1, we)` rechnet ihn mit dem einmal gelesenen Katalog (N8) |
| `ZapfprofilStand Speichern(int idProjekt, ZapfprofilStand stand, DbVorgang vorgang)` | im übergebenen `DbVorgang` (Überladung ohne: eigener Vorgang): Zonen und Wohnungstypen per Upsert, `Tab_TwwProjekt` per Upsert, **`Weg` immer**, auch ohne Zonenänderung; idempotent — ein zweites OK wiederholt nichts; liefert den Stand mit den Ids der Datenbank; Verweise, Gebäude des Projekts und Wertemengen der Projektgrößen vor dem ersten Schreiben geprüft (N8) |
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
| `θ_Speicher` | Speicher-Solltemperatur; `Δθ_Speicher = θ_Speicher − θ_KW,Auslegung` für alle Speichervolumina — eine Temperatur je Topologiegruppe für alle Verfahren (N10) | Projekt, Vorgabe Parameter `W551.Mindesttemperatur` bei Großanlage, im Schnellpfad `A100.Vereinfachung.Speichertemperatur`, sonst `Speicherauslegung.Speichertemperatur_Vorgabe` (N10) |
| `θ_Anzeige` | Temperatur der Literanzeige in Kennzahlen | Einstellung, INEKON-Setzung |

### 4.1 S1 — Mengengerüst

```
Q_a,Zone [kWh/a] = n_Bezug · q_spez(Niveau) · 365 · f_θ
  q_spez: Katalogwert des Niveaus; Experte: Bedarf_Spez (Status Ueberschrieben)
  f_θ = (θ_Zapf − θ̄_KW) / (θ_Bezug − θ_KW,Bezug)      Umrechnung auf die Projekttemperaturen,
                                                     nach A1 verpflichtend; f_θ ≠ 1 -> Status Umgerechnet,
                                                     Faktor im Herkunftsprotokoll
  Wohnen, Bezug Fläche: Q = max(a − b · A_WE ; c) · A_WE · n_WE · f_θ   (Verfahren der DIN V 18599-10,
                        a, b, c Parameter; f_θ mit den Bezugstemperaturen der Nutzungsart, N7)
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
Wert ohne Bezugstemperaturen wird nicht angenommen. Eingaben: Zone (samt Ferien und Fläche des
gebundenen Gebäudes, soweit die Zone keine eigenen trägt, N8), Nutzungsart, Wohnungstabelle,
Parametersatz. Ausgabe: `JahresenergieKwh`, Herkunft je Feld. Vorgabe: Niveau „mittel". Hinweise
(nicht blockierend): Bedarf außerhalb der Bandbreite des Niveaus; Messwert weicht vom Katalogwert mehr
ab als die Rückfrageschwelle (Konzept 2.2, Parameter). Tests:
`MengengeruestTests.Bezugsmenge_mal_Bedarf_ergibt_die_Jahresmenge`,
`…Der_Messwert_skaliert_mit_ausgewiesenem_Faktor`, `…Ueberschreibung_setzt_den_Status`,
`…Die_Flaechenformel_folgt_dem_Verfahren` (erfundene a, b, c; Knick und Untergrenze),
`…Messwert_mit_Zirkulation_zaehlt_nicht_doppelt` (Grenze 2: Zapfung + Zirkulation = Messwert),
`…Der_Volumenmesswert_wird_ueber_die_Temperaturen_umgerechnet`,
`MengengeruestTests.Umrechnung_auf_Bezugstemperatur` (N7; Relation `V_neu = V_Tab · Δθ_Tab / Δθ_neu`, erfundene
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
Ferienfaktor, `…Ein_Wochenende_ohne_Kennzeichen_ist_ein_Werktag`), Kaltwassergang in `ZapfkalenderTests` (N7):
`…Das_Jahresmittel_des_Faktors_ist_eins`,
`…Die_Monatswerte_sind_gerundet`, `FormvektorTests` (Summen, Wochenfaktor Null, Tagesgang Null ergibt 0
mit Hinweis, `…Die_Wochenreihe_entsteht_ohne_Stundenreihe`). Toleranz relativ 1e-12.

**Wochenreihe der Auslegung.** `Formvektor.Wochenreihe` bildet die 168 Stunden der maßgebenden Woche
aus `Tagesmengen` (mit `f_KW,A` statt `f_KW`) mal Tagesgang — über dieselben Bausteine, aber **ohne**
`Stundenreihe` und ohne `ZapfprofilRechner`. Maßgebend ist die Woche mit der größten Summe der
Tagesmengen (4.7).

**VDI-4655-Typtage (Z4b).** Der Import legt die Typtage im eigenen Modell `Tab_TwwTyptag_IMPORT` ab;
die Klasse `Typtagzuordnung` ordnet jedem Kalendertag einen Typtag zu (Jahreszeit aus der
Tagesmitteltemperatur, Wochentag, Bedeckung) und setzt die Tagesmenge nach der Methodik der
Richtlinie: `Q_TT = Q_a · (1/365 + N_Pers · F_TT)` (N_Pers Personen bzw. WE der Zone); ergäbe die
Gleichung für einen Typtag einen negativen Tagesbedarf, wird **sein Faktor auf 0 gesetzt** (Grundlagen 5,
Abschnitt 2.5, Anmerkung zu Gl. (1)–(3); N14 (N2)) — sein Tag trägt dann `Q_a/365` —, danach skaliert die
Reihe auf `Σ Q_d = Q_a`, beides mit Hinweis; `F_TT` stammt aus den eingespielten Daten, nie aus dem Produkt.
Steht die Jahresreihe auf „stochastisch", zieht das Ensemble über **diese** Tagesmengen (N14 (N1)). Vorfragen: `Tab_Solar.Bedeckungsgrad` ist in
der Testdatenbank überall leer, `Tab_Klimadaten.TagTyp_W` nur eine Näherung heiter/bewölkt
(`KlimaImportAblauf.cs:982-995`), `Tab_Klimaregion` ohne TRY-Zone. Die Wetterkopplung ist deshalb ein
eigener Unterpunkt Z4b mit Vorbedingung K3a/K8 (Kapitel 7); die Vorfragen sind in **N14 (d)**
beantwortet (Jahreszeitgrenzen und Bewölkungsschwelle als Kennwerte des eingespielten Pakets, Feiertag
als Sonntag, Samstag als Werktag, kein Urlaubstag).

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

Die Laufzeitstunden liegen zusammenhängend um die Tagesmitte der Zapfung (Festlegung in N7: Schwerpunkt
der Stundensummen der Zapfung in Z1, Beginn `⌊m − t_Lauf/2 + ½⌋`, an den Tagesrand geschoben, eine
gebrochene Laufzeit belegt die letzte Stunde anteilig); `t_Lauf` folgt dem Rahmen
nach DVGW W 551 (Parameter). **Vorgabe** ist die Methode Flächenkennwert: `A_N` aus `Zirk_Flaeche_m2`
(gebäudeweit, mit α), sonst aus Wohnfläche je WE × WE der Wohnzonen oder aus dem gebundenen Gebäude (A8;
Gebäudefläche minus die eigenen Flächen seiner Zonen, zu gleichen Teilen auf die übrigen, N8);
stammt `A_N` aus den Zonen, zählen nur die Zonen in Z1 und α entfällt (N7). Kennwert `k_A` und
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
Urlaube werden je Einheit versetzt gezogen (Entkopplung). **Auf dem Typtagweg (Z4b)** ist `Q_d,Zone`
die Tagesmenge des Typtagjahres, `Dichte(t)` der Tagesgang seines Typtags, sofern das Paket welche
führt, und die Urlaube werden nicht versetzt (N14 (N1)). Weil `z` aus zwölf Gleichverteilten nur
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
Wohnungsstation je Einheit und für die Summe; je Einheit mit Z3, N10). Das Ergebnis trägt die Topologie. DIN 4708 und das
GLF-Verfahren der Vorlage sind nur bei Speicher gültig, sonst Gültigkeitshinweis.

**Bedarfstag (`Bedarfstag.cs`).** 1440 Minutenwerte in kWh, aus Mengengerüst und Formvektor, nie aus
der Jahresreihe; Energie bei `θ_KW,Auslegung` (Faktor `f_KW,A`, 4.2), nicht beim winterlichen
Kaltwasser des Kalendertags. Quellen: (1) Stundenprofil der Zonen am Tag des größten Tagesbedarfs,
gleichmäßig auf Minuten expandiert — **nur mit Warnbanner „Spitzen unterschätzt"** (VDI-6002-Warnung
zu Einzeltagesspitzen) und **nie als Empfehlung ohne Rückfrage**; (2) A100-Referenzprofil aus dem
Katalog, erst nach K1/K8 (bis dahin benannt gesperrt, N11); (3) DIN-4708-Profil, nur Wohnen, ebenfalls ein Normdatensatz und damit
K1/K8-pflichtig (aus W_z(N) und den Zapfblöcken des Parametersatzes, N10); (4) manuell konstruiert nach dem Verfahren der A100 (Konstruktor, Z2, Ablage als
Katalogeintrag Status EIGEN); (5) Ecodesign-Zapfprofil, nur Einfamilienhaus, zur Plausibilisierung
(Katalogzeile aus dem freien Paketteil, N12 (p) und (q)).
**Vorgaberegel:** Wohnen → (3), sobald nach K1/K8 zulässig, sonst (4); Nichtwohnen → (4); ohne
konstruierten Tag öffnet die Auslegung den Konstruktor statt still (1) zu nehmen.

**(a) Summenlinie (`Summenlinie.cs`)** nach DIN EN 12831-3 mit den Rechenregeln des Entwurfs A100/A1
(Formeln nach Grundlagen 3, Koeffizienten als Parameter):

```
Φ_Ü     = U·A · Δθ_Ü / 1000; U·A aus Uebertrager_UA_W_K, sonst U (Parameter je Werkstoff) · A_HE,
          A_HE eingegeben oder aus der Schätzformel je Erzeugerart NA.1/NA.2 (Plausibilitätswächter, N10) [kW]
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
τ = m · c_w / (U·A) · k_τ   nur informativ (Anzeige, Dimensionsprobe); k_τ Parameter der A1,
          c_w in kJ/(kg·K): mit c_w in Wh/(l·K) τ = V · c_w · 3,6 / (U·A) · k_τ (N10)
Wertepaarkurve:    für Φ_Erzeuger auf einem Raster bis zur Erzeugerleistung das kleinste V mit
                   Φ_N = min(Φ_Erzeuger, Φ_Ü(V)) — jedes Paar baubar (N10)
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
DIN 1988-300 (Parameter gekapselt; offen, N10; in der Karte benannt gesperrt, N11).

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
                          Reserve = min SOC / C_sp;  V = Nenninhalt des empfohlenen Punkts (N10)
Plausibilität Ladung:     P_lade · t_F ≥ Q_d,Zapfung + P_zirk · t_Lauf, sonst Mindestleistung nennen
```

**Zum GLF-Verfahren.** Der Faktor ist die Definition der Vorlage, keine DIN-4708-Größe, sondern aus
ihr abgeleitet; die Formel ist gegen die Vorlage (Blatt „Berechnung") nachgemessen. Mit
`P ≈ N · p_b` wird `V_GLF ∝ N · W_z(1)² / W_z(N) = W_z(1)² / (W_b · [K(u_1) + K(u_2)/√N])`: V_GLF
steigt mit N monoton, strebt aber gegen eine Schranke — für große N hängt es kaum noch von der
Gebäudegröße ab. Die übliche Form `W_z(N) / (N · W_z(1))` ergäbe `V_GLF = V_DIN · P / (N · p_b)` und
damit kein eigenes Verfahren. Folge: Das GLF-Verfahren ist nur Teil des Plausibilitätsbands, gilt
bis zu einer Obergrenze N_GLF (INEKON-Setzung, Parameter `Speicherauslegung.GLF_Gueltigkeitsgrenze`, an
Vorlage und Summenlinie festzulegen, N10) und trägt darüber einen Gültigkeitshinweis; ohne Wannen ist es eingeschränkt.

**Großanlage (`Grossanlage.cs`).** Erkennung aus Speichervolumen (`Nachweis_Volumen_l`, sonst gewählter
Auslegungspunkt; die Überlagerung „Auslegung" rechnet ohne übernommenen Punkt, N11) und Leitungsinhalt (`Leitungsinhalt_l`, sonst `Zirk_Laenge_m` × Parameter Inhalt je
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
- **Links**: Zonenliste (`Raster` — in Z1 die Haustabelle `epos-raster` mit Summenfuß, N9 —,
  Spalten Zone, Nutzungsart, Bezugsmenge, Topologie, MWh/a, Summenzeile) mit eigener Listenleiste Zone hinzufügen… · Duplizieren · Entfernen; darunter der
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
  Bedarfsprofil-Dialogs — in dessen OK-Weg, **bevor er schließt** (Parameter `Speichern`,
  `ZapfprofilHuelle.Schreibweg`); lehnt der Schreibweg ab, bleibt er mit dem Grund offen (N9). Die Startseite (`StartseiteHuelle.Brauchwasser`, `:900-911`) schreibt
  `Del/Add_Projekt_Brauchwasser` **und** `ZapfprofilCtrl.Speichern(idProjekt, stand, vorgang)` in
  **einem gemeinsamen `DbVorgang`** (`Add_Projekt_Brauchwasser` nimmt ihn an, `WizardCtrl.cs:2763`);
  der Gebäudekatalog ebenso in `GebaeudeKatalogHuelle.BrauchwasserSchreiben`. `Speichern` schreibt
  `Weg` immer. Ein Esc der vierten Überlagerung schließt nur sie.
- **Ohne Projekt** (Gebäudekatalog aus der Verwaltung) reicht die Hülle keinen Delegaten (umgesetzt:
  kein Zapfprofil-Behälter im Modus Admin, N9); im Assistenten
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
mit `Gaben(int idProjekt, ZapfprofilStand? arbeitsstand)`, dazu `Einstieg`/`Einhaengen` über die Naht
`Zapfprofilwege` und den `ZapfprofilBehaelter` (N9),
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
Renderer: Tagesgang und Wochenprofil `StundenprofileModell` (mehrere Reihen, neu in Z1, N9), Jahresgang gestapelt Zapfung +
Zirkulation `MonatsStapelModell` (`:3048`, aus den getrennten Monatssummen, 2.2), Dauerlinie
`JahresverlaufModell`-Familie bzw. `DauerlinieWaermeModell` (`:363`) mit Perzentillinien;
Wertepaarkurve und Wochendiagramm mit Füllstand zeichnet ebenfalls das `SummenlinieModell` (N11).
**Neu** sind `StundenprofileModell` (Z1, N9) und — mit Z2 — das `SummenlinieModell`, ein Linienbild über
einer x-Größe mit eigener Teilung, zweiter Achse und Marken: kumulierter Bedarf und Versorgung über 1440
Minuten mit markiertem Abstand, Wertepaarkurve und maßgebende Woche der Stundenbilanz (N11); dafür Fälle
in `Proben/ChartProben` samt neuer Messlatte.
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
- **(b) Testdatenbank nur fiktiv** (gilt angepasst, N12 (o) und ZU19)**.** Die Testdatenbank enthält nur fiktive Nutzungsarten, Tagesgangsätze,
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
- **(c) Wache** (gilt angepasst, N12 (o))**.** `TwwKatalogWacheTests`: keine Zeile einer `Tab_Tww*_STAMM` der Testdatenbank mit
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
| **Z1 — Bilanz deterministisch** | S1 samt Temperaturumrechnung und Messwertgrenzen, S2 mit Tagtypgewicht und Ferienregel, S5 mit Zonenanteil und Vorgabe Flächenkennwert, Fassade mit `Bilanzreihe`, Weiche (2.2) samt getrennter Monatssummen, `ZapfprofilCtrl.Speichern/Eingang`, `Tab_TwwProjekt`; Dialog Stufe Einfach mit Vorschau (Tagesgang, Wochenprofil, Jahresgang, Kennzahlen) als Überlagerung unter Windows; Knopf, Optionsgruppe, Leiste „monatlicher Verlauf" und gemeinsamer `DbVorgang` im Bedarfsprofil-Dialog; Wiki-Entwurf | Z0; A3, A4/ZU5, A6, K4; fiktiver Testkatalog genügt | Tests aus 2.4, 4.1–4.3; **ein unabhängig per Tabellenkalkulation (umgesetzt als Python-Skript, N7) gerechneter fiktiver Referenzfall über 8760 h mit Abweichung 0** (Testdaten erfunden, Konzept 3.6 P1); `ZapfprofilWeicheTests` auf einer Projektkopie der Testdatenbank; Referenzlauf grün innerhalb der Toleranz (kein Referenzprojekt setzt die Weiche); `EinheitenWacheTests` mit den neuen Dateien; bunit; Sichtabnahme Windows | 14–18 PT |
| **Z2 — Auslegung deterministisch** | Bedarfstag mit Vorgaberegel und Konstruktor, Wochenreihe, Summenlinie mit Speicherart, Übertrager, Einschaltpunkt, Wertepaarkurve, Monotonieprüfung und Ladezeit, Schnellpfad, Wohnungstabelle und DIN-4708-Kennzahl, DIN 1988-300 nachrichtlich, Speicherauslegung nach V4 mit Ladefenster, GLF, Plausibilitätsband und Warnliste, Großanlagenerkennung, Topologiegruppen; Überlagerung „Auslegung"; `SummenlinieModell` | Z1; K1/K8 für A100- und DIN-4708-Profil (ohne: Konstruktor) | `SummenlinieTests`, `Din4708KennzahlTests`, `SpeicherauslegungTests`, `AuslegungsergebnisTests`, `GrossanlageTests`, `ZapfprofilTrennungWacheTests`; `ChartProben` mit neuem Fall; Referenzlauf unberührt | 16–20 PT |
| **Z3 — Stochastik** | T2, `ZapfZufall` samt Plattformtest, Generator mit gestutztem Mittel, Ensembles der Jahresreihe und des Bedarfstags über `Kulturweitergabe`, Perzentil je Topologie, Gleichzeitigkeit als Ergebnis, Entkopplung der Urlaube, Rechenweg der Jahresreihe „stochastisch" | Z2; ZU8 | `ZapfZufallTests`, `ZapfereignisgeneratorTests`, `ZapfensembleTests` (Toleranz nach 4.4, √N, Topologie); lokal gegen DHWcalc-Referenzdateien; Referenzlauf unberührt | 16–22 PT |
| **Z4 — Oberfläche vollständig** (umgesetzt, N13) | Stufen Erweitert und Experte, Zonenliste für Mischnutzung, Wohnungstabelle, Tagesgang-Editor, Auslastungsgang, Kategorien als Katalogkopie, Schätzhilfen, Warnlogik, Dauerlinie, Katalogdialog mit Untermenü und Katalogimport, KiSicht, Hilfeschlüssel, Wiki, beide Sprachen | Z3; ZU3 (iU11) | alle Oberflächenwachen; Rasterprobe; `MenuebandTests`; erweiterte `WikiProduktdatenWacheTests`; Wiki gegengelesen; iOS-Lauf nur nach Rückfrage und nur, wenn die Bedarfsprofil-Hülle umgezogen ist | 11–14 PT (+2–3 PT iPad-Voraussetzung) |
| **Z4b — VDI-4655-Import mit Typtagzuordnung** (umgesetzt, N14) | T3 (Schritt 131), `Normformvektorleser`, `Typtagzuordnung` mit Wetterkopplung (Vorfragen 4.2 in N14 (d) beantwortet), Importdialog (Gruppe 2, offen) | Z4; K3a, K8 | Tests mit erfundenen Typtagen; Auslieferungsvorlage leert `Tab_TwwTyptag_IMPORT`; kein VDI-Wert in Repository oder CI | 3–5 PT |
| **Z5 — Kalibrierung und Validierung** (umgesetzt, N15; Werkzeug bereit, Daten offen, N22) | Messdatenimport, Vergleichsbericht, Validierung gegen freie Messreihen und freigegebene INEKON-Projekte, Kalibrierung der Nichtwohn-Parameter, Katalogausbau auf 25–27 Typen; gegebenenfalls Referenzprojekt auf dem Generator (ZU7) | Z4; K5, K6 | Validierungsbericht mit messbaren Kriterien: Messspitze im P85–P95-Band der synthetischen Dauerlinie (Konzept 3.6), √N-Skalierung der Überschätzung, Formabgleich des Tagesgangs mit einer Schwelle (Parameter), Energie nach Kalibrierung exakt; bei Referenzprojekt: vierte Einfrierregel, Neueinfrieren mit Begründung, grüner CI-Lauf | 10–12 PT |

**Umsetzungsstand und Abweichungen:** Z0 umgesetzt, N2 bis N4 (Kapitel 11); T1 ist Schritt 103
(N4). Z1 umgesetzt und nach `ios_migration_september` zusammengeführt (Push `4971556a`, Gate auf
dem Merge-Stand grün), Abweichungen und Festlegungen in N7 bis N9; die Sichtabnahme unter Windows
steht aus. Z2 Gruppe 1 (Rechenweg der Auslegung) ist auf dem Zweig `z2` umgesetzt, gegengeprüft und
nachgebessert; Abweichungen und Festlegungen in N10. Z2 Gruppe 2 (Oberfläche der Auslegung: DTO, Hülle,
Überlagerung „Auslegung" samt Konstruktor, `SummenlinieModell` und die Bilder der Auslegung) ist auf
demselben Zweig umgesetzt, gegengeprüft und nachgebessert; Abweichungen und Festlegungen in N11. Z2 ist
mit dem Stand von `ios_migration_september` zusammengeführt, die Testdatenbank nachgezogen und das Gate
auf dem Merge-Stand grün; die Sichtabnahme unter Windows steht aus. Stand je Stufe in der Statusdatei
(#438, #443, #451).

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
am 23.09.2026 entschieden (N6). ZU19 und ZU23 sind mit den Stufen Z3 und Z4b entschieden (N12, N14); am 25.09.2026 sind ZU20, ZU21, ZU22 und ZU24 entschieden, K5 ist zurückgestellt und ZU7 terminiert (Nachtrag N16). ZU25 bis ZU29 sind mit dem Sammelposten N18 hinzugekommen und am 25.09.2026 nach Empfehlung entschieden (Nachtrag N19): ZU25 als ein Schemaschritt nach der Sichtabnahme (umgesetzt, N21), ZU26 als eigene Welle nach iU11, ZU27 zurückgestellt, ZU28 und ZU29 umgesetzt. ZU30 bis ZU33 sind mit dem Katalogimport der Bedarfstage und Parameter am 25.09.2026 entschieden und umgesetzt (N20). Das Validierungswerkzeug der Stufe Z5 steht seit dem 26.09.2026 samt einem ersten Lauf an offen lizenzierten Fremddaten (N22); K5 selbst bleibt zurückgestellt, und N22 nennt mit V1 bis V5 fünf Punkte, die der Lauf aufgeworfen hat. ZU26 ist mit N23 vor iU11 umgesetzt; der iOS-Lauf steht aus. ZU7 ist mit N24 umgesetzt: Projekt
1045 rechnet sein Brauchwasser über den Generator, siebte Einfrierregel „gesäte
Zapfprofil-Eingaben", Basis `2026-09-26_R20_Zapfprofil`. Der zweite Validierungslauf an offen lizenzierten Daten (N27) arbeitet V1 bis V5 ab und stellt die Frage ZU35. K2–K4, K6, K7 (samt K3a) und A1–A12 waren nicht Gegenstand dieser Entscheide; das Papier setzt ihre
Empfehlung weiterhin voraus (Mockup Abschnitt 8), entschieden sind sie damit nicht. Die Spalte
„Entscheid" zeigt den Stand je Punkt.

**K1–K8 und A1–A12 aus dem Mockup** (dort ausführlich), zusammengefasst mit der Empfehlung, die dieses
Papier voraussetzt:

| Nr. | Frage | Empfehlung | Entscheid |
|---|---|---|---|
| K1 | Beschaffung A100-Profildateien, Weißdruck-Status, DIN 4708-2/-3 | sofort anfragen; Verzicht auf Verwertungslizenz zur Mitauslieferung bestätigen | nach Empfehlung, 23.09.2026 (N1); Unterlagen liegen vor, A100 weiter Entwurf (N6) |
| K2 | Typenumfang v1.0 | rund 15 neue Typen neben dem Bestandskatalog; Ablösung erst mit K6 | Empfehlung vorausgesetzt |
| K3 | Auslegungsperzentil | P99 Vorgabe, P95 wählbar; Brauchwasser-Auslegung nur aus der Dreiergruppe, Empfehlung der Summenlinienpunkt | Empfehlung vorausgesetzt |
| K3a | VDI-4655-Datenstrategie | Import-Schnittstelle (Z4b), gleichrangig, Vorgabe Eigenkonstruktion | Empfehlung vorausgesetzt |
| K4 | Kaltwasser in der Bilanz | fester Jahresgang in Z1, Kopplung an die Klimaregion als Option in Z4; die Auslegung rechnet unabhängig davon mit `θ_KW,Auslegung` | Empfehlung vorausgesetzt |
| K5 | Messdaten | Freigabe vor Z5, bis dahin nur Verhältniszahlen | **zurückgestellt 25.09.2026** (N16, „später“); Empfehlung für später: zwei bis drei Mehrfamilienhäuser und ein Nichtwohnobjekt mit mindestens einem Messjahr, CSV-Stundenwerte, anonymisiert; Objektdaten nie im Repositorium. **Das Werkzeug steht** (`Werkzeuge/ZapfprofilValidierung`, N22), und ein erster Lauf an offen lizenzierten Fremddaten liegt vor; die Freigabe eigener Reihen bleibt offen. **Zweiter Lauf an offenen Daten (N27):** Bezugsmengen belegt oder abgeleitet, Feiertage je Land, Band je Größenklasse als Analyse; die √N-Skalierung ist an den offenen Daten nicht prüfbar (keine Nutzungsart über eine Größenordnung von N) und bleibt Teil von K5 |
| K6 | Bestandsweg | Koexistenz bis nach Z5 | Empfehlung vorausgesetzt |
| K7 | Katalogpflege | Auslieferung ReadOnly, Vier-Augen-Freigabe, Anwenderkopie „eigen", benutzte Zeilen unveränderlich | Empfehlung vorausgesetzt |
| K8 | Juristische Prüfung | mit Z0 beauftragen; umfasst auch die lokalen Normkopien und die Digitalisate des Bestandskatalogs | nach Empfehlung, 23.09.2026 (N1); Nutzung vorab zu Testzwecken OK (N5); die Setzungen der Speicherauslegung kommen aus der INEKON-Vorlage V4 und hängen nicht an K8 — ausgeliefert bis auf Ladefenster-Beginn und GLF-Grenze (N28) |
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
| **ZU7** | Deckt ein Referenzprojekt den Generatorweg ab? | bis Z4 **nein** — der Generator wird in Kern-Tests auf einer Projektkopie der Testdatenbank geprüft; in Z5 ein Referenzprojekt umstellen und die vierte Einfrierregel einführen (3.4) | nach Empfehlung, 23.09.2026 (N1); **terminiert 25.09.2026** (N16); **umgesetzt 26.09.2026** (N24): Projekt 1045 auf den Generator umgestellt, siebte Einfrierregel „gesäte Zapfprofil-Eingaben", Basis `2026-09-26_R20_Zapfprofil` |
| **ZU8** | Bitgleichheit Windows/iOS | ganzzahliger Zufall, Normalverteilung ohne transzendente Funktionen, gerundete Einmalwerte, feste Summationsfolge (4.2, 4.4); plattformübergreifend zusätzlich Vergleich mit Toleranz | nach Empfehlung, 23.09.2026 (N1) |
| **ZU9** | Neue Tabellen auf iOS | über einen neuen Seed; ältere Datenbank ohne Tabellen rechnet den Bestandsweg, der Knopf ist benannt gesperrt | nach Empfehlung, 23.09.2026 (N1) |
| **ZU10** | Zapfprofil ohne gespeichertes Projekt (Gebäudekatalog aus Verwaltung oder Assistent vor dem Speichern) | Knopf nur mit gespeichertem Projekt; im Assistenten erst nach dem Speichern des Projekts | nach Empfehlung, 23.09.2026 (N1) |
| **ZU11** | Wache für lokale Normdaten | ja, als Fall in `RepositoryOrdnungWacheTests` und Posten der Auslieferungsvorlage (Kapitel 6) | nach Empfehlung, 23.09.2026 (N1) |
| **ZU12** | Projektkopie der Nutzungsart oder Katalogverweis? | **Katalogverweis auf unveränderliche Versionen** (3.2): kein Kopieren je Projekt, keine rückwirkende Änderung; Umstellen auf eine neue Version nur ausdrücklich je Zone | nach Empfehlung, 23.09.2026 (N1) |
| **ZU13** | Topologie je Zone oder je Gebäude? | je Zone wie im Konzept 2.2; die Auslegung rechnet je Topologiegruppe (4.5) | nach Empfehlung, 23.09.2026 (N1) |
| **ZU14** | Wie kommt der Auslieferungskatalog in Bestandsinstallationen? | Katalogpaket außerhalb des Repositoriums, eingespielt von der Auslieferungsvorlage (neue Installation) bzw. über einen Katalogimport in der Verwaltung (Z4); nie über den Schemaschritt | nach Empfehlung, 23.09.2026 (N1) |
| **ZU15** | Nutzung der VDI-6002-Kopien in der Ablage des Anwenders, deren Exemplare den Lizenzstempel einer Universität tragen? | **eigene Lizenz prüfen oder beschaffen**; bis dahin bleiben die daraus extrahierten Tabellen lokal (Kapitel 6, „Lokale Testdaten") und werden nicht weitergegeben — nicht an Dritte, nicht ins Repository, nicht in Testdatenbank, CI oder Auslieferung | nach Empfehlung, 23.09.2026 (N1); Nutzung vorab zu Testzwecken OK (N5) |
| **ZU16** | Ersetzt `--katalogpaket` auch die Zeilen mit `Status = 'AUSLIEFERUNG'`, die die Quelle schon führt? | **ja** — das Paket ist die Quelle der Wahrheit für den Auslieferungskatalog; so ist das Werkzeug gebaut (N2 (j)) | ersetzen, 23.09.2026 (N6) |
| **ZU17** | Der Projektimport ordnet eine namensgleiche `EIGEN`-Zeile (gleicher Bezeichner und Katalogversion) mit anderem Inhalt ohne Inhaltsvergleich der Zielzeile zu — soll er vergleichen? | **ja, in Z1:** Inhaltsvergleich über die Wertgruppen; bei Abweichung Mitnahme als neue Version mit Zusatz im Bezeichner, nie stilles Umhängen; Festlegungen der Umsetzung (N8) | nach Empfehlung, 23.09.2026 (N6) |
| **ZU18** | Eine oder mehrere Testklassen (noch aufzuspüren, N3 (d)), die die Repo-Testdatenbank direkt öffnen (danach liegen `-shm`/`-wal` daneben), auf eine Arbeitskopie oder `immutable` umstellen? | **ja**, als kleiner Folgeposten außerhalb der Z-Stufen | nach Empfehlung, 23.09.2026 (N6) |
| **ZU19** | Dürfen Normwerte als geringfügig abweichende, abgeleitete Werte im Repositorium stehen? | **ja**, wenn die Ableitung reproduzierbar und rückrechenbar ist und die Provenienz sie nennt | Anwenderentscheid 23./24.09.2026 (N12, N14); umgesetzt für VDI 6002 (N12) und VDI 4655 (N14) |
| **ZU20** | Werden die abgeleiteten VDI-6002-Werte (Katalogtypen nach ZU19) ausgeliefert? | **ja**, mit Herkunftsvermerk „abgeleitet aus VDI 6002“ im Katalog | **entschieden 25.09.2026** (N16), **umgesetzt (N17)**: ausliefern mit Herkunftsvermerk „abgeleitet aus VDI 6002 Blatt n“, Herkunftsart `VERFAHREN`, Träger im freien Paketteil |
| **ZU21** | Setzungen des freien Paketteils bestätigen oder ändern (N12 (p)–(r), N13, N15 (e)/(g))? | bis zur fachlichen Durchsicht **nicht ausliefern**; Prüfliste je Setzung vorlegen | **entschieden 25.09.2026** (N16): die Setzungen bleiben bis zur fachlichen Durchsicht durch den Anwender **ungeliefert**; Prüfliste [Prüfliste ZU21](Zapfprofilgenerator/2026-09-25_Pruefliste_ZU21_Setzungen.md); **entschieden 26.09.2026** — Abschnitte 1 und 2 der Prüfliste bestätigt bis auf Ecodesign: Profile erweitern, Folgeposten (N25); Ecodesign erweitert (N26); Abschnitt 3, Speicherauslegung: aus der Vorlage V4 ausgeliefert, offen Ladefenster-Beginn und GLF-Grenze (N28) |
| **ZU22** | Werden die abgeleiteten VDI-4655-Werte ausgeliefert (die Richtlinie untersagt schon innerbetriebliche Kopien)? | **nein**; der lizenzierte Anwender spielt sie aus einem eigenen Paket ein | **entschieden 25.09.2026** (N16): **nicht** ausliefern, der heutige Weg bleibt — eigenes Paket des lizenzierten Anwenders |
| **ZU23** | Auch die VDI-4655-Originalwerte des Repositoriums nach der Regel ZU19 ableiten? | **ja**, gleiche Regel wie ZU19 | Anwenderentscheid 24.09.2026 (N14); umgesetzt für die Ableitung und das Grundlagenpapier |
| **ZU24** | Katalogtypen 25–27 (Hotel, Krankenhaus, Sportstätte u. a. aus DIN EN 12831-3 Beiblatt A100): ZU19 auf die A100 ausdehnen oder externes Katalogpaket? | **externes Katalogpaket** beim Anwender | **entschieden 25.09.2026** (N16), **umgesetzt (N17)**: externes Katalogpaket beim Anwender, keine Ausdehnung von ZU19 auf die A100; Paketvorlage ohne Werte im Repositorium |
| **ZU25** | Konstruktorzeilen des Bedarfstags in der Datenbank (N13 (p)) und der redundante Index auf `Tab_TwwMessreihe.ID_Projekt` (N15 (a)): je ein Schemaschritt oder einer für beide? | **ein** Schemaschritt für beide — reines DDL, ein eigener Schritt je Kleinigkeit kostet eine Nummer und einen Referenzlauf | **entschieden 25.09.2026** (N19, Empfehlung angenommen): ein Schemaschritt; **umgesetzt (N21)** als Schemaschritt 145 (T5 „Konstruktor") — `Tab_TwwKonstruktorzeile` am Auslegungssatz und `DROP INDEX` des redundanten Index; **Rest behoben (N30):** der wieder geöffnete Konstruktor beginnt mit Bezugsart und Bezugsmenge des gespeicherten Tags, ohne Schemaschritt |
| **ZU26** | Katalogdialog „Brauchwasser-Nutzungsarten" auf iOS (N13 (r)): jetzt oder als eigene Welle? | **eigene Welle nach iU11**; bis dahin lehnt die Hülle ihn dort benannt ab | **entschieden 25.09.2026** (N19, Empfehlung angenommen): eigene Welle **nach iU11**; **umgesetzt (N23), iOS-Lauf ausstehend** — vorgezogen vor iU11 |
| **ZU27** | Referenzfall der Wetterkopplung mit `Tab_Solar.Bedeckungsgrad` aus einem TRY-Import (N14 (c), N15 (m)): an ZU7 koppeln oder liegen lassen? | **nicht an ZU7 koppeln** — der Typtag-Weg ist eine wahlfreie Jahresgang-Alternative allein für Brauchwasser und für VDI 6007 nicht erforderlich; liegen lassen, bis ein Anwender ihn einsetzt | **zurückgestellt 25.09.2026** (N19, Empfehlung angenommen): nicht an ZU7 gekoppelt, die Bewölkungsschwelle bleibt bis dahin an erfundenen Werten geprüft |
| **ZU28** | Anzeige des Herkunftsprotokolls (N13 (b), N18 (b)): Herleitungszeilen der Stufe Experte oder eigene Karte? | **eigene Karte** im Ergebnisbereich — die Herleitungszeile trägt einen Satz, das Protokoll trägt je Zone ein Dutzend Werte | **entschieden 25.09.2026** (Anwender: „Anzeige ermöglichen (eigene Karte im Ergebnisdialog)"), **umgesetzt (N19)**: zugeklappte Karte „Herkunft" ab Stufe Erweitert, dieselbe in der Auslegung |
| **ZU29** | Größenschutz des Typtag-Paketlesers (`TwwTyptagCtrl.PaketLesen`): nachziehen wie beim Katalogimport? | **ja**, dasselbe Muster wie `TwwNutzungsartCtrl.PaketLesen` (N18 (c)): drei Grenzen als Konstanten, Prüfung aus dem Zentralverzeichnis **und** beim Lesen, Pfadprüfung, eigene Kennungen des Lesers | **entschieden 25.09.2026** (N19, Empfehlung angenommen), **umgesetzt (N19)** |
| **ZU30** | Was ist eine Dublette bei einem eingespielten Bedarfstag — und darf er eine vorhandene Zeile ändern? | **ersetzen am Platz**: gleicher Bezeichner und gleiche Katalogversion, gleicher Inhalt — übersprungen; abweichender Inhalt — die vorhandene Zeile trägt die Werte des Pakets, mit derselben `ID`, damit ein Projekt weiter darauf zeigt | **entschieden 25.09.2026**, **umgesetzt (N20)**: „importierter Bedarfstag darf eine vorhandene Auslieferungszeile ersetzen. Hinweis geben."; auch eine Auslieferungszeile, danach Stand `IMPORT`, `ReadOnly` 0, zwei Hinweise im Bericht |
| **ZU31** | Was tut der Import mit einem Parameter, den der Katalog schon führt? | **den Wert ersetzen** — ein Parameter ist ein Wert, keine Version; keine Bildung von „(Import n)" | **entschieden 25.09.2026**, **umgesetzt (N20)**: „Import ersetzt den Wert. Hinweis geben"; ein Schlüssel, den kein Rechenweg liest, eine abweichende Einheit und ein Wert außerhalb des Bereichs sind benannt abgelehnt (`TwwParameterkatalog`) |
| **ZU32** | Wie berichtet ein Import, der drei Tabellen anfasst? | **je Tabelle eigene Zeilen** mit Ergebnis und Grund, im Dialog als Gruppen | **entschieden 25.09.2026**, **umgesetzt (N20)**: „Bericht: je Tabelle eigene Zeilen mit Ergebnis und Grund"; Reihenfolge Bedarfstage, Parameter, Nutzungsarten, dazu der Prüflauf „Nur prüfen, nichts schreiben" mit „würde …" |
| **ZU33** | Welche Regeln prüft der Import an einem Bedarfstag und an einem Parameter? | **nach Empfehlung**: Wertemengen, Tagesfenster der Ereignisse, positive Energiesumme, lückenlose Reihenfolge, bekannter Parameterschlüssel samt Einheit und Bereich; ein Fehler lehnt nur den Eintrag ab | **entschieden 25.09.2026** („Prüfung: Empfehlung"), **umgesetzt (N20)** mit zwei benannten Abweichungen: eine leere `Bezugsmenge` bleibt erlaubt, und ein Ereignis ohne seinen Bedarfstag lehnt das Paket als Ganzes ab |
| **ZU34** | Einstieg in den Katalog der Brauchwasser-Nutzungsarten auf dem iPad, wenn der Hilfe-Assistent nicht verfügbar ist (N23 (b)) | **mit iU11 ein Einstieg für alle Kataloge**, nicht einzeln für diesen; bis dahin öffnet der Assistent den Katalog | **umgesetzt (N29)** — Knopf „Kataloge…“ der Projektliste auf dem iPad |
| **ZU35** | Bandkriterium nach Größenklasse (N27 (c)): Im zweiten Lauf lag die Messspitze der acht Wohn- und Pflegeobjekte mit N ≥ 10 bei P96 bis P99,9 der gerechneten Dauerlinie — keine im bestätigten Band P85–P95; bei N < 10 misst ein Quantilband die Ziehung einer Stunde | **N ≥ 10: Band P95–P99,9** (`Zapfprofil.Validierung.Band.Unten` 0,95, `.Oben` 0,999); **N < 10: „nicht bewertbar" (gelb)** als Setzung des Werkzeugs; übernehmen erst, wenn eigene Objekte aus K5 den Vorschlag bestätigen — er ist an denselben Daten abgelesen, die er einfängt | **offen** (N27) |

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
| ZU19 | Anwenderentscheid 23./24.09.2026: geringfügig abweichende VDI-Werte im Repositorium, Ableitung reproduzierbar (Rückrechenbarkeit zugelassen) — umgesetzt für VDI 6002 (N12) | Anwender (entschieden) | Z3 |
| ZU20 | Auslieferung der abgeleiteten VDI-Werte: ja/nein (N12) | Anwender | nach K8 |
| ZU21 | Setzungen des freien Paketteils bestätigen oder ändern (N12 (u), erweitert in N13) | Anwender | vor der ersten Auslieferung |
| ZU23 | Anwenderentscheid 24.09.2026: auch die Originalwerte der VDI 4655 im Repositorium werden nach der Regel ZU19 abgeleitet aufgenommen — umgesetzt für die Ableitung und das Grundlagenpapier (Nachtrag N14, Absatz ZU23) | Anwender (entschieden) | Z4b |
| ZU24 | Katalogtypen 25–27: ZU19 auf DIN EN 12831-3 Beiblatt A100 ausdehnen oder externes Katalogpaket (N15 (f)) | Anwender | vor der Auslieferung |
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

### N4 (23.09.2026) — Schrittnummer von T1: 103

**Anlass.** Beim Zusammenführen des Zweigs `z0` mit `origin/ios_migration_september` stand dort
Schritt 101 schon veröffentlicht. N2 (a) bleibt unverändert; dieser Nachtrag berichtigt ihn, der
Hauptteil ist mit Verweis „(N4)" nachgezogen. Er enthält **keinen Entscheid** des Anwenders; die
Vergabe ist mit der Sitzung der Wirtschaftlichkeit abgestimmt.

**Befund.** T1 ist Schritt 103: Schritt 101 gehört der Gebäudesimulation (Gebäudespalten, auf
`origin` seit `a6dd0fa4`), Schritt 102 dem KWKG-Schritt der Wirtschaftlichkeit (leere
`KWKG_Anlagenart` wird NULL); `SchemaStand.Zielversion` = 103, Konstante
`SCHRITT_103_ZAPFPROFIL_KATALOG`. Die Testdatenbank ist aus der origin-Fassung (Stand 101) mit den
Schritten 102 und 103 migriert und trägt den fiktiven Testkatalog wie zuvor (19 Zeilen); der
Zellvergleich gegen die frühere Fassung auf 102 zeigt nur den Schemastand und die Gebäudespalten
aus Schritt 101. Merge `23437ea1`, Testdatenbank `3d5e5b84`.

### N5 (23.09.2026) — Lizenz: vorab zu Testzwecken freigegeben

**Anwenderentscheid** (im Wortlaut: „Lizenz - vorab zu testzwecken OK"): Die Nutzung der
Normkopien in der Ablage des Anwenders und der daraus extrahierten Tabellen (VDI 4655, VDI 6002)
ist **vorab für Testzwecke freigegeben**. Sie gilt für die lokalen Testdaten unter
`Referenzlaeufe/Normzahlen/` (gitignoriert), für die Ladeweiche des Mockups und für lokale
Proben in den Stufen Z1 bis Z5.

**Was unverändert bleibt:** Keine Weitergabe an Dritte, nichts davon ins Repository, in die
Testdatenbank, in die CI oder in die Auslieferung (Kapitel 6); die juristische Prüfung K8 und die
Lizenzprüfung ZU15 laufen weiter und entscheiden über Auslieferung, Mitauslieferung und
Weitergabe. K8 wird mit Z0 beauftragt (N1); dieser Nachtrag ändert nur den Zwischenstand bis zu
ihrem Ergebnis.

**Folgen:** Kapitel 9, Zeilen K8 und ZU15, tragen den Zusatz „vorab zu Testzwecken OK (N5)".
Keine Änderung an Code, Wachen oder Testdatenbank.

### N6 (23.09.2026) — K1 Unterlagen liegen vor; ZU16 bis ZU18 entschieden

**Anwenderentscheid** (im Wortlaut: „K1: Dokumente liegen vor unter [Ablage des Anwenders,
Ordner Wärmespeicher]; ZU16: ersetzen; ZU17: Empfehlung; ZU18: Empfehlung").

**K1.** In der Ablage des Anwenders liegen (nur lesend, nie ins Repositorium): DIN 4708-2 und
DIN 4708-3 (Ausgabe 1994), DIN EN 12831-3 samt den Entwürfen A1 und A100, DIN V 18599-10, VDI 4655,
VDI 6002 Blatt 1 und 2. A100 ist weiterhin ein **Entwurf**; der Weißdruck-Status bleibt zu
beobachten, das Auslegungsergebnis trägt den Vermerk „Entwurfsstand" (Konzept, Dreiergruppe).
Die A100-Profildateien liegen nicht als Datenträger vor, sondern als Tabellen im Entwurf; sie
werden wie die übrigen Normtabellen als **lokale Testdaten** unter `Referenzlaeufe/Normzahlen/`
(gitignoriert, Muster U8, Freigabe zu Testzwecken nach N5) erfasst, nie im Repositorium. Der
Verzicht auf eine Verwertungslizenz zur Mitauslieferung ist mit N1 bestätigt. K1 gilt damit als
**erledigt für Z1 und Z2**; die Erfassung der A100-Bedarfstage und der DIN-4708-2-Tabellen als
lokale Testdaten ist ein Posten der Stufe Z2 (Anhang-A-Muster: Skript, `QUELLE.txt`, keine Werte
in Papieren).

**ZU16 — ersetzen.** `--katalogpaket` ersetzt den gesamten Tww-Auslieferungskatalog der Quelle,
auch vorhandene Zeilen mit `Status = 'AUSLIEFERUNG'`; das Werkzeug bleibt wie gebaut (N2 (j)).

**ZU17 — nach Empfehlung.** Der Projektimport vergleicht in Stufe Z1 namensgleiche `EIGEN`-Zeilen
über die Wertgruppen; bei Abweichung Mitnahme als neue Version mit Zusatz im Bezeichner, nie
stilles Umhängen. Posten der Stufe Z1 (Kapitel 7).

**ZU18 — nach Empfehlung.** Die Testklassen, die die Repo-Testdatenbank direkt öffnen, werden
aufgespürt und auf Arbeitskopie oder `immutable` umgestellt; kleiner Folgeposten außerhalb der
Z-Stufen.

**Folgen:** Kapitel 9, Zeilen K1 und ZU16–ZU18, tragen den Entscheid mit Verweis „(N6)". Offen
beim Anwender bleiben K8 (juristische Prüfung) und ZU15 (Lizenz der VDI-6002-Kopien), beide mit
der Zwischenfreigabe aus N5.

### N7 (23.09.2026) — Umsetzungsbefunde Z1, Gruppe 1 (Rechenweg)

**Anlass.** Der Rechenweg der Stufe Z1 — Mengengerüst, Kalender, Formvektor, Zirkulation, Fassade und
der unabhängige Referenzfall — ist auf dem Zweig `z1` umgesetzt und gegengeprüft. Dieser Nachtrag hält
fest, wo die Umsetzung vom Papier abweicht oder es genauer fasst; der Hauptteil ist an den betroffenen
Stellen mit Verweis „(N7)" berichtigt. Er enthält **keinen Entscheid** des Anwenders.

**Befunde und Festlegungen:**

- **(a) Signaturen (2.1, 2.2).** Die Zone ist `ZonenStand` (`ZapfprofilStand.cs`); ein
  `Zoneneingang.cs` gibt es nicht. `Mengengeruest.JahresenergieKwh` liefert ein `Mengenergebnis`
  (Jahresenergie, Bezugsmenge, f_θ, Zonenfläche) statt `double`; die Temperaturen der Zone kommen als
  `Zonentemperaturen` herein, der Messwert über `MesswertAus`. Der Formvektor arbeitet auf einer
  normierten `Zeitstruktur` statt auf der `Nutzungsart`. `AusJahrestagen` liefert null, ein oder zwei
  Fenster. Die Zirkulation ist zweigeteilt: `Ansetzen` (Methode, Leistung, α, Jahresverlust,
  Zonenanteile) und `Reihe(Jahresverlust, Laufzeit, Fenster)`; eine Klasse `ZirkulationEingang` gibt es
  nicht, die Projektgrößen kommen als `ProjektStand`. `Bilanzreihe` ist eine unveränderliche Klasse mit
  `IReadOnlyList<double> StundenKwh`; die Weiche (2.2) übergibt deshalb `KopieStundenKwh()`.
- **(b) Flächenformel und f_θ (4.1).** Auch der Kennwert der Flächenformel Wohnen wird über f_θ
  umgerechnet (A1: jeder Kennwert mit anderem Temperaturbezug zwingend). Bezug sind die
  Bezugstemperaturen der Nutzungsart, die die Formel wählt; a, b, c tragen keinen eigenen
  Temperaturbezug. Der Katalog setzt die Bezugstemperaturen einer solchen Nutzungsart deshalb auf den
  Temperaturbezug des Verfahrens.
- **(c) Messwert ohne Einheit oder Grenze (3.1, 4.1, 2.2).** Ein Jahresmesswert ohne Einheit und ein
  Messwert in kWh/a ohne Bilanzgrenze werden benannt abgelehnt (`MesswertUngueltig`), statt still als
  kWh/a bzw. Grenze 1 gelesen zu werden; ein Volumenmesswert ist ohne Angabe Grenze 1. Einheit, Grenze
  und Umrechnung stehen im Herkunftsprotokoll.
- **(d) Parameter, die die Rechnung nicht entscheiden (2.1).** Fehlen die Warnschwelle des Formvektors,
  die Rückfrageschwelle des Messwerts (nur bei einer Zone mit Messwert) oder die Vorgabe der Wohnfläche
  je WE für die Zonenfläche, nennt der Hinweis `PARAMETER_FEHLT` den Schlüssel einmal; die Prüfung bzw.
  die Fläche entfällt, einen Rückfallwert gibt es nicht. Parameter, die die Rechnung entscheiden, lehnen
  weiter benannt ab.
- **(e) Kalender (4.2).** Es gilt die Regel des Papiers: Samstag und Sonn-/Feiertag nur mit
  Kennzeichen `We[d]`. Ein Klimakalender ohne Kennzeichen (Altkonvention) kennt damit kein Wochenende.
- **(f) Fläche A_N der Zirkulation (4.3).** α ist für ein gebäudeweites A_N gedacht: Mit
  `Zirk_Flaeche_m2` gilt `P = α · k_A · A_N / (365 · t_Lauf)`. Stammt A_N aus den Zonenflächen, ist
  A_N die Summe der Flächen der Zonen in Z1 und α entfällt — sonst kürzte eine Zone außerhalb Z1 ohne
  Fläche den Verlust der Zonen in Z1 ein zweites Mal. Trägt eine Zone in Z1 keine Fläche, nennt ein
  Hinweis sie; ist die Fläche von Z1 null, gilt der Rückfall auf die Methode Anteil. Die Zonenfläche
  ist bei Bezugsart Fläche die Bezugsmenge, sonst WE · Wohnfläche je WE, mit der WE-Zahl als
  Bezugsmenge (Wohneinheiten) oder Σ Anzahl der Wohnungstabelle (Personen) und der Wohnfläche je WE
  aus der Zone, sonst aus dem Parameter. Das gebundene Gebäude (A8) speist `ZapfprofilCtrl` in
  Gruppe 2 ein.
- **(g) Laufzeitfenster (4.3), Festlegung der Umsetzung.** Tagesmitte ist der Schwerpunkt
  `m = Σ_h (h + ½) · E_h / Σ_h E_h` der Stundensummen der Zapfung der Zonen in Z1 (ohne Z1 aller
  Zonen, ohne Zapfung 12 Uhr); Beginn `⌊m − t_Lauf/2 + ½⌋`, an den Tagesrand geschoben; eine
  gebrochene Laufzeit belegt die letzte Stunde anteilig.
- **(h) Zirkulation vor und nach der Kalibrierung (4.1).** `Zirkulationsansatz` trägt Jahresverlust und
  Zonenanteile vor der Kalibrierung (`JahresverlustVorKalibrierungKwh`); nach einer Kalibrierung mit
  Grenze 2 oder 3 ist der verbuchte Jahresverlust nur `Kennzahlen.JahresverlustZirkulationKwh`.
- **(i) Referenzfall (Kapitel 7, Zeile Z1).** An Stelle der Tabellenkalkulation steht das
  Python-Skript `EPOS.Kern.Tests/Proben/Zapfprofil/referenzfall_bauen.py`: Es liest nur die fiktive
  Eingabe, rechnet ohne C#-Code nach den Formeln des Papiers und schreibt Stundenwerte und Kennzahlen
  wiederholbar byte-gleich. Abgedeckt sind vier Zonen mit Feiertagen, Ferienfenstern, Temperaturumrechnung,
  Messwert in m³ (Grenze 1) und in kWh (Grenze 2), einer Zone mit Katalog-Grenze 2 und dem
  Flächenkennwert mit gebäudeweiter Fläche. Das Laufzeitfenster (g) spiegelt das Skript nur.
- **(j) Testklassen (2.4, 4.1, 4.2).** `NutzungsartTests.Tagesgang_und_Woche_summieren_zu_eins` steht in
  `FormvektorTests`, `TemperaturTests.Umrechnung_auf_Bezugstemperatur` in `MengengeruestTests`, die
  Fälle von `KaltwassergangTests` in `ZapfkalenderTests`.
- **(k) Unveränderlichkeit.** Bilanzreihe und Zonenanteile des Ansatzes sind nur lesbar; die
  `Zeitstruktur` bleibt intern und verlässt den Rechenweg nicht. Der Umgebungswächter über
  `EPOS.Kern/Allgemein/Zapfprofil/` prüft auch `System.Data`, SQLite, `System.IO`, `SpecialFolder`,
  `Program.*` und `Environment.*`.

**Folgen:**

| Punkt | Folge | Verantwortlich | Stufe |
|---|---|---|---|
| (f) | gebundenes Gebäude (A8) als Fläche über `ZapfprofilCtrl.Eingang` | Agent der Stufe Z1 | Z1, Gruppe 2 |
| (a) | die Weiche übergibt Kopien der Bilanzreihen | Agent der Stufe Z1 | Z1, Gruppe 2 |
| (b) | Bezugstemperaturen einer Nutzungsart mit Flächenformel im Katalog auf den Bezug des Verfahrens setzen | Katalogpflege | mit dem Auslieferungskatalog |

### N8 (23.09.2026) — Umsetzungsbefunde Z1, Gruppe 2 (Weiche, Schreibweg, Eingang, Projekttransfer)

**Anlass.** Weiche, Schreib- und Eingangsweg, Vorschau und Inhaltsvergleich im Projektimport (ZU17)
sind auf dem Zweig `z1` umgesetzt und gegengeprüft; die Befunde der Gegenprüfung sind nachgebessert.
Wo ein Befund dem Papier widersprach, gilt das Papier. Dieser Nachtrag hält fest, wo die Umsetzung
vom Papier abweicht oder es genauer fasst; der Hauptteil ist an den betroffenen Stellen mit Verweis
„(N8)" berichtigt. Er enthält **keinen Entscheid** des Anwenders.

**Befunde und Festlegungen:**

- **(a) Signaturen (2.2, 3.3).** `ZapfprofilCtrl.Speichern` liefert den `ZapfprofilStand` mit den Ids
  der Datenbank (neue Zonen und Wohnungstypen tragen ihre neue Id) statt `void`; eine Überladung ohne
  `DbVorgang` schreibt im eigenen Vorgang. Neu sind `ZapfprofilCtrl.Rechnen(idProjekt, stand,
  wochentagJan1, we)` — Katalog einmal gelesen, dann Eingang und `ZapfprofilRechner.Rechnen` — und die
  Überladung `Eingang(idProjekt, stand, wochentagJan1, we, katalog)` mit Arbeitsstand. Die Weiche liest
  den gespeicherten Stand mit `Lies` und rechnet ihn in `SimulationWaermebedarf.BrauchwasserAusGenerator`;
  dieselbe Methode rechnet die Leiste „monatlicher Verlauf" (`BedarfsVorschauCtrl.ProjektVorschau` mit
  optionalem Arbeitsstand). Der Codeblock in 2.2 gibt die Wirkung wieder, nicht den Wortlaut.
- **(b) Weiche: Nullzone und Abbruch (2.2).** Eine abgelehnte Zone (oder Zirkulation) trägt 0, jede
  Ablehnung steht als Warnung „Zone „…" trägt 0: …" im Protokoll, und der Lauf rechnet die übrigen Zonen
  weiter; die Vorschau nennt dieselben Zonen in ihrer Meldung. Benannt **abgebrochen** wird nur, wenn der
  Generator für das Projekt nicht rechnen kann: Tww-Tabellen fehlen, die Parameter tragen keine
  Katalogversion, oder Lesen, Katalog oder Rechnen scheitern unerwartet. Dann meldet der Kern den Grund
  als Fehler, `SimulationWaermebedarf.Fehlertext` nennt ihn, Lauf und `SimulationLaufCtrl.Bedarf`
  brechen ab, und Summen, Summenvektor und Kanäle stehen auf 0. Begründung: Ohne Tabellen oder
  Parametersatz rechnet keine Zone; ein Lauf, der das ganze Projekt still auf 0 setzte, wäre der
  Rückfall, den 2.2 ausschließt. Die Projektzusammenfassung der Startseite zeigt dann den Grund statt
  einer Zahl, die Ergebnisvorabrechnung trägt ihn ins Protokoll.
- **(c) Gebundenes Gebäude (A8; 4.1, 4.3).** Die Zone übernimmt die Ferien des Gebäudes
  (`Tab_Gebaeude`, nur mit gesetztem Ferienkennzeichen), wenn sie keine eigenen trägt, und seine Fläche
  (`Wohnflaeche_gesamt`, sonst `Nutzflaeche`), wenn sie keine eigene trägt; die Gebäudefläche geht dann
  dem Parameter der Wohnfläche je WE vor, weil sie eine Angabe des Projekts ist. **Gemischte Zonen:**
  Die Gebäudefläche zählt einmal — die eigenen Flächen der Zonen desselben Gebäudes (Bezugsart Fläche:
  Bezugsmenge; sonst WE-Zahl × eigene Wohnfläche je WE) gehen ab, der Rest zu gleichen Teilen an die
  Zonen ohne eigene Fläche; ohne positiven Rest belegt das Gebäude keine Fläche vor, und es gilt N7 (f).
  Gebunden wird nur ein Gebäude des Projekts: `Speichern` lehnt ein fremdes benannt ab
  (`GebaeudeFremd`), der Eingang liest nur Gebäude mit `ID_Projekt` des Projekts.
- **(d) Schreibweg (3.3).** Vor dem ersten Schreiben prüft `Speichern` auch die Projektgrößen gegen
  ihre Wertemengen (`ProjektUngueltig`): Aufzählungen über ihre Mitglieder, Perzentil und Realisierungen
  aus Konstanten in `TwwSchema`, aus denen auch die CHECK-Klauseln der DDL entstehen (DDL-Text
  unverändert). Die Id eines Wohnungstyps einer anderen Zone wird abgelehnt (`WohnungstypUngueltig`),
  eine unbekannte Id legt eine neue Zeile an.
- **(e) Inhaltsvergleich im Projektimport (ZU17; 3.2, Kapitel 9).** Festlegungen der Umsetzung:
  1. Die Provenienzspalten (Quelle, Ausgabe, Version, Herkunftsart je Wertgruppe) gehen **nicht** in
     den Vergleich ein — sie beschreiben einen Wert, sie sind keiner. Eine Zeile mit gleichen Werten und
     anderer Provenienz wird der Zielzeile zugeordnet; das Projekt übernimmt deren Provenienz. Die
     Invariante 2.4 (jede Katalogzeile trägt Provenienz je Wertgruppe) bleibt erfüllt.
  2. Eine Auslieferungszeile am Ziel gilt als gleich (unveränderliche Version).
  3. Verglichen werden `EIGEN`- und `IMPORT`-Zeilen, weil beide beim Anwender änderbar sind.
  4. Eine Nutzungsart kommt auch dann als neue Version, wenn nur ihr Tagesgangsatz abweicht — sie muss
     auf den Satz des Pakets zeigen.
  5. Trägt eine frühere Version „(Import n)" denselben Inhalt, wird sie wiederverwendet.
  6. Kindtabellen und Schlüssel der Tww-Kataloge kommen aus den festen Tabellen des Programms
     (`TWW_KINDER`, `KATALOG_NATURALKEY`, Primärschlüssel `ID`); das Manifest liefert nur den Namen zum
     Zuordnen. Ein abweichendes Manifest lehnt der Import benannt ab, bevor er schreibt.
- **(f) Testnamen (2.4, Kapitel 7).** `ZapfprofilWeicheTests.Die_Energieprobe_zaehlt_Zapfung_und_Zirkulation`
  steht als eigene Methode; `…Netzverluste_gehen_anteilig_auf_den_Generatorkanal` prüft die
  Netzverlustverteilung im Generatorweg in % und kWh/a (Anteil des Brauchwasserkanals = Stundenbetrag ×
  Kanalanteil, Energieprobe ohne Verletzung, Monatssummen ohne Netzverlust). Die Nullzone und der
  Abbruch (b) stehen in `…Ein_fehlender_Parameter_nennt_die_Zone_und_sie_traegt_null`,
  `…Ohne_Katalogversion_bricht_der_Lauf_benannt_ab` und `…Ein_unerwarteter_Fehler_bricht_den_Lauf_benannt_ab`.
  Der Fall in `BedarfsProfileDialogTests` (2.4) kommt mit der Oberfläche.
- **(g) Nicht in dieser Gruppe, mit Grund.** Die Hülle `BedarfsProfileHuelle` übergibt den Arbeitsstand
  noch nicht als Delegat und zeigt die Meldung der Vorschau nicht; das ist Oberfläche (Gruppe 3) und
  heute nicht erreichbar, weil der Weg nur über die Datenbank gesetzt wird. Der Kopf der Testdatenbank
  steht auf WAL (vorbestehend); jeder Leser legt gitignorierte Beidateien an. An der Datenbank ändert
  diese Gruppe nichts.

**Folgen:**

| Punkt | Folge | Verantwortlich | Stufe |
|---|---|---|---|
| (g) | `BedarfsProfileHuelle`: Arbeitsstand als Delegat (5.2), Meldung der Vorschau anzeigen, Titelzusatz im Zapfprofilweg leer oder „Zapfprofilgenerator", bunit-Fall für eine Ablehnung mit sichtbarem Grund | Agent der Stufe Z1 | Z1, Gruppe 3 |
| (b) | Abbruchgrund der Wärmerechnung auf der Ergebnisseite sichtbar machen | Agent der Stufe Z1 | Z1, Gruppe 3 |
| (e) 1 | Frage an den Anwender: Provenienz im Inhaltsvergleich mitvergleichen? Empfehlung: nein, wie umgesetzt | Anwender | vor Z2 |
| (g) | Journalmodus der Testdatenbank im Skript auf DELETE, eigener LFS-Commit mit byte-gleichem Wiederholungsnachweis | Folgeposten mit ZU18 | außerhalb der Z-Stufen |

### N9 (23.09.2026) — Umsetzungsbefunde Z1, Gruppe 3 (Oberfläche)

**Anlass.** Dialog „Brauchwasser-Zapfprofil" (Stufe Einfach mit Vorschau), Einstieg im
Bedarfsprofil-Dialog, Hüllen, Naht und Vorschaubilder sind auf dem Zweig `z1` umgesetzt und
gegengeprüft; die Befunde der Gegenprüfung sind nachgebessert. Wo ein Befund dem Papier oder dem
Mockup widersprach, gilt das Papier. Dieser Nachtrag hält fest, wo die Umsetzung vom Papier abweicht
oder es genauer fasst; der Hauptteil ist an den betroffenen Stellen mit Verweis „(N9)" berichtigt
(5.1, 5.2, 5.5, 5.6). Er enthält **keinen Entscheid** des Anwenders.

**Befunde und Festlegungen:**

- **(a) Diagramme (5.6).** Tagesgang und Wochenprofil zeichnet das neue
  `ChartRenderer.StundenprofileModell` (mehrere Reihen: die erste als Fläche, weitere als Linie in
  ihrer Strichart, Legende oben) — `StundenprofilModell` trägt nur eine Reihe. Den Jahresgang stapelt
  `MonatsStapelModell`. Die Bilder baut `ZapfprofilBilder` (Kern, `Allgemein/Bericht/`), die
  Auswertung der Reihen — größter Monat und Tag, mittlerer Tagesgang je Tagtyp, Woche mit dem größten
  Tagesbedarf — `Zapfauswertung` (Kern, `Allgemein/Zapfprofil/`); die Hülle rechnet keinen Bedarf.
  `SummenlinieModell` kommt mit Z2. `Proben/ChartProben`: fünf Maß-, drei Gegen- und drei
  SVG-Proben, elf Bilder neu, kein altes geändert; die Linux-Messlatte `Messlatte_2026-09-26.sha256` führt sie.
- **(b) Textbündel und Titel (5.1, 5.3).** Drei Bündel: `ZapfprofilTexte` (Dialog),
  `ZapfprofilEinstiegTexte` (Knopf, Titel der Überlagerung, Optionsgruppe, Hinweise und Vermerk der
  Leiste im Bedarfsprofil-Dialog) und `ZapfprofilBildtexte` (Vorschaubilder). Der Dialog trägt den
  Titel „Brauchwasser-Zapfprofil – ‹Projekt›"; eingebettet trägt ihn die Überlagerung allein. Auf dem
  Zapfprofilweg hängt der Ergebnisdialog „Zapfprofilgenerator" statt eines Profilnamens an seinen
  Titel (`BPF_TITEL_ZAPFPROFILGENERATOR`). 173 `ZPG_`- und neun neue `BPF_`-Schlüssel je Sprache.
- **(c) Weich gesperrt (5.1).** Die Stufen Erweitert und Experte, der Reiter Dauerlinie und die
  Knöpfe „Stochastisch rechnen" und „Auslegung…" stehen da, weich gesperrt mit Grund
  (`ZPG_GRUND_NOCH_NICHT`); der Versuch meldet ihn als leise Zeile. Die leise Zeile unter dem
  Eingabeblock verweist in Z1 nicht auf die gesperrte Stufe („Weitere Angaben stehen auf den Vorgaben
  des Katalogs."); den Verweis auf Erweitert bringt Z4 zurück.
- **(d) Naht und Behälter (5.2, 5.5).** Die Schale steuert über die Naht `Zapfprofilwege`
  (Arbeitsstand, Übernehmen, Sperrgrund) bei; `ZapfprofilHuelle.Einstieg` liefert daraus einen
  `ZapfprofilEinstieg` (Delegaten oder benannter Grund), `ZapfprofilHuelle.Einhaengen` setzt ihn samt
  Optionsgruppe in den Parametersatz des Bedarfsprofil-Dialogs. Den Arbeitsstand bis zum OK hält ein
  `ZapfprofilBehaelter` je Öffnen (unverändert = nichts schreiben; die Optionsgruppe stellt nur den
  Weg um). Ohne Delegat, aber mit Grund (kein gespeichertes Projekt, ZU10; Plattform, A11) steht der
  Knopf weich gesperrt da und nennt ihn; ganz ohne Behälter gibt es weder Knopf noch Optionsgruppe.
- **(e) Schreibweg im OK (5.2).** Das OK des Bedarfsprofil-Dialogs schreibt, **bevor** er schließt:
  Parameter `Speichern` (`Func<string>`, leer = geschrieben), belegt von `BedarfsProfileHuelle.Gaben`
  bei Brauchwasser mit `ZapfprofilHuelle.Schreibweg` — die Projektzeilen des Dialogs als Zuordnungen
  und der Behälter in EINEM `DbVorgang`. Lehnt `ZapfprofilCtrl.Speichern` ab oder scheitert
  `Del/Add_Projekt_Brauchwasser`, rollt der Vorgang zurück, der Dialog nennt den Grund als Banner und
  bleibt offen; Zuordnungen und Arbeitsstand bleiben, ein zweites OK schreibt erneut. Die
  Datenbankmeldung der Zuordnungen wird dabei gesammelt (`DataRepository.EngineModus`) statt als
  Plattformfenster aus dem Oberflächenereignis gezeigt (`ZPG_MSG_ZUORDNUNG_NICHT_GESPEICHERT`).
  Startseite und Gebäudekatalog schreiben nach dem Schließen nichts mehr; der Katalog setzt nur das
  Änderungsdatum des Projekts.
- **(f) Gebäudekatalog aus der Verwaltung (5.2).** Im Modus Admin reicht `GebaeudeKatalogHuelle`
  keinen Behälter: Der Bedarfsprofil-Dialog zeigt weder Knopf noch Optionsgruppe, sein OK schreibt nur
  die Zuordnungen des geöffneten Projekts, wie der Bestand.
- **(g) ZU5 schon in Z1 (Kapitel 9, Risiko 8).** Der Hinweis `NETZVERLUST_UND_ZIRKULATION` steht im
  Rechenweg (`ZapfprofilRechner`), nicht erst in der Warnlogik von Z4: bei Netzverlusten des Projekts
  größer 0 (`Tab_Einstellungen`, über `ZapfprofilCtrl.Eingang` als
  `Zapfprofileingang.NetzverlusteProjekt`), einer nicht abgelehnten Zone mit Zirkulation und einem
  Zirkulationsverlust größer 0. Nicht blockierend; Laufprotokoll und Vorschau tragen ihn über den
  vorhandenen Meldungsweg (`ZPG_HINW_NETZVERLUST_UND_ZIRKULATION`); die Netzverlustverteilung bleibt
  unberührt. Die Warnliste von Z4 übernimmt ihn.
- **(h) Kennzahlen (4.6, Mockup Abschnitt 3).** Der Reiter zeigt Zapfung, Zirkulation mit Vermerk,
  Brauchwasser gesamt mit Zirkulationsanteil, Tagesmittel, je Zone den spezifischen Wert, für die
  Summe Volllaststunden und den größten Stundenwert mit Vermerk, „Stunden über Schwelle" nur mit
  Schwelle (`Zapfkennzahlen.SchwelleKw`), die Gleichzeitigkeit mit „—" und Vermerk in der **Bilanz** —
  so im Mockup; der Befund der Gegenprüfung, sie unter die Stochastik zu stellen, widersprach ihm —
  und die Gruppe Stochastik mit der Erklärzeile der Stufe Einfach (`ZPG_KZ_STOCHASTIK_ERKLAERUNG`).
  Die drei Zeilen „nach dem Lauf" (P50 bis P99, Gleichzeitigkeit als Ergebnis, Konsistenzprobe) sind
  im Mockup Zeilen der Stufe Experte und kommen mit Z3/Z4. Der Mengenvermerk „m³/a" am Jahresbedarf
  entfällt: Der Kern weist kein Volumen je Jahr aus. Die Temperatur der Literanzeige (`θ_Anzeige`,
  4.0) und die Schwelle der Stundenzählung setzt der Eingang in Z1 nicht — Literangabe und
  Schwellenzeile erscheinen erst mit ihnen.
- **(i) Vorschau je Zone (5.1).** Der Tagesgang einer Zone mittelt mit ihrem wirksamen Kalender
  (`ZonenErgebnis.Kalender`, samt Ferien, auch denen eines gebundenen Gebäudes, N8 c); die Summe
  mittelt über den Kalender der Klimaregion ohne die Ruhetage irgendeiner Zone
  (`Zapfauswertung.OhneRuhetage`). Beide Monatsschichten des Ergebnisdialogs kommen aus dem Kern
  (`SimulationWaermebedarf.Waermebedarf_Brauchwasser_Zapfung_Monat` neben `…_Zirkulation_Monat`); die
  Hülle rechnet keine Schicht.
- **(j) Zonennamen und Meldungen.** Das OK lehnt doppelte Zonennamen ab (`ZPG_MSG_ZONE_NAME_DOPPELT`) —
  Protokoll und Meldungen nennen Zonen beim Namen. Die Vorschau gibt jeder Meldung die Position ihrer
  Zone mit, wo der Name sie eindeutig bestimmt (`ZapfprofilMeldung.Position`); der Dialog ordnet
  darüber zu, eine Meldung ohne eindeutige Zone steht bei den allgemeinen Meldungen.
- **(k) Zonenliste (5.1).** Die Zonenliste ist die Haustabelle `<table class="epos-raster">` in
  `.epos-raster-huelle` statt des Bausteins `Raster`: `Raster` (QuickGrid) führt weder einen
  Tabellenfuß für die Summenzeile noch die Markierung der gewählten Zeile; dieselbe Bauform trägt die
  Projektliste des Bedarfsprofil-Dialogs. Die Liste zieht auf `Raster` um, sobald der Baustein einen
  Fuß führt (dann mit Rasterprobe). Das Zahlenfeld der Bezugsgröße trägt `epos-feld--kurz` schon durch
  den Baustein `Zahlenfeld`.
- **(l) Logbuch (5.8).** Der Logbuch-Satz der Stufe Z1 steht in 5.8 und geht mit der Statuszeile der
  Stufe Z1 in deren Punkt „Logbuch"; eine eigene Datei unter `Projekte/Wiki/` gibt es dafür nicht.

**Zu N8, Folgen (b) und (g) — erledigt:**

- (g) Arbeitsstand als Delegat über den Behälter (d); die Meldung der Vorschau in der Leiste
  „Simulation" (`ZapfprofilHuelle.Leistenmeldung`, Parameter `SimulationMeldung`); Titelzusatz
  „Zapfprofilgenerator" (b); bunit-Fall
  `BedarfsProfileDialogTests.Die_Leiste_rechnet_den_Zapfprofilweg_und_zeigt_seine_Meldung` (Zone, die
  0 trägt, mit sichtbarem Grund), dazu
  `ZapfprofilEinstiegTests.Die_Leiste_rechnet_den_Arbeitsstand_und_nennt_eine_Nullzone`.
- (b) Der Abbruchgrund der Wärmerechnung steht als Banner im Bedarfsprofil-Dialog
  (`BedarfsProfileDialogTests.Ein_Abbruch_des_Zapfprofilwegs_nennt_den_Grund`); Projektzusammenfassung
  und Ergebnisvorabrechnung nennen ihn nach N8 (b).

**Folgen:**

| Punkt | Folge | Verantwortlich | Stufe |
|---|---|---|---|
| (a) | die elf Zapfprofilbilder in die Linux-Messlatte aufnehmen — **erledigt:** `Proben/ChartProben/Messlatte_2026-09-26.sha256` (183 Hashes) führt sie, alle älteren Zeilen gleich (Verfahren in `Proben/ChartProben/LIESMICH.md`) | Orchestrator | mit der Statuszeile Z1 |
| (l) | Logbuch-Satz aus 5.8 (Z1) als Punkt „Logbuch" der Statuszeile Z1; Versionsnummer beim Anwender erfragen | Orchestrator | mit der Statuszeile Z1 |
| (c) | Verweis der leisen Zeile auf die Stufe Erweitert zurückholen | Agent der Stufe Z4 | Z4 |
| (g) | Hinweis ZU5 in die Warnliste übernehmen | Agent der Stufe Z4 | Z4 |
| (h) | `θ_Anzeige` und Schwelle der Stundenzählung in den Eingang; Zeilen der Stochastik „nach dem Lauf" | Agenten der Stufen Z3 und Z4 | Z3, Z4 |
| (k) | Zonenliste auf `Raster`, sobald der Baustein einen Tabellenfuß führt (mit Rasterprobe) | Oberfläche | offen |

### N10 (23.09.2026) — Umsetzungsbefunde Z2, Gruppe 1 (Rechenweg der Auslegung)

**Anlass.** Bedarfstag, Wochenreihe, Summenlinie, DIN-4708-Kennzahl, Speicherauslegung nach V4,
Großanlage, Auslegungsergebnis mit Fassade und der unabhängige Referenzfall sind auf dem Zweig `z2`
umgesetzt und gegengeprüft; die Befunde der Gegenprüfung sind nachgebessert. Wo ein Befund dem Papier
widersprach, gilt das Papier. Dieser Nachtrag hält fest, wo die Umsetzung vom Papier abweicht oder es
genauer fasst; der Hauptteil ist an den betroffenen Stellen mit Verweis „(N10)" berichtigt (2.1, 4.0,
4.5, 4.7). Er enthält **keinen Entscheid** des Anwenders.

**Befunde und Festlegungen:**

- **(a) Fassade (2.1).** `ZapfprofilAuslegung.Rechnen(eingang, katalog, auslegungseingang)` bildet
  Wochenreihe und Bedarfstag je Topologiegruppe selbst; die Signatur in 2.1 mit `Bedarfstag`,
  `Wochenreihe` und `Zapfensemble` entfällt, das Ensemble kommt mit Z3. `Auslegungseingang` trägt
  DIN-4708-Katalog, Bedarfstage, Nenninhalte und die Laufangaben (i). Die Gruppe nennt ihre
  Speichertemperatur (`Speichertemperatur`) und das Laufzeitfenster der Zirkulation
  (`ZirkulationLaufzeit`).
- **(b) DIN-4708-Profil (4.5).** Der Bedarfstag der Quelle (3) entsteht aus W_z(N) der Kennzahl und den
  Zapfblöcken des Parametersatzes (`DIN4708.Profil.Bloecke`, `DIN4708.Profil.Block.{k}.Beginn|Dauer|Anteil`),
  nicht aus einer Katalogzeile der Art 3. Festlegung: Eine Katalogzeile der Art 3 würde über ihre
  Bezugsmenge linear skaliert, W_z(N) wächst aber nichtlinear in N; eine gespeicherte Zeile der Art 3
  rechnet deshalb nur als gewählter Katalogtag, die Vorgaberegel nimmt das Profil aus W_z(N).
- **(c) DIN 1988-300 nachrichtlich (4.5 c) — offen.** Der Rohrnetz-Spitzendurchfluss braucht die Summe
  der Entnahmearmaturen ΣV̇_A; das Datenmodell trägt sie nicht. Der Wert entfällt bis dahin (Folgen).
- **(d) Wohnungsstation (4.5).** Die Spitze je Einheit folgt mit dem Ensemble (Z3); Z2 weist die Summe
  mit dem Hinweis `WOHNUNGSSTATION_JE_EINHEIT` aus.
- **(e) Speichertemperatur (4.0) — berichtigt.** Die Umsetzung setzte θ_Speicher immer auf die
  Mindesttemperatur nach DVGW W 551, das Papier nur bei Großanlage. Jetzt wählt die Fassade EINE
  Temperatur je Gruppe (`Speichertemperaturwahl`): Projektwert, sonst bei Großanlage
  `W551.Mindesttemperatur`, sonst im Schnellpfad `A100.Vereinfachung.Speichertemperatur`, sonst der neue
  Parameter `Speicherauslegung.Speichertemperatur_Vorgabe` (INEKON-Setzung; im Testkatalog fiktiv).
  Summenlinie, V_DIN, Verfahrensvergleich, Band und Reihenfolge rechnen mit ihr — der Schnellpfad nahm
  seine Temperatur zuvor nur für die Summenlinie. Festlegungen: Die Großanlage geht dem Schnellpfad vor
  (dessen Setzung hält die Mindesttemperatur nicht zugesichert ein); erkennt erst das empfohlene Volumen
  die Großanlage, rechnet die Gruppe einmal neu mit der Mindesttemperatur (Hinweis
  `SPEICHERTEMPERATUR_GROSSANLAGE`), die Einstufung gilt dann am neuen Volumen; die Warnung „unter der
  Mindesttemperatur" entfällt bei erkannter Kleinanlage.
- **(f) Großanlage (4.7).** Erkannt wird am empfohlenen Volumen als Nenninhalt, sonst am Punkt der
  Summenlinie (`NenninhaltL ?? V`), vor dem Projektvolumen nur, wenn das Projekt keines nennt; vorab aus
  Projektvolumen und Leitungsinhalt. Ein ungültiger Leitungsinhalt ist ein benannter Hinweis.
- **(g) N_GLF (4.7).** Die Gültigkeitsgrenze des GLF-Verfahrens ist der Parameter
  `Speicherauslegung.GLF_Gueltigkeitsgrenze` (im Testkatalog fiktiv), kein fester Wert; die Festlegung an
  Vorlage und Summenlinie steht aus (Folgen). Das Verfahren trägt stets den Gültigkeitshinweis „ohne
  Wannen eingeschränkt" (`GUELTIGKEIT_GLF_WANNEN`).
- **(h) Zeitkonstante (4.5 a).** A1 setzt c_w in kJ/(kg·K) an; mit m = V (1 kg/l) und c_w in Wh/(l·K)
  gilt `τ = V · c_w · 3,6 / (U·A) · k_τ`. Der Schlüssel `A100.Zeitkonstante.Koeffizient` ist der
  A1-Koeffizient [min·W/kJ], keine Umrechnung von Wh; ein Koeffizient 60 hatte das verdeckt.
- **(i) Wertepaarkurve und Übertrager (4.5 a).** Die Kurve rastert die Erzeugerleistung bis zur
  Erzeugerleistung des Projekts (ohne Erzeuger bis zur Leistung des Auslegungspunkts) und sucht je
  Stufe mit `Φ_N(V) = min(Φ_Erzeuger, Φ_Ü(V))` — ein festes Φ_N über Φ_Ü(V) war nicht baubar; der
  letzte Punkt ist der Auslegungspunkt. Die Schätzformel der Übertragerfläche hat je Erzeugerart ein
  Schlüsselpaar (`A100.Uebertragerflaeche.Kessel.*` nach NA.1, `….Waermepumpe.*` nach NA.2), U je
  Werkstoff (`A100.Uebertrager.U.Stahl`, `….Edelstahl`). Erzeugerart und Werkstoff sind Laufangaben
  des `Auslegungseingang`; fehlt die gebrauchte Angabe, lehnt die Summenlinie benannt ab
  (`UebertragerUnbestimmt`), statt ein Paar zu raten.
- **(j) Bedarfstag aus dem Katalog (4.5).** Skaliert wird auf die Bezugsmenge der Gruppe nur, wenn alle
  Zonen dieselbe Bezugsart tragen; Mengen verschiedener Bezugsarten werden nicht summiert (benannte
  Ablehnung). Festlegung: Ein Katalogtag gilt bei θ_KW,A des Parametersatzes
  (`A100.Kaltwasser.Auslegung`) und wird auf θ_KW,A des Projekts umgerechnet,
  `f = (θ_Zapf − θ_KW,A) / (θ_Zapf − θ_KW,A,Katalog)` mit der Zapftemperatur der Zonen; verschiedene
  Zapftemperaturen machen die Umrechnung mehrdeutig und werden benannt abgelehnt. Die Tabelle trägt
  weder Bezugsart noch Temperaturen (Folgen).
- **(k) Nenninhalt und Füllstand (4.7).** Über dem Ende der Nenninhaltsliste gilt „Mehrspeicheranlage
  prüfen" auch ohne Raster-Parameter (der Nenninhalt bleibt dann offen); der Parameter wird nur gelesen,
  wenn er gebraucht wird. Der Füllstand bezieht sich auf das empfohlene Volumen (Nenninhalt des
  Summenlinienpunkts, sonst der Punkt; ohne Punkt der Nenninhalt des Bands), beschriftet in
  `FuellstandBezug`.
- **(l) Summenkontrolle und Textformat (4.7).** Die Wochenreihe hält die Summe ihrer 168 Stundenwerte
  gegen die Summe der Tagesmengen ihres Fensters (relativ 1e-9); eine Abweichung ist die Warnung
  `SUMMENKONTROLLE`. Zahlen in Rechenweg-Sätzen und Hinweisen stehen in invarianter Kultur wie die
  Vermerke des Mengengerüsts.
- **(m) Wachen (2.4, Kapitel 6).** `ZapfprofilTrennungWacheTests` wertet eine Zeile mit `*` nur innerhalb
  `/* … */` als Kommentar und prüft Zahlenlisten auch als Rückgabe, Eigenschaft und Feld, mit benannten
  Ausnahmen je Glied (`Summenliniennachweis.InhaltKwh`, `Speicherauslegungsergebnis.DefizitKwh`,
  `Nenninhaltsliste.WerteL` und `.Aus`). **Übergang:** Die Repo-Testdatenbank trägt den Testkatalog der
  Stufe Z2 erst nach dem Nachzug beim Merge; bis dahin darf der erste Lauf des Einspielskripts in
  `TwwKatalogWacheTests` anlegen (datierter Vermerk im Test), der zweite und dritte müssen 0/0 melden —
  trägt die Repo-Datei jeden Parameterschlüssel, gilt 0/0 von selbst ab dem ersten Lauf. Der
  Bedarfstagkatalog wird in `KatalogpflegeTests` auf einer Arbeitskopie nach dem Skriptlauf gezählt
  (heute und nach dem Nachzug drei Sätze).
- **(n) Referenzfall (Kapitel 7).** Das Skript
  `EPOS.Kern.Tests/Proben/Zapfprofil/auslegung_referenzfall_bauen.py` rechnet neben drei Summenlinien
  und der Speicherauslegung einen Fassadenfall (zwei Zonen am Durchfluss: Wochenreihe, f_KW,A, Wahl und
  Umrechnung des Bedarfstags, Laufzeitfenster); Abweichung 0 auf 1e-9, ein zweiter Lauf schreibt
  dieselben Bytes.

**Folgen:**

| Punkt | Folge | Verantwortlich | Stufe |
|---|---|---|---|
| (m) | Nachzug der Testdatenbank mit dem Einspielskript beim Merge (LFS), danach Wache und Katalogzählung ohne Übergang | Orchestrator | Merge Z2 |
| (c) | ΣV̇_A der Entnahmearmaturen ins Datenmodell, dann der Rohrnetz-Spitzendurchfluss nach DIN 1988-300 nachrichtlich | Agent der Stufe Z2 | Z2, Gruppe 2 oder Folgeposten mit Schemaschritt |
| (d) | Spitze je Wohnungsstation aus dem Ensemble | Agent der Stufe Z3 | Z3 |
| (i) | Erzeugerart und Werkstoff speichern oder aus dem Projekt ableiten (Schemaschritt oder `ZapfprofilCtrl`) | Agent der Stufe Z4 | Z4 |
| (e), (g), (i) | Auslieferungswerte für `Speicherauslegung.Speichertemperatur_Vorgabe`, `Speicherauslegung.GLF_Gueltigkeitsgrenze` (an Vorlage und Summenlinie festgelegt) und die Übertragerpaare NA.1/NA.2 ins Katalogpaket | Katalogpflege | mit dem Auslieferungskatalog |
| (j) | Bezugsart am Bedarfstag (Schemaschritt); der Konstruktor legt seinen Tag bei θ_KW,A des Parametersatzes ab | Agent der Stufe Z2 | Z2, Gruppe 2 bzw. Z4 |

### N11 (23.09.2026) — Umsetzungsbefunde Z2, Gruppe 2 (Oberfläche der Auslegung)

**Anlass.** DTO und Textbündel der Überlagerung „Auslegung", ihre Hülle, die Überlagerung samt
Konstruktor, das `SummenlinieModell` und die Bilder der Auslegung sind auf dem Zweig `z2` umgesetzt und
gegengeprüft; die Befunde der Gegenprüfung sind nachgebessert. Wo ein Befund dem Papier widersprach,
gilt das Papier. Dieser Nachtrag hält fest, wo die Umsetzung vom Papier abweicht oder es genauer fasst;
der Hauptteil ist an den betroffenen Stellen mit Verweis „(N11)" berichtigt (3.1, 4.5, 4.7, 5.6,
Kapitel 7). Er enthält **keinen Entscheid** des Anwenders.

**Befunde und Festlegungen:**

- **(a) Diagramme (5.6).** Summenlinie des Bedarfstags, Wertepaarkurve und maßgebende Woche der
  Stundenbilanz zeichnet EIN neues Modell, `ChartRenderer.SummenlinieModell` (x-Größe mit eigener
  Teilung, vorzeichenfähige linke Achse, zweite Achse, Marken als Strecke oder Punkt) — nicht
  `KennlinienModell` und `SpeicherbetriebModell`, die weder eigene x-Stellen noch Marken führen. Die
  Reihen bildet `ZapfprofilBilder` aus den Ergebnissen des Kerns. `Proben/ChartProben`: vier Maß-, drei
  Gegen- und drei SVG-Proben, zehn Bilder neu, kein altes geändert; die Linux-Messlatte
  `Messlatte_2026-09-26.sha256` führt sie.
- **(b) Nenninhalte (3.1).** Die Liste bleibt die Einstellung `Zapfprofil.Nenninhalte`; ihre Vorgabe
  kommt aus dem Parametersatz (`Speicherauslegung.Nenninhalt.Liste.{k}`, geordnet nach k), nicht aus dem
  Code — keine Liste im Quelltext (Kapitel 6). Eine ungültige Einstellung oder Vorgabe nennt ein Hinweis
  (`NENNINHALTE_EINSTELLUNG_UNGUELTIG`, `NENNINHALTE_PARAMETER_UNGUELTIG`); ohne Liste wird nicht gerundet
  (`NENNINHALTE_FEHLEN`).
- **(c) Stufe und Schnellauslegung (4.5).** Die Überlagerung öffnet in dieser Fassung stets in der Stufe
  Einfach (die Gaben der Hülle setzen sie fest); jeder Punkt trägt deshalb die Marke „Schnellauslegung".
- **(d) Eingaben des Verfahrensvergleichs (4.7).** Ladeleistung (auto oder manuell), Personen,
  Kennzahl N, nutzbarer Anteil und Zuschlag stehen als Lesezeile über der Tabelle, nicht als Felder; das
  Muster `Schaetzwert` wirkt im Kern. Der Bezug des Füllstands ist nicht wählbar — er folgt N10 (k) und
  steht beschriftet in der Kachel.
- **(e) Quellen des Bedarfstags (4.5).** A100-Referenzprofil und Ecodesign-Zapfprofil stehen in der Wahl
  benannt gesperrt, solange der Katalog keine Zeile ihrer Art führt: die Zeilen der A100 folgen mit dem
  Katalogpaket (K1/K8), das Ecodesign-Zapfprofil mit Z3; eine Katalogzeile ihrer Art steht als Katalogtag
  da. Ein Normtag des Katalogs ist gesperrt, er rechnet als DIN-4708-Profil (N10 (b)). Jeder gesperrte
  Eintrag trägt seinen eigenen Grund.
- **(f) Nicht gerechnete Werte benannt (4.5).** Der Rohrnetz-Spitzendurchfluss nach DIN 1988-300 steht in
  der Karte (c) als gesperrte Zeile mit Grund (N10 (c)); der Konsistenzhinweis (stochastische Spitze
  gegen die Leistung des Summenlinienpunkts) steht in der Warnliste der Speichergruppe gesperrt — er
  braucht das Perzentil (Z3).
- **(g) Vorgaberegel und Stundenprofil (4.5).** Ohne konstruierten Tag öffnet die Überlagerung den
  Konstruktor, sobald der Fall eintritt — beim Öffnen und nach einer Neuberechnung, die ihn herbeiführt,
  nicht nach jedem Abbrechen erneut. Ein Tag aus dem Stundenprofil trägt je Gruppe das Warnbanner
  „Spitzen unterschätzt"; OK fragt vor der Übernahme nach.
- **(h) Der Punkt ist Ergebnis, nicht Eingabe (4.7, N10 (f)).** Die Überlagerung rechnet ohne
  übernommenen Punkt; ein alter Punkt — im Arbeitsstand oder gespeichert — steuerte sonst
  Großanlagenerkennung, Speichertemperatur und Warnliste und damit den neuen Punkt. In ihr erkennt die
  Großanlage deshalb am Nachweisvolumen, sonst am empfohlenen Volumen; die Regel der Großanlage bleibt im
  Kern. Der Punkt geht nur mit OK und Speichern in die Projektgrößen. Ändert der Anwender danach eine
  Zone so, dass sich die Auslegung ändert (neue, doppelte oder entfernte Zone, Nutzungsart,
  Bezugsgröße, Niveau), ist der Punkt überholt: eine leise Zeile im Zapfprofil sagt es, das Speichern
  verwirft ihn — auch einen Punkt, den schon der Stand beim Öffnen trug —, ein neues OK der Auslegung
  setzt ihn wieder.
- **(i) Laufangaben (4.5 a, N10 (i)).** Erzeugerart und Werkstoff sind in der Überlagerung Laufangaben:
  gewählt, nicht gespeichert, beim nächsten Öffnen „keine Angabe". Die Erzeugerart schlägt der
  Anlagenbestand vor, wenn er eindeutig ist; die Zeile unter dem Feld nennt die Herkunft.
- **(j) Konstruktor (4.5).** Solange der Zapfprofil-Dialog offen ist, trägt der Entwurf die Zeilen, aus
  denen er entstand; ein erneutes Öffnen des Konstruktors beginnt mit ihnen. Der Arbeitsstand des Kerns
  trägt nur Ereignisse — öffnet der Anwender das Zapfprofil erneut, beginnt der Konstruktor mit einer
  Zeile. Den Namen prüft der Konstruktor lesend gegen die Katalogversion und nennt einen freien; der
  Schreibweg prüft ihn erneut. Jedes Feld der Zeilentabelle trägt „Spalte, Zeile n" als Beschriftung;
  eine Fehleingabe hält das OK an und wird benannt; die letzte Zeile bleibt, der Versuch nennt den Grund.
- **(k) Sprache der Sätze (Kapitel 6, N10 (l)).** Beschriftungen, Titel und Gründe der Oberfläche stehen
  in beiden Sprachen; die Wache `HuellenTextschluesselWacheTests` hält jeden Literalschlüssel der Hüllen
  gegen beide Ressourcendateien. Die Sätze des Kerns — Rechenweg-Sätze, Hinweistexte der Warnliste,
  Gründe der Karten und Empfehlungen — bleiben nach N10 (l) deutsch und in invarianter Kultur, auch in
  der englischen Oberfläche; die Titel der Warnliste übersetzt die Hülle.

**Folgen:**

| Punkt | Folge | Verantwortlich | Stufe |
|---|---|---|---|
| (a) | die zehn Auslegungsbilder in die Linux-Messlatte von `Proben/ChartProben` aufnehmen — **erledigt:** `Messlatte_2026-09-26.sha256` (183 Hashes) führt sie (Verfahren in `Proben/ChartProben/LIESMICH.md`) | Orchestrator | Merge Z2 |
| (b) | neutrale Auslieferungswerte der Nenninhaltsliste (`Speicherauslegung.Nenninhalt.Liste.{k}`) ins Katalogpaket | Katalogpflege | mit dem Auslieferungskatalog |
| (c) | die Marke „Schnellauslegung" nach der Stufe des Dialogs, sobald Erweitert und Experte wählbar sind | Agent der Stufe Z4 | Z4 |
| (d) | Eingaben des Verfahrensvergleichs als Felder (auto/manuell), Bezug des Füllstands wählbar | Agent der Stufe Z4 | Z4 |
| (e) | Katalogzeilen der Art A100-Referenzprofil mit dem Katalogpaket; Ecodesign-Zapfprofil als Quelle | Katalogpflege; Agent der Stufe Z3 | nach K1/K8; Z3 |
| (f) | Konsistenzhinweis mit dem Perzentil; DIN 1988-300 nach Folge (c) von N10 | Agent der Stufe Z3; Folgeposten mit Schemaschritt | Z3; offen |
| (i) | Erzeugerart und Werkstoff speichern oder ableiten (Folge (i) von N10) | Agent der Stufe Z4 | Z4 |
| (j) | die Zeilen eines konstruierten Tags über das Schließen des Zapfprofils hinaus tragen (am Arbeitsstand des Kerns oder am Bedarfstag) | Agent der Stufe Z4 | Z4 |
| (k) | die Sätze des Kerns in Oberflächensprache und -kultur (Kennung und Werte statt fertiger Sätze) | Agent der Stufe Z4 | Z4 |

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

**Umsetzungsstand und Abweichungen:** N2 bis N4 (Kapitel 11); T1 ist Schritt 103 (N4).

### N12 (24.09.2026) — Umsetzungsbefunde Z3 (Stochastik): Kern, Katalog, Oberfläche

**Anlass.** Stufe Z3 nach Kapitel 7: portabler Zufall, Zapfereignisgenerator mit gestutztem Mittel,
Ensembles der Jahresreihe und des Bedarfstags, Perzentil je Topologiegruppe, Gleichzeitigkeit als
Ergebnis, Schemaschritt T2 (115) mit `Tab_TwwZapfkategorie_STAMM`, Katalog der Zapfkategorien,
Ecodesign-Zapfprofil, Oberfläche der Stochastik. Drei Gruppen (Kern · Katalog und Schema · Oberfläche)
durch Agenten mit `model: opus` im Worktree `z3`, je Gruppe eine Gegenprüfung; Protokoll
[Z3](../ueberholt/Protokolle/Zapfprofilgenerator/2026-09-24_Z3_Stochastik.md), Statuszeile #453.

**Anwenderentscheid ZU19 (23.09.2026, wörtlich: „nehme geringfügig abweichende vdi werte ins
repository").** VDI-Werte dürfen als *geringfügig abweichende* Werte ins Repositorium; die Originale
bleiben lokal unter `Referenzlaeufe/Normzahlen/` (gitignoriert). Festlegungen dazu:
1. **Ableitung reproduzierbar, Rückrechenbarkeit zulässig (Anwender, 24.09.2026: „reproducible
   acceptable").** Es gilt die dokumentierte Regel: v' = v · (1 + δ) mit δ zyklisch je Zeilenindex aus
   (+4; −3; +5; −4; +3; −5 %), Rundung auf die Stellenzahl der Quelle (mindestens zwei signifikante
   Ziffern), Verteilungen renormiert, kein Wert gleich dem Original (sonst nächstes δ, dann eine Stelle
   feiner), jeder Wert 0,19–5,88 % vom Original entfernt (497 Werte). Die Gegenprüfung hatte gezeigt,
   dass sich rund drei Viertel der Originale aus JSON und Regel zurückrechnen lassen; der Anwender hat
   das ausdrücklich zugelassen. Skript `Referenzlaeufe/Skripte/normzahlen_abgeleitet_bauen.py` (läuft nur
   mit den lokalen Originalen, zwei Läufe schreiben dieselben Bytes), Ausgabe
   `Referenzlaeufe/Skripte/tww_katalogwerte_abgeleitet.json` (committet).
2. **Herkunftsart `FIKTIV`, Quelle „VDI 6002 Blatt 1/2 (abgeleitet)".** Die Zeilen sind keine
   Normwerte und keine Eigenkonstruktion; sie bleiben bis zum Ergebnis von K8 aus der
   Auslieferungsvorlage (Regel 6 (b)). Ob die abgeleiteten Werte ausgeliefert werden, ist eine eigene
   Frage: **ZU20** (Anwender, nach K8).
3. **Umfang.** Nur VDI 6002; VDI 4655 folgt mit Z4b unter derselben Regel. DIN 4708 und DIN EN 12831
   Beiblatt A100 sind DIN-Tabellen und bleiben K1/K8-gesperrt.
4. **Hinweis an den Anwender (gegeben).** Auch geringfügig veränderte Tabellen können urheberrechtlich
   als Bearbeitung gelten; K8 bleibt offen.

**Befunde und Festlegungen (Kern, Gruppe 1):**

- **(a) Realisierungsseed (4.4).** Es gilt `SplitMix64(SplitMix64(Seed) ⊕ r)` statt
  `SplitMix64(Seed ⊕ r)`, damit benachbarte Seeds keine Realisierungen teilen.
- **(b) Ereigniszahl (4.4).** Poisson je Kategorie und Tag (Exponentialabstände nach von Neumann, ohne
  transzendente Funktion) statt Bernoulli je Minute; der Erwartungswert ist exakt, p wird nicht bei 1
  gekappt.
- **(c) Gestutztes Mittel (4.4, N7).** Das exakte Irwin-Hall-Mittel als Stückpolynom statt μF + σf;
  `Kappung_l_min` je Kategorie begrenzt den Volumenstrom nach oben, die λ-Kalibrierung rechnet mit dem
  doppelt gestutzten Mittel. Nur Grundrechenarten, bitgleich auf jeder Plattform; veröffentlichter
  Prüfvektor für xoshiro256** (Zustand {1,2,3,4}) im Test und im Referenzskript.
- **(d) Einheiten n_E je Bezugsart.** Wohnungstabelle → Σ Anzahl; Wohneinheiten → Bezugsmenge;
  Personen → Bezugsmenge / Personen je WE; Fläche → Bezugsmenge / Wohnfläche je WE; sonst die
  Bezugsmenge; kaufmännisch gerundet, mindestens 1.
- **(e) Gleichzeitigkeit (4.4).** GLF_V: die erste Einheit jeder Zone rechnet mit dem Anteil ihrer
  Tagesmenge an Φ_N, Speicherverlust und Zirkulation; Σ_i = Σ_Zonen n_E · P_p(V_Einheit). GLF_P über
  die Minutenspitze. Beide sind Ergebnis, kein Eingabefaktor.
- **(f) Konsistenzhinweis (4.4, N11 (f)).** Er vergleicht das Perzentil der größten Stundenleistung des
  Bedarfstags mit Schwelle · Φ_N des Summenlinienpunkts; er entscheidet die Rechnung nicht.
- **(g) Bilanz ist das Jahr zum Seed (2.3, 4.4).** In die Bilanz geht die Realisierung r = 0 mal
  E_det/E_0; die R Jahre dienen nur der Konsistenzprobe (Mittel gegen die deterministische Reihe,
  s_R). Die erste Fassung gab das Ensemblemittel in die Bilanz; die Gegenprüfung hat das
  richtiggestellt. Die Bilanz hängt nicht von R ab.
- **(h) Obergrenzen (benannt als `StochastikUngueltig`).** Bedarfstag höchstens 100 000
  Realisierungen; R · Σ n_E höchstens 10⁷ Einheitentage; Jahresreihe höchstens 1000 Jahre; je Zone
  höchstens 10⁶ Einheiten. Das Ensemble bewahrt je Realisierung nur Kennzahlen und die Vertretertage zu
  P50/P90/P95/P99; das Volumen rechnet ein `Volumenauftrag` während der Ziehung; parallel und seriell
  bleiben bitgleich, auch über Blockgrenzen.
- **(i) Kategorien ohne Anteil** ziehen nicht (Rate 0); eine nicht endliche Rate wird benannt
  abgelehnt; innere Ausnahmen des parallelen Laufs werden ausgepackt.
- **(j) Trennungswache (2.4).** Minutenwerte außerhalb `Bedarfstag` nur im Typ `Minutenstatistik`; die
  Wache prüft auch nicht private Konstruktoren auf Zahlenfelder.
- **(k) Testorakel.** Unabhängige Referenzfälle (Python, ohne C#-Aufruf) für Zufall, Auslegungs- und
  Jahresensemble (Urlaubsfenster über den Jahreswechsel, Ereignis über Mitternacht, zwölf Spreizungen,
  Energiefaktor, Feiertag); Vergleich gegen eine DHWcalc-Referenzdatei aus OpenDHW (MIT, gz 45 kB,
  Attribution im Testordner `EPOS.Kern.Tests/Proben/Zapfprofil/OpenDHW/`) in Verteilungsgrößen
  (Tagesmittel ± 5 %, Minutenspitze/Tagesmittel, Zapfminuten je Tag) — kein Bitvergleich.

**Befunde und Festlegungen (Katalog und Schema, Gruppe 2):**

- **(l) T2 = Schritt 115 — Spaltenliste (ersetzt die Liste in 3.1).** `Tab_TwwZapfkategorie_STAMM`,
  STRICT, 16 Spalten: `ID`, `ID_Nutzungsart` (Fremdschlüssel, `ON DELETE CASCADE` wie Tagesgang und
  Ereignis, 3.2), `Kategorie`, `Reihenfolge`, `Volumenstrom_l_min` ≥ 0, `Dauer_min` 1…1440, `Anteil`
  ≥ 0, `Sigma` ≥ 0, `Kappung_l_min` > 0 oder NULL, `Quelle`, `Ausgabe`, `Version`, `Herkunftsart`
  (CHECK wie T1), `Status`, `Beleg`, `ReadOnly`; eindeutig über (`ID_Nutzungsart`, `Kategorie`). Die
  Kategorie hat keine eigene Katalogversion: sie gehört zur Version ihrer Nutzungsart.
  „Provenienz" ist die übliche Vierergruppe Quelle/Ausgabe/Version/Herkunftsart, keine Textspalte.
- **(m) Kategorien als Datenblock der Nutzungsart.** Im Projekttransfer reisen sie als Kindzeilen
  (Status IMPORT, ReadOnly 0) und zählen im Inhaltsvergleich; eine ReadOnly-Kategorie sperrt ihre
  Nutzungsart (Löschen, Ändern, Tagesgang speichern); „Speichern unter" und `TagesgangSpeichern`
  kopieren die Kategorien mit; die Auslieferungsvorlage prüft Waisen und schreibt eine eigene
  Katalogpaket-Datei (Paketformat N2 um `Tab_TwwZapfkategorie_STAMM.csv` mit Status ergänzt); ohne die
  Tabelle (Stand vor 115, älterer iOS-Seed) laufen Löschen, Dublettenscan und Import wie bisher.
- **(n) Eingang.** `ZapfprofilCtrl.Eingang` liest die Kategorien je Nutzungsart der Zone in
  Reihenfolge; eine stochastisch gerechnete Zone ohne Kategorien lehnt benannt ab (Nutzungsart und
  Katalogversion getrennt als Kennung und Argument, sprachfest); eine deterministische braucht sie
  nicht. Die fünf Parameter `Zapfprofil.Stochastik.*` und die Vorgaben (Seed 1, R = 10,
  ⌈Vielfaches · 1/(1 − p)⌉) sind durch Tests belegt.
- **(o) Kapitel 6 (b) und (c) gelten angepasst.** Die Testdatenbank ist nicht mehr rein fiktiv: je
  `Tab_Tww*_STAMM` sind Paare aus Herkunftsart und Quelle zugelassen — `FIKTIV`/„Testkatalog (fiktiv)"
  (Testnutzungen A/B/C bleiben, acht Testklassen hängen an ihren runden Werten), `FIKTIV`/„VDI 6002
  Blatt 1/2 (abgeleitet)" (ZU19), `FREI`/Verordnung bzw. Modellannahme (Ecodesign, Zapfkategorien,
  Stochastik-Parameter). Die Wache prüft die Paare, hält die abgeleiteten Werte gegen die lokalen
  Originale (nur wo sie liegen: kein Wert gleich, jeder innerhalb ± 6 %) und die Testdatenbank gegen
  die JSON und den freien Paketteil (überall, auch in der CI).
- **(p) Freier Paketteil im Repositorium (Erweiterung von N2).** Die Zapfkategorien (Modellannahme bis
  Z5 nach Jordan/Vajen, IEA SHC Task 26; Streuungen nach dem DHWcalc-Protokoll im Testordner), die fünf
  Stochastik-Parameter und das Ecodesign-Zapfprofil L sind frei und liegen als Paketdateien unter
  `Referenzlaeufe/Katalogpaket_frei/` (Herkunftsart `FREI`, Status AUSLIEFERUNG, ReadOnly 1; Kopf und
  Ereignisse über `ID`/`ID_Bedarfstag` verknüpft; LIESMICH). `Werkzeuge/Auslieferungsvorlage` spielt den
  Paketteil **immer** ein (`TwwKataloge.PaketteilEinspielen`, nach `--katalogpaket`, in einer
  Transaktion); die Zeilen übernehmen die Katalogversion des Katalogs (sonst `FREI-1`); führt das
  externe Paket dieselbe Zeile, gewinnt es, und der Prüfbericht meldet das. Die Kategorien sind ein
  **Vorgabesatz ohne `ID_Nutzungsart`** im Paket und binden beim Einspielen an jede Nutzungsart mit
  Status AUSLIEFERUNG, die keine eigenen führt; der Paketteil trägt keine eigene Katalogversion. Das
  Katalogskript schreibt dieselben Dateien in die Testdatenbank (eine Quelle); Wachen halten die
  Testdatenbank gegen JSON und Paketteil, ohne die Originale. Ohne den Paketteil könnte die Auslieferung
  nicht stochastisch rechnen. Der Sperrgrund der Bedarfstag-Quelle (5) gilt nur ohne Katalogzeile und
  lautet „Der Katalog führt das Ecodesign-Zapfprofil nicht; es kommt mit der Auslieferungsvorlage oder
  dem Katalogimport."
- **(q) Ecodesign-Zapfprofil (4.5 (5), N11 (e)).** Profil L (Einfamilienhaus), 24 Zapfungen, Q_ref
  11,655 kWh (Tagessumme per Test), Quelle Verordnung (EU) Nr. 814/2013, Anhang III (ABl. L 239 vom
  6.9.2013, amtliche Fassung des Amts für Veröffentlichungen). Die Verordnung nennt keine Dauer: es gilt
  Dauer = Volumen / f mit T_p, sonst T_m, gegen 10 °C, kaufmännisch gerundet; Bezugsmenge NULL. Die
  Profile M und XL sind ebenso frei, aber nicht aufgenommen (Setzung; Folgeposten bei Bedarf).
- **(r) Zuordnung der abgeleiteten Werte.** Tagtyp 4 nimmt den Sonntag; Bezugstemperaturen 60/12 °C;
  niedrig/mittel/hoch = Minimum/Mittel/Maximum (die Extrema der Quelle sind Monatsextrema, der
  Jahresgang zählt damit doppelt — für Testdaten tragbar, im Skriptkopf vermerkt); Campingplatz,
  Hallenbad und Ein-/Zweifamilienhaus stehen nicht im Katalog (keine passende Bezugsart bzw. keine
  Profile); die Montags- und Freitagsprofile des Studentenwohnheims bleiben ungenutzt.

**Befunde und Festlegungen (Oberfläche, Gruppe 3):**

- **(s) Oberfläche (Kapitel 5).** Die Stufe Experte ist im Zapfprofil-Dialog wählbar und trägt die
  Gruppe „Stochastik · Jahresreihe" mit dem Rechenweg (deterministisch | stochastisch), Seed und
  Realisierungen (leer = Vorgabe 1 bzw. 10, Grenzen 1…1000); Erweitert bleibt mit Grund gesperrt (Z4),
  deshalb steht der Rechenweg in Experte statt ab Erweitert (5.x). Der Reiter Kennzahlen zeigt in
  Experte je Zone die Konsistenzprobe (Abweichung, Mittel, s_R, Toleranz, ✓/≠) als Textzeile; eine Zone
  ohne Zapfkategorien steht als Banner mit Nutzungsart. Die Vorschau rechnet den Rechenweg des Stands,
  also auch stochastisch, synchron und entprellt — Abweichung von 5.1 („deterministischer Pfad"). Der
  Fußknopf „Stochastisch rechnen" öffnet die Auslegung mit gesetztem Schalter statt eines eigenen
  nebenläufigen Laufs. In der Auslegung schaltet „Stochastisch rechnen" die Perzentilwahl (P95 | P99,
  K3) und die Realisierungen des Bedarfstags frei (leer = Kernvorgabe ⌈Vielfaches · 1/(1 − p)⌉); Karte
  (b) zeigt Perzentilwert, Streuband P50…P99 mit Spannweite als Tabelle, GLF_V bzw. GLF_P mit Σ n_E,
  den Vermerk „nicht belastbar" und den Konsistenzhinweis; ohne Schalter bleibt die deterministische
  Karte (b). P50…P99 und Gleichzeitigkeit stehen in Karte (b), nicht im Reiter Kennzahlen; die
  Schlüssel des Auslegungsbündels tragen das Präfix `ZPG_AUS_` (`ZPG_AUS_LBL_PERZENTIL`,
  `ZPG_AUS_LBL_REALISIERUNGEN` statt der Namen in 5.3); die Kernsätze der Karte (b) bleiben deutsch
  (N11 (k)); die Stufe geht nicht an die Überlagerung, die Marke „Schnellauslegung" steht (N11 (c), Z4).
  Kein neues und kein geändertes Diagrammbild. Kernänderung außerhalb der Schnittstellen der Gruppen
  1/2: `ZapfprofilCtrl.Auslegung.Auslegungslauf` trägt den Parameter `Stochastisch` (Vorgabe false).
- **(s2) Nachbesserung der Oberfläche (Gegenprüfung).** Die Vorschau rechnet **immer deterministisch**
  (5.1; festgelegt in `BedarfsVorschauCtrl`, gilt für Dialog, Öffnen und die Leiste des
  Bedarfsprofil-Dialogs); eine leise Zeile darunter sagt, dass die Jahresreihe erst im Lauf stochastisch
  entsteht — Vorschau und Lauf sind bei „stochastisch" bewusst ungleich (gleiche Jahresmenge). Das
  Jahresensemble rechnet nur über den Fußknopf „Stochastisch rechnen" (bei Rechenweg stochastisch),
  **nebenläufig** nach dem Muster `KapitalwertVerlaufAbschnitt`/`ProjektKopieDialog`:
  `CancellationTokenSource`, Baustein `Fortschritt`, Start über `Kulturweitergabe.Starten` (ein nacktes
  `Task.Run` weist `ParallelitaetWacheTests` ab), Abbruchmarke bis in den Kern (`ZapfprofilCtrl.Rechnen`,
  `ZapfprofilRechner`, `Auslegungslauf`/`Auslegungseingang.Abbruch`, `ParallelOptions`), Ergebnis per
  `InvokeAsync`, überholte Läufe verworfen; OK rechnet nichts — die Simulation zieht die Reihe selbst.
  Das Auslegungsensemble rechnet ebenso nebenläufig (Karte (b) „rechnet …"); ein Abbruch schaltet
  „Stochastisch rechnen" aus und rechnet deterministisch nach; OK während des Laufs nimmt den
  deterministischen Punkt. Der Kern begrenzt die Einheitentage der Jahresreihe **je Projekt**
  (R · Σ n_E · 365 ≤ 10⁷, Kennung `STOCHASTIK_EINHEITSTAGE` mit Anzahl und Grenze); die Pflichtprüfung
  des OK prüft sie nicht, der Lauf lehnt benannt ab. Der Seed steht in Experte immer, nur die
  Realisierungen der Jahresreihe hängen am Rechenweg. Die Wohnungsstation zeigt die P_p-Spitze je
  Einheit samt Zone in Karte (b) (`Perzentilergebnis.SpitzeJeEinheit*`). Der Schalter der Auslegung
  kommt allein aus dem Öffnen (`AuslegungGaben`; „Auslegung…" öffnet ohne Schalter, kein erstes
  Ergebnis, genau ein Lauf). Bei P_p = ∞ zeigt Karte (b) Standtext und „entfällt". Das
  `Perzentilergebnis` trägt Schwelle und verglichene Größe als Felder (`Konsistenz*`), der Satz der
  Karte (b) entsteht aus Werten in der Oberflächensprache; der Kernsatz der Warnliste bleibt deutsch
  (N10 (l)). „(K3)" steht nicht mehr in Oberflächentexten; der maßgebende Tag erscheint als Datum (kein
  Schaltjahr); die Abweichung der Konsistenzprobe rechnet der Kern (`Jahreskonsistenz.Abweichung`);
  die Dialoggrenze der Auslegungsrealisierungen ist die Kernobergrenze (100 000). 18 neue und 7
  geänderte Schlüssel je Sprache.
- **(t) KI-Maskenanmeldung.** Die Anmeldung von `ZapfprofilDialog`, `ZapfprofilAuslegungDialog` und
  `BedarfstagKonstruktor` beim KI-Assistenten sowie die Optionsgruppe „Rechenweg" in der Feldkarte des
  `BedarfsProfileDialog` macht die Sitzung Dialog Design nach dem Z3-Merge (#458), nicht Z3.

**Folgen:**

| Folge | Was | Wer | Wann |
|---|---|---|---|
| ZU20 | Auslieferung der abgeleiteten VDI-Werte: ja/nein | Anwender | nach K8 |
| ZU21 | Setzungen des Paketteils (u) bestätigen oder ändern | Anwender | vor der ersten Auslieferung |
| (p) | freier Paketteil im Repositorium; Paketformat um Kategorien | Agent Z3 (Nachbesserung) | Z3 |
| (q) | Profile M und XL der Verordnung als weitere Bedarfstage | Folgeposten | bei Bedarf |
| (k) | ChartProben-Messlatte auf Linux einfrieren (unverändert: Z3 fügt keine Bilder hinzu) | Anwender/CI | nach #451 |
| (s) | Sichtabnahme unter Windows (Prüfliste im Übergabepapier, Abschnitt 10) | Anwender | nach dem Push |
| Wiki | Abschnitt „Stochastik" der Seite Brauchwasser-Zapfprofil; Logbuch-Sätze (Versionsnummer) | Anwender (Upload gebündelt) | nächster Upload |

### N13 (24.09.2026) — Umsetzungsbefunde Z4 (Oberfläche vollständig): Kern, Dialoge, Katalogdialog

**Anlass.** Stufe Z4 nach Kapitel 7: Stufen Erweitert und Experte, Zonenliste für Mischnutzung,
Wohnungstabelle, Tagesgang-Editor, Auslastungsgang, Kategorien als Katalogkopie, Schätzhilfen,
Warnlogik, Dauerlinie, Katalogdialog mit Untermenü und Katalogimport, KiSicht, Hilfeschlüssel, Wiki;
dazu die Z4-Folgen aus N9 (c, g, h), N10 (i, j), N11 (c, d, i, j, k) und N12. Vier Gruppen (Kern und
Controller · Zapfprofil-Dialog · Editoren, Auslegung und Konstruktor · Katalogdialog) durch Agenten
mit `model: opus` im Worktree `z4`, je Gruppe eine Gegenprüfung; Schemaschritt **124**
(zunächst 120, dann 121 — beide Nummern wurden während der Stufe von anderen Konten belegt, Regel:
wer zuerst pusht, hat die Nummer); Protokoll
[Z4](../ueberholt/Protokolle/Zapfprofilgenerator/2026-09-24_Z4_Oberflaeche.md), Statuszeile #464.

**Befunde und Festlegungen (Kern und Controller, Gruppe 1):**

- **(a) Schemaschritt 124 `SCHRITT_124_ZAPFPROFIL_LAUFANGABEN` (N10 (i), N11 (d), (j)).**
  `Tab_TwwProjekt`: `Erzeugerart` (1,2), `Uebertrager_Werkstoff` (1,2), `Personen_Auto` (0/1),
  `Personen_Manuell` ≥ 0, `Fuellstand_Bezug` (1…4); `Tab_TwwBedarfstag_STAMM`: `Bezugsart` (1…7,
  NULL erlaubt). Wertemengen je einmal in `TwwSchema` für DDL und Schreibweg. Vor dem Schritt läuft
  das Speichern ohne Angabe durch, eine gesetzte Angabe wird benannt abgelehnt; ein Ziel vor dem
  Schritt importiert, der Bericht nennt die liegen gebliebenen Werte. Das Ecodesign-Profil L trägt die
  Bezugsart 2 (Wohneinheiten) ohne Bezugsmenge und wird nicht skaliert (Fachentscheid offen).
- **(b) Sätze des Kerns (N11 (k)).** `ZapfSatz` mit Kennung und Werten (`ZPG_SATZ_*`, 385 Muster je
  Sprache, 64 alte Schlüssel entfernt); Wache `ZapfSaetzeWacheTests` prüft Muster, Platzhalterzahl und
  Sprachen; `ZapfSatz.Text` wirft bei unpassendem Muster. Satzregeln: kein Präfix „Nicht rechenbar —"
  (der Bannertitel sagt das), die Hülle setzt den Zonenvorsatz nur vor Sätze ohne Zone, Nutzungsart
  ohne Id, Tabellennamen als Katalogbezeichnung (`BEGRIFF_TABELLE_*`). Die Vermerke des
  Herkunftsprotokolls bleiben deutsche Klartexte (Folge).
- **(c) Kategorien als Katalogkopie (4.4, N12).** `TwwNutzungsartCtrl.KategorienLesen/Vorgabe/
  Speichern` mit `Zapfkategoriensatz.Pruefen` (dieselben Einzelregeln wie der Rechenweg); eine freie
  Nutzungsart wird an Ort und Stelle geändert, eine gesperrte als neue Katalogversion (EIGEN,
  Katalogversion „<Version>-E<n>"); Vorgabesatz eindeutig: nur ganz freie Sätze, Auslieferungszeilen
  zuerst, dann der häufigste, dann der größere Satz.
- **(d) Warnlogik und Schätzhilfen (4.x, N9 (g)).** `ZapfHinweis.Warnung` trennt Warnung und
  Hinweis; die Warnstufe geht in die Auslegung (Titel `ZPG_WARN_<Code>` als Rückfall). Neu: Hinweis
  auf die Bezugsmenge neben der Wohnungstabelle, Bandbreite auch beim manuellen Tagesbedarf, Hinweis
  `ZIRKULATION_GROSS` über dem Katalogverhältnis `Zapfprofil.Zirkulation.Hinweisverhaeltnis` (nicht
  als Warnung — Lehre 3 des Mockups). Schätzhilfen für Tagesbedarf (nach Kalibrierung; „angesetzt" nur
  ohne Messwert), Zirkulation je Methode und Ladeleistung als Kennung und Werte. Die Regel
  „Ein-/Zweifamilienhaus: größte Einzelentnahme" bleibt vorgemerkt (Normwert aus dem Katalogpaket
  nach K1/K8, Erkennung „höchstens zwei Wohneinheiten").
- **(e) Dauerlinie und Auslastungsgang (5.6).** `Zapfauswertung.Dauerlinie` (8760 sortierte Stunden,
  P50/P90/P95/P99 als ganzzahlige Rangquantile), `Auslastung` (Mittel 1), `Formvektor.Auslastungsgang`
  (4.2, eine Regel für Rechnung und Anzeige); neues Bild `zapfprofil_dauerlinie` (vier Proben in
  `Proben/ChartProben`, alte Bilder unverändert; in der Linux-Messlatte `Messlatte_2026-09-26.sha256` — Folge N9 (a)).
- **(f) Eingang (N9 (h), N11 (c)).** Anzeigetemperatur und Stundenschwelle: Laufangabe → Einstellung
  `Zapfprofil.*` → Parametersatz (`Zapfprofil.Anzeigetemperatur` 45 °C, `Zapfprofil.Stundenschwelle`
  0,1 kW im Paketteil); ungültige Werte benannt. Die Stufe geht in den `Auslegungslauf`; in Einfach
  setzt der Kern Marke und Vermerk „Schnellauslegung" (zusätzlich beim Schnellpfad, wie seit Z2).

**Befunde und Festlegungen (Zapfprofil-Dialog, Gruppe 2a):**

- **(g) Stufen (2.x, 5.1).** Alle drei Stufen wählbar; Stufen blenden nur ein und aus, Eingaben
  bleiben; die leise Zeile verweist in Einfach auf Erweitert, in Erweitert auf Experte (N9 (c)); der
  Rechenweg der Jahresreihe steht ab Erweitert, Seed und Realisierungen in Experte (N12 (s)); das
  Auslegungsperzentil bleibt in der Auslegung.
- **(h) Zonenliste und Wohnungstabelle (5.3).** Die Zonenliste ist eine Haustabelle mit Summenfuß
  (`Raster` hat keinen Fuß, N9 (k)); ab Erweitert Topologie, Anteil und Rechenweg; die wirksame
  Bezugsmenge liefert der Kern, OK übernimmt sie. Wohnungstabelle als `Raster` nur bei Wohnen —
  das Kriterium ist eine Kernfunktion (`Mengengeruest.WohnungstabelleWirksam`: Bezugsart Wohneinheiten oder Personen, ohne Kalenderbezug), die Rechnung, Pflichtprüfung, Schreibweg und Hülle gleichermaßen nutzen; eine nicht wirksame Tabelle wird weder gerechnet noch geprüft, der Schreibweg verwirft sie (Gegenprüfung).
- **(i) Abweichungen von 5.3.** Der Vorschlag zur Ladeleistung entsteht im Kern der Auslegung und
  wird ab Erweitert auch im Hauptdialog gezeigt (2b); das Bundesland ist gesperrt, zählt nicht und
  fehlt in der KI (keine Kalendertabelle je Bundesland, A6); der Kalender bindet an ein Gebäude des
  Projekts (A8, neuer Kernleser `GebaeudeDesProjekts`); Ferien als vier Zeiträume Tag/Monat
  (Jahrestag, 0 und 366 = keine Angabe); Schlüsselnamen teils `ZPG_LBL_*_MANUELL`; die Dauerlinie
  steht ab Erweitert (Mockup); die KI-Zonenfelder gelten der markierten Zone; jede Gruppe trägt einen
  eigenen Hilfeknopf mit Assistent.
- **(j) Jahresmesswert (4.1).** Einheit kWh oder m³; die Bilanzgrenze nur bei kWh, der Speicherverlust
  nur bei der passenden Grenze; der Kern nutzt ihn ebenso.
- **(k) Überschrieben-Zähler.** Er zählt nur wirksame Abweichungen: ein auto/manuell-Paar einmal, der Speicherverlust nur bei Bilanzgrenze 3 mit Messwert, Zirkulationsangaben nur für die gewählte Methode, die Wohnungstabelle nur bei wirksamer Bezugsart (Gegenprüfung; die erste Fassung zählte Schalter und Wert doppelt).
- **(l) Pflichtprüfung des OK.** Die Gültigkeit manueller Werte (Tagesbedarf, Zirkulation) prüfen Kernfunktionen (`Mengengeruest.TagesbedarfManuellGueltig`, `Zirkulationskanal.ManuellGueltig`), die Rechenweg und Pflichtprüfung des OK gleichermaßen rufen — keine zweite Regelsammlung in der Oberfläche (Gegenprüfung).

**Befunde und Festlegungen (Editoren, Auslegung, Konstruktor, Gruppe 2b):**

- **(m) Tagesgang-Editor (5.1, 4.2).** Überlagerung `TagesgangEditor.razor` (Muster
  `TypProfilDialog`): vier Tagtypen × 24 Anteile, Summenzeile mit Vorschau der Normierung, Normieren,
  Tag kopieren/einfügen, Vorlage laden, Zurücksetzen; eine gesperrte Nutzungsart wird als
  Anwenderkopie (EIGEN) geschrieben, die Zone rechnet danach mit ihr; hat eine Zone einen eigenen
  Satz gewählt, schreibt der Editor über die Nutzungsart und die Wahl der Zone folgt dem
  geschriebenen Satz; zeigt der Editor einen anderen Satz als den der Nutzungsart, entsteht immer
  eine Kopie (nie wird ein nicht gezeigter Satz überschrieben). **Herkunft bleibt erhalten:** das
  DTO führt die Originalanteile des Stands beim Öffnen mit, unveränderte Reihen und Wochenfaktoren
  gehen bitgleich zurück, normiert wird nur bei |Σ−1| > 1e-9, der Kernvergleich `Gleich` arbeitet
  mit Toleranz 1e-12, „Vorlage laden" gibt die Herkunft der Vorlage mit — die erste Fassung hätte
  16 von 20 abgeleiteten Reihen als Eigenkonstruktion ohne Beleg geschrieben (Gegenprüfung,
  lizenzrelevant). Ein Tagtypwechsel räumt die Fehleingaben des verlassenen Tagtyps. Esc schließt
  erst den Editor, dann den Dialog.
- **(n) Kategorien-Raster (5.3).** Überlagerung `ZapfkategorienEditor` mit allen Spalten,
  Vorgabesatz, Prüfregeln allein aus dem Kern: eine Anteilsumme ≠ 1 ist nur ein Hinweis, abgelehnt
  wird Summe 0 (fachlich richtig: `Zapfkategoriensatz.Aus` teilt jeden Anteil durch die Summe, der Generator rechnet mit dem normierten Anteil); Kopie bei gesperrtem Eintrag; schreibgeschützte Ansicht mit Schloss
  und Grund.
- **(o) Auslegung (N11 (d), N10 (i)).** Eingaben des Verfahrensvergleichs als Felder (Ladeleistung
  und Personen auto/manuell mit Vorschlag, Ladefenster, Nutzanteil, Zuschlag, Füllstand-Bezug),
  nachrichtlich; Erzeugerart und Werkstoff mit Vorschlag aus dem Projekt, gespeichert; der
  Ladeleistungs-Vorschlag steht ab Erweitert auch im Hauptdialog.
- **(p) Konstruktor (N11 (j), Schritt 124).** Bezugsart und Bezugsmenge wählbar; Zeilen und
  Bezug bleiben über das Schließen des Zapfprofils hinaus am Arbeitsstand; nach OK im Bedarfsprofil
  bleiben Ereignisse und Bezug (die Konstruktorzeilen selbst in der Datenbank zu halten braucht einen
  Schemaschritt — Folge).
- **(q) Hilfe und Wiki (5.8).** Hilfeschlüssel je Gruppe (`Form_Zapfprofil.grp_*`, `grp_Auslegung`,
  `grp_Konstruktor`, Editoren); `Form_Zapfprofil_Berechnung` zeigt auf den Anker `stochastik` der
  Bedienseite, bis die Berechnungsseite `Zapfprofil.wiki` existiert (Folge); Bedienseite um
  Stufen, Belegung, Schätzhilfen, Fachwerte, Dauerlinie, Warnliste, Tagesgang bearbeiten,
  Zapfkategorien, Verfahrensvergleich und Konstruktor-Bezug fortgeschrieben.

**Befunde und Festlegungen (Katalogdialog, Gruppe 3):**

- **(r) Katalogdialog (5.4).** `TwwNutzungsartAdminDialog.razor` nach dem gültigen Muster der
  Verwaltungen (Stammblatt statt „Gestapelt + lesendes Formularraster"): Liste mit Spaltenrängen und
  Schloss, Stammblatt mit Kennzahlen, Tagesgang- und Jahresgangbild (bestehende Renderer), Editor
  `TwwNutzungsartEditor` für Neu, Ändern und Speichern unter (gesperrte Zeilen als neue eigene Zeile),
  Löschen mit Sperrgrund (ReadOnly, benutzende Projekte), Tagesgang, Kategorien, Grafik und Import;
  Ändern/Tagesgang/Kategorien/Grafik in den Gruppenköpfen, Speichern unter und Löschen in der
  Auswahlleiste, der Fuß trägt Import · Neu · Beenden; kein eigener Knopf „Typ ändern" (Bezugsart und
  Kalender im Editor). Menü: Administration → Brauchwasser ist ein Untermenü mit „Brauchwasserprofile"
  und „Brauchwasser-Nutzungsarten" (`Menuetabelle.cs`, `MenuebandTests`); auf iOS bleibt der Katalog
  geschlossen (Folge).
- **(s) Katalogimport (2.5, 3.2, N2).** Paket aus Ordner, ZIP oder CSV im Format N2; Zeilen mit
  Herkunftsart IMPORT (FREI und FIKTIV bleiben), Dublettenscan wie im Projektimport, Bericht
  angelegt/übersprungen/abgelehnt mit Grund, Katalogsperre unberührt; Bedarfstag und Parameter deckt
  der Import nicht ab (Folge).
- **(t) Wachen (Kapitel 6, ZU-Folge (c)).** `WikiProduktdatenWacheTests` prüft die Texte aller
  `Tab_Tww*_STAMM` ohne `Beleg` und alle `ZPG_`/`ZPGK_`-Ressourcen mit Gegenprobe; die Katalogsperre
  ist auch über den Dialogweg gehalten.
- **(u) Rasterprobe (5.7).** Gelaufen im echten Browser (Chromium/Playwright aus NuGet): drei
  Verstöße behoben — die Katalogliste rollte quer (Spaltenränge im Profil), die Überlagerung
  „Zapfkategorien" rollte quer mit (versteckte Feldbeschriftung ohne positionierten Vorfahren,
  `position: relative`), das Kategorien-Raster war 1 548 px breit (schmalere Felder, umbrechende
  Köpfe). Messwerte: Katalogliste 6 654 Sätze, Zeilenhöhe 46 px, Abstandshalter 0/305 256 px,
  Sichtbarkeitsmelder 3–4 (Soll ≤ 12), Zeilen nach dem Rollen in 122 ms; Wohnungstabelle und
  Kategorien-Raster 45 px ohne senkrechtes Rollen. Die Wohnungstabelle rollte im 498 px breiten Eingabeblock bei 1 088 px Breite noch 102 px quer (Folge, Kleinigkeit).

**KI-Maskenanmeldung (Pflegeregel seit #458).** Jede Eingabestelle der Zapfprofil-Dialoge steht in
der Feldkarte und in der Zählliste von `KiMaskenabdeckungWacheTests`: ZapfprofilDialog 55,
ZapfprofilAuslegungDialog 20, BedarfstagKonstruktor 10, TagesgangEditor 5, ZapfkategorienEditor 7,
TwwNutzungsartEditor 13, TwwNutzungsartAdminDialog 0 (nur Auswahl und Knöpfe); der KI-Katalog führt 80 Masken und 11 Zahlenreihen.

**Setzungen des Paketteils (ZU21, Fortschreibung).** Zusätzlich zu N12 (u): Zirkulations-Hinweis-
verhältnis 1,5; Anzeigetemperatur 45 °C; Stundenschwelle 0,1 kW.

**Folgen:**

| Folge | Was | Wer | Wann |
|---|---|---|---|
| (a) | Ecodesign L nach Wohneinheiten skalieren: ja/nein | Anwender (Fachentscheid) | vor Z5 |
| (b) | Vermerke des Herkunftsprotokolls als Kennung und Werte | Agent der Stufe Z5 | Z5 |
| (d) | Regel „Ein-/Zweifamilienhaus: größte Einzelentnahme" mit Normwert aus dem Katalogpaket | Katalogpflege nach K1/K8 | nach K8 |
| (e) | Dauerlinienbild in die Linux-Messlatte von `Proben/ChartProben` — **erledigt** (`Messlatte_2026-09-26.sha256`) | CI-Lauf, Anwender | mit N9 (a) |
| (p) | Konstruktorzeilen in der Datenbank (Schemaschritt) | Agent der Stufe Z5 | **erledigt mit N21** (Schemaschritt 145) |
| (q) | Berechnungsseite `Zapfprofil.wiki` und Umlenkung von `Form_Zapfprofil_Berechnung` | Wiki-Runde | nächster Upload |
| (r) | Katalogdialog auf iOS (Naht der Schale) | Agent einer iOS-Welle | nach iU11 |
| (s) | Katalogimport um Bedarfstage und Parameter erweitern; Größenschutz beim ZIP-Import (wie im Projektimport) | Agent der Stufe Z5 | Z5 |
| Rest | geringe Befunde der Gegenprüfungen 2a/2b: Fehleingaben verschwundener Felder räumen, Zonenliste mit wirksamer Bezugsmenge und Summenfuß, Maximum des Zirkulationsanteils in der KI-Karte, Ladeleistungs-Vorschlag mit gefangenen Ausnahmen und Grundtext, Editor-Knöpfe ohne Delegat nicht rendern, `FreieKopieversion` ohne „-E1-E1", Vorgabesatz bei leerer Nutzungsart, Balkengrafik im Tagesgang-Editor, Wohnungstabelle 102 px | Agent eines Folgepostens | Z5 |
| ZU21 | Setzungen des Paketteils (N12 (u) und oben) bestätigen | Anwender | vor der ersten Auslieferung |
| Wiki | Bedienseite hochladen; Logbuch-Sätze (Versionsnummer) | Anwender (Upload gebündelt) | nächster Upload |
| Sicht | Sichtabnahme unter Windows (Übergabe, Abschnitt 11) | Anwender | nach dem Push |

### N14 (24.09.2026) — Umsetzungsbefunde Z4b, Gruppe 1 (VDI-4655-Import mit Typtagzuordnung, Kern)

**Anlass.** Stufe Z4b nach Kapitel 7, Gruppe 1: Schemaschritt T3 „Typtage", `Normformvektorleser`,
`Typtagzuordnung` mit Wetterkopplung (Vorfragen 4.2), Schreibweg und Weiche. Ein Agent mit
`model: opus` im Worktree `z4b`; der Importdialog ist Gruppe 2 und steht aus.

- **(a) Schrittnummer von T3: 125.** Vor der Arbeit gemessen — `SchemaStand.Zielversion` stand auf
  origin auf 124, `SCHRITT_125` war frei. **Namenskollision:** Den Papiernamen „T3" trägt im
  Bestand schon Schritt 124 (die Laufangaben der Auslegung, N11); gemeint ist dort die
  Spaltenerweiterung, hier die Tabelle der Typtage. Die Tabelle heißt im Code deshalb
  `TwwSchema.AnweisungenT3Typtage`, die Spalten von 124 bleiben `TwwSchema.SpaltenT3`.
- **(b) Das Paketformat.** Ein ZIP-Archiv (oder ein Ordner, den die Hülle liest) mit sechs Dateien,
  UTF-8 (BOM erlaubt), Kopfzeile mit den Spaltennamen, Trenner `;` oder `,` **je Datei**, RFC 4180,
  Zahlen in invarianter Kultur mit Punkt (Exponent erlaubt), leeres Feld = fehlt. Eine unbekannte
  Spalte ist ein Fehler, keine stille Annahme.

  | Datei | Spalten |
  |---|---|
  | `typtage.csv` | `code;jahreszeit;tagart;bewoelkung` (`uebergang`/`sommer`/`winter`, `werktag`/`sonntag`, `heiter`/`bewoelkt`/`ohne`) |
  | `klimazonen.csv` | `zone;bezeichnung` (`bezeichnung` wahlfrei, nur für den Bericht) |
  | `typtage_je_zone.csv` | `zone;gebaeudeart;typtag;anzahl` |
  | `f_twe_tt.csv` | `gebaeudeart;zone;typtag;faktor` |
  | `kennwerte.csv` | `schluessel;wert;text` |
  | `tagesgaenge.csv` (wahlfrei) | `gebaeudeart;typtag;aufloesung_min;index;anteil` |

  Pflichtkennwerte: `quelle` (Text), `wintergrenze`, je Gebäudeart `heizgrenze.<gebaeudeart>` und —
  sobald eine Kategorie nach Bewölkung unterscheidet — `bewoelkung.schwelle` (Achtel). Wahlfrei:
  `ausgabe` (Text) und `pruefsumme.toleranz`. **Jede Grenze des Verfahrens kommt aus dem Paket, nie
  aus dem Quelltext** (Kapitel 6 (a)).
- **(c) Eine Zeile je Wert, dazu die Spalte `Art`.** `Tab_TwwTyptag_IMPORT` führt elf Spalten
  (`ID`, `Art`, `Klimazone`, `Gebaeudeart`, `Typtag`, `Aufloesung_min`, `Zeilenindex`, `Wert`,
  `Quelle`, `Ausgabe`, `Datum_Import`), natürlicher Schlüssel
  (`Art`, `Klimazone`, `Gebaeudeart`, `Typtag`, `Zeilenindex`), STRICT, kein `Status`, kein
  `ReadOnly`, keine Provenienzgruppe. `Art` unterscheidet `KATEGORIE` (drei Zeilen je Typtag:
  Jahreszeit, Tagart, Bewölkung als Zahl), `ANZAHL`, `FAKTOR`, `GANG` und `KENNWERT`; bei `KENNWERT`
  trägt die Spalte `Typtag` den Schlüssel. `Klimazone` = 0 heißt „für jede Zone", `Gebaeudeart` = ""
  „für jede Gebäudeart". Die Klimazonennamen des Pakets werden **nicht** gespeichert (die Rechnung
  braucht nur die Nummer).
- **(d) Die Vorfragen aus 4.2, beantwortet.**
  - **Jahreszeitgrenzen:** Sommertag, wenn das Tagesmittel der Außentemperatur **über** der
    Heizgrenze der Gebäudeart liegt; Wintertag **unter** der Wintergrenze; sonst Übergangstag. Beide
    Grenzen kommen als Kennwert aus dem Paket (`heizgrenze.<gebaeudeart>`, `wintergrenze`) — der
    Kern kennt keine Zahl.
  - **Bewölkungsschwelle:** Das Tagesmittel des Bedeckungsgrads in **Achteln** (Quelle
    `Tab_Solar.Bedeckungsgrad`, TRY-Größe `N`) gegen den Kennwert `bewoelkung.schwelle`:
    **ab** der Schwelle bewölkt, darunter heiter. Die Schwelle ist ein Parameter des Pakets, keine
    Zahl des Quelltexts. `Tab_Klimadaten.TagTyp_W` wird **nicht** benutzt — es ist der Diffusanteil
    der Strahlung, nicht der Bedeckungsgrad. Unterscheidet das Paket nach Bewölkung und fehlen die
    Tagesmittel, wird die Zone **benannt abgelehnt**; es gibt keinen stillen Rückfall.
  - **Feiertage und Samstag:** Sonntag ist jeder Tag mit dem Kennzeichen „Wochenende oder Feiertag"
    der Klimaregion, dessen Wochentag **nicht Samstag** ist — ein Feiertag zählt damit als Sonntag,
    ein Samstag bleibt Werktag. **Abweichung, benannt:** Ein Feiertag, der auf einen Samstag fällt,
    bleibt Werktag, weil der Klimakalender ihn nicht von einem gewöhnlichen Samstag unterscheidet
    (A6 bringt den Feiertagskalender).
  - **Ferien:** Der Urlaubstag der Richtlinie (kein Warmwasser) wird **nicht** umgesetzt; ein
    Ferientag der Zone bleibt Werktag oder Sonntag seiner Jahreszeit, damit die Jahresenergie
    erhalten bleibt. Ein Hinweis nennt das, sobald die Zone Ferienfenster trägt.
  - **Bezugsart:** Der Typtagweg gilt nur für Wohngrößen (Personen, Wohneinheiten) — jede andere
    Bezugsart wird benannt abgelehnt. Die Einheiten `n_E` sind die wirksame Bezugsmenge der Zone.
- **(e) Der Faktor darf negativ sein** — Abweichung vom Auftrag („Faktoren ≥ 0"): `F_TWE,TT` ist eine
  Schwankung um den Jahresmittelwert und in der Richtlinie teilweise negativ (Grundlagen 5,
  Abschnitt 2.5). Geprüft wird deshalb nur, dass er endlich ist; positiv bleibt die **Tagesmenge**
  (Klemmung auf `Q_TT ≥ 0` mit Warnung), und die Anteile eines Tagesgangs sind ≥ 0 mit Summe 1.
- **(f) Energieerhaltung und Kontrolle.** Nach der Klemmung skaliert `Typtagzuordnung` die 365
  Tagesmengen so, dass `Σ Q_d = Q_a` bleibt, und nennt den Faktor als Hinweis. Die gerechnete Zahl
  der Kalendertage je Kategorie hält sie gegen die eingespielte Tabelle; eine Abweichung ist
  erwartbar (das Wetter des Projekts und die Tabelle der Richtlinie stammen aus verschiedenen
  Jahren) und ein **Hinweis**, keine Ablehnung.
- **(g) Tagesgänge sind wahlfrei.** Führt das Paket zu jeder benutzten Kategorie einen Tagesgang,
  trägt er auch die Tagesform (auf Stunden zusammengefasst); sonst bleibt der Tagesgangsatz der Zone
  die Quelle, und ein Hinweis nennt das. Damit ist die Zeile „Tagesgangsatz … VDI 4655 nur über Z4b"
  aus 5.3 erfüllt, ohne dass ein Tagesgangsatz im Katalog entsteht.
- **(h) ZU19 für VDI 4655.** `Referenzlaeufe/Skripte/normzahlen_abgeleitet_bauen.py` rechnet beide
  Normen (`--norm vdi6002|vdi4655|beide`); Ausgabe `Referenzlaeufe/Skripte/vdi4655_abgeleitet.json`
  (committet, 756 Werte, Abweichung 1,16 % bis 50,00 %). Dieselbe Regel wie für VDI 6002, mit einer
  **Ausnahme für ganze Zahlen:** Die Kalendertage je Klimazone weichen um mindestens einen und
  höchstens max(2; 6 %) Tag(e) ab, bleiben ≥ 0, und die Zeilensumme ist wieder genau 365 — innerhalb
  von 6 % ließe sich eine Zahl von drei Tagen nicht verändern (daher die 50 % im Band). Die Faktoren
  werden **nicht renormiert**; die Prüfsumme des Originals gilt für die abgeleiteten Werte nicht
  mehr, und der Kopf der Datei sagt das. **Codes, Zonennamen und Namen der Gebäudevarianten stehen
  nicht in der Ausgabe:** Typtage heißen `TT01…`, Varianten `variante_1…`, von den Zonen bleibt die
  Nummer. Die Wache `TwwKatalogWacheTests` hält die Datei gegen die lokalen Originale unter
  `Referenzlaeufe/Normzahlen/vdi4655/` und schweigt ohne sie. **Die Testdatenbank bleibt ohne
  Typtage** — `Tab_TwwTyptag_IMPORT` ist dort leer.
- **(i) Die Weiche braucht keine Schemaspalte.** Sie steht als `Zapfprofileingang.Typtage`
  (`Typtaganbindung`: Satz, Klimazone, Gebäudeart, 365 Tagesmittel der Temperatur und wahlfrei des
  Bedeckungsgrads). `null` = Formvektor wie im Bestand; gesetzt, aber ohne eingespielte Typtage =
  **benannte Ablehnung** (`ZapfEingabefehler.TyptageUngueltig`), nie ein stiller Rückfall. **Offen
  für Gruppe 2:** Wo die Wahl des Anwenders (Typtagweg ja/nein, Klimazone, Gebäudeart) je Projekt
  gespeichert wird — drei Spalten an `Tab_TwwProjekt` und damit ein weiterer Schemaschritt.
- **(j) Die Auslegung bleibt unberührt.** Wochenreihe, Bedarfstag und Summenlinie rechnen weiter über
  den Formvektor; die Referenzlastprofile sind ausdrücklich nicht für Auslegungsspitzen gedacht
  (Grundlagen 5, Abschnitt 7.4).
- **(k) Prüfnaht für den Rollback.** `TwwTyptagCtrl.Pruefnaht` ist ein `static Action` mit
  folgenloser Vorbelegung, das allein die Probe des Rollbacks belegt (Muster der Test-Naht in
  `SchemaMigration`).

**Abnahme (Gruppe 1).** Kern-Filter 0 Fehler; Windows-Schale mit `-p:EnableWindowsTargeting=true`
0 Fehler; `SqlDialektPruefer` 0 Fundstellen; Auslieferungsvorlage-Tests grün (132 STRICT-Tabellen);
Referenzlauf der fünf CI-Projekte gegen `2026-09-24_R14_Kaelteerzeuger` **PASS und byte-gleich**;
Testdatenbank auf Schemastand 131 (Tabelle leer, LFS-Zeiger 133 Byte); `ResourceDesigner` ohne Diff.

**Folgen:**

| Folge | Was | Wer | Wann |
|---|---|---|---|
| (a) | **Gruppe 2: Importdialog** — Datei über `Dienste.Datei` (Startordner `Zapfprofil.Importordner`, Muster `VDI3805Path`), Stand, Löschen, Bericht, Hülle und DTO, beide Sprachen, Hilfeschlüssel | Agent der Stufe Z4b | nächster Auftrag |
| (b) | Speicherort der Wahl (Typtagweg, Klimazone, Gebäudeart) je Projekt: Schemaschritt mit drei Spalten an `Tab_TwwProjekt` | Agent der Gruppe 2 | Gruppe 2 |
| (c) | `Tab_Solar.Bedeckungsgrad` ist in der Testdatenbank leer — ein Referenzfall der Wetterkopplung braucht einen TRY-Import (Muster `TryPaketLeser`) | Agent der Stufe Z5 | Z5 |
| (d) | `Tab_Klimaregion` führt keine TRY-Zone: Die Klimazone wählt der Anwender im Dialog; eine Zuordnung über Ort/PLZ wäre eine eigene Karte | Folgeposten | nach K8 |
| ZU22 | Auslieferung der abgeleiteten VDI-4655-Werte und Vervielfältigungsfrage der Richtlinie (VDI 4655 untersagt schon innerbetriebliche Kopien) | Anwender | mit K3a/K8 |
| Wiki | Abschnitt „Typtage (VDI 4655)" der Seite Brauchwasser-Zapfprofil; Logbuch-Satz (Versionsnummer) | Anwender (Upload gebündelt) | nach Gruppe 2 |

**ZU23 (24.09.2026, wörtlich: „modifiziere die VDI 4655 Originalwerte geringfügig und nehme auf").**
Der Entscheid dehnt ZU19 auf jedes Papier des Repositoriums aus: Alle Originalwerte der VDI 4655,
die im Repositorium stehen, werden durch geringfügig abweichende Werte nach der Regel ZU19 ersetzt;
die Originale bleiben lokal und gitignoriert unter `Referenzlaeufe/Normzahlen/vdi4655/`. Umgesetzt:

- **Die Ableitung** (`Referenzlaeufe/Skripte/normzahlen_abgeleitet_bauen.py`, `--norm vdi4655`)
  führt neben den Rechenwerten den Abschnitt `papierwerte` mit allem, was allein das
  Grundlagenpapier braucht: Jahresmittel der Außentemperatur je Klimazone, die beiden
  Urlaubstaganteile, die Jahresstrombedarfe und die Beispielrechnung des Abschnitts 8. Jahres-TWW-
  und Jahresstrombedarf des Beispiels und dessen zehn Tages-TWW-Energien werden nicht einzeln
  gestört, sondern aus schon abgeleiteten Werten nach Gleichung (3) gerechnet, damit das Papier in
  sich stimmt. Drei neue Wachen: kein abgeleiteter Wert gleicht einem kennzeichnenden Originalwert
  (auch nicht dem einer anderen Zelle), die Reihe der Jahresstrombedarfe je Person fällt weiter,
  und die Jahresmittel meiden zusätzlich den Satz ihrer eigenen Spalte. Die bisherigen Abschnitte
  der JSON-Datei bleiben Wert für Wert gleich; zwei Läufe schreiben dieselben Bytes.
- **`Dokumentation/aktuell/Grundlagen_5_VDI-4655_Auswertung.md`** trägt keinen Zahlenwert der
  Richtlinie mehr: 450 Faktoren, 300 Typtagzahlen samt neu gerechneten Heiztagen, 15 Jahresmittel,
  Tabelle 16 mit allen drei Spalten und den daraus gerechneten Anteilen, Jahresstrombedarfe,
  Jahres-TWW-Kennwerte, Heiz- und Wintergrenze, Bewölkungsschwelle, Urlaubstaganteile. Ein
  Hinweisabsatz am Anfang nennt Entscheid, Regel, Skript und Ausgabe. Abschnitt 3.3.4 sagt jetzt,
  dass die Prüfsummen Σ n_TT·F_TWE,TT ≈ 0 für die Werte der Richtlinie gelten, nicht für die
  abgeleiteten Zahlen des Papiers — geprüft wird das vom Anwender eingespielte Paket. Fundstellen
  sowie Struktur- und Geltungsangaben bleiben unverändert.
- **Nachweis:** 799 Tabellenzellen Zelle gegen Zelle gegen die lokalen Originale gehalten — kein
  Feld gleich; keine der 461 kennzeichnenden Originalzahlen (nicht ganzzahlig, mindestens drei
  signifikante Ziffern und zwei Nachkommastellen) und keine ihrer Schreibweisen mehr im Papier.
  Kleine ganze Zahlen und Zahlen mit einer Nachkommastelle bleiben aus der Tokenprobe heraus: Sie
  sind von Seiten-, Tabellen-, Abschnitts- und Fassungsnummern nicht zu unterscheiden; für sie
  zählt die Probe Zelle gegen Zelle.
- **Offen, dem Agenten der Gruppe 2 zugeschrieben:** Die Testproben halten die drei Grenzwerte noch
  im Wortlaut der Richtlinie (`EPOS.Kern.Tests/Typtagpaketbauer.cs`,
  `EPOS.Kern.Tests/TyptagzuordnungTests.cs`, `EPOS.Kern.Tests/NormformvektorleserTests.cs`,
  `Werkzeuge/Auslieferungsvorlage.Tests/TwwVorlageTests.cs`) — sie sind auf erfundene Werte zu
  stellen. Außerdem führt `Dokumentation/aktuell/Konzept_TWW-Zapfprofile_WP-Plan_1.md` die
  Jahresanker und die Beispielrechnung noch im Original (Kapitel „VDI-4655-Anker").

**Nachbesserung Gruppe 1 (24.09.2026) — nach der Gegenprüfung.** Die Gegenprüfung fand sieben
Punkte; alle sind umgesetzt. Was hier steht, gilt gegenüber (e), (f) und (g) oben vor.

- **(N1) Der Typtagweg trägt jetzt auch die stochastische Jahresreihe** (Befund hoch): Bisher
  überschrieb der Rechenweg „stochastisch" den Typtagweg still — das Ensemble zog seine
  Tagesmengen aus Formvektor, Kalender und Kaltwasserfaktor und ersetzte die Typtagreihe damit
  vollständig; die Energieprobe warnte ohne erkennbaren Grund. Jetzt zieht das Ensemble über die
  **Tagesmengen des Typtagjahres** (`Jahreszone.TyptagmengenKwh`, je Einheit geteilt), und führt
  das Paket Tagesgänge, auch über die **Tagesform des Typtags** (`Typtagzuordnung.Dichten`,
  `Tageszeitdichte.Aus(double[])`). Die Energieprobe hält die gezogene Reihe damit gegen die
  Typtagreihe, nicht gegen den Formvektor. Die **Entkopplung der Urlaube entfällt** auf dem
  Typtagweg — dort wirkt kein Ferienfenster (Hinweis wie bisher); ein unbrauchbarer Typtageingang
  wird benannt abgelehnt (`EINGABE_JAHRESZONE_TYPTAGE`, `EINGABE_JAHRESZONE_TYPTAGE_URLAUB`).
- **(N2) Nicht die Tagesmenge wird geklemmt, sondern der Faktor genullt** (gilt vor (e) und (f)):
  Grundlagen 5, Abschnitt 2.5, Anmerkung zu Gl. (1)–(3) verlangt, für die betroffene
  Typtagkategorie **den Faktor auf 0 zu setzen**; ihr Tag trägt dann den Mittelwertanteil
  `Q_a/365`. Die Entscheidung fällt **je Typtag** (der Faktor ist innerhalb eines Typtags
  derselbe), die Skalierung auf `Σ Q_d = Q_a` folgt danach. Hinweis und Warnungstitel heißen
  jetzt `TYPTAGE_FAKTOR_NULL`.
- **(N3) Das Tagesgangraster muss sich stündlich summieren lassen:** `Normformvektorleser`
  verlangt neben „teilt 1440" auch „Teiler oder Vielfaches von 60"
  (`AufloesungTauglich`) — 16 Minuten teilen den Tag, lassen sich aber nicht auf Stunden
  zusammenfassen. Benannte Ablehnung; `TwwTyptagCtrl` hält dieselbe Schranke beim Lesen aus der
  Tabelle. Zu (b) gehört damit: `aufloesung_min` ist ein Teiler oder ein Vielfaches von 60.
- **(N4) Der Merkmalsdreier ist der Schlüssel:** Zwei Kategorien mit gleicher Jahreszeit, Tagart
  und Bewölkung waren nicht unterscheidbar, die zweite blieb still ungenutzt — jetzt benannt
  abgelehnt. Dazu eine **Mengengrenze des Archivs** wie im TRY-Paketleser, allein aus dem
  Zentralverzeichnis und vor dem Entpacken (200 Einträge, 64 MB entpackt).
- **(N5) Der Kaltwasserfaktor wirkt auf dem Typtagweg nicht.** Die Gleichung (3) der Richtlinie
  kennt keine Kaltwasserkorrektur der Tagesmenge; `Kaltwassergang.Monatsfaktoren` bleibt deshalb
  ohne Wirkung, sobald die Typtage rechnen — die Jahreszeit steckt in den Typtagfaktoren selbst.
  Die Spreizung θ_Zapf − θ_KW(m) wirkt weiter, wo sie hingehört: in den Zapfereignissen der
  stochastischen Reihe und in der Literanzeige.
- **(N6) ZU23 abgeschlossen** (der offene Punkt des ZU23-Absatzes oben): Die drei Grenzwerte der
  Proben stehen einmal als erfundene Konstanten `Typtagpaketbauer.GRENZE_WINTER`, `GRENZE_HEIZEN`
  und `GRENZE_BEWOELKUNG` — weder Wert der Richtlinie noch abgeleiteter Wert; kein Fall hängt an
  ihrer Höhe. `Konzept_TWW-Zapfprofile_WP-Plan_1.md` trägt die Jahresanker und die
  Beispielrechnung jetzt aus dem Abschnitt `papierwerte` der abgeleiteten Datei samt
  Hinweisabsatz, und in Grundlagen 5 trägt auch das Tagesband des Abschnitts 7.6 die abgeleiteten
  Prozente. **Nachweis** (Python über `git ls-files`, 3 262 Textdateien): Keine Zeile, die einen
  der Grenzwerte nennt, trägt noch eine Originalschreibweise (je Wert ganz, mit Punkt, mit Komma,
  als Bruch), und keine Zeile, die VDI 4655 nennt, trägt noch eine Originalschreibweise eines
  Papierwerts — 0 von 13 Fundstellen des Ausgangsstands (Gegenprobe gegen `6a2b351e`).
- **(N7) Die Wache und die Proben:** Die Spanne der abgeleiteten ganzen Zahlen hat ihre eigene
  Konstante (`GANZ_BAND` = 0,06, dieselbe Zahl wie das Skript; `BAND` = 0,059 gilt nur den reellen
  Werten). Die Wache prüft zusätzlich die **Vollständigkeit je Abschnitt** gegen die Quelle
  (Faktoren, Kalendertage, benutzte Quellzeilen, Zonenzahl, Summe der Vergleiche), und die
  **Gegenprobe** der beiden Regeln läuft als eigener Fall auch ohne die lokalen Originale — also
  in der CI. Neue Fälle: Typtagweg mit Stochastik (mit und ohne Tagesgänge), Prüfung des
  Typtageingangs der Jahresreihe, Faktornullung, Raster 16 Minuten, zwanzig untaugliche Raster,
  doppelter Merkmalsdreier, Archiv mit zu vielen Einträgen, Feiertag am Samstag, Klimakalender
  ohne Kennzeichen, RFC-4180-Feld mit Anführungszeichen und Trenner.

**Abnahme der Nachbesserung.** Kern-Filter 0 Fehler; voller Testlauf des Filters 0 Fehler;
Auslieferungsvorlage-Tests grün; Windows-Schale mit `-p:EnableWindowsTargeting=true` 0 Fehler;
Referenzlauf der fünf CI-Projekte gegen `2026-09-24_R14_Kaelteerzeuger` **PASS**;
`ResourceDesigner` ohne Diff. Kein neuer Schemaschritt, keine neue Spalte.

**N14, Ergänzung (24.09.2026) — Umsetzungsbefunde Z4b, Gruppe 2 (Importdialog und Projektwahl) und Nachbesserungen**

- **(l) Projektwahl im selben Schritt.** Die Wahl je Projekt — Typtagweg ja/nein, Klimazone,
  Gebäudeart — steht als drei Spalten an `Tab_TwwProjekt` (`Typtage_Aktiv` 0/1, `Typtage_Klimazone`
  > 0 oder NULL = keine Wahl, `Typtage_Gebaeudeart`) im selben Schritt wie die Typtag-Tabelle
  (`TwwSchema.SpaltenT3Typtage`); Folge (b) von N14 ist damit erledigt. `ZapfprofilCtrl.Lies/Speichern`
  tragen die Wahl (vor dem Schritt läuft das Speichern ohne Wahl durch, mit Wahl benannte Ablehnung);
  der Projekttransfer trägt die Wahl, nie die Daten.
- **(m) Der Eingang baut die Anbindung immer, sobald die Wahl steht.** `ZapfprofilCtrl.Eingang` liefert
  dann die 365 Tagesmittel der Temperatur aus `Tab_Klimadaten` und die Tagesmittel des Bedeckungsgrads
  aus den 8 760 Zeilen von `Tab_Solar`; eine Lücke macht die Reihe `null`, die benannte Ablehnung
  leistet `Typtagzuordnung.Zuordnen` (fehlende Daten, Zone, Gebäudeart, Temperatur, Bedeckung) — kein
  stiller Rückfall. Die Testdatenbank führt keinen Bedeckungsgrad; ein Referenzfall der Wetterkopplung
  braucht einen TRY-Import (Folge, Z5).
- **(n) Importdialog.** `TwwTyptagImportDialog.razor` (Überlagerung aus dem Katalogdialog
  „Brauchwasser-Nutzungsarten" und aus dem Zapfprofil-Experten): Stand (Quelle, Ausgabe, Importdatum,
  Zonen, Gebäudearten, Typtage, Auflösungen), Paketwahl über `Dienste.Datei` mit gemerktem Startordner
  `Zapfprofil.Importordner`, Prüfung ohne Schreibzugriff mit Bericht (Datei und Zeile), Einspielen mit
  Rückfrage (ersetzt vollständig), Löschen mit Rückfrage, zwei Herleitungszeilen (anwenderlokal,
  Paketformat). Beim KI-Assistenten als Maske ohne Einstellwert angemeldet (Paketwahl, Einspielen und
  Löschen bleiben Klicks). Hinweise eines gelungenen Einspielens werden gezeigt (Nachbesserung).
- **(o) Wahl im Zapfprofil-Dialog (5.3).** Gruppe „Typtage nach VDI 4655" bei den Fachwerten der
  gewählten Zone — die Wahl gilt dem Projekt, die Herleitungszeile sagt das (Abweichung, benannt);
  Schalter ohne Daten gesperrt mit Grund, Klimazone und Gebäudeart aus dem Stand, eine einzige wird
  vorbelegt, eine gespeicherte Fremdwahl bleibt sichtbar; die Vorschau rechnet über die Wahl; die
  Warnliste zeigt die `ZPG_WARN_TYPTAGE_*`.
- **(p) Wiki und Hilfe.** Abschnitt „Typtage nach VDI 4655" (Anker `typtage`) mit Lizenzhinweis und
  Paketformat in Worten, ohne Zahl der Richtlinie; Hilfeschlüssel `Form_Zapfprofil.grp_Typtage` und
  der des Importdialogs auf denselben Anker.
- **(q) Nachbesserung Gruppe 1.** Siehe den Absatz „Nachbesserung Gruppe 1" (N1)–(N7) oben: das Ensemble zieht über die Typtagmengen samt Tagesform (Typtagweg und Stochastik rechnen zusammen, keine Urlaubsentkopplung), Faktornullung je Typtag nach Grundlagen 5 §2.5, stündlich summierbares Tagesgangraster, Merkmalsdreier als Schlüssel, Mengengrenze des Archivs, erfundene Grenzwerte in den Proben, Wache mit eigener Spanne, Vollständigkeit und Gegenprobe, ZU23 vollständig (Konzept 1 und Grundlagen 5 ohne Originalzahlen, Nachweis 0 Fundstellen).
- **(r) Nachbesserung Gruppe 2.** Die Hinweise eines gelungenen Einspielens bleiben sichtbar (der Prüfbericht wird nur ohne Hinweise weggenommen); Hüllentests `ZapfprofilHuelleTyptageTests` (Gaben, Stand, Prüfung ohne Schreibzugriff, Einspielen/Ersetzen/Löschen, Paketwahl mit gemerktem Ordner, `MitTyptagwahl`, Vorschau über die Hülle); der Auslegungspunkt gilt nur als überholt, wenn der Typtagweg vorher oder nachher trägt; der Katalogdialog meldet einen geänderten Typtagstand mit eigenem Text; Wiki („ein Archiv oder eine Datei des Paketordners") und Kopfkommentar der Typtagzuordnung berichtigt; Konzept 3.1 nennt die Projektspalten im Schritt.
- **(s) Schemanummer.** Gruppe 1 maß 125 als frei; bis zum Abschluss belegten E15 (125), Dialog Design
  (126), E17 (127) und AK1 W3 (128) die Nummern, E16 (129) und Dialog Design #493 (130); Z4b nummerierte beim Abschluss auf
  **131** um; Statusnummer #486 (#481 nahm die parallele Anwender-Sitzung des
  Katalogimport-Fixes).

**Abnahme (Stufe).** Auf dem Stand `40ef6ff3` (Schritt 131, nach Merge 247e2091): Kern-Filter 0 Fehler; voller Testlauf 13 257 grün (1 übersprungen); SqlDialektPruefer 1 836 Texte ohne Fund; Auslieferungsvorlage 31/31 mit 133 STRICT-Tabellen; ChartProben 165 Bilder; Windows-Schale 0 Fehler; Referenzlauf der fünf CI-Projekte gegen R14 PASS; Testdatenbank oid a4a88c33…, Typtag-Tabelle leer. Zwei fremde Wachen (E16, #493) prüfen die Zielversion seither nur noch „nicht darüber".

**Folgen (Ergänzung):**

| Folge | Was | Wer | Wann |
|---|---|---|---|
| (m) | Referenzfall der Wetterkopplung mit `Tab_Solar.Bedeckungsgrad` (TRY-Import) | Agent der Stufe Z5 | Z5 |
| (n) | Hüllentests für Stand/Prüfen/Einspielen/Löschen/Paketwahl und `MitTyptagwahl` | Agent eines Folgepostens | Z5 |
| ZU22 | Auslieferung der abgeleiteten VDI-4655-Werte; Vervielfältigungsfrage (VDI 4655 untersagt innerbetriebliche Kopien) | Anwender mit K3a/K8 | vor der Auslieferung |
| Wiki | Abschnitt „Typtage nach VDI 4655" hochladen; Logbuch-Satz mit Versionsnummer | Anwender (Upload gebündelt) | nächster Upload |
| Sicht | Sichtabnahme unter Windows (Übergabe, Abschnitt 12) | Anwender | nach dem Push |

### N15 (25.09.2026) — Umsetzungsbefunde Z5 (Kalibrierung und Validierung): Messreihen, Vergleich, Katalog, Oberfläche

**Anlass.** Stufe Z5 nach Kapitel 7: Messdatenimport, Vergleichsbericht mit den Validierungskennzahlen,
Kalibrierung (Jahresmesswert, Nichtwohn-Parameter), Katalogausbau, Nichtwohn-Zapfkategorien,
Oberfläche. Drei Gruppen (Kern · Katalog · Oberfläche) durch Agenten mit `model: opus` im Worktree
`z5`, je Gruppe eine Gegenprüfung und eine Nachbesserung; Schemaschritt T4 „Messreihen" =
**140** (`TwwSchema.SCHRITT_T4_MESSREIHEN`, symbolisch als Nachfolger des letzten fremden
Schritts; die Nummer wanderte während der Stufe von 132 über 135, weil die Anlagenkopplung und die
Gebäudesimulation 132–137 belegten); Protokoll
[Z5](../ueberholt/Protokolle/Zapfprofilgenerator/2026-09-25_Z5_Kalibrierung.md), Statuszeile #495.
Messdaten von INEKON-Projekten lagen nicht vor (K5); validiert wurde mit erfundenen Reihen und der
freien OpenDHW-Datei, alle Vergleichsergebnisse sind Verhältniszahlen.

**Befunde und Festlegungen (Kern, Gruppe 1):**

- **(a) Schemaschritt 140 `Tab_TwwMessreihe`** (STRICT, eine Zeile je Wert): `ID`, `ID_Projekt`
  (Fremdschlüssel auf `Tab_Projekt`, ON DELETE CASCADE), `Bezeichnung`, `Groesse` (ENERGIE | VOLUMEN |
  LEISTUNG), `Aufloesung_min` 1…1440, `Beginn` (ISO mit Uhrzeit), `Zeilenindex` ≥ 0, `Wert` ≥ 0,
  `Quelle`, `Datum_Import`; eindeutig über (`ID_Projekt`, `Bezeichnung`, `Zeilenindex`). Messreihen sind
  Projektdaten: Projekttransfer und Projektkopie tragen sie, die Auslieferungsvorlage leert die Tabelle
  (eigener Prüfposten). Der zusätzliche Index auf `ID_Projekt` ist neben dem eindeutigen Index
  redundant und in einem späteren Schritt zu entfernen (Folge). Neue Wache: Schrittnummern der
  Migration lückenlos aufsteigend (löst symbolische Konstanten auf).
- **(b) Messreihenleser.** CSV mit Kopfzeile; Trenner `;`, Tabulator oder `,` (häufigster der
  Kopfzeile, Gleichstand `;`); Zeitstempel ISO, Datum + Uhrzeit oder getrennte Spalten; Dezimalkomma
  zulässig, wenn das Komma nicht Trenner ist; Einheit im Kopf der Wertspalte (`[kWh]`, `[m³]`, `[kW]`)
  wählt die Größe, die Option schlägt sie; genau so viele Felder wie Kopfspalten; Auflösung aus dem
  kleinsten positiven Abstand (1, 5, 10, 15, 60 Minuten, Tag); Lücken werden mit 0 gefüllt, gezählt
  und benannt, über `Zapfprofil.Validierung.Lueckenanteil` abgelehnt; Grenzen 600 000 Zeilen und
  64 MiB; negative Werte und NaN benannt abgelehnt. **Sommerzeit:** Option Ortszeit/Normalzeit; in
  Ortszeit gilt der doppelte Zeitstempel der Herbstumstellung einmal als Folgeschritt (Hinweis), die
  fehlende Stunde im Frühjahr als Lücke. Stundenwerte entstehen nur aus vollständigen Stunden; die
  Ablage unterscheidet gefüllte Lücken nicht von Zeiten ohne Zapfung (Hinweis beim Rücklesen).
- **(c) Vergleichsbericht (`Messvergleich`), nur Verhältniszahlen (K5).** (1) Jahresenergie
  gemessen/gerechnet über genau die abgedeckten Tage (Teiljahr benannt); (2) **Band P85–P95 der
  Dauerlinie:** Das Band ist das Quantil der **synthetischen Dauerlinie** — der 8 760 sortierten Stundenwerte der gerechneten Reihe —, bezogen auf deren größte Stundenleistung; dagegen wird die Messspitze (größter Stundenwert der Messung / größter gerechneter Stundenwert) gehalten. Lehre 1 („Messspitze ≈ P90 der synthetischen Dauerlinie") ist eine Aussage über die eine gerechnete Reihe und gilt auch für eine deterministische Rechnung ohne Ensemble; die Bandgrenzen liegen unter 1, eine Messung auf dem gerechneten Maximum liegt oberhalb. Wie weit die Stochastik streut, ist eine eigene Kennzahl `Spitzenstreuung` (Quantile der Realisierungsspitzen, ohne Ensemble benannt unbestimmt); die erste Fassung hatte beides in einer Zahl vermischt und war damit unerfüllbar (Gegenprüfung). (3) √N-Skalierung: Verhältnis der Spitzen gemessen/gerechnet mal √N
  über die Einheitenzahl der Zonen (1 = folgt dem Gesetz); (4) Formabgleich je Tagtyp: mittlere
  absolute Abweichung der 24 Stundenanteile gegen `Zapfprofil.Validierung.Formschwelle`, dazu der
  verschobene Anteil ½·Σ|a−b|; (5) Monatsanteile beider Seiten über die abgedeckten Monate mit der
  größten Abweichung. Die Messung bleibt im echten Kalender; Feiertage zählen als Werktag ihres
  Wochentags (Hinweis an jedem vollen Tag), Schalttag und Lücken werden benannt.
- **(d) Kalibrierung.** Der Jahresmesswert aus der Reihe geht denselben Weg wie der Handwert (4.1,
  `Mengengeruest.Kalibrieren`): die Jahresenergie ist danach exakt der Nettomesswert; kürzere Reihen ab
  `Zapfprofil.Validierung.Kalibrierung.MindestTage` werden mit dem Jahresgang der gerechneten Reihe
  hochgerechnet (flach nur ohne Rechnung), der Bias ist benannt. **Nichtwohn-Parameter** als
  Vorschlag (nichts wird gespeichert): Tagesbedarf, Wochenfaktoren (Σ 1) und Tagesgänge je Tagtyp
  (Σ 1) als Lösung der kleinsten Quadrate über die Stundenanteile (a_h = Σ Q·x / Σ Q², nichtnegativ);
  Übernahme als Anwenderkopie über `TwwNutzungsartCtrl.VorschlagUebernehmen` (EIGEN, „…-E<n>",
  Herkunftsart VERFAHREN, Quelle „Kalibriert aus Messreihe …", Kategorien mitkopiert, Bandbreite
  entfällt, Jahresgang bleibt).
- **(e) Parameter des Paketteils (ZU21, Fortschreibung):** `Zapfprofil.Validierung.Band.Unten` 0,85,
  `…Band.Oben` 0,95, `…Formschwelle` 0,01, `…Lueckenanteil` 0,05, `…Kalibrierung.MindestTage` 30.

**Befunde und Festlegungen (Katalog, Gruppe 2):**

- **(f) Katalogausbau.** Die Ableitung nach ZU19 deckt die lokalen VDI-6002-Originale vollständig ab
  (acht Nutzungsarten, fünf Profilsätze); neu im Katalog ist das Ein- und Zweifamilienhaus (Bezugsart
  Personen, Wochen-/Monatsgang und geteilter Tagesgangsatz der Gruppe „Wohnen groß", mittlerer Bedarf
  als Mitte der abgeleiteten Spanne — zwei Setzungen, ZU21). Campingplatz und Hallenbäder bleiben
  ausgelassen (keine Bezugsart des Schemas, keine Profile). **Das Ziel „25–27 Typen" ist aus VDI 6002
  nicht erreichbar**: die Zahl stammt aus DIN EN 12831-3 Beiblatt A100, die K1/K8-gesperrt bleibt.
  Wege: externes Katalogpaket außerhalb des Repositoriums (Kapitel 6 (b)) oder eine Ausdehnung der
  Regel ZU19 auf die A100 — **ZU24, Anwender**.
- **(g) Nichtwohn-Zapfkategorien.** Der Paketteil führt zwei Vorgabesätze, getrennt durch die
  Steuerspalte `Gruppe` (Erweiterung des Paketformats N2, keine Tabellenspalte): Wohnen = die vier
  Kategorien nach Jordan/Vajen; Nichtwohnen = zwei Kategorien nach dem OpenDHW-Muster (Kurzzapfung,
  Duschzapfung; Werte freie Modellannahme, ZU21). Gruppenregel = Kalenderart (1 Wohnen, sonst
  Nichtwohnen), eine Quelle `TwwSchema.Kategoriengruppe` für Vorlage, Skript, Kern, Import und Wachen;
  ein älterer Paketteil ohne Spalte bindet wie bisher.

**Befunde und Festlegungen (Oberfläche, Gruppe 3):**

- **(h) Oberfläche.** Messdaten-Dialog (`TwwMessreihenDialog`, Überlagerung aus dem Zapfprofil-Dialog): Liste der Reihen des Projekts, Dateiwahl über `Dienste.Datei` mit gemerktem Ordner, Eingaben Größe, Lückenschwelle, Zeitstempel (Ortszeit/Normalzeit) und Bezeichnung, sofortige Prüfung ohne Schreibzugriff, Einspielen und Löschen mit Rückfrage, Herleitungszeilen zur Projektbindung und zu den Nullläufen; KI-Maske ohne Setzweg. Vergleichsbericht im Reiter Kennzahlen (ab Erweitert) mit Reihenwahl, nebenläufigem Lauf mit Fortschritt und Abbruch, acht Kennzahlenzeilen und einer Formzeile je Tagtyp, Strich mit Grund statt Null (die Spitzenstreuung bleibt in der Oberfläche unbestimmt, weil `Jahresensemble.StundenspitzenKw` nicht im Ergebnis reist — Folge), Veraltet-Markierung. Knöpfe „Aus Messreihe kalibrieren" (füllt Jahresmesswert, Einheit, Bilanzgrenze, Quelle und Zeitraum, nennt Hochrechnung und Bias; eine eigene Eingabe räumt die Kalibrierhinweise) und „Vorschlag übernehmen…" (Vorschau von Tagesbedarf, Wochenfaktoren und Tagesgängen, Rückfrage, Umstellung der Zone auf die Kopie). Warnliste mit den 32 Validierungs- und Kalibrierhinweisen. Kein neues Diagrammbild: 5.6 sieht für den Vergleich keines vor, die Abnahme sind Zahlen. Wiki-Abschnitte „Messdaten" und „Vergleich und Kalibrierung". Nebenfund: Esc des Zapfprofil-Dialogs prüfte die Typtag-Überlagerung nicht (behoben).

**Gegenprüfungen und Nachbesserungen.** Gruppe 1: das Band war zunächst aus den Realisierungsspitzen
gebildet und damit unerfüllbar (hoch), die Herbstumstellung wurde abgelehnt (hoch), die Schrittliste
hatte eine Lücke — alles behoben; Teiljahr, Ausgleichsrechnung, Hochrechnung, Lückenzahl und
Stapelablage nachgezogen. Gruppe 2: der Katalogimport bindet die Vorgabesätze je Gruppe; Provenienz-
texte ohne Stufenkürzel. Gruppe 3: die Kalibrierung aus der Messreihe rechnet die Jahresreihe nur bei Teiljahr und dann nebenläufig mit Fortschritt und Abbruch; die Messdaten-Prüfung läuft nebenläufig und nur bei Größe, Lückenschwelle, Zeitstempel oder Datei neu; die Vorschlagshinweise stehen in der Warnliste und überleben die Übernahme; Kreuz und Esc beider Überlagerungen lesen neu; der Vergleichsbericht rechnet mit der mengengewichteten Spreizung aller Zonen und benennt sie bei Volumenreihen; ein Katalogpaket ohne Vorgabesatz wird benannt statt geraten.

**Abnahme (Stufe).** Die Abnahme nach Kapitel 7 ist im Worktree an synthetischen Reihen erfüllt: die Messspitze liegt im P85–P95-Band der synthetischen Dauerlinie, die Spitzenstreuung skaliert mit √N, der Formabgleich je Tagtyp hält die Schwelle, und die Jahresenergie ist nach der Kalibrierung gleich dem Messwert; Gate und Referenzlauf gegen R14 sind grün. Echte Messreihen lagen nicht vor (K5) — die Validierung an Messdaten steht aus und ist der eigentliche Nachweis der Stufe.

**Folgen:**

| Folge | Was | Wer | Wann |
|---|---|---|---|
| ZU24 | 25–27 Katalogtypen: ZU19 auf DIN EN 12831-3 A100 ausdehnen oder externes Katalogpaket | Anwender | vor der Auslieferung |
| K5 | Freigabe von INEKON-Messreihen für die Validierung; Validierungsbericht mit echten Reihen | Anwender, Agent eines Folgepostens | nach Freigabe |
| ZU7 | Referenzprojekt auf den Generator umstellen, vierte Einfrierregel, Basis neu einfrieren | Agent eines Folgepostens, mit den Nachbarsitzungen abgestimmt | nach Sichtabnahme Z1–Z5 |
| (a) | redundanten Index auf `Tab_TwwMessreihe.ID_Projekt` entfernen | nächster Schemaschritt des Zapfprofils | bei Gelegenheit |
| ZU21 | Setzungen dieser Stufe bestätigen (fünf Validierungsparameter, EFH-Setzungen, Nichtwohn-Kategorien) | Anwender | vor der ersten Auslieferung |
| Wiki | Abschnitte „Messdaten" und „Vergleich und Kalibrierung" hochladen; Logbuch-Satz | Anwender (Upload gebündelt) | nächster Upload |
| Sicht | Sichtabnahme unter Windows (Übergabe, Abschnitt 13) | Anwender | nach dem Push |

### N16 — Anwenderentscheide 25.09.2026 (ZU20, ZU21, ZU22, ZU24, K5, ZU7)

**Wortlaut** (Anwender, 25.09.2026): „ZU20: Empfehlung / ZU21: Empfehlung / ZU22: Empfehlung /
ZU24: Empfehlung / K5: später, empfehlung / ZU7: Empfehlung". Jeder der sechs Punkte folgt damit der
Empfehlung des Papiers; Kapitel 9 führt den Stand in der Spalte „Entscheid".

**Inhalt der Entscheide:**

- **ZU20 — die abgeleiteten VDI-6002-Werte werden ausgeliefert.** Die Katalogtypen nach ZU19 gehören
  zur Auslieferung. Im Katalog trägt jede dieser Zeilen einen Herkunftsvermerk „abgeleitet aus
  VDI 6002"; der Vermerk ist Teil der Auslieferung, nicht nur der Herleitung.
- **ZU21 — die Setzungen des freien Paketteils bleiben ungeliefert.** Bis zur fachlichen Durchsicht
  durch den Anwender wird der freie Paketteil nicht ausgeliefert. Zur Durchsicht liegt eine Prüfliste
  vor: [`2026-09-25_Pruefliste_ZU21_Setzungen.md`](Zapfprofilgenerator/2026-09-25_Pruefliste_ZU21_Setzungen.md)
  — je Setzung Parameter, heutiger Wert, Einheit, Quelle mit Datei und Zeile, Begründung aus dem
  Nachtrag und eine leere Spalte „Entscheid". Der Anwender bestätigt oder ändert jede Zeile; erst
  danach geht der Paketteil in die Auslieferung.
- **ZU22 — die abgeleiteten VDI-4655-Werte werden nicht ausgeliefert.** Der heutige Weg bleibt: die
  Typtage kommen beim lizenzierten Anwender aus einem eigenen Paket über den Import (Stufe Z4b),
  nie aus der Auslieferung, nie aus dem Repositorium.
- **ZU24 — die Katalogtypen 25–27 kommen als externes Katalogpaket.** Hotel, Krankenhaus,
  Sportstätte und die übrigen Typen des Beiblatts A100 der DIN EN 12831-3 werden **nicht** durch eine
  Ausdehnung der Regel ZU19 auf die A100 erzeugt, sondern als Katalogpaket außerhalb des
  Repositoriums beim Anwender geführt und über den Katalogimport bzw. die Auslieferungsvorlage
  eingespielt (Weg ZU14). Das Repositorium bekommt eine Paketvorlage **ohne Werte**.
- **K5 — zurückgestellt („später").** Die Freigabe von Messreihen bleibt offen; die Validierung
  rechnet weiter mit erfundenen Reihen und Verhältniszahlen. Empfehlung für den späteren Entscheid:
  zwei bis drei Mehrfamilienhäuser und ein Nichtwohnobjekt, je mindestens ein Messjahr, CSV mit
  Stundenwerten, anonymisiert — Objektdaten kommen nie ins Repositorium.
- **ZU7 — das Referenzprojekt auf dem Generator wartet.** Die Umstellung eines Referenzprojekts, die
  vierte Einfrierregel (3.4) und das neue Einfrieren der Basis geschehen erst **nach** der
  Sichtabnahme der Stufen Z1–Z5 unter Windows und **nach** K5. Bis dahin bleibt der Generator durch
  Kern-Tests auf einer Projektkopie der Testdatenbank gedeckt.

**Umsetzungsstand im Bestand** (geprüft am 25.09.2026, nur gelesen; keine Änderung an Skript,
Paket, Testdatenbank oder Werkzeug):

- **ZU20 ist heute nicht erfüllt** — der Entscheid ist jünger als der Bestand. Der Katalogtext lautet
  „VDI 6002 Blatt n (abgeleitet)" (`Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py:180`, Zusatz
  `:131`, Ausgabe `:130`), die Wendung „abgeleitet aus VDI 6002" steht allein im Kopf der
  Zwischendatei (`Referenzlaeufe/Skripte/normzahlen_abgeleitet_bauen.py:296`) — sinngemäß, nicht
  wörtlich. Schwerer wiegt der Weg: die abgeleiteten Zeilen tragen `Herkunftsart = FIKTIV`
  (`tww_testkatalog_fiktiv.py:129`) und `Status = EIGEN` (`:91`) und fallen daher in der
  Auslieferungsvorlage (`Werkzeuge/Auslieferungsvorlage/TwwKataloge.cs:139`, `:152`–`:157`,
  Tagesgangsätze über ihre Tagesgänge `:161`–`:164`, Prüfposten `:849`); das Skript sagt es selbst
  (`tww_testkatalog_fiktiv.py:40`–`41`: gewollt, solange ZU20 offen ist). Ein Träger fehlt zudem:
  `Referenzlaeufe/Katalogpaket_frei/` führt keine CSV für Nutzungsart, Tagesgangsatz und Tagesgang
  (`Referenzlaeufe/Katalogpaket_frei/LIESMICH.md:21`), und keine Tww-Zeile der Testdatenbank trägt
  `Status = AUSLIEFERUNG` oder `ReadOnly = 1`. Die Regel selbst steht: `TwwKataloge.cs:209`–`:211`
  setzt `ReadOnly = 1` für jede Zeile mit `Status = 'AUSLIEFERUNG'`, Prüfposten `:815`–`:816`,
  festgehalten von `Werkzeuge/Auslieferungsvorlage.Tests/TwwVorlageTests.cs:35` (T1, Zusicherungen
  `:74`–`:75`, `:81`). **Der Provenienztext wurde nicht geändert**, weil die Wache ihn festnagelt
  (`EPOS.Kern.Tests/TwwKatalogWacheTests.cs:68`, Prüfungen `:158` und `:310`) und das Skript
  nachrechnet: Text und Testdatenbank müssen in einem Schritt wandern — Folgeposten ZU20 unten.
- **ZU22 ist erfüllt.** Die Auslieferungsvorlage leert `Tab_TwwTyptag_IMPORT`
  (`Werkzeuge/Auslieferungsvorlage/TwwKataloge.cs:183`–`:184`, Regelsatz `:120`–`:121`, Bericht
  `:222`, Prüfposten `:858`–`:863`; Test
  `Werkzeuge/Auslieferungsvorlage.Tests/TwwVorlageTests.cs:375` (T12), Zusicherungen `:408`, `:409`,
  `:413`, `:416` — die Tabelle bleibt im Schema und ist leer). Die drei Projektspalten
  `Tab_TwwProjekt.Typtage_Aktiv`, `Typtage_Klimazone` und `Typtage_Gebaeudeart`
  (`EPOS.Kern/Allgemein/Update/TwwSchema.cs:722`, `:729`, `:736`, Definitionen `:757`–`:763`) fallen
  als Projektdaten mit `Tab_TwwProjekt` (`Werkzeuge/Auslieferungsvorlage/Projektsicht.cs:61`, Regel
  `TwwKataloge.cs:31`–`:33`) — ohne eigenen Prüfposten.
  `Referenzlaeufe/Skripte/vdi4655_abgeleitet.json` sät **kein** Skript in die Testdatenbank; gelesen
  wird die Datei nur als Prüfpaket (`EPOS.Kern.Tests/NormformvektorleserTests.cs:98`–`:101`,
  `EPOS.Kern.Tests/TwwKatalogWacheTests.cs:473`), `tww_testkatalog_fiktiv.py:128` liest allein
  `tww_katalogwerte_abgeleitet.json`. Keine Tww-Zeile der Testdatenbank führt „4655" in einer
  Quellenspalte, und `Tab_TwwTyptag_IMPORT` hat schemaseitig weder `Status` noch `ReadOnly`
  (`TwwSchema.cs:671`–`:672`, `:682`–`:694`) — eine 4655-Ableitung mit `Status = AUSLIEFERUNG` ist
  dort nicht darstellbar.
- **Restlücke ohne Entscheidbruch.** Ein mit `--beispiele` mitgenommenes Projekt könnte
  `Typtage_Aktiv = 1` tragen, während die Tabelle leer ist; dafür fehlt ein Prüfposten. Folgenlos,
  weil der Kern dann benannt ablehnt (`ZapfEingabefehler.TyptageUngueltig`,
  `EPOS.Kern/Allgemein/Zapfprofil/Zapfprofileingang.cs:68`,
  `EPOS.Kern/Controller/ZapfprofilCtrl.Eingang.cs:108`) und nie still auf den Formvektor zurückfällt.
- **Verweisfehler in den Papieren.** Die Zeilen zu ZU21 zitieren „N12 (u)"; N12 endet bei (t). Die
  Setzungen des Paketteils stehen in N12 **(p)**, **(q)** und **(r)**. Kapitel 9 zitiert sie ab jetzt
  richtig; die Zeilen der Nachträge N12–N15 bleiben, wie sie sind (ein Nachtrag wird nicht
  umgeschrieben).

**Folgen:**

| Folge | Was | Wer | Wann |
|---|---|---|---|
| ZU20 | abgeleitete VDI-6002-Zeilen in die Auslieferung heben: CSV-Paketteil für Nutzungsart, Tagesgangsatz und Tagesgang unter `Referenzlaeufe/Katalogpaket_frei/` mit `Herkunftsart FREI`, `Status AUSLIEFERUNG`, `ReadOnly 1`; Herkunftsart im Skript von `FIKTIV` lösen; Provenienztext auf „abgeleitet aus VDI 6002 Blatt n"; Wache und Testdatenbank im selben Schritt nachziehen | Agent eines Folgepostens | vor der ersten Auslieferung |
| ZU21 | Prüfliste durchsehen, je Zeile bestätigen oder ändern; erst danach geht der freie Paketteil in die Auslieferung | Anwender | vor der ersten Auslieferung |
| ZU24 | Paketvorlage für die A100-Typen **ohne Werte** (Spalten, Provenienzpflicht, Beispielzeile mit runden Platzhaltern) im Repositorium; die Werte trägt der Anwender außerhalb ein | Agent eines Folgepostens | vor der Auslieferung |
| K5 | Validierungsbericht mit echten Reihen, sobald der Anwender Messreihen freigibt (zwei bis drei Mehrfamilienhäuser und ein Nichtwohnobjekt, je ein Messjahr, CSV-Stundenwerte, anonymisiert). Das Werkzeug dafür steht samt Ablageregel und Berichtswache, und ein erster Lauf an offen lizenzierten Fremddaten liegt vor (N22) | Anwender, danach Agent eines Folgepostens | nach der Freigabe |
| ZU7 | Referenzprojekt auf den Generator umstellen, vierte Einfrierregel (3.4), Basis neu einfrieren | Agent eines Folgepostens, mit den Nachbarsitzungen abgestimmt | nach der Sichtabnahme Z1–Z5 und nach K5 |
| (Lücke) | Prüfposten der Auslieferungsvorlage: kein Beispielprojekt mit `Typtage_Aktiv = 1` bei leerer `Tab_TwwTyptag_IMPORT` | Agent eines Folgepostens | bei Gelegenheit |

### N17 (25.09.2026) — Umsetzung der Folgeposten ZU20 und ZU24

Zwei Folgeposten aus N16 sind umgesetzt (Statuszeile #504, Protokoll
[`2026-09-25_Folgeposten_ZU20_ZU24.md`](../ueberholt/Protokolle/Zapfprofilgenerator/2026-09-25_Folgeposten_ZU20_ZU24.md));
**kein Schemaschritt** (nur Katalogdaten); der Schemastand der Testdatenbank kommt aus #505 und
steht auf 142. Abschlussstand ist der Merge `70ab531a` (origin `f83ce27d`): Testdatenbank
`22eeb75c…`, Gate **6/6 PASS** gegen die Basis `2026-09-25_R15_Anlagenkopplung`.

**(a) ZU20 — die abgeleiteten VDI-6002-Katalogtypen gehören zur Auslieferung.** Die fünf
Nutzungsarten „Wohnen groß (abgeleitet)", „Ein- und Zweifamilienhaus (abgeleitet)",
„Studentenwohnheim (abgeleitet)", „Seniorenheim (abgeleitet)" und „Krankenhaus (abgeleitet)" samt
ihren vier Tagesgangsätzen und sechzehn Tagesgängen stehen jetzt als **CSV-Träger im freien
Paketteil** `Referenzlaeufe/Katalogpaket_frei/`: `Tab_TwwTagesgangsatz_STAMM.csv` (4 Zeilen),
`Tab_TwwTagesgang_STAMM.csv` (16), `Tab_TwwNutzungsart_STAMM.csv` (5), jede Zeile mit `Status`
`AUSLIEFERUNG`, `ReadOnly` 1 und **ohne** `Katalogversion` (sie tritt der des Zielkatalogs bei —
Regel 2 des Paketteils). Die Zapfkategorien brauchten keine neue Datei: Die beiden Vorgabesätze
binden über die Steuerspalte `Gruppe` von selbst an jede neue Nutzungsart ihrer Gruppe (drei mal
Wohnen mit vier, zwei mal Nichtwohnen mit zwei Kategorien — sechzehn Zeilen).

**Herkunftsart `VERFAHREN`, nicht `FREI`.** N16 hatte `FREI` vorgeschlagen; umgesetzt ist
`VERFAHREN`. Begründung: `FREI` heißt nach der Definition in
[`Provenienz.cs`](../../EPOS.Kern/Allgemein/Zapfprofil/Provenienz.cs) „frei verfügbare Quelle" —
VDI 6002 ist keine. Die Zeile trüge damit eine falsche Aussage über die Richtlinie, sichtbar im
Katalogdialog und im Bericht. `VERFAHREN` heißt „aus einem Verfahren gerechnet", und genau das ist
der Wert: Er kommt aus der Ableitungsregel von
[`normzahlen_abgeleitet_bauen.py`](../../Referenzlaeufe/Skripte/normzahlen_abgeleitet_bauen.py) und
steht in keiner Richtlinie. Die Quelle nennt die Herkunft im Klartext: **„abgeleitet aus VDI 6002
Blatt 1"** bzw. „… Blatt 2", Ausgabe `2014-03`, Provenienz-Version `FREI-1`. Damit ist die Regel 1
des Paketteils erweitert: Eine Zeile trägt `FREI` **oder** `VERFAHREN`; `Werkzeuge/Auslieferungsvorlage`
prüft beide (`TwwKataloge.PAKETTEIL_HERKUNFT`). `FIKTIV` ist damit fort — die drei
„Testnutzung A/B/C (fiktiv)" bleiben `FIKTIV`/`EIGEN` und nur in der Testdatenbank.

**Keine Fundstelle im Quellentext.** Der Auftrag nannte „abgeleitet aus VDI 6002 Blatt n,
Bild/Tabelle …". Bild- und Tabellennummern stehen nicht zur Verfügung: Das Ableitungsskript
übernimmt Seiten- und Tabellenverweise der Quelle bewusst nicht
(`normzahlen_abgeleitet_bauen.py`, Kopf). Der Quellentext nennt deshalb Richtlinie und Blatt, die
Spalte `Ausgabe` den Ausgabestand.

**Die Träger werden erzeugt, nicht getippt.** `tww_testkatalog_fiktiv.py --paketteil-schreiben`
schreibt die drei Dateien aus `tww_katalogwerte_abgeleitet.json`; **jeder** Lauf des Skripts hält die
Dateien im Arbeitsbaum gegen das Erzeugnis und bricht bei einer Abweichung ab. So gibt es eine Quelle
(die JSON) und drei Ablagen, die nicht auseinanderlaufen können: Dateien, Testdatenbank und Vorlage.

**Weitere Umsetzungspunkte:**

1. `Werkzeuge/Auslieferungsvorlage/TwwKataloge.cs`: `PAKETTEIL_TABELLEN` führt die drei neuen
   Tabellen in Einspielreihenfolge (Satz, Gang, Nutzungsart vor Parameter, Bedarfstag, Ereignis,
   Kategorie); `PaketteilLesen` prüft Status und **jede** Herkunftsspalte einer Tabelle (die
   Nutzungsart hat drei, der Tagesgangsatz keine); `PaketteilEinspielen` rechnet die Paket-`ID` des
   Satzes auf die echte um — tritt ein Satz zurück, weil das Katalogpaket ihn führt, treten seine
   Tagesgänge und die Nutzungsarten des Paketteils, die auf ihn zeigen, **benannt** mit ihm zurück.
   Der Prüfposten zählt die Paketteil-Zeilen je Tabelle (Tagesgangsatz über seine Tagesgänge).
2. Testdatenbank neu gesät aus der origin-Fassung: 21 Zellen einer Zeile je Wertgruppe geändert
   (45 Zellen in `Tab_TwwNutzungsart_STAMM`, 48 in `Tab_TwwTagesgang_STAMM` — Quelle,
   Provenienz-Version, Herkunftsart), sonst nichts; Zeilenzahlen unverändert (5 Sätze, 20 Gänge,
   8 Nutzungsarten, 24 Kategorien), `integrity_check` ok, keine `AUSLIEFERUNG`-Zeile (die
   Testdatenbank behält ihre Regel: `EIGEN`, `ReadOnly` 0, `TEST-1`).
3. Wächter: `TwwKatalogWacheTests` nagelt den neuen Quellentext und `VERFAHREN` fest und vergleicht
   die drei neuen Dateien Wert für Wert mit der Testdatenbank (Satz über den Bezeichner, Tagesgänge
   je Tagtyp, Nutzungsart samt Verweis auf ihren Satz); `Auslieferungsvorlage.Tests` bekommt **T13**
   („die abgeleiteten Nutzungsarten stehen in der Vorlage": `AUSLIEFERUNG`, `ReadOnly` 1,
   `VERFAHREN`, Quelle, Satz mit vier Gängen, Vorgabesatz der Gruppe — und keine Testnutzung), die
   Zählungen der übrigen Fälle rechnen jetzt aus den Dateien statt aus festen Zahlen.
4. **Berührt ZU21:** Die neuen Zeilen liegen im **selben** Ordner, den ZU21 bis zur fachlichen
   Durchsicht zurückhält. Das Werkzeug spielt den Ordner immer ein; die Zurückhaltung ist eine
   Sache des Anwenders vor der ersten Auslieferung, keine Codeschaltung. Die Prüfliste ZU21 betrifft
   die Setzungen, nicht die abgeleiteten Werte; die zwei Setzungen des Ein- und
   Zweifamilienhauses (geliehene Formen, Mitte der Spanne) stehen dort weiter zur Bestätigung.

**(b) ZU24 — Paketvorlage für die A100-Typen, ohne Werte.** Neu ist
`Referenzlaeufe/Katalogpaket_Vorlage_A100/` mit den vier Dateien des **Importformats** (Tagesgangsatz,
Tagesgang, Nutzungsart, Zapfkategorie), vollständigen Kopfzeilen und je einer Beispielzeile aus
Platzhaltern: Bedarf 10/20/30 kWh je Einheit und Tag (bewusst außerhalb jeder plausiblen Spanne),
Tagesgang gleichverteilt (1/24 je Stunde, vier Tagtypen), Woche gleichverteilt (1/7), Monatsfaktoren
1, zwei Zapfkategorien mit Anteil 0,5 in der Gruppe `Nichtwohnen`, Bezugsart 3 (Betten),
Bilanzgrenze 1, Kalenderart 5 (Auslastungsgang), Bezugstemperaturen 60/10 °C. Provenienz vorbelegt
mit `Quelle` = „DIN EN 12831-3 Beiblatt A100, Tabelle …" (ohne Zahl), `Herkunftsart` = `IMPORT`,
`Version` = `A100-1`; kein `Status`, kein `ReadOnly`. `Katalogversion` **ist** gesetzt (`A100-1`) —
sie ist im Importformat Pflichtspalte, anders als im freien Paketteil.

Die [`LIESMICH.md`](../../Referenzlaeufe/Katalogpaket_Vorlage_A100/LIESMICH.md) der Vorlage nennt
Zweck, die vier Dateien, sieben Schritte (kopieren, je Typ eine Zeile und ein Tagesgangsatz, Werte
aus dem eigenen Normexemplar, Summenregeln, Provenienz, Import über **Administration → Brauchwasser
→ Katalog-Import…**), die Ablehnungsgründe in zwei Stufen, den Unterschied zum Weg über die
Auslieferungsvorlage und eine Liste **empfohlener Typnamen ohne Werte** (Hotel nach Größe und
Sterneklasse, Hotelküche, Krankenhaus nach Bettenzahl, Klinikum Funktionsgebäude, Schule ohne und
mit Duschen, Sportstätte mit Duschen, Schwimmbad, Bürogebäude, Werkstatt, Kaserne, JVA-Zellentrakt).
Dass die **gefüllte** Datei nie ins Repositorium gehört, steht dort ausdrücklich.

Zwei Fälle in `EPOS.Kern.Tests/TwwKatalogimportTests.cs` halten die Vorlage: Sie spielt **ohne
Ablehnung** ein (eine Nutzungsart, vier Tagesgänge, Herkunftsart `IMPORT`, Vorgabesatz der Gruppe
Nichtwohnen gebunden), und **jede Zahl** ihrer Felder steht in einer Liste von Platzhaltern —
ein eingetragener Normwert fällt sofort auf. Die Wiki-Quelle
`Projekte/Wiki/Programm Dokumentation - Brauchwasser-Zapfprofil.wiki` beschreibt am Anker
`katalog-import` jetzt die Steuerspalte `Gruppe` und die Paketvorlage als Funktion.

**Nebenbefund.** Der freie Paketteil taugt nicht als Katalogimport-Paket: Er führt keine
`Katalogversion`, die der Import als Pflichtspalte verlangt. Vorher scheiterte er dort an der
fehlenden Nutzungsart-Datei, jetzt an der Katalogversion — beides benannt abgelehnt, nichts geändert
(`TwwKatalogimportTests.Ohne_Datei_der_Nutzungsarten_ist_das_Paket_benannt_abgelehnt` prüft es).

**(c) Die Paketvorlage gehört in die Auslieferung.** Ohne Eintrag im Setup läge sie nur im
Repositorium und erreichte den Anwender nie. `Setup/EPOS-Plan.iss` nimmt
`Referenzlaeufe/Katalogpaket_Vorlage_A100/*` nach `{app}\Vorlage\Katalogpaket_A100` (`Components:
programm`, `ignoreversion`) — neben die Vorlagendatenbank, weil der Anwender aus dem Ordner nur
liest und sich eine Kopie herausnimmt; ein `#define` mit `DirExists`-Prüfung bricht wie bei den
Herstellerdaten ab, wenn der Ordner fehlt. Der Deinstallierer nimmt ihn mit `{app}\Vorlage`; der
Wiki-Absatz und die Anleitung nennen den Ablageort. Der Installer selbst ist damit **ungeprüft** —
der Setup-Lauf der CI läuft nur auf Zuruf, die Sichtabnahme steht beim nächsten Setup-Lauf an.

**(d) Herkunftsart beim Einspielen bleibt zweierlei — mit Absicht.** Der Katalogimport setzt die
Herkunftsart jeder eingespielten Zeile auf `IMPORT` (`TwwNutzungsartCtrl.ImportHerkunft`, außer
`FREI` und `FIKTIV`), die Auslieferungsvorlage lässt `FREI` und `VERFAHREN` unangetastet stehen: Ein
eingespieltes Paket **ist** ein Anwenderimport und trägt das auch, während die ausgelieferten Zeilen
des freien Paketteils nie durch den Import, sondern durch `Werkzeuge/Auslieferungsvorlage` laufen.
Beide Wege bleiben, wie sie sind.

**(e) Rückrechenbarkeit.** Ableitungsregel und Liste der Abweichungen zur Richtlinie stehen offen im
Kopf von
[`normzahlen_abgeleitet_bauen.py`](../../Referenzlaeufe/Skripte/normzahlen_abgeleitet_bauen.py):
Wer das Skript und die Ausgangswerte hat, rechnet die ausgelieferten Zahlen auf die Richtlinienwerte
zurück. Bisher reichte das nur bis in die Testdatenbank, mit ZU20 in jede Auslieferung. Das ist kein
Versehen, sondern die Bedingung des Entscheids: ZU19 erlaubt abgeleitete Werte im Repositorium
**nur**, „wenn die Ableitung reproduzierbar und rückrechenbar ist und die Provenienz sie nennt" —
und ZU20 liefert sie auf dieser Grundlage aus. Keine Codeänderung.

**Folgen:**

| Folge | Was | Wer | Wann |
|---|---|---|---|
| Setup | Sichtabnahme des Installers: `{app}\Vorlage\Katalogpaket_A100` mit den vier CSV-Dateien und `LIESMICH.md` | Anwender, beim nächsten Setup-Lauf (`windows.yml`, Schalter „setup") | vor der Auslieferung |
| ZU21 | Setzungen des freien Paketteils bestätigen — die abgeleiteten Werte sind nicht Gegenstand, die zwei Setzungen des Ein- und Zweifamilienhauses schon | Anwender | vor der ersten Auslieferung |
| Logbuch | zwei Sätze: „Der Katalog der Brauchwasser-Nutzungsarten enthält fünf aus VDI 6002 abgeleitete Nutzungsarten mit Herkunftsvermerk." und „Für die Nichtwohn-Nutzungsarten nach DIN EN 12831-3 Beiblatt A100 liegt eine Paketvorlage zum Ausfüllen und Einspielen im Programmordner bei." — Version beim Anwender zu erfragen | Anwender (Upload gebündelt) | nächster Upload |
| Wiki | Absatz `katalog-import` (Steuerspalte `Gruppe`, Paketvorlage samt Ablageort) hochladen | Anwender (Upload gebündelt) | nächster Upload |
| Sicht | Katalogdialog: fünf Typen „… (abgeleitet)" mit Herkunft „Verfahren", Quelle „abgeleitet aus VDI 6002 Blatt n", Stand „Auslieferung"; Katalogimport spielt die A100-Vorlage ohne Ablehnung ein | Anwender | nach dem Push |

### N18 (25.09.2026) — Sammelposten „Zapfprofil-Reste": vier Folgeposten aus N13–N16

**Anlass.** Vier offene Folgeposten der Nachträge N13 bis N16, in einem Zug und **ohne
Schemaschritt** (Testdatenbank unberührt): der fehlende Prüfposten der Auslieferungsvorlage zum
Typtagweg (N16, Restlücke), das Herkunftsprotokoll als Sätze (N13 Folge (b)), der Größenschutz des
Katalogimports (N13 Folge (s)) und die Spitzenstreuung im Vergleichsbericht (N15 Gruppe 3, Folge).
Ein Agent mit `model: opus` im Worktree `zr`, Zweig `zr` von `dbcaf63a` (Schemastand 142,
Referenzbasis `2026-09-25_R16_Anlagenprio`); je Posten ein Commit.

**(a) Prüfposten Typtagweg (N16, Restlücke) — erfüllt.** `Werkzeuge/Auslieferungsvorlage/TwwKataloge.cs`
führt einen siebten Tww-Prüfposten: Ist `Tab_TwwTyptag_IMPORT` leer — und die Vorlage leert sie
immer —, darf kein Projekt der Vorlage `Tab_TwwProjekt.Typtage_Aktiv = 1` tragen. Der Bericht nennt
je Fund die Projektkennung samt gewählter Klimazone und Gebäudeart, denn genau diese Wahl bliebe
liegen. Ohne die Tabelle oder die Spalte ist nichts zu melden; steht eine Typtagzeile, ist der Weg
gedeckt und der Posten schweigt. Test T14 in `Werkzeuge/Auslieferungsvorlage.Tests/TwwVorlageTests.cs`
prüft beide Seiten: ein Lauf ohne Beispielpaket bleibt grün, ein Beispielpaket mit gesetztem
Typtagweg fällt mit Rückgabecode 5 und nennt Zone und Gebäudeart.

**(b) Herkunftsprotokoll als Sätze (N13 Folge (b)) — erfüllt, mit einer Einschränkung.**
`Herkunftseintrag.Vermerk` ist ein `ZapfSatz` statt eines deutschen Klartexts; `null` heißt „nichts
zu vermerken" (vorher der leere Text). 41 Muster `ZPG_SATZ_HERKUNFT_…` in beiden Sprachen, die Wache
`ZapfSaetzeWacheTests` hält sie wie jede andere Kennung. Wo eine Aufzählung den Vermerk bestimmt
(Bedarfsniveau, Übertragerwerkstoff, Zirkulationsmethode, Quelle der Speichertemperatur,
Erzeugerart der Schätzformel), trägt **jede Ausprägung ihre eigene Kennung** — eine Zahl im Vermerk
wäre keine Aussage. Die Formeln (Temperaturfaktor, Flächenkennwert, Kaltwasser- und
Bedarfstagfaktor) tragen ihre Temperaturen als Werte, nicht als zusammengesetzten Text; die
Zahlformate stehen im Muster.

- **Einschränkung: Hülle und Dialog reichen das Protokoll nicht durch — es reist heute nirgends
  hin.** `ZapfprofilErgebnis.Herkunft` und `Auslegungsergebnis.Herkunft` werden von keiner Hülle und
  keiner Seite gelesen (geprüft über den ganzen Baum); das Protokoll ist Rechennachweis für Kern und
  Tests. Der Umbau macht es **anzeigefähig** (die Hülle baut den Satz in der Oberflächensprache, wie
  bei jedem Hinweis), führt die Anzeige aber nicht ein — dafür gibt es keinen Beschluss und keinen
  Ort in 5.x. Folge unten.

**(c) Katalogimport: Größenschutz und Pfadprüfung (N13 Folge (s)) — teils erfüllt.**
`TwwNutzungsartCtrl.PaketLesen` prüft drei Grenzen als benannte Konstanten: `HOECHSTENS_EINTRAEGE`
(200), `HOECHSTENS_BYTE_ENTPACKT` (64 MB) und `HOECHSTENS_BYTE_JE_DATEI` (16 MB). Eintragszahl und
entpackte Gesamtgröße stehen im Zentralverzeichnis und werden geprüft, **bevor ein Byte entpackt
wird** (`KATALOGIMPORT_ZU_GROSS`) — dasselbe Muster wie im `Normformvektorleser` und im
`TryPaketLeser`; die Grenze je Datei (`KATALOGIMPORT_DATEI_ZU_GROSS`) gilt für Archiv, Ordner und
Einzeldatei, weil der Leser jede Datei ganz im Speicher hält. Ein Eintragsname, der aus dem Archiv
herauszeigt (`..`, Wurzel, Laufwerk), wird benannt abgelehnt (`KATALOGIMPORT_PFAD_UNZULAESSIG`);
Verzeichniseinträge fallen still, sie tragen keinen Inhalt.

- **Abweichung: Ein Unterordner bleibt erlaubt.** Der Auftrag sah eine flache Pfadprüfung vor. Ein
  ZIP, das aus einem Ordner entstanden ist, trägt seinen Ordnernamen (`paket/Tab_….csv`) — der
  Bestand liest solche Pakete, und `TwwKatalogimportTests` prüft das ausdrücklich. Abgelehnt wird
  deshalb nur, was aus dem Archiv herauszeigt; der Leser nimmt ohnehin allein den Dateinamen, und
  zwei gleichnamige Dateien fallen schon als `KATALOGIMPORT_DATEI_DOPPELT`. Nichts wird auf die
  Platte entpackt — Zip-Slip im engen Sinn ist hier kein Weg, die Prüfung hält den Namen trotzdem.
- **Weiter offen: Bedarfstage und Parameter deckt der Katalogimport nicht ab.** N13 (s) und die
  Folge nennen die Erweiterung, beschreiben sie aber nicht: Für `Tab_TwwBedarfstag_STAMM`,
  `Tab_TwwBedarfstagEreignis_STAMM` und `Tab_TwwParameter_STAMM` fehlen Dublettenregel,
  Versionsbildung und Berichtszeilen; die Auslieferungsvorlage spielt sie über einen eigenen Weg
  ein (`--katalogpaket`), nicht über diesen Import. **Still ist das nicht:** Eine solche Datei im
  Paket steht als `KATALOGIMPORT_DATEI_UEBERGANGEN` mit Namen im Bericht (geprüft in
  `TwwKatalogimportTests`). Folge unten.

**(d) Spitzenstreuung im Vergleichsbericht (N15 Gruppe 3, Folge) — erfüllt, mit benannter Grenze.**
`Jahresensemble.StundenspitzenKw` reist je Zone ins Ergebnis
(`ZonenErgebnis.StundenspitzenKw`, leer auf dem deterministischen Weg und bei einer abgelehnten
Zone); die Hülle belegt damit `Messvergleich.SynthetischeStundenspitzenKw`, und der Reiter
Kennzahlen zeigt die Spitzenstreuung als Zahl statt als Strich. **Ergebnisneutral:** Kein Rechenweg
liest die Spitzen, die Reihen bleiben Bit für Bit, wie sie waren.

- **Benannte Grenze: nur EINE Zone mit Ensemble.** Tragen mehrere Zonen ein Ensemble, ist die
  Stichprobe nicht zu bilden — jede Zone zieht ihre Realisierungen für sich, und die Spitze der
  Summe ist nicht die Summe der Spitzen. Die Hülle sagt das (`MESSVERGLEICH_ENSEMBLE_ZONEN`, wie
  schon `MESSVERGLEICH_SPREIZUNG_ZONEN` ein Satz der Hülle, nicht des Kerns) und schätzt nicht; die
  Zeile trägt dazu ihren **eigenen** Strichvermerk (siehe Gegenprüfung, Befund 2). Eine
  Stichprobe über die Summe bräuchte die Realisierungen aller Zonen gleichzeitig; das ist eine
  Änderung am Ensemble, nicht am Bericht.
- Der bunit-Fall `Mit_Ensemble_steht_die_Spitzenstreuung_als_Zahl` in `EPOS.UI.Tests` hält beide
  Grenzen, Realisierungszahl und Streubreite gegen die Zeile; die Wiki-Quelle
  „Brauchwasser-Zapfprofil" nennt im Abschnitt „Vergleich und Kalibrierung" die Bedingung.

**Gegenprüfung und Nachbesserung.** Eine zweite Sitzung hat den Stand gegen die Regeln gehalten;
**sechs Befunde**, alle behoben (Zweig `zr`, drei Commits):

1. **Zweite Wand beim ZIP-Lesen (mittel).** Beide Paketleser — `TwwNutzungsartCtrl.PaketLesen` und
   `Normformvektorleser.AusStrom` — prüften die entpackte Größe allein am Zentralverzeichnis und
   lasen den Eintrag danach mit `ReadToEnd()` ohne Grenze. Sie lesen nun je Eintrag bis zur Grenze
   und ein Byte darüber, mit mitlaufender Summe (Muster
   `Allgemein/Import/Ifc/IfcLeser.Entpacken`); die Ablehnung behält ihre Kennung
   (`KATALOGIMPORT_DATEI_ZU_GROSS`, `KATALOGIMPORT_ZU_GROSS`, `NORMVEKTOR_PAKET_ZU_GROSS`), und die
   Grenzen sind Parameter mit den Konstanten als Vorgabe, damit die Prüfung an Kilobyte messbar ist
   statt an 64 MB. **Gemessen dabei:** `ZipArchiveEntry.Open` begrenzt den Entpackstrom selbst auf
   die ausgewiesene Größe — ein lügendes Verzeichnis bläht das Paket also nicht auf, es **kürzt**
   den Eintrag. Die zweite Wand ist damit Vorsorge (der Leser verlässt sich nicht auf eine
   Eigenschaft des Rahmenwerks), und der gekürzte Eintrag fällt der Formprüfung zu. Beides hält der
   Prüfstand `EPOS.Kern.Tests/Archivluege.cs` fest: Er schreibt die ausgewiesene Größe im
   Zentralverzeichnis um und lässt die Nutzlast unberührt; je ein Fall in
   `TwwKatalogimportTests` und `NormformvektorleserTests` zeigt, dass nichts aufgebläht und nichts
   still halb eingespielt wird.
2. **Mehrere Ensemble-Zonen: falscher Strichgrund (mittel).** Die leere Stichprobe der Hülle ließ
   den Kern `MESSVERGLEICH_OHNE_ENSEMBLE` setzen, und die Zeile zeigte „ohne Ensemble nicht
   entscheidbar" — der falsche Grund, denn die Rechnung **ist** stochastisch. Die Hülle legt die
   Zahl der tragenden Zonen nun ins DTO (`ZapfprofilMessvergleichDaten.EnsembleZonen`, 0 bei genau
   einer), und die Zeile trägt den eigenen Vermerk **`ZPG_VERGL_ENSEMBLE_ZONEN`** („mehrere
   stochastische Zonen — Stichprobe nicht bildbar") in beiden Sprachen. Der Hinweis der Warnliste
   bleibt, wie er war. Der Satz der Wiki-Quelle traf schon zu („es steht ein Strich mit diesem
   Grund") — jetzt trägt ihn auch die Maske.
3. **Eintragszahl und Gesamtgröße nur im Archiv (gering).** Der Ordner- und der Einzeldateiweg des
   Katalogimports prüften nur die Größe **einer** Datei. Beide prüfen nun auch Eintragszahl und
   Gesamtgröße (`MengeZuGross`, aus dem Dateisystem statt aus dem Zentralverzeichnis).
4. **`Pfadsicher` lehnte jedes `:` ab (gering).** Ein unter Unix gepacktes Paket darf ein `:` im
   Dateinamen tragen; abgelehnt wird jetzt allein das Laufwerksmuster `^[A-Za-z]:` am Anfang eines
   Pfadteils.
5. **Überlauf der Summe (gering).** Die Summe der ausgewiesenen Größen bricht **an** der Grenze ab,
   statt an erfundenen Längen überzulaufen (200 Einträge mit je 2^62 Byte wären sonst eine kleine
   Zahl gewesen) — im Archiv wie im Dateisystem.
6. **Aufräumen (gering).** Das ungenutzte `using System.Globalization` in
   `EPOS.Kern/Allgemein/Zapfprofil/ZapfprofilRechner.cs` (Rest des entfernten Zahlenformatierers).

**Neu geprüft:** die Hüllen-Weiche „genau eine / mehrere stochastische Zonen" auf der Testdatenbank
(`ZapfprofilHuelleMessreihenTests`), der bunit-Fall der Zeile für **beide** Strichgründe, der
Ordner- und Einzeldateiweg des Größenschutzes, und der Satz `KATALOGIMPORT_ZU_GROSS` nun samt
gemessener und erlaubter Gesamtgröße (`Werte[2]`, `Werte[3]`).

**Folgen:**

| Folge | Was | Wer | Wann |
|---|---|---|---|
| (p) | Konstruktorzeilen des Bedarfstags in der Datenbank (N13 Folge (p)) **und** der redundante Index auf `Tab_TwwMessreihe.ID_Projekt` (N15 Folge (a)) in **einem** künftigen Schemaschritt des Zapfprofils — beide sind reines DDL, ein eigener Schritt je Kleinigkeit kostet eine Nummer und einen Referenzlauf | Agent eines Folgepostens | **erledigt mit N21** (Schemaschritt 145) |
| (r) | Katalogdialog auf iOS (N13 Folge (r)): Naht der Schale, eigene Welle **nach iU11** — die Hülle lehnt ihn dort heute benannt ab, das bleibt bis dahin der Stand | Agent einer iOS-Welle | nach iU11 |
| (m) | Referenzfall der Wetterkopplung mit `Tab_Solar.Bedeckungsgrad` aus einem TRY-Import (N14 Folge (c), N15 Folge (m)): Die Testdatenbank führt keinen Bedeckungsgrad, die Bewölkungsschwelle der Typtagzuordnung ist damit nur an erfundenen Werten geprüft | Agent eines Folgepostens | offen |
| (s) | Katalogimport um Bedarfstage und Parameter erweitern — vorher Dublettenregel, Versionsbildung und Berichtszeilen je Tabelle festlegen (N13 (s) nennt sie, beschreibt sie nicht) | Agent eines Folgepostens, nach Festlegung | offen |
| (b) | Herkunftsprotokoll anzeigen: Ort in 5.x festlegen (Herleitungszeilen der Stufe Experte oder eigene Karte), dann Hülle und Dialog; der Kern ist vorbereitet | Anwender (Entscheid), danach Agent | offen |
| Logbuch | ein Satz: „Der Vergleich einer Messreihe zeigt die Streuung der Realisierungsspitzen, wenn die Jahresreihe stochastisch gerechnet ist." — Version beim Anwender zu erfragen | Anwender (Upload gebündelt) | nächster Upload |
| Wiki | Abschnitt „Vergleich und Kalibrierung" der Seite Brauchwasser-Zapfprofil (Satz zur Spitzenstreuung) hochladen | Anwender (Upload gebündelt) | nächster Upload |
| Sicht | Sichtabnahme unter Windows: Reiter Kennzahlen mit stochastischer Jahresreihe und **einer** Zone — die Streuung steht als Zahl; mit zwei stochastischen Zonen steht ein Strich mit dem Vermerk „mehrere stochastische Zonen — Stichprobe nicht bildbar", dazu der Grund in der Warnliste | Anwender | nach dem Push |

### N19 (25.09.2026) — Anwenderentscheide ZU25–ZU29; Karte „Herkunft" und Größenschutz des Typtag-Paketlesers

**Anlass.** Die fünf Folgen des Sammelpostens N18 lagen beim Anwender. Er hat am 25.09.2026
entschieden (wörtlich: „1. … Empfehlung / 2. … Empfehlung / 5. Anzeigeort des Herkunftsprotokolls:
Anzeige ermöglichen (eigene karte im Ergebnisdialog) / 6. :Empfehlung"; zur dritten Frage die
Rückfrage „Ist Typtag für VDI 6007 erforderlich?"). Zwei der Entscheide sind in diesem Zug
umgesetzt, **ohne Schemaschritt**, die Testdatenbank unberührt: ein Agent mit `model: opus` im
Worktree `zh`, Zweig `zh` von `d2200ebb` (= `origin/ios_migration_september`, Schemastand 142,
Referenzbasis `2026-09-25_R16_Anlagenprio`); je Posten ein Commit, die Papiere im Folgecommit.

**Die Entscheide.**

| Nr. | Gegenstand | Entscheid | Stand |
|---|---|---|---|
| ZU25 (N18 (p)) | Konstruktorzeilen des Bedarfstags und der redundante Index auf `Tab_TwwMessreihe.ID_Projekt` | **ein** Schemaschritt für beide, **nach der Sichtabnahme Z1–Z5** | entschieden, noch nicht ausgeführt |
| ZU26 (N18 (r)) | Katalogdialog „Brauchwasser-Nutzungsarten" auf iOS | eigene Welle **nach iU11** | entschieden, offen |
| ZU27 (N18 (m)) | Referenzfall der Wetterkopplung mit `Tab_Solar.Bedeckungsgrad` | **nicht an ZU7 koppeln**, liegen lassen | zurückgestellt |
| ZU28 (N18 (b)) | Anzeigeort des Herkunftsprotokolls | **eigene Karte im Ergebnisdialog** | umgesetzt, Posten A unten |
| ZU29 | Größenschutz des Typtag-Paketlesers | Empfehlung angenommen | umgesetzt, Posten B unten |

**Zur Rückfrage bei ZU27: Typtage sind für VDI 6007 nicht erforderlich.** Der Typtag-Weg nach
VDI 4655 ist eine **wahlfreie Alternative des Jahresgangs allein für Brauchwasser** (4.2): Er
verteilt die Jahresenergie einer Zone über Typtage statt über Monats- und Wochenfaktoren. Das
Gebäudemodell nach VDI 6007 rechnet davon unabhängig aus Klimadaten, Bauteilen und Sollwerten; kein
Rechenweg der Gebäudesimulation liest eine Typtagzeile, und ohne eingespieltes Paket ist der Weg
benannt nicht verfügbar, während alles andere rechnet. Daher **keine Kopplung an ZU7** (Umstellung
eines Referenzprojekts auf den Generatorweg samt neuer Basis): Ein Referenzfall für die
Bewölkungsschwelle der Typtagzuordnung braucht einen Bedeckungsgrad aus einem TRY-Import, den die
Testdatenbank nicht führt, und käme sonst als dritte Änderung in denselben Einfrierschritt. Die
Schwelle bleibt bis dahin an erfundenen Werten geprüft — benannt, nicht still (N14 (c)).

**(a) Posten A — Karte „Herkunft" im Ergebnisbereich (ZU28) — erfüllt.** Das Herkunftsprotokoll
reist jetzt bis zur Maske; N18 (b) hatte es anzeigefähig gemacht, aber nicht angezeigt.

- **Hülle.** `ZapfprofilHuelle.Herkunftszeilen` baut aus `ZapfprofilErgebnis.Herkunft` und
  `Auslegungsergebnis.Herkunft` je Eintrag eine `ZapfprofilHerkunftZeile` der Oberflächensprache, in
  der Reihenfolge des Protokolls — sie ist die Reihenfolge, in der der Rechenweg die Werte festlegt,
  und wird nicht sortiert. Übersetzt werden der **Vermerk** (der `ZapfSatz` des Kerns, über denselben
  Weg wie jeder Hinweis), der **Stand** (`Wertstatus`: Vorgabe, überschrieben, kalibriert,
  umgerechnet) und die **Quelle** (`Herkunftsart` in Worten, dazu Regelwerk, Ausgabe und
  Katalogfassung als Daten). Ohne Provenienz steht „Eingabe des Anwenders", eine Zeile ohne Zone
  steht unter „Projekt", ein Eintrag ohne Wert trägt eine leere Wertspalte, und die Einheit „-"
  (dimensionslos) wird nicht angehängt. Die Spalte `Beleg` steht hier nie (Kapitel 6 (e)).
- **Oberfläche.** Unter der Warnliste des Ergebnisbereichs liegt eine **zugeklappte** Karte
  (`<details class="epos-zapfprofil-herkunft">`, Muster der Aufklapper der Speicherflotte) mit
  Untertitel und einer sechsspaltigen Tabelle: Größe, Wert, Zone, Stand, Quelle, Vermerk. Sie steht
  **ab Stufe Erweitert** — in der Stufe Einfach zeigt der Dialog nur das Notwendige, und das
  Protokoll trägt je Zone ein Dutzend Zeilen. Die Überlagerung „Auslegung" führt **keine eigene
  Stufe**, deshalb entscheidet dort die Hülle über das Kennzeichen
  `ZapfprofilAuslegungDaten.HerkunftSichtbar` (ab Erweitert wahr) und zeigt dieselbe Karte mit den
  Werten der Auslegung.
- **Leer heißt benannt „nichts zu vermerken"**, die Karte bleibt stehen: Eine weggelassene Karte
  wäre von einer fehlenden nicht zu unterscheiden.
- **Entscheidung im Posten: Die Spalte „Größe" trägt den Feldnamen des Protokolls als DATEN** —
  `Tagesbedarf`, `Zirkulation.Laufzeit`, `Auslegung.ErzeugerKw`, in beiden Sprachen derselbe, wie ein
  Zonen- oder Katalogname. Die Feldnamen sind Bezeichner des Rechenwegs, stehen ebenso in Kern,
  Tests und Referenzlauf, und **zwölf von zweiundvierzig leben als Zeichenketten außerhalb von
  `ZapfFeld`** (die Größen der Auslegung); eine Namenstafel wäre eine zweite Quelle der Wahrheit und
  bliebe unbewacht. Folge unten.
- **Keine neue KI-Feldkarte.** Die Karte trägt keine Eingabestelle; wie die Warnliste ist sie reine
  Auskunft, und `KiMaskenabdeckungWacheTests` behält seine Zahlen (59 für den Zapfprofildialog, 20
  für die Auslegung). Der `KiDialoge`-Katalog führt auch die Warnliste nicht.
- **30 Ressourcenschlüssel** in beiden Sprachen (`ZPG_HERKUNFT_…`, `ZPG_GRP_HERKUNFT`,
  `ZPG_AUS_HERKUNFT_…`), Designer nachgezogen; fünf CSS-Regeln in `epos-ui.css`. **Ergebnisneutral:**
  kein Rechenweg ist berührt, die Reihen bleiben Bit für Bit, wie sie waren.
- **Geprüft:** drei bunit-Fälle am Zapfprofildialog (Karte mit Einträgen samt Spaltenköpfen und
  Zeilenreihenfolge, Karte ohne Eintrag, englische Kultur), zwei an der Auslegung (mit und ohne
  Kennzeichen, leere Karte) und drei an der Hülle in `EPOS.Kern.Tests/ZapfprofilHuelleHerkunftTests`
  (Reihenfolge und Einheiten, beide Sprachen samt Dezimalzeichen, leeres und fehlendes Protokoll).
- **Wiki-Quelle** „Brauchwasser-Zapfprofil": ein Absatz im Abschnitt „Vorschau" mit dem Anker
  `herkunft` (35 Anker statt 34).

**(b) Posten B — Größenschutz des Typtag-Paketlesers (ZU29) — erfüllt.**
`TwwTyptagCtrl.PaketLesen` folgt jetzt dem Muster von `TwwNutzungsartCtrl.PaketLesen` (N18 (c)):
drei Grenzen als benannte Konstanten — `HOECHSTENS_EINTRAEGE` (200), `HOECHSTENS_BYTE_ENTPACKT`
(64 MB) und `HOECHSTENS_BYTE_JE_DATEI` (16 MB) —, die beiden Byte-Grenzen als **Parameter mit den
Konstanten als Vorgabe**, damit ein Test an Kilobyte messen kann statt an 64 MB.

- Im Archiv stehen Eintragszahl und entpackte Gesamtgröße im Zentralverzeichnis und werden geprüft,
  **bevor ein Byte entpackt wird** (`TYPTAGIMPORT_ZU_GROSS`), und **ein zweites Mal beim Lesen**
  (`EintragLesen` bis zur Grenze und ein Byte darüber, mit mitlaufender Summe; Muster
  `IfcLeser.Entpacken`). Die Summe bricht **an** der Grenze ab statt an erfundenen Längen
  überzulaufen. Die Grenze je Datei (`TYPTAGIMPORT_DATEI_ZU_GROSS`) gilt für Archiv, Ordner und
  Einzeldatei, weil der Leser jede Datei ganz im Speicher hält; `MengeZuGross` prüft Eintragszahl und
  Gesamtgröße auf den beiden Dateisystemwegen.
- Die Pfadprüfung (`Pfadsicher`) lehnt `..`, die Wurzel und das Laufwerksmuster `^[A-Za-z]:` benannt
  ab (`TYPTAGIMPORT_PFAD_UNZULAESSIG`); **ein Unterordner bleibt erlaubt** — ein ZIP aus einem Ordner
  trägt dessen Namen, der Leser nimmt ohnehin allein den Dateinamen, und nichts wird auf die Platte
  entpackt. Verzeichniseinträge fallen still, sie tragen keinen Inhalt.
- **Drei eigene Kennungen des Lesers** in beiden Sprachen (Designer nachgezogen), keine geteilten mit
  dem Katalogimport: Die Sätze nennen das Typtagpaket, und `ZapfSaetzeWacheTests` hält sie wie jede
  andere Kennung, ohne Änderung an der Wache.
- **Abweichung: der Einzeldateiweg bleibt, wie er war.** Beim Katalogimport nimmt er die gewählte
  Datei in den Satz auf, auch wenn ihr Name nicht auf `Tab_Tww*.csv` passt. Der Typtagleser sammelt
  `*.csv` des Ordners; eine gewählte Datei ohne `.csv` gehörte nie dazu, und sie aufzunehmen wäre eine
  Änderung des Verhaltens ohne Anlass.
- **Geprüft** in der neuen Klasse `EPOS.Kern.Tests/TwwTyptagPaketleserTests` (ohne Datenbank, sechs
  Fälle): der gerade Weg je Zweig, die drei Archivgrenzen, Unterordner und Verzeichniseintrag,
  Ordner- und Einzeldateiweg an kleinen Grenzen, die Sättigung der Summe, der
  `Archivluege`-Prüfstand (ein lügendes Zentralverzeichnis **kürzt** den Eintrag — der gekürzte
  fällt der Formprüfung des `Normformvektorleser` zu, nicht dem Größenschutz) und die Muster beider
  Sprachen. Vorher trug `TwwTyptagCtrlTests` keinen Fall zu `PaketLesen`.

**Folgen:**

| Folge | Was | Wer | Wann |
|---|---|---|---|
| ZU25 | Konstruktorzeilen des Bedarfstags (N13 (p)) und der redundante Index auf `Tab_TwwMessreihe.ID_Projekt` (N15 (a)) in **einem** Schemaschritt des Zapfprofils | Agent eines Folgepostens | **erledigt mit N21** (Schemaschritt 145) |
| ZU26 | Katalogdialog auf iOS (N13 (r)): Naht der Schale; bis dahin lehnt die Hülle ihn dort benannt ab | Agent einer iOS-Welle | **nach iU11** |
| ZU27 | Referenzfall der Wetterkopplung mit `Tab_Solar.Bedeckungsgrad` aus einem TRY-Import | Agent eines Folgepostens | **zurückgestellt**, bis ein Anwender den Typtag-Weg einsetzt |
| (s) | Katalogimport um Bedarfstage und Parameter erweitern — vorher Dublettenregel, Versionsbildung und Berichtszeilen je Tabelle festlegen (N18 (s)) | Agent eines Folgepostens, nach Festlegung | offen |
| Namen | Namenstafel der Größen des Herkunftsprotokolls in beiden Sprachen — sinnvoll erst, wenn alle zweiundvierzig Feldnamen Konstanten in `ZapfFeld` sind (zwölf sind heute Zeichenketten der Auslegung); dann eine Wache über Reflexion, die jede Größe gegen beide Sprachen hält | Agent eines Folgepostens, auf Zuruf | **erledigt mit N21** |
| Archiv | Der Größenschutz des **Katalogimports** hat keinen Archivfall mit kleiner Grenze (nur Ordner und Einzeldatei messen an Kilobyte) — beim nächsten Anlass nachtragen | Agent eines Folgepostens | **erledigt mit N21** |
| Logbuch | ein Satz: „Das Brauchwasser-Zapfprofil zeigt ab der Stufe Erweitert eine Karte ‚Herkunft', die je Wert der Rechnung nennt, woher er kommt." — Version beim Anwender zu erfragen | Anwender (Upload gebündelt) | nächster Upload |
| Wiki | Abschnitt „Vorschau" der Seite Brauchwasser-Zapfprofil (Absatz zur Karte „Herkunft", Anker `herkunft`) hochladen | Anwender (Upload gebündelt) | nächster Upload |
| Sicht | Sichtabnahme unter Windows: Stufe Erweitert, Karte „Herkunft" aufklappen — je Wert eine Zeile mit Stand und Quelle, in der Reihenfolge des Rechenwegs; in der Stufe Einfach steht keine Karte; die Auslegung zeigt dieselbe Karte | Anwender | nach dem Push |
### N20 (25.09.2026) — Katalogimport der Bedarfstage und Parameter (ZU30 bis ZU33)

**Anlass.** Der letzte Folgeposten aus N13 (s) und N18 (c): Der Anwender-Katalogimport nahm allein
die vier Dateien des Nutzungsartkatalogs an; für `Tab_TwwBedarfstag_STAMM`,
`Tab_TwwBedarfstagEreignis_STAMM` und `Tab_TwwParameter_STAMM` fehlten Dublettenregel, Prüfung und
Berichtszeilen. Ein Agent mit `model: opus` im Worktree `zi`, Zweig `zi` von `822ba803`, **ohne
Schemaschritt** (Testdatenbank unberührt, Schemastand 143); Referenzbasis
`2026-09-25_R16_Anlagenprio`. Protokoll:
[`2026-09-25_Katalogimport_Bedarfstage_Parameter.md`](../ueberholt/Protokolle/Zapfprofilgenerator/2026-09-25_Katalogimport_Bedarfstage_Parameter.md).

**Die vier Entscheide des Anwenders vom 25.09.2026 im Wortlaut:** „Bedarfstage: Was ist eine
Dublette: importierter Bedarfstag darf eine vorhandene Auslieferungszeile ersetzen. Hinweis geben. /
Parameter: Import ersetzt den Wert. Hinweis geben / Bericht: je Tabelle eigene Zeilen mit Ergebnis
und Grund / Prüfung: Empfehlung" — die Zeilen ZU30 bis ZU33 in Kapitel 9.

**(a) Drei wahlfreie Dateien im Paket.** `IMPORT_TABELLEN` führt sie in Einspielreihenfolge vor den
vier bekannten: Bedarfstag, seine Ereignisse, Parameter. Format wie im Werkzeugweg (Kopfzeile,
Trenner `;` oder `,`, Punkt als Dezimalzeichen, Provenienzspalten). Die `ID` eines Bedarfstags ist
**nur Schlüssel des Pakets** — die Ereignisse verweisen über `ID_Bedarfstag` darauf, die Datenbank
vergibt die echte; dieselbe Regel wie `ID_Tagesgangsatz`. Fehlt eine Tabelle im Schema, ist ihre
Datei benannt übergangen (`KATALOGIMPORT_BEDARFSTAGE_OHNE_TABELLE`,
`KATALOGIMPORT_PARAMETER_OHNE_TABELLE`); ein Paket mit der Spalte `Bezugsart` an einer Datenbank vor
Schritt 124 fällt nicht, seine Angabe bleibt benannt liegen
(`KATALOGIMPORT_BEZUGSART_OHNE_SPALTE`).

**(b) Ersetzen statt Versionsbildung (ZU30, ZU31).** Ein Bedarfstag ist über `Bezeichner` und
`Katalogversion` bestimmt, ein Parameter über `Schluessel` und `Katalogversion`. Gleicher Inhalt —
übersprungen (`KATALOGIMPORT_GLEICH_VORHANDEN`; verglichen werden beim Bedarfstag `Quelle_Art`,
`Bezugsmenge`, `Bezugsart` und die Ereignisse in ihrer Reihenfolge, beim Parameter Wert und Einheit,
die Provenienz jeweils nicht — dieselbe Gruppenregel wie bei den Nutzungsarten). Abweichender Inhalt
— die vorhandene Zeile trägt danach die Werte des Pakets, **am Platz und mit derselben `ID`**, damit
ein Projekt, das den Bedarfstag gewählt hat, weiter darauf zeigt; die Ereignisse werden vollständig
ersetzt. Die Zeile ist danach eine Anwenderzeile: `Status = 'IMPORT'`, `ReadOnly = 0`, ohne `Beleg`,
Herkunftsart `IMPORT` (`FREI` und `FIKTIV` bleiben, `ImportHerkunft`). **Auch eine Zeile der
Auslieferung** — das ist der Kern des Entscheids ZU30 und der einzige Ort des Katalogs, an dem der
Import eine vorhandene Zeile anfasst. Eine Version „(Import n)" gibt es hier nicht: Ein Parameter ist
ein Wert, keine Version.

**(c) Was mit der gelieferten Fassung geschieht — geprüft, nicht behauptet.** Der Hinweis
`KATALOGIMPORT_AUSLIEFERUNG_ERSETZT` nennt Zahl und Namen der ersetzten Auslieferungszeilen und sagt,
dass die gelieferte Fassung **nicht von selbst zurückkommt**. Zwei Messungen dahinter:
`Erstbereitstellung` kopiert die Vorlage mit `File.Copy(…, false)` — ein Programmupdate legt die
Datenbank des Anwenders nie neu an; und `Werkzeuge/Auslieferungsvorlage/TwwKataloge.Bereinigen`
**löscht** jede Zeile, die nicht `Status = 'AUSLIEFERUNG'` trägt — eine aus dieser Datenbank gebaute
Vorlage führt die ersetzte Zeile also gar nicht mehr. Die gelieferte Fassung braucht deshalb eine
neue Installation oder ein Katalogpaket, das sie führt. Für ersetzte Parameter nennt
`KATALOGIMPORT_PARAMETER_WIRKUNG` zusätzlich, dass sie für jede weitere Auslegung und jede
Validierung dieser Katalogversion gelten.

**(d) Die Liste der gelesenen Parameterschlüssel — neu, weil ZU31 sie braucht.** Ein Schlüssel, den
kein Rechenweg liest, wäre eine stille Zeile im Katalog. `TwwParameterkatalog`
(`EPOS.Kern/Allgemein/Zapfprofil/TwwParameterschluessel.cs`) führt **69 Einträge** mit Schlüssel,
Einheit und zulässigem Bereich, gespeist aus den Konstanten von `ZapfAuslegungParameter`,
`ZapfParameter` und `ZapfStochastikParameter` — kein zweites Literal. Vier Einträge sind **Vorsätze
einer Familie** (`DIN4708.Profil.Block.`, `Konstruktor.Regel.`,
`Speicherauslegung.Nenninhalt.Liste.`, `Zapfprofil.Stochastik.Quantil.P`); sie nehmen nur einen
Schlüssel MIT Rest an, und wo die Glieder verschiedene Einheiten tragen (ein Zapfblock führt Beginn
und Dauer in Minuten, den Anteil dimensionslos), prüft der Import keine Einheit. Die Wache
`EPOS.Kern.Tests/TwwParameterschluesselWacheTests` hält beide Seiten gleich und prüft zusätzlich,
dass jeder Parameter des freien Paketteils bekannt, in seiner Einheit und in seinem Bereich bleibt —
sonst lehnte der Import die eigene Auslieferung ab. **Die Bereiche sind Rahmen, keine Fachwerte:**
Sie fangen den Zahlendreher und die verrutschte Zehnerpotenz ab; den Fachwert setzt der Katalog.

**(e) Prüfung (ZU33, nach Empfehlung).** Bedarfstag: `Quelle_Art` aus {2, 3, 4, 5} — die 1 ist das
Stundenprofil der Zonen und entsteht im Lauf, nie in einem Katalog —, `Bezugsart` 1 bis 7 oder leer,
mindestens ein Ereignis, jedes Ereignis im Tag (`Minute_Beginn` 0 bis 1439, `Dauer_min` ab 1, Ende
höchstens 1440), `Energie_Kwh` nicht negativ und in der Summe positiv, `Reihenfolge` lückenlos ab 1.
Ein Fehler lehnt **nur diesen Bedarfstag** ab, wie bei den Nutzungsarten. Parameter: unbekannter
Schlüssel (`KATALOGIMPORT_PARAMETER_UNBEKANNT`), abweichende Einheit
(`KATALOGIMPORT_PARAMETER_EINHEIT`), Wert außerhalb des Bereichs (`KATALOGIMPORT_PARAMETER_BEREICH`).

- **Abweichung zur Auftragszeile „`Bezugsmenge` > 0": eine leere Bezugsmenge bleibt erlaubt.** Die
  Spalte ist im Schema `REAL` ohne `NOT NULL`, und der Ecodesign-Bedarfstag des freien Paketteils
  führt keine — ohne Bezugsmenge wird ein Tag nie skaliert, das ist eine Aussage und kein Mangel.
  Geprüft wird deshalb: leer **oder** positiv (`KATALOGIMPORT_BEDARFSTAG_BEZUGSMENGE`). Mit der
  strengen Lesart wäre der eigene Paketteil nicht mehr einspielbar.
- **Abweichung: Ein Ereignis ohne seinen Bedarfstag lehnt das PAKET ab, nicht eine Zeile.** Der
  Auftrag nennt „abgelehnt"; eine Berichtszeile hat ein Ereignis aber nicht — es gibt keinen
  Eintrag, dem es zufallen könnte. Ein solcher Verweis ist deshalb ein Formfehler
  (`KATALOGIMPORT_EREIGNIS_OHNE_TAG` mit Datei, Zeile und Paket-`ID`), und nichts ist geschrieben —
  dieselbe Stufe wie eine unbekannte Spalte. Ein Tagesgang ohne Satz bleibt dagegen ein Hinweis: Er
  trägt keine eigene Zeile im Katalog, ein Ereignis dagegen schon.

**(f) Bericht in Gruppen und Prüflauf (ZU32).** Jede Berichtszeile trägt ihre Tabelle
(`TwwImportbereich`), und der Bericht führt sie in der Reihenfolge Bedarfstage, Parameter,
Nutzungsarten. Der Katalogdialog zeigt je Tabelle eine eigene Gruppe mit eigener Überschrift und
eigenem Spaltenkopf (beide Sprachen); eine leere Gruppe steht nicht da. Die Zusammenfassung nennt
jetzt vier Zahlen (angelegt · ersetzt · übersprungen · abgelehnt). **Der Prüflauf**
(`Importieren(dateien, pruefen: true)`, im Dialog der Schalter „Nur prüfen, nichts schreiben")
rechnet denselben Bericht und rollt den Vorgang zurück; die Ausgänge lauten dann „würde anlegen",
„würde ersetzen", „würde überspringen", „würde ablehnen", der Katalog wird nicht neu geladen und die
Statuszeile bleibt leer. Der Schalter wählt die Betriebsart eines Knopfdrucks, wird mit der
Überlagerung zurückgesetzt und nie gespeichert — so steht er mit Vermerk in
`KiMaskenabdeckungWacheTests`.

**(g) Nachweis.** 17 Muster `ZPG_SATZ_KATALOGIMPORT_…` und zwölf Beschriftungen `ZPGK_IMPORT_…` in
beiden Sprachen; das Probepaket unter `EPOS.Kern.Tests/Proben/Zapfprofil/Katalogpaket/` führt drei
weitere Dateien mit erfundenen, runden Werten. Gates: Kern-Filter 0 Fehler, voller Testlauf 0 Fehler
(14 643 erfolgreich), Windows-Schale 0 Fehler, `SqlDialektPruefer` 0 Fundstellen,
Auslieferungsvorlage-Tests 36 erfolgreich, Referenzlauf der sechs CI-Projekte gegen
`2026-09-25_R16_Anlagenprio` **GESAMT: PASS**, Designer wiederholbar.

**Folgen.**

| Nr. | Gegenstand | Wer | Wann |
|---|---|---|---|
| Wiki | Seite „Brauchwasser-Zapfprofil" (Abschnitt Katalogimport) hochladen | Anwender (Upload gebündelt) | nächster Upload |
| Logbuch | ein Satz: „Der Katalogimport für Brauchwasser nimmt auch Bedarfstage und Parameter an und ersetzt vorhandene Werte mit Hinweis." — Version beim Anwender zu erfragen | Anwender | mit dem Upload |
| (a) | Bereiche der 69 Parametereinträge fachlich durchsehen — sie sind Rahmen gegen Zahlendreher, keine Fachgrenzen; eine engere Grenze gehört in dieselbe Liste | Anwender | mit ZU21 |
| Sicht | Sichtabnahme unter Windows: die drei Gruppen des Importberichts und der Schalter „Nur prüfen, nichts schreiben" | Anwender | nach dem Push |

---

### N21 (26.09.2026) — Anwenderentscheid ZU25: die Konstruktorzeilen in der Datenbank, der redundante Index weg, zwei Pflegereste

**Anlass.** Der Anwenderentscheid ZU25 (N19): die Zeilen des Bedarfstag-Konstruktors gehören in die
Datenbank, und der redundante Index auf `Tab_TwwMessreihe.ID_Projekt` (N15 (a)) gehört weg — **ein**
Schemaschritt für beides, weil beides reines DDL ist und ein eigener Schritt je Kleinigkeit eine
Nummer und einen Referenzlauf kostet. Dazu die zwei liegen gebliebenen Pflegereste aus N19: der
fehlende Archivfall des Katalogimports und die Namenstafel der Größen der Karte „Herkunft".
Protokoll
[Konstruktorzeilen](../ueberholt/Protokolle/Zapfprofilgenerator/2026-09-26_Konstruktorzeilen_Schemaschritt.md),
Statuszeile #522.

**(a) Schemaschritt 145 `SCHRITT_145_ZAPFPROFIL_KONSTRUKTOR` (T5 „Konstruktor").** Neu ist
`Tab_TwwKonstruktorzeile` (STRICT, zehn Spalten): `ID`, `ID_TwwProjekt` → `Tab_TwwProjekt(ID)` mit
`ON DELETE CASCADE`, `Reihenfolge` (≥ 1), `Beginn_h` und `Ende_h` (0 … 24), `Regel`, `Anzahl` (≥ 0),
`Volumen_l` (≥ 0), `Zapftemperatur_C`, `Verbraucher`; natürlicher Schlüssel
(`ID_TwwProjekt`, `Reihenfolge`). **Der Verweis geht auf den Auslegungssatz**, nicht auf das Projekt:
`Tab_TwwProjekt` trägt eine Zeile je Projekt und ist der Ort, an dem `ID_Bedarfstag` den
konstruierten Tag nennt; über diese Kette räumt auch `Tab_Projekt` mit ab. **Kein eigener Index auf
dem Verweis** — der UNIQUE-Index trägt die Spalte an führender Stelle, und ein zweiter wäre genau
die Redundanz, die derselbe Schritt bei `Tab_TwwMessreihe` wegnimmt (`DROP INDEX IF EXISTS`; die
Anweisungen von T4 bleiben, wie sie sind — eine ausgeführte Nummer wird nicht umgeschrieben,
ADR-001). Eine Quelle für Migration, Werkzeug und Nachweis:
`TwwSchema.SCHRITT_T5_KONSTRUKTOR` (= `NachtzeitSchema.SCHRITT` + 1), `AnweisungenT5Konstruktor`,
`AufraeumenT5Index`; `SchemaStand.Zielversion` verweist symbolisch darauf. **Ergebnisneutral:** Die
Tabelle entsteht leer, kein Rechenweg liest eine Konstruktorzeile, und ein Index ändert kein
Ergebnis.

**Abweichung von der Vorgabe des Auftrags, mit Grund.** Der Auftrag skizzierte Spalten
`Minute_Beginn` (0–1439), `Dauer_min` und `Energie_Kwh`/`Volumen_l` — das ist die Form des
**Ereignisses**, die `Tab_TwwBedarfstagEreignis_STAMM` schon führt. Gespeichert wird stattdessen,
**was der Konstruktor führt**: das Zeitfenster in Stunden und wahlweise eine Zapfregel des Katalogs
samt Anzahl ihrer Vorgänge oder ein Volumen samt Zapftemperatur. Nur so lässt sich der Konstruktor
wieder öffnen und eine Zeile ändern: Aus Minuten und Energien des gebauten Tages liesse sich weder
die Regel noch die Anzahl noch die Zapftemperatur zurückrechnen. Ebenso weggelassen ist der Index
auf dem Verweis (siehe oben).

**(b) Projektdaten.** Die Zeilen sind Bestandteil des Projekts: Projektkopie und `.wpx`-Paket tragen
sie (`ProjektDuplizierenCtrl`, `FK_MAP` und `KINDER` — ausdrücklich wie bei der Wohnungstabelle,
weil die Tabelle kein eigenes `ID_Projekt` führt), und die Auslieferungsvorlage leert sie samt
Prüfposten: Der konstruierte Bedarfstag selbst trägt `Status` `EIGEN` und fällt dort ohnehin — seine
Zeilen hätten danach niemanden mehr, den sie beschreiben.

**(c) Der Konstruktor speichert und lädt.** Der Schreibweg (`ZapfprofilCtrl.Speichern`) legt die
Zeilen des Arbeitsstands **ersetzend** am Auslegungssatz ab — erst weg, was steht, dann die Liste in
ihrer Reihenfolge —, und der zurückgegebene Stand trägt sie weiter. Der Leseweg
(`ZapfprofilCtrl.Lies`, neu `Konstruktorzeilen(int)`) gibt sie beim Öffnen wieder her; die Hülle
führt sie an den Konstruktor — **mit Entwurf und ohne**, denn ein gespeicherter Konstruktortag hat
keinen Entwurf mehr, und die Startzeilen der Überlagerung kommen jetzt aus
`Eingabe.Konstruktorzeilen`. Die Bedienung bleibt: Zeilen ändern und OK bauen einen neuen Tag (unter
neuem Namen, denn der alte ist im Katalog vergeben). Vor dem Schritt fehlt die Tabelle: Gegebene
Zeilen lehnt der Schreibweg benannt ab (`SPEICHER_TABELLE_FEHLT`), ohne Zeilen läuft das Speichern
durch wie zuvor. **Nicht mit geladen** werden Bezugsart und Bezugsmenge eines gespeicherten
Konstruktortags — sie stehen an seiner Katalogzeile; der wieder geöffnete Konstruktor beginnt dort
„ohne Bezug" (Folge unten).

**(d) Pflegerest Archivfall (N19, Folge „Archiv").** Der Größenschutz des Katalogimports war im
Archiv nur an 16 MiB gemessen, Ordner- und Einzeldateiweg dagegen an Kilobyte. Der neue Fall nimmt
die beiden Grenzen als Parameter von `TwwNutzungsartCtrl.PaketLesen` und prüft an einem erfundenen
Archiv mit zwei Einträgen je 60 Byte: ein Eintrag knapp über der Grenze je Datei fällt als
`KATALOGIMPORT_DATEI_ZU_GROSS` und nennt Namen, gemessene und erlaubte Größe; zwei ehrliche
Einträge, in der Summe knapp über der Gesamtgrenze, fallen als `KATALOGIMPORT_ZU_GROSS` — aus dem
Zentralverzeichnis, bevor ein Byte entpackt wird; unter beiden Grenzen läuft dasselbe Archiv
vollständig durch. Kein Quelltext des Kerns geändert, allein der fehlende Nachweis.

**(e) Pflegerest Namenstafel (N19, Folge „Namen").** Die Spalte „Größe" der Karte „Herkunft" trug den
Feldnamen des Kerns als Daten — in beiden Sprachen dasselbe Wort. Die zwölf Größen der Auslegung,
die als Zeichenketten im Rechenweg standen, sind jetzt Konstanten in `ZapfFeld`; damit sind es
zweiundvierzig. `ZapfFeld` baut daraus über Reflexion eine Namenstafel Größe →
`ZPG_GROESSE_<KONSTANTE>` — also ohne zweite Liste, die auseinanderlaufen könnte —, und die Hülle
setzt sie um (`ZapfprofilHuelle.Groessenname`), mit benanntem Rückfall auf den Feldnamen, wenn der
Kern den Namen nicht als Größe kennt. Zweiundvierzig Schlüssel in beiden Sprachen; die Wache
`ZapfprofilGroessennamenWacheTests` zählt die Namen aus dem Kern, verlangt je Größe einen Schlüssel
und einen Text in beiden Sprachen, die nicht dasselbe Wort sind, und hält fest, dass kein Vermerkweg
des Zapfprofil-Rechenwegs einen Größennamen mehr als Zeichenkette trägt.

**(f) Nachweis.** Testdatenbank aus der origin-Fassung migriert (Werkzeug `Testdatenbankschema`):
Schemastand **145**, STRICT-Tabellen **145**, Zellvergleich gegen die Vorfassung **genau zwei
Unterschiede** — der Marker `Tab_Applikation.SchemaVersion` 144 → 145 und die neue, leere Tabelle —,
keine Zeile berührt, das Prüfprojekt ohne Referenzrolle unverändert, `integrity_check` ok, zweiter
Lauf 0/0. Gates nach dem Merge von `origin` (die Posten #525 und #526): Kern-Filter 0 Fehler, voller
Testlauf 0 Fehler (**15 042 erfolgreich**, 2 übersprungen), Windows-Schale 0 Fehler,
`SqlDialektPruefer` 0 Fundstellen (1 946 Texte), Auslieferungsvorlage-Tests 36 erfolgreich,
Referenzlauf der sechs CI-Projekte gegen `2026-09-25_R19_BhkwNetzbezug` **6/6 PASS** (198 CSV,
2 208 587 Werte), Designer wiederholbar. Dieselben Gates waren vor dem Merge grün (14 990
erfolgreich).

**Folgen.**

| Nr. | Gegenstand | Wer | Wann |
|---|---|---|---|
| Wiki | Seite „Brauchwasser-Zapfprofil" (Absatz zum Konstruktor: die Zapfungen bleiben mit der Auslegung gespeichert) hochladen | Anwender (Upload gebündelt) | nächster Upload |
| Logbuch | ein Satz: „Die Zapfungen eines selbst konstruierten Bedarfstags bleiben mit der Auslegung gespeichert und lassen sich wieder bearbeiten." — Version beim Anwender zu erfragen | Anwender | mit dem Upload |
| (a) | Bezugsart und Bezugsmenge eines **gespeicherten** Konstruktortags in den wieder geöffneten Konstruktor laden (heute beginnt er dort „ohne Bezug"); sie stehen an der Katalogzeile des Tags | Agent eines Folgepostens | bei Gelegenheit |
| Sicht | Sichtabnahme unter Windows: Konstruktor füllen, OK, Dialog schließen, wieder öffnen — die Zeilen stehen; Karte „Herkunft" mit übersetzten Größen | Anwender | nach dem Push |

---

### N22 (26.09.2026) — Validierungswerkzeug der Stufe Z5 und erster Lauf an offenen Messreihen (K5)

*(N21 gehört zum parallelen Posten.)*

**Anlass.** Die Abnahme der Stufe Z5 ist ein Vergleich gegen Messungen (Kapitel 7 Zeile Z5); umgesetzt
wurde sie an erfundenen Reihen (N15). Echte Reihen dürfen nicht ins Repositorium (K5). Ein Werkzeug
löst genau diesen Widerspruch: Die Reihen liegen beim Anwender, das Werkzeug liegt im Repositorium,
und der Bericht trägt nur Verhältniszahlen. Der Anwender hat am 26.09.2026 dazu drei offen
lizenzierte Fremddatensätze freigegeben („Nehme Norwegen und Zenodo", danach Forbell) und eine vierte
Quelle nach Prüfung verworfen.

**(a) Das Werkzeug** `Werkzeuge/ZapfprofilValidierung` (Konsole, `net10.0`, eigene Projektmappe mit
Testprojekt, nicht in `WP-Plan.sln` und nicht im Kern-Filter — wie `Auslieferungsvorlage`,
`Gebaeudevergleich` und `Formularkarte`; eigener Schritt in `kern.yml`). Aufruf:

```
dotnet run --project Werkzeuge/ZapfprofilValidierung -c Release -- <ordner> --ziel <berichtordner>
       [--katalog <sqlite|paketordner>] [--realisierungen N] [--seed S] [--trocken] [--beispielreihe]
```

Je Messobjekt ein Unterordner mit `objekt.json` (anonyme Kennung, Nutzungsart-Bezeichner,
Bezugsmenge, Niveau, Bilanzgrenze des Zählers, Zirkulation, Temperaturen, Kalender mit Wochentag des
1. Januar und Feiertagen als Jahrestage, Messangaben, Stochastik) und `messreihe.csv` im Format des
`Messreihenleser`. Rückgabe: 0 abgenommen, 2 Aufruf, 3 Schreibort, 5 nicht abgenommen, 6 Fund der
Berichtswache, 1 unerwartet. Format, Felder und Grenzen stehen im
[`LIESMICH.md`](../../Werkzeuge/ZapfprofilValidierung/LIESMICH.md) des Werkzeugs.

**(b) Der Rechenweg ist datenbankfrei.** `ZapfprofilRechner.Rechnen` auf einem `Zapfprofileingang`
mit einer Zone, `Messvergleich`, `Messkalibrierung`, `Messreihenleser` — alles Bestand des Kerns. Der
Katalog kommt als **Modell** herein: `Nutzungsart`, `Tagesgangsatz`, `Parametersatz` und
`Zapfkategorie`, gebaut aus einem Paketordner im Format N2 (Kapitel 6 (b)) **oder** aus einer
`.sqlite`. Kein Controller, keine Projektkopie, kein `DataRepository`; eine SQLite-Quelle wird
`immutable=1` und `ReadOnly` geöffnet und bleibt byte-gleich. Die im Auftrag vorgesehene
Projektkopie war damit nicht nötig. Der **freie Paketteil allein genügt nicht** — er führt die
Kaltwasser-, Wohnen- und Zirkulationsparameter des Mengengerüsts nicht; Vorgabe der Katalogquelle ist
deshalb `Referenzlaeufe/Kenndaten_Test.sqlite`, und das Beispiel bringt einen vollständigen,
erfundenen Paketteil mit.

**(c) Zwei benannte Unterschiede zum Dialogweg**, beide mit Grund im Quelltext:

1. **Verglichen wird gegen die kalibrierte Reihe.** Die gerechnete Stundenleistung ist der
   Bezugsmenge proportional; bei einem Messobjekt ist die Bezugsmenge eine Schätzung, die Reihe nicht.
   Gegen die rohe Rechnung prüfte das Band der Dauerlinie die Schätzung statt der Gestalt. Nach der
   Kalibrierung (4.1) steht der Niveaufehler allein im **Kalibrierfaktor**. Der erste Lauf bestätigt
   die Wahl: Die Faktoren reichen von 0,16 bis 5,1.
2. **Die verglichene Reihe folgt der Bilanzgrenze des Zählers** aus `objekt.json`: an der Zapfstelle
   allein die Zapfung, mit Verteilung oder Speicher beide Teile — dieselbe Grenze, mit der die
   Kalibrierung ohnehin rechnet. `ZapfprofilHuelle.Vergleichsbericht` summiert stets beide; im Dialog
   ist das vertretbar, weil dort ein Projekt mit gepflegter Messwertgrenze steht.

**(d) Die Abnahmekriterien als Ampel.** Je Objekt **drei**: Band der Dauerlinie (Lage der Messspitze
im P85–P95-Band), Formabgleich des Tagesgangs gegen die Parameterschwelle, Jahresenergie nach der
Kalibrierung (relativ 1e-9). Die **√N-Skalierung ist das vierte Kriterium und steht im
Sammelbericht, nicht je Objekt**: Sie ist eine Aussage über Objekte **verschiedener Größe** — die
Spitze je Einheit fällt wie 1/√N (4.4). An einem Objekt ist `Spitzenverhältnis · √N` nur eine Zahl;
ein Band um 1 hieße zu behaupten, die Rechnung überschätze die Spitze jedes Objekts um genau den
Faktor √N, und schon ein Mehrfamilienhaus mit 48 Personen könnte das nicht erfüllen. Geprüft wird die
Steigung von ln(Spitzenverhältnis) über ln(N) gegen **−0,5 ± 0,25** (die Schranke ist eine numerische
Setzung des Werkzeugs, sie steht in jedem Bericht); unter drei Objekten mit verschiedenem N bleibt das
Kriterium **gelb**. Gelb heißt überall „nicht entschieden", nie „in Ordnung".

**(e) Die Berichtswache** hält jeden Berichtstext vor dem Schreiben in drei Schichten: keine Einheit
einer Menge oder Leistung; keine Kennzahl einer Messreihe (Jahresmenge, Energie, größter Wert,
Tagesmittel) in irgendeiner Schreibweise mit **mindestens sechs Ziffern**, geprüft mit
Ziffernrandprüfung; und als Bauform ein `Objektbefund`, der gar keinen absoluten Messwert führt. Die
Ziffernschranke ist gemessen, nicht angenommen: Ohne sie hielt die Wache zwei Berichte zurück, weil
eine Bandgrenze und eine Energieabweichung zufällig mit einer gerundeten Messgröße zusammenfielen.
Ein Fund bricht mit Rückgabe 6 ab, ohne eine Datei zu schreiben. **Der Kalibriervorschlag einer
Nichtwohn-Zone steht als Verhältnis** („Tagesbedarf je Einheit, Messung/Rechnung") und nicht als
Betrag — ein Tagesbedarf in kWh ist eine gemessene Menge und gehört in die Katalogkopie des
Anwenders, nicht in einen Bericht. Einen Objektnamen kann die Wache nicht erkennen; die
Anonymisierung bleibt Sache des Anwenders.

**(f) Ablage.** `Referenzlaeufe/Messreihen_INEKON/` mit `LIESMICH.md`, in `.gitignore` bis auf dieses
ausgenommen — Bauform und Begründung wie `Referenzlaeufe/Normzahlen/`;
`RepositoryOrdnungWacheTests` prüft die Regel und die Gegenprobe. Offen lizenzierte Fremddaten
gehören in einen Ordner **außerhalb** des Repositoriums.

**(g) Das Beispiel** (`Beispiel/`): erfundener Katalog (fünf CSV-Dateien, runde Werte, Herkunftsart
`FIKTIV`) und zwei Objekte — ein Wohnobjekt und eine Nichtwohn-Zone, damit der Kalibriervorschlag
vorkommt. Die Messreihen stammen aus derselben Rechnung: deterministische Jahresreihe mal festem
Rauschen (± 15 %, feste Saat), **gleiche Jahresenergie**, Spitze auf die **Bandmitte gekappt** und
die abgeschnittene Energie wertgewichtet verteilt (`--beispielreihe`, wiederholbar). Gekappt statt
gestreckt, weil gegen die kalibrierte Reihe verglichen wird: Ein Streckfaktor verschwindet dort
wieder; was die Spitze senkt, ist eine flachere Gestalt bei gleicher Energie — genau die Lehre, an der
das Band hängt (3.6). Beide Objekte sind grün **durch Konstruktion**; das Beispiel prüft den Weg des
Werkzeugs, nicht den Rechenweg. 30 Proben, darunter Teiljahr, Volumenreihe mit Spreizung, Ensemble,
Nichtwohn-Vorschlag, Trockenlauf, Aufruffehler, Katalogleser, die Berichtswache samt Gegenproben und
das vierte Kriterium; der Fall gegen `Referenzlaeufe/Messreihen_INEKON/` schweigt ohne Objekte.

**(h) Drei Konverter** unter `Werkzeuge/ZapfprofilValidierung/Konverter/` setzen offen lizenzierte
Fremddaten (je CC BY 4.0, Namensnennung in jedem Bericht) in das Format des Lesers um — ohne
Zusatzpaket, auch die Excel-Quelle wird mit `zipfile` und `xml.etree` gelesen. Vier gemeinsame Regeln:
ein Kalenderjahr; ein zusammenhängendes Fenster mit höchstens 5 % Lücken und mindestens 30 Tagen,
sonst **benannt übergangen**; negative Werte auf 0 und gezählt; Bezugsmengen als **Platzhalter**,
solange sie nicht belegt sind, und als solche im Vermerk. Eine vierte Quelle (Flexitility) wurde
geprüft und **nicht verwendet** — sie führt den Gesamt-Trinkwasserdurchfluss am Hausanschluss, kein
getrenntes Warmwasser.

**(i) Der erste Lauf** über 21 Objekte (stochastisch, zehn Realisierungen, Katalog der Testdatenbank)
steht mit Zahlen, Ursachen und sechs Folgen in
[`Zapfprofilgenerator/2026-09-26_Validierung_offene_Messreihen.md`](Zapfprofilgenerator/2026-09-26_Validierung_offene_Messreihen.md).
Der Befund in zwei Sätzen: Die **Energie stimmt nach der Kalibrierung bei allen 21 Objekten exakt**,
und die **√N-Skalierung ist mit einer Steigung von −0,29 bestätigt**. **Band und Formabgleich sind an
keinem Objekt erfüllt** — mit benannten Ursachen: Platzhalter-Bezugsmengen, unbekannte Feiertage, eine
Zone je Objekt, aus VDI 6002 abgeleitete Klassenmittel als Tagesgänge, und für kleine Einheitenzahlen
ein Bandkriterium, das dort keine brauchbare Messlatte ist (bei drei Personen trägt eine einzelne
gezogene Gleichzeitigkeit die Jahresspitze, und P85/P95 der Dauerlinie liegen bei 0,04 bis 0,09 des
Maximums).

**Folgen.**

| Nr. | Gegenstand | Wer | Wann |
|---|---|---|---|
| K5 | Validierung an **eigenen, freigegebenen** Objekten — der eigentliche Nachweis der Stufe. Werkzeug und Weg stehen jetzt; gebraucht werden zwei bis drei Mehrfamilienhäuser und ein Nichtwohnobjekt, je ein Messjahr, CSV-Stundenwerte, anonymisiert | Anwender | nach der Freigabe |
| V1 | Bezugsmengen und **Bezugsarten** der offenen Datensätze nachtragen (für die norwegischen Wohngebäude Personen statt Wohnungen); danach trägt das vierte Kriterium belastbar | Folgeposten | nach Bedarf |
| V2 | Feiertage je Land und Jahr in die `objekt.json` eintragen, Formabgleich neu lesen | Folgeposten | nach Bedarf |
| V3 | **Das Bandkriterium für kleine Einheitenzahlen überarbeiten** — Quantil der Ensemblespitzen statt Quantil der Dauerlinie, oder eine Untergrenze für N, unter der es gelb bleibt. Berührt `Messvergleich` im Kern | Anwenderentscheid, dann Kern | offen |
| V4 | Die Bandgrenzen prüfen (`Zapfprofil.Validierung.Band.Unten`/`.Oben`, heute 0,85 / 0,95): Bei den großen Objekten lag die Messspitze durchweg knapp darüber | Anwenderentscheid | mit ZU21 |
| V5 | Die Formschwelle prüfen (`Zapfprofil.Validierung.Formschwelle`, heute 0,01): an erfundenen Reihen gesetzt, gemessene Objekte liegen bei 0,01 bis 0,04 | Anwenderentscheid | mit ZU21 |
| ZU7 | Referenzprojekt auf den Generator umstellen, vierte Einfrierregel, Basis neu einfrieren — unverändert **nach** der Sichtabnahme Z1–Z5 und **nach** K5 | Folgeposten | offen |

### N23 (26.09.2026) — ZU26 umgesetzt: der Katalog der Brauchwasser-Nutzungsarten auf iOS

**Anlass.** ZU26 war am 25.09.2026 als eigene Welle **nach iU11** entschieden (N19). Der Posten ist
als #524 vorgezogen; iU11 selbst ist nicht begonnen. Umgesetzt ist allein dieser Katalog — die acht
übrigen Katalogverwaltungen lehnt die Wurzel auf iOS weiter benannt ab (KI-D-Q10). Kein
Schemaschritt, Testdatenbank unberührt, Windows-Weg unverändert.

**(a) Die Naht nach dem Muster der Baustoffe.** `IProjektQuelle.NutzungsartKatalogGaben()` mit
Standardumsetzung `null`; `IosProjektQuelle` liefert denselben Parametersatz wie die Windows-Schale
(`ZapfprofilHuelle.KatalogGaben`, `EPOS.UI.Daten`). In der `AppWurzel`: Schlüssel
`Seitenschluessel.BrauchwasserNutzungsarten` in der Positivliste von `OeffneMaske`, Weiche in
`Zeige()`, eigener Render-Zweig mit `TwwNutzungsartAdminDialog`, Schließen über `ZurueckZurListe`.
Ohne Parametersatz bleibt der Katalog zu und sagt es (`ZPGK_KEINE_ANSICHT`, beide Sprachen). **Kein
Delegat des Hauptfensters** — anders als bei den zwei Katalogen der Gebäudesimulation: Unter Windows
fängt `WinFormsNavigation` den Maskenschlüssel ab und zeigt dieselbe Komponente im Fenster
`TwwNutzungsartAdminHuelle`; die Wurzel kommt dort nicht an die Reihe.

**(b) Der Einstieg auf iOS.** Die iOS-Schale hat kein Menü (Entscheid E-1: eine Wurzel, zwei
Schalen). Der Katalog geht dort über den **Hilfe-Assistenten** auf: KI-Knopf neben jedem Infoknopf,
`dialog_oeffnen` mit `KiMaskenziele` (`BRAUCHWASSER_NUTZUNGSARTEN`, `TWW_NUTZUNGSART_EDITOR`,
`BRAUCHWASSER_TYPTAGE`, `BRAUCHWASSER_MESSREIHEN` → `Masken.BrauchwasserNutzungsarten`),
`IosNavigation` → `AppWurzel.OeffneMaske` — derselbe Weg wie für Baustoffe und Bauteilaufbauten.
Unter Windows bleibt der Menüweg (Administration → Wärmebedarf & Heizung → Brauchwasser). „Beenden"
führt auf dem iPad zur Startansicht (Projektliste), ohne Rückwegstapel — wie bei den Katalogen der
Gebäudesimulation.

**(c) ZIP statt Ordner.** `IDateiDienst.OrdnerwahlMoeglich` (Standardumsetzung `true`, damit
vorhandene Fassungen nicht brechen; `IosDateiDienst` = `false`) ist eine Fähigkeitsfrage, kein
Probeaufruf: Die Hülle legt die Antwort als `OrdnerwahlVerfuegbar` in den Parametersatz, und der
Dialog beschriftet danach seinen Knopf („ZIP-Paket wählen…", `ZPGK_IMPORT_DATEI_ZIP`), setzt den
Filter `(*.zip)` (`ZPGK_IMPORT_DATEIFILTER_ZIP`) und nennt die Einschränkung in einem Satz vor dem
Knopf (`ZPGK_IMPORT_NUR_ZIP`). Er fragt nie nach einem Paketordner, den es auf dem Gerät nicht gibt.
Der Dokumentenwähler bekommt für `.zip` die Kennung `public.zip-archive` (`Dateifilter`, Bestand).

**(d) Der Prüfmodus.** `EPOS_PRUEFLAUF_KATALOGIMPORT` hängt hinter den Rechennachweis die
Katalogprobe (`EPOS.iOS/Pruefung/Katalogprobe.cs`): Das erfundene Probepaket
(`EPOS.Kern.Tests/Proben/Zapfprofil/Katalogpaket/`, sieben CSV, rund 3 KB) liegt als `MauiAsset`
mit Präfix `katalogprobe_` in jedem App-Paket; die Schale packt es zur Laufzeit zu **einem**
ZIP-Archiv, dem Weg des Anwenders. Gelesen, geprüft, eingespielt und gezählt wird im Kern
(`TwwKatalogprobe.Probelauf`, Zeilen `KATALOGPROBE`: Dateifilter samt `ordnerwahl=NEIN`, Paket,
Prüflauf, Import, Vergleich, Schlusszeile). `ios.yml` setzt den Schalter im Hauptlauf und wertet
die Zeilen im Schritt „Katalogprobe auswerten" aus; rot wird er bei abgelehntem Paket, abgelehnten
Einträgen, verschiedener Zählung von Prüflauf und Import oder fehlender Schlusszeile.

**(e) Tests und KI-Sicht.** `AppWurzelTests` (Öffnen über die Quelle und Schließen; Ablehnung ohne
Parametersatz; elf Ablehnungstexte verschieden), `TwwNutzungsartAdminDialogTests` (Knopf, Filter und
Satz je Ordnerwahl), `ZapfprofilHuelleKatalogdialogTests`, `TwwKatalogprobeTests` (ZIP-Probepaket
ohne Befund, Probe auf der Testdatenbank, Ordnerwahl im Parametersatz, Standardumsetzung, fehlendes
Paket als benannter Befund). Der Dialog bekommt kein neues Eingabefeld; die KI-Sicht und
`KiMaskenabdeckungWacheTests` bleiben unverändert.

**(f) Abweichungen.** (1) Der Rückweg ist die Startansicht, nicht die Ansicht, aus der der Katalog
geöffnet wurde; der Wiki-Satz des Postens hatte Letzteres behauptet und ist berichtigt. (2) Die
Probedateien liegen **ohne** Bauschalter in jedem Paket — anders als die Importproben
(`-p:Importproben=true`): Sie sind so klein, dass ein eigener Bauweg mehr kostet als sie selbst; die
Probe läuft trotzdem nur auf Zuruf. (3) Die Probe verändert den Katalog der Simulator-Datenbank; das
ist unbedenklich, weil der Rechennachweis vorher gerechnet hat und die Datenbank mit dem Lauf endet.
(4) Die iOS-Schale ist auf Windows nicht baubar; ihre Dateien sind gelesen und gegen die
Schnittstellen gehalten, übersetzt und bewiesen werden sie erst im iOS-Lauf.

**Folgen.**

| Nr. | Gegenstand | Wer | Wann |
|---|---|---|---|
| ZU26 | iOS-Lauf (`ios.yml`, zählt zehnfach) als Nachweis der Übersetzung und der Katalogprobe | Orchestrator nach Freigabe des Anwenders | nach dem Push |
| ZU34 | Einstieg auf dem iPad ohne Hilfe-Assistenten: Ist der Assistent abgeschaltet oder ohne Schlüssel, geht der Katalog dort nicht auf (dieselbe Lage wie bei Baustoffen und Bauteilaufbauten). Empfehlung: mit iU11 einen Einstieg für die Kataloge schaffen, nicht einzeln für diesen | Anwenderentscheid | mit iU11 |
| iU11 | die übrigen Katalogverwaltungen, Importe und Feinschliff nach iF2 | iOS-Welle | offen |

### N24 (26.09.2026) — ZU7 umgesetzt: Projekt 1045 als Referenzprojekt auf dem Generator, Basis R20

**Anlass.** ZU7 war am 25.09.2026 terminiert worden — Umstellung, Einfrierregel und neue Basis erst
nach der Sichtabnahme Z1–Z5 und nach K5 (Nachtrag N16). Die Sichtabnahme liegt vor, das
Validierungswerkzeug der Stufe Z5 steht (N22); K5 selbst (die Freigabe eigener Messreihen) bleibt
offen, ist aber keine Voraussetzung mehr — der Generator rechnet bereits gegen offen lizenzierte
Fremddaten (N22) und in den Kern-Tests gegen eine Projektkopie. Umgesetzt von einem Folgeposten-Agenten
(Zweig `z7`, Worktree `.claude/worktrees/z7`, Sonnet 5; der ursprüngliche Opus-Agent war durch ein
Nutzungslimit abgebrochen, der Stand wurde übernommen und fortgesetzt).

**(a) Die Projektwahl.** Von den sechs CI-Projekten (1030, 1007, 1017, 1045, 1046, 1047) trägt allein
1045 „Prüfprojekt Ost/West Stränge" ein echtes, unkompliziertes Warmwasserprofil: ein Gebäude
(10651 „EFH-A-U-347s", Einfamilienhaus, VDI-6007-Weg), nicht gekoppelt, keine Kühlung, Bestandsprofil
„EFH Wohnen, 1 Person" mit 5,0 MWh/a, eigenem Brauchwasser-Puffer, Wärmepumpe und Kessel — der
Generator erreicht damit Speicher und Erzeuger. Nicht 1030 (kein Gebäude), nicht 1017 (Kälte), nicht
1046 (Flottenstand eingefroren, SP‑O‑8), nicht 1047 (Anlagenkopplung AK1), nicht 1007 (sein
Bestandsprofil „Haushalt-3" ist ein Stromprofil, das als Brauchwasser läuft — kein Vergleichsmaß für
eine Warmwasserbilanz).

**(b) Die Saat.** [`Referenzlaeufe/Skripte/referenzprojekt_zapfprofil.py`](../../Referenzlaeufe/Skripte/referenzprojekt_zapfprofil.py)
setzt genau zwei Zeilen: `Tab_TwwProjekt` (`Weg = GENERATOR`, Seed 1045, 10 Realisierungen, P99,
Zirkulationsmethode Flächenkennwert, Speicherart 1, Personen-Automatik, alle übrigen Spalten NULL)
und `Tab_TwwZone` (Nutzungsart „Wohnen groß (abgeleitet)" am Gebäude 10651, Bilanzgrenze Zapfstelle
wie der Bestandsweg, 8,3 Personen — trifft den Bestandsweg-Jahresbedarf von 5,0 MWh/a ungefähr über
das mittlere Niveau umgerechnet auf das Kaltwasser-Jahresmittel der Parameter). Keine Wohnungstypen,
kein Konstruktor, keine Messreihe; die Bestandszeile in `Z_Projekt_Brauchwasser` bleibt stehen, rechnet
auf dem Generatorweg aber nicht mit (Weiche, 2.2). Wiederholbar (zweiter Lauf 0/0, `integrity_check`
ok, `foreign_key_check` leer); Zellvergleich gegen die Vorfassung: nur die zwei neuen Zeilen plus zwei
Zähler in `sqlite_sequence`, Schema unverändert.

**(c) Die siebte Einfrierregel „gesäte Zapfprofil-Eingaben".** `CLAUDE.md` und
[`Referenzlaeufe/LIESMICH.md`](../../Referenzlaeufe/LIESMICH.md) nennen sie jetzt: Änderungen an
`Tab_TwwProjekt`/`Tab_TwwZone` eines Referenzprojekts, den benutzten `Tab_Tww*_STAMM`-Zeilen samt
Tagesgangsatz und Kaltwasser-Parametern sowie das Umstellen eines Referenzprojekts auf den Generator
oder zurück erzwingen ein Neueinfrieren. Gehalten von
`EPOS.Kern.Tests/ZapfprofilReferenzprojektWacheTests`: jede gesäte Zelle der Projektzeile und der
Zone, die Kennzeichen der benutzten Nutzungsart samt Tagesgangsatz, die drei Kaltwasser-Parameter und
die Generator-Bilanz von 1045 — Jahresenergie auf 1e‑6 genau und die Stundenreihe Zeichen für Zeichen
gegen `waermebedarf_brauchwasser.csv` der Basis. `ZapfprofilCtrlTests` und `ZapfprofilWeicheTests`
sind angepasst: die Aussage „kein Referenzprojekt auf dem Generator" gilt jetzt für die übrigen
dreizehn. `ZapfprofilSpeichernTests.Zeilen()` zählte `Tab_TwwZone`/`-Wohnungstyp`/`-Projekt` global und
nahm eine leere Datenbank an; mit der gesäten Zeile von 1045 zählt die Hilfsfunktion jetzt ohne dessen
Zeilen.

**(d) Die neue Basis R20.** [`Referenzlaeufe/2026-09-26_R20_Zapfprofil`](../../Referenzlaeufe/2026-09-26_R20_Zapfprofil/)
löst R19 ab: **A/B gegen R19** 13/14 Projekte PASS und byte-gleich (416/432 CSV), allein 1045 FAIL mit
16 von 32 Dateien (91 713 Abweichungen von 324 299 Werten) — Jahresbrauchwasser 5,00 → 5,01 MWh/a
(5 000,00 → 5 006,62138 kWh/a), Gesamtwärme 80,94 → 80,95 MWh/a; das andere Stundenprofil verschiebt
zugleich die stundenweise PV-Eigenverbrauchszuordnung von 1045 mit (die theoretische PV-Erzeugung
bleibt unverändert). Determinismus mehrfach geprüft (byte-gleich), auch nach dem Merge von
`origin/ios_migration_september` (Schemastand 148, G6b/G7a — ohne Rechenwirkung auf die vierzehn
Projekte; die Saat wurde auf der gemergten Fassung wiederholt). R19 ist mit dieser Einfrierung aus dem
Arbeitsbaum gefallen (Protokoll archiviert, samt der eigenen A/B-Tafel gegen R18 als „Basis R19 im
Einzelnen" in `Dokumentation/ueberholt/Referenzbasen/LIESMICH.md`); `.github/workflows/kern.yml` und
`ios.yml` zeigen jetzt auf R20. Einzelheiten und Herleitung:
[`Referenzlaeufe/LIESMICH.md`](../../Referenzlaeufe/LIESMICH.md).

**Abweichungen.** (1) Der ursprüngliche Opus-Agent des Postens ist durch ein Nutzungslimit
abgebrochen; ein Nachfolger hat den committeten Stand geprüft und fortgesetzt, ohne ihn zu verwerfen.
(2) Der Merge von `origin` brachte umfangreiche fremde Wellen (G6b/G7a: Zonenkopplung, gbXML-Export,
Baualtersklassen) mit Schemastand 145 → 148 mit sich, die vor der Neueinfrierung eingearbeitet wurden
(„früh mergen" statt am Schluss) — ohne Rechenwirkung auf die Referenzprojekte, aber mit einer neuen
Testdatenbank-Fassung, gegen die die Saat wiederholt wurde. (3) Das Saatskript ließ beim ersten Lauf
`Kenndaten_Test.sqlite-shm`/`-wal` liegen (WAL nicht checkpointet); das Skript checkpointet jetzt vor
dem Schließen.

**Folgen.**

| Nr. | Gegenstand | Wer | Wann |
|---|---|---|---|
| ZU7 | keine — umgesetzt | — | erledigt |

### N25 (26.09.2026) — ZU21 entschieden: Abschnitte 1 und 2 der Prüfliste bestätigt

**Wortlaut** (Anwender, 26.09.2026): „Abschnitt 1,2: entschieden", ergänzt um „Abschnitt 1: Ecodesign -
erweitere Profil". **Lesart:** alle Setzungen der Abschnitte 1 (34 Zeilen des freien Paketteils) und 2
(17 numerische Setzungen im Rechenkern) der
[Prüfliste ZU21](Zapfprofilgenerator/2026-09-25_Pruefliste_ZU21_Setzungen.md) sind bestätigt, wie sie
stehen — mit einer Ausnahme: die drei Ecodesign-Zeilen des Abschnitts 1 (Dauer der 24
Ecodesign-Zapfungen, Ecodesign-Profil L Bezug, Auswahl der Ecodesign-Profile) sind nicht bestätigt,
sondern erweitert — alle Zapfprofile der Verordnung (EU) Nr. 814/2013 (XXS bis 4XL) werden
aufgenommen, Rechenregel und Bezug bleiben unverändert, als eigener Folgeposten.

**Freigegeben** ist damit der freie Paketteil `Referenzlaeufe/Katalogpaket_frei/` samt
Nichtwohn-Vorgabesatz (Kurz- und Duschzapfung, Gruppenregel und Bindung des Vorgabesatzes) und den
Validierungsparametern (Band, Formschwelle, Lückenanteil, Kalibrierung) für die Auslieferung — mit den
bestätigten Werten, ohne die drei Ecodesign-Zeilen, die dem Folgeposten harren.

**Offen bleibt** Abschnitt 3 der Prüfliste („Setzungen ohne belegten Auslieferungswert": die
Rückfrage-/Warnschwellen des Messvergleichs, die Setzungen der Speicherauslegung, „Ecodesign L nach
Wohneinheiten skalieren") und Abschnitt 4 („Was diese Liste nicht enthält" — Normzahlen, Hersteller-
und Produktwerte, Messobjektdaten); beide sind von diesem Entscheid nicht berührt.

**Folge:** keine Codeänderung — der Entscheid ist reine Papierarbeit (Prüfliste, Kapitel 9, dieser
Nachtrag). Die Ecodesign-Erweiterung läuft als eigener Folgeposten. Ein späterer Einzelentscheid zu
`Zapfprofil.Validierung.Band.*` kann aus der Folge V3 des
[Validierungslaufs](Zapfprofilgenerator/2026-09-26_Validierung_offene_Messreihen.md) entstehen
(Bandkriterium für kleine Anlagen); Änderungen an einzelnen Werten der Prüfliste sind dann je ein
eigener Anwenderentscheid.

**Folgen.**

| Nr. | Gegenstand | Wer | Wann |
|---|---|---|---|
| — | Ecodesign-Profile XXS bis 4XL der Verordnung (EU) Nr. 814/2013 aufnehmen, Rechenregel und Bezug unverändert | Folgeposten | offen |
| V3 | Bandkriterium für kleine Anlagen überarbeiten, kann `Zapfprofil.Validierung.Band.*` neu entscheiden | Anwenderentscheid, dann Kern | offen |

### N26 (26.09.2026) — Ecodesign erweitert: alle neun Zapfprofile der Verordnung im Paketteil

**Wortlaut** (Anwender, 26.09.2026): „Abschnitt 1: Ecodesign - erweitere Profil" (N25). **Umsetzung:**
der freie Paketteil `Referenzlaeufe/Katalogpaket_frei/` führt jetzt alle Lastprofile der Tabelle 1
der Verordnung (EU) Nr. 814/2013 der Kommission, Anhang III, außer 3XS: XXS, XS, S, M, L, XL, XXL,
3XL, 4XL (ABl. L 239 vom 6.9.2013, S. 162) — EU-Recht, keine Normzahl. Quelle des Klartexts:
konsolidierte Fassung der Verordnung, Anhang III Tabelle 1 (abgerufen über
`https://www.legislation.gov.uk/eur/2013/814/annexes/data.xht`), Q_ref gegengeprüft an derselben
Tabelle.

**Verfahren.** `Referenzlaeufe/Skripte/ecodesign_profile_bauen.py` erzeugt
`Tab_TwwBedarfstag_STAMM.csv` und `Tab_TwwBedarfstagEreignis_STAMM.csv` des Paketteils aus der
Rohtabelle `Referenzlaeufe/Skripte/ecodesign_profile_814_2013.json`; die Rechenregel der Dauer
(Setzung der Umsetzung, unverändert seit Profil L: Dauer = Volumen / Volumenstrom, Volumen =
Q_tap · 1000 / (c_w · (Nutztemperatur − 10 °C)), Nutztemperatur = Spitzentemperatur, wo angegeben,
sonst Mindesttemperatur, c_w = 1,163 Wh/(l·K); ganze Minuten kaufmännisch, mindestens 1) und der
Bezug (Bezugsart 2, ohne Bezugsmenge) gelten unverändert für alle neun Profile. Profil L behält die
ID 1 und seine 24 Ereignisse; das Skript prüft bei jedem Lauf, dass sie byte-genau dem bisherigen
Bestand gleichen (Kontrolle des Verfahrens) — bestätigt.

**Prüfsummen** (Summe der Zapfungen Q_tap gegen Q_ref der Verordnung, Toleranz relativ 1e-9): XXS
20 Zapfungen/2,100 kWh, XS 3/2,100 kWh, S 11/2,100 kWh, M 23/5,845 kWh, L 24/11,655 kWh (unverändert),
XL 30/19,07 kWh, XXL 30/24,53 kWh, 3XL 10/46,76 kWh, 4XL 10/93,52 kWh — alle neun treffen ihr Q_ref
genau, kein Übertragungsfehler. Bedarfstage insgesamt 12 (3 fiktiv, 9 Ecodesign), Ereignisse
insgesamt 170 (9 fiktiv, 161 Ecodesign).

**Abweichungen.** Keine: die Verordnung führt für kein Profil zwei Zapfungen zur gleichen Minute
(auch nicht bei 3XL/4XL, wo hohe Volumenströme das vermuten ließen); jede Dauer liegt zwischen
1 und 10 Minuten wie bei Profil L.

**Folge:** `EcodesignTests` prüft jetzt alle neun Profile (Theorie über Bezeichner, Anzahl der
Ereignisse und Q_ref); `TwwKatalogWacheTests.Jede_Tww_Katalogzeile_ist_EIGEN_mit_zugelassener_Herkunft`
zählt neun statt eine Ecodesign-Zeile. Die Testdatenbank ist aus dem geänderten Paketteil neu gesät
(`tww_testkatalog_fiktiv.py`, zweiter Lauf ohne Änderung); der Referenzlauf der sechs CI-Projekte
bleibt ergebnisneutral (kein Referenzprojekt benutzt ein Ecodesign-Profil). Prüfliste ZU21, Zeile
„Auswahl der Ecodesign-Profile": heutiger Wert auf „alle neun Profile (XXS bis 4XL)" fortgeschrieben.
Statuszeile #537, Protokoll
[`2026-09-26_Ecodesign_Profile.md`](../ueberholt/Protokolle/Zapfprofilgenerator/2026-09-26_Ecodesign_Profile.md).

**Folgen.**

| Nr. | Gegenstand | Wer | Wann |
|---|---|---|---|
| — | „Ecodesign L nach Wohneinheiten skalieren" bleibt offener Fachentscheid (Prüfliste ZU21, Abschnitt 3); gilt unverändert für alle neun Profile | Anwenderentscheid | offen |

---

### N27 (26.09.2026) — Zweiter Validierungslauf an offenen Messreihen: Folgen V1 bis V5 (K5)

**Anlass.** Anwenderauftrag „führe aus: Validierung an echten Daten" (26.09.2026): die Folgen V1 bis
V5 des ersten Laufs (N22) abarbeiten und einen zweiten Lauf fahren. Zahlen, Vergleich zum ersten Lauf
und Empfehlung stehen als Abschnitt 7 „Zweiter Lauf" im
[Validierungsbericht](Zapfprofilgenerator/2026-09-26_Validierung_offene_Messreihen.md). Kein
Schemaschritt, Testdatenbank unberührt, kein Parameter geändert.

**(a) Bezugsmengen (V1).** Belegt oder abgeleitet statt Platzhalter: Norwegen aus Tabelle 1 der
Beschreibung (Data in Brief 2021), Wohngebäude in Personen mit einer benannten Belegung nach der
Schlafzimmerzahl (1,5 / 2,0 / 2,5), Hotelzimmer als ein Bett, Pflegeheimzimmer als Betten; New York
„approximately 50 apartments" je Haus (Building America Case Study DOE/GO-102016-4704) mal 2,5
Personen; Spanien ohne Bewohnerzahl in der Quelle (Rechenwert 2,5, „unbekannt"). Die Kennwerte
stehen mit Zitat in den Konvertern, `objekt.json` führt **`bezugsmenge_herkunft`**, und die
√N-Skalierung nimmt nur belegte und abgeleitete Mengen.

**(b) Kalender (V2) — mit einer Ergänzung im Kern.** Die Konverter setzen die Feiertage des Landes und
Messjahrs (Norwegen gesetzlich, Spanien landesweit, USA Bundesfeiertage). Damit sie wirken, bekommt
`Messvergleichseingang` die **`MessFeiertage`**: Ein voller Messtag auf einem genannten Feiertag zählt
im Formabgleich als Sonn-/Feiertag, wie derselbe Tag in der Rechnung; ein Feiertag am Samstag bleibt
Samstag (4.2). Ohne Angabe ist der Dialogweg unverändert. Die spanischen Reihen laufen in Ortszeit
statt in UTC. Wirkung: Das Formmaß ändert sich je Objekt um höchstens 0,005, keine Ampel wechselt —
die Abweichung liegt in der Tagesgestalt der Gebäudeart gegen das Klassenmittel, nicht im Kalender.

**(c) Band je Größenklasse (V3, V4) — Analyse, keine Parameteränderung.** Das Werkzeug weist je Objekt
das Perzentil der Messspitze in der gerechneten Dauerlinie und ihre Lage gegen die Jahresspitzen der
Realisierungen aus, verdichtet je Größenklasse. Ergebnis: Bei den acht Wohn- und Pflegeobjekten mit
N ≥ 10 liegt die Messspitze zwischen P96 und P99,9 — alle über dem Konzeptband P85–P95, alle unter der
Rechenspitze. Die Lehre aus 3.6 stimmt der Richtung nach, nicht dem Quantil nach. Bei N < 10 misst ein
Quantilband der Dauerlinie die Ziehung einer Stunde. Daraus die neue Frage **ZU35** (Kapitel 9).

**(d) Formschwelle (V5)** benannt geschlossen: ZU21 hat 0,01 bestätigt; große Wohnobjekte liegen bei
0,0085 bis 0,0153 (drei von sechs grün), Einzelhaushalte gegen ein Klassenmittel bei 0,022 bis 0,042
— das gehört zu ZU35. **Zweite Zone je Objekt** nicht anwendbar: Die Quelle nennt eine Küche nur für
ein Hotel und keine Mahlzeitenzahl.

**(e) Befund an den Daten.** Die Jahresspitze einiger spanischer Haushalte war ein Messartefakt
(Nachholwert nach einer Übertragungslücke, Einzelintervall mit unplausiblem Durchfluss); der
Konverter verwirft und zählt beide, ihr Spitzenverhältnis fällt von bis zu 8,2 auf höchstens 3,2.

**(f) Ergebnis (erster → zweiter Lauf).** Band grün 0 → 0, Form grün 3 → 3, Energie grün 21 → 21,
Objekte grün 0 → 0. √N-Skalierung grün (−0,29, alle 21, mit Platzhaltern) → **rot (+0,54, elf
Objekte mit belastbarer Bezugsmenge)**; über alle 21 zum Vergleich −0,06. Die Bestätigung des ersten
Laufs hing an den Platzhaltern; die offenen Daten spannen innerhalb einer Nutzungsart keine
Größenordnung von N, die Prüfung bleibt bei K5. Kalibrierfaktoren Norwegen 1,1 bis 3,1 statt 0,6 bis
4,5.
### N29 (26.09.2026) — ZU34 umgesetzt: ein gemeinsamer Katalogeinstieg auf dem iPad

**Auftrag:** „führe aus: Gemeinsamer Katalogeinstieg auf iOS“ — der Katalog der
Brauchwasser-Nutzungsarten und die zwei Kataloge der Gebäudehülle sollen auf dem iPad sichtbar
erreichbar sein, nicht nur über den Hilfe-Assistenten (ZU34, N23 (b)); die übrigen
Katalogverwaltungen bleiben dort geschlossen (KI-D-Q10), und ein weiterer Katalog soll später als
Datenzeile hinzukommen. Kein Schemaschritt, Testdatenbank unberührt.

**Festlegungen.**

(a) **Ort: die Projektliste, nicht die Startseite.** Auf iOS ist die Projektliste die Startansicht;
die Startseite geht dort nicht auf (`IProjektQuelle.StartseiteGaben` ist ohne iOS-Fassung, sie kommt
mit iU11). Der Knopf „Kataloge…“ steht deshalb im Seitenkopf der Projektliste neben „Neues Projekt…“
— ein Katalog hängt wie ein neues Projekt an keinem vorhandenen.

(b) **Plattform: nur ohne Menüband.** Die Wurzel reicht die Einträge nur, wenn keine Kopfleiste
hereingereicht ist — dasselbe Merkmal, an dem schon die Gattungszeile der Startseite hängt. Unter
Windows führt das Menü alle Kataloge; ein Knopf mit dreien davon wäre dort eine zweite,
unvollständige Wahrheit. Die Windows-Schale ist unverändert.

(c) **Datenquelle: Menütabelle und Positivliste, keine zweite Liste.** `Menuepunkt` trägt das neue
Kennzeichen `Katalog`; zwölf Punkte der `Menuetabelle` führen es (Baustoffe, Bauteilaufbauten,
Brauchwasser-Nutzungsarten und die neun Geräte- und Verbraucherkataloge aus KI-D-Q10).
`Menuetabelle.Kataloge(freigegeben)` liefert die gekennzeichneten Punkte in Baumreihenfolge, deren
Ziel die Plattform öffnet; die Wurzel reicht dafür ihre Positivliste herein, die dazu als
`AppWurzel.FuehrtZiel` aus `OeffneMaske` herausgezogen ist (`OeffneMaske` fragt dieselbe Methode).
Heute ergibt das genau Baustoffe, Bauteilaufbauten und Brauchwasser-Nutzungsarten; ein weiterer
Katalog erscheint, sobald die Wurzel seinen Schlüssel führt. Namen aus den Textschlüsseln der
Menütabelle (beide Sprachen).

(d) **Bedienung.** Ab zwei Katalogen klappt unter dem Knopf eine Liste auf (`role="menu"`, je
Eintrag ein ganzer Knopf mit Berührungsmaß); bei genau einem trägt der Knopf dessen Namen und öffnet
ihn unmittelbar (Regel „kein Untermenü mit nur einem Punkt“). Geöffnet wird über
`AppWurzel.OeffneMaske` — derselbe Weg wie aus dem Hilfe-Assistenten; ohne Parametersatz bleibt die
Liste stehen und das Banner nennt den Grund.

(e) **KI-Sicht.** Knopf und Liste tragen kein Eingabefeld; eine Feldkarte entfällt, die
`KiMaskenabdeckungWacheTests` bleibt ohne Nachtrag grün. Keine Änderung an der iOS-Schale und am
Prüfmodus: Der Einstieg läuft ganz in der `AppWurzel`, die Katalogprobe aus #524 bleibt an
`EPOS_PRUEFLAUF_KATALOGIMPORT`.

**Tests:** `KatalogeinstiegTests` (13 Fälle): Freigabe genau der drei Kataloge, zwölf gekennzeichnete
Punkte mit Beschriftung in beiden Sprachen, kein Knopf ohne Einträge und mit Kopfleiste, ein Katalog
unmittelbar, Liste ab zwei, Öffnen der drei Kataloge über die Wurzel, benannte Ablehnung ohne
Parametersatz. Zwei Ressourcenschlüssel `KATEIN_KNOPF`, `KATEIN_LISTE`.

**Gate:** vor dem Merge gefilterte Tests (Startseite, AppWurzel, Katalog, KiMasken, Menue, Projektliste, Dokumentations-, Wiki- und Ordnungswache) 2 509 erfolgreich; nach dem Merge von `origin` (Stand `ebf01a90`): Kern-Filter 0 Fehler; voller Lauf 0 Fehler (15 614 erfolgreich, 2 übersprungen — EPOS.Kern.Tests 8 109, EPOS.UI.Tests 6 543, KiKern.Tests 549, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27); Windows-Schale 0 Fehler; Designer wiederholbar (+2 Schlüssel, zweiter Lauf +0); Wiki-Tabuwörter 0; kein Referenzlauf (kein Rechenweg).

**Nicht auf Windows prüfbar:** die Darstellung auf dem iPad (Lage der aufgeklappten Liste im Hoch-
und Querformat, Berührung); sie steht mit dem nächsten iOS-Lauf aus (Rückfrage beim Anwender).

Statuszeile #540, Protokoll
[`2026-09-26_ZU34_Katalogeinstieg_iOS.md`](../ueberholt/Protokolle/Zapfprofilgenerator/2026-09-26_ZU34_Katalogeinstieg_iOS.md).

**Folgen.**

| Nr. | Gegenstand | Wer | Wann |
|---|---|---|---|
| — | Sichtprüfung des Knopfes „Kataloge…“ auf dem iPad (`ios.yml`) | Anwender (Rückfrage) | mit dem nächsten iOS-Lauf |
| — | Weitere Kataloge auf dem iPad (KI-D-Q10): je Katalog ein Zweig der Wurzel und sein Schlüssel in `FuehrtZiel` — der Einstieg zieht ohne Änderung nach | Anwenderentscheid | iU11 |

---

### N28 (26.09.2026) — Speicherauslegung: Auslieferungswerte aus der Vorlage V4 im freien Paketteil

**Wortlaut** (Anwender, 26.09.2026): „setze um: Speicherauslegung". **Lesart:** die Setzungen der
Speicherauslegung (Prüfliste ZU21, Abschnitt 3; Abschnitt 4.7) bekommen ihre Auslieferungswerte aus
der INEKON-Vorlage `TWW-Auslegung_V4.xlsx` (Blattköpfe Version 2.1.2, Dateistand 30.07.2026; Ablage des
Anwenders, nur gelesen) und wandern in den freien Paketteil. Die Vorlage ist eine INEKON-eigene
Unterlage, weder Norm noch Produktdatenblatt; K8 greift nicht. Kein Schemaschritt.

**Werte und Fundstellen** (Blatt, Zelle, Beschriftung der Vorlage; daneben der fiktive Testwert, der
bis hierher in der Testdatenbank stand):

| Schlüssel | Wert | Fundstelle in V4 | Testwert vorher |
|---|---|---|---|
| `Speicherauslegung.Speichertemperatur_Vorgabe` | 60 °C | Eingaben B22 „Speichertemperatur T_Speicher" | 56 °C |
| `Speicherauslegung.Nutzanteil` | 0,80 | Eingaben B30 „nutzbarer Speicheranteil f_nutz" | 0,75 |
| `Speicherauslegung.Zuschlag` | 0,15 | Eingaben B31 „Sicherheitszuschlag" | 0,10 |
| `Speicherauslegung.Ladefenster.Laenge` | 8 h | Eingaben B63 „verfügbares Ladezeitfenster" | 10 h |
| `Speicherauslegung.Klassisch.LiterJePersonTag` | 35 l/(P·d) | Berechnung B387 „Faustwert-Zapfmenge" | 40 l/(P·d) |
| `Speicherauslegung.Klassisch.Spreizung` | 50 K | Berechnung B388 − B389 „Bezug Warmwasser" 60 °C − „Bezug Kaltwasser" 10 °C | 45 K |
| `Speicherauslegung.Klassisch.Warnfaktor` | 3 | Ergebnis B34 (Formel `E16 > 3·MAX(E13:E15)`, „klassischer Faustwert") | 2,5 |
| `Speicherauslegung.Nenninhalt.Raster` | 1 000 l | Ergebnis B25 (`CEILING(B24; 1000)` über dem Listenende) und A54 | 500 l |
| `Speicherauslegung.Nenninhalt.Liste.1` … `.14` | 100, 150, 200, 300, 400, 500, 800, 1 000, 1 500, 2 000, 3 000, 5 000, 8 000, 10 000 l | Ergebnis A40:A53 „Nachschlagetabelle marktübliche Speichergrößen" | 6 Stufen 120 … 1 400 l |

**Offen** (V4 führt keinen Wert; nicht ausgeliefert, in der Testdatenbank weiter fiktiv):
`Speicherauslegung.Ladefenster.Beginn` (22 h fiktiv) — die Bilanz der Vorlage lädt konstant über 24 h
(Berechnung B12), ihr Ladezeitfenster dient nur dem Vorschlag der Ladeleistung; und
`Speicherauslegung.GLF_Gueltigkeitsgrenze` (30 fiktiv) — Hilfe B66 nennt die Grenze des GLF-Faustwerts
nur qualitativ („ohne Wannen eingeschränkt aussagefähig"). Ohne Beginn aus Projekt oder externem
Katalogpaket lehnt die Speicherauslegung einer Auslieferung benannt ab (`PARAMETER_SCHLUESSEL_FEHLT`);
ohne Grenze entfällt der Gültigkeitshinweis des GLF-Verfahrens.

**Gegenprüfung** gegen die Formelsammlung der Vorlagenanalyse (Abschnitt 3) und den Rechenweg 4.7:
Nutzanteil, Zuschlag, Bezugsspreizung (60 − 10) und 35 l/(P·d) stehen dort wie in den Zellen; die
Rundung „kleinster Listenwert ≥ V_max, darüber auf volle 1 000 l" (Ergebnis B25) ist die Regel des
Kerns mit Raster 1 000 l; der Warnfaktor 3 ist die Schwelle „klassischer Faustwert über dem Dreifachen
des Maximums". **Abweichungen:** (1) Wirkung des Ladefensters — in V4 nur Schätzhilfe der Ladeleistung,
in EPOS-Plan begrenzt es zusätzlich die Nachladung der Stundenbilanz (4.7); der Wert ist derselbe,
seine Wirkung weiter. (2) Die Speichertemperatur 60 °C fällt mit der Mindesttemperatur nach
DVGW W 551 zusammen, die V4 in der Beschriftung nennt; ausgeliefert wird der Eingabewert der Vorlage,
nicht der Normwert (`W551.Mindesttemperatur` bleibt unberührt). (3) Nicht Gegenstand: die übrigen
Eingaben der Vorlage (Kaltwasser 10 °C, Zirkulation 10 W/m, Ladeleistung) — sie gehören zu anderen
Schlüsseln und bleiben, wie sie sind.

**Herkunftsart.** `EIGENKONSTRUKTION` — eine Setzung von INEKON aus einer eigenen Unterlage, weder
frei verfügbare Quelle (`FREI`) noch gerechnetes Verfahren (`VERFAHREN`). Die Paketteil-Regel
`TwwKataloge.PAKETTEIL_HERKUNFT` ließ nur `FREI` und `VERFAHREN` zu und ist um `EIGENKONSTRUKTION`
erweitert (ebenso die Prüfregel des Einspielskripts, nur für die Parameterdatei). `Quelle` nennt statt
der neutralen „Eigenkonstruktion" des Quellendossiers die Vorlage samt Fundstelle
(„INEKON-Vorlage TWW-Auslegung V4 (Version 2.1.2, 30.07.2026), Blatt …, Zeile …, Spalte …"),
`Ausgabe` die Beschriftung der Zelle — genauer als die Dossier-Regel und im Auftrag verlangt. Die
Fundstelle steht in Worten (Zeile, Spalte), weil eine Zelladresse wie „B387" das Typcode-Muster der
Produktdatenwache trifft; die Adresse selbst steht in der JSON-Quelle. Die älteren INEKON-Setzungen des
Paketteils (Urlaubsversatz, Anzeigetemperatur u. a.) behalten `FREI`.

**Erzeugungsweg.** Neue Quelle `Referenzlaeufe/Skripte/speicherauslegung_v4.json` (Wert, Einheit,
Blatt, Zelle, Fundstelle, Beschriftung; Kopf mit den zwei offenen Schlüsseln).
`tww_testkatalog_fiktiv.py --paketteil-schreiben` erzeugt daraus die 22 Zeilen am Ende von
`Tab_TwwParameter_STAMM.csv` (die 13 übrigen bleiben Handpflege) und hält sie bei jedem Lauf dagegen.
Die fiktiven Einträge dieser 22 Schlüssel fallen aus dem Testkatalog des Skripts: Paketteil und
Testkatalog teilen den natürlichen Schlüssel (Schlüssel, `TEST-1`), und die Wache
`Die_freien_Zeilen_der_Testdatenbank_gleichen_dem_Paketteil` verlangt jede Paketteilzeile in der
Testdatenbank. Die Testdatenbank führt also die V4-Werte (EIGEN, ReadOnly 0, `TEST-1`); drei Fälle,
die die fiktiven Werte auf ihr erwarteten, lesen jetzt die Werte der Datenbank oder erwarten die
V4-Werte (`ZapfprofilAuslegungHuelleTests`, `ZapfprofilAuslegungCtrlTests`,
`ZapfprofilHuelleStufenTests`); Fälle mit einem Parametersatz im Speicher behalten ihre erfundenen
Werte (`AuslegungTestbau`).

**Werkzeug und Wachen.** `Werkzeuge/Auslieferungsvorlage` prüft beim Einspielen jeden Parameter des
Paketteils gegen `TwwParameterkatalog` (Schlüssel bekannt, Einheit, Bereich; Berichtszeile „jeder
Parameter des Paketteils ist ein Schluessel des Programms, in Einheit und Bereich (35)") und zählt
`EIGENKONSTRUKTION` zu den Paketteilzeilen; `TwwKatalogWacheTests` und die Vorlagentests
(`TwwVorlageTests`, `AblaufTests`) vergleichen Herkunftsart und Anzahl je Zeile der Datei.

**Kapitel 9.** Zeilen K8 und ZU21 fortgeschrieben. Eine eigene Zeile für Abschnitt 3 der Prüfliste
führt Kapitel 9 nicht; der Auftrag ist der Entscheid, keine neue Kennung.

Statuszeile #543, Protokoll
[`2026-09-26_Speicherauslegung_V4.md`](../ueberholt/Protokolle/Zapfprofilgenerator/2026-09-26_Speicherauslegung_V4.md).

**Folgen.**

| Nr. | Gegenstand | Wer | Wann |
|---|---|---|---|
| — | `Speicherauslegung.Ladefenster.Beginn` und `…GLF_Gueltigkeitsgrenze` festlegen (V4 führt keinen Wert) — oder bewusst dem Projekt bzw. dem externen Katalogpaket überlassen | Anwenderentscheid | offen |

---

### N30 (26.09.2026) — ZU25, Rest (a) behoben: der Konstruktor öffnet mit dem Bezug des gespeicherten Tags

**Anlass.** Folge (a) aus N21: Ein wieder geöffneter Konstruktor begann bei einem gespeicherten
Konstruktortag „ohne Bezug", und ein erneutes OK baute den Tag unskaliert — die Auslegung rechnete
danach anders als vor dem Schließen. Statuszeile #547, Protokoll
[Konstruktor: Bezug](../ueberholt/Protokolle/Zapfprofilgenerator/2026-09-26_Konstruktor_Bezug.md).

**Ursache.** Der Dialog gab dem Konstruktor den Bezug allein aus dem Entwurf. Ein gespeicherter Tag
hat keinen Entwurf mehr; sein Bezug steht an seiner Katalogzeile (`Tab_TwwBedarfstag_STAMM`,
`Bezugsmenge` und `Bezugsart`, Schritt 124), und `ZapfprofilCtrl.Lies` las ihn nicht.

**(a) Kern.** `ZapfprofilStand.KonstruktorBezug` (`KonstruktorBezugStand`: Tag gefunden, Bezugsmenge,
Bezugsart): `Lies` liest ihn bei Quelle Konstruktor mit `ID_Bedarfstag` aus der Katalogzeile, der
Schreibweg gibt ihn mit dem zurückgegebenen Stand weiter (beim Entwurf dessen Bezug, sonst im Vorgang
gelesen). Kein Rechenweg geändert.

**(b) Hülle und Dialog.** Die Hülle setzt Bezugsart und Bezugsmenge an die Eingaben
(`KonstruktorBezugsart`, `KonstruktorBezugsmenge`) — nur bei Quelle Konstruktor ohne Entwurf; der
Konstruktor beginnt mit ihnen. Trägt die Katalogzeile keinen vollständigen Bezug (ein Datensatz vor
Schritt 124 oder ein Tag „ohne Bezug") oder steht sie nicht mehr, beginnt er ohne Bezug **mit
benanntem Hinweis** (`ZPG_AUS_KON_BEZUG_OHNE`, `ZPG_AUS_KON_BEZUG_TAG_FEHLT`, beide Sprachen). Ein OK
macht den Entwurf zum Träger des Bezugs; der gespeicherte Bezug und der Hinweis fallen weg.

**(c) Kein Schemaschritt.** Bezugsart und Bezugsmenge gehören zum Tag, nicht zum Auslegungssatz —
die Auslegung skaliert den Tag auf die Menge einer Gruppe derselben Bezugsart, und die Katalogzeile
trägt beide. Eine Spalte an `Tab_TwwProjekt` oder `Tab_TwwKonstruktorzeile` hielte dieselbe Angabe
doppelt; eine Ableitung aus den Zonen ist entbehrlich. Projektkopie und `.wpx`-Paket tragen den Bezug
schon (Katalogverweis bzw. Katalogkopf mit Bezugsart).

**(d) Nachweis.** Kern-Fall speichern → laden → Auslegung exakt gleich (auch nach erneutem OK mit
geladenen Zeilen und geladenem Bezug), Gegenprobe ohne Bezug rechnet anders, Kopie und Paket; Fall
„alter Datensatz ohne Bezug" mit Hinweis; zwei bunit-Fälle (Dialog zeigt den Bezug; Hinweis als
Statuszeile). Gates im Protokoll. Kein Logbuch-Satz (Kleinigkeit), keine Wiki-Änderung.

**Folgen.**

| Nr. | Gegenstand | Wer | Wann |
|---|---|---|---|
| Sicht | Sichtabnahme unter Windows: Konstruktortag mit Bezug bauen, speichern, schließen, wieder öffnen — Bezugsart und Menge stehen im Konstruktor | Anwender | nach dem Push |

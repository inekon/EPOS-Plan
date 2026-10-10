# Umsetzungskonzept: Kältemittel als Stammdatum (KM4)

**Stand 10.10.2026 — Fassung 2 (nach Gegenlesen), Umsetzungsentwurf zur Abnahme durch den Anwender** · Codestand
`abe290f70` (Zweig `ios_migration_september`) · Schemastand 211 (`KaelteKatalogfelderSchema`, K‑A; 212 KB‑D bei der
Sitzung Gebäudesimulation angemeldet, nächster freier Schritt 213) · Referenzbasis R51
`2026-10-10_R51_FreieKuehlung` (29 Projekte) · Fachkonzept
[`Konzept_Kaeltemittel_Stammdatum_EPOS-Plan.md`](Konzept_Kaeltemittel_Stammdatum_EPOS-Plan.md) (Fassung 2, Fragen
KM4‑Q1 bis KM4‑Q17 offen).

**Geltung und Abgrenzung.** Das Fachkonzept sagt, *was* entsteht (Kältemitteltabelle, Verweise, Zuordnung,
Zulässigkeitsprüfung, Meldungsweg, Eingaben, Schema, Oberfläche, Regressionsnetz, Fragen). Dieses Papier sagt,
*wie, wo, in welcher Reihenfolge und mit welcher Abnahme* es gebaut wird; Verweise der Form „FK 3.3“ zeigen auf
Abschnitte des Fachkonzepts. Es plant nach den Empfehlungen zu KM4‑Q1 bis KM4‑Q17; entscheidet der Anwender anders,
ändert sich der Schnitt dort, wo es angegeben ist. Dieses Papier ist kein Rechtsrat.

---

## 1 Zielbild und Grundsätze

1. **Keine Rechenwirkung.** Kältemittel, Tabelle, Verweis und Prüfung ändern keinen Vektor, keinen Skalar und —
   ohne Inbetriebnahmejahr — keine Protokollzeile; die Basis bleibt, keine Einfrierregel kommt hinzu (FK 8).
2. **Berechnet, nie gespeichert.** Zulässigkeit entsteht je Aufruf aus Stammdaten, Jahr und Regelzeilen; keine
   Spalte trägt sie, `Katalog_Ausgelaufen` bleibt dem Katalogabgleich (FK 2.6).
3. **Eine Quelle der Spalten.** `ID_Kaeltemittel` geht in `KaeltemaschineSchema.Fachspalten` (Katalogabgleich,
   Katalogpaket, Katalogfassung mit `Katalogverweis` über den Code); Projektkopie und Schreibwege bekommen je einen
   benannten Schritt (Muster `TeillastSchreiben`), das Projektpaket setzt die ID am Ziel aus dem Text.
4. **Prüfen in einer reinen Klasse.** `Kaeltemittelzulaessigkeit` rechnet ohne Datenbank aus einem Eingang; die
   Datenbankseite (`Warnkriterien.KaeltemittelPruefen`) baut nur den Eingang; die Regelzeilen kommen aus dem
   Gesetzeskatalog, gesät als Generation, nicht im Schemaschritt.
5. **UB bleibt unberührt.** `Bivalenzvorgaben`, Klappliste, Vorgabeklassen, `Tab_WP(_STAMM)`: keine Änderung; die
   Tabelle trägt die Codes, eine Wache hält die Teilmenge (FK 3.2). Jede Abweichung davon ist vorher mit der Sitzung
   Gebäudesimulation abzustimmen; die Prüfung jeder Wärmepumpe (KM4‑Q16) liest nur.
6. **Keine Normtexte, keine Produktdaten, kein Raten.** Gesäte Werte tragen `Quelle` und `Status`
   (`VORLAEUFIG` bis zur Prüfung am EUR‑Lex‑Volltext); unbekannte Texte bleiben unzugeordnet; Beispiele neutral.

## 2 Ausgangslage im Code (Kurzfassung, Einzelheiten FK 2)

| Ort | Heute | Andockpunkt |
|---|---|---|
| `EPOS.Kern/Allgemein/Update/KaeltemaschineSchema.cs` (182), `KaelteKatalogfelderSchema.cs` (211, K‑A) | `Kaeltemittel` TEXT in `Grundspalten`/`Fachspalten`; `Geraeteart`, `Kaeltemittel_GWP`, Füllmenge; Muster „nachtragen, nur wo leer, Prüfsumme neu“ | neuer Schritt `KaeltemittelSchema` (ab 213): Tabelle, `ID_Kaeltemittel`, `Inbetriebnahmejahr`, Saat der Kältemittel, Zuordnung |
| `EPOS.Kern/Allgemein/Katalog/Katalogfassung.cs` (Register Stufe 3 „KM“, `Katalogverweis`, `Schluesselstamm`), `Update/KatalogfassungSchema.cs` (`KatalogSchluesselSaat`) | Katalog der Kältemaschine; Verweise über stabile Namen | Register `KMT` vor `KM`, Verweis `ID_Kaeltemittel` → `Code` |
| `EPOS.Kern/Controller/ProjektExportImportCtrl.cs` (`LoeseKatalogAuf`, Ausnahme `ID_Stamm`, `SqlNachtragProjekt`) | fehlende Katalogzeilen werden am Ziel angelegt; Kältemaschinen‑Verweise reisen nicht | `ID_Kaeltemittel` als Ausnahme, Nachtrag aus dem Text am Ziel; `Paketanhebung` Art Import |
| `EPOS.Kern/Model/KaeltemaschineModel.cs`, `Controller/KaeltemaschineStammCtrl.cs` (`Lesen`, `Pruefen` ohne Datenbank, `KopfSchreiben`, `TeillastSchreiben`, `KaeltemaschineCtrl.AusKatalogUebernehmen`), `KaeltemaschineAnlageCtrl.cs` | `Kaeltemittel` als `string`, ungeprüft; Leer bleibt Leer | Modellfeld, `Lesen` mit Spaltenprobe, Schreibschritt mit Code‑Abgleich und Leer → NULL, Trefferprobe im Schreibweg |
| `EPOS.Kern/Allgemein/Katalog/ParameterVerwendung.cs` (`Kaeltemaschine`) | jede Stammspalte hat einen Eintrag (`ParameterVerwendungTests`) | Eintrag `ID_Kaeltemittel` (`DLG`) |
| `EPOS.Kern/Allgemein/Wirtschaftlichkeit/GesetzKatalog.cs` (`Vorbelegung()`, `StelleKatalogSicher`, `AlleDerKlasse`, `WertMitHerkunft`, `KlassenVorrat`, `KlasseAnzeige`), `Allgemein/DbWerte.cs` (`GESETZ_KLASSE_*`), `EPOS.UI/Dialoge/Wirtschaftlichkeit/GesetzeskatalogDialog.razor` | Generationensaat (Testdatenbank 9), Lesefassade, Pflegemaske | Klasse `FGAS`, Generation 10, Schlüsselvokabular, Leser der Regelzeilen |
| `EPOS.Kern/Allgemein/Simulation/Warnkriterien.cs` (`PruefeProjekt`, `KaeltespeicherPruefen`, `Warnbefund`), `SimulationControl.cs` (`WarnkriterienMelden`), `SimulationControl.Kaelte.cs` (`kuehl-…`‑Haken) | Befunde je Anlage, Laufstart, Bericht automatisch | `KaeltemittelPruefen`, Hinweis „nicht prüfbar“ nur mit Jahr |
| `EPOS.Kern/Allgemein/Simulation/SchemaModell.cs` (`KaelteBahnAnlegen`, Knoten mit `Warnung`/`Warntext`), `EPOS.UI.Daten/Simulation/SimulationKonfigHuelle.cs` (reicht durch; `WarnChip` an Wärmeerzeuger‑Kacheln), `EPOS.UI/Bausteine/SchemaBild.cs`, `Schema.razor` | Kältebahn‑Knoten (KB‑A) ohne Kältemittelbefund; Chip nur an Kaskaden‑Kacheln | Warntext am Kältemaschinen‑Knoten (nach KB‑D), Chip der Wärmepumpe greift schon |
| `EPOS.Kern/Allgemein/Simulation/Bivalenz/Bivalenzvorgaben.cs`, `Geraetegrenzen.cs`, `Controller/WaermepumpeGeraeteCtrl.cs`, `GeraetegrenzWerte.cs` | zehn Codes und `SONSTIGES`, Vorgabeklassen, Lesen/Schreiben des Texts (UB) | nur lesend: Code → Tabellenzeile; Wache „Codes ⊆ Tabelle“ |
| `EPOS.UI.Daten/Erzeuger/KuehlungKachelBau.cs`, `EPOS.UI.Daten/Simulation/KaeltebereichBau.cs` (KB‑A), `EPOS.UI/Seiten/Assistent/ProjektKopfSeite.razor`, `Controller/ProjektCtrl.cs` | Kachel mit Name und Anzahl; Bereich „Kälte“; Projektdaten | Hinweiszeile der Kachel; Feld Inbetriebnahmejahr (KM4‑Q5 a) |
| `EPOS.UI/Dialoge/Erzeuger/KaeltemaschineKatalogDialog.razor` (+ Daten, Texte, KiSicht; Überlagerung der Katalogauswahl), `KaeltemaschineAnlageDialog.razor` (Stufe 5), Komponente `KaeltemaschineKonfiguration` (KB‑B), `EPOS.UI.Daten/Erzeuger/Kaeltemaschine*Huelle.cs`; `EPOS.UI/Dialoge/Waermepumpe/WaermepumpeGeraetegrenzenFelder.razor`, `WaermepumpeAnlageDialog.razor` | Textfeld; Klappliste der Wärmepumpe mit Platz −1 | Klappliste aus der Tabelle (nach Abstimmung), Lesezeilen (nach KB‑B bzw. Stufe 5), Lesezeile der Wärmepumpe |
| `EPOS.Kern/Allgemein/Bericht/BerichtsDatenSammler.cs`, `BerichtsDaten`, `Tabellen/Berichtstabellen.Projekt.cs` (`tabelle.kaelteerzeuger`), `BerichtTexte.cs`, `Bausteine/BausteineProjekt.cs` (`HINWEIS_KAELTEMITTEL`) | Tabelle ohne Kältemittelspalte; Satz bleibt | Code und Klasse in `BerichtsDaten`, Spalte, Fußnote |
| `EPOS.Kern/Allgemein/Import/KaeltemaschineImportLeser.cs` (K‑C, gebaut), `Quellen/Kaeltemaschine_Kennfeldvorlage.csv` | Kopf „Kältemittel“ nur Trim, `GWP`, `FUELLMENGE` | ID über den Schreibweg (Code‑Abgleich) |
| `EPOS.Kern.Tests/Kaeltemaschine*Tests.cs`, `KaeltemaschineSchemaTests.cs` (32/30), `KatalogabgleichTests.cs` (51, Verweisreihenfolge), `ParameterVerwendungTests.cs`, `AufheizSchemaTests.cs`, `GesetzkatalogSaatWacheTests.cs`, `KaeltespeicherDatenbankTests.cs`, `SchemaModellTests.cs`, `UebergabegrenzeTests.cs`, `WikiProduktdatenWacheTests.cs`; `EPOS.UI.Tests/Dialoge/Kaeltemaschine*Tests.cs`, `Hilfe/KiMaskenabdeckungWacheTests.cs`, `SeitenschluesselTests.cs`, `MenuebandTests.cs` | Schema, Katalog, Paket, Saat, Dialoge, Wachen | Zahlen heben, neue Klassen nach Abschnitt 4 |

## 3 Etappen

Je Etappe eine Folge von Agentenwellen, jede mit höchstens rund 150 Werkzeugaufrufen, im eigenen Worktree,
committet auf dem Wellenzweig, ohne Push, ohne CI‑Lauf und ohne vollständiges Gate (das fährt die Orchestrierung
nach dem Merge). Builds laufen nie parallel; Tests mit `--filter` auf die betroffenen Klassen und den
xUnit‑Schaltern aus `CLAUDE.md`. **Modellwahl (Anwenderregel), je Auftrag ausdrücklich gesetzt:** `fable`
schneidet, nimmt ab, führt zusammen und bereitet Entscheide vor (Orchestrierung, komplexe Aufgaben, Konflikte
zwischen Ständen); `opus` Schema, Kern, Hüllen, Dialoge, Tests, Fachkonflikte, Konzeptabsätze; `sonnet` Merges ohne
Fachkonflikt, Gate, Statuszeile und Protokoll, Ressourcen, Wiki und Logbuch; `haiku` Zählungen und
Encoding‑Prüfungen. Reihenfolge je Welle: **Merge → Gate → Statuszeile und Protokoll → Push (auf Zuruf)**;
Statuszeilen unter „Sitzung Kälteanlagen“; Status‑, Entscheid‑ und Schemanummern unmittelbar vor Merge und Push
gegen `origin` messen.

### 3.0 Vorarbeit (keine Welle)

- Entscheide zu KM4‑Q1 bis KM4‑Q17 beim Anwender; Entscheidnummer nach Abgleich mit `origin`, nicht selbst vergeben.
- **Abhängigkeiten auf `origin`:** KB‑D (212) vor KM4‑E1 (Schemakette) und vor dem Kältebahn‑Teil von KM4‑E3
  (`KaelteBahnAnlegen`); KB‑B (`KaeltemaschineKonfiguration`, Seite) und Stufe 5 (E117 F6, Katalogauswahl im
  Anlagendialog) vor den Lesezeilen von KM4‑E3; der Katalogdialog nur nach Abstimmung mit der Sitzung „Dialoge und
  Korrekturen“. K‑A (211), KB‑A und K‑C liegen auf `origin`. Bis dahin baut keine Welle in den belegten Dateien.
- EUR‑Lex‑Volltext der Verordnung (EU) 2024/573 (Anhänge I, II, IV, Art. 13) vom Anwender bereitstellen lassen (die
  Umgebung erreicht EUR‑Lex nicht); ohne Text bleiben Regeln und Werte `VORLAEUFIG` bis KM4‑E2‑c.
- Schemanummer (nächste freie ab 213) in der Kopfzeile „Schemaschritt angemeldet“ der
  [Statusdatei](../Status_iOS_Migration.md) mit dem Vermerk „Sitzung Kälteanlagen“ anmelden und allein pushen.

### 3.1 KM4‑E1 — Kältemitteltabelle, Verweis, Inbetriebnahmejahr, Zuordnung

- **Ziel:** FK 3.1, 3.2, 4 und 6 — Tabelle, Register, Schemaschritt, Saat der Codes und Werte mit Quelle und
  Status, `ID_Kaeltemittel`, `Inbetriebnahmejahr`, Zuordnung des Textbestands, Lesen und Schreiben; **keine Prüfung**.
- **Schema:** `EPOS.Kern/Allgemein/Update/KaeltemittelSchema.cs` (`SCHRITT = <Vorgänger>.SCHRITT + 1`,
  `Tab_Kaeltemittel_STAMM` nach FK 4.1 mit Metaspalten und `Code … COLLATE NOCASE`, `ID_Kaeltemittel` an
  `Tab_Kaeltemaschine(_STAMM)`, `Tab_Projekt.Inbetriebnahmejahr` (KM4‑Q5 a), Register `KMT` **vor** `KM` in
  `Katalogfassung` Stufe 3 mit `Katalogverweis`, Schlüssel nach `Schluesselstamm`, Saat der Kältemittelzeilen
  (KM4‑Q2, Q3, Q10, Q11; Wertemengen als `DbWerte`‑Konstanten), Zuordnung nach FK 3.2 mit
  `KatalogSchluesselSaat.Ausfuehren`, wiederholbar in `DataRepository.VorgangOhneFremdschluessel()`); Einträge in
  `SchemaStand.Zielversion`, `Paketanhebung.STUFEN` (Art Import), `SchemaMigration` der Windows‑Schale,
  `Werkzeuge/Testdatenbankschema`, `EPOS.Kern.Tests/TestDatenbank.cs`; Testdatenbank heben (LFS‑Filter aktiv);
  `Referenzlaeufe/LIESMICH.md` um den Schemastand; Zahlen in `KaeltemaschineSchemaTests` (33/31) und
  `KatalogabgleichTests` (52) heben.
- **Katalog und Controller:** `KaeltemaschineSchema.Fachspalten` um `ID_Kaeltemittel`; `ParameterVerwendung.Kaeltemaschine`
  um den Eintrag (`DLG`); `KaeltemaschineModel` (`ID_Kaeltemittel`), `KaeltemaschineStammCtrl` (`Lesen` mit
  Spaltenprobe, Schreibschritt nach Muster `TeillastSchreiben` mit Code‑Abgleich, Trefferprobe und Leer → NULL;
  `Pruefen` bleibt ohne Datenbank), `AusKatalogUebernehmen` kopiert die ID; Projektpaket (Ausnahme wie `ID_Stamm`,
  Nachtrag aus dem Text am Ziel nach Muster `SqlNachtragProjekt`); Katalogabgleich; neuer `KaeltemittelCtrl`
  (Liste, Zeile, Anlegen, Ändern, Löschen sofern unbenutzt, Code einer verwendeten Zeile gesperrt;
  Auslieferungszeilen lesend); Wache „zehn Codes der Klappliste ⊆ Tabellencodes“; `Werkzeuge/Auslieferungsvorlage`
  gegen die neue Registertabelle prüfen.
- **Tests:** `KaeltemittelSchemaTests` (Spalten, CHECK, Wiederholung, Register, Zuordnung der drei Projektkopien
  und der Auslieferungssätze 1 bis 3 mit neuer Prüfsumme, 34 Typkennfelder leer, unbekannter Text ohne ID),
  `KaeltemittelCtrlTests`, `KaeltemaschineDatenbankTests` erweitert („Projektkopie trägt ID“, Leer → NULL,
  Trefferprobe), `KatalogabgleichTests`, `ProjektpaketAnhebungTests` (ID reist nicht, Nachtrag aus dem Text,
  fehlender Code → ID leer, Text bleibt), `KaeltemittelCodesWacheTests`, `ParameterVerwendungTests`,
  `TestdatenbankSchemastandWacheTests`.
- **Abnahme:** Build `WP-Plan.Kern.slnf`; Filter‑Tests grün; `SqlDialektPruefer` grün; Referenzlauf der
  CI‑Auswahl gegen die Basis unverändert; Windows‑Schale baut auf Linux.
- **Wellen (2):** **E1‑a** `opus` Schema, Tabelle, Register, Saat‑Gerüst mit `VORLAEUFIG`, Zuordnung, Testdatenbank,
  Zahlen der Schematests, SQL‑Dialekt (≈ 130 Aufrufe); **E1‑b** `opus` Modell, Controller, `ParameterVerwendung`,
  `KaeltemittelCtrl`, Projektpaket, Katalogabgleich, Wachen, Tests (≈ 110).
- **Statuszeile:** „Sitzung Kälteanlagen — KM4‑E1 Schema <Nr> Kältemitteltabelle `KMT`, Verweis der Kältemaschine,
  Inbetriebnahmejahr, Zuordnung des Textbestands — gebaut, Basis unverändert“.

### 3.2 KM4‑E2 — Regelsaat, Zulässigkeitsprüfung, Meldungsweg, Regelabgleich

- **Ziel:** FK 3.3 bis 3.5, FK 4.5, FK 5 — Regelzeilen als Generation 10 des Gesetzeskatalogs, reine Prüfklasse,
  Regelleser, Warnbefund, Laufstart, Hinweis „nicht prüfbar“, Kältebahn‑Warntext, Bericht; danach der Abgleich der
  gesäten Regeln und Werte am EUR‑Lex‑Volltext.
- **Code:** `GesetzKatalog.Vorbelegung()` (Klasse `DbWerte.GESETZ_KLASSE_FGAS`, Einheit `GESETZ_EINHEIT_OHNE`,
  Schlüsselvokabular als Konstanten, `Status`, `Quelle`; `KlassenVorrat`, `KlasseAnzeige`, Ressource
  `GESETZ_KLASSE_ANZ_FGAS`; Testdatenbank auf Generation 10), `EPOS.Kern/Allgemein/Simulation/Kaelte/Kaeltemittelzulaessigkeit.cs`
  (Eingang, Ergebnis mit Klasse, Regelschlüssel, Grenze, Stichtag, Jahr, Leistung; Auswertung FK 5.1 mit Oberklasse,
  Band als Intervall, Zeitreihe je Schlüssel), `Kaeltemittelregeln.cs` (Leser über `GesetzKatalog.AlleDerKlasse`,
  Zerlegung des Vokabulars, Geräteklasse aus `Geraeteart`/`Bauart`/`Typ` nach FK 3.3, GWP‑Vorrang nach KM4‑Q17),
  `Warnkriterien.KaeltemittelPruefen` (Kriterium `KAELTEMITTEL_STICHTAG`, `Warnbefund` je Kältemaschine und je
  Wärmepumpe mit Code, Ressourcen `SIMWARN_KAELTEMITTEL_*` in beiden Sprachen), `SimulationControl.Kaelte`
  (`HinweisEinmal("kuehl-km-kaeltemittel-…")` nur mit Jahr), `SchemaModell.KaelteBahnAnlegen` (`Warnung`,
  `Warntext` am Kältemaschinen‑Knoten aus den Befunden; **nach KB‑D**), `BerichtsDatenSammler` → `BerichtsDaten`
  (Code, Klasse), `Berichtstabellen.Projekt` (Spalte „Kältemittel“, Fußnote), `BerichtTexte`;
  `designer_neu.py schreiben`.
- **Regelabgleich (E2‑c):** jede gesäte Regel‑ und Kältemittelzeile gegen den vom Anwender bereitgestellten
  Volltext; `Status` auf `GESICHERT` oder Zeile entfernen; n.b.‑Regeln bleiben draußen; Protokoll mit Fundstelle je
  Zeile (Anhang, Nummer, Buchstabe) — keine Textabschrift ins Repositorium.
- **Tests:** `KaeltemittelZulaessigkeitTests` (Proben FK 10: Klassen 1, 2, 4; Oberklasse Split; Bänder; Zeitreihe;
  „jedes F‑Gas“; GWP‑Vorrang; treffende Regel des frühesten Stichtags; ohne Jahr kein Ergebnis; Absorption),
  `KaeltemittelregelnTests` (Vokabular, unbekannter Schlüssel benannt verworfen, Geräteklasse),
  `GesetzkatalogSaatWacheTests` (Generation 10 in den Schranken), `KaeltemittelZulaessigkeitLaufTests`
  (Arbeitskopie, Muster `KaeltespeicherDatenbankTests`: 1059 mit Jahr 2027 → Warnbefund, Protokollwarnung,
  `bericht.warnungen`; 1063 → kein Befund; ohne Jahr → nichts), `SchemaModellTests` (Warntext am Knoten),
  `KaeltemaschineBerichtTests` (Spalte, `BerichtSchreiberOhneDatenbankWacheTests`).
- **Abnahme:** Filter‑Tests grün; Referenzlauf aller Projekte der Basis `GESAMT: PASS`, Projektdateien
  byte‑gleich, `protokoll.txt` der Referenzprojekte ohne neue Zeile.
- **Wellen (3):** **E2‑a** `opus` Regelsaat, reine Klassen und Rechenproben (≈ 100); **E2‑b** `opus` Warnkriterien,
  Laufstart, Hinweis, Kältebahn, Berichtsweg, Ressourcen, Lauftests, Referenzlauf (≈ 120); **E2‑c** `opus`
  Regelabgleich am Volltext, Statuspflege, Protokoll (≈ 60; braucht den Text vom Anwender, sonst entfällt die Welle
  und der Status bleibt `VORLAEUFIG`).
- **Statuszeile:** „Sitzung Kälteanlagen — KM4‑E2 Regelzeilen `FGAS` (Generation 10), Zulässigkeitsprüfung nach
  Anhang IV (Warnung, nie Sperre), Warnkriterium, Kältebahn, Berichtsspalte; Regeln <GESICHERT|VORLAEUFIG>; Basis
  unverändert“.

### 3.3 KM4‑E3 — Dialoge, Projektdaten, Kachel, Verwaltung, KI‑Sichten

- **Ziel:** FK 7.1 bis 7.6.
- **Oberfläche:** **E3‑a** Katalogdialog Kältemaschine (Klappliste aus der Tabelle mit Bestandseintrag „nicht
  zugeordnet“, Lesezeilen GWP mit Herkunft und Abweichungshinweis, F‑Gas, Sicherheitsgruppe, PFAS, natürlich;
  Schnellwahl „GWP aus Kältemittel übernehmen“; Hülle `KaeltemaschineKatalogHuelle`; KI‑Sicht) — **nur nach
  Abstimmung mit der Sitzung „Dialoge und Korrekturen“**, weil der Dialog die Überlagerung der Katalogauswahl ist,
  die Stufe 5 umbaut (KM4‑Q15). **E3‑b** Feld „Inbetriebnahmejahr“ in `ProjektKopfSeite.razor` mit `ProjektCtrl`
  (frei, KM4‑Q5 a); Lesezeile unter der Klappliste der Wärmepumpe in `WaermepumpeGeraetegrenzenFelder.razor` und
  Lesezeile „Zulässigkeit“ in `WaermepumpeAnlageDialog.razor` (frei, nur lesend); Hinweiszeile der Kachel „Kühlung
  und Kälteanlagen“ (`KuehlungKachelBau`, frei); Lesezeile „Zulässigkeit“ in `KaeltemaschineKonfiguration` (nach
  KB‑B) und im Anlagendialog der Kältemaschine (nach Stufe 5) — wartet, der Rest nicht. **E3‑c** Verwaltungsdialog
  „Kältemittel“ im Menü „Daten & Import“ (`Menuetabelle.cs`, kein Untermenü mit nur einem Punkt; Seitenschlüssel,
  Hilfeziel, Menüband; Hülle; KI‑Sicht). Ressourcen in beiden Sprachen je Welle, `designer_neu.py schreiben`.
- **Tests:** bunit `KaeltemaschineKatalogDialogTests` (Klappliste, Bestandseintrag, Lesezeilen, Schnellwahl,
  Lesemodus), `KaeltemaschineAnlageDialogTests` (Lesezeile), `WaermepumpeStammGrenzenTests` und
  `WaermepumpeBivalenzTests` (Lesezeile, Klappliste unverändert), `ProjektKopfSeiteTests` (Feld, CHECK‑Bereich),
  `ErzeugerReiterKuehlungTests` (Hinweiszeile), `KaeltemittelDialogTests`, `SeitenschluesselTests`,
  `HilfezielTests`, `MenuebandTests`, `KiMaskenabdeckungWacheTests`, `KiDialogkatalogTests`; Kultur de‑DE über
  `Kulturvorrichtung`.
- **Abnahme:** Filter‑Tests grün; Windows‑Schale baut auf Linux; Windows‑Sichtabnahme durch den Anwender
  (Klappliste, Lesezeilen, Inbetriebnahmejahr, Kachel, Kältebahn, Verwaltung).
- **Wellen (3):** **E3‑a** `opus` Katalogdialog, Hülle, Schnellwahl, KI‑Sicht, bunit (≈ 140; wartet auf die
  Abstimmung); **E3‑b** `opus` Projektdaten, Wärmepumpen‑Lesezeilen, Kachel, Lesezeile `KaeltemaschineKonfiguration`
  und Anlagendialog (≈ 110; die zwei Lesezeilen warten auf KB‑B und Stufe 5); **E3‑c** `opus` Verwaltungsdialog mit
  Menü, Seitenschlüssel, Hilfeziel und Tests (≈ 80).
- **Statuszeile:** „Sitzung Kälteanlagen — KM4‑E3 Kältemittel‑Klappliste und Lesezeilen, Inbetriebnahmejahr an
  den Projektdaten, Kachel‑Hinweis, Verwaltung Kältemittel“.

### 3.4 KM4‑E4 — Wiki, Logbuch, Konzeptnachzug, Konzepte „wie gebaut“

- **Ziel:** FK 7.7 und KM4‑Q14; beide Papiere bekommen einen Abschnitt „Umsetzung — wie gebaut“ und wandern per
  `git mv` nach `Dokumentation/ueberholt/`, Indexzeilen in `Dokumentation/LIESMICH.md` angepasst.
- **E4‑a Wiki und Logbuch (`sonnet`):** `Projekte/Wiki/Programm Dokumentation - Gerätekataloge.wiki`,
  `… - Kühlung.wiki` (mit KB und den Dialogsitzungen abstimmen, die Seite ist mitbelegt), `… - Wärmepumpe.wiki`;
  Logbuch‑Entwurf (zwei Sätze, Version beim Anwender erfragen) in `Dokumentation/aktuell/Wiki_Update_2026-09-26.md`
  Abschnitt 2; ausstehender Upload in der Statusdatei; `WikiProduktdatenWacheTests.Normbezeichnungen` um die gesäten
  Codes.
- **E4‑b Konzeptnachzug und „wie gebaut“ (`opus`, nach Abstimmung mit der Sitzung Gebäudesimulation):**
  Kühlkonzept §14 und 6.3 um je einen Satz (Stammdatum und Zulässigkeitsprüfung zugelassen, Treibhauswirkung weiter
  ausgeschlossen); `ParameterVerwendung` Text der Spalte `Kaeltemittel`; Abschnitt „Umsetzung — wie gebaut“ in
  beiden Papieren, `git mv`, Index.
- **Abnahme:** `DokumentationLinkWacheTests`, `WikiProduktdatenWacheTests`, `RepositoryOrdnungWacheTests`,
  Gegenlesemuster aus `CLAUDE.md` ohne Treffer.
- **Wellen (2):** **E4‑a** `sonnet` (≈ 50); **E4‑b** `opus` (≈ 40).
- **Statuszeile:** „Sitzung Kälteanlagen — KM4‑E4 Wiki‑Quellen, Logbuch‑Entwurf, Nachzug Kühlkonzept §14/6.3,
  Konzepte nach ueberholt“.

**Summe:** **10 Wellen** (E1 2, E2 3, E3 3, E4 2), davon 9 `opus` und 1 `sonnet`, rund 940 Werkzeugaufrufe; dazu je
Etappe Merge, Gate, Statuszeile und Protokoll durch die Orchestrierung (`fable`; Merges ohne Fachkonflikt, Gate und
Textpflege an `sonnet`, Zählungen an `haiku`). Ein iOS‑Lauf ist nicht begründet (keine Änderung an der iOS‑Hülle).
Abhängigkeiten: E1 nach KB‑D (212) auf `origin`; E2 nach E1 (Kältebahn‑Teil nach KB‑D); E3‑a nach E2 und der
Abstimmung; E3‑b nach E2 (zwei Lesezeilen nach KB‑B und Stufe 5); E3‑c nach E1; E4 nach E3. E2‑a kann parallel zu
E1‑b laufen (reine Klassen und Regelsaat ohne Schemaschritt).

## 4 Prüfungen und Abnahme

**4.1 Proben (FK 10) → Testklassen.**

| Probe | Klasse | Etappe |
|---|---|---|
| Tabelle, Register `KMT` vor `KM`, Schlüssel, CHECK, Wiederholung, Zuordnung der Projektkopien und der Auslieferungssätze 1 bis 3, Typkennfelder leer, unbekannter Text ohne ID, Spaltenzahlen 33/31, Register 52 | `KaeltemittelSchemaTests`, `KaeltemaschineSchemaTests`, `KatalogabgleichTests` | E1 |
| Lesen, Schreiben, Code‑Abgleich, Trefferprobe, Leer → NULL, Projektkopie trägt ID, Löschen gesperrt bei Verwendung, Code unveränderlich | `KaeltemittelCtrlTests`, `KaeltemaschineDatenbankTests` | E1 |
| ID reist nicht, Nachtrag aus dem Text am Ziel, fehlender Code → ID leer, Text bleibt | `ProjektpaketAnhebungTests` | E1 |
| Zehn Codes der Klappliste ⊆ Tabellencodes; jede Stammspalte in `ParameterVerwendung` | `KaeltemittelCodesWacheTests`, `ParameterVerwendungTests` | E1 |
| Regelsaat Generation 10 in den Schranken; Klasse im Vorrat | `GesetzkatalogSaatWacheTests`, `KaeltemittelregelnTests` | E2 |
| Klassen 1, 2, 4; Oberklasse Split; Band als Intervall; Zeitreihe je Schlüssel; „jedes F‑Gas“; GWP‑Vorrang; frühester Stichtag; ohne Jahr nichts; Absorption | `KaeltemittelZulaessigkeitTests` | E2 |
| 1059 mit Jahr 2027 → Warnbefund, Protokoll, Bericht; 1063 → nichts; ohne Jahr → nichts; Warntext am Kältebahn‑Knoten | `KaeltemittelZulaessigkeitLaufTests`, `SchemaModellTests` | E2 |
| Dialoge, Lesezeilen, Inbetriebnahmejahr, Kachel, Verwaltung, Seitenschlüssel, Hilfeziel, Menüband | `EPOS.UI.Tests/Dialoge/…`, `Seiten/…`, Wachen | E3 |
| Wiki ohne Produktdaten, Links, Ordnung | Wachen | E4 |

**4.2 Referenzlauf.** Nach E1 (CI‑Auswahl) und nach E2 (alle Projekte der Basis) gegen die zur Bauzeit aktuelle
Basis; Toleranz der CI (Betrag ≥ 1 relativ 1e‑4, sonst absolut 0,01), Erwartung byte‑gleich; `protokoll.txt` der
Referenzprojekte ohne neue Zeile. Keine neue Basis; wird in der Zwischenzeit eine Basis von anderer Seite
eingefroren (R52 bei dieser Sitzung angemeldet), gilt die jüngste.

**4.3 Weitere Abnahmen.** `SqlDialektPruefer` nach jeder neuen Anweisung; Linux‑Bau der Windows‑Schale bei jeder
Änderung an Hülle oder Naht (`SchemaMigration`, Menü); Windows‑Sichtabnahme durch den Anwender nach E3; Protokoll
des Regelabgleichs (E2‑c) mit Fundstelle je Zeile.

## 5 Risiken und Festlegungen

| Risiko | Festlegung |
|---|---|
| Parallelsitzungen und Schemanummern (212 angemeldet, 213 frei) | Nummer vor dem Bau in der Kopfzeile mit Vermerk „Sitzung Kälteanlagen“ anmelden; `SCHRITT` hängt über `+ 1` an der Vorgängerklasse zur Bauzeit; bei Verschiebung nur Konstante und Statuszeile anpassen |
| KB‑B, KB‑D und Stufe 5 noch nicht auf `origin`; Katalogdialog als Überlagerung der Katalogauswahl | keine Welle baut vorher in deren Dateien; E1 wartet auf 212, der Kältebahn‑Teil auf KB‑D, die zwei Lesezeilen auf KB‑B und Stufe 5, der Katalogdialog auf die Abstimmung |
| UB‑Schnittstelle (`Bivalenzvorgaben`, `Tab_WP`, Klappliste, Einfrierregel „gesäte Übergabegrenzdaten“); Prüfung jeder Wärmepumpe (KM4‑Q16) | nur lesend; Wache „Codes ⊆ Tabelle“; jede Erweiterung der Klappliste oder eine ID an `Tab_WP` vorher mit der Sitzung Gebäudesimulation abstimmen |
| Rechtsangaben ohne Primärquelle; Berichtigungen des deutschen Texts | `Status = VORLAEUFIG` bis E2‑c; n.b.‑Regeln und Nr. 9 a (Füllmengenkriterium) werden nicht gesät; Warntext nennt Regel, Stichtag, Jahr und Leistung; nie sperrend; kein Rechtsrat |
| Normwerte und Normtexte | GWP nur aus der Verordnung (amtliches Werk) mit Quelle und Bezug je Zeile (KM4‑Q2); Sicherheitsgruppen leer (KM4‑Q3); keine Tafeln in Papier, Code oder Wiki |
| Regelauswertung lässt Fälle durch | Oberklasse erbt, Band als Intervall, Zeitreihe je Schlüssel, alle Schlüssel (FK 3.3); Proben in `KaeltemittelZulaessigkeitTests` |
| Mehrdeutige oder unbekannte Texte („R1234ze“, Anwendertexte) | nie raten: ID leer, Lesezeile „nicht zugeordnet“; nur der Auslieferungssatz über seinen Katalogschlüssel |
| Geräte‑GWP mit unbekanntem Bezug | Tabellen‑GWP vor Geräte‑GWP (KM4‑Q17); Abweichung über 1 % als Hinweis |
| Testdatenbank in parallelen Wellen | nur E1‑a (Schema) und E2‑a (Generation 10) heben sie; LFS‑Filter aktiv; vor dem Merge den Stand von `origin` mergen und Schritt und Nachsaat neu laufen lassen |
| Prüfsumme der Auslieferungssätze | Zuordnung setzt nur die leere ID; Prüfsumme über `KatalogSchluesselSaat` neu; Projektkopien und Anwendersätze unberührt; Katalogabgleich prüft die neue Fassung |
| Unsichtbarer Befund der Kältemaschine auf der Karte | Kältebahn‑Warntext (E2‑b, nach KB‑D) und Kachel‑Hinweiszeile (E3‑b); bis dahin Protokoll und Bericht |
| Wiki‑Wache gegen Typcode‑Muster (`R290`, `R1234ze` u. a.) | Codes als Normbezeichnungen in `WikiProduktdatenWacheTests` aufnehmen; Wiki nennt keine Produkte |
| Ein späterer Entscheid zu E118 (Treibhauswirkung) | dann eigenes Konzept mit Rechenwirkung und Einfrierregel; KM4 legt nur Füllmenge (K‑A) und GWP bereit |

## 6 Aufwand

| Etappe | Wellen | Modell | Werkzeugaufrufe (Schätzung) |
|---|---|---|---|
| KM4‑E1 | 2 | opus | ≈ 240 |
| KM4‑E2 | 3 | opus | ≈ 280 |
| KM4‑E3 | 3 | opus | ≈ 330 |
| KM4‑E4 | 2 | sonnet, opus | ≈ 90 |
| **Summe** | **10** | | **≈ 940** |

Dazu die Orchestrierung (`fable`): vier Merges mit Gate (`sonnet`), Statuszeilen und Protokolle (`sonnet`), eine
Anmeldung der Schemanummer, die Abstimmungen mit den Sitzungen Gebäudesimulation (UB, KB‑B, KB‑D, Kühlkonzept §14)
und „Dialoge und Korrekturen“ (Katalogdialog, Stufe 5), und die Bereitstellung des EUR‑Lex‑Volltexts durch den
Anwender.

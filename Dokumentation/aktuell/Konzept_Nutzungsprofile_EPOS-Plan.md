# Konzept: Nutzungsprofile — Katalog mit Kategorien, Generator der Konditionierungskalender und Zuordnung beim Import (Stufe NP)

> **Stand der Stufe: NP1 bis NP4 gebaut** (Wiki-Quellen und Logbuch-Entwurf mit NP4e nachgezogen; Upload gebündelt, Version beim Anwender).
> **NP5b gebaut:** Die Kategorie DIN führt die DIN/TS 18599-10:2025-10 — 43 Nutzungen mit Nummer und Name, ohne Werte
> (E96, Schemaschritt 190, 5.2); die Zuordnung `DIN_NUMMER` zählt weiter nach der Projektdatei (5.4).

> **Rev. 2 (vereint aus zwei Fassungen vom 05.10.2026) — Auftrag aus Entscheid E90 (Q37, 05.10.2026); Fragen
> Q38 bis Q47 mit E91 nach Empfehlung entschieden.** Der Anwender hat entschieden: Nutzungsprofile werden ein
> **frei definierbarer Katalog mit Katalogkategorien** — eine Kategorie für den Katalog nach DIN V 18599-10, eigene
> Kataloge mit eigenen Profilen, weitere Kataloge für SIA 2024, VDI 2078 usw. —, dazu eine **änderbare Zuordnung** der
> DIN-Profilnummern und der IFC-Nutzungsklassen auf Profile; Zugang aus dem Gebäudedialog, dem Zonenbaum des Imports
> und dem Zonendialog. **Normwerte werden weiterhin nicht ausgeliefert** (B15, P4 der
> [Konditionierungsprofile](Konzept_Konditionierungsprofile_EPOS-Plan.md)): Die Normen sind kostenpflichtig, keine
> Werttabelle aus DIN V 18599-10, SIA 2024 oder VDI 2078 steht im Repositorium. Ausgeliefert wird die **Struktur** —
> Kategorie, Profilnummern und -namen —; die Werte trägt der Anwender ein oder importiert sie. Dieses Papier arbeitet
> den Entscheid aus: Festlegungen NP-F1 bis NP-F24, Stufen NP1 bis NP4, Fragen Q38 bis Q47 (entschieden mit E91), Risiken in
> Kapitel 11. Register:
> [Status der Gebäudesimulation](Status_Gebaeudesimulation_VDI6007.md) (E90, Stufe NP).

---

## 0. Das Ergebnis in acht Punkten

1. **Ein Nutzungsprofil ist ein Parametersatz**, kein Kalender: Nutzungszeit, Betriebszeit, Nutzungstage,
   Sollwerte, Außenluft, Personen, Geräte, Beleuchtung — der gemeinsame Nenner der Normprofile. Ein **Generator**
   macht daraus die fünf Konditionierungskalender (Heizen, Kühlen, Lüftung, Geräte, Personen). Die Kalendermechanik
   der Konditionierungsprofile bleibt die Ausführungsschicht; am Rechenweg ändert sich nichts.
2. **Fünf neue Katalogtabellen** — Kategorie, Profil, Zeilenbild, Stundenprofil, Zuordnung — unter dem Namensstamm
   **`Raumnutzung`**, weil `Tab_Nutzungsprofil_STAMM` und `Z_Nutzungsprofil` schon der Pufferauslegung gehören
   (Befund B6).
3. **Ausgeliefert:** die Kategorie „EPOS-Muster“ mit den heutigen drei Mustern Wohnen, Büro, Schule, dazu Sonstige
   und fünf neue runde Muster (Sport, Gastronomie, Lager, Verkehr, Technik); die Kategorie „DIN/TS 18599-10“ mit
   Nummern und Namen **ohne Werte**; die Kategorien „SIA 2024“ und „VDI 2078“ **leer** mit ihrer Struktur.
4. **Die 14 ausgelieferten Konditionierungsvorlagen bleiben**, wie sie sind (P11 bleibt). Die drei EPOS-Muster
   Wohnen, Büro, Schule erzeugen über den Generator **bitgleich** dieselben Kalender wie heute „Vorlage übernehmen“
   mit den Vorlagen derselben Nutzung — das ist der Nachweis der Ergebnisneutralität.
5. **Die Zuordnung** (DIN-Profilnummer, IFC-Nutzungsklasse, HottCAD-Raumtyp → Profil) ersetzt die festen Tabellen
   im Code und ist änderbar; ausgeliefert wird sie mit den heutigen Paaren. Die Rangfolge beim Import bleibt: Ganglinie
   vor Nutzungsprofil der Datei vor Profil des Katalogs; „keine“ bleibt möglich.
6. **Am Ziel liegen nur Kopien**: Zone und Gebäude bekommen Kalenderkopien und den Profilnamen als Text, nie eine ID.
   `Tab_Konditionierungskalender.Nutzung` wird freier Text (der `CHECK` fällt, Schemaschritt mit Tabellenneubau).
7. **Oberfläche:** ein Blatt „Nutzungsprofile“ (Katalogbaum, Profileditor mit Vorschau, Zuordnungstabelle,
   CSV-Import); Zugänge aus Gebäudeeditor, Zonenbaum und Zonendialog; kein Menüpunkt.
8. **Vier Stufen, 19–28 PT**; die Referenzbasis bleibt unverändert, 1051, 1052 und 1054 bleiben byte-gleich.

---

## 1. Auftrag und Einordnung

### 1.1 Der Entscheid

E90 beantwortet Q37: „Nutzung der Zonen im Zonenbaum — unklar, nur drei feste Werte (Wohnen, Büro, Schule), beim
HottCAD-Import ohne Wirkung; frei konfigurierbar und aus dem Gebäudedialog erreichbar?“ Entschieden ist **Weg 3
erweitert**: Katalog mit Kategorien, änderbare Zuordnung, drei Zugänge. E79 hatte „DIN-V-18599-Profile als spätere
Welle“ vorgemerkt ([Befund HottCAD-Projektdatei](Gebaeudesimulation/2026-10-05_Befund_HottCAD_Projektdatei.md),
`PdProfileUsage`); dieses Papier ist diese Welle.

### 1.2 Was ausdrücklich bleibt

| Festlegung | Quelle | bleibt, weil |
|---|---|---|
| Keine Normwerte im Repositorium und in der Auslieferung | B15, P4 ([Konditionierungsprofile](Konzept_Konditionierungsprofile_EPOS-Plan.md) 1, 9.2) | Lizenz: DIN V 18599-10, SIA 2024 und VDI 2078 sind kostenpflichtig ([Grundlagen 1](Grundlagen_1_Normen_Regelwerke_TWW-Zapfprofile.md), Vorbemerkung) |
| Vorlagen je Größe, „alle Größen“ als Abkürzung | P11, E57 (Konditionierungsprofile 3.5, 7.4) | Die Kalenderkarte arbeitet je Größe; das Profil ist ein zusätzlicher Weg, kein Ersatz (Q45) |
| Weder Nennwert noch Saison in einer Vorlage | E54 | gilt für Vorlagen; das Profil regelt Nennwerte eigens (Q39, Q40) |
| Herkunft nie als ID am Ziel | KP1b, Statusdatei iOS | Eine spätere Änderung des Katalogs erreicht kein Gebäude und keine Zone |
| Rangfolge beim Import: Ganglinie vor Nutzungsprofil der Datei vor Vorlage | [Mehrzonenmodell](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) (Zonenplan mit Projektdatei), [Datenaustausch](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 16.3 | „Vorlage“ heißt künftig „Profil aus der Zuordnung“ |
| Der Rechenweg liest die Nutzung nicht | Befund B4 | keine Rückwirkung, keine neue Basis |
| Kein Schemaschritt für leere Zonen und den Merker „angelegt“ | E81 | nur eine Textspalte des Profils wird neu bewertet (Q41) |

### 1.3 Abgrenzung

- **Keine Normwerte**, auch nicht als „Beispiel“ oder in Tests; Testfälle tragen runde Phantasiewerte.
- **Keine Rückwirkung auf den Rechenweg**: Der Löser liest weiter allein die Kalender (B4). Eine sechste Größe
  „Beleuchtung“ im Rechenweg gehört nicht hierher (Q38).
- **Kein Bezug am Ziel**: Ein geändertes Profil ändert keine Zone; „neu übernehmen“ ist ein Handgriff des Anwenders.
- **Keine TWW-Profile**: Brauchwasser bleibt beim Zapfprofilgenerator (VDI 6002) und den Zapf-Nutzungsarten.
- **Keine Feuchte, kein Sonnenschutz, keine Raumluftqualität** aus den Normprofilen — der Parametersatz nimmt nur, was
  die fünf Größen tragen (2.2).

---

## 2. Befund

### 2.1 Der Bestand

| Nr. | Befund | Ort |
|---|---|---|
| B1 | Der Zonenbaum des Imports bietet je Zone ein `<select>` aus `plan.Nutzungen`; die Hülle füllt es aus der festen Liste `DbWerte.KOND_NUTZUNG_WOHNEN/BUERO/SCHULE`; der Kern führt dieselbe Liste als `Zonenplan.NUTZUNGEN` | `EPOS.UI/Dialoge/Import/GebaeudeImportDialog.razor:403–449`, `EPOS.UI.Daten/Bedarf/GebaeudeImportZonen.cs:154–158`, `EPOS.Kern/Allgemein/Import/Gebaeude/Zonenplan.cs:68` |
| B2 | Vorbelegung aus der IFC-Nutzungsklasse (Regel Z6): Büro → `BUERO`; Wohnen, Schlafen, Küche → `WOHNEN`; Sanitär, Verkehr, Lager, Technik, Sport, Gastronomie, Sonstige → keine | `Zonenplan.cs:288–298` (`NutzungAusKlasse`) |
| B3 | Vorbelegung aus der DIN-V-18599-10-Nummer der HottCAD-Projektdatei: 1–5 → `BUERO`, 8, 9, 28, 29 → `SCHULE`, 70, 71 → `WOHNEN`, jede andere → keine; die Nummern sind die der Projektdatei und zählen nach DIN V 18599-10:2018-09 (28, 29 Bibliothek) — nicht nach der Kategorie DIN/TS 18599-10 des Katalogs (5.4) | `EPOS.Kern/Allgemein/Import/Sqproj/Din18599Nutzung.cs`; ausgewiesen in `Referenzlaeufe/Importproben/LIESMICH_Importproben.md` |
| B4 | Speichern: `ZonenplanCtrl.NutzungUebernehmen` legt je Größe aus `Konditionierungsgroessen.Alle` die erste ausgelieferte Vorlage dieser Nutzung als Kalenderkopie an (`Konditionierungsarbeit.VorlageUebernehmen`); `ProjektdateiUebernehmen` mit der Rangfolge Ganglinie vor DIN-Nutzungsprofil der Datei vor Vorlage — Dateiwerte verwerfen die Vorlagenkopie je Größe. Der Rechenweg liest die Nutzung nicht, nur die Kalender; Kaskade Zone vor Gebäude | `EPOS.Kern/Controller/ZonenplanCtrl.cs:29–74`, `:81–148`; `GebaeudeModellEingang.cs:1234–1258`, `:2537–2622`, `:2710–2722`; `Zonenvorgaben.cs:166–205`; `Konditionierungdatenweg.cs:46–109` |
| B5 | Es gibt keine Spalte `Tab_Zone.Nutzung` (E81). Die Nutzung liegt nur als Kopie in `Tab_Konditionierungskalender.Nutzung` (Schritt 176), mit `CHECK` auf `WOHNEN`, `BUERO`, `SCHULE`, `SONSTIGE`; derselbe `CHECK` steht an `Tab_Konditionierungsvorlage_STAMM.Nutzung`. Eine unbeheizte Zone ohne Heiz- und Kühlkalender verliert ihre Nutzung beim erneuten Import | `EPOS.Kern/Allgemein/Update/KonditionierungNutzungSchema.cs:61`, `KonditionierungVorlagenSchema.cs:170` |
| B6 | **Namenskollision:** `Tab_Nutzungsprofil_STAMM` (fünf Profile der Pufferauslegung: WOHNEN, BEHERBERGUNG, PFLEGE, BUERO_SCHULE, GEWERBE) und `Z_Nutzungsprofil` (Quellen `ZAPF`, `KONDITIONIERUNG`, `GEBAEUDEART` → Pufferprofil) bestehen seit Schemaschritt 178. Die Quelle `KONDITIONIERUNG` liest genau die Kalenderspalte `Nutzung` aus B5 (`BUERO`, `SCHULE` → `BUERO_SCHULE`); Schritt 176 hat die Nutzung eigens für diesen Weg an die Kalenderkopie gelegt | `EPOS.Kern/Allgemein/Update/ProzessNutzungSchema.cs`, `EPOS.Kern/Allgemein/Pufferauslegung/NutzungsprofilZuordnung.cs:44–45`, `Nutzungsprofil.cs:48–55` |
| B7 | Ausgelieferte Vorlagen: `Tab_Konditionierungsvorlage_STAMM`, 14 Zeilen, `ReadOnly`, „EPOS-Muster mit runden Werten, weder Norm- noch Messwerte“ — Werte in 2.3 | [Konditionierungsprofile](Konzept_Konditionierungsprofile_EPOS-Plan.md) 3.5 (F22), 5.7 |
| B8 | Die Vorlagenverwaltung ist ein Blatt im Katalogeditor ohne Menüpunkt (E56): `KonditionierungVorlagenverwaltung.razor`, „Als Vorlage speichern…“ in `Kalenderkarte.razor` (fragt die Nutzung aus der festen Liste), „Vorlagen verwalten“ in `KonditionierungReiter.razor`; Gebäudeeditor `GebaeudeKatalogDialog.razor` Reiter Konditionierung (:554–564, Blatt :758–767); Zonendialog `ZonenDialog.razor` (Konditionierungsreiter :137); Projektliste `GebaeudeDialog.razor` (:184–187 Import, :369 Importdialog); Controller `KonditionierungsvorlageCtrl.cs`, `KonditionierungCtrl.cs` | `EPOS.UI/Dialoge/Bedarf/`, `EPOS.Kern/Controller/` |
| B9 | DIN V 18599-10 steht im Repositorium nur als **Nutzungszeiten-Tabelle** (Beginn, Ende, Stunden je Tag, Tage je Jahr; Nummern 1–41 teils in Sammelzeilen); Sollwerte, Luftwechsel und innere Lasten stehen nicht darin | [Grundlagen 1](Grundlagen_1_Normen_Regelwerke_TWW-Zapfprofile.md) 1.1, 1.4 |
| B10 | HottCAD-Projektdateien liefern je Zone die DIN-Profilnummer (`PdProfileUsage.ProfileUsageType`, beobachtet 1–47, 70, 71) und den Spaltensatz der Norm-Tabelle (Nutzungszeit, Personen, Raumtemperatur, Absenkung, Außenluft, Lasten, Beleuchtung); EPOS liest sie als Herkunft „Nutzungsprofil“. 44–47, 70 und 71 sind in keiner Ausgabe der Norm eine Nummer (2025 endet bei 43, Wohngebäude ohne Nummer) — Werte der Projektdatei; ob die Datei ab 22 wie 2018 oder wie 2025 zählt, belegt keine Datei im Repositorium (5.4) | `EPOS.Kern/Allgemein/Import/Sqproj/SqprojKonditionierung.cs:136–166`; [Befund HottCAD](Gebaeudesimulation/2026-10-05_Befund_HottCAD_Projektdatei.md) |
| B11 | Nennwerte: Geräte tragen `Interne_Waermegewinne` [W] als Nennwert des Kalenders, Personen den Nennwert Zahl × 70 W (`Matrixeingang.PERSON_W`); die ausgelieferte Personenvorlage trägt keinen Nennwert und wirkt nur als Nutzungszeit der Komfortkennzahlen. Nennwerte haben nur Geräte und Personen (`Konditionierungsgroessen.HatNennwert`) | `EPOS.Kern/Allgemein/Simulation/Gebaeude/Konditionierung/Matrixeingang.cs:21`, `:84–88`; `Konditionierungsgroesse.cs:136` |

### 2.2 Was die Normprofile gemeinsam haben

DIN V 18599-10, SIA 2024 und VDI 2078 beschreiben eine Raumnutzung mit verschiedenen Spaltensätzen, aber einem
gemeinsamen Kern. Nur dieser Kern geht in den Parametersatz; was keine der fünf Größen trägt, bleibt draußen. Die
Tabelle nennt Begriffe, keine Werte.

| Kern | DIN V 18599-10 (Spaltensatz, B10) | SIA 2024 | VDI 2078 | Ziel in EPOS |
|---|---|---|---|---|
| Nutzungszeit von/bis | Nutzungsbeginn/-ende | Tagesprofil Personen | Nutzungszeit | Personen, Geräte |
| Betriebszeit der Anlage | Betriebszeit Heizen, Kühlen, RLT | Betriebszeit | — | Heizen, Kühlen, Lüftung |
| Nutzungstage | Tage je Jahr | Tage je Woche | Arbeitstage | Wochentage des Generators |
| Heizsollwert, Absenkung | Raum-Solltemperatur, Absenkung | Raumtemperatur Winter | — | Heizen Tag/außerhalb |
| Kühlsollwert | Raum-Solltemperatur Kühlen | Raumtemperatur Sommer | Raumtemperatur | Kühlen Tag/außerhalb |
| Außenluft | Mindestaußenluft je m² bzw. je Person | Außenluft je m² | Außenluft | Lüftung |
| Personen | Belegungsdichte, Wärmeabgabe | Personenfläche, Wärmeabgabe | Personen, Wärme | Personen (Nennwert, Q40) |
| Geräte | spezifische Wärmeabgabe Geräte | Geräte je m² | Maschinen | Geräte (Nennwert, Q39) |
| Beleuchtung | Wartungswert der Beleuchtungsstärke | Leistung je m² | Beleuchtung | Geräte (Q38) |
| Tagesverlauf | Teilbetriebsfaktoren | Stundenprofile | Lastprofil | Standardwoche (NP-F9) |

Draußen bleiben: Feuchte, Sonnenschutz, relative Abwesenheit als eigene Zahl (sie geht in den Anteil der
Nutzungszeit), Aktivitätsklasse, TWW-Bedarf.

### 2.3 Die ausgelieferten Muster heute (F22)

| Vorlage | Heizen | Kühlen | Lüftung | Geräte | Personen |
|---|---|---|---|---|---|
| Wohnen | Tag 20 °C, Nacht 18 °C (22–6 Uhr) | Tag 26 °C, Nacht 28 °C | — | 100 % | Tag (7–17 Uhr) 50 %, Nacht 100 %, Wochenende 100 % |
| Büro | Mo–Fr 7–18 Uhr 20 °C, sonst 16 °C | Tag 26 °C, sonst aus | Nacht (18–7) und Wochenende 0,1 1/h | Tag 100 %, sonst 10 % | Mo–Fr 8–17 Uhr 100 %, sonst 0 % |
| Schule | Mo–Fr 7–15 Uhr 20 °C, sonst 16 °C | Tag 26 °C, sonst aus | wie Büro | wie Büro | Mo–Fr 8–14 Uhr 100 %, sonst 0 % |

Büro und Schule tragen die neun bundeseinheitlichen Feiertage „wie Sonntag“; die Personenvorlagen tragen keinen
Nennwert (B11). Die Muster sind **keine** Norm- und keine Messwerte.

### 2.4 Was daraus folgt

1. Eine Liste aus drei Kennungen kann E90 nicht tragen: Die Nutzung muss ein **Katalogobjekt** werden, und die
   Nutzungsspalte der Kalender muss Text aufnehmen.
2. Die Muster sind heute Zeilen der Vorgabe-Matrix je Größe; ein Profil, das sie abbildet, muss diese Zeilen
   **bitgleich** erzeugen können (NP-F7).
3. Wer an der Nutzungsspalte dreht, dreht an der Pufferauslegung (B6) — der Weg dorthin wird mitgezogen (NP-F15).
4. Normprofile ohne Werte sind leer — eine Zuordnung auf ein leeres DIN-Profil darf beim Import keinen leeren Kalender
   erzeugen (NP-F13).

---

## 3. Festlegungen

Festlegungen nach Empfehlung; sie gelten mit E91 und der Beauftragung von NP1. Die Fragen mit Entscheidbedarf
stehen in Kapitel 9.

| Nr. | Festlegung | Grund |
|---|---|---|
| **NP-F1** | **Profil = Parametersatz.** Ein Nutzungsprofil trägt den Kern aus 2.2; ein Generator erzeugt daraus die Vorgabezeilen je Größe und über die vorhandene Mechanik (`Vorgabematrix`, `Konditionierungsarbeit`) die Kalender. Profile werden nie gerechnet, nur übernommen | Die Kalender sind die Ausführungsschicht und bleiben es; der Rechenweg bleibt unberührt |
| **NP-F2** | **Namensstamm `Raumnutzung`** für die Tabellen (`Tab_Raumnutzungskatalog`, `Tab_Raumnutzungsprofil`, `Tab_Raumnutzungszeile`, `Tab_Raumnutzungsstunden`, `Tab_Raumnutzungszuordnung`); Klassen im Kern `Raumnutzungsprofil`, `Raumnutzungsgenerator`, Controller `RaumnutzungCtrl`. In der Oberfläche heißt es „Nutzungsprofil“ | `Tab_Nutzungsprofil_STAMM` und `Z_Nutzungsprofil` gehören der Pufferauslegung (B6); ein `Tab_Nutzungsprofil` neben `Tab_Nutzungsprofil_STAMM` wäre nach der Namensregel der Kataloge dessen Projektkopie — eine Falle für jeden Kopierweg |
| **NP-F3** | **Ein Katalog, projektübergreifend**, ohne Projektkopie: Kategorien und Profile stehen einmal in der Datenbank; `ReadOnly = 1` gehört zur Auslieferung | Wie die Konditionierungsvorlagen (Konditionierungsprofile 5.7); am Ziel liegen Kopien (NP-F14) |
| **NP-F4** | **Die Kategorie hat eine Art:** `EPOS_MUSTER`, `DIN_V_18599_10`, `SIA_2024`, `VDI_2078`, `EIGEN`. Die Art steuert nur Anzeige, Lizenzhinweis und die Prüfung „keine Werte in ausgelieferten Normkategorien“; der Generator kennt sie nicht | Eine Kategorie je Quelle, beliebig viele eigene (E90) |
| **NP-F5** | **Die Nummer in der Quelle ist Text** (`„22.1“`, `„70“`), eindeutig je Kategorie ohne Unterschied der Schreibung, darf leer sein | DIN V 18599-10 nummeriert die Hallen 22.1 bis 22.3; SIA 2024 nummeriert mit Punkten |
| **NP-F6** | **Jeder Kennwert darf leer sein** und heißt dann „Größe nicht belegt“: Der Generator legt für eine Größe ohne Kennwerte keinen Kalender an, das Ziel behält seinen (wie eine leere Zelle der Vorlage, Konditionierungsprofile 3.5) | Normkategorien kommen ohne Werte; ein halbes Profil setzt nur seine Größen |
| **NP-F7** | **Zeilenbild vor Generator:** Ein Profil darf je Größe ein **Zeilenbild** tragen — die Vorgabezeilen der Matrix (Tag, Nacht mit Zeiten, Wochenende, Ferien) in der Form von `Tab_Konditionierungsvorgabe`. Trägt eine Größe ein Zeilenbild, gilt es statt der Kennwerte dieser Größe. Die EPOS-Muster Wohnen, Büro und Schule tragen ihr heutiges Vorlagenbild als Zeilenbild, wo der Generator es nicht bitgleich aus Kennwerten herleitet | Bitgleichheit ohne Sonderlogik im Generator (Wohnen: Personen nachts 100 %, tags 50 %) |
| **NP-F8** | **Wochentage sind der Generatoreingang**; die Nutzungstage je Jahr sind abgeleitet aus Wochenmuster, Feiertagen und Ferien (E93), keine Eingabe: Das Blatt zeigt „Nutzungstage im Jahr: 252 (aus Wochenmuster und Feiertagen)“, die Übernahme am Ziel „…, abzüglich n Ferientage = m“; es entstehen keine Ferien aus einer Tageszahl (Q44) | Ferienzeiträume gehören dem Gebäude (Konditionierungsprofile 3.5); eine erfundene Ferienlage wäre eine stille Annahme |
| **NP-F9** | **Stundenprofile (SIA-Tagesverläufe)** sind optional: je Größe und Tagesart (Werktag, nutzungsfreier Tag) 24 Anteile 0 … 1 bzw. Sollwerte. Ein Profil mit Stundenprofil erzeugt einen **angelegten Kalender** mit Standardwoche; ohne Stundenprofil bleibt es beim Matrixweg | Die Kalender können das schon (Standardwoche, Konditionierungsprofile 3.2); kein neuer Rechenpfad |
| **NP-F10** | **Einheiten im Profil, Umrechnung bei der Übernahme:** Außenluft als `1/h` oder `m³/(h·m²)` (Feld mit Einheit); bei `m³/(h·m²)` wird mit der lichten Höhe des Ziels in `1/h` umgerechnet — ohne Höhe wird die Lüftung benannt nicht gesetzt. Geräte und Beleuchtung in W/m², Personen in m² je Person und W je Person | Die Normen geben flächenbezogene Werte; die Kalender rechnen in 1/h und Watt |
| **NP-F11** | **Grenzen** sind die der Größen (`Konditionierungsgroessen.Min/Max`); ein Kennwert außerhalb wird beim Speichern benannt abgelehnt, beim CSV-Import zeilenweise gemeldet und nicht übernommen | Eine Quelle für Grenzen |
| **NP-F12** | **Die Zuordnung ist eine Tabelle** mit Art (`DIN_NUMMER`, `IFC_KLASSE`, `HOTTCAD_RAUMTYP`), Schlüssel (Text, ganzer Vergleich, getrimmt, ohne Unterschied der Schreibung) und Profil; je (Art, Schlüssel) höchstens eine Zeile. Die heutigen Tabellen in `Din18599Nutzung` und `Zonenplan.NutzungAusKlasse` werden zur **Vorgabe im Code** — eine Quelle, aus der der Schemaschritt sät und auf die der Leser ohne Tabelle zurückfällt (Muster `NutzungsprofilZuordnung.VORGABE`) | Änderbar (E90), ohne Teiltextvergleich, wie V32 der Pufferauslegung |
| **NP-F13** | **Leere Profile werden übersprungen:** Führt die Zuordnung auf ein Profil ohne einen einzigen Kennwert (etwa ein DIN-Profil ohne Werte), wird die Zone mit dem Profilnamen vorbelegt, bekommt aber keinen Kalender; die Herleitungszeile nennt „Profil ohne Werte“ | Die DIN-Kategorie ist ausgeliefert leer; der Import darf nichts vortäuschen |
| **NP-F14** | **Am Ziel nur Kopien:** Übernehmen schreibt Kalenderkopien und den Profilnamen als Text (`„Einzelbüro · DIN V 18599-10 Nr. 1“`), nie eine ID; spätere Änderungen am Profil erreichen kein Ziel | KP1b, Statusdatei iOS |
| **NP-F15** | **`Nutzung` wird Text:** `Tab_Konditionierungskalender.Nutzung` und `Tab_Konditionierungsvorlage_STAMM.Nutzung` verlieren den `CHECK` auf die vier Kennungen und bekommen `CHECK (Nutzung IS NULL OR length(Nutzung) BETWEEN 1 AND 120)`. Vorhandene Werte bleiben **unverändert** (keine Umschreibung `BUERO` → „Büro“); die Anzeige übersetzt die vier alten Kennungen weiter über die Ressourcen. Die Pufferauslegung findet ihr Profil weiter über `Z_Nutzungsprofil` (Quelle `KONDITIONIERUNG`); der Schritt sät dort die Namen der EPOS-Muster mit derselben Zuordnung wie die Kennungen | Ergebnisneutral für Bestand und Pufferauslegung (B6); kein Datenumbau in den Kalendern der Referenzprojekte |
| **NP-F16** | **Rangfolge beim Import unverändert:** Ganglinie der Datei vor DIN-Nutzungsprofil der Datei vor **Profil aus der Zuordnung** (vorher: ausgelieferte Vorlage), je Größe. Wird die Zone von Hand auf ein anderes Profil gestellt, ersetzt dieses nur die Größen, die nicht aus der Datei kommen | Datenaustauschkonzept 16.3; Dateiwerte sind genauer als jedes Profil Einzonenweg der Projektdatei (Anwender 06.10.2026): kein Profil automatisch; Zuweisung über „Nutzungsprofil übernehmen…“ im Gebäudeeditor, der Import zeigt den Vorschlag aus der DIN-Nummer ohne Vorbelegung. |
| **NP-F17** | **Übernahme auf ein Ziel mit Kalendern** folgt P12: Rückfrage, dann ersetzt das Profil je Größe nur den Matrixbereich; eigene Perioden und Ausnahmetage des Ziels bleiben | Eine Regel für Vorlage und Profil |
| **NP-F18** | **Nennwerte** setzt die Übernahme nur nach Q39 und Q40 und nur, wenn das Profil den Kennwert trägt und das Ziel eine Fläche hat; die Rückfrage nennt Wert und Herleitung (`8 W/m² × 120 m² = 960 W`). Die EPOS-Muster tragen keinen Nennwert (bitgleich zu heute). Bringt das Profil einen Personennennwert, bleibt der Gerätewert, wie die Übernahme ihn setzt: P1 gilt nicht für die Übernahme eines Profils mit eigenem Personennennwert (E93) | E54 gilt für Vorlagen; ein Profil ist ein vollständiger Parametersatz |
| **NP-F19** | **Eigene Profile und Kategorien** lassen sich anlegen, umbenennen, duplizieren, löschen; ausgelieferte nur duplizieren. Ein ausgeliefertes DIN-Profil wird durch **Duplizieren in eine eigene Kategorie** mit Werten befüllt; die Zuordnung lässt sich dann auf das Duplikat stellen. Löschen eines Profils, auf das die Zuordnung zeigt, fragt nach und setzt die Zuordnung auf „keine“ | Ausgelieferte Zeilen bleiben unberührt und vom Katalogabgleich ersetzbar |
| **NP-F20** | **Namensregel:** neutrale Nutzungsnamen ohne Hersteller- und Produktdaten; Doppelname je Kategorie benannt abgelehnt; `WikiProduktdatenWacheTests` und eine neue Katalogwache halten die Saat | Regel der Kataloge und des Wikis |
| **NP-F21** | **Auslieferungsvorlage:** `Werkzeuge/Auslieferungsvorlage` behält die ausgelieferten Zeilen (`ReadOnly = 1`) und entfernt eigene Kategorien, Profile und Zuordnungen des Anwenders in jedem Modus; der Prüfbericht zählt je Kategorie und meldet jeden Kennwert in einer Normkategorie als Fehler | Keine Normwerte in der Auslieferung, auch nicht aus der Datenbank des Entwicklers |
| **NP-F22** | **Kein Menüpunkt:** Das Blatt „Nutzungsprofile“ öffnet aus Gebäudeeditor, Zonenbaum und Zonendialog, auf beiden Plattformen | Wie die Vorlagenverwaltung (E56) |
| **NP-F23** | **Der Zonenplan hält das Profil als Kennung des Arbeitsstands**, nicht als Text: Die Nutzung einer Planzone wird ein Verweis auf das Profil im Arbeitsstand, beim Speichern in Name und Kopien aufgelöst. Der erneute Import derselben Datei findet die Nutzung über `Tab_Zone.Nutzungsprofil` (Q41) bzw. die Kalendernutzung wieder; ein Name, zu dem kein Profil mehr passt, erscheint als „(nicht im Katalog)“ und bleibt | Der Plan arbeitet mit Profilen; am Ziel liegt Text (NP-F14) |
| **NP-F24** | **Keine Rückwirkung auf die Referenzbasis:** Kein Referenzprojekt bekommt neue Kalender; der Schemaschritt baut die Kalendertabelle zeilengleich neu; der Referenzlauf ist byte-gleich | Die Einfrierregeln „gesäte Konditionierungsdaten“ und „gesäte Zonendaten“ werden nicht berührt |

---

## 4. Datenmodell

### 4.1 Tabellen

Alle `STRICT`, Boolean-Spalten als 0/1 mit `CHECK (spalte IN (0,1))`, Beziehungen über IDs, keine
Access-Schreibweisen ([BETRIEB_SQLITE](BETRIEB_SQLITE.md) Abschnitt 6).

**`Tab_Raumnutzungskatalog`** — die Kategorie: `ID`; `Bezeichner` TEXT NOT NULL (1–80, eindeutig ohne Unterschied
der Schreibung); `Art` TEXT NOT NULL `CHECK (Art IN ('EPOS_MUSTER','DIN_V_18599_10','SIA_2024','VDI_2078','EIGEN'))`;
`Beschreibung` TEXT (≤ 400); `Quellenhinweis` TEXT (≤ 400; Norm, Ausgabe, Lizenzhinweis); `ReadOnly` INTEGER NOT NULL
DEFAULT 0; `Reihenfolge` INTEGER.

**`Tab_Raumnutzungsprofil`** — das Profil: `ID`; `ID_Katalog` INTEGER NOT NULL → `Tab_Raumnutzungskatalog.ID`;
`Nummer` TEXT (NP-F5); `Bezeichner` TEXT NOT NULL (1–80, eindeutig je Katalog); `Beschreibung` TEXT; `ReadOnly`;
dazu die Kennwerte, jeder nullbar (NP-F6):

| Gruppe | Spalten | Einheit, Grenzen |
|---|---|---|
| Zeiten | `Nutzung_Von`, `Nutzung_Bis`, `Betrieb_Von`, `Betrieb_Bis` | Stunde 0…24; Betrieb leer = wie Nutzung |
| Tage | `Nutzungstage_Woche` (sieben Zeichen, Mo bis So, etwa `1111100`), `Nutzungstage_Jahr`, `Feiertage_Wie_Sonntag` | Text aus sieben Ziffern 0/1; `Nutzungstage_Jahr` abgeleitet aus Wochenmuster, Feiertagen und Ferien (E93), keine Eingabe — die Spalte bleibt nullbar und wird nicht beschrieben; 0/1 |
| Heizen | `Heiz_Soll`, `Heiz_Soll_Ausserhalb`, `Heiz_Aus_Ausserhalb` | °C in den Grenzen der Heizspalte; 0/1 |
| Kühlen | `Kuehl_Soll`, `Kuehl_Soll_Ausserhalb`, `Kuehl_Aus_Ausserhalb` | °C in den Grenzen der Kühlspalte; 0/1 |
| Lüftung | `Aussenluft`, `Aussenluft_Einheit`, `Aussenluft_Ausserhalb` | `'1/h'` oder `'m3/hm2'` (NP-F10) |
| Personen | `Personen_Flaeche`, `Personen_Waerme`, `Personen_Anteil`, `Personen_Anteil_Ausserhalb` | m²/Person, W/Person, 0…1 |
| Geräte | `Geraete_Leistung`, `Geraete_Anteil`, `Geraete_Anteil_Ausserhalb` | W/m², 0…1 |
| Beleuchtung | `Beleuchtung_Leistung`, `Beleuchtung_Anteil` | W/m², 0…1 (Q38) |

**`Tab_Raumnutzungszeile`** — das Zeilenbild (NP-F7): `ID`; `ID_Profil`; `Groesse`; `Zeile`; `Wert`; `Aus`; `Von`;
`Bis`; `Bedingt_K` — dieselben Spalten und Prüfklauseln wie `Tab_Konditionierungsvorgabe` ohne die Zeilen `NENNWERT`
und `SAISON`; eindeutig je (Profil, Größe, Zeile). Die Feiertagsregel „wie Sonntag“ trägt das Profil in
`Feiertage_Wie_Sonntag`.

**`Tab_Raumnutzungsstunden`** — Stundenprofile (NP-F9): `ID`; `ID_Profil`; `Groesse`; `Tagesart` TEXT
`CHECK (Tagesart IN ('WERKTAG','FREI'))`; `Werte` TEXT — 24 Zahlen mit Semikolon, invariant (Muster `WQ_Wochenwerte`);
eindeutig je (Profil, Größe, Tagesart).

**`Tab_Raumnutzungszuordnung`** — die Zuordnung (NP-F12): `ID`; `Art` TEXT
`CHECK (Art IN ('DIN_NUMMER','IFC_KLASSE','HOTTCAD_RAUMTYP'))`; `Schluessel` TEXT NOT NULL (1–80); `ID_Profil`
INTEGER → `Tab_Raumnutzungsprofil.ID` (NULL = „keine“, damit sich eine ausgelieferte Zeile bewusst abschalten lässt);
`ReadOnly`; `UNIQUE (Art, Schluessel COLLATE NOCASE)`.

### 4.2 Änderungen an Bestandstabellen

| Tabelle | Änderung | Weg |
|---|---|---|
| `Tab_Konditionierungskalender` | `CHECK` der Spalte `Nutzung` wird Längenprüfung (NP-F15) | Tabellenneubau nach [ADR-001](ADR-001_Schema-Ausrollung.md): neue Tabelle, Zeilen gleich kopieren, alte entfernen, umbenennen, Indizes und abhängige Sichten neu; Zeilenzahl und Prüfsumme vor und nach dem Schritt im Nachweis |
| `Tab_Konditionierungsvorlage_STAMM` | ebenso | ebenso |
| `Tab_Zone` | Textspalte `Nutzungsprofil` (nur bei Ja zu Q41) | `ADD COLUMN`, Sichtneubau der Zonensichten, Kopierwege der Zone tragen die Spalte mit |
| `Z_Nutzungsprofil` | Saat der Musternamen unter Quelle `KONDITIONIERUNG` (NP-F15) | `INSERT OR IGNORE` |

**Ein Schemaschritt** (Papiername **NP-S1**); seine Nummer wird vor dem Bau in der Zeile „Schemaschritt angemeldet“
der [Statusdatei](Status_iOS_Migration.md) angemeldet und hängt über `+ 1` an der Vorgängerklasse. Leser wie bei
jedem Schritt: Schalenmigration, `Werkzeuge/Testdatenbankschema`, Testvorrichtung, Paketanhebung.

### 4.3 Der Generator

`Raumnutzungsgenerator` liefert je Größe die Vorgabezeilen; `Konditionierungsarbeit` legt daraus wie bei einer
Vorlage den Kalender an (Ferienzeiträume des Ziels, P12). Reihenfolge je Größe: **Zeilenbild** (NP-F7) →
**Stundenprofil** (NP-F9) → **Kennwerte** → nichts (NP-F6).

| Größe | Zeile Tag (im Zeitfenster) | Zeile Nacht (außerhalb, mit Zeiten) | Zeile Wochenende (nutzungsfreie Tage) | Ferien |
|---|---|---|---|---|
| Heizen | `Heiz_Soll` im Betriebsfenster | `Heiz_Soll_Ausserhalb` bzw. „aus“ | wie außerhalb | wie außerhalb |
| Kühlen | `Kuehl_Soll` im Betriebsfenster | `Kuehl_Soll_Ausserhalb` bzw. „aus“ | wie außerhalb | wie außerhalb |
| Lüftung | `Aussenluft` (umgerechnet) im Betriebsfenster | `Aussenluft_Ausserhalb` | wie außerhalb | wie außerhalb |
| Geräte | `Geraete_Anteil` (leer = 1) im Nutzungsfenster | `Geraete_Anteil_Ausserhalb` | wie außerhalb | wie außerhalb |
| Personen | `Personen_Anteil` (leer = 1) im Nutzungsfenster | `Personen_Anteil_Ausserhalb` (leer = 0) | wie außerhalb | wie außerhalb |

Ein Fenster 0–24 an allen sieben Tagen erzeugt nur die Zeile Tag. Nutzungsfreie Tage sind die Nullen in
`Nutzungstage_Woche`; Samstag und Sonntag zugleich frei ergibt die Zeile Wochenende; jeder andere freie Tag
(ein freier Werktag, ein freier Samstag allein) erzwingt einen angelegten Kalender mit Standardwoche, weil die Matrix
nur das Wochenende kennt. `Feiertage_Wie_Sonntag = 1` setzt die neun bundeseinheitlichen Feiertage als Regel wie
heute bei Büro und Schule. Die Nennwerte folgen Q39 und Q40.

**Nachweis der Bitgleichheit** (NP1): Für die Muster Wohnen, Büro und Schule ergibt „Profil übernehmen“ an einem
leeren Ziel je Größe dieselben Zeilen in `Tab_Konditionierungsvorgabe`, dieselbe Woche, dieselben Perioden und
Feiertagsregeln und dieselbe Reihe von 8 760 Werten wie „Vorlage übernehmen“ mit der Vorlage gleicher Nutzung —
geprüft je Größe und für Gebäude, Zone und Katalogbau. Nur `Bemerkung` (Herkunftstext) und `Nutzung` (Name statt
Kennung, NP-F15) unterscheiden sich, und genau das prüft der Test mit.

### 4.4 Einbau in den Kern

Was der Schritt NP-S1 und die Stufen NP1 und NP2 an den Lesern und Schreibern der Nutzung ändern; der Rechenweg
bleibt unberührt (NP-F24).

| Stelle | heute | künftig | Stufe |
|---|---|---|---|
| `RaumnutzungCtrl` (neu, `EPOS.Kern/Controller/`) | — | Kategorien und Profile lesen; anlegen, duplizieren (auch in eine andere Kategorie), umbenennen, löschen mit Rückfrage zur Zuordnung (NP-F19); Kennwerte, Zeilenbild und Stundenprofil schreiben; `ProfilUebernehmen` an Gebäude, Zone und Katalogbau über `Raumnutzungsgenerator` und `Konditionierungsarbeit`; Zuordnung lesen und pflegen | NP1 |
| `DbWerte.KOND_NUTZUNG_*`, `KOND_NUTZUNGEN`, `Zonenplan.NUTZUNGEN` | Auswahl- und Prüfliste der vier Kennungen | als Auswahl- und Prüfliste ersetzt durch den Katalog; die vier Kennungen bleiben als Konstanten, weil Bestandswerte sie tragen (Anzeige über die Ressourcen, Pufferauslegung, NP-F15) | NP1, NP2 |
| `KonditionierungCtrl` (`NutzungDerHerkunft`, `NutzungSetzen`), `KonditionierungsvorlageCtrl` | Kennung aus der festen Liste | Text (Profilname oder alte Kennung), Längenprüfung wie im Schema | NP1 |
| `KonditionierungNutzung` (Aufzählung in `EPOS.UI/Dialoge/Bedarf/KonditionierungDaten.cs`), Hüllen `KonditionierungHuelle`, `GebaeudeImportZonen` | vier feste Werte | Text mit Vorschlägen aus dem Katalog bzw. Profilverweis des Arbeitsstands (NP-F23) | NP2, NP3 |
| `Zonenplan.NutzungAusKlasse`, `Din18599Nutzung` | feste Tabellen im Code | Vorgabe im Code, Leser aus `Tab_Raumnutzungszuordnung` (NP-F12) | NP2 |
| `GebaeudeZonierung.RAUMTYP_KLASSE` | Raumtyp der Datei (ohne `mrt`) → Nutzungsklasse | bleibt im Code; eine Zeile der Art `HOTTCAD_RAUMTYP` geht ihr vor | NP2 |
| `IfcAbbildBauer` (`EPOS_Zone.Nutzung`) | nur übernommen, wenn der Wert eine der Kennungen aus `Zonenplan.NUTZUNGEN` ist | Name eines Profils → Profil; alte Kennung → EPOS-Muster gleicher Nutzung; sonst „(nicht im Katalog)“, der Text bleibt (NP-F23) | NP2 |
| IFC-Export (`IfcKonditionierungssatz`), IDS `Setup/Vorlage/EPOS_Export.ids` | `Nutzung` als Text der Kalendernutzung | unverändert: die Kalendernutzung ist dann der Profilname; das IDS führt `Nutzung` als `IFCLABEL` ohne Wertvorgabe und bleibt, wie es ist | — |
| `PufferAuslegungCtrl`, `Nutzungsprofil.Ableiten` | Kennung → Pufferprofil über `Z_Nutzungsprofil` (Quelle `KONDITIONIERUNG`) | dieselbe Tabelle, gesät um die Musternamen (NP-F15) | NP1 |
| `Vdi6007Rechenweg` | Hinweis `SIMENG_KOND_NUTZUNG_LEER` | unverändert; der Löser liest nur die Kalender | — |
| Katalogpaket (`Katalogpaket.json`, Katalogabgleich der Katalogfassung), `Werkzeuge/Auslieferungsvorlage` | — | die ausgelieferten Zeilen der fünf Tabellen gehören zum Katalogpaket; ein Projektpaket trägt keine Profile, weil am Ziel nur Kopien liegen (NP-F14, NP-F21) | NP1 |

---

## 5. Auslieferung

### 5.1 Kategorie „EPOS-Muster“ (`EPOS_MUSTER`, ReadOnly)

Wohnen, Büro und Schule tragen die Werte aus 2.3 (als Kennwerte, wo der Generator sie bitgleich ergibt, sonst als
Zeilenbild). **Sonstige** trägt keinen Kennwert (nur der Name an der Zone, wie die heutige Kennung `SONSTIGE`). Neu,
**als Vorschlag mit runden Zahlen** (Q43), Herkunft „EPOS-Muster, weder Norm- noch Messwerte“, ohne Nennwerte:

| Muster | Zeiten, Tage | Heizen | Kühlen | Lüftung | Geräte | Personen |
|---|---|---|---|---|---|---|
| Sport | 8–22 Uhr, täglich | 18 °C, außerhalb 15 °C | aus | außerhalb 0,1 1/h | 100 %, außerhalb 10 % | 100 %, außerhalb 0 % |
| Gastronomie | 10–23 Uhr, Di–So | 20 °C, außerhalb 16 °C | 26 °C, außerhalb aus | außerhalb 0,1 1/h | 100 %, außerhalb 20 % | 100 %, außerhalb 0 % |
| Lager | 7–16 Uhr, Mo–Fr | 12 °C durchgehend | aus | — | 10 % durchgehend | 10 %, außerhalb 0 % |
| Verkehr | 7–18 Uhr, Mo–Fr | 18 °C, außerhalb 16 °C | aus | — | 10 % durchgehend | 0 % |
| Technik | durchgehend | 15 °C durchgehend | aus | — | 100 % durchgehend | 0 % |

Lager und Verkehr nehmen die Feiertage wie Sonntag wie die Werktagsmuster Büro und Schule (`Feiertage_Wie_Sonntag` = 1, E93); der Wert ist im Blatt änderbar.

Die fünf Muster decken das Sportheim der HottCAD-Probe (Halle, Gastraum, Lager, Flure, Technik) und die
IFC-Nutzungsklassen ohne Vorbelegung (B2). Die 14 Konditionierungsvorlagen bleiben daneben bestehen; aus den neuen
Mustern entstehen keine neuen Vorlagen (Q45).

### 5.2 Kategorie „DIN/TS 18599-10“ (`DIN_V_18599_10`, ReadOnly, ohne Werte)

Ausgeliefert werden **Nummer und Name** der 43 Nutzungen der DIN/TS 18599-10:2025-10, Tabelle 6 (E96) — Tatsachen ohne
Werte —, kein Kennwert, kein Zeilenbild, kein Stundenprofil (E90). Die Technische Spezifikation ersetzt
DIN V 18599-10:2018-09 und nummeriert ab 22 durchgehend ganzzahlig (2018: 22.1 bis 22.3 und 23 bis 41). Für EPOS gelten
aus ihr nur die für die Simulation relevanten Größen — Zeiten, Sollwerte und Absenkung, Außenluft, Personen, Geräte und
ihre Anteile —, keine Beleuchtung und keine weiteren Normgrößen; die Werte trägt der Anwender aus seiner lizenzierten
Ausgabe ein oder importiert sie (CSV, 6.4), im Repositorium stehen sie nicht. `Quellenhinweis`: „DIN/TS 18599-10:2025-10,
Energetische Bewertung von Gebäuden – Teil 10: Nutzungsrandbedingungen, Klimadaten; ersetzt DIN V 18599-10:2018-09. Werte
trägt der Anwender aus seiner lizenzierten Ausgabe ein oder importiert sie.“ Die Art bleibt `DIN_V_18599_10`.

| Nr. | Name (Tabelle 6) | Nr. 2018 |
|---|---|---|
| 1 | Einzelbüro | 1 |
| 2 | Gruppenbüro (zwei bis sechs Arbeitsplätze) | 2 |
| 3 | Großraumbüro (ab sieben Arbeitsplätze) | 3 |
| 4 | Besprechung, Sitzung, Seminar | 4 |
| 5 | Schalterhalle | 5 |
| 6 | Einzelhandel/Kaufhaus | 6 |
| 7 | Einzelhandel/Kaufhaus (Lebensmittelabteilung mit Kühlprodukten) | 7 |
| 8 | Klassenzimmer (Schule), Gruppenraum (Kindergarten) | 8 |
| 9 | Hörsaal, Auditorium | 9 |
| 10 | Bettenzimmer | 10 |
| 11 | Hotelzimmer | 11 |
| 12 | Kantine | 12 |
| 13 | Restaurant | 13 |
| 14 | Küchen in Nichtwohngebäuden | 14 |
| 15 | Küche – Vorbereitung, Lager | 15 |
| 16 | WC und Sanitärräume in Nichtwohngebäuden | 16 |
| 17 | Sonstige Aufenthaltsräume | 17 |
| 18 | Nebenflächen (ohne Aufenthaltsräume) | 18 |
| 19 | Verkehrsflächen | 19 |
| 20 | Lager, Technik, Archiv | 20 |
| 21 | Rechenzentrum | 21 |
| 22 | Gewerbliche und industrielle Hallen – schwere Arbeit, stehende Tätigkeit | 22.1 |
| 23 | Gewerbliche und industrielle Hallen – mittelschwere Arbeit | 22.2 |
| 24 | Gewerbliche und industrielle Hallen – leichte Arbeit | 22.3 |
| 25 | Zuschauerbereich (Theater und Veranstaltungsbauten) | 23 |
| 26 | Foyer (Theater und Veranstaltungsbauten) | 24 |
| 27 | Bühne (Theater und Veranstaltungsbauten) | 25 |
| 28 | Messe/Kongress | 26 |
| 29 | Ausstellungsräume und Museum mit konservatorischen Anforderungen | 27 |
| 30 | Bibliothek – Lesesaal | 28 |
| 31 | Bibliothek – Freihandbereich | 29 |
| 32 | Bibliothek – Magazin und Depot | 30 |
| 33 | Turnhalle (ohne Zuschauerbereich) | 31 |
| 34 | Parkhäuser (Büro- und Privatnutzung) | 32 |
| 35 | Parkhäuser (öffentliche Nutzung) | 33 |
| 36 | Saunabereich | 34 |
| 37 | Fitnessraum | 35 |
| 38 | Labor | 36 |
| 39 | Untersuchungs- und Behandlungsräume | 37 |
| 40 | Spezialpflegebereiche | 38 |
| 41 | Flure des allgemeinen Pflegebereichs | 39 |
| 42 | Arztpraxen und Therapeutische Praxen | 40 |
| 43 | Lagerhallen, Logistikhallen | 41 |

Namen nach Tabelle 6 mit großem Anfangsbuchstaben; 23 und 24 tragen den Titel aus Anhang A, weil der Name der Tabelle 6
länger als 80 Zeichen ist. **Wohngebäude** (Tabelle 5, Einfamilien- und Mehrfamilienhaus) tragen keine Nummer und stehen
nicht als Profil in der Kategorie; sie führen über das Muster Wohnen und die Werte 70 und 71 der Projektdatei (5.4).
44 bis 47 sind in keiner Ausgabe eine Nummer der Norm (B10). Schemaschritt 190 (`RaumnutzungDinTsSchema`) stellt eine
Datenbank mit der Saat 2018 um: Kategorie umbenannt, die 24 ausgelieferten Profile auf Nummer und Namen 2025 (Ids
bleiben, eine Zuordnung oder ein Duplikat des Anwenders zeigt danach auf dieselbe Nutzung), die übrigen 19 ohne Werte
angelegt; Zeilen des Anwenders und die Zuordnung bleiben, wie sie sind.

### 5.3 Kategorien „SIA 2024“ und „VDI 2078“ (leer)

Je eine Kategorie ohne Profil, mit `Quellenhinweis` (Merkblatt bzw. Richtlinie, lizenzpflichtig) und Beschreibung
der Struktur: SIA 2024 nummeriert Raumnutzungen mit Punkten und gibt Tagesverläufe — sie füllt die Stundenprofile
(NP-F9); VDI 2078 gibt Nutzungszeit und innere Lasten. Profile legt der Anwender an oder importiert sie.

### 5.4 Zuordnung

| Art | Schlüssel | Profil (ausgeliefert) | Quelle |
|---|---|---|---|
| `DIN_NUMMER` | 1, 2, 3, 4, 5 | EPOS-Muster Büro | B3, unverändert |
| `DIN_NUMMER` | 8, 9, 28, 29 | EPOS-Muster Schule | B3, unverändert |
| `DIN_NUMMER` | 70, 71 | EPOS-Muster Wohnen | B3, unverändert |
| `IFC_KLASSE` | Buero | EPOS-Muster Büro | B2, unverändert |
| `IFC_KLASSE` | Wohnen, Schlafen, Kueche | EPOS-Muster Wohnen | B2, unverändert |
| `IFC_KLASSE` | Sport, Gastronomie, Lager, Verkehr, Technik | die gleichnamigen neuen Muster | **nur bei Ja zu Q42** |
| `DIN_NUMMER` | 31, 35 → Sport; 12, 13 → Gastronomie; 20, 41 → Lager; 19 → Verkehr | neue Muster | **nur bei Ja zu Q42** (19 und 20 erst nach Bestätigung aus 5.2) |
| `HOTTCAD_RAUMTYP` | — | keine Zeile | Raumtyp-Codes (`mrt…`) sind im Befund HottCAD belegt; ihre Nutzungsklasse gibt weiter `GebaeudeZonierung.RAUMTYP_KLASSE` im Code (4.4), eine Zeile hier geht ihr vor |

Die DIN-Nummern zeigen ausgeliefert auf **EPOS-Muster**, nicht auf die leeren DIN-Profile (NP-F13); wer Normwerte
eingetragen hat, stellt die Zuordnung auf sein Duplikat um (NP-F19).

**Zählung der Schlüssel `DIN_NUMMER`.** Der Schlüssel ist die Nummer der HottCAD-Projektdatei
(`PdProfileUsage.ProfileUsageType`), nicht die Nummer der Kategorie DIN/TS 18599-10. Er zählt wie ausgeliefert nach
DIN V 18599-10:2018-09: 28, 29 (Bibliothek) → Schule, 31, 35 (Turnhalle, Fitnessraum) → Sport, 41 (Lagerhallen) → Lager;
1 bis 13 sind von der Neunummerierung nicht berührt. Ob HottCAD ab 22 wie 2018 oder durchgehend wie 2025 zählt (dann
30, 31 → Schule, 33, 37 → Sport, 43 → Lager), belegt keine Datei im Repositorium: Die Importproben entstehen synthetisch
mit den Nummern 1, 20 und 71 und runden Werten, die Projektdateien des Anwenders liegen nur lokal. Entscheiden kann ein
Fingerabdruck an einer echten Datei — Sollwert, Absenkung, Außenluft und Personenwärme einer Zone mit einer Nummer ab 22
gegen die eigene Ausgabe des Anwenders. Bis dahin bleiben Saat und `Din18599Nutzung` bei 2018, und eine Nummer ab 22
der Projektdatei trifft im Katalog DIN/TS auf eine andere Nutzung (etwa 28: Lesesaal in der Datei, Messe/Kongress im
Katalog). 19 und 20 sind in beiden Ausgaben bestätigt (Verkehrsflächen; Lager, Technik, Archiv) und bleiben ohne Zeile.

---

## 6. Oberfläche

### 6.1 Das Blatt „Nutzungsprofile“

Razor-Komponente in `EPOS.UI/Dialoge/Bedarf/`, Hülle in `EPOS.UI.Daten/Bedarf/`, Datenbankseite in
`RaumnutzungCtrl` (Kern), Texte in `MyResource.Resource.*` in beiden Sprachen; Blatt im Katalogeditor wie die
Vorlagenverwaltung, auf beiden Plattformen (NP-F22). Drei Bereiche:

1. **Katalogbaum** links: Kategorien (Schloss bei ausgelieferten, Art als Kennzeichen) mit ihren Profilen, sortiert
   nach Nummer, dann Name; Suche über Nummer und Name; „Neue Kategorie“, „Neues Profil“, „Duplizieren“ (auch in eine
   andere Kategorie), „Umbenennen“, „Löschen“ (NP-F19). Lange Listen über die vorhandene `Katalogliste`
   (Rasterprobe ziehen, wenn ihre Regeln berührt werden).
2. **Profileditor** rechts: Kopf (Kategorie, Nummer, Name, Beschreibung), die Kennwertgruppen aus 4.1 als Felder mit
   Einheit und Grenzen, je Größe ein Umschalter „aus Kennwerten | Zeilenbild | Stundenprofil“ und die **Vorschau**:
   Woche und Teppichbild je Größe an einem neutralen Vorschauziel (Heizen 20 °C, Kühlen 26 °C, Luftwechsel nach Vorgabe, eine Person, Geräte 100 W, Ferien 1. bis 14. August) — dieselben Bausteine wie die Kalenderkarte. Das Stundenprofil kennt zwei Tagesarten: Werktag und nutzungsfreier Tag (Anwender 06.10.2026); Werte lassen sich aus der Zwischenablage einfügen. Eine
   Lesezeile „Nutzungstage im Jahr: … (aus Wochenmuster und Feiertagen)“ (NP-F8, E93). Ausgelieferte Profile sind schreibgeschützt; DIN-Profile zeigen
   „ohne Werte — Duplizieren, um Werte einzutragen“.
3. **Zuordnung** als Reiter: Tabelle Art, Schlüssel, Profil (Auswahl gruppiert nach Kategorie, dazu „keine“),
   ausgelieferte Zeilen mit Schloss und „Zurücksetzen“; neue Zeilen frei.

**CSV-Import** (NP4) im Kopf des Blatts („CSV…“): wählt Datei und Zielkategorie (eine eigene), zeigt eine Vorschau mit Zeilenmeldungen
(NP-F11), schreibt erst nach „Übernehmen“; derselbe Zugang exportiert. „Aus Projektdatei…“ im Blattkopf übernimmt die Profile einer
HottCAD-Projektdatei in eine eigene Kategorie (Ergänzen oder Ersetzen; die Beleuchtung bleibt leer, die Beleuchtungsstärke steht in der
Beschreibung). Format in 6.4.

### 6.2 Zugänge

| Ort | Bedienung |
|---|---|
| Gebäudeeditor, Reiter Konditionierung (`GebaeudeKatalogDialog.razor` :554–564) | „Nutzungsprofil übernehmen…“ (Liste gruppiert nach Kategorie, Vorschau, Rückfrage nach P12, Nennwertzeile nach Q39 und Q40) und „Nutzungsprofile verwalten…“ (öffnet das Blatt, Platz wie :758–767) |
| Zonenbaum des Imports (`GebaeudeImportDialog.razor` :403–449) | Das `<select>` je Zone führt die Profile gruppiert nach Kategorie (`<optgroup>`), dazu „keine“; unter der Zone eine **Herleitungszeile** („Büro · EPOS-Muster — aus IFC-Klasse Buero“ bzw. „aus DIN-Nr. 1 der Projektdatei; Heizen und Personen aus der Datei“) mit den Kennwerten in Kurzform; „Nutzungsprofile…“ öffnet das Blatt als Überlagerung, nach dem Schließen liest der Plan Katalog und Zuordnung neu; „Nutzungsprofile der Datei übernehmen…“ legt die Profile der Projektdatei in einer eigenen Kategorie an. Im Einzonenweg steht nur der Hinweis „Vorschlag aus DIN-Nr. …, zuweisbar im Gebäudeeditor“, gesetzt wird nichts |
| Zonendialog (`ZonenDialog.razor`, Konditionierungsreiter :137) | „Nutzungsprofil übernehmen…“ wie im Gebäudeeditor, auch nach dem Import; die Kopfzeile nennt das zuletzt übernommene Profil (aus `Tab_Zone.Nutzungsprofil` bzw. der Kalendernutzung) |
| Kalenderkarte | „Als Vorlage speichern…“ fragt die Nutzung künftig als Text mit Vorschlägen aus dem Katalog (NP-F15) |

### 6.3 Assistent, Hilfe, Tests

- **KI-Feldkarten**: die Felder des Profileditors und der Zuordnung als Feldkarte nach dem Muster
  `KiKonditionierungsfelder.cs`; die KI-Sicht der Zone nennt das Profil.
- **Wiki** (NP4): neue Seite „Nutzungsprofile“ (Programm Dokumentation), Nachzug der Quellen „Gebäudeimport“
  (Zonenbaum, Herleitungszeile), „Mehrzonenmodell“, „Gebäude“ und „Konditionierung“ (Vorlage neben Profil); nur die
  Funktion, wie sie ist; keine Normwerte und keine Produktdaten im Wiki
  ([Konzept Hilfesystem](Konzept_Hilfesystem_Wikidokumentation.md) 13). Logbuch-Entwurf, ein Satz: „Nutzungsprofile
  sind ein eigener Katalog mit Kategorien; Gebäude und Zonen übernehmen ihre Kalender aus einem Profil, und der
  Gebäudeimport ordnet Nutzungen über änderbare Zuordnungen zu.“ Die Versionsnummer erfragt die Sitzung beim
  Anwender; hochgeladen wird gebündelt.
- **bunit**: Blatt, Editor, Zuordnung, Zonenbaum mit Gruppen und Herleitung, Rückfragen; **Fensterprobe**, wenn
  Dialogkopf oder Schlussleiste berührt werden.

### 6.4 CSV-Format (NP4)

UTF-8, Semikolon, Dezimalpunkt, eine Kopfzeile mit den Spaltennamen aus 4.1 (`Nummer;Bezeichner;Nutzung_Von;…`),
eine Zeile je Profil; unbekannte Spalten werden benannt ignoriert, fehlende heißen „leer“; eine Spalte `Nutzungstage_Jahr` wird benannt ignoriert — die Zahl ist abgeleitet (E93) und wird nicht exportiert; eine Spalte
`Heiz_Absenkung_K` wird als Alternative zu `Heiz_Soll_Ausserhalb` angenommen und umgerechnet; Stundenprofile als
Spalten `Stunden_<Größe>_<Tagesart>` mit 24 Werten, durch Leerzeichen getrennt; die Größe steht als Kennwort (`HEIZSOLL`, `KUEHLSOLL`,
`LUEFTUNG`, `GERAETE`, `PERSONEN`), die Tagesart ist `WERKTAG` oder `FREI`. Das Zeilenbild steht in Spalten `Zeile_<Größe>_<Zeile>` mit
`<Zeile>` = `TAG`, `NACHT`, `WOCHENENDE`, `FERIEN`; die Zelle trägt den Wert, `aus` oder `-` (nur Zeiten) und die Zusätze `von=`, `bis=`,
`bedingt=`. Export UTF-8 mit BOM (für Tabellenprogramme), Import mit und ohne BOM. Dasselbe Format exportiert ein
Katalog (Rundlauf). Beschrieben auf der Wiki-Seite, mit einer Beispieldatei aus runden Phantasiewerten in den
Testdaten.

---

## 7. Stufen und Aufwand

| Stufe | Inhalt | Aufwand | Abhängigkeit |
|---|---|---|---|
| **NP1** | Schemaschritt NP-S1 (fünf Tabellen, Tabellenneubau zweier Konditionierungstabellen, Saat der Kategorien, Muster, DIN-Nummern und Zuordnung, Saat in `Z_Nutzungsprofil`, bei Ja zu Q41 die Spalte an `Tab_Zone`), `Raumnutzungsprofil`, `Raumnutzungsgenerator`, `RaumnutzungCtrl` (Lesen, Schreiben, Übernehmen), Wachen, Bitgleichheitsnachweis der Muster, Auslieferungsvorlage samt Prüfbericht | 6–9 PT | keine; ergebnisneutral |
| **NP2** | Zuordnung und Import: `Din18599Nutzung` und `NutzungAusKlasse` als Vorgabe im Code mit Leser aus der Tabelle, `Zonenplan` mit Profil statt Kennung (NP-F23), `ZonenplanCtrl.NutzungUebernehmen` und `ProjektdateiUebernehmen` über den Generator (NP-F16), Wiederfinden beim erneuten Import, Hülle `GebaeudeImportZonen` mit Gruppen und Herleitung, Importproben und ihr LIESMICH | 4–6 PT | NP1; **Abstimmung mit der Sitzung IFC-Ganglinie**, die an den Importdateien (`Sqproj*`, `Zonenplan`, `ZonenplanCtrl`) arbeitet — NP2 beginnt erst nach deren Merge oder in verabredeten Dateien |
| **NP3** | Oberfläche: Blatt „Nutzungsprofile“, Profileditor mit Vorschau, Zuordnung, die drei Zugänge, „Als Vorlage speichern“ mit freier Nutzung, Ressourcen in beiden Sprachen, KI-Feldkarten, bunit, Windows-Schale kompiliert | 6–9 PT | NP1, NP2 |
| **NP4** | CSV-Import und -Export, Übernahme der Profile aus einer Projektdatei in einen eigenen Katalog (bei Ja zu Q46), Wiki-Seite und Nachzüge, Hilfe, Logbuch-Entwurf | 3–4 PT | NP3 |
| | **zusammen** | **19–28 PT** | |

Die Referenzbasis bleibt in allen Stufen unverändert. Keine Stufe braucht einen iOS-Lauf (keine `Dienste.*`-Änderung;
der CSV-Import nutzt den vorhandenen Dateidienst).

---

## 8. Prüfung

| Prüfung | Stufe | Was sie hält |
|---|---|---|
| `RaumnutzungskatalogWacheTests` (neu) | NP1 | ausgelieferte Zeilen `ReadOnly`; **kein Kennwert, kein Zeilenbild, kein Stundenprofil in Kategorien der Art DIN, SIA, VDI**; eindeutige Namen je Kategorie; Grenzen; keine Katalognamen von Produkten |
| `RaumnutzungsgeneratorTests` (neu) | NP1 | Tabelle 4.3 je Größe, Fenster über Mitternacht, freie Einzeltage, Feiertage, Einheiten (NP-F10), leere Profile (NP-F6) |
| Bitgleichheit der Muster (neu) | NP1 | 4.3: Profil = Vorlage gleicher Nutzung, je Größe und Ziel |
| Schemafall des Tabellenneubaus | NP1 | Zeilenzahl, Inhalt und Indizes von `Tab_Konditionierungskalender` und `Tab_Konditionierungsvorlage_STAMM` vor und nach dem Schritt gleich; Sichten neu |
| Pufferauslegung | NP1 | `Nutzungsprofil.Ableiten` liefert für alte Kennungen und neue Musternamen dasselbe Pufferprofil (B6, NP-F15) |
| `KonditionierungsvorlagenWacheTests` | NP1 | die 14 Vorlagen unverändert |
| `SqlDialektPruefer` | jede | jede neue SQL-Anweisung |
| Importproben, Fälle zu `ZonenplanCtrl` | NP2 | Vorbelegung über die Zuordnung gleich der heutigen festen Tabellen; Rangfolge Datei vor Profil |
| `ZonenReferenzprojektWacheTests`, `ZonenHeizkreisReferenzprojektWacheTests`, `KonditionierungReferenzprojektWacheTests` | NP1, NP2 | 1051, 1052 und 1054 mit ihren Zonen- und Gebäudekalendern unverändert |
| Referenzlauf gegen die aktuelle Basis | NP1, NP2 | byte-gleich (NP-F24) |
| bunit, Fensterprobe, Rasterprobe bei Bedarf | NP3 | Blatt und Zugänge |
| `AuslieferungsvorlagenWacheTests`, `KatalogpaketAuslieferungWacheTests`, Prüfbericht der Auslieferungsvorlage | NP1 | ausgelieferte Zeilen im Katalogpaket, eigene Zeilen entfernt, kein Kennwert in Normkategorien (NP-F21) |
| `IfcQuelldateienDurchgangTests`, `ZonenimportProbenTests`, Proben des Zonenbaums | NP2 | Import aller Quelldateien mit Zuordnung statt fester Tabellen; `EPOS_Zone.Nutzung` mit Profilname, alter Kennung und unbekanntem Text (4.4) |
| IFC-Rundreise, `IdsWacheTests` | NP2 | exportierte Nutzung = Profilname, erneuter Import findet das Profil wieder (NP-F23); IDS unverändert gültig |
| Windows-Schale auf Linux kompiliert, gestörter Lauf | NP3 | Hüllen und Nähte der Schale; Oberfläche ohne Katalogzeilen (leere Datenbank) benannt statt still |
| `DokumentationLinkWacheTests`, `WikiProduktdatenWacheTests` | NP4 | Wiki-Quellen, Verweise |

---

## 9. Fragen an den Anwender mit Empfehlung

Alle zehn Fragen sind mit **E91** nach Empfehlung entschieden; die Empfehlung ist damit die Festlegung.

| Nr. | Frage | Empfehlung | Was daran hängt |
|---|---|---|---|
| **Q38** | **Beleuchtung:** als Anteil der Gerätelast oder als sechste Größe? | **Anteil der Gerätelast:** `Beleuchtung_Leistung × Beleuchtung_Anteil` wird bei der Übernahme zum Gerätenennwert addiert und folgt dem Zeitverlauf der Geräte; das Profil führt die Beleuchtung getrennt, damit eine sechste Größe später ohne Datenverlust möglich bleibt | Eine sechste Größe berührt Kalender, Matrix, Rechenweg und Ergebnisse — außerhalb dieser Stufe (1.3) |
| **Q39** | **Gerätenennwert aus dem Profil:** Setzt die Übernahme den Nennwert der Geräte aus W/m² × Fläche? | **Ja**, wenn das Profil den Wert trägt und das Ziel eine Fläche hat, mit der Herleitung in der Rückfrage; sonst bleibt der Nennwert des Ziels. Beim Import wie heute bei Dateiwerten | Ohne Nennwert sind Normprofile nur Zeitprofile; der Weg aus der Projektdatei setzt Nennwerte heute schon |
| **Q40** | **Personennennwert:** Setzt die Übernahme die Personen aus Fläche ÷ m² je Person × W je Person? | **Ja, nach derselben Regel wie Q39**, mit `Matrixeingang.PERSON_W` (70 W), wo das Profil keine Wärmeabgabe trägt; die EPOS-Muster bleiben ohne Personennennwert, damit P1 (Gerätenennwert = Gesamtwert minus Personenmittel) für den Bestand gleich bleibt; bringt das Profil einen Personennennwert, gilt P1 bei der Übernahme nicht (E93) | Wirkt auf die inneren Gewinne der übernehmenden Zone; Bestand und Referenzprojekte unberührt |
| **Q41** | **`Tab_Zone.Nutzungsprofil` als Textspalte** (E81 neu bewerten)? | **Ja**: eine nullbare Textspalte mit dem Profilnamen als Kopie, keine ID; sie trägt die Nutzung auch einer unbeheizten Zone ohne Kalender, macht den erneuten Import vollständig und gibt Zonendialog und Bericht eine Stelle. Leere Zonen und der Merker „angelegt“ bleiben ungespeichert (E81) | Ein `ADD COLUMN` im Schritt NP-S1 samt Sichtneubau; Kopierwege der Zone tragen die Spalte mit |
| **Q42** | **Ausgelieferte Zuordnung:** nur die heutigen Paare oder dazu die neuen Muster für IFC-Klassen und DIN-Nummern (5.4)? | **Dazu die neuen Muster**: Sonst erreicht der Import die neuen Muster nie und das Sportheim bleibt ohne Vorbelegung. Die Erweiterung ändert, was ein Import vorbelegt — nie den Rechenweg; die Importproben bekommen ihre Erwartung in NP2 neu | Vorbelegung beim Import für Sport, Gastronomie, Lager, Verkehr, Technik |
| **Q43** | **Werte der neuen EPOS-Muster** wie in 5.1? | **Ja, als Vorschlag**; der Anwender prüft sie am Sportheim vor der Saat in NP1 und kann jede Zahl ändern — es sind runde Muster, keine Normwerte | Saat NP1, Wache |
| **Q44** | **Nutzungstage je Jahr:** nur Vergleichswert oder Ferien daraus erzeugen? | **Keine Ferien daraus** (NP-F8): Der Generator nimmt die Wochentage; die Zahl ist abgeleitet aus Wochenmuster, Feiertagen und Ferien des Ziels (E93), keine Eingabe | Ferienzeiträume bleiben beim Gebäude |
| **Q45** | **Vorlagen je Größe und Profile nebeneinander?** | **Ja, beide bleiben**: Vorlagen für die Arbeit je Kalenderkarte (P11, E57), Profile für die ganze Zone; ein „Profil als Vorlagen speichern“ gibt es nicht — wer Vorlagen will, speichert sie aus der Karte | Keine Doppelpflege in der Auslieferung; die 14 Vorlagen bleiben unverändert |
| **Q46** | **Profile aus einer HottCAD-Projektdatei** in einen eigenen Katalog übernehmen (Nummer, Name und die Werte von `PdProfileUsage`)? | **Ja, in NP4** — die Werte stammen aus der lizenzierten Software des Anwenders und landen nur in seiner Datenbank, nie im Repositorium oder in der Auslieferung (NP-F21) | Der schnellste Weg zu befüllten DIN-Profilen; Wache und Auslieferungsvorlage halten die Grenze |
| **Q47** | **Namen der DIN-Profile ausliefern** (5.2) und die offenen Einzelnamen vom Anwender bestätigen lassen? | **Ja**: Nummer und Name sind Tatsachen ohne Werte; ausgeliefert wird nur, was der Anwender an seiner Ausgabe bestätigt hat, unbestätigte Nummern bleiben bis dahin weg | Umfang der Saat NP1 |

---

## 10. Register

### 10.1 Festlegungen

NP-F1 bis NP-F24 in Kapitel 3 — nach Empfehlung, gültig mit E91 und der Beauftragung von NP1.

### 10.2 Fragen

| Nr. | Gegenstand | fällig vor | Stand |
|---|---|---|---|
| Q38 | Beleuchtung | NP1 | entschieden (E91) |
| Q39 | Gerätenennwert | NP1 | entschieden (E91) |
| Q40 | Personennennwert | NP1 | entschieden (E91) |
| Q41 | `Tab_Zone.Nutzungsprofil` | NP1 (Schemaschritt) | entschieden (E91) |
| Q42 | Zuordnung der neuen Muster | NP1 (Saat) | entschieden (E91) |
| Q43 | Werte der neuen Muster | NP1 (Saat) | entschieden (E91) |
| Q44 | Nutzungstage je Jahr | NP1 | entschieden (E91) |
| Q45 | Vorlagen und Profile nebeneinander | NP1 | entschieden (E91) |
| Q46 | Profile aus der Projektdatei | NP4 | entschieden (E91) |
| Q47 | Namen der DIN-Profile | NP1 (Saat) | entschieden (E91) |

### 10.3 Abgrenzung

Keine Normwerte im Repositorium, in Tests, im Wiki und in der Auslieferung; keine Rückwirkung auf den Rechenweg und
keine neue Referenzbasis; kein Bezug vom Ziel auf den Katalog; keine TWW-, Feuchte- oder Sonnenschutzprofile; keine
sechste Größe in dieser Stufe.

### 10.4 Verweise

- [Status der Gebäudesimulation](Status_Gebaeudesimulation_VDI6007.md) — E81, E90, Stufe NP
- [Konditionierungsprofile](Konzept_Konditionierungsprofile_EPOS-Plan.md) — B15, P4, P11, P12, E54, E56, E57, F22; 3.5, 5.6, 5.7, 7.4
- [Mehrzonenmodell](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) — 2.6, Zonenplan und Zonenplan mit Projektdatei
- [Datenaustausch](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) — 16.3, 16.4
- [Befund HottCAD-Projektdatei](Gebaeudesimulation/2026-10-05_Befund_HottCAD_Projektdatei.md) — `PdProfileUsage`, Raumtypen
- [Grundlagen 1](Grundlagen_1_Normen_Regelwerke_TWW-Zapfprofile.md) — Nutzungszeiten DIN V 18599-10, Lizenzlage
- [ADR-001](ADR-001_Schema-Ausrollung.md) — Schemaschritt und Tabellenneubau
- [BETRIEB_SQLITE](BETRIEB_SQLITE.md) — SQL-Dialekt
- [Konzept Hilfesystem](Konzept_Hilfesystem_Wikidokumentation.md) — Wiki, Logbuch, Produktdaten
- [Status iOS-Migration](Status_iOS_Migration.md) — Zeile „Schemaschritt angemeldet“
- Schemaschritt 176 `KonditionierungNutzungSchema` (Nutzung an der Kalenderkopie), Schemaschritt 178
  `ProzessNutzungSchema` (`Tab_Nutzungsprofil_STAMM`, `Z_Nutzungsprofil` der Pufferauslegung)
- [Projektkontext `CLAUDE.md`](../../CLAUDE.md) — Datenhaltung, SQL-Dialekt, Regressionsnetz

### 10.5 Zusammenführung der Fassungen (05.10.2026)

Zum Entscheid E90 entstanden am 05.10.2026 zwei Fassungen dieses Papiers; Grundlage der vereinten Rev. 2 ist die
Fassung mit Parametersatz und Generator (A). Aus der Fassung mit Vorlagen je Größe (B) ist übernommen bzw. verworfen:

- Übernommen: die Risiken als eigenes Kapitel 11 (Tabellenneubau in Anwenderdatenbanken, Lizenz der Normnamen, Parallelsitzungen am Zuordnungsdialog).
- Übernommen: die Einbauliste der Leser und Schreiber im Kern als 4.4, ergänzt um den Filter in `IfcAbbildBauer`, den IFC-Export samt IDS und die Raumtyp-Klassen-Tabelle.
- Übernommen: die Nachweise am Import (`IfcQuelldateienDurchgangTests`, Zonenimportproben), an Auslieferung und Katalogpaket, die Windows-Schale und der gestörte Lauf in Kapitel 8.
- Übernommen: die Wiki-Seite „Konditionierung“ im Nachzug und der Logbuch-Satz, umformuliert auf Kopien statt Verweise.
- Übernommen: die Verweise auf die Schemaschritte 176 und 178 und auf `CLAUDE.md`.
- Verworfen: die Tabellen `Tab_Nutzungskategorie_STAMM` und `Tab_Nutzungsprofil_STAMM` — der zweite Name gehört seit Schemaschritt 178 der Pufferauslegung (B6, NP-F2).
- Verworfen: `ID_Nutzungsprofil` an Vorlage, Kalenderkopie, Zone und Gebäude — eine Herkunft als ID am Ziel widerspricht 1.2 und NP-F14; die Zone trägt eine Textkopie (Q41, E91).
- Verworfen: die Profilwerte als Konditionierungsvorlagen je Größe (`Z_Nutzungsprofil_Vorlage`) — das Profil ist ein Parametersatz mit Generator (NP-F1), die 14 Vorlagen bleiben daneben (Q45, E91).
- Verworfen: die Umschlüsselung der Kennungen auf Profil-IDs (Entscheidvorschlag N‑3) — vorhandene Werte bleiben unverändert (NP-F15).
- Verworfen: Nummern und Kurznamen für SIA 2024 und VDI 2078 (Entscheidvorschlag N‑2 über DIN hinaus) — beide Kategorien kommen leer (5.3), für DIN gilt Q47 (E91).
- Verworfen: ein eigenes Profilfeld am Gebäude (Entscheidvorschlag N‑1) — das Gebäude bekommt das Profil als Kalenderkopie mit Nutzungstext, die Zone ohne eigene Kalender folgt dem Gebäude über die Kaskade (B4).
- Verworfen: die Zapf-Nutzungsart am Profil als Weg der Pufferauslegung — er bleibt bei `Z_Nutzungsprofil` (NP-F15).
- Verworfen: der Menüpunkt im Katalogmenü — das Blatt hat keinen (NP-F22, E56).
- Verworfen: die Vorbelegung aus der flächengrößten Klasse unter jeder Zonierungsregel — sie bleibt bei Z6 wie heute; eine Erweiterung wäre ein eigener Entscheid.
- Verworfen: die Stufen NP0 bis NP3 mit 4 PT — es gelten NP1 bis NP4 mit 19–28 PT (Kapitel 7).
- Die Entscheidvorschläge N‑1 bis N‑3 der Fassung B waren offen und keine Entscheide des Anwenders; sie widersprechen E91 nicht und sind mit den genannten Punkten erledigt.

---

## 11. Risiken

- **Tabellenneubau in Anwenderdatenbanken:** `Tab_Konditionierungskalender` und `Tab_Konditionierungsvorlage_STAMM`
  werden zeilengleich neu gebaut (4.2). Eine Anwenderdatenbank mit eigenen Sichten oder abweichenden Indizes darf
  dabei nichts verlieren — Zeilenzahl und Prüfsumme vor und nach dem Schritt im Nachweis, Sicherung nach
  [BETRIEB_SQLITE](BETRIEB_SQLITE.md) vor dem Schritt wie bei jedem Tabellenneubau.
- **Lizenz der Normnamen:** Nummer und Name der DIN-Profile gelten als Tatsachen ohne Werte; ausgeliefert wird nur, was
  der Anwender an seiner Ausgabe bestätigt hat (Q47). Im Zweifel bleibt eine Nummer weg; SIA 2024 und VDI 2078 kommen
  leer (5.3).
- **Normwerte über die Hintertür:** Werte aus einer Projektdatei (Q46) oder aus der Datenbank des Entwicklers dürfen
  nie in die Auslieferung — die Katalogwache und der Prüfbericht der Auslieferungsvorlage halten die Grenze (NP-F21).
- **Pufferauslegung:** Wer die Nutzungsspalte umbaut, trifft die Quelle `KONDITIONIERUNG` von `Z_Nutzungsprofil`
  (B6). Der Test „alte Kennung und Mustername ergeben dasselbe Pufferprofil“ (Kapitel 8) hält das.
- **Parallelsitzungen am Import:** Die Sitzung IFC-Ganglinie und die Arbeit am Zonenbaum ändern
  `GebaeudeImportDialog.razor`, `GebaeudeImportZonen.cs`, `Zonenplan.cs`, `ZonenplanCtrl.cs` und die `Sqproj*`-Leser;
  Zusammenführungskonflikte sind zu erwarten. Vor NP2 und NP3 `origin` zusammenführen, NP2 erst nach deren Merge oder
  in verabredeten Dateien (Kapitel 7).
- **Neue Muster ändern die Vorbelegung:** Mit Q42 belegt der Import Zonen vor, die heute „keine“ Nutzung bekommen; die
  Importproben bekommen ihre Erwartung in NP2 neu, Rechenweg und Referenzbasis bleiben unberührt.

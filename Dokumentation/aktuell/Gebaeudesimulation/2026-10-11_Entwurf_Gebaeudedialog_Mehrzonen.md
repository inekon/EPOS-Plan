# Entwurf GD — Gebäudedialog „Hülle und Zonen“ und Übernahme als Mehrzonenmodell

**Stand 11.10.2026 · Befund und Entwurf, nichts gebaut außer einer Probe.** Gelesen auf `d48d44fb0` (Zweig
`ios_migration_september`, Basis R52). Auftrag der Sitzung Gebäudesimulation. Fortsetzung des Dialogkonzepts des Anwenders
(`Gebäudesimulation Dialogkonzept-2026-10-11.docx` in diesem Ordner: „Dialog start: Name des Gebäudes — Vorgabe: Name der
Eingabedatei“). Probe zum Befund: `EPOS.Kern.Tests/GebaeudedialogBefundProbeTests.cs`.

**Anwendermeldung (Wortlaut, 11.10.2026):** „Der Dialog muss überarbeitet werden — vermutlich teilweise nicht funktionell
(Werte werden nicht verwendet). Die Zonenübernahme ist nicht übersichtlich — das Gebäude … hat Zonen und sollte automatisch
nach Zonen aufgeteilt werden (Zuordnung nach DIN 18599-10 wie in Datei). Gebäudeart sollte aus dem Import gesetzt werden —
wenn vorhanden auch andere Werte (Baualter, Bauart, …). Raumhöhe und Luftwechsel für das gesamte Gebäude sind nur im
Einzonenmodell sinnvoll. Gebäude sollte als Mehrzonenmodell übernommen werden können — nur Einzonenmodell zur Auswahl. Die
Kalender und Zonenparameter sollten alle vorbesetzt werden bzw. nach Wahl einem Kalender oder Profil zugeordnet werden. Es
soll einen sinnvollen Default geben.“

Bildschirmfotos der Meldung (Zitat): Reiter „Gebäude und Hülle“ mit Gebäudeart „Einfamilienhaus“, Baujahr 1995, Klasse
„1995 bis 2001“, Verwendung „Wohngebäude“, Raumhöhe 3,2436 m, Luftwechselrate 0,7 1/h; Rechenweg „VDI 6007“, Hülle
„Klassenweg — U-Wert-Gruppen und Bauweise“, Knopf „Gebäude als eine Zone übernehmen“. Kurz zuvor im Import: „Baualtersklasse
H – 1984 bis 1994“, Zonenregel „X1 – eine Zone je Zone der Datei“, 12 Zonen, Fehler „3 Räume sind keiner Zone zugeordnet
(UG-R10, UG-R17, UG-R18) — nicht gespeichert“.

## 0 Das Ergebnis in Punkten

- **Fünf Felder im Kopf wirken in keinem Rechenweg:** Gebäudeart, Verwendung, Baualtersklasse, Baujahr und Energiestandard. Sie
  dienen Bericht, Katalogfilter, dem Profilvorschlag der Pufferauslegung und den U-Wert-Vorgaben des Imports. „Fläche/Nutzer“
  wirkt nur mittelbar, über die Bewohnerzahl (Personen-Vorschlag, Brauchwasser). Alles andere wirkt. Raumhöhe, Luftwechselrate,
  Fensterdurchlassgrad und Bauweise wirken aber **je nach Weg verschieden** (Tabelle 1.1).
- **Raumhöhe und Luftwechselrate des Gebäudes sind keine Einzonenfelder:** Sie gelten auch je Zone, und zwar als geerbter Wert,
  solange die Zone keinen eigenen trägt. Importierte Zonen tragen Höhe und Volumen selbst. Die Raumhöhe des Gebäudes ist dort
  wirkungslos, die Luftwechselrate des Gebäudes wirkt in jeder Zone ohne Lüftungskalender weiter.
- **Warum das Gebäude ohne Zonen ankam:** Beim Weg über die Projektdatei hebt die Übernahme der DIN-Zonen die Zonierung der
  Regel auf. Ein Raum, der in keiner DIN-Zone steht, bleibt „nicht zugeordnet“. Das sperrt OK **und** den Bauteilvorschlag;
  der Schalter „als Zonen mit Bauteilen“ fällt dabei aus. Gespeichert werden kann erst, wenn der Plan anders aussieht (Rest
  zuordnen, Projektdatei entfernen, andere Regel). Bei abgeschaltetem Schalter schreibt der Import nur die Kenngrößen. Das
  Gebäude rechnet dann im Klassenweg. Nachgestellt in der Probe.
- **Die Projektdatei liefert mehr Gebäudewerte, als der Import liest:** Gelesen wird aus `BmBuilding` nur Name, Baujahr und
  Standort. Gebäudetyp, Nutzungstyp, Wärmeschutzstandard und Wirkkapazität (`Cwirk`) bleiben liegen. Die Gebäudeart kommt
  deshalb nie aus der Datei. Sie ist die Vorgabe eines neuen Satzes („Einfamilienhaus“) und wird nur dann „Sonstige“, wenn die
  Flächenmehrheit der DIN-Nummern auf Nichtwohnen deutet.
- **Zum Mehrzonenmodell führt im Gebäudedialog kein Weg:** Mehrere Zonen entstehen nur im Import, von Hand im Reiter „Zonen“ oder
  im Zonenbaum des Imports. Der Kern hat die Bausteine für eine Übernahme aus der gespeicherten Quelle: Neulesen mit
  Hashprüfung (HC-4), gespeicherter Plan `ZonenplanCtrl.Gespeichert`, Schreibweg `VorschlagSchreiben`. Kein Dialog verbindet
  sie bisher.
- **Vorbesetzung der Zonen:** Ganglinie vor Datei vor Profil (NP-F16) gilt. Eine Zone ohne zugeordnetes Profil bekommt aber
  **keinen** Kalender und erbt alles vom Gebäude. Das betrifft eine DIN-Nummer ohne Zuordnung, die Regeln X1 bis X3 ohne
  Projektdatei und das Einzonenmodell der Projektdatei. Einen sichtbaren Default je Zone gibt es nicht.
- **Vorschlag:** Wellen GD-1 bis GD-5 (Abschnitt 7). Felder ohne Wirkung werden gekennzeichnet oder wandern in einen Block
  „Beschreibung und Einordnung“. Jedes Feld zeigt seine Herkunft. Der Import liest die Gebäudewerte der Projektdatei, teilt nach
  DIN-Zonen und schlägt Räume ohne Zone selbst einer Zone zu. Dazu kommen ein Knopf „Gebäude als Mehrzonenmodell übernehmen“ und
  je Zone eine Wahl „Profil/Kalender zuweisen“ mit sichtbarer Vorgabe. **Die Referenzbasis bleibt unberührt**, solange GD die
  Leseregeln des Rechenwegs nicht ändert (Abschnitt 5.3).

## 1 Bestand

### 1.1 Feldkarte des Reiters „Gebäude und Hülle“ (Kopfgruppen)

Dialog: `EPOS.UI/Dialoge/Bedarf/GebaeudeKatalogDialog.razor`, Kenngrößen ab Zeile 147 und Rechenweg ab Zeile 453. Schreibweg
Feld → Spalte: `EPOS.UI.Daten/Bedarf/GebaeudeKatalogHuelle.cs:1335` (`NachModell`). Spalten: „VDI Einzone“ = Klassenweg ohne
Zone; „Bauteilweg“ = Gebäude mit Zonen und Bauteilen (eine oder mehrere); „Altweg“ = Tagesbilanz.

| Feld | Spalte | VDI Einzone (Klassenweg) | Bauteilweg (Zonen) | Altweg | Sonst | Urteil |
|---|---|---|---|---|---|---|
| Name | `Gebaeudename` | — | — | — | Anzeige, Bericht | Kennung |
| Gebäudeart | `Gebaeudeart` | — | — | — | Profilvorschlag Pufferauslegung (`Pufferauslegung/Nutzungsprofil.cs:29`), Bericht (`BausteineProjekt.cs`), Katalogfilter. Das Zapfprofil nimmt seine eigene Gebäudeart (`ZapfprofilCtrl.Eingang.cs:121`) | **wirkt nicht im Rechenweg** |
| Baualtersklasse | `Baualtersklasse` | — | — | — | Bericht (`BausteineProjekt.cs:50`); im Import Quelle der U-Wert-Vorgaben (`GebaeudeVorgaben`) | **wirkt nicht im Rechenweg** |
| Baujahr | `Baujahr` | — | — | — | schlägt die Klasse vor (`GebaeudeKatalogDaten.cs:90`), Standprüfung des Imports | **wirkt nicht** |
| Verwendung | `Wohngebaeude_Nicht_Wohngebaeude` | — | — | — | filtert nur die Liste des Energiestandards (`GebaeudeArbeitsstand.cs:598`) | **wirkt nicht** |
| Energiestandard | `Energiestandard` | — | — | — | im Import Vorrang vor der Klasse bei den U-Vorgaben (`GebaeudeVorgaben.cs:243`) | **wirkt nicht im Rechenweg** |
| Bauart → Bauweise | `Bauweise` | C_ges aus Bauweise (`GebaeudeModellEingang.cs:2713`, `ErsatzparameterRC.cs:958`) | nur Rückfall für eine Gruppe ohne Aufbau, je Zone anteilig (`GebaeudeModellEingang.cs:829`) | ja (`TagesbilanzRechenweg.cs:348`) | — | wirkt je nach Weg |
| Nutzfläche | `Wohnflaeche_gesamt`, `Nutzflaeche` | ja | Flächenschlüssel der Zonen (`Zonenvorgaben.cs:184`) | ja | Brauchwasser, Bericht | wirkt |
| Fläche/Nutzer | `Flaeche_Nutzer` → `Bewohner` | nur Pflichtprüfung (`GebaeudeModellEingang.cs:3110`) | dto. | über `Bewohner` | Bewohner = Fläche ÷ Fläche/Nutzer (`GebaeudeKatalogHuelle.cs:1351`): Personen-Vorschlag der Konditionierung (`Konditionierungsarbeit.cs:840`), Brauchwasser (`SimulationWaermebedarf.cs:1181`) | **wirkt nur mittelbar** |
| Fensterdurchlassgrad | `Fensterdurchlassgrad` | g aller Fenster (`GebaeudeModellEingang.cs:2728`) | nur Rückfall für ein Fenster ohne eigenes g (`GebaeudeModellEingang.cs:1574`, `BauteilEingang.cs:246`) | ja (`TagesbilanzRechenweg.cs:308`) | — | wirkt je nach Weg |
| Raumhöhe | `Raumhoehe` | Luftvolumen (`GebaeudeModellEingang.cs:2712`) | geerbt, wenn die Zone keine Höhe trägt (`Zonenvorgaben.cs:177`). Importierte Zonen tragen Höhe und Volumen (`GebaeudeBauteilvorschlag.cs:1384`) → **dort wirkungslos** | ja (`TagesbilanzRechenweg.cs:321`) | Bericht | wirkt je nach Weg |
| Luftwechselrate | `Luftwechselrate` | nur wenn Infiltration **und** Nutzerlüftung leer sind (`Gebaeuderechenweg.cs:100`) | dieselbe Regel je Zone (`GebaeudeModellEingang.cs:650`, `Zonenvorgaben.cs:204`) | ja | Bericht, Abweichungsermittler | wirkt, wenn keine Lüftungswerte da sind |
| Rechenweg | `Gebaeude_Modell` | Weiche (`Gebaeuderechenweg`) | Weiche | Weiche | — | wirkt |
| Gebäudetyp (nur Altweg) | `Typ` | — | — | ja | — | wirkt nur im Altweg |
| Hülle (U·A-Tabelle) | `k_Wert_*`, Flächen, Wärmebrücken | ja | bei Zonen nur Anzeige aus den Bauteilen (Summenregel, Dialog Zeile 214) | ja | — | wirkt nur ohne Zonen |

Kurz zu den Nachbarreitern:

- **Konditionierung** (`KonditionierungReiter.razor`, Matrix je Größe): Heiz- und Kühlsollwert, Infiltration, Nutzerlüftung,
  Sommerlüftung, innere Gewinne, Maximalraumtemperatur, Ferien. **Wirkt allein im VDI-Weg** (`Konditionierungseingang.cs`).
  Kaskade: Zone vor Gebäude. Der Altweg kennt keine Kalender, er liest die alten Sollwertspalten (`TagesbilanzRechenweg`,
  `Raumsolltemperatur_*`).
- **Zonen** (`ZonenDialog.razor`, Werte je Zone `GebaeudeKatalogHuelle.cs:380`): Fläche, Raumhöhe, Volumen, beheizt, die vier
  Sollwerte, θ_max, Infiltration, Nutzerlüftung, innere Gewinne, Bewohner. Jedes Feld leer = Wert des Gebäudes
  (`Zonenvorgaben.Bilden`), mehrere Zonen anteilig nach Fläche. Wirkt im VDI-Weg, im Altweg nicht (der Altweg kennt keine
  Zonen).

### 1.2 Der Weg „Projektdatei mit Zonen“ — warum das Gebäude ohne Zonen ankam

1. Beim Weg „nur Projektdatei“ legt der Dialog nach dem Lesen den Schritt „Zonen der Projektdatei“ von selbst auf den Plan
   (`GebaeudeImportDialog.razor:1574`); beim Weg „IFC + Projektdatei“ geschieht das nach dem Dazuladen (Zeile 2309).
2. Der Schritt hebt die Zonierung der Regel auf (`SqprojZonen.cs:78`, `plan.ZonierungAufheben()`). Er hängt nur die Räume ein, die
   in einer Zone der gewählten Zonierung (DIN- oder Simulationszonen, E87) stehen. Ein Raum ohne DIN-Zone bleibt **nicht
   zugeordnet** (`Zonenplan.cs:165`).
3. `Zonenplan.Abschlusspruefung` (`Zonenplan.cs:610`) meldet das als Fehler. Er sperrt den Bauteilvorschlag
   (`GebaeudeBauteilvorschlag.cs:1358`, damit `Moeglich = false`). Der Schalter „als Zonen mit Bauteilen“ steht dann aus und
   gesperrt (`GebaeudeImportDialog.razor:1296`, `AlsZoneWirksam`). Zugleich sperrt der Fehler OK (`GebaeudeImportHuelle.cs:967`),
   **auch bei abgeschaltetem Schalter**.
4. Zum Speichern muss der Plan geändert werden: „Rest nach Regel zuordnen“ (`Zonenplan.cs:533`), Räume von Hand zuordnen, die
   Projektdatei entfernen oder eine andere Regel wählen. Läuft das bis zum OK, ohne dass der Bauteilvorschlag wieder möglich wird
   oder der Schalter wieder an ist, schreibt der Import nur die Kenngrößen und die Gebäudekonditionierung des Einzonenwegs
   (`GebaeudeImportHuelle.cs:1400`, 1404). Das Gebäude hat danach keine Zone und rechnet im Klassenweg, wie im Bildschirmfoto.
   Welchen dieser Wege der Anwender nahm, sagt der Code nicht. Die Probe zeigt Sperre, Lösung über den Rest und die
   Vorbelegung.

Probe `GebaeudedialogBefundProbeTests.Raum_ohne_DIN_Zone_sperrt_Uebernahme_und_Zonen` (synthetisches Sporthaus, ein Raum ohne
DIN-Zone, grün): Der Plan führt den Raum als nicht zugeordnet, `Bauteile.Moeglich = false`, und `Pruefen` meldet einen Fehler
mit und ohne Schalter. „Rest nach Regel X3“ leert die Liste.

**Befund zur Übersicht:** Der Fehlertext nennt die Räume. Er schlägt aber keinen Platz für sie vor, und der Zusammenhang
„gesperrter Schalter ↔ nicht zugeordnete Räume“ steht an zwei Stellen des Dialogs. Das ist die „nicht übersichtliche“
Zonenübernahme der Meldung.

### 1.3 Gebäudewerte der Projektdatei: was da ist, was gelesen wird

- **In der Datei** (Befund HottCAD, Abschnitt 5 und Tabelle der `HSETU_*`-Sätze): an `BmBuilding` bzw. `HSETU_Gebäude…`
  `BuildingType`, `BuildingUsageType`, `BuildingHeatInsulationStandardType`, `BuildingEnergyEfficientType`,
  `YearOfConstruction`, Hüllkennwerte (`UmValue`, `Cwirk`). An den Geschossen `ElevationOfRefHeight`, an den Räumen Höhe,
  Volumen, `InsideTemperature`, `RatedMinAirExchangeRate`. An den Zonen die DIN-Nummer des Nutzungsprofils.
- **Gelesen** (`SqprojLeser.cs:184`): an `BmBuilding` nur Kennung, Name, `YearOfConstruction` (→ Baujahr) und `SiteUUID`.
  Gebäudetyp, Nutzungstyp, Wärmeschutzstandard und `Cwirk` liest niemand.
- **Gebäudeart:** Vorgabe eines neuen Satzes `GebaeudeModel.cs:229` („Einfamilienhaus“). `GebaeudeImportHuelle.VerwendungSetzen`
  (Zeile 1456) setzt die Verwendung aus der Datei: Gebäudeart der Datei (nur gbXML und IFC), sonst DIN-Nummern der Zonen nach
  Fläche (`Gebaeudeverwendung.WohnenAusDinNummer`: 70/71 Wohnen, 1–43 Nichtwohnen, andere zählen nicht). Nur bei
  Nichtwohnen wird aus „Einfamilienhaus“ „Sonstige“. In der Probe greift das (Büro-Nummer → „Sonstige“). Steht im
  Bildschirmfoto „Einfamilienhaus/Wohngebäude“, fand die Ableitung keinen Anhalt: keine Zone mit Nummer, nur Gruppenprofile,
  Nummern außerhalb der Tabelle. Oder der Satz entstand nicht über die Vorbelegung des Imports.
- **Baujahr und Klasse:** Das Baujahr der Datei wird übernommen (`GebaeudeImportHuelle.cs:1528`), die Klasse kommt aus dem Satz
  (Zeile 1524). Eine ausdrücklich gewählte Klasse gilt vor der aus dem Baujahr (`GebaeudeImportHuelle.cs:753`). Kopfzeile des
  Imports „H – 1984 bis 1994“ und Editor „1995 bis 2001 / Baujahr 1995“ widersprechen sich. Entweder galt im Import eine
  ausdrücklich gewählte Klasse, und der Editor zeigt die Klasse, die später gespeichert wurde. Oder das Baujahr stand im Import
  noch nicht fest. Der Code lässt beides zu. Der Entwurf beseitigt die doppelte Quelle (2.2).
- **Bauart:** nur über die Importzeile `BAUART` (`GebaeudeImportHuelle.cs:1530`). Aus der Projektdatei kommt keine, die Bauart
  bleibt Vorgabe. `Cwirk` könnte sie liefern.
- **Raumhöhe 3,2436 m im Bildschirmfoto:** die aus Volumen und Fläche gemittelte Höhe der Importzeile `RAUMHOEHE`. Ohne Zonen
  bestimmt sie das Luftvolumen.

### 1.4 Wege zu mehreren Zonen

| Weg | Wo | Ergebnis |
|---|---|---|
| Import mit Zonenregel und Zonenbaum (G6c, Mehrzonenkonzept 6.4) | `GebaeudeImportDialog`, `GebaeudeImportZonen` | Zonen mit Bauteilen, Profil, Konditionierung der Projektdatei (`GebaeudeZonenCtrl.cs:1195`) |
| Reiter „Zonen“ von Hand | `ZonenDialog.razor` | Zonen ohne Bauteile oder mit Bauteilen von Hand |
| „Gebäude als eine Zone übernehmen“ | `GebaeudeZonenuebernahme` (Dialog Zeile 1421) | **eine** Zone aus den U-Wert-Gruppen, Bauteile ohne Schichten |
| Datei erneut lesen (HC-4) | `GebaeudeNeulesen`, `GebaeudeDialog.razor` | Abbild, Zonierung, Grundriss für die **Ansicht**; schreibt nichts |
| Gespeicherter Plan eines früheren Imports | `ZonenplanCtrl.Gespeichert` (`ZonenplanCtrl.cs:202`), `Zonenplan.AusGespeichert` | Kernweg vorhanden, **nur in Tests gerufen** |

Für „Gebäude als Mehrzonenmodell übernehmen“ fehlt ein Dialogweg. Er müsste die Datei neu lesen (Hash gegen
`Tab_Importquelle`), den gespeicherten Plan oder die Vorgabe der Datei laden, den Zonenbaum des Imports zeigen und den Vorschlag
über `VorschlagSchreiben` in das **bestehende** Gebäude schreiben, mit Ersetzen vorhandener Zonen nach Rückfrage. Die
Raumgrundrisse liegen schon an der Quelle (`Tab_Raumgrundriss`, HC-5).

### 1.5 Vorbesetzung einer importierten Zone heute

- Werte der Zone aus der Datei: Fläche, Höhe, Volumen, beheizt (`GebaeudeBauteilvorschlag.cs:1384`). Infiltration,
  Nutzerlüftung, innere Gewinne und Bewohner bleiben leer und werden vom Gebäude geerbt.
- Kalender (`GebaeudeZonenCtrl.cs:1195`): mit Projektdateikonditionierung `ZonenplanCtrl.ProjektdateiUebernehmen` (erst das
  Profil, dann je Größe die Datei, zuletzt die Ganglinie). Sonst `NutzungUebernehmen`. Rangfolge Ganglinie vor Datei vor Profil
  (NP-F16).
- **Lücke 1:** `NutzungUebernehmen` mit Profil `null` schreibt nichts (`ZonenplanCtrl.cs:36`). Die Zone hat dann keinen
  Kalender und erbt die Gebäudekalender, ohne dass der Dialog das sagt. Das trifft die Regeln X1 bis X3 ohne Profil, X1 aus
  IFC/gbXML ohne Raumnutzung und eine DIN-Nummer ohne Zuordnung (Meldung `NUTZUNG_OHNE_ABBILDUNG`, Info, `SqprojZonen.cs:93`).
- **Lücke 2:** Der Einzonenweg der Projektdatei setzt bewusst kein Profil (NP-F16, Anwender 06.10.2026). Die Zuweisung geht über
  „Nutzungsprofil übernehmen…“ im Gebäudeeditor.
- **Lücke 3:** Ein Profil des Katalogs ohne Kennwert für eine Größe legt für diese Größe keinen Kalender an (NP-F18). Die Zone
  erbt dann still. Was nach NP6 (SIA/VDI heraus) an DIN-Wohnprofilen bleibt, bestimmt, wie oft das vorkommt.

## 2 Zielbild

### 2.1 Gebäudedialog: nur Felder, die wirken — mit Herkunft

1. **Block „Kenngrößen“ wird zweigeteilt:**
   - **„Rechengrößen“:** Nutzfläche, Bauart/Bauweise, Fensterdurchlassgrad, Raumhöhe, Luftwechselrate, Fläche/Nutzer. Jede
     Zeile trägt rechts eine kleine Herkunftsmarke (Datei · Vorgabe · Hand · geerbt), dieselbe Marke wie die Importzeilen
     (`GebaeudeFeldzeile`).
   - **„Beschreibung und Einordnung“:** Name, Beschreibung, Gebäudeart, Verwendung, Baujahr, Baualtersklasse, Energiestandard.
     Darunter eine leise Zeile: „Diese Angaben ordnen das Gebäude ein (Bericht, Vorschläge); gerechnet wird mit ihnen nicht.“
2. **Mit Zonen** (Bauteilweg, eine oder mehrere):
   - Raumhöhe: Bei Zonen, die alle eine eigene Höhe bzw. ein Volumen tragen, steht die Zeile als Text „gilt je Zone, siehe
     Reiter Zonen“ ohne Feld. Trägt eine Zone keine, bleibt das Feld mit der Herleitung „Vorgabe für n Zonen ohne eigene Höhe“.
   - Luftwechselrate: Ist eine Zone über Konditionierung oder Zonenwerte belüftet (Infiltration/Nutzer gesetzt), entfällt das
     Feld. Sonst bleibt es mit „gilt in n Zonen ohne Lüftungswerte“. Die Regel steht einmal im Kern
     (`Gebaeudemodellvorgaben.WirksamerLuftwechsel`); der Dialog fragt sie nur ab.
   - Fensterdurchlassgrad: „Rückfall für n Fenster ohne eigenes g“, entfällt bei n = 0.
   - Bauart: „Rückfall für n Gruppen ohne Aufbau“, entfällt bei n = 0.
   - Die U·A-Tabelle steht bei Zonen schon heute als abgeleitete Anzeige (Summenregel).
3. **Im Altweg** bleiben alle Rechengrößen. Die Konditionierung steht wie heute als Hinweis „gilt im VDI-Weg“.
4. **Rechenweg-Block:** Statt allein „Gebäude als eine Zone übernehmen“ zwei Knöpfe:
   „Als eine Zone übernehmen“ und **„Als Mehrzonenmodell übernehmen…“** (2.4). Der zweite ist aktiv, wenn eine
   Importquelle gespeichert ist; sonst weich gesperrt mit Grund („kein Import gespeichert — Gebäude über ‚Import…‘ anlegen“).

### 2.2 Import setzt die Gebäudewerte aus der Datei

- **Name** = Dateiname ohne Endung (Dialogkonzept des Anwenders). Trägt die Datei einen Gebäudenamen, steht er als Vorschlag
  daneben.
- **Projektdatei** (neu zu lesen aus `BmBuilding`/`HSETU_Gebäude…`):
  - `BuildingType` → Gebäudeart, über eine feste Tabelle im Kern (wie `Din18599Nutzung`). Unbekannte Codes führen zu
    „Sonstige“ mit Beleg.
  - `BuildingUsageType` → Verwendung (Wohn/Nichtwohn). Sie hat Vorrang vor der Flächenmehrheit der DIN-Nummern.
  - `BuildingHeatInsulationStandardType`/`BuildingEnergyEfficientType` → Energiestandard, wenn eine Abbildung auf
    `Energiestandard.CODES` eindeutig ist, sonst leer mit Beleg.
  - `Cwirk` → Bauweise (Wh/K) unmittelbar, mit Herkunft Datei. Die Bauart folgt als nächstliegende Klasse, nur zur Anzeige.
  - `YearOfConstruction` → Baujahr (gelesen wie bisher).
- **IFC/gbXML:** wie heute über `Gebaeudeverwendung` (Gebäudeart der Datei, Raumtypen).
- **Eine Quelle für die Klasse:** Kopf des Imports und Editor zeigen **dieselbe** Klasse mit derselben Herleitung („aus dem
  Baujahr der Datei“ / „gewählt“). Eine ausdrückliche Wahl im Import wird als Handwert gespeichert und im Editor als „gewählt“
  gezeigt.
- Was die Datei nicht hat, steht als **sichtbare Vorgabe** da (Marke „Vorgabe“), nie still.

### 2.3 Projektdatei mit Zonen: automatisch nach DIN-Zonen, nichts blockiert stumm

- Vorgabe der Zonierung ist die der DIN V 18599 (E87), mit Nutzung aus der DIN-Nummer der Datei (Zuordnung `DIN_NUMMER`). So
  arbeitet der Kern schon heute.
- **Neu: Räume ohne DIN-Zone werden beim Übernehmen selbst platziert**, nach einer Regel in fester Folge:
  1. **unbeheizt** (Haken der Datei oder Namensregel) → eine gemeinsame Zone „unbeheizt“ (frei schwingend), oder außerhalb, wenn
     die Zone keine Fläche zum Gebäude beiträgt;
  2. **beheizt mit Nachbarraum in einer Zone** (Trennfläche aus `Koerpernachbarschaft`/Raumgrenzen) → zur Nachbarzone mit der
     größten gemeinsamen Fläche;
  3. **sonst** → eigene Zone je Geschoss („Rest EG“ …) ohne Profil, mit der Vorgabe nach 2.5.
  Jede Platzierung ist ein Planschritt mit Herkunft „Vorschlag“. Der Zonenbaum zeigt ihn markiert, und der Anwender kann ihn
  zurücknehmen. Die Fehlermeldung „nicht gespeichert“ entfällt für diesen Fall. Sie bleibt nur, wenn der Anwender die Vorschläge
  verwirft und Räume offen lässt. Dann steht sie **am Schalter**: „als Zonen mit Bauteilen — gesperrt: 3 Räume ohne Zone
  [zuordnen…]“.
- Das Protokoll nennt je platziertem Raum Regel und Ziel.

### 2.4 „Gebäude als Mehrzonenmodell übernehmen…“ aus der gespeicherten Importquelle

1. Der Knopf öffnet die Dateiwahl mit der gespeicherten Quelle als Vorgabe (Name, Format). HC-4 prüft den Hash. Bei
   Abweichung: Rückfrage „Datei geändert — trotzdem lesen?“, wie in HC-4 festgelegt.
2. Danach öffnet sich der **Zonenbaum des Imports** als Blatt im Gebäudedialog. Er startet mit dem gespeicherten Plan
   (`ZonenplanCtrl.Gespeichert`), sonst mit der Vorgabe der Datei, also nach 2.3.
3. OK schreibt die Zonen samt Bauteilen und Kalendern über `VorschlagSchreiben` in das bestehende Gebäude. Vorhandene Zonen
   werden nach Rückfrage ersetzt. Die Kenngrößen bleiben, außer denen, die die Zonen nun selbst tragen. Geschrieben wird im
   OK-Weg des Gebäudedialogs (Arbeitsstand), wie beim Einzonenknopf.
4. Auf iOS gilt derselbe Weg: Die Datei kommt über die Dateiwahl der Schale, die Größengrenze gilt je Plattform.

### 2.5 Vorbesetzung jeder Zone: sinnvolle Vorgabe und Wahl je Zone

- **Vorgaberegel**, in fester Folge je Zone und Größe:
  1. Ganglinie der Datei;
  2. Wert der Datei (Projektdatei-Konditionierung);
  3. Profil aus der Zuordnung (DIN-Nummer, Raumtyp, Nutzungsklasse);
  4. **neu: Profil der Gebäudeart** (Zuordnung `GEBAEUDEART`, wie im Profilvorschlag der Pufferauslegung) für Zonen ohne eigene
     Nutzung;
  5. Gebäudekalender (Erbe), sichtbar als „geerbt vom Gebäude“.
  Das Einzonenmodell der Projektdatei bekommt nach Stufe 3 bzw. 4 ein Profil, **wenn der Anwender das wählt** (Frage F5, NP-F16
  hält heute „kein Profil automatisch“).
- **Reiter „Zonen“:** je Zone eine Spalte „Nutzung/Profil“ mit Herkunft und eine Aktion **„Profil/Kalender zuweisen…“**: Profil
  des Katalogs (Generator) oder Kalender einer Vorlage je Größe. Mehrfachwahl wendet die Aktion auf mehrere Zonen an. Eine Zone
  ohne Kalender zeigt „geerbt vom Gebäude“ statt nichts.
- **Unbeheizte Zonen** bekommen nie Heiz-/Kühlkalender. Das ist schon heute so und wird sichtbar gemacht.

## 3 Abstimmung mit den laufenden Wellen

| Welle | Berührung | Vorschlag |
|---|---|---|
| AB/AB2 (Gebäudeansicht, Legendenzuordnung, Bauteile im Weg nur Projektdatei) | Bauteilvorschlag im Weg nur Projektdatei, Zonenbaum, Ansicht | GD-3 und GD-4 setzen auf AB2 auf; die Platzierung 2.3 nutzt dieselbe Nachbarschaft wie die Ansicht. Erst nach dem Merge von AB2 bauen |
| AB3 (Baustoffe aus dem Import, Baustoffwahl im Aufbau) | `GebaeudeBauteilvorschlag`, Aufbauten | keine Überschneidung im Kopfblock; GD-4 ruft denselben Vorschlag, also nach AB3 mergen |
| SQ2 (Raumflächen der Projektdatei) | Flächen und Höhen der Räume der Projektdatei | GD-2 liest Gebäudewerte, SQ2 Raumwerte: getrennte Methoden in `SqprojLeser`, Merge-Reihenfolge SQ2 vor GD-2 |
| NP6 (DIN-Wohnprofile, SIA/VDI heraus, Schemaschritt 219) | Zuordnung `DIN_NUMMER`, Profilkatalog | GD-5 (Vorgaberegel 2.5) baut auf dem Katalog nach NP6; die Liste der DIN-Nummern ohne Profil wird nach NP6 gemessen |

## 4 Folgen

### 4.1 Kern

- `SqprojLeser`: Gebäudewerte aus `BmBuilding`/`HSETU_Gebäude…` lesen (Typ, Nutzung, Wärmeschutz, `Cwirk`), dazu
  `AbbildGebaeude.Art`, `Verwendung`, `Energiestandard`, `BauweiseWhK` und je eine Abbildungstabelle im Kern.
- `Zonenplan`: Schritt „Rest platzieren“ (unbeheizt, Nachbarzone, je Geschoss) als reiner Planschritt mit Herkunft „Vorschlag“.
- `GebaeudeZonenCtrl`/`ZonenplanCtrl`: Ersetzen der Zonen eines bestehenden Gebäudes in einem Vorgang. Die Vorgaberegel 2.5
  Stufe 4 läuft über die vorhandene Zuordnung `GEBAEUDEART`.
- Eine Auskunft „wirksame Herkunft je Gebäudefeld“ (Datei/Vorgabe/Hand/geerbt/wirkungslos), die die Zahlen von
  `Zonenvorgaben` und `WirksamerLuftwechsel` **ruft**, nicht nachrechnet.

### 4.2 `EPOS.UI.Daten` und Oberfläche

- `GebaeudeKatalogHuelle`: Herkunftsmarken je Feld, die Auskunft aus 4.1, ein Delegat „Mehrzonenmodell übernehmen“.
- `GebaeudeKatalogDialog.razor`: Kopf zweigeteilt, Zonenfälle nach 2.1, Uebernahmeblock mit zwei Knöpfen, ein Blatt für den
  Zonenbaum (Baustein aus dem Import wiederverwendet, kein Nachbau).
- `GebaeudeImportDialog.razor`: die Meldung am Schalter, Vorschläge im Zonenbaum markiert.
- `ZonenDialog.razor`: Spalte Nutzung/Profil, Aktion „Profil/Kalender zuweisen…“.
- Ressourcen in beiden Sprachen, `ResourceDesigner` ziehen, KI-Feldkarten (`KiDialoge`) nachziehen.

### 4.3 Proben und Wiki

- `fensterprobe.mjs` für das neue Blatt; Rasterprobe nur, wenn der Zonenreiter eine `Katalogliste` bekommt.
- Wiki-Quellen „Gebäude“ und „Gebäudeimport“ fortschreiben, Logbuch-Entwurf je sichtbarer Änderung.

## 5 Rechenweg, Basis und Schema

### 5.1 Schema

- GD-1, GD-3 und GD-5 brauchen keinen Schemaschritt.
- GD-2 braucht einen, wenn die Herkunft der Gebäudefelder gespeichert werden soll. Dafür gibt es zwei Möglichkeiten: Die
  vorhandene Herkunftstabelle des Imports wird um die Felder erweitert, oder `Tab_Gebaeude` bekommt eine Spalte
  `Bauweise_Herkunft`. Der Schritt ginge über `SchemaMigration`.
- GD-4 braucht keinen, die Ablage über `Tab_Importquelle`/`Tab_Importzuordnung` besteht.
- Nächste freie Nummer laut Kopfzeile der Statusdatei: **220**. Hier nur benannt, nicht angemeldet.

### 5.2 Einfrierregeln

- Die Regeln „gesäte Gebäudedaten“ und „gesäte Zonendaten“ greifen nur, wenn ein Referenzgebäude oder eine Referenzzone
  **geändert** wird. GD ändert die Testdatenbank nicht. Der Import schreibt nur neue Gebäude, und „Als Mehrzonenmodell
  übernehmen“ ist eine Handaktion.
- Die Vorgaberegel 2.5 Stufe 4 wirkt beim **Import**, nicht im Lauf: Sie legt Kalender an, statt den Leser zu ändern. Kein
  Referenzprojekt ist ein Importgebäude ohne Profil, also keine neue Basis.

### 5.3 Rechenweg

- GD ändert keine Leseregel. Die Kaskade Zone vor Gebäude, `WirksamerLuftwechsel` und die Rückfälle von g und Bauweise bleiben
  bitgleich, auch die Referenzläufe der Gebäude mit Zonen (1051, 1052, 1054, 1062, 1065).
- **Ausnahme auf Wunsch (F6):** Wird die Gebäude-Luftwechselrate bei Zonen abgeschafft statt nur ausgeblendet, ändert sich der
  Rechenweg aller Zonen ohne Lüftungswerte. Das betrifft die Einfrierregel „gesäte Zonendaten“ und verlangt eine neue Basis.
  Empfehlung: nicht.

## 6 Fragen an den Anwender

1. **F1 Felder ohne Wirkung:** im Block „Beschreibung und Einordnung“ mit Hinweis behalten, oder Verwendung/Energiestandard aus
   dem Dialog nehmen? **Empfehlung:** behalten und kennzeichnen. Sie tragen den Bericht und die Vorgaben des Imports.
2. **F2 Raumhöhe und Luftwechsel bei Zonen:** ausblenden, wenn jede Zone eigene Werte trägt, sonst als „Vorgabe für n Zonen“
   zeigen? **Empfehlung:** ja (2.1 Nr. 2).
3. **F3 Räume ohne DIN-Zone:** automatisch platzieren (unbeheizt → Zone „unbeheizt“, beheizt → Nachbarzone, sonst je Geschoss),
   mit Rücknahme im Zonenbaum? **Empfehlung:** ja. Alternative: eine einzige Zone „Rest“.
4. **F4 Gebäudewerte der Projektdatei:** `BuildingType`, `BuildingUsageType`, Wärmeschutzstandard und `Cwirk` lesen, mit
   Vorrang vor Ableitung und Vorgabe? **Empfehlung:** ja. `Cwirk` direkt als Bauweise, die Bauart nur abgeleitet.
5. **F5 Profil im Einzonenweg der Projektdatei:** NP-F16 hält „kein Profil automatisch“. Soll die Vorgaberegel 2.5 (Profil der
   DIN-Nummer bzw. der Gebäudeart) auch hier greifen? **Empfehlung:** ja, sichtbar als Vorgabe und abwählbar. Das ändert NP-F16.
6. **F6 Luftwechselrate des Gebäudes bei Zonen:** nur ausblenden (Rechenweg bleibt) oder fachlich abschaffen (neue Basis)?
   **Empfehlung:** nur ausblenden.
7. **F7 Mehrzonenübernahme:** „Als Mehrzonenmodell übernehmen…“ nur aus einer gespeicherten Importquelle oder auch aus einer neu
   gewählten Datei? **Empfehlung:** gespeicherte Quelle zuerst. Eine andere Datei ist ein neuer Import.
8. **F8 Name:** immer der Dateiname, oder der Gebäudename der Datei, wenn vorhanden? **Empfehlung:** Dateiname nach dem
   Dialogkonzept, Gebäudename der Datei als Vorschlag daneben.

## 7 Wellenplan

| Welle | Inhalt | Aufwand | Abnahme |
|---|---|---|---|
| **GD-1** Kopf und Herkunft | Kopf zweigeteilt, Herkunftsmarken, Zonenfälle Raumhöhe/Luftwechsel/g/Bauart (2.1), Auskunft der wirksamen Herkunft im Kern | 1,5–2 PT | Kern-Filter grün; Tests der Auskunft gegen `Zonenvorgaben` (ruft, rechnet nicht nach); bunit Feldbestand ein- und mehrzonig; Referenzlauf byte-gleich; `fensterprobe.mjs` |
| **GD-2** Gebäudewerte der Projektdatei | `SqprojLeser` liest Typ, Nutzung, Wärmeschutz, `Cwirk`; Abbildungstabellen; Vorbelegung; eine Quelle der Klasse; ggf. Schemaschritt der Herkunft | 1,5–2,5 PT | Leser- und Huelltests mit `SqprojProbenErzeuger`; `SqlDialektPruefer`; Referenzlauf byte-gleich; nach SQ2 |
| **GD-3** Räume ohne DIN-Zone | Planschritt „Rest platzieren“, Meldung am Schalter, Markierung im Zonenbaum; Probe `GebaeudedialogBefundProbeTests` wird auf das neue Verhalten umgestellt | 1,5–2 PT | Zonenplan- und Huelltests; Durchlauf „nur Projektdatei“ speichert Zonen ohne Handschritt; nach AB2 |
| **GD-4** Als Mehrzonenmodell übernehmen | Knopf, Neulesen mit Hash, Zonenbaum als Blatt, Schreiben in das bestehende Gebäude mit Ersetzen | 2,5–3,5 PT | Kern-Test Ersetzen in einem Vorgang (Rückweg bei Fehler); bunit Blatt, Abbrechen ohne Schreiben; Windows-Schale baut; `fensterprobe.mjs`; nach AB2 und AB3 |
| **GD-5** Vorbesetzung der Zonen | Vorgaberegel 2.5, Spalte Nutzung/Profil, Aktion „Profil/Kalender zuweisen…“, Anzeige „geerbt“ | 2–3 PT | Kern-Tests der Rangfolge je Größe; Test „Zone ohne Profil bekommt Gebäudeartprofil“; Referenzlauf byte-gleich; nach NP6 |
| Wiki/Logbuch | Seiten „Gebäude“, „Gebäudeimport“, Logbuch-Entwurf | 0,5 PT | Gegenlesemuster der Wurzel-`CLAUDE.md` ohne Treffer |

Summe rund 9,5–13,5 PT. Reihenfolge: GD-1 sofort (keine Abhängigkeit), GD-2 nach SQ2, GD-3 nach AB2, GD-4 nach GD-3 und AB3,
GD-5 nach NP6. Jede Welle mit eigenem Zweig, Kern-Filter und `--filter` auf die betroffenen Klassen; das Gate fährt die
Orchestrierung nach dem Merge.

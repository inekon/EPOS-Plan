# Übergabe der Sitzung „Gebäudesimulation“ — Stand 12.10.2026 (Kontowechsel)

Dieses Papier hält, was eine neue Cloud-Sitzung auf einem anderen Konto braucht, um die Sitzung „Gebäudesimulation“
ohne Verlust fortzuführen: Stand, laufende und nächste Arbeit, offene Anwenderpunkte, Arbeitsweise und am Ende den
Einstiegs-Prompt. Regelquelle bleibt [`CLAUDE.md`](../../../CLAUDE.md) mit den Projekt-`CLAUDE.md`; der dauerhafte Stand steht
in der [Statusdatei iOS-Migration](../Status_iOS_Migration.md) (eine Zeile je Schritt) und in der
[Statusdatei Gebäudesimulation](../Status_Gebaeudesimulation_VDI6007.md) (Entscheidungsregister bis E138, Stufentabelle).
Es löst die [Übergabe vom 08.10.2026](../../ueberholt/2026-10-08_Uebergabe_Gebaeudesimulation_Kontowechsel.md) ab und wandert
nach `Dokumentation/ueberholt/`, sobald die Fortsetzung den ersten Importlauf, BK-2b und die GD-Nachzüge abgeschlossen hat.

## 1. Stand bei der Übergabe

| Was | Wert |
|---|---|
| Arbeitszweig | `ios_migration_september` (Remote `origin`). Der Sitzungszweig des alten Kontos `claude/magical-bohr-ff5ufj` wurde bei jedem Push mitgezogen; die neue Sitzung nimmt ihren eigenen Sitzungszweig |
| Letzte Wellen | Nachzüge N1 (#1091, Oberfläche zu MA-1b/MA-2/MA-5/MA-7) und N2 (#1092, Kern zu MA-6/DA-4/DA-7) gepusht am 14.10.2026 (Runde N1+N2, Nachgate grün auf 48eb857ed: 12/12 gegen R54, UI 9 535, Kern gefiltert 2 625); davor DA-4 (#1081, Restdaten), DA-5 (#1082, Profilprüfung) und DA-7 (#1083, Round-Trip-Wache): abgenommen und gemergt (Zweig `r5` mit origin zusammengeführt, Kern-Filter 2 109 / 0, UI-Filter 135 / 0), Push steht an; davor DA-2 (#1076, gbXML-Export, Schritt 225) und DA-3 (#1077, IFC-Standardsätze, Nachzug IDS-Datei `694aa2bb3`); #1061 Wiki-Quellen und Logbuch-Entwurf 1.2.0.12 (abgenommen, kein Upload), #1060 MA-6 (Wiederimport bewahrt; abgenommen, Zweig `ma6` gemergt; Runde 4 im Nachgate, Push steht an), #1059 MA-5 (Ansicht als Werkzeug; abgenommen, Zweig `ma5` gemergt, Push mit Runde 3), #1058 MA-4 (Aufbauvorschläge vertieft; abgenommen, Zweig `ma4` gemergt, Push mit Runde 3), #1055 MA-3 (Hülle geschlossen; abgenommen, Zweig `ma3` gemergt, Push steht an), #1054 MA-7 (Einzonen-Vorgaben; abgenommen, Zweig `ma7` gemergt, Push steht an), #1053 MA-2 (Nutzung und Kalender; abgenommen, Zweig `ma2` gemergt, Push steht an), #1052 MA-1b (Modellprüfung Oberfläche), #1051 MA-1a (Modellprüfung Kern), #1050 GD-N (Nachzüge, NP7 70/71), #1049 DA-1 (gbXML-Import Lasten und Zeitpläne), #1048 AB4 (Ein- und Ausblenden in der Körperansicht), #1047 BK-3b/3c, #1046 GD-6a, #1032 BK-2b Oberfläche und Bänder, #1031 BK-3 Import, #1030 BK-2 (Schritt 221), #1026 GD-4, #1023 BK-1, #1020 GD-5, #1022 NP7/NP7b (Schritt 220); davor #1011 NP6, #1012 Quick Assist (Admin-Zweig), #1013 AB2, #1014 AB3, #1015 GD-1, #1016 GD-2, #1017 Entwurf BK — gepusht `f40fe2fa1`; danach Anmeldung Schritt 220 (`6870f7169`) und GD-3 mit diesem Papier (#1019) |
| CI | Kern-Lauf 38206954119 grün auf `02d4ddfe0` (enthält alles bis #1022); #1010 grün mit 38188012085 auf `228a10287`; vermerkt in #1010, #1011, #1013–#1017, #1019, #1020, #1022. #1023 (BK-1) und die Papiere danach sind mit [skip ci] gepusht und laufen mit dem nächsten Push ohne [skip ci] durch die CI |
| Referenzbasis | **R54** `Referenzlaeufe/2026-10-14_R54_Dachflaechen`, 36 Projekte (eingefroren mit #1084; R53 liegt nach `Dokumentation/ueberholt/`); R55 K6 (Anlagendialog) auf origin (8bf550004, 14.10.2026); nächste Basen R56 KB-5, R57 SA4. CI-Auswahl 1030, 1007, 1017, 1045, 1046, 1047, 1049, 1051, 1058, 1060, 1063, 1065. R52 liegt nach `Dokumentation/ueberholt/`. Reihenfolge der Folgebasen (abgestimmt am 13.10.2026): R54 = Dachflächen (erledigt, #1084), R55 = K6 (auf origin, 8bf550004, 14.10.2026), R56 = KB-5, R57 = SA4 |
| Testdatenbank | Schemastand **227** (Git LFS) |
| Schemakette | 218 `KaelteArtSchema` (Sitzung Kälteanlagen), 219 `RaumnutzungNormkategorienSchema` (NP6), 220 `RaumnutzungNormwerteSchema` (NP7); 221 `BauteilkatalogSchema` (BK-2); 222 `BauteilkatalogHaustechnikSchema` (BK-3c); 223 `TwwZonenferienSchema` (Zapfprofilgenerator), 224 `KaeltebedarfSplitAbrechnungSchema` (K6, Sitzung Anlagendialog), **225 `ZonenLastfelderSchema` (DA-2, gebaut)**, **226 `KatalogkostenSaatSchema` und 227 Brennstoffpreise/Trägerkarte (Kältebedarf, gebaut, Datenbank 227)**, 228 angemeldet, 229 BK-5/BK-6 angemeldet |
| Statuszeilen | höchste vergebene #1092 (N1 #1091, N2 #1092; #1086 und #1087 gehören anderen Sitzungen; #1085 DA-9 Wiki; #1084 Basis R54; #1083 DA-7; #1081 DA-4, #1082 DA-5; #1078 bis #1080 gehören anderen Sitzungen) (Nummern werden beim Push gegen origin geprüft; #1033 bis #1038 gehören anderen Sitzungen); vor jeder neuen Zeile Tabelle und Kopf auf Reservierungen anderer Sitzungen prüfen. Entscheide bis **E138**, **E139 frei** |
| Parallele Sitzungen | „Kälteanlagen“, „Dialoge und Korrekturen“ (Konto 3), „Administration und Support“; alle pushen auf denselben Zweig. Statuskonflikte zeilenweise lösen, Nummernkollisionen über die nächste freie Nummer |

## 2. Was die Sitzung seit dem 08.10.2026 gebaut hat

Alle Zeilen mit grünem Gate gepusht; Einzelheiten in der Statuszeile und im Protokoll unter
[`ueberholt/Protokolle/Gebaeudesimulation/`](../../ueberholt/Protokolle/Gebaeudesimulation/).

| Welle | Zeilen | Inhalt |
|---|---|---|
| UB, KM3 | #837–#878 | Übergabegrenze und Bivalenz (Referenzprojekt 1060), Teillast der Kältemaschine (1063) |
| AH V0–V8 | bis #990 | Aufheizoptimierung Fassung 2 mit berechneter Vorheizzeit (E122, E124, E125), Referenzprojekt 1065, Basis R52 |
| SA0–SA3 | #984–#995 | Schnellaufheizung (E128): Kern, Heizkurve im Vorheizfenster, Schritt 217, Oberfläche, Bericht, Export; offen SA4 |
| SQ, SQ2, EG | #996, #997, #1008 | Projektdatei-Import bleibt nicht stehen; Raumflächen werden richtig gelesen; Geste des Entwicklermodus Strg+Umschalt+F12 |
| KS | #1002 | Datum im Gemeinjahr (Tag und Monat) für Saison, Ferien, Zeiträume, Einzeltage, Perioden |
| GD-Entwurf | #1004 | [Entwurf Gebäudedialog und Mehrzonenübernahme](2026-10-11_Entwurf_Gebaeudedialog_Mehrzonen.md), F1–F8 mit E130 entschieden |
| AB, AB2, AB3 | #1010, #1013, #1014 | Farbmodi „Aufbau“/„Befund“, Zuordnung über die Legende, Baustoffwahl an der Schicht statt Abschnitt „Baustoffe“ im Import |
| NP6 | #1011 | SIA 2024 und VDI 2078 aus dem Katalog der Nutzungsprofile, zwei Wohnprofile der DIN, Schritt 219 (E129) |
| GD-1, GD-2, GD-3, GD-5 | #1015, #1016, #1019, #1020 | Gebäudekopf mit Herkunft; Gebäudewerte der Projektdatei und Name = Dateiname; Räume ohne DIN-Zone automatisch platziert; Vorbesetzung der Zonen mit Profilen |
| NP7, NP7b | #1022 | Normwerte der DIN einlesen (CSV oder Projektdatei), Außenluft je Person, Schritt 220, Dialog, Anleitung im Assistenten (E131) |
| BK-Entwurf | #1017 | [Entwurf Bauteilkatalog und Hottgenroth-Import](2026-10-12_Entwurf_Bauteilkatalog_Hottgenroth.md), Fragen F1–F8 mit E132 entschieden |
| BK-1 | #1023 | Editoren ohne Schema: Wärmekapazität immer sichtbar, Stoffgruppe als Auswahl mit Bändern, Summenfuß R_si…U/C, Kapazität im Bauteildialog |
| GD-4 | #1026 | Als Mehrzonenmodell übernehmen (Prüfsumme, Blatt, Ersetzen in einem Vorgang), Sprung Kachel Gebäudekühlung → Gruppe „Kühlung“ |
| BK-2 | #1030 | Schritt 221: Herkunft, Quellkennung, Gruppe/Klasse, Kennzeichen „meist nicht geeignet“, Felder der Quelle, Abschnitte |
| BK-3 | #1031 | „Bauteilkatalog einlesen…“ direkt aus dem CSV-Export: Leser, Regeln, Zusammenführung, Vorschau, Dialog |
| BK-2b | #1032 | Gruppe, Klasse, Herkunft und Kennzeichen in Katalogen und Bauteilwahl; Bänder des Rechenwegs ohne Ausnahme, Filter „nur geeignete“ |
| MA-2 | #1053 | Blatt „Nutzung und Kalender“ mit Mehrfachwahl: Profil zuweisen, Projektvorgabe Ferien/Heizperiode, Kalender kopieren, Sollwerte setzen, Zurücknehmen; Einstiege im Reiter „Zonen“ und im Ampelblatt (Gruppe 3); Kern `ZonenNutzungCtrl`, 81 Ressourcen |
| MA-3 | #1055 | Hülle geschlossen: Prüfung je Zone in Gruppe 1 der Modellprüfung (vier Codes `HUELLE_*`) und fehlendes Dach, Bodenplatte oder Geschossdecke als Vorschlag mit Übernahme und Rücknahme; Kern `Modellpruefung.HuelleJeZone`, `GebaeudeZonenCtrl.BauteilEinfuegen`, 10 Ressourcen `MP_*` |
| MA-4 | #1058 | Aufbauvorschläge vertieft: Rangfolge Projekt, Katalog (Typgruppe nach Namen, Aufbauklasse, U-Nähe), Typaufbau; Stufe C mit Ziel-U der Baualtersklasse; Kern `Aufbaugruppenabgleich`, `Pruefvorschlag.UBauteil`, 3 Texte geändert, 2 neu |
| MA-5 | #1059 | Ansicht als Werkzeug: Sprung aus dem Ampelblatt in die Gebäudeansicht im Farbmodus der Gruppe mit Hervorhebung der betroffenen Flächen und Zonen und Prüfzeile; Spalte „Vorschläge“ in den Legenden mit Übernehmen/Verwerfen je Zeile; Farbmodus „Nutzung“; Kern `ModellpruefungCtrl.Uebernehmen/Verwerfen`, 15 Ressourcen |
| MA-6 | #1060 | Wiederimport bewahrt: Zusammenführung über Quellkennung und Herkunft (Hand > Datei > Vorgabe), Vorschau der Unterschiede, Entfallendes nur mit Zustimmung; Kern `MehrzonenuebernahmeCtrl.Zusammenfuehren`/`Vorschau`, 37 Ressourcen `GEBWI_*`, kein Schemaschritt |
| Wiki | #1061 | Wiki-Quellen Gebäude, Gebäudeimport, Nutzungsprofile, Baustoffe und Bauteilaufbauten, Index und `help_mapping.txt`; Logbuch-Entwurf 1.2.0.12 (17 Sätze), kein Upload |
| DA-2 | #1076 | gbXML-Export von Lasten und Zeitplänen (Kalender als `Schedule`, Lasten je Zone, Rückfallweg), Rundlauf gbXML, Inventar, Schritt 225 `ZonenLastfelderSchema` |
| DA-3 | #1077 | IFC-Standardsätze lesen und schreiben (`EPOS_Zone`/`EPOS_Gebaeude`), Rundlauf IFC (43 991 Felder), Inventar IFC |
| DA-4 | #1081 | Restdaten beider Formate (Zone, Luftströme, Erdreich, Zeilenbild, benannte Wochen) über den Rückfall; IFC-Azimut der Ostwand behoben |
| DA-5 | #1082 | Profilprüfung beim Import: DIN-Nummer und gbXML-Raumtyp, Ampel Gruppe 3, `EPOS_Zone.DinNummer` |
| DA-7 | #1083 | Round-Trip-Wache: Inventartafel, „jedes Feld eingestuft“, gemeinsamer Rundlauf gbXML/IFC |
| R54 | #1084 | Basis R54 Dachflächen: Satteldach 42° der Referenzprojekte 1052 und 1054 (E141), 36 Projekte byte-gleich zu R53, Testdatenbank 83 Bauteile |
| MA-7 | #1054 | Einzonen-Vorgaben: Knopf „Vorgaben der Baualtersklasse übernehmen…“ im Gebäudeeditor mit Vorschau Jetzt/Vorgabe, Rücknahme und Sperre mit Grund; Profil der Gebäudeart über den bestehenden Generator; Kern `Einzonenvorgaben`, 23 Ressourcen |
| N1 | #1091 | Nachzüge Oberfläche: Sollwerte im Heizkalender der Zone, Zonen der Ansicht über die Kennung, Vorgaben der Baualtersklasse im Stammblatt der Verwaltung, Ampelleiste im Reiter „Zonen“, „Zone…“ aus dem Ampelblatt, Wochenkurzbild; 12 Ressourcen je Sprache |
| N2 | #1092 | Nachzüge Kern: Herkunft MANUELL und `HandfelderGeaendert` beim OK des Zonendialogs (MA-6), Periodenwochen und Höchsttemperatur in gbXML/IFC (DA-4, DA-7), `IfcAnreicherung` mit Restdaten und Erdreich |
| N3 | — | Nachzug: Gebäudeeditor liest Feiertagsland und Konditionierung nach einer Übernahme neu (Zweig `n3`, zur nächsten Runde) |
| BK-3d | — | Wiederimport des Bauteilkatalogs konvergiert in einem Lauf (Zweig `bk3d`, zur nächsten Runde) |
| DA-6 | 231 | DIN-Saat im Bau (Zweig `da6`) |
| BK-5/BK-6 | 232 | fertig im Zweig `bk5` |

## 3. Laufende Arbeit beim Schreiben

Kein Agent läuft. **GD-6a** wurde beim Kontowechsel mitten im Bau angehalten; seine Arbeit ist verworfen und wird mit dem
Auftrag in Abschnitt 4 neu beauftragt. Die Routinen des alten Kontos sind gelöscht. Festlegungen, die Folgewellen kennen müssen:

- **Außenluft je Person** (NP7) ist eine eigene Spalte `Aussenluft_Person` (m³/(h·Person)) neben `Aussenluft`/`Aussenluft_Einheit`;
  im Generator geht die raum- oder flächenbezogene Außenluft vor.
- **Bauteilkatalog** (BK-2): Gruppe und Klasse sind Code-Spalten mit CHECK (Texte zweisprachig), Abschnitte stehen in
  `Tab_Bauteilabschnitt(_STAMM)`, das Kennzeichen `Simulation_Ungeeignet` setzt nur der Kern; importierte Sätze tragen
  Herkunft IMPORT, Quelle und Quellkennung und bleiben in der Auslieferung.
- **gbXML-Konditionierung (DA-1):** Herkunft `GbxmlDatei` in `SqprojKonditionierung` mit `NennwertDatei`/`AusGbxml`; Rangfolge Profil, dann je Größe die Datei; Schemabedarf für den Rundlauf (DA-2): `Tab_Zone.Aussenluft_Person`, Licht je Zone, optional `Personen_Waerme` — Nummer vor DA-2 anmelden (225 ist für DA-2 angemeldet; 223 Zapfprofilgenerator, 224 K6 Kältebedarf).
- **Modellprüfung (MA-1a):** Kernbaustein `Modellpruefung` mit `ModellpruefungCtrl`, fünf Gruppen, Rechenstatus; MA-1b baut die Oberfläche darauf.
- **Nutzung und Kalender (MA-2):** Kern `ZonenNutzungCtrl` mit fünf Schreibwegen für n Zonen in einem Vorgang und Sitzungsgedächtnis zum Zurücknehmen; Hülle `ZonenNutzungHuelle`, Blatt `ZonenNutzungsblatt.razor`, Parametersatz `ZonenNutzungGaben` am Gebäudedialog.
- **Einzonen-Vorgaben (MA-7):** Kern `Einzonenvorgaben` (Vorschau, Anwenden, Zurücknehmen, Sperre) über `GebaeudeVorgaben` und die Bauart-Tafel `BAUART_JE_KLASSE`; `GebaeudeKatalogDaten.KlasseGesetzt` hält eine beim Laden leere Baualtersklasse fest; das Profil der Gebäudeart läuft über die Naht `KonditionierungWeg.ProfilDerGebaeudeart`. Kein Schemaschritt, kein SQL.
- **Hülle geschlossen (MA-3):** Gruppe 1 der Modellprüfung prüft je Zone über `Zonenkopplungsregeln.Bilanz` mit den Codes `HUELLE_OBEN_FEHLT`/`HUELLE_UNTEN_FEHLT` (rot), `HUELLE_OFFEN` und `HUELLE_GESPIEGELT` (gelb); die gemeinsame Schwelle (10 %) steht in `Zonenkopplungsregeln.SenkrechtOffen`. `Vorschlagsart.Huellbauteil` (`Modellvorschlagsregeln.Huellbauteil`) schlägt das fehlende Bauteil an der Nachbarzone mit der Lückenfläche vor, Übernahme und Rücknahme über `ModellpruefungCtrl`, Einfügen über `GebaeudeZonenCtrl.BauteilEinfuegen`. Kein Schemaschritt, kein Eingriff in den Rechenweg.
- **Aufbauvorschläge (MA-4):** Kern `Aufbaugruppenabgleich` (Synonymtafel, vier Typgruppen, N4/N5, längstes Synonym gewinnt) ordnet Bauteil- und Aufbaunamen einer Typgruppe zu; Rangfolge Projektaufbau, Katalogaufbau im U-Band ±25 % (Typgruppe, Aufbauklasse, U-Nähe, Katalogfolge), Typaufbau; in Stufe C liefert die Baualtersklasse über `GebaeudeVorgaben.Wert` das Ziel-U, `Pruefvorschlag.UBauteil` trägt es, `ModellpruefungCtrl.AufbauAnwenden` schreibt U und Aufbau in einem UPDATE, die Rücknahme stellt beide zurück. Keine Oberfläche, kein Schemaschritt.
- **Ansicht als Werkzeug (MA-5):** `Modellpruefungsblatt.AnsichtGruppe` reicht die Gruppe an den Gebäudedialog, der die Ansicht im Farbmodus der Gruppe öffnet und `GebaeudeAnsicht.Pruefung` setzt (Befundzeilen mit `IdZone`/`IdBauteil`; Bauteile über `Tab_Importzuordnung`, Zonen über den Namen). `ModellpruefungCtrl.Uebernehmen(gruppe, auswahl)` und `Verwerfen(gruppe, auswahl)` arbeiten je Auswahl (Verwerfen nur im Sitzungsgedächtnis, schreibt nichts); Neulesen über `GebaeudeNeulesestand.AnsichtNeuLesen`; der Farbmodus „Nutzung" kommt aus `ZonenNutzungCtrl.Lesen` über `GebaeudeNeulesenHuelle.MitNutzung`. Kein Schemaschritt, kein SQL, Rechenweg unberührt.
- **Wiederimport (MA-6):** `MehrzonenuebernahmeCtrl.Zusammenfuehren` (in `MehrzonenuebernahmeCtrl.Zusammenfuehrung.cs`) führt in einem Vorgang (`Vorgangsklammer`, Rückweg bei Fehler) über Quellkennung und Herkunft zusammen: Ids bleiben, je Feld Hand vor Datei vor Vorgabe, Sätze ohne Quellkennung bleiben, Entfallendes nur mit Zustimmung; `MehrzonenuebernahmeCtrl.Vorschau` liefert die Unterschiede; Hülle `GebaeudeMehrzonenHuelle` mit Gabe `Wiederimport`, Baustein `EPOS.UI/Dialoge/Import/WiederimportVorschau.razor`; `ZonenNutzungCtrl` setzt `Herkunft = MANUELL` (auch im Ferienweg). Kein Schemaschritt (229 mit `Tab_Konditionierungskalender.Herkunft` und `Tab_Bauteil.U_Herkunft` nur benannt).
- **Restdaten (DA-4):** `GebaeudeRestdaten` (Kern, `EPOS.Kern/Allgemein/Import/Gebaeude/`) trägt Vorheizzeit, Höchstleistungen, Übergabe und Kühlübergabe je Zone, Luftströme, Umfang, wirksamen U-Wert am Erdreich, Kellertemperatur, Zeilenbild und benannte Wochen; gbXML-Rückfall (Weg 2) ist der Block `EPOS:` mit Schlüssel=Wert in `Description`, IFC die Sätze `EPOS_Zone`/`EPOS_Gebaeude`; der Leser übergeht unlesbare Werte benannt. Kein Schemaschritt.
- **Profilprüfung (DA-5):** `GbxmlRaumtyptafel` (Kern) bildet `spaceType` auf Profile ab; `Raumnutzungsvorbelegung.DinTreffer` löst die DIN-Nummer über die Profile der Kategorie DIN/TS 18599-10 auf; IFC trägt `EPOS_Zone.DinNummer`; Ampel in Prüfgruppe 3 nach Herkunft des Profils (`ZONE_OHNE_PROFIL` gelb, die Zone erbt vom Gebäude).
- **Round-Trip-Wache (DA-7):** `Datenaustauschinventar` (Tafel der EPOS-Felder mit Welle und Grund, `EPOS.Kern.Tests`) und `DatenaustauschRundlaufWacheTests`: jedes Feld eingestuft, Verlustliste ↔ Tafel über `Abstimmung`, gemeinsamer Rundlauf (je Format 70 661 Felder). Eine neue Spalte braucht einen Eintrag in der Tafel, ein neuer Weg einen in der Kerntafel `DatenaustauschInventar`.
- **Maskenschlüssel `Projektgebaeude`** (GD-N) öffnet unter Windows `GebaeudeFenster.ProjektOeffnen`, auf iOS benannte Ablehnung.
- **Haustechnik:** Klasse `HAUSTECHNIK` (Schritt 222) ist die letzte Klasse der Gruppe Sonstige an Baustoff und Bauteilaufbau; der Import setzt sie für Basis-Codes 14, 19, 29, 30.

## 4. Nächste Arbeit (Reihenfolge)

Erledigt am 13.10.: Wiki-Quellen und Logbuch-Entwurf (#1061), MA-6 (#1060), CI-Vermerk, GD-6a (#1046), BK-3b/3c (#1047), AB4 (#1048), DA-1 (#1049), GD-N (#1050), MA-1a (#1051), MA-1b (#1052), MA-2 (#1053), MA-3 (#1055), MA-7 (#1054), MA-4 (#1058), MA-5 (#1059), DA-4 (#1081), DA-5 (#1082), DA-7 (#1083).

1. **Erneuter Importlauf beim Anwender** nach BK-3b/3c („Bauteilkatalog einlesen…“ ordnet per Quellkennung um, keine Dubletten);
   Zählungen laut Statuszeile #1047 ins Protokoll.
2. ~~MA-1 Geführter Modellaufbau~~ erledigt mit MA-1a (#1051) und MA-1b (#1052), nach dem [Entwurf Geführter Modellaufbau](2026-10-12_Entwurf_Gefuehrter_Modellaufbau.md)
   (E137, F1–F8 nach Empfehlung entschieden): Modellprüfung in fünf Gruppen mit Rechenstatus, „Alle Vorschläge übernehmen“,
   Profilregel, Sprung in die Ansicht. Kern in MA-1a, Oberfläche (Ampelblatt, „Alle Vorschläge übernehmen“, Sprung in die Ansicht) in MA-1b.
3. ~~DA-2 (gbXML-Export, mit Schemaschritt für den Rundlauf), DA-3~~ **erledigt** mit DA-2 (#1076, Schritt 225) und DA-3 (#1077) (weiter nach Abschnitt 7 des
   [Konzepts Datenaustausch Simulationsdaten](2026-10-12_Konzept_Datenaustausch_Simulationsdaten.md)) . DA-4, DA-5 und DA-7 erledigt (Punkt 7).
4. ~~NP7-Nachzug~~ erledigt mit GD-N (#1050): Nummern 70/71 auf die Wohnprofile, nur beim Einlesen.
5. ~~GD-Nachzüge~~ erledigt mit GD-N (#1050): gespeicherter Zonenplan, Projektdatei-Befund, Sprung Kühlung, Jahreswärmebedarf.
6. ~~MA-3 bis MA-7~~ **alle MA-Wellen erledigt (MA-2 bis MA-7)**, nach Wellenplan des Entwurfs (MA-5 setzt an der Sichtlegende von AB4 an): MA-2 (#1053), MA-3 (#1055), MA-4 (#1058), MA-5 (#1059), MA-6 (#1060) und MA-7 (#1054) erledigt; MA-6 (#1060) erledigt, damit alle MA-Wellen (MA-2 bis MA-7) erledigt; kein Schemaschritt (229 bleibt frei). Nachzüge aus MA-6: ein nur im Kalendereditor geänderter Kalender markiert die Zone nicht; Hand-U ohne Aufbauwechsel wird überschrieben; Zonenfelder im Zonendialog ohne MANUELL nimmt die Datei; Aufbauten mit Namensgleichheit „(2)“; der Gebäudekalender im Einzonenweg ersetzt die Datei weiterhin. Nachzüge aus MA-2: Ferien aus den Kalenderperioden, Sollwertweg bei Heizkalender (Zeilen `TAG`/`NACHT`), Kurzbild der Woche je Größe, Sprung „Zone…“ aus dem Ampelblatt. Nachzüge aus MA-3: `HUELLE_OFFEN` erst ab zwei Zonen; gespiegelte Bauteile ohne Vorschlag („Neigung spiegeln“ passt zu MA-5); geneigte Dächer von 1052/1054 bleiben gelb; der Typaufbau bleibt nach Rücknahme nur der Gruppe 1 ungenutzt im Projekt. Nachzüge aus MA-7: leere Baualtersklasse bis zum Speichern erkennbar halten (echte Wahl „keine Klasse“), Bauart-Tafel (siehe Abschnitt 5), Knopf im Stammblatt der Gebäudeverwaltung, Wiki-Seite „Gebäude“. Nachzüge aus MA-4: Baualtersklasse je Katalogaufbau (bräuchte einen Schemaschritt); Schichtnamen der Quelldatei beim Import am Bauteil mitgeben (`Abgleichen(IEnumerable<string>)` steht bereit); die Wahl „Klasse und Bauart einmal in Gruppe 2“ (Oberfläche). Nachzüge aus MA-5: Windows-Sichtabnahme; die Ansicht braucht weiter die Importdatei (Ansicht des gespeicherten Modells ohne Datei); Ampelleiste im Reiter „Zonen“; Verwerfen nur je Sitzung, nicht einzeln aufhebbar; Zonen über den Namen zugeordnet, umbenannte Zonen werden nicht gefunden; eingecheckte Probe für das Körpermodul im Rasterprobe-Wirt. **Nachzüge aus MA-1b, MA-2, MA-5, MA-6, MA-7 erledigt mit N1 (#1091) bzw. N2 (#1092).** Verbleibend: MA-6 (b) Hand-U ohne Aufbauwechsel markiert nicht, Kalenderschreibwege außerhalb des Zonendialogs markieren nicht, die Übernahme der Gruppe 4 (Nordrichtung) liest die Hüllfelder im Editor nicht neu — in Arbeit als N3.
7. ~~DA-4, DA-5, DA-7~~ **erledigt** (#1081, #1082, #1083). **DA-6** (DIN-Saat) im Bau: Lieferung der DIN-Werte am 14.10.2026 als NP7-CSV (45 Profile, Fassung 2025-10, zwei Lesewege 0 Abweichungen, Gegenprobe 45/45), Schemaschritt 231 `RaumnutzungDinWerteSchema` angemeldet (hängt an 230 KBP-2); offen E143 (Wochenmuster); **DA-8** bleibt offen (optional); **DA-9** Wiki (erledigt bis auf den Upload, #1085); BK-5/BK-6 fertig im Zweig `bk5` (Schemaschritt 232 nach Tausch mit KB-5, hängt an 231; Merge nach 230). Schemaschritt erst bei **Nachzüge DA-4/DA-7 erledigt mit N2 (#1092)** bis auf Feiertagsperioden (reisen ohne Verweis) und das Gebäudefeld `Maximaleraumtemperatur` (benannter Verlust). **DA-6:** Lieferung der DIN-Werte am 14.10.2026 erfolgt, die CSV ist in Übertragung (lokale Sitzung); Beginn nach Eingang der CSV.
   Baubeginn anmelden (223 Zapfprofilgenerator, 224 K6 Kältebedarf, 225 DA-2, 226/227 Kältebedarf gebaut, 228 KB-4 und 229 KB-5 Kältebedarf, 230 KBP-2 Dialoge, 231 DA-6 und 232 BK-5/BK-6 wir — alle angemeldet, Kette in dieser Reihenfolge).
   Nachzüge der drei Wellen: DA-4: `IfcAnreicherung` schreibt die Restdaten nicht in eine Originaldatei, Erweiterungspunkt gbXML Weg 1 ungeprüft (Schemakopie fehlt), der Verweis `ID_Woche` einer Periode geht verloren; DA-5: Zuordnungsart `GBXML_RAUMTYP` bräuchte einen Schemaschritt (CHECK der Spalte `Art`), Tafelnamen gegen die Schemakopie `spaceTypeEnum` ungeprüft, fremde IFC-Eigenschaften mit DIN-Nummer werden nicht gelesen; DA-7: B10 (Höchsttemperatur) offen, Verlustliste für die übrigen Abstimmungseinträge nachführen.
8. ~~Basis R54 Dachflächen (E141)~~ **erledigt** mit #1084 (Basis `Referenzlaeufe/2026-10-14_R54_Dachflaechen`; SA4 bleibt R57). Ursprünglicher Auftrag: Neigung der Dächer von 1052 und 1054 berichtigen, Referenzlauf 30 Projekte, Basis
   `Referenzlaeufe/<Datum>_R54_Dachflaechen` einfrieren, LIESMICH der Referenzläufe mit Begründung, Statuskopf; erst nach R53 auf origin;
   kein neues Projekt anlegen; Nummern ≥ 1075 sind noch zu messen (vor jeder neuen Zeile höchste Nummer prüfen).
9. **Wiki und Logbuch** der Wellen GD, AB, NP6/NP7, KS, BK, DA-1, MA-1 bis MA-7: erledigt bis auf den Upload (Quellen und Logbuch-Entwurf 1.2.0.12 mit #1061; Upload auf Zuruf, Version beim Anwender bestätigen); Seiten (Seiten „Gebäude“, „Gebäudeimport“, „Nutzungsprofile“, „Baustoffe“,
   „Bauteilaufbauten“, „Bauteilkatalog einlesen“); Upload nur gebündelt und auf Zuruf, Versionsnummer beim Anwender erfragen.
10. **BK-5 bis BK-7** (Entwurf Abschnitte 6a und 7): inhomogene Schichten nach DIN EN ISO 6946 (Abschnitte in Editor,
   Projektkopie `CopyFromStamm`, Katalogpaket), Fenster ans Bauteil, Teilkatalognamen aus `Katalog_Inhalt.csv`, Hilfeschlüssel
   des Importdialogs; BK-4 LocalDB-Werkzeug nur für einen neuen Katalogstand.
11. **SA4:** Referenzprojekt mit Schnellaufheizung, Basis R57, Wiki (Stufentabelle SA).

## 5. Offen beim Anwender

- ~~Lieferung der DIN/TS-18599-10-Werte~~ erfolgt 14.10.2026 (PDF lokal, Übertragung in NP7-CSV durch die lokale Sitzung); offen **E143**: Wochenmuster der DIN-Profile — (a) aus den Nutzungstagen je Jahr ableiten (365 → alle Tage, 300 → Mo–Sa, sonst Mo–Fr; Empfehlung) oder (b) leer lassen
- **Erneuter Importlauf Bauteilkatalog** mit Zählungen (Abschnitt 4 Punkt 1): läuft lokal nach E142 (Werte in den Katalog übernehmen, Sicherung vorher); gelaufen 14.10.2026: Wiederimport konvergiert nach zwei Übernahmen, 7 549 Baustoffe, Befund BK-3d behoben im Zweig `bk3d`.
- **BK-1, Katalogprobe:** `Proben/Rasterprobe` Katalogprobe meldet 8 von 66 Fällen mit Überlagerung (Kessel, BHKW, Wärmepumpe,
  Klimadaten) — gegen den Stand vor BK-1 prüfen, ob vorbestehend (kein Baustofffall betroffen).
- **Sichtabnahmen unter Windows:** gesperrte Farbmodusknöpfe mit Grund (#1010), Legendenzuordnung (#1013), Schichtwahl im
  Aufbau (#1014), Herkunftsmarken und Rundung im Gebäudekopf (#1015), Namensvorschlag „Übernehmen“ im Importdialog (#1016),
  Marke und „Zurücknehmen“ der automatisch platzierten Räume (#1019), Saisondatum (#1002), Geste Strg+Umschalt+F12 (#997),
  Kopfleiste des Gebäudedialogs mit drei Knöpfen (#1046), Körperansicht Ein-/Ausblenden (#1048), Zonenplan-Vorbelegung, Projektdatei-Befund, Jahreswärmebedarf und Sprung Kühlung (#1050), Kopfleiste mit zwei Knöpfen („Simulation…“, „Modell prüfen…“) und Ampelblatt der Modellprüfung (#1052), Blatt „Nutzung und Kalender“ mit Mehrfachwahl (#1053), Vorschau der Vorgaben der Baualtersklasse (#1054), Prüfzeile und Spalte „Vorschläge“ der Gebäudeansicht bei schmaler Breite, Dimmung in beiden Farbschemata und Kontrastmodus (#1059), Vorschau des Wiederimports (#1060), Rückfrage vor dem Ersetzen eigener Ferien im Blatt „Nutzung und Kalender“ (E139), Export und Import der Restdaten (DA-4: Mehrzonengebäude mit Luftströmen nach gbXML und IFC ausgeben und im Importdialog wieder einlesen), Ampel der Prüfgruppe 3 mit Herkunft des Profils (DA-5).
- **SQ2:** gegen welchen Wert der Anwender die Raumgrößen des Sportheims verglichen hat.
- **GD-2:** fachliche Bestätigung Gebäudetyp 3 = Nichtwohngebäude und Code 0 = keine Angabe an weiteren Projektdateien.
- **Hilfeseite „Gebäude“:** Abschnitt Modellprüfung mit Anker (Wiki-Quelle), danach `help_mapping.txt` — mit dem Wiki-Auftrag.
- **Modellprüfung:** Bauteilart `SONSTIGES` in der Modellprüfung gelb lassen oder als bekannt werten? Empfehlung: gelb lassen, bis die Art gepflegt ist.

## 6. Arbeitsweise in der Cloud

- **Anwenderdateien** (`Quellen/*.sqproj`, Hottgenroth-Katalog `.mdf`) liegen nie im Repositorium; Namen und Werte daraus nie in
  Code, Tests, Protokollen oder Nachrichten — nur Struktur, Zählungen, Anteile. Katalogwerte eines Herstellers nur in die
  Datenbank des Anwenders, nie in die Auslieferung. **Normwerte:** allein die DIN/TS-18599-10-Profile nach E135 kommen ins
  Repositorium und in die Auslieferung, und nur aus der Lieferung des Anwenders; alle anderen Normwerte bleiben tabu. Google-Drive-Freigaben des Anwenders lassen sich mit
  `curl -L "https://drive.usercontent.google.com/download?id=<ID>&export=download&confirm=t"` laden (`drive.google.com` selbst
  ist gesperrt); große Dateien (> 5 GB) sprengen das Plattenkontingent des Containers.
- **Agenten** mit ausdrücklichem Modell (`opus-umsetzung` für Fachcode, `sonnet-mechanik`, `haiku-pruefung`), je Welle ein
  Worktree, Commits auf dem eigenen Zweig, kein Push. Die Orchestrierung mergt in einem Integrations-Worktree, fährt das Gate
  und pusht.
- **Gate nach dem Merge:** `python3 Werkzeuge/ResourceDesigner/designer_neu.py`, `python3 Werkzeuge/SqlDialektPruefer/pruefer.py
  --db Referenzlaeufe/Kenndaten_Test.sqlite`, Bau `WP-Plan.Kern.slnf` (Release) und Windows-Schale mit
  `-p:EnableWindowsTargeting=true`, Referenzlauf der zwölf CI-Projekte gegen R52 (FAIL nur für nicht gerechnete Projekte ist
  erwartet), `EPOS.UI.Tests` voll, `EPOS.Kern.Tests` mit `--filter` auf die betroffenen Klassen, Werkzeugtests
  `Werkzeuge/Auslieferungsvorlage`, `Formularkarte`, `Gebaeudevergleich` — immer mit
  `-- xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2`. Die CI rechnet den Kern-Filter **ohne** Filter:
  Tests, die feste Zahlen der Kataloge erwarten, auf Konstanten der Saat beziehen.
- **Ressourcenkonflikte** beim Merge (`.resx`, Anhänge am Dateiende): beide Seiten behalten — zuerst die eigene, dann ein
  `</data>`, dann die fremde Seite, wenn hinter dem Konfliktblock das schließende `</data>` steht; danach
  `designer_neu.py schreiben` und die Namen auf Dubletten prüfen.
- **Testdatenbank** nach Merges oft als LFS-Zeiger: `git lfs pull --include=Referenzlaeufe/Kenndaten_Test.sqlite`.
- **Push:** `git -c "lfs.https://github.com/inekon/EPOS-Plan.git/info/lfs.locksverify=false" push -q origin
  HEAD:refs/heads/<zweig>` auf `ios_migration_september` und den Sitzungszweig; den doppelten Kern-Lauf des Sitzungszweigs
  abbrechen. Reihenfolge einer Welle: Merge → Gate → Statuszeile und Protokoll → Push → CI-Vermerk `[skip ci]`.
- **Modellregel (Anwender, 13.10.2026):** Fable 5.1 nur für Orchestrierung und sehr komplexe Analysen; Fachcode, Schema, Tests und Fachkonflikte Opus 5.5; Merges ohne Fachkonflikt, Gates, Nachweise, Status, Protokolle, Wiki und CI-Auswertung Sonnet; Zählungen und Prüfungen Haiku; Check-ins lösen Agenten aus.
- Keine macOS-, iOS- oder Setup-Läufe, kein Wiki-Upload, keine Pull Requests, kein Force-Push und keine Tags ohne Rückfrage.

## 7. Einstiegs-Prompt

```text
Sitzung „Gebäudesimulation“ (EPOS-Plan, inekon/EPOS-Plan) auf neuem Konto fortsetzen. Arbeitszweig ios_migration_september;
der eigene Sitzungszweig wird bei jedem Push mitgezogen.

1. Lies CLAUDE.md und das Übergabepapier
   Dokumentation/aktuell/Gebaeudesimulation/2026-10-12_Uebergabe_Gebaeudesimulation_Kontowechsel.md ganz; aus
   Dokumentation/aktuell/Status_iOS_Migration.md nur den Kopf (Schemaanmeldung) und die Zeilen ab #1046 (#1034 gehört der Sitzung Kälteanlagen; höchste eigene #1084); aus
   Dokumentation/aktuell/Status_Gebaeudesimulation_VDI6007.md die Entscheide E135–E138. Weitere Papiere nur gezielt.
2. Prüfe mit git log --format='%h %<(72,trunc)%s' -n 20 origin/ios_migration_september, was seit dem Papier hinzukam.
3. Arbeite Abschnitt 4 der Reihe nach ab: Importlauf beim Anwender, MA-1 (erledigt, MA-1a und MA-1b), DA-2 und DA-3 (erledigt, #1076/#1077), DA-4, DA-5, DA-7 (erledigt, #1081 bis #1083),
   MA-3 bis MA-7 (alle MA-Wellen erledigt; offen DA-6, DA-8, DA-9, BK-5 bis BK-7, SA4), DA-6 (im Bau, 231), Wiki, BK-5 ff., SA4.
4. Arbeitsweise: Orchestrierung mit Agenten (opus-umsetzung für Fachcode, sonnet-mechanik für Status/Protokolle, Modell immer
   ausdrücklich setzen), je Welle ein Worktree; Merge im Integrations-Worktree, Gate nach Abschnitt 6, Statuszeile (nächste
   freie Nummer vor dem Push gegen origin prüfen — andere Sitzungen vergeben laufend Nummern), Push auf
   ios_migration_september und den Sitzungszweig; doppelten Kern-Lauf des Sitzungszweigs abbrechen; reine Papier-Pushes mit
   [skip ci], damit ein laufender CI-Lauf nicht abbricht. Übergabepapier bei jedem Push nachführen.
5. Grenzen: keine macOS-, iOS- oder Setup-Läufe, kein Wiki-Upload, keine Pull Requests, kein Force-Push ohne Rückfrage.
   Anwenderdateien (.sqproj, .mdf, CSV-Export) nie ins Repositorium; Normwerte nur die DIN/TS-18599-10-Profile nach E135
   (aus der Lieferung des Anwenders), alles andere bleibt tabu; aus Anwenderdateien nur Struktur und Zählungen.
   Die lokale Sitzung „iOS migration merge conflicts“ auf dem Rechner des Anwenders kann den Hottgenroth-Export auswerten
   (nur Kategorien und Zählungen zurückmelden lassen).
6. Offene Anwenderfragen (Abschnitt 5) gesammelt mit Empfehlung vorlegen; kurze Antworten in Stichpunkten, Tabellen für
   Stände.
```

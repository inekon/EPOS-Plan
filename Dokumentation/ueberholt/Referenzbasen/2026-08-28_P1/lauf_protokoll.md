# Referenzlauf-Protokoll

**Zeitpunkt:** 27.08.2026 22:58:41

**Quelle (produktiv, nur gelesen):** `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`

**Arbeitskopie (beschrieben):** `C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb`

**Zielordner:** `C:\Users\Dirk\AppData\Local\Temp\claude\C--Waermeplan-WP-Plan-WindowsFormsApplication1\1357174a-9cda-442b-b001-abe215ed7b64\scratchpad\P1_Gegen`

**Gesamtdauer:** 00:00:17  |  **Timeout je Projekt:** 300 s

**Warnungen:** 30  |  **Fehler:** 0

## Projekte

| ID | Projekt | Ausstattung | Auswahlgrund | Dauer | CSV | Status |
|---|---|---|---|---|---|---|
| 1007 | Laurentiuskirche | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:01 | 29 | OK |
| 1008 | Heinestr 15 | Tools: Wärmepumpe / Anlagen: WP,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:01 | 21 | OK |
| 1011 | test1 | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:01 | 29 | OK |
| 1017 | WP_PV-Speicher | Tools: BHKW, Heizkessel, Stromspeicher / Anlagen: WP,Batterie,Kessel,BHKW | per --projekte vorgegeben | 00:00 | 21 | OK |
| 1018 | BHKW Test München | Tools: BHKW, Heizkessel / Anlagen: Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:00 | 22 | OK |
| 1021 | TestSpeichernUnter | Tools: Wärmepumpe / Anlagen: WP,Puffer / Quellspeicher(WP) | per --projekte vorgegeben | 00:00 | 21 | OK |
| 1023 | Wöhler - Test1 | Tools: Wärmepumpe, Heizkessel / Anlagen: WP,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:01 | 25 | OK |
| 1024 | Wöhler - Test2 | Tools: Wärmepumpe, Heizkessel, BHKW / Anlagen: WP,Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:01 | 26 | OK |
| 1030 | Referenz BHKW-Kaskade (Regressionstest) | Tools: BHKW, Heizkessel / Anlagen: Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:01 | 22 | OK |
| 1039 | Mehrgebäude | Tools: Heizkessel, Wärmepumpe / Anlagen: WP,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:01 | 25 | OK |
| 1040 | zwei Puffer je Kanal | Tools: Wärmepumpe, Heizkessel, Photovoltaik, Stromspeicher / Anlagen: WP,PV,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:01 | 30 | OK |
| 1041 | Prozesswärme mit eigenem Puffer | Tools: Wärmepumpe, Heizkessel, Photovoltaik, Stromspeicher / Anlagen: WP,Kessel,Puffer | per --projekte vorgegeben | 00:01 | 27 | OK |
| 1042 | Booster-Kette mit Kombi-Speicher | Tools: Wärmepumpe, Heizkessel, Solarthermie, Photovoltaik, Stromspeicher / Anlagen: WP,Kessel,Puffer | per --projekte vorgegeben | 00:01 | 31 | OK |

## Ablauf

```
Referenzlauf gestartet.
Projektwurzel: C:\Waermeplan\WP_Plan
Zielordner:    C:\Users\Dirk\AppData\Local\Temp\claude\C--Waermeplan-WP-Plan-WindowsFormsApplication1\1357174a-9cda-442b-b001-abe215ed7b64\scratchpad\P1_Gegen
Timeout je Projekt: 300 s

Quelle gefunden (ProgramData): C:\ProgramData\EPOS_PLAN\Kenndaten.accdb
Arbeitskopie angelegt: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb (144 MB)
DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb

Schema-Migration der Arbeitskopie ...
  C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
  Zeitpunkt: 27.08.2026 22:58:24
  Schemastand vorher: 51   (Zielstand 53)
  Bootstrap Schemamarker Tab_Applikation.SchemaVersion: OK
  Schritt 1  Spalten in Tab_Energieanlagen (Konzept 5.3): bereits erledigt
  Schritt 2  Spalten in Tab_Pufferspeicher, Tab_Klimaregion und Tab_Einstellungen (Konzept 5.1/12): bereits erledigt
  Schritt 3  Ergebnistabelle Tab_ErgebnisPufferspeicher (Konzept 6.6): bereits erledigt
  Schritt 4  Beziehungen der Pufferspeicher (Konzept 5.3 / B0-6b): bereits erledigt
  Schritt 5  Datenmigration Quellen/Senken (Konzept 5.5): bereits erledigt
  Schritt 6  Feature-Flag Kaskade_Zweikanalig in Tab_Einstellungen (Konzept Kapitel 9): bereits erledigt
  Schritt 7  Vorbelegung Extrapolation_erlaubt in Tab_Einstellungen (Konzept 13.4): bereits erledigt
  Schritt 8  Energieträger-Verweis ID_Carrier in Tab_Energieanlagen: bereits erledigt
  Schritt 9  Quellpuffer-Fremdschlüssel WQ_ID_Puffer (Etappe E0, Regel R7): bereits erledigt
  Schritt 10  Ergebnisspalte Quellwaerme in Tab_ErgebnisHeizkessel (Etappe D4): bereits erledigt
  Schritt 11  Stromspeicher: Gerätespalten, Tab_StromspeicherVariante, Tab_ErgebnisStromspeicher, Ladeparameter (AP3): bereits erledigt
  Schritt 12  Preismodell: Aufschlagsspalten, Tab_Preisreihe(Daten), Tab_Kostenprofil, Vorbelegung (AP4): bereits erledigt
  Schritt 13  BHKW-Regulär: Spalte Schwelle_Reserve, Vorbelegung 10 %, Leistungsgrenze 30 %: bereits erledigt
  Schritt 14  Parallelverbund: Tabelle Z_AnlagePufferVerbund samt Index und Beziehungen: bereits erledigt
  Schritt 15  Kessel-Wartungseinheit: Spalte Wartungskosten_Einheit, Vorbelegung €/a: bereits erledigt
  Schritt 16  Anlagenzeilen-Eindeutigkeit: Indizes auf (ID_Projekt, ID_WP | ID_Kessel | ID_BHKW | ID_PUFFER): bereits erledigt
  Schritt 17  Doppelt belegte Anlagenzeilen in eigene Gerätekopien überführen: bereits erledigt
  Schritt 18  BHKW-Vollbenutzungsstunden: VbhElektrisch in Tab_ErgebnisBHKW, VbhThermisch und VbhElektrisch in Tab_ErgebnisBHKWModul (Etappe E2): bereits erledigt
  Schritt 19  Kostenposition: Kostenart, Bemessung, IstErloes, Menge und Einheitpreis in Tab_ProjektWerte, Vorbelegung BETRAG (Etappe E3): bereits erledigt
  Schritt 20  Steuerangaben: Unternehmensart, raeumlicher Zusammenhang, Hocheffizienz, Jahresnutzungsgrad, Wahl der Energiesteuerentlastung und Aufteilungsmethode in Tab_ProjektWirtschaftlichkeit (Etappe E4): bereits erledigt
  Schritt 21  Tarifmodell: Rollen Bezug/Reststrom/Einspeisung mit drei Leistungspreismodellen in Tab_ProjektTarif, Aufschlagsschalter und KWK-Einspeiseverguetung in Tab_ProjektWirtschaftlichkeit, Vorbelegung ZONEN (Etappe E5): bereits erledigt
  Schritt 22  KWK-Zuschlag je Modul: Stichtag, Inbetriebnahme, Anlagenart, Eigenstromfall, Zuschlagssaetze, Vbh-Kontingent und Jahresdeckel in Tab_Energieanlagen (Etappe E6): bereits erledigt
  Schritt 23  Bilanzierung: Bilanzjahr, Bewertungsmethode KWK-Strom, Biomasse-Konvention und Nachhaltigkeitsnachweis in Tab_ProjektWirtschaftlichkeit, Vorbelegung KATALOG / NULLANSATZ / NACHWEIS_JA (L12 und L13): bereits erledigt
  Schritt 24  Doppelte Katalogeinträge aus dem zweiten Importlauf entfernen: bereits erledigt
  Schritt 25  Einheiten-Konsistenz: Tabelle energy_conversion sicherstellen, Spalten faktor_name und aktiv, Vorbelegung z-Faktor / Umrechnungsfaktor und aktiv = WAHR (Etappe K2, HF2/M-A): bereits erledigt
  Schritt 26  Einheiten-Seeds: Nm³ als Abrechnungseinheit der Gasträger, z-Faktor m³ → Nm³ mit 1,0, Namensberichtigung der Identitätsregeln (Etappe K3, HF3/M-B): bereits erledigt
  Schritt 27  Komponentenkatalog: Wärmezentrale, Bauliche Anlagen und Stromeinspeisung in Tab_KostenKomponente, Haupt- und Nebenpositionen in Tab_Kostenfaktor (Etappe K5, HF5/M-C): bereits erledigt
  Schritt 28  KWKG-Angaben: Tatbestand § 6 Abs. 3, Anlagenart § 8, Kostenanteil und Pauschalmodus § 9 in Tab_ProjektWirtschaftlichkeit; CO2-Preispfad ab 2028 auf 80 €/t (Etappe K6, HF6/M-D): bereits erledigt
  Schritt 29  Alttabellen entfernen: Beziehungen, dann DROP von Tab_Brennstoff_Projekt, energy_unit, energy_group, Tab_KostenKategorie, Tab_KWKG_Staffel, Tab_BHKW_neu und Tab_BHKW_Einf; Kategorie-3-Altzeilen in Tab_ProjektWerte loeschen (Etappe K6, HF1/M-E, Entscheidung E3): bereits erledigt
  Schritt 30  Doppelte Katalogeintraege in allen Katalogen der Registry entfernen (Dublettenpruefung D4): bereits erledigt
  Schritt 31  Eindeutiger Index auf die Namensspalte jedes Katalogs (Dublettenpruefung D5): bereits erledigt
  Schritt 32  Gespeicherte Abfragen nachziehen: Abfrage_Kostenfaktoren ohne Tab_KostenKategorie neu schreiben, Abfrage_ProjektKostenInvestBetrieb (Entscheidung E4), Abfrage1 und Tab_BHKW_Einfügen_Test entfernen (Nachzug zu Schritt 29): bereits erledigt
  Schritt 33  Leseprobe auf Abfrage_Kostenfaktoren; bei Bedarf mit ausgeschriebenem Kategorie-Ausdruck im ORDER BY neu schreiben (Nachzug zu Schritt 32): bereits erledigt
  Schritt 34  Verwaiste Geraetezeilen entfernen: Zeilen in Tab_WP, Tab_Heizkessel, Tab_BHKW, Tab_Pufferspeicher, Tab_PV, Tab_Solarkollektoren und Tab_Stromspeicher, auf die keine Anlagenzeile desselben Projekts mehr zeigt (Befund 22.08.2026): bereits erledigt
  Schritt 35  Gespeicherte Abfragen, zweiter Durchgang: Abfrage_Heizkessel_Kosten und Abfrage_Neues_Kosten_Model entfernen (fachlich tot), Abfrage_SST, Abfrage_Kuehlung_MaxLast und Abfrage_KenndatenKuehlung_Max auf die heutigen Spaltennamen bringen (Nutzerentscheid 22.08.2026): bereits erledigt
  Schritt 36  Gespeicherte Abfrage Abfrage_Energietraeger_Effektiv anlegen, falls sie fehlt (K6-Nachtrag, Protokoll Abschnitt 12): bereits erledigt
  Schritt 37  BHKW-Kosten abgleichen: Investition_kwel aus den fuenf Einzelposten nachziehen und, wo nur der spezifische Wert gepflegt ist, daraus Kosten_Modul ableiten - in Tab_BHKW und Tab_BHKW_STAMM (Befund 23.08.2026): bereits erledigt
  Schritt 38  Kostenvorlagen-Strukturen: Tab_KostenVorlage/-Position anlegen, Tab_ProjektWerte um VorlageID/StartJahr und energy_carrier um price_power/price_power_modus ergaenzen (Etappe KD1): bereits erledigt
  Schritt 39  Auslieferungsvorlagen saeen: 20 Standardvorlagen (10 Komponenten x Investition/Betrieb) mit den Positionslisten der Vorlagen-Folien, Saetze bewusst leer (Etappe KD1): bereits erledigt
  Schritt 40  Leistungspreis-Reihen: Tab_Preisreihe um ID_Energietraeger ergaenzen (Etappe KD4, FK6a): bereits erledigt
  Schritt 41  PV-Verguetung: Tab_ProjektPhotovoltaik anlegen und Marktwert-Solar-Monatsreihen 2024/2025/2026 saeen (Etappe P3): bereits erledigt
  Schritt 42  Katalogtraeger Fluessiggas saeen (Nachtrag Ä9): bereits erledigt
  Schritt 43  Fehlende VDI-3805-Katalogtraeger nachsaeen (Nachtrag Ä9): bereits erledigt
  Schritt 44  Strom-Leistungspreis freischalten: pricing_model ELECTRICITY erhaelt has_powerprice (Nachtrag Ä18): bereits erledigt
  Schritt 45  Anlagenkosten: Tab_ProjektWerte.ID_Anlage anlegen und den Bestand der jeweils ersten verbauten Anlage zuordnen (Ä20): bereits erledigt
  Schritt 46  Anlagenkosten-Geräteanker: Tab_ProjektWerte.ID_AnlageGeraet anlegen und aus den bestehenden Zuordnungen befuellen (Ä21): bereits erledigt
  Schritt 47  Anlagenkosten-Geräteanker aus den gültigen Zuordnungen neu ableiten (Ä24: Variantenkopien ankerten am Quellprojekt): bereits erledigt
  Schritt 48  Ganglinienkanal: Z_ProjektWaermebedarf.Kanal anlegen und den Bestand verhaltensneutral auf 'Heizung' vorbelegen (Paket K1, F18): bereits erledigt
  Schritt 49  Klassen-Set: Tab_Pufferspeicher.Nutzung_Heizung/_Brauchwasser/_Prozess anlegen und aus Verwendung befuellen; Tab_Einstellungen.Kanal_Knappheitsreihenfolge anlegen und vorbelegen (Paket K2, F5/F10): bereits erledigt
  Schritt 50  Senkenliste: Z_AnlageSenke anlegen, die Senken-Slots als Raenge uebernehmen (inkl. Regel R-Prozess) und Z_AnlagePufferVerbund um ID_Senke erweitern (Paket S1, L4/L5/F17): bereits erledigt
  Schritt 51  Altpfad-Stilllegung: Betriebstemperaturen aus Z_ProjektPufferSp an die Pufferzeilen uebernehmen und Kaskade_Zweikanalig im Bestand auf WAHR setzen (Paket A1, L1): bereits erledigt
  Schritt 52  Ergebnis je Kanal: Waermebedarf_/Deckung_/Entladung_Heizung, _Brauchwasser, _Prozess anlegen; Tab_ErgebnisPufferspeicher zusaetzlich um die Durchsatzsummen, ID_Anlage und T_oben_* erweitern (Paket E1): OK
          - Tab_ErgebnisEnergiebedarf: 3 Spalten angelegt, 0 bereits vorhanden
          - Tab_ErgebnisWaermepumpe: 3 Spalten angelegt, 0 bereits vorhanden
          - Tab_ErgebnisHeizkessel: 3 Spalten angelegt, 0 bereits vorhanden
          - Tab_ErgebnisBHKW: 3 Spalten angelegt, 0 bereits vorhanden
          - Tab_ErgebnisSolarthermie: 3 Spalten angelegt, 0 bereits vorhanden
          - Tab_ErgebnisPufferspeicher: 8 Spalten angelegt, 0 bereits vorhanden
  Schichtmodell (Schritt 53): 45 Pufferspeicher auf Schichten_Anzahl = 1 gesetzt (Ein-Zonen-Modell des Bestands, verhaltensneutral); 45 auf Ladeleistung_Max = 0 und 45 auf Entladeleistung_Max = 0 (unbegrenzt) vorbelegt. Hoehe, Lambda_Eff, T_Nutz_BW und die drei Entnahmehoehen bleiben bewusst leer - NULL bedeutet dort Konzept-Vorgabe, nicht 0.
  Schritt 53  Schichtmodell: Tab_Pufferspeicher um Schichten_Anzahl, Hoehe, Lambda_Eff, T_Nutz_BW, die drei Entnahmehoehen und die beiden Leistungsgrenzen erweitern und verhaltensneutral vorbelegen (Paket P1, L7): OK
          - Tab_Pufferspeicher: 9 Spalten angelegt, 0 bereits vorhanden
  Abschlussprüfung Anlagenzeilen-Eindeutigkeit
          - Eindeutigkeitsindex idx_Anlage_ID_WP (Wärmepumpe): bereits vorhanden
          - Eindeutigkeitsindex idx_Anlage_ID_Kessel (Heizkessel): bereits vorhanden
          - Eindeutigkeitsindex idx_Anlage_ID_BHKW (BHKW): bereits vorhanden
          - Eindeutigkeitsindex idx_Anlage_ID_PUFFER (Pufferspeicher): bereits vorhanden
  Abschlusspruefung Katalog-Eindeutigkeitsindizes
          - Eindeutigkeitsindex UX_Tab_WP_STAMM_Bezeichner (WP): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Heizkessel_STAMM_Bezeichner (HEIZKESSEL): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Pufferspeicher_STAMM_Bezeichner (PUFFERSPEICHER): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Solarkollektoren_STAMM_Bezeichner (SOLARKOLLEKTOREN): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_PV_STAMM_Bezeichner (PV): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_BHKW_STAMM_Bezeichner (BHKW): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Stromspeicher_STAMM_Bezeichner (STROMSPEICHER): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Gebaeude_STAMM_Bezeichner (GEBAEUDE): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Klimaregion_STAMM_Name (KLIMAREGION): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Brauchwasser_STAMM_Bezeichner (BRAUCHWASSER): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Brauchwassertyp_STAMM_Bezeichner (BRAUCHWASSERTYP): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Stromverbraucher_STAMM_Bezeichner (STROMVERBRAUCHER): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Stromverbrauchertyp_STAMM_Typname (STROMVERBRAUCHERTYP): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Prozesswaerme_STAMM_Bezeichner (PROZESSWAERME): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Prozesstyp_STAMM_Bezeichner (PROZESSTYP): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Stromganglinie_STAMM_Bezeichner (STROMGANGLINIE): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Solarganglinie_STAMM_Bezeichner (SOLARGANGLINIE): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_Waermebedarf_STAMM_Bezeichner (WAERMEBEDARF): bereits vorhanden
          - Eindeutigkeitsindex UX_Tab_DBTagV_STAMM_Bezeichner (GEBAEUDETYP): bereits vorhanden
  Abschlusspruefung Leseprobe Abfrage_Kostenfaktoren
          - Abschluss: Abfrage_Kostenfaktoren ist lesbar - nichts zu tun.
  Abschlusspruefung gespeicherte Abfragen (zweiter Durchgang)
          - Abschluss 35A: Abfrage_Heizkessel_Kosten: nicht vorhanden - nichts zu tun.
          - Abschluss 35A: Abfrage_Neues_Kosten_Model: nicht vorhanden - nichts zu tun.
          - Abschluss 35B: Abfrage_SST ist lesbar - nichts zu tun.
          - Abschluss 35B: Abfrage_Kuehlung_MaxLast ist lesbar - nichts zu tun.
          - Abschluss 35B: Abfrage_KenndatenKuehlung_Max ist lesbar - nichts zu tun.
          - Abschluss 35: 0 von 2 toten Abfragen entfernt, 0 von 3 Abfragen auf die heutigen Spaltennamen gebracht, 0 offen. Es gab nichts zu tun (Idempotenz-Nachweis: Genau das meldet ein zweiter Lauf).
  Abschlusspruefung BHKW-Kosten (Einzelposten und Investition_kwel)
          - Abschluss 37: Tab_BHKW (6 Zeilen unveraendert): 0 x Investition_kwel nachgezogen, 0 x Kosten_Modul abgeleitet, 4 bereits stimmig, 2 ohne jede Kostenangabe, 0 offen (Pel = 0). Gegenprobe: nichts mehr zu tun.
          - Abschluss 37: Tab_BHKW_STAMM (79 Zeilen unveraendert): 0 x Investition_kwel nachgezogen, 0 x Kosten_Modul abgeleitet, 42 bereits stimmig, 37 ohne jede Kostenangabe, 0 offen (Pel = 0). Gegenprobe: nichts mehr zu tun.
          - Abschluss 37: zusammen 0 angeglichen, 0 abgeleitet, 0 offen; 46 Zeilen waren bereits stimmig, 39 fuehren ueberhaupt keine Kosten. Es gab nichts zu tun (Idempotenz-Nachweis: Genau das meldet ein zweiter Lauf).
  Schemastand nachher: 53   (Zielstand 53)
  Parallelverbund (Schritt 14): 0 Zeilen in Z_AnlagePufferVerbund - kein Projekt führt einen Pufferverbund, der Rechenweg bleibt unverändert.
  Dublettenauflösung (Schritt 17): 0 Anlagenzeilen auf eine eigene Gerätekopie überführt - es gab keine doppelt belegte Anlagenzeile.
  Katalogbereinigung (Schritt 24): 0 doppelte Katalogeinträge entfernt - es gab keinen doppelt vergebenen Katalognamen.
  Katalogbereinigung alle Kataloge (Schritt 30): 0 doppelte Katalogeintraege entfernt - es gab keinen doppelt vergebenen Katalognamen.
  Anlagenzeilen-Eindeutigkeit (Schritt 16): 4 von 4 Eindeutigkeitsindizes aktiv, 0 doppelt belegte Anlagenzeilen - je Projekt und Gerät genau eine Zeile.
  Katalog-Eindeutigkeit (Schritt 31): 19 von 19 Eindeutigkeitsindizes aktiv - jeder Katalogname ist durch einen eindeutigen Index gesichert.
  Kostenarten (Schritt 19): 0 Kostenpositionen auf Bemessung "BETRAG" vorbelegt, 0 nach VDI 2067 eingeordnet - die Bemessung war bereits gesetzt, der Rechenweg bleibt unveraendert.
  Steuerangaben (Schritt 20): 0 Angaben ueber drei Spalten vorbelegt - die Steuerangaben standen bereits, es entsteht keine neue Gutschrift.
  Tarifmodell (Schritt 21): 0 Angaben ueber drei Spalten vorbelegt - es gibt keinen Tarifsatz oder er steht bereits; der Rechenweg bleibt unveraendert.
  Bilanzierung (Schritt 23): 0 Angaben ueber drei Spalten vorbelegt - die Bilanzierungsangaben standen bereits; der Rechenweg bleibt unveraendert.
  Energieträger-Abfrage (Schritt 36): Abfrage_Energietraeger_Effektiv war bereits vorhanden - nichts geändert.
  Gespeicherte Abfragen (Schritt 32): 0 Produktivabfrage erneuert, 0 von 3 Altabfragen entfernt - keine gespeicherte Abfrage verweist mehr auf eine in Schritt 29 gedroppte Tabelle.
  Leseprobe Abfrage_Kostenfaktoren (Schritt 33): bestanden - die Abfrage war bereits in Ordnung und wurde nicht angefasst.
  Verwaiste Geraetezeilen (Schritt 34): 0 Geraetezeilen und 0 Kennlinienzeilen aus 0 Projekten entfernt - auf jede Geraetezeile zeigt eine Anlagenzeile ihres Projekts.
  Gespeicherte Abfragen, zweiter Durchgang (Schritt 35): 0 von 2 toten Abfragen entfernt, 0 von 3 auf die heutigen Spaltennamen gebracht - es gab nichts zu tun.
  BHKW-Kosten (Schritt 37): 0 x Investition_kwel aus den Einzelposten nachgezogen, 0 x Kosten_Modul aus Investition_kwel abgeleitet, 0 offen - beide Seiten stimmen ueberein; es gab nichts zu tun.
Migration: ERFOLG (Zielstand 53).

Projektlandschaft wird gelesen ...
28 Projekte in Tab_Projekt.

Gewaehlte Referenzprojekte (13):
  - Projekt 1007 "Laurentiuskirche"
      Ausstattung: Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher | Anlagen: WP,PV,Batterie,Kessel,Puffer | Puffer(anderer Erzeuger)
      Grund:       per --projekte vorgegeben
  - Projekt 1008 "Heinestr 15"
      Ausstattung: Tools: Wärmepumpe | Anlagen: WP,Kessel,Puffer | Puffer(WP)
      Grund:       per --projekte vorgegeben
  - Projekt 1011 "test1"
      Ausstattung: Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher | Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer | Puffer(anderer Erzeuger)
      Grund:       per --projekte vorgegeben
  - Projekt 1017 "WP_PV-Speicher"
      Ausstattung: Tools: BHKW, Heizkessel, Stromspeicher | Anlagen: WP,Batterie,Kessel,BHKW
      Grund:       per --projekte vorgegeben
  - Projekt 1018 "BHKW Test München"
      Ausstattung: Tools: BHKW, Heizkessel | Anlagen: Kessel,BHKW,Puffer
      Grund:       per --projekte vorgegeben
  - Projekt 1021 "TestSpeichernUnter"
      Ausstattung: Tools: Wärmepumpe | Anlagen: WP,Puffer | Quellspeicher(WP)
      Grund:       per --projekte vorgegeben
  - Projekt 1023 "Wöhler - Test1"
      Ausstattung: Tools: Wärmepumpe, Heizkessel | Anlagen: WP,Kessel,Puffer | Puffer(WP)
      Grund:       per --projekte vorgegeben
  - Projekt 1024 "Wöhler - Test2"
      Ausstattung: Tools: Wärmepumpe, Heizkessel, BHKW | Anlagen: WP,Kessel,BHKW,Puffer
      Grund:       per --projekte vorgegeben
  - Projekt 1030 "Referenz BHKW-Kaskade (Regressionstest)"
      Ausstattung: Tools: BHKW, Heizkessel | Anlagen: Kessel,BHKW,Puffer
      Grund:       per --projekte vorgegeben
  - Projekt 1039 "Mehrgebäude"
      Ausstattung: Tools: Heizkessel, Wärmepumpe | Anlagen: WP,Kessel,Puffer | Puffer(WP)
      Grund:       per --projekte vorgegeben
  - Projekt 1040 "zwei Puffer je Kanal"
      Ausstattung: Tools: Wärmepumpe, Heizkessel, Photovoltaik, Stromspeicher | Anlagen: WP,PV,Kessel,Puffer | Puffer(WP)
      Grund:       per --projekte vorgegeben
  - Projekt 1041 "Prozesswärme mit eigenem Puffer"
      Ausstattung: Tools: Wärmepumpe, Heizkessel, Photovoltaik, Stromspeicher | Anlagen: WP,Kessel,Puffer
      Grund:       per --projekte vorgegeben
  - Projekt 1042 "Booster-Kette mit Kombi-Speicher"
      Ausstattung: Tools: Wärmepumpe, Heizkessel, Solarthermie, Photovoltaik, Stromspeicher | Anlagen: WP,Kessel,Puffer
      Grund:       per --projekte vorgegeben

--- Projekt 1007 (Laurentiuskirche) ---
      | [22:58:25] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [22:58:25] Simulation startet fuer Projekt 1007 ...
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoTEC plus VCI 20/26CS/1-5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-15,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [22:58:26] Simulation beendet, Ergebnis-Kopf-ID 207.
      | [22:58:26] Projekt 1007: 29 CSV-Dateien, 99 Skalare.
Projekt 1007: OK, 29 CSV-Dateien, 00:01
--- Projekt 1008 (Heinestr 15) ---
      | [22:58:26] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [22:58:27] Simulation startet fuer Projekt 1008 ...
      | Simulation Warnung: Speicher-Registry: Puffer 1008007 (Vitocell 140-E 600 Liter) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 6,96 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Anlage „CS7800iLW 16": Die Sole-Wasser-Wärmepumpe hat keine konfigurierte Wärmequelle — gerechnet wird ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt. Die Quelle über den Chip „Quelle" der Erzeugerkarte wählen (Erdreich, konstante Temperatur, Quellprofil oder Pufferspeicher).
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoTEC plus VCI 20/26CS/1-5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur überschreitet in 221 Stunden die obere Stützstelle der Kennlinie (25,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS5800i AW 12 M + AW 5 OR-S': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [22:58:27] Simulation beendet, Ergebnis-Kopf-ID 208.
      | [22:58:27] Projekt 1008: 21 CSV-Dateien, 101 Skalare.
Projekt 1008: OK, 21 CSV-Dateien, 00:01
--- Projekt 1011 (test1) ---
      | [22:58:28] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [22:58:28] Simulation startet fuer Projekt 1011 ...
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „GC7000F 22 23 - MX25" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „GC7000F 22 23 - MX25 (2)" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-15,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 12 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 12 OR-T (2)': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [22:58:29] Simulation beendet, Ergebnis-Kopf-ID 209.
      | [22:58:29] Projekt 1011: 29 CSV-Dateien, 121 Skalare.
Projekt 1011: OK, 29 CSV-Dateien, 00:01
--- Projekt 1017 (WP_PV-Speicher) ---
      | [22:58:30] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [22:58:30] Simulation startet fuer Projekt 1017 ...
      | Simulation Warnung: Anlage „WPE-I 59 H 400 Premium": Die Sole-Wasser-Wärmepumpe hat keine konfigurierte Wärmequelle — gerechnet wird ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt. Die Quelle über den Chip „Quelle" der Erzeugerkarte wählen (Erdreich, konstante Temperatur, Quellprofil oder Pufferspeicher).
      | Simulation Warnung: Energieträger-Zuordnung: Der BHKW-Anlage „BHKW EW K 10 S [K] Heizol" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „eloBLOCK VE 28" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Zum Stichtag 01.01.2026 gab es noch keine Preisversion - es gilt die aelteste vorhandene.
      | [22:58:30] Simulation beendet, Ergebnis-Kopf-ID 210.
      | [22:58:30] Projekt 1017: 21 CSV-Dateien, 112 Skalare.
Projekt 1017: OK, 21 CSV-Dateien, 00:00
--- Projekt 1018 (BHKW Test München) ---
      | [22:58:31] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [22:58:31] Simulation startet fuer Projekt 1018 ...
      | Simulation Warnung: Speicher-Registry: Puffer 1054175 (Stora B 1000-6 ER 1 B) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 11,194 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „Vitocrossal 200 CM2 raumluftabh�ngig" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | [22:58:31] Simulation beendet, Ergebnis-Kopf-ID 211.
      | [22:58:31] Projekt 1018: 22 CSV-Dateien, 139 Skalare.
Projekt 1018: OK, 22 CSV-Dateien, 00:00
--- Projekt 1021 (TestSpeichernUnter) ---
      | [22:58:32] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [22:58:32] Simulation startet fuer Projekt 1021 ...
      | Simulation Warnung: Anlage „CS7800iLW 12": Der Speicher „allSTOR exclusiv VPS 800/3-7" ist ihre Wärmequelle, wird aber von keiner Anlage dieses Projekts geladen. Nach der Startfüllung liefe die Quelle leer.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [22:58:32] Simulation beendet, Ergebnis-Kopf-ID 212.
      | [22:58:32] Projekt 1021: 21 CSV-Dateien, 94 Skalare.
Projekt 1021: OK, 21 CSV-Dateien, 00:00
--- Projekt 1023 (Wöhler - Test1) ---
      | [22:58:32] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [22:58:33] Simulation startet fuer Projekt 1023 ...
      | Simulation Warnung: Anlage „CS7800iLW 12": Die Sole-Wasser-Wärmepumpe hat keine konfigurierte Wärmequelle — gerechnet wird ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt. Die Quelle über den Chip „Quelle" der Erzeugerkarte wählen (Erdreich, konstante Temperatur, Quellprofil oder Pufferspeicher).
      | Simulation Warnung: Anlage „CS6800iAW MB + AW 10 OR-T": Der Erzeuger-Vorlauf 45 °C liegt unter dem wirksamen Vorlauf 65 °C des Zielspeichers „Vitocell 140-E 600 Ltr". Der Erzeuger kann den Speicher nie auf Solltemperatur laden.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoVIT VKK 186/5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 12': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 12': Die Quelltemperatur überschreitet in 221 Stunden die obere Stützstelle der Kennlinie (25,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [22:58:33] Simulation beendet, Ergebnis-Kopf-ID 213.
      | [22:58:33] Projekt 1023: 25 CSV-Dateien, 135 Skalare.
Projekt 1023: OK, 25 CSV-Dateien, 00:01
--- Projekt 1024 (Wöhler - Test2) ---
      | [22:58:34] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [22:58:34] Simulation startet fuer Projekt 1024 ...
      | Simulation Hinweis: Kaskade: Der Heizkessel steht zwischen zwei Erzeugern der Speicherstufe. Er rechnet deshalb als Mitglied der Stundenschleife an seiner Kaskadenposition mit (Phase B) - ohne Puffer-Senke als reine Heizkreis-Stufe.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [22:58:34] Simulation beendet, Ergebnis-Kopf-ID 214.
      | [22:58:34] Projekt 1024: 26 CSV-Dateien, 155 Skalare.
Projekt 1024: OK, 26 CSV-Dateien, 00:01
--- Projekt 1030 (Referenz BHKW-Kaskade (Regressionstest)) ---
      | [22:58:35] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [22:58:35] Simulation startet fuer Projekt 1030 ...
      | [22:58:36] Simulation beendet, Ergebnis-Kopf-ID 215.
      | [22:58:36] Projekt 1030: 22 CSV-Dateien, 147 Skalare.
Projekt 1030: OK, 22 CSV-Dateien, 00:01
--- Projekt 1039 (Mehrgebäude) ---
      | [22:58:36] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [22:58:36] Simulation startet fuer Projekt 1039 ...
      | Simulation Warnung: Speicher-Registry: Puffer 1054182 (Puffer 3000Ltr) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 34,8 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Anlage „WPE-I 59 H 400 Premium": Die Sole-Wasser-Wärmepumpe hat keine konfigurierte Wärmequelle — gerechnet wird ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt. Die Quelle über den Chip „Quelle" der Erzeugerkarte wählen (Erdreich, konstante Temperatur, Quellprofil oder Pufferspeicher).
      | Simulation Warnung: Anlage „WPE-I 59 H 400 Premium": Der Erzeuger-Vorlauf 55 °C liegt unter dem wirksamen Vorlauf 65 °C des Zielspeichers „Stora B 1000-6 ER 1 B (2)". Der Erzeuger kann den Speicher nie auf Solltemperatur laden.
      | Simulation Hinweis: Wärmepumpe 'WPE-I 59 H 400 Premium': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'WPE-I 59 H 400 Premium': Die Quelltemperatur überschreitet in 6112 Stunden die obere Stützstelle der Kennlinie (5,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [22:58:37] Simulation beendet, Ergebnis-Kopf-ID 216.
      | [22:58:37] Projekt 1039: 25 CSV-Dateien, 148 Skalare.
Projekt 1039: OK, 25 CSV-Dateien, 00:01
--- Projekt 1040 (zwei Puffer je Kanal) ---
      | [22:58:37] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [22:58:37] Simulation startet fuer Projekt 1040 ...
      | Simulation Warnung: Speicher-Registry: Puffer 1054188 (Stora B 1000-6 ER 1 B (2)) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 11,194 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Hinweis: Parallelverbund: Speicher 1054187 (Puffer 3000Ltr) rechnet als EIN gemeinsamer Vorrat aus 2 Behältern - nutzbare Kapazität Q_max 34,8 kWh (Leitspeicher) + 11,194 kWh (1 Mitglieder) = 45,994 kWh. Schwellen, Notreserve, Entladepriorität und Verwendung gelten aus dem Leitspeicher; es entsteht EINE Ergebniszeile unter seiner ID.
      | Simulation Warnung: Speicher-Registry: Puffer 1054189 (Puffer 3000Ltr (2)) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 34,8 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Anlage „CS6800iAW MB + AW 10 OR-T": Der Erzeuger-Vorlauf 45 °C liegt unter dem wirksamen Vorlauf 65 °C des Zielspeichers „Puffer 3000Ltr". Der Erzeuger kann den Speicher nie auf Solltemperatur laden.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Stromspeicher: Dem Projekt ist kein Stromspeicher zugeordnet - die Speicherrechnung entfällt.
      | [22:58:38] Simulation beendet, Ergebnis-Kopf-ID 217.
      | [22:58:38] Projekt 1040: 30 CSV-Dateien, 163 Skalare.
Projekt 1040: OK, 30 CSV-Dateien, 00:01
--- Projekt 1041 (Prozesswärme mit eigenem Puffer) ---
      | [22:58:39] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [22:58:39] Simulation startet fuer Projekt 1041 ...
      | Simulation Warnung: Speicher-Registry: Puffer 1054191 (Puffer 3000Ltr) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 34,8 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Speicher-Registry: Puffer 1054193 (Stora B 1000-6 ER 1 B) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 11,194 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Hinweis: Parallelverbund: Speicher 1054191 (Puffer 3000Ltr) rechnet als EIN gemeinsamer Vorrat aus 2 Behältern - nutzbare Kapazität Q_max 34,8 kWh (Leitspeicher) + 11,194 kWh (1 Mitglieder) = 45,994 kWh. Schwellen, Notreserve, Entladepriorität und Verwendung gelten aus dem Leitspeicher; es entsteht EINE Ergebniszeile unter seiner ID.
      | Simulation Warnung: Speicher-Registry: Puffer 1054192 (Puffer 3000Ltr (2)) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 34,8 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Stromspeicher: Dem Projekt ist kein Stromspeicher zugeordnet - die Speicherrechnung entfällt.
      | [22:58:40] Simulation beendet, Ergebnis-Kopf-ID 218.
      | [22:58:40] Projekt 1041: 27 CSV-Dateien, 149 Skalare.
Projekt 1041: OK, 27 CSV-Dateien, 00:01
--- Projekt 1042 (Booster-Kette mit Kombi-Speicher) ---
      | [22:58:40] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [22:58:40] Simulation startet fuer Projekt 1042 ...
      | Simulation Warnung: Speicher-Registry: Puffer 1054197 (Stora B 1000-6 ER 1 B) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 11,194 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Speicher-Registry: Puffer 1054196 (Puffer 3000Ltr) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 34,8 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Speicher-Registry: Puffer 1054198 (Puffer 3000Ltr (2)) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 34,8 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Wärmesenke: Die Anlage 14785 ist auf PufferKombi gesetzt, hat aber KEINEN Pufferspeicher zugeordnet (WS_ID_Puffer leer). Sie rechnet deshalb auf den HEIZKREIS.
      | Simulation Warnung: Wärmesenke: Die Anlage 14806 hat eine Zweitsenke PufferKombi ohne zugeordneten Pufferspeicher (WS_ID_Puffer2 leer). Die Zweitsenke bleibt unberücksichtigt.
      | Simulation Warnung: Anlage „CS7800iLW 16": Die Sole-Wasser-Wärmepumpe hat keine konfigurierte Wärmequelle — gerechnet wird ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt. Die Quelle über den Chip „Quelle" der Erzeugerkarte wählen (Erdreich, konstante Temperatur, Quellprofil oder Pufferspeicher).
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur überschreitet in 221 Stunden die obere Stützstelle der Kennlinie (25,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Stromspeicher: Dem Projekt ist kein Stromspeicher zugeordnet - die Speicherrechnung entfällt.
      | [22:58:41] Simulation beendet, Ergebnis-Kopf-ID 219.
      | [22:58:41] Projekt 1042: 31 CSV-Dateien, 186 Skalare.
Projekt 1042: OK, 31 CSV-Dateien, 00:01

Fertig. Gesamtdauer 00:00:17
Erfolgreich: 13 von 13
```

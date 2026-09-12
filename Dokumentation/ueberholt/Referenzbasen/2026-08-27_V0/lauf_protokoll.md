# Referenzlauf-Protokoll

**Zeitpunkt:** 27.08.2026 10:08:40

**Quelle (produktiv, nur gelesen):** `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`

**Arbeitskopie (beschrieben):** `C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb`

**Zielordner:** `C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-27_V0`

**Gesamtdauer:** 00:00:09  |  **Timeout je Projekt:** 300 s

**Warnungen:** 12  |  **Fehler:** 0

## Projekte

| ID | Projekt | Ausstattung | Auswahlgrund | Dauer | CSV | Status |
|---|---|---|---|---|---|---|
| 1007 | Laurentiuskirche | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:01 | 29 | OK |
| 1008 | Heinestr 15 | Tools: Wärmepumpe / Anlagen: WP,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:00 | 21 | OK |
| 1011 | test1 | Tools: Solarthermie, Wärmepumpe, Photovoltaik, Stromspeicher / Anlagen: WP,Solar,PV,Batterie,Kessel,Puffer / Puffer(anderer Erzeuger) | per --projekte vorgegeben | 00:01 | 29 | OK |
| 1017 | WP_PV-Speicher | Tools: BHKW, Heizkessel, Stromspeicher / Anlagen: WP,Batterie,Kessel,BHKW | per --projekte vorgegeben | 00:00 | 21 | OK |
| 1018 | BHKW Test München | Tools: BHKW, Heizkessel / Anlagen: Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:00 | 22 | OK |
| 1021 | TestSpeichernUnter | Tools: Wärmepumpe / Anlagen: WP,Puffer / Quellspeicher(WP) | per --projekte vorgegeben | 00:00 | 21 | OK |
| 1023 | Wöhler - Test1 | Tools: Wärmepumpe, Heizkessel / Anlagen: WP,Kessel,Puffer / Puffer(WP) | per --projekte vorgegeben | 00:00 | 25 | OK |
| 1024 | Wöhler - Test2 | Tools: Wärmepumpe, Heizkessel, BHKW / Anlagen: WP,Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:00 | 26 | OK |
| 1030 | Referenz BHKW-Kaskade (Regressionstest) | Tools: BHKW, Heizkessel / Anlagen: Kessel,BHKW,Puffer | per --projekte vorgegeben | 00:00 | 22 | OK |

## Ablauf

```
Referenzlauf gestartet.
Projektwurzel: C:\Waermeplan\WP_Plan
Zielordner:    C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-27_V0
Timeout je Projekt: 300 s

Quelle gefunden (ProgramData): C:\ProgramData\EPOS_PLAN\Kenndaten.accdb
Arbeitskopie angelegt: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb (144 MB)
DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb

Schema-Migration der Arbeitskopie ...
  C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
  Zeitpunkt: 27.08.2026 10:08:30
  Schemastand vorher: 44   (Zielstand 47)
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
  Anlagenkosten (Schritt 45): 85 Position(en) der jeweils ersten verbauten Anlage zugeordnet; 4 Projekt-Komponenten ohne verbaute Anlage bleiben ohne Zuordnung (Ausweis "ohne Anlagenzuordnung").
  Schritt 45  Anlagenkosten: Tab_ProjektWerte.ID_Anlage anlegen und den Bestand der jeweils ersten verbauten Anlage zuordnen (Ä20): OK
  Geräteanker (Schritt 46): 85 Position(en) mit dem Gerät ihrer Anlage verankert.
  Schritt 46  Anlagenkosten-Geräteanker: Tab_ProjektWerte.ID_AnlageGeraet anlegen und aus den bestehenden Zuordnungen befuellen (Ä21): OK
  Geräteanker-Nachzug (Schritt 47): 85 Position(en) aus ihrer Anlagenzeile abgeleitet.
  Schritt 47  Anlagenkosten-Geräteanker aus den gültigen Zuordnungen neu ableiten (Ä24: Variantenkopien ankerten am Quellprojekt): OK
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
  Schemastand nachher: 47   (Zielstand 47)
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
Migration: ERFOLG (Zielstand 47).

Projektlandschaft wird gelesen ...
24 Projekte in Tab_Projekt.

Gewaehlte Referenzprojekte (9):
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

--- Projekt 1007 (Laurentiuskirche) ---
      | [10:08:30] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [10:08:31] Simulation startet fuer Projekt 1007 ...
      | Simulation Hinweis: Speicher-Registry: Puffer 1007007 (Vitocell 140-E 600 Liter) hat kein Temperaturpaar in der Projektkopie - es gilt die Zuordnungszeile (50/30 °C).
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoTEC plus VCI 20/26CS/1-5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-15,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [10:08:31] Simulation beendet, Ergebnis-Kopf-ID 204.
      | [10:08:31] Projekt 1007: 29 CSV-Dateien, 90 Skalare.
Projekt 1007: OK, 29 CSV-Dateien, 00:01
--- Projekt 1008 (Heinestr 15) ---
      | [10:08:32] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [10:08:32] Simulation startet fuer Projekt 1008 ...
      | Simulation Warnung: Speicher-Registry: Puffer 1008007 (Vitocell 140-E 600 Liter) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 6,96 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Speicher-Registry: Puffer 1008008 (allSTOR exclusiv VPS 800/3-7) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 9,025 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoTEC plus VCI 20/26CS/1-5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 16': Die Quelltemperatur überschreitet in 114 Stunden die obere Stützstelle der Kennlinie (25,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS5800i AW 12 M + AW 5 OR-S': Die Quelltemperatur überschreitet in 233 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [10:08:33] Simulation beendet, Ergebnis-Kopf-ID 205.
      | [10:08:33] Projekt 1008: 21 CSV-Dateien, 87 Skalare.
Projekt 1008: OK, 21 CSV-Dateien, 00:00
--- Projekt 1011 (test1) ---
      | [10:08:33] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [10:08:33] Simulation startet fuer Projekt 1011 ...
      | Simulation Warnung: Speicher-Registry: Puffer 1011007 (Vitocell 140-E 600 Liter) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 6,96 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „GC7000F 22 23 - MX25" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „GC7000F 22 23 - MX25 (2)" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-15,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 12 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 12 OR-T (2)': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS3400i AWS 10 E + CS3400i AWS 4 OR-S': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Dem Projekt ist kein Strom-Energietraeger zugeordnet - es gelten die Vorgabewerte fuer Aufschlag und Verguetung. Fuer den Strombezug ist kein Arbeitspreis gepflegt - gerechnet wird mit dem Rueckfallwert 20 ct/kWh.
      | [10:08:34] Simulation beendet, Ergebnis-Kopf-ID 206.
      | [10:08:34] Projekt 1011: 29 CSV-Dateien, 112 Skalare.
Projekt 1011: OK, 29 CSV-Dateien, 00:01
--- Projekt 1017 (WP_PV-Speicher) ---
      | [10:08:34] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [10:08:34] Simulation startet fuer Projekt 1017 ...
      | Simulation Hinweis: Das Projekt enthält ein BHKW - dieser Lauf rechnet deshalb IMMER über die Speicherstufe mit herausgelöster Ladephase (Konzept 6.3), unabhängig von der Projekteinstellung Kaskade_Zweikanalig. Der einkanalige BHKW-Altpfad ist entfallen (Paket BHKW-Regulär).
      | Simulation Warnung: Energieträger-Zuordnung: Der BHKW-Anlage „BHKW EW K 10 S [K] Heizol" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „eloBLOCK VE 28" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Zum Stichtag 01.01.2026 gab es noch keine Preisversion - es gilt die aelteste vorhandene.
      | [10:08:35] Simulation beendet, Ergebnis-Kopf-ID 207.
      | [10:08:35] Projekt 1017: 21 CSV-Dateien, 103 Skalare.
Projekt 1017: OK, 21 CSV-Dateien, 00:00
--- Projekt 1018 (BHKW Test München) ---
      | [10:08:35] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [10:08:35] Simulation startet fuer Projekt 1018 ...
      | Simulation Warnung: Speicher-Registry: Puffer 1054175 (Stora B 1000-6 ER 1 B) hat KEIN Temperaturpaar - es gilt der Rückfall ΔT = 10 K, nutzbare Kapazität Q_max 11,194 kWh. Ein gepflegtes Vorlauf-/Rücklaufpaar am Puffer ergäbe eine andere Kapazität.
      | Simulation Hinweis: Das Projekt enthält ein BHKW - dieser Lauf rechnet deshalb IMMER über die Speicherstufe mit herausgelöster Ladephase (Konzept 6.3), unabhängig von der Projekteinstellung Kaskade_Zweikanalig. Der einkanalige BHKW-Altpfad ist entfallen (Paket BHKW-Regulär).
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „Vitocrossal 200 CM2 raumluftabh�ngig" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | [10:08:36] Simulation beendet, Ergebnis-Kopf-ID 208.
      | [10:08:36] Projekt 1018: 22 CSV-Dateien, 122 Skalare.
Projekt 1018: OK, 22 CSV-Dateien, 00:00
--- Projekt 1021 (TestSpeichernUnter) ---
      | [10:08:36] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [10:08:36] Simulation startet fuer Projekt 1021 ...
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [10:08:37] Simulation beendet, Ergebnis-Kopf-ID 209.
      | [10:08:37] Projekt 1021: 21 CSV-Dateien, 80 Skalare.
Projekt 1021: OK, 21 CSV-Dateien, 00:00
--- Projekt 1023 (Wöhler - Test1) ---
      | [10:08:37] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [10:08:37] Simulation startet fuer Projekt 1023 ...
      | Simulation Warnung: Energieträger-Zuordnung: Der Heizkessel-Anlage „ecoVIT VKK 186/5" ist kein Energieträger zugeordnet (ID_Carrier leer) - Brennstoff, Kosten und Emissionen dieser Anlage können im Bericht nicht ausgewiesen werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 12': Die Quelltemperatur unterschreitet die untere Stützstelle der Kennlinie (-5,0 °C). Es wird extrapoliert (Projekteinstellung „Extrapolation der Kennlinie erlauben“).
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 662 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | Simulation Hinweis: Wärmepumpe 'CS7800iLW 12': Die Quelltemperatur überschreitet in 163 Stunden die obere Stützstelle der Kennlinie (25,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [10:08:37] Simulation beendet, Ergebnis-Kopf-ID 210.
      | [10:08:38] Projekt 1023: 25 CSV-Dateien, 118 Skalare.
Projekt 1023: OK, 25 CSV-Dateien, 00:00
--- Projekt 1024 (Wöhler - Test2) ---
      | [10:08:38] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [10:08:38] Simulation startet fuer Projekt 1024 ...
      | Simulation Hinweis: Das Projekt enthält ein BHKW - dieser Lauf rechnet deshalb IMMER über die Speicherstufe mit herausgelöster Ladephase (Konzept 6.3), unabhängig von der Projekteinstellung Kaskade_Zweikanalig. Der einkanalige BHKW-Altpfad ist entfallen (Paket BHKW-Regulär).
      | Simulation Hinweis: Kaskade: Der Heizkessel steht zwischen zwei Erzeugern der Speicherstufe. Er rechnet deshalb als Mitglied der Stundenschleife an seiner Kaskadenposition mit (Phase B) - ohne Puffer-Senke als reine Heizkreis-Stufe.
      | Simulation Hinweis: Wärmepumpe 'CS6800iAW MB + AW 10 OR-T': Die Quelltemperatur überschreitet in 957 Stunden die obere Stützstelle der Kennlinie (20,0 °C). Für diese Stunden gilt der COP der obersten Stützstelle; es wird nicht extrapoliert. Liefert der Hersteller ein Hochtemperatur-Kennfeld, sollte die Kennlinie um höhere Stützstellen ergänzt werden.
      | [10:08:38] Simulation beendet, Ergebnis-Kopf-ID 211.
      | [10:08:39] Projekt 1024: 26 CSV-Dateien, 135 Skalare.
Projekt 1024: OK, 26 CSV-Dateien, 00:00
--- Projekt 1030 (Referenz BHKW-Kaskade (Regressionstest)) ---
      | [10:08:39] DB-Pfad der App verifiziert: C:\Waermeplan\WP_Plan\Referenzlaeufe\Arbeitskopie\Kenndaten.accdb
      | [10:08:39] Simulation startet fuer Projekt 1030 ...
      | Simulation Hinweis: Das Projekt enthält ein BHKW - dieser Lauf rechnet deshalb IMMER über die Speicherstufe mit herausgelöster Ladephase (Konzept 6.3), unabhängig von der Projekteinstellung Kaskade_Zweikanalig. Der einkanalige BHKW-Altpfad ist entfallen (Paket BHKW-Regulär).
      | [10:08:39] Simulation beendet, Ergebnis-Kopf-ID 212.
      | [10:08:39] Projekt 1030: 22 CSV-Dateien, 130 Skalare.
Projekt 1030: OK, 22 CSV-Dateien, 00:00

Fertig. Gesamtdauer 00:00:09
Erfolgreich: 9 von 9
```

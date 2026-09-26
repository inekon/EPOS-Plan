# Analyse des Wirtschaftlichkeitskonzepts für die Umsetzung in EPOS-Plan

**Stand 25.09.2026** (Erhebung vom 19.09.2026 abends, seither fortgeschrieben) · Codestand
`80a7b9fb` · `SchemaStand.Zielversion` = **144**, Schemaschritte 90–144 vergeben (105 = K‑1, #440; 108–110 =
Kühlung KU1; 111–113 = die Schritte E, F, G, #446; 114 = Kühlung KU2; 115 = Zapfprofil Z3, T2, #453; 119 = Kühlung
KU2 Welle 3, E34), **116–118 = die Schritte B, C, D, gebaut #461 (E9a); E9b (#462) ohne Schritt; 120 = die Sätze der
Nutzungsdauertabelle, gebaut #463 (E10); E13 (#474) und E14 (#477) ohne Schritt**; 121 = Katalogverweis des Projektgebäudes (#468),
122–123 = Anlagenkopplung AK1, 124 = Zapfprofil-Stufe Z4 (#464), **125 = das Risikomodul V‑G7, gebaut #478 (E15)**, 126 = Reparatur der Gebäude-Katalogsätze (#485), **127 = die nicht monetarisierbaren Wirkungen V‑G11, gebaut #479 (E17)**, 128 = Heizkreis je Gebäude der Anlagenkopplung AK1, Welle 3, **129 = die Wiederholperiode je Kostenposition V‑G3, gebaut #484 (E16)**, 130 = Anschlusslängen im Gebäudekatalog (#493); **E18 (#492) ohne Schritt**; 131 = Zapfprofil-Stufe Z4b (#486), 132–139 = die Cloud-Sitzungen G3, G4 und AK1, 140 = Zapfprofil-Stufe Z5 (#495), 141 = Folgeberichtigung der Anschlusslängen (#496); **E19 (#498) ohne Schritt**; 142 = dritte Reparatur der Anschlusslängen (#505); 143 = Quellenberichtigung der Baustoffe (Cloud-Sitzung G3, E39); 144 = Nachtzeit je Gebäude (Cloud-Sitzung G4, E43); **E20 (#502), E21 (#506), E22 (#503), E23 (#510), E24 (#514), E26 (#518), E25 (#519) und E27 (#521) ohne Schritt** · Referenzbasis `2026-09-25_R19_BhkwNetzbezug` · Gegenstand: das konsolidierte Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
mit den Nebenkonzepten [Nutzungsdauer/AfA](../../ueberholt/Konzept_Nutzungsdauer_AfA_EPOS-Plan.md) (seit #474 unter `ueberholt/`),
[Szenarien/VALERI](../Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md) und
[Grundlagen KWKG/Energiesteuer/Stromsteuer](../Grundlagen_KWKG_Energiesteuer_Stromsteuer.md), gemessen am
Codestand nach dem Zusammenführen vom Abend (`SchemaStand.Zielversion = 96`, nächster freier Schemaschritt
**97**; Referenzbasis `2026-09-18_R9_Kesselbrennstoff`) · Acht Prüfprotokolle mit allen Einzelbefunden,
Zeilennummern und Messungen:
[`ueberholt/Protokolle/Reporting/Analyse_Konzept_Wirtschaftlichkeit_2026-09-19/`](../../ueberholt/Protokolle/Reporting/Analyse_Konzept_Wirtschaftlichkeit_2026-09-19/)

> **Dieses Papier ändert nichts am Code und nichts an den Papieren.** Es beantwortet die Frage, was die
> Umsetzung des Konzepts heute noch verlangt: § 2 zeigt je Konzeptteil, was gebaut ist und was nicht,
> § 3 die Befunde nach Gewicht, § 4 die Entscheide, die vorher beim Anwender liegen, § 5 den
> Umsetzungsplan in Etappen mit Größe, Nachweis und Modellwahl, § 6 die Schemaschritte, § 7 die
> Berichtigungen an den Papieren. Es baut auf der Mockup-Prüfung vom Vormittag auf
> ([`2026-09-19_Pruefung_Mockups_Wirtschaftlichkeit.md`](2026-09-19_Pruefung_Mockups_Wirtschaftlichkeit.md),
> Fragen Q1–Q25) und wiederholt deren Befunde nicht. Die Protokollkennungen: `01` Rechenkern
> Kosten/Energie, `02` Rechenkern Vergütung/Steuern, `03` Datenmodell, `04` Oberfläche/Hüllen,
> `05` Berichte/VALERI/Nutzungsdauer, `06` Wiki/Hilfe, `07` Konzeptqualität/Entscheide, `08` Altanwendung.

> **Fortschreibung 22.09.2026 (E0c).** Die Erhebung selbst bleibt, wie sie war; hinzugekommen sind die
> Standmarken. **Entschieden:** Der Anwender hat am 20.09.2026 **alle Entscheide A1–A20 nach der
> Empfehlung** dieses Papiers entschieden (§ 4) — ebenso Q1–Q25 der Mockup-Prüfung. **Gebaut:** E0
> (#379), E1 (#380), E2 (#405) und E3 (#431, Merge `2cfee66b`) des Umsetzungsplans § 5, dazu DL‑2e
> (#390) für die Knopfleisten der beiden Kostendialoge und KI‑F4 (#423) für die Freigabe der Kosten-
> und Wirtschaftlichkeitsmasken an den Hilfe-Assistenten. E0 hat nur die Pflege gemacht; der Schnitt
> in drei Papiere (A13) folgte mit #435. Die **Schemaschritte 97–100** sind inzwischen anderweitig
> vergeben — § 6 ist entsprechend umgeschrieben.

> **Nachtrag 23./24.09.2026 (#432 bis #463).** Gebaut sind inzwischen auch **E4** (#432), **E5** (#434),
> **E6** (#436), **E7 Teil a** (#437: Nr. 29, Nr. 30 ohne die Kern-Regel, Nr. 32), **E7 Teil b** (#439:
> Q11 — kein Zeitzonentarif, Leistungspreis-Staffel am Stromträger, Tarifdialog im Rollenmodell; E7b‑Q1 bis
> E7b‑Q4 am 23.09.2026 entschieden), **E7 Teil c1** (#440: K‑1 mit Schemaschritt 105, das Fristende der
> Inbetriebnahme 2030 als Katalogdatum, die Kern-Regel und Kohärenzzeile zu Nr. 30; E7‑Q1 bis E7‑Q3
> umgesetzt, die acht Fragen E7c1‑Q1 bis E7c1‑Q8 am 23.09.2026 entschieden), **E7 Teil c2** (#446: die
> Schritte E, F, G als Schemaschritte 111, 112, 113, die Sperre S‑2, V‑1/V‑2, B‑4 Rest und die Reste aus E7c1;
> die acht Fragen E7c2‑Q1 bis E7c2‑Q8 am 23.09.2026 entschieden) und **E7 Teil c3** (#452: die
> Vollbenutzungsstunden nach der Definition des Anwenders, Vbh = W_a ÷ P_Nenn in beiden Fällen, B‑6 in den fünf
> Prioritätsdateien, der Kapitalwert 1024 als Datenstand, die Energiesteuer-Vorschau je Wahl, die Wahlen der
> Überlagerung als Anzeigezeilen, `VpvCtKwh` ungerundet und die Katalog-Generation 9 ohne Schemaschritt; die acht
> Fragen E7c3‑Q1 bis E7c3‑Q8 offen) — **E7 ist damit abgeschlossen** —, dazu **E8 Teil a** (#454: die
> ValERI-Ansicht mit allen fünf Blöcken — Block 2 „Zahlungsreihen" mit dem Zahlungsstrombild, Block 4 mit Spannenbild
> und Verlauf nach E6‑Q1 —, die Gliederung mit Nominalsumme und Differenzspalte, das Brückenbild, „Was daraus im Lauf
> wird" und die Fußzeile, ohne Rechenwirkung; die vier Fragen E8a‑Q1 bis E8a‑Q4 am 23.09.2026 entschieden) und
> **E8 Teil b** (#455: die Formelmappe in den Stufen 0 bis 3 — EPOS trägt die Werte ein, Excel rechnet beim Öffnen
> neu —, die Anhang-E-Checkliste in beiden Berichten und hinter einem Knopf der Ergebnisseite, die Anhang-D-Gegenprobe
> gegen `KapitalwertRechner.Rechne`, ohne Rechenwirkung; die sechs Fragen E8b‑Q1 bis E8b‑Q6 am 23.09.2026 nach
> Empfehlung entschieden) — **E8 ist damit abgeschlossen**; aus E8b dazu die Nachbesserung **E8c** (#460: die
> Betriebskostentabelle der Berichte mit den Bemessungstexten aller Arten aus dem Bemessungskatalog und der
> Gliederungsprobe mit den Positionen des ersten Jahres — E8b‑Q2 und E8b‑Q3 —, ohne Rechenwirkung; die zwei Fragen
> E8c‑Q1 und E8c‑Q2 offen); der Schnitt **A13** ist mit #435 ausgeführt. Von **E9** ist **Teil a** gebaut (#461: die
> Schemaschritte 116, 117 und 118 — Szenariorahmen, Trägerpreise und Erlössätze best/worst —, der Kern liest die
> Paare je Größe an einer Stelle, rechenwirksam je Pflege und ohne Pflege bitgleich; die sieben Fragen E9a‑Q1 bis
> E9a‑Q7 offen) und **Teil b** (#462: die Pflege in den Dialogen — die Zeilen 8 und 9 der Szenariotafel, der ±-Knopf an
> Trägerpreisen und Erlössätzen —, der Hinweistext entfällt zugunsten des Ausweises „n von m Parametern szenariert",
> ohne Pflege keine Rechenwirkung; die fünf Fragen E9b‑Q1 bis E9b‑Q5 offen) — **E9 ist damit abgeschlossen**.
> Gebaut ist auch **E10** (#463: Nutzungsdauer S3 — die Instandsetzungs- und Wartungssätze je Technik im Dialog
> „Nutzungsdauern (AfA)", gesät mit Schemaschritt 120 und wirksam nur über die ausdrückliche Vorbelegung —, die
> Speicherflotte an der Nutzungsdauertabelle mit linearem Restwert je Einheit und die Kennzeichnung der
> geräteeigenen Spalten (A8); Anker unverändert, Referenzlauf byte-gleich, keine neue Basis; von den sieben Fragen
> E10‑Q1 bis E10‑Q7 sechs offen). **Nächste Etappe: E12** (Wiki-Runden); E11 entfällt — damit ist der Etappenplan
> E0–E12 bis auf E12 abgearbeitet. Die
> Schemaschritte sind am 23.09.2026 neu geordnet und am 24.09.2026 weiter vergeben (§ 6): 101 Gebäudesimulation, 102
> Nr. 30, 103 Zapfprofilgenerator, 104 Q11, 105 K‑1 (gebaut #440), 106 Datenbereinigung der Welle #444, 107
> Gebäudesimulation E30, 108–110 Kühlung KU1, 111–113 die Schritte E, F, G (gebaut #446), 114 Kühlung KU2 (KU‑S3),
> 115 Zapfprofil-Stufe Z3 (T2, #453), 116–118 die Schritte B, C und D der Etappe E9 (gebaut #461, E9a), 119 Kühlung
> KU2 Welle 3 (E34), 120 die Sätze der Nutzungsdauertabelle (gebaut #463, E10); E9b (#462) brauchte keinen Schritt;
> 121 ist für die Zapfprofil-Stufe Z4 vorgesehen.

> **Nachtrag 24.09.2026 abends (#470, #474).** **E12** ist mit #470 vorbereitet, der Sammel-Upload am 26.09.2026
> (Version 1.2.0.4) ist freigegeben. Der Anwender hat am 24.09.2026 **alle offenen Fragen nach Empfehlung
> entschieden** (E7c3‑Q1…Q8, E8c‑Q1/Q2, E9a‑Q1…Q7, E9b‑Q1…Q5, E10‑Q1…Q7 außer dem erledigten Q5, E12‑Q1…Q4). Die zwei
> Entscheide mit offenem Bau — E9b‑Q5 b (Punkt 9 der Anhang-E-Checkliste „erfüllt", sobald Günstig und Ungünstig
> gerechnet sind) und E7c3‑Q6 a (Lade-, Speicher- und Vorsorgegrund in der Oberfläche) — sind als kleine Bauwelle
> **E13 (#474)** gebaut, dazu der Halbsatz aus A8 (eine neue Speichervariante nimmt die Nutzungsdauer der Tabelle) und
> zwei Hilfe-Anker der Seite Kosten; ohne Rechenwirkung, ohne Schemaschritt. Die Schemaschritte sind anders vergeben als
> oben vorgesehen: 121 der Katalogverweis des Projektgebäudes (#468), 122 und 123 die Anlagenkopplung AK1, 124 die
> Zapfprofil-Stufe Z4 (#464). Das Nutzungsdauer-Konzept liegt seit #474 unter `ueberholt/`.

> **Nachtrag 24.09.2026 abends (#477).** Nach dem Befund 1 aus E9a (die Formelmappe rechnete Stufe 1 und 2 nur für
> Erwartet) ist **E14 (#477)** gebaut: Die Mappe rechnet Mehrjahrestabellen, Kennzahlen, Zinsfuß und Bandbreite für alle
> drei Szenarien mit Formeln auf den Parametersatz des jeweiligen Szenarios — ergänzt E8 Teil b (V‑D); ohne Rechenwirkung,
> ohne Schemaschritt. E14‑Q2 a löst E8b‑Q1 a ab; die drei Fragen E14‑Q1…Q3 sind offen (gebaut jeweils a, → Register
> R‑E14). Freigegeben und im Bau sind E15 (V‑G7 Risikomodul) und E17 (V‑G11 nicht monetarisierbare Wirkungen); E16 (V‑G3
> Wiederholperiode je Kostenposition) folgt nach E15.

> **Nachtrag 24.09.2026 abends (#478).** Die Lücke V‑G7 aus V‑E ist mit **E15 (#478)** gebaut, auf den Auftrag des
> Anwenders „V‑G7 Risiko: eigener kleiner Auftrag ausführen": Schemaschritt **125** (`Risiko_Art`,
> `Risiko_Zinszuschlag`, `Risiko_Verlust`, `Risiko_Wahrscheinlichkeit` an `Tab_ProjektWirtschaftlichkeit`, reines
> DDL), das Risiko nach DIN EN 17463, 6.5 und Anhang F wahlweise als Zinszuschlag in allen drei Szenarien oder als
> Zahlungsstromabzug R_loss × p_loss je Periode ab Jahr 1, Vorgabe aus — ohne Pflege bitgleich, die Basis bleibt.
> R_loss ist gebaut als Betrag in € je Periode; Anhang F, Tabelle F.2 rechnet ihn als Prozent des Nettorückflusses
> (E15‑Q4 c). Die vier Fragen E15‑Q1…Q4 sind offen (gebaut jeweils a, → Register R‑E15). E17 (V‑G11) läuft mit dem
> Schritt 126, E16 (V‑G3) folgt.

> **Nachtrag 24.09.2026 abends (#479).** Die Lücke V‑G11 ist mit **E17 (#479)** gebaut, auf den Auftrag des Anwenders
> „V‑G11 … kleiner Dialog-und-Bericht-Auftrag ohne Rechenwirkung": Schemaschritt **127** (nach 126, der Reparatur
> der Gebäude-Katalogsätze #485) legt die Tabelle `Tab_ProjektWirkung` an und übernimmt einen gepflegten Freitext
> `Nicht_Monetaer` als eine Wirkung „sonstig" ohne Beurteilung; je Wirkung Kategorie (6.1), Beschreibung, Dauer und
> die Wirkung auf Organisation, Mitarbeiter und Umwelt, die Beurteilung nach 8.2 als Dauer × stärkste Wirkung (0 bis 9).
> Gepflegt im Bewertungsblock (Baustein `WirkungenListe`), ausgewiesen als Tabelle in Wort- und Tabellenbericht und in
> den Punkten 2b und 3b der Anhang-E-Checkliste — ohne Rechenwirkung, die Basis bleibt. Die vier Fragen E17‑Q1…Q4 sind
> offen (gebaut jeweils a, → Register R‑E17). Aus der Gap-Tafel des Konzepts ist nur noch V‑G3 offen (E16, #484).

> **Nachtrag 24.09.2026 abends (#484).** Die Lücke V‑G3 aus V‑E ist mit **E16 (#484)** gebaut, auf den Auftrag des
> Anwenders „V‑G3 n‑jährliche Zeitpunkte: Ausbau der Bemessung an den Kostenpositionen (eine Wiederholperiode je
> Position plus Rechenweg und Ausweis), ebenfalls mit Schemaspalte": Schemaschritt **129** (nach 128, dem Heizkreis der
> Anlagenkopplung AK1, Welle 3; in Phase 1 vorläufig 128) legt die Spalte `Wiederholperiode_a` an `Tab_ProjektWerte`
> und `Tab_KostenVorlagePosition` an. Eine Betriebsposition mit n ≥ 2 zahlt in den Jahren s, s + n, … ≤ T
> (`KapitalwertRechner.ZahltImJahr`), gepflegt im Zeileneditor „Zahlung alle: [n] Jahre", ausgewiesen als „alle n
> Jahre ab Jahr X" in der Betriebskostentabelle und als Hilfsspalte der Formelmappe — ohne Pflege ergebnisneutral,
> A/B-Nachweis an 1030 gleich der Handrechnung. Die vier Fragen E16‑Q1…Q4 sind offen (gebaut jeweils a, → Register
> R‑E16). Damit ist die Gap-Tafel V‑G des Konzepts geschlossen.

> **Nachtrag 24.09.2026 abends (#492).** Die kleine Welle **E18 (#492)** erledigt aus Konzept § 6.3 die Nr. 14 — die
> Wache hält die Stromsteuer-Rückfallebene gegen die älteste Katalogzeile in Saat und Testdatenbank (§ 6.5) — und die
> Nr. 16 — der Dialog „BHKW-Wirtschaftlichkeit" zeigt unter der Unternehmensart den erfassten Stromsteueranteil mit
> Satzabgleich und Kohärenzzeile, nur Anzeige —, auf das „fahre fort" des Anwenders zur Empfehlung der kleinen Welle.
> Nr. 18 ist nachgemessen und bleibt offen: Der Umbau der fünf Rechenweg-Sortierungen änderte die Reihenfolge in 5 von
> 13 Referenzprojekten und ist eine eigene Etappe. Kein Schemaschritt, keine Rechenwirkung — die Basis bleibt.
> E18‑Q1…Q6 sind am 24.09.2026 nach Empfehlung a entschieden, E18‑Q7 (Unternehmensart ohne BHKW) steht als Restpunkt
> Nr. 33 im Konzept (→ Register R‑E18).

> **Nachtrag 25.09.2026 (#498).** Die kleine Welle **E19 (#498)** schließt aus Konzept § 6.3 die Nr. 15 als durch die
> Schalentrennung überholt — mit einer Wache — und erledigt die Nr. 33: Ohne BHKW pflegt der Parameterdialog in der
> Gruppe Strom die Unternehmensart samt Anzeige des erfassten Stromsteueranteils, § 9b ist damit für Projekte ohne BHKW
> erreichbar (Konzept § 2.4, § 3.8); auf das „fahre fort“ des Anwenders nach dem Statusbericht. Kein Schemaschritt,
> keine Rechenwirkung — die Basis bleibt. E19‑Q1…Q6 sind am 25.09.2026 nach Empfehlung entschieden (Q4 b, → Register
> R‑E19). Mit den Papieren zu #498 nachgetragen sind die Anwenderentscheide vom 25.09.2026 zu § 6.3 Nr. 10, 11, 13, 18
> und 19 (→ Register R‑Rest): Nr. 11 und 13 geschlossen, Nr. 19 belassen, Nr. 10 präzisiert mit einer folgenden kleinen
> Bauwelle, Nr. 18 nach einer Messwelle — gemessen am 25.09.2026 ohne Rechenwirkung (12 von 13 Referenzprojekten
> byte-gleich, in 1042 nur die Modulreihenfolge der Wärmepumpen); der Entscheid über den Umbau steht aus.

> **Nachtrag 25.09.2026 (#502, #506, #503, #510, #514).** Aus § 6.3 Nr. 10 präzisiert („Wärmepumpe beides“ nur bei
> Investitionskosten nach kW elektrisch und kW thermisch) ist **E20 (#502)** gebaut: Die Investitionskosten der
> Wärmepumpe lassen sich auch „je kW elektrisch“ bemessen, Bezugsgröße P_el = Ptherm ÷ COP am Normpunkt der
> Kennlinie; die Betriebskosten blieben zunächst unverändert — ohne Rechenwirkung im Bestand, kein Schemaschritt.
> Die acht Fragen E20‑Q1…Q8 sind nach Empfehlung entschieden (E20‑Q6 zunächst offen, siehe E23). Als nächste kleine
> Welle ist **E21 (#506)** gebaut: Nr. 23 als resx-Sammelnachtrag erledigt (zwei verwaiste Ressourcenschlüssel
> gestrichen, drei Rückfalltexte angeglichen), Nr. 24 und die Datenlücken der Referenzprojekte 1018/1024/1023/
> 1030/1026 gemessen und benannt — keine Datenpflege, kein Schemaschritt (E21‑Q1…Q9 nach Empfehlung entschieden).
> Aus Nr. 18 (HB1-O1) ist **E22 (#503)** gebaut: Die acht Rechenweg-Leser der Anlagen sortieren jetzt nach
> `Ladeordnung.SqlAnlagenprio` wie Hydraulikbild und Erzeugerkarten — gepflegte Priorität zuerst, eine Anlage ohne
> Priorität nach der Regel „99“ hinten; die Referenzbasis ist deshalb neu eingefroren als
> `2026-09-25_R16_Anlagenprio` mit vierzehn Projekten, einzige Wirkung die Modulreihenfolge der zwei Wärmepumpen
> in 1042 — ohne Rechenwirkung, kein Schemaschritt. Mit dem Anwenderentscheid E20‑Q6 (b, während des Baus auf
> Strom und Wärme erweitert) ist **E23 (#510)** gebaut: Die Betriebskosten der Wärmepumpe werden nicht mehr je
> kWh bemessen — „je kWh elektrisch“ und „je kWh thermisch“ sind im Betriebsraster der Wärmepumpe gesperrt
> (`WirtschaftlichkeitCtrl.BasisGrund`, Grund GEWERK), Bestandszeilen rechnen weiter und tragen den
> Herleitungsvermerk „Altbestand“ — kein Schemaschritt, die Basis bleibt R16. Die fünf Fragen E23‑Q1…Q5 sind
> nach Empfehlung entschieden. Aus Nr. 24 ist mit dem Anwenderentscheid vom 25.09.2026 („nehme die Empfehlungen
> vor: für Später“) die Datenpflege **E24 (#514)** gebaut: Die Kessel der Referenzprojekte 1018 und 1023 tragen den
> Energieträger „Erdgas E“, 1023 dazu eine Erdgas-Projektzeile mit Preis und rechnet in einer frischen
> Wirtschaftlichkeit erstmals Energiekosten und Kapitalwert; die Referenzbasis ist neu eingefroren als
> `2026-09-25_R17_Datenpflege` (einzige Wirkung in der Simulation die Trägerkennung der Kessel in zwei
> `aggregate.csv`) — kein Schemaschritt. Die sechs Fragen E24‑Q1…Q6 sind nach Empfehlung entschieden.

> **Nachtrag 25.09.2026 abends (#518).** Mit dem Anwenderentscheid vom 25.09.2026 zu den Kern-Befunden N1 und N3
> aus E25 („Befunde aus E25: Empfehlung/bearbeiten“) ist **E26 (#518)** gebaut: Die Stromproduktion der
> Photovoltaik ist die Erzeugung der Module (vorher der Direktverbrauch), der Eigenverbrauch des Ausweises ist
> Erzeugung − Einspeisung, und der Bedarf der Strommatrix zählt alle Verbraucher des Anschlusses (Reihe
> `STROMBEDARF_GESAMT`, auch für den KWK-Split, Konzept § 3.6); „PV: vermiedener Bezug“ ist nicht mehr negativ, im
> Rollentarif sind vermiedene Menge und Kosten der Wärmepumpen-Projekte positiv, der Kapitalwert bleibt an allen
> Ankern gleich. Die Referenzbasis ist neu eingefroren als `2026-09-25_R18_PvAusweis` (einzige Wirkung
> `Photovoltaik.Stromproduktion` in vier `aggregate.csv`) — kein Schemaschritt. Die sieben Fragen E26‑Q1…Q7 sind
> nach Empfehlung entschieden; benannt bleiben Q6, N5 (kapitalwertwirksam, Empfehlung eigene Welle E27) und N6
> (Konzept § 6.3 Nr. 34).

> **Nachtrag 25.09.2026 spätabends (#521).** Mit dem Anwenderentscheid vom 25.09.2026, „E27: nach Empfehlung
> bauen“, ist der Befund N5 aus E26 als **E27 (#521)** gebaut: Der Netzbezug ist nie negativ — ein Stromüberschuss
> des BHKW, den keine spätere Stufe aufnimmt, steht allein im KWK-Split als Einspeisung, der Reststrom wird am
> Laufende bei 0 geklemmt, der Reststrombedarf der BHKW-Zeile je Stunde (Konzept § 3.6). An 1018 fällt der
> Netzbezug von −27,46 auf 0 MWh, im Rollentarif entfällt die Gutschrift der Reststromkosten (−8.237,25 → 0 €/a), die
> CO₂-Bilanz steigt um 11,95 t/a; an 1030 wandert der Kapitalwert um rund 1.700 € (Erwartet −31.142.971,06 €, Anker
> neu gesetzt). Die Referenzbasis ist neu eingefroren als `2026-09-25_R19_BhkwNetzbezug` (Wirkung allein in 1018 und
> 1030, 4/432 CSV) — kein Schemaschritt. E27‑Q1 und Q2 hat der Anwender entschieden, Q3…Q8 sind nach Empfehlung
> entschieden; benannt bleiben Q3 b (eigene Ausweisgröße „BHKW-Einspeisung“), N7 (Empfehlung Prüfwelle E28) und Q6
> (Konzept § 6.3 Nr. 36).

> **Entscheidungsregister.** Die geltende Fassung aller Entscheide führt das
> [Entscheidungsregister](Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md) (A1–A20 unter R‑A);
> § 4 bleibt als Teil dieser Analyse mit ihrem Datum stehen.

## 0 Das Ergebnis in acht Sätzen

1. **Der Rechenkern ist weiter als sein Konzept.** Kaskade, Zuschussklemme, Ersatz und Restwert mit
   Preisindex, drei Preissteigerungstöpfe, Elektrokessel-Regel, anlageneigener Strompreis, Mischsatz,
   Kontingent und Jahresdeckel je Anlage, Ersatzweg, Netting, § 53/53a/54, § 9 Nr. 3 mit vier
   Bedingungen, § 9b, CO₂-Preispfad und Methodenwechsel 2027 sind gebaut; von den 27 Befunden in § 4 sind
   zwölf erledigt, und vier Sätze der §§ 3.2/3.4 beschreiben Zustände, die es nicht mehr gibt (`01/§ 7`).
2. **Rechenwirksam offen sind drei Lücken:** der zweite Fall des § 2 Nr. 16 KWKG (Abwärmeabfuhr,
   Stromkennzahl — braucht zwei Anlagenspalten), das projektweite Doppelentlastungsverbot § 53 gegen § 54
   und die Bewertung des § 51a mit dem anzulegenden Wert statt der Einspeisevergütung; dazu als neuer
   Befund eine Restreihenfolgeabhängigkeit in Kaskadenrunde 2 und als Ausweislücke die fehlende
   CO₂-Kohärenzzeile (`01/§ 6`, `02/§ 7`). — **Stand:** Kaskadenrunde 2 ist **umgesetzt #380**, die
   CO₂-Kohärenzzeile **umgesetzt #405**; von den drei rechenwirksamen Lücken ist der zweite Fall des
   § 2 Nr. 16 (A2) **umgesetzt #440**, das Doppelentlastungsverbot (A3, Sperre der Mischlage) und die
   Bewertung des § 51a mit der Einspeisevergütung (A4) sind **umgesetzt #446**.
3. **Die größte Lücke ist keine Formel, sondern die Schicht:** Die Energiekosten- und
   Wirtschaftlichkeitsrechnung des Berichts wird allein aus der Windows-Schale gerufen
   (`WindowsFormsApplication1/Allgemein/Bericht/BerichtsDatenSammler.cs`, Zeilen 180 und 421), 6 631
   Zeilen Datenseite der Wirtschaftlichkeit und Kostenverwaltung liegen in `Views/` bei nur 341 Zeilen
   echter Fensternaht, und die iOS-Schale erreicht von der ganzen Wirtschaftlichkeit einen Dialog
   (`04/§ 4`). Auf iOS ist die Wirtschaftlichkeit heute nicht rechenbar. — **Stand:** Mit E3
   (**umgesetzt #431**) liegt der Rechenaufruf jetzt plattformfrei in
   `EPOS.Kern/Allgemein/Bericht/BerichtsDatenSammler.cs`, und `IosProjektQuelle.BerichteKostenGaben`
   liefert alle vier Seiten (Übersicht, Kosten, Wirtschaftlichkeit, Bericht). Ein bestätigender
   `ios.yml`-Lauf steht noch aus und läuft nur nach Rückfrage beim Anwender.
4. **Das größte offene Stück der Oberfläche ist die Ergebnisansicht § 2.13:** Umschalter, Bandbreite,
   Gliederung, Empfehlungskarten, Hinweistext, Dreiszenarien-Verlauf und die fünf ValERI-Blöcke fehlen
   auf `WirtschaftlichkeitSeite.razor` durchweg; von den ValERI-Etappen ist allein V‑B (Referenzwahl)
   fertig, der Excel-Generator schreibt keine einzige Formel, und der Verlauf kennt nur zwei Stricharten
   für drei Szenarien (`04/§ 6`, `05/§ 1, § 3, § 4`). — **Stand:** Die Ergebnisansicht ist gebaut (E5 **#434**,
   E6 **#436**), die fünf ValERI-Blöcke sind vollständig (E8 Teil a **#454**), und der Excel-Bericht ist eine
   Formelmappe in den Stufen 0 bis 3 (E8 Teil b **#455**); V‑E ist gebaut — im Kern (E9 Teil a **#461**) und
   mit der Pflege in den Dialogen samt dem Ausweis „n von m Parametern szenariert" (E9 Teil b **#462**).
5. **Das Datenmodell ist gesünder als das Konzept sagt:** alle 20 Wirtschaftlichkeitstabellen sind
   `STRICT`, sechs der acht als „neu" geführten Rahmen-Szenariospalten stehen seit Schritt 71; nötig sind
   sieben ergebnisneutrale DDL-Schritte (Schritte **A–G**, § 6; Nummern bei der Umsetzung — A (K‑1)
   ist 105, gebaut #440; E, F und G sind 111, 112 und 113, gebaut #446; B, C und D sind 116, 117 und 118, gebaut
   #461) und ein
   DML-Schritt für den Einheitenbruch, der entgegen dem Konzept nie gelaufen ist (Schritt 62 löscht
   Klimawaisen) — **gebaut #446** als Schritt 113 (G). Nur der Anschluss der
   Speicherflotte berührt die Einfrierregel der Referenzbasis — **gebaut #463** (E10) ohne neue Basis: Der Projektlauf
   rechnet keine Flottenwirtschaftlichkeit, der Referenzlauf blieb byte-gleich, die Einfrierregel nennt den Anschluss.
   Der zweite Migrationsmechanismus im
   `WirtschaftlichkeitCtrl` legt fünf Tabellen ohne `STRICT` und ohne Fremdschlüssel an — gegen ADR‑001,
   heute latent (`03/§ 2, § 3`).
6. **Der Nachweis trägt nicht:** Der Referenzlauf friert nur Simulationsgrößen ein, kein Test sichert
   einen absoluten Kapitalwert, drei der sechs Regressionsanker aus § 6.2 stehen in keinem Test,
   `SteuerGutschriftRechner`, `EegSatzRechner` und die EEG-Logik des `PvErloesRechner` haben keine
   Testklasse, und weder Excel- noch Word-Generator sind gedeckt (`01/§ 3–5`, `02/§ 6`, `05/§ 3.2`). —
   **Erledigt mit E1 (#380):** neun Anker in `WirtschaftlichkeitAnkerTests` (darunter absolute
   Kapitalwerte für 1024 und 1030), die drei fehlenden Rechnerklassen, `BerichtBlattstrukturWacheTests`
   für Excel **und** Word und `WirtZeileFormatWacheTests`. Offen bleibt die Frage der
   Referenzlauf-Erweiterung — sie fällt mit der nächsten Basis an (A11).
7. **Als Umsetzungsvorlage ist das Konzept nicht reif:** Der Geltungsblock sagt „ausdrücklich nicht
   implementiert", § 6.1 führt sechzehn abgeschlossene Etappen, § 7 schlägt den seit #286 gebauten
   BHKW-Dialog als nächste Etappe vor, zwei Quelldokumente der Kopftabelle haben das Repositorium nie
   erreicht, und von 151 Kennungen aus neun Quellen sind 41 offen, 34 als offen geführt, obwohl
   erledigt; fünf Kennungen sind doppelt belegt. Ein fachlicher Widerspruch ist ungelöst: das Konzept
   rechnet die Degradation in V‑E ein, das Szenarienkonzept hat sie als G3 abgelehnt (`07`). —
   **Stand:** Die Papierpflege E0 (**#379**) hat Geltungsblock, Kopf, Quellen, § 6.1, § 6.3 und § 7
   nachgezogen; der **Widerspruch zur Degradation ist mit A5 (20.09.2026) aufgelöst — V‑E ohne
   Degradation**. Offen bleibt allein der Schnitt in drei Papiere (A13).
8. **Wiki, Hilfe und Altanwendung:** 34 von 39 Bedienstücken sind beschrieben, drei erledigte Punkte
   fehlen noch (U17, U23, U36), die Hilfe-Taste der Tarifstruktur zeigt auf die falsche Seite, das
   Beispiel „Höfingen" ist ein reales Altprojekt und gehört vor jeder Wiki-Verwendung neutralisiert
   (`06`); die Zahlenprobe gegen die Altanwendung (A8, § 6.3 Nr. 20) ist nicht mehr blockiert, weil die
   Excel-Mappen auf dem Netzlaufwerk liegen und maschinell lesbar sind — es fehlt allein die in
   Rechenweg 08 zitierte Referenzmappe (`08`).

## 1 Gegenstand, Methode, Modelle

| Teil | Womit verglichen | Modell | Protokoll |
|---|---|---|---|
| Rechenkern: Rahmen, Investition, Zuschüsse, Betriebskosten, Energiekosten, Reihenfolge, Anker, Referenzlauf | Konzept § 3.1–3.5, 3.10, § 4, § 6.2, Rechenwege 01–04/08 gegen `EPOS.Kern` (Regel für Regel, Datei:Zeile), Tests gezählt, Referenzlauf-Quelltexte gelesen | Opus 5 | `01` |
| Rechenkern: KWKG, EEG, Steuern, Kohärenz, Emissionen, Gesetzeskatalog, Erlösrubrik | Konzept § 3.6–3.9, 3.11, § 4, § 5 R‑U, § 2.6, Rechenwege 05–07, Grundlagen gegen `EPOS.Kern`; Saat des Gesetzeskatalogs gegen das Grundlagenpapier | Opus 5 | `02` |
| Datenmodell und Schema | Konzept § 1.2, § 2.11.5, § 2.13 (3), § 6.5, ADR‑001, Nebenkonzepte gegen eine Kopie der Testdatenbank (`sqlite_master`, `PRAGMA table_info`, Zählungen) und die Migrationsschritte | Opus 5 | `03` |
| Oberfläche, Hüllen, Plattformfreiheit | Konzept § 2 und § 5 gegen `EPOS.UI`, `EPOS.UI.Daten`, `WindowsFormsApplication1/Views`, `EPOS.iOS` (Whitelist, Gaben), bunit-Tests gezählt; Umsetzungsstand U1–U40 | Opus 5 | `04` |
| Berichte, Formelbericht, VALERI, Nutzungsdauer, Verlauf | Konzept § 2.9–2.11, § 2.13, Szenarien- und Nutzungsdauerkonzept gegen Wort-/Excel-Generator, `ChartRenderer`, `KapitalwertRechner`, `WirtschaftlichkeitEmpfehlung`, ChartProben | Opus 5 | `05` |
| Wiki und Hilfe | Konzeptabschnitte und offene Punkte gegen `Projekte/Wiki/*.wiki`, `help_mapping.txt`, Konzept Hilfesystem § 13 | Sonnet 5 | `06` |
| Konzeptqualität und Entscheidungsregister | Geltung, Kopf, Quellen, § 5, § 6, § 7, Kennungsräume, Statuszeilen #300–#369, Git-Geschichte | Opus 5 | `07` |
| Altanwendung BHKW-Plan | die Excel-Mappen des Netzlaufwerks (Hülle, Vorlage, drei Testprojekte, Kataloge) gelesen, Blatt- und Zelltafeln erstellt, Rechenwege gegen das Konzept | Opus 5 | `08` |

Orchestrierung, Widerspruchsentscheide zwischen den Berichten und Zusammenführung: Fable 5.1. Drei
Widersprüche wurden am Code entschieden: B‑5 (`InvestSummeFuer` rechnet über `InvestKaskade.Summen`,
`BetriebskostenCtrl.cs:254`), I‑1 (`PhotovoltaikCtrl.KwpSumme`) und I‑3 (Runde 3 mit eingefrorenen
Basiszeilen) sind gebaut; R‑2 ist erledigt (`IstEnergiepreisArt`, `WirtschaftlichkeitCtrl.cs:6371`); die
KWKG-Pauschale hat ihre Rubrikzeile (`WirtschaftlichkeitZeilen.cs:407`). Wo `07` diese Punkte als offen
führt, folgt es dem Konzepttext, nicht dem Code. Nichts im Repositorium wurde verändert, nichts gebaut,
kein Test gelaufen; alle Aussagen sind Quelltext- und Datenbankmessungen.

**Nach dem Zusammenführen vom Abend** (Statuszeilen #368–#375 des anderen Rechners) gilt zusätzlich:
Schemaschritt 95 ist mit KL‑3 (Klimaspalten) und Schritt 96 mit FK‑2 (Projekt-Fremdschlüssel)
vergeben, die Testdatenbank steht auf 96, der Gesetzeskatalog-Dialog nimmt seit #372 die
`Katalogliste` des Hauses (Suche, Trichter, Sortierung), und die gesetzlichen Parameter hängen im Menü
unter Administration → Kosten. Die Protokolle nennen noch „Zielversion 94, nächster freier
Schritt 95"; alle Schrittnummern dieses Papiers sind um zwei erhöht.

## 2 Umsetzungsstand des Konzepts

Stand-Schlüssel: **gebaut** · **teils** · **fehlt** · **überholt** (Konzepttext stimmt nicht mehr).

### 2.1 Dialograum (§ 2)

| § | Gegenstand | Stand | Beleg | Bemerkung |
|---|---|---|---|---|
| 2.1 | Wirtschaftlichkeitsfelder je Anlage | gebaut | `04/§ 1` | Tabelle nennt `Form_*`-Namen (`04/n‑1`) |
| 2.2 | BHKW-Wirtschaftlichkeitsdialog | gebaut, Text **überholt** | `04/#1`, `02/d‑1…d‑5` | acht statt sechs Gruppen; „neu (BW9)" ist falsch (#286); Tarif-Sprung öffnet ein zweites WinForms-Fenster mit `MessageBox`; Überlagerung „Sätze und Herkunft" (U22) fehlt |
| 2.3 | PV-Vergütungsdialog | gebaut | `04/#2` | Marktwert-Import über `OpenFileDialog` statt `Dienste.Datei` — die einzige Plattformbindung; aufgeschlüsselte Vorschau (U27) fehlt |
| 2.4 | Parameterdialog | gebaut | `04/#3` | Hülle ohne eine einzige WinForms-Anweisung — Umzugskandidat Nr. 1 |
| 2.5 | Trägerkarte, Preisbestandteile, Emissionsanzeige | gebaut, plattformfrei | `04/#4–#8` | das Muster für alle übrigen Hüllen; Preisbasis-Kennung bleibt Altlast (U32); Sammelknopf (U11) fehlt |
| 2.6 | Erlösrubrik Block A/B | gebaut | `02/§ 5` | KWKG-Pauschale als Zeile gebaut (9c/9g erledigt); § 53/53a gegen § 54 eine Summe (U7); Komponente innen (U6) fehlt |
| 2.7 | Hausstil | teils | Mockup-Prüfung `04/B13–B15` | Farben als Literale, kein Kopfband, kein Baustein `Dialogkopf`, `SpeichernLeiste` ohne Aktionsparameter |
| 2.8 | Betriebskosten-Raster Entwurf B | gebaut | `04/#9`, U28–U31/U33–U35/U40 | Konzept sagt „nur Konzept, keine Umsetzung" — überholt |
| 2.9 | Vergleichsprojekt | gebaut | `04/#18`, Schritt 92 | — |
| 2.10 | Integrationsort ValERI | fehlt | `04/§ 6` | Aufklappblock mit einem Textfeld statt Umschalter mit fünf Blöcken |
| 2.11 | ValERI: V‑A…V‑E | nur V‑B gebaut | `05/§ 1` | siehe § 2.4 |
| 2.12 | Kategorien-Mockups | Papier | — | ein Mockup, Zahlen valide (Mockup-Prüfung) |
| 2.13 | Ergebnisansicht (1)–(6) | (1) teils, (2) Ressource, (3) teils, (4) fehlt, (5) fehlt, (6) gebaut | `04/§ 6` | Punkte 2 und 3 der „fünf fehlenden Stücke" sind mit #357 gebaut — Konzept überholt |
| 2.14 | Erfassungsgruppen | gebaut | `04/#17` | — |
| 2.15 | Vergleichssicht | gebaut | `04/#19` | — |
| 2.16 | Vergütung je Variante | gebaut | `04/#20`, Schritt 93 | — |
| Admin | Nutzungsdauern, Gesetzeskatalog, Emissionskatalog, Kostenfaktoren, Übernahme, Tarifstruktur, Verlauf | gebaut | `04/#10–#15` | Nutzungsdauern plattformfrei, aber nicht in der iOS-Whitelist; Tarifstruktur ohne Menüpunkt; Gesetzeskatalog seit #372 mit Katalogliste |

### 2.2 Rechenwege (§ 3)

| § | Gegenstand | Stand | Beleg | Bemerkung |
|---|---|---|---|---|
| 3.1 | Kapitalwert, Ersatz, Restwert, Kennzahlen, Szenariowert, Sensitivität | gebaut | `01/§ 1.1` | Formelkarte kennt weder den Endenergie-Topf mit p_E noch den Preisindex p_I des Ersatzes — beides gebaut, Konzept nachziehen; kein absoluter Kapitalwert-Anker |
| 3.2 | Drei-Runden-Kaskade | gebaut; I‑1 und I‑3 erledigt | `01/§ 1.2` | **neu:** Runde 2 zählt eine zweite `PROZENT_ERZEUGERKOSTEN`-Hauptzeile derselben Komponente mit (`InvestKaskade.cs:215–232`); Kaskadenwirkung 1042 (+20.927,61 €) in keinem Test |
| 3.3 | Zuschüsse, Klemme | gebaut | `01/§ 1.3` | Klemme `Math.Min` ohne Testfall |
| 3.4 | Betriebskosten, Vorränge, Endenergie, E1, Strompreis je Anlage | gebaut; vier Sätze **überholt** | `01/§ 1.4, § 7` | „fehlt Menge oder Satz ⇒ 0" ist seit I‑2 umgekehrt; „9 Arten" sind 10; `EUR_PRO_H`/`EUR_PRO_KWH_*` sind frisch; `InvestSummeFuer` rechnet über die Kaskade (B‑5 erledigt) |
| 3.5 | Energiekosten, Anteile, Leistungspreis, BEHG, Emissionskette | gebaut | `01/§ 1.5` | Kohärenzfall „CO₂ im Arbeitspreis und BEHG-Reihe" fehlt (`KohaerenzPruefung.cs` kennt weder `CO2` noch `BEHG`) |
| 3.6 | KWKG: Mischsatz, Tranchen, Kontingent, Deckel, Pauschale, Ersatzweg, Netting, Prüfkette; EEG | gebaut | `02/§ 1.1–1.2` | der gerechnete Satz ist der an der Anlage gespeicherte, der Mischsatz nur Vorschlag; **K‑1 fehlt** (kein `Abwaermeabfuhr`, keine Stromkennzahl); Mindestabstand § 8 Abs. 2 fehlt; § 51a mit AW statt EV (**V‑2**); Förderende 2030 nicht gesät (R‑U5) |
| 3.7 | Energiesteuer anlagenscharf | gebaut | `02/§ 1.3` | **S‑2 fehlt** (§ 53 an A und § 54 an B rechnen beide, nur Kohärenz-Hinweis); § 53a Abs. 3 fehlt (R‑U2) |
| 3.8 | Stromsteuer § 9 Nr. 3, Modus, § 9b | gebaut | `02/§ 1.4` | Erlaubnisschwelle 1.000 kW gesät, kein Leser (S‑5); `STROMST_REDUZIERT_SATZ` gesät und gelesen (S‑6 erledigt, Konzept überholt) |
| 3.9 | Kohärenzprüfung | gebaut (elf Fälle) | `02/§ 4` | CO₂-Fall fehlt; Strommix-Rückfall ist Laufhinweis, keine Kohärenzzeile; Zeilen erscheinen auf Seite, im BHKW-Dialog und im Umschlag — nicht in Word, Excel und Rubrik |
| 3.10 | Rechenreihenfolge | gebaut | `01/§ 1.6` | Schritt 5/6 (Energiekosten) läuft nur aus der Windows-Schale |
| 3.11 | Emissionsfaktoren, CO₂-Pfad, Methodenwechsel 2027 | gebaut | `02/§ 1.5, § 3` | Saat deckt sich mit dem Grundlagenpapier; `EF_BILANZ_EBEV_ERDGAS_HO`/`_UMRECHNUNG_HO` ohne Leser (Hi/Ho-Falle in der 270-g-Prüfung); EU‑ETS 2 ohne eigenen Schlüssel |

### 2.3 Befunde (§ 4), Entscheide (§ 5), Etappen (§ 6, § 7)

| Gegenstand | Stand | Beleg |
|---|---|---|
| § 4 Investitionsseite I‑1…I‑6 | I‑1, I‑2, I‑3 erledigt; I‑4 gewollt; I‑5, I‑6 offen | `01/§ 2.1` |
| § 4 Betriebsseite B‑1…B‑7 | B‑1, B‑5 erledigt; B‑4 halb; B‑2, B‑3, B‑6, B‑7 offen | `01/§ 2.2` |
| § 4 Energiekosten N1–N3 | N1, N3 erledigt; N2 gewollt | `01/§ 2.3` |
| § 4 Steuern/Vergütungen | S‑2 offen (rechenwirksam); S‑1 gegenstandslos; S‑3, S‑5 offen; S‑4 bewusst; S‑6 erledigt; K‑1 offen (rechenwirksam, entschieden 18.09.); V‑1 offen; V‑2 offen (rechenwirksam); V‑3 offen (Umfang S); V‑4 bewusst; R‑1 offen (Schema); R‑2 erledigt; R‑3 Absicht | `02/§ 2` |
| § 5 K1–K11 | K1, K3, K5, K6, K7, K8, K10, K11 entschieden/erledigt; K2 durch #365/#366 überholt; K4 im Dialog gebaut; K9 Papierkorrektur | `07/§ 1.1`, `04/§ 5` |
| § 5 D‑1…D‑3, ET‑D‑1…3, E1 | alle umgesetzt; ET‑D‑3 mit Rest U32 | `04/§ 5` |
| § 5 U‑1 Einheitenbruch | **nicht gelaufen**; Konzeptangaben (Schritt 62, Zweig) überholt | `03/§ 3.10`, `07/§ 2.10` |
| § 5 R‑U1…R‑U5 | Rechtsfragen, offen; wortgleich in Grundlagen § 6; Grundlagen § 10 Nr. 1–4 im Konzept nirgends | `07/§ 1.3` |
| § 6.1 Etappen | 19 Zeilen; es fehlen acht: B5, B6, VV (#359), Hilfsstrom (#365/#366), Nutzungsdauer S2 (#357), B‑1/#331 mit Basis #333, Übernahme #363, Bezugsgrößen #364 | `07/§ 5` |
| § 6.2 Anker | drei von sechs in Tests; 1030 auf der Investitionsseite neu verankert (410.000 €) | `01/§ 3` |
| § 6.3 Offene Punkte | 37 Punkte, 19 erledigt oder überholt, neun davon nicht gekennzeichnet; Nummer 9 doppelt, Reihe 9a…9m unsortiert | `07/§ 1.5, § 2.6` |
| § 6.4 Fallstricke | fünf von sieben gegenstandslos (ACE, `dev\`, MSBuild) | `07/§ 2.7` |
| § 6.5 Doppelte Wahrheiten | Migrationsmechanismus größer als beschrieben; Einspeisevergütung an drei statt vier Orten; `Form_Kosten`/`UcBkKosten` existieren nicht; Access-Abfrage ist die SQLite-Sicht `Abfrage_Kostenfaktoren` im Repo | `03/§ 4` |
| § 7 Reihenfolge | B5 gebaut, B6/B7 gelaufen, B8 zu vier Punkten geschrumpft (S‑2, V‑3-Rest, B‑6, I‑5), B9 nicht mehr blockiert | `07/§ 2.9`, `08/§ 6` |

### 2.4 Nebenkonzepte

| Konzept | Etappe | Stand | Beleg |
|---|---|---|---|
| VALERI | V‑A Ausweis, Deklarationen, IZF-Warnung, Steigungsspalte | **fehlt** (nur zwei Ausweissätze aus G1/G3/G5 und G10) | `05/§ 1.1, § 5.5` |
| VALERI | V‑B Referenzwahl | gebaut (#358, Schritt 92) | `05/§ 1.1` |
| VALERI | V‑C Ansicht, fünf Blöcke, Umschalter | fehlt | `04/§ 6` |
| VALERI | V‑D Formelbericht, Anhang E, Anhang D | fehlt; 0 Formeln repoweit; Normtext und Vorlage liegen unter `Quellen/VALERI/` | `05/§ 3, § 7` |
| VALERI | V‑E Szenarioabdeckung, Risiko, Degradation, n-jährlich | teils (nur Parametersatz W5‑B‑9/‑12); `FuerSzenario` variiert Zins, p_E, p_B, p_I | `05/§ 1.2`, `01/§ 6` |
| Szenarien | G1–G11 | G1, G3, G5 abgelehnt; G2, G4, G6, G7, G8, G9, G10, G11 gebaut; G7 fehlt in Excel; G8 ohne „Spanne" und Referenzzeile; G9-Text nennt „Stammprojekt" statt Referenz | `05/§ 1.3, § 5` |
| Nutzungsdauer | S1 Tabelle, S2 Vorbelegung | gebaut (#357) | `05/§ 2` |
| Nutzungsdauer | S3 Instandsetzung/Wartung, Gerätekataloge | offen, Spalten liegen (`Instandsetzung_Prozent`, `Wartung_Prozent`) — **Stand: umgesetzt #463 (E10)**: die Sätze sichtbar und gesät (Schritt 120), wirksam über die ausdrückliche Vorbelegung, neue Kesseleinträge in %/a vorbelegt | `05/§ 2.1` |
| Nutzungsdauer | fünf Stücke § 2.13 (3) | 2 und 3 gebaut; 1 Entkopplung, 4 geräteeigene Spalten, 5 Speicherflotte offen; Hinweis „k von n ohne Dauer" halb (Kostendialog ja, Seite/Bericht nein) — **Stand:** der Hinweis ganz gebaut #434, 1 gebaut #446, 4 gekennzeichnet und 5 angeschlossen #463 (A8, A7) | `05/§ 2.3` |

## 3 Befunde für die Umsetzung

### 3.1 Rechenkern

| Nr | Befund | Beleg | Umfang |
|---|---|---|---|
| R1 | **K‑1 fehlt:** keine Spalte `KWKG_Abwaermeabfuhr`, keine Stromkennzahl; die Mengenbildung rechnet stets `max(0, Klemmenerzeugung − Hilfsstrom)`; Zuschlag für Notkühler-Anlagen zu hoch | `02/§ 1.1`, `WirtschaftlichkeitCtrl.cs:2939` | L, Schema, Entscheid (Nutzwärme je Modul im Ergebnismodell?) |
| R2 | **S‑2 fehlt:** `SteuerGutschriftRechner.Energiesteuer` summiert § 53- und § 54-Beträge je Anlage ohne projektweite Gegenprüfung; nur Kohärenzfall 5 meldet die Mischlage als Hinweis | `02/§ 1.3`, `:285–425` | M, Entscheid Sperre oder Warnung (hängt an R‑U1) |
| R3 | **V‑2:** § 51a wird mit `AwMixCt` statt `EvMix` bewertet — einmaliger, kleiner Betrag | `PvErloesRechner.cs:443` | S, fachliche Klärung |
| R4 | **Kaskadenrunde 2** reihenfolgeabhängig bei zwei `PROZENT_ERZEUGERKOSTEN`-Hauptzeilen derselben Komponente — dasselbe Muster, das Runde 3 seit FX2 vermeidet | `01/§ 1.2`, `InvestKaskade.cs:215–232` | S, Testfall mit vertauschter Reihenfolge |
| R5 | **CO₂-Kohärenzzeile fehlt** (Arbeitspreis mit CO₂-Bestandteil und BEHG-Reihe gleichzeitig); `KapitalwertRechner.Rechne` bucht eine übergebene BEHG-Reihe zusätzlich | `01/B1` der Mockup-Prüfung, `02/§ 4.2` | S–M, Ausweis |
| R6 | Kohärenzzeilen erreichen weder Word- noch Excelbericht noch die Rubrik; Strommix-Rückfall (435 g/kWh) ist Laufhinweis ohne Wert im Text | `02/§ 4.3` | S |
| R7 | **U7:** `SteuerErgebnis.EnergiesteuerEur` eine Summe; der Rechner führt `summe54` intern schon getrennt — zwei Felder, zwei Zeilen, Umschlagfassung erhöhen, kein Schema | `02/§ 5.3` | M |
| R8 | **U6:** Zeilenkatalog ohne Anlagenfeld; `StromMatrix` trennt nach Tarifzone, nicht nach Anlage — der Konzeptsatz „liegen dort getrennt vor" stimmt nicht; Verteilschlüssel ist die Näherung V‑4 | `02/§ 5.2` | L, Entscheid Q15 und Verteilschlüssel |
| R9 | 9d: Nullzeilen-Gründe kommen aus den Ergebnisdaten, nicht vom Rechner; mit U7 zusammen als (Position, Grund)-Paare im `SteuerErgebnis` | `02/§ 5.4` | M |
| R10 | B‑4 Rest (`PROZENT_BRENNSTOFFKOSTEN`/`_STROMKOSTEN` nur Konserve), B‑6 (`catch {}` an fünf Stellen), B‑7 (`MengenEinheit` „€"), I‑5 (Vergleichsstrenge), S‑3 („(0 kW)" am Kessel), S‑5 (Erlaubnisschwelle ohne Leser) — **Stand:** B‑6 in den fünf Prioritätsdateien erledigt #452 (strenger Leseweg, Warnzeilen); die Gründe `Ladefehler`, `Speicherfehler` und `Vorsorgewarnung` stehen seit #474 in der Oberfläche — Statuszeile der Ergebnisseite und Dialog BHKW-Wirtschaftlichkeit, je Grund einmal, einen Datenbankfehler meldet die Anwendung selbst (E7c3‑Q6 a); der Rest außerhalb der fünf Dateien bleibt offen (E7c3‑Q5 a, eigene kleine Etappe) | `01/§ 2, § 6`, `02/§ 2` | S–M je Punkt |
| R11 | Gesetzeskatalog: 118 Zeilen stimmen mit dem Grundlagenpapier; drei Schlüssel ohne Leser; die Ho-Faktoren der EBeV-Emissionsfaktoren werden nicht gelesen (Grenzwert 270 g/kWh brennwertbezogen rund 10 % zu hoch); Förderende 2030 fehlt (Zuschlag läuft rechnerisch über 2030); EU‑ETS 2 nur als Status | `02/§ 3` | S–M |
| R12 | R‑1 Rahmenparameter je Stammprojekt, nicht je Variante — Muster für den Umbau liegt mit Schritt 92/93 vor | `02/§ 2` | L, Schema |
| R13 | Vollständige Szenarien § 2.11.5: gemessen fehlt alles außer den Positionsspalten; das heutige Modell (pauschale Prozent- und Jahresausschläge je Zeile) ist ein anderes als die Best/Worst-Werte je Parameter des Konzepts | `01/§ 6 Nr. 11` | L, Schema |

### 3.2 Architektur und Plattform

| Nr | Befund | Beleg | Umfang |
|---|---|---|---|
| P1 | **Rechenaufruf in der Schale:** `ctrl.Berechne(daten, p)` und `KostenEmissionRechner.Berechne(v)` haben ihre einzige Produktions-Aufrufstelle in `BerichtsDatenSammler.cs` (510 Zeilen Fachlogik in der Windows-Schale, gegen `CLAUDE.md` „keine Fachmaske"); iOS kann keine Wirtschaftlichkeit rechnen | `01/§ 6 Nr. 1`; nachgemessen | L |
| P2 | **Datenseite in der Windows-Schale:** 6 631 Zeilen Hüllen und Gaben in `Views/Kosten` und `Views/Wirtschaftlichkeit`, davon rund 341 Zeilen echte Fensternaht (5 %); keine Hülle setzt SQL ab, alle gehen über Kern-Controller; vier Hüllen ohne jede WinForms-Anweisung (`WirtschaftlichkeitParameterHuelle`, `KostenfaktorKatalogHuelle`, `VorlagenUebernahmeHuelle`, `ErtragBonusGaben`); `WirtschaftlichkeitSeiteGaben` hat drei Windows-Zeilen, die dritte nur wegen des `OpenFileDialog` im PV-Dialog; `KostenSeiteGaben` drei tote | `04/§ 4.2` | S je Hülle bis M |
| P3 | **iOS-Erreichbarkeit:** Whitelist in `AppWurzel.razor:1431–1445`; erreichbar ist der BHKW-Dialog; `BERICHTE_KOSTEN` steht in der Whitelist, aber `IProjektQuelle.BerichteKostenGaben` liefert in der iOS-Schale `null`; `NutzungsdauerHuelle` ist plattformfrei und trotzdem nicht erreichbar — plattformfrei heißt nicht erreichbar | `04/§ 4.1` | M |
| P4 | **Zweitfenster:** die Tarif-Sprünge aus BHKW- und PV-Dialog öffnen ein zweites WinForms-Fenster, der BHKW-Sprung mit `MessageBox`; Dateiwahl im PV-Dialog über `OpenFileDialog` (Muster `SpotpreisImportHuelle:57` über `Dienste.Datei` liegt vor) | `04/#1, #2, #14` | S–M |
| P5 | **Bausteine fehlen:** kein `Dialogkopf` (21 Dialoge in vier Bauarten), `SpeichernLeiste` ohne Parameter für Aktionsknöpfe (elf Dialoge mit eigener Leiste), Spaltenfilter im Emissionskatalog, in Kostenfaktoren und Nutzungsdauern (Baustein erprobt, Gesetzeskatalog seit #372), Szenario-±-Knopf nur an Kostenpositionen (ein Wirt), nicht an Trägerpreisen, Erlösfeldern, Rahmen | `04/§ 7` | M, für ± L mit Schema |
| P6 | **Zweiter Migrationsmechanismus:** `WirtschaftlichkeitCtrl.StelleTabellenSicher` legt fünf Tabellen per `CREATE TABLE` ohne `STRICT` und ohne Fremdschlüssel an und rüstet 55 Spalten per `SpalteSicher` nach — wörtlich die in ADR‑001 verworfene Bauart; im Normalbetrieb wirkungslos (die Vorlage bringt alles `STRICT` mit), aber `SpalteSicher` kann gelöschte Spalten wieder anlegen (Schritt 91 musste einen Eintrag entfernen). Das Konzept verlangt den Umbau nicht, § 6.5 führt die Doppelpflicht als Regel | `03/§ 2` | umgesetzt #501: `CREATE TABLE` und Nachzug der Eingabespalten entfernt, verblieben der Nachzug der drei Ergebnisspalten ohne Schemaschritt |
| P7 | Verlaufs-Ablauffolge (sammeln, rechnen, zeichnen) liegt in `KapitalwertVerlaufHuelle` (Windows); `EPOS.UI.Daten` hat keinen Ordner `Wirtschaftlichkeit`; die Zeilenliste der Seite entsteht in `WirtschaftlichkeitSeiteGaben` — der Kern liefert die Texte bereits (`NutzungsdauerAbgleich.Hinweis`), die Hülle sammelt nur die Positionen | `04/§ 4.5`, `05/§ 4.3` | M |

**Stand 22.09.2026 — umgesetzt #431, nachgemessen am Codestand `2cfee66b`.** Die Etappe **E3
Plattform**, die der Anwender mit **A1** als eigene Welle vor der Ergebnisansicht entschieden hat
(20.09.2026), ist mit Zweig `e3` (sieben Commits) und Merge `2cfee66b` gebaut; P1–P4 und P7 sind
umgesetzt, P5 und P6 bleiben unverändert.

- **P1 umgesetzt #431:** `ctrl.Berechne(daten, p)` und `KostenEmissionRechner.Berechne(v)` stehen
  jetzt in `EPOS.Kern/Allgemein/Bericht/BerichtsDatenSammler.cs` — ein Umzug, kein Umbau: Der
  Befund aus #380 (`BerichtsDatenSammler` war schon WinForms-frei, einzige Naht
  `EnergieMengen.BaueBrennstoffmengen`) hat sich bestätigt.
- **P2 umgesetzt #431:** Die Datenseite liegt jetzt in `EPOS.UI.Daten/{Kosten,Wirtschaftlichkeit,
  Bericht}`. In der Windows-Schale bleiben nur die zwei Fenster-Adapter `KostenKomponenteFenster`
  und `GesetzeskatalogFenster` sowie `Views/Kosten/ErzeugerKostenwege.cs`, das den Fensterbesitzer
  an die beiden Adapter weiterreicht.
- **P3 umgesetzt #431:** Die Positivliste der Wurzel (`EPOS.UI/Seiten/AppWurzel.razor`) führt jetzt
  **21 Schlüssel** — mit E3 Schritt 8 sind `KOSTENVERWALTUNG`, `NUTZUNGSDAUER_VERWALTUNG` und
  `GESETZESKATALOG` hinzugekommen, je Zweig mit Ablehnungstext. `IosProjektQuelle.BerichteKostenGaben`
  liefert jetzt alle vier Seiten (Übersicht, Kosten, Wirtschaftlichkeit, Bericht) aus einer je
  Sitzung gehaltenen Hülle — der Umfang wie mit **A19** entschieden (alle, in der Reihenfolge des
  Hüllen-Umzugs).
- **P4 umgesetzt #431:** Die Zweitfenster sind weg. Die Tarif-Sprünge (BHKW-Tarif, Strombezug,
  PV-Tarif) öffnen die Tarifstruktur jetzt als Überlagerung; der `MessageBox`-Sprung aus dem
  BHKW-Dialog ist mit dem Fall der alten Fensternaht ersatzlos entfallen (kein Aufrufer mehr,
  `Dienste.Dialog` war dafür nicht nötig). Die PV-Dateiwahl läuft über
  `Dienste.Datei.DateiOeffnenAsync` statt `OpenFileDialog`.
- **P5 unverändert** — nicht Gegenstand von E3. **Stand 24.09.2026: der Teil „Szenario-±-Knopf" erledigt #462**
  (E9 Teil b): Der `CaseEingabeDialog` ist ein allgemeiner Baustein, der ±-Knopf steht jetzt auch an Arbeits-, Grund-
  und Leistungspreis der Trägerkarte, an den Einspeisevergütungen PV und KWK und an DV-Entgelt und PPA-Preis; die
  Rahmen-Gruppe pflegt die Szenariotafel des Parameterdialogs (Zeilen 1 bis 4 und 8). `Dialogkopf`, die
  Aktionsknöpfe der `SpeichernLeiste` und die Spaltenfilter berührt E9 nicht.
- **P6 unverändert** — eigener Auftrag (A10).
- **P7 umgesetzt #431:** Der Verlauf (sammeln, rechnen, zeichnen) läuft jetzt plattformfrei über den
  Renderer des Kerns; `EPOS.UI.Daten` hat jetzt einen Ordner `Wirtschaftlichkeit`, der unter anderem
  `KapitalwertVerlaufHuelle` trägt.

**Das Muster für den Umzug lag vor** — mit **#428** (KI‑F8) waren vier Hüllen plattformfrei nach
`EPOS.UI.Daten` gewandert (`KlimadatenHuelle`, `ProjektKopieHuelle`, `PeakShavingHuelle`,
`StromganglinieAdminHuelle`), während Windows je Hülle einen **Fenster-Adapter** behielt und die
Wurzel dieselben Masken auf iOS über **Nähte in `IProjektQuelle`** öffnet. Nach genau diesem Muster
sind die Schritte 1, 5 und 8 der Etappe E3 gelaufen (#431).

### 3.3 Ergebnisansicht, ValERI, Verlauf, Bericht

| Nr | Befund | Beleg | Umfang |
|---|---|---|---|
| A1 | Der Stand der Seite kennt weder Bandbreite (U4) noch Empfehlungskarten je Version (U5, nur `Empfehlungszeile`), Gliederung, Brücke, die fünf ValERI-Blöcke oder den Szenario-Hinweistext (U10, `WIRT_SZEN_HINWEIS` repoweit 0 Treffer); statt des Umschalters (K8/V‑1) ein Aufklappblock mit einem Textfeld. **Stand 22.09.2026: gebaut #434** — Umschalter (K8/V‑1) mit vier Abschnitten, Bandbreite (U4), Empfehlungskarten je Version (U5), Hinweistext `WIRT_SZEN_HINWEIS` (U10), Gliederung ohne doppelte Kennzahlzeilen, die Blöcke 1, 3, 4 und 5 als Darstellung „ValERI-Bewertung"; **Rest:** Brückenbild, Block 2 und damit die fünf Blöcke vollständig (E8); der Verlauf mit drei Szenarien ist **gebaut #436** (A3). **Stand 23.09.2026: gebaut #454** — das Brückenbild, Block 2 mit dem Zahlungsstrombild und Block 4 mit Spannenbild und Verlauf (E6‑Q1): die fünf Blöcke sind vollständig | `04/§ 6` | U2, U10 S; U4, U5 M; Blöcke L |
| A2 | V‑A: keine „nachrichtlich"-Kennzeichnung, keine Deklarationszeilen (nominal, Steuern, Restwert, Risiko), keine IZF-Mehrdeutigkeitswarnung (`InternerZinsfuss` bricht bei fehlendem Vorzeichenwechsel ab, zählt nicht), keine Steigungsspalte; `Referenzwahl.Deklarationszeile` ist die Benennung der Referenz, nicht die Normdeklaration — Verwechslungsfalle. **Stand 22.09.2026: gebaut #434** — „nachrichtlich" an Amortisation und Zinsfuß (E5‑Q3), Deklarationszeilen (`ValeriAusweis.Deklarationen()`), Zähler `KapitalwertRechner.Vorzeichenwechsel` mit Warnung und „kein Zinsfuß bestimmbar" statt Abbruch (Nachweisumschlag Fassung 7), Steigungsspalte auf der Seite und in beiden Berichten — V‑A ist damit vollständig; aus demselben Normumkreis bleiben offen die Sensitivität mit T-Variation, Endzahlungen und Diagramm (Konzept V‑G6) und die Formelmappe (V‑D, E8 — **gebaut #455**, A4) | `05/§ 1.1, § 5.5` | M |
| A3 | Verlauf: `BerechneVerlauf` rechnet ein Szenario je Lauf; `VerlaufsReihen` vergibt Farben nach laufendem Index, Namen ohne Szenario; `Reihe.Gestrichelt` ist ein `bool` — drei Szenarien brauchen eine **dritte Strichart** (im Konzept nicht genannt); Bildmaß 1240 × 620 trägt zwei Legendenzeilen; ChartProben und Gegenproben vorhanden, Bilder entstehen beim Lauf. **Stand 23.09.2026: gebaut #436** — Aufzählung `ChartRenderer.Strichart` mit der dritten Strichart (Vorgabe byte-gleich), `WirtschaftlichkeitCtrl.BerechneVerlaufSzenarien` (drei Läufe ohne Speichern), Farbe = Variante und Strichart = Szenario in `VerlaufsReihenSzenarien`, zweigeteilte Legende, Bild je weitere Legendenzeile 30 px höher; der Verlauf steht als Abschnitt der Seite, „Verlauf…" ist entfallen; ChartProben 17 neue Bilder, die 91 bisherigen byte-gleich | `05/§ 4` | S–M je Schritt, ChartProben als Nachweis |
| A4 | Formelbericht: Messung des Konzepts bestätigt — 0 Formeln, 167 `.Value`, fünf Parameterzugriffe, keine Zahl des Parametersatzes erreicht eine Zelle; 1 412 statt 1 380 Zeilen; **auch der Word-Generator ist ungedeckt**; vor Stufe 0 offen: ob ClosedXML 0.105.1 Formeln mit zwischengespeichertem Wert ablegt, ob `RecalculateAllFormulas` NBW/RMZ/IKV trägt, ob andere Tabellenkalkulationen dieselben Werte zeigen. **Stand 23.09.2026: gebaut #455** — die Fragen gemessen: ClosedXML legt kein Ergebnis ab (auch nicht nach `RecalculateAllFormulas`), seine Rechenmaschine kennt `NPV` und `IRR` nicht, `PMT` rechnet sie; daraus die Regel „EPOS trägt die Werte ein, Excel rechnet beim Öffnen neu" (Konzept § 2.11.6), Excel 16 rechnet jede Formelzelle auf den eingetragenen Wert, andere Tabellenkalkulationen sind nicht gemessen; die Formelmappe in den Stufen 0 bis 3 mit Wertfassung = Formelfassung in 13 Prüfgruppen, die Wache über den Befund (`FormelmappeClosedXmlBefundTests`) — erledigt | `05/§ 3` | Wache M, Stufen 0–3 M/L/L/S |
| A5 | Bandbreite G8 in Word und Excel ohne „Spanne" und Referenzzeile, auf der Seite gar nicht; G9-Ressource `WIRT_EMPF_KEINE` nennt „Stammprojekt", Maßstab ist seit #358 die gewählte Referenz; G7-Zeitraumhinweis fehlt im Excel-Blatt; Word druckt nur Erwartet, Excel drei Blöcke — nirgends als Entscheid vermerkt. **Stand 22.09.2026: gebaut #434** — die Bandbreite mit Spanne, Referenzzeile und Einstufung steht jetzt auch auf der Seite, beide Berichte lesen sie aus einem Modell (`BerichtsDaten.Bewertung`); G7, G8 in den Berichten und G9 kamen mit #405 | `05/§ 5, § 6.3` | S je Punkt |
| A6 | Sichtbarkeitsregel hält (Seite, Word, Excel, BHKW-Vorschau ziehen `WirtschaftlichkeitZeilen.Sichtbare`); `Format`/`ExcelFormat` nur durch Disziplin gekoppelt, kein Wächter; „Nach #342" (vier gegen zwei Nachkommastellen der KWKG-Sätze) betrifft Rechner-Herleitung, Excel und Dialogzeile zugleich | `05/§ 6` | S |
| A7 | Anhang-E-Checkliste und Anhang-D-Gegenprobe: nichts gebaut; Normtext und `VALERI_Vorlage_V7.xlsx` liegen unter `Quellen/VALERI/`; die Fallstudie gehört als Prüfvorrichtung gegen `KapitalwertRechner.Rechne`, nicht gegen die volle Kette; zwei Zeilen der Sensitivitätstafel D.6 tragen im Normtext Werte des Pumpenbeispiels und dürfen nicht in die Vorrichtung. **Stand 23.09.2026: gebaut #455 — erledigt:** die Anhang-E-Checkliste (15 Punkte in fünf Gruppen, Stand je Punkt aus dem Lauf) als Abschlussseite des Wortberichts, als letztes Blatt der Mappe und hinter dem Knopf „Anhang-E-Checkliste…" der Ergebnisseite (U43); die Anhang-D-Gegenprobe als Kern-Fall `AnhangDFallstudieTests` gegen `KapitalwertRechner.Rechne` — 64.479,51 €, −202.801,57 € und 598.319,65 € gegen 64.480 €, −202.802 € und 598.320 € der Norm, die Tafel D.5 und sechs Zeilen der Tafel D.6 treffen; ausgenommen die zwei Zeilen des Pumpenbeispiels und die Zeile „Gasverbrauch BHKW" (sie trifft den ganzen Energie-Nettostrom), vermerkt der Tippfehler 348.583 statt 349.583 kWh/a in D.7 | `05/§ 7` | S (Checkliste), M (Gegenprobe) |
| A8 | Etappenkürzel kollidieren: V‑G1…V‑G12 des Konzepts gegen G1…G11 des Szenarienkonzepts meinen Verschiedenes (V‑G2 Degradation gegen G2 Preisänderung; V‑G6 Sensitivität gegen G6 nicht monetär; V‑G11 nicht monetär gegen G11 investitionsgekoppelt); daraus der falsche Eintrag V‑G11 „fehlt" und der fachliche Widerspruch Degradation (V‑E) gegen G3 (abgelehnt 09.09.2026) | `05/§ 8.2`, `07/§ 2.11` | Papier, Entscheid |

### 3.4 Datenmodell und Schema

| Nr | Befund | Beleg |
|---|---|---|
| D1 | Namen: das Konzept sagt „Satz" und „Betrag", die Spalten heißen `Einheitpreis` und `EingegebenerWert`; `BestCase`/`WorstCase` heißen `Bestcase`/`Worstcase`; `Tab_Energieanlagen` trägt neun `KWKG_*`-Spalten und keine Nutzungsdauer | `03/§ 1.3` |
| D2 | § 2.11.5 „Rahmen, 8 Spalten neu": sechs stehen seit Schritt 71 (`Szen_Best/Worst_Zins`, `_Preis_E`, `_Preis_B`); neu sind Betrachtungszeitraum und Mengenfaktor; Namensfalle: `Szen_*_Dauer` ist die Nutzungsdaueränderung | `03/§ 1.3` |
| D3 | Trägerpreise best/worst, Erlössätze best/worst, Mengenfaktor: nicht vorhanden — Bedarf bestätigt | `03/§ 3.2` |
| D4 | Speicherflotte: `ErsatzintervallJahre` und `RestwertEuro` sind keine Spalten, sondern JSON-Felder des Flottenstands in `Tab_SpeicherAuslegung` — ihr Anschluss ändert den Flottenstand des Projekts 1046 und berührt die **Einfrierregel** — **Stand #463 (E10):** angeschlossen, ohne den Flottenstand zu ändern: Die Studie rechnet den Restwert je Einheit linear aus dem Ersatzintervall, ein Intervall 0 nimmt die Batteriezeile der Tabelle, `RestwertEuro` der Einheit ist Altfeld; die Referenz bewegt sich nicht, die dritte Einfrierregel nennt den Anschluss | `03/§ 3.6` |
| D5 | U‑1 nicht gelaufen: Schritt 62 ist `Schritt_62_KlimaWaisen`; die fünf Gase führen `m³`, `energy_carrier.billing_unit` seit Schritt 26a `Nm³`, eine `energy_price`-Zeile `m³`; das Einheitenbruch-Konzept liegt in `ueberholt/`, der genannte Zweig existiert nicht | `03/§ 3.10`, `07/§ 2.10` |
| D6 | `Nachweis_Json` in 0 von 78 Ergebniszeilen der Testdatenbank gefüllt, obwohl § 6.3 Nr. 17 die Persistenz als erledigt führt (Zeilen älter als B7P?); sieben Energieanlagen mit `KWKG_Anlagenart = ''` statt NULL; 95 von 101 Kat.‑1-Positionen ohne Nutzungsdauer (Konzept: 103 von 109), 27 davon mit Betrag; `NutzungsdauerID` in 0 von 164 Projektpositionen gesetzt — das Werkzeug zum Nachpflegen ist gebaut, der Bestand nicht nachgepflegt | `03/§ 1.3, § 5.3` |
| D7 | Fremdschlüssel: `Tab_ProjektWerte` trägt keine FK auf Anlage und Vorlage — bewusst (gelbe Zeile § 2.14); kein FK nachrüsten, ohne den Entscheid neu zu stellen | `03/§ 5` |
| D8 | Ersatz/Restwert-Kennzeichen: an der Position (`Tab_ProjektWerte` und `Tab_KostenVorlagePosition`, nullbar, NULL = wie bisher) oder an der Technik — der Konzepttext lässt beides zu; Empfehlung Position | `03/§ 3.3` |
| D9 | Testdatenbank, Auslieferungsvorlage und Erstbereitstellung brauchen für neue Spalten keine Sonderbehandlung; die Doppelpflicht „Schritt und `SpalteSicher`" gilt für `Tab_ProjektWirtschaftlichkeit`, `Tab_ErgebnisWirtschaftlichkeit`, `Tab_ErgebnisStromMatrix` und `Tab_ProjektPhotovoltaik`, solange P6 besteht | `03/§ 6.1` |

### 3.5 Nachweis und Tests

| Nr | Befund | Beleg |
|---|---|---|
| N1 | `EPOS.Referenzlauf` und `Referenzlauf/` frieren ausschließlich Simulationsgrößen ein (`aggregate.csv`: 160 Größen, keine aus der Wirtschaftlichkeit); jede Änderung an Kaskade, Betriebs-, Energiekosten oder Kapitalwert läuft dort unbemerkt durch | `01/§ 4`, `02/§ 6` |
| N2 | Kein Test sichert einen absoluten Kapitalwert; alle Zusicherungen sind Differenz- oder Bitgleichheitsproben; 99,00 €/a, −2.220.322,32 € und +20.927,61 € aus § 6.2 stehen in keinem Test | `01/§ 3` |
| N3 | `SteuerGutschriftRechner`, `EegSatzRechner`, `PvKennzahlenRechner`, `StromTarifRechner`: keine Testklasse; `PvErloesRechner`: § 51, Kappung, Marktprämie, § 51a ungetestet — die elf Handproben Energiesteuer und die 16 BNetzA-Werte EEG sind als Soll dokumentiert und warten auf ihre Tests | `02/§ 6` |
| N4 | `ExcelBerichtGenerator.Erzeuge` und `WordBerichtGenerator.Erzeuge` werden von keinem Test gerufen; ohne Wache über die Blattstruktur ist keine Stufe des Formelberichts abnehmbar. **Stand 23.09.2026: geschlossen** — `BerichtBlattstrukturWacheTests` ruft beide Generatoren seit E1 (#380) und hält mit E8 Teil b (#455) auch die Stellen fest, die die Stufen umbauen (Kopf und Δ%-Block des Vergleichsblatts, Parameterblock, Mehrjahrestabellen, Kennzahlen, Betriebskosten, die Abschnittsfolge des Wortberichts): 6 Fälle auf `591229e1`, 8 vor Stufe 0 (E8b/0), 13 nach Stufe 3 und dem Nachzug; jede Stufe vorher/nachher abgenommen | `05/§ 3.2, § 3.4` |
| N5 | Der kleinste Nachweisweg für Rechenänderungen: dotnet-Dateiskript gegen die reinen Rechner (`KapitalwertRechner`, `ErsatzRestwertTafel`, `KwkgSatzRechner`, `EegSatzRechner`) plus die 14 datenbanknahen Testklassen; die 16 `InlineData`-Zeilen in `InvestKaskadeTests` sind der einzige projektweite Zahlenanker | `01/§ 4` |
| N6 | Testabdeckung dieses Feldes gezählt: Teil 1 27 Klassen mit 253 Fakten und 16 Theorien (85 Datensätze); KWKG-Seite gut, Katalog sehr gut; die ValERI-Zeilen über `ValeriLueckenTests` (15) und `SzenarioParameterTests` (17) | `01/§ 5`, `02/§ 6` |

### 3.6 Das Konzept als Vorlage

| Nr | Befund | Beleg |
|---|---|---|
| K1 | Geltungsblock (Z. 25–29, „ausdrücklich nicht implementiert", Arbeitsregel 30.08.2026) gegen § 6.1 mit sechzehn abgeschlossenen Etappen und drei Überschriften „umgesetzt" — der schwerste Widerspruch | `07/§ 2.1` |
| K2 | Kopfzeile: Codestand `922228a` ist eine Kennung vor dem Umschreiben (heute `e1c4275e`); Quelltabelle nennt Formelkarte und Feldkarte, die das Repositorium nie erreicht haben (Sitzungs-Scratchpad) — § 4 ist gegen seine Quelle nicht mehr prüfbar; fünf von sechs Artifacts sind durch Repo-Dateien abgelöst | `07/§ 2.2–2.4` |
| K3 | § 7 schlägt B5 vor (gebaut seit #286), B8 führt drei erledigte Befunde, B9 ist nicht mehr blockiert; „Voraussetzungen vor der Umsetzung" nennt zwei gefallene Entscheide und keinen der heutigen | `07/§ 2.9` |
| K4 | § 6.1 fehlen acht Etappenzeilen (siehe § 2.3); 56 von 66 Statuszeilen #300–#369 betreffen das Feld, acht führt das Konzept | `07/§ 5` |
| K5 | Kennungen: 151 aus neun Quellen, 41 offen, 34 als offen geführt, obwohl erledigt; Kollisionen K‑1/K1, B‑1/B‑1 (benannt), U‑1/U1, V‑1/V‑1, K8/K‑8 (nicht benannt); V‑G gegen G; D‑1 gegen d‑1; Statusnummern #302, #304, #328, #331 je doppelt | `07/§ 1.10` |
| K6 | Grundlagen § 10 Nr. 1–4 (ETS‑2-Mechanismus, § 10 Abs. 3 BEHG, Projektionsbericht 2026, Enddatum Versteigerung) tragen den CO₂-Preispfad des § 3.11 und stehen im Konzept nirgends; Grundlagen § 5 (Werte der Altanwendung) ist die Abnahmeliste für A8 und wird nicht genannt | `07/§ 4` |
| K7 | Struktur: 14 von 16 Unterabschnitten des § 2 tragen ein Entscheiddatum in der Überschrift, sieben Blöcke sind durchgestrichen, Auftrags- und Wellenkürzel stehen im Fließtext — das Papier ist zu gleichen Teilen Stand und Geschichte; achtzehn falsche Aussagen des Tages entstanden, weil eine Geschichtszeile wie eine Regelzeile gelesen wurde | `07/§ 3.3` |
| K8 | Was ein Umsetzer je Etappe braucht: Rechenwirkung und Reihenfolge liefert das Papier, Testklasse, Wiki-Seite und Größe fehlen durchgängig; die einzige vollständige Etappenbeschreibung ist § 2.11.6 (Formelbericht) — das Muster für die übrigen | `07/§ 3.1–3.2` |

### 3.7 Wiki und Hilfe

| Nr | Befund | Beleg |
|---|---|---|
| W1 | Keine Seite „Berechnung/Wirtschaftlichkeit" unter `EPOS.Kern/Allgemein/Hilfe/Berechnung/` — nach Regel 13.1 folgerichtig (keine Formeln im Wiki), aber eine Entscheidung, keine Lücke | `06/§ 2.2` |
| W2 | Über die fünf Lücken der Mockup-Prüfung hinaus fehlen drei erledigte Punkte: KWKG-Pauschale als Zeile (U17), Satzherkunft-Zeilen (U23), zweite PV-Anlagenwarnung (U36); G7/G8-Hinweiszeilen ohne erkennbaren Anker; p_I-Feld ohne eigenen Anker | `06/§ 2.1, § 2.4` |
| W3 | Hilfe-Zuordnung: `Form_Tarifstruktur.btn_Help` zeigt auf „Kosten", der Inhalt liegt auf `Wirtschaftlichkeit#strombezug`; `Form_Gesetzesparameter` zeigt auf eine Seite ohne Repo-Quelle (seit #372 unter Administration → Kosten); dazu die bekannten Lücken (BHKW ohne Ziel, PV zu grob, acht bereite Anker unverdrahtet) | `06/§ 4` |
| W4 | „Höfingen" ist mit hoher Wahrscheinlichkeit ein reales Altprojekt (Altmappe `Quellen/BHKWPlan/`, „reale Höfingen-Zahlen", nicht runde Beträge) — vor jeder Wiki-Verwendung neutralisieren; Regel 13.2 sollte Mockup-Beispiele ausdrücklich einschließen | `06/§ 5` |
| W5 | Pflegeplan: 19 offene U-Zeilen, V‑C…V‑E und S3 haben je Seite, Anker und Logbuch-Einstufung; Sätze auf Vorrat entworfen | `06/§ 3` |

### 3.8 Zahlenprobe gegen die Altanwendung (A8, § 6.3 Nr. 20, § 7 B9)

**Anwenderentscheid 22.09.2026: „BHKW-Plan-Mappen: nicht relevant."** Die Zahlenprobe gegen die
Altanwendung entfällt damit (Etappe E11, § 7 B9, Entscheid A17); der Abschnitt bleibt als Befund der
Analyse stehen, der Nachweis der Wirtschaftlichkeitsgrößen läuft über die Anker aus E1 und die
A/B-Nachweise der rechenwirksamen Etappen.

Die Sperre „wartet auf Zulieferung der BHKW-Plan-Excel" ist aufgehoben: Die Mappen liegen auf dem
Netzlaufwerk und sind maschinell lesbar (`08/§ 1`). Die Programmhülle `BHKW-WP-PLAN.XLSM` (neuere
Fassung 23.08.2026) enthält keine Rechenlogik in Zellen; die gesamte Wirtschaftlichkeit steckt in der
Projektvorlage `TABELLEN.XLS` (41 Blätter): Eingaben `Tab_Kosten`, Erlöse `Tab_Erloese`, statischer
Jahresvergleich `Tab_Wirtschaftlichkeit`, Kapitalwert `Tab_Wirtschaftlichkeit_kap` und **ein zweiter
Kapitalwert mit anderen Preissteigerungen** auf `Tab_kurz_KWKG2020` (im Testprojekt −72.507 gegen
−79.187 €). Der Rechenkern deckt sich besser als erwartet: Annuität ohne Restwert, Energiesteuer
5,50 €/MWh auf den ungeteilten BHKW-Brennstoff mit Faktor 1,108, Vbh-Kontingent 30.000 h und Jahresdeckel
5.000/4.000/3.500 h stimmen mit dem Konzept; es fehlen Restwert, Ersatz, § 53a, § 54, § 9b,
Eigenstromtatbestand; Ölsteuerbasis und Stromsteuermenge laufen auseinander (`08/§ 5`). Die drei
benannten Testprojekte tragen nur zur Hälfte: `goetz_test.XLS` ist ein synthetischer Funktionstest,
die beiden `englmar`-Mappen sind ein echtes Projekt der älteren Blattgeneration ohne Kapitalwertblatt.
`Rechenweg/08` enthält bereits eine bestandene Gegenprobe („Höfingen", Kapitalwert 65.259 €), aber keine
der drei Höfingen-Dateien des Bestands trägt diese Zahlen — die Referenzmappe ist zu benennen oder zu
ersetzen (`08/§ 6.1`). Prüfverfahren in sieben Schritten mit Referenzzellen: `08/§ 6.5`; Umfang M ohne
die Mengenprobe über die Stundenreihen.

## 4 Entscheide, die die Umsetzung braucht

Zusätzlich zu Q1–Q25 der Mockup-Prüfung (dort § 4). Ein Stern heißt: blockiert mehr als eine Etappe.

> **Alle zwanzig Entscheide sind gefallen: „entschieden 20.09.2026 nach Empfehlung"**
> (Anwenderauftrag vom 20.09.2026 „fahre fort mit der Umsetzung der Wirtschaftlichkeitsberechnung nach
> Konzept wie im Mockup" mit dem Zusatz „Entscheidung nach Empfehlung"; Statuszeile **#405**).
> Die Empfehlungsspalte unten **ist damit der Entscheid** — sie bleibt im Wortlaut stehen. Ausdrücklich
> bestätigt hat der Anwender **A5** (V‑E ohne Degradation), **A3** (Sperre mit Begründungszeile) und
> **A4** (§ 51a mit dem anzulegenden Wert, eigener Testfall). Was jeder Entscheid für den Bau bedeutet,
> steht in der Tafel unter der Entscheidtabelle.

| # | Frage | Empfehlung |
|---|---|---|
| **A1\*** | Rechenaufruf und Datenseite aus der Windows-Schale holen (P1, P2, P3) — als eigene Welle vor der Ergebnisansicht, oder erst mit ihr? | **Vorher**, in der gemessenen Reihenfolge (§ 5 E3): sonst entsteht jedes neue Stück der Ergebnisansicht ein zweites Mal nur für Windows |
| **A2\*** | K‑1: Stromkennzahl und Abwärmeabfuhr je Anlage (Schritt **A**, § 6) — Nutzwärme je Modul aus dem Ergebnismodell oder Aufteilung nach Leistung? | Vor der Umsetzung messen, ob die modulscharfe Nutzwärme vorliegt; sonst Aufteilung nach P_el mit Herleitungszeile |
| **A3** | S‑2: Mischlage § 53/53a neben § 54 sperren (§ 54-Betrag verwerfen) oder als Warnung hochstufen? | **Sperre** mit Begründungszeile — solange R‑U1 offen ist, ist die Kombination nie zulässig |
| **A4** | V‑2: § 51a mit der Einspeisevergütung bewerten, wenn die Anlage feste Vergütung fährt? | Ja, nach Volltextprüfung; eine Zeile, eigener Testfall |
| **A5\*** | Degradation: V‑E des Konzepts rechnet sie ein, G3 des Szenarienkonzepts lehnt sie ab | Entscheid neu stellen; bis dahin V‑E ohne Degradation planen |
| **A6** | Ersatz/Restwert-Kennzeichen je Position (Schritt **E**, § 6) oder je Technik? | **Position**, nullbar, NULL = wie bisher |
| **A7** | Speicherflotte an `Tab_Nutzungsdauer` anschließen (Basis neu einfrieren) — jetzt oder mit ND‑S3? | Mit ND‑S3, als eigener Auftrag mit Neueinfrieren |
| **A8** | Geräteeigene Nutzungsdauer-Spalten (`Tab_BHKW`, `Tab_Heizkessel`) abkündigen? | **Nicht jetzt** — kennzeichnen; die Speichervariante sollte die Positionsarten 20/21 lesen |
| **A9** | U‑1 Einheitenbruch (Gase `m³` → `Nm³`) als DML-Schritt **G** (§ 6) freigeben? | Ja, vor dem nächsten Vorlagenbau; die fünf Randfragen ins Register |
| **A10** | Zweiten Migrationsmechanismus entkernen (fünf `CREATE`, 55 `SpalteSicher`, Rückfallebene `SchemaKatalog.Alle`)? | Ja, als eigener Auftrag nach den Schritten 97–102 — bis dahin gilt die Doppelpflicht |
| **A11\*** | Nachweis: Referenzlauf um Wirtschaftlichkeitsgrößen erweitern oder Ankertests im Kern? | **Ankertests zuerst** (drei fehlende § 6.2-Anker, ein absoluter Kapitalwert je Referenzprojekt), Referenzlauf-Erweiterung als Frage für die nächste Basis |
| **A12** | Erlösrubrik U6: Eigenverbrauch je Anlage nach der Näherung V‑4 verteilen (Zwischensumme sagt „Näherung") oder modulscharfe Stundenreihen? | Näherung, ausgewiesen — Stundenreihen sind ein Simulationsthema |
| **A13\*** | Konzept in drei Papiere schneiden (gültiger Stand, Entscheidungsregister, Protokoll der Entscheidwege)? | **Ja**, vor der ersten Codeetappe; Papierpflege ohne Entscheid zuerst (§ 5 E0) |
| **A14** | Wortlaut des Szenario-Hinweistexts (Konzept § 2.11.7 gegen Mockup) und des G9-Vorschlagssatzes mit gewählter Referenz | Konzeptfassung ohne den Roadmap-Satz; G9 nennt die Referenz beim Namen |
| **A15** | „Nach #342": vier Nachkommastellen der KWKG-Sätze in Rechner-Herleitung, Excel und Dialogzeile in einem Zug? | Ja, ein Auftrag |
| **A16** | „Höfingen" in Konzept, Rechenweg 08 und künftigen Wiki-Texten neutralisieren („Beispielprojekt B", gerundete Beträge)? | Ja, vor dem Sammel-Upload 28.09.2026 |
| **A17** | A8/B9: Referenzmappe beschaffen oder aus den 231 Bestandsmappen ersetzen; maßgebliches Kapitalwertblatt festlegen (`_kap` oder `kurz_KWKG2020`) | `_kap` als Blatt der Vorlage; Referenzmappe aus dem Bestand mit 41 Blättern und echten Zahlen |
| **A18** | Hilfe: eigene Wiki-Seite „Gesetzliche Parameter" anlegen oder `help_mapping` auf einen Abschnitt der Kostenseite umbiegen? | Abschnitt auf der Seite Kosten (Menüort seit #372) |
| **A19** | iOS-Whitelist: welche Wirtschaftlichkeitsseiten sollen auf dem Gerät erreichbar sein (Kostenverwaltung, Parameter, Nutzungsdauern, PV, Tarif, Verlauf, Berichte & Kosten)? | Alle, in der Reihenfolge des Hüllen-Umzugs; iOS-Lauf nur nach Rückfrage |
| **A20** | Katalogpflege ohne Leser: `KWKG_MINDESTALTER_*` (Mindestabstand § 8 Abs. 2) lesen, Förderende 2030 säen, EU‑ETS‑2-Schlüssel anlegen? | Förderende ja (R‑U5), Mindestabstand nur mit Inbetriebnahmedatum der Altanlage, ETS 2 mit dem Preispfad |

**Stand je Entscheid am 22.09.2026.** Alle zwanzig sind entschieden; gebaut ist, was die Spalte sagt.

| # | Umsetzungsstand | Etappe |
|---|---|---|
| **A1** | entschieden **und gebaut** — E3 ist umgesetzt (**#431**) | E3, erledigt |
| **A2** | entschieden, nicht gebaut; die Messung der modulscharfen Nutzwärme steht aus | E7 |
| **A3** | entschieden (**Sperre** mit Begründungszeile), nicht gebaut — Befund S‑2 | E7 |
| **A4** | entschieden (§ 51a mit dem anzulegenden Wert, **eigener Testfall**), nicht gebaut; der heutige Weg ist mit `PvErloesRechnerEegTests` **gepinnt** (#380) | E7 |
| **A5** | entschieden: **V‑E ohne Degradation** — der Widerspruch zum Szenarienkonzept (G3) ist aufgelöst; beide Papiere sind nachgezogen | E9 |
| **A6** | entschieden (Kennzeichen je **Position**, nullbar), nicht gebaut | E7 |
| **A7** | entschieden (Speicherflotte **mit ND‑S3**, eigener Auftrag mit Neueinfrieren) **und gebaut #463** — linearer Restwert je Einheit aus der Nutzungsdauer, Intervall-Vorgabe aus der Tabelle, `RestwertEuro` der Einheit Altfeld; das Neueinfrieren entfällt, der Referenzlauf ist byte-gleich (E10‑Q3, → Register R‑E10) | E10, erledigt |
| **A8** | entschieden (geräteeigene Spalten **nicht jetzt**, nur kennzeichnen) und **gekennzeichnet #463** — „Nutzungsdauer (Gerätedaten)" mit Tooltip, KI- und Verwendungsvermerk, kein Schritt (H); der Halbsatz zur Speichervariante (Positionsarten 20/21) **erledigt #474** — nur Vorgabe neuer Einträge: eine neue Speichervariante nimmt die Nutzungsdauer der Zeile „Stromspeicher · Batterie" | E10, E13 |
| **A9** | entschieden (U‑1 freigeben, vor dem nächsten Vorlagenbau), nicht gebaut | E7 |
| **A10** | entschieden (entkernen, als **eigener Auftrag** nach den Schemaschritten dieser Reihe) | eigener Auftrag |
| **A11** | entschieden **und gebaut** — die Ankertests stehen seit **#380** (§ 6.2 des Konzepts) | E1, erledigt |
| **A12** | entschieden (Näherung, ausgewiesen), **gebaut #432** (`VermiedenAnlageNachweis.Verteile()`, Herleitung nennt „Näherung"; U6‑Q1 entschieden 22.09.2026 — Bezugsgröße ohne jede Eigenerzeugung mit E7, Konzept § 6.3 Nr. 32) | E4 |
| **A13** | entschieden (**ja**, Schnitt in drei Papiere), **A13 ausgeführt #435** — Konzept (gültiger Stand), Entscheidungsregister und Protokoll der Entscheidwege; E0 (#379) hatte nur die Pflege gemacht, E0c die Fortschreibung | **nach E5, vor E6** (Anwender 22.09.2026 nach Empfehlung: erst wenn E4 und E5 die § 2.6 und § 2.13 umgebaut haben) — erledigt |
| **A14** | entschieden (Konzeptfassung ohne Roadmap-Satz; G9 nennt die Referenz beim Namen) **und gebaut** — der **G9-Teil mit #405**, der Hinweistext (U10) **gebaut #434** als `WIRT_SZEN_HINWEIS` de/en (die Zahlen im Text sind die wirksamen, Vorgaben oder gepflegte Sätze) | E5, erledigt |
| **A15** | entschieden (ein Auftrag), nicht gebaut | offener Auftrag |
| **A16** | entschieden („Höfingen" neutralisieren), ausgeführt | #470 |
| **A17** | **gegenstandslos** — Anwenderentscheid 22.09.2026 „BHKW-Plan-Mappen: nicht relevant": die Zahlenprobe gegen die Altanwendung entfällt, eine Referenzmappe wird nicht benannt | E11 entfällt |
| **A18** | entschieden (Abschnitt auf der Seite Kosten), ausgeführt | #470 |
| **A19** | entschieden **und gebaut #431** (**alle**, in der Reihenfolge des Hüllen-Umzugs; iOS-Lauf nur nach Rückfrage) | E3 Schritt 8, erledigt |
| **A20** | entschieden (Förderende ja; Mindestabstand nur mit Inbetriebnahmedatum, ETS 2 mit dem Preispfad), nicht gebaut | E7 |

**Nicht** von diesem Entscheid gedeckt sind die drei Punkte, die erst mit E0 und E2 entstanden sind und
keine Empfehlung tragen: **Hi/Ho am CO₂-Grenzwert (R11)**, die sieben Energieanlagen mit leerer
`KWKG_Anlagenart` und `Nachweis_Json` in 0 von 78 Ergebniszeilen (Konzept § 6.3 Nr. 29, 30, 31). Sie
brauchen je ein eigenes Wort des Anwenders, mit E7. **Stand 22.09.2026: alle drei entschieden**
(→ Register R‑NR): Nr. 29 „es gilt immer der Brennwert" und Nr. 30 NULL statt leerer Zeichenkette,
beide mit E7; Nr. 31 kein Nachziehlauf, die Kennzeichnung ist gebaut #434.

## 5 Umsetzungsplan

Größe: S ≤ ½ Tag · M 1–2 Tage · L > 2 Tage. Modell nach `CLAUDE.md`: Opus 5 für Umsetzung, Tests und
Hüllen; Sonnet 5 für Suchen, Listen und Textpflege; Fable 5.1 nur für Konzeptarbeit. Nachweis: „Anker"
= die Ankertests aus E1; „Referenzlauf" = byte-gleich gegen R9 (die Basis der Erhebung; die Zeilen nennen die Basis ihrer
Zeit, seit E30 (#548) gilt `2026-09-26_R21_BhkwDeckung`); „bunit" = `EPOS.UI.Tests`.

| Etappe | Inhalt | Größe | Rechenwirkung | Nachweis | Schema | Wiki | Modell | Voraussetzung |
|---|---|---|---|---|---|---|---|---|
| **E0 Papierpflege** — **umgesetzt #379** (A13 ausgeführt #435) | Kopfzeile, Geltungsblock, Quelltabelle, Artifacts (`07/§ 2.1–2.4`); § 6.1 um acht Zeilen, § 6.3 um neun Erledigte bereinigen, § 6.4/§ 6.5/§ 7 berichtigen; die vier falschen Sätze der §§ 3.2/3.4 (`01/§ 7`), die Kern-Aussagen aus `02/§ 8`, die Schemaaussagen aus `03/§ 6.2`, die WinForms-Reste `04/§ 3`, die Etappenkürzel `05/§ 8.2`; Übersetzungstafel `07/§ 2.12`; Mockup-Prüfung P1; dann der Schnitt in drei Papiere (A13) | M, Schnitt L | keine | Dokumentationswache | — | — | Sonnet (Pflege), Fable (Schnitt) | keine |
| **E1 Nachweisfundament** — **umgesetzt #380** | drei fehlende § 6.2-Anker und ein absoluter Kapitalwert je Referenzprojekt als Theorie-Klasse; `SteuerGutschriftRechnerTests`, `EegSatzRechnerTests`, `PvErloesRechner`-EEG-Fälle; Wache über die Blattstruktur von Excel und Word; Wächter `Format`/`ExcelFormat`; Runde‑2-Fall der Kaskade | M | keine | die Tests selbst; Referenzlauf unverändert | — | — | Opus | keine |
| **E2 Kleine Kernkorrekturen** — **umgesetzt #405** (als W‑E2) | CO₂-Kohärenzfall (R5), Kohärenzzeilen in Rubrik und Bericht (R6), Kaskadenrunde 2 (R4), V‑3 PV-Spalte, B‑7, I‑5, S‑3, S‑5-Hinweis, Strommix-Zeile, G9-Referenztext, G7 in Excel, Bandbreite „Spanne" und Referenzzeile, Hi/Ho-Leser, Kommentare (`WirtschaftlichkeitSeiteGaben.cs:727`, `StrompreisZerlegungModel.cs:86`); dazu P3 der Mockup-Prüfung | M | R4 ja (Sonderfall), sonst Ausweis | Anker, Kern-Tests, Berichtsprobe | — | Kleinigkeiten, kein Logbuch | Opus | E1 |
| **E3 Plattform** — **umgesetzt #431** | (1) vier nahtlose Hüllen verschieben; (2) `OpenFileDialog` → `Dienste.Datei`; (3) `KostenSeiteGaben`, `WirtschaftlichkeitSeiteGaben` verschieben; (4) Rechenaufruf aus `BerichtsDatenSammler` in einen Kern-Controller oder nach `EPOS.UI.Daten` (P1); (5) `KostenKomponenteHuelle` mit Fenster-Adapter; (6) PV-, Tarif-, Gesetzeskatalog-, Verlaufs-Hülle; (7) Tarif-Sprünge zu Überlagerungen, `MessageBox` → `Dienste.Dialog`; (8) `IosProjektQuelle.BerichteKostenGaben` belegen, Whitelist erweitern (A19) | L gesamt, S–M je Schritt | keine | alle Tests unverändert grün, Windows-Schale 0 Fehler, Referenzlauf; iOS-Prüflauf nur nach Rückfrage | — | kein Logbuch (keine sichtbare Änderung auf Windows) | Opus | E1; Schritte 1–3 sofort |
| **E4 Erlösrubrik und Steuerzeilen** | U7 (zwei Beträge, zwei Zeilen, Umschlagfassung), 9d (Gründe je Position), dann U6 (Anlagenfeld, Komponentenblöcke, Zwischensummen, Block „projektweit", Näherung ausgewiesen) | M + L | keine (Summen unverändert) | Anker; `ErloesrubrikTests`; Zahlenprobe 293.245,6 + 22.914,0 = 316.159,6 €/a | — | Wirtschaftlichkeit `block-a`, `energiekosten-je-anlage`; U6 wesentlich | Opus | Q15, A12; E1 — **Q15 entschieden 22.09.2026: U6 ja, der Leistungsanteil der vermiedenen Stromkosten bleibt projektweit; Q11 „kein HT/NT" (22.09.2026) ist **nicht** Teil von E4: Die Rückführung der Strommatrix auf eine Zone ohne HT/NT ändert die Bezugskosten und damit den Rechenweg (Messung `Messung_Pflegewege_Tarifstruktur_Strom.md`, Nach #291 Weg 3) — sie gehört mit A/B-Nachweis und gegebenenfalls neuer Referenzbasis zu E7. Beauftragt 22.09.2026 („E4 starten mit Q15 ja")** — **umgesetzt #432** (Merge `1190622c`; Zweig `e4`: `1a4ec24d`, `ee3efacc`, `74aa192d`, `f9fe94a1`, `468ce047`; 10 451 Tests, Referenzlauf 13/13 mit 357 byte-gleichen CSV; Anwenderfragen U6‑Q1 bis Q3 und U7‑Q1/Q2 in Nach #432; U6‑Q1 als Konzept § 6.3 Nr. 32 zu E7) |
| **E5 Ergebnisansicht und V‑A** — **umgesetzt #434** | U2 Umschalter, U10 Hinweistext (A14), U4 Bandbreite, U5 Empfehlungskarten, Kennzahl-Reihenfolge, Strich/Null (Q16), V‑A (Deklarationen, IZF-Warnung, Steigung, „nachrichtlich"), Hinweiszeile „k von n ohne Dauer" über den Kern (N1) | L | keine | bunit, Kern-Tests, Berichtsprobe, Sichtprüfung | — | Wirtschaftlichkeit, neue Anker; wesentlich | Opus | E3 (sonst nur Windows), Q8/Q9/Q13 für neue Rahmen — **umgesetzt #434** (Merge `deba5e57`; Zweig `e5`: neun Commits `e8ebef76` … `7724ffda` samt zwei Zusammenführungen des Arbeitszweigs; dazu U44 „Bericht erzeugen" und die Kennzeichnung Nr. 31; 10 624 Tests, Referenzlauf 13/13 mit 357 byte-gleichen CSV; vier offene Fragen in Nach #434) |
| **E6 Verlauf mit drei Szenarien** — **umgesetzt #436** | dritte Strichart (Aufzählung, Vorgabe byte-gleich), `VerlaufsReihen` Farbe = Variante / Strichart = Szenario, Dreierlauf, Legende und Bildmaß, Hülle nach `EPOS.UI.Daten`, Knopf „Verlauf…" entfällt (mit E5 bewusst stehen geblieben), Spaltengruppen je Szenario im Tabellenbericht, zweites Bild im Wortbericht; aus E5 nach Empfehlung zu Frage (4) in Nach #434: das Spannen-Balkenbild unter „Wie sicher ist das?" | M–L | keine | **ChartProben** (Bild und Gegenprobe), Berichtsprobe, bunit | — | Wirtschaftlichkeit `verlauf` neu; wesentlich | Opus | E5 (Umschalter), E1 (Wache) — **umgesetzt #436** (Merge `57b15a7c`; Zweig `e6`: siebzehn Commits `ef84a3ec` … `fa80786a` samt den Zusammenführungen `67e56293` und `c54188c2`; dazu die Nachträge E5b‑2 und E5b‑3; im Wortbericht ersetzt das Dreierbild das Differenzbild „Erwartet", das Bild je Version bleibt; 10 758 Tests, Referenzlauf 13/13 gegen R11 mit 357 byte-gleichen CSV; zwei offene Fragen E6‑Q1/E6‑Q2 in Nach #436) |
| **E7 Rechenwirksame Lücken** — **Teil a umgesetzt #437, Teil b umgesetzt #439, Teil c1 umgesetzt #440, Teil c2 umgesetzt #446, Teil c3 umgesetzt #452 — E7 abgeschlossen** | K‑1 (Schritt **A**, Dialogfeld Gruppe 1b, Schreibweg), S‑2 (A3), V‑2/V‑1 (A4), Ersatz/Restwert-Kennzeichen (Schritt **E**), Preisbasis-Spalte (Schritt **F**), U‑1 (Schritt **G**), B‑4 Rest, B‑6, Hi/Ho-Leser (R11, entschieden 22.09.2026: Brennwert), Förderende (A20), **Q11 (22.09.2026): Zeitzonentarif HT/NT streichen (Weg 3 aus Nach #291, A/B-Nachweis, gegebenenfalls neue Basis) und die Leistungspreis-Staffel in die Kostenverwaltung neben die Energiepreisstruktur verlegen (Weg 2), danach Tarifstrukturdialog und Menüpunkt abkündigen**; U6‑Q1 aus E4 (entschieden 22.09.2026 nach Empfehlung: vermiedene Bezugsmenge ohne jede Eigenerzeugung, § 9b-Korrektur auf beide Anlagen, KWK-Split unverändert, Anker 316.159,6 €/a; Konzept § 6.3 Nr. 32) | L | **ja**, je Punkt mit A/B | Anker als Vorher/Nachher, Referenzlauf byte-gleich (Simulation unberührt), neue Testklassen aus E1 | 102 (Nr. 30, gebaut #437; bis #438 als 101 geführt), 104 (Q11, gebaut #439), A = 105 (K‑1, gebaut #440); E = 111, F = 112, G = 113 (gebaut #446) | Wirtschaftlichkeit `kwk-abwaermeabfuhr` neu; Kosten `ersatz-restwert` | Opus | A2, A3, A4, A6, A9; E1 — **Teil a umgesetzt #437** (Merge `befec9dc`; Zweig `e7`: neun Commits `57ba5097` … `ff8a17f9` — Nr. 29 CO₂-Grenzwert brennwertbezogen, Nr. 30 Schemaschritt 102 (bis #438 als 101 geführt) und „(bitte wählen)" ohne die Kern-Regel, Nr. 32 vermiedene Menge ohne jede Eigenerzeugung mit beiden Schlüsseln brutto, dazu die Messung nach A2 für K‑1; 10 778 Tests, Referenzlauf 13/13 gegen R11 mit 357 byte-gleichen CSV, die dreizehn Basisprojekte wirtschaftlich unverändert; drei Fragen E7‑Q1 bis E7‑Q3 in Nach #437, alle am 23.09.2026 entschieden (E7‑Q1, E7‑Q3 Lesart b; E7‑Q2 mit Auflage zu σ). **Teil b umgesetzt #439** (Merge `954d4dcc`; Zweig `e7b`: E7b/1 bis E7b/11, `1b3797a3` … `bfbfbbb9`, mit den Zusammenführungen `a9be7581` und `d93488ef` — Q11: Strommatrix ohne Tarifzonen mit einer Jahreszeile je Projekt, Schemaschritt 104 übernimmt die Staffel an den Stromträger, löscht die Zonensätze und verwirft ihre gespeicherten Läufe, die zweistufige Leistungspreis-Staffel in der Kostenverwaltung an der Viertelstundenspitze, der Tarifstrukturdialog nur noch im Rollenmodell, der Einstieg „Strombezug…" entfällt, einen Menüpunkt gab es nicht; Gate 10 996 Tests, Referenzlauf 13/13 gegen R11 byte-gleich, die dreizehn Basisprojekte unverändert; vier Fragen E7b‑Q1 bis E7b‑Q4 in Nach #439, alle am 23.09.2026 entschieden). **Teil c1 umgesetzt #440** (Merge `ea8e2a12`; Zweig `e7c1`: E7c1/1 bis E7c1/9, `e531f0bf` … `ccf9f22f`, mit den Zusammenführungen `91dd77ba` und `c3bf4882` — K‑1 mit Schemaschritt 105: Fall 2 `min(Netto, Nutzwärme × σ)` auf Regel- und Ersatzweg, σ gepflegt oder P_el ÷ P_th, Kürzung zuerst an der Einspeisung, die Überlagerung „Sätze und Herkunft" mit den zwei Feldern statt des Dialogfelds in Gruppe 1b (E7‑Q2 (5)); A20: das Fristende der Inbetriebnahme 31.12.2030 als Katalogdatum (Generation 8) statt `KWKG_REALISIERUNG_JAHRE`; Nr. 30: die Kohärenzzeile „Anlagenart fehlt" nur bei abzuleitendem Kontingent und die Anlagenart der 1030-BHKW in der Testdatenbank; Gate 11 058 Tests, Referenzlauf 13/13 gegen R11 byte-gleich, die dreizehn Basisprojekte unverändert; acht Fragen E7c1‑Q1 bis E7c1‑Q8 in Nach #440, alle am 23.09.2026 entschieden). **Teil c2 umgesetzt #446** (Merge `41764ab0` auf dem Hilfszweig `pm3`; Zweig `e7c2`: E7c2/1 bis E7c2/14, `e924834d` … `94db9f92`, mit den Zusammenführungen `c3eb2cfe` und `62bcd8a2` und den Umnummerierungen 107/108/109 → 108/109/110 → 111/112/113 — Schritt E als Schemaschritt 111 (Ersatz und Restwert je Position), F als 112 (Preisbasis als Kartenzustand), G als 113 (Gase Nm³, Brennstoff 24 kWh); S‑2 (A3) die Mischlage gesperrt; V‑1/V‑2 (A4) EV-Mix ungerundet und § 51a mit der Einspeisevergütung; B‑4 Rest frisch aus dem Lauf; E7c1‑Q2 b Vollbenutzungsstunden aus dem KWK-Strom, E7c1‑Q1 der Rundungsgrund, E7c1‑Q7 der Rest der Überlagerung samt KI-Feldkatalog und Berichtsspalten; Testdatenbank 113; Gate 11 430 Tests, Referenzlauf 13/13 gegen R12 byte-gleich, die dreizehn Basisprojekte unverändert; acht Fragen E7c2‑Q1 bis E7c2‑Q8 in Nach #446, alle am 23.09.2026 entschieden). **Teil c3 umgesetzt #452** (Merge `9c7a0023` auf dem Hilfszweig `pm4`, Nachtrags-Merge `387c2d9f`; Zweig `e7c3`: E7c3/1a bis E7c3/13, `ba9d0b13` … `035b14db`, mit den Zusammenführungen `91315089`, `716b6449` und `c3246cb6`, ohne Schemaschritt — die Vollbenutzungsstunden nach der Definition des Anwenders vom 23.09.2026, Vbh = W_a ÷ P_Nenn in Fall 1 und Fall 2 (Rückbau von E7c2/7, E7c1‑Q2 b präzisiert, E7c2‑Q7 erledigt); E7c2‑Q5 b `VpvCtKwh` ungerundet; die Katalog-Generation 9 als Nachpflege — Brennstoff 24 H_i = H_s = 1,0, die zwei KWKG-Zeilen ohne Leser abgekündigt (E7c1‑Q8); der Kapitalwert 1024 als Datenstand nachgerechnet (Schritt 83 und die Übernahme vom 02.09.2026, der Anker bleibt); B‑6 in den fünf Prioritätsdateien mit strengem Leseweg und Warnzeilen; E7c2‑Q8 b die Energiesteuer-Vorschau je Wahl im Kern (Nachweisfassung 8); die Wahlen der Überlagerung als Anzeigezeilen (U22); Nr. 9h gemessen, der Rest mit ND‑S3; Testdatenbank auf Generation 9; Gate 11 791 Tests, Referenzlauf 13/13 gegen R12 byte-gleich, die dreizehn Basisprojekte unverändert; acht Fragen E7c3‑Q1 bis E7c3‑Q8 in Nach #452, **entschieden 24.09.2026, nach Empfehlung** (E7c3‑Q6 a, Bau offen)). **E7 ist damit abgeschlossen**; nach dem Entscheid folgen der B‑6-Rest (E7c3‑Q5) und die Anzeige der drei Kerneigenschaften (E7c3‑Q6) als eigene kleine Aufträge |
| **E8 V‑C und V‑D** — **Teil a (V‑C) umgesetzt #454, Teil b (V‑D) umgesetzt #455 — E8 abgeschlossen; Teil b ergänzt #477 (E14)** | fünf ValERI-Blöcke hinter dem Umschalter (die Blöcke 1, 3, 4 und 5 mit E5 vorgezogen, offen Block 2 samt Zahlungsstrombild U42); **E6‑Q1** (entschieden 23.09.2026: Verlauf und Spannenbild auch in Block 4, U49); Formelbericht Stufe 0 (Parameterblock), 1 (Mehrjahrestabelle), 2 (NBW/RMZ/IKV über Differenzreihe), 3 (Betriebskostenblock); Anhang-E-Checkliste (U43); Anhang-D-Gegenprobe gegen `KapitalwertRechner.Rechne`; aus E5 nach Empfehlung zu Frage (4) in Nach #434: Nominalsummen, Differenzspalte und Brückenbild der Gliederung, Tafel „Was daraus im Lauf wird", Fußzeile „Drei Szenarien gerechnet…" | L | keine (Werte bleiben gleich) | Blattstruktur-Wache vorher/nachher, Kern-Fall mit Normsollwerten | — | Wirtschaftlichkeit `bericht`, je Stufe ein Logbuch-Satz | Opus; Fable für die Stufenauslegung und die ClosedXML-Fragen | E1, E5; ClosedXML-Fragen aus `05/§ 3.3` geklärt — **Teil a umgesetzt #454** (Merge `485052c6` auf dem Hilfszweig `pm5`; Zweig `e8a`: E8a/1 bis E8a/9, `ddf252bb` … `51bf6d6b`, mit den Zusammenführungen `8a586448`, `ac51fdb5`, `b5de6db9`, `45154b9f` und `bf113cf8`, ohne Schemaschritt und ohne Rechenwirkung — Block 2 „Zahlungsreihen" je Stand und Szenario aus der Kern-Klasse `Zahlungsgliederung` mit dem Zahlungsstrombild (E8a‑Q1 a), Block 4 mit Spannenbild und Verlauf (E6‑Q1), die Gliederung mit Nominalsumme und Differenzspalte (U46), das Brückenbild auf der Seite und im Wortbericht (U41), „Was daraus im Lauf wird" (U47), die Fußzeile (U48); 43 Ressourcenschlüssel, 14 ChartProben-Bilder; Gate auf dem Gesamtstand mit #455 11 942 Tests, Referenzlauf 13/13 gegen R13 byte-gleich, Anker unverändert; vier Fragen E8a‑Q1 bis E8a‑Q4 in Nach #454, alle am 23.09.2026 entschieden). **Teil b umgesetzt #455** (Merge `704356a4` auf dem Hilfszweig `pm5` über dem Merge #454; Zweig `e8b`: E8b/0 bis E8b/8, `4b06bce8` … `198dcb07`, mit den Zusammenführungen `d5da3480` und `51ef2b99`, ohne Schemaschritt und ohne Rechenwirkung — die Blattstruktur-Wache über Excel- und Wortbericht und die Wache über den ClosedXML-Befund; die Formelmappe Stufen 0 bis 3: der Parameterblock aus echten Zellen mit Namen, die Mehrjahrestabellen und die Kennzahlen des Erwartungsfalls in Formeln über NBW/RMZ/IKV und die Differenzreihe, Betriebskosten Menge × Satz, der Δ%-Block als Zellbezug — EPOS trägt die Werte ein, Excel rechnet beim Öffnen neu, Wertfassung = Formelfassung in 13 Prüfgruppen; die Anhang-E-Checkliste (U43) als Abschlussseite beider Berichte und hinter dem Knopf der Ergebnisseite; die Anhang-D-Gegenprobe gegen `KapitalwertRechner.Rechne` (64.479,51 € gegen 64.480 € der Norm); die Fußzeile in der Knopfreihe (E8a‑Q4); 103 Ressourcenschlüssel; Gate auf dem Gesamtstand 11 942 Tests, Referenzlauf 13/13 gegen R13 byte-gleich, Anker unverändert; sechs Fragen E8b‑Q1 bis E8b‑Q6 in Nach #455, am 23.09.2026 nach Empfehlung entschieden — Q2 und Q3 als eigene Aufträge gebaut mit E8c, #460). **E8 ist damit abgeschlossen.** **Teil b ergänzt #477** (E14 nach dem Befund 1 aus E9a; erster Merge `f3f071d2`, End-Merge `b9c660b9`; Zweig `e14`: E14/1 `dd0ec440`, E14/2 `d29f6496`, E14/3 `7935316d`, E14/4 `172971e4`, Zusammenführung `f6727fdd`; ohne Schemaschritt und ohne Rechenwirkung — Stufe 1 und 2 der Formelmappe auch für Günstig und Ungünstig: je Stand und Szenario eine Mehrjahrestabelle aus der Spalte des Szenarios im Parameterblock bis zum längsten Zeitraum mit Schutzformel jenseits von T_s, Kennzahlen, Zinsfuß und Bandbreite als Formeln, Punkt 11 der Checkliste; Wertfassung = Formelfassung in 16 Prüfgruppen, Referenzlauf 13/13 gegen R14 byte-gleich, Anker unverändert; E14‑Q2 a löst E8b‑Q1 a ab, drei Fragen E14‑Q1…Q3 entschieden 24.09.2026, nach Empfehlung, in Nach #477) |
| **E8c Nachbesserung aus E8b** — **umgesetzt #460** | E8b‑Q2: die Bemessungstexte aller Arten aus dem Bemessungskatalog, Herleitung und Formelmappe über denselben Faktor (`BetriebskostenCtrl.Bemessungsfaktor`); E8b‑Q3 (Lesart b): die Gliederungsprobe der Betriebskosten nur mit den Positionen des ersten Jahres, „ab Jahr X" in der Herleitung, Nachweisfassung 9; der Kommentar zu U42 | S | keine (Texte und Probe) | Zellvergleich über 15 Prüfgruppen (nur die Spalte „Bemessung" und die Warnzeilen), Anker unverändert, Referenzlauf 13/13 gegen R13 | — | Wirtschaftlichkeit `bericht`, zwei Logbuch-Sätze | Opus | E8b‑Q2 und E8b‑Q3 entschieden 23.09.2026 — **umgesetzt #460** (Merge `9ab55946` auf dem Hilfszweig `pm7`; Zweig `e8c`: E8c/1 bis E8c/3, `e91617da` … `b7dd5e1b`, mit der Zusammenführung `b18237c0` (`origin` `e513f05e`), ohne Schemaschritt und ohne Rechenwirkung; fünf Ressourcenschlüssel gestrichen, einer neu, zwei neu gefasst, je Sprache 8.709; Gate auf `9ab55946` 12.123 Tests, ChartProben 146 gleich der Messlatte, Referenzlauf 13/13 gegen R13; zwei Fragen E8c‑Q1 und E8c‑Q2 in Nach #460, offen) |
| **E9 V‑E Szenarioabdeckung** — **Teil a (E9a) umgesetzt #461, Teil b (E9b) umgesetzt #462 — E9 abgeschlossen** | Schritte **B–D** (Zeitraum und Mengenfaktor, Trägerpreise, Erlössätze), ±-Knopf an drei neuen Orten, Kern liest die Paare, Hinweistext entfällt; **ohne Degradation** (A5) | L | **ja**, je Pflege (NULL = wie Erwartet) | A/B je Projekt, Referenzlauf byte-gleich, `SzenarioParameterTests` je Größe | B = 116, C = 117, D = 118 (gebaut #461; 114 ist die Kühlung KU2, 115 die Zapfprofil-Stufe Z3, #453) | Wirtschaftlichkeit `szenarien`; wesentlich | Opus | A5 entschieden; E5, E7 — in zwei Wellen: **Teil a umgesetzt #461** (Merge `62613292` auf dem Hilfszweig `pm8`; Zweig `e9`: E9a/1 bis E9a/9, `7f481f6a` … `49fd15fa`, mit den Zusammenführungen `bdd06e6e` (E8c #460, #458 Stufe 2) und `debb3a5c` (Zapfprofil Z3 mit Schritt 115) — die Schemaschritte 116 (Szenariorahmen), 117 (Trägerpreise) und 118 (Erlössätze), reines DDL, 18 nullbare Spalten; der Kern liest die Paare an je einer Stelle (Zeitraum und Einspeisevergütungen in `FuerSzenario`, der Mengenfaktor in `SzenarioMengen`, die Trägerpreise in `TraegerpreisSzenario.Wirksam`, DV-Entgelt und PPA-Preis in `ProjektPhotovoltaikCtrl.FuerSzenario`); Nachweiszeile, Parameterblock und Verlauf je Szenario; 22 neue Fälle in `SzenarioParameterTests` (jetzt 39); 17 Ressourcenschlüssel, je Sprache 8.848; Testdatenbank 118; Gate auf `62613292` 12.308 Tests, ChartProben 146 gleich der Messlatte, Referenzlauf 13/13 gegen R13 byte-gleich, A/B über neun Größen mit Erwartet bitgleich; sieben Fragen E9a‑Q1 bis E9a‑Q7 in Nach #461, **entschieden 24.09.2026, nach Empfehlung**). **Teil b umgesetzt #462** (Merge `75d45630` über `origin` = `f06c8c9e`, der erste Merge `9fbac8c6` über `b7572d42` ist überholt; Zweig `e9b`: E9b/1 bis E9b/8, `8e3e9357` … `018520f8`, mit der Zusammenführung `f56cdab4` (Dialog Design #458 Stufe 3a/3b), Endstand `a605c803`, ohne Schemaschritt, ohne Pflege ohne Rechenwirkung — die Zeilen 8 (Betrachtungszeitraum) und 9 (Mengenänderung) der Szenariotafel, „Vorgaben" leert 18 Felder; der ±-Knopf als verallgemeinerter `CaseEingabeDialog` an Arbeits-, Grund- und Leistungspreis der Trägerkarte, an den Einspeisevergütungen PV (Parameterdialog) und KWK (Dialog „BHKW-Wirtschaftlichkeit"), an DV-Entgelt und PPA-Preis (PV-Vergütungsdialog); der Hinweistext `WIRT_SZEN_HINWEIS` entfällt, an seiner Stelle der Ausweis „n von m Parametern szenariert" (`SzenarioAbdeckung`) auf der Seite, in beiden Berichten und in Punkt 9 der Anhang-E-Checkliste; 38 Ressourcenschlüssel neu, 1 entfallen, 2 geändert, je Sprache 9.020; erstes Gate auf `9fbac8c6` 12.500 Tests, ChartProben 146 gleich der Messlatte, Referenzlauf 13/13 gegen R13 byte-gleich, Anker unverändert, zweites Gate Build 0 Fehler, ChartProben 146/146 gleich der Windows-Messlatte, voller Lauf 12.532 bestanden / 0 Fehler / 1 übersprungen (EPOS.Kern 5.700, EPOS.UI 5.877, KiKern 542, SpeicherEngine 386, SpeicherPlanung 27+1), Dokumentationswachen 26/26 (Log GATE462b); fünf Fragen E9b‑Q1 bis E9b‑Q5 in Nach #462, **entschieden 24.09.2026, nach Empfehlung** (E9b‑Q5 b, Bau offen)). **E9 ist damit abgeschlossen** |
| **E10 Nutzungsdauer S3 und Speicherflotte** — **umgesetzt #463** | Instandsetzung/Wartung je Technik aus den vorhandenen Spalten, Gerätekataloge; Speicherflotte an `Tab_Nutzungsdauer` mit **Neueinfrieren der Basis**; geräteeigene Spalten kennzeichnen (A8) | M + M | **ja** | A/B, neue Referenzbasis mit Begründung in `Referenzlaeufe/LIESMICH.md` | (H optional) | Kosten `nutzungsdauern` | Opus | ND‑S3-Entscheid, A7 — **umgesetzt #463** (Merge `94521f2e` über `origin` = `502fea3c`; Zweig `e10`: E10/1 bis E10/10, `62795858` … `b8cb7287`, mit der Zusammenführung `f9eb2615` (Dialog Design #466) — Schemaschritt 120 sät die Instandsetzungssätze der Standardzeilen (die Mitte der Vorlagenbereiche), der Dialog „Nutzungsdauern (AfA)" zeigt Instandsetzung und Wartung; die Sätze wirken nur über die ausdrückliche Vorbelegung — Übernahme einer Kostenvorlage oder „Sätze vorbelegen…" —, der Rechenweg liest den gepflegten Satz (E10/9: der implizite Rückfall zur Rechenzeit zählte bei 1030 doppelt und widersprach ND‑Q4, er ist zurückgebaut), die Herkunft steht am Satz (Nachweisfassung 10); neue Kesseleinträge in %/a nehmen den Wartungssatz; die Flottenstudie rechnet den Restwert je Einheit linear aus der Nutzungsdauer, ohne eigenes Intervall aus der Batteriezeile (1046 in der Studie +3.432,79 €); die Nutzungsdauer von Kessel und BHKW als Gerätedaten gekennzeichnet (A8), kein Schritt (H); 24 Ressourcenschlüssel neu, 6 geändert, je Sprache 9.044; Testdatenbank 120; A/B mit dem Knopf auf einer Arbeitskopie (1030 −630.612,02 €), Anker unverändert; Referenzlauf 13/13 gegen R13 byte-gleich — **keine neue Basis**, die LIESMICH-Nachträge 116 bis 120 und die Einfrierregel; Gate auf `94521f2e` 12.580 Tests, ChartProben 146/146, Wachen 26/26; sieben Fragen E10‑Q1 bis E10‑Q7 in Nach #463, sechs **entschieden 24.09.2026, nach Empfehlung** (E10‑Q5 erledigt)). **E10 ist damit abgeschlossen** |
| **E11 Zahlenprobe A8/B9** — **entfällt** (Anwenderentscheid 22.09.2026: „BHKW-Plan-Mappen: nicht relevant") | ~~Referenzmappe festlegen (A17), Generation und Zelltafel einfrieren, Eingabespiegel, Neutralschaltung, fünf Teilproben (Annuität, Brennstoff, § 53, KWKG, Kapitalwert), erwartete Abweichungen vorab benennen (Grundlagen § 5)~~ — Nachweis der Wirtschaftlichkeitsgrößen über die Anker aus E1 und die A/B-Nachweise von E7, E9, E10 | — | keine | — | — | — | — | entfällt |
| **E12 Wiki-Runden** — **Wiki-Quellen vorbereitet #470; E12‑Q1 bis E12‑Q4 entschieden 24.09.2026, nach Empfehlung (Termin 26.09.2026, Version 1.2.0.4); Upload nach Freigabe des Anwenders** | Sammel-Upload: die 14+1 Sätze der Mockup-Prüfung, die fünf Lücken, U17/U23/U36, `help_mapping` (Tarifstruktur, BHKW, PV bereits korrekt verdrahtet; die „acht Anker" tatsächlich neun Seitenebene-Zuordnungen, davon fünf bewusst, vier offene Kandidaten — zwei davon gesetzt #474, zwei bleiben mit Begründung Seitenebene), Höfingen neutralisiert (A16), Hilfesystem 13.2 ergänzt; danach je Etappe die Sätze aus `06/§ 3` | S je Runde | — | Tabuwort-Regex, Produktdaten-Wache | — | — | Sonnet | A16, A18 |
| **E13 Nachbesserung aus E7c3 und E9b** — **umgesetzt #474** | E9b‑Q5 b: Punkt 9 der Anhang-E-Checkliste „erfüllt", sobald Günstig und Ungünstig gerechnet sind, „teilweise" bei nur Erwartet oder einem Szenario, „offen" ohne Lauf, der Ausweis bleibt Beleg; E7c3‑Q6 a: `Ladefehler`, `Speicherfehler` und `Vorsorgewarnung` in der Statuszeile der Ergebnisseite und im Dialog BHKW-Wirtschaftlichkeit, je Grund einmal, einen Datenbankfehler meldet die Anwendung selbst; dazu der Halbsatz aus A8 (neue Speichervariante mit der Nutzungsdauer der Tabelle) und zwei Hilfe-Anker der Seite Kosten | S | keine | Anker unverändert, Referenzlauf 13/13 gegen R14 byte-gleich, Maskenwache, bunit je Ort | — | Wirtschaftlichkeit `bericht`, `wirtschaftlichkeit`; Kosten `kosten` — drei Logbuch-Sätze | Opus | E9b‑Q5 und E7c3‑Q6 entschieden 24.09.2026 — **umgesetzt #474** (Merge `c71addf5`; Zweig `e13`: E13/1 `58e27722`, E13/2 `f674e839`, E13/4 `a4a38a72`, E13/5 `fa7dfd37`, E13/6 `ba78f7d2`) |
| **E15 Risikomodul (V‑G7 aus V‑E)** — **umgesetzt #478** | das Risiko nach DIN EN 17463, 6.5 und Anhang F: Zinszuschlag in allen drei Szenarien oder Zahlungsstromabzug R_loss × p_loss je Periode ab Jahr 1 (nicht Jahr 0, nicht der Restwert) für jeden Stand außer der Referenz; Gruppe „Risiko (DIN EN 17463, 6.5)" im Parameterdialog, Ausweis nur bei Pflege (Nachweiszeile, Annahmentafel, Deklaration 6.5, Checkliste Punkt 6, Bestandteil RISIKO, Formelmappe) | M | **ja**, je Pflege (Vorgabe aus) | A/B an 1030, 1019 und 1024 (Zuschlag = Lauf mit i + 1, Abzug = −R_loss × p_loss × Rentenbarwertfaktor, Rückweg exakt), Anker unverändert, Referenzlauf 13/13 gegen R14 byte-gleich, `RisikoModulTests`, Zellvergleich über 19 Mappen | **125** | Wirtschaftlichkeit `wirtschaftlichkeit`, ein Logbuch-Satz | Opus | Anwenderauftrag 24.09.2026 („V‑G7 Risiko: eigener kleiner Auftrag ausführen") — **umgesetzt #478** (erster Merge `d176b378`, End-Merge `dedfc760`; Zweig `e15`: E15/1 `d46280f7`, E15/2 `e111f7ee`, E15/3 `fe795e10`, E15/4 `a40c9b3c`, E15/5 `a8a82d0c`, Zusammenführung `8494f244`, E15/6 `5eaed19c`; Testdatenbank 125, LFS `6c4c32f9`; vier Fragen E15‑Q1…Q4 entschieden 24.09.2026, nach Empfehlung — Lesart c von E15‑Q4 als spätere Erweiterung nicht beauftragt) |
| **E17 Nicht monetarisierbare Wirkungen (V‑G11)** — **umgesetzt #479** | die nicht monetarisierbaren Wirkungen nach DIN EN 17463, 6.1 und 8.2 als Liste je Projekt: Kategorie, Beschreibung, Dauer, Wirkung auf Organisation, Mitarbeiter und Umwelt, Beurteilung Dauer × stärkste Wirkung (0 bis 9) als Anzeige; Baustein `WirkungenListe` im Bewertungsblock statt des Freitexts (Altfeld lesbar), Tabelle in Wort- und Tabellenbericht, Punkte 2b und 3b der Anhang-E-Checkliste | S | nein | Anker „keine Rechenwirkung" bitgleich, Referenzlauf 13/13 gegen R14 byte-gleich, `NichtMonetaereWirkungenTests`, `WirkungenListeTests`, Blattstruktur-Wache zwei Fälle | **127** | Wirtschaftlichkeit `wirtschaftlichkeit` (Anker `nicht-monetaer`, `checkliste`), ein Logbuch-Satz | Opus | Anwenderauftrag 24.09.2026 („V‑G11 … kleiner Dialog-und-Bericht-Auftrag ohne Rechenwirkung") — **umgesetzt #479** (erster Merge `0462f92e`, End-Merge `52614c33`; Zweig `e17`: E17/1 `1ca8273b`, E17/2 `3f5f0412`, E17/3 `c902018c`, E17/3a `2512865e`, E17/4 `c5094dd4`, E17/5 `0769c84d`, Zusammenführung `311780cd`, E17/6 `4ede85a5`, E17/7 `db909d37`, E17/8 `079de7d7`, Nachzug `86ebd491` auf #485, E17/9 `f545f86b`; Testdatenbank 127, LFS `87e49ed1`; vier Fragen E17‑Q1…Q4 entschieden 24.09.2026, nach Empfehlung) |
| **E16 Wiederholperiode je Kostenposition (V‑G3 aus V‑E)** — **umgesetzt #484** | die n-jährlichen Zeitpunkte nach DIN EN 17463, 6.3.1: je Betriebsposition eine Wiederholperiode, Zahlung in s, s + n, … ≤ T (`KapitalwertRechner.ZahltImJahr`), nur Betriebspositionen; Ganzzahlfeld „Zahlung alle: [n] Jahre" im Zeileneditor und in den Kostenvorlagen, Vorlagenübernahme, KI-Feld `wiederholperiode`; „alle n Jahre ab Jahr X" in der Betriebskostentabelle, Hilfsspalte je Topf in der Formelmappe, Nachweisumschlag Fassung 11 | S | **ja**, je Pflege (leer = jährlich) | A/B an 1030 (Zeile 101600098 auf n = 2, drei Szenarien gleich der Handrechnung), Anker unverändert, Referenzlauf 13/13 gegen R14 byte-gleich, `WiederholperiodeTests`, fünf bUnit-Fälle | **129** | Kosten `kosten` (neuer Anker `zahlung-alle-n-jahre`), Wirtschaftlichkeit `bericht-betriebskosten`, `formelmappe`; ein Logbuch-Satz | Opus | Anwenderauftrag 24.09.2026 („V‑G3 n‑jährliche Zeitpunkte …") — **umgesetzt #484** (Merge `ae7b0ed0`; Zweig `e16`: E16/1 `68a4915a`, E16/2 `7572c0cf`, E16/3 `b8ff3931`, E16/4 `c5ac83f7`, E16/5 `43776f61`, Zusammenführung `cb06a7c3`, E16/6 `de49dec9`, Zusammenführung `6ddb0132` mit AK1 W3, E16/7 `37538993`, E16/8 `7c8f4fc4`; Testdatenbank 129, LFS `4c546a7c`; vier Fragen E16‑Q1…Q4 offen) |
| **E18 Restpunkte Stromsteuer (Konzept § 6.3 Nr. 14, 16, 18)** — **umgesetzt #492** | Wache der Stromsteuer-Rückfallebene gegen die älteste Katalogzeile in Saat und Testdatenbank (Nr. 14, § 6.5), zwei tote Ressourcen gestrichen; der erfasste Stromsteueranteil unter der Unternehmensart im Dialog „BHKW-Wirtschaftlichkeit" mit Satzabgleich und Kohärenzzeile, nur Anzeige (Nr. 16); Nr. 18 nachgemessen, offen, HB1-O1 an den fünf Rechenweg-Sortierungen vermerkt | S | nein | Anker unberührt, Referenzlauf 13/13 gegen R14 byte-gleich, Gegenprobe der Wache (21 EUR/MWh → rot), `StromsteuerErfasstTests`, acht bUnit-Fälle | — (kein Schemaschritt) | Wirtschaftlichkeit `wirtschaftlichkeit` (Dialog „BHKW-Wirtschaftlichkeit"), ein Logbuch-Satz | Opus | Anwender 24.09.2026 („fahre fort" auf die Empfehlung der kleinen Welle) — **umgesetzt #492** (Merge `e79bffb1`; Zweig `e18`: E18/1 `52e223e2`, E18/2 `52b94866`, E18/3 `50ef787d`, E18/4 `5c05ea6b`, E18/5 `29fad34d`; E18‑Q1…Q6 entschieden 24.09.2026, nach Empfehlung a; Q7 Restpunkt § 6.3 Nr. 33) |
| **E19 Restpunkte Unternehmensart (Konzept § 6.3 Nr. 15, 33)** — **umgesetzt #498** | Nr. 15 als durch die Schalentrennung überholt geschlossen, Wache `KatalogjahrJeOeffnungTests`; die Unternehmensart ohne BHKW im Parameterdialog, Gruppe Strom, mit der Anzeige des erfassten Stromsteueranteils und der § 9b-Erklärzeile, mit BHKW nur der Verweis (Nr. 33); KI-Feld `unternehmensart` mit Sperre, Maskenwache 35 | S | nein (§ 9b ohne BHKW erreichbar, gerechnet nur mit gewählter Unternehmensart) | Anker unberührt, Referenzlauf 13/13 gegen R14 byte-gleich, `KatalogjahrJeOeffnungTests`, `UnternehmensartOhneBhkwTests` (§ 9b an 1041), sechs bUnit-Fälle | — (kein Schemaschritt) | Wirtschaftlichkeit `parameter`, `bhkw-wirtschaftlichkeit`, ein Logbuch-Satz | Opus | Anwender 25.09.2026 („fahre fort“ nach dem Statusbericht) — **umgesetzt #498** (Merge `31a0b085`; Zweig `e19`: E19/1 `a8832acd`, E19/2 `c87354a0`, E19/3 `14d55922`, E19/4 `bfa2c9e5`; E19‑Q1…Q6 entschieden 25.09.2026, nach Empfehlung, Q4 b) |
| **E21 Pflegewelle (Konzept § 6.3 Nr. 23, 24)** — **umgesetzt #506** | resx-Sammelnachtrag erledigt: zwei verwaiste Schlüssel aus de/en/Designer gestrichen, drei Rückfalltexte in `EnergietraegerHuelle.cs` an die resx angeglichen, Kommentare `KiDialoge.cs` und `SteuerGutschriftRechner.cs` nachgezogen (Nr. 23); Nr. 24 und die Datenlücken der Projekte 1018, 1024, 1023, 1030, 1026 gemessen und benannt, keine Datenpflege | S | nein | Anker unberührt, Referenzlauf 13/13 gegen R14 byte-gleich, Testdatenbank unverändert (`a427aa72`) | — (kein Schemaschritt) | kein Wiki | Opus | Anwender 25.09.2026 („sonst nach Empfehlung“) — **umgesetzt #506** (Merge `e7c2f8f7`; Zweig `e21`: E21/1 `25984cbd`, E21/2 `02ea7650`; E21‑Q1…Q9 entschieden 25.09.2026, nach Empfehlung, alle a) |
| **E20 Wärmepumpe je kW elektrisch (Konzept § 6.3 Nr. 10)** — **umgesetzt #502** | die Investitionskosten der Wärmepumpe auch „je kW elektrisch“: P_el = Ptherm ÷ COP am Normpunkt der Kennlinie `Tab_Kenndaten` (A2/W35, B0/W35, W10/W35, bei W35 interpoliert, nie extrapoliert), ein gerechneter Zweig der Landkarte mit dem Schalter `investition` — nur Kategorie 1, die Betriebsseite unverändert; Herleitung „11,60 kW ÷ COP 2,90 (A2/W35) = 4,00 kW“ mit zwei neuen Schlüsseln | S | nein im Bestand (keine Zeile an der WP trägt die Art); **ja**, je Pflege | Anker unberührt, Referenzlauf 13/13 gegen R14 byte-gleich, A/B an 1024 (1.000 €/kW → 4.000 €, Investitionssumme 12.001,00 → 16.001,00 €), `WaermepumpeElektrischeLeistungTests` (13 Fälle), Kreuztafel je Raster | — (kein Schemaschritt) | Kosten `bemessung`, ein Logbuch-Satz | Opus | Anwender 25.09.2026 („‚Wärmepumpe beides‘ nur bei Investitionskosten nach kW elektrisch und kW thermisch“) — **umgesetzt #502** (Merge `49ea20e0`; Zweig `e20`: E20/1 `13d8a9a4`, E20/2 `e10300d7`, E20/3 `f50ac248`, E20/4 `a414b768`; E20‑Q1…Q5, Q7, Q8 entschieden 25.09.2026, nach Empfehlung a; Q6 offen beim Anwender) |
| **E22 Rechenweg-Sortierung nach der Regel „99“ (Konzept § 6.3 Nr. 18)** — **umgesetzt #503** | die fünf Rechenweg-Leser (`WP_Liste_Laden`, `QuellbezuegeAufbauen`, `SenkenPufferDerAnlagen`, `SenkenLaden`, `SenkenlistenLaden`) und die drei Modul-Lader (`SPK_Liste_Laden`, `Solar_Liste_Laden`, `BHKW_Liste_Laden`) nach `Ladeordnung.SqlAnlagenprio` wie Hydraulikbild und Erzeugerkarten — gepflegte Priorität zuerst, ungepflegt hinten; die Vermerke HB1-O1 entfernt | S | nein (alle Werte und Zeitreihen gleich; allein der Modulindex der Wärmepumpen in 1042) | Anker unberührt; A/B gegen R14 und R15 (nur 1042 `aggregate.csv`, 10 Werte, Index), Determinismus 14/14 byte-gleich; **neue Basis `2026-09-25_R16_Anlagenprio`** (vierzehn Projekte), Referenzlauf 14/14 gegen R16 byte-gleich; `AnlagenprioRechenwegTests` (4 Fälle) | — (kein Schemaschritt) | kein Wiki | Opus | Anwender 25.09.2026 („Nr. 18: so umsetzen“, nach der Messwelle `mess18`) — **umgesetzt #503** (Merge `76f8661d`; Zweig `e22`: E22/1 `0e1c0938`, E22/2 `53394b73`, E22/4 `0dd97e05` (Basis R15_Anlagenprio, verworfen), E22/5 `548d5983`, Zusammenführung `8cb5c69b` mit AK1 Welle 5, E22/6 `c6ee0961`; E22‑Q1 entschieden 25.09.2026, nach Empfehlung a) |
| **E23 Betriebskosten der Wärmepumpe ohne kWh (Konzept § 6.3 Nr. 10, E20‑Q6)** — **umgesetzt #510** | „je kWh elektrisch“ und „je kWh thermisch“ im Betriebsraster der Wärmepumpe gesperrt (Landkarte `BasisGrund` → GEWERK; Auswahl, KI-Wahlliste und Kreuztafel folgen); Bestandszeilen über `benutzt` wählbar und rechenfähig, Herleitungsvermerk „Altbestand — Betriebskosten der Wärmepumpe werden nicht je kWh bemessen“ (`KostenHerleitung.IstAltbestandWpKwh`, ein neuer Schlüssel); wählbar bleiben fester Jahresbetrag, Prozentbemessungen und je kW | S | nein (keine Zeile an der Wärmepumpe trägt eine kWh-Art) | Anker unberührt, Referenzlauf 14/14 gegen R16 byte-gleich; `WaermepumpeBetriebKwhSperreTests` (8 Fälle), `BemessungsauswahlJeGewerkTests`, `BetriebskostenBemessungsmatrixTests` | — (kein Schemaschritt) | Kosten `laufgroessen`, ein Logbuch-Satz | Opus | Anwender 25.09.2026 (E20‑Q6 b, erweitert: „nicht nach kWh/a — weder Strom noch Wärme“) — **umgesetzt #510** (Merge `f7823b8e`; Zweig `e23`: E23/1 `d1fa1d8a`, E23/2 `b41b5232`, E23/3 `d08446a0`; E23‑Q1…Q5 entschieden 25.09.2026, nach Empfehlung a; Q6 und Q7 offen beim Anwender) |
| **E24 Datenpflege 1018/1023 (Konzept § 6.3 Nr. 24)** — **umgesetzt #514** | Testdatenbank: Kessel 10369 (1018) und 11205 (1023) mit Träger 63 „Erdgas E“, für 1023 die Erdgas-Projektzeile `energy_project_settings` 10130 (0,84 €/Nm³, 1.200 €/a, Hi 10,5, CO₂ 240) und der Preisstand `energy_price` 10185; einmaliges dotnet-Dateiskript, kein Schemaschritt; Neueinfrierung `2026-09-25_R17_Datenpflege`, R16 ins Archiv; `DatenpflegeKesseltraegerTests` (4 Fälle), `PreisbasisSchrittTests` 17 → 18 | S | nur Wirtschaftlichkeit 1023 (frische Rechnung: Energiekosten Erdgas und Kapitalwert erstmals); Simulation und Emissionen unverändert, allein `HeizkesselModul[0].carrier_id` in 1018 und 1023 | Zellvergleich 10.645.701 Zellen: 2 Zellen, 2 neue Zeilen, 2 Zähler; A/B gegen R16 12/14 PASS, 430/432 CSV byte-gleich; Determinismus 432/432; Referenzlauf 14/14 gegen R17 | — (kein Schemaschritt; Testdatenbank `76dd9e48` → `0c2fe21a`, Schemastand 143) | kein Wiki, kein Logbuch | Opus | Anwender 25.09.2026 („nehme die Empfehlungen vor: für Später“) — **umgesetzt #514** (Merge `edf89ae8`; Zweig `e24`: E24/1 `3e20336e`, E24/2 `717de7d9`, E24/3 `63667c9b`, E24/4 `7da6bb81`; E24‑Q1…Q6 entschieden 25.09.2026, nach Empfehlung) |
| **E26 PV-Ausweis und Strommatrix-Bedarf (Befunde N1, N3 aus E25; Konzept § 3.6, § 6.3 Nr. 34)** — **umgesetzt #518** | N1: `Ergebnis.Photovoltaik.Stromproduktion` = Erzeugung der Module (`Stromproduktion_Theoretisch`, `SimulationRunner.cs:989-998`), Eigenverbrauch des Ausweises = Erzeugung − Einspeisung; N3: Reihe `STROMBEDARF_GESAMT` (Rest nach der Kaskade + BHKW-Strom) als Bedarf der Strommatrix, auch für den KWK-Split; `PvAusweisStromMatrixTests` (11 Fälle); Neueinfrierung `2026-09-25_R18_PvAusweis`, R17 ins Archiv | S (rund 5 h und Gate) | Ausweis: „PV: vermiedener Bezug“ ≥ 0, im Rollentarif vermiedene Menge und Kosten positiv (1040 −4.496 → +1.332 €/a, 1026 −5.401 → +1.604 €/a); Kapitalwert unverändert; in der Simulation allein `Photovoltaik.Stromproduktion` in 1007, 1040, 1045, 1046 | Anker bitgleich (1024, 1030, KWKG Jahr 1); A/B gegen R17 10/14 PASS, 428/432 CSV byte-gleich; Determinismus 14/14; Referenzlauf 14/14 gegen R18; Tests 14.632/2/0 | — (kein Schemaschritt; Testdatenbank unverändert `0c2fe21a`) | kein Wiki-Fachtext; Logbuch-Vorschlag in der Statuszeile | Opus | Anwender 25.09.2026 („Befunde aus E25: Empfehlung/bearbeiten“) — **umgesetzt #518** (Merge `025a8707`; Zweig `e26`: E26/1 `bc1d8ad8`, E26/2 `4fd6eaa6`, E26/3 `3136a267`, E26/4 `4653fa06`; E26‑Q1…Q7 entschieden 25.09.2026, nach Empfehlung; Restpunkte Q6, N5, N6) |
| **E25 Prüfprojekt 1048 „PV mit Preisen“ (Befund aus E9a, E21‑Q9; Konzept § 6.3 Nr. 35)** — **umgesetzt #519** | Testdatenbank: Prüfprojekt 1048 ohne Referenzrolle, Kopie von 1040 über den Kopierweg des Programms (VDI 6007, 40 Module 10,40 kWp, Strom 0,30 €/kWh + 120 €/a, Erdgas 0,80 €/Nm³ + 150 €/a, je mit Szenariopreisen, Parametersatz 3 % / 20 a / 2 % / 1,5 %, Einspeisevergütung 0,08 (0,10 / 0,06) €/kWh, 1.200 €/kWp, Wartung 150 €/a, Instandhaltung 1 %); wiederholbares Skript `Referenzlaeufe/Skripte/pruefprojekt_1048_pv_preise.cs`; `PvPreisProjektTests` (13 Fälle); Zählungen in drei Testklassen, drei Wachen an 1048 angepasst | S | keine (nur Testdaten; an 1048 Kapitalwert Erwartet −237.134,727 €, Einspeiseerlös 564,80 €/a) | Zellvergleich gegen `19a7b632`: 44.537 neue Zeilen, 0 bestehende geändert; Referenzlauf 14/14 gegen R18 byte-gleich; voller Lauf 0 Fehler (Kern 7.450, UI 6.395); Auslieferungsvorlage 36/36; SqlDialektPruefer 1.927/0 | — (kein Schemaschritt; Testdatenbank `19a7b632` → `b68638da`, Schemastand 144) | kein Wiki, kein Logbuch | Opus | Anwender 25.09.2026 („nehme die Empfehlungen vor: für Später“, E21‑Q9 a) — **umgesetzt #519** (Merge `3936003c`; Zweig `e25`: E25/1+2 `51eee6a6`, Merge `a499feb7`, E25/1b `65458a10`, E25/3 `a313d776` und `e0f6d847`; E25‑Q1…Q10 entschieden 25.09.2026, nach Empfehlung, alle a) |
| **E27 Netzbezug nie negativ (Befund N5 aus E26; Konzept § 3.6, § 6.3 Nr. 34, 36)** — **umgesetzt #521** | Klemme des Reststroms am Laufende vor `ReststromMwh` (`SimulationControl.NetzbezugGeklemmt`, nur Werte < 0 → 0, nicht bei der Speicherflotte); `BHKW.Reststrombedarf` je Stunde geklemmt (`BhkwReststrombedarfMwh`, `SimulationRunner.cs:607-609`, `SimulationErgebnisCtrl.cs:743-745`); ein BHKW-Überschuss allein als Einspeisung im KWK-Split; `BhkwNetzbezugKlemmeTests` (7 Fälle), Kapitalwert-Anker 1030 neu; Neueinfrierung `2026-09-25_R19_BhkwNetzbezug`, R18 ins Archiv | S (rund 5 h und Gate) | 1018 Netzbezug −27,46 → 0 MWh, Reststromkosten im Rollentarif −8.237,25 → 0 €/a, CO₂ +11,95 t/a; 1030 Netzbezug 4.357,78 → 4.358,17 MWh, Kapitalwert Erwartet −31.141.242,71 → −31.142.971,06 €; übrige zwölf Projekte unverändert | A/B gegen R18 12/14 PASS, 428/432 CSV byte-gleich; Determinismus 14/14; Tests 14.803/2/0; SqlDialektPruefer 1.927/0; ChartProben 174/0; Referenzlauf gegen R19 nach dem Merge: Statusdatei Nach #521 (i) | — (kein Schemaschritt; Testdatenbank von E27 unverändert) | kein Wiki-Fachtext; Logbuch-Vorschlag in der Statuszeile | Opus | Anwender 25.09.2026 („E27: nach Empfehlung bauen“, E27‑Q1 a, Q2 a) — **umgesetzt #521** (Merge `80a7b9fb`; Zweig `e27`: E27/1 `bba74bca` und `8aa53a99`, E27/3 `13ae6216`, E27/4 `a96500eb`; E27‑Q3…Q8 entschieden 25.09.2026, nach Empfehlung; Restpunkte Q3 b, N7, Q6) |
| **E28 Prüfwelle N7: Strom-Stufeneingang geklemmt (Befund N7 aus E27, E27‑Q7; Nebenbefund N8; Konzept § 3.6, § 6.3 Nr. 36)** — **umgesetzt #535** | PV-Modus der Wärmepumpe nur auf PV-Überschuss (`SimulationControl.PvUeberschussVorab`, negativer Bedarf je Stunde als 0, Aufruf in `PV_Ueberschuss_Vorabberechnen`); Strom-Stufeneingang der Kesselzeile je Stunde über `NetzbezugGeklemmt` an allen drei Wegen (Nachzug hinter der WP, Mitglied der Speicherstufe, Vektorstufe `Simulation_SPK_Ctrl_Zweikanalig`) und der PV-Zeile (`SimulationRunner.cs:1031-1033`, `SimulationErgebnisCtrl.cs:866-867`); `StromStufeneingangKlemmeTests` (13 Fälle); keine Neueinfrierung | S (rund 1 h und 25 min Läufe, Gate) | keine — beide Stellen latent (keine Wärmepumpe im PV-Modus, kein Projekt mit BHKW und Photovoltaik), Kapitalwert 0 €, kein Anker wandert; fiktiv (1018 + PV von 1040 + WP im PV-Modus) wären 27,46 MWh in 3.501 h als PV-Überschuss gezählt worden | Referenzlauf 14/14 gegen R20, 432/432 CSV byte-gleich, 1048 32/32; Tests 15.593/2/0; Kern gefiltert 161/161; Gate und CI: Statusdatei Nach #535 (g) | — (kein Schemaschritt; Testdatenbank unverändert) | kein Wiki-Fachtext; kein Logbuchsatz (keine Referenzrechnung ändert sich) | Opus | Anwender 26.09.2026 („Prüfwelle ausführen“) — **umgesetzt #535** (Merge `6695caec`; Zweig `e28`: E28/1 `ed8a307e`, E28/2 `3337810b`, E28/3 `eac2378f`; E28‑Q1…Q5 entschieden 26.09.2026, nach Empfehlung; Restpunkte Strombedarfsdeckung der PV-Zeile (E29), Vorrichtung für Stelle 1) |
| **E29 Anzeige-Welle Stromausweis (E27‑Q3 b, E26‑Q6, N6; Restpunkt aus E28‑Q3; Befund N9; Konzept § 3.6, § 6.3 Nr. 34, 36)** — **umgesetzt #536** | BHKW-Einspeisung = KWK-Split (`SimulationControl.BhkwEinspeisungStuendlich`), Diagnosereihe `BHKW_UEBERSCHUSS` auch ohne PV/Flotte, Zeile „Stromeinspeisung“ im BHKW-Reiter mit Formel-Tooltip (de/en), Excel-Spalte „BHKW-Einspeisung“ ohne Flotte; Strombilanz-Linie und Excel „Strombedarf“ `STROMBEDARF_GESAMT ?? STROMBEDARF`; Übersicht mit Kältestrom der Stufenrechnung (N6); PV-Deckungsgrad am je Stunde geklemmten Bedarf; Stromgang „Heizkessel“ = Kesselstrom (N9); `BhkwEinspeisungAusweisTests` (21 Fälle), bUnit; Messlatte `Bericht_Excel_1030` begründet neu | S (rund 1 h und 45 min Läufe, Gate) | keine im Kapitalwert — Ausweis: 1018 Einspeisung 27,46 MWh/a, 1030 0,39; 1040 Strombilanz/Excel „Strombedarf“ 8,0 → 27,4 MWh/a; Stromgang „Heizkessel“ 1017 635,2 → 20,12, 1024 409,31 → 47,67, 1030 4.790,09 → 0, 1047 640,19 → 1,0 MWh | Referenzlauf 14/14 gegen R20, 432/432 CSV byte-gleich, 1048 32/32; ChartProben 161 Hashes = Messlatte; voller Lauf 0 rot (Kern 8.121, UI 6.533); Gate und CI: Statusdatei Nach #536 (g) | — (kein Schemaschritt; Testdatenbank unverändert) | kein Wiki-Fachtext; Logbuchsatz im Wiki-Upload-Papier (1.2.0.4) | Opus | Anwender 26.09.2026 (E27‑Q3 b; E26‑Q6/N6 „in einer kleinen Welle nachziehen“) — **umgesetzt #536** (Merge `32d84023`; Zweig `e29`: E29/1 `71d74522`, E29/2 `656718a1`, E29/3 `39e540c3`, E29/4 `30572adb`, E29/5 `83418db8`, E29/6 `80b29c7f` und `ed838081`; E29‑Q1…Q12 entschieden 26.09.2026, nach Empfehlung; Restpunkte N10 (→ E30 #548), N11 offen) |
| **E30 Hilfsenergiekosten, BHKW-Stromdeckung, Datenpflege 1030/1026, Kapitalwert-Anker 1030 (Befunde B3, B4, B5, B8 der Sichtprüfung P1030; N10 aus E29; Konzept § 3.4, § 3.6, § 6.2, § 6.3 Nr. 21, 36)** — **umgesetzt #548** | Datenpflege 1030/1026 mit dem wiederholbaren Skript `Referenzlaeufe/Skripte/datenpflege_1030_1026_betriebskosten.cs` (9 Zeilen: Wartung als `JAHRESBETRAG` 18.000/2.000 in den Pflichtzeilen, Altzeilen gelöscht, fünf Hilfsenergiezeilen auf `PROZENT_ENDENERGIEBEDARF`); Hilfsenergiekosten aus dem Anlagenanteil (`HilfsenergieAusAnteil`: Pflichtzeile ohne Satz nach Weg B mit dem Anteil als Satz, sonst abgeleitete Zeile, gepflegte Position hat Vorrang, Elektrokessel/Wärmepumpe/Kälte ausgenommen); Stromdeckung des BHKW = Eigenverbrauch ÷ Strombedarf aller Verbraucher an allen fünf Stellen (`SimulationErgebnisCtrl.BhkwStromdeckungProzent`); Kernanker 1030 als „gespeicherter Altlauf 212“ mit `KapitalwertAnkerZerlegungTests`; Messlatten Word/Excel 1030 begründet neu; Neueinfrierung `2026-09-26_R21_BhkwDeckung`, R20 ins Archiv | S (rund 5 h, davon 1,5 h Läufe, und Gate) | Kapitalwert an allen Ankern bitgleich (kein Referenzprojekt trägt einen Anteil, Datenpflege ergebnisneutral; 1030 Betriebskosten 20.000 €/a); `BHKW.Strombedarfsdeckung` 1017 5,48 → 5,31, 1024 26,22 → 20,94, 1047 5,34 → 5,30 %; fiktiv 2 % an beiden BHKW von 1030 ≈ +6.208 €/a; Ankerdifferenz 1030 zerlegt: −9.247.593,78 €, richtig −31.142.971,06 € | A/B gegen R20 11/14 PASS, 429/432 CSV byte-gleich; Referenzlauf 14/14 gegen R21, 432/432 byte-gleich, 4.610.207 Werte; ChartProben 183 = Messlatte; voller Lauf 0 rot (Kern 8.267, UI 6.564); Gate und CI: Statusdatei Nach #548 (h) | — (kein Schemaschritt; Testdatenbank `22e67400` → `40df1bf2`, Schemastand 148) | Wiki-Quelle „Kosten“ (Hilfsenergiekosten aus dem Anlagenanteil); Logbuchsatz im Wiki-Upload-Papier (1.2.0.4) | Opus | Anwender 26.09.2026 (~09:50: B3 „bereinigen“, B5 „Prozent des Endenergiebedarfs“, B4 „Hilfsenergiekosten daraus ermitteln“, B8 und N10 „nach Empfehlung“) — **umgesetzt #548** (Merge `7d1b6d28`, Folgecommit `593e9048`; Zweig `e30`: E30/1 `4dc4a662`, E30/2 `b9e35a3e`, E30/4 `44e52518`, E30/5 `2299b8da` und `9597762f`, E30/3 `fb936e4c`, E30/6 `fb2dcc3f`; E30‑Q1…Q12 entschieden 26.09.2026, nach Empfehlung; Restpunkte Satzfeld im Kostenraster, Katalogempfehlung Kessel, Emission/Steuer des Hilfsstroms, N11) |

**Stand der Etappen am 24.09.2026.**

**E0 — umgesetzt #379.** Gebaut ist die Papierpflege ohne Entscheid: Konzept (2 427 → 2 632 Zeilen),
Szenarienkonzept, Nutzungsdauer-Konzept, Rechenwege 04/05/08 und der Wegweiser des Ordners (E0a); das
Hauptmockup an rund 90 Stellen, vier weitere Mockups und der Index (E0b). Der Schnitt in drei Papiere
(A13) gehörte nicht zu E0; er ist als eigene Aufgabe mit **#435** ausgeführt. E0c hat die Papiere am
22.09.2026 auf den Stand nach #428 nachgezogen.

**E1 — umgesetzt #380.** `WirtschaftlichkeitAnkerTests` (9), `SteuerGutschriftRechnerTests` (39),
`EegSatzRechnerTests` (49), `PvErloesRechnerEegTests` (23), `BerichtBlattstrukturWacheTests` (5),
`WirtZeileFormatWacheTests` (4) und die reihenfolgeunabhängige Kaskadenrunde 2 (R4). **Zwei Befunde
gegen das Konzept:** Kaskade 1042 ±0,00 € statt +20.927,61 €, Kapitalwert 1024 −2.896.359,13 € statt
−2.220.322,32 € (Differenz −676.036,81 €, Ursache mit E7 nachzurechnen). **Ein Befund verkleinert
E3 Schritt 4:** `BerichtsDatenSammler` ist bereits WinForms-frei; nur seine **Lage** muss wandern,
einzige Naht ist `EnergieMengen.BaueBrennstoffmengen`.

**E2 — umgesetzt #405** (in der Statusdatei als **W‑E2** geführt), **ohne Rechenwirkung**. Erledigt:
R5, R6, R4 (schon mit E1), V‑3, B‑7, I‑5, S‑3, S‑5-Hinweis, Strommix-Zeile, G7, G8 („Spanne" und
Referenzzeile), G9-Referenztext, Formel `N4` sowie P3 der Mockup-Prüfung. **Was „Nach #405" offen
lässt:** Hi/Ho am CO₂-Grenzwert (R11, bewusst offen, Entscheid für E7), die trägerscharfe Aufteilung
der CO₂-Warnung (so gelassen), 22 gleichlautende Knopfschlüssel außerhalb dieses Feldes und
`WIRT_ENK_ANLAGE` ohne Leser (mit der Fußleisten-Welle, Q8). Der **Hi/Ho-Leser** stand in der Zeile
oben als Inhalt von E2, ist aber **nicht** gebaut worden — er gehört zu E7 und ist mit **E7 Teil a (#437)**
gebaut. Von B8 bleiben damit
**S‑2** (≡ A3) und **B‑6**.

**E3 — umgesetzt #431.** Stand je Schritt, nachgemessen am Codestand `2cfee66b` (Merge; Zweig `e3`,
sieben Commits):

| Schritt | Stand |
|---|---|
| (1) vier nahtlose Hüllen verschieben | **umgesetzt #431** — `WirtschaftlichkeitParameterHuelle` → `EPOS.UI.Daten/Wirtschaftlichkeit/`; `KostenfaktorKatalogHuelle`, `VorlagenUebernahmeHuelle`, `ErtragBonusGaben` → `EPOS.UI.Daten/Kosten/` |
| (2) `OpenFileDialog` → `Dienste.Datei` | **umgesetzt #431** — `PhotovoltaikVerguetungHuelle.MarktwerteImportieren` über `Dienste.Datei.DateiOeffnenAsync` |
| (3) `KostenSeiteGaben`, `WirtschaftlichkeitSeiteGaben` verschieben | **umgesetzt #431**, nach `EPOS.UI.Daten/Kosten/` bzw. `.../Wirtschaftlichkeit/` — **Abweichung:** lief nach Schritt 4, weil `WirtschaftlichkeitSeiteGaben` die geschachtelten Typen des Sammlers benutzt und erst übersetzt, wenn er plattformfrei ist |
| (4) Rechenaufruf aus `BerichtsDatenSammler` | **umgesetzt #431** — nach `EPOS.Kern/Allgemein/Bericht/` (statt `Controller/`); reiner Umzug (Befund #380), einzige Naht `EnergieMengen.BaueBrennstoffmengen`; **Abweichung:** lief vor Schritt 3 |
| (5) `KostenKomponenteHuelle` mit Fenster-Adapter | **umgesetzt #431**, mit Adapter `KostenKomponenteFenster` — **Abweichung:** in einem Commit mit Schritt 6 (Schritt 5 brauchte den Haken, den Schritt 6 löscht; getrennt gäbe es einen nicht bauenden Zwischenstand) |
| (6) PV-, Tarif-, Gesetzeskatalog-, Verlaufs-Hülle | **umgesetzt #431** — `GesetzeskatalogHuelle` mit Adapter `GesetzeskatalogFenster`; **Abweichung:** `TarifstrukturHuelle`, `PhotovoltaikVerguetungHuelle`, `BhkwWirtschaftlichkeitHuelle` und `KapitalwertVerlaufHuelle` bekamen **keinen** Fenster-Adapter — ihre Fensterhälften hatten keinen Aufrufer mehr und sind ersatzlos gefallen; damit ist auch die `MessageBox` aus P4 weg |
| (7) Tarif-Sprünge zu Überlagerungen, `MessageBox` → `Dienste.Dialog` | **umgesetzt #431** — BHKW-Tarif, Strombezug und PV-Tarif öffnen die Tarifstruktur als Überlagerung; **Abweichung:** die `MessageBox` war bereits mit Schritt 6 ersatzlos entfallen, keine Ablösung durch `Dienste.Dialog` |
| (8) `IosProjektQuelle.BerichteKostenGaben` belegen, Whitelist erweitern | **umgesetzt #431** — liefert jetzt alle vier Seiten (Übersicht, Kosten, Wirtschaftlichkeit, Bericht); **Abweichung:** zusätzlich `UebersichtSeiteGaben`, `BerichteKostenHuelle` und `BerichtSeiteGaben` → `EPOS.UI.Daten/Bericht/`; Whitelist jetzt 21 Schlüssel (`KOSTENVERWALTUNG`, `NUTZUNGSDAUER_VERWALTUNG`, `GESETZESKATALOG`), Umfang wie mit **A19** entschieden |

**E4 — umgesetzt #432, E5 — umgesetzt #434, E6 — umgesetzt #436, E7 Teil a — umgesetzt #437, E7 Teil b —
umgesetzt #439, E7 Teil c1 — umgesetzt #440, E7 Teil c2 — umgesetzt #446, E7 Teil c3 — umgesetzt #452, E8
Teil a — umgesetzt #454, E8 Teil b — umgesetzt #455, E8c — umgesetzt #460, E9 Teil a — umgesetzt #461, E9 Teil b —
umgesetzt #462, E10 — umgesetzt #463** (Einzelheiten in der Tafel oben, in den
Statuszeilen und ihren Protokollen) — **E7, E8, E9 und E10 sind abgeschlossen**: von E8 die ValERI-Ansicht (V‑C) mit allen fünf
Blöcken samt Block 2 und Zahlungsstrombild U42, Nominalsummen, Brückenbild, „Was daraus im Lauf wird", Fußzeile
(U41/U46–U48) und E6‑Q1, und V‑D mit der Formelmappe Stufen 0 bis 3, der Anhang-E-Checkliste U43 und der
Anhang-D-Gegenprobe (die ClosedXML-Fragen aus `05/§ 3.3` sind gemessen); die Nachbesserung E8c hat die zwei kleinen
Aufträge aus E8b gebaut (Bemessungstexte aller Arten, Gliederungsprobe mit den Positionen des ersten Jahres); der
**A13-Schnitt** ist mit #435 ausgeführt; **E9** (V‑E: die Schritte B bis D — Szenariorahmen mit
Betrachtungszeitraum und Mengenfaktor, Trägerpreise best/worst, Erlössätze best/worst —, der ±-Knopf an drei neuen
Orten, der Hinweistext entfällt; ohne Degradation) ist in zwei Wellen gebaut: E9a (die Schritte als 116, 117 und 118,
der Kern liest die Paare, #461) und E9b (die Dialoge, der Wegfall des Hinweistexts, der Ausweis „n von m", #462);
**E10** (Nutzungsdauer S3 und Speicherflotte) ist in einer Welle gebaut (#463): die Instandsetzungs- und Wartungssätze
mit Schemaschritt 120, wirksam nur über die ausdrückliche Vorbelegung, die Speicherflotte an der Nutzungsdauertabelle
und die Kennzeichnung der geräteeigenen Spalten, ohne neue Basis; den eigenen Entscheid zu ND‑S3 vertreten die Fragen
aus E10. **Alle Fragen sind entschieden** (24.09.2026, nach Empfehlung); die zwei Entscheide mit offenem Bau —
E9b‑Q5 b und E7c3‑Q6 a — sind mit der kleinen Bauwelle **E13 (#474)** gebaut, dazu der Halbsatz aus A8 und zwei
Hilfe-Anker. **E12** (Wiki-Runden) ist mit #470 vorbereitet, der Sammel-Upload am 26.09.2026 freigegeben; E11 entfällt —
damit ist der Etappenplan E0–E12 bis auf den Upload abgearbeitet. Nach dem Befund 1 aus E9a ergänzt **E14 (#477)** den
Teil b von E8: Die Formelmappe rechnet die Stufen 1 und 2 für alle drei Szenarien; ihre drei Fragen E14‑Q1…Q3 sind am 24.09.2026 nach Empfehlung entschieden (a).
Aus V‑E (E9) ist das Risiko V‑G7 mit dem eigenen Auftrag **E15 (#478)** gebaut — Schemaschritt 125, Zinszuschlag oder
Zahlungsstromabzug, Vorgabe aus; seine vier Fragen E15‑Q1…Q4 sind am 24.09.2026 nach Empfehlung entschieden (a). Die nicht monetarisierbaren Wirkungen V‑G11
sind mit **E17 (#479)** gebaut — Schemaschritt 127, Liste mit Kategorie und Beurteilung statt Freitext, ohne
Rechenwirkung; seine vier Fragen E17‑Q1…Q4 sind am 24.09.2026 nach Empfehlung entschieden (a). Die n-jährlichen Zeitpunkte V‑G3 sind mit **E16 (#484)** gebaut —
Schemaschritt 129, Betriebspositionen „alle n Jahre", ohne Pflege ergebnisneutral; seine vier Fragen E16‑Q1…Q4 sind
offen. Damit ist die Gap-Tafel V‑G des Konzepts geschlossen. Aus Konzept § 6.3 erledigt die kleine Welle **E18 (#492)**
die Nr. 14 und 16 ohne Schemaschritt; Nr. 18 bleibt offen, Nr. 33 ist neu. Die kleine Welle **E19 (#498)** schließt
Nr. 15 als überholt und erledigt Nr. 33 ohne Schemaschritt; zu Nr. 10, 11, 13, 18 und 19 hat der Anwender am
25.09.2026 entschieden (→ Register R‑Rest).
**Wiederaufnahme:** Die Umsetzung war am 20.09.2026
zurückgestellt (Statusdatei, „Nach #405" (f)); der Anwender hat sie am **22.09.2026** mit dem Auftrag
wieder aufgenommen, das Mockup `Dialog_Formel_Zahlenprobe.html` umzusetzen.

**Reihenfolge und Begründung.** E0 und E1 haben keine Voraussetzung und sichern alles Folgende ab —
ohne Anker und Wachen ist keine Rechen- oder Berichtsänderung dieses Feldes abnehmbar (N1–N4). E2 und
die ersten drei Schritte von E3 sind ohne Entscheid möglich. E3 vor E5, weil sonst jedes Stück der
Ergebnisansicht nur für Windows entsteht. E7 und E9 sind die beiden Wellen mit Rechenwirkung; sie
brauchen die Anker aus E1 und je einen A/B-Nachweis, der Referenzlauf sieht sie nicht. E10 wirkt auf die
Projektwirtschaftlichkeit erst nach der Vorbelegung durch den Anwender und in der Flottenstudie; ihr A/B-Nachweis steht
in Nach #463. E8 ist die
einzige Etappe mit belastbarer Beschreibung im Konzept (§ 2.11.6). Ein iOS-Lauf ist erst mit E3
Schritt 8 begründet und läuft nur nach Rückfrage.

## 6 Schemaschritte dieser Etappen

**Die Nummern stehen nicht mehr im Plan.** Als dieses Papier entstand, war 97 der nächste freie
Schritt; inzwischen sind **97–100 anderweitig vergeben** — 97 Szenario und Bezugsjahr der Klimaregion
(`Schritt97_KlimaSzenario`, KL‑6, #382), 98 BHKW-Gesamtwirkungsgrad als Faktor (reines DML, BW‑1,
#383), 99 die zwei Wirkungsgrade des BHKW (`Schritt99_BhkwWirkungsgradAnteile`, BW‑1) und 100 die
Vorgabe 0 der Fremdschlüsselspalten (FK‑1, #426). Am 23.09.2026 sind die folgenden Nummern neu geordnet
worden (#438): **101** trägt die Gebäudespalten der Gebäudesimulation (`SCHRITT_101_GEBAEUDESPALTEN`),
**102** E7 Teil a — die leere `KWKG_Anlagenart` wird NULL (Konzept § 6.3 Nr. 30, #437; bis #438 als 101
geführt) —, **103** Katalog, Zonen und Projekt des Zapfprofilgenerators (Z0, #438), **104** E7 Teil b — der
Zeitzonentarif wird abgelöst, die Leistungspreis-Staffel zieht an den Stromträger (Q11, #439) —, **105**
E7 Teil c1 — Kennzeichen „Vorrichtung zur Abwärmeabfuhr" und Stromkennzahl je Anlage (K‑1, Schritt **A**,
#440). Danach kamen **106** — fremde Ergebnisverweise der Wirtschaftlichkeit werden NULL, eine
Datenbereinigung der Welle #444 (`SCHRITT_106_WIRTSCHAFTLICHKEIT_FREMDVERWEIS`) —, **107** — die
Ergebnistabelle je Gebäude der Gebäudesimulation (`SCHRITT_107_ERGEBNIS_GEBAEUDE`, Entscheid E30) —,
**108** bis **110** — die Kühlung der Gebäudesimulation, Stufe KU1 (108 KU-S1 `SCHRITT_108_KUEHLUNG_GEBAEUDE`,
109 KU-S2 `SCHRITT_109_KUEHLUNG_PROJEKTEINSTELLUNG`, 110 KU-S4 `SCHRITT_110_KUEHLUNG_ERGEBNIS`) — und die drei
Schritte von E7 Teil c2 (#446): **111** = E, **112** = F, **113** = G (gebaut als 107/108/109, beim
Zusammenführen mit 106 und 107 auf 108/109/110 und mit der Kühlung auf 111/112/113 umnummeriert). Danach kam
**114** — der Kühlbetrieb am Erzeuger der Gebäudesimulation, Stufe KU2 (KU‑S3, `SCHRITT_114_KUEHLUNG_ERZEUGER`,
Commit `24074b3a`). **Vergabe vom 24.09.2026:** **115** — die Zapfkategorien der Zapfprofil-Stufe Z3 (T2,
`SCHRITT_115_ZAPFKATEGORIEN`, `Tab_TwwZapfkategorie_STAMM`, #453) — und die Schritte B, C und D der Etappe E9 als
**116, 117 und 118** (`SCHRITT_116_SZENARIO_RAHMEN`, `SCHRITT_117_TRAEGERPREIS_SZENARIO`,
`SCHRITT_118_ERLOESSATZ_SZENARIO`, gebaut #461, E9a; die Lücke bei 115 ist mit dem Nachzug von Z3 vor dem Merge
geschlossen). **119** trägt die Abrechnungsart des Kältestroms und die Kälteseite der Wärmepumpenergebnisse (Kühlung
KU2 Welle 3, Entscheid E34, `SCHRITT_119_KAELTESTROM`). `SchemaStand.Zielversion` steht auf **120**: **120** trägt die
Instandsetzungssätze der Standardzeilen der Nutzungsdauertabelle — die Nachsaat der Etappe E10, reines DML ohne eigenen
Buchstaben (`SCHRITT_120_NUTZUNGSDAUER_SAETZE`, gebaut #463); der nächste freie Schritt ist 121, vorgesehen für die
Zapfprofil-Stufe Z4. Die Angabe vom 23.09.2026 („114 Zapfprofil-Stufe Z3, 115
Nachbarsitzung „Dialog Design"") ist damit überholt: 114 hat die Kühlung genommen, „Dialog Design" braucht für #458
und #459 keinen Schritt. **Weitere Vergabe vom 24.09.2026:** 121 der Katalogverweis des Projektgebäudes (#468), 122
und 123 die Anlagenkopplung AK1, 124 die Zapfprofil-Stufe Z4 (#464) und **125** — das Risikomodul der Etappe E15
(V‑G7, `SCHRITT_125_RISIKOMODUL`, gebaut #478); `SchemaStand.Zielversion` steht auf **125**. Die nächste Nummer
bekommt, wer zuerst pusht (126 ist für E17 vorgesehen). **Nachtrag #479:** 126 hat die Reparatur der Gebäude-Katalogsätze
genommen (Dialog Design, #485), E17 **127** — die Tabelle der nicht monetarisierbaren Wirkungen (V‑G11,
`SCHRITT_127_NICHT_MONETAERE_WIRKUNGEN`, gebaut #479); `SchemaStand.Zielversion` steht auf **127**, der Schritt steht
nach 126, ohne Lücke. **Nachtrag #484:** 128 hat die Anlagenkopplung AK1, Welle 3, genommen (Heizkreis je Gebäude im
Ergebnis, `ErgebnisGebaeudeSchema.SCHRITT_HEIZKREIS`, gepusht am 24.09.2026 um 21:22 Uhr), E16 **129** — die
Wiederholperiode je Kostenposition (V‑G3, `WiederholperiodeSchema.SCHRITT`, gebaut #484; in Phase 1 vorläufig 128);
`SchemaStand.Zielversion` steht auf **129**. **Nachtrag #492:** 130 hat die Berichtigung der Anschlusslängen im
Gebäudekatalog genommen (#493, `GebaeudeAnschlusslaengenReparatur.SCHRITT`, reines DML; in der Sitzung vorläufig 131);
E18 (#492) kommt ohne Schritt aus; `SchemaStand.Zielversion` steht auf **130**. **Nachtrag #498:** 131 hat die
Zapfprofil-Stufe Z4b genommen (#486, `SCHRITT_131_ZAPFPROFIL_TYPTAGE`), 132 bis 139 die Cloud-Sitzungen G3, G4 und AK1
(Baustoffkatalog, Bauteilaufbau, Zonen, Kühlübergabe mit Ergebnis und Zone, Importzuordnung, Baujahr), 140 die
Messreihen der Zapfprofil-Stufe Z5 (#495) und 141 die Folgeberichtigung der Anschlusslängen im Gebäudekatalog (#496,
`GebaeudeAnschlusslaengenFolgereparatur.SCHRITT`); E19 (#498) kommt ohne Schritt aus; `SchemaStand.Zielversion` steht
auf **141**.

Damit keine Nummer zweimal vergeben wird, führt dieses Papier die geplanten Schritte fortan mit
**Buchstaben**. Jeder bekommt seine Nummer **bei der Umsetzung**, aus dem dann freien Bereich (nach der Vergabe vom
24.09.2026 ab 121), und der Umsetzende misst sie an `SchemaStand.Zielversion` neu — nicht an diesem Papier. Alle
Schritte außer **F** (DDL mit einmaligem DML) und **G** (reines DML) sind reines DDL ohne DML,
ergebnisneutral bis zur ersten Pflege; Testdatenbank über `Werkzeuge/Testdatenbankschema`,
Auslieferungsvorlage und Erstbereitstellung ohne Sonderbehandlung.

| Schritt | vormals | Inhalt | Tabelle | Doppelpflicht `SpalteSicher` | Einfrierregel | Etappe |
|---|---|---|---|---|---|---|
| **102** | — | Nr. 30: leere `KWKG_Anlagenart` → NULL (`SCHRITT_102_KWKG_ANLAGENART_LEER`; bis #438 als 101 geführt) — **reines DML**, sieben Zellen der Testdatenbank, kein BHKW | `Tab_Energieanlagen` | nein | nein | E7 Teil a, **gebaut #437** |
| **104** | — | Q11: `Leistungspreis_Staffelgrenze`, `Leistungspreis_Staffel1`, `Leistungspreis_Staffel2` (DOUBLE) — dazu DML in einer Transaktion: die Staffel der Zonensätze an den Stromträger, die Zonensätze gelöscht, ihre gespeicherten Läufe verworfen, die Zonenzeilen der Strommatrix je Projekt eine Jahreszeile (`SCHRITT_104_ZEITZONENTARIF_ABLOESUNG`) | `energy_project_settings`, `Tab_ProjektTarif`, `Tab_ErgebnisWirtschaftlichkeit`, `Tab_ErgebnisWirtSensitivitaet`, `Tab_ErgebnisStromMatrix` | nein | nein | E7 Teil b, **gebaut #439** |
| **105** (A) | 97 | K‑1: `KWKG_Abwaermeabfuhr` (INTEGER 0/1, CHECK, Vorgabe 0), `KWKG_Stromkennzahl` (REAL, nullbar) — **reines DDL** (`SCHRITT_105_KWKG_ABWAERMEABFUHR`), ergebnisneutral | `Tab_Energieanlagen` | nein | nein | E7 Teil c1, **gebaut #440** |
| **116** (B) | 98 | Szenariorahmen: `Szen_Best/Worst_Zeitraum` (ganze Jahre), `Szen_Best/Worst_Menge` (%) (nicht `_Dauer`) — **reines DDL** (`SCHRITT_116_SZENARIO_RAHMEN`), ergebnisneutral bis zur ersten Pflege | `Tab_ProjektWirtschaftlichkeit` | **ja** (CREATE-TABLE-Texte und `SpalteSicher` der `WirtschaftlichkeitCtrl`) | nein | E9 Teil a, **gebaut #461** |
| **117** (C) | 99 | Trägerpreise best/worst: `custom_price_work/base/power_best/_worst` — **reines DDL** (`SCHRITT_117_TRAEGERPREIS_SZENARIO`); der Kern legt die Tabelle nirgends selbst an, `VariantenCtrl` kopiert die sechs Spalten beim Anlegen einer Variante mit | `energy_project_settings` | nein | nein | E9 Teil a, **gebaut #461** |
| **118** (D) | 100 | Erlössätze best/worst: `Einspeiseverguetung(_KWK)_Best/_Worst`; `DvEntgelt_Best/_Worst`, `PpaPreis_Best/_Worst` — **reines DDL** (`SCHRITT_118_ERLOESSATZ_SZENARIO`); nicht: `PpaSpotAufschlag` und die Marktwertfelder (E9a‑Q1) | `Tab_ProjektWirtschaftlichkeit`, `Tab_ProjektPhotovoltaik` | **ja** (PPV und Wirtschaftlichkeit) | nein | E9 Teil a, **gebaut #461** |
| **111** (E) | 101 | `ErsatzFuehren`, `RestwertAnsetzen` (nullbar, CHECK; NULL = wie bisher) — **reines DDL** (`SCHRITT_111_ERSATZ_RESTWERT_KENNZEICHEN`), ergebnisneutral | `Tab_ProjektWerte`, `Tab_KostenVorlagePosition` | nein | nein | E7 Teil c2, **gebaut #446** |
| **112** (F) | 102 | `Preisbasis` (TEXT, nullbar) mit einmaligem DML aus `ID_Umrechnung` (`SCHRITT_112_PREISBASIS`; Testdatenbank 5 Zeilen „kWh", 23 Abrechnungseinheit) | `energy_project_settings` | nein | nein | E7 Teil c2, **gebaut #446** |
| **113** (G) | 103 | U‑1: `Einheit`/`PreisEinheit` der fünf Gase auf `Nm³`, eine `energy_price`-Zeile (Projekt 1039), dazu der Brennstoff 24 auf `kWh` (E7c2‑Q4) — **reines DML** (`SCHRITT_113_GASE_NM3`), vor dem Vorlagenbau | `Tab_Brennstoff_Stamm`, `energy_price` | nein | nein (Einfrierliste nennt nur CO₂/SO₂/NOx/Staub) | E7 Teil c2, **gebaut #446** |
| **120** | — | ND‑S3: die leeren Instandsetzungssätze der Standardzeilen bekommen die Mitte des Empfehlungsbereichs der Betriebsvorlagen (Heizkessel 2,0, BHKW 6,0, Wärmezentrale 2,0, Stromeinspeisung 2,0, Bauliche Anlagen 1,25 %; Wartung leer) — **reines DML** (`SCHRITT_120_NUTZUNGSDAUER_SAETZE`), setzt nur leere Zellen, wiederholbar; ergebnisneutral, weil ein Satz erst über die Vorbelegung rechnet | `Tab_Nutzungsdauer` | nein | nein | E10, **gebaut #463** |
| **125** | — | V‑G7: `Risiko_Art` (TEXT(10); leer = kein Risiko, `ZINS`, `ABZUG`), `Risiko_Zinszuschlag` [%-Punkte], `Risiko_Verlust` [€ je Periode, R_loss], `Risiko_Wahrscheinlichkeit` [%, p_loss], nullbar, ohne Vorgabe — **reines DDL** (`SCHRITT_125_RISIKOMODUL`, Spaltenliste `SchemaKatalog.RisikomodulSpalten`), ergebnisneutral bis zur ersten Pflege; Testdatenbank 125 (vier Spalten leer, LFS `6c4c32f9…`) | `Tab_ProjektWirtschaftlichkeit` | **ja** (CREATE-TABLE-Text und `SpalteSicher` der `WirtschaftlichkeitCtrl`) | nein | E15, **gebaut #478** |
| **127** | — | V‑G11: die Tabelle `Tab_ProjektWirkung` (STRICT; `ID`, `ID_Projekt` mit Fremdschlüssel auf `Tab_Projekt` ON DELETE/UPDATE CASCADE, `Sortierung`, `Kategorie` mit CHECK `ENERGIEFLUSS`/`FINANZIELL`/`SONSTIG`, `Beschreibung`, `Dauer` 1–3, `Wirkung_Organisation`, `Wirkung_Mitarbeiter`, `Wirkung_Umwelt` je 0–3, NULL = nicht beurteilt) samt Index `idx_ProjektWirkung_Projekt`; DML: ein gepflegter Freitext `Nicht_Monetaer` wird eine Wirkung SONSTIG ohne Beurteilung — nur nicht leere Texte, nur Projekte ohne Wirkung, wiederholbar; das Freitextfeld bleibt (`SCHRITT_127_NICHT_MONETAERE_WIRKUNGEN`, `ProjektWirkungSchema` für Migration, Werkzeug und Testvorrichtung), ergebnisneutral; Testdatenbank 127 nach 126 (132 Tabellen STRICT, 210 Indizes, 0 Freitexte übernommen, LFS `87e49ed1…`) | `Tab_ProjektWirkung` (neu), liest `Tab_ProjektWirtschaftlichkeit` | nein (eigene Tabelle) | nein | E17, **gebaut #479** |
| **129** | — | V‑G3: `Wiederholperiode_a` (INTEGER, nullbar, ohne Vorgabe; leer, 0 und 1 = jährlich, n ≥ 2 = Zahlung in s, s + n, … ≤ T) — **reines DDL** (`WiederholperiodeSchema.SCHRITT`, `SCHRITT_WIEDERHOLPERIODE`; die Zahl allein in der Konstante, Migration, Werkzeug und Testvorrichtung leiten sich ab), ergebnisneutral bis zur ersten Pflege; Testdatenbank 129 nach 128 (zwei Spalten leer, 133 Tabellen, 210 Indizes, LFS `4c546a7c…`) | `Tab_ProjektWerte`, `Tab_KostenVorlagePosition` | nein (nicht in `SchemaKatalog.Alle`; jeder Leser prüft die Spalte) | nein | E16, **gebaut #484** |
| **(H)** | (104) | optional: geräteeigene Nutzungsdauer entfernen | `Tab_BHKW`, `Tab_Heizkessel` | nein | nein | E10, nach A8 — **nicht gebaut**: A8 ist mit der Kennzeichnung „Nutzungsdauer (Gerätedaten)" erfüllt (#463, E10‑Q4 a); (H) bleibt optional |
| — | — | Speicherflotte an `Tab_Nutzungsdauer` | JSON in `Tab_SpeicherAuslegung` | — | **ja** (Projekt 1046) | E10, eigener Auftrag — **gebaut #463** ohne Schemaschritt: der Flottenstand bleibt, die Studie rechnet den Restwert linear; der Referenzlauf ist byte-gleich, die dritte Einfrierregel ist ergänzt |
| — | — | `SteuerErgebnis`-Trennung, Anlagenbezug der Erlöszeilen | nur im Nachweisumschlag | — | nein | E4 |

*Die Spalte „vormals" nennt die Nummer aus der Fassung vom 19.09.2026, damit Verweise aus Protokollen,
Mockup-Anhang und Statuszeilen weiter treffen. **Der Mockup-Anhang nennt bei U1 und U32 denselben
Schritt A** (dort als „Schemaschritt 97" geführt; mit E0c auf „Nummer bei der Umsetzung" geändert).*

## 7 Berichtigungen an den Papieren

Vollständige Listen mit Zeile, alt und neu: `01/§ 7` (13 Stellen Kern), `02/§ 8` (13 Stellen
Vergütung/Steuern), `03/§ 6.2` (14 Stellen Schema), `04/§ 3` und `§ 8.2` (15 WinForms-Reste, 7 Stellen),
`05/§ 8.1` (9 Stellen Bericht/Nutzungsdauer), `07/§ 2` (Geltung, Kopf, Quellen, § 6.3–§ 7). Die
Mockup-Prüfung § 3.2 bleibt daneben gültig (Kopfzeile, § 2.2–2.4, § 2.7, § 2.13, § 6.1). Die schwersten:

> **Stand 22.09.2026: mit E0 (#379) ausgeführt** — die Berichtigungen am konsolidierten Konzept
> (Geltungsblock, Kopfzeile, Quelltabelle und Artifacts, § 3.1 Formelkarte, §§ 3.2/3.4, die
> Schemaaussagen, § 2.13 (3)/(4)/(5), § 6.1, § 6.3, § 6.4, § 6.5, § 7), am Szenarienkonzept, am
> Nutzungsdauer-Konzept und an den Rechenwegen 04/05/08. **Offen geblieben und mit E0c (22.09.2026)
> nachgezogen:** die Kopfzeile trägt jetzt Stand 22.09.2026, Codestand `3b71871c` und Zielversion
> **100** (nicht mehr 96/97 wie in der Tafel unten); `WIRT_EMPF_KEINE` ist mit **#405** nachgezogen
> (G9-Referenztext); die Degradationsfrage ist mit **A5** entschieden. Die `help_mapping`-Korrektur,
> der Satz im Konzept Hilfesystem § 13.2, die Neutralisierung von „Höfingen" (A16) und der Umzug von
> „Gesetzliche Parameter" (A18) sind mit **#470** (E12) nachgezogen. **Noch offen:** die Benennung der
> Referenzmappe (A17, E11 entfällt) und der Sammel-Upload selbst (nach Freigabe des Anwenders).
>
> Wo die Tafel unten **Schemaschritte** nennt (97, 103), gelten die Buchstaben aus § 6: Schritt **A**
> statt 97, Schritt **G** statt 103.

| Papier · Stelle | Berichtigung | Quelle |
|---|---|---|
| Konzept Z. 25–29 Geltungsblock | „Stand der Umsetzung: § 6.1. Was hier als Soll steht, ist gebaut, sofern § 6.1 die Etappe führt." Arbeitsregel ins Protokoll | `07/§ 2.1` |
| Konzept Kopfzeile | „Stand 19.09.2026 · Zielversion 96 · Schritte 90–96 vergeben · neue ab 97"; Codestand streichen oder als `e1c4275e` übersetzen | `07/§ 2.2`, Q25 |
| Konzept Z. 31–38 Quelltabelle, Z. 40–52 Artifacts | Formelkarte und Feldkarte als „nicht erhalten" führen oder streichen; fünf Artifacts ins Protokoll, eines mit „Repo-Datei führt" | `07/§ 2.3–2.4` |
| Konzept § 3.1 Formelkarte | dritter Topf `Endenergie_1 × (1+p_E)^(t−1)` und `Ersatz_t = A₀ · (1+p_I)^t` aufnehmen | `01/§ 7` |
| Konzept Z. 1543–1557 (§ 3.4) | I‑2-Regel (erfasster Betrag statt 0), zehn statt neun Rückfallarten, `EUR_PRO_H`/`EUR_PRO_KWH_*` frisch, `InvestSummeFuer` über die Kaskade | `01/§ 7` |
| Konzept Z. 1478, 1494, 2076, 2080, 2418 | I‑1 und I‑3 als erledigt; „ACE" streichen; B8 auf S‑2, V‑3-Rest, B‑6, I‑5 | `01/§ 7`, `07/§ 2.9` |
| Konzept Z. 1751, 1851, 2111, 2246, 2274, 2299, 2352 | Ersatzweg je Anlage; Schrittnummer 97; S‑6, K7, 9c, 9g, Nr. 14 als erledigt | `02/§ 8` |
| Konzept Z. 969–971 (§ 2.13 (4)) | „die Strommatrix trennt nur nach Tarifzone; der Kern verteilt nach dem Netto-Stromanteil (V‑4)" | `02/§ 8` |
| Konzept Z. 712 (§ 2.11.5), Z. 928, Z. 966–968 | „6 von 8 Rahmenspalten vorhanden (Schritt 71)"; „95 von 101 Positionen ohne Dauer"; Speicherflotte als JSON-Felder mit Einfrierfolge | `03/§ 6.2` |
| Konzept Z. 3, 2178, 2189–2190 (U‑1) | Schritt 62 ist anderweitig vergeben, U‑1 steht aus (Schritt 103); Einheitenbruch-Konzept liegt in `ueberholt/`; nicht mit `Konzept_Einheiten_EPOS-Plan.md` verwechseln | `03/§ 6.2`, `07/§ 2.10` |
| Konzept § 6.5 | fünf Tabellen und 55 `SpalteSicher`; Einspeisevergütung drei Orte; `Form_Kosten`/`UcBkKosten` streichen; „Access-Abfrage" → Sicht `Abfrage_Kostenfaktoren` im Repo; Stromsteuer-Wache prüft nicht gegen den Katalog | `03/§ 6.2` |
| Konzept Z. 968–972 (§ 2.13 (3) Nr. 2, 3) | mit #357 gebaut; offen bleibt der gesperrte Zwilling auf der Betriebsseite | `05/§ 8.1` |
| Konzept Z. 684 (V‑E) ↔ Szenarienkonzept Z. 271 (G3) | Degradation: Entscheid neu stellen (A5); Umsetzungstafel V‑Gn ↔ Gn an den Anfang von § 2.11.2 | `05/§ 8.2` |
| Konzept Z. 741–742, 794 (§ 2.11.6) | 1 412 Zeilen; „kein Test deckt Excel **und** Word" | `05/§ 8.1` |
| Konzept Z. 1002–1003 (§ 2.13 (5)) | `Reihe.Gestrichelt` ist ein `bool`, dritte Strichart nötig; Palette acht Farben; `EPOS.UI.Daten` hat keinen Ordner `Wirtschaftlichkeit` | `05/§ 8.1`, `04/§ 8.2` |
| Konzept Z. 1339 | iOS: zwei Bedingungen (Hülle **und** `BerichteKostenGaben`/Whitelist) | `04/§ 8.2` |
| Szenarienkonzept § 9.1 Z. 322–337, § 7.1 Z. 240 | Maßstab ist die gewählte Referenz; Ressource `WIRT_EMPF_KEINE` nachziehen | `05/§ 8.1` |
| Konzept § 6.4 | drei ACE-Fallen auf den Migrationslauf einschränken; `dev\` und „nur MSBuild" streichen | `07/§ 2.7` |
| Konzept § 6.1 | acht Etappenzeilen ergänzen (§ 2.3 dieses Papiers) | `07/§ 5` |
| Konzept § 5 (neu) | Grundlagen § 10 Nr. 1–4 aufnehmen; Grundlagen § 5 als Abnahmeliste von A8 verlinken | `07/§ 4` |
| Rechenweg 05 Z. 303; Rechenweg 08 Z. 39; Rechenweg 08 Höfingen-Absatz | K‑1 Schritt 97; Mockup-Knöpfe ohne Element kennzeichnen; Referenzmappe benennen | `02/§ 8`, `05/§ 8.1`, `08/§ 6.1` |
| `help_mapping.txt` Z. 259, 334 | `Form_Tarifstruktur.btn_Help = Wirtschaftlichkeit#strombezug`; Gesetzesparameter auf einen Abschnitt der Kostenseite (A18) | `06/§ 4` |
| Konzept Hilfesystem § 13.2 | Satz zu Mockup-Beispielen; Orts- und Projektnamen wie Produktdaten behandeln | `06/§ 6.2` |

## Nicht geprüft

Kein Bau, kein Test, kein Referenzlauf: Alle Aussagen über Tests sind gezählte Marken, keine Läufe.
Das Laufzeitverhalten von ClosedXML (Formelablage, Neuberechnung, fremde Tabellenkalkulationen), der
Inhalt des Normtexts unter `Quellen/VALERI/`, die Formeln der XLS-Mappen der Altanwendung (nur Werte
lesbar) und die Live-Wiki-Seiten wurden nicht geprüft. Die iOS-Angaben stützen sich auf Whitelist, Gaben
und Adapter im Quelltext, nicht auf einen Gerätelauf. `WirtschaftlichkeitCtrl.cs` (7 336 Zeilen) wurde
abschnittweise gelesen; `BaueKwkgReihe`, `BaueSteuerReihen`, `RechneRollentarif`, `RechnePvVerguetung`
und `Persistiere` blieben außen vor. Die Zahlen des Beispielprojekts wurden nicht erneut nachgerechnet
(Mockup-Prüfung `01`). Ob die Berichte der Agenten an Stellen, die der Pull vom Abend verändert hat
(`GesetzeskatalogDialog.razor`, Menü, Stilblatt), noch wörtlich gelten, wurde nur für Gesetzeskatalog,
Menüort und Zielversion nachgemessen.

# Wiki- und Hilfeseite der Umsetzung — Wirtschaftlichkeitskonzept

Reine Prüfung, keine Repo-Änderung, kein Build, kein Upload. Stand der Prüfung: 19.09.2026.

**Abkürzungen der Belege:** Konzept = `Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`;
ND = `Dokumentation/aktuell/Konzept_Nutzungsdauer_AfA_EPOS-Plan.md`; VALERI = `Dokumentation/aktuell/Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md`;
Hilfesystem = `Dokumentation/aktuell/Konzept_Hilfesystem_Wikidokumentation.md`; Status = `Dokumentation/aktuell/Status_iOS_Migration.md`;
Mockup = `Dokumentation/aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`; 05 = `Dokumentation/ueberholt/Protokolle/Reporting/Pruefung_Mockups_2026-09-19/05_Wiki.md`;
K.wiki/W.wiki/V.wiki/E.wiki/P.wiki/S.wiki/Sim.wiki = `Projekte/Wiki/Programm Dokumentation - {Kosten|Wirtschaftlichkeit|Varianten|Emissionen|Photovoltaik|Stromspeicher|Simulationsergebnisse}.wiki`;
help_mapping = `WindowsFormsApplication1/Allgemein/Hilfe/help_mapping.txt`.

## 0 Ergebnis in fünf Sätzen

Von den elf Repo-Wiki-Seiten tragen sieben einen Wirtschaftlichkeitsbezug, doch für die Rechenwege des Konzepts (§ 3.1–3.11) gibt es keine eigene Berechnung-Seite — anders als beim Stromspeicher fehlt „Berechnung/Wirtschaftlichkeit" oder „Berechnung/Kosten" unter `EPOS.Kern/Allgemein/Hilfe/Berechnung/` ganz. Die als „umgesetzt" geführten Konzeptabschnitte (§ 2.6, 2.9, 2.14–2.16) sind im Wiki überwiegend mit eigenem Anker beschrieben; zu den fünf Lücken aus 05/§4 kommen zwei weitere hinzu, die der Mockup-Anhang selbst als „erledigt" führt, aber im Wiki noch fehlen (U23 Satzherkunft-Zeilen, U36 zweite PV-Anlagenwarnung — beide bereits mit 05/§4.4–4.5 identisch benannt). Der Mockup-Anhang „Umsetzungsstand" (Mockup:4344–4746) führt 39 U-Zeilen, davon 19 weiterhin offen (U1–U7, U9–U15, U22, U25, U27, U32, U39); sie bilden zusammen mit den VALERI-Etappen V‑C bis V‑E und der Nutzungsdauer-Stufe S3 den Pflegeplan der nächsten Wiki-Runden. In der Hilfe-Zuordnung bestätigen sich die zwei Funde aus 05/§6 und kommt ein dritter hinzu: `Form_Tarifstruktur.btn_Help` zeigt auf die Seite „Kosten" (help_mapping:259), obwohl der zugehörige Inhalt „Strombezug…" auf `Wirtschaftlichkeit#strombezug` liegt, und `Form_Gesetzesparameter` zeigt auf eine Seite ohne jede Repo-Quelle. „Höfingen" ist über die Altmappe `Quellen/BHKWPlan/BHKW_Höfingen_Erneuerung_20kWel.XLS` und die vom Konzept selbst als „reale Höfingen-Zahlen" bezeichneten Kennwerte (Konzept:51, 846, 857) mit hoher Wahrscheinlichkeit ein echtes Altprojekt und gehört vor jeder Wiki-Veröffentlichung neutralisiert, auch wenn Regel 13.2 wörtlich nur Hersteller- und Produktdaten nennt.

## 1 Inventar der Wiki-Quellen

Anker-Muster verifiziert (eigener Grep, deckungsgleich mit 05/§3): ausschließlich `{{Anker|name}}`, teils mit zwei Namen in einem Tag (`{{Anker|a|b}}`); `<span id=…>` kommt nicht vor. Überschriften/Zeilenzahl je Seite: 05/§3 — hier nicht wiederholt, nur die für diese Aufgabe neue Spalte „Statusbezug".

| Seite | Zeilen | Anker (eigen gezählt) | Jüngster Statusbezug (Repo-Quelle) | Beleg |
|---|---|---|---|---|
| Kosten.wiki | 168 | 41 Tags / 43 Namen | **#366** „Hilfsstrom: Preis des eigenen Stromträgers je Anlage, Vorlagenhinweis" | Status:287 |
| Wirtschaftlichkeit.wiki | 71 | 34 Tags/Namen | **#360** „Projekttransfer einer einzeln übertragenen Variante" (PV-Vergütung reist mit) | Status:183 |
| Varianten.wiki | 32 | 3 Tags / 4 Namen | **#359/#360** „Vergütung je Variante" / „PV-Vergütung beim Weitergeben" | Status:182–183 |
| Stromspeicher.wiki | 504 | 34 (05/§3) | **#281** „Größensuche Stromspeicher nur über Kapazität und Leistung…" — jüngster im Rahmen dieser Prüfung sicher zugeordnete Treffer; ob #327 (18.09., Schließkreuz-Sperre) die Seite ebenfalls änderte, ist nicht geprüft | Status:205, vgl. 148 |
| Photovoltaik.wiki | 49 | 9 (05/§3) | kein Treffer neuer als die Bereinigung **#251** (13.09., Revision 587); die PV-Vergütungs-Aufträge (#350, #359, #360) betreffen laut 05/§4.4 die Seite Wirtschaftlichkeit, nicht Photovoltaik | Status:174; Hilfesystem:937 |
| Emissionen.wiki | 23 | 2 (05/§3) | kein Treffer neuer als **#251** (13.09., Revision 586); #280 (CO₂-Rückfall aus der Kategorie) betrifft nach Wortlaut eher `Kosten#wert-aus-kategorie` als die Emissionskatalog-Seite | Status:174, 204 |
| Simulationsergebnisse.wiki | 35 | 3 (05/§3) | kein Treffer neuer als die Dokumentationspflege **11.09.2026** (Revision 532); kein #3xx-Auftrag im geprüften Zeitraum nennt die Seite namentlich | Hilfesystem:831 |

Methode: Themen-Kurztitel aller Statuszeilen #76–#369 wurden gegen die sieben Seitennamen abgeglichen (Status:97–290, Volltext der einschlägigen Zeilen zusätzlich gelesen); kein Volltextabgleich jeder einzelnen der rund 200 Auftragszeilen — Einschränkung siehe „Nicht geprüft".

## 2 Zuordnung Konzept → Wiki

### 2.1 Konzept § 2.2–2.16 (Dialograum)

„Stand" übernimmt die Markierung des Konzepts selbst (`umgesetzt`-Vermerk in der Abschnittsüberschrift bzw. § 6.1); „Wiki" nennt den bestehenden Anker oder „fehlt".

| § | Abschnitt | Stand | Wiki-Seite · Anker | Befund |
|---|---|---|---|---|
| 2.2 | `Form_BhkwWirtschaftlichkeit`, Gruppen 1–4, 6 | in Arbeit (§7 nennt B5 ohne „umgesetzt", einzelne Gruppen aber über #325/#329/#335/#336/#342/#346/#352/#353/#356/#357 real ausgeliefert) | W.wiki `kwk-je-anlage`(28) `kwk-eine-quelle`(29) `kwk-vorschlag`(30) `kwk-projektweit`(31) `stromsteuer-modus`(26) | überwiegend beschrieben; **Doppelpflege-Warnzeile Gruppe 5 fehlt** (deckungsgleich mit 05/§4.1) |
| 2.2 Gruppe 1b/Überlagerung „Sätze und Herkunft" | U22 | **offen** (nicht gebaut) | — | zu Recht kein Wiki-Eintrag |
| 2.3 | `Form_PhotovoltaikVerguetung` | Bestand + U36 erledigt (Mockup:4636–4648) | W.wiki `pv-verguetung`(32), V.wiki `pv-verguetung`(12) | Herkunftszeile/eigene Werte beschrieben; **beide Anlagenwarnungen fehlen** (05/§4.4 — U36 „erledigt", Wiki zieht nicht nach) |
| 2.4 | `Form_WirtschaftlichkeitParameter` (vier Gruppen nach Auszug) | umgesetzt | W.wiki `parameter`(19) `szenario`(18) `szenarien`(21) `einspeiseverguetung`(22) `nicht-monetaer`(24) `stromsteuer-modus`(26) | Kernfelder beschrieben; Feld „Preissteigerung Investition" (p_I, VALERI Teil b, § 10.4) ohne erkennbaren eigenen Anker — vermutlich unter `parameter` mitgeführt, Wortlaut nicht geprüft |
| 2.5 | Preis-/Trägerdialoge, Emissionsspalte | umgesetzt | K.wiki `traegerkarte`(90) `einheiten`(91) `preisbasis`(92) `umrechnungsregeln`(93) `preishistorie`(96) `katalogwerte-uebernehmen`(97) `emissionsblock`(98) `emissionsspalte`(12) `aufschlaege`(102) `preisbestandteile`(120) `traeger-je-anlage`(126) | sehr vollständig; `traeger-je-anlage` deckt vermutlich §3.4/E1 (#366) — Wortlaut nicht gelesen |
| 2.6 | Erlösrubrik Block A/B | umgesetzt (B7) | W.wiki `block-a`(41) `block-b`(42) `vermiedene-kosten`(43) `nullzeile`(44) `energiekosten-je-anlage`(45) | Blöcke selbst beschrieben; **U23-Satzzeilen „… · Vorschlag/eigener Wert" unter Einspeisung/Eigenverbrauch fehlen**, obwohl U23 „erledigt" ist (Mockup:4630–4642, deckungsgleich mit 05/§4.5) |
| 2.7 | Hausstil | Vorgabe für Entwickler, kein Anwenderinhalt | — | zu Recht kein Wiki-Eintrag (keine Bedienung) |
| 2.8 | Betriebskosten-Raster Entwurf B | Konzept selbst: „nur Konzept, keine Umsetzung" (Konzept:527) — **aber** Teile sind über #345/#347 real ausgeliefert | K.wiki `herleitung`(60) `betriebspositionen`(10) `empfehlung`(64) `betriebsmengen`(63) | die ausgelieferten Teile (Herleitung, Schloss, Empfehlungszeile, Laufstand) sind beschrieben (05/§4.1); der Rest (Banner „Mengen stammen aus dem Simulationslauf vom…") bleibt zu Recht offen |
| 2.9 | Vergleichsprojekt/Referenz | umgesetzt | W.wiki `referenz`(9) | beschrieben (05/§4.5) |
| 2.10 | Integrationsort ValERI | interne Platzierungsentscheidung | — | kein eigener Anker nötig |
| 2.11.1–2.11.2 | ValERI-Kernaussage, Gap-Tabelle V-G | Analyse/Planung, kein Programmzustand | — | zu Recht kein Wiki-Eintrag (Regel 13.1: kein Planungsstand) |
| 2.11.3–2.11.6 | Fünf Darstellungsblöcke, Etappen, Szenarioabdeckung, Formelbericht | **offen** (V-C/V-D/V-E nicht umgesetzt, Konzept:682–684) | — | zu Recht kein Wiki-Eintrag; siehe Abschnitt 3 |
| 2.11.7 | Hinweistext bis vollständiger Szenarioabdeckung | **offen** — U10 unerledigt (Mockup:4436–4438) bestätigt: Ressourcenschlüssel `WIRT_SZEN_HINWEIS` noch nicht angelegt | — | Wortlaut vorab geprüft, siehe Abschnitt 5 |
| 2.12 | Kategorien-Mockups mit Rechenweg | Dev-Artefakt, kein Programmzustand | — | kein Wiki-Eintrag; liefert aber Höfingen-Beispieldaten, siehe Abschnitt 5 |
| 2.13 (1)–(6) | Ergebnisansicht-Durchsicht | **fast vollständig offen** — nur (2) „Zuschuss" evtl. bereits Bestand | — | Einzelpunkte decken sich mit § 6.3 Nr. 9h–9j und U3/U6/U39; Pflegeplan in Abschnitt 3 |
| 2.14 | Erfassungsgruppen | umgesetzt (#343) | K.wiki `erfassungsgruppen`(16) | beschrieben |
| 2.15 | Vergleichssicht | umgesetzt | W.wiki `vergleichssicht`(12) `vergleichswahl`(11) `spalten`(13) | beschrieben (05/§4.5); U37 „erledigt" bestätigt |
| 2.16 | Vergütung je Variante | umgesetzt, Konzept nennt Ziel selbst (Konzept:1345–1346) | W.wiki `pv-verguetung`(32) `bericht-pv-herkunft`(59), V.wiki `pv-verguetung`(12) | beschrieben (05/§4.4, 4.5); U38 „erledigt" bestätigt |

### 2.2 Konzept § 3.1–3.11 (Rechenwege)

**Strukturbefund vorab:** Unter `EPOS.Kern/Allgemein/Hilfe/Berechnung/` liegen 13 Rechenweg-Seiten (`BHKW.wiki`, `Heizkessel.wiki`, `Photovoltaik.wiki`, `Pufferspeicher.wiki`, `Solarthermie.wiki`, `Stromspeicher.wiki` u. a. — vollständige Liste per `Glob`) plus `_Index.wiki`/`_Bezuege.wiki`; **eine Seite „Berechnung/Wirtschaftlichkeit" oder „Berechnung/Kosten" existiert nicht.** Rechenwege des Wirtschaftlichkeitskonzepts stehen deshalb, wenn überhaupt, nur in vereinfachter Form auf den Bedienungsseiten.

| § | Rechenweg | Wiki-Erklärung | Befund |
|---|---|---|---|
| 3.1 | Kapitalwert-Rahmenformel, Kennzahlen | fehlt (keine Formel im Wiki) | Kennzahlnamen tauchen in W.wiki `spalten`(13) nur als Tabellenspalten auf, ohne Formel |
| 3.2 | Investitionskaskade (3 Runden) | teilweise — K.wiki `bemessung`(22)/`herleitung`(60) erklären die Bemessungsarten und die Herleitungszeile umgangssprachlich, ohne Kaskaden-Formel | Rechenweg im engeren Sinn fehlt |
| 3.3 | Zuschüsse | fehlt | — |
| 3.4 | Betriebskosten, Endenergie je Komponente, Stromträger je Anlage | teilweise — K.wiki `betriebsmengen`(63), `traeger-je-anlage`(126) | Grundregel plausibel beschrieben (Wortlaut nicht geprüft), Formel fehlt |
| 3.5 | Energiekosten/CO₂, Anteile vs. Arbeitspreis | teilweise — K.wiki `preisbestandteile`(120), `aufschlaege`(102), `emissionsblock`(98) erklären das Prinzip „Transparenz, keine Preiswirkung" | Formeln fehlen |
| 3.6 | KWKG-Zuschlag (marginale Staffel, Ersatzweg) | teilweise — W.wiki `kwk-eine-quelle`(29) beschreibt den Ersatzweg umgangssprachlich zutreffend | Staffelformel und Hilfsstrom-Netting-Formel fehlen |
| 3.7–3.8 | Energiesteuer, Stromsteuer | teilweise — W.wiki `stromsteuer-modus`(26) | Sätze/Formeln fehlen |
| 3.9 | Kohärenzprüfung | teilweise — W.wiki `kohaerenz`(52) | Regelliste vermutlich vorhanden, nicht im Detail geprüft |
| 3.10 | Rechenreihenfolge | fehlt (reine Entwicklerinformation, gehört nicht ins Wiki) | zu Recht kein Anker |
| 3.11 | Emissionsfaktoren/CO₂-Preispfad | fehlt (Formeln), K.wiki `emissionsblock` nennt nur die Bilanzierungsmethode | Rechtsstand-Tabelle (Faktoren, Preispfad) ist ohnehin Tabuwort-kritisch (§ 13.1) und gehört so nicht ins Wiki |

**Einordnung:** § 3 ist die Formelkarte für Entwickler und Nachweis, nicht für Anwender — dass sie im Wiki fehlt, ist überwiegend **kein Mangel**, sondern folgerichtig (Regel 13.1: nur Ist-Zustand der Bedienung, keine Formeln). Eine Ausnahme wäre eine künftige `Berechnung/Wirtschaftlichkeit`-Seite nach dem Muster von `Berechnung/Stromspeicher` — dafür liegt aber kein Auftrag vor.

### 2.3 Nutzungsdauer-Konzept (ND)

| Abschnitt | Stand | Wiki | Befund |
|---|---|---|---|
| S1 Tabelle „Nutzungsdauern (AfA)" | umgesetzt (ND:225) | K.wiki `nutzungsdauern|afa`(129), `nutzungsdauer-raster`(133), `nutzungsdauer-standard`(134) | beschrieben |
| S2 Vorbelegung, Knopf, Tafel „Ersatz und Restwert" | umgesetzt; ND nennt Ziel selbst: „Wiki „Programm Dokumentation/Kosten"" (ND:226, 262) | K.wiki `nutzungsdauer-vorbelegen`(147), `ersatz-restwert`(17) | beschrieben (05/§4.1) |
| Hinweiszeile „T über Vorgabe, k von n ohne Dauer" (§ 2.4a) | **offen** — bleibt Windows-Schale (Status „Nach #357 (a)") | — | zu Recht kein Anker; siehe Abschnitt 3 |
| S3 Instandsetzung/Wartung, Gerätekataloge | offen, eigener Entscheid nötig (ND:227) | — | zu Recht kein Anker; siehe Abschnitt 3 |

### 2.4 Szenarien/VALERI-Konzept

| Etappe | Stand | Wiki | Befund |
|---|---|---|---|
| W5‑B‑9 Parametersatz je Szenario | umgesetzt | W.wiki `szenarien`(21), `parameter`(19) | Dialogabschnitt „Szenarien" vermutlich mitbeschrieben; Wortlaut nicht geprüft |
| W5‑B‑10 V1 Restwert/Ersatz eigener Ausweis | umgesetzt | W.wiki `spalten`(13) — Zeilen „Ersatzbeschaffungen, Barwert"/„Restwert, Barwert" **wörtlich genannt** (Grep-Fund) | beschrieben |
| W5‑B‑10 V2 Annahmen in Nachweiszeile | umgesetzt | W.wiki `nachweis`(51) | vermutlich beschrieben, Wortlaut nicht geprüft |
| W5‑B‑11 G9 Empfehlungsregel | umgesetzt | W.wiki `vorschlag`(53) | vermutlich beschrieben |
| W5‑B‑11 G7 Hinweiszeile „kürzeste/längste Nutzungsdauer, Ersatz/Restwert" | umgesetzt (VALERI:349–355) | **kein erkennbarer Anker gefunden** | **möglicher Fund:** diese ältere, bereits gebaute Hinweiszeile ist von der neueren, noch offenen Hinweiszeile aus Konzept § 2.13 (3) zu unterscheiden (die zweite ersetzt die erste, ist aber selbst noch nicht gebaut) — im Wiki ist damit vermutlich **keine** von beiden beschrieben |
| W5‑B‑11 G8 Bandbreitentabelle „ΔKW…", Spalte „Einstufung" | umgesetzt | wie G7 — kein sicher zuordenbarer Anker | ungeprüft, siehe „Nicht geprüft" |
| W5‑B‑12 p_I-Feld, Freitext „Nicht monetäre Wirkungen" | umgesetzt | Freitext: W.wiki `nicht-monetaer`(24) **bestätigt vorhanden**; p_I-Feld ohne erkennbaren eigenen Anker | Freitext beschrieben, p_I-Dialogfeld wahrscheinlich Lücke |
| § 11 Entscheide 18.09.2026 (V-4, K-8/V-1, V-G10) | **ausdrücklich nicht umgesetzt** (VALERI:551) | — | zu Recht kein Anker; deckt sich mit § 2.11.4–2.11.7 |

## 3 Wiki-Pflegeplan je Umsetzungsetappe

### 3.1 Mockup-Anhang „Umsetzungsstand" — offene U-Zeilen (Mockup:4344–4746)

39 Zeilen U1–U39, davon 19 ohne Klasse `gestrichen` = offen: **U1, U2, U3, U4, U5, U6, U7, U9, U10, U11, U12, U13, U14, U15, U22, U25, U27, U32, U39.** Für offene Punkte gilt einheitlich: kein Logbuch-Eintrag vor der Umsetzung (Regel 13.4 setzt eine sichtbare Änderung voraus), Sätze unten sind **Entwürfe auf Vorrat**; Upload frühestens mit dem Sammelpaket, das auf die jeweilige Umsetzung folgt.

| U | Thema (Konzept-Bezug) | Ziel-Seite/Anker (heute/neu) | Wesentlich? — Satzentwurf (Vorrat) |
|---|---|---|---|
| U1 | BHKW-Abwärmeabfuhr-Kennzeichen + Stromkennzahl σ (§ 3.6 Befund K‑1) | W.wiki, neuer Anker `kwk-abwaermeabfuhr` unter Gruppe 1b | wesentlich — „Anlagen mit Abwärmeabfuhr-Vorrichtung tragen ein eigenes Kennzeichen und eine Stromkennzahl für den KWK-Zuschlag." |
| U2 | Umschalter „Kennzahlen / ValERI-Bewertung" | W.wiki, neuer Anker `valeri-umschalter` (oder Erweiterung von `spalten`) | wesentlich — „Die Wirtschaftlichkeitsseite trägt oben einen Umschalter zwischen der Kennzahlansicht und der ValERI-Bewertung." |
| U3 | Verlauf mit drei Szenarien in einem Bild | W.wiki, Erweiterung eines neuen Anker `verlauf` | wesentlich — „Der Verlauf zeigt alle drei Szenarien einer Variante in einem Bild." |
| U4 | Bandbreite nebeneinander (statt ein Szenario je Ansicht) | W.wiki `spalten` erweitern | wesentlich, mit U3 zusammen ein Eintrag |
| U5 | Empfehlungskarten je Version | W.wiki `vorschlag` erweitern | Kleinigkeit (Darstellungsvariante derselben Empfehlung) — kein eigener Eintrag |
| U6 | Erlösrubrik: Anlagenbezug je Zeile, vermiedene Kosten je Anlage | W.wiki `block-a`/`vermiedene-kosten` erweitern | wesentlich — „Die Erlösrubrik gliedert Block A und die vermiedenen Stromkosten zusätzlich nach Anlage." |
| U7 | § 53/53a und § 54 getrennt ausgewiesen | W.wiki `energiekosten-je-anlage` erweitern | Kleinigkeit (reine Aufschlüsselung) |
| U9 | Degradation mit Quellenangabe | W.wiki/P.wiki, neuer Anker an `pv-verguetung` | Kleinigkeit |
| U10 | Hinweistext „Was ein Szenario variiert" (§ 2.11.7) als Ressource | W.wiki `szenarien` erweitern | wesentlich, sobald gebaut — Wortlaut vorab geprüft (Abschnitt 5) |
| U11 | Sammelknopf „Vorschlagswerte übernehmen" (Strompreis Details) | K.wiki `aufschlaege` erweitern | Kleinigkeit |
| U12 | Excel-Formelmappe Stufen 0–3 | W.wiki `bericht` erweitern | wesentlich, je Stufe ein Eintrag — Konzept § 2.11.6 Stufenplan bildet die Etappierung bereits ab |
| U13 | Spaltengruppe je Szenario im Bericht | W.wiki `bericht` erweitern | Kleinigkeit, mit U3 im selben Zug |
| U14 | Referenzprojekt-Randfälle (Schemabeschreibung) | W.wiki `referenz` | rein technisch — kein Eintrag |
| U15 | Vollständige Szenarioabdeckung (§ 2.11.5) | W.wiki `szenarien` erweitern, löst U10 ab | wesentlich — größte Einzeländerung, siehe auch VALERI-Etappe V‑E unten |
| U22 | BHKW-Überlagerung „Sätze und Herkunft" | W.wiki Gruppe-1-Anker erweitern | wesentlich — Neugestaltung des BHKW-Dialogs |
| U25 | BHKW: Staffelzeile + Warnband Deckelanteil | W.wiki `kwk-vorschlag` erweitern | Kleinigkeit |
| U27 | PV-Dialog: aufgeschlüsselte Vorschau (statt einer Zeile) | W.wiki `pv-verguetung` erweitern | wesentlich |
| U32 | Preisbasis-Kennung als Regelkennung (Schemaschritt) | — | rein technisch, kein Wiki-Bezug |
| U39 | Rest von U8: Entkopplung Ersatz/Restwert, Gerätekataloge, Speicherflotte, plattformfreie Hinweiszeile | K.wiki `ersatz-restwert` erweitern | wesentlich, deckungsgleich mit ND-Stufe S3 unten |

### 3.2 Konzept-interner Klärbedarf (eigener Fund, kein U-Punkt)

§ 6.3 Nr. 9c (B7‑3) und Nr. 9g (BK1‑3) beschreiben beide, die KWKG-Pauschale habe „keine Rubrikzeile" bzw. der Jahr‑0-Ausweis sei „nicht gebaut" (Konzept:2274–2277, 2299–2301). Das widerspricht dem als **erledigt** geführten U17 (Mockup:4460–4474: „Block A trägt … die Zeile ‚KWKG-Pauschale (§ 9 KWKG)'"). Für den Wiki-Pflegeplan heißt das: **U17 ist real umgesetzt und muss in W.wiki `block-a` nachgezogen werden** (deckt sich mit dem in Abschnitt 2.1 genannten U23-Fund); die zwei §6.3-Punkte sind vermutlich nur nicht als erledigt nachgetragen — das ist ein Befund für die Konzeptpflege des Anwenders, keine Wiki-Aufgabe.

### 3.3 VALERI-Etappen V‑C bis V‑E (Konzept:682–684)

| Etappe | Inhalt | Wiki-Ziel bei Umsetzung | Logbuch |
|---|---|---|---|
| V‑C | ValERI-Ansicht (fünf Blöcke + Cashflow-Chart) | neuer Abschnitt in W.wiki, neue Anker je Block (`valeri-investition`, `valeri-betrieb`, `valeri-erloese`, `valeri-energie`, `valeri-nutzungsdauer`) | wesentlich, ein Satz je Block oder gebündelt |
| V‑D | XLSX-Formelbericht + Anhang-E-Checkliste, Gegenprobe Anhang D | W.wiki `bericht` erweitern | wesentlich |
| V‑E | Vollständige Szenarioabdeckung (Trägerpreise, Erlössätze, Mengenfaktor) | W.wiki `szenarien` erweitern, löst U10/U15 ab | wesentlich |

Upload für alle drei: frühestens das Sammelpaket nach der jeweiligen Umsetzung, nicht vor 28.09.2026.

### 3.4 Nutzungsdauer-Stufe S3 (ND:227, 263)

Instandsetzung/Wartung je Technik, Vorbelegung der Gerätekataloge — eigener Entscheid steht aus (ND:229). Ziel bei Umsetzung: K.wiki `nutzungsdauern|afa` erweitern (Spalten `Instandsetzung_Prozent`/`Wartung_Prozent` sichtbar machen). Kein Logbuch-Eintrag, solange kein Entscheid vorliegt.

### 3.5 Hausstil-Dialoge (Konzept § 2.7)

Reine Gestaltungsrichtlinie für Entwickler (Kopfband, Farben, Knöpfe) ohne eigenen Anwenderinhalt — **kein Wiki-Bezug vorzusehen**, auch nicht bei Umsetzung neuer Dialoge im selben Stil.

## 4 Hilfe-Zuordnung

Die zwölf von 05/§6 geprüften Dialoge werden hier **nicht wiederholt** (Befund dort: BhkwWirtschaftlichkeit ohne Ziel, PhotovoltaikVerguetung zu grob, acht weitere mit bereitem, aber unverdrahtetem Anker). Ergänzt werden die in dieser Prüfung zusätzlich gefundenen Kosten-/Katalogdialoge aus help_mapping (Zeilen 240–336):

| Dialog (Schlüssel) | Heutiges Ziel | Beleg | Ankergenauer Vorschlag | Befund |
|---|---|---|---|---|
| `Form_Tarifstruktur.btn_Help` | `Kosten` | help_mapping:259 | **`Wirtschaftlichkeit#strombezug`** | **Fehlzuordnung, neuer Fund:** Der Dialog „Tarifstruktur…"/„Strombezug…" ist inhaltlich in W.wiki beschrieben (`strombezug`, W.wiki:33 — Bezugspreise Hoch-/Niedertarif, Leistungspreisstaffel), nicht in K.wiki |
| `Form_Gesetzesparameter.btn_Help` | `Gesetzesparameter` | help_mapping:334 | ungeklärt — **keine Repo-Quelle** unter `Projekte/Wiki/` | wie 05/§6: nicht prüfbar ohne Netzzugriff; verstößt zugleich gegen die Hausregel „jede Bedienungsseite bekommt eine Quelle" (Hilfesystem:855–858), falls die Seite live existiert |
| `Form_CaseEingabe.btn_Help` (Worst/Best-Eingabe) | `Kosten` | help_mapping:240 | Seitenebene bleibt sinnvoll (Dialog wird aus mehreren Kontexten geöffnet — Kosten **und** Wirtschaftlichkeit-Parameter); kein Einzelanker möglich, ohne den Dialog kontextabhängig zu parametrieren | kein Mangel, nur Grenze des Formats notiert |
| `Form_KostenAdmin.btn_Help` | `Kosten` | help_mapping:242 | Seitenebene passt (Administrationseinstieg) | kein Mangel |
| `Form_Kostenprofil.btn_Help` | `Kosten` | help_mapping:244 | Seitenebene, Anker unklar ohne Quellenlesung des Dialogs | ungeprüft |
| `Form_Kosten_Auswahl.btn_Help` | `Kosten` | help_mapping:250 | Seitenebene passt (Auswahldialog) | kein Mangel |
| `Form_VorlagenPosition.btn_Help` (Zeileneditor der Vorlagenposition) | `Kosten` | help_mapping:254 | Kandidat `Kosten#nutzungsdauer-standard` (Positionsart-Klappliste laut ND:134–138 sitzt in genau diesem Editor) | plausibler, aber ungeprüfter Anker-Vorschlag |
| `Form_LeistungspreisReihe.btn_Help` | `Kosten` | help_mapping:255 | Kandidat `Kosten#aufschlaege` (Leistungspreis-Staffel gehört zum Block „Strompreis Details") | ungeprüft |
| `Form_SpotpreisImport.btn_Help` | `Kosten` | help_mapping:256 | Kandidat `Kosten#preishistorie` | ungeprüft |
| `Form_VorlagenUebernahme.btn_Help` | `Kosten` | help_mapping:257 | `Kosten#uebernahme-vorlage` (K.wiki:15) | Anker existiert bereits, nur nicht verdrahtet — deckungsgleich mit 05/§6-Befund „acht Dialoge mit bereitem Anker" |
| `Form_KatalogDubletten.btn_Help` | `Katalogpflege` | help_mapping:335 | außerhalb des engeren Wirtschaftlichkeitsbezugs (allgemeiner Katalogwächter) | nicht weiter vertieft |

**Vorschlag für den nächsten Sammelauftrag (H2/A4 aus dem Hilfesystem-Konzept):** die neun Anker-Ergänzungen aus 05/§6 plus die Korrektur `Form_Tarifstruktur` → `Wirtschaftlichkeit#strombezug` in einem Zug — reine Konfigurationsänderung an help_mapping.txt, kein Code-Umbau (Hilfesystem:606–608).

## 5 Tabuwort- und Produktdatenprüfung der wiki-bestimmten Konzepttexte

Regex (CLAUDE.md:262) angewandt auf die drei vom Konzept ausdrücklich als künftige Wiki-/Ressourcentexte markierten Stellen (case-sensitiv, wie in 05/§5.1 gehandhabt):

| Textstelle | Beleg | Regex-Treffer | Bewertung |
|---|---|---|---|
| § 2.11.7 Hinweistext „Was ein Szenario heute variiert — und was nicht." | Konzept:809–817 | **0 harte Treffer.** Ein weicher Grenzfall: „…kommen nach dieser Darstellung; **bis dahin** steht dieser Hinweis…" — kleingeschriebenes „bis dahin" trifft das großgeschriebene Musterglied „Bis dahin" bei case-sensitiver Prüfung nicht | inhaltlich zulässig (beschreibt den *jetzigen* Programmzustand, nicht eine vergangene Änderung); die Formulierung „kommen nach dieser Darstellung" ist Meta-Kommentar zur Roadmap und sollte beim Einpflegen gestrichen werden, auch ohne Regelverstoß |
| § 2.13 (3) Hinweiszeile „Betrachtungszeitraum {T} a über der Vorgabe {n} a …" | Konzept:930–932 | 0 Treffer | unauffällig, direkt wiki-tauglich (identisch auch in ND:162–166) |
| § 2.14 Zeilentexte „{0} — ohne Anlagenzuordnung" / „{0} — Erfassungsgruppe (ohne Anlage)" | Konzept:1026–1027 | 0 Treffer | unauffällig — deckt sich mit dem bereits veröffentlichten K.wiki `erfassungsgruppen` |

Produktdatenprüfung (Regel 13.2): Keine der drei Stellen nennt Hersteller, Typcode oder Kennwerte — Regel 13.2 betrifft hier nicht.

### 5.1 „Höfingen" — reales Kundenprojekt?

Befund, mit Beleg:

- Der Anhang „Begleitende Artifacts" des Konzepts führt ein Artifact „ValERI-Bewertung Höfingen" ausdrücklich mit **„reale Höfingen-Zahlen"** (Konzept:51).
- § 2.9 nennt die Quelle konkret: „Die Altanwendung (**Höfingen-Mappe**, `Tab_kurz_KWKG2020`) … Investition 9.624 €, Betriebskosten 518 €/a, Brennstoff 11.498 €/a" (Konzept:554–557).
- § 2.11 nennt die Datei wörtlich: „die Altmappe `Quellen\BHKWPlan\BHKW_Höfingen_Erneuerung_20kWel.XLS`" (Konzept:613) — **diese Datei liegt tatsächlich im Repository** (`Quellen/BHKWPlan/BHKW_Höfingen_Erneuerung_20kWel.XLS`, 1 205 248 Byte, neben weiteren Altmappen desselben Werkzeugs „BHKW-Plan").
- § 2.12 und Rechenweg 08 verwenden die Zahlen als Gegenprobe: „Höfingen: Näherung 65.073 €, jahresscharf 65.259 €" (Konzept:857), zusätzlich IZF 20,4 %, Amortisation 4,33 a (Konzept:666).

**Einschätzung:** Die Kombination aus Ortsname, einer eigens benannten Excel-Datei eines Vorgänger-Werkzeugs und nicht-runden, mehrstelligen Euro-Beträgen spricht stark dafür, dass „Höfingen" ein reales Altprojekt bezeichnet (Höfingen ist ein bekannter Ortsteil/Ortsname, keine erkennbare Kunstbezeichnung). Regel 13.2 verbietet wörtlich nur Hersteller- und Produktdaten, nicht Orts- oder Projektnamen — trifft den Fall also nicht direkt. Der Sache nach gilt aber dieselbe Begründung wie bei 13.2 ("Wie Beispiele geschrieben werden": neutrale Namen, runde Werte) und die Grundregel 13.1 (kein Bezug auf einen konkreten, identifizierbaren Fall außerhalb des Programms): **Empfehlung, dem Anwender vorzulegen — vor jeder Wiki-Verwendung „Höfingen" durch einen neutralen Namen ersetzen (z. B. „Beispielprojekt B", passend zum bereits neutralen `Beispielprojekt.md`) und die Beträge wie bei Regel 13.2 runden**, auch wenn kein Wächter dafür greift (der Produktdaten-Wächter prüft nur Hersteller-/Typnamen, keine Orts- oder Projektnamen).

## 6 Konzept Hilfesystem selbst

### 6.1 Fehlt eine Bedienungsseite, die das Wirtschaftlichkeitskonzept braucht?

Die Bedienungsseiten-Tabelle (Hilfesystem:879–893) führt zehn Titel (Klimadaten fehlt dort bereits laut 05/§3, unabhängig von dieser Aufgabe). Geprüft gegen die drei vorgeschlagenen Kandidaten:

- **„Nutzungsdauern"** — braucht **keine eigene Seite**: Der Admin-Dialog ist als Abschnitt in K.wiki verankert (`nutzungsdauern|afa`, K.wiki:129) und dem Muster anderer Admin-Unterdialoge gleich (z. B. Energieträgerverwaltung als Abschnitt derselben Seite statt eigener Seite).
- **„Gesetzesparameter"** — **echte Lücke.** help_mapping.txt verweist mit `Form_Gesetzesparameter.btn_Help = Gesetzesparameter` (help_mapping:334) auf einen eigenständigen Seitentitel, für den es weder in der Bedienungsseiten-Tabelle noch unter `Projekte/Wiki/` eine Repo-Quelle gibt (deckungsgleich mit 05/§6, dort als „nicht prüfbar" markiert — hier zusätzlich als **fehlender Tabelleneintrag** benannt). Empfehlung: entweder die Seite mit Repo-Quelle anlegen (Hausregel Hilfesystem:855–858) oder das Ziel in help_mapping auf einen bestehenden Anker umbiegen (Kandidat: neuer Abschnitt in K.wiki, da der Gesetzeskatalog dort administrativ neben Nutzungsdauern und Energieträgern sitzen würde).
- **„Energieträger"** — bleibt sinnvoll als **Abschnitt** von K.wiki (`energietraegerverwaltung|energietraeger`, K.wiki:68), belegt aber mit rund 60 der 168 Zeilen bereits über ein Drittel der Seite; bei weiterem Wachstum (z. B. durch V‑C/V‑D) wäre eine Auslagerung in eine eigene Seite eine spätere, keine akute Empfehlung.

### 6.2 Regel für Mockup-Beispiele ergänzen

„Mockup" kommt im gesamten Konzept Hilfesystem kein einziges Mal vor (eigener Grep, 0 Treffer) — Regel 13.2 nennt als Geltungsbereich ausdrücklich nur `EPOS.Kern/Allgemein/Hilfe/Berechnung/*.wiki` und `Projekte/Wiki/*.wiki` (Hilfesystem:791). 05/§5.2 hat dazu bereits die Tatsachen geliefert (drei von sechs Mockups mit realen Hersteller-/Typdaten, `Katalogfilter_Vorschlag.html` am dichtesten). Empfehlung an dieser Stelle, konkret formuliert: **Abschnitt 13.2 um einen Satz „Dieselbe Regel gilt für Beispieldaten, die aus `Dokumentation/aktuell/Mockups/*.html` in eine `*.wiki`-Datei übernommen werden — vor der Übernahme neutralisieren, der Wächter prüft Mockups nicht" ergänzen** — das schließt exakt die Lücke, die 05 unter „Einordnung" bereits benannt, aber noch nicht als Regeltext vorgeschlagen hatte, und passt zur selben Systematik wie die Höfingen-Frage in Abschnitt 5.1 dieses Berichts (Orts-/Projektnamen sind von 13.2 ebenfalls nicht wörtlich erfasst, gehören der Sache nach aber dazu).

## Nicht geprüft

- Vollständiger Wortlaut aller in Abschnitt 2 genannten Anker (`parameter`, `nachweis`, `kohaerenz`, `vorschlag`, `traeger-je-anlage` u. a.) — nur Namen und Position geprüft, nicht der Inhalt gegen den Konzepttext gelesen; wo „vermutlich beschrieben" steht, ist das eine Wahrscheinlichkeitsaussage aus dem Ankernamen, kein Textvergleich.
- „Letzter Statusbezug" für Stromspeicher.wiki, Photovoltaik.wiki, Emissionen.wiki, Simulationsergebnisse.wiki beruht auf einem Abgleich der Themen-Kurztitel aller Statuszeilen (Status:97–290), nicht auf vollständigem Lesen jeder der rund 200 dichten Auftragszeilen — ein neuerer, thematisch nicht offensichtlicher Treffer kann übersehen sein.
- Die Katalognamen der Testdatenbank (`Referenzlaeufe/Kenndaten_Test.sqlite`) wurden nicht geprüft (kein Datenbankzugriff, wie schon 05/§2 einschränkt).
- Ob „Höfingen" tatsächlich ein reales Kundenprojekt von Inekon ist, wurde nicht extern verifiziert — nur die interne Beleglage (Dateiname, „reale Zahlen", Nicht-Rundheit der Beträge) geprüft; das ist eine begründete Vermutung, keine Bestätigung.
- Die neu vorgeschlagenen Anker für `Form_VorlagenPosition`, `Form_LeistungspreisReihe`, `Form_SpotpreisImport`, `Form_Kostenprofil` (Abschnitt 4) sind Kandidaten aus Namen und Kommentaren von help_mapping.txt, nicht aus einer Lesung der zugehörigen Razor-Dialoge.
- G7/G8-Hinweise des VALERI-Konzepts (Abschnitt 2.4) wurden nur über die Anker-Namensliste geprüft; ein eigener, noch unbenannter Anker in W.wiki, der sie doch abdeckt, ist nicht auszuschließen.
- Live-Wiki (`wiki.epos-plan.de`) nicht aufgerufen — Auftrag ist rein Repo-basiert; letzter bekannter Live-Stand laut Revisionslog ist der 13.09.2026 (Hilfesystem:937).
- `Form_Gesetzesparameter`-Zielseite: ob sie live im Wiki existiert, bleibt wie in 05/§6 ungeprüft (kein Netzzugriff).

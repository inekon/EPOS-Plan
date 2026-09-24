# Prüfung der Mockups zur Wirtschaftlichkeit — Konsistenz, Darstellung, Berechnung, Einheitlichkeit

**Stand 23.09.2026** (Prüfung vom 19.09.2026, seither fortgeschrieben) · Codestand `41764ab0` ·
Prüfgegenstand: die sechs HTML-Mockups unter `../Mockups/` und die
einschlägigen Papiere dieses Ordners · Schemastand 94 zur Prüfzeit; heute steht
`SchemaStand.Zielversion` auf **113** (90–113 vergeben), **nächster freier Schritt 114** ·
Prüfprotokolle mit allen Einzelbefunden:
[`ueberholt/Protokolle/Reporting/Pruefung_Mockups_2026-09-19/`](../../ueberholt/Protokolle/Reporting/Pruefung_Mockups_2026-09-19/)

> **Dieses Papier ändert nichts am Code und nichts an den Mockups.** Es ist ein Befund mit
> Änderungsplan: § 2 sagt, was gefunden wurde, § 3 sagt **wo welche Änderung** vorzunehmen ist,
> § 4 nennt die Entscheide, die vorher beim Anwender liegen, § 5 die vorgeschlagene Reihenfolge.
> Jeder Befund trägt in der Spalte *Quelle* die Kennung des Prüfprotokolls (`00` Sichtprüfung,
> `01` Nachrechnung, `02` Papierabgleich, `03` Code-Abgleich, `04` Einheitlichkeit, `05` Wiki,
> `06` HTML-Struktur), dort steht er mit Zeilennummern und Messung.

> **Entscheidungsregister.** Die geltende Fassung aller Entscheide führt das
> [Entscheidungsregister](Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md) (Q1–Q25 unter R‑Q);
> § 4 bleibt als Teil dieser Prüfung mit ihrem Datum stehen.

## 0 Das Ergebnis in sechs Sätzen

1. **Die Zahlen des konsolidierten Mockups sind valide.** Rund 180 Größen wurden nachgerechnet,
   der Kennzahlenblock der Kategorie 8 mit dem `KapitalwertRechner` ohne Datenbank bis auf ±2 €
   getroffen, und alle 235 Beträge aus Beispielprojekt, Rechenwegen und Konzept stehen
   deckungsgleich im Mockup; die neun Abweichungen sind Rundungsfolgen unter 3 €.
2. **Die eine schwere fachliche Lücke ist die doppelte CO₂-Bepreisung des Beispiels:** Der
   Erdgas-Arbeitspreis 0,7560 €/m³ enthält den CO₂-Bestandteil, daneben steht eine BEHG-Reihe von
   56.699,50 €/a; Rechenweg 04 verlangt eine Kohärenzzeile dafür, die es im Kern nicht gibt.
3. **Die Papiere hinken dem Mockup und dem Code hinterher:** Das zweite Mockup trägt in 22 Zeilen
   die Zahlen von vor der PV-Neurechnung, das konsolidierte Konzept nennt in der Kopfzeile
   Zielversion 61, beschreibt den gebauten BHKW-Dialog als „neu" und führt Gebautes (Knopf
   „Nutzungsdauern vorbelegen", Positionsart) als offen; drei Papiere nennen „Schemaschritt 92"
   als nächsten freien, tatsächlich ist es 97 (zur Prüfzeit 95; die Schritte 95 und 96 haben am Abend
   KL‑3 und FK‑2 belegt).
4. **Darstellung:** Die Seite ist bei 1.440 px Fensterbreite 185 px zu breit — eine einzige
   Rasterregel schneidet die rechten Spalten aller Dialograhmen ab —, sie ist 69.438 px lang bei
   flacher Navigation, und Fenster, Seitentafeln und Überlagerungen tragen denselben Rahmenstil;
   die HTML-Struktur selbst ist wohlgeformt.
5. **Felder und Aktionen:** Alle 369 als vorhanden geführten Ressourcenschlüssel stehen in
   beiden Sprachen; gegen den Code bleiben 97 Abweichungen, zehn davon schwer — darunter ein im
   PV-Reiter nie gezeichneter Knopf „Gesetzesparameter…", eine bedienbare, aber wirkungslose
   Klappliste in der Neuzeile, die Erlösrubrik ohne Komponentengliederung (U6) und die Semantik
   von Gedankenstrich und Null in den Ergebniszellen.
6. **Einheitlichkeit:** 21 Dialoge bauen die Fußleiste auf sieben Arten, vier ohne Abbrechen,
   der Dialogkopf in vier Bauarten, kein Wirtschaftlichkeitsdialog trägt die Kontextzeile
   „Projekt · netto", kein Kostenkatalog ein Suchfeld, und der Sprungknopf „Tarif…" des
   PV-Vergütungsdialogs verwirft Eingaben still; die Mockups selbst zeigen fünf verschiedene
   Fußleisten. Im Wiki sind 34 von 39 Bedienstücken beschrieben, fünf fehlen, die Hilfe-Taste des
   BHKW-Dialogs läuft ins Leere, und drei Mockups zeigen reale Herstellerdaten.

## 1 Prüfgegenstand und Methode

| Prüfpunkt | Womit verglichen | Werkzeug und Modell | Protokoll |
|---|---|---|---|
| Sichtprüfung Darstellung | 32 Bildschirmfotos bei 1.440 px (Edge im Hintergrundmodus), Messung von Seitenhöhe, -breite und Rasterregeln im eingebetteten Browser | Orchestrierung (Fable 5.1) | `00` |
| 3 Valide Berechnung | jede Zahl der Zahlenproben gegen Beispielprojekt, Konzept § 3, Rechenwege 01–08 und — wo rein aufrufbar — gegen die Kernrechner (`KapitalwertRechner`, `KwkgSatzRechner`, `EegSatzRechner`, `PvErloesRechner`, `SteuerGutschriftRechner`, `HilfsstromRechner` u. a.) über ein dotnet-Dateiskript | Opus 5 | `01` |
| 1 Konsistenz | beide Wirtschaftlichkeits-Mockups gegen Konzept, LIESMICH, Beispielprojekt, Rechenwege, Nutzungsdauer-, VALERI- und Grundlagenpapier, Statuszeilen #343–#366 und Index; die vier übrigen Mockups gegen ihre Konzepte; Anker, Links, Wächtermuster | Opus 5 | `02` |
| 3 Eingabefelder, Ausgabefelder, Aktionen | jedes gezeichnete Element gegen die Razor-Komponenten, Hüllen, Kern-Controller und Ressourcen (de/en); Marker U1–U40 gegen den Codestand | Opus 5 | `03` |
| 4 Einheitliche Dialoge | 21 Dialoge des Administrationsmenüs und der Wirtschaftlichkeit als Merkmalsmatrix (Kopf, Fußleiste, Schreibweg, Rückfragen, Raster, Filter, Hülle) gegen Hausstil § 2.7, `EPOS.UI/CLAUDE.md` und die Mockups | Opus 5 | `04` |
| Wiki-Folgen | Bedienstücke gegen `Projekte/Wiki/*.wiki`, Tabuwörter- und Produktdatenregel, Hilfe-Zuordnung, Logbuch nach Konzept Hilfesystem § 13 | Sonnet 5 | `05` |
| 2 Struktur und Lesbarkeit | Parse-Fehler, Ids, Überschriften, Navigation, Tabellen, SVG, Farben, Schriftgrößen, Responsivität aller sechs Mockups per HtmlAgilityPack | Sonnet 5 | `06` |

Nichts im Repository wurde verändert; gebaut wurde allein `EPOS.Kern` für das Nachrechenskript.
**Nicht geprüft:** das Laufzeitverhalten der Anwendung, die Katalognamen der Testdatenbank gegen die
Mockups, die veröffentlichten Artifacts (nur genannt) und die englische Ressourcenfassung Wort für Wort.

## 2 Befunde je Prüfpunkt

### 2.1 Konsistenz

**Zahlen.** Im konsolidierten Strang (Beispielprojekt ↔ `../Mockups/Dialog_Formel_Zahlenprobe.html`
↔ Rechenwege ↔ Konzept § 2.12) keine Abweichung. `../Mockups/Ergebnis_Bandbreite_Herkunft.html`
ist eine zweite Wahrheit mit alten Zahlen: Kapitalwertdifferenz V3 1.877.827 statt 1.842.695 €,
V2 217.622 statt 182.491 €, PV-Vergütung 10.396,34 statt 9.001,44 €, Reihe 199.545/148.931 statt
150.118/113.800 €, Bandbreiten, Annuitäten, Amortisationen, Zinsfüße, Gestehungskosten, Erlöse
(22 Stellen, `02/a‑1…a‑14`). Fünf Textabschnitte stehen wortgleich in beiden Dateien (`02/e‑7`),
und die Vokabel „neu" bedeutet in beiden etwas anderes (`02/e‑1`).

**Stände und Nummern** (`02/b`, `02/f`): Konzept-Kopfzeile „Stand 02.09.2026 · ZIEL_VERSION = 61 ·
Schemaschritt 62 vergeben" ist überholt (Zielversion 96, nächster freier 97); § 3.6 Befund K‑1 und
Mockup-Anhang U1/U32 nennen „92" als nächsten freien Schritt; § 6.3 Nr. 9b „Zielversion bleibt 89";
Konzept Nutzungsdauer „Zielversion 74". Die elf Indexzeilen des Ordners tragen 2026‑09‑02, die
Dateien sind vom 18./19.09. Konzept Katalogfilter, Wechselrichter und PV-Ertragsmodell behaupten in
ihren Kopfblöcken offene Stufen, die ihre eigenen Kapitel als umgesetzt führen (`02/f‑6…f‑8`).

**Umsetzungsstand** (`02/c`): Der Anhang des Mockups (39 Zeilen, 20 erledigt, 19 offen) stimmt mit
dem Codestand überein (`03/§8`), aber: Marker „umgesetzt U40" ohne Anhangzeile; zwei Schlüssel
„geplant" ohne U‑Nummer (`WIRT_KWKG_KONTINGENT_LEER` ist im Kern längst gebaut, `03/§6.3`); Konzept
§ 2.13 (3) und § 6.3 Nr. 9h nennen den mit #357 gebauten Knopf „Nutzungsdauern vorbelegen" und die
Positionsart als nicht gebaut; § 6.3 B5‑Kernaufgaben 1 und 2 sind erledigt; § 6.1 führt weder VV
(Schritt 93) noch die Hilfsstrom-Umstellung (Schritt 94). Fünf gezeichnete Stücke haben **keine**
Anhangzeile, obwohl sie nicht gebaut sind: Brückenbild, Zahlungsstrombild, „Anhang‑E‑Checkliste…",
„Bericht erzeugen", Fußzeile der Schnellwahl (`03/§8`).

**Begriffe und Bedienelemente** (`02/d`): § 2.2 beschreibt `Form_BhkwWirtschaftlichkeit` als „neu
(BW9)" mit sechs Gruppen und „Vorschlag am Feld, nicht als Sammelknopf" — das Mockup zeichnet acht
Gruppen und die Überlagerung „Sätze und Herkunft" (U22), der Code hat drei Vorschlagsknöpfe am Feld.
§ 2.3 (PV-Dialog zweispaltig, ohne Degradation), § 2.4 (vier Gruppen ohne Vergleichsprojekt und
Szenarien), § 2.7 („sieben Knöpfe", gebaut fünf), § 2.8/§ 2.12/LIESMICH (Spaltenliste, Reiter,
Knopfliste des Kostendialogs) beschreiben den WinForms-Stand statt des Razor-Dialogs. Der Wortlaut des
Szenario-Hinweises (§ 2.11.7) weicht vom Mockup ab. Die Mockup-Ressourcentafel nennt die Klappliste
noch „Stammprojekt:" (VV‑Q7 entschied „Projekt:"), der Fließtext zur Übernahme den Stand vor #363.

**Die vier übrigen Mockups** (`02/j`): Katalogfilter (S1–S3), Wechselrichter (S1–S3 samt Nachtrag)
und Stromspeicher-Dialoge (P1–P9) sind vollständig umgesetzt — die Mockups beschreiben keinen offenen
Zustand mehr und gehören mit ihren Konzepten nach `ueberholt/`; das Wechselrichter-Mockup behauptet
sichtbar „Solange Stufe S3 aussteht, rechnet der Kern die Stränge nicht". Der Hydraulik-Entwurf vom
15.08.2026 hat kein Konzept und keine Indexzeile. `Katalogfilter_Vorschlag.html` ist in
`EPOS.Kern.Tests/DokumentationLinkWacheTests` als Existenzprobe eingetragen — ein Umzug ohne
Nachzug macht den Test rot (`02/g‑7`).

**Verweise** (`02/g`): Alle 21 internen Anker und alle aus den Papieren referenzierten Mockup-Anker
treffen. Der Wegweiser `LIESMICH.md` nennt unter „Verwandte Dokumente" zwei Pfade, die nach
`ueberholt/` gewandert sind. Das konsolidierte Mockup lädt als einziges eine Schrift von außen
(Google Fonts); das Artifact des Mockups trägt den Stand vor #351.

### 2.2 Übersichtliche Darstellung und Bedienung

| Nr | Befund | Quelle |
|---|---|---|
| D1 | **Breitenüberlauf:** `.rumpf { grid-template-columns: 200px 1fr }` — die Hauptspalte hat keine Untergrenze null, Tabellenzellen mit `white-space: nowrap` (`table.tafel td.r`, `td.formelzelle`, `.f-raster th`, `td.feld`) drücken sie auf 1.227 px; die Seite wird 1.625 px breit. Folge bei 1.440 px: Spalten Nutzungsdauer/Worst/Best, Fußleistenknöpfe, rechte Tafelspalten und die zweite Ergebnisseiten-Ansicht sind abgeschnitten; bei 800 px erscheint ein Rollbalken. Mit `minmax(0, 1fr)` und rollbaren Tafeln fällt die Breite auf 1.425 px, vier Tafeln rollen innen (im Browser geprüft) | `00/S1`, Messung |
| D2 | **Länge und Navigation:** 69.438 px Seitenhöhe (≈ 70 Bildschirme), Kategorie 5 und 8 je 11–12 Bildschirme; die Navigation führt elf Einträge ohne Unterpunkte, kein „nach oben" | `00/S2`, `06/§3` |
| D3 | **Rahmenstile:** Fenster (Titelleiste mit ⓘ/×, Fußleiste), Seitentafeln („Erlöse und Vorteile", „Bandbreite", „Annahmen", „Gliederung") und Überlagerungen tragen dasselbe dunkle Kopfband — ein Fenster ist von einem Seitenabschnitt nicht zu unterscheiden; ⓘ/× fehlen bei Energieträger, Parameterdialog und Ergebnisseite | `00/S6`, `00/S7`, `04/B25` |
| D4 | **Fußleisten der gezeichneten Dialoge widersprechen sich:** Kat. 1–4 „Abbrechen · Speichern · OK", Kat. 5 „Abbrechen · Speichern", Kat. 6/8 „Abbrechen · Übernehmen", Zeileneditor „Abbrechen · OK", Reiter Ertrag/Bonus ohne Fußleiste, Ergebnisseite ohne Regel; Katalogfilter „… Löschen · OK" ohne Abbrechen und einmal „OK · Abbrechen" verdreht; Wechselrichter dreimal verdreht | `00/S8`, `04/B24`, `04/B26` |
| D5 | Lange, kleingesetzte Erläuterungsabsätze unter den Dialograhmen (Kat. 1, 2, 5) statt Tafeln „Element → Feldart → Verhalten"; Anhang Umsetzungsstand als Tabelle mit Absatz-Zellen ohne Statusspalte; Chip-Legende an drei Stellen verteilt, nirgends vollständig | `00/S3–S5`, `06/#9` |
| D6 | Kat. 8 „Dialog — Parameter" zeigt sechs von 26 Eingaben; die Szenariotafel (3 × 7, Knopf „Vorgaben") fehlt, der Ausschnitt ist nicht als solcher gekennzeichnet | `00/S9`, `03/#93` |
| D7 | Struktur wohlgeformt: 0 Parse-Fehler, 0 doppelte Ids, 0 defekte Anker, 0 von 83 Tabellen mit ungleicher Zellenzahl; Überschriften springen fünfmal von h2 auf h4; Schriftgrößen bis 10 px; Farbnotation Hex und `rgb()` gemischt; Grauton weicht vom „DimGray" des Hausstils ab | `06/#3, #12, #13, #15` |
| D8 | **Zwei Gestaltungsfamilien:** die beiden Wirtschaftlichkeits-Mockups (Kopf mit Kennzeile und Leseweg, Farbregister, IBM Plex Sans) gegen Katalogfilter/Wechselrichter/Stromspeicher (`epos-*`-Klassen, zusätzliche Akzentpalette) und den Hydraulik-Entwurf (eigene Chips, ohne viewport-Meta); vier von sechs Dateien ohne Abschnittsnavigation; eine fest 1.320 px breite SVG im Wechselrichter-Mockup | `06/§5`, `06/#1, #2, #5` |

### 2.3 Valide Berechnung

**Nachrechnung** (`01/§3`): Investitionskaskade, Ersatz und Restwert, Betriebskosten, Energiekosten,
Preisbestandteile, KWKG-Mischsätze und Jahresreihe, Energie- und Stromsteuer, PV-Vergütungsreihe
(Degression, § 51, Marktprämie, Alterung, § 51a), vermiedene Kosten, Erlösrubrik, Kapitalwerte,
Kennzahlen, Bandbreiten, Verlauf, Sicht 2 und die Gegenprobe „Beispielprojekt B" treffen. Rundungsfolgen: die
Brennwertmenge 4.797,2 MWh entsteht aus dem gerundeten Faktor 1,1048 (exakt 4.796,99), daraus
§ 53a 21.203,4 statt 21.202,71 €, § 53 26.384,3 statt 26.383,46 €, § 54 6.370,1 statt 6.369,85 €;
BEHG 56.699,50 statt 56.701,38 €; „energetisch × 0,458 → 12.082" statt 12.079 €; Fall‑2‑Menge
1.651,3 statt 1.651,2 MWh; zwei Erlös-Nominalsummen um je 2 € (`01/B4–B8`).

**Herleitbarkeit** (`01/B1–B3, B11, B13`):

| Nr | Befund | Quelle |
|---|---|---|
| R1 | **CO₂ doppelt:** Arbeitspreis mit CO₂-Bestandteil 0,1371 €/m³ **und** BEHG-Reihe 56.699,50 €/a; die Gliederung der Kategorie 8 führt die BEHG-Reihe nicht (Nominalsumme 11.200.320 = 20 × 560.016), sagt es aber nicht. `KapitalwertRechner.Rechne` würde eine übergebene BEHG-Reihe zusätzlich buchen (−443.981 € auf ΔKW V1 und V3); die in Rechenweg 04 als Behandlung genannte Kohärenzzeile fehlt in `KohaerenzPruefung.cs` | `01/B1`, Stichprobe |
| R2 | „Vermiedene Stromkosten — Leistungsanteil −4.180,0 €" ist aus keiner Eingabe ableitbar: das Beispiel führt weder Bezugsspitze noch Strom-Leistungspreis; Stamm, V1, V2 tragen „keine Bezugsspitze gerechnet" | `01/B2` |
| R3 | Kessel-Entlastung § 54 (2.885,7 €/a) setzt eine anlagenscharfe Steuerwahl am Kessel voraus, die `Beispielprojekt.md` nicht führt | `01/B3` |
| R4 | Wärmegestehungskosten ohne Formel und Bezugsmenge (1.953,9 MWh Nutzwärme) auf der Seite; Netzbezugsmengen 335,5 / 1.344,2 MWh in Kat. 4 ohne Herleitung (sie folgt erst in Kat. 5/7) | `01/B11, B13` |
| R5 | Formelzeile der Trägerkarte „0,76 €/m³ ÷ 10,50 kWh/m³ = 0,0720 €/kWh" rechnet nicht auf: `EnergietraegerPreiskarte.Formel` formatiert den Arbeitspreis mit `N2`, das Ergebnis mit `N4` — im Kern, nicht im Mockup | `01/B9`, Stichprobe |

**Felder und Aktionen gegen den Code** (`03`): 369 Ressourcenschlüssel vorhanden (de/en), 49
„geplant" zu Recht nicht vorhanden; drei Karteileichen — `KDLG_TAB_KOSTEN` und `KDLG_TAB_ERTRAG`
werden nie gelesen, die Reiter des Kostendialogs bleiben in en‑US deutsch; `WIRT_ENK_KOPF` hat keine
Gruppenüberschrift. Die zehn schweren Abweichungen:

| Nr | Befund | Quelle |
|---|---|---|
| F1 | Knopf „Gesetzesparameter…" im Reiter Ertrag/Bonus der Photovoltaik: Rückruf und Text stehen bereit, der Knopf wird nur im BHKW-Zweig gezeichnet — im PV-Blatt nie | `03/#29`, Stichprobe |
| F2 | Neuzeile des Zeilenrasters: die Bemessungs-Klappliste ist bedienbar (`Aktiv="@Schreibbar"`), aber wirkungslos — `PositionNeu` setzt immer `BEMESSUNG_BETRAG`; das Satzfeld daneben ist korrekt gesperrt | `03/#10`, Stichprobe |
| F3 | Erlösrubrik: keine Komponentenblöcke, keine Teilsummen, kein Block „projektweit" (U6); die Zahlenprobe 293.245,6 + 22.914,0 = 316.159,6 €/a ist im Programm nicht nachvollziehbar | `03/#54, #62, #65, #67` |
| F4 | § 53/53a und § 54 kommen als eine Summe `SteuerErgebnis.EnergiesteuerEur` zurück; die getrennten Zeilen bei BHKW und Kessel (U7) fehlen — im Mockup ohne Umsetzungsvermerk | `03/#64`, `01/B12` |
| F5 | Gedankenstrich und Null: ohne Wert zeigt `WirtschaftlichkeitZeilen` „—" ohne Grund, bei Wert 0 „0 — ‹Grund›"; das Mockup verlangt „— ‹Grund›" ohne Wert und eine 0 nur, wo null gerechnet wurde | `03/#63`, Stichprobe |
| F6 | Kennzahltafel: `NETTOBARWERT` steht vor `KAPITALWERT_DIFF`; das Mockup begründet die umgekehrte Ordnung | `03/#71`, Stichprobe |
| F7 | Endenergie-Tafel: Zeichnung sechs Spalten (mit Energieträger, Arbeitspreis), Schlüsseltafel und Code vier | `03/#18` |
| F8 | Kat. 8 auf der Seite nicht gebaut: Bandbreitentafel mit Spanne und Referenzzeile, Gliederung mit Nominalsummen und Differenzspalte, Annahmentafel (Spaltenfolge Ungünstig/Erwartet/Günstig; heute „Best/Worst"), Verlauf mit drei Szenarien, Umschalter, Empfehlungskarten, Spannen-, Brücken- und Zahlungsstrombild — U2–U5, U13; Brücke und Zahlungsstrom ohne U‑Nummer | `03/#72–#85` |
| F9 | Sieben gezeichnete Aktionen ohne Code („Sätze und Herkunft…", „Wahl und Herkunft…", „Übernehmen" der Überlagerung, „Verlauf nach Excel…", „Anhang‑E‑Checkliste…", „Bericht erzeugen", „Schließen" der Schnellwahl); die letzten drei ohne U‑Nummer. Die Ergebnisseite trägt im Mockup zwei verschiedene Fußleisten | `03/§5`, `03/#86` |
| F10 | Prosa der Kategorie 5 zählt „sechs Wahlfelder" und „elf editierbare Felder" — es sind acht Klapplisten und neun Zahlen- plus vier Datumsfelder; die Zahlen sind zugleich die Abnahmekriterien von U22 | `03/#44, #45` |

Dazu 43 mittlere und 44 geringe Abweichungen im Wortlaut der Herleitungszeilen, in Reihenfolgen und
in fehlenden Nebenelementen (Positionsart im Zeileneditor, Worst/Best-Felder, neunter Grund
`BASISGRUND_STROMPREIS`, Nachweisumschlag Fassung 3 statt 1, Schnellwahl drei statt fünf Spalten,
Kohärenzfälle) — vollständig in `03/§4`.

### 2.4 Einheitliche Dialoge

Gemessen an 21 Dialogen (`04/§3`): gleich sind Kopfbaustein, Esc-Regel, Meldungsweg und die
Ressourcenpflicht (0 nackte Literale). Ungleich sind:

| Nr | Befund | Quelle |
|---|---|---|
| E1 | **Fußleiste in sieben Bauarten:** nur 10 Dialoge nehmen die `SpeichernLeiste`, 11 bauen eine eigene Knopfzeile; vier führen kein Abbrechen (Kostenfaktoren, Kapitalwertverlauf, Gesetzeskatalog, Katalog-Dubletten); der OK-Weg heißt „OK", „Speichern" oder „Übernehmen"; sieben Schlüssel für „Abbrechen", vier für „OK"; `VorlagenUebernahmeDialog` stellt OK links | `04/B01–B05` |
| E2 | **Dialogkopf in vier Bauarten** (`TitelAnzeigen`, leerer `TitelText`, fest gezeichnet, gar keiner); die drei fest gezeichneten Köpfe (Nutzungsdauern, Einstellungen, Katalog-Dubletten) sind nicht einbettbar; `GesetzeskatalogZeileDialog` ohne `InfoKnopf`; `Dialogname` nur bei zwei von 21 gesetzt; eingebettete Überlagerungen verlieren den Langtitel („Kostenverwaltung BHKW 1 — Musterprojekt" wird „Kostenverwaltung"); beim Öffnen aus dem Menü steht der Titel im Fenster **und** als `h1` | `04/B06–B08, B12, B23` |
| E3 | **Kontextzeile „Projekt · Variante · netto"** fehlt in allen sechs Wirtschaftlichkeits- und beiden Admin-Dialogen; die Netto-Aussage ist im Kostendialog ein wegklickbares Banner, in der Trägerkarte Kopfkontext, sonst nirgends | `04/B09, B10` |
| E4 | **Hausstil § 2.7 gegen Stilblatt:** kein dunkles Kopfband `#0F1F3D` (die Farbe trägt nur die Schrift), zwei Rottöne statt Firebrick, Knopfmaß 88 × 44 statt 110 × 30 (44 px ist die stärkere Regel), Nachkommastellen ohne Regel (fünf Stellenzahlen, 15 Dialoge ohne Angabe) | `04/B13–B15, B28` |
| E5 | **Sprungknopf „Tarif…" im PV-Vergütungsdialog** schließt über `Schliessen(PvSprung.Tarif)` ohne zu schreiben; der Hinweistext rät „bitte vorher übernehmen", was den Dialog schlösse; der BHKW-Dialog nimmt für beide Sprünge den OK-Weg | `04/B11`, Stichprobe |
| E6 | Rückfragen: vier Löschwege ohne `Rueckfrage` (Kostenfaktoren, Leistungspreisreihe, Emissionskatalog ×2); Kontextwechsel im Kostendialog verwirft still (bekannt aus „Nach #263"); Warnstufe für dieselbe Lage mal amber, mal rot | `04/B16–B18` |
| E7 | Listen in vier Bauarten (Zeilenraster, QuickGrid, HTML-Tabelle, Baum); **kein Suchfeld, kein Spaltenfilter** in den vier Kostenkatalogen (Emissionsarten, Kostenfaktoren, Gesetzesparameter, Nutzungsdauern) — der Katalogfilter-Entwurf gilt dort nirgends; Nachtrag vom Abend: der Gesetzeskatalog nimmt seit #372 die `Katalogliste`, offen bleiben die drei übrigen | `04/B19, B20` |
| E8 | 15 von 21 Datenseiten liegen nur in der Windows-Schale (alle Wirtschaftlichkeits- und Admin-Hüllen, `KostenKomponenteHuelle`); Emoji in zwei Ressourcentexten; Tarifstrukturdialog in Wärmepumpenprojekten ohne BHKW/PV nicht erreichbar (bekannt aus „Nach #291/#292"); BHKW-Dialog schreibt im OK-Weg auch ohne Änderung | `04/B21, B22, B29, B30` |

### 2.5 Wiki-Folgen

34 von 39 Bedienstücken sind auf den Seiten Kosten, Wirtschaftlichkeit und Varianten mit Anker
beschrieben (`05/§4`). Die drei Verdachtsfälle („Strombezug…", „Verlauf…", „Stammprojekt:") sind
**nicht** überholt, weil die zugehörigen Entscheide noch ausstehen. Lücken: Doppelpflege-Warnung,
beide PV-Anlagenwarnungen, Kohärenzhinweis „übernehmen ohne Stammprojekt", die Satzzeilen unter
Einspeisung/Eigenstrom (U23), und die Seite „Klimadaten" fehlt in der Bedienungsseiten-Tabelle des
Konzepts Hilfesystem. Tabuwörter: 20 Treffer in den Wiki-Quellen, alle fachlich; 38 in den Mockups,
fast alle in Quellcode-Kommentaren. **Produktdaten:** `Katalogfilter_Vorschlag.html`,
`stromspeicher-optimierung-v2.html` und der Hydraulik-Entwurf zeigen reale Hersteller, Typcodes und
Datenblattwerte — außerhalb des Wächters, aber die Vorlage für Wiki-Beispiele (`05/§5.2`).
**Hilfe-Zuordnung:** alle zwölf Dialoge tragen einen Hilfeschlüssel; `BhkwWirtschaftlichkeitDialog`
fehlt in `help_mapping.txt` (Hilfe-Taste wirkungslos), `PhotovoltaikVerguetungDialog` zeigt auf
„Kosten" statt auf den Vergütungsabschnitt der Seite Wirtschaftlichkeit, und keiner nutzt die
ankergenaue Zuordnung, obwohl acht passende Anker bereitstehen (`05/§6`). Logbuch: 14 Sätze für den
Sammel-Upload am 28.09.2026 liegen geprüft vor, ein fünfzehnter (#361) zur Bestätigung; Version
1.2.0.2 ist noch nicht bestätigt (`05/§7`).

## 3 Änderungsplan — wo welche Änderungen

Ziel-Kürzel: **M** Mockup · **P** Papier · **C** Code · **W** Wiki. Schwere: hoch / mittel / gering.
Ein Stern (*) heißt: erst nach dem Entscheid in § 4 — **alle diese Entscheide sind am 20.09.2026 nach
Empfehlung gefallen**, der Stern bedeutet also keine Blockade mehr, sondern nur die Herkunft.

> **Stand 22.09.2026.** Erledigte Zeilen tragen am Ende ihrer Änderungsspalte eine Standmarke
> (**umgesetzt #…**). Zeilen ohne Marke sind **nicht** geprüft worden oder unverändert offen; das gilt
> besonders für § 3.1 (Mockup) und § 3.2 (Papiere), wo E0 (#379) und E0c vieles, aber nicht
> zeilengenau belegbar nachgezogen haben.

### 3.1 Mockups

**`../Mockups/Dialog_Formel_Zahlenprobe.html`** — bleibt das eine Mockup des Themas.

| Stelle | Änderung | Quelle | Schwere |
|---|---|---|---|
| `<style>`: `.rumpf` | `grid-template-columns: 200px minmax(0, 1fr)`; dazu `.tafel-huelle, .fenster, .chart-karte, .seite { max-width: 100%; overflow-x: auto }`; `.duo` (Kat. 8, zwei Ergebnisseiten-Rahmen) untereinander; `.f-spalten` (Kat. 5) auf ≤ 50 % je Spalte | `00/S1, S10, S13` | hoch |
| Navigation | zweistufig (Kategorie → Dialog · Grundlage · Erläuterung · Schlüssel · Abnahme; Kat. 8 → die vier Fragen), „nach oben" am Ende jeder Kategorie | `00/S2`, `06` | mittel |
| Leseweg-Kasten | eine Legende aller Chips (umgesetzt Un · Vorschlag Un · geplant · Un · Beleg · Beispielzahl · abgeleitet · Maß der Norm · Einstufungen) | `00/S5`, `06/#9` | gering |
| alle Dialograhmen | zwei Rahmenstile: Fenster (Titelleiste mit ⓘ Hilfe und ×, Fußleiste) und Seitentafel (Kopfzeile ohne ⓘ/×); ⓘ/× bei Energieträger (Kat. 4), Parameterdialog (Kat. 8) und Reiter Ertrag/Bonus (Kat. 3) nachziehen; Fußleiste am Reiter Ertrag/Bonus ergänzen; Zusatz „× schließt ohne zu schreiben" in den Werkzeugtipp | `00/S6, S7`, `04/B25, B26` | mittel |
| Fußleisten Kat. 1–8 | *eine Regel nach § 4 Q8 (OK/Abbrechen/Speichern-Übernehmen) in allen Rahmen gleich zeichnen; Ergebnisseite als Seite mit Aktionsleiste und genau einem Primärknopf | `00/S8`, `04/B24, B27` | hoch |
| Kat. 1, 2, 5: Erläuterungsabsätze | in Tafeln „Element → Feldart → Verhalten" umsetzen | `00/S3` | mittel |
| Anhang Umsetzungsstand | Spalte „Stand" mit Chip (offen · erledigt · Entscheid ausstehend), offene zuerst; Zeile **U40** anlegen und als erledigt streichen (Vorlage „Standard" auf % des Endenergiebedarfs, Schritt 94, #365/#366); Zeilen für Brückenbild, Zahlungsstrombild, „Anhang‑E‑Checkliste…", „Bericht erzeugen", Fußzeile der Schnellwahl anlegen; U1 und U32: „Schemaschritt 97 (90–96 vergeben)" | `00/S4`, `02/c‑1, b‑3`, `03/#28, #43, #83, #84, #86, #38` | hoch |
| Kat. 5 Ressourcentafel Z. 2713 | `WIRT_KWKG_KONTINGENT_LEER` und `WIRT_KWKG_ERSATZ_GEWICHTET`: auf die vorhandenen Schlüssel `WIRT_KWKG_KONTINGENT_OHNE_ART/_ANTEIL_FEHLT/_ZU_KLEIN` ziehen, Marker „geplant" entfernen | `02/c‑2`, `03/§6.3` | mittel |
| Kat. 3 Ressourcentafel Z. 1652 | Klappliste „Stammprojekt:" → „Projekt:" (VV‑Q7) | `02/d‑17`, `03/#35` | mittel |
| Kat. 1 Fließtext Z. 784–786 | Übernahme ins Projekt auf den Katalogblock aus #363 beschreiben (Komponente · Kategorie · Variante · Positionsvorschau mit „Ziel") | `02/d‑16`, `03/#14` | mittel |
| Kat. 1 Raster | Herleitungszeilen auf die Ressourcenwerte ziehen („× 196.080,00 € · Hauptpositionen · Runde 2" statt „von …", ohne „Hauptposition", „Stufe Anlage" ohne Namen, „Satz = Betrag" ohne Zusatz); Herleitungszeile der Nutzungsdauer („15 a · Vorgabe der Technik") einzeichnen; Zeileneditor um „Positionsart:" ergänzen; Worst/Best-Überlagerung um Eingabeart, Umrechnungszeile, Zuschuss-Schalter; Tafel „Ersatz und Restwert" auf die gewählte Komponente reduzieren (die Variantenzeile gehört auf die Ergebnisseite); Absatz Z. 404–406 in die Ist-Form; vierte Summenzeile €/kWp erwähnen | `03/#1–#9, #12, #13, #15, #16` | mittel |
| Kat. 2 Raster | Endenergie-Tafel *vier oder sechs Spalten (Q17); Betriebszeile „× 1.650.000 kWh · Lauf" ohne Anlagenname; Abzeichen „Pflicht nach VDI 2067" streichen oder als Anhangpunkt führen; Empfehlungszeile unter das Satzfeld, Wortlaut `KDLG_EMPF_ZEILE`; Doppelpflege-Warnung über das Raster; Gruppen ins Fenster vor die Knopfleiste; neunten Grund `KDLG_BASIS_GRUND_STROMPREIS` in Prosa und Tafel; rückgerechnete Menge „21.710 kWh" *streichen oder Entscheid neu fassen (Kern verwirft sie ausdrücklich) | `03/#18–#27` | mittel |
| Kat. 3 | Pflichtzeilen mit 🔒 wie Kat. 2; fünf Bemessungsarten im Betriebsraster (auch „je kW elektrisch"); vierter Rasterknopf „Nutzungsdauern vorbelegen…" in Kat. 1 und 3; Optionsgruppe und Klappliste „Projekt:" als entweder/oder | `03/#30–#33, #36` | mittel |
| Kat. 4 | Nachweisumschlag: Fassung 3, vier Listen und sieben Skalare; Schnellwahl drei Spalten (Katalogwert und Jahr stecken in der Herkunft); Emissionssumme als Textzeile unter dem Raster; Trägerliste links als Rahmen einzeichnen; Formelzeile mit vierstelligem Arbeitspreis (mit C‑Änderung R5) | `03/#37, #39, #40`, `00/S12`, `01/B9` | gering |
| Kat. 4 BEHG | *nach Q3: BEHG-Zeile als „Ausweis — bereits im Arbeitspreis" kennzeichnen und in Kat. 8 eine Fußzeile ergänzen (Weg a), oder Arbeitspreis 0,6189 €/m³ ohne CO₂ und alle Energiekosten-/Kapitalwertzahlen neu rechnen (Weg b); Wert 181,4 g/kWh als Katalogwert kennzeichnen | `01/B1, B10` | hoch |
| Kat. 5 | Zählungen: acht Wahlfelder, neun Zahlenfelder, dreizehn Zahlen- und Datumsfelder (auch in der U22-Abnahme); Herleitungswortlaute auf die Ressourcen (`WIRT_KWKG_HERLEITUNG_TRANCHEN`, `NormEigen` „i.V.m.", `WIRT_KWKG_KONTINGENT_NEU`, `BHW_G6`, `BHW_V_STAND`); alle neun Kohärenzfälle nennen; Kesselhinweis als bedingten Fall; Anzeigezeile unter „Anteil Neuherstellungskosten" *streichen oder bauen; Brennwertkette ungerundet (4.797,0 MWh; 21.202,7 / 26.383,5 / 6.369,8 €) oder Faktor als „11,6 ÷ 10,5"; „12.079 €"; Fall‑2‑Menge 1.651,2 MWh; § 54 als anlagenscharfe Kesselwahl ausweisen | `03/#44–#53, #56, #57`, `01/B3–B6` | mittel |
| Kat. 6 | Herkunftszeile ohne Variantennamen (oder Ressource mit `{0}`); Knopf „Gesetzesparameter…" (mit C‑Änderung F1) | `03/#61, #29` | gering |
| Kat. 7 | Blockköpfe auf die Ressourcentexte; Block B mit „brutto", „abzüglich § 9b", „wirksam" wie das zweite Mockup; Leistungsanteil *nach Q4 herleiten oder in allen Spalten „keine Bezugsspitze"; § 53a/§ 54‑Zeilen mit Marker U7; Fußhinweis „Block B wird nicht summiert" *als Zeile bauen oder streichen; Erlös-Nominalsummen 624.594 / 740.512 | `03/#66, #68, #69`, `01/B2, B8, B12` | mittel |
| Kat. 8 | Parameterdialog: Vergleichsprojekt auf die Seite (§ 2.9), Gruppen wie im Dialog, Szenariotafel einzeichnen oder als Ausschnitt kennzeichnen, Fuß „Abbrechen · Speichern" ohne Statuszeile; Knopf „BHKW-Wirtschaftlichkeit…"; die zwei Fußleisten der Ergebnisseite zusammenführen; Wärmegestehungskosten mit Formel und Bezugsmenge; Hinweistext „Was ein Szenario variiert" *einen Wortlaut mit Konzept § 2.11.7 | `03/#91–#95, #86`, `01/B11`, `02/d‑18` | mittel |
| Kopf Z. 7 | Google-Schrift *einbetten oder Systemschriften (LIESMICH: „lokal im Browser öffnen") | `02/g‑5` | mittel |
| Überschriften, Schrift | h4 → h3 oder Erläuterungskästen aus der Gliederung nehmen; Schriftgrößen ≥ 11 px; Farben in einer Notation; `title` an Eingabezellen | `06/#3, #15, #12, #19` | gering |

**`../Mockups/Ergebnis_Bandbreite_Herkunft.html`** — *ablösen (Q1) in zwei Schritten: erst die zwei
anlagenscharfen Ergebnisansichten „5 Blockheizkraftwerk" und „6 Photovoltaik" (Zielansicht der Knöpfe
„BHKW…"/„Photovoltaik…", von Konzept § 2.13 (1) gebraucht) und den Kopfabschnitt „Was sich ändert" ins
konsolidierte Mockup übernehmen — mit den Zahlen aus `Beispielprojekt.md` —, dann `git mv` nach
`ueberholt/` mit Indexzeile und Nachzug in `Dokumentation/LIESMICH.md`, `LIESMICH.md` dieses Ordners,
Konzept § 2.13 und Statuseintrag „Nach #344". Kein Test bricht (`02/§4.1`). Bleibt es: 22 Zahlen
(`02/a‑1…a‑14`), Absatz „für bestehende Positionen gibt es keinen Handgriff" (U8), „sieben Knöpfe"
(`03/#79`), Bildunterschrift „bisher" (`02/h‑2`) nachziehen.

**Die vier übrigen Mockups** (`02/§4.2`, `06`, `05/§5.2`):

| Datei | Änderung | Schwere |
|---|---|---|
| `../Mockups/Katalogfilter_Vorschlag.html` | *nach `ueberholt/` im Bündel mit `../Konzept_Katalogfilter_EPOS-Plan.md` (Kopfblock Z. 7–12 vorher berichtigen), Indexzeile, zwei Code-Spannen und der Gegenprobe in `EPOS.Kern.Tests/DokumentationLinkWacheTests.cs` (Z. 398/402 auf eine bleibende Datei umhängen). Bleibt es in `aktuell/`: Herstellerdaten neutralisieren, Fußleisten (kein Abbrechen, „OK · Abbrechen"), „?" → ⓘ, vier `h1`, Navigation, eigene Ids je h2 | hoch |
| `../Mockups/Wechselrichter_Mockup_2026-09-06.html` | Absatz Z. 414–421 („Solange Stufe S3 aussteht …") entfernen; SVG Z. 1149 ohne feste 1.320 px; Fußleisten verdreht; Herstellerzeile „Ablytek 6MN6A275" neutralisieren; dann *nach `ueberholt/` mit `../Konzept_Wechselrichter_EPOS-Plan.md` (Z. 26–27 berichtigen) und Nachtrag 6 des PV-Ertragsmodells | hoch |
| `../Mockups/stromspeicher-optimierung-v2.html` | *nach `ueberholt/` mit `../Konzept_Stromspeicher_Dialoge_EPOS-Plan.md` (§ 8.4 Z. 982 in die Vergangenheitsform); Abschnittsnavigation; Herstellerzeile „Growatt WIT-M+APX ESS" neutralisieren | mittel |
| `../Mockups/Entwurf_Hydraulikuebersicht_Konfiguration.html` | *Q21: eigenes Konzept mit Indexzeile oder `ueberholt/`; viewport-Meta; Typcode „CS6800iAW" im Anlagentext neutralisieren | mittel |

### 3.2 Konzeptpapiere, Rechenwege, Beispielprojekt, Index, Status

| Datei · Stelle | Änderung | Quelle | Schwere |
|---|---|---|---|
| [`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md) Kopfzeile | „Stand 19.09.2026 · Zielversion 96 · Schritte 90–96 vergeben · neue ab 97"; Codestand *streichen (Q25) | `02/b‑1, f‑1` | hoch |
| dito § 3.6 Befund K‑1, § 5 U‑1, § 6.3 Nr. 9b | „nächster freier Schemaschritt 97"; Merksatz „ab 63" als historisch kennzeichnen; „Zielversion bleibt 89" → „wird davon nicht bewegt" | `02/b‑2, b‑4, b‑11` | hoch |
| dito § 2.2 | auf den gebauten Dialog umstellen: acht Gruppen, Feldbeschriftung „Hilfsenergieanteil [% des Endenergiebedarfs]", zweiter Sprungknopf „BHKW-Tarif…", Beispiel 5,5667 / 2,4167, Herleitungsbeispiel auf das Musterprojekt (4.797,2 MWh × 4,42 € = 21.203,4 €); *Q2: „Vorschlag am Feld" und Überlagerung U22 zusammenführen; § 7 B5 als umgesetzt | `02/d‑1…d‑5, d‑21` | hoch |
| dito § 2.3 | einspaltig/untereinander, Feld „Degradation [%/a]", Gruppennamen und -reihenfolge des Razor-Dialogs | `02/d‑6…d‑8` | mittel |
| dito § 2.4 | Vergleichsprojekt (§ 2.9) und Szenarien-Parametersatz (Konzept Szenarien § 4) ergänzen | `02/d‑10` | mittel |
| dito § 2.7 | „fünf Knöpfe" statt „sieben", K8 gegenstandslos; Fußknöpfe „mindestens 88 × 44"; *Q9 Kopfband und Fehlerfarbe; neuer Abschnitt „Hausstil Dialoge" nach der Regeltafel in `04/§6` (Bauform E/K, Kopfbaustein, Fußleiste, Knopftexte, Schreibweg, Rückfragen, Listen, Filter, Zahlenformat, Seiten, Datenseite) | `02/d‑11`, `04/B13–B15, B27, B28, §6` | hoch |
| dito § 2.8, § 2.12 | Herleitung „unter dem Betrag"; Spalten „Aktionen · Position · Bemessung · Satz · Betrag netto · Nutzungsdauer · Worst/Best"; Optionsgruppe plus zwei Reiter; vier Rasterknöpfe | `02/d‑12…d‑15` | mittel |
| dito § 2.11.7 | *einen Wortlaut des Hinweistextes mit dem Mockup (Ressource `WIRT_SZEN_HINWEIS`) | `02/d‑18` | mittel |
| dito § 2.13 (3), § 6.3 Nr. 9h, B5‑Kernaufgaben 1–2 | Knopf „Nutzungsdauern vorbelegen" und Positionsart als erledigt; 9h auf U39 (drei Stücke) kürzen; Nr. 1 (`KwkgAnlagenCtrl.Speichere` mit elf Spalten) und Nr. 2 (U31) abräumen; „fünf Punkte" → „fünf Punkte, dazu (6)"; Satz zum Kopfabschnitt „Was sich ändert" streichen oder auf das abzulösende Mockup beziehen | `02/c‑3…c‑6, d‑19, d‑20` | hoch |
| dito § 6.1 | zwei Etappenzeilen: VV (§ 2.16, Schritt 93, #359/#360) und Hilfsstrom-Umstellung (Schritt 94, #365/#366) | `02/c‑13` | mittel |
| dito § 2.16, „Begleitende Artifacts" | Anker `#pv` neben `#pvkosten`; am Artifact-Link vermerken, dass die Repo-Datei führt (oder Redeploy, *Q22); WinForms-Namen als Paar mit den Razor-Dateien | `02/g‑4, g‑6, d‑22` | gering |
| dito § 3.9 / Rechenweg 04 | Kohärenzfall „CO₂ im Arbeitspreis und BEHG-Reihe gleichzeitig aktiv" als Zeile der Tabelle aufnehmen (mit C‑Änderung R1) | `01/B1` | hoch |
| [`LIESMICH.md`](LIESMICH.md) | Pfade `../../ueberholt/KONTEXT_Kosten_Energie_Wirtschaftlichkeit.md` und `../../ueberholt/Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md`; Dialogform (Optionsgruppe, zwei Reiter, Spalten, vier Knöpfe); Artifact-Vermerk; nach Q1 die Zeile zum zweiten Mockup | `02/g‑1, d‑12…d‑14, g‑6` | mittel |
| [`Beispielprojekt.md`](Beispielprojekt.md) | § 1 Zeile „Energiesteuerwahl Kessel: § 54 EnergieStG (anlagenscharf)"; *Q4 Bezugsspitze und Strom-Leistungspreis; *Q3 CO₂-Weg; § 2 Brennwertkette ungerundet | `01/B2–B4` | mittel |
| [`Rechenweg/02_Betriebskosten_BHKW.md`](Rechenweg/02_Betriebskosten_BHKW.md), [`05_Verguetungen_BHKW.md`](Rechenweg/05_Verguetungen_BHKW.md) | Brennwertmenge 4.797,0 MWh bzw. Faktor „11,6 ÷ 10,5"; Beträge § 53a/§ 53/§ 54 ungerundet | `01/B4` | gering |
| [`Rechenweg/04_Energiekosten.md`](Rechenweg/04_Energiekosten.md) | BEHG 56.701,38 € oder Rundung nennen; 181,4 g/kWh als Katalogwert | `01/B7, B10` | gering |
| [`Rechenweg/07_Erloesrubrik.md`](Rechenweg/07_Erloesrubrik.md) | Leistungsanteil: Menge, Satz und Rechnung (nach Q4) | `01/B2` | mittel |
| [`../../ueberholt/Konzept_Nutzungsdauer_AfA_EPOS-Plan.md`](../../ueberholt/Konzept_Nutzungsdauer_AfA_EPOS-Plan.md) § 2 | „Zielversion 74" → 94 oder streichen | `02/b‑5` | mittel |
| [`../Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md`](../Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md) § 10.3 | „Zielstand dieser Etappe" | `02/b‑6` | gering |
| [`../Konzept_Katalogfilter_EPOS-Plan.md`](../Konzept_Katalogfilter_EPOS-Plan.md) Z. 7–12 | Kopfblock: S1–S3 umgesetzt (wie Kap. 9) | `02/f‑6, j‑2` | hoch |
| [`../Konzept_Wechselrichter_EPOS-Plan.md`](../Konzept_Wechselrichter_EPOS-Plan.md) Z. 26–27 | „S2 und S3 sind es nicht" berichtigen (Kap. 8: umgesetzt) | `02/f‑7` | hoch |
| [`../Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md`](../Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md) Nachtrag 6 | „Nichts davon ist umgesetzt" und „Migrationsschritte ab 65" berichtigen | `02/f‑8, b‑8` | hoch |
| [`../Konzept_Stromspeicher_Dialoge_EPOS-Plan.md`](../Konzept_Stromspeicher_Dialoge_EPOS-Plan.md) § 8.4 | „wird auf diesen Stand nachgezogen" → „ist nachgezogen" | `02/j‑5` | mittel |
| [`../Konzept_Hilfesystem_Wikidokumentation.md`](../Konzept_Hilfesystem_Wikidokumentation.md) | Zeile „Programm Dokumentation/Klimadaten" in die Bedienungsseiten-Tabelle; Regel, dass Beispieldaten aus Mockups vor der Wiki-Übernahme neutralisiert werden | `05/§3, §5.2` | mittel |
| [`../../LIESMICH.md`](../../LIESMICH.md) | Daten der elf Zeilen dieses Ordners; Katalogfilter-Zeile nach dem Umzug in die zweite Tabelle; Mockup-Absatz nach der Ablösung | `02/f‑2, f‑3` | mittel |
| [`../Status_iOS_Migration.md`](../Status_iOS_Migration.md) | „Nach #348 (a)/(b)", „Nach #352 (a)/(b)" als erledigt streichen; „Nach #362 (a)" 49 → 50; „Nach #355 (b)" auf die Ressourcentafel verengen; „Nach #344" nach Q1 schließen; Logbuch-Satz #361 und Version 1.2.0.2 bestätigen | `02/a‑15, a‑16, c‑9…c‑12, e‑8`, `05/§7` | gering |
| [`../../../EPOS.UI/CLAUDE.md`](../../../EPOS.UI/CLAUDE.md) | nach Q8 die Bauformen E/K und die Fußleistenregel; `SpeichernLeiste`-Kommentar zur entfallenen Ausnahme bereinigen | `04/B02, B31` | gering |

### 3.3 Programm

Rechenwirkung hat keine dieser Änderungen auf den Referenzlauf (er rechnet Simulationen); die
Kohärenzzeile R1 und die Zellensemantik F5 ändern Ausweise, U6/U7 die Zeilenstruktur der Rubrik.

| Datei | Änderung | Quelle | Schwere |
|---|---|---|---|
| `EPOS.Kern/Allgemein/Wirtschaftlichkeit/KohaerenzPruefung.cs` | neuer Fall „CO₂-Bestandteil im Arbeitspreis aktiv **und** BEHG-Reihe gebucht" (Warnung mit Betrag), Anzeige in Kat. 5 des BHKW-Dialogs und in der Rubrik — **umgesetzt #405** (Fall R5, `KohaerenzCo2Tests`) | `01/B1` | hoch |
| `EPOS.Kern/Controller/EnergietraegerPreiskarte.cs` `Formel` | Arbeitspreis mit `N4` statt `N2` — **umgesetzt #405** (Q7) | `01/B9` | gering |
| `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitZeilen.cs` | Reihenfolge `KAPITALWERT_DIFF` vor `NETTOBARWERT`; *Q16 Wert nullbar führen, `Anzeige` liefert „— ‹Grund›" ohne Wert und eine grundlose 0 bei gerechneter Null; *Q15 U6: Komponentenblöcke, Teilsummen, Block „projektweit", Gründe `WIRT_ERL_GRUND_KEIN_KESSELBRENNSTOFF`/`_KEINE_BEZUGSSPITZE`, Leistungsanteil projektweit; Fußzeile „Block B wird nicht summiert" — **Reihenfolge umgesetzt #405** (Q19); Q16 (E5) und U6/Q15 (E4) bleiben offen | `03/#62–#71` | hoch |
| `EPOS.Kern/Allgemein/Wirtschaftlichkeit/SteuerGutschriftRechner.cs` | U7: `SteuerErgebnis` mit zwei Beträgen (§ 53/53a und § 54), zwei Rubrikzeilen | `03/#64` | hoch |
| `EPOS.Kern/Allgemein/Bericht/Bausteine/BausteineWirtschaftlichkeit.cs` | Bandbreite im Bericht mit Spalte „Spanne" und Referenzzeile; Kommentar „an genau einem Ort" um den Dialogvorbehalt ergänzen; Tabellenbericht mit Spaltengruppe je Szenario (U13/U15) — **Bandbreite mit „Spanne" und Referenzzeile umgesetzt #405** (G8, in Word und Excel), die Spaltengruppe je Szenario bleibt offen (E6) | `03/#72, #96, #89` | mittel |
| `EPOS.Kern/MyResource/Resource.resx` (+ en‑US) | `KDLG_ERTRAG_G_PV` ohne „(V4/F7)", `KDLG_ERTRAG_PV` mit „(eine Vergütungswahrheit je Projekt)"; `PVV_SPRUNG_HINWEIS` neu fassen (mit E5); Emoji aus `ETV_BTN_SPEICHERN`; Standardknöpfe auf `ALLG_BTN_OK`/`ALLG_BTN_ABBRECHEN`/`ADM_BTN_SPEICHERN`; `WIRT_ENK_KOPF` bauen oder streichen — **teilweise umgesetzt #405**: acht neue Schlüssel, drei parametriert, zwei ohne Leser gestrichen, `PVV_SPRUNG_HINWEIS` neu gefasst, Standardknöpfe auf die Hausschlüssel; **offen:** `WIRT_ENK_ANLAGE` ohne Leser (mit der Fußleisten-Welle, Q8) | `03/#34, §6.2`, `04/B05, B11, B21` | mittel |
| `WindowsFormsApplication1/Views/Kosten/KostenKomponenteHuelle.cs` `GabenIntern` | `TitelReiterKosten`/`TitelReiterErtrag` aus `KDLG_TAB_KOSTEN`/`KDLG_TAB_ERTRAG` belegen (en‑US) — **umgesetzt #405** (Reiterbeschriftungen en‑US) | `03/§6.2` | mittel |
| `EPOS.UI/Dialoge/Kosten/ErtragBonus.razor` | Knopf „Gesetzesparameter…" auch im PV-Zweig (mit `GesetzeGewuenscht.HasDelegate`-Wache) — **umgesetzt #405** | `03/#29` | hoch |
| `EPOS.UI/Dialoge/Kosten/VorlagenZeile.razor` | Bemessungs-Klappliste der Neuzeile `Aktiv="@(Schreibbar && !Neuzeile)"` oder Auswahl an `PositionNeu` durchreichen — **umgesetzt #405** | `03/#10` | mittel |
| `EPOS.UI/Dialoge/Kosten/KostenKomponenteDialog.razor` | Knopf „Nutzungsdauern vorbelegen…" nur sichtbar, wenn wirksam (`_stand.NutzungsdauerVorbelegbar`); Rückfrage vor Kontextwechsel mit ungespeicherten Eingaben; Netto-Banner durch Kontextzeile ersetzen — **Knopfsichtbarkeit umgesetzt #405**; die Fußleiste dieses Dialogs ist mit **#390 (DL‑2e)** auf die `SpeichernLeiste` umgebaut; Rückfrage und Kontextzeile bleiben offen | `03/#26`, `04/B18, B10` | mittel |
| `EPOS.UI/Dialoge/Kosten/EnergietraegerEinstellungen.razor` | „Saisonale Sätze…" und „Katalogwerte übernehmen" in einer Leiste; Hinweiszeile hinter das Verstoßbanner | `03/#41, #42` | gering |
| `EPOS.UI/Dialoge/Kosten/BrennstoffBestandteile.razor`, `EPOS.UI/Bausteine/Ueberlagerung.razor` | *Fußzeile der Schnellwahl (Statustext, „Schließen"); `Ueberlagerung` mit optionalem `HilfeSchluessel`, damit eingebettete Dialoge den ⓘ erben | `03/#38`, `04/B07` | mittel |
| `EPOS.UI/Dialoge/Wirtschaftlichkeit/PhotovoltaikVerguetungDialog.razor` | *Q12 `TarifKlick` auf den OK-Weg (`nurBeiAenderung: true`) wie im BHKW-Dialog; Kennzahlenzeilen mit Umbruch (`white-space: pre-line` an `.epos-herleitung`) — **beides umgesetzt #405** (`PVV_SPRUNG_HINWEIS` neu) | `04/B11`, `03/#60` | hoch |
| `EPOS.UI/Dialoge/Wirtschaftlichkeit/BhkwWirtschaftlichkeitDialog.razor` (+ `…Texte.cs`) | OK-Weg nur bei Änderung (`Schreiben(Keiner, nurBeiAenderung: true)`); veralteten Kommentar Z. 1214 und Rückfalltext `BHW_G1B` bereinigen; Vorschau *nach Q15 auf die Rubrik — **OK-Weg und Kommentare umgesetzt #405**, die Vorschau auf die Rubrik bleibt offen (E4) | `04/B30`, `03/#58, #59, #54` | gering |
| `EPOS.UI/Seiten/Berichte/WirtschaftlichkeitSeite.razor` (+ `WirtschaftlichkeitDaten.cs`, Windows `WirtschaftlichkeitSeiteGaben.cs`) | Ergebnisansicht U2–U5, U10, U13: Umschalter, vier Fragen, Bandbreitentafel mit Spanne, Gliederung mit Nominalsummen, Annahmentafel (Ungünstig/Erwartet/Günstig), Empfehlungskarten, Verlauf mit drei Szenarien und Haken, Spannen-/Brücken-/Zahlungsstrombild, Deklarationszeilen, Hinweistext; Hinweiszeile Nutzungsdauer plattformfrei (U39); *Q18 Berichtserzeugung auf der Seite | `03/#72–#90, #97` | hoch |
| `EPOS.UI/Dialoge/Wirtschaftlichkeit/WirtschaftlichkeitParameterDialog.razor`, `…/TarifstrukturDialog.razor` | *Q8 Beschriftung des OK-Knopfs; Statuszeile im Fuß | `04/B04`, `03/#94` | gering |
| `EPOS.UI/Dialoge/Kosten/KostenfaktorKatalogDialog.razor`, `LeistungspreisReiheDialog.razor`, `EmissionskatalogDialog.razor` | `Rueckfrage` mit `VorgabeNein` vor jedem Löschen — **umgesetzt #405** | `04/B17` | mittel |
| die elf Dialoge mit eigener `.epos-leiste` (Liste in `04/B01`) | *Q8: Abschluss immer `SpeichernLeiste` (mit `RenderFragment Aktionen`), Reihenfolge Status → Aktionen · Speichern · Abbrechen · OK; Bauform K (Sofortschreiber) mit „Schließen" und Rückfrage je Änderung — **zwei der elf umgesetzt #390 (DL‑2e)**: `KostenKomponenteDialog` und `EnergietraegerDialog` tragen die `SpeichernLeiste` (Status · Speichern · Abbrechen · OK); die Zeilenaktionen bleiben Sofortschreiber, Abbrechen trägt dafür einen Kurztext (Entscheid DL‑Q3 a) | `04/B01–B03` | hoch |
| alle 21 Dialoge | neuer Baustein `Dialogkopf` (Titel, Kontextzeile „{Projekt} · {Variante} · netto", `InfoKnopf` mit `Dialogname`, `Schliesskreuz`), eine Bauart `TitelAnzeigen`; Titel der `Ueberlagerung` aus der Quelle des Dialogs; *Q13 Fenstertitel trägt den Langtitel, der Dialog keinen; Wache `UeberlagerungstitelTests` um die Bauarten c/d und den Fensterfall | `04/B06–B09, B12, B23` | hoch |
| `EPOS.UI/wwwroot/epos-ui.css` | *Q9 `.epos-dialog-kopf` mit `background: var(--epos-karte-titel)` und heller Schrift; ein Token `--epos-fehler-text`; `.epos-herleitung { white-space: pre-line }` | `04/B13, B14`, `03/#60` | mittel |
| `EPOS.UI/Dialoge/Wirtschaftlichkeit/GesetzeskatalogDialog.razor`, `EmissionskatalogDialog.razor`, `KostenfaktorKatalogDialog.razor`, `NutzungsdauerDialog.razor` | *Q10 `Katalogliste`/Spaltenfilter mit Suchfeld und Trefferzahl, Filterstand über `Katalogfilterregister` | `04/B20` | hoch |
| `EPOS.UI/Standards/Zahlen.cs` | benannte Stellenzahlen je Größenart (Geld 2 · ct/kWh 2 · Faktor 4 · Prozent 2 · Jahr/Stück 0 · Leistung 1) | `04/B28` | mittel |
| `EPOS.UI.Daten/` | *Q14 Hüllen der Wirtschaftlichkeits- und Admin-Dialoge sowie `KostenKomponenteHuelle` plattformfrei, Adapter nach dem Muster `EnergietraegerFenster.cs` — **umgesetzt #431** (E3 Schritte 1, 5, 6; Muster aus #428 angewandt) | `04/B22` | mittel |
| `EPOS.UI/Bausteine/Menuetabelle.cs` | *Q11 Menüpunkt „Administration → Kostenverwaltung → Tarifstruktur" auf `TarifstrukturHuelle.Oeffnen` | `04/B29` | mittel |
| `WindowsFormsApplication1/Allgemein/Hilfe/help_mapping.txt` | Zeile `Form_BhkwWirtschaftlichkeit.btn_Help = Wirtschaftlichkeit#…`; `Form_PhotovoltaikVerguetung.btn_Help = Wirtschaftlichkeit#pv-verguetung`; Anker für Kostenverwaltung, Energieträger, Parameter, Nutzungsdauern, Übernahme, Verlauf (Anker „Verlauf" auf der Wiki-Seite anlegen) — **umgesetzt #405** (`help_mapping.txt` und Wiki-Anker); die Zuordnung der gesetzlichen Parameter (A18) bleibt für E12 | `05/§6` | mittel |
| `EPOS.Kern.Tests/DokumentationLinkWacheTests.cs` | Gegenprobe Z. 398/402 vor dem Umzug des Katalogfilter-Mockups auf eine bleibende Datei umhängen | `02/g‑7` | hoch |
| `EPOS.Kern.Tests/WikiProduktdatenWacheTests.cs` | *Q24: Mockups in den Prüfpfad aufnehmen oder Regel im Konzept Hilfesystem (Neutralisierung vor der Übernahme) | `05/§5.2` | gering |

### 3.4 Wiki und Logbuch

| Seite (Repo-Quelle `Projekte/Wiki/`) | Änderung | Quelle |
|---|---|---|
| Programm Dokumentation - Kosten | bei `betriebsmengen` ein Satz zur Doppelpflege-Warnung (Wortlaut in `05/§4.1`) | `05/§4.1` |
| Programm Dokumentation - Wirtschaftlichkeit | bei `pv-verguetung` die beiden Anlagenwarnungen; bei `block-a` die Satzzeilen unter Einspeisung und Eigenstrom; Anker für den Verlaufsabschnitt; nach Umsetzung von K8 die Zeile zum Knopf „Verlauf…" streichen (Merkposten) | `05/§4.4, §4.5, §6` |
| Programm Dokumentation - Varianten | bei `pv-verguetung` der Kohärenzhinweis „übernehmen ohne Stammprojekt" | `05/§4.4` |
| Update-Logbuch (Sammel-Upload 28.09.2026) | 14 Sätze je Thema nach `05/§7.1` (#345, #347, #349/#353, #350, #352, #356, #357, #358, #359, #360, #363, #364, #365, #366), *dazu #361; Version 1.2.0.2 bestätigen | `05/§7` |
| alle Seiten | Beispiele aus den drei Mockups mit Herstellerdaten nur neutralisiert übernehmen („Wärmepumpe A", „Speicher 1, 100 kWh") | `05/§5.2` |

## 4 Entscheide des Anwenders — Fragen mit Empfehlung

> **Alle fünfundzwanzig Fragen sind beantwortet: „entschieden 20.09.2026 nach Empfehlung"**
> (Anwenderauftrag vom 20.09.2026 mit dem Zusatz „Entscheidung nach Empfehlung"; Statuszeile **#405**,
> zugleich für A1–A20 des Analysepapiers). **Die Empfehlungsspalte ist damit der Entscheid** — sie
> bleibt im Wortlaut stehen.
>
> **Umgesetzt seither:** **Q3** (Weg a, CO₂-Zeile als Ausweis) · **Q7** (Formel `N4`) · **Q12**
> (schreiben und springen, `PVV_SPRUNG_HINWEIS` neu) · **Q19** (Kapitalwertdifferenz über dem
> Nettobarwert) — alle vier **umgesetzt #405**. **Q16** (Gedankenstrich oder Null) ist entschieden und
> für **E5** vorgemerkt. **Q8** (Fußleistenregel) ist für die beiden Dialoge dieses Feldes mit **#390
> (DL‑2e)** gebaut, für die übrigen neun offen. **Q14** (Hüllenumzug) ist mit **E3 umgesetzt #431**.
>
> **Sieben Fragen trugen trotz „nach Empfehlung" einen Rest, der ein Wort des Anwenders braucht.**
> Entschieden 22.09.2026: **Q11** („kein HT/NT" — der Zeitzonentarif entfällt, offen nur die
> Leistungspreis-Staffel), **Q23** („letzte Nummer erhöhen" — Version 1.2.0.4), **Q15** (U6 ja, der
> Leistungsanteil bleibt projektweit), **Q18** (U43 mit E5, U44 mit E8) und **Q20** (mit der Abnahme
> des Mockups gegenstandslos). Am 22.09.2026 ebenfalls nach Empfehlung entschieden: **Q9** (`#B00020`) und **Q22** (Artifact neu
> veröffentlicht, Systemschrift). **Damit sind alle 25 Fragen entschieden.**

| # | Frage | Empfehlung |
|---|---|---|
| **Q1** | `Ergebnis_Bandbreite_Herkunft.html` abnehmen oder ablösen? | **Ablösen** in zwei Schritten (§ 3.1): erst die anlagenscharfen Ansichten BHKW/PV und den Kopfabschnitt ins konsolidierte Mockup, dann `git mv` nach `ueberholt/` |
| **Q2** | Konzept § 2.2 „Vorschlag am Feld" gegen die Überlagerung „Sätze und Herkunft" (U22)? | **Beides:** Grundlagenzeile mit Knopf am Feld bleibt (gebaut), die Überlagerung ergänzt Wahl und Herleitung; § 2.2 um einen Satz erweitern; die Zählungen der Kat. 5 sofort berichtigen |
| **Q3** | CO₂ doppelt im Beispiel: Weg (a) BEHG-Zeile als Ausweis kennzeichnen oder Weg (b) Arbeitspreis ohne CO₂ und alles neu rechnen? | **Weg (a)** — erhält alle nachgerechneten Zahlen; unabhängig davon die Kohärenzzeile im Kern bauen |
| **Q4** | Leistungsanteil −4.180,0 € herleitbar machen? | **Ja:** Bezugsspitze und Strom-Leistungspreis als Beispielgrößen aufnehmen und in Kat. 7 vorrechnen; sonst die Zelle in allen Spalten auf „keine Bezugsspitze" |
| **Q5** | Kesselwahl § 54 im Beispielprojekt ausweisen? | **Ja**, eine Zeile in § 1 — zugleich das einzige Beispiel der anlagenscharfen Wahl |
| **Q6** | Rundungsdisziplin: ungerundete Kette im Mockup? | **Ja für Beträge**, nein für angezeigte Mengen (Faktor als „11,6 ÷ 10,5" schreiben) |
| **Q7** | Formelzeile der Trägerkarte im Kern auf `N4`? | **Ja** — eine Zeile, sonst steht die Formel dauerhaft falsch |
| **Q8** | Fußleistenregel: OK überall, wo geschrieben und geschlossen wird; Katalogpflege als Sofortschreiber mit „Schließen" und Rückfrage je Änderung; Reihenfolge Status → Aktionen · Speichern · Abbrechen · OK? | **Ja** — Bauform E (Eingabemaske) und K (Katalogpflege) im Dateikopf benennen; sieben Dialoge ändern je eine Zeile |
| **Q9** | Kopfband `#0F1F3D` einführen oder § 2.7 auf den hellen Kopf umschreiben; welche Fehlerfarbe? | **Band einführen** (Tokens vorhanden, Kontrast 4,5:1 hält); Fehlerfarbe als ein Token, Wert `#B22222` oder `#B00020` nach Wahl. **Anwenderentscheid 22.09.2026 (nach Empfehlung): `#B00020`** — der Wert des gebauten Tokens `--epos-stufe-fehler` in `epos-ui.css`; die Mockups werden beim nächsten Nachzug angeglichen |
| **Q10** | Spaltenfilter für die drei übrigen Kostenkataloge (Emissionsarten, Kostenfaktoren, Nutzungsdauern) nachziehen? Der Gesetzeskatalog hat ihn seit #372 | **Ja**, in dieser Reihenfolge |
| **Q11** | Tarifstrukturdialog erreichbar machen: Menüpunkt, Schalter in der Kostenverwaltung oder Zonenmodell abkündigen? | **Menüpunkt** als kleinster Schritt; **Anwenderentscheid 22.09.2026: „kein HT/NT"** — der Zeitzonentarif (Winter/Sommer × HT/NT) wird nicht geführt, das Zonenmodell entfällt in dieser Ausprägung; offen bleibt nur, ob die zweistufige Leistungspreis-Staffel des Dialogs bleibt (Messung `Messung_Pflegewege_Tarifstruktur_Strom.md`). Folge für den Kern: `StromMatrix.Zone` trennt heute nach Tarifzone (Winter/Sommer × HT/NT) und bildet daraus die Bezugskosten — die Rückführung auf eine Zone ohne HT/NT ist ein Eingriff in den Rechenweg (Nach #291, Weg 3) und gehört mit A/B-Nachweis und gegebenenfalls neuer Referenzbasis zu **E7**, nicht zu E4. **Rest entschieden 22.09.2026 (nach Empfehlung): die zweistufige Leistungspreis-Staffel wird in die Kostenverwaltung neben die Energiepreisstruktur verlegt (Weg 2 aus Nach #291), mit E7; danach entfällt der Tarifstrukturdialog samt Menüpunkt** |
| **Q12** | Sprungknopf „Tarif…" im PV-Dialog: schreiben und springen oder verwerfen und es sagen? | **Schreiben und springen** wie im BHKW-Dialog; `PVV_SPRUNG_HINWEIS` in beiden Sprachen neu fassen |
| **Q13** | Beim Öffnen aus dem Menü trägt das Fenster den Titel, der Dialog keinen? | **Ja** — dieselbe Regel wie bei der `Ueberlagerung`; der Fenstertitel muss dann der lange sein |
| **Q14** | Hüllen der 15 windowsgebundenen Dialoge nach `EPOS.UI.Daten`? | **Ja, gestaffelt:** `KostenKomponenteHuelle` zuerst |
| **Q15** | Erlösrubrik nach Komponente innen (U6) bauen? | **Ja** — sonst bleibt die Zahlenprobe der Kat. 7 im Programm nicht nachvollziehbar; vorher klären, ob der Leistungsanteil projektweit bleibt. **Anwenderentscheid 22.09.2026 (nach Empfehlung): U6 bauen (E4); der Leistungsanteil der vermiedenen Stromkosten bleibt projektweit** (Block „projektweit", keiner Anlage zuzurechnen), nur der Arbeitsanteil wird nach der Näherung V‑4 je Anlage verteilt und als Näherung ausgewiesen (A12) |
| **Q16** | Gedankenstrich oder Null in Ergebniszellen? | **Dem Mockup folgen** („— ‹Grund›" ohne Wert); Excel bleibt numerisch; mit Referenzlauf abnehmen |
| **Q17** | Endenergie-Tafel vier oder sechs Spalten? | **Vier** — Zeichnung kürzen; Träger und Arbeitspreis stehen in der Energieträgerverwaltung |
| **Q18** | „Bericht erzeugen" und „Anhang‑E‑Checkliste…" auf der Wirtschaftlichkeitsseite? | Zweiter Einstieg in den Bericht vertretbar; die Checkliste ist eigene Arbeit — **Anhangzeile anlegen, dann entscheiden**. **Anwenderentscheid 22.09.2026 (nach Empfehlung): U44 „Bericht erzeugen" wird mit E5 gebaut (ruft den bestehenden Berichtsweg), U43 „Anhang‑E‑Checkliste…" mit E8, wenn V‑C und V‑D den Inhalt liefern** (Nummern nach dem Anhang des Mockups: U43 Checkliste, U44 Bericht; berichtigt 22.09.2026) |
| **Q19** | Kennzahltafel: Nettobarwert unter die Differenz? | **Ja**, reine Anzeige |
| **Q20** | Parameterdialog im Mockup vollständig zeichnen? | **Ergänzen** (mindestens Szenariotafel und Gruppe „Bewertung") oder als Ausschnitt kennzeichnen. **Mit der Anwenderabnahme des Mockups am 22.09.2026 gegenstandslos:** Die Zeichnung bleibt, wie abgenommen; der vollständige Dialog steht gebaut im Programm (Szenarienkonzept § 4, § 10.4) |
| **Q21** | Katalogfilter, Wechselrichter, Stromspeicher-Dialoge nach `ueberholt/`; Hydraulik-Entwurf weiterverfolgen? | **Ja**, als ein Auftrag je Thema mit Konzept, Index, Code-Spannen und Test-Gegenprobe; Hydraulik-Entwurf ohne Konzept → `ueberholt/` |
| **Q22** | Google-Schrift im Mockup; Artifact neu veröffentlichen? | Systemschrift-Kette; Artifact redeployen oder Vermerk „Repo-Datei führt". **Anwenderentscheid 22.09.2026 (nach Empfehlung): Systemschrift-Kette, Artifact neu veröffentlicht** — unter dem Konto dieser Sitzung mit neuer Adresse (Wegweiser des Ordners), weil das alte Artifact dem früheren Konto gehört |
| **Q23** | Logbuch: #346 ohne Eintrag lassen, #361 aufnehmen; Version 1.2.0.2? | **#346 ohne, #361 mit**; **Anwenderentscheid 22.09.2026: „letzte Nummer erhöhen"** — Version **1.2.0.4** (das Programm trägt 1.2.0.3 in `AssemblyInfo.cs`; die Anhebung gehört zur Auslieferung, das Update-Papier führt 1.2.0.4) |
| **Q24** | Gestaltungsfamilien der Mockups vereinheitlichen; Akzentfarben ins Farbregister; Produktdaten-Wache auf Mockups ausdehnen? | Entscheidung nur für **bleibende** Mockups nötig (nach Q1/Q21 bleibt eines); Wache auf Mockups ausdehnen, solange sie Wiki-Vorlage sind |
| **Q25** | Kopfzeile des konsolidierten Konzepts mit Codestand führen? | **Streichen** — Datum und Zielversion pflegen, die Zielversion ist maschinell prüfbar |

## 5 Vorgeschlagene Reihenfolge

**Stand 22.09.2026.** Diese Reihe entstand am Vormittag des 19.09.2026, vor dem Etappenplan **E0–E12**
des Analysepapiers (§ 5 dort). Beide meinen in weiten Teilen dieselbe Arbeit; maßgeblich für die
Umsetzung ist **E0–E12**. Die Zuordnung: **P6 ≈ E4** · **P7 ≈ E5** · **P8 ≈ Teil von E3** (die Hüllen
aus Q14) und Q1/Q21 als eigene Aufträge · **P9 ≈ E12**. **P5 „Hausstil Dialoge" hat keine
Entsprechung im Etappenplan** — die Arbeit läuft heute unter der eigenen Wellenreihe **DL‑2**
(Knopfleisten), von der **DL‑2e (#390)** die beiden Dialoge dieses Feldes erledigt hat; eine
Einordnung von P5 in E0–E12 ist **nicht** vorgenommen worden und bleibt zu klären.

| Etappe | Inhalt | Rechenwirkung |
|---|---|---|
| **P1 Papierpflege ohne Entscheid** — **umgesetzt #379** (E0a/E0b); auf den Stand nach #428 nachgezogen mit E0c | Mockup: CSS-Überlauf, U40-Anhangzeile, Schemaschritt 97, Ressourcentafel-Nachzüge, Wortlaute der Herleitungszeilen, Zählungen der Kat. 5, Rundungen; Konzept: Kopfzeile, § 2.2/2.3/2.4/2.7/2.8/2.12/2.13/6.1/6.3, Kopfblöcke der drei Konzepte; LIESMICH-Pfade; Index-Daten; Statuseinträge | keine |
| **P2 Entscheide Q1–Q25** einholen — **erledigt 20.09.2026**, alle nach Empfehlung (#405) | die Tabelle in § 4 | — |
| **P3 Kleine Codekorrekturen** — **umgesetzt #405** (in W‑E2 enthalten; alle zehn Punkte) | Formel `N4`, Kennzahl-Reihenfolge, Gesetzesparameter-Knopf im PV-Reiter, Neuzeile-Klappliste, Reiterbeschriftungen en‑US, `help_mapping.txt`, Kommentare und Rückfalltexte, PV-Sprungknopf (Q12), Löschrückfragen, Knopfsichtbarkeit | keine auf den Referenzlauf; Gate mit Kern- und UI-Tests |
| **P4 Kohärenzzeile CO₂ und Zellensemantik** — **Kohärenzzeile umgesetzt #405** (Fall R5, dazu R6 und die Strommix-Zeile); die Zellensemantik (Q16) bleibt offen und gehört zu **E5** | `KohaerenzPruefung` neuer Fall; `WirtschaftlichkeitZeilen` nullbar (Q16) | Ausweis; Referenzlauf byte-gleich, Berichtsprobe |
| **P5 Hausstil Dialoge** — **offen**; die Fußleisten der beiden Kostendialoge sind mit **#390 (DL‑2e)** gebaut, der Rest läuft unter der Wellenreihe DL‑2 und ist im Etappenplan E0–E12 **nicht** eingeordnet | Fußleistenregel (Q8), Baustein `Dialogkopf`, Kontextzeile, Kopfband (Q9), Fenstertitel (Q13), Zahlenformat, Spaltenfilter (Q10), Menüpunkt Tarifstruktur (Q11); Konzeptabschnitt „Hausstil Dialoge" und `EPOS.UI/CLAUDE.md` | keine; bunit und Wachen |
| **P6 Erlösrubrik und Steuerzeilen** — offen, **≈ E4** | U6 (Q15), U7, Gründe der Nullzeilen, BHKW-Vorschau auf die Rubrik | Zeilenstruktur; A/B-Nachweis am Beispiel 293.245,6 + 22.914,0 |
| **P7 Ergebnisansicht** — offen, **≈ E5** (der Verlauf mit drei Szenarien in E6) | U2–U5, U10, U13, U39; Bericht mit Spanne, Nominalsummen, Spaltengruppen je Szenario | Ausweis; ChartProben, Berichtsprobe |
| **P8 Ablösungen und Umzüge** — Hüllen-Teil (Q14) **umgesetzt #431**; Q1 und Q21 (Mockup-Ablösungen) bleiben **offen**, eigene Aufträge | Q1 (zweites Mockup), Q21 (drei Mockups mit Konzepten, Test-Gegenprobe), Hüllen (Q14) — erledigt | keine |
| **P9 Wiki** — offen, **≈ E12**; die `help_mapping`-Anker sind mit **#405** gesetzt | fünf Lücken, Anker, Logbuch 14+1 Sätze, Klimadaten-Zeile — im Sammel-Upload 28.09.2026 | — |

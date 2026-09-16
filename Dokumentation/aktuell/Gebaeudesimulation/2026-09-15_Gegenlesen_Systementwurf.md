# Gegenlesen des Systementwurfs Gebäudesimulation (15.09.2026)

**Papier:** [`Systementwurf_Gebaeudesimulation_EPOS-Plan.md`](../Systementwurf_Gebaeudesimulation_EPOS-Plan.md)
(Rev. 1 → **Rev. 2**)
**Schwesterpapier:** [`Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md`](../Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md)
— dort arbeitet ein zweiter Leser parallel; die Abstimmung der Namen lief über die verbindliche
Skizze des Auftrags, **nicht** über Änderungen am Schwesterpapier.

## Vorgehen

Der Entwurf wurde von vier Lesern mit vier Blickwinkeln gegengelesen. Jeder Leser bekam dieselbe
Skizze (Gliederung, Namen, Auflösung der Widersprüche, Entscheidevorschläge, Grenzen) und las gegen
den Quelltext, die Testdatenbank (nur lesend), die vier Konzeptpapiere und die Hausregeln.

| Leser | Blickwinkel | Frage, die er stellte |
|---|---|---|
| **sys-bestand** | Quelltext und Bestand | Stimmt jeder `Datei:Zeile`-Beleg? Stimmt jede Bildkante mit der Projektverweisrichtung? |
| **sys-konsistenz** | Papier gegen Papier | Widerspricht sich der Entwurf selbst oder einem geltenden Konzeptpapier? Steht eine Zahl zweimal? |
| **arch-bestand / arch-konsistenz** | Schwesterpapier | Sagen beide Papiere dasselbe über dieselbe Naht, dieselbe Kennzahl, dieselbe Einfrierkette? |
| **quer** | beide Papiere gemeinsam | Welche Aussage steht an zwei Stellen — und ist sie schon auseinandergelaufen? |

**Phasen:** Skizze (verbindliche Gliederung und Namen) → Entwurf Rev. 1 → Gegenlesen mit
60 Befunden → Einarbeitung zu Rev. 2.

**Regel der Einarbeitung:** *hoch* und *mittel* werden eingearbeitet; bei Widerspruch zwischen zwei
Lesern entscheidet der Quelltext bzw. das geltende Papier, und die Entscheidung steht unten in der
Spalte *Erledigung*. *niedrig* wird eingearbeitet, wenn es schnell geht, sonst als „offen (Form)"
vermerkt. Der **Papierteil** aller 60 Befunde ist eingearbeitet; bei fünf bleibt ein Rest
außerhalb der beauftragten Dateien (Indexzeilen, Belegquelle) — die Zählung unten weist ihn
getrennt aus.

## Die Befunde

| Nr. | Schwere | Leser | Stelle | Befund | Erledigung |
|---|---|---|---|---|---|
| 1 | hoch | sys-bestand | 1.3 B1, Entscheidetabelle | Entscheid **E11** (Gebäudebetrachter, Zonengeometrie-Modell) fehlt vollständig | E11 in die Entscheidetabelle („Die elf Entscheide") und in B1; Baustein in Bild 1 und 2.2; Vertrag **V13**; 3.4 „gelesen, nicht gerechnet"; three.js in Kapitel 9 |
| 2 | hoch | sys-bestand | 2.1 Bild 1 | Drei Kanten gegen die Abhängigkeitsrichtung (`KOMP --> HUE`, `LES --> CTRL`, `PHY --> BER/EXP`) | `KOMP --> HUE` gestrichen, als gestrichelter Datenfluss „Ergebnis-Record" gezeichnet; `LES --> CTRL` gestrichen; `BER --> PHY`, `EXP --> PHY` gedreht. Belegt: `EPOS.UI.Daten/EPOS.UI.Daten.csproj:52`, `EPOS.UI/EPOS.UI.csproj:44` |
| 3 | hoch | sys-bestand | 1.3 B9 | „Vier Kanäle" — es sind drei | B9 auf **drei** Kanäle mit Beleg `SimulationKanaele.cs:429-438` (`ANZAHL = 3`); Verweis auf Abwägung 10 („ein Kühlkanal wäre der vierte") |
| 4 | hoch | sys-bestand | 0.1, Bild 2 | `:647` liegt **nicht** im Rumpf von `HeizwaermeEinesGebaeudes` (566–611), sondern in `Bewohner_und_Flaeche_berechnen` (613), gerufen bei `:575` — also **vor** `:581` | Nachgezählt und bestätigt. Punkt 0.1 und 3.1 umformuliert: ein Verzweigungspunkt im Rumpf (`:581`) und eine zweite Stelle, die derselben Modellwahl folgt und vorher läuft. Bild 2 in dieser Reihenfolge beschriftet |
| 5 | hoch | sys-bestand | Vorwort; `Dokumentation/LIESMICH.md` | Indexpflicht des Wächters (Fall 2) übersehen; Indexzeilen fehlen | **Papierteil erledigt:** Vorwort nennt die Indexpflicht ausdrücklich und verweist auf `../LIESMICH.md`. **Indexzeilen selbst pflegt der Orchestrator** — sie liegen außerhalb der beauftragten Dateien |
| 6 | hoch | sys-bestand | 8.3 Bild 8 und Tabelle | Klimaspalten hinter G1 + G2 gezeichnet, Sichtneubau fehlt im Bild, Reihenfolge der Tabelle falsch | Bild 8 auf die Reihenfolge des Schwesterpapiers 2.8 gebracht: Umbenennung, Gebäudespalten samt Sichtneubau und Klimaspalten als Selbstschleife an `Basis_GB`; Tabellen- und Zonenschritte an `Basis_G1G2` |
| 7 | mittel | sys-bestand | 3.1 | Beleg `EPOS.Kern/CLAUDE.md:124-126` trägt die Zwei-Nähte-Regel nicht | Nachgezählt: Punkt 4 steht bei `:141-142`. Beleg geändert |
| 8 | mittel | sys-bestand | 3.1 | Abbruchblock steht bei `:590-595`, nicht `:591-599` | Nachgezählt (`if (!tagv_found)` :590 … `}` :595). Auf `:590-595` geändert — gleichlautend mit Schwesterpapier A16 |
| 9 | mittel | sys-bestand | 6, Bild 7 | Text sagt sechs Klassen, Bild zeichnet sieben (`K7`) | `K7` gestrichen; der Importfall steht im Bild als **Regel 4 über allen Klassen**, ohne Klassenkennung — so, wie ihn die Tabelle führt. Bildunterschrift sagt es ausdrücklich |
| 10 | mittel | sys-bestand | 5.2 | „weniger als ein Drittel" — 3 500 von 8 760 sind rund 40 % | Auf „rund **40 %**" geändert. Die gleichlautende Zahl in Befund V 6.2/0.7 ist **nicht** geändert: Befunde sind Belegquellen und liegen außerhalb der beauftragten Dateien — Korrektur dort ist nachzuziehen |
| 11 | mittel | sys-bestand | 11 | „der einzige Textvergleich seines Umfelds" — es sind mehrere | Nachgelesen: `SimulationWaermebedarf.cs:353`, `:569`, `:601`, `:617-638` und `SimulationKanaele.cs:454-463`. Zeile auf „mehrere Textvergleiche" umgeschrieben, alle benannt, kein Umbauauftrag |
| 12 | mittel | sys-bestand | 8.3 | A14 falsch zugeschnitten: Träger und Name der Handlung sind entschieden | Knopf „Gebäude als eine Zone übernehmen" im Reiter „Hülle und Rechenmodell", mit G3 (Schwesterpapier 2.3 W1, 3.2); A14 auf den Umschalter Klassenweg → Bauteilweg eingegrenzt |
| 13 | mittel | sys-bestand + arch | 8.3; Schwesterpapier 2.8 | Ordnungszahl der Klimaregel kollidiert mit der Eintragungsreihenfolge | Zusammen mit Befund 30 gelöst: Der Entwurf benennt die Regeln über ihren **Gegenstand** („gesäte Klimareihen", „gesäte Gebäudedaten", „gesäte Zonendaten") und sagt, dass die Nummer der Schritt bei seiner Beauftragung vergibt. Diese Form ist mit jeder Nummerierung des Schwesterpapiers verträglich — das war die einzige Abstimmung, die ohne Eingriff in das Schwesterpapier möglich war |
| 14 | niedrig | sys-bestand | Bild 2 | Anführungszeichen an `participant … as` erscheinen im Kästchen | Die acht `participant`-Zeilen ohne Anführungszeichen geschrieben |
| 15 | niedrig | sys-bestand | Bild 5 | Eine Methode als Klasse modelliert; Mitgliedsnamen weichen vom Schwesterpapier ab | Kasten auf `SimulationWaermebedarf` mit der Methode umgestellt; `Gebaeudepruefung.Pruefe(eingang) IReadOnlyList`, `GebaeudeModellEingang.Bauen/Pruefmodus/Zonen` und `GebaeudeModellErgebnis` aus Schwesterpapier 1.3 übernommen |
| 16 | niedrig | sys-bestand | Bild 6 | Zwei Namenswelten; Sicht und Kopierweg als Fremdschlüssel gezeichnet | Alle Entitäten mit ihrem echten Bezeichner (`Tab_Projekt`, `Z_ProjektGebaeude`, `Tab_Klimaregion`, `Abfrage_Projektgebaeude`); die zwei Nicht-FK-Kanten beschriftet. **Zusätzlich beim Nachlesen gefunden:** Die Kante Zuordnung ↔ Gebäude lief falsch herum — `Tab_Gebaeude.ID_ProjektGebaeude` zeigt auf `Z_ProjektGebaeude.ID` (`sql/schema/001_grundschema.sql`, `sql/schema/002_views.sql:89-91`); gedreht und im Text erläutert |
| 17 | niedrig | sys-bestand | Bild 4 | Ja/Nein-Frage mit Plattformantworten | Knoten auf `Plattform?` geändert |
| 18 | niedrig | sys-bestand | 8.4 | Gegenposition von Befund V 5.2 nicht genannt | Mit Befund 31 zusammen gelöst — siehe dort |
| 19 | hoch | sys-konsistenz | 1.3 B9 | = Befund 3 | siehe Befund 3 |
| 20 | hoch | sys-konsistenz | Bild 1 | = Befund 2 (a) | siehe Befund 2 |
| 21 | hoch | sys-konsistenz | 1.1, 2, 8, 9, 12 | E11 fehlt: keine funktionale Anforderung, kein Baustein, keine Lizenzzeile, keine Geometrieprobe | **F17** aufgenommen (Betrachter, Kennzeichnung „schematisch", Geometrieherkunft je Zone); Baustein in Bild 1 und 2.2; Zeile „Determinismus der Geometrie" in 8.1; three.js in Kapitel 9; Abgrenzung in Kapitel 12 verweist auf Datenaustausch 14 |
| 22 | hoch | sys-konsistenz | `Dokumentation/LIESMICH.md` | = Befund 5 | siehe Befund 5 |
| 23 | mittel | sys-konsistenz | 1.2 N3 gegen 1.1 F10 und 7.1 | N3 gilt ausnahmslos, F10 überschreitet es um zwei Größenordnungen | N3 zweigeteilt: Einzonengebäude ≤ 10 ms, **Mehrzonengebäude benannt ausgenommen** (1,1–2,0 s bei 50 Zonen, Mehrzonenkonzept 2.9). 7.1 um den ungekoppelten adiabaten Vorlauf (rund 0,25 s) und um die Gesamtzeile ergänzt |
| 24 | mittel | sys-konsistenz | 0.3 und 5.3 | „genau ein Sichtneubau" ist eine offene Anwenderfrage (U5 / A9), keine Systemeigenschaft | Vorbehalt in 0.3 und 5.3 gesetzt: ein Sichtneubau, sobald U5 / A9 verschmilzt, sonst zwei; der Klimaschritt bleibt in beiden Fällen getrennt |
| 25 | mittel | sys-konsistenz | 6, K6 | Schätzweg gibt es nur für Gegenstrahlung; Wind und Feuchte haben einen festen Rückfall | K6 in **K6a** (Schätzweg, Gegenstrahlung) und **K6b** (fester Rückfall, Wind/Feuchte) geteilt. Die Klassenzahl bleibt sechs, weil beide gleich melden; der Einleitungssatz sagt das jetzt. V2 in 4.1 nachgezogen. Beleg: Umsetzungskonzept 1.7, Spaltentabelle (`:462-464`) |
| 26 | mittel | sys-konsistenz | 6, Bild 7 | = Befund 9 | siehe Befund 9 |
| 27 | mittel | sys-konsistenz | 1.1 F7 | Quelle trägt die Aussage nicht; achte Kennzahl ohne Beleg; zwei Namen für dieselbe Größe | Quelle auf „Umsetzungskonzept 1.4 **und** Befund U 5.2" erweitert; F7 nennt **acht** Kennzahlen einschließlich Jahresheizwärme; „Übertemperaturstunden" und „Überhitzungsstunden" ausdrücklich als dieselbe Größe benannt |
| 28 | mittel | sys-konsistenz | 3.1 | = Befund 7 | siehe Befund 7 |
| 29 | mittel | sys-konsistenz | F5, V1/V3 | Die Skalierung steckt im Rückgabewert der Tagesrechnung, nicht in einer Nachmultiplikation | F5 um den Satz ergänzt („Teil der Modellrechnung … das Stundenmodell führt die Verhältnisrechnung selbst"), Quelle Umsetzungskonzept 1.5; V3 sagt jetzt, dass `HeizlastW` **bereits skaliert** den Kern verlässt |
| 30 | mittel | sys-konsistenz | 8.3 | „sechste Einfrierregel" — Nummer kollidiert; kein Konzeptpapier kennt die Regel | Ordnungszahl weggelassen, Regel über ihren Gegenstand benannt, und ausdrücklich als **Empfehlung dieses Entwurfs** gekennzeichnet, die in den Abschnitt „Regressionsnetz" der Wurzel-`CLAUDE.md` eingreift |
| 31 | mittel | sys-konsistenz | 8.4 | Empfehlung widerspricht E4 und Befund V 5.2, ohne beide zu nennen | Beide wörtlich zitiert und die Empfehlung ausdrücklich als **Abweichung** gekennzeichnet; dazu der Verbleib der GB-Basis (aktuelle Basis bis zum Merge G1 + G2, danach mit Protokoll nach `ueberholt/Referenzbasen/`) und der gitignorierte Rückweg-Test als Übergangsweg |
| 32 | mittel | sys-konsistenz | 3.3 Bild 3 | Zonenbildung aus gbXML ist mit D16 offen | Regel 3 um den Vorbehalt ergänzt: Zonen aus IFC ab G6c; gbXML-Zonen sind mit **D16** offen, bis dahin legt G4c ein Gebäude ohne Zonen an. Das Bild zeichnet den ausgebauten Fall, was jetzt dabeisteht |
| 33 | niedrig | sys-konsistenz | 3.1 | = Befund 8 | siehe Befund 8 |
| 34 | niedrig | sys-konsistenz | 1.1 F12 | „Stufe G4" statt G4c | Auf **G4c** geändert |
| 35 | niedrig | sys-konsistenz | 0.1 | Fünf Quelltextbelege im ersten Satz für die Projektverantwortung | Punkt 0.1 verdichtet, Belege stehen nur noch in 3.1 — dort neu als eigener Absatz „Die Naht im Wortlaut ihrer Fundstellen" |
| 36 | niedrig | sys-konsistenz | 0 | Datenaustausch (E9) kommt in der Zusammenfassung nicht vor | **Siebter Punkt** ergänzt (Zuordnungsgerüst, eine Geometriequelle, Lizenzhinweisseite als Vorbedingung); Kapitelüberschrift auf „sieben Punkten". **Bewusste Abweichung von der Skizze** („sechs Sätze") — sie war nötig, weil derselbe Punkt zugleich E11 trägt (Befund 1, 21) |
| 37 | niedrig | sys-konsistenz | 4 | Kein Vertrag für Bericht und Diagramme, obwohl Bild 1 den Baustein führt | Vertrag **V14 Berichtskante** ergänzt (Eingaben, Ausgaben, Fehlerfall „Merkmal am Gebäude statt leerem Bild", Beleg `Proben/ChartProben`); 2.2 bekommt die zugehörige Zeile; Kapitelvorspann auf „Vierzehn Verträge" |
| 38 | niedrig | sys-konsistenz | 1.2 N5 | L9 steht in Befund U 8.2, nicht in Kapitel 7 | Quelle auf „Befund U 7; Befund U 8.2 (L9); Befund T 1.4" geändert |
| 39 | niedrig | sys-konsistenz | 3.1 | Berichtigung des Umsetzungskonzepts 1.4 nicht benannt | **Durch Befund 45 aufgelöst:** Mit der Drei-Nähte-Fassung ist Umsetzungskonzept 1.4 („umgerechnet wird im Kern, `GebaeudeBedarfCtrl` bzw. `SimulationErgebnisCtrl`") richtig und braucht keine Berichtigung; fortzuschreiben ist stattdessen `EPOS.Kern/CLAUDE.md`, Einheitenregel 4. Der Entwurf sagt das jetzt |
| 40 | niedrig | sys-konsistenz | 10 | Vier Abwägungen ohne ADR, ohne dass es dasteht | Die vier Zeilen tragen den Zusatz „**ohne eigenen ADR, Festlegung dieses Papiers**"; die zwei von ADR-Gewicht (Zeilen 7 und 8) stehen zusätzlich in Kapitel 11 zur Wiedervorlage |
| 41 | hoch | arch-bestand | `Dokumentation/LIESMICH.md` | = Befund 5 | siehe Befund 5 |
| 42 | hoch | arch-konsistenz | beide Papiere | = Befund 1 / 21, mit den Folgen für das Schwesterpapier | Der Systementwurf-Teil ist erledigt (siehe Befund 1, 21). Die Punkte (a) bis (g) für das Schwesterpapier gehören dem zweiten Leser; abgestimmt über die Skizze |
| 43 | mittel | arch-konsistenz | 1.5 / V6 | Dieselbe Naht mit zwei Signaturen (Pfad gegen Strom, mit und ohne Profil) | Siehe Befund 49 — gemeinsam gelöst |
| 44 | hoch | quer | beide Papiere | = Befund 4, mit dem Selbstwiderspruch des Schwesterpapiers | Systementwurf-Teil erledigt (siehe Befund 4). Der Satz „der Einheiten- und Bewohnerzweig davor bleibt unberührt" steht im Schwesterpapier 4.1 und gehört dem zweiten Leser |
| 45 | hoch | quer | 3.1 und 11 gegen Schwesterpapier 1.7 | Der Entwurf sagt „keine dritte Naht", das Schwesterpapier beauftragt die Regeländerung — direkter Widerspruch über eine Hausregel | **Quelltext entscheidet:** `GebaeudeBedarfCtrl.cs:130` bildet `werte.Sum() / 1000`, also eine Energiemenge im Controller; `:121` ist die Leistungsumrechnung. Die Fassung des Schwesterpapiers ist belegt und wird übernommen: **drei Nähte, alle drei im Kern**, die dritte benannt und in die Regel aufgenommen; `EPOS.Kern/CLAUDE.md` Punkt 4 ist im selben Merge fortzuschreiben. 3.1 und Kapitel 11 entsprechend umgeschrieben |
| 46 | hoch | quer | 1.3 B9 | = Befund 3 | siehe Befund 3 |
| 47 | hoch | quer | 8.3 Bild 8 | = Befund 6 | siehe Befund 6 |
| 48 | hoch | quer | `Dokumentation/LIESMICH.md` | = Befund 5 | siehe Befund 5 |
| 49 | mittel | quer | V6/Bild 5 gegen Schwesterpapier 1.5 | Leser nimmt einen Strom oder einen Pfad, mit oder ohne Profil | Der Entwurf nennt jetzt **Strom, Profil, Melder, Abbruchzeichen**, sagt ausdrücklich, dass die **Signatur des Schwesterpapiers verbindlich** ist, und begründet die empfohlene Angleichung in drei Punkten (der Schreiber nimmt schon einen Strom; iOS liefert den Zugriff als Strom; die Größenprüfung eines gepackten IFC läuft gegen die entpackte Größe). So entsteht kein zweiter Vertrag, während der zweite Leser am Schwesterpapier arbeitet |
| 50 | mittel | quer | V8 und Bild 5 | `Gebaeudewege` ist keine Naht des Rechenwegs | V8 umbenannt in „**Plattformnaht der Gebäudemaske** (Gaben-Haken)" mit dem Zusatz, dass sie in `EPOS.UI.Daten` liegt und die Rechennaht im Kern; die Kante im Klassenbild gestrichen |
| 51 | mittel | quer | 2.1 Bild 1 | `CTRL --> EING` und `LES --> CTRL` zeigen falsch; der Lauf fehlt im Bild | Knoten **Lauf und Kernnaht** und **Importablauf** ergänzt; `CTRL --> LAUF --> EING --> PHY`; der Leser hängt am Ablauf, der Controller ist nur Schreibziel der Übernahme (`ABL --> CTRL`) |
| 52 | mittel | quer | 5.6 gegen Schwesterpapier 2.9 | Hinweis **vor** dem Schreiben gegen Banner **nach** dem Schreiben | Auf einen Weg festgelegt, wie im Befund empfohlen: **Rückfrage vor dem Schreiben** mit der Zahl der Zonen, danach die Bestätigung — das ist die Staffel aus Kapitel 6. Dazu der Satz des Schwesterpapiers, dass ein zurückgeholtes Gebäude ohne Zonen den Klassenweg rechnet |
| 53 | mittel | quer | F7 gegen Schwesterpapier 4.3/4.4 | Sieben geforderte Größen gegen fünf Kennzahlen; keine Zuordnung | F7 aufgeteilt: **fünf im Bericht** (Schwesterpapier 4.3), **zwei zusätzlich im Referenzlauf-Export**; dazu die Auflage, dass eine später in den Bericht gehobene Größe ihre Aggregationsregel mitbringt |
| 54 | mittel | quer | 7.2 | „Vorgabe, nicht hart" und „lehnt benannt ab" widersprechen sich | Zeile in zwei geteilt: **Import = weich**, Warnung mit Rückfrage (K2); **Rechnung = hart**, benannte Ablehnung (K3) mit Meldungsschlüssel des Präfixes `GEBP_`. Die Zahl 50 ist ein Datum des Prüfsatzes, nicht eine Konstante im Quelltext; der Messauftrag bleibt in Kapitel 11 |
| 55 | mittel | quer | 8.3 gegen Schwesterpapier 2.8 | Die Einfrierkette steht zweimal vollständig und ist bereits auseinandergelaufen | **Teilweise übernommen, mit Begründung:** Die Skizze verlangt für den Systementwurf ausdrücklich ein Bild der Einfrierkette, also bleibt es — korrigiert nach Befund 6/47. Gestrichen ist die **Schritttabelle mit den Nachweisen je Schritt**; an ihrer Stelle stehen die drei Anlässe, die Klimaregel, die G6d-Vorbedingung und der Verweis auf Schwesterpapier 2.8 |
| 56 | mittel | quer | 3.3, 3.4, 5.5, 7.2, 9 | Vier Zahlenpaare und zwei Festlegungen mehrfach | Größengrenzen: nur noch als „vier Zahlen, je Format und Plattform eine — Schwesterpapier 1.5, Regel 2" in 3.3 und 7.2. Produktausweis/Wasserzeichen: Wortlaut nur noch in Kapitel 9, 3.4 nennt den Ort im Ablauf. 5.5 auf die Systemeigenschaft verdichtet, Tabellen und Stand stehen in Schwesterpapier 2.9. Die Zeilenzahl 3 500 steht nur noch in 5.2 und 7.1 |
| 57 | niedrig | quer | 6, Bild 7 | = Befund 9 | siehe Befund 9 |
| 58 | niedrig | quer | 3.1 | = Befund 8 | siehe Befund 8 |
| 59 | niedrig | quer | 1.2 N10 | „gemessen / gelesen / Katalog / Vorgabe" sind nicht die verbindlichen fünf Werte | N10 auf die fünf Persistenzwerte (`MANUELL`, `KATALOG`, `IFC`, `GBXML`, `VORGABE`) gestellt, mit Verweis auf Schwesterpapier 2.2 / W9 und dem `CHECK` als Nachweis |
| 60 | niedrig | quer | 11 | „jede neue Kindtabelle hängt am Gebäude" trifft wörtlich auf die meisten nicht zu | Umformuliert: **kein Kind bekommt ein eigenes Projektfeld**; es hängt über seinen unmittelbaren Elternteil (Gebäude, Zone, Aufbau, Importquelle) am Projekt, und die Projektkataloge tragen ihr Projektfeld wie ihre Vorbilder im Bestand — zugleich die Bedingung des Projekttransfers |

## Zählung

| Schwere | Einträge | davon vollständig in diesem Papier | außerhalb der beauftragten Dateien |
|---|---|---|---|
| **hoch** | 14 | 10 | 4 (Nr. 5, 22, 41, 48 — vier Nummern derselben Indexsache) |
| **mittel** | 28 | 27 | 1 (Nr. 10 — der Rechenfehler in Befund V 6.2/0.7) |
| **niedrig** | 18 | 18 | — |
| **Summe** | **60** | **55** | **5** |

Bei allen fünf ist der **Papierteil** erledigt; offen ist je ein Handgriff an einer Datei, die
dieser Auftrag nicht anfasst. Die Zählung hat damit dieselbe Form wie im
[Schwesterprotokoll](2026-09-15_Gegenlesen_Softwarearchitektur.md).

Davon **13 Doppelnennungen** derselben Sache durch verschiedene Leser (3 = 19 = 46; 2 = 20;
5 = 22 = 41 = 48; 4 = 44; 8 = 33 = 58; 9 = 26 = 57; 1 = 21 = 42; 6 = 47; 7 = 28; 18 = 31;
13 = 30; 43 = 49) — sie sind einmal gelöst und in der Tabelle zurückverwiesen.

## Vier Entscheidungen, die beim Einarbeiten zu treffen waren

1. **Zwei Leser widersprachen sich über die dritte Umrechnungsnaht** (Befund 45 gegen den Wortlaut
   des Entwurfs). Der Quelltext entscheidet: `EPOS.Kern/Controller/GebaeudeBedarfCtrl.cs:130`
   rechnet eine Energiemenge um. Die Regel wird fortgeschrieben, nicht gedehnt — und der Entwurf
   sagt jetzt dasselbe wie sein Schwesterpapier.
2. **Befund 55 wollte Bild und Tabelle der Einfrierkette streichen, Befund 6 und 47 wollten sie
   berichtigen.** Die verbindliche Skizze verlangt für dieses Papier ein Bild der Einfrierkette;
   also bleibt das Bild (berichtigt), und die doppelte Nachweistabelle geht.
3. **Die Ordnungszahl der neuen Einfrierregel** (Befund 13, 30) ließ sich nicht durch Abgleich mit
   dem Schwesterpapier lösen, weil dort parallel gearbeitet wird. Gewählt ist die Form, die mit
   jeder Nummerierung verträglich ist: Benennung über den Gegenstand, Nummer bei der Beauftragung.
4. **Ein siebter Punkt in Kapitel 0** (Befund 36) weicht von der Skizze ab. Er war nötig, weil
   derselbe Punkt Datenaustausch **und** E11 trägt und beides für die Projektverantwortung zum
   Ergebnis gehört.

## Was außerhalb dieses Auftrags liegt

- **Die Indexzeilen in `Dokumentation/LIESMICH.md`** (Befunde 5, 22, 41, 48) — die ganze Familie der
  Gebäudesimulationspapiere fehlt dort. Der Orchestrator pflegt Index, Statusdatei und ADR.
- **Die Rechenfehler in Befund V 6.2 und 0.7** (Befund 10). Sie sind Belegquelle und liegen
  außerhalb der beauftragten Dateien; die Zahl im Entwurf ist berichtigt.
- **Alle Änderungen am Schwesterpapier** (Befunde 42, 43, 44). Sie gehören dem zweiten Leser; die
  Namen sind über die Skizze abgestimmt.
- **`EPOS.Kern/CLAUDE.md`, Einheitenregel Punkt 4** (Befund 45). Sie ist im selben Merge
  fortzuschreiben, in dem die Gebäudesimulation die Auskunft anfasst.

---

## Nachtrag: zweite unabhängige Prüfung (16.09.2026)

Eine zweite, unabhängige Prüfung beider Papiere hat neun halb oder gar nicht erledigte Punkte
gefunden, dazu vier Restdivergenzen zwischen den Papieren. Die den Systementwurf betreffenden sind
hier eingearbeitet; die Zählung oben ist zugleich auf die Form des Schwesterprotokolls gebracht.

| Stelle | Was noch fehlte | Erledigung |
|---|---|---|
| Kap. 4, Absatz „Zur Signatur des Lesers" | Der Absatz gab die Fassung des Schwesterpapiers 1.5 als `Lesen(pfad, melder, abbruch)` wieder, erklärte sie für verbindlich und empfahl erst die Angleichung — die mit Befund 43/49 erledigte Frage also erneut gestellt, obwohl beide Papiere längst dasselbe führen | Absatz neu gefasst: Strom, Profil, Melder und Abbruchzeichen sind die Fassung **beider** Papiere; die drei Gründe bleiben als Begründung, nicht als Empfehlung |
| Kap. 4, Bild 5 | `GebaeudeModellEingang.Bauen(…, ort)` — der Sammelparameter, den das Schwesterpapier 1.3 weder in der Tabelle noch im Klassenbild führt | auf `Bauen(gebaeude, solarOrtszeit, wochenende, laengengrad, breitengrad)` gebracht |
| Kap. 4, Bild 5 | `IDateiDienst` mit drei `*Async`-Gliedern und ohne `OrdnerWaehlen` — die Fassung, die der Quelltext widerlegt | alle **sieben** Glieder gezeichnet; dazu der neue Absatz „Zur Signatur des Dateidienstes" mit den Belegen `EPOS.Kern/Allgemein/Dienste/IDateiDienst.cs:19`, `:25`, `:28`, `:34`, `:63` (die synchrone Form **ist** die Schnittstelle) und `:107`, `:115`, `:123` (die `*Async`-Zwillinge sind Standardimplementierungen) |
| 3.1, letzter Punkt | A16 als ganze Frage „wo die Verzweigung relativ zum Abbruch sitzt" geführt, obwohl das Schwesterpapier sie auf `:647` verengt hat | der Zuschnitt am Punkt im Rumpf (`:581`) steht jetzt als **entschieden** (Umsetzungskonzept 1.5); offen ist allein `:647` und das Durchreichen der Modellwahl |
| 3.1 und Bild 2 | die Ordinalzahlen der zwei Verzweigungspunkte gegenüber dem Schwesterpapier 4.1 vertauscht | Zählung des Schwesterpapiers übernommen: `:647` ist der **erste**, `:581` der **zweite**; Bild 2 und der Satz zum Abbruch nachgezogen |
| Kap. 12 | der Widerspruchsbereich auf W1–W18 verkürzt; das Schwesterpapier 2.3 löst dort zusätzlich W19–W21 | Zeile auf **W1–W21** gestellt, die Herkunft beider Gruppen getrennt benannt |
| Zählung | „Alle 60 Befunde sind eingearbeitet; keiner blieb offen" gegen die eigene Liste „Was außerhalb dieses Auftrags liegt" (Nr. 5, 22, 41, 48 und 10) | auf die Form des Schwesterprotokolls gebracht: 60 Einträge, 55 vollständig in diesem Papier, 5 mit einem Rest außerhalb |

**Weiterhin außerhalb dieses Auftrags:** die Indexzeilen in
[`Dokumentation/LIESMICH.md`](../../LIESMICH.md) — die Zeile für dieses Protokoll steht dort, die
für das [Schwesterprotokoll](2026-09-15_Gegenlesen_Softwarearchitektur.md) fehlt als einzige der
Familie; den Index pflegt der Orchestrator. Ebenso die Rechenfehler in Befund V 6.2 und 0.7.

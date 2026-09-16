# Gegenlesen der Softwarearchitektur Gebäudesimulation (15.09.2026)

**Papier:** [`Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md`](../Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md)
— gegengelesen als Rev. 1 (1 778 Zeilen), nach Einarbeitung **Rev. 2** (1 991 Zeilen).

Fünf Leser haben die beiden Architekturpapiere unabhängig voneinander geprüft, jeder aus einem
eigenen Blickwinkel. Zwei lasen das Schwesterpapier
([Systementwurf](../Systementwurf_Gebaeudesimulation_EPOS-Plan.md)), zwei dieses Papier, einer beide
gegeneinander; ihre Einträge zu „beiden Papieren" stehen in diesem Protokoll, soweit sie dieses
Papier treffen.

| Kürzel | Leser | Blickwinkel |
|---|---|---|
| **S-B** | 1 | **Systementwurf gegen den Arbeitsbaum** — stimmt jede Datei-, Zeilen- und Namensangabe? |
| **S-K** | 2 | **Systementwurf gegen die geltenden Papiere** — Entscheide E1–E11, ADR, Hausregeln |
| **A-B** | 3 | **Softwarearchitektur gegen den Arbeitsbaum** — Klassen, Zeilen, Wächter, Schemastand |
| **A-K** | 4 | **Softwarearchitektur gegen die geltenden Papiere** — Konzepte, ADR, Entscheide, Befunde |
| **Q** | 5 | **Quer** — beide Papiere gegeneinander und gegen den Auftrag: Doppelungen, Widersprüche, Lücken |

**Phase:** Kritik nach der verbindlichen Skizze (Gliederung, Namen, Auflösung der Widersprüche
W1–W21, Entscheidevorschläge, Grenzen). Die Skizze bleibt Maßstab: Wo ein Leser einen anderen Namen
vorschlug als die Skizze, gilt die Skizze; wo die Skizze eine Sache nicht nennt — der
Gebäudebetrachter nach **E11** —, sind die Namen hier neu gesetzt, wie es
[Konzept N1.16](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) ausdrücklich diesem Papier
überlässt.

**Vier Widersprüche zwischen Lesern wurden am Quelltext geklärt** und sind unten in der Spalte
„Erledigung" mit **abweichend** vermerkt: die Zahl der Verzweigungspunkte und ihre Reihenfolge
(`SimulationWaermebedarf.cs`), der Verbleib der Wohnflächenhülle, die Nummerierung der
Einfrierregeln und die Frage, ob der Wortlaut des Produktausweises in dieses Papier gehört.

---

## Die Einträge

| Nr. | Schwere | Leser | Stelle | Befund | Erledigung |
|---|---|---|---|---|---|
| 1 | hoch | S-B | durchgehend | **E11** (Gebäudebetrachter, ein Zonengeometrie-Modell) fehlt in beiden Papieren, obwohl entschieden — neuer Kernbaustein, neue Komponente, neues Fremdstück im Browser | eingearbeitet: E11 im Vorwort und in Kap. 0 Punkt 1; `Zonengeometrie`/`Zonenumriss` in 1.2 und 1.3 (zwei Fabrikwege, drei Abnehmer); `GebaeudeAnsicht` in 1.2, 3.2, 3.4; Präfix `GAN_` in 3.6; `three.js` in 1.2, 3.7, 4.5; Geometriequelle in 4.6; Stufen G6c und G7b in Kap. 5 |
| 2 | hoch | S-B | `Dokumentation/LIESMICH.md` | Keine Indexzeile für die Papiere der Gebäudesimulation; der Wächter prüft die Indexpflicht als eigenen Fall | **nicht in diesem Papier:** Den Index pflegt der Orchestrator. Hier ist **AR14** um die Indexpflicht erweitert und um den Satz, dass die Indexzeile im **Abnahme-Commit** entsteht |
| 3 | mittel | S-B | 2.8, W18 | „gesäte Klimareihen" als **sechste** Regel mit M4, „gesäte Zonendaten" als **fünfte** mit G6d — M4 läuft aber lange vor G6d, es entstünde eine Nummernlücke | eingearbeitet, **abweichend**: Die Ordinalzahlen fallen **ganz** weg; jede Regel heißt nach ihrem Gegenstand („gesäte Gebäudedaten", „gesäte Klimareihen", „gesäte Zonendaten") — in W18, 2.4, 2.8, Bild 7 und Kap. 5 |
| 4 | hoch | S-K | 1.3, 2.1/2.2, 9, 12 (Schwesterpapier) | Dasselbe wie 1, aus Sicht der Entscheide: E11 hat dieselbe Verbindlichkeit wie E1–E10 | siehe 1; Anforderung F17, Komponentenbild und Kapitel 9 betreffen das **Schwesterpapier** |
| 5 | hoch | S-K | Ablage | Wie 2 | siehe 2 |
| 6 | hoch | A-B | 1.1, 1.7, 2.1, 2.4–2.9, Kap. 5 | Die Schemaschritte **77 und 78 sind vergeben** (`SchemaStand.Zielversion = 78`), die Kette 77–83 damit falsch; Befund V geht noch von 76 aus | eingearbeitet: **alle** harten Nummern aus Text, Tabellen und Mermaid-Beschriftungen entfernt, Adresse sind die Papiernamen; 2.4 nennt den Zielstand **78** mit Beleg (`EPOS.Kern/Allgemein/Update/SchemaStand.cs:106`), die nächste freie **79** und die überholten Stellen |
| 7 | hoch | A-B | Kap. 0 Punkt 5, 1.6, 4.1, A16 | `:647` liegt **nicht** in `HeizwaermeEinesGebaeudes` (566–611), sondern in `Bewohner_und_Flaeche_berechnen` (613–656), gerufen bei `:575` — also **vor** `:581`; 4.1 widersprach sich selbst | eingearbeitet: 4.1 neu formuliert (566 → 575 → 647 → 581), „Alles davor bleibt unberührt" auf die Bewohner- und Flächenermittlung eingeschränkt, Kap. 0 Punkt 5 und 1.6 nachgezogen, A16 verengt; Beleg zusätzlich ADR-002, Entscheidung Punkt 3 |
| 8 | hoch | A-B | 4.4 | Alle drei neuen Vektordateien pauschal „in kWh" — zwei davon führen **°C** | eingearbeitet: eigene Zeile „Die Einheit je Datei" (Kühlbedarf kWh, zwei Temperaturreihen °C, Muster `stundentemperatur.csv`), Vektorsumme als Vergleichsanker benannt, Toleranzfrage zum ersten Einfrieren |
| 9 | hoch | A-B | 1.4, 2.7, A6 | „Löschen + Neuanlegen" im Zonencontroller zerstört `Tab_Importzuordnung` (ohne Kaskade) und die Normierung von `Tab_Zonenluftstrom` | eingearbeitet, Variante „Ändern statt Löschen": 1.4 und A6 auf den **Abgleich über die Ids** (Entfernen → Ändern → Anlegen, eine Transaktion) umgestellt; 2.7 nennt die Wirkung auf Importzuordnung und Luftstrom und trennt den Gebäude-Löschweg (Kaskadenrettung) vom Zonen-Schreibweg |
| 10 | hoch | A-B | `Dokumentation/LIESMICH.md` | Wie 2 | siehe 2 |
| 11 | mittel | A-B | 1.1 | `EnableWindowsTargeting=false` steht in **sechs** csproj, nicht fünf | eingearbeitet, alle sechs namentlich |
| 12 | mittel | A-B | 1.7 AR8 | `DatenzugriffTests` trägt die Regel nicht, `EPOS.UI/CLAUDE.md` führt keinen grep-Block | eingearbeitet: **AR8a** (Parameter — `DatenzugriffTests`, Texte — `SqlDialektPruefer`, Gegenbeispiel `GebaeudeKatalogHuelle.cs:309`) und **AR8b** (Hausregel `EPOS.UI/CLAUDE.md:14`, **bislang unbewacht**); Satzzahl in 1.7 richtiggestellt |
| 13 | mittel | A-B | 2.5 Bild, 2.9 | Der Katalogeditor liest **nicht** die Sicht, sondern `GebaeudeStammCtrl`/`Tab_Gebaeude_STAMM` | eingearbeitet: Kante `L1 --> L4` entfernt, eigener Knoten für die Stammtabelle, Absatz in 2.5 samt Beleg `:308-309`, eigene Leseweg-Zeile in 2.9 |
| 14 | mittel | A-B | 2.1 | Die erDiagramme führen sechs **Sammelnamen**, die keine Spalten sind | eingearbeitet: echte Spaltennamen, Sammelhinweis „dazu …", Zeile `TEXT weitere "… siehe 2.2"`, `genau_ein_Ziel` in die Beziehungsbeschriftung |
| 15 | mittel | A-B | 1.1, 1.2, 1.4, 3.7 | `GebaeudeFenster.cs`/`GebaeudeKatalogFenster.cs` gibt es nicht; `GebaeudeWohnflaecheHuelle` fehlt in der Zielliste, `GebaeudeBedarfHuelle` gibt es nicht | eingearbeitet, **abweichend**: Die Fensterdateien sind als **Rümpfe** der zwei Hüllen ausgewiesen, die heute ein Fenster öffnen; `GebaeudeWohnflaecheHuelle` zieht als **reiner Gabenbauer** mit — ihr Fensterweg `Oeffnen` hat schon heute **keinen Aufrufer** (nur `.Gaben` wird gerufen) und fällt weg; `GebaeudeBedarfHuelle` ist als **neu** gekennzeichnet; Hüllenzahl auf acht (neun mit G7) |
| 16 | mittel | A-B | 3.3 Punkt 3 | Die Hausregel war umgekehrt wiedergegeben | eingearbeitet, im Wortlaut der Hausregel: der gescheiterte Schritt **wird** beim zweiten OK erneut versucht, das Geschriebene nicht |
| 17 | mittel | A-B | 4.4 | Die Dateinamen folgen dem genannten Muster nicht | eingearbeitet: `gebaeude_<n>_raumtemperatur.csv` usw. — Gegenstandsvorsatz, Kennung, Größe, wie `quellspeicher_<n>_soc.csv` |
| 18 | mittel | A-B | 1.5 | `IDateiDienst` trägt die synchrone Form; die `*Async`-Zwillinge sind Standardfassungen mit Rückfall, `OrdnerWaehlen` fehlte im Vertrag | eingearbeitet, mit Zeilenbelegen; „asynchron ist Pflicht" ist jetzt als **Voraussetzung an die Adapter** formuliert, nicht als Zusage der Schnittstelle; `OrdnerWaehlen(Async)` auf iOS benannt nicht verfügbar |
| 19 | mittel | A-B | 3.4 Sequenzbild | Der Ablehnungspfad ohne `alt`-Rahmen liest sich als „ablehnen und trotzdem weiterlesen"; der Ablauf antwortete der Komponente | eingearbeitet: `alt`/`else` um den ganzen Leseast, Absage über die **Hülle**, Prüfschritt dort, wo `MaxBytes` liegt |
| 20 | niedrig | A-B | 4.3 | „sieben Stücke", die Klasse führt acht Felder | eingearbeitet: acht |
| 21 | niedrig | A-B | 1.8 | `Referenzlaeufe/Importproben/` gibt es schon | eingearbeitet: „im vorhandenen Ordner", dazu die Namensregel `ifc_`/`gbxml_` |
| 22 | niedrig | A-B | 1.3 | Die XML-Doku der Wächterliste nennt die Dateizahl im Wortlaut | eingearbeitet: dritter Handgriff desselben Merges — `:195-200` und `:447` auf neun ziehen |
| 23 | hoch | A-K | 0, 1.2, 3.2, 3.4, 3.6, 4.5, 4.6, Kap. 5, Kap. 7 | Wie 1, aus Sicht der Papiere: E11 verlangt Kernklassen, Komponente, Lizenzhinweisseite, Geometriequelle | siehe 1; zusätzlich ist der Satz „genau **eine** neue Paketzeile" zu „ein NuGet-Paket **und** eine lokal ausgelieferte JS-Bibliothek" fortgeschrieben |
| 24 | hoch | A-K | 2.4 | **S-E** steht in zwei geltenden Papieren für den `CopyFromStamm`-Umbau, hier für den Kopplungsschritt | eingearbeitet, Variante (a): S-E bleibt der `CopyFromStamm`-Umbau und bekommt eine **eigene Zeile** (kein DDL, Sperrpunkt und Bestandteil von M3); der Kopplungsschritt heißt **S-G**. Begründung im Text unter der Tabelle |
| 25 | hoch | A-K | 2.2, 2.4, 1.7 | `STRICT` und `IF NOT EXISTS` fehlen genau dort, wo die Tabellen festgelegt werden — obwohl 4.5 die `STRICT`-Zahl voraussetzt | eingearbeitet: Vorspann vor die Tabellenliste in 2.2 (STRICT, AUTOINCREMENT, IF NOT EXISTS, `CHECK (length(...))`, Kaskade nur zum Eltern), Satz in 2.4 (die Zahl wächst, Prüfmodus und Seed nachziehen), **AR15** in 1.7 |
| 26 | mittel | A-K | 1.5, 3.4 | `IGebaeudeLeser` nahm einen **Pfad** ohne Profil, der Schreiber einen `Stream` mit Profil | eingearbeitet: `Lesen(Stream quelle, GebaeudeImportProfil profil, IProgress<ImportFortschritt> melder, CancellationToken abbruch)` — in der Nahttabelle, im classDiagram und im Sequenzbild, mit Begründung |
| 27 | mittel | A-K | 1.5 Regel 2, 3.4 | ADR-003 verlangt bei `.ifczip` die **entpackte** Größe | eingearbeitet, mit dem Zusatz „Behälter öffnen, nicht entpacken" und „Behälter nicht lesbar" als eigener benannter Ablehnung; im Zustandsbild und im Sequenzbild sichtbar |
| 28 | mittel | A-K | 3.1, 1.1, 3.7 | Für keine Maske war gesagt, **wer** den Maskenschlüssel beantwortet; die Übersetzungszeile in `IosNavigation` läuft ohne Fall in der Wurzel ins Leere | eingearbeitet: neue Spalte **„Beantwortet von"** in 3.1 samt Zeile für die zwei Bestandsmasken (Windows: Hülle, iOS: Fall in `AppWurzel`), zwei Folgerungen im Text, 1.1 und 3.7 nachgezogen; Beleg `EPOS.iOS/Dienste/IosNavigation.cs:64-78` |
| 29 | mittel | A-K | Kap. 5 G6a gegen W1, 2.1, 2.2 | Der Kopplungsschritt stand an **zwei** Stufen | eingearbeitet: Er steht bei **G6b** (so W1 und beide Bilder in 2.1); die Zeile G6a ist entsprechend gekürzt, die Abnahme von G6b um „Migrationstests grün" erweitert |
| 30 | mittel | A-K | 2.4 S-D, 2.6 | Der einzige Schritt, der **nur** aus Registerpflege besteht, war in der Sache leer | eingearbeitet: 2.6 um `SchemaKatalog` („sonst startet das Programm nicht") und `KatalogRegistry` (`BAUSTOFF`, `BAUTEILAUFBAU`) erweitert; die S-D-Zeile in 2.4 zählt die Handgriffe samt Verweisen auf 3.1 und 3.6 |
| 31 | mittel | A-K | 1.2, 3.2, 3.6, 3.8, 4.6 | Der Gebäudeexport hatte Präfix, Hülle und Einstieg, aber keine Maske und keine Dateien | eingearbeitet: `GebaeudeExportDialog.razor` + `…Daten.cs` (`EPOS.UI/Dialoge/Export/`) und `GebaeudeExportHuelle.cs` in 1.2, Zeile in 3.2, Maskenzeile in 4.6, Kap. 5 G7; in 3.8 „fünf bis G6, mit dem Export sechs" und die Dialogkatalogzahlen **zwölf**/**dreizehn** |
| 32 | mittel | A-K | 4.6, 1.4, 1.6 | Für den Export las der **Ablauf** die Datenbank — im Import ist genau das verboten | eingearbeitet: Die **Hülle** liest über die vorhandenen Controller **vor** dem Fadenwechsel und gibt den fertigen Satz hinein; 1.6 hat dafür einen fünften Punkt, und es entsteht **kein** Exportcontroller |
| 33 | mittel | A-K | 2.2, 2.1 | Drei Schreibweisen für eine Spalte (`Fensterflaeche_Ost_West` / `_OstWest` / `_Ost_und_West`) | eingearbeitet: NULL-Bedeutung auf die **Bestandsspalte** `Fensterflaeche_Ost_West` bezogen, Modellfeld ab M2 in Klammern, erDiagram berichtigt, Satz „die Spalte bleibt und wird nicht umbenannt" ergänzt |
| 34 | mittel | A-K | 2.2 | `Tab_Gebaeude.Luftwechselrate` blieb ungenannt, obwohl sie Eingang des Klassenwegs und Gegenstand einer Einfrierregel ist | eingearbeitet: eigener Absatz — Spalte bleibt, im Stundenweg gilt `Luftwechsel_Infiltration + Luftwechsel_Nutzer` mit Rückfall auf `Luftwechselrate`, Herleitungszeile statt stillem Überschreiben, dieselbe Kette für die Zone |
| 35 | mittel | A-K | Kap. 6 A16 | A16 war unter Umsetzungskonzept 1.5 bereits entschieden | eingearbeitet, Variante (b): A16 auf das **neu** Offene verengt — Zuschnitt und Durchreichen der Modellwahl am zweiten Punkt (`:647`); der erste Punkt ist als entschieden benannt. Die Zahl der Fragen bleibt davon unberührt |
| 36 | mittel | A-K | 3.2, Kap. 6 | U2 (Feldsichtbarkeit je Schalterstellung) ist ein Maskenzustand und fehlte | eingearbeitet: neue Regel 4 in 3.2 (verstecken, Herleitungszeile in beiden Stellungen, Feldbestandstest prüft beide) und **A19** als Sperrpunkt (= U2); Zählungen in Kap. 0, Kap. 6 und Kap. 7 nachgezogen |
| 37 | mittel | A-K | 4.3, 4.6, A12 | Der Produktausweis nach E10 wird zur Pflicht erklärt, aber nirgends wiedergegeben oder verwiesen | eingearbeitet, **abweichend**: **kein Blockzitat** — dieses Papier führt keine Zahlen des Nachweises. Stattdessen die eine Quelle ([ADR-002](../ADR-002_Stundenmodell_VDI6007_Einbindung.md), Entscheidung Punkt 7) und **ein** Ressourcenschlüssel in beiden `.resx` (3.6), aus dem Bericht, Wiki-Seite und Exportdatei denselben Text ziehen |
| 38 | niedrig | A-K | 1.1, 1.7 | Die Verbotsliste des Kerns war unvollständig (`SpecialFolder`, `ProtectedData`, `System.Drawing`) | eingearbeitet: Zelle vervollständigt, dazu der Satz, dass die zwei `git grep`-Wächter nach jedem Merge der Import- und Exportfamilie leer bleiben müssen |
| 39 | niedrig | A-K | 1.3 | Sammelparameter „ort" ohne Typ | eingearbeitet: `laengengrad`, `breitengrad` in Tabelle und classDiagram, wie Umsetzungskonzept 1.4 |
| 40 | niedrig | A-K | 1.8, Kap. 5 G0 | Die Prüfklasse der Normfälle war nicht benannt | eingearbeitet: `EPOS.Kern.Tests/GebaeudeModellNormfallTests` in 1.8 und in der Abnahme von G0 |
| 41 | hoch | Q | 4.1, 0.5 | Wie 7 (aus dem Quervergleich beider Papiere) | siehe 7 |
| 42 | hoch | Q | 1.7 gegen Systementwurf 3.1/11 | Widerspruch **zwischen** den Papieren über die Zahl der Umrechnungsnähte | Fassung dieses Papiers bleibt (sie ist belegt) und ist **geschärft**: `:130` (`werte.Sum() / 1000`) ist die Energienaht, `:121` (`WattToKw`) eine Leistungsumrechnung; dazu der Satz, dass der Wächter die **Namensliste** prüft. Die Gegenfassung ist im **Schwesterpapier** zu berichtigen |
| 43 | hoch | Q | 4.4 | Wie 8 | siehe 8 |
| 44 | hoch | Q | `Dokumentation/LIESMICH.md` | Wie 2 | siehe 2 |
| 45 | mittel | Q | 1.5 gegen Systementwurf 4.1 | Wie 26 | siehe 26; das Schwesterpapier zieht die Signatur nach oder verweist auf 1.5 |
| 46 | mittel | Q | 1.1 Bild | Die Kante ließ die Razor-Komponenten unmittelbar auf die Kern-Controller zeigen | eingearbeitet: `UI --> KCTRL` ersetzt durch `UID --> KCTRL`; `UID --> UI` ist als Gaben- und Ergebnis-Kante beschriftet |
| 47 | mittel | Q | 2.9 gegen Systementwurf 5.6 | Hinweis **nach** dem Schreiben gegen Hinweis **vor** dem Schreiben | eingearbeitet: `Rueckfrage`-Komponente im OK-Weg **vor** dem Schreiben, danach das Bestätigungsbanner — mit dem Grund aus der Meldungsstaffel |
| 48 | mittel | Q | 1.2, 3.2, 3.8 | Wie 31 | siehe 31 |
| 49 | mittel | Q | 3.2, 1.2, Kap. 5 | Stufe von `ZonenDialog`/`BauteilDialog` widersprüchlich; in G3 gäbe es Bauteiltabellen, aber keinen Weg, ein Bauteil zu erfassen | eingearbeitet: `BauteilDialog` und der Zonenreiter in der **Grundform mit G3**, `ZonenDialog` mit G6b um Mehrzonenfelder und Luftaustausch erweitert; 1.2 je Datei ausgezeichnet, Kap. 5 (G3, G6b) nachgezogen |
| 50 | mittel | Q | 2.6 | „Projekttransfer: nichts zu tun" — er erbt die Tabellenmenge aus `ErmittlePlan()`, und fünf neue Tabellen tragen kein `ID_Projekt` | eingearbeitet: Zeile neu geschrieben samt Beleg (`ProjektExportImportCtrl.cs:131`), `KINDER` als einziger Reiseweg und einer benannten Abnahme (ausgeben, in leere Datenbank einlesen, Zeilen zählen) |
| 51 | mittel | Q | 1.2, 3.7 | Wie 15 | siehe 15 |
| 52 | mittel | Q | 3.1 gegen 1.1/1.2 | Wie 28 (Schlüsselform der neuen Katalogeditoren) | siehe 28; die zwei Katalogeditoren sind ausdrücklich als **freie Ansicht** in `AppWurzel.razor` festgelegt — erstmals ein Katalogeditor ohne Plattformhülle, mit Begründung |
| 53 | mittel | Q | 4.3, 4.4 | Die Anforderung nennt sieben Größen je Gebäude, der Katalog fünf | eingearbeitet: fünf Kennzahlen im Bericht; „Stunden mit Kühlbedarf" und die Gebäudespitze **nur** als Skalar im Referenzlauf-Export, jeweils mit Grund (Aggregation bzw. `Waermelast_Max`) |
| 54 | mittel | Q | 2.8 gegen Systementwurf 8.3 | Die Einfrierkette steht zweimal vollständig und ist bereits auseinandergelaufen | Dieses Papier behält sie (Einfrierplan ist Teil 2 hier) und sagt es nun im Vorspann von 2.8; Bild und Schritttabelle im **Schwesterpapier** zu streichen |
| 55 | mittel | Q | 1.5, 1.6, 4.3, 2.9 | Vier Zahlenpaare und zwei Festlegungen stehen doppelt; „3 200" gegen „3 500 Zeilen" | eingearbeitet, soweit dieses Papier betroffen: Die vier Größengrenzen stehen **allein** in 1.5 (mit dem Satz dazu); 1.6 Punkt 2 nennt **3 500** Zeilen und erklärt die 3 200 des Befunds; Produktausweis und `Tab_DBTagV` bleiben hier geführt, das Schwesterpapier verweist |
| 56 | niedrig | Q | 2.1 | Wie 14 | siehe 14 |
| 57 | niedrig | Q | 1.7 | „Vier Wächter kommen hinzu" — drei sind neu | eingearbeitet: „Drei Wächter kommen hinzu, ein vorhandener bekommt neue Einträge" |
| 58 | niedrig | Q | 2.3 W16 | „jede Kindtabelle hängt über `ID_Gebaeude`" trifft wörtlich auf die meisten nicht | eingearbeitet: W16 auf die **Projektbindung** umformuliert (kein eigenes `ID_Projekt`, Bindung über den unmittelbaren Elternteil), Projektkataloge ausgenommen, `ErmittlePlan()` als Bedingung genannt |
| 59 | niedrig | Q | 1.3, 4.5, A18 | „Prüfmodus" trug zwei Bedeutungen | eingearbeitet: „**Prüfmodus der iOS-Schale**" in 4.5 und im Bild der Nähte; in 1.3 der Zusatz „nicht zu verwechseln mit …" |
| 60 | niedrig | Q | 4.4 | `<n>` als Schleifenindex verschiebt alle Dateinamen | eingearbeitet: `<n>` ist `ID_ProjektGebaeude` mit laufendem Zähler als Rückfall, wie `ID_Anlage` bei den Speicherdateien (`Referenzlauf/Ergebnisexport.cs:90-101`) |

---

## Zählung

| Schwere | Einträge | davon eingearbeitet | darunter abweichend gelöst | außerhalb dieses Papiers |
|---|---|---|---|---|
| **hoch** | 16 | 12 | — | 4 (Nr. 2, 5, 10, 44 — vier Nummern derselben Indexsache) |
| **mittel** | 33 | 32 | 5 (Nr. 3, 15, 35, 37, 51) | 1 (Nr. 54 — Kürzung im Schwesterpapier) |
| **niedrig** | 11 | 11 | — | — |
| **Summe** | **60** | **55** | **5** | **5** |

Nach Leser: **S-B** 3, **S-K** 2, **A-B** 17, **A-K** 18, **Q** 20. Doppelbefunde sind über die
Spalte „Erledigung" zusammengeführt (1/4/23, 2/5/10/44, 7/41, 8/43, 14/56, 15/51, 17/60, 26/45,
28/52, 31/48) und **nicht** entdoppelt gezählt, damit jede Lesernummer wiederzufinden ist.

Die fünf Einträge außerhalb dieses Papiers sind **vier Nummern derselben Sache** (die Indexzeilen in
[`Dokumentation/LIESMICH.md`](../../LIESMICH.md) — sie pflegt der Orchestrator im Abnahme-Commit) und
**eine** Kürzung, die im Schwesterpapier geschieht (Nr. 54). Zwei Einträge trafen zusätzlich das
Schwesterpapier und sind dort nachzuziehen: Nr. 42 (Zahl der Umrechnungsnähte) und Nr. 45
(Signatur des Lesers).

## Was dabei am Quelltext geklärt wurde

- **Die Reihenfolge der zwei Verzweigungspunkte** (Nr. 7, 41): `HeizwaermeEinesGebaeudes` reicht von
  `:566` bis `:611`; `:647` liegt in `Bewohner_und_Flaeche_berechnen` (`:613-656`), das bei `:575`
  gerufen wird. Die Verbrauchsrückrechnung läuft also **vor** der Modellwahl des Bedarfs, und die
  Modellwahl ist durch eine zweite Methode durchzureichen. ADR-002, Entscheidung Punkt 3 sagt
  dasselbe.
- **Der Verbleib der Wohnflächenhülle** (Nr. 15, 51): `GebaeudeWohnflaecheHuelle.Oeffnen` hat **keinen
  Aufrufer** mehr; gerufen wird allein `.Gaben` aus `GebaeudeHuelle`. Sie zieht deshalb als reiner
  Gabenbauer mit, und es bleiben genau **zwei** Fensterdateien in der Schale — nicht drei.
- **Der Schemastand** (Nr. 6): `SchemaStand.Zielversion = 78`; 77 und 78 sind am 15.09.2026 anderweitig
  vergeben worden. Damit sind die Nummern in Befund V, im Konzept und im Datenaustauschkonzept an
  dieser Stelle überholt — dieses Papier vergibt keine Nummern mehr.
- **Die Dateinamen des Exports** (Nr. 17, 60): Die Speicherdateien tragen die **Anlagen-Id** mit dem
  ausgeschriebenen Grund „damit sie stabil bleiben"; der Schleifenindex kommt nur bei den
  Flotteneinheiten vor. Das Muster ist also Vorsatz, stabile Kennung, Größe.

## Anmerkungen zur Einarbeitung

- **Neu gesetzte Namen** (E11, von der Skizze nicht vergeben, vom Konzept diesem Papier überlassen):
  `Zonengeometrie` und `Zonenumriss` in `EPOS.Kern/Allgemein/Simulation/Gebaeude/` mit den zwei
  Fabrikwegen `AusRaumgrenzen(...)` und `AusFlaechen(...)`; `GebaeudeAnsicht.razor` +
  `GebaeudeAnsichtDaten.cs`; Ressourcenpräfix `GAN_`; dazu `GebaeudeExportDialog.razor` +
  `GebaeudeExportDaten.cs` und `GebaeudeExportHuelle.cs` für den Export und der Schrittname **S-G**.
  Sie sind mit dem Schwesterpapier über die Skizze abzustimmen, nicht über Änderungen an ihm.
- **Die Zahl der offenen Architekturfragen steigt auf neunzehn** (A19 = U2, Feldsichtbarkeit je
  Schalterstellung). Kapitel 0, 6 und 7 sind nachgezogen.
- **Drei Zählungen wurden fortgeschrieben:** vierzehn Bausteine der Kernablage (statt zwölf), acht
  Hüllen (neun mit G7), sechzehn prüfbare Abhängigkeitssätze in fünfzehn Nummern.
- **Keine Normzahl und kein Wert aus VDI 6020:2022** ist in das Papier gelangt; der Produktausweis
  wird über ADR-002 und einen Ressourcenschlüssel geführt, nicht abgeschrieben (Nr. 37).

---

## Nachtrag: zweite unabhängige Prüfung (16.09.2026)

Die zweite Prüfung hat drei Einträge dieses Protokolls als **gemeldet, aber nicht zu Ende geführt**
nachgewiesen. Alle drei sind jetzt ausgeführt.

| Eintrag | Was die Meldung sagte | Was das Papier zeigte | Erledigung |
|---|---|---|---|
| **Nr. 3** (Ordinalzahlen der Einfrierregeln) | „in W18, 2.4, 2.8, Bild 7 **und Kap. 5**" | W18, 2.4, 2.8 und Bild 7 waren umgestellt, das Bild in Kap. 5 **nicht**: `EINFRIEREN, 4. Regel`, `Merge M4 — Klimaspalten / 6. Regel`, `G6d … EINFRIEREN, 5. Regel` — also genau die beanstandete Reihenfolge, und zugleich ein Widerspruch zum eigenen Satz in 2.8 („die Reihenfolge trägt bewusst keine Ordnungszahlen") | die drei Beschriftungen tragen jetzt den **Gegenstand**: `Regel Gebaeudedaten`, `Regel Klimareihen`, `Regel Zonendaten` — gleichlautend mit Bild 7 und mit Systementwurf 8.3 |
| **Nr. 6** (vergebene Schemaschrittnummern) | „**alle** harten Nummern aus Text, Tabellen und Mermaid-Beschriftungen entfernt" | drei überlebten: das Bild in Kap. 5 (`Merge M3 — Schema 77`), das Flussbild 2.5 (`NAMENSZUGRIFF ab 77`) und Kap. 6 A9 („Schemaschritte 77 und 78") — Nummern, die nach 2.4 desselben Papiers eine andere Fachänderung tragen | alle drei auf den Papiernamen **M3** gestellt; A9 fragt nach den „zwei Gebäudespalten-Schritten des Konzepts" und verweist auf 2.4 |
| **Nr. 35** (Zuschnitt von A16, mit Nr. 7) | A16 in Kap. 6 auf das neu Offene verengt | 4.1 trug weiter die weite Fassung „Dass die Verzweigung dort und nicht enger sitzt, ist A16" — also genau das, was Kap. 6 für entschieden erklärt | 4.1 nennt den Zuschnitt am Punkt im Rumpf als entschieden (Umsetzungskonzept 1.5) und A16 als die Frage allein zu `:647`; die Frage in Kap. 6 trägt keine Ordnungszahl mehr, die ihrem eigenen Rumpf widerspräche |

Zwei weitere Punkte der zweiten Prüfung betrafen dieses Papier:

- **Kap. 0 Punkt 3** sagte „bei **genau einem** Sichtneubau" — ohne den Vorbehalt, den dasselbe
  Papier in W13 („Vorbehalt: A9 = U5") und in Kap. 6 A9 als offene Anwenderfrage führt und den der
  Systementwurf in 0.3 und 5.3 setzt. Der Vorbehalt steht jetzt auch dort; ohne ihn griffe die
  Kurzfassung einem Entscheid des Anwenders vor.
- **2.9** schloss die Indexzeile mit „drei Größenordnungen unter den Klimareihen einer einzigen
  Region" — derselbe Rechenfehler aus Befund V 0.7, den der Systementwurf mit seinem Befund 10
  bereits beseitigt hatte: rund 3 500 gegen 8 760 Zeilen ist Faktor 2,5, nicht 1 000. Der Satz nennt
  jetzt dieselbe Größe wie Systementwurf 5.2.

**Weiterhin außerhalb dieses Papiers:** die Indexzeile für **dieses Protokoll** in
[`Dokumentation/LIESMICH.md`](../../LIESMICH.md). Sie fehlt als einzige der
Gebäudesimulations-Familie; ohne sie ist der Fall `Der_Index_nennt_jedes_Papier` der
`DokumentationLinkWacheTests` rot und `kern.yml` mit ihm. Den Index pflegt der Orchestrator (Nr. 2).

# Konzept: Zapfprofil — Nutzungsbereiche, zwei Stufen, Methoden getrennt, Profil ablegen

**Stand: Entwurf (17.10.2026), Entscheidungen beim Anwender.** Abschnitt 1 hält den Befund zur Meldung
des Anwenders fest, Abschnitt 2 das Zielbild, Abschnitt 3 die Vorschläge ZC1 bis ZC6, Abschnitt 4 die
Entscheidungsfragen ZC‑Q1 bis ZC‑Q6, Abschnitt 5 die Wellen. Es setzt das
[Konzept Zapfprofil — Benutzerführung](Konzept_Zapfprofil_Benutzerfuehrung_EPOS-Plan.md) (ZB1–ZB12,
umgesetzt) fort; dessen Zielbild L1–L7 gilt weiter.

**Umsetzungsstand:** W8 (ZC2, ZC3), W11 (ZC6) und ZC1 umgesetzt mit #1201; offen W9 (ZC5, ZC4 a/c), W10 (ZC4 b), W12.

**Anlass:** Auftrag des Anwenders vom 17.10.2026 nach Sichtprobe unter Windows (drei Bildschirmfotos:
Zonenblock der Stufe Einfach, Katalogsatz eines Brauchwasserprofils, Dialog Brauchwasserwärme):

> Der Zapfprofil-Generator ist fehlerhaft und das Design nicht gut. Beides soll verbessert werden:
> Statt Nutzungszonen → Nutzungsbereiche. Mache aus den beiden Bereichen Nutzungszonen und der Zone
> einen Bereich: Eingabe der Nutzung direkt per Dropdown, Personen direkt eingebbar, Bedarfsniveau per
> Dropdown. Die Funktionen in Erweitert scheinen nicht zu gehen (Kalender hat keine Wirkung, Eingaben
> Einfluss Anlagentopologie — gibt es eine Funktionalität? Keine Änderung sichtbar). Im Dialog
> „Zapfprofil erzeugen“ sollte das erstellte Profil zum Katalog oder Projekt hinzugefügt werden können
> (Warnung bei Projekt, da nicht in DB enthalten!). Nur Auswahl Einfach, Experte. Die verschiedenen
> Methoden sollten klar getrennt sein (bei Experte) — jede Methode in einem eigenen Bereich mit klarer
> Struktur und Benutzerführung.

**Geltungsbereich:** `EPOS.UI/Dialoge/Bedarf/ZapfprofilDialog.razor` samt `ZapfprofilDaten.cs`,
`ZapfprofilTexte.cs`, die Hülle `EPOS.UI.Daten/Bedarf/ZapfprofilHuelle*.cs`, der Einstieg aus
`BedarfsProfileDialog.razor`, für ZC6 die Controller der Brauchwasserprofile. Nicht berührt: der
Rechenweg vollständiger Zonen, die Referenzbasis, das Schema (kein Schemaschritt nötig).


## 1 Befund (Codelesung 17.10.2026)

| Nr. | Meldung | Befund |
|---|---|---|
| F1 | „Kalender hat keine Wirkung“ | **Kein Fehler im Datenfluss.** Quelle, eigene Ferien und Gebäudebindung kommen über `MitAngaben` → `MitGebaeude` → `Zapfkalender.Bilden` in die Rechnung, die Vorschau rechnet nach 400 ms neu. Aber: Die **Jahressumme hängt nie vom Kalender ab** (`Q_a = n·q·365·f_θ`, der Formvektor verteilt nur um), und die Vorschau zeigt den Tagesgang des größten Monats, die Woche um den größten Tag (Winter) und die Monatssummen — Ferien sind allein in den Monatsbalken sichtbar, und dort schwach. Bei **Wohnen** ist `Ferienfaktor` im Katalog leer; ein Ferientag bekommt dann das Sonntagsgewicht und zapft eher **mehr** als ein Werktag. Ob Wohnen in den Ferien gleich, weniger oder gar nicht zapft, ist eine Verhaltensfrage (ZC‑Q4). Bei Büro fällt der Bedarf an Ferientagen auf 0. |
| F2 | „Einfluss Anlagentopologie — keine Änderung sichtbar“ | **Richtig beobachtet, so gebaut.** Die Topologie wirkt allein in der Auslegung (Gruppe je Topologie: Speicher bekommt Nenninhalt und Ladeleistung über die Summenlinie, Frischwasserstation und Durchfluss nur die Minutenspitze, Wohnungsstation einen Hinweis). Im Hauptdialog ändert sich nur die Spalte „Topologie“ und die leise Zeile „Punkt überholt“; die Vorschau bleibt gleich, und das Auslegungsergebnis sieht man erst nach erneutem „Auslegung…“. Es fehlt die Rückmeldung am Ort der Eingabe. |
| F3 | Eingaben der Stufe Erweitert | Alle Eingaben gehen in Rechnung, Auslegung oder Speicherung; keine ist tot. Die Wirkung ist nur unterschiedlich und nirgends benannt: **Summe** (Belegung, Tagesbedarf, Messwert-Kalibrierung, Fachwerte), **Verteilung** (Kalender, Tagesgangsatz, Auslastung), **Simulation ohne Summenänderung** (Zirkulation, Stochastik, Typtage), **nur Auslegung** (Topologie, Ladeleistung, Ladefenster, Leitungsinhalt, Speichertemperatur, Kaltwasser der Auslegung), **nur Herkunft** (Quelle und Zeitraum des Messwerts). |
| F4 | „Zapfprofil erzeugen“ ohne Ablage | Der Knopf öffnet den Generator als Blatt; das OK stellt den Rechenweg auf Generator und schreibt nach `Tab_TwwProjekt`, `Tab_TwwZone`, `Tab_TwwZonenferien`. **Es gibt keinen Weg** zu einem Profilsatz (`Tab_Brauchwasser`, `Z_Projekt_Brauchwasser`, Katalog `Tab_Brauchwasser_STAMM`). Bausteine sind da: `BrauchwasserStammCtrl.SaveHead` (Kopf mit zwölf Monatswerten), `TypProfilCtrl.Neu/Speichern` (Wochenprofil 168 h), `BedarfStammCtrl.CopyFromStamm` plus Zuordnungszeile, und eine Zuordnungszeile kann eigene Monatsanteile und ein eigenes Wochenprofil tragen (Eingabeart „monat“/„woche“). |
| F5 | Stufen | Die Stufe ist reiner Ansichtszustand (`_stufe`, Start immer Einfach), nicht gespeichert. Einfach zeigt Zonentabelle, Karte Nutzung, Vorschau, Auslegung; Erweitert dazu Spalten Topologie/Anteil/Rechenweg, Karten Belegung, Kalender, Messwert, Tagesbedarf und die Gruppe „Gebäude und Anlage“; Experte dazu Fachwerte, Editoren, Seed/Realisierungen, Typtage. Wegfall von Erweitert: Datenbank nichts, Razor 45 Stellen, Hülle 4, drei Ressourcen, KI-Sicht 2, Tests 66 Treffer in 9 Dateien, Wiki 12 Stellen. |
| F6 | Zonenblock | Tabelle (nur lesend, Zeilenwahl) und darunter die Karte „Nutzung“ der gewählten Zone mit Name, Nutzungsart, Bezugsgröße, Herleitung aus der Wohnungstabelle, Niveau, nächstem Schritt und Meldungen. Die Setzer je Zone (`ZonenNutzungsartSetzen(z, …)` usw.) gibt es schon für den Assistenten — das Verschmelzen ist technisch einfach. Zu lösen: Fokus nach „hinzufügen“, Fehlzuordnung je Feld, `@key` je Zeile, 44‑px‑Ziele und Spaltenbreiten (Hausregel `table-layout: fixed`). |
| F7 | Begriff | „Nutzungszonen“ steht sichtbar nur in `ZPG_GRP_ZONEN` (de/en), zwei UI-Texten, acht Wiki-Stellen und zwei UI-Tests. Das Wort „Zone“ allein steht in rund 212 Ressourcenwerten je Sprache (Knöpfe, Satzzeilen, Meldungen). Schema (`Tab_TwwZone`), Bezeichner und Referenzlauf bleiben in jedem Fall. |


## 2 Zielbild

- **Z1 Ein Block „Nutzungsbereiche“.** Jede Zeile ist der Bereich: Nutzungsart (Klappliste), Bezugsgröße
  mit Einheit (Zahl), Bedarfsniveau (Klappliste), MWh/a. Keine zweite Karte für dieselben drei Werte.
- **Z2 Zwei Stufen.** *Einfach* ist der Block aus Z1 mit Vorschau und Auslegung. *Experte* öffnet unter
  dem Block die Methoden.
- **Z3 Jede Methode ein Bereich** mit Kopfzeile (Name, was sie ändert: Summe, Verteilung, Jahresreihe,
  Auslegung), Zusammenfassung zugeklappt, eigenem „Wirkt auf …“ und eigener Prüfung.
- **Z4 Wirkung sichtbar.** Was eine Eingabe ändert, zeigt der Dialog am Ort der Eingabe: Tagtypen und
  Ruhetage beim Kalender, Auslegungsergebnis je Topologie, Summenänderung bei Belegung und Messwert.
- **Z5 Das Ergebnis ist ein Profil wie jedes andere.** Aus dem Generator entsteht auf Wunsch ein
  Projektsatz oder ein Katalogsatz der Brauchwasserprofile.


## 3 Vorschläge

| Nr. | Vorschlag | Aufwand |
|---|---|---|
| **ZC1** | **Begriff „Nutzungsbereich“.** Überschrift, Satzzeile, Spaltenkopf, Knöpfe („Bereich hinzufügen“, „Duplizieren“, „Entfernen“), Meldungen und Hinweise des Dialogs, KI-Sicht, Wiki-Quelle und Logbuch; Bezeichner, Schema, Referenzlauf und das Wort „Zone“ im Gebäudekontext bleiben. Umfang nach ZC‑Q1. | klein bis mittel (Ressourcen in beiden Sprachen, Tests mit deutschen Texten) |
| **ZC2** | **Ein Block statt Tabelle plus Karte.** Zeile = Bereich mit Name (Text), Nutzungsart (Klappliste mit Bezugsart im Eintrag, gesperrte Einträge wie heute), Bezugsgröße (Zahlenfeld, Einheit aus der Nutzungsart, bei Wohnungstabelle grau mit der wirksamen Menge), Bedarfsniveau (Klappliste niedrig/mittel/hoch), MWh/a, Zustand („unvollständig · Nutzungsart wählen“). Unter der markierten Zeile eine schmale Detailzeile für Meldungen, Herleitung und „Katalog…“. Fokus nach „hinzufügen“ in die Klappliste der neuen Zeile. In Experte wandern die Spalten Topologie, Anteil, Rechenweg in die Methoden (ZC5); die Tabelle bleibt fünfspaltig und passt ins Blatt. Baustein: `Zeilenraster` (Kostenpositionen) oder die eigene Tabelle mit `table-layout: fixed`; Maß mit der Rasterprobe. | mittel |
| **ZC3** | **Zwei Stufen: Einfach und Experte.** Der Enum behält den Wert, die Wahl zeigt zwei Einträge; alles, was heute „ab Erweitert“ erscheint, erscheint ab Experte. Einfach bekommt zusätzlich die Zeile „Zirkulation ja/nein“ in der Gruppe „Gebäude und Anlage“ (ZC‑Q2), weil sie das Ergebnis der Stufe Einfach spürbar ändert. Hinweise „weitere Angaben in der Stufe Erweitert“ sagen Experte. KI-Sicht, Tests und Wiki folgen. | mittel (vor allem Tests) |
| **ZC4** | **Wirkung am Ort der Eingabe.** (a) **Kalender:** zugeklappt „251 Werktage · 62 Sonn- und Feiertage · 52 Ferientage · Ruhetage zapfen wie Sonntag“; aufgeklappt unter dem Jahresraster die Zeile „Der Kalender verteilt den Jahresbedarf um, er ändert die Summe nicht“ und die Verteilung Werktag/Ruhetag in kWh. Die Vorschau bekommt im Monatsbild die Ferientage als schmale Marke. (b) **Ferien bei Wohnen** nach ZC‑Q4: heute Sonntagsgewicht; Vorschlag „Abwesenheit“ als wählbarer Ferienfaktor der Zone mit Vorgabe aus dem Katalog (0 = kein Zapfen, 1 = wie Sonntag, leer = Katalog). (c) **Topologie:** wandert in die Methode „Auslegung“ (ZC5); dort steht unmittelbar das Ergebnis je Gruppe (Speicher: Nenninhalt und Ladeleistung; Frischwasser/Durchfluss: Minutenspitze; Wohnungsstation: Hinweis) und rechnet beim Wechsel neu; der Knopf „Auslegung…“ öffnet weiter das Blatt mit Summenlinie und Vergleich. Im Block der Bereiche entfällt die Spalte Topologie. | mittel |
| **ZC5** | **Methoden als Bereiche (Experte).** Unter dem Block der Bereiche, je Methode eine `Klappgruppe` mit Kopfzeile und Wirkungsmarke: **Bedarf** (Wirkung: Summe) — Wohnungstabelle, Personen/Fläche je WE, Tagesbedarf auto/manuell, Bedarf spezifisch, Zapftemperatur; **Kalender** (Verteilung) — Quelle, Ferien, Jahresraster, Tagtypzeile (ZC4 a), Ferienfaktor (ZC4 b); **Messwert** (Kalibrierung der Summe) — Wert, Einheit, Bilanzgrenze, Speicherverlust, Quelle, Zeitraum, Ergebnis „Faktor 0,93 gegenüber Norm“; **Jahresreihe** (Simulation) — Rechenweg Norm-Jahresgang / stochastisch / Typtage VDI 4655 mit den je eigenen Feldern (Seed, Realisierungen; Klimazone, Gebäudeart), Tagesgangsatz, Auslastung je Monat, Kaltwassergang; **Zirkulation und Anlage** (Simulation) — Zirkulation auto/Methode/Lage/Länge/Verlust/Laufzeit, Leitungsinhalt; **Auslegung** (nur Auslegung) — Topologie mit Ergebnis je Gruppe (ZC4 c), Ladeleistung und Ladefenster, Speichertemperatur, Kaltwasser der Auslegung, Knopf „Auslegung…“. Jede Gruppe sagt in einem Satz, was sie tut und worauf sie wirkt; die Zusammenfassung nennt die wirksamen Werte; Felder je Bereich (Zone) stehen in der Gruppe unter der Marke des markierten Bereichs, Felder je Projekt unter „Gebäude und Anlage“. Die heutigen Karten Belegung, Kalender, Messwert, Tagesbedarf, Fachwerte und die Gruppe „Gebäude und Anlage“ gehen in diesen sechs Gruppen auf. | mittel bis groß (Umbau der Razor-Gliederung, Tests der Karten) |
| **ZC6** | **Profil ablegen aus „Zapfprofil erzeugen“.** In der Schlussleiste des Generator-Blatts zwei weitere Knöpfe: **„Als Katalogsatz speichern…“** (Name, Typ, Beschreibung abfragen; Kopf mit zwölf Monatssummen über `BrauchwasserStammCtrl.SaveHead`, Wochenprofil 168 h über `TypProfilCtrl`, als Anwendersatz, nicht ReadOnly) und **„Zum Projekt hinzufügen“** (Zuordnungszeile mit eigenen Monatsanteilen und eigenem Wochenprofil, Eingabeart „woche“, Jahressumme aus dem Generator; **Warnung vorher:** „Das Profil steht dann nur in diesem Projekt, nicht im Katalog. Es rechnet als Bestandsprofil ohne Ferien, Feiertage, Stochastik und Typtage.“). Die Abbildung im Kern (`ZapfprofilErgebnis` → zwölf Monatssummen und mittlere Woche, mit oder ohne Zirkulation nach ZC‑Q6) ist eine neue Kernmethode mit Tests; der Rechenweg des Projekts bleibt, wie der Anwender ihn gewählt hat, ein Hinweis nennt den Weg, der gerade gilt. | mittel |

**Was bleibt, wie es ist:** Rechenweg, Zapfkalender, Auslegungsverfahren, Schreibweg nach `Tab_Tww*`,
Schema (kein Schemaschritt), Referenzbasis R58 (ZC4 b ändert Ergebnisse nur, wenn ein Referenzprojekt
einen Ferienfaktor setzt — keines tut es), OK-Prüfung, Blattwechsel aus dem Bedarfsprofil-Dialog.


## 4 Entscheidungsfragen

| Nr. | Frage | Möglichkeiten | Empfehlung |
|---|---|---|---|
| ZC‑Q1 | Reichweite der Umbenennung (ZC1) | **A** nur Überschrift, Satzzeile, Spaltenkopf und Knöpfe des Dialogs · **B** alle sichtbaren Texte des Zapfprofils („Bereich“ statt „Zone“ in Meldungen, Hinweisen, KI-Sicht, Auslegungsblatt, Wiki) | **B** — ein Begriff im ganzen Dialog; „Zone“ bleibt dem Gebäudemodell |
| ZC‑Q2 | Was die Stufe Einfach zusätzlich zeigt (ZC3) | **A** nur den Block, Vorschau und Auslegung wie heute · **B** dazu „Zirkulation ja/nein“ und der Knopf „Auslegung…“ mit der Topologie-Klappliste daneben | **A** — Einfach bleibt die Drei-Werte-Eingabe; Zirkulation und Topologie sind Expertenentscheidungen |
| ZC‑Q3 | Name des Bereichs (ZC2) | **A** Textfeld in der Zeile · **B** automatisch „Bereich 1“, „Bereich 2“, änderbar in der Detailzeile | **B** — die Zeile bleibt schmal, der Name ist selten nötig |
| ZC‑Q4 | Ferien bei Wohnen (ZC4 b) | **A** wie heute (Ferientag = Sonntag) · **B** Ferientag = Abwesenheit (0) als Vorgabe für Wohnen · **C** wählbarer Ferienfaktor je Bereich, Vorgabe aus dem Katalog (heute Sonntag) | **C** — Wohnheim und Hotel ruhen in den Ferien, ein Mehrfamilienhaus nicht; ohne Eingabe ändert sich nichts an der Basis |
| ZC‑Q5 | Topologie im Hauptdialog (ZC4 c) | **A** in der Methode „Auslegung“ mit Ergebnis je Gruppe · **B** ganz aus dem Hauptdialog, nur noch im Blatt „Auslegung…“ | **A** — das Ergebnis am Ort der Wahl beantwortet die Frage „gibt es eine Funktionalität?“ |
| ZC‑Q6 | Zirkulation im abgelegten Profil (ZC6) | **A** nur Zapfwärme · **B** Zapfwärme plus Zirkulationsverlust als Jahressumme · **C** wählbar im Dialog | **B** — der Profilsatz soll den Bedarf tragen, den der Generator in der Simulation liefert |


## 5 Wellen

| Welle | Inhalt | Abnahme |
|---|---|---|
| W8 | ZC1, ZC2, ZC3: Begriff, ein Block der Bereiche, zwei Stufen | bunit je Zustand (Fokus, unvollständig, Wahl, Sperre), Rasterprobe des Blocks, `StilblattTests`, Kern-Filter grün, Referenzlauf PASS ohne neue Basis |
| W9 | ZC5 mit ZC4 (a) und (c): Methoden als Bereiche, Tagtypzeile, Topologie mit Ergebnis | bunit je Gruppe (Zusammenfassung, Wirkungsmarke), Auslegungstests, Referenzlauf PASS |
| W10 | ZC4 (b) Ferienfaktor je Bereich (nur bei ZC‑Q4 B oder C) | `ZapfkalenderTests`, `FormvektorTests`, Wache 1067 (Kalender) unverändert, Referenzlauf PASS; bei B neue Basis mit Begründung |
| W11 | ZC6 Profil ablegen (Kernabbildung, zwei Knöpfe, Warnung) | Kerntests der Abbildung (Summe erhalten, 168 Werte, zwölf Monate), bunit der Knöpfe, SQL-Dialekt-Prüfer |
| W12 | Wiki-Quelle und Logbuch (Version nach Anwender), Konzept Benutzerführung und dieses Konzept nach `ueberholt/` | `WikiProduktdatenWacheTests`, Gegenlesen mit dem Muster aus `CLAUDE.md` |

W8 und W11 sind unabhängig und laufen parallel; W9 setzt auf W8 auf, W10 auf W9. Keine Welle braucht
einen Schemaschritt oder eine Änderung der Testdatenbank.

# Speichergröße der Füllstandslinie am Wochenbild (27.09.2026)

Protokoll des Auftrags **D9** der Sitzung „Zapfprofilgenerator Cloud". Anwenderauftrag vom
27.09.2026: „Setze Teil D um"; der Punkt stammt aus der Übergabe
`Dokumentation/aktuell/Zapfprofilgenerator/2026-09-23_Uebergabe_Zapfprofilgenerator.md`,
Abschnitt 2.4: „Die Speichergrößen-Auswahl im Wochendiagramm der Auslegung ist nur ein Bild, nicht
bedienbar." Umsetzungskonzept Zapfprofilgenerator 4.7, N10 (k), N11 (d), N13 (o); Mockup
`Zapfprofilgenerator_Mockup.html`, „Maßgebende Woche der Stundenbilanz".

**Rahmen.** Worktree `agent-a8a8d299762aafdbf`, Zweig `worktree-agent-a8a8d299762aafdbf` von
`903f926` (= `origin/ios_migration_september`), Opus 5.5, Cloud-Umgebung (Linux). Kein
Schemaschritt, kein Rechenwegwechsel, Testdatenbank unberührt, Mockup und Wiki unberührt, kein Push.

---

## 1 Befund

| Stelle | Stand vor dem Auftrag |
|---|---|
| Mockup | Über dem Wochenbild ein Feld „Speichergröße der Füllstandslinie" („Listengröße · 500 l"), darunter „zur Wahl: …" mit den Volumina der Verfahren — nur ein Bild |
| Dialog | Die Wahl des Füllstandsbezugs (Stufe Z4, N13 (o)) stand als Auswahlfeld „Bezug des Füllstands" in der Gruppe „Eingaben des Verfahrensvergleichs", weit vom Wochenbild; Einträge ohne Liter; ein Bezug ohne Volumen war wählbar, der Kern fiel dann auf die Vorgabe zurück und nannte es erst nach dem Rechnen in der Warnliste |
| Speicherung | `Tab_TwwProjekt.Fuellstand_Bezug` (CHECK 1 … 4, `NULL` = Vorgabe, Schritt 124) — unverändert |
| Kern | `Speicherauslegungsergebnis` trug `FuellstandBezugL`/`FuellstandBezug`, `NenninhaltL` (Band) und `BandMaxL`; nicht: den Punkt der Summenlinie (nur Eingang), den Nenninhalt des Punkts (lokal gerechnet) und welchen Bezug die Vorgabe auflöst |

Die Wahl des Mockups (Volumina der Verfahren) ist nicht die umgesetzte Wertemenge; es gelten die
vier Bezüge aus N11 (d). Das Mockup bleibt, wie es ist.

## 2 Umsetzung

| Schicht | Datei | Änderung |
|---|---|---|
| Kern | `EPOS.Kern/Allgemein/Zapfprofil/TwwSpeicherauslegung.cs` | Nur Ergebnisfelder: `SummenlinienpunktL`, `NenninhaltPunktL`, `FuellstandVorgabe` (Aufruf der reinen Funktion `Fuellstandbezug(null, …, hinweise: null)`), dazu `BezugsvolumenL(art)` und `Fuellstandsperre(art)` mit den Sätzen `FUELLSTAND_GESPERRT_OHNE_PUNKT`, `…_PUNKT_OHNE_NENNINHALT`, `…_OHNE_BAND`, `…_BAND_OHNE_NENNINHALT` |
| DTO | `EPOS.UI/Dialoge/Bedarf/ZapfprofilDaten.cs` | `ZapfprofilFuellstandwahlDaten` (Art, Volumen, Sperrgrund); `ZapfprofilVergleichDaten.FuellstandWahl`, `FuellstandVorgabeArt` |
| Hülle | `EPOS.UI.Daten/Bedarf/ZapfprofilHuelle.Auslegung.cs` | Je Bezug der Wertemenge des Schemas Volumen und Satz des Kerns; fünf neue Texte ins Bündel |
| Texte | `EPOS.UI/Dialoge/Bedarf/ZapfprofilAuslegungTexte.cs`, `EPOS.Kern/MyResource/Resource(.en-US).resx`, `Resource.Designer.cs` | `ZPG_AUS_LBL_FUELLSTAND_BEZUG` umgewidmet zu „Speichergröße der Füllstandslinie"; neu `ZPG_AUS_FUELLSTAND_VORGABE_WAHL`, `…_WAHL`, `…_WAHL_GESPERRT`, `ZPG_AUS_HERL_FUELLSTAND_GESPERRT`, `…_GRUPPE`; `ZPG_AUS_HERL_FUELLSTAND` und `KI_DLG_ZPGA_FUELLSTAND_BEZUG_ERL` nachgeführt; vier `ZPG_SATZ_FUELLSTAND_GESPERRT_*` — alles in beiden Sprachen; Designer neu erzeugt (Blöcke gleich, zweiter Lauf +0) |
| Dialog | `EPOS.UI/Dialoge/Bedarf/ZapfprofilAuslegungDialog.razor` | Das Feld verlässt die Eingabegruppe und steht im Verfahrensvergleich der ersten Speichergruppe unmittelbar über dem Wochenbild (`.epos-zapfausl-fuellstandwahl`, im `Formularraster`); Einträge „Nenninhalt des Punkts · 400 l" in der Kultur, „Vorgabe: … · … l"; ein Bezug ohne Volumen „… · nicht bestimmbar", gesperrt mit Grund; darunter die Zeile „angesetzt" und je gesperrtem Bezug eine Zeile mit Grund; weitere Wochenbilder mit Herleitungszeile; der Assistent sieht dieselben Einträge und bekommt beim gesperrten denselben Grund |

Ein Wechsel geht über den vorhandenen Weg `FuellstandSetzen` → `Geaendert` (entprellte Neuberechnung):
Wochenbild, Kachel „Füllstand" und Herleitung zeichnen mit dem neuen Ergebnis. Stilblatt unberührt
(die Klasse trägt keine eigene Regel; das Feld nimmt das Hausmuster `Formularraster`).

## 3 Entscheide

- **Platz bei mehreren Gruppen:** über dem Wochenbild der ersten Gruppe mit Verfahrensvergleich
  (dieselbe Gruppe, deren angesetzte Werte schon an den Eingaben stehen); jedes weitere Bild trägt
  „Speichergröße der Füllstandslinie: … — gewählt am ersten Wochenbild". Die Liter der Klappliste
  sind die des ersten Bilds. Der Kern bildet je Topologie eine Gruppe, und nur die Speichergruppe
  trägt eine Speicherauslegung — mehrere Wochenbilder kommen heute nicht vor, die Regel sichert ab.
- **Keine Zeile „zur Wahl":** Die Klappliste trägt die Liter an jedem Eintrag; eine zweite Zeile
  wiederholte dieselben vier Zahlen. Unter dem Feld stehen die angesetzte Größe (sie zeigt auch
  einen Rückfall des Kerns auf die Vorgabe) und, nur wenn es sie gibt, die gesperrten Einträge mit
  Grund.
- **Gesperrte Einträge:** `disabled` mit `title` am Eintrag (Muster `Auswahlfeld.GesperrteEintraege`
  wie bei den Quellen des Bedarfstags); weil ein `title` an einer `<option>` kein Touchgerät
  erreicht, steht der Grund zusätzlich sichtbar unter dem Feld. Ein gespeicherter Bezug, der im
  Ergebnis keinen Wert hat, bleibt als gewählt sichtbar; der Kern rechnet mit der Vorgabe und nennt
  es in der Warnliste (vorhandener Hinweis `FUELLSTAND_BEZUG_VORGABE`).
- **Gründe als Sätze des Kerns:** Warum ein Bezug kein Volumen hat, entscheidet der Kern an seinen
  Ergebnisfeldern (`Fuellstandsperre`); die Hülle übersetzt nur. Die Wache `ZapfSaetzeWacheTests`
  hält die vier Sätze in beiden Sprachen.
- **Stufenregel unverändert:** Das Feld hatte keine Stufenbindung und hat keine; es steht, sobald
  ein Verfahrensvergleich gerechnet ist. Ohne Vergleich steht kein Feld — der gespeicherte Wert
  bleibt, der Assistent kann ihn weiter setzen.
- **Beschriftung umgewidmet** statt neuer Schlüssel: `ZPG_AUS_LBL_FUELLSTAND_BEZUG` benennt auch das
  Feld des Assistenten (`KiDialogTexte.ZpgaFuellstandBezugName`) — beide heißen gleich.

## 4 Tests

| Projekt | Datei | Fälle |
|---|---|---|
| `EPOS.UI.Tests` | `Dialoge/ZapfprofilAuslegungDialogTests.Fuellstand.cs` (neu) | Feld über dem Wochenbild und nicht in der Eingabegruppe, Einträge mit Litern in de-DE (`1.540 l`), Vorgabe mit aufgelöstem Bezug; gesperrter Eintrag mit `disabled`, `title` und sichtbarem Grund, Klick ohne Wirkung und ohne Neuberechnung, Assistent abgelehnt mit Grund; Wechsel zeichnet Bild, Kachel und Herleitung neu; zwei Speichergruppen — ein Feld am ersten Bild, Herleitungszeile am zweiten; ohne Vergleich kein Feld |
| `EPOS.UI.Tests` | `Dialoge/ZapfprofilAuslegungDialogTests.Vergleich.cs` | fachlich umgeschrieben: neue Beschriftung, Einträge mit Litern, Herleitungstext; die Wahl geht weiter mit OK zurück, der Assistent setzt sie weiter |
| `EPOS.Kern.Tests` | `SpeicherauslegungTests.cs` | +1: Bezugsvolumina, Vorgabe unabhängig von der Wahl, Sperrgründe (ohne Punkt, Punkt ohne Liste, ohne Band); eine Wahl ändert Defizit, Verfahren, Band, Nenninhalt und Hinweise nicht |
| `EPOS.Kern.Tests` | `ZapfprofilHuelleZ4Tests.cs` | +1 ohne Datenbank: die Hülle reicht Volumina und Sätze des Kerns in beiden Sprachen durch |
| `EPOS.Kern.Tests` | `ZapfprofilAuslegungHuelleTests.cs` | Fall auf der Testdatenbank erweitert: vier Einträge, angesetztes Volumen = Eintrag des Bezugs |

`KiMaskenabdeckungWacheTests` (Eingabezahl 20 des Auslegungsdialogs), `KiDialogkatalogTests` und
`KiFeldwerteTests` bleiben unverändert grün — das Feld ist dasselbe, nur an anderem Ort.

## 5 Gate

Commits `effca6e` (Umsetzung) und `ed65d68` (Kommentarform des Textbündels), dazu dieses Protokoll.

| Prüfung | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler; keine neue Warnung in den geänderten Dateien |
| gefilterte Tests nach der Umsetzung | UI 307 / 307 (Auslegungsdialog, KI-Maskenabdeckung, Dialogkatalog, Feldwerte), Kern 262 / 262 (Speicherauslegung, Zapfprofil-Hüllen, Auslegung, Satzwache, Referenzfall, Schritt 124) |
| erster voller Lauf (auf `effca6e`) | ein Rot aus dem Auftrag: `ZapfprofilAuslegungDatenTests.Jede_Beschriftung_steht_mit_ihrem_Schluessel_in_beiden_Sprachen` — die neuen Beschriftungen trugen einen Zusatz im `summary`-Kommentar, die Wache verlangt genau `<c>SCHLÜSSEL</c>`; behoben in `ed65d68` |
| voller Lauf `WP-Plan.Kern.slnf` (auf `ed65d68`) | EPOS.UI.Tests 6846 / 6846; EPOS.Kern.Tests 8833 grün, 1 übersprungen, **1 rot, umgebungsbedingt** (siehe unten); KiKern 549 / 549; SpeicherEngine 386 / 386; SpeicherPlanung 27 grün, 1 übersprungen |
| Designer | `designer_neu.py`: 13 046 Blöcke gleich, keiner neu oder abweichend (Zeilenenden des Arbeitsbaums auf LF zurückgesetzt), zweiter Lauf +0 |
| Windows-Schale | nicht gebaut — keine Hülle der Schale berührt; `WindowsFormsApplication1` nennt keinen der geänderten Typen |
| `SqlDialektPruefer` | nicht gezogen — kein SQL-Text geändert |

**Das umgebungsbedingte Rot.** `TwwKatalogWacheTests.Das_Einspielskript_ist_wiederholbar` scheitert
in der Cloud-Umgebung am Prüfschritt des Einspielskripts `Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py`:
`Tab_TwwNutzungsart_STAMM.csv` des freien Paketteils weiche vom Erzeugnis ab. Ursache ist die
Python-Fassung, nicht dieser Auftrag (Skript und Paketteil sind auf diesem Zweig unverändert): `python3`
ist hier 3.11; ab 3.12 summiert `sum()` Gleitkommazahlen kompensiert, und mit 3.12 erzeugt ist die
eingecheckte Datei (Wochenfaktoren wie `0.9209999999999999` gegen `0.9210000000000002` unter 3.11).
Gegenprobe auf einer Kopie der Testdatenbank: `python3.12` meldet „0 Zeile(n) angelegt, 0
nachgefuehrt", `python3.11` bricht mit derselben Meldung ab. Der Kern-Läufer (`ubuntu-latest`, System-Python 3.12)
ist nicht betroffen; in dieser Umgebung bleibt die Wache rot, bis das Skript fassungsfest summiert
oder `python3` auf 3.12 zeigt (Folge unten).

## 6 Referenzlauf

Nicht gezogen und nicht nötig: Im Kern ist allein `TwwSpeicherauslegung.cs` geändert, und nur um
Ergebnisfelder und zwei lesende Methoden. Die Speicherauslegung ist nachrichtlich (4.7) und geht in
keinen Simulationslauf ein; innerhalb der Auslegung bleiben Defizit, Band, Nenninhalt, Füllstand
und Warnliste gleich — der Kerntest hält das für eine gesetzte Wahl gegen die Vorgabe.

## 7 Folgen

- **Windows-Sichtabnahme:** Lage und Breite des Felds im Verfahrensvergleich, `title` an einer
  gesperrten Option in WebView2, Verhalten auf Touch.
- **Wiki** (paralleler Auftrag, hier nicht bearbeitet): Aufzählungspunkt „Bezug des Füllstands"
  aus der Gruppe „Eingaben des Verfahrensvergleichs" nehmen und die Wahl beim Wochenbild im Absatz
  „Verfahrensvergleich" beschreiben (Vorschlag im Bericht an die Orchestrierung).
- **Logbuch:** Satz im Bericht entworfen; Version beim Anwender erfragen.
- **Umsetzungskonzept:** N13 (o) nennt den „Füllstand-Bezug" bei den Eingaben — ein Nachtrag
  (Ort der Wahl am Wochenbild) bleibt der Orchestrierung.
- **Dokumentations-Index:** Die Zeile des Ordners `ueberholt/Protokolle/Zapfprofilgenerator/` in
  `Dokumentation/LIESMICH.md` zählt 25 Protokolle; dieses ist das sechsundzwanzigste (Zahl und
  Kurzbeschreibung nachtragen — die Wache verlangt nur die Ordnerzeile).
- **Einspielskript fassungsfest:** `tww_testkatalog_fiktiv.py` erzeugt die abgeleiteten Träger je
  nach Python-Fassung bitweise verschieden (`sum()` ab 3.12 kompensiert); `math.fsum` oder eine
  Rundung beim Schreiben machte Wache und Paketteil von der Fassung unabhängig.

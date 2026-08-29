# H8 — Aktion `projekt_aktiv` und lokale Klarnamen-Rückeinsetzung

**Stand:** 29.08.2026
**Grundlage:** Auftrag H8 („Der Assistent soll Fragen wie ‚Wie heißt das aktuelle Projekt?'
beantworten können — OHNE die Datenschutz-Zusage anzutasten").
**Vorgänger:** [`H4H5_Umsetzung_Protokoll.md`](H4H5_Umsetzung_Protokoll.md)
**Ausgangsstand:** sauberer Arbeitsbaum, HEAD `e5394cf`.

Umgesetzt sind alle fünf Auftragspunkte. `..\KiKern\` und `KiSchreibschutz.cs` sind
nachweislich unberührt (`git status` führt beide nicht), `KiEinwilligung.FASSUNG` bleibt **2**,
es wurde **kein** Git-Schreibkommando ausgeführt.

---

## 1. Kodierungsbehandlung je Datei

Vor jeder Änderung strikt als UTF-8 gelesen (`UTF8Encoding(false, true)` im `try/catch`) und auf
`U+FFFD` geprüft. **Keine** der berührten Dateien ist CP1252 — das CP1252-Rezept kam wie schon bei
H1/H2 und H4/H5 nirgends zum Einsatz. BOM-Zustand und Zeilenenden sind je Datei unverändert.

| Datei | vorher | nachher | Werkzeug |
|---|---|---|---|
| `Allgemein\KI\Aktionen\KiAktionenProjekt.cs` | UTF-8 **+BOM**, CRLF | unverändert | Edit |
| `Allgemein\KI\Aktionen\KiAktionsTexte.cs` | UTF-8 +BOM, CRLF | unverändert | Edit |
| `Allgemein\KI\Aktionen\KiAktionen.cs` | UTF-8 +BOM, CRLF | unverändert | Edit |
| `Allgemein\KI\KiChatService.cs` | UTF-8 **ohne** BOM, CRLF | unverändert | Edit |
| `Views\Help\Form_KiChat.cs` | UTF-8 ohne BOM, CRLF | unverändert | Edit |
| `MyResource\Resource.resx` | UTF-8 +BOM, CRLF | unverändert | PowerShell (Einfügung vor `</root>`) |
| `MyResource\Resource.en-US.resx` | UTF-8 +BOM, CRLF | unverändert | PowerShell (dito) |
| `MyResource\Resource.Designer.cs` | UTF-8 +BOM | UTF-8 +BOM | **von Visual Studio selbst regeneriert**, § 6.2 |

Schlussprobe: alle acht Dateien strikt UTF-8 lesbar, **kein `U+FFFD`**, **keine** reine
LF-Zeile. Sonderzeichenzählung (`äöüÄÖÜß„""–—•`) gegen `HEAD` — jede Datei hat **mehr**
Sonderzeichen als vorher (Delta 0…+33), keine einzige verlor eines. Beide `.resx` zusätzlich als
XML geladen: wohlgeformt, je **2647** `<data>`-Knoten (vorher 2643, also genau +4).

---

## 2. Aufgabe 1 — neue Leseaktion `projekt_aktiv`

`Allgemein\KI\Aktionen\KiAktionenProjekt.cs`, drei neue Bausteine:

| Fundstelle | Inhalt |
|---|---|
| `:90 ProjektAktiv()` | die `KiAktion` — Name `projekt_aktiv`, `Schutzstufe.Lesen`, **keine Parameter**, Andockpunkt `Program.startfrm / ApplikationCtrl.ReadSingle + ProjektCtrl.ReadSingle(int)` |
| `:108 AktivesProjektErgebnis()` | Ergebnisaufbau: eine Zeile mit `id, projektname, kunde, bearbeiter, geaendert` |
| `:158 AktivesProjektErmitteln(out int, out string)` | die Quelle des aktiven Projekts |

Registriert in `KiAktionen.Erzeuge()` (`Allgemein\KI\Aktionen\KiAktionen.cs:65`), unmittelbar
hinter `projekte_auflisten` und vor `projekt_suchen`. Das Register führt damit **24** statt 23
Aktionen; die 23 Bestandsaktionen sind unverändert.

### 2.1 Quelle des aktiven Projekts — im Code nachgesehen, nicht geraten

Der Auftrag verlangt „dieselbe Quelle, aus der `HilfeKontext.OhneKlarnamen()` den Namen kennt".
Nachgesehen: `HilfeKontext.cs:543` liest **`Program.startfrm.m_szProjektname`**. Genau dort
setzt die Ermittlung an; `Form_Start` führt daneben `m_ID_Projekt` (`Form_Start.cs:13–14`).

Reihenfolge in `AktivesProjektErmitteln`:

1. **`Program.startfrm`** — ID und Name der laufenden Oberfläche. Damit kann die Aktion nie ein
   anderes Projekt melden als das, dessen Name aus dem Kontext herausgeschnitten wird.
2. **Nur wenn es überhaupt kein Startfenster gibt** (Prüfharnisch, Konsole):
   **`ApplikationCtrl.ReadSingle()`** auf `Tab_Applikation` — das *zuletzt geöffnete* Projekt,
   dieselbe Quelle wie `Form_Start.pBox_ProjektZuletzt_Click` (`Form_Start.cs:821`).
3. Die fehlende Hälfte wird nachgeschlagen: ID über `ProjektCtrl.ReadSingle(string)`, Name über
   `KiHilfe.ProjektName(int)`. Begründung im Code: `Tab_Applikation` führte die ID historisch
   nicht immer mit (Befund 3, dokumentiert in `Form_Start.cs:825–828`) — der Name ist dort der
   verlässliche Teil.

**Wichtige Einschränkung des Ersatzwegs (bewusst enger als der Auftragswortlaut).** Der Auftrag
nennt beide Quellen nebeneinander. `Tab_Applikation` führt aber das *zuletzt geöffnete* Projekt,
nicht das *aktive*. Läuft die Oberfläche und hat sie kein Projekt geladen — der Zustand direkt
nach dem Programmstart —, wäre „zuletzt geöffnet" eine **falsche** Antwort auf „welches Projekt
ist offen?". Die Ersatzquelle greift deshalb ausschließlich bei `Program.startfrm == null`; mit
laufender Oberfläche und ohne geladenes Projekt lautet die Antwort „Zurzeit ist kein Projekt
geöffnet."

Jeder Datenbankzugriff ist eingefangen. Eine nicht erreichbare Datenbank führt zu
„kein Projekt geöffnet", nicht zu einem Fehler.

### 2.2 Ergebnis

* **Projekt offen:** Kopfdaten über `ProjektCtrl.ReadSingle(int)`, eine Zeile,
  Ergebnissatz `KI_REG_PROJEKT_AKTIV_GELESEN` („Aktuell geöffnet ist das Projekt {0} (ID {1}).").
* **Datensatz nicht lesbar** (Projekt zwischenzeitlich gelöscht): trotzdem `Ok` mit den Angaben
  der Oberfläche und der Meldung `KI_REG_PROJEKT_AKTIV_NICHT_GELESEN`.
* **Kein Projekt offen:** `KiErgebnis.Ok(KI_REG_PROJEKT_AKTIV_KEINES)` — **ohne Zeilen, kein
  Fehler**. Beim Programmstart ist das der Regelfall.

Der Projektname steht **absichtlich auch im Ergebnissatz**: `KiRueckmeldung.Erzeuge` baut zuerst
die Zeilen (dabei entsteht der Platzhalter) und säubert den Satz danach (`KiRueckmeldung.cs:257`,
`:276`). Beide Stellen führen denselben Zeichenkettenwert, sonst griffe die Ersetzung nicht —
im Prüflauf eigens nachgewiesen (§ 7, Block 3).

### 2.3 Zweck-Text

Der Zweck ist so formuliert, dass das Modell die Aktion greift (Ressource, beide Sprachen):

> Nennt das gerade geöffnete Projekt - also das, an dem der Benutzer aktuell arbeitet. Nimm diese
> Aktion bei Fragen wie "Wie heißt das aktuelle Projekt?", "Welches Projekt ist offen?" oder
> "Woran arbeite ich gerade?". Liefert genau eine Zeile mit ID, Projektname, Kunde, Bearbeiter
> und Änderungsdatum.

---

## 3. Aufgabe 2 — Platzhalter-Weg unverändert

An der Datenschutzschicht wurde **nichts** geändert. Die Ergebniszeile läuft durch dasselbe
`KiRueckmeldung.Erzeuge(aufruf, ergebnis, platzhalter)` wie jede andere Aktion
(`KiChatService.cs:1189`). Beleg aus dem Prüflauf, wörtlich das, was an das Modell geht:

```json
{"aktion":"projekt_aktiv","status":"ausgefuehrt","anzahl":1,
 "text":"Aktuell geöffnet ist das Projekt Name 1 (ID 1042).",
 "zeilen":[{"id":1042,"projektname":"Name 1","kunde":"","bearbeiter":"","geaendert":"Name 2"}]}
```

Die ID geht unverändert hinaus — sie ist der einzige wörtliche Bezug, den das Modell
zurückgeben kann (Fachkonzept 4.2).

---

## 4. Aufgabe 3 — Rückeinsetzung bei der Anzeige

### 4.1 Befund: die Auflösung lag an der falschen Stelle

Der Auftrag ging davon aus, dass der Antworttext platzgehalten bei der Oberfläche ankommt. Im
Ist-Stand löste **der Dienst** ihn auf (`KiChatService.cs`, vor der Änderung Zeile 1188:
`platzhalter.Aufloesen(schlusstext)`). Der Klarname stand damit in `KiAntwort.Text` — und
`Form_KiChat` legt genau diesen Text in den Gesprächsverlauf (`:982`), der bei der **nächsten**
Frage wieder in den Prompt geht (`PromptBauen`, Block „Bisheriger Verlauf",
`KiChatService.cs:657–663`). Der Klarname wäre also ab der zweiten Frage beim Modellanbieter
gewesen — genau das, was die Platzhalterung verhindern soll.

**Umgesetzt ist deshalb die Fassung des Auftrags**, nicht die Bestandslogik:

| Fundstelle | Änderung |
|---|---|
| `KiChatService.cs:58` | neues Feld `KiAntwort.Platzhalter` (die Tabelle wird durchgereicht, nicht kopiert) |
| `KiChatService.cs:1017` | `antwort.Platzhalter = platzhalter;` — auf **jedem** Rückweg, auch bei den frühen Abbrüchen |
| `KiChatService.cs:1223` | `antwort.Text` bleibt **platzgehalten** (vorher: `Aufloesen`) |
| `Form_KiChat.cs:1578` | neue `private static string KlarnamenFuerAnzeige(string, KiPlatzhalter)` |
| `Form_KiChat.cs:963` | die Antwortzeile läuft durch diese Funktion |
| `Form_KiChat.cs:982` | der Verlaufseintrag **nicht** — mit Begründung im Code |

Damit gilt: Der Klarname geht keinen Schritt weiter als bis zur Bildschirmausgabe.
Sendevorschau, Protokollzeilen und „Was wird gesendet?" zeigen weiterhin den Platzhalter — sie
dokumentieren, was übertragen wurde, und wurden nicht angefasst.

### 4.2 Ersetzungsregeln

`KlarnamenFuerAnzeige` zählt die Tabelle wie `KiRueckmeldung.BekannteKlarnamen` über
`KiPlatzhalter.Anzahl` + `KiPlatzhalter.Klarname` auf — **ohne Eingriff in `KiKern`**.

* **Höchste Nummer zuerst** (`for i = Anzahl … 1`), sonst träfe „Name 1" den Anfang von „Name 12".
* **Nur ganze Vorkommen:** `(?<!\w)Name\ n\b`. Die Wortgrenze hinter der Ziffer fängt den Fall ab,
  den ein schlichtes `Replace` nicht abfängt: ein der Tabelle **unbekanntes** „Name 15" zerfällt
  nicht in „Name 1" + „5". `KiPlatzhalter.Aufloesen` (KiKern) sortiert zwar ebenfalls nach Länge,
  ersetzt aber ohne Wortgrenze — die Anzeigefunktion ist an dieser Stelle also strenger.
* `MatchEvaluator` statt Ersatzzeichenkette: ein „$" im Klarnamen wird nicht als Rückverweis
  gelesen (eigens geprüft, § 7 Block 5).

**Bewusste Verschärfung gegenüber dem Bestand:** Schreibt das Modell den Platzhalter verändert
(„Name1", „Name  1"), bleibt er stehen, statt halb ersetzt zu werden. Die Promptregel aus
Aufgabe 4 wirkt genau dagegen.

---

## 5. Aufgabe 4 — Promptregel

`PromptBauen`, `KiChatService.cs:609`, **ein** Satz im `mitAktionen`-Zweig, unmittelbar hinter der
bestehenden Platzhalterregel:

```csharp
sb.AppendLine("Bezeichner erscheinen als Platzhalter („Name 1“); übernimm sie unverändert.");
sb.AppendLine("Nenne den Platzhalter ruhig in deiner Antwort - das Programm zeigt dem Anwender an seiner Stelle den Klarnamen.");
```

Ohne diesen Satz weicht das Modell dem Platzhalter aus („das geöffnete Projekt") — dann kann das
Programm auch keinen Klarnamen einsetzen, weil nichts dasteht, was zu ersetzen wäre. Die
bestehenden Absätze sind unverändert.

---

## 6. Ressourcen

### 6.1 Vier neue Schlüssel, beide Sprachen (ans Dateiende angehängt)

| Schlüssel | de | en |
|---|---|---|
| `KI_REG_ZWECK_PROJEKT_AKTIV` | Zweck-Text aus § 2.3 | Names the project that is currently open … |
| `KI_REG_PROJEKT_AKTIV_GELESEN` | Aktuell geöffnet ist das Projekt {0} (ID {1}). | The project currently open is {0} (ID {1}). |
| `KI_REG_PROJEKT_AKTIV_KEINES` | Zurzeit ist kein Projekt geöffnet. | No project is open at the moment. |
| `KI_REG_PROJEKT_AKTIV_NICHT_GELESEN` | Die Kopfdaten zu ID {0} konnten nicht gelesen werden; es gelten die Angaben der Oberfläche. | The header data for ID {0} could not be read; the values shown by the user interface apply. |

Abgebildet in `KiAktionsTexte.cs:64` (Zweck) und `:122–124` (Meldungen) — die Aktionsdatei kennt
wie gehabt keinen Ressourcennamen.

### 6.2 `Resource.Designer.cs`

Wie bei H1/H2 und H4/H5 ist **Visual Studio zuvorgekommen**: die vier Eigenschaften waren
alphabetisch eingeordnet, bevor von Hand etwas hätte ergänzt werden können. Eine
Hand-Einfügung fand deshalb **nicht** statt; CS0102-Duplikate sind ausgeschlossen (der Build
ist der Prüfstein, § 7). Wer diesen Stand ohne laufendes Visual Studio nachbaut, muss die vier
Eigenschaften ggf. selbst ergänzen.

---

## 7. Beweise — Prüfharnisch `..\dev\h8probe\`

Wegwerf-Konsolenprojekt nach dem Vorbild `dev\h4probe` (gitignored, **keine** `.cs` unterhalb von
`WindowsFormsApplication1`), gebaut gegen `dev\build_h8\WindowsFormsApplication1.dll` und
`KiKern.dll`; `internal`-Mitglieder über Reflexion. **75 Prüfungen, alle grün**
(`ALLES GRUEN`, ExitCode 0). Kein einziger Aufruf bei Google — die Werkzeugrunde läuft über den
eingespeisten `KiChatService.Modellkanal`.

**Gegen zwei Wegwerf-Kopien der Datenbank, nie gegen den Produktivbestand.** Der Lauf legt
`dev\h8probe\db_mit\` (Kopie von `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`) und
`dev\h8probe\db_ohne\` (dieselbe Kopie mit geleerter `Tab_Applikation`) an und biegt
`Properties.Settings.Default.DBPath` in-process darauf um (ohne `Save()`, also nicht dauerhaft).
Damit landet auch das Aktionsprotokoll `ki_aktionen.txt` neben der Kopie und nicht in
`C:\ProgramData`. Die Produktivdatenbank war beim Lauf ungesperrt und wurde nur gelesen.

| # | Block | Kernergebnis |
|---|---|---|
| 0 | Wegwerf-Datenbanken | keine `.laccdb`; `DBPath` zeigt auf die Kopie; `Program.startfrm == null` |
| 1 | Registereintrag | Register führt **24** Aktionen; `projekt_aktiv` ist Stufe 1, **ohne Parameter**, Andockpunkt nennt `startfrm`; der Zweck nennt „aktuelle" + „Projekt" und die Wortlaute der Anwenderfrage; `projekte_auflisten`/`projekt_suchen`/`projekt_lesen` unverändert vorhanden |
| 2 | Ermittlung | ohne Startfenster greift `Tab_Applikation`: **id=1042**, Name „Booster-Kette mit Kombi-Speicher" |
| 3 | Ergebniszeile | `Ok`, **genau eine** Zeile mit allen fünf Feldern, lokal der Klarname; durch `KiRueckmeldung.Erzeuge`: **`Name 1` steht drin, der Klarname nirgends** — auch nicht im Ergebnissatz; `"id":1042` geht unverändert hinaus; Rückweg `Klarname("Name 1")` stimmt |
| 4 | Werkzeugrunde Ende zu Ende | Lauf erfolgreich, Tageszähler unverändert, 2 Runden; die Aktion lief; **(a)** Runde 2 trägt die Ergebniszeile mit Platzhalter; **(b)** *keine* der beiden Runden (4094 / 4471 Zeichen) enthält den Klarnamen; Promptregel steht im Prompt, die alte Regel ebenfalls; `KiAntwort.Text` = „… ist Name 1." (Platzhalter, **kein** Klarname), `KiAntwort.Platzhalter` ist dieselbe Instanz; **(c)** die Anzeige liefert „… ist Booster-Kette mit Kombi-Speicher." ohne Restplatzhalter; der Verlaufseintrag bliebe platzgehalten |
| 5 | Ersetzungsregeln | **(d)** Tabelle mit 12 *nicht* durchnummerierten Klarnamen (Eins…Zwoelf — bei „Projekt-12" bliebe der Fehler unsichtbar): „Zuerst Name 12, dann Name 1 und noch Name 2." → „Zuerst Zwoelf, dann Eins und noch Zwei.", **kein „Eins2"**; unbekanntes „Name 15" bleibt stehen, **kein „Eins5"**, das bekannte „Name 1" daneben wird trotzdem ersetzt; deutsche Anführungszeichen, Satzzeichen dahinter, „XName 4" (kein Treffer ohne Wortanfang), leere Tabelle, `null`-Tabelle, Klarname mit „$" |
| 6 | Kein Projekt geöffnet | **(e)** Ermittlung meldet „nichts offen" (id=0, Name leer); Ergebnis ist **`Ausgefuehrt`**, 0 Zeilen, Text = `KI_REG_PROJEKT_AKTIV_KEINES`; die Rückmeldung an das Modell führt `"text"` ohne `"zeilen"` |
| 7 | Ressourcen | alle vier Schlüssel in beiden Sprachen belegt und **verschieden**; `{0}`/`{1}` im Ergebnissatz; **`KiEinwilligung.FASSUNG == 2`** |

**Einwilligung im Prüflauf.** Wie bei H4: Der Riegel steht vor dem eingespeisten Kanal; der
Harnisch prüft `KiEinwilligung.BestaetigteFassung`, hängt bei fehlender Einwilligung
`Nachfragen = () => true` ein und **nimmt sie danach mit `Zuruecknehmen()` wieder zurück**. Auf
diesem Rechner lag keine Einwilligung vor; der Registry-Stand ist nachweislich wiederhergestellt
(`HKCU\Software\wp-plan` führt danach keinen Einwilligungswert, `KiZaehler` unverändert 4).

---

## 8. Build

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
  C:\Waermeplan\WP_Plan\WP-Plan.sln -p:Configuration=Debug -p:Platform=x64 `
  -p:OutDir=C:\Waermeplan\WP_Plan\dev\build_h8\
```

**0 Fehler.** Warnungen: **genau die 5 bekannten**, alle in nicht berührtem Code
(CS0109 ×2 `KlimaregionStammCtrl`, CS0108 `StromverbraucherStammCtrl`, CS0108 `WErzeugerModel`,
CS1998 `MDIMainForm`). **Keine neue Warnung.**

---

## 9. Abweichungen und Befunde

1. **`KiAntwort.Text` bleibt platzgehalten** (§ 4.1). Der Auftrag beschreibt genau das; der
   Bestand tat es nicht. Die Zeile `platzhalter.Aufloesen(schlusstext)` ist entfallen. Betroffen
   ist nur `FrageMitAktionenAsync` — der reine Hilfefall (`FrageAsync`) kennt keine Platzhalter.
   Einziger Aufrufer im Projekt ist `Form_KiChat`; die Sonden unter `..\KiHarnisch` gehören
   **nicht** zur Solution und prüfen an dieser Stelle nichts Platzgehaltenes.
2. **Wortgrenze statt schlichtem Ersetzen** (§ 4.2) — strenger als `KiPlatzhalter.Aufloesen`.
   Bewusst und beauftragt; `KiKern` bleibt unangetastet.
3. **Das Feld `geaendert` erreicht das Modell als Platzhalter.** Im Prüflauf oben steht
   `"geaendert":"Name 2"`. Ursache: `KiHilfe.Datum` liefert eine **Zeichenkette**, und
   `KiRueckmeldung.WertKnoten` ersetzt jede Zeichenkette eines Feldwertes vollständig durch
   ihren Platzhalter (`KiRueckmeldung.cs:330`, `:349`). Das ist **Bestandsverhalten** und trifft
   `projekte_auflisten` und `projekt_lesen` genauso; `projekt_aktiv` folgt bewusst dem Muster,
   statt als einzige Aktion auszuscheren. Wirkung: Das Modell kann das Änderungsdatum nicht
   nennen. Behebbar wäre es nur einheitlich — `KiHilfe.Datum` gäbe ein `DateTime` zurück, das
   `WertKnoten` bereits ungeschützt und invariant formatiert. **Offener Punkt, nicht in H8
   entschieden.**
4. **Befund außerhalb des Umfangs: abgelehnte Aufrufe können Klarnamen hinaustragen.**
   `KiRueckmeldung.Abgelehnt(name, grund)` säubert den Grund **nicht**
   (`KiRueckmeldung.cs:300`), und `KiRueckmeldung.Erzeuge` kann nur Namen ersetzen, die schon in
   der Tabelle stehen. Scheitert die Vorbedingung `KiHilfe.ProjektMussAufloesbarSein`, enthält
   der Grund über `KiHilfe.Aufzaehlen` bis zu **zwölf Projektnamen im Klartext**
   (`KiAktionen.cs:226–247`) — bei leerer Tabelle gehen sie ungeschützt an das Modell. Das ist
   **vorbestehend** und von H8 nicht berührt (`projekt_aktiv` hat weder Parameter noch
   Vorbedingung und kann den Fall nicht auslösen). Ein Fix gehört in ein eigenes Paket:
   entweder Sauberkeit in `KiAusfuehrer`/`KiChatService` vor dem Absenden, oder eine
   Aufzählung ohne Klarnamen.
5. **`Tab_Applikation` gilt nur ohne Startfenster** (§ 2.1). Der Auftrag nennt beide Quellen
   nebeneinander; die Ersatzquelle führt aber das *zuletzt geöffnete* und nicht das *aktive*
   Projekt. Mit laufender Oberfläche ohne geladenes Projekt lautet die Antwort deshalb „kein
   Projekt geöffnet" statt „das von gestern". Bewusst enger als der Wortlaut, siehe § 2.1.
6. **Der Prüfharnisch arbeitet gegen zwei Wegwerf-Kopien der Datenbank**, nicht gegen
   `C:\ProgramData\EPOS_PLAN`. Zusammen rund 300 MB unter `dev\h8probe\db_mit|db_ohne\`
   (gitignored); sie können jederzeit gelöscht werden, der nächste Lauf legt sie neu an.

---

## 10. Offene Prüfpunkte für die Abnahme (nur am laufenden Programm)

1. **Projekt öffnen, Chatfenster mit eingeschaltetem Aktionsbetrieb, fragen: „Wie heißt das
   aktuelle Projekt?"** → der Assistent ruft `projekt_aktiv` (Zeile „Ausgeführt: …") und nennt
   in der Antwort den **Klarnamen**.
2. **„Was wird gesendet?" unmittelbar danach** → die Vorschau zeigt weiterhin Platzhalter, nicht
   den Projektnamen. Ebenso die Protokollzeile unter dem Schritt.
3. **Zweite Frage in derselben Sitzung** („Und wer ist der Bearbeiter?") → im mitgesendeten
   Verlauf steht „Assistent: … Name 1 …", nicht der Klarname. Prüfbar über „Was wird gesendet?"
   vor dem Absenden.
4. **Ohne offenes Projekt** (Programmstart, kein Projekt geladen) → „Zurzeit ist kein Projekt
   geöffnet.", keine Fehlermeldung, kein roter Text.
5. **Englische Oberfläche** → Zweck und Meldungen erscheinen englisch; die Antwort selbst
   ebenfalls (Sprachregel aus H4 unverändert).
6. **Werkzeugliste von Hand** („Werkzeuge…") → `projekt_aktiv` steht in der Liste, lässt sich
   ohne Eingabefelder ausführen und zeigt eine Ergebniszeile.
7. **Projekt zwischenzeitlich gelöscht** (zweites Fenster, Projekt löschen, dann fragen) → die
   Meldung „Die Kopfdaten zu ID … konnten nicht gelesen werden" erscheint unter dem Schritt.

---

## 11. Nicht angefasst

Auf Weisung außerhalb des Umfangs und nachweislich unberührt (`git status` führt sie nicht):
`..\KiKern\` (eigenes Projekt, **inklusive `KiPlatzhalter` und `KiRueckmeldung`**),
`Allgemein\KI\KiSchreibschutz.cs`, `KiEinwilligung.cs` (Fassung bleibt 2), der
Einwilligungstext (es wird nichts Neues übertragen), Tageslimit und Rundendeckel,
`Allgemein\KI\WikiWissen.cs`, `HilfeWissen.cs`, `HilfeKontext.cs`, beide `CLAUDE.md`.
Es wurde **kein** Git-Schreibkommando ausgeführt und `GitHub_Sync.bat` nicht aufgerufen.

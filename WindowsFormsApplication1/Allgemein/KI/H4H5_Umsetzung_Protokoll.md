# H4 + H5 — Umsetzungsprotokoll

**Stand:** 29.08.2026
**Grundlage:** [`Konzept_Hilfesystem_Wikidokumentation.md`](../../../Konzept_Hilfesystem_Wikidokumentation.md),
3. Fassung, Abschnitte 5 (B1–B6), 7 (Entscheidungen), 8 (H4/H5) und 11 (Fallstricke).
**Vorgänger:** [`../Hilfe/H1H2_Umsetzung_Protokoll.md`](../Hilfe/H1H2_Umsetzung_Protokoll.md)
(Bausteine `WikiHelpCatalog`, `DokuUebersetzung.FuerAnzeige`, `Program.WIKI_STANDARD`).

**Umfang:** Paket **H4** (`WikiWissen`, Einspeisung an drei Stellen, Quellen-Links im Chat,
Online-Doku-Suche ohne KI) und **H5** (Hinweistext, Riegel). H3 (die 23 Wiki-Seiten) ist
**erledigt** — die Rubrik führt seit heute 23 Unterseiten, per API im Prüflauf nachgewiesen
(§ 8, Abschnitt 2). H6 ist nicht Teil dieser Umsetzung.

`KiEinwilligung.FASSUNG` bleibt **2** (Entscheid 7.5) — geprüft, siehe § 8.

---

## 1. Kodierungsbehandlung je Datei

Vor jeder Änderung strikt als UTF-8 gelesen (`UTF8Encoding(false, true)` in `try/catch`) und auf
`U+FFFD` geprüft. **Keine** der berührten Dateien ist CP1252 — das CP1252-Rezept kam wie schon bei
H1/H2 nirgends zum Einsatz. Der BOM-Zustand ist je Datei unverändert geblieben; die Umlaute wurden
nach jeder Änderung zurückgelesen.

| Datei | vorher | nachher | Werkzeug |
|---|---|---|---|
| `Allgemein\KI\WikiWissen.cs` | *(neu)* | UTF-8 ohne BOM, CRLF, 13 Umlaute rückgelesen | Write + PowerShell (CRLF-Angleich) |
| `Allgemein\KI\KiChatService.cs` | UTF-8 **ohne** BOM, CRLF | unverändert (316 Umlaute) | Edit |
| `Allgemein\KI\HilfeWissen.cs` | UTF-8 ohne BOM, CRLF | unverändert (152 Umlaute) | Edit |
| `Views\Help\Form_KiChat.cs` | UTF-8 ohne BOM, CRLF | unverändert (183 Umlaute) | Edit |
| `MyResource\Resource.resx` | UTF-8 **+BOM**, CRLF | unverändert (1727 Umlaute) | PowerShell (Einfügung vor `</root>`, Ersetzung in einem Wert) |
| `MyResource\Resource.en-US.resx` | UTF-8 +BOM, CRLF | unverändert | PowerShell (dito) |
| `MyResource\Resource.Designer.cs` | UTF-8 +BOM | UTF-8 +BOM | **von Visual Studio selbst regeneriert**, siehe § 7.3 |

Schlussprobe über alle sechs Quelldateien: strikt UTF-8 lesbar, **kein `U+FFFD`**; beide `.resx`
zusätzlich als XML geladen (je **2643** `<data>`-Knoten, wohlgeformt).

`WikiWissen.cs` wurde bewusst auf **CRLF** normalisiert — die Nachbardateien im Ordner (`HilfeWissen.cs`,
`KiChatService.cs`, `Form_KiChat.cs`) sind durchweg CRLF. (`DokuUebersetzung.cs` aus H1 ist LF; das
bleibt, wie es ist — `core.autocrlf=true` gleicht beim Einchecken ohnehin an.)

---

## 2. Aufgabe 1 — neue Klasse `Allgemein\KI\WikiWissen.cs` (Konzept B1)

`public static class WikiWissen`, Namespace `WindowsFormsApplication1`, 670 Zeilen.

### 2.1 Ablauf je Frage

| Schritt | Umsetzung | Fundstelle |
|---|---|---|
| Basis-URL | `Properties.Settings.Default.WordPressUrl`, leer → `Program.WIKI_STANDARD`; abschließender `/` wird abgeschnitten | `Basis()`, `:96` |
| a) Stichwörter | `Stichwoerter()` — Wörter **ab 4 Zeichen**, kleingeschrieben, `Distinct`, höchstens 8; identische Trennzeichen wie `HilfeWissen.Zerlegen` | `:118 TRENNER`, `:126` |
| b) Suche | `GET {basis}/rest.php/v1/search/page?q={stichwörter}&limit=5`; gelesen werden `title`, `anchor`, `description` | `SuchAdresse()`, `SuchtrefferAsync()` |
| Reihung | Treffer mit Titelpräfix `Programm Dokumentation/` vor die übrigen (`OrderBy` ist stabil, die Kontextseite bleibt vorn) | `SucheAsync(basis,…)` |
| c) Auszüge | `GET {basis}/api.php?action=query&prop=extracts&titles=A%7CB%7CC&explaintext=1&exlimit=max&format=json&redirects=1`, je Seite auf **6.000 Zeichen** gekappt | `AuszugAdresse()`, `Kappen()` |
| d) Kontextseite | Bereichstext aus der Kontextzeile → Tabelle `SEITE_JE_BEREICH` → Rubrik-Unterseite, **immer erster Abschnitt** | `BereichAus()`, `KontextSeite()` |
| e) Ergebnis | `WissensAbschnitt` mit Titel = Seitentitel, `Bereich = "Wiki"`, Inhalt = Auszug, `QuellUrl = {basis}/wiki/{Titel}` (+ `#anchor`) | `SeitenUrl()` |

Höchstens **3** Seiten je Frage (`MAX_SEITEN`).

### 2.2 Rückgabetyp — gewählter Eingriff

Beauftragt war die Wahl zwischen „`WissensAbschnitt` um ein Quell-URL-Feld erweitern" und „eigener
Ergebnistyp mit Konvertierung". Gewählt ist die **Erweiterung** (`HilfeWissen.cs:16–31`): ein
zusätzliches Feld `QuellUrl` (Vorgabe `""`) und ein optionaler vierter Konstruktorparameter.

Begründung: Der Renderblock „Hilfeabschnitte" in `KiChatService.PromptBauen` bleibt damit
**buchstäblich unverändert**, die Mischung mit `HilfeWissen.Suchen` ist eine Listenoperation ohne
Konvertierung, und die Anzeige liest `a.QuellUrl` direkt. Ein eigener Typ hätte an drei Stellen eine
Umwandlung gebraucht. Die Adresse geht **nicht** in den Prompt (im Prüflauf eigens nachgewiesen).

### 2.3 Cache

`%APPDATA%\wp-plan\wiki-wissen\`, je Seite **eine** Datei. Dateiname = SHA-256 über
`Basis-URL + "|" + Titel`, erste 16 Bytes hexadezimal (32 Zeichen) + `.txt`. Inhalt:

```
Titel: Programm Dokumentation/Simulation
Abgerufen: 2026-08-29T12:41:07Z
Quelle: https://wiki.epos-plan.de

<Klartext>
```

* Gültigkeit **24 h**; Reihenfolge **frischer Cache → Online → abgelaufener Cache → nichts**.
* Trefferlisten der Suche werden **nicht** gecacht (sie hängen an der Frage).
* Die Basis-URL geht in den Streuwert ein: sonst läge der Auszug eines anderen Wikis unter
  demselben Dateinamen — und die Offline-Probe mit falscher Basis (§ 8, Abschnitt 6) läse
  fälschlich Treffer aus dem Cache des echten Wikis.

### 2.4 Stille und Zeitgrenzen

Ein **statischer** `HttpClient` mit `Timeout = 4 s`; je Aufruf zusätzlich ein
`CancellationTokenSource.CreateLinkedTokenSource(abbruch)` mit `CancelAfter(4 s)` — der Abbruch des
Chatfensters wirkt damit durch. Jeder Fehler (HTTP ≠ 200, Zeitüberschreitung, unlesbares JSON,
Ausnahme) endet mit `Debug.WriteLine` und **leerem Ergebnis**; `SucheAsync` fängt zusätzlich
umfassend ab. Antworten werden ausdrücklich als **UTF-8** dekodiert (`ReadAsByteArrayAsync`), weil
die REST-Schnittstelle des Wikis `application/json` **ohne** Zeichensatzangabe meldet.

### 2.5 Zuordnungstabelle Bereich → Rubrikseite (B1.4)

Aufgestellt **gegen `HilfeKontext.POSITIVLISTE`**, nicht geraten. 27 Einträge; der Prüflauf vergleicht
sie maschinell gegen die Positivliste (28 Einträge) und gegen die 23 live vorhandenen Rubrikseiten.

| Bereich (Positivliste) | Rubrik-Unterseite |
|---|---|
| Administration | Einstellungen |
| Assistent (Wizard) | Kurzanleitung |
| BHKW · Brauchwasser · Heizkessel · Klimadaten · Photovoltaik · Projektverwaltung · Prozesswärme · Pufferspeicher · Simulation · Solarthermie · Stromspeicher · Stromverbraucher · Varianten · Wärmebedarf · Wärmepumpe · Wirtschaftlichkeit · Gebäude | gleichnamig |
| Detaillierte Simulation | Simulation |
| Hauptfenster | Programmablauf |
| Hilfe | Hilfe-Assistent |
| Kosten und Preise | Kosten |
| Simulation Konfiguration *(Kurzform)* | Simulation |
| Simulation Konfiguration (Erzeuger definieren, Pufferspeicher zuordnen) | Simulation |
| Wärmequelle Erdreich (Quellsystem, Bodentyp, Auslegungsprüfung VDI 4640) | Wärmepumpe |
| *Ergebnis* (kein Positivlisten-Bereich, aber Bereich mehrerer Einbauabschnitte) | Simulation |

**Bewusst ohne Seite** (in der Rubrik existiert keine): `Unbekannter Bereich`, `Bericht`, `Lizenz`.

**Zwei Entscheidungen, die kein Namensgleichnis trägt — hier offen benannt:**

1. `Wärmequelle Erdreich (…)` → **Wärmepumpe**. Der Dialog `Form_QuelleErdreich` legt das
   Quellsystem *einer Wärmepumpe* fest (Bodentyp, VDI 4640); eine eigene Rubrikseite gibt es nicht.
   Die Alternative wäre `Simulation` gewesen (seine Geschwister `Form_QuellePufferspeicher` und
   `Form_Quellprofil` tragen den Bereich `Simulation`). Fachlich näher ist die Wärmepumpe.
2. `Hauptfenster` → **Programmablauf**. Das entspricht der Zuordnung aus H2
   (`Form_Start.btn_Help = Programmablauf`).

Die Auflösung nimmt zuerst den **genauen** Bereichstext; findet sich keiner, den **längsten
passenden Anfang**. Deshalb trifft sowohl die Kurzform `Bereich: Simulation Konfiguration` als auch
der volle Positivlisten-Wortlaut dieselbe Seite.

---

## 3. Aufgabe 2 — Einspeisung an allen drei Stellen (Konzept B2)

Eine gemeinsame Beschaffung statt dreier Kopien:

```csharp
internal static async Task<List<WissensAbschnitt>> AbschnitteBeschaffenAsync(
    string frage, string kontext, CancellationToken abbruch)      // KiChatService.cs:373
```
→ `WikiWissen.SucheAsync(...)`, danach `Mischen(...)` (`:386`): **Wiki zuerst, höchstens 3**
(`MAX_WIKI_ABSCHNITTE`), dann mit `HilfeWissen.Suchen` auf die bestehende Obergrenze **4**
(`MAX_ABSCHNITTE`, `:350`) aufgefüllt, **ohne Titeldoppel** (`OrdinalIgnoreCase`).

| Stelle | vorher | nachher |
|---|---|---|
| Hilfefall `FrageAsync` | `:376` `HilfeWissen.Suchen(frage, kontext, 4)` | `:475` `await AbschnitteBeschaffenAsync(frage, kontext, abbruch)` |
| Sendevorschau `SendeVorschau` | `:415` dito | `:519` dito |
| Aktionsbetrieb `FrageMitAktionenAsync` | `:912` dito | `:1036` dito |

Der Renderblock „Hilfeabschnitte" (`PromptBauen`) ist **unverändert**; die strikte Bindung
(„Stütze dich AUSSCHLIESSLICH …") bleibt ebenfalls stehen.

**Abbruchmarke.** `FrageMitAktionenAsync` reicht ihren vorhandenen `abbruch` durch.
`FrageAsync` hatte keinen — sie bekommt einen **optionalen** vierten Parameter
(`CancellationToken abbruch = default`, `:419`), der sowohl an `WikiWissen` als auch neu an den
Gemini-Aufruf geht (`AufrufenAsync` → `AufrufenMitModellAsync` → `SendenAsync` → `_http.SendAsync`,
`:655 ff.`). Damit ist „derselbe Token wie der Gemini-Aufruf" wörtlich erfüllt. Alle vorhandenen
Aufrufer (`Form_KiChat`, `..\KiHarnisch`) bleiben quelltextkompatibel.

**Antwort-Cache.** Der Treffer im prozessweiten Antwort-Cache steht weiterhin **vor** allem
anderen — eine gemerkte Antwort löst also **keinen** Wiki-Abruf aus. Damit die Quellenangabe
trotzdem erscheint, trägt die Kopie jetzt auch die Abschnitte (`:458`).

**Neu in `KiAntwort`:** `public List<WissensAbschnitt> Abschnitte` (`:37`) — genau das, was in den
Prompt ging. Grundlage der Anzeige (§ 5).

---

## 4. Aufgabe 3 — Promptregel Sprache

`PromptBauen`, `KiChatService.cs:559–571`. Der **deutsche Zweig ist zeichengleich zu vorher**:

```csharp
if (Program.nLanguage != 0)
{
    sb.AppendLine("Beantworte die Frage kurz und sachlich - höchstens 6 Sätze.");
    sb.AppendLine("Answer in English.");
}
else
{
    sb.AppendLine("Beantworte die Frage kurz, sachlich und auf Deutsch - höchstens 6 Sätze.");
}
```

> **Abweichung von der Vorgabe (bewusst, dokumentiert):** Beauftragt war eine *Zusatzregel*.
> Als reine Ergänzung hätte im englischen Fall „… und auf Deutsch …" **und** „Answer in English."
> nebeneinander gestanden — zwei sich widersprechende Anweisungen an das Modell. Ergänzt ist
> deshalb der Satz „Answer in English." im Wortlaut der Vorgabe, und im selben Zweig entfällt das
> widersprechende „auf Deutsch". Nachgewiesen in § 8, Abschnitt 8.

---

## 5. Aufgabe 4 — Quellenangabe im Chat (Konzept B3)

| Stelle | Änderung |
|---|---|
| `Form_KiChat.cs:200` | `_verlaufAnzeige.DetectUrls = true` (ausdrücklich statt implizit) |
| `:202` | `_verlaufAnzeige.LinkClicked += Verlauf_LinkClicked` |
| `:1477 Verlauf_LinkClicked` | öffnet die angeklickte Adresse über `DokuUebersetzung.FuerAnzeige(...)` und `Process.Start(UseShellExecute)`; **nur** `http://`/`https://` |
| `:1504 QuellenZeigen` | Kopfzeile `MyResource.Resource.KI_WIKI_QUELLEN`, danach je Abschnitt mit `QuellUrl` eine Zeile `• {Titel} — {Adresse}` |
| `:967` | Aufruf nach einer erfolgreichen Antwort, mit `antwort.Abschnitte` |
| `:995` | Aufruf im Fehlerpfad, mit denselben Abschnitten |

**Anzeige und Prompt stimmen überein** — und zwar ohne zweite Suche: Die frühere unabhängige
Anzeige-Suche (`:894` `HilfeWissen.Suchen(frage, _kontext, 4)`) ist **entfallen**; angezeigt wird
`antwort.Abschnitte`, also genau das, was der Dienst gesendet hat. Nur wenn der Dienst gar nicht
zum Zug kam (Riegel, Tageslimit — dann ist die Liste leer), sucht die Oberfläche ersatzweise lokal
(`:985–987`), ohne Netz.

**Angezeigt wird die deutsche Originaladresse**; der Übersetzungs-Proxy greift erst beim **Klick**
— dasselbe Muster wie `Form_HelpPopup` (H1). So steht im Verlauf die Adresse, die auch im Wiki gilt.

*Nebenwirkung, bewusst:* Der `LinkClicked`-Behandler gilt für die ganze Verlaufsanzeige. Eine vom
Anwender eingetippte oder vom Modell genannte Adresse wird damit ebenfalls anklickbar — deshalb die
Beschränkung auf `http`/`https`.

---

## 6. Aufgabe 5 — Online-Doku-Suche ohne KI (Entscheid 7.4)

Neu `Form_KiChat.cs:1021 DokuSucheZeigen(string frage)`; `FrageStellen(mitKi: false)` verzweigt
dorthin (`:934–938`). Der Pfad

* beschafft über **`KiChatService.AbschnitteBeschaffenAsync`** — dieselbe Kette wie der Prompt
  (Stichwörter → Wiki-Suche → Auszüge), plus das eingebaute Wissen;
* zeigt Trefferliste, Auszüge (auf 220 Zeichen gekürzt) **und** die Quellen-Links;
* **berührt weder `KiEinwilligung` noch den Einwilligungsriegel und ruft nichts bei Google**:
  `AbschnitteBeschaffenAsync` kennt nur `WikiWissen` und `HilfeWissen`;
* sperrt währenddessen „Fragen"/„Suchen" und zeigt `KI_WIKI_SUCHE_LAEUFT` in der Statuszeile;
* fällt offline auf Cache und Einbauwissen zurück (`WikiWissen` liefert dann still nichts).

Erreichbar sind damit beide Wege des Konzepts: der Knopf „Suchen"/„Nur suchen" und — im
Hilfe-Betrieb — die Eingabetaste (`Eingabe_KeyDown` ruft `FrageStellen(!_hilfeBetrieb)`).

---

## 7. Aufgabe 6 — Hinweistext und Riegel (H5, Entscheid 7.5)

### 7.1 Rechtshinweis: genau ein Satz ergänzt

Der Hinweistext liegt **nicht** in `Form_KiHinweis.cs`, sondern vollständig in den Ressourcen
`KI_HINWEIS_*` (die Maske setzt sie nur zusammen). Ergänzt wurde der Abschnitt **„Empfänger"**
(`KI_HINWEIS_EMPFAENGER`) — dort steht, wer etwas bekommt, und genau darum geht es:

* **de:** „… Für den produktiven Einsatz ist ein kostenpflichtiger Zugang vorgesehen.
  **Zur Suche in der Online-Dokumentation werden Stichwörter Ihrer Frage an wiki.epos-plan.de
  übertragen.**"
* **en:** „… A paid plan is intended for productive use. **Keywords from your question are sent to
  wiki.epos-plan.de to search the online documentation.**"

Der Abschnitt „Was übertragen wird" wurde bewusst **nicht** angefasst: seine drei Aufzählungspunkte
beschreiben durchweg den Verkehr mit dem Modellanbieter; ein Wiki-Punkt dazwischen würde die
Empfänger vermischen.

**`KiEinwilligung.FASSUNG` bleibt 2** — nicht erhöht, im Prüflauf eigens geprüft (§ 8, Abschnitt 9).

### 7.2 Derselbe Hinweis im Chatfenster

`HilfeBetriebAnwenden`, `Form_KiChat.cs:509–525`. Die Hinweiszeile über der Linkleiste war im
Hilfe-Betrieb bisher **ausgeblendet** (`_hinweisZeile.Visible = mitKi`). Nach Entscheid 7.4 gibt es
dort aber sehr wohl einen Datenfluss. Deshalb:

* **Regelbetrieb (unverändert):** „Ihre Frage geht im Wortlaut an einen externen Dienst (Google
  Gemini). [Rechtshinweis anzeigen]" — und der Rechtshinweis enthält seit 7.1 den Wiki-Satz.
* **Hilfe-Betrieb (neu):** Die Zeile bleibt **sichtbar** und trägt genau den Wiki-Satz
  (`KI_WIKI_HINWEIS_ZEILE`), ohne Verweis (`LinkArea(0, 0)`) — der Rechtshinweis beschreibt einen
  Dienst, den diese Installation nicht nutzt.

### 7.3 Datenschutz-Zusage im Klassenkopf

`KiChatService.cs:135–141`: Die Zusage bleibt wörtlich stehen und wird um den zweiten Datenfluss
ergänzt (Stichwörter statt Rohfrage, eigener Server, gilt auch ohne KI, Fassung unverändert, und:
Wiki-Auszüge *sind* Hilfetexte im Sinne der Zusage).

### 7.4 `Resource.Designer.cs`

Wie bei H1/H2 ist **Visual Studio zuvorgekommen**: rund eine Sekunde nach dem Schreiben der `.resx`
waren die drei neuen Eigenschaften bereits generiert und alphabetisch eingeordnet. Eine
Hand-Ergänzung fand deshalb **nicht** statt (Duplikate/CS0102 sind ausgeschlossen). Wer diesen Stand
ohne laufendes Visual Studio nachbaut, muss die drei Eigenschaften ggf. selbst ergänzen — der Build
ist der Prüfstein.

### 7.5 Neue Ressourcenschlüssel (beide Sprachen, ans Dateiende angehängt)

| Schlüssel | de | en |
|---|---|---|
| `KI_WIKI_HINWEIS_ZEILE` | Zur Suche in der Online-Dokumentation werden Stichwörter Ihrer Frage an wiki.epos-plan.de übertragen. | Keywords from your question are sent to wiki.epos-plan.de to search the online documentation. |
| `KI_WIKI_QUELLEN` | Quellen in der Online-Dokumentation: | Sources in the online documentation: |
| `KI_WIKI_SUCHE_LAEUFT` | Die Online-Dokumentation wird durchsucht... | Searching the online documentation... |

Geändert (kein neuer Schlüssel): `KI_HINWEIS_EMPFAENGER` in beiden Sprachen (§ 7.1).

---

## 8. Aufgabe 7 — Prüfharnisch `..\dev\h4probe\`

Wegwerf-Konsolenprojekt nach dem Vorbild `dev\h1probe` (gitignored, **keine** `.cs` unterhalb von
`WindowsFormsApplication1`), gebaut gegen `dev\build_h4\WindowsFormsApplication1.dll` und
`KiKern.dll`; `internal`-Mitglieder über Reflexion. **55 Prüfungen, alle grün** (`ALLES GRUEN`,
ExitCode 0).

| # | Block | Kernergebnis |
|---|---|---|
| 1 | Stichwortextraktion | „Wie funktioniert der Pufferspeicher?" → `funktioniert pufferspeicher`; die gebaute Adresse lautet `…/search/page?q=funktioniert%20pufferspeicher&limit=5`; **Assert: die Rohfrage kommt darin nicht vor**, `q` entschlüsselt ist genau die Stichwortliste, kein Wort unter 4 Zeichen; ein kurzer Klarname („Ott") fällt heraus |
| 2 | Zuordnungstabelle | 10 Einzelfälle (Kurz-/Langform, Registerkarte dahinter, Umlaute, Bereiche ohne Seite); **alle 28 Positivlisten-Bereiche außer den 3 bewussten haben eine Seite**; die Rubrik führt live **23** Unterseiten; **alle 22 Tabellenziele existieren dort** |
| 3 | Kappung | 20.000 Zeichen → **genau 6.000**, Marke `...` am Ende; 5.999 bleibt unangetastet |
| 4 | Live-Suche | 3 Abschnitte; **erster ist die Kontextseite** `Programm Dokumentation/Simulation`; Bereich durchweg `Wiki`; längster Auszug genau 6.000 (FAQ, roh 7.190); **alle drei Quell-URLs HTTP 200**; ohne Kontext führt die Suche (Grundlagen/Pufferspeicher …); auch `LetzteSuchAdresse` ohne Rohfrage |
| 5 | Cache | Ordner und **je Seite eine Datei** entstehen; nach Austausch eines Cache-Inhalts gegen eine Marke liefert der zweite Lauf **die Marke** → er kam nachweislich aus dem Cache; Zeiten 520 ms → 125 ms; 48 h alter Eintrag gilt bei 24 h nicht mehr, ist als letzte Stufe (`TimeSpan.MaxValue`) aber noch lesbar; der Dateiname hängt an der Basis-URL |
| 6 | Offline | falsche Basis `wiki.epos-plan.invalid` → **0 Abschnitte, keine Ausnahme**, 607 ms |
| 7 | Prompt ohne Google | Beschaffung: 4 Abschnitte, davon 3 Wiki, **Wiki vorn**, keine Titeldoppel. Über den eingespeisten `Modellkanal`: Anfragerumpf abgefangen, Prompt führt 4 Abschnitte, die Wiki-Abschnitte stehen **vor** den lokalen, die Kontextseite ist der erste, `KiAntwort.Abschnitte` deckt sich damit, **die Quell-URL steht nicht im Prompt**, der Tageszähler blieb bei 0. `SendeVorschau(mitAktionen: true)` liefert **denselben Prompt** wie der gesendete Rumpf; `SendeVorschau(mitAktionen: false)` führt dieselben Abschnitte |
| 8 | Sprachregel | DE: „auf Deutsch" da, „Answer in English." nicht; EN: umgekehrt |
| 9 | Ressourcen/Riegel | alle drei neuen Schlüssel in beiden Sprachen; `KI_HINWEIS_EMPFAENGER` nennt in beiden Sprachen `wiki.epos-plan.de`; **`KiEinwilligung.FASSUNG == 2`** |

### 8.1 Konflikt mit der Auftragsvorgabe — offen ausgewiesen

Beauftragt war der Ende-zu-Ende-Nachweis über **`FrageAsync` mit injiziertem `Modellkanal`**. Im
Ist-Code wertet **nur `FrageMitAktionenAsync` den `Modellkanal` aus** (`RundeSendenAsync`,
`KiChatService.cs:1419–1422`); der Hilfefall `FrageAsync` geht über `AufrufenMitModellAsync` und damit
immer über das echte HTTP. Der Kanal wurde **nicht** nachgerüstet — das wäre ein Eingriff in den
Transport des Hilfefalls gewesen, der über den Auftrag hinausgeht.

Ersatzweise nachgewiesen, ohne einen einzigen Google-Aufruf:
* Ende-zu-Ende über **`FrageMitAktionenAsync` + `Modellkanal`** — derselbe `PromptBauen`, dieselbe
  Beschaffung, dieselbe Mischregel;
* der Hilfefall über **`SendeVorschau(mitAktionen: false)`**, die per Bauart genau den Prompt von
  `FrageAsync` liefert (beide rufen `AbschnitteBeschaffenAsync` und `PromptBauen(…, false, null)`).

### 8.2 Einwilligung im Prüflauf

Der Einwilligungsriegel steht — wie dokumentiert — **vor** dem eingespeisten Kanal. Der Harnisch
prüft deshalb `KiEinwilligung.BestaetigteFassung`, hängt bei fehlender Einwilligung
`KiEinwilligung.Nachfragen = () => true` ein und **nimmt sie danach mit `Zuruecknehmen()` wieder
zurück**. Auf diesem Rechner lag keine Einwilligung vor; der Registry-Stand ist wiederhergestellt
(Meldung im Prüflauf). Bei gesetztem Abschalter überspringt der Harnisch diesen Block mit Meldung.

---

## 9. Aufgabe 8 — Build

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
  C:\Waermeplan\WP_Plan\WP-Plan.sln -p:Configuration=Debug -p:Platform=x64 `
  -p:OutDir=C:\Waermeplan\WP_Plan\dev\build_h4\
```

**0 Fehler.** Warnungen: **genau die 5 bekannten aus dem H1-Stand**, alle in nicht berührtem Code:

| Warnung | Stelle |
|---|---|
| CS0109 ×2 | `Controller\KlimaregionStammCtrl.cs:22,24` / `:23,48` |
| CS0108 | `Controller\StromverbraucherStammCtrl.cs:25,44` |
| CS0108 | `Model\WErzeugerModel.cs:6,20` |
| CS1998 | `MDIMainForm.cs:489,28` |

**Keine neue Warnung.** Insbesondere erzeugt keine der neuen `async`-Methoden CS1998.

---

## 10. Abweichungen im Überblick

1. **`exlimit=max` in der Auszugsadresse (Erweiterung gegenüber der Vorgabe).** Die beauftragte
   Sammelanfrage `titles=A%7CB%7CC` liefert an `wiki.epos-plan.de` **nur für die erste Seite** einen
   vollen Auszug; die Antwort meldet ausdrücklich `"exlimit" was too large for a whole article
   extracts request, lowered to 1` (am 29.08.2026 nachgemessen: Sammelanfrage über drei Titel →
   1× 7.190 Zeichen, 2× leer; einzeln abgefragt liefern dieselben Seiten 1.079 und 4.181 Zeichen).
   Umsetzung: Die Sammeladresse bleibt wie beauftragt und trägt zusätzlich `exlimit=max` (wirkt,
   sobald die Wiki-Einstellung es zulässt); jede Seite, die dabei leer bleibt, wird **einzeln**
   nachgeholt (`AuszugEinzelnAsync`). Ohne diesen Nachlauf hätte der Assistent je Frage nur **einen**
   Auszug bekommen.
2. **Sprachregel** — kein reines Hinzufügen, siehe § 4.
3. **Anzeige-Suche entfällt statt „auf dieselben Treffer gestützt"** — sie wird durch
   `KiAntwort.Abschnitte` ersetzt (§ 5). Das ist strenger als beauftragt: Anzeige und Prompt sind
   nicht nur gleich beschafft, sondern buchstäblich dieselbe Liste.
4. **`SendeVorschau` ist jetzt `async`** (`Task<string>`). Anders ließe sich die Vorschau nicht auf
   die Wiki-Abschnitte stützen. Einziger Aufrufer im Projekt ist `Form_KiChat.VorschauZeigen`
   (jetzt `async void`, mit `IsDisposed`-Prüfung nach dem Warten). `..\KiHarnisch` ruft die Methode
   nicht.
5. **`FrageAsync` bekam einen optionalen `CancellationToken`** (§ 3) — quelltextkompatibel.
6. **Hinweiszeile im Hilfe-Betrieb jetzt sichtbar** (§ 7.2) — eine bewusste Abkehr von der
   F5-Regel „Zeile beschreibt Dienstverkehr, den es hier nicht gibt", weil Entscheid 7.4 genau das
   geändert hat.

---

## 11. Offene UI-Prüfpunkte für die Abnahme

Nur am laufenden Programm prüfbar; der Harnisch erreicht sie nicht:

1. **Quellen-Links sind anklickbar.** Frage im Chat stellen → unter der Antwort steht
   „Quellen in der Online-Dokumentation:" mit einer Zeile je Wiki-Seite; die Adresse ist blau und
   der Klick öffnet den Browser. *(Technisch: `RichTextBox.DetectUrls` + `LinkClicked`.)*
2. **Englische Oberfläche:** derselbe Klick öffnet die Adresse über
   `wiki-epos--plan-de.translate.goog/...?_x_tr_sl=de&_x_tr_tl=en&_x_tr_hl=en`.
3. **Zeilenumbruch der Quellzeile.** Lange Titel + lange Adresse in einem schmalen Fenster —
   die Verlaufsanzeige bricht um; prüfen, dass die Adresse dabei anklickbar bleibt.
4. **Hilfe-Betrieb (KI abgeschaltet, `HKCU\Software\wp-plan\KiDeaktiviert = 1`):** Fenster öffnen →
   Hinweiszeile trägt den Wiki-Satz **ohne** Verweis; „Suchen" liefert Wiki-Treffer, Auszüge und
   Links; **kein** Google-Aufruf (mit einem Mitschnitt oder ohne Schlüssel prüfbar).
5. **Ohne Netz:** dieselbe Frage im Hilfe-Betrieb → Rückfall auf Einbauwissen, keine
   Fehlermeldung, Wartezeit spürbar unter 15 s.
6. **Sendevorschau** („Was wird gesendet?") öffnet weiterhin sofort und zeigt die Wiki-Abschnitte;
   das Fenster darf durch das neue `await` nicht doppelt aufgehen (schnelles Doppelklicken prüfen).
7. **Statuszeile** zeigt beim Suchen „Die Online-Dokumentation wird durchsucht..." und wird danach
   wieder leer; „Fragen"/„Suchen" sind währenddessen gesperrt.
8. **Rechtshinweis** (Chatfenster → „Rechtshinweis anzeigen"): Abschnitt „Empfänger" endet mit dem
   Wiki-Satz; die Fußzeile meldet **keine** neue Fassung, der Dialog erscheint bei Bestandsnutzern
   **nicht** erneut (Prüfliste 9, Punkt 11).
9. **Prüfliste 9 des Konzepts:** Punkte 7–11 sind mit diesem Stand abfahrbar; 1–6, 12–14 gehören zu
   H1–H3 und bleiben unberührt, 15 ist mit H1/H2 § 9 erledigt.

Ebenfalls offen (aus H1/H2 übernommen, hier nicht angefasst): `txt_WPPrefix`/`lbl_WPPrefix` restlos
aus dem Designer entfernen; Popup-Kurzbeschreibungen (A5/7.6) und die `doku_suchen`-Aktion (B6)
gehören nach H6.

---

## 12. Nicht angefasst

Auf Weisung außerhalb des Umfangs und nachweislich unberührt: `..\KiKern\` (eigenes Projekt),
`Allgemein\KI\KiSchreibschutz.cs` (Zusage aus `KONTEXT_Stammdaten_Aenderbarkeit.md` §3.4 —
`WikiWissen` ist reine Lesequelle), `Allgemein\Update\SchemaMigration.cs`, `Allgemein\DbWerte.cs`,
sämtlicher Emissions-/CO₂-Bezug, beide `CLAUDE.md`, das Konzeptdokument selbst,
`Allgemein\Hilfe\help_mapping.txt` und `help_cache.json` (H2-Stand), `KiEinwilligung.cs`
(Fassung bleibt 2). Erscheinen diese Dateien im `git status` als geändert, stammen die Änderungen
aus der parallel laufenden Sitzung. Es wurde **kein** Git-Schreibkommando ausgeführt und
`GitHub_Sync.bat` nicht aufgerufen.

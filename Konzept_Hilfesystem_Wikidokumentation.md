# Hilfesystem auf die Wiki-Dokumentation umstellen — Analyse und Vorgehensweise

Stand: 29.08.2026. Betrachtet wurde der Bestand unter `WindowsFormsApplication1`
(ohne Altkopien und Worktrees) sowie — per API und Seitenabruf empirisch verifiziert —
das Wiki unter `https://wiki.epos-plan.de`.

**Aufgabe.** Die Info-Buttons und Info-Verweise der Anwendung sollen auf die Seiten der
Wiki-Rubrik **[Programm Dokumentation](https://wiki.epos-plan.de/wiki/Programm_Dokumentation)**
verweisen, und der **Hilfe-Assistent** soll die Wiki-Dokumentation
(`https://wiki.epos-plan.de/wiki/`) als Wissensquelle nutzen — statt des heutigen
statischen Einbauwissens und der WordPress-Altbestände.

Dieses Dokument ist reine Analyse und Planung — es wurde keine Zeile Code geändert.

---

## 1. Kurzfassung des Befunds

Der Umzug aufs Wiki ist **halb begonnen und dabei liegengeblieben**: Die Basis-URL des
Hilfe-Katalogs zeigt seit einiger Zeit hartkodiert auf `https://wiki.epos-plan.de`
(`Program.cs:148`), der Abruf verwendet aber weiterhin die **WordPress-API-Form**
(`/rest.php/v1/pages?per_page=…&_fields=slug,link,title`), die MediaWiki nicht anbietet —
der Aufruf liefert **HTTP 404** (am 29.08.2026 empirisch geprüft). Da der Katalog jeden
Fehler still schluckt, fällt die Anwendung unbemerkt auf den **eingebetteten Startbestand**
`help_cache.json` zurück: 116 Einträge, **alle mit `epos-plan.de`-Zielen**. Die 26
Info-Buttons funktionieren also, öffnen aber die alte WordPress-Doku.

Der Hilfe-Assistent überträgt an Gemini ein statisches Einbauwissen (17 Abschnitte,
≈ 10 kB) plus **Titel-Stubs** aus derselben `help_cache.json` — echten Dokumentationstext
sieht er nie.

Das Wiki ist auf den Anschluss **bereits vorbereitet**: Die Rubrikseite
`Programm Dokumentation` existiert, benennt genau die drei Aufrufwege aus dem Programm,
regelt Umbenennungen (Weiterleitung bleibt) und Sprungmarken (`Vorlage:Anker`, ergänzen
statt löschen) — ihr Abschnitt „Hilfeseiten“ ist aber noch Platzhalter. Alle nötigen
API-Wege sind vorhanden und **funktionieren nachweislich**: Seitenliste, Volltextsuche
(mit Abschnitts-Ankern) und Klartext-Auszüge (Extension TextExtracts).

Die Aufgabe zerfällt in drei Teile: **A** — den Katalog-Lader auf MediaWiki umstellen und
das Mapping der 26 Buttons auf Wiki-Seitentitel umziehen; **B** — dem Assistenten einen
Wiki-Abruf (Suche + Auszüge, mit Cache) vorschalten; **C** — Redaktion: den Abschnitt
„Hilfeseiten“ der Rubrikseite als Vertragsliste füllen und die Sprungmarken setzen.
Neue Seiten sind kaum nötig — für alle 26 Buttons existieren passende Wiki-Seiten.

---

## 2. Ist-Zustand

### 2.1 Das dreistufige Hilfesystem

So beschreibt es die Wiki-Seite „Einstellungen, Hilfe und Sprache“, und so ist es gebaut:

| Stufe | Mechanik | Code |
|---|---|---|
| **Kurzhinweis am Eingabefeld** (Info-Button) | `HelpExtender` zeigt Popup mit Titel + Link, Klick öffnet Browser | `Allgemein\Hilfe\HelpCatalog.cs`, `HilfeAutomatik.cs`, `Views\Help\Form_HelpPopup.cs` |
| **Menüpunkt Dokumentation** | öffnet die Doku-Startseite im Browser | `MDIMainForm.cs:810–830` |
| **Hilfe-Assistent** (F1) | Chatfenster, Gemini-REST, lokale Wissenssuche | `Allgemein\KI\`, `Views\Help\Form_KiChat.cs` |

### 2.2 Info-Buttons und Hilfe-Katalog

**Bestand:** 26 Info-Buttons (Namenskonvention `btn_Help*`, automatische Registrierung
über `Application.Idle` in `HilfeAutomatik.cs:89`), dazu 26 Zeilen in der eingebetteten
Zuordnungsdatei `Allgemein\Hilfe\help_mapping.txt` — **1:1 deckungsgleich, keine Lücke**.
Zeilenformat: `Formname.Controlpfad = de-ziel | en-ziel`, wobei jedes Ziel ein
WordPress-**Slug** (`klimadaten`) oder ein **Link-Pfad** (`/epos-plan/…/klimadaten/`)
sein darf.

**Ablauf heute:**

1. `Program.cs:148` erzeugt den Katalog mit **hartkodierter** Basis-URL
   `https://wiki.epos-plan.de` — der konfigurierbare Weg über
   `Properties.Settings.Default.WordPressUrl` steht **auskommentiert daneben**.
2. `HelpCatalog.cs:355` ruft `{_baseUrl}/rest.php/v1/{WordPressPrefix}?per_page=100&page=N&_fields=slug,link,title`
   auf — WordPress-Form auf MediaWiki-Host ⇒ **404** (geprüft 29.08.2026). Timeout 10 s,
   Fehler nur nach `Debug.WriteLine`.
3. Fallback-Rangfolge (`HelpCatalog.cs:434–467`): Online → Sicherung
   `%APPDATA%\<ProductName>\help_cache.json` → eingebetteter Startbestand. Der
   Startbestand (`Allgemein\Hilfe\help_cache.json`, 116 Einträge) trägt **ausschließlich
   `epos-plan.de`-URLs** — er ist das, was die Anwender heute tatsächlich öffnen.
4. `HelpEntry.Url` wird wörtlich aus dem `link`-Feld übernommen und unverändert an
   `Process.Start` gegeben (`Form_HelpPopup.cs:202`). **Keine Anker** — die
   Pfadnormalisierung schneidet `#` sogar ab (`HelpCatalog.cs:187–208`).
5. Sprachwahl (`HelpCatalog.cs:1033–1055`): `Program.nLanguage != 0` ⇒ EN-Hälfte der
   Mapping-Zeile, sonst DE; einzelnes Ziel gilt für beide Sprachen.

**Nebenbefunde:** Der Popup-Linktext ist hartkodiert deutsch (`Form_HelpPopup.cs:82–84`
— Verstoß gegen die Drei-Schichten-Regel); Fehlerbilder sind durchweg still (Button wird
nur grau); die Konstante `MDIMainForm.DOKU_URL` (`MDIMainForm.cs:808`) zeigt auf
`https://epos-plan.de/epos-plan/epos-plan-dokumetation/` (Tippfehler im Original) und
wird vom LinkLabel im Chatfenster (`Form_KiChat.cs:249–264`) sowie als Klartext im
KI-Basiswissen (`HilfeWissen.cs:173`) verwendet. Der Menüpunkt **Dokumentation** liest
dagegen bereits `Settings.WordPressUrl` (Default `https://wiki.epos-plan.de`,
`app.config:41–43`) und landet damit heute schon auf der Wiki-Hauptseite.

**Konfigurierbarkeit:** Nur der REST-**Prefix** (`WordPressPrefix`, Default `pages`) und
die Menüpunkt-URL sind über `Form_AdminSettings` änderbar; die Katalog-Basis-URL nicht
(hartkodiert). Eine Datenbank-Beteiligung gibt es nicht — `Tab_Applikation` und
`Tab_Einstellungen` führen keinerlei Hilfe-/URL-Spalten (per Code-Grep geprüft).

### 2.3 Hilfe-Assistent

Kern in `Allgemein\KI\KiChatService.cs` (Aktionslogik im separaten Projekt `KiKern\`,
das von diesem Konzept **nicht berührt** wird).

**Prompt-Aufbau** (`PromptBauen()`, `KiChatService.cs:447–529`), alles als Text-Part der
ersten `user`-Runde, kein `systemInstruction`-Feld:

1. Rolle + Grundregeln (fest im Code); ohne Aktionsbetrieb gilt: „Stütze dich
   AUSSCHLIESSLICH auf die unten stehenden Hilfeabschnitte.“
2. Kontextzeile aus `HilfeKontext.Beschreibung()` (Positivlisten-Architektur, ~110
   Formulartypen zugeordnet, Projektname wird entfernt).
3. **Bis zu 4 „Hilfeabschnitte“** aus `HilfeWissen.Suchen()` — Stichwortscoring über
   den lokalen Korpus.
4. Letzte 4 Verlaufseinträge (Antworten auf 400 Zeichen gekappt), dann die Frage.

**Wissenskorpus** (`HilfeWissen.cs`): 17 fest einkompilierte Abschnitte (≈ 10 kB
Nutztext, Schwerpunkt Simulation/Konfiguration) **plus** die Einträge aus
`%APPDATA%\…\help_cache.json` — von denen aber nur das Feld `Tooltip` übernommen wird,
und das enthält konstruktionsbedingt **nur den Seitentitel** (`HelpCatalog.cs:405–410`).
Der Assistent kennt also Navigationsstichworte, aber **keinen einzigen Satz echten
Dokumentationstext** aus der Online-Doku. Der Korpus wird einmal aufgebaut und statisch
gecacht, ohne Invalidierung (`HilfeWissen.cs:42–52`).

**Rahmenbedingungen, die jede Erweiterung einhalten muss:**

| Riegel | Wert | Stelle |
|---|---|---|
| Einwilligung (versioniert, `FASSUNG = 2`) vor **jeder** Übertragung | Zusage: „Übertragen werden ausschließlich Hilfetexte, die Frage des Benutzers und eine grobe Kontextangabe“ | `KiChatService.cs:318–323`, `:120–122`; `KiEinwilligung.cs:47` |
| Tageslimit | 50 Anfragen | `KiChatService.cs:229` |
| Timeout / Ausgabelimit | 30 s / 400 bzw. 600 Tokens | `:241`, `:222/:228` |
| Antwort-Cache | prozessweit (in-memory), Schlüssel Kontext+Frage | `:244`, `:354–365` |
| Hilfe-Betrieb ohne KI | bei Abschaltung reine lokale Suche, kein Dienstaufruf | `Form_KiChat.cs:485–488` |
| `KiSchreibschutz` | bleibt unangetastet (Zusage aus `KONTEXT_Stammdaten_Aenderbarkeit.md` §3.4) | `Allgemein\KI\KiSchreibschutz.cs` |

**Vorbereitete, heute wirkungslose Kopplung:** `KiAktionenDialog.Hilfetext()`
(`Aktionen\KiAktionenDialog.cs:699–711`) ruft `Program.HelpCatalog.Get(feld.HilfeSlug)` —
kein Katalogfeld trägt bislang einen Slug.

### 2.4 Das Wiki (empirisch erhoben, 29.08.2026)

- **MediaWiki 1.46.0**, deutsch, Citizen-Skin; Artikel unter `/wiki/<Titel>`, Action-API
  unter `/api.php` (Wurzel, nicht `/w/`), REST-API unter `/rest.php/v1/`. Ohne Anmeldung
  lesbar — laut Rubrikseite bewusst, „damit die Software sie jederzeit öffnen kann“.
- **55 Seiten**, klares Schema: acht Rubriken (Erste Schritte, Programmablauf,
  Programmfunktionen, Grundlagen, Industrie und Gewerbe, Beispiele, Installation und
  Update, FAQ), 14 Unterseiten `Grundlagen/<Thema>`, je Wizard-Schritt eine Ablaufseite
  (Projekt anlegen … Varianten und Bericht), je Gewerk eine Dialogseite (Heizkessel,
  Pufferspeicher, Photovoltaik, Wärmepumpe, Solarthermie, Stromspeicher,
  Blockheizkraftwerk), dazu Meta-Seiten (Hauptseite, FAQ, Systemvoraussetzungen,
  Update-Logbuch, Impressum, Datenschutz, …).
- **Verifizierte API-Fähigkeiten:**

| Zweck | Aufruf | Ergebnis der Probe |
|---|---|---|
| Seitenliste | `api.php?action=query&list=allpages&aplimit=500&format=json` | 55 Titel, kein `continue` |
| Volltextsuche | `api.php?action=query&list=search&srsearch=…` | Treffer + Snippets |
| Suche mit Abschnitts-Ankern | `rest.php/v1/search/page?q=…&limit=…` | Treffer mit `title`, `excerpt`, `anchor`, `description` |
| Klartext-Auszug | `api.php?action=query&prop=extracts&titles=…&explaintext=1` | TextExtracts installiert; z. B. „Hilfe-Assistent“ ≈ 4.700 Zeichen |
| WordPress-Form (heutiger Lader) | `rest.php/v1/pages?per_page=…` | **HTTP 404** |

- **Rubrikseite `Programm Dokumentation`** (Wortlaut liegt vor): definiert sich als Heimat
  der „Hilfeseiten, die EPOS-Plan direkt aus dem Programm heraus aufruft – über die
  Kurzhinweise an den Eingabefeldern, den Menüpunkt Dokumentation und den
  Hilfe-Assistenten“. Pflegeregeln stehen bereits fest: Umbenennung nur mit bleibender
  Weiterleitung; Sprungmarken mit `Vorlage:Anker`, bei Bedarf um neue Namen **ergänzt
  statt gelöscht**. Abschnitt „Hilfeseiten“ (`{{Anker|hilfeseiten|seiten}}`) ist noch
  Platzhalter: „Die Hilfeseiten zu den einzelnen Dialogen werden nach und nach ergänzt.“

---

## 3. Zielbild

Ein Host, ein Katalog, eine Vertragsseite:

```
                    ┌────────────────────────────── wiki.epos-plan.de ───────────────┐
                    │  Rubrikseite „Programm Dokumentation“ = Vertragsliste           │
                    │  (welche Seite/Sprungmarke von welchem Programmteil gerufen wird)│
                    └──────────────────────────────────────────────────────────────────┘
                         ▲                    ▲                          ▲
        Seitenliste      │      Seite öffnen  │       Suche + Auszüge    │
        (allpages)       │      (/wiki/Titel#Anker)   (search, extracts) │
                         │                    │                          │
┌────────────────┐   ┌───┴────────────┐   ┌───┴──────────┐   ┌───────────┴──────────┐
│ MediaWiki-Lader│──▶│ Hilfe-Katalog  │──▶│ Info-Buttons │   │ WikiWissen (neu)     │
│ (ersetzt 404-  │   │ (HelpEntry m.  │   │ + Menüpunkt  │   │ Suche→Auszüge→Cache  │
│  WordPress-Weg)│   │  Titel/URL)    │   │ Dokumentation│   │        │             │
└────────────────┘   └────────────────┘   └──────────────┘   └────────┼─────────────┘
                                                                      ▼
                                                        „Hilfeabschnitte“ im Gemini-
                                                        Prompt (bestehender Renderweg)
```

Der Assistent zitiert künftig echte Doku-Auszüge und nennt die Quellseiten als
klickbare Links; die Info-Buttons öffnen Wiki-Seiten samt Sprungmarken. `epos-plan.de`
bleibt für Lizenz, Portal und AGB zuständig (`Form_Lizenz`, `LizenzServerClient`) —
diese Wege sind **nicht** Teil der Aufgabe.

---

## 4. Teil A — Info-Buttons und Info-Verweise aufs Wiki

### A1 — Katalog-Lader auf MediaWiki umstellen

`LoadAllCoreAsync` (`HelpCatalog.cs:355 ff.`) ruft künftig
`{_baseUrl}/api.php?action=query&list=allpages&aplimit=500&format=json` (Fortsetzung über
`apcontinue`, obwohl bei 55 Seiten derzeit nicht nötig) und bildet je Seite einen
`HelpEntry`:

- `Tooltip` = Seitentitel (wie bisher — das Popup zeigt „Kapitel: {titel}“),
- `Url` = `{_baseUrl}/wiki/{Titel, URL-kodiert, Leerzeichen→Unterstrich}`,
- `Slug` = normalisierter Titel (für die Mapping-Auflösung).

Die bestehende Auflösungslogik (Slug-Abgleich, Pfadnormalisierung mit Kleinschreibung,
Suffix-Suche) bleibt unverändert — sie ist host-agnostisch, solange Katalog-URL und
Mapping-Ziel durch dieselbe Normalisierung laufen. Die Einstellung `WordPressPrefix`
verliert ihre Funktion und wird stillgelegt (Feld im Admin-Dialog entfernen oder
ausblenden; der Settings-Wert bleibt aus Kompatibilität stehen).

### A2 — Basis-URL wieder konfigurierbar

`Program.cs:148` zurück auf den auskommentierten Weg:
`new WordPressHelpCatalog(Properties.Settings.Default.WordPressUrl)` — damit steuert
**ein** Einstellwert (Default `https://wiki.epos-plan.de`) Katalog **und** Menüpunkt
Dokumentation, und das vorhandene Admin-Feld `txt_OnlineDokuUrl` wirkt wieder auf beides.
Der historische Settings-Name `WordPressUrl` bleibt (eine Umbenennung würde gespeicherte
Anwenderwerte in `user.config` verlieren); im Admin-Dialog heißt das Feld ohnehin neutral.

### A3 — Mapping auf Wiki-Titel und Sprungmarken

`help_mapping.txt` wird umgeschrieben: Ziele sind künftig **Wiki-Seitentitel** (mit
Unterstrichen), optional mit Sprungmarke `Titel#anker`. Dafür braucht der Bestand zwei
kleine Erweiterungen:

1. **Anker-Durchlass:** Der Anker wird vor der Katalog-Auflösung vom Ziel getrennt und
   erst beim Öffnen wieder an `HelpEntry.Url` angehängt — die Normalisierung
   (`HelpCatalog.cs:187–208`) und der Abgleich bleiben ankerfrei. Der Mapping-Parser
   (`:759–812`) darf `#` nur am Zeilenanfang als Kommentar werten (bei der Umsetzung
   prüfen — die Zeilen `epos-plan-…#…` gab es bisher nicht).
2. **Startbestand erneuern:** `Allgemein\Hilfe\help_cache.json` wird einmalig aus der
   `allpages`-Antwort neu generiert (55 Einträge, Wiki-URLs) — `EmbeddedResource` mit
   festem `LogicalName` beibehalten (`WindowsFormsApplication1.csproj:164–194`). Damit
   stimmen Online- und Offline-Stand erstmals überein.

**Zuordnungsvorschlag** für die 26 Buttons (22 distinkte Ziele; alle vorgeschlagenen
Seiten **existieren bereits** — Redaktionsentscheid in Teil C, Randfrage 7.2):

| Mapping-Zeile (heutiges DE-Ziel) | Vorschlag Wiki-Seite |
|---|---|
| `epos-plan-programmablauf` | `Programmablauf` |
| `epos-plan-kurzanleitung` (2×: Form_Start, WizardParent) | `Erste Schritte` |
| Wärmebedarfsberechnung (Form_Start, Form_Waermebedarf) | `Wärmebedarf erfassen` |
| `waermebedarfsrechnung` (Gebäude, Brauchwasser, Prozesswärme) | `Wärmebedarf erfassen` mit Ankern `#gebaeude`, `#brauchwasser`, `#prozesswaerme` (Teil C legt sie an) — Grundlagenvertiefung: `Grundlagen/Wärmebedarfsrechnung` |
| `strombedarf` / `strombedarfsberechnung` | `Strombedarf erfassen` (Vertiefung `Grundlagen/Strombedarf und Lastprofile`) |
| `klimadaten` | `Klimadaten festlegen` (Vertiefung `Grundlagen/Klimadaten`) |
| `kostenrechnung` | `Kosten und Energiepreise` (Vertiefung `Grundlagen/Kostenrechnung`) |
| `waermepumpe` | `Wärmepumpe` |
| `kessel-spitzenlast` | `Heizkessel` (Vertiefung `Grundlagen/Kessel und Spitzenlast`) |
| `bhkw` | `Blockheizkraftwerk` |
| `solarkollektoren` | `Solarthermie` |
| `photovoltaik` | `Photovoltaik` |
| `pufferspeicher` | `Pufferspeicher` |
| `stromspeicher` | `Stromspeicher` |
| `hydraulikschemata` | `Grundlagen/Hydraulikschemata` |
| Wirtschaftlichkeitsrechnung | `Wirtschaftlichkeit` |
| `vergleich-energiebilanz` | `Grundlagen/Vergleich Energiebilanz` (alternativ `Varianten und Bericht`) |
| `projektverwaltung` (2×: Speichern-unter, Importkonflikte) | `Projektverwaltung` |
| `epos-plan-systemvoraussetzungen` | `Systemvoraussetzungen` |
| `epos-plan-faq` (KiChat) | `Hilfe-Assistent` (alternativ `FAQ`) |

Leitlinie dabei: Der Info-Button an einem **Eingabedialog** zielt auf die zugehörige
**Dialog-/Ablaufseite** (dort steht, was einzugeben ist), nicht auf die
Grundlagen-Unterseite (Theorie) — die ist von dort verlinkt.

### A4 — Streuverweise nachziehen

- `MDIMainForm.DOKU_URL` (`MDIMainForm.cs:808`) auf `https://wiki.epos-plan.de`
  umstellen (Tippfehler-URL entfällt); Konstante bleibt reiner Not-Fallback, führend ist
  der Settings-Wert.
- LinkLabel „Online-Dokumentation öffnen“ im Chatfenster (`Form_KiChat.cs:249–264`)
  folgt damit automatisch; der Klartext-Verweis im KI-Basiswissen (`HilfeWissen.cs:173`)
  wird auf die Wiki-URL umformuliert.
- Popup-Linktext (`Form_HelpPopup.cs:82–84`) über `MyResource.Resource.*` lokalisieren
  (beide Satelliten-`.resx` — Ordner `Help` hat bisher keine `de-DE.resx`, Katalogpflege
  nach `Lokalisierung_Katalog.md`).

Lizenz-, AGB-, Impressums- und Portal-URLs auf `epos-plan.de` bleiben unverändert.

### A5 — Option: sprechende Kurzbeschreibungen

Die REST-Suche liefert je Seite ein `description`-Feld; alternativ liefert TextExtracts
mit `exintro` Einleitungssätze (Batch bis 20 Titel je Aufruf). Damit könnte das Popup
statt nur „Kapitel: {titel}“ eine Ein-Satz-Beschreibung zeigen. Bewusst als Option
geführt — Mehrwert klein, eigener Abrufweg (Randfrage 7.6).

---

## 5. Teil B — Hilfe-Assistent nutzt die Wiki-Dokumentation

### B1 — Neuer Baustein `WikiWissen`

Neue Klasse `Allgemein\KI\WikiWissen.cs` (bewusst im Anwendungsprojekt, nicht in
`KiKern` — sie braucht HttpClient und `%APPDATA%`). Ablauf je Frage:

1. **Suchbegriffe lokal ableiten** — dieselbe Stichwortlogik wie `HilfeWissen.Suchen`
   (Wörter ≥ 4 Zeichen): an das Wiki geht eine kurze Stichwortliste, **nicht die
   Rohfrage** (Begründung in B5).
2. **Suche:** `rest.php/v1/search/page?q=…&limit=5` — liefert Titel, Snippet,
   Abschnitts-`anchor` und `description` in einem Aufruf (verifiziert).
3. **Auszüge:** für die besten 2–3 Titel
   `api.php?action=query&prop=extracts&titles=A|B|C&explaintext=1&format=json`;
   je Seite auf ~6.000 Zeichen kappen (Prompt-Zusatz gesamt ≤ ~18 kB — für die
   Flash-Lite-Modelle unkritisch, `maxOutputTokens` bleibt 400/600).
4. **Kontextseite bevorzugen:** Aus dem `HilfeKontext`-Bereich wird über eine kleine
   Zuordnungstabelle (Bereich → Wiki-Titel, gepflegt neben `BEREICH_JE_TYP`) die
   passende Dialogseite bestimmt und ihr Auszug immer als erster Abschnitt geführt.
5. **Ergebnisform:** `WissensAbschnitt`e (Titel = Seitentitel, Bereich = `"Wiki"`,
   Inhalt = Auszug) + Quell-URL je Abschnitt für die Anzeige.

**Cache:** `%APPDATA%\wp-plan\wiki-wissen\` — je Seite eine Datei (Titel, Abrufzeit,
Text), Gültigkeit 24 h; die Trefferlisten der Suche werden nicht gecacht. Reihenfolge:
frischer Cache → Online → abgelaufener Cache → nichts. Kurzer eigener Timeout (~4 s je
Aufruf), damit das Chatfenster nie am Wiki hängt; alle Abrufe asynchron, Fehler still
mit Rückfall.

**Mischregel im Prompt:** Wiki-Abschnitte zuerst (max. 3), dann mit `HilfeWissen`
auf die bestehende Obergrenze auffüllen — das statische Einbauwissen bleibt als
Offline-Rückfallebene und für die Simulation-Detailthemen, die im Wiki (noch) fehlen.
Die bisherige Titel-Stub-Übernahme aus `help_cache.json` (`HilfeWissen.cs:80–85`)
bleibt unverändert bestehen (liefert nach Teil A Wiki-Titel).

### B2 — Einspeisestellen (alle drei, sonst laufen Vorschau und Wirklichkeit auseinander)

| Stelle | Rolle |
|---|---|
| `KiChatService.cs:376` | Beschaffung im Hilfefall (`FrageAsync`) |
| `KiChatService.cs:912` | Beschaffung im Aktionsbetrieb (`FrageMitAktionenAsync`) |
| `KiChatService.cs:415` | Beschaffung in der Sendevorschau (`SendeVorschau`) |

Der Renderblock „Hilfeabschnitte“ (`:493–505`) bleibt unverändert — `WikiWissen` liefert
dieselbe Abschnittsform. Die strikte Prompt-Bindung („Stütze dich AUSSCHLIESSLICH …“)
bleibt bestehen und wird durch die echten Doku-Auszüge erstmals inhaltlich tragfähig.

### B3 — Quellenangabe im Chat

Unter der Antwort zeigt das Chatfenster die verwendeten Wiki-Seiten als klickbare Links
(`Titel — https://wiki.epos-plan.de/wiki/…#anchor`, Anker aus der Suche, sofern
vorhanden). Anzeigetexte über `MyResource.Resource.*` (beide Sprachen). Die zweite,
unabhängige Anzeige-Suche in `Form_KiChat.cs:894` nutzt dieselben Treffer, damit Anzeige
und Prompt übereinstimmen.

### B4 — Hilfe-Betrieb ohne KI

Ist der Assistent abgeschaltet (`KiEinwilligung`), arbeitet das Chatfenster heute als
reine lokale Hilfesuche. Empfehlung: In diesem Betrieb **nur Cache und Einbauwissen**
verwenden, keine Wiki-Abrufe — „abgeschaltet“ soll verlässlich „keine Netzzugriffe des
Assistenten“ bedeuten (die Info-Buttons rufen das Wiki ja ohnehin erst beim Klick).
Randfrage 7.4, falls stattdessen die Doku-Suche auch dort online gehen soll.

### B5 — Datenschutz und Einwilligung

Die Zusage der Einwilligung (Fassung 2) — „Übertragen werden ausschließlich Hilfetexte,
die Frage des Benutzers und eine grobe Kontextangabe“ — bleibt gegenüber Google
**unverändert wahr**: Wiki-Auszüge *sind* Hilfetexte. Neu ist ein zweiter Datenfluss:
Stichwörter der Frage gehen an `wiki.epos-plan.de` (eigener Server, TLS, ohne Anmeldung).
Durch die lokale Stichwortextraktion (B1) wird keine Rohfrage übertragen. Empfehlung:
den Hinweistext um einen Satz ergänzen („Zur Suche in der Online-Dokumentation werden
Stichwörter Ihrer Frage an wiki.epos-plan.de übertragen“) — ob das die Fassung auf 3
hebt (erneute Bestätigung durch Bestandsnutzer), ist Randfrage 7.5.

### B6 — Option: Werkzeug `doku_suchen`

Im Aktionsbetrieb (Weg A) könnte eine 20. Aktion dem Modell erlauben, selbst
nachzuschlagen (Registrierung über `KiAktionen.Erzeuge`, Rundendeckel 3 lässt einen
Nachschlag zu; die vorbereitete Stelle `KiAktionenDialog.Hilfetext()` zeigt das Muster).
Bewusst Option: Die Vorab-Beschaffung (B1) wirkt in **beiden** Wegen und im Hilfefall,
das Werkzeug nur im Aktionsbetrieb. Erst nach B1 bewerten.

---

## 6. Teil C — Redaktion und Vertragsliste im Wiki

Kein Anwendungscode — Pflege über die bekannten Browser-Rezepte.

1. **Abschnitt „Hilfeseiten“ füllen:** Auf `Programm Dokumentation` entsteht die
   Vertragstabelle: *Programmstelle (Formular/Button) → Seite → Sprungmarke*. Quelle ist
   die Zuordnungstabelle aus A3; die Rubrikseite ist damit das Gegenstück zur
   eingebetteten `help_mapping.txt` — wer eine Seite umbenennen will, sieht dort, was
   daran hängt.
2. **Sprungmarken setzen:** Für die Mehrfach-Nutzer von `Wärmebedarf erfassen`
   (Gebäude, Brauchwasser, Prozesswärme) `{{Anker|gebaeude}}` usw. an die passenden
   Abschnitte — gemäß der bestehenden Regel „ergänzen statt löschen“. Anker-Namen
   ASCII-klein ohne Umlaute (sie werden Teil der URL und der Mapping-Datei).
3. **Bereichs-Zuordnung für B1.4:** je `HilfeKontext`-Bereich die passende Wiki-Seite
   benennen (28 Positivlisten-Werte, viele teilen sich eine Seite).
4. Der Menüpunkt Dokumentation zeigt weiterhin auf die **Hauptseite** (Einstieg mit
   Navigation); die Rubrikseite bleibt Pflege-/Vertragsseite und wird nicht als
   Anwenderziel verlinkt.

---

## 7. Zu entscheiden, bevor programmiert wird

1. **Englische Zielseiten.** Das Wiki ist rein deutsch; die EN-Hälften des Mappings
   zeigen heute auf `epos-plan.de/english/…`. Nach dem Umbau kennt der Katalog nur noch
   Wiki-Seiten — die EN-Ziele würden ins Leere laufen. Wege: **(a)** EN-Hälfte zeigt
   übergangsweise auf dieselben deutschen Wiki-Seiten (ein Katalog, ein Host — englische
   Oberfläche, deutsche Doku); **(b)** der Katalog lädt zusätzlich die
   WordPress-EN-Seiten (zweite Quelle, zwei API-Formen — genau die Mischlage, die wir
   gerade auflösen); **(c)** EN-Seiten im Wiki anlegen (`<Titel>/en`-Unterseiten), dann
   EN-Hälfte darauf. Empfehlung: **(a) sofort, (c) als eigenes Redaktionspaket** — (b)
   nicht.
2. **Dialogseite oder Grundlagenseite** als Button-Ziel je Gewerk (A3-Leitlinie:
   Dialogseite). Bestätigen oder je Button abweichend festlegen.
3. **`WordPressPrefix`-Feld** im Admin-Dialog: entfernen oder nur ausblenden?
4. **Hilfe-Betrieb ohne KI** (B4): strikt offline (Empfehlung) oder Doku-Suche online?
5. **Einwilligungs-Fassung** (B5): reicht die Textergänzung, oder Fassung 3 mit
   erneuter Bestätigung?
6. **Popup-Kurzbeschreibungen** (A5): mitnehmen oder weglassen?

---

## 8. Vorgehensweise

Sechs Pakete; H1–H3 stellen die Info-Verweise um (Teil A + C), H4–H5 den Assistenten
(Teil B), H6 ist Ausbau. H1 ist ohne H2 wirkungslos, H2 ohne H1 gefährlich (Wiki-Titel
gegen WordPress-Katalog) — **H1+H2 in einem Zug umsetzen und ausliefern**.

### H1 — Katalog auf MediaWiki (A1, A2, A4)

Lader auf `list=allpages`, Basis-URL aus den Settings, `DOKU_URL`/LinkLabel/Basiswissen
nachziehen, Popup-Text lokalisieren. Beweis: Debug-Protokoll zeigt „online geladen,
55 Einträge“; Sicherung in `%APPDATA%` enthält Wiki-URLs.

### H2 — Mapping und Startbestand (A3)

`help_mapping.txt` gemäß Zuordnungstabelle (nach Entscheid 7.1/7.2), Anker-Durchlass im
Extender, `help_cache.json` neu generiert. Beweis: alle 26 Buttons öffnen die
vorgesehene Wiki-Seite (Prüfliste 9).

### H3 — Redaktion Wiki (Teil C)

Vertragstabelle auf der Rubrikseite, Anker in `Wärmebedarf erfassen`,
Bereichs-Zuordnung dokumentiert. Browser-Arbeit nach den bestehenden Rezepten.

### H4 — `WikiWissen` (B1–B3)

Suche + Auszüge + Cache, Einspeisung an den drei Stellen, Quellen-Links im Chat.
Beweis: Sendevorschau zeigt Wiki-Abschnitte; Antwort zu „Pufferspeicher“ zitiert die
Wiki-Seite; Netzstecker-Test fällt sauber auf Einbauwissen zurück.

### H5 — Riegel und Einwilligung (B4, B5)

Hilfe-Betrieb-Verhalten festziehen, Hinweistext ergänzen (nach Entscheid 7.5),
Prüfliste 9 vollständig abfahren.

### H6 — Ausbau (Optionen)

`doku_suchen`-Aktion (B6), Popup-Kurzbeschreibungen (A5), EN-Seiten im Wiki (7.1c).

---

## 9. Prüfliste für die Abnahme

| # | Prüfung | Erwartung |
|---|---|---|
| 1 | Jeden der 26 Info-Buttons klicken (DE) | vorgesehene Wiki-Seite öffnet, Sprungmarken-Ziele springen zum Abschnitt |
| 2 | Sprache auf EN, Stichprobe der Buttons | Ziel gemäß Entscheid 7.1, kein grauer Button |
| 3 | Erststart ohne Netz (Startbestand) | Buttons aktiv, Ziele sind Wiki-URLs (aus erneuertem `help_cache.json`) |
| 4 | Start mit Netz, danach offline | Sicherung aus `%APPDATA%` greift, Stand identisch online/offline |
| 5 | Menüpunkt Dokumentation | Wiki-Hauptseite; geänderte URL im Admin-Dialog wirkt auf Menü **und** Katalog |
| 6 | Assistent: Frage zu einem Wiki-Thema | Antwort stützt sich auf Doku-Auszug, Quell-Link unter der Antwort stimmt |
| 7 | Assistent: Sendevorschau | zeigt die Wiki-Abschnitte, identisch mit dem tatsächlich Gesendeten |
| 8 | Assistent offline | Rückfall auf Einbauwissen, keine Fehlermeldung, Antwortverhalten wie bisher |
| 9 | KI abgeschaltet (Hilfe-Betrieb) | Verhalten gemäß Entscheid 7.4; keinerlei Google-Aufruf (Riegel greift vor allem) |
| 10 | Tageslimit/Einwilligung | unverändert wirksam; bei Fassungswechsel erscheint die Nachfrage genau einmal |
| 11 | F1 | öffnet weiterhin den Assistenten (nicht die Wiki-Seite) |
| 12 | `KiSchreibschutz`-Stichprobe | Katalog-/`_STAMM`-Schreibversuch des Assistenten weiterhin abgelehnt |
| 13 | Wiki-Seite testweise umbenannt (mit Weiterleitung) | Button folgt der Weiterleitung — Regel der Rubrikseite trägt |

Referenzläufe sind nicht betroffen (keine Engine-Änderung); ein Regressionslauf nach H4
ist trotzdem billig und empfohlen, weil `Form_KiChat` im MDI-Umfeld hängt.

---

## 10. Aufwand

| Paket | Umfang | Aufwand |
|---|---|---|
| H1 Katalog | 1 Lader-Methode, `Program.cs`, 3 Streustellen, 2 `.resx` | 4–6 h |
| H2 Mapping | 26 Zeilen, Anker-Durchlass, Startbestand generieren | 3–5 h |
| H3 Redaktion | Rubrikseite, Anker, Bereichstabelle | 3–5 h |
| H4 WikiWissen | 1 neue Klasse, 3 Einspeisestellen, Chat-Links, Cache | 8–12 h |
| H5 Riegel/Abnahme | Hinweistext, 13 Prüfungen × 2 Sprachen | 4–6 h |
| H6 Ausbau | je Option | separat |

**Kernumfang H1–H5: rund 22–34 h.** H1+H2 als erster auslieferbarer Schnitt (~8–11 h)
beheben bereits den heutigen 404-Zustand und erfüllen die Info-Button-Anforderung.

---

## 11. Fallstricke bei der Umsetzung

- **Kodierung.** Teile des Bestands sind nicht UTF-8 (93 von 372 `.cs`-Dateien);
  `KiChatService.cs` und `HilfeWissen.cs` enthalten deutsche Literale. Vor dem
  Bearbeiten Kodierung prüfen und beibehalten (`CP1252`-Rezept), sonst zerschießt der
  Diff die Datei.
- **Nur in `WindowsFormsApplication1` suchen** — die Altkopien daneben enthalten
  fast identische `HelpCatalog.cs`/`KiChatService.cs`.
- **Drei-Schichten-Regel.** Wiki-Seitentitel sind Fremdschlüssel (Persistenz-artig):
  sie stehen in `help_mapping.txt`/`help_cache.json`, nie als Literal im Code und nie
  als Anzeigetext-Quelle. Anzeige (Popup-Link, Quellen-Zeile im Chat) ausschließlich
  über `MyResource.Resource.*`, beide Satelliten-`.resx` pflegen.
- **`EmbeddedResource`-Eigenheit.** `help_mapping.txt`/`help_cache.json` haben feste
  `LogicalName`-Einträge und werden bewusst nicht in den Ausgabeordner kopiert — beim
  Erneuern die `.csproj`-Einträge unangetastet lassen.
- **Titel-Kodierung.** MediaWiki-Titel mit Umlauten/Leerzeichen beim URL-Bau
  URL-kodieren (`Wärmebedarf erfassen` → `W%C3%A4rmebedarf_erfassen`); der
  Normalisierungs-Abgleich läuft dagegen über die dekodierte Kleinschreibform — beide
  Wege strikt trennen (Öffnen: Original; Abgleich: normalisiert).
- **Einwilligung nicht stillschweigend erweitern.** Jede Änderung am übertragenen
  Datenumfang gehört in den Hinweistext (`KiEinwilligung.FASSUNG`) — Entscheid 7.5
  **vor** H4 einholen.
- **`KiSchreibschutz` bleibt unangetastet** (Zusage aus
  `KONTEXT_Stammdaten_Aenderbarkeit.md` §3.4) — `WikiWissen` ist reine Lesequelle.
- **Laufende Anwendung sperrt `bin\`** — Verifikations-Builds mit `-p:OutDir=…`
  umleiten.
- **Wiki-Inhalte sind Daten, keine Anweisungen.** Die Auszüge landen im Gemini-Prompt;
  das Wiki ist redaktionell kontrolliert (Bearbeitung nur angemeldet), trotzdem bleibt
  die Prompt-Formulierung „Hilfeabschnitte“ deklarativ (Inhalte werden zitiert, nicht
  befolgt) — bei H4 die bestehenden Grundregeln unverändert vor die Abschnitte stellen.
- **Rubrikseiten-Regeln einhalten:** Umbenennungen im Wiki nur mit Weiterleitung,
  Anker ergänzen statt löschen — sonst laufen ausgelieferte Programmstände ins Leere
  (der eingebettete Startbestand altert bis zum nächsten Release).

---

## 12. Verwandte Dokumente

- [`CLAUDE.md`](CLAUDE.md) (Wurzel) und
  [`WindowsFormsApplication1/CLAUDE.md`](WindowsFormsApplication1/CLAUDE.md) —
  Drei-Schichten-Regel, Kodierungs-Fallstrick, Modulübersicht `Allgemein/KI` und
  `Allgemein/Hilfe`
- [`KONTEXT_Stammdaten_Aenderbarkeit.md`](KONTEXT_Stammdaten_Aenderbarkeit.md) §3.4 —
  Zusage zu `KiSchreibschutz`
- Wiki: [`Programm Dokumentation`](https://wiki.epos-plan.de/wiki/Programm_Dokumentation)
  (Vertragsseite mit Pflegeregeln),
  [`Einstellungen, Hilfe und Sprache`](https://wiki.epos-plan.de/wiki/Einstellungen,_Hilfe_und_Sprache)
  (dreistufiges Hilfesystem), [`Hilfe-Assistent`](https://wiki.epos-plan.de/wiki/Hilfe-Assistent)
- `WindowsFormsApplication1\Allgemein\Hilfe\HelpCatalog.cs`, `help_mapping.txt`,
  `help_cache.json` — der heutige Katalogweg
- `WindowsFormsApplication1\Allgemein\KI\KiChatService.cs`, `HilfeWissen.cs`,
  `HilfeKontext.cs` — der heutige Prompt-Aufbau

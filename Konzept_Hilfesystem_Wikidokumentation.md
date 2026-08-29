# Hilfesystem auf die Wiki-Dokumentation umstellen — Analyse und Vorgehensweise

Stand: 29.08.2026, **3. Fassung** — alle Randfragen entschieden.
**Umgesetzt am 29.08.2026 (H1–H5):** 23 Hilfeseiten + Vertragstabelle im Wiki,
Katalog/Zuordnung/EN-Proxy im Code, `WikiWissen` für den Assistenten, Doku-Suche ohne
KI, Hinweistext. Protokolle:
[`WindowsFormsApplication1/Allgemein/Hilfe/H1H2_Umsetzung_Protokoll.md`](WindowsFormsApplication1/Allgemein/Hilfe/H1H2_Umsetzung_Protokoll.md)
und
[`WindowsFormsApplication1/Allgemein/KI/H4H5_Umsetzung_Protokoll.md`](WindowsFormsApplication1/Allgemein/KI/H4H5_Umsetzung_Protokoll.md).
Offen: UI-Abnahme am laufenden Programm (Prüfliste §9) und H6-Optionen.
**Ausbaustufe H7 umgesetzt am 29.08.2026** (auf Nutzerwunsch): 73 zusätzliche
Info-Buttons (3 Startmasken-Tabs + 70 Hauptdialoge, zentrale Klasse
`Allgemein/Hilfe/InfoKnopf.cs`) und 9 neue Rubrikseiten — Bestand jetzt **99
Zuordnungen auf 32 Unterseiten**; Popup-Position wird am Bildschirmrand geklemmt.
Inventar-, Entscheidungs- und Umsetzungsprotokoll:
[`WindowsFormsApplication1/Allgemein/Hilfe/H7_InfoButtons_Protokoll.md`](WindowsFormsApplication1/Allgemein/Hilfe/H7_InfoButtons_Protokoll.md).
**H8 umgesetzt am 29.08.2026**: Aktion `projekt_aktiv` + Klarnamen-Rückeinsetzung
ausschließlich in der Chat-Anzeige — dabei ein Platzhalter-Leck geschlossen (aufgelöste
Namen wären über den Gesprächsverlauf ab der zweiten Frage doch übertragen worden).
Protokoll:
[`WindowsFormsApplication1/Allgemein/KI/H8_ProjektAktiv_Protokoll.md`](WindowsFormsApplication1/Allgemein/KI/H8_ProjektAktiv_Protokoll.md).
Betrachtet wurde der Bestand unter `WindowsFormsApplication1` (ohne Altkopien und
Worktrees) sowie — per API und Seitenabruf empirisch verifiziert — das Wiki unter
`https://wiki.epos-plan.de`.

**Aufgabe.** Die Info-Buttons und Info-Verweise der Anwendung sollen auf die Seiten der
Wiki-Rubrik **[Programm Dokumentation](https://wiki.epos-plan.de/wiki/Programm_Dokumentation)**
verweisen, und der **Hilfe-Assistent** soll die Wiki-Dokumentation
(`https://wiki.epos-plan.de/wiki/`) als Wissensquelle nutzen — statt des heutigen
statischen Einbauwissens und der WordPress-Altbestände.

**Vorgaben und Entscheidungen vom 29.08.2026:**

- Das bestehende Mapping von EPOS-Plan wird **nicht** übernommen. Alle Hilfeseiten
  werden **neu über das Wiki angelegt** — je Dialog eine eigene Unterseite der Rubrik;
  die App-Zuordnung entsteht vollständig neu, kein WordPress-Slug und kein Alt-Ziel
  wird konvertiert.
- **Englisch per Online-Übersetzung** über den Proxy in der App (7.1a), **kein
  WordPress mehr** im Hilfesystem (7.3), **Doku-Suche auch ohne KI** online (7.4).
- Info-Buttons öffnen **immer eine Seite der Rubrik** (7.2, Startinhalt echtes
  Kurzgerüst statt Weiterleitung), Einwilligung nur **Textergänzung** ohne
  Fassungswechsel (7.5), Popup-Kurzbeschreibungen kommen in H6 (7.6). Details:
  Abschnitt 7.

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
statt löschen) — ihr Abschnitt „Hilfeseiten“ ist aber noch Platzhalter („werden nach und
nach ergänzt“). Alle nötigen API-Wege sind vorhanden und **funktionieren nachweislich**:
Seitenliste, Volltextsuche (mit Abschnitts-Ankern) und Klartext-Auszüge (Extension
TextExtracts). Auch der Google-Übersetzungs-Proxy für die englische Anzeige ist
empirisch geprüft (A6).

Die Aufgabe zerfällt in drei Teile: **A** — den Katalog-Lader auf MediaWiki umstellen
und die App-Zuordnung **vollständig neu** gegen die Rubrik aufbauen (das alte
WordPress-Mapping und der eingebettete Altbestand werden ersatzlos verworfen, nichts wird
konvertiert); **B** — dem Assistenten einen Wiki-Abruf (Suche + Auszüge, mit Cache)
vorschalten, der auch **ohne KI** als Online-Doku-Suche arbeitet; **C** — Redaktion:
**je Dialog eine neue Hilfeseite** als Unterseite der Rubrik anlegen (26 Buttons →
23 Unterseiten) und den Abschnitt „Hilfeseiten“ als Vertragsliste führen. Die bestehende
Dokumentation (Programmablauf, `Grundlagen/…`, Gewerkeseiten) bleibt unangetastet und
wird von den neuen Hilfeseiten **verlinkt**, nicht ersetzt.

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
Dokumentationstexts** aus der Online-Doku. Der Korpus wird einmal aufgebaut und statisch
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
  Update-Logbuch, Impressum, Datenschutz, …). Das Unterseiten-Muster mit Schrägstrich
  (`Grundlagen/BHKW`) ist also **etablierte Praxis**.
- **Verifizierte API- und Abruf-Fähigkeiten:**

| Zweck | Aufruf | Ergebnis der Probe |
|---|---|---|
| Seitenliste | `api.php?action=query&list=allpages&aplimit=500&format=json` | 55 Titel, kein `continue` |
| Volltextsuche | `api.php?action=query&list=search&srsearch=…` | Treffer + Snippets |
| Suche mit Abschnitts-Ankern | `rest.php/v1/search/page?q=…&limit=…` | Treffer mit `title`, `excerpt`, `anchor`, `description` |
| Klartext-Auszug | `api.php?action=query&prop=extracts&titles=…&explaintext=1` | TextExtracts installiert; z. B. „Hilfe-Assistent“ ≈ 4.700 Zeichen |
| Englisch per Übersetzungs-Proxy | `https://wiki-epos--plan-de.translate.goog/wiki/<Titel>?_x_tr_sl=de&_x_tr_tl=en&_x_tr_hl=en` | Seite „Pufferspeicher“ vollständig und sauber übersetzt geladen |
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

Ein Host, ein Katalog, eine Rubrik mit eigenen Hilfeseiten:

```
                    ┌────────────────────────────── wiki.epos-plan.de ───────────────┐
                    │  Rubrik „Programm Dokumentation“: je Dialog eine NEUE Unterseite │
                    │  (Programm Dokumentation/<Kurzname>) + Vertragsliste auf der     │
                    │  Rubrikseite; bestehende Doku wird von dort verlinkt             │
                    └──────────────────────────────────────────────────────────────────┘
                         ▲                    ▲                          ▲
        Seitenliste      │      Seite öffnen  │       Suche + Auszüge    │
        (allpages mit    │      (/wiki/Programm_Dokumentation/…,         │
         apprefix)       │       EN via Übersetzungs-Proxy)              │
                         │                    │       (search, extracts) │
┌────────────────┐   ┌───┴────────────┐   ┌───┴──────────┐   ┌───────────┴──────────┐
│ MediaWiki-Lader│──▶│ Hilfe-Katalog  │──▶│ Info-Buttons │   │ WikiWissen (neu)     │
│ (ersetzt 404-  │   │ (HelpEntry m.  │   │ + Menüpunkt  │   │ Suche→Auszüge→Cache  │
│  WordPress-Weg)│   │  Titel/URL)    │   │ Dokumentation│   │   │            │     │
└────────────────┘   └────────────────┘   └──────────────┘   ┌───┴────────┐ ┌─┴────────────┐
                                                             │ Doku-Suche │ │ „Hilfeab-    │
                                                             │ ohne KI    │ │ schnitte“ im │
                                                             │ (Chat-     │ │ Gemini-      │
                                                             │  fenster)  │ │ Prompt       │
                                                             └────────────┘ └──────────────┘
```

Der Assistent zitiert künftig echte Doku-Auszüge und nennt die Quellseiten als
klickbare Links; ohne KI-Schlüssel/Einwilligung arbeitet dasselbe Fenster als
Online-Doku-Suche (Entscheid 7.4); die Info-Buttons öffnen die neuen Hilfe-Unterseiten
der Rubrik. `epos-plan.de` bleibt für Lizenz, Portal und AGB zuständig (`Form_Lizenz`,
`LizenzServerClient`) — diese Wege sind **nicht** Teil der Aufgabe und kein
WordPress-Rest im Sinne von 7.3.

---

## 4. Teil A — Info-Buttons und Info-Verweise aufs Wiki

### A1 — Katalog-Lader auf MediaWiki, Geltungsbereich = die Rubrik

`LoadAllCoreAsync` (`HelpCatalog.cs:355 ff.`) ruft künftig

```
{_baseUrl}/api.php?action=query&list=allpages&apprefix=Programm%20Dokumentation/&aplimit=500&format=json
```

(Fortsetzung über `apcontinue`; `apprefix` URL-kodiert). Der Katalog enthält damit
**genau die Hilfeseiten der Rubrik** — die Rubrikseite selbst hat keinen Schrägstrich
und bleibt automatisch außen vor. Je Seite entsteht ein `HelpEntry`:

- `Tooltip` = Kurzname (Titelteil hinter `Programm Dokumentation/` — das Popup zeigt
  „Kapitel: {titel}“),
- `Url` = `{_baseUrl}/wiki/{Titel, URL-kodiert, Leerzeichen→Unterstrich}`,
- `Slug` = normalisierter Kurzname (für die Mapping-Auflösung).

Die bestehende Auflösungslogik (Slug-Abgleich, Pfadnormalisierung mit Kleinschreibung,
Suffix-Suche) bleibt unverändert — sie ist host-agnostisch, solange Katalog-URL und
Mapping-Ziel durch dieselbe Normalisierung laufen.

**Kein WordPress mehr (Entscheid 7.3):** Die Einstellung `WordPressPrefix` und ihr
Admin-Feld `txt_WPPrefix` werden **entfernt**; die Klasse `WordPressHelpCatalog` wird in
`WikiHelpCatalog` **umbenannt** (reine Umbenennung, Verhalten identisch — betrifft
`Program.cs`, `HilfeAutomatik.cs` und die `KiAktionenDialog`-Referenz). Der
Settings-Schlüssel `WordPressUrl` bleibt als **interner** Speicherschlüssel bestehen
(eine Umbenennung würde gespeicherte Anwenderwerte in `user.config` verwerfen); im
Admin-Dialog ist das Feld neutral als Dokumentations-URL beschriftet.

### A2 — Basis-URL wieder konfigurierbar

`Program.cs:148` zurück auf den auskommentierten Weg:
`new WikiHelpCatalog(Properties.Settings.Default.WordPressUrl)` — damit steuert
**ein** Einstellwert (Default `https://wiki.epos-plan.de`) Katalog **und** Menüpunkt
Dokumentation, und das vorhandene Admin-Feld `txt_OnlineDokuUrl` wirkt wieder auf beides.

### A3 — Zuordnung vollständig neu: kein Alt-Mapping, keine Konvertierung

Die heutigen Inhalte von `help_mapping.txt` (WordPress-Slugs und -Pfade) und der
eingebettete Startbestand `help_cache.json` (116 `epos-plan.de`-Einträge) werden
**ersatzlos verworfen** — es wird nichts umgezogen, nichts übersetzt:

1. **`help_mapping.txt` neu schreiben:** je Info-Button eine Zeile, Ziel ist der
   **Kurzname der neuen Rubrik-Unterseite** (z. B.
   `Form_Klimadaten.btn_Help… = Klimadaten`). Das Zeilenformat und der Parser bleiben;
   die **EN-Hälfte entfällt** — ein Ziel je Zeile genügt (der Parser behandelt
   Einzelziele schon heute sprachneutral), Englisch entsteht beim Öffnen über den
   Übersetzungs-Proxy (A6). Anker sind für die Buttons **nicht mehr nötig** — jeder
   Dialog hat seine eigene Seite. (Der Anker-Durchlass wird trotzdem als kleine
   Erweiterung mitgebaut — Trennung vor der Auflösung, Anhängen beim Öffnen — damit
   später feldgenaue Hilfe innerhalb einer Seite möglich ist; `Vorlage:Anker` existiert.)
2. **Startbestand neu generieren:** `Allgemein\Hilfe\help_cache.json` wird einmalig aus
   der `allpages`-Antwort der Rubrik erzeugt (23 Einträge, Wiki-URLs) —
   `EmbeddedResource` mit festem `LogicalName` beibehalten
   (`WindowsFormsApplication1.csproj:164–194`). Alte Sicherungen in `%APPDATA%`
   überschreibt der erste erfolgreiche Onlinelauf von selbst.

**Seiteninventar** — 26 Buttons → **23 neue Unterseiten** `Programm Dokumentation/<Kurzname>`
(Ziel ist per Entscheid 7.2 immer eine Rubrik-Unterseite; Feinschliff einzelner
Kurznamen bleibt der Redaktion überlassen — maßgeblich ist die Vertragsliste, das
Mapping wird synchron gehalten):

| Programmstelle (Button) | Neue Unterseite |
|---|---|
| Startmaske „Programmablauf“ | `Programm Dokumentation/Programmablauf` |
| Startmaske „Kurzanleitung“ + Wizard | `…/Kurzanleitung` |
| Startmaske „Wärmebedarf“ + Form_Waermebedarf | `…/Wärmebedarf` |
| Startmaske „Strombedarf“ | `…/Strombedarf` |
| Form_Klimadaten | `…/Klimadaten` |
| Form_Kosten | `…/Kosten` |
| Form_WP (Wärmepumpe) | `…/Wärmepumpe` |
| Form_Heizkessel | `…/Heizkessel` |
| Form_BHKWEing | `…/BHKW` |
| Form_SolarKollektoren | `…/Solarthermie` |
| Form_PV | `…/Photovoltaik` |
| Form_PufferSp | `…/Pufferspeicher` |
| Form_Stromspeicher | `…/Stromspeicher` |
| Form_Gebaeude | `…/Gebäude` |
| Form_Brauchwasser | `…/Brauchwasser` |
| Form_Prozesswaerme | `…/Prozesswärme` |
| Form_Stromverbraucher | `…/Stromverbraucher` |
| Form_Simulation_Config | `…/Simulation` (Hydraulikschema als Abschnitt) |
| UcWirtschaftlichkeit | `…/Wirtschaftlichkeit` |
| Form_Variantentest | `…/Varianten` |
| Form_ProjektSpeichernUnter + Form_ImportKonflikte | `…/Projektverwaltung` |
| Form_AdminSettings | `…/Einstellungen` |
| Form_KiChat | `…/Hilfe-Assistent` |

Jede Unterseite ist eine **echte, neu geschriebene Dialoghilfe** (Gerüst siehe Teil C)
und verlinkt auf die bestehende Doku (Ablaufseite, `Grundlagen/…`) — die bestehenden
Seiten werden weder verschoben noch dupliziert.

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

Lizenz-, AGB-, Impressums- und Portal-URLs auf `epos-plan.de` bleiben unverändert
(eigene Infrastruktur, kein Teil des Hilfesystems).

### A5 — Option: sprechende Kurzbeschreibungen

Die REST-Suche liefert je Seite ein `description`-Feld; alternativ liefert TextExtracts
mit `exintro` Einleitungssätze (Batch bis 20 Titel je Aufruf — bei 23 Rubrikseiten sind
das zwei Aufrufe beim Katalogladen). Damit könnte das Popup statt nur „Kapitel: {titel}“
den ersten Satz der Hilfeseite zeigen. Per Entscheid 7.6 wird das umgesetzt — in H6,
sobald die Seiten echte Einleitungssätze tragen.

### A6 — Englisch über Online-Übersetzung (Entscheid 7.1: Variante a)

Es werden **keine englischen Wiki-Seiten** gepflegt. Englisch entsteht maschinell beim
Anzeigen. **Gewählt ist (a), der Übersetzungs-Proxy in der App**; (b) bleibt als
natürliches Browserverhalten ohnehin bestehen, (c) ist optionaler Ausbau in H6:

- **(a) Übersetzungs-Proxy in der App** *(Empfehlung)*: Bei `Program.nLanguage != 0`
  leitet die App die Ziel-URL beim Öffnen durch den Google-Übersetzungs-Proxy. Schema:
  Punkte des Hosts → `-`, vorhandene Bindestriche → `--`, Anhang `.translate.goog`,
  Query `_x_tr_sl=de&_x_tr_tl=en&_x_tr_hl=en`. **Empirisch geprüft 29.08.2026:**
  `https://wiki-epos--plan-de.translate.goog/wiki/Pufferspeicher?_x_tr_sl=de&_x_tr_tl=en&_x_tr_hl=en`
  liefert die vollständig übersetzte Seite. Ein URL-Wrapper an einer Stelle
  (Öffnungsweg des `HelpExtender` + Quellen-Links im Chat + optional Menüpunkt);
  deterministisch, kein Zutun des Anwenders. Rückfall bei Nichterreichbarkeit: die
  deutsche Original-URL.
- **(b) Browser-Übersetzung**: EN öffnet die deutsche Seite; Edge/Chrome (und Firefox
  lokal) bieten die Übersetzung von selbst an. Null Aufwand, aber ein Klick beim
  Anwender und nicht in jeder Umgebung gleich.
- **(c) Übersetzungslink im Wiki**: kleines Skin-/Vorlagen-Element „Read in English“
  auf jeder Seite, das den Proxy-Link erzeugt. Zentral im Wiki gepflegt, hilft auch
  Besuchern außerhalb der App.

Der **Assistent** ist davon unabhängig: Er erhält eine kleine Promptregel „Antworte in
der Sprache der Benutzeroberfläche“ — die deutschen Auszüge übersetzt das Modell beim
Antworten von selbst.

---

## 5. Teil B — Hilfe-Assistent nutzt die Wiki-Dokumentation

### B1 — Neuer Baustein `WikiWissen`

Neue Klasse `Allgemein\KI\WikiWissen.cs` (bewusst im Anwendungsprojekt, nicht in
`KiKern` — sie braucht HttpClient und `%APPDATA%`). Ablauf je Frage:

1. **Suchbegriffe lokal ableiten** — dieselbe Stichwortlogik wie `HilfeWissen.Suchen`
   (Wörter ≥ 4 Zeichen): an das Wiki geht eine kurze Stichwortliste, **nicht die
   Rohfrage** (Begründung in B5).
2. **Suche:** `rest.php/v1/search/page?q=…&limit=5` — liefert Titel, Snippet,
   Abschnitts-`anchor` und `description` in einem Aufruf (verifiziert). Suchraum ist
   das **ganze Wiki** (auch `Grundlagen/…` und die Ablaufseiten sind wertvoll);
   Treffer aus der Rubrik werden bevorzugt gereiht.
3. **Auszüge:** für die besten 2–3 Titel
   `api.php?action=query&prop=extracts&titles=A|B|C&explaintext=1&format=json`;
   je Seite auf ~6.000 Zeichen kappen (Prompt-Zusatz gesamt ≤ ~18 kB — für die
   Flash-Lite-Modelle unkritisch, `maxOutputTokens` bleibt 400/600).
4. **Kontextseite bevorzugen:** Aus dem `HilfeKontext`-Bereich wird die zugehörige
   Rubrik-Unterseite bestimmt — durch die Namensgleichheit von Bereichen und Kurznamen
   fast 1:1 (kleine Tabelle neben `BEREICH_JE_TYP`) — und ihr Auszug immer als erster
   Abschnitt geführt.
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
bleibt unverändert bestehen (liefert nach Teil A die Kurznamen der Rubrikseiten).

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
vorhanden; bei englischer Oberfläche gemäß 7.1 durch den Übersetzungs-Proxy geleitet).
Anzeigetexte über `MyResource.Resource.*` (beide Sprachen). Die zweite, unabhängige
Anzeige-Suche in `Form_KiChat.cs:894` nutzt dieselben Treffer, damit Anzeige und Prompt
übereinstimmen.

### B4 — Online-Doku-Suche ohne KI (Entscheid 7.4)

Ist der KI-Dienst abgeschaltet oder ohne Schlüssel (`KiEinwilligung` / Hilfe-Betrieb),
wird das Chatfenster zur **Online-Doku-Suche**: dieselbe `WikiWissen`-Kette — Stichwörter
→ Wiki-Suche → Auszüge → Quellen-Links — nur **ohne** Gemini-Aufruf; angezeigt werden
Trefferliste und Auszüge statt einer generierten Antwort. Der Abschalter bedeutet damit
präzise „**keine Google-Aufrufe**“; Zugriffe auf das eigene Wiki bleiben erlaubt — wie
bei den Info-Buttons, die das Wiki ebenfalls öffnen. Offline fällt die Suche auf Cache
und Einbauwissen zurück (heutiges Verhalten). Der Hinweistext im Fenster erwähnt den
Wiki-Zugriff (Wortlaut: der per 7.5 beschlossene Zusatzsatz, B5).

### B5 — Datenschutz und Einwilligung

Die Zusage der Einwilligung (Fassung 2) — „Übertragen werden ausschließlich Hilfetexte,
die Frage des Benutzers und eine grobe Kontextangabe“ — bleibt gegenüber Google
**unverändert wahr**: Wiki-Auszüge *sind* Hilfetexte. Neu ist ein zweiter Datenfluss:
Stichwörter der Frage gehen an `wiki.epos-plan.de` (eigener Server, TLS, ohne Anmeldung)
— nach Entscheid 7.4 auch im Betrieb ohne KI. Durch die lokale Stichwortextraktion (B1)
wird keine Rohfrage übertragen. **Entscheid 7.5: nur Textergänzung** —
`KiEinwilligung.FASSUNG` bleibt 2, der Hinweistext erhält den Satz „Zur Suche in der
Online-Dokumentation werden Stichwörter Ihrer Frage an wiki.epos-plan.de übertragen“.
Konsequenz, ehrlich benannt: Bestandsnutzer, die Fassung 2 bereits bestätigt haben,
sehen den Dialog nicht erneut; sie erreichen den Satz über den sichtbaren Hinweis im
Chatfenster (B4), und in jeder künftigen Fassung ist er enthalten.

### B6 — Option: Werkzeug `doku_suchen`

Im Aktionsbetrieb (Weg A) könnte eine 20. Aktion dem Modell erlauben, selbst
nachzuschlagen (Registrierung über `KiAktionen.Erzeuge`, Rundendeckel 3 lässt einen
Nachschlag zu; die vorbereitete Stelle `KiAktionenDialog.Hilfetext()` zeigt das Muster).
Bewusst Option: Die Vorab-Beschaffung (B1) wirkt in **beiden** Wegen und im Hilfefall,
das Werkzeug nur im Aktionsbetrieb. Erst nach B1 bewerten.

---

## 6. Teil C — Die neuen Hilfeseiten der Rubrik

Kern der 2. Fassung: **alle Zielseiten entstehen neu im Wiki.** Kein Anwendungscode —
Pflege über die bekannten Browser-Rezepte.

1. **Namensschema festlegen:** Unterseiten `Programm Dokumentation/<Kurzname>` gemäß
   Inventar in A3 — dasselbe Schrägstrich-Muster wie das etablierte
   `Grundlagen/<Thema>`. Der Kurzname ist zugleich der Mapping-Schlüssel der App;
   Umlaute sind erlaubt (URL-Kodierung übernimmt die App).
2. **Seitengerüst je Hilfeseite** (einheitliche Struktur):
   - ein Satz: Zweck des Dialogs;
   - Abschnitt **Eingaben** — die Felder/Entscheidungen des Dialogs, so knapp wie
     möglich (das ist die eigentliche neue Dialoghilfe);
   - Abschnitt **Siehe auch** — Links auf die bestehende Ablaufseite und die
     `Grundlagen/…`-Vertiefung;
   - `{{Anker|…}}` nach Bedarf für spätere feldgenaue Verweise (ASCII-klein, ohne
     Umlaute — sie werden Teil von URLs und Mapping-Zeilen).
   Startinhalt ist per Entscheid 7.2 ein **echtes Kurzgerüst, keine Weiterleitung** —
   eine Weiterleitung würde den Klick aus der Rubrik hinausführen. Die Inhalte wachsen
   „nach und nach“ — genau der Wortlaut, den die Rubrikseite dafür schon vorsieht; das
   Gerüst muss aber **vor** der Auslieferung von H2 stehen, sonst löst der Katalog
   nichts auf und die Buttons erscheinen grau.
3. **Anlage der 23 Seiten** am effizientesten per **XML-Import** über
   `Spezial:Importieren` (bewährtes Rezept: Interwiki-Präfix setzen, „Benutzer
   zuordnen“ aktivieren, Zeitstempel jüngste Vergangenheit); alternativ einzeln über
   das Bearbeitungsformular.
4. **Rubrikseite:** Der Platzhalter im Abschnitt „Hilfeseiten“ wird durch die
   **Vertragstabelle** ersetzt: *Programmstelle (Formular/Button) → Unterseite*. Sie ist
   das Gegenstück zur eingebetteten `help_mapping.txt` — wer eine Seite umbenennen will,
   sieht dort, was daran hängt.
5. **Bereichs-Zuordnung für B1.4** dokumentieren (28 `HilfeKontext`-Bereiche → Unterseite;
   durch Namensgleichheit fast 1:1).
6. Der Menüpunkt Dokumentation zeigt weiterhin auf die **Hauptseite** (Einstieg mit
   Navigation); die Rubrikseite bleibt Pflege-/Vertragsseite und wird nicht als
   Anwenderziel verlinkt.

---

## 7. Entscheidungen (alle getroffen, 29.08.2026)

- **7.1 — Englisch per Online-Übersetzung, Variante (a).** Keine englischen
  Wiki-Seiten; die App leitet Ziel-URLs bei englischer Oberfläche durch den
  Übersetzungs-Proxy (Mechanik und Nachweis in A6). (b) Browser-Übersetzung bleibt
  natürlicher Rückfall, (c) Übersetzungslink im Wiki ist optionaler Ausbau in H6.
- **7.2 — Der Info-Button öffnet immer eine Seite der Rubrik „Programm
  Dokumentation“.** Nie eine Bestandsseite der übrigen Doku; je Dialog die eigene
  Unterseite gemäß Inventar A3. Daraus folgt auch der Startinhalt: **echtes
  Kurzgerüst, keine Weiterleitungen** — eine Weiterleitung würde den Klick aus der
  Rubrik hinausführen. Feinschliff einzelner Kurznamen bleibt der Redaktion
  überlassen; maßgeblich ist die Vertragsliste, das Mapping wird synchron gehalten.
  *(Die frühere Frage „Dialogseite oder Grundlagenseite als Button-Ziel“ aus der
  1. Fassung ist damit endgültig gegenstandslos.)*
- **7.3 — Kein WordPress mehr im Hilfesystem.** `WordPressPrefix` samt Admin-Feld
  entfernen, Klasse `WordPressHelpCatalog` → `WikiHelpCatalog` umbenennen (A1). Der
  interne Settings-Schlüssel `WordPressUrl` bleibt als Speicherort (A1). **Bestätigte
  Ausnahme:** die Aufrufe der Lizenz-Infrastruktur (AGB, Lizenzserver, Portal auf
  `epos-plan.de` — `Form_Lizenz`, `LizenzServerClient`) sind kein Hilfesystem und
  bleiben unberührt.
- **7.4 — Doku-Suche online auch ohne KI-Assistent.** Das Chatfenster wird im Betrieb
  ohne KI zur Online-Doku-Suche gegen das Wiki (B4); „abgeschaltet“ heißt fortan
  präzise „keine Google-Aufrufe“.
- **7.5 — Einwilligung: nur Textergänzung.** `KiEinwilligung.FASSUNG` bleibt 2; der
  Hinweistext erhält den Wiki-Satz (B5). Bestandsnutzer sehen den Dialog nicht erneut
  — sie erreichen den Satz über den sichtbaren Hinweis im Chatfenster.
- **7.6 — Popup-Kurzbeschreibungen werden umgesetzt.** Gemäß Empfehlung in H6, sobald
  die Hilfeseiten echte Einleitungssätze tragen (A5).

Damit ist keine Randfrage mehr offen — **das Konzept ist freigabereif**. Die Freigabe
ist das Startsignal für die Umsetzung in der Reihenfolge H3 → H1+H2 → H4 → H5
(H6 danach), siehe Abschnitt 8.

---

## 8. Vorgehensweise

Sechs Pakete; H1–H3 stellen die Info-Verweise um (Teil A + C), H4–H5 den Assistenten
(Teil B), H6 ist Ausbau. **Reihenfolge-Zwang:** die Wiki-Seiten (H3) müssen vor der
Auslieferung von H1+H2 stehen — ein Katalog ohne existierende Seiten lässt alle Buttons
grau. H3 → H1+H2 bilden zusammen den ersten auslieferbaren Schnitt.

### H1 — Katalog auf MediaWiki (A1, A2, A4, A6)

Lader auf `list=allpages` mit `apprefix`, Basis-URL aus den Settings, Umbenennung
`WikiHelpCatalog`, `WordPressPrefix` entfernen, EN-Übersetzungs-Wrapper (7.1a),
`DOKU_URL`/LinkLabel/Basiswissen nachziehen, Popup-Text lokalisieren. Beweis:
Debug-Protokoll zeigt „online geladen, 23 Einträge“; Sicherung in `%APPDATA%` enthält
Rubrik-URLs; EN-Stichprobe öffnet die übersetzte Fassung.

### H2 — Zuordnung neu (A3)

`help_mapping.txt` komplett neu (26 Zeilen, ein Ziel je Zeile — Kurznamen gemäß 7.2),
Anker-Durchlass im Extender, `help_cache.json` aus der Rubrik-Liste neu generiert —
kein Alt-Eintrag überlebt. Beweis: alle 26 Buttons öffnen ihre Unterseite (Prüfliste 9).

### H3 — Die 23 Hilfeseiten anlegen (Teil C)

Seitengerüste per XML-Import, Vertragstabelle auf der Rubrikseite, Bereichs-Zuordnung
dokumentiert. Browser-Arbeit nach den bestehenden Rezepten. **Muss vor H2-Auslieferung
abgeschlossen sein.**

### H4 — `WikiWissen` (B1–B4)

Suche + Auszüge + Cache, Einspeisung an den drei Stellen, Quellen-Links im Chat,
**Online-Doku-Suche im Betrieb ohne KI** (Entscheid 7.4). Beweis: Sendevorschau zeigt
Wiki-Abschnitte; Antwort zu „Pufferspeicher“ zitiert die Wiki-Seite; ohne KI-Schlüssel
liefert dieselbe Frage Trefferliste + Auszüge; Netzstecker-Test fällt sauber auf
Einbauwissen zurück.

### H5 — Riegel und Einwilligung (B5)

Hinweistext ergänzen (7.5: Textergänzung, kein Fassungswechsel), Prüfliste 9
vollständig abfahren.

### H6 — Ausbau (Optionen)

Popup-Kurzbeschreibungen (7.6, beschlossen — nach Befüllung der Seiten),
`doku_suchen`-Aktion (B6), Übersetzungslink im Wiki (7.1c), feldgenaue Anker-Hilfe
innerhalb der Seiten.

---

## 9. Prüfliste für die Abnahme

| # | Prüfung | Erwartung |
|---|---|---|
| 1 | Jeden der 26 Info-Buttons klicken (DE) | die zugehörige Rubrik-Unterseite öffnet |
| 2 | Sprache auf EN, Stichprobe der Buttons | übersetzte Fassung über den Übersetzungs-Proxy öffnet (7.1a), kein grauer Button |
| 3 | Erststart ohne Netz (Startbestand) | Buttons aktiv, Ziele sind Rubrik-URLs (aus erneuertem `help_cache.json`) |
| 4 | Start mit Netz, danach offline | Sicherung aus `%APPDATA%` greift, Stand identisch online/offline |
| 5 | Menüpunkt Dokumentation | Wiki-Hauptseite; geänderte URL im Admin-Dialog wirkt auf Menü **und** Katalog |
| 6 | Rubrikseite | Vertragstabelle vollständig: 23 Unterseiten, alle Buttons zugeordnet |
| 7 | Assistent: Frage zu einem Wiki-Thema | Antwort stützt sich auf Doku-Auszug, Quell-Link unter der Antwort stimmt |
| 8 | Assistent: Sendevorschau | zeigt die Wiki-Abschnitte, identisch mit dem tatsächlich Gesendeten |
| 9 | Assistent offline | Rückfall auf Einbauwissen, keine Fehlermeldung, Antwortverhalten wie bisher |
| 10 | Betrieb ohne KI (Doku-Suche) | Wiki-Treffer + Auszüge + Links erscheinen; **keinerlei** Google-Aufruf (Riegel greift vor allem) |
| 11 | Tageslimit/Einwilligung | unverändert wirksam; kein Fassungswechsel (7.5) — der ergänzte Wiki-Satz steht im Hinweistext |
| 12 | F1 | öffnet weiterhin den Assistenten (nicht die Wiki-Seite) |
| 13 | `KiSchreibschutz`-Stichprobe | Katalog-/`_STAMM`-Schreibversuch des Assistenten weiterhin abgelehnt |
| 14 | Unterseite testweise umbenannt (mit Weiterleitung) | Button folgt der Weiterleitung — Regel der Rubrikseite trägt |
| 15 | `WordPressPrefix` | Feld nicht mehr im Admin-Dialog, kein Codepfad liest die Einstellung mehr |

Referenzläufe sind nicht betroffen (keine Engine-Änderung); ein Regressionslauf nach H4
ist trotzdem billig und empfohlen, weil `Form_KiChat` im MDI-Umfeld hängt.

---

## 10. Aufwand

| Paket | Umfang | Aufwand |
|---|---|---|
| H1 Katalog | 1 Lader-Methode, Umbenennung, Prefix-Abbau, EN-Wrapper, 3 Streustellen, 2 `.resx` | 5–7 h |
| H2 Zuordnung | 26 Zeilen neu, Anker-Durchlass, Startbestand generieren | 3–4 h |
| H3 Hilfeseiten | 23 Seitengerüste (XML-Import), Vertragstabelle, Bereichstabelle | 6–10 h |
| H4 WikiWissen | 1 neue Klasse, 3 Einspeisestellen, Chat-Links, Cache, Doku-Suche ohne KI | 9–13 h |
| H5 Riegel/Abnahme | Hinweistext, 15 Prüfungen × 2 Sprachen | 4–6 h |
| H6 Ausbau | je Option | separat |

**Kernumfang H1–H5: rund 27–40 h.** Erster auslieferbarer Schnitt H3 → H1+H2
(~14–21 h) behebt den heutigen 404-Zustand und erfüllt die Info-Button-Anforderung;
die redaktionelle Vertiefung der Seiteninhalte läuft danach fortlaufend und ist in der
Schätzung nur als Gerüst enthalten.

---

## 11. Fallstricke bei der Umsetzung

- **Seiten zuerst, Code danach ausliefern.** Ein H2-Stand ohne die H3-Seiten lässt
  alle Buttons grau (Katalog löst nichts auf) — der Reihenfolge-Zwang aus Abschnitt 8
  ist real, nicht kosmetisch.
- **Kodierung.** Teile des Bestands sind nicht UTF-8 (93 von 372 `.cs`-Dateien);
  `KiChatService.cs` und `HilfeWissen.cs` enthalten deutsche Literale. Vor dem
  Bearbeiten Kodierung prüfen und beibehalten (`CP1252`-Rezept), sonst zerschießt der
  Diff die Datei.
- **Altkopien entsorgt (29.08.2026):** `..\WindowsFormsApplication1 - Kopie` und
  `..\mit_Puffer_KI_Lösungsversuch` wurden auf Anweisung in den Papierkorb verschoben —
  die frühere Verwechslungsgefahr beim Suchen/Greppen besteht nicht mehr.
- **Drei-Schichten-Regel.** Die Kurznamen der Unterseiten sind Fremdschlüssel
  (Persistenz-artig): sie stehen in `help_mapping.txt`/`help_cache.json`, nie als
  Literal im Code und nie als Anzeigetext-Quelle. Anzeige (Popup-Link, Quellen-Zeile im
  Chat) ausschließlich über `MyResource.Resource.*`, beide Satelliten-`.resx` pflegen.
- **`EmbeddedResource`-Eigenheit.** `help_mapping.txt`/`help_cache.json` haben feste
  `LogicalName`-Einträge und werden bewusst nicht in den Ausgabeordner kopiert — beim
  Erneuern die `.csproj`-Einträge unangetastet lassen.
- **Titel-Kodierung.** Unterseiten-Titel mit Umlauten/Leerzeichen beim URL-Bau
  URL-kodieren (`Programm Dokumentation/Wärmebedarf` →
  `Programm_Dokumentation/W%C3%A4rmebedarf`); der Normalisierungs-Abgleich läuft
  dagegen über die dekodierte Kleinschreibform — beide Wege strikt trennen (Öffnen:
  Original; Abgleich: normalisiert). Auch `apprefix` im Lader-Aufruf URL-kodieren.
- **Übersetzungs-Proxy ist ein Fremddienst.** Das `translate.goog`-URL-Schema ist
  inoffiziell, aber seit Jahren stabil; bei Nichterreichbarkeit oder Schema-Änderung
  muss der Wrapper auf die deutsche Original-URL zurückfallen (nie ein toter Link).
- **XML-Import-Fallen** (aus den bestehenden Rezepten): Interwiki-Präfix ist
  Pflichtfeld, „Benutzer zuordnen“ aktivieren (sonst sind die Revisionen in den
  Letzten Änderungen unsichtbar), Zeitstempel in jüngster Vergangenheit.
- **Einwilligung nicht stillschweigend erweitern.** Der per 7.5 beschlossene
  Zusatzsatz wird zusammen mit H4 ausgeliefert (`FASSUNG` bleibt 2); nach 7.4 betrifft
  der Wiki-Datenfluss auch den Betrieb ohne KI.
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
  (Rubrik- und Vertragsseite mit Pflegeregeln),
  [`Einstellungen, Hilfe und Sprache`](https://wiki.epos-plan.de/wiki/Einstellungen,_Hilfe_und_Sprache)
  (dreistufiges Hilfesystem), [`Hilfe-Assistent`](https://wiki.epos-plan.de/wiki/Hilfe-Assistent)
- `WindowsFormsApplication1\Allgemein\Hilfe\HelpCatalog.cs`, `help_mapping.txt`,
  `help_cache.json` — der heutige Katalogweg
- `WindowsFormsApplication1\Allgemein\KI\KiChatService.cs`, `HilfeWissen.cs`,
  `HilfeKontext.cs` — der heutige Prompt-Aufbau

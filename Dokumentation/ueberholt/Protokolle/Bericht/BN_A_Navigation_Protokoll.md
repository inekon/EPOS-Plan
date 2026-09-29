# BN-A — Navigation „Berichte & Kosten“ nach Variante A (Protokoll)

Umsetzung des Konzepts
[`Konzept_BerichteKosten_Navigation_EPOS-Plan.md`](../../Konzept_BerichteKosten_Navigation_EPOS-Plan.md), das mit
diesem Auftrag nach `ueberholt/` wandert. Statusnummer #NNN. Anwenderentscheide vom 27.09.2026: **BN-Q1** Variante A
(Reiterzeile), **BN-Q2** Statuszeile je Reiter mit „zuletzt erstellt“ (A3), **BN-Q3** Stammname,
Platzhalter-Umschalter und Hilfe rechts in die Reiterzeile, die eigene Kopfzeile entfällt. Am 29.09.2026 hat der
Anwender bestätigt: Variante A **vollständig mit A3** — ein Vermerk „A3 entfällt“ aus einer Nachbarsitzung ist damit
überholt. Umgesetzt sind alle Etappen A1 bis A4. Der gültige Stand steht im Code, in den Wiki-Quellen und in der
[Statusdatei](../../../aktuell/Status_iOS_Migration.md); hier steht, wie es geworden ist. Die Mockups bleiben unter
[`aktuell/Mockups/`](../../../aktuell/Mockups/BerichteKosten_Navigation_A.html).

Worktree-Zweig `worktree-agent-a9757889a3bf4b9b4`, 27. bis 29.09.2026, Opus 5.5. Kein Schemaschritt (der nächste, 152, ist
von einer Nachbarsitzung belegt), kein Rechenweg, keine Referenzbasis berührt; `EPOS.iOS/` nicht berührt.

| Commit | Inhalt |
|---|---|
| `b18c827` | Kern: `BerichtsKonfiguration.ZuletztErstellt`, `BerichtCtrl.MerkeErstellt`/`ZuletztErstellt`, `Speichere` behält den Zeitpunkt; 15 Schlüssel `BK_STATUS_*` |
| `69fb7db` | A1 und A2 samt Oberflächenseite von A3: `Reiter`/`Reiterblatt`, `BerichteKostenSeite`, Stilblatt, Hülle, Gaben der vier Seiten, Tests |
| `ce25f7b` | Befund der Sichtprobe: Leistenende und Stammname passen bei 1 280 px in die Zeile der Reiter; `BK_LBL_STAMM_KURZ`. Enthält versehentlich auch die reine Umbenennung des Konzepts nach `ueberholt/` (die Verschiebung lag schon im Index); dessen Verweise folgen erst im letzten Commit — dazwischen zeigen drei Verweise ins Leere |
| `e3f7f594` | Wiki-Quellen „Wirtschaftlichkeit“, „Berichtsvorlagen“, „Kosten“, „Varianten“ |
| `b4fb716f` | Sichtprobe `/berichtekosten` samt `berichtekostenprobe.mjs` und LIESMICH der Rasterprobe |
| (dieser Commit) | Konzept nach `ueberholt/` mit Verweisen und Indexzeile, dieses Protokoll |


## 1 Befund

`EPOS.UI/Seiten/Berichte/BerichteKostenSeite.razor` führte eine eigene senkrechte Navigation (`.epos-navigation*`,
208 px, `#23282d` und fünf weitere Festfarben ohne Token, Textsinnbilder ☰ € ▤ ▦, ↑ ↓ als `tablist`), darüber eine
Kopfzeile mit Rückweg, „Titel · Stammname“, Umschalter und Hilfe. Der Hausbaustein `Reiter` war für genau diese Seite
entstanden und wurde von ihr nicht benutzt. Die Knopfkennungen `bknav-*` waren eigene; der Baustein `Reiter` vergab
`reiter-SCHLUESSEL`, was in der Startseite (sechstes Blatt) mit dem Reiter der Simulationsergebnisse (`reiter-UEBERSICHT`)
zusammengestoßen wäre.


## 2 Umsetzung je Etappe

### A1 — Reiterzeile auf dem Hausbaustein

- `BerichteKostenSeite` trägt `<Reiter Kennung="bk" @bind-Aktiv="_seite" @bind-Aktiv:after="SeiteGewechselt">` mit vier
  `Reiterblatt`-Kindern. Die Seitenschlüssel (`UEBERSICHT`, `KOSTEN`, `WIRTSCHAFT`, `BERICHT`) bleiben; `Zeige`,
  `Seitenwunsch` der Hülle (Menüsprung „Varianten und Bericht…“) und „Zum Bericht ›“ mit `BerichtVorbelegung` setzen
  den aktiven Reiter. Tastatur wie jeder Reiter: ← → Pos1 Ende.
- **Verzögerter Aufbau bleibt:** Ein nicht gewähltes `Reiterblatt` zeichnet nichts; jedes Blatt trägt dasselbe
  Inhaltsfragment, gezeichnet wird es nur im sichtbaren, und nur die gewählte Seite bekommt Gaben.
- `Reiter.Kennung` (optional, leer = Kennungen wie immer): Vorsatz der Kennungen von Knopf und Blatt
  (`bk-reiter-KOSTEN`, `bk-blatt-KOSTEN`); `Reiterblatt` holt seine Kennungen beim Reiter.
- Die Stilfamilie `.epos-navigation*` ist samt der Umschalter-Regel der alten Kopfzeile entfernt; die Stilblattwache
  prüft, dass weder `.epos-navigation` noch `#23282d` mehr im Hausblatt stehen.

### A2 — Statuszeile und Leistenende

- `Reiterblatt.Status`, `StatusKurz`, `Statusstufe` (neuer Typ `Statusstufe` Normal/Warnung und `Reiterstatus` in
  `EPOS.UI/Bausteine/Reiterstatus.cs`), alle optional. Mit Status trägt der Knopf Titel und darunter eine leise Zeile
  (`.epos-reiter-knopf--status`, 52 px); eine Warnung steht in `--epos-warn-text` mit ▲ (`aria-hidden`), im
  Kontrastmodus in `LinkText`. Unter 900 px steht die Kurzform (`.epos-reiter-status-kurz`); ohne eigene Kurzform
  steht EIN Text. Ohne Status bleibt das Markup des Knopfs genau das alte — Simulationsseite, Startseite und die
  Dialoge mit Reiter sind unverändert (Wachen: `ReiterTests`, volle `EPOS.UI.Tests`).
- `Reiter.Leistenende` (RenderFragment, optional): steht neben der `tablist`, nicht in ihr
  (`.epos-reiter-kopfzeile`). Es nimmt den Rest der Zeile mit einem Grundmaß von 26rem; reicht der Platz nicht, bricht
  das ganze Ende in eine eigene Zeile, unter 900 px immer. `BerichteKostenSeite` setzt hinein: Stammname
  („Stamm: …“, `BK_LBL_STAMM_KURZ`), `Vorlagenfeldumschalter`, `InfoKnopf` (nur wo kein Kopfband die Pille trägt) und
  als eigene Ansicht den Rückweg — rechts außen wie „← zurück“ der Simulationsansicht. Die frühere Kopfzeile entfällt.
- **Kurzstände der Hülle** (`BerichteKostenHuelle.Kurzstand`, Parameter `Status` als `Func<string, Reiterstatus>`),
  ohne Rechnung beim Öffnen:

| Reiter | Text (lang · kurz) | Quelle | ab wann |
|---|---|---|---|
| Übersicht | „3 Versionen · simuliert“ · „3 Versionen“; Warnung „3 Versionen · 1 nicht aktuell“ · „▲ 1“ | Ereignis `Geladen` von `UebersichtSeiteGaben` (zählt, was ihr Laden ohnehin liest) | sobald die Übersicht geladen hat (Startseite des Bereichs) |
| Kosten | „3 Träger“; Warnung „3 Träger · 2 Warnungen“ · „▲ 2“ | Ereignis `Geladen` von `KostenSeiteGaben` (Träger der Tabelle, Befunde der Fußzeile einzeln gezählt) | sobald die Kostenseite in der Sitzung geladen hat |
| Wirtschaftlichkeit | „beste: mit PV, +61.500 €“ · „+61.500 €“; Warnung „… · veraltet“ · „▲ veraltet“; „nicht berechnet“ | leichte Lesung der GESPEICHERTEN Ergebnisse (`LadeErgebnisse`, `BesteVariante.Waehle`, `ErgebnisAktuell`) je Stammprojekt, zwischengespeichert, neu nach Laden/Berechnen der Seite | sofort |
| Bericht | „zuletzt 26.09.26 18:12“ · „26.09. 18:12“; „noch keiner erstellt“ · „—“ | `BerichtCtrl.ZuletztErstellt` je Stammprojekt, zwischengespeichert, neu nach einem Lauf | sofort |

- Ein geänderter Kurzstand läuft über das neue Ereignis `SeitenZustand.KurzstandGeaendert`: Die Seite zeichnet nur
  neu, holt keine Gaben und baut die offene Seite nicht neu auf (bunit-Fall
  `Ein_geaenderter_Kurzstand_zeichnet_nur_die_Reiterzeile_neu`). Ein Fehler der Hülle kostet nur die Zeile.

### A3 — „zuletzt erstellt“ ohne Schemaschritt

- Abgelegt im JSON der vorhandenen Tabelle `Berichtskonfiguration` (je Stammprojekt, Schemaschritt 96 hält schon den
  Fremdschlüssel): `BerichtsKonfiguration.ZuletztErstellt`, invariant `yyyy-MM-ddTHH:mm:ss`, duldsam gelesen, ohne
  Wert nicht geschrieben (das JSON älterer Stände bleibt byte-gleich).
- `BerichtCtrl.MerkeErstellt(idStamm, zeitpunkt)` liest die gespeicherte Konfiguration streng — ist sie nicht lesbar,
  wird NICHTS geschrieben (sonst ersetzte der Standard die Auswahl) —, setzt den Zeitpunkt und speichert.
  `BerichtCtrl.Speichere` behält einen gespeicherten Zeitpunkt, solange die übergebene Konfiguration keinen neueren
  trägt: Häkchen (vor jedem Lauf) und Vorlagenwahl bauen ihre Konfiguration ohne ihn. Folge: Der spätere Zeitpunkt
  gilt; eine zurückgestellte Uhr überschreibt ihn nicht.
- `BerichtSeiteGaben.Erstellen` merkt den Zeitpunkt nur nach einem Lauf mit Datei und meldet ihn über das Ereignis
  `Erstellt`; ein Fehler beim Merken kostet nur die Zeile, nie den Bericht. Alle SQL-Texte über `DataRepository` mit
  `?`; `SqlDialektPruefer`: 2 043 SQL-Texte, 0 Fundstellen.

### A4 — Prüfen und Doku

- Wiki-Quellen: „Berichtsvorlagen“ (Umschalter rechts in der Reiterzeile; „zuletzt erstellt“ am Reiter „Bericht“),
  „Wirtschaftlichkeit“ (Anker `reiterzeile` mit den vier Statuszeilen und der schmalen Anordnung; „Zum Bericht ›“
  wechselt auf den Reiter), „Kosten“ und „Varianten“ (Reiter statt Seite). Gegengelesen mit dem Muster der
  Wurzel-`CLAUDE.md`: keine Treffer; keine Hersteller- oder Produktdaten.
- Sichtprobe im Browser (Chromium aus `/opt/pw-browsers`, Wirt der Rasterprobe): neue Seite `/berichtekosten` und
  Messung `Proben/Rasterprobe/berichtekostenprobe.mjs`, siehe Abschnitt 4.


## 3 Tests

| Lauf | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler |
| `EPOS.UI.Tests` vollständig | 6 859 bestanden, 0 fehlgeschlagen |
| `EPOS.Kern.Tests`, betroffene Klassen und Wachen (`BerichteKostenKurzstandTests`, `BerichtsvorlagenHuelleTests`, `BerichtsKonfigurationJsonTests`, `BerichtCtrlKonfigurationTests`, `BerichtCtrlVorlagenTests`, `BerichtsvorlagenHaekchenHuelleTests`, `WirtschaftlichkeitHuellenPlattformTests`, `AnhangEStellenHuelleTests`, `NachtraegeE5bTests`, Wachen Dokumentationsverweise, Repositoryordnung, Wiki-Produktdaten, Parallelität, Einheiten, KI-Maskenabdeckung) | 158 bestanden, 0 fehlgeschlagen |
| Windows-Schale `WindowsFormsApplication1.csproj` (Debug, x64, `EnableWindowsTargeting`) | 0 Fehler |
| `Werkzeuge/SqlDialektPruefer` | 2 043 geprüft, 0 Fundstellen |
| `Proben/Rasterprobe/Wirt` | 0 Fehler |

Neue Fälle: `ReiterTests` (Kennung, ohne Kennung unverändert, ohne Status nur Titel, leise Statuszeile, Warnung mit
Zeichen und Kurzform, neuer Status erreicht die Leiste, Leistenende neben der tablist, ohne Leistenende unverändert);
`BerichteKostenSeiteTests` (Reiterzeile ist der Hausbaustein, Vorsatz `bk`, Leistenende mit Stamm, Umschalter und
Hilfe, ohne Stammnamen, ← → Pos1 Ende, Rückweg rechts außen, Statuszeilen, Warnung, ohne Kurzstand, Fehler im
Kurzstand, Kurzstand ohne Neuaufbau, Entsorgen, Seitenwunsch stellt den Reiter um; die Fälle zu „Zum Bericht ›“ und
Vorbelegung laufen unverändert über die Reiterknöpfe); `StilblattTests.BN_A_…` (Token, Kontrastmodus, Kurzform und
Umbruch unter 900 px, keine `.epos-navigation` mehr); `EPOS.Kern.Tests/BerichteKostenKurzstandTests` (JSON,
duldsames Lesen, Merken und Behalten, der spätere gilt, ohne Stamm, Kurzstände der Hülle für Übersicht, Kosten,
Wirtschaftlichkeit — Gruppe 1019, beste Variante „Test1“ — und Bericht, zwei Sprachen; alle an einer Arbeitskopie der
Testdatenbank); `BerichtsvorlagenHuelleTests.Der_erfolgreiche_Lauf_merkt_seinen_Zeitpunkt_und_meldet_ihn`.
Angepasst: `AppWurzelTests` und `HauptfensterTests` (Klassen `.epos-berichtekosten`, `.epos-berichtekosten-zurueck`).


## 4 Sichtprobe

`node berichtekostenprobe.mjs` gegen den Wirt, sechs Fälle, Rückgabe 0 („kein Verstoß“):

| Fall | Ergebnis |
|---|---|
| 1 280 × 900, Reiterblatt der Startseite (Übersicht, Bericht) | zwei Reiterebenen; vier Reiter 52 px hoch mit langer Statuszeile; Leistenende in derselben Zeile (Stamm „Beispielprojekt“ ganz, Umschalter, Hilfe); Warnung in `rgb(138, 91, 0)`; kein Querrollen |
| 1 280 × 900, eigene Ansicht mit Rückweg | eine Zeile; der Stammname kürzt sich auf „Beis…“, die Beschriftung fällt weg (voller Name im `title`) |
| 1 280 × 900, langer Stammname | „Stamm: Sehr lange…“ — die Zeile bricht nicht |
| 820 × 1 180 (iPad hochkant), Blatt und Ansicht | Kurzformen „3 Versionen“, „▲ 2“, „+61.500 €“, „26.09. 18:12“; Leistenende in eigener Zeile, Stamm links, Umschalter rechts — wie Bild 2 des Mockups |
| Kontrastmodus (`forcedColors: 'active'`) | Warnung in `LinkText`, übrige Zeilen in `CanvasText`, ▲ sichtbar |

Der erste Lauf fand den Befund, der zu `ce25f7b` führte: Mit den langen Entwurfstexten („3 Energieträger · 2 Warnungen“,
„zuletzt 26.09.2026 18:12“, „Stammprojekt:“) brach das Leistenende bei 1 280 px in eine eigene Zeile.


## 5 Offen

1. **Wiki-Seite „Programm Dokumentation/Berichte und Kosten“:** Unter `Projekte/Wiki/` gibt es für sie keine
   Repo-Quelle (die Seiten „Varianten“ und „Wirtschaftlichkeit“ verweisen auf ihren Anker `vergleichswahl`). Die
   Reiterzeile ist deshalb auf der Seite „Wirtschaftlichkeit“ (Anker `reiterzeile`) beschrieben; die Quelle der Seite
   „Berichte und Kosten“ wäre vor dem nächsten Upload aus dem Wiki zu holen und nachzuziehen. Upload der vier
   geänderten Quellen steht aus (gebündelt, Regel Hilfesystem 13.3).
2. **Kosten und Übersicht** nennen ihren Stand erst, wenn ihre Seite in der Sitzung geladen hat — gewollt: Ein
   Kostenstand ohne Laden der Seite hieße, beim Öffnen die ganze Kostenseite zu lesen.
3. **Eigene Ansicht bei 1 280 px:** Mit Rückweg bleiben dem Stammnamen rund 50 px („Beis…“, voller Name im
   `title`); das Kopfband der Startseite nennt das Projekt ohnehin.
4. **Windows-Abnahme:** geprüft in Chromium unter Linux; WebView2 unter Windows und die iPad-Hülle nicht gesehen.
5. `BK_KOPF_KOSTEN`, `BK_KOPF_WIRTSCHAFT`, `BK_KOPF_BERICHT` wurden nicht mehr gelesen (die Reiter tragen den Titel)
   und sind bei der Zusammenführung aus beiden `.resx` entfernt, Designer neu erzeugt. `BK_KOPF_UEBERSICHT` bleibt in
   Gebrauch.


## 6 Logbuch (Vorschlag)

„Berichte & Kosten zeigt Übersicht, Kosten, Wirtschaftlichkeit und Bericht als Reiter mit Stand je Reiter statt der
dunklen Seitenleiste.“

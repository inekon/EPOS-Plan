# EPOS-Plan Dokumentation – Design-Skizze für wiki.epos-plan.de

Kurzkonzept, Stand 21.08.2026 · Entwurf zur Abstimmung

## 1. Ausgangslage

wiki.epos-plan.de ist ein frisch installiertes MediaWiki 1.45.1 (noch ohne Artikel) mit dem alten Standard-Skin „Vector 2010", Platzhalter-Logo, ohne HTTPS und ohne Kurz-URLs (`/index.php?title=…`). Vorhanden sind die Skins Vector 2022, Timeless, MonoBook und Minerva sowie die Erweiterungen Cite, SyntaxHighlight, ConfirmEdit und WikiEditor.

epos-plan.de (WordPress/Avada) gibt die Gestaltung vor: kräftige rote Kopfleiste, weiße Inhaltsflächen auf hellgrauem Grund, Schrift Manrope, runde Icon-Kreise in Hellrosa mit rotem Symbol, Links und Buttons in Rot. Inhaltlich existiert dort bereits eine sehr lange Seite „Dokumentation" (Überblick, sieben Programmschritte, 15 Programmfunktionen, Industrie & Gewerbe), auf die die Software aus Tooltips und dem Hilfe-Assistenten verlinkt.

## 2. Zielbild

Das Wiki tritt als „EPOS-Plan Dokumentation" auf – eine Doku-Website im Look der Hauptseite, nicht als Wikipedia-Klon. Leser sehen keine Wiki-Werkzeuge; Bearbeiten bleibt dem Inekon-Team vorbehalten. Jede Programmfunktion und jeder Programmschritt bekommt eine eigene Seite mit stabiler Adresse, damit die Software-Hilfe direkt auf Kapitel und Abschnitte verlinken kann.

## 3. Skin-Empfehlung: Citizen

Empfohlen wird der Skin **Citizen** (StarCitizenTools, aktiv gepflegt, aktuelle Version 3.21 unterstützt MediaWiki ≥ 1.43, also auch 1.45). Er liefert das Doku-Layout ohne Umbauten: Kopfleiste mit Menü und Befehlspaletten-Suche (Tastenkürzel `/`, mit Vorschaubild und Kurzbeschreibung je Treffer), Inhalt mittig mit sticky Inhaltsverzeichnis „Auf dieser Seite", Mobilansicht, Darstellungseinstellungen für Leser (Schriftgröße, Seitenbreite, helles/dunkles Theme). Farben und Schrift werden über CSS-Variablen in `MediaWiki:Citizen.css` gesetzt (Primärfarbe als OKLCH-Tripel `--color-progressive-oklch__l/c/h`, Flächen `--color-surface-0…4`, Text `--color-base`, Links `--color-link`, Schrift `--font-family-citizen-base`). Bearbeiten- und Seitenwerkzeuge blendet Citizen für Leser aus, sobald `$wgCitizenShowPageTools = 'permission'` gesetzt ist.

Rückfalloption ohne Installation: **Vector 2022** (bereits vorhanden) mit Anpassungen in `MediaWiki:Vector-2022.css` – ebenfalls responsiv mit sticky Inhaltsverzeichnis, rote Kopfleiste und Karten-Layout müssen aber per CSS übergestülpt werden; das Ergebnis wirkt weniger wie eine Doku-Site. Timeless und MonoBook werden nicht empfohlen.

## 4. Farben (aus epos-plan.de übernommen)

| Rolle | Wert | Herkunft / Einsatz |
|---|---|---|
| Primär: Kopfleiste, aktive Navigation, Links | `#B5321F` | Header-Rot der Website; weiße Schrift darauf bzw. Rot auf Weiß: Kontrast 6,1:1 |
| Akzent: Buttons, Icons, Balken | `#C0392B` | Hero- und Icon-Rot; auf Weiß 5,4:1 |
| Link-Hover und Link besucht | `#8F2718` | dunkleres Rot (8,5:1) statt MediaWiki-Lila; Hover wird dunkler, nicht heller |
| Signalrot (sparsam) | `#DD3333` | Avada-Primärfarbe; nur Buttons/Hervorhebungen, als Textfarbe grenzwertig (4,6:1 auf Weiß, 4,0:1 auf Grau) |
| Icon-Hintergrund, Warnbox | `#FBE3E0` | helles Rosa der Icon-Kreise |
| Tipp-Grün | `#2E7D32` auf Fläche `#E6F4EA` | einzige Farbe außerhalb der Website-Palette, nur für Tipp-Boxen (5,1:1 auf Weiß) |
| Seitenhintergrund | `#EEF1F5` | Body-Hintergrund der Website |
| Inhaltsfläche | `#FFFFFF`, sekundär `#F9FAFB` | Textkarte; Tabellenköpfe, Code, Chips |
| Infofläche | `#E0ECF0` | Hinweisboxen „Info", Hervorhebungen |
| Rahmen | `#C5C8CC` kräftig / `#E3E6EA` leicht | Tabellen, Karten, Trennlinien |
| Text | `#1F2933`, sekundär `#5B6572` | Fließtext / Meta-Angaben, Bildunterschriften |

## 5. Schrift

Manrope (SIL Open Font License) als Variable Font (Gewichte 200–800 in einer Datei), selbst gehostet in einem update-sicheren Verzeichnis außerhalb des MediaWiki-Kerns – kein Abruf von Google Fonts (DSGVO). In `MediaWiki:Citizen.css` wird nur der Schriftname gesetzt (`--font-family-citizen-base: 'Manrope'`), die Fallbacks system-ui/sans-serif ergänzt Citizen selbst. Die 22 px Fließtext der Website sind für Dokumentation zu groß; hier gilt:

| Element | Größe / Gewicht | Bemerkung |
|---|---|---|
| Fließtext | 17 px / 400, Zeilenhöhe 1,6 | Zeilenlänge max. ca. 75 Zeichen (Inhaltsspalte ca. 800 px) |
| H1 Seitentitel | 34 px / 700 | serifenlos, ohne Trennlinie des Vector-Skins |
| H2 | 26 px / 700 | Abstand oben 2 em, feine Linie `#E3E6EA` darunter |
| H3 | 20 px / 600 | |
| Navigation, Inhaltsverzeichnis, Meta | 15 px / 500 | |
| Code, Dateipfade | 15 px Monospace (ui-monospace, Consolas) | auf `#F9FAFB` |

## 6. Layout

Kopfleiste (`#B5321F`, ca. 64 px, oben fixiert – Citizen stellt sie standardmäßig senkrecht an den linken Rand, daher `$wgCitizenHeaderPosition = 'top'`): links Menü-Symbol und das runde EPOS-Plan-Logo im weißen Kreis plus Wortmarke „EPOS-Plan Dokumentation" (Manrope 700, weiß); daneben die Suche; rechts drei bis vier Links: Website (epos-plan.de), Download, Schulung, Update-Logbuch. Keine Benutzer- oder Bearbeiten-Links für Leser; „Anmelden" dezent im Footer.

Kapitelnavigation: aus `MediaWiki:Sidebar` in den Gruppen Erste Schritte · Programmablauf · Programmfunktionen · Grundlagen · Industrie & Gewerbe · Beispiele · Installation & Update · FAQ · Update-Logbuch. Citizen zeigt sie im ausklappbaren Menü der Kopfleiste; auf breiten Bildschirmen wird sie per CSS als feste linke Spalte (weiß, ca. 280 px, aktives Kapitel mit rotem Balken) eingeblendet – kleiner Zusatzaufwand in Phase 2. Zusätzlich erhält jede Seite unten eine Navigationsbox ihres Kapitels (Vorlage), damit Leser auch ohne Menü von Funktion zu Funktion kommen.

Inhalt: weiße Karte mit 8 px Radius und dezentem Schatten auf `#EEF1F5`, Innenabstand 40 px; darüber Brotkrumen („Programmfunktionen › Wärmepumpe"). Daneben das sticky Inhaltsverzeichnis „Auf dieser Seite" (H2/H3), das Citizen mitbringt.

Footer: hellgrau, Links Impressum · Datenschutz · Kontakt · AGB (auf epos-plan.de verweisend), „© 2026 INEKON", Anmelden-Link.

## 7. Wiederkehrende Bausteine (Vorlagen mit TemplateStyles)

Hinweisboxen `{{Hinweis}}`, `{{Tipp}}`, `{{Achtung}}`: 4 px Balken links, Icon im Kreis wie auf der Website (Hinweis: Fläche `#E0ECF0`, Icon `#1F2933`; Tipp: Fläche `#E6F4EA`, Icon `#2E7D32`; Achtung: Fläche `#FBE3E0`, Icon `#C0392B`). Keine dieser Vorlagen bringt MediaWiki mit – sie werden in Phase 2 angelegt. Menüpfade `{{Menü|Projekte|Neu}}` als graue Chips „Projekte › Neu". Kacheln `{{Kachel}}` für die Startseite (Icon-Kreis + Titel + ein Satz) im Stil der Website-Karten. Screenshots mit 1 px Rahmen, 8 px Radius und Bildunterschrift in 15 px. Tabellen (`wikitable`): Kopf `#F9FAFB`, Zeilen abwechselnd Weiß/`#F9FAFB`, Rahmen `#E3E6EA`. Stabile Sprungmarken `{{Anker|kennfeld}}` an allen Abschnitten, auf die die Software verlinkt – so überleben die Links spätere Umbenennungen von Überschriften.

## 8. Startseite und Struktur

Die Startseite wird zur Doku-Landingpage: roter Hero-Block mit „EPOS-Plan Dokumentation", einem Satz und großer Suche, darunter acht Kacheln (Erste Schritte, Programmablauf, Programmfunktionen, Grundlagen, Industrie & Gewerbe, Beispiele, Installation & Update, FAQ). Die bestehende Dokumentationsseite wird in rund 30 Einzelseiten aufgeteilt: je eine Seite pro Programmschritt (7) und pro Programmfunktion (15), dazu Grundlagen- und Beispielseiten. Adressen der Form `https://wiki.epos-plan.de/wiki/Wärmepumpe#Kennfeld` werden in der Software hinterlegt (Einstellungen › Adresse der Online-Dokumentation, Tooltips). Kategorien spiegeln die Sidebar-Gruppen.

## 9. Technische Eckpunkte (LocalSettings.php)

- MediaWiki auf den aktuellen 1.45er-Stand (1.45.4) aktualisieren; HTTPS (z. B. Let's Encrypt) und `$wgServer = "https://wiki.epos-plan.de"`.
- Kurz-URLs nach Manual:Short URL: Installation vom Wurzelverzeichnis nach `/w` verschieben, `$wgScriptPath = "/w"`, `$wgArticlePath = "/wiki/$1"` plus Rewrite-Regel – Artikel- und Skriptpfad dürfen nicht kollidieren.
- Citizen installieren, `$wgDefaultSkin = "citizen"`, `$wgCitizenHeaderPosition = 'top'`, `$wgCitizenShowPageTools = 'permission'`; Standard-Theme hell über `MediaWiki:Citizen-preferences.json` (`$wgCitizenThemeDefault` ist veraltet). Logo und Wortmarke über `$wgLogos` mit den Schlüsseln `icon` (SVG oder 100×100 px) und `wordmark` (`src`, `width`, `height`, max. 124×32 px) – `1x`/`2x` braucht Citizen nicht; dazu `$wgFavicon`.
- Rechte: `$wgGroupPermissions['*']['edit'] = false;` und `$wgGroupPermissions['*']['createaccount'] = false;` – Bearbeiten nur für angemeldete Nutzer, Konten legt der Administrator an.
- Erweiterungen, bereits im 1.45-Paket enthalten (nur `wfLoadExtension`): TemplateStyles (Bausteine), VisualEditor (Parsoid ist im Kern – komfortables Bearbeiten fürs Team ohne Zusatzdienst), CategoryTree, PageImages + TextExtracts (Vorschaubild und Beschreibung in der Citizen-Suche, dafür `$wgExtractsExtendRestSearch = true`). Separat zu installieren: Popups (Seitenvorschau, benötigt PageImages + TextExtracts), optional CodeMirror und ShortDescription.
- Styling: `MediaWiki:Citizen.css` (Farb- und Schriftvariablen, Kopfleiste, Karte, feste Navigationsspalte), `MediaWiki:Common.css` (Tabellen, Bilder, Druckansicht); Schrift- und Logodateien in einem eigenen Verzeichnis außerhalb des Core-Baums (z. B. `/assets/` im Document Root), damit Updates sie nicht überschreiben.
- `$wgSitename = "EPOS-Plan Dokumentation"`, `$wgLanguageCode = "de"`, `$wgEnableUploads = true` (Screenshots), Footer-Links Impressum/Datenschutz auf die Seiten von epos-plan.de.
- Verknüpfung: Menüpunkt „Dokumentation" auf epos-plan.de zeigt auf das Wiki, die Wiki-Kopfleiste verlinkt zurück.

## 10. Vorgehen

Phase 1 – Grundlage (etwa ½–1 Tag): Update auf 1.45.4, HTTPS, Kurz-URLs, Rechte, Citizen, Logo, Sitename.
Phase 2 – Gestaltung (etwa 1–2 Tage): Farb- und Schriftvariablen, Kopfleiste/Footer, feste Navigationsspalte, Startseite, Vorlagen für Hinweisboxen, Kacheln, Menüpfade und Kapitel-Navigationsboxen, Tabellen- und Bildstile, Druckansicht.
Phase 3 – Inhalte (Aufwand nach Umfang): Dokumentationsseite aufteilen, Screenshots, Anker setzen, Software-Links prüfen, Weiterleitungen von alten Adressen.

Als nächster Schritt kann auf dieser Basis das Umsetzungspaket folgen: LocalSettings-Snippet, Citizen.css/Common.css, Vorlagen und Logo-Dateien.
